//namespace PhanMemThiDua2026
//{
//    public static class Module_ThoatAnToan
//    {
//        //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
//        private static bool isExiting = false; // Tránh gọi nhiều lần
//        /// Kích hoạt ESC cho Form
//        public static void KichHoatESC(Form frm)
//        {
//            if (frm == null) return;
//            frm.KeyPreview = true; // Form nhận key trước các control
//            // Chỉ thêm handler nếu chưa được thêm
//            if (!isHandlerRegistered(frm))
//            {
//                frm.KeyDown += FrmKeyDown;
//                MarkHandlerRegistered(frm);
//            }
//        }
//        private static void FrmKeyDown(object sender, KeyEventArgs e)
//        {
//            if (e.KeyCode == Keys.Escape && !isExiting)
//            {
//                isExiting = true;
//                try
//                {
//                    DialogResult result = MessageBox.Show(
//                        "Bạn có muốn thoát phần mềm ngay bây giờ?",
//                        "Thoát khẩn cấp",
//                        MessageBoxButtons.YesNo,
//                        MessageBoxIcon.Warning
//                    );
//                    if (result == DialogResult.Yes)
//                    {
//                        ThoatAnToan();
//                    }
//                }
//                catch
//                {
//                    ThoatAnToan();
//                }
//                finally
//                {
//                    isExiting = false;
//                }
//            }
//        }
//        public static void ThoatAnToan()
//        {
//            try
//            {
//                foreach (Form f in Application.OpenForms)
//                {
//                    if (!f.IsDisposed)
//                        f.Close();
//                }
//            }
//            catch { }
//            finally
//            {
//                Application.Exit();
//            }
//        }
//        private static readonly HashSet<Form> registeredForms = new();
//        private static bool isHandlerRegistered(Form frm) => registeredForms.Contains(frm);
//        private static void MarkHandlerRegistered(Form frm) => registeredForms.Add(frm);
//    }
//}
///Phiên bản 2 – ESC hỏi thoát ngay cả khi phần mềm treo (đường thoát độc lập với luồng giao diện), không rò rỉ Form,
///đóng đủ mọi Form, không bị FormClosing chặn, luôn có chốt chặn cuối cùng (tự kết thúc tiến trình)
///
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
namespace PhanMemThiDua2026
{
    public static class Module_ThoatAnToan
    {
        //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
        // ===================== NGUYÊN TẮC THIẾT KẾ =====================
        // Có 2 đường để bấm ESC mà ra được hộp hỏi thoát:
        //  1) Bình thường (giao diện còn sống): sự kiện KeyDown của Form -> MessageBox có chủ sở hữu (không bị lọt ra sau cửa sổ khác).
        //  2) Phần mềm TREO (luồng giao diện không bơm thông điệp, KeyDown không bao giờ được gọi):
        //     1 luồng giám sát riêng, hầu hết thời gian chỉ "gõ cửa" luồng giao diện mỗi ~0,75 giây; khi phát hiện không trả lời liên tục
        //     mới dò phím ESC (GetAsyncKeyState) và mở hộp hỏi từ CHÍNH luồng giám sát. Hộp thoại gốc của Windows có vòng lặp riêng nên vẫn hiện được.
        // Cả hai đường đều kết thúc bằng ThoatKhanCap(): dọn nhật ký có giới hạn thời gian -> Environment.Exit -> và có "chốt chặn"
        // (luồng riêng) tự kết thúc tiến trình sau vài giây nếu mọi thứ vẫn kẹt (ProcessExit/finalizer treo, FormClosing chặn...).
        private const string TIEU_DE = "Thoát khẩn cấp";
        private const string NOI_DUNG = "Bạn có muốn thoát phần mềm ngay bây giờ?";
        private const string NOI_DUNG_TREO = "Phần mềm đang không phản hồi.\r\n\r\nBạn có muốn thoát phần mềm ngay bây giờ?";
        private const int VK_ESCAPE = 0x1B;
        private const uint WM_NULL = 0x0000;
        private const uint SMTO_ABORTIFHUNG = 0x0002;
        private const uint MB_YESNO = 0x00000004;
        private const uint MB_ICONWARNING = 0x00000030;
        private const uint MB_SETFOREGROUND = 0x00010000;
        private const uint MB_TOPMOST = 0x00040000;
        private const int IDYES = 6;
        private const int SO_LAN_TREO_TOI_THIEU = 3;       // ~ 2-3 giây không phản hồi mới coi là treo (tránh báo nhầm khi giao diện bận ngắn)
        private const int THOI_GIAN_CHO_KHAN_CAP_MS = 3000; // quá hạn này mà chưa thoát được -> tự kết thúc tiến trình
        private const int THOI_GIAN_CHO_THONG_THUONG_MS = 10000;
        private const int THOI_GIAN_XA_LOG_MS = 1800;
        // 0/1 – khóa không cho hiện 2 hộp hỏi cùng lúc (từ cả 2 đường) và không phụ thuộc luồng
        private static int _dangHoi;
        private static int _daKhoiDongGiamSat;
        private static int _uiThreadId;                           // luồng giao diện (gán ở lần KichHoatESC đầu tiên, luôn gọi từ luồng giao diện)
        private static volatile SynchronizationContext? _uiContext;
        // Chỉ lưu handle (số nguyên), KHÔNG giữ tham chiếu Form -> không làm rò rỉ bộ nhớ như HashSet<Form> của bản cũ
        private static readonly ConcurrentDictionary<IntPtr, byte> _handles = new();
        // ===================== P/INVOKE =====================
        [DllImport("user32.dll", EntryPoint = "MessageBoxW", CharSet = CharSet.Unicode)]
        private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        [DllImport("user32.dll", EntryPoint = "GetClassNameW", CharSet = CharSet.Unicode)]
        private static extern int GetClassNameW(IntPtr hWnd, StringBuilder text, int maxCount);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindow(IntPtr hWnd);
        [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW")]
        private static extern IntPtr SendMessageTimeoutW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam,
            uint flags, uint timeoutMs, out IntPtr result);
        // ===================== ĐĂNG KÝ ESC CHO FORM =====================
        /// <summary>Kích hoạt ESC cho Form. Gọi từ luồng giao diện (thường trong constructor hoặc Load). Gọi lặp lại nhiều lần vẫn an toàn.</summary>
        public static void KichHoatESC(Form frm)
        {
            if (frm == null || frm.IsDisposed) return;
            if (_uiThreadId == 0)
            {
                _uiThreadId = Environment.CurrentManagedThreadId;
                _uiContext = SynchronizationContext.Current;
            }
            else
            {
                _uiContext ??= SynchronizationContext.Current;
            }
            frm.KeyPreview = true; // Form nhận phím trước các control con
            // "-=" rồi "+=" cùng 1 delegate tĩnh: không bao giờ bị đăng ký đôi, không cần bảng theo dõi (bản cũ giữ Form trong HashSet mãi mãi)
            frm.KeyDown -= FrmKeyDown;
            frm.KeyDown += FrmKeyDown;
            frm.HandleCreated -= FrmHandleCreated;
            frm.HandleCreated += FrmHandleCreated;
            if (frm.IsHandleCreated) _handles[frm.Handle] = 0;
            KhoiDongGiamSat();
        }
        private static void FrmHandleCreated(object? sender, EventArgs e)
        {
            if (sender is Control c && c.IsHandleCreated) _handles[c.Handle] = 0;
        }
        // ===================== ĐƯỜNG 1: GIAO DIỆN CÒN SỐNG =====================
        private static void FrmKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Escape) return;
            // Đã xử lý ở đây: ESC không "chảy" tiếp xuống control con (vd. tự đóng hộp thoại phía sau khi người dùng chỉ định bấm "Không")
            e.Handled = true;
            e.SuppressKeyPress = true;
            if (Interlocked.CompareExchange(ref _dangHoi, 1, 0) != 0) return;   // đang có 1 hộp hỏi rồi
            bool thoat;
            try
            {
                // Có chủ sở hữu (Form hiện tại): hộp thoại luôn nằm trên cùng, không bị lọt ra sau cửa sổ khác như bản cũ
                thoat = MessageBox.Show(sender as IWin32Window, NOI_DUNG, TIEU_DE,
                            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
            }
            catch
            {
                thoat = true;   // như bản gốc: không hiện được hộp thoại -> vẫn thoát
            }
            finally
            {
                Interlocked.Exchange(ref _dangHoi, 0);
            }
            if (thoat) ThoatKhanCap();
        }
        // ===================== ĐƯỜNG 2: PHẦN MỀM TREO =====================
        private static void KhoiDongGiamSat()
        {
            if (Interlocked.CompareExchange(ref _daKhoiDongGiamSat, 1, 0) != 0) return;
            try
            {
                new Thread(VongGiamSat)
                {
                    IsBackground = true,          // không giữ tiến trình sống khi thoát bình thường
                    Name = "ThoatAnToan.GiamSat",
                    Priority = ThreadPriority.AboveNormal
                }.Start();
            }
            catch
            {
                Interlocked.Exchange(ref _daKhoiDongGiamSat, 0);
            }
        }
        private static void VongGiamSat()
        {
            int demKhongPhanHoi = 0;
            bool escTruoc = false;
            while (true)
            {
                try
                {
                    if (UiConPhanHoi())
                    {
                        demKhongPhanHoi = 0;
                        escTruoc = false;
                        Thread.Sleep(750);   // lúc bình thường: gần như không tốn CPU, KHÔNG đọc trạng thái bàn phím
                        continue;
                    }
                    demKhongPhanHoi++;
                    // Trong lúc treo: dò ESC mỗi 40ms (~0,5 giây giữa 2 lần gõ cửa lại luồng giao diện)
                    for (int i = 0; i < 12; i++)
                    {
                        bool esc = GetAsyncKeyState(VK_ESCAPE) < 0;   // bit cao = đang giữ phím
                        if (esc && !escTruoc && demKhongPhanHoi >= SO_LAN_TREO_TOI_THIEU && CuaSoThuocPhanMem())
                            HoiThoatKhiTreo();
                        escTruoc = esc;
                        Thread.Sleep(40);
                    }
                }
                catch
                {
                    // Luồng giám sát không bao giờ được chết vì lỗi lặt vặt
                    try { Thread.Sleep(1000); } catch { }
                }
            }
        }
        /// <summary>Gửi WM_NULL tới 1 cửa sổ của phần mềm; luồng giao diện đang bơm thông điệp sẽ trả lời ngay. Không có cửa sổ nào -> coi là bình thường.</summary>
        private static bool UiConPhanHoi()
        {
            foreach (IntPtr h in _handles.Keys)
            {
                if (!IsWindow(h)) { _handles.TryRemove(h, out _); continue; }
                // SMTO_ABORTIFHUNG: nếu Windows đã đánh dấu "Not Responding" thì trả về ngay, không chờ hết timeout
                return SendMessageTimeoutW(h, WM_NULL, IntPtr.Zero, IntPtr.Zero, SMTO_ABORTIFHUNG, 250, out _) != IntPtr.Zero;
            }
            return true;
        }
        /// <summary>Chỉ phản ứng với ESC khi cửa sổ đang ở trên cùng là của phần mềm này (hoặc cửa sổ "Ghost" Windows dựng thay cho cửa sổ bị treo),
        /// để ESC bấm ở phần mềm khác không bật hộp hỏi.</summary>
        private static bool CuaSoThuocPhanMem()
        {
            IntPtr fg = GetForegroundWindow();
            if (fg == IntPtr.Zero) return false;
            GetWindowThreadProcessId(fg, out uint pid);
            if (pid == (uint)Environment.ProcessId) return true;
            var sb = new StringBuilder(16);
            GetClassNameW(fg, sb, sb.Capacity);
            return string.Equals(sb.ToString(), "Ghost", StringComparison.Ordinal);
        }
        private static void HoiThoatKhiTreo()
        {
            if (Interlocked.CompareExchange(ref _dangHoi, 1, 0) != 0) return;
            try
            {
                // MessageBox gốc của Windows từ luồng giám sát: có vòng lặp riêng nên hiện được dù luồng giao diện đã đứng.
                // TOPMOST + SETFOREGROUND để nổi lên trên cửa sổ đang treo.
                int kq = MessageBoxW(IntPtr.Zero, NOI_DUNG_TREO, TIEU_DE,
                                     MB_YESNO | MB_ICONWARNING | MB_TOPMOST | MB_SETFOREGROUND);
                if (kq == IDYES)
                    ThoatCore(khanCap: true, dongForm: false);   // giao diện đang treo: không cố đóng Form (sẽ kẹt), thoát thẳng
            }
            finally
            {
                Interlocked.Exchange(ref _dangHoi, 0);
            }
        }
        // ===================== THOÁT =====================
        /// <summary>Thoát nhẹ nhàng: đóng mọi Form (Form được quyền chặn bằng FormClosing) rồi thoát. Có chốt chặn 10 giây để không bao giờ kẹt vĩnh viễn.</summary>
        public static void ThoatAnToan() => ThoatCore(khanCap: false, dongForm: true);
        /// <summary>Thoát khẩn cấp: cố đóng Form nhưng KHÔNG để Form nào chặn được; tối đa ~3 giây là tiến trình kết thúc.</summary>
        public static void ThoatKhanCap() => ThoatCore(khanCap: true, dongForm: true);
        private static void ThoatCore(bool khanCap, bool dongForm)
        {
            // Chốt chặn đặt TRƯỚC mọi thứ khác: dù bước nào bên dưới kẹt thì tiến trình vẫn tự kết thúc
            DatDongHoKetThuc(khanCap ? THOI_GIAN_CHO_KHAN_CAP_MS : THOI_GIAN_CHO_THONG_THUONG_MS);
            if (dongForm)
            {
                bool trenLuongGiaoDien = _uiThreadId == 0 || Environment.CurrentManagedThreadId == _uiThreadId;
                if (trenLuongGiaoDien)
                {
                    DongTatCaForm();
                }
                else if (!khanCap && _uiContext is { } ctx)
                {
                    // Gọi từ luồng khác: đóng Form phải làm trên luồng giao diện (không chờ, vì có thể giao diện đang bận)
                    ctx.Post(static _ =>
                    {
                        DongTatCaForm();
                        XaLogToiDa();
                        Application.Exit();
                    }, null);
                    return;
                }
            }
            XaLogToiDa();
            if (khanCap) Environment.Exit(0);
            else Application.Exit();
        }
        private static void DongTatCaForm()
        {
            // Bản cũ lặp thẳng trên Application.OpenForms: Close() làm bộ sưu tập thay đổi -> InvalidOperationException,
            // bị "catch {}" nuốt mất nên chỉ Form đầu tiên được đóng. Sao chép ra mảng trước, đóng từ Form mở sau cùng trở về trước.
            Form[] ds;
            try
            {
                ds = new Form[Application.OpenForms.Count];
                for (int i = 0; i < ds.Length; i++) ds[i] = Application.OpenForms[i]!;
            }
            catch
            {
                return;
            }
            for (int i = ds.Length - 1; i >= 0; i--)
            {
                try
                {
                    Form f = ds[i];
                    if (f != null && !f.IsDisposed) f.Close();
                }
                catch { /* 1 Form lỗi khi đóng không được cản các Form còn lại */ }
            }
        }
        /// <summary>Ghi nốt nhật ký còn trong hàng đợi, có giới hạn thời gian (đĩa chậm/treo cũng không giữ được việc thoát).</summary>
        private static void XaLogToiDa()
        {
            try
            {
                Task.Run(Module_BaoVeDuLieu.FlushFinalLogs).Wait(THOI_GIAN_XA_LOG_MS);
            }
            catch { }
        }
        private static void DatDongHoKetThuc(int mili)
        {
            try
            {
                new Thread(() =>
                {
                    try
                    {
                        Thread.Sleep(mili);
                        using var p = Process.GetCurrentProcess();
                        p.Kill();   // TerminateProcess: kết thúc ngay kể cả khi ProcessExit/finalizer/FormClosing đang kẹt
                    }
                    catch { }
                })
                { IsBackground = true, Name = "ThoatAnToan.ChotChan" }.Start();
            }
            catch { }
        }
    }
}