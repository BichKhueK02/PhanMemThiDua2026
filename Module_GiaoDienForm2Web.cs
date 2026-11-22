using System.Diagnostics;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Nạp trước Form2_FormCha (giao diện chính) trong lúc người dùng còn ở form đăng nhập.
    /// - TẤT CẢ thao tác UI đều chạy trên luồng UI (không tạo Form ở luồng nền).
    /// - Trước khi bấm Đăng nhập: chia nhỏ từng bước, nhường khi người dùng đang gõ (Module_NhuongUI) và khi có "đèn đỏ".
    /// - Khi bấm Đăng nhập: <see cref="DamBaoSanSangAsync"/> bật chế độ TĂNG TỐC (Module_NhuongUI.TangToc) và dựng nốt Form2
    ///   NGAY, song song với việc xác thực (BCrypt) đang chạy ở luồng nền => thời gian chờ ~ max(BCrypt, dựng Form2) thay vì cộng dồn.
    /// - Mỗi lần chạy có "thế hệ" riêng: lần chạy tăng tốc thay thế mọi lần chạy cũ đang dở (lần cũ tự dừng ở bước kế tiếp),
    ///   nên không bị kẹt chờ "đèn đỏ" / "người dùng rảnh" của lần chạy trước.
    /// - Chỉ làm những việc KHÔNG phụ thuộc người dùng. Dữ liệu theo tài khoản vẫn nạp sau khi đăng nhập (Form2_Load).
    /// </summary>
    internal static class Module_GiaoDienForm2Web
    {
        private static Form2_FormCha? _form2;
        private static Task? _tacVuNap;
        private static int _theHe;                 // tăng mỗi lần có lần chạy mới -> lần chạy cũ tự dừng
        private static bool _daDung;               // form đã được lấy ra dùng -> dừng các bước nạp trước
        private static volatile bool _sanSang;     // Form2 + trang chủ đã dựng xong (hiện được ngay)
        /// <summary>Gọi từ luồng UI (vd. Form1 sau khi hiện). An toàn khi gọi nhiều lần.</summary>
        public static Task NapTruocAsync(Func<bool> nguoiDungDaRoi)
        {
            if (_sanSang) return Task.CompletedTask;
            var t = _tacVuNap;
            if (t != null && !t.IsCompleted) return t;
            return _tacVuNap = ChayAsync(nguoiDungDaRoi);
        }
        /// <summary>
        /// Gọi từ luồng UI ngay khi người dùng bấm Đăng nhập (SAU khi đã đẩy việc xác thực xuống luồng nền).
        /// Dựng nốt Form2 ở chế độ tăng tốc; Task hoàn tất khi sẵn sàng (hoặc bị dừng). Không bao giờ ném lỗi.
        /// </summary>
        public static async Task DamBaoSanSangAsync()
        {
            if (_sanSang || _daDung) return;
            Module_NhuongUI.TangToc = true;
            try
            {
                var t = _tacVuNap = ChayAsync(() => false);   // thế hệ mới: thay thế lần chạy cũ đang chờ nhường
                await t;
            }
            finally
            {
                Module_NhuongUI.TangToc = false;
            }
        }
        /// <summary>
        /// Gọi từ luồng UI sau khi xác thực THÀNH CÔNG, trước khi hiện Form2: nạp đủ các trang chính (Form4, Form6,
        /// Form15, Form34, Form10) vào kho RAM ở chế độ tăng tốc. Nhờ vậy vào giao diện chính là mọi trang đã sẵn,
        /// không còn cảnh "vào rồi mới nạp từ từ" (trước đây kho chỉ kịp nạp nếu người dùng ngồi chờ ở màn hình đăng nhập
        /// khoảng 5 giây; đăng nhập nhanh thì mọi trang đều bị dựng sau khi vào -> bấm trang nào cũng giật).
        /// Không bao giờ ném lỗi. Trả true nếu đủ các trang chính.
        /// </summary>
        public static async Task<bool> DamBaoKhoTrangChinhAsync(Action<int, int>? tienDo, int toiDaMs = 20000)
        {
            var f = _form2;
            if (f == null || f.IsDisposed || !_sanSang) return false;   // Form2 chưa dựng xong: Show() tự lo như cũ
            bool cu = Module_NhuongUI.TangToc;
            Module_NhuongUI.TangToc = true;
            try { return await f.DamBaoTrangChinhTrongKhoAsync(tienDo, toiDaMs); }
            catch (Exception ex) { Debug.WriteLine("[Preload] DamBaoKhoTrangChinh: " + ex); return false; }
            finally { Module_NhuongUI.TangToc = cu; }
        }
        private static async Task ChayAsync(Func<bool> nguoiDungDaRoi)
        {
            int theHe = Interlocked.Increment(ref _theHe);
            bool Het() => _daDung || theHe != Volatile.Read(ref _theHe) || nguoiDungDaRoi();
            var sw = Stopwatch.StartNew();
            try
            {
                if (!Module_NhuongUI.TangToc)
                {
                    // Cho form đăng nhập hiện ra trước đã (người dùng gõ gì thì ChoRanhAsync bên dưới còn nhường tiếp)
                    await Task.Delay(100);
                    if (Het()) return;
                    await Module_UuTienDangNhap.NhuongAsync();   // vừa bấm Đăng nhập -> chưa dựng gì lúc này
                    if (Het()) return;
                }
                var f = _form2;
                if (f == null || f.IsDisposed)
                {
                    // 1. Nền: nạp danh sách CSDL (không đụng UI)
                    var taskDuLieu = Task.Run(() =>
                    {
                        try { DataLoader.PreloadDanhSach(Module_DanduongGPS.DuongDanCSDL2); return true; }
                        catch (Exception ex) { Debug.WriteLine("[Preload] DataLoader: " + ex.Message); return false; }
                    });
                    // 2. UI: tạo instance Form2 (trên luồng UI)
                    f = new Form2_FormCha();
                    f.TacVuPreloadDuLieu = taskDuLieu;
                    _form2 = f;
                    Debug.WriteLine($"[Preload] new Form2: {sw.ElapsedMilliseconds} ms");
                    if (!Module_NhuongUI.TangToc)
                    {
                        await Task.Delay(30);
                        if (Het()) return;
                        await Module_UuTienDangNhap.NhuongAsync();
                        if (Het()) return;
                    }
                }
                // 3. UI: handle + trang chủ (+ vẽ thử nếu chưa tăng tốc). Gọi lại nhiều lần vẫn an toàn.
                bool xong = await f.NapTruocGiaoDienAsync(Het);
                if (xong && theHe == Volatile.Read(ref _theHe)) _sanSang = true;
                Debug.WriteLine($"[Preload] giao diện {(xong ? "sẵn sàng" : "dừng giữa chừng")}: {sw.ElapsedMilliseconds} ms");
                // 4. KHO GIAO DIỆN: người dùng vẫn đang gõ -> nạp tiếp các trang (Trang chủ, Phân loại, Thống kê, Khen thưởng,
                //    Nhật ký, Cài đặt) vào RAM: Load + dữ liệu + vẽ thật 1 lần. Chạy riêng (không giữ Task này) để đăng nhập
                //    không phải chờ; bấm Đăng nhập -> tự đứng chờ ("đèn đỏ"), đăng nhập xong -> dừng, Form2 làm nốt phần còn lại.
                if (xong && !Module_NhuongUI.TangToc && !Het())
                    _ = f.NapKhoAsync(Het, truocDangNhap: true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Preload] Lỗi: " + ex);
                // Form dở dang -> bỏ đi để lần Lay() sau tạo mới sạch sẽ
                if (!_daDung)
                {
                    try { _form2?.Dispose(); } catch { }
                    _form2 = null;
                    _sanSang = false;
                }
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