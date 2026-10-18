///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form24_XacMinhAdmin : Form
    {
        /// 1. CONFIG & STATE
        private readonly string _csdl1Path = Module_DanduongGPS.DuongDanCSDL1;
        // [CẬP NHẬT]: Thêm đường dẫn CSDL2 để đọc bảng cấu hình (ThongTin)
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private const int MAX_SAI = 3;
        private const int THOI_GIAN_KHOA_PHUT = 5;
        private static int _soLanSaiToanCuc = 0;
        private static DateTime _thoiGianMoKhoa = DateTime.MinValue;
        private bool _isProcessing = false;
        // Cờ chống spam nháy đúp chuột liên tục
        private bool _dangXuLyGoiYTaiKhoan = false;
        private readonly string _cacheUserPath = Path.Combine(Application.StartupPath, "last_admin.txt");
        // 2. UI CONSTANTS
        // Viền ô nhập giữ CÙNG độ dày ở mọi trạng thái (chỉ đổi màu) để chữ không bị xê dịch 1px khi focus
        private static readonly Color FocusBorderColor = Color.FromArgb(99, 102, 241);
        private static readonly Color NormalBorderColor = Color.FromArgb(203, 213, 225);
        private const int BorderWidthCoDinh = 2;
        // 3. CONSTRUCTOR
        public Form24_XacMinhAdmin()
        {
            InitializeComponent();
            SetupFormProperties();
            RegisterEvents();
            InitToolTips();
            InitFocusEffects();
            InitNenTrai();
        }
        private void SetupFormProperties()
        {
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            CancelButton = btn_Thoat;
            // text_MatKhau.UseSystemPasswordChar = true;
        }
        // 4. QUẢN LÝ SỰ KIỆN
        private void RegisterEvents()
        {
            this.Load += Form24_XacMinhAdmin_Load;
            // Thêm 2 dòng này để đăng ký sự kiện ẩn/hiện mật khẩu
            check_HienMatKhau.CheckedChanged -= check_HienMatKhau_CheckedChanged;
            check_HienMatKhau.CheckedChanged += check_HienMatKhau_CheckedChanged;
            btn_XacThuc.Click -= btn_XacThuc_Click;
            btn_XacThuc.Click += btn_XacThuc_Click;
            btn_Thoat.Click -= btn_Thoat_Click;
            btn_Thoat.Click += btn_Thoat_Click;
            PictureBox1.Click -= PictureBox1_Click;
            PictureBox1.Click += PictureBox1_Click;
            text_TenDangNhap.KeyDown += Text_TenDangNhap_KeyDown;
            text_MatKhau.KeyDown += Text_MatKhau_KeyDown;
            text_TenDangNhap.DoubleClick -= Text_TenDangNhap_DoubleClick;
            text_TenDangNhap.DoubleClick += Text_TenDangNhap_DoubleClick;
            // Cảnh báo Caps Lock khi đang ở ô mật khẩu
            text_MatKhau.Enter += (s, e) => CapNhatCanhBaoCapsLock();
            text_MatKhau.KeyUp += (s, e) => CapNhatCanhBaoCapsLock();
            text_MatKhau.Leave += (s, e) => label_CapsLock.Visible = false;
        }
        private void CapNhatCanhBaoCapsLock()
        {
            if (IsDisposed || label_CapsLock.IsDisposed) return;
            label_CapsLock.Visible = text_MatKhau.ContainsFocus && Control.IsKeyLocked(Keys.CapsLock);
        }
        // ===== NỀN TRÁI: chuyển sắc tím đậm + bong bóng (vẽ 1 lần vào Bitmap, về sau chỉ blit nên không giật) =====
        private Bitmap? _nenTrai;
        private void InitNenTrai()
        {
            // Bật double buffer cho các panel/bảng (thuộc tính protected nên dùng reflection)
            foreach (Control c in new Control[] { pnlTrai, pnlPhai, tblTrai, TableLayoutPanel1, TableLayoutPanel5 })
                typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(c, true);
            pnlTrai.BackgroundImageLayout = ImageLayout.None;
            pnlTrai.SizeChanged += (s, e) => DatNenTrai();
            Disposed += (s, e) =>
            {
                var cu = _nenTrai;
                _nenTrai = null;
                cu?.Dispose();
            };
        }
        private void DatNenTrai()
        {
            if (IsDisposed || pnlTrai.IsDisposed) return;
            int w = pnlTrai.ClientSize.Width, h = pnlTrai.ClientSize.Height;
            if (w < 40 || h < 40) return;
            if (_nenTrai != null && _nenTrai.Width == w && _nenTrai.Height == h) return;
            Bitmap? bmp = null;
            try
            {
                bmp = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    // Nền chuyển sắc tím: sáng ở góc trên-trái, đậm ở góc dưới-phải
                    using (var br = new LinearGradientBrush(new Rectangle(0, 0, w, h),
                        Color.FromArgb(139, 92, 246), Color.FromArgb(76, 29, 149), 55f))
                        g.FillRectangle(br, 0, 0, w, h);
                    // Bong bóng: toạ độ và bán kính theo tỉ lệ (không phụ thuộc DPI)
                    //          cx    cy    r(theo w)  alpha  loại (0 = đặc, 1 = vòng)
                    float[][] bong =
                    {
                        new[] { 0.95f, 0.08f, 0.30f, 30f, 0f },
                        new[] { 0.04f, 0.94f, 0.38f, 26f, 0f },
                        new[] { 0.84f, 0.80f, 0.14f, 34f, 0f },
                        new[] { 0.14f, 0.11f, 0.09f, 44f, 0f },
                        new[] { 0.92f, 0.45f, 0.05f, 60f, 0f },
                        new[] { 0.27f, 0.74f, 0.04f, 50f, 0f },
                        new[] { 0.58f, 0.97f, 0.06f, 40f, 0f },
                        new[] { 0.07f, 0.50f, 0.11f, 85f, 1f },
                        new[] { 0.78f, 0.24f, 0.08f, 75f, 1f },
                        new[] { 0.45f, 0.06f, 0.022f, 120f, 0f },
                        new[] { 0.66f, 0.64f, 0.018f, 110f, 0f },
                        new[] { 0.16f, 0.32f, 0.014f, 110f, 0f },
                    };
                    foreach (var b in bong)
                    {
                        float cx = b[0] * w, cy = b[1] * h, r = b[2] * w;
                        int a = (int)b[3];
                        var rect = new RectangleF(cx - r, cy - r, 2 * r, 2 * r);
                        if (b[4] == 0f)
                        {
                            using var fill = new SolidBrush(Color.FromArgb(a, 255, 255, 255));
                            g.FillEllipse(fill, rect);
                            if (r > 0.07f * w)
                            {
                                // Viền sáng + điểm sáng nhỏ ở góc trên-trái cho giống bong bóng xà phòng
                                using var vien = new Pen(Color.FromArgb(Math.Min(255, a + 25), 255, 255, 255), 1.5f);
                                g.DrawEllipse(vien, rect);
                                using var diem = new SolidBrush(Color.FromArgb(Math.Min(255, a + 40), 255, 255, 255));
                                g.FillEllipse(diem, cx - 0.62f * r, cy - 0.62f * r, 0.30f * r, 0.18f * r);
                            }
                        }
                        else
                        {
                            using var vong = new Pen(Color.FromArgb(a, 255, 255, 255), 2f);
                            g.DrawEllipse(vong, rect);
                        }
                    }
                }
                var cu = _nenTrai;
                pnlTrai.BackgroundImage = bmp;
                _nenTrai = bmp;
                bmp = null;
                cu?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi DatNenTrai Form24]: {ex.Message}");
            }
            finally
            {
                bmp?.Dispose();
            }
        }
        // 1. CHUYỂN SỰ KIỆN LOAD THÀNH ASYNC ĐỂ CHẠY BẤT ĐỒNG BỘ
        private async void Form24_XacMinhAdmin_Load(object? sender, EventArgs e)
        {
            DatNenTrai(); // vẽ sẵn nền tím + bong bóng trước lần hiện đầu tiên
            // Đợi nạp tên tài khoản tự  (nếu được phép) - đọc CSDL chạy ở luồng nền, form hiện ra ngay
            await LoadGoiYTenTaiKhoanAsync();
            if (IsDisposed) return;
            // Sau khi nạp xong, nhảy con trỏ xuống ô Mật khẩu (nếu chưa có tên thì ở lại ô Tên đăng nhập)
            BeginInvoke(new Action(() =>
            {
                if (IsDisposed) return;
                if (string.IsNullOrWhiteSpace(text_TenDangNhap.Text))
                {
                    text_TenDangNhap.Focus();
                    text_TenDangNhap.Select();
                }
                else
                {
                    text_MatKhau.Focus();
                    text_MatKhau.Select();
                }
            }));
        }
        // 2. LÕI TỰ ĐỘNG GỢI Ý TÊN ĐĂNG NHẬP
        private async Task LoadGoiYTenTaiKhoanAsync()
        {
            try
            {
                // Toàn bộ việc đọc CSDL / giải mã AES / đọc file chạy ở luồng nền
                string tenGoiY = await Task.Run(LayTenGoiYNenSau);
                if (IsDisposed || string.IsNullOrEmpty(tenGoiY)) return;
                // Không ghi đè nếu người dùng đã kịp gõ trong lúc chờ
                if (string.IsNullOrEmpty(text_TenDangNhap.Text))
                    text_TenDangNhap.Text = tenGoiY;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi LoadGoiYTenTaiKhoanAsync Form24]: {ex.Message}");
            }
        }
        // Chạy ở luồng nền: không đụng tới control nào
        private string LayTenGoiYNenSau()
        {
            try
            {
                // BƯỚC 1: Kiểm tra cấu hình trong CSDL2 xem có cho phép gợi ý không
                if (CoChoPhepGoiYTenTaiKhoan())
                {
                    // BƯỚC 2: Truy xuất lấy Tên đăng nhập từ CSDL1 (giải mã AES)
                    string? tenTK = DocTenAdminTuCsdl1();
                    if (!string.IsNullOrEmpty(tenTK))
                        return tenTK; // Điền thành công thì thoát hàm ngay, không cần quét Cache nữa
                }
                // BƯỚC 3: DỰ PHÒNG (Fallback)
                // Nếu người dùng thiết lập KHÔNG cho phép (FALSE), ta sẽ lấy lại tên của lần đăng nhập thành công gần nhất từ file ẩn
                if (File.Exists(_cacheUserPath))
                {
                    string savedUser = File.ReadAllText(_cacheUserPath).Trim();
                    if (!string.IsNullOrEmpty(savedUser))
                        return savedUser;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi LayTenGoiYNenSau Form24]: {ex.Message}");
            }
            return string.Empty;
        }
        // Đọc tên admin (đã giải mã) từ CSDL1. Trả về null nếu không có. Chạy ở luồng nền.
        private string? DocTenAdminTuCsdl1()
        {
            if (string.IsNullOrWhiteSpace(_csdl1Path) || !File.Exists(_csdl1Path))
                return null;
            string connStr = $"Data Source={_csdl1Path};Mode=ReadOnly;Default Timeout=5;Pooling=True;";
            using var conn = new SqliteConnection(connStr);
            conn.Open();
            const string sql = "SELECT TenTaiKhoan FROM Admin WHERE ID = 1 LIMIT 1";
            using var cmd = new SqliteCommand(sql, conn);
            var val = cmd.ExecuteScalar();
            if (val == null || val == DBNull.Value) return null;
            // Giải mã AES
            return Module_BaoMatAES.GiaiMa(val.ToString() ?? string.Empty);
        }
        // TÍNH NĂNG MỚI: KIỂM TRA ĐIỀU KIỆN VÀ GỢI Ý AUTO-FILL TÊN ĐĂNG NHẬP
        private bool CoChoPhepGoiYTenTaiKhoan()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path))
                    return false;
                // 🛡️ Mở kết nối với Timeout ngắn vì đây chỉ là hàm kiểm tra nhanh
                using var cn = new SqliteConnection($"Data Source={_csdl2Path};Mode=ReadOnly;Default Timeout=5;Pooling=True;");
                cn.Open();
                using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT ChoPhepGoiYMatKhau FROM ThongTin WHERE ID = 1 LIMIT 1";
                object? val = cmd.ExecuteScalar();
                if (val == null || val == DBNull.Value) return false;
                string giaiMa = Module_BaoMatAES.GiaiMa(val.ToString() ?? string.Empty);
                return string.Equals(giaiMa, "TRUE", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
        private async void Text_TenDangNhap_DoubleClick(object? sender, EventArgs e)
        {
            // 1. Chống Spam Click
            if (_dangXuLyGoiYTaiKhoan) return;
            try
            {
                _dangXuLyGoiYTaiKhoan = true;
                // 2 + 3. Kiểm tra cờ cho phép rồi truy xuất tên đăng nhập - cả hai chạy ở luồng nền
                string? tenTK = await Task.Run(() => CoChoPhepGoiYTenTaiKhoan() ? DocTenAdminTuCsdl1() : null);
                if (IsDisposed) return;
                if (tenTK != null)
                {
                    // 4. Giải mã và nạp lên giao diện
                    text_TenDangNhap.Text = tenTK;
                    text_MatKhau.Focus(); // Tự động đưa con trỏ xuống ô mật khẩu cho mượt
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi gợi ý tên đăng nhập Form 24]: {ex.Message}");
            }
            finally
            {
                _dangXuLyGoiYTaiKhoan = false;
            }
        }
        // 6. LÕI XÁC THỰC BẢO MẬT (Async/Await)
        // Microsoft.Data.Sqlite thực chất đọc đồng bộ nên toàn bộ việc xác thực được chạy ở luồng nền để giao diện không đơ
        private Task<(bool isSuccess, string errorMessage)> KiemTraAdminAsync(string tenDangNhap, string matKhauNhap)
        {
            return Task.Run(() => KiemTraAdmin(tenDangNhap, matKhauNhap));
        }
        private (bool isSuccess, string errorMessage) KiemTraAdmin(string tenDangNhap, string matKhauNhap)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_csdl1Path) || !File.Exists(_csdl1Path))
                    return (false, "Không tìm thấy CSDL hệ thống!");
                using var cn = new SqliteConnection($"Data Source={_csdl1Path}");
                cn.Open();
                string sql = "SELECT TenTaiKhoan, MatKhau FROM Admin";
                using var cmd = new SqliteCommand(sql, cn);
                using var reader = cmd.ExecuteReader();
                bool isFoundAndMatched = false;
                while (reader.Read())
                {
                    if (reader.IsDBNull(0) || reader.IsDBNull(1)) continue;
                    string userDecrypted, passDecrypted;
                    try
                    {
                        userDecrypted = Module_BaoMatAES.GiaiMa(reader.GetString(0));
                        passDecrypted = Module_BaoMatAES.GiaiMa(reader.GetString(1));
                    }
                    catch
                    {
                        continue; // 1 dòng hỏng không làm hỏng việc xác thực các dòng còn lại
                    }
                    // So sánh theo thời gian cố định (không lộ thông tin qua độ trễ) và đánh giá cả hai vế
                    bool khopUser = SoSanhHangDinh(userDecrypted, tenDangNhap);
                    bool khopPass = SoSanhHangDinh(passDecrypted, matKhauNhap);
                    if (khopUser && khopPass)
                    {
                        isFoundAndMatched = true;
                        break;
                    }
                }
                if (isFoundAndMatched)
                {
                    return (true, string.Empty);
                }
                else
                {
                    return (false, "Tên đăng nhập hoặc mật khẩu không chính xác!");
                }
            }
            catch (Exception)
            {
                return (false, "Hệ thống gặp sự cố an ninh khi xác thực. Vui lòng thử lại sau.");
            }
        }
        private static bool SoSanhHangDinh(string? a, string? b)
        {
            byte[] x = Encoding.UTF8.GetBytes(a ?? string.Empty);
            byte[] y = Encoding.UTF8.GetBytes(b ?? string.Empty);
            return CryptographicOperations.FixedTimeEquals(x, y);
        }
        private async void btn_XacThuc_Click(object? sender, EventArgs e)
        {
            if (DateTime.Now < _thoiGianMoKhoa)
            {
                TimeSpan thoiGianConLai = _thoiGianMoKhoa - DateTime.Now;
                MessageBox.Show(this, $"Tài khoản bị khóa do nhập sai nhiều lần.\nVui lòng thử lại sau {thoiGianConLai.Minutes} phút {thoiGianConLai.Seconds} giây.",
                    "Cảnh báo an ninh", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_isProcessing) return;
            try
            {
                _isProcessing = true;
                btn_XacThuc.Enabled = false;
                Cursor = Cursors.WaitCursor;
                string tenNhap = text_TenDangNhap.Text.Trim();
                string passNhap = text_MatKhau.Text;
                var (isSuccess, errorMessage) = await KiemTraAdminAsync(tenNhap, passNhap);
                if (IsDisposed) return; // form đã bị đóng trong lúc chờ
                if (isSuccess)
                {
                    _soLanSaiToanCuc = 0;
                    try
                    {
                        File.WriteAllText(_cacheUserPath, tenNhap);
                    }
                    catch { }
                    text_MatKhau.Clear(); // xóa mật khẩu khỏi ô nhập trước khi đóng
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    return;
                }
                _soLanSaiToanCuc++;
                text_MatKhau.Clear();
                text_MatKhau.Focus();
                if (_soLanSaiToanCuc >= MAX_SAI)
                {
                    MessageBox.Show(this,
                        $"Xác thực thất bại\nPhát hiện có nỗ lực truy cập trái phép!\nChính sách bảo mật đã được cấp quyền đóng phần mềm ngay lập tức",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Stop);
                    Application.Exit();
                    return;
                }
                else
                {
                    MessageBox.Show(this,
                        $"{errorMessage}\nBạn đã nhập sai lần {_soLanSaiToanCuc}/{MAX_SAI}.",
                        "Xác thực thất bại", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            finally
            {
                _isProcessing = false;
                if (!IsDisposed)
                {
                    btn_XacThuc.Enabled = true;
                    Cursor = Cursors.Default;
                }
            }
        }
        private void btn_Thoat_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
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
                btn_XacThuc.PerformClick();
            }
        }
        private void PictureBox1_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(this, "Vui lòng nhập mật khẩu xác thực để thực hiện thao tác xóa!",
                "Xác thực quyền admin", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        // 8. UX TỐI ƯU
        private void InitFocusEffects()
        {
            var controls = new List<KryptonTextBox> { text_TenDangNhap, text_MatKhau };
            foreach (var ktb in controls)
            {
                if (ktb == null) continue;
                ktb.StateCommon.Border.DrawBorders = PaletteDrawBorders.All;
                ktb.StateCommon.Border.Color1 = NormalBorderColor;
                ktb.StateCommon.Border.Color2 = NormalBorderColor;
                ktb.StateCommon.Border.Width = BorderWidthCoDinh;
                // Chỉ đổi màu viền; Krypton tự vẽ lại, không cần Refresh() cưỡng bức (gây giật)
                ktb.Enter += (s, e) =>
                {
                    ktb.StateCommon.Border.Color1 = FocusBorderColor;
                    ktb.StateCommon.Border.Color2 = FocusBorderColor;
                };
                ktb.Leave += (s, e) =>
                {
                    ktb.StateCommon.Border.Color1 = NormalBorderColor;
                    ktb.StateCommon.Border.Color2 = NormalBorderColor;
                };
            }
        }
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Dang_Nhap;
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            toolTip1.InitialDelay = 300;
            toolTip1.AutoPopDelay = 2000;
            toolTip1.ReshowDelay = 100;
            toolTip1.ShowAlways = true;
            var tips = new Dictionary<Control, string>
            {
                { text_TenDangNhap, "Nhập tên đăng nhập hệ thống" },
                { text_MatKhau, "Nhập mật khẩu đăng nhập" },
                { check_HienMatKhau, "Hiển thị / ẩn mật khẩu đang nhập" },
                { btn_XacThuc, "Xác thực thông tin để tiếp tục" },
                { btn_Thoat, "Thoát khỏi chương trình" }
            };
            foreach (var tip in tips)
            {
                if (tip.Key != null) toolTip1.SetToolTip(tip.Key, tip.Value);
            }
        }
        private void check_HienMatKhau_CheckedChanged(object? sender, EventArgs e)
        {
            if (check_HienMatKhau.Checked)
            {
                // Khi tích chọn -> Xóa ký tự che để hiện mật khẩu
                text_MatKhau.PasswordChar = '\0';
            }
            else
            {
                // Khi bỏ chọn -> Che lại bằng dấu chấm đen
                text_MatKhau.PasswordChar = '●';
            }
        }
    }
    /// <summary>Vòng tròn trắng có quầng sáng (khử răng cưa) nằm sau biểu tượng ở nửa trái.</summary>
    internal sealed class VongTronLogo : Panel
    {
        public VongTronLogo()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int d = Math.Min(Width, Height);
            if (d < 20) return;
            // Quầng sáng mờ bên ngoài
            using (var halo = new SolidBrush(Color.FromArgb(55, 255, 255, 255)))
                g.FillEllipse(halo, 1, 1, d - 3, d - 3);
            // Vòng tròn trắng chính
            int inset = Math.Max(6, d / 14);
            using (var trang = new SolidBrush(Color.White))
                g.FillEllipse(trang, inset, inset, d - 2 * inset - 1, d - 2 * inset - 1);
        }
    }
}