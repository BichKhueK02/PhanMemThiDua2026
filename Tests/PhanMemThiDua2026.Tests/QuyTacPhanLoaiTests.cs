using PhanMemThiDua2026;
using Xunit;

namespace PhanMemThiDua2026.Tests;

/// <summary>Quy tắc phân loại tháng từ 4 tuần (Module_QuyTacPhanLoai, tách từ Form36).</summary>
public class QuyTacPhanLoaiTests
{
    private const string L1 = Module_HeThong.Loai_1, L2 = Module_HeThong.Loai_2, L3 = Module_HeThong.Loai_3, L4 = Module_HeThong.Loai_4;

    [Theory]
    // Không tuần nào hợp lệ => Loại 4
    [InlineData(null, null, null, null, L4)]
    [InlineData("", " ", "abc", "Loại 0", L4)]
    // Loại 1: >= 2 tuần Loại 1, không có Loại 3/4
    [InlineData("Loại 1", "Loại 1", null, null, L1)]
    [InlineData("Loại 1", "Loại 1", "Loại 2", "Loại 2", L1)]
    [InlineData("Loại 1", "Loại 1", "Loại 1", "Loại 3", L3)]
    // Loại 2: >= 2 tuần Loại 1-2, không có Loại 3/4
    [InlineData("Loại 1", "Loại 2", null, null, L2)]
    [InlineData("Loại 2", "Loại 2", "Loại 2", "Loại 2", L2)]
    // Loại 3: >= 2 tuần Loại 1-3, không có Loại 4
    [InlineData("Loại 3", "Loại 3", null, null, L3)]
    [InlineData("Loại 1", "Loại 3", null, null, L3)]
    // Có Loại 4 => Loại 4
    [InlineData("Loại 1", "Loại 1", "Loại 1", "Loại 4", L4)]
    // Chỉ 1 tuần hợp lệ => không đủ 2 tuần => Loại 4
    [InlineData("Loại 1", null, null, null, L4)]
    // Chỉ lấy CHỮ SỐ ĐẦU TIÊN: "Loại 12" là Loại 1; "Loại 5" không tính
    [InlineData("Loại 12", "1", null, null, L1)]
    [InlineData("Loại 5", "Loại 1", "Loại 1", null, L1)]
    public void PhanLoaiThang_DungQuyTac(string? t1, string? t2, string? t3, string? t4, string mongDoi)
        => Assert.Equal(mongDoi, Module_QuyTacPhanLoai.PhanLoaiThangTu4Tuan(t1, t2, t3, t4));

    /// <summary>
    /// Đối chiếu VÉT CẠN với bản gốc trong Form36 (chép nguyên văn bên dưới): mọi tổ hợp 4 tuần trên tập giá trị đại diện
    /// (null, rỗng, khoảng trắng, chữ, Loại 0-5, số nhiều chữ số, chữ số Unicode) phải cho cùng kết quả.
    /// </summary>
    [Fact]
    public void PhanLoaiThang_TrungKhopBanGoc_VetCan()
    {
        string?[] giaTri =
        {
            null, "", "  ", "abc", "Không PL", "Loại 0", "Loại 1", "Loại 2", "Loại 3", "Loại 4", "Loại 5",
            "1", "2", "3", "4", "Loại 12", "Loại 41", " 3 ", "x2y", "١", "١ Loại 1"
        };
        int soCa = 0;
        foreach (var a in giaTri)
        foreach (var b in giaTri)
        foreach (var c in giaTri)
        foreach (var d in giaTri)
        {
            Assert.Equal(BanGoc(a, b, c, d), Module_QuyTacPhanLoai.PhanLoaiThangTu4Tuan(a, b, c, d));
            soCa++;
        }
        Assert.Equal(giaTri.Length * giaTri.Length * giaTri.Length * giaTri.Length, soCa);
    }

    /// <summary>Bản gốc Form36.TinhLogicPhanLoaiMDK_Fast trước khi tách (giữ làm chuẩn đối chiếu).</summary>
    private static string BanGoc(string? t1, string? t2, string? t3, string? t4)
    {
        int l1 = 0, l2 = 0, l3 = 0, l4 = 0;
        int validCount = 0;
        void Check(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return;
            char digit = s.FirstOrDefault(char.IsDigit);
            if (digit == '1') { l1++; validCount++; }
            else if (digit == '2') { l2++; validCount++; }
            else if (digit == '3') { l3++; validCount++; }
            else if (digit == '4') { l4++; validCount++; }
        }
        Check(t1); Check(t2); Check(t3); Check(t4);
        if (validCount == 0) return Module_HeThong.Loai_4;
        if (l1 >= 2 && l3 == 0 && l4 == 0) return Module_HeThong.Loai_1;
        if ((l1 + l2) >= 2 && l3 == 0 && l4 == 0) return Module_HeThong.Loai_2;
        if ((l1 + l2 + l3) >= 2 && l4 == 0) return Module_HeThong.Loai_3;
        return Module_HeThong.Loai_4;
    }
}
