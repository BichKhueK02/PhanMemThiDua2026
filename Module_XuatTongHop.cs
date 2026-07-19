using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace PhanMemThiDua2026
{
    internal static class Module_XuatTongHop
    {
        public static string? LinkDanTep;
        public static string TEN_TRUNG_DOAN;
        public static string TEN_TIEU_DOAN;
        public static string TEN_TRUNG_DOAN_DONG_1;
        public static string TOM_TAT_GHI_CHU;
        public static void NapThongTinDonVi()
        {
            LayThongTinDonVi(
                out TEN_TRUNG_DOAN_DONG_1,
                out TEN_TRUNG_DOAN,
                out TEN_TIEU_DOAN,
                out TOM_TAT_GHI_CHU
            );
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
        public static void LayThongTinDonVi(
     out string tenTrungDoanDong1,
     out string tenTrungDoan,
     out string tenTieuDoan,
     out string tomTatGhiChu)
        {
            tenTrungDoanDong1 = "";
            tenTrungDoan = "";
            tenTieuDoan = "";
            tomTatGhiChu = "";
            string dbPath = Module_DanduongGPS.DuongDanCSDL2;
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
                return;
            try
            {
                using var cn = new SqliteConnection($"Data Source={dbPath}");
                cn.Open();

                using var cmd = cn.CreateCommand();
                cmd.CommandText = @"
            SELECT
                textBox1_TenTrungDoanDong1,
                TenTrungDoan,
                TenTieuDoan,
                TomTatGhiChu
            FROM ThongTin
            ORDER BY ID ASC
            LIMIT 1";
                using var rd = cmd.ExecuteReader();
                if (!rd.Read()) return;
                tenTrungDoanDong1 = SafeDecrypt(rd["textBox1_TenTrungDoanDong1"]);
                tenTrungDoan = SafeDecrypt(rd["TenTrungDoan"]);
                tenTieuDoan = SafeDecrypt(rd["TenTieuDoan"]);
                tomTatGhiChu = SafeDecrypt(rd["TomTatGhiChu"]);
            }
            catch
            {
                // im lặng – không phá luồng
            }
        }
        public static void XuatBaoCaoTongHop(string fileXuat)
        {
            string tuanBaoCao = "";
            string loaiBaoCao = "";
            try
            {
                NapThongTinDonVi(); // 🔥 BẮT BUỘC PHẢI CÓ
                if (string.IsNullOrWhiteSpace(fileXuat) || !File.Exists(fileXuat))
                    throw new Exception("File Excel tổng hợp không tồn tại.");

                LinkDanTep = fileXuat;

                using var wb = new XLWorkbook(fileXuat);

                // 👉 BẮT ĐÚNG SHEET
                var ws = wb.Worksheet("BAO CAO TONG HOP");

                string csdl2 = Module_DanduongGPS.DuongDanCSDL2;
                if (string.IsNullOrWhiteSpace(csdl2) || !File.Exists(csdl2))
                    throw new Exception("Không tìm thấy CSDL csdl2.");

                // ====================================================================================
                // ⭐ LẤY KÝ HIỆU ĐƠN VỊ VÀ GHI VÀO Ô A11 (Đã cập nhật dấu phẩy và Font Size 12)
                // ====================================================================================
                string kyHieuTieuDoan = "";
                string kyHieuTrungDoan = "";

                try
                {
                    using var cnKyHieu = new SqliteConnection($"Data Source={csdl2}");
                    cnKyHieu.Open();
                    // Lọc chuẩn xác chỉ lấy ID = 1 theo yêu cầu
                    using var cmdKyHieu = new SqliteCommand("SELECT KyHieu_TrungDoan, KyHieu_TieuDoan FROM KyHieu_DonVi WHERE ID = 1", cnKyHieu);
                    using var rdKyHieu = cmdKyHieu.ExecuteReader();

                    if (rdKyHieu.Read())
                    {
                        // Lấy dữ liệu và giải mã an toàn qua module V2
                        kyHieuTrungDoan = SafeDecrypt(rdKyHieu["KyHieu_TrungDoan"]);
                        kyHieuTieuDoan = SafeDecrypt(rdKyHieu["KyHieu_TieuDoan"]);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[XuatBaoCaoTongHop] Lỗi lấy Ký hiệu đơn vị: {ex.Message}");
                }

                // Ghép chuỗi: Phân cách bằng dấu phẩy theo yêu cầu. 
                // Xử lý thông minh: Nếu 1 trong 2 chuỗi trống thì không in dấu phẩy thừa.
                kyHieuTieuDoan = kyHieuTieuDoan.Trim();
                kyHieuTrungDoan = kyHieuTrungDoan.Trim();

                string chuoiKyHieuGhep = "";
                if (!string.IsNullOrEmpty(kyHieuTieuDoan) && !string.IsNullOrEmpty(kyHieuTrungDoan))
                {
                    chuoiKyHieuGhep = $"{kyHieuTieuDoan}, {kyHieuTrungDoan}";
                }
                else if (!string.IsNullOrEmpty(kyHieuTieuDoan))
                {
                    chuoiKyHieuGhep = kyHieuTieuDoan;
                }
                else if (!string.IsNullOrEmpty(kyHieuTrungDoan))
                {
                    chuoiKyHieuGhep = kyHieuTrungDoan;
                }

                // Gán vào ô A11 và định dạng sơ bộ
                var cellA11 = ws.Cell("A11");
                cellA11.Value = chuoiKyHieuGhep;
                cellA11.Style.Font.FontName = "Times New Roman";

                // ⭐ Cập nhật Font Size thành 12 theo yêu cầu
                cellA11.Style.Font.FontSize = 12;
                cellA11.Style.Font.Bold = false;
                cellA11.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cellA11.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                // ====================================================================================

                // ===== 4. BIẾN THỜI GIAN & ĐỊA ĐIỂM =====
                string khoangTrang = "     "; // Khoảng trắng nếu không có giá trị
                string thang = khoangTrang;
                string nam = khoangTrang;
                string ngay = khoangTrang;
                string diaDiem = khoangTrang;
                string deNghi = khoangTrang; // Loại đề nghị

                try
                {
                    string csdlPath = Module_DanduongGPS.DuongDanCSDL2;
                    using var conn = new SqliteConnection($"Data Source={csdlPath}");
                    conn.Open();

                    // Lấy luôn cột LoaiDeNghi
                    using var cmd = new SqliteCommand("SELECT Thang, Nam, Ngay, DiaDiem, LoaiDeNghi FROM ThongTin WHERE ID = 1", conn);
                    using var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        thang = string.IsNullOrWhiteSpace(reader["Thang"]?.ToString())
                            ? khoangTrang
                            : BaoMatAES.GiaiMa(reader["Thang"].ToString());

                        nam = string.IsNullOrWhiteSpace(reader["Nam"]?.ToString())
                            ? khoangTrang
                            : BaoMatAES.GiaiMa(reader["Nam"].ToString());

                        ngay = string.IsNullOrWhiteSpace(reader["Ngay"]?.ToString())
                            ? khoangTrang
                            : BaoMatAES.GiaiMa(reader["Ngay"].ToString());

                        diaDiem = string.IsNullOrWhiteSpace(reader["DiaDiem"]?.ToString())
                            ? khoangTrang
                            : BaoMatAES.GiaiMa(reader["DiaDiem"].ToString());

                        deNghi = string.IsNullOrWhiteSpace(reader["LoaiDeNghi"]?.ToString())
                            ? khoangTrang
                            : BaoMatAES.GiaiMa(reader["LoaiDeNghi"].ToString());
                    }
                }
                catch (Exception ex)
                {
                    Module_ThongBao.Loi("Lỗi khi load thông tin Ngày/Tháng/Năm/Địa điểm/Đề nghị từ CSDL:\n" + ex.Message);
                }
                string tenTrungDoan =
      string.IsNullOrWhiteSpace(Module_XuatTongHop.TEN_TRUNG_DOAN)
          ? khoangTrang
          : Module_XuatTongHop.TEN_TRUNG_DOAN;
                string tenTrungDoanDong1 = "";
                string tenTrungDoanCSCD = "";
                string tenTieuDoanCSCD = "";

                // Lấy từ Module đã load
                tenTrungDoanDong1 = string.IsNullOrWhiteSpace(Module_XuatTongHop.TEN_TRUNG_DOAN_DONG_1)
                    ? khoangTrang
                    : Module_XuatTongHop.TEN_TRUNG_DOAN_DONG_1;

                tenTrungDoanCSCD = string.IsNullOrWhiteSpace(Module_XuatTongHop.TEN_TRUNG_DOAN)
                    ? ""
                    : Module_XuatTongHop.TEN_TRUNG_DOAN;

                tenTieuDoanCSCD = string.IsNullOrWhiteSpace(Module_XuatTongHop.TEN_TIEU_DOAN)
                    ? khoangTrang
                    : Module_XuatTongHop.TEN_TIEU_DOAN;



                void GanGachChan1Phan3(IXLCell cell, string text)
                {
                    if (cell == null) return;
                    if (string.IsNullOrWhiteSpace(text)) return;

                    int totalLen = text.Length;
                    int underlineLen = (int)Math.Round(totalLen / 3.0);
                    if (underlineLen <= 0) return;

                    int start = (totalLen - underlineLen) / 2;

                    var rich = cell.GetRichText();
                    rich.ClearText(); // ❗ KHÔNG xóa cell, chỉ reset richtext

                    if (start > 0)
                        rich.AddText(text.Substring(0, start));

                    rich.AddText(text.Substring(start, underlineLen))
                        .SetUnderline();

                    int end = start + underlineLen;
                    if (end < totalLen)
                        rich.AddText(text.Substring(end));
                }

                // ===== A1 : Trung đoàn dòng 1 (LUÔN GHI – KHÔNG ĐẬM) =====
                var cellA1 = ws.Cell("A1");
                cellA1.Value = string.IsNullOrWhiteSpace(tenTrungDoanDong1)
                    ? khoangTrang
                    : tenTrungDoanDong1;

                cellA1.Style.Font.FontName = "Times New Roman";
                cellA1.Style.Font.FontSize = 13;
                cellA1.Style.Font.Bold = false;
                cellA1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cellA1.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

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

                    // ===== A3 : Tiểu đoàn (IN ĐẬM + GẠCH CHÂN 1/3) =====
                    var cellA3 = ws.Cell("A3");
                    cellA3.Value = tenTieuDoanCSCD;
                    cellA3.Style.Font.FontName = "Times New Roman";
                    cellA3.Style.Font.FontSize = 13;
                    cellA3.Style.Font.Bold = true;
                    cellA3.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cellA3.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // 🔥 Gạch chân dòng CUỐI
                    GanGachChan1Phan3(cellA3, tenTieuDoanCSCD);
                }
                else
                {
                    // ===== KHÔNG CÓ A2 → A2 LÀ DÒNG CUỐI =====
                    var cellA2 = ws.Cell("A2");
                    cellA2.Value = tenTieuDoanCSCD;
                    cellA2.Style.Font.FontName = "Times New Roman";
                    cellA2.Style.Font.FontSize = 13;
                    cellA2.Style.Font.Bold = true;
                    cellA2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cellA2.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // 🔥 Gạch chân dòng CUỐI
                    GanGachChan1Phan3(cellA2, tenTieuDoanCSCD);

                    // ===== A3 : ĐỂ TRỐNG =====
                    var cellA3 = ws.Cell("A3");
                    cellA3.Value = "";
                    cellA3.Style.Font.Bold = false;
                }

                //========================
                var cellH4 = ws.Cell("H4");
                cellH4.Value = $"{diaDiem}, ngày {ngay} tháng {thang} năm {nam}";
                cellH4.Style.Font.FontName = "Times New Roman";
                cellH4.Style.Font.FontSize = 14;
                cellH4.Style.Font.Italic = true;
                cellH4.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cellH4.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                try
                {
                    using var conn = new SqliteConnection($"Data Source={csdl2}");
                    conn.Open();

                    using var cmd = new SqliteCommand(
                        "SELECT ChonLoaiBaoCao, ChonTuan FROM ChonLoaiBaoCao ORDER BY ID DESC LIMIT 1",
                        conn);

                    using var rd = cmd.ExecuteReader();

                    if (rd.Read())
                    {
                        loaiBaoCao = BaoMatAES.GiaiMa(rd["ChonLoaiBaoCao"]?.ToString() ?? "");
                        tuanBaoCao = BaoMatAES.GiaiMa(rd["ChonTuan"]?.ToString() ?? "");
                    }
                }
                catch { }
                string chuoiThoiGian;

                loaiBaoCao = (loaiBaoCao ?? "").Trim().ToUpper();
                tuanBaoCao = (tuanBaoCao ?? "").Trim();

                // 👉 GỌI HÀM LẤY THÁNG HỆ THỐNG TỪ MODULE XUẤT PHÂN LOẠI
                string thangHT = Module_XuatPhanLoai.LayThangHeThong();

                if (loaiBaoCao.Contains("TUẦN"))
                {
                    // nếu dữ liệu là "Tuần 1" → lấy số 1
                    if (tuanBaoCao.ToUpper().Contains("TUẦN"))
                    {
                        tuanBaoCao = tuanBaoCao.ToUpper().Replace("TUẦN", "").Trim();
                    }

                    if (string.IsNullOrWhiteSpace(tuanBaoCao))
                        tuanBaoCao = "1";

                    // ✅ Đổi 'thang' thành 'thangHT'
                    chuoiThoiGian = $"TUẦN {tuanBaoCao} THÁNG {thangHT}/{nam}";
                }
                else
                {
                    // ✅ Đổi 'thang' thành 'thangHT'
                    chuoiThoiGian = $"THÁNG {thangHT}/{nam}";
                }
                var tieuDe = ws.Range("A6:M6");

                tieuDe.Merge();
                tieuDe.Value = $"DANH SÁCH TỔNG HỢP CÁC LOẠI {chuoiThoiGian}";

                tieuDe.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                tieuDe.Style.Font.Bold = true;
                tieuDe.Style.Font.FontName = "Times New Roman";
                tieuDe.Style.Font.FontSize = 14;

                // ===== 6. DÒNG A7 (RICH TEXT) =====
                var cellA7 = ws.Cell("A7");
                cellA7.Clear(XLClearOptions.Contents);

                var rt = cellA7.GetRichText();
                rt.AddText("(Kèm theo Báo cáo ")
                  .SetFontName("Times New Roman")
                  .SetFontSize(14)
                  .SetItalic();

                string kyHieuBaoCao = "...............";
                try
                {
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
                catch { }

                string tieuDoanHienThi = string.IsNullOrWhiteSpace(tenTieuDoanCSCD)
                    ? ""  // nếu rỗng thì dùng chuỗi trống
                    : char.ToUpper(tenTieuDoanCSCD.ToLower()[0]) + tenTieuDoanCSCD.ToLower().Substring(1);
                rt.AddText($"số:            {kyHieuBaoCao}, ngày {ngay}/{thang}/{nam}")
                  .SetFontName("Times New Roman").SetFontSize(14).SetItalic().SetUnderline();
                rt.AddText(" của " + tieuDoanHienThi + ")").SetFontName("Times New Roman").SetFontSize(14).SetItalic();

                cellA7.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cellA7.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                // ===== 7. ĐỌC TOÀN BỘ BẢNG TyLe (1 LẦN DUY NHẤT) =====
                var tyLe = new Dictionary<int, (int hienTai, int canDat)>();

                using (var conn = new SqliteConnection($"Data Source={csdl2}"))
                {
                    conn.Open();
                    using var cmd = new SqliteCommand(
                        "SELECT ID, [KQ Hien tai], [KQ Can dat] FROM TyLe", conn);
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
                ws.Cell("L11").Value = string.IsNullOrWhiteSpace(Module_XuatTongHop.TOM_TAT_GHI_CHU) ? "" : Module_XuatTongHop.TOM_TAT_GHI_CHU;
                // E12
                ws.Cell("E12").FormulaA1 =
                "=IFERROR((E11*100)/F11," +
                "\"Dữ liệu từ tệp csdl2 có thể không đúng hoặc CSDL này không tồn tại, khôi phục lại bản xuất xưởng\")";

                // F12
                ws.Cell("F12").FormulaA1 =
                "=IFERROR(IF(B11=0,0,(F11*100)/B11)," +
                "\"Dữ liệu từ tệp csdl2 có thể không đúng hoặc CSDL này không tồn tại, khôi phục lại bản xuất xưởng\")";

                // G12
                ws.Cell("G12").FormulaA1 =
                "=IFERROR(100-F12," +
                "\"Dữ liệu từ tệp csdl2 có thể không đúng hoặc CSDL này không tồn tại, khôi phục lại bản xuất xưởng\")";

                // H12
                ws.Cell("H12").FormulaA1 =
                "=IFERROR(IF(OR(H11=\"\",H11=0),\"0%\",(H11*100)/B11 & \"%\")," +
                "\"Dữ liệu từ tệp csdl2 có thể không đúng hoặc CSDL này không tồn tại, khôi phục lại bản xuất xưởng\")";

                // I12
                ws.Cell("I12").FormulaA1 =
                "=IFERROR(IF(OR(I11=\"\",I11=0),\"0%\",(I11*100)/B11 & \"%\")," +
                "\"Dữ liệu từ tệp csdl2 có thể không đúng hoặc CSDL này không tồn tại, khôi phục lại bản xuất xưởng\")";

                // ===== ĐỊNH DẠNG xx.xx% =====
                ws.Cell("E12").Style.NumberFormat.Format = "00.00\"%\"";
                ws.Cell("F12").Style.NumberFormat.Format = "00.00\"%\"";
                ws.Cell("G12").Style.NumberFormat.Format = "00.00\"%\"";
                ws.Cell("H12").Style.NumberFormat.Format = "00.00\"%\"";
                ws.Cell("I12").Style.NumberFormat.Format = "00.00\"%\"";
                // ===== 9. KÝ TÊN =====
                VietKyTenBaoCaoTongHop(ws, csdl2);
                // ===== 10. LƯU FILE =====
                wb.Save();
            }
            catch (Exception ex)
            {
                Module_ThongBao.DangXuLy("Lỗi xuất báo cáo tổng hợp: " + ex.Message);
            }
        }
        // KÝ TÊN – LẤY TỪ CSDL
        private static void VietKyTenBaoCaoTongHop(IXLWorksheet ws, string fileDB)
        {
            string hoTenKy = "     "; // Khoảng trắng mặc định
            try
            {
                string csdlPath = Module_DanduongGPS.DuongDanCSDL2;
                using var conn = new SqliteConnection($"Data Source={csdlPath}");
                conn.Open();

                using var cmd = new SqliteCommand("SELECT ChiHuyD FROM ThongTin WHERE ID = 1", conn);
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    // Cắt Trim() ngay từ đầu phòng hờ khoảng trắng
                    string chiHuyRaw = reader["ChiHuyD"]?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrWhiteSpace(chiHuyRaw))
                    {
                        string decoded = BaoMatAES.GiaiMa(chiHuyRaw);
                        hoTenKy = string.IsNullOrEmpty(decoded) ? chiHuyRaw : decoded.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                Module_ThongBao.Loi("Lỗi khi load thông tin Chỉ huy từ CSDL:\n" + ex.Message);
            }

            string chucVuTieuDoanTruong = "";
            string chucVuNguoiKy = "";
            int idDauTien = -1;
            int idNguoiKy = -1;

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

                        // Bắt buộc Trim() ngay lúc lấy ra để tránh lỗi StartsWith("v2|")
                        string hoTenRaw = rd["HoVaTen"]?.ToString()?.Trim() ?? "";
                        string chucVuRaw = rd["ChucVu"]?.ToString()?.Trim() ?? "";

                        // Giải mã an toàn (Hàm GiaiMa đã bọc try-catch)
                        string htDec = BaoMatAES.GiaiMa(hoTenRaw);
                        if (string.IsNullOrEmpty(htDec)) htDec = hoTenRaw;

                        string cvDec = BaoMatAES.GiaiMa(chucVuRaw);
                        if (string.IsNullOrEmpty(cvDec)) cvDec = chucVuRaw;

                        // Lưu người đầu tiên làm Tiểu đoàn trưởng (dự phòng)
                        if (isFirst)
                        {
                            idDauTien = id;
                            chucVuTieuDoanTruong = cvDec;
                            isFirst = false;
                        }

                        // So khớp người ký
                        if (htDec.Equals(hoTenKy, StringComparison.OrdinalIgnoreCase))
                        {
                            idNguoiKy = id;
                            chucVuNguoiKy = cvDec;
                            foundMatch = true;
                            break;
                        }
                    }

                    // Nếu không khớp ai thì dùng mặc định người đầu tiên
                    if (!foundMatch)
                    {
                        idNguoiKy = idDauTien;
                        chucVuNguoiKy = chucVuTieuDoanTruong;
                    }
                }
            }
            // 🔥 SỬA CHỖ NÀY NÈ: Xử lý logic người ký và ký thay (KT.)
            if (idNguoiKy == idDauTien)
            {
                // Trưởng đơn vị trực tiếp ký
                ws.Cell("L14").Value = chucVuTieuDoanTruong.ToUpper();
                ws.Cell("L15").Value = ""; // Dọn sạch ô L15 phòng hờ template có chữ rác
            }
            else
            {
                // Cấp phó ký thay
                ws.Cell("L14").Value = ("KT. " + chucVuTieuDoanTruong).ToUpper();
                ws.Cell("L15").Value = chucVuNguoiKy.ToUpper();
            }

            ws.Cell("L19").Value = hoTenKy;
            foreach (string addr in new[] { "L14", "L15", "L19" })
            {
                var c = ws.Cell(addr);
                c.Style.Font.FontName = "Times New Roman";
                c.Style.Font.FontSize = 14;
                c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                c.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                if (addr != "L19") c.Style.Font.Bold = true;
            }
        }
    }
}
