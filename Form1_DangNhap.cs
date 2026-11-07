/// Phiên bản 2
/// 
using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using BCrypt.Net;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form1 : Form
    {
        // ================= TRẠNG THÁI & CẤU HÌNH =================
        private readonly string _csdl1Path = Module_DanduongGPS.DuongDanCSDL1;
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        // Chuỗi kết nối dựng 1 lần (dùng chung 1 pool, không nối chuỗi lặp lại mỗi lần gọi)
        private readonly string _cs1Doc;
        private readonly string _cs1Ghi;
        private readonly string _cs2Doc;
        private readonly Icon? _appIcon;
        public DateTime ThoiGianDangNhap { get; private set; }
        private const int GioiHanGoiYMatKhau = 1;
        private int _soLanDaGoiYMatKhau = 0;
        private bool _dangXuLyGoiYMatKhau = false;
        private int demSai = 0;        // Đếm số lần đăng nhập sai
        // 🌟 UX: HIỆU ỨNG FOCUS CHUẨN WINDOWS 10/11
        private static readonly Color FocusBorderColor = Color.FromArgb(0, 120, 215); // Xanh dương Win 11
        private static readonly Color NormalBorderColor = Color.Silver;
        private const int FocusBorderWidth = 2;
        private const int NormalBorderWidth = 1;
        private bool _daKhoiTaoToolTip = false;
        private KryptonTextBox[] _oNhap = Array.Empty<KryptonTextBox>();
        // Checkbox "hiện mật khẩu": phân biệt người dùng bấm với code gán giá trị nạp từ CSDL
        private bool _dangApDungCauHinh = false;
        private bool _nguoiDungDaDoiCheck = false;
        // Lưu cấu hình: tuần tự hóa các lần ghi, chỉ tạo bảng 1 lần / phiên chạy
        private static readonly SemaphoreSlim _khoaLuuCauHinh = new(1, 1);
        private static bool _daDamBaoBangCheckKhoiDong = false;
        private static volatile bool _giaTriCanLuu = false;
        // Bo góc
        private const int BanKinhBoGoc = 18; // càng lớn càng tròn (thử 12 – 24)
        private Size _kichThuocBoGoc = Size.Empty;
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int l, int t, int r, int b, int w, int h);
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr obj);
        private enum KetQuaXacThuc { Sai, Admin, ThanhVien }
        private sealed class CauHinhKhoiDong
        {
            public string? GoiY;   // ChoPhepGoiYMatKhau (đang mã hóa)
            public string? Link;   // LinkLabel1_DangKyTaiKhoanMoi (đang mã hóa)
            public string? TenTk;  // Admin.TenTaiKhoan ID=1 (đang mã hóa)
            public string? HienMk; // Check_KhoiDong ID=1 (đang mã hóa)
            // Bản chụp phục vụ bấm PictureBox1: chỉ là CHUỖI MÃ HÓA, KHÔNG giữ mật khẩu rõ trong RAM
            public bool GoiYAnh_DaNap;      // true = đọc cả 2 CSDL thành công (lỗi/thiếu cột -> false -> lúc bấm đọc lại như cũ)
            public bool GoiYAnh_CoFile;
            public string? GoiYAnh_Flag;    // ChoPhepDupChuotVaoAnh_GoiYMatKhau (đang mã hóa)
            public string? GoiYAnh_Pass;    // Admin.MatKhau ID=1 (đang mã hóa / BCrypt)
        }
        // Bản chụp bất biến, nạp sẵn ở luồng nền lúc khởi động; PictureBox1_Click dùng ngay, không đọc ổ đĩa.
        private sealed class GoiYMatKhauSnapshot
        {
            public bool CoFile;
            public string? Flag;
            public string? Pass;
        }
        private volatile GoiYMatKhauSnapshot? _goiYSnap;
        public Form1()
        {
            _cs1Doc = TaoChuoiKetNoi(_csdl1Path, "ReadOnly", 10);
            _cs1Ghi = TaoChuoiKetNoi(_csdl1Path, "ReadWrite", 5);
            _cs2Doc = TaoChuoiKetNoi(_csdl2Path, "ReadOnly", 10);
            InitializeComponent();
            ApplyPerformanceTuning();
            this.Load += Form1_Load;
            this.Shown += Form1_Show;
            // Chuyển đổi byte[] từ Resource sang Icon chuẩn
            try
            {
                using (var ms = new System.IO.MemoryStream(Properties.Resources.ic_PhanMem))
                {
                    _appIcon = new Icon(ms);
                }
                this.Icon = _appIcon;
            }
            catch (Exception)
            {
                // Nếu lỗi, dùng icon mặc định để không crash app
            }
            this.FormBorderStyle = FormBorderStyle.None;
            this.MaximizeBox = false;
            text_TenDangNhap.KeyDown += Text_TenDangNhap_KeyDown;
            text_MatKhau.KeyDown += Text_MatKhau_KeyDown;
            InitKeoDiChuyenForm();
        }
        private static string TaoChuoiKetNoi(string path, string mode, int timeoutGiay)
            => $"Data Source={path};Mode={mode};Default Timeout={timeoutGiay};Pooling=True;";
        // ================= LOAD / SHOWN =================
        // Mọi thay đổi giao diện nằm ở Load (trước khi hiện form) → khung hình đầu tiên đã hoàn chỉnh,
        // không có chuyện "vẽ rồi mới sửa". Việc nặng chạy nền sau khi form đã hiện.
        private void Form1_Load(object? sender, EventArgs e)
        {
            try
            {
                label3_HienThiTenPhanMem.Text = "Phần mềm thi đua năm " + Module_HeThong.LayNamHeThong();
                text_MatKhau.UseSystemPasswordChar = true;
                this.CenterToScreen();
                string phienBan = Module_TaiKhoan.LayPhienBanPhanMem();
                bool laTanBinh = LaPhienBanTanBinh(phienBan);
                if (laTanBinh) label1_ThongBaoPhienBan.Text = phienBan;
                label1_ThongBaoPhienBan.Visible = laTanBinh;
                SetTableLayoutPanelColor_ChinhSach(TableLayoutPanel6, phienBan);
                // Nhãn phiên bản: gán trước khi hiện form (tránh nhảy vị trí sau khi đã hiện)
                label1_PhienBanPhanMem.AutoSize = true;
                label1_PhienBanPhanMem.Text = "Phiên bản: " + Module_PhienBan.SoftwareVersion;
                label1_PhienBanPhanMem.Visible = true;
                label1_PhienBanPhanMem.BringToFront();
                PictureBox1.Click -= PictureBox1_Click;
                PictureBox1.Click += PictureBox1_Click;
                Check_HienMatKhau.ForeColor = Color.Red; // mặc định: chưa tick
                Check_HienMatKhau.CheckedChanged -= Check_HienMatKhau_CheckedChanged;
                Check_HienMatKhau.CheckedChanged += Check_HienMatKhau_CheckedChanged;
                InitFocusEffects();
                ActiveControl = text_TenDangNhap; // focus sẵn lúc hiện, không cần Focus() sau Shown
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi khởi tạo UI Form1: " + ex.Message);
            }
        }
        private void Form1_Show(object? sender, EventArgs e)
        {
            if (IsDisposed) return;
            _ = ChayCacTacVuNangNgamAsync();
        }
        private static bool LaPhienBanTanBinh(string? phienBan)
            => !string.IsNullOrWhiteSpace(phienBan) &&
               phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase);
        private void SetTableLayoutPanelColor_ChinhSach(TableLayoutPanel panel, string phienBan)
        {
            panel.BackColor = LaPhienBanTanBinh(phienBan) ? Color.LightGreen : Color.FromArgb(192, 192, 255);
        }
        // ================= TÁC VỤ NỀN SAU KHI HIỆN FORM =================
        private async Task ChayCacTacVuNangNgamAsync()
        {
            try
            {
                // Chờ khung hình đầu vẽ xong (máy yếu vẽ chậm hơn) rồi mới làm việc nền
                await Task.Delay(250);
                if (IsDisposed) return;
                InitToolTips();
                // 1 lượt đọc nền duy nhất cho cả 2 CSDL (thay cho 3 tác vụ mở kết nối riêng lẻ,
                // trong đó có 1 lệnh đọc đồng bộ trên luồng UI). Chỉ ĐỌC chuỗi thô ở luồng nền;
                // giải mã và gán UI làm trên luồng UI (không đổi mô hình luồng của Module_BaoMatAES).
                string p1 = _csdl1Path, p2 = _csdl2Path, c1 = _cs1Doc, c2 = _cs2Doc;
                // Làm nóng thư viện mật mã (nạp bcrypt + JIT) SONG SONG với việc đọc CSDL -> lần giải mã đầu tiên trên luồng UI không bị khựng.
                // Idempotent: nếu nơi khác (Program.cs) đã gọi KhoiDongNong thì gọi thêm lần này vô hại.
                Task tacVuNong = Task.Run(Module_BaoMatAES.KhoiDongNong);
                CauHinhKhoiDong cfg = await Task.Run(() => DocCauHinhKhoiDong(p1, p2, c1, c2));
                await tacVuNong;   // KhoiDongNong tự bắt mọi lỗi, không ném ngoại lệ
                if (IsDisposed) return;
                ApDungCauHinhKhoiDong(cfg);
                // SAU KHI DB XONG VÀ CPU RẢNH: nạp trước Form 2 (chia nhỏ từng bước trên luồng UI)
                _ = Form2Preloader.NapTruocAsync(() => IsDisposed);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Lỗi Load Ngầm Form 1]: " + ex.Message);
            }
        }
        /// <summary>Chạy ở luồng nền. Mỗi giá trị đọc cô lập lỗi: thiếu cột/bảng chỉ làm giá trị đó = null.</summary>
        private static CauHinhKhoiDong DocCauHinhKhoiDong(string p1, string p2, string cs1, string cs2)
        {
            var kq = new CauHinhKhoiDong();
            try
            {
                if (!string.IsNullOrWhiteSpace(p2) && File.Exists(p2))
                {
                    using var cn = new SqliteConnection(cs2);
                    cn.Open();
                    kq.GoiY = DocChuoi(cn, "SELECT ChoPhepGoiYMatKhau FROM ThongTin WHERE ID = 1 LIMIT 1", false);
                    kq.Link = DocChuoi(cn, "SELECT LinkLabel1_DangKyTaiKhoanMoi FROM ThongTin WHERE ID = 1", false);
                }
            }
            catch (Exception ex) { Debug.WriteLine("[DocCauHinh CSDL2] " + ex.Message); }
            try
            {
                if (!string.IsNullOrWhiteSpace(p1) && File.Exists(p1))
                {
                    using var cn = new SqliteConnection(cs1);   // chỉ đọc: không còn ghi/tạo bảng mỗi lần mở app
                    cn.Open();
                    kq.TenTk = DocChuoi(cn, "SELECT TenTaiKhoan FROM Admin WHERE ID = 1 LIMIT 1", false);
                    kq.HienMk = DocChuoi(cn, "SELECT GiaTri_Luu FROM Check_KhoiDong WHERE ID = 1 LIMIT 1", false); // chưa có bảng → null → mặc định false
                }
            }
            catch (Exception ex) { Debug.WriteLine("[DocCauHinh CSDL1] " + ex.Message); }
            // Nạp sẵn chuỗi MÃ HÓA cho PictureBox1 (nếu lỗi: để GoiYAnh_DaNap = false, lúc bấm sẽ đọc lại và báo lỗi như cũ)
            try
            {
                var r = DocGoiYMatKhauRaw(p1, p2, cs1, cs2);
                kq.GoiYAnh_CoFile = r.CoFile;
                kq.GoiYAnh_Flag = r.Flag;
                kq.GoiYAnh_Pass = r.Pass;
                kq.GoiYAnh_DaNap = true;
            }
            catch (Exception ex) { Debug.WriteLine("[DocCauHinh GoiYAnh] " + ex.Message); }
            return kq;
        }
        private static string? DocChuoi(SqliteConnection cn, string sql, bool nemLoi)
        {
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.CommandText = sql;
                object? v = cmd.ExecuteScalar();
                return (v == null || v == DBNull.Value) ? null : Convert.ToString(v);
            }
            catch
            {
                if (nemLoi) throw;
                return null;
            }
        }
        private static string GiaiMaAnToan(string? s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            try { return Module_BaoMatAES.GiaiMa(s) ?? string.Empty; }
            catch { return string.Empty; }
        }
        /// <summary>Luồng UI: áp dụng cấu hình đã đọc.</summary>
        private void ApDungCauHinhKhoiDong(CauHinhKhoiDong cfg)
        {
            // Lưu bản chụp (chuỗi mã hóa) để PictureBox1_Click phản hồi tức thì
            _goiYSnap = cfg.GoiYAnh_DaNap
                ? new GoiYMatKhauSnapshot { CoFile = cfg.GoiYAnh_CoFile, Flag = cfg.GoiYAnh_Flag, Pass = cfg.GoiYAnh_Pass }
                : null;
            // Link đăng ký: ẩn khi cấu hình = "TRUE", mặc định hiện
            LinkLabel1_DangKyTaiKhoanMoi.Visible =
                !string.Equals(GiaiMaAnToan(cfg.Link), "TRUE", StringComparison.Ordinal);
            // Checkbox hiện mật khẩu: bỏ qua nếu người dùng đã tự bấm trong lúc chờ nạp
            if (!_nguoiDungDaDoiCheck)
            {
                bool hienMk = string.Equals(GiaiMaAnToan(cfg.HienMk), "TRUE", StringComparison.OrdinalIgnoreCase);
                if (Check_HienMatKhau.Checked != hienMk)
                {
                    _dangApDungCauHinh = true;   // gán bằng code → KHÔNG ghi ngược vào CSDL
                    try { Check_HienMatKhau.Checked = hienMk; }
                    finally { _dangApDungCauHinh = false; }
                }
            }
            // Gợi ý tên tài khoản: chỉ điền khi ô còn trống (không đè lên thứ người dùng đang gõ)
            bool choPhepGoiY = string.Equals(GiaiMaAnToan(cfg.GoiY), "TRUE", StringComparison.OrdinalIgnoreCase);
            if (choPhepGoiY && string.IsNullOrEmpty(text_TenDangNhap.Text))
            {
                string tenTK = GiaiMaAnToan(cfg.TenTk);
                if (tenTK.Length > 0)
                {
                    text_TenDangNhap.Text = tenTK;
                    if (string.IsNullOrEmpty(text_MatKhau.Text)) text_MatKhau.Focus(); // nhảy xuống ô Mật khẩu
                }
            }
        }
        // ================= HIỆU ỨNG FOCUS =================
        private void InitFocusEffects()
        {
            try
            {
                _oNhap = new[] { text_TenDangNhap, text_MatKhau };
                foreach (var ktb in _oNhap)
                {
                    if (ktb == null || ktb.IsDisposed) continue;
                    ktb.StateCommon.Border.DrawBorders = PaletteDrawBorders.All;
                    ktb.StateCommon.Border.Color1 = NormalBorderColor;
                    ktb.StateCommon.Border.Color2 = NormalBorderColor;
                    ktb.StateCommon.Border.Width = NormalBorderWidth;
                    // Chống Memory Leak: rút event trước khi gắn
                    ktb.Enter -= KryptonTextBox_EnterFocus;
                    ktb.Leave -= KryptonTextBox_LeaveFocus;
                    ktb.Enter += KryptonTextBox_EnterFocus;
                    ktb.Leave += KryptonTextBox_LeaveFocus;
                }
            }
            catch (Exception ex) { Debug.WriteLine("Init Focus Error: " + ex.Message); }
        }
        private void RemoveFocusEvents()
        {
            try
            {
                foreach (var ktb in _oNhap)
                {
                    if (ktb == null || ktb.IsDisposed) continue;
                    ktb.Enter -= KryptonTextBox_EnterFocus;
                    ktb.Leave -= KryptonTextBox_LeaveFocus;
                }
            }
            catch { }
        }
        private void KryptonTextBox_EnterFocus(object? sender, EventArgs e)
        {
            if (sender is KryptonTextBox ktb)
            {
                ktb.StateCommon.Border.Color1 = FocusBorderColor;
                ktb.StateCommon.Border.Color2 = FocusBorderColor;
                ktb.StateCommon.Border.Width = FocusBorderWidth;
            }
        }
        private void KryptonTextBox_LeaveFocus(object? sender, EventArgs e)
        {
            if (sender is KryptonTextBox ktb)
            {
                ktb.StateCommon.Border.Color1 = NormalBorderColor;
                ktb.StateCommon.Border.Color2 = NormalBorderColor;
                ktb.StateCommon.Border.Width = NormalBorderWidth;
            }
        }
        // ================= EVENT & NGHIỆP VỤ CHÍNH =================
        private void Text_TenDangNhap_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                text_MatKhau.Focus();
            }
        }
        private void Text_MatKhau_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                btn_DangNhap.PerformClick();
            }
        }
        private static bool LaBcrypt(string hash)
            => hash.StartsWith("$2a$", StringComparison.Ordinal) ||
               hash.StartsWith("$2b$", StringComparison.Ordinal) ||
               hash.StartsWith("$2y$", StringComparison.Ordinal);
        /// <summary>Chạy ở luồng nền: đọc cờ cho phép + mật khẩu (chuỗi thô, chưa giải mã).</summary>
        private static (bool CoFile, string? Flag, string? Pass) DocGoiYMatKhauRaw(string p1, string p2, string cs1, string cs2)
        {
            if (!File.Exists(p2) || !File.Exists(p1)) return (false, null, null);
            string? flag;
            using (var cn2 = new SqliteConnection(cs2))
            {
                cn2.Open();
                flag = DocChuoi(cn2, "SELECT ChoPhepDupChuotVaoAnh_GoiYMatKhau FROM ThongTin WHERE ID = 1", true);
            }
            if (string.IsNullOrWhiteSpace(flag)) return (true, flag, null);
            string? pass;
            using (var cn1 = new SqliteConnection(cs1))
            {
                cn1.Open();
                pass = DocChuoi(cn1, "SELECT MatKhau FROM Admin WHERE ID = 1 LIMIT 1", true);
            }
            return (true, flag, pass);
        }
        private async void PictureBox1_Click(object? sender, EventArgs e)
        {
            if (_dangXuLyGoiYMatKhau) return;
            _dangXuLyGoiYMatKhau = true;
            var sw = Stopwatch.StartNew();
            try
            {
                // 1. KIỂM TRA GIỚI HẠN
                if (_soLanDaGoiYMatKhau >= GioiHanGoiYMatKhau)
                {
                    text_MatKhau.Clear();
                    text_MatKhau.Focus();
                    return;
                }
                // 2. LẤY CHUỖI MÃ HÓA: ưu tiên bản đã nạp sẵn lúc khởi động (không await, không đụng ổ đĩa)
                bool coFile;
                string? flagEnc, passEnc;
                var snap = _goiYSnap;
                bool dungBanNapSan = snap != null;
                if (snap != null)
                {
                    coFile = snap.CoFile;
                    flagEnc = snap.Flag;
                    passEnc = snap.Pass;
                }
                else
                {
                    // Chưa nạp kịp / nạp lỗi -> đọc CSDL ở luồng nền như cũ (giữ nguyên cách báo lỗi cũ)
                    string p1 = _csdl1Path, p2 = _csdl2Path, c1 = _cs1Doc, c2 = _cs2Doc;
                    (coFile, flagEnc, passEnc) = await Task.Run(() => DocGoiYMatKhauRaw(p1, p2, c1, c2));
                    if (IsDisposed) return;
                }
                long msDoc = sw.ElapsedMilliseconds;
                if (!coFile) return;
                // 3. KIỂM TRA CHO PHÉP GỢI Ý
                string flag = string.IsNullOrWhiteSpace(flagEnc) ? "FALSE" : Module_BaoMatAES.GiaiMa(flagEnc);
                if (!string.Equals(flag, "TRUE", StringComparison.OrdinalIgnoreCase))
                {
                    text_MatKhau.Clear();
                    return;
                }
                // 4. MẬT KHẨU
                if (string.IsNullOrWhiteSpace(passEnc)) return;
                // [GIA CỐ BẢO MẬT]: Không thể giải mã ngược chuỗi BCrypt để xem lại mật khẩu
                if (LaBcrypt(passEnc))
                {
                    MessageBox.Show("Tài khoản này đã được nâng cấp lên mã hóa một chiều (BCrypt). Hệ thống không thể hiển thị lại mật khẩu bằng chữ gốc!",
                        "Bảo mật cấp cao", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    text_MatKhau.Clear();
                    return;
                }
                // 5. GIẢI MÃ (vài micro-giây khi thư viện đã được làm nóng) + HIỂN THỊ + TĂNG BỘ ĐẾM
                long msTruocGiaiMa = sw.ElapsedMilliseconds;
                string matKhau = Module_BaoMatAES.GiaiMa(passEnc);
                long msSauGiaiMa = sw.ElapsedMilliseconds;
                text_MatKhau.Text = matKhau;
                _soLanDaGoiYMatKhau++;
                _goiYSnap = null;   // chỉ dùng 1 lần (GioiHanGoiYMatKhau = 1): không giữ chuỗi mã hóa lâu hơn cần thiết
                Debug.WriteLine($"[PictureBox1] {(dungBanNapSan ? "nạp sẵn" : "đọc CSDL")}: lấy chuỗi {msDoc} ms, kiểm tra {msTruocGiaiMa - msDoc} ms, giải mã {msSauGiaiMa - msTruocGiaiMa} ms, tổng {sw.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                text_MatKhau.Clear();
                Module_ThongBao.Loi("Lỗi hiển thị mật khẩu: " + ex.Message);
            }
            finally
            {
                _dangXuLyGoiYMatKhau = false;
            }
        }
        // ================= CHECKBOX "HIỆN MẬT KHẨU" =================
        private async void Check_HienMatKhau_CheckedChanged(object? sender, EventArgs e)
        {
            bool bat = Check_HienMatKhau.Checked;
            Check_HienMatKhau.ForeColor = bat ? Color.Green : Color.Red;
            text_MatKhau.UseSystemPasswordChar = !bat;
            if (_dangApDungCauHinh) return;   // giá trị nạp từ CSDL → không ghi ngược lại
            _nguoiDungDaDoiCheck = true;
            await LuuTrangThaiHienMatKhauAsync(bat);
        }
        /// <summary>
        /// Ghi trạng thái vào CSDL1 ở luồng nền. Các lần bấm liên tiếp được xếp hàng tuần tự
        /// và luôn ghi giá trị mới nhất → kết quả cuối cùng luôn đúng, không tranh khóa SQLite.
        /// </summary>
        private async Task LuuTrangThaiHienMatKhauAsync(bool checkedState)
        {
            if (string.IsNullOrWhiteSpace(_csdl1Path)) return;
            _giaTriCanLuu = checkedState;
            await _khoaLuuCauHinh.WaitAsync();
            try
            {
                string encryptedValue = Module_BaoMatAES.MaHoa(_giaTriCanLuu ? "TRUE" : "FALSE");
                string cs = _cs1Ghi;
                await Task.Run(() => GhiCheckKhoiDong(cs, encryptedValue));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Lỗi lưu Check_KhoiDong] " + ex.Message);
            }
            finally
            {
                _khoaLuuCauHinh.Release();
            }
        }
        /// <summary>Luồng nền, được gọi tuần tự dưới semaphore nên truy cập cờ static là an toàn.</summary>
        private static void GhiCheckKhoiDong(string cs, string encryptedValue)
        {
            using var conn = new SqliteConnection(cs);
            conn.Open();
            if (!_daDamBaoBangCheckKhoiDong)
            {
                using var create = conn.CreateCommand();
                create.CommandText = "CREATE TABLE IF NOT EXISTS Check_KhoiDong (ID INTEGER NOT NULL, GiaTri_Luu TEXT, PRIMARY KEY(ID AUTOINCREMENT));";
                create.ExecuteNonQuery();
                _daDamBaoBangCheckKhoiDong = true;
            }
            using var cmd = conn.CreateCommand();
            // UPSERT: thêm mới nếu ID=1 chưa có, cập nhật nếu đã tồn tại
            cmd.CommandText = "INSERT INTO Check_KhoiDong (ID, GiaTri_Luu) VALUES (1, @Val) ON CONFLICT(ID) DO UPDATE SET GiaTri_Luu = @Val;";
            cmd.Parameters.AddWithValue("@Val", encryptedValue);
            cmd.ExecuteNonQuery();
        }
        // ================= ĐĂNG NHẬP =================
        private async void btn_DangNhap_Click(object? sender, EventArgs e)
        {
            // 1. NGĂN CHẶN SPAM CLICK
            btn_DangNhap.Enabled = false;
            try
            {
                // 2. RESET MÀU NỀN VÀ KIỂM TRA RỖNG
                text_TenDangNhap.StateCommon.Back.Color1 = Color.White;
                text_MatKhau.StateCommon.Back.Color1 = Color.White;
                string tenNhap = text_TenDangNhap.Text.Trim();
                string mkTho = text_MatKhau.Text;          // nguyên bản người dùng gõ
                string mkNhap = mkTho.Trim();              // bản cắt khoảng trắng (hành vi cũ)
                KryptonTextBox? oCanFocus = null;
                if (string.IsNullOrWhiteSpace(tenNhap))
                {
                    text_TenDangNhap.StateCommon.Back.Color1 = Color.LightGreen;
                    oCanFocus = text_TenDangNhap;
                }
                if (string.IsNullOrWhiteSpace(mkNhap))
                {
                    text_MatKhau.StateCommon.Back.Color1 = Color.LightGreen;
                    oCanFocus ??= text_MatKhau;
                }
                if (oCanFocus != null)
                {
                    oCanFocus.Focus();
                    MessageBox.Show("Vui lòng nhập đầy đủ tài khoản và mật khẩu!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                // 3. XÁC THỰC (BCrypt chạy ở luồng nền → form không đứng hình)
                KetQuaXacThuc kq = await XacThucAsync(tenNhap, mkNhap, mkTho);
                if (IsDisposed) return;
                switch (kq)
                {
                    case KetQuaXacThuc.Admin:
                        XacNhanDangNhapThanhCong(tenNhap, "Đăng nhập thành công (Quyền Admin cao nhất)");
                        break;
                    case KetQuaXacThuc.ThanhVien:
                        XacNhanDangNhapThanhCong(tenNhap, "Đăng nhập thành công");
                        break;
                    default:
                        XulyDangNhapSai(tenNhap);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối hoặc xử lý dữ liệu: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    btn_DangNhap.Enabled = true;
                }
            }
        }
        private async Task<KetQuaXacThuc> XacThucAsync(string tenNhap, string mkNhap, string mkTho)
        {
            await using var conn = new SqliteConnection(_cs1Doc);
            await conn.OpenAsync();
            // BƯỚC 1: TÀI KHOẢN ADMIN CAO NHẤT (lỗi dữ liệu ở bước này không chặn tài khoản thường)
            try
            {
                await using var cmdAdmin = new SqliteCommand("SELECT encAdminUs, encAdminPa FROM Admin_DefaultValue LIMIT 1", conn);
                await using var rd = await cmdAdmin.ExecuteReaderAsync();
                if (await rd.ReadAsync())
                {
                    string reversedMixed1 = rd.IsDBNull(0) ? "" : (Convert.ToString(rd.GetValue(0)) ?? "").Trim();
                    string reversedMixed2 = rd.IsDBNull(1) ? "" : (Convert.ToString(rd.GetValue(1)) ?? "").Trim();
                    string mixed1 = Module_BaoMatAES.TraLaiTenChoMeoCam(reversedMixed1).Trim();
                    string mixed2 = Module_BaoMatAES.TraLaiTenChoMeoCam(reversedMixed2).Trim();
                    if (mixed1.Length == 47 && mixed2.Length == 47)
                    {
                        string encAdminUser = mixed1.Substring(0, 24) + mixed2.Substring(24);
                        string encAdminPass = mixed2.Substring(0, 24) + mixed1.Substring(24);
                        string adminUserGiaiMa = Module_BaoMatAES.GiaiMa(encAdminUser).Trim();
                        string adminPassGiaiMa = Module_BaoMatAES.GiaiMa(encAdminPass).Trim();
                        if (string.Equals(tenNhap, adminUserGiaiMa, StringComparison.Ordinal) &&
                            string.Equals(mkNhap, adminPassGiaiMa, StringComparison.Ordinal))
                        {
                            return KetQuaXacThuc.Admin;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[XacThuc Admin] " + ex.Message);
            }
            // BƯỚC 2: TÀI KHOẢN THƯỜNG TRONG BẢNG ADMIN
            await using var cmdUser = new SqliteCommand("SELECT TenTaiKhoan, MatKhau FROM Admin", conn);
            await using var rdUser = await cmdUser.ExecuteReaderAsync();
            while (await rdUser.ReadAsync())
            {
                if (rdUser.IsDBNull(0) || rdUser.IsDBNull(1)) continue;
                string userGiaiMa;
                try { userGiaiMa = Module_BaoMatAES.GiaiMa(rdUser.GetString(0)).Trim(); }
                catch { continue; } // Bỏ qua nếu lỗi giải mã Tên đăng nhập
                if (!string.Equals(tenNhap, userGiaiMa, StringComparison.Ordinal)) continue;
                string dbPassData = rdUser.GetString(1).Trim();
                if (LaBcrypt(dbPassData))
                {
                    // BCrypt cố ý chạy chậm (~100–300ms, lâu hơn trên máy yếu) → đẩy sang luồng nền
                    if (await Task.Run(() => BcryptKhop(dbPassData, mkNhap, mkTho)))
                        return KetQuaXacThuc.ThanhVien;
                }
                else
                {
                    // Fallback: AES cũ (cho tài khoản chưa nâng cấp BCrypt)
                    try
                    {
                        string passGiaiMa = Module_BaoMatAES.GiaiMa(dbPassData).Trim();
                        if (string.Equals(mkNhap, passGiaiMa, StringComparison.Ordinal))
                            return KetQuaXacThuc.ThanhVien;
                    }
                    catch { /* Bỏ qua lỗi giải mã AES */ }
                }
            }
            return KetQuaXacThuc.Sai;
        }
        /// <summary>Thử bản đã Trim trước (hành vi cũ), rồi bản nguyên gốc nếu khác nhau (mật khẩu có khoảng trắng ở rìa).</summary>
        private static bool BcryptKhop(string hash, string mkDaTrim, string mkNguyenGoc)
        {
            try
            {
                if (BCrypt.Net.BCrypt.Verify(mkDaTrim, hash)) return true;
                return !string.Equals(mkDaTrim, mkNguyenGoc, StringComparison.Ordinal)
                       && BCrypt.Net.BCrypt.Verify(mkNguyenGoc, hash);
            }
            catch
            {
                return false; // hash sai định dạng → coi như không khớp
            }
        }
        private void InitToolTips()
        {
            // Chống gọi lại nhiều lần không cần thiết
            if (_daKhoiTaoToolTip) return;
            if (toolTip1 == null) return;
            try
            {
                toolTip1.IsBalloon = true;
                toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
                toolTip1.ToolTipIcon = ToolTipIcon.Info;
                // UX: phản hồi nhanh – không gây khó chịu khi rê chuột qua
                toolTip1.InitialDelay = 300;
                toolTip1.AutoPopDelay = 2500;
                toolTip1.ReshowDelay = 100;
                toolTip1.ShowAlways = true;
                (Control? control, string noiDung)[] danhSachToolTip =
                {
                    (Check_HienMatKhau, "Hiển thị / Ẩn mật khẩu"),
                    (btn_DangNhap,      "Đăng nhập vào hệ thống"),
                    (btn_Thoat,         "Thoát chương trình")
                };
                foreach (var (control, noiDung) in danhSachToolTip)
                {
                    if (control == null || control.IsDisposed) continue;
                    try { toolTip1.SetToolTip(control, noiDung); }
                    catch { /* lỗi 1 control không làm hỏng các control khác */ }
                }
                _daKhoiTaoToolTip = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi nghiêm trọng tại InitToolTips]: {ex.Message}");
            }
        }
        private void btn_Thoat_Click(object sender, EventArgs e)
        {
            this.Close();
            Application.Exit();
        }
        private void LinkLabel1_DangKyTaiKhoanMoi_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            if (!LinkLabel1_DangKyTaiKhoanMoi.Enabled) return;
            // CỜ KIỂM TRA ĐỂ AUTO LOGIN
            bool tuDongDangNhap = false;
            try
            {
                LinkLabel1_DangKyTaiKhoanMoi.Enabled = false;
                using var frm = new Form3_DangKyTaiKhoan
                {
                    Owner = this,
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.CenterParent
                };
                this.Hide();
                var result = frm.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    tuDongDangNhap = true;
                    string csdlPath = _csdl2Path;
                    if (string.IsNullOrWhiteSpace(csdlPath) || !File.Exists(csdlPath))
                        throw new Exception("Không tìm thấy CSDL cấu hình.");
                    // 🛡️ SQLITE SAFETY: có Timeout và Pooling
                    string connStr = TaoChuoiKetNoi(csdlPath, "ReadWrite", 15);
                    using var conn = new SqliteConnection(connStr);
                    conn.Open();
                    using var tran = conn.BeginTransaction();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tran;
                        cmd.CommandText = @"INSERT INTO ThongTin (ID) SELECT 1 WHERE NOT EXISTS (SELECT 1 FROM ThongTin WHERE ID = 1);";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = @"UPDATE ThongTin SET LinkLabel1_DangKyTaiKhoanMoi = @Value WHERE ID = 1;";
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@Value", Module_BaoMatAES.MaHoa("TRUE"));
                        cmd.ExecuteNonQuery();
                    }
                    tran.Commit();
                    LinkLabel1_DangKyTaiKhoanMoi.Visible = false;
                }
            }
            catch (Exception ex)
            {
                Module_ThongBao.Loi("Lỗi xử lý đăng ký: " + ex.Message);
            }
            finally
            {
                // CHỈ HIỆN LẠI FORM 1 NẾU NGƯỜI DÙNG HỦY ĐĂNG KÝ
                if (!tuDongDangNhap && !this.IsDisposed && this.IsHandleCreated)
                {
                    this.Show();
                    this.Activate();
                }
                LinkLabel1_DangKyTaiKhoanMoi.Enabled = true;
            }
            // AUTO LOGIN MỞ FORM 2 SAU KHI FINALLY ĐÃ XỬ LÝ XONG
            if (tuDongDangNhap)
            {
                var frmTrangChu = Form2Preloader.Lay();
                frmTrangChu.ApDungPhienDangNhap();
                frmTrangChu.Show();
                // Vẫn giữ this.Hide() cho Form1 (không Close vì Form1 là form khởi chạy ở Program.cs,
                // đóng nó sẽ tắt luôn cả phần mềm).
            }
        }
        private void LinkLabel_QuenMatKhau_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            this.Hide();
            using (Form5_QuenPass frm5 = new Form5_QuenPass())
            {
                frm5.StartPosition = FormStartPosition.CenterScreen;
                DialogResult ketQua = frm5.ShowDialog(this);
                if (ketQua == DialogResult.OK)
                {
                    DanhThucFrom2();
                    ThoiGianDangNhap = DateTime.Now;
                }
                else if (ketQua == DialogResult.Abort)
                {
                    Application.Exit();
                }
                else
                {
                    if (!this.IsDisposed && this.IsHandleCreated)
                    {
                        this.Show();
                        this.Activate();
                    }
                }
            }
        }
        private void DanhThucFrom2()
        {
            // Lấy Form2 đã nạp trước (nếu chưa có thì tự tạo), gắn phiên đăng nhập rồi hiện
            var form2 = Form2Preloader.Lay();
            form2.ApDungPhienDangNhap();
            form2.Show();
            form2.BringToFront();
            form2.Activate();
            this.Hide();
        }
        private void XacNhanDangNhapThanhCong(string tenNhap, string thongDiepNhatKy)
        {
            SessionInfo.TenTaiKhoan = tenNhap;
            SessionInfo.ThoiGianDangNhap = DateTime.Now;
            Module_NhatKy.GhiNhatKy(
                taiKhoan: tenNhap,
                hanhDong: thongDiepNhatKy,
                ghiChu: $"Thời gian: {SessionInfo.ThoiGianDangNhap:dd-MM-yyyy HH:mm:ss}"
            );
            DanhThucFrom2();
            ThoiGianDangNhap = DateTime.Now;
        }
        private void XulyDangNhapSai(string tenNhap)
        {
            demSai++;
            Module_NhatKy.GhiNhatKy(
                taiKhoan: tenNhap,
                hanhDong: "Đăng nhập thất bại",
                ghiChu: $"Thử {demSai}/3 lần vào lúc {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
            );
            text_MatKhau.Clear();
            text_MatKhau.Focus();
            if (demSai >= 3)
            {
                MessageBox.Show("Hệ thống nghi ngờ truy cập trái phép, phần mềm sẽ thoát!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Application.Exit();
            }
            else
            {
                MessageBox.Show($"Sai tài khoản hoặc mật khẩu! ({demSai}/3)", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        // ================= TỐI ƯU HIỂN THỊ (chống chớp, ổn định lâu dài) =================
        // Lưu ý: WinForms/Krypton vẽ bằng GDI+ trên CPU, không có cờ "bật GPU".
        // Tuyệt đối KHÔNG dùng WS_EX_COMPOSITED / UserPaint cho form này (gây chớp).
        private static readonly PropertyInfo? _piDoubleBuffered =
            typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
        private void ApplyPerformanceTuning()
        {
            DoubleBuffered = true;
            EnableDoubleBuffer(panelDangNhap);
            EnableDoubleBuffer(TableLayoutPanel6);
        }
        private static void EnableDoubleBuffer(Control c) => _piDoubleBuffered?.SetValue(c, true);
        // ================= GIAO DIỆN HIỆN ĐẠI: FORM KHÔNG VIỀN =================
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;
        private static readonly Color HoverThuong = Color.FromArgb(243, 244, 246);
        private static readonly Color HoverDong = Color.FromArgb(232, 17, 35);
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        /// <summary>Cho phép bấm icon taskbar để thu nhỏ khi form không viền.</summary>
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // Không dùng CS_DROPSHADOW: gây giật khi mở/kéo form có nhiều control Krypton
                cp.Style |= 0x20000;      // WS_MINIMIZEBOX
                return cp;
            }
        }
        // ---- BO GÓC: áp dụng TRƯỚC khi form hiện → không còn khung hình góc vuông ----
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            CapNhatBoGoc();
        }
        // Autoscale / đổi DPI làm đổi kích thước → Region luôn khớp (vẫn xong trước khi hiện form)
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (IsHandleCreated) CapNhatBoGoc();
        }
        /// <summary>Bo góc cửa sổ bằng Region. Đang thu nhỏ hoặc cùng kích thước → bỏ qua (không tạo lại).</summary>
        private void CapNhatBoGoc()
        {
            if (WindowState == FormWindowState.Minimized) return;
            Size s = Size;
            if (s.Width <= 0 || s.Height <= 0 || s == _kichThuocBoGoc) return;
            int d = (int)Math.Round(BanKinhBoGoc * 2 * DeviceDpi / 96.0); // bo theo DPI màn hình
            IntPtr h = IntPtr.Zero;
            try
            {
                h = CreateRoundRectRgn(0, 0, s.Width + 1, s.Height + 1, d, d);
                if (h == IntPtr.Zero) return;
                Region? cu = Region;
                Region = Region.FromHrgn(h);
                cu?.Dispose();               // giải phóng Region cũ, không rò GDI
                _kichThuocBoGoc = s;
            }
            catch { /* giữ góc vuông nếu lỗi, không làm sập form */ }
            finally
            {
                if (h != IntPtr.Zero) DeleteObject(h);
            }
        }
        /// <summary>Kéo vùng trống của form để di chuyển (thay cho thanh tiêu đề). Không gắn cho PictureBox1 để giữ chức năng Click.</summary>
        private void InitKeoDiChuyenForm()
        {
            foreach (Control c in new Control[]
            {
                panelDangNhap, TableLayoutPanel6, lblChaoMung, lblMoTa,
                label3_HienThiTenPhanMem, lblPhuDe
            })
            {
                c.MouseDown += KeoDiChuyenForm_MouseDown;
            }
        }
        private void KeoDiChuyenForm_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }
        private void lblDong_Click(object? sender, EventArgs e)
        {
            this.Close();
            Application.Exit();
        }
        private void lblThuNho_Click(object? sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
        private void lblDong_MouseEnter(object? sender, EventArgs e)
        {
            lblDong.BackColor = HoverDong;
            lblDong.ForeColor = Color.White;
        }
        private void lblDong_MouseLeave(object? sender, EventArgs e)
        {
            lblDong.BackColor = Color.White;
            lblDong.ForeColor = Color.FromArgb(107, 114, 128);
        }
        private void lblThuNho_MouseEnter(object? sender, EventArgs e) => lblThuNho.BackColor = HoverThuong;
        private void lblThuNho_MouseLeave(object? sender, EventArgs e) => lblThuNho.BackColor = Color.White;
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                RemoveFocusEvents();       // 🧹 Triệt tiêu event UI
                Check_HienMatKhau.CheckedChanged -= Check_HienMatKhau_CheckedChanged;
                _appIcon?.Dispose();       // 🧹 Giải phóng Icon khỏi RAM
            }
            catch { }
            base.OnFormClosed(e);
        }
        private void label1_PhienBanPhanMem_Click(object? sender, EventArgs e)
        {
            try
            {
                // Lấy chuỗi thông tin từ Module hệ thống
                string thongTin = Module_TaiKhoan.LayThongTinPhienBanVaToken();
                // Gọi Form ảo chuyên dụng
                HienThiFormAo_ThongTinPhanMem("THÔNG TIN PHIÊN BẢN VÀ TOKEN", thongTin);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi hiển thị thông tin phần mềm] {ex.Message}");
            }
        }
        /// <summary>
        /// Dựng Form động kế thừa từ FormAoBase (Anti-Flicker).
        /// TextBox không viền để bôi đen, tích hợp trích xuất & copy Token.
        /// Font tạo 1 lần và được Dispose khi đóng (trước đây mỗi lần mở rò 4–5 đối tượng Font GDI).
        /// </summary>
        private void HienThiFormAo_ThongTinPhanMem(string tieuDe, string noiDung)
        {
            string nl = Environment.NewLine;
            // 1. Chuẩn hóa xuống dòng (tránh sinh "\r\r\n" khi nguồn đã có \r\n)
            string noiDungChuan = noiDung.Replace("\r\n", "\n").Replace("\n", nl);
            // 2. Cắt dòng "THÔNG TIN PHẦN MỀM" dư ở đầu (đã có tiêu đề Form)
            string tienTo = "THÔNG TIN PHẦN MỀM" + nl + nl;
            if (noiDungChuan.StartsWith(tienTo, StringComparison.Ordinal))
                noiDungChuan = noiDungChuan.Substring(tienTo.Length);
            string tenFont = Module_HeThong.TenFontHeThong;
            using var fontTieuDe = new System.Drawing.Font(tenFont, 11.5F, System.Drawing.FontStyle.Bold);
            using var fontNoiDung = new System.Drawing.Font(tenFont, 10.5F, System.Drawing.FontStyle.Regular);
            using var fontNut = new System.Drawing.Font(tenFont, 9.5F, System.Drawing.FontStyle.Bold);
            using (var formAo = new FormAoBase())
            {
                formAo.Text = "Thông tin phiên bản và Token";
                formAo.Size = new System.Drawing.Size(780, 520);
                formAo.ShowIcon = false;
                formAo.ShowInTaskbar = false;
                // --- 1. PANEL TIÊU ĐỀ ---
                var panelTop = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(25, 20, 20, 5) };
                panelTop.StateCommon.Color1 = System.Drawing.Color.White;
                var lblTitle = new Krypton.Toolkit.KryptonLabel { Text = tieuDe.ToUpper(), Dock = DockStyle.Fill, AutoSize = false };
                lblTitle.StateCommon.ShortText.Font = fontTieuDe;
                lblTitle.StateCommon.ShortText.Color1 = System.Drawing.Color.FromArgb(0, 82, 155); // Xanh đại dương
                panelTop.Controls.Add(lblTitle);
                // --- 2. ĐƯỜNG KẺ NGANG ---
                var separator = new Label { Height = 1, Dock = DockStyle.Top, BackColor = System.Drawing.Color.FromArgb(220, 220, 220), Margin = new Padding(0, 5, 0, 10) };
                // --- 3. PANEL NỘI DUNG ---
                var panelContent = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Fill, Padding = new Padding(25, 10, 25, 20) };
                panelContent.StateCommon.Color1 = System.Drawing.Color.White;
                // --- 4. TEXTBOX (bôi đen copy được) ---
                var txtContent = new Krypton.Toolkit.KryptonTextBox
                {
                    Text = noiDungChuan,
                    ReadOnly = true,
                    Multiline = true,
                    WordWrap = true,
                    ScrollBars = ScrollBars.Vertical,
                    Dock = DockStyle.Fill
                };
                txtContent.StateCommon.Back.Color1 = System.Drawing.Color.White;
                txtContent.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.None;
                txtContent.StateCommon.Content.Font = fontNoiDung;
                txtContent.StateCommon.Content.Color1 = System.Drawing.Color.FromArgb(45, 45, 45);
                txtContent.StateCommon.Content.Padding = new Padding(0);
                // --- 5. PANEL NÚT BẤM ---
                var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 65, BackColor = System.Drawing.Color.WhiteSmoke };
                var btnCopy = new Krypton.Toolkit.KryptonButton { Text = "Sao chép Token", Width = 150, Height = 38 };
                btnCopy.StateCommon.Content.ShortText.Font = fontNut;
                btnCopy.StateCommon.Border.Rounding = 5;
                btnCopy.Click += (s, ev) =>
                {
                    try
                    {
                        // Trích xuất phần mã Token để copy
                        const string keyword = "Token hệ thống: ";
                        int idx = noiDungChuan.LastIndexOf(keyword, StringComparison.Ordinal);
                        string tokenCopy = idx >= 0
                            ? noiDungChuan.Substring(idx + keyword.Length).Trim()
                            : noiDungChuan; // Fail-safe: không thấy chữ thì copy toàn bộ
                        if (!string.IsNullOrWhiteSpace(tokenCopy))
                        {
                            Clipboard.SetText(tokenCopy);
                            MessageBox.Show(formAo, "Đã sao chép Token nội bộ vào bộ nhớ tạm!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch { }
                };
                var btnClose = new Krypton.Toolkit.KryptonButton { Text = "Đóng", Width = 110, Height = 38, DialogResult = DialogResult.OK };
                btnClose.StateCommon.Content.ShortText.Font = fontNut;
                btnClose.StateCommon.Border.Rounding = 5;
                // Căn giữa 2 nút theo bề rộng THỰC của panel (đúng cả khi form bị scale DPI)
                void CanGiuaNut()
                {
                    int total = btnCopy.Width + 15 + btnClose.Width;
                    int startX = Math.Max(0, (panelBottom.ClientSize.Width - total) / 2);
                    btnCopy.Location = new System.Drawing.Point(startX, 12);
                    btnClose.Location = new System.Drawing.Point(startX + btnCopy.Width + 15, 12);
                }
                panelBottom.Controls.Add(btnCopy);
                panelBottom.Controls.Add(btnClose);
                panelBottom.Resize += (s, ev) => CanGiuaNut();
                CanGiuaNut();
                // --- 6. XẾP LAYER ---
                panelContent.Controls.Add(txtContent);
                panelContent.Controls.Add(separator);
                txtContent.BringToFront();
                separator.SendToBack();
                formAo.Controls.Add(panelContent);
                formAo.Controls.Add(panelTop);
                formAo.Controls.Add(panelBottom);
                formAo.AcceptButton = btnClose;
                formAo.CancelButton = btnClose;
                // Tránh nháy nút / bôi đen toàn bộ text khi vừa mở
                formAo.Shown += (s, ev) => btnClose.Focus();
                formAo.ShowDialog(this);
            }
        }
    }
}