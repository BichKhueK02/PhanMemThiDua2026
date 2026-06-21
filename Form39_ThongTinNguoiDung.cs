using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace PhanMemThiDua2026
{
    public partial class Form39_ThongTinNguoiDung : Form
    {
        private readonly string _csdl1Path = Module_DanduongGPS.DuongDanCSDL1;
        // 🔒 SYSTEM LIMITS (Anti-crash lâu dài)
        private const int MAX_IMAGE_SIZE = 50 * 1024 * 1024; // 50MB
        private const int MAX_PIXEL_LIMIT = 4096;
        private const int DISPLAY_SIZE = 256;
        // 🚀 CACHE RAM & THREAD SAFETY

        //private static readonly object _avatarLock = new object(); // Khóa luồng cho Cache
        private static readonly Color FocusBorderColor = Color.FromArgb(0, 120, 215); // Win 10/11 Blue
        private static readonly Color NormalBorderColor = Color.Silver;
        private const int FocusBorderWidth = 2;
        private const int NormalBorderWidth = 1;
        private static Form39_ThongTinNguoiDung? _instance;
        private static readonly object _formLock = new object();
        // Cờ nhận diện Form bật lần đầu tiên
        // private bool _isFirstLoad = true;
        public static class UIHelper
        {
            public static void SafeInvoke(Control ctrl, Action action)
            {
                // 🛡️ BẢO VỆ 3 LỚP (Đã gỡ bỏ bẫy !ctrl.IsHandleCreated để không bị nuốt ảnh khi load nhanh)
                if (ctrl == null || ctrl.IsDisposed || ctrl.Disposing) return;

                try
                {
                    // InvokeRequired sẽ trả về false nếu Handle chưa được tạo HOẶC đang đứng đúng luồng UI
                    if (ctrl.InvokeRequired)
                    {
                        ctrl.BeginInvoke(action);
                    }
                    else
                    {
                        // Nếu đang ở luồng UI, cứ mạnh dạn gán thẳng ảnh. 
                        // WinForms sẽ tự nhớ và hiển thị khi Form render xong.
                        action();
                    }
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
                // 🛡️ Form đã bị hủy -> tạo mới
                if (_instance == null || _instance.IsDisposed)
                {
                    _instance = new Form39_ThongTinNguoiDung();

                    // 🚀 Tự reset singleton khi form bị đóng thật
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
            // Thêm dòng này để lắng nghe khi Form ẩn/hiện
            this.VisibleChanged += Form39_ThongTinNguoiDung_VisibleChanged;
        }

        // Cờ nhận diện Form bật lần đầu tiên
        private bool _isFirstLoad = true;

        private async void Form39_ThongTinNguoiDung_Load(object sender, EventArgs e)
        {
            try
            {
                InitFocusEffects();
                this.ActiveControl = kryptonButton1_DongFrom;
                InitToolTips();

                // Luôn đảm bảo bảng tồn tại
                KhoiTaoBangAnhAdmin(_csdl1Path);

                // ⭐ GỌI HÀM NẠP TỔNG HỢP Ở LẦN ĐẦU KHỞI TẠO
                await NapDuLieuMoiNhatToanDien();
            }
            catch (Exception ex)
            {
                GhiLogHeThong("Form Load Error", ex);
            }
            finally
            {
                // Đánh dấu đã load xong lần đầu
                _isFirstLoad = false;
            }
        }

        private async void Form39_ThongTinNguoiDung_VisibleChanged(object? sender, EventArgs e)
        {
            // Cờ _isFirstLoad giúp chặn việc load đè khi Form_Load đang chạy lần đầu
            if (this.Visible && !_isFirstLoad)
            {
                try
                {
                    // Xóa cache để ép kéo data mới
                    Module_DanduongGPS.XoaCacheAvatarToanCuc();
                    await NapDuLieuMoiNhatToanDien();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Lỗi tải lại ảnh khi mở Form: " + ex.Message);
                }
            }
        }

        // ⭐ HÀM GOM CHUNG TRỌNG TÂM: Nạp cả Text lẫn Ảnh
        private async Task NapDuLieuMoiNhatToanDien()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                // Cho 2 tác vụ Text và Image chạy song song ép xung
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
            // 🚀 SỬ DỤNG UIHELPER CHUẨN XÁC: Truyền đúng PictureBox vào tham số đầu tiên
            UIHelper.SafeInvoke(pictureBox2_AnhDaiDienAdmin, () =>
            {
                // Bắt lại ảnh cũ đang hiển thị
                var oldImage = pictureBox2_AnhDaiDienAdmin.Image;

                // Gán ảnh mới và cấu hình hiển thị
                pictureBox2_AnhDaiDienAdmin.Image = img;
                pictureBox2_AnhDaiDienAdmin.SizeMode = PictureBoxSizeMode.StretchImage;

                // Giải phóng ảnh cũ khỏi RAM ngay lập tức
                oldImage?.Dispose();
            });
        }
        private void SetupUI()
        {
            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true; // Chống giật khung hình
        }
        // Đặt hàm này trong Form39 hoặc Module_KhoiTaoCSDL
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



        //private async void Form39_ThongTinNguoiDung_Load(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        // 🌟 Gọi Init Focus khi Form đã tạo Handle xong -> An toàn nhất cho Krypton
        //        InitFocusEffects();

        //        this.ActiveControl = kryptonButton1_DongFrom;
        //        InitToolTips();

        //        // 🚀 TIMEOUT 30S: An toàn cho máy cơ quan ổ HDD cũ / Antivirus quét ngầm
        //        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        //        var loadTextTask = LoadThongTinVanBanAsync();
        //        var loadAvatarTask = LoadAnhDaiDienAsync(cts.Token);
        //        KhoiTaoBangAnhAdmin(_csdl1Path); // Đảm bảo bảng tồn tại trước khi load ảnh
        //        await Task.WhenAll(loadTextTask, loadAvatarTask);
        //    }
        //    catch (OperationCanceledException)
        //    {
        //        GhiLogHeThong("Quá thời gian tải dữ liệu người dùng (Timeout 30s).");
        //    }
        //    catch (Exception ex)
        //    {
        //        GhiLogHeThong("Form Load Error", ex);
        //    }
        //}
        // Cung cấp danh sách Control tĩnh, loại bỏ đệ quy tốn CPU
        private IEnumerable<Control> GetAllTextBoxes()
        {
            return new Control[]
            {
                textBox_TenTaiKhoan,
                textBox_ThoiGianDangNhap,
                textBox_TenMayTinh,
                textBox_TenUserWindows,
                textbox_SeriaMay,
                textBox_PhienbanPhanMem,
                kryptonTextBox1_CapNhatLanCuoi
            };
        }
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
                        // Set trạng thái chuẩn để không bị rách viền trên máy DPI cao
                        ktb.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
                        ktb.StateCommon.Border.Color1 = NormalBorderColor;
                        ktb.StateCommon.Border.Color2 = NormalBorderColor;
                        ktb.StateCommon.Border.Width = NormalBorderWidth;

                        ktb.Enter -= KryptonTextBox_EnterFocus;
                        ktb.Leave -= KryptonTextBox_LeaveFocus;
                        ktb.Enter += KryptonTextBox_EnterFocus;
                        ktb.Leave += KryptonTextBox_LeaveFocus;
                    }
                    else if (ctrl is TextBox tb) // Hỗ trợ nếu có TextBox WinForms thường
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
        // 🚀 CÁC HÀM XỬ LÝ TEXT & DỮ LIỆU CỐT LÕI
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
                textBox_PhienbanPhanMem.Text = Module_PhienBan.SoftwareVersion ?? "N/A";

                if (kryptonTextBox1_CapNhatLanCuoi != null)
                    kryptonTextBox1_CapNhatLanCuoi.Text = Module_PhienBan.NgayThangNamHeThong ?? "N/A";

                SetTextBoxesReadOnly();

                textbox_SeriaMay.Text = "Đang kiểm tra...";
                string uuid = await Task.Run(() => SafeGet(() => Module_TrangThaiHeThong.LayUUIDMayTinh()));

                // 🛡️ BẢO VỆ HANDLE
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
        // 🚀 TỐI ƯU ẢNH (Stream Phòng Ngự & Clone An Toàn Tuyệt Đối)
        // =========================================================================
        private async Task LoadAnhDaiDienAsync(CancellationToken token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_csdl1Path) || !File.Exists(_csdl1Path)) return;

                // 1. Kiểm tra RAM (Cache toàn cục từ Module) trước tiên
                lock (Module_DanduongGPS.AvatarLock)
                {
                    if (Module_DanduongGPS.CachedAvatarAdmin != null)
                    {
                        HienThiAnh(new Bitmap(Module_DanduongGPS.CachedAvatarAdmin));
                        return;
                    }
                }

                // 2. Không có Cache thì lấy từ DB
                Image? resizedImage = await LayVaXuLyAnhTuDatabaseAsync(token);

                if (this.IsDisposed || this.Disposing || !this.IsHandleCreated)
                {
                    resizedImage?.Dispose();
                    return;
                }

                if (resizedImage != null)
                {
                    // Nạp bản sao vào Cache toàn cục
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
                    // Xóa trắng PictureBox nếu DB không có ảnh
                    UIHelper.SafeInvoke(pictureBox2_AnhDaiDienAdmin, () =>
                    {
                        var oldImage = pictureBox2_AnhDaiDienAdmin.Image;
                        pictureBox2_AnhDaiDienAdmin.Image = null;
                        oldImage?.Dispose();
                    });
                }
            }
            catch (OperationCanceledException) { /* Tự hủy an toàn 30s */ }
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

            // Lấy ID = 1
            using var cmd = new SqliteCommand("SELECT ThumbnailAnh, DuLieuAnh FROM AvatarAdmin WHERE ID = 1 LIMIT 1;", conn);
            using var reader = await cmd.ExecuteReaderAsync(token);

            if (!await reader.ReadAsync(token)) return null;

            byte[]? imageBytes = null;

            // Ưu tiên đọc Thumbnail trước để tối ưu RAM, nếu không có thì lấy ảnh gốc Full
            int thumbOrdinal = reader.GetOrdinal("ThumbnailAnh");
            int fullOrdinal = reader.GetOrdinal("DuLieuAnh");

            if (!reader.IsDBNull(thumbOrdinal))
            {
                imageBytes = (byte[])reader["ThumbnailAnh"];
            }
            else if (!reader.IsDBNull(fullOrdinal))
            {
                imageBytes = (byte[])reader["DuLieuAnh"];
            }

            if (imageBytes == null || imageBytes.Length == 0) return null;

            try
            {
                using var ms = new MemoryStream(imageBytes);
                // Dùng mẫu tạo dựng an toàn tránh lỗi GDI+ lock file/stream
                using var tempBmp = Image.FromStream(ms, false, true);

                // Trả về bản resize chuẩn UI
                return ResizeImageSafe(tempBmp, DISPLAY_SIZE, DISPLAY_SIZE);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi chuyển đổi chuỗi Byte sang Image: " + ex.Message);
                return null;
            }
        }


        //private async Task<Image?> LayVaXuLyAnhTuDatabaseAsync(CancellationToken token)
        //{
        //    // 🚀 SQLITE BUSY TIMEOUT: Chờ tối đa 10s nếu file .db đang bị khóa
        //    string connString = $"Data Source={_csdl1Path};Mode=ReadOnly;Default Timeout=10;Pooling=True;";
        //    using var conn = new SqliteConnection(connString);

        //    await conn.OpenAsync(token);

        //    using var cmd = new SqliteCommand("SELECT ThumbnailAnh, DuLieuAnh FROM AvatarAdmin WHERE ID = 1 LIMIT 1;", conn);
        //    using var reader = await cmd.ExecuteReaderAsync(token);

        //    if (!await reader.ReadAsync(token)) return null;

        //    int thumbOrdinal = reader.GetOrdinal("ThumbnailAnh");
        //    int fullOrdinal = reader.GetOrdinal("DuLieuAnh");
        //    int colIndex = !reader.IsDBNull(thumbOrdinal) ? thumbOrdinal :
        //                   !reader.IsDBNull(fullOrdinal) ? fullOrdinal : -1;

        //    if (colIndex == -1) return null;

        //    using Stream dbStream = reader.GetStream(colIndex);

        //    // 🚀 DEFENSIVE STREAMING: Chống Throw NotSupportedException
        //    if (!dbStream.CanRead) return null;

        //    try
        //    {
        //        if (dbStream.Length == 0 || dbStream.Length > MAX_IMAGE_SIZE) return null;
        //    }
        //    catch
        //    {
        //        return null; // Bắt an toàn các Provider không hỗ trợ .Length
        //    }

        //    using var ms = new MemoryStream();
        //    await dbStream.CopyToAsync(ms, token);
        //    ms.Position = 0;

        //    // 🚀 SAFE CLONING: Ngắt đứt hoàn toàn khóa GDI+
        //    using var tempBmp = Image.FromStream(ms, useEmbeddedColorManagement: false, validateImageData: true);
        //    using var original = new Bitmap(tempBmp);

        //    if (original.Width > MAX_PIXEL_LIMIT || original.Height > MAX_PIXEL_LIMIT)
        //        return null;

        //    return ResizeImageSafe(original, DISPLAY_SIZE, DISPLAY_SIZE);
        //}
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
        // 🛑 QUẢN LÝ ĐÓNG FORM (NGỦ ĐÔNG TRÊN RAM & DỌN DẸP RÁC)
        // =========================================================================
        private void kryptonButton1_DongFrom_Click(object sender, EventArgs e)
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
                RemoveFocusEvents(); // 🧹 Triệt tiêu event trước khi hủy form
                pictureBox2_AnhDaiDienAdmin.Image?.Dispose();
                // Không dispose _cachedAvatar vì nó là Static. GC sẽ lo khi App tắt.
            }
            catch { }
            base.OnFormClosed(e);
        }
        // =========================================================================
        // 🛠️ THREAD-SAFE PROFESSIONAL LOGGING VÀO DATABASE
        // =========================================================================
        private void GhiLogHeThong(string context, Exception? ex = null)
        {
            try
            {
                // 1. Xác định tài khoản (Fallback an toàn nếu app lỗi trước khi load xong)
                string taiKhoan = string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM)
                    ? "Hệ thống (Exception)"
                    : Module_TaiKhoan.TenTaiKhoan_RAM;

                // 2. Định dạng Hành động
                string hanhDong = $"[LỖI Form39] {context}";

                // 3. Xây dựng Ghi chú an toàn cho DB
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

                // 4. Bắn thẳng vào CSDL thông qua Module
                Module_NhatKy.GhiNhatKy(
                    taiKhoan: taiKhoan,
                    hanhDong: hanhDong,
                    ghiChu: ghiChu
                );
            }
            catch (Exception internalEx)
            {
                // Màng bảo vệ cuối: Lỗi DB khi ghi log thì đẩy ra cửa sổ Debug
                System.Diagnostics.Debug.WriteLine($"[CRITICAL] Ghi log CSDL thất bại. Context: {context}. Lỗi: {internalEx.Message}");
            }
        }

    }
}
