//TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
using System.Collections;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
namespace PhanMemThiDua2026
{
    public static class Module_KhoiTaoCSDL
    {
        // ===================== HẰNG SỐ / CẤU HÌNH =====================
        private const string THU_MUC_CONG_CU = "CongCuQuanLyCSDL";
        private const string THU_MUC_HUONG_DAN = "HuongDanSuDung";
        private const string THU_MUC_REPOSITORY = "CoreDatabaseRepository";
        private const string THU_MUC_BACKUP = "Database Backup";
        private const string THU_MUC_WINDOW = "window-x64";
        private const string THU_MUC_DATABASE = "Database";
        private const string THU_MUC_BAN_SAO_LUU = "Bansaoluu";
        private const string THU_MUC_LUUTRUTHIDUA_LICHSU = "LuuTruThiDua_LichSu"; 
       // private const string THU_MUC_THONGTUHUONGDAN_MAHOA = "ThongTuHuongDan_MaHoa";       
        private const string TEN_NHAT_KY_LAM_SACH = "NhatKy_LamSach.txt";
        private const string TEN_TAI_KHOAN_HE_THONG = "System (Quyền cao nhất)";
        private const string HAU_TO_TAM = ".tmp";
        private const string HAU_TO_SYNCBAK = ".syncbak";   // chỉ còn để nhận diện rác của phiên bản cũ
        private const int SO_LUOT_DONG_BO_TOI_DA = 3;
        private const int MAX_LOI_GHI_NHAT_KY = 50;          // chặn nhật ký phình to khi lỗi hàng loạt
        // true : thư mục CoreDatabaseRepository chỉ do TuanTraVaPhucHoiCoreRepository quản lý (chính sách "không tự ý ghi đè khi lệch SHA-256"),
        //        không để đồng bộ "bản mới hơn thắng" ghi đè lên.
        private const bool DONG_BO_BO_QUA_CORE_REPOSITORY = true;
        // Dung sai thời gian khi so sánh LastWriteTime (FAT/exFAT/chia sẻ mạng có độ phân giải 2 giây)
        private static readonly TimeSpan DungSaiThoiGian = TimeSpan.FromSeconds(2);
        // Tuần tự hóa MỌI thao tác bảo trì ghi/xóa tệp (khởi tạo, tuần tra, dọn rác, tường lửa) để không giẫm chân nhau:
        // trước đây dọn rác có thể xóa tệp .tmp đang ghi dở của khởi tạo, hoặc 2 luồng ghi cùng 1 tệp.
        private static readonly object _khoaBaoTri = new();
        private static readonly string[] ThuMucDongBo = { THU_MUC_CONG_CU, THU_MUC_HUONG_DAN };
        private readonly record struct TaiNguyenCSDL(string Backup, string Win, string Db);
        private static readonly TaiNguyenCSDL[] DanhSachTaiNguyen =
        {
            new("cs1.mdf", "cs1", "csdl1.db"),
            new("cs2.mdf", "cs2", "csdl2.db"),
            new("cs3.mdf", "cs3", "csdl3.db"),
            new("cs4.mdf", "cs4", "csdl4.db"),
            new("csex.mdf", "csex", "csdlex.xlsx")
        };
        // Ánh xạ tệp ẩn -> tệp DLL/Config thực thi
        private static readonly Dictionary<string, string> DanhSachThuVienCore = new()
        {
            { "data6",  "vcruntime140_cor3.dll" },
            { "data7",  "wpfgfx_cor3.dll" },
            { "data8",  "D3DCompiler_47_cor3.dll" },
            { "data9",  "e_sqlite3.dll" },
            { "data10", "PenImc_cor3.dll" },
            { "data11", "PhanMemThiDua2026.dll.config" },
            { "data12", "PresentationNative_cor3.dll" }
        };
        // Các tệp ĐƯỢC PHÉP tồn tại trong CoreDatabaseRepository (data1 -> data12)
        private static readonly string[] DanhSachDataTheoThuTu =
        {
            "data1", "data2", "data3", "data4", "data5", "data6",
            "data7", "data8", "data9", "data10", "data11", "data12"
        };
        private static readonly HashSet<string> DanhSachDataHopLe =
            new(DanhSachDataTheoThuTu, StringComparer.OrdinalIgnoreCase);
        // Danh sách trắng của chính sách làm sạch (tạo 1 lần, không dựng lại mỗi lần gọi)
        private static readonly HashSet<string> AllowFilesBackup = new(StringComparer.OrdinalIgnoreCase)
            { "cs1.mdf", "cs2.mdf", "cs3.mdf", "cs4.mdf", "csex.mdf", TEN_NHAT_KY_LAM_SACH };
        private static readonly HashSet<string> AllowDirsBackup = new(StringComparer.OrdinalIgnoreCase)
            { THU_MUC_CONG_CU, THU_MUC_HUONG_DAN };
        private static readonly HashSet<string> AllowFilesDatabase = new(StringComparer.OrdinalIgnoreCase)
            { "csdl1.db", "csdl2.db", "csdl3.db", "csdl4.db", "csdlex.xlsx", TEN_NHAT_KY_LAM_SACH };
        private static readonly HashSet<string> AllowDirsDatabase = new(StringComparer.OrdinalIgnoreCase)
            { THU_MUC_BAN_SAO_LUU, THU_MUC_CONG_CU, THU_MUC_HUONG_DAN, THU_MUC_LUUTRUTHIDUA_LICHSU};
        private static readonly HashSet<string> AllowFilesWindow = new(StringComparer.OrdinalIgnoreCase)
            { "cs1", "cs2", "cs3", "cs4", "csex", TEN_NHAT_KY_LAM_SACH };
        private static readonly HashSet<string> AllowFilesBanSaoLuu = new(StringComparer.OrdinalIgnoreCase)
            { TEN_NHAT_KY_LAM_SACH, "GiayPhepCapQuyen_ServiceRestore.dat", "GiayPhepCapQuyen_ServiceBackup.dat" };
        // ===================== TIỆN ÍCH CHUNG =====================
        private static string ChuanHoaDuongDan(string path) =>
            Path.GetFullPath(path).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
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
        private static void DatReadOnly(string path)
        {
            if (File.Exists(path))
            {
                try { File.SetAttributes(path, FileAttributes.ReadOnly); } catch { }
            }
        }
        private static void XoaTepBestEffort(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    ThaoGoQuyenReadOnly(path);
                    File.Delete(path);
                }
            }
            catch { /* dọn tệp tạm, best-effort */ }
        }
        // File phải tồn tại và khác 0 byte mới coi là hợp lệ.
        private static bool FileCoDuLieuHopLe(string filePath)
        {
            try
            {
                return File.Exists(filePath) && new FileInfo(filePath).Length > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FileCoDuLieuHopLe] Không thể kiểm tra '{filePath}': {ex.Message}");
                return false;
            }
        }
        private static string TinhSHA256(string filePath)
        {
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan);
                return Convert.ToHexString(SHA256.HashData(stream));   // không cấp phát đối tượng SHA256 mỗi lần
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TinhSHA256] Không thể tính SHA-256 '{filePath}': {ex.Message}");
                return string.Empty;
            }
        }
        private static bool IsDevelopmentEnv()
        {
            string currentExePath = AppDomain.CurrentDomain.BaseDirectory;
            return currentExePath.Contains(@"\bin\Debug\", StringComparison.OrdinalIgnoreCase) ||
                   currentExePath.Contains(@"\bin\Release\", StringComparison.OrdinalIgnoreCase);
        }
        /// <summary>
        /// Chốt chặn an toàn trước khi XÓA: thư mục phải nằm trong thư mục cài đặt hoặc thư mục dữ liệu,
        /// và không bao giờ là gốc ổ đĩa. Chống thảm họa do cấu hình đường dẫn sai.
        /// </summary>
        private static bool LaThuMucAnToanDeDon(string dirPath)
        {
            try
            {
                string d = ChuanHoaDuongDan(dirPath);
                string? goc = Path.GetPathRoot(d);
                if (string.IsNullOrEmpty(goc) || d.Equals(goc, StringComparison.OrdinalIgnoreCase))
                    return false;   // gốc ổ đĩa
                if (d.StartsWith(ChuanHoaDuongDan(AppContext.BaseDirectory), StringComparison.OrdinalIgnoreCase))
                    return true;
                string dbRoot = Module_DanduongGPS.ThuMucCoSoDuLieu;
                return !string.IsNullOrWhiteSpace(dbRoot) &&
                       d.StartsWith(ChuanHoaDuongDan(dbRoot), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
        /// <summary>Tệp phụ của SQLite: WAL / SHM / rollback journal. Xóa các tệp này khi DB đang ghi sẽ làm hỏng CSDL.</summary>
        private static bool LaTepPhuSQLite(string extension) =>
            extension.EndsWith("-wal", StringComparison.OrdinalIgnoreCase) ||
            extension.EndsWith("-shm", StringComparison.OrdinalIgnoreCase) ||
            extension.EndsWith("-journal", StringComparison.OrdinalIgnoreCase);
        // ===================== GHI TỆP NGUYÊN TỬ =====================
        /// <summary>
        /// Sao chép qua tệp tạm cùng thư mục: Flush xuống đĩa → kiểm tra dung lượng → Move thay thế.
        /// Mất điện / bị ngắt giữa chừng thì tệp đích cũ còn nguyên (không bao giờ để lại tệp rỗng/cụt).
        /// </summary>
        private static void SaoChepAnToan(string nguon, string dich, bool giuThuocTinhVaThoiGian)
        {
            string tam = dich + HAU_TO_TAM;
            try
            {
                XoaTepBestEffort(tam);
                const int bufferSize = 81920;
                long dungLuong;
                using (var s = new FileStream(nguon, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize, FileOptions.SequentialScan))
                using (var t = new FileStream(tam, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.WriteThrough))
                {
                    dungLuong = s.Length;
                    s.CopyTo(t, bufferSize);
                    t.Flush(true);
                }
                if (new FileInfo(tam).Length != dungLuong)
                    throw new IOException($"Sao chép không đủ dung lượng: '{nguon}'.");
                if (giuThuocTinhVaThoiGian)
                {
                    File.SetLastWriteTimeUtc(tam, File.GetLastWriteTimeUtc(nguon));   // đặt thời gian TRƯỚC khi gắn ReadOnly
                    var thuocTinh = File.GetAttributes(nguon) &
                        (FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System | FileAttributes.Archive);
                    File.SetAttributes(tam, thuocTinh == 0 ? FileAttributes.Normal : thuocTinh);
                }
                else
                {
                    File.SetAttributes(tam, FileAttributes.Normal);
                }
                ThaoGoQuyenReadOnly(dich);
                File.Move(tam, dich, true);
            }
            finally
            {
                XoaTepBestEffort(tam);
            }
        }
        /// <summary>Cho phép 1 hàm khác (giải mã/mã hóa CSDL) ghi ra tệp tạm, kiểm tra rồi mới thay thế tệp đích.</summary>
        private static void GhiAnToan(string dich, Action<string> taoTep)
        {
            string tam = dich + HAU_TO_TAM;
            try
            {
                XoaTepBestEffort(tam);
                taoTep(tam);
                if (!FileCoDuLieuHopLe(tam))
                    throw new IOException($"Tệp tạm rỗng hoặc không được tạo: '{tam}'.");
                ThaoGoQuyenReadOnly(dich);
                File.Move(tam, dich, true);
            }
            finally
            {
                XoaTepBestEffort(tam);
            }
        }
        // ===================== KHỞI ĐỘNG =====================
        // KHỞI ĐỘNG BẤT ĐỒNG BỘ: không gây đơ ứng dụng trên máy cấu hình yếu
        // LƯU Ý: nơi gọi phải await xong hàm này TRƯỚC khi mở bất kỳ CSDL nào, vì hàm có thể đang khôi phục chính các tệp CSDL đó.
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
                    try { Module_NhatKy.GhiNhatKy(TEN_TAI_KHOAN_HE_THONG, "LỖI - Khởi tạo CSDL", ex.Message); } catch { }
                }
            });
        }
        private static void BinhMinhOSantoriniCore()
        {
            lock (_khoaBaoTri)
            {
                // Tuần tra và khôi phục chéo tệp data1 -> data12 trước
                TuanTraVaPhucHoiCoreRepository();
                // Khôi phục thư viện hệ thống
                KhoiPhucThuVienHeThong();
                string srcDir = Path.Combine(AppContext.BaseDirectory, THU_MUC_BACKUP);
                string windowDir = Path.Combine(AppContext.BaseDirectory, THU_MUC_WINDOW);
                string dbDir = Module_DanduongGPS.ThuMucCoSoDuLieu;
                Directory.CreateDirectory(srcDir);
                Directory.CreateDirectory(windowDir);
                Directory.CreateDirectory(dbDir);
                var loi = new List<string>();
                byte[]? key = null;
                try
                {
                    key = Module_BaoMatAES.HoaVanNoTrenDuongRaChienDich256v1(Module_DanduongGPS.ToiYeuMeoCam1);
                    foreach (var res in DanhSachTaiNguyen)
                    {
                        // Mỗi tài nguyên tự xử lý lỗi: 1 tệp hỏng không được chặn các tệp còn lại và bước đồng bộ thư mục
                        try
                        {
                            KhoiPhucMotTaiNguyen(res, srcDir, windowDir, dbDir, key);
                        }
                        catch (Exception ex)
                        {
                            string m = $"[LỖI KHÔI PHỤC] {res.Db}: {ex.Message}";
                            Debug.WriteLine(m);
                            loi.Add(m);
                        }
                    }
                }
                finally
                {
                    if (key != null) CryptographicOperations.ZeroMemory(key);   // luôn xóa khóa khỏi RAM, kể cả khi có lỗi
                }
                if (loi.Count > 0)
                {
                    try { Module_NhatKy.GhiNhatKy(TEN_TAI_KHOAN_HE_THONG, "LỖI - Khôi phục CSDL khi khởi động", string.Join("\r\n", loi)); } catch { }
                }
                // Đồng bộ thư mục công cụ & hướng dẫn
                DongBoThuMucHeThong(srcDir, dbDir);
                // Khóa các file bên trong thư mục công cụ của Backup (không khóa thư mục)
                foreach (var folder in ThuMucDongBo)
                {
                    string path = Path.Combine(srcDir, folder);
                    if (!Directory.Exists(path)) continue;
                    foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                    {
                        try { File.SetAttributes(file, FileAttributes.ReadOnly); } catch { }
                    }
                }
            }
        }
        private static void KhoiPhucMotTaiNguyen(TaiNguyenCSDL res, string srcDir, string windowDir, string dbDir, byte[] key)
        {
            string pBackup = Path.Combine(srcDir, res.Backup);
            string pWin = Path.Combine(windowDir, res.Win);
            string pDb = Path.Combine(dbDir, res.Db);
            // Tệp 0 byte (do mất điện lúc ghi) coi như mất — bản gốc chỉ kiểm tra File.Exists nên tệp rỗng tồn tại vĩnh viễn
            bool b = FileCoDuLieuHopLe(pBackup);
            bool d = FileCoDuLieuHopLe(pDb);
            bool w = FileCoDuLieuHopLe(pWin);
            if (b) // Backup là nguồn sạch nhất
            {
                if (!d) GhiAnToan(pDb, tam => Module_BaoMatAES.GiaiMaCSDL(pBackup, tam, key));
                if (!w) GhiAnToan(pWin, tam => Module_BaoMatAES.MaHoaCSDL(pDb, tam, key));
            }
            else if (d) // Backup mất
            {
                GhiAnToan(pBackup, tam => Module_BaoMatAES.MaHoaCSDL(pDb, tam, key));
                if (!w) GhiAnToan(pWin, tam => Module_BaoMatAES.MaHoaCSDL(pDb, tam, key));
            }
            else if (w) // Mất cả Backup và DB
            {
                GhiAnToan(pDb, tam => Module_BaoMatAES.GiaiMaCSDL(pWin, tam, key));
                GhiAnToan(pBackup, tam => Module_BaoMatAES.MaHoaCSDL(pDb, tam, key));
            }
            // Bảo vệ các tệp đã đồng bộ
            DatReadOnly(pBackup);
            DatReadOnly(pWin);
        }
        // ===================== KHÔI PHỤC THƯ VIỆN HỆ THỐNG =====================
        private static void KhoiPhucThuVienHeThong()
        {
            try
            {
                string baseDir = AppContext.BaseDirectory;
                string repositoryDir = Path.Combine(Module_DanduongGPS.ThuMucCoSoDuLieu, THU_MUC_CONG_CU, THU_MUC_REPOSITORY);
                if (!Directory.Exists(repositoryDir)) return;
                // Chỉ vài tệp nhỏ và hiếm khi thiếu: chạy tuần tự (Parallel chỉ tốn luồng và làm HDD chậm hơn)
                foreach (var item in DanhSachThuVienCore)
                {
                    string target = Path.Combine(baseDir, item.Value);
                    if (FileCoDuLieuHopLe(target)) continue;   // tệp 0 byte cũng được khôi phục
                    string source = Path.Combine(repositoryDir, item.Key);
                    if (!FileCoDuLieuHopLe(source)) continue;
                    try
                    {
                        SaoChepAnToan(source, target, false);
                        Debug.WriteLine($"[ĐÃ KHÔI PHỤC THƯ VIỆN]: {Path.GetFileName(target)}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[LỖI SAO CHÉP THƯ VIỆN {target}]: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Lỗi khôi phục thư viện hệ thống]: " + ex.Message);
            }
        }
        // ===================== ĐỒNG BỘ THƯ MỤC CÔNG CỤ & HƯỚNG DẪN =====================
        private sealed class KetQuaDongBo
        {
            private readonly List<string> _chiTiet = new();
            public int SoLoi { get; private set; }
            public IReadOnlyList<string> ChiTiet => _chiTiet;
            public void Loi(string message)
            {
                SoLoi++;
                if (_chiTiet.Count < MAX_LOI_GHI_NHAT_KY) _chiTiet.Add(message);
                Debug.WriteLine(message);
            }
            public void DatLai()
            {
                SoLoi = 0;
                _chiTiet.Clear();
            }
        }
        private static void DongBoThuMucHeThong(string srcDir, string dbDir)
        {
            var kq = new KetQuaDongBo();
            // 1 lượt đồng bộ 2 chiều là đủ để hội tụ; chỉ lặp lại (tự chữa) khi còn lỗi — bản gốc luôn chạy đủ 3 lượt duyệt toàn bộ cây thư mục
            for (int luot = 1; luot <= SO_LUOT_DONG_BO_TOI_DA; luot++)
            {
                kq.DatLai();
                foreach (string folder in ThuMucDongBo)
                {
                    string a = Path.Combine(srcDir, folder);
                    string b = Path.Combine(dbDir, folder);
                    try
                    {
                        Directory.CreateDirectory(a);
                        Directory.CreateDirectory(b);
                    }
                    catch (Exception ex)
                    {
                        kq.Loi($"[ERROR DIR] {folder} | {ex.Message}");
                        continue;
                    }
                    SyncFolder(a, b, kq);
                    SyncFolder(b, a, kq);
                }
                if (kq.SoLoi == 0) break;
            }
            // Kiểm tra cấu trúc: cả 2 vùng đều không có thư mục lõi là bất thường nghiêm trọng
            bool coThuMucLoi = false;
            foreach (string folder in ThuMucDongBo)
            {
                if (Directory.Exists(Path.Combine(srcDir, folder)) || Directory.Exists(Path.Combine(dbDir, folder)))
                    coThuMucLoi = true;
            }
            if (!coThuMucLoi)
                kq.Loi("[CRITICAL WARNING] CẢ 2 VÙNG DATABASE VÀ BACKUP KHÔNG TỒN TẠI THƯ MỤC CORE (CongCuQuanLyCSDL / HuongDanSuDung)");
            // Nhật ký bản gốc chỉ nằm trong RAM rồi bị bỏ; nay nếu còn lỗi thì ghi 1 dòng tóm tắt để chẩn đoán tại hiện trường
            if (kq.SoLoi > 0)
            {
                try
                {
                    Module_NhatKy.GhiNhatKy(TEN_TAI_KHOAN_HE_THONG, "Đồng bộ thư mục hệ thống (Tự động)",
                        $"Còn {kq.SoLoi} lỗi sau {SO_LUOT_DONG_BO_TOI_DA} lượt thử.\r\n" + string.Join("\r\n", kq.ChiTiet));
                }
                catch { }
            }
        }
        private static bool LaTepTamDongBo(string name) =>
            name.EndsWith(HAU_TO_TAM, StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith(HAU_TO_SYNCBAK, StringComparison.OrdinalIgnoreCase);
        /// <summary>
        /// Quy tắc "bản mới hơn thắng". Bản gốc có thêm điều kiện "khác kích thước thì chép" KHÔNG xét thời gian,
        /// khiến bản CŨ ở chiều A→B ghi đè bản MỚI ở B. Nay: khác kích thước chỉ có giá trị khi thời gian xấp xỉ bằng nhau.
        /// </summary>
        private static bool CanSaoChep(FileInfo nguon, FileInfo dich)
        {
            if (!dich.Exists) return true;
            if (nguon.Length == 0 && dich.Length > 0) return false;   // không bao giờ ghi đè bản có dữ liệu bằng bản rỗng
            if (dich.Length == 0 && nguon.Length > 0) return true;    // tệp đích rỗng/cụt luôn được thay
            TimeSpan chenhLech = nguon.LastWriteTimeUtc - dich.LastWriteTimeUtc;
            if (chenhLech > DungSaiThoiGian) return true;             // nguồn mới hơn
            if (chenhLech < -DungSaiThoiGian) return false;           // đích mới hơn: giữ nguyên
            return nguon.Length != dich.Length;                       // cùng thời điểm nhưng khác dung lượng
        }
        private static void SyncFolder(string src, string dst, KetQuaDongBo kq)
        {
            try
            {
                if (!Directory.Exists(src)) return;
                Directory.CreateDirectory(dst);
                foreach (string file in Directory.EnumerateFiles(src))
                {
                    string name = Path.GetFileName(file);
                    // Không đồng bộ tệp tạm/rác còn sót (bản gốc từng chép cả .tmp/.syncbak sang vùng kia)
                    if (LaTepTamDongBo(name)) continue;
                    string target = Path.Combine(dst, name);
                    try
                    {
                        if (CanSaoChep(new FileInfo(file), new FileInfo(target)))
                            SaoChepAnToan(file, target, true);   // ghi nguyên tử; đã bỏ bước .syncbak thừa (Move đã thay thế nguyên tử)
                    }
                    catch (Exception ex)
                    {
                        kq.Loi($"[ERROR FILE] {file} | {ex.Message}");
                    }
                }
                foreach (string dir in Directory.EnumerateDirectories(src))
                {
                    string name = Path.GetFileName(dir);
                    // Kho lõi do TuanTraVaPhucHoiCoreRepository quản lý riêng
                    if (DONG_BO_BO_QUA_CORE_REPOSITORY &&
                        name.Equals(THU_MUC_REPOSITORY, StringComparison.OrdinalIgnoreCase))
                        continue;
                    // Chống vòng lặp vô hạn do junction/symlink
                    try
                    {
                        if ((File.GetAttributes(dir) & FileAttributes.ReparsePoint) != 0) continue;
                    }
                    catch { continue; }
                    SyncFolder(dir, Path.Combine(dst, name), kq);
                }
            }
            catch (Exception ex)
            {
                kq.Loi($"[ERROR DIR] {src} | {ex.Message}");
            }
        }
        // ===================== DỌN RÁC =====================
        /// <summary>
        /// Làm sạch thư mục: chỉ giữ các tệp thuộc họ CSDL SQLite (.db, .sqlite và tệp phụ -wal/-shm/-journal).
        /// Mọi tệp khác bị xóa. Chỉ chạy ở môi trường triển khai và chỉ với thư mục nằm trong vùng của phần mềm.
        /// </summary>
        public static void donRacThuMucBaoVe(string folderPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                    return;
                // Trước đây hàm này KHÔNG có chốt chặn môi trường phát triển và đường dẫn như các hàm dọn dẹp khác
                if (IsDevelopmentEnv() || !LaThuMucAnToanDeDon(folderPath))
                {
                    Debug.WriteLine($"[donRacThuMucBaoVe] Bỏ qua (môi trường phát triển hoặc ngoài vùng an toàn): {folderPath}");
                    return;
                }
                foreach (string filePath in Directory.EnumerateFiles(folderPath, "*", SearchOption.TopDirectoryOnly).ToArray())
                {
                    var fi = new FileInfo(filePath);
                    bool isDbFile = fi.Extension.Equals(".db", StringComparison.OrdinalIgnoreCase) ||
                                    fi.Extension.Equals(".sqlite", StringComparison.OrdinalIgnoreCase) ||
                                    LaTepPhuSQLite(fi.Extension);   // thêm -journal: xóa khi đang ghi sẽ hỏng CSDL
                    if (isDbFile) continue;
                    try
                    {
                        fi.Attributes = FileAttributes.Normal;
                        fi.Delete();
                        Debug.WriteLine($"[ĐÃ XÓA TỆP RÁC]: {fi.Name}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[LỖI XÓA TỆP RÁC {fi.Name}]: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LỖI QUÉT THƯ MỤC DỌN RÁC]: {ex.Message}");
            }
        }
        public static void ChinhSachLamSach()
        {
            lock (_khoaBaoTri)
            {
                try
                {
                    string baseDir = ChuanHoaDuongDan(AppContext.BaseDirectory);
                    // 1. Database Backup
                    DonDepVungQuanLy(Path.Combine(baseDir, THU_MUC_BACKUP), AllowFilesBackup, AllowDirsBackup, false, false);
                    // 2. Database
                    string dir2 = Path.GetFullPath(string.IsNullOrWhiteSpace(Module_DanduongGPS.ThuMucCoSoDuLieu)
                        ? Path.Combine(baseDir, THU_MUC_DATABASE)
                        : Module_DanduongGPS.ThuMucCoSoDuLieu);
                    DonDepVungQuanLy(dir2, AllowFilesDatabase, AllowDirsDatabase, false, true);
                    // 3. window-x64
                    DonDepVungQuanLy(Path.Combine(baseDir, THU_MUC_WINDOW), AllowFilesWindow, null, true, false);
                    // 4. Database\Bansaoluu — suy ra từ thư mục dữ liệu thực (bản gốc cố định baseDir\Database nên lệch khi ThuMucCoSoDuLieu khác)
                    DonDepVungQuanLy(Path.Combine(dir2, THU_MUC_BAN_SAO_LUU), AllowFilesBanSaoLuu, null, true, false);
                    // 5. Lịch sử thi đua (chỉ giữ họ *.db)
                    donRacThuMucBaoVe(Module_DanduongGPS.ThuMucLichSuThiDua);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ChinhSachLamSach] {ex.Message}");
                }
            }
        }
        private static void DonDepVungQuanLy(string dirPath, HashSet<string>? allowedFiles, HashSet<string>? allowedDirs, bool deleteSubDirs, bool isSQLiteZone)
        {
            // Chặn hủy diệt file khi đang lập trình/test code
            if (IsDevelopmentEnv() || string.IsNullOrWhiteSpace(dirPath))
                return;
            string currentDir;
            try
            {
                currentDir = ChuanHoaDuongDan(dirPath);
            }
            catch
            {
                return; // ký tự đường dẫn không hợp lệ
            }
            // Chốt chặn: chỉ dọn trong vùng của phần mềm, không bao giờ gốc ổ đĩa
            if (!LaThuMucAnToanDeDon(currentDir))
            {
                Debug.WriteLine($"[DonDepVungQuanLy] Từ chối dọn thư mục ngoài vùng an toàn: {currentDir}");
                return;
            }
            if (!Directory.Exists(currentDir))
            {
                try { Directory.CreateDirectory(currentDir); } catch { return; }
            }
            var chiTiet = new List<string>();
            int fCount = 0, dCount = 0;
            string tenTaiKhoan = Module_TaiKhoan.TenTaiKhoan_RAM;
            bool laVungBanSaoLuu = currentDir.Contains(THU_MUC_BAN_SAO_LUU, StringComparison.OrdinalIgnoreCase);
            // XỬ LÝ TỆP
            try
            {
                foreach (string file in Directory.GetFiles(currentDir))
                {
                    var fi = new FileInfo(file);
                    if (fi.Name.Equals(TEN_NHAT_KY_LAM_SACH, StringComparison.OrdinalIgnoreCase)) continue;
                    // SQLite zone: giữ WAL / SHM / rollback journal (thêm -journal: chế độ journal mặc định của SQLite)
                    if (isSQLiteZone && LaTepPhuSQLite(fi.Extension)) continue;
                    // Backup zone
                    if (laVungBanSaoLuu &&
                        fi.Name.StartsWith("Backup_", StringComparison.OrdinalIgnoreCase) &&
                        fi.Extension.Equals(".pmtd", StringComparison.OrdinalIgnoreCase))
                        continue;
                    // Danh sách trắng (O(1))
                    if (allowedFiles != null && allowedFiles.Contains(fi.Name)) continue;
                    long kichThuoc = fi.Length;
                    try
                    {
                        fi.Attributes = FileAttributes.Normal;
                        fi.Delete();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[DonDepVungQuanLy] Không thể xóa '{fi.Name}': {ex.Message}");
                        continue;
                    }
                    fi.Refresh();
                    if (!fi.Exists)
                    {
                        fCount++;
                        chiTiet.Add($"[TỆP] {fi.Name} | Dung lượng: {FormatSize(kichThuoc)} | Thư mục: {currentDir}");
                        Module_NhatKy.GhiNhatKy(
                            taiKhoan: tenTaiKhoan,
                            hanhDong: "Xóa file hệ thống (Auto Clean)",
                            ghiChu: $"=== Thông tin xóa file ==={Environment.NewLine}" +
                                   $"Thư mục xử lý  : {currentDir}{Environment.NewLine}" +
                                   $"Tên tệp        : {fi.Name}{Environment.NewLine}" +
                                   $"Dung lượng     : {FormatSize(kichThuoc)}{Environment.NewLine}" +
                                   $"Loại hành động : Xóa tự động do hệ thống phát hiện file không hợp lệ{Environment.NewLine}" +
                                   $"Thời gian      : {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi quét file]: {ex.Message}");
            }
            // XỬ LÝ THƯ MỤC
            try
            {
                foreach (string dir in Directory.GetDirectories(currentDir))
                {
                    var di = new DirectoryInfo(dir);
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
                                    hanhDong: "Xóa thư mục hệ thống (Auto Clean)",
                                    ghiChu: $"Thông tin xóa thu mục {Environment.NewLine}" +
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
                Debug.WriteLine($"[Lỗi quét thư mục]: {ex.Message}");
            }
            // Báo cáo tổng hợp nếu có biến động
            if (fCount > 0 || dCount > 0)
            {
                try
                {
                    GhiDeNhatKy(Path.Combine(currentDir, TEN_NHAT_KY_LAM_SACH), TaoNoiDungBaoCao(fCount, dCount, 0, chiTiet));
                }
                catch { /* bỏ qua lỗi ghi nhật ký cục bộ nếu thư mục bị khóa */ }
            }
        }
        // ===================== TƯỜNG LỬA BẢO VỆ HỆ THỐNG =====================
        // Quy tắc nghiệp vụ giữ nguyên (không dùng IsDevelopmentEnv theo yêu cầu cũ). Chỉ thêm: tuần tự hóa, chuẩn hóa đường dẫn
        // trước khi so sánh gốc (chống "..\" lọt qua StartsWith), và tìm tiến trình theo tên thay vì quét toàn bộ tiến trình.
        public static void TuongLuaBaoVeHeThong(string rootPath)
        {
            string currentExePath = AppDomain.CurrentDomain.BaseDirectory;
            // Bỏ qua nếu đang debug
            bool isDevelopmentEnv = currentExePath.Contains(@"\bin\Debug\", StringComparison.OrdinalIgnoreCase) ||
                                    currentExePath.Contains(@"\bin\Release\", StringComparison.OrdinalIgnoreCase);
            if (isDevelopmentEnv || string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
                return;
            // CHỐT CHẶN TỬ THẦN: rootPath phải nằm bên trong thư mục cài đặt gốc (đã chuẩn hóa, có dấu phân cách cuối)
            string rootChuan;
            try
            {
                rootChuan = ChuanHoaDuongDan(rootPath);
                if (!rootChuan.StartsWith(ChuanHoaDuongDan(currentExePath), StringComparison.OrdinalIgnoreCase))
                {
                    try { Module_NhatKy.GhiNhatKy("System", "CẢNH BÁO BẢO MẬT", "Phát hiện nỗ lực quét sai thư mục gốc: " + rootPath); } catch { }
                    return;
                }
            }
            catch
            {
                return;
            }
            rootPath = rootChuan;
            // DANH SÁCH TRẮNG CỐ ĐỊNH: khi nâng phiên bản .NET hoặc đổi cách publish (thêm/bớt tệp ở thư mục gốc)
            // PHẢI cập nhật các danh sách dưới đây, nếu không tường lửa sẽ xóa tệp hợp lệ của phiên bản mới.
            // CSDL_Backups (Module_BaoVeDuLieu) nằm ở thư mục gốc: thiếu tên này trong danh sách trắng thì tường lửa xóa sạch mọi bản sao lưu CSDL
            var allowedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { THU_MUC_DATABASE, THU_MUC_BACKUP, THU_MUC_WINDOW, Module_BaoVeDuLieu.TenThuMucBackup};
            var allowedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "D3DCompiler_47_cor3.dll", "e_sqlite3.dll", "PenImc_cor3.dll", "PhanMemThiDua2026.dll.config",
                "PhanMemThiDua2026.exe", "PresentationNative_cor3.dll", "ServiceBackup.exe",
                "ServiceRestore.exe", "Uninstall_PhanMemThiDua2026.exe", "vcruntime140_cor3.dll",
                "wpfgfx_cor3.dll", TEN_NHAT_KY_LAM_SACH
            };
            lock (_khoaBaoTri)
            {
                var chiTiet = new List<string>(32);
                int fCount = 0, dCount = 0, eCount = 0;
                const int MAX_DELETE_PER_SESSION = 100;
                string logPath = Path.Combine(rootPath, TEN_NHAT_KY_LAM_SACH);
                try
                {
                    // 1. XỬ LÝ THƯ MỤC LẠ
                    try
                    {
                        foreach (string dirPath in Directory.EnumerateDirectories(rootPath))
                        {
                            if (fCount + dCount >= MAX_DELETE_PER_SESSION) break;
                            var dirInfo = new DirectoryInfo(dirPath);
                            if ((dirInfo.Attributes & FileAttributes.ReparsePoint) != 0 || allowedFolders.Contains(dirInfo.Name))
                                continue;
                            try
                            {
                                RemoveReadOnlyRecursive(dirInfo.FullName);
                                dirInfo.Delete(true);
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
                    catch { /* bỏ qua lỗi Access Denied cấp thư mục gốc nếu có */ }
                    // 2. XỬ LÝ TỆP TIN LẠ
                    try
                    {
                        foreach (string filePath in Directory.EnumerateFiles(rootPath))
                        {
                            if (fCount + dCount >= MAX_DELETE_PER_SESSION) break;
                            var fileInfo = new FileInfo(filePath);
                            if (allowedFiles.Contains(fileInfo.Name)) continue;
                            bool laTepThucThi = fileInfo.Extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
                                                fileInfo.Extension.Equals(".dll", StringComparison.OrdinalIgnoreCase);
                            bool daXoaThanhCong = XoaTepVoiCoCheRetry(fileInfo);
                            if (!daXoaThanhCong && laTepThucThi)
                            {
                                // Tệp thực thi đang chạy ngầm -> Kill tiến trình và thử lại
                                try
                                {
                                    if (CuongBucTatTienTrinhDangLockFile(fileInfo.FullName, rootPath))
                                    {
                                        daXoaThanhCong = XoaTepVoiCoCheRetry(fileInfo, retries: 5, delayMs: 100);
                                        if (daXoaThanhCong)
                                            chiTiet.Add($"[TIÊU DIỆT SAU KILL] Đã ép xóa: {fileInfo.Name}");
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
                    catch { /* bỏ qua lỗi Access Denied cấp thư mục gốc */ }
                    // 3. GHI NHẬT KÝ
                    if (fCount > 0 || dCount > 0 || eCount > 0)
                    {
                        GhiDeNhatKy(logPath, TaoNoiDungBaoCao(fCount, dCount, eCount, chiTiet));
                        Module_NhatKy.GhiNhatKy("System", "Chính sách tự vệ (Bảo vệ tự động)", $"Xử lý an toàn {fCount} tệp, {dCount} thư mục.");
                    }
                    else
                    {
                        try { if (File.Exists(logPath)) { File.SetAttributes(logPath, FileAttributes.Normal); File.Delete(logPath); } } catch { }
                    }
                }
                catch (Exception ex)
                {
                    try { Module_NhatKy.GhiNhatKy("System", "LỖI - Chính sách tự vệ (Bảo vệ tự động)", ex.Message); } catch { }
                }
            }
        }
        // Thử xóa file nhiều lần (Retry Pattern), chống crash do Windows chưa kịp nhả handle
        private static bool XoaTepVoiCoCheRetry(FileInfo fileInfo, int retries = 3, int delayMs = 50)
        {
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    if (fileInfo.Exists)
                    {
                        fileInfo.Attributes = FileAttributes.Normal;
                        fileInfo.Delete();   // KHÔNG chuyển vào thùng rác
                    }
                    return true;
                }
                catch (UnauthorizedAccessException)
                {
                    return false;   // lỗi phân quyền từ HĐH: không cần chờ
                }
                catch (IOException)
                {
                    if (i < retries - 1) Thread.Sleep(delayMs);   // tệp đang bị khóa: chờ 1 nhịp rồi thử lại
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
            Process[] danhSach = Array.Empty<Process>();
            try
            {
                // Chỉ lấy các tiến trình cùng tên với tệp .exe cần xóa (trước đây liệt kê và mở MainModule của TOÀN BỘ tiến trình trên máy)
                danhSach = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(filePath));
                foreach (Process p in danhSach)
                {
                    try
                    {
                        if (p.HasExited || p.Id <= 4 || p.Id == Environment.ProcessId) continue;   // không bao giờ tự kill mình
                        string? processPath = p.MainModule?.FileName;
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
                }
            }
            catch { }
            finally
            {
                foreach (Process p in danhSach)
                {
                    try { p.Dispose(); } catch { }   // luôn giải phóng handle, tránh phình RAM trên máy trạm yếu
                }
            }
            return daKillThanhCong;
        }
        // ===================== BÁO CÁO / NHẬT KÝ =====================
        private static string TaoNoiDungBaoCao(int files, int dirs, int errs, List<string> chiTiet)
        {
            string tenTaiKhoan = string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM) ? "System (Quyền cao nhất)" : Module_TaiKhoan.TenTaiKhoan_RAM;
            DateTime bayGio = DateTime.Now;   // lấy 1 lần: ngày và giờ không lệch nhau khi qua nửa đêm
            var sb = new StringBuilder(1024);
            sb.AppendLine();
            sb.AppendLine("[BÁO CÁO DỌN DẸP HỆ THỐNG - PHẦN MỀM THI ĐUA 2026]");
            sb.AppendLine("-------------------------------------------------------------");
            sb.AppendLine($"Tên phần mềm        : PhanMemThiDua2026");
            sb.AppendLine($"Phiên bản           : {Module_PhienBan.SoftwareVersion}");
            sb.AppendLine($"Cập nhật ngày       : {Module_PhienBan.NgayThangNamHeThong}");
            sb.AppendLine($"Người phát triển    : {Module_PhienBan.NguoiPhatTrienPhanMem}");
            sb.AppendLine($"Tên tài khoản       : {tenTaiKhoan}");
            sb.AppendLine($"Máy tính            : {Environment.MachineName}");
            sb.AppendLine($"Thời gian thực hiện : ngày {bayGio:dd/MM/yyyy}, giờ {bayGio:HH:mm:ss}");
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
            return sb.ToString();
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
            if (bytes < 1024L * 1024 * 1024) return $"{(bytes / (1024.0 * 1024.0)):F1} MB";
            return $"{(bytes / (1024.0 * 1024.0 * 1024.0)):F1} GB";
        }
        private static void GhiDeNhatKy(string logPath, string logContent)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    string? dir = Path.GetDirectoryName(logPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    if (File.Exists(logPath)) File.SetAttributes(logPath, FileAttributes.Normal);
                    File.WriteAllText(logPath, logContent, Encoding.UTF8);
                    return;
                }
                catch (Exception ex)
                {
                    if (i == 2) Debug.WriteLine($"[GhiDeNhatKy] Không thể ghi log '{logPath}' sau 3 lần thử: {ex.Message}");
                    else Thread.Sleep(200);   // không ngủ thừa sau lần thử cuối
                }
            }
        }
        private static void RemoveReadOnlyRecursive(string folder)
        {
            try
            {
                foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
                {
                    try { File.SetAttributes(file, FileAttributes.Normal); } catch { }
                }
                foreach (string dir in Directory.EnumerateDirectories(folder, "*", SearchOption.AllDirectories))
                {
                    try { new DirectoryInfo(dir).Attributes = FileAttributes.Normal; } catch { }
                }
                new DirectoryInfo(folder).Attributes = FileAttributes.Normal;
            }
            catch { }
        }
        // ===================== QUẢN LÝ COMBOBOX CÂU HỎI BẢO MẬT =====================
        public static readonly string[] DanhSachCauHoiNhom1 = {
            "Họ và tên của bạn?", "Bạn sinh ra ở Tỉnh/Thành phố nào?", "Món ăn yêu thích nhất của bạn là gì?",
            "Tên trường THPT của bạn?", "Tên thú cưng bạn yêu thích?", "Tên cô người bạn yêu đầu tiên?",
            "Tên bộ phim mà bạn yêu thích nhất?", "Công việc đầu tiên bạn làm để kiếm ra tiền?",
            "Nghề nghiệp mơ ước của bạn khi còn nhỏ là gì?", "Bạn đã từng đi du lịch nước ngoài chưa?"
        };
        public static readonly string[] DanhSachCauHoiNhom2 = {
            "Tên con vật yêu thích của bạn?", "Giới tính của bạn là Nam hay nữ?", "Bạn vào CAND ngày tháng năm nào?",
            "Món quà sinh nhật đầu tiên bạn nhận được là gì?", "Bạn có thích chơi game không?",
            "Bạn thích đội bóng hoặc câu lạc bộ thể thao nào?", "Bài hát mà bạn nghe đi nghe lại nhiều nhất thời học sinh là gì?",
            "Bạn biết bơi lội không?", "Tên con mèo của bạn là gì?", "Bạn có biết ngôn ngữ lập trình Visual Basic không?",
            "Bạn có biết ngôn ngữ lập trình JavaScript không?"
        };
        /// <summary>Cấu hình của 1 nhóm ô chọn (lưu trong Tag): danh sách câu hỏi gốc + các ô cùng nhóm. Dùng chung cho ComboBox và KryptonComboBox.</summary>
        private sealed class NhomCauHoi
        {
            public NhomCauHoi(string[] danhSachGoc, Control?[] cacO)
            {
                DanhSachGoc = danhSachGoc;
                CacO = cacO;
            }
            public string[] DanhSachGoc { get; }
            public Control?[] CacO { get; }
        }
        // KryptonComboBox KHÔNG kế thừa System.Windows.Forms.ComboBox: bản gốc kiểm tra "sender is ComboBox" và Tag kiểu
        // Tuple<string[], ComboBox[]> nên với KryptonComboBox chức năng "ẩn câu hỏi đã chọn" không bao giờ hoạt động.
        private static IList? LayItems(Control c) => c switch
        {
            ComboBox cb => cb.Items,
            Krypton.Toolkit.KryptonComboBox k => k.Items,
            _ => null
        };
        private static object? LayMucDangChon(Control c) => c switch
        {
            ComboBox cb => cb.SelectedItem,
            Krypton.Toolkit.KryptonComboBox k => k.SelectedItem,
            _ => null
        };
        private static void BatDauCapNhat(Control c)
        {
            if (c is ComboBox cb) cb.BeginUpdate();
            else if (c is Krypton.Toolkit.KryptonComboBox k) k.BeginUpdate();
        }
        private static void KetThucCapNhat(Control c)
        {
            if (c is ComboBox cb) cb.EndUpdate();
            else if (c is Krypton.Toolkit.KryptonComboBox k) k.EndUpdate();
        }
        private static void DatChuaChon(Control c)
        {
            if (c is ComboBox cb) cb.SelectedIndex = -1;
            else if (c is Krypton.Toolkit.KryptonComboBox k) k.SelectedIndex = -1;
        }
        private static void DangKySuKien(Control c, bool dangKy)
        {
            switch (c)
            {
                case ComboBox cb:
                    if (dangKy) cb.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
                    else cb.SelectedIndexChanged -= ComboBox_SelectedIndexChanged;
                    break;
                case Krypton.Toolkit.KryptonComboBox k:
                    if (dangKy) k.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
                    else k.SelectedIndexChanged -= ComboBox_SelectedIndexChanged;
                    break;
            }
        }
        private static void ComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (sender is not Control changed || changed.Tag is not NhomCauHoi nhom) return;
            if (!changed.ContainsFocus) return;   // chống kích hoạt lặp vòng lặp khi code tự đổi lựa chọn
            CapNhatDanhSachHienThi(nhom);
        }
        private static void CapNhatDanhSachHienThi(NhomCauHoi nhom)
        {
            var daChon = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in nhom.CacO)
            {
                if (c != null && LayMucDangChon(c) != null && !string.IsNullOrWhiteSpace(c.Text))
                    daChon.Add(c.Text);
            }
            foreach (var c in nhom.CacO)
            {
                if (c == null) continue;
                var items = LayItems(c);
                if (items == null) continue;
                DangKySuKien(c, false);
                try
                {
                    string hienTai = c.Text;
                    BatDauCapNhat(c);
                    try
                    {
                        items.Clear();
                        foreach (string cauHoi in nhom.DanhSachGoc)
                        {
                            if (!daChon.Contains(cauHoi) || cauHoi.Equals(hienTai, StringComparison.OrdinalIgnoreCase))
                                items.Add(cauHoi);
                        }
                        if (!string.IsNullOrEmpty(hienTai)) c.Text = hienTai;
                    }
                    finally
                    {
                        KetThucCapNhat(c);
                    }
                }
                finally
                {
                    DangKySuKien(c, true);   // luôn gắn lại sự kiện, kể cả khi có lỗi (bản gốc để mất sự kiện nếu ném ngoại lệ)
                }
            }
        }
        private static void NapDuLieuCore(string[]? danhSachCauHoi, Control?[]? cacO)
        {
            if (cacO == null || cacO.Length == 0 || danhSachCauHoi == null) return;
            var nhom = new NhomCauHoi(danhSachCauHoi, cacO);
            foreach (var c in cacO)
            {
                if (c == null) continue;
                var items = LayItems(c);
                if (items == null) continue;
                DangKySuKien(c, false);
                BatDauCapNhat(c);
                try
                {
                    items.Clear();
                    foreach (string cauHoi in danhSachCauHoi) items.Add(cauHoi);
                    DatChuaChon(c);
                }
                finally
                {
                    KetThucCapNhat(c);
                }
                c.Tag = nhom;
                DangKySuKien(c, true);
            }
        }
        // 1. Dành cho ComboBox truyền thống (WinForms mặc định)
        public static void NapDuLieuCauHoiBaoMat(string[] danhSachCauHoi, params ComboBox[] comboBoxes) =>
            NapDuLieuCore(danhSachCauHoi, comboBoxes);
        // 2. Dành cho KryptonComboBox hiện đại (Giao diện mới)
        public static void NapDuLieuCauHoiBaoMat(string[] danhSachCauHoi, params Krypton.Toolkit.KryptonComboBox[] comboBoxes) =>
            NapDuLieuCore(danhSachCauHoi, comboBoxes);
        // ===================== TUẦN TRA & KHÔI PHỤC CHÉO CORE REPOSITORY =====================
        private sealed class KetQuaTuanTra
        {
            public int TepBiXoa;
            public int TepDuocCuu;
            public int TepHong;      // tệp tồn tại nhưng 0 byte (bản gốc khai báo biến này nhưng không bao giờ tăng → luôn báo 0)
            public int CanhBao;
            public readonly List<string> ChiTiet = new();
        }
        public static void TuanTraVaPhucHoiCoreRepository()
        {
            lock (_khoaBaoTri)
            {
                try
                {
                    string baseDir = AppContext.BaseDirectory;
                    // 2 vùng chứa repository
                    string dirDatabase = Path.Combine(Module_DanduongGPS.ThuMucCoSoDuLieu, THU_MUC_CONG_CU, THU_MUC_REPOSITORY);
                    string dirBackup = Path.Combine(baseDir, THU_MUC_BACKUP, THU_MUC_CONG_CU, THU_MUC_REPOSITORY);
                    Directory.CreateDirectory(dirDatabase);
                    Directory.CreateDirectory(dirBackup);
                    var kq = new KetQuaTuanTra();
                    // BƯỚC 1: tiêu diệt tệp lạ
                    TieuDietTepLa(dirDatabase, "Database Gốc", kq);
                    TieuDietTepLa(dirBackup, "Database Backup", kq);
                    // BƯỚC 2: khôi phục chéo + kiểm tra toàn vẹn
                    foreach (string fileName in DanhSachDataTheoThuTu)
                        KiemTraMotTep(fileName, dirDatabase, dirBackup, kq);
                    // BƯỚC 3: ghi nhật ký
                    GhiNhatKyTuanTra(kq);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[Lỗi Tuần tra Core Repository]: " + ex);
                }
            }
        }
        private static void TieuDietTepLa(string thuMucPath, string tenVung, KetQuaTuanTra kq)
        {
            foreach (string filePath in Directory.GetFiles(thuMucPath))
            {
                string fileName = Path.GetFileName(filePath);
                if (DanhSachDataHopLe.Contains(fileName)) continue;   // file hợp lệ → giữ nguyên
                try
                {
                    ThaoGoQuyenReadOnly(filePath);
                    File.Delete(filePath);
                    kq.TepBiXoa++;
                    kq.ChiTiet.Add($"[TIÊU DIỆT] Đã xóa tệp lạ '{fileName}' tại vùng {tenVung}.");
                }
                catch (Exception ex)
                {
                    kq.ChiTiet.Add($"[LỖI XÓA] Không thể xóa tệp lạ '{fileName}' tại {tenVung}: {ex.Message}");
                    Debug.WriteLine($"[TuanTraCoreRepository] Lỗi xóa {fileName}: {ex.Message}");
                }
            }
        }
        private static void KiemTraMotTep(string fileName, string dirDatabase, string dirBackup, KetQuaTuanTra kq)
        {
            string fileInDB = Path.Combine(dirDatabase, fileName);
            string fileInBackup = Path.Combine(dirBackup, fileName);
            // File tồn tại nhưng 0 byte → coi như không hợp lệ
            bool existsInDB = FileCoDuLieuHopLe(fileInDB);
            bool existsInBackup = FileCoDuLieuHopLe(fileInBackup);
            // TH1: BACKUP CÒN - DATABASE MẤT/HỎNG
            if (!existsInDB && existsInBackup)
            {
                if (File.Exists(fileInDB)) kq.TepHong++;
                try
                {
                    // Ghi nguyên tử + gỡ ReadOnly của tệp đích rỗng: bản gốc File.Copy(..., true) đè lên tệp ReadOnly 0 byte sẽ ném
                    // UnauthorizedAccessException → tệp lõi không bao giờ tự phục hồi được.
                    SaoChepAnToan(fileInBackup, fileInDB, false);
                    File.SetAttributes(fileInDB, FileAttributes.ReadOnly);
                    kq.TepDuocCuu++;
                    kq.ChiTiet.Add($"[KHÔI PHỤC] '{fileName}' được phục hồi từ Backup → Database Gốc.");
                }
                catch (Exception ex)
                {
                    kq.ChiTiet.Add($"[LỖI KHÔI PHỤC] Không thể phục hồi '{fileName}' từ Backup → Database: {ex.Message}");
                    Debug.WriteLine($"[TuanTraCoreRepository] Lỗi copy {fileName}: {ex.Message}");
                }
                return;
            }
            // TH2: DATABASE CÒN - BACKUP MẤT/HỎNG
            if (existsInDB && !existsInBackup)
            {
                if (File.Exists(fileInBackup)) kq.TepHong++;
                try
                {
                    SaoChepAnToan(fileInDB, fileInBackup, false);
                    File.SetAttributes(fileInBackup, FileAttributes.ReadOnly);
                    kq.TepDuocCuu++;
                    kq.ChiTiet.Add($"[KHÔI PHỤC] '{fileName}' được sao lưu lại từ Database → Backup.");
                }
                catch (Exception ex)
                {
                    kq.ChiTiet.Add($"[LỖI KHÔI PHỤC] Không thể sao lưu '{fileName}' từ Database → Backup: {ex.Message}");
                    Debug.WriteLine($"[TuanTraCoreRepository] Lỗi copy {fileName}: {ex.Message}");
                }
                return;
            }
            // TH3: CẢ HAI ĐỀU MẤT/HỎNG
            if (!existsInDB && !existsInBackup)
            {
                if (File.Exists(fileInDB)) kq.TepHong++;
                if (File.Exists(fileInBackup)) kq.TepHong++;
                kq.CanhBao++;
                kq.ChiTiet.Add($"[CẢNH BÁO ĐỎ] Tệp cốt lõi '{fileName}' không tồn tại hoặc đã bị hỏng ở cả 2 vùng.");
                Debug.WriteLine($"[TuanTraCoreRepository] CẢNH BÁO ĐỎ: {fileName}");
                return;
            }
            // TH4: CẢ HAI ĐỀU TỒN TẠI → KIỂM TRA TOÀN VẸN
            try
            {
                var infoDB = new FileInfo(fileInDB);
                var infoBackup = new FileInfo(fileInBackup);
                // 4A. Kích thước khác nhau — không tự ý ghi đè vì chưa biết bên nào đúng
                if (infoDB.Length != infoBackup.Length)
                {
                    kq.ChiTiet.Add(
                        $"[CẢNH BÁO TOÀN VẸN] '{fileName}' có kích thước khác nhau " +
                        $"giữa Database ({infoDB.Length:N0} byte) và Backup ({infoBackup.Length:N0} byte).");
                    kq.CanhBao++;
                    return;
                }
                // 4B. Cùng kích thước → so sánh SHA-256
                string hashDB = TinhSHA256(fileInDB);
                string hashBackup = TinhSHA256(fileInBackup);
                if (string.IsNullOrEmpty(hashDB) || string.IsNullOrEmpty(hashBackup))
                {
                    kq.CanhBao++;
                    kq.ChiTiet.Add($"[CẢNH BÁO TOÀN VẸN] Không thể xác định SHA-256 của '{fileName}'.");
                    return;
                }
                // 4C. Hash giống → toàn vẹn
                if (string.Equals(hashDB, hashBackup, StringComparison.OrdinalIgnoreCase)) return;
                // 4D. Hash khác → dữ liệu đã lệch
                kq.CanhBao++;
                kq.ChiTiet.Add(
                    $"[CẢNH BÁO TOÀN VẸN] Hash SHA-256 của '{fileName}' khác nhau giữa Database và Backup. " +
                    $"Hệ thống KHÔNG tự ý ghi đè để tránh mất dữ liệu.");
                Debug.WriteLine($"[TuanTraCoreRepository] Hash không khớp: {fileName}");
            }
            catch (Exception ex)
            {
                kq.CanhBao++;
                kq.ChiTiet.Add($"[LỖI KIỂM TRA TOÀN VẸN] '{fileName}': {ex.Message}");
                Debug.WriteLine($"[TuanTraCoreRepository] Lỗi integrity {fileName}: {ex}");
            }
        }
        private static void GhiNhatKyTuanTra(KetQuaTuanTra kq)
        {
            if (kq.TepBiXoa == 0 && kq.TepDuocCuu == 0 && kq.TepHong == 0 && kq.CanhBao == 0) return;
            string ghiChu =
                $"Hệ thống đã tiêu diệt {kq.TepBiXoa} tệp lạ, " +
                $"khôi phục {kq.TepDuocCuu} tệp, " +
                $"phát hiện {kq.TepHong} tệp có dấu hiệu không hợp lệ " +
                $"và {kq.CanhBao} cảnh báo toàn vẹn.\r\n" +
                $"Chi tiết:\r\n" +
                string.Join("\r\n", kq.ChiTiet);
            try
            {
                Module_NhatKy.GhiNhatKy(TEN_TAI_KHOAN_HE_THONG, "Bảo vệ Core Repository (Tự động)", ghiChu);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TuanTraCoreRepository] Không thể ghi nhật ký: {ex.Message}");
            }
        }
    }
}