using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices; // <--- Đảm bảo có dòng này ở đầu file

namespace PhanMemThiDua2026
{
    public static class Module_XuatPhanLoai
    {
        public static string LastFilePath = "";
        public static string LinkDanTep = "";
        private static bool _daNapThongTin = false;
        private static string _tenTrungDoan = "";
        private static string _tenTieuDoan = "";
        private static string _tomTatGhiChu = "";
        // Biến toàn cục trong module
        public static string LinkLuuDuongDanTepXuat = string.Empty;
        public static string TEN_TRUNG_DOAN => _tenTrungDoan;
        public static string TEN_TIEU_DOAN => _tenTieuDoan;
        public static string TOM_TAT_GHI_CHU => _tomTatGhiChu;
        public static void NapThongTinDonVi()
        {
            if (_daNapThongTin) return;

            if (LayThongTinDonVi(
                out _tenTrungDoan,
                out _tenTieuDoan,
                out _tomTatGhiChu))
            {
                _daNapThongTin = true;
            }
        }
        // Helper mở kết nối CSDL
        private static SqliteConnection GetOpenConnection()
        {
            var conn = new SqliteConnection($"Data Source={Module_DanduongGPS.DuongDanCSDL2}");
            conn.Open();
            return conn;
        }
        // Helper tạo command
        private static SqliteCommand CreateCommand(string sql, SqliteConnection conn)
        {
            return new SqliteCommand(sql, conn);
        }
        private static bool _canReloadLink = true;
        //cờ hiệu đảm bảo nhận được link mới khi người dùng thay đổi
        public static string GetLinkLuuDuongDanTepXuat(bool forceReload = false)
        {
            if (forceReload || string.IsNullOrWhiteSpace(LinkLuuDuongDanTepXuat) || _canReloadLink)
            {
                NapLinkLuuDuongDanTepXuat();
                _canReloadLink = false;
            }

            return LinkLuuDuongDanTepXuat ?? string.Empty;
        }
        public static void NapLinkLuuDuongDanTepXuat()
        {
            string csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
            if (string.IsNullOrWhiteSpace(csdl2Path))
            {
                LinkLuuDuongDanTepXuat = string.Empty;
                return;
            }

            try
            {
                using var conn = new SqliteConnection($"Data Source={csdl2Path};Mode=ReadOnly;");
                conn.Open();

                using var cmd = new SqliteCommand(
                    "SELECT ChonDuongDanXuatTep FROM ThongTin WHERE ID=1", conn);

                object result = cmd.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                {
                    string giaiMa = BaoMatAES.GiaiMa(result.ToString()).Trim();

                    if (!string.IsNullOrWhiteSpace(giaiMa) && Directory.Exists(giaiMa))
                    {
                        LinkLuuDuongDanTepXuat = giaiMa;
                    }
                    else
                    {
                        LinkLuuDuongDanTepXuat = string.Empty;
                    }
                }
                else
                {
                    LinkLuuDuongDanTepXuat = string.Empty;
                }
            }
            catch (Exception ex)
            {
                // ghi log nội bộ, KHÔNG làm rối UX
                Module_NhatKy.GhiNhatKy(
                    taiKhoan: "SYSTEM",
                    hanhDong: "Lỗi đọc đường dẫn xuất",
                    ghiChu: ex.Message);

                LinkLuuDuongDanTepXuat = string.Empty;
                _canReloadLink = true; // cho phép lần sau đọc lại
            }
        }
        public static string InHoa(string input)
            => string.IsNullOrWhiteSpace(input) ? "" : input.ToUpperInvariant();
        #region Hàm Windows API: Mở thư mục và chọn tệp thông minh
        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern void ILFree(IntPtr pidl);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern IntPtr ILCreateFromPathW(string pszPath);

        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, uint dwFlags);

        /// <summary>
        /// Gọi thư mục chứa tệp lên màn hình và bôi đen tệp. Không mở thêm cửa sổ nếu thư mục đã mở.
        /// </summary>
        public static void MoThuMucVaChonTep(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return;

            IntPtr pidlList = ILCreateFromPathW(filePath);
            if (pidlList != IntPtr.Zero)
            {
                try
                {
                    // dwFlags = 0: Mặc định. Gọi window đang mở lên, hoặc mở mới nếu chưa có.
                    SHOpenFolderAndSelectItems(pidlList, 0, null, 0);
                }
                finally
                {
                    ILFree(pidlList);
                }
            }
        }
        #endregion
        public class KetQuaTinhBCH
        {
            public int TongBCH;
            public int Loai1;
            public int Loai2;
            public int Loai3;
            public int Loai4;
            public int KhongPhanLoai;
            // Kết quả tính theo đề nghị
            public int DeNghiLoai1;
            public int DeNghiLoai2;
            public int DeNghiLoai3;
        }
        public static int soLuongBCHD;
        public static int soLuongBCHDLoai1;
        public static int soLuongBCHDLoai2;
        public static int soLuongBCHDLoai3;
        public static int soLuongBCHDLoai4;
        public static int soLuongBCHDKhongPhanLoai;

        public static void XuatPhanLoai(string phanLoai, int soThuTuFile)
        {
            try
            {
                string fileDB = Module_DanduongGPS.DuongDanCSDL2;
                string fileMau = Module_DanduongGPS.DuongDanCSDL4ex;
                // Lấy dữ liệu từ CSDL
                DataTable dt = new();
                using (var conn = GetOpenConnection())
                {
                    using var cmd = CreateCommand("SELECT * FROM DanhSach", conn);
                    using var rd = cmd.ExecuteReader();
                    dt.Load(rd);
                }
                // Lọc dữ liệu theo phân loại (Đã fix tương thích V2)
                var data = dt.AsEnumerable()
                    .Where(r =>
                    {
                        string raw = r["PhanLoai"]?.ToString() ?? "";
                        string dec = BaoMatAES.GiaiMa(raw).Trim();
                        if (!string.IsNullOrEmpty(dec)) raw = dec;
                        return raw.Trim().Equals(phanLoai.Trim(), StringComparison.OrdinalIgnoreCase);
                    })
                    .ToList();

                if (data.Count == 0)
                {
                    Module_ThongBao.Loi($"Không có CBCS phân loại: {phanLoai}!");
                    return;
                }

                // Chuẩn bị thư mục và tên file xuất
                string thuMucGoc = Module_XuatPhanLoai.GetLinkLuuDuongDanTepXuat();
                if (string.IsNullOrWhiteSpace(thuMucGoc))
                    throw new Exception("Bạn chưa chọn thư mục lưu!");
                string thangHT = LayThangHeThong();
                string namHT = LayNamHeThong();
                string tenThuMuc = $"DANH SÁCH PHÂN LOẠI THI ĐUA THÁNG {thangHT} NĂM {namHT}";
                string thuMucDich = Path.Combine(thuMucGoc, tenThuMuc);
                Directory.CreateDirectory(thuMucDich);

                // Lấy tên Tiểu đoàn từ CSDL
                string tenTieuDoan = "";
                using (var conn = GetOpenConnection())
                using (var cmd = CreateCommand("SELECT TenTieuDoan FROM ThongTin WHERE ID=1", conn))
                {
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        string raw = result.ToString().Trim();
                        try { tenTieuDoan = BaoMatAES.GiaiMa(raw); } catch { tenTieuDoan = raw; }
                    }
                }

                string tieuDoanHienThi = string.IsNullOrWhiteSpace(tenTieuDoan)
                    ? "      "
                    : char.ToUpper(tenTieuDoan[0]) + tenTieuDoan.Substring(1).ToLower();
                int sttFile = Directory.GetFiles(thuMucDich, "*.xlsx").Length + 1;
                string fileName = $"{sttFile}. DANH SÁCH {InHoa(phanLoai)} CỦA {tenTieuDoan} - {DateTime.Now:yyyyMMdd-HHmmss}.xlsx";
                string fileDich = Path.Combine(thuMucDich, fileName);

                using var wb = new XLWorkbook(fileMau);
                string sheetName = GetTenSheet(phanLoai);
                if (!wb.Worksheets.Contains(sheetName))
                    throw new Exception($"Sheet '{sheetName}' không tồn tại trong file mẫu.");
                var ws = wb.Worksheet(sheetName);

                foreach (var sht in wb.Worksheets.ToList())
                {
                    if (sht.Name != sheetName && sht.Name != "GIOI_THIEU")
                        wb.Worksheets.Delete(sht.Name);
                }

                bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                int rowStart = 10;
                int row = rowStart;

                var cols = dt.Columns.Cast<DataColumn>()
                    .Where(c => c.ColumnName is not ("ID" or "STT" or "PhanLoai" or "GhiChu"))
                    .ToList();

                foreach (var r in data)
                {
                    var cellSTT = ws.Cell(row, 1);
                    cellSTT.Value = row - rowStart + 1;
                    cellSTT.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cellSTT.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cellSTT.Style.Font.FontName = "Times New Roman";
                    cellSTT.Style.Font.FontSize = 13;

                    for (int c = 0; c < cols.Count; c++)
                    {
                        int excelCol = c + 2;
                        var cell = ws.Cell(row, excelCol);

                        if (laTanBinh && excelCol == 3)
                        {
                            cell.Value = "";
                        }
                        else
                        {
                            string val = r[cols[c]]?.ToString() ?? "";
                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                string decVal = BaoMatAES.GiaiMa(val).Trim();
                                if (!string.IsNullOrEmpty(decVal)) val = decVal;
                            }
                            cell.Value = val;
                        }

                        cell.Style.NumberFormat.Format = "@";
                        cell.Style.Font.FontName = "Times New Roman";
                        cell.Style.Font.FontSize = 13;
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        if (excelCol == 2 || excelCol == 5)
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        else
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }

                    var cellDatLoai = ws.Cell(row, 10);
                    string phanLoaiHienThi = BaoMatAES.GiaiMa(phanLoai ?? "").Trim();
                    if (string.IsNullOrEmpty(phanLoaiHienThi)) phanLoaiHienThi = phanLoai ?? "";
                    cellDatLoai.Value = phanLoaiHienThi;
                    cellDatLoai.Style.Font.FontName = "Times New Roman";
                    cellDatLoai.Style.Font.FontSize = 13;
                    cellDatLoai.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cellDatLoai.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    row++;
                }

                var tableRange = ws.Range(rowStart, 1, row - 1, 11);
                tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                ws.Range(row, 1, row, 11).Merge();
                var tong = ws.Cell(row, 1);
                tong.Value = $"Tổng cộng: {data.Count} đồng chí./.";
                tong.Style.Font.Bold = true;
                tong.Style.Font.Italic = true;
                tong.Style.Font.FontName = "Times New Roman";
                tong.Style.Font.FontSize = 14;
                tong.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                // --- PHẦN CHỮ KÝ (GIỮ NGUYÊN GỐC) ---
                int dongKy = row + 1;
                string hoTenKy = "";
                try
                {
                    using (var conn = GetOpenConnection())
                    {
                        using var cmd = CreateCommand("SELECT ChiHuyD FROM ThongTin WHERE ID = 1", conn);
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value) hoTenKy = BaoMatAES.GiaiMa(result.ToString()).Trim();
                    }
                }
                catch (Exception ex) { Module_ThongBao.Loi("Lỗi lấy chỉ huy: " + ex.Message); }

                string chucVuTieuDoanTruong = "";
                int dongDauTienID = -1;
                string chucVuNguoiKy = "";
                int dongNguoiKyID = -1;

                using (var cn = new SqliteConnection("Data Source=" + fileDB))
                {
                    cn.Open();
                    using (var cmd = new SqliteCommand("SELECT ID, HoVaTen, ChucVu FROM ChiHuyD ORDER BY ID ASC", cn))
                    using (var rd = cmd.ExecuteReader())
                    {
                        bool isFirst = true;
                        bool foundMatch = false;
                        while (rd.Read())
                        {
                            int id = Convert.ToInt32(rd["ID"]);
                            string htRaw = rd["HoVaTen"]?.ToString() ?? "";
                            string cvRaw = rd["ChucVu"]?.ToString() ?? "";
                            string htDec = BaoMatAES.GiaiMa(htRaw).Trim(); if (string.IsNullOrEmpty(htDec)) htDec = htRaw.Trim();
                            string cvDec = BaoMatAES.GiaiMa(cvRaw).Trim(); if (string.IsNullOrEmpty(cvDec)) cvDec = cvRaw.Trim();

                            if (isFirst) { dongDauTienID = id; chucVuTieuDoanTruong = cvDec; isFirst = false; }
                            if (htDec.Equals(hoTenKy, StringComparison.OrdinalIgnoreCase)) { dongNguoiKyID = id; chucVuNguoiKy = cvDec; foundMatch = true; }
                        }
                        if (!foundMatch) { dongNguoiKyID = dongDauTienID; chucVuNguoiKy = chucVuTieuDoanTruong; }
                    }
                }

                void GhiDongKy(string text)
                {
                    ws.Range(dongKy, 6, dongKy, 10).Merge();
                    var c = ws.Cell(dongKy, 6);
                    c.Value = text;
                    c.Style.Font.Bold = true; c.Style.Font.FontName = "Times New Roman"; c.Style.Font.FontSize = 14;
                    c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    c.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    dongKy++;
                }

                if (dongNguoiKyID == dongDauTienID) GhiDongKy(chucVuTieuDoanTruong.ToUpper());
                else { GhiDongKy("KT. " + chucVuTieuDoanTruong.ToUpper()); GhiDongKy(chucVuNguoiKy.ToUpper()); }
                dongKy += 4; GhiDongKy(hoTenKy);

                // --- LẤY THÔNG TIN THỜI GIAN (DÙNG CHO CẢ A6 VÀ E4) ---
                string khoangTrang = "      ";
                string thang = khoangTrang, nam = khoangTrang, ngay = khoangTrang, diaDiem = khoangTrang;
                string loaiBaoCao = "", tuanBaoCao = "";

                try
                {
                    using (var conn = new SqliteConnection($"Data Source={fileDB}"))
                    {
                        conn.Open();
                        using (var cmd = new SqliteCommand("SELECT Thang, Nam, Ngay, DiaDiem FROM ThongTin WHERE ID = 1", conn))
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                thang = BaoMatAES.GiaiMa(reader["Thang"]?.ToString() ?? "").Trim();
                                nam = BaoMatAES.GiaiMa(reader["Nam"]?.ToString() ?? "").Trim();
                                ngay = BaoMatAES.GiaiMa(reader["Ngay"]?.ToString() ?? "").Trim();
                                diaDiem = BaoMatAES.GiaiMa(reader["DiaDiem"]?.ToString() ?? "").Trim();
                            }
                        }
                        // ⭐ Lấy loại báo cáo
                        using (var cmd2 = new SqliteCommand("SELECT ChonLoaiBaoCao, ChonTuan FROM ChonLoaiBaoCao WHERE ID = 1", conn))
                        using (var rd2 = cmd2.ExecuteReader())
                        {
                            if (rd2.Read())
                            {
                                loaiBaoCao = BaoMatAES.GiaiMa(rd2["ChonLoaiBaoCao"]?.ToString() ?? "").Trim();
                                tuanBaoCao = BaoMatAES.GiaiMa(rd2["ChonTuan"]?.ToString() ?? "").Trim();
                            }
                        }
                    }
                }
                catch { }
                if (string.IsNullOrEmpty(thang)) thang = khoangTrang; if (string.IsNullOrEmpty(nam)) nam = khoangTrang;
                if (string.IsNullOrEmpty(ngay)) ngay = khoangTrang; if (string.IsNullOrEmpty(diaDiem)) diaDiem = khoangTrang;

                // Xử lý chuỗi thời gian cho tiêu đề
                string chuoiThoiGian = loaiBaoCao.Equals("Tuần", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(tuanBaoCao)
                    ? $"{tuanBaoCao.Trim().ToUpper()} THÁNG {thangHT}/{nam}"
                    : $"THÁNG {thangHT}/{nam}";

                // --- PHẦN ĐỊNH DẠNG ĐƠN VỊ A1, A2, A3 (GIỮ NGUYÊN GỐC) ---
                string dong1TrungDoan = "";
                string donviCapTrungDoan = "";
                string donviCapTieuDoan = "";
                using (var conn = GetOpenConnection())
                {
                    using (var cmd = CreateCommand("SELECT textBox1_TenTrungDoanDong1, TenTrungDoan, TenTieuDoan FROM ThongTin WHERE ID = 1", conn))
                    using (var rd = cmd.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            dong1TrungDoan = BaoMatAES.GiaiMa(rd[0].ToString());
                            donviCapTrungDoan = BaoMatAES.GiaiMa(rd[1].ToString());
                            donviCapTieuDoan = BaoMatAES.GiaiMa(rd[2].ToString());
                        }
                    }
                }

                void GanGachChan1Phan3(IXLCell cell, string text)
                {
                    if (cell == null || string.IsNullOrWhiteSpace(text)) return;
                    cell.Value = text;
                    int totalLen = text.Length;
                    int underlineLen = (int)Math.Ceiling(totalLen / 2.0);
                    int start = (totalLen - underlineLen) / 2;
                    cell.GetRichText().Substring(start, underlineLen).SetUnderline();
                }

                var cellA1 = ws.Cell("A1");
                cellA1.Value = string.IsNullOrWhiteSpace(dong1TrungDoan) ? khoangTrang : dong1TrungDoan;
                cellA1.Style.Font.FontName = "Times New Roman"; cellA1.Style.Font.FontSize = 13;
                cellA1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                if (!string.IsNullOrWhiteSpace(donviCapTrungDoan))
                {
                    var cellA2 = ws.Cell("A2"); cellA2.Value = donviCapTrungDoan;
                    cellA2.Style.Font.FontName = "Times New Roman"; cellA2.Style.Font.FontSize = 13;
                    cellA2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    var cellA3 = ws.Cell("A3"); cellA3.Value = donviCapTieuDoan;
                    cellA3.Style.Font.FontName = "Times New Roman"; cellA3.Style.Font.FontSize = 13; cellA3.Style.Font.Bold = true;
                    cellA3.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    GanGachChan1Phan3(cellA3, donviCapTieuDoan);
                }
                else
                {
                    var cellA2 = ws.Cell("A2"); cellA2.Value = donviCapTieuDoan;
                    cellA2.Style.Font.FontName = "Times New Roman"; cellA2.Style.Font.FontSize = 13; cellA2.Style.Font.Bold = true;
                    cellA2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    GanGachChan1Phan3(cellA2, donviCapTieuDoan);
                    ws.Cell("A3").Value = "";
                }

                // ⭐ TIÊU ĐỀ A6 (CHUYỂN ĐỔI "KHÔNG PL" THÀNH "KHÔNG PHÂN LOẠI")
                var cellA6 = ws.Cell("A6");
                string tmp = BaoMatAES.GiaiMa(phanLoai ?? "").Trim();
                string phanLoaiGiaiMa = string.IsNullOrEmpty(tmp) ? (phanLoai ?? "") : tmp;

                // Logic bổ sung:
                if (phanLoaiGiaiMa.Equals("KHÔNG PL", StringComparison.OrdinalIgnoreCase))
                {
                    phanLoaiGiaiMa = "KHÔNG PHÂN LOẠI";
                }

                cellA6.Value = $"CBCS ĐỀ NGHỊ {InHoa(phanLoaiGiaiMa)} TRONG PHONG TRÀO THI ĐUA \"VÌ ANTQ\" {chuoiThoiGian}";
                cellA6.Style.Font.FontName = "Times New Roman"; cellA6.Style.Font.FontSize = 14; cellA6.Style.Font.Bold = true;
                cellA6.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // --- PHẦN A7 (GIỮ NGUYÊN GỐC) ---
                var cellA7 = ws.Cell("A7");
                cellA7.Clear(XLClearOptions.Contents);
                var rt = cellA7.GetRichText();
                rt.AddText("(Kèm theo Báo cáo ").SetFontName("Times New Roman").SetFontSize(14).SetItalic();

                string kyHieuBaoCao = "...............";
                try
                {
                    using var cn = new SqliteConnection($"Data Source={fileDB}"); cn.Open();
                    using var cm = cn.CreateCommand(); cm.CommandText = "SELECT KyHieuBaoCao FROM ThongTin WHERE ID=1";
                    var res = cm.ExecuteScalar(); if (res != null) kyHieuBaoCao = BaoMatAES.GiaiMa(res.ToString());
                }
                catch { }

                rt.AddText($"số:            {kyHieuBaoCao}, ngày {ngay}/{thang}/{nam}").SetFontName("Times New Roman").SetFontSize(14).SetItalic().SetUnderline();
                string d2HienThi = string.IsNullOrWhiteSpace(donviCapTieuDoan) ? "      " : char.ToUpper(donviCapTieuDoan.ToLower()[0]) + donviCapTieuDoan.ToLower().Substring(1);
                rt.AddText(" của " + d2HienThi + ")").SetFontName("Times New Roman").SetFontSize(14).SetItalic();
                cellA7.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // --- PHẦN E4 (GIỮ NGUYÊN GỐC) ---
                var cellE4 = ws.Cell("E4");
                cellE4.Value = $"{diaDiem}, ngày {ngay} tháng {thang} năm {nam}";
                cellE4.Style.Font.FontName = "Times New Roman"; cellE4.Style.Font.FontSize = 14; cellE4.Style.Font.Italic = true;
                cellE4.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                Module_BanQuyen.DongDauExcel(wb);
                wb.SaveAs(fileDich);
                LastFilePath = fileDich; LinkDanTep = fileDich;
                Module_ThongBao.ThanhCong($"Đã xuất danh sách {data.Count} CBCS {phanLoai}!");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        public static bool LayThongTinDonVi(out string tenTrungDoan, out string tenTieuDoan, out string tomTatGhiChu)
        {
            tenTrungDoan = "";
            tenTieuDoan = "";
            tomTatGhiChu = "";

            try
            {
                string dbPath = Module_DanduongGPS.DuongDanCSDL2;
                if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
                    return false;

                using var cn = new SqliteConnection($"Data Source={dbPath}");
                cn.Open();

                using var cmd = cn.CreateCommand();
                cmd.CommandText = @"
            SELECT TenTrungDoan, TenTieuDoan, TomTatGhiChu
            FROM ThongTin
            ORDER BY ID ASC
            LIMIT 1";

                using var rd = cmd.ExecuteReader();
                if (!rd.Read())
                    return false;

                tenTrungDoan = SafeDecrypt(rd, "TenTrungDoan");
                tenTieuDoan = SafeDecrypt(rd, "TenTieuDoan");
                tomTatGhiChu = SafeDecrypt(rd, "TomTatGhiChu");

                return true;
            }
            catch
            {
                return false;
            }
        }
        private static string SafeDecrypt(SqliteDataReader rd, string columnName)
        {
            try
            {
                int idx = rd.GetOrdinal(columnName);
                if (idx < 0 || rd.IsDBNull(idx)) return "";
                string raw = rd.GetString(idx);
                if (string.IsNullOrWhiteSpace(raw)) return "";
                return BaoMatAES.GiaiMa(raw);
            }
            catch
            {
                return "";
            }
        }
        private static void VietTieuDeVaKyTen(IXLWorksheet ws, string phanLoai, int lastRow, string fileDB)
        {
            int dongKy = lastRow + 1;
            string hoTenKy = "";
            string csdlPath = Module_DanduongGPS.DuongDanCSDL2;
            string loaiBaoCao = "";
            string tuanBaoCao = "";

            try
            {
                using (var conn = new SqliteConnection($"Data Source={csdlPath}"))
                {
                    conn.Open();
                    using var cmd = new SqliteCommand("SELECT ChiHuyD FROM ThongTin WHERE ID = 1", conn);
                    using var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        hoTenKy = (BaoMatAES.GiaiMa(reader["ChiHuyD"]?.ToString() ?? "")).Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                Module_ThongBao.Loi("Lỗi khi lấy thông tin ChiHuyD từ CSDL:\n" + ex.Message);
            }
            string chucVuTieuDoanTruong = "";
            int dongDauTienID = -1;
            string chucVuNguoiKy = "";
            int dongNguoiKyID = -1;

            using (var cn = new SqliteConnection("Data Source=" + fileDB))
            {
                cn.Open();
                using (var cmd = new SqliteCommand("SELECT ID, HoVaTen, ChucVu FROM ChiHuyD ORDER BY ID ASC", cn))
                using (var rd = cmd.ExecuteReader())
                {
                    bool isFirst = true;
                    bool foundMatch = false;

                    while (rd.Read())
                    {
                        int id = Convert.ToInt32(rd["ID"]);
                        string hoTenRaw = rd["HoVaTen"]?.ToString() ?? "";
                        string chucVuRaw = rd["ChucVu"]?.ToString() ?? "";

                        string htDec = "";
                        string cvDec = "";
                        try { htDec = string.IsNullOrWhiteSpace(hoTenRaw) ? "" : BaoMatAES.GiaiMa(hoTenRaw).Trim(); }
                        catch { htDec = hoTenRaw.Trim(); }
                        if (string.IsNullOrEmpty(htDec)) htDec = hoTenRaw.Trim();

                        try { cvDec = string.IsNullOrWhiteSpace(chucVuRaw) ? "" : BaoMatAES.GiaiMa(chucVuRaw).Trim(); }
                        catch { cvDec = chucVuRaw.Trim(); }
                        if (string.IsNullOrEmpty(cvDec)) cvDec = chucVuRaw.Trim();

                        if (isFirst)
                        {
                            dongDauTienID = id;
                            chucVuTieuDoanTruong = cvDec;
                            isFirst = false;
                        }

                        if (htDec.Equals(hoTenKy, StringComparison.OrdinalIgnoreCase))
                        {
                            dongNguoiKyID = id;
                            chucVuNguoiKy = cvDec;
                            foundMatch = true;
                            break;
                        }
                    }

                    if (!foundMatch)
                    {
                        dongNguoiKyID = dongDauTienID;
                        chucVuNguoiKy = chucVuTieuDoanTruong;
                    }
                }
            }

            void GhiDongKy(string text)
            {
                var range = ws.Range(dongKy, 6, dongKy, 10);
                range.Merge();
                var c = ws.Cell(dongKy, 6);
                c.Value = text;
                c.Style.Font.Bold = true;
                c.Style.Font.FontName = "Times New Roman";
                c.Style.Font.FontSize = 14;
                c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                c.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                dongKy++;
            }

            if (dongNguoiKyID == dongDauTienID)
            {
                GhiDongKy(chucVuTieuDoanTruong);
            }
            else
            {
                GhiDongKy("KT. " + chucVuTieuDoanTruong);
                GhiDongKy(chucVuNguoiKy);
            }

            dongKy += 4;
            GhiDongKy(hoTenKy);

            string khoangTrang = "      ";
            string thang = khoangTrang;
            string nam = khoangTrang;
            string ngay = khoangTrang;
            string diaDiem = khoangTrang;

            try
            {
                using (var conn = new SqliteConnection($"Data Source={csdlPath}"))
                {
                    conn.Open();
                    using var cmd = new SqliteCommand("SELECT Thang, Nam, Ngay, DiaDiem FROM ThongTin WHERE ID = 1", conn);
                    using var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        thang = string.IsNullOrWhiteSpace(BaoMatAES.GiaiMa(reader["Thang"]?.ToString() ?? ""))
                                    ? khoangTrang
                                    : BaoMatAES.GiaiMa(reader["Thang"].ToString());

                        nam = string.IsNullOrWhiteSpace(BaoMatAES.GiaiMa(reader["Nam"]?.ToString() ?? ""))
                              ? khoangTrang
                              : BaoMatAES.GiaiMa(reader["Nam"].ToString());

                        ngay = string.IsNullOrWhiteSpace(BaoMatAES.GiaiMa(reader["Ngay"]?.ToString() ?? ""))
                               ? khoangTrang
                               : BaoMatAES.GiaiMa(reader["Ngay"].ToString());

                        diaDiem = string.IsNullOrWhiteSpace(BaoMatAES.GiaiMa(reader["DiaDiem"]?.ToString() ?? ""))
                                  ? khoangTrang
                                  : BaoMatAES.GiaiMa(reader["DiaDiem"].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Module_ThongBao.Loi("Lỗi khi lấy thông tin Thang/Nam/Ngay/DiaDiem từ CSDL:\n" + ex.Message);
            }
            try
            {
                using var conn = new SqliteConnection($"Data Source={csdlPath}");
                conn.Open();

                using var cmd = new SqliteCommand(
                    "SELECT ChonLoaiBaoCao, ChonTuan FROM ChonLoaiBaoCao WHERE ID = 1",
                    conn);

                using var rd = cmd.ExecuteReader();

                if (rd.Read())
                {
                    loaiBaoCao = BaoMatAES.GiaiMa(rd["ChonLoaiBaoCao"]?.ToString() ?? "").Trim();
                    tuanBaoCao = BaoMatAES.GiaiMa(rd["ChonTuan"]?.ToString() ?? "").Trim();
                }
            }
            catch
            {
            }

            // Them Tieu de trung doan 113
            string donviCapTrungDoan =
                string.IsNullOrWhiteSpace(TenTrungDoan)
                    ? khoangTrang
                    : TenTrungDoan;
            // Cell A1, A2, A3
            // ================= TIÊU ĐỀ ĐƠN VỊ (A1 - A2 - A3) =================
            // Lấy dữ liệu từ bảng ThongTin (ID = 1)
            string tenTrungDoanDong1 = "";
            string tenTrungDoanCSCD = "";
            string tenTieuDoanCSCD = "";
            try
            {
                using (var conn = new SqliteConnection($"Data Source={csdlPath}"))
                {
                    conn.Open();
                    using var cmd = new SqliteCommand(@"
            SELECT 
                textBox1_TenTrungDoanDong1,
                TenTrungDoan,
                TenTieuDoan
            FROM ThongTin
            WHERE ID = 1", conn);

                    using var rd = cmd.ExecuteReader();
                    if (rd.Read())
                    {
                        tenTrungDoanDong1 = BaoMatAES.GiaiMa(rd["textBox1_TenTrungDoanDong1"]?.ToString() ?? "").Trim();
                        tenTrungDoanCSCD = BaoMatAES.GiaiMa(rd["TenTrungDoan"]?.ToString() ?? "").Trim();
                        tenTieuDoanCSCD = BaoMatAES.GiaiMa(rd["TenTieuDoan"]?.ToString() ?? "").Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                Module_ThongBao.Loi("Lỗi lấy tiêu đề đơn vị từ CSDL:\n" + ex.Message);
            }
            string chuoiThoiGian;
            string thangHT = LayThangHeThong(); // Lấy tháng từ CSDL (Combobox Tháng xét thi đua)

            if (loaiBaoCao.Equals("Tuần", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(tuanBaoCao))
            {
                string tuanInHoa = tuanBaoCao.Trim().ToUpperInvariant();
                chuoiThoiGian = $"{tuanInHoa} THÁNG {thangHT}/{nam}"; // Đổi {thang} thành {thangHT}
            }
            else
            {
                chuoiThoiGian = $"THÁNG {thangHT}/{nam}"; // Đổi {thang} thành {thangHT}
            }
            // ⭐ FIX LỖI 1: Tối ưu lại hàm Gạch chân bằng phương thức an toàn của ClosedXML
            void GanGachChan1Phan3(IXLCell cell, string text)
            {
                if (cell == null || string.IsNullOrWhiteSpace(text)) return;

                cell.Value = text; // Gán trước để xóa mọi định dạng rich text cũ nếu có

                int totalLen = text.Length;
                int underlineLen = (int)Math.Round(totalLen / 3.0);
                if (underlineLen <= 0) return;

                int start = (totalLen - underlineLen) / 2;

                // Sử dụng Substring của GetRichText để set Underline, tránh dùng AddText liên tục sinh lỗi cấu trúc XML trên Office cũ
                cell.GetRichText().Substring(start, underlineLen).SetUnderline();
            }

            // ===== A1 : Trung đoàn dòng 1 (LUÔN GHI) =====
            var cellA1 = ws.Cell("A1");
            cellA1.Value = string.IsNullOrWhiteSpace(tenTrungDoanDong1) ? khoangTrang : tenTrungDoanDong1;
            cellA1.Style.Font.FontName = "Times New Roman";
            cellA1.Style.Font.FontSize = 13;
            cellA1.Style.Font.Bold = false;
            cellA1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellA1.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            // ===== PHÂN NHÁNH THEO A2 =====
            if (!string.IsNullOrWhiteSpace(tenTrungDoanCSCD))
            {
                // ===== A2 : Trung đoàn CSCD (KHÔNG ĐẬM) =====
                var cellA2 = ws.Cell("A2");
                cellA2.Value = tenTrungDoanCSCD;
                cellA2.Style.Font.FontName = "Times New Roman";
                cellA2.Style.Font.FontSize = 13;
                cellA2.Style.Font.Bold = false;
                cellA2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cellA2.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // ===== A3 : Tiểu đoàn (IN ĐẬM) =====
                var cellA3 = ws.Cell("A3");
                cellA3.Value = tenTieuDoanCSCD;
                cellA3.Style.Font.FontName = "Times New Roman";
                cellA3.Style.Font.FontSize = 13;
                cellA3.Style.Font.Bold = true;   // ✅ IN ĐẬM
                cellA3.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cellA3.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                // 🔥 GẠCH CHÂN 1/3 CHÍNH GIỮA
                GanGachChan1Phan3(cellA3, tenTieuDoanCSCD);
            }
            else
            {
                // ===== KHÔNG CÓ A2 =====

                // ===== A2 : Tiểu đoàn (IN ĐẬM) =====
                var cellA2 = ws.Cell("A2");
                cellA2.Value = tenTieuDoanCSCD;
                cellA2.Style.Font.FontName = "Times New Roman";
                cellA2.Style.Font.FontSize = 13;
                cellA2.Style.Font.Bold = true;   // ✅ IN ĐẬM
                cellA2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cellA2.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                GanGachChan1Phan3(cellA2, tenTieuDoanCSCD);
                // ===== A3 : ĐỂ TRỐNG =====
                var cellA3 = ws.Cell("A3");
                cellA3.Value = "";
                cellA3.Style.Font.Bold = false;
            }

            // A6
            var cellA6 = ws.Cell("A6");
            // Giải mã thử
            string tmp = BaoMatAES.GiaiMa(phanLoai ?? "").Trim();
            // Nếu chuỗi rỗng (do lỗi giải mã kép bị BaoMatAES nuốt), dùng lại chuỗi gốc ban đầu
            string phanLoaiGiaiMa = string.IsNullOrEmpty(tmp) ? (phanLoai ?? "") : tmp;
            cellA6.Value = $"CBCS ĐỀ NGHỊ {InHoa(phanLoaiGiaiMa)} TRONG PHONG TRÀO THI ĐUA \"VÌ ANTQ\" {chuoiThoiGian}";
            cellA6.Style.Font.FontName = "Times New Roman";
            cellA6.Style.Font.FontSize = 14;
            cellA6.Style.Font.Bold = true;
            cellA6.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellA6.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            // A7
            // ===== A7 =====
            var cellA7 = ws.Cell("A7");

            // Thay vì dùng ClearText, gán giá trị chuỗi đầy đủ trước rồi set RichText sau để ổn định XML
            string kyHieuBaoCao = "..............."; // mặc định nếu không có
            try
            {
                string csdl2 = Module_DanduongGPS.DuongDanCSDL2; // dùng trực tiếp Module
                if (File.Exists(csdl2))
                {
                    using var cn = new SqliteConnection($"Data Source={csdl2}");
                    cn.Open();
                    using var cmd = cn.CreateCommand();
                    cmd.CommandText = "SELECT KyHieuBaoCao FROM ThongTin WHERE ID=1";
                    var result = cmd.ExecuteScalar();
                    if (result != null && !string.IsNullOrWhiteSpace(result.ToString()))
                        kyHieuBaoCao = BaoMatAES.GiaiMa(result.ToString());
                }
            }
            catch
            {
                // im lặng nếu lỗi
            }

            // ===== Chuyển chữ in hoa từ CSDL thành chỉ in hoa chữ đầu =====
            string tieuDoanHienThi = "     "; // mặc định nếu trống
            if (!string.IsNullOrWhiteSpace(tenTieuDoanCSCD))
            {
                string s = tenTieuDoanCSCD.ToLower();            // chuyển toàn bộ thành chữ thường
                tieuDoanHienThi = char.ToUpper(s[0]) + s.Substring(1); // in hoa chữ đầu
            }

            string phanDau = "(Kèm theo Báo cáo ";
            string phanGiua = $"số:            {kyHieuBaoCao}, ngày {ngay}/{thang}/{nam}";
            string phanCuoi = " của " + tieuDoanHienThi + ")";
            string fullText = phanDau + phanGiua + phanCuoi;

            cellA7.Value = fullText;
            cellA7.Style.Font.FontName = "Times New Roman";
            cellA7.Style.Font.FontSize = 14;
            cellA7.Style.Font.Italic = true;
            cellA7.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellA7.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            // Set Underline cho phần giữa bằng Substring (cách an toàn nhất của ClosedXML)
            cellA7.GetRichText().Substring(phanDau.Length, phanGiua.Length).SetUnderline();

            // E4:J4
            var cellE4 = ws.Cell("E4");
            cellE4.Value = $"{diaDiem}, ngày {ngay} tháng {thang} năm {nam}";
            cellE4.Style.Font.FontName = "Times New Roman";
            cellE4.Style.Font.FontSize = 14;
            cellE4.Style.Font.Italic = true;
            cellE4.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellE4.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        public static void VietDuLieuCBCSVaoSheet(DataTable dt, List<DataRow> data, IXLWorksheet ws)
        {
            int rowStart = 10;
            int row = rowStart;
            var cols = dt.Columns.Cast<DataColumn>()
                .Where(c => c.ColumnName is not ("ID" or "STT" or "PhanLoai" or "GhiChu"))
                .ToList();

            // Ghi dữ liệu
            foreach (var r in data)
            {
                // ===== CỘT A – STT =====
                var cellSTT = ws.Cell(row, 1);
                cellSTT.Value = row - rowStart + 1;
                cellSTT.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cellSTT.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cellSTT.Style.Font.FontName = "Times New Roman";
                cellSTT.Style.Font.FontSize = 14;

                // ===== CÁC CỘT B → I =====
                for (int c = 0; c < cols.Count; c++)
                {
                    var cell = ws.Cell(row, c + 2);
                    string val = r[cols[c]]?.ToString() ?? "";

                    try
                    {
                        if (!string.IsNullOrWhiteSpace(val))
                            val = BaoMatAES.GiaiMa(val).Trim();
                    }


                    catch { }

                    cell.Value = val;
                    cell.Style.NumberFormat.Format = "@";
                    cell.Style.Font.FontName = "Times New Roman";
                    cell.Style.Font.FontSize = 13;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Căn lề theo cột
                    if (cell.Address.ColumnNumber == 2 || cell.Address.ColumnNumber == 5)
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    else
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                // ===== CỘT J – ĐẠT LOẠI =====
                var cellDatLoai = ws.Cell(row, 10);

                string rawPhanLoai = r["PhanLoai"]?.ToString() ?? "";
                string phanLoaiHienThi = "";

                if (!string.IsNullOrWhiteSpace(rawPhanLoai))
                {
                    try
                    {
                        string decVal = BaoMatAES.GiaiMa(rawPhanLoai);

                        if (!string.IsNullOrWhiteSpace(decVal))
                        {
                            phanLoaiHienThi = decVal.Trim();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log để debug, không làm crash chương trình
                        Debug.WriteLine($"[AES] Lỗi giải mã PhanLoai: {ex.Message}");
                    }
                }

                // Fallback
                if (string.IsNullOrWhiteSpace(phanLoaiHienThi))
                {
                    phanLoaiHienThi = rawPhanLoai.Trim();
                }

                // Ghi Excel
                cellDatLoai.Value = phanLoaiHienThi;

                cellDatLoai.Style.Font.FontName = "Times New Roman";
                cellDatLoai.Style.Font.FontSize = 13;
                cellDatLoai.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cellDatLoai.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                row++;
            }
            // Kẻ viền cho toàn bộ vùng bảng
            var tableRange = ws.Range(rowStart, 1, row - 1, 11);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            // Tổng cộng
            ws.Range(row, 1, row, 11).Merge();
            var tong = ws.Cell(row, 1);
            tong.Value = $"Tổng cộng: {data.Count} đồng chí./.";
            tong.Style.Font.Bold = true;
            tong.Style.Font.Italic = true;
            tong.Style.Font.FontName = "Times New Roman";
            tong.Style.Font.FontSize = 14;
            tong.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            string phanLoaiGiaiMa = "";
            try
            {
                phanLoaiGiaiMa = BaoMatAES.GiaiMa(data.FirstOrDefault()?["PhanLoai"]?.ToString() ?? "").Trim();
            }
            catch { }

            VietTieuDeVaKyTen(ws, phanLoaiGiaiMa, row, Module_DanduongGPS.DuongDanCSDL2);
        }
        public static void VietDuLieuTanBinhVaoSheet(DataTable dt, List<DataRow> data, IXLWorksheet ws)
        {
            int rowStart = 10;

            var listDTO = data.Select((r, index) => new CbcDTO
            {
                STT = index + 1,
                HoVaTen = BaoMatAES.GiaiMa(r["HoVaTen"]?.ToString() ?? "").Trim(),
                SoHieuCAND = "", // TÂN BINH KHÔNG CÓ SỐ HIỆU
                NamSinh = BaoMatAES.GiaiMa(r["NamSinh"]?.ToString() ?? "").Trim(),
                QueQuan = BaoMatAES.GiaiMa(r["QueQuan"]?.ToString() ?? "").Trim(),
                NgayVaoCAND = BaoMatAES.GiaiMa(r["NgayVaoCAND"]?.ToString() ?? "").Trim(),
                CapBac = BaoMatAES.GiaiMa(r["CapBac"]?.ToString() ?? "").Trim(),
                ChucVu = BaoMatAES.GiaiMa(r["ChucVu"]?.ToString() ?? "").Trim(),
                DonVi = BaoMatAES.GiaiMa(r["DonVi"]?.ToString() ?? "").Trim(),
                PhanLoai = BaoMatAES.GiaiMa(r["PhanLoai"]?.ToString() ?? "").Trim(),
                GhiChu = ""
            }).ToList();

            if (listDTO.Count == 0) return;

            // Đổ dữ liệu
            ws.Cell(rowStart, 1).InsertData(listDTO);

            // Format nguyên cụm
            int rowEnd = rowStart + listDTO.Count - 1;
            var dataRange = ws.Range(rowStart, 1, rowEnd, 11);

            dataRange.Style.Font.FontName = "Times New Roman";
            dataRange.Style.Font.FontSize = 13;
            dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.NumberFormat.Format = "@";

            // Căn trái
            ws.Range(rowStart, 2, rowEnd, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            ws.Range(rowStart, 5, rowEnd, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            // Căn giữa
            var centerCols = new[] { 1, 3, 4, 6, 7, 8, 9, 10, 11 };
            foreach (var col in centerCols)
            {
                ws.Range(rowStart, col, rowEnd, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Wrap-text
            foreach (var cell in ws.Range(rowStart, 11, rowEnd, 11).Cells())
            {
                if (cell.Value.ToString().Length > 15)
                {
                    cell.Style.Font.FontSize = 9;
                    cell.Style.Alignment.WrapText = true;
                }
            }

            // Tính tổng cộng
            int rowTongCong = rowEnd + 1;
            var rangeTong = ws.Range(rowTongCong, 1, rowTongCong, 11);
            rangeTong.Merge();
            rangeTong.Value = $"Tổng cộng: {listDTO.Count} đồng chí./.";
            rangeTong.Style.Font.Bold = true;
            rangeTong.Style.Font.Italic = true;
            rangeTong.Style.Font.FontName = "Times New Roman";
            rangeTong.Style.Font.FontSize = 14;
            rangeTong.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            VietTieuDeVaKyTen(ws, listDTO.First().PhanLoai, rowTongCong, Module_DanduongGPS.DuongDanCSDL2);
        }
        public static string GetTenSheet(string pl) => pl switch
        {
            "Loại 1" => "LOAI_1",
            "Loại 2" => "LOAI_2",
            "Loại 3" => "LOAI_3",
            "Loại 4" => "LOAI_4",
            _ => "KHONG_PL"
        };
        private static void FormatCell(IXLWorksheet ws, int row, int colStart, int colEnd)
        {
            for (int c = colStart; c <= colEnd; c++)
            {
                var cell = ws.Cell(row, c);
                cell.Style.Font.FontName = "Times New Roman";
                cell.Style.Font.FontSize = 13;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Alignment.Horizontal = (c == 1 || c == 4) ? XLAlignmentHorizontalValues.Left : XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.NumberFormat.Format = "@";
            }
        }
        // Hàm helper
        private static string Loai(string loai)
        {
            if (string.IsNullOrWhiteSpace(loai))
                return "";
            loai = loai.Trim().ToLowerInvariant();       // "loại 1"
            return char.ToUpper(loai[0]) + loai.Substring(1); // "Loại 1"
        }
        public static string TenTrungDoan
        {
            get
            {
                if (!_daNapThongTin)
                    NapThongTinDonVi();
                return _tenTrungDoan ?? "";
            }
        }
        public static string TenTieuDoan
        {
            get
            {
                if (!_daNapThongTin)
                    NapThongTinDonVi();
                return _tenTieuDoan ?? "";
            }
        }
        public static void TinhVaGhiChiHuyTinhToan(XLWorkbook wb)
        {
            if (wb == null) return;

            // ================== 1. ĐẾM BCH ==================
            int tongBCH = 0;
            string fileDB = Module_DanduongGPS.DuongDanCSDL2;

            using (var cn = new SqliteConnection("Data Source=" + fileDB))
            {
                cn.Open();
                using var cmd = new SqliteCommand("SELECT DonVi FROM DanhSach", cn);
                using var rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    try
                    {
                        string donVi = BaoMatAES.GiaiMa(rd["DonVi"]?.ToString() ?? "")
                                        .Trim()
                                        .ToUpperInvariant();

                        if (donVi == "BCH")
                            tongBCH++;
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            // ================== 2. LƯU TỔNG BCH ==================
            soLuongBCHD = tongBCH;
            soLuongBCHDLoai1 = 0;
            soLuongBCHDLoai2 = 0;
            soLuongBCHDLoai3 = 0;

            string deNghiRaw = "";

            try
            {
                using var conn = new SqliteConnection($"Data Source={fileDB}");
                conn.Open();

                using var cmd = new SqliteCommand("SELECT LoaiDeNghi FROM ThongTin WHERE ID = 1", conn);
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                    deNghiRaw = BaoMatAES.GiaiMa(reader["LoaiDeNghi"]?.ToString() ?? "");
            }
            catch (Exception ex)
            {
                Module_ThongBao.Loi("Lỗi khi lấy thông tin LoaiDeNghi:\n" + ex.Message);
            }

            string deNghi = deNghiRaw.Trim().ToUpperInvariant();

            if (tongBCH > 0)
            {
                switch (deNghi)
                {
                    case "LOẠI 1":
                        soLuongBCHDLoai1 = (int)Math.Round(tongBCH * 0.75);
                        soLuongBCHDLoai2 = tongBCH - soLuongBCHDLoai1;
                        break;

                    case "LOẠI 2":
                        soLuongBCHDLoai1 = (int)Math.Round(tongBCH * 0.5);
                        soLuongBCHDLoai2 = tongBCH - soLuongBCHDLoai1;
                        break;

                    case "LOẠI 3":
                    case "LOẠI 4":
                        soLuongBCHDLoai2 = (int)Math.Round(tongBCH * 0.5);
                        soLuongBCHDLoai3 = tongBCH - soLuongBCHDLoai2;
                        break;
                }
            }

            // ================== 3. GHI A11:G11 ==================
            var ws = wb.Worksheet("DE XUAT");
            // ================== TIÊU ĐỀ DANH SÁCH ==================
            string tieuDe = LayTieuDeBaoCao();

            var rangeTitle = ws.Range("A6:G6");

            // đảm bảo luôn in hoa
            rangeTitle.Value = tieuDe.ToUpperInvariant();

            // ===== Format =====
            rangeTitle.Style.Font.Bold = true;
            rangeTitle.Style.Font.FontName = "Times New Roman";
            rangeTitle.Style.Font.FontSize = 13;

            rangeTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            rangeTitle.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            // ================== XỬ LÝ DÒNG 11 (GHI CHÚ BCH) ==================
            if (tongBCH == 0)
            {
                // Nếu không có BCH: Xóa sổ luôn dòng 11 cho gọn file Excel.
                // Đã xóa thì tuyệt đối không được đụng chạm gì đến dòng 11 nữa.
                ws.Row(11).Delete();
            }
            else
            {
                // Nếu CÓ BCH: Thực hiện gộp ô, ghi nội dung và format
                var range = ws.Range("A11:G11");
                range.Clear(XLClearOptions.Contents);
                range.Merge();

                string noiDung = deNghi switch
                {
                    "LOẠI 1" => $"Tập thể D2 đạt Loại 1 thì BCH xét 75% (Loại 1: {soLuongBCHDLoai1} đ/c; Loại 2: {soLuongBCHDLoai2} đ/c)",
                    "LOẠI 2" => $"Tập thể D2 đạt Loại 2 thì BCH xét 50% (Loại 1: {soLuongBCHDLoai1} đ/c; Loại 2: {soLuongBCHDLoai2} đ/c)",
                    "LOẠI 3" => $"Tập thể D2 đạt Loại 3 thì BCH xét 50% (Loại 2: {soLuongBCHDLoai2} đ/c; Loại 3: {soLuongBCHDLoai3} đ/c)",
                    "LOẠI 4" => $"Tập thể D2 đạt Loại 4 thì BCH xét 50% (Loại 2: {soLuongBCHDLoai2} đ/c; Loại 3: {soLuongBCHDLoai3} đ/c)",
                    _ => $"Tập thể D2 đạt {deNghiRaw} thì BCH chưa xác định phương án xét"
                };

                ws.Cell(11, 1).Value = noiDung;

                // ================== FORMAT ==================
                var cell = ws.Cell(11, 1);

                cell.Style.Font.Bold = true;
                cell.Style.Font.FontName = "Times New Roman";
                cell.Style.Font.FontSize = 13;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            }
        }
        public static void XuatTrinhKyTanBinh(string duongDanLuu, bool xoaXinYKien = false)
        {
            try
            {
                // ================== 1. Lấy dữ liệu từ CSDL ==================
                string fileDB = Module_DanduongGPS.DuongDanCSDL2;
                DataTable dt = new();

                using (var cn = new SqliteConnection("Data Source=" + fileDB))
                {
                    cn.Open();
                    using var cmd = new SqliteCommand("SELECT * FROM DanhSach", cn);
                    using var rd = cmd.ExecuteReader();
                    dt.Load(rd);
                }

                if (dt.Rows.Count == 0)
                {
                    Module_ThongBao.DangXuLy("SDL không có dữ liệu để xuất!");
                    return;
                }
                // ================== 2. Chuẩn bị đường dẫn ==================
                string thuMucGoc = duongDanLuu;
                if (string.IsNullOrWhiteSpace(thuMucGoc))
                    throw new Exception("Bạn chưa chọn thư mục lưu!");
                string thangHT = LayThangHeThong();
                thuMucGoc = Path.Combine(
                    thuMucGoc,
                    $"DANH SÁCH PHÂN LOẠI THI ĐUA THÁNG {thangHT} NĂM {DateTime.Now:yyyy}"
                );
                // TỰ TẠO THƯ MỤC NẾU CHƯA CÓ
                Directory.CreateDirectory(thuMucGoc);

                int soThuTu = Directory.GetFiles(thuMucGoc, "*.xlsx").Length + 1;
                string fileName = $"{soThuTu}. DANH SÁCH TRÌNH KÝ TÂN BINH - {DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                string fileDich = Path.Combine(thuMucGoc, fileName);

                // ================== 3. Mở file Excel mẫu ==================
                string fileMau = Module_DanduongGPS.DuongDanCSDL4ex;
                if (!File.Exists(fileMau))
                    throw new Exception("Không tìm thấy file mẫu Excel!");

                using var wb = new XLWorkbook(fileMau);
                // ===== GIỮ SHEET DE XUAT + GIOI_THIEU (ẨN) =====
                string tenSheetCanGiu = "DE XUAT";
                string sheetAnKhongXoa = "GIOI_THIEU";
                var cacSheetCanXoa = wb.Worksheets
                    .Where(s =>
                        !s.Name.Equals(tenSheetCanGiu, StringComparison.OrdinalIgnoreCase) &&
                        !s.Name.Equals(sheetAnKhongXoa, StringComparison.OrdinalIgnoreCase) &&
                        s.Visibility == XLWorksheetVisibility.Visible
                    )
                    .ToList();
                foreach (var s in cacSheetCanXoa)
                {
                    wb.Worksheets.Delete(s.Name);
                }
                var ws = wb.Worksheet("DE XUAT");
                // ================== TIÊU ĐỀ DANH SÁCH ==================
                string tieuDe = LayTieuDeBaoCao();

                var rangeTitle = ws.Range("A6:G6");

                // đảm bảo luôn in hoa
                rangeTitle.Value = tieuDe.ToUpperInvariant();

                // ===== Format =====
                rangeTitle.Style.Font.Bold = true;
                rangeTitle.Style.Font.FontName = "Times New Roman";
                rangeTitle.Style.Font.FontSize = 13;
                rangeTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangeTitle.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                // ================== 4. Xóa dữ liệu cũ ==================
                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 11;
                if (lastRow >= 12)
                    ws.Rows(12, lastRow).Clear(XLClearOptions.Contents);

                // ================== 5. Header ==================
                ws.Cell("A1").Value = string.IsNullOrWhiteSpace(TenTieuDoan) ? "" : TenTieuDoan;
                ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                // ================== 5a. Địa điểm, ngày tháng năm ==================
                string khoangTrang = "      ";
                string thang = khoangTrang;
                string nam = khoangTrang;
                string ngay = khoangTrang;
                string diaDiem = khoangTrang;

                try
                {
                    using var conn = new SqliteConnection($"Data Source={fileDB}");
                    conn.Open();

                    using var cmd = new SqliteCommand("SELECT Thang, Nam, Ngay, DiaDiem FROM ThongTin WHERE ID = 1", conn);
                    using var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        thang = string.IsNullOrWhiteSpace(BaoMatAES.GiaiMa(reader["Thang"]?.ToString() ?? ""))
                                ? khoangTrang
                                : BaoMatAES.GiaiMa(reader["Thang"].ToString());

                        nam = string.IsNullOrWhiteSpace(BaoMatAES.GiaiMa(reader["Nam"]?.ToString() ?? ""))
                              ? khoangTrang
                              : BaoMatAES.GiaiMa(reader["Nam"].ToString());

                        ngay = string.IsNullOrWhiteSpace(BaoMatAES.GiaiMa(reader["Ngay"]?.ToString() ?? ""))
                               ? khoangTrang
                               : BaoMatAES.GiaiMa(reader["Ngay"].ToString());

                        diaDiem = string.IsNullOrWhiteSpace(BaoMatAES.GiaiMa(reader["DiaDiem"]?.ToString() ?? ""))
                                  ? khoangTrang
                                  : BaoMatAES.GiaiMa(reader["DiaDiem"].ToString());
                    }
                }
                catch { }

                // Ghi vào C4:G4
                var cellC4 = ws.Cell("C4");
                cellC4.Value = $"{diaDiem}, ngày {ngay} tháng {thang} năm {nam}";
                cellC4.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cellC4.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                // ================== 6. Tiêu đề danh sách ==================
                int row = 11;
                var header = ws.Range(row, 1, row, 7);
                header.Merge();
                header.Value = "DANH SÁCH PHÂN LOẠI TÂN BINH";
                header.Style.Font.Bold = true;
                header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                header.Style.Fill.BackgroundColor = XLColor.LightGray;
                header.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                row++;

                // ================== 7. Phân loại Tân binh ==================
                string[] cacLoai =
                {
            "LOẠI 1",
            "LOẠI 2",
            "LOẠI 3",
            "LOẠI 4",
            "KHÔNG PL"
        };

                foreach (string loai in cacLoai)
                {
                    var dsTheoLoai = dt.AsEnumerable()
                        .Where(r =>
                        {
                            try
                            {
                                return BaoMatAES.GiaiMa(r["PhanLoai"]?.ToString() ?? "")
                                    .Trim().ToUpperInvariant() == loai;
                            }
                            catch { return false; }
                        })
                        .ToList();

                    if (dsTheoLoai.Count == 0)
                        continue;

                    // ---- Tiêu đề loại ----
                    var rangeLoai = ws.Range(row, 1, row, 7);
                    rangeLoai.Merge();
                    rangeLoai.Value = $"{Loai(loai)}: {dsTheoLoai.Count} đồng chí";
                    rangeLoai.Style.Font.Bold = true;
                    rangeLoai.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    rangeLoai.Style.Fill.BackgroundColor = XLColor.LightGray;
                    rangeLoai.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    row++;

                    int stt = 1;

                    foreach (var r in dsTheoLoai)
                    {
                        ws.Cell(row, 1).Value = stt++; // STT
                        ws.Cell(row, 2).Value = BaoMatAES.GiaiMa(r["HoVaTen"]?.ToString() ?? ""); // Họ và tên
                        ws.Cell(row, 3).Value = BaoMatAES.GiaiMa(r["NamSinh"]?.ToString() ?? "");
                        ws.Cell(row, 4).Value = BaoMatAES.GiaiMa(r["ChucVu"]?.ToString() ?? "");
                        ws.Cell(row, 5).Value = BaoMatAES.GiaiMa(r["DonVi"]?.ToString() ?? "");
                        ws.Cell(row, 6).Value = BaoMatAES.GiaiMa(r["PhanLoai"]?.ToString() ?? "");
                        ws.Cell(row, 7).Value = BaoMatAES.GiaiMa(r["GhiChu"]?.ToString() ?? "");

                        // ========== Style ==========
                        string ghiChu = BaoMatAES.GiaiMa(r["GhiChu"]?.ToString() ?? "").Trim();
                        ws.Cell(row, 7).Value = ghiChu;
                        // ========== Style mặc định ==========
                        FormatCell(ws, row, 1, 7);
                        //// ========== Logic thu nhỏ Ghi chú ==========
                        if (!string.IsNullOrEmpty(ghiChu) && ghiChu.Length > 15)
                        {
                            var cellGhiChu = ws.Cell(row, 7);
                            cellGhiChu.Style.Font.FontSize = 8;
                            cellGhiChu.Style.Alignment.WrapText = true;
                        }
                        // Căn trái riêng cột B (Họ và tên) từ B13 trở xuống
                        if (row >= 13)
                            ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        row++;
                    }

                }

                // ================== 8. LẤY DỮ LIỆU TỪ BẢNG TyLe ==================
                DataTable dtTyLe = new DataTable();
                using (var cn = new SqliteConnection("Data Source=" + fileDB))
                {
                    cn.Open();
                    using var cmd = new SqliteCommand("SELECT * FROM TyLe ORDER BY ID", cn);
                    using var rd = cmd.ExecuteReader();
                    dtTyLe.Load(rd);
                }

                // ================== YÊU CẦU 1: GHI TỔNG QS ==================
                // Gộp ô A7:G7
                var rangeTQS = ws.Range(7, 1, 7, 7);
                rangeTQS.Merge();
                rangeTQS.Style.Font.FontName = "Times New Roman";
                rangeTQS.Style.Font.FontSize = 12;
                rangeTQS.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangeTQS.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Chuẩn bị nội dung: "Tổng QS " & ID1 Thong tin & " đồng chí (Trong đó: Loại1 ... Loại2 ... Loại3 ...)"
                string phanTramL1 = "";
                string phanTramL2 = "";
                string phanTramL3 = "";

                try
                {
                    string csdlPath = Module_DanduongGPS.DuongDanCSDL2;
                    using var conn = new SqliteConnection($"Data Source={csdlPath}");
                    conn.Open();

                    using var cmd = new SqliteCommand("SELECT PTLoai1, PTLoai2, PTLoai3 FROM ThongTin WHERE ID = 1", conn);
                    using var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        phanTramL1 = BaoMatAES.GiaiMa(reader["PTLoai1"]?.ToString() ?? "");
                        phanTramL2 = BaoMatAES.GiaiMa(reader["PTLoai2"]?.ToString() ?? "");
                        phanTramL3 = BaoMatAES.GiaiMa(reader["PTLoai3"]?.ToString() ?? "");
                    }
                }
                catch (Exception ex)
                {
                    Module_ThongBao.Loi("Lỗi load thông tin PTLoai từ CSDL:\n" + ex.Message);
                }
                string tongQSText = $"Tổng QS {dtTyLe.Rows[0]["KQ Can dat"]} đồng chí (Trong đó: " +
                                     $"Loại 1 {phanTramL1}% = {dtTyLe.Rows[1]["KQ Can dat"]} đ/c, " +
                                     $"Loại 2 {phanTramL2}% = {dtTyLe.Rows[2]["KQ Can dat"]} đ/c, " +
                                     $"Loại 3 {phanTramL3}% = {dtTyLe.Rows[3]["KQ Can dat"]} đ/c)";

                rangeTQS.Value = tongQSText;

                // ================== YÊU CẦU 2: COPY CHI TIẾT TyLe ==================


                // Tìm dòng cuối cột B
                int lastRowB = ws.Column(2).LastCellUsed()?.Address.RowNumber ?? 11;

                // Thêm 1 dòng trống dưới cột B, rồi gộp A:G để ghi "Ghi chú: ..."
                int startRow = lastRowB + 2; // xuống 1 dòng trống

                var rangeGhiChu = ws.Range(startRow, 1, startRow, 7);
                rangeGhiChu.Merge();
                rangeGhiChu.Value = "Ghi chú:";
                rangeGhiChu.Style.Font.FontName = "Times New Roman";
                rangeGhiChu.Style.Font.FontSize = 12;
                rangeGhiChu.Style.Font.Bold = true;
                rangeGhiChu.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                rangeGhiChu.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                startRow++; // xuống dòng tiếp theo để bắt đầu ghi dữ liệu TyLe
                            // Duyệt dữ liệu TyLe
                for (int i = 0; i < dtTyLe.Rows.Count && i < 6; i++) // chỉ từ ID 1 → 6
                {
                    DataRow r = dtTyLe.Rows[i];

                    // Kiểm tra KQ Can dat, nếu = 0 thì bỏ qua
                    if (int.TryParse(r["KQ Can dat"]?.ToString(), out int kqCanDat) && kqCanDat == 0)
                        continue;

                    string thongTin = r["Thong tin"]?.ToString() ?? "";
                    string kqGuiE29 = r["KQ Gui E29"]?.ToString() ?? "";
                    string ketLuan = r["Ket luan"]?.ToString() ?? "";

                    string cellValue = thongTin + " " + kqGuiE29;

                    // Nếu có Ket luan, ghép thêm " - Ket luan"
                    if (!string.IsNullOrWhiteSpace(ketLuan))
                        cellValue += " - " + ketLuan;

                    // Nếu là ID 1, ghép thêm " đồng chí./."
                    if (i == 0)
                        cellValue += " đồng chí.";
                    // Gộp ô A:G
                    var range = ws.Range(startRow, 1, startRow, 7);
                    range.Merge();
                    range.Value = cellValue;
                    range.Style.Font.FontName = "Times New Roman";
                    range.Style.Font.FontSize = 12;
                    range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    startRow++;
                    ws.Rows(12, startRow).AdjustToContents();
                }
                // ================== 8. Lưu file ==================
                if (xoaXinYKien)
                {
                    int lastRowUsed = ws.LastRowUsed()?.RowNumber() ?? 12;

                    for (int r = 12; r <= lastRowUsed; r++)
                    {
                        if (!ws.Cell(r, 7).IsMerged() && !ws.Cell(r, 2).IsEmpty())
                        {
                            ws.Cell(r, 7).Clear(XLClearOptions.Contents);
                        }
                    }
                }
                Module_BanQuyen.DongDauExcel(wb);
                wb.SaveAs(fileDich);
                LastFilePath = fileDich;
                LinkDanTep = fileDich;
                MoThuMucVaChonTep(fileDich);
                Module_ThongBao.ThanhCong("Xuất Trình Ký Tân binh thành công!");
            }
            catch (Exception ex)
            {
                Module_ThongBao.DangXuLy("Lỗi xuất Trình Ký:\n" + ex.Message);
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public static void XuatTrinhKyCBCS(string duongDanLuu)
        {
            NapThongTinDonVi();
            KetQuaTinhBCH kqBCH;
            try
            {
                // ================== 1. Lấy dữ liệu từ CSDL ==================
                string fileDB = Module_DanduongGPS.DuongDanCSDL2;
                DataTable dt = new();
                using (var cn = new SqliteConnection("Data Source=" + fileDB))
                {
                    cn.Open();
                    using var cmd = new SqliteCommand("SELECT * FROM DanhSach", cn);
                    using var rd = cmd.ExecuteReader();
                    dt.Load(rd);
                }
                if (dt.Rows.Count == 0)
                {
                    Module_ThongBao.DangXuLy("SDL không có dữ liệu để xuất!");
                    return;
                }
                string thuMucGoc = duongDanLuu;
                if (string.IsNullOrWhiteSpace(thuMucGoc))
                    throw new Exception("Bạn chưa chọn thư mục lưu!");
                string thangHT = LayThangHeThong();
                thuMucGoc = Path.Combine(
                    thuMucGoc,
                    $"DANH SÁCH PHÂN LOẠI THI ĐUA THÁNG {thangHT} NĂM {DateTime.Now:yyyy}"
                );
                Directory.CreateDirectory(thuMucGoc);
                int soFileExcel = Directory.GetFiles(thuMucGoc, "*.xlsx").Length;
                int soThuTu = soFileExcel + 1;
                // Ghép tên file với số thứ tự và thời gian
                string fileName = $"{soThuTu}. DANH SÁCH TRÌNH KÝ - {DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                string fileDich = Path.Combine(thuMucGoc, fileName);
                string fileMau = Module_DanduongGPS.DuongDanCSDL4ex;
                if (!File.Exists(fileMau))
                    throw new Exception("Không tìm thấy file mẫu Excel!");
                using var wb = new XLWorkbook(fileMau);
                // ===== CHỈ GIỮ LẠI SHEET "DE XUAT" =====
                string tenSheetCanGiu = "DE XUAT";

                var cacSheetCanXoa = wb.Worksheets
                    .Where(s => !s.Name.Equals(tenSheetCanGiu, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var s in cacSheetCanXoa)
                {
                    wb.Worksheets.Delete(s.Name);
                }

                // Lấy sheet để thao tác
                var ws = wb.Worksheet(tenSheetCanGiu);
                string khoangTrang = "      "; // Khoảng trắng nếu không có giá trị
                string thang = khoangTrang;
                string nam = khoangTrang;
                string ngay = khoangTrang;
                string diaDiem = khoangTrang;

                try
                {
                    string csdlPath = Module_DanduongGPS.DuongDanCSDL2;
                    using var conn = new SqliteConnection($"Data Source={csdlPath}");
                    conn.Open();

                    using var cmd = new SqliteCommand("SELECT Thang, Nam, Ngay, DiaDiem FROM ThongTin WHERE ID = 1", conn);
                    using var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        thang = string.IsNullOrWhiteSpace(BaoMatAES.GiaiMa(reader["Thang"]?.ToString() ?? ""))
                                ? khoangTrang
                                : BaoMatAES.GiaiMa(reader["Thang"].ToString());

                        nam = string.IsNullOrWhiteSpace(BaoMatAES.GiaiMa(reader["Nam"]?.ToString() ?? ""))
                              ? khoangTrang
                              : BaoMatAES.GiaiMa(reader["Nam"].ToString());

                        ngay = string.IsNullOrWhiteSpace(BaoMatAES.GiaiMa(reader["Ngay"]?.ToString() ?? ""))
                               ? khoangTrang
                               : BaoMatAES.GiaiMa(reader["Ngay"].ToString());

                        diaDiem = string.IsNullOrWhiteSpace(BaoMatAES.GiaiMa(reader["DiaDiem"]?.ToString() ?? ""))
                                  ? khoangTrang
                                  : BaoMatAES.GiaiMa(reader["DiaDiem"].ToString());
                    }
                }
                catch (Exception ex)
                {
                    Module_ThongBao.Loi("Lỗi load thông tin Ngày/Tháng/Năm/Địa điểm từ CSDL:\n" + ex.Message);
                }
                string donviCapTieuDoan =
                    string.IsNullOrWhiteSpace(TenTieuDoan)
                        ? khoangTrang
                        : TenTieuDoan;


                // Cell A3
                var cellA1 = ws.Cell("A1");
                cellA1.Value = $"{donviCapTieuDoan}"; // chắc bạn muốn một giá trị khác thì đổi vào đây
                cellA1.Style.Font.FontName = "Times New Roman";
                cellA1.Style.Font.FontSize = 13;
                cellA1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cellA1.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                // Gán vào ô C4:G4 đã gộp
                var cellC4G4 = ws.Cell("C4");
                cellC4G4.Value = $"{diaDiem}, ngày {ngay} tháng {thang} năm {nam}";
                cellC4G4.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cellC4G4.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                // ================== 4. Xóa dữ liệu cũ ==================
                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 11;
                if (lastRow >= 12)
                    ws.Rows(12, lastRow).Clear(XLClearOptions.Contents);
                // ================== 5. TÍNH BCH ==================
                Module_XuatPhanLoai.TinhVaGhiChiHuyTinhToan(wb);
                // ================== 6. COPY DANH SÁCH BCH ==================
                int row = 12;
                int stt = 1;
                var dsBCH = dt.AsEnumerable()
                    .Where(r =>
                    {
                        try
                        {
                            string chucVu = BaoMatAES.GiaiMa(r["DonVi"]?.ToString() ?? "")
                                .Trim()
                                .ToUpperInvariant();
                            return chucVu == "BCH";
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .ToList();

                //if (dsBCH.Count == 0)
                //{
                //    Module_ThongBao.DangXuLy("Không tìm thấy BCH trong CSDL!");

                //    return;
                //} Để phù họp với các đơn vị không có BCH, vẫn cho phép xuất nhưng sẽ bỏ qua phần BCH

                foreach (var r in dsBCH)
                {
                    ws.Cell(row, 1).Value = stt++;
                    ws.Cell(row, 2).Value = BaoMatAES.GiaiMa(r["HoVaTen"]?.ToString() ?? "");
                    ws.Cell(row, 3).Value = BaoMatAES.GiaiMa(r["NamSinh"]?.ToString() ?? "");
                    ws.Cell(row, 4).Value = BaoMatAES.GiaiMa(r["ChucVu"]?.ToString() ?? "");
                    ws.Cell(row, 5).Value = BaoMatAES.GiaiMa(r["DonVi"]?.ToString() ?? "");
                    ws.Cell(row, 6).Value = "";          // Đề xuất
                    ws.Cell(row, 7).Value = "Họp xét";   // Xin ý kiến

                    FormatCell(ws, row, 1, 7);

                    row++;
                }

                // ================== 6b. TIÊU ĐỀ: DANH SÁCH PHÂN LOẠI CBCS ==================
                var rangeHeader = ws.Range(row, 1, row, 7);
                rangeHeader.Merge();
                rangeHeader.Value = "DANH SÁCH PHÂN LOẠI CBCS";
                rangeHeader.Style.Font.Bold = true;
                rangeHeader.Style.Font.FontName = "Times New Roman";
                rangeHeader.Style.Font.FontSize = 13;
                rangeHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangeHeader.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                rangeHeader.Style.Fill.BackgroundColor = XLColor.LightGray;

                // Kẻ viền cho ô gộp
                rangeHeader.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rangeHeader.Style.Border.OutsideBorderColor = XLColor.Black;
                row++;
                // ================== 6c. CÁC LOẠI PHÂN LOẠI ==================
                string[] cacLoai =
                {
            "LOẠI 1",
            "LOẠI 2",
            "LOẠI 3",
            "LOẠI 4",
            "KHÔNG PL"
        };
                // Lấy danh sách đơn vị hợp lệ từ Module_DonVi
                string[] donViHopLe = Module_DonVi.LayDanhSachDonViUuTienArray()
                    .Where(x => !string.IsNullOrWhiteSpace(x))  // loại bỏ các giá trị null hoặc rỗng
                    .ToArray();

                foreach (string loai in cacLoai)
                {
                    // ===== ĐẾM SỐ LƯỢNG KHÔNG BCH =====
                    int soLuongKhongBCH = dt.AsEnumerable()
                        .Count(r =>
                        {
                            try
                            {
                                string phanLoai = BaoMatAES.GiaiMa(r["PhanLoai"]?.ToString() ?? "")
                                    .Trim().ToUpperInvariant();

                                string donVi = BaoMatAES.GiaiMa(r["DonVi"]?.ToString() ?? "")
                                    .Trim().ToUpperInvariant();

                                return phanLoai == loai && donVi != "BCH";
                            }
                            catch
                            {
                                return false;
                            }
                        });
                    // ===== Xác định số BCH dự kiến theo từng Loại =====
                    // ===== Xác định số BCH dự kiến theo từng Loại =====
                    int soLuongBCHTheoLoai = (dsBCH.Count == 0) ? 0 : loai switch
                    {
                        "LOẠI 1" => soLuongBCHDLoai1,
                        "LOẠI 2" => soLuongBCHDLoai2,
                        "LOẠI 3" => soLuongBCHDLoai3,
                        "LOẠI 4" => soLuongBCHDLoai4,
                        "KHÔNG PL" => soLuongBCHDKhongPhanLoai,
                        _ => 0
                    };
                    // ===== BỎ QUA NẾU KHÔNG CÓ AI =====
                    if (soLuongKhongBCH == 0 && soLuongBCHTheoLoai == 0)
                        continue;
                    // ===== TIÊU ĐỀ LOẠI =====
                    var rangeLoai = ws.Range(row, 1, row, 7);
                    rangeLoai.Merge();

                    // Dùng hàm Loai để viết "LOẠI 1" → "Loại 1"
                    rangeLoai.Value = $"{Loai(loai)}: {soLuongKhongBCH} đồng chí";
                    rangeLoai.Style.Font.Bold = true;
                    rangeLoai.Style.Font.FontName = "Times New Roman";
                    rangeLoai.Style.Font.FontSize = 13;
                    rangeLoai.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    rangeLoai.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    rangeLoai.Style.Fill.BackgroundColor = XLColor.LightGray;
                    // Kẻ viền cho ô gộp
                    rangeLoai.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    rangeLoai.Style.Border.OutsideBorderColor = XLColor.Black;
                    row++;
                    // ===== STT RIÊNG CHO TỪNG LOẠI =====
                    int sttChung = 1;
                    // ===== BCH ĐỀ NGHỊ (Chừa ô trống cho tất cả các Loại) =====
                    for (int i = 0; i < soLuongBCHTheoLoai; i++)
                    {
                        ws.Cell(row, 1).Value = sttChung++;
                        ws.Cell(row, 2).Value = "";
                        ws.Cell(row, 3).Value = "";
                        ws.Cell(row, 4).Value = "";
                        ws.Cell(row, 5).Value = "BCH";
                        ws.Cell(row, 6).Value = Loai(loai); // <-- Chuyển "LOẠI 1" thành "Loại 1"
                        ws.Cell(row, 7).Value = "Họp xét";
                        FormatCell(ws, row, 1, 7);
                        row++;
                    }
                    // ===== CBCS (KHÔNG BCH) =====
                    var dsCBCS = dt.AsEnumerable()
                        .Where(r =>
                        {
                            try
                            {
                                string phanLoai = BaoMatAES.GiaiMa(r["PhanLoai"]?.ToString() ?? "")
                                    .Trim().ToUpperInvariant();

                                string donVi = BaoMatAES.GiaiMa(r["DonVi"]?.ToString() ?? "")
                                    .Trim().ToUpperInvariant();

                                return phanLoai == loai
                                       && donVi != "BCH"
                                       && donViHopLe.Contains(donVi);
                            }
                            catch
                            {
                                return false;
                            }
                        })
                        .ToList();
                    ws.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    foreach (var r in dsCBCS)
                    {
                        ws.Cell(row, 1).Value = sttChung++;
                        ws.Cell(row, 2).Value = BaoMatAES.GiaiMa(r["HoVaTen"]?.ToString() ?? "");
                        ws.Cell(row, 3).Value = BaoMatAES.GiaiMa(r["NamSinh"]?.ToString() ?? "");
                        ws.Cell(row, 4).Value = BaoMatAES.GiaiMa(r["ChucVu"]?.ToString() ?? "");
                        ws.Cell(row, 5).Value = BaoMatAES.GiaiMa(r["DonVi"]?.ToString() ?? "");
                        ws.Cell(row, 6).Value = BaoMatAES.GiaiMa(r["PhanLoai"]?.ToString() ?? "");
                        ws.Cell(row, 7).Value = BaoMatAES.GiaiMa(r["GhiChu"]?.ToString() ?? "");
                        // Bắt Ghi chú vào biến riêng và làm sạch khoảng trắng
                        string ghiChu = BaoMatAES.GiaiMa(r["GhiChu"]?.ToString() ?? "").Trim();
                        ws.Cell(row, 7).Value = ghiChu;
                        FormatCell(ws, row, 1, 7);
                        // Logic an toàn: Thu nhỏ cỡ chữ 10 và ngắt dòng nếu vượt quá 15 ký tự
                        if (!string.IsNullOrEmpty(ghiChu) && ghiChu.Length > 15)
                        {
                            var cellGhiChu = ws.Cell(row, 7);
                            cellGhiChu.Style.Font.FontSize = 8;
                            cellGhiChu.Style.Alignment.WrapText = true;
                        }
                        row++;
                    }
                }
                // Tìm dòng cuối cùng có dữ liệu trong cột B
                var lastRowCotB = ws.Column(2).LastCellUsed()?.Address.RowNumber ?? 1;

                // 1️⃣ B1 → B9: căn giữa
                if (lastRowCotB >= 1)
                {
                    int endRowCenter = Math.Min(9, lastRowCotB); // nếu dữ liệu ít hơn 9 dòng
                    var rangeBTop = ws.Range(1, 2, endRowCenter, 2);
                    rangeBTop.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                // 2️⃣ B11 → B cuối: căn trái
                if (lastRowCotB >= 11)
                {
                    var rangeBLeft = ws.Range(11, 2, lastRowCotB, 2);
                    rangeBLeft.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                }
                // ================== 8. LẤY DỮ LIỆU TỪ BẢNG TyLe ==================
                DataTable dtTyLe = new DataTable();
                using (var cn = new SqliteConnection("Data Source=" + fileDB))
                {
                    cn.Open();
                    using var cmd = new SqliteCommand("SELECT * FROM TyLe ORDER BY ID", cn);
                    using var rd = cmd.ExecuteReader();
                    dtTyLe.Load(rd);
                }
                // ================== YÊU CẦU 1: GHI TỔNG QS ==================
                // Gộp ô A7:G7
                var rangeTQS = ws.Range(7, 1, 7, 7);
                rangeTQS.Merge();
                rangeTQS.Style.Font.FontName = "Times New Roman";
                rangeTQS.Style.Font.FontSize = 12;
                rangeTQS.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangeTQS.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Chuẩn bị nội dung: "Tổng QS " & ID1 Thong tin & " đồng chí (Trong đó: Loại1 ... Loại2 ... Loại3 ...)"
                string phanTramL1 = "";
                string phanTramL2 = "";
                string phanTramL3 = "";

                try
                {
                    string csdlPath = Module_DanduongGPS.DuongDanCSDL2;
                    using var conn = new SqliteConnection($"Data Source={csdlPath}");
                    conn.Open();

                    using var cmd = new SqliteCommand("SELECT PTLoai1, PTLoai2, PTLoai3 FROM ThongTin WHERE ID = 1", conn);
                    using var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        phanTramL1 = BaoMatAES.GiaiMa(reader["PTLoai1"]?.ToString() ?? "");
                        phanTramL2 = BaoMatAES.GiaiMa(reader["PTLoai2"]?.ToString() ?? "");
                        phanTramL3 = BaoMatAES.GiaiMa(reader["PTLoai3"]?.ToString() ?? "");
                    }
                }
                catch (Exception ex)
                {
                    Module_ThongBao.Loi("Lỗi load thông tin PTLoai từ CSDL:\n" + ex.Message);
                }
                string tongQSText = $"Tổng QS {dtTyLe.Rows[0]["KQ Can dat"]} đồng chí (Trong đó: " +
                                     $"Loại 1 {phanTramL1}% = {dtTyLe.Rows[1]["KQ Can dat"]} đ/c, " +
                                     $"Loại 2 {phanTramL2}% = {dtTyLe.Rows[2]["KQ Can dat"]} đ/c, " +
                                     $"Loại 3 {phanTramL3}% = {dtTyLe.Rows[3]["KQ Can dat"]} đ/c)";

                rangeTQS.Value = tongQSText;

                // ================== YÊU CẦU 2: COPY CHI TIẾT TyLe ==================


                // Tìm dòng cuối cột B
                int lastRowB = ws.Column(2).LastCellUsed()?.Address.RowNumber ?? 11;

                // Thêm 1 dòng trống dưới cột B, rồi gộp A:G để ghi "Ghi chú: ..."
                int startRow = lastRowB + 2; // xuống 1 dòng trống

                var rangeGhiChu = ws.Range(startRow, 1, startRow, 7);
                rangeGhiChu.Merge();
                rangeGhiChu.Value = "Ghi chú:";
                rangeGhiChu.Style.Font.FontName = "Times New Roman";
                rangeGhiChu.Style.Font.FontSize = 12;
                rangeGhiChu.Style.Font.Bold = true;
                rangeGhiChu.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                rangeGhiChu.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                startRow++; // xuống dòng tiếp theo để bắt đầu ghi dữ liệu TyLe

                // Duyệt dữ liệu TyLe
                for (int i = 0; i < dtTyLe.Rows.Count && i < 6; i++) // chỉ từ ID 1 → 6
                {
                    DataRow r = dtTyLe.Rows[i];

                    // Kiểm tra KQ Can dat, nếu = 0 thì bỏ qua
                    if (int.TryParse(r["KQ Can dat"]?.ToString(), out int kqCanDat) && kqCanDat == 0)
                        continue;

                    string thongTin = r["Thong tin"]?.ToString() ?? "";
                    string kqGuiE29 = r["KQ Gui E29"]?.ToString() ?? "";
                    string ketLuan = r["Ket luan"]?.ToString() ?? "";

                    string cellValue = thongTin + " " + kqGuiE29;

                    // Nếu có Ket luan, ghép thêm " - Ket luan"
                    if (!string.IsNullOrWhiteSpace(ketLuan))
                        cellValue += " - " + ketLuan;

                    // Nếu là ID 1, ghép thêm " đồng chí./."
                    if (i == 0)
                        cellValue += " đồng chí.";

                    // Gộp ô A:G
                    var range = ws.Range(startRow, 1, startRow, 7);
                    range.Merge();
                    range.Value = cellValue;
                    range.Style.Font.FontName = "Times New Roman";
                    range.Style.Font.FontSize = 12;
                    range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    startRow++;
                }
                // ================== XÓA DÒNG RỖNG NẾU KHÔNG CÓ BCH ==================
                // Dùng dsBCH.Count == 0 để nhận biết đơn vị không có BCH
                if (dsBCH.Count == 0)
                {
                    // Lệnh Delete() này sẽ xóa nguyên dòng 11 và tự động đôn tất cả các dòng bên dưới lên 1 nấc
                    ws.Row(11).Delete();
                }
                // ================== 7. Lưu & mở thư mục ==================
                ws.Rows(12, startRow).AdjustToContents();
                Module_BanQuyen.DongDauExcel(wb);
                wb.SaveAs(fileDich);
                LastFilePath = fileDich;
                LinkDanTep = fileDich;
                // Mở thư mục và tự động chọn file vừa tạo
                MoThuMucVaChonTep(fileDich);
                Module_ThongBao.ThanhCong("Xuất Trình Ký thành công!");
            }
            catch (Exception ex)
            {
                Module_ThongBao.Loi("Lỗi xuất Trình Ký:\n" + ex.Message);
                MessageBox.Show("Lỗi xuất Trình Ký: \n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public static void XuatTatCaPhanLoai()
        {
            try
            {
                NapThongTinDonVi(); // 🔥 BẮT BUỘC PHẢI CÓ
                string fileDB = Module_DanduongGPS.DuongDanCSDL2;
                string fileMau = Module_DanduongGPS.DuongDanCSDL4ex;

                // Lấy dữ liệu từ CSDL
                DataTable dt = new();
                using (var cn = new SqliteConnection("Data Source=" + fileDB))
                {
                    cn.Open();
                    using var cmd = new SqliteCommand("SELECT * FROM DanhSach", cn);
                    using var rd = cmd.ExecuteReader();
                    dt.Load(rd);
                }

                // Chuẩn bị thư mục lưu
                string thuMucGoc = Module_XuatPhanLoai.GetLinkLuuDuongDanTepXuat(true);
                if (string.IsNullOrWhiteSpace(thuMucGoc))
                    throw new Exception("Bạn chưa chọn thư mục lưu!");
                string thangHT = LayThangHeThong();
                string tenThuMuc = $"DANH SÁCH PHÂN LOẠI THI ĐUA THÁNG {thangHT} NĂM {DateTime.Now:yyyy}";
                string thuMucDich = Path.Combine(thuMucGoc, tenThuMuc);
                Directory.CreateDirectory(thuMucDich);

                int sttFile = Directory.GetFiles(thuMucDich, "*.xlsx").Length + 1;
                string fileName = $"{sttFile}. DANH SÁCH TẤT CẢ PHÂN LOẠI - {DateTime.Now:yyyyMMdd-HHmmss}.xlsx";
                string fileDich = Path.Combine(thuMucDich, fileName);

                // Mở file mẫu
                using var wb = new XLWorkbook(fileMau);

                // ================== GHI TÓM TẮT GHI CHÚ ==================
                string ghiChuTomTat = "";

                using (var conn = new SqliteConnection($"Data Source={fileDB};Mode=ReadOnly"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT TomTatGhiChu FROM ThongTin WHERE ID = 1";
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                            ghiChuTomTat = BaoMatAES.GiaiMa(result.ToString()) ?? "";
                    }
                }

                if (wb.Worksheets.Contains("BAO CAO TONG HOP"))
                {
                    var wsTongHop = wb.Worksheet("BAO CAO TONG HOP");
                    var range = wsTongHop.Range("L11:L12");
                    if (!range.IsMerged())
                        range.Merge();

                    var cell = wsTongHop.Cell("L11");
                    cell.Value = ghiChuTomTat;
                    cell.Style.NumberFormat.Format = "@";
                    cell.Style.Alignment.WrapText = true;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                }
                ///==================
                // ===== XÁC ĐỊNH PHIÊN BẢN PHẦN MỀM (CBCS / TÂN BINH) =====
                bool laTanBinh = Module_TaiKhoan
                    .LayPhienBanPhanMem()
                    .Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                ///==================
                string[] phanLoaiArr = { "Loại 1", "Loại 2", "Loại 3", "Loại 4", "Không PL" };
                HashSet<string> usedSheets = new();

                foreach (string pl in phanLoaiArr)
                {
                    var data = dt.AsEnumerable()
                        .Where(r =>
                        {
                            string raw = r["PhanLoai"]?.ToString() ?? "";
                            try { raw = BaoMatAES.GiaiMa(raw).Trim(); } catch { }

                            if (pl == "Chưa PL") // trống
                                return string.IsNullOrWhiteSpace(raw);

                            return raw.Equals(pl.Trim(), StringComparison.OrdinalIgnoreCase);
                        })
                        .ToList();

                    if (data.Count == 0) continue;

                    string sheetName = GetTenSheet(pl);

                    IXLWorksheet ws = wb.Worksheets.Contains(sheetName)
                        ? wb.Worksheet(sheetName)
                        : wb.AddWorksheet(sheetName);

                    if (laTanBinh)
                    {
                        VietDuLieuTanBinhVaoSheet(dt, data, ws); // ❌ không gán Số hiệu
                    }
                    else
                    {
                        VietDuLieuCBCSVaoSheet(dt, data, ws);        // ✅ CBCS đầy đủ
                    }
                    usedSheets.Add(ws.Name);
                }
                // ================== XÓA SHEET KHÔNG DÙNG ==================
                var sheetsToDelete = wb.Worksheets
                    .Where(ws =>
                        !usedSheets.Contains(ws.Name) &&
                        ws.Name != "BAO CAO TONG HOP" &&
                        ws.Name != "GIOI_THIEU"
                    )
                    .ToList();

                foreach (var ws in sheetsToDelete)
                    wb.Worksheets.Delete(ws.Name); // <-- dùng ws.Name thay vì ws
                Module_BanQuyen.DongDauExcel(wb);
                // Lưu file xuất ra
                wb.SaveAs(fileDich);
                LastFilePath = fileDich;
                LinkDanTep = fileDich;

                // SỬA LỖI THÔNG BÁO 2 LẦN: Đã vô hiệu hóa thông báo ở hàm gốc
                // Module_ThongBao.DangXuLy("Xuất tất cả phân loại thành công!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public static void XuatTatCaPhanLoai(string fileXuat)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileXuat))
                    throw new Exception("Đường dẫn file xuất không hợp lệ.");

                // Gọi logic gốc để tạo file
                XuatTatCaPhanLoai();

                // Sau khi tạo xong, LastFilePath đã có
                if (string.IsNullOrWhiteSpace(LastFilePath) || !File.Exists(LastFilePath))
                    throw new Exception("Không tạo được file phân loại.");

                // Nếu file tạo ra KHÁC fileXuat → copy/ghi đè
                if (!LastFilePath.Equals(fileXuat, StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(LastFilePath, fileXuat, true);

                    // SỬA LỖI TẠO 2 TỆP: Xóa tệp thừa được tạo ra ở hàm gốc sau khi đã copy đến đích
                    File.Delete(LastFilePath);
                }

                // Đồng bộ link
                LinkDanTep = fileXuat;
                LastFilePath = fileXuat;

                // HIỂN THỊ 1 THÔNG BÁO DUY NHẤT VÀ CHÍNH XÁC:
                Module_ThongBao.ThanhCong("Xuất tất cả phân loại thành công!");
            }
            catch (Exception ex)
            {
                Module_ThongBao.Loi("Lỗi xuất tất cả phân loại:\n" + ex.Message);
            }
        }
        public static string LayTieuDeBaoCao()
        {
            try
            {
                string csdl = Module_DanduongGPS.DuongDanCSDL2;

                using var conn = new SqliteConnection($"Data Source={csdl}");
                conn.Open();

                string loaiBaoCao = "";
                string chonTuan = "";
                string nam = "";

                // ===== Lấy dữ liệu =====
                using (var cmd = new SqliteCommand(
                    @"SELECT 
                c.ChonLoaiBaoCao,
                c.ChonTuan,
                t.Nam
            FROM ChonLoaiBaoCao c
            LEFT JOIN ThongTin t ON t.ID = 1
            WHERE c.ID = 1", conn))
                using (var rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        try { loaiBaoCao = BaoMatAES.GiaiMa(rd["ChonLoaiBaoCao"]?.ToString() ?? "").Trim(); } catch { }
                        try { chonTuan = BaoMatAES.GiaiMa(rd["ChonTuan"]?.ToString() ?? "").Trim(); } catch { }
                        try { nam = BaoMatAES.GiaiMa(rd["Nam"]?.ToString() ?? "").Trim(); } catch { }
                    }
                }

                // Gọi hàm lấy tháng hệ thống chỉ 1 lần duy nhất ở đây
                string thangHT = LayThangHeThong();

                if (string.IsNullOrWhiteSpace(thangHT) || string.IsNullOrWhiteSpace(nam))
                    return "";

                // ===== Báo cáo THÁNG =====
                if (loaiBaoCao.Equals("Tháng", StringComparison.OrdinalIgnoreCase))
                {
                    return $"DANH SÁCH ĐỀ NGHỊ XÉT PHÂN LOẠI THI ĐUA THÁNG {thangHT}/{nam}";
                }

                // ===== Báo cáo TUẦN =====
                if (loaiBaoCao.Equals("Tuần", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(chonTuan))
                        return $"DANH SÁCH ĐỀ NGHỊ XÉT PHÂN LOẠI THI ĐUA THÁNG {thangHT}/{nam}";

                    return $"DANH SÁCH ĐỀ NGHỊ XÉT PHÂN LOẠI THI ĐUA {chonTuan.ToUpper()} THÁNG {thangHT}/{nam}";
                }

                return "";
            }
            catch
            {
                return "";
            }
        }
        public static string LayTenFileBaoCao()
        {
            try
            {
                string csdl = Module_DanduongGPS.DuongDanCSDL2;

                using var conn = new SqliteConnection($"Data Source={csdl}");
                conn.Open();

                string loai = "";
                string tuan = "";
                string nam = "";

                using var cmd = new SqliteCommand(
                @"SELECT c.ChonLoaiBaoCao, c.ChonTuan, t.Nam
          FROM ChonLoaiBaoCao c
          LEFT JOIN ThongTin t ON t.ID = 1
          WHERE c.ID = 1", conn);

                using var rd = cmd.ExecuteReader();

                if (rd.Read())
                {
                    try { loai = BaoMatAES.GiaiMa(rd["ChonLoaiBaoCao"]?.ToString() ?? ""); } catch { }
                    try { tuan = BaoMatAES.GiaiMa(rd["ChonTuan"]?.ToString() ?? ""); } catch { }
                    try { nam = BaoMatAES.GiaiMa(rd["Nam"]?.ToString() ?? ""); } catch { }
                }

                loai = loai.Trim();
                tuan = tuan.Trim();
                nam = nam.Trim();

                // Gọi hàm lấy tháng hệ thống chỉ 1 lần duy nhất ở đây
                string thangHT = LayThangHeThong();

                if (loai.Equals("Tuần", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(tuan))
                        return $"DANH SÁCH PHÂN LOẠI THI ĐUA {tuan.ToUpper()} THÁNG {thangHT}-{nam}.xlsx";
                }
                return $"DANH SÁCH PHÂN LOẠI THI ĐUA THÁNG {thangHT}-{nam}.xlsx";
            }
            catch
            {
                return $"PHAN_LOAI_THI_DUA_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            }
        }
        public static string LayThangHeThong()
        {
            try
            {
                using var conn = new SqliteConnection($"Data Source={Module_DanduongGPS.DuongDanCSDL2}");
                conn.Open();

                // ⭐ THÊM DÒNG NÀY: Đảm bảo bảng tồn tại trước khi truy vấn (Fix lỗi sập ngầm)
                string sqlCreate = @"CREATE TABLE IF NOT EXISTS ThangHeThong (ID INTEGER PRIMARY KEY, Thang TEXT);";
                using (var cmdCreate = new SqliteCommand(sqlCreate, conn))
                {
                    cmdCreate.ExecuteNonQuery();
                }

                using var cmd = new SqliteCommand("SELECT Thang FROM ThangHeThong WHERE ID = 1", conn);
                var res = cmd.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                {
                    // Đọc trực tiếp, không giải mã
                    string thangGiaiMa = res.ToString().Trim();
                    if (!string.IsNullOrEmpty(thangGiaiMa))
                    {
                        return thangGiaiMa.PadLeft(2, '0'); // Đảm bảo luôn ra định dạng 01, 02...
                    }
                }
            }
            catch { }

            return DateTime.Now.ToString("MM"); // Chỉ lấy tháng máy tính khi CSDL thực sự hỏng
        }
        public static string LayNamHeThong()
        {
            try
            {
                using var conn = new SqliteConnection($"Data Source={Module_DanduongGPS.DuongDanCSDL2}");
                conn.Open();
                using var cmd = new SqliteCommand("SELECT Nam FROM ThongTin WHERE ID = 1", conn);
                var res = cmd.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                {
                    string namGiaiMa = BaoMatAES.GiaiMa(res.ToString()).Trim();
                    if (!string.IsNullOrEmpty(namGiaiMa)) return namGiaiMa;
                }
            }
            catch { }
            // Chỉ lấy năm của máy tính làm phương án dự phòng cuối cùng nếu CSDL hỏng
            return DateTime.Now.Year.ToString();
        }
    }
    //Trong C# (và lập trình hướng đối tượng nói chung), đoạn mã bạn cung cấp được gọi là kỹ thuật Data Transfer Object, viết tắt là DTO.
    public class CbcDTO
    {
        public int STT { get; set; }
        public string HoVaTen { get; set; }
        public string SoHieuCAND { get; set; }
        public string NamSinh { get; set; }
        public string QueQuan { get; set; }
        public string NgayVaoCAND { get; set; }
        public string CapBac { get; set; }
        public string ChucVu { get; set; }
        public string DonVi { get; set; }
        public string PhanLoai { get; set; }
        public string GhiChu { get; set; }
    }
}

