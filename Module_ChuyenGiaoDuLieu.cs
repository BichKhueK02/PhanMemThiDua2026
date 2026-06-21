using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace PhanMemThiDua2026
{
    public static class Module_ChuyenGiaoDuLieu
    {
        private static readonly ConcurrentDictionary<string, List<string>> _cacheDanhSachBang = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, List<string>> _cacheThongTin = new(StringComparer.OrdinalIgnoreCase);
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };
        public static void VacuumDatabase(string duongDanDb)
        {
            if (!File.Exists(duongDanDb)) return;
            try
            {
                using var ketNoi = new SqliteConnection(TaoConnectionString(duongDanDb, false));
                ketNoi.Open();
                using var lenh = ketNoi.CreateCommand();
                lenh.CommandTimeout = 180;
                lenh.CommandText = "VACUUM; ANALYZE;";
                lenh.ExecuteNonQuery();
                _cacheThongTin.TryRemove(duongDanDb, out _);
            }
            catch { }
        }
        private static readonly Color MauTieuDe = Color.FromArgb(0, 51, 153);
        private static readonly Color MauThanhCong = Color.ForestGreen;
        private static readonly Color MauCanhBao = Color.DarkOrange;
        private static readonly Color MauLoi = Color.Red;
        private static readonly Color MauThongTin = Color.DarkBlue;
        private static readonly Color MauPhu = Color.DimGray;
        private static readonly Color MauSeparator = Color.DarkGray;
        private static string TaoConnectionString(string dbPath, bool readOnly)
        {
            return $"Data Source={dbPath};Mode={(readOnly ? "ReadOnly" : "ReadWrite")};Pooling=True;Cache=Private;Default Timeout=30;";
        }
        public static List<string> NoiVongTayLonKetNoiTimKiemCSDL()
        {
            HashSet<string> ketQua = new(StringComparer.OrdinalIgnoreCase);
            try
            {
                string thuMucDatabase = Path.Combine(AppContext.BaseDirectory, "Database");
                if (Directory.Exists(thuMucDatabase))
                {
                    foreach (string file in Directory.GetFiles(thuMucDatabase, "*.db", SearchOption.TopDirectoryOnly))
                        ketQua.Add(Path.GetFullPath(file));
                }

                string thuMucDesktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Database-PhanMemThiDua2026");
                if (Directory.Exists(thuMucDesktop))
                {
                    // 🛡️ SỬA LỖI NGHẼN: Chỉ quét TopDirectoryOnly. Tuyệt đối không cho càn quét sâu.
                    foreach (string file in Directory.GetFiles(thuMucDesktop, "*.db", SearchOption.TopDirectoryOnly))
                        ketQua.Add(Path.GetFullPath(file));
                }
            }
            catch (Exception ex) { Debug.WriteLine("[NoiVongTayLonKetNoiTimKiemCSDL] " + ex.Message); }

            return ketQua.ToList();
        }
        public static List<string> LayDanhSachBang(string duongDanDb)
        {
            if (string.IsNullOrWhiteSpace(duongDanDb))
                return new List<string>();

            if (!File.Exists(duongDanDb))
                return new List<string>();

            try
            {
                string fullPath = Path.GetFullPath(duongDanDb);

                if (_cacheDanhSachBang.TryGetValue(fullPath, out var cache))
                    return new List<string>(cache);

                List<string> danhSachBang = new List<string>(64);

                using SqliteConnection ketNoi = new SqliteConnection(
                    $"Data Source={fullPath};" +
                    $"Mode=ReadOnly;" +
                    $"Pooling=True;" +
                    $"Cache=Private;" +
                    $"Default Timeout=5;"
                );

                ketNoi.Open();

                using SqliteCommand pragma = ketNoi.CreateCommand();
                pragma.CommandText =
                    """
            PRAGMA query_only = TRUE;
            PRAGMA temp_store = MEMORY;
            PRAGMA foreign_keys = OFF;
            PRAGMA busy_timeout = 5000;
            """;

                pragma.ExecuteNonQuery();

                using SqliteCommand lenh = ketNoi.CreateCommand();

                lenh.CommandText =
                    """
            SELECT name
            FROM sqlite_master
            WHERE type='table'
            AND name NOT LIKE 'sqlite_%'
            ORDER BY name COLLATE NOCASE;
            """;

                using SqliteDataReader reader = lenh.ExecuteReader();

                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        string tenBang = reader.GetString(0);

                        if (!string.IsNullOrWhiteSpace(tenBang))
                            danhSachBang.Add(tenBang);
                    }
                }

                danhSachBang.TrimExcess();

                _cacheDanhSachBang[fullPath] = new List<string>(danhSachBang);

                return danhSachBang;
            }
            catch (SqliteException ex)
            {
                Debug.WriteLine("[LayDanhSachBang][SQLite] " + ex.Message);
                return new List<string>();
            }
            catch (IOException ex)
            {
                Debug.WriteLine("[LayDanhSachBang][IO] " + ex.Message);
                return new List<string>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[LayDanhSachBang][Unknown] " + ex.Message);
                return new List<string>();
            }
        }
        // ==========================================================
        // PHÂN TÍCH CHI TIẾT DATABASE SQLITE
        // ENGINEERING / PHYSICAL / INTERNAL METADATA
        // ==========================================================
        public static List<string> LayThongTinChiTietCSDL(string duongDanDb)
        {
            List<string> thongTin = new List<string>(256);

            try
            {
                if (string.IsNullOrWhiteSpace(duongDanDb))
                {
                    thongTin.Add("Database không hợp lệ.");
                    return thongTin;
                }

                if (!File.Exists(duongDanDb))
                {
                    thongTin.Add("Không tìm thấy database.");
                    return thongTin;
                }

                string fullPath = Path.GetFullPath(duongDanDb);

                if (_cacheThongTin.TryGetValue(fullPath, out var cache))
                    return new List<string>(cache);
                FileInfo info = new FileInfo(fullPath);
                thongTin.Add("Thông tin Database");
                thongTin.Add("");
                thongTin.Add($"Tên tệp: {info.Name}");
                //thongTin.Add($"Đường dẫn: {fullPath}");
                thongTin.Add($"Dung lượng vật lý: {(info.Length / 1024d / 1024d):F2} MB");
                thongTin.Add($"Ngày tạo: {info.CreationTime:dd/MM/yyyy HH:mm:ss}");
                thongTin.Add($"Chỉnh sửa cuối: {info.LastWriteTime:dd/MM/yyyy HH:mm:ss}");
                thongTin.Add($"Readonly: {info.IsReadOnly}");
                thongTin.Add($"Thuộc tính: {info.Attributes}");
                using SqliteConnection ketNoi = new SqliteConnection(
                    $"Data Source={fullPath};" +
                    $"Mode=ReadOnly;" +
                    $"Pooling=True;" +
                    $"Cache=Private;" +
                    $"Default Timeout=10;"
                );

                ketNoi.Open();

                using SqliteCommand pragma = ketNoi.CreateCommand();

                pragma.CommandText =
                    """
            PRAGMA query_only = TRUE;
            PRAGMA temp_store = MEMORY;
            PRAGMA busy_timeout = 10000;
            """;

                pragma.ExecuteNonQuery();

                // ======================================================
                // PAGE SIZE
                // ======================================================

                long pageSize;
                long pageCount;
                long freelistCount;

                using (SqliteCommand cmd = ketNoi.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA page_size;";
                    pageSize = Convert.ToInt64(cmd.ExecuteScalar() ?? 0);

                    cmd.CommandText = "PRAGMA page_count;";
                    pageCount = Convert.ToInt64(cmd.ExecuteScalar() ?? 0);

                    cmd.CommandText = "PRAGMA freelist_count;";
                    freelistCount = Convert.ToInt64(cmd.ExecuteScalar() ?? 0);
                }

                long kichThuocThuc = pageSize * pageCount;
                long kichThuocRac = freelistCount * pageSize;
                double phanManh =
                    pageCount > 0
                        ? ((double)freelistCount / pageCount) * 100d
                        : 0d;


                thongTin.Add($"Kích thước Page: {pageSize:N0} bytes");
                thongTin.Add($"Tổng số Page: {pageCount:N0}");
                thongTin.Add($"Free Pages: {freelistCount:N0}");
                thongTin.Add($"Dung lượng thực SQLite: {(kichThuocThuc / 1024d / 1024d):F2} MB");
                thongTin.Add($"Dung lượng rác nội bộ: {(kichThuocRac / 1024d / 1024d):F2} MB");
                thongTin.Add($"Mức phân mảnh: {phanManh:F2}%");

                // ======================================================
                // SQLITE ENGINE
                // ======================================================

                string journalMode;
                string synchronous;
                string encoding;
                string autoVacuum;
                string sqliteVersion;

                using (SqliteCommand cmd = ketNoi.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA journal_mode;";
                    journalMode = Convert.ToString(cmd.ExecuteScalar()) ?? "Unknown";

                    cmd.CommandText = "PRAGMA synchronous;";
                    synchronous = Convert.ToString(cmd.ExecuteScalar()) ?? "Unknown";

                    cmd.CommandText = "PRAGMA encoding;";
                    encoding = Convert.ToString(cmd.ExecuteScalar()) ?? "Unknown";

                    cmd.CommandText = "PRAGMA auto_vacuum;";
                    autoVacuum = Convert.ToString(cmd.ExecuteScalar()) ?? "Unknown";

                    cmd.CommandText = "SELECT sqlite_version();";
                    sqliteVersion = Convert.ToString(cmd.ExecuteScalar()) ?? "Unknown";
                }

                //thongTin.Add("");
                thongTin.Add($"SQLite Version: {sqliteVersion}");
                thongTin.Add($"Journal Mode: {journalMode}");
                thongTin.Add($"Synchronous: {synchronous}");
                thongTin.Add($"Encoding: {encoding}");
                thongTin.Add($"Auto Vacuum: {autoVacuum}");

                // ======================================================
                // SCHEMA
                // ======================================================

                long soBang;
                long soIndex;
                long soView;
                long soTrigger;

                using (SqliteCommand cmd = ketNoi.CreateCommand())
                {
                    cmd.CommandText =
                        """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type='table'
                AND name NOT LIKE 'sqlite_%';
                """;

                    soBang = Convert.ToInt64(cmd.ExecuteScalar() ?? 0);

                    cmd.CommandText =
                        """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type='index';
                """;

                    soIndex = Convert.ToInt64(cmd.ExecuteScalar() ?? 0);

                    cmd.CommandText =
                        """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type='view';
                """;

                    soView = Convert.ToInt64(cmd.ExecuteScalar() ?? 0);

                    cmd.CommandText =
                        """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type='trigger';
                """;

                    soTrigger = Convert.ToInt64(cmd.ExecuteScalar() ?? 0);
                }

                //thongTin.Add("");
                thongTin.Add($"Số bảng dữ liệu: {soBang:N0}");
                thongTin.Add($"Số Index: {soIndex:N0}");
                thongTin.Add($"Số View: {soView:N0}");
                thongTin.Add($"Số Trigger: {soTrigger:N0}");

                // ======================================================
                // INTEGRITY CHECK
                // ======================================================

                string integrityResult;

                using (SqliteCommand cmd = ketNoi.CreateCommand())
                {
                    cmd.CommandTimeout = 60;
                    cmd.CommandText = "PRAGMA integrity_check;";
                    integrityResult = Convert.ToString(cmd.ExecuteScalar()) ?? "Unknown";
                }

                //thongTin.Add("");

                thongTin.Add($"Integrity Check: {integrityResult}");

                // ======================================================
                // WAL FILE
                // ======================================================

                string walFile = fullPath + "-wal";
                string shmFile = fullPath + "-shm";

                //thongTin.Add("");

                thongTin.Add($"WAL tồn tại: {File.Exists(walFile)}");
                thongTin.Add($"SHM tồn tại: {File.Exists(shmFile)}");

                if (File.Exists(walFile))
                {
                    FileInfo walInfo = new FileInfo(walFile);

                    thongTin.Add($"Dung lượng WAL: {(walInfo.Length / 1024d / 1024d):F2} MB");
                }

                // ======================================================
                // CACHE
                // ======================================================

                _cacheThongTin[fullPath] = new List<string>(thongTin);

                return thongTin;
            }
            catch (SqliteException ex)
            {
                thongTin.Add("[SQLite Error] " + ex.Message);
                return thongTin;
            }
            catch (IOException ex)
            {
                thongTin.Add("[IO Error] " + ex.Message);
                return thongTin;
            }
            catch (UnauthorizedAccessException ex)
            {
                thongTin.Add("[Access Error] " + ex.Message);
                return thongTin;
            }
            catch (Exception ex)
            {
                thongTin.Add("[Unknown Error] " + ex.Message);
                return thongTin;
            }
        }
        public static void XuatDuLieuRaJson(string duongDanDb, string tenBang, string thuMucLuu)
        {
            if (!File.Exists(duongDanDb)) return;

            string fullPath = Path.GetFullPath(duongDanDb);
            List<Dictionary<string, object?>> danhSachDong = new(1024);

            try
            {
                // BƯỚC 1: Chỉ mở kết nối DB trong phạm vi hẹp nhất để lấy dữ liệu
                using (SqliteConnection ketNoi = new SqliteConnection(TaoConnectionString(fullPath, true)))
                {
                    ketNoi.Open();
                    using var lenh = ketNoi.CreateCommand();
                    lenh.CommandText = $"SELECT * FROM [{tenBang}];";

                    using var reader = lenh.ExecuteReader();
                    while (reader.Read())
                    {
                        Dictionary<string, object?> dong = new(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            object value = reader.GetValue(i);
                            dong[reader.GetName(i)] = value == DBNull.Value ? null : value;
                        }
                        danhSachDong.Add(dong);
                    }
                } // 🛡️ KẾT THÚC USING: Tới dòng này SQLite chắc chắn đã nhả khóa file DB

                // BƯỚC 2: Xử lý I/O độc lập, nếu máy tính bị kẹt ổ đĩa văng lỗi ở đây 
                // thì file DB cũng không bị khóa lây lan sang Form khác.
                Directory.CreateDirectory(thuMucLuu);
                string fileJson = Path.Combine(thuMucLuu, $"{tenBang}.json");

                using FileStream fs = new FileStream(fileJson, FileMode.Create, FileAccess.Write, FileShare.None, 65536);
                JsonSerializer.Serialize(fs, danhSachDong, _jsonOptions);
                CapNhatHeartbeat();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi export bảng {tenBang}: {ex.Message}");
            }
        }

        public static void NhapDuLieuVaoCSDL(string duongDanJson, string duongDanDb, string tenBang, bool xoaDuLieuCu, string duongDanFileBackup = "", string thuMucGoiCon = "")
        {
            if (!File.Exists(duongDanJson) || !File.Exists(duongDanDb)) return;

            string fullPathDb = Path.GetFullPath(duongDanDb);

            using FileStream fs = new FileStream(duongDanJson, FileMode.Open, FileAccess.Read, FileShare.Read, 65536);
            var danhSachDong = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(fs);
            if (danhSachDong == null) danhSachDong = new List<Dictionary<string, JsonElement>>();

            using var ketNoi = new SqliteConnection(TaoConnectionString(fullPathDb, false));
            ketNoi.Open();

            using SqliteTransaction giaoDich = ketNoi.BeginTransaction();
            using var lenh = ketNoi.CreateCommand();
            lenh.Transaction = giaoDich;

            try
            {
                if (xoaDuLieuCu)
                {
                    lenh.CommandText = $"DELETE FROM [{tenBang}];";
                    lenh.ExecuteNonQuery();

                    try
                    {
                        lenh.CommandText = $"DELETE FROM sqlite_sequence WHERE name='{tenBang.Replace("'", "''")}';";
                        lenh.ExecuteNonQuery();
                    }
                    catch { }
                }

                if (danhSachDong.Count > 0)
                {
                    HashSet<string> cotMayMoi = new(StringComparer.OrdinalIgnoreCase);
                    lenh.CommandText = $"PRAGMA table_info('{tenBang.Replace("'", "''")}');";
                    using (var readerSchema = lenh.ExecuteReader())
                    {
                        while (readerSchema.Read()) cotMayMoi.Add(readerSchema.GetString(1));
                    }

                    var danhSachCotChuan = danhSachDong.First().Keys.Where(cot => cotMayMoi.Contains(cot)).ToList();

                    if (danhSachCotChuan.Count > 0)
                    {
                        string cotSQL = string.Join(",", danhSachCotChuan.Select(c => $"[{c}]"));
                        string thamSoSQL = string.Join(",", danhSachCotChuan.Select((c, i) => $"@p{i}"));

                        lenh.CommandText = $"INSERT OR REPLACE INTO [{tenBang}] ({cotSQL}) VALUES ({thamSoSQL});";

                        for (int i = 0; i < danhSachCotChuan.Count; i++)
                        {
                            var param = lenh.CreateParameter();
                            param.ParameterName = $"@p{i}";
                            lenh.Parameters.Add(param);
                        }

                        lenh.Prepare();

                        foreach (var dong in danhSachDong)
                        {
                            for (int i = 0; i < danhSachCotChuan.Count; i++)
                            {
                                string tenCot = danhSachCotChuan[i];
                                var param = lenh.Parameters[$"@p{i}"];

                                if (dong.TryGetValue(tenCot, out JsonElement itemValue))
                                {
                                    // Pattern Matching chuẩn: DateTime cụ thể đặt lên trước String
                                    param.Value = itemValue.ValueKind switch
                                    {
                                        JsonValueKind.String when DateTime.TryParse(itemValue.GetString(), out DateTime dt) => dt,
                                        JsonValueKind.String => itemValue.GetString(),
                                        JsonValueKind.Number => itemValue.TryGetInt64(out long l) ? l : itemValue.GetDouble(),
                                        JsonValueKind.True => 1,
                                        JsonValueKind.False => 0,
                                        JsonValueKind.Null => DBNull.Value,
                                        _ => itemValue.GetRawText()
                                    };
                                }
                                else param.Value = DBNull.Value;
                            }
                            lenh.ExecuteNonQuery();
                        }
                    }
                }

                // ĐÃ KHỞI PHỤC THÀNH CÔNG XUỐNG Ổ CỨNG VẬT LÝ
                giaoDich.Commit();

                _cacheDanhSachBang.TryRemove(fullPathDb, out _);
                _cacheThongTin.TryRemove(fullPathDb, out _);

                // ====================================================================
                // 🔥 TRÌNH TIÊU HỦY NÓNG CHỐNG LỘ DỮ LIỆU CHUẨN CƠ MẬT CAND
                // ====================================================================

                // 1. Tiêu hủy file JSON nguồn vừa nạp thành công
                try
                {
                    if (File.Exists(duongDanJson))
                    {
                        File.SetAttributes(duongDanJson, FileAttributes.Normal);
                        File.Delete(duongDanJson);
                    }
                }
                catch { }

                // 2. Tiêu hủy file .bak bảo hiểm hệ thống tự đẻ ra của CSDL này
                try
                {
                    if (!string.IsNullOrWhiteSpace(duongDanFileBackup) && File.Exists(duongDanFileBackup))
                    {
                        File.SetAttributes(duongDanFileBackup, FileAttributes.Normal);
                        File.Delete(duongDanFileBackup);
                    }
                }
                catch { }

                // 3. Quét kiểm tra dọn dẹp phân vùng thư mục con cuối cùng (csdl1, csdl2...) và thư mục mẹ trên Desktop
                if (!string.IsNullOrWhiteSpace(thuMucGoiCon))
                {
                    XoaSachThuMucNeuRongTuanTu(thuMucGoiCon);
                }
            }
            catch (Exception ex)
            {
                giaoDich.Rollback();
                throw new Exception(ex.Message);
            }
        }
        private static void XoaSachThuMucNeuRongTuanTu(string thuMucCon)
        {
            try
            {
                if (!Directory.Exists(thuMucCon)) return;

                // Kiểm tra thư mục con (ví dụ: Desktop\Database-PhanMemThiDua2026\csdl1)
                DirectoryInfo subDir = new DirectoryInfo(thuMucCon);
                if (subDir.GetFiles().Length == 0 && subDir.GetDirectories().Length == 0)
                {
                    subDir.Attributes = FileAttributes.Normal;
                    subDir.Delete(true);
                    Debug.WriteLine($"[TỰ HỦY PHÂN VÙNG CON] Đã dọn dẹp sạch: {subDir.Name}");
                }

                // Kiểm tra thư mục mẹ ngoài Desktop
                string thuMucMe = Path.GetDirectoryName(thuMucCon)!;
                if (Directory.Exists(thuMucMe))
                {
                    DirectoryInfo mainDir = new DirectoryInfo(thuMucMe);

                    // Nếu Bản dự phòng (System) rỗng, xóa nó trước
                    string pathSystemBackup = Path.Combine(thuMucMe, "Bản dự phòng (System)");
                    if (Directory.Exists(pathSystemBackup))
                    {
                        DirectoryInfo sysDir = new DirectoryInfo(pathSystemBackup);
                        if (sysDir.GetFiles().Length == 0) { sysDir.Attributes = FileAttributes.Normal; sysDir.Delete(true); }
                    }

                    // Nếu không còn file hoặc thư mục con nào khác ngoài Desktop -> Xóa sổ luôn thư mục mẹ gốc
                    if (mainDir.GetFiles().Length == 0 && mainDir.GetDirectories().Length == 0)
                    {
                        mainDir.Attributes = FileAttributes.Normal;
                        mainDir.Delete(true);
                        Debug.WriteLine("[TỰ HỦY TOÀN CỤC] Không còn rác dữ liệu, đã xóa sạch thư mục gốc ngoài Desktop.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[XoaSachThuMucNeuRongTuanTu Lỗi] " + ex.Message);
            }
        }
        // ⭐ 6. SẢN XUẤT ĐIỂM KHÔI PHỤC BẢO HIỂM - ĐÃ CHUYỂN VÙNG RA DESKTOP
        // ====================================================================      
        public static string SaoLuuCSDLTruocKhiNhap(string duongDanDb)
        {
            if (string.IsNullOrWhiteSpace(duongDanDb))
                throw new ArgumentException("Đường dẫn CSDL không hợp lệ.", nameof(duongDanDb));

            string fullPathDb = Path.GetFullPath(duongDanDb);

            if (!File.Exists(fullPathDb))
                throw new FileNotFoundException("Không tìm thấy tệp database gốc để tiến hành sao lưu bảo hiểm.", fullPathDb);

            string thuMucMeDesktop = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "Database-PhanMemThiDua2026");

            string thuMucConBackup = Path.Combine(
                thuMucMeDesktop,
                "Bản dự phòng (System)");

            Directory.CreateDirectory(thuMucConBackup);

            // ==========================================================
            // DỌN FILE BACKUP CŨ
            // Giữ lại 7 ngày gần nhất để an toàn phục hồi
            // ==========================================================
            try
            {
                DateTime mocXoa = DateTime.Now.AddDays(-7);

                foreach (string file in Directory.EnumerateFiles(
                             thuMucConBackup,
                             "*.bak",
                             SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        FileInfo fi = new FileInfo(file);

                        if (fi.LastWriteTime < mocXoa)
                        {
                            fi.Attributes = FileAttributes.Normal;
                            fi.Delete();
                        }
                    }
                    catch
                    {
                        // Bỏ qua từng file lỗi
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Dọn rác backup lỗi] " + ex.Message);
            }

            string tenFileGoc = Path.GetFileNameWithoutExtension(fullPathDb);

            string tenBackup = string.Format(
                "{0}_BackupTruocKhiNhap_{1:yyyyMMdd_HHmmss}.bak",
                tenFileGoc,
                DateTime.Now);

            string duongDanBackup = Path.Combine(
                thuMucConBackup,
                tenBackup);

            // ==========================================================
            // COPY AN TOÀN - RETRY 3 LẦN
            // ==========================================================
            const int BufferSize = 65536;

            Exception? lastError = null;

            for (int lanThu = 1; lanThu <= 3; lanThu++)
            {
                try
                {
                    using FileStream source = new FileStream(
                        fullPathDb,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite,
                        BufferSize);

                    using FileStream dest = new FileStream(
                        duongDanBackup,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        BufferSize);

                    source.CopyTo(dest, BufferSize);

                    dest.Flush(true);

                    lastError = null;
                    break;
                }
                catch (IOException ex)
                {
                    lastError = ex;
                    Thread.Sleep(300);
                }
            }

            if (lastError != null)
                throw new IOException(
                    "Không thể tạo bản sao lưu bảo hiểm.",
                    lastError);

            // ==========================================================
            // CẬP NHẬT DẤU THỜI GIAN HOẠT ĐỘNG
            // ==========================================================
            try
            {
                DateTime now = DateTime.Now;

                File.SetLastWriteTime(duongDanBackup, now);
                Directory.SetLastWriteTime(thuMucConBackup, now);
                Directory.SetLastWriteTime(thuMucMeDesktop, now);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Cập nhật thời gian backup lỗi] " + ex.Message);
            }
            CapNhatHeartbeat();
            return duongDanBackup;
        }        //kỹ thuật Overloading (Nạp chồng phương thức)
        public static void InThongTinDatabaseLenRichTextBox(
    RichTextBox rtb,
    string duongDanFile,
    string thongBaoLoi)
        {
            if (rtb == null ||
                rtb.IsDisposed ||
                rtb.Disposing)
            {
                return;
            }

            rtb.SuspendLayout();

            Font fontBold = null;

            try
            {
                rtb.Clear();

                fontBold = new Font(
                    rtb.Font,
                    FontStyle.Bold);

                void AppendStyleText(
                    string text,
                    Color color,
                    bool bold = false)
                {
                    if (rtb.IsDisposed ||
                        rtb.Disposing)
                    {
                        return;
                    }

                    rtb.SelectionStart = rtb.TextLength;
                    rtb.SelectionLength = 0;
                    rtb.SelectionColor = color;
                    rtb.SelectionFont = bold
                        ? fontBold
                        : rtb.Font;

                    rtb.AppendText(text);
                }

                AppendStyleText(
                    "❌ LỖI HỆ THỐNG: KHÔNG THỂ TRUY CẬP PHÂN VÙNG DỮ LIỆU\n",
                    MauLoi,
                    true);

                AppendStyleText(
                    "--------------------------------------------------------------------------------\n",
                    MauSeparator);

                AppendStyleText(
                    " [THÔNG TIN TIẾN TRÌNH]\n",
                    MauPhu,
                    true);

                AppendStyleText(
                    "  • Tệp tin sự cố     : ",
                    Color.Black,
                    true);

                AppendStyleText(
                    $"{Path.GetFileName(duongDanFile)}\n",
                    Color.DarkRed);

                AppendStyleText(
                    "  • Đường dẫn tệp     : ",
                    Color.Black,
                    true);

                AppendStyleText(
                    $"{duongDanFile}\n",
                    Color.DimGray);

                AppendStyleText(
                    "--------------------------------------------------------------------------------\n",
                    MauSeparator);

                AppendStyleText(
                    " [CHI TIẾT NGUYÊN NHÂN BÁO VỀ]\n",
                    MauLoi,
                    true);

                AppendStyleText(
                    $"  {thongBaoLoi}\n",
                    Color.DarkRed);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "[RichTextBox Error Render Fail] " +
                    ex);
            }
            finally
            {
                fontBold?.Dispose();

                if (!rtb.IsDisposed &&
                    !rtb.Disposing)
                {
                    rtb.ResumeLayout();
                }
            }
        }
        public static void InThongTinDatabaseLenRichTextBox(
    RichTextBox rtb,
    string duongDanFile,
    List<string> danhSachBang)
        {
            if (rtb == null || rtb.IsDisposed || rtb.Disposing)
                return;

            danhSachBang ??= new List<string>();

            Font fontBold = null;

            try
            {
                rtb.SuspendLayout();
                rtb.Clear();

                fontBold = new Font(rtb.Font, FontStyle.Bold);

                List<string> thongTinGoc =
                    LayThongTinChiTietCSDL(duongDanFile);

                Dictionary<string, string> dictMeta =
                    new(StringComparer.OrdinalIgnoreCase);

                foreach (string line in thongTinGoc)
                {
                    int pos = line.IndexOf(':');

                    if (pos <= 0)
                        continue;

                    dictMeta[line[..pos].Trim()] =
                        line[(pos + 1)..].Trim();
                }

                void AppendStyleText(
                    string text,
                    Color color,
                    bool bold = false)
                {
                    if (rtb.IsDisposed || rtb.Disposing)
                        return;

                    rtb.SelectionStart = rtb.TextLength;
                    rtb.SelectionLength = 0;
                    rtb.SelectionColor = color;
                    rtb.SelectionFont = bold ? fontBold : rtb.Font;
                    rtb.AppendText(text);
                }

                string tenFile =
                    Path.GetFileName(duongDanFile);

                string dungLuongPhysical =
                    dictMeta.GetValueOrDefault(
                        "Dung lượng vật lý",
                        "Không rõ");

                string dungLuongSqlite =
                    dictMeta.GetValueOrDefault(
                        "Dung lượng thực SQLite",
                        "Không rõ");

                string mucPhanManh =
                    dictMeta.GetValueOrDefault(
                        "Mức phân mảnh",
                        "0.00%");

                string triggersCount =
                    dictMeta.GetValueOrDefault(
                        "Số Trigger",
                        "0");

                string integrity =
                    dictMeta.GetValueOrDefault(
                        "Integrity Check",
                        "Unknown");

                bool coLoi = false;

                foreach (string dong in thongTinGoc)
                {
                    if (dong.Contains(
                        "Lỗi",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        coLoi = true;
                        break;
                    }
                }

                AppendStyleText(
                    "THÔNG TIN PHÂN VÙNG CSDL CHỌN GIAO NHẬN CSDL\n",
                    MauTieuDe,
                    true);

                AppendStyleText(
                    "--------------------------------------------------------------------------------\n",
                    MauSeparator);

                AppendStyleText(
                    " [VẬT LÝ TỆP TIN]\n",
                    MauPhu,
                    true);

                AppendStyleText(
                    "  • Tên tệp dữ liệu   : ",
                    Color.Black,
                    true);

                AppendStyleText(
                    $"{tenFile}\n",
                    Color.DarkCyan);

                AppendStyleText(
                    "  • Dung lượng ổ đĩa  : ",
                    Color.Black,
                    true);

                AppendStyleText(
                    $"{dungLuongPhysical}\n",
                    Color.DarkMagenta);

                AppendStyleText(
                    "  • Trạng thái tệp    : ",
                    Color.Black,
                    true);

                if (integrity.Equals(
                    "ok",
                    StringComparison.OrdinalIgnoreCase))
                {
                    AppendStyleText(
                        "Online ➜ Cấu trúc Toàn vẹn an toàn\n",
                        MauThanhCong,
                        true);
                }
                else if (coLoi)
                {
                    AppendStyleText(
                        "Hỏng cấu trúc hoặc Tệp bị khóa chặn\n",
                        MauLoi,
                        true);
                }
                else
                {
                    AppendStyleText(
                        "Kết nối Online\n",
                        MauThanhCong,
                        true);
                }

                AppendStyleText(
                    "--------------------------------------------------------------------------------\n",
                    MauSeparator);

                AppendStyleText(
                    " [CẤU HÌNH THỜI GIAN THỰC & CHỈ SỐ ENGINE]\n",
                    MauPhu,
                    true);

                AppendStyleText(
                    "  • Phiên bản SQLite  : ",
                    Color.Black,
                    true);

                AppendStyleText(
                    $"v{dictMeta.GetValueOrDefault("SQLite Version", "3.x")}\n",
                    Color.Black);

                AppendStyleText(
                    "  • Dung lượng thực   : ",
                    Color.Black,
                    true);

                AppendStyleText(
                    $"{dungLuongSqlite}\n",
                    Color.DarkSlateBlue);

                AppendStyleText(
                    "  • Dung lượng rác    : ",
                    Color.Black,
                    true);

                AppendStyleText(
                    $"{dictMeta.GetValueOrDefault("Dung lượng rác nội bộ", "0.00 MB")}\n",
                    Color.Brown);

                AppendStyleText(
                    "  • Mức phân mảnh     : ",
                    Color.Black,
                    true);

                double.TryParse(
                    mucPhanManh.Replace("%", ""),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double phanTramFrag);

                AppendStyleText(
                    $"{mucPhanManh}\n",
                    phanTramFrag > 15.0
                        ? MauLoi
                        : MauThanhCong,
                    phanTramFrag > 15.0);

                AppendStyleText(
                    "  • Triggers bảo vệ   : ",
                    Color.Black,
                    true);

                AppendStyleText(
                    $"{triggersCount} bộ giám sát\n",
                    Color.DarkGoldenrod);

                AppendStyleText(
                    "  • Nhật ký WAL Mode  : ",
                    Color.Black,
                    true);

                bool walOn =
                    dictMeta
                    .GetValueOrDefault(
                        "WAL tồn tại",
                        "False")
                    .Equals(
                        "True",
                        StringComparison.OrdinalIgnoreCase);

                AppendStyleText(
                    walOn
                        ? "Đang kích hoạt (Tăng tốc ghi ngầm)\n"
                        : "Tắt\n",
                    Color.Teal);

                AppendStyleText(
                    "--------------------------------------------------------------------------------\n",
                    MauSeparator);

                AppendStyleText(
                    $"DANH SÁCH THÀNH PHẦN CẤU TRÚC ({danhSachBang.Count} BẢNG DỮ LIỆU)\n",
                    MauThongTin,
                    true);

                AppendStyleText(
                    "  Mọi bảng dưới đây sẽ được dời phân vùng tự động sang Json.\n\n",
                    Color.Gray);

                for (int i = 0; i < danhSachBang.Count; i++)
                {
                    AppendStyleText(
                        $"   [{i + 1:D2}] ",
                        MauPhu);

                    AppendStyleText(
                        danhSachBang[i],
                        Color.Black,
                        true);

                    AppendStyleText(
                        " ➜ Sẵn sàng xuất bản\n",
                        Color.MediumSeaGreen);
                }

                rtb.SelectionStart = 0;
                rtb.SelectionLength = 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "[RichTextBox Render Error] " + ex);

                if (!rtb.IsDisposed &&
                    !rtb.Disposing)
                {
                    rtb.Clear();

                    rtb.AppendText(
                        "LỖI HIỂN THỊ ĐỊNH DẠNG DATA:\r\n" +
                        ex.Message);
                }
            }
            finally
            {
                fontBold?.Dispose();

                if (!rtb.IsDisposed &&
                    !rtb.Disposing)
                {
                    rtb.ResumeLayout();
                }
            }
        }
        // ====================================================================
        // ⭐ TRÌNH TỰ VỆ CHỦ ĐỘNG - TIÊU DIỆT PHÂN VÙNG BỎ HOANG (ANTI-WASTE)
        // Chuẩn kỹ sư: Đếm ngày không sử dụng và xóa sổ trực tiếp khỏi Desktop
        private static void CapNhatHeartbeat()
        {
            try
            {
                string thuMucGoc = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "Database-PhanMemThiDua2026");

                Directory.CreateDirectory(thuMucGoc);

                string touchFile = Path.Combine(
                    thuMucGoc,
                    "system.touch");

                if (!File.Exists(touchFile))
                {
                    using (File.Create(touchFile))
                    {
                    }
                }

                DateTime now = DateTime.UtcNow;

                File.SetLastWriteTimeUtc(touchFile, now);
                Directory.SetLastWriteTimeUtc(thuMucGoc, now);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Heartbeat] " + ex.Message);
            }
        }
        public static void KiemTraVaHuyRacHeThong(int soNgayToiDaKhongDung = 2)
        {
            try
            {
                if (soNgayToiDaKhongDung < 1)
                    soNgayToiDaKhongDung = 1;

                string thuMucGoc = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "Database-PhanMemThiDua2026");

                if (!Directory.Exists(thuMucGoc))
                    return;

                // ==================================================
                // CHỐNG XÓA KHI PHẦN MỀM ĐANG CHẠY
                // ==================================================

                string tenExe = Path.GetFileNameWithoutExtension(
                    Application.ExecutablePath);

                if (Process.GetProcessesByName(tenExe).Length > 1)
                    return;

                DateTime lanHoatDongCuoi;

                string touchFile = Path.Combine(
                    thuMucGoc,
                    "system.touch");

                // ==================================================
                // ƯU TIÊN HEARTBEAT
                // ==================================================

                if (File.Exists(touchFile))
                {
                    lanHoatDongCuoi =
                        File.GetLastWriteTimeUtc(touchFile);
                }
                else
                {
                    // ==================================================
                    // FALLBACK QUÉT THƯ MỤC
                    // ==================================================

                    DirectoryInfo dir = new DirectoryInfo(thuMucGoc);

                    lanHoatDongCuoi =
                        dir.LastWriteTimeUtc;

                    try
                    {
                        foreach (FileSystemInfo item in
                                 dir.EnumerateFileSystemInfos(
                                     "*",
                                     SearchOption.AllDirectories))
                        {
                            if (item.LastWriteTimeUtc > lanHoatDongCuoi)
                            {
                                lanHoatDongCuoi =
                                    item.LastWriteTimeUtc;
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                double soNgayKhongDung =
                    (DateTime.UtcNow - lanHoatDongCuoi).TotalDays;

                if (soNgayKhongDung < soNgayToiDaKhongDung)
                    return;

                // ==================================================
                // GỠ READONLY
                // ==================================================

                try
                {
                    foreach (string file in Directory.EnumerateFiles(
                                 thuMucGoc,
                                 "*",
                                 SearchOption.AllDirectories))
                    {
                        try
                        {
                            File.SetAttributes(
                                file,
                                FileAttributes.Normal);
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                }

                // ==================================================
                // XÓA THƯ MỤC
                // ==================================================

                try
                {
                    Directory.Delete(thuMucGoc, true);

                    Debug.WriteLine(
                        $"[SYSTEM CLEANUP] Đã xóa: {thuMucGoc}");
                }
                catch (IOException ex)
                {
                    Debug.WriteLine(
                        "[SYSTEM CLEANUP][IO] " + ex.Message);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Debug.WriteLine(
                        "[SYSTEM CLEANUP][AUTH] " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "[KiemTraVaHuyRacHeThong] " + ex.Message);
            }
        }
    }
}