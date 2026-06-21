using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhanMemThiDua2026
{
    internal static class Module_DonVi
    {
        private static List<string> _cacheDonVi;

        // =====================================================
        // LẤY DANH SÁCH ĐƠN VỊ (CHO COMBOBOX / SORT / BIẾN)
        // =====================================================
        public static List<string> GetDanhSachDonVi(bool forceReload = false)
        {
            if (_cacheDonVi != null && !forceReload)
                return new List<string>(_cacheDonVi);

            var result = new List<string>();
            string csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
            if (string.IsNullOrWhiteSpace(csdl2Path)) return result;

            try
            {
                using var cn = new SqliteConnection($"Data Source={csdl2Path}");
                cn.Open();

                using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT Ten_DonVi FROM DanhSach_DonVi ORDER BY ID ASC";

                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    string val = rd["Ten_DonVi"]?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        try
                        {
                            val = BaoMatAES.GiaiMa(val); // giải mã nếu cần
                        }
                        catch { }
                        result.Add(val);
                    }
                }

                _cacheDonVi = result;
            }
            catch
            {
                _cacheDonVi = new List<string>();
            }

            return new List<string>(_cacheDonVi);
        }
        // Trả về mảng string để dùng toàn hệ thống
        public static string[] LayDanhSachDonViUuTienArray()
        {
            // 🔥 Luôn load mới từ CSDL để chắc chắn giá trị cập nhật
            Reload();
            return GetDanhSachDonVi().ToArray();
        }
        // Xóa cache để reload nếu cần
        public static void Reload()
        {
            _cacheDonVi = null;
        }
        // HÀM KHOI TAO (giữ tương thích code cũ)
        public static void KhoiTao()
        {
            GetDanhSachDonVi();
        }

        // =====================================================
        // HÀM MỚI: CẬP NHẬT / THÊM ĐƠN VỊ → tự động reload cache
        // =====================================================
        public static void CapNhatDonVi(string tenDonVi, int? id = null)
        {
            string csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
            if (string.IsNullOrWhiteSpace(csdl2Path)) return;

            try
            {
                using var cn = new SqliteConnection($"Data Source={csdl2Path}");
                cn.Open();
                using var cmd = cn.CreateCommand();

                if (id.HasValue)
                {
                    // Update theo ID
                    cmd.CommandText = "UPDATE DanhSach_DonVi SET Ten_DonVi=@ten WHERE ID=@id";
                    cmd.Parameters.AddWithValue("@id", id.Value);
                }
                else
                {
                    // Thêm mới
                    cmd.CommandText = "INSERT INTO DanhSach_DonVi (Ten_DonVi, ThoiGian) VALUES (@ten, @tg)";
                    cmd.Parameters.AddWithValue("@tg", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                }

                cmd.Parameters.AddWithValue("@ten", BaoMatAES.MaHoa(tenDonVi));
                cmd.ExecuteNonQuery();

                // 🔥 Sau khi cập nhật, reset cache để giá trị mới được dùng ngay
                Reload();
            }
            catch
            {
                // bỏ qua lỗi
            }
        }

        // =====================================================
        // HÀM MỚI: XÓA ĐƠN VỊ → tự động reload cache
        // =====================================================
        public static void XoaDonVi(int id)
        {
            string csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
            if (string.IsNullOrWhiteSpace(csdl2Path)) return;

            try
            {
                using var cn = new SqliteConnection($"Data Source={csdl2Path}");
                cn.Open();
                using var cmd = cn.CreateCommand();
                cmd.CommandText = "DELETE FROM DanhSach_DonVi WHERE ID=@id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();

                // 🔥 Sau khi xóa, reset cache
                Reload();
            }
            catch { }
        }

    }
}
