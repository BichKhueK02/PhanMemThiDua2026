///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System.Data;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form49_QuanLyKhenThuongTapThe : Form
    {
        private readonly string _csdl4Path = Module_DanduongGPS.DuongDanCSDL4;
        //Thực tiễn là thước đo tiêu chuẩn để kiểm nghiệm chân lý. - Karl Marx
        //Anh yêu em, nhưng em không yêu anh. Anh đau, nhưng anh vẫn yêu em. - Trịnh Công Sơn
        // Hãy sống như thể bạn sẽ chết vào ngày mai. Hãy học như thể bạn sẽ sống mãi mãi. - Mahatma Gandhi
        // Tôi yêu mèo cam Garfield, vì nó dạy tôi rằng: "Ăn, ngủ, chơi, lười biếng, và vẫn được yêu thương". - Jim Davis
        // Biến lưu trạng thái
        private int _selectedID = -1;
        private bool isEditing = false;
        //private List<string> _cachedDonVi = null;
        private System.Threading.CancellationTokenSource _searchCts = null!;
        private int _msgCounter = 0;
        // LỚP CACHE TOÀN CỤC CHỐNG QUÉT DB
        private sealed class DuLieuCache
        {
            public int ID, STT;
            public long TienThuong;
            public string TenTapThe = "", DonVi = "", HinhThucKhenThuong = "", TenTim = "";
            public string SoQuyetDinh = "", NgayQuyetDinh = "", NguoiKy = "", NoiDung = "", NgayCapPhat = "",
                          CanBoCapPhat = "", NguoiDaiDienNhan = "", GhiChu = "";
        }
        private List<DuLieuCache>? _globalCache = null; // TOÀN BỘ bảng đã giải mã (đọc 1 lần ở luồng nền) -> lọc chỉ chạy trên RAM
        private Task<List<DuLieuCache>>? _dangDocCache;   // gộp các lần đọc chồng nhau
        private bool _dangNapCombo;                       // đang nạp lại combo lọc bằng code -> bỏ qua TextChanged
        private List<int>? _idHienThi;                    // tập ID đang hiển thị: lọc ra đúng tập cũ thì không gắn lại lưới
        public Form49_QuanLyKhenThuongTapThe()
        {
            InitializeComponent();
            Module_BieuTuongNut.ApDung(this);   // biểu tượng nút: Hiện đại = tự vẽ, Cổ điển = ảnh gốc nhúng trong exe
            Module_HeThong.BocBoTronGiaoDien(richTextBox1_NoiDungKhenThuong);
            Module_GiaoDienForm49Web.ApDung(this);
            // Đăng ký sự kiện Shown
            this.Shown += Form49_QuanLyKhenThuongTapThe_Shown;
        }
        private async void Form49_QuanLyThiDuaTapThe_Load(object? sender, EventArgs e)
        {
            // 1. KHỞI TẠO / KIỂM TRA CSDL
            KiemTraVaTaoBangThongKeKhenThuongTapThe();
            // 2. CẤU HÌNH DATAGRIDVIEW  
            CauHinhGridCoBan(kryptonDataGridView1);
            CauHinhStyleWeb(kryptonDataGridView1);
            CauHinhCotGrid(kryptonDataGridView1);
            // 3. CẤU HÌNH Ô STT            
            kryptonTextBox_STT.ReadOnly = true;
            kryptonTextBox_STT.StateCommon.Back.Color1 = Color.LightGreen;
            // 4. GÁN SỰ KIỆN DATAGRIDVIEW
            kryptonDataGridView1.CellClick -= kryptonDataGridView1_CellClick;
            kryptonDataGridView1.CellClick += kryptonDataGridView1_CellClick;
            // 5. GÁN SỰ KIỆN Ô TIỀN THƯỞNG            
            kryptonTextBox_TienThuong.TextChanged -= kryptonTextBox_TienThuong_TextChanged;
            kryptonTextBox_TienThuong.TextChanged += kryptonTextBox_TienThuong_TextChanged;
            // 6. GÁN SỰ KIỆN TÌM KIẾM REAL-TIME            
            if (textBox_TimKiemTheoTen != null)
            {
                textBox_TimKiemTheoTen.TextChanged -= textBox_TimKiemTheoTen_TextChanged;
                textBox_TimKiemTheoTen.TextChanged += textBox_TimKiemTheoTen_TextChanged;
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
            // Gọi kiểm tra ngay lúc đầu để ẩn nút nếu ô text đang trống
            KiemTraHienThiNutCoChu();
            // 7. KHỞI TẠO TRẠNG THÁI FORM
            ResetInput();
            // 8. TẢI DỮ LIỆU BAN ĐẦU
            await LoadComboBoxDonViAsync();
            await LoadComboBoxHinhThucKTAsync(); // <--- THÊM DÒNG NÀY ĐỂ TẢI BỘ LỌC HÌNH THỨC
            await LoadDataToGridAsync();
            CapNhatTrangThaiNutThaoTac();
            InitToolTips();
            Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1);
        }
        private void Form49_QuanLyKhenThuongTapThe_Shown(object? sender, EventArgs e)
        {
            textBox_TimKiemTheoTen.Focus();
        }
        private void richTextBox1_NoiDungKhenThuong_TextChanged(object? sender, EventArgs e)
        {
            KiemTraHienThiNutCoChu();
        }
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
        private void InitToolTips()
        {
            // KHỞI TẠO TOOLTIP - ỔN ĐỊNH CHO HỆ THỐNG NỘI BỘ
            // 1. Kiểm tra ToolTip
            if (toolTip1 == null)
                return;
            try
            {
                // 2. Cấu hình chung
                toolTip1.IsBalloon = true;
                toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
                toolTip1.ToolTipIcon = ToolTipIcon.Info;
                // Thời gian chờ trước khi hiển thị
                toolTip1.InitialDelay = 300;
                // Thời gian Tooltip hiển thị
                toolTip1.AutoPopDelay = 2500;
                // Thời gian chờ khi chuyển sang Control khác
                toolTip1.ReshowDelay = 100;
                // Cho phép hiển thị ngay cả khi Form chưa active
                toolTip1.ShowAlways = true;
                // 3. Gán Tooltip cho từng Control
                GanToolTipAnToan(
                    kryptonButton_LamMoiCacOTimKiem,
                    "Làm mới (xóa) bộ lọc tìm kiếm hiện tại");
                GanToolTipAnToan(
                    kryptonButton2_GiamCoChuRichText,
                    "Giảm cỡ chữ nội dung đang hiển thị");
                GanToolTipAnToan(
                    kryptonButton2_TangCoChuRichText,
                    "Tăng cỡ chữ nội dung đang hiển thị");
                GanToolTipAnToan(
                    kryptonButton_ThemKhenThuong,
                    "Thêm thông tin khen thưởng mới");
                GanToolTipAnToan(
                    kryptonButton_SuaVaLuuKhenThuong,
                    "Sửa và lưu thông tin khen thưởng");
                GanToolTipAnToan(
                    kryptonButton_XoaKhenThuong,
                    "Xóa thông tin khen thưởng đang chọn");
            }
            catch (ObjectDisposedException)
            {
                // ToolTip hoặc Control đã được giải phóng trong lúc thao tác.
                // Không để chức năng Tooltip ảnh hưởng đến hoạt động chính.
            }
            catch (InvalidOperationException)
            {
                // Trạng thái WinForms không phù hợp để cấu hình Tooltip.
                // Không để chức năng Tooltip làm Form dừng hoạt động.
            }
        }
        private void GanToolTipAnToan(Control control, string noiDung)
        {
            // 1. Control không tồn tại
            if (control == null)
                return;
            // 2. Control đã được giải phóng hoặc đang giải phóng
            if (control.IsDisposed || control.Disposing)
                return;
            // 3. Nội dung Tooltip không hợp lệ
            if (string.IsNullOrWhiteSpace(noiDung))
                return;
            // 4. ToolTip chưa được khởi tạo
            if (toolTip1 == null)
                return;
            try
            {
                // 5. Gán Tooltip
                toolTip1.SetToolTip(control, noiDung);
            }
            catch (ObjectDisposedException)
            {
                // Control đã bị giải phóng đúng thời điểm thao tác.
            }
            catch (InvalidOperationException)
            {
                // Control đang ở trạng thái không phù hợp.
            }
        }
        // HÀM BẢO VỆ GIAO DIỆN (MÃ HÓA & GIẢI MÃ AN TOÀN)
        /// <summary>
        /// Đọc + giải mã TOÀN BỘ bảng khen thưởng tập thể vào RAM ở luồng nền (1 lần cho tới khi dữ liệu đổi). Bản cũ chỉ giữ
        /// Tên/Đơn vị/Hình thức rồi MỖI LẦN GÕ lại truy vấn + giải mã các cột còn lại và cắt cụt ở 500 dòng.
        /// </summary>
        private async Task BuildGlobalCacheAsync(bool forceReload = false)
        {
            if (forceReload) _globalCache = null;
            if (_globalCache != null) return;
            Task<List<DuLieuCache>> viec;
            if (!forceReload && _dangDocCache != null) viec = _dangDocCache;
            else
            {
                string chuoiKetNoi = Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path);
                viec = Task.Run(() => DocToanBoBang(chuoiKetNoi));
                _dangDocCache = viec;
            }
            List<DuLieuCache> ds;
            try { ds = await viec; }
            finally { if (ReferenceEquals(_dangDocCache, viec)) _dangDocCache = null; }
            if (IsDisposed) return;
            _globalCache ??= ds;
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
                string ten = S(2);
                ds.Add(new DuLieuCache
                {
                    ID = rd.IsDBNull(0) ? 0 : Convert.ToInt32(rd.GetValue(0)),
                    STT = rd.IsDBNull(1) ? 0 : Convert.ToInt32(rd.GetValue(1)),
                    TenTapThe = ten, TenTim = BoLocDanhSach.ChuanHoaTimKiem(ten),
                    HinhThucKhenThuong = S(3), DonVi = S(4),
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi SafeDecrypt]: {ex.Message}");
                return "[Lỗi dữ liệu]";
            }
        }
        private string SafeEncrypt(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            return Module_BaoMatAES.MaHoa(input.Trim());
        }
        /// <summary>Giá trị các ô nhập của 1 bản ghi khen thưởng tập thể, đọc TRÊN LUỒNG UI rồi mới đưa xuống luồng nền.</summary>
        private sealed record KhenThuongTapTheNhap(string TenTapThe, string? HinhThuc, string? DonViCap, string SoQuyetDinh,
            string NgayQuyetDinh, string NguoiKy, string NoiDung, string TienThuong, string NgayCapPhat, string CanBoCapPhat,
            string NguoiDaiDienNhan, string GhiChu);
        private KhenThuongTapTheNhap DocKhenThuongTuGiaoDien()
        {
            string tienThuongStr = kryptonTextBox_TienThuong.Text.Replace(".", "").Replace(",", "").Trim();
            if (string.IsNullOrWhiteSpace(tienThuongStr)) tienThuongStr = "0";
            return new KhenThuongTapTheNhap(
                kryptonTextBox_TenTapThe.Text,
                comboBox_HinhThucKhenThuong?.Text,
                comboBox_DonViKhenThuong?.Text,
                kryptonTextBox_SoQuyetDinh.Text,
                kryptonTextBox_NgayQuyetDinh.Text,
                kryptonTextBox_NguoiKy.Text,
                richTextBox1_NoiDungKhenThuong.Text,
                tienThuongStr,
                kryptonTextBox_NgayCapPhat.Text,
                kryptonTextBox_CanBoCapPhat.Text,
                kryptonTextBox_NguoiDaiDienNhan.Text,
                kryptonTextBox_GhiChu.Text);
        }
        /// <summary>Gán 12 tham số dữ liệu (đã mã hóa) - dùng chung cho INSERT và UPDATE. An toàn ở luồng nền.</summary>
        private void ThemThamSoKhenThuong(SqliteCommand cmd, KhenThuongTapTheNhap kt)
        {
            cmd.Parameters.AddWithValue("@TenTapThe", SafeEncrypt(kt.TenTapThe));
            cmd.Parameters.AddWithValue("@HinhThuc_KhenThuong", SafeEncrypt(kt.HinhThuc));
            cmd.Parameters.AddWithValue("@DonVi_CapKhenThuong", SafeEncrypt(kt.DonViCap));
            cmd.Parameters.AddWithValue("@SoQuyetDinh", SafeEncrypt(kt.SoQuyetDinh));
            cmd.Parameters.AddWithValue("@NgayQuyetDinh", SafeEncrypt(kt.NgayQuyetDinh));
            cmd.Parameters.AddWithValue("@NguoiKy", SafeEncrypt(kt.NguoiKy));
            cmd.Parameters.AddWithValue("@NoiDung_KhenThuong", SafeEncrypt(kt.NoiDung));
            cmd.Parameters.AddWithValue("@TienThuong", SafeEncrypt(kt.TienThuong));
            cmd.Parameters.AddWithValue("@NgayCapPhat", SafeEncrypt(kt.NgayCapPhat));
            cmd.Parameters.AddWithValue("@CanBoCapPhat", SafeEncrypt(kt.CanBoCapPhat));
            cmd.Parameters.AddWithValue("@NguoiDaiDienNhan", SafeEncrypt(kt.NguoiDaiDienNhan));
            cmd.Parameters.AddWithValue("@GhiChu", SafeEncrypt(kt.GhiChu));
        }
        // LOAD VÀ CACHE DANH SÁCH ĐƠN VỊ VÀO COMBOBOX
        // Danh sách 2 combo lọc do LoadDataToGridAsync dựng theo BỘ LỌC KÉP; 2 hàm cũ (nơi gọi không đổi) chỉ còn bảo đảm bộ đệm.
        private Task LoadComboBoxDonViAsync(bool forceReload = false) => BuildGlobalCacheAsync(forceReload);
        // SỰ KIỆN TEXT CHANGED & DEBOUNCE TÌM KIẾM
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
                    // TỐI ƯU LỌC KÝ TỰ SỐ (KHÔNG DÙNG REGEX)
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
        // Ô gõ tên: chờ 150 ms sau phím cuối mới lọc. Chọn combo: lọc ngay (lọc trên RAM chỉ tốn vài mili-giây).
        private async void textBox_TimKiemTheoTen_TextChanged(object? sender, EventArgs e)
        {
            if (!_dangNapCombo) await DebouncedSearchAsync(150);
        }
        private async void comboBox_TimKiemDonViKhenThuong_TextChanged(object? sender, EventArgs e)
        {
            if (!_dangNapCombo) await DebouncedSearchAsync(0);
        }
        private async void comboBox1_HinhThucKT_TextChanged(object? sender, EventArgs e)
        {
            if (!_dangNapCombo) await DebouncedSearchAsync(0);
        }
        private async Task DebouncedSearchAsync(int treMs)
        {
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new System.Threading.CancellationTokenSource();
            var token = _searchCts.Token;
            try
            {
                if (treMs > 0) await Task.Delay(treMs, token);
                if (!token.IsCancellationRequested) await LoadDataToGridAsync(token);
            }
            catch (OperationCanceledException) { /* người dùng đang gõ tiếp */ }
        }
        private static bool GiaTriHopLe(string s) => s != "[Lỗi giải mã]" && s != "[Lỗi dữ liệu]";
        /// <summary>
        /// Lọc bảng khen thưởng tập thể trên RAM (tên tập thể không dấu, nhiều từ + đơn vị khen + hình thức, so khớp CHÍNH XÁC giá trị
        /// combo) và dựng BỘ LỌC KÉP cho 2 combo trong cùng lượt: combo nào cũng chỉ còn giá trị có trong các dòng thỏa mọi bộ lọc khác.
        /// Không truy vấn lại CSDL, không giới hạn số dòng (bản cũ cắt ở 500). Ra đúng tập dòng đang hiển thị thì không gắn lại lưới.
        /// </summary>
        public async Task LoadDataToGridAsync(System.Threading.CancellationToken token = default)
        {
            try
            {
                await BuildGlobalCacheAsync();
                if (token.IsCancellationRequested || IsDisposed) return;
                var ds = _globalCache ?? new List<DuLieuCache>();
                string[] tu = Module_BoLocKhenThuong.TachTuKhoa(textBox_TimKiemTheoTen?.Text);
                var tcDv = new Module_BoLocKhenThuong.TieuChiKep<DuLieuCache>(
                    Module_BoLocKhenThuong.GiaTriLoc(comboBox_TimKiemDonViKhenThuong), x => x.DonVi, GiaTriHopLe);
                var tcHt = new Module_BoLocKhenThuong.TieuChiKep<DuLieuCache>(
                    Module_BoLocKhenThuong.GiaTriLoc(comboBox1_HinhThucKT), x => x.HinhThucKhenThuong, GiaTriHopLe);
                var chiSo = Module_BoLocKhenThuong.LocKep(ds,
                    tu.Length == 0 ? null : x => Module_BoLocKhenThuong.KhopTuKhoa(x.TenTim, tu), tcDv, tcHt);
                _dangNapCombo = true;
                try
                {
                    Module_BoLocKhenThuong.NapLaiCombo(comboBox_TimKiemDonViKhenThuong, Module_BoLocKhenThuong.SapABC(tcDv.Con), "");
                    Module_BoLocKhenThuong.NapLaiCombo(comboBox1_HinhThucKT, Module_BoLocKhenThuong.SapABC(tcHt.Con), "");
                }
                finally { _dangNapCombo = false; }
                GanLuoi(ds, chiSo);
                CapNhatNhanTongSo();
                CapNhatTrangThaiNutThaoTac();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private List<DuLieuCache>? _cacheDaGan;   // bộ đệm của lần gắn lưới trước (nạp lại CSDL = bộ đệm mới => luôn gắn lại)
        /// <summary>Gắn kết quả lọc lên lưới (tắt vẽ trong lúc gắn). Cùng bộ đệm + cùng tập ID với lần trước thì bỏ qua.</summary>
        private void GanLuoi(List<DuLieuCache> ds, List<int> chiSo)
        {
            var grid = kryptonDataGridView1;
            if (grid == null || grid.IsDisposed) return;
            var ids = new List<int>(chiSo.Count);
            foreach (int i in chiSo) ids.Add(ds[i].ID);
            if (ReferenceEquals(_cacheDaGan, ds) && grid.DataSource is DataTable && Module_BoLocKhenThuong.CungChiSo(_idHienThi, ids)) return;
            DataTable dt = CreateDataTableSchema();
            dt.BeginLoadData();
            foreach (int i in chiSo)
            {
                var x = ds[i];
                dt.Rows.Add(x.ID, x.STT, x.TenTapThe, x.HinhThucKhenThuong, x.DonVi, x.SoQuyetDinh, x.NgayQuyetDinh, x.NguoiKy,
                            x.NoiDung, x.TienThuong, x.NgayCapPhat, x.CanBoCapPhat, x.NguoiDaiDienNhan, x.GhiChu);
            }
            dt.EndLoadData();
            using (Module_LuoiAo.TamDungVe(grid))
            {
                grid.DataSource = dt;
            }
            _idHienThi = ids;
            _cacheDaGan = ds;
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
        private async void kryptonButton_LamMoiCacOTimKiem_Click(object? sender, EventArgs e)
        {
            // Đưa 3 ô lọc về mặc định mà không bắn lọc lẻ tẻ, rồi lọc đúng 1 lần
            _searchCts?.Cancel();
            _dangNapCombo = true;
            try
            {
                if (textBox_TimKiemTheoTen != null) textBox_TimKiemTheoTen.Text = string.Empty;
                if (comboBox_TimKiemDonViKhenThuong != null && comboBox_TimKiemDonViKhenThuong.Items.Count > 0) comboBox_TimKiemDonViKhenThuong.SelectedIndex = 0;
                if (comboBox1_HinhThucKT != null && comboBox1_HinhThucKT.Items.Count > 0) comboBox1_HinhThucKT.SelectedIndex = 0;
            }
            finally { _dangNapCombo = false; }
            await LoadDataToGridAsync();
            _ = ShowHanhDongStatusAsync("✔ Đã làm mới bộ lọc!", true);
        }
        private void ResetInput()
        {
            _selectedID = -1;
            isEditing = false;
            kryptonTextBox_STT.Text = "Tự động";
            kryptonTextBox_TenTapThe?.Clear();
            if (comboBox_HinhThucKhenThuong != null) comboBox_HinhThucKhenThuong.SelectedIndex = -1;
            if (comboBox_DonViKhenThuong != null) comboBox_DonViKhenThuong.SelectedIndex = -1;
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
                kryptonTextBox_STT.Text = row.Cells["STT"].Value?.ToString() ?? "";
                kryptonTextBox_TenTapThe.Text = row.Cells["TenTapThe"].Value?.ToString() ?? "";
                if (comboBox_HinhThucKhenThuong != null) comboBox_HinhThucKhenThuong.Text = row.Cells["HinhThuc_KhenThuong"].Value?.ToString() ?? "";
                if (comboBox_DonViKhenThuong != null) comboBox_DonViKhenThuong.Text = row.Cells["DonVi_CapKhenThuong"].Value?.ToString() ?? "";
                kryptonTextBox_SoQuyetDinh.Text = row.Cells["SoQuyetDinh"].Value?.ToString() ?? "";
                kryptonTextBox_NgayQuyetDinh.Text = row.Cells["NgayQuyetDinh"].Value?.ToString() ?? "";
                kryptonTextBox_NguoiKy.Text = row.Cells["NguoiKy"].Value?.ToString() ?? "";
                richTextBox1_NoiDungKhenThuong.Text = row.Cells["NoiDung_KhenThuong"].Value?.ToString() ?? "";
                kryptonTextBox_TienThuong.Text = row.Cells["TienThuong"].Value?.ToString() ?? "0";
                kryptonTextBox_NgayCapPhat.Text = row.Cells["NgayCapPhat"].Value?.ToString() ?? "";
                kryptonTextBox_CanBoCapPhat.Text = row.Cells["CanBoCapPhat"].Value?.ToString() ?? "";
                kryptonTextBox_NguoiDaiDienNhan.Text = row.Cells["NguoiDaiDienNhan"].Value?.ToString() ?? "";
                kryptonTextBox_GhiChu.Text = row.Cells["GhiChu"].Value?.ToString() ?? "";
                if (kryptonButton_SuaVaLuuKhenThuong != null)
                {
                    kryptonButton_SuaVaLuuKhenThuong.Enabled = true;
                    kryptonButton_SuaVaLuuKhenThuong.Values.Text = "Lưu";
                }
                if (kryptonButton_XoaKhenThuong != null) kryptonButton_XoaKhenThuong.Enabled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi CellClick Grid]: {ex.Message}");
            }
        }
        private bool KiemTraDuLieuDauVao()
        {
            if (string.IsNullOrWhiteSpace(kryptonTextBox_TenTapThe.Text))
            {
                MessageBox.Show("Vui lòng nhập 'Tên tập thể' (bắt buộc)!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                kryptonTextBox_TenTapThe.Focus();
                return false;
            }
            return true;
        }
        private async void kryptonButton_ThemKhenThuong_Click(object? sender, EventArgs e)
        {
            if (kryptonButton_ThemKhenThuong.Values.Text == "Làm mới")
            {
                ResetInput();
                kryptonTextBox_TenTapThe.Focus();
                return;
            }
            if (!KiemTraDuLieuDauVao()) return;
            kryptonButton_ThemKhenThuong.Enabled = false;
            try
            {
                // Đọc ô nhập TRÊN LUỒNG UI; mở kết nối + mã hóa + ghi chạy ở LUỒNG NỀN (Microsoft.Data.Sqlite không có I/O bất
                // đồng bộ thật: OpenAsync/ExecuteNonQueryAsync trước đây vẫn chạy đồng bộ ngay trên luồng giao diện).
                var kt = DocKhenThuongTuGiaoDien();
                string connectionString = Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path);
                await Task.Run(() =>
                {
                    using var conn = new SqliteConnection(connectionString);
                    conn.Open();
                    using var tran = conn.BeginTransaction();
                    try
                    {
                        int newStt = 1;
                        using (var cmdMaxStt = new SqliteCommand("SELECT COALESCE(MAX(STT), 0) FROM ThongKe_KhenThuongTapThe", conn, tran))
                        {
                            var maxSttObj = cmdMaxStt.ExecuteScalar();
                            if (maxSttObj != null && maxSttObj != DBNull.Value)
                            {
                                newStt = Convert.ToInt32(maxSttObj) + 1;
                            }
                        }
                        string sqlInsert = @"INSERT INTO ThongKe_KhenThuongTapThe
                                     (STT, TenTapThe, HinhThuc_KhenThuong, DonVi_CapKhenThuong,
                                      SoQuyetDinh, NgayQuyetDinh, NguoiKy, NoiDung_KhenThuong,
                                      TienThuong, NgayCapPhat, CanBoCapPhat, NguoiDaiDienNhan, GhiChu)
                                     VALUES
                                     (@STT, @TenTapThe, @HinhThuc_KhenThuong, @DonVi_CapKhenThuong,
                                      @SoQuyetDinh, @NgayQuyetDinh, @NguoiKy, @NoiDung_KhenThuong,
                                      @TienThuong, @NgayCapPhat, @CanBoCapPhat, @NguoiDaiDienNhan, @GhiChu)";
                        using (var cmd = new SqliteCommand(sqlInsert, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@STT", newStt);
                            ThemThamSoKhenThuong(cmd, kt);
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
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Thêm khen thưởng tập thể", $"Tập thể: {kryptonTextBox_TenTapThe.Text?.Trim()}");
                if (IsDisposed) return;
                _ = ShowHanhDongStatusAsync("✔ Thêm mới dữ liệu thành công!", true);
                // ================= FIX LỖI Ở ĐÂY =================
                _globalCache = null; // 1. XÓA SẠCH CACHE CŨ TRÊN RAM
                await LoadComboBoxDonViAsync(); // 2. ÉP NẠP LẠI CACHE TỪ DB (Gồm cả dữ liệu vừa thêm)
                await LoadComboBoxHinhThucKTAsync();
                await LoadDataToGridAsync(); // 3. GRID SẼ HIỂN THỊ DỮ LIỆU MỚI NHẤT TỪ CACHE
                ResetInput();
            }
            catch (Exception ex)
            {
                _ = ShowHanhDongStatusAsync("❌ Lỗi khi thêm mới dữ liệu!", false);
                MessageBox.Show("Lỗi khi thêm dữ liệu:\n" + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                kryptonButton_ThemKhenThuong.Enabled = true;
            }
        }
        private async void kryptonButton_SuaVaLuuKhenThuong_Click(object? sender, EventArgs e)
        {
            if (!isEditing || _selectedID < 0) return;
            if (!KiemTraDuLieuDauVao()) return;
            kryptonButton_SuaVaLuuKhenThuong.Enabled = false;
            try
            {
                // Chụp trên luồng UI: ô nhập + ID đang sửa (người dùng có thể bấm sang dòng khác trong lúc đang ghi)
                var kt = DocKhenThuongTuGiaoDien();
                int idSua = _selectedID;
                string connectionString = Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path);
                await Task.Run(() =>
                {
                    using var conn = new SqliteConnection(connectionString);
                    conn.Open();
                    string query = @"UPDATE ThongKe_KhenThuongTapThe SET
                             TenTapThe = @TenTapThe,
                             HinhThuc_KhenThuong = @HinhThuc_KhenThuong,
                             DonVi_CapKhenThuong = @DonVi_CapKhenThuong, SoQuyetDinh = @SoQuyetDinh,
                             NgayQuyetDinh = @NgayQuyetDinh, NguoiKy = @NguoiKy,
                             NoiDung_KhenThuong = @NoiDung_KhenThuong, TienThuong = @TienThuong,
                             NgayCapPhat = @NgayCapPhat, CanBoCapPhat = @CanBoCapPhat,
                             NguoiDaiDienNhan = @NguoiDaiDienNhan, GhiChu = @GhiChu
                             WHERE ID = @ID";
                    using var cmd = new SqliteCommand(query, conn);
                    cmd.Parameters.AddWithValue("@ID", idSua);
                    ThemThamSoKhenThuong(cmd, kt);
                    cmd.ExecuteNonQuery();
                });
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Sửa khen thưởng tập thể", $"ID={_selectedID} | Tập thể: {kryptonTextBox_TenTapThe.Text?.Trim()}");
                if (IsDisposed) return;
                _ = ShowHanhDongStatusAsync("✔ Cập nhật dữ liệu thành công!", true);
                // ================= FIX LỖI Ở ĐÂY =================
                _globalCache = null; // XÓA SẠCH CACHE CŨ
                await LoadComboBoxDonViAsync();
                await LoadComboBoxHinhThucKTAsync();
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
        private async void kryptonButton_XoaKhenThuong_Click(object? sender, EventArgs e)
        {
            if (!isEditing || _selectedID < 0) return;
            string tenTapThe = kryptonTextBox_TenTapThe.Text?.Trim() ?? string.Empty;
            if (MessageBox.Show($"Bạn có chắc chắn muốn xóa bản ghi:\n{tenTapThe}?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
            {
                return;
            }
            kryptonButton_XoaKhenThuong.Enabled = false;
            try
            {
                int idXoa = _selectedID;   // chụp trên luồng UI (sau hộp xác nhận)
                string connectionString = Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path);
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
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Xóa khen thưởng tập thể", $"ID={idXoa} | Tập thể: {tenTapThe}");
                if (IsDisposed) return;
                await DanhLaiSoThuTuAsync();
                _ = ShowHanhDongStatusAsync("✔ Xóa dữ liệu thành công!", true);
                // ================= FIX LỖI Ở ĐÂY =================
                _globalCache = null; // XÓA SẠCH CACHE BẢN GHI ĐÃ BỊ XÓA
                await LoadComboBoxDonViAsync();
                await LoadComboBoxHinhThucKTAsync();
                await LoadDataToGridAsync();
                ResetInput();
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
        // CÁC HÀM TIỆN ÍCH & TỐI ƯU CƠ SỞ DỮ LIỆU
        // Chạy ở LUỒNG NỀN (không đụng control): OpenAsync/ExecuteNonQueryAsync của Microsoft.Data.Sqlite vẫn chạy đồng bộ
        // trên luồng gọi, nên trước đây lệnh UPDATE toàn bảng này chạy ngay trên luồng giao diện.
        private Task DanhLaiSoThuTuAsync()
        {
            string connectionString = Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path);
            return Task.Run(() => DanhLaiSoThuTu(connectionString));
        }
        private static async Task DanhLaiSoThuTu(string connectionString)
        {
            using (var conn = new SqliteConnection(connectionString))
            {
                await conn.OpenAsync();
                // Cú pháp UPDATE - FROM của SQLite xử lý cực nhanh vì nó join bảng ảo chỉ 1 lần.
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
        private void CapNhatNhanTongSo()
        {
            int total = kryptonDataGridView1.Rows.Count;
            if (toolStripStatusLabel1 != null)
            {
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
        public void KiemTraVaTaoBangThongKeKhenThuongTapThe()
        {
            string connectionString = Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path);
            using (SqliteConnection conn = new SqliteConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string checkTableQuery = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='ThongKe_KhenThuongTapThe'";
                    using (SqliteCommand cmdCheck = new SqliteCommand(checkTableQuery, conn))
                    {
                        int tableCount = Convert.ToInt32(cmdCheck.ExecuteScalar());
                        if (tableCount == 0)
                        {
                            string createTableQuery = @"
                            CREATE TABLE IF NOT EXISTS ""ThongKe_KhenThuongTapThe"" (
                                ""ID"" INTEGER,
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
                                ""GhiChu"" TEXT,
                                PRIMARY KEY(""ID"")
                            );
                            CREATE INDEX IF NOT EXISTS ""idx_kt_tentapthe"" ON ""ThongKe_KhenThuongTapThe"" (""TenTapThe"");
                            ";
                            using (SqliteCommand cmdCreate = new SqliteCommand(createTableQuery, conn)) { cmdCreate.ExecuteNonQuery(); }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Lỗi Khởi tạo DB Form49]: {ex.Message}");
                    MessageBox.Show("Lỗi kết nối cơ sở dữ liệu. Vui lòng kiểm tra phân quyền thư mục hoặc tệp CSDL.\n\nChi tiết: " + ex.Message,
                        "Lỗi nghiêm trọng", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        // CẤU HÌNH GIAO DIỆN
        private void CauHinhGridCoBan(DataGridView dgv)
        {
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
            // ⭐ THÊM DÒNG NÀY ĐỂ CÁC CỘT TỰ ĐỘNG DÃN ĐẦY BỀ NGANG
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            // --- THÊM CÁC THUỘC TÍNH KHÓA CỘT TẠI ĐÂY ---
            dgv.AllowUserToOrderColumns = false;  // Khóa: Không cho kéo thả đổi vị trí cột
            dgv.AllowUserToResizeColumns = false; // Khóa: Không cho kéo giãn thay đổi độ rộng cột
        }
        private void CauHinhStyleWeb(DataGridView dgv) { dgv.RowTemplate.Height = 48; dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None; dgv.AllowUserToResizeRows = false; dgv.ColumnHeadersHeight = 60; dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing; dgv.CellBorderStyle = DataGridViewCellBorderStyle.Single; dgv.GridColor = Color.FromArgb(224, 224, 224); dgv.RowsDefaultCellStyle.BackColor = Color.White; dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252); dgv.BackgroundColor = Color.White; if (dgv is Krypton.Toolkit.KryptonDataGridView kDgv) { kDgv.GridStyles.Style = Krypton.Toolkit.DataGridViewStyle.List; kDgv.StateCommon.HeaderColumn.Content.Padding = new System.Windows.Forms.Padding(6, 8, 6, 8); kDgv.StateCommon.HeaderColumn.Content.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 9F, System.Drawing.FontStyle.Bold); kDgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; kDgv.StateCommon.DataCell.Content.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 9F, System.Drawing.FontStyle.Regular); kDgv.StateCommon.DataCell.Content.Padding = new System.Windows.Forms.Padding(6, 8, 6, 8); kDgv.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All; kDgv.StateCommon.DataCell.Border.Color1 = System.Drawing.Color.FromArgb(224, 224, 224); kDgv.StateCommon.DataCell.Border.Width = 1; kDgv.StateSelected.DataCell.Back.Color1 = System.Drawing.Color.FromArgb(232, 244, 253); kDgv.StateSelected.DataCell.Back.Color2 = System.Drawing.Color.FromArgb(232, 244, 253); kDgv.StateSelected.DataCell.Content.Color1 = System.Drawing.Color.FromArgb(0, 102, 204); kDgv.Margin = new Padding(0, 0, 0, 30); } }
        private void CauHinhCotGrid(DataGridView dgv)
        {
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
        private void kryptonButton2_TangCoChuRichText_Click(object? sender, EventArgs e)
        {
            float currentSize = richTextBox1_NoiDungKhenThuong.Font.Size;
            if (currentSize < 36.0f) { richTextBox1_NoiDungKhenThuong.Font = new Font(richTextBox1_NoiDungKhenThuong.Font.FontFamily, currentSize + 2, richTextBox1_NoiDungKhenThuong.Font.Style); }
        }
        private void kryptonButton2_GiamCoChuRichText_Click(object? sender, EventArgs e)
        {
            float currentSize = richTextBox1_NoiDungKhenThuong.Font.Size;
            if (currentSize > 8.0f) { richTextBox1_NoiDungKhenThuong.Font = new Font(richTextBox1_NoiDungKhenThuong.Font.FontFamily, currentSize - 2, richTextBox1_NoiDungKhenThuong.Font.Style); }
        }
        private Task LoadComboBoxHinhThucKTAsync(bool forceReload = false) => BuildGlobalCacheAsync(forceReload);
        private async void xoaToanBoDuLieu_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!System.IO.File.Exists(_csdl4Path))
                {
                    MessageBox.Show("Không tìm thấy tệp cơ sở dữ liệu!", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                int tongSoDong = 0;
                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path)))
                {
                    await conn.OpenAsync();
                    using (var cmd = new Microsoft.Data.Sqlite.SqliteCommand("SELECT COUNT(*) FROM ThongKe_KhenThuongTapThe", conn))
                    {
                        tongSoDong = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }
                }
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
                "Dữ liệu đã xóa không thể khôi phục.",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes) return;
            try
            {
                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path)))
                {
                    await conn.OpenAsync();
                    using (var tran = conn.BeginTransaction())
                    {
                        try
                        {
                            using (var cmd = new SqliteCommand("DELETE FROM ThongKe_KhenThuongTapThe", conn, tran))
                            {
                                await cmd.ExecuteNonQueryAsync();
                            }
                            tran.Commit();
                        }
                        catch
                        {
                            tran.Rollback();
                            throw;
                        }
                    }
                }
                try
                {
                    Module_NhatKy.GhiNhatKy(
                        Module_TaiKhoan.TenTaiKhoan_RAM,
                        "Xóa TOÀN BỘ dữ liệu khen thưởng Tập thể",
                        $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                    );
                }
                catch { }
                _ = ShowHanhDongStatusAsync("✔ Đã xóa sạch toàn bộ dữ liệu!", true);
                await BuildGlobalCacheAsync(forceReload: true);   // đọc lại bảng (đã trống) TRƯỚC khi gắn lưới, kẻo lưới hiện bộ đệm cũ
                await LoadDataToGridAsync();
                ResetInput();
            }
            catch (Exception ex)
            {
                _ = ShowHanhDongStatusAsync("❌ Lỗi khi xóa dữ liệu!", false);
                MessageBox.Show("Lỗi trong quá trình xóa dữ liệu:\n\n" + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void xoaTimKiem_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            kryptonButton_LamMoiCacOTimKiem.PerformClick();
        }
        private async void xuatDuLieuRaTepExcel_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (kryptonDataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var sfd = new SaveFileDialog
            {
                Title = "Chọn nơi lưu file Excel thống kê",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"ThongKe_KhenThuongTapThe_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;
            string filePath = sfd.FileName;
            string? textBanDau = xuatDuLieuRaTepExcel_ToolStripMenuItem.Text;
            Form_Loading frmLoad = new Form_Loading("Đang tạo tệp Excel, vui lòng đợi...");
            bool isLoadShown = false;
            frmLoad.Icon = this.Icon;
            try
            {
                this.Enabled = false;
                xuatDuLieuRaTepExcel_ToolStripMenuItem.Text = "Đang xuất...";
                frmLoad.Show(this);
                isLoadShown = true;
                await Task.Delay(50);
                // Lấy data đẩy vào List (Chuẩn bị cho InsertData)
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
                try
                {
                    Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Xuất Excel bảng thống kê khen thưởng Tập thể", $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
                }
                catch { }
                await Task.Run(() =>
                {
                    using var wb = new ClosedXML.Excel.XLWorkbook();
                    var ws = wb.Worksheets.Add("ThongKeKhenThuongTapThe");
                    ws.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                    ws.Style.Font.FontSize = 11;
                    ws.Cell("A1").Value = "DANH SÁCH";
                    ws.Range(1, 1, 1, colCount).Merge().Style.Font.SetBold().Font.SetFontSize(14)
                        .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);
                    ws.Cell("A2").Value = "THỐNG KÊ KẾT QUẢ KHEN THƯỞNG TẬP THỂ";
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
                _ = ShowHanhDongStatusAsync("✔ Xuất tệp Excel thành công!", true);
            }
            catch (Exception ex)
            {
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
    }
}