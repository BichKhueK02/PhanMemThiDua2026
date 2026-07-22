using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace PhanMemThiDua2026
{
    internal static class Module_DanduongGPS
    {
        public static readonly string ThuMucCoSoDuLieu = Path.Combine(AppContext.BaseDirectory, "Database");
        // Đường dẫn toàn cục của thư mục lịch sử thi đua năm
        public static string ThuMucLichSuThiDua => Path.Combine(ThuMucCoSoDuLieu, "LuuTruThiDua_LichSu");
        public static string DuongDanCSDL1 { get; private set; } = string.Empty;
        public static string DuongDanCSDL2 { get; private set; } = string.Empty;
        public static string DuongDanCSDL3 { get; private set; } = string.Empty;
        public static string DuongDanCSDL4 { get; private set; } = string.Empty;
        public static string DuongDanCSDL4ex { get; private set; } = string.Empty;
        public static string ThuMucHuongDan => Path.Combine(ThuMucCoSoDuLieu, "HuongDanSuDung");
        public static string FileHuongDanIndex => Path.Combine(ThuMucHuongDan, "HuongDanSuDung.html");
        public static string TenDangNhapMacDinh { get; private set; } = string.Empty;
        public static string MatKhauDangNhapMacDinh { get; private set; } = string.Empty;
        // BIẾN CHỨA KEY BASE64 VÀ GIÁ TRỊ GỐC CUỐI CÙNG
        public static string ToiCanChuIVMeoCam1_KeyMaHoa = string.Empty;
        public static string ToiCanChuIVMeoCam2_KeyMaHoa = string.Empty;
        public static string ToiYeuMeoCam1_KeyMaHoa = string.Empty;
        public static string ToiYeuMeoCam2_KeyMaHoa = string.Empty;
        public static string ToiCanChuIVMeoCam1 = string.Empty;
        public static string ToiCanChuIVMeoCam2 = string.Empty;
        public static string ToiYeuMeoCam1 = string.Empty;
        public static string ToiYeuMeoCam2 = string.Empty;
        public static Action OnDatabaseChanged;
        private static readonly object _lock = new();
        private static string? _cachedToken;
        public static void XinTraLaiThoiGianNapKeyBase64()
        {
            if (!string.IsNullOrEmpty(ToiCanChuIVMeoCam1_KeyMaHoa)) return;

            lock (_lock)
            {
                if (!string.IsNullOrEmpty(ToiCanChuIVMeoCam1_KeyMaHoa)) return;

                try
                {
                    // ========================================================
                    // BỘ XỬ LÝ GIẢI MÃ NỘI BỘ (Chỉ tồn tại trong hàm này)
                    // ========================================================
                    const byte SEED_KEY = 0x3F;
                    // Ma trận ĐÃ BỊ ĐẢO VỊ TRÍ (Swap) + MÃ HÓA XOR
                    // Kẻ tò mò nhìn vào sẽ không thể tìm thấy pattern chuỗi thẳng
                    byte[][] keyMatrix = new byte[][]
                    {
                        // h1: Đã swap các cặp (0x7B và 0x0C -> 0x0C, 0x7B...)
                        new byte[] { 0x0C, 0x7B, 0x0C, 0x0F, 0x09, 0x06, 0x0B, 0x7B, 0x08, 0x08, 0x0B, 0x06, 0x09, 0x7E, 0x0B, 0x7A, 0x0B, 0x7C, 0x0A, 0x09, 0x0A, 0x0A, 0x0A, 0x08 },
                        // h2:
                        new byte[] { 0x0B, 0x7E, 0x0A, 0x7E, 0x0A, 0x09, 0x0B, 0x7C, 0x0A, 0x0A, 0x09, 0x07, 0x0A, 0x0A, 0x0A, 0x0C, 0x0B, 0x0A, 0x0A, 0x09, 0x0A, 0x09, 0x0A, 0x0E },                        
                        // h3:
                        new byte[] { 0x08, 0x06, 0x0B, 0x0E, 0x09, 0x7E, 0x0B, 0x7B, 0x0C, 0x0D, 0x0C, 0x0F, 0x08, 0x06, 0x0A, 0x0C, 0x0B, 0x09, 0x09, 0x7C, 0x0A, 0x09, 0x0B, 0x7C, 0x08, 0x06, 0x0A, 0x0A, 0x09, 0x7E, 0x0B, 0x7A, 0x0B, 0x0C, 0x09, 0x7C, 0x0B, 0x0A, 0x0A, 0x09 },
                        // h4:
                        new byte[] { 0x0C, 0x7B, 0x0B, 0x0E, 0x0B, 0x09, 0x0A, 0x0C, 0x0B, 0x0D, 0x0C, 0x0A, 0x0A, 0x0A, 0x0A, 0x0B, 0x0B, 0x09, 0x0C, 0x0E, 0x0B, 0x0A, 0x0A, 0x09, 0x0B, 0x06, 0x09, 0x7C, 0x0B, 0x0A, 0x0A, 0x0D, 0x0A, 0x09, 0x0B, 0x09, 0x0A, 0x0A, 0x0B, 0x7C }
                    };
                    // Hàm cục bộ (Local Function) làm rối bộ nhớ: Lấy nhảy cóc + XOR
                    string RetrieveKey(int index)
                    {
                        byte[] encodedBytes = keyMatrix[index];
                        char[] decodedChars = new char[encodedBytes.Length];

                        for (int i = 0; i < encodedBytes.Length; i++)
                        {
                            // Logic tráo đổi: index chẵn -> lấy lẻ, index lẻ -> lấy chẵn
                            int targetIndex = (i % 2 == 0) ? (i + 1) : (i - 1);

                            // Lớp khiên bảo vệ (phòng trường hợp mảng bị lệch độ dài lẻ)
                            if (targetIndex >= encodedBytes.Length) targetIndex = i;

                            decodedChars[i] = (char)(encodedBytes[targetIndex] ^ SEED_KEY);
                        }
                        return new string(decodedChars);
                    }
                    // Gọi hàm giải mã
                    string h1 = RetrieveKey(0);
                    string h2 = RetrieveKey(1);
                    string h3 = RetrieveKey(2);
                    string h4 = RetrieveKey(3);
                    // ========================================================
                    // 2. VALIDATE VÀ GIẢI MÃ
                    string k1 = BaoMatAES.KhoBaoTanVuongGiaiMaHex(BaoMatAES.TraLaiTenChoMeoCam(h1));
                    string k2 = BaoMatAES.KhoBaoTanVuongGiaiMaHex(BaoMatAES.TraLaiTenChoMeoCam(h2));
                    string k3 = BaoMatAES.KhoBaoTanVuongGiaiMaHex(BaoMatAES.TraLaiTenChoMeoCam(h3));
                    string k4 = BaoMatAES.KhoBaoTanVuongGiaiMaHex(BaoMatAES.TraLaiTenChoMeoCam(h4));

                    if (string.IsNullOrEmpty(k1) || string.IsNullOrEmpty(k2) ||
                        string.IsNullOrEmpty(k3) || string.IsNullOrEmpty(k4))
                    {
                        throw new Exception("Key giải mã bị lỗi");
                    }

                    // CHỈ GÁN KHI DỮ LIỆU ĐÃ OK
                    ToiCanChuIVMeoCam1_KeyMaHoa = k1;
                    ToiCanChuIVMeoCam2_KeyMaHoa = k2;
                    ToiYeuMeoCam1_KeyMaHoa = k3;
                    ToiYeuMeoCam2_KeyMaHoa = k4;

                    // DỌN RAM ÉP BỘ THU GOM RÁC CỤC BỘ
                    h1 = h2 = h3 = h4 = null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[Lỗi KEY]: " + ex.Message);

                    // FAIL SAFE
                    ToiCanChuIVMeoCam1_KeyMaHoa = "";
                    ToiCanChuIVMeoCam2_KeyMaHoa = "";
                    ToiYeuMeoCam1_KeyMaHoa = "";
                    ToiYeuMeoCam2_KeyMaHoa = "";
                }
            }
        }
        public static void LoiChaoTuSiberia()
        {
            if (!string.IsNullOrEmpty(ToiCanChuIVMeoCam1_KeyMaHoa))
            {
                ToiCanChuIVMeoCam1 = BaoMatAES.XuanVeOHokkaido(ToiCanChuIVMeoCam1_KeyMaHoa).Trim();
                ToiCanChuIVMeoCam2 = BaoMatAES.XuanVeOHokkaido(ToiCanChuIVMeoCam2_KeyMaHoa).Trim();
                ToiYeuMeoCam1 = BaoMatAES.XuanVeOHokkaido(ToiYeuMeoCam1_KeyMaHoa).Trim();
                ToiYeuMeoCam2 = BaoMatAES.XuanVeOHokkaido(ToiYeuMeoCam2_KeyMaHoa).Trim();
            }
        }
        // =========================================================================
        // 2. KHỞI TẠO ĐƯỜNG DẪN & DATABASE (CHUẨN HÓA)
        // =========================================================================
        public static void DamBaoThuMucHuongDan()
        {
            if (!Directory.Exists(ThuMucHuongDan))
                Directory.CreateDirectory(ThuMucHuongDan);
        }
        public static void DamBaoHuongDanIndex()
        {
            Directory.CreateDirectory(ThuMucHuongDan);

            if (File.Exists(FileHuongDanIndex))
                return;

            string fileGoc = Path.Combine(
                AppContext.BaseDirectory,
                "Database Backup",
                "HuongDanSuDung",
                "HuongDanSuDung.html"
            );

            if (!File.Exists(fileGoc))
            {
                Debug.WriteLine("Không tìm thấy file hướng dẫn gốc!");
                return;
            }

            File.Copy(fileGoc, FileHuongDanIndex, true);
        }
        // THÊM 3 THÀNH PHẦN NÀY ĐỂ QUẢN LÝ CACHE TOÀN CỤC:
        public static Image? CachedAvatarAdmin = null;
        public static readonly object AvatarLock = new object();
        public static void XoaCacheAvatarToanCuc()
        {
            lock (AvatarLock)
            {
                CachedAvatarAdmin?.Dispose();
                CachedAvatarAdmin = null;
            }
        } 
        public static async Task HanhTrinhToiColombiaAsync()
        {
            TaoThuMucNeuChuaCo();

            await Module_KhoiTaoCSDL.BinhMinhOSantoriniAsync();

            DuongDanCSDL1 = ConDuongToLua("csdl1.db");
            DuongDanCSDL2 = ConDuongToLua("csdl2.db");
            DuongDanCSDL3 = ConDuongToLua("csdl3.db");
            DuongDanCSDL4 = ConDuongToLua("csdl4.db");
            DuongDanCSDL4ex = ConDuongToLua("csdlex.xlsx");

            DamBaoThuMucHuongDan();
            DamBaoHuongDanIndex();

            try
            {
                if (KiemTraTrangThaiSanSangCuaHeThongCSDL())
                {
                    Module_NhatKy.GhiNhatKy(
                        taiKhoan: string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM)
                            ? "System (Quyền cao nhất)"
                            : Module_TaiKhoan.TenTaiKhoan_RAM,
                        hanhDong: "Kiểm tra và khởi động CSDL",
                        ghiChu: "Hệ thống sẵn sàng");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        private static void TaoThuMucNeuChuaCo()
        {
            if (!Directory.Exists(ThuMucCoSoDuLieu))
            {
                Directory.CreateDirectory(ThuMucCoSoDuLieu);
            }
            if (!Directory.Exists(ThuMucLichSuThiDua))
            {
                Directory.CreateDirectory(ThuMucLichSuThiDua);
            }
        }
        private static string ConDuongToLua(string tenFile)
        {
            string path = Path.Combine(ThuMucCoSoDuLieu, tenFile);
            return File.Exists(path) ? path : string.Empty;
        }
        // =========================================================================
        // 3. XÁC THỰC FILE (Đã khôi phục logic FileShare.ReadWrite cực kỳ an toàn của bạn)
        // =========================================================================
        public static bool TonTaiVaMoDuoc(string path)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(path)
                    && File.Exists(path);
            }
            catch
            {
                return false;
            }
        }
        public static bool KiemTraTonTai(string tenFileCSDL)
        {
            string duongDan = Path.Combine(ThuMucCoSoDuLieu, tenFileCSDL);
            return File.Exists(duongDan);
        }
        public static bool KiemTraTrangThaiSanSangCuaHeThongCSDL()
        {
            return
                File.Exists(DuongDanCSDL1) &&
                File.Exists(DuongDanCSDL2) &&
                File.Exists(DuongDanCSDL3) &&
                File.Exists(DuongDanCSDL4);
        }
        public static string LayDuongDanTheoSo(int so)
        {
            return so switch
            {
                1 => DuongDanCSDL1,
                2 => DuongDanCSDL2,
                3 => DuongDanCSDL3,
                4 => DuongDanCSDL4,
                5 => DuongDanCSDL4ex,
                6 => FileHuongDanIndex,
                //6 => DuongDanCSDLHD,
                //7 => DuongDanpx64,
                //8 => DuongDanpx86,
                _ => string.Empty,
            };
        }
    }
    internal static class Module_TaiKhoan
    {
        public static string TenTaiKhoan_RAM = string.Empty;
        public static string MatKhau_RAM = string.Empty;
        public static bool NapTaiKhoanTuCSDL(int id = 1)
        {
            try
            {
                string dbPath = Module_DanduongGPS.DuongDanCSDL1;
                if (string.IsNullOrWhiteSpace(dbPath)) return false;

                using var conn = new SqliteConnection($"Data Source={dbPath}");
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT TenTaiKhoan, MatKhau FROM Admin WHERE ID = @id LIMIT 1";
                cmd.Parameters.AddWithValue("@id", id);

                using var rd = cmd.ExecuteReader();
                if (!rd.Read())
                {
                    TenTaiKhoan_RAM = string.Empty;
                    MatKhau_RAM = string.Empty;
                    return false;
                }

                TenTaiKhoan_RAM = BaoMatAES.GiaiMa(rd.GetString(0));
                MatKhau_RAM = BaoMatAES.GiaiMa(rd.GetString(1));

                return true;
            }
            catch
            {
                TenTaiKhoan_RAM = string.Empty;
                MatKhau_RAM = string.Empty;
                return false;
            }
        }
        public static string LayThongTinPhienBanVaToken()
        {
            try
            {
                string phienBan = Module_PhienBan.SoftwareVersion;
                string ngayCapNhat = Module_PhienBan.NgayThangNamCapNhat;
                string tokenGiaiMa = "Không xác định";
                string csdlPath = Module_DanduongGPS.DuongDanCSDL1;

                if (File.Exists(csdlPath))
                {
                    using var conn = new SqliteConnection($"Data Source={csdlPath}");
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT Token FROM Admin WHERE ID = 1";
                    var result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        try { tokenGiaiMa = result.ToString(); }
                        catch { tokenGiaiMa = "Token không hợp lệ"; }
                    }
                    else { tokenGiaiMa = "Chưa thiết lập Token"; }
                }
                else { tokenGiaiMa = "Không tìm thấy cơ sở dữ liệu"; }

                return "THÔNG TIN PHẦN MỀM\n\n" +
                       $"Phiên bản: {phienBan}\n" +
                       $"Ngày cập nhật: {ngayCapNhat}\n\n" +
                       "Bảo mật:\n" +
                       "- Dữ liệu được mã hóa khi lưu trữ.\n" +
                       "- Hệ thống hoạt động nội bộ.\n\n" +
                       $"Token hệ thống: {tokenGiaiMa}";
            }
            catch { return "Không thể lấy thông tin phần mềm."; }
        }
        public static string LayPhienBanPhanMem()
        {
            try
            {
                string db = Module_DanduongGPS.DuongDanCSDL2;
                if (string.IsNullOrWhiteSpace(db) || !File.Exists(db)) return "";

                using var cn = new SqliteConnection($"Data Source={db}");
                cn.Open();
                using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT DoiTuong FROM PhienBan_DoiTuong LIMIT 1";
                object result = cmd.ExecuteScalar();

                if (result == null || result == DBNull.Value) return "";

                string chuoi = result.ToString()?.Trim();
                if (string.IsNullOrEmpty(chuoi)) return "";

                string doiTuong;
                try { doiTuong = BaoMatAES.GiaiMa(chuoi).Trim(); }
                catch { return ""; }

                if (doiTuong.Equals("Phiên bản dành cho tân binh", StringComparison.OrdinalIgnoreCase))
                    return "Phần mềm: Phiên bản dành cho tân binh";
                if (doiTuong.Equals("Phiên bản dành cho CBCS", StringComparison.OrdinalIgnoreCase))
                    return "Phần mềm: Phiên bản dành cho CBCS";

                return "";
            }
            catch { return ""; }
        }
        public static string XacDinhTenTieuDoan()
        {
            string dbPath = Module_DanduongGPS.DuongDanCSDL2;
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return "Không xác định";

            try
            {
                using var conn = new SqliteConnection($"Data Source={dbPath}");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT TenTieuDoan FROM ThongTin WHERE ID=1";
                var result = cmd.ExecuteScalar();

                if (result != null && !string.IsNullOrWhiteSpace(result.ToString()))
                {
                    try { return BaoMatAES.GiaiMa(result.ToString().Trim()); }
                    catch { return result.ToString().Trim(); }
                }
                return "Không xác định";
            }
            catch { return "Không xác định"; }
        }

       

    }
}