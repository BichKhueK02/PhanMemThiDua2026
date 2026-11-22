#nullable enable
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// TỆP KHÓA HỆ THỐNG — khóa mã hóa KHÔNG còn nằm trong tệp .exe.
    ///
    /// NGUYÊN TẮC
    ///  1) Khóa lưu ở tệp riêng trong thư mục Database (đường dẫn tương đối theo thư mục chạy) => chép cả thư mục
    ///     phần mềm sang máy khác là đọc được ngay, không gắn với máy/tài khoản Windows (không dùng DPAPI/TPM).
    ///  2) Giá trị khóa trong tệp GIỮ NGUYÊN khóa cũ => toàn bộ CSDL, nhật ký, bản sao lưu, Excel v3,
    ///     và khóa trao cho ServiceBackup/ServiceRestore đều không đổi, không phải mã hóa lại.
    ///  3) Có bản dự phòng; mất/hỏng 1 bản thì tự phục hồi từ bản còn lại. Tệp hỏng được đổi tên giữ lại, không xóa.
    ///  4) Không có tệp khóa hợp lệ => KHÔNG sinh khóa mới, KHÔNG dùng khóa cứng (tránh ghi dữ liệu không giải mã được).
    ///
    /// ĐỊNH DẠNG (UTF-8, 3 dòng):
    ///     PMTD-KEYFILE|1
    ///     b64(IV1)|b64(IV2)|b64(Khoa1)|b64(Khoa2)
    ///     hex16( SHA256( dòng1 + "\n" + dòng2 ) )      — phát hiện tệp bị cắt/sửa
    /// </summary>
    internal static class Module_TepKhoa
    {
        public const string TEN_THU_MUC = "KhoaBaoMat";
        public const string TEN_TEP = "PhanMemThiDua2026.pmtdkey";
        private const string DONG_DAU = "PMTD-KEYFILE|1";
        private const int KICH_THUOC_TOI_DA = 16 * 1024;
        // LƯU Ý: thư mục "KhoaBaoMat" PHẢI có trong danh sách trắng AllowDirsDatabase / AllowDirsBackup của
        // Module_KhoiTaoCSDL.ChinhSachLamSach, nếu không chính sách làm sạch sẽ xóa mất tệp khóa.
        /// <summary>Bản chính: &lt;thư mục phần mềm&gt;\Database\KhoaBaoMat\PhanMemThiDua2026.pmtdkey</summary>
        public static string DuongDanChinh { get; } = Path.Combine(AppContext.BaseDirectory, "Database", TEN_THU_MUC, TEN_TEP);
        /// <summary>Bản dự phòng: &lt;thư mục phần mềm&gt;\Database Backup\KhoaBaoMat\PhanMemThiDua2026.pmtdkey</summary>
        public static string DuongDanDuPhong { get; } = Path.Combine(AppContext.BaseDirectory, "Database Backup", TEN_THU_MUC, TEN_TEP);
        /// <summary>Vị trí của bản thử nghiệm trước (cạnh .exe). CHỈ ĐỌC để chuyển sang vị trí mới; không bao giờ ghi vào đây.</summary>
        private static string DuongDanCu { get; } = Path.Combine(AppContext.BaseDirectory, TEN_THU_MUC, TEN_TEP);
        /// <summary>Mô tả lỗi của lần nạp gần nhất (null nếu nạp thành công).</summary>
        public static string? LoiGanNhat { get; private set; }
        /// <summary>Đường dẫn tệp khóa đã nạp thành công.</summary>
        public static string? NguonDaNap { get; private set; }
        internal sealed record BoKhoa(string IV1, string IV2, string Khoa1, string Khoa2);
        private static readonly object _khoa = new();
        // ===================== NẠP =====================
        /// <summary>Đọc tệp khóa theo thứ tự: bản chính → bản dự phòng → vị trí cũ. Null nếu không có tệp hợp lệ.</summary>
        public static BoKhoa? Nap()
        {
            lock (_khoa)
            {
                var loi = new List<string>();
                foreach (string p in new[] { DuongDanChinh, DuongDanDuPhong, DuongDanCu })
                {
                    if (!File.Exists(p)) continue;
                    if (DocTep(p, out BoKhoa? bo, out string l) && bo != null)
                    {
                        NguonDaNap = p;
                        LoiGanNhat = null;
                        TuPhucHoi(bo);
                        return bo;
                    }
                    loi.Add(p + " — " + l);
                }
                LoiGanNhat = loi.Count == 0
                    ? "Không tìm thấy tệp khóa \"" + TEN_TEP + "\"."
                    : "Tệp khóa không hợp lệ:\n" + string.Join("\n", loi);
                Debug.WriteLine("[TepKhoa] " + LoiGanNhat);
                return null;
            }
        }
        private static bool DocTep(string path, out BoKhoa? bo, out string loi)
        {
            bo = null;
            loi = "";
            try
            {
                var fi = new FileInfo(path);
                if (fi.Length == 0) { loi = "tệp rỗng"; return false; }
                if (fi.Length > KICH_THUOC_TOI_DA) { loi = "tệp quá lớn"; return false; }
                byte[] raw;
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var ms = new MemoryStream())
                {
                    fs.CopyTo(ms);
                    raw = ms.ToArray();
                }
                // Chịu được BOM và xuống dòng CRLF (trường hợp tệp từng được mở/lưu bằng Notepad)
                string text = Encoding.UTF8.GetString(raw).TrimStart('\uFEFF');
                string[] dong = text.Split('\n')
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .ToArray();
                if (dong.Length < 3 || !string.Equals(dong[0], DONG_DAU, StringComparison.Ordinal))
                { loi = "sai định dạng"; return false; }
                byte[] tinh = Encoding.ASCII.GetBytes(TinhKiemTra(dong[0], dong[1]));
                byte[] ghi = Encoding.ASCII.GetBytes(dong[2].ToUpperInvariant());
                if (!CryptographicOperations.FixedTimeEquals(tinh, ghi))
                { loi = "sai mã kiểm tra (tệp bị sửa hoặc hỏng)"; return false; }
                string[] phan = dong[1].Split('|');
                if (phan.Length != 4) { loi = "thiếu thành phần khóa"; return false; }
                var v = new string[4];
                for (int i = 0; i < 4; i++)
                {
                    v[i] = Encoding.UTF8.GetString(Convert.FromBase64String(phan[i])).Trim();
                    if (v[i].Length == 0) { loi = "thành phần khóa rỗng"; return false; }
                }
                bo = new BoKhoa(v[0], v[1], v[2], v[3]);
                return true;
            }
            catch (Exception ex)
            {
                loi = ex.Message;
                return false;
            }
        }
        private static string TinhKiemTra(string dong1, string dong2)
        {
            byte[] h = SHA256.HashData(Encoding.UTF8.GetBytes(dong1 + "\n" + dong2));
            return Convert.ToHexString(h, 0, 16);   // chữ HOA
        }
        internal static string TaoNoiDung(BoKhoa bo)
        {
            static string B(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
            string d2 = B(bo.IV1) + "|" + B(bo.IV2) + "|" + B(bo.Khoa1) + "|" + B(bo.Khoa2);
            return DONG_DAU + "\n" + d2 + "\n" + TinhKiemTra(DONG_DAU, d2) + "\n";
        }
        // ===================== TỰ PHỤC HỒI =====================
        /// <summary>Đảm bảo bản chính và bản dự phòng đều tồn tại và đúng với bộ khóa vừa nạp. Lỗi ghi không ảnh hưởng việc chạy.</summary>
        private static void TuPhucHoi(BoKhoa bo)
        {
            string noiDung = TaoNoiDung(bo);
            foreach (string dich in new[] { DuongDanChinh, DuongDanDuPhong })
            {
                try
                {
                    if (File.Exists(dich) && DocTep(dich, out BoKhoa? hienCo, out _) && hienCo == bo) continue;
                    GiuLaiTepHong(dich);
                    GhiNguyenTu(dich, noiDung);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[TepKhoa] Không phục hồi được " + dich + ": " + ex.Message);
                }
            }
        }
        /// <summary>Tệp hỏng/khác nội dung: đổi tên giữ lại để có thể điều tra, KHÔNG xóa.</summary>
        private static void GiuLaiTepHong(string path)
        {
            if (!File.Exists(path)) return;
            ThaoGoReadOnly(path);
            string giuLai = path + ".hong-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            File.Move(path, giuLai, true);
        }
        private static void GhiNguyenTu(string dich, string noiDung)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dich)!);
            string tam = dich + ".tmp";
            try
            {
                ThaoGoReadOnly(tam);
                File.WriteAllText(tam, noiDung, new UTF8Encoding(false));
                ThaoGoReadOnly(dich);
                File.Move(tam, dich, true);
                File.SetAttributes(dich, File.GetAttributes(dich) | FileAttributes.ReadOnly);   // chống sửa nhầm
            }
            finally
            {
                try { if (File.Exists(tam)) { ThaoGoReadOnly(tam); File.Delete(tam); } } catch { }
            }
        }
        private static void ThaoGoReadOnly(string path)
        {
            try
            {
                if (!File.Exists(path)) return;
                var a = File.GetAttributes(path);
                if ((a & FileAttributes.ReadOnly) != 0) File.SetAttributes(path, a & ~FileAttributes.ReadOnly);
            }
            catch { }
        }
        // ===================== THÔNG BÁO =====================
        public static string ThongBaoThieuKhoa() =>
            "Tình trạng: Không nạp được KHÓA BẢO MẬT của phần mềm.\n\n" +
            (LoiGanNhat ?? "Không rõ nguyên nhân.") + "\n\n" +
            "Khắc phục: chép tệp \"" + TEN_TEP + "\" (lấy từ bộ cài đặt / máy đang dùng phần mềm) vào:\n" +
            DuongDanChinh + "\n\n" +
            "Phần mềm đã dừng để bảo vệ dữ liệu: KHÔNG có dữ liệu nào bị ghi hoặc thay đổi.";
    }
}
