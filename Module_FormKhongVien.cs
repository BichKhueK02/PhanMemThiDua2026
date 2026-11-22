///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Form không viền, bo góc, kéo thả bằng chuột. Dùng chung cho Form8_CauHinhCSDL và Form9_GoCaiDat.
    /// Gọi 1 lần trong hàm khởi tạo, ngay sau InitializeComponent():
    ///     Module_FormKhongVien.ApDung(this, this, cardPanel, pnlHeader, picLogo, lblTitle, lblSubtitle);
    /// - Windows 11: nhờ DWM bo góc gốc của hệ điều hành (mép mịn, có viền mảnh), KHÔNG dùng Region.
    /// - Windows 10 trở xuống: dùng Region bo góc (mép hơi răng cưa do GDI không khử răng cưa cho Region).
    /// </summary>
    internal static class Module_FormKhongVien
    {
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;
        private const int BAN_KINH_REGION = 14;              // pixel ở 96 DPI, chỉ dùng cho nhánh Region (Windows 10)
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_BORDER_COLOR = 34;
        private const int DWMWCP_ROUND = 2;
        private const int BUILD_WINDOWS_11 = 22000;
        // COLORREF = 0x00BBGGRR  (viền slate-600: 71,85,105)
        private const int MAU_VIEN_DWM = 71 | (85 << 8) | (105 << 16);
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
        /// <param name="vungKeo">Các điều khiển cho phép nắm kéo cả form (nên gồm chính form và các panel/nhãn/logo phủ lên nó).</param>
        public static void ApDung(Form form, params Control[] vungKeo)
        {
            form.HandleCreated += (_, _) => BoGoc(form);
            form.SizeChanged += (_, _) =>
            {
                // Chỉ nhánh Region mới cần tính lại khi đổi kích thước/DPI
                if (form.IsHandleCreated && form.Region != null) BoGocRegion(form);
            };
            foreach (Control c in vungKeo)
                c.MouseDown += (_, e) => KeoForm(form, e);
        }
        private static void BoGoc(Form form)
        {
            if (!BoGocHeThong(form)) BoGocRegion(form);
        }
        // Windows 11: bo góc gốc + màu viền; trả về false nếu hệ điều hành không hỗ trợ
        private static bool BoGocHeThong(Form form)
        {
            try
            {
                if (Environment.OSVersion.Version.Build < BUILD_WINDOWS_11) return false;
                int kieu = DWMWCP_ROUND;
                if (DwmSetWindowAttribute(form.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref kieu, sizeof(int)) != 0) return false;
                int mau = MAU_VIEN_DWM;
                DwmSetWindowAttribute(form.Handle, DWMWA_BORDER_COLOR, ref mau, sizeof(int));   // lỗi ở bước này không quan trọng
                Region? cu = form.Region;
                if (cu != null) { form.Region = null; cu.Dispose(); }
                return true;
            }
            catch (Exception ex) when (ex is DllNotFoundException || ex is EntryPointNotFoundException)
            {
                return false;
            }
        }
        private static void BoGocRegion(Form form)
        {
            int w = form.Width, h = form.Height;
            if (w <= 0 || h <= 0) return;
            int d = Math.Min((int)Math.Round(BAN_KINH_REGION * 2 * form.DeviceDpi / 96.0), Math.Min(w, h));
            using var path = new GraphicsPath();
            path.AddArc(0, 0, d, d, 180, 90);
            path.AddArc(w - d, 0, d, d, 270, 90);
            path.AddArc(w - d, h - d, d, d, 0, 90);
            path.AddArc(0, h - d, d, d, 90, 90);
            path.CloseFigure();
            Region? cu = form.Region;
            form.Region = new Region(path);
            cu?.Dispose();
        }
        private static void KeoForm(Form form, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || !form.IsHandleCreated) return;
            ReleaseCapture();
            SendMessage(form.Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
        }
    }
}