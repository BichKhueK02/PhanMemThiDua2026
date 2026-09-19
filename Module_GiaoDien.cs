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
        public static void ApDungTheme(string tenMau, Color mauNen, Color mauLe, Color mauHover)
        {
            MauNenMenu = mauNen;
            MauLeIcon = mauLe;
            MauHover = mauHover;
            // 🌟 ĐÃ LOẠI BỎ Properties.Settings. SỬ DỤNG CSDL ĐỂ LƯU VĨNH VIỄN
            try
            {
                string csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
                if (!string.IsNullOrWhiteSpace(csdl2Path) && System.IO.File.Exists(csdl2Path))
                {
                    using var cn = new SqliteConnection($"Data Source={csdl2Path}");
                    cn.Open();
                    // Đảm bảo bảng tồn tại
                    using (var cmdCreate = cn.CreateCommand())
                    {
                        cmdCreate.CommandText = @"CREATE TABLE IF NOT EXISTS MauSacMenuHeThong (
                                                    ID INTEGER NOT NULL PRIMARY KEY, 
                                                    MauSacNguoiDungChon TEXT);";
                        cmdCreate.ExecuteNonQuery();
                    }
                    // Lưu hoặc cập nhật màu
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