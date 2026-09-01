using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PhanMemThiDua2026
{
    internal static class Module_HeThong
    {
        // 1. Dùng chung Font cho toàn hệ thống
        public const string TenFontHeThong = "Segoe UI";
        public const string Font_Times_New_Roman = "Times New Roman";
        private const int ID_NAM_HE_THONG = 1;
        public const string TT_DANG_CONG_TAC = "Đang công tác";
        public const string TT_CHUYEN_CONG_TAC = "Chuyển công tác";
        public const string Tu_Dong_Chi = "Đồng chí";
        public const string Tu_dong_chi = "đồng chí";
        public const string Goi_Y_Thao_Tac = "Gợi ý thao tác";
        public const string PL_CSTD = "CSTĐ";
        public const string PL_CSTT = "CSTT";
        public const string PL_HTNV = "HTNV";
        public const string PL_KHTNV = "KHTNV";
        public const string PL_KHONG_PL = "Không PL";
        public const string Loai_1 = "Loại 1";
        public const string Loai_2 = "Loại 2";
        public const string Loai_3 = "Loại 3";
        public const string Loai_4 = "Loại 4";
        // Tên cột thực tế trong SQLite
        public const string COL_LOAI_1 = "Loai_1";
        public const string COL_LOAI_2 = "Loai_2";
        public const string COL_LOAI_3 = "Loai_3";
        public const string COL_LOAI_4 = "Loai_4";
        public const string Tat_Ca = "Tất cả";
        private static string DbPath => Module_DanduongGPS.DuongDanCSDL2;

        // Mảng chuẩn 5 loại (Dành cho Form46_ThongKeThiDuaNamCu - CSDL Năm)
        public static readonly object[] DanhSach_PhanLoai_Chuan =
        {
            PL_CSTD, PL_CSTT, PL_HTNV, PL_KHTNV, PL_KHONG_PL
        };

        // Mảng có thêm chuỗi rỗng ở đầu (Dành cho Form22, Form30)
        public static readonly object[] DanhSach_PhanLoai_CoRong =
        {
            "", PL_CSTD, PL_CSTT, PL_HTNV, PL_KHTNV
        };

        // Mảng có thêm "Tất cả" (Dành cho Form46 - Bộ lọc tìm kiếm)
        public static readonly object[] DanhSach_PhanLoai_TatCa =
        {
            "Tất cả", PL_CSTD, PL_CSTT, PL_HTNV, PL_KHTNV, PL_KHONG_PL
        };
        // ==========================================

        // ⭐ WINDOWS SHELL API VÀ QUẢN LÝ GDI HANDLE
        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        // Lê Trung Kiên -  Yêu mèo cam 🐈
        private const int SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;

        /// <summary>
        /// Wrapper đóng gói lời gọi API Windows Explorer để dễ quản lý và bắt lỗi
        /// </summary>
        private static void LamMoiExplorer()
        {
            try
            {
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi Shell API]: {ex.Message}");
            }
        }

        // ⭐ GÁN ICON TÙY BIẾN TỪ RESOURCES VÀO THƯ MỤC XUẤT FILE       
        public static void GanIconThuMuc(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return;

            IntPtr hIcon = IntPtr.Zero;
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
                        using var fs = new FileStream(iconPath, FileMode.Create, FileAccess.Write, FileShare.None);
                        ico.Save(fs);
                    }
                    else if (resObj is byte[] bytes)
                    {
                        File.WriteAllBytes(iconPath, bytes);
                    }
                    else if (resObj is Bitmap bmp)
                    {
                        // Kiểm soát vòng đời HICON chuẩn kỹ sư, tránh rò rỉ GDI
                        hIcon = bmp.GetHicon();
                        using var tempIcon = Icon.FromHandle(hIcon);
                        using var fs = new FileStream(iconPath, FileMode.Create, FileAccess.Write, FileShare.None);
                        tempIcon.Save(fs);
                    }

                    if (File.Exists(iconPath))
                        File.SetAttributes(iconPath, FileAttributes.Hidden | FileAttributes.System);
                }

                // 2. Tạo/ghi đè desktop.ini an toàn (Dùng ASCII để tránh xung đột Encoding Locale)
                if (File.Exists(iniPath))
                    File.SetAttributes(iniPath, FileAttributes.Normal);

                string iniContent = "[.ShellClassInfo]\r\n" +
                                    "IconResource=IconThuMuc.ico,0\r\n" +
                                    "[ViewState]\r\n" +
                                    "Mode=\r\n" +
                                    "Vid=\r\n" +
                                    "FolderType=Generic\r\n";

                File.WriteAllText(iniPath, iniContent, Encoding.ASCII);
                File.SetAttributes(iniPath, FileAttributes.Hidden | FileAttributes.System);

                // 3. Gán cờ ReadOnly cho thư mục để Windows tiến hành đọc desktop.ini
                var folderInfo = new DirectoryInfo(folderPath);
                folderInfo.Attributes |= FileAttributes.ReadOnly;

                // 4. Báo hiệu Explorer cập nhật giao diện
                LamMoiExplorer();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi gán icon thư mục]: {ex.Message}");
            }
            finally
            {
                // Giải phóng dứt điểm handle native nếu có sử dụng
                if (hIcon != IntPtr.Zero)
                    DestroyIcon(hIcon);
            }
        }

        // ==========================================
        // 1. Lấy năm hệ thống
        // ==========================================
        public static int LayNamHeThong()
        {
            try
            {
                if (!File.Exists(DbPath))
                    return DateTime.Now.Year;

                using var conn = new SqliteConnection($"Data Source={DbPath}");
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT NAM 
                    FROM NamHeThong 
                    WHERE ID = @id 
                    LIMIT 1;";
                cmd.Parameters.AddWithValue("@id", ID_NAM_HE_THONG);

                object result = cmd.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                {
                    int nam = Convert.ToInt32(result);
                    // Đảm bảo dữ liệu trong CSDL luôn hợp lý trước khi nạp vào hệ thống
                    if (nam >= 2000 && nam <= 2100)
                        return nam;
                }
            }
            catch (Exception ex)
            {
                // Fallback mượt mà khi CSDL khóa hoặc hỏng
                Debug.WriteLine($"[Lỗi CSDL - LayNamHeThong]: {ex.Message}");
            }

            return DateTime.Now.Year;
        }
        // 2. Lưu năm hệ thống
        /// Yêu Mèo Cam 🐈
        public static void LuuNamHeThong(int nam)
        {
            // Chặn dữ liệu rác/lỗi từ đầu vào
            if (nam < 2000 || nam > 2100)
                throw new ArgumentOutOfRangeException(nameof(nam), "Năm hệ thống không hợp lệ (Giới hạn: 2000 - 2100).");

            try
            {
                using var conn = new SqliteConnection($"Data Source={DbPath}");
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT OR REPLACE INTO NamHeThong (ID, NAM)
                    VALUES (@id, @nam);";

                cmd.Parameters.AddWithValue("@id", ID_NAM_HE_THONG);
                cmd.Parameters.AddWithValue("@nam", nam);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi CSDL - LuuNamHeThong]: {ex.Message}");
                throw new Exception($"Lỗi gián đoạn khi lưu năm hệ thống: {ex.Message}", ex);
            }
        }
        // 3. Lấy danh sách biên độ năm
        public static List<int> LayDanhSachNam(int bienDo = 5)
        {
            int namTrungTam = LayNamHeThong();
            var ds = new List<int>(bienDo * 2 + 1); // Cấp phát tĩnh trước để tối ưu phân bổ vùng nhớ

            int min = namTrungTam - bienDo;
            int max = namTrungTam + bienDo;

            for (int i = min; i <= max; i++)
            {
                ds.Add(i);
            }

            return ds;
        }
    }
}