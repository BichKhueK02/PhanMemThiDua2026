///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using Microsoft.Data.Sqlite;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form5_QuenPass : Form
    {
        private readonly string _csdl1Path = Module_DanduongGPS.DuongDanCSDL1;
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(
            IntPtr hWnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam);
        //private bool _isInitializing = false; // Cờ bảo vệ
        private const int CB_SETEDITSEL = 0x0142;
        //private const int PBM_SETSTATE = 0x0410;
        //private const int PBST_NORMAL = 0x0001;
        private int soLanSai = 0;
        private const int MaxSoLanSai = 3;
        public Form5_QuenPass()
        {
            InitializeComponent();
            pnlCard.CornerRadius = 18;   // thẻ trắng bo 18px
            pnlHero.CornerRadius = 0;    // ⭐ Hero KHÔNG tự bo → để Card clip
            Module_BieuTuongNut.ApDung(this);   // biểu tượng nút: Hiện đại = tự vẽ, Cổ điển = ảnh gốc nhúng trong exe
            ApplyWebTheme(); // Giao diện kiểu web (xem Form5_QuenPass.Theme.cs)
            ApplyBorderless();   // form không viền, bo tròn, kéo thả được
            //this.ShowInTaskbar = false;
            this.Shown += Form5_Shown;
            ComboBox1_CauHoi1.HandleCreated += ComboBox_ClearHighlight;
            ComboBox2_CauHoi2.HandleCreated += ComboBox_ClearHighlight;
            // Cho phép click vào thông tin tác giả trên StatusStrip
            toolStripStatusLabel1.Click += ToolStripStatusLabel1_Click;
        }
        private async void Form5_Load(object? sender, EventArgs e)
        {
            // 1. CẤU HÌNH CỬA SỔ
            DoubleBuffered = true;
            AcceptButton = Btn_KiemTra;
            // Không cho phép thay đổi kích thước StatusStrip
            statusStrip1.SizingGrip = false;
            // 2. CẤU HÌNH CÁC Ô HIỂN THỊ THÔNG TIN
            SetupReadOnlyBox(Text_TenTaiKhoan);
            SetupReadOnlyBox(Text_MatKhauLayLai);
            // 5. HIỂN THỊ THÔNG TIN PHIÊN BẢN
            label1_PhienBanPhanMem.Text = $"Phiên bản: {Module_PhienBan.SoftwareVersion}";
            // 6. CẤU HÌNH NÚT HƯỚNG DẪN TRÊN STATUS STRIP
            toolStripStatusLabel1.Text = "Hướng dẫn tôi";
            toolStripStatusLabel1.Spring = true;
            toolStripStatusLabel1.TextAlign = ContentAlignment.MiddleRight;
            toolStripStatusLabel1.IsLink = true;
            // ⭐ GỌI HÀM TẢI ẢNH ĐẠI DIỆN LÊN PictureBox2
            await LoadAnhDaiDienAsync();
        }
        // Khai báo P/Invoke
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_NCRENDERING_POLICY = 2;
        private const int DWMNCRP_DISABLED = 1;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;  // Win11 only
        private const int DWMWCP_DONOTROUND = 1;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                // Tắt viền glass DWM
                int policy = DWMNCRP_DISABLED;
                DwmSetWindowAttribute(Handle, DWMWA_NCRENDERING_POLICY, ref policy, sizeof(int));

                // Win11: tắt tự bo góc của hệ điều hành (nếu không sẽ đè lên Region ta vẽ)
                int corner = DWMWCP_DONOTROUND;
                DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
            }
            catch { /* OS cũ không hỗ trợ */ }
        }

        private async Task LoadAnhDaiDienAsync()
        {
            try
            {
                Image? img = null;
                // 1. Thử đọc ảnh từ CSDL (bảng AvatarAdmin ID = 1)
                if (!string.IsNullOrWhiteSpace(_csdl1Path) && File.Exists(_csdl1Path))
                {
                    try
                    {
                        using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl1Path, chiDoc: true, thoiGianCho: 5));
                        await conn.OpenAsync();
                        using var cmd = new SqliteCommand("SELECT ThumbnailAnh, DuLieuAnh FROM AvatarAdmin WHERE ID = 1 LIMIT 1;", conn);
                        using var reader = await cmd.ExecuteReaderAsync();
                        if (await reader.ReadAsync())
                        {
                            int thumbOrdinal = reader.GetOrdinal("ThumbnailAnh");
                            int fullOrdinal = reader.GetOrdinal("DuLieuAnh");
                            foreach (int ordinal in new[] { thumbOrdinal, fullOrdinal })
                            {
                                byte[]? imageBytes = DocDuLieuAnhLinhHoat(reader, ordinal);
                                if (imageBytes != null && imageBytes.Length > 0)
                                {
                                    try
                                    {
                                        using var ms = new MemoryStream(imageBytes);
                                        using var tempBmp = Image.FromStream(ms, false, true);
                                        img = new Bitmap(tempBmp);
                                        break; // Đã đọc thành công ảnh từ DB
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Lỗi đọc ảnh từ CSDL: " + ex.Message);
                    }
                }
                // 2. Nếu CSDL không có ảnh, lấy ảnh mặc định từ Resources (ac.png)
                if (img == null && Properties.Resources.ac != null)
                {
                    img = new Bitmap(Properties.Resources.ac);
                }
                // 3. Hiển thị lên PictureBox2 an toàn
                if (PictureBox2 != null)
                {
                    if (PictureBox2.InvokeRequired)
                    {
                        PictureBox2.Invoke(new Action(() =>
                        {
                            var oldImage = PictureBox2.Image;
                            PictureBox2.Image = img;
                            PictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
                            oldImage?.Dispose();
                        }));
                    }
                    else
                    {
                        var oldImage = PictureBox2.Image;
                        PictureBox2.Image = img;
                        PictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
                        oldImage?.Dispose();
                    }
                }
                else
                {
                    img?.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi tải ảnh Form5: " + ex.Message);
                // Fallback an toàn nếu có lỗi
                try
                {
                    if (PictureBox2 != null && Properties.Resources.ac != null)
                    {
                        PictureBox2.Image = new Bitmap(Properties.Resources.ac);
                        PictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
                    }
                }
                catch { }
            }
        }
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
                System.Diagnostics.Debug.WriteLine("Lỗi đọc cột ảnh: " + ex.Message);
                return null;
            }
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                PictureBox2.Image?.Dispose();
            }
            catch { }
            base.OnFormClosed(e);
        }
        private void SetupReadOnlyBox(TextBox tb)
        {
            if (tb == null) return;
            tb.ReadOnly = true;
            tb.ShortcutsEnabled = true;
            // Màu nền/viền do WebFieldPanel bọc ngoài quyết định (xem ApplyWebTheme)
        }
        // ====== Form không viền + bo tròn ======
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HTCAPTION = 2;

        private void ApplyBorderless()
        {
            FormBorderStyle = FormBorderStyle.None;
            SizeChanged += (s, e) => CapNhatBoGoc();
            Load += (s, e) => CapNhatBoGoc();   // ⭐ cập nhật lại sau khi form đã show
            CapNhatBoGoc();
            // Không còn thanh tiêu đề: bấm giữ chuột trên nền/hero để kéo form
            foreach (Control c in new Control[] { pnlRoot, pnlCard, pnlRight, pnlHero, tlpHero, lblHeroTitle, lblHeroDesc, lblHeroHint, lblTieuDe, lblMoTa, statusStrip1 })
                c.MouseDown += KeoForm;
        }
        // 1. Bo góc Form mượt hơn + làm dịu viền Region
        private void CapNhatBoGoc()
        {
            if (Width <= 0 || Height <= 0) return;
            int r = (int)Math.Round(20 * DeviceDpi / 96f);
            using var path = WebCardPanel.RoundedRect(new Rectangle(0, 0, Width, Height), r);
            var cu = Region;
            Region = new Region(path);
            cu?.Dispose();
            Invalidate();
        }
        private void KeoForm(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
        }

        private void Form5_Shown(object? sender, EventArgs e)
        {
            InitCauHoiVaFocus();
        }
        private void Btn_KiemTra_Click(object? sender, EventArgs e)
        {
            string cauHoi1 = ComboBox1_CauHoi1.Text.Trim();
            string traLoi1 = TextBox1_TraLoi1.Text.Trim();
            string cauHoi2 = ComboBox2_CauHoi2.Text.Trim();
            string traLoi2 = TextBox2_TraLoi2.Text.Trim();
            if (string.IsNullOrWhiteSpace(cauHoi1) || string.IsNullOrWhiteSpace(traLoi1) ||
                string.IsNullOrWhiteSpace(cauHoi2) || string.IsNullOrWhiteSpace(traLoi2))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin!", "Cảnh báo",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl1Path));
                conn.Open();
                // TỐI ƯU HIỆU SUẤT: Gom chung thành 1 vòng truy vấn
                string sqlKiemTra = "SELECT ID, CauHoi, CauTraLoi FROM CauHoiBaoMat WHERE ID IN (1, 2)";
                bool cau1HopLe = false;
                bool cau2HopLe = false;
                using (var cmd = new SqliteCommand(sqlKiemTra, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        string cauHoiDB_Enc = reader.GetString(1);
                        string cauTraLoiDB_Enc = reader.GetString(2);
                        // ⭐ SỬA LỖI LOGIC: Phải Giải Mã CSDL rồi mới so sánh (Giống hệt cách Form1 làm)
                        string cauHoiGiaiMa = Module_BaoMatAES.GiaiMa(cauHoiDB_Enc).Trim();
                        string cauTraLoiGiaiMa = Module_BaoMatAES.GiaiMa(cauTraLoiDB_Enc).Trim();
                        if (id == 1)
                        {
                            // Dùng StringComparison.OrdinalIgnoreCase để cho phép người dùng lỡ gõ hoa/thường vẫn qua được (UX tốt hơn)
                            if (string.Equals(cauHoiGiaiMa, cauHoi1, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(cauTraLoiGiaiMa, traLoi1, StringComparison.OrdinalIgnoreCase))
                            {
                                cau1HopLe = true;
                            }
                        }
                        else if (id == 2)
                        {
                            if (string.Equals(cauHoiGiaiMa, cauHoi2, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(cauTraLoiGiaiMa, traLoi2, StringComparison.OrdinalIgnoreCase))
                            {
                                cau2HopLe = true;
                            }
                        }
                    }
                }
                bool hopLe = cau1HopLe && cau2HopLe; // Cả 2 câu đều phải đúng
                if (hopLe)
                {
                    // Lấy TenTaiKhoan và MatKhau từ bảng Admin
                    string sqlAdmin = "SELECT TenTaiKhoan, MatKhau FROM Admin WHERE ID=1";
                    using var cmdAdmin = new SqliteCommand(sqlAdmin, conn);
                    using var readerAdmin = cmdAdmin.ExecuteReader();
                    if (readerAdmin.Read())
                    {
                        string taiKhoan = Module_BaoMatAES.GiaiMa(readerAdmin.GetString(0));
                        string matKhau = Module_BaoMatAES.GiaiMa(readerAdmin.GetString(1));
                        Text_TenTaiKhoan.Text = taiKhoan;
                        Text_MatKhauLayLai.Text = matKhau;
                        soLanSai = 0;
                        // KIỂM TRA TRẠNG THÁI: Đã đăng nhập hay chưa?
                        bool isDaDangNhap = !string.IsNullOrEmpty(SessionInfo.TenTaiKhoan);
                        if (isDaDangNhap)
                        {
                            // KỊCH BẢN 1: Đang gọi từ Form 12 (Đã vào phần mềm)
                            MessageBox.Show($"Đối chiếu và giải mã thành công!\n\nMật khẩu của bạn là: {matKhau}",
                                            "Kết quả giải mã",
                                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                            try
                            {
                                Module_NhatKy.GhiNhatKy(
                                    taiKhoan: SessionInfo.TenTaiKhoan,
                                    hanhDong: "Xem lại thông tin tài khoản và mật khẩu từ mục Cấu hình",
                                    ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                                );
                            }
                            catch { }
                        }
                        else
                        {
                            // KỊCH BẢN 2: Đang gọi từ Form 1 (Chưa đăng nhập)
                            MessageBox.Show($"Đối chiếu và giải mã thành công!\n\nMật khẩu của bạn là: {matKhau}\n\nNhấn OK để đăng nhập ngay vào hệ thống.",
                                            "Kết quả giải mã",
                                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                            SessionInfo.TenTaiKhoan = taiKhoan;
                            SessionInfo.ThoiGianDangNhap = DateTime.Now;
                            try
                            {
                                Module_NhatKy.GhiNhatKy(
                                    taiKhoan: taiKhoan,
                                    hanhDong: "Đăng nhập thành công từ Khôi phục mật khẩu",
                                    ghiChu: $"Thời gian: {SessionInfo.ThoiGianDangNhap:dd-MM-yyyy HH:mm:ss}"
                                );
                            }
                            catch { }
                        }
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
                else
                {
                    soLanSai++;
                    if (soLanSai >= MaxSoLanSai)
                    {
                        MessageBox.Show("Bạn đã thử quá 3 lần! Hệ thống nghi ngờ truy cập trái phép, phần mềm sẽ khóa.", "Lỗi bảo mật",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.DialogResult = DialogResult.Abort;
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show($"Thông tin câu hỏi hoặc câu trả lời không chính xác! Lần sai thứ {soLanSai}/{MaxSoLanSai}", "Cảnh báo",
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi kiểm tra dữ liệu: {ex.Message}", "Lỗi",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ComboBox_ClearHighlight(object? sender, EventArgs e)
        {
            if (sender is ComboBox cb &&
                cb.DropDownStyle == ComboBoxStyle.DropDown &&
                cb.IsHandleCreated)
            {
                SendMessage(cb.Handle, CB_SETEDITSEL, 0, -1);
            }
        }
        private void InitCauHoiVaFocus()
        {
            //_isInitializing = true; // Bắt đầu nạp
            try
            {
                // 1. Nạp danh sách gợi ý từ Module
                Module_KhoiTaoCSDL.NapDuLieuCauHoiBaoMat(Module_KhoiTaoCSDL.DanhSachCauHoiNhom1, ComboBox1_CauHoi1);
                Module_KhoiTaoCSDL.NapDuLieuCauHoiBaoMat(Module_KhoiTaoCSDL.DanhSachCauHoiNhom2, ComboBox2_CauHoi2);
                // 2. Load câu hỏi thật từ CSDL
                LoadCauHoiDaLuu();
                // 3. CHO PHÉP người dùng chọn hoặc tự nhập câu hỏi mới
                ComboBox1_CauHoi1.Enabled = true;
                ComboBox2_CauHoi2.Enabled = true;
            }
            finally
            {
                //_isInitializing = false; // Kết thúc nạp
            }
            // 4. Focus vào ô trả lời
            this.ActiveControl = TextBox1_TraLoi1;
            TextBox1_TraLoi1.SelectionStart = TextBox1_TraLoi1.TextLength;
        }
        private void LoadCauHoiDaLuu()
        {
            try
            {
                using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl1Path));
                conn.Open();
                string sql = "SELECT ID, CauHoi FROM CauHoiBaoMat WHERE ID IN (1, 2)";
                using var cmd = new SqliteCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string cauHoiMaHoa = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                    if (!string.IsNullOrWhiteSpace(cauHoiMaHoa))
                    {
                        string cauHoiGiaiMa = Module_BaoMatAES.GiaiMa(cauHoiMaHoa);
                        if (id == 1)
                        {
                            // Nếu câu hỏi nằm trong danh sách có sẵn -> Chọn Index
                            // Nếu là câu hỏi tự gõ (không có trong list) -> Gán vào thuộc tính Text
                            int idx = ComboBox1_CauHoi1.FindStringExact(cauHoiGiaiMa);
                            if (idx != -1) ComboBox1_CauHoi1.SelectedIndex = idx;
                            else ComboBox1_CauHoi1.Text = cauHoiGiaiMa;
                        }
                        else if (id == 2)
                        {
                            int idx = ComboBox2_CauHoi2.FindStringExact(cauHoiGiaiMa);
                            if (idx != -1) ComboBox2_CauHoi2.SelectedIndex = idx;
                            else ComboBox2_CauHoi2.Text = cauHoiGiaiMa;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi load CSDL: " + ex.Message);
            }
        }
        //private void StartProgressBar()
        //{
        //if (progressBar1 == null) return;
        //progressBar1.Style = ProgressBarStyle.Marquee;
        //progressBar1.MarqueeAnimationSpeed = 30; // tốc độ vừa phải
        //// Đảm bảo handle đã tạo
        private void StopProgressBar()
        {
            if (progressBar1 == null) return;
            progressBar1.MarqueeAnimationSpeed = 0;
            progressBar1.Style = ProgressBarStyle.Blocks;
            progressBar1.Visible = false;
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopProgressBar();
            // DỌN DẸP BỘ NHỚ TRỰC QUAN: Xóa sạch text để lần sau mở lên (từ RAM) nó như mới
            TextBox1_TraLoi1.Text = string.Empty;
            TextBox2_TraLoi2.Text = string.Empty;
            Text_TenTaiKhoan.Text = string.Empty;
            Text_MatKhauLayLai.Text = string.Empty;
            // Xóa luôn trạng thái Focus cũ, đưa về ô đầu tiên
            this.ActiveControl = TextBox1_TraLoi1;
            // ĐỐI VỚI SHOWDIALOG: KHÔNG gọi e.Cancel = true ở đây!
            // Cứ để base.OnFormClosing chạy, WinForms sẽ tự động Hide() form này thay vì Dispose.
            base.OnFormClosing(e);
        }
        /// <summary>
        /// Dựng Form động kế thừa FormAoBase (Chống giật 100%).
        /// Chuyên hiển thị Thông báo Liên hệ hỗ trợ.
        /// Sử dụng tone màu Cam Đất (Warning), TextBox tàng hình để bôi đen và nút Copy danh thiếp.
        /// </summary>
        private void HienThiFormAo_LienHe(string tieuDe, string noiDung)
        {
            // Tiêu chuẩn hóa ký tự xuống dòng cho TextBox
            string noiDungChuan = noiDung.Replace("\n", Environment.NewLine);
            // ⭐ SỬ DỤNG CLASS FORMAOBASE ĐỂ KÍCH HOẠT CHỐNG GIẬT TẦNG HỆ ĐIỀU HÀNH
            using (var formAo = new FormAoBase())
            {
                formAo.Text = "Hỗ trợ hệ thống";
                formAo.Size = new System.Drawing.Size(780, 640); // Giữ nguyên kích thước lớn của bác
                formAo.FormBorderStyle = FormBorderStyle.FixedDialog;
                formAo.MaximizeBox = false;
                formAo.MinimizeBox = false;
                formAo.ShowIcon = false;
                formAo.ShowInTaskbar = false; // Chuẩn UI hộp thoại
                // --- 1. PANEL TIÊU ĐỀ (Tone màu Cam Đất - Warning) ---
                var panelTop = new Krypton.Toolkit.KryptonPanel
                {
                    Dock = DockStyle.Top,
                    Height = 70,
                    Padding = new Padding(30, 25, 20, 5)
                };
                panelTop.StateCommon.Color1 = System.Drawing.Color.White;
                var lblTitle = new Krypton.Toolkit.KryptonLabel
                {
                    Text = tieuDe.ToUpper(),
                    Dock = DockStyle.Fill,
                    AutoSize = false
                };
                lblTitle.StateCommon.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 13F, System.Drawing.FontStyle.Bold);
                lblTitle.StateCommon.ShortText.Color1 = System.Drawing.Color.FromArgb(211, 84, 0);
                panelTop.Controls.Add(lblTitle);
                // --- 2. ĐƯỜNG KẺ NGANG (Separator) ---
                var separator = new Label
                {
                    Height = 1,
                    Dock = DockStyle.Top,
                    BackColor = System.Drawing.Color.FromArgb(240, 220, 200),
                    Margin = new Padding(0, 5, 0, 10)
                };
                // --- 3. NỘI DUNG VĂN BẢN (TextBox TÀNG HÌNH) ---
                var panelContent = new Krypton.Toolkit.KryptonPanel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(30, 15, 30, 20)
                };
                panelContent.StateCommon.Color1 = System.Drawing.Color.White;
                var txtContent = new Krypton.Toolkit.KryptonTextBox
                {
                    Text = noiDungChuan,
                    ReadOnly = true,
                    Multiline = true,
                    Dock = DockStyle.Fill,
                    WordWrap = true,
                    ScrollBars = ScrollBars.Vertical
                };
                txtContent.StateCommon.Back.Color1 = System.Drawing.Color.White;
                txtContent.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.None;
                txtContent.StateCommon.Content.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 11.5F, System.Drawing.FontStyle.Regular);
                txtContent.StateCommon.Content.Color1 = System.Drawing.Color.FromArgb(45, 45, 45);
                txtContent.StateCommon.Content.Padding = new Padding(0);
                // --- 4. PANEL NÚT BẤM (Nền xám nhạt) ---
                var panelBottom = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 75,
                    BackColor = System.Drawing.Color.WhiteSmoke
                };
                // Nút 1: Copy thông tin
                var btnCopy = new Krypton.Toolkit.KryptonButton
                {
                    Text = "Sao chép thông tin",
                    Width = 190,
                    Height = 42
                };
                btnCopy.StateCommon.Content.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 10.5F, System.Drawing.FontStyle.Bold);
                btnCopy.StateCommon.Border.Rounding = 6;
                btnCopy.Click += (s, ev) =>
                {
                    try
                    {
                        Clipboard.SetText(noiDungChuan);
                        MessageBox.Show(formAo, "Đã sao chép thông tin liên hệ vào bộ nhớ tạm!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch { }
                };
                // Nút 2: Đóng
                var btnClose = new Krypton.Toolkit.KryptonButton
                {
                    Text = "Đóng",
                    Width = 140,
                    Height = 42
                };
                btnClose.StateCommon.Content.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 10.5F, System.Drawing.FontStyle.Bold);
                btnClose.StateCommon.Border.Rounding = 6;
                // Căn giữa cụm 2 nút bấm với khoảng cách 20px
                int totalWidth = btnCopy.Width + 20 + btnClose.Width;
                int startX = (formAo.Width - totalWidth) / 2;
                btnCopy.Location = new System.Drawing.Point(startX, 16);
                btnClose.Location = new System.Drawing.Point(startX + btnCopy.Width + 20, 16);
                panelBottom.Controls.Add(btnCopy);
                panelBottom.Controls.Add(btnClose);
                // --- 5. RÁP LAYER ---
                panelContent.Controls.Add(txtContent);
                panelContent.Controls.Add(separator);
                panelTop.Controls.Add(lblTitle);
                // Đẩy TextBox lên trên để không bị đè, đẩy Separator xuống dưới
                txtContent.BringToFront();
                separator.SendToBack();
                formAo.Controls.Add(panelContent);
                formAo.Controls.Add(panelTop);
                formAo.Controls.Add(panelBottom);
                formAo.AcceptButton = btnClose;
                formAo.CancelButton = btnClose;
                // Tránh TextBox bị bôi đen toàn bộ văn bản khi vừa hiển thị Form
                formAo.Shown += (s, ev) => btnClose.Focus();
                formAo.ShowDialog(this);
            }
        }
        /// <summary>
        /// Hiển thị thông báo hướng dẫn khi người dùng chưa đăng nhập.
        /// </summary>
        private void HienThiThongBaoQuenThongTinKhoiPhuc()
        {
            const string tieuDe = "Nếu tôi quên thông tin khôi phục?";
            string msgTuChoi =
                $"Kính chào {Module_HeThong.Tu_dong_chi} !\n\n" +
                $"Hệ thống nhận thấy {Module_HeThong.Tu_dong_chi} chưa thực hiện đăng nhập.\n" +
                "Vui lòng xác minh qua câu hỏi bảo mật hoặc đăng nhập\n" +
                "để xem thông tin.\n\n" +
                "Trong trường hợp cần cấp lại quyền hoặc hỗ trợ kỹ thuật,\n" +
                "xin vui lòng liên hệ người phát triển:\n\n" +
                "  • Admin: TrungKien\n" +
                "  • Điện thoại: 0975 287 973\n" +
                "  • Email: tramnamcodon535@gmail.com\n\n" +
                "Trân trọng!";
            HienThiFormAo_LienHe(tieuDe, msgTuChoi);
        }
        /// <summary>
        /// Sự kiện khi nhấn vào PictureBox.
        /// </summary>
        private void PictureBox2_Click(object? sender, EventArgs e)
        {
            HienThiThongBaoQuenThongTinKhoiPhuc();
        }
        /// <summary>
        /// Sự kiện khi nhấn vào ToolStripStatusLabel.
        /// </summary>
        private void ToolStripStatusLabel1_Click(object? sender, EventArgs e)
        {
            HienThiThongBaoQuenThongTinKhoiPhuc();
        }
        // Bảng màu (lấy cảm hứng từ Tailwind)
        private static readonly Color MauChinh = Color.FromArgb(37, 99, 235);   // blue-600
        private static readonly Color MauChinhHover = Color.FromArgb(29, 78, 216);   // blue-700
        private static readonly Color MauChinhNhan = Color.FromArgb(30, 64, 175);   // blue-800
        private static readonly Color MauChu = Color.FromArgb(15, 23, 42);     // slate-900
        private static readonly Color MauNhan = Color.FromArgb(51, 65, 85);    // slate-700
        private static readonly Color MauNutPhu = Color.FromArgb(241, 245, 249);  // slate-100
        private static readonly Color MauNutPhuHover = Color.FromArgb(226, 232, 240);  // slate-200
        private static readonly Color MauNutPhuNhan = Color.FromArgb(203, 213, 225);  // slate-300
                                                                                      // 2. Bảng màu: thêm màu đỏ cho nút Thoát
        private static readonly Color MauDo = Color.FromArgb(220, 38, 38);         // red-600
        private static readonly Color MauDoHover = Color.FromArgb(185, 28, 28);    // red-700
        private static readonly Color MauDoNhan = Color.FromArgb(153, 27, 27);     // red-800
        /// <summary>
        /// WS_EX_COMPOSITED: vẽ toàn bộ control con qua một bộ đệm chung,
        /// hết giật/nhấp nháy khi rê chuột hoặc khi form vừa hiện lên.
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }
        /// <summary>Gọi một lần ngay sau InitializeComponent().</summary>
        private void ApplyWebTheme()
        {
            StyleInput(TextBox1_TraLoi1);
            StyleInput(TextBox2_TraLoi2);
            StyleInput(Text_TenTaiKhoan);
            StyleInput(Text_MatKhauLayLai);
            // Ô kết quả chỉ đọc: không đổi màu khi bấm vào (chỉ để bôi đen/sao chép)
            pnlTaiKhoan.MauNenFocus = pnlTaiKhoan.MauNen;
            pnlMatKhau.MauNenFocus = pnlMatKhau.MauNen;
            // Ô đang nhập: nhãn câu hỏi tương ứng chuyển xanh (thay cho viền focus)
            GanNhanFocus(lblCauHoi1, pnlCombo1, pnlTraLoi1);
            GanNhanFocus(lblCauHoi2, pnlCombo2, pnlTraLoi2);
            // Trong ApplyWebTheme():
            StylePrimaryButton(Btn_KiemTra);
            StyleDangerButton(Btn_Thoat);
        }
        private static void StyleDangerButton(Krypton.Toolkit.KryptonButton btn)
        {
            btn.StateCommon.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn.StateCommon.Back.Color1 = MauDo;
            btn.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            btn.StateCommon.Border.Color1 = MauDo;
            btn.StateCommon.Border.Width = 1;
            btn.StateCommon.Border.Rounding = 12F;
            btn.StateCommon.Content.ShortText.Color1 = Color.White;
            btn.StateCommon.Content.ShortText.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);

            btn.StateTracking.Back.Color1 = MauDoHover;
            btn.StateTracking.Border.Color1 = MauDoHover;
            btn.StatePressed.Back.Color1 = MauDoNhan;
            btn.StatePressed.Border.Color1 = MauDoNhan;

            btn.OverrideDefault.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn.OverrideDefault.Back.Color1 = MauDo;
            btn.OverrideDefault.Border.Color1 = MauDo;
            btn.OverrideDefault.Border.Rounding = 12F;

            btn.OverrideFocus.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn.OverrideFocus.Back.Color1 = MauDoHover;
            btn.OverrideFocus.Border.Color1 = MauDoHover;
            btn.OverrideFocus.Border.Rounding = 12F;
        }
        /// <summary>Ô nhập không viền: nền do WebFieldPanel bọc ngoài tô (bo góc, đổi màu khi focus).</summary>
        private static void StyleInput(TextBox tb)
        {
            tb.BorderStyle = BorderStyle.None;   // WebTextBox: không còn viền Windows
            tb.ForeColor = MauChu;
            tb.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
        }
        private static void GanNhanFocus(Label nhan, params Control[] cacO)
        {
            foreach (var o in cacO)
            {
                o.Enter += (s, e) => nhan.ForeColor = MauChinh;
                o.Leave += (s, e) => nhan.ForeColor = MauNhan;
            }
        }
        /// <summary>Nút phụ (Thoát): nền xám nhạt, không viền (viền trùng màu nền để giữ bo góc).</summary>
        private static void StyleSecondaryButton(Krypton.Toolkit.KryptonButton btn)
        {
            btn.StateCommon.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn.StateCommon.Back.Color1 = MauNutPhu;
            btn.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            btn.StateCommon.Border.Color1 = MauNutPhu;
            btn.StateCommon.Border.Width = 1;
            btn.StateCommon.Border.Rounding = 10F;
            btn.StateCommon.Content.ShortText.Color1 = MauNhan;
            btn.StateCommon.Content.ShortText.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btn.StateTracking.Back.Color1 = MauNutPhuHover;
            btn.StateTracking.Border.Color1 = MauNutPhuHover;
            btn.StatePressed.Back.Color1 = MauNutPhuNhan;
            btn.StatePressed.Border.Color1 = MauNutPhuNhan;
            btn.OverrideFocus.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn.OverrideFocus.Back.Color1 = MauNutPhuHover;
            btn.OverrideFocus.Border.Color1 = MauNutPhuHover;
            btn.OverrideFocus.Border.Rounding = 10F;
        }
        private void StylePrimaryButton(Krypton.Toolkit.KryptonButton btn)
        {
            btn.StateCommon.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn.StateCommon.Back.Color1 = MauChinh;
            btn.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            btn.StateCommon.Border.Color1 = MauChinh;
            btn.StateCommon.Border.Width = 1;
            btn.StateCommon.Border.Rounding = 10F;
            btn.StateCommon.Content.ShortText.Color1 = Color.White;
            btn.StateCommon.Content.ShortText.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            // Hover / nhấn
            btn.StateTracking.Back.Color1 = MauChinhHover;
            btn.StateTracking.Border.Color1 = MauChinhHover;
            btn.StatePressed.Back.Color1 = MauChinhNhan;
            btn.StatePressed.Border.Color1 = MauChinhNhan;
            // Nút là AcceptButton nên Krypton dùng thêm trạng thái "default"/"focus"
            btn.OverrideDefault.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn.OverrideDefault.Back.Color1 = MauChinh;
            btn.OverrideDefault.Border.Color1 = MauChinh;
            btn.OverrideDefault.Border.Rounding = 10F;
            btn.OverrideFocus.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn.OverrideFocus.Back.Color1 = MauChinh;
            btn.OverrideFocus.Border.Color1 = MauChinh;
            btn.OverrideFocus.Border.Rounding = 10F;
        }
    }
    /// <summary>
    /// Panel bo góc (dùng làm "card" và vòng tròn biểu tượng).
    /// Bo góc bằng Region nên các control con cũng bị cắt theo.
    /// </summary>
    public class WebCardPanel : Panel
    {
        private int _cornerRadius = 16;

        public WebCardPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
        }

        [DefaultValue(16)]
        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = Math.Max(0, value); UpdateRegion(); }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRegion();
        }

        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0) return;
            var old = Region;
            if (_cornerRadius <= 0)
            {
                Region = null;
            }
            else
            {
                using var path = RoundedRect(new Rectangle(0, 0, Width, Height), _cornerRadius);
                Region = new Region(path);
            }
            old?.Dispose();
        }

        // Overload cho pixel nguyên
        internal static GraphicsPath RoundedRect(Rectangle r, int radius)
            => RoundedRect(new RectangleF(r.X, r.Y, r.Width, r.Height), radius);

        // ⭐ Overload cho nửa-pixel (chống răng cưa) — trước đây bị thiếu
        internal static GraphicsPath RoundedRect(RectangleF r, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0 || r.Width <= 0 || r.Height <= 0)
            {
                path.AddRectangle(r);
                return path;
            }
            float d = Math.Min(radius * 2f, Math.Min(r.Width, r.Height));
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
    /// <summary>
    /// Panel nền gradient xanh → tím kèm vài hình tròn mờ trang trí (kiểu hero banner của web).
    /// </summary>


    public class HeroGradientPanel : Panel
    {
        private int _cornerRadius = 16;   // ⭐ BỔ SUNG field bị thiếu

        public HeroGradientPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
        }

        [DefaultValue(16)]
        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Width <= 2 || Height <= 2) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            float r = _cornerRadius * DeviceDpi / 96f;
            // ⭐ Gọi qua WebCardPanel.RoundedRect (method static của class khác)
            using var path = WebCardPanel.RoundedRect(
                new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f),
                (int)Math.Round(r));

            // Nền gradient xanh → tím (đúng tinh thần "HeroGradientPanel")
            using (var brush = new LinearGradientBrush(
                new RectangleF(0, 0, Width, Height),
                Color.FromArgb(37, 99, 235),    // blue-600
                Color.FromArgb(99, 102, 241),   // indigo-500
                45f))
            {
                g.FillPath(brush, path);
            }

            // Hình tròn mờ trang trí kiểu hero banner
            using (var br = new SolidBrush(Color.FromArgb(28, 255, 255, 255)))
            {
                g.FillEllipse(br, Width - 110, -70, 200, 200);
                g.FillEllipse(br, -70, Height - 130, 220, 220);
            }
        }
    }
    /// <summary>TextBox phẳng không viền, đặt trong WebFieldPanel (khung bo tròn do panel vẽ).</summary>
    public class WebTextBox : TextBox
    {
        public WebTextBox()
        {
            BorderStyle = BorderStyle.None;
        }
    }
    /// <summary>Panel có double-buffer.</summary>
    public class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            DoubleBuffered = true;
        }
    }
    /// <summary>TableLayoutPanel có double-buffer (mặc định của WinForms bị nháy khi vẽ lại).</summary>
    public class BufferedTableLayoutPanel : TableLayoutPanel
    {
        public BufferedTableLayoutPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            DoubleBuffered = true;
        }
    }
    /// <summary>
    /// Khung ô nhập kiểu "filled" của web: nền đặc bo góc, KHÔNG viền.
    /// Khi control bên trong đang focus thì nền chuyển sang xanh rất nhạt.
    /// Tự đồng bộ màu nền cho control con (ComboBox/KryptonTextBox) để không lộ mép.
    /// BackColor của panel = màu nền thẻ phía sau (để 4 góc bo trùng màu thẻ).
    /// Control con được căn giữa theo chiều dọc; bấm vào phần đệm cũng focus vào ô.
    /// </summary>
    public class WebFieldPanel : Panel
    {
        private Color _mauNen = Color.FromArgb(241, 245, 249);      // slate-100
        private Color _mauNenFocus = Color.FromArgb(232, 240, 254); // xanh rất nhạt
        private int _boGoc = 10;
        private bool _dangFocus;
        public WebFieldPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
        }
        [DefaultValue(typeof(Color), "241, 245, 249")]
        public Color MauNen
        {
            get => _mauNen;
            set { _mauNen = value; CapNhatMau(); }
        }
        [DefaultValue(typeof(Color), "232, 240, 254")]
        public Color MauNenFocus
        {
            get => _mauNenFocus;
            set { _mauNenFocus = value; CapNhatMau(); }
        }
        [DefaultValue(10)]
        public int BoGoc
        {
            get => _boGoc;
            set { _boGoc = Math.Max(0, value); Invalidate(); }
        }
        private Color MauHienTai => _dangFocus ? _mauNenFocus : _mauNen;
        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            if (e.Control != null) GanMauNen(e.Control, MauHienTai);
        }
        protected override void OnEnter(EventArgs e)
        {
            _dangFocus = true;
            CapNhatMau();
            base.OnEnter(e);
        }
        protected override void OnLeave(EventArgs e)
        {
            _dangFocus = false;
            CapNhatMau();
            base.OnLeave(e);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (Controls.Count > 0 && Controls[0].CanFocus) Controls[0].Focus();
        }
        private void CapNhatMau()
        {
            Color c = MauHienTai;
            foreach (Control con in Controls) GanMauNen(con, c);
            Invalidate();
        }
        private static void GanMauNen(Control con, Color c)
        {
            if (con is Krypton.Toolkit.KryptonTextBox ktb)
            {
                if (ktb.StateCommon.Back.Color1 != c) ktb.StateCommon.Back.Color1 = c;
            }
            else if (con.BackColor != c)
            {
                con.BackColor = c;
            }
        }
        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            foreach (Control c in Controls)
            {
                int w = Math.Max(0, Width - Padding.Horizontal);
                int y = Math.Max(0, (Height - c.Height) / 2);
                c.SetBounds(Padding.Left, y, w, c.Height);
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            if (Width < 4 || Height < 4) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int r = (int)Math.Round(_boGoc * DeviceDpi / 96f);
            using var br = new SolidBrush(MauHienTai);
            using var path = WebCardPanel.RoundedRect(new Rectangle(0, 0, Width, Height), r);
            g.FillPath(br, path);
        }
    }
    /// <summary>
    /// ComboBox phẳng không viền: vẫn là ComboBox gốc (code cũ không đổi), nhưng sau khi
    /// WinForms vẽ kiểu Flat (viền 1px + ô nút xám, đổi màu khi rê chuột/focus) thì phủ lại
    /// toàn bộ phần ngoài ô chữ bằng màu nền và tự vẽ mũi tên chevron mảnh.
    /// Vẽ trên CHÍNH DC của WM_PAINT nên không bị form WS_EX_COMPOSITED vẽ đè mất.
    /// </summary>
    public class WebComboBox : ComboBox
    {
        private const int WM_PAINT = 0x000F;
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)]
        private struct COMBOBOXINFO
        {
            public int cbSize;
            public RECT rcItem;
            public RECT rcButton;
            public int stateButton;
            public IntPtr hwndCombo;
            public IntPtr hwndItem;
            public IntPtr hwndList;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct PAINTSTRUCT
        {
            public IntPtr hdc;
            public int fErase;
            public RECT rcPaint;
            public int fRestore;
            public int fIncUpdate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] rgbReserved;
        }
        [DllImport("user32.dll")]
        private static extern bool GetComboBoxInfo(IntPtr hwndCombo, ref COMBOBOXINFO pcbi);
        [DllImport("user32.dll")]
        private static extern IntPtr BeginPaint(IntPtr hWnd, out PAINTSTRUCT lpPaint);
        [DllImport("user32.dll")]
        private static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);
        private Color _mauMuiTen = Color.FromArgb(100, 116, 139); // slate-500
        public WebComboBox()
        {
            FlatStyle = FlatStyle.Flat;
        }
        [DefaultValue(typeof(Color), "100, 116, 139")]
        public Color MauMuiTen
        {
            get => _mauMuiTen;
            set { _mauMuiTen = value; Invalidate(); }
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg != WM_PAINT || DropDownStyle == ComboBoxStyle.Simple)
            {
                base.WndProc(ref m);
                return;
            }
            if (m.WParam != IntPtr.Zero)
            {
                // Đã có DC sẵn (WM_PAINT gửi kèm HDC / in): vẽ gốc rồi phủ lên cùng DC
                base.WndProc(ref m);
                VePhu(m.WParam);
                return;
            }
            IntPtr hdc = BeginPaint(Handle, out PAINTSTRUCT ps);
            try
            {
                m.WParam = hdc;          // WinForms vẽ Flat vào DC này thay vì tự BeginPaint
                base.WndProc(ref m);
                VePhu(hdc);
            }
            finally
            {
                m.WParam = IntPtr.Zero;
                EndPaint(Handle, ref ps);
            }
        }
        private void VePhu(IntPtr hdc)
        {
            try
            {
                var info = new COMBOBOXINFO { cbSize = Marshal.SizeOf<COMBOBOXINFO>() };
                if (!GetComboBoxInfo(Handle, ref info)) return;
                var rcItem = Rectangle.FromLTRB(info.rcItem.Left, info.rcItem.Top, info.rcItem.Right, info.rcItem.Bottom);
                var rcNut = Rectangle.FromLTRB(info.rcButton.Left, info.rcButton.Top, info.rcButton.Right, info.rcButton.Bottom);
                using var g = Graphics.FromHdc(hdc);
                // 1. Xóa viền + ô nút xám: tô màu nền mọi chỗ trừ ô chữ
                using (var vung = new Region(ClientRectangle))
                {
                    vung.Exclude(rcItem);
                    using var brNen = new SolidBrush(Enabled ? BackColor : SystemColors.Control);
                    g.FillRegion(brNen, vung);
                }
                // 2. Mũi tên chevron mảnh ở giữa vùng nút
                if (rcNut.Width <= 0 || rcNut.Height <= 0) return;
                float k = DeviceDpi / 96f;
                float nuaRong = 4.5f * k, cao = 2.5f * k;
                float cx = rcNut.Left + rcNut.Width / 2f, cy = rcNut.Top + rcNut.Height / 2f;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(Enabled ? _mauMuiTen : SystemColors.GrayText, 1.6f * k)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                };
                g.DrawLines(pen, new[]
                {
                    new PointF(cx - nuaRong, cy - cao),
                    new PointF(cx, cy + cao),
                    new PointF(cx + nuaRong, cy - cao)
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[WebComboBox] " + ex.Message);
            }
        }
    }
}