using Microsoft.Data.Sqlite;

namespace PhanMemThiDua2026
{
    internal static class Module_QuyDinhTyLe
    {
        // ======================================================
        // BIẾN TOÀN CỤC – FORM4 DÙNG TRỰC TIẾP
        // ======================================================
        public static Dictionary<string, int[]> DeNghiMapping { get; private set; }
            = new Dictionary<string, int[]>();
        private static readonly int[] IDS = { 1, 2, 3 };
        private static readonly string[] COLS = { "Loai_1", "Loai_2", "Loai_3", "Loai_4", "Khong_PL" };
        private static readonly string[] TXT = { "A", "B", "C", "D", "E" };
        // ======================================================
        // LOAD E29: CSDL → TextBox Form12 + Dictionary
        // ======================================================
        // ======================================================
        // HÀM TRỢ GIÚP ĐỂ LẤY TÊN BẢNG LINH HOẠT
        // ======================================================
        private static string TenBangHienTai
        {
            get
            {
                string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
                return phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase)
                    ? "QuyDinhTyLe_TanBinh"
                    : "QuyDinhTyLe";
            }
        }

        // ======================================================
        // LOAD E29: CSDL → TextBox Form12 + Dictionary
        // ======================================================
        public static void LoadE29(Control.ControlCollection controls)
        {
            string csdlPath = Module_DanduongGPS.DuongDanCSDL2;
            if (string.IsNullOrWhiteSpace(csdlPath)) return;

            DeNghiMapping.Clear();

            int[] loai1 = new int[3];
            int[] loai2 = new int[3];
            int[] loai3 = new int[3];
            int[] loai4 = new int[3];
            int[] khongPL = new int[3];

            // Dùng Mode=ReadWriteCreate để cho phép tự động tạo bảng nếu chưa có
            using var conn = new SqliteConnection($"Data Source={csdlPath};Mode=ReadWriteCreate");
            conn.Open();

            string tableName = TenBangHienTai;

            // Đảm bảo bảng luôn tồn tại trước khi đọc (Chống lỗi văng phần mềm)
            using (var cmdCreate = conn.CreateCommand())
            {
                cmdCreate.CommandText = $@"
                CREATE TABLE IF NOT EXISTS [{tableName}] (
                    ID INTEGER NOT NULL,
                    TenLoaiTapThe TEXT,
                    Loai_1 TEXT,
                    Loai_2 TEXT,
                    Loai_3 TEXT,
                    Loai_4 TEXT,
                    Khong_PL TEXT,
                    PRIMARY KEY(ID AUTOINCREMENT)
                );
                INSERT OR IGNORE INTO [{tableName}] (ID, TenLoaiTapThe, Loai_1, Loai_2, Loai_3, Loai_4, Khong_PL) 
                VALUES 
                (1, 'Loại 1', '0', '0', '0', '0', '0'),
                (2, 'Loại 2', '0', '0', '0', '0', '0'),
                (3, 'Loại 3', '0', '0', '0', '0', '0');";
                cmdCreate.ExecuteNonQuery();
            }

            for (int row = 0; row < IDS.Length; row++)
            {
                int id = IDS[row];

                using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"SELECT Loai_1,Loai_2,Loai_3,Loai_4,Khong_PL 
                            FROM [{tableName}] WHERE ID=@id";
                cmd.Parameters.AddWithValue("@id", id);

                using var rd = cmd.ExecuteReader();
                if (!rd.Read()) continue;

                loai1[row] = int.TryParse(rd["Loai_1"]?.ToString(), out int v1) ? v1 : 0;
                loai2[row] = int.TryParse(rd["Loai_2"]?.ToString(), out int v2) ? v2 : 0;
                loai3[row] = int.TryParse(rd["Loai_3"]?.ToString(), out int v3) ? v3 : 0;
                loai4[row] = int.TryParse(rd["Loai_4"]?.ToString(), out int v4) ? v4 : 0;
                khongPL[row] = int.TryParse(rd["Khong_PL"]?.ToString(), out int v5) ? v5 : 0;

                if (controls.Find($"textBox_A{id}", true).FirstOrDefault() is TextBox a) a.Text = loai1[row].ToString();
                if (controls.Find($"textBox_B{id}", true).FirstOrDefault() is TextBox b) b.Text = loai2[row].ToString();
                if (controls.Find($"textBox_C{id}", true).FirstOrDefault() is TextBox c) c.Text = loai3[row].ToString();
                if (controls.Find($"textBox_D{id}", true).FirstOrDefault() is TextBox d) d.Text = loai4[row].ToString();
                if (controls.Find($"textBox_E{id}", true).FirstOrDefault() is TextBox e) e.Text = khongPL[row].ToString();
            }

            DeNghiMapping["Loai1_TapThe"] = loai1;
            DeNghiMapping["Loai2_TapThe"] = loai2;
            DeNghiMapping["Loai3_TapThe"] = loai3;
            DeNghiMapping["Loai4_TapThe"] = loai4;
            DeNghiMapping["KhongPL_TapThe"] = khongPL;
        }

        public static void Reload()
        {
            string csdlPath = Module_DanduongGPS.DuongDanCSDL2;
            if (string.IsNullOrWhiteSpace(csdlPath)) return;

            DeNghiMapping.Clear();

            int[] loai1 = new int[3];
            int[] loai2 = new int[3];
            int[] loai3 = new int[3];
            int[] loai4 = new int[3];
            int[] khongPL = new int[3];

            using var conn = new SqliteConnection($"Data Source={csdlPath};Mode=ReadWriteCreate");
            conn.Open();

            string tableName = TenBangHienTai;

            using (var cmdCreate = conn.CreateCommand())
            {
                cmdCreate.CommandText = $@"
                CREATE TABLE IF NOT EXISTS [{tableName}] (
                    ID INTEGER NOT NULL,
                    TenLoaiTapThe TEXT,
                    Loai_1 TEXT,
                    Loai_2 TEXT,
                    Loai_3 TEXT,
                    Loai_4 TEXT,
                    Khong_PL TEXT,
                    PRIMARY KEY(ID AUTOINCREMENT)
                );
                INSERT OR IGNORE INTO [{tableName}] (ID, TenLoaiTapThe, Loai_1, Loai_2, Loai_3, Loai_4, Khong_PL) 
                VALUES 
                (1, 'Loại 1', '0', '0', '0', '0', '0'),
                (2, 'Loại 2', '0', '0', '0', '0', '0'),
                (3, 'Loại 3', '0', '0', '0', '0', '0');";
                cmdCreate.ExecuteNonQuery();
            }

            for (int row = 0; row < 3; row++)
            {
                int id = row + 1;

                using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"SELECT Loai_1,Loai_2,Loai_3,Loai_4,Khong_PL 
                            FROM [{tableName}] WHERE ID=@id";
                cmd.Parameters.AddWithValue("@id", id);

                using var rd = cmd.ExecuteReader();
                if (!rd.Read()) continue;

                loai1[row] = int.TryParse(rd["Loai_1"]?.ToString(), out int v1) ? v1 : 0;
                loai2[row] = int.TryParse(rd["Loai_2"]?.ToString(), out int v2) ? v2 : 0;
                loai3[row] = int.TryParse(rd["Loai_3"]?.ToString(), out int v3) ? v3 : 0;
                loai4[row] = int.TryParse(rd["Loai_4"]?.ToString(), out int v4) ? v4 : 0;
                khongPL[row] = int.TryParse(rd["Khong_PL"]?.ToString(), out int v5) ? v5 : 0;
            }

            DeNghiMapping["Loai1_TapThe"] = loai1;
            DeNghiMapping["Loai2_TapThe"] = loai2;
            DeNghiMapping["Loai3_TapThe"] = loai3;
            DeNghiMapping["Loai4_TapThe"] = loai4;
            DeNghiMapping["KhongPL_TapThe"] = khongPL;
        }

        // ======================================================
        // SAVE E29: TextBox Form12 → CSDL + Dictionary
        // ======================================================
        public static void SaveE29(Control.ControlCollection controls)
        {
            string csdlPath = Module_DanduongGPS.DuongDanCSDL2;
            if (string.IsNullOrWhiteSpace(csdlPath)) return;

            DeNghiMapping.Clear();

            using var conn = new SqliteConnection($"Data Source={csdlPath}");
            conn.Open();

            string tableName = TenBangHienTai;

            for (int idx = 0; idx < IDS.Length; idx++)
            {
                int id = IDS[idx];
                int[] values = new int[5];

                for (int i = 0; i < COLS.Length; i++)
                {
                    string tbName = $"textBox_{TXT[i]}{id}";
                    TextBox tb = controls.Find(tbName, true).FirstOrDefault() as TextBox;

                    int v = int.TryParse(tb?.Text ?? "0", out int x) ? x : 0;
                    values[i] = v;

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"UPDATE [{tableName}] SET {COLS[i]}=@v WHERE ID=@id";
                    // Ép về ToString để tương thích chuẩn cột TEXT của SQLite
                    cmd.Parameters.AddWithValue("@v", v.ToString());
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }

                DeNghiMapping[$"Loai{idx + 1}_TapThe"] = values;
            }
        }
        public static int[] GetLoaiTapThe(string key)
        {
            if (DeNghiMapping.Count == 0)
                Reload();

            return DeNghiMapping.TryGetValue(key, out var v)
                ? v
                : new int[] { 0, 0, 0, 0, 0 };
        }
    }
}
