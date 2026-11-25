using System.Reflection;
using Microsoft.Data.Sqlite;
using PhanMemThiDua2026;
using Xunit;
namespace PhanMemThiDua2026.Tests;
/// <summary>Lịch xét nhóm tuần (Module_LichXet): cài được cho Tân binh + Học viên, kéo sang năm sau, nâng cấp lược đồ cũ.</summary>
[Collection("LichXet")]
public sealed class LichXetTests
{
    /// <summary>Đặt sẵn bộ đệm cấu hình + năm (không cần CSDL thật).</summary>
    private static void DatBoDem(CheDoDoiTuong c, CauHinhLich ch, int nam)
    {
        var t = typeof(Module_LichXet);
        const BindingFlags BF = BindingFlags.NonPublic | BindingFlags.Static;
        Module_LichXet.XoaBoDem();
        ((Dictionary<CheDoDoiTuong, CauHinhLich>)t.GetField("_boDemCauHinh", BF)!.GetValue(null)!)[c] = ch;
        t.GetField("_boDemNam", BF)!.SetValue(null, (Nam: nam, Luc: Environment.TickCount64));
    }

    [Fact]
    public void TanBinh_MacDinh_GiuCachXetCu()
    {
        Assert.Equal(new CauHinhLich(2, 4, true), Module_LichXet.MacDinh(CheDoDoiTuong.TanBinh));
        DatBoDem(CheDoDoiTuong.TanBinh, Module_LichXet.MacDinh(CheDoDoiTuong.TanBinh), 2026);
        var lich = Module_LichXet.LichXet(CheDoDoiTuong.TanBinh);
        Assert.Equal(new[] { 2, 3, 4, 5 }, lich.Select(d => d.Thang));
        Assert.Equal(new[] { 3, 4, 5, 6 }, lich.Select(d => d.ThangKetQua));
        Assert.Equal(new[] { "Tuan_1_D1", "Tuan_2_D1", "Tuan_3_D1", "Tuan_4_D1" }, lich[0].CotTuan);
        Assert.Equal("Thang_D4", lich[3].CotKetQua);
        Assert.Equal("Tuần 1\nTháng 2", Module_LichXet.NhanCot(CheDoDoiTuong.TanBinh, "Tuan_1_D1"));
        Assert.Equal("Kết quả\nTháng 3", Module_LichXet.NhanCot(CheDoDoiTuong.TanBinh, "Thang_D1"));
        Assert.True(Module_LichXet.LaCotKhongDung(CheDoDoiTuong.TanBinh, "Thang_D5"));
        Assert.Empty(Module_LichXet.LichXet(CheDoDoiTuong.CBCS));
    }

    [Fact]
    public void TanBinh_DoiLich_BatDauThang3_KhongSuaPhanMem()
    {
        DatBoDem(CheDoDoiTuong.TanBinh, new CauHinhLich(3, 5, true), 2026);
        var lich = Module_LichXet.LichXet(CheDoDoiTuong.TanBinh);
        Assert.Equal(new[] { 3, 4, 5, 6, 7 }, lich.Select(d => d.Thang));
        Assert.Equal(8, lich[4].ThangKetQua);
        Assert.Equal("Kết quả Tháng 4", Module_LichXet.DanhSachChon(CheDoDoiTuong.TanBinh).First(kv => kv.Value == "Thang_D1").Key);
    }

    [Fact]
    public void HocVien_KeoSangNamSau_NhanCoNam()
    {
        DatBoDem(CheDoDoiTuong.HocVien, new CauHinhLich(11, 4, false), 2026);
        var lich = Module_LichXet.LichXet(CheDoDoiTuong.HocVien);
        Assert.Equal(new[] { 11, 12, 1, 2 }, lich.Select(d => d.Thang));
        Assert.Equal(new[] { 2026, 2026, 2027, 2027 }, lich.Select(d => d.Nam));
        Assert.Equal("Thang_D3", lich[2].CotKetQua);
        Assert.Equal("Tuần 2\nTháng 1/2027", Module_LichXet.NhanCot(CheDoDoiTuong.HocVien, "Tuan_2_D3"));
        Assert.Equal("Kết quả\nTháng 11", Module_LichXet.NhanCot(CheDoDoiTuong.HocVien, "Thang_D1"));
        Assert.True(Module_LichXet.LaCotKhongDung(CheDoDoiTuong.HocVien, "Thang_D5"));
        var chon = Module_LichXet.DanhSachChon(CheDoDoiTuong.HocVien);
        Assert.Equal(20, chon.Count);
        Assert.Equal("Tuan_1_D1", chon["Tuần 1 - Tháng 11"]);
        Assert.Equal("Thang_D4", chon["Kết quả Tháng 2/2027"]);
        // kết quả tháng sau kéo qua năm: tháng 12 -> kết quả tháng 1 năm sau
        var lich2 = Module_LichXet.TaoLich(new CauHinhLich(12, 1, true), 2026);
        Assert.Equal((1, 2027), (lich2[0].ThangKetQua, lich2[0].NamKetQua));
        // tệp năm cũ còn lược đồ cũ => hiển thị theo lược đồ cũ
        var cu = Module_LichXet.CotDangDungTrong(CheDoDoiTuong.HocVien, new List<string> { "ID", "Thang_3", "Tuan_1_T2" });
        Assert.Equal(new[] { "Tuan_1_T2", "Thang_3" }, cu);
    }

    [Fact]
    public void NangCapBang_ChuyenDuLieuCuSangDot_GiuCotPhatSinh()
    {
        string thuMuc = Path.Combine(AppContext.BaseDirectory, "tmp_lichxet_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(thuMuc);
        string db = Path.Combine(thuMuc, "csdl4.db");
        try
        {
            using var cn = new SqliteConnection("Data Source=" + db + ";Pooling=False");
            cn.Open();
            using (var cmd = cn.CreateCommand())
            {
                // Lược đồ cũ của Tân binh
                cmd.CommandText = "CREATE TABLE ThiDuaThang_TanBinh (ID INTEGER PRIMARY KEY AUTOINCREMENT, HoVaTen TEXT, SoHieu TEXT, DonVi TEXT, TinhTrang TEXT, " +
                    string.Join(", ", Module_LichXet.CotVatLyCu.Select(c => c + " TEXT")) +
                    ", TS_Loai1 TEXT, TS_Loai2 TEXT, TS_Loai3 TEXT, TS_Loai4 TEXT, GhiChu TEXT);" +
                    "INSERT INTO ThiDuaThang_TanBinh (HoVaTen, SoHieu, Tuan_1_T2, Thang_3, Tuan_4_T5, Thang_6, TS_Loai1, GhiChu)" +
                    " VALUES ('A', 'TB00001', '1', '2', '3', '4', '1', 'gc');";
                cmd.ExecuteNonQuery();
            }
            using (var tran = cn.BeginTransaction())
            {
                Module_LichXet.NangCapBang(cn, tran, "ThiDuaThang_TanBinh");
                Module_LichXet.NangCapBang(cn, tran, "ThiDuaThang_TanBinh");   // lần 2: đã nâng cấp => không làm gì
                tran.Commit();
            }
            using var doc = cn.CreateCommand();
            doc.CommandText = "SELECT Tuan_1_D1, Thang_D1, Tuan_4_D4, Thang_D4, TS_Loai1, GhiChu, SoHieu FROM ThiDuaThang_TanBinh";
            using (var r = doc.ExecuteReader())
            {
                Assert.True(r.Read());
                Assert.Equal(new[] { "1", "2", "3", "4", "1", "gc", "TB00001" }, Enumerable.Range(0, 7).Select(i => r.GetString(i)));
            }
            doc.CommandText = "SELECT COUNT(*) FROM pragma_table_info('ThiDuaThang_TanBinh') WHERE name GLOB 'Tuan_*_T*'";
            Assert.Equal(0L, (long)doc.ExecuteScalar()!);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            try { Directory.Delete(thuMuc, true); } catch { }
        }
    }
}
