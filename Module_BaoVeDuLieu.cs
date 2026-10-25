//using Microsoft.Data.Sqlite;
//using System.Collections.Concurrent;
//using System.Diagnostics;
//namespace PhanMemThiDua2026
//{
//    public static class Module_BaoVeDuLieu
//    {
//        //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
//        //Đây giống như là Firewall - Lớp bảo vệ ở bên trong ứng dụng - Database Firewall
//        // 🛡️ ANTI-RACE: Khóa luồng đồng bộ
//        private static readonly SemaphoreSlim _khoaTienTrinh = new SemaphoreSlim(1, 1);
//        private static readonly string _thuMucBackup = Path.Combine(Application.StartupPath, "CSDL_Backups");
//        // 🛡️ CHUẨN CHUYÊN GIA 1: Tối ưu Connection String (Thêm Pooling và Timeout)
//        private static string MoLoiChoEmTaoChuoiKetNoi(string dbPath, bool usePooling = true)
//            => $"Data Source={dbPath};Pooling={usePooling};Default Timeout=30;";
//        // 🚀 HỆ THỐNG GHI LOG BẤT ĐỒNG BỘ CẤP ĐỘ DOANH NGHIỆP (DEDICATED WORKER)      
//        private static readonly ConcurrentQueue<string> _logFileQueue = new ConcurrentQueue<string>();
//        private static int _isFlushingLog = 0;
//        private static void ToQuocGoiTenAnhGhiLogBaoTri(string thongDiep)
//        {
//            try
//            {
//                // Chỉ đẩy vào Queue RAM cực nhanh, tuyệt đối KHÔNG TẠO Task.Run (fire-and-forget) ở đây nữa.
//                _logFileQueue.Enqueue(thongDiep);
//                Debug.WriteLine($"[Trình Bảo Trì] {thongDiep}");
//                // Kích hoạt Worker xả Queue (chỉ 1 Worker chạy tại 1 thời điểm)
//                if (Interlocked.CompareExchange(ref _isFlushingLog, 1, 0) == 0)
//                {
//                    _ = Task.Run(FlushLogWorkerAsync);
//                }
//            }
//            catch { }
//        }
//        private static async Task FlushLogWorkerAsync()
//        {
//            try
//            {
//                if (!Directory.Exists(_thuMucBackup)) Directory.CreateDirectory(_thuMucBackup);
//                string filePath = Path.Combine(_thuMucBackup, "BaoTri_NhatKy.txt");
//                var batchThongDiep = new List<string>();
//                using (var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, useAsync: true))
//                using (var writer = new StreamWriter(stream))
//                {
//                    // Xả sạch Queue vào File I/O
//                    while (_logFileQueue.TryDequeue(out string thongDiep))
//                    {
//                        batchThongDiep.Add(thongDiep);
//                        await writer.WriteLineAsync($"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] {thongDiep}");
//                    }
//                }
//                // Sau khi ghi file xong mới tiến hành ghi DB theo dạng Batch, tránh Lock kéo dài
//                foreach (var thongDiep in batchThongDiep)
//                {
//                    try
//                    {
//                        string tenNhap = "System (Trình Bảo Trì CSDL)";
//                        string thongDiepNhatKy = thongDiep;
//                        // 🔹 Đã sử dụng chính xác lệnh gọi mà bạn yêu cầu
//                        Module_NhatKy.GhiNhatKy(
//                             taiKhoan: tenNhap,
//                             hanhDong: thongDiepNhatKy,
//                             ghiChu: $"Thời gian: {SessionInfo.ThoiGianDangNhap:dd-MM-yyyy HH:mm:ss}"
//                        );
//                    }
//                    catch { /* Bỏ qua lỗi ghi DB để Worker không bị chết */ }
//                }
//            }
//            catch { }
//            finally
//            {
//                // Mở khóa Worker
//                Interlocked.Exchange(ref _isFlushingLog, 0);
//                // Double-Check Lock: Tránh sót log rơi vào queue đúng lúc mở khóa
//                if (!_logFileQueue.IsEmpty && Interlocked.CompareExchange(ref _isFlushingLog, 1, 0) == 0)
//                {
//                    _ = Task.Run(FlushLogWorkerAsync);
//                }
//            }
//        }
//        // 🚀 RETRY ENGINE (CHỐNG SQLITE_BUSY / SQLITE_LOCKED)   
//        private static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, int maxRetries = 3, CancellationToken cancellationToken = default)
//        {
//            int delayMs = 1000;
//            for (int i = 0; i < maxRetries; i++)
//            {
//                cancellationToken.ThrowIfCancellationRequested();
//                try
//                {
//                    return await action();
//                }
//                catch (SqliteException ex) when (ex.SqliteErrorCode == 5 || ex.SqliteErrorCode == 6)
//                {
//                    if (i == maxRetries - 1) throw;
//                    ToQuocGoiTenAnhGhiLogBaoTri($"[SQLite Busy] DB đang bận, thử lại lần {i + 1} sau {delayMs}ms...");
//                    try
//                    {
//                        await Task.Delay(delayMs, cancellationToken);
//                    }
//                    catch (TaskCanceledException)
//                    {
//                        throw new OperationCanceledException("Quá trình chờ bị hủy do hệ thống yêu cầu dừng.");
//                    }
//                    delayMs += 500; // Exponential Backoff
//                }
//            }
//            return default;
//        }
//        private static async Task ExecuteWithRetryAsync(Func<Task> action, int maxRetries = 3, CancellationToken cancellationToken = default)
//        {
//            await ExecuteWithRetryAsync<bool>(async () => { await action(); return true; }, maxRetries, cancellationToken);
//        }
//        // 🚀 BƯỚC 1: KHỞI TẠO CẤU HÌNH TỐI ƯU    
//        public static async Task KhoiTaoCauHinhToiUuAsync(string dbPath)
//        {
//            if (!File.Exists(dbPath)) return;
//            try
//            {
//                await ExecuteWithRetryAsync(async () =>
//                {
//                    using var conn = new SqliteConnection(MoLoiChoEmTaoChuoiKetNoi(dbPath));
//                    await conn.OpenAsync();
//                    using var cmd = conn.CreateCommand();
//                    // 🛡️ CHUẨN ENTERPRISE: Bổ sung wal_autocheckpoint để WAL không bị phình to
//                    cmd.CommandText = @"
//                        PRAGMA journal_mode = WAL;
//                        PRAGMA synchronous = NORMAL;
//                        PRAGMA temp_store = MEMORY;
//                        PRAGMA foreign_keys = ON;
//                        PRAGMA auto_vacuum = INCREMENTAL;
//                        PRAGMA wal_autocheckpoint = 1000;";
//                    await cmd.ExecuteNonQueryAsync();
//                });
//            }
//            catch (Exception ex) { ToQuocGoiTenAnhGhiLogBaoTri($"[Lỗi Khởi tạo PRAGMA] {ex.Message}"); }
//        }
//        // 🚀 BƯỚC 2: BẢO TRÌ NHẸ HÀNG NGÀY      
//        public static async Task BaoTriNheHangNgayAsync(string dbPath)
//        {
//            if (!File.Exists(dbPath)) return;
//            if (!await _khoaTienTrinh.WaitAsync(TimeSpan.FromSeconds(5)))
//            {
//                ToQuocGoiTenAnhGhiLogBaoTri($"[Bảo Trì Nhẹ] Bỏ qua cho {Path.GetFileName(dbPath)} vì hệ thống đang bận.");
//                return;
//            }
//            try
//            {
//                await ExecuteWithRetryAsync(async () =>
//                {
//                    using var conn = new SqliteConnection(MoLoiChoEmTaoChuoiKetNoi(dbPath));
//                    await conn.OpenAsync();
//                    using (var cmd = conn.CreateCommand())
//                    {
//                        cmd.CommandText = @"
//                            PRAGMA optimize;
//                            PRAGMA incremental_vacuum(1000);";
//                        await cmd.ExecuteNonQueryAsync();
//                    }
//                }, maxRetries: 2);
//                ToQuocGoiTenAnhGhiLogBaoTri($"[Bảo Trì Nhẹ] Hoàn tất Optimize & Incremental Vacuum cho {Path.GetFileName(dbPath)}");
//            }
//            catch (Exception ex) { ToQuocGoiTenAnhGhiLogBaoTri($"[Lỗi Bảo trì nhẹ] {ex.Message}"); }
//            finally { _khoaTienTrinh.Release(); }
//        }
//        // 🚀 BƯỚC 3: BẢO TRÌ NẶNG CUỐI THÁNG (KÈM AUTO-RESTORE VÀ TIMEOUT)      
//        public static async Task TuDongBaoTriNangCuoiThangAsync(string duongDanDB, CancellationToken cancellationToken = default)
//        {
//            if (string.IsNullOrWhiteSpace(duongDanDB) || !File.Exists(duongDanDB)) return;
//            string tenFile = Path.GetFileNameWithoutExtension(duongDanDB);
//            string thangHienTai = DateTime.Now.ToString("yyyy_MM");
//            string duongDanBackup = Path.Combine(_thuMucBackup, $"{tenFile}_Backup_{thangHienTai}.db");
//            // Smart Check
//            if (File.Exists(duongDanBackup)) return;
//            if (DateTime.Now.Day < DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month) - 2) return;
//            await _khoaTienTrinh.WaitAsync(cancellationToken);
//            try
//            {
//                if (File.Exists(duongDanBackup)) return;
//                if (!Directory.Exists(_thuMucBackup)) Directory.CreateDirectory(_thuMucBackup);
//                ToQuocGoiTenAnhGhiLogBaoTri($"[Bảo Trì Nặng] Đang xử lý: {tenFile}...");
//                // 1. INTEGRITY CHECK
//                bool isHealthy = await ExecuteWithRetryAsync(async () =>
//                {
//                    using var conn = new SqliteConnection(MoLoiChoEmTaoChuoiKetNoi(duongDanDB));
//                    await conn.OpenAsync(cancellationToken);
//                    using var cmd = conn.CreateCommand();
//                    cmd.CommandText = "PRAGMA integrity_check;";
//                    var result = await cmd.ExecuteScalarAsync(cancellationToken);
//                    return result?.ToString()?.Equals("ok", StringComparison.OrdinalIgnoreCase) == true;
//                });
//                // 🛡️ CHUẨN ENTERPRISE: CORRUPTION RECOVERY (Auto-Restore)
//                if (!isHealthy)
//                {
//                    ToQuocGoiTenAnhGhiLogBaoTri($"[NGUY HIỂM] {tenFile} bị Corrupt! Tiến hành cách ly và tự động phục hồi.");
//                    // Cách ly file hỏng
//                    string corruptPath = Path.Combine(_thuMucBackup, $"{tenFile}_CORRUPTED_{DateTime.Now:yyyyMMdd_HHmmss}.db");
//                    File.Move(duongDanDB, corruptPath);
//                    // Tìm bản backup an toàn gần nhất
//                    var lastBackup = new DirectoryInfo(_thuMucBackup)
//                        .GetFiles($"{tenFile}_Backup_*.db")
//                        .OrderByDescending(f => f.CreationTime)
//                        .FirstOrDefault();
//                    if (lastBackup != null)
//                    {
//                        File.Copy(lastBackup.FullName, duongDanDB, true);
//                        ToQuocGoiTenAnhGhiLogBaoTri($"[Phục Hồi] Đã khôi phục thành công từ: {lastBackup.Name}. Hủy Vacuum.");
//                        return; // Khôi phục xong thì thoát, không Vacuum nữa.
//                    }
//                    else
//                    {
//                        ToQuocGoiTenAnhGhiLogBaoTri($"[CẢNH BÁO ĐỎ] Không có bản sao lưu nào để phục hồi cho {tenFile}!");
//                        return;
//                    }
//                }
//                // 2. ONLINE BACKUP
//                bool backupThanhCong = await Task.Run(() => ThucThiBackupAnToan(duongDanDB, duongDanBackup, cancellationToken), cancellationToken);
//                // 3. FULL VACUUM (Chỉ làm khi DB khỏe và thực sự nhiều rác)
//                if (backupThanhCong && isHealthy)
//                {
//                    bool canFullVacuum = await KiemTraCanFullVacuumAsync(duongDanDB, cancellationToken);
//                    if (canFullVacuum)
//                    {
//                        await ExecuteWithRetryAsync(async () =>
//                        {
//                            using var conn = new SqliteConnection(MoLoiChoEmTaoChuoiKetNoi(duongDanDB));
//                            await conn.OpenAsync(cancellationToken);
//                            using (var cmd = conn.CreateCommand())
//                            {
//                                cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
//                                await cmd.ExecuteNonQueryAsync(cancellationToken);
//                            }
//                            // 🛡️ CHUẨN ENTERPRISE: Timeout 10 phút riêng cho Vacuum để tránh kẹt
//                            using var vacuumCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
//                            vacuumCts.CancelAfter(TimeSpan.FromMinutes(10));
//                            using (var cmd = conn.CreateCommand())
//                            {
//                                cmd.CommandText = "VACUUM;";
//                                await cmd.ExecuteNonQueryAsync(vacuumCts.Token);
//                            }
//                        });
//                        ToQuocGoiTenAnhGhiLogBaoTri($"[Bảo Trì Nặng] Full VACUUM hoàn tất.");
//                    }
//                    // 4. CLEANUP (Giữ tối đa 3 bản VÀ dung lượng tổng dưới 2GB)
//                    DondepBackupCu(tenFile, 3, maxSizeBytes: 2L * 1024 * 1024 * 1024);
//                }
//            }
//            catch (OperationCanceledException) { ToQuocGoiTenAnhGhiLogBaoTri("[Bảo Trì Nặng] Đã hủy tiến trình (Timeout hoặc User tắt App)."); }
//            catch (Exception ex) { ToQuocGoiTenAnhGhiLogBaoTri($"[Lỗi Bảo trì nặng] {ex.Message}"); }
//            finally { _khoaTienTrinh.Release(); }
//        }
//        private static bool ThucThiBackupAnToan(string dbGoc, string dbBackup, CancellationToken token)
//        {
//            try
//            {
//                using var ketNoiGoc = new SqliteConnection(MoLoiChoEmTaoChuoiKetNoi(dbGoc));
//                using var ketNoiBackup = new SqliteConnection(MoLoiChoEmTaoChuoiKetNoi(dbBackup));
//                ketNoiGoc.Open();
//                ketNoiBackup.Open();
//                token.ThrowIfCancellationRequested();
//                ketNoiGoc.BackupDatabase(ketNoiBackup);
//                return true;
//            }
//            catch
//            {
//                if (File.Exists(dbBackup)) File.Delete(dbBackup);
//                return false;
//            }
//        }
//        private static async Task<bool> KiemTraCanFullVacuumAsync(string dbPath, CancellationToken token)
//        {
//            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return false;
//            return await ExecuteWithRetryAsync(async () =>
//            {
//                token.ThrowIfCancellationRequested();
//                using var conn = new SqliteConnection(MoLoiChoEmTaoChuoiKetNoi(dbPath));
//                await conn.OpenAsync(token);
//                long pageCount;
//                using (var cmdPage = conn.CreateCommand())
//                {
//                    cmdPage.CommandText = "PRAGMA page_count;";
//                    object result = await cmdPage.ExecuteScalarAsync(token);
//                    if (result == null || result == DBNull.Value) return false;
//                    pageCount = Convert.ToInt64(result);
//                }
//                if (pageCount <= 0) return false;
//                long freeCount;
//                using (var cmdFree = conn.CreateCommand())
//                {
//                    cmdFree.CommandText = "PRAGMA freelist_count;";
//                    object result = await cmdFree.ExecuteScalarAsync(token);
//                    if (result == null || result == DBNull.Value) return false;
//                    freeCount = Convert.ToInt64(result);
//                }
//                double fragmentationPercent = (freeCount * 100.0) / pageCount;
//                ToQuocGoiTenAnhGhiLogBaoTri($"[Kiểm Tra Vacuum] PageCount={pageCount:N0}, FreeList={freeCount:N0}, Fragment={fragmentationPercent:F2}%");
//                return fragmentationPercent > 20.0;
//            }, 3, token);
//        }
//        // 🛡️ CHUẨN ENTERPRISE: Dọn dẹp backup không chỉ theo số lượng (Count) mà còn theo dung lượng (Size)
//        private static void DondepBackupCu(string tenFileGoc, int soLuongGiuLai, long maxSizeBytes)
//        {
//            try
//            {
//                var files = new DirectoryInfo(_thuMucBackup)
//                    .GetFiles($"{tenFileGoc}_Backup_*.db")
//                    .OrderByDescending(f => f.CreationTime)
//                    .ToList();
//                long currentTotalSize = files.Sum(f => f.Length);
//                // Ưu tiên 1: Xóa các file cũ vượt quá số lượng cho phép (Ví dụ > 3 bản)
//                foreach (var file in files.Skip(soLuongGiuLai))
//                {
//                    currentTotalSize -= file.Length;
//                    file.Delete();
//                }
//                // Ưu tiên 2: Nếu 3 bản còn lại nhưng tổng dung lượng > 2GB -> Xóa dần từ file cũ nhất
//                var remainingFiles = files.Take(soLuongGiuLai).OrderBy(f => f.CreationTime).ToList();
//                foreach (var file in remainingFiles)
//                {
//                    if (currentTotalSize <= maxSizeBytes) break; // Đạt mức an toàn thì dừng
//                    currentTotalSize -= file.Length;
//                    file.Delete();
//                }
//            }
//            catch { }
//        }
//        // Thêm vào trong Module_BaoVeDuLieu
//        public static async Task KiemTraVaVaccumTheoSoDongAsync(string dbPath)
//        {
//            if (!File.Exists(dbPath)) return;
//            try
//            {
//                int tongDong = 0;
//                using (var conn = new SqliteConnection($"Data Source={dbPath}"))
//                {
//                    await conn.OpenAsync();
//                    using var cmd = conn.CreateCommand();
//                    cmd.CommandText = "SELECT COUNT(*) FROM NhatKy"; // Hoặc tên bảng bạn cần kiểm tra
//                    tongDong = Convert.ToInt32(await cmd.ExecuteScalarAsync());
//                }
//                // Logic: Nếu tổng dòng là bội số của 1000 (1000, 2000, 3000...)
//                // Và phải lớn hơn 0 để tránh chạy lúc DB rỗng
//                if (tongDong > 0 && tongDong % 1000 == 0)
//                {
//                    // Gọi hàm bảo trì nặng (Full Vacuum + Integrity Check) đã viết ở turn trước
//                    // Chúng ta dùng Task.Run để nó chạy ngầm hoàn toàn, không block Form Nhật Ký
//                    _ = TuDongBaoTriNangCuoiThangAsync(dbPath);
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Lỗi Check Vacuum 1000 dòng]: {ex.Message}");
//            }
//        }
//        public static void FlushFinalLogs()
//        {
//            // Đợi Worker ghi nốt những gì còn sót lại trong Queue trước khi đóng hẳn Process
//            while (!_logFileQueue.IsEmpty)
//            {
//                // Chạy đồng bộ (Sync) lần cuối cùng để đảm bảo an toàn dữ liệu
//                if (_logFileQueue.TryDequeue(out string msg))
//                {
//                    File.AppendAllText(Path.Combine(_thuMucBackup, "BaoTri_NhatKy.txt"), msg + Environment.NewLine);
//                }
//            }
//        }
//    }
//}
///Phiên bản 2
///
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PhanMemThiDua2026
{
    public static class Module_BaoVeDuLieu
    {
        //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
        //Đây giống như là Firewall - Lớp bảo vệ ở bên trong ứng dụng - Database Firewall

        // ===================== HẰNG SỐ CẤU HÌNH =====================
        /// <summary>
        /// Tên thư mục chứa bản sao lưu + nhật ký bảo trì (nằm cạnh file .exe).
        /// PHẢI nằm trong danh sách trắng thư mục gốc của Module_KhoiTaoCSDL.TuongLuaBaoVeHeThong,
        /// nếu không tường lửa sẽ xóa cả thư mục này ở môi trường triển khai.
        /// </summary>
        public const string TenThuMucBackup = "CSDL_Backups";

        private const string TEN_FILE_NHAT_KY = "BaoTri_NhatKy.txt";
        private const long NGUONG_XOAY_VONG_LOG = 2L * 1024 * 1024;       // log > 2 MB -> chuyển thành .old
        private const int TRAN_HANG_DOI_LOG = 2000;                       // chặn RAM phình nếu Worker bị kẹt
        private const int SO_LOG_MOI_LAN_XA = 256;
        private const int SO_BAN_BACKUP_GIU_LAI = 3;
        private const int SO_BAN_HONG_GIU_LAI = 2;
        private const long TRAN_DUNG_LUONG_BACKUP = 2L * 1024 * 1024 * 1024;
        private const int SO_NGAY_CUOI_THANG = 3;                         // cửa sổ sao lưu: 3 ngày cuối tháng
        private static readonly bool CHO_PHEP_BACKUP_BU = true;                    // máy không mở đúng 3 ngày cuối tháng -> sao lưu bù
        private const int SO_NGAY_BACKUP_BU = 35;
        private const double NGUONG_PHAN_TRAM_PHAN_MANH = 20.0;
        private const long NGUONG_BYTE_LANG_PHI_TOI_THIEU = 2L * 1024 * 1024;   // < 2 MB rác thì không đáng VACUUM
        private const long NGUONG_WAL_BYTE = 32L * 1024 * 1024;           // WAL > 32 MB thì gộp lại

        private const int SQLITE_BUSY = 5;
        private const int SQLITE_LOCKED = 6;
        private const int SQLITE_CORRUPT = 11;
        private const int SQLITE_NOTADB = 26;

        private static readonly string[] HauToTepPhu = { "-wal", "-shm", "-journal" };
        private static readonly UTF8Encoding Utf8KhongBom = new UTF8Encoding(false);

        // 🛡️ ANTI-RACE: Khóa luồng đồng bộ
        private static readonly SemaphoreSlim _khoaTienTrinh = new SemaphoreSlim(1, 1);
        // Cùng gốc AppContext.BaseDirectory với Module_DanduongGPS / Module_KhoiTaoCSDL (không dùng Application.StartupPath nữa)
        private static readonly string _thuMucBackup = Path.Combine(AppContext.BaseDirectory, TenThuMucBackup);

        private enum TinhTrangDb { Tot, Hong }

        // 🛡️ Chuỗi kết nối: Pooling=false mặc định để Dispose = nhả tệp ngay (cần cho Move/Copy/xóa backup trên Windows)
        private static string MoLoiChoEmTaoChuoiKetNoi(string dbPath, bool usePooling = false, SqliteOpenMode mode = SqliteOpenMode.ReadWrite)
            => new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = mode,
                Pooling = usePooling,
                DefaultTimeout = 30
            }.ToString();

        // ===================== HỆ THỐNG GHI LOG BẤT ĐỒNG BỘ (DEDICATED WORKER) =====================
        private static readonly ConcurrentQueue<(DateTime Gio, string NoiDung)> _logFileQueue = new();
        private static readonly ConcurrentQueue<string> _logDbQueue = new();
        private static int _isFlushingLog = 0;
        // 1 = CSDL đang bị khóa bởi bảo trì nặng (VACUUM / khôi phục) -> hoãn ghi NhatKy vào DB, vẫn ghi file ngay
        private static int _dangBaoTriNang = 0;

        private static void ToQuocGoiTenAnhGhiLogBaoTri(string thongDiep)
        {
            try
            {
                // Chỉ đẩy vào Queue RAM cực nhanh, KHÔNG tạo Task fire-and-forget cho từng dòng log.
                _logFileQueue.Enqueue((DateTime.Now, thongDiep));
                _logDbQueue.Enqueue(thongDiep);
                Debug.WriteLine($"[Trình Bảo Trì] {thongDiep}");

                // Giới hạn RAM: bỏ dòng cũ nhất nếu Worker không theo kịp
                while (_logFileQueue.Count > TRAN_HANG_DOI_LOG) _logFileQueue.TryDequeue(out _);
                while (_logDbQueue.Count > TRAN_HANG_DOI_LOG) _logDbQueue.TryDequeue(out _);

                KichHoatWorkerGhiLog();
            }
            catch { }
        }

        private static void KichHoatWorkerGhiLog()
        {
            // Chỉ 1 Worker chạy tại 1 thời điểm
            if (Interlocked.CompareExchange(ref _isFlushingLog, 1, 0) == 0)
            {
                _ = Task.Run(FlushLogWorkerAsync);
            }
        }

        private static async Task FlushLogWorkerAsync()
        {
            try
            {
                // 1. Ghi file trước (nhanh, không phụ thuộc CSDL)
                if (!_logFileQueue.IsEmpty)
                    await GhiLogRaFileAsync().ConfigureAwait(false);

                // 2. Ghi NhatKy vào DB sau, theo lô, và chỉ khi CSDL không bị bảo trì nặng khóa
                GhiLogVaoCsdl();
            }
            catch (Exception ex) { Debug.WriteLine($"[Log Worker] {ex.Message}"); }
            finally
            {
                // Mở khóa Worker
                Interlocked.Exchange(ref _isFlushingLog, 0);
                // Double-Check: tránh sót log rơi vào queue đúng lúc mở khóa
                bool conViecFile = !_logFileQueue.IsEmpty;
                bool conViecDb = !_logDbQueue.IsEmpty && Volatile.Read(ref _dangBaoTriNang) == 0;
                if ((conViecFile || conViecDb) && Interlocked.CompareExchange(ref _isFlushingLog, 1, 0) == 0)
                {
                    _ = Task.Run(FlushLogWorkerAsync);
                }
            }
        }

        private static string DinhDangDongLog((DateTime Gio, string NoiDung) m)
            => $"[{m.Gio:dd/MM/yyyy HH:mm:ss}] {m.NoiDung}";

        private static async Task GhiLogRaFileAsync()
        {
            Directory.CreateDirectory(_thuMucBackup);   // idempotent
            string filePath = Path.Combine(_thuMucBackup, TEN_FILE_NHAT_KY);
            XoayVongNhatKyNeuCan(filePath);

            var sb = new StringBuilder(1024);
            while (!_logFileQueue.IsEmpty)
            {
                sb.Clear();
                int dem = 0;
                // Dequeue vào bộ đệm cục bộ TRƯỚC khi ghi: lỗi file không làm kẹt vòng lặp
                while (dem < SO_LOG_MOI_LAN_XA && _logFileQueue.TryDequeue(out var m))
                {
                    sb.AppendLine(DinhDangDongLog(m));
                    dem++;
                }
                if (dem == 0) break;
                try
                {
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

        private static void GhiLogVaoCsdl()
        {
            while (Volatile.Read(ref _dangBaoTriNang) == 0 && _logDbQueue.TryDequeue(out var thongDiep))
            {
                try
                {
                    string tenNhap = "System (Trình Bảo Trì CSDL)";
                    Module_NhatKy.GhiNhatKy(
                         taiKhoan: tenNhap,
                         hanhDong: thongDiep,
                         ghiChu: $"Thời gian: {SessionInfo.ThoiGianDangNhap:dd-MM-yyyy HH:mm:ss}"
                    );
                }
                catch { /* Bỏ qua lỗi ghi DB để Worker không bị chết (dòng log vẫn nằm trong file) */ }
            }
        }

        // ===================== RETRY ENGINE (CHỐNG SQLITE_BUSY / SQLITE_LOCKED) =====================
        private static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, int maxRetries = 3, CancellationToken cancellationToken = default)
        {
            if (maxRetries < 1) maxRetries = 1;
            int delayMs = 1000;
            for (int i = 0; ; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return await action().ConfigureAwait(false);
                }
                catch (SqliteException ex) when ((ex.SqliteErrorCode == SQLITE_BUSY || ex.SqliteErrorCode == SQLITE_LOCKED) && i < maxRetries - 1)
                {
                    ToQuocGoiTenAnhGhiLogBaoTri($"[SQLite Busy] DB đang bận, thử lại lần {i + 1} sau {delayMs}ms...");
                }
                // Lần cuối mà vẫn BUSY: filter ở trên không khớp -> exception tự lan ra (không bao giờ trả về giá trị giả như default)
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                delayMs = Math.Min(delayMs * 2, 8000);   // Exponential Backoff thật sự
            }
        }

        private static async Task ExecuteWithRetryAsync(Func<Task> action, int maxRetries = 3, CancellationToken cancellationToken = default)
        {
            await ExecuteWithRetryAsync<bool>(async () => { await action().ConfigureAwait(false); return true; }, maxRetries, cancellationToken).ConfigureAwait(false);
        }

        // ===================== TIỆN ÍCH NHỎ =====================
        private static long DocPragmaSo(SqliteConnection conn, string tenPragma)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA " + tenPragma + ";";   // tenPragma chỉ nhận hằng số nội bộ
            object? r = cmd.ExecuteScalar();
            return r == null || r == DBNull.Value ? 0 : Convert.ToInt64(r);
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
                string? goc = Path.GetPathRoot(Path.GetFullPath(duongDan));
                if (string.IsNullOrEmpty(goc)) return true;
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

        private static void XoaNeuCo(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        private static void XoaKemTepPhu(string dbPath)
        {
            XoaNeuCo(dbPath);
            foreach (string hau in HauToTepPhu) XoaNeuCo(dbPath + hau);
        }

        private static List<FileInfo> LayDanhSachBackup(string tenFile)
        {
            if (!Directory.Exists(_thuMucBackup)) return new List<FileInfo>();
            // Tên chứa yyyy_MM -> sắp theo TÊN giảm dần = mới nhất trước (không phụ thuộc CreationTime, vốn bị reset khi chép thư mục)
            return new DirectoryInfo(_thuMucBackup)
                .EnumerateFiles($"{tenFile}_Backup_*.db")
                .OrderByDescending(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string LayDuongDanBackupThang(string tenFile, DateTime now)
            => Path.Combine(_thuMucBackup, $"{tenFile}_Backup_{now:yyyy_MM}.db");

        // ===================== BƯỚC 1: KHỞI TẠO CẤU HÌNH TỐI ƯU =====================
        private static readonly ConcurrentDictionary<string, byte> _daToiUu = new(StringComparer.OrdinalIgnoreCase);

        public static async Task KhoiTaoCauHinhToiUuAsync(string dbPath)
        {
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return;
            string khoa;
            try { khoa = Path.GetFullPath(dbPath); } catch { return; }
            if (_daToiUu.ContainsKey(khoa)) return;   // mỗi CSDL chỉ cần áp dụng 1 lần / phiên chạy

            try
            {
                string? cheDo = await ExecuteWithRetryAsync<string?>(() => Task.Run(() => ApDungCauHinhNen(dbPath))).ConfigureAwait(false);
                if (string.Equals(cheDo, "wal", StringComparison.OrdinalIgnoreCase))
                {
                    _daToiUu[khoa] = 0;
                }
                else
                {
                    // WAL không bật được (ổ mạng, USB FAT32, thư mục chỉ-đọc...) -> hệ thống vẫn chạy bình thường ở chế độ cũ
                    ToQuocGoiTenAnhGhiLogBaoTri($"[Khởi tạo PRAGMA] Không bật được WAL cho {Path.GetFileName(dbPath)} (chế độ hiện tại: {cheDo ?? "không rõ"}).");
                }
            }
            catch (Exception ex) { ToQuocGoiTenAnhGhiLogBaoTri($"[Lỗi Khởi tạo PRAGMA] {ex.Message}"); }
        }

        private static string? ApDungCauHinhNen(string dbPath)
        {
            using var conn = new SqliteConnection(MoLoiChoEmTaoChuoiKetNoi(dbPath));
            conn.Open();

            // Chỉ giữ các PRAGMA được LƯU VÀO TỆP .db (có hiệu lực cho mọi kết nối về sau):
            //  - journal_mode = WAL : ghi/đọc đồng thời, ít khóa hơn
            //  - auto_vacuum = INCREMENTAL : chỉ có tác dụng với CSDL còn trống bảng, hoặc sau 1 lần VACUUM
            // (synchronous / foreign_keys / temp_store / wal_autocheckpoint chỉ sống trong kết nối này nên đặt ở đây vô nghĩa.)
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
            if (!await _khoaTienTrinh.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false))
            {
                ToQuocGoiTenAnhGhiLogBaoTri($"[Bảo Trì Nhẹ] Bỏ qua cho {Path.GetFileName(dbPath)} vì hệ thống đang bận.");
                return;
            }
            try
            {
                // Toàn bộ việc nặng chạy trên ThreadPool: Microsoft.Data.Sqlite không có I/O bất đồng bộ thật,
                // gọi trực tiếp từ UI thread sẽ làm đơ giao diện.
                await ExecuteWithRetryAsync(() => Task.Run(() => ThucThiBaoTriNhe(dbPath)), maxRetries: 2, cancellationToken: cancellationToken).ConfigureAwait(false);
                ToQuocGoiTenAnhGhiLogBaoTri($"[Bảo Trì Nhẹ] Hoàn tất Optimize & dọn dẹp cho {Path.GetFileName(dbPath)}");
            }
            catch (OperationCanceledException) { /* app đang tắt */ }
            catch (Exception ex) { ToQuocGoiTenAnhGhiLogBaoTri($"[Lỗi Bảo trì nhẹ] {ex.Message}"); }
            finally { _khoaTienTrinh.Release(); }
        }

        private static void ThucThiBaoTriNhe(string dbPath)
        {
            using var conn = new SqliteConnection(MoLoiChoEmTaoChuoiKetNoi(dbPath));
            conn.Open();
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
            var now = DateTime.Now;
            bool canBackup = CanTaoBackupThang(tenFile, now);
            if (!canBackup && !cuongBuc) return;

            await _khoaTienTrinh.WaitAsync(cancellationToken).ConfigureAwait(false);
            Interlocked.Exchange(ref _dangBaoTriNang, 1);
            try
            {
                // Kiểm tra lại sau khi giành được khóa (tiến trình khác có thể vừa làm xong)
                now = DateTime.Now;
                canBackup = CanTaoBackupThang(tenFile, now);
                if (!canBackup && !cuongBuc) return;

                Directory.CreateDirectory(_thuMucBackup);
                ToQuocGoiTenAnhGhiLogBaoTri($"[Bảo Trì Nặng] Đang xử lý: {tenFile}...");

                // 1. INTEGRITY CHECK (trên ThreadPool)
                var tinhTrang = await ExecuteWithRetryAsync<TinhTrangDb>(
                    () => Task.Run(() => KiemTraSucKhoe(duongDanDB), cancellationToken), 3, cancellationToken).ConfigureAwait(false);

                // 🛡️ CORRUPTION RECOVERY (Auto-Restore)
                if (tinhTrang == TinhTrangDb.Hong)
                {
                    await PhucHoiTuBackupAsync(duongDanDB, tenFile, now, cancellationToken).ConfigureAwait(false);
                    return;   // Khôi phục xong (hoặc không thể) thì thoát, không Vacuum nữa.
                }

                // 2. ONLINE BACKUP (ghi ra .tmp -> kiểm tra -> đổi tên nguyên tử)
                bool coBackup = !canBackup;   // đã có bản của tháng này thì coi như đã an toàn
                if (canBackup)
                {
                    string duongDanBackup = LayDuongDanBackupThang(tenFile, now);
                    coBackup = await Task.Run(() => ThucThiBackupAnToan(duongDanDB, duongDanBackup, cancellationToken), cancellationToken).ConfigureAwait(false);
                }

                // 3. FULL VACUUM (chỉ khi DB khỏe, có backup, và thực sự nhiều rác / cần chuyển sang INCREMENTAL)
                if (coBackup)
                {
                    if (await KiemTraCanFullVacuumAsync(duongDanDB, cancellationToken).ConfigureAwait(false))
                    {
                        await ExecuteWithRetryAsync(() => Task.Run(() => ThucThiFullVacuum(duongDanDB)), 3, cancellationToken).ConfigureAwait(false);
                        ToQuocGoiTenAnhGhiLogBaoTri("[Bảo Trì Nặng] Full VACUUM hoàn tất.");
                    }
                    // 4. CLEANUP (giữ tối đa 3 bản VÀ dung lượng tổng dưới 2GB, không bao giờ xóa bản mới nhất)
                    DondepBackupCu(tenFile, SO_BAN_BACKUP_GIU_LAI, maxSizeBytes: TRAN_DUNG_LUONG_BACKUP);
                }
            }
            catch (OperationCanceledException) { ToQuocGoiTenAnhGhiLogBaoTri("[Bảo Trì Nặng] Đã hủy tiến trình (Timeout hoặc User tắt App)."); }
            catch (Exception ex) { ToQuocGoiTenAnhGhiLogBaoTri($"[Lỗi Bảo trì nặng] {ex.Message}"); }
            finally
            {
                Interlocked.Exchange(ref _dangBaoTriNang, 0);
                _khoaTienTrinh.Release();
                KichHoatWorkerGhiLog();   // xả các dòng NhatKy bị hoãn trong lúc CSDL bị khóa
            }
        }

        /// <summary>Hỏng thật sự (integrity_check != ok, hoặc SQLite báo CORRUPT / NOTADB). BUSY không bao giờ bị coi là hỏng.</summary>
        private static TinhTrangDb KiemTraSucKhoe(string dbPath)
        {
            try
            {
                using var conn = new SqliteConnection(MoLoiChoEmTaoChuoiKetNoi(dbPath));
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA integrity_check(1);";   // dừng ở lỗi đầu tiên cho nhanh
                object? kq = cmd.ExecuteScalar();
                return string.Equals(kq?.ToString(), "ok", StringComparison.OrdinalIgnoreCase) ? TinhTrangDb.Tot : TinhTrangDb.Hong;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == SQLITE_CORRUPT || ex.SqliteErrorCode == SQLITE_NOTADB)
            {
                return TinhTrangDb.Hong;
            }
        }

        private static async Task PhucHoiTuBackupAsync(string duongDanDB, string tenFile, DateTime now, CancellationToken ct)
        {
            ToQuocGoiTenAnhGhiLogBaoTri($"[NGUY HIỂM] {tenFile} bị Corrupt! Tiến hành tìm bản sao lưu hợp lệ để phục hồi.");

            string tam = Path.Combine(_thuMucBackup, $"{tenFile}_Restore.tmp");
            string? tenBanDung = null;
            try
            {
                // 1. Chọn bản sao lưu MỚI NHẤT mà bản thân nó còn toàn vẹn (chưa đụng tới DB đang chạy)
                foreach (var f in LayDanhSachBackup(tenFile))
                {
                    ct.ThrowIfCancellationRequested();
                    XoaNeuCo(tam);
                    File.Copy(f.FullName, tam, true);
                    var tinhTrang = await Task.Run(() => KiemTraSucKhoe(tam), ct).ConfigureAwait(false);
                    if (tinhTrang == TinhTrangDb.Tot) { tenBanDung = f.Name; break; }
                    ToQuocGoiTenAnhGhiLogBaoTri($"[Phục Hồi] Bản {f.Name} cũng không toàn vẹn, thử bản cũ hơn...");
                }

                if (tenBanDung == null)
                {
                    // Không có bản thay thế -> GIỮ NGUYÊN tệp hiện tại (không cách ly), tránh để ứng dụng chạy với CSDL trống
                    ToQuocGoiTenAnhGhiLogBaoTri($"[CẢNH BÁO ĐỎ] Không có bản sao lưu hợp lệ để phục hồi cho {tenFile}! Giữ nguyên tệp hiện tại, cần xử lý thủ công.");
                    return;
                }

                // 2. Nhả mọi kết nối đang được giữ trong pool (nếu không Windows sẽ báo "tệp đang được sử dụng")
                SqliteConnection.ClearAllPools();

                // 3. Cách ly tệp hỏng (kèm -wal/-shm/-journal: WAL cũ mà dính vào bản backup sẽ làm hỏng chính bản backup) rồi đặt bản tốt vào chỗ
                string corruptPath = Path.Combine(_thuMucBackup, $"{tenFile}_CORRUPTED_{now:yyyyMMdd_HHmmss}.db");
                var daChuyen = new List<(string Tu, string Den)>(4);
                try
                {
                    foreach (string hau in new[] { "", "-wal", "-shm", "-journal" })
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

                ToQuocGoiTenAnhGhiLogBaoTri($"[Phục Hồi] Đã khôi phục thành công từ: {tenBanDung}. Bản hỏng được lưu ở {Path.GetFileName(corruptPath)}. Hủy Vacuum.");

                // 4. Bản backup ở chế độ journal thường -> bật lại WAL; dọn bớt bản hỏng cũ
                _daToiUu.TryRemove(Path.GetFullPath(duongDanDB), out _);
                await KhoiTaoCauHinhToiUuAsync(duongDanDB).ConfigureAwait(false);
                DondepBanHongCu(tenFile, SO_BAN_HONG_GIU_LAI);
            }
            finally
            {
                XoaNeuCo(tam);
            }
        }

        private static bool ThucThiBackupAnToan(string dbGoc, string dbBackup, CancellationToken token)
        {
            string tam = dbBackup + ".tmp";
            try
            {
                token.ThrowIfCancellationRequested();

                // Không đủ chỗ trống thì thôi, tránh làm đầy ổ đĩa trên máy cấu hình thấp
                long canByte = KichThuocCsdlKemWal(dbGoc);
                canByte += canByte / 10 + 16L * 1024 * 1024;
                if (!DuDungLuong(_thuMucBackup, canByte))
                {
                    ToQuocGoiTenAnhGhiLogBaoTri($"[Backup] Bỏ qua {Path.GetFileName(dbGoc)}: ổ đĩa không đủ dung lượng trống.");
                    return false;
                }

                XoaNeuCo(tam);
                using (var ketNoiGoc = new SqliteConnection(MoLoiChoEmTaoChuoiKetNoi(dbGoc)))
                using (var ketNoiBackup = new SqliteConnection(MoLoiChoEmTaoChuoiKetNoi(tam, mode: SqliteOpenMode.ReadWriteCreate)))
                {
                    ketNoiGoc.Open();
                    ketNoiBackup.Open();
                    token.ThrowIfCancellationRequested();
                    ketNoiGoc.BackupDatabase(ketNoiBackup);

                    // Bản backup là 1 tệp tự đủ, không kéo theo -wal/-shm
                    HoanThienVaKiemTraBackup(ketNoiBackup);
                }

                // Chỉ khi bản .tmp đã được kiểm tra mới đổi tên thành tên chính thức (nguyên tử):
                // sập nguồn giữa chừng sẽ không để lại "bản backup" dở dang mà lần sau tưởng là hợp lệ.
                File.Move(tam, dbBackup, overwrite: true);
                return true;
            }
            catch (OperationCanceledException)
            {
                XoaNeuCo(tam);
                throw;
            }
            catch (Exception ex)
            {
                ToQuocGoiTenAnhGhiLogBaoTri($"[Lỗi Backup] {Path.GetFileName(dbGoc)}: {ex.Message}");
                XoaNeuCo(tam);
                return false;
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

        private static bool _khongDoiDuocAutoVacuum = false;   // đã thử chuyển INCREMENTAL mà không có tác dụng -> đừng VACUUM lặp lại vô ích

        private static async Task<bool> KiemTraCanFullVacuumAsync(string dbPath, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return false;
            return await ExecuteWithRetryAsync<bool>(() => Task.Run(() => DanhGiaCanFullVacuum(dbPath, token), token), 3, token).ConfigureAwait(false);
        }

        private static bool DanhGiaCanFullVacuum(string dbPath, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            using var conn = new SqliteConnection(MoLoiChoEmTaoChuoiKetNoi(dbPath));
            conn.Open();

            long pageCount = DocPragmaSo(conn, "page_count");
            if (pageCount <= 0) return false;
            long freeCount = DocPragmaSo(conn, "freelist_count");
            long pageSize = DocPragmaSo(conn, "page_size");
            long autoVacuum = DocPragmaSo(conn, "auto_vacuum");

            double fragmentationPercent = (freeCount * 100.0) / pageCount;
            long byteLangPhi = freeCount * pageSize;
            ToQuocGoiTenAnhGhiLogBaoTri($"[Kiểm Tra Vacuum] PageCount={pageCount:N0}, FreeList={freeCount:N0}, Fragment={fragmentationPercent:F2}%, AutoVacuum={autoVacuum}");

            bool nhieuRac = fragmentationPercent > NGUONG_PHAN_TRAM_PHAN_MANH && byteLangPhi >= NGUONG_BYTE_LANG_PHI_TOI_THIEU;
            bool canDoiAutoVacuum = autoVacuum != 2 && !_khongDoiDuocAutoVacuum;   // 1 lần VACUUM duy nhất để bật INCREMENTAL
            if (!nhieuRac && !canDoiAutoVacuum) return false;

            // VACUUM cần ~ 1 lần dung lượng DB ở thư mục tạm (ổ hệ thống) và ~ 2 lần ở ổ chứa DB (WAL)
            long kichThuoc = pageCount * pageSize;
            string thuMucDb = Path.GetDirectoryName(Path.GetFullPath(dbPath)) ?? dbPath;
            if (!DuDungLuong(thuMucDb, kichThuoc * 2 + 16L * 1024 * 1024) ||
                !DuDungLuong(Path.GetTempPath(), kichThuoc + 16L * 1024 * 1024))
            {
                ToQuocGoiTenAnhGhiLogBaoTri("[Kiểm Tra Vacuum] Bỏ qua VACUUM: không đủ dung lượng trống.");
                return false;
            }
            return true;
        }

        private static void ThucThiFullVacuum(string dbPath)
        {
            using var conn = new SqliteConnection(MoLoiChoEmTaoChuoiKetNoi(dbPath));
            conn.Open();
            ChayLenh(conn, "PRAGMA wal_checkpoint(TRUNCATE);");
            // Đặt TRƯỚC khi VACUUM, trên CÙNG kết nối: đây là cách duy nhất để CSDL cũ chuyển sang INCREMENTAL
            ChayLenh(conn, "PRAGMA auto_vacuum = INCREMENTAL;");
            ChayLenh(conn, "VACUUM;");
            if (DocPragmaSo(conn, "auto_vacuum") != 2)
                _khongDoiDuocAutoVacuum = true;
        }

        // 🛡️ Dọn dẹp backup theo Số lượng VÀ Dung lượng; KHÔNG BAO GIỜ xóa bản mới nhất
        private static void DondepBackupCu(string tenFileGoc, int soLuongGiuLai, long maxSizeBytes)
        {
            try
            {
                var files = LayDanhSachBackup(tenFileGoc);   // mới nhất ở đầu danh sách
                long tongDungLuong = files.Sum(f => f.Length);
                for (int i = files.Count - 1; i >= 1; i--)   // từ cũ nhất -> mới dần, bỏ qua files[0]
                {
                    bool vuotSoLuong = i >= soLuongGiuLai;
                    bool vuotDungLuong = tongDungLuong > maxSizeBytes;
                    if (!vuotSoLuong && !vuotDungLuong) break;   // đạt mức an toàn thì dừng
                    try
                    {
                        long len = files[i].Length;
                        files[i].Delete();
                        tongDungLuong -= len;
                    }
                    catch (Exception ex) { Debug.WriteLine($"[Dọn backup] Không xóa được {files[i].Name}: {ex.Message}"); }
                }
            }
            catch { }
        }

        private static void DondepBanHongCu(string tenFileGoc, int soLuongGiuLai)
        {
            try
            {
                if (!Directory.Exists(_thuMucBackup)) return;
                var hong = new DirectoryInfo(_thuMucBackup)
                    .EnumerateFiles($"{tenFileGoc}_CORRUPTED_*.db")
                    .OrderByDescending(f => f.Name, StringComparer.OrdinalIgnoreCase)
                    .Skip(soLuongGiuLai)
                    .ToList();
                foreach (var f in hong) XoaKemTepPhu(f.FullName);
            }
            catch { }
        }

        // ===================== KIỂM TRA THEO SỐ DÒNG NHẬT KÝ =====================
        private static long _bucketNhatKyDaXuLy = -1;

        public static Task KiemTraVaVaccumTheoSoDongAsync(string dbPath) => KiemTraVaVaccumTheoSoDongAsync(dbPath, CancellationToken.None);

        public static async Task KiemTraVaVaccumTheoSoDongAsync(string dbPath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return;
            try
            {
                long tongDong = await Task.Run(() => DemSoDongNhatKy(dbPath), cancellationToken).ConfigureAwait(false);

                // Logic: tổng dòng là bội số của 1000 (1000, 2000, ...) và > 0
                if (tongDong > 0 && tongDong % 1000 == 0)
                {
                    // Mỗi mốc 1000 chỉ kích hoạt đúng 1 lần (hàm này có thể được gọi lại nhiều lần khi số dòng chưa đổi)
                    long moc = tongDong / 1000;
                    if (Interlocked.Exchange(ref _bucketNhatKyDaXuLy, moc) != moc)
                    {
                        // Chạy nền hoàn toàn, có bắt lỗi (fire-and-forget không được để Task lỗi "rơi" mất)
                        _ = Task.Run(() => ChayBaoTriNangNenAnToanAsync(dbPath));
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi Check Vacuum 1000 dòng]: {ex.Message}");
            }
        }

        private static long DemSoDongNhatKy(string dbPath)
        {
            // Hàm này có thể được gọi thường xuyên -> dùng pool + chỉ đọc cho rẻ
            using var conn = new SqliteConnection(MoLoiChoEmTaoChuoiKetNoi(dbPath, usePooling: true));
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM NhatKy"; // Hoặc tên bảng bạn cần kiểm tra
            object? kq = cmd.ExecuteScalar();
            return kq == null || kq == DBNull.Value ? 0 : Convert.ToInt64(kq);
        }

        private static async Task ChayBaoTriNangNenAnToanAsync(string dbPath)
        {
            try
            {
                // cuongBuc: bỏ qua rào "3 ngày cuối tháng" (trước đây rào này khiến mốc 1000 dòng gần như không bao giờ chạy)
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
                while (Volatile.Read(ref _isFlushingLog) == 1 && sw.ElapsedMilliseconds < 1500)
                    Thread.Sleep(20);

                if (_logFileQueue.IsEmpty) return;

                Directory.CreateDirectory(_thuMucBackup);
                var sb = new StringBuilder(512);
                while (_logFileQueue.TryDequeue(out var m))
                    sb.AppendLine(DinhDangDongLog(m));   // cùng định dạng có mốc thời gian như Worker

                File.AppendAllText(Path.Combine(_thuMucBackup, TEN_FILE_NHAT_KY), sb.ToString(), Utf8KhongBom);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FlushFinalLogs] {ex.Message}");
            }
        }
    }
}