using Microsoft.Data.Sqlite;
using System.Runtime.InteropServices;

namespace PhanMemThiDua2026
{
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
            //this.ShowInTaskbar = false;
            this.Shown += Form5_Shown;
            ComboBox1_CauHoi1.HandleCreated += ComboBox_ClearHighlight;
            ComboBox2_CauHoi2.HandleCreated += ComboBox_ClearHighlight;
        }
        private void Form5_Load(object sender, EventArgs e)
        {
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            DoubleBuffered = true;
            AcceptButton = Btn_KiemTra;
            statusStrip1.SizingGrip = false;

            // ===== 2️⃣ CẤU HÌNH TEXTBOX CHỈ XEM (CHO COPY) =====
            SetupReadOnlyBox(Text_TenTaiKhoan);
            SetupReadOnlyBox(Text_MatKhauLayLai);

            // ===== 3️⃣ CĂN GIỮA FORM =====
            CenterFormOnScreen();

            // ===== 4️⃣ KHỞI ĐỘNG PROGRESS =====
            StartProgressBar();
            label1_PhienBanPhanMem.Text = "Phiên bản: " + Module_PhienBan.SoftwareVersion;
            // ===== THIẾT LẬP CĂN PHẢI CHO NHÃN TÁC GIẢ =====
            toolStripStatusLabel1.Text = "Written by Trung Kien";
            toolStripStatusLabel1.Spring = true; // Chiếm toàn bộ khoảng trống còn lại trên thanh StatusStrip
            toolStripStatusLabel1.TextAlign = ContentAlignment.MiddleRight; // Đẩy chữ sang mép bên phải
        }
        private void SetupReadOnlyBox(Krypton.Toolkit.KryptonTextBox tb)
        {
            if (tb == null) return;

            tb.ReadOnly = true;
            tb.ShortcutsEnabled = true;

            // Màu nền xanh nhẹ
            Color mauNen = Color.FromArgb(230, 245, 255);
            tb.StateCommon.Back.Color1 = mauNen;

            // Giữ chữ rõ
            tb.StateCommon.Content.Color1 = Color.Black;

            // Viền xanh nhẹ để dễ phân biệt
            tb.StateCommon.Border.Color1 = Color.SteelBlue;
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
        private void Btn_KiemTra_Click(object sender, EventArgs e)
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
                        string cauHoiGiaiMa = BaoMatAES.GiaiMa(cauHoiDB_Enc).Trim();
                        string cauTraLoiGiaiMa = BaoMatAES.GiaiMa(cauTraLoiDB_Enc).Trim();

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
                        string taiKhoan = BaoMatAES.GiaiMa(readerAdmin.GetString(0));
                        string matKhau = BaoMatAES.GiaiMa(readerAdmin.GetString(1));

                        Text_TenTaiKhoan.Text = taiKhoan;
                        Text_MatKhauLayLai.Text = matKhau;
                        soLanSai = 0;

                        // KIỂM TRA TRẠNG THÁI: Đã đăng nhập hay chưa?
                        bool isDaDangNhap = !string.IsNullOrEmpty(SessionInfo.TenTaiKhoan);

                        if (isDaDangNhap)
                        {
                            // KỊCH BẢN 1: Đang gọi từ Form 12 (Đã vào phần mềm)
                            MessageBox.Show($"Xác minh thành công!\n\nMật khẩu của bạn là: {matKhau}",
                                            "Lấy lại mật khẩu",
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
                            MessageBox.Show($"Xác minh thành công!\n\nMật khẩu của bạn là: {matKhau}\n\nNhấn OK để đăng nhập ngay vào hệ thống.",
                                            "Giải mã thành công",
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
                        string cauHoiGiaiMa = BaoMatAES.GiaiMa(cauHoiMaHoa);

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


        private void PictureBox2_Click(object sender, EventArgs e)
        {
            // Tối ưu chuỗi: Sửa lỗi cú pháp và căn chỉnh ngắt dòng cho vuông vắn giao diện
            string msgTuChoi = "Kính chào đồng chí!\n\n" +
                               "Hệ thống nhận thấy đồng chí chưa thực hiện đăng nhập.\n" +
                               "Vui lòng xác minh qua câu hỏi bảo mật hoặc đăng nhập\n" +
                               "để xem thông tin.\n\n" +
                               "Trong trường hợp cần cấp lại quyền hoặc hỗ trợ kỹ thuật,\n" +
                               "xin vui lòng liên hệ người phát triển:\n\n" +
                               "  • Admin: TrungKien\n" +
                               "  • Điện thoại: 0975 287 973\n" +
                               "  • Email: tramnamcodon535@gmail.com\n\n" +
                               "Trân trọng!";
            string tieuDe = "Nếu tôi quên thông tin khôi phục?";

            // Gọi Form ảo chuyên dụng Liên hệ / Cảnh báo
            HienThiFormAo_LienHe(tieuDe, msgTuChoi);
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
                lblTitle.StateCommon.ShortText.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
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
                txtContent.StateCommon.Content.Font = new System.Drawing.Font("Segoe UI", 11.5F, System.Drawing.FontStyle.Regular);
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
                btnCopy.StateCommon.Content.ShortText.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
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
                btnClose.StateCommon.Content.ShortText.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
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
    }
}