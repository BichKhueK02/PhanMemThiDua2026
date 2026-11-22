///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form51_QuanLyKhenThuongTapTheNamCu : Form
    {
        // BIẾN QUAN TRỌNG: Lưu đường dẫn CSDL năm cũ hiện tại đang được chọn
        private string _currentDbPath = string.Empty;
        // Biến lưu trạng thái
        private int _selectedID = -1;
        private bool isEditing = false;
        private System.Threading.CancellationTokenSource _searchCts = null!;
        private int _msgCounter = 0;
        // ⭐ KHAI BÁO CÁC BIẾN BỊ THIẾU TẠI ĐÂY (PHẢI NẰM NGOÀI HÀM)
        private bool _isInitialized = false;
        // Quản lý Font tập trung dùng biến hệ thống, chống rò rỉ GDI handle
        private static readonly Font _fontGridHeader9Bold = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Bold);
        private static readonly Font _fontGridCell9Regular = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Regular);
        // BỘ ĐỆM TOÀN BỘ BẢNG TẬP THỂ CỦA TỆP NĂM ĐANG CHỌN (đã giải mã, 1 lần / tệp). Lọc chỉ chạy trên RAM.
        // Bản cũ chỉ đệm 4 cột rồi MỖI lần lọc lại truy vấn + giải mã CSDL theo danh sách ID, và cắt ở 500 dòng (Take(500))
        // => dữ liệu nhiều thì vừa chậm vừa thiếu dòng mà không báo.
        private sealed class DuLieuCache
        {
            public int ID, STT;
            public long TienThuong;
            public string TenTapThe = "", DonVi = "", HinhThucKhenThuong = "";
            public string SoQuyetDinh = "", NgayQuyetDinh = "", NguoiKy = "", NoiDung = "", NgayCapPhat = "",
                          CanBoCapPhat = "", NguoiDaiDienNhan = "", GhiChu = "";
        }
        private List<DuLieuCache>? _globalCache = null;
        private string _dauVetDaTai = "";                   // dấu vết tệp lúc đọc bộ đệm: tệp đổi (Form34 ghi đè, Form50 xóa...) thì đọc lại
        private Task<List<DuLieuCache>>? _dangDocCache;      // gộp các lần đọc chồng nhau của cùng 1 tệp
        private string _duongDanDangDoc = "";
        private bool _dangNapCombo;                         // đang nạp lại combo lọc -> bỏ qua TextChanged
        private List<int>? _idHienThi;                      // tập ID đang hiển thị: lọc ra đúng tập cũ thì không gắn lại lưới
        public Form51_QuanLyKhenThuongTapTheNamCu()
        {
            InitializeComponent();
            Module_HeThong.BocBoTronGiaoDien(richTextBox1_NoiDungKhenThuong);
            Module_GiaoDienForm51Web.ApDung(this);
            // Tệp năm cũ bị xóa / ghi đè ở Form34 / Form50: danh sách năm + lưới ở đây khớp theo
            Module_BoLocKhenThuong.KhoNamCuThayDoi += KhiKhoNamCuThayDoi;
            VisibleChanged += (_, _) => { if (Visible && _isInitialized) _ = DongBoDanhSachNamAsync(); };
            Disposed += (_, _) => Module_BoLocKhenThuong.KhoNamCuThayDoi -= KhiKhoNamCuThayDoi;
        }
        private void KhiKhoNamCuThayDoi(string duongDan)
        {
            if (IsDisposed || !_isInitialized) return;
            _dauVetDaTai = "";                              // ép đọc lại
            if (Visible) _ = DongBoDanhSachNamAsync();      // đang ẩn: lần hiện sau tự quét (VisibleChanged)
        }
        private async void Form51_QuanLyKhenThuongTapTheNamCu_Load(object? sender, EventArgs e)
                {
                    if (_isInitialized) return;
                    _isInitialized = true;
                    try
                    {
                        // 1. KHỞI TẠO MODULE / DEPENDENCY
                        Module_DonVi.KhoiTao();
                        // 2. KHỞI TẠO DATAGRIDVIEW
                        CauHinhGridCoBan(kryptonDataGridView1);
                        CauHinhStyleWeb(kryptonDataGridView1);
                        CauHinhCotGrid(kryptonDataGridView1);
                        // 3. KHỞI TẠO TRẠNG THÁI CONTROL
                        if (kryptonTextBox_STT != null)
                        {
                            kryptonTextBox_STT.ReadOnly = true;
                            kryptonTextBox_STT.StateCommon.Back.Color1 = Color.LightGreen;
                        }
                        // 4. ĐĂNG KÝ EVENT DATAGRIDVIEW
                        if (kryptonDataGridView1 != null)
                        {
                            kryptonDataGridView1.CellClick -= kryptonDataGridView1_CellClick;
                            kryptonDataGridView1.CellClick += kryptonDataGridView1_CellClick;
                        }
                        // 5. ĐĂNG KÝ EVENT CÁC CONTROL NHẬP LIỆU
                        if (kryptonTextBox_TienThuong != null)
                        {
                            kryptonTextBox_TienThuong.TextChanged -= kryptonTextBox_TienThuong_TextChanged;
                            kryptonTextBox_TienThuong.TextChanged += kryptonTextBox_TienThuong_TextChanged;
                        }
                        if (combobox1_TimKiemTheoTen != null)
                        {
                            combobox1_TimKiemTheoTen.TextChanged -= combobox1_TimKiemTheoTen_TextChanged;
                            combobox1_TimKiemTheoTen.TextChanged += combobox1_TimKiemTheoTen_TextChanged;
                        }
                        if (comboBox_TimKiemDonViKhenThuong != null)
                        {
                            comboBox_TimKiemDonViKhenThuong.TextChanged -= comboBox_TimKiemDonViKhenThuong_TextChanged;
                            comboBox_TimKiemDonViKhenThuong.TextChanged += comboBox_TimKiemDonViKhenThuong_TextChanged;
                        }
                        if (comboBox1_HinhThucKT != null)
                        {
                            comboBox1_HinhThucKT.TextChanged -= comboBox1_HinhThucKT_TextChanged;
                            comboBox1_HinhThucKT.TextChanged += comboBox1_HinhThucKT_TextChanged;
                        }
                        if (richTextBox1_NoiDungKhenThuong != null)
                        {
                            richTextBox1_NoiDungKhenThuong.TextChanged -= richTextBox1_NoiDungKhenThuong_TextChanged;
                            richTextBox1_NoiDungKhenThuong.TextChanged += richTextBox1_NoiDungKhenThuong_TextChanged;
                        }
                        if (comboBox1_ChonCscdKhenThuongTapTheNamCu != null)
                        {
                            comboBox1_ChonCscdKhenThuongTapTheNamCu.SelectedIndexChanged -= ComboBox1_ChonCscdKhenThuongTapTheNamCu_SelectedIndexChanged;
                            comboBox1_ChonCscdKhenThuongTapTheNamCu.SelectedIndexChanged += ComboBox1_ChonCscdKhenThuongTapTheNamCu_SelectedIndexChanged;
                        }
                        // 6. ĐƯA FORM VỀ TRẠNG THÁI BAN ĐẦU
                        ResetInput();
                        KiemTraHienThiNutCoChu();
                        // 7 + 8. NẠP DANH SÁCH NĂM (cùng nguồn Form50) + DỮ LIỆU NĂM ĐANG CHỌN
                        await DongBoDanhSachNamAsync();
                        // 9. CẬP NHẬT TRẠNG THÁI CÁC NÚT
                        CapNhatTrangThaiNutThaoTac();
                        // 10. KHỞI TẠO TOOLTIP
                        InitToolTips();
                        Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1);
            }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Lỗi Form51_Load: {ex.Message}");
                        _isInitialized = false;
                    }
                }
        // Hàm tự động ẩn/hiện nút chỉnh cỡ chữ theo nội dung RichTextBox
        private void KiemTraHienThiNutCoChu()
        {
            var richTextBox = richTextBox1_NoiDungKhenThuong;
            if (richTextBox == null ||
                richTextBox.IsDisposed ||
                richTextBox.Disposing)
            {
                return;
            }
            bool coNoiDung = !string.IsNullOrWhiteSpace(richTextBox.Text);
            if (kryptonButton2_TangCoChuRichText != null &&
                !kryptonButton2_TangCoChuRichText.IsDisposed &&
                !kryptonButton2_TangCoChuRichText.Disposing)
            {
                kryptonButton2_TangCoChuRichText.Visible = coNoiDung;
            }
            if (kryptonButton2_GiamCoChuRichText != null &&
                !kryptonButton2_GiamCoChuRichText.IsDisposed &&
                !kryptonButton2_GiamCoChuRichText.Disposing)
            {
                kryptonButton2_GiamCoChuRichText.Visible = coNoiDung;
            }
        }
        private void CapNhatTrangThaiNutThaoTac()
        {
            if (kryptonDataGridView1 == null ||
                kryptonDataGridView1.IsDisposed ||
                kryptonDataGridView1.Disposing)
            {
                return;
            }
            bool coDuLieu = kryptonDataGridView1.Rows.Count > 0;
            if (kryptonButton_SuaVaLuuKhenThuong != null &&
                !kryptonButton_SuaVaLuuKhenThuong.IsDisposed &&
                !kryptonButton_SuaVaLuuKhenThuong.Disposing)
            {
                kryptonButton_SuaVaLuuKhenThuong.Visible = coDuLieu;
            }
            if (kryptonButton_XoaKhenThuong != null &&
                !kryptonButton_XoaKhenThuong.IsDisposed &&
                !kryptonButton_XoaKhenThuong.Disposing)
            {
                kryptonButton_XoaKhenThuong.Visible = coDuLieu;
            }
        }
        private void richTextBox1_NoiDungKhenThuong_TextChanged(object? sender, EventArgs e)
        {
            KiemTraHienThiNutCoChu();
        }
        public void LoadDanhSachFileLichSu() => _ = DongBoDanhSachNamAsync();
        /// <summary>
        /// Quét lại danh sách năm — CÙNG nguồn, cùng thứ tự với Form50 (Module_HoTroLuuDataTheoNamCu; bản Tân binh: không có năm nào) —
        /// giữ năm đang chọn nếu tệp còn. Tệp đang xem biến mất thì chuyển sang năm khác / dọn lưới; tệp còn nhưng đã bị ghi đè thì đọc lại.
        /// Sửa lỗi cũ: gọi lần 2 là Items.Clear() trên combo đã gắn DataSource => ném lỗi, danh sách năm không bao giờ được làm mới.
        /// </summary>
        private async Task DongBoDanhSachNamAsync()
        {
            var cb = comboBox1_ChonCscdKhenThuongTapTheNamCu;
            if (cb == null || IsDisposed) return;
            // Chỉ liệt kê năm CÒN dữ liệu khen thưởng tập thể: xóa hết bảng của năm nào thì năm đó biến khỏi combo (Form50 làm
            // tương tự với bảng giấy khen cá nhân).
            var danhSach = Module_HoTroLuuDataTheoNamCu.LayDanhSachFileLichSu_KhenThuongCaNhan()
                .Where(x => Module_BoLocKhenThuong.CoDuLieuBang(x.DuongDan, "ThongKe_KhenThuongTapThe"))
                .ToList();
            cb.SelectedIndexChanged -= ComboBox1_ChonCscdKhenThuongTapTheNamCu_SelectedIndexChanged;
            try
            {
                cb.DisplayMember = "TenHienThi";
                cb.ValueMember = "DuongDan";
                cb.DataSource = danhSach;
                int idx = danhSach.FindIndex(x => string.Equals(x.DuongDan, _currentDbPath, StringComparison.OrdinalIgnoreCase));
                if (idx < 0) idx = danhSach.Count > 0 ? 0 : -1;
                cb.SelectedIndex = idx;
            }
            finally
            {
                cb.SelectedIndexChanged += ComboBox1_ChonCscdKhenThuongTapTheNamCu_SelectedIndexChanged;
            }
            string chon = (cb.SelectedItem as FileLichSuDTO)?.DuongDan ?? "";
            if (!string.Equals(chon, _currentDbPath, StringComparison.OrdinalIgnoreCase))
                await MoTepNamAsync(chon);
            else if (chon.Length > 0 && !string.Equals(Module_BoLocKhenThuong.DauVetTep(chon), _dauVetDaTai, StringComparison.Ordinal))
            {
                _globalCache = null;                         // tệp đã bị ghi đè / sửa ở nơi khác
                await LoadDataToGridAsync();
            }
            CapNhatTrangThaiNutThaoTac();
        }
        private async void ComboBox1_ChonCscdKhenThuongTapTheNamCu_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string chon = (comboBox1_ChonCscdKhenThuongTapTheNamCu.SelectedItem as FileLichSuDTO)?.DuongDan ?? "";
            await MoTepNamAsync(chon);
        }
        private async Task MoTepNamAsync(string duongDan)
        {
            _currentDbPath = duongDan ?? "";
            _globalCache = null;
            _dauVetDaTai = "";
            _idHienThi = null;
            ResetInput();
            if (_currentDbPath.Length == 0 || !File.Exists(_currentDbPath))
            {
                kryptonDataGridView1.DataSource = CreateDataTableSchema();
                await LoadDataToGridAsync();                 // combo lọc về rỗng + nhãn tổng số
                return;
            }
            KiemTraVaTaoBangThongKeKhenThuongTapThe();
            await LoadDataToGridAsync();
        }
        // KHU VỰC TOOLTIP ĐÃ ĐƯỢC CHUẨN HÓA
        private void InitToolTips()
        {
            if (toolTip1 == null) return;
            try
            {
                toolTip1.IsBalloon = true;
                toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
                toolTip1.ToolTipIcon = ToolTipIcon.Info;
                toolTip1.InitialDelay = 300;
                toolTip1.AutoPopDelay = 2500;
                toolTip1.ReshowDelay = 100;
                toolTip1.ShowAlways = true;
                GanToolTipAnToan(kryptonButton_LamMoiCacOTimKiem, "Làm mới (xóa) bộ lọc tìm kiếm hiện tại");
                GanToolTipAnToan(kryptonButton2_GiamCoChuRichText, "Giảm cỡ chữ nội dung đang hiển thị");
                GanToolTipAnToan(kryptonButton2_TangCoChuRichText, "Tăng cỡ chữ nội dung đang hiển thị");
                GanToolTipAnToan(kryptonButton_ThemKhenThuong, "Thêm thông tin khen thưởng mới");
                GanToolTipAnToan(kryptonButton_SuaVaLuuKhenThuong, "Sửa và lưu thông tin khen thưởng");
                GanToolTipAnToan(kryptonButton_XoaKhenThuong, "Xóa thông tin khen thưởng đang chọn");
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }
        private void GanToolTipAnToan(Control control, string noiDung)
        {
            if (control == null || control.IsDisposed || control.Disposing || string.IsNullOrWhiteSpace(noiDung) || toolTip1 == null) return;
            try { toolTip1.SetToolTip(control, noiDung); }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }
        // HÀM BẢO VỆ GIAO DIỆN & CACHE
        /// <summary>
        /// Đọc + giải mã TOÀN BỘ bảng tập thể của tệp năm đang chọn vào RAM (luồng nền, 1 lần / tệp). Các lần gọi chồng nhau cho cùng
        /// tệp dùng chung 1 lượt đọc. Kết quả về muộn của tệp cũ (đã chọn năm khác) thì bỏ.
        /// </summary>
        private async Task BuildGlobalCacheAsync(bool forceReload = false)
        {
            string duongDan = _currentDbPath;
            if (string.IsNullOrEmpty(duongDan) || !File.Exists(duongDan)) { _globalCache = null; return; }
            if (forceReload) _globalCache = null;
            if (_globalCache != null) return;
            Task<List<DuLieuCache>> viec;
            if (!forceReload && _dangDocCache != null && string.Equals(_duongDanDangDoc, duongDan, StringComparison.OrdinalIgnoreCase))
                viec = _dangDocCache;
            else
            {
                string chuoiKetNoi = Module_KetNoiCSDL.ChuoiKetNoi(duongDan, chiDoc: true);
                viec = Task.Run(() => DocToanBoBang(chuoiKetNoi));
                _dangDocCache = viec;
                _duongDanDangDoc = duongDan;
            }
            List<DuLieuCache> ds;
            try { ds = await viec; }
            finally { if (ReferenceEquals(_dangDocCache, viec)) _dangDocCache = null; }
            if (!string.Equals(duongDan, _currentDbPath, StringComparison.OrdinalIgnoreCase) || IsDisposed) return;
            _globalCache = ds;
            _dauVetDaTai = Module_BoLocKhenThuong.DauVetTep(duongDan);
        }
        // Chạy ở LUỒNG NỀN: không đụng control.
        private List<DuLieuCache> DocToanBoBang(string chuoiKetNoi)
        {
            var ds = new List<DuLieuCache>();
            using var conn = new SqliteConnection(chuoiKetNoi);
            conn.Open();
            using (var kt = new SqliteCommand("SELECT 1 FROM sqlite_master WHERE type='table' AND name='ThongKe_KhenThuongTapThe' LIMIT 1", conn))
                if (kt.ExecuteScalar() == null) return ds;
            const string sql = @"SELECT ID, STT, TenTapThe, HinhThuc_KhenThuong, DonVi_CapKhenThuong, SoQuyetDinh, NgayQuyetDinh,
                                        NguoiKy, NoiDung_KhenThuong, TienThuong, NgayCapPhat, CanBoCapPhat, NguoiDaiDienNhan, GhiChu
                                 FROM ThongKe_KhenThuongTapThe ORDER BY STT ASC, ID ASC";
            using var cmd = new SqliteCommand(sql, conn);
            using var rd = cmd.ExecuteReader();
            string S(int i) => rd.IsDBNull(i) ? "" : SafeDecrypt(rd.GetValue(i));
            while (rd.Read())
            {
                string tien = S(9);
                long.TryParse(tien.Replace(".", string.Empty).Replace(",", string.Empty), out long soTien);
                ds.Add(new DuLieuCache
                {
                    ID = rd.IsDBNull(0) ? 0 : Convert.ToInt32(rd.GetValue(0)),
                    STT = rd.IsDBNull(1) ? 0 : Convert.ToInt32(rd.GetValue(1)),
                    TenTapThe = S(2), HinhThucKhenThuong = S(3), DonVi = S(4),
                    SoQuyetDinh = S(5), NgayQuyetDinh = S(6), NguoiKy = S(7), NoiDung = S(8),
                    TienThuong = soTien, NgayCapPhat = S(10), CanBoCapPhat = S(11), NguoiDaiDienNhan = S(12), GhiChu = S(13)
                });
            }
            return ds;
        }
        private string SafeDecrypt(object? value)
        {
            if (value == null || value == DBNull.Value) return "";
            string s = value.ToString()?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(s)) return "";
            try
            {
                string decrypted = Module_BaoMatAES.GiaiMa(s);
                return string.IsNullOrEmpty(decrypted) ? "[Lỗi giải mã]" : decrypted;
            }
            catch
            {
                return "[Lỗi dữ liệu]";
            }
        }
        private string SafeEncrypt(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            return Module_BaoMatAES.MaHoa(input.Trim());
        }
        // LOAD DỮ LIỆU BỘ LỌC: danh sách 3 combo lọc do LoadDataToGridAsync dựng theo BỘ LỌC KÉP (mỗi combo chỉ còn giá trị có trong
        // các dòng thỏa 2 bộ lọc kia). Giữ 3 hàm cũ (nơi gọi không đổi): chỉ còn việc bảo đảm bộ đệm đã nạp / nạp lại.
        private Task LoadComboBoxDonViAsync(bool forceReload = false) => BuildGlobalCacheAsync(forceReload);
        private Task LoadComboBoxHinhThucKTAsync(bool forceReload = false) => BuildGlobalCacheAsync(forceReload);
        private Task LoadComboBoxTenTapTheAsync(bool forceReload = false) => BuildGlobalCacheAsync(forceReload);
        // SỰ KIỆN TÌM KIẾM (3 combo DropDownList): bỏ qua khi chính phần mềm đang nạp lại danh sách combo
        private async void combobox1_TimKiemTheoTen_TextChanged(object? sender, EventArgs e) { if (!_dangNapCombo) await DebouncedSearchAsync(); }
        private async void comboBox_TimKiemDonViKhenThuong_TextChanged(object? sender, EventArgs e) { if (!_dangNapCombo) await DebouncedSearchAsync(); }
        private async void comboBox1_HinhThucKT_TextChanged(object? sender, EventArgs e) { if (!_dangNapCombo) await DebouncedSearchAsync(); }
        private void kryptonTextBox_TienThuong_TextChanged(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(kryptonTextBox_TienThuong.Text)) return;
            kryptonTextBox_TienThuong.TextChanged -= kryptonTextBox_TienThuong_TextChanged;
            try
            {
                int cursorPosition = kryptonTextBox_TienThuong.Text.Length - kryptonTextBox_TienThuong.SelectionStart;
                string rawText = kryptonTextBox_TienThuong.Text.Replace(".", "").Replace(",", "").Trim();
                if (long.TryParse(rawText, out long tienThuong))
                {
                    if (tienThuong < 0) tienThuong = 0;
                    kryptonTextBox_TienThuong.Text = string.Format("{0:#,##0}", tienThuong).Replace(",", ".");
                    kryptonTextBox_TienThuong.SelectionStart = Math.Max(0, kryptonTextBox_TienThuong.Text.Length - cursorPosition);
                }
                else
                {
                    string cleanStr = new string(rawText.Where(char.IsDigit).ToArray());
                    if (long.TryParse(cleanStr, out long cleanedTien))
                    {
                        kryptonTextBox_TienThuong.Text = string.Format("{0:#,##0}", cleanedTien).Replace(",", ".");
                        kryptonTextBox_TienThuong.SelectionStart = Math.Max(0, kryptonTextBox_TienThuong.Text.Length - cursorPosition);
                    }
                    else
                    {
                        kryptonTextBox_TienThuong.Text = "0";
                        kryptonTextBox_TienThuong.SelectionStart = kryptonTextBox_TienThuong.Text.Length;
                    }
                }
            }
            finally
            {
                kryptonTextBox_TienThuong.TextChanged += kryptonTextBox_TienThuong_TextChanged;
            }
        }
        private async Task DebouncedSearchAsync()
        {
            if (_searchCts != null)
            {
                _searchCts.Cancel();
                _searchCts.Dispose(); // ⭐ Bổ sung giải phóng bộ nhớ
            }
            _searchCts = new System.Threading.CancellationTokenSource();
            var token = _searchCts.Token;
            try
            {
                await Task.Delay(150, token);   // chọn combo là lọc gần như ngay (lọc trên RAM)
                if (!token.IsCancellationRequested)
                {
                    await LoadDataToGridAsync(token);
                }
            }
            catch (TaskCanceledException) { }
        }
        // TẢI DỮ LIỆU XUỐNG GRID CHÍNH
        /// <summary>
        /// Lọc bảng khen thưởng tập thể trên RAM theo 3 combo (tên tập thể, đơn vị khen thưởng, hình thức) — so khớp CHÍNH XÁC giá trị
        /// đã chọn (bản cũ dùng Contains nên chọn "Sư đoàn" ra lẫn cả "Sư đoàn 10") — và dựng BỘ LỌC KÉP cho 3 combo trong cùng lượt.
        /// Không truy vấn lại CSDL, không giới hạn số dòng. Ra đúng tập dòng đang hiển thị thì không gắn lại lưới (giữ vị trí cuộn).
        /// </summary>
        public async Task LoadDataToGridAsync(CancellationToken token = default)
        {
            await BuildGlobalCacheAsync().ConfigureAwait(true);
            if (token.IsCancellationRequested || IsDisposed) return;
            var ds = _globalCache ?? new List<DuLieuCache>();
            try
            {
                _dangNapCombo = true;
                string ten, dv, ht;
                try
                {
                    // Giá trị đang chọn không còn trong dữ liệu (vừa xóa bản ghi cuối cùng của nó / đổi năm) -> về không lọc
                    VeKhongLocNeuKhongCon(combobox1_TimKiemTheoTen, ds, x => x.TenTapThe);
                    VeKhongLocNeuKhongCon(comboBox_TimKiemDonViKhenThuong, ds, x => x.DonVi);
                    VeKhongLocNeuKhongCon(comboBox1_HinhThucKT, ds, x => x.HinhThucKhenThuong);
                    ten = Module_BoLocKhenThuong.GiaTriLoc(combobox1_TimKiemTheoTen);
                    dv = Module_BoLocKhenThuong.GiaTriLoc(comboBox_TimKiemDonViKhenThuong);
                    ht = Module_BoLocKhenThuong.GiaTriLoc(comboBox1_HinhThucKT);
                    var ketQua = new List<DuLieuCache>(ds.Count);
                    var conTen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var conDv = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var conHt = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var x in ds)
                    {
                        bool okTen = ten.Length == 0 || string.Equals(x.TenTapThe, ten, StringComparison.OrdinalIgnoreCase);
                        bool okDv = dv.Length == 0 || string.Equals(x.DonVi, dv, StringComparison.OrdinalIgnoreCase);
                        bool okHt = ht.Length == 0 || string.Equals(x.HinhThucKhenThuong, ht, StringComparison.OrdinalIgnoreCase);
                        if (okDv && okHt && GiaTriHopLe(x.TenTapThe)) conTen.Add(x.TenTapThe);
                        if (okTen && okHt && GiaTriHopLe(x.DonVi)) conDv.Add(x.DonVi);
                        if (okTen && okDv && GiaTriHopLe(x.HinhThucKhenThuong)) conHt.Add(x.HinhThucKhenThuong);
                        if (okTen && okDv && okHt) ketQua.Add(x);
                    }
                    Module_BoLocKhenThuong.NapLaiCombo(combobox1_TimKiemTheoTen, Module_BoLocKhenThuong.SapABC(conTen), "");
                    Module_BoLocKhenThuong.NapLaiCombo(comboBox_TimKiemDonViKhenThuong, Module_BoLocKhenThuong.SapABC(conDv), "");
                    Module_BoLocKhenThuong.NapLaiCombo(comboBox1_HinhThucKT, Module_BoLocKhenThuong.SapABC(conHt), "");
                    GanLuoi(ketQua);
                }
                finally { _dangNapCombo = false; }
                CapNhatNhanTongSo();
                CapNhatTrangThaiNutThaoTac();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private static bool GiaTriHopLe(string? s) => !string.IsNullOrWhiteSpace(s) && s != "[Lỗi giải mã]" && s != "[Lỗi dữ liệu]";
        private static void VeKhongLocNeuKhongCon(ComboBox? cb, List<DuLieuCache> ds, Func<DuLieuCache, string> lay)
        {
            string dang = Module_BoLocKhenThuong.GiaTriLoc(cb);
            if (cb == null || dang.Length == 0) return;
            foreach (var x in ds)
                if (string.Equals(lay(x), dang, StringComparison.OrdinalIgnoreCase)) return;
            cb.SelectedIndex = 0;
        }
        /// <summary>Gắn kết quả lọc lên lưới (tắt vẽ trong lúc gắn). Cùng tập ID với lần trước thì bỏ qua.</summary>
        private void GanLuoi(List<DuLieuCache> ketQua)
        {
            var grid = kryptonDataGridView1;
            if (grid == null || grid.IsDisposed) return;
            var ids = new List<int>(ketQua.Count);
            foreach (var x in ketQua) ids.Add(x.ID);
            if (_idHienThi != null && grid.DataSource is DataTable && _idHienThi.SequenceEqual(ids)) return;
            DataTable dt = CreateDataTableSchema();
            dt.BeginLoadData();
            foreach (var x in ketQua)
                dt.Rows.Add(x.ID, x.STT, x.TenTapThe, x.HinhThucKhenThuong, x.DonVi, x.SoQuyetDinh, x.NgayQuyetDinh, x.NguoiKy,
                            x.NoiDung, x.TienThuong, x.NgayCapPhat, x.CanBoCapPhat, x.NguoiDaiDienNhan, x.GhiChu);
            dt.EndLoadData();
            using (Module_LuoiAo.TamDungVe(grid))
            {
                grid.DataSource = dt;
            }
            _idHienThi = ids;
        }
        private DataTable CreateDataTableSchema()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("STT", typeof(int));
            dt.Columns.Add("TenTapThe", typeof(string));
            dt.Columns.Add("HinhThuc_KhenThuong", typeof(string));
            dt.Columns.Add("DonVi_CapKhenThuong", typeof(string));
            dt.Columns.Add("SoQuyetDinh", typeof(string));
            dt.Columns.Add("NgayQuyetDinh", typeof(string));
            dt.Columns.Add("NguoiKy", typeof(string));
            dt.Columns.Add("NoiDung_KhenThuong", typeof(string));
            dt.Columns.Add("TienThuong", typeof(long));
            dt.Columns.Add("NgayCapPhat", typeof(string));
            dt.Columns.Add("CanBoCapPhat", typeof(string));
            dt.Columns.Add("NguoiDaiDienNhan", typeof(string));
            dt.Columns.Add("GhiChu", typeof(string));
            return dt;
        }
        // CÁC HÀM TIỆN ÍCH / GIAO DIỆN
        private async void kryptonButton_LamMoiCacOTimKiem_Click(object? sender, EventArgs e)
        {
            // Ngắt sự kiện tạm thời
            if (combobox1_TimKiemTheoTen != null) combobox1_TimKiemTheoTen.TextChanged -= combobox1_TimKiemTheoTen_TextChanged;
            if (comboBox_TimKiemDonViKhenThuong != null) comboBox_TimKiemDonViKhenThuong.TextChanged -= comboBox_TimKiemDonViKhenThuong_TextChanged;
            if (comboBox1_HinhThucKT != null) comboBox1_HinhThucKT.TextChanged -= comboBox1_HinhThucKT_TextChanged;
            // Đưa tất cả ComboBox về trạng thái rỗng (Index 0 là mục "")
            if (combobox1_TimKiemTheoTen != null && combobox1_TimKiemTheoTen.Items.Count > 0) combobox1_TimKiemTheoTen.SelectedIndex = 0;
            if (comboBox_TimKiemDonViKhenThuong != null && comboBox_TimKiemDonViKhenThuong.Items.Count > 0) comboBox_TimKiemDonViKhenThuong.SelectedIndex = 0;
            if (comboBox1_HinhThucKT != null && comboBox1_HinhThucKT.Items.Count > 0) comboBox1_HinhThucKT.SelectedIndex = 0;
            // Bật lại sự kiện
            if (combobox1_TimKiemTheoTen != null) combobox1_TimKiemTheoTen.TextChanged += combobox1_TimKiemTheoTen_TextChanged;
            if (comboBox_TimKiemDonViKhenThuong != null) comboBox_TimKiemDonViKhenThuong.TextChanged += comboBox_TimKiemDonViKhenThuong_TextChanged;
            if (comboBox1_HinhThucKT != null) comboBox1_HinhThucKT.TextChanged += comboBox1_HinhThucKT_TextChanged;
            await LoadDataToGridAsync();
            _ = ShowHanhDongStatusAsync("✔ Đã làm mới bộ lọc!", true);
        }
        private void ResetInput()
        {
            _selectedID = -1;
            isEditing = false;
            if (kryptonTextBox_STT != null) kryptonTextBox_STT.Text = "Tự động";
            kryptonTextBox_TenTapThe?.Clear();
            if (comboBox_HinhThucKhenThuong != null) { comboBox_HinhThucKhenThuong.SelectedIndex = -1; comboBox_HinhThucKhenThuong.Text = ""; }
            if (comboBox_DonViKhenThuong != null) { comboBox_DonViKhenThuong.SelectedIndex = -1; comboBox_DonViKhenThuong.Text = ""; }
            kryptonTextBox_SoQuyetDinh?.Clear();
            kryptonTextBox_NgayQuyetDinh?.Clear();
            kryptonTextBox_NguoiKy?.Clear();
            richTextBox1_NoiDungKhenThuong?.Clear();
            if (kryptonTextBox_TienThuong != null)
            {
                kryptonTextBox_TienThuong.TextChanged -= kryptonTextBox_TienThuong_TextChanged;
                kryptonTextBox_TienThuong.Text = "0";
                kryptonTextBox_TienThuong.TextChanged += kryptonTextBox_TienThuong_TextChanged;
            }
            kryptonTextBox_NgayCapPhat?.Clear();
            kryptonTextBox_CanBoCapPhat?.Clear();
            kryptonTextBox_NguoiDaiDienNhan?.Clear();
            kryptonTextBox_GhiChu?.Clear();
            if (kryptonButton_ThemKhenThuong != null) kryptonButton_ThemKhenThuong.Values.Text = "Thêm";
            if (kryptonButton_SuaVaLuuKhenThuong != null)
            {
                kryptonButton_SuaVaLuuKhenThuong.Enabled = false;
                kryptonButton_SuaVaLuuKhenThuong.Values.Text = "Sửa";
            }
            if (kryptonButton_XoaKhenThuong != null) kryptonButton_XoaKhenThuong.Enabled = false;
        }
        private void kryptonDataGridView1_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || sender is not DataGridView grid) return;
            try
            {
                var row = grid.Rows[e.RowIndex];
                if (int.TryParse(row.Cells["ID"].Value?.ToString(), out int parsedId))
                {
                    _selectedID = parsedId;
                    isEditing = true;
                }
                else return;
                if (kryptonTextBox_STT != null) kryptonTextBox_STT.Text = row.Cells["STT"].Value?.ToString() ?? "";
                if (kryptonTextBox_TenTapThe != null) kryptonTextBox_TenTapThe.Text = row.Cells["TenTapThe"].Value?.ToString() ?? "";
                if (comboBox_HinhThucKhenThuong != null) comboBox_HinhThucKhenThuong.Text = row.Cells["HinhThuc_KhenThuong"].Value?.ToString() ?? "";
                if (comboBox_DonViKhenThuong != null) comboBox_DonViKhenThuong.Text = row.Cells["DonVi_CapKhenThuong"].Value?.ToString() ?? "";
                if (kryptonTextBox_SoQuyetDinh != null) kryptonTextBox_SoQuyetDinh.Text = row.Cells["SoQuyetDinh"].Value?.ToString() ?? "";
                if (kryptonTextBox_NgayQuyetDinh != null) kryptonTextBox_NgayQuyetDinh.Text = row.Cells["NgayQuyetDinh"].Value?.ToString() ?? "";
                if (kryptonTextBox_NguoiKy != null) kryptonTextBox_NguoiKy.Text = row.Cells["NguoiKy"].Value?.ToString() ?? "";
                if (richTextBox1_NoiDungKhenThuong != null) richTextBox1_NoiDungKhenThuong.Text = row.Cells["NoiDung_KhenThuong"].Value?.ToString() ?? "";
                if (kryptonTextBox_TienThuong != null) kryptonTextBox_TienThuong.Text = row.Cells["TienThuong"].Value?.ToString() ?? "0";
                if (kryptonTextBox_NgayCapPhat != null) kryptonTextBox_NgayCapPhat.Text = row.Cells["NgayCapPhat"].Value?.ToString() ?? "";
                if (kryptonTextBox_CanBoCapPhat != null) kryptonTextBox_CanBoCapPhat.Text = row.Cells["CanBoCapPhat"].Value?.ToString() ?? "";
                if (kryptonTextBox_NguoiDaiDienNhan != null) kryptonTextBox_NguoiDaiDienNhan.Text = row.Cells["NguoiDaiDienNhan"].Value?.ToString() ?? "";
                if (kryptonTextBox_GhiChu != null) kryptonTextBox_GhiChu.Text = row.Cells["GhiChu"].Value?.ToString() ?? "";
                if (kryptonButton_SuaVaLuuKhenThuong != null)
                {
                    kryptonButton_SuaVaLuuKhenThuong.Enabled = true;
                    kryptonButton_SuaVaLuuKhenThuong.Values.Text = "Lưu";
                }
                if (kryptonButton_XoaKhenThuong != null) kryptonButton_XoaKhenThuong.Enabled = true;
                // ⭐ BỔ SUNG DÒNG NÀY ĐỂ MỞ KHÓA TRẠNG THÁI CHO NGƯỜI DÙNG
                if (kryptonButton_ThemKhenThuong != null) kryptonButton_ThemKhenThuong.Values.Text = "Làm mới";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi CellClick Grid]: {ex.Message}");
            }
        }
        private async void kryptonButton_XoaKhenThuong_Click(object sender, EventArgs e)
        {
            if (!isEditing || _selectedID < 0) return;
            if (string.IsNullOrEmpty(_currentDbPath)) return;
            string tenTapThe = kryptonTextBox_TenTapThe.Text?.Trim() ?? string.Empty;
            if (MessageBox.Show($"Bạn có chắc chắn muốn xóa bản ghi:\n{tenTapThe}?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
            {
                return;
            }
            // 🔥 THÊM ĐOẠN XÁC MINH ADMIN TẠI ĐÂY
            using (Form24_XacMinhAdmin frm = new Form24_XacMinhAdmin())
            {
                frm.TopMost = true;
                frm.StartPosition = FormStartPosition.CenterScreen;
                if (frm.ShowDialog(this) != DialogResult.OK) return;
            }
            kryptonButton_XoaKhenThuong.Enabled = false;
            try
            {
                int idXoa = _selectedID;   // chụp trên luồng UI (sau xác nhận + xác minh Admin)
                string connectionString = Module_KetNoiCSDL.ChuoiKetNoi(_currentDbPath);
                await Task.Run(() =>
                {
                    using var conn = new SqliteConnection(connectionString);
                    conn.Open();
                    using var tran = conn.BeginTransaction();
                    try
                    {
                        const string query = @"DELETE FROM ""ThongKe_KhenThuongTapThe"" WHERE ID = @ID;";
                        using (var cmd = new SqliteCommand(query, conn, tran))
                        {
                            cmd.Parameters.Add("@ID", SqliteType.Integer).Value = idXoa;
                            cmd.ExecuteNonQuery();
                        }
                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                });
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Xóa khen thưởng tập thể (năm cũ)",
                    $"ID={idXoa} | Tập thể: {tenTapThe} | CSDL: {Path.GetFileName(_currentDbPath)}");
                if (IsDisposed) return;
                await DanhLaiSoThuTuAsync();
                _ = ShowHanhDongStatusAsync("✔ Xóa dữ liệu thành công!", true);
                _globalCache = null;
                await LoadComboBoxDonViAsync();
                await LoadComboBoxHinhThucKTAsync();
                await LoadDataToGridAsync();
                ResetInput();
                // Vừa xóa bản ghi cuối cùng của năm => năm đó phải biến khỏi combo chọn CSDL (Form50 cũng quét lại theo)
                // Giấy khen cá nhân (Form50) của năm đó cũng trống thì xóa hẳn tệp (không để rác)
                if (_globalCache is { Count: 0 })
                {
                    Module_HoTroLuuDataTheoNamCu.XoaTepLichSuNeuRong(_currentDbPath);
                    Module_BoLocKhenThuong.BaoKhoNamCuThayDoi(_currentDbPath);
                }
            }
            catch (Exception ex)
            {
                _ = ShowHanhDongStatusAsync("❌ Lỗi khi xóa dữ liệu!", false);
                MessageBox.Show("Lỗi khi xóa dữ liệu:\n\n" + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                kryptonButton_XoaKhenThuong.Enabled = true;
            }
        }
        private bool KiemTraDuLieuDauVao()
        {
            if (string.IsNullOrEmpty(_currentDbPath))
            {
                MessageBox.Show("Vui lòng chọn cơ sở dữ liệu năm cũ từ danh sách trước khi thao tác!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (kryptonTextBox_TenTapThe != null && string.IsNullOrWhiteSpace(kryptonTextBox_TenTapThe.Text))
            {
                MessageBox.Show("Vui lòng nhập 'Tên tập thể' (bắt buộc)!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                kryptonTextBox_TenTapThe.Focus();
                return false;
            }
            return true;
        }
        // CRUD: THÊM, SỬA, XÓA TRÊN DB NĂM CŨ CHỌN BỞI COMBOBOX   
        private async void kryptonButton_ThemKhenThuong_Click(object? sender, EventArgs e)
        {
            // 1. THAO TÁC GIAO DIỆN CƠ BẢN
            if (kryptonButton_ThemKhenThuong.Values.Text == "Làm mới")
            {
                ResetInput();
                kryptonTextBox_TenTapThe?.Focus();
                return;
            }
            // 2. GUARD CLAUSE - CHẶN CỬA DỮ LIỆU ĐẦU VÀO
            if (!KiemTraDuLieuDauVao()) return;
            using (Form24_XacMinhAdmin frm = new Form24_XacMinhAdmin())
            {
                frm.TopMost = true;
                frm.StartPosition = FormStartPosition.CenterScreen;
                if (frm.ShowDialog(this) != DialogResult.OK) return;
            }
            kryptonButton_ThemKhenThuong.Enabled = false;
            // 🌟 CỜ TRẠNG THÁI: Tách biệt lỗi DB và lỗi Giao diện
            bool isDbSuccess = false;
            try
            {
                // 3. CHUẨN HÓA DỮ LIỆU (Thực hiện trên RAM, trước khi mở kết nối DB để tiết kiệm thời gian lock DB)
                string tienThuongStr = kryptonTextBox_TienThuong.Text.Replace(".", "").Replace(",", "").Trim();
                if (string.IsNullOrWhiteSpace(tienThuongStr)) tienThuongStr = "0";
                string connectionString = Module_KetNoiCSDL.ChuoiKetNoi(_currentDbPath);
                string sqlInsert = @"INSERT INTO ThongKe_KhenThuongTapThe
                    (ID, STT, TenTapThe, HinhThuc_KhenThuong, DonVi_CapKhenThuong,
                     SoQuyetDinh, NgayQuyetDinh, NguoiKy, NoiDung_KhenThuong,
                     TienThuong, NgayCapPhat, CanBoCapPhat, NguoiDaiDienNhan, GhiChu)
                    VALUES
                    (@ID, @STT, @TenTapThe, @HinhThuc_KhenThuong, @DonVi_CapKhenThuong,
                     @SoQuyetDinh, @NgayQuyetDinh, @NguoiKy, @NoiDung_KhenThuong,
                     @TienThuong, @NgayCapPhat, @CanBoCapPhat, @NguoiDaiDienNhan, @GhiChu)";
                // Gán tham số (ĐỌC Ô NHẬP + MÃ HÓA) trên LUỒNG UI bằng đúng hàm helper cũ; lệnh được chuyển xuống luồng nền thực thi
                // (một SqliteCommand dùng LẦN LƯỢT ở 2 luồng, không đồng thời => an toàn).
                using var cmd = new SqliteCommand(sqlInsert);
                GanThamSoVaMaHoaKhenThuong(cmd, tienThuongStr);
                // 4. KẾT NỐI VÀ GIAO DỊCH CƠ SỞ DỮ LIỆU - ở LUỒNG NỀN (OpenAsync/ExecuteAsync của Microsoft.Data.Sqlite thực chất
                //    chạy đồng bộ ngay trên luồng gọi => bản cũ chạy trên luồng giao diện)
                await Task.Run(() =>
                {
                    using var conn = new SqliteConnection(connectionString);
                    conn.Open();
                    using var tran = conn.BeginTransaction();
                    try
                    {
                        int newStt = 1;
                        int newId = 1;
                        // 🌟 TỐI ƯU HÓA: Gộp 2 lệnh MAX() thành 1 truy vấn duy nhất. Giảm 50% thời gian đọc.
                        string queryMax = "SELECT COALESCE(MAX(ID), 0) AS MaxID, COALESCE(MAX(STT), 0) AS MaxSTT FROM ThongKe_KhenThuongTapThe";
                        using (var cmdMax = new SqliteCommand(queryMax, conn, tran))
                        using (var reader = cmdMax.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                newId = Convert.ToInt32(reader["MaxID"]) + 1;
                                newStt = Convert.ToInt32(reader["MaxSTT"]) + 1;
                            }
                        }
                        cmd.Connection = conn;
                        cmd.Transaction = tran;
                        // Gán tham số tự tính SAU helper. SỬA LỖI CŨ: bản cũ thêm @ID TRƯỚC rồi mới gọi helper, mà helper gọi
                        // Parameters.Clear() => mất @ID => "Must add values for the following parameters: @ID" => Thêm luôn thất bại.
                        cmd.Parameters.AddWithValue("@ID", newId);
                        AddParam(cmd, "@STT", SqliteType.Integer, newStt);
                        cmd.ExecuteNonQuery();
                        // Chốt giao dịch
                        tran.Commit();
                    }
                    catch (Exception dbEx)
                    {
                        tran.Rollback(); // Thu hồi toàn bộ thay đổi nếu rớt mạng, cúp điện, tràn ram
                        System.Diagnostics.Debug.WriteLine($"[Lỗi Dữ Liệu][ThemKhenThuong]: {dbEx.Message}");
                        // Ném lỗi ra ngoài với thông điệp rõ ràng để catch tổng xử lý
                        throw new Exception("Quá trình ghi vào CSDL thất bại. Đã hoàn tác an toàn (Rollback).", dbEx);
                    }
                });
                isDbSuccess = true; // Đánh dấu dữ liệu đã an toàn nằm trong ổ cứng
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Thêm khen thưởng tập thể (năm cũ)",
                    $"Tập thể: {kryptonTextBox_TenTapThe.Text?.Trim()} | CSDL: {Path.GetFileName(_currentDbPath)}");
                if (IsDisposed) return;
                // 5. CẬP NHẬT GIAO DIỆN (Chỉ chạy khi DB đã an toàn)
                if (isDbSuccess)
                {
                    _ = ShowHanhDongStatusAsync("✔ Thêm mới dữ liệu thành công!", true);
                    _globalCache = null;
                    await LoadComboBoxDonViAsync();
                    await LoadComboBoxHinhThucKTAsync();
                    await LoadComboBoxTenTapTheAsync();
                    await LoadDataToGridAsync();
                    ResetInput();
                }
            }
            catch (Exception ex)
            {
                // Phân loại lỗi để thông báo đúng bản chất cho người dùng
                string errorTitle = "Lỗi hệ thống";
                string errorMessage = "Lỗi khi thêm dữ liệu:\n" + ex.Message;
                if (isDbSuccess)
                {
                    errorTitle = "Lỗi hiển thị";
                    errorMessage = "Dữ liệu ĐÃ ĐƯỢC LƯU THÀNH CÔNG vào hệ thống, nhưng có lỗi khi tải lại giao diện hiển thị. Vui lòng thử mở lại Form.\n\nChi tiết lỗi: " + ex.Message;
                }
                _ = ShowHanhDongStatusAsync("❌ Có lỗi xảy ra!", false);
                MessageBox.Show(errorMessage, errorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 6. PHỤC HỒI TRẠNG THÁI (Defensive)
                // Đảm bảo không gọi .Enabled = true trên một nút bấm đã bị dispose (nếu form bị đóng giữa chừng)
                if (kryptonButton_ThemKhenThuong != null && !kryptonButton_ThemKhenThuong.IsDisposed)
                {
                    kryptonButton_ThemKhenThuong.Enabled = true;
                }
            }
        }
        private async void kryptonButton_SuaVaLuuKhenThuong_Click(object? sender, EventArgs e)
        {
            if (!isEditing || _selectedID < 0) return;
            if (!KiemTraDuLieuDauVao()) return;
            using (Form24_XacMinhAdmin frm = new Form24_XacMinhAdmin())
            {
                frm.TopMost = true;
                frm.StartPosition = FormStartPosition.CenterScreen;
                if (frm.ShowDialog(this) != DialogResult.OK) return;
            }
            kryptonButton_SuaVaLuuKhenThuong.Enabled = false;
            try
            {
                string tienThuongStr = kryptonTextBox_TienThuong.Text.Replace(".", "").Replace(",", "").Trim();
                if (string.IsNullOrWhiteSpace(tienThuongStr)) tienThuongStr = "0";
                string connectionString = Module_KetNoiCSDL.ChuoiKetNoi(_currentDbPath);
                string query = @"UPDATE ThongKe_KhenThuongTapThe SET
             TenTapThe = @TenTapThe,
             HinhThuc_KhenThuong = @HinhThuc_KhenThuong,
             DonVi_CapKhenThuong = @DonVi_CapKhenThuong, SoQuyetDinh = @SoQuyetDinh,
             NgayQuyetDinh = @NgayQuyetDinh, NguoiKy = @NguoiKy,
             NoiDung_KhenThuong = @NoiDung_KhenThuong, TienThuong = @TienThuong,
             NgayCapPhat = @NgayCapPhat, CanBoCapPhat = @CanBoCapPhat,
             NguoiDaiDienNhan = @NguoiDaiDienNhan, GhiChu = @GhiChu
             WHERE ID = @ID";
                // Gán tham số (đọc ô nhập + mã hóa) trên LUỒNG UI bằng helper cũ; ID chụp ngay lúc bấm.
                using var cmd = new SqliteCommand(query);
                // ⭐ GỌI HÀM HELPER RÚT GỌN CODE
                GanThamSoVaMaHoaKhenThuong(cmd, tienThuongStr);
                // (Bản cũ thêm @ID TRƯỚC helper; helper gọi Parameters.Clear() => mất @ID. Nay thêm SAU helper.)
                cmd.Parameters.AddWithValue("@ID", _selectedID);
                await Task.Run(() =>
                {
                    using var conn = new SqliteConnection(connectionString);
                    conn.Open();
                    cmd.Connection = conn;
                    cmd.ExecuteNonQuery();
                });
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Sửa khen thưởng tập thể (năm cũ)",
                    $"ID={_selectedID} | Tập thể: {kryptonTextBox_TenTapThe.Text?.Trim()} | CSDL: {Path.GetFileName(_currentDbPath)}");
                if (IsDisposed) return;
                _ = ShowHanhDongStatusAsync("✔ Cập nhật dữ liệu thành công!", true);
                _globalCache = null;
                await LoadComboBoxDonViAsync();
                await LoadComboBoxHinhThucKTAsync();
                await LoadComboBoxTenTapTheAsync();
                await LoadDataToGridAsync();
                ResetInput();
            }
            catch (Exception ex)
            {
                _ = ShowHanhDongStatusAsync("❌ Lỗi khi cập nhật dữ liệu!", false);
                MessageBox.Show("Lỗi khi cập nhật dữ liệu:\n" + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                kryptonButton_SuaVaLuuKhenThuong.Enabled = true;
            }
        }
        private async void xoaToanBoDuLieu_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentDbPath)) return;
            try
            {
                await BuildGlobalCacheAsync();
                int tongSoDong = _globalCache?.Count ?? 0;   // đếm cả bảng, không phải số dòng đang lọc trên lưới
                if (tongSoDong == 0)
                {
                    MessageBox.Show("Hiện tại không có dữ liệu thống kê khen thưởng tập thể nào để xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kiểm tra dữ liệu trước khi xóa: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            using (Form24_XacMinhAdmin frmXacMinh = new Form24_XacMinhAdmin())
            {
                frmXacMinh.TopMost = true;
                frmXacMinh.StartPosition = FormStartPosition.CenterScreen;
                if (frmXacMinh.ShowDialog() != DialogResult.OK) return;
            }
            DialogResult result = MessageBox.Show(
              "Bạn có chắc chắn muốn xóa toàn bộ dữ liệu khen thưởng tập thể không?\n\n" +
              "Lưu ý: Dữ liệu đã xóa sẽ không thể khôi phục.",
              "Xác nhận xóa toàn bộ",
              MessageBoxButtons.YesNo,
              MessageBoxIcon.Warning,
              MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes) return;
            try
            {
                string chuoiKetNoi = Module_KetNoiCSDL.ChuoiKetNoi(_currentDbPath);   // đọc trên luồng UI
                await Task.Run(() =>
                {
                    using var conn = new Microsoft.Data.Sqlite.SqliteConnection(chuoiKetNoi);
                    conn.Open();
                    using var tran = conn.BeginTransaction();
                    try
                    {
                        using (var cmd = new SqliteCommand("DELETE FROM ThongKe_KhenThuongTapThe", conn, tran))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                });
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Xóa TOÀN BỘ khen thưởng tập thể (năm cũ)",
                    $"CSDL: {Path.GetFileName(_currentDbPath)}");
                if (IsDisposed) return;
                // ⭐ ĐÃ XÓA LỆNH await DanhLaiSoThuTuAsync(); THEO ĐÚNG YÊU CẦU LOGIC VÌ LÚC NÀY BẢNG TRỐNG
                _ = ShowHanhDongStatusAsync("✔ Đã xóa sạch toàn bộ dữ liệu!", true);
                ResetInput();
                // Bảng của năm này đã trống => quét lại danh sách năm: năm vừa xóa biến khỏi combo chọn CSDL, tự chuyển sang
                // năm khác còn dữ liệu (hoặc dọn trống lưới nếu không còn năm nào). Form50 nhận cùng tín hiệu.
                _globalCache = null;
                Module_HoTroLuuDataTheoNamCu.XoaTepLichSuNeuRong(_currentDbPath);   // cả giấy khen cá nhân cũng trống => xóa hẳn tệp
                Module_BoLocKhenThuong.BaoKhoNamCuThayDoi(_currentDbPath);
                if (!Visible) await DongBoDanhSachNamAsync();   // tín hiệu chỉ tự làm mới trang đang hiện
            }
            catch (Exception ex)
            {
                _ = ShowHanhDongStatusAsync("❌ Lỗi khi xóa dữ liệu!", false);
                MessageBox.Show("Lỗi trong quá trình xóa dữ liệu:\n\n" + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Chạy ở LUỒNG NỀN (không đụng control): bản cũ dùng OpenAsync/ExecuteNonQueryAsync nhưng thực chất chạy đồng bộ
        // trên luồng giao diện. Đường dẫn CSDL năm cũ đọc ngay lúc gọi (trên luồng UI).
        private Task DanhLaiSoThuTuAsync()
        {
            if (string.IsNullOrEmpty(_currentDbPath)) return Task.CompletedTask;
            string connectionString = Module_KetNoiCSDL.ChuoiKetNoi(_currentDbPath);
            return Task.Run(() => DanhLaiSoThuTu(connectionString));
        }
        private static async Task DanhLaiSoThuTu(string connectionString)
        {
            using (var conn = new SqliteConnection(connectionString))
            {
                await conn.OpenAsync();
                string updateQuery = @"
        WITH RankedData AS (
            SELECT ID, ROW_NUMBER() OVER (ORDER BY STT ASC, ID ASC) AS NewSTT
            FROM ThongKe_KhenThuongTapThe
        )
        UPDATE ThongKe_KhenThuongTapThe
        SET STT = RankedData.NewSTT
        FROM RankedData
        WHERE ThongKe_KhenThuongTapThe.ID = RankedData.ID 
          AND ThongKe_KhenThuongTapThe.STT != RankedData.NewSTT;";
                using (var cmd = new SqliteCommand(updateQuery, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
        // HÀM KHỞI TẠO BẢNG DB (NẾU CHƯA CÓ)    
        public void KiemTraVaTaoBangThongKeKhenThuongTapThe()
        {
            // 1. Kiểm tra "Guard Clause" - Dừng sớm nếu điều kiện cơ sở không thỏa mãn
            if (string.IsNullOrWhiteSpace(_currentDbPath) || !File.Exists(_currentDbPath))
            {
                System.Diagnostics.Debug.WriteLine("[Cảnh báo] KiemTraVaTaoBang: Đường dẫn CSDL không hợp lệ hoặc file không tồn tại.");
                return;
            }
            string connectionString = Module_KetNoiCSDL.ChuoiKetNoi(_currentDbPath);
            using (SqliteConnection conn = new SqliteConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    // 2. Kiểm tra sự tồn tại của bảng (Dùng COUNT(1) tối ưu hơn COUNT(*))
                    string checkTableQuery = "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name='ThongKe_KhenThuongTapThe';";
                    bool isTableExists = false;
                    using (SqliteCommand cmdCheck = new SqliteCommand(checkTableQuery, conn))
                    {
                        isTableExists = Convert.ToInt32(cmdCheck.ExecuteScalar()) > 0;
                    }
                    // 3. Luồng xử lý phân nhánh rạch ròi
                    if (!isTableExists)
                    {
                        // TRƯỜNG HỢP A: Bảng chưa tồn tại -> Tạo mới chuẩn chỉ cấu trúc
                        // Bắt buộc phải có AUTOINCREMENT ở đây để phòng hờ trường hợp tạo file trắng từ đầu
                        string createTableQuery = @"
                    CREATE TABLE ""ThongKe_KhenThuongTapThe"" (
                        ""ID"" INTEGER PRIMARY KEY AUTOINCREMENT,
                        ""STT"" INTEGER,
                        ""TenTapThe"" TEXT,
                        ""HinhThuc_KhenThuong"" TEXT,
                        ""DonVi_CapKhenThuong"" TEXT,
                        ""SoQuyetDinh"" TEXT,
                        ""NgayQuyetDinh"" TEXT,
                        ""NguoiKy"" TEXT,
                        ""NoiDung_KhenThuong"" TEXT,
                        ""TienThuong"" TEXT,
                        ""NgayCapPhat"" TEXT,
                        ""CanBoCapPhat"" TEXT,
                        ""NguoiDaiDienNhan"" TEXT,
                        ""GhiChu"" TEXT
                    );";
                        using (SqliteCommand cmdCreate = new SqliteCommand(createTableQuery, conn))
                        {
                            cmdCreate.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // TRƯỜNG HỢP B: Bảng ĐÃ TỒN TẠI (Thường do lệnh CREATE TABLE AS sinh ra)
                        // Bảng này bị mất thuộc tính AUTOINCREMENT. 
                        // Cơ chế tự chữa lành: Quét và sửa các ID bị NULL do phần mềm từng bị lỗi Insert
                        string healDataQuery = @"UPDATE ""ThongKe_KhenThuongTapThe"" SET ID = rowid WHERE ID IS NULL;";
                        using (SqliteCommand cmdHeal = new SqliteCommand(healDataQuery, conn))
                        {
                            int rowsFixed = cmdHeal.ExecuteNonQuery();
                            if (rowsFixed > 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"[Tự động sửa lỗi] Đã khôi phục {rowsFixed} bản ghi có ID = NULL trong bảng ThongKe_KhenThuongTapThe.");
                            }
                        }
                    }
                    // 4. Khởi tạo Index (Luôn chạy để đảm bảo Index không bị mất)
                    // Index giúp các thao tác tìm kiếm bằng Textbox mượt mà hơn trên CSDL lớn
                    string createIndexQuery = @"CREATE INDEX IF NOT EXISTS ""idx_kt_tentapthe"" ON ""ThongKe_KhenThuongTapThe"" (""TenTapThe"");";
                    using (SqliteCommand cmdIndex = new SqliteCommand(createIndexQuery, conn))
                    {
                        cmdIndex.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    // 5. Ghi log ngoại lệ chi tiết bao gồm StackTrace để dễ dàng truy vết
                    System.Diagnostics.Debug.WriteLine($"[Nghiêm trọng][Khởi tạo DB Form51] Lỗi thao tác CSDL: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }        // XUẤT EXCEL & TRỢ GIÚP GIAO DIỆN
        private async void xuatDuLieuRaTepExcel_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (kryptonDataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            // ⭐ 1. LẤY TÊN NĂM CŨ TỪ COMBOBOX VÀ CHUẨN HÓA KHÔNG DẤU (VD: "Nam2025")
            string tenNamCu = "";
            if (comboBox1_ChonCscdKhenThuongTapTheNamCu.SelectedItem is FileLichSuDTO selectedFile)
            {
                tenNamCu = selectedFile.TenHienThi;
            }
            else if (!string.IsNullOrWhiteSpace(comboBox1_ChonCscdKhenThuongTapTheNamCu.Text))
            {
                tenNamCu = comboBox1_ChonCscdKhenThuongTapTheNamCu.Text;
            }
            // Dùng LINQ trích xuất đúng 4 chữ số của năm từ chuỗi (Bỏ qua toàn bộ chữ và dấu)
            string soNam = new string(tenNamCu.Where(char.IsDigit).ToArray());
            // Xây dựng biến an toàn cho Tên File và Tiêu đề
            string tenFileSafe = string.IsNullOrEmpty(soNam) ? "NamCu" : $"Nam{soNam}";
            string chuoiTieuDe = string.IsNullOrEmpty(soNam) ? "NĂM CŨ" : $"NĂM {soNam}";
            using var sfd = new SaveFileDialog
            {
                Title = "Chọn nơi lưu file Excel thống kê",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                // ⭐ 2. CHÈN TÊN FILE SAFE VÀO (Sẽ ra dạng ThongKe_KhenThuongTapThe_Nam2025_...)
                FileName = $"ThongKe_KhenThuongTapThe_{tenFileSafe}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;
            string filePath = sfd.FileName;
            string? textBanDau = xuatDuLieuRaTepExcel_ToolStripMenuItem.Text;
            Form_Loading frmLoad = new Form_Loading("Đang tạo tệp Excel, vui lòng đợi...");
            bool isLoadShown = false;
            frmLoad.Icon = this.Icon;
            // ⭐ 3. TẠO BIẾN TIÊU ĐỀ TRƯỚC KHI VÀO LUỒNG NGẦM ĐỂ TRÁNH LỖI CROSS-THREAD
            string tieuDeBaoCao = $"THỐNG KÊ KẾT QUẢ KHEN THƯỞNG TẬP THỂ {chuoiTieuDe}";
            try
            {
                this.Enabled = false;
                xuatDuLieuRaTepExcel_ToolStripMenuItem.Text = "Đang xuất...";
                frmLoad.Show(this);
                isLoadShown = true;
                await Task.Delay(50);
                var exportCols = kryptonDataGridView1.Columns.Cast<DataGridViewColumn>()
                    .Where(c => c.Visible && c.Name != "ID")
                    .ToList();
                int rowCount = kryptonDataGridView1.Rows.Count;
                int colCount = exportCols.Count;
                string[] headerArray = new string[colCount];
                double[] colWidths = new double[colCount];
                var dataList = new List<object[]>(rowCount);
                for (int c = 0; c < colCount; c++)
                {
                    headerArray[c] = exportCols[c].HeaderText;
                    colWidths[c] = exportCols[c].Width / 7.5;
                }
                for (int r = 0; r < rowCount; r++)
                {
                    var rowValues = new object[colCount];
                    for (int c = 0; c < colCount; c++)
                    {
                        rowValues[c] = kryptonDataGridView1.Rows[r].Cells[exportCols[c].Index].Value?.ToString() ?? "";
                    }
                    dataList.Add(rowValues);
                }
                await Task.Run(() =>
                {
                    using var wb = new ClosedXML.Excel.XLWorkbook();
                    var ws = wb.Worksheets.Add("ThongKeKhenThuongTapThe");
                    ws.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                    ws.Style.Font.FontSize = 11;
                    ws.Cell("A1").Value = "DANH SÁCH";
                    ws.Range(1, 1, 1, colCount).Merge().Style.Font.SetBold().Font.SetFontSize(14)
                        .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);
                    // ⭐ 4. CHÈN TIÊU ĐỀ VÀO TRANG TÍNH EXCEL
                    ws.Cell("A2").Value = tieuDeBaoCao;
                    ws.Range(2, 1, 2, colCount).Merge().Style.Font.SetBold().Font.SetFontSize(13)
                        .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);
                    int excelStartRow = 4;
                    for (int c = 0; c < colCount; c++)
                    {
                        var cell = ws.Cell(excelStartRow, c + 1);
                        cell.Value = string.IsNullOrWhiteSpace(headerArray[c]) ? exportCols[c].Name : headerArray[c];
                        cell.Style.Font.Bold = true;
                        cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                        cell.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                        cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(217, 225, 242);
                        ws.Column(c + 1).Width = colWidths[c];
                    }
                    ws.Row(excelStartRow).Height = 35;
                    if (rowCount > 0)
                    {
                        ws.Cell(excelStartRow + 1, 1).InsertData(dataList);
                        var dataRange = ws.Range(excelStartRow, 1, excelStartRow + rowCount, colCount);
                        dataRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                        dataRange.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                        var bodyRange = ws.Range(excelStartRow + 1, 1, excelStartRow + rowCount, colCount);
                        bodyRange.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                        bodyRange.Style.Alignment.WrapText = true;
                        ws.Range(excelStartRow + 1, 1, excelStartRow + rowCount, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                    }
                    int tongDong = rowCount + excelStartRow + 1;
                    var totalCell = ws.Cell(tongDong, 1);
                    totalCell.Value = $"Tổng cộng: {rowCount} tập thể được thống kê./.";
                    totalCell.Style.Font.SetBold().Font.SetItalic();
                    ws.Range(tongDong, 1, tongDong, colCount).Merge();
                    ws.PageSetup.PaperSize = ClosedXML.Excel.XLPaperSize.A4Paper;
                    ws.PageSetup.PageOrientation = ClosedXML.Excel.XLPageOrientation.Landscape;
                    ws.PageSetup.FitToPages(1, 0);
                    ws.PageSetup.Margins.Top = 0.5;
                    ws.PageSetup.Margins.Bottom = 0.5;
                    ws.PageSetup.Margins.Left = 0.4;
                    ws.PageSetup.Margins.Right = 0.4;
                    Module_BanQuyen.DongDauExcel(wb);
                    Module_ExcelAnToan.LuuAnToan(wb, filePath);
                });
                try
                {
                    Module_XuatNhapDuLieuThiDua.MoVaChonTepTrongExplorer(filePath);
                }
                catch { }
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Xuất Excel khen thưởng tập thể (năm cũ)",
                    $"{rowCount} dòng | {chuoiTieuDe} | Tệp: {Path.GetFileName(filePath)}");
                _ = ShowHanhDongStatusAsync("✔ Xuất tệp Excel thành công!", true);
            }
            catch (Exception ex)
            {
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Lỗi xuất Excel khen thưởng tập thể (năm cũ)", ex.Message);
                _ = ShowHanhDongStatusAsync("❌ Lỗi xuất dữ liệu Excel!", false);
                MessageBox.Show("Lỗi xuất dữ liệu:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (isLoadShown)
                {
                    frmLoad.Close();
                    this.Enabled = true;
                    this.Focus();
                }
                xuatDuLieuRaTepExcel_ToolStripMenuItem.Text = textBanDau;
            }
        }
        private void CauHinhGridCoBan(DataGridView dgv)
        {
            if (dgv == null) return;
            dgv.AutoGenerateColumns = false;
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.MultiSelect = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.RowHeadersVisible = false;
            dgv.EnableHeadersVisualStyles = false;
            dgv.ScrollBars = ScrollBars.Both;
            dgv.BorderStyle = BorderStyle.None;
            dgv.AllowUserToOrderColumns = false;
            dgv.AllowUserToResizeColumns = false;
        }
        private void CauHinhStyleWeb(DataGridView dgv)
        {
            if (dgv == null) return;
            dgv.RowTemplate.Height = 36;
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgv.AllowUserToResizeRows = false;
            dgv.ColumnHeadersHeight = 60;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            dgv.GridColor = Color.FromArgb(224, 224, 224);
            dgv.RowsDefaultCellStyle.BackColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.BackgroundColor = Color.White;
            if (dgv is Krypton.Toolkit.KryptonDataGridView kDgv)
            {
                kDgv.GridStyles.Style = Krypton.Toolkit.DataGridViewStyle.List;
                kDgv.StateCommon.HeaderColumn.Content.Padding = new System.Windows.Forms.Padding(6, 8, 6, 8);
                kDgv.StateCommon.HeaderColumn.Content.Font = _fontGridHeader9Bold;
                kDgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                kDgv.StateCommon.DataCell.Content.Font = _fontGridCell9Regular;
                kDgv.StateCommon.DataCell.Content.Padding = new System.Windows.Forms.Padding(6, 8, 6, 8);
                kDgv.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
                kDgv.StateCommon.DataCell.Border.Color1 = System.Drawing.Color.FromArgb(224, 224, 224);
                kDgv.StateCommon.DataCell.Border.Width = 1;
                kDgv.StateSelected.DataCell.Back.Color1 = System.Drawing.Color.FromArgb(232, 244, 253);
                kDgv.StateSelected.DataCell.Back.Color2 = System.Drawing.Color.FromArgb(232, 244, 253);
                kDgv.StateSelected.DataCell.Content.Color1 = System.Drawing.Color.FromArgb(0, 102, 204);
                kDgv.Margin = new Padding(0, 0, 0, 30);
            }
        }
        private void CauHinhCotGrid(DataGridView dgv)
        {
            if (dgv == null) return;
            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ID", Name = "ID", Visible = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "STT", Name = "STT", HeaderText = "STT", Width = 50, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TenTapThe", Name = "TenTapThe", HeaderText = "Tên tập thể", Width = 220, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "HinhThuc_KhenThuong", Name = "HinhThuc_KhenThuong", HeaderText = "Hình thức khen thưởng", Width = 150, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DonVi_CapKhenThuong", Name = "DonVi_CapKhenThuong", HeaderText = "Đơn vị khen thưởng", Width = 180, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SoQuyetDinh", Name = "SoQuyetDinh", HeaderText = "Số Quyết định", Width = 100, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NgayQuyetDinh", Name = "NgayQuyetDinh", HeaderText = "Ngày Quyết định", Width = 100, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NguoiKy", Name = "NguoiKy", HeaderText = "Người ký", Width = 130, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NoiDung_KhenThuong", Name = "NoiDung_KhenThuong", HeaderText = "Nội dung khen thưởng", Width = 250, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TienThuong", Name = "TienThuong", HeaderText = "Tiền thưởng (VNĐ)", Width = 140, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N0" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NgayCapPhat", Name = "NgayCapPhat", HeaderText = "Ngày cấp phát", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CanBoCapPhat", Name = "CanBoCapPhat", HeaderText = "Cán bộ cấp", Width = 130, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NguoiDaiDienNhan", Name = "NguoiDaiDienNhan", HeaderText = "Người đại diện nhận", Width = 150, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "GhiChu", Name = "GhiChu", HeaderText = "Ghi chú", Width = 150, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }
        private void CapNhatNhanTongSo()
        {
            if (kryptonDataGridView1 == null || toolStripStatusLabel1 == null) return;
            int total = kryptonDataGridView1.Rows.Count;
            if (total > 0)
            {
                toolStripStatusLabel1.Text = $"Tổng cộng: {total} mục khen thưởng";
                toolStripStatusLabel1.Visible = true;
            }
            else
            {
                toolStripStatusLabel1.Visible = false;
            }
        }
        private async Task ShowHanhDongStatusAsync(string message, bool isSuccess = true)
        {
            if (toolStripStatusLabel2 == null) return;
            int currentCounter = ++_msgCounter;
            toolStripStatusLabel2.Text = message;
            toolStripStatusLabel2.ForeColor = isSuccess ? Color.SeaGreen : Color.Red;
            toolStripStatusLabel2.Visible = true;
            await Task.Delay(2000);
            if (_msgCounter == currentCounter)
            {
                toolStripStatusLabel2.Visible = false;
            }
        }
        private void kryptonButton2_TangCoChuRichText_Click(object? sender, EventArgs e)
        {
            if (richTextBox1_NoiDungKhenThuong == null) return;
            float currentSize = richTextBox1_NoiDungKhenThuong.Font.Size;
            if (currentSize < 36.0f) { richTextBox1_NoiDungKhenThuong.Font = new Font(richTextBox1_NoiDungKhenThuong.Font.FontFamily, currentSize + 2, richTextBox1_NoiDungKhenThuong.Font.Style); }
        }
        private void kryptonButton2_GiamCoChuRichText_Click(object? sender, EventArgs e)
        {
            if (richTextBox1_NoiDungKhenThuong == null) return;
            float currentSize = richTextBox1_NoiDungKhenThuong.Font.Size;
            if (currentSize > 8.0f) { richTextBox1_NoiDungKhenThuong.Font = new Font(richTextBox1_NoiDungKhenThuong.Font.FontFamily, currentSize - 2, richTextBox1_NoiDungKhenThuong.Font.Style); }
        }
        //private void kryptonButton1_Thoat_Click(object? sender, EventArgs e)
        //{
            //// 1. Tìm Form 50 (Thống kê khen thưởng năm cũ) trong RAM
        private void xoaTimKiem_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (kryptonButton_LamMoiCacOTimKiem == null ||
                kryptonButton_LamMoiCacOTimKiem.IsDisposed ||
                kryptonButton_LamMoiCacOTimKiem.Disposing)
            {
                return;
            }
            kryptonButton_LamMoiCacOTimKiem.PerformClick();
        }
        // ⭐ HÀM HELPER: GOM TOÀN BỘ LOGIC MÃ HÓA VÀ GÁN THAM SỐ VÀO 1 CHỖ DUY NHẤT
        // using Microsoft.Data.Sqlite;  <-- cần có using này để dùng SqliteType
        private void GanThamSoVaMaHoaKhenThuong(SqliteCommand cmd, string tienThuongStr, int? newStt = null)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            // AN TOÀN: đảm bảo không còn tham số cũ sót lại nếu cmd bị tái sử dụng
            // giữa nhiều lần gọi (tránh lỗi "parameter count mismatch" khó debug).
            cmd.Parameters.Clear();
            if (newStt.HasValue)
                AddParam(cmd, "@STT", SqliteType.Integer, newStt.Value);
            // Dùng "?? string.Empty" trước khi truyền vào SafeEncrypt:
            //  - Hết cảnh báo CS8604 (possible null reference argument) khi bật nullable reference types.
            //  - An toàn thực sự: không phụ thuộc việc SafeEncrypt có tự xử lý null hay không.
            AddParam(cmd, "@TenTapThe", SqliteType.Text, SafeEncrypt(kryptonTextBox_TenTapThe?.Text ?? string.Empty));
            AddParam(cmd, "@HinhThuc_KhenThuong", SqliteType.Text, SafeEncrypt(comboBox_HinhThucKhenThuong?.Text ?? string.Empty));
            AddParam(cmd, "@DonVi_CapKhenThuong", SqliteType.Text, SafeEncrypt(comboBox_DonViKhenThuong?.Text ?? string.Empty));
            AddParam(cmd, "@SoQuyetDinh", SqliteType.Text, SafeEncrypt(kryptonTextBox_SoQuyetDinh?.Text ?? string.Empty));
            AddParam(cmd, "@NgayQuyetDinh", SqliteType.Text, SafeEncrypt(kryptonTextBox_NgayQuyetDinh?.Text ?? string.Empty));
            AddParam(cmd, "@NguoiKy", SqliteType.Text, SafeEncrypt(kryptonTextBox_NguoiKy?.Text ?? string.Empty));
            AddParam(cmd, "@NoiDung_KhenThuong", SqliteType.Text, SafeEncrypt(richTextBox1_NoiDungKhenThuong?.Text ?? string.Empty));
            AddParam(cmd, "@TienThuong", SqliteType.Text, SafeEncrypt(tienThuongStr ?? string.Empty));
            AddParam(cmd, "@NgayCapPhat", SqliteType.Text, SafeEncrypt(kryptonTextBox_NgayCapPhat?.Text ?? string.Empty));
            AddParam(cmd, "@CanBoCapPhat", SqliteType.Text, SafeEncrypt(kryptonTextBox_CanBoCapPhat?.Text ?? string.Empty));
            AddParam(cmd, "@NguoiDaiDienNhan", SqliteType.Text, SafeEncrypt(kryptonTextBox_NguoiDaiDienNhan?.Text ?? string.Empty));
            AddParam(cmd, "@GhiChu", SqliteType.Text, SafeEncrypt(kryptonTextBox_GhiChu?.Text ?? string.Empty));
        }
        /// <summary>
        /// Thêm 1 tham số vào SqliteCommand với KIỂU DỮ LIỆU TƯỜNG MINH thay vì để
        /// AddWithValue tự suy luận từ runtime type. Lợi ích:
        ///  - Hiệu suất: không tốn thời gian phản chiếu/kiểm tra kiểu CLR ở mỗi lần bind.
        ///  - An toàn/nhất quán: giá trị luôn được ghi vào cột đúng kiểu SQLite (TEXT/INTEGER...),
        ///    không phụ thuộc việc SafeEncrypt trả về string hay kiểu nào khác trong tương lai.
        ///  - Null CLR luôn được ánh xạ tường minh sang DBNull.Value, tránh hành vi mập mờ
        ///    tùy theo provider khi truyền thẳng null vào AddWithValue.
        /// </summary>
        private static void AddParam(SqliteCommand cmd, string name, SqliteType type, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.SqliteType = type;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
    }
}