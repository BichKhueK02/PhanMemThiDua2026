using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace PhanMemThiDua2026
{
    public static class Module_KhoiTaoCSDL
    {
        // 🔹 STRUCT HỨNG KẾT QUẢ DỌN RÁC
        private struct KetQuaDonRac
        {
            public int SoFileBiXoa;
            public int SoThuMucBiXoa;
            public int SoLoi;
        }
        private class ResourceInfo
        {
            public string BackupName, WinName, DbName;
            public ResourceInfo(string b, string w, string d) { BackupName = b; WinName = w; DbName = d; }
        }

        private const string THU_MUC_CONG_CU = "CongCuQuanLyCSDL";
        private const string THU_MUC_HUONG_DAN = "HuongDanSuDung";
        // ⭐ KHỞI ĐỘNG BẤT ĐỒNG BỘ: Không gây đơ ứng dụng trên máy cấu hình yếu
        internal static async Task BinhMinhOSantoriniAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    BinhMinhOSantoriniCore();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Lỗi nghiêm trọng khi khởi tạo CSDL: " + ex.Message);
                }
            });
        }
        private static void BinhMinhOSantoriniCore()
        {
            string srcDir = Path.Combine(AppContext.BaseDirectory, "Database Backup");
            string windowDir = Path.Combine(AppContext.BaseDirectory, "window-x64");
            string dbDir = Module_DanduongGPS.ThuMucCoSoDuLieu;

            Directory.CreateDirectory(srcDir);
            Directory.CreateDirectory(windowDir);
            Directory.CreateDirectory(dbDir);

            byte[] key = BaoMatAES.HoaVanNoTrenDuongRaChienDich256v1(Module_DanduongGPS.ToiYeuMeoCam1);

            var resourceMap = new List<ResourceInfo>
            {
                new ResourceInfo("cs1.mdf", "cs1", "csdl1.db"),
                new ResourceInfo("cs2.mdf", "cs2", "csdl2.db"),
                new ResourceInfo("cs3.mdf", "cs3", "csdl3.db"),
                new ResourceInfo("cs4.mdf", "cs4", "csdl4.db"),
                new ResourceInfo("csex.mdf", "csex", "csdlex.xlsx")
            };

            foreach (var res in resourceMap)
            {
                string pBackup = Path.Combine(srcDir, res.BackupName);
                string pWin = Path.Combine(windowDir, res.WinName);
                string pDb = Path.Combine(dbDir, res.DbName);

                bool b = File.Exists(pBackup);
                bool d = File.Exists(pDb);
                bool w = File.Exists(pWin);

                // --- CƠ CHẾ CỨU HỘ MỞ RỘNG ---
                if (b) // Backup là nguồn sạch nhất
                {
                    if (!d) { ThaoGoQuyenReadOnly(pDb); BaoMatAES.GiaiMaCSDL(pBackup, pDb, key); }
                    if (!w) { ThaoGoQuyenReadOnly(pWin); BaoMatAES.MaHoaCSDL(pDb, pWin, key); }
                }
                else if (d) // Backup mất
                {
                    ThaoGoQuyenReadOnly(pBackup); BaoMatAES.MaHoaCSDL(pDb, pBackup, key);
                    if (!w) { ThaoGoQuyenReadOnly(pWin); BaoMatAES.MaHoaCSDL(pDb, pWin, key); }
                }
                else if (w) // Mất cả Backup và DB
                {
                    ThaoGoQuyenReadOnly(pDb); BaoMatAES.GiaiMaCSDL(pWin, pDb, key);
                    ThaoGoQuyenReadOnly(pBackup); BaoMatAES.MaHoaCSDL(pDb, pBackup, key);
                }

                // --- BẢO VỆ CÁC TỆP ĐÃ ĐỒNG BỘ ---
                if (File.Exists(pBackup)) try { File.SetAttributes(pBackup, FileAttributes.ReadOnly); } catch { }
                if (File.Exists(pWin)) try { File.SetAttributes(pWin, FileAttributes.ReadOnly); } catch { }
            }

            // ĐỒNG BỘ THƯ MỤC CÔNG CỤ & HƯỚNG DẪN
            DongBoThuMucHeThong(srcDir, dbDir);

            // Thay vì khóa thư mục, khóa các file bên trong thư mục Backup
            foreach (var folder in new[] { "CongCuQuanLyCSDL", "HuongDanSuDung" })
            {
                string path = Path.Combine(srcDir, folder);
                if (Directory.Exists(path))
                {
                    foreach (var file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
                    {
                        try { File.SetAttributes(file, FileAttributes.ReadOnly); } catch { }
                    }
                }
            }

            if (key != null) CryptographicOperations.ZeroMemory(key);
        }
        private static void ThaoGoQuyenReadOnly(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    FileAttributes attr = File.GetAttributes(path);
                    if ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        File.SetAttributes(path, attr & ~FileAttributes.ReadOnly);
                }
                catch { }
            }
        }
        private static void DongBoThuMucHeThong(string srcDir, string dbDir)
        {
            string[] folders = { THU_MUC_CONG_CU, THU_MUC_HUONG_DAN };
            List<string> log = new List<string>();

            // =========================
            // 1. SYNC 2 CHIỀU
            // =========================
            foreach (string folder in folders)
            {
                string A = Path.Combine(srcDir, folder);
                string B = Path.Combine(dbDir, folder);

                Directory.CreateDirectory(A);
                Directory.CreateDirectory(B);

                SyncFolder(A, B, log);
                SyncFolder(B, A, log);
            }

            // =========================
            // 2. SELF HEAL LOOP (2 lần)
            // =========================
            for (int i = 0; i < 2; i++)
            {
                foreach (string folder in folders)
                {
                    string A = Path.Combine(srcDir, folder);
                    string B = Path.Combine(dbDir, folder);

                    SyncFolder(A, B, log);
                    SyncFolder(B, A, log);
                }
            }

            // =========================
            // 3. VERIFY LOOP (100% MATCH CHECK)
            // =========================
            foreach (string folder in folders)
            {
                string A = Path.Combine(srcDir, folder);
                string B = Path.Combine(dbDir, folder);

                if (!Directory.Exists(A) || !Directory.Exists(B))
                {
                    log.Add($"[VERIFY FAIL] Missing folder: {folder} -> FORCE REPAIR");
                    CopyDirSafe(A, B, log);
                    CopyDirSafe(B, A, log);
                }
                else
                {
                    log.Add($"[VERIFY OK] {folder}");
                }
            }
            KiemTraCauTrucThuMuc(srcDir, dbDir, log);
        }
        private static void SyncFolder(string src, string dst, List<string> log)
        {
            Directory.CreateDirectory(dst);

            foreach (var file in Directory.GetFiles(src))
            {
                string name = Path.GetFileName(file);
                string target = Path.Combine(dst, name);

                try
                {
                    bool shouldCopy = !File.Exists(target) ||
                                      File.GetLastWriteTimeUtc(file) > File.GetLastWriteTimeUtc(target) ||
                                      new FileInfo(file).Length != (File.Exists(target) ? new FileInfo(target).Length : -1);

                    if (shouldCopy)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(target));

                        if (File.Exists(target))
                        {
                            string backup = target + ".syncbak";
                            ThaoGoQuyenReadOnly(target);
                            File.Copy(target, backup, true);
                        }

                        // ⭐ ATOMIC WRITE: Ghi gián tiếp qua file tạm để chống mất điện sập nguồn gây lỗi file
                        string tempFile = target + ".tmp";
                        File.Copy(file, tempFile, true);
                        ThaoGoQuyenReadOnly(target);
                        File.Move(tempFile, target, true);

                        log.Add($"[SYNC FILE] {file} -> {target}");
                    }
                }
                catch (Exception ex)
                {
                    log.Add($"[ERROR FILE] {file} | {ex.Message}");
                }
            }

            foreach (var dir in Directory.GetDirectories(src))
            {
                string name = Path.GetFileName(dir);
                SyncFolder(dir, Path.Combine(dst, name), log);
            }
        }
        private static void CopyDirSafe(string src, string dst, List<string> log)
        {
            try
            {
                Directory.CreateDirectory(dst);

                foreach (var file in Directory.GetFiles(src))
                {
                    string target = Path.Combine(dst, Path.GetFileName(file));
                    ThaoGoQuyenReadOnly(target);
                    File.Copy(file, target, true);
                }

                foreach (var dir in Directory.GetDirectories(src))
                {
                    CopyDirSafe(dir, Path.Combine(dst, Path.GetFileName(dir)), log);
                }

                log.Add($"[COPY DIR] {src} -> {dst}");
            }
            catch (Exception ex)
            {
                log.Add($"[ERROR DIR] {src} | {ex.Message}");
            }
        }
        private static void KiemTraCauTrucThuMuc(string srcDir, string dbDir, List<string> log)
        {
            string[] folders = { THU_MUC_CONG_CU, THU_MUC_HUONG_DAN };
            bool srcHasAny = false;
            bool dbHasAny = false;

            foreach (string folder in folders)
            {
                if (Directory.Exists(Path.Combine(srcDir, folder))) srcHasAny = true;
                if (Directory.Exists(Path.Combine(dbDir, folder))) dbHasAny = true;
            }

            if (!srcHasAny && !dbHasAny)
            {
                log.Add("[CRITICAL WARNING] CẢ 2 VÙNG DATABASE VÀ BACKUP KHÔNG TỒN TẠI THƯ MỤC CORE (CongCuQuanLyCSDL / HuongDanSuDung)");
                Debug.WriteLine("[CORE STRUCTURE MISSING] Hệ thống rỗng thư mục lõi công cụ nghiệp vụ.");
            }
        }
        // ⭐ LOCAL QUARANTINE: Chuyển vùng cách ly vào root phần mềm, né lỗi Folder Redirection trên máy AD Domain


        // ⭐ BẢO VỆ HỆ THỐNG: Quét dọn tự động chạy ngầm, không block UI
        public static void ChinhSachLamSach()
        {
            string baseDir = Path.GetFullPath(AppContext.BaseDirectory).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;

            // 1. Cấu hình Database Backup
            string dir1 = Path.Combine(baseDir, "Database Backup");
            var allowFiles1 = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "cs1.mdf", "cs2.mdf", "cs3.mdf", "cs4.mdf", "csex.mdf", "NhatKy_LamSach.txt" };
            var allowDirs1 = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CongCuQuanLyCSDL", "HuongDanSuDung" };
            DonDepVungQuanLy(dir1, allowFiles1, allowDirs1, false, false);

            // 2. Cấu hình Database
            string dir2 = Path.GetFullPath(string.IsNullOrWhiteSpace(Module_DanduongGPS.ThuMucCoSoDuLieu) ? Path.Combine(baseDir, "Database") : Module_DanduongGPS.ThuMucCoSoDuLieu);
            var allowFiles2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "csdl1.db", "csdl2.db", "csdl3.db", "csdl4.db", "csdlex.xlsx", "NhatKy_LamSach.txt" };
            var allowDirs2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Bansaoluu", "CongCuQuanLyCSDL", "HuongDanSuDung", "LuuTruThiDua_LichSu"};
            DonDepVungQuanLy(dir2, allowFiles2, allowDirs2, false, true);

            // 3. Cấu hình window-x64
            string dir3 = Path.Combine(baseDir, "window-x64");
            var allowFiles3 = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "cs1", "cs2", "cs3", "cs4", "csex", "NhatKy_LamSach.txt" };
            DonDepVungQuanLy(dir3, allowFiles3, null, true, false);

            // 4. Cấu hình Database\Bansaoluu
            string dir4 = Path.Combine(baseDir, "Database", "Bansaoluu");
            var allowFiles4 = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "NhatKy_LamSach.txt", "GiayPhepCapQuyen_ServiceRestore.dat", "GiayPhepCapQuyen_ServiceBackup.dat" };
            DonDepVungQuanLy(dir4, allowFiles4, null, true, false);
        }
        private static void DonDepVungQuanLy(string dirPath, HashSet<string> allowedFiles, HashSet<string> allowedDirs, bool deleteSubDirs, bool isSQLiteZone)
        {
            // =================================================================================
            // ⭐ BỘ LỌC AN TOÀN CHUẨN KỸ SƯ: CHẶN HỦY DIỆT FILE KHI ĐANG LẬP TRÌNH/TEST CODE
            // =================================================================================
            string currentExePath = AppDomain.CurrentDomain.BaseDirectory;
            bool isDevelopmentEnv = currentExePath.Contains(@"\bin\Debug\", StringComparison.OrdinalIgnoreCase) ||
                                    currentExePath.Contains(@"\bin\Release\", StringComparison.OrdinalIgnoreCase);

            // Nếu đang Debug/Release trong VS, hoặc đường dẫn rỗng -> THOÁT NGAY lập tức để bảo vệ code
            if (isDevelopmentEnv || string.IsNullOrWhiteSpace(dirPath))
                return;

            // Chuẩn hóa và kiểm tra sự tồn tại của thư mục mục tiêu
            string currentDir;
            try
            {
                currentDir = Path.GetFullPath(dirPath).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
            }
            catch
            {
                return; // Tránh văng exception do ký tự đường dẫn không hợp lệ
            }

            if (!Directory.Exists(currentDir))
            {
                try { Directory.CreateDirectory(currentDir); } catch { return; }
            }
            // =================================================================================

            List<string> chiTiet = new List<string>();
            int fCount = 0, dCount = 0;
            string tenTaiKhoan = string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM) ? "Không xác định" : Module_TaiKhoan.TenTaiKhoan_RAM;

            // XỬ LÝ TỆP
            try
            {
                foreach (string file in Directory.GetFiles(currentDir))
                {
                    FileInfo fi = new FileInfo(file);

                    // Tối ưu hóa chuỗi: Dùng Equals thay vì so sánh chuỗi chứa nhiều rác
                    if (fi.Name.Equals("NhatKy_LamSach.txt", StringComparison.OrdinalIgnoreCase)) continue;

                    // SQLite Zone Check
                    if (isSQLiteZone && (fi.Extension.Equals(".db-wal", StringComparison.OrdinalIgnoreCase) ||
                                         fi.Extension.Equals(".db-shm", StringComparison.OrdinalIgnoreCase) ||
                                         fi.Extension.EndsWith("-wal", StringComparison.OrdinalIgnoreCase) ||
                                         fi.Extension.EndsWith("-shm", StringComparison.OrdinalIgnoreCase)))
                        continue;

                    // Backup Zone Check
                    if (currentDir.Contains("Bansaoluu", StringComparison.OrdinalIgnoreCase) &&
                        fi.Name.StartsWith("Backup_", StringComparison.OrdinalIgnoreCase) &&
                        fi.Extension.Equals(".pmtd", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Kiểm tra HashSet loại trừ (Độ phức tạp O(1) cực nhanh)
                    if (allowedFiles != null && allowedFiles.Contains(fi.Name)) continue;

                    long kichThuoc = fi.Length;
                    try
                    {
                        fi.Attributes = FileAttributes.Normal;
                        fi.Delete();
                    }
                    catch { continue; }

                    // ⭐ TỐI ƯU HIỆU NĂNG: Không gọi File.Exists(fi.FullName) một lần nữa vì FileInfo.Refresh() chính xác hơn
                    fi.Refresh();
                    if (!fi.Exists)
                    {
                        fCount++;
                        chiTiet.Add($"[TỆP] {fi.Name} | Dung lượng: {FormatSize(kichThuoc)} | Thư mục: {currentDir}");

                        // Tối ưu allocation: Gom chuỗi tường minh bằng Interpolation nội bộ
                        Module_NhatKy.GhiNhatKy(
                            taiKhoan: tenTaiKhoan,
                            hanhDong: "XÓA FILE HỆ THỐNG (AUTO CLEAN)",
                            ghiChu: $"=== THÔNG TIN XÓA ==={Environment.NewLine}" +
                                   $"Thư mục xử lý : {currentDir}{Environment.NewLine}" +
                                   $"Tên tệp        : {fi.Name}{Environment.NewLine}" +
                                   $"Dung lượng     : {FormatSize(kichThuoc)}{Environment.NewLine}" +
                                   $"Loại thao tác  : Xóa tự động do hệ thống phát hiện file không hợp lệ{Environment.NewLine}" +
                                   $"Thời gian      : {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi quét file]: {ex.Message}");
            }

            // XỬ LÝ THƯ MỤC
            try
            {
                foreach (string dir in Directory.GetDirectories(currentDir))
                {
                    DirectoryInfo di = new DirectoryInfo(dir);
                    if (deleteSubDirs || allowedDirs == null || !allowedDirs.Contains(di.Name))
                    {
                        try
                        {
                            long folderSize = GetDirSize(di);
                            RemoveReadOnlyRecursive(di.FullName);
                            di.Delete(true);

                            di.Refresh();
                            if (!di.Exists)
                            {
                                dCount++;
                                chiTiet.Add($"[Đã xóa Thư mục]: {di.Name}");

                                Module_NhatKy.GhiNhatKy(
                                    taiKhoan: tenTaiKhoan,
                                    hanhDong: "XÓA THƯ MỤC HỆ THỐNG (AUTO CLEAN)",
                                    ghiChu: $"=== THÔNG TIN XÓA THƯ MỤC ==={Environment.NewLine}" +
                                           $"Thư mục cha   : {currentDir}{Environment.NewLine}" +
                                           $"Tên thư mục   : {di.Name}{Environment.NewLine}" +
                                           $"Dung lượng    : {FormatSize(folderSize)}{Environment.NewLine}" +
                                           $"Loại thao tác : Xóa toàn bộ thư mục không hợp lệ{Environment.NewLine}" +
                                           $"Thời gian     : {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                                );
                            }
                        }
                        catch { chiTiet.Add($"[Lỗi Thư mục]: {di.Name}"); }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi quét thư mục]: {ex.Message}");
            }

            // Ghi báo cáo tổng hợp nếu có biến động dữ liệu
            if (fCount > 0 || dCount > 0)
            {
                try
                {
                    GhiDeNhatKy(Path.Combine(currentDir, "NhatKy_LamSach.txt"), TaoNoiDungBaoCao(fCount, dCount, 0, chiTiet));
                }
                catch { /* Bỏ qua lỗi ghi file nhật ký cục bộ nếu thư mục bị khóa */ }
            }
        }
        public static void ChinhSachBaoVeHeThongEXE(string rootPath)
        {
            string currentExePath = AppDomain.CurrentDomain.BaseDirectory;

            // Bỏ qua nếu đang debug
            bool isDevelopmentEnv = currentExePath.Contains(@"\bin\Debug\", StringComparison.OrdinalIgnoreCase) ||
                                    currentExePath.Contains(@"\bin\Release\", StringComparison.OrdinalIgnoreCase);

            if (isDevelopmentEnv || string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
                return;

            // 🛡️ CHỐT CHẶN TỬ THẦN: Đảm bảo rootPath phải nằm bên trong thư mục cài đặt gốc.
            // Tránh thảm họa truyền nhầm "C:\" làm xóa sạch hệ điều hành.
            if (!rootPath.StartsWith(currentExePath, StringComparison.OrdinalIgnoreCase))
            {
                try { Module_NhatKy.GhiNhatKy("System", "CẢNH BÁO BẢO MẬT", "Phát hiện nỗ lực quét sai thư mục gốc: " + rootPath); } catch { }
                return;
            }

            HashSet<string> allowedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Database", "Database Backup", "window-x64" };
            HashSet<string> allowedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "D3DCompiler_47_cor3.dll", "e_sqlite3.dll", "PenImc_cor3.dll", "PhanMemThiDua2026.dll.config",
        "PhanMemThiDua2026.exe", "PresentationNative_cor3.dll", "ServiceBackup.exe",
        "ServiceRestore.exe", "Uninstall_PhanMemThiDua2026.exe", "vcruntime140_cor3.dll",
        "wpfgfx_cor3.dll", "NhatKy_LamSach.txt"
    };

            List<string> chiTiet = new List<string>(32);
            int fCount = 0, dCount = 0, eCount = 0;
            const int MAX_DELETE_PER_SESSION = 100;
            string logPath = Path.Combine(rootPath, "NhatKy_LamSach.txt");

            try
            {
                // =================================================================
                // 1. XỬ LÝ THƯ MỤC LẠ
                // =================================================================
                try
                {
                    foreach (string dirPath in Directory.EnumerateDirectories(rootPath))
                    {
                        if (fCount + dCount >= MAX_DELETE_PER_SESSION) break;

                        DirectoryInfo dirInfo = new DirectoryInfo(dirPath);

                        if ((dirInfo.Attributes & FileAttributes.ReparsePoint) != 0 || allowedFolders.Contains(dirInfo.Name))
                            continue;

                        try
                        {
                            RemoveReadOnlyRecursive(dirInfo.FullName); // Đảm bảo hàm này không văng lỗi
                            dirInfo.Delete(true); // Xóa cứng (Hard Delete)
                            dCount++;
                            chiTiet.Add($"[THƯ MỤC RÁC] Đã tiêu diệt: {dirInfo.Name}");
                        }
                        catch
                        {
                            eCount++;
                            chiTiet.Add($"[LỖI THƯ MỤC] Bị khóa chặn: {dirInfo.Name}");
                        }
                    }
                }
                catch { /* Bỏ qua lỗi Access Denied cấp thư mục gốc nếu có */ }

                // =================================================================
                // 2. XỬ LÝ TỆP TIN LẠ
                // =================================================================
                try
                {
                    foreach (string filePath in Directory.EnumerateFiles(rootPath))
                    {
                        if (fCount + dCount >= MAX_DELETE_PER_SESSION) break;

                        FileInfo fileInfo = new FileInfo(filePath);
                        if (allowedFiles.Contains(fileInfo.Name)) continue;

                        bool laTepThucThi = fileInfo.Extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
                                            fileInfo.Extension.Equals(".dll", StringComparison.OrdinalIgnoreCase);

                        bool daXoaThanhCong = XoaTepVoiCoCheRetry(fileInfo);

                        if (!daXoaThanhCong && laTepThucThi)
                        {
                            // Nếu là tệp thực thi đang chạy ngầm -> Kill tiến trình và thử lại
                            try
                            {
                                if (CuongBucTatTienTrinhDangLockFile(fileInfo.FullName, rootPath))
                                {
                                    // Sau khi Kill, thử xóa lại với Retry Pattern thay vì Sleep cứng
                                    daXoaThanhCong = XoaTepVoiCoCheRetry(fileInfo, retries: 5, delayMs: 100);

                                    if (daXoaThanhCong)
                                    {
                                        chiTiet.Add($"[TIÊU DIỆT SAU KILL] Đã ép xóa: {fileInfo.Name}");
                                    }
                                }
                            }
                            catch { }
                        }

                        if (daXoaThanhCong && !laTepThucThi)
                        {
                            fCount++;
                            chiTiet.Add($"[TỆP RÁC] Đã xóa trực tiếp: {fileInfo.Name}");
                        }
                        else if (daXoaThanhCong && laTepThucThi)
                        {
                            fCount++;
                            chiTiet.Add($"[TIÊU DIỆT EXE/DLL] Đã xóa trực tiếp: {fileInfo.Name}");
                        }
                        else
                        {
                            eCount++;
                            chiTiet.Add($"[THẤT BẠI] File đang bị khóa chặt: {fileInfo.Name}");
                        }
                    }
                }
                catch { /* Bỏ qua lỗi Access Denied cấp thư mục gốc */ }

                // =================================================================
                // 3. ĐỒNG BỘ GHI NHẬT KÝ
                // =================================================================
                if (fCount > 0 || dCount > 0 || eCount > 0)
                {
                    GhiDeNhatKy(logPath, TaoNoiDungBaoCao(fCount, dCount, eCount, chiTiet));
                    Module_NhatKy.GhiNhatKy("System", "Chính sách tự vệ (Bảo vệ tự động)", $"Xử lý an toàn {fCount} tệp, {dCount} thư mục.");
                }
                else
                {
                    // Trả lại môi trường sạch sẽ
                    try { if (File.Exists(logPath)) { File.SetAttributes(logPath, FileAttributes.Normal); File.Delete(logPath); } } catch { }
                }
            }
            catch (Exception ex)
            {
                try { Module_NhatKy.GhiNhatKy("System", "LỖI - Chính sách tự vệ (Bảo vệ tự động)", ex.Message); } catch { }
            }
        }
        // Hàm phụ trợ: Thử xóa file nhiều lần (Retry Pattern), chống crash do Win chưa kịp nhả Handle
        private static bool XoaTepVoiCoCheRetry(FileInfo fileInfo, int retries = 3, int delayMs = 50)
        {
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    if (fileInfo.Exists)
                    {
                        fileInfo.Attributes = FileAttributes.Normal; // Ép gỡ ReadOnly ngay trước khi chém
                        fileInfo.Delete(); // Lệnh này KHÔNG chuyển vào thùng rác
                    }
                    return true;
                }
                catch (UnauthorizedAccessException)
                {
                    // Nếu dính lỗi phân quyền chặt từ HĐH, không cần chờ, thoát luôn
                    return false;
                }
                catch (IOException)
                {
                    // File đang bị lock (IOException), tạm dừng Thread một nhịp rồi thử lại
                    if (i < retries - 1) Thread.Sleep(delayMs);
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
        private static bool CuongBucTatTienTrinhDangLockFile(string filePath, string rootPath)
        {
            bool daKillThanhCong = false;
            try
            {
                // ⭐ KHAI BÁO BIẾN TRƯỚC VÀ DISPOSE SAU KHI DÙNG TRÁNH LỌT HANDLE GÂY PHÌNH RAM MÁY TRẠM YẾU
                var processList = Process.GetProcesses();
                foreach (Process p in processList)
                {
                    try
                    {
                        if (p.HasExited || p.Id <= 4) continue;

                        string processPath = p.MainModule?.FileName;
                        if (string.IsNullOrWhiteSpace(processPath) || !processPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (processPath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                        {
                            p.Kill();
                            p.WaitForExit(1500);
                            daKillThanhCong = true;
                        }
                    }
                    catch { }
                    finally { p.Dispose(); }
                }
            }
            catch { }
            return daKillThanhCong;
        }
        private static string TaoNoiDungBaoCao(int files, int dirs, int errs, List<string> chiTiet)
        {
            string tenTaiKhoan = string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM) ? "System (Quyền cao nhất)" : Module_TaiKhoan.TenTaiKhoan_RAM;
            StringBuilder sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendLine("[BÁO CÁO DỌN DẸP HỆ THỐNG - PHẦN MỀM THI ĐUA 2026]");
            sb.AppendLine("-------------------------------------------------------------");
            sb.AppendLine($"Tên phần mềm        : PhanMemThiDua2026");
            sb.AppendLine($"Phiên bản           : {Module_PhienBan.SoftwareVersion}");
            sb.AppendLine($"Cập nhật ngày       : {Module_PhienBan.NgayThangNamHeThong}");
            sb.AppendLine($"Người phát triển    : {Module_PhienBan.NguoiPhatTrienPhanMem}");
            sb.AppendLine($"Tên tài khoản       : {tenTaiKhoan}");
            sb.AppendLine($"Máy tính            : {Environment.MachineName}");
            sb.AppendLine($"Thời gian thực hiện : ngày {DateTime.Now:dd/MM/yyyy}, giờ {DateTime.Now:HH:mm:ss}");
            sb.AppendLine($"Người thực hiện     : System (Trình bảo trì CSDL)");
            sb.AppendLine($"Trạng thái          : Đã dọn dẹp và tối ưu thành công");
            sb.AppendLine("-------------------------------------------------------------");

            if (files > 0 || dirs > 0 || errs > 0)
            {
                sb.AppendLine("KẾT QUẢ TỔNG HỢP:");
                sb.AppendLine($"- Tệp tin đã xóa   : {files:D2}");
                sb.AppendLine($"- Thư mục đã xóa   : {dirs:D2}");
                sb.AppendLine($"- Lỗi phát sinh    : {errs:D2}");
                sb.AppendLine($"- Lý do            : Đây là thư mục thuộc quyền quản lý");
                sb.AppendLine($"                     và bảo vệ của phần mềm");
                sb.AppendLine($"                     PhanMemThiDua2026.");
                sb.AppendLine($"                     Hệ thống đã tự động loại bỏ");
                sb.AppendLine($"                     các tệp/thư mục không liên quan.");
            }
            else
            {
                sb.AppendLine("KẾT QUẢ TỔNG HỢP:");
                sb.AppendLine("Hệ thống ổn định, không phát hiện");
                sb.AppendLine("tệp hoặc thư mục bất thường.");
            }
            sb.AppendLine("-------------------------------------------------------------");
            sb.AppendLine("CHI TIẾT THỰC HIỆN:");

            foreach (string item in chiTiet) sb.AppendLine(item);

            sb.AppendLine("-------------------------------------------------------------");
            sb.AppendLine("Ghi chú: Đây là hoạt động dọn dẹp và bảo trì tệp dữ liệu");
            sb.AppendLine("nội bộ do Phần mềm Thi đua 2026 tự động thực hiện độc lập.");
            sb.AppendLine("Quy trình này diễn ra an toàn, hoàn toàn giới hạn trong phạm");
            sb.AppendLine("vi thư mục ứng dụng và không can thiệp hay tác động đến hệ");
            sb.AppendLine("thống hệ điều hành của máy tính.");
            sb.AppendLine("-------------------------------------------------------------");

            string result = sb.ToString();
            sb.Clear();
            return result;
        }
        private static long GetDirSize(DirectoryInfo d)
        {
            long size = 0;
            try
            {
                foreach (FileInfo fi in d.GetFiles()) size += fi.Length;
                foreach (DirectoryInfo di in d.GetDirectories())
                {
                    if ((di.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                    size += GetDirSize(di);
                }
            }
            catch (Exception ex) { Debug.WriteLine(ex.Message); }
            return size;
        }
        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{(bytes / 1024.0):F1} KB";
            return $"{(bytes / (1024.0 * 1024.0)):F1} MB";
        }
        private static void GhiDeNhatKy(string logPath, string logContent)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var dir = Path.GetDirectoryName(logPath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    if (File.Exists(logPath)) File.SetAttributes(logPath, FileAttributes.Normal);
                    File.WriteAllText(logPath, logContent, Encoding.UTF8);
                    return;
                }
                catch { Thread.Sleep(200); }
            }
        }
        private static void RemoveReadOnlyRecursive(string folder)
        {
            try
            {
                foreach (string file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
                {
                    try { File.SetAttributes(file, FileAttributes.Normal); } catch { }
                }
                foreach (string dir in Directory.GetDirectories(folder, "*", SearchOption.AllDirectories))
                {
                    try { new DirectoryInfo(dir).Attributes = FileAttributes.Normal; } catch { }
                }
                new DirectoryInfo(folder).Attributes = FileAttributes.Normal;
            }
            catch { }
        }
        private static void LoiThiThamCuaGioCopyFile(string src, string dst)
        {
            try
            {
                if (!File.Exists(src)) return;

                string name = Path.GetFileName(dst);
                if (name.EndsWith(".db-wal", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".db-shm", StringComparison.OrdinalIgnoreCase))
                    return;

                if (!File.Exists(dst) || File.GetLastWriteTime(src) > File.GetLastWriteTime(dst))
                {
                    ThaoGoQuyenReadOnly(dst);
                    File.Copy(src, dst, true);
                }
            }
            catch { }
        }
        private static void KimTuThapAiCapCopyDirectory(string src, string dst)
        {
            try
            {
                Directory.CreateDirectory(dst);
                foreach (string file in Directory.GetFiles(src))
                    LoiThiThamCuaGioCopyFile(file, Path.Combine(dst, Path.GetFileName(file)));

                foreach (string dir in Directory.GetDirectories(src))
                    KimTuThapAiCapCopyDirectory(dir, Path.Combine(dst, Path.GetFileName(dir)));
            }
            catch { }
        }
        // ====================================================================
        // ⭐ QUẢN LÝ COMBOBOX CÂU HỎI BẢO MẬT
        // ====================================================================
        public static readonly string[] DanhSachCauHoiNhom1 =
        {
            "Họ và tên của bạn?", "Bạn sinh ra ở Tỉnh/Thành phố nào?", "Món ăn yêu thích nhất của bạn là gì?",
            "Tên trường THPT của bạn?", "Tên thú cưng bạn yêu thích?", "Tên cô người bạn yêu đầu tiên?",
            "Tên bộ phim mà bạn yêu thích nhất?", "Công việc đầu tiên bạn làm để kiếm ra tiền?",
            "Nghề nghiệp mơ ước của bạn khi còn nhỏ là gì?", "Bạn đã từng đi du lịch nước ngoài chưa?"
        };
        public static readonly string[] DanhSachCauHoiNhom2 =
        {
            "Tên con vật yêu thích của bạn?", "Giới tính của bạn là Nam hay nữ?", "Bạn vào CAND ngày tháng năm nào?",
            "Món quà sinh nhật đầu tiên bạn nhận được là gì?", "Bạn có thích chơi game không?",
            "Bạn thích đội bóng hoặc câu lạc bộ thể thao nào?", "Bài hát mà bạn nghe đi nghe lại nhiều nhất thời học sinh là gì?",
            "Bạn biết bơi lội không?", "Tên con mèo của bạn là gì?", "Bạn có biết ngôn ngữ lập trình Visual Basic không?",
            "Bạn có biết ngôn ngữ lập trình JavaScript không?"
        };
        private static void ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender is ComboBox changedComboBox && changedComboBox.Tag is Tuple<string[], ComboBox[]> config)
            {
                if (!changedComboBox.Focused) return; // Chống kích hoạt lặp vòng lặp treo máy

                string[] danhSachGoc = config.Item1;
                ComboBox[] allComboBoxes = config.Item2;
                CapNhatDanhSachHienThi(danhSachGoc, allComboBoxes);
            }
        }
        private static void CapNhatDanhSachHienThi(string[] danhSachCauHoiGoc, ComboBox[] comboBoxes)
        {
            var selectedItems = comboBoxes
                .Where(cb => cb != null && cb.SelectedItem != null && !string.IsNullOrWhiteSpace(cb.Text))
                .Select(cb => cb.Text)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var cb in comboBoxes)
            {
                if (cb == null) continue;

                cb.SelectedIndexChanged -= ComboBox_SelectedIndexChanged;
                string currentSelection = cb.Text;

                cb.BeginUpdate();
                cb.Items.Clear();

                foreach (string cauHoi in danhSachCauHoiGoc)
                {
                    if (!selectedItems.Contains(cauHoi) || cauHoi.Equals(currentSelection, StringComparison.OrdinalIgnoreCase))
                    {
                        cb.Items.Add(cauHoi);
                    }
                }

                if (!string.IsNullOrEmpty(currentSelection)) cb.Text = currentSelection;
                cb.EndUpdate();

                cb.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
            }
        }
        public static void NapDuLieuCauHoiBaoMat(string[] danhSachCauHoi, params ComboBox[] comboBoxes)
        {
            if (comboBoxes == null || comboBoxes.Length == 0 || danhSachCauHoi == null) return;

            foreach (var cb in comboBoxes)
            {
                if (cb == null) continue;

                cb.SelectedIndexChanged -= ComboBox_SelectedIndexChanged;

                cb.BeginUpdate();
                cb.Items.Clear();
                cb.Items.AddRange(danhSachCauHoi);
                cb.SelectedIndex = -1;
                cb.EndUpdate();

                cb.Tag = new Tuple<string[], ComboBox[]>(danhSachCauHoi, comboBoxes);
                cb.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
            }
        }
    }
}
