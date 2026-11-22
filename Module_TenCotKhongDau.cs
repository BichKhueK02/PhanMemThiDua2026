using System.Diagnostics;
using Microsoft.Data.Sqlite;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Tên bảng / tên cột trong CSDL chỉ dùng chữ KHÔNG DẤU.
    ///
    /// Bản cũ có 2 cột tên có dấu: "SuKien_DuọcChon" (csdl2, bảng SuKien_ThoatPhanMem) và "Chọn_GiaiTri"
    /// (csdl3, bảng TuDong_XoaNhatKy; bản cũ hơn nữa của Form10 còn tạo tên sai "Chon_GiaTri").
    /// CSDL cũ vẫn có thể quay lại qua: bản dự phòng gốc (Database Backup / window-x64), bản sao lưu (Bansaoluu),
    /// dịch vụ khôi phục, chuyển giao dữ liệu => mỗi lần nạp đường dẫn CSDL và mỗi lần đảm bảo bảng đều đổi tên về
    /// tên mới. Đổi tên bằng ALTER TABLE ... RENAME COLUMN nên giữ nguyên dữ liệu.
    /// </summary>
    internal static class Module_TenCotKhongDau
    {
        public const string BANG_SU_KIEN_THOAT = "SuKien_ThoatPhanMem";
        public const string COT_SU_KIEN_THOAT = "SuKien_DuocChon";
        public const string BANG_TU_DONG_XOA = "TuDong_XoaNhatKy";
        public const string COT_TU_DONG_XOA = "Chon_GiaiTri";
        private static readonly string[] TenCuSuKienThoat = { "SuKien_DuọcChon" };
        private static readonly string[] TenCuTuDongXoa = { "Chọn_GiaiTri", "Chon_GiaTri" };
        /// <summary>Gọi sau khi xác định đường dẫn CSDL (khởi động, sau khôi phục). Không bao giờ ném lỗi.</summary>
        public static void ChuanHoaTatCa()
        {
            ChuanHoaTep(Module_DanduongGPS.DuongDanCSDL2, ChuanHoaCSDL2);
            ChuanHoaTep(Module_DanduongGPS.DuongDanCSDL3, ChuanHoaCSDL3);
        }
        /// <summary>csdl2: SuKien_ThoatPhanMem.SuKien_DuọcChon -> SuKien_DuocChon.</summary>
        public static void ChuanHoaCSDL2(SqliteConnection cn) =>
            DoiTenCot(cn, BANG_SU_KIEN_THOAT, COT_SU_KIEN_THOAT, TenCuSuKienThoat);
        /// <summary>csdl3: TuDong_XoaNhatKy.Chọn_GiaiTri / Chon_GiaTri -> Chon_GiaiTri.</summary>
        public static void ChuanHoaCSDL3(SqliteConnection cn) =>
            DoiTenCot(cn, BANG_TU_DONG_XOA, COT_TU_DONG_XOA, TenCuTuDongXoa);
        /// <summary>
        /// Tên cột cũ (có dấu) trong dữ liệu đưa vào từ ngoài (tệp JSON chuyển giao xuất từ CSDL cũ) -> tên cột hiện tại.
        /// Không phải tên cũ thì trả lại nguyên tên.
        /// </summary>
        public static string TenCotHienTai(string tenBang, string tenCot)
        {
            if (string.Equals(tenBang, BANG_SU_KIEN_THOAT, StringComparison.OrdinalIgnoreCase)
                && Array.IndexOf(TenCuSuKienThoat, tenCot) >= 0)
                return COT_SU_KIEN_THOAT;
            if (string.Equals(tenBang, BANG_TU_DONG_XOA, StringComparison.OrdinalIgnoreCase)
                && Array.IndexOf(TenCuTuDongXoa, tenCot) >= 0)
                return COT_TU_DONG_XOA;
            return tenCot;
        }
        private static void ChuanHoaTep(string? duongDan, Action<SqliteConnection> chuanHoa)
        {
            if (string.IsNullOrWhiteSpace(duongDan) || !File.Exists(duongDan)) return;
            try
            {
                // Pooling=false: đóng là nhả hẳn tệp (sao lưu / khôi phục / di chuyển tệp ngay sau đó không bị khóa)
                using var cn = Module_KetNoiCSDL.MoKetNoi(duongDan, khongTaoMoi: true, pooling: false, thoiGianCho: 5);
                chuanHoa(cn);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TenCotKhongDau] {Path.GetFileName(duongDan)}: {ex.Message}");
            }
        }
        private static void DoiTenCot(SqliteConnection cn, string bang, string tenMoi, string[] tenCu)
        {
            var cot = new List<string>();
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "SELECT name FROM pragma_table_info(@bang);";
                cmd.Parameters.AddWithValue("@bang", bang);
                using var rd = cmd.ExecuteReader();
                while (rd.Read()) cot.Add(rd.GetString(0));
            }
            if (cot.Count == 0) return; // chưa có bảng: nơi tạo bảng sẽ tạo luôn tên mới
            // So khớp như SQLite: không phân biệt hoa/thường với chữ ASCII
            bool coTenMoi = cot.Contains(tenMoi, StringComparer.OrdinalIgnoreCase);
            foreach (string cu in tenCu)
            {
                if (!cot.Contains(cu, StringComparer.Ordinal)) continue;
                using var cmd = cn.CreateCommand();
                if (!coTenMoi)
                {
                    cmd.CommandText = $"ALTER TABLE {Q(bang)} RENAME COLUMN {Q(cu)} TO {Q(tenMoi)};";
                    cmd.ExecuteNonQuery();
                    coTenMoi = true;
                }
                else
                {
                    // Cả tên cũ lẫn tên mới cùng tồn tại: giữ giá trị cũ cho dòng chưa có giá trị mới rồi bỏ cột cũ
                    using var tx = cn.BeginTransaction();
                    cmd.Transaction = tx;
                    cmd.CommandText = $"UPDATE {Q(bang)} SET {Q(tenMoi)} = {Q(cu)} WHERE {Q(tenMoi)} IS NULL;";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = $"ALTER TABLE {Q(bang)} DROP COLUMN {Q(cu)};";
                    cmd.ExecuteNonQuery();
                    tx.Commit();
                }
            }
        }
        private static string Q(string ten) => "\"" + ten.Replace("\"", "\"\"") + "\"";
    }
}
