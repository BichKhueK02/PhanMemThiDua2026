///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026

    public partial class FormWelcome : Form
    {
        public FormWelcome()
        {
            this.SuspendLayout();
            InitializeComponent();
            // ===== 1. CẤU HÌNH UI =====
            this.FormBorderStyle = FormBorderStyle.None; // ẩn tiêu đề + viền ngoài
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true;
            // ===== 2. NẠP VĂN BẢN =====
            label1_TenPhanMem.Text = "PhanMemThiDua2026";
            label2_TinCayAnToan.Text = "Tin cậy  •  An toàn  •  Bảo mật  •  Tối ưu cho máy tính cấu hình thấp";
            richTextBox1.Text =
                "CHỨC NĂNG CHÍNH\n" +
                "•  Quản lý dữ liệu thi đua, khen thưởng tập trung, thống nhất\n" +
                "•  Quản lý, tra cứu thông tin và danh sách cán bộ, chiến sĩ\n" +
                "•  Tự động thống kê, tổng hợp và phân loại kết quả thi đua tuần, tháng, năm\n" +
                "•  Xuất danh sách và báo cáo Excel nhanh chóng\n" +
                "•  Hỗ trợ sao lưu, khôi phục và kiểm tra cơ sở dữ liệu\n" +
                "•  Mã hóa dữ liệu, hỗ trợ bảo đảm tính toàn vẹn\n" +
                "•  Giao diện trực quan, dễ sử dụng, tối ưu tài nguyên máy tính\n\n" +
                "THÔNG TIN PHÁT TRIỂN\n" +
                "- Bắt đầu từ một ý tưởng tình cờ, cùng niềm đam mê lập trình cá nhân, phần mềm được xây dựng với mong muốn giúp anh em tiết kiệm thời gian, công sức trong công tác thống kê, tổng hợp và lập báo cáo thi đua, khen thưởng trong môi trường Cảnh sát cơ động.\n" +
                "- Phần mềm phục vụ môi trường nội bộ, không yêu cầu kết nối Internet hoặc quyền Administrator trong điều kiện vận hành thông thường. Bản đóng gói được ký số nội bộ với thời hạn chứng thư 12 tháng.\n" +
                "- Phần mềm tiếp tục được bảo trì, khắc phục lỗi và tối ưu hiệu năng. Mọi ý kiến đóng góp của người dùng đều là động lực để hoàn thiện các phiên bản tiếp theo.\n\n" +
                "  Trân trọng cảm ơn sự tin tưởng và đồng hành của Quý người dùng!\n" +
                "  — Nhóm phát triển —";
            richTextBox1.ReadOnly = true;
            richTextBox1.BorderStyle = BorderStyle.None;

            // Tô màu + in đậm các tiêu đề cho dễ nhìn
            Font headFont = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            foreach (var title in new[] { "CHỨC NĂNG CHÍNH", "THÔNG TIN PHÁT TRIỂN", "  — Nhóm phát triển —" })
            {
                int idx = richTextBox1.Text.IndexOf(title, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    richTextBox1.Select(idx, title.Length);
                    richTextBox1.SelectionColor = Color.FromArgb(96, 165, 250);
                    richTextBox1.SelectionFont = headFont;
                }
            }
            richTextBox1.SelectionLength = 0;
            richTextBox1.SelectionStart = 0;
            // ===== 3. PHÍM =====
            this.KeyPreview = true;
            this.AcceptButton = btn_BatDau;
            // ===== 4. SỰ KIỆN =====
            this.Load += FormWelcome_Load;
            this.FormClosing += FormWelcome_FormClosing;
            btn_BatDau.Click += btn_BatDau_Click;
            // Bo tròn góc form + kéo form bằng chuột (vì không còn thanh tiêu đề)
            this.Resize += (s, e) => ApplyRoundedRegion();
            foreach (Control c in new Control[] { this, panelCard, panelHeader, pictureBox1, label1_TenPhanMem, label2_TinCayAnToan })
            {
                c.MouseDown += DragForm_MouseDown;
            }
            ApplyRoundedRegion();
            this.ResumeLayout(false);
        }

        // ===== Bo góc form =====
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

        // ===== Kéo form =====
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

        private void FormWelcome_Load(object? sender, EventArgs e)
        {
            // Focus vào nút Bắt đầu, tránh con trỏ nhấp nháy trong RichTextBox
            if (btn_BatDau != null && btn_BatDau.Enabled)
            {
                btn_BatDau.Focus();
            }
        }

        private void btn_BatDau_Click(object? sender, EventArgs e)
        {
            try
            {
                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FormWelcome] Lỗi: {ex.Message}");
                this.DialogResult = DialogResult.Cancel;
            }
            finally
            {
                this.Close();
            }
        }

        // Chặn Alt + F4: đổi thành bấm nút Bắt đầu để luồng cập nhật không bị gãy
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Alt | Keys.F4))
            {
                btn_BatDau.PerformClick();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ===== 5. GIẢI PHÓNG KHI ĐÓNG FORM =====
        private void FormWelcome_FormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                this.Load -= FormWelcome_Load;
                this.FormClosing -= FormWelcome_FormClosing;
                if (btn_BatDau != null)
                {
                    btn_BatDau.Click -= btn_BatDau_Click;
                }
            }
            catch
            {
                // Silent
            }
        }
    }
}