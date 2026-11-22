///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Đọc và xác thực tài khoản Admin (csdl1.db, bảng Admin, ID = 1) cho Form8_CauHinhCSDL và Form9_GoCaiDat.
    /// Trước đây mỗi form tự viết 1 bản (và 2 bản lệch nhau); gom về 1 nơi để quy tắc bảo mật chỉ có một chỗ sửa.
    ///
    /// QUY ƯỚC: mọi hàm ở đây chạy ĐỒNG BỘ và nặng I/O (SQLite + AES) nên PHẢI gọi từ luồng nền:
    ///     var kq = await Task.Run(() => Module_XacThucAdmin.XacThuc(ten, matKhau));
    /// </summary>
    internal static class Module_XacThucAdmin
    {
        internal enum KetQua
        {
            ThanhCong,
            SaiThongTin,
            /// <summary>Không đọc/giải mã được tài khoản (thiếu CSDL, thiếu khóa, dữ liệu hỏng). KHÔNG tính là gõ sai mật khẩu.</summary>
            KhongDocDuocCSDL
        }
        private const string SQL_DOC_ADMIN = "SELECT TenTaiKhoan, MatKhau FROM Admin WHERE ID = 1 LIMIT 1";
        private readonly record struct TaiKhoan(string? Ten, string? MatKhau);
        /// <summary>Tên Admin đã giải mã; null nếu không đọc được.</summary>
        public static string? DocTenAdmin()
        {
            TaiKhoan? tk = DocTaiKhoan();
            if (tk is null) return null;
            string? ten = tk.Value.Ten;
            return string.IsNullOrWhiteSpace(ten) ? null : ten;
        }
        /// <summary>
        /// tenNhap = null: chỉ kiểm tra mật khẩu (Form9). Tên so sánh không phân biệt hoa/thường, mật khẩu phân biệt.
        /// TenChuan là tên đúng như trong CSDL (không phải chuỗi người dùng gõ) để gán cho phiên làm việc.
        /// </summary>
        public static (KetQua Kq, string TenChuan) XacThuc(string? tenNhap, string? matKhauNhap)
        {
            if (string.IsNullOrEmpty(matKhauNhap)) return (KetQua.SaiThongTin, string.Empty);
            TaiKhoan? tk = DocTaiKhoan();
            if (tk is null) return (KetQua.KhongDocDuocCSDL, string.Empty);
            string? ten = tk.Value.Ten;
            string? matKhau = tk.Value.MatKhau;
            // Không giải mã được thì từ chối và báo lỗi dữ liệu, không "đoán" bằng cách so với chính chuỗi mã hóa
            if (string.IsNullOrWhiteSpace(ten) || string.IsNullOrEmpty(matKhau))
                return (KetQua.KhongDocDuocCSDL, string.Empty);
            // Tính đủ cả 2 điều kiện rồi mới kết luận: không để thời gian trả lời lộ ra "tên đúng, mật khẩu sai"
            bool tenDung = tenNhap is null || string.Equals(ten, tenNhap.Trim(), StringComparison.OrdinalIgnoreCase);
            bool matKhauDung = BangNhauHangThoiGian(matKhau, matKhauNhap);
            return tenDung && matKhauDung ? (KetQua.ThanhCong, ten) : (KetQua.SaiThongTin, string.Empty);
        }
        private static TaiKhoan? DocTaiKhoan()
        {
            try
            {
                // Đọc đường dẫn tại thời điểm dùng (không giữ ở trường readonly): sau khi khôi phục CSDL đường dẫn có thể đổi
                string path = Module_DanduongGPS.DuongDanCSDL1;
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
                // Thiếu khóa thì GiaiMa trả "" - phải dừng ở đây (đóng cửa), không được rơi sang so sánh với bản mã
                Module_DanduongGPS.DamBaoKhoaDaNap();
                if (!Module_DanduongGPS.KhoaDaSanSang) return null;
                string? tenRaw, matKhauRaw;
                // MoKetNoiDoc: Pooling=false (đóng là nhả tệp, không chặn sao lưu/gỡ cài đặt), không tự tạo tệp rỗng khi thiếu CSDL
                using (var conn = Module_DanduongGPS.MoKetNoiDoc(path))
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = SQL_DOC_ADMIN;
                    using var rd = cmd.ExecuteReader(CommandBehavior.SingleRow);
                    if (!rd.Read()) return null;
                    tenRaw = rd.IsDBNull(0) ? null : rd.GetString(0);
                    matKhauRaw = rd.IsDBNull(1) ? null : rd.GetString(1);
                }
                return new TaiKhoan(GiaiMaHoacVanBan(tenRaw), GiaiMaHoacVanBan(matKhauRaw));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Module_XacThucAdmin] Không đọc được tài khoản Admin: {ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// Giải mã AES; CSDL cũ chưa mã hóa (văn bản thuần) thì dùng nguyên văn.
        /// Chuỗi mang tiền tố "v2|"/"v5|" chắc chắn là bản mã: nếu giải mã hỏng thì trả null thay vì coi chính bản mã là mật khẩu
        /// (bản cũ cho phép gõ đúng chuỗi "v2|..." chép từ tệp .db để vượt qua xác thực).
        /// </summary>
        private static string? GiaiMaHoacVanBan(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            raw = raw.Trim();
            string giaiMa = Module_BaoMatAES.GiaiMa(raw);
            if (!string.IsNullOrEmpty(giaiMa)) return giaiMa;
            return Module_BaoMatAES.LaChuoiMa(raw) ? null : raw;
        }
        /// <summary>So sánh thời gian cố định, băm trước để độ dài mật khẩu cũng không lộ ra.</summary>
        private static bool BangNhauHangThoiGian(string a, string b)
        {
            Span<byte> ha = stackalloc byte[32];
            Span<byte> hb = stackalloc byte[32];
            SHA256.HashData(Encoding.UTF8.GetBytes(a), ha);
            SHA256.HashData(Encoding.UTF8.GetBytes(b), hb);
            return CryptographicOperations.FixedTimeEquals(ha, hb);
        }
    }
}