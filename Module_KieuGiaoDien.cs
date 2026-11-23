using System.Diagnostics;
using Microsoft.Data.Sqlite;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Kiểu giao diện của khung chính Form2: "Hiện đại" (Module_SidebarHienDai) hoặc "Cổ điển" (Module_SidebarCoDien).
    /// Người dùng chọn ở Form12 > Tab 2 "Thiết lập thông số phần mềm" > mục 10; giá trị lưu ở csdl2.db, bảng KieuGiaoDien_HeThong (ID = 1).
    /// Form2 đọc 1 lần lúc dựng (constructor) => đổi kiểu sẽ có hiệu lực khi mở lại phần mềm.
    /// Mọi lỗi đọc/ghi đều không ném ra ngoài: đọc lỗi => dùng mặc định "Hiện đại".
    /// </summary>
    internal static class Module_KieuGiaoDien
    {
        public const string HienDai = "Hiện đại";
        public const string CoDien = "Cổ điển";
        public static readonly string[] DanhSach = { HienDai, CoDien };
        public const string MacDinh = HienDai;
        private const string SqlTaoBang = @"
            CREATE TABLE IF NOT EXISTS KieuGiaoDien_HeThong(ID INTEGER PRIMARY KEY, KieuGiaoDien TEXT);
            INSERT OR IGNORE INTO KieuGiaoDien_HeThong (ID, KieuGiaoDien) VALUES (1, 'Hiện đại');";
        /// <summary>Kiểu mà Form2 đang dùng (đặt lúc Form2 dựng). null = Form2 chưa dựng.</summary>
        public static string? DangApDung { get; set; }
        /// <summary>Chuẩn hóa giá trị đọc từ CSDL / giao diện về 1 trong 2 kiểu hợp lệ.</summary>
        public static string ChuanHoa(string? giaTri)
        {
            string s = (giaTri ?? "").Trim();
            return string.Equals(s, CoDien, StringComparison.OrdinalIgnoreCase) ? CoDien : HienDai;
        }
        public static bool LaCoDien(string? giaTri) => ChuanHoa(giaTri) == CoDien;
        /// <summary>Tạo bảng (nếu chưa có) + dòng mặc định ID = 1 trên kết nối đang mở (có thể kèm giao dịch).</summary>
        public static void TaoBang(SqliteConnection cn, SqliteTransaction? tran = null)
        {
            using var cmd = cn.CreateCommand();
            cmd.Transaction = tran;
            cmd.CommandText = SqlTaoBang;
            cmd.ExecuteNonQuery();
        }
        /// <summary>Đọc trên kết nối đang mở (bảng phải có sẵn hoặc sẽ được tạo).</summary>
        public static string Doc(SqliteConnection cn)
        {
            TaoBang(cn);
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT KieuGiaoDien FROM KieuGiaoDien_HeThong WHERE ID = 1 LIMIT 1;";
            return ChuanHoa(cmd.ExecuteScalar()?.ToString());
        }
        /// <summary>Tự mở csdl2 và đọc. Không ném lỗi; lỗi => "Hiện đại".</summary>
        public static string DocTuCSDL2()
        {
            try
            {
                string duongDan = Module_DanduongGPS.DuongDanCSDL2;
                if (string.IsNullOrWhiteSpace(duongDan) || !File.Exists(duongDan)) return MacDinh;
                using var cn = Module_KetNoiCSDL.MoKetNoi(duongDan, khongTaoMoi: true, thoiGianCho: 5);
                return Doc(cn);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[KieuGiaoDien] Đọc: " + ex.Message);
                return MacDinh;
            }
        }
        /// <summary>
        /// Đọc CHỈ ĐỌC (không tạo bảng, không ghi gì) — dùng lúc khởi động màn hình đăng nhập.
        /// Chưa có bảng / lỗi => "Hiện đại".
        /// </summary>
        public static string DocChiDoc()
        {
            try
            {
                string duongDan = Module_DanduongGPS.DuongDanCSDL2;
                if (string.IsNullOrWhiteSpace(duongDan) || !File.Exists(duongDan)) return MacDinh;
                using var cn = Module_KetNoiCSDL.MoKetNoi(duongDan, chiDoc: true, thoiGianCho: 5);
                using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT KieuGiaoDien FROM KieuGiaoDien_HeThong WHERE ID = 1 LIMIT 1;";
                return ChuanHoa(cmd.ExecuteScalar()?.ToString());
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[KieuGiaoDien] Đọc chỉ đọc: " + ex.Message);   // vd. chưa có bảng
                return MacDinh;
            }
        }
        /// <summary>Ghi giá trị (upsert) trong giao dịch có sẵn của Form12.</summary>
        public static void Ghi(SqliteConnection cn, SqliteTransaction? tran, string? kieu)
        {
            TaoBang(cn, tran);
            using var cmd = cn.CreateCommand();
            cmd.Transaction = tran;
            cmd.CommandText = "INSERT INTO KieuGiaoDien_HeThong (ID, KieuGiaoDien) VALUES (1, @kieu) " +
                              "ON CONFLICT(ID) DO UPDATE SET KieuGiaoDien = excluded.KieuGiaoDien;";
            cmd.Parameters.AddWithValue("@kieu", ChuanHoa(kieu));
            cmd.ExecuteNonQuery();
        }
    }
}
