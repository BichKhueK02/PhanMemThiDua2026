using System.Diagnostics;

namespace PhanMemThiDua2026
{
    /// <summary>
    /// Nạp trước Form2_FormCha (giao diện chính) trong lúc người dùng còn ở form đăng nhập.
    /// - TẤT CẢ thao tác UI đều chạy trên luồng UI (không tạo Form ở luồng nền).
    /// - Chia nhỏ từng bước, xen kẽ Task.Delay để form đăng nhập vẫn gõ phím mượt.
    /// - Chỉ làm những việc KHÔNG phụ thuộc người dùng. Dữ liệu theo tài khoản vẫn nạp sau khi
    ///   đăng nhập (Form2_Load chạy ở lần Show đầu tiên, lúc đó SessionInfo đã có).
    /// </summary>
    internal static class Form2Preloader
    {
        private static Form2_FormCha? _form2;
        private static bool _dangNap;
        private static bool _daDung;   // form đã được lấy ra dùng -> dừng các bước nạp trước

        /// <summary>Gọi từ luồng UI (vd. Form1 sau khi hiện). An toàn khi gọi nhiều lần.</summary>
        public static async Task NapTruocAsync(Func<bool> nguoiDungDaRoi)
        {
            if (_dangNap || _form2 != null) return;
            _dangNap = true;
            bool Het() => _daDung || nguoiDungDaRoi();
            var sw = Stopwatch.StartNew();
            try
            {
                // Cho form đăng nhập hiện ra và người dùng bắt đầu gõ trước đã
                await Task.Delay(400);
                if (Het()) return;

                // 1. Nền: nạp danh sách CSDL (không đụng UI)
                var taskDuLieu = Task.Run(() =>
                {
                    try { DataLoader.PreloadDanhSach(Module_DanduongGPS.DuongDanCSDL2); return true; }
                    catch (Exception ex) { Debug.WriteLine("[Preload] DataLoader: " + ex.Message); return false; }
                });

                // 2. UI: tạo instance Form2 (trên luồng UI)
                var f = new Form2_FormCha();
                f.TacVuPreloadDuLieu = taskDuLieu;
                _form2 = f;
                Debug.WriteLine($"[Preload] new Form2: {sw.ElapsedMilliseconds} ms");
                await Task.Delay(30);
                if (Het()) return;

                // 3. UI: handle + trang chủ + vẽ thử (từng bước nhỏ, tự dừng nếu người dùng đã đăng nhập)
                await f.NapTruocGiaoDienAsync(Het);
                Debug.WriteLine($"[Preload] xong giao diện: {sw.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Preload] Lỗi: " + ex);
                // Form dở dang -> bỏ đi để lần Lay() sau tạo mới sạch sẽ
                if (!_daDung)
                {
                    try { _form2?.Dispose(); } catch { }
                    _form2 = null;
                }
            }
            finally
            {
                _dangNap = false;
            }
        }

        /// <summary>Lấy Form2 để hiển thị (dùng bản đã nạp trước nếu có, nếu chưa thì tạo mới). Gọi trên luồng UI.</summary>
        public static Form2_FormCha Lay()
        {
            _daDung = true;
            if (_form2 == null || _form2.IsDisposed)
                _form2 = new Form2_FormCha();
            return _form2;
        }
    }
}