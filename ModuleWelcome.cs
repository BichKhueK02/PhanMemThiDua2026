using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace PhanMemThiDua2026
{
    public static class ModuleWelcome
    {
        //"Dấu vân tay môi trường" (GenerateCurrentFingerprint)
        private static string Csdl2Path
        {
            get
            {
                try
                {
                    string path = Module_DanduongGPS.DuongDanCSDL2;
                    if (string.IsNullOrWhiteSpace(path)) return null;
                    return path;
                }
                catch
                {
                    return null;
                }
            }
        }

        // =========================================================
        // 1. TẠO DẤU VÂN TAY "TÀNG HÌNH" - HIỆU SUẤT CAO, AN TOÀN AV
        // =========================================================
        private static string GenerateCurrentFingerprint()
        {
            try
            {
                // Lấy tên tài khoản hiện tại trong RAM phần mềm
                string taiKhoan = Module_TaiKhoan.TenTaiKhoan_RAM ?? "SYSTEM";

                // Lấy tên máy tính
                string tenMay = Environment.MachineName ?? "UNKNOWN_PC";

                // Lấy tên người dùng Windows (Đổi User Windows cũng tính là người mới)
                string userWindows = Environment.UserName ?? "UNKNOWN_USER";

                // Lấy số định danh thư mục Windows (Đảm bảo tính duy nhất của hệ điều hành)
                string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                long dirTicks = 0;
                if (Directory.Exists(winDir))
                {
                    dirTicks = new DirectoryInfo(winDir).CreationTimeUtc.Ticks;
                }

                // Gộp tất cả thành một chuỗi đặc trưng môi trường
                string rawData = $"{taiKhoan}|{tenMay}|{userWindows}|{dirTicks}";

                // Băm SHA-256 để tạo chuỗi bảo mật 64 ký tự (Antivirus hoàn toàn không soi hành vi này)
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                    StringBuilder sb = new StringBuilder(64);
                    foreach (byte b in bytes)
                    {
                        sb.Append(b.ToString("x2"));
                    }
                    return sb.ToString();
                }
            }
            catch
            {
                // Nếu lỗi, trả về một chuỗi ngẫu nhiên cố định để không làm sập phần mềm
                return "FAILSAFE_FINGERPRINT_2026";
            }
        }

        // =========================================================
        // 2. KHỞI TẠO BẢNG DỮ LIỆU
        // =========================================================
        public static void KhoiTaoBangWelcome()
        {
            string dbPath = Csdl2Path;
            if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath)) return;

            try
            {
                using var conn = new SqliteConnection($"Data Source={dbPath};Pooling=True;");
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Welcome (
                        ID INTEGER PRIMARY KEY,
                        TrangThai TEXT
                    );";
                cmd.ExecuteNonQuery();

                // Đảm bảo dòng ID = 1 luôn tồn tại
                cmd.CommandText = "SELECT COUNT(*) FROM Welcome WHERE ID = 1;";
                long count = (long)cmd.ExecuteScalar();

                if (count == 0)
                {
                    cmd.CommandText = "INSERT INTO Welcome (ID, TrangThai) VALUES (1, @val);";
                    cmd.Parameters.AddWithValue("@val", "FIRST_RUN_BLANK");
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("KhoiTaoBangWelcome lỗi: " + ex.Message);
            }
        }

        // =========================================================
        // 3. ĐỌC DẤU VÂN TAY CŨ TỪ CSDL
        // =========================================================
        private static string GetSavedFingerprintFromDB()
        {
            string dbPath = Csdl2Path;
            if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath)) return string.Empty;

            try
            {
                using var conn = new SqliteConnection($"Data Source={dbPath};Pooling=True;");
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT TrangThai FROM Welcome WHERE ID = 1 LIMIT 1;";
                object result = cmd.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                    return string.Empty;

                return result.ToString().Trim();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GetSavedFingerprintFromDB lỗi: " + ex.Message);
                return string.Empty;
            }
        }

        // =========================================================
        // 4. LƯU DẤU VÂN TAY MỚI VÀO CSDL
        // =========================================================
        public static void SaveFingerprintAfterShow()
        {
            string dbPath = Csdl2Path;
            if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath)) return;

            try
            {
                string newFingerprint = GenerateCurrentFingerprint();

                using var conn = new SqliteConnection($"Data Source={dbPath};Pooling=True;");
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Welcome SET TrangThai = @val WHERE ID = 1;";
                cmd.Parameters.AddWithValue("@val", newFingerprint);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SaveFingerprintAfterShow lỗi: " + ex.Message);
            }
        }

        // =========================================================
        // 5. BỘ NÃO ĐIỀU KHIỂN - GỌI KHI KHỞI ĐỘNG PHẦN MỀM
        // =========================================================
        public static void ShowWelcomeIfNeeded()
        {
            // Kiểm tra đường dẫn CSDL trước khi làm việc
            if (string.IsNullOrEmpty(Csdl2Path) || !File.Exists(Csdl2Path)) return;

            try
            {
                // Bước 1: Đảm bảo cấu trúc bảng ổn định
                KhoiTaoBangWelcome();

                // Bước 2: Lấy vân tay cũ và vân tay hiện tại để so sánh
                string savedPrint = GetSavedFingerprintFromDB();
                string currentPrint = GenerateCurrentFingerprint();

                // Bước 3: Nếu phát hiện đổi người hoặc đổi máy (vân tay khác nhau)
                if (savedPrint != currentPrint)
                {
                    // Chạy Form hiển thị giới thiệu của bạn
                    using var frm = new FormWelcome();

                    // ShowDialog ép người dùng đọc xong, nếu họ đóng Form thành công
                    if (frm.ShowDialog() == DialogResult.OK || frm.DialogResult == DialogResult.Cancel)
                    {
                        // Ghi dấu vân tay mới xuống làm mốc chặn cho lần sau
                        SaveFingerprintAfterShow();
                    }
                }
            }
            catch (Exception ex)
            {
                // Bẫy lỗi Fail-Safe tuyệt đối: Có lỗi xảy ra thì bỏ qua Welcome để người dùng vào thẳng phần mềm làm việc
                Debug.WriteLine("ShowWelcomeIfNeeded lỗi: " + ex.Message);
            }
        }
    }
}
