using Microsoft.Data.Sqlite;
using PhanMemThiDua2026;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
public static class Module_QuanLyHoSoDb
{
    private const string THU_MUC_LUU_TRU_GOC = "HoSoTepMaHoa";
    ///Cách gọi nơi khác          // 1. Xác định đường dẫn tới csdl4.db dựa trên thư mục CSDL chung của hệ thống
    //string csdl4Path = Path.Combine(Module_DanduongGPS.ThuMucCoSoDuLieu, "csdl4.db");
    //// 2. Gọi hàm khởi tạo bảng HoSoTepMaHoa
    //Module_QuanLyHoSoDb.KhoiTaoBangHoSoTep(csdl4Path);
    /// <summary>
    /// Giai đoạn 1: Khởi tạo bảng HoSoTepMaHoa trong csdl4.db
    /// </summary>
    public static void KhoiTaoBangHoSoTep(string csdl4Path)
    {
        if (string.IsNullOrWhiteSpace(csdl4Path)) return;
        try
        {
            using var conn = new SqliteConnection($"Data Source={csdl4Path}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS HoSoTepMaHoa (
                    ID INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    MaHoSo TEXT NOT NULL,
                    Nam INTEGER NOT NULL,
                    LoaiHoSo TEXT NOT NULL,
                    SoVanBan TEXT,
                    NgayBanHanh TEXT,
                    CoQuanBanHanh TEXT,
                    TrichYeu TEXT,
                    TenTepGoc TEXT NOT NULL,
                    DuoiTepGoc TEXT NOT NULL,
                    DuongDanTuongDoi TEXT NOT NULL,
                    DungLuongGoc INTEGER,
                    DungLuongMaHoa INTEGER,
                    PhienBanDinhDang TEXT DEFAULT 'v4',
                    ThuatToanMaHoa TEXT DEFAULT 'AES-256-GCM',
                    SHA256TepMaHoa TEXT,
                    TrangThai TEXT DEFAULT 'HienHanh',
                    GhiChu TEXT,
                    NguoiTao TEXT,
                    NgayTao TEXT,
                    NgayCapNhat TEXT
                );
                CREATE INDEX IF NOT EXISTS IX_HoSoTepMaHoa_MaHoSo ON HoSoTepMaHoa(MaHoSo);
                CREATE INDEX IF NOT EXISTS IX_HoSoTepMaHoa_Nam_Loai ON HoSoTepMaHoa(Nam, LoaiHoSo);
            ";
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khởi tạo bảng hồ sơ tệp: {ex.Message}", "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    /// <summary>
    /// 1. HÀM MÃ HÓA TỆP: Đọc tệp gốc, mã hóa chuẩn AES-GCM theo khối (tránh tràn RAM), 
    /// lưu vào thư mục phân cấp và ghi metadata vào csdl4.db.
    /// </summary>
    public static bool MaHoaVaLuuTruTep(
        string csdl4Path,
        string sourceFilePath,
        int nam,
        string maHoSo,
        string loaiHoSo,
        string soVanBan,
        string ngayBanHanh,
        string coQuanBanHanh,
        string trichYeu,
        string nguoiTao,
        out string errorMessage)
    {
        errorMessage = string.Empty;
        if (!File.Exists(sourceFilePath))
        {
            errorMessage = "Không tìm thấy tệp nguồn cần mã hóa.";
            return false;
        }
        try
        {
            FileInfo fiGoc = new FileInfo(sourceFilePath);
            long dungLuongGoc = fiGoc.Length;
            string tenTepGoc = fiGoc.Name;
            string duoiTepGoc = fiGoc.Extension.ToLowerInvariant();
            // Kiểm tra định dạng hỗ trợ
            if (duoiTepGoc != ".pdf" && duoiTepGoc != ".xlsx" && duoiTepGoc != ".xls" && duoiTepGoc != ".docx" && duoiTepGoc != ".doc")
            {
                errorMessage = $"Định dạng tệp '{duoiTepGoc}' không được hỗ trợ.";
                return false;
            }
            // Tạo cấu trúc thư mục lưu trữ: HoSoTepMaHoa/{Nam}/{MaHoSo}/
            string appRoot = AppContext.BaseDirectory;
            string thuMucHoSo = Path.Combine(appRoot, THU_MUC_LUU_TRU_GOC, nam.ToString(), maHoSo);
            Directory.CreateDirectory(thuMucHoSo);
            // Tên tệp mã hóa ngẫu nhiên/định danh duy nhất đuôi .bmdb
            string tenTepBmdb = $"{Guid.NewGuid():N}.bmdb";
            string duongDanTuyetDoiBmdb = Path.Combine(thuMucHoSo, tenTepBmdb);
            // Lấy khóa hệ thống từ Module_BaoMatAES
            byte[] systemKey = Module_BaoMatAES.LayKhoaHeThongDungChung();
            // Gọi động cơ mã hóa luồng phân đoạn (Stream Chunked AES-GCM) sẵn có trong hệ thống
            Module_BaoMatAES.MaHoaCSDL(sourceFilePath, duongDanTuyetDoiBmdb, systemKey);
            CryptographicOperations.ZeroMemory(systemKey);
            if (!File.Exists(duongDanTuyetDoiBmdb))
            {
                errorMessage = "Quá trình mã hóa không tạo được tệp .bmdb.";
                return false;
            }
            FileInfo fiBmdb = new FileInfo(duongDanTuyetDoiBmdb);
            long dungLuongMaHoa = fiBmdb.Length;
            // Tính mã SHA-256 tệp mã hóa để kiểm tra toàn vẹn
            string sha256Hash = TinhSHA256Tep(duongDanTuyetDoiBmdb);
            // Đường dẫn tương đối lưu trong CSDL để đảm bảo tính Portable (sao chép sang máy khác không hỏng đường dẫn)
            string duongDanTuongDoi = Path.Combine(THU_MUC_LUU_TRU_GOC, nam.ToString(), maHoSo, tenTepBmdb);
            // Ghi thông tin vào bảng SQLite (csdl4.db)
            using var conn = new SqliteConnection($"Data Source={csdl4Path}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO HoSoTepMaHoa (
                    MaHoSo, Nam, LoaiHoSo, SoVanBan, NgayBanHanh, CoQuanBanHanh, TrichYeu,
                    TenTepGoc, DuoiTepGoc, DuongDanTuongDoi, DungLuongGoc, DungLuongMaHoa,
                    PhienBanDinhDang, ThuatToanMaHoa, SHA256TepMaHoa, TrangThai, NguoiTao, NgayTao
                ) VALUES (
                    @MaHoSo, @Nam, @LoaiHoSo, @SoVanBan, @NgayBanHanh, @CoQuanBanHanh, @TrichYeu,
                    @TenTepGoc, @DuoiTepGoc, @DuongDanTuongDoi, @DungLuongGoc, @DungLuongMaHoa,
                    'v4', 'AES-256-GCM', @SHA256TepMaHoa, 'HienHanh', @NguoiTao, @NgayTao
                );
            ";
            cmd.Parameters.AddWithValue("@MaHoSo", maHoSo);
            cmd.Parameters.AddWithValue("@Nam", nam);
            cmd.Parameters.AddWithValue("@LoaiHoSo", loaiHoSo);
            cmd.Parameters.AddWithValue("@SoVanBan", soVanBan ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@NgayBanHanh", ngayBanHanh ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CoQuanBanHanh", coQuanBanHanh ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TrichYeu", trichYeu ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TenTepGoc", tenTepGoc);
            cmd.Parameters.AddWithValue("@DuoiTepGoc", duoiTepGoc);
            cmd.Parameters.AddWithValue("@DuongDanTuongDoi", duongDanTuongDoi);
            cmd.Parameters.AddWithValue("@DungLuongGoc", dungLuongGoc);
            cmd.Parameters.AddWithValue("@DungLuongMaHoa", dungLuongMaHoa);
            cmd.Parameters.AddWithValue("@SHA256TepMaHoa", sha256Hash);
            cmd.Parameters.AddWithValue("@NguoiTao", nguoiTao ?? "Hệ thống");
            cmd.Parameters.AddWithValue("@NgayTao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Lỗi mã hóa và lưu trữ tệp: {ex.Message}";
            return false;
        }
    }
    /// <summary>
    /// 2. HÀM GIẢI MÃ TỆP: Đọc tệp .bmdb từ đường dẫn tương đối, giải mã xác thực toàn vẹn 
    /// ra tệp tạm tại thư mục Windows Temp và trả về đường dẫn để mở.
    /// </summary>
    public static bool GiaiMaVaMoTep(string duongDanTuongDoi, string tenTepGoc, out string decryptedTempFilePath, out string errorMessage)
    {
        decryptedTempFilePath = string.Empty;
        errorMessage = string.Empty;
        string appRoot = AppContext.BaseDirectory;
        string fullBmdbPath = Path.Combine(appRoot, duongDanTuongDoi);
        if (!File.Exists(fullBmdbPath))
        {
            errorMessage = "Không tìm thấy tệp mã hóa .bmdb trên đĩa.";
            return false;
        }
        try
        {
            // Tạo tệp tạm đích có giữ nguyên phần mở rộng gốc (ví dụ: .pdf, .docx, .xlsx)
            string extension = Path.GetExtension(tenTepGoc);
            string safeTempFileName = $"PMTD_View_{Guid.NewGuid():N}{extension}";
            decryptedTempFilePath = Path.Combine(Path.GetTempPath(), safeTempFileName);
            // Lấy khóa hệ thống
            byte[] systemKey = Module_BaoMatAES.LayKhoaHeThongDungChung();
            // Gọi động cơ giải mã luồng phân đoạn (Stream Chunked AES-GCM với xác thực Tag thời gian thực)
            Module_BaoMatAES.GiaiMaCSDL(fullBmdbPath, decryptedTempFilePath, systemKey);
            CryptographicOperations.ZeroMemory(systemKey);
            if (!File.Exists(decryptedTempFilePath) || new FileInfo(decryptedTempFilePath).Length == 0)
            {
                errorMessage = "Giải mã thất bại: Tệp đầu ra trống hoặc bị lỗi cấu trúc.";
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Lỗi giải mã tệp (có thể do tệp bị hỏng hoặc sai khóa): {ex.Message}";
            if (!string.IsNullOrEmpty(decryptedTempFilePath) && File.Exists(decryptedTempFilePath))
            {
                try { File.Delete(decryptedTempFilePath); } catch { }
            }
            return false;
        }
    }
    // Tiện ích tính SHA-256 tệp mã hóa
    private static string TinhSHA256Tep(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan);
            return Convert.ToHexString(SHA256.HashData(stream));
        }
        catch
        {
            return string.Empty;
        }
    }
}