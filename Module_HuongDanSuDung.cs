using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
namespace PhanMemThiDua2026
{
    public static class Module_HuongDanSuDung
    {
        // 1. CƠ CHẾ DANH SÁCH CHO PHÉP (WHITELIST) - MỞ RỘNG DỄ DÀNG Ở ĐÂY
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
        // BẮT ĐẦU ĐOẠN CODE GIỮ NGUYÊN 100%
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
        // 3. CƠ CHẾ PHỤC HỒI NGƯỢC (REVERSE RECOVERY) - XỬ LÝ TRƯỜNG HỢP HY HỮU
        private static bool TryDeepRecoverFromAppData()
        {
            return false; // 🟢 Đã vô hiệu hóa để tránh IT quét: Không dùng AppData nữa
        }
        private static void ThucHienPhucHoiNguoc()
        {
            return; // 🟢 Đã vô hiệu hóa để tránh IT quét: Không dùng AppData nữa
        }
        // 4. CƠ CHẾ ĐỒNG BỘ 2 CHIỀU THÔNG MINH
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
        // 5. CƠ CHẾ TIÊU DIỆT RÁC & BẢO VỆ PHẦN MỀM 
       private static void TieuDietRac()
        {
            return; // 🟢 Đã vô hiệu hóa thao tác vào AppData để tránh IT quét
        }
        // 6. CHECK + UPDATE APPDATA 
        public static void UpdateLocal()
        {
            return; // 🟢 Đã vô hiệu hóa AppData
        }
        // 7. EXTRACT HASH 
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