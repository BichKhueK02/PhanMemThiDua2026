namespace PhanMemThiDua2026
{
    /// <summary>
    /// Nhận diện chế độ phần mềm từ chuỗi phiên bản (PhienBan_DoiTuong).
    /// "Học viên" dùng chung LOGIC với "Tân binh" (xét theo tuần) nhưng có BỘ BẢNG RIÊNG (_HocVien).
    /// LaCheDoTanBinh() chỉ để rẽ nhánh logic; tên bảng luôn lấy qua Module_CheDoDoiTuong.
    /// </summary>
    internal static class Module_DoiTuongPhanMem
    {
        /// <summary>Chế độ (CBCS / Tân binh / Học viên) của chuỗi phiên bản.</summary>
        public static CheDoDoiTuong LayCheDo(this string? phienBan) => Module_CheDoDoiTuong.TuChuoi(phienBan);
        /// <summary>Chuỗi phiên bản thuộc nhóm xét theo tuần (Tân binh HOẶC Học viên).</summary>
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
