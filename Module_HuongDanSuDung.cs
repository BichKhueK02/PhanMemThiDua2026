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
        // Đường dẫn cố định -> tính 1 lần (trước đây tính lại mỗi lần truy cập, kể cả gọi Windows lấy thư mục AppData)
        public static string SrcBackupDir { get; } = Path.Combine(AppContext.BaseDirectory, "Database Backup", TenThuMuc);
        public static string SrcMainDir { get; } = Path.Combine(AppContext.BaseDirectory, "Database", TenThuMuc);
        // Đã bỏ hẳn AppDataDir/AppDataHtmlFile/AppDataVersionFile: hướng dẫn chỉ đọc từ Database\HuongDanSuDung (không chạm %LOCALAPPDATA%)
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
                BoChiDoc(path);   // tệp trong "Database Backup" bị bảo trì nền gắn ReadOnly: Move đè lên tệp ReadOnly sẽ lỗi "Access denied"
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
                BoChiDoc(tmp);
                File.Copy(src, tmp, true);   // giữ nguyên ngày sửa của nguồn -> lần sau so sánh "bản mới hơn" đúng
                BoChiDoc(tmp);               // tệp nguồn ReadOnly thì bản chép cũng ReadOnly
                BoChiDoc(dst);
                File.Move(tmp, dst, true);
            }
            catch
            {
                try { if (File.Exists(tmp)) { BoChiDoc(tmp); File.Delete(tmp); } } catch { }
                throw;
            }
        }
        private static void BoChiDoc(string path)
        {
            try
            {
                if (!File.Exists(path)) return;
                var a = File.GetAttributes(path);
                if ((a & FileAttributes.ReadOnly) != 0) File.SetAttributes(path, a & ~FileAttributes.ReadOnly);
            }
            catch { }
        }
        // Dung sai thời gian (FAT/exFAT/ổ mạng làm tròn 2 giây) — giống Module_KhoiTaoCSDL.CanSaoChep
        private static readonly TimeSpan DungSaiThoiGian = TimeSpan.FromSeconds(2);
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
                            // "Bản mới hơn thắng" — cùng quy tắc với đồng bộ nền (Module_KhoiTaoCSDL.SyncFolder).
                            // Trước đây luôn chép Database -> Backup: nếu bản mới được đặt vào "Database Backup"
                            // thì 2 cơ chế giành nhau, bản hướng dẫn cũ có thể đè mất bản mới.
                            if (backup.LastWriteTimeUtc - main.LastWriteTimeUtc > DungSaiThoiGian)
                            {
                                CopyAtomic(backup.FullName, main.FullName);
                                Debug.WriteLine($"[SYNC] Đã cập nhật bản mới {fileName} từ Backup về Database.");
                            }
                            else
                            {
                                CopyAtomic(main.FullName, backup.FullName);
                                Debug.WriteLine($"[SYNC] Đã cập nhật bản mới {fileName} từ Database sang Backup.");
                            }
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
                    DangKyDongKhiThoat();
                    // Ưu tiên mở bằng Edge/Chrome ở chế độ "--app": cửa sổ riêng chỉ 1 trang, dùng chung tiến trình
                    // trình duyệt đang chạy (không tốn thêm RAM), và khi thoát phần mềm có thể đóng cửa sổ này
                    // mà KHÔNG đụng tới các tab khác của người dùng.
                    string? trinhDuyet = TimTrinhDuyetHoTroApp();
                    if (trinhDuyet != null)
                    {
                        try
                        {
                            var psi = new ProcessStartInfo { FileName = trinhDuyet, UseShellExecute = false };
                            psi.ArgumentList.Add("--app=" + new Uri(targetWebPath).AbsoluteUri);
                            _huongDanWebProcess = Process.Start(psi);
                            _ = Task.Run(GhiNhanCuaSoAppVuaMo);   // ghi nhận handle cửa sổ để đóng khi thoát
                            return true;
                        }
                        catch (Exception ex) { Debug.WriteLine("[HUONG DAN APP MODE] " + ex.Message); }
                    }
                    // Dự phòng: mở bằng trình duyệt mặc định (tab thường -> khi thoát sẽ không tự đóng để tránh tắt nhầm tab khác)
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
        // Gọi khi đã giữ _processLock
        // Trình duyệt (Chrome/Edge/Firefox) đang chạy thì Process.Start trả về null hoặc tiến trình trung gian
        // tự thoát ngay -> không thể dựa vào _huongDanWebProcess. Vì vậy tìm cửa sổ trình duyệt theo TIÊU ĐỀ
        // trang hướng dẫn (thẻ <title>), thấy thì đưa lên trước thay vì mở thêm tab mới.
        private static bool KichHoatCuaSoDangMo_NoLock()
        {
            IntPtr h = TimCuaSoTheoTieuDe();
            if (h == IntPtr.Zero)
            {
                // Dự phòng: tiến trình do mình mở trực tiếp (trường hợp trình duyệt chưa chạy trước đó)
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
                    h = p.MainWindowHandle;
                }
                catch { return false; }
                if (h == IntPtr.Zero) return false;
            }
            try
            {
                if (IsIconic(h)) ShowWindow(h, SW_RESTORE);   // đang thu nhỏ thì bung lên
                SetForegroundWindow(h);
                return true;
            }
            catch { return false; }
        }
        // ---- Mở bằng cửa sổ app riêng + tự đóng khi thoát phần mềm ----
        private static IntPtr _hwndCuaSoApp;          // cửa sổ "--app" do phần mềm mở (chỉ chứa trang hướng dẫn)
        private static int _daDangKyThoat;
        private static string? TimTrinhDuyetHoTroApp()
        {
            // Edge có sẵn trên Win10/11; Chrome nếu có. Đọc đường dẫn từ "App Paths" của Windows.
            foreach (string exe in new[] { "msedge.exe", "chrome.exe" })
            {
                foreach (var goc in new[] { Microsoft.Win32.Registry.CurrentUser, Microsoft.Win32.Registry.LocalMachine })
                {
                    try
                    {
                        using var k = goc.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + exe);
                        if (k?.GetValue(null) is string p && File.Exists(p = p.Trim('"'))) return p;
                    }
                    catch { }
                }
            }
            return null;
        }
        private static void GhiNhanCuaSoAppVuaMo()
        {
            // Cửa sổ mới xuất hiện trong vòng vài giây; dò theo tiêu đề trang tối đa ~10 giây
            for (int i = 0; i < 50; i++)
            {
                IntPtr h = TimCuaSoTheoTieuDe();
                if (h != IntPtr.Zero) { _hwndCuaSoApp = h; return; }
                Thread.Sleep(200);
            }
        }
        private static void DangKyDongKhiThoat()
        {
            // ProcessExit chạy cả khi thoát bình thường lẫn Environment.Exit
            if (Interlocked.Exchange(ref _daDangKyThoat, 1) == 0)
                AppDomain.CurrentDomain.ProcessExit += (_, _) => DongCuaSoHuongDanKhiThoat();
        }
        /// <summary>
        /// Đóng cửa sổ hướng dẫn dạng web khi thoát phần mềm. Chỉ đóng cửa sổ "--app" do phần mềm tự mở
        /// (cửa sổ đó chỉ có trang hướng dẫn) nên không bao giờ tắt nhầm các tab khác của người dùng.
        /// </summary>
        public static void DongCuaSoHuongDanKhiThoat()
        {
            try
            {
                IntPtr h = _hwndCuaSoApp;
                _hwndCuaSoApp = IntPtr.Zero;
                if (h == IntPtr.Zero || !IsWindow(h)) return;
                // Kiểm tra lại tiêu đề: handle có thể đã bị Windows cấp cho cửa sổ khác sau khi người dùng tự đóng
                string tieuDe = LayTieuDeTrang();
                if (tieuDe.Length < 5) return;
                int len = GetWindowTextLength(h);
                var sb = new StringBuilder(len + 1);
                GetWindowText(h, sb, sb.Capacity);
                if (!sb.ToString().Normalize(NormalizationForm.FormC)
                        .StartsWith(tieuDe.Normalize(NormalizationForm.FormC), StringComparison.OrdinalIgnoreCase)) return;
                PostMessage(h, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex) { Debug.WriteLine("[DONG HUONG DAN KHI THOAT] " + ex.Message); }
        }
        private const uint WM_CLOSE = 0x0010;
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool IsWindow(IntPtr hWnd);
        // ---- Tìm cửa sổ trình duyệt đang hiển thị trang hướng dẫn theo tiêu đề ----
        private static (string Path, long Ticks, string Title)? _tieuDeDem;
        private static string LayTieuDeTrang()
        {
            try
            {
                string path = Path.Combine(SrcMainDir, TenFileHtml);
                var fi = new FileInfo(path);
                if (!fi.Exists) return string.Empty;
                if (_tieuDeDem is { } c && c.Path == path && c.Ticks == fi.LastWriteTimeUtc.Ticks) return c.Title;
                string title = string.Empty;
                // Thẻ <title> nằm ở đầu tệp -> chỉ đọc 64KB đầu, không nạp cả tệp
                using (var sr = new StreamReader(path, Encoding.UTF8, true))
                {
                    char[] buf = new char[65536];
                    int n = sr.ReadBlock(buf, 0, buf.Length);
                    var m = System.Text.RegularExpressions.Regex.Match(new string(buf, 0, n),
                        @"<title[^>]*>\s*(.*?)\s*</title>",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                    if (m.Success) title = System.Net.WebUtility.HtmlDecode(m.Groups[1].Value).Trim();
                }
                _tieuDeDem = (path, fi.LastWriteTimeUtc.Ticks, title);
                return title;
            }
            catch { return string.Empty; }
        }
        private static IntPtr TimCuaSoTheoTieuDe()
        {
            string tieuDe = LayTieuDeTrang();
            if (tieuDe.Length < 5) return IntPtr.Zero;   // tiêu đề quá ngắn/không có -> dễ nhận nhầm, bỏ qua
            string tieuDeChuan = tieuDe.Normalize(NormalizationForm.FormC);
            uint pidMinh = (uint)Environment.ProcessId;
            IntPtr ketQua = IntPtr.Zero;
            var sb = new StringBuilder(512);
            EnumWindows((hWnd, _) =>
            {
                if (!IsWindowVisible(hWnd)) return true;
                GetWindowThreadProcessId(hWnd, out uint pid);
                if (pid == pidMinh) return true;   // bỏ qua cửa sổ của chính phần mềm
                int len = GetWindowTextLength(hWnd);
                if (len < tieuDe.Length) return true;
                sb.Clear();
                sb.EnsureCapacity(len + 1);
                if (GetWindowText(hWnd, sb, sb.Capacity) <= 0) return true;
                string t = sb.ToString().Normalize(NormalizationForm.FormC);
                if (t.StartsWith(tieuDeChuan, StringComparison.OrdinalIgnoreCase))
                {
                    ketQua = hWnd;
                    return false;   // dừng duyệt
                }
                return true;
            }, IntPtr.Zero);
            return ketQua;
        }
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
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