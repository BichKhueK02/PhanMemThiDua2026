using Krypton.Toolkit;
namespace PhanMemThiDua2026
{
    // LỚP 1: KẾT THỪA GIAO DIỆN (CHỐNG GIẬT TẦNG HỆ ĐIỀU HÀNH)
    // Dùng 'sealed' để JIT Compiler tối ưu hóa tốc độ gọi hàm
    public sealed class FormAoBase : KryptonForm
    {
        //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
        // Cờ cửa sổ WS_EX_COMPOSITED: Windows vẽ form + TẤT CẢ control con vào 1 bộ đệm rồi mới đưa lên màn hình.
        // Khi rê chuột, các control Krypton (nút, panel, label nền trong suốt...) vẽ lại từng phần lệch nhau
        // -> thấy giật/nháy. Bộ đệm chung này làm các lần vẽ đó hiện ra cùng lúc, mượt hơn rõ rệt.
        private const int WS_EX_COMPOSITED = 0x02000000;
        // Mặc định BẬT, nhưng tự tắt khi chạy qua Remote Desktop (cờ này làm RDP chậm).
        private bool _chongGiat = !SystemInformation.TerminalServerSession;
        /// <summary>
        /// Bật/tắt chế độ vẽ bộ đệm chung (WS_EX_COMPOSITED) cho form này.
        /// Nên đặt NGAY TRONG CONSTRUCTOR của form con (trước khi form hiện).
        /// Tắt (false) cho form chứa WebView2 / trình xem PDF / video / control vẽ bằng DirectX,
        /// vì các control kiểu đó tự vẽ bằng GPU và có thể bị trắng hoặc nháy khi chồng bộ đệm chung.
        /// </summary>
        public bool ChongGiatBoDemChung
        {
            get => _chongGiat;
            set
            {
                if (_chongGiat == value) return;
                _chongGiat = value;
                if (IsHandleCreated) RecreateHandle();   // áp dụng ngay nếu form đã hiện (hiếm khi cần)
            }
        }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                if (_chongGiat && !DesignMode) cp.ExStyle |= WS_EX_COMPOSITED;   // không bật trong Designer cho đỡ lỗi hiển thị
                return cp;
            }
        }
        public FormAoBase()
        {
            // Chống giật UI bằng DoubleBuffer tầng sâu.
            // (Thuộc tính DoubleBuffered chỉ là OptimizedDoubleBuffer + AllPaintingInWmPaint nên đã nằm sẵn trong đây;
            //  UpdateStyles() chưa có handle thì không làm gì -> bỏ cho gọn.)
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            // Tắt autoscale để tránh redraw dư thừa
            AutoScaleMode = AutoScaleMode.None;
            StartPosition = FormStartPosition.CenterParent;
        }
        // Đã bỏ: InitializeComponent/FormAoBase_Load (không nơi nào gọi, Load rỗng). Không ảnh hưởng hành vi.
    }
    // LỚP 2: CÔNG CỤ QUẢN LÝ ĐÓNG/MỞ FORM TOÀN HỆ THỐNG
    // Bộ xương sống quản lý vòng đời UI, tiêu diệt Memory Leak và GDI Leak
    public static class FormManager
    {
        private const string TieuDeLoi = "Lỗi hệ thống";
        /// <summary>
        /// Mở Form dạng tự do (Modeless) CHỐNG TRÙNG LẶP.
        /// Nếu RAM đã có Form -> Kéo lên trên cùng. Chưa có -> Mở mới.
        /// Dành cho Dashboard lớn, Bảng dữ liệu dài cần theo dõi liên tục.
        /// </summary>
        public static void OpenOrBringToFront<T>(Form? parent = null) where T : Form, new()
        {
            if (parent != null && !parent.IsDisposed && parent.InvokeRequired)
            {
                parent.Invoke(new Action(() => OpenOrBringToFront<T>(parent)));
                return;
            }
            // 1. Quét RAM xem Form đã mở chưa (chụp snapshot để không lỗi nếu danh sách đổi giữa chừng)
            foreach (Form frm in Application.OpenForms.Cast<Form>().ToArray())
            {
                if (frm is T && !frm.IsDisposed)
                {
                    try
                    {
                        if (!frm.Visible) frm.Show();   // form đang bị Hide() thì hiện lại
                        if (frm.WindowState == FormWindowState.Minimized)
                        {
                            frm.WindowState = FormWindowState.Normal;
                        }
                        frm.BringToFront();
                        frm.Activate();   // Activate đã bao gồm focus, bỏ Focus() thừa
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("[FormManager.BringToFront] " + ex.Message);
                    }
                    return; // Đã tìm thấy thì thoát hàm ngay, KHÔNG tạo mới.
                }
            }
            // 2. Quét hết RAM không thấy -> Khởi tạo mới 1 lần duy nhất
            T? newForm = null;
            try
            {
                newForm = new T();
                newForm.StartPosition = FormStartPosition.CenterScreen;
                if (parent != null && !parent.IsDisposed)
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
                newForm?.Dispose();   // tạo dở dang thì giải phóng luôn, không để rò handle
                MessageBox.Show(
                    "Không thể khởi tạo giao diện.\nChi tiết: " + ex.Message,
                    TieuDeLoi,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}