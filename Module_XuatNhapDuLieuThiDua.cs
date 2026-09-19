using ClosedXML.Excel;
using ExcelDataReader;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;
namespace PhanMemThiDua2026
{
    // BƯỚC 1: TẠO CLASS CHUẨN ĐỂ QUẢN LÝ DỮ LIỆU (Thay thế string[])
    //code chuẩn kỷ sư phần mềm, đảm bảo hệ thống hoạt động 10 năm nữa ổn định
    public class CBCSModel
    {
        public int STT { get; set; }
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
        // Ràng buộc hợp lệ: Bắt buộc phải có Họ tên
        public bool IsValid => !string.IsNullOrWhiteSpace(HoVaTen);
    }
    internal class Module_XuatNhapDuLieuThiDua
    {
        private static string Csdl2Path => Module_DanduongGPS.DuongDanCSDL2;
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern IntPtr ILCreateFromPathW(string pszPath);
        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, IntPtr apidl, uint dwFlags);
        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern void ILFree(IntPtr pidlList);
        public static void MoVaChonTepTrongExplorer(string filePath)
        {
            if (!File.Exists(filePath)) return;
            IntPtr pidl = ILCreateFromPathW(filePath);
            if (pidl != IntPtr.Zero)
            {
                try { SHOpenFolderAndSelectItems(pidl, 0, IntPtr.Zero, 0); }
                finally { ILFree(pidl); }
            }
        }
        // DÁN ĐOẠN NÀY VÀO TRONG CLASS Module_XuatNhapDuLieuThiDua
        public static void ThucThiXuatExcel(Form ownerForm, bool epXuatFileMau, DataTable? dtDanhSachGoc)
        {
            string csdl2 = Csdl2Path;
            string phienBan = "";
            int soLuong = 0;
            try
            {
                bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                phienBan = laTanBinh ? "Phiên bản dành cho tân binh" : "";
                // Lấy số lượng trực tiếp từ RAM (dtDanhSachGoc)
                if (!epXuatFileMau && dtDanhSachGoc != null)
                {
                    soLuong = dtDanhSachGoc.AsEnumerable().Count(r => !string.IsNullOrWhiteSpace(r.Field<string>("HoVaTen")));
                }
            }
            catch { phienBan = ""; soLuong = 0; }
            string thoiGian = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            string fileName = "";
            if (epXuatFileMau || soLuong == 0)
            {
                fileName = (phienBan == "Phiên bản dành cho tân binh")
                    ? $"Mau_DanhSach_TongHop (Tan Binh)_{thoiGian}.xlsx"
                    : $"Mau_DanhSach_TongHop (CBCS)_{thoiGian}.xlsx";
            }
            else
            {
                fileName = (phienBan == "Phiên bản dành cho tân binh")
                    ? $"DanhSach_TongHop ({soLuong} tân binh)_{thoiGian}.xlsx"
                    : $"DanhSach_TongHop ({soLuong} CBCS)_{thoiGian}.xlsx";
            }
            using SaveFileDialog sfd = new SaveFileDialog { Filter = "Excel Files (*.xlsx, *.xlsm)|*.xlsx;*.xlsm", FileName = fileName };
            if (sfd.ShowDialog(ownerForm) != DialogResult.OK) return;
            string filePath = sfd.FileName;
            Exception? backgroundException = null;
            using (Form_Loading fLoad = new Form_Loading("Đang giải mã và tạo tệp Excel..."))
            {
                fLoad.Shown += async (s, args) =>
                {
                    try
                    {
                        await Task.Run(() =>
                        {
                            if (epXuatFileMau || soLuong == 0)
                            {
                                XuatTepExcelMau(filePath, phienBan);
                            }
                            else
                            {
                                if (phienBan == "Phiên bản dành cho tân binh")
                                    XuatDanhSachRaExcelTanBinh(filePath);
                                else
                                    XuatDanhSachRaExcelCBCS(filePath);
                            }
                            try
                            {
                                using var conn2 = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={csdl2}");
                                conn2.Open();
                                using var cmd = conn2.CreateCommand();
                                cmd.CommandText = "SELECT TomTatGhiChu FROM ThongTin";
                                using var reader = cmd.ExecuteReader();
                                using var package = new ClosedXML.Excel.XLWorkbook(filePath);
                                try
                                {
                                    var mainSheet = package.Worksheet(1);
                                    if (mainSheet != null)
                                    {
                                        int lastRow = mainSheet.LastRowUsed()?.RowNumber() ?? 1;
                                        if (lastRow >= 2)
                                        {
                                            string[] centerCols = { "A", "C", "D", "F", "G", "H", "I", "J" };
                                            foreach (string col in centerCols)
                                            {
                                                var rng = mainSheet.Range($"{col}2:{col}{lastRow}");
                                                rng.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                                                rng.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine("Lỗi định dạng căn giữa sheet chính: " + ex.Message);
                                }
                                var ws = package.Worksheets.Add("BAO_CAO_TONG_HOP");
                                ws.Visibility = ClosedXML.Excel.XLWorksheetVisibility.Hidden;
                                ws.TabColor = ClosedXML.Excel.XLColor.FromArgb(220, 255, 220);
                                ws.Column("A").Width = 14;
                                ws.Column("B").Width = 160;
                                ws.Cell("A1").Value = "Chuỗi mã hóa";
                                ws.Cell("A1").Style.Font.Bold = true;
                                int row = 1;
                                while (reader.Read())
                                {
                                    string encodedFromDB = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                    string stealthData = Module_BaoMatAES.MaHoaGiaCo(encodedFromDB);
                                    ws.Cell(row, 2).Value = stealthData;
                                    ws.Row(row).Height = 190;
                                    row++;
                                }
                                if (row > 1)
                                {
                                    var dataRange = ws.Range(1, 1, row - 1, 2);
                                    dataRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(220, 255, 220);
                                    dataRange.Style.Alignment.WrapText = true;
                                    dataRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                                    dataRange.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                                }
                                try
                                {
                                    if (!package.Worksheets.Contains("DS_BaNhat"))
                                    {
                                        var wsBaNhat = package.Worksheets.Add("DS_BaNhat");
                                        wsBaNhat.TabColor = ClosedXML.Excel.XLColor.Gold;
                                    }
                                }
                                catch (Exception exBN)
                                {
                                    System.Diagnostics.Debug.WriteLine("Lỗi tạo sheet DS_BaNhat: " + exBN.Message);
                                }
                                Module_BaNhat.XuatDuLieuVaoBangQuanLyBaNhat(package, csdl2, epXuatFileMau);
                                Module_BanQuyen.DongDauExcel(package);
                                package.Save();
                                string nhomDoiTuong = (phienBan == "Phiên bản dành cho tân binh") ? "Tân binh" : "CBCS";
                                Module_NhatKy.GhiNhatKy(
                                    Module_TaiKhoan.TenTaiKhoan_RAM,
                                    $"Xuất danh sách tổng hợp thi đua {nhomDoiTuong} ({soLuong} dòng) ra tệp Excel",
                                    DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")
                                );
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("Lỗi khi xử lý file Excel (Định dạng & Thêm Sheet): " + ex.Message, ex);
                            }
                        });
                    }
                    catch (Exception ex) { backgroundException = ex; }
                    finally
                    {
                        if (!fLoad.IsDisposed)
                        {
                            if (fLoad.InvokeRequired) fLoad.Invoke(new Action(() => fLoad.DialogResult = DialogResult.OK));
                            else fLoad.DialogResult = DialogResult.OK;
                        }
                    }
                };
                fLoad.ShowDialog(ownerForm);
            }
            if (backgroundException != null)
                MessageBox.Show(ownerForm, "Lỗi: " + backgroundException.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                MoVaChonTepTrongExplorer(filePath);
        }
        public static void XuatDanhSachRaExcelCBCS(string filePath)
        {
            try
            {
                List<CBCSModel> danhSach = new List<CBCSModel>();
                using (var conn = new SqliteConnection($"Data Source={Csdl2Path}"))
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu FROM DanhSach ORDER BY STT ASC";
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        danhSach.Add(new CBCSModel
                        {
                            STT = reader.GetInt32(0),
                            HoVaTen = Module_BaoMatAES.GiaiMa(reader.GetString(1)),
                            SoHieu = Module_BaoMatAES.GiaiMa(reader.GetString(2)),
                            NamSinh = Module_BaoMatAES.GiaiMa(reader.GetString(3)),
                            QueQuan = reader.IsDBNull(4) ? "" : Module_BaoMatAES.GiaiMa(reader.GetString(4)),
                            NgayVaoCAND = reader.IsDBNull(5) ? "" : Module_BaoMatAES.GiaiMa(reader.GetString(5)),
                            CapBac = reader.IsDBNull(6) ? "" : Module_BaoMatAES.GiaiMa(reader.GetString(6)),
                            ChucVu = reader.IsDBNull(7) ? "" : Module_BaoMatAES.GiaiMa(reader.GetString(7)),
                            DonVi = reader.IsDBNull(8) ? "" : Module_BaoMatAES.GiaiMa(reader.GetString(8)),
                            PhanLoai = reader.IsDBNull(9) ? "" : Module_BaoMatAES.GiaiMa(reader.GetString(9)),
                            GhiChu = reader.IsDBNull(10) ? "" : Module_BaoMatAES.GiaiMa(reader.GetString(10))
                        });
                    }
                }           
                // 🚀 FAST EMPTY EXPORT MODE (CÁN BỘ CHIẾN SĨ)
                // Không có dữ liệu -> tạo file mẫu cực nhanh, bỏ qua toàn bộ logic nặng          
                if (danhSach.Count == 0)
                {
                    using var wbEmpty = new XLWorkbook();
                    var wsEmpty = wbEmpty.Worksheets.Add("DSCBCS_PhanMemThiDua2026");
                    string[] headersEmpty = { "STT", "Họ và tên", "Số hiệu", "Năm sinh", "Quê quán", "Ngày vào CAND", "Cấp bậc", "Chức vụ", "Đơn vị", "Phân loại", "Ghi chú" };
                    for (int i = 0; i < headersEmpty.Length; i++)
                    {
                        var cell = wsEmpty.Cell(1, i + 1);
                        cell.Value = headersEmpty[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }
                    wsEmpty.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                    wsEmpty.Style.Font.FontSize = 13;
                    wsEmpty.Columns().AdjustToContents();
                    // 👇 GỌI HÀM HELPER TẠI ĐÂY 👇 (Truyền font size 13 cho CBCS)
                    TaoDuLieuMauVungData(wsEmpty, 13);
                    wbEmpty.SaveAs(filePath);
                    return; // Thoát ngay lập tức
                }
                // 🟢 FULL EXPORT MODE (Khi có dữ liệu)
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("DSCBCS_PhanMemThiDua2026");
                string[] headers = { "STT", "Họ và tên", "Số hiệu", "Năm sinh", "Quê quán", "Ngày vào CAND", "Cấp bậc", "Chức vụ", "Đơn vị", "Phân loại", "Ghi chú" };
                for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
                for (int r = 0; r < danhSach.Count; r++)
                {
                    var item = danhSach[r];
                    int row = r + 2;
                    ws.Cell(row, 1).Value = "'" + item.STT;
                    ws.Cell(row, 2).Value = item.HoVaTen;
                    ws.Cell(row, 3).Value = "'" + item.SoHieu;
                    ws.Cell(row, 4).Value = "'" + item.NamSinh;
                    ws.Cell(row, 5).Value = item.QueQuan;
                    ws.Cell(row, 6).Value = "'" + item.NgayVaoCAND;
                    ws.Cell(row, 7).Value = item.CapBac;
                    ws.Cell(row, 8).Value = item.ChucVu;
                    ws.Cell(row, 9).Value = item.DonVi;
                    ws.Cell(row, 10).Value = item.PhanLoai;
                    ws.Cell(row, 11).Value = item.GhiChu;
                }
                // 1. Định dạng toàn bộ bảng
                var fullRange = ws.Range(1, 1, danhSach.Count + 1, 11);
                fullRange.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                fullRange.Style.Font.FontSize = 13;
                fullRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                fullRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                // 2. Định dạng riêng vùng Tiêu đề (Từ A1 đến K1)
                var headerRange = ws.Range(1, 1, 1, 11);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                // 3. Tự động căn chỉnh kích thước cột 1 lần duy nhất
                ws.Columns().AdjustToContents();
                wb.SaveAs(filePath);
            }
            catch (Exception ex) { MessageBox.Show("Lỗi xuất CBCS: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        public static void XuatDanhSachRaExcelTanBinh(string filePath)
        {
            try
            {
                string csdl2 = Csdl2Path;
                List<CBCSModel> danhSach = new List<CBCSModel>();
                using (var conn = new SqliteConnection($"Data Source={csdl2}"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"SELECT STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND,
                                      CapBac, ChucVu, DonVi, PhanLoai, GhiChu
                                      FROM DanhSach ORDER BY STT ASC";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                danhSach.Add(new CBCSModel
                                {
                                    STT = reader.GetInt32(0),
                                    HoVaTen = Module_BaoMatAES.GiaiMa(reader.GetString(1)),
                                    SoHieu = Module_BaoMatAES.GiaiMa(reader.GetString(2)),
                                    NamSinh = Module_BaoMatAES.GiaiMa(reader.GetString(3)),
                                    QueQuan = reader.IsDBNull(4) ? "" : Module_BaoMatAES.GiaiMa(reader.GetString(4)),
                                    NgayVaoCAND = reader.IsDBNull(5) ? "" : Module_BaoMatAES.GiaiMa(reader.GetString(5)),
                                    CapBac = reader.IsDBNull(6) ? "" : Module_BaoMatAES.GiaiMa(reader.GetString(6)),
                                    ChucVu = reader.IsDBNull(7) ? "" : Module_BaoMatAES.GiaiMa(reader.GetString(7)),
                                    DonVi = reader.IsDBNull(8) ? "" : Module_BaoMatAES.GiaiMa(reader.GetString(8)),
                                    PhanLoai = reader.IsDBNull(9) ? "" : Module_BaoMatAES.GiaiMa(reader.GetString(9)),
                                    GhiChu = reader.IsDBNull(10) ? "" : Module_BaoMatAES.GiaiMa(reader.GetString(10))
                                });
                            }
                        }
                    }
                }
                // 🚀 FAST EMPTY EXPORT MODE (TÂN BINH)
                // Không hiện MessageBox khó chịu, tạo luôn template rỗng 
                if (danhSach.Count == 0)
                {
                    using var wbEmpty = new XLWorkbook();
                    var wsEmpty = wbEmpty.Worksheets.Add("DSTanBinh_PhanMemThiDua2026");
                    wsEmpty.TabColor = XLColor.DarkGreen;
                    string[] headersEmpty = { "STT", "Họ và tên", "Số hiệu", "Năm sinh", "Quê quán", "Ngày vào CAND", "Cấp bậc", "Chức vụ", "Đơn vị", "Phân loại", "Ghi chú" };
                    for (int i = 0; i < headersEmpty.Length; i++)
                    {
                        var cell = wsEmpty.Cell(1, i + 1);
                        cell.Value = headersEmpty[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }
                    wsEmpty.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                    wsEmpty.Style.Font.FontSize = 14;
                    wsEmpty.Columns().AdjustToContents();
                    // 👇 GỌI HÀM HELPER TẠI ĐÂY 👇 (Truyền font size 13 cho CBCS)
                    TaoDuLieuMauVungData(wsEmpty, 13);
                    wbEmpty.SaveAs(filePath);
                    return; // Thoát ngay lập tức
                }
                // 🟢 FULL EXPORT MODE (Khi có dữ liệu)
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("DSTanBinh_PhanMemThiDua2026");
                ws.TabColor = XLColor.DarkGreen;
                // Gán font mặc định cho toàn bộ Sheet
                ws.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                ws.Style.Font.FontSize = 14;
                // 1. GHI TIÊU ĐỀ
                string[] headers = { "STT", "Họ và tên", "Số hiệu", "Năm sinh", "Quê quán", "Ngày vào CAND", "Cấp bậc", "Chức vụ", "Đơn vị", "Phân loại", "Ghi chú" };
                for (int c = 0; c < headers.Length; c++)
                {
                    ws.Cell(1, c + 1).Value = headers[c];
                }
                // 2. GHI DỮ LIỆU
                for (int r = 0; r < danhSach.Count; r++)
                {
                    var item = danhSach[r];
                    int rowIdx = r + 2;
                    ws.Cell(rowIdx, 1).Value = "'" + item.STT;
                    ws.Cell(rowIdx, 2).Value = item.HoVaTen;
                    ws.Cell(rowIdx, 3).Value = item.SoHieu;
                    ws.Cell(rowIdx, 4).Value = "'" + item.NamSinh;
                    ws.Cell(rowIdx, 5).Value = item.QueQuan;
                    ws.Cell(rowIdx, 6).Value = "'" + item.NgayVaoCAND;
                    ws.Cell(rowIdx, 7).Value = item.CapBac;
                    ws.Cell(rowIdx, 8).Value = item.ChucVu;
                    ws.Cell(rowIdx, 9).Value = item.DonVi;
                    ws.Cell(rowIdx, 10).Value = item.PhanLoai;
                    ws.Cell(rowIdx, 11).Value = item.GhiChu;
                }
                // 3. ĐỊNH DẠNG VÙNG DỮ LIỆU (1 lần chạm)
                if (danhSach.Count > 0)
                {
                    var dataRange = ws.Range(2, 1, danhSach.Count + 1, 11);
                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                // 4. ĐỊNH DẠNG VÙNG TIÊU ĐỀ (A1:K1)
                var headerRange = ws.Range(1, 1, 1, 11);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.OutsideBorderColor = XLColor.Black;
                // 5. CĂN CHỈNH KÍCH THƯỚC
                ws.Columns().AdjustToContents();
                ws.Rows().AdjustToContents();
                wb.SaveAs(filePath);
                // Ghi nhật ký
                GhiNhatKyVaMoThuMuc(danhSach.Count, filePath, "Tân binh", isExport: true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xuất dữ liệu Tân binh:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // 🛠️ HELPER: CHỈ ĐỊNH DẠNG VÀ ĐỔ DỮ LIỆU MẪU TRONG VÙNG A2:K5     
        private static void TaoDuLieuMauVungData(IXLWorksheet ws, int fontSize = 13)
        {
            // 1. Tự động nhận dạng chế độ dựa trên tên Sheet hoặc cấu hình hiện tại
            // Nếu Sheet tên có chứa "TanBinh" thì kích hoạt chế độ Tân binh
            bool laTanBinh = ws.Name.Contains("TanBinh", StringComparison.OrdinalIgnoreCase);
            // 2. Thiết lập cấu trúc dữ liệu mẫu
            var danhSachMau = new List<object[]>();
            if (laTanBinh)
            {
                danhSachMau.Add(new object[] { 1, "Nguyễn Văn A (Mẫu)", "(Bỏ trống)", 1996, "Hòa Thuận, An Giang", "'02/" + DateTime.Now.Year, "B2", "CS", "C1, DHL1", Module_HeThong.Loai_1, "Đảng viên DB" });
                danhSachMau.Add(new object[] { 2, "Nguyễn Văn B (Mẫu)", "(Bỏ trống)", 1996, "Hòa Thuận, An Giang", "'02/" + DateTime.Now.Year, "B2", "CS", "C2, DHL1", Module_HeThong.Loai_2, "Đảng viên DB" });
                danhSachMau.Add(new object[] { 3, "Nguyễn Văn C (Mẫu)", "(Bỏ trống)", 1996, "Hòa Thuận, An Giang", "'02/" + DateTime.Now.Year, "B2", "CS", "C3, DHL1", Module_HeThong.Loai_3, "" });
            }
            else
            {
                danhSachMau.Add(new object[] { 1, "Nguyễn Văn A (Mẫu)", "'123456", 1996, "Hòa Thuận, An Giang", "'02/2016", "U2", "Cán bộ", "TTM,D2", Module_HeThong.Loai_1, "Đảng viên DB" });
                danhSachMau.Add(new object[] { 2, "Nguyễn Văn B (Mẫu)", "'123789", 1997, "Hòa Thuận, An Giang", "'02/2017", "U2", "Cán bộ", "TCT,D2", Module_HeThong.Loai_2, "Rớt CAK" });
            }
            // 3. Đổ dữ liệu mẫu vào Sheet từ dòng 2
            for (int i = 0; i < danhSachMau.Count; i++)
            {
                for (int j = 0; j < danhSachMau[i].Length; j++)
                {
                    ws.Cell(i + 2, j + 1).Value = danhSachMau[i][j].ToString();
                }
            }
            // 4. Định dạng chung cho vùng A2:K5
            var dataRange = ws.Range(2, 1, 5, 11);
            dataRange.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
            dataRange.Style.Font.FontSize = fontSize;
            dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            dataRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Font.FontColor = XLColor.Blue;
            // 6. Căn trái các cột dữ liệu chữ
            ws.Range(2, 2, 5, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left; // Họ tên
            ws.Range(2, 5, 5, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left; // Quê quán
            ws.Range(2, 11, 5, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left; // Ghi chú
            // 7. Độ rộng cột
            ws.Columns("A:K").AdjustToContents();
            ws.Column(2).Width += 4;
            ws.Column(5).Width += 4;
        }
        // 🚀 HÀM MỚI: CHUYÊN BIỆT ĐỂ XUẤT FILE MẪU (KHÔNG CHẠM VÀO DB, KHÔNG ẢNH HƯỞNG HÀM GỐC)    
        public static void XuatTepExcelMau(string filePath, string phienBan)
        {
            using var wbEmpty = new XLWorkbook();
            bool laTanBinh = (phienBan == "Phiên bản dành cho tân binh");
            // Đặt tên sheet và màu sắc chuẩn theo phiên bản
            string sheetName = laTanBinh ? "DSTanBinh_PhanMemThiDua2026" : "DSCBCS_PhanMemThiDua2026";
            var wsEmpty = wbEmpty.Worksheets.Add(sheetName);
            if (laTanBinh) wsEmpty.TabColor = XLColor.DarkGreen;
            string[] headersEmpty = { "STT", "Họ và tên", "Số hiệu", "Năm sinh", "Quê quán", "Ngày vào CAND", "Cấp bậc", "Chức vụ", "Đơn vị", "Phân loại", "Ghi chú" };
            for (int i = 0; i < headersEmpty.Length; i++)
            {
                var cell = wsEmpty.Cell(1, i + 1);
                cell.Value = headersEmpty[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            // Cấu hình Font size (14 cho Tân Binh, 13 cho CBCS)
            int fontSize = laTanBinh ? 14 : 13;
            wsEmpty.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
            wsEmpty.Style.Font.FontSize = fontSize;
            wsEmpty.Columns().AdjustToContents();
            // Gọi Helper đổ 1 dòng dữ liệu mẫu vào vùng A2:K5
            TaoDuLieuMauVungData(wsEmpty, fontSize);
            wbEmpty.SaveAs(filePath);
        }
        public static List<string> NhapDanhSachExcelCBCS(string excelPath, bool xoaDuLieuCu)
        {
            if (string.IsNullOrWhiteSpace(excelPath) || !File.Exists(excelPath))
                throw new Exception("Đường dẫn Excel không hợp lệ!");
            string sheetName = "DSCBCS_PhanMemThiDua2026";
            string csdl2 = Csdl2Path;
            List<CBCSModel> data = new List<CBCSModel>();
            using (var wb = new XLWorkbook(excelPath))
            {
                if (!wb.Worksheets.Any(w => w.Name == sheetName))
                    throw new Exception("Không tìm thấy sheet CBCS hợp lệ!");
                var ws = wb.Worksheet(sheetName);
                int lastRow = ws.LastRowUsed().RowNumber();
                for (int r = 2; r <= lastRow; r++)
                {
                    var cbcs = new CBCSModel
                    {
                        HoVaTen = ws.Cell(r, 2).GetString().Trim(),
                        SoHieu = ws.Cell(r, 3).GetString().Trim(),
                        NamSinh = ws.Cell(r, 4).GetString().Trim(),
                        QueQuan = ws.Cell(r, 5).GetString().Trim(),
                        NgayVaoCAND = ws.Cell(r, 6).GetString().Trim(),
                        CapBac = ws.Cell(r, 7).GetString().Trim(),
                        ChucVu = ws.Cell(r, 8).GetString().Trim(),
                        DonVi = ws.Cell(r, 9).GetString().Trim(),
                        PhanLoai = ws.Cell(r, 10).GetString().Trim(),
                        GhiChu = ws.Cell(r, 11).GetString().Trim()
                    };
                    if (cbcs.IsValid) data.Add(cbcs);
                }
            }
            if (data.Count == 0) throw new Exception("File Excel không có dữ liệu hợp lệ!");
            using var conn = new SqliteConnection($"Data Source={csdl2}");
            conn.Open();
            if (xoaDuLieuCu)
            {
                using var cmdDel = conn.CreateCommand();
                cmdDel.CommandText = "DELETE FROM DanhSach";
                cmdDel.ExecuteNonQuery();
            }
            using var tran = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tran;
            cmd.CommandText = @"INSERT INTO DanhSach 
                                 (STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu)
                                 VALUES 
                                 ($STT, $HoVaTen, $SoHieu, $NamSinh, $QueQuan, $NgayVaoCAND, $CapBac, $ChucVu, $DonVi, $PhanLoai, $GhiChu)";
            var pSTT = cmd.Parameters.Add("$STT", SqliteType.Integer);
            var pHoTen = cmd.Parameters.Add("$HoVaTen", SqliteType.Text);
            var pSoHieu = cmd.Parameters.Add("$SoHieu", SqliteType.Text);
            var pNamSinh = cmd.Parameters.Add("$NamSinh", SqliteType.Text);
            var pQueQuan = cmd.Parameters.Add("$QueQuan", SqliteType.Text);
            var pNgayVao = cmd.Parameters.Add("$NgayVaoCAND", SqliteType.Text);
            var pCapBac = cmd.Parameters.Add("$CapBac", SqliteType.Text);
            var pChucVu = cmd.Parameters.Add("$ChucVu", SqliteType.Text);
            var pDonVi = cmd.Parameters.Add("$DonVi", SqliteType.Text);
            var pPhanLoai = cmd.Parameters.Add("$PhanLoai", SqliteType.Text);
            var pGhiChu = cmd.Parameters.Add("$GhiChu", SqliteType.Text);
            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                pSTT.Value = i + 1;
                pHoTen.Value = Module_BaoMatAES.MaHoa(item.HoVaTen);
                pSoHieu.Value = Module_BaoMatAES.MaHoa(item.SoHieu);
                pNamSinh.Value = Module_BaoMatAES.MaHoa(item.NamSinh);
                pQueQuan.Value = Module_BaoMatAES.MaHoa(item.QueQuan);
                pNgayVao.Value = Module_BaoMatAES.MaHoa(item.NgayVaoCAND);
                pCapBac.Value = Module_BaoMatAES.MaHoa(item.CapBac);
                pChucVu.Value = Module_BaoMatAES.MaHoa(item.ChucVu);
                pDonVi.Value = Module_BaoMatAES.MaHoa(item.DonVi);
                pPhanLoai.Value = Module_BaoMatAES.MaHoa(item.PhanLoai);
                pGhiChu.Value = Module_BaoMatAES.MaHoa(item.GhiChu);
                cmd.ExecuteNonQuery();
            }
            tran.Commit();
            GhiNhatKyVaMoThuMuc(data.Count, excelPath, "CBCS", isExport: false);
            return new List<string>(); // CBCS không có logic trùng lặp phức tạp nên trả về rỗng
        }
        // HÀM 4: NHẬP EXCEL (TÂN BINH) - ĐÃ LỌC SẠCH MESSAGEBOX
        public static List<string> NhapDanhSachExcelTanBinh(string excelPath, bool xoaDuLieuCu)
        {
            if (string.IsNullOrWhiteSpace(excelPath) || !File.Exists(excelPath))
                throw new Exception("Đường dẫn Excel không hợp lệ!");
            string sheetName = "DSTanBinh_PhanMemThiDua2026";
            string csdl2 = Csdl2Path;
            List<CBCSModel> data = new List<CBCSModel>();
            using (var wb = new XLWorkbook(excelPath))
            {
                if (!wb.Worksheets.Any(w => w.Name == sheetName))
                    throw new Exception("Không tìm thấy sheet Tân binh hợp lệ!");
                var ws = wb.Worksheet(sheetName);
                int lastRow = ws.LastRowUsed().RowNumber();
                for (int r = 2; r <= lastRow; r++)
                {
                    var cbcs = new CBCSModel
                    {
                        HoVaTen = ws.Cell(r, 2).GetString().Trim(),
                        SoHieu = ws.Cell(r, 3).GetString().Trim(),
                        NamSinh = ws.Cell(r, 4).GetString().Trim(),
                        QueQuan = ws.Cell(r, 5).GetString().Trim(),
                        NgayVaoCAND = ws.Cell(r, 6).GetString().Trim(),
                        CapBac = ws.Cell(r, 7).GetString().Trim(),
                        ChucVu = ws.Cell(r, 8).GetString().Trim(),
                        DonVi = ws.Cell(r, 9).GetString().Trim(),
                        PhanLoai = ws.Cell(r, 10).GetString().Trim(),
                        GhiChu = ws.Cell(r, 11).GetString().Trim()
                    };
                    if (cbcs.IsValid) data.Add(cbcs);
                }
            }
            if (data.Count == 0) throw new Exception("File Excel không có dữ liệu hợp lệ!");
            using var conn = new SqliteConnection($"Data Source={csdl2}");
            conn.Open();
            if (xoaDuLieuCu)
            {
                using var cmdDel = conn.CreateCommand();
                cmdDel.CommandText = "DELETE FROM DanhSach";
                cmdDel.ExecuteNonQuery();
            }
            HashSet<string> soHieuDaTonTai = new HashSet<string>();
            using (var cmdCheck = conn.CreateCommand())
            {
                cmdCheck.CommandText = "SELECT SoHieu FROM DanhSach WHERE SoHieu IS NOT NULL AND SoHieu <> ''";
                using var rd = cmdCheck.ExecuteReader();
                while (rd.Read())
                {
                    string sh = Module_BaoMatAES.GiaiMa(rd.GetString(0));
                    if (!string.IsNullOrWhiteSpace(sh)) soHieuDaTonTai.Add(sh);
                }
            }
            using var tran = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tran;
            cmd.CommandText = @"INSERT INTO DanhSach 
                                 (STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu)
                                 VALUES 
                                 ($STT, $HoVaTen, $SoHieu, $NamSinh, $QueQuan, $NgayVaoCAND, $CapBac, $ChucVu, $DonVi, $PhanLoai, $GhiChu)";
            var pSTT = cmd.Parameters.Add("$STT", SqliteType.Integer);
            var pHoTen = cmd.Parameters.Add("$HoVaTen", SqliteType.Text);
            var pSoHieu = cmd.Parameters.Add("$SoHieu", SqliteType.Text);
            var pNamSinh = cmd.Parameters.Add("$NamSinh", SqliteType.Text);
            var pQueQuan = cmd.Parameters.Add("$QueQuan", SqliteType.Text);
            var pNgayVao = cmd.Parameters.Add("$NgayVaoCAND", SqliteType.Text);
            var pCapBac = cmd.Parameters.Add("$CapBac", SqliteType.Text);
            var pChucVu = cmd.Parameters.Add("$ChucVu", SqliteType.Text);
            var pDonVi = cmd.Parameters.Add("$DonVi", SqliteType.Text);
            var pPhanLoai = cmd.Parameters.Add("$PhanLoai", SqliteType.Text);
            var pGhiChu = cmd.Parameters.Add("$GhiChu", SqliteType.Text);
            HashSet<string> soHieuTrongLanNap = new HashSet<string>();
            List<string> dongTrung = new List<string>();
            int chiSoSoHieu = 1;
            int stt = xoaDuLieuCu ? 1 : soHieuDaTonTai.Count + 1;
            int demThanhCong = 0;
            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                string soHieuSuDung = "";
                if (!string.IsNullOrWhiteSpace(item.SoHieu) && item.SoHieu.StartsWith("ID"))
                {
                    if (soHieuDaTonTai.Contains(item.SoHieu) || soHieuTrongLanNap.Contains(item.SoHieu))
                    {
                        dongTrung.Add($"{i + 2}. {item.HoVaTen} - {item.NamSinh} - {item.QueQuan} - {item.DonVi}: Trùng số hiệu {item.SoHieu}");
                        continue;
                    }
                    soHieuSuDung = item.SoHieu;
                }
                else
                {
                    do
                    {
                        soHieuSuDung = $"ID{chiSoSoHieu:D5}";
                        chiSoSoHieu++;
                    }
                    while (soHieuDaTonTai.Contains(soHieuSuDung) || soHieuTrongLanNap.Contains(soHieuSuDung));
                }
                soHieuTrongLanNap.Add(soHieuSuDung);
                pSTT.Value = stt++;
                pHoTen.Value = Module_BaoMatAES.MaHoa(item.HoVaTen);
                pSoHieu.Value = Module_BaoMatAES.MaHoa(soHieuSuDung);
                pNamSinh.Value = Module_BaoMatAES.MaHoa(item.NamSinh);
                pQueQuan.Value = Module_BaoMatAES.MaHoa(item.QueQuan);
                pNgayVao.Value = Module_BaoMatAES.MaHoa(item.NgayVaoCAND);
                pCapBac.Value = Module_BaoMatAES.MaHoa(item.CapBac);
                pChucVu.Value = Module_BaoMatAES.MaHoa(item.ChucVu);
                pDonVi.Value = Module_BaoMatAES.MaHoa(item.DonVi);
                pPhanLoai.Value = Module_BaoMatAES.MaHoa(item.PhanLoai);
                pGhiChu.Value = Module_BaoMatAES.MaHoa(item.GhiChu);
                cmd.ExecuteNonQuery();
                demThanhCong++;
            }
            tran.Commit();
            GhiNhatKyVaMoThuMuc(demThanhCong, excelPath, "Tân binh", isExport: false);
            return dongTrung; // Trả về danh sách lỗi trùng để UI hiển thị
        }
        // HÀM TIỆN ÍCH DÙNG CHUNG (Để code gọn hơn)
        private static void GhiNhatKyVaMoThuMuc(int soLuong, string path, string doiTuong, bool isExport)
        {
            string taiKhoan = string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM) ? "Không xác định" : Module_TaiKhoan.TenTaiKhoan_RAM;
            string hanhDong = isExport
                ? $"Xuất danh sách {soLuong} {doiTuong} theo đường dẫn {path}"
                : $"Nạp danh sách {soLuong} {doiTuong} từ file {Path.GetFileName(path)}";
            Module_NhatKy.GhiNhatKy(
                taiKhoan: taiKhoan,
                hanhDong: hanhDong,
                ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
            );
            if (isExport)
            {
                try
                {
                    // GỌI HÀM UX MỚI Ở ĐÂY
                    MoVaChonTepTrongExplorer(path);
                }
                catch
                {
                    MessageBox.Show("Không mở được thư mục chứa file.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
        public static List<string> NhapDanhSachExcelCBCSFastDataReader(string filePath, bool xoaDuLieuCu)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                    });
                    var dataTable = result.Tables["DSCBCS_PhanMemThiDua2026"];
                    if (dataTable == null) throw new Exception("Không tìm thấy Sheet 'DSCBCS_PhanMemThiDua2026' trong tệp Excel.");
                    List<CBCSModel> data = new List<CBCSModel>();
                    // 1. ĐỌC DỮ LIỆU TỪ DATATABLE VÀO MODEL (Dùng Index)
                    foreach (DataRow row in dataTable.Rows)
                    {
                        var cbcs = new CBCSModel
                        {
                            HoVaTen = row[1]?.ToString()?.Trim() ?? "",
                            SoHieu = row[2]?.ToString()?.Trim().Trim('\'') ?? "",
                            NamSinh = row[3]?.ToString()?.Trim().Trim('\'') ?? "",
                            QueQuan = row[4]?.ToString()?.Trim() ?? "",
                            NgayVaoCAND = row[5]?.ToString()?.Trim().Trim('\'') ?? "",
                            CapBac = row[6]?.ToString()?.Trim() ?? "",
                            ChucVu = row[7]?.ToString()?.Trim() ?? "",
                            DonVi = row[8]?.ToString()?.Trim() ?? "",
                            PhanLoai = row[9]?.ToString()?.Trim() ?? "",
                            GhiChu = row[10]?.ToString()?.Trim() ?? ""
                        };
                        if (cbcs.IsValid) data.Add(cbcs);
                    }
                    if (data.Count == 0) throw new Exception("Tệp Excel không có dữ liệu hợp lệ!");
                    // 2. KẾT NỐI DB VÀ XỬ LÝ LOGIC XÓA/NỐI TIẾP
                    using var conn = new SqliteConnection($"Data Source={Module_DanduongGPS.DuongDanCSDL2}");
                    conn.Open();
                    if (xoaDuLieuCu)
                    {
                        using var cmdDel = conn.CreateCommand();
                        cmdDel.CommandText = "DELETE FROM DanhSach";
                        cmdDel.ExecuteNonQuery();
                    }
                    // Tìm STT bắt đầu nếu là nạp nối tiếp
                    int stt = 1;
                    if (!xoaDuLieuCu)
                    {
                        using var cmdStt = conn.CreateCommand();
                        cmdStt.CommandText = "SELECT MAX(STT) FROM DanhSach";
                        var res = cmdStt.ExecuteScalar();
                        if (res != DBNull.Value && res != null)
                        {
                            stt = Convert.ToInt32(res) + 1;
                        }
                    }
                    // 3. TRANSACTION GHI SIÊU TỐC KÈM MÃ HÓA AES
                    using var tran = conn.BeginTransaction();
                    try
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.Transaction = tran;
                        cmd.CommandText = @"INSERT INTO DanhSach 
                                            (STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu)
                                            VALUES 
                                            ($STT, $HoVaTen, $SoHieu, $NamSinh, $QueQuan, $NgayVaoCAND, $CapBac, $ChucVu, $DonVi, $PhanLoai, $GhiChu)";
                        var pSTT = cmd.Parameters.Add("$STT", SqliteType.Integer);
                        var pHoTen = cmd.Parameters.Add("$HoVaTen", SqliteType.Text);
                        var pSoHieu = cmd.Parameters.Add("$SoHieu", SqliteType.Text);
                        var pNamSinh = cmd.Parameters.Add("$NamSinh", SqliteType.Text);
                        var pQueQuan = cmd.Parameters.Add("$QueQuan", SqliteType.Text);
                        var pNgayVao = cmd.Parameters.Add("$NgayVaoCAND", SqliteType.Text);
                        var pCapBac = cmd.Parameters.Add("$CapBac", SqliteType.Text);
                        var pChucVu = cmd.Parameters.Add("$ChucVu", SqliteType.Text);
                        var pDonVi = cmd.Parameters.Add("$DonVi", SqliteType.Text);
                        var pPhanLoai = cmd.Parameters.Add("$PhanLoai", SqliteType.Text);
                        var pGhiChu = cmd.Parameters.Add("$GhiChu", SqliteType.Text);
                        for (int i = 0; i < data.Count; i++)
                        {
                            var item = data[i];
                            pSTT.Value = stt++;
                            pHoTen.Value = Module_BaoMatAES.MaHoa(item.HoVaTen);
                            pSoHieu.Value = Module_BaoMatAES.MaHoa(item.SoHieu);
                            pNamSinh.Value = Module_BaoMatAES.MaHoa(item.NamSinh);
                            pQueQuan.Value = Module_BaoMatAES.MaHoa(item.QueQuan);
                            pNgayVao.Value = Module_BaoMatAES.MaHoa(item.NgayVaoCAND);
                            pCapBac.Value = Module_BaoMatAES.MaHoa(item.CapBac);
                            pChucVu.Value = Module_BaoMatAES.MaHoa(item.ChucVu);
                            pDonVi.Value = Module_BaoMatAES.MaHoa(item.DonVi);
                            pPhanLoai.Value = Module_BaoMatAES.MaHoa(item.PhanLoai);
                            pGhiChu.Value = Module_BaoMatAES.MaHoa(item.GhiChu);
                            cmd.ExecuteNonQuery();
                        }
                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                    return new List<string>(); // CBCS trả về list rỗng
                }
            }
        }
        // ĐỘNG CƠ FAST DATA READER DÀNH CHO TÂN BINH (TRÊN 1500 DÒNG)
        public static List<string> NhapDanhSachExcelTanBinhFastDataReader(string filePath, bool xoaDuLieuCu)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                    });
                    var dataTable = result.Tables["DSTanBinh_PhanMemThiDua2026"];
                    if (dataTable == null) throw new Exception("Không tìm thấy Sheet 'DSTanBinh_PhanMemThiDua2026' trong tệp Excel.");
                    List<CBCSModel> data = new List<CBCSModel>();
                    foreach (DataRow row in dataTable.Rows)
                    {
                        var cbcs = new CBCSModel
                        {
                            HoVaTen = row[1]?.ToString()?.Trim() ?? "",
                            SoHieu = row[2]?.ToString()?.Trim().Trim('\'') ?? "",
                            NamSinh = row[3]?.ToString()?.Trim().Trim('\'') ?? "",
                            QueQuan = row[4]?.ToString()?.Trim() ?? "",
                            NgayVaoCAND = row[5]?.ToString()?.Trim().Trim('\'') ?? "",
                            CapBac = row[6]?.ToString()?.Trim() ?? "",
                            ChucVu = row[7]?.ToString()?.Trim() ?? "",
                            DonVi = row[8]?.ToString()?.Trim() ?? "",
                            PhanLoai = row[9]?.ToString()?.Trim() ?? "",
                            GhiChu = row[10]?.ToString()?.Trim() ?? ""
                        };
                        if (cbcs.IsValid) data.Add(cbcs);
                    }
                    if (data.Count == 0) throw new Exception("Tệp Excel không có dữ liệu hợp lệ!");
                    using var conn = new SqliteConnection($"Data Source={Module_DanduongGPS.DuongDanCSDL2}");
                    conn.Open();
                    if (xoaDuLieuCu)
                    {
                        using var cmdDel = conn.CreateCommand();
                        cmdDel.CommandText = "DELETE FROM DanhSach";
                        cmdDel.ExecuteNonQuery();
                    }
                    HashSet<string> soHieuDaTonTai = new HashSet<string>();
                    List<string> dongTrung = new List<string>();
                    // Nạp danh sách số hiệu cũ để check trùng (nếu nạp nối tiếp)
                    using (var cmdCheck = conn.CreateCommand())
                    {
                        cmdCheck.CommandText = "SELECT SoHieu FROM DanhSach WHERE SoHieu IS NOT NULL AND SoHieu <> ''";
                        using var rd = cmdCheck.ExecuteReader();
                        while (rd.Read())
                        {
                            string sh = Module_BaoMatAES.GiaiMa(rd.GetString(0));
                            if (!string.IsNullOrWhiteSpace(sh)) soHieuDaTonTai.Add(sh);
                        }
                    }
                    int chiSoSoHieu = 1;
                    int stt = 1;
                    // Lấy STT hiện tại để nối tiếp
                    if (!xoaDuLieuCu)
                    {
                        using var cmdStt = conn.CreateCommand();
                        cmdStt.CommandText = "SELECT MAX(STT) FROM DanhSach";
                        var res = cmdStt.ExecuteScalar();
                        if (res != DBNull.Value && res != null)
                        {
                            stt = Convert.ToInt32(res) + 1;
                        }
                    }
                    HashSet<string> soHieuTrongLanNap = new HashSet<string>();
                    using var tran = conn.BeginTransaction();
                    try
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.Transaction = tran;
                        cmd.CommandText = @"INSERT INTO DanhSach 
                                            (STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu)
                                            VALUES 
                                            ($STT, $HoVaTen, $SoHieu, $NamSinh, $QueQuan, $NgayVaoCAND, $CapBac, $ChucVu, $DonVi, $PhanLoai, $GhiChu)";
                        var pSTT = cmd.Parameters.Add("$STT", SqliteType.Integer);
                        var pHoTen = cmd.Parameters.Add("$HoVaTen", SqliteType.Text);
                        var pSoHieu = cmd.Parameters.Add("$SoHieu", SqliteType.Text);
                        var pNamSinh = cmd.Parameters.Add("$NamSinh", SqliteType.Text);
                        var pQueQuan = cmd.Parameters.Add("$QueQuan", SqliteType.Text);
                        var pNgayVao = cmd.Parameters.Add("$NgayVaoCAND", SqliteType.Text);
                        var pCapBac = cmd.Parameters.Add("$CapBac", SqliteType.Text);
                        var pChucVu = cmd.Parameters.Add("$ChucVu", SqliteType.Text);
                        var pDonVi = cmd.Parameters.Add("$DonVi", SqliteType.Text);
                        var pPhanLoai = cmd.Parameters.Add("$PhanLoai", SqliteType.Text);
                        var pGhiChu = cmd.Parameters.Add("$GhiChu", SqliteType.Text);
                        for (int i = 0; i < data.Count; i++)
                        {
                            var item = data[i];
                            string soHieuSuDung = "";
                            if (!string.IsNullOrWhiteSpace(item.SoHieu) && item.SoHieu.StartsWith("ID"))
                            {
                                if (soHieuDaTonTai.Contains(item.SoHieu) || soHieuTrongLanNap.Contains(item.SoHieu))
                                {
                                    dongTrung.Add($"{i + 2}. {item.HoVaTen} - {item.DonVi}: Trùng số hiệu {item.SoHieu}");
                                    continue;
                                }
                                soHieuSuDung = item.SoHieu;
                            }
                            else
                            {
                                do
                                {
                                    soHieuSuDung = $"ID{chiSoSoHieu:D5}";
                                    chiSoSoHieu++;
                                }
                                while (soHieuDaTonTai.Contains(soHieuSuDung) || soHieuTrongLanNap.Contains(soHieuSuDung));
                            }
                            soHieuTrongLanNap.Add(soHieuSuDung);
                            pSTT.Value = stt++;
                            pHoTen.Value = Module_BaoMatAES.MaHoa(item.HoVaTen);
                            pSoHieu.Value = Module_BaoMatAES.MaHoa(soHieuSuDung);
                            pNamSinh.Value = Module_BaoMatAES.MaHoa(item.NamSinh);
                            pQueQuan.Value = Module_BaoMatAES.MaHoa(item.QueQuan);
                            pNgayVao.Value = Module_BaoMatAES.MaHoa(item.NgayVaoCAND);
                            pCapBac.Value = Module_BaoMatAES.MaHoa(item.CapBac);
                            pChucVu.Value = Module_BaoMatAES.MaHoa(item.ChucVu);
                            pDonVi.Value = Module_BaoMatAES.MaHoa(item.DonVi);
                            pPhanLoai.Value = Module_BaoMatAES.MaHoa(item.PhanLoai);
                            pGhiChu.Value = Module_BaoMatAES.MaHoa(item.GhiChu);
                            cmd.ExecuteNonQuery();
                        }
                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                    return dongTrung; // Trả về lỗi trùng để UI hiển thị
                }
            }
        }
        //yêu yêu yêu mèo cam
        public static async Task KiemTraVaNapDuLieuTapTheAsync(string filePath, string csdl4Path)
        {
            try
            {
                using var wb = new XLWorkbook(filePath);
                // 1. Nếu không có sheet Tập thể -> Tự động thoát, không hiện MessageBox
                if (!wb.Worksheets.Contains("ThongKe_PhanLoaiTapThe"))
                {
                    return;
                }
                var ws = wb.Worksheet("ThongKe_PhanLoaiTapThe");
                var firstRow = ws.FirstRowUsed();
                int lastRowIndex = ws.LastRowUsed()?.RowNumber() ?? 0;
                if (firstRow == null || lastRowIndex <= 1) return;
                // 2. Chỉ hiện MessageBox khi tệp thực sự có sheet và có dữ liệu
                var dialogResult = MessageBox.Show(
                    "Phát hiện dữ liệu 'Thống kê phân loại tập thể' trong file Excel.\n\nBạn có muốn nạp dữ liệu thi đua tập thể này vào hệ thống không?",
                    "Xác nhận nạp dữ liệu Tập thể",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );
                if (dialogResult != DialogResult.Yes) return;
                // 3. Đọc dữ liệu và nạp vào csdl4.db
                List<string> colNames = new List<string>();
                foreach (var cell in firstRow.CellsUsed())
                {
                    colNames.Add(cell.Value.ToString().Trim());
                }
                await Task.Run(() =>
                {
                    using var cn = new SqliteConnection($"Data Source={csdl4Path}");
                    cn.Open();
                    using var transaction = cn.BeginTransaction();
                    try
                    {
                        string columnsJoined = string.Join(", ", colNames);
                        string paramsJoined = string.Join(", ", colNames.Select(c => $"@{c}"));
                        string sqlInsert = $"INSERT OR REPLACE INTO ThongKe_PhanLoaiTapThe ({columnsJoined}) VALUES ({paramsJoined})";
                        for (int r = 2; r <= lastRowIndex; r++)
                        {
                            var row = ws.Row(r);
                            using var cmd = new SqliteCommand(sqlInsert, cn, transaction);
                            for (int c = 0; c < colNames.Count; c++)
                            {
                                string cellValue = row.Cell(c + 1).Value.ToString().Trim();
                                string colName = colNames[c];
                                if (string.IsNullOrEmpty(cellValue))
                                {
                                    cmd.Parameters.AddWithValue($"@{colName}", DBNull.Value);
                                }
                                else
                                {
                                    // 🔒 MÃ HÓA DỮ LIỆU BẰNG AES TRƯỚC KHI LƯU VÀO CSDL
                                    // (Nếu cột ID là số tự tăng hoặc khóa chính dạng số thì không mã hóa ID)
                                    if (colName.Equals("ID", StringComparison.OrdinalIgnoreCase))
                                    {
                                        cmd.Parameters.AddWithValue($"@{colName}", cellValue);
                                    }
                                    else
                                    {
                                        cmd.Parameters.AddWithValue($"@{colName}", Module_BaoMatAES.MaHoa(cellValue));
                                    }
                                }
                            }
                            cmd.ExecuteNonQuery();
                        }
                        transaction.Commit();
                        MessageBox.Show("Nạp dữ liệu thi đua tập thể thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show("Lỗi khi nạp dữ liệu tập thể vào CSDL:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi đọc sheet tập thể: " + ex.Message);
            }
        }
    }
}