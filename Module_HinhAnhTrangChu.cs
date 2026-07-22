using Microsoft.Data.Sqlite;

namespace PhanMemThiDua2026
{
    public static class Module_HinhAnhTrangChu
    {
        private static int _indexHienTai = 1;
        private static readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;       
        // 1. TẦNG LOGIC HÌNH ẢNH (CHẠY NGẦM)
        public static Image LayHinhTiepTheo()
        {
            _indexHienTai++;
            if (_indexHienTai > 5) _indexHienTai = 1; // Vòng lặp liên tục từ 1 -> 5
            return TrichXuatHinhAnToan(_indexHienTai);
        }

        public static Image LayHinhMacDinh()
        {
            return TrichXuatHinhAnToan(1); // Mặc định luôn lấy hình gốc số 1
        }

        private static Image TrichXuatHinhAnToan(int index)
        {
            string name = $"desktop_{index}";
            try
            {
                object resourceObj = Properties.Resources.ResourceManager.GetObject(name);
                if (resourceObj is Image img)
                {
                    // CLONE cực kỳ quan trọng để chống lỗi RAM khi Dispose ở Form2
                    return (Image)img.Clone();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi trích xuất hình nền]: {ex.Message}");
            }
            return null;
        }   
        // 2. TẦNG TRUY XUẤT CƠ SỞ DỮ LIỆU (CSDL 2)
        public static string DocCauHinhThoiGian()
        {
            try
            {
                using var cn = new SqliteConnection($"Data Source={_csdl2Path}");
                cn.Open();

                // Đảm bảo bảng luôn tồn tại đúng cấu trúc
                using (var cmdTaoBang = new SqliteCommand(@"
                    CREATE TABLE IF NOT EXISTS ""CaiDat_AnhDaiDien"" (
                        ""ID"" INTEGER NOT NULL,
                        ""HinhAnh"" TEXT,
                        PRIMARY KEY(""ID"" AUTOINCREMENT)
                    );", cn))
                {
                    cmdTaoBang.ExecuteNonQuery();
                }

                // Chỉ đọc duy nhất dòng ID = 1
                using var cmdDoc = new SqliteCommand("SELECT HinhAnh FROM CaiDat_AnhDaiDien WHERE ID = 1", cn);
                var kq = cmdDoc.ExecuteScalar();

                if (kq != null && !string.IsNullOrWhiteSpace(kq.ToString()))
                {
                    return kq.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi đọc cài đặt hình: {ex.Message}");
            }

            return "Mặc định"; // Nếu lỗi hoặc CSDL rỗng thì trả về cấu hình an toàn
        }
        public static void LuuCauHinhThoiGian(string giaTriDuocChon)
        {
            try
            {
                using var cn = new SqliteConnection($"Data Source={_csdl2Path}");
                cn.Open();

                // ⭐ TỐI ƯU HÓA: Dùng UPSERT của SQLite
                // Chỉ dùng đúng 1 câu lệnh để vừa Insert vừa Update ở đúng dòng ID = 1
                using var cmdLuu = new SqliteCommand(@"
                    INSERT INTO CaiDat_AnhDaiDien (ID, HinhAnh) 
                    VALUES (1, @val) 
                    ON CONFLICT(ID) DO UPDATE SET HinhAnh = @val;", cn);

                cmdLuu.Parameters.AddWithValue("@val", giaTriDuocChon);
                cmdLuu.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi lưu cài đặt hình: {ex.Message}");
            }
        }
    }
}