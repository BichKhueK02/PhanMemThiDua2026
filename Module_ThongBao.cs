using Microsoft.Data.Sqlite;

namespace PhanMemThiDua2026
{
    // =========================================================
    // DÒNG THÔNG BÁO (CÓ MÀU)
    // =========================================================
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

    // MODULE THÔNG BÁO (STATIC – DÙNG TOÀN HỆ THỐNG)
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
            ThucThiUI(HienThongTinMacDinh);
        }
        public static void CapNhatThongTin()
        {
            ThucThiUI(HienThongTinMacDinh);
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
                _listBox.Items.Add(new DongThongBao($"{DateTime.Now:HH:mm:ss}  {text}", mau, false));

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
        private static void HienThongTinMacDinh()
        {
            if (_listBox == null || _listBox.IsDisposed) return;

            DateTime now = DateTime.Now;
            List<DongThongBao> dsGhimMoi = new List<DongThongBao>();

            try
            {
                string csdlPath = Module_DanduongGPS.DuongDanCSDL2;
                using (var conn = new SqliteConnection($"Data Source={csdlPath};Mode=ReadOnly"))
                {
                    conn.Open();
                    using var cmd = new SqliteCommand(
                        "SELECT DiaDiem, Ngay, Thang, Nam, ChiHuyD, LoaiDeNghi FROM ThongTin WHERE ID = 1", conn);

                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        string diaDiem = Dep(BaoMatAES.GiaiMa(reader["DiaDiem"]?.ToString() ?? ""));
                        string ngay = Dep(BaoMatAES.GiaiMa(reader["Ngay"]?.ToString() ?? ""));
                        string thang = Dep(BaoMatAES.GiaiMa(reader["Thang"]?.ToString() ?? ""));
                        string nam = Dep(BaoMatAES.GiaiMa(reader["Nam"]?.ToString() ?? ""));
                        string chiHuyD = Dep(BaoMatAES.GiaiMa(reader["ChiHuyD"]?.ToString() ?? ""));
                        string deNghi = Dep(BaoMatAES.GiaiMa(reader["LoaiDeNghi"]?.ToString() ?? ""));

                        // THÊM VÀO LIST TẠM VỚI CỜ LaGhim = true
                        dsGhimMoi.Add(new DongThongBao($"  Địa điểm: {diaDiem}, ngày {ngay} tháng {thang} năm {nam}", Color.MediumPurple, true));
                        dsGhimMoi.Add(new DongThongBao($"  Chỉ huy duyệt: {chiHuyD}", Color.MediumPurple, true));
                        dsGhimMoi.Add(new DongThongBao($"  KQ thông báo của Cụm thi đua: {deNghi}", Color.MediumPurple, true));
                    }
                }
            }
            catch (Exception ex)
            {
                dsGhimMoi.Add(new DongThongBao($"Lỗi khi lấy thông tin từ CSDL: {ex.Message}", Color.Red, true));
            }

            dsGhimMoi.Add(new DongThongBao($"  Thời gian đăng nhập: {now:HH:mm:ss dd/MM/yyyy}", Color.Gray, true));
            dsGhimMoi.Add(new DongThongBao(new string('-', 40), Color.Silver, true));

            _listBox.BeginUpdate();
            _listBox.ItemHeight = 22;

            // 1. XÓA CÁC DÒNG GHIM CŨ (NẾU CÓ)
            for (int i = _listBox.Items.Count - 1; i >= 0; i--)
            {
                if (_listBox.Items[i] is DongThongBao dtb && dtb.LaGhim)
                {
                    _listBox.Items.RemoveAt(i);
                }
            }

            // 2. CHÈN CÁC DÒNG GHIM MỚI VÀO ĐẦU LISTBOX
            for (int i = 0; i < dsGhimMoi.Count; i++)
            {
                _listBox.Items.Insert(i, dsGhimMoi[i]);
            }

            _listBox.EndUpdate();
        }
        private static void VeDong(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _listBox.Items.Count) return;

            e.DrawBackground();

            if (_listBox.Items[e.Index] is DongThongBao item)
            {
                using var brush = new SolidBrush(item.Mau);
                Rectangle r = new Rectangle(e.Bounds.X + 3, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height);
                e.Graphics.DrawString(item.Text, e.Font, brush, r);
            }

            e.DrawFocusRectangle();
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