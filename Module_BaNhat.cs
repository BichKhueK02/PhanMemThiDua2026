using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.Data.Sqlite;
using PhanMemThiDua2026;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
public class CbcDTO
{
    public int STT { get; set; }
    public string HoVaTen { get; set; }
    public string SoHieu { get; set; }
    public string NamSinh { get; set; }
    public string QueQuan { get; set; }
    public string NgayVaoCAND { get; set; }
    public string CapBac { get; set; }
    public string ChucVu { get; set; }
    public string DonVi { get; set; }
    public string PhanLoai { get; set; }
    public string ThanhTich { get; set; }
    public string GhiChu { get; set; }
}
public static class Module_BaNhat
{
    public const int GioiHanToiDa = 3000;
    [DllImport("shell32.dll", ExactSpelling = true)]
    private static extern void ILFree(IntPtr pidlList);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern IntPtr ILCreateFromPathW(string pszPath);
    [DllImport("shell32.dll")]
    private static extern int SHOpenFolderAndSelectItems(
        IntPtr pidlFolder,
        uint cidl,
        IntPtr apidl,
        int dwFlags);
    // Lưu ý: Vì Module_BaNhat là static class, nên hàm này cũng phải là "public static"


    // ==============================================================================
    // HÀM NHẬP DỮ LIỆU (Đã tái cấu trúc tương thích tuyệt đối & Bắt lỗi cục bộ)
    // ==============================================================================
    public static void XuatDuLieuVaoBangQuanLyBaNhat(ClosedXML.Excel.XLWorkbook package, string dbPath, bool epXuatFileMau)
    {
        try
        {
            // 1. Kiểm tra và xử lý tạo mới hoặc dọn dẹp Sheet cũ
            ClosedXML.Excel.IXLWorksheet wsBN;
            if (package.Worksheets.Contains("DS_BaNhat"))
            {
                wsBN = package.Worksheet("DS_BaNhat");
                wsBN.Clear(); // Xóa sạch nội dung cũ nếu có để ghi đè chuẩn xác
            }
            else
            {
                wsBN = package.Worksheets.Add("DS_BaNhat");
            }

            // Cấu hình phông nền và màu sắc nhận diện hệ thống
            wsBN.TabColor = ClosedXML.Excel.XLColor.Gold;
            wsBN.Style.Font.SetFontName("Times New Roman");
            wsBN.Style.Font.SetFontSize(12);

            // 2. Thiết lập tiêu đề dòng (Header) cho bảng
            wsBN.Range("A1:M1").Merge().Value = "DANH SÁCH CÁN BỘ CHIẾN SĨ ĐỀ NGHỊ BIỂU DƯƠNG PHONG TRÀO \"BA NHẤT\"";
            wsBN.Cell("A1").Style.Font.SetBold(true).Font.SetFontSize(14).Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);
            wsBN.Row(1).Height = 30;

            // ⭐ ĐÃ SỬA: Khớp chuẩn 13 cột với Database (Bỏ cột ID vì chỉ dùng ngầm)
            string[] headers = { "STT", "Họ và tên", "Số hiệu", "Năm sinh", "Quê quán", "Ngày vào CAND", "Cấp bậc", "Chức vụ", "Đơn vị", "Phân loại", "Ghi chú", "Đề nghị", "Thành tích" };
            wsBN.Row(3).Height = 25;
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = wsBN.Cell(3, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                cell.Style.Alignment.WrapText = true;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#EBF1F5");
                cell.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
            }

            // ⭐ ĐÃ SỬA: Thiết lập độ rộng chuẩn cho 13 cột
            double[] widths = { 6, 25, 12, 10, 25, 12, 14, 15, 15, 12, 20, 10, 35 };
            for (int i = 0; i < widths.Length; i++) wsBN.Column(i + 1).Width = widths[i];

            // 3. Đọc dữ liệu từ SQLite & Giải mã lọc điều kiện (Chỉ chạy khi không phải file mẫu)
            if (!epXuatFileMau && File.Exists(dbPath))
            {
                var listXuat = new List<Dictionary<string, string>>();

                // Hàm giải mã nội bộ bảo hiểm zero-crash tuyệt đối cho vòng lặp
                string ThucThiGiaiMa(object? val)
                {
                    if (val == null || val == DBNull.Value) return string.Empty;
                    string s = val.ToString()?.Trim() ?? string.Empty;
                    if (string.IsNullOrEmpty(s)) return string.Empty;
                    try { return BaoMatAES.GiaiMa(s)?.Trim() ?? string.Empty; } catch { return s; }
                }

                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();

                    // ⭐ ĐÃ SỬA: Gọi đúng thứ tự cột khớp với mảng Header phía trên
                    cmd.CommandText = "SELECT STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu, DeNghi, ThanhTich FROM DanhSachBaNhat ORDER BY STT ASC";

                    using var rd = cmd.ExecuteReader();
                    int sttDem = 1;
                    while (rd.Read())
                    {
                        string deNghiDecrypted = ThucThiGiaiMa(rd["DeNghi"]);
                        if (!deNghiDecrypted.Equals("X", StringComparison.OrdinalIgnoreCase))
                            continue;

                        // Dùng Dictionary để linh động không phụ thuộc hoàn toàn vào Class DTO nếu sau này có đổi cột
                        var rowData = new Dictionary<string, string>
                        {
                            { "STT", (sttDem++).ToString() },
                            { "HoVaTen", ThucThiGiaiMa(rd["HoVaTen"]) },
                            { "SoHieu", ThucThiGiaiMa(rd["SoHieu"]) },
                            { "NamSinh", ThucThiGiaiMa(rd["NamSinh"]) },
                            { "QueQuan", ThucThiGiaiMa(rd["QueQuan"]) },
                            { "NgayVaoCAND", ThucThiGiaiMa(rd["NgayVaoCAND"]) },
                            { "CapBac", ThucThiGiaiMa(rd["CapBac"]) },
                            { "ChucVu", ThucThiGiaiMa(rd["ChucVu"]) },
                            { "DonVi", ThucThiGiaiMa(rd["DonVi"]) },
                            { "PhanLoai", ThucThiGiaiMa(rd["PhanLoai"]) },
                            { "GhiChu", ThucThiGiaiMa(rd["GhiChu"]) },
                            { "DeNghi", deNghiDecrypted },
                            { "ThanhTich", ThucThiGiaiMa(rd["ThanhTich"]) }
                        };
                        listXuat.Add(rowData);
                    }
                }

                // 4. Đổ dữ liệu hàng loạt lên trang tính (Tốc độ cao)
                int dataRowIndex = 4;
                foreach (var item in listXuat)
                {
                    wsBN.Cell(dataRowIndex, 1).Value = item["STT"];
                    wsBN.Cell(dataRowIndex, 2).Value = item["HoVaTen"];
                    wsBN.Cell(dataRowIndex, 3).Value = item["SoHieu"];
                    wsBN.Cell(dataRowIndex, 4).Value = item["NamSinh"];
                    wsBN.Cell(dataRowIndex, 5).Value = item["QueQuan"];
                    wsBN.Cell(dataRowIndex, 6).Value = item["NgayVaoCAND"];
                    wsBN.Cell(dataRowIndex, 7).Value = item["CapBac"];
                    wsBN.Cell(dataRowIndex, 8).Value = item["ChucVu"];
                    wsBN.Cell(dataRowIndex, 9).Value = item["DonVi"];
                    wsBN.Cell(dataRowIndex, 10).Value = item["PhanLoai"];
                    wsBN.Cell(dataRowIndex, 11).Value = item["GhiChu"];
                    wsBN.Cell(dataRowIndex, 12).Value = item["DeNghi"];
                    wsBN.Cell(dataRowIndex, 13).Value = item["ThanhTich"];

                    // Kẻ viền khung nét mảnh và căn lề từng dòng dữ liệu (Tới cột 13)
                    var rngRow = wsBN.Range(dataRowIndex, 1, dataRowIndex, 13);
                    rngRow.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                    rngRow.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                    rngRow.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                    rngRow.Style.Alignment.SetWrapText(true);

                    // Căn lề riêng biệt cho từng loại dữ liệu
                    wsBN.Cell(dataRowIndex, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center; // STT
                    wsBN.Cell(dataRowIndex, 2).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;   // Họ tên
                    wsBN.Cell(dataRowIndex, 3).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center; // Số hiệu
                    wsBN.Cell(dataRowIndex, 4).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center; // Năm sinh
                    wsBN.Cell(dataRowIndex, 5).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;   // Quê quán
                    wsBN.Cell(dataRowIndex, 6).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center; // Vào CAND
                    wsBN.Cell(dataRowIndex, 7).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center; // Cấp bậc
                    wsBN.Cell(dataRowIndex, 8).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center; // Chức vụ
                    wsBN.Cell(dataRowIndex, 9).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center; // Đơn vị
                    wsBN.Cell(dataRowIndex, 10).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;// Phân loại
                    wsBN.Cell(dataRowIndex, 11).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;  // Ghi chú
                    wsBN.Cell(dataRowIndex, 12).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;// Đề nghị
                    wsBN.Cell(dataRowIndex, 13).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;  // Thành tích

                    wsBN.Row(dataRowIndex).Height = 24;
                    dataRowIndex++;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Lỗi thực thi tích hợp tại Sheet DS_BaNhat: " + ex.Message);
        }
    }
    public static void NhapDuLieuVaoBangQuanLyBaNhat(string excelPath, string dbPath)
    {
        try
        {
            if (!File.Exists(excelPath) || !File.Exists(dbPath)) return;

            var danhSachBoQua = new List<string>();

            using (var wb = new ClosedXML.Excel.XLWorkbook(excelPath))
            {
                if (!wb.Worksheets.TryGetWorksheet("DS_BaNhat", out var ws)) return;

                // KIỂM TRA HEADER
                string[] mauHeaderChuan = { "STT", "Họ và tên", "Số hiệu", "Năm sinh", "Quê quán", "Ngày vào CAND", "Cấp bậc", "Chức vụ", "Đơn vị", "Phân loại", "Ghi chú", "Đề nghị", "Thành tích" };
                for (int i = 0; i < mauHeaderChuan.Length; i++)
                {
                    string headerExcel = ws.Cell(3, i + 1).GetString().Trim();
                    if (!headerExcel.Equals(mauHeaderChuan[i], StringComparison.OrdinalIgnoreCase))
                    {
                        System.Windows.Forms.MessageBox.Show(
                            $"Cấu trúc tệp Excel không hợp lệ!\nCột số {i + 1} đang là '{headerExcel}', yêu cầu chuẩn phải là '{mauHeaderChuan[i]}'.\nVui lòng không tự ý chèn, xóa hoặc đổi tên cột mẫu.",
                            "Lỗi cấu trúc tệp nhập", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                        return;
                    }
                }

                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                if (lastRow < 4) return;

                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
                {
                    conn.Open();

                    int nextSTT = 0;
                    using (var cmdMax = conn.CreateCommand())
                    {
                        cmdMax.CommandText = "SELECT IFNULL(MAX(STT), 0) FROM DanhSachBaNhat";
                        nextSTT = Convert.ToInt32(cmdMax.ExecuteScalar()) + 1;
                    }

                    using var transaction = conn.BeginTransaction();

                    using var cmdCheck = conn.CreateCommand();
                    cmdCheck.Transaction = transaction;
                    cmdCheck.CommandText = "SELECT EXISTS(SELECT 1 FROM DanhSachBaNhat WHERE SoHieu = @sh)";
                    var paramShCheck = cmdCheck.Parameters.Add("@sh", Microsoft.Data.Sqlite.SqliteType.Text);

                    // ⭐ ĐÃ SỬA: Loại bỏ hoàn toàn DeNghi và ThanhTich khỏi lệnh UPDATE
                    using var cmdUpdate = conn.CreateCommand();
                    cmdUpdate.Transaction = transaction;
                    cmdUpdate.CommandText = @"UPDATE DanhSachBaNhat SET 
                HoVaTen = @ht, NamSinh = @ns, QueQuan = @qq, NgayVaoCAND = @nv, 
                CapBac = @cb, ChucVu = @cv, DonVi = @dv, PhanLoai = @pl, 
                GhiChu = @gc 
                WHERE SoHieu = @sh";
                    PrepareParameters(cmdUpdate);

                    using var cmdInsert = conn.CreateCommand();
                    cmdInsert.Transaction = transaction;
                    cmdInsert.CommandText = @"INSERT INTO DanhSachBaNhat 
                (STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu, DeNghi, ThanhTich) 
                VALUES (@stt, @ht, @sh, @ns, @qq, @nv, @cb, @cv, @dv, @pl, @gc, @dn, @tt)";
                    var paramSttInsert = cmdInsert.Parameters.Add("@stt", Microsoft.Data.Sqlite.SqliteType.Integer);
                    PrepareParameters(cmdInsert);

                    for (int r = 4; r <= lastRow; r++)
                    {
                        string hoTen = ws.Cell(r, 2).GetString().Trim();
                        if (string.IsNullOrWhiteSpace(hoTen)) continue;

                        try
                        {
                            string soHieu = ws.Cell(r, 3).GetString().Trim();
                            string deNghi = ws.Cell(r, 12).GetString().Trim();

                            if (string.IsNullOrWhiteSpace(soHieu))
                            {
                                danhSachBoQua.Add($"- Đồng chí: {hoTen} dòng {r} không thể nạp vì thiếu Số hiệu quân số.");
                                continue;
                            }

                            if (!deNghi.Equals("X", StringComparison.OrdinalIgnoreCase))
                            {
                                danhSachBoQua.Add($"- Đồng chí: {hoTen} (Số hiệu: {soHieu}) bỏ qua do không có dấu X Đề nghị.");
                                continue;
                            }

                            string namSinh = ws.Cell(r, 4).GetString().Trim();
                            string queQuan = ws.Cell(r, 5).GetString().Trim();
                            string ngayVao = ws.Cell(r, 6).GetString().Trim();
                            string capBac = ws.Cell(r, 7).GetString().Trim();
                            string chucVu = ws.Cell(r, 8).GetString().Trim();
                            string donVi = ws.Cell(r, 9).GetString().Trim();
                            string phanLoai = ws.Cell(r, 10).GetString().Trim();
                            string ghiChu = ws.Cell(r, 11).GetString().Trim();
                            string thanhTich = ws.Cell(r, 13).GetString().Trim();

                            string shMaHoa = BaoMatAES.MaHoa(soHieu);
                            paramShCheck.Value = shMaHoa;
                            bool daTonTai = Convert.ToInt64(cmdCheck.ExecuteScalar()) > 0;

                            if (daTonTai)
                            {
                                BindParameterValues(cmdUpdate, shMaHoa, hoTen, namSinh, queQuan, ngayVao, capBac, chucVu, donVi, phanLoai, ghiChu, deNghi, thanhTich);
                                // ⭐ ĐÃ XÓA BỎ 2 lệnh gán param @dnTrong và @ttTrong dư thừa
                                cmdUpdate.ExecuteNonQuery();
                            }
                            else
                            {
                                paramSttInsert.Value = nextSTT;
                                BindParameterValues(cmdInsert, shMaHoa, hoTen, namSinh, queQuan, ngayVao, capBac, chucVu, donVi, phanLoai, ghiChu, deNghi, thanhTich);
                                cmdInsert.ExecuteNonQuery();
                                nextSTT++;
                            }
                        }
                        catch (Exception ex)
                        {
                            danhSachBoQua.Add($"- Đồng chí: {hoTen} (Dòng {r}) - Lỗi kỹ thuật: {ex.Message}");
                            continue;
                        }
                    }
                    transaction.Commit();
                }
            }

            if (danhSachBoQua.Count > 0)
            {
                string msgCanhBao = string.Join("\n", danhSachBoQua.Take(15));
                if (danhSachBoQua.Count > 15) msgCanhBao += $"\n... và {danhSachBoQua.Count - 15} trường hợp khác.";

                var activeForms = System.Windows.Forms.Application.OpenForms;
                if (activeForms.Count > 0)
                {
                    activeForms[0].Invoke(new Action(() =>
                    {
                        System.Windows.Forms.MessageBox.Show(activeForms[0], msgCanhBao, "Thông báo nạp Sổ Vàng", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                    }));
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[Lỗi NhapDataSoVang] " + ex.Message);
        }
    }
    //public static void NhapDuLieuVaoBangQuanLyBaNhat(string excelPath, string dbPath)
    //{
    //    try
    //    {
    //        if (!File.Exists(excelPath) || !File.Exists(dbPath)) return;

    //        var danhSachBoQua = new List<string>();

    //        using (var wb = new ClosedXML.Excel.XLWorkbook(excelPath))
    //        {
    //            if (!wb.Worksheets.TryGetWorksheet("DS_BaNhat", out var ws)) return;

    //            // KIỂM TRA HEADER: Khóa cứng cấu trúc dòng 3 tương thích với hàm Xuất
    //            string[] mauHeaderChuan = { "STT", "Họ và tên", "Số hiệu", "Năm sinh", "Quê quán", "Ngày vào CAND", "Cấp bậc", "Chức vụ", "Đơn vị", "Phân loại", "Ghi chú", "Đề nghị", "Thành tích" };
    //            for (int i = 0; i < mauHeaderChuan.Length; i++)
    //            {
    //                string headerExcel = ws.Cell(3, i + 1).GetString().Trim();
    //                if (!headerExcel.Equals(mauHeaderChuan[i], StringComparison.OrdinalIgnoreCase))
    //                {
    //                    System.Windows.Forms.MessageBox.Show(
    //                        $"Cấu trúc tệp Excel không hợp lệ!\nCột số {i + 1} đang là '{headerExcel}', yêu cầu chuẩn phải là '{mauHeaderChuan[i]}'.\nVui lòng không tự ý chèn, xóa hoặc đổi tên cột mẫu.",
    //                        "Lỗi cấu trúc tệp nhập", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
    //                    return;
    //                }
    //            }

    //            int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
    //            if (lastRow < 4) return;

    //            using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
    //            {
    //                conn.Open();

    //                int nextSTT = 0;
    //                using (var cmdMax = conn.CreateCommand())
    //                {
    //                    cmdMax.CommandText = "SELECT IFNULL(MAX(STT), 0) FROM DanhSachBaNhat";
    //                    nextSTT = Convert.ToInt32(cmdMax.ExecuteScalar()) + 1;
    //                }

    //                // Khởi tạo Transaction bảo vệ dữ liệu toàn vẹn
    //                using var transaction = conn.BeginTransaction();

    //                using var cmdCheck = conn.CreateCommand();
    //                cmdCheck.Transaction = transaction;
    //                cmdCheck.CommandText = "SELECT EXISTS(SELECT 1 FROM DanhSachBaNhat WHERE SoHieu = @sh)";
    //                var paramShCheck = cmdCheck.Parameters.Add("@sh", SqliteType.Text);

    //                using var cmdUpdate = conn.CreateCommand();
    //                cmdUpdate.Transaction = transaction;
    //                cmdUpdate.CommandText = @"UPDATE DanhSachBaNhat SET 
    //                HoVaTen = @ht, NamSinh = @ns, QueQuan = @qq, NgayVaoCAND = @nv, 
    //                CapBac = @cb, ChucVu = @cv, DonVi = @dv, PhanLoai = @pl, 
    //                GhiChu = @gc, 
    //                DeNghi = CASE WHEN @dnTrong = 1 THEN DeNghi ELSE @dn END, 
    //                ThanhTich = CASE WHEN @ttTrong = 1 THEN ThanhTich ELSE @tt END 
    //                WHERE SoHieu = @sh";
    //                PrepareParameters(cmdUpdate);
    //                cmdUpdate.Parameters.Add("@dnTrong", SqliteType.Integer);
    //                cmdUpdate.Parameters.Add("@ttTrong", SqliteType.Integer);

    //                using var cmdInsert = conn.CreateCommand();
    //                cmdInsert.Transaction = transaction;
    //                cmdInsert.CommandText = @"INSERT INTO DanhSachBaNhat 
    //                (STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu, DeNghi, ThanhTich) 
    //                VALUES (@stt, @ht, @sh, @ns, @qq, @nv, @cb, @cv, @dv, @pl, @gc, @dn, @tt)";
    //                var paramSttInsert = cmdInsert.Parameters.Add("@stt", SqliteType.Integer);
    //                PrepareParameters(cmdInsert);

    //                for (int r = 4; r <= lastRow; r++)
    //                {
    //                    string hoTen = ws.Cell(r, 2).GetString().Trim();
    //                    if (string.IsNullOrWhiteSpace(hoTen)) continue;

    //                    // 🔥 TRY...CATCH CỤC BỘ: Bắt chính xác lỗi phát sinh trên từng CBCS
    //                    try
    //                    {
    //                        string soHieu = ws.Cell(r, 3).GetString().Trim();
    //                        string deNghi = ws.Cell(r, 12).GetString().Trim();

    //                        if (string.IsNullOrWhiteSpace(soHieu))
    //                        {
    //                            danhSachBoQua.Add($"- Đồng chí: {hoTen} dòng {r} không thể nạp vì thiếu Số hiệu quân số.");
    //                            continue;
    //                        }

    //                        if (!deNghi.Equals("X", StringComparison.OrdinalIgnoreCase))
    //                        {
    //                            danhSachBoQua.Add($"- Đồng chí: {hoTen} (Số hiệu: {soHieu}) bỏ qua do không có dấu X Đề nghị.");
    //                            continue;
    //                        }

    //                        string namSinh = ws.Cell(r, 4).GetString().Trim();
    //                        string queQuan = ws.Cell(r, 5).GetString().Trim();
    //                        string ngayVao = ws.Cell(r, 6).GetString().Trim();
    //                        string capBac = ws.Cell(r, 7).GetString().Trim();
    //                        string chucVu = ws.Cell(r, 8).GetString().Trim();
    //                        string donVi = ws.Cell(r, 9).GetString().Trim();
    //                        string phanLoai = ws.Cell(r, 10).GetString().Trim();
    //                        string ghiChu = ws.Cell(r, 11).GetString().Trim();
    //                        string thanhTich = ws.Cell(r, 13).GetString().Trim();

    //                        // Bước thực thi mã hóa AES & Thao tác DB (Điểm dễ phát sinh Exception nhất)
    //                        string shMaHoa = BaoMatAES.MaHoa(soHieu);
    //                        paramShCheck.Value = shMaHoa;
    //                        bool daTonTai = Convert.ToInt64(cmdCheck.ExecuteScalar()) > 0;

    //                        if (daTonTai)
    //                        {
    //                            BindParameterValues(cmdUpdate, shMaHoa, hoTen, namSinh, queQuan, ngayVao, capBac, chucVu, donVi, phanLoai, ghiChu, deNghi, thanhTich);
    //                            cmdUpdate.Parameters["@dnTrong"].Value = string.IsNullOrWhiteSpace(deNghi) ? 1 : 0;
    //                            cmdUpdate.Parameters["@ttTrong"].Value = string.IsNullOrWhiteSpace(thanhTich) ? 1 : 0;

    //                            cmdUpdate.ExecuteNonQuery();
    //                        }
    //                        else
    //                        {
    //                            paramSttInsert.Value = nextSTT;
    //                            BindParameterValues(cmdInsert, shMaHoa, hoTen, namSinh, queQuan, ngayVao, capBac, chucVu, donVi, phanLoai, ghiChu, deNghi, thanhTich);

    //                            cmdInsert.ExecuteNonQuery();
    //                            nextSTT++;
    //                        }
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        // Ghi nhận đích danh đồng chí bị lỗi hệ thống/SQLite/AES và bỏ qua để nạp người tiếp theo
    //                        danhSachBoQua.Add($"- Đồng chí: {hoTen} (Dòng {r}) - Lỗi kỹ thuật: {ex.Message}");
    //                        continue;
    //                    }
    //                }

    //                // Commit những người đã được xử lý thành công (Skip các dòng rơi vào catch)
    //                transaction.Commit();
    //            }
    //        }

    //        if (danhSachBoQua.Count > 0)
    //        {
    //            string msgCanhBao = string.Join("\n", danhSachBoQua.Take(15));
    //            if (danhSachBoQua.Count > 15) msgCanhBao += $"\n... và {danhSachBoQua.Count - 15} trường hợp khác.";

    //            var activeForms = System.Windows.Forms.Application.OpenForms;
    //            if (activeForms.Count > 0)
    //            {
    //                activeForms[0].Invoke(new Action(() =>
    //                {
    //                    System.Windows.Forms.MessageBox.Show(activeForms[0], msgCanhBao, "Thông báo nạp Sổ Vàng", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
    //                }));
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        System.Diagnostics.Debug.WriteLine("[Lỗi NhapDataSoVang] " + ex.Message);
    //    }
    //}
    // ==============================================================================
    // CÁC HÀM HELPER HỖ TRỢ (Khởi tạo và Gán Parameter)
    // ==============================================================================
    private static void PrepareParameters(Microsoft.Data.Sqlite.SqliteCommand cmd)
    {
        cmd.Parameters.Add("@sh", SqliteType.Text);
        cmd.Parameters.Add("@ht", SqliteType.Text);
        cmd.Parameters.Add("@ns", SqliteType.Text);
        cmd.Parameters.Add("@qq", SqliteType.Text);
        cmd.Parameters.Add("@nv", SqliteType.Text);
        cmd.Parameters.Add("@cb", SqliteType.Text);
        cmd.Parameters.Add("@cv", SqliteType.Text);
        cmd.Parameters.Add("@dv", SqliteType.Text);
        cmd.Parameters.Add("@pl", SqliteType.Text);
        cmd.Parameters.Add("@gc", SqliteType.Text);
        cmd.Parameters.Add("@dn", SqliteType.Text);
        cmd.Parameters.Add("@tt", SqliteType.Text);
    }
    private static void BindParameterValues(Microsoft.Data.Sqlite.SqliteCommand cmd, string sh, string ht, string ns, string qq, string nv, string cb, string cv, string dv, string pl, string gc, string dn, string tt)
    {
        cmd.Parameters["@sh"].Value = sh;
        cmd.Parameters["@ht"].Value = BaoMatAES.MaHoa(ht);
        cmd.Parameters["@ns"].Value = BaoMatAES.MaHoa(ns);
        cmd.Parameters["@qq"].Value = BaoMatAES.MaHoa(qq);
        cmd.Parameters["@nv"].Value = BaoMatAES.MaHoa(nv);
        cmd.Parameters["@cb"].Value = BaoMatAES.MaHoa(cb);
        cmd.Parameters["@cv"].Value = BaoMatAES.MaHoa(cv);
        cmd.Parameters["@dv"].Value = BaoMatAES.MaHoa(dv);
        cmd.Parameters["@pl"].Value = BaoMatAES.MaHoa(pl);
        cmd.Parameters["@gc"].Value = BaoMatAES.MaHoa(gc);
        cmd.Parameters["@dn"].Value = BaoMatAES.MaHoa(dn);
        cmd.Parameters["@tt"].Value = BaoMatAES.MaHoa(tt);
    }
    public static void MoTepExcelThiDuaPhongTrao3Nhat(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return;

            IntPtr pidl = ILCreateFromPathW(filePath);
            if (pidl == IntPtr.Zero) return;

            SHOpenFolderAndSelectItems(pidl, 0, IntPtr.Zero, 0);
            ILFree(pidl);
        }
        catch
        {
        }
    }
    private static string SafeDecrypt(object value)
    {
        try
        {
            if (value == null || value == DBNull.Value) return "";
            string s = value.ToString();
            if (string.IsNullOrWhiteSpace(s)) return "";
            return BaoMatAES.GiaiMa(s);
        }
        catch
        {
            return "";
        }
    }
    public static void XuatBaoCaoTongHop(string fileXuat)
    {
        string tuanBaoCao = "";
        string loaiBaoCao = "";
        try
        {
            if (string.IsNullOrWhiteSpace(fileXuat) || !File.Exists(fileXuat))
                throw new Exception("File Excel tổng hợp không tồn tại.");

            using var wb = new XLWorkbook(fileXuat);
            var ws = wb.Worksheet("BC_BANHAT");

            string csdl2 = Module_DanduongGPS.DuongDanCSDL2;
            if (string.IsNullOrWhiteSpace(csdl2) || !File.Exists(csdl2))
                throw new Exception("Không tìm thấy CSDL csdl2.");

            // ====================================================================================
            // 1. ĐỌC TRỰC TIẾP THÔNG TIN ĐƠN VỊ TỪ BẢNG ThongTin (KHÔNG QUA MODULE TRUNG GIAN)
            // ====================================================================================
            string tenTrungDoanDong1 = "";
            string tenTrungDoanCSCD = "";
            string tenTieuDoanCSCD = "";
            string thang = "     ", nam = "     ", ngay = "     ", diaDiem = "     ", deNghi = "     ";

            try
            {
                using var conn = new SqliteConnection($"Data Source={csdl2}");
                conn.Open();
                using var cmd = new SqliteCommand("SELECT textBox1_TenTrungDoanDong1, TenTrungDoan, TenTieuDoan, Thang, Nam, Ngay, DiaDiem, LoaiDeNghi FROM ThongTin WHERE ID = 1", conn);
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    tenTrungDoanDong1 = SafeDecrypt(reader["textBox1_TenTrungDoanDong1"]);
                    tenTrungDoanCSCD = SafeDecrypt(reader["TenTrungDoan"]);
                    tenTieuDoanCSCD = SafeDecrypt(reader["TenTieuDoan"]);

                    if (!string.IsNullOrWhiteSpace(reader["Thang"]?.ToString())) thang = SafeDecrypt(reader["Thang"]);
                    if (!string.IsNullOrWhiteSpace(reader["Nam"]?.ToString())) nam = SafeDecrypt(reader["Nam"]);
                    if (!string.IsNullOrWhiteSpace(reader["Ngay"]?.ToString())) ngay = SafeDecrypt(reader["Ngay"]);
                    if (!string.IsNullOrWhiteSpace(reader["DiaDiem"]?.ToString())) diaDiem = SafeDecrypt(reader["DiaDiem"]);
                    if (!string.IsNullOrWhiteSpace(reader["LoaiDeNghi"]?.ToString())) deNghi = SafeDecrypt(reader["LoaiDeNghi"]);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi đọc bảng ThongTin: " + ex.Message);
            }

            // ====================================================================================
            // 2. LẤY KÝ HIỆU ĐƠN VỊ VÀ GHI VÀO Ô A11
            // ====================================================================================
            string kyHieuTieuDoan = "";
            string kyHieuTrungDoan = "";

            try
            {
                using var cnKyHieu = new SqliteConnection($"Data Source={csdl2}");
                cnKyHieu.Open();
                using var cmdKyHieu = new SqliteCommand("SELECT KyHieu_TrungDoan, KeHieu_TieuDoan FROM KyHieu_DonVi WHERE ID = 1", cnKyHieu);
                using var rdKyHieu = cmdKyHieu.ExecuteReader();

                if (rdKyHieu.Read())
                {
                    kyHieuTrungDoan = SafeDecrypt(rdKyHieu["KyHieu_TrungDoan"]);
                    kyHieuTieuDoan = SafeDecrypt(rdKyHieu["KeHieu_TieuDoan"]);
                }
            }
            catch { }

            kyHieuTieuDoan = kyHieuTieuDoan.Trim();
            kyHieuTrungDoan = kyHieuTrungDoan.Trim();

            string chuoiKyHieuGhep = "";
            if (!string.IsNullOrEmpty(kyHieuTieuDoan) && !string.IsNullOrEmpty(kyHieuTrungDoan))
                chuoiKyHieuGhep = $"{kyHieuTieuDoan}, {kyHieuTrungDoan}";
            else if (!string.IsNullOrEmpty(kyHieuTieuDoan))
                chuoiKyHieuGhep = kyHieuTieuDoan;
            else if (!string.IsNullOrEmpty(kyHieuTrungDoan))
                chuoiKyHieuGhep = kyHieuTrungDoan;

            var cellA11 = ws.Cell("A11");
            cellA11.Value = chuoiKyHieuGhep;
            cellA11.Style.Font.FontName = "Times New Roman";
            cellA11.Style.Font.FontSize = 12;
            cellA11.Style.Font.Bold = false;
            cellA11.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellA11.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            // ====================================================================================
            // 3. ĐỊNH DẠNG KHỐI ĐƠN VỊ PHÍA TRÊN (A1, A2, A3)
            // ====================================================================================
            void GanGachChan1Phan3(IXLCell cell, string text)
            {
                if (cell == null || string.IsNullOrWhiteSpace(text)) return;
                int totalLen = text.Length;
                int underlineLen = (int)Math.Round(totalLen / 3.0);
                if (underlineLen <= 0) return;

                int start = (totalLen - underlineLen) / 2;
                var rich = cell.GetRichText();
                rich.ClearText();

                if (start > 0) rich.AddText(text.Substring(0, start));
                rich.AddText(text.Substring(start, underlineLen)).SetUnderline();
                int end = start + underlineLen;
                if (end < totalLen) rich.AddText(text.Substring(end));
            }

            var cellA1 = ws.Cell("A1");
            cellA1.Value = string.IsNullOrWhiteSpace(tenTrungDoanDong1) ? "     " : tenTrungDoanDong1;
            cellA1.Style.Font.FontName = "Times New Roman"; cellA1.Style.Font.FontSize = 13;
            cellA1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            if (!string.IsNullOrWhiteSpace(tenTrungDoanCSCD))
            {
                var cellA2 = ws.Cell("A2"); cellA2.Value = tenTrungDoanCSCD;
                cellA2.Style.Font.FontName = "Times New Roman"; cellA2.Style.Font.FontSize = 13;
                cellA2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var cellA3 = ws.Cell("A3"); cellA3.Value = tenTieuDoanCSCD;
                cellA3.Style.Font.FontName = "Times New Roman"; cellA3.Style.Font.FontSize = 13; cellA3.Style.Font.Bold = true;
                cellA3.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                GanGachChan1Phan3(cellA3, tenTieuDoanCSCD);
            }
            else
            {
                var cellA2 = ws.Cell("A2"); cellA2.Value = tenTieuDoanCSCD;
                cellA2.Style.Font.FontName = "Times New Roman"; cellA2.Style.Font.FontSize = 13; cellA2.Style.Font.Bold = true;
                cellA2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                GanGachChan1Phan3(cellA2, tenTieuDoanCSCD);
                ws.Cell("A3").Value = "";
            }

            // Địa điểm ngày tháng H4
            var cellH4 = ws.Cell("H4");
            cellH4.Value = $"{diaDiem}, ngày {ngay} tháng {thang} năm {nam}";
            cellH4.Style.Font.FontName = "Times New Roman"; cellH4.Style.Font.FontSize = 14; cellH4.Style.Font.Italic = true;
            cellH4.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Loại báo cáo
            try
            {
                using var conn = new SqliteConnection($"Data Source={csdl2}");
                conn.Open();
                using var cmd = new SqliteCommand("SELECT ChonLoaiBaoCao, ChonTuan FROM ChonLoaiBaoCao ORDER BY ID DESC LIMIT 1", conn);
                using var rd = cmd.ExecuteReader();
                if (rd.Read())
                {
                    loaiBaoCao = SafeDecrypt(rd["ChonLoaiBaoCao"]);
                    tuanBaoCao = SafeDecrypt(rd["ChonTuan"]);
                }
            }
            catch { }

            string chuoiThoiGian;
            loaiBaoCao = (loaiBaoCao ?? "").Trim().ToUpper();
            tuanBaoCao = (tuanBaoCao ?? "").Trim();
            string thangHT = Module_XuatPhanLoai.LayThangHeThong();

            if (loaiBaoCao.Contains("TUẦN"))
            {
                if (tuanBaoCao.ToUpper().Contains("TUẦN")) tuanBaoCao = tuanBaoCao.ToUpper().Replace("TUẦN", "").Trim();
                if (string.IsNullOrWhiteSpace(tuanBaoCao)) tuanBaoCao = "1";
                chuoiThoiGian = $"TUẦN {tuanBaoCao} THÁNG {thangHT}/{nam}";
            }
            else
            {
                chuoiThoiGian = $"THÁNG {thangHT}/{nam}";
            }

            var tieuDe = ws.Range("A6:M6");
            tieuDe.Merge();
            tieuDe.Value = $"TRONG THỰC HIỆN PHONG TRÀO THI ĐUA \"BA NHẤT\" {chuoiThoiGian}";
            tieuDe.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            tieuDe.Style.Font.Bold = true; tieuDe.Style.Font.FontName = "Times New Roman"; tieuDe.Style.Font.FontSize = 14;

            // ====================================================================================
            // 4. ĐỊNH DẠNG DÒNG A7 CHUẨN XÁC VÀ GẠCH CHÂN DỰA TRÊN ĐƠN VỊ ĐỘNG
            // ====================================================================================
            string kyHieuBaoCao = "...............";
            try
            {
                using var cn = new SqliteConnection($"Data Source={csdl2}");
                cn.Open();
                using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT KyHieuBaoCao FROM ThongTin WHERE ID=1";
                var result = cmd.ExecuteScalar();
                if (result != null && !string.IsNullOrWhiteSpace(result.ToString()))
                    kyHieuBaoCao = BaoMatAES.GiaiMa(result.ToString());
            }
            catch { }

            string tieuDoanHienThi = string.IsNullOrWhiteSpace(tenTieuDoanCSCD)
                ? ""
                : char.ToUpper(tenTieuDoanCSCD.ToLower()[0]) + tenTieuDoanCSCD.ToLower().Substring(1);

            string textA7 = $"(Kèm theo Báo cáo số:            {kyHieuBaoCao}, ngày {ngay}/{thang}/{nam} của {tieuDoanHienThi})";
            var cellA7 = ws.Cell("A7");
            ws.Range("A7:M7").Merge();
            cellA7.Value = textA7;
            cellA7.Style.Font.Italic = true; cellA7.Style.Font.FontName = "Times New Roman"; cellA7.Style.Font.FontSize = 14;
            cellA7.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int startUnderline = textA7.IndexOf("cáo");
            int endUnderline = textA7.IndexOf("của");
            if (startUnderline >= 0 && endUnderline > startUnderline)
            {
                cellA7.GetRichText().Substring(startUnderline, endUnderline - startUnderline + 4).SetUnderline();
            }

            // ====================================================================================
            // 5. ĐỌC BẢNG TyLe VÀ ĐIỀN CÁC Ô SỐ LIỆU
            // ====================================================================================
            var tyLe = new Dictionary<int, (int hienTai, int canDat)>();
            using (var conn = new SqliteConnection($"Data Source={csdl2}"))
            {
                conn.Open();
                using var cmd = new SqliteCommand("SELECT ID, [KQ Hien tai], [KQ Can dat] FROM TyLe", conn);
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    int id = Convert.ToInt32(rd["ID"]);
                    int ht = Convert.ToInt32(rd["KQ Hien tai"] ?? 0);
                    int cd = Convert.ToInt32(rd["KQ Can dat"] ?? 0);
                    tyLe[id] = (ht, cd);
                }
            }

            int tongQS = tyLe.GetValueOrDefault(1).hienTai;
            int khongPL = tyLe.GetValueOrDefault(6).hienTai;
            ws.Cell("B11").Value = tongQS;
            ws.Cell("C11").Value = tongQS;
            ws.Cell("D11").Value = khongPL;
            ws.Cell("E11").Value = tyLe.GetValueOrDefault(2).canDat;
            ws.Cell("F11").Value = tyLe.GetValueOrDefault(3).canDat;
            ws.Cell("G11").Value = tyLe.GetValueOrDefault(4).canDat;
            ws.Cell("H11").Value = tyLe.GetValueOrDefault(5).canDat;
            ws.Cell("I11").Value = tyLe.GetValueOrDefault(6).canDat;
            ws.Cell("K11").Value = deNghi;

            // Đọc Tóm tắt thành tích đổ vào L11
            // ====================================================================================
            // Đọc Tóm tắt thành tích đổ vào L11
            // ====================================================================================
            string tomTatThanhTich = "";
            try
            {
                // 🌟 BỔ SUNG: Kiểm tra phiên bản tĩnh ngay tại đây
                string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
                string tenBangTomTat = phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase)
                    ? "TomTatThanhTichBaNhat_TanBinh"
                    : "TomTatThanhTichBaNhat_CBCS";

                using var cnTomTat = new SqliteConnection($"Data Source={csdl2}");
                cnTomTat.Open();

                // 🌟 SỬA ĐỊNH TUYẾN: Truy vấn vào đúng bảng
                using var cmdTomTat = new SqliteCommand($"SELECT NoiDung FROM [{tenBangTomTat}] WHERE ID = 1", cnTomTat);

                var objResult = cmdTomTat.ExecuteScalar();
                if (objResult != null && objResult != DBNull.Value)
                    tomTatThanhTich = BaoMatAES.GiaiMa(objResult.ToString()).Trim();
            }
            catch { }
            ws.Cell("L11").Value = tomTatThanhTich;
            // Thiết lập Công thức tính toán dữ liệu
            ws.Cell("E12").FormulaA1 = "=IFERROR((E11*100)/F11,\"Dữ liệu sai\")";
            ws.Cell("F12").FormulaA1 = "=IFERROR(IF(B11=0,0,(F11*100)/B11),\"Dữ liệu sai\")";
            ws.Cell("G12").FormulaA1 = "=IFERROR(100-F12,\"Dữ liệu sai\")";
            ws.Cell("H12").FormulaA1 = "=IFERROR(IF(OR(H11=\"\",H11=0),\"0%\",(H11*100)/B11 & \"%\"),\"Dữ liệu sai\")";
            ws.Cell("I12").FormulaA1 = "=IFERROR(IF(OR(I11=\"\",I11=0),\"0%\",(I11*100)/B11 & \"%\"),\"Dữ liệu sai\")";

            ws.Cell("E12").Style.NumberFormat.Format = "00.00\"%\"";
            ws.Cell("F12").Style.NumberFormat.Format = "00.00\"%\"";
            ws.Cell("G12").Style.NumberFormat.Format = "00.00\"%\"";
            ws.Cell("H12").Style.NumberFormat.Format = "00.00\"%\"";
            ws.Cell("I12").Style.NumberFormat.Format = "00.00\"%\"";

            // ===== 9. KÝ TÊN =====
            VietKyTenBaoCaoTongHop(ws, csdl2);

            // ===== 10. ÉP TÁCH SHEET VÀ ĐƯA CON TRỎ VỀ "BC_BANHAT" =====
            try
            {
                // 1. Quét toàn bộ và ép hủy chọn tất cả các sheet (Gỡ Group)
                foreach (var sheet in wb.Worksheets)
                {
                    sheet.SetTabSelected(false);
                }
                // 2. Chuyển sang sheet Báo Cáo
                if (wb.Worksheets.TryGetWorksheet("BC_BANHAT", out var wsBaoCao))
                {
                    // 3. Đặt nó làm sheet đang mở và chọn duy nhất nó
                    wsBaoCao.SetTabActive();
                    wsBaoCao.SetTabSelected(true);
                    // 4. Bôi đen vùng A1:G1 và đặt con trỏ nhấp nháy vào ô A1
                    wsBaoCao.Range("A1:G1").Select();
                    wsBaoCao.Cell("A1").SetActive();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi định vị con trỏ Excel: {ex.Message}");
            }
            // ===== 11. LƯU FILE =====
            wb.Save();
        }
        catch (Exception ex)
        {
            Module_ThongBao.DangXuLy("Lỗi xuất báo cáo tổng hợp: " + ex.Message);
        }
    }
    private static void VietKyTenBaoCaoTongHop(IXLWorksheet ws, string fileDB)
    {
        string hoTenKy = "     ";
        try
        {
            using var conn = new SqliteConnection($"Data Source={fileDB}");
            conn.Open();
            using var cmd = new SqliteCommand("SELECT ChiHuyD FROM ThongTin WHERE ID = 1", conn);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                string chiHuyRaw = reader["ChiHuyD"]?.ToString()?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(chiHuyRaw))
                {
                    string decoded = BaoMatAES.GiaiMa(chiHuyRaw);
                    hoTenKy = string.IsNullOrEmpty(decoded) ? chiHuyRaw : decoded.Trim();
                }
            }
        }
        catch { }

        string chucVuTieuDoanTruong = "";
        string chucVuNguoiKy = "";
        int idDauTien = -1, idNguoiKy = -1;

        using (var cn = new SqliteConnection("Data Source=" + fileDB))
        {
            cn.Open();
            using var cmd = new SqliteCommand("SELECT ID, HoVaTen, ChucVu FROM ChiHuyD ORDER BY ID ASC", cn);
            using var rd = cmd.ExecuteReader();
            bool isFirst = true, foundMatch = false;

            while (rd.Read())
            {
                int id = Convert.ToInt32(rd["ID"]);
                string hoTenRaw = rd["HoVaTen"]?.ToString()?.Trim() ?? "";
                string chucVuRaw = rd["ChucVu"]?.ToString()?.Trim() ?? "";

                string htDec = BaoMatAES.GiaiMa(hoTenRaw);
                if (string.IsNullOrEmpty(htDec)) htDec = hoTenRaw;

                string cvDec = BaoMatAES.GiaiMa(chucVuRaw);
                if (string.IsNullOrEmpty(cvDec)) cvDec = chucVuRaw;

                if (isFirst)
                {
                    idDauTien = id; chucVuTieuDoanTruong = cvDec; isFirst = false;
                }

                if (htDec.Equals(hoTenKy, StringComparison.OrdinalIgnoreCase))
                {
                    idNguoiKy = id; chucVuNguoiKy = cvDec; foundMatch = true;
                    break;
                }
            }
            if (!foundMatch) idNguoiKy = idDauTien;
        }

        if (idNguoiKy == idDauTien)
        {
            ws.Cell("L14").Value = chucVuTieuDoanTruong.ToUpper();
            ws.Cell("L15").Value = "";
        }
        else
        {
            ws.Cell("L14").Value = ("KT. " + chucVuTieuDoanTruong).ToUpper();
            ws.Cell("L15").Value = chucVuNguoiKy.ToUpper();
        }

        ws.Cell("L17").Value = hoTenKy;
        foreach (string addr in new[] { "L14", "L15", "L17" })
        {
            var c = ws.Cell(addr);
            c.Style.Font.FontName = "Times New Roman"; c.Style.Font.FontSize = 14;
            c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            c.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            if (addr != "L17") c.Style.Font.Bold = true;
        }
    }
}
