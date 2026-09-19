using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using System.Runtime.InteropServices;
namespace PhanMemThiDua2026
{
    public partial class Form56_TomTatThanhTichTapTheHangThang : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private const int GioiHanKhuyenNghi = 830;
        private const float RichText_MinFontSize = 8f;
        private const float RichText_MaxFontSize = 30f;
        private const float RichText_FontStep = 1f;
        private bool _daLoadDuLieuThanhCong;
        private bool _dangLuu;
        // ============================================================
        // CĂN ĐỀU ĐOẠN VĂN CHO RICHTEXTBOX (WIN32 API)
        // ============================================================
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
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref PARAFORMAT lParam);
        private void CanDeuRichTextBox()
        {
            if (richTextBox1_TomTatThanhTichTapThe == null ||
                richTextBox1_TomTatThanhTichTapThe.IsDisposed ||
                !richTextBox1_TomTatThanhTichTapThe.IsHandleCreated ||
                richTextBox1_TomTatThanhTichTapThe.TextLength == 0)
            {
                return;
            }
            // Tắt cập nhật giao diện tạm thời để chống nhấp nháy hoàn toàn
            richTextBox1_TomTatThanhTichTapThe.SuspendLayout();
            int selectionStart = richTextBox1_TomTatThanhTichTapThe.SelectionStart;
            int selectionLength = richTextBox1_TomTatThanhTichTapThe.SelectionLength;
            try
            {
                richTextBox1_TomTatThanhTichTapThe.SelectAll();
                var paraFormat = new PARAFORMAT
                {
                    cbSize = Marshal.SizeOf<PARAFORMAT>(),
                    dwMask = PFM_ALIGNMENT,
                    wAlignment = PFA_JUSTIFY,
                    rgxTabs = new int[32]
                };
                SendMessage(richTextBox1_TomTatThanhTichTapThe.Handle, EM_SETPARAFORMAT, IntPtr.Zero, ref paraFormat);
            }
            finally
            {
                int textLength = richTextBox1_TomTatThanhTichTapThe.TextLength;
                selectionStart = Math.Max(0, Math.Min(selectionStart, textLength));
                selectionLength = Math.Max(0, Math.Min(selectionLength, textLength - selectionStart));
                richTextBox1_TomTatThanhTichTapThe.Select(selectionStart, selectionLength);
                richTextBox1_TomTatThanhTichTapThe.ResumeLayout();
            }
        }
        public Form56_TomTatThanhTichTapTheHangThang()
        {
            InitializeComponent();
            ShowInTaskbar = false;
            MaximizeBox = false;
            MinimizeBox = false;
            CauHinhStatusStrip();
            // Gắn sự kiện
            richTextBox1_TomTatThanhTichTapThe.TextChanged += richTextBox1_TomTatThanhTichTapThe_TextChanged;
            richTextBox1_TomTatThanhTichTapThe.Leave += richTextBox1_TomTatThanhTichTapThe_Leave;
            // Đăng ký lắng nghe sự kiện
            Module_HeThong.OnThoiGianChanged += Module_HeThong_OnThoiGianChanged;
            // Hủy đăng ký khi đóng form để tránh Memory Leak
            this.FormClosed += (s, e) =>
            {
                Module_HeThong.OnThoiGianChanged -= Module_HeThong_OnThoiGianChanged;
            };
        }
        private async void Form56_TomTatThanhTichTapTheHangThang_Load(object? sender, EventArgs e)
        {
            kryptonButton_LuuDataDeNghi.Enabled = false;
            AcceptButton = kryptonButton_LuuDataDeNghi;
            InitToolTips();
            toolStripStatusLabel1_ThongBao.Text = string.Empty;
            await TaiDuLieuTomTatAsync();
            await CapNhatLabel3Async();
            if (_daLoadDuLieuThanhCong)
            {
                kryptonButton_LuuDataDeNghi.Enabled = true;
                CapNhatSoLuongKyTu();
                CanDeuRichTextBox();
                DuaConTroVeCuoiVanBan();
            }
        }
        // Tự động kích hoạt khi Form4 (hoặc form khác) bấm Lưu
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
        // Cập nhật giao diện Label3
        private async Task CapNhatLabel3Async()
        {
            try
            {
                var (cheDo, thang, nam) = await Module_HeThong.LayThongTinThoiGianAsync(_csdl2Path);
                string tieuDeGoc = "Tóm tắt thành tích tập thể phong trào thi đua \"Vì ANTQ\"";
                if (cheDo.Equals("Năm", StringComparison.OrdinalIgnoreCase))
                {
                    // Chế độ NĂM: "Tóm tắt thành tích tập thể trong phong trào thi đua "Vì ANTQ" năm xxx"
                    label3.Text = $"{tieuDeGoc} năm {nam}";
                }
                else
                {
                    // Chế độ THÁNG: Chuẩn hóa tháng dạng 2 chữ số (VD: 05, 09, 12)
                    if (int.TryParse(thang, out int soThang))
                    {
                        thang = soThang.ToString("D2");
                    }
                    // "Tóm tắt thành tích tập thể trong phong trào thi đua "Vì ANTQ" tháng xx/năm-xxx"
                    label3.Text = $"{tieuDeGoc} tháng {thang}/{nam}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi cập nhật label3: " + ex.Message);
            }
        }
        private void richTextBox1_TomTatThanhTichTapThe_TextChanged(object? sender, EventArgs e)
        {
            // CHỈ cập nhật đếm ký tự & nút cỡ chữ - KHÔNG căn đều tại đây để tránh giật màn hình
            CapNhatSoLuongKyTu();
            KiemTraHienThiNutCoChu();
        }
        private void richTextBox1_TomTatThanhTichTapThe_Leave(object? sender, EventArgs e)
        {
            // Căn đều khi người dùng gõ xong và di chuyển con trỏ chuột ra ngoài
            CanDeuRichTextBox();
        }
        private async Task TaiDuLieuTomTatAsync()
        {
            _daLoadDuLieuThanhCong = false;
            if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path))
            {
                MessageBox.Show("Không tìm thấy cơ sở dữ liệu CSDL2.", "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                var connectionString = new SqliteConnectionStringBuilder
                {
                    DataSource = _csdl2Path,
                    Mode = SqliteOpenMode.ReadOnly
                }.ToString();
                using var conn = new SqliteConnection(connectionString);
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT TomTatGhiChu FROM ThongTin WHERE ID = 1;";
                object? result = await cmd.ExecuteScalarAsync();
                string duLieuMaHoa = result?.ToString() ?? string.Empty;
                string noiDungGiaiMa = string.IsNullOrEmpty(duLieuMaHoa)
                    ? string.Empty
                    : (Module_BaoMatAES.GiaiMa(duLieuMaHoa) ?? string.Empty);
                richTextBox1_TomTatThanhTichTapThe.Text = noiDungGiaiMa.Trim();
                _daLoadDuLieuThanhCong = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải dữ liệu: {ex.Message}", "Lỗi tải dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async void kryptonButton_LuuDataDeNghi_Click(object? sender, EventArgs e)
        {
            if (_dangLuu || !_daLoadDuLieuThanhCong) return;
            // 1. Tự động cắt bỏ khoảng trắng thừa ở đầu/cuối chuỗi
            string noiDungLuu = richTextBox1_TomTatThanhTichTapThe.Text.Trim();
            richTextBox1_TomTatThanhTichTapThe.Text = noiDungLuu;
            // 2. Kiểm tra độ dài
            if (noiDungLuu.Length > Module_BaNhat.GioiHanToiDa)
            {
                MessageBox.Show(
                    $"Nội dung hiện có {noiDungLuu.Length} ký tự, vượt quá giới hạn cho phép ({Module_BaNhat.GioiHanToiDa} ký tự).\n\nVui lòng rút gọn nội dung trước khi lưu.",
                    "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                richTextBox1_TomTatThanhTichTapThe.Focus();
                return;
            }
            _dangLuu = true;
            string tenNutGoc = kryptonButton_LuuDataDeNghi.Text;
            kryptonButton_LuuDataDeNghi.Text = "Đang lưu...";
            kryptonButton_LuuDataDeNghi.Enabled = false;
            try
            {
                // 3. Thực hiện lưu vào SQLite đơn giản & mượt mà
                string noiDungMaHoa = string.IsNullOrEmpty(noiDungLuu) ? string.Empty : Module_BaoMatAES.MaHoa(noiDungLuu);
                var connectionString = new SqliteConnectionStringBuilder
                {
                    DataSource = _csdl2Path,
                    Mode = SqliteOpenMode.ReadWrite
                }.ToString();
                using (var conn = new SqliteConnection(connectionString))
                {
                    await conn.OpenAsync();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "UPDATE ThongTin SET TomTatGhiChu = @TomTatGhiChu WHERE ID = 1;";
                    cmd.Parameters.AddWithValue("@TomTatGhiChu", noiDungMaHoa);
                    await cmd.ExecuteNonQueryAsync();
                }
                // Ghi nhật ký
                Module_NhatKy.GhiNhatKy(
                    Module_TaiKhoan.TenTaiKhoan_RAM,
                    hanhDong: "Cập nhật thành tích tập thể phong trào thi đua Vì ANTQ",
                    ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                );
                // Thông báo thành công và đóng form
                await HienThongBaoLuuThanhCongAsync();
                await Task.Delay(300);
                if (!IsDisposed && !Disposing)
                {
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể lưu dữ liệu: {ex.Message}", "Lỗi lưu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _dangLuu = false;
                if (!IsDisposed && !Disposing)
                {
                    kryptonButton_LuuDataDeNghi.Text = tenNutGoc;
                    kryptonButton_LuuDataDeNghi.Enabled = true;
                }
            }
        }
        private void CauHinhStatusStrip()
        {
            if (toolStripStatusLabel1_ThongBao == null || toolStripStatusLabel1_SoLuongKyTu == null) return;
            toolStripStatusLabel1_SoLuongKyTu.TextAlign = ContentAlignment.MiddleLeft;
            toolStripStatusLabel1_SoLuongKyTu.ImageAlign = ContentAlignment.MiddleLeft;
            toolStripStatusLabel1_SoLuongKyTu.TextImageRelation = TextImageRelation.ImageBeforeText;
            toolStripStatusLabel1_SoLuongKyTu.Spring = true;
            toolStripStatusLabel1_ThongBao.TextAlign = ContentAlignment.MiddleRight;
            toolStripStatusLabel1_ThongBao.Spring = false;
        }
        private void CapNhatSoLuongKyTu()
        {
            if (richTextBox1_TomTatThanhTichTapThe == null || toolStripStatusLabel1_SoLuongKyTu == null) return;
            int soKyTu = richTextBox1_TomTatThanhTichTapThe.TextLength;
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
            toolStripStatusLabel1_SoLuongKyTu.Text = noiDung;
            toolStripStatusLabel1_SoLuongKyTu.ForeColor = mauChu;
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
                { kryptonButton_LuuDataDeNghi, "Lưu thông tin thi đua tập thể phong trào thi đua 'Vì ANTQ' vào cơ sở dữ liệu" }
            };
            foreach (var tip in tips)
            {
                if (tip.Key != null && !tip.Key.IsDisposed)
                {
                    toolTip1.SetToolTip(tip.Key, tip.Value);
                }
            }
        }
        private async Task HienThongBaoLuuThanhCongAsync()
        {
            if (IsDisposed || Disposing || toolStripStatusLabel1_ThongBao == null) return;
            toolStripStatusLabel1_ThongBao.Text = "Đã lưu tóm tắt thành tích vào cơ sở dữ liệu.";
            toolStripStatusLabel1_ThongBao.Visible = true;
            Module_ThongBao.ThanhCong($"Đã cập nhật tóm tắt thành tích");
            await Task.Delay(400);
        }
        private void DuaConTroVeCuoiVanBan()
        {
            if (richTextBox1_TomTatThanhTichTapThe == null || richTextBox1_TomTatThanhTichTapThe.IsDisposed) return;
            richTextBox1_TomTatThanhTichTapThe.Focus();
            richTextBox1_TomTatThanhTichTapThe.SelectionStart = richTextBox1_TomTatThanhTichTapThe.TextLength;
            richTextBox1_TomTatThanhTichTapThe.SelectionLength = 0;
            richTextBox1_TomTatThanhTichTapThe.ScrollToCaret();
        }
        private void ThayDoiCoChuRichText(float delta)
        {
            if (richTextBox1_TomTatThanhTichTapThe == null || richTextBox1_TomTatThanhTichTapThe.IsDisposed) return;
            Font fontHienTai = richTextBox1_TomTatThanhTichTapThe.Font;
            if (fontHienTai == null) return;
            float kichThuocMoi = fontHienTai.Size + delta;
            if (kichThuocMoi >= RichText_MaxFontSize) kichThuocMoi = RichText_MaxFontSize;
            if (kichThuocMoi <= RichText_MinFontSize) kichThuocMoi = RichText_MinFontSize;
            if (Math.Abs(kichThuocMoi - fontHienTai.Size) < 0.01f) return;
            richTextBox1_TomTatThanhTichTapThe.Font = new Font(fontHienTai.FontFamily, kichThuocMoi, fontHienTai.Style, GraphicsUnit.Point);
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
            bool coNoiDung = !string.IsNullOrWhiteSpace(richTextBox1_TomTatThanhTichTapThe.Text);
            kryptonButton2_TangCoChuRichText.Visible = coNoiDung;
            kryptonButton2_GiamCoChuRichText.Visible = coNoiDung;
        }
    }
}