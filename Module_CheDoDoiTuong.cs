using Microsoft.Data.Sqlite;
using System.Diagnostics;
namespace PhanMemThiDua2026
{
    /// <summary>Ba chế độ đối tượng của phần mềm (PhienBan_DoiTuong trong CSDL2).</summary>
    public enum CheDoDoiTuong { CBCS, TanBinh, HocVien }

    /// <summary>
    /// NƠI DUY NHẤT quyết định tên bảng theo chế độ. Mỗi chế độ có bộ bảng RIÊNG, cùng một mẫu "TênGốc_HậuTố":
    ///   CBCS     : hậu tố _CBCS    (DanhSach_CBCS, ThiDuaThang_CBCS, ThongKe_GiayKhen_CBCS, ...).
    ///   Tân binh : hậu tố _TanBinh (DanhSach_TanBinh, ThiDuaThang_TanBinh, ...).
    ///   Học viên : hậu tố _HocVien (DanhSach_HocVien, ThiDuaThang_HocVien, ...).
    /// CSDL / tệp năm cũ / gói chuyển giao của bản trước mang tên cũ (DanhSach, DanhSachTanBinh, ThongKeCBCS_DuocKhenThuong...)
    /// => tự đổi sang tên mới theo BangDoiTenCSDL2 / BangDoiTenCSDL4 (Module_TenCotKhongDau).
    /// Học viên và Tân binh dùng CHUNG LOGIC nghiệp vụ (xét theo tuần/tháng 2-6, không BCH, không xét năm,
    /// không thống kê thi đua tập thể) nên LaPhienBanTanBinh()/LaCheDoTanBinh() vẫn trả true cho cả hai;
    /// chỉ DỮ LIỆU là tách riêng. Không bao giờ viết cứng "..._TanBinh" ở form/module: luôn gọi qua đây.
    /// </summary>
    internal static class Module_CheDoDoiTuong
    {
        public const string TEN_CBCS = "Phiên bản dành cho CBCS";
        public const string TEN_TANBINH = "Phiên bản dành cho tân binh";
        public const string TEN_HOCVIEN = "Phiên bản dành cho học viên";

        /// <summary>Nhận chế độ từ chuỗi phiên bản (có thể có tiền tố "Phần mềm: ").</summary>
        public static CheDoDoiTuong TuChuoi(string? phienBan)
        {
            if (string.IsNullOrEmpty(phienBan)) return CheDoDoiTuong.CBCS;
            if (phienBan.Contains("học viên", StringComparison.OrdinalIgnoreCase)) return CheDoDoiTuong.HocVien;
            if (phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase)) return CheDoDoiTuong.TanBinh;
            return CheDoDoiTuong.CBCS;
        }

        /// <summary>Chế độ đang chạy (dùng chung bộ đệm 5 giây của Module_TaiKhoan).</summary>
        public static CheDoDoiTuong Hien => Module_TaiKhoan.CheDoHienTai;

        // ===================== TÊN BẢNG =====================
        // CSDL2
        public static string DanhSach(CheDoDoiTuong c) => Chon(c, "DanhSach_CBCS", "DanhSach_TanBinh", "DanhSach_HocVien");
        public static string ChiHuyD(CheDoDoiTuong c) => Chon(c, "ChiHuyD_CBCS", "ChiHuyD_TanBinh", "ChiHuyD_HocVien");
        public static string DonVi(CheDoDoiTuong c) => Chon(c, "DanhSach_DonVi_CBCS", "DanhSach_DonVi_TanBinh", "DanhSach_DonVi_HocVien");
        public static string QuyDinhTyLe(CheDoDoiTuong c) => Chon(c, "QuyDinhTyLe_CBCS", "QuyDinhTyLe_TanBinh", "QuyDinhTyLe_HocVien");
        public static string BaNhat(CheDoDoiTuong c) => Chon(c, "DanhSachBaNhat_CBCS", "DanhSachBaNhat_TanBinh", "DanhSachBaNhat_HocVien");
        public static string SoVangBaNhat(CheDoDoiTuong c) => Chon(c, "DanhSach_SoVangBaNhat_CBCS", "DanhSach_SoVangBaNhat_TanBinh", "DanhSach_SoVangBaNhat_HocVien");
        public static string TomTatBaNhat(CheDoDoiTuong c) => Chon(c, "TomTatThanhTichBaNhat_CBCS", "TomTatThanhTichBaNhat_TanBinh", "TomTatThanhTichBaNhat_HocVien");
        /// <summary>Đơn vị được bỏ qua cảnh báo tỷ lệ ở Bảng 1 Trang chủ (Form40) - cả 3 chế độ, mỗi chế độ danh sách riêng.</summary>
        public static string BoQuaCanhBao(CheDoDoiTuong c) => Chon(c, "ChonDonVi_DeBoQuaCanhBao_CBCS", "ChonDonVi_DeBoQuaCanhBao_TanBinh", "ChonDonVi_DeBoQuaCanhBao_HocVien");
        /// <summary>Quân số Bảng 1 Trang chủ (Form4 ghi lại mỗi lần tính) - cả 3 chế độ, mỗi chế độ bảng riêng.</summary>
        public static string QuanSo(CheDoDoiTuong c) => Chon(c, "QuanSoThiDuaD2_CBCS", "QuanSoThiDuaD2_TanBinh", "QuanSoThiDuaD2_HocVien");
        /// <summary>Loại báo cáo Tháng/Tuần - CHỈ Tân binh, Học viên (CBCS luôn báo cáo theo tháng => null, không có bảng).</summary>
        public static string? ChonLoaiBaoCao(CheDoDoiTuong c) => c == CheDoDoiTuong.CBCS ? null : Chon(c, "", "ChonLoaiBaoCao_TanBinh", "ChonLoaiBaoCao_HocVien");

        // Bảng CHỈ chế độ CBCS dùng (Tân binh/Học viên không có BCH, không xét năm, không thống kê tập thể) - vẫn mang hậu tố _CBCS.
        /// <summary>csdl2: tỷ lệ quy định Ban chỉ huy (Form41).</summary>
        public const string CBCS_QuyDinhTyLeBCH = "QuyDinhTyLe_BCH_CBCS";
        /// <summary>csdl2: tỷ lệ xét thi đua năm (Form61).</summary>
        public const string CBCS_QuyDinhTyLeXetNam = "QuyDinhTyLe_XetThiDuaNam_CBCS";
        /// <summary>csdl2: chế độ xét thi đua "Tháng"/"Năm".</summary>
        public const string CBCS_CheDoXetNam = "CheDo_XetThiDuaNam_CBCS";
        /// <summary>csdl4 + tệp ThiDua_CBCS_Nam*.db: phân loại thi đua tập thể (Form23/54/59).</summary>
        public const string CBCS_PhanLoaiTapThe = "ThongKe_PhanLoaiTapThe_CBCS";
        // CSDL4 (và tệp lưu trữ năm cũ)
        /// <summary>Cấu hình lịch xét nhóm tuần (csdl2) - chỉ Tân binh / Học viên; CBCS trả "".</summary>
        public static string CauHinhLichXet(CheDoDoiTuong c) => Chon(c, "", "CauHinhLichXet_TanBinh", "CauHinhLichXet_HocVien");
        public static string ThiDuaThang(CheDoDoiTuong c) => Chon(c, "ThiDuaThang_CBCS", "ThiDuaThang_TanBinh", "ThiDuaThang_HocVien");

        // CSDL4 + tệp năm cũ KhenThuong_*_Nam*.db: KHEN THƯỞNG (cả 3 chế độ đều có, mỗi chế độ bộ bảng riêng)
        public static string KhenThuongNguoi(CheDoDoiTuong c) => Chon(c, "ThongKe_DuocKhenThuong_CBCS", "ThongKe_DuocKhenThuong_TanBinh", "ThongKe_DuocKhenThuong_HocVien");
        public static string GiayKhen(CheDoDoiTuong c) => Chon(c, "ThongKe_GiayKhen_CBCS", "ThongKe_GiayKhen_TanBinh", "ThongKe_GiayKhen_HocVien");
        public static string KhenThuongTapThe(CheDoDoiTuong c) => Chon(c, "ThongKe_KhenThuongTapThe_CBCS", "ThongKe_KhenThuongTapThe_TanBinh", "ThongKe_KhenThuongTapThe_HocVien");
        /// <summary>Chỉ mục tên tập thể (tên chỉ mục trong SQLite là duy nhất toàn tệp nên phải khác nhau theo chế độ).</summary>
        public static string ChiMucTapThe(CheDoDoiTuong c) => Chon(c, "idx_kt_tentapthe", "idx_kt_tentapthe_tanbinh", "idx_kt_tentapthe_hocvien");
        /// <summary>Tiền tố tệp khen thưởng năm cũ: KhenThuong_CBCS_Nam / KhenThuong_TanBinh_Nam / KhenThuong_HocVien_Nam.</summary>
        public static string TienToTepKhenThuong(CheDoDoiTuong c) => $"KhenThuong_{MaTep(c)}_Nam";

        // Theo chế độ đang chạy
        public static string BangKhenThuongNguoi => KhenThuongNguoi(Hien);
        public static string BangGiayKhen => GiayKhen(Hien);
        public static string BangKhenThuongTapThe => KhenThuongTapThe(Hien);
        public static string BangChiMucTapThe => ChiMucTapThe(Hien);
        public static string TienToTepKhenThuongHienTai => TienToTepKhenThuong(Hien);
        /// <summary>Tên đối tượng hiển thị: "CBCS" / "Tân binh" / "Học viên".</summary>
        public static string TenDoiTuong(CheDoDoiTuong c) => Chon(c, "CBCS", "Tân binh", "Học viên");
        public static string TenDoiTuongHienTai => TenDoiTuong(Hien);

        // ===================== NHÃN GIAO DIỆN THEO CHẾ ĐỘ =====================
        // Chữ "CBCS" đứng riêng (không dính chữ/số/_ : "DanhSachCBCS", "ThongKeCBCS_" giữ nguyên)
        private static readonly System.Text.RegularExpressions.Regex _chuCBCS =
            new(@"(?<![\p{L}\p{N}_])CBCS(?![\p{L}\p{N}_])", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        /// <summary>Đổi chữ "CBCS" trong nhãn thành "Tân binh"/"Học viên" theo chế độ đang chạy (CBCS: giữ nguyên).</summary>
        public static string DoiChuCBCS(string? s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? "";
            var c = Hien;
            return c == CheDoDoiTuong.CBCS ? s : _chuCBCS.Replace(s, TenDoiTuong(c));
        }
        /// <summary>
        /// Đổi nhãn do Designer sinh (Text, mục menu, tiêu đề cột lưới) của cả cây control theo chế độ đang chạy.
        /// Gọi NGAY SAU InitializeComponent (trước khi giao diện hiện đại chép tiêu đề GroupBox sang thẻ).
        /// Không sửa tệp Designer; form bị hủy và dựng lại khi đổi chế độ nên không cần khôi phục chữ cũ.
        /// </summary>
        public static void ApNhanTheoCheDo(Control goc)
        {
            if (goc == null || Hien == CheDoDoiTuong.CBCS) return;
            void DoiItem(ToolStripItem it)
            {
                it.Text = DoiChuCBCS(it.Text);
                if (!string.IsNullOrEmpty(it.ToolTipText)) it.ToolTipText = DoiChuCBCS(it.ToolTipText);
                if (it is ToolStripDropDownItem dd) foreach (ToolStripItem con in dd.DropDownItems) DoiItem(con);
            }
            void Duyet(Control c)
            {
                if (!string.IsNullOrEmpty(c.Text)) c.Text = DoiChuCBCS(c.Text);
                if (c is ToolStrip ts) foreach (ToolStripItem it in ts.Items) DoiItem(it);
                if (c.ContextMenuStrip != null) foreach (ToolStripItem it in c.ContextMenuStrip.Items) DoiItem(it);
                if (c is DataGridView g) foreach (DataGridViewColumn col in g.Columns) col.HeaderText = DoiChuCBCS(col.HeaderText);
                foreach (Control con in c.Controls) Duyet(con);
            }
            Duyet(goc);
        }
        public static string BangDanhSach => DanhSach(Hien);
        public static string BangChiHuyD => ChiHuyD(Hien);
        public static string BangDonVi => DonVi(Hien);
        public static string BangQuyDinhTyLe => QuyDinhTyLe(Hien);
        public static string BangBaNhat => BaNhat(Hien);
        public static string BangSoVangBaNhat => SoVangBaNhat(Hien);
        public static string BangTomTatBaNhat => TomTatBaNhat(Hien);
        public static string BangBoQuaCanhBao => BoQuaCanhBao(Hien);
        public static string BangQuanSo => QuanSo(Hien);
        public static string? BangChonLoaiBaoCao => ChonLoaiBaoCao(Hien);
        public static string BangThiDuaThang => ThiDuaThang(Hien);

        /// <summary>Tất cả bảng Sổ vàng Ba nhất hợp lệ (chống chèn tên bảng lạ).</summary>
        public static readonly string[] TatCaBangSoVang =
            { SoVangBaNhat(CheDoDoiTuong.CBCS), SoVangBaNhat(CheDoDoiTuong.TanBinh), SoVangBaNhat(CheDoDoiTuong.HocVien) };

        // ===================== TÊN BẢNG CŨ (bản trước 2026-11-23) -> TÊN MỚI =====================
        // Không xóa dòng nào ở đây: CSDL cũ vẫn có thể quay lại qua dự phòng gốc, sao lưu, khôi phục, chuyển giao.
        /// <summary>CSDL2: (tên cũ, tên mới).</summary>
        public static readonly (string Cu, string Moi)[] BangDoiTenCSDL2 =
        {
            ("DanhSach_DonVi", DonVi(CheDoDoiTuong.CBCS)),
            ("DanhSach", DanhSach(CheDoDoiTuong.CBCS)),
            ("DanhSachTanBinh", DanhSach(CheDoDoiTuong.TanBinh)),
            ("DanhSachHocVien", DanhSach(CheDoDoiTuong.HocVien)),
            ("ChiHuyD", ChiHuyD(CheDoDoiTuong.CBCS)),
            ("QuyDinhTyLe", QuyDinhTyLe(CheDoDoiTuong.CBCS)),
            ("DanhSachBaNhat", BaNhat(CheDoDoiTuong.CBCS)),
            ("DanhSach_SoVangBaNhat", SoVangBaNhat(CheDoDoiTuong.CBCS)),
            ("DanhSachTanBinh_SoVangBaNhat", SoVangBaNhat(CheDoDoiTuong.TanBinh)),
            ("DanhSachHocVien_SoVangBaNhat", SoVangBaNhat(CheDoDoiTuong.HocVien)),
            ("ChonDonVi_DeBoQuaCanhBao", BoQuaCanhBao(CheDoDoiTuong.CBCS)),
            // 2026-11-24: bảng chỉ CBCS thêm hậu tố; bảng dùng chung nhưng dữ liệu theo chế độ tách riêng
            ("QuyDinhTyLeBCH", CBCS_QuyDinhTyLeBCH),
            ("QuyDinhTyLe_XetThiDuaNam", CBCS_QuyDinhTyLeXetNam),
            ("CheDo_XetThiDuaNam", CBCS_CheDoXetNam),
            ("QuanSoThiDuaD2", QuanSo(CheDoDoiTuong.CBCS)),             // bộ đệm, Form4 tính lại mỗi lần
            ("ChonLoaiBaoCao", ChonLoaiBaoCao(CheDoDoiTuong.TanBinh)!), // chỉ Tân binh/Học viên dùng; Học viên tạo mới
        };
        /// <summary>CSDL4 + tệp năm cũ (ThiDua_*_Nam*.db, KhenThuong_*_Nam*.db): (tên cũ, tên mới).</summary>
        public static readonly (string Cu, string Moi)[] BangDoiTenCSDL4 =
        {
            ("ThiDuaThang", ThiDuaThang(CheDoDoiTuong.CBCS)),
            ("ThongKeCBCS_DuocKhenThuong", KhenThuongNguoi(CheDoDoiTuong.CBCS)),
            ("ThongKeTanBinh_DuocKhenThuong", KhenThuongNguoi(CheDoDoiTuong.TanBinh)),
            ("ThongKeHocVien_DuocKhenThuong", KhenThuongNguoi(CheDoDoiTuong.HocVien)),
            ("ThongKe_GiayKhen", GiayKhen(CheDoDoiTuong.CBCS)),
            ("ThongKe_KhenThuongTapThe", KhenThuongTapThe(CheDoDoiTuong.CBCS)),
            ("ThongKe_PhanLoaiTapThe", CBCS_PhanLoaiTapThe),
        };

        // ===================== TỆP LƯU TRỮ NĂM CŨ =====================
        /// <summary>"CBCS" / "TanBinh" / "HocVien" - dùng trong tên tệp ThiDua_{x}_Nam{nam}.db.</summary>
        public static string MaTep(CheDoDoiTuong c) => Chon(c, "CBCS", "TanBinh", "HocVien");
        /// <summary>Nhận chế độ từ tên tệp năm cũ (ThiDua_HocVien_Nam2025.db ...). Tệp không rõ => CBCS.</summary>
        public static CheDoDoiTuong TuTenTep(string? tenTep)
        {
            // Chỉ xét TÊN tệp (không xét thư mục): ThiDua_HocVien_Nam2025.db, KhenThuong_TanBinh_Nam2025.db...
            string t = Path.GetFileName(tenTep ?? "");
            if (t.Contains("HocVien", StringComparison.OrdinalIgnoreCase)) return CheDoDoiTuong.HocVien;
            if (t.Contains("TanBinh", StringComparison.OrdinalIgnoreCase)) return CheDoDoiTuong.TanBinh;
            return CheDoDoiTuong.CBCS;
        }
        /// <summary>Chế độ có xét theo tuần (Tân binh, Học viên) hay theo tháng/năm (CBCS).</summary>
        public static bool LaNhomTuan(CheDoDoiTuong c) => c != CheDoDoiTuong.CBCS;

        // ===================== MÃ TẠM (người chưa có Số hiệu) =====================
        /// <summary>Tiền tố mã tạm phần mềm tự cấp: Tân binh "TB", Học viên "HV". CBCS dùng Số hiệu thật => "".</summary>
        public static string TienToMaTam(CheDoDoiTuong c) => Chon(c, "", "TB", "HV");
        public static string TienToMaTam() => TienToMaTam(Hien);
        /// <summary>Mã tạm thứ n: TB00001, HV00001...</summary>
        public static string TaoMaTam(CheDoDoiTuong c, int n) => $"{TienToMaTam(c)}{n:D5}";
        /// <summary>Chuỗi có phải mã tạm của chế độ này không (không phân biệt hoa/thường; nhận cả mã "ID....." của dữ liệu cũ).</summary>
        public static bool LaMaTam(CheDoDoiTuong c, string? soHieu)
        {
            if (string.IsNullOrWhiteSpace(soHieu) || c == CheDoDoiTuong.CBCS) return false;
            string s = soHieu.Trim();
            return s.StartsWith(TienToMaTam(c), StringComparison.OrdinalIgnoreCase)
                || s.StartsWith("ID", StringComparison.OrdinalIgnoreCase);
        }
        /// <summary>Chuẩn hóa mã người dùng gõ: "5" -> "TB00005"; "tb5" -> "TB00005"; mã cũ "ID00005" giữ nguyên.</summary>
        public static string ChuanHoaMaTam(CheDoDoiTuong c, string? nhap)
        {
            string s = (nhap ?? "").Trim();
            if (s.Length == 0 || c == CheDoDoiTuong.CBCS) return s;
            string tt = TienToMaTam(c);
            if (s.StartsWith("ID", StringComparison.OrdinalIgnoreCase)) return s.ToUpperInvariant();
            string phanSo = s.StartsWith(tt, StringComparison.OrdinalIgnoreCase) ? s.Substring(tt.Length) : s;
            return int.TryParse(phanSo, out int n) && n > 0 ? TaoMaTam(c, n) : tt + phanSo;
        }

        private static string Chon(CheDoDoiTuong c, string cbcs, string tanBinh, string hocVien) => c switch
        {
            CheDoDoiTuong.TanBinh => tanBinh,
            CheDoDoiTuong.HocVien => hocVien,
            _ => cbcs
        };

        // ===================== TẠO BẢNG =====================
        // Bảng của Tân binh / Học viên cùng cấu trúc; CBCS đã có sẵn trong CSDL mẫu (đổi tên từ bảng cũ) nên không tạo ở đây.
        private static string DdlDanhSach(string ten) => $@"CREATE TABLE IF NOT EXISTS ""{ten}"" (
    ""ID"" INTEGER NOT NULL, ""STT"" INTEGER NOT NULL, ""HoVaTen"" TEXT NOT NULL, ""SoHieu"" TEXT NOT NULL,
    ""NamSinh"" TEXT NOT NULL, ""QueQuan"" TEXT, ""NgayVaoCAND"" TEXT, ""CapBac"" TEXT, ""ChucVu"" TEXT,
    ""DonVi"" TEXT, ""PhanLoai"" TEXT, ""GhiChu"" TEXT, ""ThuTuSapXep"" INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY(""ID"" AUTOINCREMENT));";
        private static string DdlChiHuy(string ten) => $@"CREATE TABLE IF NOT EXISTS ""{ten}"" (
    ""ID"" INTEGER NOT NULL, ""HoVaTen"" TEXT NOT NULL, ""ChucVu"" TEXT NOT NULL, PRIMARY KEY(""ID"" AUTOINCREMENT));";
        private static string DdlDonVi(string ten) => $@"CREATE TABLE IF NOT EXISTS ""{ten}"" (
    ""ID"" INTEGER NOT NULL, ""Ten_DonVi"" TEXT, ""ThoiGian"" TEXT, PRIMARY KEY(""ID"" AUTOINCREMENT));";
        private static string DdlQuyDinhTyLe(string ten) => $@"CREATE TABLE IF NOT EXISTS ""{ten}"" (
    ID INTEGER NOT NULL, TenLoaiTapThe TEXT, Loai_1 TEXT, Loai_2 TEXT, Loai_3 TEXT, Loai_4 TEXT, Khong_PL TEXT,
    PRIMARY KEY(ID AUTOINCREMENT));
INSERT OR IGNORE INTO ""{ten}"" (ID, TenLoaiTapThe, Loai_1, Loai_2, Loai_3, Loai_4, Khong_PL) VALUES
    (1, 'Loại 1', '0', '0', '0', '0', '0'), (2, 'Loại 2', '0', '0', '0', '0', '0'), (3, 'Loại 3', '0', '0', '0', '0', '0');";
        private static string DdlBaNhat(string ten) => $@"CREATE TABLE IF NOT EXISTS ""{ten}"" (
    ""ID"" INTEGER NOT NULL, ""STT"" INTEGER NOT NULL, ""HoVaTen"" TEXT NOT NULL, ""SoHieu"" TEXT NOT NULL,
    ""NamSinh"" TEXT NOT NULL, ""QueQuan"" TEXT, ""NgayVaoCAND"" TEXT, ""CapBac"" TEXT, ""ChucVu"" TEXT,
    ""DonVi"" TEXT, ""PhanLoai"" TEXT, ""GhiChu"" TEXT, ""DeNghi"" TEXT, ""ThanhTich"" TEXT, ""TinhTrang"" TEXT,
    PRIMARY KEY(""ID"" AUTOINCREMENT));";
        private static string DdlSoVang(string ten) => $@"CREATE TABLE IF NOT EXISTS ""{ten}"" (
    ""ID"" INTEGER NOT NULL, ""STT"" INTEGER NOT NULL, ""HoVaTen"" TEXT NOT NULL, ""SoHieu"" TEXT NOT NULL,
    ""NamSinh"" TEXT NOT NULL, ""QueQuan"" TEXT, ""NgayVaoCAND"" TEXT, ""CapBac"" TEXT, ""ChucVu"" TEXT,
    ""DonVi"" TEXT, ""PhanLoai"" TEXT, ""GhiChu"" TEXT, ""ThanhTich"" TEXT, ""ThongBaoTrungDoan"" TEXT,
    ""SoTTTrongSo"" TEXT, ""ThangCongNhan"" TEXT, PRIMARY KEY(""ID"" AUTOINCREMENT));";
        /// <summary>Bảng đơn vị bỏ qua cảnh báo tỷ lệ (cùng lược đồ bảng CBCS Form40 xưa nay vẫn tạo).</summary>
        public static string DdlBoQuaCanhBao(string ten) => $@"CREATE TABLE IF NOT EXISTS ""{ten}"" (
    ""ID"" INTEGER NOT NULL, ""Ten_DonViBoQuaCanhBaoTyLe"" TEXT, PRIMARY KEY(""ID"" AUTOINCREMENT));";
        private static string DdlQuanSo(string ten) => $@"CREATE TABLE IF NOT EXISTS ""{ten}"" (
    ""ID"" INTEGER NOT NULL, ""DonVi"" TEXT, ""TongQS"" TEXT, ""Loai_1"" TEXT, ""Loai_2"" TEXT, ""Loai_3"" TEXT, ""Loai_4"" TEXT,
    ""Khong_PL"" TEXT, ""PhanTramLoai_1"" TEXT, ""PhanTramLoai_2"" TEXT, ""PhanTramLoai_3"" TEXT, ""PhanTramLoai_4"" TEXT,
    ""PhanTramKhong_PL"" TEXT, PRIMARY KEY(""ID"" AUTOINCREMENT));";
        private static string DdlChonLoaiBaoCao(string ten) => $@"CREATE TABLE IF NOT EXISTS ""{ten}"" (
    ""ID"" INTEGER NOT NULL, ""ChonLoaiBaoCao"" TEXT NOT NULL, ""ChonTuan"" TEXT NOT NULL, PRIMARY KEY(""ID"" AUTOINCREMENT));";
        private static string DdlTomTat(string ten) => $@"CREATE TABLE IF NOT EXISTS ""{ten}"" (
    ""ID"" INTEGER NOT NULL, ""NoiDung"" TEXT, PRIMARY KEY(""ID"" AUTOINCREMENT));";
        /// <summary>Bảng thi đua nhóm tuần (Tân binh / Học viên): lược đồ theo Module_LichXet.</summary>
        public static string DdlThiDuaThangNhomTuan(string ten) => Module_LichXet.DdlThiDuaThang(ten);

        private static readonly HashSet<string> _daDamBao = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Bảo đảm đủ bộ bảng của chế độ <paramref name="c"/> (Tân binh/Học viên) trong CSDL2 và CSDL4.
        /// Mỗi tệp + chế độ chỉ chạy 1 lần mỗi phiên; lỗi (CSDL đang khóa) thì lần sau thử lại. CBCS: không làm gì.
        /// </summary>
        public static void DamBaoBang(CheDoDoiTuong c, string? csdl2, string? csdl4)
        {
            // Khen thưởng: cả 3 chế độ (CSDL cũ có thể thiếu bảng/cột khen thưởng, kể cả của CBCS)
            Module_BangKhenThuong.DamBaoTrongTep(csdl4, c);
            // Đơn vị bỏ qua cảnh báo tỷ lệ: cả 3 chế độ (CSDL cũ chưa có bảng của Tân binh/Học viên)
            // Quân số Bảng 1: cả 3 chế độ
            DamBaoMotTep(c, csdl2, "2bq", new[] { DdlBoQuaCanhBao(BoQuaCanhBao(c)), DdlQuanSo(QuanSo(c)) });
            if (c == CheDoDoiTuong.CBCS) return;
            DamBaoMotTep(c, csdl2, "2", new[]
            {
                DdlDanhSach(DanhSach(c)), DdlChiHuy(ChiHuyD(c)), DdlDonVi(DonVi(c)), DdlQuyDinhTyLe(QuyDinhTyLe(c)),
                DdlBaNhat(BaNhat(c)), DdlSoVang(SoVangBaNhat(c)), DdlTomTat(TomTatBaNhat(c)),
                DdlChonLoaiBaoCao(ChonLoaiBaoCao(c)!),
            });
            // Bảng thi đua 6 đợt (lược đồ cũ của Tân binh tự nâng cấp) + bảng cấu hình lịch xét (Module_LichXet)
            DamBaoMotTep(c, csdl4, "4", new[] { DdlThiDuaThangNhomTuan(ThiDuaThang(c)) },
                (cn, tran) => Module_LichXet.NangCapBang(cn, tran, ThiDuaThang(c)));
            DamBaoMotTep(c, csdl2, "2lx", new[] { Module_LichXet.DdlCauHinh(c) },
                (cn, tran) => Module_LichXet.BoSungCotCauHinh(cn, tran, c));
        }
        private static void DamBaoMotTep(CheDoDoiTuong c, string? db, string loai, string[] lenh,
            Action<SqliteConnection, SqliteTransaction>? sauKhiTao = null)
        {
            if (string.IsNullOrWhiteSpace(db) || !File.Exists(db)) return;
            string khoa = c + "|" + loai + "|" + db;
            lock (_daDamBao)
            {
                if (_daDamBao.Contains(khoa)) return;
                try
                {
                    using var cn = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = db, DefaultTimeout = 10 }.ToString());
                    cn.Open();
                    using var tran = cn.BeginTransaction();
                    foreach (string sql in lenh)
                    {
                        using var cmd = cn.CreateCommand();
                        cmd.Transaction = tran;
                        cmd.CommandText = sql;
                        cmd.ExecuteNonQuery();
                    }
                    sauKhiTao?.Invoke(cn, tran);
                    tran.Commit();
                    _daDamBao.Add(khoa);
                }
                catch (Exception ex) { Debug.WriteLine($"[Module_CheDoDoiTuong.DamBaoBang {c}/{loai}] " + ex.Message); }
            }
        }
        /// <summary>Quên trạng thái "đã bảo đảm" (gọi khi khôi phục/chuyển giao ghi đè tệp CSDL).</summary>
        public static void XoaBoDemDamBao()
        {
            lock (_daDamBao) _daDamBao.Clear();
            Module_LichXet.XoaBoDem();
            Module_BangKhenThuong.XoaBoDem();
        }
    }
}
