using System.Reflection;
namespace PhanMemThiDua2026
{
    internal static class Module_PhienBan
    {
        // ================= VERSION =================
        // Đọc từ FileVersion của exe (khai báo DUY NHẤT trong PhanMemThiDua2026.csproj). Trước đây là hằng số gõ tay
        // nên lệch với thông tin tệp (giao diện 1.2.336.0, Properties của exe 1.2.323.0).
        public static readonly string SoftwareVersion =
            typeof(Module_PhienBan).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
            ?? typeof(Module_PhienBan).Assembly.GetName().Version?.ToString()
            ?? "1.2.352.0";
        // ================= DATE (GIỮ NGUYÊN CHUỖI GỐC) =================
        public const string NgayThangNamHeThong = "22/11/2026";
        public const string NgayThangNamCapNhat = "Cập nhật lần cuối ngày " + NgayThangNamHeThong;
        public const string NguoiPhatTrienPhanMem = "TrungKien";
        public const string LienHe = "0975 2879 73";
        public const string TenPhanMem = "PhanMemThiDua2026";
        public const string NgayThangNam = NgayThangNamHeThong;
        // ================= BỔ SUNG (KHÔNG ẢNH HƯỞNG CODE CŨ) =================
        public static string GetFullVersion()
        {
            return SoftwareVersion;
        }
        //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
        public static int GetBuildNumber()
        {
            if (Version.TryParse(SoftwareVersion, out var ver))
            {
                return ver.Build;
            }
            return -1;
        }
    }
}