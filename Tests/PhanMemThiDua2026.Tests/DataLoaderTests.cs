using Xunit;
namespace PhanMemThiDua2026.Tests;
/// <summary>Khóa sắp xếp STT của DataLoader: chịu được NULL / chữ / kiểu số khác (bản cũ 1 dòng lỗi làm hỏng cả lần nạp).</summary>
public class DataLoaderTests
{
    [Theory]
    [InlineData(5L, 5L)]
    [InlineData(7, 7L)]
    [InlineData("12", 12L)]
    [InlineData(" 42 ", 42L)]
    [InlineData(3.0, 3L)]
    [InlineData(null, long.MaxValue)]
    [InlineData("", long.MaxValue)]
    [InlineData("abc", long.MaxValue)]
    [InlineData("1.5", long.MaxValue)]      // không phải số nguyên => xếp cuối, không ném lỗi
    public void LaySttDeSapXep_ChiuDuocDuLieuBan(object? giaTri, long mongDoi)
        => Assert.Equal(mongDoi, DataLoader.LaySttDeSapXep(giaTri));
    [Fact]
    public void LaySttDeSapXep_DBNull_XepCuoi()
        => Assert.Equal(long.MaxValue, DataLoader.LaySttDeSapXep(DBNull.Value));
    [Fact]
    public void LaySttDeSapXep_TranSo_XepCuoi()
        => Assert.Equal(long.MaxValue, DataLoader.LaySttDeSapXep(decimal.MaxValue));
}
