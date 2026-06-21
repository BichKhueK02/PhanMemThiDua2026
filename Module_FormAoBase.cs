using Krypton.Toolkit;

namespace PhanMemThiDua2026
{
    // =========================================================================
    // LỚP 1: KẾT THỪA GIAO DIỆN (CHỐNG GIẬT TẦNG HỆ ĐIỀU HÀNH)
    // Dùng 'sealed' để JIT Compiler tối ưu hóa tốc độ gọi hàm
    // =========================================================================
    public sealed class FormAoBase : KryptonForm
    {
        public FormAoBase()
        {
            // Chống giật UI bằng DoubleBuffer tầng sâu
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            UpdateStyles();
            DoubleBuffered = true;

            // Tắt autoscale để tránh redraw dư thừa
            AutoScaleMode = AutoScaleMode.None;
            StartPosition = FormStartPosition.CenterParent;
        }

        // TRONG CLASS FORMAOBASE
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // BỎ DÒNG NÀY ĐI - Đây là thủ phạm gây trắng form
                // cp.ExStyle |= 0x02000000; 
                return cp;
            }
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // FormAoBase
            // 
            ClientSize = new Size(284, 261);
            Name = "FormAoBase";
            ResumeLayout(false);

        }
    }

    // =========================================================================
    // LỚP 2: CÔNG CỤ QUẢN LÝ ĐÓNG/MỞ FORM TOÀN HỆ THỐNG
    // Bộ xương sống quản lý vòng đời UI, tiêu diệt Memory Leak và GDI Leak
    // =========================================================================
    public static class FormManager
    {
        /// <summary>
        /// Mở Form dạng hộp thoại chặn (Modal). Bắt buộc phải tắt form này mới thao tác được form dưới.
        /// Sử dụng 'using' để thu hồi sạch RAM và Handle ngay lập tức khi đóng.
        /// </summary>
        public static void OpenModal<T>(Form parent) where T : Form, new()
        {
            if (parent == null || parent.IsDisposed) return;

            try
            {
                using (T child = new T())
                {
                    child.StartPosition = FormStartPosition.CenterParent;
                    child.ShowInTaskbar = false;
                    child.FormBorderStyle = FormBorderStyle.FixedDialog;
                    child.MinimizeBox = false;
                    child.MaximizeBox = false;

                    child.ShowDialog(parent);
                } // Thoát khỏi đây là GC dọn sạch rác
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể mở cửa sổ.\nChi tiết: " + ex.Message,
                    "Lỗi hệ thống",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Mở Form dạng Modal và CÓ TRẢ VỀ KẾT QUẢ. 
        /// Chuyên dùng cho form bảo mật (Quên mật khẩu, Xác thực Admin).
        /// </summary>
        public static DialogResult OpenModalWithResult<T>(Form parent) where T : Form, new()
        {
            if (parent == null || parent.IsDisposed) return DialogResult.None;

            try
            {
                using (T child = new T())
                {
                    child.StartPosition = FormStartPosition.CenterParent;
                    child.ShowInTaskbar = false;
                    child.FormBorderStyle = FormBorderStyle.FixedDialog;
                    child.MinimizeBox = false;
                    child.MaximizeBox = false;

                    return child.ShowDialog(parent);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể mở cửa sổ.\nChi tiết: " + ex.Message,
                    "Lỗi hệ thống",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return DialogResult.Abort;
            }
        }

        /// <summary>
        /// Mở Form dạng tự do (Modeless) CHỐNG TRÙNG LẶP.
        /// Nếu RAM đã có Form -> Kéo lên trên cùng. Chưa có -> Mở mới.
        /// Dành cho Dashboard lớn, Bảng dữ liệu dài cần theo dõi liên tục.
        /// </summary>
        public static void OpenOrBringToFront<T>(Form parent = null) where T : Form, new()
        {
            // 1. Quét RAM xem Form đã mở chưa
            foreach (Form frm in Application.OpenForms)
            {
                if (frm is T)
                {
                    // Nếu đang thu nhỏ thì bung lên
                    if (frm.WindowState == FormWindowState.Minimized)
                    {
                        frm.WindowState = FormWindowState.Normal;
                    }

                    // Kéo lên trên cùng và nhấp nháy focus
                    frm.BringToFront();
                    frm.Activate();
                    frm.Focus();

                    return; // Đã tìm thấy thì thoát hàm ngay, KHÔNG tạo mới.
                }
            }

            // 2. Nếu quét hết RAM không thấy -> Khởi tạo mới 1 lần duy nhất
            try
            {
                T newForm = new T();
                newForm.StartPosition = FormStartPosition.CenterScreen;

                if (parent != null)
                {
                    newForm.Show(parent);
                }
                else
                {
                    newForm.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể khởi tạo giao diện.\nChi tiết: " + ex.Message,
                    "Lỗi hệ thống",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }

}