using Microsoft.Data.Sqlite;
namespace PhanMemThiDua2026
{
    internal static class Module_DonVi
    {
        //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
        private static List<string>? _cacheDonVi;
        // Bộ đệm nạp từ bảng nào / CSDL nào: đổi phiên bản (DanhSach_DonVi <-> DanhSach_DonVi_TanBinh) hoặc đổi CSDL thì tự đọc lại.
        // (Trước đây đổi CBCS -> Tân binh vẫn trả đơn vị CBCS => Form26/Form15/Form4 không khớp đơn vị nào.)
        private static string _cacheKhoa = "";
        // ⭐ HÀM MỚI: TỰ ĐỘNG NHẬN DIỆN BẢNG THEO PHIÊN BẢN PHẦN MỀM
        public static string LayTenBangDonVi()
        {
            bool laTanBinh = false;
            try
            {
                laTanBinh = Module_TaiKhoan.LaPhienBanTanBinh();
            }
            catch { }
            return laTanBinh ? "DanhSach_DonVi_TanBinh" : "DanhSach_DonVi";
        }
        // LẤY DANH SÁCH ĐƠN VỊ (CHO COMBOBOX / SORT / BIẾN)
        public static List<string> GetDanhSachDonVi(bool forceReload = false)
        {
            string csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
            string tenBang = LayTenBangDonVi(); // Gọi hàm tự nhận diện bảng
            string khoa = tenBang + "|" + csdl2Path;
            var cache = _cacheDonVi;
            if (cache != null && !forceReload && khoa == _cacheKhoa)
                return new List<string>(cache);
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(csdl2Path)) return result;
            try
            {
                using var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(csdl2Path));
                cn.Open();
                using var cmd = cn.CreateCommand();
                cmd.CommandText = $"SELECT Ten_DonVi FROM {tenBang} ORDER BY ID ASC";
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    string val = rd["Ten_DonVi"]?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        try
                        {
                            val = Module_BaoMatAES.GiaiMa(val); // giải mã nếu cần
                        }
                        catch { }
                        result.Add(val);
                    }
                }
                _cacheKhoa = khoa;
                _cacheDonVi = result;
            }
            catch
            {
                _cacheKhoa = "";   // lỗi đọc: lần sau thử lại
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
    }
}
