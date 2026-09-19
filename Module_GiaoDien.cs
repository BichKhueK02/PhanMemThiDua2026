using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
namespace PhanMemThiDua2026
{
    public static class Module_GiaoDien
    {
        // Sự kiện báo hiệu khi đổi Theme toàn hệ thống
        public static event Action OnThemeChanged;
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
                    using var cn = new SqliteConnection($"Data Source={csdl2Path}");
                    cn.Open();
                    // Kiểm tra bảng tồn tại trước khi đọc tránh lỗi
                    using var cmdCheck = cn.CreateCommand();
                    cmdCheck.CommandText = @"CREATE TABLE IF NOT EXISTS MauSacMenuHeThong (
                                                ID INTEGER NOT NULL PRIMARY KEY, 
                                                MauSacNguoiDungChon TEXT);";
                    cmdCheck.ExecuteNonQuery();
                    // Đọc giá trị đã lưu ở ID = 1
                    using var cmdRead = cn.CreateCommand();
                    cmdRead.CommandText = "SELECT MauSacNguoiDungChon FROM MauSacMenuHeThong WHERE ID = 1";
                    var result = cmdRead.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        string tenMau = result.ToString().Trim();
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
        public static void ApDungTheme(string tenMau, Color mauNen, Color mauLe, Color mauHover)
        {
            MauNenMenu = mauNen;
            MauLeIcon = mauLe;
            MauHover = mauHover;
            try
            {
                string csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
                if (!string.IsNullOrWhiteSpace(csdl2Path) && System.IO.File.Exists(csdl2Path))
                {
                    using var cn = new SqliteConnection($"Data Source={csdl2Path}");
                    cn.Open();
                    using (var cmdCreate = cn.CreateCommand())
                    {
                        cmdCreate.CommandText = @"CREATE TABLE IF NOT EXISTS MauSacMenuHeThong (
                                                    ID INTEGER NOT NULL PRIMARY KEY, 
                                                    MauSacNguoiDungChon TEXT);";
                        cmdCreate.ExecuteNonQuery();
                    }
                    using var cmdUpdate = cn.CreateCommand();
                    cmdUpdate.CommandText = @"
                        INSERT INTO MauSacMenuHeThong (ID, MauSacNguoiDungChon)
                        VALUES (1, @mau)
                        ON CONFLICT(ID) DO UPDATE SET MauSacNguoiDungChon = excluded.MauSacNguoiDungChon;";
                    cmdUpdate.Parameters.AddWithValue("@mau", tenMau.Trim());
                    cmdUpdate.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi lưu theme vào CSDL]: {ex.Message}");
            }
            // Kích hoạt cập nhật giao diện toàn bộ Form đang mở
            OnThemeChanged?.Invoke();
        }
    }
}