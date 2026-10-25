///Phiên bản 3 – ổn định dài hạn: nạp khóa an toàn đa luồng, không cấp phát khi giải mã ma trận, mở SQLite không khóa tệp, ghi tệp nguyên tử
///
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Globalization;
namespace PhanMemThiDua2026
{
    internal static class Module_DanduongGPS
    {
        // Lê Trung Kiên -  Yêu mèo cam
        // ===================== TÊN / ĐƯỜNG DẪN (tính 1 lần, không Path.Combine lặp lại mỗi lần truy cập) =====================
        public const string TenThuMucCongCuQuanLyCSDL = "CongCuQuanLyCSDL";
        public const string TenFileDBBrowserSQLite = "DB Browser for SQLite.exe";
        private const string TEN_THU_MUC_HUONG_DAN = "HuongDanSuDung";
        private const string TEN_FILE_HUONG_DAN_INDEX = "HuongDanSuDung.html";
        private const string TEN_THU_MUC_BACKUP = "Database Backup";
        private const string HAU_TO_TAM = ".tmp";
        // LƯU Ý THỨ TỰ: static field/property khởi tạo theo đúng thứ tự khai báo, ThuMucCoSoDuLieu phải đứng trước các đường dẫn dẫn xuất.
        public static readonly string ThuMucCoSoDuLieu = Path.Combine(AppContext.BaseDirectory, "Database");
        // Đường dẫn toàn cục của thư mục lịch sử thi đua năm
        public static string ThuMucLichSuThiDua { get; } = Path.Combine(ThuMucCoSoDuLieu, "LuuTruThiDua_LichSu");
        public static string ThuMucHuongDan { get; } = Path.Combine(ThuMucCoSoDuLieu, TEN_THU_MUC_HUONG_DAN);
        public static string FileHuongDanIndex { get; } = Path.Combine(ThuMucHuongDan, TEN_FILE_HUONG_DAN_INDEX);
        public static string ThuMucCongCuQuanLyCSDL { get; } = Path.Combine(ThuMucCoSoDuLieu, TenThuMucCongCuQuanLyCSDL);
        public static string ThuMucDatabaseBackup { get; } = Path.Combine(AppContext.BaseDirectory, TEN_THU_MUC_BACKUP);
        public static string ThuMucBackupCongCuQuanLyCSDL { get; } = Path.Combine(ThuMucDatabaseBackup, TenThuMucCongCuQuanLyCSDL);
        public static string DuongDanDBBrowserSQLite { get; } = Path.Combine(ThuMucCongCuQuanLyCSDL, TenFileDBBrowserSQLite);
        public static string DuongDanCSDL1 { get; private set; } = string.Empty;
        public static string DuongDanCSDL2 { get; private set; } = string.Empty;
        public static string DuongDanCSDL3 { get; private set; } = string.Empty;
        public static string DuongDanCSDL4 { get; private set; } = string.Empty;
        public static string DuongDanCSDL4ex { get; private set; } = string.Empty;
        // Hiện không nơi nào trong module gán giá trị cho 2 thuộc tính này (setter private) nên luôn rỗng.
        public static string TenDangNhapMacDinh { get; private set; } = string.Empty;
        public static string MatKhauDangNhapMacDinh { get; private set; } = string.Empty;
        /// <summary>Cảnh báo môi trường (vd: thư mục dữ liệu không ghi được). Null nếu không có vấn đề. Giao diện có thể hiển thị để chẩn đoán.</summary>
        public static string? CanhBaoMoiTruong { get; private set; }
        // ===================== KHÓA MÃ HÓA =====================
        // volatile: 4 trường *_KeyMaHoa được đọc không khóa ở nhiều luồng (double-checked locking). Trên ARM64 (Windows on ARM)
        // nếu thiếu volatile, luồng khác có thể thấy "cờ hiệu" đã gán nhưng các khóa còn lại chưa hiển thị.
        public static volatile string ToiCanChuIVMeoCam1_KeyMaHoa = string.Empty;
        public static volatile string ToiCanChuIVMeoCam2_KeyMaHoa = string.Empty;
        public static volatile string ToiYeuMeoCam1_KeyMaHoa = string.Empty;
        public static volatile string ToiYeuMeoCam2_KeyMaHoa = string.Empty;
        public static string ToiCanChuIVMeoCam1 = string.Empty;
        public static string ToiCanChuIVMeoCam2 = string.Empty;
        public static string ToiYeuMeoCam1 = string.Empty;
        public static string ToiYeuMeoCam2 = string.Empty;
        public static Action? OnDatabaseChanged;
        private static readonly object _lock = new();
        // Ma trận ĐÃ BỊ ĐẢO VỊ TRÍ (Swap) + MÃ HÓA XOR. Khai báo dạng ReadOnlySpan<byte> trả về mảng hằng:
        // trình biên dịch đặt dữ liệu vào vùng tĩnh của assembly, KHÔNG cấp phát mảng trên heap (bản gốc dựng 4 mảng byte[] mỗi lần nạp).
        private const byte SEED_KEY = 0x3F;
        private static ReadOnlySpan<byte> MaTran1 => new byte[] { 0x0C, 0x7B, 0x0C, 0x0F, 0x09, 0x06, 0x0B, 0x7B, 0x08, 0x08, 0x0B, 0x06, 0x09, 0x7E, 0x0B, 0x7A, 0x0B, 0x7C, 0x0A, 0x09, 0x0A, 0x0A, 0x0A, 0x08 };
        private static ReadOnlySpan<byte> MaTran2 => new byte[] { 0x0B, 0x7E, 0x0A, 0x7E, 0x0A, 0x09, 0x0B, 0x7C, 0x0A, 0x0A, 0x09, 0x07, 0x0A, 0x0A, 0x0A, 0x0C, 0x0B, 0x0A, 0x0A, 0x09, 0x0A, 0x09, 0x0A, 0x0E };
        private static ReadOnlySpan<byte> MaTran3 => new byte[] { 0x08, 0x06, 0x0B, 0x0E, 0x09, 0x7E, 0x0B, 0x7B, 0x0C, 0x0D, 0x0C, 0x0F, 0x08, 0x06, 0x0A, 0x0C, 0x0B, 0x09, 0x09, 0x7C, 0x0A, 0x09, 0x0B, 0x7C, 0x08, 0x06, 0x0A, 0x0A, 0x09, 0x7E, 0x0B, 0x7A, 0x0B, 0x0C, 0x09, 0x7C, 0x0B, 0x0A, 0x0A, 0x09 };
        private static ReadOnlySpan<byte> MaTran4 => new byte[] { 0x0C, 0x7B, 0x0B, 0x0E, 0x0B, 0x09, 0x0A, 0x0C, 0x0B, 0x0D, 0x0C, 0x0A, 0x0A, 0x0A, 0x0A, 0x0B, 0x0B, 0x09, 0x0C, 0x0E, 0x0B, 0x0A, 0x0A, 0x09, 0x0B, 0x06, 0x09, 0x7C, 0x0B, 0x0A, 0x0A, 0x0D, 0x0A, 0x09, 0x0B, 0x09, 0x0A, 0x0A, 0x0B, 0x7C };
        private static ReadOnlySpan<byte> LayMaTran(int index)
        {
            switch (index)
            {
                case 0: return MaTran1;
                case 1: return MaTran2;
                case 2: return MaTran3;
                default: return MaTran4;
            }
        }
        /// <summary>Lấy nhảy cóc + XOR: index chẵn -> lấy lẻ, index lẻ -> lấy chẵn (đúng logic bản gốc), giải vào bộ đệm stack.</summary>
        private static string GiaiMaMaTran(ReadOnlySpan<byte> encoded)
        {
            Span<char> decoded = stackalloc char[encoded.Length];   // tối đa 40 ký tự
            for (int i = 0; i < encoded.Length; i++)
            {
                int targetIndex = (i % 2 == 0) ? (i + 1) : (i - 1);
                // Lớp khiên bảo vệ (phòng trường hợp mảng bị lệch độ dài lẻ)
                if (targetIndex >= encoded.Length) targetIndex = i;
                decoded[i] = (char)(encoded[targetIndex] ^ SEED_KEY);
            }
            return new string(decoded);
        }
        private static string GiaiMaKhoa(int index) =>
            Module_BaoMatAES.KhoBaoTanVuongGiaiMaHex(
                Module_BaoMatAES.TraLaiTenChoMeoCam(
                    GiaiMaMaTran(LayMaTran(index))));
        public static void XinTraLaiThoiGianNapKeyBase64()
        {
            if (!string.IsNullOrEmpty(ToiCanChuIVMeoCam1_KeyMaHoa)) return;
            lock (_lock)
            {
                if (!string.IsNullOrEmpty(ToiCanChuIVMeoCam1_KeyMaHoa)) return;
                try
                {
                    string k1 = GiaiMaKhoa(0);
                    string k2 = GiaiMaKhoa(1);
                    string k3 = GiaiMaKhoa(2);
                    string k4 = GiaiMaKhoa(3);
                    if (string.IsNullOrEmpty(k1) || string.IsNullOrEmpty(k2) ||
                        string.IsNullOrEmpty(k3) || string.IsNullOrEmpty(k4))
                    {
                        throw new InvalidOperationException("Key giải mã bị lỗi");
                    }
                    // CHỈ GÁN KHI DỮ LIỆU ĐÃ OK. Bản gốc gán k1 (chính là "cờ hiệu" mà các luồng khác kiểm tra để thoát sớm) ĐẦU TIÊN
                    // nên luồng khác có thể thấy k1 đã có và dùng k2..k4 còn rỗng. Nay gán k1 CUỐI CÙNG.
                    ToiCanChuIVMeoCam2_KeyMaHoa = k2;
                    ToiYeuMeoCam1_KeyMaHoa = k3;
                    ToiYeuMeoCam2_KeyMaHoa = k4;
                    ToiCanChuIVMeoCam1_KeyMaHoa = k1;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[Lỗi KEY]: " + ex.Message);
                    // FAIL SAFE: xóa cờ hiệu trước
                    ToiCanChuIVMeoCam1_KeyMaHoa = string.Empty;
                    ToiCanChuIVMeoCam2_KeyMaHoa = string.Empty;
                    ToiYeuMeoCam1_KeyMaHoa = string.Empty;
                    ToiYeuMeoCam2_KeyMaHoa = string.Empty;
                }
            }
        }
        public static void LoiChaoTuSiberia()
        {
            lock (_lock)
            {
                // Bản gốc chỉ kiểm tra khóa số 1; nay kiểm tra đủ 4 khóa trước khi giải
                string k1 = ToiCanChuIVMeoCam1_KeyMaHoa, k2 = ToiCanChuIVMeoCam2_KeyMaHoa;
                string k3 = ToiYeuMeoCam1_KeyMaHoa, k4 = ToiYeuMeoCam2_KeyMaHoa;
                if (string.IsNullOrEmpty(k1) || string.IsNullOrEmpty(k2) ||
                    string.IsNullOrEmpty(k3) || string.IsNullOrEmpty(k4))
                    return;
                try
                {
                    // Tính đủ 4 giá trị vào biến cục bộ rồi mới gán: không để lại trạng thái "nửa vời"
                    string a = Module_BaoMatAES.XuanVeOHokkaido(k1).Trim();
                    string b = Module_BaoMatAES.XuanVeOHokkaido(k2).Trim();
                    string c = Module_BaoMatAES.XuanVeOHokkaido(k3).Trim();
                    string d = Module_BaoMatAES.XuanVeOHokkaido(k4).Trim();
                    ToiCanChuIVMeoCam1 = a;
                    ToiCanChuIVMeoCam2 = b;
                    ToiYeuMeoCam1 = c;
                    ToiYeuMeoCam2 = d;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[Lỗi nạp khóa gốc]: " + ex.Message);
                    ToiCanChuIVMeoCam1 = ToiCanChuIVMeoCam2 = ToiYeuMeoCam1 = ToiYeuMeoCam2 = string.Empty;
                }
            }
        }
        /// <summary>true khi cả 4 khóa gốc đã được nạp. Nơi dùng khóa để mã hóa/giải mã tệp nên kiểm tra trước khi chạy.</summary>
        public static bool KhoaDaSanSang =>
            !string.IsNullOrEmpty(ToiCanChuIVMeoCam1) && !string.IsNullOrEmpty(ToiCanChuIVMeoCam2) &&
            !string.IsNullOrEmpty(ToiYeuMeoCam1) && !string.IsNullOrEmpty(ToiYeuMeoCam2);
        /// <summary>Nạp khóa nếu chưa nạp (idempotent, an toàn đa luồng, gọi nhiều lần không sao).</summary>
        public static void DamBaoKhoaDaNap()
        {
            if (KhoaDaSanSang) return;
            XinTraLaiThoiGianNapKeyBase64();
            LoiChaoTuSiberia();
        }
        // ===================== TIỆN ÍCH SQLITE DÙNG CHUNG =====================
        /// <summary>
        /// Mở kết nối đọc ngắn hạn tới CSDL SQLite.
        /// - Mode=ReadWrite (KHÔNG Create): tệp bị mất giữa lúc kiểm tra và lúc mở sẽ báo lỗi thay vì âm thầm tạo ra tệp .db rỗng 0 byte.
        /// - Pooling=false: Dispose là đóng hẳn tệp. Mặc định Microsoft.Data.Sqlite giữ kết nối trong pool nên tệp .db vẫn bị khóa,
        ///   khiến thao tác sao lưu / khôi phục / ghi đè tệp CSDL báo "tệp đang được sử dụng".
        /// </summary>
        internal static SqliteConnection MoKetNoiDoc(string dbPath)
        {
            var sb = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWrite,
                Pooling = false
            };
            var cn = new SqliteConnection(sb.ToString());
            try
            {
                cn.Open();
                return cn;
            }
            catch
            {
                cn.Dispose();
                throw;
            }
        }
        // ===================== THƯ MỤC HƯỚNG DẪN =====================
        public static void DamBaoThuMucHuongDan() => Directory.CreateDirectory(ThuMucHuongDan);   // idempotent, không cần kiểm tra Exists
        public static void DamBaoHuongDanIndex()
        {
            Directory.CreateDirectory(ThuMucHuongDan);
            // Tệp 0 byte (do mất điện lúc chép) coi như chưa có — bản gốc chỉ File.Exists nên tệp rỗng tồn tại mãi
            if (CoDuLieu(FileHuongDanIndex))
                return;
            string fileGoc = Path.Combine(ThuMucDatabaseBackup, TEN_THU_MUC_HUONG_DAN, TEN_FILE_HUONG_DAN_INDEX);
            if (!File.Exists(fileGoc))
            {
                Debug.WriteLine("Không tìm thấy file hướng dẫn gốc!");
                return;
            }
            SaoChepAnToan(fileGoc, FileHuongDanIndex);
        }
        /// <summary>Chép qua tệp tạm rồi Move thay thế: ngắt giữa chừng không để lại tệp đích rỗng/cụt.</summary>
        private static void SaoChepAnToan(string nguon, string dich)
        {
            string tam = dich + HAU_TO_TAM;
            try
            {
                XoaTepBestEffort(tam);
                File.Copy(nguon, tam, true);
                ThaoGoReadOnly(tam);        // tệp nguồn trong Database Backup bị gắn ReadOnly, bản chép thừa hưởng thuộc tính đó
                ThaoGoReadOnly(dich);
                File.Move(tam, dich, true);
            }
            finally
            {
                XoaTepBestEffort(tam);
            }
        }
        private static void ThaoGoReadOnly(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var attr = File.GetAttributes(path);
                    if ((attr & FileAttributes.ReadOnly) != 0)
                        File.SetAttributes(path, attr & ~FileAttributes.ReadOnly);
                }
            }
            catch { }
        }
        private static void XoaTepBestEffort(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    ThaoGoReadOnly(path);
                    File.Delete(path);
                }
            }
            catch { }
        }
        // ===================== CACHE AVATAR TOÀN CỤC =====================
        public static Image? CachedAvatarAdmin = null;
        public static readonly object AvatarLock = new();
        public static void XoaCacheAvatarToanCuc()
        {
            lock (AvatarLock)
            {
                CachedAvatarAdmin?.Dispose();
                CachedAvatarAdmin = null;
            }
        }
        // ===================== HÀNH TRÌNH KHỞI ĐỘNG =====================
        public static async Task HanhTrinhToiColombiaAsync()
        {
            TaoThuMucNeuChuaCo();
            KiemTraQuyenGhiThuMucDuLieu();
            // Khóa phải sẵn sàng TRƯỚC khi khôi phục CSDL: Module_KhoiTaoCSDL dựng khóa AES từ ToiYeuMeoCam1.
            // Khóa rỗng sẽ giải mã ra tệp rác. Idempotent nên không ảnh hưởng nếu nơi khác đã nạp rồi.
            DamBaoKhoaDaNap();
            if (!KhoaDaSanSang)
                CanhBaoMoiTruong = (CanhBaoMoiTruong == null ? "" : CanhBaoMoiTruong + "\n") + "Không nạp được khóa mã hóa hệ thống.";
            // ConfigureAwait(false): phần việc còn lại (kiểm tra tệp, chép hướng dẫn, ghi nhật ký SQLite) là I/O đĩa,
            // không được chạy lại trên luồng giao diện vì sẽ gây khựng trên máy ổ HDD/cấu hình yếu.
            await Module_KhoiTaoCSDL.BinhMinhOSantoriniAsync().ConfigureAwait(false);
            NapLaiDuongDan();
            try
            {
                DamBaoThuMucHuongDan();
                DamBaoHuongDanIndex();
            }
            catch (Exception ex)
            {
                // Bản gốc để lỗi chép tệp hướng dẫn làm sập cả hành trình khởi động
                Debug.WriteLine("[HanhTrinh] Lỗi chuẩn bị hướng dẫn: " + ex.Message);
            }
            try
            {
                if (KiemTraTrangThaiSanSangCuaHeThongCSDL())
                {
                    var cau = ChonCauNhatKyKhoiDong();
                    Module_NhatKy.GhiNhatKy(
                        taiKhoan: string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM) ? "System (Quyền cao nhất)" : Module_TaiKhoan.TenTaiKhoan_RAM,
                        hanhDong: cau.HanhDong,
                        ghiChu: cau.GhiChu
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        /// <summary>Xác định lại đường dẫn các CSDL (rỗng nếu tệp không có dữ liệu). Gọi lại sau khi khôi phục/ghi đè tệp CSDL trong lúc chương trình đang chạy.</summary>
        public static void NapLaiDuongDan()
        {
            DuongDanCSDL1 = ConDuongToLua("csdl1.db");
            DuongDanCSDL2 = ConDuongToLua("csdl2.db");
            DuongDanCSDL3 = ConDuongToLua("csdl3.db");
            DuongDanCSDL4 = ConDuongToLua("csdl4.db");
            DuongDanCSDL4ex = ConDuongToLua("csdlex.xlsx");
        }
        // ===================== NHẬT KÝ KHỞI ĐỘNG =====================
        private static readonly (string HanhDong, string GhiChu)[] _cauNhatKyKhoiDong =
        {
            ("Khởi tạo dữ liệu thành công!", "Kết luận: Dữ liệu đã được xác nhận và sẵn sàng sử dụng."),
            ("Hoàn tất kiểm tra hệ thống!", "Kết luận: Các thành phần cần thiết đã đáp ứng yêu cầu."),
            ("Xác nhận môi trường hoạt động!", "Kết luận: Sẵn sàng khởi động"),
            ("Thiết lập phiên làm việc thành công!", "Kết luận: Các bước chuẩn bị ban đầu đã hoàn tất."),
            ("Hoàn tất xác thực dữ liệu!", "Kết luận: Quyền truy cập được kiểm tra theo cấu hình hiện hành."),
            ("Khởi tạo chức năng nghiệp vụ!", "Kết luận: Hệ thống đã chuyển sang trạng thái hoạt động."),
            ("Hoàn thành quy trình khởi động!", "Kết luận: Phần mềm sẵn sàng tiếp nhận thao tác người dùng.")
        };
        // "Túi xáo trộn": chọn ngẫu nhiên không lặp cho đến khi dùng hết cả 7 câu (thay cho LINQ + nối chuỗi + HashSet mỗi lần gọi).
        private static readonly object _khoaNhatKy = new();
        private static int[]? _thuTuNhatKy;
        private static int _viTriNhatKy;
        private static int _cuoiCungNhatKy = -1;
        private static (string HanhDong, string GhiChu) ChonCauNhatKyKhoiDong()
        {
            lock (_khoaNhatKy)
            {
                int n = _cauNhatKyKhoiDong.Length;
                if (_thuTuNhatKy == null || _viTriNhatKy >= n)
                {
                    var thuTu = _thuTuNhatKy ??= new int[n];
                    for (int i = 0; i < n; i++) thuTu[i] = i;
                    for (int i = n - 1; i > 0; i--)   // Fisher–Yates
                    {
                        int j = Random.Shared.Next(i + 1);
                        (thuTu[i], thuTu[j]) = (thuTu[j], thuTu[i]);
                    }
                    // Không để câu đầu của vòng mới trùng câu cuối của vòng trước
                    if (n > 1 && thuTu[0] == _cuoiCungNhatKy)
                        (thuTu[0], thuTu[1]) = (thuTu[1], thuTu[0]);
                    _viTriNhatKy = 0;
                }
                int chon = _thuTuNhatKy[_viTriNhatKy++];
                _cuoiCungNhatKy = chon;
                return _cauNhatKyKhoiDong[chon];
            }
        }
        // ===================== THƯ MỤC / TỆP =====================
        private static void TaoThuMucNeuChuaCo()
        {
            try
            {
                Directory.CreateDirectory(ThuMucCoSoDuLieu);     // idempotent
                Directory.CreateDirectory(ThuMucLichSuThiDua);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[TaoThuMucNeuChuaCo] " + ex.Message);
                CanhBaoMoiTruong = "Không tạo được thư mục dữ liệu: " + ex.Message;
            }
        }
        /// <summary>
        /// Phần mềm lưu dữ liệu cạnh tệp chạy. Nếu cài vào nơi chỉ đọc (vd Program Files với người dùng thường) mọi thao tác ghi sẽ lỗi khó hiểu;
        /// thử ghi 1 tệp tạm tự xóa để phát hiện sớm và ghi lại cảnh báo rõ ràng.
        /// </summary>
        private static void KiemTraQuyenGhiThuMucDuLieu()
        {
            try
            {
                string thu = Path.Combine(ThuMucCoSoDuLieu, ".probe_" + Guid.NewGuid().ToString("N"));
                using (new FileStream(thu, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose)) { }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[KiemTraQuyenGhi] " + ex.Message);
                CanhBaoMoiTruong = "Thư mục dữ liệu không có quyền ghi: " + ThuMucCoSoDuLieu;
            }
        }
        /// <summary>Tệp tồn tại và khác 0 byte.</summary>
        private static bool CoDuLieu(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return false;
                var fi = new FileInfo(path);
                return fi.Exists && fi.Length > 0;
            }
            catch
            {
                return false;
            }
        }
        private static string ConDuongToLua(string tenFile)
        {
            string path = Path.Combine(ThuMucCoSoDuLieu, tenFile);
            return CoDuLieu(path) ? path : string.Empty;   // tệp 0 byte coi như chưa có (nhất quán với Module_KhoiTaoCSDL)
        }
        // ===================== XÁC THỰC TỆP =====================
        /// <summary>
        /// Tệp tồn tại VÀ mở đọc được (chia sẻ ReadWrite|Delete nên không tranh chấp với SQLite/Excel đang mở).
        /// Bản gốc có chú thích "FileShare.ReadWrite" nhưng thực tế chỉ gọi File.Exists, trái với tên hàm.
        /// </summary>
        public static bool TonTaiVaMoDuoc(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete, 1, FileOptions.None);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static bool KiemTraTonTai(string tenFileCSDL)
        {
            // Chỉ lấy phần tên tệp: chặn "..\" và đường dẫn tuyệt đối (Path.Combine sẽ bỏ qua thư mục gốc nếu tham số 2 là đường dẫn tuyệt đối)
            string ten = Path.GetFileName(tenFileCSDL ?? string.Empty);
            if (ten.Length == 0) return false;
            return File.Exists(Path.Combine(ThuMucCoSoDuLieu, ten));
        }
        public static bool KiemTraTrangThaiSanSangCuaHeThongCSDL()
        {
            return
                CoDuLieu(DuongDanCSDL1) &&
                CoDuLieu(DuongDanCSDL2) &&
                CoDuLieu(DuongDanCSDL3) &&
                CoDuLieu(DuongDanCSDL4);
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
                _ => string.Empty,
            };
        }
    }
    internal static class Module_TaiKhoan
    {
        public static string TenTaiKhoan_RAM = string.Empty;
        public static string MatKhau_RAM = string.Empty;
        /// <summary>Giải mã không bao giờ ném lỗi / không bao giờ trả null.</summary>
        private static string GiaiMaAnToan(string? giaTri)
        {
            if (string.IsNullOrWhiteSpace(giaTri)) return string.Empty;
            try { return Module_BaoMatAES.GiaiMa(giaTri) ?? string.Empty; }
            catch { return string.Empty; }
        }
        public static bool NapTaiKhoanTuCSDL(int id = 1)
        {
            string ten = string.Empty, matKhau = string.Empty;
            try
            {
                string dbPath = Module_DanduongGPS.DuongDanCSDL1;
                if (!string.IsNullOrWhiteSpace(dbPath))
                {
                    using var conn = Module_DanduongGPS.MoKetNoiDoc(dbPath);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT TenTaiKhoan, MatKhau FROM Admin WHERE ID = @id LIMIT 1";
                    cmd.Parameters.Add("@id", SqliteType.Integer).Value = id;
                    using var rd = cmd.ExecuteReader(System.Data.CommandBehavior.SingleRow);
                    if (rd.Read())
                    {
                        ten = GiaiMaAnToan(rd.IsDBNull(0) ? null : rd.GetString(0));
                        matKhau = GiaiMaAnToan(rd.IsDBNull(1) ? null : rd.GetString(1));
                    }
                }
            }
            catch
            {
                ten = string.Empty;
                matKhau = string.Empty;
            }
            // Giải mã thất bại (sai khóa/dữ liệu hỏng) làm GiaiMa trả "" → bản gốc vẫn trả true với tài khoản/mật khẩu rỗng,
            // có thể khiến màn hình đăng nhập coi "rỗng == rỗng" là hợp lệ. Nay yêu cầu tên tài khoản phải có giá trị.
            bool ok = !string.IsNullOrWhiteSpace(ten);
            // Gán cả hai cùng lúc từ biến cục bộ: luồng khác không thấy tên mới đi kèm mật khẩu cũ
            TenTaiKhoan_RAM = ok ? ten : string.Empty;
            MatKhau_RAM = ok ? matKhau : string.Empty;
            return ok;
        }
        public static string LayThongTinPhienBanVaToken()
        {
            try
            {
                string phienBan = Module_PhienBan.SoftwareVersion;
                string ngayCapNhat = Module_PhienBan.NgayThangNamCapNhat;
                string tokenGiaiMa;
                string csdlPath = Module_DanduongGPS.DuongDanCSDL1;
                if (!string.IsNullOrEmpty(csdlPath) && File.Exists(csdlPath))
                {
                    using var conn = Module_DanduongGPS.MoKetNoiDoc(csdlPath);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT Token FROM Admin WHERE ID = 1";
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        // GIỮ NGUYÊN: hiển thị giá trị lưu trong CSDL (biến tên "tokenGiaiMa" nhưng bản gốc không giải mã).
                        tokenGiaiMa = Convert.ToString(result, CultureInfo.InvariantCulture) ?? "Token không hợp lệ";
                    }
                    else { tokenGiaiMa = "Chưa thiết lập Token"; }
                }
                else { tokenGiaiMa = "Không tìm thấy cơ sở dữ liệu"; }
                return "THÔNG TIN PHẦN MỀM\n\n" +
                       $"Ngày khởi công: 07/11/2025\n" +
                       $"Môi trường phát triển: Visual Studio 2022 + Visual Studio 2026\n" +
                       $"Admin - Nhóm phát triển: TrungKien\n" +
                       $"Phiên bản hiện tại số: {phienBan}\n" +
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
                using var cn = Module_DanduongGPS.MoKetNoiDoc(db);
                using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT DoiTuong FROM PhienBan_DoiTuong LIMIT 1";
                object? result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value) return "";
                string chuoi = (Convert.ToString(result, CultureInfo.InvariantCulture) ?? "").Trim();
                if (chuoi.Length == 0) return "";
                string doiTuong = GiaiMaAnToan(chuoi).Trim();
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
            const string KHONG_XAC_DINH = "Không xác định";
            string dbPath = Module_DanduongGPS.DuongDanCSDL2;
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return KHONG_XAC_DINH;
            try
            {
                using var conn = Module_DanduongGPS.MoKetNoiDoc(dbPath);
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT TenTieuDoan FROM ThongTin WHERE ID=1";
                var result = cmd.ExecuteScalar();
                string raw = (Convert.ToString(result, CultureInfo.InvariantCulture) ?? "").Trim();
                if (raw.Length == 0) return KHONG_XAC_DINH;
                // GiaiMa KHÔNG ném lỗi mà trả "" khi thất bại, nên khối catch của bản gốc không bao giờ chạy:
                // dữ liệu cũ dạng chữ thường bị trả về rỗng. Nay: giải mã được thì dùng; thất bại thì chữ thường dùng tiếp,
                // còn chuỗi mã hóa "v2|..." thì coi là không xác định (không in chuỗi mã lên giao diện).
                string dec = GiaiMaAnToan(raw).Trim();
                if (dec.Length > 0) return dec;
                return raw.StartsWith("v2|", StringComparison.Ordinal) ? KHONG_XAC_DINH : raw;
            }
            catch { return KHONG_XAC_DINH; }
        }
        // ===================== SỰ KIỆN =====================
        public static event EventHandler? OnPhienBanThayDoi;
        // Một người nghe ném lỗi không được làm các người nghe còn lại (hoặc nơi phát sự kiện) bị sập
        public static void ThongBaoPhienBanThayDoi()
        {
            var h = OnPhienBanThayDoi;
            if (h == null) return;
            foreach (Delegate d in h.GetInvocationList())
            {
                try { ((EventHandler)d)(null, EventArgs.Empty); }
                catch (Exception ex) { Debug.WriteLine("[OnPhienBanThayDoi] " + ex.Message); }
            }
        }
        public static event Action? OnDoiTuongPhanMemChanged;
        // Sự kiện C# chỉ được phát từ bên trong lớp khai báo; bản gốc khai báo sự kiện này nhưng KHÔNG có hàm phát nên không bao giờ kích hoạt.
        public static void ThongBaoDoiTuongPhanMemThayDoi()
        {
            var h = OnDoiTuongPhanMemChanged;
            if (h == null) return;
            foreach (Delegate d in h.GetInvocationList())
            {
                try { ((Action)d)(); }
                catch (Exception ex) { Debug.WriteLine("[OnDoiTuongPhanMemChanged] " + ex.Message); }
            }
        }
    }
}