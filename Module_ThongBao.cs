using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace PhanMemThiDua2026
{
    internal sealed class DongThongBao
    {
        public string Text { get; }
        public Color Mau { get; }
        // Cờ xác định dòng này là tiêu đề cố định (Ghim), không bị tự động xóa
        public bool LaGhim { get; }
        public DongThongBao(string text, Color mau, bool laGhim = false)
        {
            Text = text;
            Mau = mau;
            LaGhim = laGhim;
        }
        public override string ToString() => Text;
    }
    internal static class Module_ThongBao
    {
        private static ListBox _listBox;
        private static System.Windows.Forms.Timer _timer;
        // Biến lưu trữ số lượng tối đa đọc từ CSDL
        private static int _soDongCache = -1;
        // Hàm ĐỌC TỪ CSDL ĐÚNG YÊU CẦU
        private static int LaySoDongToiDa()
        {
            if (_soDongCache > 0) return _soDongCache;

            int val = 25; // Mặc định trong trường hợp xấu nhất

            try
            {
                string csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
                if (!string.IsNullOrWhiteSpace(csdl2Path) && System.IO.File.Exists(csdl2Path))
                {
                    using (var conn = new SqliteConnection($"Data Source={csdl2Path};Mode=ReadOnly"))
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT SoLanChoPhepHienThi FROM ThongTin WHERE ID = 1";
                            var result = cmd.ExecuteScalar();

                            if (result != null && result != DBNull.Value)
                            {
                                // 🔑 Gọi hàm giải mã
                                string giaiMa = BaoMatAES.GiaiMa(result.ToString()).Trim();
                                if (!string.IsNullOrEmpty(giaiMa))
                                {
                                    // Ép sang số nguyên
                                    if (int.TryParse(giaiMa, out int soTuCSDL))
                                    {
                                        val = soTuCSDL;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Nếu lỗi, vẫn dùng giá trị mặc định là 25
            }

            // Chốt số (từ 3 đến 30)
            val = Math.Max(3, Math.Min(30, val));
            _soDongCache = val;

            return _soDongCache;
        }
        // Gọi ở Form Cài đặt khi có thay đổi
        // Mở file chứa Module_ThongBao và tìm đến khu vực có ResetCacheSoDong()

        public static void ResetCacheSoDong()
        {
            // Đặt lại biến cache để ép hàm LaySoDongToiDa() đọc lại từ SQLite
            _soDongCache = -1;

            // Gọi lại hàm để đọc ngay lập tức giá trị mới
            int gioiHanMoi = LaySoDongToiDa();

            // TIẾN HÀNH XÓA BỚT DÒNG NẾU ĐANG VƯỢT GIỚI HẠN
            ThucThiUI(() =>
            {
                if (_listBox == null || _listBox.IsDisposed) return;

                // Đếm số dòng không ghim hiện tại
                int soLuongHienTai = _listBox.Items.Cast<DongThongBao>().Count(x => !x.LaGhim);

                // Nếu số lượng hiện tại đang lớn hơn giới hạn mới thiết lập
                if (soLuongHienTai > gioiHanMoi)
                {
                    _listBox.BeginUpdate();

                    // Xóa sạch các dòng cũ (Logic phân trang của bạn)
                    for (int i = _listBox.Items.Count - 1; i >= 0; i--)
                    {
                        if (_listBox.Items[i] is DongThongBao dtb && !dtb.LaGhim)
                        {
                            _listBox.Items.RemoveAt(i);
                        }
                    }

                    _listBox.EndUpdate();

                    // In ra một thông báo xác nhận
                    ThemDong($"Đã áp dụng giới hạn mới: {gioiHanMoi} dòng.", Color.MediumPurple);
                }
            });
        }
        private const int THOI_GIAN_TU_XOA = 30000; // 30 giây
        public static void GanListBox(ListBox listBox)
        {
            if (listBox == null || listBox.IsDisposed) return;

            _listBox = listBox;

            _listBox.DrawMode = DrawMode.OwnerDrawFixed;
            _listBox.DrawItem -= VeDong;
            _listBox.DrawItem += VeDong;

            // Xóa sạch ở lần load Form đầu tiên
            _listBox.Items.Clear();

            // Kích hoạt việc đọc CSDL ngay từ đầu
            LaySoDongToiDa();

            KhoiTaoTimer();
            ThucThiUI(async () => await HienThongTinMacDinhAsync());
        }
        public static void CapNhatThongTin()
        {
            ThucThiUI(async () => await HienThongTinMacDinhAsync());
        }
        public static void Info(string s) => ThemDong("✔ " + s, Color.MediumPurple);
        public static void DangXuLy(string s) => ThemDong("⏳ " + s, Color.DarkOrange);
        public static void ThanhCong(string s) => ThemDong("✔ " + s, Color.ForestGreen);
        public static void Loi(string s) => ThemDong("❌ " + s, Color.Red);
        public static void XoaTatCa()
        {
            ThucThiUI(() =>
            {
                if (_listBox == null || _listBox.IsDisposed) return;

                _listBox.BeginUpdate();
                // Duyệt ngược từ dưới lên để xóa các dòng KHÔNG GHIM
                for (int i = _listBox.Items.Count - 1; i >= 0; i--)
                {
                    if (_listBox.Items[i] is DongThongBao dtb && !dtb.LaGhim)
                    {
                        _listBox.Items.RemoveAt(i);
                    }
                }
                _listBox.EndUpdate();
            });
        }
        private static void ThemDong(string text, Color mau)
        {
            ThucThiUI(() =>
            {
                if (_listBox == null || _listBox.IsDisposed) return;

                int gioiHanDong = LaySoDongToiDa();

                // Đếm số lượng thông báo KHÔNG ghim (chỉ đếm phần động)
                int soLuongHienTai = _listBox.Items.Cast<DongThongBao>().Count(x => !x.LaGhim);

                // LOGIC PHÂN TRANG: Xóa sạch dòng cũ nếu đạt ngưỡng, bắt đầu lại chu kỳ mới
                if (soLuongHienTai >= gioiHanDong)
                {
                    _listBox.BeginUpdate();
                    for (int i = _listBox.Items.Count - 1; i >= 0; i--)
                    {
                        if (_listBox.Items[i] is DongThongBao dtb && !dtb.LaGhim)
                        {
                            _listBox.Items.RemoveAt(i);
                        }
                    }
                    _listBox.EndUpdate();
                }

                // Thêm dòng mới
                _listBox.Items.Add(new DongThongBao($"{DateTime.Now:HH:mm:ss} {text}", mau, false));

                // Cuộn xuống cuối
                _listBox.TopIndex = _listBox.Items.Count - 1;
                _listBox.SelectedIndex = _listBox.Items.Count - 1;

                if (_timer != null)
                {
                    _timer.Stop();
                    _timer.Start();
                }
            });
        }
        private static void KhoiTaoTimer()
        {
            if (_timer != null) return;

            _timer = new System.Windows.Forms.Timer
            {
                Interval = THOI_GIAN_TU_XOA
            };

            _timer.Tick += (s, e) =>
            {
                _timer.Stop();
                XoaTatCa();
            };
        }
        private static async Task HienThongTinMacDinhAsync()
        {
            if (_listBox == null || _listBox.IsDisposed) return;

            DateTime now = DateTime.Now;
            var dsGhimMoi = new List<DongThongBao>();

            try
            {
                string csdlPath = Module_DanduongGPS.DuongDanCSDL2;
                if (string.IsNullOrWhiteSpace(csdlPath) || !File.Exists(csdlPath))
                {
                    return;
                }

                // TỐI ƯU 1: Dùng Pooling=True và Mode=ReadOnly để đọc file SQLite cực nhanh
                string connectionString = $"Data Source={csdlPath};Mode=ReadOnly;Pooling=True;";

                using (var conn = new SqliteConnection(connectionString))
                {
                    // TỐI ƯU 2: Dùng OpenAsync để không gây đơ/khựng giao diện (UI Thread)
                    await conn.OpenAsync();

                    // AN TOÀN 1: Tạo bảng CheDo_XetThiDuaNam nếu chưa có để tránh văng Exception "no such table"
                    using (var cmdInit = new SqliteCommand(@"
                CREATE TABLE IF NOT EXISTS CheDo_XetThiDuaNam (
                    ID INTEGER NOT NULL PRIMARY KEY,
                    ChoPhepCheDoXetThiDuaNam TEXT
                );", conn))
                    {
                        await cmdInit.ExecuteNonQueryAsync();
                    }

                    // TỐI ƯU 3: Lấy toàn bộ thông tin chỉ qua 1 câu truy vấn LEFT JOIN duy nhất
                    string query = @"
                SELECT 
                    t.DiaDiem, t.Ngay, t.Thang, t.Nam, t.ChiHuyD, t.LoaiDeNghi,
                    c.ChoPhepCheDoXetThiDuaNam
                FROM ThongTin t
                LEFT JOIN CheDo_XetThiDuaNam c ON c.ID = 1
                WHERE t.ID = 1 LIMIT 1";

                    using var cmd = new SqliteCommand(query, conn);
                    using var reader = await cmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        // AN TOÀN 2: Giải mã bảo vệ Null-safe
                        string diaDiem = Dep(BaoMatAES.GiaiMa(reader["DiaDiem"]?.ToString() ?? ""));
                        string ngay = Dep(BaoMatAES.GiaiMa(reader["Ngay"]?.ToString() ?? ""));
                        string thang = Dep(BaoMatAES.GiaiMa(reader["Thang"]?.ToString() ?? ""));
                        string nam = Dep(BaoMatAES.GiaiMa(reader["Nam"]?.ToString() ?? ""));
                        string chiHuyD = Dep(BaoMatAES.GiaiMa(reader["ChiHuyD"]?.ToString() ?? ""));
                        string deNghi = Dep(BaoMatAES.GiaiMa(reader["LoaiDeNghi"]?.ToString() ?? ""));

                        // Chế độ xét thi đua lưu text thuần, Fallback về "Tháng" nếu chưa có dữ liệu
                        string cheDoThiDua = reader["ChoPhepCheDoXetThiDuaNam"]?.ToString();
                        if (string.IsNullOrWhiteSpace(cheDoThiDua))
                        {
                            cheDoThiDua = "Tháng";
                        }

                        // Kiểm tra logic chế độ để đổi tiêu đề tương ứng
                        bool isCheDoNam = cheDoThiDua.Equals("Năm", StringComparison.OrdinalIgnoreCase);
                        string nhanPhanLoai = isCheDoNam ? "Danh hiệu thi đua tập thể" : "Kết quả phân loại tập thể";

                        // Nạp vào danh sách ghim
                        dsGhimMoi.Add(new DongThongBao($"  Địa điểm: {diaDiem}, ngày {ngay} tháng {thang} năm {nam}", Color.MediumPurple, true));
                        dsGhimMoi.Add(new DongThongBao($"  Chỉ huy duyệt: {chiHuyD}", Color.MediumPurple, true));
                        dsGhimMoi.Add(new DongThongBao($"  Chế độ xét thi đua: {cheDoThiDua}", Color.MediumPurple, true));
                        dsGhimMoi.Add(new DongThongBao($"  {nhanPhanLoai}: {deNghi}", Color.MediumPurple, true));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi HienThongTinMacDinh: " + ex.Message);
                dsGhimMoi.Add(new DongThongBao($"Lỗi khi lấy thông tin từ CSDL: {ex.Message}", Color.Red, true));
            }

            dsGhimMoi.Add(new DongThongBao($"  Thời gian đăng nhập: {now:HH:mm:ss dd/MM/yyyy}", Color.Gray, true));
            dsGhimMoi.Add(new DongThongBao(new string('-', 40), Color.Silver, true));

            // CẬP NHẬT GIAO DIỆN LẠI TRÊN UI THREAD
            _listBox.BeginUpdate();
            try
            {
                _listBox.ItemHeight = 22;

                // Xóa các dòng ghim cũ
                for (int i = _listBox.Items.Count - 1; i >= 0; i--)
                {
                    if (_listBox.Items[i] is DongThongBao dtb && dtb.LaGhim)
                    {
                        _listBox.Items.RemoveAt(i);
                    }
                }

                // Chèn danh sách mới vào đầu ListBox
                for (int i = 0; i < dsGhimMoi.Count; i++)
                {
                    _listBox.Items.Insert(i, dsGhimMoi[i]);
                }
            }
            finally
            {
                _listBox.EndUpdate(); // AN TOÀN 3: Đảm bảo luôn EndUpdate dù có lỗi xảy ra để không đơ ListBox
            }
        }
        private static void VeDong(object sender, DrawItemEventArgs e)
        {
            if (_listBox == null ||
                _listBox.IsDisposed ||
                e.Index < 0 ||
                e.Index >= _listBox.Items.Count)
            {
                return;
            }

            bool dangChon = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            // ============================================================
            // 1. MÀU NỀN
            // ============================================================
            Color mauNen;

            if (dangChon)
            {
                // Xanh lá rất nhạt -> nhẹ mắt, hiện đại, không lấn át nội dung
                mauNen = Color.FromArgb(220, 245, 225);
            }
            else
            {
                // Nền bình thường
                mauNen = _listBox.BackColor;
            }

            using (var brushNen = new SolidBrush(mauNen))
            {
                e.Graphics.FillRectangle(brushNen, e.Bounds);
            }

            // ============================================================
            // 2. VẼ NỘI DUNG
            // ============================================================
            if (_listBox.Items[e.Index] is DongThongBao item)
            {
                Color mauChu;

                if (dangChon)
                {
                    // Khi chọn -> dùng xanh lá đậm để tương phản tốt
                    mauChu = Color.FromArgb(35, 120, 65);
                }
                else
                {
                    // Bình thường -> giữ nguyên màu từng loại thông báo
                    mauChu = item.Mau;
                }

                using var brushChu = new SolidBrush(mauChu);

                Rectangle r = new Rectangle(
                    e.Bounds.X + 8,
                    e.Bounds.Y,
                    e.Bounds.Width - 8,
                    e.Bounds.Height);

                e.Graphics.DrawString(
                    item.Text,
                    e.Font,
                    brushChu,
                    r);
            }

            // ============================================================
            // 3. KHÔNG VẼ KHUNG FOCUS XANH/ĐEN MẶC ĐỊNH
            // ============================================================
            // Không gọi e.DrawFocusRectangle();
        }
        private static void ThucThiUI(Action action)
        {
            if (_listBox == null || _listBox.IsDisposed) return;

            if (_listBox.InvokeRequired)
                _listBox.Invoke(action);
            else
                action();
        }
        private static string Dep(string s) => string.IsNullOrWhiteSpace(s) ? "          " : s;
    }
}