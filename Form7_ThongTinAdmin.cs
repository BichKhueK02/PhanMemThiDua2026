using Krypton.Toolkit;
using System.Security.Cryptography.X509Certificates;

namespace PhanMemThiDua2026
{
    public partial class Form7_ThongTinAdmin : Form
    {
        private readonly Color _focusColor = Color.FromArgb(0, 120, 215); // Windows 10/11 blue
        private readonly Dictionary<Control, Color> _originalBorderColors = new();

        public Form7_ThongTinAdmin()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
        }

        private void Form7_Load(object sender, EventArgs e)
        {
            textBox_PhienBanPhanMem.Text += Module_PhienBan.SoftwareVersion;
            textBox_ThongTinNgayCapNhat.Text += Module_PhienBan.NgayThangNamCapNhat;

            InitToolTips(); // Đã thêm gọi hàm khởi tạo ToolTips
            HienThiThongTinChuKySo();
            InitFocusHighlight(tabPage1);
            InitFocusHighlight(tabPage2);
        }

        private void kryptonButton1_Dong_Click(object sender, EventArgs e) => this.Close();
        private void kryptonButton1_DongFrom_Click(object sender, EventArgs e) => this.Close();

        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Gợi ý";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            toolTip1.InitialDelay = 300;
            toolTip1.AutoPopDelay = 2000;
            toolTip1.ReshowDelay = 100;
            toolTip1.ShowAlways = true;

            var tips = new Dictionary<Control, string>
            {
                { kryptonButton1_DongFrom, "Đóng cửa sổ hiện tại" },
                { kryptonButton1_Dong, "Đóng cửa sổ hiện tại" }
            };

            foreach (var tip in tips)
            {
                if (tip.Key != null) toolTip1.SetToolTip(tip.Key, tip.Value);
            }
        }

        #region Thông tin chữ ký số (Textbox + màu)
        private void HienThiThongTinChuKySo()
        {
            string exePath = Application.ExecutablePath;
            textBox_TenPhanMem.Text = Application.ProductName;
            textBox_PhienBan.Text = Module_PhienBan.SoftwareVersion;
            SetTrangThai(
                "Unknown",
                "Unknown",
                "—",
                "—",
                "Software integrity has not been evaluated",
                Color.DimGray
            );

            if (!File.Exists(exePath))
            {
                SetTrangThai(
                    "Executable file not found",
                    "—",
                    "—",
                    "—",
                    "⚠ The application executable file does not exist",
                    Color.Red
                );
                return;
            }

            try
            {
                X509Certificate2 cert = new X509Certificate2(
                    X509Certificate.CreateFromSignedFile(exePath)
                );

                if (cert == null)
                {
                    SetTrangThai(
                        "Not digitally signed",
                        "—",
                        "—",
                        "—",
                        "⚠ The software origin cannot be verified",
                        Color.Red
                    );
                    return;
                }

                // ✅ 1. Kiểm tra hết hạn
                if (DateTime.Now < cert.NotBefore || DateTime.Now > cert.NotAfter)
                {
                    SetTrangThai(
                        "Digital signature expired",
                        LayTenNhaPhatHanh(cert),
                        $"{cert.NotBefore:dd/MM/yyyy}  →  {cert.NotAfter:dd/MM/yyyy}",
                        cert.Thumbprint,
                        "⚠ The digital certificate is no longer valid",
                        Color.OrangeRed
                    );
                    return;
                }

                // ✅ 2. Kiểm tra chain hợp lệ
                X509Chain chain = new X509Chain();
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

                bool isValid = chain.Build(cert);

                if (!isValid)
                {
                    SetTrangThai(
                        "Invalid digital signature",
                        LayTenNhaPhatHanh(cert),
                        $"{cert.NotBefore:dd/MM/yyyy}  →  {cert.NotAfter:dd/MM/yyyy}",
                        cert.Thumbprint,
                        "⚠ Certificate chain validation failed",
                        Color.Red
                    );
                    return;
                }

                // ✅ 3. Hợp lệ hoàn toàn
                SetTrangThai(
                    "Digitally signed – Verified",
                    LayNguoiKy(cert),
                    $"{cert.NotBefore:dd/MM/yyyy}  →  {cert.NotAfter:dd/MM/yyyy}",
                    cert.Thumbprint,
                    "✔ Software integrity verified. Certificate chain valid.",
                    Color.Green
                );
            }
            catch
            {
                SetTrangThai(
                    "Unable to read digital signature",
                    "—",
                    "—",
                    "—",
                    "⚠ Digital signature verification failed",
                    Color.Red
                );
            }
        }

        private void SetTrangThai(
            string trangThai,
            string nhaPhatHanh,
            string thoiGianKy,
            string dauVanTay,
            string ketLuan,
            Color mau)
        {
            textBox_TrangThai.Text = trangThai;
            textBox_TrangThai.ForeColor = mau;
            textBox_ChuKySo.Text = nhaPhatHanh;
            textBox_ThoiGianKy.Text = thoiGianKy;
            textBox_DauVanTayDienTu.Text = dauVanTay;
            textBox_KetLuan.Text = ketLuan;
            textBox_KetLuan.ForeColor = mau;
        }

        private static string LayTenNhaPhatHanh(X509Certificate2 cert)
        {
            if (cert == null || string.IsNullOrWhiteSpace(cert.Subject))
                return "Không xác định";
            // Ưu tiên tổ chức (O=)
            foreach (string part in cert.Subject.Split(','))
            {
                string item = part.Trim();
                if (item.StartsWith("O=", StringComparison.OrdinalIgnoreCase))
                    return item.Substring(2).Trim();
            }
            // Fallback
            return cert.GetNameInfo(X509NameType.SimpleName, false);
        }

        private static string LayNguoiKy(X509Certificate2 cert)
        {
            if (cert == null)
                return "Không xác định";
            // Ưu tiên CN / SimpleName
            string ten = cert.GetNameInfo(X509NameType.SimpleName, false);
            if (!string.IsNullOrWhiteSpace(ten))
                return ten;
            // Fallback cuối
            return cert.Subject;
        }
        #endregion

        private void pictureBox3_Click(object sender, EventArgs e) => Module_DatabaseBackup.HienThiThongTinChungThu();

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            try
            {
                string version = Module_PhienBan.SoftwareVersion;
                string[] parts = version.Split('.');
                if (parts.Length != 3)
                {
                    MessageBox.Show(
                        $"Phiên bản hiện tại: {version}\nĐịnh dạng phiên bản không hợp lệ.",
                        "Thông tin phiên bản",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
                string msg =
                    "PHÂN TÍCH PHIÊN BẢN PHẦN MỀM\n" +
                    " \n" +
                    $"Phiên bản hiện tại : {version}\n\n" +
                    $"• Major : {parts[0]}  (Thay đổi lớn / kiến trúc)\n" +
                    $"• Minor : {parts[1]}  (Nâng cấp chức năng)\n" +
                    $"• Build : {parts[2]}  (Lần sửa đổi thứ {parts[2]})\n\n" +
                    $"{Module_PhienBan.NgayThangNamCapNhat}\n" +
                    "Trạng thái: Ổn định – đã kiểm thử thực tế.";
                MessageBox.Show(
                    msg,
                    "Khai thác thông tin phiên bản",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể hiển thị thông tin phiên bản.\n" + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void InitFocusHighlight(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                if (ctrl is KryptonTextBox ktb)
                {
                    ktb.GotFocus -= KryptonTextBox_GotFocus;
                    ktb.LostFocus -= KryptonTextBox_LostFocus;
                    ktb.GotFocus += KryptonTextBox_GotFocus;
                    ktb.LostFocus += KryptonTextBox_LostFocus;
                    // Lưu trạng thái viền ban đầu (chỉ lưu 1 lần)
                    if (!_originalBorderColors.ContainsKey(ktb))
                        _originalBorderColors[ktb] = ktb.StateCommon.Border.Color1;
                }

                if (ctrl.HasChildren)
                    InitFocusHighlight(ctrl);
            }
        }

        private void KryptonTextBox_GotFocus(object sender, EventArgs e)
        {
            if (sender is KryptonTextBox ktb)
            {
                ktb.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
                ktb.StateCommon.Border.Color1 = _focusColor;
                ktb.StateCommon.Border.Color2 = _focusColor;
                ktb.StateCommon.Border.Width = 2;
                ktb.Refresh(); // nhẹ hơn Update()
            }
        }

        private void KryptonTextBox_LostFocus(object sender, EventArgs e)
        {
            if (sender is KryptonTextBox ktb)
            {
                if (_originalBorderColors.TryGetValue(ktb, out Color originalColor))
                {
                    ktb.StateCommon.Border.Color1 = originalColor;
                    ktb.StateCommon.Border.Color2 = originalColor;
                }
                ktb.StateCommon.Border.Width = 1;
                ktb.Refresh();
            }
        }
    }
}