using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
namespace PhanMemThiDua2026
{
    public partial class Form39_ThongTinNguoiDung : Form
    {
        private readonly string _csdl1Path = Module_DanduongGPS.DuongDanCSDL1;
        // 🔒 SYSTEM LIMITS
        private const int MAX_IMAGE_SIZE = 50 * 1024 * 1024; // 50MB
        private const int MAX_PIXEL_LIMIT = 4096;
        private const int DISPLAY_SIZE = 256;
        // 🌟 CẤU HÌNH VIỀN UI/UX (đồng bộ với Form8)
        private static readonly Color FocusBorderColor = Color.FromArgb(99, 102, 241);  // Chàm
        private static readonly Color NormalBorderColor = Color.FromArgb(51, 65, 85);   // Xám đậm
        private const int FocusBorderWidth = 2;
        private const int NormalBorderWidth = 1;
        private static Form39_ThongTinNguoiDung? _instance;
        private static readonly object _formLock = new object();
        public static class UIHelper
        {
            public static void SafeInvoke(Control ctrl, Action action)
            {
                if (ctrl == null || ctrl.IsDisposed || ctrl.Disposing) return;
                try
                {
                    if (ctrl.InvokeRequired)
                        ctrl.BeginInvoke(action);
                    else
                        action();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[UIHelper] Lỗi SafeInvoke: {ex.Message}");
                }
            }
        }
        public static Form39_ThongTinNguoiDung GetInstance()
        {
            lock (_formLock)
            {
                if (_instance == null || _instance.IsDisposed)
                {
                    _instance = new Form39_ThongTinNguoiDung();
                    _instance.FormClosed += (_, __) =>
                    {
                        lock (_formLock)
                        {
                            _instance = null;
                        }
                    };
                }
                return _instance;
            }
        }
        public Form39_ThongTinNguoiDung()
        {
            InitializeComponent();
            SetupUI();
            InitBorderlessStyle();
            this.VisibleChanged += Form39_ThongTinNguoiDung_VisibleChanged;
        }
        private bool _isFirstLoad = true;
        // =========================================================================
        // 🎨 GIAO DIỆN KHÔNG VIỀN, BO TRÒN, KÉO ĐƯỢC (theo Form8)
        // =========================================================================
        private void InitBorderlessStyle()
        {
            this.DoubleBuffered = true;
            this.CancelButton = kryptonButton1_DongFrom; // Esc = Đóng
            this.Load += (s, e) => ApplyRoundedRegion();
            this.Resize += (s, e) => ApplyRoundedRegion();
            foreach (Control c in new Control[] { this, cardPanel, pnlHeader, lblTitle, lblSubtitle,
                                                  label9, label10, label12, label13, label15 })
                c.MouseDown += DragForm_MouseDown;
            // Ảnh đại diện hình tròn
            pictureBox2_AnhDaiDienAdmin.MouseDown += DragForm_MouseDown;
            using (var gp = new GraphicsPath())
            {
                gp.AddEllipse(0, 0, pictureBox2_AnhDaiDienAdmin.Width - 1, pictureBox2_AnhDaiDienAdmin.Height - 1);
                pictureBox2_AnhDaiDienAdmin.Region = new Region(gp);
            }
        }
        private void ApplyRoundedRegion()
        {
            const int radius = 22;
            int d = radius * 2;
            using var path = new GraphicsPath();
            path.AddArc(0, 0, d, d, 180, 90);
            path.AddArc(Width - d, 0, d, d, 270, 90);
            path.AddArc(Width - d, Height - d, d, d, 0, 90);
            path.AddArc(0, Height - d, d, d, 90, 90);
            path.CloseFigure();
            this.Region?.Dispose();
            this.Region = new Region(path);
        }
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        private void DragForm_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, 0xA1, 0x2, 0); // WM_NCLBUTTONDOWN, HTCAPTION
            }
        }
        // Lê Trung Kiên -  Yêu mèo cam
        private async void Form39_ThongTinNguoiDung_Load(object? sender, EventArgs e)
        {
            try
            {
                InitFocusEffects();
                this.ActiveControl = kryptonButton1_DongFrom;
                InitToolTips();
                KhoiTaoBangAnhAdmin(_csdl1Path);
                await NapDuLieuMoiNhatToanDien();
            }
            catch (Exception ex)
            {
                GhiLogHeThong("Form Load Error", ex);
            }
            finally
            {
                _isFirstLoad = false;
            }
        }
        private async void Form39_ThongTinNguoiDung_VisibleChanged(object? sender, EventArgs e)
        {
            if (this.Visible && !_isFirstLoad)
            {
                try
                {
                    Module_DanduongGPS.XoaCacheAvatarToanCuc();
                    await NapDuLieuMoiNhatToanDien();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Lỗi tải lại ảnh khi mở Form: " + ex.Message);
                }
            }
        }
        private async Task NapDuLieuMoiNhatToanDien()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var loadTextTask = LoadThongTinVanBanAsync();
                var loadAvatarTask = LoadAnhDaiDienAsync(cts.Token);
                await Task.WhenAll(loadTextTask, loadAvatarTask);
                Debug.WriteLine("🔄 [Form39] Đã nạp lại TOÀN BỘ Text và Ảnh mới nhất!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi nạp dữ liệu toàn diện Form39: " + ex.Message);
            }
        }
        private void HienThiAnh(Image img)
        {
            UIHelper.SafeInvoke(pictureBox2_AnhDaiDienAdmin, () =>
            {
                var oldImage = pictureBox2_AnhDaiDienAdmin.Image;
                pictureBox2_AnhDaiDienAdmin.Image = img;
                pictureBox2_AnhDaiDienAdmin.SizeMode = PictureBoxSizeMode.Zoom;
                oldImage?.Dispose();
            });
        }
        private void SetupUI()
        {
            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.None; // Không viền như Form8
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true;
        }
        public static void KhoiTaoBangAnhAdmin(string dbPath)
        {
            try
            {
                using var cn = new SqliteConnection($"Data Source={dbPath}");
                cn.Open();
                using var cmd = cn.CreateCommand();
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS AvatarAdmin (
                                ID INTEGER PRIMARY KEY, 
                                ThumbnailAnh BLOB, 
                                DuLieuAnh BLOB
                            );";
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi khởi tạo bảng AvatarAdmin: " + ex.Message);
            }
        }
        private IEnumerable<Control> GetAllTextBoxes()
        {
            return new Control[]
            {
                textBox_TenTaiKhoan,
                textBox_ThoiGianDangNhap,
                textBox_TenMayTinh,
                textBox_TenUserWindows,
                textbox_SeriaMay
            };
        }
        // 🌟 TÔ MÀU VIỀN KHI FOCUS (giống Form8)
        private void InitFocusEffects()
        {
            try
            {
                foreach (var ctrl in GetAllTextBoxes())
                {
                    if (ctrl == null || ctrl.IsDisposed)
                        continue;
                    if (ctrl is Krypton.Toolkit.KryptonTextBox ktb)
                    {
                        ktb.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
                        ktb.StateCommon.Border.Color1 = NormalBorderColor;
                        ktb.StateCommon.Border.Color2 = NormalBorderColor;
                        ktb.StateCommon.Border.Width = NormalBorderWidth;
                        ktb.Enter -= KryptonTextBox_EnterFocus;
                        ktb.Leave -= KryptonTextBox_LeaveFocus;
                        ktb.Enter += KryptonTextBox_EnterFocus;
                        ktb.Leave += KryptonTextBox_LeaveFocus;
                    }
                    else if (ctrl is TextBox tb)
                    {
                        tb.Enter -= StandardTextBox_EnterFocus;
                        tb.Leave -= StandardTextBox_LeaveFocus;
                        tb.Enter += StandardTextBox_EnterFocus;
                        tb.Leave += StandardTextBox_LeaveFocus;
                    }
                }
            }
            catch (Exception ex)
            {
                GhiLogHeThong("Init Focus UX Error", ex);
            }
        }
        private void RemoveFocusEvents()
        {
            try
            {
                foreach (var ctrl in GetAllTextBoxes())
                {
                    if (ctrl == null || ctrl.IsDisposed) continue;
                    if (ctrl is Krypton.Toolkit.KryptonTextBox ktb)
                    {
                        ktb.Enter -= KryptonTextBox_EnterFocus;
                        ktb.Leave -= KryptonTextBox_LeaveFocus;
                    }
                    else if (ctrl is TextBox tb)
                    {
                        tb.Enter -= StandardTextBox_EnterFocus;
                        tb.Leave -= StandardTextBox_LeaveFocus;
                    }
                }
            }
            catch { }
        }
        private void KryptonTextBox_EnterFocus(object? sender, EventArgs e)
        {
            if (sender is Krypton.Toolkit.KryptonTextBox ktb)
            {
                ktb.StateCommon.Border.Color1 = FocusBorderColor;
                ktb.StateCommon.Border.Color2 = FocusBorderColor;
                ktb.StateCommon.Border.Width = FocusBorderWidth;
                ktb.Refresh();
            }
        }
        private void KryptonTextBox_LeaveFocus(object? sender, EventArgs e)
        {
            if (sender is Krypton.Toolkit.KryptonTextBox ktb)
            {
                ktb.StateCommon.Border.Color1 = NormalBorderColor;
                ktb.StateCommon.Border.Color2 = NormalBorderColor;
                ktb.StateCommon.Border.Width = NormalBorderWidth;
                ktb.Refresh();
            }
        }
        private void StandardTextBox_EnterFocus(object? sender, EventArgs e)
        {
            if (sender is TextBox tb) tb.BackColor = Color.AliceBlue;
        }
        private void StandardTextBox_LeaveFocus(object? sender, EventArgs e)
        {
            if (sender is TextBox tb) tb.BackColor = SystemColors.Window;
        }
        // =========================================================================
        // 🚀 CÁC HÀM XỬ LÝ TEXT & DỮ LIỆU CỐT LÕI (GIỮ NGUYÊN)
        // =========================================================================
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Gợi ý thao tác";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            toolTip1.InitialDelay = 200;
            toolTip1.AutoPopDelay = 3000;
            toolTip1.ReshowDelay = 50;
            string tenTaiKhoan = string.IsNullOrWhiteSpace(textBox_TenTaiKhoan.Text)
                                 ? "người dùng"
                                 : textBox_TenTaiKhoan.Text.Trim();
            var tips = new Dictionary<Control, string>
            {
                { pictureBox2_AnhDaiDienAdmin, $"Ảnh đại diện của tài khoản {tenTaiKhoan}" },
                { kryptonButton1_DongFrom, "Đóng cửa sổ thông tin người dùng" }
            };
            foreach (var tip in tips)
            {
                if (tip.Key != null) toolTip1.SetToolTip(tip.Key, tip.Value);
            }
        }
        private async Task LoadThongTinVanBanAsync()
        {
            try
            {
                textBox_TenTaiKhoan.Text = SessionInfo.TenTaiKhoan ?? "N/A";
                textBox_ThoiGianDangNhap.Text = FormatThoiGianDangNhap(SessionInfo.ThoiGianDangNhap);
                textBox_TenMayTinh.Text = SafeGet(() => Environment.MachineName);
                textBox_TenUserWindows.Text = SafeGet(() => Environment.UserName);
                SetTextBoxesReadOnly();
                textbox_SeriaMay.Text = "Đang kiểm tra...";
                string uuid = await Task.Run(() => SafeGet(() => Module_TrangThaiHeThong.LayUUIDMayTinh()));
                if (!this.IsDisposed && !this.Disposing && this.IsHandleCreated)
                {
                    textbox_SeriaMay.Text = uuid;
                }
            }
            catch (Exception ex)
            {
                GhiLogHeThong("Text Load Error", ex);
            }
        }
        private void SetTextBoxesReadOnly()
        {
            foreach (var ctrl in GetAllTextBoxes())
            {
                if (ctrl is Krypton.Toolkit.KryptonTextBox ktb)
                {
                    ktb.ReadOnly = true;
                }
                else if (ctrl is TextBox tb)
                {
                    tb.ReadOnly = true;
                    tb.BackColor = SystemColors.Window;
                }
            }
        }
        private string SafeGet(Func<string> func)
        {
            try { return func() ?? "N/A"; }
            catch { return "N/A"; }
        }
        private string FormatThoiGianDangNhap(DateTime dt)
        {
            try
            {
                if (dt == default || dt.Year < 2000) return "N/A";
                dt = dt.ToLocalTime();
                return $"{dt:HH} giờ {dt:mm} phút {dt:ss} giây, ngày {dt:dd} tháng {dt:MM} năm {dt:yyyy}";
            }
            catch { return "N/A"; }
        }
        // =========================================================================
        // 🚀 TỐI ƯU ẢNH
        // =========================================================================
        private async Task LoadAnhDaiDienAsync(CancellationToken token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_csdl1Path) || !File.Exists(_csdl1Path)) return;
                lock (Module_DanduongGPS.AvatarLock)
                {
                    if (Module_DanduongGPS.CachedAvatarAdmin != null)
                    {
                        HienThiAnh(new Bitmap(Module_DanduongGPS.CachedAvatarAdmin));
                        return;
                    }
                }
                Image? resizedImage = await LayVaXuLyAnhTuDatabaseAsync(token);
                if (this.IsDisposed || this.Disposing || !this.IsHandleCreated)
                {
                    resizedImage?.Dispose();
                    return;
                }
                if (resizedImage != null)
                {
                    lock (Module_DanduongGPS.AvatarLock)
                    {
                        Module_DanduongGPS.CachedAvatarAdmin?.Dispose();
                        Module_DanduongGPS.CachedAvatarAdmin = new Bitmap(resizedImage);
                    }
                    HienThiAnh(new Bitmap(resizedImage));
                    resizedImage.Dispose();
                }
                else
                {
                    UIHelper.SafeInvoke(pictureBox2_AnhDaiDienAdmin, () =>
                    {
                        var oldImage = pictureBox2_AnhDaiDienAdmin.Image;
                        pictureBox2_AnhDaiDienAdmin.Image = null;
                        oldImage?.Dispose();
                    });
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                GhiLogHeThong("Avatar Load Error", ex);
            }
        }
        private async Task<Image?> LayVaXuLyAnhTuDatabaseAsync(CancellationToken token)
        {
            string connString = $"Data Source={_csdl1Path};Mode=ReadOnly;Default Timeout=10;Pooling=True;";
            using var conn = new SqliteConnection(connString);
            await conn.OpenAsync(token);
            using var cmd = new SqliteCommand("SELECT ThumbnailAnh, DuLieuAnh FROM AvatarAdmin WHERE ID = 1 LIMIT 1;", conn);
            using var reader = await cmd.ExecuteReaderAsync(token);
            if (!await reader.ReadAsync(token)) return null;
            int thumbOrdinal = reader.GetOrdinal("ThumbnailAnh");
            int fullOrdinal = reader.GetOrdinal("DuLieuAnh");
            foreach (int ordinal in new[] { thumbOrdinal, fullOrdinal })
            {
                token.ThrowIfCancellationRequested();
                byte[]? imageBytes = DocDuLieuAnhLinhHoat(reader, ordinal);
                if (imageBytes == null || imageBytes.Length == 0) continue;
                try
                {
                    using var ms = new MemoryStream(imageBytes);
                    using var tempBmp = Image.FromStream(ms, false, true);
                    return ResizeImageSafe(tempBmp, DISPLAY_SIZE, DISPLAY_SIZE);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Lỗi chuyển đổi dữ liệu ảnh (cột " + ordinal + "): " + ex.Message);
                }
            }
            return null;
        }
        /// <summary>
        /// Đọc dữ liệu ảnh từ 1 cột SQLite bất kể kiểu lưu trữ:
        /// BLOB (byte[]), TEXT Base64, TEXT dạng "data:image/...;base64,...", hoặc TEXT là đường dẫn file ảnh.
        /// </summary>
        private static byte[]? DocDuLieuAnhLinhHoat(System.Data.Common.DbDataReader reader, int ordinal)
        {
            try
            {
                if (reader.IsDBNull(ordinal)) return null;
                object value = reader.GetValue(ordinal);
                if (value is byte[] bytes)
                    return bytes.Length > 0 ? bytes : null;
                if (value is string text)
                {
                    text = text.Trim();
                    if (text.Length == 0) return null;
                    if (text.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    {
                        int idx = text.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
                        if (idx >= 0) text = text.Substring(idx + 7);
                    }
                    try { return Convert.FromBase64String(text); }
                    catch (FormatException) { }
                    if (File.Exists(text)) return File.ReadAllBytes(text);
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi đọc cột ảnh: " + ex.Message);
                return null;
            }
        }
        private Bitmap ResizeImageSafe(Image img, int maxW, int maxH)
        {
            double ratio = Math.Min((double)maxW / img.Width, (double)maxH / img.Height);
            int newW = Math.Max(1, (int)(img.Width * ratio));
            int newH = Math.Max(1, (int)(img.Height * ratio));
            var bmp = new Bitmap(newW, newH);
            using var g = Graphics.FromImage(bmp);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.DrawImage(img, 0, 0, newW, newH);
            return bmp;
        }
        // =========================================================================
        // 🛑 QUẢN LÝ ĐÓNG FORM
        // =========================================================================
        private void kryptonButton1_DongFrom_Click(object? sender, EventArgs e)
        {
            this.Hide();
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                base.OnFormClosing(e);
            }
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                RemoveFocusEvents();
                pictureBox2_AnhDaiDienAdmin.Image?.Dispose();
            }
            catch { }
            base.OnFormClosed(e);
        }
        // =========================================================================
        // 🛠️ LOGGING VÀO DATABASE
        // =========================================================================
        private void GhiLogHeThong(string context, Exception? ex = null)
        {
            try
            {
                string taiKhoan = string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM)
                    ? "Hệ thống (Exception)"
                    : Module_TaiKhoan.TenTaiKhoan_RAM;
                string hanhDong = $"[LỖI Form39] {context}";
                string ghiChu = "Không có Exception chi tiết.";
                if (ex != null)
                {
                    string stackTrace = ex.StackTrace ?? "";
                    if (stackTrace.Length > 800)
                    {
                        stackTrace = stackTrace.Substring(0, 800) + "... [Đã cắt bớt]";
                    }
                    ghiChu = $"Message: {ex.Message} || StackTrace: {stackTrace}";
                }
                Module_NhatKy.GhiNhatKy(
                    taiKhoan: taiKhoan,
                    hanhDong: hanhDong,
                    ghiChu: ghiChu
                );
            }
            catch (Exception internalEx)
            {
                System.Diagnostics.Debug.WriteLine($"[CRITICAL] Ghi log CSDL thất bại. Context: {context}. Lỗi: {internalEx.Message}");
            }
        }
    }
}