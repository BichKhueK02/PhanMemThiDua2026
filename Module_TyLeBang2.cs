using System.Globalization;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// KẾT QUẢ BẢNG 2 (Bảng tỷ lệ phần trăm trên Form4) — bất biến, tính bằng MỘT hàm duy nhất (<see cref="Module_TyLeBang2.Tinh"/>).
    /// Form4 dùng nó để dựng Bảng 2, Module_XuatTongHop dùng đúng đối tượng này để ghi sheet "BAO_CAO_TONG_HOP" => hai nơi KHÔNG THỂ lệch nhau.
    /// </summary>
    /// <param name="TongQS">Tổng quân số.</param>
    /// <param name="Loai1">KQ Hiện tại — Loại 1.</param>
    /// <param name="Loai2">KQ Hiện tại — Loại 2 THUẦN (KHÔNG gồm Loại 1).</param>
    /// <param name="Loai3">KQ Hiện tại — Loại 3.</param>
    /// <param name="Loai4">KQ Hiện tại — Loại 4.</param>
    /// <param name="KhongPL">KQ Hiện tại — Không phân loại.</param>
    /// <param name="DuDieuKien">Tổng quân số trừ Loại 4 và Không PL (mẫu số của Loại 2 / Loại 3).</param>
    /// <param name="CanDatLoai1">KQ Cần đạt — Loại 1 (nằm TRONG Loại 2).</param>
    /// <param name="CanDatLoai2Tong">KQ Cần đạt — Loại 2 TỔNG (đã gồm cả Loại 1).</param>
    /// <param name="CanDatLoai3">KQ Cần đạt — Loại 3.</param>
    /// <param name="TyLeLoai1">% quy định của Loại 1 (tính trên Loại 2).</param>
    /// <param name="TyLeLoai2">% quy định của Loại 2 (tính trên đủ điều kiện).</param>
    internal sealed record KetQuaBang2(
        int TongQS, int Loai1, int Loai2, int Loai3, int Loai4, int KhongPL,
        int DuDieuKien, int CanDatLoai1, int CanDatLoai2Tong, int CanDatLoai3,
        double TyLeLoai1, double TyLeLoai2)
    {
        /// <summary>% quy định của Loại 3 (chỉ để in chữ "Loại 3 x%" trên tờ trình; không tham gia công thức).</summary>
        public double TyLeLoai3 { get; init; }
        /// <summary>
        /// Chuyển sang dạng bảng "TyLe" cũ (ID 1..6 -> KQ Hiện tại, KQ Cần đạt) để Module_XuatTongHop giữ nguyên cách ghi ô:
        /// 1 Tổng quân số | 2 Loại 1 | 3 Loại 2 (Cần đạt = Loại 2 TỔNG) | 4 Loại 3 | 5 Loại 4 | 6 Không PL.
        /// </summary>
        public Dictionary<int, (int hienTai, int canDat)> ToTyLe() => new()
        {
            [1] = (TongQS, TongQS),
            [2] = (Loai1, CanDatLoai1),
            [3] = (Loai2, CanDatLoai2Tong),
            [4] = (Loai3, CanDatLoai3),
            [5] = (Loai4, 0),
            [6] = (KhongPL, 0)
        };
    }
    /// <summary>
    /// Một lát của biểu đồ tròn 3D. <paramref name="GiaTri"/> = diện tích lát (các lát KHÔNG chồng lấn, tổng = tổng quân số).
    /// <paramref name="ChuThich"/> = chữ cột phải của chú thích (dạng "a/b = x%", giống cột "KQ Gửi E09").
    /// <paramref name="NhanTrenLat"/> = chữ in trên lát (cùng quy tắc với chú thích). <paramref name="MauIndex"/> = màu cố định theo loại.
    /// </summary>
    internal sealed record LatBieuDo(string Nhan, double GiaTri, string ChuThich, string NhanTrenLat, int MauIndex);
    internal static class Module_TyLeBang2
    {
        private sealed record DaCongBo(CheDoDoiTuong CheDo, KetQuaBang2 KetQua);
        private static volatile DaCongBo? _hien;
        /// <summary>
        /// Kết quả Bảng 2 mới nhất mà Form4 đã tính CHO CHẾ ĐỘ ĐANG CHẠY (CBCS / Tân binh / Học viên).
        /// null nếu Form4 chưa tính lần nào trong phiên, hoặc lần tính cuối thuộc chế độ khác (vừa đổi chế độ).
        /// </summary>
        public static KetQuaBang2? Hien
        {
            get
            {
                var h = _hien;
                return h != null && h.CheDo == Module_CheDoDoiTuong.Hien ? h.KetQua : null;
            }
        }
        /// <summary>Form4 gọi ngay sau khi tính xong Bảng 2 (cùng lúc đổ lên lưới); kết quả gắn với chế độ đang chạy.</summary>
        public static void CongBo(KetQuaBang2 kq) => _hien = new DaCongBo(Module_CheDoDoiTuong.Hien, kq);
        /// <summary>Thông báo dùng chung khi xuất báo cáo mà Bảng 2 của chế độ đang chạy chưa được tính.</summary>
        public const string ThongBaoChuaTinh =
            "Chưa có số liệu Bảng 2 (tỷ lệ %) của chế độ đang chạy. Hãy mở Trang chủ để phần mềm tính Bảng 2 rồi xuất lại.";
        /// <summary>
        /// Quy tắc Bảng 2 (GIỮ NGUYÊN công thức của Form4):
        ///  đủ điều kiện = Tổng − Loại 4 − Không PL;  Loại 2 TỔNG cần đạt = ⌊đủ điều kiện × %Loại 2⌋ (đã gồm Loại 1);
        ///  Loại 3 cần đạt = đủ điều kiện − Loại 2 TỔNG;  Loại 1 cần đạt = ⌊Loại 2 TỔNG × %Loại 1⌋ (Loại 1 nằm trong Loại 2).
        /// </summary>
        /// <param name="loai2Thuan">Loại 2 THUẦN, không gồm Loại 1.</param>
        public static KetQuaBang2 Tinh(int tongQS, int loai1, int loai2Thuan, int loai3, int loai4, int khongPL,
                                       double tyLeLoai1, double tyLeLoai2)
        {
            double loai1Rate = tyLeLoai1 <= 0 ? 0 : tyLeLoai1;
            double loai2Rate = tyLeLoai2 <= 0 ? 0 : tyLeLoai2;
            int duDieuKien = tongQS - loai4 - khongPL;
            if (duDieuKien < 0) duDieuKien = 0;
            int canDatL2Tong = (int)Math.Floor(duDieuKien * loai2Rate / 100.0);
            int canDatL3 = duDieuKien - canDatL2Tong;
            int canDatL1 = (int)Math.Floor(canDatL2Tong * loai1Rate / 100.0);
            return new KetQuaBang2(tongQS, loai1, loai2Thuan, loai3, loai4, khongPL,
                                   duDieuKien, canDatL1, canDatL2Tong, canDatL3, loai1Rate, loai2Rate);
        }
        /// <summary>
        /// Dựng ĐỦ 6 dòng Bảng 2 (ID 1..6, cột như bảng "TyLe" cũ: ID, Thong tin, KQ Hien tai, KQ Can dat, KQ Gui E29, Ket luan).
        /// Form4 lọc bớt dòng không phát sinh để hiển thị; Xuất trình ký dùng nguyên bảng cho khối "Ghi chú"
        /// => Excel luôn khớp Bảng 2 đang hiển thị (bảng TyLe trong CSDL đã ngừng cập nhật, đọc nó sẽ ra số cũ).
        /// "Thong tin" dòng 2..6 ghi nhãn chế độ Tháng (Loại 1..4); nơi dùng tự thay sang nhãn chế độ Năm nếu cần.
        /// </summary>
        public static System.Data.DataTable TaoBangTyLe(KetQuaBang2 kq)
        {
            int tong = kq.TongQS, dk = kq.DuDieuKien;
            int l1 = kq.CanDatLoai1, l2Tong = kq.CanDatLoai2Tong, l3 = kq.CanDatLoai3;
            string fmt(double v) => v.ToString("0.00");
            string thuaThieu(int hienTai, int canDat) =>
                hienTai == canDat ? ""
                : hienTai > canDat ? $"Đang thừa {hienTai - canDat} {Module_HeThong.Tu_dong_chi}"
                : $"Đang thiếu {canDat - hienTai} đồng chí";
            int[] hienTaiArr = { tong, kq.Loai1, kq.Loai2, kq.Loai3, kq.Loai4, kq.KhongPL };
            int[] canDatArr = { tong, l1, l2Tong, l3, 0, 0 };
            // GIỮ NGUYÊN cách tính đã dùng (xem thư mục "Cách tính tỷ lệ %"): Loại 2 / Loại 3 tính trên "đủ điều kiện".
            string[] guiArr =
            {
                $"{tong}",
                l2Tong > 0 ? $"{l1}/{l2Tong} = {fmt(l1 * 100.0 / l2Tong)}%" : "",
                dk > 0 ? $"{l2Tong}/{dk} = {fmt(l2Tong * 100.0 / dk)}%" : "",
                dk > 0 ? $"{l3}/{dk} = {fmt(l3 * 100.0 / dk)}%" : "",
                kq.Loai4 > 0 && tong > 0 ? $"{kq.Loai4:D2}/{dk} = {fmt(kq.Loai4 * 100.0 / tong)}%" : "",
                kq.KhongPL > 0 && tong > 0 ? $"{kq.KhongPL:D2}/{dk} = {fmt(kq.KhongPL * 100.0 / tong)}%" : ""
            };
            bool dat = kq.Loai1 >= l1 && kq.Loai2 >= (l2Tong - l1) && kq.Loai3 >= l3;
            string[] ketLuanArr =
            {
                dat ? "Đạt tỷ lệ quy định" : "Chưa đạt tỷ lệ quy định",
                thuaThieu(kq.Loai1, l1),
                thuaThieu(kq.Loai2, l2Tong - l1),
                thuaThieu(kq.Loai3, l3),
                kq.Loai4 > 0 ? $"Phát sinh {kq.Loai4} {Module_HeThong.Tu_dong_chi}" : "",
                kq.KhongPL > 0 ? $"Phát sinh {kq.KhongPL} {Module_HeThong.Tu_dong_chi}" : ""
            };
            string[] thongTinArr = { "Tổng quân số", Module_HeThong.Loai_1, Module_HeThong.Loai_2,
                                     Module_HeThong.Loai_3, Module_HeThong.Loai_4, Module_HeThong.PL_KHONG_PL };
            var dt = new System.Data.DataTable();
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("Thong tin", typeof(string));
            dt.Columns.Add("KQ Hien tai", typeof(int));
            dt.Columns.Add("KQ Can dat", typeof(int));
            dt.Columns.Add("KQ Gui E29", typeof(string));
            dt.Columns.Add("Ket luan", typeof(string));
            dt.BeginLoadData();
            for (int i = 0; i < 6; i++)
                dt.Rows.Add(i + 1, thongTinArr[i], hienTaiArr[i], canDatArr[i], guiArr[i], ketLuanArr[i]);
            dt.EndLoadData();
            return dt;
        }
        // ===================== BIỂU ĐỒ TRÒN: lát KHÔNG chồng lấn + chú thích theo quy tắc Bảng 2 =====================
        public const string GhiChuChuThich = "Loại 2 gồm cả Loại 1 · % Loại 1 tính trên Loại 2";
        private static string PhanSo(int tu, int mau) =>
            mau > 0 ? $"{tu}/{mau} = {(tu * 100.0 / mau).ToString("0.00")}%" : $"{tu}/{mau}";
        private static string PhanTramNguyen(int tu, int mau) =>
            (mau > 0 ? Math.Round(tu * 100.0 / mau, MidpointRounding.AwayFromZero) : 0).ToString("0", CultureInfo.CurrentCulture) + "%";
        /// <summary>
        /// Dựng danh sách lát cho BieuDoTron3D theo ĐÚNG quy tắc người dùng quy định:
        ///  • Tổng 100% = Loại 2 TỔNG (gồm Loại 1) + Loại 3 + Loại 4 + Không PL = tổng quân số.
        ///  • % Loại 1 = Loại 1 / Loại 2 TỔNG;  % Loại 2 = Loại 2 TỔNG / Tổng;  % Loại 3 / 4 / Không PL = số đó / Tổng.
        ///  • Diện tích: lát "Loại 2" là Loại 2 THUẦN (không cộng chồng lấn với lát Loại 1) nên tổng các lát đúng bằng tổng quân số.
        ///  • Loại 4 và Không PL chỉ hiện khi có phát sinh (&gt; 0).
        /// </summary>
        public static List<LatBieuDo> DungLatBieuDo(int loai1, int loai2Thuan, int loai3, int loai4, int khongPL,
            string nhanLoai1, string nhanLoai2, string nhanLoai3, string nhanLoai4, string nhanKhongPL)
        {
            loai1 = Math.Max(0, loai1); loai2Thuan = Math.Max(0, loai2Thuan); loai3 = Math.Max(0, loai3);
            loai4 = Math.Max(0, loai4); khongPL = Math.Max(0, khongPL);
            int loai2Tong = loai1 + loai2Thuan;
            int tong = loai2Tong + loai3 + loai4 + khongPL;
            var ds = new List<LatBieuDo>(5);
            if (tong <= 0) return ds;
            ds.Add(new LatBieuDo(nhanLoai1, loai1, PhanSo(loai1, loai2Tong), PhanTramNguyen(loai1, loai2Tong), 0));
            ds.Add(new LatBieuDo(nhanLoai2, loai2Thuan, PhanSo(loai2Tong, tong), PhanTramNguyen(loai2Tong, tong), 1));
            ds.Add(new LatBieuDo(nhanLoai3, loai3, PhanSo(loai3, tong), PhanTramNguyen(loai3, tong), 2));
            if (loai4 > 0) ds.Add(new LatBieuDo(nhanLoai4, loai4, PhanSo(loai4, tong), PhanTramNguyen(loai4, tong), 3));
            if (khongPL > 0) ds.Add(new LatBieuDo(nhanKhongPL, khongPL, PhanSo(khongPL, tong), PhanTramNguyen(khongPL, tong), 4));
            return ds;
        }
    }
}