////Token CSDL - Phiên bản 2: chữ ký toàn vẹn cho bộ 4 CSDL
///
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// TOKEN CSDL (v2) — xác định 4 tệp csdl1..csdl4 thuộc CÙNG MỘT BỘ và không bị sửa tay.
    ///
    /// NGUYÊN TẮC:
    ///  1) Token đi theo BỘ CSDL, không gắn với máy => chép nguyên bộ sang máy khác vẫn hợp lệ.
    ///  2) KHÔNG BAO GIỜ xóa tệp, KHÔNG tự ghi đè khi phát hiện sai lệch: chỉ báo cáo; việc ký lại
    ///     do Admin quyết định (sau khi xác minh mật khẩu ở Form24_XacMinhAdmin).
    ///  3) CSDL1 (chứa tài khoản Admin) là bản chuẩn. Không còn "bỏ phiếu số đông".
    ///
    /// CẤU TRÚC: mỗi CSDL có bảng Token_ChuKyCSDL (ID=1):
    ///     VaiTro   : "CSDL1".."CSDL4" (đưa vào chữ ký => phát hiện tráo tệp giữa các vai trò)
    ///     MaBo     : mã ngẫu nhiên 128-bit của bộ CSDL (sinh 1 lần, giữ nguyên khi đổi Token)
    ///     PhienBan : số nguyên, tăng 1 mỗi lần cấp lại Token
    ///     ChuKy    : HMAC-SHA256(K, "PMTD-TOKEN|VaiTro|MaBo|PhienBan")
    ///   với K = HMAC-SHA256(khóa phụ hệ thống, Token gốc của Admin trong CSDL1).
    ///
    /// TƯƠNG THÍCH: bảng cũ Token_XacDinhChinhChu vẫn được ghi khi cấp phát (bản phần mềm cũ còn đọc nó).
    /// CSDL cũ chưa có chữ ký sẽ được nâng cấp tự động 1 lần nếu Token cũ khớp (hoặc chưa từng có bảng cũ).
    /// </summary>
    internal static class Module_TokenCSDL
    {
        private const string BANG_MOI = "Token_ChuKyCSDL";
        private const string BANG_CU = "Token_XacDinhChinhChu";
        private const string NHAN_KHOA = "PMTD2026|TOKEN-CSDL|v2";
        private const string DINH_DANG_THOI_GIAN = Module_HeThong.DinhDang_NgayGio;
        internal enum TrangThai
        {
            HopLe,          // chữ ký đúng, cùng bộ, cùng phiên bản
            DaNangCap,      // CSDL cũ chưa có chữ ký => vừa được ký tự động (an toàn: Token cũ khớp)
            DaKy,           // vừa được cấp / ký lại theo yêu cầu
            KhongKhop,      // sai chữ ký / khác bộ / khác phiên bản => CẦN Admin quyết định
            KhongDocDuoc    // tệp đang bị khóa, thiếu, hỏng... => không kết luận
        }
        internal sealed record KetQua(string VaiTro, string DuongDan, TrangThai TrangThai, string ChiTiet);
        private sealed record BanGhi(string MaBo, long PhienBan, string ChuKy);
        private static (string VaiTro, string DuongDan)[] DanhSachCSDL() => new[]
        {
            ("CSDL1", Module_DanduongGPS.DuongDanCSDL1),
            ("CSDL2", Module_DanduongGPS.DuongDanCSDL2),
            ("CSDL3", Module_DanduongGPS.DuongDanCSDL3),
            ("CSDL4", Module_DanduongGPS.DuongDanCSDL4)
        };
        // ===================== 1. MẬT MÃ =====================
        private static byte[] TaoKhoa(string tokenGoc)
        {
            byte[] khoaPhu = Module_BaoMatAES.TaoKhoaPhu(NHAN_KHOA);
            try { return HMACSHA256.HashData(khoaPhu, Encoding.UTF8.GetBytes(tokenGoc)); }
            finally { CryptographicOperations.ZeroMemory(khoaPhu); }
        }
        private static string TaoChuKy(byte[] k, string vaiTro, string maBo, long phienBan)
            => Convert.ToHexString(HMACSHA256.HashData(k, Encoding.UTF8.GetBytes($"PMTD-TOKEN|{vaiTro}|{maBo}|{phienBan}")));
        private static bool ChuKyDung(byte[] k, string vaiTro, BanGhi bg)
            => CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(TaoChuKy(k, vaiTro, bg.MaBo, bg.PhienBan)),
                Encoding.ASCII.GetBytes(bg.ChuKy ?? string.Empty));
        // ===================== 2. TRUY CẬP CSDL =====================
        // ReadWrite (không Create): tệp thiếu => lỗi, KHÔNG sinh tệp rỗng. Pooling=false: đóng là nhả hẳn tệp.
        private static SqliteConnection MoKetNoi(string path, bool chiDoc)
            => Module_KetNoiCSDL.MoKetNoi(path, chiDoc: chiDoc, khongTaoMoi: !chiDoc, pooling: false, thoiGianCho: 10);
        private static bool CoBang(SqliteConnection conn, string tenBang)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=@n LIMIT 1";
            cmd.Parameters.AddWithValue("@n", tenBang);
            return cmd.ExecuteScalar() != null;
        }
        private static BanGhi? DocBanGhi(SqliteConnection conn)
        {
            if (!CoBang(conn, BANG_MOI)) return null;
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT MaBo, PhienBan, ChuKy FROM {BANG_MOI} WHERE ID=1";
            using var rd = cmd.ExecuteReader();
            if (!rd.Read() || rd.IsDBNull(0) || rd.IsDBNull(1) || rd.IsDBNull(2)) return null;
            return new BanGhi(rd.GetString(0), rd.GetInt64(1), rd.GetString(2));
        }
        /// <summary>Token trong bảng cũ: null = chưa từng có bảng/dòng cũ; "" = có nhưng không giải mã được.</summary>
        private static string? DocTokenBangCu(SqliteConnection conn)
        {
            if (!CoBang(conn, BANG_CU)) return null;
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT Ma_ToKen FROM {BANG_CU} WHERE ID=1";
            object? v = cmd.ExecuteScalar();
            if (v == null || v == DBNull.Value) return null;
            string raw = Convert.ToString(v) ?? string.Empty;
            return raw.Length == 0 ? null : Module_BaoMatAES.GiaiMa(raw);
        }
        /// <summary>Ghi chữ ký (và tùy chọn bảng cũ cho tương thích) trong 1 transaction.</summary>
        private static void GhiChuKy(SqliteConnection conn, string vaiTro, string maBo, long phienBan, string chuKy,
                                     (string TokenMaHoa, string TaiKhoanMaHoa)? bangCu)
        {
            string thoiGian = DateTime.Now.ToString(DINH_DANG_THOI_GIAN);
            using var tx = conn.BeginTransaction();
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS {BANG_MOI} (
                        ID INTEGER NOT NULL PRIMARY KEY,
                        VaiTro TEXT NOT NULL, MaBo TEXT NOT NULL, PhienBan INTEGER NOT NULL,
                        ChuKy TEXT NOT NULL, ThoiGian TEXT);
                    INSERT INTO {BANG_MOI} (ID, VaiTro, MaBo, PhienBan, ChuKy, ThoiGian)
                    VALUES (1, @vt, @mb, @pb, @ck, @tg)
                    ON CONFLICT(ID) DO UPDATE SET VaiTro=excluded.VaiTro, MaBo=excluded.MaBo,
                        PhienBan=excluded.PhienBan, ChuKy=excluded.ChuKy, ThoiGian=excluded.ThoiGian;";
                cmd.Parameters.AddWithValue("@vt", vaiTro);
                cmd.Parameters.AddWithValue("@mb", maBo);
                cmd.Parameters.AddWithValue("@pb", phienBan);
                cmd.Parameters.AddWithValue("@ck", chuKy);
                cmd.Parameters.AddWithValue("@tg", thoiGian);
                cmd.ExecuteNonQuery();
            }
            if (bangCu != null)
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS {BANG_CU} (
                        ID INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        STT TEXT, Ma_ToKen TEXT, Tai_Khoan_Cap_Nhat TEXT, Thoi_Gian_Nap TEXT);
                    INSERT OR REPLACE INTO {BANG_CU} (ID, STT, Ma_ToKen, Tai_Khoan_Cap_Nhat, Thoi_Gian_Nap)
                    VALUES (1, '1', @token, @tk, @tg);";
                cmd.Parameters.AddWithValue("@token", bangCu.Value.TokenMaHoa);
                cmd.Parameters.AddWithValue("@tk", bangCu.Value.TaiKhoanMaHoa);
                cmd.Parameters.AddWithValue("@tg", thoiGian);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        /// <summary>Token gốc (đã giải mã) của Admin trong CSDL1. "" nếu chưa đăng ký hoặc không giải mã được.</summary>
        private static string DocTokenAdmin()
        {
            string path = Module_DanduongGPS.DuongDanCSDL1;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return string.Empty;
            using var conn = MoKetNoi(path, chiDoc: true);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Token FROM Admin WHERE ID=1";
            object? v = cmd.ExecuteScalar();
            if (v == null || v == DBNull.Value) return string.Empty;
            // KHÔNG rơi về chuỗi thô khi giải mã lỗi: chuỗi mã của bộ khóa khác không phải Token hợp lệ.
            return Module_BaoMatAES.GiaiMa(Convert.ToString(v) ?? string.Empty);
        }
        private static string MoTaTep(string path)
        {
            try
            {
                var fi = new FileInfo(path);
                return $"{fi.Name} — {fi.Length:N0} bytes — sửa lần cuối {fi.LastWriteTime:dd/MM/yyyy HH:mm:ss}";
            }
            catch { return path; }
        }
        // ===================== 3. API CÔNG KHAI =====================
        /// <summary>
        /// Cấp (hoặc cấp lại) Token cho cả 4 CSDL — gọi từ Form3 sau khi lưu tài khoản Admin.
        /// Giữ nguyên Mã bộ nếu CSDL1 đã có, tăng Phiên bản. Trả về kết quả từng CSDL (DaKy / KhongDocDuoc).
        /// </summary>
        public static List<KetQua> CapPhat(string tokenGoc, string taiKhoan)
        {
            var kq = new List<KetQua>();
            if (string.IsNullOrWhiteSpace(tokenGoc)) throw new ArgumentException("Token rỗng.", nameof(tokenGoc));
            byte[] k = TaoKhoa(tokenGoc);
            try
            {
                string maBo = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
                long phienBan = 1;
                string path1 = Module_DanduongGPS.DuongDanCSDL1;
                if (!string.IsNullOrWhiteSpace(path1) && File.Exists(path1))
                {
                    try
                    {
                        using var conn = MoKetNoi(path1, chiDoc: true);
                        var bg = DocBanGhi(conn);
                        if (bg != null) { maBo = bg.MaBo; phienBan = bg.PhienBan + 1; }
                    }
                    catch (Exception ex) { Debug.WriteLine("[TokenCSDL] Đọc CSDL1: " + ex.Message); }
                }
                var bangCu = (Module_BaoMatAES.MaHoa(tokenGoc), Module_BaoMatAES.MaHoa(taiKhoan ?? string.Empty));
                foreach (var (vaiTro, path) in DanhSachCSDL())
                {
                    if (string.IsNullOrWhiteSpace(path)) continue;
                    if (!File.Exists(path)) { kq.Add(new KetQua(vaiTro, path, TrangThai.KhongDocDuoc, "Không tìm thấy tệp")); continue; }
                    try
                    {
                        using var conn = MoKetNoi(path, chiDoc: false);
                        GhiChuKy(conn, vaiTro, maBo, phienBan, TaoChuKy(k, vaiTro, maBo, phienBan), bangCu);
                        kq.Add(new KetQua(vaiTro, path, TrangThai.DaKy, $"Phiên bản {phienBan}"));
                    }
                    catch (Exception ex) { kq.Add(new KetQua(vaiTro, path, TrangThai.KhongDocDuoc, ex.Message)); }
                }
            }
            finally { CryptographicOperations.ZeroMemory(k); }
            return kq;
        }
        /// <summary>
        /// Kiểm tra bộ CSDL (gọi ở luồng nền). Không xóa, không ghi đè dữ liệu đã có chữ ký.
        /// Chỉ GHI khi nâng cấp CSDL cũ chưa có chữ ký (an toàn). Trả về danh sách rỗng nếu chưa đăng ký Admin
        /// hoặc khóa hệ thống chưa nạp (không đủ căn cứ kết luận).
        /// </summary>
        public static List<KetQua> KiemTra()
        {
            var kq = new List<KetQua>();
            string token;
            try { token = DocTokenAdmin(); }
            catch (Exception ex) { Debug.WriteLine("[TokenCSDL] Đọc Token Admin: " + ex.Message); return kq; }
            if (string.IsNullOrWhiteSpace(token)) return kq;
            byte[] k;
            try { k = TaoKhoa(token); }
            catch (Exception ex) { Debug.WriteLine("[TokenCSDL] Khóa chưa sẵn sàng: " + ex.Message); return kq; }
            try
            {
                var ds = DanhSachCSDL();
                // ---- BƯỚC 1: CSDL1 là bản chuẩn ----
                var (vt1, path1) = ds[0];
                BanGhi chuan;
                try
                {
                    using var conn = MoKetNoi(path1, chiDoc: false);
                    var bg = DocBanGhi(conn);
                    if (bg == null)
                    {
                        // CSDL1 chưa có chữ ký (dữ liệu từ bản cũ) => khởi tạo Mã bộ. Token lấy chính từ CSDL1 nên luôn an toàn.
                        chuan = new BanGhi(Convert.ToHexString(RandomNumberGenerator.GetBytes(16)), 1, string.Empty);
                        GhiChuKy(conn, vt1, chuan.MaBo, chuan.PhienBan, TaoChuKy(k, vt1, chuan.MaBo, chuan.PhienBan), null);
                        kq.Add(new KetQua(vt1, path1, TrangThai.DaNangCap, "Khởi tạo chữ ký lần đầu"));
                    }
                    else
                    {
                        chuan = bg;
                        kq.Add(ChuKyDung(k, vt1, bg)
                            ? new KetQua(vt1, path1, TrangThai.HopLe, "")
                            : new KetQua(vt1, path1, TrangThai.KhongKhop,
                                "Chữ ký không khớp với Token của tài khoản Admin (bảng Token hoặc tài khoản Admin bị thay đổi ngoài phần mềm)."));
                    }
                }
                catch (Exception ex)
                {
                    kq.Add(new KetQua(vt1, path1, TrangThai.KhongDocDuoc, ex.Message));
                    return kq;   // không có bản chuẩn => không kết luận các CSDL còn lại
                }
                // ---- BƯỚC 2: CSDL2..4 so với bản chuẩn ----
                for (int i = 1; i < ds.Length; i++)
                {
                    var (vt, path) = ds[i];
                    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) continue;
                    try
                    {
                        using var conn = MoKetNoi(path, chiDoc: false);
                        var bg = DocBanGhi(conn);
                        if (bg == null)
                        {
                            string? tokenCu = DocTokenBangCu(conn);
                            // Nâng cấp tự động CHỈ khi không có bằng chứng thuộc bộ khác:
                            // chưa từng có Token cũ, hoặc Token cũ khớp Token Admin hiện tại.
                            if (tokenCu == null || string.Equals(tokenCu, token, StringComparison.Ordinal))
                            {
                                GhiChuKy(conn, vt, chuan.MaBo, chuan.PhienBan, TaoChuKy(k, vt, chuan.MaBo, chuan.PhienBan), null);
                                kq.Add(new KetQua(vt, path, TrangThai.DaNangCap, "Nâng cấp chữ ký từ định dạng cũ"));
                            }
                            else
                            {
                                kq.Add(new KetQua(vt, path, TrangThai.KhongKhop,
                                    "Chưa có chữ ký mới và Token cũ KHÁC Token hiện tại (có thể chép từ bộ CSDL khác)."));
                            }
                        }
                        else if (!string.Equals(bg.MaBo, chuan.MaBo, StringComparison.Ordinal))
                            kq.Add(new KetQua(vt, path, TrangThai.KhongKhop, "Thuộc bộ CSDL khác (Mã bộ không trùng với CSDL1)."));
                        else if (bg.PhienBan != chuan.PhienBan)
                            kq.Add(new KetQua(vt, path, TrangThai.KhongKhop,
                                $"Chữ ký phiên bản {bg.PhienBan}, CSDL1 là phiên bản {chuan.PhienBan} (lần cấp Token trước chưa ghi được vào tệp này)."));
                        else if (!ChuKyDung(k, vt, bg))
                            kq.Add(new KetQua(vt, path, TrangThai.KhongKhop, "Chữ ký sai (bảng Token bị sửa hoặc tệp bị tráo vai trò)."));
                        else
                            kq.Add(new KetQua(vt, path, TrangThai.HopLe, ""));
                    }
                    catch (Exception ex) { kq.Add(new KetQua(vt, path, TrangThai.KhongDocDuoc, ex.Message)); }
                }
            }
            finally { CryptographicOperations.ZeroMemory(k); }
            return kq;
        }
        /// <summary>
        /// Ký lại các CSDL được chỉ định theo bản chuẩn CSDL1 (Mã bộ + Phiên bản hiện tại). CHỈ gọi sau khi Admin
        /// đã xác minh mật khẩu. Chỉ ghi bảng Token_ChuKyCSDL, không đụng tới dữ liệu nghiệp vụ.
        /// </summary>
        public static List<KetQua> KyLai(IEnumerable<string> vaiTroCanKy)
        {
            var kq = new List<KetQua>();
            var can = new HashSet<string>(vaiTroCanKy, StringComparer.Ordinal);
            string token = DocTokenAdmin();
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("Không đọc được Token của tài khoản Admin trong CSDL1.");
            byte[] k = TaoKhoa(token);
            try
            {
                var ds = DanhSachCSDL();
                BanGhi? chuan;
                using (var conn1 = MoKetNoi(ds[0].DuongDan, chiDoc: true)) chuan = DocBanGhi(conn1);
                chuan ??= new BanGhi(Convert.ToHexString(RandomNumberGenerator.GetBytes(16)), 1, string.Empty);
                foreach (var (vt, path) in ds)
                {
                    if (!can.Contains(vt)) continue;
                    try
                    {
                        using var conn = MoKetNoi(path, chiDoc: false);
                        GhiChuKy(conn, vt, chuan.MaBo, chuan.PhienBan, TaoChuKy(k, vt, chuan.MaBo, chuan.PhienBan), null);
                        kq.Add(new KetQua(vt, path, TrangThai.DaKy, ""));
                    }
                    catch (Exception ex) { kq.Add(new KetQua(vt, path, TrangThai.KhongDocDuoc, ex.Message)); }
                }
            }
            finally { CryptographicOperations.ZeroMemory(k); }
            return kq;
        }
        /// <summary>
        /// Dấu vân tay ngắn của Token (dạng "A3F9-12C0") để hiển thị/hỗ trợ kỹ thuật, KHÔNG suy ngược ra Token.
        /// Trả "" nếu chưa thiết lập hoặc không đọc được.
        /// </summary>
        public static string LayDauVanTay()
        {
            try
            {
                string token = DocTokenAdmin();
                if (string.IsNullOrWhiteSpace(token)) return string.Empty;
                byte[] k = TaoKhoa(token);
                try
                {
                    byte[] h = SHA256.HashData(k);
                    string hex = Convert.ToHexString(h, 0, 4);
                    return hex.Substring(0, 4) + "-" + hex.Substring(4, 4);
                }
                finally { CryptographicOperations.ZeroMemory(k); }
            }
            catch { return string.Empty; }
        }
        /// <summary>Mô tả 1 dòng cho thông báo (tên tệp, dung lượng, thời gian sửa).</summary>
        public static string MoTa(KetQua kq) => $"• {kq.VaiTro}: {kq.ChiTiet}\n   {MoTaTep(kq.DuongDan)}";
    }
}
