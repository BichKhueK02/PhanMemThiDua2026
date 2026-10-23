///Phiên bản 2 (Đang test)
using ClosedXML.Excel;
using ExcelDataReader;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
namespace PhanMemThiDua2026
{
    // Mô hình dữ liệu 1 dòng CBCS / Tân binh (thay cho string[]).
    // TrungKien - https://github.com/BichKhueK02/PhanMemThiDua2026
    public class CBCSModel
    {
        public int STT { get; set; }
        /// <summary>Số dòng gốc trong Excel (dòng 1 = tiêu đề). Dùng để báo lỗi trùng đúng vị trí.</summary>
        public int DongExcel { get; set; }
        public string HoVaTen { get; set; } = "";
        public string SoHieu { get; set; } = "";
        public string NamSinh { get; set; } = "";
        public string QueQuan { get; set; } = "";
        public string NgayVaoCAND { get; set; } = "";
        public string CapBac { get; set; } = "";
        public string ChucVu { get; set; } = "";
        public string DonVi { get; set; } = "";
        public string PhanLoai { get; set; } = "";
        public string GhiChu { get; set; } = "";
        // Ràng buộc hợp lệ: bắt buộc phải có Họ tên
        public bool IsValid => !string.IsNullOrWhiteSpace(HoVaTen);
    }
    internal static class Module_XuatNhapDuLieuThiDua
    {
        // HẰNG SỐ & CẤU HÌNH DÙNG CHUNG (khai báo 1 lần, không cấp phát lại mỗi lần gọi)
        private const string SheetCBCS = "DSCBCS_PhanMemThiDua2026";
        private const string SheetTanBinh = "DSTanBinh_PhanMemThiDua2026";
        private const string SheetTapThe = "ThongKe_PhanLoaiTapThe";
        private const string TenPhienBanTanBinh = "Phiên bản dành cho tân binh";
        private const int SoDongVungMau = 4; // vùng mẫu A2:K5
        private static readonly string[] TieuDe =
            { "STT", "Họ và tên", "Số hiệu", "Năm sinh", "Quê quán", "Ngày vào CAND", "Cấp bậc", "Chức vụ", "Đơn vị", "Phân loại", "Ghi chú" };
        // Độ rộng cố định: bỏ AdjustToContents (đo từng ô bằng GDI -> điểm nghẽn lớn nhất khi xuất hàng ngàn dòng)
        private static readonly double[] DoRongCot = { 7, 30, 14, 11, 34, 16, 10, 20, 24, 16, 30 };
        // Các cột căn trái (1-based): Họ tên, Quê quán, Ghi chú. Các cột còn lại căn giữa.
        private static readonly int[] CotCanTrai = { 2, 5, 11 };
        private static readonly string[] TenThamSo =
            { "$HoVaTen", "$SoHieu", "$NamSinh", "$QueQuan", "$NgayVaoCAND", "$CapBac", "$ChucVu", "$DonVi", "$PhanLoai", "$GhiChu" };
        private const string SqlDocDanhSach =
            "SELECT STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu FROM DanhSach ORDER BY STT ASC";
        private const string SqlChenDanhSach =
            "INSERT INTO DanhSach (STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu) " +
            "VALUES ($STT, $HoVaTen, $SoHieu, $NamSinh, $QueQuan, $NgayVaoCAND, $CapBac, $ChucVu, $DonVi, $PhanLoai, $GhiChu)";
        static Module_XuatNhapDuLieuThiDua()
        {
            // Chỉ cần 1 lần cho cả vòng đời ứng dụng (ExcelDataReader đọc .xls cần code page)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
        private static string Csdl2Path => Module_DanduongGPS.DuongDanCSDL2;
        private static string ThoiGianHienTai() =>
            DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
        // MỞ THƯ MỤC & CHỌN TỆP TRONG EXPLORER
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern IntPtr ILCreateFromPathW(string pszPath);
        [DllImport("shell32.dll", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, IntPtr apidl, uint dwFlags);
        [DllImport("shell32.dll", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern void ILFree(IntPtr pidlList);
        public static void MoVaChonTepTrongExplorer(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;
            IntPtr pidl = ILCreateFromPathW(filePath);
            if (pidl == IntPtr.Zero)
            {
                MoExplorerDuPhong(filePath);
                return;
            }
            try
            {
                // HRESULT != 0 (S_OK) -> dùng đường dự phòng thay vì im lặng thất bại
                if (SHOpenFolderAndSelectItems(pidl, 0, IntPtr.Zero, 0) != 0)
                    MoExplorerDuPhong(filePath);
            }
            finally { ILFree(pidl); }
        }
        private static void MoExplorerDuPhong(string filePath)
        {
            try
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo("explorer.exe", $"/select,\"{filePath}\"") { UseShellExecute = false });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Không mở được Explorer: " + ex.Message);
            }
        }
        // XUẤT EXCEL TỔNG HỢP (điểm vào chính từ UI)
        public static void ThucThiXuatExcel(Form ownerForm, bool epXuatFileMau, DataTable? dtDanhSachGoc)
        {
            bool laTanBinh = false;
            int soLuong = 0;
            try
            {
                laTanBinh = LaPhienBanTanBinh();
                // Lấy số lượng trực tiếp từ RAM (dtDanhSachGoc), không đụng DB
                if (!epXuatFileMau && dtDanhSachGoc != null)
                    soLuong = DemSoDongHopLe(dtDanhSachGoc);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Không xác định được phiên bản/số lượng: " + ex.Message);
                laTanBinh = false;
                soLuong = 0;
            }
            bool chiXuatMau = epXuatFileMau || soLuong == 0;
            string thoiGian = DateTime.Now.ToString("ddMMyyyy_HHmmss", CultureInfo.InvariantCulture);
            string tenTep = chiXuatMau
                ? $"Mau_DanhSach_TongHop ({(laTanBinh ? "Tan Binh" : "CBCS")})_{thoiGian}.xlsx"
                : $"DanhSach_TongHop ({soLuong} {(laTanBinh ? "tân binh" : "CBCS")})_{thoiGian}.xlsx";
            // Chỉ cho lưu .xlsx: ClosedXML luôn ghi định dạng xlsx, đặt đuôi .xlsm sẽ khiến Excel từ chối mở.
            using var sfd = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                DefaultExt = "xlsx",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = tenTep
            };
            if (sfd.ShowDialog(ownerForm) != DialogResult.OK) return;
            string filePath = sfd.FileName;
            Exception? loi = ChayNenVoiLoading(ownerForm, "Đang giải mã và tạo tệp Excel...",
                () => XuatTongHop(filePath, laTanBinh, chiXuatMau, epXuatFileMau));
            if (loi != null)
            {
                string thongBao = loi is IOException
                    ? "Không ghi được tệp. Hãy đóng tệp nếu đang mở trong Excel rồi thử lại.\n\n" + loi.Message
                    : loi.Message;
                MessageBox.Show(ownerForm, "Lỗi: " + thongBao, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MoVaChonTepTrongExplorer(filePath); // mở đúng 1 lần, trên luồng UI (STA)
            }
        }
        /// <summary>Dựng toàn bộ workbook trong RAM -> lưu ĐÚNG 1 LẦN (trước đây: lưu, mở lại, lưu lần 2).</summary>
        private static void XuatTongHop(string filePath, bool laTanBinh, bool chiXuatMau, bool epXuatFileMau)
        {
            // Đọc xong ThongTin và đóng kết nối TRƯỚC khi dựng Excel -> không giữ khóa đọc SQLite suốt quá trình xuất
            List<string> chuoiMaHoa = DocTomTatGhiChu();
            using var wb = TaoWorkbookDanhSach(laTanBinh, chiXuatMau, out int soDong);
            ThemSheetBaoCaoTongHop(wb, chuoiMaHoa);
            if (!wb.Worksheets.Contains("DS_BaNhat"))
                wb.Worksheets.Add("DS_BaNhat").TabColor = XLColor.Gold;
            Module_BaNhat.XuatDuLieuVaoBangQuanLyBaNhat(wb, Csdl2Path, epXuatFileMau);
            Module_BanQuyen.DongDauExcel(wb);
            LuuAnToan(wb, filePath);
            string nhomDoiTuong = laTanBinh ? "Tân binh" : "CBCS";
            Module_NhatKy.GhiNhatKy(
                taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                hanhDong: $"Xuất danh sách tổng hợp thi đua {nhomDoiTuong} ({(chiXuatMau ? 0 : soDong)} dòng) ra tệp Excel",
                ghiChu: ThoiGianHienTai());
        }
        private static bool LaPhienBanTanBinh() =>
            (Module_TaiKhoan.LayPhienBanPhanMem() ?? "").Contains("tân binh", StringComparison.OrdinalIgnoreCase);
        private static int DemSoDongHopLe(DataTable dt)
        {
            if (!dt.Columns.Contains("HoVaTen")) return 0;
            int dem = 0;
            foreach (DataRow r in dt.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue; // đọc dòng đã xóa sẽ ném exception
                if (!string.IsNullOrWhiteSpace(Convert.ToString(r["HoVaTen"], CultureInfo.InvariantCulture))) dem++;
            }
            return dem;
        }
        // HIỂN THỊ LOADING + CHẠY NỀN (dùng chung, an toàn)
        private static Exception? ChayNenVoiLoading(Form owner, string thongBao, Action congViec)
        {
            Exception? loi = null;
            bool dangChay = true;
            using var fLoad = new Form_Loading(thongBao);
            // Chặn người dùng đóng form loading (Alt+F4...) khi tác vụ nền chưa xong,
            // tránh mở Explorer / báo "thành công" trong lúc tệp còn đang ghi dở.
            fLoad.FormClosing += (_, e) => { if (dangChay) e.Cancel = true; };
            fLoad.Shown += async (_, _) =>
            {
                try { await Task.Run(congViec); }
                catch (Exception ex) { loi = ex; }
                finally
                {
                    dangChay = false; // phần tiếp theo chạy lại trên luồng UI (SynchronizationContext)
                    if (!fLoad.IsDisposed) fLoad.DialogResult = DialogResult.OK;
                }
            };
            fLoad.ShowDialog(owner);
            return loi;
        }
        // DỰNG WORKBOOK DANH SÁCH (1 hàm duy nhất cho CBCS + Tân binh + File mẫu)
        private static XLWorkbook TaoWorkbookDanhSach(bool laTanBinh, bool chiXuatMau, out int soDong)
        {
            soDong = 0;
            var wb = new XLWorkbook();
            try
            {
                var ws = wb.Worksheets.Add(laTanBinh ? SheetTanBinh : SheetCBCS);
                if (laTanBinh) ws.TabColor = XLColor.DarkGreen;
                int fontSize = laTanBinh ? 14 : 13;
                // Đặt font cho cả sheet TRƯỚC khi ghi ô -> không phải duyệt lại từng ô đã tạo
                ws.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                ws.Style.Font.FontSize = fontSize;
                GhiTieuDe(ws);
                // Stream thẳng từ DB -> giải mã -> ghi ô, không qua List<CBCSModel> trung gian
                if (!chiXuatMau) soDong = DocDanhSachVaoSheet(ws, tienToSoHieu: !laTanBinh);
                if (soDong > 0) DinhDangVungDuLieu(ws, soDong);
                else TaoDuLieuMauVungData(ws, laTanBinh, fontSize);
                DatDoRongCot(ws);
                return wb;
            }
            catch
            {
                wb.Dispose();
                throw;
            }
        }
        private static void GhiTieuDe(IXLWorksheet ws)
        {
            for (int c = 0; c < TieuDe.Length; c++) ws.Cell(1, c + 1).Value = TieuDe[c];
            var h = ws.Range(1, 1, 1, TieuDe.Length);
            h.Style.Font.Bold = true;
            h.Style.Fill.BackgroundColor = XLColor.LightBlue;
            h.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            h.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            h.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            h.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            h.Style.Border.OutsideBorderColor = XLColor.Black;
        }
        private static int DocDanhSachVaoSheet(IXLWorksheet ws, bool tienToSoHieu)
        {
            int soDong = 0;
            using var conn = MoKetNoi(Csdl2Path);
            using var cmd = TaoLenh(conn, null, SqlDocDanhSach);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                soDong++;
                GhiDong(ws, soDong + 1, rd, tienToSoHieu);
            }
            return soDong;
        }
        private static void GhiDong(IXLWorksheet ws, int row, SqliteDataReader rd, bool tienToSoHieu)
        {
            int stt = rd.IsDBNull(0) ? row - 1 : rd.GetInt32(0);
            ws.Cell(row, 1).Value = "'" + stt;                                   // giữ dạng văn bản
            ws.Cell(row, 2).Value = GiaiMaO(rd, 1);
            ws.Cell(row, 3).Value = (tienToSoHieu ? "'" : "") + GiaiMaO(rd, 2);
            ws.Cell(row, 4).Value = "'" + GiaiMaO(rd, 3);
            ws.Cell(row, 5).Value = GiaiMaO(rd, 4);
            ws.Cell(row, 6).Value = "'" + GiaiMaO(rd, 5);
            ws.Cell(row, 7).Value = GiaiMaO(rd, 6);
            ws.Cell(row, 8).Value = GiaiMaO(rd, 7);
            ws.Cell(row, 9).Value = GiaiMaO(rd, 8);
            ws.Cell(row, 10).Value = GiaiMaO(rd, 9);
            ws.Cell(row, 11).Value = GiaiMaO(rd, 10);
        }
        private static string GiaiMaO(SqliteDataReader rd, int i) =>
            rd.IsDBNull(i) ? "" : Module_BaoMatAES.GiaiMa(rd.GetString(i));
        private static void DinhDangVungDuLieu(IXLWorksheet ws, int soDong)
        {
            int cuoi = soDong + 1;
            var vung = ws.Range(2, 1, cuoi, TieuDe.Length);
            vung.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            vung.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            vung.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            vung.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            foreach (int c in CotCanTrai)
                ws.Range(2, c, cuoi, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        }
        private static void DatDoRongCot(IXLWorksheet ws)
        {
            for (int c = 0; c < DoRongCot.Length; c++) ws.Column(c + 1).Width = DoRongCot[c];
        }
        // Đổ dữ liệu mẫu + định dạng vùng A2:K5 (dùng cho file mẫu / DB trống)
        private static void TaoDuLieuMauVungData(IXLWorksheet ws, bool laTanBinh, int fontSize)
        {
            string nam = DateTime.Now.Year.ToString(CultureInfo.InvariantCulture);
            string[][] mau = laTanBinh
                ? new[]
                {
                    new[] { "1", "Nguyễn Văn A (Mẫu)", "(Bỏ trống)", "1996", "Hòa Thuận, An Giang", "'02/" + nam, "B2", "CS", "C1, DHL1", $"{Module_HeThong.Loai_1}", "Đảng viên DB" },
                    new[] { "2", "Nguyễn Văn B (Mẫu)", "(Bỏ trống)", "1996", "Hòa Thuận, An Giang", "'02/" + nam, "B2", "CS", "C2, DHL1", $"{Module_HeThong.Loai_2}", "Đảng viên DB" },
                    new[] { "3", "Nguyễn Văn C (Mẫu)", "(Bỏ trống)", "1996", "Hòa Thuận, An Giang", "'02/" + nam, "B2", "CS", "C3, DHL1", $"{Module_HeThong.Loai_3}", "" }
                }
                : new[]
                {
                    new[] { "1", "Nguyễn Văn A (Mẫu)", "'123456", "1996", "Hòa Thuận, An Giang", "'02/2016", "U2", "Cán bộ", "TTM,D2", $"{Module_HeThong.Loai_1}", "Đảng viên DB" },
                    new[] { "2", "Nguyễn Văn B (Mẫu)", "'123789", "1997", "Hòa Thuận, An Giang", "'02/2017", "U2", "Cán bộ", "TCT,D2", $"{Module_HeThong.Loai_2}", "Rớt CAK" }
                };
            for (int i = 0; i < mau.Length; i++)
                for (int j = 0; j < mau[i].Length; j++)
                    ws.Cell(i + 2, j + 1).Value = mau[i][j];
            var vung = ws.Range(2, 1, 1 + SoDongVungMau, TieuDe.Length);
            vung.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
            vung.Style.Font.FontSize = fontSize;
            vung.Style.Font.FontColor = XLColor.Blue;
            vung.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            vung.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            vung.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            vung.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            foreach (int c in CotCanTrai)
                ws.Range(2, c, 1 + SoDongVungMau, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        }
        // SHEET ẨN "BAO_CAO_TONG_HOP"
        private static List<string> DocTomTatGhiChu()
        {
            var ds = new List<string>();
            using var conn = MoKetNoi(Csdl2Path);
            using var cmd = TaoLenh(conn, null, "SELECT TomTatGhiChu FROM ThongTin");
            using var rd = cmd.ExecuteReader();
            while (rd.Read()) ds.Add(rd.IsDBNull(0) ? "" : rd.GetString(0));
            return ds;
        }
        private static void ThemSheetBaoCaoTongHop(XLWorkbook wb, List<string> chuoiMaHoa)
        {
            var ws = wb.Worksheets.Add("BAO_CAO_TONG_HOP");
            ws.Visibility = XLWorksheetVisibility.Hidden;
            ws.TabColor = XLColor.FromArgb(220, 255, 220);
            ws.Column("A").Width = 14;
            ws.Column("B").Width = 160;
            ws.Cell("A1").Value = "Chuỗi mã hóa";
            ws.Cell("A1").Style.Font.Bold = true;
            int row = 1;
            foreach (string encodedFromDb in chuoiMaHoa)
            {
                ws.Cell(row, 2).Value = Module_BaoMatAES.MaHoaGiaCo(encodedFromDb);
                ws.Row(row).Height = 190;
                row++;
            }
            if (row > 1)
            {
                var vung = ws.Range(1, 1, row - 1, 2);
                vung.Style.Fill.BackgroundColor = XLColor.FromArgb(220, 255, 220);
                vung.Style.Alignment.WrapText = true;
                vung.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                vung.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
        }
        /// <summary>Ghi ra tệp tạm cùng thư mục rồi đổi tên: lỗi giữa chừng không để lại tệp Excel hỏng/cụt.</summary>
        private static void LuuAnToan(XLWorkbook wb, string filePath)
        {
            string tam = filePath + ".tmp.xlsx";
            try
            {
                wb.SaveAs(tam);
                File.Move(tam, filePath, overwrite: true);
            }
            finally
            {
                try { if (File.Exists(tam)) File.Delete(tam); } catch { /* dọn tệp tạm, bỏ qua lỗi */ }
            }
        }
        // CÁC HÀM XUẤT CÔNG KHAI (giữ nguyên chữ ký để không vỡ chỗ gọi cũ)
        public static void XuatDanhSachRaExcelCBCS(string filePath) => XuatDanhSachDonGian(filePath, laTanBinh: false);
        public static void XuatDanhSachRaExcelTanBinh(string filePath) => XuatDanhSachDonGian(filePath, laTanBinh: true);
        private static void XuatDanhSachDonGian(string filePath, bool laTanBinh)
        {
            string nhom = laTanBinh ? "Tân binh" : "CBCS";
            try
            {
                using var wb = TaoWorkbookDanhSach(laTanBinh, chiXuatMau: false, out int soDong);
                LuuAnToan(wb, filePath);
                if (soDong > 0) GhiNhatKyVaMoThuMuc(soDong, filePath, nhom, isExport: true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xuất dữ liệu {nhom}:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Xuất file mẫu: không đụng DB
        public static void XuatTepExcelMau(string filePath, string phienBan)
        {
            bool laTanBinh = phienBan == TenPhienBanTanBinh;
            using var wb = TaoWorkbookDanhSach(laTanBinh, chiXuatMau: true, out _);
            LuuAnToan(wb, filePath);
        }
        // NHẬP EXCEL (4 hàm cũ -> 1 bộ đọc streaming + 1 bộ ghi CSDL)
        public static List<string> NhapDanhSachExcelCBCS(string excelPath, bool xoaDuLieuCu)
        {
            var data = DocDanhSachTuExcel(excelPath, SheetCBCS);
            var kq = GhiVaoCsdl(data, xoaDuLieuCu, kiemTraSoHieu: false);
            GhiNhatKyVaMoThuMuc(kq.SoThanhCong, excelPath, "CBCS", isExport: false);
            return kq.DongTrung; // CBCS không kiểm tra trùng -> luôn rỗng
        }
        public static List<string> NhapDanhSachExcelTanBinh(string excelPath, bool xoaDuLieuCu)
        {
            var data = DocDanhSachTuExcel(excelPath, SheetTanBinh);
            var kq = GhiVaoCsdl(data, xoaDuLieuCu, kiemTraSoHieu: true);
            GhiNhatKyVaMoThuMuc(kq.SoThanhCong, excelPath, "Tân binh", isExport: false);
            return kq.DongTrung;
        }
        public static List<string> NhapDanhSachExcelCBCSFastDataReader(string filePath, bool xoaDuLieuCu)
        {
            var data = DocDanhSachTuExcel(filePath, SheetCBCS);
            return GhiVaoCsdl(data, xoaDuLieuCu, kiemTraSoHieu: false).DongTrung;
        }
        public static List<string> NhapDanhSachExcelTanBinhFastDataReader(string filePath, bool xoaDuLieuCu)
        {
            var data = DocDanhSachTuExcel(filePath, SheetTanBinh);
            return GhiVaoCsdl(data, xoaDuLieuCu, kiemTraSoHieu: true).DongTrung;
        }
        /// <summary>
        /// Đọc streaming đúng 1 sheet bằng ExcelDataReader: không nạp cả workbook (ClosedXML) và không
        /// nạp mọi sheet vào DataSet. Mở tệp với FileShare.ReadWrite để đọc được cả khi tệp đang mở trong Excel.
        /// </summary>
        private static List<CBCSModel> DocDanhSachTuExcel(string excelPath, string tenSheet)
        {
            if (string.IsNullOrWhiteSpace(excelPath) || !File.Exists(excelPath))
                throw new FileNotFoundException("Đường dẫn Excel không hợp lệ!", excelPath);
            var data = new List<CBCSModel>();
            bool timThay = false;
            using (var stream = MoTepDoc(excelPath))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                do
                {
                    if (!string.Equals(reader.Name, tenSheet, StringComparison.OrdinalIgnoreCase)) continue;
                    timThay = true;
                    int dong = 0;
                    while (reader.Read())
                    {
                        dong++;
                        if (dong == 1) continue; // dòng tiêu đề
                        var m = new CBCSModel
                        {
                            DongExcel = dong,
                            HoVaTen = DocO(reader, 1),
                            SoHieu = DocO(reader, 2, boDauNhay: true),
                            NamSinh = DocO(reader, 3, boDauNhay: true),
                            QueQuan = DocO(reader, 4),
                            NgayVaoCAND = DocO(reader, 5, boDauNhay: true),
                            CapBac = DocO(reader, 6),
                            ChucVu = DocO(reader, 7),
                            DonVi = DocO(reader, 8),
                            PhanLoai = DocO(reader, 9),
                            GhiChu = DocO(reader, 10)
                        };
                        if (m.IsValid) data.Add(m);
                    }
                    break; // đã đọc xong sheet cần thiết, bỏ qua các sheet còn lại
                } while (reader.NextResult());
            }
            if (!timThay) throw new InvalidDataException($"Không tìm thấy Sheet '{tenSheet}' trong tệp Excel.");
            if (data.Count == 0) throw new InvalidDataException("Tệp Excel không có dữ liệu hợp lệ!");
            return data;
        }
        private static FileStream MoTepDoc(string path) =>
            new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 81920, FileOptions.SequentialScan);
        private static string DocO(IExcelDataReader rd, int i, bool boDauNhay = false)
        {
            if (i >= rd.FieldCount) return ""; // dòng ngắn hơn 11 cột -> không vỡ IndexOutOfRange
            object? v = rd.GetValue(i);
            if (v == null || v is DBNull) return "";
            string s = v switch
            {
                string str => str,
                DateTime d => d.TimeOfDay == TimeSpan.Zero
                    ? d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                    : d.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                _ => Convert.ToString(v, CultureInfo.InvariantCulture) ?? ""
            };
            s = s.Trim();
            return boDauNhay ? s.Trim('\'') : s;
        }
        // GHI CSDL: xóa cũ + nạp mới nằm TRONG CÙNG 1 TRANSACTION
        private static (List<string> DongTrung, int SoThanhCong) GhiVaoCsdl(List<CBCSModel> data, bool xoaDuLieuCu, bool kiemTraSoHieu)
        {
            var dongTrung = new List<string>();
            int soThanhCong = 0;
            using var conn = MoKetNoi(Csdl2Path);
            using var tran = conn.BeginTransaction(); // thoát không Commit => tự Rollback, dữ liệu cũ còn nguyên
            if (xoaDuLieuCu)
            {
                using var cmdDel = TaoLenh(conn, tran, "DELETE FROM DanhSach");
                cmdDel.ExecuteNonQuery();
            }
            // STT nối tiếp theo MAX(STT) (đúng cả khi STT đang có khoảng trống sau các lần xóa)
            int stt = xoaDuLieuCu ? 1 : LaySttTiepTheo(conn, tran);
            // Cấp số hiệu (chỉ Tân binh)
            string?[] soHieuGan = new string?[data.Count];
            bool[] boQua = new bool[data.Count];
            if (kiemTraSoHieu)
            {
                // 1 tập duy nhất cho "đã có trong DB" + "đã dùng trong lần nạp này"
                var daDung = xoaDuLieuCu ? new HashSet<string>() : DocSoHieuDaCo(conn, tran);
                // Lượt 1: giữ chỗ các số hiệu "ID..." do người dùng tự nhập -> ID tự sinh không thể chiếm chỗ của chúng
                for (int i = 0; i < data.Count; i++)
                {
                    var m = data[i];
                    if (string.IsNullOrWhiteSpace(m.SoHieu) || !m.SoHieu.StartsWith("ID", StringComparison.Ordinal)) continue;
                    if (daDung.Add(m.SoHieu)) soHieuGan[i] = m.SoHieu;
                    else
                    {
                        boQua[i] = true;
                        dongTrung.Add($"{m.DongExcel}. {m.HoVaTen} - {m.NamSinh} - {m.QueQuan} - {m.DonVi}: Trùng số hiệu {m.SoHieu}");
                    }
                }
                // Lượt 2: tự sinh ID cho các dòng còn lại
                int chiSo = 1;
                for (int i = 0; i < data.Count; i++)
                {
                    if (boQua[i] || soHieuGan[i] != null) continue;
                    string ungVien;
                    do { ungVien = $"ID{chiSo:D5}"; chiSo++; }
                    while (!daDung.Add(ungVien));
                    soHieuGan[i] = ungVien;
                }
            }
            using var cmd = TaoLenh(conn, tran, SqlChenDanhSach);
            var pSTT = cmd.Parameters.Add("$STT", SqliteType.Integer);
            var pg = new SqliteParameter[TenThamSo.Length];
            for (int k = 0; k < pg.Length; k++) pg[k] = cmd.Parameters.Add(TenThamSo[k], SqliteType.Text);
            for (int i = 0; i < data.Count; i++)
            {
                if (boQua[i]) continue;
                var m = data[i];
                pSTT.Value = stt++;
                pg[0].Value = Module_BaoMatAES.MaHoa(m.HoVaTen);
                pg[1].Value = Module_BaoMatAES.MaHoa(kiemTraSoHieu ? soHieuGan[i]! : m.SoHieu);
                pg[2].Value = Module_BaoMatAES.MaHoa(m.NamSinh);
                pg[3].Value = Module_BaoMatAES.MaHoa(m.QueQuan);
                pg[4].Value = Module_BaoMatAES.MaHoa(m.NgayVaoCAND);
                pg[5].Value = Module_BaoMatAES.MaHoa(m.CapBac);
                pg[6].Value = Module_BaoMatAES.MaHoa(m.ChucVu);
                pg[7].Value = Module_BaoMatAES.MaHoa(m.DonVi);
                pg[8].Value = Module_BaoMatAES.MaHoa(m.PhanLoai);
                pg[9].Value = Module_BaoMatAES.MaHoa(m.GhiChu);
                cmd.ExecuteNonQuery();
                soThanhCong++;
            }
            tran.Commit();
            return (dongTrung, soThanhCong);
        }
        private static int LaySttTiepTheo(SqliteConnection conn, SqliteTransaction tran)
        {
            using var cmd = TaoLenh(conn, tran, "SELECT MAX(STT) FROM DanhSach");
            object? kq = cmd.ExecuteScalar();
            return kq is null or DBNull ? 1 : Convert.ToInt32(kq, CultureInfo.InvariantCulture) + 1;
        }
        private static HashSet<string> DocSoHieuDaCo(SqliteConnection conn, SqliteTransaction tran)
        {
            var tap = new HashSet<string>(StringComparer.Ordinal);
            using var cmd = TaoLenh(conn, tran, "SELECT SoHieu FROM DanhSach WHERE SoHieu IS NOT NULL AND SoHieu <> ''");
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                string sh = Module_BaoMatAES.GiaiMa(rd.GetString(0));
                if (!string.IsNullOrWhiteSpace(sh)) tap.Add(sh);
            }
            return tap;
        }
        // TIỆN ÍCH SQLITE
        /// <summary>Mode=ReadWrite: sai đường dẫn sẽ báo lỗi thay vì lặng lẽ tạo ra 1 CSDL rỗng mới.</summary>
        private static SqliteConnection MoKetNoi(string duongDanCsdl)
        {
            var csb = new SqliteConnectionStringBuilder { DataSource = duongDanCsdl, Mode = SqliteOpenMode.ReadWrite };
            var conn = new SqliteConnection(csb.ToString());
            try
            {
                conn.Open();
                return conn;
            }
            catch
            {
                conn.Dispose();
                throw;
            }
        }
        private static SqliteCommand TaoLenh(SqliteConnection conn, SqliteTransaction? tran, string sql)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Transaction = tran;
            return cmd;
        }
        // HÀM TIỆN ÍCH DÙNG CHUNG: ghi nhật ký (+ mở thư mục nếu là xuất)
        private static void GhiNhatKyVaMoThuMuc(int soLuong, string path, string doiTuong, bool isExport)
        {
            string hanhDong = isExport
                ? $"Xuất danh sách {soLuong} {doiTuong} theo đường dẫn {path}"
                : $"Nạp danh sách {soLuong} {doiTuong} từ file {Path.GetFileName(path)}";
            Module_NhatKy.GhiNhatKy(
                taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                hanhDong: hanhDong,
                ghiChu: $"Thời gian: {ThoiGianHienTai()}");
            if (isExport) MoVaChonTepTrongExplorer(path);
        }
        // NẠP DỮ LIỆU THI ĐUA TẬP THỂ
        private sealed class DuLieuTapThe
        {
            public List<string> TenCot { get; } = new();
            public List<int> ChiSoCot { get; } = new();
            public List<string?[]> Dong { get; } = new();
        }
        public static async Task KiemTraVaNapDuLieuTapTheAsync(string filePath, string csdl4Path, IWin32Window? owner = null)
        {
            // 1. Quét tệp ở luồng nền (trước đây new XLWorkbook chạy ngay trên UI -> đơ giao diện)
            DuLieuTapThe? du;
            try
            {
                du = await Task.Run(() => DocSheetTapThe(filePath));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi đọc sheet tập thể: " + ex.Message);
                return;
            }
            // 2. Không có sheet / không có dữ liệu -> thoát lặng lẽ
            if (du == null) return;
            // 3. Chỉ hỏi khi tệp thực sự có dữ liệu tập thể
            var traLoi = MessageBox.Show(
                owner,
                "Phát hiện dữ liệu 'Thống kê phân loại tập thể' trong file Excel.\n\nBạn có muốn nạp dữ liệu thi đua tập thể này vào hệ thống không?",
                "Xác nhận nạp dữ liệu Tập thể",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (traLoi != DialogResult.Yes) return;
            // 4. Ghi CSDL ở luồng nền; thông báo lỗi hiển thị trên luồng UI
            try
            {
                await Task.Run(() => GhiTapThe(csdl4Path, du));
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, "Lỗi khi nạp dữ liệu tập thể vào CSDL:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // Ghi nhật ký nằm NGOÀI khối ghi CSDL: lỗi nhật ký không còn bị nhầm thành lỗi nạp dữ liệu
            try
            {
                Module_NhatKy.GhiNhatKy(
                    taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                    hanhDong: "Nạp dữ liệu thi đua tập thể thành công",
                    ghiChu: ThoiGianHienTai());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi ghi nhật ký tập thể: " + ex.Message);
            }
        }
        private static DuLieuTapThe? DocSheetTapThe(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            using var stream = MoTepDoc(filePath);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            do
            {
                if (!string.Equals(reader.Name, SheetTapThe, StringComparison.OrdinalIgnoreCase)) continue;
                var kq = new DuLieuTapThe();
                if (!reader.Read()) return null; // sheet rỗng
                // Dòng đầu = tiêu đề (không còn cố định "dòng 2" như trước)
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string ten = DocO(reader, i);
                    if (ten.Length == 0) continue;
                    kq.TenCot.Add(ten);
                    kq.ChiSoCot.Add(i);
                }
                if (kq.TenCot.Count == 0) return null;
                while (reader.Read())
                {
                    var giaTri = new string?[kq.TenCot.Count];
                    bool coDuLieu = false;
                    for (int k = 0; k < giaTri.Length; k++)
                    {
                        string v = DocO(reader, kq.ChiSoCot[k]);
                        if (v.Length == 0) continue;
                        giaTri[k] = v;
                        coDuLieu = true;
                    }
                    if (coDuLieu) kq.Dong.Add(giaTri); // bỏ dòng trống (trước đây chèn cả dòng NULL)
                }
                return kq.Dong.Count > 0 ? kq : null;
            } while (reader.NextResult());
            return null;
        }
        private static void GhiTapThe(string csdl4Path, DuLieuTapThe du)
        {
            using var conn = MoKetNoi(csdl4Path);
            // CHỐNG SQL INJECTION: tên cột lấy từ tiêu đề Excel (dữ liệu bên ngoài) -> chỉ chấp nhận
            // tên cột có thật trong bảng, và luôn đặt trong dấu nháy kép.
            var cotHopLe = DocTenCotBang(conn, SheetTapThe);
            if (cotHopLe.Count == 0)
                throw new InvalidDataException($"Không tìm thấy bảng '{SheetTapThe}' trong CSDL.");
            var cotSql = new List<string>(du.TenCot.Count);
            var laCotId = new List<bool>(du.TenCot.Count);
            var khongHopLe = new List<string>();
            var daGap = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string ten in du.TenCot)
            {
                if (!cotHopLe.TryGetValue(ten, out string? chuan)) { khongHopLe.Add(ten); continue; }
                if (!daGap.Add(chuan)) throw new InvalidDataException($"Tiêu đề cột bị trùng: {ten}");
                cotSql.Add("\"" + chuan.Replace("\"", "\"\"") + "\"");
                laCotId.Add(chuan.Equals("ID", StringComparison.OrdinalIgnoreCase));
            }
            if (khongHopLe.Count > 0)
                throw new InvalidDataException("Tệp Excel có cột không khớp với CSDL: " + string.Join(", ", khongHopLe));
            var dsThamSo = new string[cotSql.Count];
            for (int k = 0; k < dsThamSo.Length; k++) dsThamSo[k] = "$p" + k;
            using var tran = conn.BeginTransaction(); // lỗi giữa chừng => tự Rollback khi thoát khối
            using var cmd = TaoLenh(conn, tran,
                $"INSERT OR REPLACE INTO \"{SheetTapThe}\" ({string.Join(", ", cotSql)}) VALUES ({string.Join(", ", dsThamSo)})");
            var ps = new SqliteParameter[dsThamSo.Length];
            for (int k = 0; k < ps.Length; k++) ps[k] = cmd.Parameters.Add(dsThamSo[k], SqliteType.Text);
            // 1 command + tham số tạo 1 lần, chỉ đổi Value theo từng dòng (trước đây: tạo command + AddWithValue mỗi dòng)
            foreach (var dong in du.Dong)
            {
                for (int k = 0; k < ps.Length; k++)
                {
                    string? v = dong[k];
                    object giaTri = DBNull.Value;
                    if (v != null)
                        giaTri = laCotId[k] ? v : Module_BaoMatAES.MaHoa(v); // 🔒 mã hóa AES, riêng cột ID giữ nguyên
                    ps[k].Value = giaTri;
                }
                cmd.ExecuteNonQuery();
            }
            tran.Commit();
        }
        private static Dictionary<string, string> DocTenCotBang(SqliteConnection conn, string tenBang)
        {
            var kq = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using var cmd = TaoLenh(conn, null, $"PRAGMA table_info(\"{tenBang}\")"); // tenBang là hằng nội bộ
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                string ten = rd.GetString(1);
                kq[ten] = ten;
            }
            return kq;
        }
    }
}