using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
namespace PhanMemThiDua2026
{
    internal static class Module_HeThong
    {
        // 1. Dùng chung Font cho toàn hệ thống
        public const string TenFontHeThong = "Segoe UI";
        public const string Font_Times_New_Roman = "Times New Roman";
        public const string TT_DANG_CONG_TAC = "Đang công tác";
        public const string TT_CHUYEN_CONG_TAC = "Chuyển công tác";
        public const string Tu_Dong_Chi = "Đồng chí";
        public const string Tu_dong_chi = "đồng chí";
        public const string Goi_Y_Thao_Tac = "Gợi ý thao tác";
        public const string Goi_Y_Dang_Nhap = "Gợi ý đăng nhập";
        public const string PL_CSTD = "CSTĐ";
        public const string PL_CSTT = "CSTT";
        public const string PL_HTNV = "HTNV";
        public const string PL_KHTNV = "KHTNV";
        public const string PL_KHONG_PL = "Không PL";
        public const string Loai_1 = "Loại 1";
        public const string Loai_2 = "Loại 2";
        public const string Loai_3 = "Loại 3";
        public const string Loai_4 = "Loại 4";
        // Tên cột thực tế trong SQLite
        public const string COL_LOAI_1 = "Loai_1";
        public const string COL_LOAI_2 = "Loai_2";
        public const string COL_LOAI_3 = "Loai_3";
        public const string COL_LOAI_4 = "Loai_4";
        public const string Tat_Ca = "Tất cả";
        //Nhóm xếp loại đơn vị
        public const string XLDV_DVQT = "ĐVQT";
        public const string XLDV_DVTT = "ĐVTT";
        public const string XLDV_HTNV = "HTNV";
        public const string XLDV_KHTNV = "KHTNV";
        public const string XLDV_KHONG_XET = "Không PL";
        public const string Thao_Tac = "Thao tác";
        public const string Nhap_Tim_Kiem = "Nhập tìm kiếm";
        public const string Sy_Quan = "Sỹ quan";
        public const string Ha_Sy_Quan = "Hạ sỹ quan";
        public const string Chien_Sy_Nghia_Vu = "Chiến sĩ nghĩa vụ";
        public const string Ngay_Thang_Nam = "dd/MM/yyyy";
        private const int ID_NAM_HE_THONG = 1;
        private static string DbPath => Module_DanduongGPS.DuongDanCSDL2;
        // Mảng chuẩn 5 loại (Dành cho Form46_ThongKeThiDuaNamCu - CSDL Năm)
        public static readonly object[] DanhSach_PhanLoai_Chuan = {
            PL_CSTD, PL_CSTT, PL_HTNV, PL_KHTNV, PL_KHONG_PL
        };
        // Mảng có thêm chuỗi rỗng ở đầu (Dành cho Form22, Form30)
        public static readonly object[] DanhSach_PhanLoai_CoRong = {
            "", PL_CSTD, PL_CSTT, PL_HTNV, PL_KHTNV
        };
        // Mảng có thêm "Tất cả" (Dành cho Form46 - Bộ lọc tìm kiếm)
        public static readonly object[] DanhSach_PhanLoai_TatCa = {
            "Tất cả", PL_CSTD, PL_CSTT, PL_HTNV, PL_KHTNV, PL_KHONG_PL
        };
        // ==========================================
        // ⭐ WINDOWS SHELL API VÀ QUẢN LÝ GDI HANDLE
        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);
        // Lê Trung Kiên -  Yêu mèo cam 🐈
        private const int SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;
        /// <summary>
        /// Wrapper đóng gói lời gọi API Windows Explorer để dễ quản lý và bắt lỗi
        /// </summary>
        private static void LamMoiExplorer()
        {
            try
            {
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi Shell API]: {ex.Message}");
            }
        }
        // ⭐ GÁN ICON TÙY BIẾN TỪ RESOURCES VÀO THƯ MỤC XUẤT FILE       
        public static void GanIconThuMuc(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return;
            IntPtr hIcon = IntPtr.Zero;
            try
            {
                string iconPath = Path.Combine(folderPath, "IconThuMuc.ico");
                string iniPath = Path.Combine(folderPath, "desktop.ini");
                // 1. Trích xuất file IconThuMuc.ico từ Resources
                if (!File.Exists(iconPath))
                {
                    object resObj = Properties.Resources.IconThuMuc;
                    if (resObj is Icon ico)
                    {
                        using var fs = new FileStream(iconPath, FileMode.Create, FileAccess.Write, FileShare.None);
                        ico.Save(fs);
                    }
                    else if (resObj is byte[] bytes)
                    {
                        File.WriteAllBytes(iconPath, bytes);
                    }
                    else if (resObj is Bitmap bmp)
                    {
                        // Kiểm soát vòng đời HICON chuẩn kỹ sư, tránh rò rỉ GDI
                        hIcon = bmp.GetHicon();
                        using var tempIcon = Icon.FromHandle(hIcon);
                        using var fs = new FileStream(iconPath, FileMode.Create, FileAccess.Write, FileShare.None);
                        tempIcon.Save(fs);
                    }
                    if (File.Exists(iconPath))
                        File.SetAttributes(iconPath, FileAttributes.Hidden | FileAttributes.System);
                }
                // 2. Tạo/ghi đè desktop.ini an toàn (Dùng ASCII để tránh xung đột Encoding Locale)
                if (File.Exists(iniPath))
                    File.SetAttributes(iniPath, FileAttributes.Normal);
                string iniContent = "[.ShellClassInfo]\r\n" +
                                    "IconResource=IconThuMuc.ico,0\r\n" +
                                    "[ViewState]\r\n" +
                                    "Mode=\r\n" +
                                    "Vid=\r\n" +
                                    "FolderType=Generic\r\n";
                File.WriteAllText(iniPath, iniContent, Encoding.ASCII);
                File.SetAttributes(iniPath, FileAttributes.Hidden | FileAttributes.System);
                // 3. Gán cờ ReadOnly cho thư mục để Windows tiến hành đọc desktop.ini
                var folderInfo = new DirectoryInfo(folderPath);
                folderInfo.Attributes |= FileAttributes.ReadOnly;
                // 4. Báo hiệu Explorer cập nhật giao diện
                LamMoiExplorer();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi gán icon thư mục]: {ex.Message}");
            }
            finally
            {
                // Giải phóng dứt điểm handle native nếu có sử dụng
                if (hIcon != IntPtr.Zero)
                    DestroyIcon(hIcon);
            }
        }
        // ==========================================
        // 1. Lấy năm hệ thống
        // ==========================================
        public static int LayNamHeThong()
        {
            try
            {
                if (!File.Exists(DbPath))
                    return DateTime.Now.Year;
                using var conn = new SqliteConnection($"Data Source={DbPath}");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT NAM 
                    FROM NamHeThong 
                    WHERE ID = @id 
                    LIMIT 1;";
                cmd.Parameters.AddWithValue("@id", ID_NAM_HE_THONG);
                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    int nam = Convert.ToInt32(result);
                    // Đảm bảo dữ liệu trong CSDL luôn hợp lý trước khi nạp vào hệ thống
                    if (nam >= 2000 && nam <= 2100)
                        return nam;
                }
            }
            catch (Exception ex)
            {
                // Fallback mượt mà khi CSDL khóa hoặc hỏng
                Debug.WriteLine($"[Lỗi CSDL - LayNamHeThong]: {ex.Message}");
            }
            return DateTime.Now.Year;
        }
        // 2. Lưu năm hệ thống
        /// Yêu Mèo Cam 🐈
        public static void LuuNamHeThong(int nam)
        {
            // Chặn dữ liệu rác/lỗi từ đầu vào
            if (nam < 2000 || nam > 2100)
                throw new ArgumentOutOfRangeException(nameof(nam), "Năm hệ thống không hợp lệ (Giới hạn: 2000 - 2100).");
            try
            {
                using var conn = new SqliteConnection($"Data Source={DbPath}");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT OR REPLACE INTO NamHeThong (ID, NAM)
                    VALUES (@id, @nam);";
                cmd.Parameters.AddWithValue("@id", ID_NAM_HE_THONG);
                cmd.Parameters.AddWithValue("@nam", nam);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi CSDL - LuuNamHeThong]: {ex.Message}");
                throw new Exception($"Lỗi gián đoạn khi lưu năm hệ thống: {ex.Message}", ex);
            }
        }
        // 3. Lấy danh sách biên độ năm
        public static List<int> LayDanhSachNam(int bienDo = 5)
        {
            int namTrungTam = LayNamHeThong();
            var ds = new List<int>(bienDo * 2 + 1); // Cấp phát tĩnh trước để tối ưu phân bổ vùng nhớ
            int min = namTrungTam - bienDo;
            int max = namTrungTam + bienDo;
            for (int i = min; i <= max; i++)
            {
                ds.Add(i);
            }
            return ds;
        }
        public static string TenDonViHienTai { get; private set; } = string.Empty;
        public static event Action SuKienThayDoiTenDonVi;
        public static string LayTenDonViChuan(bool lamMoiTuCSDL = false)
        {
            // Nếu đã có trong RAM và không yêu cầu đọc lại từ CSDL -> Trả về luôn
            if (!lamMoiTuCSDL && !string.IsNullOrEmpty(TenDonViHienTai))
                return TenDonViHienTai;
            string dbPath = Module_DanduongGPS.DuongDanCSDL2;
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
                return string.Empty;
            try
            {
                using var cn = new SqliteConnection($"Data Source={dbPath}");
                cn.Open();
                using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT TenTieuDoan FROM ThongTin ORDER BY ID ASC LIMIT 1";
                var result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    return string.Empty;
                string rawText = result.ToString()!;
                string tenDonViGiaiMa = string.Empty;
                try
                {
                    tenDonViGiaiMa = Module_BaoMatAES.GiaiMa(rawText);
                }
                catch
                {
                    tenDonViGiaiMa = rawText;
                }
                if (string.IsNullOrWhiteSpace(tenDonViGiaiMa))
                    return string.Empty;
                // Chuẩn hóa: "TIỂU ĐOÀN 2" -> "Tiểu đoàn 2"
                string lower = tenDonViGiaiMa.Trim().ToLower(new CultureInfo("vi-VN"));
                if (lower.Length == 0) return string.Empty;
                TenDonViHienTai = char.ToUpper(lower[0], new CultureInfo("vi-VN")) + lower.Substring(1);
                // Bắn sự kiện thông báo cho các Form đang lắng nghe
                SuKienThayDoiTenDonVi?.Invoke();
                return TenDonViHienTai;
            }
            catch
            {
                return string.Empty;
            }
        }
        //        //ở namespace PhanMemThiDua2026
        //{
        //    internal static class Module_HeThong
        //        {
        //============= Ánh xạ tên phân loại chuẩn sang tên cột SQLite =================
        // 🎯 QUẢN LÝ BẢNG CheDo_XetThiDuaNam ("Tháng" / "Năm")
        // 1. Cache lưu giá trị trong RAM để truy vấn siêu nhanh
        private static string _cheDoXetThiDuaNamCache = string.Empty;
        /// <summary>
        /// Sự kiện phát ra toàn hệ thống khi Chế độ xét thi đua bị thay đổi.
        /// Tất cả các Form đang mở có thể đăng ký sự kiện này để tự động cập nhật lại UI.
        /// </summary>
        public static event Action SuKienThayDoiCheDoXetThiDua;
        /// <summary>
        /// HÀM 1: Đọc giá trị từ CSDL2 (ID = 1, cột ChoPhepCheDoXetThiDuaNam).
        /// Trả về "Năm" hoặc "Tháng" (văn bản thuần không mã hóa).
        /// </summary>
        /// <param name="lamMoiTuCSDL">True: Buộc đọc lại từ CSDL2; False: Lấy từ RAM Cache</param>
        public static string LayCheDoXetThiDuaNam(bool lamMoiTuCSDL = false)
        {
            if (!lamMoiTuCSDL && !string.IsNullOrEmpty(_cheDoXetThiDuaNamCache))
                return _cheDoXetThiDuaNamCache;
            string dbPath = Module_DanduongGPS.DuongDanCSDL2;
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
            {
                _cheDoXetThiDuaNamCache = "Tháng";
                return _cheDoXetThiDuaNamCache;
            }
            try
            {
                using var conn = new SqliteConnection($"Data Source={dbPath}");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT ChoPhepCheDoXetThiDuaNam FROM CheDo_XetThiDuaNam WHERE ID = 1 LIMIT 1;";
                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    string val = result.ToString()?.Trim() ?? "";
                    if (val.Equals("Năm", StringComparison.OrdinalIgnoreCase))
                        _cheDoXetThiDuaNamCache = "Năm";
                    else
                        _cheDoXetThiDuaNamCache = "Tháng";
                }
                else
                {
                    _cheDoXetThiDuaNamCache = "Tháng";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi CSDL - LayCheDoXetThiDuaNam]: {ex.Message}");
                _cheDoXetThiDuaNamCache = "Tháng";
            }
            return _cheDoXetThiDuaNamCache;
        }
        /// <summary>
        /// HÀM 2: Kiểm tra nhanh xem hệ thống có đang ở Chế độ xét thi đua "Năm" hay không.
        /// </summary>
        public static bool IsCheDoXetThiDuaNam(bool lamMoiTuCSDL = false)
        {
            return LayCheDoXetThiDuaNam(lamMoiTuCSDL).Equals("Năm", StringComparison.OrdinalIgnoreCase);
        }
        /// <summary>
        /// HÀM 3: Dành cho Form4 (hoặc Form Cấu hình) gọi khi người dùng bấm LƯU.
        /// Ghi trực tiếp text thuần "Năm" hoặc "Tháng" vào ID = 1, cập nhật Cache và phát Event toàn hệ thống.
        /// </summary>
        /// <param name="giaTri">Truyền vào "Năm" hoặc "Tháng"</param>
        public static bool LuuCheDoXetThiDuaNam(string giaTri)
        {
            string dbPath = Module_DanduongGPS.DuongDanCSDL2;
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
                return false;
            // Chuẩn hóa chuỗi lưu trữ
            string valToSave = giaTri?.Trim().Equals("Năm", StringComparison.OrdinalIgnoreCase) == true ? "Năm" : "Tháng";
            try
            {
                using var conn = new SqliteConnection($"Data Source={dbPath}");
                conn.Open();
                // 1. Tạo bảng nếu chưa tồn tại
                using (var cmdCreateTable = conn.CreateCommand())
                {
                    cmdCreateTable.CommandText = @"
                        CREATE TABLE IF NOT EXISTS CheDo_XetThiDuaNam (
                            ID INTEGER NOT NULL PRIMARY KEY,
                            ChoPhepCheDoXetThiDuaNam TEXT
                        );";
                    cmdCreateTable.ExecuteNonQuery();
                }
                // 2. Ghi đè/Chèn dữ liệu text thuần tại ID = 1
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT OR REPLACE INTO CheDo_XetThiDuaNam (ID, ChoPhepCheDoXetThiDuaNam)
                        VALUES (1, @val);";
                    cmd.Parameters.AddWithValue("@val", valToSave);
                    cmd.ExecuteNonQuery();
                }
                // 3. Cập nhật lại RAM Cache ngay lập tức
                _cheDoXetThiDuaNamCache = valToSave;
                // 4. 🚀 BẮN SỰ KIỆN THÔNG BÁO CHO TẤT CẢ CÁC FORM ĐANG MỞ RECEIVE GIÁ TRỊ MỚI
                SuKienThayDoiCheDoXetThiDua?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi CSDL - LuuCheDoXetThiDuaNam]: {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// HÀM 4: Kiểm tra xem có kích hoạt chế độ Ánh xạ hay không.
        /// Điều kiện: Không phải phiên bản "Tân binh" (tức phiên bản CBCS) VÀ Chế độ = "Năm".
        /// </summary>
        public static bool IsKichHoatAnhXa(bool lamMoiTuCSDL = false)
        {
            bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
            bool isCBCS = !laTanBinh;
            return isCBCS && IsCheDoXetThiDuaNam(lamMoiTuCSDL);
        }
        public static async Task<bool> LuuCheDoXetThiDuaNamAsync(string giaTri, SqliteConnection conn = null, SqliteTransaction tran = null)
        {
            string valToSave = giaTri?.Trim().Equals("Năm", StringComparison.OrdinalIgnoreCase) == true ? "Năm" : "Tháng";
            try
            {
                bool isExternalConn = conn != null;
                if (!isExternalConn)
                {
                    string dbPath = Module_DanduongGPS.DuongDanCSDL2;
                    if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return false;
                    conn = new SqliteConnection($"Data Source={dbPath}");
                    await conn.OpenAsync();
                }
                using (var cmd = conn.CreateCommand())
                {
                    if (tran != null) cmd.Transaction = tran;
                    cmd.CommandText = @"
                INSERT OR REPLACE INTO CheDo_XetThiDuaNam (ID, ChoPhepCheDoXetThiDuaNam)
                VALUES (1, @val);";
                    cmd.Parameters.AddWithValue("@val", valToSave);
                    await cmd.ExecuteNonQueryAsync();
                }
                if (!isExternalConn) conn.Dispose();
                // Cập nhật Cache RAM & Bắn Sự kiện
                _cheDoXetThiDuaNamCache = valToSave;
                SuKienThayDoiCheDoXetThiDua?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi CSDL - LuuCheDoXetThiDuaNamAsync]: {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// Sự kiện phát ra toàn hệ thống khi Tháng/Năm hoặc Thông tin cấu hình bị thay đổi
        /// </summary>
        public static event Action SuKienThayDoiThoiGianHeThong;
        /// <summary>
        /// Hàm phát thông báo làm mới thời gian hệ thống và xóa Cache dữ liệu chung
        /// </summary>
        public static void ThongBaoThayDoiThoiGian()
        {
            // 1. Xóa Cache dữ liệu cũ nếu ứng dụng có dùng DataCache
            // DataCache.Clear(); // Bỏ comment nếu project bạn có DataCache.Clear() hoặc DataCache.IsLoaded = false;
            // 2. Bắn sự kiện cho các Form đang mở (Form6, FormMain,...) reload
            SuKienThayDoiThoiGianHeThong?.Invoke();
        }
        public static class UIHelper
        {
            [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075",
                Justification = "DoubleBuffered luôn tồn tại trên mọi Control kế thừa từ System.Windows.Forms.Control")]
            public static void EnableDoubleBuffer(Control ctrl)
            {
                if (ctrl == null) return;
                var pi = typeof(Control).GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                pi?.SetValue(ctrl, true, null);
            }
        }
        // Sự kiện báo hiệu toàn hệ thống khi có thay đổi dữ liệu
        public static event EventHandler? OnThoiGianChanged;
        public static void ThongBaoThayDoiThoiGianFrom4()
        {
            OnThoiGianChanged?.Invoke(null, EventArgs.Empty);
        }
        /// Đọc thông tin Chế độ, Tháng, Năm từ CSDL2
        public static async Task<(string cheDo, string thang, string nam)> LayThongTinThoiGianAsync(string csdlPath)
        {
            string cheDo = "Tháng";
            string thang = "";
            string nam = DateTime.Now.Year.ToString(); // Mặc định năm hiện tại nếu rỗng
            using (var conn = new SqliteConnection($"Data Source={csdlPath}"))
            {
                await conn.OpenAsync();
                // 1. Đọc Năm hệ thống (Bảng NamHeThong - Không mã hóa)
                using (var cmdNam = new SqliteCommand("SELECT NAM FROM NamHeThong WHERE ID = 1", conn))
                {
                    var valNam = await cmdNam.ExecuteScalarAsync();
                    if (valNam != null && valNam != DBNull.Value)
                    {
                        nam = valNam.ToString() ?? nam;
                    }
                }
                // 2. Đọc Chế độ xét thi đua (Bảng CheDo_XetThiDuaNam - Không mã hóa)
                using (var cmdCheDo = new SqliteCommand("SELECT ChoPhepCheDoXetThiDuaNam FROM CheDo_XetThiDuaNam WHERE ID = 1", conn))
                {
                    var valCheDo = await cmdCheDo.ExecuteScalarAsync();
                    if (valCheDo != null && valCheDo != DBNull.Value)
                    {
                        cheDo = valCheDo.ToString() ?? "Tháng";
                    }
                }
                // 3. Nếu là chế độ "Tháng" -> Đọc & Giải mã Tháng từ Bảng ThongTin (Có mã hóa AES)
                if (cheDo.Equals("Tháng", StringComparison.OrdinalIgnoreCase))
                {
                    using (var cmdThang = new SqliteCommand("SELECT Thang FROM ThongTin WHERE ID = 1", conn))
                    {
                        var valThang = await cmdThang.ExecuteScalarAsync();
                        if (valThang != null && valThang != DBNull.Value)
                        {
                            string thangMaHoa = valThang.ToString() ?? "";
                            // GIẢI MÃ AES
                            thang = Module_BaoMatAES.GiaiMa(thangMaHoa);
                        }
                    }
                }
            }
            return (cheDo, thang, nam);
        }
    } 
}