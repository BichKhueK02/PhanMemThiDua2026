using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;

namespace PhanMemThiDua2026
{
    internal static class Module_NhatKy
    {
        private static readonly ConcurrentQueue<LogItem> _logQueue = new();
        private static readonly CancellationTokenSource _cts = new();
        private static int _isFlushing = 0;
        // 3) MACHINE ID (LƯU VÀO SQLITE, KHÔNG DÙNG FILE)
        private static readonly object _lockMachineId = new object();
        private static string? _cachedMachineId;

        // 1) CẤU HÌNH CSDL
        private static string DuongDanCSDL
        {
            get
            {
                string path = Module_DanduongGPS.DuongDanCSDL3;
                if (string.IsNullOrWhiteSpace(path))
                    throw new InvalidOperationException("Chưa cấu hình đường dẫn CSDL nhật ký.");
                return path;
            }
        }
        private static string ConnectionString => $"Data Source={DuongDanCSDL};";
        // 2) HÀNG ĐỢI GHI LOG (CHỐNG LAG UI)
        //Tối ưu hiệu năng
        private static readonly object _lockTaiKhoan = new();

        private static string? _cachedTaiKhoanAdmin;

        private static DateTime _lastTaiKhoanRefresh = DateTime.MinValue;

        private static readonly TimeSpan _taiKhoanCacheTime =
            TimeSpan.FromMinutes(10);
        private static int _backgroundWorkerStarted = 0;
        private struct LogItem
        {
            public string ThoiGian;
            public string TaiKhoan;
            public string HanhDong;
            public string GhiChu;
        }

        private static string GetMachineID()
        {
            if (!string.IsNullOrEmpty(_cachedMachineId))
                return _cachedMachineId;

            lock (_lockMachineId)
            {
                if (!string.IsNullOrEmpty(_cachedMachineId))
                    return _cachedMachineId;

                try
                {
                    using var conn = new SqliteConnection(ConnectionString);
                    conn.Open();

                    // Đọc từ CSDL
                    using var cmdGet = conn.CreateCommand();
                    cmdGet.CommandText = "SELECT Value FROM SystemInfo WHERE Key = 'MACHINE_ID' LIMIT 1;";
                    var result = cmdGet.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        _cachedMachineId = result.ToString();
                    }
                    else
                    {
                        // Sinh mới và lưu vào CSDL
                        _cachedMachineId = Guid.NewGuid().ToString();
                        using var cmdInsert = conn.CreateCommand();
                        cmdInsert.CommandText = "INSERT INTO SystemInfo (Key, Value) VALUES ('MACHINE_ID', $Value);";
                        cmdInsert.Parameters.AddWithValue("$Value", _cachedMachineId);
                        cmdInsert.ExecuteNonQuery();
                    }
                }
                catch
                {
                    // Fallback
                    _cachedMachineId = Guid.NewGuid().ToString();
                }

                return _cachedMachineId;
            }
        }
        // =========================
        // 4) MÃ HÓA CÓ TIỀN TỐ "AES:"
        // =========================
        private const string AES_PREFIX = "AES:";
        private static string MaHoaAnToan(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            if (value.StartsWith(AES_PREFIX)) return value;

            return AES_PREFIX + BaoMatAES.MaHoa(value);
        }
        private static string GiaiMaAnToan(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            if (value.StartsWith(AES_PREFIX))
            {
                string encodedPart = value.Substring(AES_PREFIX.Length);
                return BaoMatAES.GiaiMa(encodedPart);
            }
            return value;
        }
        // =========================
        // 5) KHỞI TẠO BẢNG & TỐI ƯU CSDL
        // =========================
        public static void TaoBangNhatKy()
        {
            // Tạo thư mục nếu chưa có
            string dir = Path.GetDirectoryName(DuongDanCSDL) ?? "";
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            // BẬT TỐI ƯU HÓA SQLITE (RẤT QUAN TRỌNG)
            using var pragma = conn.CreateCommand();
            pragma.CommandText = @"
                PRAGMA journal_mode=WAL;
                PRAGMA synchronous=NORMAL;
                PRAGMA temp_store=MEMORY;
                PRAGMA busy_timeout=5000;
            ";
            pragma.ExecuteNonQuery();

            // Tạo bảng SystemInfo và NhatKyUngDung
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS SystemInfo (
                    Key TEXT PRIMARY KEY,
                    Value TEXT
                );

                CREATE TABLE IF NOT EXISTS NhatKyUngDung (
                    ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    ThoiGian TEXT NOT NULL,
                    TenMay TEXT,
                    ID_CPU TEXT,
                    TaiKhoan TEXT,
                    HanhDong TEXT,
                    GhiChu TEXT
                );
                
                CREATE INDEX IF NOT EXISTS IDX_NhatKy_ThoiGian ON NhatKyUngDung(ThoiGian);
                CREATE INDEX IF NOT EXISTS IDX_NhatKy_TaiKhoan ON NhatKyUngDung(TaiKhoan);
            ";
            cmd.ExecuteNonQuery();

            // Khởi chạy tiến trình xả log ngầm
            if (Interlocked.Exchange(ref _backgroundWorkerStarted, 1) == 0)
            {
                Task.Run(() => BackgroundFlushLoop(_cts.Token));
            }
        }
        // =========================
        // 6) RESOLVE TÀI KHOẢN (GIỮ NGUYÊN)
        // =========================
        private static string ResolveTaiKhoan(string taiKhoan)
        {
            if (!string.IsNullOrWhiteSpace(taiKhoan) &&
                !taiKhoan.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase))
            {
                return taiKhoan;
            }

            try
            {
                lock (_lockTaiKhoan)
                {
                    bool needRefresh =
                        string.IsNullOrWhiteSpace(_cachedTaiKhoanAdmin)
                        ||
                        (DateTime.UtcNow - _lastTaiKhoanRefresh) >
                        _taiKhoanCacheTime;

                    if (!needRefresh)
                    {
                        return _cachedTaiKhoanAdmin!;
                    }

                    string dbPath = Module_DanduongGPS.DuongDanCSDL1;

                    if (!File.Exists(dbPath))
                    {
                        return taiKhoan;
                    }

                    using var conn =
                        new SqliteConnection($"Data Source={dbPath}");

                    conn.Open();

                    using var cmd = conn.CreateCommand();

                    cmd.CommandText =
                        "SELECT TenTaiKhoan FROM Admin WHERE ID = 1 LIMIT 1;";

                    object? result = cmd.ExecuteScalar();

                    if (result != null &&
                        result != DBNull.Value)
                    {
                        _cachedTaiKhoanAdmin =
                            BaoMatAES.GiaiMa(result.ToString() ?? "");

                        _lastTaiKhoanRefresh = DateTime.UtcNow;

                        return _cachedTaiKhoanAdmin;
                    }
                }
            }
            catch
            {
            }

            return taiKhoan;
        }
        // 7) GHI NHẬT KÝ (ĐẨY VÀO HÀNG ĐỢI)
        public static void GhiNhatKy(string taiKhoan, string hanhDong, string ghiChu = "")
        {
            _logQueue.Enqueue(new LogItem
            {
                ThoiGian = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                TaiKhoan = taiKhoan,
                HanhDong = hanhDong,
                GhiChu = ghiChu
            });
        }
        // 8) XẢ LOG XUỐNG CSDL (BACKGROUND)
        private static async Task BackgroundFlushLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(2000, token); // Mỗi 2 giây kiểm tra 1 lần
                    if (!_logQueue.IsEmpty)
                    {
                        FlushQueueToDatabase();
                    }
                }
                catch (TaskCanceledException) { break; }
                catch (Exception ex) { Debug.WriteLine("Lỗi vòng lặp Flush: " + ex); }
            }
        }
   
        private const int MAX_BATCH_SIZE = 500;
        private static long _flushCounter = 0;
        public static void FlushQueueToDatabase()
        {
            if (Interlocked.CompareExchange(ref _isFlushing, 1, 0) != 0)
                return;

            try
            {
                while (!_logQueue.IsEmpty)
                {
                    var logsToInsert = new List<LogItem>(MAX_BATCH_SIZE);

                    while (logsToInsert.Count < MAX_BATCH_SIZE &&
                           _logQueue.TryDequeue(out var item))
                    {
                        logsToInsert.Add(item);
                    }

                    if (logsToInsert.Count == 0)
                        break;

                    string currentMachine =
                        MaHoaAnToan(Environment.MachineName);

                    string currentCpu =
                        MaHoaAnToan(GetMachineID());

                    string resolvedSystemAccount =
                        ResolveTaiKhoan("SYSTEM");

                    using var conn =
                        new SqliteConnection(ConnectionString);

                    conn.Open();

                    using var transaction =
                        conn.BeginTransaction();

                    try
                    {
                        using var cmd =
                            conn.CreateCommand();

                        cmd.Transaction = transaction;

                        cmd.CommandText = @"
INSERT INTO NhatKyUngDung
(
    ThoiGian,
    TenMay,
    ID_CPU,
    TaiKhoan,
    HanhDong,
    GhiChu
)
VALUES
(
    $ThoiGian,
    $TenMay,
    $ID_CPU,
    $TaiKhoan,
    $HanhDong,
    $GhiChu
);";

                        var pThoiGian =
                            cmd.Parameters.Add("$ThoiGian", SqliteType.Text);

                        var pTenMay =
                            cmd.Parameters.Add("$TenMay", SqliteType.Text);

                        var pIdCpu =
                            cmd.Parameters.Add("$ID_CPU", SqliteType.Text);

                        var pTaiKhoan =
                            cmd.Parameters.Add("$TaiKhoan", SqliteType.Text);

                        var pHanhDong =
                            cmd.Parameters.Add("$HanhDong", SqliteType.Text);

                        var pGhiChu =
                            cmd.Parameters.Add("$GhiChu", SqliteType.Text);

                        foreach (var log in logsToInsert)
                        {
                            pThoiGian.Value = log.ThoiGian;
                            pTenMay.Value = currentMachine;
                            pIdCpu.Value = currentCpu;

                            string taiKhoanThucTe =
                                string.IsNullOrWhiteSpace(log.TaiKhoan) ||
                                log.TaiKhoan.Equals(
                                    "SYSTEM",
                                    StringComparison.OrdinalIgnoreCase)
                                ? resolvedSystemAccount
                                : log.TaiKhoan;

                            pTaiKhoan.Value =
                                MaHoaAnToan(taiKhoanThucTe);

                            pHanhDong.Value =
                                MaHoaAnToan(log.HanhDong);

                            pGhiChu.Value =
                                MaHoaAnToan(log.GhiChu);

                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        try
                        {
                            transaction.Rollback();
                        }
                        catch
                        {
                        }

                        throw;
                    }

                    long currentFlush =
                        Interlocked.Increment(ref _flushCounter);

                    if (currentFlush % 500 == 0)
                    {
                        CleanupOldLogs();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[FlushQueueToDatabase] {ex}");
            }
            finally
            {
                Interlocked.Exchange(ref _isFlushing, 0);
            }
        }
        private static void CleanupOldLogs()
        {
            try
            {
                using var conn =
                    new SqliteConnection(ConnectionString);

                conn.Open();

                using var countCmd =
                    conn.CreateCommand();

                countCmd.CommandText =
                    "SELECT COUNT(*) FROM NhatKyUngDung;";

                long totalRows =
                    Convert.ToInt64(
                        countCmd.ExecuteScalar());

                if (totalRows <= 50000)
                    return;

                using var transaction =
                    conn.BeginTransaction();

                using var cmd =
                    conn.CreateCommand();

                cmd.Transaction = transaction;

                cmd.CommandText = @"
DELETE FROM NhatKyUngDung
WHERE ID <
(
    SELECT MAX(ID) - 50000
    FROM NhatKyUngDung
);";

                cmd.ExecuteNonQuery();

                transaction.Commit();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[CleanupOldLogs] {ex}");
            }
        }
        public static DataTable LoadTatCaNhatKy()
        {
            var dt = new DataTable();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM NhatKyUngDung ORDER BY ID ASC;";
            using var reader = cmd.ExecuteReader();
            dt.Load(reader);

            // Giải mã dữ liệu trước khi trả về GridView
            foreach (DataRow row in dt.Rows)
            {
                row["TenMay"] = GiaiMaAnToan(row["TenMay"].ToString() ?? "");
                row["ID_CPU"] = GiaiMaAnToan(row["ID_CPU"].ToString() ?? "");
                row["TaiKhoan"] = GiaiMaAnToan(row["TaiKhoan"].ToString() ?? "");
                row["HanhDong"] = GiaiMaAnToan(row["HanhDong"].ToString() ?? "");
                row["GhiChu"] = GiaiMaAnToan(row["GhiChu"].ToString() ?? "");
            }

            return dt;
        }
        // 10) CÁC HÀM TIỆN ÍCH KHÁC
        public static void XoaTatCaNhatKy()
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM NhatKyUngDung;";
            cmd.ExecuteNonQuery();
        }
        public static List<string> LayDanhSachTaiKhoan()
        {
            var list = new List<string>();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT TaiKhoan FROM NhatKyUngDung WHERE TaiKhoan IS NOT NULL AND TaiKhoan <> '';";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string raw = reader.GetString(0);
                list.Add(GiaiMaAnToan(raw));
            }

            return list;
        }

        public static void DocVaNapStatusLabelForm10()
        {
            // Chạy ngầm tác vụ đọc DB để không làm đơ UI của bất kỳ Form nào gọi tới nó
            Task.Run(() =>
            {
                try
                {
                    string luaChon = "Không xóa";

                    // Đảm bảo bảng cấu hình đã được khởi tạo


                    using var conn = new SqliteConnection(ConnectionString);
                    conn.Open();

                    using (var cmdSelect = conn.CreateCommand())
                    {
                        cmdSelect.CommandText = "SELECT Chọn_GiaiTri FROM TuDong_XoaNhatKy WHERE ID = 1 LIMIT 1;";
                        var result = cmdSelect.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            luaChon = result.ToString();
                    }

                    int soDongTuCsdl = luaChon switch
                    {
                        "1000 dòng xóa tự động" => 1000,
                        "5000 dòng xóa tự động" => 5000,
                        "10000 dòng xóa tự động" => 10000,
                        _ => 0
                    };

                    // Chuỗi định dạng hiển thị kết quả
                    string textHienThi = soDongTuCsdl > 0
                        ? $"Tài khoản: {Module_TaiKhoan.TenTaiKhoan_RAM} | Tự động xóa khi đạt {soDongTuCsdl} dòng"
                        : $"Tài khoản: {Module_TaiKhoan.TenTaiKhoan_RAM}";

                    // Tự động dò tìm Form10 trong danh sách các Form đang mở của ứng dụng
                    // Sử dụng đồng bộ luồng Invoke an toàn để nạp chuỗi văn bản lên UI
                    var f10 = System.Windows.Forms.Application.OpenForms["Form10_NhatKy"] as Form10_NhatKy;
                    if (f10 != null && f10.IsHandleCreated && !f10.IsDisposed)
                    {
                        f10.Invoke(new Action(() =>
                        {
                            // Truy cập vào toolStripStatusLabel1 của Form10 thông qua thuộc tính hoặc hàm (Xem hướng dẫn bước 2)
                            f10.CapNhatVanBanStatusLabel(textHienThi);
                        }));
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DocVaNapStatusLabelForm10] Lỗi: {ex.Message}");
                }
            });
        }

    }
}