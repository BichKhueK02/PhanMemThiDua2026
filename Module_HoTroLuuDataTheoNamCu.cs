using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using PhanMemThiDua2026;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PhanMemThiDua2026
{
    public class FileLichSuDTO
    {
        public string TenHienThi { get; set; }
        public string DuongDan { get; set; }
        public bool LaTanBinh { get; set; }
    }
    public class HistoryCBCSDTO
    {
        public string ID, HoVaTen, SoHieu, DonVi, TinhTrang;
        public string KQ_TD, KQ_XL_CB, KQ_XL_DV, T12;
        public string[] Thang = new string[11]; // Thang_1 -> Thang_11
        public string Sau_Thang_Dau_Nam, TongKet_Nam, TS_Loai1, TS_Loai2, TS_Loai3, TS_Loai4;
        public string GhiChu;

        // Thuộc tính phục vụ thuật toán tìm kiếm và sắp xếp
        public string HoVaTen_Search;
        public int SortPriority;
        public Dictionary<string, string> CotPhatSinh = new Dictionary<string, string>();
    }
    public class HistoryTanBinhDTO
    {
        public string ID, HoVaTen, SoHieu, DonVi, TinhTrang;
        public string TS_Loai1, TS_Loai2, TS_Loai3, TS_Loai4;
        public string GhiChu;

        // Thuộc tính phục vụ thuật toán tìm kiếm và sắp xếp
        public string HoVaTen_Search;
        public int SortPriority;
        public Dictionary<string, string> DuLieuThang = new Dictionary<string, string>(); // Lưu tuần và kết quả tháng
        public Dictionary<string, string> CotPhatSinh = new Dictionary<string, string>();
    }
    internal static class Module_HoTroLuuDataTheoNamCu
    {      
        public static List<FileLichSuDTO> LayDanhSachFileLichSu()
        {
            var danhSach = new List<FileLichSuDTO>();
            string dir = Module_DanduongGPS.ThuMucLichSuThiDua;

            if (!Directory.Exists(dir)) return danhSach;

            bool phienBanHienTaiLaTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);

            var files = Directory.GetFiles(dir, "*.db");
            foreach (var file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                bool isFileTanBinh = fileName.Contains("TanBinh", StringComparison.OrdinalIgnoreCase);

                if (phienBanHienTaiLaTanBinh != isFileTanBinh) continue;

                string nam = fileName.Split(new string[] { "Nam" }, StringSplitOptions.None).LastOrDefault() ?? "???";

                danhSach.Add(new FileLichSuDTO
                {
                    TenHienThi = $"Năm {nam} - {(isFileTanBinh ? "Tân binh" : "CBCS")}",
                    DuongDan = file,
                    LaTanBinh = isFileTanBinh
                });
            }
            return danhSach.OrderByDescending(x => x.TenHienThi).ToList();
        }
        public static DataTable LoadDataFromHistoryDB(string dbPath, string tableName)
        {
            DataTable dt = new DataTable();
            if (!File.Exists(dbPath)) return dt;

            try
            {
                using (var cn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly"))
                {
                    cn.Open();
                    using var cmd = cn.CreateCommand();
                    cmd.CommandText = $"SELECT * FROM [{tableName}]";
                    using var rd = cmd.ExecuteReader();
                    dt.Load(rd);
                }

                // ⭐ KHỐI VÁ CHUẨN: Chỉ giải mã chính xác 3 cột bảo mật hệ thống
                string[] secureCols = { "HoVaTen", "SoHieu", "DonVi" };

                foreach (DataRow row in dt.Rows)
                {
                    foreach (string colName in secureCols)
                    {
                        if (dt.Columns.Contains(colName) && row[colName] != DBNull.Value)
                        {
                            string value = row[colName].ToString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                try
                                {
                                    string decrypted = BaoMatAES.GiaiMa(value);
                                    row[colName] = string.IsNullOrEmpty(decrypted) ? value : decrypted;
                                }
                                catch
                                {
                                    // Giữ nguyên giá trị gốc nếu không phải định dạng mã hóa hoặc lỗi khóa
                                }
                            }
                        }
                    }
                }
                Debug.WriteLine($"Load thành công CSDL năm cũ: {dt.Rows.Count} dòng, {dt.Columns.Count} cột.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi Load DB Lịch sử: {ex.Message}");
            }
            return dt;
        }
        // ⭐ HÀM GIẢI MÃ AN TOÀN TRÁNH BỎ LỖI DỰ ÁN
        private static string SafeDecrypt(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            try
            {
                string result = BaoMatAES.GiaiMa(input);
                return string.IsNullOrEmpty(result) ? input : result;
            }
            catch { return input; }
        }
        public static string LuuTruDuLieuThiDuaNam()
        {
            int nam = Module_NamHeThong.LayNamHeThong();
            bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
            string tableName = laTanBinh ? "ThiDuaThang_TanBinh" : "ThiDuaThang";
            string fileName = $"ThiDua_{(laTanBinh ? "TanBinh" : "CBCS")}_Nam{nam}.db";
            string thuMucLuu = Module_DanduongGPS.ThuMucLichSuThiDua;
            string targetPath = Path.Combine(thuMucLuu, fileName);

            if (!File.Exists(Module_DanduongGPS.DuongDanCSDL4))
                throw new FileNotFoundException("Không tìm thấy cơ sở dữ liệu nguồn.");

            Directory.CreateDirectory(thuMucLuu);
            if (File.Exists(targetPath))
                throw new InvalidOperationException($"Dữ liệu lưu trữ năm {nam} đã tồn tại.");

            bool attachThanhCong = false;
            try
            {
                using var cn = new SqliteConnection($"Data Source={Module_DanduongGPS.DuongDanCSDL4}");
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA busy_timeout=5000;";
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@TenBang;";
                    cmd.Parameters.AddWithValue("@TenBang", tableName);
                    if ((long)cmd.ExecuteScalar() == 0)
                        throw new Exception($"Không tìm thấy bảng [{tableName}] trong CSDL.");
                }

                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = $"ATTACH DATABASE '{targetPath.Replace("'", "''")}' AS NamCu;";
                    cmd.ExecuteNonQuery();
                }
                attachThanhCong = true;

                using var tran = cn.BeginTransaction();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = $"CREATE TABLE NamCu.{tableName} AS SELECT * FROM main.{tableName};";
                    cmd.ExecuteNonQuery();
                }
                tran.Commit();
            }
            catch
            {
                try { if (File.Exists(targetPath)) File.Delete(targetPath); } catch { }
                throw;
            }
            finally
            {
                if (attachThanhCong)
                {
                    try
                    {
                        using var cn = new SqliteConnection($"Data Source={Module_DanduongGPS.DuongDanCSDL4}");
                        cn.Open();
                        using var cmd = cn.CreateCommand();
                        cmd.CommandText = "DETACH DATABASE NamCu;";
                        cmd.ExecuteNonQuery();
                    }
                    catch { }
                }
            }
            return targetPath;
        }
        public static void XuatExcelLichSuCore(
        string targetExcelPath,
        bool laTanBinh,
        bool isDataMaxMode,
        List<ColumnExportMeta> exportCols,
        List<int> filteredIndexes,
        DataTable dtSource,
        List<HistoryCBCSDTO> cacheCBCS,
        List<HistoryTanBinhDTO> cacheTanBinh,
        string tenTieuDoan)
        {
            int rowCount = filteredIndexes.Count;
            int colCount = exportCols.Count + 1;
            var dataArray = new object[rowCount, colCount];
            for (int r = 0; r < rowCount; r++)
            {
                int cIndex = 0;
                dataArray[r, cIndex++] = r + 1; // STT tự động sinh

                int actualIndex = filteredIndexes[r];

                foreach (var col in exportCols)
                {
                    string cellValue = "";
                    if (isDataMaxMode)
                    {
                        if (laTanBinh)
                        {
                            var data = cacheTanBinh[actualIndex];
                            if (col.Name == "HoVaTen") cellValue = data.HoVaTen;
                            else if (col.Name == "SoHieu") cellValue = data.SoHieu;
                            else if (col.Name == "DonVi") cellValue = data.DonVi;
                            else if (col.Name == "TinhTrang") cellValue = data.TinhTrang;
                            else if (col.Name == "GhiChu") cellValue = data.GhiChu;
                            else if (col.Name == "TS_Loai1") cellValue = data.TS_Loai1;
                            else if (col.Name == "TS_Loai2") cellValue = data.TS_Loai2;
                            else if (col.Name == "TS_Loai3") cellValue = data.TS_Loai3;
                            else if (col.Name == "TS_Loai4") cellValue = data.TS_Loai4;
                            // ⭐ ĐỒNG BỘ CHÍNH XÁC TÊN BIẾN RA: dThang và dynVal
                            else if (data.DuLieuThang.TryGetValue(col.Name, out string dThang)) cellValue = dThang;
                            else if (data.CotPhatSinh.TryGetValue(col.Name, out string dynVal)) cellValue = dynVal;
                        }
                        else
                        {
                            var data = cacheCBCS[actualIndex];
                            if (col.Name == "HoVaTen") cellValue = data.HoVaTen;
                            else if (col.Name == "SoHieu") cellValue = data.SoHieu;
                            else if (col.Name == "DonVi") cellValue = data.DonVi;
                            else if (col.Name == "TinhTrang") cellValue = data.TinhTrang;
                            else if (col.Name == "GhiChu") cellValue = data.GhiChu;
                            else if (col.Name == "KQ_ThiDua_Nam_Cu") cellValue = data.KQ_TD;
                            else if (col.Name == "KQ_XepLoaiCB_Nam_Cu") cellValue = data.KQ_XL_CB;
                            else if (col.Name == "KQ_XepLoaiDangVien_Nam_Cu") cellValue = data.KQ_XL_DV;
                            else if (col.Name == "Thang_12_Nam_Cu") cellValue = data.T12;
                            else if (col.Name == "Sau_Thang_Dau_Nam") cellValue = data.Sau_Thang_Dau_Nam;
                            else if (col.Name == "TongKet_Nam") cellValue = data.TongKet_Nam;
                            else if (col.Name == "TS_Loai1") cellValue = data.TS_Loai1;
                            else if (col.Name == "TS_Loai2") cellValue = data.TS_Loai2;
                            else if (col.Name == "TS_Loai3") cellValue = data.TS_Loai3;
                            else if (col.Name == "TS_Loai4") cellValue = data.TS_Loai4;
                            else if (col.Name.StartsWith("Thang_") && int.TryParse(col.Name.Replace("Thang_", ""), out int tIdx)) cellValue = data.Thang[tIdx - 1];
                            // ⭐ ĐỒNG BỘ CHÍNH XÁC TÊN BIẾN RA: dynVal
                            else if (data.CotPhatSinh.TryGetValue(col.Name, out string dynVal)) cellValue = dynVal;
                        }
                    }
                    else
                    {
                        if (dtSource != null && dtSource.Columns.Contains(col.Name))
                            cellValue = dtSource.DefaultView[actualIndex][col.Name]?.ToString() ?? "";
                    }
                    dataArray[r, cIndex++] = cellValue;
                }
            }

            // [Toàn bộ khối tạo File XLWorkbook và định dạng giữ nguyên như đã cấu hình xịn mịn ở phiên trước]
            using (var wb = new ClosedXML.Excel.XLWorkbook())
            {
                var ws = wb.Worksheets.Add("ThongKeThiDua");
                ws.Cell("A1").Value = "DANH SÁCH";
                ws.Range(1, 1, 1, colCount).Merge().Style.Font.SetBold().Font.SetFontSize(16).Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center).Alignment.SetVertical(ClosedXML.Excel.XLAlignmentVerticalValues.Center);
                ws.Cell("A2").Value = laTanBinh ? $"THỐNG KÊ PHÂN LOẠI THI ĐUA CỦA TÂN BINH {tenTieuDoan}" : $"THỐNG KÊ PHÂN LOẠI THI ĐUA CỦA CBCS {tenTieuDoan}";
                ws.Range(2, 1, 2, colCount).Merge().Style.Font.SetBold().Font.SetFontSize(12).Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center).Alignment.SetVertical(ClosedXML.Excel.XLAlignmentVerticalValues.Center);

                // 1. Vẽ tiêu đề STT
                int excelStartRow = 4;
                var cellStt = ws.Cell(excelStartRow, 1);
                cellStt.Value = "STT";
                cellStt.Style.Font.Bold = true;
                cellStt.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                cellStt.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                cellStt.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

                // ⭐ BỔ SUNG MỚI: Căn giữa toàn bộ cột A (từ dòng 4 đến dòng cuối)
                ws.Column(1).Width = 5; // Độ rộng nhỏ lại tí
                var rangeColA = ws.Range(excelStartRow, 1, rowCount + excelStartRow, 1);
                rangeColA.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                rangeColA.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;

                int excelCol = 2;
                foreach (var col in exportCols)
                {
                    var cell = ws.Cell(excelStartRow, excelCol);
                    cell.Value = string.IsNullOrWhiteSpace(col.HeaderText) ? col.Name.Replace("_", " ") : col.HeaderText;
                    // ⭐ ĐỊNH DẠNG ĐỘ RỘNG CỘT THEO YÊU CẦU
                    if (col.HeaderText.Contains("Họ và tên")) ws.Column(excelCol).Width = 30;
                    else if (excelCol == 5) ws.Column(excelCol).Width = 20; // Cột thứ 5 là cột E
                    else ws.Column(excelCol).Width = 15;
                    cell.Style.Font.Bold = true; cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center; cell.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center; cell.Style.Alignment.WrapText = true; cell.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                    if (laTanBinh && (col.Name == "Tuan_1_T2" || col.Name == "Tuan_2_T2" || col.Name == "Tuan_3_T2" || col.Name == "Tuan_4_T2" || col.Name == "Thang_3" || col.Name == "Tuan_1_T4" || col.Name == "Tuan_2_T4" || col.Name == "Tuan_3_T4" || col.Name == "Tuan_4_T4" || col.Name == "Thang_5")) cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(220, 235, 255);
                    else if (col.Name.StartsWith("TS_Loai")) cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(230, 250, 230);
                    else cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                    excelCol++;
                }
                for (int r = 0; r < rowCount; r++)
                    for (int c = 0; c < colCount; c++)
                        ws.Cell(r + excelStartRow + 1, c + 1).Value = dataArray[r, c]?.ToString() ?? "";
                if (rowCount > 0)
                {
                    var dataRange = ws.Range(excelStartRow + 1, 1, excelStartRow + rowCount, colCount);
                    dataRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin; dataRange.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin; dataRange.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center; dataRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                    ws.Range(excelStartRow + 1, 1, excelStartRow + rowCount, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;
                    int hoTenColIndex = exportCols.FindIndex(c => c.Name.Equals("HoVaTen", StringComparison.OrdinalIgnoreCase));
                    if (hoTenColIndex >= 0) ws.Range(excelStartRow + 1, hoTenColIndex + 2, excelStartRow + rowCount, hoTenColIndex + 2).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;
                }
                int tongDong = rowCount + excelStartRow + 1;
                var totalCell = ws.Cell(tongDong, 1); totalCell.Value = $"Tổng cộng: {rowCount} đồng chí./."; totalCell.Style.Font.SetBold().Font.SetItalic(); ws.Range(tongDong, 1, tongDong, colCount).Merge();
                Module_BanQuyen.DongDauExcel(wb);
                wb.SaveAs(targetExcelPath);
            }
        }
        public static void CapNhatTinhTrangThiDuaNamCu(string pathCsdl2, string pathCsdlNamCu, bool laTanBinh)
        {
            if (string.IsNullOrEmpty(pathCsdlNamCu) || !File.Exists(pathCsdlNamCu)) return;
            if (!File.Exists(pathCsdl2)) return;

            string tableDich = laTanBinh ? "ThiDuaThang_TanBinh" : "ThiDuaThang";

            try
            {
                // -----------------------------------------------------------------
                // BƯỚC 1: Đọc toàn bộ danh sách SoHieu hiện tại từ CSDL2 (Giải mã AES ra Plaintext)
                // -----------------------------------------------------------------
                var hashSoHieuCsdl2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                using (var cn2 = new SqliteConnection($"Data Source={pathCsdl2};Mode=ReadOnly"))
                {
                    cn2.Open();
                    using var cmd = new SqliteCommand("SELECT SoHieu FROM DanhSach", cn2);
                    using var rd = cmd.ExecuteReader();

                    while (rd.Read())
                    {
                        string encSh = rd["SoHieu"]?.ToString() ?? "";
                        string plainSh = SafeDecrypt(encSh)?.Trim();

                        if (!string.IsNullOrEmpty(plainSh))
                        {
                            hashSoHieuCsdl2.Add(plainSh);
                        }
                    }
                }

                // -----------------------------------------------------------------
                // BƯỚC 2: Đọc tệp CSDL Năm Cũ (Đang chọn trên Form46) & Phân loại ID
                // -----------------------------------------------------------------
                var idDangCongTac = new List<int>();
                var idChuyenCongTac = new List<int>();

                using (var cnNamCu = new SqliteConnection($"Data Source={pathCsdlNamCu};Mode=ReadOnly"))
                {
                    cnNamCu.Open();
                    using var cmd = new SqliteCommand($"SELECT ID, SoHieu, TinhTrang FROM [{tableDich}]", cnNamCu);
                    using var rd = cmd.ExecuteReader();

                    while (rd.Read())
                    {
                        int id = Convert.ToInt32(rd["ID"]);
                        string encSh = rd["SoHieu"]?.ToString() ?? "";
                        string plainSh = SafeDecrypt(encSh)?.Trim();
                        string ttHienTai = rd["TinhTrang"]?.ToString()?.Trim() ?? "";

                        // Kiểm tra SoHieu năm cũ có nằm trong CSDL2 hiện tại không
                        bool tonTaiInCsdl2 = !string.IsNullOrEmpty(plainSh) && hashSoHieuCsdl2.Contains(plainSh);

                        if (tonTaiInCsdl2)
                        {
                            if (ttHienTai != "Đang công tác") idDangCongTac.Add(id);
                        }
                        else
                        {
                            if (ttHienTai != "Chuyển công tác") idChuyenCongTac.Add(id);
                        }
                    }
                }

                // -----------------------------------------------------------------
                // BƯỚC 3: Cập nhật trực tiếp chuỗi Text thuần vào cột TinhTrang
                // -----------------------------------------------------------------
                if (idDangCongTac.Count == 0 && idChuyenCongTac.Count == 0) return;

                using (var cnNamCu = new SqliteConnection($"Data Source={pathCsdlNamCu}"))
                {
                    cnNamCu.Open();
                    using var tran = cnNamCu.BeginTransaction();
                    try
                    {
                        // A. Cập nhật "Đang công tác"
                        if (idDangCongTac.Count > 0)
                        {
                            using var cmdUpd = new SqliteCommand($"UPDATE [{tableDich}] SET TinhTrang = 'Đang công tác' WHERE ID = @id", cnNamCu, tran);
                            cmdUpd.Parameters.Add("@id", SqliteType.Integer);
                            foreach (int id in idDangCongTac)
                            {
                                cmdUpd.Parameters["@id"].Value = id;
                                cmdUpd.ExecuteNonQuery();
                            }
                        }

                        // B. Cập nhật "Chuyển công tác"
                        if (idChuyenCongTac.Count > 0)
                        {
                            using var cmdUpd = new SqliteCommand($"UPDATE [{tableDich}] SET TinhTrang = 'Chuyển công tác' WHERE ID = @id", cnNamCu, tran);
                            cmdUpd.Parameters.Add("@id", SqliteType.Integer);
                            foreach (int id in idChuyenCongTac)
                            {
                                cmdUpd.Parameters["@id"].Value = id;
                                cmdUpd.ExecuteNonQuery();
                            }
                        }

                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi cập nhật TinhTrang CSDL năm cũ: " + ex.Message);
            }
        }
        // Thêm vào trong class Module_HoTroLuuDataTheoNamCu
        public static void CapNhatTinhTrangLichSuTuDanhSachGoc(string pathCsdl2, string pathCsdlNamCu, bool laTanBinh)
        {
            // 1. CHỐT CHẶN AN TOÀN I/O
            if (string.IsNullOrEmpty(pathCsdlNamCu) || !File.Exists(pathCsdlNamCu)) return;
            if (string.IsNullOrEmpty(pathCsdl2) || !File.Exists(pathCsdl2)) return;
            string tableDich = laTanBinh ? "ThiDuaThang_TanBinh" : "ThiDuaThang";
            try
            {
                // =========================================================================
                // BƯỚC 1: RÚT TRÍCH SỐ HIỆU GỐC (TỐI ƯU BỘ NHỚ RAM)
                // =========================================================================
                // Kỹ thuật 1: Khởi tạo sẵn dung lượng (Capacity) cho HashSet là 10.000 
                // -> Chống phân mảnh RAM (Garbage Collection Spikes) khi danh sách lớn.
                var hashSoHieuCsdl2 = new HashSet<string>(10000, StringComparer.OrdinalIgnoreCase);

                // Kỹ thuật 2: Thêm Cache=Shared để tăng tốc độ đọc từ đĩa
                using (var cn2 = new SqliteConnection($"Data Source={pathCsdl2};Mode=ReadOnly;Cache=Shared"))
                {
                    cn2.Open();
                    // Kỹ thuật 3: Lọc Null ngay từ câu lệnh SQL để giảm tải cho C#
                    using var cmd = new SqliteCommand("SELECT SoHieu FROM DanhSach WHERE SoHieu IS NOT NULL AND SoHieu <> ''", cn2);
                    using var rd = cmd.ExecuteReader();

                    while (rd.Read())
                    {
                        // Kỹ thuật 4: Dùng GetString(0) nhanh và tốn ít chu kỳ CPU hơn rd["SoHieu"].ToString()
                        string rawSh = rd.GetString(0);
                        string plainSh = SafeDecrypt(rawSh);

                        if (!string.IsNullOrWhiteSpace(plainSh))
                        {
                            hashSoHieuCsdl2.Add(plainSh.Trim());
                        }
                    }
                }

                if (hashSoHieuCsdl2.Count == 0) return; // Không có dữ liệu gốc -> Không có căn cứ đối chiếu -> Thoát.

                // =========================================================================
                // BƯỚC 2 & 3: ĐỐI CHIẾU VÀ CẬP NHẬT 1 LUỒNG (CHỐNG DEADLOCK)
                // =========================================================================
                using (var cnCu = new SqliteConnection($"Data Source={pathCsdlNamCu}"))
                {
                    cnCu.Open();

                    // Kỹ thuật 5: Ép SQLite sử dụng WAL (Write-Ahead Logging) và Normal Sync
                    // -> Chống cháy nổ CSDL khi cúp điện đột ngột và tăng tốc độ Ghi (Write) gấp 5 lần.
                    using (var cmdPragma = new SqliteCommand("PRAGMA synchronous = NORMAL; PRAGMA journal_mode = WAL;", cnCu))
                    {
                        cmdPragma.ExecuteNonQuery();
                    }

                    // Kiểm tra an toàn xem bảng có tồn tại không
                    using (var cmdCheck = new SqliteCommand("SELECT 1 FROM sqlite_master WHERE type='table' AND name=@tableName LIMIT 1;", cnCu))
                    {
                        cmdCheck.Parameters.AddWithValue("@tableName", tableDich);
                        if (cmdCheck.ExecuteScalar() == null) return;
                    }

                    // Khởi tạo trước dung lượng cho danh sách cần sửa
                    var updateQueue = new List<(int id, string ttMoi)>(10000);

                    using (var cmdSelect = new SqliteCommand($"SELECT ID, SoHieu, TinhTrang FROM [{tableDich}]", cnCu))
                    using (var rd = cmdSelect.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            int id = rd.GetInt32(0);
                            string rawSh = rd.IsDBNull(1) ? "" : rd.GetString(1);
                            string ttCu = rd.IsDBNull(2) ? "" : rd.GetString(2).Trim();

                            string plainSh = SafeDecrypt(rawSh).Trim();

                            // Thuật toán đối chiếu lõi
                            string ttMoi = (!string.IsNullOrEmpty(plainSh) && hashSoHieuCsdl2.Contains(plainSh))
                                ? "Đang công tác"
                                : "Chuyển công tác";

                            // Kỹ thuật 6: Chỉ đưa vào hàng đợi nếu thực sự có sự thay đổi
                            if (!string.Equals(ttCu, ttMoi, StringComparison.OrdinalIgnoreCase))
                            {
                                updateQueue.Add((id, ttMoi));
                            }
                        }
                    } // Phải đóng Reader trước khi nhảy vào Transaction ghi

                    // THỰC THI GHI Ổ CỨNG BẰNG GIAO DỊCH (TRANSACTION)
                    if (updateQueue.Count > 0)
                    {
                        using var tran = cnCu.BeginTransaction();
                        try
                        {
                            using var cmdUpd = new SqliteCommand($"UPDATE [{tableDich}] SET TinhTrang = @tt WHERE ID = @id", cnCu, tran);

                            // Kỹ thuật 7: Tạo Parameter 1 lần duy nhất bên ngoài vòng lặp
                            var pTt = cmdUpd.Parameters.Add("@tt", SqliteType.Text);
                            var pId = cmdUpd.Parameters.Add("@id", SqliteType.Integer);

                            // Kỹ thuật 8: PREPARE STATEMENT (Tuyệt kỹ cho dữ liệu lớn)
                            // Báo cho SQLite biên dịch sẵn câu lệnh SQL, vòng lặp bên dưới chỉ việc nạp biến.
                            cmdUpd.Prepare();

                            foreach (var item in updateQueue)
                            {
                                pTt.Value = item.ttMoi;
                                pId.Value = item.id;
                                cmdUpd.ExecuteNonQuery();
                            }
                            tran.Commit();
                        }
                        catch
                        {
                            tran.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi Cốt lõi Đồng Bộ Tình Trạng Lịch Sử: {ex.Message}");
            }
        }
    } ///Ngoài luồng  
}
