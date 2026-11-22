///Phiên bản 1 – nơi DUY NHẤT phần mềm ghi tệp nhật ký kỹ thuật / tệp tạm (không ghi vào %APPDATA%, %LOCALAPPDATA%, %TEMP%)
///
using System.Diagnostics;
using System.Text;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Thư mục nhật ký nhà phát triển: &lt;thư mục phần mềm&gt;\Database\NhatKyNhaPhatTrien
    ///   • *.log        : nhật ký lỗi dự phòng (error-*), đo hiệu năng đăng nhập (dangnhap-*)...
    ///   • TepTam\...   : thư mục làm việc tạm (xuất PDF...), tự xóa khi xong, sót lại thì dọn ở lần khởi động sau.
    /// Lý do: phần mềm diệt virus hay chấm điểm "đáng ngờ" cho ứng dụng tự ghi tệp vào AppData/Temp;
    /// gom mọi thứ về thư mục của chính phần mềm vừa an toàn, vừa dễ chẩn đoán (chép nguyên thư mục là đủ).
    /// LƯU Ý: TEN_THU_MUC PHẢI có trong danh sách trắng AllowDirsDatabase (Module_KhoiTaoCSDL), nếu không chính sách làm sạch sẽ xóa.
    /// Module này KHÔNG phụ thuộc module khác (an toàn để gọi từ bẫy lỗi toàn cục, kể cả khi CSDL/khóa chưa nạp được).
    /// </summary>
    internal static class Module_NhatKyNhaPhatTrien
    {
        public const string TEN_THU_MUC = "NhatKyNhaPhatTrien";
        private const string TEN_THU_MUC_TAM = "TepTam";
        private const long KICH_THUOC_TOI_DA_MOI_TEP = 5L * 1024 * 1024;   // 5 MB/tệp/ngày: chặn "bão lỗi" làm đầy ổ đĩa
        private static readonly TimeSpan TuoiToiDaTepTam = TimeSpan.FromHours(12);
        // Tự tính từ AppContext.BaseDirectory (giống Module_DanduongGPS.ThuMucCoSoDuLieu) để không kích hoạt khởi tạo tĩnh của module khác
        public static string ThuMuc { get; } = Path.Combine(AppContext.BaseDirectory, "Database", TEN_THU_MUC);
        public static string ThuMucTam { get; } = Path.Combine(ThuMuc, TEN_THU_MUC_TAM);
        private static readonly object _khoaGhi = new();
        private static readonly UTF8Encoding Utf8KhongBom = new(false);
        /// <summary>Ghi nguyên văn (đồng bộ) vào &lt;tiền tố&gt;-yyyyMMdd.log. Không bao giờ ném lỗi.</summary>
        public static void GhiTep(string tienTo, string noiDung)
        {
            try
            {
                lock (_khoaGhi)
                {
                    Directory.CreateDirectory(ThuMuc);
                    string file = Path.Combine(ThuMuc, $"{tienTo}-{DateTime.Now:yyyyMMdd}.log");
                    var fi = new FileInfo(file);
                    if (fi.Exists && fi.Length >= KICH_THUOC_TOI_DA_MOI_TEP) return;
                    File.AppendAllText(file, noiDung, Utf8KhongBom);
                }
            }
            catch (Exception ex) { Debug.WriteLine("[NhatKyNhaPhatTrien] Không ghi được: " + ex.Message); }
        }
        /// <summary>Ghi 1 dòng có dấu thời gian.</summary>
        public static void GhiDong(string tienTo, string noiDung) =>
            GhiTep(tienTo, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {noiDung}{Environment.NewLine}");
        /// <summary>Ghi 1 dòng ở luồng nền (dùng cho đo hiệu năng trên luồng giao diện).</summary>
        public static void GhiDongNen(string tienTo, string noiDung) => _ = Task.Run(() => GhiDong(tienTo, noiDung));
        /// <summary>Tạo thư mục làm việc tạm duy nhất trong Database\NhatKyNhaPhatTrien\TepTam (thay cho %TEMP%).</summary>
        public static string TaoThuMucTam(string tienTo)
        {
            string dir = Path.Combine(ThuMucTam, tienTo + "_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }
        /// <summary>Xóa *.log cũ hơn số ngày cho trước và thư mục tạm bị sót (app tắt đột ngột). Chạy ở luồng nền.</summary>
        public static void DonDep(int soNgayGiuNhatKy)
        {
            try
            {
                if (!Directory.Exists(ThuMuc)) return;
                DateTime nguongLog = DateTime.Now.AddDays(-soNgayGiuNhatKy);
                foreach (string file in Directory.EnumerateFiles(ThuMuc, "*.log"))
                {
                    try { if (File.GetLastWriteTime(file) < nguongLog) File.Delete(file); } catch { }
                }
                if (!Directory.Exists(ThuMucTam)) return;
                DateTime nguongTam = DateTime.Now - TuoiToiDaTepTam;
                foreach (string dir in Directory.EnumerateDirectories(ThuMucTam))
                {
                    try
                    {
                        var di = new DirectoryInfo(dir);
                        if ((di.Attributes & FileAttributes.ReparsePoint) != 0) continue;   // không đi theo liên kết ra ngoài
                        if (di.LastWriteTime < nguongTam) di.Delete(true);
                    }
                    catch { /* tệp còn bị khóa: để lần sau */ }
                }
            }
            catch { }
        }
        /// <summary>
        /// Một lần duy nhất: chuyển nhật ký cũ từ %LOCALAPPDATA%\PhanMemThiDua2026 (phiên bản trước) về thư mục mới rồi xóa thư mục cũ,
        /// để máy người dùng không còn dấu vết phần mềm trong AppData. Chạy ở luồng nền, lỗi thì bỏ qua.
        /// </summary>
        public static void ChuyenDuLieuCuTuAppData()
        {
            try
            {
                string cu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhanMemThiDua2026");
                if (!Directory.Exists(cu)) return;
                var diCu = new DirectoryInfo(cu);
                if ((diCu.Attributes & FileAttributes.ReparsePoint) != 0) return;
                string logsCu = Path.Combine(cu, "Logs");
                if (Directory.Exists(logsCu))
                {
                    Directory.CreateDirectory(ThuMuc);
                    foreach (string file in Directory.EnumerateFiles(logsCu, "*.log"))
                    {
                        try
                        {
                            string dich = Path.Combine(ThuMuc, "appdata-cu-" + Path.GetFileName(file));
                            if (!File.Exists(dich)) File.Move(file, dich);
                        }
                        catch { }
                    }
                }
                // Thư mục cũ chỉ chứa Logs + HuongDanSuDung (bản sao đã bỏ từ lâu) -> xóa toàn bộ
                diCu.Delete(true);
            }
            catch (Exception ex) { Debug.WriteLine("[NhatKyNhaPhatTrien] Không dọn được AppData cũ: " + ex.Message); }
        }
    }
}
