using System.Data;
using PhanMemThiDua2026;
using Xunit;
using M = PhanMemThiDua2026.Module_BoLocKhenThuong;

namespace PhanMemThiDua2026.Tests
{
    /// <summary>Lõi BỘ LỌC KÉP dùng chung (Module_BoLocKhenThuong) + bộ lọc danh sách Form6 (BoLocDanhSach).</summary>
    public class BoLocKepTests
    {
        private sealed record Dong(string Ten, string DonVi, string HinhThuc);

        private static readonly Dong[] Mau =
        {
            new("Nguyễn Văn An", "Sư đoàn", "Giấy khen"),
            new("Trần Thị Bình", "Sư đoàn 10", "Bằng khen"),
            new("Lê Văn Cường", "Sư đoàn", "Bằng khen"),
            new("Phạm Minh Đức", "Trung đoàn 1", "Giấy khen"),
            new("Đỗ Hữu Ân", " Trung đoàn 1 ", "Chiến sĩ thi đua"),
        };

        [Fact]
        public void KhongLoc_TraVeMoiDong_VaDuMoiGiaTri()
        {
            var dv = new M.TieuChiKep<Dong>("", x => x.DonVi);
            var ht = new M.TieuChiKep<Dong>("", x => x.HinhThuc);
            var kq = M.LocKep(Mau, null, dv, ht);
            Assert.Equal(new[] { 0, 1, 2, 3, 4 }, kq);
            Assert.Equal(3, dv.Con.Count);   // "Trung đoàn 1" có khoảng trắng thừa vẫn gộp làm 1
            Assert.Equal(3, ht.Con.Count);
        }

        [Fact]
        public void ChonDonVi_SoKhopChinhXac_KhongRaLanDonViCoTenDaiHon()
        {
            var dv = new M.TieuChiKep<Dong>("sư đoàn", x => x.DonVi);
            var kq = M.LocKep(Mau, null, dv);
            Assert.Equal(new[] { 0, 2 }, kq);   // không lẫn "Sư đoàn 10"
        }

        [Fact]
        public void MoiCombo_ChiConGiaTriThoaCacBoLocKhac()
        {
            var dv = new M.TieuChiKep<Dong>("Sư đoàn", x => x.DonVi);
            var ht = new M.TieuChiKep<Dong>("Bằng khen", x => x.HinhThuc);
            var kq = M.LocKep(Mau, null, dv, ht);
            Assert.Equal(new[] { 2 }, kq);
            // Combo đơn vị: các đơn vị có "Bằng khen" (bỏ qua chính điều kiện đơn vị)
            Assert.True(dv.Con.SetEquals(new[] { "Sư đoàn", "Sư đoàn 10" }));
            // Combo hình thức: các hình thức của "Sư đoàn" (bỏ qua chính điều kiện hình thức)
            Assert.True(ht.Con.SetEquals(new[] { "Giấy khen", "Bằng khen" }));
        }

        [Fact]
        public void DieuKienChung_ThuHepCaKetQuaLanMoiCombo()
        {
            string[] tu = M.TachTuKhoa("van");   // bỏ dấu: khớp "Văn"
            var dv = new M.TieuChiKep<Dong>("", x => x.DonVi);
            var kq = M.LocKep(Mau, x => M.KhopTuKhoa(Lop.ChuanHoa(x.Ten), tu), dv);
            Assert.Equal(new[] { 0, 2 }, kq);
            Assert.True(dv.Con.SetEquals(new[] { "Sư đoàn" }));
        }

        [Fact]
        public void HopLe_LoaiGiaTriLoiKhoiCombo()
        {
            var ds = new[] { new Dong("A", "[Lỗi giải mã]", ""), new Dong("B", "X", "") };
            var dv = new M.TieuChiKep<Dong>("", x => x.DonVi, v => v != "[Lỗi giải mã]");
            M.LocKep(ds, null, dv);
            Assert.True(dv.Con.SetEquals(new[] { "X" }));
        }

        [Fact]
        public void TachTuKhoa_BoDau_NhieuTu()
        {
            Assert.Equal(new[] { "nguyen", "van" }, M.TachTuKhoa("  Nguyễn   VĂN "));
            Assert.Empty(M.TachTuKhoa("   "));
        }

        [Fact]
        public void ThoatChuoi_RowFilter()
        {
            Assert.Equal("a''b[%][*][[][]]", M.ThoatLike("a'b%*[]"));
            Assert.Equal("Đội [1]''s", M.ThoatBang("Đội [1]'s"));   // "=" không được thoát ký tự LIKE
        }

        [Fact]
        public void CotTimKiem_DungRowFilter_TuCapNhatKhiSuaHoacThemDong()
        {
            var dt = new DataTable();
            dt.Columns.Add("HoVaTen"); dt.Columns.Add("SoHieu"); dt.Columns.Add("DonVi");
            dt.Rows.Add("Nguyễn Văn An", "123-456", "Đội [1]");
            dt.Rows.Add("Trần Văn Bình", "789", "Đội 2");
            dt.AcceptChanges();
            M.DamBaoCotTimKiem(dt, "HoVaTen", "SoHieu");
            Assert.Null(dt.GetChanges());                         // dựng cột ẩn không làm dòng thành "đã sửa"
            Assert.Equal(MappingType.Hidden, dt.Columns[M.COT_TIM_KIEM]!.ColumnMapping);

            var view = dt.DefaultView;
            view.RowFilter = M.DieuKienTimKiem("nguyen an");
            Assert.Single(view);
            view.RowFilter = M.DieuKienTimKiem("456");             // tìm theo số hiệu
            Assert.Single(view);
            view.RowFilter = $"DonVi = '{M.ThoatBang("Đội [1]")}'"; // đơn vị có ngoặc vuông vẫn khớp
            Assert.Single(view);

            view.RowFilter = M.DieuKienTimKiem("cuong");
            Assert.Empty(view);
            dt.Rows[1]["HoVaTen"] = "Lê Văn Cường";                 // sửa ô nguồn -> cột ẩn đổi theo
            Assert.Single(view);
            dt.Rows.Add("Đỗ Cường", "1", "Đội 3");                   // thêm dòng mới -> cột ẩn tự điền
            Assert.Equal(2, view.Count);
            Assert.Equal("", M.DieuKienTimKiem("  "));
        }

        [Fact]
        public void BoLocDanhSach_TimTheoSoHieu_VaGomDonVi()
        {
            var dt = new DataTable();
            dt.Columns.Add("HoVaTen"); dt.Columns.Add("SoHieu"); dt.Columns.Add("DonVi"); dt.Columns.Add("PhanLoai");
            dt.Rows.Add("Nguyễn Văn An", "111", "Đội 1", "Loại 1");
            dt.Rows.Add("Trần Văn Bình", "222", "Đội 2", "Loại 2");
            dt.Rows.Add("Lê Thị Cúc", "333", "Đội 2", "");
            dt.Rows.Add("", "", "", "");                             // dòng đệm ảo
            var bo = BoLocDanhSach.TaoTu(dt);
            var khongPL = new[] { "Không PL" };

            Assert.Equal(new[] { 1 }, bo.Loc("222", null, null, false, khongPL));

            var con = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var kq = bo.Loc("van", "Đội 1", null, false, khongPL, con);
            Assert.Equal(new[] { 0 }, kq);
            Assert.True(con.SetEquals(new[] { "Đội 1", "Đội 2" }));  // combo đơn vị: mọi đơn vị có người tên "van"

            var conTatCa = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Assert.Null(bo.Loc("", null, null, false, khongPL, conTatCa));   // không lọc gì => mọi dòng
            Assert.Equal(2, conTatCa.Count);
        }

        private static class Lop
        {
            public static string ChuanHoa(string s) => BoLocDanhSach.ChuanHoaTimKiem(s);
        }
    }
}
