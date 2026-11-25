using Microsoft.Data.Sqlite;
using PhanMemThiDua2026;
using Xunit;
namespace PhanMemThiDua2026.Tests;
/// <summary>Đổi tên cột có dấu của CSDL cũ sang tên không dấu (Module_TenCotKhongDau). Chỉ dùng CSDL trong RAM.</summary>
public sealed class TenCotKhongDauTests
{
    private static SqliteConnection MoRam(string sql)
    {
        var cn = new SqliteConnection("Data Source=:memory:");
        cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
        return cn;
    }
    private static List<string> Cot(SqliteConnection cn, string bang)
    {
        var ds = new List<string>();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = $"SELECT name FROM pragma_table_info('{bang}')";
        using var rd = cmd.ExecuteReader();
        while (rd.Read()) ds.Add(rd.GetString(0));
        return ds;
    }
    private static object? Scalar(SqliteConnection cn, string sql)
    {
        using var cmd = cn.CreateCommand();
        cmd.CommandText = sql;
        return cmd.ExecuteScalar();
    }
    [Fact]
    public void CSDL2_CotCoDau_DoiTen_GiuDuLieu()
    {
        using var cn = MoRam("CREATE TABLE SuKien_ThoatPhanMem(ID INTEGER PRIMARY KEY AUTOINCREMENT, SuKien_DuọcChon TEXT);" +
                             "INSERT INTO SuKien_ThoatPhanMem VALUES (1, 'AES:v4|abc');");
        Module_TenCotKhongDau.ChuanHoaCSDL2(cn);
        Assert.Equal(new[] { "ID", "SuKien_DuocChon" }, Cot(cn, "SuKien_ThoatPhanMem"));
        Assert.Equal("AES:v4|abc", Scalar(cn, "SELECT SuKien_DuocChon FROM SuKien_ThoatPhanMem WHERE ID = 1"));
        Module_TenCotKhongDau.ChuanHoaCSDL2(cn);   // chạy lại: không đổi gì, không lỗi
        Assert.Equal(new[] { "ID", "SuKien_DuocChon" }, Cot(cn, "SuKien_ThoatPhanMem"));
    }
    [Theory]
    [InlineData("Chọn_GiaiTri")]
    [InlineData("Chon_GiaTri")]
    public void CSDL3_TenCu_DoiTen_GiuDuLieu(string tenCu)
    {
        using var cn = MoRam($"CREATE TABLE TuDong_XoaNhatKy(ID INTEGER PRIMARY KEY AUTOINCREMENT, \"{tenCu}\" TEXT);" +
                             $"INSERT INTO TuDong_XoaNhatKy VALUES (1, '5000 dòng xóa tự động');");
        Module_TenCotKhongDau.ChuanHoaCSDL3(cn);
        Assert.Equal(new[] { "ID", "Chon_GiaiTri" }, Cot(cn, "TuDong_XoaNhatKy"));
        Assert.Equal("5000 dòng xóa tự động", Scalar(cn, "SELECT Chon_GiaiTri FROM TuDong_XoaNhatKy WHERE ID = 1"));
    }
    [Fact]
    public void CSDL3_CaTenCuLanTenMoi_GopVaBoCotCu()
    {
        using var cn = MoRam("CREATE TABLE TuDong_XoaNhatKy(ID INTEGER PRIMARY KEY, Chon_GiaiTri TEXT, \"Chọn_GiaiTri\" TEXT);" +
                             "INSERT INTO TuDong_XoaNhatKy VALUES (1, NULL, 'cu1'), (2, 'moi2', 'cu2');");
        Module_TenCotKhongDau.ChuanHoaCSDL3(cn);
        Assert.Equal(new[] { "ID", "Chon_GiaiTri" }, Cot(cn, "TuDong_XoaNhatKy"));
        Assert.Equal("cu1", Scalar(cn, "SELECT Chon_GiaiTri FROM TuDong_XoaNhatKy WHERE ID = 1"));
        Assert.Equal("moi2", Scalar(cn, "SELECT Chon_GiaiTri FROM TuDong_XoaNhatKy WHERE ID = 2"));
    }
    [Fact]
    public void KhongCoBang_KhongLoi()
    {
        using var cn = MoRam("CREATE TABLE Khac(ID INTEGER);");
        Module_TenCotKhongDau.ChuanHoaCSDL2(cn);
        Module_TenCotKhongDau.ChuanHoaCSDL3(cn);
    }
    [Fact]
    public void TenCotJsonCu_AnhXaSangTenMoi()
    {
        Assert.Equal("SuKien_DuocChon", Module_TenCotKhongDau.TenCotHienTai("SuKien_ThoatPhanMem", "SuKien_DuọcChon"));
        Assert.Equal("Chon_GiaiTri", Module_TenCotKhongDau.TenCotHienTai("TuDong_XoaNhatKy", "Chọn_GiaiTri"));
        Assert.Equal("ID", Module_TenCotKhongDau.TenCotHienTai("TuDong_XoaNhatKy", "ID"));
        Assert.Equal("SuKien_DuọcChon", Module_TenCotKhongDau.TenCotHienTai("BangKhac", "SuKien_DuọcChon"));
    }

    private const string DdlDonVi = "(ID INTEGER PRIMARY KEY AUTOINCREMENT, Ten_DonVi TEXT, ThoiGian TEXT)";

    [Fact]
    public void BangDonViCBCS_DoiTen_GiuDuLieu()
    {
        using var cn = MoRam($"CREATE TABLE DanhSach_DonVi {DdlDonVi}; INSERT INTO DanhSach_DonVi (Ten_DonVi) VALUES ('BCH'), ('TRUNG ĐỘI 1');");
        Module_TenCotKhongDau.ChuanHoaCSDL2(cn);
        Assert.Equal(0L, Scalar(cn, "SELECT COUNT(*) FROM sqlite_master WHERE name='DanhSach_DonVi'"));
        Assert.Equal(2L, Scalar(cn, "SELECT COUNT(*) FROM DanhSach_DonVi_CBCS"));
        Assert.Equal("TRUNG ĐỘI 1", Scalar(cn, "SELECT Ten_DonVi FROM DanhSach_DonVi_CBCS WHERE ID = 2"));
        Module_TenCotKhongDau.ChuanHoaCSDL2(cn);   // chạy lại: không làm gì
        Assert.Equal(2L, Scalar(cn, "SELECT COUNT(*) FROM DanhSach_DonVi_CBCS"));
    }

    [Fact]
    public void BangDonViCBCS_BangMoiRong_ThayBangBangCu()
    {
        using var cn = MoRam($"CREATE TABLE DanhSach_DonVi {DdlDonVi}; INSERT INTO DanhSach_DonVi (Ten_DonVi) VALUES ('C1,D2'); CREATE TABLE DanhSach_DonVi_CBCS {DdlDonVi};");
        Module_TenCotKhongDau.ChuanHoaCSDL2(cn);
        Assert.Equal("C1,D2", Scalar(cn, "SELECT Ten_DonVi FROM DanhSach_DonVi_CBCS"));
        Assert.Equal(0L, Scalar(cn, "SELECT COUNT(*) FROM sqlite_master WHERE name='DanhSach_DonVi'"));
    }

    [Fact]
    public void BangDonViCBCS_CaHaiCoDuLieu_KhongXoaGi()
    {
        using var cn = MoRam($"CREATE TABLE DanhSach_DonVi {DdlDonVi}; INSERT INTO DanhSach_DonVi (Ten_DonVi) VALUES ('CU'); CREATE TABLE DanhSach_DonVi_CBCS {DdlDonVi}; INSERT INTO DanhSach_DonVi_CBCS (Ten_DonVi) VALUES ('MOI');");
        Module_TenCotKhongDau.ChuanHoaCSDL2(cn);
        Assert.Equal("MOI", Scalar(cn, "SELECT Ten_DonVi FROM DanhSach_DonVi_CBCS"));
        Assert.Equal("CU", Scalar(cn, "SELECT Ten_DonVi FROM DanhSach_DonVi"));
    }

    [Fact]
    public void TenBangHienTai_AnhXaGoiChuyenGiaoCu()
    {
        Assert.Equal("DanhSach_DonVi_CBCS", Module_TenCotKhongDau.TenBangHienTai("DanhSach_DonVi"));
        Assert.Equal("DanhSach_DonVi_TanBinh", Module_TenCotKhongDau.TenBangHienTai("DanhSach_DonVi_TanBinh"));
        Assert.Equal("DanhSach_DonVi_CBCS", Module_CheDoDoiTuong.DonVi(CheDoDoiTuong.CBCS));
    }

    [Fact]
    public void BangTheoCheDo_TenCu_DoiSangHauTo_GiuDuLieu()
    {
        using var cn = MoRam("CREATE TABLE DanhSach (ID INTEGER PRIMARY KEY, HoVaTen TEXT); INSERT INTO DanhSach (HoVaTen) VALUES ('A');" +
                             "CREATE TABLE DanhSachTanBinh (ID INTEGER PRIMARY KEY, HoVaTen TEXT); INSERT INTO DanhSachTanBinh (HoVaTen) VALUES ('B');" +
                             "CREATE TABLE DanhSachHocVien_SoVangBaNhat (ID INTEGER PRIMARY KEY, HoVaTen TEXT); INSERT INTO DanhSachHocVien_SoVangBaNhat (HoVaTen) VALUES ('C');" +
                             "CREATE TABLE ChiHuyD (ID INTEGER PRIMARY KEY); CREATE TABLE ChiHuyD_TanBinh (ID INTEGER PRIMARY KEY);" +
                             "CREATE TABLE DanhSach_Nhap (ID INTEGER PRIMARY KEY);");
        Module_TenCotKhongDau.ChuanHoaCSDL2(cn);
        Module_TenCotKhongDau.ChuanHoaCSDL2(cn);   // chạy lại: không đổi gì, không lỗi
        Assert.Equal("A", Scalar(cn, "SELECT HoVaTen FROM DanhSach_CBCS"));
        Assert.Equal("B", Scalar(cn, "SELECT HoVaTen FROM DanhSach_TanBinh"));
        Assert.Equal("C", Scalar(cn, "SELECT HoVaTen FROM DanhSach_SoVangBaNhat_HocVien"));
        foreach (var cu in new[] { "DanhSach", "DanhSachTanBinh", "DanhSachHocVien_SoVangBaNhat", "ChiHuyD" })
            Assert.Equal(0L, Scalar(cn, "SELECT COUNT(*) FROM sqlite_master WHERE name='" + cu + "'"));
        Assert.Equal(1L, Scalar(cn, "SELECT COUNT(*) FROM sqlite_master WHERE name='ChiHuyD_CBCS'"));
        Assert.Equal(1L, Scalar(cn, "SELECT COUNT(*) FROM sqlite_master WHERE name='ChiHuyD_TanBinh'"));   // đã đúng mẫu: giữ nguyên
        Assert.Equal(1L, Scalar(cn, "SELECT COUNT(*) FROM sqlite_master WHERE name='DanhSach_Nhap'"));     // bảng dùng chung: giữ nguyên
    }

    [Fact]
    public void CSDL4_TenCu_DoiSangHauTo_VaTenBangCoTrongTep()
    {
        using var cn = MoRam("CREATE TABLE ThiDuaThang (ID INTEGER PRIMARY KEY, SoHieu TEXT); INSERT INTO ThiDuaThang (SoHieu) VALUES ('1');" +
                             "CREATE TABLE ThongKeTanBinh_DuocKhenThuong (ID INTEGER PRIMARY KEY); CREATE TABLE ThongKe_GiayKhen (ID INTEGER PRIMARY KEY);");
        Assert.Equal("ThiDuaThang", Module_TenCotKhongDau.TenBangCoTrongTep(cn, "ThiDuaThang_CBCS"));   // tệp cũ chỉ đọc
        Module_TenCotKhongDau.ChuanHoaCSDL4(cn);
        Assert.Equal("1", Scalar(cn, "SELECT SoHieu FROM ThiDuaThang_CBCS"));
        Assert.Equal(2L, Scalar(cn, "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('ThongKe_DuocKhenThuong_TanBinh','ThongKe_GiayKhen_CBCS')"));
        Assert.Equal("ThiDuaThang_CBCS", Module_TenCotKhongDau.TenBangCoTrongTep(cn, "ThiDuaThang_CBCS"));
        Assert.Equal("ThongKe_GiayKhen_CBCS", Module_TenCotKhongDau.TenBangHienTai("ThongKe_GiayKhen"));
        Assert.Equal("DanhSach_HocVien", Module_TenCotKhongDau.TenBangHienTai("DanhSachHocVien"));
    }
    [Fact]
    public void BangChiMotCheDo_TenCu_DoiSangHauTo_GiuDuLieu()
    {
        using var cn2 = MoRam("CREATE TABLE QuyDinhTyLeBCH (ID INTEGER PRIMARY KEY, Loai_1 TEXT); INSERT INTO QuyDinhTyLeBCH (Loai_1) VALUES ('10');" +
                              "CREATE TABLE QuyDinhTyLe_XetThiDuaNam (ID INTEGER PRIMARY KEY); CREATE TABLE CheDo_XetThiDuaNam (ID INTEGER PRIMARY KEY, ChoPhepCheDoXetThiDuaNam TEXT);" +
                              "INSERT INTO CheDo_XetThiDuaNam VALUES (1, 'Năm');" +
                              "CREATE TABLE QuanSoThiDuaD2 (ID INTEGER PRIMARY KEY); CREATE TABLE ChonLoaiBaoCao (ID INTEGER PRIMARY KEY, ChonTuan TEXT); INSERT INTO ChonLoaiBaoCao VALUES (1, '2');" +
                              "CREATE TABLE CheDo_XetThiDuaNam_CBCS (ID INTEGER PRIMARY KEY, ChoPhepCheDoXetThiDuaNam TEXT);");   // bảng mới rỗng tạo trước
        Module_TenCotKhongDau.ChuanHoaCSDL2(cn2);
        Assert.Equal("10", Scalar(cn2, $"SELECT Loai_1 FROM {Module_CheDoDoiTuong.CBCS_QuyDinhTyLeBCH}"));
        Assert.Equal("Năm", Scalar(cn2, $"SELECT ChoPhepCheDoXetThiDuaNam FROM {Module_CheDoDoiTuong.CBCS_CheDoXetNam}"));
        Assert.Equal("2", Scalar(cn2, $"SELECT ChonTuan FROM {Module_CheDoDoiTuong.ChonLoaiBaoCao(CheDoDoiTuong.TanBinh)}"));
        foreach (var t in new[] { Module_CheDoDoiTuong.CBCS_QuyDinhTyLeXetNam, Module_CheDoDoiTuong.QuanSo(CheDoDoiTuong.CBCS) })
            Assert.Equal(1L, Scalar(cn2, "SELECT COUNT(*) FROM sqlite_master WHERE name='" + t + "'"));
        foreach (var cu in new[] { "QuyDinhTyLeBCH", "QuyDinhTyLe_XetThiDuaNam", "CheDo_XetThiDuaNam", "QuanSoThiDuaD2", "ChonLoaiBaoCao" })
            Assert.Equal(0L, Scalar(cn2, "SELECT COUNT(*) FROM sqlite_master WHERE name='" + cu + "'"));
        using var cn4 = MoRam("CREATE TABLE ThongKe_PhanLoaiTapThe (ID INTEGER PRIMARY KEY, Thang_1 TEXT); INSERT INTO ThongKe_PhanLoaiTapThe VALUES (1, 'x');");
        Module_TenCotKhongDau.ChuanHoaCSDL4(cn4);
        Assert.Equal("x", Scalar(cn4, $"SELECT Thang_1 FROM {Module_CheDoDoiTuong.CBCS_PhanLoaiTapThe}"));
        // Mọi tên bảng của chế độ mang đúng hậu tố; CBCS không có bảng loại báo cáo
        Assert.All(new[] { Module_CheDoDoiTuong.CBCS_QuyDinhTyLeBCH, Module_CheDoDoiTuong.CBCS_QuyDinhTyLeXetNam,
                           Module_CheDoDoiTuong.CBCS_CheDoXetNam, Module_CheDoDoiTuong.CBCS_PhanLoaiTapThe }, t => Assert.EndsWith("_CBCS", t));
        Assert.Null(Module_CheDoDoiTuong.ChonLoaiBaoCao(CheDoDoiTuong.CBCS));
        Assert.EndsWith("_HocVien", Module_CheDoDoiTuong.ChonLoaiBaoCao(CheDoDoiTuong.HocVien));
        Assert.True(Module_TenCotKhongDau.LaBangDaLoaiBo("csdl2", "Tinh"));
        Assert.True(Module_TenCotKhongDau.LaBangDaLoaiBo("csdl2", "TyLe"));
    }
}
