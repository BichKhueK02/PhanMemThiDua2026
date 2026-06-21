using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace PhanMemThiDua2026
{
    public static class Module_BaoTriCSDL
    {
        // 🛡️ ANTI-RACE: Khóa luồng đồng bộ
        private static readonly SemaphoreSlim _khoaTienTrinh = new SemaphoreSlim(1, 1);
        private static readonly string _thuMucBackup = Path.Combine(Application.StartupPath, "CSDL_Backups");
        // 🛡️ CHUẨN CHUYÊN GIA 1: Tối ưu Connection String (Thêm Pooling và Timeout)
        private static string MoLoiChoEmTaoChuoiKetNoi(string dbPath, bool usePooling = true)
            => $"Data Source={dbPath};Pooling={usePooling};Default Timeout=30;";
        // 🚀 HỆ THỐNG GHI LOG BẤT ĐỒNG BỘ CẤP ĐỘ DOANH NGHIỆP (DEDICATED WORKER)      
        private static readonly ConcurrentQueue<string> _logFileQueue = new ConcurrentQueue<string>();
        private static int _isFlushingLog = 0;
        private static void ToQuocGoiTenAnhGhiLogBaoTri(string thongDiep)
        {
            try
            {
                // Chỉ đẩy vào Queue RAM cực nhanh, tuyệt đối KHÔNG TẠO Task.Run (fire-and-forget) ở đây nữa.
                _logFileQueue.Enqueue(thongDiep);
                Debug.WriteLine($"[Trình Bảo Trì] {thongDiep}");

                // Kích hoạt Worker xả Queue (chỉ 1 Worker chạy tại 1 thời điểm)
                if (Interlocked.CompareExchange(ref _isFlushingLog, 1, 0) == 0)
                {
                    _ = Task.Run(FlushLogWorkerAsync);
                }
            }
            catch { }
        }
        private static async Task FlushLogWorkerAsync()
        {
            try
            {
                if (!Directory.Exists(_thuMucBackup)) Directory.CreateDirectory(_thuMucBackup);
                string filePath = Path.Combine(_thuMucBackup, "BaoTri_NhatKy.txt");

                var batchThongDiep = new List<string>();

                using (var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, useAsync: true))
                using (var writer = new StreamWriter(stream))
                {
                    // Xả sạch Queue vào File I/O
                    while (_logFileQueue.TryDequeue(out string thongDiep))
                    {
                        batchThongDiep.Add(thongDiep);
                        await writer.WriteLineAsync($"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] {thongDiep}");
                    }
                }

                // Sau khi ghi file xong mới tiến hành ghi DB theo dạng Batch, tránh Lock kéo dài
                foreach (var thongDiep in batchThongDiep)
                {
                    try
                    {
                        string tenNhap = "System (Trình Bảo Trì CSDL)";
                        string thongDiepNhatKy = thongDiep;

                        // 🔹 Đã sử dụng chính xác lệnh gọi mà bạn yêu cầu
                        Module_NhatKy.GhiNhatKy(
                             taiKhoan: tenNhap,
                             hanhDong: thongDiepNhatKy,
                             ghiChu: $"Thời gian: {SessionInfo.ThoiGianDangNhap:dd-MM-yyyy HH:mm:ss}"
                        );
                    }
                    catch { /* Bỏ qua lỗi ghi DB để Worker không bị chết */ }
                }
            }
            catch { }
            finally
            {
                // Mở khóa Worker
                Interlocked.Exchange(ref _isFlushingLog, 0);

                // Double-Check Lock: Tránh sót log rơi vào queue đúng lúc mở khóa
                if (!_logFileQueue.IsEmpty && Interlocked.CompareExchange(ref _isFlushingLog, 1, 0) == 0)
                {
                    _ = Task.Run(FlushLogWorkerAsync);
                }
            }
        }
        // 🚀 RETRY ENGINE (CHỐNG SQLITE_BUSY / SQLITE_LOCKED)   
        private static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, int maxRetries = 3, CancellationToken cancellationToken = default)
        {
            int delayMs = 1000;
            for (int i = 0; i < maxRetries; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return await action();
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 5 || ex.SqliteErrorCode == 6)
                {
                    if (i == maxRetries - 1) throw;
                    ToQuocGoiTenAnhGhiLogBaoTri($"[SQLite Busy] DB đang bận, thử lại lần {i + 1} sau {delayMs}ms...");
                    try
                    {
                        await Task.Delay(delayMs, cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        throw new OperationCanceledException("Quá trình chờ bị hủy do hệ thống yêu cầu dừng.");
                    }

                    delayMs += 500; // Exponential Backoff
                }
            }
            return default;
        }
        private static async Task ExecuteWithRetryAsync(Func<Task> action, int maxRetries = 3, CancellationToken cancellationToken = default)
        {
            await ExecuteWithRetryAsync<bool>(async () => { await action(); return true; }, maxRetries, cancellationToken);
        }
        // 🚀 BƯỚC 1: KHỞI TẠO CẤU HÌNH TỐI ƯU    
        public static async Task KhoiTaoCauHinhToiUuAsync(string dbPath)
        {
            if (!File.Exists(dbPath)) return;
            try
            {
                await ExecuteWithRetryAsync(async () =>
                {
                    using var conn = new SqliteConnection(MoLoiChoEmTaoChuoiKetNoi(dbPath));
                    await conn.OpenAsync();
                    using var cmd = conn.CreateCommand();

                    // 🛡️ CHUẨN ENTERPRISE: Bổ sung wal_autocheckpoint để WAL không bị phình to
                    cmd.CommandText = @"
                        PRAGMA journal_mode = WAL;
                        PRAGMA synchronous = NORMAL;
                        PRAGMA temp_store = MEMORY;
                        PRAGMA foreign_keys = ON;
                        PRAGMA auto_vacuum = INCREMENTAL;
                        PRAGMA wal_autocheckpoint = 1000;";
                    await cmd.ExecuteNonQueryAsync();
                });
            }
            catch (Exception ex) { ToQuocGoiTenAnhGhiLogBaoTri($"[Lỗi Khởi tạo PRAGMA] {ex.Message}"); }
        }
        // 🚀 BƯỚC 2: BẢO TRÌ NHẸ HÀNG NGÀY      
        public static async Task BaoTriNheHangNgayAsync(string dbPath)
        {
            if (!File.Exists(dbPath)) return;

            if (!await _khoaTienTrinh.WaitAsync(TimeSpan.FromSeconds(5)))
            {
                ToQuocGoiTenAnhGhiLogBaoTri($"[Bảo Trì Nhẹ] Bỏ qua cho {Path.GetFileName(dbPath)} vì hệ thống đang bận.");
                return;
            }

            try
            {
                await ExecuteWithRetryAsync(async () =>
                {
                    using var conn = new SqliteConnection(MoLoiChoEmTaoChuoiKetNoi(dbPath));
                    await conn.OpenAsync();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            PRAGMA optimize;
                            PRAGMA incremental_vacuum(1000);";
                        await cmd.ExecuteNonQueryAsync();
                    }
                }, maxRetries: 2);

                ToQuocGoiTenAnhGhiLogBaoTri($"[Bảo Trì Nhẹ] Hoàn tất Optimize & Incremental Vacuum cho {Path.GetFileName(dbPath)}");
            }
            catch (Exception ex) { ToQuocGoiTenAnhGhiLogBaoTri($"[Lỗi Bảo trì nhẹ] {ex.Message}"); }
            finally { _khoaTienTrinh.Release(); }
        }
        // 🚀 BƯỚC 3: BẢO TRÌ NẶNG CUỐI THÁNG (KÈM AUTO-RESTORE VÀ TIMEOUT)      
        public static async Task TuDongBaoTriNangCuoiThangAsync(string duongDanDB, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(duongDanDB) || !File.Exists(duongDanDB)) return;

            string tenFile = Path.GetFileNameWithoutExtension(duongDanDB);
            string thangHienTai = DateTime.Now.ToString("yyyy_MM");
            string duongDanBackup = Path.Combine(_thuMucBackup, $"{tenFile}_Backup_{thangHienTai}.db");

            // Smart Check
            if (File.Exists(duongDanBackup)) return;
            if (DateTime.Now.Day < DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month) - 2) return;

            await _khoaTienTrinh.WaitAsync(cancellationToken);
            try
            {
                if (File.Exists(duongDanBackup)) return;
                if (!Directory.Exists(_thuMucBackup)) Directory.CreateDirectory(_thuMucBackup);

                ToQuocGoiTenAnhGhiLogBaoTri($"[Bảo Trì Nặng] Đang xử lý: {tenFile}...");

                // 1. INTEGRITY CHECK
                bool isHealthy = await ExecuteWithRetryAsync(async () =>
                {
                    using var conn = new SqliteConnection(MoLoiChoEmTaoChuoiKetNoi(duongDanDB));
                    await conn.OpenAsync(cancellationToken);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "PRAGMA integrity_check;";
                    var result = await cmd.ExecuteScalarAsync(cancellationToken);
                    return result?.ToString()?.Equals("ok", StringComparison.OrdinalIgnoreCase) == true;
                });

                // 🛡️ CHUẨN ENTERPRISE: CORRUPTION RECOVERY (Auto-Restore)
                if (!isHealthy)
                {
                    ToQuocGoiTenAnhGhiLogBaoTri($"[NGUY HIỂM] {tenFile} bị Corrupt! Tiến hành cách ly và tự động phục hồi.");

                    // Cách ly file hỏng
                    string corruptPath = Path.Combine(_thuMucBackup, $"{tenFile}_CORRUPTED_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                    File.Move(duongDanDB, corruptPath);

                    // Tìm bản backup an toàn gần nhất
                    var lastBackup = new DirectoryInfo(_thuMucBackup)
                        .GetFiles($"{tenFile}_Backup_*.db")
                        .OrderByDescending(f => f.CreationTime)
                        .FirstOrDefault();

                    if (lastBackup != null)
                    {
                        File.Copy(lastBackup.FullName, duongDanDB, true);
                        ToQuocGoiTenAnhGhiLogBaoTri($"[Phục Hồi] Đã khôi phục thành công từ: {lastBackup.Name}. Hủy Vacuum.");
                        return; // Khôi phục xong thì thoát, không Vacuum nữa.
                    }
                    else
                    {
                        ToQuocGoiTenAnhGhiLogBaoTri($"[CẢNH BÁO ĐỎ] Không có bản sao lưu nào để phục hồi cho {tenFile}!");
                        return;
                    }
                }

                // 2. ONLINE BACKUP
                bool backupThanhCong = await Task.Run(() => ThucThiBackupAnToan(duongDanDB, duongDanBackup, cancellationToken), cancellationToken);

                // 3. FULL VACUUM (Chỉ làm khi DB khỏe và thực sự nhiều rác)
                if (backupThanhCong && isHealthy)
                {
                    bool canFullVacuum = await KiemTraCanFullVacuumAsync(duongDanDB, cancellationToken);
                    if (canFullVacuum)
                    {
                        await ExecuteWithRetryAsync(async () =>
                        {
                            using var conn = new SqliteConnection(MoLoiChoEmTaoChuoiKetNoi(duongDanDB));
                            await conn.OpenAsync(cancellationToken);

                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                                await cmd.ExecuteNonQueryAsync(cancellationToken);
                            }

                            // 🛡️ CHUẨN ENTERPRISE: Timeout 10 phút riêng cho Vacuum để tránh kẹt
                            using var vacuumCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                            vacuumCts.CancelAfter(TimeSpan.FromMinutes(10));

                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = "VACUUM;";
                                await cmd.ExecuteNonQueryAsync(vacuumCts.Token);
                            }
                        });
                        ToQuocGoiTenAnhGhiLogBaoTri($"[Bảo Trì Nặng] Full VACUUM hoàn tất.");
                    }

                    // 4. CLEANUP (Giữ tối đa 3 bản VÀ dung lượng tổng dưới 2GB)
                    DondepBackupCu(tenFile, 3, maxSizeBytes: 2L * 1024 * 1024 * 1024);
                }
            }
            catch (OperationCanceledException) { ToQuocGoiTenAnhGhiLogBaoTri("[Bảo Trì Nặng] Đã hủy tiến trình (Timeout hoặc User tắt App)."); }
            catch (Exception ex) { ToQuocGoiTenAnhGhiLogBaoTri($"[Lỗi Bảo trì nặng] {ex.Message}"); }
            finally { _khoaTienTrinh.Release(); }
        }
        private static bool ThucThiBackupAnToan(string dbGoc, string dbBackup, CancellationToken token)
        {
            try
            {
                using var ketNoiGoc = new SqliteConnection(MoLoiChoEmTaoChuoiKetNoi(dbGoc));
                using var ketNoiBackup = new SqliteConnection(MoLoiChoEmTaoChuoiKetNoi(dbBackup));
                ketNoiGoc.Open();
                ketNoiBackup.Open();
                token.ThrowIfCancellationRequested();
                ketNoiGoc.BackupDatabase(ketNoiBackup);
                return true;
            }
            catch
            {
                if (File.Exists(dbBackup)) File.Delete(dbBackup);
                return false;
            }
        }
        private static async Task<bool> KiemTraCanFullVacuumAsync(string dbPath, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return false;

            return await ExecuteWithRetryAsync(async () =>
            {
                token.ThrowIfCancellationRequested();
                using var conn = new SqliteConnection(MoLoiChoEmTaoChuoiKetNoi(dbPath));
                await conn.OpenAsync(token);

                long pageCount;
                using (var cmdPage = conn.CreateCommand())
                {
                    cmdPage.CommandText = "PRAGMA page_count;";
                    object result = await cmdPage.ExecuteScalarAsync(token);
                    if (result == null || result == DBNull.Value) return false;
                    pageCount = Convert.ToInt64(result);
                }
                if (pageCount <= 0) return false;

                long freeCount;
                using (var cmdFree = conn.CreateCommand())
                {
                    cmdFree.CommandText = "PRAGMA freelist_count;";
                    object result = await cmdFree.ExecuteScalarAsync(token);
                    if (result == null || result == DBNull.Value) return false;
                    freeCount = Convert.ToInt64(result);
                }

                double fragmentationPercent = (freeCount * 100.0) / pageCount;
                ToQuocGoiTenAnhGhiLogBaoTri($"[Kiểm Tra Vacuum] PageCount={pageCount:N0}, FreeList={freeCount:N0}, Fragment={fragmentationPercent:F2}%");

                return fragmentationPercent > 20.0;
            }, 3, token);
        }
        // 🛡️ CHUẨN ENTERPRISE: Dọn dẹp backup không chỉ theo số lượng (Count) mà còn theo dung lượng (Size)
        private static void DondepBackupCu(string tenFileGoc, int soLuongGiuLai, long maxSizeBytes)
        {
            try
            {
                var files = new DirectoryInfo(_thuMucBackup)
                    .GetFiles($"{tenFileGoc}_Backup_*.db")
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                long currentTotalSize = files.Sum(f => f.Length);

                // Ưu tiên 1: Xóa các file cũ vượt quá số lượng cho phép (Ví dụ > 3 bản)
                foreach (var file in files.Skip(soLuongGiuLai))
                {
                    currentTotalSize -= file.Length;
                    file.Delete();
                }

                // Ưu tiên 2: Nếu 3 bản còn lại nhưng tổng dung lượng > 2GB -> Xóa dần từ file cũ nhất
                var remainingFiles = files.Take(soLuongGiuLai).OrderBy(f => f.CreationTime).ToList();
                foreach (var file in remainingFiles)
                {
                    if (currentTotalSize <= maxSizeBytes) break; // Đạt mức an toàn thì dừng
                    currentTotalSize -= file.Length;
                    file.Delete();
                }
            }
            catch { }
        }
        // Thêm vào trong Module_BaoTriCSDL
        public static async Task KiemTraVaVaccumTheoSoDongAsync(string dbPath)
        {
            if (!File.Exists(dbPath)) return;

            try
            {
                int tongDong = 0;
                using (var conn = new SqliteConnection($"Data Source={dbPath}"))
                {
                    await conn.OpenAsync();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT COUNT(*) FROM NhatKy"; // Hoặc tên bảng bạn cần kiểm tra
                    tongDong = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // Logic: Nếu tổng dòng là bội số của 1000 (1000, 2000, 3000...)
                // Và phải lớn hơn 0 để tránh chạy lúc DB rỗng
                if (tongDong > 0 && tongDong % 1000 == 0)
                {
                    // Gọi hàm bảo trì nặng (Full Vacuum + Integrity Check) đã viết ở turn trước
                    // Chúng ta dùng Task.Run để nó chạy ngầm hoàn toàn, không block Form Nhật Ký
                    _ = TuDongBaoTriNangCuoiThangAsync(dbPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi Check Vacuum 1000 dòng]: {ex.Message}");
            }
        }
        public static void FlushFinalLogs()
        {
            // Đợi Worker ghi nốt những gì còn sót lại trong Queue trước khi đóng hẳn Process
            while (!_logFileQueue.IsEmpty)
            {
                // Chạy đồng bộ (Sync) lần cuối cùng để đảm bảo an toàn dữ liệu
                if (_logFileQueue.TryDequeue(out string msg))
                {
                    File.AppendAllText(Path.Combine(_thuMucBackup, "BaoTri_NhatKy.txt"), msg + Environment.NewLine);
                }
            }
        }
    }
}
