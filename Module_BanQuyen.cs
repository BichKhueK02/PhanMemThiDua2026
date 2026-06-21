using ClosedXML.Excel;
using Microsoft.Data.Sqlite;

namespace PhanMemThiDua2026
{
    public static class Module_BanQuyen
    {
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
        public static void DongDauExcel(XLWorkbook workbook)
        {
            if (workbook == null) return;
            try
            {
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
                bool laTanBinh = phienBanHienTai.Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                string textPhienBan = laTanBinh ? "Phần mềm dành cho Tân binh" : "Phần mềm dành cho CBCS";

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
                    BaoMatAES.MaHoa(textPhienBan) // ⭐ Mã hóa ô B9
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
                ws.Column(2).Width = 42;

                for (int i = 1; i <= 9; i++) ws.Row(i).Height = 22;

                ws.Cell("A1").SetActive();
            }
            catch { }
        }
        private static string DocTokenAnToan()
        {
            try
            {
                string dbPath = Module_DanduongGPS.DuongDanCSDL1;
                if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return "";
                using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;Pooling=True;Cache=Shared;");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Token FROM Admin WHERE ID = 1 LIMIT 1";
                object result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value) return "";
                string token = result.ToString();
                if (string.IsNullOrWhiteSpace(token)) return "";
                if (token.Length > 200) token = token.Substring(0, 200);
                return token;
            }
            catch { return ""; }
        }
    }
}