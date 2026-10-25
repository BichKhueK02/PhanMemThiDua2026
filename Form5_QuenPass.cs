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
        private bool _isInitializing = false; // Cờ bảo vệ
        private const int CB_SETEDITSEL = 0x0142;
        private const int PBM_SETSTATE = 0x0410;
        private const int PBST_NORMAL = 0x0001;
        private int soLanSai = 0;
        private const int MaxSoLanSai = 3;
        public Form5_QuenPass()
        {
            InitializeComponent();
            ApplyWebTheme(); // Giao diện kiểu web (xem Form5_QuenPass.Theme.cs)
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
                        using var conn = new SqliteConnection($"Data Source={_csdl1Path};Mode=ReadOnly;Default Timeout=5;Pooling=True;");
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
        private void SetupReadOnlyBox(Krypton.Toolkit.KryptonTextBox tb)
        {
            if (tb == null) return;
            tb.ReadOnly = true;
            tb.ShortcutsEnabled = true;
            // Màu nền xanh nhẹ
            Color mauNen = Color.FromArgb(241, 245, 249);
            tb.StateCommon.Back.Color1 = mauNen;
            // Giữ chữ rõ
            tb.StateCommon.Content.Color1 = Color.FromArgb(15, 23, 42);
            // Viền xanh nhẹ để dễ phân biệt
            tb.StateCommon.Border.Color1 = Color.FromArgb(203, 213, 225);
        }
        private void CenterFormOnScreen()
        {
            var screen = Screen.PrimaryScreen;
            if (screen == null) return;
            int x = (screen.Bounds.Width - this.Width) / 2;
            int y = (screen.Bounds.Height - this.Height) / 2;
            this.Location = new Point(x, y);
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
                using var conn = new SqliteConnection($"Data Source={_csdl1Path}");
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
            _isInitializing = true; // Bắt đầu nạp
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
                _isInitializing = false; // Kết thúc nạp
            }
            // 4. Focus vào ô trả lời
            this.ActiveControl = TextBox1_TraLoi1;
            TextBox1_TraLoi1.SelectionStart = TextBox1_TraLoi1.TextLength;
        }
        private void LoadCauHoiDaLuu()
        {
            try
            {
                using var conn = new SqliteConnection($"Data Source={_csdl1Path}");
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
        private void StartProgressBar()
        {
            if (progressBar1 == null) return;
            progressBar1.Style = ProgressBarStyle.Marquee;
            progressBar1.MarqueeAnimationSpeed = 30; // tốc độ vừa phải
            // Đảm bảo handle đã tạo
            if (progressBar1.IsHandleCreated)
            {
                SendMessage(progressBar1.Handle, PBM_SETSTATE,
                    (IntPtr)PBST_NORMAL, IntPtr.Zero);
            }
            progressBar1.Visible = true;
        }
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
        private static readonly Color MauVien = Color.FromArgb(203, 213, 225);  // slate-300
        private static readonly Color MauNenReadOnly = Color.FromArgb(241, 245, 249);  // slate-100
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
            StyleInput(TextBox1_TraLoi1, readOnly: false);
            StyleInput(TextBox2_TraLoi2, readOnly: false);
            StyleInput(Text_TenTaiKhoan, readOnly: true);
            StyleInput(Text_MatKhauLayLai, readOnly: true);
            StylePrimaryButton(Btn_KiemTra);
        }
        private void StyleInput(Krypton.Toolkit.KryptonTextBox tb, bool readOnly)
        {
            tb.StateCommon.Back.Color1 = readOnly ? MauNenReadOnly : Color.White;
            tb.StateCommon.Content.Color1 = MauChu;
            tb.StateCommon.Content.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            tb.StateCommon.Content.Padding = new Padding(6, 5, 6, 5);
            tb.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            tb.StateCommon.Border.Color1 = MauVien;
            tb.StateCommon.Border.Width = 1;
            tb.StateCommon.Border.Rounding = 10F;
            if (!readOnly)
            {
                // Viền xanh khi ô đang được focus (giống :focus trên web)
                tb.StateActive.Border.Color1 = MauChinh;
            }
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
                     ControlStyles.ResizeRedraw, true);
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
        internal static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            var path = new GraphicsPath();
            if (d <= 0)
            {
                path.AddRectangle(r);
                return path;
            }
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
        public HeroGradientPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var rc = ClientRectangle;
            if (rc.Width <= 0 || rc.Height <= 0) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var br = new LinearGradientBrush(rc,
                       Color.FromArgb(37, 99, 235),    // blue-600
                       Color.FromArgb(124, 58, 237),   // violet-600
                       125F))
            {
                g.FillRectangle(br, rc);
            }
            // Hình tròn trang trí, trong suốt nhẹ
            using (var soft = new SolidBrush(Color.FromArgb(26, 255, 255, 255)))
            using (var softer = new SolidBrush(Color.FromArgb(16, 255, 255, 255)))
            {
                g.FillEllipse(soft, rc.Right - 150, rc.Y - 90, 240, 240);
                g.FillEllipse(softer, rc.X - 110, rc.Bottom - 190, 300, 300);
                g.FillEllipse(softer, rc.Right - 70, rc.Bottom - 130, 120, 120);
            }
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
    /// Khung ô nhập bo góc, có viền xám và đổi sang viền xanh khi control bên trong đang focus.
    /// Dùng để bọc ComboBox (vẫn là ComboBox gốc nên code cũ không đổi).
    /// Control con được căn giữa theo chiều dọc.
    /// </summary>
    public class WebFieldPanel : Panel
    {
        private static readonly Color MauVien = Color.FromArgb(203, 213, 225);
        private static readonly Color MauFocus = Color.FromArgb(37, 99, 235);
        public WebFieldPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
        }
        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            e.Control!.Enter += (s, a) => Invalidate();
            e.Control.Leave += (s, a) => Invalidate();
            e.Control.GotFocus += (s, a) => Invalidate();
            e.Control.LostFocus += (s, a) => Invalidate();
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
            bool focus = ContainsFocus;
            using var pen = new Pen(focus ? MauFocus : MauVien, focus ? 1.6F : 1F);
            using var path = WebCardPanel.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 10);
            g.DrawPath(pen, path);
        }
    }
}