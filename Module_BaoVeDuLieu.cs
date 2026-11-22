using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
namespace PhanMemThiDua2026
{
    public static class Module_BaoVeDuLieu
    {
        //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
        //Đây giống như là Firewall - Lớp bảo vệ ở bên trong ứng dụng - Database Firewall
        // ===================== HẰNG SỐ CẤU HÌNH =====================
        // CSDL_Backups ở thư mục gốc là vị trí CŨ (đã bỏ sao lưu tự động): chỉ còn dùng để chuyển nhật ký bảo trì cũ sang
        // Database\NhatKyNhaPhatTrien; phần còn lại do tường lửa (Module_KhoiTaoCSDL) dọn vì không còn trong danh sách trắng.
        private const string TEN_THU_MUC_CU = "CSDL_Backups";
        private const string TEN_FILE_NHAT_KY = "BaoTri_NhatKy.txt";
        private const string HAU_TO_TAM = ".tmp";
        private const long NGUONG_XOAY_VONG_LOG = 2L * 1024 * 1024;       // log > 2 MB -> chuyển thành .old.txt
        private const int TRAN_HANG_DOI_LOG = 2000;                       // chặn RAM phình nếu Worker bị kẹt
        private const int SO_LOG_MOI_LAN_XA = 256;
        private const double NGUONG_PHAN_TRAM_PHAN_MANH = 20.0;
        private const long NGUONG_BYTE_LANG_PHI_TOI_THIEU = 2L * 1024 * 1024;   // < 2 MB rác thì không đáng VACUUM
        private const long DU_PHONG_BYTE = 16L * 1024 * 1024;             // dự phòng dung lượng trống
        private const int MOC_SO_DONG = 1000;                             // mốc số dòng nhật ký xét bảo trì nặng
        private static readonly TimeSpan ThoiGianToiDaVacuum = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan ThoiGianChoKhiThoat = TimeSpan.FromSeconds(3);
        // Mã lỗi SQLite (primary result code)
        private const int SQLITE_BUSY = 5;
        private const int SQLITE_LOCKED = 6;
        private const int SQLITE_INTERRUPT = 9;
        private const int SQLITE_CORRUPT = 11;
        private const int SQLITE_NOTADB = 26;
        private static readonly UTF8Encoding Utf8KhongBom = new UTF8Encoding(false);
        // 🛡️ ANTI-RACE: chỉ 1 tác vụ bảo trì (nhẹ hoặc nặng) tại 1 thời điểm
        private static readonly SemaphoreSlim _khoaTienTrinh = new SemaphoreSlim(1, 1);
        private static readonly string _thuMucCu = Path.Combine(AppContext.BaseDirectory, TEN_THU_MUC_CU);
        /// <summary>Hủy khi tiến trình thoát: ngắt êm VACUUM / kiểm tra đang chạy (SQLite tự hoàn tác) thay vì để tiến trình bị giết giữa chừng.</summary>
        private static readonly CancellationTokenSource _ctsThoat = new();
        // Nhật ký bảo trì nằm trong Database\NhatKyNhaPhatTrien (không tạo thêm tệp/thư mục ngoài Database);
        // vị trí cũ là CSDL_Backups\BaoTri_NhatKy.txt — tự chuyển sang ở lần ghi đầu tiên.
        private static string DuongDanNhatKy => Path.Combine(Module_NhatKyNhaPhatTrien.ThuMuc, TEN_FILE_NHAT_KY);
        private static int _daChuyenNhatKyCu;
        /// <summary>Một lần mỗi phiên: chuyển BaoTri_NhatKy.txt (+ .old.txt) từ CSDL_Backups sang Database\NhatKyNhaPhatTrien. Không ném lỗi.</summary>
        internal static void ChuyenNhatKyCuNeuCan()
        {
            if (Interlocked.Exchange(ref _daChuyenNhatKyCu, 1) == 1) return;
            try
            {
                Directory.CreateDirectory(Module_NhatKyNhaPhatTrien.ThuMuc);
                foreach (string ten in new[] { TEN_FILE_NHAT_KY, Path.ChangeExtension(TEN_FILE_NHAT_KY, ".old.txt") })
                {
                    try
                    {
                        string cu = Path.Combine(_thuMucCu, ten);
                        if (!File.Exists(cu)) continue;
                        string moi = Path.Combine(Module_NhatKyNhaPhatTrien.ThuMuc, ten);
                        if (!File.Exists(moi)) { File.Move(cu, moi); continue; }
                        // Đã có tệp mới: nối nội dung cũ vào đầu để không mất lịch sử
                        string gop = moi + ".gop";
                        using (var dich = new FileStream(gop, FileMode.Create, FileAccess.Write))
                        {
                            using (var nguon = File.OpenRead(cu)) nguon.CopyTo(dich);
                            using (var nguon = File.OpenRead(moi)) nguon.CopyTo(dich);
                        }
                        File.Move(gop, moi, overwrite: true);
                        File.Delete(cu);
                    }
                    catch (Exception ex) { Debug.WriteLine($"[Nhật ký bảo trì] Không chuyển được {ten}: {ex.Message}"); }
                }
            }
            catch { }
        }
        static Module_BaoVeDuLieu()
        {
            AppDomain.CurrentDomain.ProcessExit += (_, _) => DungKhiThoat();
        }
        private static void DungKhiThoat()
        {
            try
            {
                _ctsThoat.Cancel();
                // Cho tác vụ đang chạy tối đa vài giây để hoàn tác / nhả khóa (đoạn thay thế CSDL khi phục hồi không bị ngắt giữa chừng)
                var dongHo = Stopwatch.StartNew();
                while (!BaoTriDangRanh() && dongHo.Elapsed < ThoiGianChoKhiThoat)
                    Thread.Sleep(25);
            }
            catch { }
        }
        // ===================== TRẠNG THÁI THEO TỪNG CSDL =====================
        private sealed class TrangThaiDb
        {
            public volatile bool KhongDoiDuocAutoVacuum;   // đã VACUUM mà vẫn không chuyển được sang INCREMENTAL -> đừng lặp lại
            public long MocNhatKy = -1;                    // mốc 1000 dòng đã quan sát gần nhất (-1 = chưa quan sát)
        }
        private static readonly ConcurrentDictionary<string, TrangThaiDb> _trangThai = new(StringComparer.OrdinalIgnoreCase);
        private static TrangThaiDb? LayTrangThai(string dbPath)
        {
            try { return _trangThai.GetOrAdd(Path.GetFullPath(dbPath), _ => new TrangThaiDb()); }
            catch { return null; }
        }
        // ===================== KẾT NỐI =====================
        /// <summary>
        /// Pooling=false: Dispose là đóng hẳn tệp (cần cho Move/Copy/xóa tệp CSDL trên Windows).
        /// Mặc định ReadWrite (không Create): tệp biến mất giữa chừng thì báo lỗi thay vì âm thầm tạo ra tệp rỗng.
        /// </summary>
        private static SqliteConnection MoKetNoi(string dbPath, SqliteOpenMode mode = SqliteOpenMode.ReadWrite)
        {
            var sb = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = mode,
                Pooling = false,
                DefaultTimeout = 30
            };
            var cn = new SqliteConnection(sb.ToString());
            try
            {
                cn.Open();
                return cn;
            }
            catch
            {
                cn.Dispose();
                throw;
            }
        }
        /// <summary>
        /// Microsoft.Data.Sqlite thực thi theo kiểu ĐỒNG BỘ và chỉ kiểm tra CancellationToken trước khi bắt đầu câu lệnh,
        /// nên hủy/timeout không ngắt được VACUUM đang chạy. sqlite3_interrupt ngắt thật (VACUUM được hoàn tác an toàn).
        /// LƯU Ý: phải Dispose đăng ký này TRƯỚC khi đóng kết nối ('using var reg' khai báo sau 'using var conn' là đúng thứ tự).
        /// </summary>
        private static CancellationTokenRegistration DangKyNgat(SqliteConnection cn, CancellationToken ct)
        {
            if (!ct.CanBeCanceled) return default;
            return ct.Register(static state =>
            {
                try
                {
                    var c = (SqliteConnection)state!;
                    var h = c.Handle;
                    if (h != null) SQLitePCL.raw.sqlite3_interrupt(h);
                }
                catch { }
            }, cn);
        }
        private static bool LaLoiBan(SqliteException ex) =>
            ex.SqliteErrorCode == SQLITE_BUSY || ex.SqliteErrorCode == SQLITE_LOCKED;
        // ===================== ĐỊNH DẠNG (không phụ thuộc cài đặt vùng / lịch của máy) =====================
        // Máy cài lịch Phật giáo (th-TH), Hijri (ar-SA)... làm "yyyy" ra năm khác, và "/" bị đổi thành dấu phân cách của vùng:
        // tên bản sao lưu sẽ lệch và việc sắp xếp "mới nhất trước" theo tên bị sai.
        private static string Dinh(DateTime t, string dinhDang) => t.ToString(dinhDang, CultureInfo.InvariantCulture);
        private static string DinhDangDong((DateTime Gio, string NoiDung) m) =>
            $"[{Dinh(m.Gio, "dd/MM/yyyy HH:mm:ss")}] {m.NoiDung}";
        // ===================== HỆ THỐNG GHI LOG BẤT ĐỒNG BỘ (1 WORKER) =====================
        private static readonly ConcurrentQueue<(DateTime Gio, string NoiDung)> _hangDoiFile = new();
        private static readonly ConcurrentQueue<(DateTime Gio, string NoiDung)> _hangDoiDb = new();
        private static int _dangXaLog;
        /// <summary>Không có bảo trì (nhẹ/nặng) nào đang giữ khóa. Khi bận, việc ghi NhatKy vào CSDL được hoãn để không tranh khóa với VACUUM.</summary>
        private static bool BaoTriDangRanh() => _khoaTienTrinh.CurrentCount > 0;
        private static void DayVaoHangDoi(ConcurrentQueue<(DateTime Gio, string NoiDung)> q, (DateTime Gio, string NoiDung) muc)
        {
            q.Enqueue(muc);
            while (q.Count > TRAN_HANG_DOI_LOG && q.TryDequeue(out _)) { }   // giới hạn RAM: bỏ dòng cũ nhất
        }
        private static void GhiLog(string thongDiep)
        {
            try
            {
                var muc = (DateTime.Now, thongDiep);   // giờ lúc xảy ra sự kiện (không phải giờ lúc xả hàng đợi)
                Debug.WriteLine($"[Trình Bảo Trì] {thongDiep}");
                DayVaoHangDoi(_hangDoiFile, muc);
                DayVaoHangDoi(_hangDoiDb, muc);
                KichHoatWorker();
            }
            catch { }
        }
        private static void KichHoatWorker()
        {
            if (Interlocked.CompareExchange(ref _dangXaLog, 1, 0) == 0)
                _ = Task.Run(XaLogAsync);
        }
        private static async Task XaLogAsync()
        {
            try
            {
                await XaRaFileAsync().ConfigureAwait(false);
                XaVaoCsdl();
            }
            catch (Exception ex) { Debug.WriteLine($"[Log Worker] {ex.Message}"); }
            finally
            {
                Interlocked.Exchange(ref _dangXaLog, 0);
                // Double-check: tránh sót log rơi vào hàng đợi đúng lúc mở khóa.
                // Mọi nhánh lỗi ở trên đều rút cạn hàng đợi nên điều kiện này không thể lặp vô hạn.
                if (!_hangDoiFile.IsEmpty || (!_hangDoiDb.IsEmpty && BaoTriDangRanh()))
                    KichHoatWorker();
            }
        }
        private static async Task XaRaFileAsync()
        {
            string? filePath = null;
            var sb = new StringBuilder(1024);
            while (!_hangDoiFile.IsEmpty)
            {
                sb.Clear();
                int dem = 0;
                // Rút khỏi hàng đợi TRƯỚC khi ghi: lỗi ghi tệp (thư mục chỉ đọc, bị chặn...) chỉ làm mất lô này,
                // không để hàng đợi đầy mãi khiến Worker khởi động lại liên tục.
                // (Bản 2 gọi Directory.CreateDirectory ngoài khối bắt lỗi: ném lỗi → hàng đợi không rỗng → finally kích hoạt lại
                //  Worker → vòng lặp vô hạn chiếm 100% một nhân CPU.)
                while (dem < SO_LOG_MOI_LAN_XA && _hangDoiFile.TryDequeue(out var m))
                {
                    sb.AppendLine(DinhDangDong(m));
                    dem++;
                }
                if (dem == 0) break;
                try
                {
                    if (filePath == null)
                    {
                        ChuyenNhatKyCuNeuCan();
                        Directory.CreateDirectory(Module_NhatKyNhaPhatTrien.ThuMuc);
                        filePath = DuongDanNhatKy;
                        XoayVongNhatKyNeuCan(filePath);
                    }
                    await File.AppendAllTextAsync(filePath, sb.ToString(), Utf8KhongBom).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Log Worker] Không ghi được file nhật ký: {ex.Message}");
                }
            }
        }
        private static void XoayVongNhatKyNeuCan(string filePath)
        {
            try
            {
                var fi = new FileInfo(filePath);
                if (fi.Exists && fi.Length > NGUONG_XOAY_VONG_LOG)
                    File.Move(filePath, Path.ChangeExtension(filePath, ".old.txt"), overwrite: true);
            }
            catch { }
        }
        private static void XaVaoCsdl()
        {
            while (BaoTriDangRanh() && _hangDoiDb.TryDequeue(out var m))
            {
                try
                {
                    Module_NhatKy.GhiNhatKy(
                        taiKhoan: "System (Trình Bảo Trì CSDL)",
                        hanhDong: m.NoiDung,
                        // Bản 2 chỉ ghi giờ đăng nhập (giống nhau cho mọi dòng) dưới nhãn "Thời gian"
                        ghiChu: FormattableString.Invariant(
                            $"Thời gian: {m.Gio:dd-MM-yyyy HH:mm:ss} | Phiên đăng nhập: {SessionInfo.ThoiGianDangNhap:dd-MM-yyyy HH:mm:ss}")
                    );
                }
                catch { /* Bỏ qua lỗi ghi DB để Worker không bị chết (dòng log vẫn nằm trong file) */ }
            }
        }
        // ===================== RETRY (CHỐNG SQLITE_BUSY / SQLITE_LOCKED) =====================
        // Toàn bộ thao tác CSDL chạy ĐỒNG BỘ trên 1 luồng nền: Microsoft.Data.Sqlite không có I/O bất đồng bộ thật,
        // các hàm *Async của nó chạy đồng bộ và sẽ chiếm luồng gọi (thường là luồng giao diện).
        private static T ChayVoiRetry<T>(Func<T> action, int maxRetries, CancellationToken ct)
        {
            maxRetries = Math.Max(1, maxRetries);
            for (int i = 1; ; i++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    return action();
                }
                catch (SqliteException ex) when (LaLoiBan(ex) && i < maxRetries)
                {
                    // Exponential backoff + jitter (tránh nhiều tiến trình thử lại cùng lúc)
                    int delayMs = Math.Min(8000, 1000 * (1 << (i - 1))) + Random.Shared.Next(0, 250);
                    GhiLog($"[SQLite Busy] DB đang bận, thử lại lần {i} sau {delayMs}ms...");
                    if (ct.WaitHandle.WaitOne(delayMs)) ct.ThrowIfCancellationRequested();
                }
            }
        }
        // ===================== TIỆN ÍCH NHỎ =====================
        private static long DocPragmaSo(SqliteConnection conn, string tenPragma)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA " + tenPragma + ";";   // tenPragma chỉ nhận hằng số nội bộ
            object? r = cmd.ExecuteScalar();
            return r == null || r == DBNull.Value ? 0 : Convert.ToInt64(r, CultureInfo.InvariantCulture);
        }
        private static void ChayLenh(SqliteConnection conn, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
        private static bool DuDungLuong(string duongDan, long canByte)
        {
            try
            {
                string full = Path.GetFullPath(duongDan);
                string? goc = Path.GetPathRoot(full);
                if (string.IsNullOrEmpty(goc) || goc.StartsWith(@"\\", StringComparison.Ordinal)) return true;
                return new DriveInfo(goc).AvailableFreeSpace >= canByte;
            }
            catch { return true; }   // ổ mạng/UNC hoặc không xác định được -> không chặn
        }
        // ===================== TÁC VỤ ĐỘC QUYỀN TỪ GIAO DIỆN =====================
        /// <summary>
        /// Chạy 1 tác vụ cần độc quyền CSDL (vd: thu gọn ở Form33) dưới CÙNG khóa với bảo trì nền => không bao giờ chạy song song
        /// với VACUUM / sao lưu / phục hồi. Chạy trên luồng riêng (LongRunning). Hủy khi tiến trình thoát.
        /// Ném TimeoutException nếu bảo trì nền đang bận quá <paramref name="thoiGianCho"/>.
        /// </summary>
        public static async Task<T> ChayDocQuyenAsync<T>(Func<CancellationToken, T> viec, TimeSpan thoiGianCho, CancellationToken ct = default)
        {
            using var lk = CancellationTokenSource.CreateLinkedTokenSource(ct, _ctsThoat.Token);
            if (!await _khoaTienTrinh.WaitAsync(thoiGianCho, lk.Token).ConfigureAwait(false))
                throw new TimeoutException("Hệ thống đang bảo trì CSDL ở chế độ nền. Vui lòng thử lại sau ít phút.");
            try
            {
                return await Task.Factory.StartNew(() => viec(lk.Token), lk.Token,
                    TaskCreationOptions.LongRunning, TaskScheduler.Default).ConfigureAwait(false);
            }
            finally
            {
                _khoaTienTrinh.Release();
            }
        }
        /// <summary>Ghi 1 dòng vào nhật ký bảo trì (BaoTri_NhatKy.txt) từ module khác.</summary>
        public static void GhiNhatKyBaoTri(string thongDiep) => GhiLog(thongDiep);
        // ===================== THU GỌN ĐỊNH KỲ (TỐI THIỂU 6 THÁNG / LẦN) =====================
        // Mốc lần thu gọn gần nhất của TỪNG CSDL lưu trong Database\NhatKyNhaPhatTrien\LichThuGonCSDL.txt (tên tệp|thời điểm):
        // đi theo bản portable khi chép sang máy khác, không ghi vào CSDL (không làm tệp CSDL thay đổi chỉ vì lưu lịch).
        private static readonly TimeSpan ChuKyThuGon = TimeSpan.FromDays(182);
        private static readonly TimeSpan TreKhiKhoiDong = TimeSpan.FromMinutes(3);   // không tranh tài nguyên lúc vừa mở phần mềm
        private const string TEN_TEP_LICH_THU_GON = "LichThuGonCSDL.txt";
        private const string DINH_DANG_MOC = "yyyy-MM-dd HH:mm:ss";
        private static readonly object _khoaLich = new();
        private static int _daLenLichThuGon;
        private static string DuongDanLichThuGon => Path.Combine(Module_NhatKyNhaPhatTrien.ThuMuc, TEN_TEP_LICH_THU_GON);
        private static Dictionary<string, DateTime> DocLichThuGon()
        {
            var kq = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(DuongDanLichThuGon)) return kq;
                foreach (string dong in File.ReadAllLines(DuongDanLichThuGon, Utf8KhongBom))
                {
                    int i = dong.IndexOf('|');
                    if (i <= 0) continue;
                    if (DateTime.TryParseExact(dong[(i + 1)..].Trim(), DINH_DANG_MOC, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime t))
                        kq[dong[..i].Trim()] = t;
                }
            }
            catch (Exception ex) { Debug.WriteLine("[Lịch thu gọn] Không đọc được: " + ex.Message); }
            return kq;
        }
        /// <summary>Ghi mốc "vừa thu gọn" cho 1 CSDL (gọi cả khi người dùng bấm Vacuum ở Form33). Ghi ra .tmp rồi đổi tên: không bao giờ để tệp lịch dở dang.</summary>
        public static void GhiMocThuGon(string dbPath, DateTime? luc = null)
        {
            try
            {
                lock (_khoaLich)
                {
                    var lich = DocLichThuGon();
                    lich[Path.GetFileName(dbPath)] = luc ?? DateTime.Now;
                    Directory.CreateDirectory(Module_NhatKyNhaPhatTrien.ThuMuc);
                    string tam = DuongDanLichThuGon + HAU_TO_TAM;
                    File.WriteAllLines(tam, lich.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase)
                        .Select(k => k.Key + "|" + k.Value.ToString(DINH_DANG_MOC, CultureInfo.InvariantCulture)), Utf8KhongBom);
                    File.Move(tam, DuongDanLichThuGon, overwrite: true);
                }
            }
            catch (Exception ex) { Debug.WriteLine("[Lịch thu gọn] Không ghi được: " + ex.Message); }
        }
        /// <summary>
        /// Đã đủ 6 tháng kể từ lần thu gọn gần nhất? Chưa từng có mốc (bản cập nhật đầu tiên) => lấy hôm nay làm mốc, KHÔNG chạy ngay
        /// (tránh mọi máy cùng dồn CSDL ngay sau khi cập nhật). Mốc nằm ở tương lai (đồng hồ máy sai / chép từ máy khác) => đặt lại mốc.
        /// </summary>
        private static bool DaDenHanThuGon(string dbPath, DateTime now)
        {
            string ten = Path.GetFileName(dbPath);
            DateTime moc;
            lock (_khoaLich)
            {
                if (!DocLichThuGon().TryGetValue(ten, out moc))
                {
                    GhiMocThuGon(dbPath, now);
                    return false;
                }
            }
            if (moc > now.AddDays(1))
            {
                GhiMocThuGon(dbPath, now);
                return false;
            }
            return now - moc >= ChuKyThuGon;
        }
        /// <summary>
        /// Gọi 1 lần khi phần mềm đã mở xong (Form2). Sau vài phút, CSDL nào đã quá 6 tháng chưa thu gọn thì dồn lại ở luồng nền
        /// (cùng cơ chế với nút Vacuum ở Form33, KHÔNG tự xóa dòng trùng, không đủ lợi thì không ghi gì). Đang bị dùng thì để lần mở sau.
        /// <paramref name="layDanhSachCsdl"/> được gọi lúc chạy (đường dẫn có thể đổi sau khi khôi phục / chuyển thư mục lưu trữ).
        /// </summary>
        public static void LenLichThuGonDinhKy(Func<IEnumerable<string?>> layDanhSachCsdl)
        {
            if (Interlocked.Exchange(ref _daLenLichThuGon, 1) == 1) return;
            _ = Task.Run(async () =>
            {
                try { await Task.Delay(TreKhiKhoiDong, _ctsThoat.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }
                List<string> ds;
                try { ds = layDanhSachCsdl().Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p)).Select(p => p!).ToList(); }
                catch (Exception ex) { GhiLog("[Thu gọn định kỳ] Không lấy được danh sách CSDL: " + ex.Message); return; }
                foreach (string path in ds)
                {
                    try
                    {
                        if (!DaDenHanThuGon(path, DateTime.Now)) continue;
                        await ChayDocQuyenAsync(ct => { ThuGonDinhKy(path, ct); return true; }, TimeSpan.FromMinutes(2)).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { return; }   // phần mềm đang thoát
                    catch (TimeoutException) { GhiLog($"[Thu gọn định kỳ] {Path.GetFileName(path)}: hệ thống đang bảo trì, để lần mở sau."); }
                    catch (Exception ex) { GhiLog($"[Thu gọn định kỳ] {Path.GetFileName(path)}: {ex.Message}"); }
                    finally { KichHoatWorker(); }
                }
            });
        }
        private static void ThuGonDinhKy(string path, CancellationToken ct)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(ThoiGianToiDaVacuum);
            string? thuMucTam = null;
            string ten = Path.GetFileName(path);
            try
            {
                try { thuMucTam = Module_NhatKyNhaPhatTrien.TaoThuMucTam("ThuGonDinhKy"); } catch { thuMucTam = null; }
                var kq = Module_ThuGonCSDL.ThuGon(path, thuMucTam, cts.Token, batBuoc: false, khuTrungLap: false, chapNhanDoiCheDo: false);
                GhiLog($"[Thu gọn định kỳ] {ten}: {kq.ThongBao}");
                // Thành công (kể cả "đã gọn, bỏ qua") => tính lại 6 tháng. Thất bại (đang bị dùng / lỗi) => không ghi mốc, thử lại lần mở sau.
                if (kq.ThanhCong) GhiMocThuGon(path);
            }
            catch (OperationCanceledException)
            {
                if (ct.IsCancellationRequested) throw;
                GhiLog($"[Thu gọn định kỳ] {ten}: vượt quá thời gian cho phép, đã ngắt an toàn (CSDL giữ nguyên).");
            }
            finally
            {
                if (thuMucTam != null) { try { Directory.Delete(thuMucTam, true); } catch { } }
            }
        }
        private static async Task ChayBaoTriNangAsync(string duongDanDB, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(duongDanDB) || !File.Exists(duongDanDB)) return;
            using var lk = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _ctsThoat.Token);
            try
            {
                await _khoaTienTrinh.WaitAsync(lk.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                GhiLog("[Bảo Trì Nặng] Đã hủy khi đang chờ đến lượt chạy.");
                return;
            }
            try
            {
                // LongRunning: tác vụ kéo dài hàng phút (VACUUM) chạy trên luồng riêng, không chiếm ThreadPool (máy 2 nhân vẫn mượt)
                await Task.Factory.StartNew(
                        () => BaoTriNangDongBo(duongDanDB, lk.Token),
                        lk.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) { GhiLog("[Bảo Trì Nặng] Đã hủy tiến trình (Timeout hoặc User tắt App)."); }
            catch (Exception ex) { GhiLog($"[Lỗi Bảo trì nặng] {ex.Message}"); }
            finally
            {
                _khoaTienTrinh.Release();
                KichHoatWorker();   // xả các dòng NhatKy bị hoãn trong lúc CSDL bị khóa
            }
        }
        /// <summary>
        /// Bảo trì nặng (khi chạm mốc 1000 dòng nhật ký): kiểm tra toàn vẹn -> dồn CSDL nếu đã đủ 6 tháng và thực sự cần.
        /// Đã bỏ sao lưu / tự phục hồi: CSDL hỏng chỉ được ghi cảnh báo và GIỮ NGUYÊN (không VACUUM), khôi phục bằng ServiceRestore.
        /// </summary>
        private static void BaoTriNangDongBo(string duongDanDB, CancellationToken ct)
        {
            string ten = Path.GetFileName(duongDanDB);
            GhiLog($"[Bảo Trì Nặng] Đang xử lý: {ten}...");
            if (!CsdlKhoeManh(duongDanDB, ct))
            {
                GhiLog($"[CẢNH BÁO ĐỎ] {ten} không vượt qua kiểm tra toàn vẹn! Giữ nguyên tệp, bỏ qua VACUUM; cần khôi phục bằng ServiceRestore.");
                return;
            }
            if (DaDenHanThuGon(duongDanDB, DateTime.Now) && CanFullVacuum(duongDanDB, ct))
                ThucHienFullVacuum(duongDanDB, ct);
        }
        /// <summary>
        /// Hỏng thật sự khi integrity_check != ok hoặc SQLite báo CORRUPT / NOTADB. BUSY không bao giờ bị coi là hỏng.
        /// Xác nhận 2 lần cách nhau 1,5 giây để loại trừ lỗi đọc thoáng qua (phần mềm diệt virus quét tệp, ổ đĩa chập chờn).
        /// </summary>
        private static bool CsdlKhoeManh(string dbPath, CancellationToken ct)
        {
            if (ChayVoiRetry(() => KiemTraToanVen(dbPath, nhanh: false, ct), 3, ct)) return true;
            GhiLog($"[Cảnh Báo] {Path.GetFileName(dbPath)} không vượt qua kiểm tra toàn vẹn, kiểm tra lại lần 2 trước khi phục hồi...");
            if (ct.WaitHandle.WaitOne(1500)) ct.ThrowIfCancellationRequested();
            return ChayVoiRetry(() => KiemTraToanVen(dbPath, nhanh: false, ct), 3, ct);
        }
        /// <summary>true nếu "ok". Tệp hỏng nặng làm SQLite ném CORRUPT/NOTADB thay vì trả kết quả: coi là hỏng.</summary>
        private static bool KiemTraToanVen(string dbPath, bool nhanh, CancellationToken ct)
        {
            try
            {
                using var conn = MoKetNoi(dbPath);
                using var reg = DangKyNgat(conn, ct);
                using var cmd = conn.CreateCommand();
                cmd.CommandText = nhanh ? "PRAGMA quick_check(1);" : "PRAGMA integrity_check(1);";   // dừng ở lỗi đầu tiên cho nhanh
                object? kq = cmd.ExecuteScalar();
                return string.Equals(kq?.ToString(), "ok", StringComparison.OrdinalIgnoreCase);
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == SQLITE_CORRUPT || ex.SqliteErrorCode == SQLITE_NOTADB)
            {
                return false;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == SQLITE_INTERRUPT && ct.IsCancellationRequested)
            {
                throw new OperationCanceledException(ct);
            }
        }
        private static bool CanFullVacuum(string dbPath, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return false;
            var st = LayTrangThai(dbPath);
            return ChayVoiRetry(() =>
            {
                ct.ThrowIfCancellationRequested();
                using var conn = MoKetNoi(dbPath);
                long pageCount = DocPragmaSo(conn, "page_count");
                if (pageCount <= 0) return false;
                long freeCount = DocPragmaSo(conn, "freelist_count");
                long pageSize = DocPragmaSo(conn, "page_size");
                long autoVacuum = DocPragmaSo(conn, "auto_vacuum");
                double fragmentationPercent = (freeCount * 100.0) / pageCount;
                long byteLangPhi = freeCount * pageSize;
                GhiLog(FormattableString.Invariant(
                    $"[Kiểm Tra Vacuum] PageCount={pageCount:N0}, FreeList={freeCount:N0}, Fragment={fragmentationPercent:F2}%, AutoVacuum={autoVacuum}"));
                bool nhieuRac = fragmentationPercent > NGUONG_PHAN_TRAM_PHAN_MANH && byteLangPhi >= NGUONG_BYTE_LANG_PHI_TOI_THIEU;
                // 1 lần VACUUM duy nhất để bật INCREMENTAL. Cờ "không đổi được" nay theo TỪNG CSDL (bản 2 dùng 1 biến chung: 1 CSDL thất bại làm tắt cho tất cả)
                bool canDoiAutoVacuum = autoVacuum != 2 && !(st?.KhongDoiDuocAutoVacuum ?? false);
                if (!nhieuRac && !canDoiAutoVacuum) return false;
                // VACUUM INTO chỉ cần chỗ cho tệp MỚI (= phần dữ liệu thật, không gồm trang rỗng) trên ổ chứa CSDL; không dùng %TEMP%
                long kichThuoc = (pageCount - freeCount) * pageSize;
                string thuMucDb = Path.GetDirectoryName(Path.GetFullPath(dbPath)) ?? dbPath;
                if (!DuDungLuong(thuMucDb, kichThuoc + DU_PHONG_BYTE))
                {
                    GhiLog("[Kiểm Tra Vacuum] Bỏ qua VACUUM: không đủ dung lượng trống.");
                    return false;
                }
                return true;
            }, 3, ct);
        }
        /// <summary>
        /// Dồn CSDL bằng CÙNG cơ chế với nút "Chạy Vacuum" ở Form33 (Module_ThuGonCSDL): VACUUM INTO ra tệp mới -> kiểm chứng -> File.Replace.
        /// So với VACUUM tại chỗ (bản cũ): ghi đĩa ~1 lần phần dữ liệu thật thay vì ~2 lần cả tệp (WAL + checkpoint), KHÔNG ghi bản tạm vào %TEMP%,
        /// và tệp gốc không bị đụng tới cho tới khi tệp mới đã được kiểm chứng.
        /// Chạy nền tự động nên: KHÔNG tự xóa dòng trùng (chỉ làm khi người dùng bấm), không thử lại khi tệp đang bị mở (để lượt sau).
        /// </summary>
        private static void ThucHienFullVacuum(string dbPath, CancellationToken ct)
        {
            var st = LayTrangThai(dbPath);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(ThoiGianToiDaVacuum);   // timeout THẬT: Module_ThuGonCSDL gọi sqlite3_interrupt khi token bị hủy
            string? thuMucTam = null;
            try
            {
                try { thuMucTam = Module_NhatKyNhaPhatTrien.TaoThuMucTam("BaoTriNang"); } catch { thuMucTam = null; }
                var kq = Module_ThuGonCSDL.ThuGon(dbPath, thuMucTam, cts.Token,
                    batBuoc: true,              // CanFullVacuum đã quyết định là cần
                    khuTrungLap: false,
                    chapNhanDoiCheDo: true);    // đổi sang INCREMENTAL 1 lần dù tệp mới lớn hơn vài KB
                string ten = Path.GetFileName(dbPath);
                if (kq.ThanhCong)
                {
                    GhiLog($"[Bảo Trì Nặng] {ten}: {kq.ThongBao}");
                    GhiMocThuGon(dbPath);   // tính lại chu kỳ 6 tháng
                    // Đã chạy mà vẫn không ở INCREMENTAL -> đừng lặp lại mỗi lượt
                    if (kq.AutoVacuumSau >= 0 && kq.AutoVacuumSau != 2 && st != null) st.KhongDoiDuocAutoVacuum = true;
                }
                else if (kq.TepDangMo)
                {
                    GhiLog($"[Bảo Trì Nặng] {ten}: bỏ qua lượt này vì CSDL đang được sử dụng ({kq.ThongBao}). CSDL giữ nguyên.");
                }
                else
                {
                    GhiLog($"[Bảo Trì Nặng] {ten}: không dồn được, CSDL giữ nguyên. {kq.ThongBao}");
                }
            }
            catch (OperationCanceledException)
            {
                if (ct.IsCancellationRequested) throw;   // phần mềm đang thoát
                GhiLog("[Bảo Trì Nặng] Dồn CSDL vượt quá thời gian cho phép, đã ngắt an toàn (CSDL giữ nguyên).");
            }
            finally
            {
                if (thuMucTam != null) { try { Directory.Delete(thuMucTam, true); } catch { } }
            }
        }
        // ===================== KIỂM TRA THEO SỐ DÒNG NHẬT KÝ =====================
        private static int _dangDemDong;
        public static Task KiemTraVaVaccumTheoSoDongAsync(string dbPath) => KiemTraVaVaccumTheoSoDongAsync(dbPath, CancellationToken.None);
        public static async Task KiemTraVaVaccumTheoSoDongAsync(string dbPath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return;
            if (!BaoTriDangRanh()) return;   // đang bảo trì: không đếm thêm; mốc sẽ được phát hiện ở lần gọi sau (xem bên dưới)
            // Không để nhiều lần gọi chồng nhau đếm toàn bảng cùng lúc (hàm có thể được gọi sau mỗi lần ghi nhật ký)
            if (Interlocked.CompareExchange(ref _dangDemDong, 1, 0) != 0) return;
            try
            {
                long tongDong = await Task.Run(() => DemSoDongNhatKy(dbPath), cancellationToken).ConfigureAwait(false);
                var st = LayTrangThai(dbPath);
                if (st == null || tongDong <= 0) return;
                // Bản 2 chỉ kích hoạt khi tổng số dòng đúng bằng bội số của 1000: nếu hàm không được gọi đúng khoảnh khắc đó
                // (vd chỉ gọi khi mở form, hoặc 2 dòng được ghi cùng lúc) thì mốc bị bỏ lỡ vĩnh viễn.
                // Nay: kích hoạt khi (a) đúng bội số của 1000 và mốc này chưa xử lý, hoặc (b) đã VƯỢT qua 1 mốc kể từ lần quan sát trước.
                // Lần quan sát đầu tiên của mỗi phiên chỉ ghi nhận mốc (không kích hoạt) trừ khi đúng bội số.
                long moc = tongDong / MOC_SO_DONG;
                long truoc = Interlocked.Exchange(ref st.MocNhatKy, moc);
                bool chamMoc = (tongDong % MOC_SO_DONG == 0 && truoc != moc) || (truoc >= 0 && moc > truoc);
                if (chamMoc)
                    _ = Task.Run(() => ChayBaoTriNangNenAnToanAsync(dbPath));   // chạy nền hoàn toàn, có bắt lỗi
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi Check Vacuum 1000 dòng]: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _dangDemDong, 0);
            }
        }
        private static long DemSoDongNhatKy(string dbPath)
        {
            // Pooling=false: bản 2 dùng pool nên kết nối nhàn rỗi giữ tệp CSDL mở mãi, chặn File.Move/Delete ở các thao tác khác
            // Bảng nhật ký thật trong csdl3 là NhatKyUngDung. Bản cũ đếm bảng "NhatKy" (không tồn tại) => luôn lỗi, bị nuốt im lặng,
            // nên bảo trì theo mốc 1000 dòng chưa từng chạy. Nay tra bảng theo tên, không có thì trả 0 (không bảo trì).
            using var conn = MoKetNoi(dbPath, SqliteOpenMode.ReadOnly);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name IN ('NhatKyUngDung','NhatKy') " +
                              "ORDER BY name = 'NhatKyUngDung' DESC LIMIT 1;";
            if (cmd.ExecuteScalar() is not string tenBang) return 0;
            cmd.CommandText = "SELECT COUNT(*) FROM \"" + tenBang + "\";";   // tenBang chỉ là 1 trong 2 hằng ở trên
            object? kq = cmd.ExecuteScalar();
            return kq == null || kq == DBNull.Value ? 0 : Convert.ToInt64(kq, CultureInfo.InvariantCulture);
        }
        private static async Task ChayBaoTriNangNenAnToanAsync(string dbPath)
        {
            try
            {
                await ChayBaoTriNangAsync(dbPath, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi bảo trì nền]: {ex.Message}");
            }
        }
        // ===================== XẢ LOG LẦN CUỐI KHI THOÁT =====================
        public static void FlushFinalLogs()
        {
            // Không bao giờ được ném lỗi ở đây: hàm này chạy lúc đóng ứng dụng
            try
            {
                // Chờ Worker đang chạy (nếu có) ghi xong, tối đa ~1.5 giây, rồi tự xả phần còn lại
                var sw = Stopwatch.StartNew();
                while (Volatile.Read(ref _dangXaLog) == 1 && sw.ElapsedMilliseconds < 1500)
                    Thread.Sleep(20);
                if (_hangDoiFile.IsEmpty) return;
                var sb = new StringBuilder(512);
                while (_hangDoiFile.TryDequeue(out var m))
                    sb.AppendLine(DinhDangDong(m));   // cùng định dạng có mốc thời gian như Worker
                if (sb.Length == 0) return;
                ChuyenNhatKyCuNeuCan();
                Directory.CreateDirectory(Module_NhatKyNhaPhatTrien.ThuMuc);
                File.AppendAllText(DuongDanNhatKy, sb.ToString(), Utf8KhongBom);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FlushFinalLogs] {ex.Message}");
            }
        }
    }
}