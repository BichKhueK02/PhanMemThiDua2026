using Microsoft.Data.Sqlite;
using System.Diagnostics;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Lược đồ DUY NHẤT của 3 bảng khen thưởng (CSDL4 + tệp năm cũ KhenThuong_*_Nam*.db), cho CẢ 3 CHẾ ĐỘ:
    ///   người được khen : ThongKe_DuocKhenThuong_CBCS / _TanBinh / _HocVien
    ///   giấy khen       : ThongKe_GiayKhen_CBCS / _TanBinh / _HocVien
    ///   tập thể         : ThongKe_KhenThuongTapThe_CBCS / _TanBinh / _HocVien
    /// (tên bảng: Module_CheDoDoiTuong). Khớp cột Module_SaoLuuKhoiPhucKhenThuong sao lưu/khôi phục.
    /// Mọi nơi gọi DamBao: chưa có bảng thì tạo đủ cột, bảng cũ (CSDL phát hành trước) thiếu cột thì bổ sung.
    /// Không bao giờ đụng dữ liệu đã có. Không truyền chế độ => chế độ đang chạy.
    /// </summary>
    internal static class Module_BangKhenThuong
    {
        private static readonly string[] CotBangTong = { "HoVaTen", "SoHieu", "DonVi", "TinhTrang", "SoLuong_Khen", "GhiChu_Khen" };
        private static readonly string[] CotGiayKhen = { "HoVaTen", "SoHieu", "DonVi", "TinhTrang", "HinhThuc_Khen", "QuyetDinh_Khen",
            "NgayCapQD_Khen", "DonVi_Khen", "VeViec_Khen", "TienThuong", "NgayCapPhat", "CanBoCapPhat", "GhiChu_Khen" };
        private static readonly string[] CotTapThe = { "STT", "TenTapThe", "HinhThuc_KhenThuong", "DonVi_CapKhenThuong", "SoQuyetDinh",
            "NgayQuyetDinh", "NguoiKy", "NoiDung_KhenThuong", "TienThuong", "NgayCapPhat", "CanBoCapPhat", "NguoiDaiDienNhan", "GhiChu" };

        // Cùng cấu trúc với bảng CBCS đang có trong CSDL mẫu (ID INTEGER NOT NULL, PRIMARY KEY AUTOINCREMENT)
        private static string LenhTaoBangTong(string ten) => $@"CREATE TABLE IF NOT EXISTS ""{ten}"" (
            ""ID"" INTEGER NOT NULL, ""HoVaTen"" TEXT, ""SoHieu"" TEXT, ""DonVi"" TEXT, ""TinhTrang"" TEXT,
            ""SoLuong_Khen"" TEXT, ""GhiChu_Khen"" TEXT, PRIMARY KEY(""ID"" AUTOINCREMENT));";
        private static string LenhTaoGiayKhen(string ten) => $@"CREATE TABLE IF NOT EXISTS ""{ten}"" (
            ""ID"" INTEGER NOT NULL, ""HoVaTen"" TEXT, ""SoHieu"" TEXT, ""DonVi"" TEXT, ""TinhTrang"" TEXT,
            ""HinhThuc_Khen"" TEXT, ""QuyetDinh_Khen"" TEXT, ""NgayCapQD_Khen"" TEXT, ""DonVi_Khen"" TEXT, ""VeViec_Khen"" TEXT,
            ""TienThuong"" TEXT, ""NgayCapPhat"" TEXT, ""CanBoCapPhat"" TEXT, ""GhiChu_Khen"" TEXT, PRIMARY KEY(""ID"" AUTOINCREMENT));";
        // Tập thể: giữ đúng lược đồ Form49 xưa nay vẫn tạo (ID INTEGER PRIMARY KEY - là rowid nên tự tăng khi chèn NULL)
        private static string LenhTaoTapThe(string ten, string chiMuc) => $@"CREATE TABLE IF NOT EXISTS ""{ten}"" (
            ""ID"" INTEGER, ""STT"" INTEGER, ""TenTapThe"" TEXT, ""HinhThuc_KhenThuong"" TEXT, ""DonVi_CapKhenThuong"" TEXT,
            ""SoQuyetDinh"" TEXT, ""NgayQuyetDinh"" TEXT, ""NguoiKy"" TEXT, ""NoiDung_KhenThuong"" TEXT, ""TienThuong"" TEXT,
            ""NgayCapPhat"" TEXT, ""CanBoCapPhat"" TEXT, ""NguoiDaiDienNhan"" TEXT, ""GhiChu"" TEXT, PRIMARY KEY(""ID""));
            CREATE INDEX IF NOT EXISTS ""{chiMuc}"" ON ""{ten}"" (""TenTapThe"");";

        /// <summary>Tạo/bổ sung cột cho 2 bảng cá nhân (người + giấy khen). Gọi được trong hoặc ngoài giao dịch; an toàn gọi nhiều lần.</summary>
        public static void DamBao(SqliteConnection conn, SqliteTransaction? tran = null) => DamBao(conn, tran, Module_CheDoDoiTuong.Hien);
        public static void DamBao(SqliteConnection conn, SqliteTransaction? tran, CheDoDoiTuong c)
        {
            string tong = Module_CheDoDoiTuong.KhenThuongNguoi(c), gk = Module_CheDoDoiTuong.GiayKhen(c);
            DamBaoMotBang(conn, tran, tong, LenhTaoBangTong(tong), CotBangTong);
            DamBaoMotBang(conn, tran, gk, LenhTaoGiayKhen(gk), CotGiayKhen);
        }
        /// <summary>Bảng khen thưởng tập thể (+ chỉ mục tên tập thể). Bảng cũ thiếu cột thì bổ sung; ID NULL (lỗi chèn cũ) thì vá = rowid.</summary>
        public static void DamBaoTapThe(SqliteConnection conn, SqliteTransaction? tran = null) => DamBaoTapThe(conn, tran, Module_CheDoDoiTuong.Hien);
        public static void DamBaoTapThe(SqliteConnection conn, SqliteTransaction? tran, CheDoDoiTuong c)
        {
            string ten = Module_CheDoDoiTuong.KhenThuongTapThe(c);
            DamBaoMotBang(conn, tran, ten, LenhTaoTapThe(ten, Module_CheDoDoiTuong.ChiMucTapThe(c)), CotTapThe);
            using var vaId = new SqliteCommand($@"UPDATE ""{ten}"" SET ID = rowid WHERE ID IS NULL;", conn, tran);
            vaId.ExecuteNonQuery();
        }
        /// <summary>Cả 3 bảng khen thưởng của chế độ <paramref name="c"/>.</summary>
        public static void DamBaoTatCa(SqliteConnection conn, SqliteTransaction? tran, CheDoDoiTuong c)
        {
            DamBao(conn, tran, c);
            DamBaoTapThe(conn, tran, c);
        }

        private static readonly HashSet<string> _daDamBaoTep = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Bảo đảm đủ 3 bảng khen thưởng của chế độ trong tệp CSDL4 (CSDL phát hành trước chưa có bảng Tân binh/Học viên,
        /// hoặc bảng CBCS thiếu cột). Mỗi tệp + chế độ chỉ chạy 1 lần mỗi phiên; lỗi (CSDL đang khóa) thì lần sau thử lại.
        /// </summary>
        public static void DamBaoTrongTep(string? csdl4, CheDoDoiTuong c)
        {
            if (string.IsNullOrWhiteSpace(csdl4) || !File.Exists(csdl4)) return;
            string khoa = c + "|" + csdl4;
            lock (_daDamBaoTep)
            {
                if (_daDamBaoTep.Contains(khoa)) return;
                try
                {
                    using var cn = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = csdl4, DefaultTimeout = 10 }.ToString());
                    cn.Open();
                    using var tran = cn.BeginTransaction();
                    DamBaoTatCa(cn, tran, c);
                    tran.Commit();
                    _daDamBaoTep.Add(khoa);
                }
                catch (Exception ex) { Debug.WriteLine($"[Module_BangKhenThuong.DamBaoTrongTep {c}] " + ex.Message); }
            }
        }
        /// <summary>Quên trạng thái "đã bảo đảm" (khôi phục/chuyển giao ghi đè tệp CSDL).</summary>
        public static void XoaBoDem() { lock (_daDamBaoTep) _daDamBaoTep.Clear(); }

        private static void DamBaoMotBang(SqliteConnection conn, SqliteTransaction? tran, string tenBang, string lenhTao, string[] cacCot)
        {
            using (var cmdTao = new SqliteCommand(lenhTao, conn, tran))
                cmdTao.ExecuteNonQuery();
            var coSan = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var cmdCot = new SqliteCommand($@"PRAGMA table_info(""{tenBang}"");", conn, tran))
            using (var rd = cmdCot.ExecuteReader())
                while (rd.Read()) coSan.Add(rd.GetString(1));
            foreach (string cot in cacCot)
            {
                if (coSan.Contains(cot)) continue;
                string kieu = cot == "STT" ? "INTEGER" : "TEXT";
                using var cmdThem = new SqliteCommand($@"ALTER TABLE ""{tenBang}"" ADD COLUMN ""{cot}"" {kieu};", conn, tran);
                cmdThem.ExecuteNonQuery();
            }
        }
    }
}
