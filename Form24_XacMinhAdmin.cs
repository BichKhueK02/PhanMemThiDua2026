using DocumentFormat.OpenXml.Math;
using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using BCrypt.Net;

namespace PhanMemThiDua2026
{
    public partial class Form24_XacMinhAdmin : Form
    {
        // 1. CONFIG & STATE 
        private readonly string _csdl1Path = Module_DanduongGPS.DuongDanCSDL1;

        // [CẬP NHẬT]: Thêm đường dẫn CSDL2 để đọc bảng cấu hình (ThongTin)
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;

        private const int MAX_SAI = 3;
        private const int THOI_GIAN_KHOA_PHUT = 5;
        private static int _soLanSaiToanCuc = 0;
        private static DateTime _thoiGianMoKhoa = DateTime.MinValue;

        private bool _isProcessing = false;

        // Cờ chống spam nháy đúp chuột liên tục
        private bool _dangXuLyGoiYTaiKhoan = false;

        private readonly string _cacheUserPath = Path.Combine(Application.StartupPath, "last_admin.txt");

        // 2. UI CONSTANTS
        private static readonly Color FocusBorderColor = Color.FromArgb(0, 120, 215);
        private static readonly Color NormalBorderColor = Color.Silver;
        private const int FocusBorderWidth = 2;
        private const int NormalBorderWidth = 1;

        // 3. CONSTRUCTOR
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

        // 4. QUẢN LÝ SỰ KIỆN
        private void RegisterEvents()
        {
            this.Load += Form24_XacMinhAdmin_Load;

            check_HienMatKhau.CheckedChanged += (s, e) => text_MatKhau.UseSystemPasswordChar = !check_HienMatKhau.Checked;

            btn_XacThuc.Click -= btn_XacThuc_Click;
            btn_XacThuc.Click += btn_XacThuc_Click;

            btn_Thoat.Click -= btn_Thoat_Click;
            btn_Thoat.Click += btn_Thoat_Click;

            PictureBox1.Click -= PictureBox1_Click;
            PictureBox1.Click += PictureBox1_Click;

            text_TenDangNhap.KeyDown += Text_TenDangNhap_KeyDown;
            text_MatKhau.KeyDown += Text_MatKhau_KeyDown;

            // [CẬP NHẬT]: Đăng ký sự kiện nháy đúp chuột để gợi ý tài khoản
            text_TenDangNhap.DoubleClick -= Text_TenDangNhap_DoubleClick;
            text_TenDangNhap.DoubleClick += Text_TenDangNhap_DoubleClick;
        }

        // 5. FORM LOAD
        // 1. CHUYỂN SỰ KIỆN LOAD THÀNH ASYNC ĐỂ CHẠY BẤT ĐỒNG BỘ
        private async void Form24_XacMinhAdmin_Load(object sender, EventArgs e)
        {
            // Đợi nạp tên tài khoản tự động (nếu được phép)
            await LoadGoiYTenTaiKhoanAsync();

            // Sau khi nạp xong, tự động nhảy con trỏ xuống ô Mật khẩu
            BeginInvoke(new Action(() =>
            {
                if (IsDisposed) return;
                text_MatKhau.Focus();
                text_MatKhau.Select();
            }));
        }

        // 2. LÕI TỰ ĐỘNG GỢI Ý TÊN ĐĂNG NHẬP
        private async Task LoadGoiYTenTaiKhoanAsync()
        {
            try
            {
                // BƯỚC 1: Kiểm tra cấu hình trong CSDL2 xem có cho phép gợi ý không
                if (CoChoPhepGoiYTenTaiKhoan())
                {
                    // BƯỚC 2: Truy xuất lấy Tên đăng nhập từ CSDL1
                    if (!string.IsNullOrWhiteSpace(_csdl1Path) && File.Exists(_csdl1Path))
                    {
                        string connStr = $"Data Source={_csdl1Path};Mode=ReadOnly;Default Timeout=5;Pooling=True;";
                        await using var conn = new SqliteConnection(connStr);
                        await conn.OpenAsync();

                        const string sql = "SELECT TenTaiKhoan FROM Admin WHERE ID = 1 LIMIT 1";
                        await using var cmd = new SqliteCommand(sql, conn);

                        var val = await cmd.ExecuteScalarAsync();

                        if (val != null && val != DBNull.Value)
                        {
                            // Giải mã AES 
                            string tenTK = BaoMatAES.GiaiMa(val.ToString() ?? string.Empty);

                            if (!string.IsNullOrEmpty(tenTK))
                            {
                                text_TenDangNhap.Text = tenTK;
                                return; // Điền thành công thì thoát hàm ngay, không cần quét Cache nữa
                            }
                        }
                    }
                }

                // BƯỚC 3: DỰ PHÒNG (Fallback) 
                // Nếu người dùng thiết lập KHÔNG cho phép (FALSE), ta sẽ lấy lại tên của lần đăng nhập thành công gần nhất từ file ẩn
                if (File.Exists(_cacheUserPath))
                {
                    string savedUser = File.ReadAllText(_cacheUserPath).Trim();
                    if (!string.IsNullOrEmpty(savedUser))
                    {
                        text_TenDangNhap.Text = savedUser;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi LoadGoiYTenTaiKhoanAsync Form24]: {ex.Message}");
            }
        }
        // TÍNH NĂNG MỚI: KIỂM TRA ĐIỀU KIỆN VÀ GỢI Ý AUTO-FILL TÊN ĐĂNG NHẬP
        // ========================================================================
        private bool CoChoPhepGoiYTenTaiKhoan()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path))
                    return false;

                // 🛡️ Mở kết nối với Timeout ngắn vì đây chỉ là hàm kiểm tra nhanh
                using var cn = new SqliteConnection($"Data Source={_csdl2Path};Mode=ReadOnly;Default Timeout=5;Pooling=True;");
                cn.Open();

                using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT ChoPhepGoiYMatKhau FROM ThongTin WHERE ID = 1 LIMIT 1";

                object val = cmd.ExecuteScalar();
                if (val == null || val == DBNull.Value) return false;

                string giaiMa = BaoMatAES.GiaiMa(val.ToString() ?? string.Empty);
                return string.Equals(giaiMa, "TRUE", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private async void Text_TenDangNhap_DoubleClick(object sender, EventArgs e)
        {
            // 1. Chống Spam Click
            if (_dangXuLyGoiYTaiKhoan) return;

            try
            {
                _dangXuLyGoiYTaiKhoan = true;

                // 2. Kiểm tra cờ cho phép từ cơ sở dữ liệu cấu hình
                if (!CoChoPhepGoiYTenTaiKhoan())
                {
                    return; // Nếu trả về FALSE thì không làm gì cả, im lặng thoát
                }

                // 3. Tiến hành truy xuất tên đăng nhập nếu được phép
                if (string.IsNullOrWhiteSpace(_csdl1Path) || !File.Exists(_csdl1Path))
                    return;

                string connStr = $"Data Source={_csdl1Path};Mode=ReadOnly;Default Timeout=5;Pooling=True;";
                await using var conn = new SqliteConnection(connStr);
                await conn.OpenAsync();

                const string sql = "SELECT TenTaiKhoan FROM Admin WHERE ID = 1 LIMIT 1";
                await using var cmd = new SqliteCommand(sql, conn);

                var val = await cmd.ExecuteScalarAsync();

                if (val != null && val != DBNull.Value)
                {
                    // 4. Giải mã và nạp lên giao diện
                    string tenTK = BaoMatAES.GiaiMa(val.ToString() ?? string.Empty);

                    text_TenDangNhap.Text = tenTK;
                    text_MatKhau.Focus(); // Tự động đưa con trỏ xuống ô mật khẩu cho mượt
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi gợi ý tên đăng nhập Form 24]: {ex.Message}");
            }
            finally
            {
                _dangXuLyGoiYTaiKhoan = false;
            }
        }

        // 6. LÕI XÁC THỰC BẢO MẬT (Async/Await)
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

                bool isFoundAndMatched = false;

                while (await reader.ReadAsync())
                {
                    string userDecrypted = BaoMatAES.GiaiMa(reader.GetString(0));
                    string passDecrypted = BaoMatAES.GiaiMa(reader.GetString(1));

                    if (userDecrypted == tenDangNhap && passDecrypted == matKhauNhap)
                    {
                        isFoundAndMatched = true;
                        break;
                    }
                }

                if (isFoundAndMatched)
                {
                    return (true, string.Empty);
                }
                else
                {
                    return (false, "Tên đăng nhập hoặc mật khẩu không chính xác!");
                }
            }
            catch (Exception)
            {
                return (false, "Hệ thống gặp sự cố an ninh khi xác thực. Vui lòng thử lại sau.");
            }
        }

        private async void btn_XacThuc_Click(object sender, EventArgs e)
        {
            if (DateTime.Now < _thoiGianMoKhoa)
            {
                TimeSpan thoiGianConLai = _thoiGianMoKhoa - DateTime.Now;
                MessageBox.Show($"Tài khoản bị khóa do nhập sai nhiều lần.\nVui lòng thử lại sau {thoiGianConLai.Minutes} phút {thoiGianConLai.Seconds} giây.",
                    "Cảnh báo an ninh", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_isProcessing) return;

            try
            {
                _isProcessing = true;
                btn_XacThuc.Enabled = false;
                Cursor = Cursors.WaitCursor;

                string tenNhap = text_TenDangNhap.Text.Trim();
                string passNhap = text_MatKhau.Text;

                var (isSuccess, errorMessage) = await KiemTraAdminAsync(tenNhap, passNhap);

                if (isSuccess)
                {
                    _soLanSaiToanCuc = 0;

                    try
                    {
                        File.WriteAllText(_cacheUserPath, tenNhap);
                    }
                    catch { }

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    return;
                }

                _soLanSaiToanCuc++;
                text_MatKhau.Clear();
                text_MatKhau.Focus();

                if (_soLanSaiToanCuc >= MAX_SAI)
                {
                    _thoiGianMoKhoa = DateTime.Now.AddMinutes(THOI_GIAN_KHOA_PHUT);
                    MessageBox.Show(
                        $"Phát hiện có nỗ lực truy cập trái phép!\nPhiên xác minh đã bị khóa {THOI_GIAN_KHOA_PHUT} phút.",
                        "Cảnh báo an ninh", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                }
                else
                {
                    MessageBox.Show(
                        $"{errorMessage}\nBạn đã nhập sai lần {_soLanSaiToanCuc}/{MAX_SAI}.",
                        "Xác thực thất bại", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            finally
            {
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
                "Xác thực quản trị viên", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // 8. UX TỐI ƯU
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