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
        /// <summary>
        /// Tên thư mục chứa bản sao lưu + nhật ký bảo trì (nằm cạnh file .exe).
        /// PHẢI nằm trong danh sách trắng thư mục gốc của Module_KhoiTaoCSDL.TuongLuaBaoVeHeThong
        /// (bản KhoiTaoCSDL v3 đã tự lấy tên từ hằng này), nếu không tường lửa sẽ xóa cả thư mục ở môi trường triển khai.
        /// </summary>
        public const string TenThuMucBackup = "CSDL_Backups";
        private const string TEN_FILE_NHAT_KY = "BaoTri_NhatKy.txt";
        private const string HAU_TO_TAM = ".tmp";
        private const long NGUONG_XOAY_VONG_LOG = 2L * 1024 * 1024;       // log > 2 MB -> chuyển thành .old.txt
        private const int TRAN_HANG_DOI_LOG = 2000;                       // chặn RAM phình nếu Worker bị kẹt
        private const int SO_LOG_MOI_LAN_XA = 256;
        private const int SO_BAN_BACKUP_GIU_LAI = 3;
        private const int SO_BAN_HONG_GIU_LAI = 2;
        private const long TRAN_DUNG_LUONG_BACKUP = 2L * 1024 * 1024 * 1024;
        private const int SO_NGAY_CUOI_THANG = 3;                         // cửa sổ sao lưu: 3 ngày cuối tháng
        private static readonly bool CHO_PHEP_BACKUP_BU = true;           // máy không mở đúng 3 ngày cuối tháng -> sao lưu bù
        private const int SO_NGAY_BACKUP_BU = 35;
        private const double NGUONG_PHAN_TRAM_PHAN_MANH = 20.0;
        private const long NGUONG_BYTE_LANG_PHI_TOI_THIEU = 2L * 1024 * 1024;   // < 2 MB rác thì không đáng VACUUM
        private const long NGUONG_WAL_BYTE = 32L * 1024 * 1024;           // WAL > 32 MB thì gộp lại
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
        private static readonly string[] HauToTepPhu = { "-wal", "-shm", "-journal" };
        private static readonly string[] HauToTepVaChinh = { "", "-wal", "-shm", "-journal" };
        private static readonly UTF8Encoding Utf8KhongBom = new UTF8Encoding(false);
        // 🛡️ ANTI-RACE: chỉ 1 tác vụ bảo trì (nhẹ hoặc nặng) tại 1 thời điểm
        private static readonly SemaphoreSlim _khoaTienTrinh = new SemaphoreSlim(1, 1);
        // Cùng gốc AppContext.BaseDirectory với Module_DanduongGPS / Module_KhoiTaoCSDL
        private static readonly string _thuMucBackup = Path.Combine(AppContext.BaseDirectory, TenThuMucBackup);
        /// <summary>Hủy khi tiến trình thoát: ngắt êm VACUUM / kiểm tra đang chạy (SQLite tự hoàn tác) thay vì để tiến trình bị giết giữa chừng.</summary>
        private static readonly CancellationTokenSource _ctsThoat = new();
        public static string ThuMucBackup => _thuMucBackup;
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
            public volatile bool DaToiUu;                  // đã áp dụng cấu hình nền (hoặc đã quyết định bỏ qua) trong phiên này
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
        /// <summary>WAL cần bộ nhớ chia sẻ giữa các tiến trình, không an toàn trên ổ đĩa mạng/UNC.</summary>
        private static bool LaDuongDanMang(string path)
        {
            try
            {
                string full = Path.GetFullPath(path);
                if (full.StartsWith(@"\\?\", StringComparison.Ordinal))
                    return full.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase);
                if (full.StartsWith(@"\\", StringComparison.Ordinal)) return true;
                string? goc = Path.GetPathRoot(full);
                return !string.IsNullOrEmpty(goc) && new DriveInfo(goc).DriveType == DriveType.Network;
            }
            catch
            {
                return false;
            }
        }
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
                        Directory.CreateDirectory(_thuMucBackup);
                        filePath = Path.Combine(_thuMucBackup, TEN_FILE_NHAT_KY);
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
        private static long KichThuocCsdlKemWal(string dbPath)
        {
            long tong = 0;
            try
            {
                var fi = new FileInfo(dbPath);
                if (fi.Exists) tong += fi.Length;
                var wal = new FileInfo(dbPath + "-wal");
                if (wal.Exists) tong += wal.Length;
            }
            catch { }
            return tong;
        }
        /// <summary>Xóa tệp nếu có (gỡ ReadOnly trước). true = tệp không còn tồn tại.</summary>
        private static bool XoaNeuCo(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.SetAttributes(path, FileAttributes.Normal);   // bản 2 không gỡ ReadOnly nên tệp chép từ nguồn chỉ-đọc không xóa được
                    File.Delete(path);
                }
                return true;
            }
            catch { return false; }
        }
        private static void XoaKemTepPhu(string dbPath)
        {
            XoaNeuCo(dbPath);
            foreach (string hau in HauToTepPhu) XoaNeuCo(dbPath + hau);
        }
        private static List<FileInfo> LayDanhSachBackup(string tenFile)
        {
            var ds = new List<FileInfo>();
            try
            {
                if (!Directory.Exists(_thuMucBackup)) return ds;
                // Tên chứa yyyy_MM (đã cố định InvariantCulture) -> sắp theo TÊN giảm dần = mới nhất trước
                // (không phụ thuộc CreationTime, vốn bị đặt lại khi chép thư mục)
                foreach (FileInfo f in new DirectoryInfo(_thuMucBackup).EnumerateFiles($"{tenFile}_Backup_*.db"))
                {
                    if (f.Name.EndsWith(".db", StringComparison.OrdinalIgnoreCase)) ds.Add(f);
                }
                ds.Sort((a, b) => string.Compare(b.Name, a.Name, StringComparison.OrdinalIgnoreCase));
            }
            catch { }
            return ds;
        }
        private static string LayDuongDanBackupThang(string tenFile, DateTime now)
            => Path.Combine(_thuMucBackup, $"{tenFile}_Backup_{Dinh(now, "yyyy_MM")}.db");
        // ===================== BƯỚC 1: KHỞI TẠO CẤU HÌNH TỐI ƯU =====================
        public static Task KhoiTaoCauHinhToiUuAsync(string dbPath) => Task.Run(() => KhoiTaoCauHinhToiUu(dbPath));
        private static void KhoiTaoCauHinhToiUu(string dbPath)
        {
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return;
            var st = LayTrangThai(dbPath);
            if (st == null || st.DaToiUu) return;   // mỗi CSDL chỉ cần áp dụng 1 lần / phiên chạy
            try
            {
                // WAL không an toàn trên ổ đĩa mạng: giữ chế độ journal mặc định (bản 2 vẫn bật được WAL ở đó vì SQLite không chặn)
                if (LaDuongDanMang(dbPath))
                {
                    GhiLog($"[Khởi tạo PRAGMA] Bỏ qua WAL cho {Path.GetFileName(dbPath)} vì CSDL nằm trên ổ đĩa mạng.");
                    st.DaToiUu = true;
                    return;
                }
                string? cheDo = ChayVoiRetry(() => ApDungCauHinhNen(dbPath), 3, CancellationToken.None);
                st.DaToiUu = true;   // đã thử xong: không thử lại và không ghi log lặp ở mỗi lần gọi (bản 2 chỉ đánh dấu khi WAL thành công)
                if (!string.Equals(cheDo, "wal", StringComparison.OrdinalIgnoreCase))
                {
                    // WAL không bật được (USB FAT32, thư mục chỉ-đọc...) -> hệ thống vẫn chạy bình thường ở chế độ cũ
                    GhiLog($"[Khởi tạo PRAGMA] Không bật được WAL cho {Path.GetFileName(dbPath)} (chế độ hiện tại: {cheDo ?? "không rõ"}).");
                }
            }
            catch (Exception ex) { GhiLog($"[Lỗi Khởi tạo PRAGMA] {ex.Message}"); }
        }
        private static string? ApDungCauHinhNen(string dbPath)
        {
            using var conn = MoKetNoi(dbPath);
            // Chỉ giữ các PRAGMA được LƯU VÀO TỆP .db (có hiệu lực cho mọi kết nối về sau):
            //  - journal_mode = WAL : ghi/đọc đồng thời, ít khóa hơn
            //  - auto_vacuum = INCREMENTAL : chỉ có tác dụng với CSDL còn trống bảng, hoặc sau 1 lần VACUUM
            // (synchronous / foreign_keys / temp_store / wal_autocheckpoint chỉ sống trong kết nối này nên đặt ở đây vô nghĩa;
            //  muốn có hiệu lực phải đặt ở nơi mở kết nối chính của ứng dụng.)
            string? cheDo;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "PRAGMA journal_mode = WAL;";
                cheDo = cmd.ExecuteScalar()?.ToString();
            }
            ChayLenh(conn, "PRAGMA auto_vacuum = INCREMENTAL;");
            return cheDo;
        }
        // ===================== BƯỚC 2: BẢO TRÌ NHẸ HÀNG NGÀY =====================
        public static Task BaoTriNheHangNgayAsync(string dbPath) => BaoTriNheHangNgayAsync(dbPath, CancellationToken.None);
        public static async Task BaoTriNheHangNgayAsync(string dbPath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return;
            try
            {
                if (!await _khoaTienTrinh.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false))
                {
                    GhiLog($"[Bảo Trì Nhẹ] Bỏ qua cho {Path.GetFileName(dbPath)} vì hệ thống đang bận.");
                    return;
                }
            }
            catch (OperationCanceledException) { return; }   // app đang tắt (bản 2 để lỗi này thoát ra khỏi hàm)
            try
            {
                using var lk = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _ctsThoat.Token);
                await Task.Run(() => ChayVoiRetry(() => { ThucThiBaoTriNhe(dbPath); return true; }, 2, lk.Token), lk.Token)
                          .ConfigureAwait(false);
                GhiLog($"[Bảo Trì Nhẹ] Hoàn tất Optimize & dọn dẹp cho {Path.GetFileName(dbPath)}");
            }
            catch (OperationCanceledException) { /* app đang tắt */ }
            catch (Exception ex) { GhiLog($"[Lỗi Bảo trì nhẹ] {ex.Message}"); }
            finally
            {
                _khoaTienTrinh.Release();
                KichHoatWorker();   // xả nốt nhật ký CSDL đã bị hoãn trong lúc bảo trì
            }
        }
        private static void ThucThiBaoTriNhe(string dbPath)
        {
            using var conn = MoKetNoi(dbPath);
            ChayLenh(conn, "PRAGMA optimize;");
            // incremental_vacuum chỉ có tác dụng khi auto_vacuum = 2 (INCREMENTAL); ở chế độ khác là lệnh rỗng
            if (DocPragmaSo(conn, "auto_vacuum") == 2)
                ChayLenh(conn, "PRAGMA incremental_vacuum(1000);");
            // WAL phình to (có kết nối đọc giữ lâu) -> gộp về tệp chính và cắt WAL
            try
            {
                var wal = new FileInfo(dbPath + "-wal");
                if (wal.Exists && wal.Length > NGUONG_WAL_BYTE)
                    ChayLenh(conn, "PRAGMA wal_checkpoint(TRUNCATE);");
            }
            catch (SqliteException) { /* đang có người đọc -> để lần sau */ }
        }
        // ===================== BƯỚC 3: BẢO TRÌ NẶNG (KÈM AUTO-RESTORE) =====================
        public static Task TuDongBaoTriNangCuoiThangAsync(string duongDanDB, CancellationToken cancellationToken = default)
            => ChayBaoTriNangAsync(duongDanDB, cancellationToken, cuongBuc: false);
        private static bool CanTaoBackupThang(string tenFile, DateTime now)
        {
            string duongDan = LayDuongDanBackupThang(tenFile, now);
            int ngayCuoi = DateTime.DaysInMonth(now.Year, now.Month);
            var dauCuaSo = new DateTime(now.Year, now.Month, ngayCuoi - SO_NGAY_CUOI_THANG + 1);
            bool trongCuaSo = now >= dauCuaSo;
            if (!File.Exists(duongDan))
            {
                if (trongCuaSo) return true;
                if (!CHO_PHEP_BACKUP_BU) return false;
                // Sao lưu bù: chưa có bản nào, hoặc bản mới nhất đã quá cũ (máy không được mở đúng 3 ngày cuối tháng)
                var moiNhat = LayDanhSachBackup(tenFile).FirstOrDefault();
                return moiNhat == null || (now - moiNhat.LastWriteTime).TotalDays > SO_NGAY_BACKUP_BU;
            }
            // Đã có bản của tháng: chỉ làm mới nếu đó là bản "bù" tạo trước cửa sổ cuối tháng
            return trongCuaSo && File.GetLastWriteTime(duongDan) < dauCuaSo;
        }
        private static async Task ChayBaoTriNangAsync(string duongDanDB, CancellationToken cancellationToken, bool cuongBuc)
        {
            if (string.IsNullOrWhiteSpace(duongDanDB) || !File.Exists(duongDanDB)) return;
            string tenFile = Path.GetFileNameWithoutExtension(duongDanDB);
            // Smart Check ngoài khóa (rẻ, không đụng CSDL)
            if (!cuongBuc && !CanTaoBackupThang(tenFile, DateTime.Now)) return;
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
                        () => BaoTriNangDongBo(duongDanDB, tenFile, cuongBuc, lk.Token),
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
        private static void BaoTriNangDongBo(string duongDanDB, string tenFile, bool cuongBuc, CancellationToken ct)
        {
            // Kiểm tra lại sau khi giành được khóa (lượt khác có thể vừa làm xong)
            DateTime now = DateTime.Now;
            bool canBackup = CanTaoBackupThang(tenFile, now);
            if (!canBackup && !cuongBuc) return;
            Directory.CreateDirectory(_thuMucBackup);
            GhiLog($"[Bảo Trì Nặng] Đang xử lý: {tenFile}...");
            // 1. INTEGRITY CHECK
            // 🛡️ CORRUPTION RECOVERY (Auto-Restore)
            if (!CsdlKhoeManh(duongDanDB, ct))
            {
                PhucHoiTuBackup(duongDanDB, tenFile, now, ct);
                return;   // Khôi phục xong (hoặc không thể) thì thoát, không Vacuum nữa.
            }
            // 2. ONLINE BACKUP (ghi ra .tmp -> kiểm tra -> đổi tên nguyên tử)
            bool coBackup = !canBackup;   // đã có bản của tháng này thì coi như đã an toàn
            if (canBackup)
                coBackup = SaoLuuAnToan(duongDanDB, LayDuongDanBackupThang(tenFile, now), ct);
            if (!coBackup)
            {
                GhiLog($"[Bảo Trì Nặng] Sao lưu {tenFile} không thành công, bỏ qua VACUUM và dọn dẹp.");
                return;
            }
            // 3. FULL VACUUM (chỉ khi DB khỏe, có backup, và thực sự nhiều rác / cần chuyển sang INCREMENTAL)
            if (CanFullVacuum(duongDanDB, ct))
                ThucHienFullVacuum(duongDanDB, ct);
            // 4. CLEANUP (giữ tối đa 3 bản VÀ dung lượng tổng dưới 2GB, không bao giờ xóa bản mới nhất)
            DondepBackupCu(tenFile, SO_BAN_BACKUP_GIU_LAI, TRAN_DUNG_LUONG_BACKUP);
        }
        /// <summary>
        /// Hỏng thật sự khi integrity_check != ok hoặc SQLite báo CORRUPT / NOTADB. BUSY không bao giờ bị coi là hỏng.
        /// Phục hồi sẽ thay thế CSDL bằng bản sao lưu (có thể cũ tới 1 tháng) nên kết luận "hỏng" phải được xác nhận 2 lần cách nhau 1,5 giây,
        /// loại trừ lỗi đọc thoáng qua (phần mềm diệt virus quét tệp, ổ đĩa chập chờn).
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
        private static bool PhucHoiTuBackup(string duongDanDB, string tenFile, DateTime now, CancellationToken ct)
        {
            GhiLog($"[NGUY HIỂM] {tenFile} bị Corrupt! Tiến hành tìm bản sao lưu hợp lệ để phục hồi.");
            string tam = Path.Combine(_thuMucBackup, $"{tenFile}_Restore{HAU_TO_TAM}");
            string? tenBanDung = null;
            try
            {
                // 1. Chọn bản sao lưu MỚI NHẤT mà bản thân nó còn toàn vẹn (chưa đụng tới DB đang chạy)
                foreach (FileInfo f in LayDanhSachBackup(tenFile))
                {
                    ct.ThrowIfCancellationRequested();
                    XoaKemTepPhu(tam);
                    File.Copy(f.FullName, tam, true);
                    bool tot;
                    try { tot = KiemTraToanVen(tam, nhanh: false, ct); }
                    catch (SqliteException ex) { GhiLog($"[Phục Hồi] Không kiểm tra được {f.Name}: {ex.Message}"); tot = false; }
                    if (tot) { tenBanDung = f.Name; break; }
                    GhiLog($"[Phục Hồi] Bản {f.Name} cũng không toàn vẹn, thử bản cũ hơn...");
                }
                if (tenBanDung == null)
                {
                    // Không có bản thay thế -> GIỮ NGUYÊN tệp hiện tại (không cách ly), tránh để ứng dụng chạy với CSDL trống
                    GhiLog($"[CẢNH BÁO ĐỎ] Không có bản sao lưu hợp lệ để phục hồi cho {tenFile}! Giữ nguyên tệp hiện tại, cần xử lý thủ công.");
                    return false;
                }
                // ---- ĐOẠN GĂNG: từ đây không kiểm tra hủy, để không bỏ dở giữa chừng việc thay thế CSDL ----
                // 2. Nhả mọi kết nối đang được giữ trong pool (nếu không Windows sẽ báo "tệp đang được sử dụng")
                SqliteConnection.ClearAllPools();
                // 3. Cách ly tệp hỏng (kèm -wal/-shm/-journal: WAL cũ mà dính vào bản mới sẽ làm hỏng chính bản vừa khôi phục) rồi đặt bản tốt vào chỗ
                string corruptPath = Path.Combine(_thuMucBackup, $"{tenFile}_CORRUPTED_{Dinh(now, "yyyyMMdd_HHmmss")}.db");
                var daChuyen = new List<(string Tu, string Den)>(4);
                try
                {
                    foreach (string hau in HauToTepVaChinh)
                    {
                        string nguon = duongDanDB + hau;
                        if (!File.Exists(nguon)) continue;
                        string dich = corruptPath + hau;
                        File.Move(nguon, dich);
                        daChuyen.Add((nguon, dich));
                    }
                    File.Move(tam, duongDanDB);
                }
                catch
                {
                    // Hoàn tác: trả lại đúng trạng thái ban đầu, không để mất CSDL
                    for (int i = daChuyen.Count - 1; i >= 0; i--)
                    {
                        try { File.Move(daChuyen[i].Den, daChuyen[i].Tu); } catch { }
                    }
                    throw;
                }
                GhiLog($"[Phục Hồi] Đã khôi phục thành công từ: {tenBanDung}. Bản hỏng được lưu ở {Path.GetFileName(corruptPath)}. Hủy Vacuum.");
                // 4. Bản sao lưu ở chế độ journal thường -> bật lại WAL; dọn bớt bản hỏng cũ; báo cho giao diện nạp lại dữ liệu
                var st = LayTrangThai(duongDanDB);
                if (st != null) st.DaToiUu = false;
                KhoiTaoCauHinhToiUu(duongDanDB);
                DondepBanHongCu(tenFile, SO_BAN_HONG_GIU_LAI);
                ThongBaoCsdlThayDoi();
                return true;
            }
            finally
            {
                XoaKemTepPhu(tam);
            }
        }
        /// <summary>Gọi sự kiện cho từng người nghe riêng: 1 người nghe lỗi không làm hỏng các người còn lại. LƯU Ý: chạy trên luồng nền, người nghe tự chuyển về luồng giao diện nếu cần.</summary>
        private static void ThongBaoCsdlThayDoi()
        {
            var h = Module_DanduongGPS.OnDatabaseChanged;
            if (h == null) return;
            foreach (Delegate d in h.GetInvocationList())
            {
                try { ((Action)d)(); }
                catch (Exception ex) { Debug.WriteLine($"[Trình Bảo Trì] OnDatabaseChanged: {ex.Message}"); }
            }
        }
        /// <summary>
        /// Sao lưu ra tệp .tmp -> journal_mode=DELETE (bản sao là 1 tệp tự đủ) -> quick_check -> Move thành tên chính thức.
        /// Sập nguồn giữa chừng không để lại "bản sao lưu" dở dang mà lần sau tưởng là hợp lệ.
        /// </summary>
        private static bool SaoLuuAnToan(string dbGoc, string dbBackup, CancellationToken ct)
        {
            string tam = dbBackup + HAU_TO_TAM;
            try
            {
                ct.ThrowIfCancellationRequested();
                // Không đủ chỗ trống thì thôi, tránh làm đầy ổ đĩa trên máy cấu hình thấp
                long canByte = KichThuocCsdlKemWal(dbGoc);
                canByte += canByte / 10 + DU_PHONG_BYTE;
                if (!DuDungLuong(_thuMucBackup, canByte))
                {
                    GhiLog($"[Backup] Bỏ qua {Path.GetFileName(dbGoc)}: ổ đĩa không đủ dung lượng trống.");
                    return false;
                }
                XoaKemTepPhu(tam);
                using (var ketNoiGoc = MoKetNoi(dbGoc))
                using (var ketNoiBackup = MoKetNoi(tam, SqliteOpenMode.ReadWriteCreate))
                {
                    ketNoiGoc.BackupDatabase(ketNoiBackup);
                    ct.ThrowIfCancellationRequested();
                    HoanThienVaKiemTraBackup(ketNoiBackup);
                }
                ct.ThrowIfCancellationRequested();
                File.Move(tam, dbBackup, overwrite: true);
                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                GhiLog($"[Lỗi Backup] {Path.GetFileName(dbGoc)}: {ex.Message}");
                return false;
            }
            finally
            {
                XoaKemTepPhu(tam);   // bản 2 chỉ xóa .tmp, bỏ sót -wal/-shm/-journal
            }
        }
        private static void HoanThienVaKiemTraBackup(SqliteConnection ketNoiBackup)
        {
            ChayLenh(ketNoiBackup, "PRAGMA journal_mode = DELETE;");
            using var cmd = ketNoiBackup.CreateCommand();
            cmd.CommandText = "PRAGMA quick_check(1);";
            string? kq = cmd.ExecuteScalar()?.ToString();
            if (!string.Equals(kq, "ok", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Bản sao lưu vừa tạo không vượt qua quick_check: " + kq);
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
                // VACUUM cần ~ 1 lần dung lượng DB ở thư mục tạm (ổ hệ thống) và ~ 2 lần ở ổ chứa DB (WAL)
                long kichThuoc = pageCount * pageSize;
                string thuMucDb = Path.GetDirectoryName(Path.GetFullPath(dbPath)) ?? dbPath;
                if (!DuDungLuong(thuMucDb, kichThuoc * 2 + DU_PHONG_BYTE) ||
                    !DuDungLuong(Path.GetTempPath(), kichThuoc + DU_PHONG_BYTE))
                {
                    GhiLog("[Kiểm Tra Vacuum] Bỏ qua VACUUM: không đủ dung lượng trống.");
                    return false;
                }
                return true;
            }, 3, ct);
        }
        private static void ThucHienFullVacuum(string dbPath, CancellationToken ct)
        {
            var st = LayTrangThai(dbPath);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(ThoiGianToiDaVacuum);   // timeout THẬT: kích hoạt sqlite3_interrupt (bản 2 không có giới hạn thời gian)
            try
            {
                bool daIncremental = ChayVoiRetry(() =>
                {
                    using var conn = MoKetNoi(dbPath);
                    using var reg = DangKyNgat(conn, cts.Token);
                    ChayLenh(conn, "PRAGMA wal_checkpoint(TRUNCATE);");
                    // Đặt TRƯỚC khi VACUUM, trên CÙNG kết nối: đây là cách duy nhất để CSDL cũ chuyển sang INCREMENTAL
                    ChayLenh(conn, "PRAGMA auto_vacuum = INCREMENTAL;");
                    ChayLenh(conn, "VACUUM;");
                    return DocPragmaSo(conn, "auto_vacuum") == 2;
                }, 3, ct);
                if (!daIncremental && st != null) st.KhongDoiDuocAutoVacuum = true;
                GhiLog("[Bảo Trì Nặng] Full VACUUM hoàn tất.");
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == SQLITE_INTERRUPT)
            {
                if (ct.IsCancellationRequested) throw new OperationCanceledException(ct);
                GhiLog("[Bảo Trì Nặng] VACUUM vượt quá thời gian cho phép, đã ngắt an toàn (CSDL được hoàn tác, giữ nguyên).");
            }
        }
        // 🛡️ Dọn dẹp backup theo Số lượng VÀ Dung lượng; KHÔNG BAO GIỜ xóa bản mới nhất
        private static void DondepBackupCu(string tenFileGoc, int soLuongGiuLai, long maxSizeBytes)
        {
            try
            {
                var files = LayDanhSachBackup(tenFileGoc);   // mới nhất ở đầu danh sách
                long tong = files.Sum(f => f.Length);
                for (int i = files.Count - 1; i >= 1; i--)   // từ cũ nhất -> mới dần, bỏ qua files[0]
                {
                    bool vuotSoLuong = i >= soLuongGiuLai;
                    if (!vuotSoLuong && tong <= maxSizeBytes) break;   // đạt mức an toàn thì dừng
                    long len = files[i].Length;
                    if (XoaNeuCo(files[i].FullName)) tong -= len;
                    else Debug.WriteLine($"[Dọn backup] Không xóa được {files[i].Name}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Dọn backup] {ex.Message}");
            }
        }
        private static void DondepBanHongCu(string tenFileGoc, int soLuongGiuLai)
        {
            try
            {
                if (!Directory.Exists(_thuMucBackup)) return;
                var hong = new List<string>();
                foreach (string f in Directory.EnumerateFiles(_thuMucBackup, $"{tenFileGoc}_CORRUPTED_*.db"))
                {
                    if (f.EndsWith(".db", StringComparison.OrdinalIgnoreCase)) hong.Add(f);
                }
                hong.Sort((a, b) => string.Compare(Path.GetFileName(b), Path.GetFileName(a), StringComparison.OrdinalIgnoreCase));
                for (int i = soLuongGiuLai; i < hong.Count; i++)
                    XoaKemTepPhu(hong[i]);
            }
            catch { }
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
            using var conn = MoKetNoi(dbPath);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM NhatKy"; // Hoặc tên bảng bạn cần kiểm tra
            object? kq = cmd.ExecuteScalar();
            return kq == null || kq == DBNull.Value ? 0 : Convert.ToInt64(kq, CultureInfo.InvariantCulture);
        }
        private static async Task ChayBaoTriNangNenAnToanAsync(string dbPath)
        {
            try
            {
                // cuongBuc: bỏ qua rào "3 ngày cuối tháng" cho mốc số dòng
                await ChayBaoTriNangAsync(dbPath, CancellationToken.None, cuongBuc: true).ConfigureAwait(false);
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
                Directory.CreateDirectory(_thuMucBackup);
                File.AppendAllText(Path.Combine(_thuMucBackup, TEN_FILE_NHAT_KY), sb.ToString(), Utf8KhongBom);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FlushFinalLogs] {ex.Message}");
            }
        }
    }
}