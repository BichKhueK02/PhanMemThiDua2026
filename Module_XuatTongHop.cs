///Phiên bản 2
///
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Text.RegularExpressions;
namespace PhanMemThiDua2026
{
    internal static class Module_XuatTongHop
    {
        public static string? LinkDanTep;
        public static string TEN_TRUNG_DOAN = "";
        public static string TEN_TIEU_DOAN = "";
        public static string TEN_TRUNG_DOAN_DONG_1 = "";
        public static string TOM_TAT_GHI_CHU = "";
        private const string O_MUC_II_CO_DINH = "I11";
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
                return Module_BaoMatAES.GiaiMa(s);
            }
            catch
            {
                return "";
            }
        }
        /// <summary>Giải mã an toàn; nếu kết quả rỗng thì trả về giá trị mặc định (vd: khoảng trắng mẫu).</summary>
        private static string SafeDecryptHoacMacDinh(object? value, string macDinh)
        {
            string s = SafeDecrypt(value ?? DBNull.Value);
            return string.IsNullOrWhiteSpace(s) ? macDinh : s;
        }
        //Chuyển từ Chữ IN HOA "TIỂU ĐOÀN 2"sang chữ thường "Tiểu đoàn 2"
        public static void LayThongTinDonVi(out string tenTrungDoanDong1, out string tenTrungDoan, out string tenTieuDoan, out string tomTatGhiChu)
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
            catch (Exception ex)
            {
                // không phá luồng xuất báo cáo, chỉ ghi vết để dò lỗi
                Debug.WriteLine("[LayThongTinDonVi] " + ex.Message);
            }
        }
        private static string ChuyenDoiLoaiDeNghi(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            // Chuẩn hóa chuỗi (xóa khoảng trắng thừa, chuyển về chữ hoa để so sánh)
            string normalized = input.Trim().ToUpperInvariant();
            return normalized switch
            {
                "ĐVQT" => "Đơn vị Quyết thắng",
                "ĐVTT" => "Đơn vị Tiên tiến",
                "HTNV" or "HTNT" or "HTVT" => "Hoàn thành nhiệm vụ",   // HTNV là mã chuẩn; giữ HTNT/HTVT để tương thích dữ liệu cũ
                "KHTNV" => "Không hoàn thành nhiệm vụ",
                _ => input // Giữ nguyên giá trị gốc nếu không khớp các mã trên
            };
        }
        private static void VietKyTenBaoCaoTongHop(IXLWorksheet ws, string fileDB)
        {
            string hoTenKy = "     "; // Khoảng trắng mặc định
            try
            {
                using var conn = new SqliteConnection($"Data Source={fileDB}");
                conn.Open();
                using var cmd = new SqliteCommand("SELECT ChiHuyD FROM ThongTin WHERE ID = 1", conn);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    // Cắt Trim() ngay từ đầu phòng hờ khoảng trắng
                    string chiHuyRaw = reader["ChiHuyD"]?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrWhiteSpace(chiHuyRaw))
                    {
                        string decoded = Module_BaoMatAES.GiaiMa(chiHuyRaw);
                        // Giải mã thất bại: dữ liệu cũ dạng chữ thường thì dùng tiếp, còn chuỗi mã hóa "v2|..." thì bỏ trống (không in chuỗi mã lên báo cáo)
                        string ketQua = string.IsNullOrEmpty(decoded)
                            ? (chiHuyRaw.StartsWith("v2|", StringComparison.Ordinal) ? "" : chiHuyRaw)
                            : decoded.Trim();
                        if (!string.IsNullOrWhiteSpace(ketQua)) hoTenKy = ketQua;
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
            try
            {
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
                            string htDec = Module_BaoMatAES.GiaiMa(hoTenRaw);
                            htDec = (string.IsNullOrEmpty(htDec) ? hoTenRaw : htDec).Trim();
                            string cvDec = Module_BaoMatAES.GiaiMa(chucVuRaw);
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
            }
            catch (Exception ex)
            {
                // Lỗi đọc bảng ChiHuyD: giữ nguyên phần ký tên của bản mẫu, không làm hỏng cả báo cáo
                Debug.WriteLine("[VietKyTenBaoCaoTongHop] " + ex.Message);
                return;
            }
            // 🔥 SỬA CHỖ NÀY NÈ: Xử lý logic người ký và ký thay (KT.)
            if (idNguoiKy == idDauTien)
            {
                // Trưởng đơn vị trực tiếp ký
                ws.Cell("L14").Value = chucVuTieuDoanTruong.ToUpperInvariant();
                ws.Cell("L15").Value = ""; // Dọn sạch ô L15 phòng hờ template có chữ rác
            }
            else
            {
                // Cấp phó ký thay
                ws.Cell("L14").Value = ("KT. " + chucVuTieuDoanTruong).ToUpperInvariant();
                ws.Cell("L15").Value = chucVuNguoiKy.ToUpperInvariant();
            }
            ws.Cell("L19").Value = hoTenKy;
            foreach (string addr in new[] { "L14", "L15", "L19" })
            {
                var c = ws.Cell(addr);
                c.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                c.Style.Font.FontSize = 14;
                c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                c.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                if (addr != "L19") c.Style.Font.Bold = true;
            }
        }
        public static void XuatBaoCaoTongHop(string fileXuat)
        {
            try
            {
                NapThongTinDonVi(); // 🔥 BẮT BUỘC PHẢI CÓ
                if (string.IsNullOrWhiteSpace(fileXuat) || !File.Exists(fileXuat))
                    throw new Exception("File Excel tổng hợp không tồn tại.");
                using var wb = new XLWorkbook(fileXuat);
                // Chốt chế độ Năm/Tháng MỘT LẦN cho cả lần xuất (tránh lệch sheet/nội dung nếu chế độ đổi giữa chừng)
                bool laNam = Module_HeThong.IsCheDoXetThiDuaNam();
                // 👉 CHỌN SHEET ĐỔ DỮ LIỆU TỰ ĐỘNG THEO CHẾ ĐỘ NĂM / THÁNG
                string tenSheet = laNam ? "BAOCAOTONGHOP_NAM" : "BAO CAO TONG HOP";
                var ws = wb.Worksheet(tenSheet);
                string csdl2 = Module_DanduongGPS.DuongDanCSDL2;
                if (string.IsNullOrWhiteSpace(csdl2) || !File.Exists(csdl2))
                    throw new Exception("Không tìm thấy CSDL csdl2.");
                // ⭐ LẤY KÝ HIỆU ĐƠN VỊ VÀ GHI VÀO Ô A11 (Đã cập nhật dấu phẩy và Font Size 12)
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
                cellA11.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                // ⭐ Cập nhật Font Size thành 12 theo yêu cầu
                cellA11.Style.Font.FontSize = 12;
                cellA11.Style.Font.Bold = false;
                cellA11.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cellA11.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
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
                        string thangGiaiMa = SafeDecrypt(reader["Thang"]).Trim();
                        if (string.IsNullOrWhiteSpace(thangGiaiMa))
                        {
                            thang = khoangTrang;
                        }
                        else if (int.TryParse(thangGiaiMa, out int thangSoLocal))
                        {
                            // Quy định: 1 -> 01, 2 -> 02, 3..12 giữ nguyên
                            thang = (thangSoLocal == 1 || thangSoLocal == 2)
                                ? thangSoLocal.ToString("00")
                                : thangSoLocal.ToString();
                        }
                        else
                        {
                            // Nếu dữ liệu không phải số thì giữ nguyên
                            thang = thangGiaiMa;
                        }
                        nam = SafeDecryptHoacMacDinh(reader["Nam"], khoangTrang);
                        ngay = SafeDecryptHoacMacDinh(reader["Ngay"], khoangTrang);
                        diaDiem = SafeDecryptHoacMacDinh(reader["DiaDiem"], khoangTrang);
                        deNghi = SafeDecryptHoacMacDinh(reader["LoaiDeNghi"], khoangTrang);
                        // 🔥 Ánh xạ mã viết tắt sang tên đầy đủ
                        deNghi = ChuyenDoiLoaiDeNghi(deNghi);
                    }
                }
                catch (Exception ex)
                {
                    Module_ThongBao.Loi("Lỗi khi load thông tin Ngày/Tháng/Năm/Địa điểm/Đề nghị từ CSDL:\n" + ex.Message);
                }
                // Lấy từ Module đã load
                string tenTrungDoanDong1 = string.IsNullOrWhiteSpace(Module_XuatTongHop.TEN_TRUNG_DOAN_DONG_1)
                    ? khoangTrang
                    : Module_XuatTongHop.TEN_TRUNG_DOAN_DONG_1;
                string tenTrungDoanCSCD = string.IsNullOrWhiteSpace(Module_XuatTongHop.TEN_TRUNG_DOAN)
                    ? ""
                    : Module_XuatTongHop.TEN_TRUNG_DOAN;
                string tenTieuDoanCSCD = string.IsNullOrWhiteSpace(Module_XuatTongHop.TEN_TIEU_DOAN)
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
                cellA1.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                cellA1.Style.Font.FontSize = 13;
                cellA1.Style.Font.Bold = false;
                cellA1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cellA1.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                if (!string.IsNullOrWhiteSpace(tenTrungDoanCSCD))
                {
                    // ===== A2 : Trung đoàn CSCD (KHÔNG ĐẬM) =====
                    var cellA2 = ws.Cell("A2");
                    cellA2.Value = tenTrungDoanCSCD;
                    cellA2.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                    cellA2.Style.Font.FontSize = 13;
                    cellA2.Style.Font.Bold = false;
                    cellA2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cellA2.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    // ===== A3 : Tiểu đoàn (IN ĐẬM + GẠCH CHÂN 1/3) =====
                    var cellA3 = ws.Cell("A3");
                    cellA3.Value = tenTieuDoanCSCD;
                    cellA3.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
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
                    cellA2.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
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
                string viTriO = laNam ? "J4" : "H4";
                var cellThoiGian = ws.Cell(viTriO);
                cellThoiGian.Value = $"{diaDiem}, ngày {ngay} tháng {thang} năm {nam}";
                cellThoiGian.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                cellThoiGian.Style.Font.FontSize = 14;
                cellThoiGian.Style.Font.Italic = true;
                cellThoiGian.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cellThoiGian.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                string tuanBaoCao = "";
                string loaiBaoCao = "";
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
                        loaiBaoCao = SafeDecrypt(rd["ChonLoaiBaoCao"]);
                        tuanBaoCao = SafeDecrypt(rd["ChonTuan"]);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[XuatBaoCaoTongHop] Lỗi đọc ChonLoaiBaoCao: " + ex.Message);
                }
                string chuoiThoiGian;
                loaiBaoCao = (loaiBaoCao ?? "").Trim().ToUpperInvariant();
                tuanBaoCao = (tuanBaoCao ?? "").Trim();
                if (laNam)
                {
                    // 👉 CHẾ ĐỘ XÉT NĂM
                    string namHT = Module_XuatPhanLoai.LayNamHeThong();
                    var tieuDe = ws.Range("A6:M6");
                    tieuDe.Merge();
                    tieuDe.Value = $"Phân loại tập thể và tổng hợp năm {namHT}";
                    tieuDe.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    tieuDe.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    tieuDe.Style.Font.Bold = true;
                    tieuDe.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                    tieuDe.Style.Font.FontSize = 14;
                }
                else
                {
                    // 👉 CHẾ ĐỘ XÉT THÁNG (GIỮ NGUYÊN CODE GỐC)
                    string rawThang = Module_XuatPhanLoai.LayThangHeThong();
                    string namHT = Module_XuatPhanLoai.LayNamHeThong();
                    // 🌟 CHỐT CHẶN TRIỆT ĐỂ: Ép kiểu tháng để loại bỏ sạch sẽ số 0 thừa từ tháng 3 đến 12
                    string thangHT = rawThang;
                    if (int.TryParse(rawThang, out int thangSo))
                    {
                        // Tháng 1 và 2 giữ nguyên dạng "01", "02". Từ tháng 3 đến 12 chuyển thành "3", "4"... "12"
                        thangHT = (thangSo == 1 || thangSo == 2) ? thangSo.ToString("00") : thangSo.ToString();
                    }
                    if (loaiBaoCao.Contains("TUẦN"))
                    {
                        // Nếu dữ liệu là "Tuần 1" → lấy số 1
                        if (tuanBaoCao.ToUpperInvariant().Contains("TUẦN"))
                        {
                            tuanBaoCao = tuanBaoCao.ToUpperInvariant().Replace("TUẦN", "").Trim();
                        }
                        if (string.IsNullOrWhiteSpace(tuanBaoCao))
                            tuanBaoCao = "1";
                        // Vẫn giữ nguyên dấu "/" theo đúng quy chuẩn của đơn vị bạn
                        chuoiThoiGian = $"TUẦN {tuanBaoCao} THÁNG {thangHT}/{namHT}";
                    }
                    else
                    {
                        chuoiThoiGian = $"THÁNG {thangHT}/{namHT}";
                    }
                    var tieuDe = ws.Range("A6:M6");
                    tieuDe.Merge();
                    tieuDe.Value = $"DANH SÁCH TỔNG HỢP CÁC LOẠI {chuoiThoiGian}";
                    tieuDe.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    tieuDe.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center; // Kéo chữ nằm ngay giữa ô gộp
                    tieuDe.Style.Font.Bold = true;
                    tieuDe.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                    tieuDe.Style.Font.FontSize = 14;
                }
                // ===== 6. DÒNG A7 (RICH TEXT) =====
                var cellA7 = ws.Cell("A7");
                cellA7.Clear(XLClearOptions.Contents);
                var rt = cellA7.GetRichText();
                rt.AddText("(Kèm theo Báo cáo ")
                  .SetFontName(Module_HeThong.Font_Times_New_Roman)
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
                        kyHieuBaoCao = SafeDecryptHoacMacDinh(cmd.ExecuteScalar(), kyHieuBaoCao);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[XuatBaoCaoTongHop] Lỗi đọc KyHieuBaoCao: " + ex.Message);
                }
                // "TIỂU ĐOÀN 2" -> "Tiểu đoàn 2"
                string tieuDoanThuong = tenTieuDoanCSCD.Trim().ToLowerInvariant();
                string tieuDoanHienThi = tieuDoanThuong.Length == 0
                    ? ""
                    : char.ToUpperInvariant(tieuDoanThuong[0]) + tieuDoanThuong.Substring(1);
                rt.AddText($"số:            {kyHieuBaoCao}, ngày {ngay}/{thang}/{nam}")
                  .SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(14).SetItalic().SetUnderline();
                rt.AddText(" của " + tieuDoanHienThi + ")").SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(14).SetItalic();
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
                        // NULL của CSDL là DBNull (toán tử ?? không bắt được) nên phải kiểm tra IsDBNull
                        int id = rd.IsDBNull(0) ? 0 : Convert.ToInt32(rd.GetValue(0));
                        int ht = rd.IsDBNull(1) ? 0 : Convert.ToInt32(rd.GetValue(1));
                        int cd = rd.IsDBNull(2) ? 0 : Convert.ToInt32(rd.GetValue(2));
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
                if (!laNam)
                {
                    // 👉 CHẾ ĐỘ XÉT THÁNG: GIỮ NGUYÊN CODE GỐC
                    ws.Cell("I11").Value = tyLe.GetValueOrDefault(6).canDat;
                }
                // 👉 CHẾ ĐỘ XÉT NĂM: I11 dành cho mục khen thưởng (ghi ở bước 9b)
                // Gán giá trị đã được chuyển đổi vào ô K11
                var cellK11 = ws.Cell("K11");
                cellK11.Value = deNghi;
                // Định dạng ô K11
                cellK11.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                cellK11.Style.Font.FontSize = 13;
                cellK11.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cellK11.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                // ===== XỬ LÝ Ô L11 - TÓM TẮT GHI CHÚ (cỡ chữ tự co giãn như I11) =====
                string ghiChu = Module_XuatTongHop.TOM_TAT_GHI_CHU?.Trim() ?? string.Empty;
                var cell = ws.Cell("L11");
                if (cell.IsMerged()) cell = cell.MergedRange().FirstCell();
                // Mỗi dòng của ghi chú là 1 đoạn khi ước lượng số dòng cần dùng
                var cacDoanGhiChu = ghiChu.Split('\n').Select(d => d.TrimEnd('\r')).ToList();
                double coChuGhiChu = TinhCoChuVuaO(ws, cell, cacDoanGhiChu);
                cell.Value = ghiChu;
                var style = cell.Style;
                style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                style.Font.FontSize = coChuGhiChu;
                style.Alignment.Vertical = XLAlignmentVerticalValues.Center;       // giữa theo chiều dọc, giống I11
                style.Alignment.Horizontal =
                    ghiChu.Length is > 0 and < 50
                        ? XLAlignmentHorizontalValues.Center                       // câu rất ngắn thì căn giữa ngang
                        : XLAlignmentHorizontalValues.Left;                        // còn lại căn trái, giống I11
                style.Alignment.WrapText = true;
                style.Alignment.ShrinkToFit = false;                               // bỏ ShrinkToFit cũ vì đã có cỡ chữ tự tính
                // E12:H12 (cả hai chế độ)
                ws.Cell("E12").FormulaA1 = "=IFERROR((E11*100)/F11," + LOI_CONG_THUC;
                ws.Cell("F12").FormulaA1 = "=IFERROR(IF(B11=0,0,(F11*100)/B11)," + LOI_CONG_THUC;
                ws.Cell("G12").FormulaA1 = "=IFERROR(100-F12," + LOI_CONG_THUC;
                ws.Cell("H12").FormulaA1 = "=IFERROR(IF(OR(H11=\"\",H11=0),\"0%\",(H11*100)/B11 & \"%\")," + LOI_CONG_THUC;
                // I12 chỉ dùng ở chế độ Tháng (chế độ Năm: I11:I12 là ô gộp chứa mục khen thưởng)
                if (!laNam)
                    ws.Cell("I12").FormulaA1 = "=IFERROR(IF(OR(I11=\"\",I11=0),\"0%\",(I11*100)/B11 & \"%\")," + LOI_CONG_THUC;
                // ===== ĐỊNH DẠNG xx.xx% =====
                foreach (string addr in laNam ? new[] { "E12", "F12", "G12", "H12" } : new[] { "E12", "F12", "G12", "H12", "I12" })
                    ws.Cell(addr).Style.NumberFormat.Format = "00.00\"%\"";
                // ===== 9. KÝ TÊN =====
                VietKyTenBaoCaoTongHop(ws, csdl2);
                // ===== 9b. MỤC II – KHEN THƯỞNG TẬP THỂ / CÁ NHÂN (chỉ chế độ Năm) =====
                if (laNam)
                {
                    try { GhiKhenThuongMucII(ws); }
                    catch (Exception ex) { Debug.WriteLine("[XuatBaoCaoTongHop] Lỗi mục II khen thưởng: " + ex.Message); }
                }
                // ===== 10. LƯU FILE =====
                wb.Save();
                LinkDanTep = fileXuat;   // chỉ gán khi đã lưu thành công
            }
            catch (Exception ex)
            {
                LinkDanTep = null;       // tránh nơi khác mở nhầm tệp lỗi / tệp cũ
                Module_ThongBao.DangXuLy("Lỗi xuất báo cáo tổng hợp: " + ex.Message);
            }
        }
        // ===================== MỤC II – KHEN THƯỞNG (sheet BAOCAOTONGHOP_NAM, chế độ Năm) =====================
        // Nguồn dữ liệu (csdl4, mã hóa AES nên giải mã rồi gom nhóm trong RAM):
        //   Tập thể : ThongKe_KhenThuongTapThe  (HinhThuc_KhenThuong, DonVi_CapKhenThuong, NgayQuyetDinh)
        //   Cá nhân : ThongKe_GiayKhen          (HinhThuc_Khen,       DonVi_Khen,          NgayCapQD_Khen)
        // true  : chỉ tính khen thưởng có năm (trong ngày quyết định) trùng năm hệ thống; dòng không đọc được năm vẫn được tính
        // false : tính toàn bộ dữ liệu đang có trong bảng
        private const bool LOC_KHEN_THUONG_THEO_NAM_HE_THONG = true;
        private const string CHUOI_KHONG_CO_KHEN_THUONG = "Không có";
        // Thông báo lỗi chung cho các công thức E12:I12 (đã gồm dấu đóng ngoặc của IFERROR)
        private const string LOI_CONG_THUC = "\"Dữ liệu từ tệp csdl2 có thể không đúng hoặc CSDL này không tồn tại, khôi phục lại bản xuất xưởng\")";
        private sealed class NhomKhen
        {
            public string HinhThuc = "";
            public string DonVi = "";
            public int SoLuong;
            public int Hang;      // 0 = giấy khen, 1 = bằng khen, 2 = loại khác
            public int ThuTu;     // thứ tự xuất hiện đầu tiên trong CSDL
        }
        /// <summary>Đọc (đã giải mã) các dòng khen thưởng từ 1 bảng của csdl4. Bảng/cột thiếu -> danh sách rỗng.</summary>
        private static List<(string HinhThuc, string DonVi, string Ngay)> DocDongKhenThuong(
            string csdl4, string bang, string cotHinhThuc, string cotDonVi, string cotNgay)
        {
            var ds = new List<(string HinhThuc, string DonVi, string Ngay)>();
            if (string.IsNullOrWhiteSpace(csdl4) || !File.Exists(csdl4)) return ds;
            try
            {
                using var cn = new SqliteConnection($"Data Source={csdl4};Mode=ReadOnly");
                cn.Open();
                using (var chk = new SqliteCommand(
                    "SELECT 1 FROM sqlite_master WHERE type='table' AND name=@t LIMIT 1;", cn))
                {
                    chk.Parameters.AddWithValue("@t", bang);
                    if (chk.ExecuteScalar() == null) return ds;
                }
                // Tên bảng/cột là hằng do code này truyền vào (không lấy từ người dùng); rowid giữ đúng thứ tự nhập
                using var cmd = new SqliteCommand(
                    $"SELECT {cotHinhThuc}, {cotDonVi}, {cotNgay} FROM {bang} ORDER BY rowid ASC;", cn);
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    string ht = SafeDecrypt(rd.GetValue(0)).Trim();
                    string dv = SafeDecrypt(rd.GetValue(1)).Trim();
                    string ngay = SafeDecrypt(rd.GetValue(2)).Trim();
                    if (ht.Length == 0 && dv.Length == 0) continue;   // dòng rỗng
                    ds.Add((ht, dv, ngay));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TongHopKhenThuong] Lỗi đọc bảng {bang}: {ex.Message}");
            }
            return ds;
        }
        /// <summary>Lấy năm từ chuỗi ngày tự do: "15/03/2026", "2026-03-15", "15/3/26"... Không đọc được -> null.</summary>
        private static int? DocNamTuNgay(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var m4 = Regex.Matches(s, @"(?<!\d)(\d{4})(?!\d)");
            for (int i = m4.Count - 1; i >= 0; i--)
                if (int.TryParse(m4[i].Value, out int n) && n >= 1900 && n <= 2100) return n;
            var m2 = Regex.Match(s, @"^\D*\d{1,2}\s*[/\-.]\s*\d{1,2}\s*[/\-.]\s*(\d{2})\s*$");   // chỉ nhận dd/MM/yy đầy đủ
            if (m2.Success && int.TryParse(m2.Groups[1].Value, out int yy)) return 2000 + yy;
            return null;
        }
        private static string ChuanHoaKhoangTrang(string s) =>
            string.Join(" ", (s ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        // Khoảng cỡ chữ cho phép (pt) và bước nhảy
        private const double CO_CHU_TOI_DA = 16;
        private const double CO_CHU_TOI_THIEU = 8;
        private const double BUOC_CO_CHU = 0.5;
        /// <summary>Gom nhóm -> danh sách cụm (Số "01", Chữ "giấy khen của Công an tỉnh Thanh Hóa").</summary>
        private static List<(string So, string Chu)> TongHopCacCum(
            List<(string HinhThuc, string DonVi, string Ngay)> ds, int? namLoc)
        {
            var chiMuc = new Dictionary<string, NhomKhen>(StringComparer.OrdinalIgnoreCase);
            var danhSach = new List<NhomKhen>();
            foreach (var (hinhThuc, donVi, ngay) in ds)
            {
                if (namLoc.HasValue)
                {
                    int? nam = DocNamTuNgay(ngay);
                    if (nam.HasValue && nam.Value != namLoc.Value) continue;
                }
                string ht = ChuanHoaKhoangTrang(hinhThuc);
                if (ht.Length == 0) ht = "khen thưởng";
                string dv = ChuanHoaKhoangTrang(donVi);
                if (dv.StartsWith("của ", StringComparison.OrdinalIgnoreCase)) dv = dv.Substring(4).TrimStart();
                // Ô "đơn vị khen" lỡ nhập ngày tháng (vd "03/8/2026") -> coi như không có đơn vị, không in "của 03/8/2026"
                if (Regex.IsMatch(dv, @"^\d{1,2}\s*[/\-.]\s*\d{1,2}\s*[/\-.]\s*\d{2,4}$")) dv = "";
                string khoa = ht + "|" + dv;
                if (!chiMuc.TryGetValue(khoa, out var nhom))
                {
                    string h = ht.ToLowerInvariant();
                    nhom = new NhomKhen
                    {
                        HinhThuc = char.ToLowerInvariant(ht[0]) + ht.Substring(1),
                        DonVi = dv,
                        Hang = h.Contains("giấy khen") ? 0 : h.Contains("bằng khen") ? 1 : 2,
                        ThuTu = danhSach.Count
                    };
                    chiMuc[khoa] = nhom;
                    danhSach.Add(nhom);
                }
                nhom.SoLuong++;
            }
            return danhSach
                .OrderBy(x => x.Hang).ThenBy(x => x.ThuTu)
                .Select(x => ($"{x.SoLuong:00}",
                              x.DonVi.Length > 0 ? $"{x.HinhThuc} của {x.DonVi}" : x.HinhThuc))
                .ToList();
        }
        /// <summary>Dựng đoạn chữ thuần (để đo độ dài khi tính cỡ chữ).</summary>
        private static string DungDoanChuThuan(string nhan, List<(string So, string Chu)> cum)
        {
            if (cum.Count == 0) return nhan + " " + CHUOI_KHONG_CO_KHEN_THUONG + ".";
            return nhan + " " + string.Join("; ", cum.Select(c => c.So + " " + c.Chu)) + ".";
        }
        /// <summary>
        /// Chọn cỡ chữ lớn nhất (CO_CHU_TOI_DA -> CO_CHU_TOI_THIEU) mà toàn bộ các đoạn vẫn nằm gọn trong ô (kể cả ô gộp).
        /// Ước lượng theo bề rộng cột + tổng chiều cao dòng của vùng gộp; có biên an toàn để chữ không bị cắt.
        /// </summary>
        private static double TinhCoChuVuaO(IXLWorksheet ws, IXLCell o, List<string> cacDoan)
        {
            var vung = o.IsMerged() ? o.MergedRange() : o.AsRange();
            var dc = vung.RangeAddress;
            // Bề rộng khả dụng (pixel). 1 đơn vị độ rộng cột ~ 7 px, trừ lề trong ô
            double rongPx = 0;
            for (int c = dc.FirstAddress.ColumnNumber; c <= dc.LastAddress.ColumnNumber; c++)
                rongPx += ws.Column(c).Width * 7.0 + 5.0;
            rongPx -= 8;
            // Chiều cao khả dụng (pt)
            double caoPt = 0;
            for (int r = dc.FirstAddress.RowNumber; r <= dc.LastAddress.RowNumber; r++)
                caoPt += ws.Row(r).Height;
            caoPt -= 6;
            if (rongPx <= 20 || caoPt <= 10) return CO_CHU_TOI_THIEU;
            for (double co = CO_CHU_TOI_DA; co >= CO_CHU_TOI_THIEU; co -= BUOC_CO_CHU)
            {
                // Times New Roman: bề rộng trung bình ~0,5 em (tính dư vì có chữ đậm/viết hoa) ; 1pt = 96/72 px
                double rongKyTuPx = 0.5 * co * 96.0 / 72.0;
                double kyTuMoiDong = Math.Max(1, rongPx / rongKyTuPx / 1.08);   // 1,08: hao hụt do ngắt dòng theo từ
                int tongDong = 0;
                foreach (var doan in cacDoan)
                    tongDong += Math.Max(1, (int)Math.Ceiling(doan.Length / kyTuMoiDong));
                double cao = tongDong * co * 1.25;   // 1,25: giãn dòng của Times New Roman
                if (cao <= caoPt) return co;
            }
            return CO_CHU_TOI_THIEU;
        }
        /// <summary>Ghi 1 đoạn: nhãn in đậm + từng cụm (số in đậm, phần còn lại thường).</summary>
        private static void GhiDoanKhenThuong(IXLRichText rt, string nhan, List<(string So, string Chu)> cum, double co)
        {
            rt.AddText(nhan).SetBold().SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(co);
            rt.AddText(" ").SetBold(false).SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(co);
            if (cum.Count == 0)
            {
                rt.AddText(CHUOI_KHONG_CO_KHEN_THUONG + ".").SetBold(false)
                  .SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(co);
                return;
            }
            for (int i = 0; i < cum.Count; i++)
            {
                rt.AddText(cum[i].So).SetBold()
                  .SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(co);
                rt.AddText(" " + cum[i].Chu + (i < cum.Count - 1 ? "; " : ".")).SetBold(false)
                  .SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(co);
            }
        }
        /// <summary>Ghi mục II (ô I11): in đậm nhãn + số, cỡ chữ tự co giãn theo lượng dữ liệu.</summary>
        /// <summary>Ghi mục II (ô I11): in đậm nhãn + số, cỡ chữ tự co giãn theo lượng dữ liệu.</summary>
        private static void GhiKhenThuongMucII(IXLWorksheet ws)
        {
            string csdl4 = Module_DanduongGPS.DuongDanCSDL4;
            int? namLoc = null;
            if (LOC_KHEN_THUONG_THEO_NAM_HE_THONG &&
                int.TryParse(Module_XuatPhanLoai.LayNamHeThong()?.Trim(), out int namHT) && namHT > 0)
                namLoc = namHT;
            var cumTapThe = TongHopCacCum(DocDongKhenThuong(csdl4, "ThongKe_KhenThuongTapThe",
                "HinhThuc_KhenThuong", "DonVi_CapKhenThuong", "NgayQuyetDinh"), namLoc);
            var cumCaNhan = TongHopCacCum(DocDongKhenThuong(csdl4, "ThongKe_GiayKhen",
                "HinhThuc_Khen", "DonVi_Khen", "NgayCapQD_Khen"), namLoc);
            var o = ws.Cell(O_MUC_II_CO_DINH);
            if (o.IsMerged()) o = o.MergedRange().FirstCell();
            // Giữ tiêu đề cũ (nếu ô có), bỏ phần nội dung đã ghi lần trước
            string cu = o.GetString();
            int viTri = cu.IndexOf("* Về tập thể", StringComparison.OrdinalIgnoreCase);
            string tieuDe = (viTri >= 0 ? cu.Substring(0, viTri) : cu).Trim('\r', '\n', ' ');
            // Tính cỡ chữ từ nội dung thuần
            var cacDoan = new List<string>();
            if (tieuDe.Length > 0) cacDoan.Add(tieuDe);
            cacDoan.Add(DungDoanChuThuan("* Về tập thể:", cumTapThe));
            cacDoan.Add(DungDoanChuThuan("* Về cá nhân:", cumCaNhan));
            double co = TinhCoChuVuaO(ws, o, cacDoan);
            // Ghi rich text
            o.Clear(XLClearOptions.Contents);
            var rt = o.GetRichText();
            rt.ClearText();
            if (tieuDe.Length > 0)
                rt.AddText(tieuDe + "\n").SetBold().SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(co);
            GhiDoanKhenThuong(rt, "* Về tập thể:", cumTapThe, co);
            rt.AddText("\n").SetBold(false).SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(co);
            GhiDoanKhenThuong(rt, "* Về cá nhân:", cumCaNhan, co);
            o.Style.Alignment.WrapText = true;
            o.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            o.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
    }
}