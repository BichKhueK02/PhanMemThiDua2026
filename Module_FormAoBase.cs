using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
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
        // Các modal đang mở theo cặp (form cha, loại form con): chặn bấm đúp mở 2 hộp thoại trùng nhau.
        // Chỉ truy cập trên luồng UI, và luôn được gỡ trong finally nên không giữ tham chiếu Form lâu dài.
        private static readonly HashSet<(Form Cha, Type Loai)> _dangMoModal = new();
        private const string TieuDeLoi = "Lỗi hệ thống";
        /// <summary>
        /// Mở Form dạng hộp thoại chặn (Modal). Bắt buộc phải tắt form này mới thao tác được form dưới.
        /// Sử dụng 'using' để thu hồi sạch RAM và Handle ngay lập tức khi đóng.
        /// </summary>
        /// <param name="truocKhiHien">Tùy chọn: truyền dữ liệu vào form con trước khi hiện.</param>
        /// <param name="sauKhiDong">Tùy chọn: đọc dữ liệu từ form con sau khi đóng, TRƯỚC khi bị Dispose.</param>
        public static void OpenModal<T>(Form parent, Action<T>? truocKhiHien = null, Action<T>? sauKhiDong = null)
            where T : Form, new()
        {
            if (parent == null || parent.IsDisposed) return;
            if (parent.InvokeRequired)   // gọi từ luồng nền vẫn an toàn
            {
                parent.Invoke(new Action(() => OpenModal(parent, truocKhiHien, sauKhiDong)));
                return;
            }
            try
            {
                HienModal(parent, truocKhiHien, sauKhiDong);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể mở cửa sổ.\nChi tiết: " + ex.Message,
                    TieuDeLoi,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// Mở Form dạng Modal và CÓ TRẢ VỀ KẾT QUẢ.
        /// Chuyên dùng cho form bảo mật (Quên mật khẩu, Xác thực Admin).
        /// Trả về DialogResult.None nếu form cha không hợp lệ hoặc hộp thoại này đang mở sẵn.
        /// </summary>
        public static DialogResult OpenModalWithResult<T>(Form parent, Action<T>? truocKhiHien = null, Action<T>? sauKhiDong = null)
            where T : Form, new()
        {
            if (parent == null || parent.IsDisposed) return DialogResult.None;
            if (parent.InvokeRequired)
            {
                return (DialogResult)parent.Invoke(
                    new Func<DialogResult>(() => OpenModalWithResult(parent, truocKhiHien, sauKhiDong)));
            }
            try
            {
                return HienModal(parent, truocKhiHien, sauKhiDong);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể mở cửa sổ.\nChi tiết: " + ex.Message,
                    TieuDeLoi,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return DialogResult.Abort;
            }
        }
        // Lõi dùng chung của 2 hàm modal (trước đây lặp code 2 chỗ)
        private static DialogResult HienModal<T>(Form parent, Action<T>? truocKhiHien, Action<T>? sauKhiDong)
            where T : Form, new()
        {
            var khoa = (parent, typeof(T));
            if (!_dangMoModal.Add(khoa)) return DialogResult.None;   // đang mở rồi (vd. bấm đúp)
            try
            {
                using T child = new T();   // thoát khỏi hàm là Dispose -> thu hồi RAM + Handle ngay
                child.StartPosition = FormStartPosition.CenterParent;
                child.ShowInTaskbar = false;
                child.FormBorderStyle = FormBorderStyle.FixedDialog;
                child.MinimizeBox = false;
                child.MaximizeBox = false;
                truocKhiHien?.Invoke(child);
                DialogResult ketQua = child.ShowDialog(parent);
                sauKhiDong?.Invoke(child);   // form con còn sống ở đây nên đọc được dữ liệu
                return ketQua;
            }
            finally
            {
                _dangMoModal.Remove(khoa);
            }
        }
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