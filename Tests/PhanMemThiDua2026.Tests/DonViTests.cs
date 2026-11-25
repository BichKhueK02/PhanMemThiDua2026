using System.Data;
using PhanMemThiDua2026;
using Xunit;
using D = PhanMemThiDua2026.Module_DonVi;

namespace PhanMemThiDua2026.Tests
{
    /// <summary>So khớp + sắp xếp tên đơn vị dùng chung (Module_DonVi) với mọi kiểu ký hiệu cán bộ tự đặt.</summary>
    /// <summary>Tên đơn vị là CHUỖI NGUYÊN VĂN: so khớp chính xác, thứ tự theo mảng khai báo (Module_DonVi).</summary>
    public class DonViTests
    {
        [Theory]
        [InlineData("TRUNG ĐỘI 1")]
        [InlineData("C1,D2")]
        [InlineData("TRUNG ĐỘI 1 - TTHL2")]
        [InlineData("Đại đội 1 (C1)")]
        [InlineData("K10&K11")]
        public void CungDonVi_GiongHet(string a) => Assert.True(D.CungDonVi(a, a) && D.CungDonVi(" " + a + " ", a));

        [Theory]
        [InlineData("TRUNG ĐỘI 1", "TRUNG ĐỘI 10")]
        [InlineData("TRUNG ĐỘI 1", "Trung đội 1")]
        [InlineData("C1,D2", "C1, D2")]
        [InlineData("C1,D2", "C1-D2")]
        [InlineData("C1,D2", "C1;D2")]
        [InlineData("TRUNG ĐỘI 1 - TTHL2", "TRUNG ĐỘI 1-TTHL2")]
        public void CungDonVi_KhacChuoiLaKhacDonVi(string a, string b) => Assert.False(D.CungDonVi(a, b));

        [Fact]
        public void LaBCH() { Assert.True(D.LaBCH("BCH")); Assert.True(D.LaBCH(" bch ")); Assert.False(D.LaBCH("BCH D2")); }

        [Fact]
        public void SapXep_TheoMangKhaiBao_KhongDoiTen()
        {
            var khaiBao = new[] { "BCH", "TRUNG ĐỘI 2", "TRUNG ĐỘI 1", "C1,D2" };
            var duLieu = new[] { "C1,D2", "Tổ 10", "TRUNG ĐỘI 1", "Tổ 2", "BCH", "TRUNG ĐỘI 2", "TRUNG ĐỘI 1" };
            Assert.Equal(new[] { "BCH", "TRUNG ĐỘI 2", "TRUNG ĐỘI 1", "C1,D2", "Tổ 2", "Tổ 10" }, D.SapXep(duLieu, khaiBao));
        }

        [Fact]
        public void BangThuTu_ViTri()
        {
            var b = D.TaoBangThuTu(new[] { "C2,D2", "C1,D2", "C1,D2" });
            Assert.Equal(0, b.ThuTu("C2,D2"));
            Assert.Equal(1, b.ThuTu("C1,D2"));
            Assert.Equal(int.MaxValue, b.ThuTu("C1, D2"));
            Assert.Equal(2, b.DanhSach.Count);
        }

        [Fact]
        public void RowFilter_ChinhXac_KyTuDacBiet()
        {
            var dt = new DataTable();
            dt.Columns.Add("DonVi", typeof(string));
            foreach (var v in new[] { "Đội 'A' [1]", "Tổ 100%", "C*1", "C1,D2", "C1,D20" }) dt.Rows.Add(v);
            foreach (var (dv, n) in new[] { ("Đội 'A' [1]", 1), ("Tổ 100%", 1), ("C*1", 1), ("C1,D2", 1) })
            {
                dt.DefaultView.RowFilter = D.DieuKienRowFilter(dt, "DonVi", dv);
                Assert.Equal(n, dt.DefaultView.Count);
            }
        }

        [Fact]
        public void LocKep_DonVi_ChinhXac()
        {
            var ds = new[] { "TRUNG ĐỘI 1", "TRUNG ĐỘI 10", "TRUNG ĐỘI 1", "C1,D2" };
            var tc = new Module_BoLocKhenThuong.TieuChiKep<string>("TRUNG ĐỘI 1", x => x, laDonVi: true);
            Assert.Equal(new[] { 0, 2 }, Module_BoLocKhenThuong.LocKep(ds, null, tc));
            Assert.Equal(3, tc.Con.Count);
        }
    }
}
