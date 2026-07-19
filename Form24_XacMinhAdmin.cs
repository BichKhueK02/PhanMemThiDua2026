using Krypton.Toolkit;
using Microsoft.Data.Sqlite;

namespace PhanMemThiDua2026
{
    public partial class Form24_XacMinhAdmin : Form
    {
        // =====================================================
        // 1. CONFIG & STATE
        // =====================================================
        private readonly string _csdl1Path = Module_DanduongGPS.DuongDanCSDL1;
        private const int MAX_SAI = 3;
        private int _soLanSai = 0;

        // Cờ chống double-click hoặc thao tác khi đang truy xuất DB
        private bool _isProcessing = false;

        // =====================================================
        // 2. UI CONSTANTS (Fluent Design)
        // =====================================================
        private static readonly Color FocusBorderColor = Color.FromArgb(0, 120, 215); // Xanh dương Win 11
        private static readonly Color NormalBorderColor = Color.Silver;               // Màu bạc mặc định
        private const int FocusBorderWidth = 2;
        private const int NormalBorderWidth = 1;

        // =====================================================
        // 3. CONSTRUCTOR (Chỉ dựng giao diện, KHÔNG gọi DB ở đây)
        // =====================================================
        public Form24_XacMinhAdmin()
        {
            InitializeComponent();
            SetupFormProperties();
            RegisterEvents();
            InitToolTips();
            InitFocusEffects();
        }

        private void SetupFormProperties()
        {
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            CancelButton = btn_Thoat;
            text_MatKhau.UseSystemPasswordChar = true;
        }

        // =====================================================
        // 4. QUẢN LÝ SỰ KIỆN (Tránh lỗi mất Event của VS Designer)
        // =====================================================
        private void RegisterEvents()
        {
            // Vòng đời Form
            this.Load += Form24_XacMinhAdmin_Load;

            // Xử lý Checkbox & Nút bấm
            check_HienMatKhau.CheckedChanged += (s, e) => text_MatKhau.UseSystemPasswordChar = !check_HienMatKhau.Checked;
            btn_XacThuc.Click -= btn_XacThuc_Click; // Hủy đăng ký cũ (nếu có)
            btn_XacThuc.Click += btn_XacThuc_Click;
            btn_Thoat.Click -= btn_Thoat_Click;
            btn_Thoat.Click += btn_Thoat_Click;
            PictureBox1.Click -= PictureBox1_Click;
            PictureBox1.Click += PictureBox1_Click;

            // Xử lý Phím tắt (Enter)
            text_TenDangNhap.KeyDown += Text_TenDangNhap_KeyDown;
            text_MatKhau.KeyDown += Text_MatKhau_KeyDown;
        }

        // =====================================================
        // 5. FORM LOAD (Chạy bất đồng bộ, không làm đơ Form khi mở)
        // =====================================================
        private async void Form24_XacMinhAdmin_Load(object sender, EventArgs e)
        {
            await LoadGoiYTenTaiKhoanAsync();

            BeginInvoke(new Action(() =>
            {
                if (IsDisposed) return;

                text_MatKhau.Focus();
                text_MatKhau.Select();
            }));
        }
        private async Task LoadGoiYTenTaiKhoanAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_csdl1Path) || !File.Exists(_csdl1Path)) return;

                // Sử dụng "await using" để tự động giải phóng tài nguyên (C# 8.0)
                await using var cn = new SqliteConnection($"Data Source={_csdl1Path}");
                await cn.OpenAsync(); // Mở kết nối bất đồng bộ

                await using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT TenTaiKhoan FROM Admin ORDER BY ID DESC LIMIT 1";

                var obj = await cmd.ExecuteScalarAsync(); // Đọc bất đồng bộ

                if (obj != null)
                {
                    text_TenDangNhap.Text = BaoMatAES.GiaiMa(obj.ToString());
                    text_TenDangNhap.ReadOnly = true;
                }
            }
            catch
            {
                text_TenDangNhap.ReadOnly = false;
            }
        }

        // =====================================================
        // 6. CORE LOGIC XÁC THỰC (Bất đồng bộ - Async/Await)
        // =====================================================
        private async Task<(bool isSuccess, string errorMessage)> KiemTraAdminAsync(string tenDangNhap, string matKhauNhap)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_csdl1Path) || !File.Exists(_csdl1Path))
                    return (false, "Không tìm thấy CSDL hệ thống!");

                await using var cn = new SqliteConnection($"Data Source={_csdl1Path}");
                await cn.OpenAsync();

                string sql = "SELECT TenTaiKhoan, MatKhau FROM Admin";
                await using var cmd = new SqliteCommand(sql, cn);
                await using var reader = await cmd.ExecuteReaderAsync();

                bool saiTenDangNhap = true;

                while (await reader.ReadAsync())
                {
                    string userDecrypted = BaoMatAES.GiaiMa(reader.GetString(0));
                    string passDecrypted = BaoMatAES.GiaiMa(reader.GetString(1));

                    if (userDecrypted == tenDangNhap)
                    {
                        saiTenDangNhap = false;
                        if (passDecrypted == matKhauNhap)
                            return (true, string.Empty); // Khớp hoàn toàn
                    }
                }

                string error = saiTenDangNhap ? "Tên đăng nhập không tồn tại!" : "Mật khẩu không chính xác!";
                return (false, error);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi truy xuất hệ thống: {ex.Message}");
            }
        }

        // =====================================================
        // 7. SỰ KIỆN NÚT BẤM VÀ BÀN PHÍM
        // =====================================================
        private async void btn_XacThuc_Click(object sender, EventArgs e)
        {
            // Tránh user spam click khi đang xử lý
            if (_isProcessing) return;

            try
            {
                _isProcessing = true;
                btn_XacThuc.Enabled = false; // Vô hiệu hóa nút
                Cursor = Cursors.WaitCursor; // Đổi chuột sang biểu tượng chờ

                string tenNhap = text_TenDangNhap.Text.Trim();
                string passNhap = text_MatKhau.Text;

                // Sử dụng Tuples C# hiện đại thay cho tham số 'out string'
                var (isSuccess, errorMessage) = await KiemTraAdminAsync(tenNhap, passNhap);

                if (isSuccess)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    return;
                }

                _soLanSai++;
                text_MatKhau.Clear();
                text_MatKhau.Focus();

                if (_soLanSai >= MAX_SAI)
                {
                    MessageBox.Show(
                        "Hệ thống nghi ngờ bạn là kẻ phá hoại!\nPhiên xác minh đã bị khóa.",
                        "Cảnh báo an ninh", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                }
                else
                {
                    MessageBox.Show(
                        $"{errorMessage}\nBạn đã nhập sai lần {_soLanSai}/{MAX_SAI}.",
                        "Xác thực thất bại", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            finally
            {
                // Luôn khôi phục trạng thái dù có lỗi hay không
                _isProcessing = false;
                btn_XacThuc.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private void btn_Thoat_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void Text_TenDangNhap_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                text_MatKhau.Focus();
            }
        }

        private void Text_MatKhau_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                btn_XacThuc.PerformClick();
            }
        }

        private void PictureBox1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Vui lòng nhập mật khẩu xác thực để thực hiện thao tác xóa!",
                "Xác thực quản trị viên", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // =====================================================
        // 8. UX TỐI ƯU (HIỆU ỨNG & GỢI Ý)
        // =====================================================
        private void InitFocusEffects()
        {
            var controls = new List<KryptonTextBox> { text_TenDangNhap, text_MatKhau };
            foreach (var ktb in controls)
            {
                if (ktb == null) continue;

                ktb.StateCommon.Border.DrawBorders = PaletteDrawBorders.All;
                ktb.StateCommon.Border.Color1 = NormalBorderColor;
                ktb.StateCommon.Border.Width = NormalBorderWidth;

                ktb.Enter += (s, e) =>
                {
                    ktb.StateCommon.Border.Color1 = FocusBorderColor;
                    ktb.StateCommon.Border.Width = FocusBorderWidth;
                    ktb.Refresh();
                };

                ktb.Leave += (s, e) =>
                {
                    ktb.StateCommon.Border.Color1 = NormalBorderColor;
                    ktb.StateCommon.Border.Width = NormalBorderWidth;
                    ktb.Refresh();
                };
            }
        }

        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Gợi ý đăng nhập";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            toolTip1.InitialDelay = 300;
            toolTip1.AutoPopDelay = 2000;
            toolTip1.ReshowDelay = 100;
            toolTip1.ShowAlways = true;

            var tips = new Dictionary<Control, string>
            {
                { text_TenDangNhap, "Nhập tên đăng nhập hệ thống" },
                { text_MatKhau, "Nhập mật khẩu đăng nhập" },
                { check_HienMatKhau, "Hiển thị / ẩn mật khẩu đang nhập" },
                { btn_XacThuc, "Xác thực thông tin để tiếp tục" },
                { btn_Thoat, "Thoát khỏi chương trình" }
            };

            foreach (var tip in tips)
            {
                if (tip.Key != null) toolTip1.SetToolTip(tip.Key, tip.Value);
            }
        }


    }
}
