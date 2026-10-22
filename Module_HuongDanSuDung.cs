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
        //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
        private const string TenThuMuc = "HuongDanSuDung";
        private const string TenFileHtml = "HuongDanSuDung.html";
        private const string TenFileVersion = "version.txt";
        private const string TenFileNhatKy = "NhatKy_DonRac.txt";
        /// <summary>
        /// true = khi đóng hướng dẫn mà cửa sổ không tự đóng thì Kill cả cây tiến trình (hành vi cũ).
        /// Mặc định false vì tiến trình đó thường chính là TRÌNH DUYỆT của người dùng: Kill sẽ tắt luôn mọi tab khác.
        /// </summary>
        private static readonly bool BuocDongTienTrinh = false;
        public static string FolderName => TenThuMuc;
        public static string FileName => TenFileHtml;
        public static string VersionFileName => TenFileVersion;
        // Đường dẫn cố định -> tính 1 lần (trước đây tính lại mỗi lần truy cập, kể cả gọi Windows lấy thư mục AppData)
        public static string SrcBackupDir { get; } = Path.Combine(AppContext.BaseDirectory, "Database Backup", TenThuMuc);
        public static string SrcMainDir { get; } = Path.Combine(AppContext.BaseDirectory, "Database", TenThuMuc);
        public static string AppDataDir { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhanMemThiDua2026", TenThuMuc);
        public static string AppDataHtmlFile { get; } = Path.Combine(AppDataDir, TenFileHtml);
        public static string AppDataVersionFile { get; } = Path.Combine(AppDataDir, TenFileVersion);
        private static readonly string[] CacThuMucDongBo = { SrcBackupDir, SrcMainDir };
        private static readonly object _lock = new();
        // Cache hash của file html chính theo (đường dẫn, kích thước, ngày sửa) -> không băm lại khi file không đổi
        private static (string Path, long Len, long Ticks, string Hash)? _hashDem;
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
        // ================= GHI / CHÉP FILE AN TOÀN =================
        // Ghi ra file tạm rồi đổi tên: mất điện/crash giữa chừng cũng không để lại file hỏng đè lên bản tốt.
        private static void SafeWrite(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string tmp = path + ".tmp";
            try
            {
                File.WriteAllText(tmp, content, Encoding.UTF8);
                File.Move(tmp, path, true);
            }
            catch
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                throw;
            }
        }
        private static void CopyAtomic(string src, string dst)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            string tmp = dst + ".tmp";
            try
            {
                File.Copy(src, tmp, true);
                File.Move(tmp, dst, true);
            }
            catch
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                throw;
            }
        }
        // File rỗng (0 byte) coi như hỏng/mất, để không bao giờ đè bản tốt bằng bản rỗng
        private static bool CoNoiDung(FileInfo fi) => fi.Exists && fi.Length > 0;
        // 4. CƠ CHẾ ĐỒNG BỘ 2 CHIỀU THÔNG MINH
        public static void SyncMasterVersion()
        {
            try
            {
                lock (_lock)   // tránh 2 luồng (nền + UI) cùng chép/ghi 1 file
                {
                    Directory.CreateDirectory(SrcMainDir);
                    Directory.CreateDirectory(SrcBackupDir);
                    string mainHtml = Path.Combine(SrcMainDir, TenFileHtml);
                    string backupHtml = Path.Combine(SrcBackupDir, TenFileHtml);
                    // KIỂM TRA TRƯỜNG HỢP MẤT SẠCH Ở DB & BACKUP
                    if (!CoNoiDung(new FileInfo(mainHtml)) && !CoNoiDung(new FileInfo(backupHtml)))
                    {
                        return; // 🟢 Đã vô hiệu hóa AppData, mất cả 2 nơi thì dừng lại
                    }
                    foreach (string fileName in AllowedFiles)
                    {
                        if (fileName.Equals(TenFileVersion, StringComparison.OrdinalIgnoreCase) ||
                            fileName.Equals(TenFileNhatKy, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        var main = new FileInfo(Path.Combine(SrcMainDir, fileName));
                        var backup = new FileInfo(Path.Combine(SrcBackupDir, fileName));
                        bool inMain = CoNoiDung(main);
                        bool inBackup = CoNoiDung(backup);
                        if (!inMain && !inBackup) continue;
                        if (inMain && !inBackup)
                        {
                            CopyAtomic(main.FullName, backup.FullName);
                            Debug.WriteLine($"[SYNC] Đã tự động chép {fileName} từ Database sang Backup.");
                        }
                        else if (!inMain && inBackup)
                        {
                            // 🟢 LOGIC GỐC CỦA BẠN: Nếu ở Main (Database) bị mất, sẽ tự động lấy từ Backup đắp qua
                            CopyAtomic(backup.FullName, main.FullName);
                            Debug.WriteLine($"[SYNC] Đã phục hồi {fileName} từ Backup về Database.");
                        }
                        else if (!GiongNhau(main, backup))
                        {
                            CopyAtomic(main.FullName, backup.FullName);
                            Debug.WriteLine($"[SYNC] Đã cập nhật bản mới {fileName} từ Database sang Backup.");
                        }
                    }
                    // Manifest chỉ ghi lại khi hash file chính thay đổi (hoặc version.txt mất/sai), không ghi lại mỗi lần mở
                    if (File.Exists(mainHtml))
                    {
                        string hash = LayHashCoCache(mainHtml);
                        if (hash == "0") return;
                        string? manifest = null;
                        foreach (string dir in CacThuMucDongBo)
                        {
                            string versionFile = Path.Combine(dir, TenFileVersion);
                            if (ExtractHash(versionFile) == hash) continue;
                            manifest ??= BuildManifest(hash);
                            SafeWrite(versionFile, manifest);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[SYNC MASTER ERROR] " + ex.Message);
            }
        }
        // Nhanh: khác kích thước -> khác; cùng kích thước + cùng ngày sửa -> giống (File.Copy giữ nguyên ngày sửa);
        // chỉ khi cùng kích thước nhưng khác ngày sửa mới băm thật.
        private static bool GiongNhau(FileInfo a, FileInfo b)
        {
            if (a.Length != b.Length) return false;
            if (a.LastWriteTimeUtc == b.LastWriteTimeUtc) return true;
            string ha = GetFileHash(a.FullName);
            if (ha == "0" || ha != GetFileHash(b.FullName)) return false;
            // Nội dung giống mà ngày sửa lệch: đồng bộ ngày sửa để lần sau khỏi băm lại
            try { File.SetLastWriteTimeUtc(b.FullName, a.LastWriteTimeUtc); } catch { }
            return true;
        }
        private static string LayHashCoCache(string path)
        {
            var fi = new FileInfo(path);
            if (_hashDem is { } c && c.Path == path && c.Len == fi.Length && c.Ticks == fi.LastWriteTimeUtc.Ticks)
                return c.Hash;
            string h = GetFileHash(path);
            if (h != "0") _hashDem = (path, fi.Length, fi.LastWriteTimeUtc.Ticks, h);
            return h;
        }
        // 6. CHECK + UPDATE APPDATA (giữ lại cho nơi khác còn gọi)
        public static void UpdateLocal()
        {
            // 🟢 Đã vô hiệu hóa AppData
        }
        // 7. EXTRACT HASH
        private static string ExtractHash(string file)
        {
            try
            {
                if (!File.Exists(file)) return "0";
                foreach (var line in File.ReadLines(file))
                {
                    if (line.StartsWith("SHA256:", StringComparison.Ordinal))
                        return line.Substring("SHA256:".Length).Trim();
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
        /// <summary>Đồng bộ ở luồng nền (gọi lúc khởi động để lần mở hướng dẫn đầu tiên không phải chờ).</summary>
        public static Task EnsureReadyAsync() => Task.Run(EnsureReady);
        private static string GetFileHash(string path)
        {
            try
            {
                if (!File.Exists(path)) return "0";
                using var sha = SHA256.Create();
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
                    bufferSize: 81920, FileOptions.SequentialScan);
                return Convert.ToHexString(sha.ComputeHash(fs));
            }
            catch { return "0"; }
        }
        public static string LayCheDoXemHuongDan()
        {
            const string macDinh = "Chế độ web";
            try
            {
                // ReadOnly: chỉ đọc, không bao giờ tự tạo file CSDL rỗng nếu đường dẫn sai
                var csb = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
                {
                    DataSource = Module_DanduongGPS.DuongDanCSDL2,
                    Mode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadOnly
                };
                using var cn = new Microsoft.Data.Sqlite.SqliteConnection(csb.ToString());
                cn.Open();
                using var cmd = cn.CreateCommand();
                // 1 truy vấn thay cho 2 (bảng chưa có -> ném lỗi -> trả mặc định bên dưới)
                cmd.CommandText = "SELECT CheDoXem_HuongDanSD FROM CheDo_XemHuongDan WHERE ID = 1";
                string? kq = cmd.ExecuteScalar()?.ToString();
                return string.IsNullOrWhiteSpace(kq) ? macDinh : kq;
            }
            catch { return macDinh; }
        }
        /// <summary>Bản chạy nền của LayCheDoXemHuongDan (không chặn giao diện).</summary>
        public static Task<string> LayCheDoXemHuongDanAsync() => Task.Run(LayCheDoXemHuongDan);
        public static string TimFileHuongDanPdf()
        {
            try
            {
                string pdfPath = Path.Combine(SrcMainDir, "HuongDan.pdf");
                return File.Exists(pdfPath) ? pdfPath : string.Empty;
            }
            catch { return string.Empty; }
        }
        // ================= MỞ / ĐÓNG HƯỚNG DẪN DẠNG WEB =================
        private static readonly object _processLock = new();
        private static Process? _huongDanWebProcess;
        public static bool MoHuongDanBangWeb()
        {
            try
            {
                // Tự động copy từ "Database Backup" về "Database" nếu tệp bị mất (nay chỉ tốn vài lần kiểm tra file)
                SyncMasterVersion();
                // 🟢 Mở trực tiếp từ thư mục Database nội bộ thay vì AppData
                string targetWebPath = Path.Combine(SrcMainDir, TenFileHtml);
                if (!File.Exists(targetWebPath)) return false;
                lock (_processLock)
                {
                    // Cửa sổ hướng dẫn đang mở -> đưa lên trước, không mở thêm tab
                    if (KichHoatCuaSoDangMo_NoLock()) return true;
                    // Giải phóng đối tượng Process cũ trước khi tạo mới (trước đây bị ghi đè -> rò handle)
                    _huongDanWebProcess?.Dispose();
                    _huongDanWebProcess = null;
                    // Process.Start có thể trả về null dù trình duyệt đã mở (nó dùng lại tiến trình sẵn có):
                    // không ném lỗi nghĩa là hệ thống đã nhận yêu cầu mở.
                    _huongDanWebProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = targetWebPath,
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Normal
                    });
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[HUONG DAN WEB ERROR] " + ex);
                return false;
            }
        }
        /// <summary>Bản chạy nền của MoHuongDanBangWeb (đồng bộ file + gọi shell không chặn giao diện).</summary>
        public static Task<bool> MoHuongDanBangWebAsync() => Task.Run(MoHuongDanBangWeb);
        public static void DongHuongDan()
        {
            try
            {
                lock (_processLock)
                {
                    var p = _huongDanWebProcess;
                    if (p == null) return;
                    _huongDanWebProcess = null;
                    try
                    {
                        if (!p.HasExited)
                        {
                            p.CloseMainWindow();
                            if (BuocDongTienTrinh && !p.WaitForExit(1000))
                            {
                                p.Kill(true);
                            }
                        }
                    }
                    catch { }
                    p.Dispose();
                }
            }
            catch { }
        }
        public static bool TryActivateHuongDan()
        {
            try
            {
                lock (_processLock)
                {
                    return KichHoatCuaSoDangMo_NoLock();
                }
            }
            catch { return false; }
        }
        // Gọi khi đã giữ _processLock
        private static bool KichHoatCuaSoDangMo_NoLock()
        {
            var p = _huongDanWebProcess;
            if (p == null) return false;
            try
            {
                if (p.HasExited)
                {
                    p.Dispose();
                    _huongDanWebProcess = null;
                    return false;
                }
                p.Refresh();   // MainWindowHandle bị cache, làm mới để lấy handle hiện tại
                IntPtr h = p.MainWindowHandle;
                if (h == IntPtr.Zero) return false;
                if (IsIconic(h)) ShowWindow(h, SW_RESTORE);   // đang thu nhỏ thì bung lên
                SetForegroundWindow(h);
                return true;
            }
            catch { return false; }
        }
        private const int SW_RESTORE = 9;
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr hWnd);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}