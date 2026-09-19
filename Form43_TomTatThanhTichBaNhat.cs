using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
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
            this.ShowInTaskbar = false;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            // Đăng ký sự kiện
            richTextBox1_TomTatThanhTichBaNhat.TextChanged += richTextBox1_TomTatThanhTichBaNhat_TextChanged;
            richTextBox1_TomTatThanhTichBaNhat.Leave += richTextBox1_TomTatThanhTichBaNhat_Leave;
            // 1. Đăng ký lắng nghe sự kiện thay đổi thời gian từ Module_HeThong
            Module_HeThong.OnThoiGianChanged += Module_HeThong_OnThoiGianChanged;
            // 2. Hủy đăng ký khi đóng Form để tránh rò rỉ bộ nhớ (Memory Leak)
            this.FormClosed += (s, e) =>
            {
                Module_HeThong.OnThoiGianChanged -= Module_HeThong_OnThoiGianChanged;
            };
        }
        private async void Form43_TomTatThanhTichBaNhat_Load(object? sender, EventArgs e)
        {
            AcceptButton = kryptonButton_LuuDataDeNghi;
            InitToolTips();
            // 1. Tải dữ liệu từ CSDL
            await CapNhatLabel3Async();
            await TaiDuLieuTomTatAsync();
            // 2. Cập nhật số ký tự & trạng thái nút cỡ chữ
            CapNhatSoLuongKyTu();
            KiemTraHienThiNutCoChu();
            // 3. Căn đều 1 lần duy nhất sau khi dữ liệu nạp xong
            CanDeuRichTextBox();
            // 4. Đưa con trỏ về cuối văn bản
            DuaConTroVeCuoiVanBan();
        }
        // Tự động kích hoạt khi Form4 (hoặc bất kỳ Form nào) bấm Lưu thông tin thành công
        private async void Module_HeThong_OnThoiGianChanged(object? sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(async () => await CapNhatLabel3Async()));
            }
            else
            {
                await CapNhatLabel3Async();
            }
        }
        // Hàm cập nhật chuỗi hiển thị cho label3 của Form43
        private async Task CapNhatLabel3Async()
        {
            try
            {
                // Tận dụng lại hàm đọc CSDL đã viết ở Module_HeThong
                var (cheDo, thang, nam) = await Module_HeThong.LayThongTinThoiGianAsync(_csdl2Path);
                string tieuDeGoc = "Tóm tắt thành tích trong phong trào thi đua \"Ba nhất\"";
                if (cheDo.Equals("Năm", StringComparison.OrdinalIgnoreCase))
                {
                    // Chế độ NĂM: "Tóm tắt thành tích trong phong trào thi đua "Ba nhất" năm xxx"
                    label3.Text = $"{tieuDeGoc} năm {nam}";
                }
                else
                {
                    // Chế độ THÁNG: Format tháng dạng 2 chữ số (VD: 05, 09, 12)
                    if (int.TryParse(thang, out int soThang))
                    {
                        thang = soThang.ToString("D2");
                    }
                    // "Tóm tắt thành tích trong phong trào thi đua "Ba nhất" tháng xx/năm-xxx"
                    label3.Text = $"{tieuDeGoc} tháng {thang}/{nam}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi cập nhật label3 Form43: " + ex.Message);
            }
        }
        private void CanDeuRichTextBox()
        {
            if (_dangCanDeu) return;
            if (richTextBox1_TomTatThanhTichBaNhat == null ||
                richTextBox1_TomTatThanhTichBaNhat.IsDisposed ||
                !richTextBox1_TomTatThanhTichBaNhat.IsHandleCreated ||
                richTextBox1_TomTatThanhTichBaNhat.TextLength == 0)
            {
                return;
            }
            _dangCanDeu = true;
            // Tắt cập nhật giao diện tạm thời để triệt tiêu nhấp nháy
            richTextBox1_TomTatThanhTichBaNhat.SuspendLayout();
            int selectionStart = richTextBox1_TomTatThanhTichBaNhat.SelectionStart;
            int selectionLength = richTextBox1_TomTatThanhTichBaNhat.SelectionLength;
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
                int textLength = richTextBox1_TomTatThanhTichBaNhat.TextLength;
                selectionStart = Math.Max(0, Math.Min(selectionStart, textLength));
                selectionLength = Math.Max(0, Math.Min(selectionLength, textLength - selectionStart));
                richTextBox1_TomTatThanhTichBaNhat.Select(selectionStart, selectionLength);
                // Mở lại giao diện
                richTextBox1_TomTatThanhTichBaNhat.ResumeLayout();
                _dangCanDeu = false;
            }
        }
        private void richTextBox1_TomTatThanhTichBaNhat_TextChanged(object? sender, EventArgs e)
        {
            // CHỈ đếm ký tự và ẩn/hiện nút - KHÔNG gọi CanDeuRichTextBox() ở đây
            CapNhatSoLuongKyTu();
            KiemTraHienThiNutCoChu();
        }
        private void richTextBox1_TomTatThanhTichBaNhat_Leave(object? sender, EventArgs e)
        {
            // Khi người dùng hoàn thành gõ và chuyển focus ra ngoài mới tiến hành căn đều
            CanDeuRichTextBox();
        }
        private void DuaConTroVeCuoiVanBan()
        {
            if (richTextBox1_TomTatThanhTichBaNhat == null || richTextBox1_TomTatThanhTichBaNhat.IsDisposed) return;
            richTextBox1_TomTatThanhTichBaNhat.Focus();
            richTextBox1_TomTatThanhTichBaNhat.SelectionStart = richTextBox1_TomTatThanhTichBaNhat.TextLength;
            richTextBox1_TomTatThanhTichBaNhat.SelectionLength = 0;
            richTextBox1_TomTatThanhTichBaNhat.ScrollToCaret();
        }
        private void CapNhatSoLuongKyTu()
        {
            if (richTextBox1_TomTatThanhTichBaNhat == null || toolStripStatusLabel1_SoLuongKyTu == null) return;
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
            if (!string.Equals(toolStripStatusLabel1_SoLuongKyTu.Text, noiDung, StringComparison.Ordinal))
            {
                toolStripStatusLabel1_SoLuongKyTu.Text = noiDung;
            }
            if (toolStripStatusLabel1_SoLuongKyTu.ForeColor != mauChu)
            {
                toolStripStatusLabel1_SoLuongKyTu.ForeColor = mauChu;
            }
        }
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            toolTip1.InitialDelay = 300;
            toolTip1.AutoPopDelay = 2500;
            toolTip1.ReshowDelay = 100;
            toolTip1.ShowAlways = true;
            var tips = new Dictionary<Control, string>
            {
                { kryptonButton2_GiamCoChuRichText, "Giảm cỡ chữ" },
                { kryptonButton2_TangCoChuRichText, "Tăng cỡ chữ" },
                { kryptonButton_LuuDataDeNghi, "Lưu thông tin thi đua tập thể phong trào Ba nhất vào cơ sở dữ liệu" }
            };
            foreach (var tip in tips)
            {
                if (tip.Key != null && !tip.Key.IsDisposed)
                {
                    toolTip1.SetToolTip(tip.Key, tip.Value);
                }
            }
        }
        private async Task TaiDuLieuTomTatAsync()
        {
            if (!File.Exists(_csdl2Path)) return;
            try
            {
                using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT NoiDung FROM [{TenBangHienTai}] WHERE ID = 1";
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    // Tạm gỡ sự kiện TextChanged để tránh đếm trùng lúc tải dữ liệu
                    richTextBox1_TomTatThanhTichBaNhat.TextChanged -= richTextBox1_TomTatThanhTichBaNhat_TextChanged;
                    richTextBox1_TomTatThanhTichBaNhat.Text = Module_BaoMatAES.GiaiMa(result.ToString()).Trim();
                    richTextBox1_TomTatThanhTichBaNhat.TextChanged += richTextBox1_TomTatThanhTichBaNhat_TextChanged;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi tải tóm tắt thành tích: " + ex.Message);
            }
        }
        private async void kryptonButton_LuuDataDeNghi_Click(object? sender, EventArgs e)
        {
            int soKyTu = richTextBox1_TomTatThanhTichBaNhat.TextLength;
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
            string tenNutGoc = kryptonButton_LuuDataDeNghi.Text;
            kryptonButton_LuuDataDeNghi.Text = "Đang lưu...";
            kryptonButton_LuuDataDeNghi.Enabled = false;
            kryptonButton_LuuDataDeNghi.Refresh();
            try
            {
                string noiDungTho = richTextBox1_TomTatThanhTichBaNhat.Text.Trim();
                string noiDungMaHoa = Module_BaoMatAES.MaHoa(noiDungTho);
                using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
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
                cmd.Parameters.AddWithValue("@NoiDung", noiDungMaHoa);
                int soDongAnhHuong = await cmd.ExecuteNonQueryAsync();
                if (soDongAnhHuong != 1)
                {
                    throw new InvalidOperationException("Không thể xác nhận dữ liệu đã được lưu đúng.");
                }
                Module_NhatKy.GhiNhatKy(
                    taiKhoan: string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM) ? "Không xác định" : Module_TaiKhoan.TenTaiKhoan_RAM,
                    hanhDong: $"Cập nhật thành tích tập thể phong trào Ba Nhất ({TenBangHienTai})",
                    ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                );
                await Task.Delay(400);
                if (!IsDisposed && !Disposing)
                {
                    Close();
                }
            }
            catch (SqliteException ex)
            {
                System.Diagnostics.Debug.WriteLine("[Form43] SQLITE SAVE ERROR:\n" + ex);
                MessageBox.Show(
                    "Không thể lưu dữ liệu vào cơ sở dữ liệu.\n\n" +
                    "Dữ liệu trên màn hình vẫn được giữ nguyên.\n\n" +
                    "Chi tiết kỹ thuật:\n" + ex.Message,
                    "Lưu dữ liệu thất bại",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Form43] SAVE ERROR:\n" + ex);
                MessageBox.Show(
                    "Lỗi trong quá trình lưu dữ liệu:\n\n" + ex.Message,
                    "Lỗi hệ thống",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                if (!IsDisposed && !Disposing && kryptonButton_LuuDataDeNghi != null && !kryptonButton_LuuDataDeNghi.IsDisposed)
                {
                    kryptonButton_LuuDataDeNghi.Text = tenNutGoc;
                    kryptonButton_LuuDataDeNghi.Enabled = true;
                    kryptonButton_LuuDataDeNghi.Refresh();
                }
            }
        }
        private void ThayDoiCoChuRichText(float delta)
        {
            if (richTextBox1_TomTatThanhTichBaNhat == null || richTextBox1_TomTatThanhTichBaNhat.IsDisposed) return;
            Font fontHienTai = richTextBox1_TomTatThanhTichBaNhat.Font;
            if (fontHienTai == null) return;
            float kichThuocMoi = fontHienTai.Size + delta;
            if (kichThuocMoi < RichText_MinFontSize) kichThuocMoi = RichText_MinFontSize;
            if (kichThuocMoi > RichText_MaxFontSize) kichThuocMoi = RichText_MaxFontSize;
            if (Math.Abs(kichThuocMoi - fontHienTai.Size) < 0.01f) return;
            richTextBox1_TomTatThanhTichBaNhat.SuspendLayout();
            try
            {
                richTextBox1_TomTatThanhTichBaNhat.Font = new Font(
                    fontHienTai.FontFamily,
                    kichThuocMoi,
                    fontHienTai.Style,
                    GraphicsUnit.Point);
            }
            finally
            {
                richTextBox1_TomTatThanhTichBaNhat.ResumeLayout();
                CanDeuRichTextBox(); // Căn đều lại sau khi đổi kích thước chữ
            }
        }
        private void kryptonButton2_TangCoChuRichText_Click(object? sender, EventArgs e)
        {
            ThayDoiCoChuRichText(RichText_FontStep);
        }
        private void kryptonButton2_GiamCoChuRichText_Click(object? sender, EventArgs e)
        {
            ThayDoiCoChuRichText(-RichText_FontStep);
        }
        private void KiemTraHienThiNutCoChu()
        {
            bool coNoiDung = !string.IsNullOrWhiteSpace(richTextBox1_TomTatThanhTichBaNhat.Text);
            kryptonButton2_TangCoChuRichText.Visible = coNoiDung;
            kryptonButton2_GiamCoChuRichText.Visible = coNoiDung;
        }
    }
}
///Yêu mèo cam - Lê Trung Kiên
///Yêu mèo thành bất tử - Lê Trung Kiên