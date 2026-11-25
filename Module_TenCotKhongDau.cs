using System.Diagnostics;
using Microsoft.Data.Sqlite;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Tên bảng / tên cột trong CSDL chỉ dùng chữ KHÔNG DẤU.
    ///
    /// Bản cũ có 2 cột tên có dấu: "SuKien_DuọcChon" (csdl2, bảng SuKien_ThoatPhanMem) và "Chọn_GiaiTri"
    /// (csdl3, bảng TuDong_XoaNhatKy; bản cũ hơn nữa của Form10 còn tạo tên sai "Chon_GiaTri").
    /// CSDL cũ vẫn có thể quay lại qua: bản dự phòng gốc (Database Backup), bản sao lưu (Bansaoluu),
    /// dịch vụ khôi phục, chuyển giao dữ liệu => mỗi lần nạp đường dẫn CSDL và mỗi lần đảm bảo bảng đều đổi tên về
    /// tên mới. Đổi tên bằng ALTER TABLE ... RENAME COLUMN nên giữ nguyên dữ liệu.
    /// </summary>
    internal static class Module_TenCotKhongDau
    {
        public const string BANG_SU_KIEN_THOAT = "SuKien_ThoatPhanMem";
        public const string COT_SU_KIEN_THOAT = "SuKien_DuocChon";
        public const string BANG_TU_DONG_XOA = "TuDong_XoaNhatKy";
        public const string COT_TU_DONG_XOA = "Chon_GiaiTri";
        private static readonly string[] TenCuSuKienThoat = { "SuKien_DuọcChon" };
        private static readonly string[] TenCuTuDongXoa = { "Chọn_GiaiTri", "Chon_GiaTri" };
        /// <summary>Gọi sau khi xác định đường dẫn CSDL (khởi động, sau khôi phục). Không bao giờ ném lỗi.</summary>
        public static void ChuanHoaTatCa()
        {
            ChuanHoaTep(Module_DanduongGPS.DuongDanCSDL2, ChuanHoaCSDL2);
            ChuanHoaTep(Module_DanduongGPS.DuongDanCSDL3, ChuanHoaCSDL3);
            ChuanHoaTep(Module_DanduongGPS.DuongDanCSDL4, ChuanHoaCSDL4);
            ChuanHoaThuMucLichSu();
        }
        /// <summary>csdl4 + tệp năm cũ: bảng thi đua / khen thưởng tên cũ -> tên mới có hậu tố _CBCS/_TanBinh/_HocVien.</summary>
        public static void ChuanHoaCSDL4(SqliteConnection cn)
        {
            foreach (var (cu, moi) in Module_CheDoDoiTuong.BangDoiTenCSDL4) DoiTenBang(cn, cu, moi);
        }
        /// <summary>Một tệp năm cũ (ThiDua_*_Nam*.db / KhenThuong_*_Nam*.db): đổi tên bảng cũ. Không bao giờ ném lỗi.</summary>
        public static void ChuanHoaTepNamCu(string? duongDan) => ChuanHoaTep(duongDan, ChuanHoaCSDL4);
        /// <summary>Mọi tệp .db trong thư mục lịch sử thi đua (LuuTruThiDua_LichSu). Không bao giờ ném lỗi.</summary>
        public static void ChuanHoaThuMucLichSu()
        {
            try
            {
                string dir = Module_DanduongGPS.ThuMucLichSuThiDua;
                if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return;
                foreach (string f in Directory.GetFiles(dir, "*.db")) ChuanHoaTepNamCu(f);
            }
            catch (Exception ex) { Debug.WriteLine("[TenBang] Thư mục lịch sử: " + ex.Message); }
        }
        /// <summary>
        /// Tên bảng thực có trong tệp (chỉ đọc, không đổi được tên - vd tệp người dùng chọn ngoài thư mục):
        /// có bảng tên mới thì dùng tên mới, chỉ có bảng tên cũ thì dùng tên cũ, không có cả hai thì trả tên mới.
        /// </summary>
        public static string TenBangCoTrongTep(SqliteConnection cn, string tenMoi)
        {
            if (CoBang(cn, tenMoi)) return tenMoi;
            foreach (var (cu, moi) in TatCaBangDoiTen())
                if (string.Equals(moi, tenMoi, StringComparison.OrdinalIgnoreCase) && CoBang(cn, cu)) return cu;
            return tenMoi;
        }
        /// <summary>
        /// Bảng thừa đã xóa khỏi CSDL (2026-11-23, 2026-11-24; phần mềm không còn đọc/ghi): (tên CSDL không đuôi, tên bảng).
        /// Gói chuyển giao / CSDL của bản cũ có thể còn các bảng này => nạp thì bỏ qua thay vì báo lỗi.
        /// </summary>
        private static readonly (string Csdl, string Bang)[] BangDaLoaiBo =
        {
            ("csdl2", "Admin"), ("csdl2", "CauHinh_Font"), ("csdl2", "CauHinh_ThoiGianAnh"), ("csdl2", "CoChuHeThong"),
            ("csdl2", "DanhSachTinh"), ("csdl2", "Xa"), ("csdl2", "ThongKe_PhanLoaiTapThe"),
            // 2026-11-24: Tinh (thay bằng TinhVaThanhPho), TyLe (Bảng 2 tính trên RAM qua Module_TyLeBang2)
            ("csdl2", "Tinh"), ("csdl2", "TyLe"),
            ("csdl4", "Admin"), ("csdl4", "ChiHuyD"), ("csdl4", "DanhSach"), ("csdl4", "HoSoTepMaHoa"),
        };
        public static bool LaBangDaLoaiBo(string tenCSDL, string tenBang)
        {
            foreach (var (c, b) in BangDaLoaiBo)
                if (string.Equals(c, tenCSDL, StringComparison.OrdinalIgnoreCase) && string.Equals(b, tenBang, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
        private static IEnumerable<(string Cu, string Moi)> TatCaBangDoiTen() =>
            Module_CheDoDoiTuong.BangDoiTenCSDL2.Concat(Module_CheDoDoiTuong.BangDoiTenCSDL4);
        /// <summary>
        /// csdl2: SuKien_ThoatPhanMem.SuKien_DuọcChon -> SuKien_DuocChon;
        /// bảng theo chế độ tên cũ -> tên mới có hậu tố _CBCS/_TanBinh/_HocVien (DanhSach -> DanhSach_CBCS, DanhSachTanBinh -> DanhSach_TanBinh...).
        /// </summary>
        public static void ChuanHoaCSDL2(SqliteConnection cn)
        {
            DoiTenCot(cn, BANG_SU_KIEN_THOAT, COT_SU_KIEN_THOAT, TenCuSuKienThoat);
            foreach (var (cu, moi) in Module_CheDoDoiTuong.BangDoiTenCSDL2) DoiTenBang(cn, cu, moi);
        }
        /// <summary>Tên bảng cũ (gói chuyển giao / CSDL của bản cũ) -> tên bảng hiện tại; không phải tên cũ thì trả nguyên.</summary>
        public static string TenBangHienTai(string tenBang)
        {
            foreach (var (cu, moi) in TatCaBangDoiTen())
                if (string.Equals(tenBang, cu, StringComparison.OrdinalIgnoreCase)) return moi;
            return tenBang;
        }
        /// <summary>
        /// Đổi tên bảng cũ -> mới, giữ nguyên dữ liệu. Đã có cả hai: bảng mới rỗng thì bỏ bảng mới rồi đổi tên bảng cũ;
        /// bảng mới đã có dữ liệu thì giữ bảng mới và để nguyên bảng cũ (không tự xóa dữ liệu của người dùng).
        /// </summary>
        private static void DoiTenBang(SqliteConnection cn, string cu, string moi)
        {
            if (string.Equals(cu, moi, StringComparison.OrdinalIgnoreCase)) return;
            bool coCu = CoBang(cn, cu), coMoi = CoBang(cn, moi);
            if (!coCu) return;
            using var tx = cn.BeginTransaction();
            using var cmd = cn.CreateCommand();
            cmd.Transaction = tx;
            if (coMoi)
            {
                cmd.CommandText = $"SELECT EXISTS(SELECT 1 FROM {Q(moi)});";
                if (Convert.ToInt64(cmd.ExecuteScalar()) != 0)
                {
                    Debug.WriteLine($"[TenBang] Có cả {cu} và {moi} (đều có dữ liệu): giữ {moi}, để nguyên {cu}.");
                    return;
                }
                cmd.CommandText = $"DROP TABLE {Q(moi)};";
                cmd.ExecuteNonQuery();
            }
            cmd.CommandText = $"ALTER TABLE {Q(cu)} RENAME TO {Q(moi)};";
            cmd.ExecuteNonQuery();
            tx.Commit();
        }
        private static bool CoBang(SqliteConnection cn, string ten)
        {
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name = @t COLLATE NOCASE LIMIT 1;";
            cmd.Parameters.AddWithValue("@t", ten);
            return cmd.ExecuteScalar() != null;
        }
        /// <summary>csdl3: TuDong_XoaNhatKy.Chọn_GiaiTri / Chon_GiaTri -> Chon_GiaiTri.</summary>
        public static void ChuanHoaCSDL3(SqliteConnection cn) =>
            DoiTenCot(cn, BANG_TU_DONG_XOA, COT_TU_DONG_XOA, TenCuTuDongXoa);
        /// <summary>
        /// Tên cột cũ (có dấu) trong dữ liệu đưa vào từ ngoài (tệp JSON chuyển giao xuất từ CSDL cũ) -> tên cột hiện tại.
        /// Không phải tên cũ thì trả lại nguyên tên.
        /// </summary>
        public static string TenCotHienTai(string tenBang, string tenCot)
        {
            if (string.Equals(tenBang, BANG_SU_KIEN_THOAT, StringComparison.OrdinalIgnoreCase)
                && Array.IndexOf(TenCuSuKienThoat, tenCot) >= 0)
                return COT_SU_KIEN_THOAT;
            if (string.Equals(tenBang, BANG_TU_DONG_XOA, StringComparison.OrdinalIgnoreCase)
                && Array.IndexOf(TenCuTuDongXoa, tenCot) >= 0)
                return COT_TU_DONG_XOA;
            return tenCot;
        }
        private static void ChuanHoaTep(string? duongDan, Action<SqliteConnection> chuanHoa)
        {
            if (string.IsNullOrWhiteSpace(duongDan) || !File.Exists(duongDan)) return;
            try
            {
                // Pooling=false: đóng là nhả hẳn tệp (sao lưu / khôi phục / di chuyển tệp ngay sau đó không bị khóa)
                using var cn = Module_KetNoiCSDL.MoKetNoi(duongDan, khongTaoMoi: true, pooling: false, thoiGianCho: 5);
                chuanHoa(cn);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TenCotKhongDau] {Path.GetFileName(duongDan)}: {ex.Message}");
            }
        }
        private static void DoiTenCot(SqliteConnection cn, string bang, string tenMoi, string[] tenCu)
        {
            var cot = new List<string>();
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "SELECT name FROM pragma_table_info(@bang);";
                cmd.Parameters.AddWithValue("@bang", bang);
                using var rd = cmd.ExecuteReader();
                while (rd.Read()) cot.Add(rd.GetString(0));
            }
            if (cot.Count == 0) return; // chưa có bảng: nơi tạo bảng sẽ tạo luôn tên mới
            // So khớp như SQLite: không phân biệt hoa/thường với chữ ASCII
            bool coTenMoi = cot.Contains(tenMoi, StringComparer.OrdinalIgnoreCase);
            foreach (string cu in tenCu)
            {
                if (!cot.Contains(cu, StringComparer.Ordinal)) continue;
                using var cmd = cn.CreateCommand();
                if (!coTenMoi)
                {
                    cmd.CommandText = $"ALTER TABLE {Q(bang)} RENAME COLUMN {Q(cu)} TO {Q(tenMoi)};";
                    cmd.ExecuteNonQuery();
                    coTenMoi = true;
                }
                else
                {
                    // Cả tên cũ lẫn tên mới cùng tồn tại: giữ giá trị cũ cho dòng chưa có giá trị mới rồi bỏ cột cũ
                    using var tx = cn.BeginTransaction();
                    cmd.Transaction = tx;
                    cmd.CommandText = $"UPDATE {Q(bang)} SET {Q(tenMoi)} = {Q(cu)} WHERE {Q(tenMoi)} IS NULL;";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = $"ALTER TABLE {Q(bang)} DROP COLUMN {Q(cu)};";
                    cmd.ExecuteNonQuery();
                    tx.Commit();
                }
            }
        }
        private static string Q(string ten) => "\"" + ten.Replace("\"", "\"\"") + "\"";
    }
}
