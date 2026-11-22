using ClosedXML.Excel;
namespace PhanMemThiDua2026
{
    public static class Module_BanQuyen
    {
        //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
        private const string SheetName = "THONG_TIN";
        private static readonly string[] TieuDe =
        {
            "Tên phần mềm",
            "Phiên bản",
            "Ngày cập nhật",
            "Người phát triển",
            "Máy tính tạo tệp",
            "Ngày tháng năm tạo tệp",
            "Tên tài khoản tạo tệp",
            "Token",
            "Phiên bản" // ⭐ Thêm tiêu đề cho hàng 9
        };
        // Hàm đóng dấu thuộc tính (Metadata) bằng chữ không dấu
        public static void DongDauMetadata(XLWorkbook workbook)
        {
            if (workbook == null) return;
            try
            {
                // ❌ ĐÃ XÓA DÒNG GỌI NHẦM Ở ĐÂY ĐỂ TRÁNH LỖI ĐỆ QUY
                string tenUser = string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM)
                                 ? "He thong"
                                 : Module_TaiKhoan.TenTaiKhoan_RAM;
                // 1. Nhóm thông tin mô tả (Description)
                workbook.Properties.Title = "Bao cao du lieu thi dua";
                workbook.Properties.Subject = "Phan mem Thi dua 2026";
                workbook.Properties.Keywords = "thi dua, bao cao, ba nhat, phong trao";
                workbook.Properties.Category = "Ho tro Cong tac thi dua";
                workbook.Properties.Comments = "Ban quyen thuoc TrungKien-0975287973";
                // 2. Nhóm nguồn gốc (Origin)
                workbook.Properties.Author = Module_PhienBan.TenPhanMem;
                workbook.Properties.LastModifiedBy = tenUser;
                workbook.Properties.Company = "D2-E09";
                workbook.Properties.Manager = "@TrungKien";
                // 3. Nhóm nội dung (Content)
                workbook.Properties.Status = "Luu hanh noi bo";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Loi dong dau Metadata: " + ex.Message);
            }
        }
        public static void DongDauExcel(XLWorkbook workbook)
        {
            if (workbook == null) return;
            try
            {
                // ✅ GỌI HÀM ĐÓNG DẤU METADATA Ở ĐÚNG VỊ TRÍ NÀY
                DongDauMetadata(workbook);
                if (workbook.Worksheets.Contains(SheetName))
                {
                    workbook.Worksheet(SheetName).Delete();
                }
                var ws = workbook.Worksheets.Add(SheetName);
                ws.SetTabColor(XLColor.FromArgb(146, 208, 80));
                ws.Visibility = XLWorksheetVisibility.Hidden;
                ws.ShowGridLines = false;
                string token = DocTokenAnToan();
                string machineName = "Unknown";
                try { machineName = Environment.MachineName; } catch { }
                // ⭐ Đóng dấu phiên bản bằng AES để chống can thiệp file Excel
                string phienBanHienTai = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
                bool laTanBinh = phienBanHienTai.LaCheDoTanBinh();
                string textPhienBan = laTanBinh ? $"Phần mềm dành cho {Module_TaiKhoan.TenTB_Hoa}" : "Phần mềm dành cho CBCS";
                string[] giaTri =
                {
                    "Phần mềm Thi đua 2026",
                    Module_PhienBan.SoftwareVersion ?? "",
                    Module_PhienBan.NgayThangNamHeThong ?? "",
                    Module_PhienBan.NguoiPhatTrienPhanMem ?? "",
                    machineName,
                    DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                    string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM) ? "System" : Module_TaiKhoan.TenTaiKhoan_RAM,
                    token,
                    Module_BaoMatAES.MaHoa(textPhienBan) // ⭐ Mã hóa ô B9
                };
                for (int i = 0; i < TieuDe.Length; i++)
                {
                    int row = i + 1;
                    ws.Cell(row, 1).Value = TieuDe[i];
                    ws.Cell(row, 2).Value = giaTri[i];
                }
                ws.Cell("B6").Style.NumberFormat.Format = "@";
                // ⭐ Mở rộng vùng định dạng xuống dòng 9
                var usedRange = ws.Range("A1:B9");
                usedRange.Style.Font.FontName = "Calibri";
                usedRange.Style.Font.FontSize = 11;
                usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                usedRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                usedRange.Style.Alignment.WrapText = false;
                usedRange.Style.Fill.BackgroundColor = XLColor.FromArgb(220, 255, 220);
                var rangeA = ws.Range("A1:A9");
                rangeA.Style.Font.Bold = true;
                rangeA.Style.Fill.BackgroundColor = XLColor.FromArgb(146, 208, 80);
                var rangeB = ws.Range("B1:B9");
                rangeB.Style.Fill.BackgroundColor = XLColor.FromArgb(180, 198, 231);
                ws.Column(1).Width = 32;
                ws.Column(2).Width = 100;
                for (int i = 1; i <= 9; i++) ws.Row(i).Height = 22;
                ws.Cell("A1").SetActive();
            }
            catch { }
        }
        // Chỉ nhúng DẤU VÂN TAY Token vào Excel (đủ để đối chiếu nguồn gốc tệp), không nhúng chuỗi Token đã mã hóa
        // vì ai có phần mềm cũng giải mã được chuỗi đó ra Token gốc.
        private static string DocTokenAnToan()
        {
            try { return Module_TokenCSDL.LayDauVanTay(); }
            catch { return ""; }
        }
    }
}