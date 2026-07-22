using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
namespace PhanMemThiDua2026
{
    public partial class Form4_TrangDauTien : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private Form14? formTinhToan;
        private Panel? piePanel;
        private int highlightedSlice = -1;
        private Dictionary<string, int> pieData = new Dictionary<string, int>();
        private int[] loai1;
        private int[] loai2;
        private int[] loai3;
        private int phanTramLoai1 = 0;
        private int phanTramLoai2 = 0;
        private int phanTramLoai3 = 0;
        private int _cacheCounter = 0; // Đếm siêu nhẹ thay vì dùng .Count
        private int _isClearingCache = 0;
        private bool isComboBoxInitDone = false;
        private bool _allowLoadBang2 = true;
        private readonly Icon _appIcon;
        private readonly Image _iconTrue = Properties.Resources._true;   // Đảm bảo tên file trong Resources là true
        private readonly Image _iconFalse = Properties.Resources._false; // Đảm bảo tên file trong Resources là false
       // private Dictionary<string, string> _dictChiHuyD = new Dictionary<string, string>();
        private Font? _cachedGridFont;
        private Font? _cachedGridFontBold;
        private Font? _cachedGrid2HeaderFont;
        private int _cachedTongBCH = -1; // -1 nghĩa là chưa đếm
                                         // Khai báo Dictionary hỗ trợ tìm kiếm không phân biệt chữ hoa / chữ thường
        private Dictionary<string, string> _dictChiHuyD = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public void ResetCacheBCH()
        {
            _cachedTongBCH = -1;
        }
        private SqliteConnection TaoKetNoiCSDL2(bool readOnly = false)
        {
            string path = _csdl2Path;

            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Đường dẫn CSDL2 chưa được cấu hình.");

            path = Path.GetFullPath(path);

            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "Không tìm thấy CSDL2.",
                    path);

            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = readOnly
                    ? SqliteOpenMode.ReadOnly
                    : SqliteOpenMode.ReadWrite,

                Pooling = true,
                DefaultTimeout = 10
            };

            return new SqliteConnection(
                builder.ConnectionString);
        }
        public Form4_TrangDauTien()
        {
            InitializeComponent();
            try
            {
                using (var ms = new System.IO.MemoryStream(Properties.Resources.ic_PhanMem))
                {
                    _appIcon = new Icon(ms);
                }
                this.Icon = _appIcon; // Gán icon cho Form
            }
            catch (Exception)
            {
                // Nếu lỗi, sử dụng icon mặc định của hệ thống để không crash app
            }

            Shown += Form4_Shown;
            Load += Form4_Load;
            // Thêm dòng này để đăng ký sự kiện tự động căn chỉnh
            this.Resize += Form4_TrangDauTien_Resize;
            InitToolTips();
            com_DeNghi.SelectedIndexChanged += Com_DeNghi_SelectedIndexChanged;
            comboBox1_ChonLoaiBaoCao.SelectedIndexChanged += comboBox1_ChonLoaiBaoCao_SelectedIndexChanged;
            if (kryptonDataGridView2 != null)
            {
                kryptonDataGridView2.CellPainting += KryptonDataGridView2_CellPainting;
            }

            // XÓA BEGININVOKE ĐI. CHẠY TRỰC TIẾP NHƯ THẾ NÀY:
            comboBox_DiaDiem.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            comboBox_DiaDiem.AutoCompleteSource = AutoCompleteSource.ListItems;
        }
        private bool _hasLoaded = false; // Cờ bảo hiểm chặn chạy trùng lặp hàm Load
        private async void Form4_Load(object sender, EventArgs e)
        {
            // 🌟 CHỐT CHẶN BẢO HIỂM: Nếu đã nạp rồi thì thoát ngay, không chạy lại code phía dưới
            if (_hasLoaded) return;
            _hasLoaded = true;

            try
            {
                // ===== LOAD NGẦM DỮ LIỆU & CAU HÌNH CƠ BẢN =====
                LoadSettings();
                LoadCheckBoxTuDongChonNgayThang();
                Module_QuyDinhTyLe.LoadE29(this.Controls);

                loai1 = Module_QuyDinhTyLe.GetLoaiTapThe("Loai1_TapThe");
                loai2 = Module_QuyDinhTyLe.GetLoaiTapThe("Loai2_TapThe");
                loai3 = Module_QuyDinhTyLe.GetLoaiTapThe("Loai3_TapThe");

                comboBox_ChiHuyD.SelectedIndexChanged -= ComboBox_ChiHuyD_SelectedIndexChanged;
                comboBox_ChiHuyD.SelectedIndexChanged += ComboBox_ChiHuyD_SelectedIndexChanged;
                // Nạp CSDL ngầm lên RAM bằng bất đồng bộ
                await ReloadDuLieuAsync();
                LoadDuLieuChiHuyVaoComboBox();
                Module_DanduongGPS.OnDatabaseChanged -= SuKien_DatabaseChanged;
                Module_DanduongGPS.OnDatabaseChanged += SuKien_DatabaseChanged;
                // Dừng 200 mili-giây để người dùng kịp đọc thông báo "Đã lưu thành công"
               // await Task.Delay(200);

                // =========================================================================
                // ⭐ KIỂM TRA VÀ BÁO CÁO PHẦN MỀM ĐANG TRONG QUÁ TRÌNH LẬP TRÌNH / KIỂM TRA
                // =========================================================================
               // string duongDanChay = AppContext.BaseDirectory.ToLower();

                // Kiểm tra xem đường dẫn có chứa cụm thư mục build của Visual Studio hoặc đang cắm dây gỡ lỗi hay không
                //bool laMoiTruongLapTrinh = duongDanChay.Contains("bin\\release") ||
                //                           duongDanChay.Contains("bin\\debug") ||
                //                           System.Diagnostics.Debugger.IsAttached;

                //if (laMoiTruongLapTrinh)
                //{
                //    MessageBox.Show(
                //        "THÔNG BÁO HỆ THỐNG:\n\n" +
                //        "Phần mềm đang chạy trong môi trường lập trình / kiểm tra thử nghiệm (Thư mục bin\\Release\\net8.0-windows).\n\n" +
                //        "⚠️ Lưu ý: Các tính năng ghi nhận dữ liệu và phân vùng bộ nhớ có thể thay đổi liên tục trong quá trình dựng mã nguồn.",
                //        "Chế độ kiểm tra lập trình",
                //        MessageBoxButtons.OK,
                //        MessageBoxIcon.Information);
                //}
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi trong Form4_Load: " + ex.Message);
            }
        }
        private async void Form4_Shown(object sender, EventArgs e)
        {
            try
            {
                // 1️⃣ Nhịp nghỉ ngắn (150ms) cho luồng đồ họa Windows dựng xong Form4 lên màn hình
                await Task.Delay(150);

                // Kiểm tra an toàn: Nếu cán bộ tắt form nhanh trước 150ms thì thoát ngay
                if (this.IsDisposed || !this.IsHandleCreated) return;

                // 2️⃣ GỘP CHUNG TOÀN BỘ TÁC VỤ VẼ UI VÀO 1 KHỐI ĐỂ TRÁNH NGHẼN LUỒNG CHÍNH
                this.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (this.IsDisposed) return;

                        // Setup UI cơ bản
                        datChieuCaoNut();
                        AnIDGrid(kryptonDataGridView1);

                        // Vẽ đồ thị và gán thông báo
                        KhoiTaoPieChart();
                        Module_ThongBao.GanListBox(listBox1);
                        Module_ThongBao.Info("Trang chủ đã sẵn sàng");

                        // Thiết lập AutoComplete cho ComboBox địa điểm
                        comboBox_DiaDiem.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                        comboBox_DiaDiem.AutoCompleteSource = AutoCompleteSource.ListItems;

                        // Xóa tiêu điểm (Focus) mặc định để giao diện sạch sẽ, chuyên nghiệp
                        this.ActiveControl = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Lỗi xử lý UI trong BeginInvoke: " + ex.Message);
                    }
                }));

                // 3️⃣ NHỊP NGHỈ UX 2.5 GIÂY - Cho cán bộ nhìn tổng quan bảng biểu, biểu đồ tròn
                await Task.Delay(2500);

                // Kiểm tra fail-safe một lần nữa trước khi bật Form mới
                if (this.IsDisposed || !this.IsHandleCreated) return;

                // 4️⃣ KÍCH HOẠT DUY NHẤT 1 LẦN CƠ CHẾ CHÀO MỪNG / HƯỚNG DẪN NGƯỜI DÙNG
                ModuleWelcome.ShowWelcomeIfNeeded();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi luồng trong Form4_Shown: " + ex.Message);
            }
        }
        private void LoadDuLieuChiHuyVaoComboBox()
        {
            _dictChiHuyD.Clear();
            comboBox_ChiHuyD.Items.Clear();

            bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
            string tableChiHuy = laTanBinh ? "ChiHuyD_TanBinh" : "ChiHuyD";

            try
            {
                using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT HoVaTen, ChucVu FROM [{tableChiHuy}] WHERE ID BETWEEN 1 AND 6 ORDER BY ID";
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    string hoTen = GiaiMaSafe(reader["HoVaTen"]).Trim();
                    string chucVu = GiaiMaSafe(reader["ChucVu"]).Trim();

                    if (!string.IsNullOrEmpty(hoTen))
                    {
                        // 1. Lưu vào Dictionary
                        _dictChiHuyD[hoTen] = chucVu;

                        // 2. Add vào ComboBox
                        comboBox_ChiHuyD.Items.Add(hoTen);
                    }
                }

                // 3. Chọn mặc định dòng đầu tiên & Ép Label cập nhật ngay khi mở Form
                if (comboBox_ChiHuyD.Items.Count > 0)
                {
                    comboBox_ChiHuyD.SelectedIndex = 0;

                    // Gọi trực tiếp hàm sự kiện để đảm bảo Label2 nhảy chữ ngay lập tức
                    ComboBox_ChiHuyD_SelectedIndexChanged(comboBox_ChiHuyD, EventArgs.Empty);
                }
                else
                {
                    label2_ChucVu.Text = "";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi nạp ComboBox Chỉ huy: " + ex.Message);
            }
        }
        private string GiaiMaSafe(object? value)
        {
            if (value == null || value == DBNull.Value) return string.Empty;
            string input = value.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            try
            {
                string result = BaoMatAES.GiaiMa(input);
                // Nếu giải mã thành công thì trả về kết quả, nếu rỗng thì trả về dữ liệu thô ban đầu
                return string.IsNullOrEmpty(result) ? input : result;
            }
            catch
            {
                return input; // Trường hợp dữ liệu chưa mã hóa (chuỗi thường)
            }
        }
        private void SuKien_DatabaseChanged()
        {
            if (this.IsHandleCreated && !this.IsDisposed)
            {
                this.BeginInvoke(new Action(async () => await ReloadDuLieuAsync()));
            }
        }
        private void KryptonDataGridView2_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            // 1. Guard Clauses (Kiểm tra an toàn và thoát sớm)
            if (sender is not DataGridView dgv) return;
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgv.Columns[e.ColumnIndex].Name != "Ket luan") return;

            // Chỉ xử lý dòng có STT (ID) = 1
            var sttCell = dgv.Rows[e.RowIndex].Cells["ID"].Value?.ToString();
            if (sttCell != "1") return;

            string noiDung = e.Value?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(noiDung)) return;

            // 2. Xác định trạng thái, Icon và Màu sắc mặc định
            bool isChuaDat = noiDung.IndexOf("Chưa đạt", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isDaDat = noiDung.IndexOf("Đạt tỷ lệ", StringComparison.OrdinalIgnoreCase) >= 0;

            Image iconToDraw = null;
            Color textColor = e.CellStyle.ForeColor;

            if (isChuaDat)
            {
                iconToDraw = _iconFalse;
                textColor = Color.Red;
            }
            else if (isDaDat)
            {
                iconToDraw = _iconTrue;
                textColor = Color.DarkGreen;
            }

            // Nếu không thỏa điều kiện nào, nhả lại cho DataGridView tự vẽ và thoát
            if (iconToDraw == null)
            {
                e.PaintContent(e.CellBounds);
                return;
            }

            // 3. Bắt đầu vẽ: Xóa nền gốc, giữ lại hiệu ứng Bôi đen/Focus
            e.Paint(e.CellBounds, DataGridViewPaintParts.Background |
                                  DataGridViewPaintParts.SelectionBackground |
                                  DataGridViewPaintParts.Focus);

            // Đổi màu chữ thành trắng nếu dòng đang được bôi đen (chọn)
            if ((e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected)
            {
                textColor = e.CellStyle.SelectionForeColor;
            }

            // 4. Các hằng số kích thước
            const int ICON_SIZE = 16;
            const int PADDING_ICON_TEXT = 6;

            // 5. Tính toán tọa độ và Vẽ (Đóng gói Font vào khối using để tự động giải phóng RAM)
            using (Font boldFont = new Font(e.CellStyle.Font, FontStyle.Bold))
            {
                // Đo chiều rộng chữ
                int textWidth = TextRenderer.MeasureText(e.Graphics, noiDung, boldFont).Width;

                // Tính tổng chiều rộng và Tọa độ X để cụm (Icon + Text) nằm ngay giữa ô
                int totalContentWidth = ICON_SIZE + PADDING_ICON_TEXT + textWidth;
                int xIcon = e.CellBounds.X + Math.Max(0, (e.CellBounds.Width - totalContentWidth) / 2);
                int yIcon = e.CellBounds.Y + (e.CellBounds.Height - ICON_SIZE) / 2;

                // Vẽ Icon
                e.Graphics.DrawImage(iconToDraw, new Rectangle(xIcon, yIcon, ICON_SIZE, ICON_SIZE));

                // Xác định không gian vẽ chữ (Dùng Math.Max để tránh lỗi Exception Width bị âm khi kéo cột quá hẹp)
                int textStartX = xIcon + ICON_SIZE + PADDING_ICON_TEXT;
                Rectangle textBounds = new Rectangle(
                    textStartX,
                    e.CellBounds.Y,
                    Math.Max(0, e.CellBounds.Width - (textStartX - e.CellBounds.X)),
                    e.CellBounds.Height
                );

                // Vẽ Text in đậm
                TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;
                TextRenderer.DrawText(e.Graphics, noiDung, boldFont, textBounds, textColor, flags);
            }

            // 6. Khóa sự kiện: Báo cho hệ thống biết ta đã tự tay vẽ xong, không cần vẽ đè văn bản gốc lên nữa
            e.Handled = true;
        }
        private readonly SemaphoreSlim _reloadLock = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<string, string> _uiDecryptCache = new(StringComparer.Ordinal);
        private class ThongKeDonVi
        {
            public int TongQS { get; set; }
            public int Loai1 { get; set; }
            public int Loai2 { get; set; }
            public int Loai3 { get; set; }
            public int Loai4 { get; set; }
            public int KhongPL { get; set; }
        }
        private void KryptonDataGridView1_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            var grid = sender as DataGridView;
            if (grid == null || grid.Font == null) return;

            string rowIdx = (e.RowIndex + 1).ToString();

            Rectangle headerBounds = new Rectangle(
                e.RowBounds.Left,
                e.RowBounds.Top,
                grid.RowHeadersWidth,
                e.RowBounds.Height);

            // Tuyệt đối KHÔNG DÙNG new Font() ở đây. Dùng luôn grid.Font
            // Dùng TextRenderer siêu tốc của Windows Core thay cho Graphics.DrawString
            TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding;
            TextRenderer.DrawText(e.Graphics, rowIdx, grid.Font, headerBounds, Color.Black, flags);
        }
        private HashSet<string> _dsDonViBoQuaCanhBao = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private async Task Bang1_Async()
        {
            string csdl2Path = _csdl2Path;
            if (string.IsNullOrWhiteSpace(csdl2Path) || !File.Exists(csdl2Path))
            {
                Debug.WriteLine("Không tìm thấy CSDL phụ.");
                return;
            }

            try
            {
                var builder = new SqliteConnectionStringBuilder
                {
                    DataSource = csdl2Path,
                    Mode = SqliteOpenMode.ReadWriteCreate,
                    Cache = SqliteCacheMode.Shared,
                    Pooling = true,
                    DefaultTimeout = 15
                };

                using (var conn = new SqliteConnection(builder.ToString()))
                {
                    await conn.OpenAsync();

                    using (var pragmaCmd = conn.CreateCommand())
                    {
                        pragmaCmd.CommandText = "PRAGMA journal_mode = WAL; PRAGMA synchronous = NORMAL; PRAGMA temp_store = MEMORY;";
                        await pragmaCmd.ExecuteNonQueryAsync();
                    }

                    // ====================================================================
                    // 🌟 THÊM CODE: Tải danh sách đơn vị cấu hình bỏ qua cảnh báo lên RAM
                    // ====================================================================
                    _dsDonViBoQuaCanhBao.Clear();
                    using (var cmdBoQua = new SqliteCommand("SELECT Ten_DonViBoQuaCanhBaoTyLe FROM ChonDonVi_DeBoQuaCanhBao", conn))
                    using (var rdBoQua = await cmdBoQua.ExecuteReaderAsync())
                    {
                        while (await rdBoQua.ReadAsync())
                        {
                            string tenDvBc = rdBoQua["Ten_DonViBoQuaCanhBaoTyLe"]?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(tenDvBc))
                            {
                                _dsDonViBoQuaCanhBao.Add(tenDvBc);
                            }
                        }
                    }
                    // ====================================================================

                    // 1. Chuẩn bị danh sách ưu tiên hiển thị trước
                    string[] donViArr = Module_DonVi.LayDanhSachDonViUuTienArray().Select(d => (d ?? string.Empty).Trim()).ToArray();
                    List<string> thuTuDonVi = new List<string>(donViArr);
                    var dictThongKe = new Dictionary<string, ThongKeDonVi>(StringComparer.OrdinalIgnoreCase);

                    foreach (var dv in thuTuDonVi)
                    {
                        if (!string.IsNullOrWhiteSpace(dv)) dictThongKe[dv] = new ThongKeDonVi();
                    }

                    // 2. Đọc trực tiếp từ Ổ cứng -> RAM bằng Stream, kết hợp Trạm gác L1
                    using (var cmd = new SqliteCommand("SELECT DonVi, PhanLoai FROM DanhSach", conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string donvi = SafeDecrypt(reader["DonVi"]);
                            if (string.IsNullOrWhiteSpace(donvi)) continue;

                            string phanloai = SafeDecrypt(reader["PhanLoai"]);

                            // TỰ ĐỘNG MỞ RỘNG: Nếu đơn vị lạ (C1, Ban TM...) xuất hiện, lập tức thêm vào danh sách
                            if (!dictThongKe.TryGetValue(donvi, out ThongKeDonVi tk))
                            {
                                tk = new ThongKeDonVi();
                                dictThongKe[donvi] = tk;
                                thuTuDonVi.Add(donvi);
                            }

                            tk.TongQS++;
                            switch (phanloai)
                            {
                                case "Loại 1": tk.Loai1++; break;
                                case "Loại 2": tk.Loai2++; break;
                                case "Loại 3": tk.Loai3++; break;
                                case "Loại 4": tk.Loai4++; break;
                                default: tk.KhongPL++; break;
                            }
                        }
                    }

                    // 3. Đổ dữ liệu ra DataTable
                    DataTable dtGrid = new DataTable();
                    dtGrid.Columns.Add("DonVi", typeof(string)); dtGrid.Columns.Add("TongQS", typeof(int));
                    dtGrid.Columns.Add("Loai_1", typeof(int)); dtGrid.Columns.Add("Loai_2", typeof(int));
                    dtGrid.Columns.Add("Loai_3", typeof(int)); dtGrid.Columns.Add("Loai_4", typeof(int));
                    dtGrid.Columns.Add("Khong_PL", typeof(int)); dtGrid.Columns.Add("PhanTramLoai_1", typeof(string));
                    dtGrid.Columns.Add("PhanTramLoai_2", typeof(string)); dtGrid.Columns.Add("PhanTramLoai_3", typeof(string));
                    dtGrid.Columns.Add("PhanTramLoai_4", typeof(string)); dtGrid.Columns.Add("PhanTramKhong_PL", typeof(string));

                    int tong_tongQS = 0, tong_l1 = 0, tong_l2 = 0, tong_l3 = 0, tong_l4 = 0, tong_kpl = 0;

                    foreach (var tenDonVi in thuTuDonVi)
                    {
                        if (!dictThongKe.TryGetValue(tenDonVi, out ThongKeDonVi tk) || tk.TongQS <= 0) continue;

                        int tong = tk.TongQS;
                        string pt1 = (tk.Loai1 + tk.Loai2) > 0 ? Math.Round(tk.Loai1 * 100.0 / (tk.Loai1 + tk.Loai2), 2).ToString() : "0";
                        string pt2 = tong > 0 ? Math.Round((tk.Loai1 + tk.Loai2) * 100.0 / tong, 2).ToString() : "0";
                        string pt3 = tong > 0 ? Math.Round(tk.Loai3 * 100.0 / tong, 2).ToString() : "0";
                        string pt4 = tong > 0 ? Math.Round(tk.Loai4 * 100.0 / tong, 2).ToString() : "0";
                        string ptKPL = tong > 0 ? Math.Round(tk.KhongPL * 100.0 / tong, 2).ToString() : "0";

                        dtGrid.Rows.Add(tenDonVi, tong, tk.Loai1, tk.Loai2, tk.Loai3, tk.Loai4, tk.KhongPL, pt1, pt2, pt3, pt4, ptKPL);

                        tong_tongQS += tong; tong_l1 += tk.Loai1; tong_l2 += tk.Loai2;
                        tong_l3 += tk.Loai3; tong_l4 += tk.Loai4; tong_kpl += tk.KhongPL;
                    }

                    if (dtGrid.Rows.Count > 0)
                    {
                        string pt1_tot = (tong_l1 + tong_l2) > 0 ? Math.Round(tong_l1 * 100.0 / (tong_l1 + tong_l2), 2).ToString() : "0";
                        string pt2_tot = tong_tongQS > 0 ? Math.Round((tong_l1 + tong_l2) * 100.0 / tong_tongQS, 2).ToString() : "0";
                        string pt3_tot = tong_tongQS > 0 ? Math.Round(tong_l3 * 100.0 / tong_tongQS, 2).ToString() : "0";
                        string pt4_tot = tong_tongQS > 0 ? Math.Round(tong_l4 * 100.0 / tong_tongQS, 2).ToString() : "0";
                        string ptKPL_tot = tong_tongQS > 0 ? Math.Round(tong_kpl * 100.0 / tong_tongQS, 2).ToString() : "0";
                        dtGrid.Rows.Add("Tổng cộng", tong_tongQS, tong_l1, tong_l2, tong_l3, tong_l4, tong_kpl, pt1_tot, pt2_tot, pt3_tot, pt4_tot, ptKPL_tot);
                    }

                    // 4. Ghi SQLite (Cấu trúc của bạn giữ nguyên vì đã ổn định)
                    using (var tran = (SqliteTransaction)await conn.BeginTransactionAsync())
                    {
                        try
                        {
                            using (var cmdSave = conn.CreateCommand())
                            {
                                cmdSave.Transaction = tran;
                                cmdSave.CommandText = "DELETE FROM QuanSoThiDuaD2;";
                                await cmdSave.ExecuteNonQueryAsync();
                                cmdSave.CommandText = @"INSERT INTO QuanSoThiDuaD2 (DonVi, TongQS, Loai_1, Loai_2, Loai_3, Loai_4, Khong_PL) VALUES (@DonVi, @TongQS, @L1, @L2, @L3, @L4, @KPL);";
                                foreach (DataRow r in dtGrid.Rows)
                                {
                                    cmdSave.Parameters.Clear();
                                    cmdSave.Parameters.AddWithValue("@DonVi", r["DonVi"] ?? DBNull.Value);
                                    cmdSave.Parameters.AddWithValue("@TongQS", r["TongQS"] ?? 0);
                                    cmdSave.Parameters.AddWithValue("@L1", r["Loai_1"] ?? 0);
                                    cmdSave.Parameters.AddWithValue("@L2", r["Loai_2"] ?? 0);
                                    cmdSave.Parameters.AddWithValue("@L3", r["Loai_3"] ?? 0);
                                    cmdSave.Parameters.AddWithValue("@L4", r["Loai_4"] ?? 0);
                                    cmdSave.Parameters.AddWithValue("@KPL", r["Khong_PL"] ?? 0);
                                    await cmdSave.ExecuteNonQueryAsync();
                                }
                            }
                            await tran.CommitAsync();
                        }
                        catch { await tran.RollbackAsync(); throw; }
                    }

                    // 5. HIỂN THỊ GIAO DIỆN (CHỖ NÀY ĐÃ FIX LỖI DẤU X ĐỎ)
                    kryptonDataGridView1.SuspendLayout();
                    try
                    {
                        if (kryptonDataGridView1.DataSource is DataTable oldDt)
                        {
                            kryptonDataGridView1.DataSource = null;
                            oldDt.Dispose();
                        }
                        kryptonDataGridView1.DataSource = dtGrid;
                        kryptonDataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        kryptonDataGridView1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        kryptonDataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

                        var columnHeaderMap = new Dictionary<string, string>
                {
                    { "DonVi", "Đơn vị" }, { "TongQS", "Quân số" }, { "Loai_1", "Loại 1" }, { "Loai_2", "Loại 2" },
                    { "Loai_3", "Loại 3" }, { "Loai_4", "Loại 4" }, { "Khong_PL", "Không PL" },
                    { "PhanTramLoai_1", "% Loại 1" }, { "PhanTramLoai_2", "% Loại 2" },
                    { "PhanTramLoai_3", "% Loại 3" }, { "PhanTramLoai_4", "% Loại 4" }, { "PhanTramKhong_PL", "% Không PL" }
                };

                        foreach (DataGridViewColumn col in kryptonDataGridView1.Columns)
                        {
                            col.SortMode = DataGridViewColumnSortMode.NotSortable;
                            if (columnHeaderMap.TryGetValue(col.Name, out string headerText)) col.HeaderText = headerText;

                            // Logic ẩn cột rỗng
                            bool hasValue = false;
                            foreach (DataGridViewRow row in kryptonDataGridView1.Rows)
                            {
                                string strVal = row.Cells[col.Name].Value?.ToString()?.Trim() ?? "";
                                if (!string.IsNullOrWhiteSpace(strVal) && strVal != "0" && strVal != "0%") { hasValue = true; break; }
                            }
                            col.Visible = hasValue;
                        }

                        kryptonDataGridView1.RowPostPaint -= KryptonDataGridView1_RowPostPaint;
                        kryptonDataGridView1.RowPostPaint += KryptonDataGridView1_RowPostPaint;

                        // 🌟 TÍCH HỢP HÀM RÀ SOÁT TỶ LỆ Ở ĐÂY
                        kryptonDataGridView1.CellFormatting -= KhaoSatTyLePhanTramCacDonVi;
                        kryptonDataGridView1.CellFormatting += KhaoSatTyLePhanTramCacDonVi;

                        // 👇 DÁN THÊM 2 DÒNG NÀY VÀO ĐÂY:
                        kryptonDataGridView1.CellClick -= KryptonDataGridView1_CellClick_HienThiThongBao;
                        kryptonDataGridView1.CellClick += KryptonDataGridView1_CellClick_HienThiThongBao;

                        // 👉 BƯỚC 1: Gọi hàm cấu hình kích thước và ép chữ lưới thành chữ Mỏng (Regular)
                        AutoFitFont_DataGridView(kryptonDataGridView1);
                        UocLuongDoRongCacCot(kryptonDataGridView1);

                        // 👉 BƯỚC 2: Gọi lệnh in đậm Header TẠI ĐÂY (Sau khi mọi thứ đã Regular)
                        // Bằng cách này, chỉ duy nhất khu vực Header (Tiêu đề cột) bị ghi đè lại thành in đậm
                        if (_cachedGridFontBold != null)
                        {
                            kryptonDataGridView1.ColumnHeadersDefaultCellStyle.Font = _cachedGridFontBold;
                        }
                    }
                    finally
                    {
                        kryptonDataGridView1.ResumeLayout();
                    }
                }
            }
            catch (SqliteException sqlEx) { MessageBox.Show("Lỗi cơ sở dữ liệu SQLite:\n" + sqlEx.Message, "SQLite", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        private void KhaoSatTyLePhanTramCacDonVi(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (sender is not DataGridView dgv || e.RowIndex < 0 || e.ColumnIndex < 0) return;

            if (!dgv.Columns.Contains("DonVi") ||
                !dgv.Columns.Contains("TongQS") ||
                !dgv.Columns.Contains("Loai_1") ||
                !dgv.Columns.Contains("Loai_2") ||
                !dgv.Columns.Contains("Loai_3") ||
                !dgv.Columns.Contains("Loai_4") ||
                !dgv.Columns.Contains("Khong_PL"))
            {
                return;
            }

            string donViName = dgv.Rows[e.RowIndex].Cells["DonVi"].Value?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(donViName)) return;

            if (donViName.Equals("Tổng cộng", StringComparison.OrdinalIgnoreCase) || _dsDonViBoQuaCanhBao.Contains(donViName.Trim()))
            {
                if (dgv.Rows[e.RowIndex].Selected)
                {
                    e.CellStyle.ForeColor = e.CellStyle.SelectionForeColor;
                }
                else
                {
                    e.CellStyle.ForeColor = Color.FromArgb(0, 128, 0);
                }
                e.CellStyle.Font = new Font(dgv.Font, FontStyle.Regular);
                // Dùng lại Font gốc của DataGridView, tuyệt đối không tạo Font mới
                e.CellStyle.Font = dgv.Font;
                return;
            }

            try
            {
                int tongQS = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["TongQS"].Value ?? 0);
                int l1 = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["Loai_1"].Value ?? 0);
                int l2 = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["Loai_2"].Value ?? 0);
                int l3 = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["Loai_3"].Value ?? 0);
                int l4 = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["Loai_4"].Value ?? 0);
                int kpl = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["Khong_PL"].Value ?? 0);

                if (tongQS <= 0) return;

                int kqCanDat_L1 = 0;
                int kqCanDat_L2_Thuan = 0;
                int kqCanDat_L3 = 0;

                // ====================================================================
                // NHÁNH 1: LOGIC BAN CHỈ HUY (BCH)
                // ====================================================================
                if (donViName.Equals("BCH", StringComparison.OrdinalIgnoreCase))
                {
                    string deNghiTTe = com_DeNghi.Text.Trim().ToUpperInvariant();
                    if (string.IsNullOrWhiteSpace(deNghiTTe) || deNghiTTe.Contains("KHÔNG PL")) return;

                    if (_cachedTongBCH == -1)
                    {
                        int countBCH = 0;
                        try
                        {
                            using (var cn = new SqliteConnection($"Data Source={_csdl2Path}"))
                            {
                                cn.Open();
                                using var cmd = new SqliteCommand("SELECT DonVi FROM DanhSach", cn);
                                using var rd = cmd.ExecuteReader();
                                while (rd.Read())
                                {
                                    string donViDB = SafeDecrypt(rd["DonVi"])?.Trim().ToUpperInvariant() ?? "";
                                    if (donViDB == "BCH") countBCH++;
                                }
                            }
                        }
                        catch { }
                        _cachedTongBCH = countBCH;
                    }

                    if (_cachedTongBCH == 0) return;

                    if (deNghiTTe == "LOẠI 1")
                    {
                        kqCanDat_L1 = (int)Math.Floor(_cachedTongBCH * 0.75);
                        kqCanDat_L2_Thuan = _cachedTongBCH - kqCanDat_L1;
                        kqCanDat_L3 = 0;
                    }
                    else if (deNghiTTe == "LOẠI 2")
                    {
                        kqCanDat_L1 = (int)Math.Floor(_cachedTongBCH * 0.50);
                        kqCanDat_L2_Thuan = _cachedTongBCH - kqCanDat_L1;
                        kqCanDat_L3 = 0;
                    }
                    else if (deNghiTTe == "LOẠI 3" || deNghiTTe == "LOẠI 4")
                    {
                        kqCanDat_L1 = 0;
                        kqCanDat_L2_Thuan = (int)Math.Floor(_cachedTongBCH * 0.50);
                        kqCanDat_L3 = _cachedTongBCH - kqCanDat_L2_Thuan;
                    }
                }
                // ====================================================================
                // NHÁNH 2: LOGIC CHIẾN SĨ (TÍNH TOÁN BÙ TRỪ KHỚP 100%)
                // ====================================================================
                else
                {
                    int duDieuKien = tongQS - l4 - kpl;
                    if (duDieuKien < 0) duDieuKien = 0;

                    double rateL1 = phanTramLoai1 / 100.0;
                    double rateL2 = phanTramLoai2 / 100.0;

                    // 1. Ép trần Tổng Loại 2
                    int kqCanDat_L2_Tong = (int)Math.Floor(duDieuKien * rateL2);

                    // 2. Loại 3 BẮT BUỘC gánh phần dư để tổng 100%
                    kqCanDat_L3 = duDieuKien - kqCanDat_L2_Tong;

                    // 3. Ép trần Loại 1 từ Quỹ Loại 2
                    kqCanDat_L1 = (int)Math.Floor(kqCanDat_L2_Tong * rateL1);
                    if (kqCanDat_L1 > kqCanDat_L2_Tong) kqCanDat_L1 = kqCanDat_L2_Tong;

                    kqCanDat_L2_Thuan = kqCanDat_L2_Tong - kqCanDat_L1;
                }

                bool isLoai1HopLe = l1 >= kqCanDat_L1;
                bool isLoai2HopLe = l2 >= kqCanDat_L2_Thuan;
                bool isLoai3HopLe = l3 >= kqCanDat_L3;
                bool isDonViDatChuan = isLoai1HopLe && isLoai2HopLe && isLoai3HopLe;

                //// --- BẮT ĐẦU ĐOẠN CẦN THAY THẾ (Khoảng dòng 762) ---
                //Color textColor = isDonViDatChuan ? Color.FromArgb(0, 128, 0) : Color.FromArgb(255, 0, 0);

                //if (dgv.Rows[e.RowIndex].Selected)
                //{
                //    textColor = e.CellStyle.SelectionForeColor;
                //}

                //e.CellStyle.ForeColor = textColor;

                //// 🔥 SỬA LỖI IN ĐẬM: Trực tiếp lấy Font gốc của Grid làm chuẩn để phân nhánh
                //if (!isDonViDatChuan)
                //{
                //    // Nếu Chưa đạt (Màu Đỏ) -> Cảnh báo bằng cách in đậm
                //    e.CellStyle.Font = new Font(dgv.Font, FontStyle.Bold);
                //}
                //else
                //{
                //    // Nếu Đạt (Màu Xanh) -> Mặc định lấy chữ mỏng (Regular) từ cấu hình lưới
                //    e.CellStyle.Font = dgv.Font;
                //}
                // =====================================================================
                // TÔ MÀU + FONT (KHÔNG TẠO FONT MỚI - TRÁNH RÒ RỈ GDI/RAM)
                // =====================================================================

                Color textColor = isDonViDatChuan
                    ? Color.FromArgb(0, 128, 0)
                    : Color.FromArgb(255, 0, 0);

                // Khi đang chọn dòng thì giữ nguyên màu Selection của Windows
                if (dgv.Rows[e.RowIndex].Selected)
                {
                    e.CellStyle.ForeColor = e.CellStyle.SelectionForeColor;
                }
                else
                {
                    e.CellStyle.ForeColor = textColor;
                }

                // 👇 FIX LỖI TRÀN GDI/RAM
                // Không được new Font() trong CellFormatting.
                // Luôn dùng lại Font đã cache.

                if (!isDonViDatChuan)
                {
                    if (_cachedGridFontBold != null)
                    {
                        e.CellStyle.Font = _cachedGridFontBold;
                    }
                    else
                    {
                        // Phòng trường hợp cache chưa khởi tạo
                        e.CellStyle.Font = dgv.Font;
                    }
                }
                else
                {
                    e.CellStyle.Font = dgv.Font;
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Lỗi khảo sát tỷ lệ Grid] " + ex.Message);
            }
        }
        private void KryptonDataGridView1_CellClick_HienThiThongBao(object? sender, DataGridViewCellEventArgs e)
        {
            if (sender is not DataGridView dgv || e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (!dgv.Columns.Contains("DonVi")) return;

            string donViName = dgv.Rows[e.RowIndex].Cells["DonVi"].Value?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(donViName) || donViName.Equals("Tổng cộng", StringComparison.OrdinalIgnoreCase)) return;

            try
            {
                int tongQS = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["TongQS"].Value ?? 0);
                int l1 = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["Loai_1"].Value ?? 0);
                int l2 = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["Loai_2"].Value ?? 0);
                int l3 = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["Loai_3"].Value ?? 0);
                int l4 = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["Loai_4"].Value ?? 0);
                int kpl = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["Khong_PL"].Value ?? 0);

                if (tongQS <= 0) return;

                int kqCanDat_L1 = 0;
                int kqCanDat_L2_Thuan = 0;
                int kqCanDat_L3 = 0;
                int duDieuKien = tongQS - l4 - kpl;

                if (donViName.Equals("BCH", StringComparison.OrdinalIgnoreCase))
                {
                    string deNghiTTe = com_DeNghi.Text.Trim().ToUpperInvariant();
                    if (string.IsNullOrWhiteSpace(deNghiTTe) || deNghiTTe.Contains("KHÔNG PL"))
                    {
                        MessageBox.Show($"Đơn vị: {donViName}\nTổng quân số: {tongQS}\n\nChưa có đề nghị phân loại tập thể nên không xét tỷ lệ.",
                                             "Thông tin", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    int bchQS = _cachedTongBCH > 0 ? _cachedTongBCH : tongQS;

                    if (deNghiTTe == "LOẠI 1") { kqCanDat_L1 = (int)Math.Floor(bchQS * 0.75); kqCanDat_L2_Thuan = bchQS - kqCanDat_L1; kqCanDat_L3 = 0; }
                    else if (deNghiTTe == "LOẠI 2") { kqCanDat_L1 = (int)Math.Floor(bchQS * 0.50); kqCanDat_L2_Thuan = bchQS - kqCanDat_L1; kqCanDat_L3 = 0; }
                    else if (deNghiTTe == "LOẠI 3" || deNghiTTe == "LOẠI 4") { kqCanDat_L1 = 0; kqCanDat_L2_Thuan = (int)Math.Floor(bchQS * 0.50); kqCanDat_L3 = bchQS - kqCanDat_L2_Thuan; }
                }
                else
                {
                    if (duDieuKien < 0) duDieuKien = 0;
                    double rateL1 = phanTramLoai1 / 100.0;
                    double rateL2 = phanTramLoai2 / 100.0;

                    int kqCanDat_L2_Tong = (int)Math.Floor(duDieuKien * rateL2);
                    kqCanDat_L3 = duDieuKien - kqCanDat_L2_Tong;

                    kqCanDat_L1 = (int)Math.Floor(kqCanDat_L2_Tong * rateL1);
                    if (kqCanDat_L1 > kqCanDat_L2_Tong) kqCanDat_L1 = kqCanDat_L2_Tong;

                    kqCanDat_L2_Thuan = kqCanDat_L2_Tong - kqCanDat_L1;
                }

                bool isLoai1HopLe = l1 >= kqCanDat_L1;
                bool isLoai2HopLe = l2 >= kqCanDat_L2_Thuan;
                bool isLoai3HopLe = l3 >= kqCanDat_L3;
                bool isDonViDatChuan = isLoai1HopLe && isLoai2HopLe && isLoai3HopLe;

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Đơn vị: {donViName.ToUpper()}");
                sb.AppendLine(new string('-', 40));
                sb.AppendLine($"  Tổng quân số : {tongQS} đồng chí");
                sb.AppendLine($"  Loại 1       : {l1} đồng chí");
                sb.AppendLine($"  Loại 2       : {l2} đồng chí");
                sb.AppendLine($"  Loại 3       : {l3} đồng chí");
                sb.AppendLine($"  Loại 4       : {l4} đồng chí");
                sb.AppendLine($"  Không PL     : {kpl} đồng chí");
                sb.AppendLine(new string('-', 40));

                if (!donViName.Equals("BCH", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"  Quân số đủ điều kiện xét: {duDieuKien} đồng chí");
                    sb.AppendLine();
                }

                sb.AppendLine(" Đối chiếu chỉ tiêu quy định:");
                sb.AppendLine($"  Loại 1 (Cần {kqCanDat_L1}):\t{(isLoai1HopLe ? "Đạt" : "Chưa đạt")}");
                sb.AppendLine($"  Loại 2 (Cần {kqCanDat_L2_Thuan}):\t{(isLoai2HopLe ? "Đạt" : "Chưa đạt")}");
                sb.AppendLine($"  Loại 3 (Cần {kqCanDat_L3}):\t{(isLoai3HopLe ? "Đạt" : "Chưa đạt ")}");
                sb.AppendLine(new string('-', 40));

                sb.AppendLine($" Kết luận: {(isDonViDatChuan ? "Đạt tỷ lệ quy định" : "Chưa đạt tỷ lệ quy định")}");

                if (!isDonViDatChuan || l1 != kqCanDat_L1 || l2 != kqCanDat_L2_Thuan || l3 != kqCanDat_L3 || l4 > 0 || kpl > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Hướng dẫn điều chỉnh để đạt chuẩn:");

                    if (l1 != kqCanDat_L1)
                        sb.AppendLine($"  - Loại 1 đang {(l1 > kqCanDat_L1 ? "thừa" : "thiếu")} {Math.Abs(l1 - kqCanDat_L1)} đồng chí.");
                    if (l2 != kqCanDat_L2_Thuan)
                        sb.AppendLine($"  - Loại 2 đang {(l2 > kqCanDat_L2_Thuan ? "thừa" : "thiếu")} {Math.Abs(l2 - kqCanDat_L2_Thuan)} đồng chí.");
                    if (l3 != kqCanDat_L3)
                        sb.AppendLine($"  - Loại 3 đang {(l3 > kqCanDat_L3 ? "thừa" : "thiếu")} {Math.Abs(l3 - kqCanDat_L3)} đồng chí.");

                    if (l4 > 0)
                        sb.AppendLine($"  - Chú ý: Đang có {l4} đồng chí xếp Loại 4.");
                    if (kpl > 0)
                        sb.AppendLine($"  - Chú ý: Đang có {kpl} đồng chí Không phân loại.");
                }

                MessageBox.Show(
                    sb.ToString(),
                    "Phân tích chỉ tiêu thi đua",
                    MessageBoxButtons.OK,
                    isDonViDatChuan ? MessageBoxIcon.Information : MessageBoxIcon.Warning
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi hiển thị thông báo Click Bảng 1: " + ex.Message);
            }
        }
        private async Task Bang2_Async()
        {
            if (!_allowLoadBang2)
            {
                _allowLoadBang2 = true;
                return;
            }
            if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path))
                return;
            string csdl2Path = _csdl2Path;

            try
            {
                using var connection = new SqliteConnection($"Data Source={csdl2Path}");
                await connection.OpenAsync();
                string kyHieuTrungDoan = "E29";
                // ⭐ NHẬN DIỆN PHIÊN BẢN ĐỂ CHỌN ĐÚNG BẢNG TỶ LỆ KHI ĐỌC/GHI
                bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                string tableQuyDinhTyLe = laTanBinh ? "QuyDinhTyLe_TanBinh" : "QuyDinhTyLe"; // Bảng cấu hình % tương ứng
                try
                {
                    using var cmdKyHieu = new SqliteCommand("SELECT KyHieu_TrungDoan FROM KyHieu_DonVi WHERE ID = 1 LIMIT 1", connection);
                    var result = await cmdKyHieu.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        string giaiMa = GiaiMaAnToan(result.ToString());
                        if (!string.IsNullOrWhiteSpace(giaiMa))
                            kyHieuTrungDoan = giaiMa.Trim();
                    }
                }
                catch (Exception ex) { Debug.WriteLine("Lỗi đọc KyHieu: " + ex.Message); }

                DataTable dtQS = new DataTable();
                using (var cmd = new SqliteCommand("SELECT * FROM QuanSoThiDuaD2 ORDER BY ID", connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    dtQS.Load(reader);
                }

                if (dtQS.Rows.Count == 0)
                {
                    kryptonDataGridView2.DataSource = null;
                    _allowLoadBang2 = false;
                    return;
                }

                _allowLoadBang2 = true;
                DataRow totalRow = dtQS.AsEnumerable().FirstOrDefault(r => r["DonVi"]?.ToString() == "Tổng cộng") ?? dtQS.Rows[dtQS.Rows.Count - 1];

                double loai1Rate = phanTramLoai1 <= 0 ? 0 : phanTramLoai1;
                double loai2Rate = phanTramLoai2 <= 0 ? 0 : phanTramLoai2;

                int tongQuanSo = Convert.ToInt32(totalRow["TongQS"]);
                int loai1 = Convert.ToInt32(totalRow["Loai_1"]);
                int loai2 = Convert.ToInt32(totalRow["Loai_2"]);
                int loai3 = Convert.ToInt32(totalRow["Loai_3"]);
                int loai4 = Convert.ToInt32(totalRow["Loai_4"]);
                int khongPL = Convert.ToInt32(totalRow["Khong_PL"]);

                int duDieuKien = tongQuanSo - loai4 - khongPL;
                if (duDieuKien < 0) duDieuKien = 0;

                int kqCanDat_L2_Tong = (int)Math.Floor(duDieuKien * loai2Rate / 100.0);
                int kqCanDat_L3 = duDieuKien - kqCanDat_L2_Tong;
                int kqCanDat_L1 = (int)Math.Floor(kqCanDat_L2_Tong * loai1Rate / 100.0);
                using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync();

                // ⭐ BẢO VỆ CSDL: Tự động khởi tạo bảng TyLe nếu chưa có cấu trúc
                using (var cmdInit = connection.CreateCommand())
                {
                    cmdInit.Transaction = transaction;
                    cmdInit.CommandText = @"
        CREATE TABLE IF NOT EXISTS [TyLe] (
            ID INTEGER NOT NULL PRIMARY KEY,
            [Thong tin] TEXT,
            [KQ Hien tai] INTEGER,
            [KQ Can dat] INTEGER,
            [KQ Gui E29] TEXT,
            [Ket luan] TEXT
        );";

                    await cmdInit.ExecuteNonQueryAsync();
                }
                using var cmdUpdate = connection.CreateCommand();
                cmdUpdate.Transaction = transaction;
                async Task UpdateTyLeRowAsync(int id, int kqHienTai, int kqCanDat, string kqGuiE29, string ketLuan)
                {
                    cmdUpdate.Parameters.Clear();
                    cmdUpdate.CommandText = @"UPDATE TyLe SET ""KQ Hien tai""=@kqHienTai, ""KQ Can dat""=@kqCanDat, ""KQ Gui E29""=@kqGuiE29, ""Ket luan""=@ketLuan WHERE ID=@id;";
                    cmdUpdate.Parameters.AddWithValue("@kqHienTai", kqHienTai);
                    cmdUpdate.Parameters.AddWithValue("@kqCanDat", kqCanDat);
                    cmdUpdate.Parameters.AddWithValue("@kqGuiE29", kqGuiE29);
                    cmdUpdate.Parameters.AddWithValue("@ketLuan", ketLuan);
                    cmdUpdate.Parameters.AddWithValue("@id", id);
                    await cmdUpdate.ExecuteNonQueryAsync();
                }

                string fmt(double v) => v.ToString("0.00");
                string guiE29_Tong = $"{tongQuanSo}";
                string guiE29_Loai1 = kqCanDat_L2_Tong > 0 ? $"{kqCanDat_L1}/{kqCanDat_L2_Tong} = {fmt(kqCanDat_L1 * 100.0 / kqCanDat_L2_Tong)}%" : "";
                string guiE29_Loai2 = duDieuKien > 0 ? $"{kqCanDat_L2_Tong}/{duDieuKien} = {fmt(kqCanDat_L2_Tong * 100.0 / duDieuKien)}%" : "";
                string guiE29_Loai3 = duDieuKien > 0 ? $"{kqCanDat_L3}/{duDieuKien} = {fmt(kqCanDat_L3 * 100.0 / duDieuKien)}%" : "";
                int[] kqHienTaiArr = { tongQuanSo, loai1, loai2, loai3, loai4, khongPL };
                int[] kqGuiE29Arr = { tongQuanSo, kqCanDat_L1, kqCanDat_L2_Tong, kqCanDat_L3, 0, 0 };
                string[] guiE29Arr = { guiE29_Tong, guiE29_Loai1, guiE29_Loai2, guiE29_Loai3, "", "" };
                string KetLuanRow(int id, int kqHienTai, int kqCanDat, bool tinh)
                {
                    if (!tinh && (id < 5 || id > 6)) return "";

                    switch (id)
                    {
                        case 1:
                            bool loai1Ok = loai1 >= kqCanDat_L1;
                            bool loai2Ok = loai2 >= (kqCanDat_L2_Tong - kqCanDat_L1);
                            bool loai3Ok = loai3 >= kqCanDat_L3;
                            return (loai1Ok && loai2Ok && loai3Ok) ? "Đạt tỷ lệ quy định" : "Chưa đạt tỷ lệ quy định";
                        case 2:
                            if (kqHienTai == kqCanDat_L1) return "";
                            return kqHienTai > kqCanDat_L1 ? $"Đang thừa {kqHienTai - kqCanDat_L1} đồng chí" : $"Đang thiếu {kqCanDat_L1 - kqHienTai} đồng chí";
                        case 3:
                            int kqCanDatLoai2Thuan = kqCanDat_L2_Tong - kqCanDat_L1;
                            if (kqHienTai == kqCanDatLoai2Thuan) return "";
                            return kqHienTai > kqCanDatLoai2Thuan ? $"Đang thừa {kqHienTai - kqCanDatLoai2Thuan} đồng chí" : $"Đang thiếu {kqCanDatLoai2Thuan - kqHienTai} đồng chí";
                        case 4:
                            if (kqHienTai == kqCanDat_L3) return "";
                            return kqHienTai > kqCanDat_L3 ? $"Đang thừa {kqHienTai - kqCanDat_L3} đồng chí" : $"Đang thiếu {kqCanDat_L3 - kqHienTai} đồng chí";
                        case 5:
                        case 6:
                            if (kqHienTai == 0) return "";
                            return $"Phát sinh {kqHienTai} đồng chí";
                        default:
                            return "";
                    }
                }

                for (int i = 0; i < 6; i++)
                {
                    bool tinh = i < 4;
                    int kqHienTaiSoSanh = (i == 2) ? loai2 : kqHienTaiArr[i];
                    string kqGuiE29 = guiE29Arr[i];

                    if (i == 4 || i == 5)
                    {
                        if (kqHienTaiArr[i] > 0 && tongQuanSo > 0)
                        {
                            kqGuiE29 = $"{kqHienTaiArr[i]:D2}/{duDieuKien} = {fmt((double)kqHienTaiArr[i] * 100 / tongQuanSo)}%";
                        }
                        else kqGuiE29 = "";
                    }

                    await UpdateTyLeRowAsync(i + 1, kqHienTaiArr[i], kqGuiE29Arr[i], kqGuiE29, KetLuanRow(i + 1, kqHienTaiSoSanh, kqGuiE29Arr[i], tinh));
                }

                await transaction.CommitAsync();
                DataTable dt = new DataTable();
                using (var cmdLoadTyLe = new SqliteCommand("SELECT * FROM TyLe", connection))
                using (var reader = await cmdLoadTyLe.ExecuteReaderAsync())
                {
                    dt.Load(reader);
                }
                kryptonDataGridView2.SuspendLayout();
                kryptonDataGridView2.DataSource = dt;
                AutoFitFont_DataGridView(kryptonDataGridView2);
                if (kryptonDataGridView2.Columns.Contains("ID")) kryptonDataGridView2.Columns["ID"].Visible = false;
                kryptonDataGridView2.RowHeadersVisible = true;
                kryptonDataGridView2.RowHeadersWidth = 60;
                kryptonDataGridView2.AllowUserToAddRows = false;
                kryptonDataGridView2.AllowUserToDeleteRows = false;
                kryptonDataGridView2.ReadOnly = true;
                kryptonDataGridView2.SelectionMode = DataGridViewSelectionMode.CellSelect;
                kryptonDataGridView2.MultiSelect = false;
                kryptonDataGridView2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                void SetColumn(string colName, string headerText, float fillWeight)
                {
                    if (kryptonDataGridView2.Columns.Contains(colName))
                    {
                        var col = kryptonDataGridView2.Columns[colName];
                        col.HeaderText = headerText;
                        col.FillWeight = fillWeight;
                        col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }
                }
                SetColumn("Thong tin", "Thông tin", 15);
                SetColumn("KQ Hien tai", "KQ Hiện tại", 15);
                SetColumn("KQ Can dat", "KQ Cần đạt", 17);
                SetColumn("KQ Gui E29", $"KQ Gửi {kyHieuTrungDoan}", 17);
                SetColumn("Ket luan", "Kết luận", 31);

                float baseSize = kryptonDataGridView2.Font.Size;
                if (_cachedGridFont != null) baseSize = _cachedGridFont.Size;
                kryptonDataGridView2.EnableHeadersVisualStyles = false;
                if (_cachedGrid2HeaderFont == null || Math.Abs(_cachedGrid2HeaderFont.Size - baseSize) > 0.1f)
                {
                    _cachedGrid2HeaderFont?.Dispose();
                    _cachedGrid2HeaderFont = new Font("Segoe UI", baseSize, FontStyle.Bold);
                }
                kryptonDataGridView2.ColumnHeadersDefaultCellStyle.Font = _cachedGrid2HeaderFont;
                foreach (DataGridViewColumn col in kryptonDataGridView2.Columns) col.SortMode = DataGridViewColumnSortMode.NotSortable;
                kryptonDataGridView2.ResumeLayout();
                kryptonDataGridView2.RowPostPaint -= KryptonDataGridView2_RowPostPaint;
                kryptonDataGridView2.RowPostPaint += KryptonDataGridView2_RowPostPaint;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Bang2 ERROR: " + ex.Message);
            }
        }      
        ///   Nguyên lý Khống chế trần tuyệt đối(Chống vượt % dưới mọi hình thức)Giả sử tổng quân số đủ điều kiện là $N$ (biến số động) và tỷ lệ Loại 2 lấy từ CSDL là $R$ (ví dụ: $79\% = 0.79$).Số lượng Loại 2 tối đa theo thuật toán của chúng ta là:$$Max = \lfloor N \times R \rfloor$$(Hàm Math.Floor chính là phép toán lấy phần nguyên lớn nhất không vượt quá giá trị thực).Bây giờ ta test ngược lại tỷ lệ phần trăm thực tế đạt được:$$\% \text{ Thực tế } = \frac{\lfloor N \times R \rfloor }{N
        ///    }$$Theo tính chất toán học, $\lfloor X \rfloor \le X$. Do đó:$$\frac{\lfloor N \times R \rfloor }{N
        ///} \le \frac{N \times R}{ N} = R$$👉 Kết luận 1: Tỷ lệ phần trăm tính ra luôn luôn nhỏ hơn hoặc bằng tỷ lệ quy định $R$, bất chấp quân số $N$ là số chẵn hay số lẻ. Quy định "tối đa chỉ bằng hoặc thấp hơn mức % quy định" được thỏa mãn tuyệt đối.Ví dụ Test Edge Case (Trường hợp dị biệt): Đơn vị siêu nhỏ có $N = 6$ người. Tỷ lệ quy định $R = 79\%$.Máy tính: $6 \times 0.79 = 4.74$.Thuật toán Math.Floor(4.74): Lấy $4$ người.Test tỷ lệ nộp báo cáo: $4 / 6 = 66.67\% \le 79\%$. (Hợp lệ hoàn toàn, nếu lấy 5 người sẽ ra $83.33\% \rightarrow$ vi phạm quy định).2. Nguyên lý Bảo toàn quân số (Không bao giờ rớt mất người)Trong quân đội hay công an, quân số báo cáo tổng phải khớp đến từng người. Quá trình làm tròn xuống (cho Loại 1 và Loại 2) chắc chắn sẽ sinh ra những "mảnh vỡ" số thập phân (như số $0.74$ ở ví dụ trên bị vứt bỏ). Nếu không cẩn thận, cộng lại sẽ bị mất tích người.Thuật toán của chúng ta xử lý thế này:Quỹ Loại 2 (gồm L1 + L2 thuần) = Math.Floor(N * 79%).Quỹ Loại 3 = N - Quỹ Loại 2.Bởi vì: $\text{Quỹ Loại 2} + \text{Quỹ Loại 3} = \text{Quỹ Loại 2} +(N - \text{Quỹ Loại 2}) = N$.👉 Kết luận 2: Tổng số người đánh giá luôn luôn khớp đúng $100\%$ với số $N$ đầu vào. Bất kể Math.Floor đã chém bỏ bao nhiêu phần thập phân của Loại 2, phần bị chém đó đều tự động biến thành $1$ con người hoàn chỉnh đẩy sang Loại 3. Không có ai bị bỏ sót.3. Giải đáp thắc mắc: Tại sao báo lỗi "Loại 3 đang thiếu" là hợp lý 100%?Bây giờ ta quay lại kịch bản báo cáo bị lỗi của đơn vị.Giả sử hệ thống đang cảnh báo:Loại 1: Thừa 1 đồng chí (Do xét quá tay 1 người).Loại 2: Vừa đủ số lượng.Loại 3: Thiếu 1 đồng chí.Anh băn khoăn rằng: "Ông Loại 1 đang bị dư, giáng cấp ông đó xuống thì ông đó phải vào Loại 2 chứ? Sao lại nhảy xuống Loại 3 để lấp vào chỗ thiếu?"Câu trả lời nằm ở định nghĩa Trần Quỹ Loại 2.Quy định nêu rõ: "Loại 1 được lấy trong tổng số Loại 2".Nghĩa là Loại 1 và Loại 2 đang dùng chung một cái rổ.Cái rổ này có sức chứa tối đa bị khóa cứng bởi lệnh Math.Floor(N * 79%).Nếu đơn vị đã chấm Loại 2 thuần "đầy mép" cái rổ rồi, thì 1 ông Loại 1 dư thừa kia khi giáng cấp xuống sẽ không thể chui vào cái rổ Loại 2 được nữa (vì nếu chui vào, cái rổ phình to ra, chia % sẽ bị vượt mốc $79\%$).Người này trượt khỏi cái rổ L1+L2, lực hấp dẫn tự động kéo thẳng ông ấy xuống cái rổ Loại 3.
        private void KryptonDataGridView2_RowPostPaint(
            object? sender,
            DataGridViewRowPostPaintEventArgs e)
        {
            try
            {
                if (sender is not DataGridView dgv)
                    return;

                string rowNumber =
                    (e.RowIndex + 1).ToString();

                Rectangle rect =
                    new Rectangle(
                        e.RowBounds.Left,
                        e.RowBounds.Top,
                        dgv.RowHeadersWidth,
                        e.RowBounds.Height);

                bool isSelected =
                    dgv.Rows[e.RowIndex].Selected;

                Color textColor =
                    isSelected
                    ? dgv.RowHeadersDefaultCellStyle.SelectionForeColor
                    : dgv.RowHeadersDefaultCellStyle.ForeColor;

                TextRenderer.DrawText(
                    e.Graphics,
                    rowNumber,
                    dgv.Font,
                    rect,
                    textColor,
                    TextFormatFlags.HorizontalCenter |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPadding);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"RowPostPaint Error: {ex.Message}");
            }
        }
        private string SafeDecrypt(object? val)
        {
            if (val == null || val == DBNull.Value) return "";
            string s = val.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(s)) return "";

            if (_uiDecryptCache.TryGetValue(s, out string cachedResult))
                return cachedResult;

            try
            {
                string dec = BaoMatAES.GiaiMa(s);
                string finalVal = string.IsNullOrEmpty(dec) ? s.Trim() : dec;

                // BẢO VỆ CPU: Dùng biến đếm độc lập thay vì _uiDecryptCache.Count
                if (_uiDecryptCache.Count > 3000)
                {
                    var firstKey = _uiDecryptCache.Keys.FirstOrDefault();
                    if (firstKey != null)
                    {
                        _uiDecryptCache.TryRemove(firstKey, out _);
                    }
                }

                if (_uiDecryptCache.TryAdd(s, finalVal))
                    Interlocked.Increment(ref _cacheCounter);

                return finalVal;
            }
            catch
            {
                _uiDecryptCache.TryAdd(s, s.Trim());
                return s.Trim();
            }
        }
        private void AutoFitFont_DataGridView(DataGridView dgv)
        {
            if (dgv == null || dgv.Rows.Count == 0) return;

            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;

            int visibleRows = Math.Max(1, dgv.DisplayedRowCount(false));
            int gridHeight = dgv.ClientSize.Height;

            float estimatedRowHeight = (float)gridHeight / visibleRows;
            float fontSize = Math.Clamp(estimatedRowHeight * 0.40f, 8f, 10f);

            //if (_cachedGridFont == null || Math.Abs(_cachedGridFont.Size - fontSize) > 0.1f)
            //{
            //    _cachedGridFont?.Dispose();

            //    // 🟢 Cố định ép font chữ là FontStyle.Regular (chữ thường)
            //    _cachedGridFont = new Font("Segoe UI", fontSize, FontStyle.Regular);
            //}
            if (_cachedGridFont == null || Math.Abs(_cachedGridFont.Size - fontSize) > 0.1f)
            {
                _cachedGridFont?.Dispose();
                _cachedGridFontBold?.Dispose(); // 👈 Dọn dẹp font in đậm cũ
                // 🟢 Cố định ép font chữ là FontStyle.Regular (chữ thường)
                _cachedGridFont = new Font("Segoe UI", fontSize, FontStyle.Regular);
                // 🟢 TẠO SẴN 1 FONT IN ĐẬM BỎ VÀO CACHE ĐỂ DÙNG CHUNG (Cực kỳ tối ưu RAM)
                _cachedGridFontBold = new Font("Segoe UI", fontSize, FontStyle.Bold);
            }
            // 🔥 SỬA CHỮA QUAN TRỌNG TẠI ĐÂY:
            // Gán tường minh Font chữ thường cho Toàn bộ Bảng, và cho Cột (Cells)
            dgv.Font = _cachedGridFont;
            dgv.DefaultCellStyle.Font = _cachedGridFont;
            dgv.RowsDefaultCellStyle.Font = _cachedGridFont;

            dgv.RowTemplate.Height = TextRenderer.MeasureText("A", _cachedGridFont).Height + 6;
        }
        public async Task ReloadDuLieuAsync()
        {
            if (!await _reloadLock.WaitAsync(0))
                return;

            this.SuspendLayout();
            try
            {
                toolStripProgressBar1.Visible = true;

                // GỠ SỰ KIỆN GÂY NHIỄU
                Check_MoThuMuc.CheckedChanged -= Check_MoThuMuc_CheckedChanged;
                comboBox1_ChonLoaiDeXuat.SelectedIndexChanged -= comboBox1_ChonLoaiDeXuat_SelectedIndexChanged;
                checkBox1_TuDongChonNgayThang.CheckedChanged -= checkBox1_TuDongChonNgayThang_CheckedChanged;
                com_DeNghi.SelectedIndexChanged -= Com_DeNghi_SelectedIndexChanged;
                comboBox_ChiHuyD.SelectedIndexChanged -= ComboBox_ChiHuyD_SelectedIndexChanged;
                comboBox1_ChonLoaiBaoCao.SelectedIndexChanged -= comboBox1_ChonLoaiBaoCao_SelectedIndexChanged;

                // CHẠY BẤT ĐỒNG BỘ TOÀN BỘ CẤU HÌNH (Giao diện cực mượt, không khựng)
                CapNhatThongBaoPhanMem();
                await LoadChiHuyDDictionaryAsync();
                await LoadDiaDiemAsync();
                await LoadThongTinAsync();
                await LoadComboBoxLoaiXuat_CheckBoxAsync();

                LoadComboBoxDeNghi();
                SetDeNghiVaTinhTyLe();
                HienThiDuongDanXuatDaChon();

                // CHỐT CHẶN AN TOÀN
                if (KiemTraCoDuLieuDanhSach())
                {
                    await Bang1_Async();
                    _allowLoadBang2 = true;
                    await Task.Yield();
                    await Bang2_Async();
                }
                else
                {
                    pieChart.Controls.Clear();
                    kryptonDataGridView1.DataSource = null;
                    kryptonDataGridView2.DataSource = null;
                }

                await KiemTraVaDongBoCSDLAsync();
                Module_XuatPhanLoai.NapLinkLuuDuongDanTepXuat();
                KiemTraDuLieuDanhSachVaKhoaNut();

                bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                CauHoiGiaoDien_PhienBan(laTanBinh);
                // ⭐ GIA CỐ CHUẨN KỸ SƯ: Gọi hàm mới tại đây để nạp và khớp dữ liệu Chỉ huy
                await LoadComboBox_ChiHuyDAsync();
                // Giải phóng luồng UI khi lọc data
                await CapNhatDanhSachPhanLoaiDeXuatAsync();
                Module_TrangThaiHeThong.CapNhatStatusCSDL(statusStrip1, toolStripStatusLabel1);
                // GẮN LẠI SỰ KIỆN
                Check_MoThuMuc.CheckedChanged += Check_MoThuMuc_CheckedChanged;
                comboBox1_ChonLoaiDeXuat.SelectedIndexChanged += comboBox1_ChonLoaiDeXuat_SelectedIndexChanged;
                checkBox1_TuDongChonNgayThang.CheckedChanged += checkBox1_TuDongChonNgayThang_CheckedChanged;
                com_DeNghi.SelectedIndexChanged += Com_DeNghi_SelectedIndexChanged;
                comboBox_ChiHuyD.SelectedIndexChanged += ComboBox_ChiHuyD_SelectedIndexChanged;
                comboBox1_ChonLoaiBaoCao.SelectedIndexChanged += comboBox1_ChonLoaiBaoCao_SelectedIndexChanged;
                ComboBox_ChiHuyD_SelectedIndexChanged(comboBox_ChiHuyD, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ReloadDuLieu lỗi: " + ex.Message);
            }
            finally
            {
                toolStripProgressBar1.Visible = false;
                this.ResumeLayout(true);
                _reloadLock.Release();
            }
        }
        private async Task LoadChiHuyDDictionaryAsync()
        {
            _dictChiHuyD.Clear();
            try
            {
                bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                string tableChiHuy = laTanBinh ? "ChiHuyD_TanBinh" : "ChiHuyD";

                using var cn = TaoKetNoiCSDL2(true);
                await cn.OpenAsync();
                using var cmd = cn.CreateCommand();
                // Sửa query chỉ vào bảng động
                cmd.CommandText = $"SELECT HoVaTen, ChucVu FROM [{tableChiHuy}]";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string hoTen = SafeDecrypt(reader["HoVaTen"]);
                    string chucVu = SafeDecrypt(reader["ChucVu"]);

                    if (string.IsNullOrWhiteSpace(hoTen)) continue;
                    string key = hoTen.ToLowerInvariant();
                    if (!_dictChiHuyD.ContainsKey(key)) _dictChiHuyD.Add(key, chucVu);
                }
            }
            catch (Exception ex) { Debug.WriteLine("Lỗi LoadChiHuyDDictionary: " + ex.Message); }
        }
        // SỬA HÀM THỨ 2 TRONG FORM 4
        private async Task LoadComboBox_ChiHuyDAsync()
        {
            try
            {
                bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                string tableChiHuy = laTanBinh ? "ChiHuyD_TanBinh" : "ChiHuyD";

                comboBox_ChiHuyD.BeginUpdate();
                comboBox_ChiHuyD.DataSource = null;
                comboBox_ChiHuyD.Items.Clear();

                var items = new List<ComboItem>();

                using (var conn = TaoKetNoiCSDL2(true))
                {
                    await conn.OpenAsync();
                    // Sửa query chỉ vào bảng động
                    string sqlChiHuy = $"SELECT ID, HoVaTen FROM [{tableChiHuy}] WHERE HoVaTen IS NOT NULL ORDER BY ID ASC";

                    using (var cmd = new SqliteCommand(sqlChiHuy, conn))
                    using (var rd = await cmd.ExecuteReaderAsync())
                    {
                        while (await rd.ReadAsync())
                        {
                            int id = rd.GetInt32(0);
                            string name = SafeDecrypt(rd["HoVaTen"]);

                            if (!string.IsNullOrWhiteSpace(name))
                            {
                                items.Add(new ComboItem { ID = id, Text = name.Trim() });
                            }
                        }
                    }

                    if (items.Count == 0)
                    {
                        comboBox_ChiHuyD.EndUpdate();
                        return;
                    }

                    comboBox_ChiHuyD.DisplayMember = "Text";
                    comboBox_ChiHuyD.ValueMember = "ID";
                    comboBox_ChiHuyD.DataSource = items;

                    const string sqlThongTin = "SELECT ChiHuyD FROM ThongTin WHERE ID = 1 LIMIT 1";
                    using (var cmd2 = new SqliteCommand(sqlThongTin, conn))
                    using (var rd2 = await cmd2.ExecuteReaderAsync())
                    {
                        if (await rd2.ReadAsync() && !rd2.IsDBNull(0))
                        {
                            string savedName = SafeDecrypt(rd2.GetString(0)).Trim();

                            var match = items.FirstOrDefault(x => string.Equals(x.Text, savedName, StringComparison.OrdinalIgnoreCase));
                            if (match != null)
                            {
                                comboBox_ChiHuyD.SelectedValue = match.ID;
                            }
                            else if (comboBox_ChiHuyD.Items.Count > 0)
                            {
                                comboBox_ChiHuyD.SelectedIndex = 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi LoadComboBox_ChiHuyDAsync: " + ex.Message);
            }
            finally
            {
                comboBox_ChiHuyD.EndUpdate();
            }
        }
        private async Task LoadDiaDiemAsync()
        {
            if (string.IsNullOrWhiteSpace(_csdl2Path) ||
                !File.Exists(_csdl2Path))
            {
                return;
            }

            comboBox_DiaDiem.TabStop = false;
            comboBox_DiaDiem.Enabled = false;
            comboBox_DiaDiem.AutoCompleteMode = AutoCompleteMode.None;
            comboBox_DiaDiem.AutoCompleteSource = AutoCompleteSource.None;

            try
            {
                comboBox_DiaDiem.BeginUpdate();
                comboBox_DiaDiem.Items.Clear();

                using var conn = TaoKetNoiCSDL2(true);
                await conn.OpenAsync();

                var diaDiemSet = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

                using (var cmd = new SqliteCommand(
                    @"SELECT DISTINCT TenNganGon
              FROM Tinh
              WHERE TenNganGon IS NOT NULL
              ORDER BY TenNganGon ASC",
                    conn))
                using (var rd = await cmd.ExecuteReaderAsync())
                {
                    while (await rd.ReadAsync())
                    {
                        if (rd.IsDBNull(0))
                            continue;

                        string ten = rd.GetString(0).Trim();

                        if (string.IsNullOrWhiteSpace(ten))
                            continue;

                        diaDiemSet.Add(ten);
                    }
                }

                if (diaDiemSet.Count > 0)
                {
                    string[] diaDiemArray = diaDiemSet.ToArray();

                    comboBox_DiaDiem.Items.AddRange(diaDiemArray);
                }

                string? diaDiemDaLuu = null;

                using (var cmd = new SqliteCommand(
                    "SELECT DiaDiem FROM ThongTin WHERE ID = 1 LIMIT 1",
                    conn))
                {
                    object? value = await cmd.ExecuteScalarAsync();

                    if (value != null && value != DBNull.Value)
                    {
                        try
                        {
                            diaDiemDaLuu =
                                BaoMatAES.GiaiMa(value.ToString()!)
                                ?.Trim();
                        }
                        catch
                        {
                            diaDiemDaLuu = null;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(diaDiemDaLuu) &&
                    diaDiemSet.Contains(diaDiemDaLuu))
                {
                    comboBox_DiaDiem.Text = diaDiemDaLuu;
                }
                else if (comboBox_DiaDiem.Items.Count > 0)
                {
                    comboBox_DiaDiem.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"LoadDiaDiemAsync: {ex.Message}");
            }
            finally
            {
                comboBox_DiaDiem.EndUpdate();
                comboBox_DiaDiem.Enabled = true;
            }
        }
        private async Task LoadComboBoxLoaiXuat_CheckBoxAsync()
        {
            string csdlPath = _csdl2Path;
            if (string.IsNullOrWhiteSpace(csdlPath) || !File.Exists(csdlPath)) return;
            try
            {
                using var conn = new SqliteConnection($"Data Source={csdlPath}");
                await conn.OpenAsync();
                using var cmd = new SqliteCommand("SELECT ChonDanhSachXuat, Chex_MoiThuMucXuat FROM ThongTin WHERE ID = 1", conn);
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    string chonXuat = reader["ChonDanhSachXuat"]?.ToString();
                    comboBox1_ChonLoaiDeXuat.Text = string.IsNullOrWhiteSpace(chonXuat) ? "" : BaoMatAES.GiaiMa(chonXuat);

                    string chex = reader["Chex_MoiThuMucXuat"]?.ToString();
                    chex = string.IsNullOrWhiteSpace(chex) ? "FALSE" : BaoMatAES.GiaiMa(chex).ToUpper();
                    Check_MoThuMuc.Checked = chex == "TRUE";
                }
            }
            catch (Exception ex) { Debug.WriteLine("Lỗi LoadComboBoxLoaiXuat: " + ex.Message); }
        }
        private async Task LoadThongTinAsync()
        {
            try
            {
                using var conn = TaoKetNoiCSDL2(true);
                await conn.OpenAsync();
                const string sql = @"SELECT T.*, B.ChonLoaiBaoCao, B.ChonTuan FROM ThongTin T LEFT JOIN ChonLoaiBaoCao B ON B.ID = 1 WHERE T.ID = 1 LIMIT 1";
                using var cmd = new SqliteCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    comboBox_ChiHuyD.Text = SafeDecrypt(reader["ChiHuyD"]);
                    com_DeNghi.Text = SafeDecrypt(reader["LoaiDeNghi"]);
                    comboBox1_ChonLoaiDeXuat.Text = SafeDecrypt(reader["ChonDanhSachXuat"]);
                    string chex = SafeDecrypt(reader["Chex_MoiThuMucXuat"]).ToUpper();
                    Check_MoThuMuc.Checked = (chex == "TRUE");

                    if (!checkBox1_TuDongChonNgayThang.Checked)
                    {
                        comboBox_Ngay.Text = SafeDecrypt(reader["Ngay"]);
                        comboBox_Thang.Text = SafeDecrypt(reader["Thang"]);
                        comboBox_Nam.Text = SafeDecrypt(reader["Nam"]);
                    }

                    using (var cmdTaoBang = new SqliteCommand("CREATE TABLE IF NOT EXISTS ThangHeThong (ID INTEGER PRIMARY KEY, Thang TEXT);", conn))
                    {
                        await cmdTaoBang.ExecuteNonQueryAsync();
                    }

                    using (var cmdThang = new SqliteCommand("SELECT Thang FROM ThangHeThong WHERE ID = 1", conn))
                    {
                        var resThang = await cmdThang.ExecuteScalarAsync();
                        if (resThang != null && resThang != DBNull.Value)
                        {
                            string thangRaw = resThang.ToString().Trim();
                            if (!string.IsNullOrEmpty(thangRaw))
                            {
                                if (int.TryParse(thangRaw, out int soThang))
                                {
                                    bool matchFound = false;
                                    foreach (var item in comboBox2_ChonSoThang.Items)
                                    {
                                        if (int.TryParse(item.ToString(), out int val) && val == soThang)
                                        {
                                            comboBox2_ChonSoThang.SelectedItem = item;
                                            matchFound = true;
                                            break;
                                        }
                                    }
                                    if (!matchFound) comboBox2_ChonSoThang.Text = soThang.ToString();
                                }
                                else
                                {
                                    comboBox2_ChonSoThang.Text = thangRaw;
                                }
                            }
                        }
                    }

                    string loaiBaoCao = SafeDecrypt(reader["ChonLoaiBaoCao"]);
                    string chonTuan = SafeDecrypt(reader["ChonTuan"]);
                    bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);

                    if (!laTanBinh)
                    {
                        comboBox1_ChonLoaiBaoCao.Text = "Tháng";
                        comboBox1_ChonLoaiBaoCao.Enabled = false;
                        label2_ChonTuan.Visible = false;
                        comboBox2_ChonSoTuan.Visible = false;
                    }
                    else
                    {
                        comboBox1_ChonLoaiBaoCao.Enabled = true;
                        comboBox1_ChonLoaiBaoCao.Text = string.IsNullOrEmpty(loaiBaoCao) ? "Tháng" : loaiBaoCao;
                        comboBox2_ChonSoTuan.Text = chonTuan;
                        CapNhatTrangThaiTuan();
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine("Lỗi LoadThongTin Form 4: " + ex.Message); }
        }
        public async Task CapNhatDanhSachPhanLoaiDeXuatAsync()
        {
            string selectedGoc = comboBox1_ChonLoaiDeXuat.Text;
            try
            {
                comboBox1_ChonLoaiDeXuat.BeginUpdate();
                comboBox1_ChonLoaiDeXuat.Items.Clear();

                // Đẩy thuật toán duyệt 10.000 dòng xuống luồng phụ
                List<string> danhSachDaLoc = await Task.Run(() => Form6_XuLyData.LayDanhSachPhanLoaiThucTe());

                foreach (var item in danhSachDaLoc)
                {
                    comboBox1_ChonLoaiDeXuat.Items.Add(item);
                }

                if (!string.IsNullOrWhiteSpace(selectedGoc) && comboBox1_ChonLoaiDeXuat.Items.Contains(selectedGoc))
                {
                    comboBox1_ChonLoaiDeXuat.Text = selectedGoc;
                }
                else if (comboBox1_ChonLoaiDeXuat.Items.Count > 0)
                {
                    comboBox1_ChonLoaiDeXuat.SelectedIndex = 0;
                }
                else
                {
                    comboBox1_ChonLoaiDeXuat.Text = "";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi nạp combobox Loại đề xuất: " + ex.Message);
            }
            finally
            {
                comboBox1_ChonLoaiDeXuat.EndUpdate();
            }
        }
        private void CauHoiGiaoDien_PhienBan(bool laTanBinh)
        {
            if (laTanBinh)
            {
                // 🟢 CHẾ ĐỘ TÂN BINH: Mở ra cho phép thao tác
                if (label4 != null) label4.Visible = true;
                comboBox1_ChonLoaiBaoCao.Visible = true;
                comboBox1_ChonLoaiBaoCao.Enabled = true;

                // ⭐ TÂN BINH: Mở phần chọn số tháng
                if (label2_LabelThang != null) label2_LabelThang.Visible = true;
                if (comboBox2_ChonSoThang != null) comboBox2_ChonSoThang.Visible = true;

                // Gọi hàm này để nó tự tính toán ẩn/hiện "Tuần" 
                // dựa trên giá trị đang chọn trong comboBox1
                CapNhatTrangThaiTuan();
            }
            else
            {
                // 🔴 CHẾ ĐỘ CBCS: Đóng lại, ẩn mình hoàn toàn
                if (label4 != null) label4.Visible = false;
                comboBox1_ChonLoaiBaoCao.Visible = false;

                // Ẩn luôn các phần liên quan đến "Tuần"
                if (label2_ChonTuan != null) label2_ChonTuan.Visible = false;
                if (comboBox2_ChonSoTuan != null) comboBox2_ChonSoTuan.Visible = false;

                // ⭐ CBCS: Ẩn phần chọn số tháng cho giao diện gọn
                if (label2_LabelThang != null) label2_LabelThang.Visible = false;
                if (comboBox2_ChonSoThang != null) comboBox2_ChonSoThang.Visible = false;

                // Mặc định giá trị ngầm là "Tháng" để các hàm Xuất file 
                // ở phía sau vẫn chạy đúng logic cho CBCS
                comboBox1_ChonLoaiBaoCao.Text = "Tháng";
            }
        }
        private void Form4_TrangDauTien_Resize(object sender, EventArgs e)
        {
            // Giả sử splitContainer2 là cái chứa Bảng 3 (trên) và Bảng 4 (dưới)
            // Nếu tên SplitContainer của bạn khác, hãy đổi tên cho đúng
            if (splitContainer2 != null)
            {
                // Tự động chia đôi chiều cao: 50% cho bảng trên, 50% cho bảng dưới
                splitContainer2.SplitterDistance = splitContainer2.Height / 2;
            }
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            LoadComboBoxDeNghi();

            Form27_TyLeQuyDinhE29.OnQuyDinhChanged += ReloadQuyDinh;

            // 🔥 set mặc định để có SelectedItem
            if (com_DeNghi.SelectedIndex == -1)
                com_DeNghi.SelectedIndex = 0;
        }
        private void ReloadQuyDinh()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(ReloadQuyDinh));
                return;
            }

            LoadQuyDinhTheoDeNghi(); // 🔥 GỌI TRỰC TIẾP
        }
        private void CapNhatTrangThaiTuan()
        {
            if (comboBox1_ChonLoaiBaoCao.SelectedItem == null)
                return;

            bool laTuan = comboBox1_ChonLoaiBaoCao.Text
                .Equals("Tuần", StringComparison.OrdinalIgnoreCase);

            // Hiện / Ẩn control
            label2_ChonTuan.Visible = laTuan;
            comboBox2_ChonSoTuan.Visible = laTuan;

            if (!laTuan)
            {
                comboBox2_ChonSoTuan.SelectedIndex = -1;
                return;
            }

            // ===== GỢI Ý TUẦN THEO NGÀY =====
            if (string.IsNullOrWhiteSpace(comboBox2_ChonSoTuan.Text))
            {
                int day = DateTime.Now.Day;

                int tuan =
                    day <= 7 ? 1 :
                    day <= 14 ? 2 :
                    day <= 21 ? 3 : 4;

                string goiY = $"Tuần {tuan}";

                if (comboBox2_ChonSoTuan.Items.Contains(goiY))
                    comboBox2_ChonSoTuan.SelectedItem = goiY;
            }
        }
        private void comboBox1_ChonLoaiBaoCao_SelectedIndexChanged(object sender, EventArgs e)
        {
            CapNhatTrangThaiTuan();
        }
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Gợi ý thao tác";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;

            // UX: phản hồi nhanh – không gây khó chịu
            toolTip1.InitialDelay = 300;
            toolTip1.AutoPopDelay = 2500;
            toolTip1.ReshowDelay = 100;
            toolTip1.ShowAlways = true;

            var tips = new Dictionary<Control, string>
    {
        // ===== THÔNG TIN ĐƠN VỊ / ĐỊA ĐIỂM =====
        { comboBox_DiaDiem, "Chọn địa điểm tổ chức hoặc áp dụng thống kê" },
        { comboBox_ChiHuyD, "Chọn chỉ huy đơn vị phê duyệt" },
        { com_DeNghi, "Kết quả phân loại tập thể đơn vị do Cụm thi đua xét" },

        // ===== LƯU / KIỂM TRA =====
        { kryptonButton_LuuThongTin, "Lưu toàn bộ thông tin đã nhập" },
        { kryptonButton_Refresh, "Làm mới dữ liệu và nhập lại từ đầu" },
        { kryptonButton_KiemTraTLvaQS, "Kiểm tra quân số và tỷ lệ theo dữ liệu hiện có" },
        { kryptonButton_MayTinh, "Mở công cụ máy tính hỗ trợ tính toán nhanh" },

        // ===== XUẤT TỆP =====
        { kryptonButton_ChonDuongDanLuu, "Chọn đường dẫn để lưu tệp xuất ra" },
        { Check_MoThuMuc, "Tự động mở thư mục chứa tệp sau khi xuất" },
        { comboBox1_ChonLoaiDeXuat, "Chọn loại dữ liệu cần xuất ra Excel" },

        { kryptonButton_XuatDanhSachLoai, "Xuất danh sách Excel theo loại đã chọn" },
        { kryptonButton_XuatTrinhKy, "Xuất tệp trình ký theo mẫu quy định" },
        { kryptonButton_XuatTatCa, "Xuất toàn bộ dữ liệu ra các tệp Excel" },
        { kryptonButton_MoThuMuc, "Mở thư mục chứa các tệp đã xuất" },
    };

            foreach (var tip in tips)
            {
                if (tip.Key != null) // an toàn khi control bị ẩn / đổi tên
                    toolTip1.SetToolTip(tip.Key, tip.Value);
            }
        }
        private void GanNgayThangNamVaoCombobox(bool khoaCombobox)
        {
            if (comboBox_Ngay == null || comboBox_Thang == null || comboBox_Nam == null)
                return;

            DateTime now = DateTime.Now;
            int namHeThong = Module_NamHeThong.LayNamHeThong();

            // Khởi tạo 1 lần duy nhất
            if (!isComboBoxInitDone)
            {
                comboBox_Ngay.Items.Clear();
                comboBox_Thang.Items.Clear();
                comboBox_Nam.Items.Clear();

                for (int i = 1; i <= 31; i++)
                    comboBox_Ngay.Items.Add(i.ToString("D2"));

                for (int i = 1; i <= 12; i++)
                    comboBox_Thang.Items.Add(i.ToString("D2"));

                for (int i = namHeThong - 1; i <= namHeThong + 10; i++)
                    comboBox_Nam.Items.Add(i.ToString());

                isComboBoxInitDone = true;
            }

            // Gán giá trị hiện tại (đã chắc chắn format khớp)
            comboBox_Ngay.SelectedItem = now.Day.ToString("D2");
            comboBox_Thang.SelectedItem = now.Month.ToString("D2");
            comboBox_Nam.SelectedItem = namHeThong.ToString();

            // Enable / Disable đồng bộ
            bool enable = !khoaCombobox;
            comboBox_Ngay.Enabled = enable;
            comboBox_Thang.Enabled = enable;
            comboBox_Nam.Enabled = enable;
        }
        private void UocLuongDoRongCacCot(DataGridView dgv)
        {
            if (dgv == null || dgv.Columns.Count == 0) return;

            // --- 1️⃣ Ước lượng chiều rộng dựa trên header + nội dung ---
            Dictionary<string, int> colWidths = new Dictionary<string, int>();
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (!col.Visible) continue;

                int maxWidth = TextRenderer.MeasureText(col.HeaderText, dgv.ColumnHeadersDefaultCellStyle.Font).Width;

                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.IsNewRow) continue;
                    string val = row.Cells[col.Name].Value?.ToString() ?? "";
                    int w = TextRenderer.MeasureText(val, dgv.DefaultCellStyle.Font).Width;
                    if (w > maxWidth) maxWidth = w;
                }

                // Giới hạn min/max để tránh quá hẹp hoặc quá rộng
                colWidths[col.Name] = Math.Min(Math.Max(maxWidth + 20, 30), 200);
            }
            // --- 2️⃣ Gán FillWeight theo cột và ước lượng ---
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (!col.Visible) continue;

                int width = colWidths.ContainsKey(col.Name) ? colWidths[col.Name] : 50;

                switch (col.Name)
                {
                    case "STT":
                        col.FillWeight = width * 0.5f; // STT nhỏ
                        break;
                    case "DonVi":
                        col.FillWeight = width * 1.5f;
                        break;
                    case "TongQS":
                    case "Loai_1":
                    case "Loai_2":
                    case "Loai_3":
                    case "Loai_4":
                    case "Khong_PL":
                        col.FillWeight = width * 2f;
                        break;
                    case "PhanTramLoai_1":
                    case "PhanTramLoai_2":
                    case "PhanTramLoai_3":
                    case "PhanTramLoai_4":
                    case "PhanTramKhong_PL":
                        col.FillWeight = width * 1.5f;
                        break;
                    default:
                        col.FillWeight = width * 1.5f;
                        break;
                }
            }

            // --- 3️⃣ Bật AutoSizeColumnsMode.Fill để full khung ---
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }
        private void ComboBox_ChiHuyD_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Lấy chuỗi văn bản đang hiển thị trên ComboBox (An toàn với cả DataSource / DataRowView)
            string tenDangChon = comboBox_ChiHuyD.Text.Trim();

            if (string.IsNullOrEmpty(tenDangChon))
            {
                label2_ChucVu.Text = "";
                return;
            }

            // Tra cứu trong Dictionary
            if (_dictChiHuyD != null && _dictChiHuyD.TryGetValue(tenDangChon, out string? chucVu))
            {
                label2_ChucVu.Text = string.IsNullOrWhiteSpace(chucVu) ? "- Chưa có chức vụ" : $"- {chucVu}";
            }
            else
            {
                label2_ChucVu.Text = "- Không xác định";
            }
        }
        private void checkBox1_TuDongChonNgayThang_CheckedChanged(object sender, EventArgs e)
        {
            bool isChecked = checkBox1_TuDongChonNgayThang.Checked;
            // Set combobox
            GanNgayThangNamVaoCombobox(isChecked);
            // Set màu
            checkBox1_TuDongChonNgayThang.ForeColor = isChecked ? Color.Green : Color.Red;
            // Lưu vào CSDL
            try
            {
                string csdlPath = _csdl2Path;
                if (!string.IsNullOrWhiteSpace(csdlPath) && File.Exists(csdlPath))
                {
                    using var conn = new SqliteConnection($"Data Source={csdlPath}");
                    conn.Open();
                    using var cmd = new SqliteCommand(
                        "UPDATE ThongTin SET TuDongGanNgayThangNamHienTai = @val WHERE ID = 1", conn);
                    cmd.Parameters.AddWithValue("@val", BaoMatAES.MaHoa(isChecked ? "TRUE" : "FALSE"));
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                ThongBao("Lỗi lưu trạng thái tự động ngày/tháng/năm: " + ex.Message);
            }
        }
        private void LoadCheckBoxTuDongChonNgayThang()
        {
            bool isChecked = false;
            string csdlPath = _csdl2Path;
            try
            {
                if (!string.IsNullOrWhiteSpace(csdlPath) && File.Exists(csdlPath))
                {
                    using var conn = new SqliteConnection($"Data Source={csdlPath}");
                    conn.Open();

                    using var cmd = new SqliteCommand(
                        "SELECT TuDongGanNgayThangNamHienTai FROM ThongTin WHERE ID = 1", conn);
                    var result = cmd.ExecuteScalar()?.ToString();

                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        string giaiMa = BaoMatAES.GiaiMa(result)?.Trim().ToUpper();
                        isChecked = giaiMa == "TRUE";
                    }
                }
            }
            catch
            {
                isChecked = false;
            }

            // Set trạng thái, màu, chỉ chọn item (không xóa thêm)
            checkBox1_TuDongChonNgayThang.Checked = isChecked;
            checkBox1_TuDongChonNgayThang.ForeColor = isChecked ? Color.Green : Color.Red;

            GanNgayThangNamVaoCombobox(isChecked); // Chỉ chọn và khóa nếu cần
        }
        private void CapNhatThongBaoPhanMem()
        {
            try
            {
                string fileCSDL2 = _csdl2Path;
                if (!File.Exists(fileCSDL2)) return;

                string doiTuong = "";

                using (var cn = new SqliteConnection($"Data Source={fileCSDL2}"))
                {
                    cn.Open();

                    using var cmd = cn.CreateCommand();
                    cmd.CommandText = "SELECT DoiTuong FROM PhienBan_DoiTuong WHERE ID = 1";

                    object? val = cmd.ExecuteScalar();

                    if (val != null)
                    {
                        string? chuoiMaHoa = val.ToString();

                        if (!string.IsNullOrWhiteSpace(chuoiMaHoa))
                        {
                            doiTuong = BaoMatAES.GiaiMa(chuoiMaHoa);
                        }
                    }
                }

                // Khi thiết lập StatusStrip
                toolStripStatusLabel2.Spring = true;      // chiếm phần còn lại
                toolStripStatusLabel2.TextAlign = ContentAlignment.MiddleRight;

                // Hiển thị ở toolStripStatusLabel2, căn phải
                toolStripStatusLabel2.Text = $"Phần mềm: {doiTuong}";
                toolStripStatusLabel2.TextAlign = ContentAlignment.MiddleRight;
            }
            catch
            {
                toolStripStatusLabel2.Text = "Phần mềm: (không xác định)";
            }
        }
        private void LuuLuaChonXuatVaCheckBox()
        {
            string csdlPath = _csdl2Path;
            if (string.IsNullOrWhiteSpace(csdlPath) || !File.Exists(csdlPath)) return;

            try
            {
                using var conn = new SqliteConnection($"Data Source={csdlPath}");
                conn.Open();

                // Mã hóa dữ liệu
                string chonXuat = BaoMatAES.MaHoa(comboBox1_ChonLoaiDeXuat.Text.Trim());
                string chex = BaoMatAES.MaHoa(Check_MoThuMuc.Checked ? "TRUE" : "FALSE");

                // Đảm bảo bản ghi ID=1 tồn tại
                using (var cmdCheck = new SqliteCommand("SELECT COUNT(*) FROM ThongTin WHERE ID = 1", conn))
                {
                    long count = (long)cmdCheck.ExecuteScalar();
                    if (count == 0)
                    {
                        using var cmdInsert = new SqliteCommand("INSERT INTO ThongTin (ID) VALUES (1)", conn);
                        cmdInsert.ExecuteNonQuery();
                    }
                }

                // Update
                using var cmdUpdate = new SqliteCommand(@"
UPDATE ThongTin SET
    ChonDanhSachXuat = @ChonDanhSachXuat,
    Chex_MoiThuMucXuat = @Chex_MoiThuMucXuat
WHERE ID = 1", conn);
                cmdUpdate.Parameters.AddWithValue("@ChonDanhSachXuat", chonXuat);
                cmdUpdate.Parameters.AddWithValue("@Chex_MoiThuMucXuat", chex);
                cmdUpdate.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Module_ThongBao.Loi("Lỗi lưu lựa chọn xuất / mở thư mục: " + ex.Message);
            }
        }
        private void Check_MoThuMuc_CheckedChanged(object sender, EventArgs e)
        {
            Check_MoThuMuc.ForeColor = Check_MoThuMuc.Checked ? Color.Green : Color.Red;
            LuuLuaChonXuatVaCheckBox();
        }
        private void comboBox1_ChonLoaiDeXuat_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Gọi chung hàm lưu CSDL
            LuuLuaChonXuatVaCheckBox();
        }
        private void LoadSettings()
        {
            try
            {
                Check_MoThuMuc.ForeColor =
                    Check_MoThuMuc.Checked ? Color.Green : Color.Red;
            }
            catch
            {
                // Có thể ghi log nếu muốn
            }
        }
        public void LoadComboBoxDeNghi()
        {
            if (com_DeNghi.Items.Count == 0)
            {
                com_DeNghi.Items.AddRange(new string[]
                {
                    "Loại 1", "Loại 2", "Loại 3", "Loại 4", "Không phân loại"
                });
            }
        }
        private void LoadQuyDinhTheoDeNghi()
        {
            string? key = com_DeNghi.SelectedItem?.ToString() switch
            {
                "Loại 1" => "Loai1_TapThe",
                "Loại 2" => "Loai2_TapThe",
                "Loại 3" => "Loai3_TapThe",
                "Loại 4" => "Loai4_TapThe",
                "Không phân loại" => "KhongPL_TapThe",
                _ => null
            };

            int[] values = key != null
                ? Module_QuyDinhTyLe.GetLoaiTapThe(key)
                : new int[] { 0, 0, 0 };

            phanTramLoai1 = values.ElementAtOrDefault(0);
            phanTramLoai2 = values.ElementAtOrDefault(1);
            phanTramLoai3 = values.ElementAtOrDefault(2);

            CapNhatLabelPhanTram();
        }
        private void Com_DeNghi_SelectedIndexChanged(object? sender, EventArgs e)
        {
            LoadQuyDinhTheoDeNghi();
        }
        private void CapNhatLabelPhanTram()
        {
            // ===== Text luôn hiển thị như cũ =====
            label__PhanTramLoai1.Text = $"Loại 1: {phanTramLoai1}%";
            label__PhanTramLoai2.Text = $"Loại 2: {phanTramLoai2}%";
            label__PhanTramLoai3.Text = $"Loại 3: {phanTramLoai3}%";

            // ===== Xác định màu theo đề nghị =====
            Color mau;

            switch (com_DeNghi.Text.Trim())
            {
                case "Loại 1":
                    mau = Color.Green;
                    break;

                case "Loại 2":
                    mau = Color.Purple;
                    break;

                case "Loại 3":
                case "Loại 4":
                case "Không phân loại":
                default:
                    mau = Color.Red;
                    break;
            }

            // ===== ÁP DỤNG MÀU CHO TẤT CẢ LABEL =====
            label__PhanTramLoai1.ForeColor = mau;
            label__PhanTramLoai2.ForeColor = mau;
            label__PhanTramLoai3.ForeColor = mau;
        }
        private void SetDeNghiVaTinhTyLe()
        {
            Com_DeNghi_SelectedIndexChanged(com_DeNghi, EventArgs.Empty);
        }
        private void datChieuCaoNut()
        {
            int h = comboBox_DiaDiem.PreferredSize.Height;
            Krypton.Toolkit.KryptonButton[] buttons =
            {
                kryptonButton_Refresh,
                kryptonButton_KiemTraTLvaQS,
                kryptonButton_MayTinh,
                kryptonButton_XuatDanhSachLoai,
                kryptonButton_XuatTrinhKy,
                kryptonButton_XuatTatCa,
                kryptonButton_LuuThongTin,
                kryptonButton_ChonDuongDanLuu,
                kryptonButton_MoThuMuc
            };
            foreach (var btn in buttons)
                btn.Height = h + 3;
        }
        private void ThongBao(string message)
        {
            if (this.InvokeRequired) // 'this' là Form
            {
                this.Invoke(new Action(() => toolStripStatusLabel1.Text = message));
            }
            else
            {
                toolStripStatusLabel1.Text = message;
            }
        }   
        private class ComboItem
        {
            public int ID { get; set; }
            public string Text { get; set; }
            public override string ToString() => Text;
        }
       //Biểu đồ tròn
        private void tabPage2_Click(object? sender, EventArgs e)
        {
            piePanel?.Invalidate();
        }
        private void PiePanel_Paint(object? sender, PaintEventArgs e)
        {
            if (piePanel == null || pieData == null || pieData.Count == 0) return;

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // --- Lấy dữ liệu từ pieData ---
            string[] labels = pieData.Keys.ToArray();
            double[] values = pieData.Values.Select(v => (double)v).ToArray();
            double total = values.Sum();

            if (total <= 0)
            {
                using var f = new Font("Segoe UI", 10);
                string msg = "Không có dữ liệu để vẽ.";
                var textSize = g.MeasureString(msg, f);
                g.DrawString(msg, f, Brushes.Gray, (piePanel.Width - textSize.Width) / 2, (piePanel.Height - textSize.Height) / 2);
                return;
            }

            // --- Màu sắc từng phần ---
            Color[] colors = {
        Color.FromArgb(102, 204, 102),   // Loại 1
        Color.FromArgb(255, 105, 180),   // Loại 2
        Color.FromArgb(135, 206, 250),   // Loại 3
        Color.FromArgb(255, 165, 0),     // Loại 4
        Color.FromArgb(192, 192, 192)    // Không PL
    };

            int padding = 20;
            int size = Math.Min(piePanel.Width, piePanel.Height) - padding;
            Rectangle pieRect = new Rectangle(padding / 2, padding / 2, size, size);

            float startAngle = 0f;
            var font = new Font("Segoe UI", 9, FontStyle.Bold);

            for (int i = 0; i < values.Length; i++)
            {
                float sweepAngle = (float)(values[i] / total * 360.0);
                if (sweepAngle <= 0) continue;

                // Hiệu ứng nổi (nếu muốn)
                float offsetX = 0, offsetY = 0;
                if (i == highlightedSlice) // bạn có thể dùng biến toàn cục để highlight
                {
                    double midAngle = startAngle + sweepAngle / 2;
                    double rad = Math.PI * midAngle / 180.0;
                    offsetX = (float)(10 * Math.Cos(rad));
                    offsetY = (float)(10 * Math.Sin(rad));
                }

                Rectangle sliceRect = new Rectangle(pieRect.X + (int)offsetX, pieRect.Y + (int)offsetY, pieRect.Width, pieRect.Height);

                // 3D Shadow
                var shadowRect = sliceRect;
                shadowRect.Offset(3, 3);
                g.FillPie(Brushes.Gray, shadowRect, startAngle, sweepAngle);

                // Gradient fill
                using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(sliceRect, Color.White, colors[i], 45f);
                g.FillPie(brush, sliceRect, startAngle, sweepAngle);
                g.DrawPie(Pens.White, sliceRect, startAngle, sweepAngle);

                // Ghi chú với phần trăm
                double mid = startAngle + sweepAngle / 2;
                double radMid = Math.PI * mid / 180.0;
                float centerX = sliceRect.Left + sliceRect.Width / 2f;
                float centerY = sliceRect.Top + sliceRect.Height / 2f;
                float labelRadius = size / 3f;
                float labelX = centerX + (float)(labelRadius * Math.Cos(radMid));
                float labelY = centerY + (float)(labelRadius * Math.Sin(radMid));

                string text = $"{labels[i]}: {values[i]} ({values[i] / total:P0})";

                Brush textBrush = values[i] / total > 0.2 ? Brushes.White : Brushes.Black;
                var textSize = g.MeasureString(text, font);
                g.DrawString(text, font, textBrush, labelX - textSize.Width / 2, labelY - textSize.Height / 2);

                startAngle += sweepAngle;
            }
        }
        public async Task RefreshPieChartAsync()
        {
            string csdl2Path = _csdl2Path;
            if (string.IsNullOrWhiteSpace(csdl2Path))
                return;

            pieData = await Task.Run(() =>
            {
                int l1 = 0, l2 = 0, l3 = 0, l4 = 0, kpl = 0;

                try
                {
                    using (var conn = new SqliteConnection($"Data Source={csdl2Path}"))
                    {
                        conn.Open();

                        using (var cmd = new SqliteCommand("SELECT PhanLoai FROM DanhSach", conn))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string phanLoai = SafeDecrypt(reader["PhanLoai"]);

                                switch (phanLoai)
                                {
                                    case "Loại 1": l1++; break;
                                    case "Loại 2": l2++; break;
                                    case "Loại 3": l3++; break;
                                    case "Loại 4": l4++; break;
                                    case "Không PL": kpl++; break;
                                }
                            }
                        }
                    }
                }
                catch
                {
                }

                return new Dictionary<string, int>
        {
            { "Loại 1", l1 },
            { "Loại 2", l2 },
            { "Loại 3", l3 },
            { "Loại 4", l4 },
            { "Không PL", kpl }
        };
            });

            if (piePanel != null && !piePanel.IsDisposed)
                piePanel.Invalidate();
        }
        private async void KhoiTaoPieChart()
        {
            if (piePanel == null)
            {
                piePanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White
                };

                piePanel.Paint += PiePanel_Paint;

                pieChart.Controls.Clear();
                pieChart.Controls.Add(piePanel);
            }

            await RefreshPieChartAsync();
        }
        //Thuật toán khác
        private string _textGocNutMayTinh = null;
        private Image _anhGocNutMayTinh = null;
        private void kryptonButton_MayTinh_Click(object sender, EventArgs e)
        {
            // 1. Lưu lại Text và Icon gốc ở lần bấm đầu tiên
            if (_textGocNutMayTinh == null)
            {
                _textGocNutMayTinh = kryptonButton_MayTinh.Values.Text;
                _anhGocNutMayTinh = kryptonButton_MayTinh.Values.Image;
            }
            // 2. Đổi giao diện nút để báo hiệu công cụ đang được mở
            kryptonButton_MayTinh.Values.Text = "Đang mở...";
            // kryptonButton_MayTinh.Values.Image = null; // Bỏ comment dòng này nếu bạn muốn tạm ẩn icon
            // 3. Khởi tạo Form 14 (Máy tính) nếu chưa có
            if (formTinhToan == null || formTinhToan.IsDisposed)
            {
                formTinhToan = new Form14
                {
                    Owner = this,          // gán form mẹ
                    ShowInTaskbar = false  // không tạo icon taskbar mới
                };
                // 🌟 BÍ QUYẾT: Đăng ký sự kiện khi tắt Máy tính thì nút quay về bình thường
                formTinhToan.FormClosed += (s, ev) =>
                {
                    // Bảo vệ UI Thread: Kiểm tra Form 4 còn sống không trước khi đổi tên nút
                    if (!this.IsDisposed && this.IsHandleCreated)
                    {
                        kryptonButton_MayTinh.Values.Text = _textGocNutMayTinh;
                        kryptonButton_MayTinh.Values.Image = _anhGocNutMayTinh;
                    }
                };
            }

            // 4. Hiển thị và đưa Form Máy tính lên trên cùng
            if (!formTinhToan.Visible)
            {
                formTinhToan.Show();
            }

            formTinhToan.Activate();
        }
        private Form11_KiemTraTyLe form11;
        #region ProgressBar Safe Control
        //private void Progress_Start()
        //{
        //    if (IsDisposed || Disposing) return;

        //    if (InvokeRequired)
        //    {
        //        Invoke(new Action(Progress_Start));
        //        return;
        //    }

        //    toolStripProgressBar1.Visible = true;
        //    toolStripProgressBar1.Minimum = 0;
        //    toolStripProgressBar1.Maximum = 100;
        //    toolStripProgressBar1.Value = 0;
        //}

        private void Progress_Start()
        {
            if (IsDisposed || Disposing) return;

            if (InvokeRequired)
            {
                Invoke(new Action(Progress_Start));
                return;
            }

            // Khóa cứng kích thước để không bị phình to
            toolStripProgressBar1.AutoSize = false;
            toolStripProgressBar1.Size = new System.Drawing.Size(150, 16);

            toolStripProgressBar1.Visible = true;
            toolStripProgressBar1.Minimum = 0;
            toolStripProgressBar1.Maximum = 100;
            toolStripProgressBar1.Value = 0;
        }
        // ⭐ NÂNG CẤP THÀNH ASYNC TASK ĐỂ TẠO HIỆU ỨNG TRƯỢT TĂNG DẦN
        private async Task Progress_StepAsync(int value)
        {
            if (IsDisposed || Disposing) return;

            int targetValue = 0;
            Action calcTarget = () => {
                targetValue = toolStripProgressBar1.Value + value;
                if (targetValue > toolStripProgressBar1.Maximum) targetValue = toolStripProgressBar1.Maximum;
                if (targetValue < toolStripProgressBar1.Minimum) targetValue = toolStripProgressBar1.Minimum;
            };
            if (InvokeRequired) Invoke(calcTarget); else calcTarget();

            while (true)
            {
                if (IsDisposed || Disposing) break;

                bool isReached = false;
                Action stepUp = () => {
                    // Tăng bước nhảy lên 2% mỗi khung hình thay vì 1% để trượt nhanh hơn
                    toolStripProgressBar1.Value += (toolStripProgressBar1.Value + 2 <= targetValue) ? 2 : 1;

                    if (toolStripProgressBar1.Value >= targetValue)
                        isReached = true;
                };

                if (InvokeRequired) Invoke(stepUp); else stepUp();

                if (isReached) break;

                // ⭐ ÉP XUNG: Giảm từ 15ms xuống còn 5ms
                await Task.Delay(5);
            }
        }

        private async Task Progress_EndAsync()
        {
            if (IsDisposed || Disposing) return;

            int current = 0;
            int max = 100;

            Action getValues = () => {
                current = toolStripProgressBar1.Value;
                max = toolStripProgressBar1.Maximum;
            };
            if (InvokeRequired) Invoke(getValues); else getValues();

            if (current < max)
            {
                await Progress_StepAsync(max - current);
            }

            // ⭐ ÉP XUNG: Giảm thời gian khựng lại lúc đạt 100% từ 200ms xuống 50ms
            await Task.Delay(50);

            Action closeProgress = () => {
                toolStripProgressBar1.Visible = false;
                toolStripProgressBar1.Value = 0;
            };
            if (InvokeRequired) Invoke(closeProgress); else closeProgress();
        }

        // ⭐ NÂNG CẤP END: Cho trượt nốt phần còn lại tới 100% rồi mới đóng
      
        #endregion
        private async void kryptonButton_Refresh_Click(object sender, EventArgs e)
        {
            string textBanDau = kryptonButton_Refresh.Values.Text;
            Image anhBanDau = kryptonButton_Refresh.Values.Image;

            try
            {
                // 1. KHÓA GIAO DIỆN & BẮT ĐẦU PROGRESS BAR
                kryptonButton_Refresh.Enabled = false;
                kryptonButton_Refresh.Values.Text = "Đang xử lý...";
                kryptonButton_Refresh.Values.Image = null;
                Progress_Start();

                await Task.Delay(100); // Nhường nhịp cho UI render mượt

                // 2. RESET DỮ LIỆU RAM
                await Progress_StepAsync(5);
                Module_TaiKhoan.TenTaiKhoan_RAM = string.Empty;
                Module_TaiKhoan.MatKhau_RAM = string.Empty;

                // 3. RESET ĐƯỜNG DẪN CSDL RAM
                await Progress_StepAsync(5);
                string[] propNames = { "DuongDanCSDL1", "DuongDanCSDL2", "DuongDanCSDL3", "DuongDanCSDL4", "DuongDanCSDL4ex" };
                foreach (string p in propNames)
                {
                    try
                    {
                        var prop = typeof(Module_DanduongGPS).GetProperty(p);
                        if (prop != null && prop.CanWrite) prop.SetValue(null, string.Empty);
                    }
                    catch { }
                }

                await Progress_StepAsync(5);

                // =========================================================
                // ⭐ 4. TẠO CSDL TRÊN LUỒNG NGẦM (GIẢI PHÓNG UI THREAD)
                // =========================================================
                string databaseFolder = Path.Combine(AppContext.BaseDirectory, "Database");
                Directory.CreateDirectory(databaseFolder);
                string[] csdlFiles = { "csdl1.db", "csdl2.db", "csdl3.db", "csdl4.db", "csdlex.xlsx" };

                // Ném toàn bộ tác vụ ổ cứng vào Background Thread để Progress Bar không bị giật
                await Task.Run(() =>
                {
                    foreach (string file in csdlFiles)
                    {
                        try
                        {
                            string duongDan = Path.Combine(databaseFolder, file);
                            if (File.Exists(duongDan)) continue;

                            if (file.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                            {
                                var builder = new SqliteConnectionStringBuilder
                                {
                                    DataSource = duongDan,
                                    Mode = SqliteOpenMode.ReadWriteCreate,
                                    Pooling = true
                                };

                                using (var conn = new SqliteConnection(builder.ToString()))
                                {
                                    conn.Open();
                                    using (var cmd = conn.CreateCommand())
                                    {
                                        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS DanhSach (ID INTEGER PRIMARY KEY AUTOINCREMENT);";
                                        cmd.CommandTimeout = 15;
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            else if (file.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                            {
                                using (var wb = new ClosedXML.Excel.XLWorkbook())
                                {
                                    wb.AddWorksheet("Sheet1");
                                    wb.SaveAs(duongDan);
                                }
                            }
                        }
                        catch (Exception exFile)
                        {
                            Debug.WriteLine($"Lỗi tạo file [{file}]: {exFile.Message}");
                        }
                    }
                });

                await Progress_StepAsync(15);

                // 5. CẬP NHẬT LẠI ĐƯỜNG DẪN HỆ THỐNG
                string[] fileNames = { "csdl1.db", "csdl2.db", "csdl3.db", "csdl4.db", "csdlex.xlsx" };
                for (int i = 0; i < propNames.Length; i++)
                {
                    try
                    {
                        var prop = typeof(Module_DanduongGPS).GetProperty(propNames[i]);
                        if (prop != null && prop.CanWrite)
                        {
                            string fullPath = Path.Combine(Module_DanduongGPS.ThuMucCoSoDuLieu, fileNames[i]);
                            prop.SetValue(null, fullPath);
                        }
                    }
                    catch { }
                }

                await Progress_StepAsync(10);

                // 6. RESET CONTROL FORM
                foreach (Control ctrl in this.Controls)
                {
                    try
                    {
                        if (ctrl is System.Windows.Forms.ComboBox cb) cb.SelectedIndex = -1;
                        else if (ctrl is System.Windows.Forms.TextBox tb) tb.Clear();
                        else if (ctrl is System.Windows.Forms.CheckBox ck) ck.Checked = false;
                    }
                    catch { }
                }

                // 7. HOÀN TẤT & ĐÓNG GIAO DIỆN CHỜ
                await Progress_EndAsync();

                ThongBao("Phần mềm đã được làm mới thành công!");
                await Task.Delay(200);

                try { Module_ThongBao.CapNhatThongTin(); } catch { }
            }
            catch (Exception ex)
            {
                Module_ThongBao.Loi("Lỗi khi làm mới phần mềm:\n" + ex.Message);
                Invoke(new Action(() => toolStripProgressBar1.Visible = false));
            }
            finally
            {
                // Phục hồi giao diện nút bấm
                kryptonButton_Refresh.Values.Text = textBanDau;
                kryptonButton_Refresh.Values.Image = anhBanDau;
                kryptonButton_Refresh.Enabled = true;

                // Gọi 1 lần duy nhất ở đây là đủ để cập nhật thanh Status
                Module_TrangThaiHeThong.CapNhatStatusCSDL(statusStrip1, toolStripStatusLabel1);
            }
        }
        private async void kryptonButton_LuuThongTin_Click(object sender, EventArgs e)
        {
            // =========================================================
            // LỚP VỎ UX: LƯU TRẠNG THÁI GỐC CỦA NÚT
            // =========================================================
            string textBanDau = kryptonButton_LuuThongTin.Values.Text;
            Image anhBanDau = kryptonButton_LuuThongTin.Values.Image;

            try
            {
                // THIẾT LẬP TRẠNG THÁI "ĐANG LƯU"
                kryptonButton_LuuThongTin.Enabled = false;
                kryptonButton_LuuThongTin.Values.Text = "Đang lưu...";
                kryptonButton_LuuThongTin.Values.Image = null;

                // Nhịp nghỉ 100ms để giao diện vẽ lại chữ "Đang lưu..."
                await Task.Delay(100);

                // =========================================================================
                // BẮT ĐẦU: 100% CODE GỐC CỦA BẠN (ĐÃ NÂNG CẤP LÊN ASYNC)
                // =========================================================================
                Progress_Start();
                try
                {
                    // ================== 1. Kiểm tra dữ liệu ==================
                    if (string.IsNullOrWhiteSpace(comboBox_DiaDiem.Text) ||
                        string.IsNullOrWhiteSpace(comboBox_Ngay.Text) ||
                        string.IsNullOrWhiteSpace(comboBox_Thang.Text) ||
                        string.IsNullOrWhiteSpace(comboBox_Nam.Text) ||
                        string.IsNullOrWhiteSpace(comboBox_ChiHuyD.Text) ||
                        string.IsNullOrWhiteSpace(com_DeNghi.Text) ||
                        phanTramLoai1 <= 0 ||
                        phanTramLoai2 <= 0 ||
                        phanTramLoai3 <= 0)
                    {
                        await Progress_EndAsync();
                        Module_ThongBao.Loi("Chưa khai báo đầy đủ thông tin!");
                        return;
                    }
                    await Progress_StepAsync(20);
                    string csdlPath = _csdl2Path;
                    using (var conn = new SqliteConnection($"Data Source={csdlPath}"))
                    {
                        // ⭐ NÂNG CẤP BẤT ĐỒNG BỘ: OpenAsync
                        await conn.OpenAsync();

                        using (var tran = conn.BeginTransaction())
                        {
                            try
                            {
                                await Progress_StepAsync(10);
                                // ================== 2. Chuẩn bị dữ liệu ==================
                                string diaDiem = BaoMatAES.MaHoa(comboBox_DiaDiem.Text);
                                string ngay = BaoMatAES.MaHoa(comboBox_Ngay.Text);
                                string thang = BaoMatAES.MaHoa(comboBox_Thang.Text);
                                string nam = BaoMatAES.MaHoa(comboBox_Nam.Text);
                                string chiHuyD = BaoMatAES.MaHoa(comboBox_ChiHuyD.Text);
                                string deNghi = BaoMatAES.MaHoa(com_DeNghi.Text);
                                string ptLoai1 = BaoMatAES.MaHoa(phanTramLoai1.ToString());
                                string ptLoai2 = BaoMatAES.MaHoa(phanTramLoai2.ToString());
                                string ptLoai3 = BaoMatAES.MaHoa(phanTramLoai3.ToString());
                                string loaiBaoCaoRaw = comboBox1_ChonLoaiBaoCao.Text.Trim();
                                string chonTuanRaw = "";
                                if (loaiBaoCaoRaw.Equals("Tuần", StringComparison.OrdinalIgnoreCase))
                                {
                                    chonTuanRaw = comboBox2_ChonSoTuan.Text.Trim();
                                }
                                string loaiBaoCao = BaoMatAES.MaHoa(loaiBaoCaoRaw);
                                string chonTuan = BaoMatAES.MaHoa(chonTuanRaw);
                                await Progress_StepAsync(10);

                                // ================== 3. UPSERT bảng ChonLoaiBaoCao ==================
                                using (var cmd = new SqliteCommand(@"
INSERT INTO ChonLoaiBaoCao (ID, ChonLoaiBaoCao, ChonTuan)
VALUES (1,@Loai,@Tuan)
ON CONFLICT(ID)
DO UPDATE SET
ChonLoaiBaoCao=@Loai,
ChonTuan=@Tuan
", conn, tran))
                                {
                                    //cmd.Parameters.AddWithValue("@Loai", loaiBaoCao);
                                    // CODE CHUẨN
                                    cmd.Parameters.Add("@Loai", SqliteType.Text).Value = loaiBaoCao;
                                    cmd.Parameters.AddWithValue("@Tuan", chonTuan);

                                    // ⭐ NÂNG CẤP BẤT ĐỒNG BỘ: ExecuteNonQueryAsync
                                    await cmd.ExecuteNonQueryAsync();
                                }
                                await Progress_StepAsync(10);
                                // ================== 3.5 UPSERT bảng ThangHeThong ==================
                                bool isTanBinh = Module_TaiKhoan.LayPhienBanPhanMem()
                                    .Contains("tân binh", StringComparison.OrdinalIgnoreCase);

                                string thangHTRaw = isTanBinh
                                    ? comboBox2_ChonSoThang.Text.Trim()
                                    : comboBox_Thang.Text.Trim();

                                // 🚀 Validate chặt
                                if (!int.TryParse(thangHTRaw, out int soThang) || soThang < 1 || soThang > 12)
                                {
                                    throw new Exception("Tháng không hợp lệ (1-12)");
                                }

                                // 🚀 Format chuẩn
                                string thangFormat = soThang.ToString("D2");

                                using (var cmdThang = new SqliteCommand(@"
INSERT INTO ThangHeThong (ID, Thang)
VALUES (1, @Thang)
ON CONFLICT(ID)
DO UPDATE SET Thang = @Thang
", conn, tran))
                                {
                                    cmdThang.Parameters.Add("@Thang", SqliteType.Text).Value = thangFormat;

                                    // ⭐ NÂNG CẤP BẤT ĐỒNG BỘ: ExecuteNonQueryAsync
                                    await cmdThang.ExecuteNonQueryAsync();
                                }

                                // ================== 4. UPSERT bảng ThongTin ==================
                                using (var cmd = new SqliteCommand(@"
INSERT INTO ThongTin
(ID,DiaDiem,Ngay,Thang,Nam,ChiHuyD,LoaiDeNghi,PTLoai1,PTLoai2,PTLoai3)
VALUES
(1,@DiaDiem,@Ngay,@Thang,@Nam,@ChiHuyD,@LoaiDeNghi,@PTLoai1,@PTLoai2,@PTLoai3)
ON CONFLICT(ID)
DO UPDATE SET
DiaDiem=@DiaDiem,
Ngay=@Ngay,
Thang=@Thang,
Nam=@Nam,
ChiHuyD=@ChiHuyD,
LoaiDeNghi=@LoaiDeNghi,
PTLoai1=@PTLoai1,
PTLoai2=@PTLoai2,
PTLoai3=@PTLoai3
", conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@DiaDiem", diaDiem);
                                    cmd.Parameters.AddWithValue("@Ngay", ngay);
                                    cmd.Parameters.AddWithValue("@Thang", thang);
                                    cmd.Parameters.AddWithValue("@Nam", nam);
                                    cmd.Parameters.AddWithValue("@ChiHuyD", chiHuyD);
                                    cmd.Parameters.AddWithValue("@LoaiDeNghi", deNghi);
                                    cmd.Parameters.AddWithValue("@PTLoai1", ptLoai1);
                                    cmd.Parameters.AddWithValue("@PTLoai2", ptLoai2);
                                    cmd.Parameters.AddWithValue("@PTLoai3", ptLoai3);

                                    // ⭐ NÂNG CẤP BẤT ĐỒNG BỘ: ExecuteNonQueryAsync
                                    await cmd.ExecuteNonQueryAsync();
                                }
                                await Progress_StepAsync(20);

                                // ================== 5. Commit transaction ==================
                                // ⭐ NÂNG CẤP BẤT ĐỒNG BỘ: CommitAsync
                                await tran.CommitAsync();
                            }
                            catch
                            {
                                // ⭐ NÂNG CẤP BẤT ĐỒNG BỘ: RollbackAsync
                                await tran.RollbackAsync();
                                throw;
                            }
                        }
                    }

                    // ================== 6. Sau khi lưu ==================
                    if (KiemTraCoDuLieuDanhSach())
                    {
                        await Bang2_Async();
                    }
                    else
                    {
                        // Nếu không có dữ liệu thì chủ động xóa trắng grid
                        kryptonDataGridView2.DataSource = null;
                    }
                    await Progress_StepAsync(20);
                    Module_ThongBao.ThanhCong("Đã lưu thông tin vào CSDL!");
                    Module_NhatKy.GhiNhatKy(
                        taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                        hanhDong: "Lưu thông tin khai báo thành công vào CSDL",
                        ghiChu: $"Thời gian: {SessionInfo.ThoiGianDangNhap:dd-MM-yyyy HH:mm:ss}"
                    );
                    await Progress_EndAsync();

                    // Cập nhật ListBox ngay lập tức
                    Module_ThongBao.CapNhatThongTin();
                }
                catch (Exception ex)
                {
                    await Progress_EndAsync();
                    Module_ThongBao.Loi("Lỗi khi lưu thông tin vào CSDL:\n" + ex.Message);
                }
                // =========================================================================
                // KẾT THÚC CODE GỐC
                // =========================================================================
            }
            finally
            {
                // =========================================================
                // LỚP VỎ UX: KHÔI PHỤC LẠI TRẠNG THÁI NÚT DÙ THÀNH CÔNG HAY THẤT BẠI
                // =========================================================
                kryptonButton_LuuThongTin.Values.Text = textBanDau;
                kryptonButton_LuuThongTin.Values.Image = anhBanDau;
                kryptonButton_LuuThongTin.Enabled = true;
            }
        }
        private bool KiemTraVaToMauDuongDan(Label lbl)
        {
            if (string.IsNullOrWhiteSpace(lbl.Text) || lbl.Text == "Chọn đường dẫn lưu")
            {
                lbl.ForeColor = Color.Red; // tô màu đỏ nếu chưa chọn
                return false;
            }
            lbl.ForeColor = Color.FromArgb(85, 107, 47); // xanh rêu nếu đã chọn
            return true;
        }
        private async void kryptonButton_XuatTrinhKy_Click(object sender, EventArgs e)
        {
            string textBanDau = kryptonButton_XuatTrinhKy.Values.Text;
            Image anhBanDau = kryptonButton_XuatTrinhKy.Values.Image;

            // THÊM: Gọi Form_Loading
            Form_Loading frmLoad = new Form_Loading("Đang tạo danh sách trình ký, vui lòng đợi...");

            try
            {
                kryptonButton_XuatTrinhKy.Enabled = false;
                kryptonButton_XuatTrinhKy.Values.Text = "Đang xử lý...";
                kryptonButton_XuatTrinhKy.Values.Image = null;
                await Task.Delay(100);

                // =========================================================================
                // BẮT ĐẦU CODE GỐC (Kiểm tra điều kiện trên luồng chính)
                // =========================================================================
                string duongDanLuu = label11.Text?.Trim();
                if (string.IsNullOrWhiteSpace(duongDanLuu) || duongDanLuu.Equals("Chọn đường dẫn lưu", StringComparison.OrdinalIgnoreCase))
                {
                    label11.ForeColor = Color.Red;
                    Module_ThongBao.DangXuLy("Chưa chọn đường dẫn xuất tệp!");
                    return;
                }
                if (duongDanLuu.Contains(": "))
                {
                    int pos = duongDanLuu.IndexOf(": ");
                    duongDanLuu = duongDanLuu.Substring(pos + 2).Trim();
                }

                if (string.IsNullOrWhiteSpace(duongDanLuu))
                {
                    label11.ForeColor = Color.Red;
                    Module_ThongBao.DangXuLy("Chưa chọn thư mục lưu!");
                    return;
                }

                if (!Directory.Exists(duongDanLuu))
                {
                    label11.ForeColor = Color.DarkOrange;
                    try
                    {
                        Directory.CreateDirectory(duongDanLuu);
                    }
                    catch
                    {
                        label11.ForeColor = Color.Red;
                        Module_ThongBao.Loi("Không thể tạo thư mục lưu!");
                        return;
                    }
                }

                label11.ForeColor = Color.DarkGreen;

                bool laTanBinh = false;
                try
                {
                    string phienBan = Module_TaiKhoan.LayPhienBanPhanMem();
                    if (!string.IsNullOrWhiteSpace(phienBan))
                    {
                        laTanBinh = phienBan.IndexOf("tân binh", StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                }
                catch
                {
                    laTanBinh = false;
                }

                bool xoaXinYKien = false;
                if (laTanBinh)
                {
                    xoaXinYKien = MessageBox.Show(
                        "Bạn có muốn xóa nội dung cột \"Xin ý kiến\" trước khi xuất không?",
                        "Xác nhận",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    ) == DialogResult.Yes;
                }

                // =========================================================================
                // THÊM: Hiện loading và đẩy code xử lý nặng xuống Task.Run
                // =========================================================================
                this.Enabled = false;
                frmLoad.Show(this);

                await Task.Run(() =>
                {
                    if (laTanBinh)
                    {
                        Module_XuatPhanLoai.XuatTrinhKyTanBinh(duongDanLuu, xoaXinYKien);
                    }
                    else
                    {
                        Module_XuatPhanLoai.XuatTrinhKyCBCS(duongDanLuu);
                    }
                });

                // Chạy tiếp phần code gốc sau khi xuất xong
                Module_NhatKy.GhiNhatKy(
                    taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                    hanhDong: laTanBinh
                        ? "Xuất danh sách trình ký Tân binh thành công!"
                        : "Xuất danh sách trình ký CBCS thành công!",
                    ghiChu: $"Thời gian: {SessionInfo.ThoiGianDangNhap:dd-MM-yyyy HH:mm:ss}"
                );
                // =========================================================================
                // KẾT THÚC CODE GỐC
                // =========================================================================
            }
            catch (Exception ex)
            {
                label11.ForeColor = Color.Red;
                Module_ThongBao.Loi("Lỗi khi xuất trình ký:\n" + ex.Message);
            }
            finally
            {
                // THÊM: Tắt loading
                frmLoad.Close();
                this.Enabled = true;
                this.Focus();

                kryptonButton_XuatTrinhKy.Values.Text = textBanDau;
                kryptonButton_XuatTrinhKy.Values.Image = anhBanDau;
                kryptonButton_XuatTrinhKy.Enabled = true;
            }
        }
        private async void kryptonButton_XuatTatCa_Click(object sender, EventArgs e)
        {
            string textBanDau = kryptonButton_XuatTatCa.Values.Text;
            Image anhBanDau = kryptonButton_XuatTatCa.Values.Image;

            // THÊM: Gọi Form_Loading
            Form_Loading frmLoad = new Form_Loading("Đang xuất toàn bộ dữ liệu Excel...");

            try
            {
                kryptonButton_XuatTatCa.Enabled = false;
                kryptonButton_XuatTatCa.Values.Text = "Đang xử lý...";
                kryptonButton_XuatTatCa.Values.Image = null;
                await Task.Delay(100);

                // =========================================================================
                // BẮT ĐẦU CODE GỐC
                // =========================================================================
                if (string.IsNullOrWhiteSpace(label11.Text) || label11.Text == "Chọn đường dẫn lưu")
                {
                    Module_ThongBao.DangXuLy("Bạn chưa chọn thư mục lưu!");
                    return;
                }

                string duongDanGoc = (label11.Text == "Chọn đường dẫn lưu") ? "" : label11.Text;
                if (string.IsNullOrWhiteSpace(duongDanGoc) || !Directory.Exists(duongDanGoc))
                {
                    Module_ThongBao.Loi("Đường dẫn lưu trong cài đặt không hợp lệ!");
                    return;
                }

                // =========================================================================
                // THÊM: Bật form loading, chạy ngầm phần tạo file
                // =========================================================================
                this.Enabled = false;
                frmLoad.Show(this);

                await Task.Run(() =>
                {
                    string thangHT = Module_XuatPhanLoai.LayThangHeThong();
                    string tenThuMuc = $"DANH SÁCH PHÂN LOẠI THI ĐUA THÁNG {thangHT} NĂM {DateTime.Now:yyyy}";
                    string fullThuMuc = Path.Combine(duongDanGoc, tenThuMuc);
                    Directory.CreateDirectory(fullThuMuc);

                    int sttFile = Directory.GetFiles(fullThuMuc, "*.xlsx").Length + 1;
                    string fileName = $"{sttFile}. DANH SÁCH TẤT CẢ PHÂN LOẠI - {DateTime.Now:yyyyMMdd-HHmmss}.xlsx";
                    string fileXuat = Path.Combine(fullThuMuc, fileName);
                    Module_XuatPhanLoai.XuatTatCaPhanLoai(fileXuat);
                    Module_XuatPhanLoai.LinkDanTep = fileXuat;
                    Module_XuatTongHop.XuatBaoCaoTongHop(fileXuat);

                    // ================= MỞ THƯ MỤC =================
                    try
                    {
                        string csdlPath = _csdl2Path;
                        bool moThuMuc = false;
                        using (var conn = new SqliteConnection($"Data Source={csdlPath}"))
                        {
                            conn.Open();
                            using var cmd = new SqliteCommand("SELECT Chex_MoiThuMucXuat FROM ThongTin WHERE ID = 1", conn);
                            object result = cmd.ExecuteScalar();

                            if (result != null && result != DBNull.Value)
                            {
                                string giaiMa = BaoMatAES.GiaiMa(result.ToString()).ToUpper();
                                moThuMuc = giaiMa == "TRUE";
                            }
                        }
                        if (moThuMuc && File.Exists(fileXuat))
                        {
                            this.Invoke(new Action(() => Module_XuatPhanLoai.MoThuMucVaChonTep(fileXuat)));
                        }
                    }
                    catch (Exception ex)
                    {
                        // Gọi Invoke nếu cần bắn thông báo lên Form từ Task.Run
                        this.Invoke(new Action(() => Module_ThongBao.Loi("Lỗi khi kiểm tra thư mục mở tự động:\n" + ex.Message)));
                    }

                    Module_NhatKy.GhiNhatKy(
                        taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                        hanhDong: "Xuất danh sách tất cả phân loại thành công!",
                        ghiChu: $"Thời gian: {SessionInfo.ThoiGianDangNhap:dd-MM-yyyy HH:mm:ss}"
                    );
                });
                // =========================================================================
                // KẾT THÚC CODE GỐC
                // =========================================================================
            }
            catch (Exception ex)
            {
                Module_ThongBao.Loi("Lỗi xuất dữ liệu tất cả phân loại:\n" + ex.Message);
            }
            finally
            {
                // THÊM: Đóng form loading
                frmLoad.Close();
                this.Enabled = true;
                this.Focus();

                kryptonButton_XuatTatCa.Values.Text = textBanDau;
                kryptonButton_XuatTatCa.Values.Image = anhBanDau;
                kryptonButton_XuatTatCa.Enabled = true;
            }
        }
        private async void kryptonButton_XuatDanhSachLoai_Click(object sender, EventArgs e)
        {
            string textBanDau = kryptonButton_XuatDanhSachLoai.Values.Text;
            Image anhBanDau = kryptonButton_XuatDanhSachLoai.Values.Image;

            // THÊM: Gọi form loading
            Form_Loading frmLoad = new Form_Loading("Đang tạo danh sách theo loại...");

            try
            {
                kryptonButton_XuatDanhSachLoai.Enabled = false;
                kryptonButton_XuatDanhSachLoai.Values.Text = "Đang xử lý...";
                kryptonButton_XuatDanhSachLoai.Values.Image = null;
                await Task.Delay(100);

                // =========================================================================
                // BẮT ĐẦU CODE GỐC
                // =========================================================================
                if (string.IsNullOrWhiteSpace(comboBox1_ChonLoaiDeXuat.Text))
                {
                    Module_ThongBao.DangXuLy("Bạn chưa chọn phân loại!");
                    comboBox1_ChonLoaiDeXuat.BackColor = Color.LightPink;
                    return;
                }
                else
                {
                    comboBox1_ChonLoaiDeXuat.BackColor = Color.LightGreen;
                }

                if (!KiemTraVaToMauDuongDan(label11))
                {
                    Module_ThongBao.DangXuLy("Bạn chưa chọn thư mục lưu!");
                    return;
                }

                string plChon = comboBox1_ChonLoaiDeXuat.Text.Trim();
                int sttFile = 1;

                // =========================================================================
                // THÊM: Bật Form loading và đưa phần xuất file vào Task.Run
                // =========================================================================
                this.Enabled = false;
                frmLoad.Show(this);

                await Task.Run(() =>
                {
                    Module_XuatPhanLoai.XuatPhanLoai(plChon, sttFile);

                    try
                    {
                        string csdlPath = _csdl2Path;
                        bool moThuMuc = false;
                        using (var conn = new SqliteConnection($"Data Source={csdlPath}"))
                        {
                            conn.Open();
                            using var cmd = new SqliteCommand("SELECT Chex_MoiThuMucXuat FROM ThongTin WHERE ID = 1", conn);
                            object result = cmd.ExecuteScalar();

                            if (result != null && result != DBNull.Value)
                            {
                                string giaiMa = BaoMatAES.GiaiMa(result.ToString()).ToUpper();
                                moThuMuc = giaiMa == "TRUE";
                            }
                        }
                        if (moThuMuc && !string.IsNullOrWhiteSpace(Module_XuatPhanLoai.LinkDanTep) && System.IO.File.Exists(Module_XuatPhanLoai.LinkDanTep))
                        {
                            this.Invoke(new Action(() => Module_XuatPhanLoai.MoThuMucVaChonTep(Module_XuatPhanLoai.LinkDanTep)));
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Invoke(new Action(() => Module_ThongBao.Loi("Lỗi khi kiểm tra và mở thư mục xuất:\n" + ex.Message)));
                    }
                    Module_NhatKy.GhiNhatKy(
                        taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                        hanhDong: "Xuất danh sách " + plChon + " thành công!",
                        ghiChu: $"Thời gian: {SessionInfo.ThoiGianDangNhap:dd-MM-yyyy HH:mm:ss}"
                    );
                });
                // =========================================================================
                // KẾT THÚC CODE GỐC
                // =========================================================================
            }
            catch (Exception ex)
            {
                Module_ThongBao.DangXuLy("Lỗi khi xuất dữ liệu:\n" + ex.Message);
            }
            finally
            {
                // THÊM: Tắt form Loading
                frmLoad.Close();
                this.Enabled = true;
                this.Focus();

                kryptonButton_XuatDanhSachLoai.Values.Text = textBanDau;
                kryptonButton_XuatDanhSachLoai.Values.Image = anhBanDau;
                kryptonButton_XuatDanhSachLoai.Enabled = true;
            }
        }
        private void kryptonButton_MoThuMuc_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. Kiểm tra lỏng: Lấy base path
                string basePath = label11.Text?.Trim();

                if (string.IsNullOrWhiteSpace(basePath) || basePath == "Chọn đường dẫn lưu")
                {
                    KiemTraVaToMauDuongDan(label11); // tô màu đỏ nếu chưa chọn
                    MessageBox.Show("Bạn chưa cấu hình thư mục lưu tệp xuất!", "Lưu ý", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 2. Dự đoán thư mục con (Trường hợp xuất Tất cả)
                string thangHT = Module_XuatPhanLoai.LayThangHeThong();
                string folderName = $"DANH SÁCH PHÂN LOẠI THI ĐUA THÁNG {thangHT} NĂM {DateTime.Now:yyyy}";
                string fullPath = Path.Combine(basePath, folderName);

                // 3. ĐỊNH TUYẾN THÔNG MINH (SMART ROUTING)
                string thuMucCanMo = "";

                if (Directory.Exists(fullPath))
                {
                    // Ưu tiên 1: Mở thư mục con (Nếu họ vừa xuất danh sách Tổng hợp)
                    thuMucCanMo = fullPath;
                }
                else if (Directory.Exists(basePath))
                {
                    // Ưu tiên 2: Mở thư mục gốc (Nếu họ vừa xuất Trình ký, Xuất lẻ, Xuất mẫu...)
                    thuMucCanMo = basePath;
                }
                else
                {
                    // Nếu cả 2 đều không có (Do user đã xóa folder ở ngoài Windows) -> Phải báo cho họ biết!
                    MessageBox.Show("Thư mục này không tồn tại hoặc đã bị xóa/di chuyển ra khỏi máy tính!",
                                    "Không tìm thấy thư mục", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 4. MỞ BẰNG TIẾN TRÌNH CHUẨN CỦA WINDOWS (Nhanh và không bị treo App)
                Process.Start(new ProcessStartInfo()
                {
                    FileName = thuMucCanMo,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                // Bắt lỗi hệ quyền (Access Denied) hoặc các lỗi win32 khác
                MessageBox.Show("Không thể mở thư mục. Lỗi hệ điều hành:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private string GiaiMaAnToan(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            try
            {
                string giaiMa = BaoMatAES.GiaiMa(raw);

                // 🔥 Fail-safe: nếu giải mã lỗi trả về rỗng → fallback về raw
                return string.IsNullOrWhiteSpace(giaiMa) ? raw.Trim() : giaiMa;
            }
            catch
            {
                // 🔥 Không bao giờ để crash UI
                return raw.Trim();
            }
        }
        private void AnIDGrid(DataGridView dgv)
        {
            foreach (DataGridViewColumn col in dgv.Columns)
                if (string.Equals(col.Name, "ID", StringComparison.OrdinalIgnoreCase))
                    col.Visible = false;
        }
        private async Task KiemTraVaDongBoCSDLAsync()
        {
            var csdls = new[]
            {
        ("CSDL1", Module_DanduongGPS.DuongDanCSDL1),
        ("CSDL2", Module_DanduongGPS.DuongDanCSDL2),
        ("CSDL3", Module_DanduongGPS.DuongDanCSDL3),
        ("CSDL4", Module_DanduongGPS.DuongDanCSDL4)
    };

            // Chuẩn hóa câu lệnh SQL, khai báo 1 lần duy nhất
            string sqlCreate = @"CREATE TABLE IF NOT EXISTS Token_XacDinhChinhChu (
                            ID INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                            STT TEXT,
                            Ma_ToKen TEXT,
                            Tai_Khoan_Cap_Nhat TEXT,
                            Thoi_Gian_Nap TEXT
                         );";

            // Sử dụng Tuple để gom nhóm nguyên 1 bản ghi, tránh râu ông nọ cắm cằm bà kia
            var thongTinCSDL = new Dictionary<string, (string Token, string TaiKhoan, string ThoiGian)>();
            var csdlTonTai = new List<string>();

            // BƯỚC 1: ĐỌC DỮ LIỆU (Chạy trên luồng nền để chống đơ UI)
            await Task.Run(() =>
            {
                foreach (var csdl in csdls)
                {
                    if (string.IsNullOrWhiteSpace(csdl.Item2) || !File.Exists(csdl.Item2)) continue;

                    try
                    {
                        using var conn = new SqliteConnection($"Data Source={csdl.Item2}");
                        conn.Open();

                        using var cmdCreate = new SqliteCommand(sqlCreate, conn);
                        cmdCreate.ExecuteNonQuery();

                        string sqlGet = "SELECT Ma_ToKen, Tai_Khoan_Cap_Nhat, Thoi_Gian_Nap FROM Token_XacDinhChinhChu WHERE ID=1";
                        using var cmdGet = new SqliteCommand(sqlGet, conn);
                        using var rd = cmdGet.ExecuteReader();

                        if (rd.Read())
                        {
                            string token = rd.IsDBNull(0) ? "" : BaoMatAES.GiaiMa(rd.GetString(0));
                            string tk = rd.IsDBNull(1) ? "" : BaoMatAES.GiaiMa(rd.GetString(1));
                            string tg = rd.IsDBNull(2) ? "" : rd.GetString(2);

                            // Lưu nguyên cụm 3 giá trị
                            thongTinCSDL[csdl.Item1] = (token, tk, tg);
                        }
                        csdlTonTai.Add(csdl.Item1);
                        // Từ khóa 'using' sẽ tự động đóng connection, không cần gọi conn.Close()
                    }
                    catch (Exception ex)
                    {
                        // Bắt buộc ghi log khi gặp sự cố đọc (VD: Database is locked)
                        Debug.WriteLine($"[Cảnh báo] Lỗi đọc {csdl.Item1}: {ex.Message}");
                    }
                }
            });

            if (csdlTonTai.Count == 0 || thongTinCSDL.Count == 0) return;

            // BƯỚC 2: XÁC ĐỊNH BẢN GHI CHUẨN
            var banGhiChuan = thongTinCSDL.Values
                .GroupBy(v => v) // Nhóm nguyên cụm bản ghi
                .OrderByDescending(g => g.Count()) // Ưu tiên số đông
                .ThenByDescending(g => g.Key.ThoiGian) // Tỷ số hòa -> Ưu tiên mốc thời gian mới nhất
                .FirstOrDefault()?.Key;

            if (banGhiChuan == null) return;

            // Giải nén tuple chuẩn
            var (chuanToken, chuanTK, chuanTG) = banGhiChuan.Value;
            var csdlDaBoSung = new HashSet<string>();

            // BƯỚC 3: CẬP NHẬT GHI ĐÈ (Chạy nền)
            await Task.Run(() =>
            {
                foreach (var kv in csdls)
                {
                    if (!csdlTonTai.Contains(kv.Item1)) continue;

                    // Kiểm tra tính toàn vẹn: Chỉ cập nhật nếu thiếu hoặc sai lệch với bản chuẩn
                    bool canUpdate = !thongTinCSDL.ContainsKey(kv.Item1) ||
                                     thongTinCSDL[kv.Item1].Token != chuanToken ||
                                     thongTinCSDL[kv.Item1].TaiKhoan != chuanTK ||
                                     thongTinCSDL[kv.Item1].ThoiGian != chuanTG;

                    if (canUpdate)
                    {
                        try
                        {
                            using var conn = new SqliteConnection($"Data Source={kv.Item2}");
                            conn.Open();

                            string sqlUpsert = @"INSERT OR REPLACE INTO Token_XacDinhChinhChu 
                                         (ID, STT, Ma_ToKen, Tai_Khoan_Cap_Nhat, Thoi_Gian_Nap)
                                         VALUES (1, 'STT1', @token, @tk, @tg)";

                            using var cmdUpsert = new SqliteCommand(sqlUpsert, conn);
                            cmdUpsert.Parameters.AddWithValue("@token", string.IsNullOrEmpty(chuanToken) ? "" : BaoMatAES.MaHoa(chuanToken));
                            cmdUpsert.Parameters.AddWithValue("@tk", string.IsNullOrEmpty(chuanTK) ? "" : BaoMatAES.MaHoa(chuanTK));
                            cmdUpsert.Parameters.AddWithValue("@tg", chuanTG);

                            cmdUpsert.ExecuteNonQuery();
                            csdlDaBoSung.Add(kv.Item1);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[Cảnh báo] Lỗi ghi đè {kv.Item1}: {ex.Message}");
                        }
                    }
                }
            });

            // BƯỚC 4: THÔNG BÁO VÀ GHI NHẬT KÝ
            if (csdlDaBoSung.Count > 0)
            {
                string thongTinFiles = "";
                foreach (var kv in csdls)
                {
                    if (!csdlDaBoSung.Contains(kv.Item1)) continue;
                    try
                    {
                        var fi = new FileInfo(kv.Item2);
                        thongTinFiles += $"CSDL: {kv.Item1}\n" +
                                         $"Kích thước: {fi.Length:N0} bytes\n" +
                                         $"Ngày tạo: {fi.CreationTime:dd/MM/yyyy HH:mm:ss}\n" +
                                         $"Sửa gần nhất: {fi.LastWriteTime:dd/MM/yyyy HH:mm:ss}\n\n";
                    }
                    // CODE CHUẨN
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Cảnh báo] Lỗi tại: {ex.Message}");
                        // Bắt buộc tích hợp hàm GhiNhatKy() hoặc báo lỗi ra UI
                    }
                }

                string danhSachLoi = string.Join(", ", csdlDaBoSung);
                string msg = $"Phát hiện CSDL bị thiếu hoặc sai lệch dữ liệu định danh ({danhSachLoi})!\n" +
                             "Hệ thống đã tự động khôi phục sự đồng nhất từ dữ liệu chuẩn.\n\n" +
                             "Thông tin chi tiết CSDL được khôi phục:\n" + thongTinFiles;

                // Tối ưu hóa nội dung ghi nhật ký, không nên lưu chuỗi quá dài gây phình Database
                Module_NhatKy.GhiNhatKy(
                    taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                    hanhDong: $"Đồng bộ Token tự động ({danhSachLoi})",
                    ghiChu: $"Khôi phục về bản ghi của: {chuanTK} lúc {chuanTG}"
                );

                // Icon Information hợp lý hơn Warning vì phần mềm đã TỰ ĐỘNG KHÔI PHỤC thành công
                MessageBox.Show(msg, "Đồng bộ Cơ sở dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void kryptonButton_ChonDuongDanLuu_Click(object? sender, EventArgs e)
        {
            try
            {
                string csdl2Path = _csdl2Path;
                if (string.IsNullOrWhiteSpace(csdl2Path) || !File.Exists(csdl2Path))
                {
                    Module_ThongBao.DangXuLy("CSDL phụ không tồn tại.");
                    return;
                }

                string duongDanCu = Module_XuatPhanLoai.GetLinkLuuDuongDanTepXuat(true);

                using var fbd = new FolderBrowserDialog
                {
                    Description = "Chọn thư mục để lưu file Excel",
                    ShowNewFolderButton = true,
                    SelectedPath = !string.IsNullOrWhiteSpace(duongDanCu) && Directory.Exists(duongDanCu)
                        ? duongDanCu
                        : Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                if (fbd.ShowDialog() != DialogResult.OK)
                    return;   // Cancel → thoát luôn

                string duongDanMoi = fbd.SelectedPath;

                if (!Directory.Exists(duongDanMoi))
                {
                    Module_ThongBao.DangXuLy("Thư mục không hợp lệ.");
                    return;
                }

                using var conn = new SqliteConnection($"Data Source={csdl2Path}");
                conn.Open();

                using var tran = conn.BeginTransaction();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText =
                        @"INSERT INTO ThongTin (ID, ChonDuongDanXuatTep)
                  VALUES (1, @path)
                  ON CONFLICT(ID)
                  DO UPDATE SET ChonDuongDanXuatTep = excluded.ChonDuongDanXuatTep;";

                    cmd.Parameters.AddWithValue("@path", BaoMatAES.MaHoa(duongDanMoi));
                    cmd.ExecuteNonQuery();
                }

                tran.Commit();

                // Update UI
                label11.Text = duongDanMoi;
                label11.ForeColor = Color.FromArgb(85, 107, 47);

                Module_ThongBao.ThanhCong("Đã lưu thư mục thành công.");
            }
            catch (Exception ex)
            {
                Module_ThongBao.DangXuLy("Lỗi: " + ex.Message);
            }
        }
        private void HienThiDuongDanXuatDaChon()
        {
            string duongDanGoc = string.Empty;
            try
            {
                using (var conn = new SqliteConnection($"Data Source={_csdl2Path}"))
                {
                    {
                        conn.Open();

                        string sql = @"
                SELECT ChonDuongDanXuatTep
                FROM ThongTin
                WHERE ID = 1";

                        using (var cmd = new SqliteCommand(sql, conn))
                        {
                            object? result = cmd.ExecuteScalar();

                            if (result != null && result != DBNull.Value)
                            {
                                string chuoiMaHoa = result.ToString()!;
                                duongDanGoc = BaoMatAES.GiaiMa(chuoiMaHoa); // 🔑 GIẢI MÃ
                            }
                        }
                    }
                }
            }
            catch
            {
                duongDanGoc = string.Empty;
            }

            // ===== CẬP NHẬT LABEL =====
            if (!string.IsNullOrWhiteSpace(duongDanGoc) &&
                Directory.Exists(duongDanGoc))
            {
                label11.Text = duongDanGoc;
                label11.ForeColor = Color.DarkGreen;
            }
            else
            {
                label11.Text = "Chưa chọn đường dẫn xuất tệp";
                label11.ForeColor = Color.Gray;
            }
        }
        private string _textGocNutKiemTra = null;
        private Image _anhGocNutKiemTra = null;
        private void kryptonButton_KiemTraTLvaQS_Click(object sender, EventArgs e)
        {
            // 1. Lưu lại Text và Icon gốc ở lần bấm đầu tiên (tránh lưu nhầm chữ "Đang xử lý...")
            if (_textGocNutKiemTra == null)
            {
                _textGocNutKiemTra = kryptonButton_KiemTraTLvaQS.Values.Text;
                _anhGocNutKiemTra = kryptonButton_KiemTraTLvaQS.Values.Image;
            }

            // 2. Đổi giao diện nút thành "Đang xử lý..."
            kryptonButton_KiemTraTLvaQS.Values.Text = "Đang xử lý...";
            // kryptonButton_KiemTraTLvaQS.Values.Image = null; // Mở comment dòng này nếu bạn muốn tạm ẩn icon lúc đang xử lý

            // 3. Khởi tạo Form 11 nếu chưa có
            if (form11 == null || form11.IsDisposed)
            {
                form11 = new Form11_KiemTraTyLe
                {
                    Owner = this,
                    ShowInTaskbar = false
                };

                // 🌟 BÍ QUYẾT: Đăng ký sự kiện khi Form 11 ĐÓNG thì trả lại tên cũ
                form11.FormClosed += (s, ev) =>
                {
                    // Kiểm tra an toàn xem Form 4 (form hiện tại) còn sống không
                    if (!this.IsDisposed && this.IsHandleCreated)
                    {
                        kryptonButton_KiemTraTLvaQS.Values.Text = _textGocNutKiemTra;
                        kryptonButton_KiemTraTLvaQS.Values.Image = _anhGocNutKiemTra;
                    }
                };
            }

            // 4. Hiển thị và đưa Form 11 lên trên cùng
            if (!form11.Visible)
            {
                form11.Show();
            }

            form11.Activate();
        }
        private void KiemTraDuLieuDanhSachVaKhoaNut()
        {
            bool coDuLieu = false;
            string dbPath = _csdl2Path;

            try
            {
                if (!string.IsNullOrWhiteSpace(dbPath) && File.Exists(dbPath))
                {
                    using (var conn = new SqliteConnection($"Data Source={dbPath}"))
                    {
                        conn.Open();

                        // Kiểm tra xem bảng DanhSach có tồn tại và có dữ liệu không
                        string sql = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='DanhSach';";
                        using (var cmdCheckTable = new SqliteCommand(sql, conn))
                        {
                            long tableExists = (long)cmdCheckTable.ExecuteScalar();

                            if (tableExists > 0)
                            {
                                using (var cmdCount = new SqliteCommand("SELECT COUNT(1) FROM DanhSach", conn))
                                {
                                    long rowCount = (long)cmdCount.ExecuteScalar();
                                    coDuLieu = rowCount > 0;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi khi kiểm tra dữ liệu DanhSach: " + ex.Message);
            }

            // ===== BẬT/TẮT CÁC NÚT XUẤT =====
            kryptonButton_XuatTrinhKy.Enabled = coDuLieu;
            kryptonButton_XuatTatCa.Enabled = coDuLieu;
            kryptonButton_XuatDanhSachLoai.Enabled = coDuLieu;

            // Đổi màu chữ nhẹ để người dùng dễ nhận biết nút đang bị khóa
            Color mauChuKhoa = Color.Gray;
            Color mauChuMo = Color.Black; // Hoặc màu mặc định của bạn

            kryptonButton_XuatTrinhKy.StateCommon.Content.ShortText.Color1 = coDuLieu ? mauChuMo : mauChuKhoa;
            kryptonButton_XuatTatCa.StateCommon.Content.ShortText.Color1 = coDuLieu ? mauChuMo : mauChuKhoa;
            kryptonButton_XuatDanhSachLoai.StateCommon.Content.ShortText.Color1 = coDuLieu ? mauChuMo : mauChuKhoa;

            // Thông báo nếu không có dữ liệu
            if (!coDuLieu)
            {
                // Gọi hàm ThongBao có sẵn trong Form4 của bạn
                ThongBao("Chưa có dữ liệu danh sách. Các tính năng xuất tệp bị vô hiệu hóa.");
            }
        }
        public void CapNhatDanhSachPhanLoaiDeXuat()
        {
            // Đảm bảo an toàn luồng (nếu Form 6 gọi hàm này từ Task ngầm)
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(CapNhatDanhSachPhanLoaiDeXuat));
                return;
            }

            // Giữ lại text user đang chọn dở để không làm họ bực mình bị mất lựa chọn
            string selectedGoc = comboBox1_ChonLoaiDeXuat.Text;

            try
            {
                comboBox1_ChonLoaiDeXuat.BeginUpdate();
                comboBox1_ChonLoaiDeXuat.Items.Clear();

                // ========================================================================
                // ⭐ LẤY DANH SÁCH ĐÃ ĐƯỢC LỌC VÀ SẮP XẾP CHUẨN (Loại 1 -> Loại 4 -> Không PL) TỪ FORM 6
                // ========================================================================
                List<string> danhSachDaLoc = Form6_XuLyData.LayDanhSachPhanLoaiThucTe();

                // Nạp vào ComboBox
                foreach (var item in danhSachDaLoc)
                {
                    comboBox1_ChonLoaiDeXuat.Items.Add(item);
                }

                // Phục hồi lại giá trị cũ đang chọn (Nếu giá trị đó vẫn còn tồn tại trong list mới)
                if (!string.IsNullOrWhiteSpace(selectedGoc) && comboBox1_ChonLoaiDeXuat.Items.Contains(selectedGoc))
                {
                    comboBox1_ChonLoaiDeXuat.Text = selectedGoc;
                }
                else if (comboBox1_ChonLoaiDeXuat.Items.Count > 0)
                {
                    // Nếu giá trị cũ bị xóa mất, tự động lùi về Item đầu tiên (thường là Loại 1)
                    comboBox1_ChonLoaiDeXuat.SelectedIndex = 0;
                }
                else
                {
                    comboBox1_ChonLoaiDeXuat.Text = ""; // Bỏ trống nếu DB không có ai bị xếp loại nào
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi nạp combobox Loại đề xuất từ Form 6: " + ex.Message);
            }
            finally
            {
                comboBox1_ChonLoaiDeXuat.EndUpdate();
            }
        }
        private bool KiemTraCoDuLieuDanhSach()
        {
            string dbPath = _csdl2Path;
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return false;

            try
            {
                // Chỉ mở ReadOnly để quét cực nhanh
                using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;Pooling=True;");
                conn.Open();

                // Trả về 1 nếu có ít nhất 1 dòng, trả về 0 nếu bảng trống
                using var cmd = new SqliteCommand("SELECT EXISTS(SELECT 1 FROM DanhSach LIMIT 1);", conn);
                return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi kiểm tra Data: " + ex.Message);
                return false;
            }
        }
        private void Form4_FormClosing(object sender, FormClosingEventArgs e)
        {
            Module_DanduongGPS.OnDatabaseChanged -= SuKien_DatabaseChanged;

            // 🔥 Dọn rác Trạm gác
            _uiDecryptCache.Clear();

            // Giải phóng GDI+
            _appIcon?.Dispose();
            _iconTrue?.Dispose();
            _iconFalse?.Dispose();
            _cachedGridFont?.Dispose();
            _cachedGridFontBold?.Dispose();
            _cachedGrid2HeaderFont?.Dispose(); // Thêm dòng này
            if (formTinhToan != null && !formTinhToan.IsDisposed) formTinhToan.Dispose();
            if (form11 != null && !form11.IsDisposed) form11.Dispose();
        }
    }
}
