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
                try { _ = c.Handle; } catch { continue; }
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