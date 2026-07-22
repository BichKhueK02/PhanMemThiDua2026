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
                            HoVaTen = BaoMatAES.GiaiMa(reader.GetString(1)),
                            SoHieu = BaoMatAES.GiaiMa(reader.GetString(2)),
                            NamSinh = BaoMatAES.GiaiMa(reader.GetString(3)),
                            QueQuan = reader.IsDBNull(4) ? "" : BaoMatAES.GiaiMa(reader.GetString(4)),
                            NgayVaoCAND = reader.IsDBNull(5) ? "" : BaoMatAES.GiaiMa(reader.GetString(5)),
                            CapBac = reader.IsDBNull(6) ? "" : BaoMatAES.GiaiMa(reader.GetString(6)),
                            ChucVu = reader.IsDBNull(7) ? "" : BaoMatAES.GiaiMa(reader.GetString(7)),
                            DonVi = reader.IsDBNull(8) ? "" : BaoMatAES.GiaiMa(reader.GetString(8)),
                            PhanLoai = reader.IsDBNull(9) ? "" : BaoMatAES.GiaiMa(reader.GetString(9)),
                            GhiChu = reader.IsDBNull(10) ? "" : BaoMatAES.GiaiMa(reader.GetString(10))
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

                    wsEmpty.Style.Font.FontName = "Times New Roman";
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

                var fullRange = ws.Range(1, 1, danhSach.Count + 1, 11);
                fullRange.Style.Font.FontName = "Times New Roman";
                fullRange.Style.Font.FontSize = 13;
                fullRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                fullRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                ws.Row(1).Style.Font.Bold = true;
                ws.Row(1).Style.Fill.BackgroundColor = XLColor.LightBlue;
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
                                    HoVaTen = BaoMatAES.GiaiMa(reader.GetString(1)),
                                    SoHieu = BaoMatAES.GiaiMa(reader.GetString(2)),
                                    NamSinh = BaoMatAES.GiaiMa(reader.GetString(3)),
                                    QueQuan = reader.IsDBNull(4) ? "" : BaoMatAES.GiaiMa(reader.GetString(4)),
                                    NgayVaoCAND = reader.IsDBNull(5) ? "" : BaoMatAES.GiaiMa(reader.GetString(5)),
                                    CapBac = reader.IsDBNull(6) ? "" : BaoMatAES.GiaiMa(reader.GetString(6)),
                                    ChucVu = reader.IsDBNull(7) ? "" : BaoMatAES.GiaiMa(reader.GetString(7)),
                                    DonVi = reader.IsDBNull(8) ? "" : BaoMatAES.GiaiMa(reader.GetString(8)),
                                    PhanLoai = reader.IsDBNull(9) ? "" : BaoMatAES.GiaiMa(reader.GetString(9)),
                                    GhiChu = reader.IsDBNull(10) ? "" : BaoMatAES.GiaiMa(reader.GetString(10))
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

                    wsEmpty.Style.Font.FontName = "Times New Roman";
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
                ws.Style.Font.FontName = "Times New Roman";
                ws.Style.Font.FontSize = 14;

                string[] headers = { "STT", "Họ và tên", "Số hiệu", "Năm sinh", "Quê quán", "Ngày vào CAND", "Cấp bậc", "Chức vụ", "Đơn vị", "Phân loại", "Ghi chú" };

                for (int c = 0; c < headers.Length; c++)
                {
                    var cell = ws.Cell(1, c + 1);
                    cell.Value = headers[c];
                    cell.Style.Font.Bold = true;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.Black;
                }

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

                    for (int c = 1; c <= 11; c++)
                    {
                        var cell = ws.Cell(rowIdx, c);
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    }
                }

                ws.Columns().AdjustToContents();
                ws.Rows().AdjustToContents();
                wb.SaveAs(filePath);

                // Chỗ này giả định đồng chí có hàm GhiNhatKyVaMoThuMuc riêng biệt
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
                danhSachMau.Add(new object[] { 1, "Nguyễn Văn A (Mẫu)", "(Bỏ trống)", 1996, "Hòa Thuận, An Giang", "'02/" + DateTime.Now.Year, "B2", "CS", "C1, DHL1", "Loại 1", "Đảng viên DB" });
                danhSachMau.Add(new object[] { 2, "Nguyễn Văn B (Mẫu)", "(Bỏ trống)", 1996, "Hòa Thuận, An Giang", "'02/" + DateTime.Now.Year, "B2", "CS", "C2, DHL1", "Loại 2", "Đảng viên DB" });
                danhSachMau.Add(new object[] { 3, "Nguyễn Văn C (Mẫu)", "(Bỏ trống)", 1996, "Hòa Thuận, An Giang", "'02/" + DateTime.Now.Year, "B2", "CS", "C3, DHL1", "Loại 3", "" });
            }
            else
            {
                danhSachMau.Add(new object[] { 1, "Nguyễn Văn A (Mẫu)", "'123456", 1996, "Hòa Thuận, An Giang", "'02/2016", "U2", "Cán bộ", "TTM,D2", "Loại 1", "Đảng viên DB" });
                danhSachMau.Add(new object[] { 2, "Nguyễn Văn B (Mẫu)", "'123789", 1997, "Hòa Thuận, An Giang", "'02/2017", "U2", "Cán bộ", "TCT,D2", "Loại 2", "Rớt CAK" });
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
            dataRange.Style.Font.FontName = "Times New Roman";
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
            wsEmpty.Style.Font.FontName = "Times New Roman";
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
                pHoTen.Value = BaoMatAES.MaHoa(item.HoVaTen);
                pSoHieu.Value = BaoMatAES.MaHoa(item.SoHieu);
                pNamSinh.Value = BaoMatAES.MaHoa(item.NamSinh);
                pQueQuan.Value = BaoMatAES.MaHoa(item.QueQuan);
                pNgayVao.Value = BaoMatAES.MaHoa(item.NgayVaoCAND);
                pCapBac.Value = BaoMatAES.MaHoa(item.CapBac);
                pChucVu.Value = BaoMatAES.MaHoa(item.ChucVu);
                pDonVi.Value = BaoMatAES.MaHoa(item.DonVi);
                pPhanLoai.Value = BaoMatAES.MaHoa(item.PhanLoai);
                pGhiChu.Value = BaoMatAES.MaHoa(item.GhiChu);

                cmd.ExecuteNonQuery();
            }

            tran.Commit();
            GhiNhatKyVaMoThuMuc(data.Count, excelPath, "CBCS", isExport: false);

            return new List<string>(); // CBCS không có logic trùng lặp phức tạp nên trả về rỗng
        }
        //====================================================
        // HÀM 4: NHẬP EXCEL (TÂN BINH) - ĐÃ LỌC SẠCH MESSAGEBOX
        //====================================================
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
                    string sh = BaoMatAES.GiaiMa(rd.GetString(0));
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
                pHoTen.Value = BaoMatAES.MaHoa(item.HoVaTen);
                pSoHieu.Value = BaoMatAES.MaHoa(soHieuSuDung);
                pNamSinh.Value = BaoMatAES.MaHoa(item.NamSinh);
                pQueQuan.Value = BaoMatAES.MaHoa(item.QueQuan);
                pNgayVao.Value = BaoMatAES.MaHoa(item.NgayVaoCAND);
                pCapBac.Value = BaoMatAES.MaHoa(item.CapBac);
                pChucVu.Value = BaoMatAES.MaHoa(item.ChucVu);
                pDonVi.Value = BaoMatAES.MaHoa(item.DonVi);
                pPhanLoai.Value = BaoMatAES.MaHoa(item.PhanLoai);
                pGhiChu.Value = BaoMatAES.MaHoa(item.GhiChu);

                cmd.ExecuteNonQuery();
                demThanhCong++;
            }

            tran.Commit();
            GhiNhatKyVaMoThuMuc(demThanhCong, excelPath, "Tân binh", isExport: false);

            return dongTrung; // Trả về danh sách lỗi trùng để UI hiển thị
        }
        //====================================================
        // HÀM TIỆN ÍCH DÙNG CHUNG (Để code gọn hơn)
        //====================================================
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
                            pHoTen.Value = BaoMatAES.MaHoa(item.HoVaTen);
                            pSoHieu.Value = BaoMatAES.MaHoa(item.SoHieu);
                            pNamSinh.Value = BaoMatAES.MaHoa(item.NamSinh);
                            pQueQuan.Value = BaoMatAES.MaHoa(item.QueQuan);
                            pNgayVao.Value = BaoMatAES.MaHoa(item.NgayVaoCAND);
                            pCapBac.Value = BaoMatAES.MaHoa(item.CapBac);
                            pChucVu.Value = BaoMatAES.MaHoa(item.ChucVu);
                            pDonVi.Value = BaoMatAES.MaHoa(item.DonVi);
                            pPhanLoai.Value = BaoMatAES.MaHoa(item.PhanLoai);
                            pGhiChu.Value = BaoMatAES.MaHoa(item.GhiChu);

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
                            string sh = BaoMatAES.GiaiMa(rd.GetString(0));
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
                            pHoTen.Value = BaoMatAES.MaHoa(item.HoVaTen);
                            pSoHieu.Value = BaoMatAES.MaHoa(soHieuSuDung);
                            pNamSinh.Value = BaoMatAES.MaHoa(item.NamSinh);
                            pQueQuan.Value = BaoMatAES.MaHoa(item.QueQuan);
                            pNgayVao.Value = BaoMatAES.MaHoa(item.NgayVaoCAND);
                            pCapBac.Value = BaoMatAES.MaHoa(item.CapBac);
                            pChucVu.Value = BaoMatAES.MaHoa(item.ChucVu);
                            pDonVi.Value = BaoMatAES.MaHoa(item.DonVi);
                            pPhanLoai.Value = BaoMatAES.MaHoa(item.PhanLoai);
                            pGhiChu.Value = BaoMatAES.MaHoa(item.GhiChu);

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
    }
}