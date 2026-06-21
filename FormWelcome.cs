using System.Diagnostics;

namespace PhanMemThiDua2026
{
    public partial class FormWelcome : Form
    {
        public FormWelcome()
        {
            // Khóa cơ chế vẽ đồ họa tạm thời để nạp cấu hình (Chống giật/nháy hình tầng sâu)
            this.SuspendLayout();

            InitializeComponent();

            // ===== 1️⃣ CẤU HÌNH UI CHUẨN ĐƠN VỊ (BẢO MẬT - TÀNG HÌNH) =====
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Ép Form bật tính năng DoubleBuffered bảo vệ card màn hình, vẽ mượt hơn
            this.DoubleBuffered = true;

            // ===== 2️⃣ NẠP VĂN BẢN VÀ ĐÓNG KHUNG GRAPHICS =====
            label1_TenPhanMem.Text = "PHẦN MỀM THI ĐUA 2026";
            label2_TinCayAnToan.Text = "TIN CẬY – AN TOÀN – BẢO MẬT – PHÙ HỢP MỌI MÁY TÍNH";

            richTextBox1.Text =
                "CHỨC NĂNG CHÍNH\n" +
                "- Quản lý dữ liệu thi đua tập trung, thống nhất\n" +
                "- Tự động thống kê và phân loại kết quả thi đua\n" +
                "- Xuất danh sách, báo cáo Excel nhanh chóng\n" +
                "- Sao lưu và khôi phục dữ liệu an toàn\n" +
                "- CSDL được mã hóa, ký số, bảo đảm toàn vẹn và khôi phục ổn định\n" +
                "- Giao diện trực quan, dễ sử dụng\n\n" +
                "GHI CHÚ\n" +
                "Phần mềm đang trong quá trình phát triển, rất mong anh em đóng góp ý kiến\n" +
                "để hệ thống ngày càng hoàn thiện hơn.\n\n" +
                "Trân trọng!  — Nhóm phát triển —";

            richTextBox1.ReadOnly = true;
            richTextBox1.BackColor = this.BackColor;
            richTextBox1.BorderStyle = BorderStyle.None;

            // Chống người dùng bôi đen văn bản vô ích (Giữ giao diện luôn sạch)
            richTextBox1.SelectionLength = 0;

            // ===== 3️⃣ ĐIỀU HƯỚNG PHÍM CỨNG HỆ THỐNG =====
            this.KeyPreview = true; // Cho phép Form bắt phím tắt trước Control
            this.AcceptButton = btn_BatDau; // Gõ Enter tự động kích hoạt nút Bắt đầu

            // ===== 4️⃣ GẮN SỰ KIỆN CHẶT CHẼ =====
            this.Load += FormWelcome_Load;
            this.FormClosing += FormWelcome_FormClosing;
            btn_BatDau.Click += btn_BatDau_Click;

            // Nhả lệnh khóa và ép Windows vẽ nguyên khối giao diện lên màn hình RAM
            this.ResumeLayout(false);
        }
        private void FormWelcome_Load(object sender, EventArgs e)
        {
            // Ép Focus vào nút Bắt đầu, triệt tiêu con trỏ nhấp nháy (Caret) trong RichTextBox
            if (btn_BatDau != null && btn_BatDau.Enabled)
            {
                btn_BatDau.Focus();
            }
        }
        private void btn_BatDau_Click(object sender, EventArgs e)
        {
            try
            {
                // Báo hiệu xem thành công cho luồng Main
                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FormWelcome] Lỗi: {ex.Message}");
                this.DialogResult = DialogResult.Cancel; // Hướng đi Fail-Safe an toàn
            }
            finally
            {
                this.Close();
            }
        }
        // Bẫy phím tắt nguy hiểm (Chặn Alt + F4 hoặc nút X góc màn hình làm gãy luồng cập nhật vân tay)
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Alt | Keys.F4))
            {
                // Đổi hướng thành lệnh tắt an toàn, ép trả về DialogResult.OK để cập nhật vân tay máy tính
                btn_BatDau.PerformClick();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        // ===== 5️⃣ GIẢI PHÓNG RAM TUYỆT ĐỐI KHI FORM CHẾT (ANTI-MEMORY LEAK) =====
        private void FormWelcome_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // Hủy đăng ký toàn bộ sự kiện để tránh rác liên kết địa chỉ ô nhớ RAM
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