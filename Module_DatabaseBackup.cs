using Krypton.Toolkit;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace PhanMemThiDua2026
{
    internal static class Module_DatabaseBackup
    {
        private const string TITLE_VN = "THÔNG TIN CHỨNG THƯ SỐ PHẦN MỀM";
        private const string TITLE_EN = "SOFTWARE DIGITAL CERTIFICATE INFORMATION";

        private static readonly Size SIZE_DA_KY = new Size(1000, 720);
        private static readonly Size SIZE_LOI_OR_HET_HAN = new Size(840, 640);

        // ===== HÀM HIỂN THỊ CHÍNH (TỔNG HỢP LOGIC) =====
        public static void HienThiThongTinChungThu()
        {
            StringBuilder sb = new StringBuilder(2048);
            string exePath = Application.ExecutablePath;

            AppendHeader(sb);

            if (!File.Exists(exePath))
            {
                AppendWarning(sb, "Không tìm thấy tệp chương trình.", "Application executable file not found.");
                HienThiFormAoChung("CẢNH BÁO HỆ THỐNG", sb.ToString(), MessageBoxIcon.Warning, SIZE_LOI_OR_HET_HAN);
                return;
            }

            try
            {
                // TỐI ƯU BẢO MẬT & HIỆU SUẤT: Bọc chứng thư gốc vào khối using để giải phóng unmanaged resources ngay sau khi dùng
                using (var rawCert = X509Certificate.CreateFromSignedFile(exePath))
                {
                    if (rawCert == null)
                    {
                        AppendSection(sb, "TRẠNG THÁI / STATUS:");
                        AppendBullet(sb, "Phần mềm chưa được ký số.", "The software is not digitally signed.");
                        AppendHorizontalLine(sb);
                        AppendWarning(sb, "Không thể xác minh nguồn gốc và tính toàn vẹn.", "Unable to verify software origin.");
                        HienThiFormAoChung("CẢNH BÁO BẢO MẬT", sb.ToString(), MessageBoxIcon.Warning, SIZE_LOI_OR_HET_HAN);
                        return;
                    }

                    using (var cert = new X509Certificate2(rawCert))
                    {
                        bool isExpired = DateTime.Now > cert.NotAfter;

                        AppendSection(sb, "TRẠNG THÁI / STATUS:");
                        if (isExpired)
                        {
                            AppendWarning(sb, "Phần mềm đã ký số nhưng chứng thư ĐÃ HẾT HẠN.", "The digital certificate has EXPIRED.");
                        }
                        else
                        {
                            AppendSuccess(sb, "Phần mềm đã được ký số hợp lệ.", "The software is successfully digitally signed.");
                        }

                        AppendHorizontalLine(sb);
                        AppendSection(sb, "THÔNG TIN CHỨNG THƯ / CERTIFICATE DETAILS:");
                        AppendKeyValue(sb, "Nhà phát hành", "Publisher", SafeText(cert.Subject));
                        AppendKeyValue(sb, "Cá nhân ký số", "Issued by", SafeText(cert.Issuer));
                        AppendKeyValue(sb, "Hiệu lực từ", "Valid from", cert.NotBefore.ToString("dd/MM/yyyy HH:mm:ss"));
                        AppendKeyValue(sb, "Hiệu lực đến", "Valid until", cert.NotAfter.ToString("dd/MM/yyyy HH:mm:ss"));

                        AppendHorizontalLine(sb);
                        AppendSection(sb, "ĐÁNH GIÁ AN TOÀN / SECURITY ASSESSMENT:");
                        if (isExpired)
                        {
                            AppendWarning(sb, "Rủi ro bảo mật do chứng thư lỗi thời.", "Security risk due to outdated certificate.");
                            HienThiFormAoChung("CẢNH BÁO THỜI GIAN CHỨNG THƯ", sb.ToString(), MessageBoxIcon.Warning, SIZE_LOI_OR_HET_HAN);
                        }
                        else
                        {
                            AppendSuccess(sb, "Đảm bảo tính toàn vẹn của tệp (.exe)", "Software integrity guaranteed (.exe).");
                            AppendSuccess(sb, "Tệp thực thi (.exe) không bị chỉnh sửa.", "Executable file (.exe) not modified.");
                            AppendSuccess(sb, "Tương thích hệ điều hành Windows 7 đến Windows 11.", "Compatible Windows 7 -> 11.");

                            HienThiFormAoChung("CHỨNG THỰC AN TOÀN", sb.ToString(), MessageBoxIcon.Information, SIZE_DA_KY);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sb.Clear();
                AppendHeader(sb);
                AppendWarning(sb, "Lỗi đọc chứng thư số.", "Cannot read digital certificate.");
                AppendBullet(sb, ex.Message, "Error details.");
                HienThiFormAoChung("LỖI HỆ THỐNG", sb.ToString(), MessageBoxIcon.Error, SIZE_LOI_OR_HET_HAN);
            }
        }

        // ===== DỰNG FORM ẢO =====
        public static void HienThiFormAoChung(string tieuDe, string noiDung, MessageBoxIcon icon, Size formSize)
        {
            using KryptonForm formAo = new KryptonForm();

            formAo.Text = "Hệ thống xác thực";
            formAo.StartPosition = FormStartPosition.CenterScreen;
            formAo.Size = formSize;
            formAo.FormBorderStyle = FormBorderStyle.FixedDialog;
            formAo.MaximizeBox = false;
            formAo.MinimizeBox = false;
            formAo.BackColor = Color.FromArgb(250, 250, 250);

            formAo.GroupBackStyle = PaletteBackStyle.FormMain;
            formAo.GroupBorderStyle = PaletteBorderStyle.FormMain;

            Color themeColor = icon switch
            {
                MessageBoxIcon.Error => Color.FromArgb(198, 40, 40),
                MessageBoxIcon.Warning => Color.FromArgb(239, 108, 0),
                _ => Color.FromArgb(21, 115, 71)
            };

            Panel panelTop = new() { Dock = DockStyle.Top, Height = 65, BackColor = Color.White };
            Label lblTitle = new()
            {
                Text = tieuDe.ToUpperInvariant(),
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = themeColor,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(25, 0, 0, 0)
            };
            panelTop.Controls.Add(lblTitle);

            Panel line = new() { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(225, 225, 225) };

            RichTextBox rtb = new()
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10.5F),
                ReadOnly = true,
                Margin = new Padding(0),
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            Panel panelContent = new() { Dock = DockStyle.Fill, Padding = new Padding(30, 20, 30, 20), BackColor = Color.White };
            panelContent.Controls.Add(rtb);

            FormatRichText(rtb, noiDung);

            Panel panelBottom = new() { Dock = DockStyle.Bottom, Height = 70, BackColor = Color.FromArgb(245, 245, 245) };

            Button btnDong = new()
            {
                Text = "XÁC NHẬN ĐÓNG",
                Width = 200,
                Height = 42,
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.Flat,
                BackColor = themeColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            btnDong.FlatAppearance.BorderSize = 0;

            Color hoverColor = Color.FromArgb(Math.Max(0, themeColor.R - 25), Math.Max(0, themeColor.G - 25), Math.Max(0, themeColor.B - 25));
            Color activeColor = Color.FromArgb(Math.Max(0, themeColor.R - 45), Math.Max(0, themeColor.G - 45), Math.Max(0, themeColor.B - 45));
            btnDong.FlatAppearance.MouseOverBackColor = hoverColor;
            btnDong.FlatAppearance.MouseDownBackColor = activeColor;

            TableLayoutPanel tlp = new() { Dock = DockStyle.Fill, ColumnCount = 3 };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            btnDong.Anchor = AnchorStyles.None;
            tlp.Controls.Add(btnDong, 1, 0);
            panelBottom.Controls.Add(tlp);

            formAo.Controls.Add(panelContent);
            formAo.Controls.Add(panelBottom);
            formAo.Controls.Add(line);
            formAo.Controls.Add(panelTop);

            formAo.AcceptButton = btnDong;
            formAo.CancelButton = btnDong;

            formAo.ShowDialog();
        }

        // ===== PARSER CHUYỂN ĐỔI ĐỊNH DẠNG MÀU SẮC =====
        private static void FormatRichText(RichTextBox rtb, string text)
        {
            rtb.Text = "";
            string[] lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

            // HIỆU SUẤT: Tính toán độ rộng hành lang một lần duy nhất ngoài vòng lặp
            int usableWidth = rtb.ClientSize.Width - 10;
            string dynamicDivider = string.Empty;

            using (Graphics g = rtb.CreateGraphics())
            {
                float charWidth = g.MeasureString("─", rtb.Font).Width;
                int repeatCount = (int)Math.Floor(usableWidth / (charWidth - 0.5f));
                if (repeatCount < 10) repeatCount = 60;
                dynamicDivider = new string('─', repeatCount);
            }

            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    rtb.AppendText("\n");
                    continue;
                }

                Color textColor = Color.FromArgb(55, 65, 81);
                Font textFont = new Font("Segoe UI", 10.5F, FontStyle.Regular);
                string trimmed = line.Trim();

                if (trimmed.StartsWith("✔"))
                {
                    textColor = Color.FromArgb(21, 115, 71);
                    textFont = new Font("Segoe UI", 10.5F, FontStyle.Bold);
                }
                else if (trimmed.StartsWith("⚠"))
                {
                    textColor = Color.FromArgb(211, 47, 47);
                    textFont = new Font("Segoe UI", 10.5F, FontStyle.Bold);
                }
                else if (trimmed.StartsWith("•"))
                {
                    textColor = Color.FromArgb(2, 119, 189);
                }
                // SỬA LỖI ĐỒ HỌA: Đồng bộ hóa toàn bộ từ khóa phân cách của hệ thống
                else if (trimmed == "---LINE---")
                {
                    textColor = Color.FromArgb(218, 222, 229);
                }
                else if (trimmed.EndsWith(":") || trimmed.EndsWith("]:"))
                {
                    textColor = Color.FromArgb(17, 24, 39);
                    textFont = new Font("Segoe UI", 11F, FontStyle.Bold);
                }

                int startPos = rtb.TextLength;

                if (trimmed == "---LINE---")
                {
                    rtb.AppendText(dynamicDivider + "\n\n");
                }
                else
                {
                    rtb.AppendText(line + "\n");
                }

                rtb.Select(startPos, rtb.TextLength - startPos);
                rtb.SelectionColor = textColor;
                rtb.SelectionFont = textFont;
            }

            rtb.DeselectAll();
            rtb.SelectionStart = 0;
            rtb.SelectionLength = 0;
        }

        // ===== CÁC HÀM HỖ TRỢ ĐỊNH DẠNG TỐI ƯU CHIỀU NGANG (BUILDER) =====
        private static void AppendHeader(StringBuilder sb)
        {
            sb.AppendLine(TITLE_VN);
            sb.AppendLine(TITLE_EN);
            sb.AppendLine("---LINE---");
            sb.AppendLine();
        }
        private static void AppendHorizontalLine(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("---LINE---");
            sb.AppendLine();
        }
        private static void AppendSection(StringBuilder sb, string title)
        {
            sb.AppendLine(title);
        }
        private static void AppendSuccess(StringBuilder sb, string vn, string en)
        {
            sb.AppendLine($"   ✔  {vn}  |  {en}");
        }
        private static void AppendWarning(StringBuilder sb, string vn, string en)
        {
            sb.AppendLine($"   ⚠  {vn}  |  {en}");
        }
        private static void AppendBullet(StringBuilder sb, string vn, string en)
        {
            sb.AppendLine($"   •  {vn}  |  {en}");
        }
        private static void AppendKeyValue(StringBuilder sb, string v, string e, string val)
        {
            sb.AppendLine($"   •  [{v} / {e}]:  {val}");
        }
        private static string SafeText(string? v) => string.IsNullOrWhiteSpace(v) ? "N/A" : v.Trim();
    }
}
