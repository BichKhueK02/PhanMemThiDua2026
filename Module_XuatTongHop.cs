/////Phiên bản 2
/////
//    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
//using ClosedXML.Excel;
//using Microsoft.Data.Sqlite;
//using System.Diagnostics;
//using System.Text.RegularExpressions;
//namespace PhanMemThiDua2026
//{
//    internal static class Module_XuatTongHop
//    {
//        public static string? LinkDanTep;
//        public static string TEN_TRUNG_DOAN = "";
//        public static string TEN_TIEU_DOAN = "";
//        public static string TEN_TRUNG_DOAN_DONG_1 = "";
//        public static string TOM_TAT_GHI_CHU = "";
//        private const string O_MUC_II_CO_DINH = "I11";
//        public static void NapThongTinDonVi()
//        {
//            LayThongTinDonVi(
//                out TEN_TRUNG_DOAN_DONG_1,
//                out TEN_TRUNG_DOAN,
//                out TEN_TIEU_DOAN,
//                out TOM_TAT_GHI_CHU
//            );
//        }
//        private static string SafeDecrypt(object value)
//        {
//            try
//            {
//                if (value == null || value == DBNull.Value) return "";
//                string s = value.ToString();
//                if (string.IsNullOrWhiteSpace(s)) return "";
//                return Module_BaoMatAES.GiaiMa(s);
//            }
//            catch
//            {
//                return "";
//            }
//        }
//        /// <summary>Giải mã an toàn; nếu kết quả rỗng thì trả về giá trị mặc định (vd: khoảng trắng mẫu).</summary>
//        private static string SafeDecryptHoacMacDinh(object? value, string macDinh)
//        {
//            string s = SafeDecrypt(value ?? DBNull.Value);
//            return string.IsNullOrWhiteSpace(s) ? macDinh : s;
//        }
//        //Chuyển từ Chữ IN HOA "TIỂU ĐOÀN 2"sang chữ thường "Tiểu đoàn 2"
//        public static void LayThongTinDonVi(out string tenTrungDoanDong1, out string tenTrungDoan, out string tenTieuDoan, out string tomTatGhiChu)
//        {
//            tenTrungDoanDong1 = "";
//            tenTrungDoan = "";
//            tenTieuDoan = "";
//            tomTatGhiChu = "";
//            string dbPath = Module_DanduongGPS.DuongDanCSDL2;
//            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
//                return;
//            try
//            {
//                using var cn = new SqliteConnection($"Data Source={dbPath}");
//                cn.Open();
//                using var cmd = cn.CreateCommand();
//                cmd.CommandText = @"
//            SELECT
//                textBox1_TenTrungDoanDong1,
//                TenTrungDoan,
//                TenTieuDoan,
//                TomTatGhiChu
//            FROM ThongTin
//            ORDER BY ID ASC
//            LIMIT 1";
//                using var rd = cmd.ExecuteReader();
//                if (!rd.Read()) return;
//                tenTrungDoanDong1 = SafeDecrypt(rd["textBox1_TenTrungDoanDong1"]);
//                tenTrungDoan = SafeDecrypt(rd["TenTrungDoan"]);
//                tenTieuDoan = SafeDecrypt(rd["TenTieuDoan"]);
//                tomTatGhiChu = SafeDecrypt(rd["TomTatGhiChu"]);
//            }
//            catch (Exception ex)
//            {
//                // không phá luồng xuất báo cáo, chỉ ghi vết để dò lỗi
//                Debug.WriteLine("[LayThongTinDonVi] " + ex.Message);
//            }
//        }
//        private static string ChuyenDoiLoaiDeNghi(string input)
//        {
//            if (string.IsNullOrWhiteSpace(input)) return input;
//            // Chuẩn hóa chuỗi (xóa khoảng trắng thừa, chuyển về chữ hoa để so sánh)
//            string normalized = input.Trim().ToUpperInvariant();
//            return normalized switch
//            {
//                "ĐVQT" => "Đơn vị Quyết thắng",
//                "ĐVTT" => "Đơn vị Tiên tiến",
//                "HTNV" or "HTNT" or "HTVT" => "Hoàn thành nhiệm vụ",   // HTNV là mã chuẩn; giữ HTNT/HTVT để tương thích dữ liệu cũ
//                "KHTNV" => "Không hoàn thành nhiệm vụ",
//                _ => input // Giữ nguyên giá trị gốc nếu không khớp các mã trên
//            };
//        }
//        private static void VietKyTenBaoCaoTongHop(IXLWorksheet ws, string fileDB)
//        {
//            string hoTenKy = "     "; // Khoảng trắng mặc định
//            try
//            {
//                using var conn = new SqliteConnection($"Data Source={fileDB}");
//                conn.Open();
//                using var cmd = new SqliteCommand("SELECT ChiHuyD FROM ThongTin WHERE ID = 1", conn);
//                using var reader = cmd.ExecuteReader();
//                if (reader.Read())
//                {
//                    // Cắt Trim() ngay từ đầu phòng hờ khoảng trắng
//                    string chiHuyRaw = reader["ChiHuyD"]?.ToString()?.Trim() ?? "";
//                    if (!string.IsNullOrWhiteSpace(chiHuyRaw))
//                    {
//                        string decoded = Module_BaoMatAES.GiaiMa(chiHuyRaw);
//                        // Giải mã thất bại: dữ liệu cũ dạng chữ thường thì dùng tiếp, còn chuỗi mã hóa "v2|..." thì bỏ trống (không in chuỗi mã lên báo cáo)
//                        string ketQua = string.IsNullOrEmpty(decoded)
//                            ? (chiHuyRaw.StartsWith("v2|", StringComparison.Ordinal) ? "" : chiHuyRaw)
//                            : decoded.Trim();
//                        if (!string.IsNullOrWhiteSpace(ketQua)) hoTenKy = ketQua;
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Module_ThongBao.Loi("Lỗi khi load thông tin Chỉ huy từ CSDL:\n" + ex.Message);
//            }
//            string chucVuTieuDoanTruong = "";
//            string chucVuNguoiKy = "";
//            int idDauTien = -1;
//            int idNguoiKy = -1;
//            try
//            {
//                using (var cn = new SqliteConnection("Data Source=" + fileDB))
//                {
//                    cn.Open();
//                    using (var cmd = new SqliteCommand("SELECT ID, HoVaTen, ChucVu FROM ChiHuyD ORDER BY ID ASC", cn))
//                    using (var rd = cmd.ExecuteReader())
//                    {
//                        bool isFirst = true;
//                        bool foundMatch = false;
//                        while (rd.Read())
//                        {
//                            int id = Convert.ToInt32(rd["ID"]);
//                            // Bắt buộc Trim() ngay lúc lấy ra để tránh lỗi StartsWith("v2|")
//                            string hoTenRaw = rd["HoVaTen"]?.ToString()?.Trim() ?? "";
//                            string chucVuRaw = rd["ChucVu"]?.ToString()?.Trim() ?? "";
//                            // Giải mã an toàn (Hàm GiaiMa đã bọc try-catch)
//                            string htDec = Module_BaoMatAES.GiaiMa(hoTenRaw);
//                            htDec = (string.IsNullOrEmpty(htDec) ? hoTenRaw : htDec).Trim();
//                            string cvDec = Module_BaoMatAES.GiaiMa(chucVuRaw);
//                            if (string.IsNullOrEmpty(cvDec)) cvDec = chucVuRaw;
//                            // Lưu người đầu tiên làm Tiểu đoàn trưởng (dự phòng)
//                            if (isFirst)
//                            {
//                                idDauTien = id;
//                                chucVuTieuDoanTruong = cvDec;
//                                isFirst = false;
//                            }
//                            // So khớp người ký
//                            if (htDec.Equals(hoTenKy, StringComparison.OrdinalIgnoreCase))
//                            {
//                                idNguoiKy = id;
//                                chucVuNguoiKy = cvDec;
//                                foundMatch = true;
//                                break;
//                            }
//                        }
//                        // Nếu không khớp ai thì dùng mặc định người đầu tiên
//                        if (!foundMatch)
//                        {
//                            idNguoiKy = idDauTien;
//                            chucVuNguoiKy = chucVuTieuDoanTruong;
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                // Lỗi đọc bảng ChiHuyD: giữ nguyên phần ký tên của bản mẫu, không làm hỏng cả báo cáo
//                Debug.WriteLine("[VietKyTenBaoCaoTongHop] " + ex.Message);
//                return;
//            }
//            // 🔥 SỬA CHỖ NÀY NÈ: Xử lý logic người ký và ký thay (KT.)
//            if (idNguoiKy == idDauTien)
//            {
//                // Trưởng đơn vị trực tiếp ký
//                ws.Cell("L14").Value = chucVuTieuDoanTruong.ToUpperInvariant();
//                ws.Cell("L15").Value = ""; // Dọn sạch ô L15 phòng hờ template có chữ rác
//            }
//            else
//            {
//                // Cấp phó ký thay
//                ws.Cell("L14").Value = ("KT. " + chucVuTieuDoanTruong).ToUpperInvariant();
//                ws.Cell("L15").Value = chucVuNguoiKy.ToUpperInvariant();
//            }
//            ws.Cell("L19").Value = hoTenKy;
//            foreach (string addr in new[] { "L14", "L15", "L19" })
//            {
//                var c = ws.Cell(addr);
//                c.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
//                c.Style.Font.FontSize = 14;
//                c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                c.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//                if (addr != "L19") c.Style.Font.Bold = true;
//            }
//        }
//        public static void XuatBaoCaoTongHop(string fileXuat)
//        {
//            try
//            {
//                NapThongTinDonVi(); // 🔥 BẮT BUỘC PHẢI CÓ
//                if (string.IsNullOrWhiteSpace(fileXuat) || !File.Exists(fileXuat))
//                    throw new Exception("File Excel tổng hợp không tồn tại.");
//                using var wb = new XLWorkbook(fileXuat);
//                // Chốt chế độ Năm/Tháng MỘT LẦN cho cả lần xuất (tránh lệch sheet/nội dung nếu chế độ đổi giữa chừng)
//                bool laNam = Module_HeThong.IsCheDoXetThiDuaNam();
//                // 👉 CHỌN SHEET ĐỔ DỮ LIỆU TỰ ĐỘNG THEO CHẾ ĐỘ NĂM / THÁNG
//                string tenSheet = laNam ? "BAOCAOTONGHOP_NAM" : "BAO CAO TONG HOP";
//                var ws = wb.Worksheet(tenSheet);
//                string csdl2 = Module_DanduongGPS.DuongDanCSDL2;
//                if (string.IsNullOrWhiteSpace(csdl2) || !File.Exists(csdl2))
//                    throw new Exception("Không tìm thấy CSDL csdl2.");
//                // ⭐ LẤY KÝ HIỆU ĐƠN VỊ VÀ GHI VÀO Ô A11 (Đã cập nhật dấu phẩy và Font Size 12)
//                string kyHieuTieuDoan = "";
//                string kyHieuTrungDoan = "";
//                try
//                {
//                    using var cnKyHieu = new SqliteConnection($"Data Source={csdl2}");
//                    cnKyHieu.Open();
//                    // Lọc chuẩn xác chỉ lấy ID = 1 theo yêu cầu
//                    using var cmdKyHieu = new SqliteCommand("SELECT KyHieu_TrungDoan, KyHieu_TieuDoan FROM KyHieu_DonVi WHERE ID = 1", cnKyHieu);
//                    using var rdKyHieu = cmdKyHieu.ExecuteReader();
//                    if (rdKyHieu.Read())
//                    {
//                        // Lấy dữ liệu và giải mã an toàn qua module V2
//                        kyHieuTrungDoan = SafeDecrypt(rdKyHieu["KyHieu_TrungDoan"]);
//                        kyHieuTieuDoan = SafeDecrypt(rdKyHieu["KyHieu_TieuDoan"]);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"[XuatBaoCaoTongHop] Lỗi lấy Ký hiệu đơn vị: {ex.Message}");
//                }
//                // Ghép chuỗi: Phân cách bằng dấu phẩy theo yêu cầu. 
//                // Xử lý thông minh: Nếu 1 trong 2 chuỗi trống thì không in dấu phẩy thừa.
//                kyHieuTieuDoan = kyHieuTieuDoan.Trim();
//                kyHieuTrungDoan = kyHieuTrungDoan.Trim();
//                string chuoiKyHieuGhep = "";
//                if (!string.IsNullOrEmpty(kyHieuTieuDoan) && !string.IsNullOrEmpty(kyHieuTrungDoan))
//                {
//                    chuoiKyHieuGhep = $"{kyHieuTieuDoan}, {kyHieuTrungDoan}";
//                }
//                else if (!string.IsNullOrEmpty(kyHieuTieuDoan))
//                {
//                    chuoiKyHieuGhep = kyHieuTieuDoan;
//                }
//                else if (!string.IsNullOrEmpty(kyHieuTrungDoan))
//                {
//                    chuoiKyHieuGhep = kyHieuTrungDoan;
//                }
//                // Gán vào ô A11 và định dạng sơ bộ
//                var cellA11 = ws.Cell("A11");
//                cellA11.Value = chuoiKyHieuGhep;
//                cellA11.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
//                // ⭐ Cập nhật Font Size thành 12 theo yêu cầu
//                cellA11.Style.Font.FontSize = 12;
//                cellA11.Style.Font.Bold = false;
//                cellA11.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                cellA11.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//                // ===== 4. BIẾN THỜI GIAN & ĐỊA ĐIỂM =====
//                string khoangTrang = "     "; // Khoảng trắng nếu không có giá trị
//                string thang = khoangTrang;
//                string nam = khoangTrang;
//                string ngay = khoangTrang;
//                string diaDiem = khoangTrang;
//                string deNghi = khoangTrang; // Loại đề nghị
//                try
//                {
//                    string csdlPath = Module_DanduongGPS.DuongDanCSDL2;
//                    using var conn = new SqliteConnection($"Data Source={csdlPath}");
//                    conn.Open();
//                    // Lấy luôn cột LoaiDeNghi
//                    using var cmd = new SqliteCommand("SELECT Thang, Nam, Ngay, DiaDiem, LoaiDeNghi FROM ThongTin WHERE ID = 1", conn);
//                    using var reader = cmd.ExecuteReader();
//                    if (reader.Read())
//                    {
//                        string thangGiaiMa = SafeDecrypt(reader["Thang"]).Trim();
//                        if (string.IsNullOrWhiteSpace(thangGiaiMa))
//                        {
//                            thang = khoangTrang;
//                        }
//                        else if (int.TryParse(thangGiaiMa, out int thangSoLocal))
//                        {
//                            // Quy định: 1 -> 01, 2 -> 02, 3..12 giữ nguyên
//                            thang = (thangSoLocal == 1 || thangSoLocal == 2)
//                                ? thangSoLocal.ToString("00")
//                                : thangSoLocal.ToString();
//                        }
//                        else
//                        {
//                            // Nếu dữ liệu không phải số thì giữ nguyên
//                            thang = thangGiaiMa;
//                        }
//                        nam = SafeDecryptHoacMacDinh(reader["Nam"], khoangTrang);
//                        ngay = SafeDecryptHoacMacDinh(reader["Ngay"], khoangTrang);
//                        diaDiem = SafeDecryptHoacMacDinh(reader["DiaDiem"], khoangTrang);
//                        deNghi = SafeDecryptHoacMacDinh(reader["LoaiDeNghi"], khoangTrang);
//                        // 🔥 Ánh xạ mã viết tắt sang tên đầy đủ
//                        deNghi = ChuyenDoiLoaiDeNghi(deNghi);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Module_ThongBao.Loi("Lỗi khi load thông tin Ngày/Tháng/Năm/Địa điểm/Đề nghị từ CSDL:\n" + ex.Message);
//                }
//                // Lấy từ Module đã load
//                string tenTrungDoanDong1 = string.IsNullOrWhiteSpace(Module_XuatTongHop.TEN_TRUNG_DOAN_DONG_1)
//                    ? khoangTrang
//                    : Module_XuatTongHop.TEN_TRUNG_DOAN_DONG_1;
//                string tenTrungDoanCSCD = string.IsNullOrWhiteSpace(Module_XuatTongHop.TEN_TRUNG_DOAN)
//                    ? ""
//                    : Module_XuatTongHop.TEN_TRUNG_DOAN;
//                string tenTieuDoanCSCD = string.IsNullOrWhiteSpace(Module_XuatTongHop.TEN_TIEU_DOAN)
//                    ? khoangTrang
//                    : Module_XuatTongHop.TEN_TIEU_DOAN;
//                void GanGachChan1Phan3(IXLCell cell, string text)
//                {
//                    if (cell == null) return;
//                    if (string.IsNullOrWhiteSpace(text)) return;
//                    int totalLen = text.Length;
//                    int underlineLen = (int)Math.Round(totalLen / 3.0);
//                    if (underlineLen <= 0) return;
//                    int start = (totalLen - underlineLen) / 2;
//                    var rich = cell.GetRichText();
//                    rich.ClearText(); // ❗ KHÔNG xóa cell, chỉ reset richtext
//                    if (start > 0)
//                        rich.AddText(text.Substring(0, start));
//                    rich.AddText(text.Substring(start, underlineLen))
//                        .SetUnderline();
//                    int end = start + underlineLen;
//                    if (end < totalLen)
//                        rich.AddText(text.Substring(end));
//                }
//                // ===== A1 : Trung đoàn dòng 1 (LUÔN GHI – KHÔNG ĐẬM) =====
//                var cellA1 = ws.Cell("A1");
//                cellA1.Value = string.IsNullOrWhiteSpace(tenTrungDoanDong1)
//                    ? khoangTrang
//                    : tenTrungDoanDong1;
//                cellA1.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
//                cellA1.Style.Font.FontSize = 13;
//                cellA1.Style.Font.Bold = false;
//                cellA1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                cellA1.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//                if (!string.IsNullOrWhiteSpace(tenTrungDoanCSCD))
//                {
//                    // ===== A2 : Trung đoàn CSCD (KHÔNG ĐẬM) =====
//                    var cellA2 = ws.Cell("A2");
//                    cellA2.Value = tenTrungDoanCSCD;
//                    cellA2.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
//                    cellA2.Style.Font.FontSize = 13;
//                    cellA2.Style.Font.Bold = false;
//                    cellA2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                    cellA2.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//                    // ===== A3 : Tiểu đoàn (IN ĐẬM + GẠCH CHÂN 1/3) =====
//                    var cellA3 = ws.Cell("A3");
//                    cellA3.Value = tenTieuDoanCSCD;
//                    cellA3.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
//                    cellA3.Style.Font.FontSize = 13;
//                    cellA3.Style.Font.Bold = true;
//                    cellA3.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                    cellA3.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//                    // 🔥 Gạch chân dòng CUỐI
//                    GanGachChan1Phan3(cellA3, tenTieuDoanCSCD);
//                }
//                else
//                {
//                    // ===== KHÔNG CÓ A2 → A2 LÀ DÒNG CUỐI =====
//                    var cellA2 = ws.Cell("A2");
//                    cellA2.Value = tenTieuDoanCSCD;
//                    cellA2.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
//                    cellA2.Style.Font.FontSize = 13;
//                    cellA2.Style.Font.Bold = true;
//                    cellA2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                    cellA2.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//                    // 🔥 Gạch chân dòng CUỐI
//                    GanGachChan1Phan3(cellA2, tenTieuDoanCSCD);
//                    // ===== A3 : ĐỂ TRỐNG =====
//                    var cellA3 = ws.Cell("A3");
//                    cellA3.Value = "";
//                    cellA3.Style.Font.Bold = false;
//                }
//                string viTriO = laNam ? "J4" : "H4";
//                var cellThoiGian = ws.Cell(viTriO);
//                cellThoiGian.Value = $"{diaDiem}, ngày {ngay} tháng {thang} năm {nam}";
//                cellThoiGian.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
//                cellThoiGian.Style.Font.FontSize = 14;
//                cellThoiGian.Style.Font.Italic = true;
//                cellThoiGian.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                cellThoiGian.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//                string tuanBaoCao = "";
//                string loaiBaoCao = "";
//                try
//                {
//                    using var conn = new SqliteConnection($"Data Source={csdl2}");
//                    conn.Open();
//                    using var cmd = new SqliteCommand(
//                        "SELECT ChonLoaiBaoCao, ChonTuan FROM ChonLoaiBaoCao ORDER BY ID DESC LIMIT 1",
//                        conn);
//                    using var rd = cmd.ExecuteReader();
//                    if (rd.Read())
//                    {
//                        loaiBaoCao = SafeDecrypt(rd["ChonLoaiBaoCao"]);
//                        tuanBaoCao = SafeDecrypt(rd["ChonTuan"]);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine("[XuatBaoCaoTongHop] Lỗi đọc ChonLoaiBaoCao: " + ex.Message);
//                }
//                string chuoiThoiGian;
//                loaiBaoCao = (loaiBaoCao ?? "").Trim().ToUpperInvariant();
//                tuanBaoCao = (tuanBaoCao ?? "").Trim();
//                if (laNam)
//                {
//                    // 👉 CHẾ ĐỘ XÉT NĂM
//                    string namHT = Module_XuatPhanLoai.LayNamHeThong();
//                    var tieuDe = ws.Range("A6:M6");
//                    tieuDe.Merge();
//                    tieuDe.Value = $"Phân loại tập thể và tổng hợp năm {namHT}";
//                    tieuDe.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                    tieuDe.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//                    tieuDe.Style.Font.Bold = true;
//                    tieuDe.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
//                    tieuDe.Style.Font.FontSize = 14;
//                }
//                else
//                {
//                    // 👉 CHẾ ĐỘ XÉT THÁNG (GIỮ NGUYÊN CODE GỐC)
//                    string rawThang = Module_XuatPhanLoai.LayThangHeThong();
//                    string namHT = Module_XuatPhanLoai.LayNamHeThong();
//                    // 🌟 CHỐT CHẶN TRIỆT ĐỂ: Ép kiểu tháng để loại bỏ sạch sẽ số 0 thừa từ tháng 3 đến 12
//                    string thangHT = rawThang;
//                    if (int.TryParse(rawThang, out int thangSo))
//                    {
//                        // Tháng 1 và 2 giữ nguyên dạng "01", "02". Từ tháng 3 đến 12 chuyển thành "3", "4"... "12"
//                        thangHT = (thangSo == 1 || thangSo == 2) ? thangSo.ToString("00") : thangSo.ToString();
//                    }
//                    if (loaiBaoCao.Contains("TUẦN"))
//                    {
//                        // Nếu dữ liệu là "Tuần 1" → lấy số 1
//                        if (tuanBaoCao.ToUpperInvariant().Contains("TUẦN"))
//                        {
//                            tuanBaoCao = tuanBaoCao.ToUpperInvariant().Replace("TUẦN", "").Trim();
//                        }
//                        if (string.IsNullOrWhiteSpace(tuanBaoCao))
//                            tuanBaoCao = "1";
//                        // Vẫn giữ nguyên dấu "/" theo đúng quy chuẩn của đơn vị bạn
//                        chuoiThoiGian = $"TUẦN {tuanBaoCao} THÁNG {thangHT}/{namHT}";
//                    }
//                    else
//                    {
//                        chuoiThoiGian = $"THÁNG {thangHT}/{namHT}";
//                    }
//                    var tieuDe = ws.Range("A6:M6");
//                    tieuDe.Merge();
//                    tieuDe.Value = $"DANH SÁCH TỔNG HỢP CÁC LOẠI {chuoiThoiGian}";
//                    tieuDe.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                    tieuDe.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center; // Kéo chữ nằm ngay giữa ô gộp
//                    tieuDe.Style.Font.Bold = true;
//                    tieuDe.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
//                    tieuDe.Style.Font.FontSize = 14;
//                }
//                // ===== 6. DÒNG A7 (RICH TEXT) =====
//                var cellA7 = ws.Cell("A7");
//                cellA7.Clear(XLClearOptions.Contents);
//                var rt = cellA7.GetRichText();
//                rt.AddText("(Kèm theo Báo cáo ")
//                  .SetFontName(Module_HeThong.Font_Times_New_Roman)
//                  .SetFontSize(14)
//                  .SetItalic();
//                string kyHieuBaoCao = "...............";
//                try
//                {
//                    if (File.Exists(csdl2))
//                    {
//                        using var cn = new SqliteConnection($"Data Source={csdl2}");
//                        cn.Open();
//                        using var cmd = cn.CreateCommand();
//                        cmd.CommandText = "SELECT KyHieuBaoCao FROM ThongTin WHERE ID=1";
//                        kyHieuBaoCao = SafeDecryptHoacMacDinh(cmd.ExecuteScalar(), kyHieuBaoCao);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine("[XuatBaoCaoTongHop] Lỗi đọc KyHieuBaoCao: " + ex.Message);
//                }
//                // "TIỂU ĐOÀN 2" -> "Tiểu đoàn 2"
//                string tieuDoanThuong = tenTieuDoanCSCD.Trim().ToLowerInvariant();
//                string tieuDoanHienThi = tieuDoanThuong.Length == 0
//                    ? ""
//                    : char.ToUpperInvariant(tieuDoanThuong[0]) + tieuDoanThuong.Substring(1);
//                rt.AddText($"số:            {kyHieuBaoCao}, ngày {ngay}/{thang}/{nam}")
//                  .SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(14).SetItalic().SetUnderline();
//                rt.AddText(" của " + tieuDoanHienThi + ")").SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(14).SetItalic();
//                cellA7.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                cellA7.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//                // ===== 7. ĐỌC TOÀN BỘ BẢNG TyLe (1 LẦN DUY NHẤT) =====
//                var tyLe = new Dictionary<int, (int hienTai, int canDat)>();
//                using (var conn = new SqliteConnection($"Data Source={csdl2}"))
//                {
//                    conn.Open();
//                    using var cmd = new SqliteCommand(
//                        "SELECT ID, [KQ Hien tai], [KQ Can dat] FROM TyLe", conn);
//                    using var rd = cmd.ExecuteReader();
//                    while (rd.Read())
//                    {
//                        // NULL của CSDL là DBNull (toán tử ?? không bắt được) nên phải kiểm tra IsDBNull
//                        int id = rd.IsDBNull(0) ? 0 : Convert.ToInt32(rd.GetValue(0));
//                        int ht = rd.IsDBNull(1) ? 0 : Convert.ToInt32(rd.GetValue(1));
//                        int cd = rd.IsDBNull(2) ? 0 : Convert.ToInt32(rd.GetValue(2));
//                        tyLe[id] = (ht, cd);
//                    }
//                }
//                int tongQS = tyLe.GetValueOrDefault(1).hienTai;
//                int khongPL = tyLe.GetValueOrDefault(6).hienTai;
//                ws.Cell("B11").Value = tongQS;
//                ws.Cell("C11").Value = tongQS;
//                ws.Cell("D11").Value = khongPL;
//                ws.Cell("E11").Value = tyLe.GetValueOrDefault(2).canDat;
//                ws.Cell("F11").Value = tyLe.GetValueOrDefault(3).canDat;
//                ws.Cell("G11").Value = tyLe.GetValueOrDefault(4).canDat;
//                ws.Cell("H11").Value = tyLe.GetValueOrDefault(5).canDat;
//                if (!laNam)
//                {
//                    // 👉 CHẾ ĐỘ XÉT THÁNG: GIỮ NGUYÊN CODE GỐC
//                    ws.Cell("I11").Value = tyLe.GetValueOrDefault(6).canDat;
//                }
//                // 👉 CHẾ ĐỘ XÉT NĂM: I11 dành cho mục khen thưởng (ghi ở bước 9b)
//                // Gán giá trị đã được chuyển đổi vào ô K11
//                var cellK11 = ws.Cell("K11");
//                cellK11.Value = deNghi;
//                // Định dạng ô K11
//                cellK11.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
//                cellK11.Style.Font.FontSize = 13;
//                cellK11.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
//                cellK11.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//                // ===== XỬ LÝ Ô L11 - TÓM TẮT GHI CHÚ (cỡ chữ tự co giãn như I11) =====
//                string ghiChu = Module_XuatTongHop.TOM_TAT_GHI_CHU?.Trim() ?? string.Empty;
//                var cell = ws.Cell("L11");
//                if (cell.IsMerged()) cell = cell.MergedRange().FirstCell();
//                // Mỗi dòng của ghi chú là 1 đoạn khi ước lượng số dòng cần dùng
//                var cacDoanGhiChu = ghiChu.Split('\n').Select(d => d.TrimEnd('\r')).ToList();
//                double coChuGhiChu = TinhCoChuVuaO(ws, cell, cacDoanGhiChu);
//                cell.Value = ghiChu;
//                var style = cell.Style;
//                style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
//                style.Font.FontSize = coChuGhiChu;
//                style.Alignment.Vertical = XLAlignmentVerticalValues.Center;       // giữa theo chiều dọc, giống I11
//                style.Alignment.Horizontal =
//                    ghiChu.Length is > 0 and < 50
//                        ? XLAlignmentHorizontalValues.Center                       // câu rất ngắn thì căn giữa ngang
//                        : XLAlignmentHorizontalValues.Left;                        // còn lại căn trái, giống I11
//                style.Alignment.WrapText = true;
//                style.Alignment.ShrinkToFit = false;                               // bỏ ShrinkToFit cũ vì đã có cỡ chữ tự tính
//                // E12:H12 (cả hai chế độ)
//                ws.Cell("E12").FormulaA1 = "=IFERROR((E11*100)/F11," + LOI_CONG_THUC;
//                ws.Cell("F12").FormulaA1 = "=IFERROR(IF(B11=0,0,(F11*100)/B11)," + LOI_CONG_THUC;
//                ws.Cell("G12").FormulaA1 = "=IFERROR(100-F12," + LOI_CONG_THUC;
//                ws.Cell("H12").FormulaA1 = "=IFERROR(IF(OR(H11=\"\",H11=0),\"0%\",(H11*100)/B11 & \"%\")," + LOI_CONG_THUC;
//                // I12 chỉ dùng ở chế độ Tháng (chế độ Năm: I11:I12 là ô gộp chứa mục khen thưởng)
//                if (!laNam)
//                    ws.Cell("I12").FormulaA1 = "=IFERROR(IF(OR(I11=\"\",I11=0),\"0%\",(I11*100)/B11 & \"%\")," + LOI_CONG_THUC;
//                // ===== ĐỊNH DẠNG xx.xx% =====
//                foreach (string addr in laNam ? new[] { "E12", "F12", "G12", "H12" } : new[] { "E12", "F12", "G12", "H12", "I12" })
//                    ws.Cell(addr).Style.NumberFormat.Format = "00.00\"%\"";
//                // ===== 9. KÝ TÊN =====
//                VietKyTenBaoCaoTongHop(ws, csdl2);
//                // ===== 9b. MỤC II – KHEN THƯỞNG TẬP THỂ / CÁ NHÂN (chỉ chế độ Năm) =====
//                if (laNam)
//                {
//                    try { GhiKhenThuongMucII(ws); }
//                    catch (Exception ex) { Debug.WriteLine("[XuatBaoCaoTongHop] Lỗi mục II khen thưởng: " + ex.Message); }
//                }
//                // ===== 10. LƯU FILE =====
//                wb.Save();
//                LinkDanTep = fileXuat;   // chỉ gán khi đã lưu thành công
//            }
//            catch (Exception ex)
//            {
//                LinkDanTep = null;       // tránh nơi khác mở nhầm tệp lỗi / tệp cũ
//                Module_ThongBao.DangXuLy("Lỗi xuất báo cáo tổng hợp: " + ex.Message);
//            }
//        }
//        // ===================== MỤC II – KHEN THƯỞNG (sheet BAOCAOTONGHOP_NAM, chế độ Năm) =====================
//        // Nguồn dữ liệu (csdl4, mã hóa AES nên giải mã rồi gom nhóm trong RAM):
//        //   Tập thể : ThongKe_KhenThuongTapThe  (HinhThuc_KhenThuong, DonVi_CapKhenThuong, NgayQuyetDinh)
//        //   Cá nhân : ThongKe_GiayKhen          (HinhThuc_Khen,       DonVi_Khen,          NgayCapQD_Khen)
//        // true  : chỉ tính khen thưởng có năm (trong ngày quyết định) trùng năm hệ thống; dòng không đọc được năm vẫn được tính
//        // false : tính toàn bộ dữ liệu đang có trong bảng
//        private const bool LOC_KHEN_THUONG_THEO_NAM_HE_THONG = true;
//        private const string CHUOI_KHONG_CO_KHEN_THUONG = "Không có";
//        // Thông báo lỗi chung cho các công thức E12:I12 (đã gồm dấu đóng ngoặc của IFERROR)
//        private const string LOI_CONG_THUC = "\"Dữ liệu từ tệp csdl2 có thể không đúng hoặc CSDL này không tồn tại, khôi phục lại bản xuất xưởng\")";
//        private sealed class NhomKhen
//        {
//            public string HinhThuc = "";
//            public string DonVi = "";
//            public int SoLuong;
//            public int Hang;      // 0 = giấy khen, 1 = bằng khen, 2 = loại khác
//            public int ThuTu;     // thứ tự xuất hiện đầu tiên trong CSDL
//        }
//        /// <summary>Đọc (đã giải mã) các dòng khen thưởng từ 1 bảng của csdl4. Bảng/cột thiếu -> danh sách rỗng.</summary>
//        private static List<(string HinhThuc, string DonVi, string Ngay)> DocDongKhenThuong(
//            string csdl4, string bang, string cotHinhThuc, string cotDonVi, string cotNgay)
//        {
//            var ds = new List<(string HinhThuc, string DonVi, string Ngay)>();
//            if (string.IsNullOrWhiteSpace(csdl4) || !File.Exists(csdl4)) return ds;
//            try
//            {
//                using var cn = new SqliteConnection($"Data Source={csdl4};Mode=ReadOnly");
//                cn.Open();
//                using (var chk = new SqliteCommand(
//                    "SELECT 1 FROM sqlite_master WHERE type='table' AND name=@t LIMIT 1;", cn))
//                {
//                    chk.Parameters.AddWithValue("@t", bang);
//                    if (chk.ExecuteScalar() == null) return ds;
//                }
//                // Tên bảng/cột là hằng do code này truyền vào (không lấy từ người dùng); rowid giữ đúng thứ tự nhập
//                using var cmd = new SqliteCommand(
//                    $"SELECT {cotHinhThuc}, {cotDonVi}, {cotNgay} FROM {bang} ORDER BY rowid ASC;", cn);
//                using var rd = cmd.ExecuteReader();
//                while (rd.Read())
//                {
//                    string ht = SafeDecrypt(rd.GetValue(0)).Trim();
//                    string dv = SafeDecrypt(rd.GetValue(1)).Trim();
//                    string ngay = SafeDecrypt(rd.GetValue(2)).Trim();
//                    if (ht.Length == 0 && dv.Length == 0) continue;   // dòng rỗng
//                    ds.Add((ht, dv, ngay));
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TongHopKhenThuong] Lỗi đọc bảng {bang}: {ex.Message}");
//            }
//            return ds;
//        }
//        /// <summary>Lấy năm từ chuỗi ngày tự do: "15/03/2026", "2026-03-15", "15/3/26"... Không đọc được -> null.</summary>
//        private static int? DocNamTuNgay(string? s)
//        {
//            if (string.IsNullOrWhiteSpace(s)) return null;
//            var m4 = Regex.Matches(s, @"(?<!\d)(\d{4})(?!\d)");
//            for (int i = m4.Count - 1; i >= 0; i--)
//                if (int.TryParse(m4[i].Value, out int n) && n >= 1900 && n <= 2100) return n;
//            var m2 = Regex.Match(s, @"^\D*\d{1,2}\s*[/\-.]\s*\d{1,2}\s*[/\-.]\s*(\d{2})\s*$");   // chỉ nhận dd/MM/yy đầy đủ
//            if (m2.Success && int.TryParse(m2.Groups[1].Value, out int yy)) return 2000 + yy;
//            return null;
//        }
//        private static string ChuanHoaKhoangTrang(string s) =>
//            string.Join(" ", (s ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
//        // Khoảng cỡ chữ cho phép (pt) và bước nhảy
//        private const double CO_CHU_TOI_DA = 16;
//        private const double CO_CHU_TOI_THIEU = 8;
//        private const double BUOC_CO_CHU = 0.5;
//        /// <summary>Gom nhóm -> danh sách cụm (Số "01", Chữ "giấy khen của Công an tỉnh Thanh Hóa").</summary>
//        private static List<(string So, string Chu)> TongHopCacCum(
//            List<(string HinhThuc, string DonVi, string Ngay)> ds, int? namLoc)
//        {
//            var chiMuc = new Dictionary<string, NhomKhen>(StringComparer.OrdinalIgnoreCase);
//            var danhSach = new List<NhomKhen>();
//            foreach (var (hinhThuc, donVi, ngay) in ds)
//            {
//                if (namLoc.HasValue)
//                {
//                    int? nam = DocNamTuNgay(ngay);
//                    if (nam.HasValue && nam.Value != namLoc.Value) continue;
//                }
//                string ht = ChuanHoaKhoangTrang(hinhThuc);
//                if (ht.Length == 0) ht = "khen thưởng";
//                string dv = ChuanHoaKhoangTrang(donVi);
//                if (dv.StartsWith("của ", StringComparison.OrdinalIgnoreCase)) dv = dv.Substring(4).TrimStart();
//                // Ô "đơn vị khen" lỡ nhập ngày tháng (vd "03/8/2026") -> coi như không có đơn vị, không in "của 03/8/2026"
//                if (Regex.IsMatch(dv, @"^\d{1,2}\s*[/\-.]\s*\d{1,2}\s*[/\-.]\s*\d{2,4}$")) dv = "";
//                string khoa = ht + "|" + dv;
//                if (!chiMuc.TryGetValue(khoa, out var nhom))
//                {
//                    string h = ht.ToLowerInvariant();
//                    nhom = new NhomKhen
//                    {
//                        HinhThuc = char.ToLowerInvariant(ht[0]) + ht.Substring(1),
//                        DonVi = dv,
//                        Hang = h.Contains("giấy khen") ? 0 : h.Contains("bằng khen") ? 1 : 2,
//                        ThuTu = danhSach.Count
//                    };
//                    chiMuc[khoa] = nhom;
//                    danhSach.Add(nhom);
//                }
//                nhom.SoLuong++;
//            }
//            return danhSach
//                .OrderBy(x => x.Hang).ThenBy(x => x.ThuTu)
//                .Select(x => ($"{x.SoLuong:00}",
//                              x.DonVi.Length > 0 ? $"{x.HinhThuc} của {x.DonVi}" : x.HinhThuc))
//                .ToList();
//        }
//        /// <summary>Dựng đoạn chữ thuần (để đo độ dài khi tính cỡ chữ).</summary>
//        private static string DungDoanChuThuan(string nhan, List<(string So, string Chu)> cum)
//        {
//            if (cum.Count == 0) return nhan + " " + CHUOI_KHONG_CO_KHEN_THUONG + ".";
//            return nhan + " " + string.Join("; ", cum.Select(c => c.So + " " + c.Chu)) + ".";
//        }
//        /// <summary>
//        /// Chọn cỡ chữ lớn nhất (CO_CHU_TOI_DA -> CO_CHU_TOI_THIEU) mà toàn bộ các đoạn vẫn nằm gọn trong ô (kể cả ô gộp).
//        /// Ước lượng theo bề rộng cột + tổng chiều cao dòng của vùng gộp; có biên an toàn để chữ không bị cắt.
//        /// </summary>
//        private static double TinhCoChuVuaO(IXLWorksheet ws, IXLCell o, List<string> cacDoan)
//        {
//            var vung = o.IsMerged() ? o.MergedRange() : o.AsRange();
//            var dc = vung.RangeAddress;
//            // Bề rộng khả dụng (pixel). 1 đơn vị độ rộng cột ~ 7 px, trừ lề trong ô
//            double rongPx = 0;
//            for (int c = dc.FirstAddress.ColumnNumber; c <= dc.LastAddress.ColumnNumber; c++)
//                rongPx += ws.Column(c).Width * 7.0 + 5.0;
//            rongPx -= 8;
//            // Chiều cao khả dụng (pt)
//            double caoPt = 0;
//            for (int r = dc.FirstAddress.RowNumber; r <= dc.LastAddress.RowNumber; r++)
//                caoPt += ws.Row(r).Height;
//            caoPt -= 6;
//            if (rongPx <= 20 || caoPt <= 10) return CO_CHU_TOI_THIEU;
//            for (double co = CO_CHU_TOI_DA; co >= CO_CHU_TOI_THIEU; co -= BUOC_CO_CHU)
//            {
//                // Times New Roman: bề rộng trung bình ~0,5 em (tính dư vì có chữ đậm/viết hoa) ; 1pt = 96/72 px
//                double rongKyTuPx = 0.5 * co * 96.0 / 72.0;
//                double kyTuMoiDong = Math.Max(1, rongPx / rongKyTuPx / 1.08);   // 1,08: hao hụt do ngắt dòng theo từ
//                int tongDong = 0;
//                foreach (var doan in cacDoan)
//                    tongDong += Math.Max(1, (int)Math.Ceiling(doan.Length / kyTuMoiDong));
//                double cao = tongDong * co * 1.25;   // 1,25: giãn dòng của Times New Roman
//                if (cao <= caoPt) return co;
//            }
//            return CO_CHU_TOI_THIEU;
//        }
//        /// <summary>Ghi 1 đoạn: nhãn in đậm + từng cụm (số in đậm, phần còn lại thường).</summary>
//        private static void GhiDoanKhenThuong(IXLRichText rt, string nhan, List<(string So, string Chu)> cum, double co)
//        {
//            rt.AddText(nhan).SetBold().SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(co);
//            rt.AddText(" ").SetBold(false).SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(co);
//            if (cum.Count == 0)
//            {
//                rt.AddText(CHUOI_KHONG_CO_KHEN_THUONG + ".").SetBold(false)
//                  .SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(co);
//                return;
//            }
//            for (int i = 0; i < cum.Count; i++)
//            {
//                rt.AddText(cum[i].So).SetBold()
//                  .SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(co);
//                rt.AddText(" " + cum[i].Chu + (i < cum.Count - 1 ? "; " : ".")).SetBold(false)
//                  .SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(co);
//            }
//        }
//        /// <summary>Ghi mục II (ô I11): in đậm nhãn + số, cỡ chữ tự co giãn theo lượng dữ liệu.</summary>
//        private static void GhiKhenThuongMucII(IXLWorksheet ws)
//        {
//            string csdl4 = Module_DanduongGPS.DuongDanCSDL4;
//            int? namLoc = null;
//            if (LOC_KHEN_THUONG_THEO_NAM_HE_THONG &&
//                int.TryParse(Module_XuatPhanLoai.LayNamHeThong()?.Trim(), out int namHT) && namHT > 0)
//                namLoc = namHT;
//            var cumTapThe = TongHopCacCum(DocDongKhenThuong(csdl4, "ThongKe_KhenThuongTapThe",
//                "HinhThuc_KhenThuong", "DonVi_CapKhenThuong", "NgayQuyetDinh"), namLoc);
//            var cumCaNhan = TongHopCacCum(DocDongKhenThuong(csdl4, "ThongKe_GiayKhen",
//                "HinhThuc_Khen", "DonVi_Khen", "NgayCapQD_Khen"), namLoc);
//            var o = ws.Cell(O_MUC_II_CO_DINH);
//            if (o.IsMerged()) o = o.MergedRange().FirstCell();
//            // Giữ tiêu đề cũ (nếu ô có), bỏ phần nội dung đã ghi lần trước
//            string cu = o.GetString();
//            int viTri = cu.IndexOf("* Về tập thể", StringComparison.OrdinalIgnoreCase);
//            string tieuDe = (viTri >= 0 ? cu.Substring(0, viTri) : cu).Trim('\r', '\n', ' ');
//            // Tính cỡ chữ từ nội dung thuần
//            var cacDoan = new List<string>();
//            if (tieuDe.Length > 0) cacDoan.Add(tieuDe);
//            cacDoan.Add(DungDoanChuThuan("* Về tập thể:", cumTapThe));
//            cacDoan.Add(DungDoanChuThuan("* Về cá nhân:", cumCaNhan));
//            double co = TinhCoChuVuaO(ws, o, cacDoan);
//            // Ghi rich text
//            o.Clear(XLClearOptions.Contents);
//            var rt = o.GetRichText();
//            rt.ClearText();
//            if (tieuDe.Length > 0)
//                rt.AddText(tieuDe + "\n").SetBold().SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(co);
//            GhiDoanKhenThuong(rt, "* Về tập thể:", cumTapThe, co);
//            rt.AddText("\n").SetBold(false).SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(co);
//            GhiDoanKhenThuong(rt, "* Về cá nhân:", cumCaNhan, co);
//            o.Style.Alignment.WrapText = true;
//            o.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
//            o.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
//        }
//    }
//}
///Phiên bản 3 (giữ nguyên 100% hành vi nghiệp vụ của bản 2; lưu file an toàn bằng tệp tạm + File.Replace)
///
//TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;
namespace PhanMemThiDua2026
{
    internal static class Module_XuatTongHop
    {
        // ===================== API CÔNG KHAI (giữ nguyên) =====================
        public static string? LinkDanTep;
        public static string TEN_TRUNG_DOAN = "";
        public static string TEN_TIEU_DOAN = "";
        public static string TEN_TRUNG_DOAN_DONG_1 = "";
        public static string TOM_TAT_GHI_CHU = "";
        // ===================== HẰNG SỐ =====================
        private const string O_MUC_II_CO_DINH = "I11";
        private const string KHOANG_TRANG = "     ";
        private const string CHUOI_KHONG_CO_KHEN_THUONG = "Không có";
        private const string LOI_CONG_THUC = "\"Dữ liệu từ tệp csdl2 có thể không đúng hoặc CSDL này không tồn tại, khôi phục lại bản xuất xưởng\")";
        // true  : chỉ tính khen thưởng có năm (trong ngày quyết định) trùng năm hệ thống; dòng không đọc được năm vẫn được tính
        // false : tính toàn bộ dữ liệu đang có trong bảng
        private const bool LOC_KHEN_THUONG_THEO_NAM_HE_THONG = true;
        private const double CO_CHU_TOI_DA = 16;
        private const double CO_CHU_TOI_THIEU = 8;
        private const double BUOC_CO_CHU = 0.5;
        private static readonly object _khoaLuuExcel = new();
        // Regex tạo 1 lần, không dùng RegexOptions.Compiled (tránh tốn RAM/JIT cho vài lần gọi)
        private static readonly Regex RxNgayThuan =
            new(@"^\d{1,2}\s*[/\-.]\s*\d{1,2}\s*[/\-.]\s*\d{2,4}$");
        private static readonly Regex RxNgayHaiSo =
            new(@"^\D*\d{1,2}\s*[/\-.]\s*\d{1,2}\s*[/\-.]\s*(\d{2})\s*$");
        // ===================== LƯU FILE AN TOÀN =====================
        /// <summary>
        /// Lưu Excel an toàn bằng tệp tạm duy nhất cùng thư mục; xác minh gói XLSX trước khi thay thế.
        /// Giữ nguyên tệp đích cũ nếu quá trình lưu hoặc thay thế thất bại.
        /// </summary>
        private static void LuuAnToan(XLWorkbook wb, string fileDich)
        {
            ArgumentNullException.ThrowIfNull(wb);
            ArgumentException.ThrowIfNullOrWhiteSpace(fileDich);
            string dich = Path.GetFullPath(fileDich);
            string thuMuc = Path.GetDirectoryName(dich)
                ?? throw new IOException("Không xác định được thư mục lưu Excel.");
            lock (_khoaLuuExcel)
            {
                Directory.CreateDirectory(thuMuc);
                // Giữ đúng phần mở rộng của tệp đích (.xlsx / .xlsm ...) để hành vi giống Save() cũ
                string tam = Path.Combine(thuMuc,
                    $".{Path.GetFileNameWithoutExtension(dich)}.{Guid.NewGuid():N}.tmp{Path.GetExtension(dich)}");
                try
                {
                    wb.SaveAs(tam);
                    using (var fs = new FileStream(tam, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                    {
                        if (fs.Length == 0)
                            throw new InvalidDataException("Tệp Excel tạm rỗng.");
                        fs.Flush(true);   // ép ghi xuống đĩa trước khi thay thế
                    }
                    using (var zip = ZipFile.OpenRead(tam))   // tệp cụt/hỏng sẽ ném InvalidDataException ở đây
                    {
                        var ct = zip.GetEntry("[Content_Types].xml");
                        var wbXml = zip.GetEntry("xl/workbook.xml");
                        if (ct is null || wbXml is null)
                            throw new InvalidDataException("Tệp Excel tạm không có cấu trúc XLSX hợp lệ.");
                        using var s = wbXml.Open();   // đọc thử để chắc chắn giải nén được
                        s.CopyTo(Stream.Null);
                    }
                    if (File.Exists(dich))
                        File.Replace(tam, dich, null);   // thay thế nguyên tử, giữ thuộc tính/ACL của tệp đích
                    else
                        File.Move(tam, dich);
                }
                finally
                {
                    try
                    {
                        if (File.Exists(tam)) File.Delete(tam);
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }
        // ===================== TIỆN ÍCH CSDL / GIẢI MÃ =====================
        private static SqliteConnection MoCsdl(string path, bool chiDoc)
        {
            var sb = new SqliteConnectionStringBuilder { DataSource = path };
            if (chiDoc) sb.Mode = SqliteOpenMode.ReadOnly;   // csdl4 giữ ReadOnly như bản gốc; csdl2 giữ chế độ mặc định như bản gốc
            var cn = new SqliteConnection(sb.ToString());
            cn.Open();
            return cn;
        }
        private static string SafeDecrypt(object? value)
        {
            try
            {
                if (value == null || value == DBNull.Value) return "";
                string? s = value.ToString();
                if (string.IsNullOrWhiteSpace(s)) return "";
                return Module_BaoMatAES.GiaiMa(s);
            }
            catch
            {
                return "";
            }
        }
        /// <summary>Giải mã an toàn; nếu kết quả rỗng thì trả về giá trị mặc định.</summary>
        private static string SafeDecryptHoacMacDinh(object? value, string macDinh)
        {
            string s = SafeDecrypt(value ?? DBNull.Value);
            return string.IsNullOrWhiteSpace(s) ? macDinh : s;
        }
        /// <summary>Gọi GiaiMa trực tiếp (không Trim, không đổi rỗng); lỗi -> "".</summary>
        private static string GiaiMaTho(string s)
        {
            try { return Module_BaoMatAES.GiaiMa(s) ?? ""; }
            catch { return ""; }
        }
        // ===================== NẠP THÔNG TIN ĐƠN VỊ =====================
        public static void NapThongTinDonVi()
        {
            LayThongTinDonVi(
                out TEN_TRUNG_DOAN_DONG_1,
                out TEN_TRUNG_DOAN,
                out TEN_TIEU_DOAN,
                out TOM_TAT_GHI_CHU
            );
        }
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
                using var cn = MoCsdl(dbPath, false);
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
        // ===================== TIỆN ÍCH NGHIỆP VỤ / ĐỊNH DẠNG =====================
        private static string ChuyenDoiLoaiDeNghi(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            string normalized = input.Trim().ToUpperInvariant();
            return normalized switch
            {
                "ĐVQT" => "Đơn vị Quyết thắng",
                "ĐVTT" => "Đơn vị Tiên tiến",
                "HTNV"  => "Hoàn thành nhiệm vụ",   
                "KHTNV" => "Không hoàn thành nhiệm vụ",
                _ => input
            };
        }
        /// <summary>Tháng 1, 2 -> "01", "02"; 3..12 -> "3".."12"; không phải số -> giữ nguyên.</summary>
        private static string ChuanHoaThang(string? t)
        {
            if (int.TryParse(t, out int n))
                return (n == 1 || n == 2) ? n.ToString("00") : n.ToString();
            return t ?? "";
        }
        private static void DinhDang(IXLCell c, double size, bool? bold = null, bool? italic = null)
        {
            c.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
            c.Style.Font.FontSize = size;
            if (bold.HasValue) c.Style.Font.Bold = bold.Value;
            if (italic.HasValue) c.Style.Font.Italic = italic.Value;
            c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            c.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        private static void GhiTieuDe(IXLWorksheet ws, string text)
        {
            var tieuDe = ws.Range("A6:M6");
            tieuDe.Merge();
            tieuDe.Value = text;
            tieuDe.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            tieuDe.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            tieuDe.Style.Font.Bold = true;
            tieuDe.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
            tieuDe.Style.Font.FontSize = 14;
        }
        /// <summary>Gạch chân đoạn giữa chiếm ~1/3 độ dài chữ (không xóa ô, chỉ reset rich text).</summary>
        private static void GanGachChan1Phan3(IXLCell cell, string text)
        {
            if (cell == null) return;
            if (string.IsNullOrWhiteSpace(text)) return;
            int totalLen = text.Length;
            int underlineLen = (int)Math.Round(totalLen / 3.0);
            if (underlineLen <= 0) return;
            int start = (totalLen - underlineLen) / 2;
            var rich = cell.GetRichText();
            rich.ClearText();
            if (start > 0)
                rich.AddText(text.Substring(0, start));
            rich.AddText(text.Substring(start, underlineLen))
                .SetUnderline();
            int end = start + underlineLen;
            if (end < totalLen)
                rich.AddText(text.Substring(end));
        }
        // ===================== DỮ LIỆU ĐỌC TỪ csdl2 (đọc xong đóng kết nối rồi mới ghi Excel) =====================
        private sealed class ThongTinNgay
        {
            public string Thang = KHOANG_TRANG;
            public string Nam = KHOANG_TRANG;
            public string Ngay = KHOANG_TRANG;
            public string DiaDiem = KHOANG_TRANG;
            public string DeNghi = KHOANG_TRANG;
        }
        private sealed class ThongTinKy
        {
            public bool DocDuoc;                       // false: lỗi đọc ChiHuyD -> giữ nguyên phần ký tên của bản mẫu
            public string HoTenKy = KHOANG_TRANG;
            public string ChucVuTruong = "";
            public string ChucVuNguoiKy = "";
            public int IdDau = -1;
            public int IdKy = -1;
        }
        private static string DocKyHieuDonVi(SqliteConnection cn)
        {
            string kyHieuTieuDoan = "";
            string kyHieuTrungDoan = "";
            try
            {
                // Lọc chuẩn xác chỉ lấy ID = 1
                using var cmd = new SqliteCommand("SELECT KyHieu_TrungDoan, KyHieu_TieuDoan FROM KyHieu_DonVi WHERE ID = 1", cn);
                using var rd = cmd.ExecuteReader();
                if (rd.Read())
                {
                    kyHieuTrungDoan = SafeDecrypt(rd["KyHieu_TrungDoan"]);
                    kyHieuTieuDoan = SafeDecrypt(rd["KyHieu_TieuDoan"]);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[XuatBaoCaoTongHop] Lỗi lấy Ký hiệu đơn vị: {ex.Message}");
            }
            kyHieuTieuDoan = kyHieuTieuDoan.Trim();
            kyHieuTrungDoan = kyHieuTrungDoan.Trim();
            // Phân cách bằng dấu phẩy; nếu 1 trong 2 chuỗi trống thì không in dấu phẩy thừa
            if (!string.IsNullOrEmpty(kyHieuTieuDoan) && !string.IsNullOrEmpty(kyHieuTrungDoan))
                return $"{kyHieuTieuDoan}, {kyHieuTrungDoan}";
            if (!string.IsNullOrEmpty(kyHieuTieuDoan)) return kyHieuTieuDoan;
            if (!string.IsNullOrEmpty(kyHieuTrungDoan)) return kyHieuTrungDoan;
            return "";
        }
        private static ThongTinNgay DocThongTinNgay(SqliteConnection cn)
        {
            var r = new ThongTinNgay();
            try
            {
                using var cmd = new SqliteCommand("SELECT Thang, Nam, Ngay, DiaDiem, LoaiDeNghi FROM ThongTin WHERE ID = 1", cn);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    string thangGiaiMa = SafeDecrypt(reader["Thang"]).Trim();
                    r.Thang = string.IsNullOrWhiteSpace(thangGiaiMa) ? KHOANG_TRANG : ChuanHoaThang(thangGiaiMa);
                    r.Nam = SafeDecryptHoacMacDinh(reader["Nam"], KHOANG_TRANG);
                    r.Ngay = SafeDecryptHoacMacDinh(reader["Ngay"], KHOANG_TRANG);
                    r.DiaDiem = SafeDecryptHoacMacDinh(reader["DiaDiem"], KHOANG_TRANG);
                    r.DeNghi = ChuyenDoiLoaiDeNghi(SafeDecryptHoacMacDinh(reader["LoaiDeNghi"], KHOANG_TRANG));
                }
            }
            catch (Exception ex)
            {
                Module_ThongBao.Loi("Lỗi khi load thông tin Ngày/Tháng/Năm/Địa điểm/Đề nghị từ CSDL:\n" + ex.Message);
            }
            return r;
        }
        private static void DocLoaiBaoCao(SqliteConnection cn, out string loaiBaoCao, out string tuanBaoCao)
        {
            loaiBaoCao = "";
            tuanBaoCao = "";
            try
            {
                using var cmd = new SqliteCommand(
                    "SELECT ChonLoaiBaoCao, ChonTuan FROM ChonLoaiBaoCao ORDER BY ID DESC LIMIT 1", cn);
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
            loaiBaoCao = (loaiBaoCao ?? "").Trim().ToUpperInvariant();
            tuanBaoCao = (tuanBaoCao ?? "").Trim();
        }
        private static string DocKyHieuBaoCao(SqliteConnection cn)
        {
            string kyHieuBaoCao = "...............";
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT KyHieuBaoCao FROM ThongTin WHERE ID=1";
                kyHieuBaoCao = SafeDecryptHoacMacDinh(cmd.ExecuteScalar(), kyHieuBaoCao);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[XuatBaoCaoTongHop] Lỗi đọc KyHieuBaoCao: " + ex.Message);
            }
            return kyHieuBaoCao;
        }
        /// <summary>Đọc toàn bộ bảng TyLe (1 lần duy nhất). Lỗi dữ liệu -> ném ngoại lệ như bản gốc (hủy xuất, không ghi file).</summary>
        private static Dictionary<int, (int hienTai, int canDat)> DocTyLe(SqliteConnection cn)
        {
            var tyLe = new Dictionary<int, (int hienTai, int canDat)>();
            using var cmd = new SqliteCommand("SELECT ID, [KQ Hien tai], [KQ Can dat] FROM TyLe", cn);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                // NULL của CSDL là DBNull (toán tử ?? không bắt được) nên phải kiểm tra IsDBNull
                int id = rd.IsDBNull(0) ? 0 : Convert.ToInt32(rd.GetValue(0));
                int ht = rd.IsDBNull(1) ? 0 : Convert.ToInt32(rd.GetValue(1));
                int cd = rd.IsDBNull(2) ? 0 : Convert.ToInt32(rd.GetValue(2));
                tyLe[id] = (ht, cd);
            }
            return tyLe;
        }
        private static ThongTinKy DocThongTinKy(SqliteConnection cn)
        {
            var k = new ThongTinKy();
            // --- Họ tên người ký ---
            try
            {
                using var cmd = new SqliteCommand("SELECT ChiHuyD FROM ThongTin WHERE ID = 1", cn);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    // Cắt Trim() ngay từ đầu phòng hờ khoảng trắng
                    string chiHuyRaw = reader["ChiHuyD"]?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrWhiteSpace(chiHuyRaw))
                    {
                        string decoded = GiaiMaTho(chiHuyRaw);
                        // Giải mã thất bại: dữ liệu cũ dạng chữ thường thì dùng tiếp, còn chuỗi mã hóa "v2|..." thì bỏ trống
                        string ketQua = string.IsNullOrEmpty(decoded)
                            ? (chiHuyRaw.StartsWith("v2|", StringComparison.Ordinal) ? "" : chiHuyRaw)
                            : decoded.Trim();
                        if (!string.IsNullOrWhiteSpace(ketQua)) k.HoTenKy = ketQua;
                    }
                }
            }
            catch (Exception ex)
            {
                Module_ThongBao.Loi("Lỗi khi load thông tin Chỉ huy từ CSDL:\n" + ex.Message);
            }
            // --- Chức vụ: người đầu tiên = Tiểu đoàn trưởng (dự phòng); khớp tên -> người ký ---
            try
            {
                using var cmd = new SqliteCommand("SELECT ID, HoVaTen, ChucVu FROM ChiHuyD ORDER BY ID ASC", cn);
                using var rd = cmd.ExecuteReader();
                bool isFirst = true;
                bool foundMatch = false;
                while (rd.Read())
                {
                    int id = Convert.ToInt32(rd["ID"]);
                    string hoTenRaw = rd["HoVaTen"]?.ToString()?.Trim() ?? "";
                    string chucVuRaw = rd["ChucVu"]?.ToString()?.Trim() ?? "";
                    string htDec = GiaiMaTho(hoTenRaw);
                    htDec = (string.IsNullOrEmpty(htDec) ? hoTenRaw : htDec).Trim();
                    string cvDec = GiaiMaTho(chucVuRaw);
                    if (string.IsNullOrEmpty(cvDec)) cvDec = chucVuRaw;
                    if (isFirst)
                    {
                        k.IdDau = id;
                        k.ChucVuTruong = cvDec;
                        isFirst = false;
                    }
                    if (htDec.Equals(k.HoTenKy, StringComparison.OrdinalIgnoreCase))
                    {
                        k.IdKy = id;
                        k.ChucVuNguoiKy = cvDec;
                        foundMatch = true;
                        break;
                    }
                }
                // Nếu không khớp ai thì dùng mặc định người đầu tiên
                if (!foundMatch)
                {
                    k.IdKy = k.IdDau;
                    k.ChucVuNguoiKy = k.ChucVuTruong;
                }
                k.DocDuoc = true;
            }
            catch (Exception ex)
            {
                // Lỗi đọc bảng ChiHuyD: giữ nguyên phần ký tên của bản mẫu, không làm hỏng cả báo cáo
                Debug.WriteLine("[VietKyTenBaoCaoTongHop] " + ex.Message);
            }
            return k;
        }
        // ===================== GHI SHEET =====================
        private static void VietKyTenBaoCaoTongHop(IXLWorksheet ws, ThongTinKy k)
        {
            if (!k.DocDuoc) return;
            if (k.IdKy == k.IdDau)
            {
                // Trưởng đơn vị trực tiếp ký
                ws.Cell("L14").Value = k.ChucVuTruong.ToUpperInvariant();
                ws.Cell("L15").Value = "";   // dọn sạch ô L15 phòng hờ template có chữ rác
            }
            else
            {
                // Cấp phó ký thay
                ws.Cell("L14").Value = ("KT. " + k.ChucVuTruong).ToUpperInvariant();
                ws.Cell("L15").Value = k.ChucVuNguoiKy.ToUpperInvariant();
            }
            ws.Cell("L19").Value = k.HoTenKy;
            DinhDang(ws.Cell("L14"), 14, bold: true);
            DinhDang(ws.Cell("L15"), 14, bold: true);
            DinhDang(ws.Cell("L19"), 14);   // L19 không đụng tới Bold
        }
        private static void GhiDauTrang(IXLWorksheet ws, bool laNam, string chuoiKyHieuGhep, ThongTinNgay tn)
        {
            // A11: ký hiệu đơn vị
            var cellA11 = ws.Cell("A11");
            cellA11.Value = chuoiKyHieuGhep;
            DinhDang(cellA11, 12, bold: false);
            string tenTrungDoanDong1 = string.IsNullOrWhiteSpace(TEN_TRUNG_DOAN_DONG_1) ? KHOANG_TRANG : TEN_TRUNG_DOAN_DONG_1;
            string tenTrungDoanCSCD = string.IsNullOrWhiteSpace(TEN_TRUNG_DOAN) ? "" : TEN_TRUNG_DOAN;
            string tenTieuDoanCSCD = string.IsNullOrWhiteSpace(TEN_TIEU_DOAN) ? KHOANG_TRANG : TEN_TIEU_DOAN;
            // A1: Trung đoàn dòng 1 (luôn ghi, không đậm)
            var cellA1 = ws.Cell("A1");
            cellA1.Value = tenTrungDoanDong1;
            DinhDang(cellA1, 13, bold: false);
            if (!string.IsNullOrWhiteSpace(tenTrungDoanCSCD))
            {
                // A2: Trung đoàn CSCD (không đậm)
                var cellA2 = ws.Cell("A2");
                cellA2.Value = tenTrungDoanCSCD;
                DinhDang(cellA2, 13, bold: false);
                // A3: Tiểu đoàn (in đậm + gạch chân 1/3)
                var cellA3 = ws.Cell("A3");
                cellA3.Value = tenTieuDoanCSCD;
                DinhDang(cellA3, 13, bold: true);
                GanGachChan1Phan3(cellA3, tenTieuDoanCSCD);
            }
            else
            {
                // Không có trung đoàn: A2 là dòng cuối
                var cellA2 = ws.Cell("A2");
                cellA2.Value = tenTieuDoanCSCD;
                DinhDang(cellA2, 13, bold: true);
                GanGachChan1Phan3(cellA2, tenTieuDoanCSCD);
                // A3: để trống
                var cellA3 = ws.Cell("A3");
                cellA3.Value = "";
                cellA3.Style.Font.Bold = false;
            }
            // Địa điểm, ngày tháng năm
            var cellThoiGian = ws.Cell(laNam ? "J4" : "H4");
            cellThoiGian.Value = $"{tn.DiaDiem}, ngày {tn.Ngay} tháng {tn.Thang} năm {tn.Nam}";
            DinhDang(cellThoiGian, 14, italic: true);
        }
        private static void GhiTieuDeThoiGian(IXLWorksheet ws, bool laNam, string loaiBaoCao, string tuanBaoCao)
        {
            if (laNam)
            {
                // Chế độ xét Năm
                string namHT = Module_XuatPhanLoai.LayNamHeThong();
                GhiTieuDe(ws, $"Phân loại tập thể và tổng hợp năm {namHT}");
                return;
            }
            // Chế độ xét Tháng
            string rawThang = Module_XuatPhanLoai.LayThangHeThong();
            string namHeThong = Module_XuatPhanLoai.LayNamHeThong();
            // Tháng 1, 2 giữ dạng "01", "02"; từ tháng 3 đến 12 loại bỏ số 0 thừa
            string thangHT = rawThang;
            if (int.TryParse(rawThang, out int thangSo))
                thangHT = (thangSo == 1 || thangSo == 2) ? thangSo.ToString("00") : thangSo.ToString();
            string chuoiThoiGian;
            if (loaiBaoCao.Contains("TUẦN"))
            {
                // Nếu dữ liệu là "Tuần 1" -> lấy số 1
                if (tuanBaoCao.ToUpperInvariant().Contains("TUẦN"))
                    tuanBaoCao = tuanBaoCao.ToUpperInvariant().Replace("TUẦN", "").Trim();
                if (string.IsNullOrWhiteSpace(tuanBaoCao))
                    tuanBaoCao = "1";
                chuoiThoiGian = $"TUẦN {tuanBaoCao} THÁNG {thangHT}/{namHeThong}";
            }
            else
            {
                chuoiThoiGian = $"THÁNG {thangHT}/{namHeThong}";
            }
            GhiTieuDe(ws, $"DANH SÁCH TỔNG HỢP CÁC LOẠI {chuoiThoiGian}");
        }
        private static void GhiDongKemTheo(IXLWorksheet ws, string kyHieuBaoCao, ThongTinNgay tn)
        {
            string tenTieuDoanCSCD = string.IsNullOrWhiteSpace(TEN_TIEU_DOAN) ? KHOANG_TRANG : TEN_TIEU_DOAN;
            // "TIỂU ĐOÀN 2" -> "Tiểu đoàn 2"
            string tieuDoanThuong = tenTieuDoanCSCD.Trim().ToLowerInvariant();
            string tieuDoanHienThi = tieuDoanThuong.Length == 0
                ? ""
                : char.ToUpperInvariant(tieuDoanThuong[0]) + tieuDoanThuong.Substring(1);
            var cellA7 = ws.Cell("A7");
            cellA7.Clear(XLClearOptions.Contents);
            var rt = cellA7.GetRichText();
            rt.AddText("(Kèm theo Báo cáo ")
              .SetFontName(Module_HeThong.Font_Times_New_Roman)
              .SetFontSize(14)
              .SetItalic();
            rt.AddText($"số:            {kyHieuBaoCao}, ngày {tn.Ngay}/{tn.Thang}/{tn.Nam}")
              .SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(14).SetItalic().SetUnderline();
            rt.AddText(" của " + tieuDoanHienThi + ")")
              .SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(14).SetItalic();
            cellA7.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellA7.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        private static void GhiSoLieuVaTyLe(IXLWorksheet ws, bool laNam,
            Dictionary<int, (int hienTai, int canDat)> tyLe, ThongTinNgay tn)
        {
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
                // Chế độ xét Tháng
                ws.Cell("I11").Value = tyLe.GetValueOrDefault(6).canDat;
            }
            // Chế độ xét Năm: I11 dành cho mục khen thưởng (ghi ở GhiKhenThuongMucII)
            // K11: loại đề nghị đã chuyển đổi
            var cellK11 = ws.Cell("K11");
            cellK11.Value = tn.DeNghi;
            cellK11.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
            cellK11.Style.Font.FontSize = 13;
            cellK11.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cellK11.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            // L11: tóm tắt ghi chú (cỡ chữ tự co giãn như I11)
            string ghiChu = TOM_TAT_GHI_CHU?.Trim() ?? string.Empty;
            var cell = ws.Cell("L11");
            if (cell.IsMerged()) cell = cell.MergedRange().FirstCell();
            var cacDoanGhiChu = ghiChu.Split('\n').Select(d => d.TrimEnd('\r')).ToList();
            double coChuGhiChu = TinhCoChuVuaO(ws, cell, cacDoanGhiChu);
            cell.Value = ghiChu;
            var style = cell.Style;
            style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
            style.Font.FontSize = coChuGhiChu;
            style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            style.Alignment.Horizontal =
                ghiChu.Length is > 0 and < 50
                    ? XLAlignmentHorizontalValues.Center
                    : XLAlignmentHorizontalValues.Left;
            style.Alignment.WrapText = true;
            style.Alignment.ShrinkToFit = false;
            // E12:H12 (cả hai chế độ)
            ws.Cell("E12").FormulaA1 = "=IFERROR((E11*100)/F11," + LOI_CONG_THUC;
            ws.Cell("F12").FormulaA1 = "=IFERROR(IF(B11=0,0,(F11*100)/B11)," + LOI_CONG_THUC;
            ws.Cell("G12").FormulaA1 = "=IFERROR(100-F12," + LOI_CONG_THUC;
            ws.Cell("H12").FormulaA1 = "=IFERROR(IF(OR(H11=\"\",H11=0),\"0%\",(H11*100)/B11 & \"%\")," + LOI_CONG_THUC;
            // I12 chỉ dùng ở chế độ Tháng (chế độ Năm: I11:I12 là ô gộp chứa mục khen thưởng)
            if (!laNam)
                ws.Cell("I12").FormulaA1 = "=IFERROR(IF(OR(I11=\"\",I11=0),\"0%\",(I11*100)/B11 & \"%\")," + LOI_CONG_THUC;
            // Định dạng xx.xx%
            foreach (string addr in laNam ? new[] { "E12", "F12", "G12", "H12" } : new[] { "E12", "F12", "G12", "H12", "I12" })
                ws.Cell(addr).Style.NumberFormat.Format = "00.00\"%\"";
        }
        // ===================== XUẤT BÁO CÁO TỔNG HỢP =====================
        public static void XuatBaoCaoTongHop(string fileXuat)
        {
            try
            {
                NapThongTinDonVi(); // BẮT BUỘC PHẢI CÓ
                if (string.IsNullOrWhiteSpace(fileXuat) || !File.Exists(fileXuat))
                    throw new Exception("File Excel tổng hợp không tồn tại.");
                using var wb = new XLWorkbook(fileXuat);
                // Chốt chế độ Năm/Tháng MỘT LẦN cho cả lần xuất
                bool laNam = Module_HeThong.IsCheDoXetThiDuaNam();
                // Chọn sheet đổ dữ liệu tự động theo chế độ Năm / Tháng
                string tenSheet = laNam ? "BAOCAOTONGHOP_NAM" : "BAO CAO TONG HOP";
                var ws = wb.Worksheet(tenSheet);
                string csdl2 = Module_DanduongGPS.DuongDanCSDL2;
                if (string.IsNullOrWhiteSpace(csdl2) || !File.Exists(csdl2))
                    throw new Exception("Không tìm thấy CSDL csdl2.");
                // ---- ĐỌC TOÀN BỘ DỮ LIỆU: 1 kết nối csdl2, đọc xong đóng ngay trước khi ghi Excel ----
                string chuoiKyHieuGhep;
                ThongTinNgay tn;
                string loaiBaoCao, tuanBaoCao;
                string kyHieuBaoCao;
                Dictionary<int, (int hienTai, int canDat)> tyLe;
                ThongTinKy ky;
                using (var cn = MoCsdl(csdl2, false))
                {
                    chuoiKyHieuGhep = DocKyHieuDonVi(cn);
                    tn = DocThongTinNgay(cn);
                    DocLoaiBaoCao(cn, out loaiBaoCao, out tuanBaoCao);
                    kyHieuBaoCao = DocKyHieuBaoCao(cn);
                    tyLe = DocTyLe(cn);
                    ky = DocThongTinKy(cn);
                }
                // ---- GHI SHEET ----
                GhiDauTrang(ws, laNam, chuoiKyHieuGhep, tn);
                GhiTieuDeThoiGian(ws, laNam, loaiBaoCao, tuanBaoCao);
                GhiDongKemTheo(ws, kyHieuBaoCao, tn);
                GhiSoLieuVaTyLe(ws, laNam, tyLe, tn);
                // Ký tên
                VietKyTenBaoCaoTongHop(ws, ky);
                // Mục II – khen thưởng tập thể / cá nhân (chỉ chế độ Năm)
                if (laNam)
                {
                    try { GhiKhenThuongMucII(ws); }
                    catch (Exception ex) { Debug.WriteLine("[XuatBaoCaoTongHop] Lỗi mục II khen thưởng: " + ex.Message); }
                }
                // ---- LƯU FILE AN TOÀN ----
                LuuAnToan(wb, fileXuat);
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
        private sealed class NhomKhen
        {
            public string HinhThuc = "";
            public string DonVi = "";
            public int SoLuong;
            public int Hang;      // 0 = giấy khen, 1 = bằng khen, 2 = loại khác
            public int ThuTu;     // thứ tự xuất hiện đầu tiên trong CSDL
        }
        /// <summary>Gom nhóm ngay khi đọc (streaming) — không giữ danh sách mọi dòng trong RAM.</summary>
        private sealed class TongHopKhen
        {
            private readonly Dictionary<string, NhomKhen> _chiMuc = new(StringComparer.OrdinalIgnoreCase);
            private readonly List<NhomKhen> _danhSach = new();
            private readonly int? _namLoc;
            public TongHopKhen(int? namLoc) => _namLoc = namLoc;
            public void Them(string hinhThuc, string donVi, string ngay)
            {
                if (_namLoc.HasValue)
                {
                    int? nam = DocNamTuNgay(ngay);
                    if (nam.HasValue && nam.Value != _namLoc.Value) return;
                }
                string ht = ChuanHoaKhoangTrang(hinhThuc);
                if (ht.Length == 0) ht = "khen thưởng";
                string dv = ChuanHoaKhoangTrang(donVi);
                if (dv.StartsWith("của ", StringComparison.OrdinalIgnoreCase)) dv = dv.Substring(4).TrimStart();
                // Ô "đơn vị khen" lỡ nhập ngày tháng (vd "03/8/2026") -> coi như không có đơn vị
                if (RxNgayThuan.IsMatch(dv)) dv = "";
                string khoa = ht + "|" + dv;
                if (!_chiMuc.TryGetValue(khoa, out var nhom))
                {
                    string h = ht.ToLowerInvariant();
                    nhom = new NhomKhen
                    {
                        HinhThuc = char.ToLowerInvariant(ht[0]) + ht.Substring(1),
                        DonVi = dv,
                        Hang = h.Contains("giấy khen") ? 0 : h.Contains("bằng khen") ? 1 : 2,
                        ThuTu = _danhSach.Count
                    };
                    _chiMuc[khoa] = nhom;
                    _danhSach.Add(nhom);
                }
                nhom.SoLuong++;
            }
            /// <summary>Danh sách cụm (Số "01", Chữ "giấy khen của Công an tỉnh Thanh Hóa").</summary>
            public List<(string So, string Chu)> LayCacCum() =>
                _danhSach
                    .OrderBy(x => x.Hang).ThenBy(x => x.ThuTu)
                    .Select(x => ($"{x.SoLuong:00}",
                                  x.DonVi.Length > 0 ? $"{x.HinhThuc} của {x.DonVi}" : x.HinhThuc))
                    .ToList();
        }
        /// <summary>Đọc (đã giải mã) từng dòng của 1 bảng csdl4 và đẩy thẳng vào bộ gom nhóm. Bảng/cột thiếu -> bỏ qua.</summary>
        private static void DocVaGomKhenThuong(SqliteConnection cn, string bang,
            string cotHinhThuc, string cotDonVi, string cotNgay, TongHopKhen tong)
        {
            try
            {
                using (var chk = new SqliteCommand(
                    "SELECT 1 FROM sqlite_master WHERE type='table' AND name=@t LIMIT 1;", cn))
                {
                    chk.Parameters.AddWithValue("@t", bang);
                    if (chk.ExecuteScalar() == null) return;
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
                    tong.Them(ht, dv, ngay);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TongHopKhenThuong] Lỗi đọc bảng {bang}: {ex.Message}");
            }
        }
        /// <summary>Lấy năm từ chuỗi ngày tự do: "15/03/2026", "2026-03-15", "15/3/26"... Không đọc được -> null.</summary>
        private static int? DocNamTuNgay(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            // Quét từ cuối: cụm ĐÚNG 4 chữ số liền nhau (không dính chữ số khác), hợp lệ 1900..2100 — tương đương Regex cũ nhưng không cấp phát
            ReadOnlySpan<char> span = s.AsSpan();
            int i = span.Length - 1;
            while (i >= 0)
            {
                if (!char.IsDigit(span[i])) { i--; continue; }
                int cuoi = i;
                while (i >= 0 && char.IsDigit(span[i])) i--;
                if (cuoi - i == 4 &&
                    int.TryParse(span.Slice(i + 1, 4), out int n) && n >= 1900 && n <= 2100)
                    return n;
            }
            var m2 = RxNgayHaiSo.Match(s);   // chỉ nhận dd/MM/yy đầy đủ
            if (m2.Success && int.TryParse(m2.Groups[1].Value, out int yy)) return 2000 + yy;
            return null;
        }
        private static string ChuanHoaKhoangTrang(string s) =>
            string.Join(" ", (s ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
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
            // Độ dài từng đoạn chỉ lấy 1 lần, ngoài vòng lặp cỡ chữ
            var doDai = new int[cacDoan.Count];
            for (int i = 0; i < doDai.Length; i++) doDai[i] = cacDoan[i].Length;
            for (double co = CO_CHU_TOI_DA; co >= CO_CHU_TOI_THIEU; co -= BUOC_CO_CHU)
            {
                // Times New Roman: bề rộng trung bình ~0,5 em (tính dư vì có chữ đậm/viết hoa); 1pt = 96/72 px
                double rongKyTuPx = 0.5 * co * 96.0 / 72.0;
                double kyTuMoiDong = Math.Max(1, rongPx / rongKyTuPx / 1.08);   // 1,08: hao hụt do ngắt dòng theo từ
                int tongDong = 0;
                for (int i = 0; i < doDai.Length; i++)
                    tongDong += Math.Max(1, (int)Math.Ceiling(doDai[i] / kyTuMoiDong));
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
        private static void GhiKhenThuongMucII(IXLWorksheet ws)
        {
            string csdl4 = Module_DanduongGPS.DuongDanCSDL4;
            int? namLoc = null;
            if (LOC_KHEN_THUONG_THEO_NAM_HE_THONG &&
                int.TryParse(Module_XuatPhanLoai.LayNamHeThong()?.Trim(), out int namHT) && namHT > 0)
                namLoc = namHT;
            var tapThe = new TongHopKhen(namLoc);
            var caNhan = new TongHopKhen(namLoc);
            if (!string.IsNullOrWhiteSpace(csdl4) && File.Exists(csdl4))
            {
                try
                {
                    using var cn = MoCsdl(csdl4, true);   // 1 kết nối ReadOnly cho cả 2 bảng
                    DocVaGomKhenThuong(cn, "ThongKe_KhenThuongTapThe",
                        "HinhThuc_KhenThuong", "DonVi_CapKhenThuong", "NgayQuyetDinh", tapThe);
                    DocVaGomKhenThuong(cn, "ThongKe_GiayKhen",
                        "HinhThuc_Khen", "DonVi_Khen", "NgayCapQD_Khen", caNhan);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[TongHopKhenThuong] Lỗi mở csdl4: " + ex.Message);
                }
            }
            var cumTapThe = tapThe.LayCacCum();
            var cumCaNhan = caNhan.LayCacCum();
            var o = ws.Cell(O_MUC_II_CO_DINH);
            if (o.IsMerged()) o = o.MergedRange().FirstCell();
            // Giữ tiêu đề cũ (nếu ô có), bỏ phần nội dung đã ghi lần trước
            string cu = o.GetString();
            int viTri = cu.IndexOf("* Về tập thể", StringComparison.OrdinalIgnoreCase);
            string tieuDe = (viTri >= 0 ? cu.Substring(0, viTri) : cu).Trim('\r', '\n', ' ');
            // Tính cỡ chữ từ nội dung thuần
            var cacDoan = new List<string>(3);
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