///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using Krypton.Toolkit;                    // Thêm để dùng KryptonTextBox và các tính năng viền
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form9_GoCaiDat : Form
    {
        private readonly string _csdl1Path = Module_DanduongGPS.DuongDanCSDL1;
        // Pooling=False: đóng là nhả hẳn tệp .db; Mode=ReadWrite (không Create): thiếu tệp thì báo lỗi, không tự tạo tệp rỗng
        private string ConnectionString => Module_KetNoiCSDL.ChuoiKetNoi(_csdl1Path, khongTaoMoi: true, pooling: false);
        private int _soLanSai = 0;
        // 🌟 KHAI BÁO CẤU HÌNH VIỀN UI/UX (CHUẨN WINDOWS 11)       
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
        private async void Form9_Load(object? sender, EventArgs e)
        {
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            DoubleBuffered = true; // Chống nháy hình
            AcceptButton = Btn_GoCaiDat;
            Chex_HienMatKhau.Checked = false;
            CapNhatTrangThaiHienMatKhau();
            InitFocusEffects();
            InitToolTips();
            Text_Password.Focus();
            // 🌟 CHẠY NGẦM HÀM LẤY TÊN ADMIN ĐỂ KHÔNG BLOCK UI
            await GoiTenEmTrongDem_GoCaiDat();
        }
        // Lê Trung Kiên -  Yêu mèo cam
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
        { Btn_GoCaiDat, "Xác thực mật khẩu và tiến hành gỡ cài đặt phần mềm" },
        { kryptonButton_HuongDanToi, "Xem hướng dẫn sử dụng chức năng gỡ cài đặt" },
        { Btn_Thoat, "Đóng cửa sổ này, không gỡ cài đặt" }
    };
            foreach (var tip in tips)
            {
                if (tip.Key != null && !tip.Key.IsDisposed)
                    toolTip1.SetToolTip(tip.Key, tip.Value);
            }
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
        private void Ktb_EnterFocus(object? sender, EventArgs e)
        {
            if (sender is KryptonTextBox ktb)
            {
                ktb.StateCommon.Border.Color1 = FocusBorderColor;
                ktb.StateCommon.Border.Width = FocusBorderWidth;
                ktb.Refresh();
            }
        }
        private void Ktb_LeaveFocus(object? sender, EventArgs e)
        {
            if (sender is KryptonTextBox ktb)
            {
                ktb.StateCommon.Border.Color1 = NormalBorderColor;
                ktb.StateCommon.Border.Width = NormalBorderWidth;
                ktb.Refresh();
            }
        }
        /// /🌟 CHUYỂN SANG BẤT ĐỒNG BỘ ĐỂ TRÁNH GIẬT/LAG LÚC MỞ FORM
        private async Task GoiTenEmTrongDem_GoCaiDat()
        {
            try
            {
                if (!File.Exists(_csdl1Path))
                    return;
                using var conn = new SqliteConnection(ConnectionString);
                await conn.OpenAsync();
                const string sqlAdmin = "SELECT TenTaiKhoan FROM Admin WHERE ID = 1 LIMIT 1";
                using var cmd = new SqliteCommand(sqlAdmin, conn);
                var result = await cmd.ExecuteScalarAsync();
                if (result == null)
                    return;
                string encrypted = result.ToString() ?? "";
                string decrypted = Module_BaoMatAES.GiaiMa(encrypted);
                string adminName = string.IsNullOrWhiteSpace(decrypted) ? encrypted : decrypted;
                if (!string.IsNullOrEmpty(adminName))
                    Text_Admin.Text = adminName;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể đọc tài khoản Admin:\n" + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private async Task<bool> KiemTraMatKhauAdminAsync(string matKhauNhap)
        {
            if (string.IsNullOrWhiteSpace(matKhauNhap)) return false;
            try
            {
                return await Task.Run(async () =>
                {
                    using var conn = new SqliteConnection(ConnectionString);
                    await conn.OpenAsync();
                    const string sqlPass = "SELECT MatKhau FROM Admin WHERE ID = 1 LIMIT 1";
                    using var cmd = new SqliteCommand(sqlPass, conn);
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null)
                    {
                        string mkTrongCSDL = result.ToString() ?? "";
                        string mkGiaiMa = Module_BaoMatAES.GiaiMa(mkTrongCSDL);
                        if (string.IsNullOrWhiteSpace(mkGiaiMa))
                            mkGiaiMa = mkTrongCSDL;
                        return SoSanhAnToan(matKhauNhap, mkGiaiMa);
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
        // So sánh băm SHA-256 của hai chuỗi bằng FixedTimeEquals: thời gian không phụ thuộc nội dung và KHÔNG lộ độ dài mật khẩu
        // (bản cũ return sớm khi khác độ dài).
        private static bool SoSanhAnToan(string a, string b)
        {
            byte[] ha = SHA256.HashData(Encoding.UTF8.GetBytes(a));
            byte[] hb = SHA256.HashData(Encoding.UTF8.GetBytes(b));
            try { return CryptographicOperations.FixedTimeEquals(ha, hb); }
            finally { CryptographicOperations.ZeroMemory(ha); CryptographicOperations.ZeroMemory(hb); }
        }
        // ⭐ BƯỚC ĐỌC DB, GỠ BẪY VÀ GIẢI MÃ V2 (CÓ CHỐNG SPAM)
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
                        MessageBox.Show(
                            "Bạn đã nhập sai 3 lần. Ứng dụng sẽ tự động đóng để bảo vệ an toàn!",
                            "Khóa bảo mật",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        Module_ThoatAnToan.ThoatDutKhoat();
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
                // ---- ĐÚNG MẬT KHẨU → HỎI GIỮ/XÓA DỮ LIỆU → BÀN GIAO CHO TRÌNH GỠ CÀI ĐẶT ----
                bool? giuDuLieu = HoiGiuDuLieu();
                if (giuDuLieu == null) return;   // người dùng hủy
                string taiKhoan = string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM)
                    ? "System (Quyền cao nhất)" : Module_TaiKhoan.TenTaiKhoan_RAM;
                // Ghi nhật ký TRƯỚC khi bàn giao (nếu chọn xóa dữ liệu thì CSDL nhật ký cũng bị xóa)
                Module_NhatKy.GhiNhatKy(taiKhoan, "Gỡ cài đặt phần mềm",
                    giuDuLieu.Value ? "Admin xác thực: gỡ chương trình, GIỮ lại dữ liệu." : "Admin xác thực: gỡ chương trình và XÓA toàn bộ dữ liệu.");
                if (!Module_GoCaiDat.KhoiChay(giuDuLieu.Value, out string loiKhoiChay))
                {
                    MessageBox.Show("Không thể bắt đầu gỡ cài đặt:\n" + loiKhoiChay, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Module_NhatKy.GhiNhatKy("System (Quyền cao nhất)", "Gỡ cài đặt phần mềm", "Hủy tiến trình gỡ cài đặt do lỗi khởi chạy!");
                    return;
                }
                // Trình gỡ cài đặt chờ phần mềm đóng (không ép tắt): nhả khóa tệp CSDL rồi thoát.
                GiaiPhongTruocKhiThoat();
                Module_ThoatAnToan.ThoatDutKhoat();   // trình gỡ cài đặt chờ phần mềm đóng: Application.Exit có thể lỗi nên không bao giờ đóng
            }
            finally
            {
                // 🌟 MỞ KHÓA LẠI NÚT NẾU CHƯA THOÁT ỨNG DỤNG
                Btn_GoCaiDat.Enabled = true;
            }
        }
        /// <summary>Hỏi giữ hay xóa dữ liệu. true = giữ, false = xóa hết, null = hủy gỡ cài đặt.</summary>
        private bool? HoiGiuDuLieu()
        {
            DialogResult r = MessageBox.Show(this,
                "Bạn có muốn GIỮ LẠI dữ liệu (cơ sở dữ liệu và các bản sao lưu) sau khi gỡ cài đặt không?\n\n" +
                "• Có: chỉ gỡ chương trình, giữ lại thư mục Database và CSDL_Backups.\n" +
                "• Không: xóa TOÀN BỘ, kể cả dữ liệu (không thể khôi phục).\n" +
                "• Hủy: không gỡ cài đặt.",
                "Gỡ cài đặt - dữ liệu",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (r == DialogResult.Cancel) return null;
            if (r == DialogResult.Yes) return true;
            DialogResult x = MessageBox.Show(this,
                "XÁC NHẬN LẦN CUỐI: toàn bộ dữ liệu sẽ bị xóa vĩnh viễn và KHÔNG thể khôi phục.\n\nTiếp tục xóa?",
                "Xóa vĩnh viễn dữ liệu",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            return x == DialogResult.Yes ? false : (bool?)null;
        }
        /// <summary>Nhả tài nguyên giữ tệp/khóa trước khi thoát để trình gỡ cài đặt xóa được ngay.</summary>
        private static void GiaiPhongTruocKhiThoat()
        {
            try { SqliteConnection.ClearAllPools(); } catch { }                       // đóng các kết nối SQLite còn nằm trong pool
            try { Module_BaoMatAES.DonDeprRacTraLaiTaiNguyenHeThong(); } catch { }    // xóa khóa/bộ đệm giải mã khỏi RAM
        }
        private void CapNhatTrangThaiHienMatKhau()
        {
            bool isChecked = Chex_HienMatKhau.Checked;
            Text_Password.UseSystemPasswordChar = !isChecked;
            Chex_HienMatKhau.ForeColor = isChecked ? Color.Green : Color.Red;
        }
        private void Btn_Thoat_Click(object? sender, EventArgs e)
        {
            Close();
        }
        private void Chex_HienMatKhau_CheckedChanged(object? sender, EventArgs e)
        {
            CapNhatTrangThaiHienMatKhau();
        }
        private void kryptonButton_HuongDanToi_Click(object sender, EventArgs e)
        {
            string noiDungHuongDan =
                "📌 HƯỚNG DẪN SỬ DỤNG CHỨC NĂNG GỠ CÀI ĐẶT\n\n" +
                "1️. Tài khoản Admin:\n" +
                "    - Ô này hiển thị tên tài khoản Admin hiện tại (tự động tải từ cơ sở dữ liệu).\n\n" +
                "2️. Mật khẩu:\n" +
                "    - Nhập đúng mật khẩu Admin để xác thực quyền gỡ cài đặt.\n" +
                "    - Tick vào ô \"Hiện mật khẩu\" nếu muốn xem lại mật khẩu vừa nhập.\n\n" +
                "3️. Nút \"Gỡ cài đặt\":\n" +
                "    - Sau khi xác thực mật khẩu thành công, phần mềm hỏi bạn muốn GIỮ LẠI hay XÓA dữ liệu " +
                "(thư mục Database và CSDL_Backups), rồi khởi chạy trình gỡ cài đặt " +
                "(Uninstall_PhanMemThiDua2026.exe) và tự động thoát ứng dụng chính.\n" +
                "    - Nếu còn ServiceBackup / ServiceRestore đang mở, trình gỡ cài đặt sẽ yêu cầu đóng chúng (không ép tắt).\n\n" +
                "4️. Nút \"Thoát\":\n" +
                "    - Đóng cửa sổ này và quay lại phần mềm mà không gỡ cài đặt.\n\n" +
                "⚠️ Lưu ý bảo mật:\n" +
                "    - Nếu nhập sai mật khẩu 3 lần liên tiếp, ứng dụng sẽ tự động đóng để bảo vệ an toàn.\n" +
                "    - Chỉ những người có quyền Admin mới nên thực hiện thao tác gỡ cài đặt.";
            MessageBox.Show(
                noiDungHuongDan,
                "Hướng dẫn sử dụng",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
