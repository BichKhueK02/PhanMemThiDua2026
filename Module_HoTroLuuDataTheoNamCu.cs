using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PhanMemThiDua2026
{
    // =======================================================================
    // KHU VỰC CÁC CLASS DTO (DATA TRANSFER OBJECT)
    // =======================================================================
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

    public class HistoryKhenThuongCBCSDTO
    {
        public int STT { get; set; }
        public string HoVaTen { get; set; }
        public string SoHieu { get; set; }
        public string DonVi { get; set; }
        public string TinhTrang { get; set; }
        public int SoLuong_Khen { get; set; }
        public string GhiChu_Khen { get; set; }
        public string DanhSachDVKhen_An { get; set; }
    }


    // =======================================================================
    // CLASS CHÍNH: XỬ LÝ LƯU TRỮ VÀ LỊCH SỬ
    // =======================================================================
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

        public static List<FileLichSuDTO> LayDanhSachFileLichSu_KhenThuongCaNhan()
        {
            var danhSach = new List<FileLichSuDTO>();

            // [CHẶN CỬA]: Tân binh không có khen thưởng -> Trả về danh sách trống luôn
            string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
            if (phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase))
                return danhSach;

            string dir = Module_DanduongGPS.ThuMucLichSuThiDua;
            if (!Directory.Exists(dir)) return danhSach;

            var files = Directory.GetFiles(dir, "KhenThuong_CBCS_Nam*.db");
            foreach (var file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                string nam = fileName.Replace("KhenThuong_CBCS_Nam", "");

                danhSach.Add(new FileLichSuDTO
                {
                    TenHienThi = $"Năm {nam}",
                    DuongDan = file,
                    LaTanBinh = false
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
                                    // Giữ nguyên giá trị gốc
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

        // =======================================================================
        // LÕI HỆ THỐNG: HÀM REVERSE ATTACH & CLONE DDL
        // GIÚP COPY 100% CẤU TRÚC (PRIMARY KEY, AUTOINCREMENT, INDEX) TỪ DB GỐC
        // =======================================================================
        private static void SaoChepBangVaDuLieuChuanXac(string sourceDbPath, string targetDbPath, string[] tableNames)
        {
            var schemaStatements = new List<string>();
            var cacBangThucTeCo = new List<string>();

            // BƯỚC 1: Lấy schema gốc (Nguyên văn câu lệnh Create Table, Index, Primary Key, AutoIncrement...)
            using (var cnSource = new SqliteConnection($"Data Source={sourceDbPath};Pooling=False"))
            {
                cnSource.Open();
                foreach (string tbl in tableNames)
                {
                    using (var cmd = cnSource.CreateCommand())
                    {
                        // Sắp xếp ưu tiên: Tạo Table trước, sau đó mới tạo Index/Trigger
                        cmd.CommandText = @"
                            SELECT sql FROM sqlite_master 
                            WHERE tbl_name = @TenBang AND sql IS NOT NULL 
                            ORDER BY CASE type WHEN 'table' THEN 1 WHEN 'index' THEN 2 WHEN 'trigger' THEN 3 ELSE 4 END ASC;";
                        cmd.Parameters.AddWithValue("@TenBang", tbl);

                        using (var reader = cmd.ExecuteReader())
                        {
                            bool hasTable = false;
                            while (reader.Read())
                            {
                                schemaStatements.Add(reader.GetString(0));
                                hasTable = true;
                            }
                            if (hasTable) cacBangThucTeCo.Add(tbl);
                        }
                    }
                }
            }

            if (cacBangThucTeCo.Count == 0)
                throw new Exception("Không tìm thấy bảng dữ liệu nào trong CSDL nguồn để sao lưu.");

            // BƯỚC 2: Khởi tạo CSDL Đích (Năm Cũ) và tái tạo cấu trúc
            using (var cnTarget = new SqliteConnection($"Data Source={targetDbPath};Pooling=False"))
            {
                cnTarget.Open();
                using (var cmd = cnTarget.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA busy_timeout=5000;";
                    cmd.ExecuteNonQuery();
                }

                // Chạy DDL gốc để tái tạo hoàn hảo bảng, ràng buộc và index
                using (var tran = cnTarget.BeginTransaction())
                {
                    foreach (string sqlCreate in schemaStatements)
                    {
                        using (var cmd = cnTarget.CreateCommand())
                        {
                            cmd.Transaction = tran;
                            cmd.CommandText = sqlCreate;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    tran.Commit();
                }

                // BƯỚC 3: Đính kèm ngược CSDL Gốc vào CSDL Năm cũ để bơm data
                using (var cmd = cnTarget.CreateCommand())
                {
                    cmd.CommandText = $"ATTACH DATABASE '{sourceDbPath.Replace("'", "''")}' AS DBNguon;";
                    cmd.ExecuteNonQuery();
                }

                bool attachThanhCong = true;
                try
                {
                    // Bơm toàn bộ dữ liệu an toàn
                    using (var tran = cnTarget.BeginTransaction())
                    {
                        foreach (string tbl in cacBangThucTeCo)
                        {
                            using (var cmd = cnTarget.CreateCommand())
                            {
                                cmd.Transaction = tran;
                                cmd.CommandText = $"INSERT INTO main.{tbl} SELECT * FROM DBNguon.{tbl};";
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tran.Commit();
                    }
                }
                finally
                {
                    // Luôn luôn tháo CSDL gốc ra dù có lỗi hay không (Defensive)
                    if (attachThanhCong && cnTarget.State == ConnectionState.Open)
                    {
                        try
                        {
                            using (var cmd = cnTarget.CreateCommand())
                            {
                                cmd.CommandText = "DETACH DATABASE DBNguon;";
                                cmd.ExecuteNonQuery();
                            }
                        }
                        catch { }
                    }
                }
            }
        }

        // =======================================================================
        // 3 HÀM LƯU TRỮ ĐƯỢC CẬP NHẬT GỌN GÀNG VÀ CHUẨN XÁC
        // =======================================================================
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

            try
            {
                SaoChepBangVaDuLieuChuanXac(Module_DanduongGPS.DuongDanCSDL4, targetPath, new[] { tableName });
            }
            catch
            {
                try { if (File.Exists(targetPath)) File.Delete(targetPath); } catch { }
                throw;
            }

            return targetPath;
        }

        public static string LuuTruDuLieuKhenThuongTapTheNam()
        {
            string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
            if (phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase)) return string.Empty;

            int nam = Module_NamHeThong.LayNamHeThong();
            string tableName = "ThongKe_KhenThuongTapThe";
            string fileName = $"KhenThuongTapThe_Nam{nam}.db";

            string thuMucLuu = Module_DanduongGPS.ThuMucLichSuThiDua;
            string targetPath = Path.Combine(thuMucLuu, fileName);

            if (!File.Exists(Module_DanduongGPS.DuongDanCSDL4))
                throw new FileNotFoundException("Không tìm thấy cơ sở dữ liệu nguồn khen thưởng (CSDL4).");

            Directory.CreateDirectory(thuMucLuu);
            if (File.Exists(targetPath))
                throw new InvalidOperationException($"Dữ liệu khen thưởng tập thể năm {nam} đã tồn tại trong lịch sử lưu trữ.");

            try
            {
                SaoChepBangVaDuLieuChuanXac(Module_DanduongGPS.DuongDanCSDL4, targetPath, new[] { tableName });
            }
            catch
            {
                try { if (File.Exists(targetPath)) File.Delete(targetPath); } catch { }
                throw;
            }

            return targetPath;
        }

        public static string LuuTruDuLieuKhenThuongToanDienNam()
        {
            string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
            if (phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase)) return string.Empty;

            int nam = Module_NamHeThong.LayNamHeThong();
            string fileName = $"KhenThuong_CBCS_Nam{nam}.db";
            string thuMucLuu = Module_DanduongGPS.ThuMucLichSuThiDua;
            string targetPath = Path.Combine(thuMucLuu, fileName);

            if (!File.Exists(Module_DanduongGPS.DuongDanCSDL4))
                throw new FileNotFoundException("Không tìm thấy cơ sở dữ liệu nguồn khen thưởng (CSDL số 4).");

            Directory.CreateDirectory(thuMucLuu);

            if (File.Exists(targetPath))
            {
                System.Windows.Forms.DialogResult result = System.Windows.Forms.MessageBox.Show(
                    $"Dữ liệu khen thưởng tổng hợp năm {nam} đã tồn tại trong thư mục lưu trữ.\nBạn có muốn xóa phiên bản cũ và cập nhật lại bằng dữ liệu mới nhất không?",
                    "Xác nhận ghi đè dữ liệu", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Question, System.Windows.Forms.MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly);

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    try
                    {
                        SqliteConnection.ClearAllPools();
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        File.Delete(targetPath);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Không thể xóa tệp dữ liệu cũ. Chi tiết lỗi: {ex.Message}");
                    }
                }
                else return string.Empty;
            }

            try
            {
                string[] cacBangCanSaoLuu = { "ThongKeCBCS_DuocKhenThuong", "ThongKe_GiayKhen", "ThongKe_KhenThuongTapThe" };
                SaoChepBangVaDuLieuChuanXac(Module_DanduongGPS.DuongDanCSDL4, targetPath, cacBangCanSaoLuu);
            }
            catch
            {
                SqliteConnection.ClearAllPools();
                try { if (File.Exists(targetPath)) File.Delete(targetPath); } catch { }
                throw;
            }

            return targetPath;
        }

        // =======================================================================
        // XUẤT EXCEL VÀ CẬP NHẬT TÌNH TRẠNG LỊCH SỬ
        // =======================================================================
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
                dataArray[r, cIndex++] = r + 1;

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

            using (var wb = new ClosedXML.Excel.XLWorkbook())
            {
                var ws = wb.Worksheets.Add("ThongKeThiDua");
                ws.Cell("A1").Value = "DANH SÁCH";
                ws.Range(1, 1, 1, colCount).Merge().Style.Font.SetBold().Font.SetFontSize(16).Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center).Alignment.SetVertical(ClosedXML.Excel.XLAlignmentVerticalValues.Center);
                ws.Cell("A2").Value = laTanBinh ? $"THỐNG KÊ PHÂN LOẠI THI ĐUA CỦA TÂN BINH {tenTieuDoan}" : $"THỐNG KÊ PHÂN LOẠI THI ĐUA CỦA CBCS {tenTieuDoan}";
                ws.Range(2, 1, 2, colCount).Merge().Style.Font.SetBold().Font.SetFontSize(12).Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center).Alignment.SetVertical(ClosedXML.Excel.XLAlignmentVerticalValues.Center);

                int excelStartRow = 4;
                var cellStt = ws.Cell(excelStartRow, 1);
                cellStt.Value = "STT";
                cellStt.Style.Font.Bold = true;
                cellStt.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                cellStt.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                cellStt.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

                ws.Column(1).Width = 5;
                var rangeColA = ws.Range(excelStartRow, 1, rowCount + excelStartRow, 1);
                rangeColA.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                rangeColA.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;

                int excelCol = 2;
                foreach (var col in exportCols)
                {
                    var cell = ws.Cell(excelStartRow, excelCol);
                    cell.Value = string.IsNullOrWhiteSpace(col.HeaderText) ? col.Name.Replace("_", " ") : col.HeaderText;
                    if (col.HeaderText.Contains("Họ và tên")) ws.Column(excelCol).Width = 30;
                    else if (excelCol == 5) ws.Column(excelCol).Width = 20;
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

                if (idDangCongTac.Count == 0 && idChuyenCongTac.Count == 0) return;

                using (var cnNamCu = new SqliteConnection($"Data Source={pathCsdlNamCu}"))
                {
                    cnNamCu.Open();
                    using var tran = cnNamCu.BeginTransaction();
                    try
                    {
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

        public static void CapNhatTinhTrangLichSuTuDanhSachGoc(string pathCsdl2, string pathCsdlNamCu, bool laTanBinh)
        {
            if (string.IsNullOrEmpty(pathCsdlNamCu) || !File.Exists(pathCsdlNamCu)) return;
            if (string.IsNullOrEmpty(pathCsdl2) || !File.Exists(pathCsdl2)) return;
            string tableDich = laTanBinh ? "ThiDuaThang_TanBinh" : "ThiDuaThang";
            try
            {
                var hashSoHieuCsdl2 = new HashSet<string>(10000, StringComparer.OrdinalIgnoreCase);

                using (var cn2 = new SqliteConnection($"Data Source={pathCsdl2};Mode=ReadOnly;Cache=Shared"))
                {
                    cn2.Open();
                    using var cmd = new SqliteCommand("SELECT SoHieu FROM DanhSach WHERE SoHieu IS NOT NULL AND SoHieu <> ''", cn2);
                    using var rd = cmd.ExecuteReader();

                    while (rd.Read())
                    {
                        string rawSh = rd.GetString(0);
                        string plainSh = SafeDecrypt(rawSh);

                        if (!string.IsNullOrWhiteSpace(plainSh))
                        {
                            hashSoHieuCsdl2.Add(plainSh.Trim());
                        }
                    }
                }

                if (hashSoHieuCsdl2.Count == 0) return;

                using (var cnCu = new SqliteConnection($"Data Source={pathCsdlNamCu}"))
                {
                    cnCu.Open();

                    using (var cmdPragma = new SqliteCommand("PRAGMA synchronous = NORMAL; PRAGMA journal_mode = WAL;", cnCu))
                    {
                        cmdPragma.ExecuteNonQuery();
                    }

                    using (var cmdCheck = new SqliteCommand("SELECT 1 FROM sqlite_master WHERE type='table' AND name=@tableName LIMIT 1;", cnCu))
                    {
                        cmdCheck.Parameters.AddWithValue("@tableName", tableDich);
                        if (cmdCheck.ExecuteScalar() == null) return;
                    }

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

                            string ttMoi = (!string.IsNullOrEmpty(plainSh) && hashSoHieuCsdl2.Contains(plainSh))
                                ? "Đang công tác"
                                : "Chuyển công tác";

                            if (!string.Equals(ttCu, ttMoi, StringComparison.OrdinalIgnoreCase))
                            {
                                updateQueue.Add((id, ttMoi));
                            }
                        }
                    }

                    if (updateQueue.Count > 0)
                    {
                        using var tran = cnCu.BeginTransaction();
                        try
                        {
                            using var cmdUpd = new SqliteCommand($"UPDATE [{tableDich}] SET TinhTrang = @tt WHERE ID = @id", cnCu, tran);

                            var pTt = cmdUpd.Parameters.Add("@tt", SqliteType.Text);
                            var pId = cmdUpd.Parameters.Add("@id", SqliteType.Integer);

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
    }
}