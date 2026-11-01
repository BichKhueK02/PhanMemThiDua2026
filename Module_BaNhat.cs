///Phiên bản 1
///
/// //using ClosedXML.Excel;
//using DocumentFormat.OpenXml.Office2010.Excel;
//using Microsoft.Data.Sqlite;
//using PhanMemThiDua2026;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Diagnostics;
//using System.IO;
//using System.Runtime.InteropServices;
//public class CbcDTO
//{
//    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
//    //0975287973
//    public int STT { get; set; }
//    public string HoVaTen { get; set; }
//    public string SoHieu { get; set; }
//    public string NamSinh { get; set; }
//    public string QueQuan { get; set; }
//    public string NgayVaoCAND { get; set; }
//    public string CapBac { get; set; }
//    public string ChucVu { get; set; }
//    public string DonVi { get; set; }
//    public string PhanLoai { get; set; }
//    public string ThanhTich { get; set; }
//    public string GhiChu { get; set; }
//}
//// Dùng cho Form42 - Xuất danh sách gốc (Không cần điều kiện)
//public class DanhSachGocBaNhatDTO
//{
//    public int ID { get; set; }
//    public string HoVaTen { get; set; }
//    public string SoHieu { get; set; }
//    public string NamSinh { get; set; }
//    public string QueQuan { get; set; }
//    public string NgayVaoCAND { get; set; }
//    public string CapBac { get; set; }
//    public string ChucVu { get; set; }
//    public string DonVi { get; set; }
//    public string PhanLoai { get; set; }
//    public string GhiChu { get; set; }
//    public string DeNghi { get; set; }
//    public string ThanhTich { get; set; }
//}
//// Dùng cho Form42 - Xuất danh sách đề nghị (Có dùng File Mẫu)
//public class DeNghiBaNhatDTO
//{
//    public int ID { get; set; }
//    public string HoVaTen { get; set; }
//    public string SoHieu { get; set; }
//    public string NamSinh { get; set; }
//    public string NgayVaoCAND { get; set; }
//    public string CapBac { get; set; }
//    public string ChucVu { get; set; }
//    public string DonVi { get; set; }
//    public string PhanLoai { get; set; }
//    public string ThanhTich { get; set; }
//}
//// Dùng cho Form44 - Xuất Sổ vàng (Chỉ có 5 Cột)
//public class SoVangDTO
//{
//    public int STT { get; set; }
//    public string HoVaTen { get; set; }
//    public string ThongBaoTrungDoan { get; set; }
//    public string SoTTTrongSo { get; set; }
//    public string ThangCongNhan { get; set; }
//}
//public static class Module_BaNhat
//{
//    public const int GioiHanToiDa = 3000;
//    [DllImport("shell32.dll", ExactSpelling = true)]
//    private static extern void ILFree(IntPtr pidlList);
//    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
//    private static extern IntPtr ILCreateFromPathW(string pszPath);
//    [DllImport("shell32.dll")]
//    private static extern int SHOpenFolderAndSelectItems(
//        IntPtr pidlFolder,
//        uint cidl,
//        IntPtr apidl,
//        int dwFlags);
//    // HÀM NHẬP DỮ LIỆU (Đã tái cấu trúc tương thích tuyệt đối & Bắt lỗi cục bộ)
//    public static void XuatDuLieuVaoBangQuanLyBaNhat(ClosedXML.Excel.XLWorkbook package, string dbPath, bool epXuatFileMau)
//    {
//        try
//        {
//            // 1. Kiểm tra và xử lý tạo mới hoặc dọn dẹp Sheet cũ
//            ClosedXML.Excel.IXLWorksheet wsBN;
//            if (package.Worksheets.Contains("DS_BaNhat"))
//            {
//                wsBN = package.Worksheet("DS_BaNhat");
//                wsBN.Clear(); // Xóa sạch nội dung cũ nếu có để ghi đè chuẩn xác
//            }
//            else
//            {
//                wsBN = package.Worksheets.Add("DS_BaNhat");
//            }
//            // Cấu hình phông nền và màu sắc nhận diện hệ thống
//            wsBN.TabColor = ClosedXML.Excel.XLColor.Gold;
//            wsBN.Style.Font.SetFontName(Module_HeThong.Font_Times_New_Roman);
//            wsBN.Style.Font.SetFontSize(12);
//            // 2. Thiết lập tiêu đề dòng (Header) cho bảng
//            wsBN.Range("A1:M1").Merge().Value = "DANH SÁCH CÁN BỘ CHIẾN SĨ ĐỀ NGHỊ BIỂU DƯƠNG PHONG TRÀO \"BA NHẤT\"";
//            wsBN.Cell("A1").Style.Font.SetBold(true).Font.SetFontSize(14).Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);
//            wsBN.Row(1).Height = 30;
//            // ⭐ ĐÃ SỬA: Khớp chuẩn 13 cột với Database (Bỏ cột ID vì chỉ dùng ngầm)
//            string[] headers = { "STT", "Họ và tên", "Số hiệu", "Năm sinh", "Quê quán", "Ngày vào CAND", "Cấp bậc", "Chức vụ", "Đơn vị", "Phân loại", "Ghi chú", "Đề nghị", "Thành tích" };
//            wsBN.Row(3).Height = 25;
//            for (int i = 0; i < headers.Length; i++)
//            {
//                var cell = wsBN.Cell(3, i + 1);
//                cell.Value = headers[i];
//                cell.Style.Font.Bold = true;
//                cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
//                cell.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
//                cell.Style.Alignment.WrapText = true;
//                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#EBF1F5");
//                cell.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
//            }
//            // ⭐ ĐÃ SỬA: Thiết lập độ rộng chuẩn cho 13 cột
//            double[] widths = { 6, 25, 12, 10, 25, 12, 14, 15, 15, 12, 20, 10, 35 };
//            for (int i = 0; i < widths.Length; i++) wsBN.Column(i + 1).Width = widths[i];
//            // 3. Đọc dữ liệu từ SQLite & Giải mã lọc điều kiện (Chỉ chạy khi không phải file mẫu)
//            if (!epXuatFileMau && File.Exists(dbPath))
//            {
//                var listXuat = new List<Dictionary<string, string>>();
//                // Hàm giải mã nội bộ bảo hiểm zero-crash tuyệt đối cho vòng lặp
//                string ThucThiGiaiMa(object? val)
//                {
//                    if (val == null || val == DBNull.Value) return string.Empty;
//                    string s = val.ToString()?.Trim() ?? string.Empty;
//                    if (string.IsNullOrEmpty(s)) return string.Empty;
//                    try { return Module_BaoMatAES.GiaiMa(s)?.Trim() ?? string.Empty; } catch { return s; }
//                }
//                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
//                {
//                    conn.Open();
//                    using var cmd = conn.CreateCommand();
//                    // ⭐ ĐÃ SỬA: Gọi đúng thứ tự cột khớp với mảng Header phía trên
//                    cmd.CommandText = "SELECT STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu, DeNghi, ThanhTich FROM DanhSachBaNhat ORDER BY STT ASC";
//                    using var rd = cmd.ExecuteReader();
//                    int sttDem = 1;
//                    while (rd.Read())
//                    {
//                        string deNghiDecrypted = ThucThiGiaiMa(rd["DeNghi"]);
//                        if (!deNghiDecrypted.Equals("X", StringComparison.OrdinalIgnoreCase))
//                            continue;
//                        // Dùng Dictionary để linh động không phụ thuộc hoàn toàn vào Class DTO nếu sau này có đổi cột
//                        var rowData = new Dictionary<string, string>
//                        {
//                            { "STT", (sttDem++).ToString() },
//                            { "HoVaTen", ThucThiGiaiMa(rd["HoVaTen"]) },
//                            { "SoHieu", ThucThiGiaiMa(rd["SoHieu"]) },
//                            { "NamSinh", ThucThiGiaiMa(rd["NamSinh"]) },
//                            { "QueQuan", ThucThiGiaiMa(rd["QueQuan"]) },
//                            { "NgayVaoCAND", ThucThiGiaiMa(rd["NgayVaoCAND"]) },
//                            { "CapBac", ThucThiGiaiMa(rd["CapBac"]) },
//                            { "ChucVu", ThucThiGiaiMa(rd["ChucVu"]) },
//                            { "DonVi", ThucThiGiaiMa(rd["DonVi"]) },
//                            { "PhanLoai", ThucThiGiaiMa(rd["PhanLoai"]) },
//                            { "GhiChu", ThucThiGiaiMa(rd["GhiChu"]) },
//                            { "DeNghi", deNghiDecrypted },
//                            { "ThanhTich", ThucThiGiaiMa(rd["ThanhTich"]) }
//                        };
//                        listXuat.Add(rowData);
//                    }
//                }
//                // 4. Đổ dữ liệu hàng loạt lên trang tính (Tốc độ cao)
//                int dataRowIndex = 4;
//                foreach (var item in listXuat)
//                {
//                    wsBN.Cell(dataRowIndex, 1).Value = item["STT"];
//                    wsBN.Cell(dataRowIndex, 2).Value = item["HoVaTen"];
//                    wsBN.Cell(dataRowIndex, 3).Value = item["SoHieu"];
//                    wsBN.Cell(dataRowIndex, 4).Value = item["NamSinh"];
//                    wsBN.Cell(dataRowIndex, 5).Value = item["QueQuan"];
//                    wsBN.Cell(dataRowIndex, 6).Value = item["NgayVaoCAND"];
//                    wsBN.Cell(dataRowIndex, 7).Value = item["CapBac"];
//                    wsBN.Cell(dataRowIndex, 8).Value = item["ChucVu"];
//                    wsBN.Cell(dataRowIndex, 9).Value = item["DonVi"];
//                    wsBN.Cell(dataRowIndex, 10).Value = item["PhanLoai"];
//                    wsBN.Cell(dataRowIndex, 11).Value = item["GhiChu"];
//                    wsBN.Cell(dataRowIndex, 12).Value = item["DeNghi"];
//                    wsBN.Cell(dataRowIndex, 13).Value = item["ThanhTich"];
//                    // Kẻ viền khung nét mảnh và căn lề từng dòng dữ liệu (Tới cột 13)
//                    var rngRow = wsBN.Range(dataRowIndex, 1, dataRowIndex, 13);
//                    rngRow.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
//                    rngRow.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
//                    rngRow.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
//                    rngRow.Style.Alignment.SetWrapText(true);
//                    // Căn lề riêng biệt cho từng loại dữ liệu
//                    wsBN.Cell(dataRowIndex, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center; // STT
//                    wsBN.Cell(dataRowIndex, 2).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;   // Họ tên
//                    wsBN.Cell(dataRowIndex, 3).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center; // Số hiệu
//                    wsBN.Cell(dataRowIndex, 4).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center; // Năm sinh
//                    wsBN.Cell(dataRowIndex, 5).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;   // Quê quán
//                    wsBN.Cell(dataRowIndex, 6).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center; // Vào CAND
//                    wsBN.Cell(dataRowIndex, 7).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center; // Cấp bậc
//                    wsBN.Cell(dataRowIndex, 8).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center; // Chức vụ
//                    wsBN.Cell(dataRowIndex, 9).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center; // Đơn vị
//                    wsBN.Cell(dataRowIndex, 10).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;// Phân loại
//                    wsBN.Cell(dataRowIndex, 11).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;  // Ghi chú
//                    wsBN.Cell(dataRowIndex, 12).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;// Đề nghị
//                    wsBN.Cell(dataRowIndex, 13).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;  // Thành tích
//                    wsBN.Row(dataRowIndex).Height = 24;
//                    dataRowIndex++;
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            System.Diagnostics.Debug.WriteLine("Lỗi thực thi tích hợp tại Sheet DS_BaNhat: " + ex.Message);
//        }
//    }
//    public static void NhapDuLieuVaoBangQuanLyBaNhat(string excelPath, string dbPath)
//    {
//        try
//        {
//            if (!File.Exists(excelPath) || !File.Exists(dbPath)) return;
//            var danhSachBoQua = new List<string>();
//            using (var wb = new ClosedXML.Excel.XLWorkbook(excelPath))
//            {
//                if (!wb.Worksheets.TryGetWorksheet("DS_BaNhat", out var ws)) return;
//                // KIỂM TRA HEADER
//                string[] mauHeaderChuan = { "STT", "Họ và tên", "Số hiệu", "Năm sinh", "Quê quán", "Ngày vào CAND", "Cấp bậc", "Chức vụ", "Đơn vị", "Phân loại", "Ghi chú", "Đề nghị", "Thành tích" };
//                for (int i = 0; i < mauHeaderChuan.Length; i++)
//                {
//                    string headerExcel = ws.Cell(3, i + 1).GetString().Trim();
//                    if (!headerExcel.Equals(mauHeaderChuan[i], StringComparison.OrdinalIgnoreCase))
//                    {
//                        System.Windows.Forms.MessageBox.Show(
//                            $"Cấu trúc tệp Excel không hợp lệ!\nCột số {i + 1} đang là '{headerExcel}', yêu cầu chuẩn phải là '{mauHeaderChuan[i]}'.\nVui lòng không tự ý chèn, xóa hoặc đổi tên cột mẫu.",
//                            "Lỗi cấu trúc tệp nhập", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
//                        return;
//                    }
//                }
//                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
//                if (lastRow < 4) return;
//                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
//                {
//                    conn.Open();
//                    int nextSTT = 0;
//                    using (var cmdMax = conn.CreateCommand())
//                    {
//                        cmdMax.CommandText = "SELECT IFNULL(MAX(STT), 0) FROM DanhSachBaNhat";
//                        nextSTT = Convert.ToInt32(cmdMax.ExecuteScalar()) + 1;
//                    }
//                    using var transaction = conn.BeginTransaction();
//                    using var cmdCheck = conn.CreateCommand();
//                    cmdCheck.Transaction = transaction;
//                    cmdCheck.CommandText = "SELECT EXISTS(SELECT 1 FROM DanhSachBaNhat WHERE SoHieu = @sh)";
//                    var paramShCheck = cmdCheck.Parameters.Add("@sh", Microsoft.Data.Sqlite.SqliteType.Text);
//                    // ⭐ ĐÃ SỬA: Loại bỏ hoàn toàn DeNghi và ThanhTich khỏi lệnh UPDATE
//                    using var cmdUpdate = conn.CreateCommand();
//                    cmdUpdate.Transaction = transaction;
//                    cmdUpdate.CommandText = @"UPDATE DanhSachBaNhat SET 
//                HoVaTen = @ht, NamSinh = @ns, QueQuan = @qq, NgayVaoCAND = @nv, 
//                CapBac = @cb, ChucVu = @cv, DonVi = @dv, PhanLoai = @pl, 
//                GhiChu = @gc 
//                WHERE SoHieu = @sh";
//                    PrepareParameters(cmdUpdate);
//                    using var cmdInsert = conn.CreateCommand();
//                    cmdInsert.Transaction = transaction;
//                    cmdInsert.CommandText = @"INSERT INTO DanhSachBaNhat 
//                (STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu, DeNghi, ThanhTich) 
//                VALUES (@stt, @ht, @sh, @ns, @qq, @nv, @cb, @cv, @dv, @pl, @gc, @dn, @tt)";
//                    var paramSttInsert = cmdInsert.Parameters.Add("@stt", Microsoft.Data.Sqlite.SqliteType.Integer);
//                    PrepareParameters(cmdInsert);
//                    for (int r = 4; r <= lastRow; r++)
//                    {
//                        string hoTen = ws.Cell(r, 2).GetString().Trim();
//                        if (string.IsNullOrWhiteSpace(hoTen)) continue;
//                        try
//                        {
//                            string soHieu = ws.Cell(r, 3).GetString().Trim();
//                            string deNghi = ws.Cell(r, 12).GetString().Trim();
//                            if (string.IsNullOrWhiteSpace(soHieu))
//                            {
//                                danhSachBoQua.Add($"- {Module_HeThong.Tu_Dong_Chi}: {hoTen} dòng {r} không thể nạp vì thiếu Số hiệu quân số.");
//                                continue;
//                            }
//                            if (!deNghi.Equals("X", StringComparison.OrdinalIgnoreCase))
//                            {
//                                danhSachBoQua.Add($"- {Module_HeThong.Tu_Dong_Chi}: {hoTen} (Số hiệu: {soHieu}) bỏ qua do không có dấu X Đề nghị.");
//                                continue;
//                            }
//                            string namSinh = ws.Cell(r, 4).GetString().Trim();
//                            string queQuan = ws.Cell(r, 5).GetString().Trim();
//                            string ngayVao = ws.Cell(r, 6).GetString().Trim();
//                            string capBac = ws.Cell(r, 7).GetString().Trim();
//                            string chucVu = ws.Cell(r, 8).GetString().Trim();
//                            string donVi = ws.Cell(r, 9).GetString().Trim();
//                            string phanLoai = ws.Cell(r, 10).GetString().Trim();
//                            string ghiChu = ws.Cell(r, 11).GetString().Trim();
//                            string thanhTich = ws.Cell(r, 13).GetString().Trim();
//                            string shMaHoa = Module_BaoMatAES.MaHoa(soHieu);
//                            paramShCheck.Value = shMaHoa;
//                            bool daTonTai = Convert.ToInt64(cmdCheck.ExecuteScalar()) > 0;
//                            if (daTonTai)
//                            {
//                                BindParameterValues(cmdUpdate, shMaHoa, hoTen, namSinh, queQuan, ngayVao, capBac, chucVu, donVi, phanLoai, ghiChu, deNghi, thanhTich);
//                                // ⭐ ĐÃ XÓA BỎ 2 lệnh gán param @dnTrong và @ttTrong dư thừa
//                                cmdUpdate.ExecuteNonQuery();
//                            }
//                            else
//                            {
//                                paramSttInsert.Value = nextSTT;
//                                BindParameterValues(cmdInsert, shMaHoa, hoTen, namSinh, queQuan, ngayVao, capBac, chucVu, donVi, phanLoai, ghiChu, deNghi, thanhTich);
//                                cmdInsert.ExecuteNonQuery();
//                                nextSTT++;
//                            }
//                        }
//                        catch (Exception ex)
//                        {
//                            danhSachBoQua.Add($"- {Module_HeThong.Tu_Dong_Chi}: {hoTen} (Dòng {r}) - Lỗi kỹ thuật: {ex.Message}");
//                            continue;
//                        }
//                    }
//                    transaction.Commit();
//                }
//            }
//            if (danhSachBoQua.Count > 0)
//            {
//                string msgCanhBao = string.Join("\n", danhSachBoQua.Take(15));
//                if (danhSachBoQua.Count > 15) msgCanhBao += $"\n... và {danhSachBoQua.Count - 15} trường hợp khác.";
//                var activeForms = System.Windows.Forms.Application.OpenForms;
//                if (activeForms.Count > 0)
//                {
//                    activeForms[0].Invoke(new Action(() =>
//                    {
//                        System.Windows.Forms.MessageBox.Show(activeForms[0], msgCanhBao, "Thông báo nạp Sổ Vàng", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
//                    }));
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            System.Diagnostics.Debug.WriteLine("[Lỗi NhapDataSoVang] " + ex.Message);
//        }
//    }
//    private static void PrepareParameters(Microsoft.Data.Sqlite.SqliteCommand cmd)
//    {
//        cmd.Parameters.Add("@sh", SqliteType.Text);
//        cmd.Parameters.Add("@ht", SqliteType.Text);
//        cmd.Parameters.Add("@ns", SqliteType.Text);
//        cmd.Parameters.Add("@qq", SqliteType.Text);
//        cmd.Parameters.Add("@nv", SqliteType.Text);
//        cmd.Parameters.Add("@cb", SqliteType.Text);
//        cmd.Parameters.Add("@cv", SqliteType.Text);
//        cmd.Parameters.Add("@dv", SqliteType.Text);
//        cmd.Parameters.Add("@pl", SqliteType.Text);
//        cmd.Parameters.Add("@gc", SqliteType.Text);
//        cmd.Parameters.Add("@dn", SqliteType.Text);
//        cmd.Parameters.Add("@tt", SqliteType.Text);
//    }
//    private static void BindParameterValues(Microsoft.Data.Sqlite.SqliteCommand cmd, string sh, string ht, string ns, string qq, string nv, string cb, string cv, string dv, string pl, string gc, string dn, string tt)
//    {
//        cmd.Parameters["@sh"].Value = sh;
//        cmd.Parameters["@ht"].Value = Module_BaoMatAES.MaHoa(ht);
//        cmd.Parameters["@ns"].Value = Module_BaoMatAES.MaHoa(ns);
//        cmd.Parameters["@qq"].Value = Module_BaoMatAES.MaHoa(qq);
//        cmd.Parameters["@nv"].Value = Module_BaoMatAES.MaHoa(nv);
//        cmd.Parameters["@cb"].Value = Module_BaoMatAES.MaHoa(cb);
//        cmd.Parameters["@cv"].Value = Module_BaoMatAES.MaHoa(cv);
//        cmd.Parameters["@dv"].Value = Module_BaoMatAES.MaHoa(dv);
//        cmd.Parameters["@pl"].Value = Module_BaoMatAES.MaHoa(pl);
//        cmd.Parameters["@gc"].Value = Module_BaoMatAES.MaHoa(gc);
//        cmd.Parameters["@dn"].Value = Module_BaoMatAES.MaHoa(dn);
//        cmd.Parameters["@tt"].Value = Module_BaoMatAES.MaHoa(tt);
//    }
//    public static void MoTepExcelThiDuaPhongTrao3Nhat(string filePath)
//    {
//        try
//        {
//            if (!File.Exists(filePath)) return;
//            IntPtr pidl = ILCreateFromPathW(filePath);
//            if (pidl == IntPtr.Zero) return;
//            SHOpenFolderAndSelectItems(pidl, 0, IntPtr.Zero, 0);
//            ILFree(pidl);
//        }
//        catch
//        {
//        }
//    }
//    private static string SafeDecrypt(object value)
//    {
//        try
//        {
//            if (value == null || value == DBNull.Value) return "";
//            string s = value.ToString();
//            if (string.IsNullOrWhiteSpace(s)) return "";
//            return Module_BaoMatAES.GiaiMa(s);
//        }
//        catch
//        {
//            return "";
//        }
//    }
//    public static void XuatBaoCaoTongHop(string fileXuat)
//    {
//        string tuanBaoCao = "";
//        string loaiBaoCao = "";
//        try
//        {
//            if (string.IsNullOrWhiteSpace(fileXuat) || !File.Exists(fileXuat))
//                throw new Exception("File Excel tổng hợp không tồn tại.");
//            using var wb = new XLWorkbook(fileXuat);
//            var ws = wb.Worksheet("BC_BANHAT");
//            string csdl2 = Module_DanduongGPS.DuongDanCSDL2;
//            if (string.IsNullOrWhiteSpace(csdl2) || !File.Exists(csdl2))
//                throw new Exception("Không tìm thấy CSDL csdl2.");
//            // 1. ĐỌC TRỰC TIẾP THÔNG TIN ĐƠN VỊ TỪ BẢNG ThongTin (KHÔNG QUA MODULE TRUNG GIAN)
//            string tenTrungDoanDong1 = "";
//            string tenTrungDoanCSCD = "";
//            string tenTieuDoanCSCD = "";
//            string thang = "     ", nam = "     ", ngay = "     ", diaDiem = "     ", deNghi = "     ";
//            try
//            {
//                using var conn = new SqliteConnection($"Data Source={csdl2}");
//                conn.Open();
//                using var cmd = new SqliteCommand("SELECT textBox1_TenTrungDoanDong1, TenTrungDoan, TenTieuDoan, Thang, Nam, Ngay, DiaDiem, LoaiDeNghi FROM ThongTin WHERE ID = 1", conn);
//                using var reader = cmd.ExecuteReader();
//                if (reader.Read())
//                {
//                    tenTrungDoanDong1 = SafeDecrypt(reader["textBox1_TenTrungDoanDong1"]);
//                    tenTrungDoanCSCD = SafeDecrypt(reader["TenTrungDoan"]);
//                    tenTieuDoanCSCD = SafeDecrypt(reader["TenTieuDoan"]);
//                    if (!string.IsNullOrWhiteSpace(reader["Thang"]?.ToString())) thang = SafeDecrypt(reader["Thang"]);
//                    if (!string.IsNullOrWhiteSpace(reader["Nam"]?.ToString())) nam = SafeDecrypt(reader["Nam"]);
//                    if (!string.IsNullOrWhiteSpace(reader["Ngay"]?.ToString())) ngay = SafeDecrypt(reader["Ngay"]);
//                    if (!string.IsNullOrWhiteSpace(reader["DiaDiem"]?.ToString())) diaDiem = SafeDecrypt(reader["DiaDiem"]);
//                    if (!string.IsNullOrWhiteSpace(reader["LoaiDeNghi"]?.ToString())) deNghi = SafeDecrypt(reader["LoaiDeNghi"]);
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine("Lỗi đọc bảng ThongTin: " + ex.Message);
//            }
//            // 2. LẤY KÝ HIỆU ĐƠN VỊ VÀ GHI VÀO Ô A11
//            string kyHieuTieuDoan = "";
//            string kyHieuTrungDoan = "";
//            try
//            {
//                using var cnKyHieu = new SqliteConnection($"Data Source={csdl2}");
//                cnKyHieu.Open();
//                using var cmdKyHieu = new SqliteCommand("SELECT KyHieu_TrungDoan, KyHieu_TieuDoan FROM KyHieu_DonVi WHERE ID = 1", cnKyHieu);
//                using var rdKyHieu = cmdKyHieu.ExecuteReader();
//                if (rdKyHieu.Read())
//                {
//                    kyHieuTrungDoan = SafeDecrypt(rdKyHieu["KyHieu_TrungDoan"]);
//                    kyHieuTieuDoan = SafeDecrypt(rdKyHieu["KyHieu_TieuDoan"]);
//                }
//            }
//            catch { }
//            kyHieuTieuDoan = kyHieuTieuDoan.Trim();
//            kyHieuTrungDoan = kyHieuTrungDoan.Trim();
//            string chuoiKyHieuGhep = "";
//            if (!string.IsNullOrEmpty(kyHieuTieuDoan) && !string.IsNullOrEmpty(kyHieuTrungDoan))
//                chuoiKyHieuGhep = $"{kyHieuTieuDoan}, {kyHieuTrungDoan}";
//            else if (!string.IsNullOrEmpty(kyHieuTieuDoan))
//                chuoiKyHieuGhep = kyHieuTieuDoan;
//            else if (!string.IsNullOrEmpty(kyHieuTrungDoan))
//                chuoiKyHieuGhep = kyHieuTrungDoan;
//            var cellA11 = ws.Cell("A11");
//            cellA11.Value = chuoiKyHieuGhep;
//            cellA11.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
//            cellA11.Style.Font.FontSize = 12;
//            cellA11.Style.Font.Bold = false;
//            cellA11.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//            cellA11.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//            // 3. ĐỊNH DẠNG KHỐI ĐƠN VỊ PHÍA TRÊN (A1, A2, A3)
//            void GanGachChan1Phan3(IXLCell cell, string text)
//            {
//                if (cell == null || string.IsNullOrWhiteSpace(text)) return;
//                int totalLen = text.Length;
//                int underlineLen = (int)Math.Round(totalLen / 3.0);
//                if (underlineLen <= 0) return;
//                int start = (totalLen - underlineLen) / 2;
//                var rich = cell.GetRichText();
//                rich.ClearText();
//                if (start > 0) rich.AddText(text.Substring(0, start));
//                rich.AddText(text.Substring(start, underlineLen)).SetUnderline();
//                int end = start + underlineLen;
//                if (end < totalLen) rich.AddText(text.Substring(end));
//            }
//            var cellA1 = ws.Cell("A1");
//            cellA1.Value = string.IsNullOrWhiteSpace(tenTrungDoanDong1) ? "     " : tenTrungDoanDong1;
//            cellA1.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman; cellA1.Style.Font.FontSize = 13;
//            cellA1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//            if (!string.IsNullOrWhiteSpace(tenTrungDoanCSCD))
//            {
//                var cellA2 = ws.Cell("A2"); cellA2.Value = tenTrungDoanCSCD;
//                cellA2.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman; cellA2.Style.Font.FontSize = 13;
//                cellA2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                var cellA3 = ws.Cell("A3"); cellA3.Value = tenTieuDoanCSCD;
//                cellA3.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman; cellA3.Style.Font.FontSize = 13; cellA3.Style.Font.Bold = true;
//                cellA3.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                GanGachChan1Phan3(cellA3, tenTieuDoanCSCD);
//            }
//            else
//            {
//                var cellA2 = ws.Cell("A2"); cellA2.Value = tenTieuDoanCSCD;
//                cellA2.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman; cellA2.Style.Font.FontSize = 13; cellA2.Style.Font.Bold = true;
//                cellA2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                GanGachChan1Phan3(cellA2, tenTieuDoanCSCD);
//                ws.Cell("A3").Value = "";
//            }
//            // Địa điểm ngày tháng H4
//            var cellH4 = ws.Cell("H4");
//            cellH4.Value = $"{diaDiem}, ngày {ngay} tháng {thang} năm {nam}";
//            cellH4.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman; cellH4.Style.Font.FontSize = 14; cellH4.Style.Font.Italic = true;
//            cellH4.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//            // Loại báo cáo
//            try
//            {
//                using var conn = new SqliteConnection($"Data Source={csdl2}");
//                conn.Open();
//                using var cmd = new SqliteCommand("SELECT ChonLoaiBaoCao, ChonTuan FROM ChonLoaiBaoCao ORDER BY ID DESC LIMIT 1", conn);
//                using var rd = cmd.ExecuteReader();
//                if (rd.Read())
//                {
//                    loaiBaoCao = SafeDecrypt(rd["ChonLoaiBaoCao"]);
//                    tuanBaoCao = SafeDecrypt(rd["ChonTuan"]);
//                }
//            }
//            catch { }
//            string chuoiThoiGian;
//            loaiBaoCao = (loaiBaoCao ?? "").Trim().ToUpper();
//            tuanBaoCao = (tuanBaoCao ?? "").Trim();
//            string thangHT = Module_XuatPhanLoai.LayThangHeThong();
//            if (loaiBaoCao.Contains("TUẦN"))
//            {
//                if (tuanBaoCao.ToUpper().Contains("TUẦN")) tuanBaoCao = tuanBaoCao.ToUpper().Replace("TUẦN", "").Trim();
//                if (string.IsNullOrWhiteSpace(tuanBaoCao)) tuanBaoCao = "1";
//                chuoiThoiGian = $"TUẦN {tuanBaoCao} THÁNG {thangHT}/{nam}";
//            }
//            else
//            {
//                chuoiThoiGian = $"THÁNG {thangHT}/{nam}";
//            }
//            var tieuDe = ws.Range("A6:M6");
//            tieuDe.Merge();
//            tieuDe.Value = $"TRONG THỰC HIỆN PHONG TRÀO THI ĐUA \"BA NHẤT\" {chuoiThoiGian}";
//            tieuDe.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//            tieuDe.Style.Font.Bold = true; tieuDe.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman; tieuDe.Style.Font.FontSize = 14;
//            // 4. ĐỊNH DẠNG DÒNG A7 CHUẨN XÁC VÀ GẠCH CHÂN DỰA TRÊN ĐƠN VỊ ĐỘNG
//            string kyHieuBaoCao = "...............";
//            try
//            {
//                using var cn = new SqliteConnection($"Data Source={csdl2}");
//                cn.Open();
//                using var cmd = cn.CreateCommand();
//                cmd.CommandText = "SELECT KyHieuBaoCao FROM ThongTin WHERE ID=1";
//                var result = cmd.ExecuteScalar();
//                if (result != null && !string.IsNullOrWhiteSpace(result.ToString()))
//                    kyHieuBaoCao = Module_BaoMatAES.GiaiMa(result.ToString());
//            }
//            catch { }
//            string tieuDoanHienThi = string.IsNullOrWhiteSpace(tenTieuDoanCSCD)
//                ? ""
//                : char.ToUpper(tenTieuDoanCSCD.ToLower()[0]) + tenTieuDoanCSCD.ToLower().Substring(1);
//            string textA7 = $"(Kèm theo Báo cáo số:            {kyHieuBaoCao}, ngày {ngay}/{thang}/{nam} của {tieuDoanHienThi})";
//            var cellA7 = ws.Cell("A7");
//            ws.Range("A7:M7").Merge();
//            cellA7.Value = textA7;
//            cellA7.Style.Font.Italic = true; cellA7.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman; cellA7.Style.Font.FontSize = 14;
//            cellA7.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//            int startUnderline = textA7.IndexOf("cáo");
//            int endUnderline = textA7.IndexOf("của");
//            if (startUnderline >= 0 && endUnderline > startUnderline)
//            {
//                cellA7.GetRichText().Substring(startUnderline, endUnderline - startUnderline + 4).SetUnderline();
//            }
//            // 5. ĐỌC BẢNG TyLe VÀ ĐIỀN CÁC Ô SỐ LIỆU
//            var tyLe = new Dictionary<int, (int hienTai, int canDat)>();
//            using (var conn = new SqliteConnection($"Data Source={csdl2}"))
//            {
//                conn.Open();
//                using var cmd = new SqliteCommand("SELECT ID, [KQ Hien tai], [KQ Can dat] FROM TyLe", conn);
//                using var rd = cmd.ExecuteReader();
//                while (rd.Read())
//                {
//                    int id = Convert.ToInt32(rd["ID"]);
//                    int ht = Convert.ToInt32(rd["KQ Hien tai"] ?? 0);
//                    int cd = Convert.ToInt32(rd["KQ Can dat"] ?? 0);
//                    tyLe[id] = (ht, cd);
//                }
//            }
//            int tongQS = tyLe.GetValueOrDefault(1).hienTai;
//            int khongPL = tyLe.GetValueOrDefault(6).hienTai;
//            ws.Cell("B11").Value = tongQS;
//            ws.Cell("C11").Value = tongQS;
//            ws.Cell("D11").Value = khongPL;
//            ws.Cell("E11").Value = tyLe.GetValueOrDefault(2).canDat;
//            ws.Cell("F11").Value = tyLe.GetValueOrDefault(3).canDat;
//            ws.Cell("G11").Value = tyLe.GetValueOrDefault(4).canDat;
//            ws.Cell("H11").Value = tyLe.GetValueOrDefault(5).canDat;
//            ws.Cell("I11").Value = tyLe.GetValueOrDefault(6).canDat;
//            ws.Cell("K11").Value = deNghi;
//            // Đọc Tóm tắt thành tích đổ vào L11
//            // Đọc Tóm tắt thành tích đổ vào L11
//            string tomTatThanhTich = "";
//            try
//            {
//                // 🌟 BỔ SUNG: Kiểm tra phiên bản tĩnh ngay tại đây
//                string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
//                string tenBangTomTat = phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase)
//                    ? "TomTatThanhTichBaNhat_TanBinh"
//                    : "TomTatThanhTichBaNhat_CBCS";
//                using var cnTomTat = new SqliteConnection($"Data Source={csdl2}");
//                cnTomTat.Open();
//                // 🌟 SỬA ĐỊNH TUYẾN: Truy vấn vào đúng bảng
//                using var cmdTomTat = new SqliteCommand($"SELECT NoiDung FROM [{tenBangTomTat}] WHERE ID = 1", cnTomTat);
//                var objResult = cmdTomTat.ExecuteScalar();
//                if (objResult != null && objResult != DBNull.Value)
//                    tomTatThanhTich = Module_BaoMatAES.GiaiMa(objResult.ToString()).Trim();
//            }
//            catch { }
//            ws.Cell("L11").Value = tomTatThanhTich;
//            // Thiết lập Công thức tính toán dữ liệu
//            ws.Cell("E12").FormulaA1 = "=IFERROR((E11*100)/F11,\"Dữ liệu sai\")";
//            ws.Cell("F12").FormulaA1 = "=IFERROR(IF(B11=0,0,(F11*100)/B11),\"Dữ liệu sai\")";
//            ws.Cell("G12").FormulaA1 = "=IFERROR(100-F12,\"Dữ liệu sai\")";
//            ws.Cell("H12").FormulaA1 = "=IFERROR(IF(OR(H11=\"\",H11=0),\"0%\",(H11*100)/B11 & \"%\"),\"Dữ liệu sai\")";
//            ws.Cell("I12").FormulaA1 = "=IFERROR(IF(OR(I11=\"\",I11=0),\"0%\",(I11*100)/B11 & \"%\"),\"Dữ liệu sai\")";
//            ws.Cell("E12").Style.NumberFormat.Format = "00.00\"%\"";
//            ws.Cell("F12").Style.NumberFormat.Format = "00.00\"%\"";
//            ws.Cell("G12").Style.NumberFormat.Format = "00.00\"%\"";
//            ws.Cell("H12").Style.NumberFormat.Format = "00.00\"%\"";
//            ws.Cell("I12").Style.NumberFormat.Format = "00.00\"%\"";
//            // ===== 9. KÝ TÊN =====
//            VietKyTenBaoCaoTongHop(ws, csdl2);
//            // ===== 10. ÉP TÁCH SHEET VÀ ĐƯA CON TRỎ VỀ "BC_BANHAT" =====
//            try
//            {
//                // 1. Quét toàn bộ và ép hủy chọn tất cả các sheet (Gỡ Group)
//                foreach (var sheet in wb.Worksheets)
//                {
//                    sheet.SetTabSelected(false);
//                }
//                // 2. Chuyển sang sheet Báo Cáo
//                if (wb.Worksheets.TryGetWorksheet("BC_BANHAT", out var wsBaoCao))
//                {
//                    // 3. Đặt nó làm sheet đang mở và chọn duy nhất nó
//                    wsBaoCao.SetTabActive();
//                    wsBaoCao.SetTabSelected(true);
//                    // 4. Bôi đen vùng A1:G1 và đặt con trỏ nhấp nháy vào ô A1
//                    wsBaoCao.Range("A1:G1").Select();
//                    wsBaoCao.Cell("A1").SetActive();
//                }
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine($"Lỗi định vị con trỏ Excel: {ex.Message}");
//            }
//            // ===== 11. LƯU FILE =====
//            wb.Save();
//        }
//        catch (Exception ex)
//        {
//            Module_ThongBao.DangXuLy("Lỗi xuất báo cáo tổng hợp: " + ex.Message);
//        }
//    }
//    private static void VietKyTenBaoCaoTongHop(IXLWorksheet ws, string fileDB)
//    {
//        string hoTenKy = "     ";
//        try
//        {
//            using var conn = new SqliteConnection($"Data Source={fileDB}");
//            conn.Open();
//            using var cmd = new SqliteCommand("SELECT ChiHuyD FROM ThongTin WHERE ID = 1", conn);
//            using var reader = cmd.ExecuteReader();
//            if (reader.Read())
//            {
//                string chiHuyRaw = reader["ChiHuyD"]?.ToString()?.Trim() ?? "";
//                if (!string.IsNullOrWhiteSpace(chiHuyRaw))
//                {
//                    string decoded = Module_BaoMatAES.GiaiMa(chiHuyRaw);
//                    hoTenKy = string.IsNullOrEmpty(decoded) ? chiHuyRaw : decoded.Trim();
//                }
//            }
//        }
//        catch { }
//        string chucVuTieuDoanTruong = "";
//        string chucVuNguoiKy = "";
//        int idDauTien = -1, idNguoiKy = -1;
//        using (var cn = new SqliteConnection("Data Source=" + fileDB))
//        {
//            cn.Open();
//            using var cmd = new SqliteCommand("SELECT ID, HoVaTen, ChucVu FROM ChiHuyD ORDER BY ID ASC", cn);
//            using var rd = cmd.ExecuteReader();
//            bool isFirst = true, foundMatch = false;
//            while (rd.Read())
//            {
//                int id = Convert.ToInt32(rd["ID"]);
//                string hoTenRaw = rd["HoVaTen"]?.ToString()?.Trim() ?? "";
//                string chucVuRaw = rd["ChucVu"]?.ToString()?.Trim() ?? "";
//                string htDec = Module_BaoMatAES.GiaiMa(hoTenRaw);
//                if (string.IsNullOrEmpty(htDec)) htDec = hoTenRaw;
//                string cvDec = Module_BaoMatAES.GiaiMa(chucVuRaw);
//                if (string.IsNullOrEmpty(cvDec)) cvDec = chucVuRaw;
//                if (isFirst)
//                {
//                    idDauTien = id; chucVuTieuDoanTruong = cvDec; isFirst = false;
//                }
//                if (htDec.Equals(hoTenKy, StringComparison.OrdinalIgnoreCase))
//                {
//                    idNguoiKy = id; chucVuNguoiKy = cvDec; foundMatch = true;
//                    break;
//                }
//            }
//            if (!foundMatch) idNguoiKy = idDauTien;
//        }
//        if (idNguoiKy == idDauTien)
//        {
//            ws.Cell("L14").Value = chucVuTieuDoanTruong.ToUpper();
//            ws.Cell("L15").Value = "";
//        }
//        else
//        {
//            ws.Cell("L14").Value = ("KT. " + chucVuTieuDoanTruong).ToUpper();
//            ws.Cell("L15").Value = chucVuNguoiKy.ToUpper();
//        }
//        ws.Cell("L17").Value = hoTenKy;
//        foreach (string addr in new[] { "L14", "L15", "L17" })
//        {
//            var c = ws.Cell(addr);
//            c.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman; c.Style.Font.FontSize = 14;
//            c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//            c.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//            if (addr != "L17") c.Style.Font.Bold = true;
//        }
//    }
//    public static async Task TinhToanVaHienThiTyLeBaNhatAsync(ToolStripStatusLabel labelTyLe)
//    {
//        // BẢO VỆ TẦNG 1: Tránh lỗi NullReference hoặc control đã bị hủy ngay từ đầu
//        if (labelTyLe == null || labelTyLe.IsDisposed) return;
//        string dbPath = Module_DanduongGPS.DuongDanCSDL2;
//        if (!System.IO.File.Exists(dbPath)) return;
//        int tyLe = 0;
//        int tongSoLoai1 = 0;
//        int soDangDeNghi = 0;
//        try
//        {
//            await Task.Run(() =>
//            {
//                // BẢO VỆ TẦNG 2: Mở kết nối ĐỘC QUYỀN CHỈ ĐỌC (ReadOnly). 
//                // Triệt tiêu 100% lỗi "Database is locked" khi các Form khác đang thao tác Ghi/Lưu.
//                var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
//                {
//                    DataSource = dbPath,
//                    Mode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadOnly
//                };
//                using var conn = new Microsoft.Data.Sqlite.SqliteConnection(builder.ConnectionString);
//                conn.Open();
//                // 1. Lấy và Giải mã % từ bảng QuyDinhTyLe_BaNhat
//                using (var cmdTyLe = conn.CreateCommand())
//                {
//                    cmdTyLe.CommandText = "SELECT TyLe FROM QuyDinhTyLe_BaNhat WHERE ID = 1";
//                    var result = cmdTyLe.ExecuteScalar();
//                    if (result != null && result != DBNull.Value)
//                    {
//                        string tyLeMaHoa = result.ToString();
//                        if (!string.IsNullOrWhiteSpace(tyLeMaHoa))
//                        {
//                            try
//                            {
//                                string tyLeGoc = Module_BaoMatAES.GiaiMa(tyLeMaHoa);
//                                int.TryParse(tyLeGoc, out tyLe);
//                            }
//                            catch { tyLe = 0; }
//                        }
//                    }
//                }
//                // 2. Đếm số lượng "Loại 1" trong bảng DanhSach
//                using (var cmdDemLoai1 = conn.CreateCommand())
//                {
//                    cmdDemLoai1.CommandText = "SELECT PhanLoai FROM DanhSach";
//                    using var reader = cmdDemLoai1.ExecuteReader();
//                    while (reader.Read())
//                    {
//                        string phanLoaiMaHoa = reader["PhanLoai"]?.ToString() ?? "";
//                        if (!string.IsNullOrWhiteSpace(phanLoaiMaHoa))
//                        {
//                            try
//                            {
//                                string phanLoaiGoc = Module_BaoMatAES.GiaiMa(phanLoaiMaHoa).Trim();
//                                if (phanLoaiGoc.Equals("Loại 1", StringComparison.OrdinalIgnoreCase))
//                                {
//                                    tongSoLoai1++;
//                                }
//                            }
//                            catch { /* Bỏ qua an toàn */ }
//                        }
//                    }
//                }
//                // 3. Đếm số lượng đang được Đề nghị (Có chữ "X")
//                try
//                {
//                    string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
//                    string tenBangBaNhat = phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase)
//                                         ? "DanhSachBaNhat_TanBinh"
//                                         : "DanhSachBaNhat";
//                    // BẢO VỆ TẦNG 3: Kiểm tra cấu trúc CSDL trực tiếp (Kiểm tra bảng tồn tại).
//                    // Giúp RAM không bị hao hụt bởi việc quăng Exception vô ích nếu bảng chưa được tạo.
//                    using var cmdCheckTable = conn.CreateCommand();
//                    cmdCheckTable.CommandText = "SELECT count(name) FROM sqlite_master WHERE type='table' AND name=@tableName";
//                    cmdCheckTable.Parameters.AddWithValue("@tableName", tenBangBaNhat);
//                    if (Convert.ToInt64(cmdCheckTable.ExecuteScalar()) > 0)
//                    {
//                        using var cmdDemDeNghi = conn.CreateCommand();
//                        cmdDemDeNghi.CommandText = $"SELECT DeNghi FROM [{tenBangBaNhat}]";
//                        using var readerDN = cmdDemDeNghi.ExecuteReader();
//                        while (readerDN.Read())
//                        {
//                            string deNghiMaHoa = readerDN["DeNghi"]?.ToString() ?? "";
//                            if (!string.IsNullOrWhiteSpace(deNghiMaHoa))
//                            {
//                                try
//                                {
//                                    string deNghiGoc = Module_BaoMatAES.GiaiMa(deNghiMaHoa).Trim();
//                                    if (deNghiGoc.Equals("X", StringComparison.OrdinalIgnoreCase))
//                                    {
//                                        soDangDeNghi++;
//                                    }
//                                }
//                                catch { /* Bỏ qua an toàn */ }
//                            }
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    System.Diagnostics.Debug.WriteLine($"Lỗi truy xuất cấu trúc: {ex.Message}");
//                }
//            });
//            // BẢO VỆ TẦNG 4: Quét lại vòng đời UI. 
//            // Nếu người dùng đóng Form ngay lúc Task.Run đang chạy, bỏ qua ngay để không gây Crash.
//            if (labelTyLe.IsDisposed) return;
//            // 4. Tính toán số lượng chỉ tiêu
//            int soLuongChiTieu = (int)Math.Round(tongSoLoai1 * (tyLe / 100.0), MidpointRounding.AwayFromZero);
//            double tyLeHienTai = tongSoLoai1 > 0 ? Math.Round((double)soDangDeNghi / tongSoLoai1 * 100, 1) : 0;
//            // 5. Xây dựng logic chuỗi
//            string trangThai;
//            if (soDangDeNghi == soLuongChiTieu)
//            {
//                trangThai = "Đạt tỷ lệ";
//            }
//            else if (soDangDeNghi < soLuongChiTieu)
//            {
//                trangThai = $"Đang thiếu: {soLuongChiTieu - soDangDeNghi} {Module_HeThong.Tu_dong_chi} ";
//            }
//            else
//            {
//                trangThai = $"Đang thừa: {soDangDeNghi - soLuongChiTieu} {Module_HeThong.Tu_dong_chi}";
//            }
//            string textHienThi = $"           Chỉ tiêu “Ba nhất” {tyLe}% = {soLuongChiTieu} {Module_HeThong.Tu_dong_chi}    |    Tỷ lệ hiện tại {soDangDeNghi} {Module_HeThong.Tu_dong_chi} = {tyLeHienTai}%    |    {trangThai}";
//            // 6. Đẩy kết quả lên UI
//            // BẢO VỆ TẦNG 5: Sử dụng GetCurrentParent và BeginInvoke để chống treo luồng cục bộ (Cross-thread Deadlock).
//            var parentControl = labelTyLe.GetCurrentParent();
//            if (parentControl != null && parentControl.InvokeRequired)
//            {
//                parentControl.BeginInvoke(new Action(() =>
//                {
//                    if (!labelTyLe.IsDisposed)
//                    {
//                        labelTyLe.Text = textHienThi;
//                        // ⭐ BỔ SUNG: Định dạng màu chữ (Đạt = Xanh đậm, Ngược lại = Đỏ)
//                        labelTyLe.ForeColor = (soDangDeNghi == soLuongChiTieu) ? System.Drawing.Color.DarkGreen : System.Drawing.Color.Red;
//                    }
//                }));
//            }
//            else if (!labelTyLe.IsDisposed)
//            {
//                labelTyLe.Text = textHienThi;
//                // ⭐ BỔ SUNG: Định dạng màu chữ (Đạt = Xanh đậm, Ngược lại = Đỏ)
//                labelTyLe.ForeColor = (soDangDeNghi == soLuongChiTieu) ? System.Drawing.Color.DarkGreen : System.Drawing.Color.Red;
//            }
//        }
//        catch (Exception ex)
//        {
//            System.Diagnostics.Debug.WriteLine($"Lỗi tính toán tỷ lệ Ba Nhất: {ex.Message}");
//        }
//    }
//    public static async Task CapNhatTinhTrangSoVangAsync()
//    {
//        // BẢO VỆ TẦNG 1: Kiểm tra đường dẫn CSDL
//        string dbPath = Module_DanduongGPS.DuongDanCSDL2;
//        if (string.IsNullOrWhiteSpace(dbPath) || !System.IO.File.Exists(dbPath)) return;
//        try
//        {
//            // BẢO VỆ TẦNG 2: Đẩy toàn bộ tác vụ nặng xuống luồng nền (Background Thread)
//            await Task.Run(async () =>
//            {
//                // Tự động nhận diện phiên bản đang sử dụng để trỏ đúng bảng
//                string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
//                string bangDanhSach = phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase)
//                    ? "DanhSachBaNhat_TanBinh"
//                    : "DanhSachBaNhat";
//                string bangSoVang = phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase)
//                    ? "DanhSachTanBinh_SoVangBaNhat"
//                    : "DanhSach_SoVangBaNhat";
//                using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
//                await conn.OpenAsync();
//                // 1. Quét bảng Sổ Vàng để lấy danh sách Số Hiệu
//                HashSet<string> tapHopSoHieuDaVaoSo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//                // BẢO VỆ TẦNG 3: Kiểm tra bảng Sổ Vàng có tồn tại không trước khi Query
//                using (var cmdCheckSV = conn.CreateCommand())
//                {
//                    cmdCheckSV.CommandText = "SELECT count(name) FROM sqlite_master WHERE type='table' AND name=@tableName";
//                    cmdCheckSV.Parameters.AddWithValue("@tableName", bangSoVang);
//                    if (Convert.ToInt64(await cmdCheckSV.ExecuteScalarAsync()) > 0)
//                    {
//                        using var cmdSV = conn.CreateCommand();
//                        cmdSV.CommandText = $"SELECT SoHieu FROM [{bangSoVang}]";
//                        using var readerSV = await cmdSV.ExecuteReaderAsync();
//                        while (await readerSV.ReadAsync())
//                        {
//                            string shEnc = readerSV["SoHieu"]?.ToString() ?? "";
//                            if (!string.IsNullOrWhiteSpace(shEnc))
//                            {
//                                try
//                                {
//                                    tapHopSoHieuDaVaoSo.Add(Module_BaoMatAES.GiaiMa(shEnc).Trim());
//                                }
//                                catch { /* Bỏ qua dòng bị lỗi giải mã */ }
//                            }
//                        }
//                    }
//                }
//                // 2. Đối chiếu với Bảng Danh Sách Ba Nhất
//                var danhSachCanCapNhat = new List<(int ID, string TinhTrangMoi)>();
//                // Khởi tạo Transaction bảo vệ DB
//                using var transaction = conn.BeginTransaction();
//                using (var cmdDS = conn.CreateCommand())
//                {
//                    cmdDS.Transaction = transaction;
//                    // Kiểm tra bảng Danh Sách có tồn tại không
//                    cmdDS.CommandText = "SELECT count(name) FROM sqlite_master WHERE type='table' AND name=@tableNameDS";
//                    cmdDS.Parameters.AddWithValue("@tableNameDS", bangDanhSach);
//                    if (Convert.ToInt64(await cmdDS.ExecuteScalarAsync()) > 0)
//                    {
//                        cmdDS.CommandText = $"SELECT ID, SoHieu FROM [{bangDanhSach}]";
//                        using var readerDS = await cmdDS.ExecuteReaderAsync();
//                        while (await readerDS.ReadAsync())
//                        {
//                            int id = Convert.ToInt32(readerDS["ID"]);
//                            if (id == -1) continue; // Bỏ qua dòng đệm mồi của DB
//                            string shEnc = readerDS["SoHieu"]?.ToString() ?? "";
//                            string shDec = string.IsNullOrWhiteSpace(shEnc) ? "" : Module_BaoMatAES.GiaiMa(shEnc).Trim();
//                            // NẾU Số hiệu có trong Hash Sổ Vàng -> "Đã đề nghị"
//                            string tinhTrangMoi = (!string.IsNullOrEmpty(shDec) && tapHopSoHieuDaVaoSo.Contains(shDec))
//                                                ? "Đã đề nghị"
//                                                : "";
//                            //Chưa đề nghị
//                            danhSachCanCapNhat.Add((id, Module_BaoMatAES.MaHoa(tinhTrangMoi)));
//                        }
//                    }
//                }
//                // 3. Thực thi UPDATE đồng loạt vào Cơ sở dữ liệu
//                if (danhSachCanCapNhat.Count > 0)
//                {
//                    using var cmdUpdate = conn.CreateCommand();
//                    cmdUpdate.Transaction = transaction;
//                    cmdUpdate.CommandText = $"UPDATE [{bangDanhSach}] SET TinhTrang = @tt WHERE ID = @id";
//                    var pTT = cmdUpdate.Parameters.Add("@tt", Microsoft.Data.Sqlite.SqliteType.Text);
//                    var pID = cmdUpdate.Parameters.Add("@id", Microsoft.Data.Sqlite.SqliteType.Integer);
//                    foreach (var item in danhSachCanCapNhat)
//                    {
//                        pTT.Value = item.TinhTrangMoi;
//                        pID.Value = item.ID;
//                        await cmdUpdate.ExecuteNonQueryAsync();
//                    }
//                }
//                // Chốt hạ dữ liệu an toàn
//                await transaction.CommitAsync();
//            });
//        }
//        catch (Exception ex)
//        {
//            System.Diagnostics.Debug.WriteLine("Lỗi tính toán tình trạng sổ vàng: " + ex.Message);
//        }
//    }
//    // 1. HÀM XUẤT DANH SÁCH GỐC (Form 42 gọi)
//    public static async Task XuatDuLieuRaTepExcelTuCSDL_ToiUu(List<DanhSachGocBaNhatDTO> danhSach, string filePathLuu)
//    {
//        try
//        {
//            await Task.Run(() =>
//            {
//                using (var wb = new XLWorkbook())
//                {
//                    var ws = wb.Worksheets.Add("DS_BANHAT_CSDL");
//                    string[] headers = { "STT", "Họ và tên", "Số hiệu", "Năm sinh", "Quê quán", "Ngày vào CAND", "Cấp bậc", "Chức vụ", "Đơn vị", "Phân loại", "Ghi chú", "Đề nghị", "Thành tích" };
//                    ws.Row(1).Height = 36;
//                    for (int i = 0; i < headers.Length; i++)
//                    {
//                        var cell = ws.Cell(1, i + 1);
//                        cell.Value = headers[i];
//                        cell.Style.Font.Bold = true;
//                        cell.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
//                        cell.Style.Font.FontSize = 13;
//                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//                        cell.Style.Alignment.WrapText = true;
//                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EBF1F5");
//                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
//                        cell.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
//                    }
//                    double[] widths = { 6, 25, 12, 10, 25, 12, 14, 15, 15, 12, 20, 10, 35 };
//                    for (int i = 0; i < widths.Length; i++) ws.Column(i + 1).Width = widths[i];
//                    int startRow = 2;
//                    int stt = 1;
//                    foreach (var item in danhSach)
//                    {
//                        ws.Cell(startRow, 1).Value = stt++;
//                        ws.Cell(startRow, 2).Value = item.HoVaTen;
//                        ws.Cell(startRow, 3).Value = item.SoHieu;
//                        ws.Cell(startRow, 4).Value = item.NamSinh;
//                        ws.Cell(startRow, 5).Value = item.QueQuan;
//                        ws.Cell(startRow, 6).Value = item.NgayVaoCAND;
//                        ws.Cell(startRow, 7).Value = item.CapBac;
//                        ws.Cell(startRow, 8).Value = item.ChucVu;
//                        ws.Cell(startRow, 9).Value = item.DonVi;
//                        ws.Cell(startRow, 10).Value = item.PhanLoai;
//                        ws.Cell(startRow, 11).Value = item.GhiChu;
//                        ws.Cell(startRow, 12).Value = item.DeNghi;
//                        ws.Cell(startRow, 13).Value = item.ThanhTich;
//                        ws.Cell(startRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        ws.Cell(startRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
//                        ws.Cell(startRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        ws.Cell(startRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        ws.Cell(startRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
//                        ws.Cell(startRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        ws.Cell(startRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        ws.Cell(startRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        ws.Cell(startRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        ws.Cell(startRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        ws.Cell(startRow, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
//                        ws.Cell(startRow, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        ws.Cell(startRow, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
//                        ws.Cell(startRow, 11).Style.Alignment.WrapText = true;
//                        ws.Cell(startRow, 13).Style.Alignment.WrapText = true;
//                        var range = ws.Range(startRow, 1, startRow, 13);
//                        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
//                        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
//                        range.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
//                        range.Style.Font.FontSize = 13;
//                        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//                        ws.Row(startRow).Height = 36;
//                        startRow++;
//                    }
//                    Module_BanQuyen.DongDauExcel(wb);
//                    wb.SaveAs(filePathLuu);
//                }
//            });
//            Module_NhatKy.GhiNhatKy(
//                taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
//                hanhDong: "Xuất tệp excel toàn bộ CSDL gốc Ba Nhất",
//                ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
//            );
//            if (File.Exists(filePathLuu))
//            {
//                Module_BaNhat.MoTepExcelThiDuaPhongTrao3Nhat(filePathLuu);
//            }
//        }
//        catch (Exception ex)
//        {
//            MessageBox.Show("Lỗi khi xuất file: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
//        }
//    }
//    // 2. HÀM XUẤT DANH SÁCH ĐỀ NGHỊ CÓ BẢNG TỶ LỆ (Form 42 gọi)
//    public static async Task XuatDanhSachBaNhatToExcelAsync(Form parentForm, List<DeNghiBaNhatDTO> danhSach)
//    {
//        string templatePath = Module_DanduongGPS.DuongDanCSDL4ex;
//        string dbPath = Module_DanduongGPS.DuongDanCSDL2; // Dùng biến toàn cục thay cho _csdl2Path
//        if (!File.Exists(templatePath))
//        {
//            MessageBox.Show("Không tìm thấy file mẫu Excel tại: " + templatePath, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
//            return;
//        }
//        string kyHieuTieuDoan = "D2";
//        try
//        {
//            using (var conn = new SqliteConnection($"Data Source={dbPath}"))
//            {
//                await conn.OpenAsync();
//                using var cmdKyHieu = conn.CreateCommand();
//                cmdKyHieu.CommandText = "SELECT KyHieu_TieuDoan FROM KyHieu_DonVi WHERE ID = 1";
//                using var reader = await cmdKyHieu.ExecuteReaderAsync();
//                if (await reader.ReadAsync())
//                {
//                    string giaiMaTD = Module_BaoMatAES.GiaiMa(reader["KyHieu_TieuDoan"]?.ToString() ?? "");
//                    if (!string.IsNullOrWhiteSpace(giaiMaTD)) kyHieuTieuDoan = giaiMaTD;
//                }
//            }
//        }
//        catch (Exception ex) { Debug.WriteLine($"Lỗi đọc Ký hiệu: {ex.Message}"); }
//        using var sfd = new SaveFileDialog
//        {
//            Filter = "Excel Files|*.xlsx",
//            FileName = $"BÁO CÁO BIỂU DƯƠNG PHONG TRÀO BA NHẤT CỦA {kyHieuTieuDoan} {DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
//            Title = "Chọn nơi lưu file Excel"
//        };
//        if (sfd.ShowDialog() != DialogResult.OK) return;
//        string filePathLuu = sfd.FileName;
//        Form_Loading frmLoad = new Form_Loading("Đang tạo tệp excel, vui lòng đợi...");
//        bool isLoadShown = false;
//        // Gán icon theo Form cha nếu có
//        if (parentForm != null) frmLoad.Icon = parentForm.Icon;
//        try
//        {
//            // Khóa Form cha truyền vào thay vì dùng 'this'
//            if (parentForm != null) parentForm.Enabled = false;
//            frmLoad.Show(parentForm);
//            isLoadShown = true;
//            await Task.Delay(50);
//            await Task.Run(() =>
//            {
//                using (var wb = new XLWorkbook(templatePath))
//                {
//                    var danhSachSheetXoa = new List<string>();
//                    foreach (var worksheet in wb.Worksheets)
//                    {
//                        if (!worksheet.Name.Equals("BC_BANHAT", StringComparison.OrdinalIgnoreCase) &&
//                            !worksheet.Name.Equals("DS_BANHAT", StringComparison.OrdinalIgnoreCase))
//                            danhSachSheetXoa.Add(worksheet.Name);
//                    }
//                    foreach (var sheetName in danhSachSheetXoa) wb.Worksheets.Delete(sheetName);
//                    var ws = wb.Worksheet("DS_BANHAT");
//                    string dong1 = "", donviCapTrung = "", donviCapTieu = "";
//                    try
//                    {
//                        using (var conn = new SqliteConnection($"Data Source={dbPath}"))
//                        {
//                            conn.Open();
//                            using var cmd = conn.CreateCommand();
//                            cmd.CommandText = "SELECT textBox1_TenTrungDoanDong1, TenTrungDoan, TenTieuDoan FROM ThongTin WHERE ID = 1";
//                            using var rd = cmd.ExecuteReader();
//                            if (rd.Read())
//                            {
//                                dong1 = Module_BaoMatAES.GiaiMa(rd[0].ToString());
//                                donviCapTrung = Module_BaoMatAES.GiaiMa(rd[1].ToString());
//                                donviCapTieu = Module_BaoMatAES.GiaiMa(rd[2].ToString());
//                            }
//                        }
//                    }
//                    catch { }
//                    void GanGachChan1Phan3(IXLCell cell, string text)
//                    {
//                        if (cell == null || string.IsNullOrWhiteSpace(text)) return;
//                        cell.Value = text;
//                        int totalLen = text.Length;
//                        int underlineLen = (int)Math.Ceiling(totalLen / 2.0);
//                        int start = (totalLen - underlineLen) / 2;
//                        cell.GetRichText().Substring(start, underlineLen).SetUnderline();
//                    }
//                    var cellA1 = ws.Cell("A1");
//                    cellA1.Value = string.IsNullOrWhiteSpace(dong1) ? "      " : dong1;
//                    cellA1.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman; cellA1.Style.Font.FontSize = 13;
//                    cellA1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                    if (!string.IsNullOrWhiteSpace(donviCapTrung))
//                    {
//                        var cellA2 = ws.Cell("A2"); cellA2.Value = donviCapTrung;
//                        cellA2.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman; cellA2.Style.Font.FontSize = 13;
//                        cellA2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        var cellA3 = ws.Cell("A3"); cellA3.Value = donviCapTieu;
//                        cellA3.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman; cellA3.Style.Font.FontSize = 13; cellA3.Style.Font.Bold = true;
//                        cellA3.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        GanGachChan1Phan3(cellA3, donviCapTieu);
//                    }
//                    else
//                    {
//                        var cellA2 = ws.Cell("A2"); cellA2.Value = donviCapTieu;
//                        cellA2.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman; cellA2.Style.Font.FontSize = 13; cellA2.Style.Font.Bold = true;
//                        cellA2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        GanGachChan1Phan3(cellA2, donviCapTieu);
//                        ws.Cell("A3").Value = "";
//                    }
//                    string thang = "", nam = "", ngay = "", diaDiem = "", kyHieuBaoCao = "...............";
//                    try
//                    {
//                        using var conn = new SqliteConnection($"Data Source={dbPath}");
//                        conn.Open();
//                        using var cmd = conn.CreateCommand();
//                        cmd.CommandText = "SELECT Thang, Nam, Ngay, DiaDiem, KyHieuBaoCao FROM ThongTin WHERE ID = 1";
//                        using var rd = cmd.ExecuteReader();
//                        if (rd.Read())
//                        {
//                            thang = Module_BaoMatAES.GiaiMa(rd["Thang"]?.ToString() ?? "").Trim();
//                            nam = Module_BaoMatAES.GiaiMa(rd["Nam"]?.ToString() ?? "").Trim();
//                            ngay = Module_BaoMatAES.GiaiMa(rd["Ngay"]?.ToString() ?? "").Trim();
//                            diaDiem = Module_BaoMatAES.GiaiMa(rd["DiaDiem"]?.ToString() ?? "").Trim();
//                            kyHieuBaoCao = Module_BaoMatAES.GiaiMa(rd["KyHieuBaoCao"]?.ToString() ?? "");
//                        }
//                    }
//                    catch { }
//                    ws.Range("F3:J3").Clear(XLClearOptions.Contents);
//                    string tieuDeNgay = !string.IsNullOrWhiteSpace(diaDiem) ? $"{diaDiem}, ngày {ngay} tháng {thang} năm {nam}" : $"(ngày {ngay} tháng {thang} năm {nam})";
//                    var rangeF3 = ws.Range("F3:J3");
//                    rangeF3.Merge();
//                    var cellF3 = ws.Cell("F3");
//                    cellF3.Value = tieuDeNgay;
//                    cellF3.Style.Font.Italic = true;
//                    cellF3.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
//                    cellF3.Style.Font.FontSize = 13;
//                    cellF3.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                    cellF3.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//                    ws.Range("A9:J500000").Clear(XLClearOptions.All);
//                    ws.Range("A5:J5").Merge().Value = $"CBCS ĐỀ NGHỊ BIỂU DƯƠNG GƯƠNG ĐIỂN HÌNH TRONG THỰC HIỆN PHONG TRÀO THI ĐUA BA NHẤT THÁNG {thang}/{nam}";
//                    ws.Range("A5:J5").Style.Font.Bold = true; ws.Range("A5:J5").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                    var rangeA6 = ws.Range("A6:J6");
//                    rangeA6.Merge();
//                    // Ở trong Module gọi về hàm tĩnh trong Form42 để dùng hoặc viết lại Logic (đã viết lại inline cho an toàn độc lập)
//                    string donViHienThi = string.IsNullOrWhiteSpace(donviCapTieu) ? "" : char.ToUpper(donviCapTieu.Trim().ToLower()[0]) + donviCapTieu.Trim().ToLower().Substring(1);
//                    string textA6 = $"(Kèm theo Báo cáo số:                 {kyHieuBaoCao}, ngày {ngay}/{thang}/{nam} của {donViHienThi})";
//                    var cellA6 = ws.Cell("A6");
//                    cellA6.Value = textA6;
//                    cellA6.Style.Font.Italic = true;
//                    cellA6.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                    int start = textA6.IndexOf("cáo");
//                    int end = textA6.IndexOf("của");
//                    if (start >= 0 && end > start)
//                    {
//                        cellA6.GetRichText().Substring(start, end - start + 4).SetUnderline();
//                    }
//                    int startRow = 9;
//                    int stt = 1;
//                    foreach (var item in danhSach)
//                    {
//                        ws.Cell(startRow, 1).Value = stt++;
//                        ws.Cell(startRow, 2).Value = item.HoVaTen;
//                        ws.Cell(startRow, 3).Value = item.SoHieu;
//                        ws.Cell(startRow, 4).Value = item.NamSinh;
//                        ws.Cell(startRow, 5).Value = item.NgayVaoCAND;
//                        ws.Cell(startRow, 6).Value = item.CapBac;
//                        ws.Cell(startRow, 7).Value = item.ChucVu;
//                        ws.Cell(startRow, 8).Value = item.DonVi;
//                        ws.Cell(startRow, 9).Value = item.PhanLoai;
//                        ws.Cell(startRow, 10).Value = item.ThanhTich;
//                        ws.Cell(startRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        ws.Cell(startRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
//                        for (int c = 3; c <= 9; c++) ws.Cell(startRow, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        ws.Cell(startRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
//                        ws.Cell(startRow, 10).Style.Alignment.WrapText = true;
//                        var range = ws.Range(startRow, 1, startRow, 10);
//                        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
//                        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
//                        range.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
//                        range.Style.Font.FontSize = 13;
//                        startRow++;
//                    }
//                    for (int i = 9; i < startRow; i++) ws.Row(i).ClearHeight();
//                    int tong = stt - 1;
//                    string tongStr = tong.ToString("00");
//                    var rngTong = ws.Range(startRow, 1, startRow, 2);
//                    rngTong.Merge();
//                    rngTong.Value = $"Tổng cộng: {tongStr} đồng chí./.";
//                    rngTong.Style.Font.Bold = true;
//                    rngTong.Style.Font.Italic = true;
//                    rngTong.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
//                    rngTong.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//                    int rowTongCong = startRow;
//                    int dongKy = rowTongCong + 1;
//                    string hoTenKy = "";
//                    string chucVuTieuDoanTruong = "";
//                    int dongDauTienID = -1;
//                    string chucVuNguoiKy = "";
//                    int dongNguoiKyID = -1;
//                    using (var conn2 = new SqliteConnection($"Data Source={dbPath}"))
//                    {
//                        conn2.Open();
//                        try
//                        {
//                            using var cmdTT = conn2.CreateCommand();
//                            cmdTT.CommandText = "SELECT ChiHuyD FROM ThongTin WHERE ID = 1";
//                            var result = cmdTT.ExecuteScalar();
//                            if (result != null && result != DBNull.Value)
//                                hoTenKy = Module_BaoMatAES.GiaiMa(result.ToString()).Trim();
//                        }
//                        catch { }
//                        try
//                        {
//                            using var cmdCH = conn2.CreateCommand();
//                            cmdCH.CommandText = "SELECT ID, HoVaTen, ChucVu FROM ChiHuyD ORDER BY ID ASC";
//                            using var rd = cmdCH.ExecuteReader();
//                            bool isFirst = true;
//                            bool foundMatch = false;
//                            while (rd.Read())
//                            {
//                                int id = Convert.ToInt32(rd["ID"]);
//                                string htRaw = rd["HoVaTen"]?.ToString() ?? "";
//                                string cvRaw = rd["ChucVu"]?.ToString() ?? "";
//                                string htDec = Module_BaoMatAES.GiaiMa(htRaw).Trim();
//                                if (string.IsNullOrEmpty(htDec)) htDec = htRaw.Trim();
//                                string cvDec = Module_BaoMatAES.GiaiMa(cvRaw).Trim();
//                                if (string.IsNullOrEmpty(cvDec)) cvDec = cvRaw.Trim();
//                                if (isFirst) { dongDauTienID = id; chucVuTieuDoanTruong = cvDec; isFirst = false; }
//                                if (htDec.Equals(hoTenKy, StringComparison.OrdinalIgnoreCase))
//                                {
//                                    dongNguoiKyID = id; chucVuNguoiKy = cvDec; foundMatch = true;
//                                }
//                            }
//                            if (!foundMatch) { dongNguoiKyID = dongDauTienID; chucVuNguoiKy = chucVuTieuDoanTruong; }
//                        }
//                        catch { }
//                    }
//                    void GhiDongKy(string text)
//                    {
//                        var rangeKy = ws.Range(dongKy, 6, dongKy, 10);
//                        rangeKy.Merge();
//                        var c = ws.Cell(dongKy, 6);
//                        c.Value = text;
//                        c.Style.Font.Bold = true;
//                        c.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
//                        c.Style.Font.FontSize = 14;
//                        c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        c.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//                        dongKy++;
//                    }
//                    if (dongNguoiKyID == dongDauTienID)
//                    {
//                        GhiDongKy(chucVuTieuDoanTruong.ToUpper());
//                    }
//                    else
//                    {
//                        GhiDongKy("KT. " + chucVuTieuDoanTruong.ToUpper());
//                        GhiDongKy(chucVuNguoiKy.ToUpper());
//                    }
//                    dongKy += 4;
//                    GhiDongKy(hoTenKy);
//                    Module_BanQuyen.DongDauExcel(wb);
//                    try
//                    {
//                        var wsBaoCao = wb.Worksheet("BC_BANHAT");
//                        foreach (var sheet in wb.Worksheets)
//                        {
//                            try { sheet.SetTabSelected(false); } catch { }
//                        }
//                        wsBaoCao.SetTabActive();
//                        wsBaoCao.SetTabSelected(true);
//                        wsBaoCao.Cell("A1").SetActive();
//                        wsBaoCao.Range("A1").Select();
//                    }
//                    catch { }
//                    wb.SaveAs(filePathLuu);
//                }
//            });
//            Module_NhatKy.GhiNhatKy(
//                taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
//                hanhDong: "Xuất tệp excel báo cáo thi đua Ba Nhất",
//                ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
//            );
//            // Xuất tiếp báo cáo tổng hợp
//            XuatBaoCaoTongHop(filePathLuu);
//            if (File.Exists(filePathLuu))
//            {
//                MoTepExcelThiDuaPhongTrao3Nhat(filePathLuu);
//            }
//        }
//        catch (Exception ex)
//        {
//            MessageBox.Show("Lỗi xuất file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
//        }
//        finally
//        {
//            if (isLoadShown)
//            {
//                frmLoad.Close();
//                if (parentForm != null)
//                {
//                    parentForm.Enabled = true;
//                    parentForm.Focus();
//                }
//            }
//        }
//    }
//    // HÀM NHẬP DỮ LIỆU TỪ EXCEL GỐC (Form 42 gọi)
//    public static async Task<int> NhapDuLieuTuTepExcelVaoCSDL(Form parentForm, string excelPath, string dbPath)
//    {
//        int soDongCapNhatThanhCong = 0;
//        try
//        {
//            if (!File.Exists(excelPath) || !File.Exists(dbPath)) return 0;
//            // Tự động nhận diện tên bảng theo phiên bản
//            string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
//            string tenBangDanhSachBaNhat = phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase)
//                ? "DanhSachBaNhat_TanBinh"
//                : "DanhSachBaNhat";
//            var dsKhongCoSoHieu = new List<string>();
//            var dsKhongPhaiLoai1 = new List<string>();
//            var dsChuaDongBo = new List<string>();
//            var dsLoiKyThuat = new List<string>();
//            // Chạy đa luồng Task.Run để không bị đơ UI khi đọc file Excel nặng
//            await Task.Run(() =>
//            {
//                using (var wb = new ClosedXML.Excel.XLWorkbook(excelPath))
//                {
//                    if (!wb.Worksheets.TryGetWorksheet("DS_BANHAT_CSDL", out var ws))
//                    {
//                        if (parentForm != null && !parentForm.IsDisposed)
//                        {
//                            parentForm.Invoke(new Action(() => MessageBox.Show(parentForm, "Tệp Excel không hợp lệ! Không tìm thấy Sheet 'DS_BANHAT_CSDL'.\nVui lòng chọn đúng tệp được xuất từ chức năng 'Xuất danh sách gốc'.", "Lỗi cấu trúc", MessageBoxButtons.OK, MessageBoxIcon.Error)));
//                        }
//                        return;
//                    }
//                    // 1. KIỂM TRA HEADER
//                    string[] mauHeaderChuan = { "STT", "Họ và tên", "Số hiệu", "Năm sinh", "Quê quán", "Ngày vào CAND", "Cấp bậc", "Chức vụ", "Đơn vị", "Phân loại", "Ghi chú", "Đề nghị", "Thành tích" };
//                    for (int i = 0; i < mauHeaderChuan.Length; i++)
//                    {
//                        string headerExcel = ws.Cell(1, i + 1).GetString().Trim();
//                        if (!headerExcel.Equals(mauHeaderChuan[i], StringComparison.OrdinalIgnoreCase))
//                        {
//                            if (parentForm != null && !parentForm.IsDisposed)
//                            {
//                                parentForm.Invoke(new Action(() => MessageBox.Show(parentForm, $"Cấu trúc tệp Excel bị sai lệch!\nTại cột số {i + 1} đang là '{headerExcel}', yêu cầu chuẩn phải là '{mauHeaderChuan[i]}'.\nVui lòng không tự ý thay đổi tiêu đề cột.", "Từ chối nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Error)));
//                            }
//                            return;
//                        }
//                    }
//                    int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
//                    if (lastRow < 2) return;
//                    using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
//                    {
//                        conn.Open();
//                        // ⭐ BƯỚC 1: Tải & Giải mã Bảng "DanhSach"
//                        var tuDienDanhSachGoc = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
//                        using (var cmdTaiGoc = conn.CreateCommand())
//                        {
//                            cmdTaiGoc.CommandText = "SELECT SoHieu, PhanLoai FROM DanhSach";
//                            using var rdGoc = cmdTaiGoc.ExecuteReader();
//                            while (rdGoc.Read())
//                            {
//                                string shGocMaHoa = rdGoc["SoHieu"]?.ToString() ?? "";
//                                string plGocMaHoa = rdGoc["PhanLoai"]?.ToString() ?? "";
//                                string shGocGiaiMa = string.IsNullOrWhiteSpace(shGocMaHoa) ? "" : Module_BaoMatAES.GiaiMa(shGocMaHoa).Trim();
//                                string plGocGiaiMa = string.IsNullOrWhiteSpace(plGocMaHoa) ? "" : Module_BaoMatAES.GiaiMa(plGocMaHoa).Trim();
//                                if (!string.IsNullOrWhiteSpace(shGocGiaiMa)) tuDienDanhSachGoc[shGocGiaiMa] = plGocGiaiMa;
//                            }
//                        }
//                        // ⭐ BƯỚC 2: Tải & Giải mã Bảng "DanhSachBaNhat"
//                        var tuDienBaNhat = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
//                        using (var cmdTaiBaNhat = conn.CreateCommand())
//                        {
//                            cmdTaiBaNhat.CommandText = $"SELECT SoHieu FROM [{tenBangDanhSachBaNhat}]";
//                            using var rdBaNhat = cmdTaiBaNhat.ExecuteReader();
//                            while (rdBaNhat.Read())
//                            {
//                                string shBaNhatMaHoa = rdBaNhat["SoHieu"]?.ToString() ?? "";
//                                string shBaNhatGiaiMa = string.IsNullOrWhiteSpace(shBaNhatMaHoa) ? "" : Module_BaoMatAES.GiaiMa(shBaNhatMaHoa).Trim();
//                                if (!string.IsNullOrWhiteSpace(shBaNhatGiaiMa)) tuDienBaNhat[shBaNhatGiaiMa] = shBaNhatMaHoa;
//                            }
//                        }
//                        using var transaction = conn.BeginTransaction();
//                        using var cmdUpdate = conn.CreateCommand();
//                        cmdUpdate.Transaction = transaction;
//                        cmdUpdate.CommandText = $@"UPDATE [{tenBangDanhSachBaNhat}]
//                                               SET DeNghi = @dn, ThanhTich = @tt
//                                               WHERE SoHieu = @shEncExact";
//                        var paramShUpd = cmdUpdate.Parameters.Add("@shEncExact", Microsoft.Data.Sqlite.SqliteType.Text);
//                        var paramDnUpd = cmdUpdate.Parameters.Add("@dn", Microsoft.Data.Sqlite.SqliteType.Text);
//                        var paramTtUpd = cmdUpdate.Parameters.Add("@tt", Microsoft.Data.Sqlite.SqliteType.Text);
//                        // ⭐ BƯỚC 3: QUÉT EXCEL VÀ ĐỐI CHIẾU TRÊN RAM
//                        for (int r = 2; r <= lastRow; r++)
//                        {
//                            string hoTen = ws.Cell(r, 2).GetString().Trim();
//                            string soHieuExcel = ws.Cell(r, 3).GetString().Trim(); // Lấy Plaintext
//                            string deNghi = ws.Cell(r, 12).GetString().Trim();
//                            string thanhTich = ws.Cell(r, 13).GetString().Trim();
//                            if (string.IsNullOrWhiteSpace(hoTen) && string.IsNullOrWhiteSpace(soHieuExcel)) continue;
//                            try
//                            {
//                                // [RÀNG BUỘC 1]: Số hiệu Excel có tồn tại trong CSDL DanhSach gốc không?
//                                if (!tuDienDanhSachGoc.TryGetValue(soHieuExcel, out string phanLoaiGoc))
//                                {
//                                    dsKhongCoSoHieu.Add(hoTen); continue;
//                                }
//                                // [RÀNG BUỘC 2]: Phân loại có phải là Loại 1 không?
//                                if (!phanLoaiGoc.Equals("Loại 1", StringComparison.OrdinalIgnoreCase))
//                                {
//                                    dsKhongPhaiLoai1.Add(hoTen); continue;
//                                }
//                                // [RÀNG BUỘC 3]: Đã tồn tại bên bảng Ba Nhất chưa?
//                                if (!tuDienBaNhat.TryGetValue(soHieuExcel, out string exactEncSoHieuDb))
//                                {
//                                    dsChuaDongBo.Add(hoTen); continue;
//                                }
//                                // VƯỢT QUA RÀNG BUỘC -> UPDATE
//                                paramShUpd.Value = exactEncSoHieuDb;
//                                paramDnUpd.Value = Module_BaoMatAES.MaHoa(deNghi);
//                                paramTtUpd.Value = Module_BaoMatAES.MaHoa(thanhTich);
//                                int rowsAffected = cmdUpdate.ExecuteNonQuery();
//                                if (rowsAffected > 0) soDongCapNhatThanhCong++;
//                                else dsLoiKyThuat.Add(hoTen + " (Không rõ nguyên nhân DB)");
//                            }
//                            catch (Exception ex)
//                            {
//                                dsLoiKyThuat.Add(hoTen + $" ({ex.Message})");
//                            }
//                        }
//                        transaction.Commit();
//                    }
//                }
//            });
//            // XỬ LÝ ĐÓNG GÓI HỘP THOẠI BÁO CÁO LỖI (Nằm ngoài Task.Run để luồng Form xử lý an toàn)
//            int tongSoLoi = dsKhongCoSoHieu.Count + dsKhongPhaiLoai1.Count + dsChuaDongBo.Count + dsLoiKyThuat.Count;
//            if (tongSoLoi > 0)
//            {
//                var mieuTaLoi = new List<string> { "⚠️ Quá trình nạp dữ liệu hoàn tất nhưng một số trường hợp bị từ chối do vi phạm ràng buộc:\n" };
//                if (dsKhongPhaiLoai1.Count > 0)
//                {
//                    mieuTaLoi.Add($"📌 Từ chối nhập dữ liệu CBCS có tên: {string.Join(", ", dsKhongPhaiLoai1.Take(30))}{(dsKhongPhaiLoai1.Count > 30 ? "..." : "")}");
//                    mieuTaLoi.Add("Lý do: Không được xếp mức Loại 1.\n");
//                }
//                if (dsKhongCoSoHieu.Count > 0)
//                {
//                    mieuTaLoi.Add($"📌 Từ chối nhập dữ liệu CBCS có tên: {string.Join(", ", dsKhongCoSoHieu.Take(20))}{(dsKhongCoSoHieu.Count > 20 ? "..." : "")}");
//                    mieuTaLoi.Add("Lý do: Không tìm thấy Số hiệu quân số trong CSDL chính.\n");
//                }
//                if (dsChuaDongBo.Count > 0)
//                {
//                    mieuTaLoi.Add($"📌 Từ chối nhập dữ liệu CBCS có tên: {string.Join(", ", dsChuaDongBo.Take(20))}{(dsChuaDongBo.Count > 20 ? "..." : "")}");
//                    mieuTaLoi.Add("Lý do: Đạt Loại 1 nhưng chưa được đồng bộ sang hệ thống Sổ Vàng (Vui lòng bấm 'Làm mới' hệ thống trước khi nạp lại).\n");
//                }
//                if (dsLoiKyThuat.Count > 0)
//                {
//                    mieuTaLoi.Add($"📌 Lỗi kỹ thuật với các CBCS: {string.Join("; ", dsLoiKyThuat.Take(15))}{(dsLoiKyThuat.Count > 15 ? "..." : "")}");
//                }
//                string msgCanhBao = string.Join("\n", mieuTaLoi);
//                if (parentForm != null && !parentForm.IsDisposed)
//                {
//                    MessageBox.Show(parentForm, msgCanhBao, "Báo cáo đối chiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                }
//                else
//                {
//                    MessageBox.Show(msgCanhBao, "Báo cáo đối chiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            System.Diagnostics.Debug.WriteLine("[Lỗi NhapDanhSachGoc] " + ex.Message);
//            MessageBox.Show("Lỗi hệ thống khi đọc file Excel: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
//        }
//        // Trả về số lượng đã nhập thành công để Form tự làm mới giao diện
//        return soDongCapNhatThanhCong;
//    }
//    public static async Task<List<SoVangDTO>> LayDuLieuSoVangTienTienAsync(string tenBang)
//    {
//        var rawList = new List<(int STT, string HoVaTen, string ThongBao, string SoTT, string Thang)>();
//        // Tự động lấy đường dẫn CSDL mà không phụ thuộc vào Form
//        string dbPath = Module_DanduongGPS.DuongDanCSDL2;
//        try
//        {
//            // 1. ĐỌC DỮ LIỆU THÔ TỪ SQLITE (Siêu tốc)
//            using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
//            {
//                await conn.OpenAsync();
//                using var cmd = conn.CreateCommand();
//                cmd.CommandText = $"SELECT ID, HoVaTen, ThongBaoTrungDoan, SoTTTrongSo, ThangCongNhan FROM [{tenBang}] ORDER BY STT ASC";
//                using var reader = await cmd.ExecuteReaderAsync();
//                int sttCounter = 1;
//                while (await reader.ReadAsync())
//                {
//                    if (reader.IsDBNull(0) || reader.GetInt32(0) == -1) continue;
//                    // Lưu dữ liệu mã hóa vào RAM trước
//                    rawList.Add((
//                        sttCounter++,
//                        reader["HoVaTen"]?.ToString() ?? "",
//                        reader["ThongBaoTrungDoan"]?.ToString() ?? "",
//                        reader["SoTTTrongSo"]?.ToString() ?? "",
//                        reader["ThangCongNhan"]?.ToString() ?? ""
//                    ));
//                }
//            }
//            // 2. GIẢI MÃ ĐA LUỒNG (PARALLEL LINQ) TRONG QUÁ TRÌNH NỀN
//            // Đảm bảo giữ đúng thứ tự danh sách với AsOrdered()
//            return await Task.Run(() =>
//            {
//                return rawList.AsParallel().AsOrdered().Select(row => new SoVangDTO
//                {
//                    STT = row.STT,
//                    HoVaTen = Module_BaoMatAES.GiaiMa(row.HoVaTen),
//                    ThongBaoTrungDoan = Module_BaoMatAES.GiaiMa(row.ThongBao),
//                    SoTTTrongSo = Module_BaoMatAES.GiaiMa(row.SoTT),
//                    ThangCongNhan = Module_BaoMatAES.GiaiMa(row.Thang)
//                }).ToList();
//            });
//        }
//        catch (Exception ex)
//        {
//            System.Diagnostics.Debug.WriteLine($"[Lỗi LayDuLieuSoVangTienTienAsync]: {ex.Message}");
//            return new List<SoVangDTO>();
//        }
//    }
//    public static async Task XuatDanhSachSoVangToExcelAsync(Form parentForm, string filePath, string tenBangHienTai, string kyHieuTrungDoan)
//    {
//        Form_Loading frmLoad = new Form_Loading("Đang xử lý và xuất dữ liệu ra Excel...");
//        if (parentForm != null) frmLoad.Icon = parentForm.Icon;
//        bool isLoadShown = false;
//        try
//        {
//            if (parentForm != null) parentForm.Enabled = false;
//            frmLoad.Show(parentForm);
//            isLoadShown = true;
//            await Task.Delay(50);
//            // 1. LẤY DỮ LIỆU ĐA LUỒNG
//            List<SoVangDTO> danhSach = await LayDuLieuSoVangTienTienAsync(tenBangHienTai);
//            if (danhSach.Count == 0)
//            {
//                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
//                return;
//            }
//            string tieuDeThongBao = $"Thông báo của {kyHieuTrungDoan}";
//            // 2. TẠO EXCEL 
//            await Task.Run(() =>
//            {
//                using (var wb = new XLWorkbook())
//                {
//                    var ws = wb.Worksheets.Add("DANH_SACH");
//                    ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
//                    ws.PageSetup.PageOrientation = XLPageOrientation.Portrait;
//                    // --- HEADER ---
//                    ws.Range("A1:E1").Merge().Value = "SỔ VÀNG";
//                    ws.Range("A1:E1").Style.Font.SetBold(true).Font.SetFontName(Module_HeThong.Font_Times_New_Roman).Font.SetFontSize(14);
//                    ws.Range("A1:E1").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Alignment.SetVertical(XLAlignmentVerticalValues.Center);
//                    ws.Range("A2:E2").Merge().Value = "BIỂU DƯƠNG GƯƠNG ĐIỂN HÌNH TRONG THỰC HIỆN PHONG TRÀO THI ĐUA BA NHẤT";
//                    ws.Range("A2:E2").Style.Font.SetBold(true).Font.SetFontName(Module_HeThong.Font_Times_New_Roman).Font.SetFontSize(12);
//                    ws.Range("A2:E2").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Alignment.SetVertical(XLAlignmentVerticalValues.Center);
//                    // --- TITLE CỘT ---
//                    string[] headers = { "STT", "Họ và tên", tieuDeThongBao, "Vào sổ vàng số", "Ghi chú" };
//                    ws.Row(4).Height = 24;
//                    var headerRange = ws.Range("A4:E4");
//                    for (int i = 0; i < headers.Length; i++)
//                    {
//                        headerRange.Cell(1, i + 1).Value = headers[i];
//                    }
//                    headerRange.Style.Font.SetBold(true).Font.SetFontName(Module_HeThong.Font_Times_New_Roman);
//                    headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
//                    headerRange.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
//                                               .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
//                                               .Alignment.SetWrapText(true);
//                    headerRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
//                                            .Border.SetInsideBorder(XLBorderStyleValues.Thin);
//                    // --- ĐỘ RỘNG CHO 5 CỘT ---
//                    ws.Column(1).Width = 5;
//                    ws.Column(2).Width = 30;
//                    ws.Column(3).Width = 40;
//                    ws.Column(4).Width = 15;
//                    ws.Column(5).Width = 15;
//                    // --- ĐỔ DỮ LIỆU SIÊU TỐC ---
//                    var dataToInsert = danhSach.Select(x => new object[] {
//                    x.STT,
//                    x.HoVaTen,
//                    x.ThongBaoTrungDoan,
//                    x.SoTTTrongSo,
//                    x.ThangCongNhan
//                }).AsEnumerable();
//                    ws.Cell(5, 1).InsertData(dataToInsert);
//                    // --- FORMAT CSS HÀNG LOẠT CHO TOÀN BỘ BẢNG DỮ LIỆU ---
//                    int totalRows = danhSach.Count;
//                    var dataRange = ws.Range(5, 1, 4 + totalRows, 5);
//                    ws.Rows(5, 4 + totalRows).Height = 18;
//                    // Set viền và font chữ
//                    dataRange.Style.Font.SetFontName(Module_HeThong.Font_Times_New_Roman);
//                    dataRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
//                    dataRange.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center).Alignment.SetWrapText(true);
//                    // Căn lề riêng cho từng cột
//                    ws.Range(5, 1, 4 + totalRows, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
//                    ws.Range(5, 2, 4 + totalRows, 3).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
//                    ws.Range(5, 4, 4 + totalRows, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
//                    Module_BanQuyen.DongDauExcel(wb);
//                    wb.SaveAs(filePath);
//                }
//            });
//            // Mở file sau khi hoàn tất
//            if (File.Exists(filePath))
//            {
//                Process.Start("explorer.exe", $"/select, \"{filePath}\"");
//            }
//        }
//        catch (Exception ex)
//        {
//            MessageBox.Show($"Lỗi khi xuất Excel: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
//        }
//        finally
//        {
//            if (isLoadShown) frmLoad.Close();
//            if (parentForm != null)
//            {
//                parentForm.Enabled = true;
//                parentForm.Focus();
//            }
//        }
//    }
//    // HÀM NHẬP DỮ LIỆU TỪ EXCEL VÀO SỔ VÀNG (Dành cho Form 44)
//    public static async Task<int> NhapDuLieuVaoSoVangTuExcel(Form parentForm, string excelPath, string dbPath)
//    {
//        int soDongCapNhatThanhCong = 0;
//        try
//        {
//            if (!File.Exists(excelPath) || !File.Exists(dbPath)) return 0;
//            // 1. Tự động nhận diện tên bảng theo phiên bản hệ thống
//            string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
//            string tenBangSoVang = phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase)
//                ? "DanhSachTanBinh_SoVangBaNhat"
//                : "DanhSach_SoVangBaNhat";
//            var danhSachBoQua = new List<string>();
//            var dsLoiKyThuat = new List<string>();
//            await Task.Run(() =>
//            {
//                using (var wb = new ClosedXML.Excel.XLWorkbook(excelPath))
//                {
//                    // Nhận diện Sheet đầu tiên
//                    var ws = wb.Worksheets.FirstOrDefault(w => w.Name.Contains("SO_VANG") || w.Name.Contains("BANHAT"))
//                             ?? wb.Worksheets.First();
//                    if (ws == null) return;
//                    // 2. KIỂM TRA HEADER (15 cột - Không tính ID)
//                    string[] mauHeaderChuan = {
//                        "STT", "Họ và tên", "Số hiệu", "Năm sinh", "Quê quán",
//                        "Ngày vào CAND", "Cấp bậc", "Chức vụ", "Đơn vị", "Phân loại",
//                        "Ghi chú", "Thành tích", "Thông báo E", "STT Ghi sổ", "Tháng công nhận"
//                    };
//                    // Dò dòng tiêu đề (giả sử nằm ở dòng 1, 2, 3, hoặc 4)
//                    int headerRow = 1;
//                    for (int i = 1; i <= 5; i++)
//                    {
//                        if (ws.Cell(i, 3).GetString().Trim().Equals("Số hiệu", StringComparison.OrdinalIgnoreCase))
//                        {
//                            headerRow = i;
//                            break;
//                        }
//                    }
//                    // Nếu file Excel không có cột Số Hiệu thì từ chối luôn
//                    string checkHeaderSoHieu = ws.Cell(headerRow, 3).GetString().Trim();
//                    if (!checkHeaderSoHieu.Equals("Số hiệu", StringComparison.OrdinalIgnoreCase))
//                    {
//                        if (parentForm != null && !parentForm.IsDisposed)
//                        {
//                            parentForm.Invoke(new Action(() => MessageBox.Show(parentForm,
//                                $"File Excel không đúng định dạng!\nĐể nạp được dữ liệu, Cột C (Cột số 3) bắt buộc phải là 'Số hiệu'.\n(Đây là chìa khóa để nhận diện cán bộ)",
//                                "Từ chối nạp", MessageBoxButtons.OK, MessageBoxIcon.Error)));
//                        }
//                        return;
//                    }
//                    int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
//                    if (lastRow <= headerRow) return;
//                    using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
//                    {
//                        conn.Open();
//                        // Lấy STT MAX hiện tại để gán cho người mới
//                        int nextSTT = 0;
//                        using (var cmdMax = conn.CreateCommand())
//                        {
//                            cmdMax.CommandText = $"SELECT IFNULL(MAX(STT), 0) FROM [{tenBangSoVang}]";
//                            nextSTT = Convert.ToInt32(cmdMax.ExecuteScalar()) + 1;
//                        }
//                        using var transaction = conn.BeginTransaction();
//                        // Lệnh kiểm tra tồn tại
//                        using var cmdCheck = conn.CreateCommand();
//                        cmdCheck.Transaction = transaction;
//                        cmdCheck.CommandText = $"SELECT EXISTS(SELECT 1 FROM [{tenBangSoVang}] WHERE SoHieu = @sh)";
//                        var paramShCheck = cmdCheck.Parameters.Add("@sh", Microsoft.Data.Sqlite.SqliteType.Text);
//                        // Lệnh Cập nhật (Nếu đã có trong sổ vàng)
//                        using var cmdUpdate = conn.CreateCommand();
//                        cmdUpdate.Transaction = transaction;
//                        cmdUpdate.CommandText = $@"UPDATE [{tenBangSoVang}] SET 
//                            HoVaTen=@ht, NamSinh=@ns, QueQuan=@qq, NgayVaoCAND=@nv, 
//                            CapBac=@cb, ChucVu=@cv, DonVi=@dv, PhanLoai=@pl, GhiChu=@gc, 
//                            ThanhTich=@tt, ThongBaoTrungDoan=@tb, SoTTTrongSo=@sott, ThangCongNhan=@tcn 
//                            WHERE SoHieu=@sh";
//                        PrepareSoVangParameters(cmdUpdate);
//                        // Lệnh Thêm mới (Nếu chưa có trong sổ vàng)
//                        using var cmdInsert = conn.CreateCommand();
//                        cmdInsert.Transaction = transaction;
//                        cmdInsert.CommandText = $@"INSERT INTO [{tenBangSoVang}] 
//                            (STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu, ThanhTich, ThongBaoTrungDoan, SoTTTrongSo, ThangCongNhan) 
//                            VALUES (@stt, @ht, @sh, @ns, @qq, @nv, @cb, @cv, @dv, @pl, @gc, @tt, @tb, @sott, @tcn)";
//                        var paramSttInsert = cmdInsert.Parameters.Add("@stt", Microsoft.Data.Sqlite.SqliteType.Integer);
//                        PrepareSoVangParameters(cmdInsert);
//                        // 3. ĐỌC DỮ LIỆU TỪNG DÒNG
//                        for (int r = headerRow + 1; r <= lastRow; r++)
//                        {
//                            string hoTen = ws.Cell(r, 2).GetString().Trim();
//                            string soHieu = ws.Cell(r, 3).GetString().Trim();
//                            if (string.IsNullOrWhiteSpace(hoTen) || string.IsNullOrWhiteSpace(soHieu))
//                                continue;
//                            try
//                            {
//                                string namSinh = ws.Cell(r, 4).GetString().Trim();
//                                string queQuan = ws.Cell(r, 5).GetString().Trim();
//                                string ngayVao = ws.Cell(r, 6).GetString().Trim();
//                                string capBac = ws.Cell(r, 7).GetString().Trim();
//                                string chucVu = ws.Cell(r, 8).GetString().Trim();
//                                string donVi = ws.Cell(r, 9).GetString().Trim();
//                                string phanLoai = ws.Cell(r, 10).GetString().Trim();
//                                string ghiChu = ws.Cell(r, 11).GetString().Trim();
//                                string thanhTich = ws.Cell(r, 12).GetString().Trim();
//                                string thongBao = ws.Cell(r, 13).GetString().Trim();
//                                string sttSo = ws.Cell(r, 14).GetString().Trim();
//                                string thangCN = ws.Cell(r, 15).GetString().Trim();
//                                string shMaHoa = Module_BaoMatAES.MaHoa(soHieu);
//                                paramShCheck.Value = shMaHoa;
//                                bool daTonTai = Convert.ToInt64(cmdCheck.ExecuteScalar()) > 0;
//                                if (daTonTai)
//                                {
//                                    BindSoVangParameters(cmdUpdate, shMaHoa, hoTen, namSinh, queQuan, ngayVao, capBac, chucVu, donVi, phanLoai, ghiChu, thanhTich, thongBao, sttSo, thangCN);
//                                    cmdUpdate.ExecuteNonQuery();
//                                }
//                                else
//                                {
//                                    paramSttInsert.Value = nextSTT++;
//                                    BindSoVangParameters(cmdInsert, shMaHoa, hoTen, namSinh, queQuan, ngayVao, capBac, chucVu, donVi, phanLoai, ghiChu, thanhTich, thongBao, sttSo, thangCN);
//                                    cmdInsert.ExecuteNonQuery();
//                                }
//                                soDongCapNhatThanhCong++;
//                            }
//                            catch (Exception ex)
//                            {
//                                dsLoiKyThuat.Add($"- {hoTen} (Dòng {r}): {ex.Message}");
//                            }
//                        }
//                        transaction.Commit();
//                    }
//                }
//            });
//            // 4. HIỂN THỊ BÁO CÁO LỖI
//            if (danhSachBoQua.Count > 0 || dsLoiKyThuat.Count > 0)
//            {
//                var mieuTaLoi = new List<string> { "⚠️ Quá trình nạp hoàn tất nhưng có lỗi tại một số dòng:\n" };
//                if (danhSachBoQua.Count > 0) mieuTaLoi.Add(string.Join("\n", danhSachBoQua.Take(10)) + (danhSachBoQua.Count > 10 ? "\n..." : ""));
//                if (dsLoiKyThuat.Count > 0) mieuTaLoi.Add(string.Join("\n", dsLoiKyThuat.Take(10)) + (dsLoiKyThuat.Count > 10 ? "\n..." : ""));
//                if (parentForm != null && !parentForm.IsDisposed)
//                    parentForm.Invoke(new Action(() => MessageBox.Show(parentForm, string.Join("\n", mieuTaLoi), "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning)));
//            }
//        }
//        catch (Exception ex)
//        {
//            System.Diagnostics.Debug.WriteLine("[Lỗi NhapSoVang] " + ex.Message);
//            MessageBox.Show("Lỗi định dạng tệp Excel: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
//        }
//        return soDongCapNhatThanhCong;
//    }
//    // 2 Hàm tiện ích gán biến SQL cho hàm Sổ vàng
//    private static void PrepareSoVangParameters(Microsoft.Data.Sqlite.SqliteCommand cmd)
//    {
//        cmd.Parameters.Add("@sh", SqliteType.Text);
//        cmd.Parameters.Add("@ht", SqliteType.Text);
//        cmd.Parameters.Add("@ns", SqliteType.Text);
//        cmd.Parameters.Add("@qq", SqliteType.Text);
//        cmd.Parameters.Add("@nv", SqliteType.Text);
//        cmd.Parameters.Add("@cb", SqliteType.Text);
//        cmd.Parameters.Add("@cv", SqliteType.Text);
//        cmd.Parameters.Add("@dv", SqliteType.Text);
//        cmd.Parameters.Add("@pl", SqliteType.Text);
//        cmd.Parameters.Add("@gc", SqliteType.Text);
//        cmd.Parameters.Add("@tt", SqliteType.Text);
//        cmd.Parameters.Add("@tb", SqliteType.Text);
//        cmd.Parameters.Add("@sott", SqliteType.Text);
//        cmd.Parameters.Add("@tcn", SqliteType.Text);
//    }
//    private static void BindSoVangParameters(Microsoft.Data.Sqlite.SqliteCommand cmd, string sh, string ht, string ns, string qq, string nv, string cb, string cv, string dv, string pl, string gc, string tt, string tb, string sott, string tcn)
//    {
//        cmd.Parameters["@sh"].Value = sh; // Đã mã hóa ở ngoài
//        cmd.Parameters["@ht"].Value = Module_BaoMatAES.MaHoa(ht);
//        cmd.Parameters["@ns"].Value = Module_BaoMatAES.MaHoa(ns);
//        cmd.Parameters["@qq"].Value = Module_BaoMatAES.MaHoa(qq);
//        cmd.Parameters["@nv"].Value = Module_BaoMatAES.MaHoa(nv);
//        cmd.Parameters["@cb"].Value = Module_BaoMatAES.MaHoa(cb);
//        cmd.Parameters["@cv"].Value = Module_BaoMatAES.MaHoa(cv);
//        cmd.Parameters["@dv"].Value = Module_BaoMatAES.MaHoa(dv);
//        cmd.Parameters["@pl"].Value = Module_BaoMatAES.MaHoa(pl);
//        cmd.Parameters["@gc"].Value = Module_BaoMatAES.MaHoa(gc);
//        cmd.Parameters["@tt"].Value = Module_BaoMatAES.MaHoa(tt);
//        cmd.Parameters["@tb"].Value = Module_BaoMatAES.MaHoa(tb);
//        cmd.Parameters["@sott"].Value = Module_BaoMatAES.MaHoa(sott);
//        cmd.Parameters["@tcn"].Value = Module_BaoMatAES.MaHoa(tcn);
//    }
//    // HÀM XUẤT DỮ LIỆU GỐC SỔ VÀNG (15 CỘT) ĐỂ BACKUP/NHẬP LẠI (Form 44 gọi)
//    public static async Task XuatDuLieuGocSoVangRaExcelAsync(Form parentForm, string filePathLuu)
//    {
//        string dbPath = Module_DanduongGPS.DuongDanCSDL2;
//        if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return;
//        // Tự động nhận diện tên bảng Sổ vàng theo phiên bản hệ thống
//        string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
//        string tenBang = phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase)
//            ? "DanhSachTanBinh_SoVangBaNhat"
//            : "DanhSach_SoVangBaNhat";
//        Form_Loading frmLoad = new Form_Loading("Đang đọc và tạo tệp excel Sổ Vàng, vui lòng đợi...");
//        if (parentForm != null) frmLoad.Icon = parentForm.Icon;
//        bool isLoadShown = false;
//        try
//        {
//            if (parentForm != null) parentForm.Enabled = false;
//            frmLoad.Show(parentForm);
//            isLoadShown = true;
//            await Task.Delay(50); // Nhường luồng cho UI vẽ form loading mượt
//            var danhSach = new List<string[]>();
//            // 1. ĐỌC DỮ LIỆU THÔ VÀ GIẢI MÃ TỪ SQLITE (Tốc độ cao)
//            await Task.Run(() =>
//            {
//                using (var conn = new SqliteConnection($"Data Source={dbPath}"))
//                {
//                    conn.Open();
//                    using var cmd = conn.CreateCommand();
//                    cmd.CommandText = $"SELECT * FROM [{tenBang}] ORDER BY STT ASC";
//                    using var reader = cmd.ExecuteReader();
//                    int stt = 1;
//                    while (reader.Read())
//                    {
//                        if (reader["ID"] == DBNull.Value || Convert.ToInt32(reader["ID"]) == -1) continue;
//                        danhSach.Add(new string[] {
//                            (stt++).ToString(),
//                            SafeDecrypt(reader["HoVaTen"]),
//                            SafeDecrypt(reader["SoHieu"]),
//                            SafeDecrypt(reader["NamSinh"]),
//                            SafeDecrypt(reader["QueQuan"]),
//                            SafeDecrypt(reader["NgayVaoCAND"]),
//                            SafeDecrypt(reader["CapBac"]),
//                            SafeDecrypt(reader["ChucVu"]),
//                            SafeDecrypt(reader["DonVi"]),
//                            SafeDecrypt(reader["PhanLoai"]),
//                            SafeDecrypt(reader["GhiChu"]),
//                            SafeDecrypt(reader["ThanhTich"]),
//                            SafeDecrypt(reader["ThongBaoTrungDoan"]),
//                            SafeDecrypt(reader["SoTTTrongSo"]),
//                            SafeDecrypt(reader["ThangCongNhan"])
//                        });
//                    }
//                }
//            });
//            // 2. TẠO TỆP EXCEL VÀ ĐỔ DỮ LIỆU
//            await Task.Run(() =>
//            {
//                using (var wb = new XLWorkbook())
//                {
//                    var ws = wb.Worksheets.Add("DS_SO_VANG_CSDL");
//                    // Khởi tạo HEADER (Đúng 15 cột chuẩn xác với hàm Nhập)
//                    string[] headers = {
//                        "STT", "Họ và tên", "Số hiệu", "Năm sinh", "Quê quán",
//                        "Ngày vào CAND", "Cấp bậc", "Chức vụ", "Đơn vị", "Phân loại",
//                        "Ghi chú", "Thành tích", "Thông báo E", "STT Ghi sổ", "Tháng công nhận"
//                    };
//                    ws.Row(1).Height = 36;
//                    for (int i = 0; i < headers.Length; i++)
//                    {
//                        var cell = ws.Cell(1, i + 1);
//                        cell.Value = headers[i];
//                        cell.Style.Font.Bold = true;
//                        cell.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
//                        cell.Style.Font.FontSize = 13;
//                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//                        cell.Style.Alignment.WrapText = true;
//                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EBF1F5");
//                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
//                        cell.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
//                    }
//                    // Tùy chỉnh ĐỘ RỘNG CỘT
//                    double[] widths = { 6, 25, 12, 10, 25, 12, 14, 15, 15, 12, 20, 35, 15, 12, 15 };
//                    for (int i = 0; i < widths.Length; i++) ws.Column(i + 1).Width = widths[i];
//                    // ĐỔ DỮ LIỆU HÀNG LOẠT VÀ FORMAT
//                    if (danhSach.Count > 0)
//                    {
//                        ws.Cell(2, 1).InsertData(danhSach);
//                        int lastRow = 1 + danhSach.Count;
//                        var range = ws.Range(2, 1, lastRow, 15);
//                        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
//                        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
//                        range.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
//                        range.Style.Font.FontSize = 13;
//                        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//                        // Tự động xuống dòng cho Ghi Chú và Thành Tích
//                        ws.Column(11).Style.Alignment.WrapText = true;
//                        ws.Column(12).Style.Alignment.WrapText = true;
//                        // Đặt chiều cao dòng cố định
//                        for (int i = 2; i <= lastRow; i++) ws.Row(i).Height = 36;
//                        // Căn lề từng cột cho thẩm mỹ
//                        ws.Column(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // STT
//                        ws.Column(2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left; // HT
//                        ws.Column(3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // SH
//                        ws.Column(4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // NS
//                        ws.Column(5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left; // QQ
//                        ws.Column(6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // NV
//                        ws.Column(7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // CB
//                        ws.Column(8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // CV
//                        ws.Column(9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // DV
//                        ws.Column(10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // PL
//                        ws.Column(11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left; // GC
//                        ws.Column(12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left; // TT
//                        ws.Column(13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // TBE
//                        ws.Column(14).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // STT GS
//                        ws.Column(15).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // TCN
//                    }
//                    Module_BanQuyen.DongDauExcel(wb);
//                    wb.SaveAs(filePathLuu);
//                }
//            });
//            // 3. GHI NHẬT KÝ VÀ MỞ FILE
//            Module_NhatKy.GhiNhatKy(
//                taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
//                hanhDong: $"Xuất tệp excel dữ liệu gốc Sổ Vàng ({tenBang})",
//                ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
//            );
//            if (File.Exists(filePathLuu))
//            {
//                MoTepExcelThiDuaPhongTrao3Nhat(filePathLuu);
//            }
//        }
//        catch (Exception ex)
//        {
//            MessageBox.Show("Lỗi khi xuất file Sổ vàng gốc: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
//        }
//        finally
//        {
//            if (isLoadShown) frmLoad.Close();
//            if (parentForm != null)
//            {
//                parentForm.Enabled = true;
//                parentForm.Focus();
//            }
//        }
//    }
//}
///Phiên bản 2
///
// =====================================================================================================
//  Module_BaNhat – PHIÊN BẢN 2 (tối ưu cho máy cấu hình thấp / Office cũ)
//  Tác giả: TrungKien – Yêu mèo cam
//
//  NGUYÊN TẮC THIẾT KẾ (căn cứ Module_BaoMatAES v2 và Module_DanduongGPS v3):
//   1) MaHoa() dùng IV NGẪU NHIÊN => cùng 1 chuỗi mã hóa ra mỗi lần một khác. Vì vậy TUYỆT ĐỐI không tra cứu
//      "WHERE SoHieu = <chuỗi vừa mã hóa>". Mọi tra cứu đều: giải mã 1 lần -> Dictionary trên RAM -> cập nhật theo ID.
//   2) Mỗi thao tác xuất Excel chỉ mở file 1 lần, lưu 1 lần (qua tệp tạm rồi Move) => không treo luồng giao diện.
//   3) Ghi dữ liệu bằng InsertData + gán style theo VÙNG (không gán style từng ô) => nhanh, ít RAM.
//   4) Không dùng công thức Excel cho báo cáo tổng hợp: tính sẵn trong C# => Office cũ/WPS/Protected View đều hiển thị đúng.
//   5) Tên bảng chỉ lấy từ danh sách hằng cố định (không nối chuỗi từ bên ngoài).
// =====================================================================================================
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using PhanMemThiDua2026;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
public class CbcDTO
{
    public int STT { get; set; }
    public string HoVaTen { get; set; } = string.Empty;
    public string SoHieu { get; set; } = string.Empty;
    public string NamSinh { get; set; } = string.Empty;
    public string QueQuan { get; set; } = string.Empty;
    public string NgayVaoCAND { get; set; } = string.Empty;
    public string CapBac { get; set; } = string.Empty;
    public string ChucVu { get; set; } = string.Empty;
    public string DonVi { get; set; } = string.Empty;
    public string PhanLoai { get; set; } = string.Empty;
    public string ThanhTich { get; set; } = string.Empty;
    public string GhiChu { get; set; } = string.Empty;
}
// Dùng cho Form42 - Xuất danh sách gốc (Không cần điều kiện)
public class DanhSachGocBaNhatDTO
{
    public int ID { get; set; }
    public string HoVaTen { get; set; } = string.Empty;
    public string SoHieu { get; set; } = string.Empty;
    public string NamSinh { get; set; } = string.Empty;
    public string QueQuan { get; set; } = string.Empty;
    public string NgayVaoCAND { get; set; } = string.Empty;
    public string CapBac { get; set; } = string.Empty;
    public string ChucVu { get; set; } = string.Empty;
    public string DonVi { get; set; } = string.Empty;
    public string PhanLoai { get; set; } = string.Empty;
    public string GhiChu { get; set; } = string.Empty;
    public string DeNghi { get; set; } = string.Empty;
    public string ThanhTich { get; set; } = string.Empty;
}
// Dùng cho Form42 - Xuất danh sách đề nghị (Có dùng File Mẫu)
public class DeNghiBaNhatDTO
{
    public int ID { get; set; }
    public string HoVaTen { get; set; } = string.Empty;
    public string SoHieu { get; set; } = string.Empty;
    public string NamSinh { get; set; } = string.Empty;
    public string NgayVaoCAND { get; set; } = string.Empty;
    public string CapBac { get; set; } = string.Empty;
    public string ChucVu { get; set; } = string.Empty;
    public string DonVi { get; set; } = string.Empty;
    public string PhanLoai { get; set; } = string.Empty;
    public string ThanhTich { get; set; } = string.Empty;
}
// Dùng cho Form44 - Xuất Sổ vàng (Chỉ có 5 Cột)
public class SoVangDTO
{
    public int STT { get; set; }
    public string HoVaTen { get; set; } = string.Empty;
    public string ThongBaoTrungDoan { get; set; } = string.Empty;
    public string SoTTTrongSo { get; set; } = string.Empty;
    public string ThangCongNhan { get; set; } = string.Empty;
}
public static class Module_BaNhat
{
    // Giữ lại để các nơi khác (nếu có) còn biên dịch được.
    public const int GioiHanToiDa = 3000;
    // Giới hạn an toàn khi NẠP tệp Excel (chống nạp nhầm tệp khổng lồ làm cạn RAM máy yếu).
    private const long TepExcelToiDaBytes = 50L * 1024 * 1024;
    private const int DongExcelToiDa = 50000;
    // ===================== P/Invoke mở Explorer và chọn tệp =====================
    [DllImport("shell32.dll", ExactSpelling = true)]
    private static extern void ILFree(IntPtr pidlList);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern IntPtr ILCreateFromPathW(string pszPath);
    [DllImport("shell32.dll", ExactSpelling = true)]
    private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, IntPtr apidl, int dwFlags);
    // ===================== HẰNG / CẤU TRÚC DÙNG CHUNG =====================
    private static readonly XLColor MauNenTieuDe = XLColor.FromHtml("#EBF1F5");
    private static readonly string[] TieuDeBaNhat =
        { "STT", "Họ và tên", "Số hiệu", "Năm sinh", "Quê quán", "Ngày vào CAND", "Cấp bậc", "Chức vụ", "Đơn vị", "Phân loại", "Ghi chú", "Đề nghị", "Thành tích" };
    private static readonly double[] DoRongBaNhat = { 6, 25, 12, 10, 25, 12, 14, 15, 15, 12, 20, 10, 35 };
    private static readonly int[] CotTraiBaNhat = { 2, 5, 11, 13 };
    private static readonly int[] CotVanBanBaNhat = { 3, 4, 6 };            // Số hiệu, Năm sinh, Ngày vào CAND: luôn là văn bản
    private static readonly string[] TieuDeSoVangGoc =
        { "STT", "Họ và tên", "Số hiệu", "Năm sinh", "Quê quán", "Ngày vào CAND", "Cấp bậc", "Chức vụ", "Đơn vị", "Phân loại", "Ghi chú", "Thành tích", "Thông báo E", "STT Ghi sổ", "Tháng công nhận" };
    private static readonly double[] DoRongSoVangGoc = { 6, 25, 12, 10, 25, 12, 14, 15, 15, 12, 20, 35, 15, 12, 15 };
    private static readonly int[] CotTraiSoVangGoc = { 2, 5, 11, 12 };
    private static readonly int[] CotVanBanSoVangGoc = { 3, 4, 6, 13, 14, 15 };
    // Cột dữ liệu (đã mã hóa) của bảng Sổ vàng và bảng Ba nhất khi nạp từ Excel
    private static readonly string[] CotSoVang =
        { "HoVaTen", "NamSinh", "QueQuan", "NgayVaoCAND", "CapBac", "ChucVu", "DonVi", "PhanLoai", "GhiChu", "ThanhTich", "ThongBaoTrungDoan", "SoTTTrongSo", "ThangCongNhan" };
    private static readonly string[] CotBaNhatCapNhat =
        { "HoVaTen", "NamSinh", "QueQuan", "NgayVaoCAND", "CapBac", "ChucVu", "DonVi", "PhanLoai", "GhiChu" };
    private static readonly string[] CotBaNhatThem =
        { "HoVaTen", "NamSinh", "QueQuan", "NgayVaoCAND", "CapBac", "ChucVu", "DonVi", "PhanLoai", "GhiChu", "DeNghi", "ThanhTich" };
    private static readonly string[] TenThamSo = Enumerable.Range(0, 16).Select(i => "@p" + i).ToArray();
    private static readonly HashSet<string> BangSoVangHopLe =
        new(StringComparer.Ordinal) { "DanhSach_SoVangBaNhat", "DanhSachTanBinh_SoVangBaNhat" };
    // Tên bảng theo phiên bản phần mềm. Chỉ có 2 bộ giá trị cố định => không thể bị chèn SQL.
    private readonly record struct BangDuLieu(string DanhSach, string SoVang, string TomTat);
    private static BangDuLieu ChonBang()
    {
        string pb = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
        return pb.Contains("tân binh", StringComparison.OrdinalIgnoreCase)
            ? new BangDuLieu("DanhSachBaNhat_TanBinh", "DanhSachTanBinh_SoVangBaNhat", "TomTatThanhTichBaNhat_TanBinh")
            : new BangDuLieu("DanhSachBaNhat", "DanhSach_SoVangBaNhat", "TomTatThanhTichBaNhat_CBCS");
    }
    // ===================== TIỆN ÍCH SQLITE / GIẢI MÃ =====================
    // Pooling=false: đóng kết nối là nhả hẳn tệp .db (không làm kẹt sao lưu/khôi phục). chiDoc=true: không thể ghi nhầm.
    private static SqliteConnection MoKetNoi(string dbPath, bool chiDoc)
    {
        var sb = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = chiDoc ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWrite,
            Pooling = false
        };
        var cn = new SqliteConnection(sb.ToString());
        try { cn.Open(); return cn; }
        catch { cn.Dispose(); throw; }
    }
    private static bool BangTonTai(SqliteConnection cn, string ten)
    {
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=@n LIMIT 1";
        cmd.Parameters.AddWithValue("@n", ten);
        return cmd.ExecuteScalar() != null;
    }
    private static string Doc(SqliteDataReader rd, int i) => rd.IsDBNull(i) ? "" : rd.GetString(i);
    // GiaiMa() không ném lỗi và đã có cache => chỉ cần bọc kiểm tra rỗng.
    private static string Giai(string? s) => string.IsNullOrWhiteSpace(s) ? "" : Module_BaoMatAES.GiaiMa(s).Trim();
    private static string GiaiCot(SqliteDataReader rd, int i) => Giai(Doc(rd, i));
    // Dữ liệu cũ có thể chưa mã hóa: giải mã được thì dùng, không thì dùng nguyên văn (trừ chuỗi "v2|..." hỏng => rỗng).
    private static string GiaiHoacGoc(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        string g = Module_BaoMatAES.GiaiMa(raw);
        if (g.Length > 0) return g.Trim();
        raw = raw.Trim();
        return raw.StartsWith("v2|", StringComparison.Ordinal) ? "" : raw;
    }
    private static int ToInt(object? v)
    {
        try { return v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v, CultureInfo.InvariantCulture); }
        catch { return 0; }
    }
    // ===================== TIỆN ÍCH EXCEL / FILE =====================
    private static string OrBlank(string? s) => string.IsNullOrWhiteSpace(s) ? "     " : s;
    private static string VietHoaChuDau(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = s.Trim().ToLowerInvariant();
        return char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
    private static string LamSachTenTep(string s)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s;
    }
    private static string Chuoi(IXLCell c)
    {
        try { return c.GetFormattedString().Trim(); }
        catch { return c.GetString().Trim(); }
    }
    // Ghi ra tệp tạm cùng thư mục rồi Move đè: mất điện/ngắt giữa chừng không để lại tệp cụt; tệp đang mở trong Excel => báo rõ.
    private static void LuuTepAnToan(XLWorkbook wb, string duongDan)
    {
        string thuMuc = Path.GetDirectoryName(Path.GetFullPath(duongDan)) ?? AppContext.BaseDirectory;
        string tam = Path.Combine(thuMuc, "~tmp_" + Guid.NewGuid().ToString("N") + ".xlsx");
        try
        {
            wb.SaveAs(tam);
            try { File.Move(tam, duongDan, true); }
            catch (IOException)
            {
                throw new IOException("Không ghi đè được tệp '" + Path.GetFileName(duongDan) + "'. Có thể tệp đang mở trong Excel — hãy đóng tệp rồi thử lại.");
            }
        }
        finally
        {
            try { if (File.Exists(tam)) File.Delete(tam); } catch { }
        }
    }
    private static string? KiemTraKichThuocTep(string path)
    {
        long len = new FileInfo(path).Length;
        return len > TepExcelToiDaBytes
            ? $"Tệp Excel quá lớn ({len / 1048576} MB). Giới hạn cho phép là {TepExcelToiDaBytes / 1048576} MB."
            : null;
    }
    private static string? KiemTraTieuDe(IXLWorksheet ws, int hang, string[] chuan)
    {
        var row = ws.Row(hang);
        for (int i = 0; i < chuan.Length; i++)
        {
            string h = row.Cell(i + 1).GetString().Trim();
            if (!h.Equals(chuan[i], StringComparison.OrdinalIgnoreCase))
                return $"Cấu trúc tệp Excel không hợp lệ!\nCột số {i + 1} đang là '{h}', yêu cầu chuẩn phải là '{chuan[i]}'.\nVui lòng không tự ý chèn, xóa hoặc đổi tên cột mẫu.";
        }
        return null;
    }
    private static void GhiTieuDeBang(IXLWorksheet ws, int hang, string[] tieuDe, double[]? doRong, double coChu, double chieuCao, XLColor nen)
    {
        for (int i = 0; i < tieuDe.Length; i++) ws.Cell(hang, i + 1).Value = tieuDe[i];
        var r = ws.Range(hang, 1, hang, tieuDe.Length);
        r.Style.Font.Bold = true;
        r.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
        if (coChu > 0) r.Style.Font.FontSize = coChu;
        r.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        r.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        r.Style.Alignment.WrapText = true;
        r.Style.Fill.BackgroundColor = nen;
        r.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        r.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        ws.Row(hang).Height = chieuCao;
        if (doRong != null)
            for (int i = 0; i < doRong.Length; i++) ws.Column(i + 1).Width = doRong[i];
    }
    // Đổ cả khối dữ liệu 1 lần, style theo vùng/cột (thay cho gán style ~15 lần/dòng như bản cũ).
    private static void GhiThanBang(IXLWorksheet ws, int hangDau, List<object[]> dong, int soCot,
                                    int[] cotTrai, int[] cotVanBan, double coChu, double chieuCaoDong)
    {
        int n = dong.Count;
        if (n == 0) return;
        int hangCuoi = hangDau + n - 1;
        // Cột mã/số phải giữ dạng văn bản (tránh mất số 0 đầu của Số hiệu, tránh "15/03/2018" bị đổi thành ngày)
        foreach (int c in cotVanBan) ws.Range(hangDau, c, hangCuoi, c).Style.NumberFormat.Format = "@";
        ws.Cell(hangDau, 1).InsertData(dong);
        var vung = ws.Range(hangDau, 1, hangCuoi, soCot);
        vung.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
        vung.Style.Font.FontSize = coChu;
        vung.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        vung.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        vung.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        vung.Style.Alignment.WrapText = true;
        vung.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        foreach (int c in cotTrai)
            ws.Range(hangDau, c, hangCuoi, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        if (chieuCaoDong > 0) ws.Rows(hangDau, hangCuoi).Height = chieuCaoDong;
    }
    private static void DatO(IXLCell c, string text, double coChu, bool dam = false, bool nghieng = false)
    {
        c.Value = text;
        c.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
        c.Style.Font.FontSize = coChu;
        if (dam) c.Style.Font.Bold = true;
        if (nghieng) c.Style.Font.Italic = true;
        c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }
    // Gạch chân khoảng 1/3 chiều dài, căn giữa chuỗi (chuẩn tên đơn vị trên văn bản). Dùng chung cho mọi báo cáo.
    private static void GachChan1Phan3(IXLCell cell, string text)
    {
        if (cell == null || string.IsNullOrWhiteSpace(text)) return;
        int total = text.Length;
        int len = (int)Math.Round(total / 3.0);
        if (len <= 0) return;
        int start = (total - len) / 2;
        var rich = cell.GetRichText();
        rich.ClearText();
        if (start > 0) rich.AddText(text.Substring(0, start));
        rich.AddText(text.Substring(start, len)).SetUnderline();
        int end = start + len;
        if (end < total) rich.AddText(text.Substring(end));
    }
    // Gạch chân đoạn "cáo số: ... của " trong dòng "(Kèm theo Báo cáo số: ..., ngày ... của ...)"
    private static void GachChanCumBaoCao(IXLCell cell, string text)
    {
        int start = text.IndexOf("cáo", StringComparison.Ordinal);
        int end = text.IndexOf("của", StringComparison.Ordinal);
        if (start < 0 || end <= start) return;
        int len = Math.Min(end - start + 4, text.Length - start);
        cell.GetRichText().Substring(start, len).SetUnderline();
    }
    private static void HienThongBao(Form? chu, string noiDung, string tieuDe, MessageBoxIcon icon)
    {
        void Hien()
        {
            if (chu != null && !chu.IsDisposed) MessageBox.Show(chu, noiDung, tieuDe, MessageBoxButtons.OK, icon);
            else MessageBox.Show(noiDung, tieuDe, MessageBoxButtons.OK, icon);
        }
        if (chu != null && !chu.IsDisposed && chu.IsHandleCreated && chu.InvokeRequired) chu.Invoke(new Action(Hien));
        else Hien();
    }
    private static string GomDanhSachLoi(string lyDo, List<string> ds, int toiDa)
        => $"📌 Từ chối nhập dữ liệu CBCS có tên: {string.Join(", ", ds.Take(toiDa))}{(ds.Count > toiDa ? "..." : "")}\nLý do: {lyDo}\n";
    // Khóa Form cha + hiện Form_Loading; Dispose là mở khóa lại. Idempotent.
    private sealed class ManHinhCho : IDisposable
    {
        private readonly Form? _cha;
        private readonly Form_Loading _frm;
        private bool _daDong;
        public ManHinhCho(Form? cha, string noiDung)
        {
            _cha = cha;
            _frm = new Form_Loading(noiDung);
            if (cha != null) _frm.Icon = cha.Icon;
        }
        public async Task HienAsync()
        {
            if (_cha != null) _cha.Enabled = false;
            _frm.Show(_cha);
            await Task.Delay(50);                        // nhường luồng UI vẽ form chờ
        }
        public void Dispose()
        {
            if (_daDong) return;
            _daDong = true;
            try { _frm.Close(); } catch { }
            if (_cha != null && !_cha.IsDisposed)
            {
                _cha.Enabled = true;
                _cha.Focus();
            }
        }
    }
    // Chống chạy chồng nhưng KHÔNG mất lần gọi cuối: nếu có yêu cầu đến lúc đang chạy, chạy lại đúng 1 lần.
    private sealed class BoChongChayChong
    {
        private int _dang, _canLai;
        public async Task ChayAsync(Func<Task> viec)
        {
            Volatile.Write(ref _canLai, 1);
            do
            {
                if (Interlocked.Exchange(ref _dang, 1) == 1) return;
                try
                {
                    while (Interlocked.Exchange(ref _canLai, 0) == 1)
                    {
                        try { await viec(); }
                        catch (Exception ex) { Debug.WriteLine("[BoChongChayChong] " + ex.Message); }
                    }
                }
                finally { Volatile.Write(ref _dang, 0); }
            } while (Volatile.Read(ref _canLai) == 1);
        }
    }
    private static readonly BoChongChayChong _boTyLe = new();
    private static readonly BoChongChayChong _boSoVang = new();
    // ===================== MỞ EXPLORER VÀ CHỌN TỆP =====================
    public static void MoTepExcelThiDuaPhongTrao3Nhat(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;
        IntPtr pidl = IntPtr.Zero;
        try
        {
            pidl = ILCreateFromPathW(filePath);
            if (pidl == IntPtr.Zero) throw new InvalidOperationException();
            if (SHOpenFolderAndSelectItems(pidl, 0, IntPtr.Zero, 0) < 0) throw new InvalidOperationException();
        }
        catch
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{filePath}\"") { UseShellExecute = true });
            }
            catch { }
        }
        finally
        {
            if (pidl != IntPtr.Zero) ILFree(pidl);
        }
    }
    // =====================================================================================================
    //  THÔNG TIN BÁO CÁO – đọc 1 LẦN bằng 1 kết nối (bản cũ mở 6–7 kết nối và đọc trùng bảng ThongTin)
    // =====================================================================================================
    private sealed class ThongTinBaoCao
    {
        public string Dong1 = "", TrungDoan = "", TieuDoan = "", Thang = "", Nam = "", Ngay = "", DiaDiem = "";
        public string LoaiDeNghi = "", KyHieuBaoCao = "...............", HoTenKy = "";
        public string KyHieuTrungDoan = "", KyHieuTieuDoan = "";
        public string LoaiBaoCao = "", Tuan = "", TomTat = "";
        public string ChucVuTruong = "", ChucVuNguoiKy = "";
        public bool KyTrucTiep = true;                        // true: chỉ huy đứng đầu ký trực tiếp; false: "KT." + chức vụ người ký
        public readonly Dictionary<int, (int hienTai, int canDat)> TyLe = new();
    }
    private static ThongTinBaoCao DocThongTin(string dbPath, string bangTomTat)
    {
        var tt = new ThongTinBaoCao();
        using var cn = MoKetNoi(dbPath, true);
        void Thu(string ten, Action a)
        {
            try { a(); }
            catch (Exception ex) { Debug.WriteLine($"[DocThongTin:{ten}] {ex.Message}"); }
        }
        Thu("ThongTin", () =>
        {
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT textBox1_TenTrungDoanDong1, TenTrungDoan, TenTieuDoan, Thang, Nam, Ngay, DiaDiem, LoaiDeNghi, KyHieuBaoCao, ChiHuyD FROM ThongTin WHERE ID = 1";
            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return;
            tt.Dong1 = GiaiCot(rd, 0);
            tt.TrungDoan = GiaiCot(rd, 1);
            tt.TieuDoan = GiaiCot(rd, 2);
            tt.Thang = GiaiCot(rd, 3);
            tt.Nam = GiaiCot(rd, 4);
            tt.Ngay = GiaiCot(rd, 5);
            tt.DiaDiem = GiaiCot(rd, 6);
            tt.LoaiDeNghi = GiaiCot(rd, 7);
            string kh = GiaiCot(rd, 8);
            if (kh.Length > 0) tt.KyHieuBaoCao = kh;
            tt.HoTenKy = GiaiHoacGoc(Doc(rd, 9));
        });
        Thu("KyHieu_DonVi", () =>
        {
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT KyHieu_TrungDoan, KyHieu_TieuDoan FROM KyHieu_DonVi WHERE ID = 1";
            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return;
            tt.KyHieuTrungDoan = GiaiCot(rd, 0);
            tt.KyHieuTieuDoan = GiaiCot(rd, 1);
        });
        Thu("ChonLoaiBaoCao", () =>
        {
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT ChonLoaiBaoCao, ChonTuan FROM ChonLoaiBaoCao ORDER BY ID DESC LIMIT 1";
            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return;
            tt.LoaiBaoCao = GiaiCot(rd, 0);
            tt.Tuan = GiaiCot(rd, 1);
        });
        Thu("TyLe", () =>
        {
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT ID, [KQ Hien tai], [KQ Can dat] FROM TyLe";
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                tt.TyLe[ToInt(rd.GetValue(0))] = (ToInt(rd.GetValue(1)), ToInt(rd.GetValue(2)));   // DBNull => 0 (bản cũ ném InvalidCast)
        });
        Thu("TomTat", () =>
        {
            if (!BangTonTai(cn, bangTomTat)) return;
            using var cmd = cn.CreateCommand();
            cmd.CommandText = $"SELECT NoiDung FROM [{bangTomTat}] WHERE ID = 1";
            if (cmd.ExecuteScalar() is string s) tt.TomTat = Giai(s);
        });
        Thu("ChiHuyD", () =>
        {
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT HoVaTen, ChucVu FROM ChiHuyD ORDER BY ID ASC";
            using var rd = cmd.ExecuteReader();
            int idx = 0;
            while (rd.Read())
            {
                string ht = GiaiHoacGoc(Doc(rd, 0));
                string cv = GiaiHoacGoc(Doc(rd, 1));
                if (idx == 0) tt.ChucVuTruong = cv;
                if (tt.HoTenKy.Length > 0 && ht.Equals(tt.HoTenKy, StringComparison.OrdinalIgnoreCase))
                {
                    if (idx > 0) { tt.KyTrucTiep = false; tt.ChucVuNguoiKy = cv; }
                    break;
                }
                idx++;
            }
        });
        return tt;
    }
    // Khối đơn vị phía trên (A1–A3) dùng chung cho BC_BANHAT và DS_BANHAT
    private static void GhiKhoiDonVi(IXLWorksheet ws, ThongTinBaoCao tt)
    {
        DatO(ws.Cell("A1"), string.IsNullOrWhiteSpace(tt.Dong1) ? "     " : tt.Dong1, 13);
        if (!string.IsNullOrWhiteSpace(tt.TrungDoan))
        {
            DatO(ws.Cell("A2"), tt.TrungDoan, 13);
            var a3 = ws.Cell("A3");
            DatO(a3, tt.TieuDoan, 13, dam: true);
            GachChan1Phan3(a3, tt.TieuDoan);
        }
        else
        {
            var a2 = ws.Cell("A2");
            DatO(a2, tt.TieuDoan, 13, dam: true);
            GachChan1Phan3(a2, tt.TieuDoan);
            ws.Cell("A3").Value = "";
        }
    }
    private static string TyLeChu(int tu, int mau)
    {
        if (tu == 0) return "0%";
        if (mau == 0) return "Dữ liệu sai";
        return Math.Round(tu * 100.0 / mau, 2).ToString("0.##", CultureInfo.CurrentCulture) + "%";
    }
    // Đặt BC_BANHAT là sheet duy nhất được chọn, con trỏ ở A1 (gỡ "Group sheets")
    private static void DatSheetBaoCaoHienHanh(XLWorkbook wb)
    {
        try
        {
            foreach (var s in wb.Worksheets)
            {
                try { s.SetTabSelected(false); } catch { }
            }
            if (wb.Worksheets.TryGetWorksheet("BC_BANHAT", out var wsBC))
            {
                wsBC.SetTabActive();
                wsBC.SetTabSelected(true);
                wsBC.Cell("A1").SetActive();
                wsBC.Range("A1").Select();
            }
        }
        catch (Exception ex) { Debug.WriteLine("Lỗi định vị con trỏ Excel: " + ex.Message); }
    }
    // =====================================================================================================
    //  BÁO CÁO TỔNG HỢP (sheet BC_BANHAT)
    // =====================================================================================================
    // true  : ghi công thức Excel như bản cũ (Excel tự tính lại khi người dùng sửa số).
    // false : ghi GIÁ TRỊ đã tính sẵn (an toàn nhất cho Office cũ / WPS / chế độ xem trước).
    private const bool GhiBangCongThuc = false;
    private static void GhiBaoCaoTongHop(XLWorkbook wb, ThongTinBaoCao tt)
    {
        var ws = wb.Worksheet("BC_BANHAT");
        string thang = OrBlank(tt.Thang), nam = OrBlank(tt.Nam), ngay = OrBlank(tt.Ngay);
        string diaDiem = OrBlank(tt.DiaDiem), deNghi = OrBlank(tt.LoaiDeNghi);
        // A11: ký hiệu đơn vị
        string kyHieu = string.Join(", ", new[] { tt.KyHieuTieuDoan, tt.KyHieuTrungDoan }.Where(s => s.Length > 0));
        var a11 = ws.Cell("A11");
        DatO(a11, kyHieu, 12);
        a11.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        // A1–A3
        GhiKhoiDonVi(ws, tt);
        // H4: địa điểm, ngày tháng
        DatO(ws.Cell("H4"), $"{diaDiem}, ngày {ngay} tháng {thang} năm {nam}", 14, nghieng: true);
        // A6: tiêu đề thời gian
        string loai = tt.LoaiBaoCao.Trim().ToUpperInvariant();
        string tuan = tt.Tuan.Trim();
        string thangHT = Module_XuatPhanLoai.LayThangHeThong();
        string thoiGian;
        if (loai.Contains("TUẦN"))
        {
            tuan = tuan.ToUpperInvariant().Replace("TUẦN", "").Trim();
            if (tuan.Length == 0) tuan = "1";
            thoiGian = $"TUẦN {tuan} THÁNG {thangHT}/{nam}";
        }
        else thoiGian = $"THÁNG {thangHT}/{nam}";
        ws.Range("A6:M6").Merge();
        DatO(ws.Cell("A6"), $"TRONG THỰC HIỆN PHONG TRÀO THI ĐUA \"BA NHẤT\" {thoiGian}", 14, dam: true);
        // A7: "(Kèm theo Báo cáo số: ...)"
        string textA7 = $"(Kèm theo Báo cáo số:            {tt.KyHieuBaoCao}, ngày {ngay}/{thang}/{nam} của {VietHoaChuDau(tt.TieuDoan)})";
        ws.Range("A7:M7").Merge();
        var a7 = ws.Cell("A7");
        DatO(a7, textA7, 14, nghieng: true);
        GachChanCumBaoCao(a7, textA7);
        // Hàng 11–12: số liệu và tỷ lệ
        (int hienTai, int canDat) T(int id) => tt.TyLe.TryGetValue(id, out var v) ? v : (0, 0);
        int b11 = T(1).hienTai;
        int d11 = T(6).hienTai;
        int e11 = T(2).canDat, f11 = T(3).canDat, g11 = T(4).canDat, h11 = T(5).canDat, i11 = T(6).canDat;
        ws.Cell("B11").Value = b11;
        ws.Cell("C11").Value = b11;
        ws.Cell("D11").Value = d11;
        ws.Cell("E11").Value = e11;
        ws.Cell("F11").Value = f11;
        ws.Cell("G11").Value = g11;
        ws.Cell("H11").Value = h11;
        ws.Cell("I11").Value = i11;
        ws.Cell("K11").Value = deNghi;
        ws.Cell("L11").Value = tt.TomTat;
        if (GhiBangCongThuc)
        {
            ws.Cell("E12").FormulaA1 = "=IFERROR((E11*100)/F11,\"Dữ liệu sai\")";
            ws.Cell("F12").FormulaA1 = "=IFERROR(IF(B11=0,0,(F11*100)/B11),\"Dữ liệu sai\")";
            ws.Cell("G12").FormulaA1 = "=IFERROR(100-F12,\"Dữ liệu sai\")";
            ws.Cell("H12").FormulaA1 = "=IFERROR(IF(OR(H11=\"\",H11=0),\"0%\",(H11*100)/B11 & \"%\"),\"Dữ liệu sai\")";
            ws.Cell("I12").FormulaA1 = "=IFERROR(IF(OR(I11=\"\",I11=0),\"0%\",(I11*100)/B11 & \"%\"),\"Dữ liệu sai\")";
        }
        else
        {
            double f12 = b11 == 0 ? 0 : f11 * 100.0 / b11;
            if (f11 == 0) ws.Cell("E12").Value = "Dữ liệu sai";
            else ws.Cell("E12").Value = e11 * 100.0 / f11;
            ws.Cell("F12").Value = f12;
            ws.Cell("G12").Value = 100 - f12;
            ws.Cell("H12").Value = TyLeChu(h11, b11);
            ws.Cell("I12").Value = TyLeChu(i11, b11);
        }
        ws.Range("E12:I12").Style.NumberFormat.Format = "00.00\"%\"";
        // Ký tên
        if (tt.KyTrucTiep)
        {
            ws.Cell("L14").Value = tt.ChucVuTruong.ToUpperInvariant();
            ws.Cell("L15").Value = "";
        }
        else
        {
            ws.Cell("L14").Value = ("KT. " + tt.ChucVuTruong).ToUpperInvariant();
            ws.Cell("L15").Value = tt.ChucVuNguoiKy.ToUpperInvariant();
        }
        ws.Cell("L17").Value = tt.HoTenKy;
        foreach (string addr in new[] { "L14", "L15", "L17" })
        {
            var c = ws.Cell(addr);
            c.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
            c.Style.Font.FontSize = 14;
            c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            c.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            if (addr != "L17") c.Style.Font.Bold = true;
        }
    }
    // Giữ tên cũ cho nơi khác gọi: mở tệp đã xuất, ghi BC_BANHAT, lưu lại (1 lần mở, 1 lần lưu an toàn).
    public static void XuatBaoCaoTongHop(string fileXuat)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileXuat) || !File.Exists(fileXuat))
                throw new FileNotFoundException("File Excel tổng hợp không tồn tại.");
            string db = Module_DanduongGPS.DuongDanCSDL2;
            if (string.IsNullOrWhiteSpace(db) || !File.Exists(db))
                throw new FileNotFoundException("Không tìm thấy CSDL csdl2.");
            var tt = DocThongTin(db, ChonBang().TomTat);
            using var wb = new XLWorkbook(fileXuat);
            GhiBaoCaoTongHop(wb, tt);
            DatSheetBaoCaoHienHanh(wb);
            LuuTepAnToan(wb, fileXuat);
        }
        catch (Exception ex)
        {
            Module_ThongBao.DangXuLy("Lỗi xuất báo cáo tổng hợp: " + ex.Message);
        }
    }
    // =====================================================================================================
    //  SHEET DS_BaNhat nhúng trong workbook do nơi khác tạo
    // =====================================================================================================
    private static List<object[]> DocDanhSachDeNghi(string dbPath, string bang)
    {
        var ds = new List<object[]>();
        using var cn = MoKetNoi(dbPath, true);
        if (!BangTonTai(cn, bang)) return ds;
        using var cmd = cn.CreateCommand();
        cmd.CommandText = $"SELECT HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu, DeNghi, ThanhTich FROM [{bang}] ORDER BY STT ASC";
        using var rd = cmd.ExecuteReader();
        int stt = 1;
        while (rd.Read())
        {
            if (!GiaiCot(rd, 10).Equals("X", StringComparison.OrdinalIgnoreCase)) continue;
            ds.Add(new object[]
            {
                stt++, GiaiCot(rd, 0), GiaiCot(rd, 1), GiaiCot(rd, 2), GiaiCot(rd, 3), GiaiCot(rd, 4),
                GiaiCot(rd, 5), GiaiCot(rd, 6), GiaiCot(rd, 7), GiaiCot(rd, 8), GiaiCot(rd, 9), "X", GiaiCot(rd, 11)
            });
        }
        return ds;
    }
    public static void XuatDuLieuVaoBangQuanLyBaNhat(XLWorkbook package, string dbPath, bool epXuatFileMau)
    {
        try
        {
            IXLWorksheet wsBN;
            if (package.Worksheets.Contains("DS_BaNhat"))
            {
                wsBN = package.Worksheet("DS_BaNhat");
                wsBN.Clear();
            }
            else wsBN = package.Worksheets.Add("DS_BaNhat");
            wsBN.TabColor = XLColor.Gold;
            wsBN.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
            wsBN.Style.Font.FontSize = 12;
            wsBN.Range("A1:M1").Merge();
            var a1 = wsBN.Cell("A1");
            a1.Value = "DANH SÁCH CÁN BỘ CHIẾN SĨ ĐỀ NGHỊ BIỂU DƯƠNG PHONG TRÀO \"BA NHẤT\"";
            a1.Style.Font.Bold = true;
            a1.Style.Font.FontSize = 14;
            a1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            wsBN.Row(1).Height = 30;
            GhiTieuDeBang(wsBN, 3, TieuDeBaNhat, DoRongBaNhat, 12, 25, MauNenTieuDe);
            if (!epXuatFileMau && !string.IsNullOrWhiteSpace(dbPath) && File.Exists(dbPath))
            {
                var dong = DocDanhSachDeNghi(dbPath, ChonBang().DanhSach);
                GhiThanBang(wsBN, 4, dong, 13, CotTraiBaNhat, CotVanBanBaNhat, 12, 24);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Lỗi thực thi tích hợp tại Sheet DS_BaNhat: " + ex.Message);
        }
    }
    // =====================================================================================================
    //  NẠP DỮ LIỆU TỪ SHEET DS_BaNhat (tệp tổng hợp) – bảng quản lý Ba nhất
    //  SỬA LỖI: bản cũ tra "SoHieu = MaHoa(soHieu)" nhưng MaHoa dùng IV ngẫu nhiên nên KHÔNG BAO GIỜ khớp
    //  => mọi dòng đều bị INSERT trùng. Nay giải mã SoHieu trong CSDL 1 lần, tra bằng Dictionary, cập nhật theo ID.
    // =====================================================================================================
    public static void NhapDuLieuVaoBangQuanLyBaNhat(string excelPath, string dbPath, Form? parentForm = null)
    {
        Form? chu = parentForm ?? (Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null);
        try
        {
            if (!File.Exists(excelPath) || !File.Exists(dbPath)) return;
            string? loiTep = KiemTraKichThuocTep(excelPath);
            if (loiTep != null) { HienThongBao(chu, loiTep, "Lỗi tệp nhập", MessageBoxIcon.Error); return; }
            string bang = ChonBang().DanhSach;
            var boQua = new List<string>();
            using (var wb = new XLWorkbook(excelPath))
            {
                if (!wb.Worksheets.TryGetWorksheet("DS_BaNhat", out var ws)) return;
                string? loiTieuDe = KiemTraTieuDe(ws, 3, TieuDeBaNhat);
                if (loiTieuDe != null) { HienThongBao(chu, loiTieuDe, "Lỗi cấu trúc tệp nhập", MessageBoxIcon.Error); return; }
                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                if (lastRow < 4) return;
                if (lastRow > DongExcelToiDa)
                {
                    HienThongBao(chu, $"Tệp có quá nhiều dòng (>{DongExcelToiDa}).", "Lỗi tệp nhập", MessageBoxIcon.Error);
                    return;
                }
                using var cn = MoKetNoi(dbPath, false);
                // Giải mã SoHieu hiện có 1 lần -> Dictionary(SoHieu -> ID)
                var idTheoSoHieu = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                int nextSTT;
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT ID, SoHieu FROM [{bang}]";
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        int id = rd.GetInt32(0);
                        if (id == -1) continue;
                        string sh = GiaiCot(rd, 1);
                        if (sh.Length > 0) idTheoSoHieu[sh] = id;
                    }
                }
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT IFNULL(MAX(STT), 0) FROM [{bang}]";
                    nextSTT = Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) + 1;
                }
                using var tx = cn.BeginTransaction();
                using var cmdUpd = TaoLenhCapNhat(cn, tx, bang, CotBaNhatCapNhat);
                using var cmdIns = TaoLenhThem(cn, tx, bang, CotBaNhatThem);
                var daXuLy = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int r = 4; r <= lastRow; r++)
                {
                    var row = ws.Row(r);
                    string hoTen = Chuoi(row.Cell(2));
                    if (hoTen.Length == 0) continue;
                    try
                    {
                        string soHieu = Chuoi(row.Cell(3));
                        string deNghi = Chuoi(row.Cell(12));
                        if (soHieu.Length == 0)
                        {
                            boQua.Add($"- {Module_HeThong.Tu_Dong_Chi}: {hoTen} dòng {r} không thể nạp vì thiếu Số hiệu quân số.");
                            continue;
                        }
                        if (!deNghi.Equals("X", StringComparison.OrdinalIgnoreCase))
                        {
                            boQua.Add($"- {Module_HeThong.Tu_Dong_Chi}: {hoTen} (Số hiệu: {soHieu}) bỏ qua do không có dấu X Đề nghị.");
                            continue;
                        }
                        if (!daXuLy.Add(soHieu))
                        {
                            boQua.Add($"- {Module_HeThong.Tu_Dong_Chi}: {hoTen} (Số hiệu: {soHieu}) dòng {r} bị trùng Số hiệu trong tệp, bỏ qua.");
                            continue;
                        }
                        string[] giaTri =
                        {
                            hoTen, Chuoi(row.Cell(4)), Chuoi(row.Cell(5)), Chuoi(row.Cell(6)), Chuoi(row.Cell(7)),
                            Chuoi(row.Cell(8)), Chuoi(row.Cell(9)), Chuoi(row.Cell(10)), Chuoi(row.Cell(11)),
                            "X", Chuoi(row.Cell(13))
                        };
                        if (idTheoSoHieu.TryGetValue(soHieu, out int idCu))
                        {
                            cmdUpd.Parameters["@id"].Value = idCu;
                            GanThamSo(cmdUpd, giaTri, CotBaNhatCapNhat.Length);   // UPDATE không đụng DeNghi/ThanhTich
                            cmdUpd.ExecuteNonQuery();
                        }
                        else
                        {
                            cmdIns.Parameters["@stt"].Value = nextSTT++;
                            cmdIns.Parameters["@sh"].Value = Module_BaoMatAES.MaHoa(soHieu);
                            GanThamSo(cmdIns, giaTri, CotBaNhatThem.Length);
                            cmdIns.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        boQua.Add($"- {Module_HeThong.Tu_Dong_Chi}: {hoTen} (Dòng {r}) - Lỗi kỹ thuật: {ex.Message}");
                    }
                }
                tx.Commit();
            }
            if (boQua.Count > 0)
            {
                string msg = string.Join("\n", boQua.Take(15));
                if (boQua.Count > 15) msg += $"\n... và {boQua.Count - 15} trường hợp khác.";
                HienThongBao(chu, msg, "Thông báo nạp Sổ Vàng", MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[Lỗi NhapDataSoVang] " + ex.Message);
        }
    }
    // UPDATE [bang] SET c0=@p0,... WHERE ID=@id
    private static SqliteCommand TaoLenhCapNhat(SqliteConnection cn, SqliteTransaction tx, string bang, string[] cot)
    {
        var cmd = cn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = $"UPDATE [{bang}] SET {string.Join(", ", cot.Select((c, i) => $"{c}={TenThamSo[i]}"))} WHERE ID=@id";
        for (int i = 0; i < cot.Length; i++) cmd.Parameters.Add(TenThamSo[i], SqliteType.Text);
        cmd.Parameters.Add("@id", SqliteType.Integer);
        return cmd;
    }
    // INSERT [bang] (STT, SoHieu, c0..) VALUES (@stt, @sh, @p0..)
    private static SqliteCommand TaoLenhThem(SqliteConnection cn, SqliteTransaction tx, string bang, string[] cot)
    {
        var cmd = cn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = $"INSERT INTO [{bang}] (STT, SoHieu, {string.Join(", ", cot)}) VALUES (@stt, @sh, {string.Join(", ", cot.Select((_, i) => TenThamSo[i]))})";
        cmd.Parameters.Add("@stt", SqliteType.Integer);
        cmd.Parameters.Add("@sh", SqliteType.Text);
        for (int i = 0; i < cot.Length; i++) cmd.Parameters.Add(TenThamSo[i], SqliteType.Text);
        return cmd;
    }
    // Mã hóa từng giá trị (IV ngẫu nhiên) rồi gán vào tham số @p0..@pN-1. MaHoa("") trả "" như quy ước cũ.
    private static void GanThamSo(SqliteCommand cmd, string[] giaTri, int soCot)
    {
        for (int i = 0; i < soCot; i++)
            cmd.Parameters[TenThamSo[i]].Value = Module_BaoMatAES.MaHoa(giaTri[i]);
    }
    // =====================================================================================================
    //  TỶ LỆ "BA NHẤT" TRÊN THANH TRẠNG THÁI
    // =====================================================================================================
    private readonly record struct KetQuaTyLe(int TyLe, int TongLoai1, int SoDeNghi);
    private static KetQuaTyLe TinhTyLe(string dbPath)
    {
        int tyLe = 0, tongLoai1 = 0, soDeNghi = 0;
        string bangBaNhat = ChonBang().DanhSach;
        using var cn = MoKetNoi(dbPath, true);
        using (var cmd = cn.CreateCommand())
        {
            cmd.CommandText = "SELECT TyLe FROM QuyDinhTyLe_BaNhat WHERE ID = 1";
            if (cmd.ExecuteScalar() is string s) int.TryParse(Giai(s), out tyLe);
        }
        using (var cmd = cn.CreateCommand())
        {
            cmd.CommandText = "SELECT PhanLoai FROM DanhSach WHERE PhanLoai IS NOT NULL AND PhanLoai <> ''";
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                if (GiaiCot(rd, 0).Equals("Loại 1", StringComparison.OrdinalIgnoreCase)) tongLoai1++;
        }
        if (BangTonTai(cn, bangBaNhat))
        {
            using var cmd = cn.CreateCommand();
            cmd.CommandText = $"SELECT DeNghi FROM [{bangBaNhat}] WHERE DeNghi IS NOT NULL AND DeNghi <> ''";
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                if (GiaiCot(rd, 0).Equals("X", StringComparison.OrdinalIgnoreCase)) soDeNghi++;
        }
        return new KetQuaTyLe(tyLe, tongLoai1, soDeNghi);
    }
    private static void HienKetQuaTyLe(ToolStripStatusLabel lbl, string text, System.Drawing.Color mau)
    {
        void Gan()
        {
            if (lbl.IsDisposed) return;
            if (lbl.Text != text) lbl.Text = text;
            lbl.ForeColor = mau;
        }
        var cha = lbl.GetCurrentParent();
        if (cha != null && cha.InvokeRequired) cha.BeginInvoke(new Action(Gan));
        else Gan();
    }
    public static Task TinhToanVaHienThiTyLeBaNhatAsync(ToolStripStatusLabel labelTyLe)
    {
        if (labelTyLe == null || labelTyLe.IsDisposed) return Task.CompletedTask;
        string dbPath = Module_DanduongGPS.DuongDanCSDL2;
        if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return Task.CompletedTask;
        return _boTyLe.ChayAsync(async () =>
        {
            if (labelTyLe.IsDisposed) return;
            var kq = await Task.Run(() => TinhTyLe(dbPath));
            if (labelTyLe.IsDisposed) return;
            int chiTieu = (int)Math.Round(kq.TongLoai1 * (kq.TyLe / 100.0), MidpointRounding.AwayFromZero);
            double tyLeHienTai = kq.TongLoai1 > 0 ? Math.Round((double)kq.SoDeNghi / kq.TongLoai1 * 100, 1) : 0;
            string trangThai = kq.SoDeNghi == chiTieu
                ? "Đạt tỷ lệ"
                : kq.SoDeNghi < chiTieu
                    ? $"Đang thiếu: {chiTieu - kq.SoDeNghi} {Module_HeThong.Tu_dong_chi} "
                    : $"Đang thừa: {kq.SoDeNghi - chiTieu} {Module_HeThong.Tu_dong_chi}";
            string text = $"           Chỉ tiêu “Ba nhất” {kq.TyLe}% = {chiTieu} {Module_HeThong.Tu_dong_chi}    |    Tỷ lệ hiện tại {kq.SoDeNghi} {Module_HeThong.Tu_dong_chi} = {tyLeHienTai}%    |    {trangThai}";
            HienKetQuaTyLe(labelTyLe, text, kq.SoDeNghi == chiTieu ? System.Drawing.Color.DarkGreen : System.Drawing.Color.Red);
        });
    }
    // =====================================================================================================
    //  ĐỒNG BỘ TÌNH TRẠNG "ĐÃ ĐỀ NGHỊ" TỪ SỔ VÀNG
    //  Bản cũ UPDATE mọi dòng (N lần giải mã + N lần mã hóa + N lệnh ghi) dù không có gì đổi.
    //  Nay chỉ ghi những dòng THỰC SỰ thay đổi.
    // =====================================================================================================
    private static void CapNhatTinhTrangSoVang(string dbPath)
    {
        var bang = ChonBang();
        using var cn = MoKetNoi(dbPath, false);
        var daVaoSo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (BangTonTai(cn, bang.SoVang))
        {
            using var cmd = cn.CreateCommand();
            cmd.CommandText = $"SELECT SoHieu FROM [{bang.SoVang}]";
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                string sh = GiaiCot(rd, 0);
                if (sh.Length > 0) daVaoSo.Add(sh);
            }
        }
        if (!BangTonTai(cn, bang.DanhSach)) return;
        const string DA_DE_NGHI = "Đã đề nghị";
        var canGhi = new List<(int Id, string TinhTrang)>();
        using (var cmd = cn.CreateCommand())
        {
            cmd.CommandText = $"SELECT ID, SoHieu, TinhTrang FROM [{bang.DanhSach}]";
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                int id = rd.GetInt32(0);
                if (id == -1) continue;                                   // dòng đệm mồi của CSDL
                string sh = GiaiCot(rd, 1);
                string moi = sh.Length > 0 && daVaoSo.Contains(sh) ? DA_DE_NGHI : "";
                string cu = GiaiCot(rd, 2);
                if (!string.Equals(cu, moi, StringComparison.Ordinal)) canGhi.Add((id, moi));
            }
        }
        if (canGhi.Count == 0) return;
        using var tx = cn.BeginTransaction();
        using var upd = cn.CreateCommand();
        upd.Transaction = tx;
        upd.CommandText = $"UPDATE [{bang.DanhSach}] SET TinhTrang = @tt WHERE ID = @id";
        var pTT = upd.Parameters.Add("@tt", SqliteType.Text);
        var pID = upd.Parameters.Add("@id", SqliteType.Integer);
        foreach (var (id, tinhTrang) in canGhi)
        {
            pTT.Value = Module_BaoMatAES.MaHoa(tinhTrang);
            pID.Value = id;
            upd.ExecuteNonQuery();
        }
        tx.Commit();
    }
    public static Task CapNhatTinhTrangSoVangAsync()
    {
        string dbPath = Module_DanduongGPS.DuongDanCSDL2;
        if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return Task.CompletedTask;
        return _boSoVang.ChayAsync(async () =>
        {
            try { await Task.Run(() => CapNhatTinhTrangSoVang(dbPath)); }
            catch (Exception ex) { Debug.WriteLine("Lỗi tính toán tình trạng sổ vàng: " + ex.Message); }
        });
    }
    // =====================================================================================================
    //  XUẤT DANH SÁCH GỐC (Form 42)
    // =====================================================================================================
    public static async Task XuatDuLieuRaTepExcelTuCSDL_ToiUu(List<DanhSachGocBaNhatDTO> danhSach, string filePathLuu)
    {
        try
        {
            await Task.Run(() =>
            {
                var dong = new List<object[]>(danhSach.Count);
                int stt = 1;
                foreach (var x in danhSach)
                    dong.Add(new object[] { stt++, x.HoVaTen, x.SoHieu, x.NamSinh, x.QueQuan, x.NgayVaoCAND, x.CapBac, x.ChucVu, x.DonVi, x.PhanLoai, x.GhiChu, x.DeNghi, x.ThanhTich });
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("DS_BANHAT_CSDL");
                GhiTieuDeBang(ws, 1, TieuDeBaNhat, DoRongBaNhat, 13, 36, MauNenTieuDe);
                GhiThanBang(ws, 2, dong, 13, CotTraiBaNhat, CotVanBanBaNhat, 13, 36);
                Module_BanQuyen.DongDauExcel(wb);
                LuuTepAnToan(wb, filePathLuu);
            });
            Module_NhatKy.GhiNhatKy(
                taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                hanhDong: "Xuất tệp excel toàn bộ CSDL gốc Ba Nhất",
                ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
            MoTepExcelThiDuaPhongTrao3Nhat(filePathLuu);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi khi xuất file: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    // =====================================================================================================
    //  XUẤT DANH SÁCH ĐỀ NGHỊ + BÁO CÁO TỔNG HỢP (Form 42) – MỘT LẦN MỞ, MỘT LẦN LƯU
    // =====================================================================================================
    private static void GhiSheetDanhSachDeNghi(XLWorkbook wb, ThongTinBaoCao tt, List<DeNghiBaNhatDTO> danhSach)
    {
        var ws = wb.Worksheet("DS_BANHAT");
        string thang = tt.Thang.Trim(), nam = tt.Nam.Trim(), ngay = tt.Ngay.Trim(), diaDiem = tt.DiaDiem.Trim();
        GhiKhoiDonVi(ws, tt);
        // F3:J3 – địa điểm, ngày tháng
        ws.Range("F3:J3").Clear(XLClearOptions.Contents);
        ws.Range("F3:J3").Merge();
        var f3 = ws.Cell("F3");
        DatO(f3, diaDiem.Length > 0 ? $"{diaDiem}, ngày {ngay} tháng {thang} năm {nam}" : $"(ngày {ngay} tháng {thang} năm {nam})", 13, nghieng: true);
        f3.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        // Xóa vùng dữ liệu cũ của mẫu: chỉ tới dòng cuối THỰC SỰ dùng (bản cũ xóa 5.000.000 ô A9:J500000)
        int cuoiMau = ws.LastRowUsed(XLCellsUsedOptions.All)?.RowNumber() ?? 9;
        if (cuoiMau >= 9) ws.Range(9, 1, cuoiMau, 10).Clear(XLClearOptions.All);
        ws.Range("A5:J5").Merge();
        var a5 = ws.Cell("A5");
        a5.Value = $"CBCS ĐỀ NGHỊ BIỂU DƯƠNG GƯƠNG ĐIỂN HÌNH TRONG THỰC HIỆN PHONG TRÀO THI ĐUA BA NHẤT THÁNG {thang}/{nam}";
        a5.Style.Font.Bold = true;
        a5.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        string textA6 = $"(Kèm theo Báo cáo số:                 {tt.KyHieuBaoCao}, ngày {ngay}/{thang}/{nam} của {VietHoaChuDau(tt.TieuDoan)})";
        ws.Range("A6:J6").Merge();
        var a6 = ws.Cell("A6");
        a6.Value = textA6;
        a6.Style.Font.Italic = true;
        a6.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        GachChanCumBaoCao(a6, textA6);
        // Dữ liệu (10 cột)
        var dong = new List<object[]>(danhSach.Count);
        int stt = 1;
        foreach (var x in danhSach)
            dong.Add(new object[] { stt++, x.HoVaTen, x.SoHieu, x.NamSinh, x.NgayVaoCAND, x.CapBac, x.ChucVu, x.DonVi, x.PhanLoai, x.ThanhTich });
        const int hangDau = 9;
        GhiThanBang(ws, hangDau, dong, 10, new[] { 2, 10 }, new[] { 3, 4, 5 }, 13, 0);
        for (int i = 0; i < dong.Count; i++) ws.Row(hangDau + i).ClearHeight();     // để Excel tự co giãn theo nội dung
        int startRow = hangDau + dong.Count;
        var rngTong = ws.Range(startRow, 1, startRow, 2);
        rngTong.Merge();
        var cTong = ws.Cell(startRow, 1);
        cTong.Value = $"Tổng cộng: {dong.Count:00} đồng chí./.";
        cTong.Style.Font.Bold = true;
        cTong.Style.Font.Italic = true;
        cTong.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        cTong.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        // Khối ký tên (cột F–J)
        int dongKy = startRow + 1;
        void GhiDongKy(string text)
        {
            ws.Range(dongKy, 6, dongKy, 10).Merge();
            var c = ws.Cell(dongKy, 6);
            c.Value = text;
            c.Style.Font.Bold = true;
            c.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
            c.Style.Font.FontSize = 14;
            c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            c.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            dongKy++;
        }
        if (tt.KyTrucTiep)
            GhiDongKy(tt.ChucVuTruong.ToUpperInvariant());
        else
        {
            GhiDongKy("KT. " + tt.ChucVuTruong.ToUpperInvariant());
            GhiDongKy(tt.ChucVuNguoiKy.ToUpperInvariant());
        }
        dongKy += 4;
        GhiDongKy(tt.HoTenKy);
    }
    public static async Task XuatDanhSachBaNhatToExcelAsync(Form? parentForm, List<DeNghiBaNhatDTO> danhSach)
    {
        string templatePath = Module_DanduongGPS.DuongDanCSDL4ex;
        string dbPath = Module_DanduongGPS.DuongDanCSDL2;
        if (string.IsNullOrWhiteSpace(templatePath) || !File.Exists(templatePath))
        {
            MessageBox.Show("Không tìm thấy file mẫu Excel tại: " + templatePath, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
        {
            MessageBox.Show("Không tìm thấy cơ sở dữ liệu csdl2.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        // Đọc thông tin đơn vị 1 lần (dùng cho tên tệp gợi ý VÀ cho nội dung báo cáo)
        ThongTinBaoCao tt;
        try
        {
            string bangTomTat = ChonBang().TomTat;
            tt = await Task.Run(() => DocThongTin(dbPath, bangTomTat));
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi đọc thông tin báo cáo: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        string kyHieuTieuDoan = tt.KyHieuTieuDoan.Length > 0 ? tt.KyHieuTieuDoan : "D2";
        using var sfd = new SaveFileDialog
        {
            Filter = "Excel Files|*.xlsx",
            FileName = LamSachTenTep($"BÁO CÁO BIỂU DƯƠNG PHONG TRÀO BA NHẤT CỦA {kyHieuTieuDoan} {DateTime.Now:yyyyMMdd_HHmmss}") + ".xlsx",
            Title = "Chọn nơi lưu file Excel"
        };
        if (sfd.ShowDialog() != DialogResult.OK) return;
        string filePathLuu = sfd.FileName;
        ManHinhCho? cho = null;
        try
        {
            cho = new ManHinhCho(parentForm, "Đang tạo tệp excel, vui lòng đợi...");
            await cho.HienAsync();
            string? loiTongHop = null;
            await Task.Run(() =>
            {
                using var wb = new XLWorkbook(templatePath);
                // Chỉ giữ 2 sheet cần thiết
                var xoa = wb.Worksheets
                    .Where(w => !w.Name.Equals("BC_BANHAT", StringComparison.OrdinalIgnoreCase) &&
                                !w.Name.Equals("DS_BANHAT", StringComparison.OrdinalIgnoreCase))
                    .Select(w => w.Name).ToList();
                foreach (var ten in xoa) wb.Worksheets.Delete(ten);
                GhiSheetDanhSachDeNghi(wb, tt, danhSach);
                // Báo cáo tổng hợp lỗi thì vẫn lưu danh sách (đúng hành vi cũ), chỉ báo riêng
                try { GhiBaoCaoTongHop(wb, tt); }
                catch (Exception ex) { loiTongHop = ex.Message; }
                Module_BanQuyen.DongDauExcel(wb);
                DatSheetBaoCaoHienHanh(wb);
                LuuTepAnToan(wb, filePathLuu);
            });
            Module_NhatKy.GhiNhatKy(
                taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                hanhDong: "Xuất tệp excel báo cáo thi đua Ba Nhất",
                ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
            cho.Dispose();
            cho = null;
            if (loiTongHop != null) Module_ThongBao.DangXuLy("Lỗi xuất báo cáo tổng hợp: " + loiTongHop);
            MoTepExcelThiDuaPhongTrao3Nhat(filePathLuu);
        }
        catch (Exception ex)
        {
            cho?.Dispose();
            cho = null;
            MessageBox.Show("Lỗi xuất file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            cho?.Dispose();
        }
    }
    // =====================================================================================================
    //  NHẬP DANH SÁCH GỐC TỪ EXCEL (Form 42)
    //  Cập nhật theo ID; chỉ giải mã PhanLoai của những người có trong tệp Excel.
    // =====================================================================================================
    private readonly record struct DongNhapBaNhat(string Ten, string SoHieu, string DeNghi, string ThanhTich);
    public static async Task<int> NhapDuLieuTuTepExcelVaoCSDL(Form? parentForm, string excelPath, string dbPath)
    {
        int soThanhCong = 0;
        try
        {
            if (!File.Exists(excelPath) || !File.Exists(dbPath)) return 0;
            string bang = ChonBang().DanhSach;
            var dsKhongCoSoHieu = new List<string>();
            var dsKhongPhaiLoai1 = new List<string>();
            var dsChuaDongBo = new List<string>();
            var dsLoiKyThuat = new List<string>();
            string? loiCauTruc = null;
            string tieuDeLoi = "Lỗi cấu trúc";
            await Task.Run(() =>
            {
                loiCauTruc = KiemTraKichThuocTep(excelPath);
                if (loiCauTruc != null) { tieuDeLoi = "Từ chối nhập liệu"; return; }
                using var wb = new XLWorkbook(excelPath);
                if (!wb.Worksheets.TryGetWorksheet("DS_BANHAT_CSDL", out var ws))
                {
                    loiCauTruc = "Tệp Excel không hợp lệ! Không tìm thấy Sheet 'DS_BANHAT_CSDL'.\nVui lòng chọn đúng tệp được xuất từ chức năng 'Xuất danh sách gốc'.";
                    return;
                }
                loiCauTruc = KiemTraTieuDe(ws, 1, TieuDeBaNhat);
                if (loiCauTruc != null) { tieuDeLoi = "Từ chối nhập liệu"; return; }
                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                if (lastRow < 2) return;
                if (lastRow > DongExcelToiDa)
                {
                    loiCauTruc = $"Tệp có quá nhiều dòng (>{DongExcelToiDa}).";
                    tieuDeLoi = "Từ chối nhập liệu";
                    return;
                }
                // 1) Đọc Excel vào RAM
                var dong = new List<DongNhapBaNhat>();
                var tapSoHieu = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int r = 2; r <= lastRow; r++)
                {
                    var row = ws.Row(r);
                    string ten = Chuoi(row.Cell(2));
                    string sh = Chuoi(row.Cell(3));
                    if (ten.Length == 0 && sh.Length == 0) continue;
                    string dn = Chuoi(row.Cell(12));
                    if (dn.Equals("X", StringComparison.OrdinalIgnoreCase)) dn = "X";     // chuẩn hóa để các nơi đếm/lọc nhất quán
                    dong.Add(new DongNhapBaNhat(ten, sh, dn, Chuoi(row.Cell(13))));
                    if (sh.Length > 0) tapSoHieu.Add(sh);
                }
                if (dong.Count == 0) return;
                using var cn = MoKetNoi(dbPath, false);
                // 2) Bảng gốc "DanhSach": chỉ giải mã PhanLoai của người có trong tệp
                var phanLoaiGoc = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "SELECT SoHieu, PhanLoai FROM DanhSach";
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        string sh = GiaiCot(rd, 0);
                        if (sh.Length > 0 && tapSoHieu.Contains(sh)) phanLoaiGoc[sh] = GiaiCot(rd, 1);
                    }
                }
                // 3) Bảng Ba nhất: SoHieu -> ID
                var idBaNhat = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT ID, SoHieu FROM [{bang}]";
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        int id = rd.GetInt32(0);
                        if (id == -1) continue;
                        string sh = GiaiCot(rd, 1);
                        if (sh.Length > 0 && tapSoHieu.Contains(sh)) idBaNhat[sh] = id;
                    }
                }
                // 4) Đối chiếu + cập nhật
                using var tx = cn.BeginTransaction();
                using var upd = cn.CreateCommand();
                upd.Transaction = tx;
                upd.CommandText = $"UPDATE [{bang}] SET DeNghi = @dn, ThanhTich = @tt WHERE ID = @id";
                var pDn = upd.Parameters.Add("@dn", SqliteType.Text);
                var pTt = upd.Parameters.Add("@tt", SqliteType.Text);
                var pId = upd.Parameters.Add("@id", SqliteType.Integer);
                int dem = 0;
                foreach (var d in dong)
                {
                    string nhan = d.Ten.Length > 0 ? d.Ten : d.SoHieu;
                    try
                    {
                        if (!phanLoaiGoc.TryGetValue(d.SoHieu, out string? pl)) { dsKhongCoSoHieu.Add(nhan); continue; }
                        if (!pl.Equals("Loại 1", StringComparison.OrdinalIgnoreCase)) { dsKhongPhaiLoai1.Add(nhan); continue; }
                        if (!idBaNhat.TryGetValue(d.SoHieu, out int id)) { dsChuaDongBo.Add(nhan); continue; }
                        pId.Value = id;
                        pDn.Value = Module_BaoMatAES.MaHoa(d.DeNghi);
                        pTt.Value = Module_BaoMatAES.MaHoa(d.ThanhTich);
                        if (upd.ExecuteNonQuery() > 0) dem++;
                        else dsLoiKyThuat.Add(nhan + " (Không rõ nguyên nhân DB)");
                    }
                    catch (Exception ex)
                    {
                        dsLoiKyThuat.Add($"{nhan} ({ex.Message})");
                    }
                }
                tx.Commit();
                soThanhCong = dem;
            });
            if (loiCauTruc != null)
            {
                HienThongBao(parentForm, loiCauTruc, tieuDeLoi, MessageBoxIcon.Error);
                return 0;
            }
            int tongLoi = dsKhongCoSoHieu.Count + dsKhongPhaiLoai1.Count + dsChuaDongBo.Count + dsLoiKyThuat.Count;
            if (tongLoi > 0)
            {
                var mt = new List<string> { "⚠️ Quá trình nạp dữ liệu hoàn tất nhưng một số trường hợp bị từ chối do vi phạm ràng buộc:\n" };
                if (dsKhongPhaiLoai1.Count > 0) mt.Add(GomDanhSachLoi("Không được xếp mức Loại 1.", dsKhongPhaiLoai1, 30));
                if (dsKhongCoSoHieu.Count > 0) mt.Add(GomDanhSachLoi("Không tìm thấy Số hiệu quân số trong CSDL chính.", dsKhongCoSoHieu, 20));
                if (dsChuaDongBo.Count > 0) mt.Add(GomDanhSachLoi("Đạt Loại 1 nhưng chưa được đồng bộ sang hệ thống Sổ Vàng (Vui lòng bấm 'Làm mới' hệ thống trước khi nạp lại).", dsChuaDongBo, 20));
                if (dsLoiKyThuat.Count > 0)
                    mt.Add($"📌 Lỗi kỹ thuật với các CBCS: {string.Join("; ", dsLoiKyThuat.Take(15))}{(dsLoiKyThuat.Count > 15 ? "..." : "")}");
                HienThongBao(parentForm, string.Join("\n", mt), "Báo cáo đối chiếu dữ liệu", MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[Lỗi NhapDanhSachGoc] " + ex.Message);
            MessageBox.Show("Lỗi hệ thống khi đọc file Excel: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        return soThanhCong;
    }
    // =====================================================================================================
    //  SỔ VÀNG
    // =====================================================================================================
    public static Task<List<SoVangDTO>> LayDuLieuSoVangTienTienAsync(string tenBang)
        => Task.Run(() => DocSoVang(tenBang));
    private static List<SoVangDTO> DocSoVang(string tenBang)
    {
        var kq = new List<SoVangDTO>();
        try
        {
            if (!BangSoVangHopLe.Contains(tenBang))
            {
                Debug.WriteLine("[DocSoVang] Tên bảng không hợp lệ: " + tenBang);
                return kq;
            }
            string db = Module_DanduongGPS.DuongDanCSDL2;
            if (string.IsNullOrWhiteSpace(db) || !File.Exists(db)) return kq;
            using var cn = MoKetNoi(db, true);
            using var cmd = cn.CreateCommand();
            cmd.CommandText = $"SELECT ID, HoVaTen, ThongBaoTrungDoan, SoTTTrongSo, ThangCongNhan FROM [{tenBang}] ORDER BY STT ASC";
            using var rd = cmd.ExecuteReader();
            int stt = 1;
            while (rd.Read())
            {
                if (rd.IsDBNull(0) || rd.GetInt32(0) == -1) continue;
                // Giải mã tuần tự: GiaiMa đã có cache + động cơ AES theo luồng, PLINQ trên máy ít nhân chỉ tốn thêm chi phí.
                kq.Add(new SoVangDTO
                {
                    STT = stt++,
                    HoVaTen = GiaiCot(rd, 1),
                    ThongBaoTrungDoan = GiaiCot(rd, 2),
                    SoTTTrongSo = GiaiCot(rd, 3),
                    ThangCongNhan = GiaiCot(rd, 4)
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[Lỗi LayDuLieuSoVangTienTienAsync]: " + ex.Message);
            kq.Clear();
        }
        return kq;
    }
    public static async Task XuatDanhSachSoVangToExcelAsync(Form? parentForm, string filePath, string tenBangHienTai, string kyHieuTrungDoan)
    {
        ManHinhCho? cho = null;
        try
        {
            cho = new ManHinhCho(parentForm, "Đang xử lý và xuất dữ liệu ra Excel...");
            await cho.HienAsync();
            var danhSach = await LayDuLieuSoVangTienTienAsync(tenBangHienTai);
            if (danhSach.Count == 0)
            {
                cho.Dispose();
                cho = null;
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string tieuDeThongBao = $"Thông báo của {kyHieuTrungDoan}";
            await Task.Run(() =>
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("DANH_SACH");
                ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
                ws.PageSetup.PageOrientation = XLPageOrientation.Portrait;
                foreach (var (addr, text, size) in new[]
                {
                    ("A1:E1", "SỔ VÀNG", 14),
                    ("A2:E2", "BIỂU DƯƠNG GƯƠNG ĐIỂN HÌNH TRONG THỰC HIỆN PHONG TRÀO THI ĐUA BA NHẤT", 12)
                })
                {
                    var r = ws.Range(addr);
                    r.Merge();
                    r.FirstCell().Value = text;
                    r.Style.Font.Bold = true;
                    r.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                    r.Style.Font.FontSize = size;
                    r.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    r.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                string[] tieuDe = { "STT", "Họ và tên", tieuDeThongBao, "Vào sổ vàng số", "Ghi chú" };
                GhiTieuDeBang(ws, 4, tieuDe, new double[] { 5, 30, 40, 15, 15 }, 0, 24, XLColor.LightGray);
                var dong = new List<object[]>(danhSach.Count);
                foreach (var x in danhSach)
                    dong.Add(new object[] { x.STT, x.HoVaTen, x.ThongBaoTrungDoan, x.SoTTTrongSo, x.ThangCongNhan });
                GhiThanBang(ws, 5, dong, 5, new[] { 2, 3 }, new[] { 4, 5 }, 11, 18);
                Module_BanQuyen.DongDauExcel(wb);
                LuuTepAnToan(wb, filePath);
            });
            cho.Dispose();
            cho = null;
            MoTepExcelThiDuaPhongTrao3Nhat(filePath);
        }
        catch (Exception ex)
        {
            cho?.Dispose();
            cho = null;
            MessageBox.Show($"Lỗi khi xuất Excel: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            cho?.Dispose();
        }
    }
    // =====================================================================================================
    //  NHẬP DỮ LIỆU VÀO SỔ VÀNG TỪ EXCEL (Form 44)
    //  SỬA LỖI: bản cũ tra "SoHieu = MaHoa(soHieu)" (IV ngẫu nhiên => không bao giờ khớp) nên mỗi lần nạp lại
    //  đều INSERT thêm dòng trùng. Nay tra bằng Dictionary(SoHieu đã giải mã -> ID) và cập nhật theo ID.
    // =====================================================================================================
    private readonly record struct DongSoVang(int Dong, string HoTen, string SoHieu, string[] GiaTri);
    public static async Task<int> NhapDuLieuVaoSoVangTuExcel(Form? parentForm, string excelPath, string dbPath)
    {
        int soThanhCong = 0;
        try
        {
            if (!File.Exists(excelPath) || !File.Exists(dbPath)) return 0;
            string bang = ChonBang().SoVang;
            var dsBoQua = new List<string>();
            var dsLoiKyThuat = new List<string>();
            string? loiCauTruc = null;
            await Task.Run(() =>
            {
                loiCauTruc = KiemTraKichThuocTep(excelPath);
                if (loiCauTruc != null) return;
                using var wb = new XLWorkbook(excelPath);
                var ws = wb.Worksheets.FirstOrDefault(w => w.Name.Contains("SO_VANG") || w.Name.Contains("BANHAT"))
                         ?? wb.Worksheets.First();
                // Dò dòng tiêu đề (dòng 1..5): cột C phải là "Số hiệu" – "chìa khóa" nhận diện cán bộ
                int headerRow = 0;
                for (int i = 1; i <= 5; i++)
                {
                    if (ws.Cell(i, 3).GetString().Trim().Equals("Số hiệu", StringComparison.OrdinalIgnoreCase)) { headerRow = i; break; }
                }
                if (headerRow == 0)
                {
                    loiCauTruc = "File Excel không đúng định dạng!\nĐể nạp được dữ liệu, Cột C (Cột số 3) bắt buộc phải là 'Số hiệu'.\n(Đây là chìa khóa để nhận diện cán bộ)";
                    return;
                }
                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                if (lastRow <= headerRow) return;
                if (lastRow > DongExcelToiDa)
                {
                    loiCauTruc = $"Tệp có quá nhiều dòng (>{DongExcelToiDa}).";
                    return;
                }
                // 1) Đọc Excel vào RAM (cột 4..15 -> 12 giá trị; HoTen = cột 2)
                var dong = new List<DongSoVang>();
                for (int r = headerRow + 1; r <= lastRow; r++)
                {
                    var row = ws.Row(r);
                    string hoTen = Chuoi(row.Cell(2));
                    string soHieu = Chuoi(row.Cell(3));
                    if (hoTen.Length == 0 && soHieu.Length == 0) continue;
                    if (hoTen.Length == 0 || soHieu.Length == 0)
                    {
                        dsBoQua.Add($"- Dòng {r}: thiếu {(hoTen.Length == 0 ? "Họ tên" : "Số hiệu")}, bỏ qua.");
                        continue;
                    }
                    // Thứ tự khớp CotSoVang: HoVaTen, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu, ThanhTich, ThongBao, SoTT, Thang
                    dong.Add(new DongSoVang(r, hoTen, soHieu, new[]
                    {
                        hoTen, Chuoi(row.Cell(4)), Chuoi(row.Cell(5)), Chuoi(row.Cell(6)), Chuoi(row.Cell(7)), Chuoi(row.Cell(8)),
                        Chuoi(row.Cell(9)), Chuoi(row.Cell(10)), Chuoi(row.Cell(11)), Chuoi(row.Cell(12)), Chuoi(row.Cell(13)),
                        Chuoi(row.Cell(14)), Chuoi(row.Cell(15))
                    }));
                }
                if (dong.Count == 0) return;
                using var cn = MoKetNoi(dbPath, false);
                // 2) SoHieu hiện có -> ID (giải mã 1 lần)
                var idTheoSoHieu = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT ID, SoHieu FROM [{bang}]";
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        int id = rd.GetInt32(0);
                        if (id == -1) continue;
                        string sh = GiaiCot(rd, 1);
                        if (sh.Length > 0) idTheoSoHieu[sh] = id;
                    }
                }
                int nextSTT;
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT IFNULL(MAX(STT), 0) FROM [{bang}]";
                    nextSTT = Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) + 1;
                }
                // 3) Ghi
                using var tx = cn.BeginTransaction();
                using var upd = TaoLenhCapNhat(cn, tx, bang, CotSoVang);
                using var ins = TaoLenhThem(cn, tx, bang, CotSoVang);
                var daXuLy = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int dem = 0;
                foreach (var d in dong)
                {
                    try
                    {
                        if (!daXuLy.Add(d.SoHieu))
                        {
                            dsBoQua.Add($"- {d.HoTen} (Dòng {d.Dong}): trùng Số hiệu {d.SoHieu} trong tệp, bỏ qua.");
                            continue;
                        }
                        if (idTheoSoHieu.TryGetValue(d.SoHieu, out int id))
                        {
                            upd.Parameters["@id"].Value = id;
                            GanThamSo(upd, d.GiaTri, CotSoVang.Length);
                            upd.ExecuteNonQuery();
                        }
                        else
                        {
                            ins.Parameters["@stt"].Value = nextSTT++;
                            ins.Parameters["@sh"].Value = Module_BaoMatAES.MaHoa(d.SoHieu);
                            GanThamSo(ins, d.GiaTri, CotSoVang.Length);
                            ins.ExecuteNonQuery();
                        }
                        dem++;
                    }
                    catch (Exception ex)
                    {
                        dsLoiKyThuat.Add($"- {d.HoTen} (Dòng {d.Dong}): {ex.Message}");
                    }
                }
                tx.Commit();
                soThanhCong = dem;
            });
            if (loiCauTruc != null)
            {
                HienThongBao(parentForm, loiCauTruc, "Từ chối nạp", MessageBoxIcon.Error);
                return 0;
            }
            if (dsBoQua.Count > 0 || dsLoiKyThuat.Count > 0)
            {
                var mt = new List<string> { "⚠️ Quá trình nạp hoàn tất nhưng có lỗi tại một số dòng:\n" };
                if (dsBoQua.Count > 0) mt.Add(string.Join("\n", dsBoQua.Take(10)) + (dsBoQua.Count > 10 ? "\n..." : ""));
                if (dsLoiKyThuat.Count > 0) mt.Add(string.Join("\n", dsLoiKyThuat.Take(10)) + (dsLoiKyThuat.Count > 10 ? "\n..." : ""));
                HienThongBao(parentForm, string.Join("\n", mt), "Cảnh báo", MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[Lỗi NhapSoVang] " + ex.Message);
            MessageBox.Show("Lỗi định dạng tệp Excel: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        return soThanhCong;
    }
    // =====================================================================================================
    //  XUẤT DỮ LIỆU GỐC SỔ VÀNG (15 cột) để sao lưu / nạp lại (Form 44)
    // =====================================================================================================
    public static async Task XuatDuLieuGocSoVangRaExcelAsync(Form? parentForm, string filePathLuu)
    {
        string dbPath = Module_DanduongGPS.DuongDanCSDL2;
        if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return;
        string tenBang = ChonBang().SoVang;
        ManHinhCho? cho = null;
        try
        {
            cho = new ManHinhCho(parentForm, "Đang đọc và tạo tệp excel Sổ Vàng, vui lòng đợi...");
            await cho.HienAsync();
            // Đọc + giải mã + tạo Excel trong 1 luồng nền (không tách 2 lần Task.Run)
            await Task.Run(() =>
            {
                var dong = new List<object[]>();
                using (var cn = MoKetNoi(dbPath, true))
                {
                    using var cmd = cn.CreateCommand();
                    cmd.CommandText = $"SELECT ID, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu, ThanhTich, ThongBaoTrungDoan, SoTTTrongSo, ThangCongNhan FROM [{tenBang}] ORDER BY STT ASC";
                    using var rd = cmd.ExecuteReader();
                    int stt = 1;
                    while (rd.Read())
                    {
                        if (rd.IsDBNull(0) || rd.GetInt32(0) == -1) continue;
                        var o = new object[15];
                        o[0] = stt++;
                        for (int i = 1; i < 15; i++) o[i] = GiaiCot(rd, i);
                        dong.Add(o);
                    }
                }
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("DS_SO_VANG_CSDL");
                GhiTieuDeBang(ws, 1, TieuDeSoVangGoc, DoRongSoVangGoc, 13, 36, MauNenTieuDe);
                GhiThanBang(ws, 2, dong, 15, CotTraiSoVangGoc, CotVanBanSoVangGoc, 13, 36);
                Module_BanQuyen.DongDauExcel(wb);
                LuuTepAnToan(wb, filePathLuu);
            });
            Module_NhatKy.GhiNhatKy(
                taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                hanhDong: $"Xuất tệp excel dữ liệu gốc Sổ Vàng ({tenBang})",
                ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
            cho.Dispose();
            cho = null;
            MoTepExcelThiDuaPhongTrao3Nhat(filePathLuu);
        }
        catch (Exception ex)
        {
            cho?.Dispose();
            cho = null;
            MessageBox.Show("Lỗi khi xuất file Sổ vàng gốc: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            cho?.Dispose();
        }
    }
}