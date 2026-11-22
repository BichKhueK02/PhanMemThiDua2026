using Microsoft.Data.Sqlite;
///Phiene bản 2
///
namespace PhanMemThiDua2026
{
    public static class Module_GiaoDien
    {
        // Lưu màu hiện tại trong RAM
        public static Color MauNenMenu { get; set; } = Color.FromArgb(240, 252, 240);
        public static Color MauLeIcon { get; set; } = Color.FromArgb(215, 240, 215);
        public static Color MauHover { get; set; } = Color.FromArgb(200, 235, 200);
        /// <summary>
        /// 🚀 HÀM GỌI KHI KHỞI ĐỘNG PHẦN MỀM: Đọc cấu hình màu từ CSDL lên RAM
        /// </summary>
        public static void KhoiDongDocTheme()
        {
            try
            {
                string csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
                if (!string.IsNullOrWhiteSpace(csdl2Path) && System.IO.File.Exists(csdl2Path))
                {
                    // Mở CHỈ ĐỌC: không tạo bảng / không ghi mỗi lần khởi động (bảng do ApDungTheme tạo khi người dùng đổi màu).
                    using var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(csdl2Path, chiDoc: true));
                    cn.Open();
                    using var cmdRead = cn.CreateCommand();
                    cmdRead.CommandText = "SELECT MauSacNguoiDungChon FROM MauSacMenuHeThong WHERE ID = 1";
                    object? result;
                    try { result = cmdRead.ExecuteScalar(); }
                    catch (SqliteException) { result = null; }   // chưa có bảng → giữ màu mặc định
                    if (result != null && result != DBNull.Value)
                    {
                        string tenMau = result.ToString()!.Trim();
                        // Ánh xạ ngược lại giá trị màu trong RAM dựa theo tên đã lưu
                        // (Bạn có thể điều chỉnh lại các mã màu cho khớp với các lựa chọn thực tế của bạn)
                        switch (tenMau)
                        {
                            case "Xanh lá":
                                MauNenMenu = Color.FromArgb(240, 252, 240);
                                MauLeIcon = Color.FromArgb(215, 240, 215);
                                MauHover = Color.FromArgb(200, 235, 200);
                                break;
                            case "Xanh dương":
                                MauNenMenu = Color.FromArgb(240, 248, 255);
                                MauLeIcon = Color.FromArgb(210, 230, 250);
                                MauHover = Color.FromArgb(185, 215, 245);
                                break;
                            case "Xám trắng":
                                MauNenMenu = Color.FromArgb(245, 245, 245);
                                MauLeIcon = Color.FromArgb(230, 230, 230);
                                MauHover = Color.FromArgb(210, 210, 210);
                                break;
                            case "Hồng cánh sen":
                                MauNenMenu = Color.FromArgb(253, 236, 245);
                                MauLeIcon = Color.FromArgb(250, 214, 233);
                                MauHover = Color.FromArgb(248, 187, 217);
                                break;
                            case "Tím nhạt":
                                MauNenMenu = Color.FromArgb(243, 238, 255);
                                MauLeIcon = Color.FromArgb(228, 218, 252);
                                MauHover = Color.FromArgb(214, 198, 250);
                                break;
                            default:
                                // Giữ nguyên mặc định hoặc xử lý tùy ý
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi đọc theme từ CSDL]: {ex.Message}");
            }
        }
    }
}