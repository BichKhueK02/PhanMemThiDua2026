namespace PhanMemThiDua2026
{
    /// <summary>
    /// "Đèn đỏ" cho mọi việc nền KHÔNG khẩn cấp trong lúc người dùng đang bấm Đăng nhập
    /// (xác thực BCrypt + dựng/hiện Form2). Lúc đó:
    ///  - nạp trước Form2 / các trang con (chạy trên luồng UI) tự dừng ở nhịp kế tiếp,
    ///  - tuần tra CSDL, tường lửa, dọn dẹp... (luồng nền) tự chờ,
    /// để CPU / đĩa / luồng giao diện dành trọn cho việc đăng nhập. Hết đèn đỏ thì các việc đó chạy tiếp.
    /// Chỉ HOÃN, không hủy việc nào; có chốt chặn thời gian nên không bao giờ hoãn vô hạn.
    /// </summary>
    internal static class Module_UuTienDangNhap
    {
        private const int NhipKiemTraMs = 20;
        private const int ToiDaChoMacDinhMs = 15000;   // một việc nền chờ tối đa bấy nhiêu rồi cứ chạy
        private const int ToiDaGiuDenMs = 30000;       // chốt chặn: tự nhả đèn đỏ nếu quên Dispose

        private static int _dem;

        /// <summary>true = đang có người bấm Đăng nhập, việc nền nên nhường.</summary>
        public static bool DangUuTien => Volatile.Read(ref _dem) > 0;

        /// <summary>Bật đèn đỏ. Dispose() để tắt (gọi nhiều lần vẫn an toàn). Tự tắt sau 30 giây nếu quên.</summary>
        public static IDisposable Bat()
        {
            Interlocked.Increment(ref _dem);
            var token = new Token();
            _ = Task.Delay(ToiDaGiuDenMs).ContinueWith(_ => token.Dispose(), TaskScheduler.Default);
            return token;
        }

        /// <summary>Gọi từ LUỒNG NỀN giữa các bước: nếu đang đèn đỏ thì ngủ ngắn cho tới khi hết (hoặc quá hạn).</summary>
        public static void NhuongNen(CancellationToken ct = default, int toiDaMs = ToiDaChoMacDinhMs)
        {
            if (!DangUuTien) return;
            long batDau = Environment.TickCount64;
            while (DangUuTien && !ct.IsCancellationRequested && Environment.TickCount64 - batDau < toiDaMs)
                Thread.Sleep(NhipKiemTraMs);
        }

        /// <summary>
        /// Gọi từ LUỒNG UI giữa các bước nạp trước form: chờ không chặn luồng (Task.Delay) cho tới khi hết đèn đỏ.
        /// Tiếp nối chạy lại trên luồng UI (không ConfigureAwait(false)) vì bước kế tiếp tạo control.
        /// </summary>
        public static async Task NhuongAsync(int toiDaMs = ToiDaChoMacDinhMs)
        {
            if (!DangUuTien) return;
            long batDau = Environment.TickCount64;
            while (DangUuTien && Environment.TickCount64 - batDau < toiDaMs)
                await Task.Delay(NhipKiemTraMs);
        }

        private sealed class Token : IDisposable
        {
            private int _daNha;
            public void Dispose()
            {
                if (Interlocked.Exchange(ref _daNha, 1) == 0)
                    Interlocked.Decrement(ref _dem);
            }
        }
    }
}