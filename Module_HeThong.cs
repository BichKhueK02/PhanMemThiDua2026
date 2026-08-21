using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace PhanMemThiDua2026
{
    internal static class Module_HeThong
    {
        // Tên Font chuẩn dùng chung cho toàn hệ thống
        public const string TenFontHeThong = "Segoe UI";

        // Lấy đường dẫn chung toàn hệ thống
        private static string DbPath => Module_DanduongGPS.DuongDanCSDL2;

        // =========================================================================
        // ⭐ WINDOWS SHELL API: BẮN TÍN HIỆU CẬP NHẬT CACHE ICON EXPLORER
        // =========================================================================
        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private const int SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;

        // =========================================================================
        // ⭐ GÁN ICON TÙY BIẾN TỪ RESOURCES VÀO THƯ MỤC XUẤT FILE
        // =========================================================================
        public static void GanIconThuMuc(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return;

            try
            {
                string iconPath = Path.Combine(folderPath, "IconThuMuc.ico");
                string iniPath = Path.Combine(folderPath, "desktop.ini");

                // 1. Trích xuất file IconThuMuc.ico từ Resources
                if (!File.Exists(iconPath))
                {
                    object resObj = Properties.Resources.IconThuMuc;

                    if (resObj is Icon ico)
                    {
                        using var fs = new FileStream(iconPath, FileMode.Create, FileAccess.Write);
                        ico.Save(fs);
                    }
                    else if (resObj is byte[] bytes)
                    {
                        File.WriteAllBytes(iconPath, bytes);
                    }
                    else if (resObj is Bitmap bmp)
                    {
                        // Nếu Visual Studio nhận diện nhầm file ảnh là Bitmap -> Chuyển đổi an toàn thành Icon
                        IntPtr hIcon = bmp.GetHicon();
                        using (Icon tempIcon = Icon.FromHandle(hIcon))
                        {
                            using var fs = new FileStream(iconPath, FileMode.Create, FileAccess.Write);
                            tempIcon.Save(fs);
                        }
                    }

                    // Ẩn tệp icon
                    if (File.Exists(iconPath))
                    {
                        File.SetAttributes(iconPath, FileAttributes.Hidden | FileAttributes.System);
                    }
                }

                // 2. Tạo/ghi đè desktop.ini
                if (File.Exists(iniPath))
                {
                    File.SetAttributes(iniPath, FileAttributes.Normal);
                }

                string iniContent = "[.ShellClassInfo]\r\n" +
                                    "IconResource=IconThuMuc.ico,0\r\n" +
                                    "[ViewState]\r\n" +
                                    "Mode=\r\n" +
                                    "Vid=\r\n" +
                                    "FolderType=Generic\r\n";

                File.WriteAllText(iniPath, iniContent, System.Text.Encoding.Default);
                File.SetAttributes(iniPath, FileAttributes.Hidden | FileAttributes.System);

                // 3. Gán cờ ReadOnly cho thư mục
                var folderInfo = new DirectoryInfo(folderPath);
                folderInfo.Attributes |= FileAttributes.ReadOnly;

                // 4. Cập nhật icon Windows Explorer
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi gán icon thư mục]: {ex.Message}");
            }
        }
        // ==========================================
        // 1. Lấy năm hệ thống
        // ==========================================
        public static int LayNamHeThong()
        {
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT NAM FROM NamHeThong WHERE ID = 1";

            object result = cmd.ExecuteScalar();

            if (result != null && result != DBNull.Value)
                return Convert.ToInt32(result);

            return DateTime.Now.Year;
        }

        // ==========================================
        // 2. Lưu năm hệ thống
        /// <summary>
        /// yêu Mèo Cam
        /// </summary>
        /// <param name="nam"></param>
        // ==========================================
        public static void LuuNamHeThong(int nam)
        {
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO NamHeThong (ID, NAM)
                VALUES (1, @nam);
            ";

            cmd.Parameters.AddWithValue("@nam", nam);
            cmd.ExecuteNonQuery();
        }

        // ==========================================
        // 3. Lấy danh sách năm ±5
        // ==========================================
        public static List<int> LayDanhSachNam()
        {
            int namTrungTam = LayNamHeThong();

            List<int> ds = new List<int>();

            int min = namTrungTam - 5;
            int max = namTrungTam + 5;

            for (int i = min; i <= max; i++)
            {
                ds.Add(i);
            }

            return ds;
        }
    }
}