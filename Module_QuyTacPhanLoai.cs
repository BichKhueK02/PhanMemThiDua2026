namespace PhanMemThiDua2026
{
    /// <summary>
    /// Quy tắc nghiệp vụ phân loại thi đua, TÁCH KHỎI FORM để dùng chung và kiểm thử tự động được
    /// (không phụ thuộc giao diện, CSDL hay khóa mã hóa: chỉ nhận chuỗi, trả chuỗi).
    /// </summary>
    internal static class Module_QuyTacPhanLoai
    {
        /// <summary>
        /// Phân loại THÁNG từ kết quả 4 tuần (chuyển nguyên văn từ Form36.TinhLogicPhanLoaiMDK_Fast).
        /// Mỗi ô tuần lấy CHỮ SỐ ĐẦU TIÊN xuất hiện ("Loại 2" -> 2); ô rỗng / không có số 1-4 bị bỏ qua.
        /// <list type="bullet">
        /// <item>Không tuần nào hợp lệ                         => Loại 4</item>
        /// <item>≥ 2 tuần Loại 1, không có Loại 3 / Loại 4      => Loại 1</item>
        /// <item>≥ 2 tuần Loại 1-2, không có Loại 3 / Loại 4    => Loại 2</item>
        /// <item>≥ 2 tuần Loại 1-3, không có Loại 4             => Loại 3</item>
        /// <item>Còn lại                                        => Loại 4</item>
        /// </list>
        /// Chạy cho từng dòng của bảng lớn: không cấp phát bộ nhớ (không LINQ, không closure).
        /// </summary>
        public static string PhanLoaiThangTu4Tuan(string? tuan1, string? tuan2, string? tuan3, string? tuan4)
        {
            int l1 = 0, l2 = 0, l3 = 0, l4 = 0;
            Dem(tuan1, ref l1, ref l2, ref l3, ref l4);
            Dem(tuan2, ref l1, ref l2, ref l3, ref l4);
            Dem(tuan3, ref l1, ref l2, ref l3, ref l4);
            Dem(tuan4, ref l1, ref l2, ref l3, ref l4);
            int hopLe = l1 + l2 + l3 + l4;
            if (hopLe == 0) return Module_HeThong.Loai_4;
            if (l1 >= 2 && l3 == 0 && l4 == 0) return Module_HeThong.Loai_1;
            if ((l1 + l2) >= 2 && l3 == 0 && l4 == 0) return Module_HeThong.Loai_2;
            if ((l1 + l2 + l3) >= 2 && l4 == 0) return Module_HeThong.Loai_3;
            return Module_HeThong.Loai_4;
        }

        /// <summary>
        /// Lấy chữ số ĐẦU TIÊN (char.IsDigit, như bản gốc dùng FirstOrDefault(char.IsDigit)) và cộng vào bộ đếm tương ứng.
        /// Chữ số đầu tiên không thuộc 1-4 (vd "0", "5") => ô đó không được tính, giống bản gốc.
        /// </summary>
        private static void Dem(string? s, ref int l1, ref int l2, ref int l3, ref int l4)
        {
            if (string.IsNullOrWhiteSpace(s)) return;
            foreach (char c in s)
            {
                if (!char.IsDigit(c)) continue;
                switch (c)
                {
                    case '1': l1++; break;
                    case '2': l2++; break;
                    case '3': l3++; break;
                    case '4': l4++; break;
                }
                return;   // chỉ xét chữ số đầu tiên
            }
        }
    }
}
