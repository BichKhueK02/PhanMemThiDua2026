
using Krypton.Toolkit;                    // Thêm để dùng KryptonTextBox và các tính năng viền
using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace PhanMemThiDua2026
{
    public partial class Form9_GoCaiDat : Form
    {
        private readonly string _csdl1Path = Module_DanduongGPS.DuongDanCSDL1;
        private string ConnectionString => $"Data Source={_csdl1Path}";
        private int _soLanSai = 0;

        // =========================================================================
        // 🌟 KHAI BÁO CẤU HÌNH VIỀN UI/UX (CHUẨN WINDOWS 11)
        // =========================================================================
        private static readonly Color FocusBorderColor = Color.FromArgb(0, 120, 215);
        private static readonly Color NormalBorderColor = Color.Silver;
        private const int FocusBorderWidth = 2;
        private const int NormalBorderWidth = 1;
        public Form9_GoCaiDat()
        {
            InitializeComponent();
            this.Load += Form9_Load;
        }

        // ĐỔI THÀNH async void ĐỂ TẢI DỮ LIỆU NGẦM, CHỐNG GIẬT FORM
        private async void Form9_Load(object sender, EventArgs e)
        {
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            DoubleBuffered = true; // Chống nháy hình
            AcceptButton = Btn_GoCaiDat;

            Chex_HienMatKhau.Checked = false;
            CapNhatTrangThaiHienMatKhau();

            Btn_GoCaiDat.Height = Text_Admin.Height + 12;
            Btn_Thoat.Height = Text_Admin.Height + 12;

            // 🌟 KÍCH HOẠT TÍNH NĂNG TÔ MÀU VIỀN TỰ ĐỘNG
            InitFocusEffects();

            Text_Password.Focus();

            // 🌟 CHẠY NGẦM HÀM LẤY TÊN ADMIN ĐỂ KHÔNG BLOCK UI
            await ToQuoc_GoiTenAnhGiuaTroiThuongNhoAsync();
        }

        // 🌟 HÀM TÔ MÀU VIỀN CHUẨN KỸ SƯ (CHỐNG MEMORY LEAK)
        private void InitFocusEffects()
        {
            var controls = new List<KryptonTextBox> { Text_Admin, Text_Password };
            foreach (var ktb in controls)
            {
                if (ktb == null) continue;

                ktb.StateCommon.Border.DrawBorders = PaletteDrawBorders.All;
                ktb.StateCommon.Border.Color1 = NormalBorderColor;
                ktb.StateCommon.Border.Width = NormalBorderWidth;

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
                ktb.Refresh();
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
        // 🌟 CHUYỂN SANG BẤT ĐỒNG BỘ ĐỂ TRÁNH GIẬT/LAG LÚC MỞ FORM
        private async Task ToQuoc_GoiTenAnhGiuaTroiThuongNhoAsync()
        {
            try
            {
                if (!File.Exists(_csdl1Path)) return;

                // Offload xử lý AES và DB sang ThreadPool
                string adminName = await Task.Run(async () =>
                {
                    using var conn = new SqliteConnection(ConnectionString);
                    await conn.OpenAsync();
                    string sqlAdmin = BaoMatAES.TraLaiTenChoMeoCam("1 TIMIL 1 = DI EREHW nimdA MORF naohKiaTneT TCELES");
                    using var cmd = new SqliteCommand(sqlAdmin, conn);
                    var result = await cmd.ExecuteScalarAsync();

                    if (result != null)
                    {
                        string encrypted = result.ToString() ?? "";
                        string decrypted = BaoMatAES.GiaiMa(encrypted);
                        return string.IsNullOrWhiteSpace(decrypted) ? encrypted : decrypted;
                    }
                    return string.Empty;
                });

                if (!string.IsNullOrEmpty(adminName))
                {
                    Text_Admin.Text = adminName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể đọc tài khoản Admin:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // 🌟 KIỂM TRA MẬT KHẨU BẤT ĐỒNG BỘ
        private async Task<bool> KiemTraMatKhauAdminAsync(string matKhauNhap)
        {
            if (string.IsNullOrWhiteSpace(matKhauNhap)) return false;

            try
            {
                return await Task.Run(async () =>
                {
                    using var conn = new SqliteConnection(ConnectionString);
                    await conn.OpenAsync();
                    string sqlPass = BaoMatAES.TraLaiTenChoMeoCam("1 TIMIL 1 = DI EREHW nimdA MORF uahKtaM TCELES");
                    using var cmd = new SqliteCommand(sqlPass, conn);
                    var result = await cmd.ExecuteScalarAsync();

                    if (result != null)
                    {
                        string mkTrongCSDL = result.ToString() ?? "";
                        string mkGiaiMa = BaoMatAES.GiaiMa(mkTrongCSDL);

                        if (string.IsNullOrWhiteSpace(mkGiaiMa))
                            mkGiaiMa = mkTrongCSDL;

                        return SlowEquals(matKhauNhap, mkGiaiMa);
                    }
                    return false;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi xác thực mật khẩu: {ex.Message}");
            }
            return false;
        }
        private bool SlowEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];

            return diff == 0;
        }
        // =======================================================
        // ⭐ BƯỚC ĐỌC DB, GỠ BẪY VÀ GIẢI MÃ V2 (CÓ CHỐNG SPAM)
        // =======================================================
        private async void Btn_GoCaiDat_Click(object sender, EventArgs e)
        {
            // 🌟 KHÓA NÚT BẤM CHỐNG SPAM CLICK
            Btn_GoCaiDat.Enabled = false;

            try
            {
                // ---- KIỂM TRA MẬT KHẨU VÀ CHỐNG SPAM ----
                bool isCorrect = await KiemTraMatKhauAdminAsync(Text_Password.Text.Trim());

                if (!isCorrect)
                {
                    _soLanSai++;

                    if (_soLanSai >= 3)
                    {
                        MessageBox.Show("Bạn đã nhập sai quá 3 lần. Ứng dụng sẽ tự động đóng để bảo vệ an toàn!", "Khóa bảo mật", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Application.Exit();
                        return;
                    }

                    MessageBox.Show($"Mật khẩu không đúng!\nBạn đã nhập sai {_soLanSai}/3 lần.", "Từ chối truy cập", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    // 🌟 DÙNG TASK.DELAY THAY THẾ THREAD.SLEEP ĐỂ KHÔNG LÀM ĐƠ FORM
                    await Task.Delay(500 * _soLanSai);

                    Text_Password.Clear();
                    Text_Password.Focus();
                    return;
                }

                // Nếu nhập đúng thì reset số lần sai về 0
                _soLanSai = 0;

                // ---- ĐÚNG MẬT KHẨU → TẠO KHÓA & TIẾP TỤC ----
                if (!BaoMatAES.DuongVaoTraiTimEm())
                {
                    return;
                }

                try
                {
                    string uninstallExe = Path.Combine(AppContext.BaseDirectory, "Uninstall_PhanMemThiDua2026.exe");
                    if (!File.Exists(uninstallExe))
                    {
                        MessageBox.Show("Không tìm thấy file gỡ cài đặt!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = uninstallExe,
                        UseShellExecute = true,
                        WorkingDirectory = AppContext.BaseDirectory
                    });

                    Application.Exit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi gỡ cài đặt:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Module_NhatKy.GhiNhatKy("System (Quyền cao nhất)", "Gỡ cài đặt phần mềm", "Hủy tiến trình gỡ cài đặt do lỗi khởi chạy!");
                }
            }
            finally
            {
                // 🌟 MỞ KHÓA LẠI NÚT NẾU CHƯA THOÁT ỨNG DỤNG
                Btn_GoCaiDat.Enabled = true;
            }
        }
        private void CapNhatTrangThaiHienMatKhau()
        {
            bool isChecked = Chex_HienMatKhau.Checked;
            Text_Password.UseSystemPasswordChar = !isChecked;
            Chex_HienMatKhau.ForeColor = isChecked ? Color.Green : Color.Red;
        }
        private void Btn_Thoat_Click(object sender, EventArgs e)
        {
            Close();
        }
        private void Chex_HienMatKhau_CheckedChanged(object sender, EventArgs e)
        {
            CapNhatTrangThaiHienMatKhau();
        }

  
        // CỤM HÀM TƯƠNG THÍCH NGƯỢC VỚI UNINSTALL.EXE (CHUẨN V1)
    }
}
