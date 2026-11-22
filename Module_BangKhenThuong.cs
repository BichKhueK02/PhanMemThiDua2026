using Microsoft.Data.Sqlite;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Lược đồ DUY NHẤT của 2 bảng khen thưởng cá nhân trong CSDL4 (khớp cột Module_SaoLuuKhoiPhucKhenThuong sao lưu/khôi phục).
    /// Trước đây mỗi nơi tự CREATE TABLE với bộ cột khác nhau (Form34 thiếu TienThuong/NgayCapPhat/CanBoCapPhat) -> bảng nào
    /// được tạo trước sẽ làm nơi kia lỗi "no such column". Giờ mọi nơi gọi DamBao: chưa có bảng thì tạo đủ cột,
    /// bảng cũ thiếu cột thì bổ sung. Không đụng dữ liệu đã có.
    /// </summary>
    internal static class Module_BangKhenThuong
    {
        private const string TaoBangTong = @"CREATE TABLE IF NOT EXISTS ThongKeCBCS_DuocKhenThuong (
            ID INTEGER PRIMARY KEY AUTOINCREMENT, HoVaTen TEXT, SoHieu TEXT, DonVi TEXT, TinhTrang TEXT,
            SoLuong_Khen TEXT, GhiChu_Khen TEXT);";
        private const string TaoBangGiayKhen = @"CREATE TABLE IF NOT EXISTS ThongKe_GiayKhen (
            ID INTEGER PRIMARY KEY AUTOINCREMENT, HoVaTen TEXT, SoHieu TEXT, DonVi TEXT, TinhTrang TEXT,
            HinhThuc_Khen TEXT, QuyetDinh_Khen TEXT, NgayCapQD_Khen TEXT, DonVi_Khen TEXT, VeViec_Khen TEXT,
            TienThuong TEXT, NgayCapPhat TEXT, CanBoCapPhat TEXT, GhiChu_Khen TEXT);";
        private static readonly string[] CotBangTong = { "HoVaTen", "SoHieu", "DonVi", "TinhTrang", "SoLuong_Khen", "GhiChu_Khen" };
        private static readonly string[] CotGiayKhen = { "HoVaTen", "SoHieu", "DonVi", "TinhTrang", "HinhThuc_Khen", "QuyetDinh_Khen",
            "NgayCapQD_Khen", "DonVi_Khen", "VeViec_Khen", "TienThuong", "NgayCapPhat", "CanBoCapPhat", "GhiChu_Khen" };
        /// <summary>Tạo/bổ sung cột cho cả 2 bảng. Gọi được trong hoặc ngoài giao dịch; an toàn gọi nhiều lần.</summary>
        public static void DamBao(SqliteConnection conn, SqliteTransaction? tran = null)
        {
            DamBaoMotBang(conn, tran, "ThongKeCBCS_DuocKhenThuong", TaoBangTong, CotBangTong);
            DamBaoMotBang(conn, tran, "ThongKe_GiayKhen", TaoBangGiayKhen, CotGiayKhen);
        }
        /// <summary>Bảng khen thưởng tập thể (cùng lược đồ Form49 tạo). Khôi phục vào máy chưa từng mở Form49 thì tạo trước.</summary>
        public static void DamBaoTapThe(SqliteConnection conn, SqliteTransaction? tran = null)
        {
            const string lenh = @"CREATE TABLE IF NOT EXISTS ""ThongKe_KhenThuongTapThe"" (
                ""ID"" INTEGER, ""STT"" INTEGER, ""TenTapThe"" TEXT, ""HinhThuc_KhenThuong"" TEXT, ""DonVi_CapKhenThuong"" TEXT,
                ""SoQuyetDinh"" TEXT, ""NgayQuyetDinh"" TEXT, ""NguoiKy"" TEXT, ""NoiDung_KhenThuong"" TEXT, ""TienThuong"" TEXT,
                ""NgayCapPhat"" TEXT, ""CanBoCapPhat"" TEXT, ""NguoiDaiDienNhan"" TEXT, ""GhiChu"" TEXT, PRIMARY KEY(""ID""));
                CREATE INDEX IF NOT EXISTS ""idx_kt_tentapthe"" ON ""ThongKe_KhenThuongTapThe"" (""TenTapThe"");";
            using var cmd = new SqliteCommand(lenh, conn, tran);
            cmd.ExecuteNonQuery();
        }
        private static void DamBaoMotBang(SqliteConnection conn, SqliteTransaction? tran, string tenBang, string lenhTao, string[] cacCot)
        {
            using (var cmdTao = new SqliteCommand(lenhTao, conn, tran))
                cmdTao.ExecuteNonQuery();
            var coSan = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var cmdCot = new SqliteCommand($"PRAGMA table_info({tenBang});", conn, tran))
            using (var rd = cmdCot.ExecuteReader())
                while (rd.Read()) coSan.Add(rd.GetString(1));
            foreach (string cot in cacCot)
            {
                if (coSan.Contains(cot)) continue;
                using var cmdThem = new SqliteCommand($"ALTER TABLE {tenBang} ADD COLUMN {cot} TEXT;", conn, tran);
                cmdThem.ExecuteNonQuery();
            }
        }
    }
}
