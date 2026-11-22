using System.Diagnostics;
using System.Runtime.InteropServices;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Điều phối công việc "nạp trước" trên luồng giao diện sao cho KHÔNG tranh chấp với người dùng.
    ///
    /// Vấn đề gốc: nạp trước Form2 + 5 trang con bắt buộc chạy trên luồng UI (WinForms không cho dựng Form ở
    /// luồng khác). Nếu chỉ nghỉ 15 ms giữa các bước thì máy gần như bị "bận liên tục" trong vài giây, và
    /// lúc người dùng gõ mật khẩu / bấm Đăng nhập thì chuột-bàn phím phải chờ hết khối đang dựng -> khựng.
    ///
    /// Cách làm: trước MỖI bước nặng, chờ cho đến khi người dùng đã ngừng thao tác một lúc (mặc định 180 ms)
    /// và dọn sạch hàng đợi thông điệp; công việc dài (tạo handle hàng trăm control) được chia lát ~8 ms,
    /// giữa các lát trả lại vòng lặp thông điệp để phím/chuột được xử lý ngay.
    /// </summary>
    internal static class Module_NhuongUI
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        /// <summary>
        /// Chế độ TĂNG TỐC: bật khi người dùng đã bấm Đăng nhập. Lúc này người dùng đang CHỜ nên không nhường nữa:
        /// bỏ qua chờ "rảnh tay", chia lát dài hơn và nhường bằng Task.Yield (không chờ đồng hồ ~15 ms của Task.Delay).
        /// Do Module_GiaoDienForm2Web bật/tắt; luôn được tắt lại khi việc dựng Form2 kết thúc.
        /// </summary>
        public static volatile bool TangToc;
        // ---------------- Tín hiệu "kho giao diện chính đã nạp xong" ----------------
        // Các trang con cấp 2 (Form52 dựng sẵn Form49/50/51/34, Form53 dựng sẵn Form23/59/46) trước đây chạy SONG SONG với kho
        // chính ngay sau đăng nhập => các khối 200–430 ms chen vào đúng lúc người dùng bắt đầu bấm (đo: hieunang 22/11 09:23).
        // Nay chúng chờ tín hiệu này: kho chính (5 trang hay dùng) xong hẳn rồi mới tới lượt trang con cấp 2.
        private static readonly TaskCompletionSource _khoChinhXong = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>Form2 gọi khi lượt nạp kho chính sau đăng nhập kết thúc (xong hoặc dừng). Gọi nhiều lần an toàn.</summary>
        public static void BaoKhoChinhXong() => _khoChinhXong.TrySetResult();
        /// <summary>
        /// Chờ (không chặn UI) tới khi kho chính nạp xong, tối đa <paramref name="toiDaMs"/> (quá hạn vẫn cho chạy để không bị bỏ đói).
        /// Trả false nếu cần dừng hẳn (form đóng...).
        /// </summary>
        public static async Task<bool> ChoKhoChinhXongAsync(Func<bool> dung, int toiDaMs = 20000)
        {
            var t = _khoChinhXong.Task;
            var sw = Stopwatch.StartNew();
            while (!t.IsCompleted && !dung() && sw.ElapsedMilliseconds < toiDaMs)
                await Task.WhenAny(t, Task.Delay(250));
            return !dung();
        }
        // ---------------- "Lượt nặng" dùng chung giữa các bộ dựng sẵn ----------------
        // Form52 (Form49/50/51/34), Form53 (Form23/59/46) và Form6 (Form55 -> Form42) đều chờ "người dùng rảnh 0,5 s" rồi mới
        // dựng 1 khối nặng => cả 3 cùng thức dậy ở CÙNG một nhịp rảnh và chạy dồn liền nhau. Đo 22/11 12:55: UI kẹt 434 ms
        // (new Form42 -> new Form49 -> tạo handle ComboBox -> Form53...). Nay mỗi lúc chỉ 1 bộ được giữ lượt.
        private static readonly SemaphoreSlim _luotNang = new(1, 1);
        /// <summary>
        /// Xếp hàng giữ "lượt nặng" (không chặn UI). Trả null nếu phải dừng hẳn trong lúc chờ. Dispose để nhả lượt.
        /// Giữ lượt rồi mới <see cref="ChoRanhAsync"/>: khối nặng chạy ngay sau khi rảnh, không chạy chồng bộ khác.
        /// </summary>
        public static async Task<IDisposable?> GiuLuotNangAsync(Func<bool> dung)
        {
            while (!await _luotNang.WaitAsync(250).ConfigureAwait(true))
                if (dung()) return null;
            if (dung()) { _luotNang.Release(); return null; }
            return new NhaLuotNang();
        }
        private sealed class NhaLuotNang : IDisposable
        {
            private int _daNha;
            public void Dispose() { if (Interlocked.Exchange(ref _daNha, 1) == 0) _luotNang.Release(); }
        }
        /// <summary>Số ms kể từ lần cuối người dùng bấm phím / di chuột (toàn hệ thống). Lỗi thì coi như đã rảnh.</summary>
        public static int MsKeTuLanThaoTacCuoi()
        {
            try
            {
                var li = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
                if (!GetLastInputInfo(ref li)) return int.MaxValue;
                return (int)unchecked((uint)Environment.TickCount - li.dwTime);
            }
            catch { return int.MaxValue; }
        }
        /// <summary>
        /// Chờ tới lúc người dùng rảnh tay. Trả false nếu cần dừng hẳn (người dùng đã đăng nhập / form đóng).
        /// Chờ tối đa <paramref name="toiDaMs"/>: quá hạn vẫn cho chạy để nạp trước không bị bỏ đói mãi.
        /// </summary>
        public static async Task<bool> ChoRanhAsync(Func<bool> dung, int ranhMs = 180, int toiDaMs = 2000)
        {
            if (TangToc) return !dung();   // người dùng đang chờ đăng nhập: chạy ngay
            var sw = Stopwatch.StartNew();
            while (true)
            {
                if (dung()) return false;
                if (MsKeTuLanThaoTacCuoi() >= ranhMs) break;
                if (sw.ElapsedMilliseconds >= toiDaMs) break;
                await Task.Delay(40);
            }
            // Một nhịp để vòng lặp thông điệp xử lý nốt sự kiện đang chờ trước khi vào khối nặng
            await Task.Delay(1);
            return !dung();
        }
        /// <summary>
        /// Tạo handle cho cả cây control theo từng lát ngắn. Giữa các lát nhường vòng lặp thông điệp.
        /// Trả false nếu bị dừng giữa chừng.
        /// </summary>
        public static async Task<bool> TaoHandleTheoLatAsync(Control goc, Func<bool> dung, int latMs = 8)
        {
            var ngan = new Stack<Control>();
            ngan.Push(goc);
            var sw = Stopwatch.StartNew();
            int soLatYield = 0;
            while (ngan.Count > 0)
            {
                if (dung()) return false;
                var c = ngan.Pop();
                string ten = c.GetType().FullName ?? "";
                if (ten.Contains("WebView2") || ten.Contains("WebBrowser")) continue; // control nặng, để lúc cần mới tạo
                try
                {
                    if (Module_DoHieuNang.DangDo)
                    {
                        using (Module_DoHieuNang.Doan($"tạo handle {c.GetType().Name} '{c.Name}'")) _ = c.Handle;
                    }
                    else _ = c.Handle;
                }
                catch { continue; }
                for (int i = c.Controls.Count - 1; i >= 0; i--) ngan.Push(c.Controls[i]);
                if (TangToc)
                {
                    // Tăng tốc: lát 30 ms, nhường bằng Yield (Task.Delay(1) thực tế chờ ~15 ms mỗi lần => làm chậm gần gấp đôi)
                    if (sw.ElapsedMilliseconds >= 30)
                    {
                        await Task.Yield();
                        sw.Restart();
                    }
                }
                else if (sw.ElapsedMilliseconds >= latMs)
                {
                    // Task.Delay(1) thực tế chờ ~15 ms (độ phân giải đồng hồ Windows) => lát 8 ms làm + 15 ms chờ: gần 2/3 thời gian
                    // ngồi không. Khi người dùng KHÔNG thao tác: nhường bằng Yield (gần như tức thì), cứ ~4 lát mới Delay một lần để
                    // vẽ/timer vẫn chạy. Người dùng vừa gõ/di chuột thì quay lại Delay(1) mỗi lát như cũ (không giật phím).
                    if (MsKeTuLanThaoTacCuoi() >= 300 && ++soLatYield < 4)
                    {
                        await Task.Yield();
                    }
                    else
                    {
                        soLatYield = 0;
                        await Task.Delay(1);   // trả luồng UI cho phím/chuột
                    }
                    sw.Restart();
                }
            }
            return true;
        }
    }
}