namespace PhanMemThiDua2026
{
    internal static class Module_PhienBan
    {
        // ================= VERSION =================
        public const string SoftwareVersion = "1.2.336.0";
        // ================= DATE (GIỮ NGUYÊN CHUỖI GỐC) =================
        public const string NgayThangNamHeThong = "12/11/2026";
        public const string NgayThangNamCapNhat = "Cập nhật lần cuối ngày " + NgayThangNamHeThong;
        public const string NguoiPhatTrienPhanMem = "LeTrungKien và Nhóm phát triển";
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