using Microsoft.Data.Sqlite;

namespace PhanMemThiDua2026
{
    internal static class Module_NamHeThong
    {
        // Lấy đường dẫn chung toàn hệ thống
        private static string DbPath => Module_DanduongGPS.DuongDanCSDL2;

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