using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
namespace PhanMemThiDua2026
{
    internal class Module_SaoLuuKhoiPhucKhenThuong
    {
        private readonly string _csdl4Path = Module_DanduongGPS.DuongDanCSDL4;
        private const string SHEET_THONGTIN = "DuLieuSaoLuu_KhenThuong";
        // Tên sheet theo chế độ. CBCS giữ đúng tên cũ (đọc được tệp sao lưu cũ). Tân binh/Học viên dùng tên ngắn
        // (Excel giới hạn 31 ký tự) => tệp sao lưu của chế độ này không khôi phục nhầm vào chế độ khác (thiếu sheet).
        private static string SHEET_CBCS => Module_CheDoDoiTuong.Hien switch
        {
            CheDoDoiTuong.TanBinh => "TanBinh_DuocKhenThuong",
            CheDoDoiTuong.HocVien => "HocVien_DuocKhenThuong",
            _ => "ThongKeCBCS_DuocKhenThuong"
        };
        private static string SHEET_GIAYKHEN => Module_CheDoDoiTuong.Hien switch
        {
            CheDoDoiTuong.TanBinh => "TanBinh_GiayKhen",
            CheDoDoiTuong.HocVien => "HocVien_GiayKhen",
            _ => "ThongKe_GiayKhen"
        };
        private static string SHEET_TAPTHE => Module_CheDoDoiTuong.Hien switch
        {
            CheDoDoiTuong.TanBinh => "TanBinh_KhenThuongTapThe",
            CheDoDoiTuong.HocVien => "HocVien_KhenThuongTapThe",
            _ => "ThongKe_KhenThuongTapThe"
        };
        private const int MAX_KY_TU_O_EXCEL = 32767;   // giới hạn cứng của 1 ô Excel: dài hơn sẽ bị cắt => mất dữ liệu
        /// <summary>Toàn bộ nội dung 1 tệp sao lưu (bản mã giữ NGUYÊN như trong CSDL, không giải mã/mã hóa lại).</summary>
        internal sealed class DuLieuSaoLuu
        {
            public List<CBCSDuocKhenThuongDTO> CBCS = new();
            public List<GiayKhenDTO> GiayKhen = new();
            public List<KhenThuongTapTheDTO> TapThe = new();
            public string ThoiGianTao = "Không xác định", NguoiTao = "Không xác định", MayTinh = "Không xác định", UserMay = "Không xác định";
        }
        private string LayGiaTriThongTin(IXLWorksheet ws, string tenTruong)
        {
            foreach (var cell in ws.Column(1).CellsUsed())
            {
                string noiDung = cell.GetString();
                if (noiDung.StartsWith(tenTruong, StringComparison.OrdinalIgnoreCase))
                {
                    int viTri = noiDung.IndexOf(':');
                    if (viTri >= 0 && viTri < noiDung.Length - 1)
                        return noiDung[(viTri + 1)..].Trim();
                }
            }
            return "Không xác định";
        }
        public void saoLuuToanBoDuLieuKhenThuong_toolStripMenuItem_Click(object? sender, EventArgs e)
        {
            using SaveFileDialog sfd = new()
            {
                Filter = "Tệp dữ liệu Sao lưu (*.mdf)|*.mdf|Tất cả tệp (*.*)|*.*",
                Title = "Sao lưu dữ liệu khen thưởng",
                FileName = "SaoLuu_KhenThuong_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".mdf"
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;
            try
            {
                string tenMayTinh = string.IsNullOrWhiteSpace(Environment.MachineName) ? "Không xác định" : Environment.MachineName;
                string userMayTinh = string.IsNullOrWhiteSpace(Environment.UserName) ? "Không xác định" : Environment.UserName;
                DateTime thoiGianTao = DateTime.Now;
                var (soCbcs, soGk, soTt) = GhiTepSaoLuu(sfd.FileName);
                MessageBox.Show(
                    "Sao lưu dữ liệu khen thưởng thành công!\n\n" +
                    $"- {Module_CheDoDoiTuong.TenDoiTuongHienTai}: {soCbcs:N0}\n- Giấy khen cá nhân: {soGk:N0}\n- Khen thưởng tập thể: {soTt:N0}\n\n" +
                    "Tệp đã được đọc lại và đối chiếu từng ô với CSDL: khớp hoàn toàn.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Module_NhatKy.GhiNhatKy(
                    taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                    hanhDong: "Sao lưu dữ liệu khen thưởng thành công ra tệp .mdf!",
                    ghiChu:
                        $"Thời gian: {thoiGianTao:dd-MM-yyyy HH:mm:ss} - " +
                        $"Máy tính: {tenMayTinh} - " +
                        $"User máy tính: {userMayTinh} - " +
                        $"{Module_CheDoDoiTuong.TenDoiTuongHienTai}: {soCbcs}, giấy khen: {soGk}, tập thể: {soTt} - " +
                        $"Đường dẫn: {sfd.FileName}");
                Module_XuatNhapDuLieuThiDua.MoVaChonTepTrongExplorer(sfd.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi sao lưu dữ liệu: " + ex.Message, Module_HeThong.TieuDe_Loi, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// Xuất 3 bảng khen thưởng ra tệp sao lưu, rồi ĐỌC LẠI tệp và so từng ô với dữ liệu vừa xuất.
        /// Lệch bất kỳ ô nào => xóa tệp hỏng và báo lỗi (không để người dùng tin vào 1 bản sao lưu sai).
        /// </summary>
        internal (int SoCBCS, int SoGiayKhen, int SoTapThe) GhiTepSaoLuu(string duongDan)
        {
            var goc = new DuLieuSaoLuu { CBCS = LayDanhSachCBCS(), GiayKhen = LayDanhSachGiayKhen(), TapThe = LayDanhSachTapThe() };
            // Ô quá dài sẽ bị Excel cắt => chặn trước, không tạo bản sao lưu thiếu dữ liệu
            foreach (var (bang, chuoi) in CacChuoi(goc))
                if (chuoi != null && chuoi.Length > MAX_KY_TU_O_EXCEL)
                    throw new InvalidDataException($"Bảng {bang} có ô dài {chuoi.Length:N0} ký tự, vượt giới hạn {MAX_KY_TU_O_EXCEL:N0} ký tự/ô của tệp sao lưu.");
            string tenMayTinh = string.IsNullOrWhiteSpace(Environment.MachineName) ? "Không xác định" : Environment.MachineName;
            string userMayTinh = string.IsNullOrWhiteSpace(Environment.UserName) ? "Không xác định" : Environment.UserName;
            using (var wb = new XLWorkbook())
            {
                // 1. Sheet thông tin hiển thị (Excel bắt buộc phải có ít nhất 1 sheet hiển thị)
                var wsInfo = wb.Worksheets.Add(SHEET_THONGTIN);
                wsInfo.Cell(1, 1).Value = "Tệp dữ liệu sao lưu khen thưởng";
                wsInfo.Cell(2, 1).Value = $"Thời gian tạo: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                wsInfo.Cell(3, 1).Value = "Ghi chú: Dữ liệu hệ thống đã được lưu trữ an toàn trong tệp này.";
                wsInfo.Cell(4, 1).Value = "Ghi chú: Đồng chí không nên tác động vào tệp gốc - Lỗi dữ liệu.";
                wsInfo.Cell(5, 1).Value = "Tên người dùng: " + Module_TaiKhoan.TenTaiKhoan_RAM;
                wsInfo.Cell(6, 1).Value = "Tên máy tính: " + tenMayTinh;
                wsInfo.Cell(7, 1).Value = "User máy tính: " + userMayTinh;
                wsInfo.Cell(8, 1).Value = $"Số lượng: {Module_CheDoDoiTuong.TenDoiTuongHienTai} {goc.CBCS.Count} - giấy khen {goc.GiayKhen.Count} - tập thể {goc.TapThe.Count}";
                wsInfo.Columns().AdjustToContents();
                // 2. 3 sheet dữ liệu nghiệp vụ (ẩn). Không AdjustToContents: sheet ẩn, đo từng ô bằng GDI chỉ tốn thời gian
                var ws1 = wb.Worksheets.Add(SHEET_CBCS);
                ws1.Cell(1, 1).InsertTable(goc.CBCS);
                var ws2 = wb.Worksheets.Add(SHEET_GIAYKHEN);
                ws2.Cell(1, 1).InsertTable(goc.GiayKhen);
                var ws3 = wb.Worksheets.Add(SHEET_TAPTHE);
                ws3.Cell(1, 1).InsertTable(goc.TapThe);
                ws1.Hide();
                ws2.Hide();
                ws3.Hide();
                Module_BanQuyen.DongDauExcel(wb);
                // Lưu an toàn qua tệp tạm (tự dùng stream cho đuôi tệp riêng): lỗi giữa chừng không làm hỏng bản sao lưu cũ
                Module_ExcelAnToan.LuuAnToan(wb, duongDan);
            }
            // 3. Đọc lại và đối chiếu
            string? loi;
            try { loi = SoSanh(goc, DocTepSaoLuu(duongDan)); }
            catch (Exception ex) { loi = "không đọc lại được tệp: " + ex.Message; }
            if (loi != null)
            {
                try { File.Delete(duongDan); } catch { }
                throw new InvalidDataException("Tệp sao lưu vừa tạo không khớp với CSDL (" + loi + "). Tệp đã bị xóa, vui lòng sao lưu lại.");
            }
            return (goc.CBCS.Count, goc.GiayKhen.Count, goc.TapThe.Count);
        }
        private static IEnumerable<(string Bang, string? Chuoi)> CacChuoi(DuLieuSaoLuu d)
        {
            foreach (var x in d.CBCS)
                foreach (var s in new[] { x.HoVaTen, x.SoHieu, x.DonVi, x.TinhTrang, x.SoLuong_Khen, x.GhiChu_Khen }) yield return (SHEET_CBCS, s);
            foreach (var x in d.GiayKhen)
                foreach (var s in new[] { x.HoVaTen, x.SoHieu, x.DonVi, x.TinhTrang, x.HinhThuc_Khen, x.QuyetDinh_Khen, x.NgayCapQD_Khen, x.DonVi_Khen, x.VeViec_Khen, x.TienThuong, x.NgayCapPhat, x.CanBoCapPhat, x.GhiChu_Khen })
                    yield return (SHEET_GIAYKHEN, s);
            foreach (var x in d.TapThe)
                foreach (var s in new[] { x.TenTapThe, x.HinhThuc_KhenThuong, x.DonVi_CapKhenThuong, x.SoQuyetDinh, x.NgayQuyetDinh, x.NguoiKy, x.NoiDung_KhenThuong, x.TienThuong, x.NgayCapPhat, x.CanBoCapPhat, x.NguoiDaiDienNhan, x.GhiChu })
                    yield return (SHEET_TAPTHE, s);
        }
        /// <summary>So 2 bộ dữ liệu từng dòng, từng ô (NULL và chuỗi rỗng coi như nhau). null = khớp, ngược lại = mô tả chỗ lệch đầu tiên.</summary>
        internal static string? SoSanh(DuLieuSaoLuu a, DuLieuSaoLuu b)
        {
            static string N(string? s) => s ?? "";
            string? So<T>(string bang, List<T> x, List<T> y, Func<T, long> id, Func<T, string?[]> o)
            {
                if (x.Count != y.Count) return $"{bang}: {x.Count} dòng ≠ {y.Count} dòng";
                for (int i = 0; i < x.Count; i++)
                {
                    if (id(x[i]) != id(y[i])) return $"{bang} dòng {i + 2}: ID {id(x[i])} ≠ {id(y[i])}";
                    var ox = o(x[i]); var oy = o(y[i]);
                    for (int k = 0; k < ox.Length; k++)
                        if (!string.Equals(N(ox[k]), N(oy[k]), StringComparison.Ordinal)) return $"{bang} dòng {i + 2} cột {k + 2}";
                }
                return null;
            }
            return So(SHEET_CBCS, a.CBCS, b.CBCS, x => x.ID, x => new[] { x.HoVaTen, x.SoHieu, x.DonVi, x.TinhTrang, x.SoLuong_Khen, x.GhiChu_Khen })
                ?? So(SHEET_GIAYKHEN, a.GiayKhen, b.GiayKhen, x => x.ID, x => new[] { x.HoVaTen, x.SoHieu, x.DonVi, x.TinhTrang, x.HinhThuc_Khen, x.QuyetDinh_Khen, x.NgayCapQD_Khen, x.DonVi_Khen, x.VeViec_Khen, x.TienThuong, x.NgayCapPhat, x.CanBoCapPhat, x.GhiChu_Khen })
                ?? So(SHEET_TAPTHE, a.TapThe, b.TapThe, x => x.ID, x => new[] { x.STT.ToString(System.Globalization.CultureInfo.InvariantCulture), x.TenTapThe, x.HinhThuc_KhenThuong, x.DonVi_CapKhenThuong, x.SoQuyetDinh, x.NgayQuyetDinh, x.NguoiKy, x.NoiDung_KhenThuong, x.TienThuong, x.NgayCapPhat, x.CanBoCapPhat, x.NguoiDaiDienNhan, x.GhiChu });
        }
        /// <summary>Đọc tệp sao lưu (kiểm cấu trúc: đủ 3 sheet, đủ cột theo TÊN tiêu đề - không phụ thuộc thứ tự cột).</summary>
        internal DuLieuSaoLuu DocTepSaoLuu(string duongDan)
        {
            using var stream = File.OpenRead(duongDan);
            using var wb = new XLWorkbook(stream);
            string[] sheetBatBuoc = { SHEET_CBCS, SHEET_GIAYKHEN, SHEET_TAPTHE };
            var thieu = sheetBatBuoc.Where(s => !wb.Worksheets.Contains(s)).ToList();
            if (thieu.Count > 0)
                throw new InvalidDataException("Tệp dữ liệu không đúng cấu trúc! Thiếu sheet: " + string.Join(", ", thieu));
            var d = new DuLieuSaoLuu();
            if (wb.Worksheets.Contains(SHEET_THONGTIN))
            {
                var wsInfo = wb.Worksheet(SHEET_THONGTIN);
                // Tệp sao lưu ghi nhãn "Thời gian tạo:" (bản cũ tìm "Thời gian xuất bản:" nên luôn ra "Không xác định")
                d.ThoiGianTao = LayGiaTriThongTin(wsInfo, "Thời gian tạo:");
                if (d.ThoiGianTao == "Không xác định") d.ThoiGianTao = LayGiaTriThongTin(wsInfo, "Thời gian xuất bản:");
                d.NguoiTao = LayGiaTriThongTin(wsInfo, "Tên người dùng:");
                d.MayTinh = LayGiaTriThongTin(wsInfo, "Tên máy tính:");
                d.UserMay = LayGiaTriThongTin(wsInfo, "User máy tính:");
            }
            d.CBCS = DocSheetCBCS(wb.Worksheet(SHEET_CBCS));
            d.GiayKhen = DocSheetGiayKhen(wb.Worksheet(SHEET_GIAYKHEN));
            d.TapThe = DocSheetTapThe(wb.Worksheet(SHEET_TAPTHE));
            return d;
        }
        /// <summary>Số dòng có Số hiệu là bản mã nhưng KHÔNG giải mã được bằng khóa máy này (tệp từ máy khác khóa / tệp hỏng).</summary>
        internal static int DemSoHieuKhongGiaiMaDuoc(DuLieuSaoLuu d)
        {
            static bool Loi(string? s) => !string.IsNullOrWhiteSpace(s) && Module_BaoMatAES.LaChuoiMa(s.Trim())
                                           && string.IsNullOrEmpty(Module_BaoMatAES.GiaiMa(s.Trim()));
            return d.CBCS.Count(x => Loi(x.SoHieu)) + d.GiayKhen.Count(x => Loi(x.SoHieu));
        }
        public void khoiPhucDuLieuKhenThuong_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using OpenFileDialog ofd = new()
            {
                Filter = "Tệp dữ liệu Sao lưu (*.mdf)|*.mdf|Tất cả tệp (*.*)|*.*",
                Title = "Chọn file sao lưu khen thưởng (.mdf) để khôi phục dữ liệu"
            };
            if (ofd.ShowDialog() != DialogResult.OK)
                return;
            try
            {
                DuLieuSaoLuu d;
                try { d = DocTepSaoLuu(ofd.FileName); }
                catch (InvalidDataException ex)
                {
                    MessageBox.Show(ex.Message, "Lỗi cấu trúc", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // Tệp mã hóa bằng khóa khác: nạp vào sẽ ra dữ liệu không đọc được => dừng hẳn, không ghi gì
                int loiGiaiMa = DemSoHieuKhongGiaiMaDuoc(d);
                if (loiGiaiMa > 0)
                {
                    MessageBox.Show(
                        $"Có {loiGiaiMa:N0} dòng trong tệp sao lưu KHÔNG giải mã được bằng khóa bảo mật của máy này.\n\n" +
                        "Tệp có thể được tạo từ phần mềm dùng khóa khác hoặc đã bị hỏng. Đã hủy khôi phục, CSDL không bị thay đổi.",
                        "Không thể khôi phục", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                string thongBao =
                    "THÔNG TIN TỆP SAO LƯU\n\n" +
                    $"Thời gian tạo: {d.ThoiGianTao}\n" +
                    $"Tên người dùng: {d.NguoiTao}\n" +
                    $"Tên máy tính: {d.MayTinh}\n" +
                    $"User máy tính: {d.UserMay}\n" +
                    $"Nội dung: {Module_CheDoDoiTuong.TenDoiTuongHienTai} {d.CBCS.Count:N0} - giấy khen {d.GiayKhen.Count:N0} - tập thể {d.TapThe.Count:N0}\n\n" +
                    "PHƯƠNG THỨC KHÔI PHỤC\n\n" +
                    "[Yes]  XÓA SẠCH dữ liệu cũ và nạp mới.\n" +
                    "[No]   GIỮ dữ liệu cũ, chỉ thêm giấy khen còn thiếu (đối chiếu theo Số hiệu;\n" +
                    "         giấy khen đã có thì không thêm trùng).\n" +
                    "[Cancel] Hủy thao tác.\n\n" +
                    "Bạn có muốn tiếp tục khôi phục dữ liệu?";
                DialogResult confirm = MessageBox.Show(thongBao, "Khôi phục dữ liệu khen thưởng",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (confirm == DialogResult.Cancel)
                    return;
                bool isClearData = confirm == DialogResult.Yes;
                var kq = LuuVaoDatabase(d.CBCS, d.GiayKhen, d.TapThe, isClearData);
                MessageBox.Show(
                    $"Khôi phục dữ liệu khen thưởng thành công!\n\n" +
                    $"- Giấy khen cá nhân: thêm {kq.GkThem:N0}, bổ sung thông tin {kq.GkBoSung:N0}, đã có sẵn (bỏ qua) {kq.GkBoQua:N0}.\n" +
                    $"- {Module_CheDoDoiTuong.TenDoiTuongHienTai}: thêm {kq.CbcsThem:N0}, đã có sẵn (bỏ qua) {kq.CbcsBoQua:N0}.\n" +
                    $"- Khen thưởng tập thể: thêm {kq.TtThem:N0}, đã có sẵn (bỏ qua) {kq.TtBoQua:N0}.\n\n" +
                    "Tình trạng công tác được đối chiếu lại theo danh sách gốc (Form6).\n\n" +
                    $"Tệp được tạo từ máy tính: {d.MayTinh}\n" +
                    $"User máy tính: {d.UserMay}",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Module_NhatKy.GhiNhatKy(
                    taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                    hanhDong: "Khôi phục dữ liệu khen thưởng từ tệp .mdf thành công!",
                    ghiChu:
                        $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss} - " +
                        $"Chế độ: {(isClearData ? "Xóa sạch + nạp mới" : "Giữ cũ + thêm thiếu")} - " +
                        $"Giấy khen thêm {kq.GkThem}, bổ sung {kq.GkBoSung}, bỏ qua {kq.GkBoQua} - " +
                        $"Nguồn máy tính: {d.MayTinh} - " +
                        $"Nguồn User: {d.UserMay} - " +
                        $"Tệp: {ofd.FileName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Lỗi khi khôi phục dữ liệu (CSDL đã được hoàn tác, không bị thay đổi): " + ex.Message,
                    Module_HeThong.TieuDe_Loi,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        /// <summary>Kết quả ghi CSDL khi khôi phục (để báo lại cho người dùng).</summary>
        internal sealed class KetQuaKhoiPhuc
        {
            public int CbcsThem, CbcsBoQua, GkThem, GkBoSung, GkBoQua, TtThem, TtBoQua;
        }
        // Chuẩn hóa CHỈ để so trùng nội dung: bỏ khoảng trắng thừa, không phân biệt hoa/thường
        private static string ChuanSoSanh(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            return string.Join(" ", s.Normalize(System.Text.NormalizationForm.FormC)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
        }
        private static string Ro(string? s) => Module_DongBoKhenThuong.GiaiMa(s);
        private static string Doc(SqliteDataReader rd, int i) => rd.IsDBNull(i) ? "" : (Convert.ToString(rd.GetValue(i)) ?? "");
        /// <summary>Chữ ký nội dung 1 giấy khen: cùng người (Số hiệu) + cùng hình thức, số QĐ, ngày QĐ, đơn vị khen, nội dung = cùng 1 giấy khen.</summary>
        private static string KhoaGiayKhen(string? sh, string? hinhThuc, string? soQd, string? ngayQd, string? dvKhen, string? veViec) =>
            string.Join("|", ChuanSoSanh(Ro(sh)), ChuanSoSanh(Ro(hinhThuc)), ChuanSoSanh(Ro(soQd)), ChuanSoSanh(Ro(ngayQd)),
                ChuanSoSanh(Ro(dvKhen)), ChuanSoSanh(Ro(veViec)));
        private static string KhoaTapThe(string? ten, string? hinhThuc, string? dvCap, string? soQd, string? ngayQd, string? noiDung) =>
            string.Join("|", ChuanSoSanh(Ro(ten)), ChuanSoSanh(Ro(hinhThuc)), ChuanSoSanh(Ro(dvCap)),
                ChuanSoSanh(Ro(soQd)), ChuanSoSanh(Ro(ngayQd)), ChuanSoSanh(Ro(noiDung)));
        private static object GiaTri(string? s) => string.IsNullOrEmpty(s) ? DBNull.Value : s;
        private static long IdVuaThem(SqliteConnection conn, SqliteTransaction trans)
        {
            using var cmd = new SqliteCommand("SELECT last_insert_rowid();", conn, trans);
            return Convert.ToInt64(cmd.ExecuteScalar());
        }
        /// <summary>
        /// [Yes] Xóa sạch rồi nạp lại nguyên trạng (giữ ID như tệp sao lưu).
        /// [No]  KHÔNG ghép theo ID nữa (ID ở 2 máy khác nhau => ghi đè nhầm giấy khen của người khác). Thay vào đó:
        ///       - CBCS: theo Số hiệu; chưa có thì thêm, có rồi thì giữ nguyên.
        ///       - Giấy khen: 1 người có thể có nhiều giấy khen; giấy khen trùng nội dung với giấy khen đã có thì KHÔNG thêm
        ///         (chỉ bổ sung các ô đang trống: tiền thưởng, ngày cấp phát, cán bộ cấp phát, ghi chú); còn lại thêm mới với ID mới.
        ///       - Tập thể: trùng nội dung thì bỏ qua, còn lại thêm mới (STT nối tiếp).
        /// Tình trạng công tác / số lượng khen KHÔNG tin theo tệp: Form34 đối chiếu lại với Form6 ngay sau khi khôi phục.
        /// </summary>
        internal KetQuaKhoiPhuc LuuVaoDatabase(List<CBCSDuocKhenThuongDTO> listCBCS, List<GiayKhenDTO> listGiayKhen, List<KhenThuongTapTheDTO> listTapThe, bool isClearData)
        {
            var kq = new KetQuaKhoiPhuc();
            using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path));
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                // 0. CSDL4 chưa có bảng khen thưởng cá nhân (hoặc bảng cũ thiếu cột) thì tạo/bổ sung trước khi xóa + chèn
                Module_BangKhenThuong.DamBao(conn, trans);
                Module_BangKhenThuong.DamBaoTapThe(conn, trans);
                if (isClearData)
                {
                    using var cmdClear = new SqliteCommand($@"
                        DELETE FROM {Module_CheDoDoiTuong.BangKhenThuongNguoi};
                        DELETE FROM {Module_CheDoDoiTuong.BangGiayKhen};
                        DELETE FROM {Module_CheDoDoiTuong.BangKhenThuongTapThe};
                        DELETE FROM sqlite_sequence WHERE name IN ('{Module_CheDoDoiTuong.BangKhenThuongNguoi}', '{Module_CheDoDoiTuong.BangGiayKhen}', '{Module_CheDoDoiTuong.BangKhenThuongTapThe}');", conn, trans);
                    cmdClear.ExecuteNonQuery();
                }
                LuuCBCS(conn, trans, listCBCS, isClearData, kq);
                LuuGiayKhen(conn, trans, listGiayKhen, isClearData, kq);
                LuuTapThe(conn, trans, listTapThe, isClearData, kq);
                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
            Module_ChuKyCSDL.DanhDauThayDoi();
            return kq;
        }
        private static void LuuCBCS(SqliteConnection conn, SqliteTransaction trans, List<CBCSDuocKhenThuongDTO> ds, bool giuId, KetQuaKhoiPhuc kq)
        {
            var daCo = new HashSet<string>(Module_DongBoKhenThuong.SoSanhSoHieu);
            if (!giuId)
            {
                using var cmdDoc = new SqliteCommand($"SELECT SoHieu FROM {Module_CheDoDoiTuong.BangKhenThuongNguoi}", conn, trans);
                using var rd = cmdDoc.ExecuteReader();
                while (rd.Read()) { string sh = Ro(Doc(rd, 0)); if (sh.Length > 0) daCo.Add(sh); }
            }
            using var cmd = new SqliteCommand(giuId
                ? $"INSERT OR REPLACE INTO {Module_CheDoDoiTuong.BangKhenThuongNguoi} (ID, HoVaTen, SoHieu, DonVi, TinhTrang, SoLuong_Khen, GhiChu_Khen) VALUES (@ID, @HoVaTen, @SoHieu, @DonVi, @TinhTrang, @SoLuong_Khen, @GhiChu_Khen);"
                : $"INSERT INTO {Module_CheDoDoiTuong.BangKhenThuongNguoi} (HoVaTen, SoHieu, DonVi, TinhTrang, SoLuong_Khen, GhiChu_Khen) VALUES (@HoVaTen, @SoHieu, @DonVi, @TinhTrang, @SoLuong_Khen, @GhiChu_Khen);", conn, trans);
            if (giuId) cmd.Parameters.Add("@ID", SqliteType.Integer);
            foreach (var p in new[] { "@HoVaTen", "@SoHieu", "@DonVi", "@TinhTrang", "@SoLuong_Khen", "@GhiChu_Khen" }) cmd.Parameters.Add(p, SqliteType.Text);
            foreach (var item in ds)
            {
                string sh = Ro(item.SoHieu);
                if (!giuId && (sh.Length == 0 || !daCo.Add(sh))) { kq.CbcsBoQua++; continue; }   // [No]: trùng Số hiệu (đã có trong máy hoặc lặp trong tệp). [Yes]: chép nguyên trạng
                if (giuId) cmd.Parameters["@ID"].Value = item.ID == 0 ? DBNull.Value : item.ID;
                cmd.Parameters["@HoVaTen"].Value = GiaTri(item.HoVaTen);
                cmd.Parameters["@SoHieu"].Value = GiaTri(item.SoHieu);
                cmd.Parameters["@DonVi"].Value = GiaTri(item.DonVi);
                cmd.Parameters["@TinhTrang"].Value = GiaTri(item.TinhTrang);
                cmd.Parameters["@SoLuong_Khen"].Value = GiaTri(item.SoLuong_Khen);
                cmd.Parameters["@GhiChu_Khen"].Value = GiaTri(item.GhiChu_Khen);
                cmd.ExecuteNonQuery();
                kq.CbcsThem++;
            }
        }
        private sealed class GiayKhenCo
        {
            public long Id;
            public bool TrongTien, TrongNgayCap, TrongCanBo, TrongGhiChu;
        }
        private static void LuuGiayKhen(SqliteConnection conn, SqliteTransaction trans, List<GiayKhenDTO> ds, bool giuId, KetQuaKhoiPhuc kq)
        {
            var daCo = new Dictionary<string, GiayKhenCo>(StringComparer.Ordinal);
            if (!giuId)
            {
                using var cmdDoc = new SqliteCommand($"SELECT ID, SoHieu, HinhThuc_Khen, QuyetDinh_Khen, NgayCapQD_Khen, DonVi_Khen, VeViec_Khen, TienThuong, NgayCapPhat, CanBoCapPhat, GhiChu_Khen FROM {Module_CheDoDoiTuong.BangGiayKhen}", conn, trans);
                using var rd = cmdDoc.ExecuteReader();
                while (rd.Read())
                {
                    string khoa = KhoaGiayKhen(Doc(rd, 1), Doc(rd, 2), Doc(rd, 3), Doc(rd, 4), Doc(rd, 5), Doc(rd, 6));
                    daCo.TryAdd(khoa, new GiayKhenCo
                    {
                        Id = rd.GetInt64(0),
                        TrongTien = Ro(Doc(rd, 7)).Length == 0, TrongNgayCap = Ro(Doc(rd, 8)).Length == 0,
                        TrongCanBo = Ro(Doc(rd, 9)).Length == 0, TrongGhiChu = Ro(Doc(rd, 10)).Length == 0
                    });
                }
            }
            using var cmd = new SqliteCommand(giuId
                ? $@"INSERT OR REPLACE INTO {Module_CheDoDoiTuong.BangGiayKhen} (ID, HoVaTen, SoHieu, DonVi, TinhTrang, HinhThuc_Khen, QuyetDinh_Khen, NgayCapQD_Khen, DonVi_Khen, VeViec_Khen, TienThuong, NgayCapPhat, CanBoCapPhat, GhiChu_Khen)
                    VALUES (@ID, @HoVaTen, @SoHieu, @DonVi, @TinhTrang, @HinhThuc_Khen, @QuyetDinh_Khen, @NgayCapQD_Khen, @DonVi_Khen, @VeViec_Khen, @TienThuong, @NgayCapPhat, @CanBoCapPhat, @GhiChu_Khen);"
                : $@"INSERT INTO {Module_CheDoDoiTuong.BangGiayKhen} (HoVaTen, SoHieu, DonVi, TinhTrang, HinhThuc_Khen, QuyetDinh_Khen, NgayCapQD_Khen, DonVi_Khen, VeViec_Khen, TienThuong, NgayCapPhat, CanBoCapPhat, GhiChu_Khen)
                    VALUES (@HoVaTen, @SoHieu, @DonVi, @TinhTrang, @HinhThuc_Khen, @QuyetDinh_Khen, @NgayCapQD_Khen, @DonVi_Khen, @VeViec_Khen, @TienThuong, @NgayCapPhat, @CanBoCapPhat, @GhiChu_Khen);", conn, trans);
            if (giuId) cmd.Parameters.Add("@ID", SqliteType.Integer);
            foreach (var p in new[] { "@HoVaTen", "@SoHieu", "@DonVi", "@TinhTrang", "@HinhThuc_Khen", "@QuyetDinh_Khen", "@NgayCapQD_Khen", "@DonVi_Khen", "@VeViec_Khen", "@TienThuong", "@NgayCapPhat", "@CanBoCapPhat", "@GhiChu_Khen" })
                cmd.Parameters.Add(p, SqliteType.Text);
            // Bổ sung thông tin cho giấy khen ĐÃ CÓ: chỉ ghi vào ô đang trống, không đè thông tin người dùng đã nhập
            using var cmdBoSung = new SqliteCommand($@"UPDATE {Module_CheDoDoiTuong.BangGiayKhen} SET
                    TienThuong   = CASE WHEN @boTien = 1 THEN @TienThuong ELSE TienThuong END,
                    NgayCapPhat  = CASE WHEN @boNgay = 1 THEN @NgayCapPhat ELSE NgayCapPhat END,
                    CanBoCapPhat = CASE WHEN @boCanBo = 1 THEN @CanBoCapPhat ELSE CanBoCapPhat END,
                    GhiChu_Khen  = CASE WHEN @boGhiChu = 1 THEN @GhiChu_Khen ELSE GhiChu_Khen END
                WHERE ID = @ID", conn, trans);
            foreach (var p in new[] { "@TienThuong", "@NgayCapPhat", "@CanBoCapPhat", "@GhiChu_Khen" }) cmdBoSung.Parameters.Add(p, SqliteType.Text);
            foreach (var p in new[] { "@boTien", "@boNgay", "@boCanBo", "@boGhiChu", "@ID" }) cmdBoSung.Parameters.Add(p, SqliteType.Integer);
            foreach (var item in ds)
            {
                if (!giuId && Ro(item.SoHieu).Length == 0) { kq.GkBoQua++; continue; }   // [Yes]: chép nguyên trạng mọi dòng
                string khoa = KhoaGiayKhen(item.SoHieu, item.HinhThuc_Khen, item.QuyetDinh_Khen, item.NgayCapQD_Khen, item.DonVi_Khen, item.VeViec_Khen);
                if (!giuId && daCo.TryGetValue(khoa, out var co))
                {
                    bool boTien = co.TrongTien && Ro(item.TienThuong).Length > 0, boNgay = co.TrongNgayCap && Ro(item.NgayCapPhat).Length > 0;
                    bool boCanBo = co.TrongCanBo && Ro(item.CanBoCapPhat).Length > 0, boGhiChu = co.TrongGhiChu && Ro(item.GhiChu_Khen).Length > 0;
                    if (boTien || boNgay || boCanBo || boGhiChu)
                    {
                        cmdBoSung.Parameters["@TienThuong"].Value = GiaTri(item.TienThuong);
                        cmdBoSung.Parameters["@NgayCapPhat"].Value = GiaTri(item.NgayCapPhat);
                        cmdBoSung.Parameters["@CanBoCapPhat"].Value = GiaTri(item.CanBoCapPhat);
                        cmdBoSung.Parameters["@GhiChu_Khen"].Value = GiaTri(item.GhiChu_Khen);
                        cmdBoSung.Parameters["@boTien"].Value = boTien ? 1 : 0;
                        cmdBoSung.Parameters["@boNgay"].Value = boNgay ? 1 : 0;
                        cmdBoSung.Parameters["@boCanBo"].Value = boCanBo ? 1 : 0;
                        cmdBoSung.Parameters["@boGhiChu"].Value = boGhiChu ? 1 : 0;
                        cmdBoSung.Parameters["@ID"].Value = co.Id;
                        cmdBoSung.ExecuteNonQuery();
                        co.TrongTien &= !boTien; co.TrongNgayCap &= !boNgay; co.TrongCanBo &= !boCanBo; co.TrongGhiChu &= !boGhiChu;
                        kq.GkBoSung++;
                    }
                    else kq.GkBoQua++;
                    continue;
                }
                if (giuId) cmd.Parameters["@ID"].Value = item.ID == 0 ? DBNull.Value : item.ID;
                cmd.Parameters["@HoVaTen"].Value = GiaTri(item.HoVaTen);
                cmd.Parameters["@SoHieu"].Value = GiaTri(item.SoHieu);
                cmd.Parameters["@DonVi"].Value = GiaTri(item.DonVi);
                cmd.Parameters["@TinhTrang"].Value = GiaTri(item.TinhTrang);
                cmd.Parameters["@HinhThuc_Khen"].Value = GiaTri(item.HinhThuc_Khen);
                cmd.Parameters["@QuyetDinh_Khen"].Value = GiaTri(item.QuyetDinh_Khen);
                cmd.Parameters["@NgayCapQD_Khen"].Value = GiaTri(item.NgayCapQD_Khen);
                cmd.Parameters["@DonVi_Khen"].Value = GiaTri(item.DonVi_Khen);
                cmd.Parameters["@VeViec_Khen"].Value = GiaTri(item.VeViec_Khen);
                cmd.Parameters["@TienThuong"].Value = GiaTri(item.TienThuong);
                cmd.Parameters["@NgayCapPhat"].Value = GiaTri(item.NgayCapPhat);
                cmd.Parameters["@CanBoCapPhat"].Value = GiaTri(item.CanBoCapPhat);
                cmd.Parameters["@GhiChu_Khen"].Value = GiaTri(item.GhiChu_Khen);
                cmd.ExecuteNonQuery();
                kq.GkThem++;
                // Giấy khen lặp ngay trong chính tệp sao lưu cũng chỉ thêm 1 lần
                if (!giuId) daCo.TryAdd(khoa, new GiayKhenCo
                {
                    Id = IdVuaThem(conn, trans),
                    TrongTien = Ro(item.TienThuong).Length == 0, TrongNgayCap = Ro(item.NgayCapPhat).Length == 0,
                    TrongCanBo = Ro(item.CanBoCapPhat).Length == 0, TrongGhiChu = Ro(item.GhiChu_Khen).Length == 0
                });
            }
        }
        private static void LuuTapThe(SqliteConnection conn, SqliteTransaction trans, List<KhenThuongTapTheDTO> ds, bool giuId, KetQuaKhoiPhuc kq)
        {
            var daCo = new HashSet<string>(StringComparer.Ordinal);
            long sttMax = 0;
            if (!giuId)
            {
                using var cmdDoc = new SqliteCommand($"SELECT STT, TenTapThe, HinhThuc_KhenThuong, DonVi_CapKhenThuong, SoQuyetDinh, NgayQuyetDinh, NoiDung_KhenThuong FROM {Module_CheDoDoiTuong.BangKhenThuongTapThe}", conn, trans);
                using var rd = cmdDoc.ExecuteReader();
                while (rd.Read())
                {
                    if (long.TryParse(Doc(rd, 0), out long stt) && stt > sttMax) sttMax = stt;
                    daCo.Add(KhoaTapThe(Doc(rd, 1), Doc(rd, 2), Doc(rd, 3), Doc(rd, 4), Doc(rd, 5), Doc(rd, 6)));
                }
            }
            using var cmd = new SqliteCommand(giuId
                ? $@"INSERT OR REPLACE INTO {Module_CheDoDoiTuong.BangKhenThuongTapThe} (ID, STT, TenTapThe, HinhThuc_KhenThuong, DonVi_CapKhenThuong, SoQuyetDinh, NgayQuyetDinh, NguoiKy, NoiDung_KhenThuong, TienThuong, NgayCapPhat, CanBoCapPhat, NguoiDaiDienNhan, GhiChu)
                    VALUES (@ID, @STT, @TenTapThe, @HinhThuc_KhenThuong, @DonVi_CapKhenThuong, @SoQuyetDinh, @NgayQuyetDinh, @NguoiKy, @NoiDung_KhenThuong, @TienThuong, @NgayCapPhat, @CanBoCapPhat, @NguoiDaiDienNhan, @GhiChu);"
                : $@"INSERT INTO {Module_CheDoDoiTuong.BangKhenThuongTapThe} (STT, TenTapThe, HinhThuc_KhenThuong, DonVi_CapKhenThuong, SoQuyetDinh, NgayQuyetDinh, NguoiKy, NoiDung_KhenThuong, TienThuong, NgayCapPhat, CanBoCapPhat, NguoiDaiDienNhan, GhiChu)
                    VALUES (@STT, @TenTapThe, @HinhThuc_KhenThuong, @DonVi_CapKhenThuong, @SoQuyetDinh, @NgayQuyetDinh, @NguoiKy, @NoiDung_KhenThuong, @TienThuong, @NgayCapPhat, @CanBoCapPhat, @NguoiDaiDienNhan, @GhiChu);", conn, trans);
            if (giuId) cmd.Parameters.Add("@ID", SqliteType.Integer);
            cmd.Parameters.Add("@STT", SqliteType.Integer);
            foreach (var p in new[] { "@TenTapThe", "@HinhThuc_KhenThuong", "@DonVi_CapKhenThuong", "@SoQuyetDinh", "@NgayQuyetDinh", "@NguoiKy", "@NoiDung_KhenThuong", "@TienThuong", "@NgayCapPhat", "@CanBoCapPhat", "@NguoiDaiDienNhan", "@GhiChu" })
                cmd.Parameters.Add(p, SqliteType.Text);
            foreach (var item in ds)
            {
                if (!giuId && !daCo.Add(KhoaTapThe(item.TenTapThe, item.HinhThuc_KhenThuong, item.DonVi_CapKhenThuong, item.SoQuyetDinh, item.NgayQuyetDinh, item.NoiDung_KhenThuong)))
                { kq.TtBoQua++; continue; }
                if (giuId) cmd.Parameters["@ID"].Value = item.ID == 0 ? DBNull.Value : item.ID;
                cmd.Parameters["@STT"].Value = giuId ? item.STT : ++sttMax;
                cmd.Parameters["@TenTapThe"].Value = GiaTri(item.TenTapThe);
                cmd.Parameters["@HinhThuc_KhenThuong"].Value = GiaTri(item.HinhThuc_KhenThuong);
                cmd.Parameters["@DonVi_CapKhenThuong"].Value = GiaTri(item.DonVi_CapKhenThuong);
                cmd.Parameters["@SoQuyetDinh"].Value = GiaTri(item.SoQuyetDinh);
                cmd.Parameters["@NgayQuyetDinh"].Value = GiaTri(item.NgayQuyetDinh);
                cmd.Parameters["@NguoiKy"].Value = GiaTri(item.NguoiKy);
                cmd.Parameters["@NoiDung_KhenThuong"].Value = GiaTri(item.NoiDung_KhenThuong);
                cmd.Parameters["@TienThuong"].Value = GiaTri(item.TienThuong);
                cmd.Parameters["@NgayCapPhat"].Value = GiaTri(item.NgayCapPhat);
                cmd.Parameters["@CanBoCapPhat"].Value = GiaTri(item.CanBoCapPhat);
                cmd.Parameters["@NguoiDaiDienNhan"].Value = GiaTri(item.NguoiDaiDienNhan);
                cmd.Parameters["@GhiChu"].Value = GiaTri(item.GhiChu);
                cmd.ExecuteNonQuery();
                kq.TtThem++;
            }
        }
        // ===================== ĐỌC CSDL (bản mã giữ nguyên) =====================
        private static bool CoBang(SqliteConnection conn, string tenBang)
        {
            using var cmd = new SqliteCommand("SELECT 1 FROM sqlite_master WHERE type='table' AND name=@t LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@t", tenBang);
            return cmd.ExecuteScalar() != null;
        }
        private static string? DocO(SqliteDataReader dr, int i) =>
            dr.IsDBNull(i) ? null : Convert.ToString(dr.GetValue(i), System.Globalization.CultureInfo.InvariantCulture);
        private static long DocSo(SqliteDataReader dr, int i)
        {
            if (dr.IsDBNull(i)) return 0;
            object v = dr.GetValue(i);
            if (v is long l) return l;
            return long.TryParse(Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture), System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out long kq) ? kq : 0;
        }
        private List<CBCSDuocKhenThuongDTO> LayDanhSachCBCS()
        {
            var list = new List<CBCSDuocKhenThuongDTO>();
            using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path));
            conn.Open();
            if (!CoBang(conn, Module_CheDoDoiTuong.BangKhenThuongNguoi)) return list;
            using var cmd = new SqliteCommand($"SELECT ID, HoVaTen, SoHieu, DonVi, TinhTrang, SoLuong_Khen, GhiChu_Khen FROM {Module_CheDoDoiTuong.BangKhenThuongNguoi} ORDER BY ID", conn);
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
                list.Add(new CBCSDuocKhenThuongDTO
                {
                    ID = DocSo(dr, 0), HoVaTen = DocO(dr, 1), SoHieu = DocO(dr, 2), DonVi = DocO(dr, 3),
                    TinhTrang = DocO(dr, 4), SoLuong_Khen = DocO(dr, 5), GhiChu_Khen = DocO(dr, 6)
                });
            return list;
        }
        private List<GiayKhenDTO> LayDanhSachGiayKhen()
        {
            var list = new List<GiayKhenDTO>();
            using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path));
            conn.Open();
            if (!CoBang(conn, Module_CheDoDoiTuong.BangGiayKhen)) return list;
            using var cmd = new SqliteCommand($"SELECT ID, HoVaTen, SoHieu, DonVi, TinhTrang, HinhThuc_Khen, QuyetDinh_Khen, NgayCapQD_Khen, DonVi_Khen, VeViec_Khen, TienThuong, NgayCapPhat, CanBoCapPhat, GhiChu_Khen FROM {Module_CheDoDoiTuong.BangGiayKhen} ORDER BY ID", conn);
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
                list.Add(new GiayKhenDTO
                {
                    ID = DocSo(dr, 0), HoVaTen = DocO(dr, 1), SoHieu = DocO(dr, 2), DonVi = DocO(dr, 3), TinhTrang = DocO(dr, 4),
                    HinhThuc_Khen = DocO(dr, 5), QuyetDinh_Khen = DocO(dr, 6), NgayCapQD_Khen = DocO(dr, 7), DonVi_Khen = DocO(dr, 8),
                    VeViec_Khen = DocO(dr, 9), TienThuong = DocO(dr, 10), NgayCapPhat = DocO(dr, 11), CanBoCapPhat = DocO(dr, 12), GhiChu_Khen = DocO(dr, 13)
                });
            return list;
        }
        private List<KhenThuongTapTheDTO> LayDanhSachTapThe()
        {
            var list = new List<KhenThuongTapTheDTO>();
            using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path));
            conn.Open();
            if (!CoBang(conn, Module_CheDoDoiTuong.BangKhenThuongTapThe)) return list;   // máy chưa từng dùng khen thưởng tập thể: sao lưu 0 dòng thay vì báo lỗi
            using var cmd = new SqliteCommand($"SELECT ID, STT, TenTapThe, HinhThuc_KhenThuong, DonVi_CapKhenThuong, SoQuyetDinh, NgayQuyetDinh, NguoiKy, NoiDung_KhenThuong, TienThuong, NgayCapPhat, CanBoCapPhat, NguoiDaiDienNhan, GhiChu FROM {Module_CheDoDoiTuong.BangKhenThuongTapThe} ORDER BY ID", conn);
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
                list.Add(new KhenThuongTapTheDTO
                {
                    ID = DocSo(dr, 0), STT = DocSo(dr, 1), TenTapThe = DocO(dr, 2), HinhThuc_KhenThuong = DocO(dr, 3),
                    DonVi_CapKhenThuong = DocO(dr, 4), SoQuyetDinh = DocO(dr, 5), NgayQuyetDinh = DocO(dr, 6), NguoiKy = DocO(dr, 7),
                    NoiDung_KhenThuong = DocO(dr, 8), TienThuong = DocO(dr, 9), NgayCapPhat = DocO(dr, 10), CanBoCapPhat = DocO(dr, 11),
                    NguoiDaiDienNhan = DocO(dr, 12), GhiChu = DocO(dr, 13)
                });
            return list;
        }
        // ===================== ĐỌC SHEET (theo TÊN cột) =====================
        /// <summary>Bảng tra Tên cột -> số cột theo dòng tiêu đề (dòng 1). Thiếu cột bắt buộc => báo lỗi rõ ràng, không đoán.</summary>
        private sealed class BangCot
        {
            private readonly Dictionary<string, int> _cot = new(StringComparer.OrdinalIgnoreCase);
            private readonly string _sheet;
            public BangCot(IXLWorksheet ws, params string[] batBuoc)
            {
                _sheet = ws.Name;
                foreach (var c in ws.Row(1).CellsUsed())
                {
                    string ten = c.GetString().Trim();
                    if (ten.Length > 0) _cot.TryAdd(ten, c.Address.ColumnNumber);
                }
                var thieu = batBuoc.Where(t => !_cot.ContainsKey(t)).ToList();
                if (thieu.Count > 0)
                    throw new InvalidDataException($"Tệp dữ liệu không đúng cấu trúc! Sheet {_sheet} thiếu cột: {string.Join(", ", thieu)}");
            }
            /// <summary>Ô rỗng => null (đúng như NULL trong CSDL). Cột không có (tệp bản rất cũ) => null.</summary>
            public string? Chuoi(IXLRow row, string ten)
            {
                if (!_cot.TryGetValue(ten, out int c)) return null;
                var o = row.Cell(c);
                if (o.IsEmpty()) return null;
                return o.DataType == XLDataType.Number
                    ? o.GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : o.GetString();
            }
            public long So(IXLRow row, string ten)
            {
                if (!_cot.TryGetValue(ten, out int c)) return 0;
                var o = row.Cell(c);
                if (o.IsEmpty()) return 0;
                if (o.DataType == XLDataType.Number) return (long)Math.Round(o.GetDouble());
                string s = o.GetString().Trim();
                if (long.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out long kq)) return kq;
                throw new InvalidDataException($"Sheet {_sheet} dòng {row.RowNumber()}: cột {ten} không phải số ('{s}').");
            }
        }
        /// <summary>Các dòng dữ liệu (từ dòng 2 tới dòng cuối có dữ liệu), bỏ dòng trống hoàn toàn.</summary>
        private static IEnumerable<IXLRow> CacDongDuLieu(IXLWorksheet ws)
        {
            int cuoi = ws.LastRowUsed()?.RowNumber() ?? 1;
            for (int r = 2; r <= cuoi; r++)
            {
                var row = ws.Row(r);
                if (row.IsEmpty()) continue;
                yield return row;
            }
        }
        private List<CBCSDuocKhenThuongDTO> DocSheetCBCS(IXLWorksheet ws)
        {
            if (ws.LastRowUsed() == null) return new List<CBCSDuocKhenThuongDTO>();
            var c = new BangCot(ws, "ID", "HoVaTen", "SoHieu");
            var list = new List<CBCSDuocKhenThuongDTO>();
            foreach (var row in CacDongDuLieu(ws))
                list.Add(new CBCSDuocKhenThuongDTO
                {
                    ID = c.So(row, "ID"), HoVaTen = c.Chuoi(row, "HoVaTen"), SoHieu = c.Chuoi(row, "SoHieu"), DonVi = c.Chuoi(row, "DonVi"),
                    TinhTrang = c.Chuoi(row, "TinhTrang"), SoLuong_Khen = c.Chuoi(row, "SoLuong_Khen"), GhiChu_Khen = c.Chuoi(row, "GhiChu_Khen")
                });
            return list;
        }
        private List<GiayKhenDTO> DocSheetGiayKhen(IXLWorksheet ws)
        {
            if (ws.LastRowUsed() == null) return new List<GiayKhenDTO>();
            var c = new BangCot(ws, "ID", "HoVaTen", "SoHieu", "HinhThuc_Khen", "QuyetDinh_Khen", "NgayCapQD_Khen", "DonVi_Khen", "VeViec_Khen");
            var list = new List<GiayKhenDTO>();
            foreach (var row in CacDongDuLieu(ws))
                list.Add(new GiayKhenDTO
                {
                    ID = c.So(row, "ID"), HoVaTen = c.Chuoi(row, "HoVaTen"), SoHieu = c.Chuoi(row, "SoHieu"), DonVi = c.Chuoi(row, "DonVi"),
                    TinhTrang = c.Chuoi(row, "TinhTrang"), HinhThuc_Khen = c.Chuoi(row, "HinhThuc_Khen"), QuyetDinh_Khen = c.Chuoi(row, "QuyetDinh_Khen"),
                    NgayCapQD_Khen = c.Chuoi(row, "NgayCapQD_Khen"), DonVi_Khen = c.Chuoi(row, "DonVi_Khen"), VeViec_Khen = c.Chuoi(row, "VeViec_Khen"),
                    TienThuong = c.Chuoi(row, "TienThuong"), NgayCapPhat = c.Chuoi(row, "NgayCapPhat"), CanBoCapPhat = c.Chuoi(row, "CanBoCapPhat"),
                    GhiChu_Khen = c.Chuoi(row, "GhiChu_Khen")
                });
            return list;
        }
        private List<KhenThuongTapTheDTO> DocSheetTapThe(IXLWorksheet ws)
        {
            var list = new List<KhenThuongTapTheDTO>();
            if (ws.LastRowUsed() == null) return list;   // sheet trống hoàn toàn
            var c = new BangCot(ws, "ID", "TenTapThe");
            foreach (var row in CacDongDuLieu(ws))
                list.Add(new KhenThuongTapTheDTO
                {
                    ID = c.So(row, "ID"), STT = c.So(row, "STT"), TenTapThe = c.Chuoi(row, "TenTapThe"),
                    HinhThuc_KhenThuong = c.Chuoi(row, "HinhThuc_KhenThuong"), DonVi_CapKhenThuong = c.Chuoi(row, "DonVi_CapKhenThuong"),
                    SoQuyetDinh = c.Chuoi(row, "SoQuyetDinh"), NgayQuyetDinh = c.Chuoi(row, "NgayQuyetDinh"), NguoiKy = c.Chuoi(row, "NguoiKy"),
                    NoiDung_KhenThuong = c.Chuoi(row, "NoiDung_KhenThuong"), TienThuong = c.Chuoi(row, "TienThuong"), NgayCapPhat = c.Chuoi(row, "NgayCapPhat"),
                    CanBoCapPhat = c.Chuoi(row, "CanBoCapPhat"), NguoiDaiDienNhan = c.Chuoi(row, "NguoiDaiDienNhan"), GhiChu = c.Chuoi(row, "GhiChu")
                });
            return list;
        }
    }
    public class CBCSDuocKhenThuongDTO
    {
        public long ID { get; set; }
        public string? HoVaTen { get; set; }
        public string? SoHieu { get; set; }
        public string? DonVi { get; set; }
        public string? TinhTrang { get; set; }
        public string? SoLuong_Khen { get; set; }
        public string? GhiChu_Khen { get; set; }
    }
    public class GiayKhenDTO
    {
        public long ID { get; set; }
        public string? HoVaTen { get; set; }
        public string? SoHieu { get; set; }
        public string? DonVi { get; set; }
        public string? TinhTrang { get; set; }
        public string? HinhThuc_Khen { get; set; }
        public string? QuyetDinh_Khen { get; set; }
        public string? NgayCapQD_Khen { get; set; }
        public string? DonVi_Khen { get; set; }
        public string? VeViec_Khen { get; set; }
        public string? TienThuong { get; set; }
        public string? NgayCapPhat { get; set; }
        public string? CanBoCapPhat { get; set; }
        public string? GhiChu_Khen { get; set; }
    }
    public class KhenThuongTapTheDTO
    {
        public long ID { get; set; }
        public long STT { get; set; }
        public string? TenTapThe { get; set; }
        public string? HinhThuc_KhenThuong { get; set; }
        public string? DonVi_CapKhenThuong { get; set; }
        public string? SoQuyetDinh { get; set; }
        public string? NgayQuyetDinh { get; set; }
        public string? NguoiKy { get; set; }
        public string? NoiDung_KhenThuong { get; set; }
        public string? TienThuong { get; set; }
        public string? NgayCapPhat { get; set; }
        public string? CanBoCapPhat { get; set; }
        public string? NguoiDaiDienNhan { get; set; }
        public string? GhiChu { get; set; }
    }
}