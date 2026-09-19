using System.Globalization;
namespace PhanMemThiDua2026
{
    internal static class Module_PhienBan
    {
        // ================= VERSION =================
        public const string SoftwareVersion = "1.1.162.0";
        // ================= DATE (GIỮ NGUYÊN CHUỖI GỐC) =================
        public const string NgayThangNamHeThong = "01/10/2026";
        public const string NgayThangNamCapNhat = "Cập nhật lần cuối ngày " + NgayThangNamHeThong;
        public const string NguoiPhatTrienPhanMem = "LeTrungKien và Nhóm phát triển";
        public const string TenPhanMem = "PhanMemThiDua2026";
        public const string NgayThangNam = NgayThangNamHeThong;
        // ================= PARSED DATE (THÊM MỚI - KHÔNG PHÁ CODE CŨ) =================
        public static readonly DateTime? NgayHeThong_DateTime = ParseDate(NgayThangNamHeThong);
        private static DateTime? ParseDate(string input)
        {
            if (DateTime.TryParseExact(
                input,
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var result))
            {
                return result;
            }
            return null; // không throw để tránh crash hệ thống
        }
        // ================= VERSION HELPERS =================
        public static string GetSoftwareVersion()
        {
            // Giữ logic cũ: trả về Build
            if (Version.TryParse(SoftwareVersion, out var ver))
            {
                return ver.Build.ToString();
            }
            return SoftwareVersion; // fallback an toàn
        }
        // ================= BỔ SUNG (KHÔNG ẢNH HƯỞNG CODE CŨ) =================
        public static string GetFullVersion()
        {
            return SoftwareVersion;
        }
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