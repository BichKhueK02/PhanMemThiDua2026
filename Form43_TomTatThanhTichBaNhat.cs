using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using System.Runtime.InteropServices;

namespace PhanMemThiDua2026
{
    public partial class Form43_TomTatThanhTichBaNhat : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private const int GioiHanKhuyenNghi = 830;
        private const int WM_USER = 0x0400;
        private const int EM_SETPARAFORMAT = WM_USER + 71;
        private const uint PFM_ALIGNMENT = 0x00000008;
        private const short PFA_JUSTIFY = 4;
        [StructLayout(LayoutKind.Sequential)]
        private struct PARAFORMAT
        {
            public int cbSize;
            public uint dwMask;
            public short wNumbering;
            public short wReserved;
            public int dxStartIndent;
            public int dxRightIndent;
            public int dxOffset;
            public short wAlignment;
            public short cTabCount;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public int[] rgxTabs;
        }
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(
            IntPtr hWnd,
            int msg,
            IntPtr wParam,
            ref PARAFORMAT lParam);
        private bool _dangCanDeu = false;
        private const float RichText_MinFontSize = 8f;
        private const float RichText_MaxFontSize = 30f;
        private const float RichText_FontStep = 1f;
        // --- HÀM THAY ĐỔI CỠ CHỮ ---
        private void CanDeuRichTextBox()
        {
            if (_dangCanDeu)
                return;

            if (richTextBox1_TomTatThanhTichBaNhat == null ||
                richTextBox1_TomTatThanhTichBaNhat.IsDisposed ||
                !richTextBox1_TomTatThanhTichBaNhat.IsHandleCreated)
            {
                return;
            }

            if (richTextBox1_TomTatThanhTichBaNhat.TextLength == 0)
                return;

            _dangCanDeu = true;

            int selectionStart =
                richTextBox1_TomTatThanhTichBaNhat.SelectionStart;

            int selectionLength =
                richTextBox1_TomTatThanhTichBaNhat.SelectionLength;

            try
            {
                richTextBox1_TomTatThanhTichBaNhat.SelectAll();

                var paraFormat = new PARAFORMAT
                {
                    cbSize = Marshal.SizeOf<PARAFORMAT>(),
                    dwMask = PFM_ALIGNMENT,
                    wAlignment = PFA_JUSTIFY,
                    rgxTabs = new int[32]
                };

                SendMessage(
                    richTextBox1_TomTatThanhTichBaNhat.Handle,
                    EM_SETPARAFORMAT,
                    IntPtr.Zero,
                    ref paraFormat);
            }
            finally
            {
                // Khôi phục vị trí con trỏ / vùng chọn
                int textLength =
                    richTextBox1_TomTatThanhTichBaNhat.TextLength;

                selectionStart =
                    Math.Max(0, Math.Min(selectionStart, textLength));

                selectionLength =
                    Math.Max(
                        0,
                        Math.Min(
                            selectionLength,
                            textLength - selectionStart));

                richTextBox1_TomTatThanhTichBaNhat.Select(
                    selectionStart,
                    selectionLength);

                _dangCanDeu = false;
            }
        }
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
            // Tải dữ liệu từ CSDL trước
            await TaiDuLieuTomTatAsync();
            AcceptButton = kryptonButton_LuuDataDeNghi;
            // Cập nhật giao diện
            CapNhatSoLuongKyTu();
            InitToolTips();
            // Căn đều sau khi dữ liệu từ CSDL đã được nạp hoàn tất
            CanDeuRichTextBox();
            // Đưa con trỏ về cuối văn bản sau cùng
            DuaConTroVeCuoiVanBan();
        }
        private void DuaConTroVeCuoiVanBan()
        {
            if (richTextBox1_TomTatThanhTichBaNhat == null ||
                richTextBox1_TomTatThanhTichBaNhat.IsDisposed)
            {
                return;
            }

            richTextBox1_TomTatThanhTichBaNhat.Focus();

            int viTriCuoi =
                richTextBox1_TomTatThanhTichBaNhat.TextLength;

            richTextBox1_TomTatThanhTichBaNhat.SelectionStart = viTriCuoi;
            richTextBox1_TomTatThanhTichBaNhat.SelectionLength = 0;

            // Cuộn đến vị trí con trỏ
            richTextBox1_TomTatThanhTichBaNhat.ScrollToCaret();
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
            toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            // UX: phản hồi nhanh – không gây khó chịu khi rê chuột qua
            toolTip1.InitialDelay = 300;
            toolTip1.AutoPopDelay = 2500;
            toolTip1.ReshowDelay = 100;
            toolTip1.ShowAlways = true;
            // Bản đồ ánh xạ các nút điều khiển và nội dung hướng dẫn tương ứng
            var tips = new Dictionary<Control, string>{
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
            // Tự động căn đều toàn bộ nội dung
            CanDeuRichTextBox();
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
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi tải tóm tắt thành tích: " + ex.Message);
            }
        }
        private async void kryptonButton_LuuDataDeNghi_Click(object sender, EventArgs e)
        {
            int soKyTu =
                richTextBox1_TomTatThanhTichBaNhat.TextLength;

            // ============================================================
            // 1. KIỂM TRA GIỚI HẠN KÝ TỰ
            // ============================================================

            if (soKyTu > Module_BaNhat.GioiHanToiDa)
            {
                MessageBox.Show(
                    $"Nội dung hiện có {soKyTu} ký tự, vượt quá giới hạn cho phép " +
                    $"({Module_BaNhat.GioiHanToiDa} ký tự).\n\n" +
                    "Vui lòng rút gọn nội dung trước khi lưu.",
                    "Không thể lưu dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                richTextBox1_TomTatThanhTichBaNhat.Focus();
                return;
            }

            string tenNutGoc =
                kryptonButton_LuuDataDeNghi.Text;

            // ============================================================
            // 2. KHÓA NÚT NGAY LẬP TỨC
            // ============================================================

            kryptonButton_LuuDataDeNghi.Text = "Đang lưu...";
            kryptonButton_LuuDataDeNghi.Enabled = false;
            kryptonButton_LuuDataDeNghi.Refresh();

            try
            {
                // ========================================================
                // 3. LẤY NỘI DUNG
                // ========================================================

                string noiDungTho =
                    richTextBox1_TomTatThanhTichBaNhat.Text.Trim();

                string noiDungMaHoa =
                    BaoMatAES.MaHoa(noiDungTho);

                // ========================================================
                // 4. MỞ CSDL
                // ========================================================

                using var conn =
                    new SqliteConnection(
                        $"Data Source={_csdl2Path}");

                await conn.OpenAsync();

                // ========================================================
                // 5. GHI DỮ LIỆU
                // ========================================================

                using var cmd =
                    conn.CreateCommand();

                cmd.CommandText = $@"
INSERT OR REPLACE INTO [{TenBangHienTai}]
(
    ID,
    NoiDung
)
VALUES
(
    1,
    @NoiDung
);";

                cmd.Parameters.AddWithValue(
                    "@NoiDung",
                    noiDungMaHoa);

                int soDongAnhHuong =
                    await cmd.ExecuteNonQueryAsync();

                if (soDongAnhHuong != 1)
                {
                    throw new InvalidOperationException(
                        "Không thể xác nhận dữ liệu đã được lưu đúng.");
                }

                // ========================================================
                // 6. GHI NHẬT KÝ
                // ========================================================

                Module_NhatKy.GhiNhatKy(
                    taiKhoan:
                        string.IsNullOrWhiteSpace(
                            Module_TaiKhoan.TenTaiKhoan_RAM)
                            ? "Không xác định"
                            : Module_TaiKhoan.TenTaiKhoan_RAM,

                    hanhDong:
                        $"Cập nhật thành tích tập thể phong trào Ba Nhất ({TenBangHienTai})",

                    ghiChu:
                        $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                );

                // ========================================================
                // 7. LƯU THÀNH CÔNG
                //
                // Chỉ đến đây mới được xem là thành công.
                // ========================================================

                await Task.Delay(400);

                // ========================================================
                // 8. TỰ ĐỘNG ĐÓNG FORM
                // ========================================================

                if (!IsDisposed && !Disposing)
                {
                    Close();
                }
            }
            catch (SqliteException ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[Form43] SQLITE SAVE ERROR:");

                System.Diagnostics.Debug.WriteLine(ex);

                MessageBox.Show(
                    "Không thể lưu dữ liệu vào cơ sở dữ liệu.\n\n" +
                    "Dữ liệu trên màn hình vẫn được giữ nguyên.\n\n" +
                    "Chi tiết kỹ thuật:\n" +
                    ex.Message,
                    "Lưu dữ liệu thất bại",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[Form43] SAVE ERROR:");

                System.Diagnostics.Debug.WriteLine(ex);

                MessageBox.Show(
                    "Lỗi trong quá trình lưu dữ liệu:\n\n" +
                    ex.Message,
                    "Lỗi hệ thống",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                // ========================================================
                // 9. CHỈ KHÔI PHỤC UI NẾU FORM VẪN CÒN TỒN TẠI
                // ========================================================

                if (!IsDisposed &&
                    !Disposing &&
                    kryptonButton_LuuDataDeNghi != null &&
                    !kryptonButton_LuuDataDeNghi.IsDisposed)
                {
                    kryptonButton_LuuDataDeNghi.Text =
                        tenNutGoc;

                    kryptonButton_LuuDataDeNghi.Enabled =
                        true;

                    kryptonButton_LuuDataDeNghi.Refresh();
                }
            }
        }
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