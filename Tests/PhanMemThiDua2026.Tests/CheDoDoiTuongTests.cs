using Microsoft.Data.Sqlite;
using PhanMemThiDua2026;
using Xunit;
namespace PhanMemThiDua2026.Tests;
/// <summary>Ba chế độ CBCS / Tân binh / Học viên: tên bảng, tệp năm cũ, mã tạm, tạo bảng (Module_CheDoDoiTuong).</summary>
public sealed class CheDoDoiTuongTests
{
    [Theory]
    [InlineData("Phần mềm: Phiên bản dành cho CBCS", CheDoDoiTuong.CBCS)]
    [InlineData("Phần mềm: Phiên bản dành cho tân binh", CheDoDoiTuong.TanBinh)]
    [InlineData("Phần mềm: Phiên bản dành cho học viên", CheDoDoiTuong.HocVien)]
    [InlineData("Phần mềm dành cho Học viên", CheDoDoiTuong.HocVien)]   // dấu phiên bản trong tệp Excel
    [InlineData("Phần mềm dành cho Tân binh", CheDoDoiTuong.TanBinh)]
    [InlineData("", CheDoDoiTuong.CBCS)]
    [InlineData(null, CheDoDoiTuong.CBCS)]
    public void TuChuoi_NhanDungCheDo(string? chuoi, CheDoDoiTuong mong) => Assert.Equal(mong, Module_CheDoDoiTuong.TuChuoi(chuoi));

    [Fact]
    public void TenBang_BaCheDoKhacNhau_CungMauHauTo()
    {
        Assert.Equal("DanhSach_CBCS", Module_CheDoDoiTuong.DanhSach(CheDoDoiTuong.CBCS));
        Assert.Equal("ThiDuaThang_CBCS", Module_CheDoDoiTuong.ThiDuaThang(CheDoDoiTuong.CBCS));
        Assert.Equal("DanhSach_TanBinh", Module_CheDoDoiTuong.DanhSach(CheDoDoiTuong.TanBinh));
        Assert.Equal("DanhSach_HocVien", Module_CheDoDoiTuong.DanhSach(CheDoDoiTuong.HocVien));
        Assert.Equal("ThiDuaThang_HocVien", Module_CheDoDoiTuong.ThiDuaThang(CheDoDoiTuong.HocVien));
        Assert.Equal("DanhSach_SoVangBaNhat_HocVien", Module_CheDoDoiTuong.SoVangBaNhat(CheDoDoiTuong.HocVien));
        foreach (var f in new Func<CheDoDoiTuong, string>[] {
                     Module_CheDoDoiTuong.DanhSach, Module_CheDoDoiTuong.ChiHuyD, Module_CheDoDoiTuong.DonVi,
                     Module_CheDoDoiTuong.QuyDinhTyLe, Module_CheDoDoiTuong.BaNhat, Module_CheDoDoiTuong.SoVangBaNhat,
                     Module_CheDoDoiTuong.TomTatBaNhat, Module_CheDoDoiTuong.ThiDuaThang })
        {
            var ten = new[] { f(CheDoDoiTuong.CBCS), f(CheDoDoiTuong.TanBinh), f(CheDoDoiTuong.HocVien) };
            Assert.Equal(3, ten.Distinct().Count());   // không chế độ nào dùng chung bảng với chế độ khác
            // Cùng mẫu "TênGốc_HậuTố": bỏ hậu tố thì 3 chế độ ra cùng một tên gốc
            Assert.EndsWith("_CBCS", ten[0]); Assert.EndsWith("_TanBinh", ten[1]); Assert.EndsWith("_HocVien", ten[2]);
            Assert.Single(new[] { ten[0][..^5], ten[1][..^8], ten[2][..^8] }.Distinct());
        }
    }

    [Theory]
    [InlineData("ThiDua_CBCS_Nam2025.db", CheDoDoiTuong.CBCS)]
    [InlineData("ThiDua_TanBinh_Nam2025.db", CheDoDoiTuong.TanBinh)]
    [InlineData(@"C:\x\ThiDua_HocVien_Nam2025.db", CheDoDoiTuong.HocVien)]
    public void TuTenTep_NhanDungCheDo(string tep, CheDoDoiTuong mong)
    {
        Assert.Equal(mong, Module_CheDoDoiTuong.TuTenTep(tep));
        Assert.Contains(Module_CheDoDoiTuong.MaTep(mong), Path.GetFileName(tep));
    }

    [Fact]
    public void MaTam_TienToTheoCheDo()
    {
        Assert.Equal("TB00001", Module_CheDoDoiTuong.TaoMaTam(CheDoDoiTuong.TanBinh, 1));
        Assert.Equal("HV00123", Module_CheDoDoiTuong.TaoMaTam(CheDoDoiTuong.HocVien, 123));
        Assert.Equal("TB00005", Module_CheDoDoiTuong.ChuanHoaMaTam(CheDoDoiTuong.TanBinh, "5"));
        Assert.Equal("HV00005", Module_CheDoDoiTuong.ChuanHoaMaTam(CheDoDoiTuong.HocVien, " hv5 "));
        Assert.Equal("ID00007", Module_CheDoDoiTuong.ChuanHoaMaTam(CheDoDoiTuong.TanBinh, "id00007"));   // mã cũ giữ nguyên
        Assert.True(Module_CheDoDoiTuong.LaMaTam(CheDoDoiTuong.HocVien, "hv00001"));
        Assert.False(Module_CheDoDoiTuong.LaMaTam(CheDoDoiTuong.HocVien, "123456"));
        Assert.False(Module_CheDoDoiTuong.LaMaTam(CheDoDoiTuong.CBCS, "TB00001"));
        Assert.Equal("123456", Module_CheDoDoiTuong.ChuanHoaMaTam(CheDoDoiTuong.CBCS, "123456"));
    }

    private static List<(string Ten, string Kieu)> Cot(SqliteConnection cn, string bang)
    {
        var ds = new List<(string, string)>();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = $"SELECT name, type FROM pragma_table_info('{bang}')";
        using var rd = cmd.ExecuteReader();
        while (rd.Read()) ds.Add((rd.GetString(0), rd.GetString(1)));
        return ds;
    }

    [Fact]
    public void KhenThuong_TenBangTepNamCu_BaCheDoKhacNhau_CungMauHauTo()
    {
        Assert.Equal("ThongKe_DuocKhenThuong_CBCS", Module_CheDoDoiTuong.KhenThuongNguoi(CheDoDoiTuong.CBCS));
        Assert.Equal("ThongKe_DuocKhenThuong_HocVien", Module_CheDoDoiTuong.KhenThuongNguoi(CheDoDoiTuong.HocVien));
        Assert.Equal("ThongKe_GiayKhen_CBCS", Module_CheDoDoiTuong.GiayKhen(CheDoDoiTuong.CBCS));
        Assert.Equal("ThongKe_KhenThuongTapThe_CBCS", Module_CheDoDoiTuong.KhenThuongTapThe(CheDoDoiTuong.CBCS));
        Assert.Equal("idx_kt_tentapthe", Module_CheDoDoiTuong.ChiMucTapThe(CheDoDoiTuong.CBCS));
        Assert.Equal("KhenThuong_CBCS_Nam", Module_CheDoDoiTuong.TienToTepKhenThuong(CheDoDoiTuong.CBCS));
        Assert.Equal("ChonDonVi_DeBoQuaCanhBao_CBCS", Module_CheDoDoiTuong.BoQuaCanhBao(CheDoDoiTuong.CBCS));
        Assert.Equal("ChonDonVi_DeBoQuaCanhBao_HocVien", Module_CheDoDoiTuong.BoQuaCanhBao(CheDoDoiTuong.HocVien));
        Assert.Equal("ThongKe_GiayKhen_HocVien", Module_CheDoDoiTuong.GiayKhen(CheDoDoiTuong.HocVien));
        Assert.Equal("KhenThuong_TanBinh_Nam", Module_CheDoDoiTuong.TienToTepKhenThuong(CheDoDoiTuong.TanBinh));
        Assert.Equal(CheDoDoiTuong.HocVien, Module_CheDoDoiTuong.TuTenTep("KhenThuong_HocVien_Nam2026.db"));
        foreach (var f in new Func<CheDoDoiTuong, string>[] {
                     Module_CheDoDoiTuong.KhenThuongNguoi, Module_CheDoDoiTuong.GiayKhen,
                     Module_CheDoDoiTuong.KhenThuongTapThe, Module_CheDoDoiTuong.ChiMucTapThe, Module_CheDoDoiTuong.TienToTepKhenThuong })
            Assert.Equal(3, new[] { f(CheDoDoiTuong.CBCS), f(CheDoDoiTuong.TanBinh), f(CheDoDoiTuong.HocVien) }.Distinct().Count());
    }

    private static SqliteConnection MoRam(string sql = "")
    {
        var cn = new SqliteConnection("Data Source=:memory:");
        cn.Open();
        if (sql.Length > 0) { using var cmd = cn.CreateCommand(); cmd.CommandText = sql; cmd.ExecuteNonQuery(); }
        return cn;
    }
    private static object? Scalar(SqliteConnection cn, string sql)
    {
        using var cmd = cn.CreateCommand();
        cmd.CommandText = sql;
        return cmd.ExecuteScalar();
    }

    [Fact]
    public void KhenThuong_DamBaoTatCa_BaCheDoCungCauTruc_ChayLaiKhongLoi()
    {
        using var cn = MoRam();
        foreach (var c in new[] { CheDoDoiTuong.CBCS, CheDoDoiTuong.TanBinh, CheDoDoiTuong.HocVien })
        {
            Module_BangKhenThuong.DamBaoTatCa(cn, null, c);
            Module_BangKhenThuong.DamBaoTatCa(cn, null, c);   // gọi lại: an toàn
        }
        foreach (var f in new Func<CheDoDoiTuong, string>[] {
                     Module_CheDoDoiTuong.KhenThuongNguoi, Module_CheDoDoiTuong.GiayKhen, Module_CheDoDoiTuong.KhenThuongTapThe })
        {
            var cbcs = Cot(cn, f(CheDoDoiTuong.CBCS));
            Assert.NotEmpty(cbcs);
            Assert.Equal(cbcs, Cot(cn, f(CheDoDoiTuong.TanBinh)));
            Assert.Equal(cbcs, Cot(cn, f(CheDoDoiTuong.HocVien)));
        }
        Assert.Equal(3L, (long)Scalar(cn, "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name LIKE 'idx_kt_tentapthe%'")!);
    }

    [Fact]
    public void KhenThuong_CSDLCu_BangThieuCot_DuocBoSung_GiuDuLieu_VaIdNull()
    {
        // CSDL phát hành trước: bảng người thiếu GhiChu_Khen, giấy khen thiếu 3 cột cấp phát, tập thể có ID NULL
        using var cn = MoRam(
            "CREATE TABLE ThongKe_DuocKhenThuong_CBCS (ID INTEGER PRIMARY KEY AUTOINCREMENT, HoVaTen TEXT, SoHieu TEXT, DonVi TEXT, TinhTrang TEXT, SoLuong_Khen TEXT);" +
            "INSERT INTO ThongKe_DuocKhenThuong_CBCS (HoVaTen, SoHieu) VALUES ('A', '123');" +
            "CREATE TABLE ThongKe_GiayKhen_CBCS (ID INTEGER PRIMARY KEY AUTOINCREMENT, HoVaTen TEXT, SoHieu TEXT, DonVi TEXT, TinhTrang TEXT, HinhThuc_Khen TEXT, QuyetDinh_Khen TEXT, NgayCapQD_Khen TEXT, DonVi_Khen TEXT, VeViec_Khen TEXT, GhiChu_Khen TEXT);" +
            "CREATE TABLE ThongKe_KhenThuongTapThe_CBCS (ID INTEGER, STT INTEGER, TenTapThe TEXT);" +
            "INSERT INTO ThongKe_KhenThuongTapThe_CBCS (ID, STT, TenTapThe) VALUES (NULL, 1, 'C1');");
        Module_BangKhenThuong.DamBaoTatCa(cn, null, CheDoDoiTuong.CBCS);
        Assert.Contains(("GhiChu_Khen", "TEXT"), Cot(cn, "ThongKe_DuocKhenThuong_CBCS"));
        foreach (var cot in new[] { "TienThuong", "NgayCapPhat", "CanBoCapPhat" })
            Assert.Contains((cot, "TEXT"), Cot(cn, "ThongKe_GiayKhen_CBCS"));
        Assert.Contains(("NguoiDaiDienNhan", "TEXT"), Cot(cn, "ThongKe_KhenThuongTapThe_CBCS"));
        Assert.Equal("123", Scalar(cn, "SELECT SoHieu FROM ThongKe_DuocKhenThuong_CBCS WHERE HoVaTen = 'A'"));
        Assert.Equal(0L, (long)Scalar(cn, "SELECT COUNT(*) FROM ThongKe_KhenThuongTapThe_CBCS WHERE ID IS NULL")!);
    }

    [Theory]
    [InlineData("Quản lý khen thưởng CBCS năm 2026", CheDoDoiTuong.CBCS, "Quản lý khen thưởng CBCS năm 2026")]
    [InlineData("1. Tìm kiếm thông tin CBCS", CheDoDoiTuong.CBCS, "1. Tìm kiếm thông tin CBCS")]
    public void DoiChuCBCS_CheDoCBCS_GiuNguyen(string vao, CheDoDoiTuong _, string ra) => Assert.Equal(ra, Module_CheDoDoiTuong.DoiChuCBCS(vao));

    [Fact]
    public void DamBaoBang_TaoDuBoBangHocVien_CungCauTrucTanBinh()
    {
        // Tệp tạm đặt trong thư mục bin của bộ kiểm thử (không ghi vào AppData)
        string thuMuc = Path.Combine(AppContext.BaseDirectory, "tmp_chedo_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(thuMuc);
        string c2 = Path.Combine(thuMuc, "csdl2.db"), c4 = Path.Combine(thuMuc, "csdl4.db");
        try
        {
            foreach (var p in new[] { c2, c4 }) { using var t = new SqliteConnection("Data Source=" + p); t.Open(); }
            Module_CheDoDoiTuong.DamBaoBang(CheDoDoiTuong.TanBinh, c2, c4);
            Module_CheDoDoiTuong.DamBaoBang(CheDoDoiTuong.HocVien, c2, c4);
            Module_CheDoDoiTuong.XoaBoDemDamBao();
            Module_CheDoDoiTuong.DamBaoBang(CheDoDoiTuong.HocVien, c2, c4);   // chạy lại: không lỗi, không nhân đôi dữ liệu mẫu
            using (var cn2 = new SqliteConnection("Data Source=" + c2 + ";Pooling=False"))
            {
                cn2.Open();
                foreach (var f in new Func<CheDoDoiTuong, string>[] {
                             Module_CheDoDoiTuong.DanhSach, Module_CheDoDoiTuong.ChiHuyD, Module_CheDoDoiTuong.DonVi,
                             Module_CheDoDoiTuong.QuyDinhTyLe, Module_CheDoDoiTuong.BaNhat, Module_CheDoDoiTuong.SoVangBaNhat,
                             Module_CheDoDoiTuong.TomTatBaNhat, Module_CheDoDoiTuong.BoQuaCanhBao })
                {
                    var hv = Cot(cn2, f(CheDoDoiTuong.HocVien));
                    Assert.NotEmpty(hv);
                    Assert.Equal(Cot(cn2, f(CheDoDoiTuong.TanBinh)), hv);
                }
                using var dem = cn2.CreateCommand();
                dem.CommandText = "SELECT COUNT(*) FROM QuyDinhTyLe_HocVien";
                Assert.Equal(3L, (long)dem.ExecuteScalar()!);
            }
            using (var cn4 = new SqliteConnection("Data Source=" + c4 + ";Pooling=False"))
            {
                cn4.Open();
                // Tân binh + Học viên: cùng lược đồ 6 đợt (Module_LichXet) => 5 cột chung + 30 tuần/tháng + 4 tổng loại
                var cot = Cot(cn4, "ThiDuaThang_HocVien").Select(x => x.Ten).ToList();
                Assert.Equal(Cot(cn4, "ThiDuaThang_TanBinh").Select(x => x.Ten), cot);
                Assert.Equal(39, cot.Count);
                Assert.Contains("Tuan_4_D6", cot);
                Assert.Contains("Thang_D1", cot);
                // Khen thưởng của Học viên cũng được tạo trong CSDL4 (CSDL cũ chưa có)
                foreach (var b in new[] { "ThongKe_DuocKhenThuong_HocVien", "ThongKe_GiayKhen_HocVien", "ThongKe_KhenThuongTapThe_HocVien" })
                    Assert.NotEmpty(Cot(cn4, b));
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            try { Directory.Delete(thuMuc, true); } catch { }
        }
    }
}
