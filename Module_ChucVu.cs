using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace PhanMemThiDua2026
{
    internal static class Module_ChucVu
    {
        public static List<string> GetDanhSachChucVu()
        {
            var list = new List<string>();

            try
            {
                // Sử dụng chuỗi kết nối an toàn (ReadOnly để tránh khóa file không cần thiết)
                string connStr = $"Data Source={Module_DanduongGPS.DuongDanCSDL2};Mode=ReadOnly;Pooling=True;";

                using var cn = new SqliteConnection(connStr);
                cn.Open();

                using var cmd = cn.CreateCommand();
                cmd.CommandText = @"SELECT Ten_ChucVu FROM DanhSach_ChucVu ORDER BY ID ASC";

                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    list.Add(rd.GetString(0));
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi vào hệ thống nhật ký thay vì để ứng dụng crash
                Debug.WriteLine($"[Module_ChucVu Error]: {ex.Message}");
            }

            return list;
        }
    }
}
