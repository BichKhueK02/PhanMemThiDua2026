using Microsoft.Data.Sqlite;
using PhanMemThiDua2026;
using Xunit;
namespace PhanMemThiDua2026.Tests;
/// <summary>Chuỗi kết nối dùng chung (Module_KetNoiCSDL). Chỉ dùng tệp tạm, không đụng CSDL thật.</summary>
public sealed class KetNoiCSDLTests : IDisposable
{
    private readonly string _thuMuc = Path.Combine(Path.GetTempPath(), "PMTD_Test_" + Guid.NewGuid().ToString("N"));
    public KetNoiCSDLTests() => Directory.CreateDirectory(_thuMuc);
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();   // nhả tệp trước khi xóa thư mục tạm
        try { Directory.Delete(_thuMuc, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
    [Fact]
    public void MacDinh_TuongDuong_ChuoiTho()
    {
        const string duongDan = @"C:\Du Lieu\csdl2.db";
        var moi = new SqliteConnectionStringBuilder(Module_KetNoiCSDL.ChuoiKetNoi(duongDan));
        var cu = new SqliteConnectionStringBuilder($"Data Source={duongDan}");
        Assert.Equal(cu.DataSource, moi.DataSource);
        Assert.Equal(cu.Mode, moi.Mode);
        Assert.Equal(cu.Pooling, moi.Pooling);
        Assert.Equal(cu.DefaultTimeout, moi.DefaultTimeout);
        Assert.Equal(cu.Cache, moi.Cache);
        Assert.Equal(cu.ToString(), moi.ToString());
    }
    [Theory]
    [InlineData(true, false, SqliteOpenMode.ReadOnly)]
    [InlineData(true, true, SqliteOpenMode.ReadOnly)]     // chiDoc thắng khongTaoMoi
    [InlineData(false, true, SqliteOpenMode.ReadWrite)]
    [InlineData(false, false, SqliteOpenMode.ReadWriteCreate)]
    public void CheDoMo_DungTuyChon(bool chiDoc, bool khongTaoMoi, SqliteOpenMode mongDoi)
    {
        var sb = new SqliteConnectionStringBuilder(Module_KetNoiCSDL.ChuoiKetNoi("a.db", chiDoc: chiDoc, khongTaoMoi: khongTaoMoi));
        Assert.Equal(mongDoi, sb.Mode);
    }
    [Fact]
    public void TuyChonKhac_DuocGhiDung()
    {
        var sb = new SqliteConnectionStringBuilder(Module_KetNoiCSDL.ChuoiKetNoi("a.db", pooling: false, thoiGianCho: 10, cacheChung: true));
        Assert.False(sb.Pooling);
        Assert.Equal(10, sb.DefaultTimeout);
        Assert.Equal(SqliteCacheMode.Shared, sb.Cache);
    }
    [Fact]
    public void DuongDanNull_KhongNemLoi()
        => Assert.Equal(string.Empty, new SqliteConnectionStringBuilder(Module_KetNoiCSDL.ChuoiKetNoi(null)).DataSource);
    /// <summary>Lỗi thật đã sửa ở Giai đoạn 2: thư mục chứa ';' làm vỡ chuỗi ghép tay "Data Source={path}".</summary>
    [Fact]
    public void DuongDanCoDauChamPhay_VanMoDuoc()
    {
        string thuMuc = Path.Combine(_thuMuc, "Năm 2025; lưu trữ");
        Directory.CreateDirectory(thuMuc);
        string tep = Path.Combine(thuMuc, "csdl.db");
        Assert.ThrowsAny<ArgumentException>(() => new SqliteConnection($"Data Source={tep}"));   // bản cũ vỡ
        using (var cn = Module_KetNoiCSDL.MoKetNoi(tep, pooling: false))
        {
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "CREATE TABLE T(ID INTEGER PRIMARY KEY); INSERT INTO T VALUES (7);";
            cmd.ExecuteNonQuery();
        }
        using (var cn = Module_KetNoiCSDL.MoKetNoi(tep, chiDoc: true, pooling: false))
        {
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT ID FROM T";
            Assert.Equal(7L, cmd.ExecuteScalar());
        }
    }
    [Fact]
    public void KhongTaoMoi_TepThieu_BaoLoi_VaKhongTaoTepRong()
    {
        string tep = Path.Combine(_thuMuc, "khong_ton_tai.db");
        Assert.Throws<SqliteException>(() => Module_KetNoiCSDL.MoKetNoi(tep, khongTaoMoi: true, pooling: false));
        Assert.False(File.Exists(tep));
    }
    [Fact]
    public void ChiDoc_KhongGhiDuoc()
    {
        string tep = Path.Combine(_thuMuc, "chi_doc.db");
        using (var cn = Module_KetNoiCSDL.MoKetNoi(tep, pooling: false))
        {
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "CREATE TABLE T(ID INTEGER)";
            cmd.ExecuteNonQuery();
        }
        using var cnDoc = Module_KetNoiCSDL.MoKetNoi(tep, chiDoc: true, pooling: false);
        using var ghi = cnDoc.CreateCommand();
        ghi.CommandText = "INSERT INTO T VALUES (1)";
        Assert.Throws<SqliteException>(() => ghi.ExecuteNonQuery());
    }
    /// <summary>Mở thất bại thì kết nối phải được Dispose (không giữ khóa tệp làm kẹt sao lưu/khôi phục).</summary>
    [Fact]
    public void MoThatBai_NhaTep()
    {
        string tep = Path.Combine(_thuMuc, "hong.db");
        File.WriteAllText(tep, "day khong phai CSDL SQLite, chi la van ban de mo that bai");
        Assert.ThrowsAny<SqliteException>(() =>
        {
            using var cn = Module_KetNoiCSDL.MoKetNoi(tep, chiDoc: true, pooling: false);
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT count(*) FROM sqlite_master";
            cmd.ExecuteScalar();
        });
        File.Delete(tep);   // còn bị giữ handle => IOException
        Assert.False(File.Exists(tep));
    }
}
