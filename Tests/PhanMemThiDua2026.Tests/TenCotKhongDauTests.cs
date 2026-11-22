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
}
