using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing.Drawing2D;

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
        // 1. Lấy năm hệ thống
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
        /// <summary>
        /// Cập nhật trạng thái hiển thị của Menu Item dựa trên phiên bản phần mềm (Tân binh / CBCS)
        /// </summary>
        // 1. Khai báo Event thông báo khi phiên bản/chế độ thay đổi
        public static event EventHandler OnPhienBanChanged;
        // 2. Hàm kích hoạt Event này (gọi từ Form 12 khi lưu cài đặt)
        public static void ThongBaoThayDoiPhienBan()
        {
            OnPhienBanChanged?.Invoke(null, EventArgs.Empty);
        }
        /// <summary>
        /// Hàm kiểm tra và ẩn/hiện ToolStripMenuItem dựa trên phiên bản phần mềm
        /// </summary>
        /// <param name="menuItem">ToolStripMenuItem cần ẩn/hiện</param>
        public static void CapNhatMenuPhanLoaiThang(ToolStripMenuItem menuItem)
        {
            if (menuItem == null) return;
            string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? string.Empty;
            bool laTanBinh = phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase);
            // Tân binh -> Hiện (true), CBCS -> Ẩn (false)
            menuItem.Visible = laTanBinh;
        }
        /// <summary>
        /// ============================ QUẢN LÝ TÊN ĐƠN VỊ HIỆN TẠI =========================
        /// </summary>
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
        public static void BocBoTronGiaoDien(Control c, int radius = 12)
        {
            var parent = c.Parent;
            if (parent == null) return;
            var host = new RoundedPanel
            {
                Radius = radius,
                Bounds = c.Bounds,
                Anchor = c.Anchor,
                Dock = c.Dock,
                Margin = c.Margin,
                TabIndex = c.TabIndex
            };
            int idx = parent.Controls.GetChildIndex(c);
            parent.SuspendLayout();
            parent.Controls.Remove(c);
            // Bỏ viền vuông của control gốc
            if (c is RichTextBox rtb) rtb.BorderStyle = BorderStyle.None;
            if (c is ListBox lb) lb.BorderStyle = BorderStyle.None;
            c.BackColor = host.FillColor;
            c.Dock = DockStyle.Fill;
            host.Controls.Add(c);
            parent.Controls.Add(host);
            parent.Controls.SetChildIndex(host, idx);
            parent.ResumeLayout();
        }
        public static event Action? SuKienThayDoiDanhSachPhanLoai;
        public static void ThongBaoThayDoiPhanLoai()
        {
            SuKienThayDoiDanhSachPhanLoai?.Invoke();
        }

        ///Lắng nghe và phát hiện thư mục Database-PhanMemThiDua2026 ngoài Desktop
        ///
        /// 

        public static partial class ThongBaoHelper   // đổi tên theo class hiện tại của bạn
        {
            private static void HienThiThongBaoGocDuoiPhai(Form2_FormCha formCha, string duongDanThuMuc)
            {
                var popup = new ThongBaoPopup(
                    "Phát hiện dữ liệu chuyển giao",
                    "Phát hiện thư mục dữ liệu \"Database-PhanMemThiDua2026\" trên Desktop. Bạn có muốn nạp dữ liệu này vào hệ thống ngay không?",
                    formCha.DeviceDpi / 96f);

                // Căn sát góc dưới bên phải của màn hình đang chứa form cha
                popup.DatViTriGocDuoiPhai(Screen.FromControl(formCha).WorkingArea, 16);

                popup.NapNgay += (s, e) =>
                {
                    popup.Close();
                    formCha.OpenChildForm<Form12>("Trang Cài đặt");
                    formCha.OpenChildForm<Form31_ChuyenGiaoDuLieu>("Trang Chuyển giao dữ liệu");
                };

                popup.Show(formCha);
            }
            // ===================== Helper bo góc =====================
            private static GraphicsPath RoundedRect(Rectangle r, int radius)
            {
                int d = radius * 2;
                var path = new GraphicsPath();
                path.AddArc(r.X, r.Y, d, d, 180, 90);
                path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                return path;
            }

            // ===================== Nút bo tròn =====================
            private class RoundedButton : Control
            {
                private bool _hover;
                public bool Primary { get; set; }
                public int Radius { get; set; } = 10;

                public RoundedButton()
                {
                    SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                             ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                             ControlStyles.SupportsTransparentBackColor, true);
                    Cursor = Cursors.Hand;
                    Font = new Font("Segoe UI Semibold", 9.5F);
                    BackColor = Color.Transparent;
                }

                protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
                protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

                protected override void OnPaint(PaintEventArgs e)
                {
                    var g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    Color fill, border, text;
                    if (Primary)
                    {
                        fill = _hover ? Color.FromArgb(67, 56, 202) : Color.FromArgb(79, 70, 229); // indigo
                        border = fill;
                        text = Color.White;
                    }
                    else
                    {
                        fill = _hover ? Color.FromArgb(243, 244, 246) : Color.White;
                        border = Color.FromArgb(209, 213, 219);
                        text = Color.FromArgb(55, 65, 81);
                    }

                    var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                    using (var path = RoundedRect(rect, Radius))
                    using (var b = new SolidBrush(fill))
                    using (var p = new Pen(border))
                    {
                        g.FillPath(b, path);
                        g.DrawPath(p, path);
                    }
                    TextRenderer.DrawText(g, Text, Font, rect, text,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }

            // ===================== Popup =====================


            private class ThongBaoPopup : Form
            {
                // ===== Win32: cửa sổ layered (alpha từng pixel) để có bóng mềm + bo góc mượt =====
                private const int WS_EX_LAYERED = 0x80000;
                private const int WS_EX_TOOLWINDOW = 0x80;
                private const int WS_EX_NOACTIVATE = 0x08000000;
                private const int ULW_ALPHA = 0x02;
                private const byte AC_SRC_OVER = 0x00;
                private const byte AC_SRC_ALPHA = 0x01;

                [StructLayout(LayoutKind.Sequential)] private struct POINT { public int x, y; }
                [StructLayout(LayoutKind.Sequential)] private struct SIZE { public int cx, cy; }
                [StructLayout(LayoutKind.Sequential, Pack = 1)]
                private struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }

                [DllImport("user32.dll", SetLastError = true)]
                private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize,
                    IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);
                [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
                [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
                [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hDC);
                [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
                [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
                [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr hObject);

                // ===== Cấu hình =====
                private const int TongMs = 10000;   // thời gian tự đóng (ms). Rê chuột vào thẻ thì tạm dừng
                private const int AnimMs = 220;     // thời gian hiệu ứng trượt vào

                private enum Hit { None, Ok, Cancel, Close }

                public event EventHandler? NapNgay;

                private readonly float _s;          // hệ số DPI
                private readonly int _m;            // lề trong suốt quanh thẻ để chứa bóng đổ
                private readonly string _tieuDe, _noiDung;
                private const string ChuOk = "Nạp ngay";
                private const string ChuHuy = "Để sau";

                private readonly Font _fTitle, _fMsg, _fBtn;
                private readonly StringFormat _sf, _sfCenter;

                private readonly Rectangle _rcCard, _rcIcon, _rcClose, _rcOk, _rcCancel;
                private readonly RectangleF _rcTitle, _rcMsg;

                private readonly Bitmap _shadow, _canvas;
                private readonly Graphics _g;

                private readonly System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer { Interval = 16 };
                private readonly Stopwatch _sw = new Stopwatch();
                private long _lastMs;
                private double _elapsed;            // ms đã đếm ngược (không tính lúc rê chuột vào)
                private double _anim;               // ms hiệu ứng vào
                private bool _dirty = true;
                private int _lastBar = -1;
                private bool _trongCard;
                private Hit _hit = Hit.None;
                private int _baseTop;

                private int S(int v) => (int)Math.Round(v * _s);

                public ThongBaoPopup(string tieuDe, string noiDung, float dpiScale)
                {
                    _s = Math.Max(1f, dpiScale);
                    _tieuDe = tieuDe;
                    _noiDung = noiDung;

                    AutoScaleMode = AutoScaleMode.None;
                    FormBorderStyle = FormBorderStyle.None;
                    StartPosition = FormStartPosition.Manual;
                    ShowInTaskbar = false;
                    TopMost = true;

                    _fTitle = new Font("Segoe UI Semibold", S(15), FontStyle.Regular, GraphicsUnit.Pixel);
                    _fMsg = new Font("Segoe UI", S(13), FontStyle.Regular, GraphicsUnit.Pixel);
                    _fBtn = new Font("Segoe UI Semibold", S(13), FontStyle.Regular, GraphicsUnit.Pixel);

                    _sf = new StringFormat(StringFormat.GenericTypographic);
                    _sf.FormatFlags &= ~StringFormatFlags.LineLimit;
                    _sfCenter = new StringFormat(_sf) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                    // ---------- Đo chữ để tính bố cục ----------
                    int cardW = S(400), pad = S(20), icon = S(40), gap = S(14);
                    int textX = pad + icon + gap;
                    float titleW = cardW - textX - S(48);   // chừa chỗ cho nút ✕
                    float msgW = cardW - textX - pad;

                    SizeF sT, sM, sOk, sCancel;
                    using (var tmp = new Bitmap(1, 1))
                    using (var gm = Graphics.FromImage(tmp))
                    {
                        gm.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                        sT = gm.MeasureString(tieuDe, _fTitle, new SizeF(titleW, 10000), _sf);
                        sM = gm.MeasureString(noiDung, _fMsg, new SizeF(msgW, 10000), _sf);
                        sOk = gm.MeasureString(ChuOk, _fBtn, 1000, _sf);
                        sCancel = gm.MeasureString(ChuHuy, _fBtn, 1000, _sf);
                    }

                    int hTitle = (int)Math.Ceiling(sT.Height) + S(2);
                    int hMsg = (int)Math.Ceiling(sM.Height) + S(4);
                    int yTitle = S(18);
                    int yMsg = yTitle + hTitle + S(4);
                    int yBtn = yMsg + hMsg + S(16);
                    int btnH = S(36);
                    int cardH = yBtn + btnH + S(20);

                    _m = S(28);
                    int winW = cardW + 2 * _m;
                    int winH = cardH + 2 * _m;

                    _rcCard = new Rectangle(_m, _m, cardW, cardH);
                    _rcIcon = new Rectangle(_m + pad, _m + pad, icon, icon);
                    _rcTitle = new RectangleF(_m + textX, _m + yTitle, titleW, hTitle);
                    _rcMsg = new RectangleF(_m + textX, _m + yMsg, msgW, hMsg);
                    _rcClose = new Rectangle(_m + cardW - S(12) - S(28), _m + S(12), S(28), S(28));

                    int wOk = Math.Max(S(96), (int)Math.Ceiling(sOk.Width) + S(36));
                    int wCancel = Math.Max(S(80), (int)Math.Ceiling(sCancel.Width) + S(36));
                    _rcOk = new Rectangle(_m + textX, _m + yBtn, wOk, btnH);
                    _rcCancel = new Rectangle(_rcOk.Right + S(10), _m + yBtn, wCancel, btnH);

                    Size = new Size(winW, winH);

                    // ---------- Bitmap vẽ + bóng đổ (vẽ bóng một lần) ----------
                    _canvas = new Bitmap(winW, winH, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    _g = Graphics.FromImage(_canvas);
                    _g.SmoothingMode = SmoothingMode.AntiAlias;
                    _g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                    _shadow = new Bitmap(winW, winH, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using (var gs = Graphics.FromImage(_shadow))
                    {
                        gs.SmoothingMode = SmoothingMode.AntiAlias;
                        int blur = S(14), dy = S(4);
                        using var sb = new SolidBrush(Color.FromArgb(3, 15, 23, 42));
                        for (int i = blur; i >= 1; i--)
                        {
                            var r = new Rectangle(_rcCard.X - i, _rcCard.Y - i + dy, _rcCard.Width + 2 * i, _rcCard.Height + 2 * i);
                            using var path = RoundedRect(r, S(14) + i);
                            gs.FillPath(sb, path);
                        }
                    }

                    _timer.Tick += OnTick;
                }

                // Đặt popup sát góc dưới bên phải vùng làm việc (le = khoảng cách tới mép, tính theo pixel thiết kế)
                public void DatViTriGocDuoiPhai(Rectangle workingArea, int le)
                {
                    int left = workingArea.Right - S(le) - Width + _m;
                    _baseTop = workingArea.Bottom - S(le) - Height + _m;
                    Location = new Point(left, _baseTop + S(20));   // bắt đầu thấp hơn để trượt lên
                }

                // ===== Cửa sổ không cướp focus, không hiện ở Alt+Tab =====
                protected override bool ShowWithoutActivation => true;

                protected override CreateParams CreateParams
                {
                    get
                    {
                        var cp = base.CreateParams;
                        cp.ExStyle |= WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                        return cp;
                    }
                }

                protected override void OnPaintBackground(PaintEventArgs e) { }   // vẽ hoàn toàn bằng layered window

                protected override void OnShown(EventArgs e)
                {
                    base.OnShown(e);
                    _sw.Start();
                    _timer.Start();
                }

                // ===== Vòng lặp: hiệu ứng vào + đếm ngược =====
                private void OnTick(object? sender, EventArgs e)
                {
                    if (!IsHandleCreated || IsDisposed) return;

                    long now = _sw.ElapsedMilliseconds;
                    int dt = (int)(now - _lastMs);
                    _lastMs = now;

                    bool animating = _anim < AnimMs;
                    if (animating) { _anim = Math.Min(AnimMs, _anim + dt); _dirty = true; }

                    if (!_trongCard) _elapsed += dt;
                    if (_elapsed >= TongMs) { Close(); return; }

                    int bar = (int)(_rcCard.Width * (1.0 - _elapsed / TongMs));
                    if (bar != _lastBar) { _lastBar = bar; _dirty = true; }

                    if (!_dirty) return;
                    _dirty = false;

                    double t = Math.Min(1.0, _anim / AnimMs);
                    double ease = 1 - Math.Pow(1 - t, 3);             // ease-out cubic
                    Top = _baseTop + (int)((1 - ease) * S(20));

                    Render();
                    Push((byte)Math.Round(255 * ease));
                }

                // ===== Vẽ giao diện =====
                private void Render()
                {
                    var g = _g;
                    g.Clear(Color.Transparent);
                    g.DrawImageUnscaled(_shadow, 0, 0);

                    // --- Thẻ trắng + thanh đếm ngược ở đáy (cắt theo bo góc) ---
                    using (var card = RoundedRect(_rcCard, S(14)))
                    {
                        g.FillPath(Brushes.White, card);

                        int barW = (int)(_rcCard.Width * (1.0 - Math.Min(1.0, _elapsed / TongMs)));
                        if (barW > 0)
                        {
                            var st = g.Save();
                            g.SetClip(card, CombineMode.Replace);
                            using var bb = new SolidBrush(Color.FromArgb(99, 102, 241));
                            g.FillRectangle(bb, _rcCard.X, _rcCard.Bottom - S(3), barW, S(3));
                            g.Restore(st);
                        }
                    }
                    using (var border = RoundedRect(new Rectangle(_rcCard.X, _rcCard.Y, _rcCard.Width - 1, _rcCard.Height - 1), S(14)))
                    using (var penB = new Pen(Color.FromArgb(226, 232, 240)))
                        g.DrawPath(penB, border);

                    // --- Ô icon bo vuông + biểu tượng "tải xuống" ---
                    using (var path = RoundedRect(_rcIcon, S(12)))
                    using (var b = new SolidBrush(Color.FromArgb(238, 242, 255)))
                        g.FillPath(b, path);

                    using (var pen = new Pen(Color.FromArgb(79, 70, 229), 2f * _s)
                    { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                    {
                        float ix = _rcIcon.X, iy = _rcIcon.Y, u = _s;
                        PointF P(float x, float y) => new PointF(ix + x * u, iy + y * u);
                        g.DrawLine(pen, P(20, 11), P(20, 24));
                        g.DrawLines(pen, new[] { P(14.5f, 18.5f), P(20, 24), P(25.5f, 18.5f) });
                        g.DrawLines(pen, new[] { P(12, 26), P(12, 29), P(28, 29), P(28, 26) });
                    }

                    // --- Tiêu đề + nội dung ---
                    using (var bTitle = new SolidBrush(Color.FromArgb(15, 23, 42)))
                        g.DrawString(_tieuDe, _fTitle, bTitle, _rcTitle, _sf);
                    using (var bMsg = new SolidBrush(Color.FromArgb(100, 116, 139)))
                        g.DrawString(_noiDung, _fMsg, bMsg, _rcMsg, _sf);

                    // --- Nút đóng ✕ ---
                    bool hClose = _hit == Hit.Close;
                    if (hClose)
                        using (var bh = new SolidBrush(Color.FromArgb(241, 245, 249)))
                            g.FillEllipse(bh, _rcClose);
                    using (var penX = new Pen(hClose ? Color.FromArgb(51, 65, 85) : Color.FromArgb(148, 163, 184), 1.6f * _s)
                    { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        float cx = _rcClose.X + _rcClose.Width / 2f, cy = _rcClose.Y + _rcClose.Height / 2f, d = 4.5f * _s;
                        g.DrawLine(penX, cx - d, cy - d, cx + d, cy + d);
                        g.DrawLine(penX, cx - d, cy + d, cx + d, cy - d);
                    }

                    // --- Nút chính "Nạp ngay" ---
                    using (var path = RoundedRect(_rcOk, S(10)))
                    using (var b = new SolidBrush(_hit == Hit.Ok ? Color.FromArgb(67, 56, 202) : Color.FromArgb(79, 70, 229)))
                        g.FillPath(b, path);
                    g.DrawString(ChuOk, _fBtn, Brushes.White, _rcOk, _sfCenter);

                    // --- Nút phụ "Để sau" ---
                    using (var path = RoundedRect(new Rectangle(_rcCancel.X, _rcCancel.Y, _rcCancel.Width - 1, _rcCancel.Height - 1), S(10)))
                    using (var b = new SolidBrush(_hit == Hit.Cancel ? Color.FromArgb(248, 250, 252) : Color.White))
                    using (var penC = new Pen(Color.FromArgb(226, 232, 240)))
                    {
                        g.FillPath(b, path);
                        g.DrawPath(penC, path);
                    }
                    using (var bt = new SolidBrush(Color.FromArgb(51, 65, 85)))
                        g.DrawString(ChuHuy, _fBtn, bt, _rcCancel, _sfCenter);
                }

                // ===== Đẩy bitmap lên cửa sổ layered =====
                private void Push(byte alpha)
                {
                    IntPtr screenDc = GetDC(IntPtr.Zero);
                    IntPtr memDc = CreateCompatibleDC(screenDc);
                    IntPtr hBmp = IntPtr.Zero, old = IntPtr.Zero;
                    try
                    {
                        hBmp = _canvas.GetHbitmap(Color.FromArgb(0));
                        old = SelectObject(memDc, hBmp);

                        var size = new SIZE { cx = _canvas.Width, cy = _canvas.Height };
                        var src = new POINT { x = 0, y = 0 };
                        var dst = new POINT { x = Left, y = Top };
                        var blend = new BLENDFUNCTION
                        {
                            BlendOp = AC_SRC_OVER,
                            BlendFlags = 0,
                            SourceConstantAlpha = alpha,
                            AlphaFormat = AC_SRC_ALPHA
                        };
                        UpdateLayeredWindow(Handle, screenDc, ref dst, ref size, memDc, ref src, 0, ref blend, ULW_ALPHA);
                    }
                    finally
                    {
                        if (old != IntPtr.Zero) SelectObject(memDc, old);
                        if (hBmp != IntPtr.Zero) DeleteObject(hBmp);
                        DeleteDC(memDc);
                        ReleaseDC(IntPtr.Zero, screenDc);
                    }
                }

                // ===== Chuột =====
                private Hit HitTest(Point p) =>
                    _rcClose.Contains(p) ? Hit.Close :
                    _rcOk.Contains(p) ? Hit.Ok :
                    _rcCancel.Contains(p) ? Hit.Cancel : Hit.None;

                protected override void OnMouseMove(MouseEventArgs e)
                {
                    base.OnMouseMove(e);
                    var h = HitTest(e.Location);
                    bool inside = _rcCard.Contains(e.Location);
                    if (h != _hit || inside != _trongCard) { _hit = h; _trongCard = inside; _dirty = true; }
                    Cursor = h == Hit.None ? Cursors.Default : Cursors.Hand;
                }

                protected override void OnMouseLeave(EventArgs e)
                {
                    base.OnMouseLeave(e);
                    _hit = Hit.None;
                    _trongCard = false;
                    _dirty = true;
                }

                protected override void OnMouseClick(MouseEventArgs e)
                {
                    base.OnMouseClick(e);
                    if (e.Button != MouseButtons.Left) return;
                    switch (HitTest(e.Location))
                    {
                        case Hit.Ok: NapNgay?.Invoke(this, EventArgs.Empty); break;
                        case Hit.Cancel:
                        case Hit.Close: Close(); break;
                    }
                }

                protected override void OnFormClosed(FormClosedEventArgs e)
                {
                    _timer.Stop();
                    _timer.Dispose();
                    _g.Dispose();
                    _canvas.Dispose();
                    _shadow.Dispose();
                    _fTitle.Dispose();
                    _fMsg.Dispose();
                    _fBtn.Dispose();
                    _sf.Dispose();
                    _sfCenter.Dispose();
                    base.OnFormClosed(e);
                }
            }




            /// <summary>
            /// Kiểm tra thư mục Database-PhanMemThiDua2026 trên Desktop
            /// </summary>
            /// 

            /// <summary>
            /// Lắng nghe và phát hiện thư mục Database-PhanMemThiDua2026 ngoài Desktop (đã kiểm tra nội dung thông minh)
            /// </summary>
            /// <summary>
            /// Kiểm tra thư mục Database-PhanMemThiDua2026 trên Desktop và xác thực nội dung bên trong
            /// </summary>
            public static void KiemTraThuMucChuyenGiaoDesktop(Form2_FormCha formCha)
            {
                try
                {
                    string tenThuMuc = "Database-PhanMemThiDua2026";

                    // 1. Lấy tất cả các đường dẫn Desktop có thể có (Bao gồm cả OneDrive Desktop)
                    string desktopStandard = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string desktopUserProfile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop");
                    string desktopOneDrive = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive", "Desktop");

                    string targetFolder = "";

                    string path1 = Path.Combine(desktopStandard, tenThuMuc);
                    string path2 = Path.Combine(desktopUserProfile, tenThuMuc);
                    string path3 = Path.Combine(desktopOneDrive, tenThuMuc);

                    // Kiểm tra sự tồn tại VÀ tính hợp lệ của nội dung bên trong
                    if (Directory.Exists(path1) && KiemTraThuMucHopLe(path1))
                        targetFolder = path1;
                    else if (Directory.Exists(path2) && KiemTraThuMucHopLe(path2))
                        targetFolder = path2;
                    else if (Directory.Exists(path3) && KiemTraThuMucHopLe(path3))
                        targetFolder = path3;

                    // 2. Nếu tìm thấy thư mục hợp lệ và có chứa ít nhất 1 hoặc đầy đủ csdl1, csdl2, csdl3, csdl4
                    if (!string.IsNullOrEmpty(targetFolder))
                    {
                        // Đảm bảo chạy trên Thread giao diện (UI Thread)
                        formCha.BeginInvoke(new Action(() =>
                        {
                            HienThiThongBaoGocDuoiPhai(formCha, targetFolder);
                        }));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Lỗi KiemTraThuMucDesktop]: {ex.Message}");
                }
            }

            /// <summary>
            /// Hàm phụ trợ: Kiểm tra thư mục mẹ có rỗng không và có chứa ít nhất 1 trong các tệp/thư mục csdl1, csdl2, csdl3, csdl4 hay không
            /// </summary>
            private static bool KiemTraThuMucHopLe(string folderPath)
            {
                try
                {
                    if (!Directory.Exists(folderPath)) return false;

                    // Sử dụng AllDirectories thay vì AllFiles
                    string[] subDirs = Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories);
                    string[] allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

                    // Nếu thư mục mẹ hoàn toàn rỗng -> Không thông báo
                    if (subDirs.Length == 0 && allFiles.Length == 0) return false;

                    // Danh sách các định danh CSDL chuẩn của hệ thống
                    string[] targetDbNames = { "csdl1", "csdl2", "csdl3", "csdl4" };
                    bool coChuaDuLieuHopLe = false;

                    // Kiểm tra xem có thư mục con hoặc tệp tin nào mang tên csdl1, csdl2, csdl3, csdl4 không
                    foreach (string name in targetDbNames)
                    {
                        // Kiểm tra xem có thư mục con tên csdl1, csdl2... không
                        bool matchDir = Array.Exists(subDirs, d => Path.GetFileName(d).Equals(name, StringComparison.OrdinalIgnoreCase));

                        // Kiểm tra xem có tệp tin tên csdl1.db / csdl1.json / ... không
                        bool matchFile = Array.Exists(allFiles, f => Path.GetFileNameWithoutExtension(f).Equals(name, StringComparison.OrdinalIgnoreCase));

                        if (matchDir || matchFile)
                        {
                            coChuaDuLieuHopLe = true;
                            break; // Chỉ cần phát hiện ít nhất 1 tệp/thư mục cơ sở dữ liệu hợp lệ là đủ điều kiện báo hiệu
                        }
                    }

                    return coChuaDuLieuHopLe;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Lỗi kiểm tra nội dung thư mục chuyển giao]: {ex.Message}");
                    return false;
                }
            }
            /// <summary>
            /// Hàm phụ trợ: Kiểm tra thư mục mẹ có rỗng không và có chứa ít nhất 1 trong các tệp/thư mục csdl1, csdl2, csdl3, csdl4 hay không
            /// </summary>
        
            // Hàm tạo đường bo góc (đặt trong cùng class)
            private static GraphicsPath TaoDuongBoGoc(Rectangle rect, int banKinh)
            {
                int d = banKinh * 2;
                var path = new GraphicsPath();
                path.AddArc(rect.X, rect.Y, d, d, 180, 90);                       // góc trên trái
                path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);               // góc trên phải
                path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);        // góc dưới phải
                path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);               // góc dưới trái
                path.CloseFigure();
                return path;
            }

            private static void MoFormChuyenGiao(Form2_FormCha formCha)
            {
                try
                {
                    // Gọi form chuyển giao dữ liệu đảm bảo nằm trong form cha (hoặc mở dạng con tùy architecture của bạn)
                    // Ví dụ nếu bạn dùng cơ chế OpenChildForm cho Form31:
                    formCha.OpenChildForm<Form31_ChuyenGiaoDuLieu>("Trang Chuyển giao dữ liệu");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(formCha, "Không thể mở trang chuyển giao dữ liệu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
    
