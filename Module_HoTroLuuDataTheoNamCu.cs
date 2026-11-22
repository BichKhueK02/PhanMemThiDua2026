using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
namespace PhanMemThiDua2026
{
    // KHU VỰC CÁC CLASS DTO (DATA TRANSFER OBJECT)
    public class FileLichSuDTO
    {
        //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
        public int STT { get; set; }
        public string TenHienThi { get; set; } = null!;
        public string DuongDan { get; set; } = null!;
        public bool LaTanBinh { get; set; }
    }
    public class HistoryCBCSDTO
    {
        // Các trường được nạp ngay sau khi tạo đối tượng (đọc CSDL năm cũ) => "= null!" chỉ để báo trình biên dịch, không sinh mã.
        public string ID = null!, HoVaTen = null!, SoHieu = null!, DonVi = null!, TinhTrang = null!;
        public string KQ_TD = null!, KQ_XL_CB = null!, KQ_XL_DV = null!, T12 = null!;
        public string[] Thang = new string[11]; // Thang_1 -> Thang_11
        public string Sau_Thang_Dau_Nam = null!, TongKet_Nam = null!, TS_Loai1 = null!, TS_Loai2 = null!, TS_Loai3 = null!, TS_Loai4 = null!;
        public string GhiChu = null!;
        // Thuộc tính phục vụ thuật toán tìm kiếm và sắp xếp
        public string HoVaTen_Search = null!;
        public int SortPriority;
        public Dictionary<string, string> CotPhatSinh = new Dictionary<string, string>();
    }
    public class HistoryTanBinhDTO
    {
        public string ID = null!, HoVaTen = null!, SoHieu = null!, DonVi = null!, TinhTrang = null!;
        public string TS_Loai1 = null!, TS_Loai2 = null!, TS_Loai3 = null!, TS_Loai4 = null!;
        public string GhiChu = null!;
        // Thuộc tính phục vụ thuật toán tìm kiếm và sắp xếp
        public string HoVaTen_Search = null!;
        public int SortPriority;
        public Dictionary<string, string> DuLieuThang = new Dictionary<string, string>(); // Lưu tuần và kết quả tháng
        public Dictionary<string, string> CotPhatSinh = new Dictionary<string, string>();
    }
    // CLASS CHÍNH: XỬ LÝ LƯU TRỮ VÀ LỊCH SỬ
    internal static class Module_HoTroLuuDataTheoNamCu
    {
        public static List<FileLichSuDTO> LayDanhSachFileLichSu()
        {
            var danhSach = new List<FileLichSuDTO>();
            string dir = Module_DanduongGPS.ThuMucLichSuThiDua;
            if (!Directory.Exists(dir)) return danhSach;
            bool phienBanHienTaiLaTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().LaCheDoTanBinh();
            var files = Directory.GetFiles(dir, "*.db");
            foreach (var file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                bool isFileTanBinh = fileName.Contains("TanBinh", StringComparison.OrdinalIgnoreCase);
                if (phienBanHienTaiLaTanBinh != isFileTanBinh) continue;
                string nam = fileName.Split(new string[] { "Nam" }, StringSplitOptions.None).LastOrDefault() ?? "???";
                danhSach.Add(new FileLichSuDTO
                {
                    TenHienThi = $"Năm {nam} - {(isFileTanBinh ? Module_TaiKhoan.TenTB_Hoa : "CBCS")}",
                    DuongDan = file,
                    LaTanBinh = isFileTanBinh
                });
            }
            // Thay dòng này:
            // return danhSach.OrderByDescending(x => x.TenHienThi).ToList();
            // BẰNG ĐOẠN CODE NÀY:
            var ketQua = danhSach.OrderByDescending(x => x.TenHienThi).ToList();
            // Đánh số thứ tự sau khi đã sắp xếp năm giảm dần
            for (int i = 0; i < ketQua.Count; i++)
            {
                ketQua[i].STT = i + 1;
            }
            return ketQua;
        }
        public static List<FileLichSuDTO> LayDanhSachFileLichSu_KhenThuongCaNhan()
        {
            var danhSach = new List<FileLichSuDTO>();
            // [CHẶN CỬA]: Tân binh không có khen thưởng -> Trả về danh sách trống luôn
            string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
            if (phienBan.LaCheDoTanBinh())
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
            // Thay dòng này:
            // return danhSach.OrderByDescending(x => x.TenHienThi).ToList();
            // BẰNG ĐOẠN CODE NÀY:
            var ketQua = danhSach.OrderByDescending(x => x.TenHienThi).ToList();
            // Đánh số thứ tự sau khi đã sắp xếp năm giảm dần
            for (int i = 0; i < ketQua.Count; i++)
            {
                ketQua[i].STT = i + 1;
            }
            return ketQua;
        }
        public static DataTable LoadDataFromHistoryDB(string dbPath, string tableName)
        {
            DataTable dt = new DataTable();
            if (!File.Exists(dbPath)) return dt;
            try
            {
                using (var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(dbPath, chiDoc: true)))
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
                            string? value = row[colName].ToString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                try
                                {
                                    string decrypted = Module_BaoMatAES.GiaiMa(value);
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
                string result = Module_BaoMatAES.GiaiMa(input);
                return string.IsNullOrEmpty(result) ? input : result;
            }
            catch { return input; }
        }
        // LÕI HỆ THỐNG: HÀM REVERSE ATTACH & CLONE DDL
        // GIÚP COPY 100% CẤU TRÚC (PRIMARY KEY, AUTOINCREMENT, INDEX) TỪ DB GỐC
        private static void SaoChepBangVaDuLieuChuanXac(string sourceDbPath, string targetDbPath, string[] tableNames)
        {
            var schemaStatements = new List<string>();
            var cacBangThucTeCo = new List<string>();
            // BƯỚC 1: Lấy schema gốc (Nguyên văn câu lệnh Create Table, Index, Primary Key, AutoIncrement...)
            using (var cnSource = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(sourceDbPath, pooling: false)))
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
            using (var cnTarget = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(targetDbPath, pooling: false)))
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
        // 3 HÀM LƯU TRỮ ĐƯỢC CẬP NHẬT GỌN GÀNG VÀ CHUẨN XÁC
        public static string LuuTruDuLieuThiDuaNam()
        {
            int nam = Module_HeThong.LayNamHeThong();
            bool laTanBinh = Module_TaiKhoan
                .LayPhienBanPhanMem()
                .LaCheDoTanBinh();
            string sourcePath = Module_DanduongGPS.DuongDanCSDL4;
            string thuMucLuu = Module_DanduongGPS.ThuMucLichSuThiDua;
            string fileName = $"ThiDua_{(laTanBinh ? "TanBinh" : "CBCS")}_Nam{nam}.db";
            string targetPath = Path.Combine(thuMucLuu, fileName);
            // Kiểm tra CSDL nguồn
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException(
                    "Không tìm thấy cơ sở dữ liệu nguồn (CSDL4).",
                    sourcePath);
            // Đảm bảo thư mục lưu trữ tồn tại
            Directory.CreateDirectory(thuMucLuu);
            // Không ghi đè dữ liệu lưu trữ cũ
            if (File.Exists(targetPath))
                throw new InvalidOperationException(
                    $"Dữ liệu lưu trữ năm {nam} đã tồn tại.");
            string[] tablesToBackup = laTanBinh
                ? new[] { "ThiDuaThang_TanBinh" }
                : new[] { "ThiDuaThang", "ThongKe_PhanLoaiTapThe" };
            try
            {
                SaoChepBangVaDuLieuChuanXac(
                    sourcePath,
                    targetPath,
                    tablesToBackup);
                return targetPath;
            }
            catch
            {
                // Xóa file dở dang nếu quá trình sao lưu thất bại
                try
                {
                    if (File.Exists(targetPath))
                        File.Delete(targetPath);
                }
                catch
                {
                    // Không che mất exception gốc.
                }
                throw;
            }
        }
        public static string LuuTruDuLieuKhenThuongToanDienNam()
        {
            string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
            if (phienBan.LaCheDoTanBinh()) return string.Empty;
            int nam = Module_HeThong.LayNamHeThong();
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
        // XUẤT EXCEL VÀ CẬP NHẬT TÌNH TRẠNG LỊCH SỬ
        public static void XuatExcelLichSuCore(string targetExcelPath, bool laTanBinh, bool isDataMaxMode, List<ColumnExportMeta> exportCols, List<int> filteredIndexes, DataTable dtSource, List<HistoryCBCSDTO> cacheCBCS,List<HistoryTanBinhDTO> cacheTanBinh, string tenTieuDoan)
        {
            int rowCount = filteredIndexes.Count;
            int colCount = exportCols.Count + 1;
            // Bộ đọc mỗi cột tạo 1 lần (trước đây so sánh chuỗi tên cột qua chuỗi if/else cho TỪNG Ô)
            var layCot = exportCols.Select(c => TaoBoLayCotLichSu(c.Name, laTanBinh, isDataMaxMode, dtSource, cacheCBCS, cacheTanBinh)).ToArray();
            var dongDuLieu = new List<object?[]>(rowCount);
            for (int r = 0; r < rowCount; r++)
            {
                int actualIndex = filteredIndexes[r];
                var d = new object?[colCount];
                d[0] = r + 1;
                for (int c = 0; c < layCot.Length; c++) d[c + 1] = layCot[c](actualIndex);
                dongDuLieu.Add(d);
            }
            using (var wb = new ClosedXML.Excel.XLWorkbook())
            {
                var ws = wb.Worksheets.Add("ThongKeThiDua");
                ws.Cell("A1").Value = "DANH SÁCH";
                ws.Range(1, 1, 1, colCount).Merge().Style.Font.SetBold().Font.SetFontSize(16).Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center).Alignment.SetVertical(ClosedXML.Excel.XLAlignmentVerticalValues.Center);
                ws.Cell("A2").Value = laTanBinh ? $"THỐNG KÊ PHÂN LOẠI THI ĐUA CỦA {Module_TaiKhoan.TenTB_InHoa} {tenTieuDoan}" : $"THỐNG KÊ PHÂN LOẠI THI ĐUA CỦA CBCS {tenTieuDoan}";
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
                Module_ExcelAnToan.NapDong(ws, excelStartRow + 1, 1, dongDuLieu);   // nạp 1 lần thay vì gán từng ô
                if (rowCount > 0)
                {
                    var dataRange = ws.Range(excelStartRow + 1, 1, excelStartRow + rowCount, colCount);
                    dataRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin; dataRange.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin; dataRange.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center; dataRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                    ws.Range(excelStartRow + 1, 1, excelStartRow + rowCount, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;
                    int hoTenColIndex = exportCols.FindIndex(c => c.Name.Equals("HoVaTen", StringComparison.OrdinalIgnoreCase));
                    if (hoTenColIndex >= 0) ws.Range(excelStartRow + 1, hoTenColIndex + 2, excelStartRow + rowCount, hoTenColIndex + 2).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;
                }
                int tongDong = rowCount + excelStartRow + 1;
                var totalCell = ws.Cell(tongDong, 1); totalCell.Value = $"Tổng cộng: {rowCount} {Module_HeThong.Tu_dong_chi} ./."; totalCell.Style.Font.SetBold().Font.SetItalic(); ws.Range(tongDong, 1, tongDong, colCount).Merge();
                Module_BanQuyen.DongDauExcel(wb);
                Module_ExcelAnToan.LuuAnToan(wb, targetExcelPath);
            }
        }
        /// <summary>
        /// Tạo 1 hàm đọc giá trị cho 1 cột (theo chỉ số dòng gốc). Quyết định "cột nào lấy trường nào"
        /// chỉ chạy 1 lần cho mỗi cột thay vì lặp lại cho từng ô.
        /// </summary>
        private static Func<int, object?> TaoBoLayCotLichSu(string ten, bool laTanBinh, bool isDataMaxMode, DataTable dtSource,
            List<HistoryCBCSDTO> cacheCBCS, List<HistoryTanBinhDTO> cacheTanBinh)
        {
            if (!isDataMaxMode)
            {
                if (dtSource == null || !dtSource.Columns.Contains(ten)) return _ => "";
                var dv = dtSource.DefaultView;
                return i => dv[i][ten]?.ToString() ?? "";
            }
            if (laTanBinh)
            {
                Func<HistoryTanBinhDTO, string?> f = ten switch
                {
                    "HoVaTen" => x => x.HoVaTen,
                    "SoHieu" => x => x.SoHieu,
                    "DonVi" => x => x.DonVi,
                    "TinhTrang" => x => x.TinhTrang,
                    "GhiChu" => x => x.GhiChu,
                    "TS_Loai1" => x => x.TS_Loai1,
                    "TS_Loai2" => x => x.TS_Loai2,
                    "TS_Loai3" => x => x.TS_Loai3,
                    "TS_Loai4" => x => x.TS_Loai4,
                    _ => x => x.DuLieuThang.TryGetValue(ten, out var v) ? v : (x.CotPhatSinh.TryGetValue(ten, out var p) ? p : "")
                };
                return i => f(cacheTanBinh[i]) ?? "";
            }
            Func<HistoryCBCSDTO, string?> g;
            switch (ten)
            {
                case "HoVaTen": g = x => x.HoVaTen; break;
                case "SoHieu": g = x => x.SoHieu; break;
                case "DonVi": g = x => x.DonVi; break;
                case "TinhTrang": g = x => x.TinhTrang; break;
                case "GhiChu": g = x => x.GhiChu; break;
                case "KQ_ThiDua_Nam_Cu": g = x => x.KQ_TD; break;
                case "KQ_XepLoaiCB_Nam_Cu": g = x => x.KQ_XL_CB; break;
                case "KQ_XepLoaiDangVien_Nam_Cu": g = x => x.KQ_XL_DV; break;
                case "Thang_12_Nam_Cu": g = x => x.T12; break;
                case "Sau_Thang_Dau_Nam": g = x => x.Sau_Thang_Dau_Nam; break;
                case "TongKet_Nam": g = x => x.TongKet_Nam; break;
                case "TS_Loai1": g = x => x.TS_Loai1; break;
                case "TS_Loai2": g = x => x.TS_Loai2; break;
                case "TS_Loai3": g = x => x.TS_Loai3; break;
                case "TS_Loai4": g = x => x.TS_Loai4; break;
                default:
                    if (ten.StartsWith("Thang_") && int.TryParse(ten.Substring(6), out int t))
                        g = x => t >= 1 && t <= x.Thang.Length ? x.Thang[t - 1] : "";   // chặn tràn chỉ số (vd Thang_12)
                    else
                        g = x => x.CotPhatSinh.TryGetValue(ten, out var p) ? p : "";
                    break;
            }
            return i => g(cacheCBCS[i]) ?? "";
        }
        public static void CapNhatTinhTrangThiDuaNamCu(string pathCsdl2, string pathCsdlNamCu, bool laTanBinh)
        {
            if (string.IsNullOrEmpty(pathCsdlNamCu) || !File.Exists(pathCsdlNamCu)) return;
            if (!File.Exists(pathCsdl2)) return;
            string tableDich = laTanBinh ? "ThiDuaThang_TanBinh" : "ThiDuaThang";
            try
            {
                var hashSoHieuCsdl2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var cn2 = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(pathCsdl2, chiDoc: true)))
                {
                    cn2.Open();
                    using var cmd = new SqliteCommand($"SELECT SoHieu FROM {Module_TaiKhoan.BangDanhSach}", cn2);
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        string encSh = rd["SoHieu"]?.ToString() ?? "";
                        string? plainSh = SafeDecrypt(encSh)?.Trim();
                        if (!string.IsNullOrEmpty(plainSh))
                        {
                            hashSoHieuCsdl2.Add(plainSh);
                        }
                    }
                }
                var idDangCongTac = new List<int>();
                var idChuyenCongTac = new List<int>();
                using (var cnNamCu = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(pathCsdlNamCu, chiDoc: true)))
                {
                    cnNamCu.Open();
                    using var cmd = new SqliteCommand($"SELECT ID, SoHieu, TinhTrang FROM [{tableDich}]", cnNamCu);
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        int id = Convert.ToInt32(rd["ID"]);
                        string encSh = rd["SoHieu"]?.ToString() ?? "";
                        string? plainSh = SafeDecrypt(encSh)?.Trim();
                        string ttHienTai = rd["TinhTrang"]?.ToString()?.Trim() ?? "";
                        bool tonTaiInCsdl2 = !string.IsNullOrEmpty(plainSh) && hashSoHieuCsdl2.Contains(plainSh);
                        if (tonTaiInCsdl2)
                        {
                            if (ttHienTai != Module_HeThong.TT_DANG_CONG_TAC) idDangCongTac.Add(id);
                        }
                        else
                        {
                            if (ttHienTai != Module_HeThong.TT_CHUYEN_CONG_TAC) idChuyenCongTac.Add(id);
                        }
                    }
                }
                if (idDangCongTac.Count == 0 && idChuyenCongTac.Count == 0) return;
                using (var cnNamCu = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(pathCsdlNamCu)))
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
                using (var cn2 = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(pathCsdl2, chiDoc: true, cacheChung: true)))
                {
                    cn2.Open();
                    using var cmd = new SqliteCommand($"SELECT SoHieu FROM {Module_TaiKhoan.BangDanhSach} WHERE SoHieu IS NOT NULL AND SoHieu <> ''", cn2);
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
                using (var cnCu = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(pathCsdlNamCu)))
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
                                ? Module_HeThong.TT_DANG_CONG_TAC
                                : Module_HeThong.TT_CHUYEN_CONG_TAC;
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
        /// </summary>
        public static bool LaPhienBanCBCS()
        {
            try
            {
                string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(phienBan)) return false;
                // Nếu KHÔNG chứa từ "tân binh" -> Là phiên bản CBCS
                return !phienBan.LaCheDoTanBinh();
            }
            catch
            {
                return false;
            }
        }
        /// Tự động ẩn/hiện ToolStripMenuItem tùy theo phiên bản (Chỉ hiện ở CBCS, ẩn ở Tân Binh)
        public static void CapNhatAnHienMenuDongBo(ToolStripItem menuItem)
        {
            if (menuItem == null) return;
            // Dùng Available thay vì Visible để WinForms ẩn tuyệt đối trên ToolStrip/ContextMenu
            menuItem.Available = LaPhienBanCBCS();
        }
    }
}