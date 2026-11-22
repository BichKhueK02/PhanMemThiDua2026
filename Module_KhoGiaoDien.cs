using System.Diagnostics;
using System.Runtime.InteropServices;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// "KHO GIAO DIỆN TRONG RAM": các trang nặng (Trang chủ, Phân loại, Thống kê, Khen thưởng, Nhật ký, Cài đặt) được dựng,
    /// chạy Load, NẠP DỮ LIỆU và VẼ THẬT một lần ngay từ lúc người dùng còn ở màn hình đăng nhập, trong một cửa sổ chứa ẩn.
    /// Khi đăng nhập / bấm menu, Form2 chỉ việc "nhấc" trang từ kho sang (đổi cha của cửa sổ, KHÔNG dựng lại, KHÔNG nạp lại).
    ///
    /// Cửa sổ kho:
    ///  - bình thường ẨN hẳn: form con vẫn chạy Load/dàn trang đúng kích thước, nhưng mọi lệnh Focus() đều vô hiệu
    ///    (CanFocus = false) => không bao giờ giật mất con trỏ khỏi ô Mật khẩu;
    ///  - chỉ HIỆN trong tích tắc (đồng bộ, không bơm thông điệp) để vẽ thật một lần: bị "cloak" (DWM giữ ảnh, không đưa lên
    ///    màn hình), nằm ngoài màn hình, kiểu cửa sổ công cụ không kích hoạt, không có nút trên thanh tác vụ.
    /// Lần vẽ thật đầu tiên là lần chậm nhất (Krypton tạo bút vẽ/phông/ảnh đệm, lưới đo cột...) -> làm sẵn ở đây,
    /// lúc hiện cho người dùng chỉ còn là vẽ lại.
    /// </summary>
    internal static class Module_KhoGiaoDien
    {
        private static FormKho? _kho;
        public static FormKho Lay()
        {
            var k = _kho;
            if (k == null || k.IsDisposed)
            {
                k = _kho = new FormKho();
                _ = k.Handle;   // tạo handle (kèm cloak) ngay, cửa sổ vẫn ẩn
                // Trang vào kho được bo góc / tô viền y như khi nằm trong Form2 (Form2 áp cho control thêm vào nó; nay trang
                // vào kho trước nên kho phải áp). Mỗi control chỉ xử lý 1 lần, đưa sang Form2 không xử lý lại.
                Module_GiaoDienBoTron.ApDung(k);
            }
            return k;
        }
        /// <summary>Control có đang nằm trong kho (chưa được đưa ra Form2) không.</summary>
        public static bool LaTrongKho(Control? c)
        {
            var k = _kho;
            if (c == null || k == null || k.IsDisposed) return false;
            for (Control? p = c.Parent; p != null; p = p.Parent)
                if (ReferenceEquals(p, k)) return true;
            return false;
        }
        /// <summary>Đưa form con vào kho (chưa Show). Form đã ở chỗ khác (Form2) thì không động tới.</summary>
        public static void DuaVaoKho(Form f)
        {
            var k = Lay();
            if (f.TopLevel) f.TopLevel = false;
            if (f.FormBorderStyle != FormBorderStyle.None) f.FormBorderStyle = FormBorderStyle.None;
            if (f.Dock != DockStyle.Fill) f.Dock = DockStyle.Fill;
            if (f.Parent == null) k.Controls.Add(f);
        }
        private static readonly System.Reflection.MethodInfo? _createControlBool =
            typeof(Control).GetMethod("CreateControl",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, new[] { typeof(bool) }, null);
        /// <summary>
        /// Show() một form con khi cửa sổ chứa đang ẩn thì WinForms KHÔNG chạy CreateControl -> sự kiện Load không chạy
        /// (Load chỉ chạy khi form "thật sự" hiện). Gọi CreateControl(fIgnoreVisible: true) - đúng đường WinForms dùng nội bộ:
        /// Load chạy đúng 1 lần như bình thường (WinForms tự chặn chạy lại khi form được hiện thật).
        /// </summary>
        public static void ShowTrongKho(Form f)
        {
            f.Show();
            if (f.IsDisposed || !LaTrongKho(f)) return;
            try { _createControlBool?.Invoke(f, new object[] { true }); }
            catch (Exception ex) { Debug.WriteLine("[Kho] CreateControl: " + (ex.InnerException ?? ex).Message); }
        }
        /// <summary>Kích thước vùng chứa trang con (= PanelContainer của Form2) để mọi trang được dàn đúng cỡ thật.</summary>
        public static void DatKichThuoc(Size s)
        {
            if (s.Width < 300 || s.Height < 200) return;
            var k = Lay();
            if (k.ClientSize != s) k.ClientSize = s;
        }
        /// <summary>
        /// Vẽ thật 1 lần form con đang nằm trong kho (đồng bộ). Kho chỉ hiện (cloak + ngoài màn hình + không kích hoạt)
        /// trong đúng thời gian vẽ rồi ẩn lại ngay, không có thông điệp nào khác được xử lý xen giữa.
        /// </summary>
        public static void VeNong(Control f)
        {
            var k = _kho;
            if (k == null || k.IsDisposed || !k.IsHandleCreated || !LaTrongKho(f)) return;
            var sw = Stopwatch.StartNew();
            try
            {
                f.BringToFront();
                k.HienTam(true);
                RedrawWindow(k.Handle, IntPtr.Zero, IntPtr.Zero,
                    RDW_INVALIDATE | RDW_ERASE | RDW_FRAME | RDW_ALLCHILDREN | RDW_UPDATENOW);
            }
            catch (Exception ex) { Debug.WriteLine("[Kho] VeNong: " + ex.Message); }
            finally
            {
                try { k.HienTam(false); } catch { }
            }
            Debug.WriteLine($"[Kho] vẽ nóng {f.GetType().Name}: {sw.ElapsedMilliseconds} ms");
        }
        public static void Huy()
        {
            var k = Interlocked.Exchange(ref _kho, null);
            if (k == null || k.IsDisposed) return;
            try { k.Dispose(); } catch { }
        }
        // ================= VẼ MƯỢT KHI CHUYỂN TRANG =================
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr v);
        private const int WM_SETREDRAW = 0x000B, GWL_EXSTYLE = -20;
        private const long WS_EX_COMPOSITED = 0x02000000L;
        private const uint RDW_INVALIDATE = 0x1, RDW_ERASE = 0x4, RDW_FRAME = 0x400, RDW_ALLCHILDREN = 0x80, RDW_UPDATENOW = 0x100;
        /// <summary>Đặt false nếu thấy lỗi vẽ khi chuyển trang: quay về vẽ lại đồng bộ thường (vẫn nhanh hơn bản cũ).</summary>
        // Đã đo A/B (hieunang-20261122.log, 09:00 và 09:02): vẽ thường KHÔNG nhanh hơn vẽ tổng hợp (cùng ~100–150 ms/trang
        // 1748×940) => giữ vẽ tổng hợp (không xé hình). Chi phí nằm ở bản thân việc vẽ các control Krypton/lưới.
        private const bool DungVeTongHop = true;
        /// <summary>
        /// Bật lại vẽ và vẽ NGAY TOÀN BỘ vùng chứa trong 1 lượt (thay cho Invalidate: vẽ dần từng control theo vòng lặp
        /// thông điệp, xen kẽ với việc nạp dữ liệu -> người dùng thấy trang "vẽ từng mảng").
        /// Trong lượt vẽ này tạm bật WS_EX_COMPOSITED: Windows vẽ cả cây control vào 1 bộ đệm rồi mới đưa lên màn hình
        /// một lần => hết cảnh vẽ dần. Gỡ cờ ngay sau đó (bật thường trực sẽ làm Krypton vẽ lại liên tục).
        /// </summary>
        public static void VeLaiNgay(Control c)
        {
            if (!c.IsHandleCreated || c.IsDisposed) return;
            IntPtr h = c.Handle;
            SendMessage(h, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
            long ex = 0;
            bool daThem = false;
            try
            {
                if (DungVeTongHop)
                {
                    ex = GetWindowLongPtr(h, GWL_EXSTYLE).ToInt64();
                    if ((ex & WS_EX_COMPOSITED) == 0)
                    {
                        SetWindowLongPtr(h, GWL_EXSTYLE, (IntPtr)(ex | WS_EX_COMPOSITED));
                        daThem = true;
                    }
                }
                RedrawWindow(h, IntPtr.Zero, IntPtr.Zero, RDW_INVALIDATE | RDW_ERASE | RDW_FRAME | RDW_ALLCHILDREN | RDW_UPDATENOW);
            }
            catch (Exception e) { Debug.WriteLine("[VeLaiNgay] " + e.Message); c.Invalidate(true); }
            finally
            {
                if (daThem)
                {
                    try { SetWindowLongPtr(h, GWL_EXSTYLE, (IntPtr)ex); } catch { }
                }
            }
        }
    }
    /// <summary>Cửa sổ chứa ẩn của kho giao diện. Không bao giờ được kích hoạt, không lên thanh tác vụ, không hiện lên màn hình.</summary>
    internal sealed class FormKho : Form
    {
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] private static extern IntPtr SetActiveWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hWnd);
        private const int DWMWA_CLOAK = 13, SW_HIDE = 0, SW_SHOWNOACTIVATE = 4;
        private const int WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x08000000;
        private const int WM_ACTIVATE = 0x0006, WM_MOUSEACTIVATE = 0x0021, MA_NOACTIVATE = 3;
        public FormKho()
        {
            Text = "Kho giao diện";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(-20000, -20000);   // dù Windows không hỗ trợ cloak cũng không bao giờ lọt vào màn hình
            ClientSize = new Size(1264, 640);
            DoubleBuffered = true;
        }
        protected override bool ShowWithoutActivation => true;
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                return cp;
            }
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try { int v = 1; DwmSetWindowAttribute(Handle, DWMWA_CLOAK, ref v, sizeof(int)); } catch { }
        }
        /// <summary>Hiện/ẩn ở mức Windows, không kích hoạt.</summary>
        internal void HienTam(bool hien)
        {
            if (!IsHandleCreated) return;
            ShowWindow(Handle, hien ? SW_SHOWNOACTIVATE : SW_HIDE);
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEACTIVATE) { m.Result = (IntPtr)MA_NOACTIVATE; return; }
            if (m.Msg == WM_ACTIVATE && (m.WParam.ToInt64() & 0xFFFF) != 0)
            {
                // Chốt chặn: nếu vì lý do nào đó kho bị kích hoạt, trả ngay quyền kích hoạt cho cửa sổ trước đó (form đăng nhập...)
                IntPtr truoc = m.LParam;
                base.WndProc(ref m);
                if (truoc != IntPtr.Zero && IsWindow(truoc)) SetActiveWindow(truoc);
                Debug.WriteLine("[Kho] bị kích hoạt -> trả lại cửa sổ trước");
                return;
            }
            base.WndProc(ref m);
        }
    }
}
