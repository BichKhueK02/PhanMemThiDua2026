using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace PhanMemThiDua2026
{
    public static class Module_HuongDanSuDung
    {
        // =========================================================================
        // 1. CƠ CHẾ DANH SÁCH CHO PHÉP (WHITELIST) - MỞ RỘNG DỄ DÀNG Ở ĐÂY
        // =========================================================================
        private static readonly HashSet<string> AllowedFiles = new(StringComparer.OrdinalIgnoreCase)
        {
            "HuongDanSuDung.html",
            "HuongDanSuDung - Stitch Google.html",
            "version.txt",
            "NhatKy_DonRac.txt" // Phải cho phép file nhật ký tồn tại
        };
        public static string FolderName => "HuongDanSuDung";
        public static string FileName => "HuongDanSuDung.html";
        public static string VersionFileName => "version.txt";
        public static string SrcBackupDir => Path.Combine(AppContext.BaseDirectory, "Database Backup", FolderName);
        public static string SrcMainDir => Path.Combine(AppContext.BaseDirectory, "Database", FolderName);
        public static string AppDataDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhanMemThiDua2026", FolderName);
        public static string AppDataHtmlFile => Path.Combine(AppDataDir, FileName);
        public static string AppDataVersionFile => Path.Combine(AppDataDir, VersionFileName);
        private static readonly object _lock = new();
        // =========================================================================
        // BẮT ĐẦU ĐOẠN CODE GIỮ NGUYÊN 100%
        // =========================================================================
        private static string BuildManifest(string hash)
        {
            return
        $@"=== PHAN MEM THI DUA 2026 MANIFEST ===

ManifestVersion: 2.1

Application: Phan mem Thi dua 2026
Module: HuongDanSuDung
ModuleType: Offline Documentation

SoftwareVersion: {Module_PhienBan.SoftwareVersion ?? "Unknown"}
ReleaseDate: {Module_PhienBan.NgayThangNamHeThong ?? "Unknown"}
Developer: {Module_PhienBan.NguoiPhatTrienPhanMem ?? "Internal Organization"}

Publisher: Internal Organization
ApplicationSignature: Self-Signed Code Signing

Platform: Windows
Framework: .NET WinForms
Architecture: x64

RuntimeRequirement: .NET Desktop Runtime
Compatibility: Windows 7 -> Windows 11

Integrity: SHA256 Validated
HashAlgorithm: SHA256
SHA256: {hash}

Synchronization: Hash Validation Required
StorageMode: Local Offline Cache

DeploymentMode: Offline Internal
Environment: Production

ExecutionMode: Local Shell Open
NetworkAccess: Disabled

FallbackSource: Internal Application Resources
AccessScope: Internal Use Only

Build: 1.0.{DateTime.UtcNow:yyMMdd}

UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}
Local: {DateTime.Now:dd/MM/yyyy HH:mm:ss}

Status: Ready
";
        }
        private static void SafeWrite(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content, Encoding.UTF8);
        }
        // =========================================================================
        // 3. CƠ CHẾ PHỤC HỒI NGƯỢC (REVERSE RECOVERY) - XỬ LÝ TRƯỜNG HỢP HY HỮU
        // =========================================================================
        private static bool TryDeepRecoverFromAppData()
        {
            return false; // 🟢 Đã vô hiệu hóa để tránh IT quét: Không dùng AppData nữa
        }
        private static void ThucHienPhucHoiNguoc()
        {
            return; // 🟢 Đã vô hiệu hóa để tránh IT quét: Không dùng AppData nữa
        }
        // =========================================================================
        // 4. CƠ CHẾ ĐỒNG BỘ 2 CHIỀU THÔNG MINH
        // =========================================================================
        public static void SyncMasterVersion()
        {
            try
            {
                Directory.CreateDirectory(SrcMainDir);
                Directory.CreateDirectory(SrcBackupDir);

                string mainHtml = Path.Combine(SrcMainDir, FileName);
                string backupHtml = Path.Combine(SrcBackupDir, FileName);

                // KIỂM TRA TRƯỜNG HỢP MẤT SẠCH Ở DB & BACKUP
                if (!File.Exists(mainHtml) && !File.Exists(backupHtml))
                {
                    return; // 🟢 Đã vô hiệu hóa AppData, mất cả 2 nơi thì dừng lại
                }

                foreach (string fileName in AllowedFiles)
                {
                    if (fileName.Equals(VersionFileName, StringComparison.OrdinalIgnoreCase) ||
                        fileName.Equals("NhatKy_DonRac.txt", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string mainFile = Path.Combine(SrcMainDir, fileName);
                    string backupFile = Path.Combine(SrcBackupDir, fileName);

                    bool inMain = File.Exists(mainFile);
                    bool inBackup = File.Exists(backupFile);

                    if (!inMain && !inBackup) continue;

                    if (inMain && !inBackup)
                    {
                        File.Copy(mainFile, backupFile, true);
                        Debug.WriteLine($"[SYNC] Đã tự động chép {fileName} từ Database sang Backup.");
                    }
                    else if (!inMain && inBackup)
                    {
                        // 🟢 LOGIC GỐC CỦA BẠN: Nếu ở Main (Database) bị mất, sẽ tự động lấy từ Backup đắp qua
                        File.Copy(backupFile, mainFile, true);
                        Debug.WriteLine($"[SYNC] Đã phục hồi {fileName} từ Backup về Database.");
                    }
                    else if (GetFileHash(mainFile) != GetFileHash(backupFile))
                    {
                        File.Copy(mainFile, backupFile, true);
                        Debug.WriteLine($"[SYNC] Đã cập nhật bản mới {fileName} từ Database sang Backup.");
                    }
                }

                if (File.Exists(mainHtml))
                {
                    string hash = GetFileHash(mainHtml);
                    string manifest = BuildManifest(hash);

                    SafeWrite(Path.Combine(SrcBackupDir, VersionFileName), manifest);
                    SafeWrite(Path.Combine(SrcMainDir, VersionFileName), manifest);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[SYNC MASTER ERROR] " + ex.Message);
            }
        }
        // =========================================================================
        // 5. CƠ CHẾ TIÊU DIỆT RÁC & BẢO VỆ PHẦN MỀM 
        // =========================================================================
       private static void TieuDietRac()
        {
            return; // 🟢 Đã vô hiệu hóa thao tác vào AppData để tránh IT quét
        }
        // =========================
        // 6. CHECK + UPDATE APPDATA 
        // =========================
        public static void UpdateLocal()
        {
            return; // 🟢 Đã vô hiệu hóa AppData
        }
        // =========================
        // 7. EXTRACT HASH 
        // =========================
        private static string ExtractHash(string file)
        {
            try
            {
                if (!File.Exists(file)) return "0";

                foreach (var line in File.ReadLines(file))
                {
                    if (line.StartsWith("SHA256:"))
                        return line.Replace("SHA256:", "").Trim();
                }
                return "0";
            }
            catch { return "0"; }
        }

        public static void EnsureReady()
        {
            SyncMasterVersion();
            // UpdateLocal(); // Bỏ qua AppData
        }

        private static string GetFileHash(string path)
        {
            try
            {
                if (!File.Exists(path)) return "0";
                using var sha = SHA256.Create();
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return Convert.ToHexString(sha.ComputeHash(fs));
            }
            catch { return "0"; }
        }

        private static void EnsureFile(string src, string dst)
        {
            try
            {
                bool needCopy = !File.Exists(dst) || GetFileHash(src) != GetFileHash(dst);
                if (!needCopy) return;

                Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
                File.Copy(src, dst, true);
            }
            catch { }
        }

        public static string LayCheDoXemHuongDan()
        {
            try
            {
                string dbPath = Module_DanduongGPS.DuongDanCSDL2;
                using var cn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
                cn.Open();

                using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='CheDo_XemHuongDan'";
                if (cmd.ExecuteScalar() == null) return "Chế độ web";

                cmd.CommandText = "SELECT CheDoXem_HuongDanSD FROM CheDo_XemHuongDan WHERE ID = 1";
                var result = cmd.ExecuteScalar();

                return result?.ToString() ?? "Chế độ web";
            }
            catch { return "Chế độ web"; }
        }

        public static string TimFileHuongDanPdf()
        {
            try
            {
                string pdfPath = Path.Combine(AppContext.BaseDirectory, "Database", FolderName, "HuongDan.pdf");
                if (!File.Exists(pdfPath)) return string.Empty;
                return pdfPath;
            }
            catch { return string.Empty; }
        }

        private static readonly object _processLock = new();
        private static Process? _huongDanWebProcess;

        public static bool MoHuongDanBangWeb()
        {
            try
            {
                // Gọi SyncMasterVersion để tự động copy từ "Database Backup" về "Database" nếu tệp bị mất
                SyncMasterVersion();

                // 🟢 ĐỔI ĐƯỜNG DẪN: Mở trực tiếp từ thư mục Database nội bộ thay vì AppData
                string targetWebPath = Path.Combine(SrcMainDir, FileName);
                if (!File.Exists(targetWebPath)) return false;

                lock (_processLock)
                {
                    if (_huongDanWebProcess != null)
                    {
                        try
                        {
                            if (!_huongDanWebProcess.HasExited)
                            {
                                if (_huongDanWebProcess.MainWindowHandle != IntPtr.Zero)
                                {
                                    SetForegroundWindow(_huongDanWebProcess.MainWindowHandle);
                                    return true;
                                }
                            }
                        }
                        catch { }
                    }

                    _huongDanWebProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = targetWebPath,
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Normal
                    });

                    return _huongDanWebProcess != null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[HUONG DAN WEB ERROR] " + ex);
                return false;
            }
        }

        public static void DongHuongDan()
        {
            try
            {
                lock (_processLock)
                {
                    if (_huongDanWebProcess == null) return;
                    try
                    {
                        if (!_huongDanWebProcess.HasExited)
                        {
                            _huongDanWebProcess.CloseMainWindow();
                            if (!_huongDanWebProcess.WaitForExit(1000))
                            {
                                _huongDanWebProcess.Kill(true);
                            }
                        }
                    }
                    catch { }

                    _huongDanWebProcess.Dispose();
                    _huongDanWebProcess = null;
                }
            }
            catch { }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public static bool TryActivateHuongDan()
        {
            try
            {
                lock (_processLock)
                {
                    if (_huongDanWebProcess == null) return false;
                    if (_huongDanWebProcess.HasExited)
                    {
                        _huongDanWebProcess.Dispose();
                        _huongDanWebProcess = null;
                        return false;
                    }
                    if (_huongDanWebProcess.MainWindowHandle != IntPtr.Zero)
                    {
                        SetForegroundWindow(_huongDanWebProcess.MainWindowHandle);
                        return true;
                    }
                    return false;
                }
            }
            catch { return false; }
        }
    }
}

//using System.Diagnostics;
//using System.Security.Cryptography;
//using System.Text;

//namespace PhanMemThiDua2026
//{
//    public static class Module_HuongDanSuDung
//    {
//        // =========================================================================
//        // 1. CƠ CHẾ DANH SÁCH CHO PHÉP (WHITELIST) - MỞ RỘNG DỄ DÀNG Ở ĐÂY
//        // =========================================================================
//        private static readonly HashSet<string> AllowedFiles = new(StringComparer.OrdinalIgnoreCase)
//        {
//            "HuongDanSuDung.html",
//            "HuongDanSuDung - Stitch Google.html",
//            "version.txt",
//            "NhatKy_DonRac.txt" // Phải cho phép file nhật ký tồn tại
//        };

//        public static string FolderName => "HuongDanSuDung";
//        public static string FileName => "HuongDanSuDung.html";
//        public static string VersionFileName => "version.txt";
//        public static string SrcBackupDir => Path.Combine(AppContext.BaseDirectory, "Database Backup", FolderName);
//        public static string SrcMainDir => Path.Combine(AppContext.BaseDirectory, "Database", FolderName);
//        public static string AppDataDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhanMemThiDua2026", FolderName);
//        public static string AppDataHtmlFile => Path.Combine(AppDataDir, FileName);
//        public static string AppDataVersionFile => Path.Combine(AppDataDir, VersionFileName);

//        private static readonly object _lock = new();

//        // =========================================================================
//        // BẮT ĐẦU ĐOẠN CODE GIỮ NGUYÊN 100%
//        // =========================================================================
//        private static string BuildManifest(string hash)
//        {
//            return
//        $@"=== PHAN MEM THI DUA 2026 MANIFEST ===

//ManifestVersion: 2.1

//Application: Phan mem Thi dua 2026
//Module: HuongDanSuDung
//ModuleType: Offline Documentation

//SoftwareVersion: {Module_PhienBan.SoftwareVersion ?? "Unknown"}
//ReleaseDate: {Module_PhienBan.NgayThangNamHeThong ?? "Unknown"}
//Developer: {Module_PhienBan.NguoiPhatTrienPhanMem ?? "Internal Organization"}

//Publisher: Internal Organization
//ApplicationSignature: Self-Signed Code Signing

//Platform: Windows
//Framework: .NET WinForms
//Architecture: x64

//RuntimeRequirement: .NET Desktop Runtime
//Compatibility: Windows 7 -> Windows 11

//Integrity: SHA256 Validated
//HashAlgorithm: SHA256
//SHA256: {hash}

//Synchronization: Hash Validation Required
//StorageMode: Local Offline Cache

//DeploymentMode: Offline Internal
//Environment: Production

//ExecutionMode: Local Shell Open
//NetworkAccess: Disabled

//FallbackSource: Internal Application Resources
//AccessScope: Internal Use Only

//Build: 1.0.{DateTime.UtcNow:yyMMdd}

//UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}
//Local: {DateTime.Now:dd/MM/yyyy HH:mm:ss}

//Status: Ready
//";
//        }
//        private static void SafeWrite(string path, string content)
//        {
//            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
//            File.WriteAllText(path, content, Encoding.UTF8);
//        }

//        // =========================================================================
//        // 3. CƠ CHẾ PHỤC HỒI NGƯỢC (REVERSE RECOVERY) - XỬ LÝ TRƯỜNG HỢP HY HỮU
//        // =========================================================================
//        private static bool TryDeepRecoverFromAppData()
//        {
//            if (!File.Exists(AppDataHtmlFile))
//                return false; // AppData cũng mất thì chịu

//            string savedHash = ExtractHash(AppDataVersionFile);
//            string actualHash = GetFileHash(AppDataHtmlFile);

//            bool isHashValid = (savedHash != "0" && string.Equals(savedHash, actualHash, StringComparison.OrdinalIgnoreCase));

//            if (isHashValid)
//            {
//                // Trường hợp 1: Trùng Hash -> Đồng bộ lại thầm lặng
//                ThucHienPhucHoiNguoc();

//                Module_NhatKy.GhiNhatKy(
//                    taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
//                    hanhDong: "Tự động phục hồi Hướng dẫn sử dụng",
//                    ghiChu: "Hệ thống tự động phục hồi tệp từ AppData về Database do trùng khớp mã Hash bảo mật."
//                );
//                return true;
//            }
//            else
//            {
//                // Trường hợp 2: Sai Hash / Mất Hash -> Hỏi người dùng
//                DialogResult diagResult = MessageBox.Show(
//                    "Hệ thống phát hiện tài liệu hướng dẫn trên máy chủ (Database) đã bị mất, nhưng tìm thấy bản sao lưu cục bộ trên máy tính này.\n\n" +
//                    "Tuy nhiên, bản sao lưu này KHÔNG THỂ xác minh tính toàn vẹn bảo mật (mã Hash không khớp hoặc mất tệp kiểm chứng).\n" +
//                    "Bạn có muốn ÉP BUỘC khôi phục dữ liệu từ bản sao lưu này không?",
//                    "Cảnh báo bảo mật - Khôi phục dữ liệu",
//                    MessageBoxButtons.YesNo,
//                    MessageBoxIcon.Warning);

//                if (diagResult == DialogResult.Yes)
//                {
//                    ThucHienPhucHoiNguoc();

//                    Module_NhatKy.GhiNhatKy(
//                        taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
//                        hanhDong: "Ép buộc phục hồi Hướng dẫn sử dụng",
//                        ghiChu: "Người dùng chọn ÉP BUỘC phục hồi từ AppData về Database dù sai mã Hash."
//                    );
//                    return true;
//                }
//                else
//                {
//                    Module_NhatKy.GhiNhatKy(
//                        taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
//                        hanhDong: "Từ chối phục hồi Hướng dẫn sử dụng",
//                        ghiChu: "Người dùng từ chối phục hồi từ AppData do sai mã Hash."
//                    );
//                    return false;
//                }
//            }
//        }

//        private static void ThucHienPhucHoiNguoc()
//        {
//            Directory.CreateDirectory(SrcMainDir);
//            Directory.CreateDirectory(SrcBackupDir);

//            foreach (string fileName in AllowedFiles)
//            {
//                if (fileName.Equals("NhatKy_DonRac.txt", StringComparison.OrdinalIgnoreCase) ||
//                    fileName.Equals(VersionFileName, StringComparison.OrdinalIgnoreCase))
//                {
//                    continue; // version.txt sẽ được hàm SyncMaster tạo lại mới sau
//                }

//                string sourcePath = Path.Combine(AppDataDir, fileName);
//                if (File.Exists(sourcePath))
//                {
//                    File.Copy(sourcePath, Path.Combine(SrcMainDir, fileName), true);
//                    File.Copy(sourcePath, Path.Combine(SrcBackupDir, fileName), true);
//                }
//            }
//        }

//        // =========================================================================
//        // 4. CƠ CHẾ ĐỒNG BỘ 2 CHIỀU THÔNG MINH
//        // =========================================================================
//        public static void SyncMasterVersion()
//        {
//            try
//            {
//                Directory.CreateDirectory(SrcMainDir);
//                Directory.CreateDirectory(SrcBackupDir);

//                string mainHtml = Path.Combine(SrcMainDir, FileName);
//                string backupHtml = Path.Combine(SrcBackupDir, FileName);

//                // KIỂM TRA TRƯỜNG HỢP MẤT SẠCH Ở DB & BACKUP
//                if (!File.Exists(mainHtml) && !File.Exists(backupHtml))
//                {
//                    bool recovered = TryDeepRecoverFromAppData();
//                    if (!recovered) return;
//                }

//                foreach (string fileName in AllowedFiles)
//                {
//                    if (fileName.Equals(VersionFileName, StringComparison.OrdinalIgnoreCase) ||
//                        fileName.Equals("NhatKy_DonRac.txt", StringComparison.OrdinalIgnoreCase))
//                    {
//                        continue;
//                    }

//                    string mainFile = Path.Combine(SrcMainDir, fileName);
//                    string backupFile = Path.Combine(SrcBackupDir, fileName);

//                    bool inMain = File.Exists(mainFile);
//                    bool inBackup = File.Exists(backupFile);

//                    if (!inMain && !inBackup) continue;

//                    if (inMain && !inBackup)
//                    {
//                        File.Copy(mainFile, backupFile, true);
//                        Debug.WriteLine($"[SYNC] Đã tự động chép {fileName} từ Database sang Backup.");
//                    }
//                    else if (!inMain && inBackup)
//                    {
//                        File.Copy(backupFile, mainFile, true);
//                        Debug.WriteLine($"[SYNC] Đã phục hồi {fileName} từ Backup về Database.");
//                    }
//                    else if (GetFileHash(mainFile) != GetFileHash(backupFile))
//                    {
//                        File.Copy(mainFile, backupFile, true);
//                        Debug.WriteLine($"[SYNC] Đã cập nhật bản mới {fileName} từ Database sang Backup.");
//                    }
//                }

//                if (File.Exists(mainHtml))
//                {
//                    string hash = GetFileHash(mainHtml);
//                    string manifest = BuildManifest(hash);

//                    SafeWrite(Path.Combine(SrcBackupDir, VersionFileName), manifest);
//                    SafeWrite(Path.Combine(SrcMainDir, VersionFileName), manifest);
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine("[SYNC MASTER ERROR] " + ex.Message);
//            }
//        }

//        // =========================================================================
//        // 5. CƠ CHẾ TIÊU DIỆT RÁC & BẢO VỆ PHẦN MỀM 
//        // =========================================================================
//        private static void TieuDietRac()
//        {
//            if (!Directory.Exists(AppDataDir)) return;

//            int successCount = 0;
//            int failCount = 0;
//            bool hasGarbage = false;

//            StringBuilder logContent = new StringBuilder();
//            logContent.AppendLine("=====================================================");
//            logContent.AppendLine("NHẬT KÝ DỌN DẸP HỆ THỐNG (CƠ CHẾ BẢO VỆ PHẦN MỀM THI ĐUA)");
//            logContent.AppendLine("=====================================================");
//            logContent.AppendLine($"Người thực hiện (ADMIN): {Environment.UserName}");
//            logContent.AppendLine($"Tên máy tính           : {Environment.MachineName}");
//            logContent.AppendLine($"Ngày giờ thực hiện     : {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
//            logContent.AppendLine("Lý do kích hoạt        : Phát hiện dữ liệu lạ. Kích hoạt cơ chế bảo mật, tiêu diệt các tệp/thư mục không thuộc danh sách an toàn.");
//            logContent.AppendLine("-----------------------------------------------------");
//            logContent.AppendLine("DANH SÁCH CHI TIẾT:");

//            int index = 1;

//            foreach (var filePath in Directory.GetFiles(AppDataDir))
//            {
//                FileInfo fi = new FileInfo(filePath);

//                if (!AllowedFiles.Contains(fi.Name))
//                {
//                    hasGarbage = true;
//                    try
//                    {
//                        long sizeKb = fi.Length / 1024;
//                        fi.Attributes = FileAttributes.Normal;
//                        fi.Delete();

//                        logContent.AppendLine($"{index++}. [TỆP TIN] {fi.Name} - {sizeKb} KB -> Đã dọn dẹp thành công.");
//                        successCount++;
//                    }
//                    catch (Exception ex)
//                    {
//                        logContent.AppendLine($"{index++}. [TỆP TIN] {fi.Name} -> CHƯA DỌN THÀNH CÔNG (Lỗi: {ex.Message})");
//                        failCount++;
//                    }
//                }
//            }

//            foreach (var dirPath in Directory.GetDirectories(AppDataDir))
//            {
//                hasGarbage = true;
//                DirectoryInfo di = new DirectoryInfo(dirPath);
//                try
//                {
//                    di.Delete(true);
//                    logContent.AppendLine($"{index++}. [THƯ MỤC] {di.Name} -> Đã dọn dẹp thành công.");
//                    successCount++;
//                }
//                catch (Exception ex)
//                {
//                    logContent.AppendLine($"{index++}. [THƯ MỤC] {di.Name} -> CHƯA DỌN THÀNH CÔNG (Lỗi: {ex.Message})");
//                    failCount++;
//                }
//            }

//            if (hasGarbage)
//            {
//                logContent.AppendLine("-----------------------------------------------------");
//                logContent.AppendLine("TỔNG KẾT KẾT QUẢ DỌN DẸP:");
//                logContent.AppendLine($"- Tổng số mục đã xử lý: {successCount + failCount}");
//                logContent.AppendLine($"- Dọn thành công      : {successCount}");
//                logContent.AppendLine($"- Chưa dọn thành công : {failCount}");
//                logContent.AppendLine("=====================================================");

//                try
//                {
//                    string logFile = Path.Combine(AppDataDir, "NhatKy_DonRac.txt");
//                    File.WriteAllText(logFile, logContent.ToString(), Encoding.UTF8);
//                }
//                catch { }
//            }
//        }

//        // =========================
//        // 6. CHECK + UPDATE APPDATA 
//        // =========================
//        public static void UpdateLocal()
//        {
//            lock (_lock)
//            {
//                try
//                {
//                    string mainHtml = Path.Combine(SrcMainDir, FileName);
//                    string mainVer = Path.Combine(SrcMainDir, VersionFileName);

//                    if (!File.Exists(mainHtml) || !File.Exists(mainVer))
//                    {
//                        SyncMasterVersion();
//                        if (!File.Exists(mainHtml) || !File.Exists(mainVer)) return;
//                    }

//                    Directory.CreateDirectory(AppDataDir);
//                    TieuDietRac();

//                    string masterHash = ExtractHash(mainVer);
//                    string localHash = ExtractHash(AppDataVersionFile);

//                    bool needUpdate =
//                        masterHash != localHash ||
//                        !File.Exists(AppDataHtmlFile) ||
//                        GetFileHash(AppDataHtmlFile) != GetFileHash(mainHtml);

//                    if (!needUpdate) return;

//                    foreach (string fileName in AllowedFiles)
//                    {
//                        if (fileName.Equals(VersionFileName, StringComparison.OrdinalIgnoreCase) ||
//                            fileName.Equals("NhatKy_DonRac.txt", StringComparison.OrdinalIgnoreCase))
//                        {
//                            continue;
//                        }

//                        string sourcePath = Path.Combine(SrcMainDir, fileName);
//                        string destPath = Path.Combine(AppDataDir, fileName);

//                        if (File.Exists(sourcePath))
//                        {
//                            File.Copy(sourcePath, destPath, true);
//                        }
//                    }

//                    string copiedHash = GetFileHash(AppDataHtmlFile);
//                    SafeWrite(AppDataVersionFile, BuildManifest(copiedHash));
//                    Debug.WriteLine("[HUONGDAN] Updated OK: " + copiedHash);
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine("[HUONGDAN ERROR] " + ex.Message);
//                }
//            }
//        }

//        // =========================
//        // 7. EXTRACT HASH 
//        // =========================
//        private static string ExtractHash(string file)
//        {
//            try
//            {
//                if (!File.Exists(file)) return "0";

//                foreach (var line in File.ReadLines(file))
//                {
//                    if (line.StartsWith("SHA256:"))
//                        return line.Replace("SHA256:", "").Trim();
//                }
//                return "0";
//            }
//            catch { return "0"; }
//        }

//        public static void EnsureReady()
//        {
//            SyncMasterVersion();
//            UpdateLocal();
//        }

//        private static string GetFileHash(string path)
//        {
//            try
//            {
//                if (!File.Exists(path)) return "0";
//                using var sha = SHA256.Create();
//                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
//                return Convert.ToHexString(sha.ComputeHash(fs));
//            }
//            catch { return "0"; }
//        }

//        private static void EnsureFile(string src, string dst)
//        {
//            try
//            {
//                bool needCopy = !File.Exists(dst) || GetFileHash(src) != GetFileHash(dst);
//                if (!needCopy) return;

//                Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
//                File.Copy(src, dst, true);
//            }
//            catch { }
//        }

//        public static string LayCheDoXemHuongDan()
//        {
//            try
//            {
//                string dbPath = Module_DanduongGPS.DuongDanCSDL2;
//                using var cn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
//                cn.Open();

//                using var cmd = cn.CreateCommand();
//                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='CheDo_XemHuongDan'";
//                if (cmd.ExecuteScalar() == null) return "Chế độ web";

//                cmd.CommandText = "SELECT CheDoXem_HuongDanSD FROM CheDo_XemHuongDan WHERE ID = 1";
//                var result = cmd.ExecuteScalar();

//                return result?.ToString() ?? "Chế độ web";
//            }
//            catch { return "Chế độ web"; }
//        }

//        public static string TimFileHuongDanPdf()
//        {
//            try
//            {
//                string pdfPath = Path.Combine(AppContext.BaseDirectory, "Database", FolderName, "HuongDan.pdf");
//                if (!File.Exists(pdfPath)) return string.Empty;
//                return pdfPath;
//            }
//            catch { return string.Empty; }
//        }

//        private static readonly object _processLock = new();
//        private static Process? _huongDanWebProcess;

//        public static bool MoHuongDanBangWeb()
//        {
//            try
//            {
//                SyncMasterVersion();
//                UpdateLocal();

//                string targetWebPath = AppDataHtmlFile;
//                if (!File.Exists(targetWebPath)) return false;

//                lock (_processLock)
//                {
//                    if (_huongDanWebProcess != null)
//                    {
//                        try
//                        {
//                            if (!_huongDanWebProcess.HasExited)
//                            {
//                                if (_huongDanWebProcess.MainWindowHandle != IntPtr.Zero)
//                                {
//                                    SetForegroundWindow(_huongDanWebProcess.MainWindowHandle);
//                                    return true;
//                                }
//                            }
//                        }
//                        catch { }
//                    }

//                    _huongDanWebProcess = Process.Start(new ProcessStartInfo
//                    {
//                        FileName = targetWebPath,
//                        UseShellExecute = true,
//                        WindowStyle = ProcessWindowStyle.Normal
//                    });

//                    return _huongDanWebProcess != null;
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine("[HUONG DAN WEB ERROR] " + ex);
//                return false;
//            }
//        }

//        public static void DongHuongDan()
//        {
//            try
//            {
//                lock (_processLock)
//                {
//                    if (_huongDanWebProcess == null) return;
//                    try
//                    {
//                        if (!_huongDanWebProcess.HasExited)
//                        {
//                            _huongDanWebProcess.CloseMainWindow();
//                            if (!_huongDanWebProcess.WaitForExit(1000))
//                            {
//                                _huongDanWebProcess.Kill(true);
//                            }
//                        }
//                    }
//                    catch { }

//                    _huongDanWebProcess.Dispose();
//                    _huongDanWebProcess = null;
//                }
//            }
//            catch { }
//        }

//        [System.Runtime.InteropServices.DllImport("user32.dll")]
//        private static extern bool SetForegroundWindow(IntPtr hWnd);

//        public static bool TryActivateHuongDan()
//        {
//            try
//            {
//                lock (_processLock)
//                {
//                    if (_huongDanWebProcess == null) return false;
//                    if (_huongDanWebProcess.HasExited)
//                    {
//                        _huongDanWebProcess.Dispose();
//                        _huongDanWebProcess = null;
//                        return false;
//                    }
//                    if (_huongDanWebProcess.MainWindowHandle != IntPtr.Zero)
//                    {
//                        SetForegroundWindow(_huongDanWebProcess.MainWindowHandle);
//                        return true;
//                    }
//                    return false;
//                }
//            }
//            catch { return false; }
//        }
//    }
//}