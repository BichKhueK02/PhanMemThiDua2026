using Microsoft.Data.Sqlite;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Xuất/nạp dữ liệu SQLite ⇄ JSON.
    /// Nguyên tắc: (1) dữ liệu đi theo DÒNG, không bao giờ nạp cả bảng vào RAM; (2) mọi tệp ghi ra đều ghi nguyên tử
    /// (tệp tạm → đổi tên); (3) mỗi bảng nạp trong MỘT giao dịch, lỗi là hoàn tác sạch; (4) sao lưu bằng SQLite Backup API
    /// (bản chụp nhất quán, kể cả khi CSDL đang mở ở chế độ WAL); (5) không xóa gì khi chưa chắc chắn thành công.
    /// </summary>
    public static class Module_ChuyenGiaoDuLieu
    {
        // ==================== HẰNG SỐ ====================
        public const string TEN_THU_MUC_GOI = "Database-PhanMemThiDua2026";
        public const string TEN_THU_MUC_BACKUP = "Bản dự phòng (System)";
        public const string TIEN_TO_METADATA = "sync_info_";
        private const int BO_DEM_TEP = 256 * 1024;                       // bộ đệm đọc/ghi tệp
        private const int GIOI_HAN_BO_DEM_MOT_DONG = 128 * 1024 * 1024;  // chặn tệp JSON độc hại có một "dòng" khổng lồ
        private const int SO_NGAY_GIU_BACKUP = 7;
        private const long NGUONG_KIEM_TRA_DAY_DU_BYTES = 64L * 1024 * 1024; // CSDL lớn hơn mức này: dùng quick_check
        private static readonly string[] TenCSDLMucTieu = { "csdl1", "csdl2", "csdl3", "csdl4" };
        private static readonly JavaScriptEncoder _boMaHoa = JavaScriptEncoder.Create(UnicodeRanges.All);
        private static readonly JsonWriterOptions _tuyChonGhiJson = new() { Indented = true, Encoder = _boMaHoa };
        private static readonly Color MauTieuDe = Color.FromArgb(0, 51, 153);
        private static readonly Color MauThanhCong = Color.ForestGreen;
        private static readonly Color MauLoi = Color.Red;
        private static readonly Color MauThongTin = Color.DarkBlue;
        private static readonly Color MauPhu = Color.DimGray;
        private static readonly Color MauSeparator = Color.DarkGray;
        // ==================== BỘ NHỚ ĐỆM (RAM) ====================
        // [RAM] Danh sách bảng / thông tin chi tiết được giữ trong RAM để lần chọn sau hiện ngay.
        // Mỗi mục kèm "dấu vân tay" của tệp (kích thước + thời điểm ghi của .db và .db-wal): tệp đổi là tự bỏ cache,
        // không còn tình trạng hiển thị dữ liệu cũ khi nơi khác trong phần mềm đã sửa CSDL.
        private sealed record DauVanTay(long KichThuoc, long ThoiGianGhi, long KichThuocWal, long ThoiGianWal);
        private sealed record MucDem(DauVanTay DauVanTay, List<string> DuLieu);
        private static readonly ConcurrentDictionary<string, MucDem> _cacheDanhSachBang = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, MucDem> _cacheThongTin = new(StringComparer.OrdinalIgnoreCase);
        private static DauVanTay LayDauVanTay(string fullPath)
        {
            var tep = new FileInfo(fullPath);
            var wal = new FileInfo(fullPath + "-wal");
            return new DauVanTay(
                tep.Exists ? tep.Length : -1, tep.Exists ? tep.LastWriteTimeUtc.Ticks : 0,
                wal.Exists ? wal.Length : -1, wal.Exists ? wal.LastWriteTimeUtc.Ticks : 0);
        }
        private static void XoaCache(string fullPath)
        {
            _cacheDanhSachBang.TryRemove(fullPath, out _);
            _cacheThongTin.TryRemove(fullPath, out _);
        }
        // ==================== KẾT NỐI & TIỆN ÍCH SQLITE ====================
        /// <summary>
        /// [AN TOÀN] Dùng ConnectionStringBuilder (đường dẫn có dấu ';' không làm vỡ chuỗi kết nối).
        /// [RAM] Pooling=False: kết nối quản trị chỉ sống ngắn, không để lại tay cầm tệp / bộ nhớ đệm trang nằm chờ trong pool.
        /// </summary>
        private static string TaoConnectionString(string dbPath, bool readOnly, int timeoutGiay = 30, bool tatKhoaNgoai = false)
        {
            var sb = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWrite,
                Pooling = false,
                Cache = SqliteCacheMode.Private,
                DefaultTimeout = timeoutGiay
            };
            if (tatKhoaNgoai) sb.ForeignKeys = false; // DELETE ... không được kéo theo xóa dây chuyền sang bảng khác
            return sb.ToString();
        }
        /// <summary>
        /// [RAM] Dùng RAM có chừng mực để chạy nhanh: bộ đệm trang ≈ kích thước tệp (8–128 MB), bảng tạm trong RAM,
        /// và với kết nối chỉ đọc thì thêm memory-map để đọc không qua bộ đệm trung gian.
        /// </summary>
        private static void ApDungCauHinhRam(SqliteConnection ketNoi, long kichThuocTep, bool docChiDoc)
        {
            long mb = Math.Clamp(kichThuocTep / (1024 * 1024) + 8, 8, 128);
            string sql = $"PRAGMA cache_size = -{(mb * 1024).ToString(CultureInfo.InvariantCulture)}; PRAGMA temp_store = MEMORY;";
            if (docChiDoc)
                sql += $" PRAGMA mmap_size = {Math.Min(kichThuocTep, 256L * 1024 * 1024).ToString(CultureInfo.InvariantCulture)};";
            using var lenh = ketNoi.CreateCommand();
            lenh.CommandText = sql;
            lenh.ExecuteNonQuery();
        }
        private static string TrichDanhTuSql(string ten) => "\"" + ten.Replace("\"", "\"\"") + "\"";
        /// <summary>[AN TOÀN] Tên bảng cuối cùng cũng đi vào tên tệp: không cho phép ký tự đường dẫn.</summary>
        private static void KiemTraTenBang(string tenBang)
        {
            if (string.IsNullOrWhiteSpace(tenBang) || tenBang.Length > 128)
                throw new ArgumentException("Tên bảng không hợp lệ.", nameof(tenBang));
            if (tenBang.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException($"Tên bảng '{tenBang}' chứa ký tự không dùng được làm tên tệp.", nameof(tenBang));
        }
        /// <summary>[AN TOÀN] Chỉ làm việc với bảng THẬT SỰ có trong CSDL (tra bằng tham số, không ghép chuỗi).</summary>
        private static bool BangTonTai(SqliteConnection ketNoi, string tenBang)
        {
            using var lenh = ketNoi.CreateCommand();
            lenh.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name = @ten COLLATE NOCASE " +
                               "AND name NOT LIKE 'sqlite\\_%' ESCAPE '\\' LIMIT 1;";
            lenh.Parameters.AddWithValue("@ten", tenBang);
            return lenh.ExecuteScalar() != null;
        }
        private static long LayGiaTriSo(SqliteConnection ketNoi, string sql)
        {
            using var lenh = ketNoi.CreateCommand();
            lenh.CommandText = sql;
            object? kq = lenh.ExecuteScalar();
            return kq == null || kq == DBNull.Value ? 0 : Convert.ToInt64(kq, CultureInfo.InvariantCulture);
        }
        private static string LayGiaTriChuoi(SqliteConnection ketNoi, string sql)
        {
            using var lenh = ketNoi.CreateCommand();
            lenh.CommandText = sql;
            return Convert.ToString(lenh.ExecuteScalar(), CultureInfo.InvariantCulture) ?? "Unknown";
        }
        // ==================== TỐI ƯU CSDL ====================
        /// <summary>
        /// VACUUM chỉ khi thật sự có rác (≥ 5% trang trống) — trước đây luôn VACUUM + ANALYZE toàn bộ sau MỖI lần nạp,
        /// rất nặng với CSDL lớn. Thay ANALYZE bằng PRAGMA optimize (nhẹ), rồi gộp tệp WAL về tệp chính.
        /// Không ném lỗi: đây chỉ là bước dọn gọn, dữ liệu đã an toàn từ trước.
        /// </summary>
        public static void VacuumDatabase(string duongDanDb)
        {
            if (string.IsNullOrWhiteSpace(duongDanDb) || !File.Exists(duongDanDb)) return;
            string fullPath = Path.GetFullPath(duongDanDb);
            try
            {
                using var ketNoi = new SqliteConnection(TaoConnectionString(fullPath, false, 180));
                ketNoi.Open();
                // Lưu ý: KHÔNG bật temp_store=MEMORY ở đây, VACUUM dựng bản tạm cỡ cả CSDL, để trong RAM sẽ tràn bộ nhớ.
                long pageCount = LayGiaTriSo(ketNoi, "PRAGMA page_count;");
                long freePages = LayGiaTriSo(ketNoi, "PRAGMA freelist_count;");
                bool canVacuum = pageCount > 0 && freePages * 100.0 / pageCount >= 5.0;
                using var lenh = ketNoi.CreateCommand();
                lenh.CommandTimeout = 180;
                if (canVacuum)
                {
                    lenh.CommandText = "VACUUM;";
                    lenh.ExecuteNonQuery();
                }
                lenh.CommandText = "PRAGMA optimize;";
                lenh.ExecuteNonQuery();
                lenh.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                lenh.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[VacuumDatabase] " + ex.Message);
            }
            finally
            {
                XoaCache(fullPath);
            }
        }
        // ==================== TÌM CSDL ====================
        public static List<string> NoiVongTayLonKetNoiTimKiemCSDL()
        {
            HashSet<string> ketQua = new(StringComparer.OrdinalIgnoreCase);
            try
            {
                string thuMucDatabase = Path.Combine(AppContext.BaseDirectory, "Database");
                if (Directory.Exists(thuMucDatabase))
                {
                    foreach (string file in Directory.EnumerateFiles(thuMucDatabase, "*.db", SearchOption.TopDirectoryOnly))
                        ketQua.Add(Path.GetFullPath(file));
                }
                string thuMucDesktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), TEN_THU_MUC_GOI);
                if (Directory.Exists(thuMucDesktop))
                {
                    // Chỉ quét TopDirectoryOnly. Tuyệt đối không cho càn quét sâu.
                    foreach (string file in Directory.EnumerateFiles(thuMucDesktop, "*.db", SearchOption.TopDirectoryOnly))
                        ketQua.Add(Path.GetFullPath(file));
                }
            }
            catch (Exception ex) { Debug.WriteLine("[NoiVongTayLonKetNoiTimKiemCSDL] " + ex.Message); }
            var danhSach = ketQua.ToList();
            danhSach.Sort(StringComparer.OrdinalIgnoreCase); // thứ tự ổn định cho giao diện
            return danhSach;
        }
        /// <summary>Tìm thư mục gói dữ liệu đã tồn tại (Desktop chuẩn, Desktop theo profile, Desktop OneDrive).</summary>
        public static string? TimThuMucGoiDuLieu()
        {
            var ungVien = new List<string>(3);
            void Them(string? p)
            {
                if (!string.IsNullOrWhiteSpace(p) && !ungVien.Contains(p, StringComparer.OrdinalIgnoreCase))
                    ungVien.Add(p);
            }
            Them(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(profile))
            {
                Them(Path.Combine(profile, "Desktop"));
                Them(Path.Combine(profile, "OneDrive", "Desktop"));
            }
            foreach (string desktop in ungVien)
            {
                string thuMuc = Path.Combine(desktop, TEN_THU_MUC_GOI);
                if (Directory.Exists(thuMuc)) return thuMuc;
            }
            return null;
        }
        // ==================== DANH SÁCH BẢNG ====================
        /// <summary>
        /// Tệp không tồn tại → danh sách rỗng. Lỗi thật (tệp hỏng, bị khóa...) → ném ngoại lệ để nơi gọi báo đúng lỗi
        /// (trước đây nuốt lỗi và trả về danh sách rỗng, khiến CSDL hỏng bị hiểu nhầm là "không có bảng nào").
        /// </summary>
        public static List<string> LayDanhSachBang(string duongDanDb)
        {
            if (string.IsNullOrWhiteSpace(duongDanDb) || !File.Exists(duongDanDb))
                return new List<string>();
            string fullPath = Path.GetFullPath(duongDanDb);
            DauVanTay dau = LayDauVanTay(fullPath);
            if (_cacheDanhSachBang.TryGetValue(fullPath, out MucDem? cache) && cache.DauVanTay == dau)
                return new List<string>(cache.DuLieu);
            var danhSachBang = new List<string>(64);
            using (var ketNoi = new SqliteConnection(TaoConnectionString(fullPath, true, 5)))
            {
                ketNoi.Open();
                using var lenh = ketNoi.CreateCommand();
                lenh.CommandText = "SELECT name FROM sqlite_master WHERE type='table' " +
                                   "AND name NOT LIKE 'sqlite\\_%' ESCAPE '\\' ORDER BY name COLLATE NOCASE;";
                using var reader = lenh.ExecuteReader();
                while (reader.Read())
                {
                    if (reader.IsDBNull(0)) continue;
                    string ten = reader.GetString(0);
                    if (!string.IsNullOrWhiteSpace(ten)) danhSachBang.Add(ten);
                }
            }
            danhSachBang.TrimExcess();
            _cacheDanhSachBang[fullPath] = new MucDem(dau, new List<string>(danhSachBang));
            return danhSachBang;
        }
        // ==================== THÔNG TIN CHI TIẾT CSDL ====================
        /// <summary>
        /// Phân tích vật lý/engine của CSDL. NẶNG (đọc tệp, kiểm tra toàn vẹn) → luôn gọi từ luồng nền rồi đưa kết quả
        /// cho InThongTinDatabaseLenRichTextBox(..., thongTinChiTiet). Kết quả được giữ trong RAM theo dấu vân tay tệp.
        /// </summary>
        public static List<string> LayThongTinChiTietCSDL(string duongDanDb)
        {
            var thongTin = new List<string>(64);
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
                DauVanTay dau = LayDauVanTay(fullPath);
                if (_cacheThongTin.TryGetValue(fullPath, out MucDem? cache) && cache.DauVanTay == dau)
                    return new List<string>(cache.DuLieu);
                var info = new FileInfo(fullPath);
                thongTin.Add("Thông tin Database");
                thongTin.Add("");
                thongTin.Add($"Tên tệp: {info.Name}");
                thongTin.Add($"Dung lượng vật lý: {(info.Length / 1024d / 1024d):F2} MB");
                thongTin.Add($"Ngày tạo: {info.CreationTime:dd/MM/yyyy HH:mm:ss}");
                thongTin.Add($"Chỉnh sửa cuối: {info.LastWriteTime:dd/MM/yyyy HH:mm:ss}");
                thongTin.Add($"Readonly: {info.IsReadOnly}");
                thongTin.Add($"Thuộc tính: {info.Attributes}");
                using var ketNoi = new SqliteConnection(TaoConnectionString(fullPath, true, 10));
                ketNoi.Open();
                long pageSize = LayGiaTriSo(ketNoi, "PRAGMA page_size;");
                long pageCount = LayGiaTriSo(ketNoi, "PRAGMA page_count;");
                long freelistCount = LayGiaTriSo(ketNoi, "PRAGMA freelist_count;");
                long kichThuocThuc = pageSize * pageCount;
                long kichThuocRac = freelistCount * pageSize;
                double phanManh = pageCount > 0 ? (double)freelistCount / pageCount * 100d : 0d;
                thongTin.Add($"Kích thước Page: {pageSize:N0} bytes");
                thongTin.Add($"Tổng số Page: {pageCount:N0}");
                thongTin.Add($"Free Pages: {freelistCount:N0}");
                thongTin.Add($"Dung lượng thực SQLite: {(kichThuocThuc / 1024d / 1024d):F2} MB");
                thongTin.Add($"Dung lượng rác nội bộ: {(kichThuocRac / 1024d / 1024d):F2} MB");
                thongTin.Add($"Mức phân mảnh: {phanManh:F2}%");
                thongTin.Add($"SQLite Version: {LayGiaTriChuoi(ketNoi, "SELECT sqlite_version();")}");
                thongTin.Add($"Journal Mode: {LayGiaTriChuoi(ketNoi, "PRAGMA journal_mode;")}");
                thongTin.Add($"Synchronous: {LayGiaTriChuoi(ketNoi, "PRAGMA synchronous;")}");
                thongTin.Add($"Encoding: {LayGiaTriChuoi(ketNoi, "PRAGMA encoding;")}");
                thongTin.Add($"Auto Vacuum: {LayGiaTriChuoi(ketNoi, "PRAGMA auto_vacuum;")}");
                // Một câu lệnh đếm duy nhất thay cho 4 câu COUNT(*) riêng lẻ
                long soBang = 0, soIndex = 0, soView = 0, soTrigger = 0;
                using (var lenh = ketNoi.CreateCommand())
                {
                    lenh.CommandText = "SELECT type, COUNT(*) FROM sqlite_master " +
                                       "WHERE type IN ('table','index','view','trigger') " +
                                       "AND NOT (type='table' AND name LIKE 'sqlite\\_%' ESCAPE '\\') GROUP BY type;";
                    using var reader = lenh.ExecuteReader();
                    while (reader.Read())
                    {
                        long n = reader.GetInt64(1);
                        switch (reader.GetString(0))
                        {
                            case "table": soBang = n; break;
                            case "index": soIndex = n; break;
                            case "view": soView = n; break;
                            case "trigger": soTrigger = n; break;
                        }
                    }
                }
                thongTin.Add($"Số bảng dữ liệu: {soBang:N0}");
                thongTin.Add($"Số Index: {soIndex:N0}");
                thongTin.Add($"Số View: {soView:N0}");
                thongTin.Add($"Số Trigger: {soTrigger:N0}");
                // [HIỆU NĂNG] integrity_check quét TOÀN BỘ tệp (O(kích thước)). CSDL lớn dùng quick_check (nhanh, vẫn bắt được
                // phần lớn hỏng cấu trúc) để việc chọn CSDL không bị đứng hình.
                string integrityResult;
                using (var lenh = ketNoi.CreateCommand())
                {
                    lenh.CommandTimeout = 60;
                    lenh.CommandText = kichThuocThuc > NGUONG_KIEM_TRA_DAY_DU_BYTES ? "PRAGMA quick_check;" : "PRAGMA integrity_check;";
                    integrityResult = Convert.ToString(lenh.ExecuteScalar(), CultureInfo.InvariantCulture) ?? "Unknown";
                }
                thongTin.Add($"Integrity Check: {integrityResult}");
                string walFile = fullPath + "-wal";
                string shmFile = fullPath + "-shm";
                bool coWal = File.Exists(walFile);
                thongTin.Add($"WAL tồn tại: {coWal}");
                thongTin.Add($"SHM tồn tại: {File.Exists(shmFile)}");
                if (coWal)
                    thongTin.Add($"Dung lượng WAL: {(new FileInfo(walFile).Length / 1024d / 1024d):F2} MB");
                _cacheThongTin[fullPath] = new MucDem(dau, new List<string>(thongTin));
                return thongTin;
            }
            catch (SqliteException ex) { thongTin.Add("[SQLite Error] " + ex.Message); return thongTin; }
            catch (IOException ex) { thongTin.Add("[IO Error] " + ex.Message); return thongTin; }
            catch (UnauthorizedAccessException ex) { thongTin.Add("[Access Error] " + ex.Message); return thongTin; }
            catch (Exception ex) { thongTin.Add("[Unknown Error] " + ex.Message); return thongTin; }
        }
        // ==================== XUẤT DỮ LIỆU RA JSON ====================
        /// <summary>
        /// Xuất một bảng ra JSON (định dạng cũ: mảng các đối tượng, thụt lề) theo DÒNG.
        /// [RAM] Trước đây nạp cả bảng vào List&lt;Dictionary&gt; rồi mới Serialize: RAM tăng theo kích thước bảng (bảng 1 triệu dòng
        /// có thể ngốn hàng GB). Nay mỗi lúc chỉ giữ một dòng + một bộ đệm nhỏ.
        /// [AN TOÀN] Ghi vào tệp tạm, xong mới đổi tên đè: xuất lỗi/bị dừng giữa chừng không bao giờ làm hỏng tệp JSON của đợt xuất trước.
        /// </summary>
        public static void XuatDuLieuRaJson(string duongDanDb, string tenBang, string thuMucLuu, CancellationToken huy = default)
        {
            KiemTraTenBang(tenBang);
            if (!File.Exists(duongDanDb))
                throw new FileNotFoundException("Không tìm thấy CSDL nguồn để xuất.", duongDanDb);
            string fullPath = Path.GetFullPath(duongDanDb);
            string fileJson = Path.Combine(thuMucLuu, $"{tenBang}.json");
            string fileTam = $"{fileJson}.{Guid.NewGuid():N}.tmp";
            bool hoanTat = false;
            try
            {
                Directory.CreateDirectory(thuMucLuu);
                using (var ketNoi = new SqliteConnection(TaoConnectionString(fullPath, true)))
                {
                    ketNoi.Open();
                    ApDungCauHinhRam(ketNoi, new FileInfo(fullPath).Length, docChiDoc: true);
                    if (!BangTonTai(ketNoi, tenBang))
                        throw new InvalidOperationException($"Bảng '{tenBang}' không tồn tại trong CSDL nguồn.");
                    using var lenh = ketNoi.CreateCommand();
                    lenh.CommandText = "SELECT * FROM " + TrichDanhTuSql(tenBang) + ";";
                    using var reader = lenh.ExecuteReader();
                    using var fs = new FileStream(fileTam, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                        BO_DEM_TEP, FileOptions.SequentialScan);
                    using var writer = new Utf8JsonWriter(fs, _tuyChonGhiJson);
                    int soCot = reader.FieldCount;
                    // Mã hóa sẵn tên cột MỘT lần, không mã hóa lại ở từng dòng
                    var tenCot = new JsonEncodedText[soCot];
                    for (int i = 0; i < soCot; i++) tenCot[i] = JsonEncodedText.Encode(reader.GetName(i), _boMaHoa);
                    writer.WriteStartArray();
                    long soDong = 0;
                    while (reader.Read())
                    {
                        writer.WriteStartObject();
                        for (int i = 0; i < soCot; i++)
                        {
                            writer.WritePropertyName(tenCot[i]);
                            GhiGiaTriJson(writer, reader.GetValue(i));
                        }
                        writer.WriteEndObject();
                        if ((++soDong & 0x3FF) == 0)
                        {
                            huy.ThrowIfCancellationRequested();
                            if (writer.BytesPending > 64 * 1024) writer.Flush(); // đẩy xuống đĩa, giữ RAM phẳng
                        }
                    }
                    writer.WriteEndArray();
                    writer.Flush();
                    fs.Flush(true); // ép xuống ổ đĩa vật lý trước khi đổi tên
                }
                if (File.Exists(fileJson)) File.SetAttributes(fileJson, FileAttributes.Normal);
                File.Move(fileTam, fileJson, overwrite: true);
                hoanTat = true;
                CapNhatHeartbeat();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new IOException($"Lỗi export bảng {tenBang}: {ex.Message}", ex);
            }
            finally
            {
                if (!hoanTat)
                {
                    try { if (File.Exists(fileTam)) File.Delete(fileTam); } catch { }
                }
            }
        }
        private static void GhiGiaTriJson(Utf8JsonWriter w, object? v)
        {
            switch (v)
            {
                case null:
                case DBNull:
                    w.WriteNullValue();
                    break;
                case long l:
                    w.WriteNumberValue(l);
                    break;
                case double d:
                    if (double.IsFinite(d)) w.WriteNumberValue(d);
                    else w.WriteStringValue(d.ToString(CultureInfo.InvariantCulture)); // JSON không có Infinity/NaN
                    break;
                case string s:
                    w.WriteStringValue(s);
                    break;
                case byte[] b:
                    w.WriteBase64StringValue(b); // BLOB ⇄ base64 (khi nạp, cột khai báo BLOB sẽ được giải mã ngược lại)
                    break;
                default:
                    w.WriteStringValue(Convert.ToString(v, CultureInfo.InvariantCulture));
                    break;
            }
        }
        // ==================== NẠP DỮ LIỆU TỪ JSON ====================
        /// <summary>
        /// Nạp một bảng từ JSON, theo DÒNG, trong MỘT giao dịch (lỗi bất kỳ → hoàn tác toàn bộ bảng đó).
        /// Các tham số duongDanFileBackup / thuMucGoiCon được GIỮ LẠI để không vỡ nơi gọi nhưng không còn dùng:
        /// trước đây hàm tự xóa tệp JSON, tệp sao lưu và thư mục ngay sau MỖI bảng (bản sao lưu biến mất sau bảng đầu tiên,
        /// các bảng sau không còn gì để khôi phục). Việc dọn dẹp nay do nơi gọi làm SAU KHI cả CSDL nạp xong không lỗi.
        /// </summary>
        public static void NhapDuLieuVaoCSDL(
            string duongDanJson, string duongDanDb, string tenBang, bool xoaDuLieuCu,
            string duongDanFileBackup = "", string thuMucGoiCon = "", CancellationToken huy = default)
        {
            KiemTraTenBang(tenBang);
            if (!File.Exists(duongDanJson))
                throw new FileNotFoundException("Không tìm thấy tệp JSON nguồn.", duongDanJson);
            if (!File.Exists(duongDanDb))
                throw new FileNotFoundException("Không tìm thấy CSDL đích để nạp.", duongDanDb);
            string fullPathDb = Path.GetFullPath(duongDanDb);
            using var ketNoi = new SqliteConnection(TaoConnectionString(fullPathDb, false, 60, tatKhoaNgoai: true));
            ketNoi.Open();
            ApDungCauHinhRam(ketNoi, new FileInfo(fullPathDb).Length, docChiDoc: false);
            // [LOGIC] Trước đây: bảng không tồn tại hoặc không cột nào khớp → "thành công" với 0 dòng rồi XÓA tệp JSON nguồn = mất dữ liệu âm thầm.
            if (!BangTonTai(ketNoi, tenBang))
                throw new InvalidOperationException($"Bảng '{tenBang}' không tồn tại trong CSDL đích nên không thể nạp.");
            Dictionary<string, bool> cotCuaBang = LayCotBang(ketNoi, tenBang); // tên cột → khai báo BLOB?
            using var fs = new FileStream(duongDanJson, FileMode.Open, FileAccess.Read, FileShare.Read,
                BO_DEM_TEP, FileOptions.SequentialScan);
            using SqliteTransaction giaoDich = ketNoi.BeginTransaction();
            bool daCommit = false;
            try
            {
                using var lenh = ketNoi.CreateCommand();
                lenh.Transaction = giaoDich;
                string tenBangSql = TrichDanhTuSql(tenBang);
                if (xoaDuLieuCu)
                {
                    lenh.CommandText = $"DELETE FROM {tenBangSql};";
                    lenh.ExecuteNonQuery();
                    try
                    {
                        lenh.CommandText = "DELETE FROM sqlite_sequence WHERE name = @ten;";
                        lenh.Parameters.AddWithValue("@ten", tenBang);
                        lenh.ExecuteNonQuery();
                    }
                    catch (SqliteException) { /* CSDL không có AUTOINCREMENT nên không có sqlite_sequence */ }
                    lenh.Parameters.Clear();
                }
                // Câu lệnh INSERT được dựng từ DÒNG ĐẦU TIÊN (giống hành vi cũ), sau đó dùng lại cho mọi dòng.
                SqliteParameter[]? thamSo = null;
                byte[][]? tenCotUtf8 = null;
                bool[]? laBlob = null;
                long soDong = 0;
                DocMangJsonTheoLuong(fs, dong =>
                {
                    if (thamSo == null)
                    {
                        var cotChon = new List<string>();
                        foreach (JsonProperty p in dong.EnumerateObject())
                        {
                            if (cotCuaBang.ContainsKey(p.Name)) cotChon.Add(p.Name);
                        }
                        if (cotChon.Count == 0)
                            throw new InvalidDataException($"Không có cột nào trong tệp JSON khớp với bảng '{tenBang}'. Đã hủy nạp để không mất dữ liệu.");
                        string cotSql = string.Join(",", cotChon.Select(TrichDanhTuSql));
                        string thamSoSql = string.Join(",", cotChon.Select((_, i) => $"@p{i}"));
                        lenh.CommandText = $"INSERT OR REPLACE INTO {tenBangSql} ({cotSql}) VALUES ({thamSoSql});";
                        thamSo = new SqliteParameter[cotChon.Count];
                        tenCotUtf8 = new byte[cotChon.Count][];
                        laBlob = new bool[cotChon.Count];
                        for (int i = 0; i < cotChon.Count; i++)
                        {
                            SqliteParameter tham = lenh.CreateParameter();
                            tham.ParameterName = $"@p{i}";
                            lenh.Parameters.Add(tham);
                            thamSo[i] = tham; // giữ tham chiếu: không tra cứu tham số theo tên ở từng ô dữ liệu
                            tenCotUtf8[i] = System.Text.Encoding.UTF8.GetBytes(cotChon[i]);
                            laBlob[i] = cotCuaBang[cotChon[i]];
                        }
                        lenh.Prepare();
                    }
                    for (int i = 0; i < thamSo.Length; i++)
                    {
                        thamSo[i].Value = dong.TryGetProperty(tenCotUtf8![i], out JsonElement giaTri)
                            ? ChuyenGiaTriJson(giaTri, laBlob![i])
                            : DBNull.Value;
                    }
                    lenh.ExecuteNonQuery();
                    if ((++soDong & 0x7FF) == 0) huy.ThrowIfCancellationRequested();
                }, huy);
                giaoDich.Commit();
                daCommit = true;
                XoaCache(fullPathDb);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"Tệp JSON '{Path.GetFileName(duongDanJson)}' không hợp lệ: {ex.Message}", ex);
            }
            finally
            {
                if (!daCommit)
                {
                    try { giaoDich.Rollback(); } catch (Exception ex) { Debug.WriteLine("[Rollback] " + ex.Message); }
                }
            }
        }
        private static Dictionary<string, bool> LayCotBang(SqliteConnection ketNoi, string tenBang)
        {
            var ketQua = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            using var lenh = ketNoi.CreateCommand();
            lenh.CommandText = "SELECT name, type FROM pragma_table_info(@ten);";
            lenh.Parameters.AddWithValue("@ten", tenBang);
            using var reader = lenh.ExecuteReader();
            while (reader.Read())
            {
                string kieu = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                ketQua[reader.GetString(0)] = kieu.Contains("BLOB", StringComparison.OrdinalIgnoreCase);
            }
            return ketQua;
        }
        /// <summary>
        /// [LỖI DỮ LIỆU ĐÃ SỬA] Bản cũ đổi MỌI chuỗi trông giống ngày tháng thành DateTime (theo văn hóa máy) rồi ghi lại:
        /// "05/06/2026" có thể thành 6 tháng 5 hoặc 5 tháng 6, và định dạng lưu trong CSDL bị đổi. Nay chuỗi được ghi NGUYÊN VĂN.
        /// </summary>
        private static object ChuyenGiaTriJson(JsonElement giaTri, bool cotBlob)
        {
            switch (giaTri.ValueKind)
            {
                case JsonValueKind.String:
                    if (cotBlob && giaTri.TryGetBytesFromBase64(out byte[]? nhiPhan)) return nhiPhan;
                    return giaTri.GetString() ?? string.Empty;
                case JsonValueKind.Number:
                    if (giaTri.TryGetInt64(out long l)) return l;
                    return giaTri.TryGetDouble(out double d) ? d : giaTri.GetRawText();
                case JsonValueKind.True: return 1L;
                case JsonValueKind.False: return 0L;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return DBNull.Value;
                default:
                    return giaTri.GetRawText();
            }
        }
        /// <summary>
        /// Đọc tệp JSON dạng mảng đối tượng theo luồng. Mỗi lần chỉ giữ MỘT dòng trong RAM (JsonDocument của dòng đó được giải phóng
        /// ngay sau khi xử lý), thay cho việc Deserialize cả tệp thành List&lt;Dictionary&lt;string, JsonElement&gt;&gt;.
        /// </summary>
        private static long DocMangJsonTheoLuong(Stream luong, Action<JsonElement> xuLyDong, CancellationToken huy)
        {
            byte[] dem = ArrayPool<byte>.Shared.Rent(BO_DEM_TEP);
            int soByte = 0, viTri = 0;
            bool het = false, daMoMang = false;
            long soDong = 0;
            // Nạp thêm dữ liệu: dồn phần chưa xử lý về đầu, nới bộ đệm nếu một dòng dài hơn bộ đệm hiện có.
            bool NapThem()
            {
                if (het) return false;
                if (viTri > 0)
                {
                    Buffer.BlockCopy(dem, viTri, dem, 0, soByte - viTri);
                    soByte -= viTri;
                    viTri = 0;
                }
                if (soByte == dem.Length)
                {
                    if (dem.Length >= GIOI_HAN_BO_DEM_MOT_DONG)
                        throw new InvalidDataException("Một dòng dữ liệu trong tệp JSON lớn bất thường, đã dừng để bảo vệ bộ nhớ.");
                    byte[] moi = ArrayPool<byte>.Shared.Rent(dem.Length * 2);
                    Buffer.BlockCopy(dem, 0, moi, 0, soByte);
                    ArrayPool<byte>.Shared.Return(dem);
                    dem = moi;
                }
                int n = luong.Read(dem, soByte, dem.Length - soByte);
                if (n <= 0)
                {
                    het = true;
                    return false;
                }
                soByte += n;
                return true;
            }
            try
            {
                // Bỏ BOM UTF-8 nếu có
                while (soByte - viTri < 3 && NapThem()) { }
                if (soByte - viTri >= 3 && dem[viTri] == 0xEF && dem[viTri + 1] == 0xBB && dem[viTri + 2] == 0xBF)
                    viTri += 3;
                while (true)
                {
                    // Bỏ khoảng trắng và dấu phẩy ngăn cách giữa các dòng
                    while (true)
                    {
                        while (viTri < soByte && dem[viTri] is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n' or (byte)',')
                            viTri++;
                        if (viTri < soByte) break;
                        if (!NapThem())
                            throw new InvalidDataException(daMoMang ? "Tệp JSON bị cắt cụt (thiếu dấu ']')." : "Tệp JSON rỗng.");
                    }
                    byte b = dem[viTri];
                    if (!daMoMang)
                    {
                        if (b != (byte)'[') throw new InvalidDataException("Tệp JSON không phải một mảng dữ liệu.");
                        daMoMang = true;
                        viTri++;
                        continue;
                    }
                    if (b == (byte)']') return soDong;
                    if (b != (byte)'{') throw new InvalidDataException("Phần tử trong tệp JSON không phải là một dòng dữ liệu (đối tượng).");
                    while (true)
                    {
                        // Bộ đọc mới cho từng dòng, bắt đầu đúng ranh giới phần tử nên không cần mang trạng thái qua các lần nạp
                        var reader = new Utf8JsonReader(dem.AsSpan(viTri, soByte - viTri), het, default);
                        if (reader.Read() && JsonDocument.TryParseValue(ref reader, out JsonDocument? tai))
                        {
                            int daDung = (int)reader.BytesConsumed;
                            using (tai) xuLyDong(tai!.RootElement);
                            viTri += daDung;
                            soDong++;
                            if ((soDong & 0x3FF) == 0) huy.ThrowIfCancellationRequested();
                            break;
                        }
                        if (!NapThem())
                            throw new InvalidDataException("Tệp JSON bị cắt cụt giữa một dòng dữ liệu.");
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(dem);
            }
        }
        // ==================== SAO LƯU TRƯỚC KHI NẠP ====================
        /// <summary>
        /// Tạo điểm khôi phục bảo hiểm. [AN TOÀN] Dùng SQLite Backup API (bản chụp nhất quán tại một thời điểm, có cả dữ liệu trong WAL)
        /// rồi kiểm tra lại bản sao. Bản cũ chép thô tệp .db trong khi CSDL có thể đang ghi/đang ở chế độ WAL, có thể ra bản sao
        /// hỏng mà vẫn báo "đã sao lưu".
        /// </summary>
        public static string SaoLuuCSDLTruocKhiNhap(string duongDanDb)
        {
            if (string.IsNullOrWhiteSpace(duongDanDb))
                throw new ArgumentException("Đường dẫn CSDL không hợp lệ.", nameof(duongDanDb));
            string fullPathDb = Path.GetFullPath(duongDanDb);
            if (!File.Exists(fullPathDb))
                throw new FileNotFoundException("Không tìm thấy tệp database gốc để tiến hành sao lưu bảo hiểm.", fullPathDb);
            string thuMucMeDesktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), TEN_THU_MUC_GOI);
            string thuMucConBackup = Path.Combine(thuMucMeDesktop, TEN_THU_MUC_BACKUP);
            Directory.CreateDirectory(thuMucConBackup);
            DonBanSaoLuuQuaHan(thuMucConBackup);
            string tenFileGoc = Path.GetFileNameWithoutExtension(fullPathDb);
            string duongDanBackup = Path.Combine(thuMucConBackup, $"{tenFileGoc}_BackupTruocKhiNhap_{DateTime.Now:yyyyMMdd_HHmmss}.bak");
            Exception? lastError = null;
            for (int lanThu = 1; lanThu <= 3; lanThu++)
            {
                try
                {
                    SaoLuuBangApiSqlite(fullPathDb, duongDanBackup);
                    lastError = null;
                    break;
                }
                catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
                {
                    lastError = ex;
                    try { if (File.Exists(duongDanBackup)) File.Delete(duongDanBackup); } catch { }
                    Thread.Sleep(300 * lanThu);
                }
            }
            if (lastError != null)
                throw new IOException("Không thể tạo bản sao lưu bảo hiểm: " + lastError.Message, lastError);
            CapNhatHeartbeat();
            return duongDanBackup;
        }
        private static void SaoLuuBangApiSqlite(string nguon, string dich)
        {
            var chuoiDich = new SqliteConnectionStringBuilder
            {
                DataSource = dich,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false
            }.ToString();
            using (var ketNoiNguon = new SqliteConnection(TaoConnectionString(nguon, true, 30)))
            using (var ketNoiDich = new SqliteConnection(chuoiDich))
            {
                ketNoiNguon.Open();
                ketNoiDich.Open();
                ketNoiNguon.BackupDatabase(ketNoiDich);
                // Không có bằng chứng bản sao toàn vẹn thì không được coi là đã sao lưu
                string kq = LayGiaTriChuoi(ketNoiDich, "PRAGMA quick_check;");
                if (!kq.Equals("ok", StringComparison.OrdinalIgnoreCase))
                    throw new IOException("Bản sao lưu không vượt qua kiểm tra toàn vẹn: " + kq);
            }
            var fi = new FileInfo(dich);
            if (!fi.Exists || fi.Length == 0)
                throw new IOException("Bản sao lưu rỗng.");
        }
        private static void DonBanSaoLuuQuaHan(string thuMucBackup)
        {
            try
            {
                DateTime mocXoa = DateTime.Now.AddDays(-SO_NGAY_GIU_BACKUP);
                foreach (string file in Directory.EnumerateFiles(thuMucBackup, "*.bak", SearchOption.TopDirectoryOnly).ToArray())
                {
                    try
                    {
                        var fi = new FileInfo(file);
                        if (fi.LastWriteTime < mocXoa)
                        {
                            fi.Attributes = FileAttributes.Normal;
                            fi.Delete();
                        }
                    }
                    catch { /* bỏ qua từng tệp lỗi */ }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Dọn rác backup lỗi] " + ex.Message);
            }
        }
        /// <summary>
        /// Xóa bản sao lưu sau khi CSDL đã nạp XONG và KHÔNG có lỗi (giữ chính sách "không để lại bản sao dữ liệu trên Desktop").
        /// Chỉ xóa đúng tệp .bak nằm trong thư mục sao lưu của phần mềm; thư mục sao lưu rỗng thì dọn luôn.
        /// </summary>
        public static void XoaBanSaoLuuAnToan(string duongDanBackup)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(duongDanBackup) || !File.Exists(duongDanBackup)) return;
                string? thuMuc = Path.GetDirectoryName(Path.GetFullPath(duongDanBackup));
                if (thuMuc == null
                    || !string.Equals(Path.GetFileName(thuMuc), TEN_THU_MUC_BACKUP, StringComparison.OrdinalIgnoreCase)
                    || !duongDanBackup.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                {
                    return; // không phải bản sao lưu do phần mềm tạo: tuyệt đối không đụng tới
                }
                File.SetAttributes(duongDanBackup, FileAttributes.Normal);
                File.Delete(duongDanBackup);
                if (!Directory.EnumerateFileSystemEntries(thuMuc).Any()) Directory.Delete(thuMuc, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[XoaBanSaoLuuAnToan] " + ex.Message);
            }
        }
        // ==================== HIỂN THỊ LÊN RICHTEXTBOX ====================
        // Lưu ý: tạm ngừng VẼ bằng WM_SETREDRAW (SuspendLayout không chặn việc vẽ lại sau mỗi lần AppendText).
        private static class NativeMethods
        {
            public const int WM_SETREDRAW = 0x000B;
            [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        }
        private static void TamDungVe(Control c)
        {
            if (c.IsHandleCreated) NativeMethods.SendMessage(c.Handle, NativeMethods.WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        }
        private static void TiepTucVe(Control c)
        {
            if (!c.IsHandleCreated) return;
            NativeMethods.SendMessage(c.Handle, NativeMethods.WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
            c.Invalidate();
        }
        private const string DUONG_KE = "--------------------------------------------------------------------------------\n";
        public static void InThongTinDatabaseLenRichTextBox(RichTextBox rtb, string duongDanFile, string thongBaoLoi)
        {
            if (rtb == null || rtb.IsDisposed || rtb.Disposing) return;
            Font? fontBold = null;
            TamDungVe(rtb);
            try
            {
                rtb.Clear();
                fontBold = new Font(rtb.Font, FontStyle.Bold);
                void Ghi(string text, Color mau, bool dam = false)
                {
                    if (rtb.IsDisposed || rtb.Disposing) return;
                    rtb.SelectionStart = rtb.TextLength;
                    rtb.SelectionLength = 0;
                    rtb.SelectionColor = mau;
                    rtb.SelectionFont = dam ? fontBold : rtb.Font;
                    rtb.AppendText(text);
                }
                Ghi("❌ LỖI HỆ THỐNG: KHÔNG THỂ TRUY CẬP PHÂN VÙNG DỮ LIỆU\n", MauLoi, true);
                Ghi(DUONG_KE, MauSeparator);
                Ghi(" [THÔNG TIN TIẾN TRÌNH]\n", MauPhu, true);
                Ghi("  • Tệp tin sự cố     : ", Color.Black, true);
                Ghi($"{Path.GetFileName(duongDanFile)}\n", Color.DarkRed);
                Ghi("  • Đường dẫn tệp     : ", Color.Black, true);
                Ghi($"{duongDanFile}\n", Color.DimGray);
                Ghi(DUONG_KE, MauSeparator);
                Ghi(" [CHI TIẾT NGUYÊN NHÂN BÁO VỀ]\n", MauLoi, true);
                Ghi($"  {thongBaoLoi}\n", Color.DarkRed);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[RichTextBox Error Render Fail] " + ex);
            }
            finally
            {
                fontBold?.Dispose();
                if (!rtb.IsDisposed && !rtb.Disposing) TiepTucVe(rtb);
            }
        }
        /// <summary>Tương thích cũ: tự lấy thông tin chi tiết (có thể nặng, nếu gọi từ luồng UI sẽ làm đứng giao diện).</summary>
        public static void InThongTinDatabaseLenRichTextBox(RichTextBox rtb, string duongDanFile, List<string> danhSachBang) =>
            InThongTinDatabaseLenRichTextBox(rtb, duongDanFile, danhSachBang, null);
        /// <summary>
        /// Bản khuyến nghị: nơi gọi lấy sẵn <paramref name="thongTinChiTiet"/> (LayThongTinChiTietCSDL) ở luồng nền,
        /// hàm này chỉ còn việc vẽ nên không làm đứng giao diện.
        /// </summary>
        public static void InThongTinDatabaseLenRichTextBox(
            RichTextBox rtb, string duongDanFile, List<string> danhSachBang, List<string>? thongTinChiTiet)
        {
            if (rtb == null || rtb.IsDisposed || rtb.Disposing) return;
            danhSachBang ??= new List<string>();
            Font? fontBold = null;
            TamDungVe(rtb);
            try
            {
                rtb.Clear();
                fontBold = new Font(rtb.Font, FontStyle.Bold);
                List<string> thongTinGoc = thongTinChiTiet ?? LayThongTinChiTietCSDL(duongDanFile);
                var dictMeta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                bool coLoi = false;
                foreach (string line in thongTinGoc)
                {
                    if (line.Contains("Lỗi", StringComparison.OrdinalIgnoreCase)) coLoi = true;
                    int pos = line.IndexOf(':');
                    if (pos <= 0) continue;
                    dictMeta[line[..pos].Trim()] = line[(pos + 1)..].Trim();
                }
                void Ghi(string text, Color mau, bool dam = false)
                {
                    if (rtb.IsDisposed || rtb.Disposing) return;
                    rtb.SelectionStart = rtb.TextLength;
                    rtb.SelectionLength = 0;
                    rtb.SelectionColor = mau;
                    rtb.SelectionFont = dam ? fontBold : rtb.Font;
                    rtb.AppendText(text);
                }
                string tenFile = Path.GetFileName(duongDanFile);
                string dungLuongPhysical = dictMeta.GetValueOrDefault("Dung lượng vật lý", "Không rõ");
                string dungLuongSqlite = dictMeta.GetValueOrDefault("Dung lượng thực SQLite", "Không rõ");
                string mucPhanManh = dictMeta.GetValueOrDefault("Mức phân mảnh", "0.00%");
                string triggersCount = dictMeta.GetValueOrDefault("Số Trigger", "0");
                string integrity = dictMeta.GetValueOrDefault("Integrity Check", "Unknown");
                Ghi("THÔNG TIN PHÂN VÙNG CSDL CHỌN GIAO NHẬN CSDL\n", MauTieuDe, true);
                Ghi(DUONG_KE, MauSeparator);
                Ghi(" [VẬT LÝ TỆP TIN]\n", MauPhu, true);
                Ghi("  • Tên tệp dữ liệu   : ", Color.Black, true);
                Ghi($"{tenFile}\n", Color.DarkCyan);
                Ghi("  • Dung lượng ổ đĩa  : ", Color.Black, true);
                Ghi($"{dungLuongPhysical}\n", Color.DarkMagenta);
                Ghi("  • Trạng thái tệp    : ", Color.Black, true);
                if (integrity.Equals("ok", StringComparison.OrdinalIgnoreCase))
                    Ghi("Online ➜ Cấu trúc Toàn vẹn an toàn\n", MauThanhCong, true);
                else if (coLoi)
                    Ghi("Hỏng cấu trúc hoặc Tệp bị khóa chặn\n", MauLoi, true);
                else
                    Ghi("Kết nối Online\n", MauThanhCong, true);
                Ghi(DUONG_KE, MauSeparator);
                Ghi(" [CẤU HÌNH THỜI GIAN THỰC & CHỈ SỐ ENGINE]\n", MauPhu, true);
                Ghi("  • Phiên bản SQLite  : ", Color.Black, true);
                Ghi($"v{dictMeta.GetValueOrDefault("SQLite Version", "3.x")}\n", Color.Black);
                Ghi("  • Dung lượng thực   : ", Color.Black, true);
                Ghi($"{dungLuongSqlite}\n", Color.DarkSlateBlue);
                Ghi("  • Dung lượng rác    : ", Color.Black, true);
                Ghi($"{dictMeta.GetValueOrDefault("Dung lượng rác nội bộ", "0.00 MB")}\n", Color.Brown);
                Ghi("  • Mức phân mảnh     : ", Color.Black, true);
                double.TryParse(mucPhanManh.Replace("%", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out double phanTramFrag);
                Ghi($"{mucPhanManh}\n", phanTramFrag > 15.0 ? MauLoi : MauThanhCong, phanTramFrag > 15.0);
                Ghi("  • Triggers bảo vệ   : ", Color.Black, true);
                Ghi($"{triggersCount} bộ giám sát\n", Color.DarkGoldenrod);
                Ghi("  • Nhật ký WAL Mode  : ", Color.Black, true);
                bool walOn = dictMeta.GetValueOrDefault("WAL tồn tại", "False").Equals("True", StringComparison.OrdinalIgnoreCase);
                Ghi(walOn ? "Đang kích hoạt (Tăng tốc ghi ngầm)\n" : "Tắt\n", Color.Teal);
                Ghi(DUONG_KE, MauSeparator);
                Ghi($"DANH SÁCH THÀNH PHẦN CẤU TRÚC ({danhSachBang.Count} BẢNG DỮ LIỆU)\n", MauThongTin, true);
                Ghi("  Mọi bảng dưới đây sẽ được dời phân vùng tự động sang Json.\n\n", Color.Gray);
                for (int i = 0; i < danhSachBang.Count; i++)
                {
                    Ghi($"   [{i + 1:D2}] ", MauPhu);
                    Ghi(danhSachBang[i], Color.Black, true);
                    Ghi(" ➜ Sẵn sàng xuất bản\n", Color.MediumSeaGreen);
                }
                rtb.SelectionStart = 0;
                rtb.SelectionLength = 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[RichTextBox Render Error] " + ex);
                if (!rtb.IsDisposed && !rtb.Disposing)
                {
                    rtb.Clear();
                    rtb.AppendText("LỖI HIỂN THỊ ĐỊNH DẠNG DATA:\r\n" + ex.Message);
                }
            }
            finally
            {
                fontBold?.Dispose();
                if (!rtb.IsDisposed && !rtb.Disposing) TiepTucVe(rtb);
            }
        }
        // ==================== HEARTBEAT & TỰ HỦY PHÂN VÙNG BỎ HOANG ====================
        private static void CapNhatHeartbeat()
        {
            try
            {
                string thuMucGoc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), TEN_THU_MUC_GOI);
                Directory.CreateDirectory(thuMucGoc);
                string touchFile = Path.Combine(thuMucGoc, "system.touch");
                if (!File.Exists(touchFile))
                {
                    using (File.Create(touchFile)) { }
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
        private static bool LaLienKet(string duongDan)
        {
            try { return (File.GetAttributes(duongDan) & FileAttributes.ReparsePoint) != 0; }
            catch { return true; } // không đọc được thuộc tính: coi như không an toàn để xóa
        }
        private static void XoaThuMucAnToan(string thuMuc)
        {
            var tuyChon = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint // không đi theo liên kết sang nơi khác
            };
            foreach (string file in Directory.EnumerateFiles(thuMuc, "*", tuyChon).ToArray())
            {
                try { File.SetAttributes(file, FileAttributes.Normal); } catch { }
            }
            Directory.Delete(thuMuc, true);
        }
        /// <summary>
        /// Xóa gói dữ liệu trên Desktop nếu bỏ hoang quá <paramref name="soNgayToiDaKhongDung"/> ngày.
        /// [AN TOÀN] Bản sao lưu còn trong thời hạn giữ (7 ngày) KHÔNG bị xóa: trước đây chính phần này có thể xóa mất
        /// điểm khôi phục duy nhất của một lần nạp lỗi sau chỉ 2 ngày.
        /// </summary>
        public static void KiemTraVaHuyRacHeThong(int soNgayToiDaKhongDung = 2)
        {
            try
            {
                if (soNgayToiDaKhongDung < 1) soNgayToiDaKhongDung = 1;
                string thuMucGoc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), TEN_THU_MUC_GOI);
                if (!Directory.Exists(thuMucGoc) || LaLienKet(thuMucGoc)) return;
                // CHỐNG XÓA KHI PHẦN MỀM ĐANG CHẠY (giải phóng các đối tượng Process, trước đây bị bỏ rơi)
                string tenExe = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
                Process[] dangChay = Process.GetProcessesByName(tenExe);
                try
                {
                    if (dangChay.Length > 1) return;
                }
                finally
                {
                    foreach (Process p in dangChay) p.Dispose();
                }
                DateTime lanHoatDongCuoi;
                string touchFile = Path.Combine(thuMucGoc, "system.touch");
                if (File.Exists(touchFile))
                {
                    lanHoatDongCuoi = File.GetLastWriteTimeUtc(touchFile);
                }
                else
                {
                    var dir = new DirectoryInfo(thuMucGoc);
                    lanHoatDongCuoi = dir.LastWriteTimeUtc;
                    try
                    {
                        var tuyChon = new EnumerationOptions
                        {
                            RecurseSubdirectories = true,
                            IgnoreInaccessible = true,
                            AttributesToSkip = FileAttributes.ReparsePoint
                        };
                        foreach (FileSystemInfo item in dir.EnumerateFileSystemInfos("*", tuyChon))
                        {
                            if (item.LastWriteTimeUtc > lanHoatDongCuoi) lanHoatDongCuoi = item.LastWriteTimeUtc;
                        }
                    }
                    catch { }
                }
                if ((DateTime.UtcNow - lanHoatDongCuoi).TotalDays < soNgayToiDaKhongDung) return;
                string thuMucBackup = Path.Combine(thuMucGoc, TEN_THU_MUC_BACKUP);
                DateTime mocBackupMoi = DateTime.Now.AddDays(-SO_NGAY_GIU_BACKUP);
                bool conBackupMoi = Directory.Exists(thuMucBackup)
                    && Directory.EnumerateFiles(thuMucBackup, "*.bak").Any(f => File.GetLastWriteTime(f) > mocBackupMoi);
                if (!conBackupMoi)
                {
                    XoaThuMucAnToan(thuMucGoc);
                    Debug.WriteLine($"[SYSTEM CLEANUP] Đã xóa: {thuMucGoc}");
                    return;
                }
                // Còn bản sao lưu trong hạn: xóa mọi thứ khác, giữ lại thư mục sao lưu
                foreach (string muc in Directory.EnumerateFileSystemEntries(thuMucGoc).ToArray())
                {
                    if (string.Equals(Path.GetFileName(muc), TEN_THU_MUC_BACKUP, StringComparison.OrdinalIgnoreCase)) continue;
                    try
                    {
                        if (Directory.Exists(muc)) { if (!LaLienKet(muc)) XoaThuMucAnToan(muc); }
                        else { File.SetAttributes(muc, FileAttributes.Normal); File.Delete(muc); }
                    }
                    catch (Exception ex) { Debug.WriteLine("[SYSTEM CLEANUP] " + ex.Message); }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[KiemTraVaHuyRacHeThong] " + ex.Message);
            }
        }
        // ==================== LỊCH SỬ NẠP TRONG CSDL3 (ID = 1) ====================
        private static void KhoiTaoBangLichSuNapNeuChuaCo(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS LichSuNapDuLieu (
                ID INTEGER NOT NULL PRIMARY KEY,
                PhienBanSaoLuu TEXT,
                ThoiGianNap TEXT
            );";
            cmd.ExecuteNonQuery();
        }
        /// <summary>Đọc phiên bản đã nạp tại ID = 1. Hàm ĐỌC không còn tạo bảng (trước đây mỗi lần đọc đều chạy CREATE TABLE).</summary>
        public static string LayPhienBanDaNapTuCSDL3()
        {
            string dbPath = Module_DanduongGPS.DuongDanCSDL3;
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return string.Empty;
            try
            {
                using var conn = new SqliteConnection(TaoConnectionString(Path.GetFullPath(dbPath), false, 10));
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT PhienBanSaoLuu FROM LichSuNapDuLieu WHERE ID = 1 LIMIT 1;";
                object? result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value ? result.ToString() ?? string.Empty : string.Empty;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 1) // chưa có bảng lịch sử = chưa từng nạp
            {
                return string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi đọc lịch sử nạp từ csdl3]: {ex.Message}");
                return string.Empty;
            }
        }
        /// <summary>
        /// Ghi nhận phiên bản đã nạp thành công. Lỗi ghi được NÉM RA (trước đây nuốt lỗi) để nơi gọi biết và không dọn gói nguồn,
        /// người dùng còn có thể nạp lại. Chưa cấu hình CSDL3 thì bỏ qua như cũ.
        /// </summary>
        public static void GhiNhanDaNapThanhCongVaoCSDL3(string phienBanSaoLuu)
        {
            string dbPath = Module_DanduongGPS.DuongDanCSDL3;
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return;
            try
            {
                using var conn = new SqliteConnection(TaoConnectionString(Path.GetFullPath(dbPath), false, 30));
                conn.Open();
                using var giaoDich = conn.BeginTransaction();
                KhoiTaoBangLichSuNapNeuChuaCo(conn);
                using var cmd = conn.CreateCommand();
                cmd.Transaction = giaoDich;
                cmd.CommandText = @"
                INSERT OR REPLACE INTO LichSuNapDuLieu (ID, PhienBanSaoLuu, ThoiGianNap)
                VALUES (1, @phienBan, @thoiGian);";
                cmd.Parameters.AddWithValue("@phienBan", phienBanSaoLuu);
                cmd.Parameters.AddWithValue("@thoiGian", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture));
                cmd.ExecuteNonQuery();
                giaoDich.Commit();
            }
            catch (Exception ex)
            {
                throw new IOException("Không ghi nhận được lịch sử nạp vào CSDL3: " + ex.Message, ex);
            }
        }
        /// <summary>Có gói dữ liệu nào trên Desktop mới hơn phiên bản đã nạp hay không.</summary>
        public static bool CanHienThiThongBaoChuyenGiao()
        {
            string? thuMucGoi = TimThuMucGoiDuLieu();
            if (string.IsNullOrEmpty(thuMucGoi)) return false;
            long maDaNap = ChuyenPhienBanThanhSo(LayPhienBanDaNapTuCSDL3());
            // Quét độc lập cả 4 tệp metadata, không dừng sớm ở tệp cũ che khuất tệp mới
            foreach (string dbName in TenCSDLMucTieu)
            {
                string metaFile = Path.Combine(thuMucGoi, dbName, $"{TIEN_TO_METADATA}{dbName}.json");
                string phienBan = DocPhienBanTuTep(metaFile);
                if (phienBan.Length > 0 && ChuyenPhienBanThanhSo(phienBan) > maDaNap)
                    return true; // chỉ cần một CSDL mới hơn là đủ để báo
            }
            return false;
        }
        /// <summary>"20260606.143000" → 20260606143000 để so sánh lớn/nhỏ chính xác (chỉ lấy chữ số ASCII, tối đa 18 chữ số).</summary>
        private static long ChuyenPhienBanThanhSo(string? chuoi)
        {
            if (string.IsNullOrWhiteSpace(chuoi)) return 0;
            long ketQua = 0;
            int soChuSo = 0;
            foreach (char c in chuoi)
            {
                if (c < '0' || c > '9') continue;
                ketQua = ketQua * 10 + (c - '0');
                if (++soChuSo >= 18) break;
            }
            return ketQua;
        }
        /// <summary>Đọc "PhienBanSaoLuu" trong tệp metadata theo luồng; lỗi/không có → chuỗi rỗng.</summary>
        public static string DocPhienBanTuTep(string tep)
        {
            try
            {
                if (!File.Exists(tep)) return string.Empty;
                using var fs = new FileStream(tep, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var doc = JsonDocument.Parse(fs);
                return doc.RootElement.TryGetProperty("PhienBanSaoLuu", out JsonElement prop) && prop.ValueKind == JsonValueKind.String
                    ? prop.GetString() ?? string.Empty
                    : string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi đọc metadata]: {ex.Message}");
                return string.Empty;
            }
        }
        /// <summary>
        /// Bản cũ truy vấn cột "PhienBanSo" KHÔNG hề tồn tại trong bảng LichSuNapDuLieu nên luôn lỗi rồi trả về 0.
        /// Giữ nguyên kết quả 0 nhưng bỏ truy vấn hỏng. Nếu nơi nào còn gọi hàm này, hãy chuyển sang LayPhienBanDaNapTuCSDL3().
        /// </summary>
        [Obsolete("Cột PhienBanSo không tồn tại. Dùng LayPhienBanDaNapTuCSDL3().")]
        public static int LayPhienBanSoDaNapTuCSDL3() => 0;
        // ==================== MÃ PHIÊN BẢN XUẤT CHUNG & METADATA ====================
        private static readonly object _khoaPhienBan = new();
        private static string _phienBanXuatChungHienTai = string.Empty;
        public static string LayHoacTaoPhienBanXuatChung()
        {
            lock (_khoaPhienBan)
            {
                if (string.IsNullOrEmpty(_phienBanXuatChungHienTai))
                    _phienBanXuatChungHienTai = DateTime.Now.ToString("yyyyMMdd.HHmmss", CultureInfo.InvariantCulture);
                return _phienBanXuatChungHienTai;
            }
        }
        public static void ResetPhienBanXuatChung()
        {
            lock (_khoaPhienBan)
            {
                _phienBanXuatChungHienTai = DateTime.Now.ToString("yyyyMMdd.HHmmss", CultureInfo.InvariantCulture);
            }
        }
        private static string DinhDangDungLuong(long bytes) =>
            bytes >= 1024 * 1024 ? $"{bytes / (1024.0 * 1024.0):F2} MB" : $"{bytes / 1024.0:F2} KB";
        /// <summary>
        /// Tạo tệp thông tin phiên bản cho gói. [AN TOÀN] Ghi tệp tạm rồi đổi tên; lỗi được NÉM RA (trước đây nuốt lỗi nên gói
        /// không có metadata mà giao diện vẫn báo thành công). Nơi gọi chỉ nên gọi hàm này khi mọi bảng đã xuất xong.
        /// </summary>
        public static void TaoFileMetadataChuan(string thuMucCon, string tenCSDL, string phienBanChung = "")
        {
            if (!Directory.Exists(thuMucCon))
                throw new DirectoryNotFoundException($"Không có thư mục gói để tạo tệp phiên bản: {thuMucCon}");
            string phienBanDongGoi = string.IsNullOrWhiteSpace(phienBanChung) ? LayHoacTaoPhienBanXuatChung() : phienBanChung;
            var danhSachThongTinFile = new List<object>();
            long tongDungLuongBytes = 0;
            foreach (string filePath in Directory.EnumerateFiles(thuMucCon, "*.*", SearchOption.TopDirectoryOnly))
            {
                string ten = Path.GetFileName(filePath);
                if (ten.StartsWith(TIEN_TO_METADATA, StringComparison.OrdinalIgnoreCase)
                    || ten.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                var fi = new FileInfo(filePath);
                tongDungLuongBytes += fi.Length;
                danhSachThongTinFile.Add(new { TenFile = fi.Name, KichCoBytes = fi.Length, DungLuong = DinhDangDungLuong(fi.Length) });
            }
            var metadataObj = new
            {
                TenCSDL = tenCSDL,
                PhienBanSaoLuu = phienBanDongGoi,
                PhienBanDongGoi = phienBanDongGoi,
                PhienBanPhanMem = Module_PhienBan.SoftwareVersion,
                BuildNumber = Module_PhienBan.GetBuildNumber(),
                NgayHeThong = Module_PhienBan.NgayThangNamHeThong,
                NguoiPhatTrien = Module_PhienBan.NguoiPhatTrienPhanMem,
                MayTinhTao = Environment.MachineName,
                ThoiGianXuat = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                TongDungLuong = DinhDangDungLuong(tongDungLuongBytes),
                DanhSachTepThanhVien = danhSachThongTinFile
            };
            string metadataFilePath = Path.Combine(thuMucCon, $"{TIEN_TO_METADATA}{tenCSDL}.json");
            string fileTam = $"{metadataFilePath}.{Guid.NewGuid():N}.tmp";
            bool hoanTat = false;
            try
            {
                using (var fs = new FileStream(fileTam, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    JsonSerializer.Serialize(fs, metadataObj, new JsonSerializerOptions { WriteIndented = true, Encoder = _boMaHoa });
                    fs.Flush(true);
                }
                if (File.Exists(metadataFilePath)) File.SetAttributes(metadataFilePath, FileAttributes.Normal);
                File.Move(fileTam, metadataFilePath, overwrite: true);
                File.SetAttributes(metadataFilePath, FileAttributes.Normal);
                hoanTat = true;
            }
            catch (Exception ex)
            {
                throw new IOException($"Lỗi tạo tệp phiên bản của {tenCSDL}: {ex.Message}", ex);
            }
            finally
            {
                if (!hoanTat)
                {
                    try { if (File.Exists(fileTam)) File.Delete(fileTam); } catch { }
                }
            }
        }
    }
}