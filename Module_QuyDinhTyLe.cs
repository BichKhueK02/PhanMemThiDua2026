using Microsoft.Data.Sqlite;
namespace PhanMemThiDua2026
{
    internal static class Module_QuyDinhTyLe
    {
        // BIẾN TOÀN CỤC – FORM4 DÙNG TRỰC TIẾP
        //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
        public static Dictionary<string, int[]> DeNghiMapping { get; private set; }
            = new Dictionary<string, int[]>();
        private static readonly int[] IDS = { 1, 2, 3 };
        // LOAD E29: CSDL → TextBox Form12 + Dictionary
        // HÀM TRỢ GIÚP ĐỂ LẤY TÊN BẢNG LINH HOẠT
        private static string TenBangHienTai
        {
            get
            {
                string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
                return phienBan.LaCheDoTanBinh()
                    ? "QuyDinhTyLe_TanBinh"
                    : "QuyDinhTyLe";
            }
        }
        /// <summary>Kết quả đọc E29 (không chứa control nào: tạo được ở luồng nền).</summary>
        internal sealed class KetQuaE29
        {
            public required Dictionary<string, int[]> Mapping { get; init; }
            public required bool[] CoDong { get; init; }
        }
        /// <summary>Gán kết quả E29 cho DeNghiMapping (thay nguyên bảng 1 lần: nơi đọc không bao giờ thấy bảng đang dở).</summary>
        public static void ApDungE29(KetQuaE29? kq)
        {
            if (kq != null) DeNghiMapping = kq.Mapping;
        }
        /// <summary>
        /// Phần CSDL của LoadE29, KHÔNG đụng giao diện -> gọi được trong Task.Run.
        /// Form4 gọi hàm này ở luồng nền: trước đây LoadE29 chạy ngay trong Form4_Load trên luồng giao diện
        /// (mở SQLite 2 lần + 1 giao dịch GHI CREATE/INSERT + 15 lần Controls.Find đệ quy không tìm thấy gì)
        /// đúng lúc Form2 đang chờ hiện sau khi bấm Đăng nhập.
        /// </summary>
        public static KetQuaE29? DocE29()
        {
            string csdlPath = Module_DanduongGPS.DuongDanCSDL2;
            if (string.IsNullOrWhiteSpace(csdlPath)) return null;
            bool[] coDong = new bool[IDS.Length];
            int[] loai1 = new int[3];
            int[] loai2 = new int[3];
            int[] loai3 = new int[3];
            int[] loai4 = new int[3];
            int[] khongPL = new int[3];
            // Dùng Mode=ReadWriteCreate để cho phép tự động tạo bảng nếu chưa có
            using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(csdlPath));
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
                coDong[row] = true;
            }
            return new KetQuaE29
            {
                CoDong = coDong,
                Mapping = new Dictionary<string, int[]>
                {
                    ["Loai1_TapThe"] = loai1,
                    ["Loai2_TapThe"] = loai2,
                    ["Loai3_TapThe"] = loai3,
                    ["Loai4_TapThe"] = loai4,
                    ["KhongPL_TapThe"] = khongPL,
                }
            };
        }
        public static void ReloadData()
        {
            string csdlPath = Module_DanduongGPS.DuongDanCSDL2;
            if (string.IsNullOrWhiteSpace(csdlPath)) return;
            DeNghiMapping.Clear();
            int[] loai1 = new int[3];
            int[] loai2 = new int[3];
            int[] loai3 = new int[3];
            int[] loai4 = new int[3];
            int[] khongPL = new int[3];
            using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(csdlPath));
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
        public static int[] GetLoaiTapThe(string key)
                {
                    if (DeNghiMapping.Count == 0)
                        ReloadData();
                    return DeNghiMapping.TryGetValue(key, out var v)
                        ? v
                        : new int[] { 0, 0, 0, 0, 0 };
                }
    }
}
