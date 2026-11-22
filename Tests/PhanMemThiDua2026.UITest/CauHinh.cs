using System.Text.Encodings.Web;
using System.Text.Json;
namespace PhanMemThiDua2026.UITest
{
    /// <summary>Cấu hình đọc từ uitest.json cạnh exe. Thiếu tệp => tạo tệp mẫu rồi dừng để người dùng điền tài khoản.</summary>
    internal sealed class CauHinh
    {
        public string TenDangNhap { get; set; } = "";
        public string MatKhau { get; set; } = "";
        /// <summary>"Co" = hộp thoại xác nhận bấm Có/OK (đi sâu nhất, ĐỤNG DỮ LIỆU THẬT); "Khong" = bấm Không/Hủy.</summary>
        public string TraLoiXacNhan { get; set; } = "Co";
        /// <summary>Số tầng form con được bấm thử (form mở ra từ nút của form khác).</summary>
        public int DoSauToiDa { get; set; } = 3;
        /// <summary>
        /// Test sâu: sau lượt bấm "ô trống", mỗi form được bấm lại các nút với dữ liệu mẫu (điền "UITest", chọn dòng lưới)
        /// => Thêm/Sửa/Lưu chạy thật tới cuối. Nút Xóa ở lượt này chỉ bấm khi chọn được dòng "UITest" do công cụ tạo.
        /// </summary>
        public bool TestSau { get; set; } = true;
        public int SoBuocToiDaMoiForm { get; set; } = 400;
        public int ChoSauMoiLanBamMs { get; set; } = 400;
        /// <summary>Chờ tối đa (giây) phần mềm xử lý xong một lần bấm (con trỏ chờ / form Đang xử lý).</summary>
        public int ChoXuLyToiDaGiay { get; set; } = 90;
        /// <summary>Một lần bấm kéo dài quá số giây này => ghi TREO vào báo cáo (vẫn chờ tiếp).</summary>
        public int NguongTreoGiay { get; set; } = 45;
        /// <summary>Sau Form2 (giao diện chính) thì mở trực tiếp từng form còn lại (form có hàm dựng không tham số).</summary>
        public bool MoTrucTiepCacFormConLai { get; set; } = true;
        /// <summary>Một lần chạy quá số phút này thì tiến trình điều phối buộc dừng lần đó và chuyển sang lần sau.</summary>
        public int ThoiGianToiDaMoiLanPhut { get; set; } = 180;
        /// <summary>Thư mục chứa tệp mẫu để NHẬP (ngoài các tệp vừa XUẤT trong lần chạy). Để trống = không dùng.</summary>
        public string ThuMucTepMau { get; set; } = "";
        /// <summary>Nút/mục menu có chữ (hoặc tên control) chứa một trong các chuỗi này thì KHÔNG bấm.</summary>
        public List<string> BoQuaNut { get; set; } = new()
        {
            "Gỡ cài đặt", "Đăng xuất", "Thoát", "Khởi động lại", "Logout", "Exit",
            "GoCaiDat", "DangXuat", "btn_Thoat", "KhoiDongLai",
            // In ra máy in: hộp thoại in Windows 11 chạy ở tiến trình khác => không tự đóng được
            "[Menu] Print", "In ấn", "Máy in", "btn_In", "Print",
            // Sao lưu / Khôi phục: gọi ServiceBackup/ServiceRestore rồi TẮT phần mềm (Khôi phục còn ghi đè CSDL)
            "_SaoLuu", "_Khoiphuc", "_KhoiPhuc",
        };
        /// <summary>Tên lớp form không mở/không bấm.</summary>
        public List<string> BoQuaForm { get; set; } = new()
        {
            "Form1", "Form9_GoCaiDat", "Form8_CauHinhCSDL", "FormWelcome", "Form_Loading",
        };
        /// <summary>Thư mục bản phần mềm được test lần trước (gợi ý sẵn ở hộp chọn). Trống = thư mục của công cụ.</summary>
        public string ThuMucPhanMem { get; set; } = "";
        /// <summary>
        /// Sau MỖI lần chạy (phần mềm đã tắt): trả dữ liệu (Database, Database Backup, window-x64) về đúng như trước khi test,
        /// chỉ giữ lại nhật ký test uitest-*.log => mọi bản ghi "UITest", tệp xuất thử, thay đổi cài đặt... đều bị xóa.
        /// </summary>
        public bool KhoiPhucDuLieuSauMoiLan { get; set; } = true;
        /// <summary>Một lần chạy không có hoạt động bấm thử nào quá số phút này => ghi tổng kết rồi dừng lần đó (không treo mãi).</summary>
        public int DungLanNeuKhongHoatDongPhut { get; set; } = 10;
        /// <summary>Mặc định cạnh exe của công cụ; tiến trình con chạy từ thư mục phần mềm nên nhận đường dẫn qua tham số.</summary>
        public static string DuongDan { get; set; } = Path.Combine(AppContext.BaseDirectory, "uitest.json");
        public void Luu()
        {
            try { File.WriteAllText(DuongDan, JsonSerializer.Serialize(this, Json)); } catch { }
        }
        private static readonly JsonSerializerOptions Json = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };
        public static CauHinh? Doc(out string loi)
        {
            loi = "";
            try
            {
                if (!File.Exists(DuongDan))
                {
                    File.WriteAllText(DuongDan, JsonSerializer.Serialize(new CauHinh(), Json));
                    loi = "Chưa có tệp cấu hình. Đã tạo mẫu:\n" + DuongDan + "\n\nĐiền TenDangNhap / MatKhau rồi chạy lại.";
                    return null;
                }
                var ch = JsonSerializer.Deserialize<CauHinh>(File.ReadAllText(DuongDan), Json) ?? new CauHinh();
                if (string.IsNullOrWhiteSpace(ch.TenDangNhap) || string.IsNullOrEmpty(ch.MatKhau))
                {
                    loi = "Thiếu TenDangNhap / MatKhau trong:\n" + DuongDan;
                    return null;
                }
                return ch;
            }
            catch (Exception ex)
            {
                loi = "Không đọc được " + DuongDan + ":\n" + ex.Message;
                return null;
            }
        }
        public bool TraLoiCo => !TraLoiXacNhan.StartsWith("K", StringComparison.OrdinalIgnoreCase);
    }
}
