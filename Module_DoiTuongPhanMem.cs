namespace PhanMemThiDua2026
{
    /// <summary>
    /// Nhận diện chế độ phần mềm từ chuỗi phiên bản (PhienBan_DoiTuong).
    /// Chế độ "Học viên" dùng chung toàn bộ logic với chế độ "Tân binh" (chỉ khác tên hiển thị),
    /// nên mọi chỗ kiểm tra "có phải Tân binh" đều phải dùng LaCheDoTanBinh() thay vì Contains("tân binh").
    /// </summary>
    internal static class Module_DoiTuongPhanMem
    {
        /// <summary>Chuỗi phiên bản thuộc nhóm Tân binh (gồm cả Học viên).</summary>
        public static bool LaCheDoTanBinh(this string? phienBan)
            => !string.IsNullOrEmpty(phienBan) &&
               (phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase) ||
                phienBan.Contains("học viên", StringComparison.OrdinalIgnoreCase));
        /// <summary>Chuỗi phiên bản là Học viên.</summary>
        public static bool LaCheDoHocVien(this string? phienBan)
            => !string.IsNullOrEmpty(phienBan) &&
               phienBan.Contains("học viên", StringComparison.OrdinalIgnoreCase);
    }
}
