using System.Diagnostics;
using Microsoft.Data.Sqlite;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Lưu lựa chọn ô "Tự động thu gọn" (thanh menu trái Form2) vào csdl2, bảng CauHinh_TuDongThuGonMenu (ID = 1).
    /// Không theo chế độ CBCS/Tân binh/Học viên (là cài đặt giao diện, dùng chung). Mọi lỗi đều bỏ qua: mất cài đặt chứ không sập.
    /// </summary>
    internal static class Module_TuDongThuGonMenu
    {
        private const string TaoBangSql =
            "CREATE TABLE IF NOT EXISTS CauHinh_TuDongThuGonMenu(ID INTEGER PRIMARY KEY, TuDongThuGon INTEGER NOT NULL DEFAULT 0);";
        /// <summary>Đọc CHỈ ĐỌC (không tạo bảng). Chưa có bảng / lỗi => false (không tự thu gọn, như trước đây).</summary>
        public static bool Doc()
        {
            try
            {
                string duongDan = Module_DanduongGPS.DuongDanCSDL2;
                if (string.IsNullOrWhiteSpace(duongDan) || !File.Exists(duongDan)) return false;
                using var cn = Module_KetNoiCSDL.MoKetNoi(duongDan, chiDoc: true, thoiGianCho: 5);
                using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT TuDongThuGon FROM CauHinh_TuDongThuGonMenu WHERE ID = 1 LIMIT 1;";
                return Convert.ToInt64(cmd.ExecuteScalar() ?? 0L) != 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[TuDongThuGonMenu] Đọc: " + ex.Message);   // vd. CSDL cũ chưa có bảng
                return false;
            }
        }
        /// <summary>Ghi (upsert). Gọi ở luồng nền.</summary>
        public static void Ghi(bool tuDongThuGon)
        {
            try
            {
                string duongDan = Module_DanduongGPS.DuongDanCSDL2;
                if (string.IsNullOrWhiteSpace(duongDan) || !File.Exists(duongDan)) return;
                using var cn = Module_KetNoiCSDL.MoKetNoi(duongDan, khongTaoMoi: true, thoiGianCho: 5);
                using var cmd = cn.CreateCommand();
                cmd.CommandText = TaoBangSql +
                    "INSERT INTO CauHinh_TuDongThuGonMenu (ID, TuDongThuGon) VALUES (1, @v) " +
                    "ON CONFLICT(ID) DO UPDATE SET TuDongThuGon = excluded.TuDongThuGon;";
                cmd.Parameters.Add("@v", SqliteType.Integer).Value = tuDongThuGon ? 1 : 0;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { Debug.WriteLine("[TuDongThuGonMenu] Ghi: " + ex.Message); }
        }
    }
}
