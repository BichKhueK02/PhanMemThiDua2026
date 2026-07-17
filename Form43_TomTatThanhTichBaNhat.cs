using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace PhanMemThiDua2026
{
    public partial class Form43_TomTatThanhTichBaNhat : Form
    {
        // Sử dụng chung đường dẫn CSDL từ module dẫn đường của hệ thống
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private const int GioiHanKhuyenNghi = 830;
        // 🌟 THÊM THUỘC TÍNH ĐỘNG: Tự động chọn bảng Tóm tắt theo phiên bản hệ thống
        private string TenBangHienTai
        {
            get
            {
                string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
                return phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase)
                    ? "TomTatThanhTichBaNhat_TanBinh"
                    : "TomTatThanhTichBaNhat_CBCS";
            }
        }
        public Form43_TomTatThanhTichBaNhat()
        {
            InitializeComponent();

            // Cấu hình tối ưu giao diện và RAM hệ thống
            this.ShowInTaskbar = false;          // Không tạo icon riêng biệt dưới thanh Taskbar nền
            this.MaximizeBox = false;            // Vô hiệu hóa nút phóng to toàn màn hình
            this.MinimizeBox = false;            // Vô hiệu hóa nút thu nhỏ
                                                 //this.FormBorderStyle = FormBorderStyle.FixedSingle; // Khóa viền cố định, không cho kéo giãn Form
                                                 // ⭐ THÊM DÒNG NÀY: Kết nối sự kiện để mỗi khi gõ/xóa là số nhảy ngay lập tức
                                                 // =========================================================================
            richTextBox1_TomTatThanhTichBaNhat.TextChanged += richTextBox1_TomTatThanhTichBaNhat_TextChanged;
            // Đăng ký sự kiện khi nội dung chữ thay đổi
            richTextBox1_TomTatThanhTichBaNhat.TextChanged += (s, e) => KiemTraHienThiNutCoChu();
        }
        private async void Form43_TomTatThanhTichBaNhat_Load(object sender, EventArgs e)
        {
            // Tải dữ liệu tóm tắt thành tích lên RichTextBox khi Form khởi chạy
            await TaiDuLieuTomTatAsync();
            this.AcceptButton = kryptonButton_LuuDataDeNghi;
            // Đảm bảo đếm ký tự ngay khi Form vừa load xong (trường hợp DB rỗng)
            CapNhatSoLuongKyTu();
            InitToolTips();
        }
        // ⭐ HÀM MỚI: Đếm và cập nhật số lượng ký tự
        private void CapNhatSoLuongKyTu()
        {
            if (richTextBox1_TomTatThanhTichBaNhat == null ||
                toolStripStatusLabel1_SoLuongKyTu == null)
            {
                return;
            }

            int soKyTu = richTextBox1_TomTatThanhTichBaNhat.TextLength;

            string noiDung;
            Color mauChu;

            if (soKyTu == 0)
            {
                noiDung = $"Khuyến nghị đoạn văn giới hạn {GioiHanKhuyenNghi} ký tự (Tối đa {Module_BaNhat.GioiHanToiDa} ký tự)";
                mauChu = SystemColors.ControlText;
            }
            else if (soKyTu > Module_BaNhat.GioiHanToiDa)
            {
                noiDung = $"Quá giới hạn ký tự cho phép {soKyTu}/{Module_BaNhat.GioiHanToiDa} ký tự (Không thể lưu!)";
                mauChu = Color.DarkRed;
            }
            else if (soKyTu > GioiHanKhuyenNghi)
            {
                noiDung = $"Đã vượt quá khuyến nghị {soKyTu - GioiHanKhuyenNghi} ký tự";
                mauChu = Color.Red;
            }
            else
            {
                noiDung = $"Số lượng ký tự {soKyTu}/{GioiHanKhuyenNghi} ký tự khuyến nghị";
                mauChu = SystemColors.ControlText;
            }

            // Chỉ cập nhật khi thực sự thay đổi
            if (!string.Equals(toolStripStatusLabel1_SoLuongKyTu.Text, noiDung, StringComparison.Ordinal))
            {
                toolStripStatusLabel1_SoLuongKyTu.Text = noiDung;
            }

            if (toolStripStatusLabel1_SoLuongKyTu.ForeColor != mauChu)
            {
                toolStripStatusLabel1_SoLuongKyTu.ForeColor = mauChu;
            }
        }
        // ⭐ SỰ KIỆN MỚI: Gọi hàm đếm tự động mỗi khi người dùng gõ phím
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Gợi ý thao tác";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;

            // UX: phản hồi nhanh – không gây khó chịu khi rê chuột qua
            toolTip1.InitialDelay = 300;
            toolTip1.AutoPopDelay = 2500;
            toolTip1.ReshowDelay = 100;
            toolTip1.ShowAlways = true;

            // Bản đồ ánh xạ các nút điều khiển và nội dung hướng dẫn tương ứng
            var tips = new Dictionary<Control, string>
    {
        { kryptonButton2_GiamCoChuRichText, "Giảm cỡ chữ" },
        { kryptonButton2_TangCoChuRichText, "Tăng cỡ chữ" },
        { kryptonButton_LuuDataDeNghi, "Lưu thông tin thi đua tập thể phong trào Ba nhất vào cơ sở dữ liệu" }

    };

            foreach (var tip in tips)
            {
                // Kiểm tra an toàn để tránh lỗi NullReference nếu nút chưa được khởi tạo hoặc bị hủy
                if (tip.Key != null && !tip.Key.IsDisposed)
                {
                    toolTip1.SetToolTip(tip.Key, tip.Value);
                }
            }
        }
        private void richTextBox1_TomTatThanhTichBaNhat_TextChanged(object sender, EventArgs e)
        {
            CapNhatSoLuongKyTu();
        }
        private async Task TaiDuLieuTomTatAsync()
        {
            if (!File.Exists(_csdl2Path)) return;

            try
            {
                using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                // 🌟 SỬA ĐỊNH TUYẾN: Gọi tên bảng động
                cmd.CommandText = $"SELECT NoiDung FROM [{TenBangHienTai}] WHERE ID = 1";

                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    richTextBox1_TomTatThanhTichBaNhat.Text = BaoMatAES.GiaiMa(result.ToString()).Trim();
                    CapNhatSoLuongKyTu();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi tải tóm tắt thành tích: " + ex.Message);
            }
        }
        private async void kryptonButton_LuuDataDeNghi_Click(object sender, EventArgs e)
        {
            int soKyTu = richTextBox1_TomTatThanhTichBaNhat.TextLength;

            if (soKyTu > Module_BaNhat.GioiHanToiDa)
            {
                MessageBox.Show(
                    $"Nội dung hiện có {soKyTu} ký tự, vượt quá giới hạn cho phép ({Module_BaNhat.GioiHanToiDa} ký tự).\nVui lòng rút gọn nội dung trước khi lưu.",
                    "Không thể lưu dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                richTextBox1_TomTatThanhTichBaNhat.Focus();
                return;
            }
            string tenNutGoc = kryptonButton_LuuDataDeNghi.Text;
            kryptonButton_LuuDataDeNghi.Text = "Đang lưu...";
            kryptonButton_LuuDataDeNghi.Enabled = false;
            kryptonButton_LuuDataDeNghi.Refresh();

            try
            {
                string noiDungTho = richTextBox1_TomTatThanhTichBaNhat.Text.Trim();
                string noiDungMaHoa = BaoMatAES.MaHoa(noiDungTho);

                using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();

                // 🌟 SỬA ĐỊNH TUYẾN: Ghi dữ liệu vào đúng bảng hệ thống đang chọn
                cmd.CommandText = $@"
            INSERT OR REPLACE INTO [{TenBangHienTai}] (ID, NoiDung) 
            VALUES (1, @NoiDung);";

                cmd.Parameters.AddWithValue("@NoiDung", noiDungMaHoa);
                await cmd.ExecuteNonQueryAsync();

                Module_NhatKy.GhiNhatKy(
            taiKhoan: string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM) ? "Không xác định" : Module_TaiKhoan.TenTaiKhoan_RAM,
            hanhDong: $"Cập nhật thành tích tập thể phong trào Ba Nhất ({TenBangHienTai})",
            ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
        );
                await Task.Delay(200); // Tạo độ trễ nhỏ để trải nghiệm UX mượt mà
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                MessageBox.Show("Lỗi trong quá trình lưu dữ liệu:\n\n" + ex.Message,
                                "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                kryptonButton_LuuDataDeNghi.Text = tenNutGoc;
                kryptonButton_LuuDataDeNghi.Enabled = true;
                kryptonButton_LuuDataDeNghi.Refresh();
            }
        }
        private const float RichText_MinFontSize = 8f;
        private const float RichText_MaxFontSize = 30f;
        private const float RichText_FontStep = 1f;
        // --- HÀM THAY ĐỔI CỠ CHỮ ---
        private void ThayDoiCoChuRichText(float delta)
        {
            if (richTextBox1_TomTatThanhTichBaNhat == null)
                return;

            Font fontHienTai = richTextBox1_TomTatThanhTichBaNhat.Font;

            if (fontHienTai == null)
                return;

            float kichThuocMoi = fontHienTai.Size + delta;

            if (kichThuocMoi < RichText_MinFontSize)
                kichThuocMoi = RichText_MinFontSize;

            if (kichThuocMoi > RichText_MaxFontSize)
                kichThuocMoi = RichText_MaxFontSize;

            if (Math.Abs(kichThuocMoi - fontHienTai.Size) < 0.01f)
                return;

            richTextBox1_TomTatThanhTichBaNhat.SuspendLayout();

            try
            {
                richTextBox1_TomTatThanhTichBaNhat.Font =
                    new Font(
                        fontHienTai.FontFamily,
                        kichThuocMoi,
                        fontHienTai.Style,
                        GraphicsUnit.Point);

                richTextBox1_TomTatThanhTichBaNhat.Focus();
            }
            finally
            {
                richTextBox1_TomTatThanhTichBaNhat.ResumeLayout();
            }
        }
        // --- SỰ KIỆN CLICK ---
        private void kryptonButton2_TangCoChuRichText_Click(object sender, EventArgs e)
        {
            ThayDoiCoChuRichText(RichText_FontStep);
        }
        private void kryptonButton2_GiamCoChuRichText_Click(object sender, EventArgs e)
        {
            ThayDoiCoChuRichText(-RichText_FontStep);
        }
        private void KiemTraHienThiNutCoChu()
        {
            // Kiểm tra xem RichTextBox có nội dung không (bỏ qua khoảng trắng/xuống dòng thừa)
            bool coNoiDung = !string.IsNullOrWhiteSpace(richTextBox1_TomTatThanhTichBaNhat.Text);

            // Ẩn/Hiện 2 nút dựa trên kết quả kiểm tra
            kryptonButton2_TangCoChuRichText.Visible = coNoiDung;
            kryptonButton2_GiamCoChuRichText.Visible = coNoiDung;
        }
    }
}