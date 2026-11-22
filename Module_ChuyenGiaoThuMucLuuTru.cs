///Phiên bản 1 - đóng gói / nhập 2 thư mục lưu trữ (Database\Bansaoluu và Database\LuuTruThiDua_LichSu) cùng gói chuyển giao dữ liệu
//TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
using Microsoft.Data.Sqlite;
using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  CÁC KIỂU DỮ LIỆU DÙNG CHUNG VỚI Form31_ChuyenGiaoDuLieu
    // =====================================================================================
    /// <summary>Một thư mục lưu trữ được đóng gói kèm gói chuyển giao (tên thư mục giống nhau ở cả gói lẫn hệ thống).</summary>
    internal sealed class NhomThuMucLuuTru
    {
        private readonly Func<string> _thuMucHeThong;
        private readonly Func<string, bool> _hopLe;
        public NhomThuMucLuuTru(string ten, string moTa, Func<string> thuMucHeThong, Func<string, bool> hopLe, bool laSqlite)
        {
            Ten = ten;
            MoTa = moTa;
            LaSqlite = laSqlite;
            _thuMucHeThong = thuMucHeThong;
            _hopLe = hopLe;
        }
        /// <summary>Tên thư mục: "Bansaoluu" / "LuuTruThiDua_LichSu".</summary>
        public string Ten { get; }
        public string MoTa { get; }
        /// <summary>true = tệp CSDL SQLite (chụp bằng Backup API, không chép thô vì có thể đang ở chế độ WAL).</summary>
        public bool LaSqlite { get; }
        /// <summary>Thư mục thật trong hệ thống (nằm trong Database của phần mềm).</summary>
        public string ThuMucHeThong => _thuMucHeThong();
        /// <summary>Tên tệp có được phép đóng gói / nhập hay không (danh sách trắng theo từng thư mục + tên an toàn).</summary>
        public bool HopLe(string tenTep) => Module_ChuyenGiaoThuMucLuuTru.LaTenAnToan(tenTep) && _hopLe(tenTep);
    }
    internal sealed record TepLuuTru(string Ten, long KichCo);
    /// <summary>Danh sách tệp CÓ THẬT trong một thư mục lưu trữ của hệ thống, sẵn sàng để xuất.</summary>
    internal sealed record GoiXuatLuuTru(NhomThuMucLuuTru Nhom, IReadOnlyList<TepLuuTru> Tep)
    {
        public long TongByte
        {
            get
            {
                long tong = 0;
                foreach (var t in Tep) tong += t.KichCo;
                return tong;
            }
        }
    }
    /// <summary>Một dòng trong tệp xác nhận của gói: tên, dung lượng, mã SHA-256, ngày sửa đổi gốc.</summary>
    internal sealed record MucXacNhanLuuTru(string TenFile, long KichCoBytes, string Sha256, DateTime NgaySuaDoiUtc);
    /// <summary>Một thư mục lưu trữ nằm trong gói trên Desktop, đã đọc và kiểm tra tệp xác nhận.</summary>
    internal sealed class GoiNhapLuuTru
    {
        public GoiNhapLuuTru(NhomThuMucLuuTru nhom, string thuMucGoi, string thuMucDich)
        {
            Nhom = nhom;
            ThuMucGoi = thuMucGoi;
            ThuMucDich = thuMucDich;
        }
        public NhomThuMucLuuTru Nhom { get; }
        public string ThuMucGoi { get; }
        public string ThuMucDich { get; }
        /// <summary>Mã đợt xuất ghi trong tệp xác nhận (dùng khi ghi lại tệp xác nhận sau khi nhập một phần).</summary>
        public string PhienBan { get; set; } = string.Empty;
        /// <summary>Chỉ gồm tệp hợp lệ, có trong tệp xác nhận VÀ có thật trong gói.</summary>
        public List<MucXacNhanLuuTru> Tep { get; } = new();
        /// <summary>Cảnh báo khi quét gói (thiếu tệp xác nhận, tên tệp lạ...). Không làm dừng các tệp còn lại.</summary>
        public List<string> CanhBao { get; } = new();
        public long TongByte
        {
            get
            {
                long tong = 0;
                foreach (var t in Tep) tong += t.KichCoBytes;
                return tong;
            }
        }
    }
    internal enum LoaiKetQuaNhapTep
    {
        /// <summary>Đã chép vào hệ thống.</summary>
        DaNhap,
        /// <summary>Hệ thống đã có đúng tệp này (cùng mã SHA-256): không cần ghi lại.</summary>
        DaCoGiongHet
    }
    internal sealed record KetQuaNhapTepLuuTru(LoaiKetQuaNhapTep Loai, string? BanSaoLuu);
    // =====================================================================================
    //  MODULE
    //
    //  Quy ước gói trên Desktop (cùng thư mục "Database-PhanMemThiDua2026" với các gói csdl1..csdl4):
    //      Bansaoluu\Backup_*.pmtd                       + sync_info_Bansaoluu.json
    //      LuuTruThiDua_LichSu\*.db                      + sync_info_LuuTruThiDua_LichSu.json
    //  - CHỈ đóng gói khi thư mục trong hệ thống CÓ dữ liệu hợp lệ; không có thì bỏ qua cả khi xuất lẫn khi nhập.
    //  - Tệp xác nhận (sync_info_*.json) ghi tên + dung lượng + SHA-256 của từng tệp, được tạo SAU CÙNG khi mọi tệp đã chép xong:
    //    gói xuất dở dang (không có tệp xác nhận) và gói bị hỏng/bị sửa khi chuyển đi đều bị chặn khi nhập.
    //  - Bansaoluu chỉ lấy Backup_*.pmtd. Giấy phép cấp quyền dịch vụ (GiayPhepCapQuyen_*.dat) gắn với từng máy và nhật ký
    //    làm sạch (NhatKy_LamSach.txt) KHÔNG đi theo gói.
    //  - Tên tệp lấy từ gói là dữ liệu KHÔNG đáng tin: chỉ nhận tên nằm trong danh sách trắng, không có đường dẫn con, không đi theo liên kết.
    // =====================================================================================
    internal static class Module_ChuyenGiaoThuMucLuuTru
    {
        public const string TEN_THU_MUC_BAN_SAO_LUU = "Bansaoluu";
        public const string TEN_THU_MUC_LICH_SU = "LuuTruThiDua_LichSu";
        private const string TIEN_TO_XAC_NHAN = "sync_info_";   // cùng quy ước tệp phiên bản của các gói csdl
        private const string LOAI_GOI = "ThuMucLuuTru";
        private const int BO_DEM_TEP = 256 * 1024;
        private const long GIOI_HAN_TEP_XAC_NHAN = 4L * 1024 * 1024;
        private const int SO_TEP_TOI_DA = 5000;
        private const int DO_DAI_TEN_TOI_DA = 200;
        private static readonly string[] HauToPhuSqlite = { "-wal", "-shm", "-journal" };
        private static readonly JavaScriptEncoder _boMaHoa = JavaScriptEncoder.Create(UnicodeRanges.All);
        // Hai thư mục lưu trữ. Đường dẫn lấy từ Module_DanduongGPS (cùng nguồn với Module_KhoiTaoCSDL / Module_HoTroLuuDataTheoNamCu).
        private static readonly NhomThuMucLuuTru[] _nhom =
        {
            new(TEN_THU_MUC_BAN_SAO_LUU,
                "Bản sao lưu cơ sở dữ liệu",
                () => Path.Combine(Module_DanduongGPS.ThuMucCoSoDuLieu, TEN_THU_MUC_BAN_SAO_LUU),
                ten => ten.StartsWith("Backup_", StringComparison.OrdinalIgnoreCase)
                       && ten.EndsWith(".pmtd", StringComparison.OrdinalIgnoreCase),
                laSqlite: false),
            new(TEN_THU_MUC_LICH_SU,
                "Kết quả thi đua, khen thưởng các năm trước",
                () => Module_DanduongGPS.ThuMucLichSuThiDua,
                ten => ten.EndsWith(".db", StringComparison.OrdinalIgnoreCase),
                laSqlite: true)
        };
        // =====================================================================================
        //  TIỆN ÍCH
        // =====================================================================================
        /// <summary>
        /// Tên tệp an toàn: không ký tự đường dẫn / ổ đĩa / luồng phụ NTFS (':'), không kết thúc bằng dấu chấm hay khoảng trắng,
        /// chỉ gồm chữ, số, '_' '-' '.' ' ' '(' ')'.
        /// </summary>
        internal static bool LaTenAnToan(string? ten)
        {
            if (string.IsNullOrEmpty(ten) || ten.Length > DO_DAI_TEN_TOI_DA) return false;
            if (ten[0] == ' ' || ten[^1] == ' ' || ten[^1] == '.') return false;
            foreach (char c in ten)
            {
                bool ok = char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.' || c == ' ' || c == '(' || c == ')';
                if (!ok) return false;
            }
            if (ten.Contains("..", StringComparison.Ordinal)) return false;
            return string.Equals(Path.GetFileName(ten), ten, StringComparison.Ordinal);
        }
        public static string TenTepXacNhan(NhomThuMucLuuTru nhom) => $"{TIEN_TO_XAC_NHAN}{nhom.Ten}.json";
        public static string DinhDangDungLuong(long bytes) =>
            bytes >= 1024L * 1024 * 1024 ? $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB"
            : bytes >= 1024L * 1024 ? $"{bytes / (1024.0 * 1024.0):F2} MB"
            : $"{bytes / 1024.0:F2} KB";
        private static bool LaLienKet(string duongDan)
        {
            try { return (File.GetAttributes(duongDan) & FileAttributes.ReparsePoint) != 0; }
            catch { return true; } // không đọc được thuộc tính: coi như không an toàn
        }
        private static string RutGon(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "(trống)";
            return s.Length <= 60 ? s : s[..60] + "...";
        }
        private static void XoaTep(string tep)
        {
            if (!File.Exists(tep)) return;
            File.SetAttributes(tep, FileAttributes.Normal);
            File.Delete(tep);
        }
        private static void XoaTepBestEffort(string tep)
        {
            try { XoaTep(tep); }
            catch (Exception ex) { Debug.WriteLine($"[LuuTru] Không xóa được '{tep}': {ex.Message}"); }
        }
        /// <summary>"X.db-wal" / "X.db-shm" / "X.db-journal" (hoặc của tệp tạm "X.db.guid.tmp") do chính việc đóng gói/đọc CSDL để lại.</summary>
        private static bool LaTepPhuSqliteCuaGoi(NhomThuMucLuuTru nhom, string ten)
        {
            foreach (string hau in HauToPhuSqlite)
            {
                if (!ten.EndsWith(hau, StringComparison.OrdinalIgnoreCase)) continue;
                string goc = ten[..^hau.Length];
                if (goc.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) || nhom.HopLe(goc)) return true;
            }
            return false;
        }
        /// <summary>Tệp tạm của SQLite (-wal/-shm/-journal) đi kèm một tệp tạm đã/đang bị bỏ.</summary>
        private static void XoaTepPhuSqliteBestEffort(string tepChinh)
        {
            foreach (string hau in HauToPhuSqlite) XoaTepBestEffort(tepChinh + hau);
        }
        private static bool LaMaBamHopLe(string? s)
        {
            if (s is null || s.Length != 64) return false;
            foreach (char c in s)
            {
                bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!hex) return false;
            }
            return true;
        }
        private static string LayChuoi(JsonElement phanTu, string ten) =>
            phanTu.ValueKind == JsonValueKind.Object
            && phanTu.TryGetProperty(ten, out JsonElement g)
            && g.ValueKind == JsonValueKind.String
                ? g.GetString() ?? string.Empty
                : string.Empty;
        /// <summary>
        /// Chép (hoặc chỉ đọc nếu <paramref name="ra"/> null) qua MỘT bộ đệm dùng chung, băm SHA-256 ngay trong lúc chép
        /// (không đọc lại tệp lần hai). Không bao giờ nạp cả tệp vào RAM.
        /// </summary>
        private static (long KichCo, string Bam) ChepVaBam(Stream vao, FileStream? ra, CancellationToken huy)
        {
            byte[] dem = ArrayPool<byte>.Shared.Rent(BO_DEM_TEP);
            try
            {
                using var bam = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                long tong = 0;
                int n;
                while ((n = vao.Read(dem, 0, BO_DEM_TEP)) > 0)
                {
                    huy.ThrowIfCancellationRequested();
                    ra?.Write(dem, 0, n);
                    bam.AppendData(dem, 0, n);
                    tong += n;
                }
                ra?.Flush(true);
                return (tong, Convert.ToHexString(bam.GetHashAndReset()).ToLowerInvariant());
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(dem);
            }
        }
        /// <summary>Mở đọc kiểu "không cản ai": tệp đang được mở/ghi bởi tiến trình khác vẫn đọc được.</summary>
        private static FileStream MoDoc(string duongDan) =>
            new(duongDan, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1, FileOptions.SequentialScan);
        private static FileStream TaoMoi(string duongDan) =>
            new(duongDan, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.SequentialScan);
        private static (long KichCo, string Bam) TinhBam(string duongDan, CancellationToken huy)
        {
            using var vao = MoDoc(duongDan);
            return ChepVaBam(vao, null, huy);
        }
        private static (long KichCo, string Bam) SaoChepVaBam(string nguon, string dich, CancellationToken huy)
        {
            using var vao = MoDoc(nguon);
            using var ra = TaoMoi(dich);
            return ChepVaBam(vao, ra, huy);
        }
        /// <summary>
        /// Kiểm tra dung lượng trống của ổ chứa <paramref name="thuMuc"/>. Không xác định được (ổ mạng UNC...) thì bỏ qua kiểm tra.
        /// Thiếu chỗ thì báo NGAY, không để chép dở rồi mới lỗi giữa chừng.
        /// </summary>
        public static void KiemTraDungLuongTrong(string thuMuc, long canByte)
        {
            long? conTrong = null;
            try
            {
                string? goc = Path.GetPathRoot(Path.GetFullPath(thuMuc));
                if (!string.IsNullOrEmpty(goc)) conTrong = new DriveInfo(goc).AvailableFreeSpace;
            }
            catch { /* không đo được: bỏ qua */ }
            if (conTrong is null) return;
            long can = canByte + canByte / 10 + 64L * 1024 * 1024; // chừa 10% + 64 MB
            if (conTrong.Value < can)
                throw new IOException($"Ổ đĩa không đủ dung lượng trống (cần khoảng {DinhDangDungLuong(can)}, còn {DinhDangDungLuong(conTrong.Value)}).");
        }
        // =====================================================================================
        //  SQLITE: chụp nhất quán + kiểm tra toàn vẹn
        //  Tệp trong LuuTruThiDua_LichSu được đặt journal_mode = WAL (Module_HoTroLuuDataTheoNamCu) nên dữ liệu mới nhất có thể còn nằm
        //  trong tệp -wal: chép thô tệp .db sẽ ra bản thiếu dữ liệu. Dùng SQLite Backup API như Module_ChuyenGiaoDuLieu.SaoLuuCSDLTruocKhiNhap.
        // =====================================================================================
        private static string ChuoiKetNoi(string duongDan, SqliteOpenMode che) =>
            new SqliteConnectionStringBuilder { DataSource = duongDan, Mode = che, Pooling = false }.ToString();
        /// <summary>PRAGMA quick_check = "ok". Mở ở chế độ ghi để SQLite tự dọn tệp -wal/-shm khi đóng (mở chỉ-đọc sẽ để lại chúng).</summary>
        private static bool KiemTraNhanhSqlite(SqliteConnection ketNoi)
        {
            using var lenh = ketNoi.CreateCommand();
            lenh.CommandText = "PRAGMA quick_check;";
            string? kq = Convert.ToString(lenh.ExecuteScalar(), CultureInfo.InvariantCulture);
            return string.Equals(kq, "ok", StringComparison.OrdinalIgnoreCase);
        }
        private static void KiemTraTepSqlite(string duongDan)
        {
            try
            {
                using var kn = new SqliteConnection(ChuoiKetNoi(duongDan, SqliteOpenMode.ReadWrite));
                kn.Open();
                if (!KiemTraNhanhSqlite(kn))
                    throw new IOException("Tệp CSDL không vượt qua kiểm tra toàn vẹn (quick_check).");
            }
            catch (SqliteException ex)
            {
                throw new IOException("Tệp không phải CSDL SQLite hợp lệ: " + ex.Message, ex);
            }
            finally
            {
                XoaTepPhuSqliteBestEffort(duongDan);
            }
        }
        private static void ChupSqlite(string nguon, string dich)
        {
            Exception? loi = null;
            for (int lanThu = 1; lanThu <= 3; lanThu++)
            {
                try
                {
                    using (var kn = new SqliteConnection(ChuoiKetNoi(nguon, SqliteOpenMode.ReadOnly)))
                    using (var kd = new SqliteConnection(ChuoiKetNoi(dich, SqliteOpenMode.ReadWriteCreate)))
                    {
                        kn.Open();
                        kd.Open();
                        kn.BackupDatabase(kd);
                        // Không có bằng chứng bản chụp toàn vẹn thì không được coi là đã đóng gói / sao lưu
                        if (!KiemTraNhanhSqlite(kd))
                            throw new IOException("Bản chụp không vượt qua kiểm tra toàn vẹn.");
                    }
                    loi = null;
                    break;
                }
                catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
                {
                    loi = ex;
                    XoaTepBestEffort(dich);
                    XoaTepPhuSqliteBestEffort(dich);
                    Thread.Sleep(300 * lanThu);
                }
            }
            XoaTepPhuSqliteBestEffort(dich); // đóng kết nối xong SQLite đã tự dọn; đây chỉ là chốt chặn
            if (loi != null)
                throw new IOException("Không tạo được bản chụp nhất quán của tệp CSDL: " + loi.Message, loi);
            var fi = new FileInfo(dich);
            if (!fi.Exists || fi.Length == 0)
                throw new IOException("Bản chụp CSDL rỗng.");
        }
        // =====================================================================================
        //  XUẤT
        // =====================================================================================
        private static List<TepLuuTru> LietKeTepHeThong(NhomThuMucLuuTru nhom)
        {
            var kq = new List<TepLuuTru>();
            string thuMuc = nhom.ThuMucHeThong;
            if (string.IsNullOrWhiteSpace(thuMuc) || !Directory.Exists(thuMuc)) return kq;
            foreach (FileInfo fi in new DirectoryInfo(thuMuc).EnumerateFiles("*", SearchOption.TopDirectoryOnly))
            {
                if ((fi.Attributes & FileAttributes.ReparsePoint) != 0) continue; // không đi theo liên kết tệp
                if (!nhom.HopLe(fi.Name)) continue;                                 // giấy phép .dat, nhật ký .txt, tệp -wal... bị loại
                if (fi.Length <= 0) continue;                                       // tệp rỗng
                kq.Add(new TepLuuTru(fi.Name, fi.Length));
                if (kq.Count >= SO_TEP_TOI_DA) break;
            }
            kq.Sort((a, b) => string.Compare(a.Ten, b.Ten, StringComparison.OrdinalIgnoreCase));
            return kq;
        }
        /// <summary>Các thư mục lưu trữ CÓ dữ liệu hợp lệ (thư mục không có dữ liệu thì không xuất).</summary>
        public static List<GoiXuatLuuTru> LietKeDeXuat()
        {
            var ds = new List<GoiXuatLuuTru>(2);
            foreach (var nhom in _nhom)
            {
                try
                {
                    var tep = LietKeTepHeThong(nhom);
                    if (tep.Count > 0) ds.Add(new GoiXuatLuuTru(nhom, tep));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[LietKeDeXuat:{nhom.Ten}] {ex.Message}");
                }
            }
            return ds;
        }
        /// <summary>
        /// Dọn các tệp CŨ do chính lần xuất trước tạo ra trong thư mục gói (tệp hợp lệ + tệp xác nhận + tệp tạm), để gói phản ánh đúng hiện trạng.
        /// Tệp lạ do người dùng tự bỏ vào được giữ nguyên. Lỗi (tệp đang bị khóa) được ném ra để nơi gọi dừng nhóm này.
        /// </summary>
        public static void LamMoiThuMucGoi(string thuMucGoiNhom, NhomThuMucLuuTru nhom, bool xoaThuMucNeuRong = false)
        {
            if (!Directory.Exists(thuMucGoiNhom)) return;
            if (LaLienKet(thuMucGoiNhom))
                throw new IOException($"Thư mục '{nhom.Ten}' trong gói là liên kết nên không ghi vào được vì lý do an toàn.");
            string tepXacNhan = TenTepXacNhan(nhom);
            foreach (string tep in Directory.EnumerateFiles(thuMucGoiNhom, "*", SearchOption.TopDirectoryOnly).ToArray())
            {
                string ten = Path.GetFileName(tep);
                bool laCuaTa = nhom.HopLe(ten)
                               || ten.Equals(tepXacNhan, StringComparison.OrdinalIgnoreCase)
                               || ten.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
                               || (nhom.LaSqlite && LaTepPhuSqliteCuaGoi(nhom, ten));
                if (laCuaTa) XoaTep(tep);
            }
            if (xoaThuMucNeuRong && !Directory.EnumerateFileSystemEntries(thuMucGoiNhom).Any())
                Directory.Delete(thuMucGoiNhom, false);
        }
        /// <summary>
        /// Đóng gói MỘT tệp: ghi vào tệp tạm → xác minh → đổi tên (nguyên tử). Trả về dòng xác nhận để ghi vào tệp xác nhận.
        /// Tệp SQLite được chụp nhất quán bằng Backup API; tệp khác chép theo luồng và kiểm tra nguồn không đổi trong lúc chép.
        /// </summary>
        public static MucXacNhanLuuTru XuatMotTep(NhomThuMucLuuTru nhom, string tenTep, string thuMucGoiNhom, CancellationToken huy)
        {
            if (!nhom.HopLe(tenTep))
                throw new InvalidDataException($"Tên tệp không hợp lệ nên không đóng gói: {RutGon(tenTep)}");
            string nguon = Path.Combine(nhom.ThuMucHeThong, tenTep);
            var fiNguon = new FileInfo(nguon);
            if (!fiNguon.Exists) throw new FileNotFoundException("Không tìm thấy tệp nguồn.", nguon);
            if ((fiNguon.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException($"Tệp '{tenTep}' là liên kết nên không đóng gói vì lý do an toàn.");
            if (Directory.Exists(thuMucGoiNhom) && LaLienKet(thuMucGoiNhom))
                throw new IOException($"Thư mục '{nhom.Ten}' trong gói là liên kết nên không ghi vào được vì lý do an toàn.");
            Directory.CreateDirectory(thuMucGoiNhom);
            string dich = Path.Combine(thuMucGoiNhom, tenTep);
            string tam = $"{dich}.{Guid.NewGuid():N}.tmp";
            bool xong = false;
            try
            {
                DateTime ngaySua = fiNguon.LastWriteTimeUtc;
                long kichCo;
                string bam;
                if (nhom.LaSqlite)
                {
                    ChupSqlite(nguon, tam);
                    (kichCo, bam) = TinhBam(tam, huy);
                }
                else
                {
                    (kichCo, bam) = SaoChepVaBam(nguon, tam, huy);
                    fiNguon.Refresh();
                    if (fiNguon.Length != kichCo || fiNguon.LastWriteTimeUtc != ngaySua)
                        throw new IOException($"Tệp '{tenTep}' bị thay đổi trong lúc đóng gói (có thể đang được ghi). Hãy thử lại.");
                }
                if (kichCo <= 0) throw new IOException($"Tệp đóng gói '{tenTep}' rỗng.");
                if (File.Exists(dich)) File.SetAttributes(dich, FileAttributes.Normal);
                File.Move(tam, dich, overwrite: true);
                try { File.SetAttributes(dich, FileAttributes.Normal); } catch { }
                xong = true;
                return new MucXacNhanLuuTru(tenTep, kichCo, bam, ngaySua);
            }
            finally
            {
                if (!xong)
                {
                    XoaTepBestEffort(tam);
                    XoaTepPhuSqliteBestEffort(tam);
                }
            }
        }
        /// <summary>
        /// Làm mới "nhịp tim" của thư mục gói (tệp system.touch) giống Module_ChuyenGiaoDuLieu: cơ chế tự dọn gói bỏ hoang
        /// (2 ngày) dựa vào mốc này, nên lần xuất chỉ có thư mục lưu trữ cũng phải đánh dấu "vừa hoạt động"
        /// để gói vừa xuất không bị dọn nhầm theo mốc của lần xuất trước. Không bao giờ ném lỗi.
        /// </summary>
        public static void CapNhatNhipTim(string thuMucGoc)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(thuMucGoc)) return;
                Directory.CreateDirectory(thuMucGoc);
                string tep = Path.Combine(thuMucGoc, "system.touch");
                if (!File.Exists(tep))
                {
                    using (File.Create(tep)) { }
                }
                DateTime now = DateTime.UtcNow;
                File.SetLastWriteTimeUtc(tep, now);
                Directory.SetLastWriteTimeUtc(thuMucGoc, now);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[LuuTru:NhipTim] " + ex.Message);
            }
        }
        /// <summary>Ghi tệp xác nhận của gói (tệp tạm → đổi tên). Chỉ gọi khi MỌI tệp của thư mục này đã đóng gói xong.</summary>
        public static void TaoXacNhan(NhomThuMucLuuTru nhom, string thuMucGoiNhom, IReadOnlyList<MucXacNhanLuuTru> cacMuc, string phienBan)
        {
            if (cacMuc.Count == 0)
                throw new InvalidOperationException($"Không có tệp nào của '{nhom.Ten}' để ghi tệp xác nhận.");
            if (!Directory.Exists(thuMucGoiNhom))
                throw new DirectoryNotFoundException($"Không có thư mục gói để tạo tệp xác nhận: {thuMucGoiNhom}");
            var sapXep = cacMuc.OrderBy(m => m.TenFile, StringComparer.OrdinalIgnoreCase).ToList();
            long tong = 0;
            foreach (var m in sapXep) tong += m.KichCoBytes;
            string tep = Path.Combine(thuMucGoiNhom, TenTepXacNhan(nhom));
            string tam = $"{tep}.{Guid.NewGuid():N}.tmp";
            bool xong = false;
            try
            {
                using (var fs = new FileStream(tam, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var w = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true, Encoder = _boMaHoa }))
                {
                    w.WriteStartObject();
                    w.WriteString("LoaiGoi", LOAI_GOI);
                    w.WriteString("TenThuMuc", nhom.Ten);
                    w.WriteString("MoTa", nhom.MoTa);
                    w.WriteString("PhienBanDongGoi", phienBan);
                    w.WriteString("MayTinhTao", Environment.MachineName);
                    w.WriteString("ThoiGianXuat", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture));
                    w.WriteNumber("TongSoTep", sapXep.Count);
                    w.WriteString("TongDungLuong", DinhDangDungLuong(tong));
                    w.WriteStartArray("DanhSachTep");
                    foreach (var m in sapXep)
                    {
                        w.WriteStartObject();
                        w.WriteString("TenFile", m.TenFile);
                        w.WriteNumber("KichCoBytes", m.KichCoBytes);
                        w.WriteString("SHA256", m.Sha256);
                        w.WriteString("NgaySuaDoiUtc", m.NgaySuaDoiUtc.ToString("o", CultureInfo.InvariantCulture));
                        w.WriteEndObject();
                    }
                    w.WriteEndArray();
                    w.WriteEndObject();
                    w.Flush();
                    fs.Flush(true);
                }
                if (File.Exists(tep)) File.SetAttributes(tep, FileAttributes.Normal);
                File.Move(tam, tep, overwrite: true);
                File.SetAttributes(tep, FileAttributes.Normal);
                xong = true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new IOException($"Lỗi tạo tệp xác nhận của '{nhom.Ten}': {ex.Message}", ex);
            }
            finally
            {
                if (!xong) XoaTepBestEffort(tam);
            }
        }
        // =====================================================================================
        //  NHẬP
        // =====================================================================================
        private static List<string> LietKeTepHopLeTrongGoi(string thuMucGoi, NhomThuMucLuuTru nhom)
        {
            var kq = new List<string>();
            foreach (string tep in Directory.EnumerateFiles(thuMucGoi, "*", SearchOption.TopDirectoryOnly))
            {
                string ten = Path.GetFileName(tep);
                if (nhom.HopLe(ten)) kq.Add(ten);
            }
            return kq;
        }
        private static bool DocXacNhan(NhomThuMucLuuTru nhom, string tepXacNhan, List<MucXacNhanLuuTru> cacMuc, List<string> canhBao,
                                       out string phienBan)
        {
            phienBan = string.Empty;
            try
            {
                if (new FileInfo(tepXacNhan).Length > GIOI_HAN_TEP_XAC_NHAN)
                {
                    canhBao.Add("Tệp xác nhận quá lớn, nghi ngờ không hợp lệ nên không nhập thư mục này.");
                    return false;
                }
                using var fs = new FileStream(tepXacNhan, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var doc = JsonDocument.Parse(fs, new JsonDocumentOptions { MaxDepth = 8 });
                JsonElement goc = doc.RootElement;
                if (!LayChuoi(goc, "LoaiGoi").Equals(LOAI_GOI, StringComparison.Ordinal)
                    || !LayChuoi(goc, "TenThuMuc").Equals(nhom.Ten, StringComparison.OrdinalIgnoreCase))
                {
                    canhBao.Add("Tệp xác nhận không thuộc thư mục này nên không nhập.");
                    return false;
                }
                if (goc.ValueKind != JsonValueKind.Object
                    || !goc.TryGetProperty("DanhSachTep", out JsonElement ds)
                    || ds.ValueKind != JsonValueKind.Array)
                {
                    canhBao.Add("Tệp xác nhận thiếu danh sách tệp nên không nhập.");
                    return false;
                }
                phienBan = LayChuoi(goc, "PhienBanDongGoi");
                if (phienBan.Length > 64) phienBan = phienBan[..64];
                var daThay = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (JsonElement e in ds.EnumerateArray())
                {
                    if (cacMuc.Count >= SO_TEP_TOI_DA)
                    {
                        canhBao.Add($"Tệp xác nhận có quá {SO_TEP_TOI_DA} tệp: bỏ qua phần còn lại.");
                        break;
                    }
                    string ten = LayChuoi(e, "TenFile");
                    if (!nhom.HopLe(ten))
                    {
                        canhBao.Add($"Bỏ qua mục có tên tệp không hợp lệ: {RutGon(ten)}");
                        continue;
                    }
                    if (!daThay.Add(ten))
                    {
                        canhBao.Add($"Bỏ qua mục bị trùng tên trong tệp xác nhận: {ten}");
                        continue;
                    }
                    if (!e.TryGetProperty("KichCoBytes", out JsonElement kc)
                        || kc.ValueKind != JsonValueKind.Number
                        || !kc.TryGetInt64(out long kichCo)
                        || kichCo <= 0)
                    {
                        canhBao.Add($"Bỏ qua '{ten}': dung lượng ghi trong tệp xác nhận không hợp lệ.");
                        continue;
                    }
                    string bam = LayChuoi(e, "SHA256");
                    if (!LaMaBamHopLe(bam))
                    {
                        canhBao.Add($"Bỏ qua '{ten}': mã SHA-256 ghi trong tệp xác nhận không hợp lệ.");
                        continue;
                    }
                    DateTime ngaySua = default;
                    if (DateTime.TryParse(LayChuoi(e, "NgaySuaDoiUtc"), CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime d)
                        && d.Year >= 2000 && d <= DateTime.UtcNow.AddDays(1))
                    {
                        ngaySua = d;
                    }
                    cacMuc.Add(new MucXacNhanLuuTru(ten, kichCo, bam.ToLowerInvariant(), ngaySua));
                }
                return true;
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                canhBao.Add("Không đọc được tệp xác nhận: " + ex.Message);
                return false;
            }
        }
        /// <summary>
        /// Quét các thư mục lưu trữ trong gói trên Desktop. Chỉ trả về những thư mục có mặt trong gói: có tệp hợp lệ để nhập
        /// (<see cref="GoiNhapLuuTru.Tep"/>) và/hoặc có cảnh báo cần báo cho người dùng. Thư mục không có trong gói thì không có mặt.
        /// </summary>
        public static List<GoiNhapLuuTru> QuetGoiDeNhap(string? thuMucGoc)
        {
            var ds = new List<GoiNhapLuuTru>(2);
            if (string.IsNullOrWhiteSpace(thuMucGoc) || !Directory.Exists(thuMucGoc)) return ds;
            foreach (var nhom in _nhom)
            {
                try
                {
                    string thuMucGoi = Path.Combine(thuMucGoc, nhom.Ten);
                    if (!Directory.Exists(thuMucGoi)) continue;
                    var goi = new GoiNhapLuuTru(nhom, thuMucGoi, nhom.ThuMucHeThong);
                    if (LaLienKet(thuMucGoi))
                    {
                        goi.CanhBao.Add("Thư mục trong gói là liên kết (shortcut/junction) nên không nhập vì lý do an toàn.");
                        ds.Add(goi);
                        continue;
                    }
                    if (string.Equals(Path.GetFullPath(thuMucGoi).TrimEnd(Path.DirectorySeparatorChar),
                            Path.GetFullPath(goi.ThuMucDich).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                    {
                        goi.CanhBao.Add("Thư mục gói trùng với thư mục hệ thống nên không nhập.");
                        ds.Add(goi);
                        continue;
                    }
                    List<string> tepHopLe = LietKeTepHopLeTrongGoi(thuMucGoi, nhom);
                    string tepXacNhan = Path.Combine(thuMucGoi, TenTepXacNhan(nhom));
                    if (!File.Exists(tepXacNhan))
                    {
                        if (tepHopLe.Count > 0)
                        {
                            goi.CanhBao.Add($"Thiếu tệp xác nhận '{TenTepXacNhan(nhom)}' (gói xuất chưa hoàn tất hoặc chép thiếu) " +
                                            $"nên không nhập {tepHopLe.Count} tệp trong thư mục này.");
                            ds.Add(goi);
                        }
                        continue;
                    }
                    var cacMuc = new List<MucXacNhanLuuTru>();
                    bool docDuoc = DocXacNhan(nhom, tepXacNhan, cacMuc, goi.CanhBao, out string phienBan);
                    goi.PhienBan = phienBan;
                    if (!docDuoc)
                    {
                        ds.Add(goi);
                        continue;
                    }
                    foreach (var m in cacMuc)
                    {
                        if (File.Exists(Path.Combine(thuMucGoi, m.TenFile))) goi.Tep.Add(m);
                        else goi.CanhBao.Add($"Gói thiếu tệp đã ghi trong tệp xác nhận: {m.TenFile}");
                    }
                    var trongXacNhan = new HashSet<string>(cacMuc.Select(m => m.TenFile), StringComparer.OrdinalIgnoreCase);
                    foreach (string ten in tepHopLe)
                    {
                        if (!trongXacNhan.Contains(ten))
                            goi.CanhBao.Add($"Bỏ qua tệp không có trong tệp xác nhận: {ten}");
                    }
                    if (goi.Tep.Count > 0 || goi.CanhBao.Count > 0) ds.Add(goi);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[QuetGoiDeNhap:{nhom.Ten}] {ex.Message}");
                }
            }
            return ds;
        }
        /// <summary>Tạo thư mục đích và kiểm tra dung lượng trống trước khi chép.</summary>
        public static void ChuanBiNhap(GoiNhapLuuTru goi)
        {
            Directory.CreateDirectory(goi.ThuMucDich);
            KiemTraDungLuongTrong(goi.ThuMucDich, goi.TongByte);
        }
        /// <summary>
        /// Nhập MỘT tệp từ gói vào hệ thống.
        /// - Kiểm tra tên / dung lượng / SHA-256 theo tệp xác nhận (chép vào tệp tạm cạnh đích, băm trong lúc chép) rồi mới đổi tên đè lên đích.
        /// - Hệ thống đã có tệp cùng tên: giống hệt thì bỏ qua; khác nội dung thì CHỈ ghi đè khi <paramref name="ghiDe"/> = true
        ///   (lúc đó, nếu <paramref name="thuMucSaoLuu"/> khác null, bản cũ được sao lưu trước; sao lưu lỗi thì không ghi đè).
        /// - Ghi đè CSDL SQLite: giải phóng kết nối đang giữ tệp và xóa tệp -wal/-shm cũ để không áp nhầm nhật ký cũ lên CSDL mới.
        /// </summary>
        public static KetQuaNhapTepLuuTru NhapMotTep(GoiNhapLuuTru goi, MucXacNhanLuuTru muc, bool ghiDe, string? thuMucSaoLuu, CancellationToken huy)
        {
            var nhom = goi.Nhom;
            if (!nhom.HopLe(muc.TenFile))
                throw new InvalidDataException($"Tên tệp không hợp lệ nên không nhập: {RutGon(muc.TenFile)}");
            string nguon = Path.Combine(goi.ThuMucGoi, muc.TenFile);
            var fiNguon = new FileInfo(nguon);
            if (!fiNguon.Exists) throw new FileNotFoundException("Không tìm thấy tệp trong gói.", nguon);
            if ((fiNguon.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException($"Tệp '{muc.TenFile}' trong gói là liên kết nên không nhập vì lý do an toàn.");
            if (fiNguon.Length != muc.KichCoBytes)
                throw new IOException($"Dung lượng '{muc.TenFile}' không khớp tệp xác nhận (gói có thể bị hỏng hoặc chép thiếu khi chuyển).");
            string thuMucDich = Path.GetFullPath(goi.ThuMucDich);
            Directory.CreateDirectory(thuMucDich);
            string dich = Path.GetFullPath(Path.Combine(thuMucDich, muc.TenFile));
            string tienToDich = thuMucDich.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!dich.StartsWith(tienToDich, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(Path.GetDirectoryName(dich), tienToDich.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Đường dẫn đích của '{muc.TenFile}' nằm ngoài thư mục cho phép.");
            bool daCo = File.Exists(dich);
            if (daCo)
            {
                // Giống hệt (cùng dung lượng + SHA-256) thì không cần ghi lại, dù có tích ghi đè hay không
                var fiDich = new FileInfo(dich);
                if (fiDich.Length == muc.KichCoBytes)
                {
                    var (_, bamDich) = TinhBam(dich, huy);
                    if (string.Equals(bamDich, muc.Sha256, StringComparison.OrdinalIgnoreCase))
                        return new KetQuaNhapTepLuuTru(LoaiKetQuaNhapTep.DaCoGiongHet, null);
                }
                if (!ghiDe)
                    throw new InvalidOperationException(
                        $"Hệ thống đã có tệp '{muc.TenFile}' với nội dung khác nên giữ nguyên bản hiện tại. " +
                        "Tích 'Xóa dữ liệu trong csdl trước khi nạp' nếu muốn ghi đè bằng tệp trong gói.");
            }
            string tam = $"{dich}.{Guid.NewGuid():N}.tmp";
            string? banSaoLuu = null;
            bool xong = false;
            try
            {
                var (kichCo, bam) = SaoChepVaBam(nguon, tam, huy);
                if (kichCo != muc.KichCoBytes || !string.Equals(bam, muc.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new IOException($"Nội dung '{muc.TenFile}' không khớp mã SHA-256 trong tệp xác nhận (gói bị hỏng hoặc bị sửa).");
                if (nhom.LaSqlite) KiemTraTepSqlite(tam);
                if (daCo && thuMucSaoLuu != null)
                    banSaoLuu = SaoLuuTepCu(nhom, dich, thuMucSaoLuu);
                if (daCo)
                {
                    if (nhom.LaSqlite) GiaiPhongSqliteTruocKhiThay(dich);
                    File.SetAttributes(dich, FileAttributes.Normal);
                }
                File.Move(tam, dich, overwrite: true);
                xong = true;
                try { File.SetAttributes(dich, FileAttributes.Normal); } catch { }
                if (muc.NgaySuaDoiUtc != default)
                {
                    try { File.SetLastWriteTimeUtc(dich, muc.NgaySuaDoiUtc); } catch { /* không quan trọng */ }
                }
                return new KetQuaNhapTepLuuTru(LoaiKetQuaNhapTep.DaNhap, banSaoLuu);
            }
            finally
            {
                if (!xong)
                {
                    XoaTepBestEffort(tam);
                    XoaTepPhuSqliteBestEffort(tam);
                }
            }
        }
        private static string SaoLuuTepCu(NhomThuMucLuuTru nhom, string tepCu, string thuMucSaoLuu)
        {
            Directory.CreateDirectory(thuMucSaoLuu);
            string ten = Path.GetFileNameWithoutExtension(tepCu);
            string duongDan = Path.Combine(thuMucSaoLuu,
                $"{ten}_BackupTruocKhiNhap_{DateTime.Now:yyyyMMdd_HHmmss_fff}{Path.GetExtension(tepCu)}.bak");
            try
            {
                if (nhom.LaSqlite) ChupSqlite(tepCu, duongDan);
                else File.Copy(tepCu, duongDan, overwrite: false);
                var fi = new FileInfo(duongDan);
                if (!fi.Exists || fi.Length == 0) throw new IOException("Bản sao lưu rỗng.");
                return duongDan;
            }
            catch (Exception ex)
            {
                XoaTepBestEffort(duongDan);
                throw new IOException($"Không tạo được bản sao lưu bảo hiểm của '{Path.GetFileName(tepCu)}' nên hủy ghi đè: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// Chuẩn bị ghi đè một CSDL SQLite đang có: nhả kết nối còn nằm trong pool, xóa -wal/-shm/-journal cũ.
        /// Nếu tệp phụ đang bị giữ (CSDL đang mở ở nơi khác) thì dừng NGAY, chưa đụng tới tệp chính.
        /// </summary>
        private static void GiaiPhongSqliteTruocKhiThay(string tepDb)
        {
            SqliteConnection.ClearAllPools();
            foreach (string hau in HauToPhuSqlite)
            {
                string phu = tepDb + hau;
                try { XoaTep(phu); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    throw new IOException(
                        $"CSDL '{Path.GetFileName(tepDb)}' đang được sử dụng nên không ghi đè được. " +
                        "Hãy đóng các màn hình đang xem dữ liệu lịch sử rồi nhập lại.", ex);
                }
            }
        }
        /// <summary>
        /// Dọn tệp đã nhập xong khỏi gói trên Desktop. Chỉ xóa đúng tệp có tên trong <paramref name="tepDaXong"/>; khi không còn tệp nào của
        /// thư mục này chưa nhập thì xóa tệp xác nhận và thư mục gói (chỉ khi thư mục rỗng). Không bao giờ ném lỗi.
        /// </summary>
        public static void DonGoiDaNhap(GoiNhapLuuTru goi, IEnumerable<string> tepDaXong)
        {
            try
            {
                var xong = new HashSet<string>(tepDaXong, StringComparer.OrdinalIgnoreCase);
                if (!Directory.Exists(goi.ThuMucGoi) || LaLienKet(goi.ThuMucGoi)) return;
                foreach (string ten in xong)
                {
                    if (!goi.Nhom.HopLe(ten)) continue;
                    string tep = Path.Combine(goi.ThuMucGoi, ten);
                    XoaTepBestEffort(tep);
                    if (goi.Nhom.LaSqlite) XoaTepPhuSqliteBestEffort(tep);   // mở CSDL trong gói (kể cả chỉ đọc) có thể để lại -wal/-shm
                }
                var conLai = goi.Tep.Where(m => !xong.Contains(m.TenFile) && File.Exists(Path.Combine(goi.ThuMucGoi, m.TenFile))).ToList();
                if (conLai.Count > 0)
                {
                    // Giữ gói để thử lại các tệp còn lại; tệp xác nhận chỉ còn liệt kê đúng những tệp này
                    // (nếu không, lần quét sau sẽ báo "thiếu tệp" cho các tệp đã nhập và đã dọn).
                    if (xong.Count > 0) TaoXacNhan(goi.Nhom, goi.ThuMucGoi, conLai, goi.PhienBan);
                    return;
                }
                XoaTepBestEffort(Path.Combine(goi.ThuMucGoi, TenTepXacNhan(goi.Nhom)));
                if (!Directory.EnumerateFileSystemEntries(goi.ThuMucGoi).Any())
                    Directory.Delete(goi.ThuMucGoi, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[DonGoiDaNhap] " + ex.Message);
            }
        }
    }
}