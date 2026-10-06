using Krypton.Toolkit;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

// ==== HẰNG SỐ DÙNG CHUNG (TRÁNH MAGIC STRING RẢI RÁC) ====
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
        private void Form7_Load(object? sender, EventArgs e)
        {
            textBox_PhienBanPhanMem.Text += Module_PhienBan.SoftwareVersion;
            textBox_ThongTinNgayCapNhat.Text += Module_PhienBan.NgayThangNamCapNhat;
            InitToolTips(); // Đã thêm gọi hàm khởi tạo ToolTips
            HienThiThongTinChuKySo();
            InitFocusHighlight(tabPage1);
            InitFocusHighlight(tabPage2);
        }
        private void kryptonButton1_Dong_Click(object? sender, EventArgs e) => this.Close();
        private void kryptonButton1_DongFrom_Click(object? sender, EventArgs e) => this.Close();
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
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

    private const string PLACEHOLDER = "—";
    private const string DATE_FORMAT = "dd/MM/yyyy";

    /// <summary>
    /// Timeout tối đa cho việc kiểm tra thu hồi chứng chỉ (revocation check) qua mạng.
    /// Tránh treo UI vô thời hạn khi máy không có Internet hoặc CRL server không phản hồi.
    /// </summary>
    private static readonly TimeSpan ChainRevocationTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Kết quả xác thực chữ ký số — tách biệt hoàn toàn khỏi tầng hiển thị (UI),
    /// giúp hàm kiểm tra có thể unit-test độc lập, không phụ thuộc Form/Control.
    /// </summary>
    private sealed record KetQuaChuKySo(string TrangThai, string NguoiKyHoacNhaPhatHanh, string ThoiHanHieuLuc, string Thumbprint, string GhiChu,  Color MauHienThi);
    /// <summary>
    /// Hiển thị thông tin chữ ký số của file thực thi hiện tại lên giao diện.
    /// Chạy bất đồng bộ để không chặn luồng UI trong lúc kiểm tra revocation qua mạng.
    /// </summary>
    private async void HienThiThongTinChuKySo()
    {
        string exePath = Application.ExecutablePath;
        textBox_TenPhanMem.Text = Module_PhienBan.TenPhanMem;
        textBox_PhienBan.Text = Module_PhienBan.SoftwareVersion;
        // Trạng thái khởi tạo trong lúc chờ kiểm tra
        SetTrangThai("Unknown", "Unknown", PLACEHOLDER, PLACEHOLDER,
            "Software integrity has not been evaluated", Color.DimGray);

        if (!File.Exists(exePath))
        {
            SetTrangThai("Executable file not found", PLACEHOLDER, PLACEHOLDER, PLACEHOLDER,
                "⚠ The application executable file does not exist", Color.Red);
            return;
        }

        // 🌟 CHẠY KIỂM TRA CHỮ KÝ SỐ TRÊN THREAD POOL, TRÁNH GIẬT/ĐƠ FORM
        KetQuaChuKySo ketQua;
        using (var cts = new CancellationTokenSource(ChainRevocationTimeout + TimeSpan.FromSeconds(2)))
        {
            ketQua = await Task.Run(() => KiemTraChuKySo(exePath, cts.Token), cts.Token)
                               .ConfigureAwait(true); // true: quay lại UI thread để cập nhật control
        }

        SetTrangThai(
            ketQua.TrangThai,
            ketQua.NguoiKyHoacNhaPhatHanh,
            ketQua.ThoiHanHieuLuc,
            ketQua.Thumbprint,
            ketQua.GhiChu,
            ketQua.MauHienThi);
    }

    /// <summary>
    /// Logic thuần túy kiểm tra chữ ký số của file thực thi — KHÔNG đụng tới UI.
    /// Tách riêng để có thể unit-test và tái sử dụng ở nơi khác (log hệ thống, CLI, v.v).
    /// </summary>
    /// <param name="exePath">Đường dẫn file cần kiểm tra.</param>
    /// <param name="cancellationToken">Cho phép hủy nếu kiểm tra revocation quá lâu.</param>
    private KetQuaChuKySo KiemTraChuKySo(string exePath, CancellationToken cancellationToken)
    {
        X509Certificate2? cert = null;
        X509Chain? chain = null;

        try
        {
            // Bước 1: Đọc chữ ký số nhúng trong file (ném CryptographicException nếu chưa ký)
            cert = new X509Certificate2(X509Certificate.CreateFromSignedFile(exePath));
            string thoiHan = $"{cert.NotBefore:dd/MM/yyyy HH:mm:ss} → {cert.NotAfter:dd/MM/yyyy HH:mm:ss}";
                // Bước 2: Kiểm tra hết hạn chứng chỉ
            DateTime now = DateTime.Now;
            if (now < cert.NotBefore || now > cert.NotAfter)
            {
                return new KetQuaChuKySo(
                    "Digital signature expired",
                    LayTenNhaPhatHanh(cert),
                    thoiHan,
                    cert.Thumbprint,
                    "⚠ The digital certificate is no longer valid",
                    Color.OrangeRed);
            }
            // Bước 3: Kiểm tra chuỗi chứng chỉ (chain) — có timeout để không treo khi mất mạng
            chain = new X509Chain
            {
                ChainPolicy =
            {
                RevocationMode = X509RevocationMode.Online,
                RevocationFlag = X509RevocationFlag.EntireChain,
                VerificationFlags = X509VerificationFlags.NoFlag,
                UrlRetrievalTimeout = ChainRevocationTimeout
            }
            };

            bool isValid = chain.Build(cert);
            cancellationToken.ThrowIfCancellationRequested();

            if (!isValid)
            {
                // Gom lý do cụ thể để hỗ trợ chẩn đoán (hết hạn CA, không rõ nguồn gốc, bị thu hồi...)
                string lyDo = string.Join("; ",
                    chain.ChainStatus.Select(s => s.StatusInformation.Trim()));

                return new KetQuaChuKySo(
                    "Invalid digital signature",
                    LayTenNhaPhatHanh(cert),
                    thoiHan,
                    cert.Thumbprint,
                    $"⚠ Certificate chain validation failed: {lyDo}",
                    Color.Red);
            }

            // Bước 4: Hợp lệ hoàn toàn
            return new KetQuaChuKySo(
                "Digitally signed – Verified",
                LayNguoiKy(cert),
                thoiHan,
                cert.Thumbprint,
                "✔ Software integrity verified. Certificate chain valid.",
                Color.Green);
        }
        catch (CryptographicException)
        {
            // File tồn tại nhưng KHÔNG có chữ ký số nhúng — phân biệt rõ với lỗi đọc file
            return new KetQuaChuKySo(
                "Not digitally signed",
                PLACEHOLDER, PLACEHOLDER, PLACEHOLDER,
                "⚠ The software origin cannot be verified",
                Color.Red);
        }
        catch (OperationCanceledException)
        {
            return new KetQuaChuKySo(
                "Verification timed out",
                LayTenNhaPhatHanhAnToan(cert), PLACEHOLDER, cert?.Thumbprint ?? PLACEHOLDER,
                "⚠ Revocation check timed out (no network or slow CRL server)",
                Color.OrangeRed);
        }
        catch (Exception ex)
        {
            // Lỗi không lường trước (I/O, quyền truy cập...) — ghi log để chẩn đoán sau này
            Module_NhatKy.GhiNhatKy("System", "Kiểm tra chữ ký số",
                $"Lỗi không xác định khi kiểm tra chữ ký số: {ex.Message}");

            return new KetQuaChuKySo(
                "Unable to read digital signature",
                PLACEHOLDER, PLACEHOLDER, PLACEHOLDER,
                "⚠ Digital signature verification failed",
                Color.Red);
        }
        finally
        {
            // 🌟 GIẢI PHÓNG TÀI NGUYÊN NATIVE — TRÁNH RÒ RỈ HANDLE
            chain?.Dispose();
            cert?.Dispose();
        }
    }

    /// <summary>Lấy tên nhà phát hành một cách an toàn, không ném lỗi nếu cert null.</summary>
    private string LayTenNhaPhatHanhAnToan(X509Certificate2? cert)
        => cert == null ? PLACEHOLDER : LayTenNhaPhatHanh(cert);

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
        private void pictureBox3_Click(object? sender, EventArgs e) => Module_DatabaseBackup.HienThiThongTinChungThu();
        private void pictureBox4_Click(object? sender, EventArgs e)
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
        private void KryptonTextBox_GotFocus(object? sender, EventArgs e)
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
        private void KryptonTextBox_LostFocus(object? sender, EventArgs e)
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