using Krypton.Toolkit; // 🟢 BẮT BUỘC THÊM ĐỂ SỬ DỤNG GIAO DIỆN KRYPTON
using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace PhanMemThiDua2026
{
    public partial class Form8_CauHinhCSDL : Form
    {
        private readonly string _csdl1Path = Module_DanduongGPS.DuongDanCSDL1;
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private int soLanSai = 0;
        private const int MaxSai = 5;
        // 🌟 KHAI BÁO CẤU HÌNH VIỀN UI/UX (CHUẨN WINDOWS 11)
        private static readonly Color FocusBorderColor = Color.FromArgb(0, 120, 215); // Xanh dương Win 11
        private static readonly Color NormalBorderColor = Color.Silver;               // Xám bạc mặc định
        private const int FocusBorderWidth = 2;                                       // Độ dày khi Focus
        private const int NormalBorderWidth = 1;                                      // Độ dày bình thường
        public Form8_CauHinhCSDL()
        {
            InitializeComponent();
            InitToolTips_CauHinhCSDL();
        }
        private void Form8_Load(object sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.AcceptButton = btn_DangNhap;
            int h = Text_Admin.Height + 2;
            foreach (Control btn in new Control[] { btn_DangNhap, btn_Thoat })
                btn.Height = h;
            LoadTaiKhoanMacDinh();
            CapNhatTrangThaiHienMatKhau();
            // 🌟 KÍCH HOẠT TÍNH NĂNG TÔ MÀU VIỀN TỰ ĐỘNG
            InitFocusEffects();

            Text_Password.Focus();
        }
        // 🌟 HÀM TÔ MÀU VIỀN CHUẨN KỸ SƯ (CHỐNG MEMORY LEAK)
        private void InitFocusEffects()
        {
            // Đảm bảo Text_Admin và Text_Password là dạng KryptonTextBox trong Designer
            var controls = new List<KryptonTextBox> { Text_Admin, Text_Password };
            foreach (var ktb in controls)
            {
                if (ktb == null) continue;

                // Thiết lập viền mặc định
                ktb.StateCommon.Border.DrawBorders = PaletteDrawBorders.All;
                ktb.StateCommon.Border.Color1 = NormalBorderColor;
                ktb.StateCommon.Border.Width = NormalBorderWidth;

                // An toàn: Hủy đăng ký trước khi gắn sự kiện mới
                ktb.Enter -= Ktb_EnterFocus;
                ktb.Leave -= Ktb_LeaveFocus;

                ktb.Enter += Ktb_EnterFocus;
                ktb.Leave += Ktb_LeaveFocus;
            }
        }
        private void Ktb_EnterFocus(object sender, EventArgs e)
        {
            if (sender is KryptonTextBox ktb)
            {
                ktb.StateCommon.Border.Color1 = FocusBorderColor;
                ktb.StateCommon.Border.Width = FocusBorderWidth;
                ktb.Refresh(); // Cập nhật UI ngay lập tức
            }
        }
        private void Ktb_LeaveFocus(object sender, EventArgs e)
        {
            if (sender is KryptonTextBox ktb)
            {
                ktb.StateCommon.Border.Color1 = NormalBorderColor;
                ktb.StateCommon.Border.Width = NormalBorderWidth;
                ktb.Refresh();
            }
        }
        // CÁC HÀM XỬ LÝ GỐC CỦA BẠN (GIỮ NGUYÊN 100%)
        private void InitToolTips_CauHinhCSDL()
        {
            var toolTip_CSDL = new ToolTip
            {
                IsBalloon = true,
                ToolTipTitle = "Cấu hình cơ sở dữ liệu",
                ToolTipIcon = ToolTipIcon.Info,
                InitialDelay = 200,
                AutoPopDelay = 1500,
                ReshowDelay = 100,
                ShowAlways = true
            };

            var tips = new Dictionary<Control, string>
            {
                { Chex_HienMatKhau, "Hiển thị/ẩn mật khẩu đăng nhập" },
                { btn_DangNhap, "Xác thực thông tin và truy cập cấu hình CSDL" },
                { btn_Thoat, "Thoát khỏi màn hình cấu hình cơ sở dữ liệu" }
            };

            foreach (var tip in tips)
            {
                if (tip.Key != null)
                    toolTip_CSDL.SetToolTip(tip.Key, tip.Value);
            }
        }
        private string GetValidCsdlPath()
        {
            if (!string.IsNullOrWhiteSpace(_csdl2Path) && File.Exists(_csdl2Path))
                return _csdl2Path;

            if (!string.IsNullOrWhiteSpace(_csdl1Path) && File.Exists(_csdl1Path))
                return _csdl1Path;

            return string.Empty;
        }
        private void LoadTaiKhoanMacDinh()
        {
            try
            {
                string csdl = _csdl1Path;
                if (string.IsNullOrEmpty(csdl) || !File.Exists(csdl))
                {
                    csdl = GetValidCsdlPath();
                }

                if (string.IsNullOrEmpty(csdl))
                {
                    Debug.WriteLine("DEBUG: Không tìm thấy tệp CSDL để load tên tài khoản.");
                    Text_Admin.Text = "";
                    return;
                }

                using SqliteConnection conn = new SqliteConnection("Data Source=" + csdl);
                conn.Open();

                string sql = "SELECT TenTaiKhoan FROM Admin WHERE ID = 1 LIMIT 1";
                using SqliteCommand cmd = new SqliteCommand(sql, conn);
                using SqliteDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    string encrypted = reader.GetString(0);
                    // ⭐ SỬA LỖI ĐỌC (FALLBACK): Xử lý an toàn nếu AES nuốt lỗi khi CSDL chưa mã hóa
                    string dec = BaoMatAES.GiaiMa(encrypted);
                    Text_Admin.Text = string.IsNullOrWhiteSpace(dec) ? encrypted : dec;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DEBUG: Lỗi LoadTaiKhoanMacDinh: {ex.Message}");
                Text_Admin.Text = "";
            }
        }
        private void Form8_FormClosing(object sender, FormClosingEventArgs e)
        {
            // GIA CỐ: Nếu Form này được gọi bằng .ShowDialog(), việc Hide() có thể gây treo ẩn.
            // Tốt nhất là chỉ Hide khi ứng dụng cấu hình chạy nền, nếu không hãy để nó Close bình thường.
            e.Cancel = true;
            this.Hide();
        }
        private bool KiemTraTaiKhoanTrongCSDL(string tenNhap, string mkNhap)
        {
            try
            {
                string csdl = _csdl1Path;
                if (string.IsNullOrEmpty(csdl) || !File.Exists(csdl))
                {
                    MessageBox.Show("⚠️ Không tìm thấy tệp cơ sở dữ liệu. Vui lòng kiểm tra cấu hình.", "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                using SqliteConnection conn = new SqliteConnection("Data Source=" + csdl);
                conn.Open();

                string sql = "SELECT TenTaiKhoan, MatKhau FROM Admin WHERE ID = 1 LIMIT 1";
                using SqliteCommand cmd = new SqliteCommand(sql, conn);
                using SqliteDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    string tenCSDLRoi = reader.GetString(0).Trim();
                    string mkCSDLRoi = reader.GetString(1).Trim();

                    // ⭐ SỬA LỖI LOGIC: Phải GIẢI MÃ CSDL rồi mới đem so với thông tin người dùng gõ
                    // Dùng kèm Fallback nếu CSDL cũ chưa mã hóa.
                    string tenThucTe = BaoMatAES.GiaiMa(tenCSDLRoi);
                    if (string.IsNullOrWhiteSpace(tenThucTe)) tenThucTe = tenCSDLRoi;

                    string mkThucTe = BaoMatAES.GiaiMa(mkCSDLRoi);
                    if (string.IsNullOrWhiteSpace(mkThucTe)) mkThucTe = mkCSDLRoi;

                    // So sánh plain-text (Bỏ qua hoa thường cho Tài khoản, phân biệt hoa thường cho Mật khẩu)
                    return string.Equals(tenThucTe, tenNhap, StringComparison.OrdinalIgnoreCase) &&
                           mkThucTe == mkNhap;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"⚠️ Lỗi kết nối CSDL: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return false;
        }
        // --- BẮT ĐẦU KHU VỰC GIA CỐ ASYNC/AWAIT ---
        private async Task<string> VanLyTruongThanh_DuongDanMoPhanMem_CongCuQuanLyCSDLAsync()
        {
            string baseDir = AppContext.BaseDirectory;
            string dbDir = Module_DanduongGPS.ThuMucCoSoDuLieu;
            string folderName = "CongCuQuanLyCSDL";
            string exeName = "DB Browser for SQLite.exe";

            string motherDir = Path.Combine(baseDir, "Database Backup");
            string dbFolder = Path.Combine(dbDir, folderName);
            string motherFolder = Path.Combine(motherDir, folderName);
            string exePath = Path.Combine(dbFolder, exeName);

            try
            {
                // Nếu chưa có trong thư mục đích, chờ thực hiện copy từ thư mục mẹ (Chạy nền)
                if (!Directory.Exists(dbFolder) && Directory.Exists(motherFolder))
                {
                    await MuaThuOHoKaido_CopyDirectorySmartAsync(motherFolder, dbFolder);
                }

                if (File.Exists(exePath)) return exePath;

                string motherExe = Path.Combine(motherFolder, exeName);
                if (File.Exists(motherExe)) return motherExe;

                string fallback1 = Path.Combine(Application.StartupPath, folderName, exeName);
                if (File.Exists(fallback1)) return fallback1;

                string fallback2 = Path.Combine(Application.StartupPath, exeName);
                if (File.Exists(fallback2)) return fallback2;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Vạn Lý Trường Thành] Lỗi truy xuất: {ex.Message}");
            }

            return string.Empty;
        }
        // Bọc tác vụ IO nặng vào Task.Run để giải phóng giao diện
        private async Task MuaThuOHoKaido_CopyDirectorySmartAsync(string srcDirPath, string dstDirPath)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(srcDirPath)) return;
                    Directory.CreateDirectory(dstDirPath);

                    foreach (string srcFile in Directory.GetFiles(srcDirPath))
                    {
                        string dstFile = Path.Combine(dstDirPath, Path.GetFileName(srcFile));
                        if (!File.Exists(dstFile) || File.GetLastWriteTime(srcFile) > File.GetLastWriteTime(dstFile))
                            File.Copy(srcFile, dstFile, true);
                    }

                    foreach (string folder in Directory.GetDirectories(srcDirPath))
                    {
                        string dstFolder = Path.Combine(dstDirPath, Path.GetFileName(folder));
                        // Đệ quy vẫn chạy ngon lành bên trong Task
                        MuaThuOHoKaido_CopyDirectorySmartAsync(folder, dstFolder).Wait();
                    }
                }
                catch (Exception ex)
                {
                    // Ghi log để sau này dễ truy vết nếu copy thất bại do quyền truy cập
                    Debug.WriteLine($"[Mùa Thu Hokkaido] Lỗi chép file: {ex.Message}");
                }
            });
        }
        // Hàm này được thăng cấp lên async
        private async void GoCuaTraiTim_MoChucNangSQLite()
        {
            try
            {
                this.Hide();

                // Đợi quá trình tìm/chép file hoàn tất
                string pathApp = await VanLyTruongThanh_DuongDanMoPhanMem_CongCuQuanLyCSDLAsync();

                if (string.IsNullOrEmpty(pathApp))
                {
                    MessageBox.Show(
                        "❌ Không tìm thấy chương trình 'DB Browser for SQLite.exe'.\n\n" +
                        "Hãy kiểm tra thư mục:\n" +
                        $"- {Module_DanduongGPS.ThuMucCoSoDuLieu}\n" +
                        $"- %LocalAppData%\\PhanMemThiDua2026\\CongCuQuanLyCSDL\n" +
                        $"- {Application.StartupPath}\\CongCuQuanLyCSDL\n\n" +
                        "Hoặc đặt 'DB Browser for SQLite.exe' vào một trong các thư mục trên.",
                        "Thiếu file", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    this.Show();
                    return;
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = pathApp,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Lỗi khi mở phần mềm: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                try { this.Show(); } catch { }
            }
        }
        // --- KẾT THÚC KHU VỰC GIA CỐ ---
        private void CapNhatTrangThaiHienMatKhau()
        {
            bool isChecked = Chex_HienMatKhau.Checked;
            Text_Password.UseSystemPasswordChar = !isChecked;
            Chex_HienMatKhau.ForeColor = isChecked ? Color.Green : Color.Red;
        }
        // Thay đổi thành async để đợi Gõ Cửa Trái Tim
        private void btn_DangNhap_Click(object sender, EventArgs e)
        {
            string ten = Text_Admin.Text.Trim();
            string mk = Text_Password.Text.Trim();
            if (string.IsNullOrEmpty(ten) || string.IsNullOrEmpty(mk))
            {
                MessageBox.Show("⚠️ Vui lòng nhập đầy đủ tên và mật khẩu.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // GIA CỐ UX: Khóa nút đăng nhập để tránh người dùng click đúp (spam click)
            btn_DangNhap.Enabled = false;
            if (KiemTraTaiKhoanTrongCSDL(ten, mk))
            {
                Module_TaiKhoan.TenTaiKhoan_RAM = ten;
                SessionInfo.TenTaiKhoan = ten;
                SessionInfo.ThoiGianDangNhap = DateTime.Now;

                Module_NhatKy.GhiNhatKy(
                    taiKhoan: ten,
                    hanhDong: "Truy cập phần mềm quản lý cơ sở dữ liệu",
                    ghiChu: $"Thời gian: {SessionInfo.ThoiGianDangNhap:dd-MM-yyyy HH:mm:ss}"
                );

                // Gõ cửa trái tim giờ sẽ chạy bất đồng bộ
                GoCuaTraiTim_MoChucNangSQLite();
            }
            else
            {
                LamLoMotCuocTinh_ThongBaoSaiMatKhau();
                Module_NhatKy.GhiNhatKy(
                    taiKhoan: ten,
                    hanhDong: "Truy cập phần mềm quản lý cơ sở dữ liệu",
                    ghiChu: "Thất bại!"
                );
            }

            // Mở khóa lại nút sau khi xử lý xong
            btn_DangNhap.Enabled = true;
        }
        private void LamLoMotCuocTinh_ThongBaoSaiMatKhau()
        {
            soLanSai++;
            Text_Password.Clear();
            Text_Password.Focus();

            if (soLanSai >= MaxSai)
            {
                MessageBox.Show("Bạn đã nhập sai quá 5 lần. Ứng dụng sẽ đóng.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Application.Exit();
            }
            else
            {
                MessageBox.Show($"❌ Sai tên hoặc mật khẩu! Lần {soLanSai}/{MaxSai}", "Đăng nhập thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btn_Thoat_Click(object sender, EventArgs e)
        {
            Module_NhatKy.GhiNhatKy(
                taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                hanhDong: "Truy cập phần mềm quản lý cơ sở dữ liệu",
                ghiChu: "Hủy tiến trình!"
            );
            Close();
        }
        private void Chex_HienMatKhau_CheckedChanged(object sender, EventArgs e)
        {
            CapNhatTrangThaiHienMatKhau();
        }
    }

    public static class SessionInfo
    {
        public static string TenTaiKhoan { get; set; } = "";
        public static DateTime ThoiGianDangNhap { get; set; } = DateTime.MinValue;
    }
}