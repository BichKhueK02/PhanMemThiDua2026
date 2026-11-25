///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
using Krypton.Toolkit;
using System.Diagnostics;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form8_CauHinhCSDL : Form
    {
        private const int MaxSai = 5;
        private const string HANH_DONG_NHAT_KY = "Truy cập phần mềm quản lý cơ sở dữ liệu";
        private const string CHU_NUT_XAC_THUC = "Xác thực";
        private const string CHU_NUT_DANG_XU_LY = "Đang xử lý...";
        // static: đếm theo tiến trình. Đóng rồi mở lại form không làm đếm lại từ 0 (bản cũ đếm theo từng form => thử vô hạn lần)
        private static int _soLanSai;
        private bool _dangXuLy;
        // Viền ô nhập (Windows 11): chàm khi có focus, xám đậm bình thường
        private static readonly Color FocusBorderColor = Color.FromArgb(99, 102, 241);
        private static readonly Color NormalBorderColor = Color.FromArgb(51, 65, 85);
        private const int FocusBorderWidth = 2;
        private const int NormalBorderWidth = 1;
        public Form8_CauHinhCSDL()
        {
            InitializeComponent();
            Module_BieuTuongNut.ApDung(this);   // biểu tượng nút: Hiện đại = tự vẽ, Cổ điển = ảnh gốc nhúng trong exe
            NapLogo();
            DoubleBuffered = true;
            AcceptButton = btn_DangNhap;     // Enter = Xác thực
            CancelButton = btn_Thoat;        // Esc   = Hủy
            InitToolTips();
            InitFocusEffects();
            CapNhatTrangThaiHienMatKhau();   // che mật khẩu ngay từ đầu, không đợi sự kiện Load
            Module_FormKhongVien.ApDung(this, this, cardPanel, pnlHeader, picLogo, lblTitle, lblSubtitle);
        }
        // Tải tên Admin ở luồng nền: bản cũ đọc SQLite + giải mã AES ngay trên luồng giao diện lúc mở form
        private async void Form8_Load(object? sender, EventArgs e)
        {
            try
            {
                ActiveControl = Text_Password;   // Focus() trước khi form hiện là vô tác dụng; ActiveControl thì có
                string? ten = await Task.Run(Module_XacThucAdmin.DocTenAdmin);
                if (!IsDisposed && !string.IsNullOrEmpty(ten) && string.IsNullOrEmpty(Text_Admin.Text))
                    Text_Admin.Text = ten;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Form8] Lỗi nạp tên tài khoản: {ex.Message}");
            }
        }
        // ===================== GIAO DIỆN =====================
        private void InitToolTips()
        {
            // Dùng toolTip1 (thuộc 'components') nên được giải phóng cùng form. Bản cũ tạo ToolTip cục bộ rồi bỏ => rò handle.
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Cấu hình cơ sở dữ liệu";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            toolTip1.InitialDelay = 200;
            toolTip1.AutoPopDelay = 1500;
            toolTip1.ReshowDelay = 100;
            toolTip1.ShowAlways = true;
            toolTip1.SetToolTip(Chex_HienMatKhau, "Hiển thị/ẩn mật khẩu đăng nhập");
            toolTip1.SetToolTip(btn_DangNhap, "Xác thực thông tin và truy cập cấu hình CSDL");
            toolTip1.SetToolTip(btn_Thoat, "Thoát khỏi màn hình cấu hình cơ sở dữ liệu");
        }
        private void InitFocusEffects()
        {
            foreach (KryptonTextBox ktb in new[] { Text_Admin, Text_Password })
            {
                ktb.StateCommon.Border.DrawBorders = PaletteDrawBorders.All;
                DatVien(ktb, NormalBorderColor, NormalBorderWidth);
                ktb.Enter += Ktb_EnterFocus;
                ktb.Leave += Ktb_LeaveFocus;
            }
        }
        // Không gọi Refresh(): Krypton tự vẽ lại khi đổi palette; Refresh ép vẽ đồng bộ mỗi lần chuyển ô
        private void Ktb_EnterFocus(object? sender, EventArgs e)
        {
            if (sender is KryptonTextBox ktb) DatVien(ktb, FocusBorderColor, FocusBorderWidth);
        }
        private void Ktb_LeaveFocus(object? sender, EventArgs e)
        {
            if (sender is KryptonTextBox ktb) DatVien(ktb, NormalBorderColor, NormalBorderWidth);
        }
        private static void DatVien(KryptonTextBox ktb, Color mau, int doDay)
        {
            ktb.StateCommon.Border.Color1 = mau;
            ktb.StateCommon.Border.Color2 = mau;
            ktb.StateCommon.Border.Width = doDay;
        }
        // Một cơ chế duy nhất cho che/hiện mật khẩu (bản cũ đặt song song PasswordChar và UseSystemPasswordChar)
        private void CapNhatTrangThaiHienMatKhau()
        {
            bool hien = Chex_HienMatKhau.Checked;
            Text_Password.UseSystemPasswordChar = false;
            Text_Password.PasswordChar = hien ? '\0' : '●';
            Chex_HienMatKhau.ForeColor = hien ? Color.FromArgb(74, 222, 128) : Color.FromArgb(148, 163, 184);
        }
        // Trạng thái "đang bận": khóa nút Xác thực (chống bấm đúp / Enter liên tục), đổi chữ nút cho người dùng biết
        private void DatTrangThaiBan(bool ban)
        {
            _dangXuLy = ban;
            btn_DangNhap.Enabled = !ban;
            btn_DangNhap.Values.Text = ban ? CHU_NUT_DANG_XU_LY : CHU_NUT_XAC_THUC;
        }
        // ===================== XÁC THỰC =====================
        private async void btn_DangNhap_Click(object? sender, EventArgs e)
        {
            if (_dangXuLy) return;
            string ten = Text_Admin.Text.Trim();
            string mk = Text_Password.Text.Trim();
            if (ten.Length == 0 || mk.Length == 0)
            {
                MessageBox.Show(this, "⚠️ Vui lòng nhập đầy đủ tên và mật khẩu.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DatTrangThaiBan(true);
            bool dongUngDung = false;
            try
            {
                // SQLite + AES chạy ở luồng nền: giao diện không bị khựng khi bấm Xác thực
                var (kq, tenChuan) = await Task.Run(() => Module_XacThucAdmin.XacThuc(ten, mk));
                if (IsDisposed) return;
                switch (kq)
                {
                    case Module_XacThucAdmin.KetQua.ThanhCong:
                        _soLanSai = 0;
                        // Gán tên CHUẨN trong CSDL (bản cũ gán chuỗi người dùng gõ: "ADMIN" thay cho "admin")
                        Module_TaiKhoan.TenTaiKhoan_RAM = tenChuan;
                        SessionInfo.TenTaiKhoan = tenChuan;
                        // Không ghi đè giờ đăng nhập phần mềm nếu nơi khác đã đặt (nhật ký bảo trì lấy giá trị này)
                        if (SessionInfo.ThoiGianDangNhap == DateTime.MinValue)
                            SessionInfo.ThoiGianDangNhap = DateTime.Now;
                        Module_NhatKy.GhiNhatKy(taiKhoan: tenChuan, hanhDong: HANH_DONG_NHAT_KY,
                            ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
                        await MoCongCuQuanLyCSDLAsync();
                        break;
                    case Module_XacThucAdmin.KetQua.SaiThongTin:
                        _soLanSai++;
                        // Ghi nhật ký TRƯỚC khi hiện hộp thoại / thoát ứng dụng để lần sai cuối chắc chắn được ghi
                        Module_NhatKy.GhiNhatKy(taiKhoan: ten, hanhDong: HANH_DONG_NHAT_KY, ghiChu: $"Thất bại! (lần {_soLanSai}/{MaxSai})");
                        Text_Password.Clear();
                        if (_soLanSai >= MaxSai)
                        {
                            dongUngDung = true;
                            MessageBox.Show(this, $"Bạn đã nhập sai quá {MaxSai} lần. Ứng dụng sẽ đóng.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            Module_ThoatAnToan.ThoatDutKhoat();
                            return;
                        }
                        await Task.Delay(250 * _soLanSai);   // càng sai nhiều càng chờ lâu (làm chậm dò mật khẩu)
                        if (IsDisposed) return;
                        MessageBox.Show(this, $"❌ Sai tên hoặc mật khẩu! Lần {_soLanSai}/{MaxSai}", "Đăng nhập thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Text_Password.Focus();
                        break;
                    default:
                        // Lỗi dữ liệu/hệ thống: không tính vào số lần nhập sai
                        MessageBox.Show(this, "⚠️ Không đọc được tài khoản quản trị từ cơ sở dữ liệu.\nVui lòng kiểm tra tệp CSDL rồi thử lại.",
                            "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Form8] Lỗi xác thực: {ex}");
                if (!IsDisposed)
                    MessageBox.Show(this, $"⚠️ Lỗi hệ thống: {ex.Message}", Module_HeThong.TieuDe_Loi, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Luôn mở khóa nút trừ khi form đã đóng / ứng dụng đang thoát (bản cũ không có finally: lỗi giữa chừng làm nút chết vĩnh viễn)
                if (!dongUngDung && !IsDisposed) DatTrangThaiBan(false);
            }
        }
        // ===================== MỞ CÔNG CỤ QUẢN LÝ CSDL =====================
        /// <summary>
        /// Chuẩn bị công cụ ở luồng nền (form vẫn hiện, nút báo "Đang xử lý..."), mở xong mới ẩn form,
        /// chờ người dùng đóng công cụ rồi đóng form. Bản cũ: Hide() ngay từ đầu (người dùng không thấy gì suốt lúc chép tệp),
        /// bỏ quên form ẩn mãi mãi (rò RAM; nếu mở bằng ShowDialog thì cửa sổ chủ bị khóa cứng) và cho bấm Xác thực lần nữa => mở 2 công cụ.
        /// </summary>
        private async Task MoCongCuQuanLyCSDLAsync()
        {
            string duongDan = await Task.Run(TimDuongDanCongCu);
            if (IsDisposed) return;
            if (duongDan.Length == 0)
            {
                MessageBox.Show(this,
                    $"❌ Không tìm thấy chương trình '{Module_DanduongGPS.TenFileDBBrowserSQLite}'.\n\n" +
                    "Đã tìm trong các thư mục:\n" +
                    $"- {Module_DanduongGPS.ThuMucCongCuQuanLyCSDL}\n" +
                    $"- {Module_DanduongGPS.ThuMucBackupCongCuQuanLyCSDL}\n" +
                    $"- {Path.Combine(AppContext.BaseDirectory, Module_DanduongGPS.TenThuMucCongCuQuanLyCSDL)}\n" +
                    $"- {AppContext.BaseDirectory}\n\n" +
                    "Hãy đặt tệp này vào một trong các thư mục trên.",
                    "Thiếu file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Process? tienTrinh;
            try
            {
                tienTrinh = Process.Start(new ProcessStartInfo
                {
                    FileName = duongDan,
                    WorkingDirectory = Path.GetDirectoryName(duongDan) ?? AppContext.BaseDirectory,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"❌ Lỗi khi mở phần mềm: {ex.Message}", Module_HeThong.TieuDe_Loi, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Hide();
            if (tienTrinh != null)
            {
                using (tienTrinh)
                {
                    try { await tienTrinh.WaitForExitAsync(); }
                    catch (Exception ex) { Debug.WriteLine($"[Form8] Chờ công cụ thoát: {ex.Message}"); }
                }
            }
            if (!IsDisposed) Close();
        }
        private static string TimDuongDanCongCu()
        {
            string thuMucCongCu = Module_DanduongGPS.ThuMucCongCuQuanLyCSDL;
            string thuMucMe = Module_DanduongGPS.ThuMucBackupCongCuQuanLyCSDL;
            string exe = Module_DanduongGPS.DuongDanDBBrowserSQLite;
            string tenExe = Module_DanduongGPS.TenFileDBBrowserSQLite;
            try
            {
                if (File.Exists(exe)) return exe;
                // Thiếu công cụ hoặc thư mục dở dang: chép bổ sung từ bản sao lưu (bản cũ chỉ chép khi cả thư mục chưa tồn tại)
                if (Directory.Exists(thuMucMe))
                {
                    SaoChepThuMucThongMinh(thuMucMe, thuMucCongCu);
                    if (File.Exists(exe)) return exe;
                }
                string[] khac =
                {
                    Path.Combine(thuMucMe, tenExe),
                    Path.Combine(AppContext.BaseDirectory, Module_DanduongGPS.TenThuMucCongCuQuanLyCSDL, tenExe),
                    Path.Combine(AppContext.BaseDirectory, tenExe)
                };
                foreach (string p in khac)
                    if (File.Exists(p)) return p;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Form8] Lỗi tìm công cụ quản lý CSDL: {ex.Message}");
            }
            return string.Empty;
        }
        /// <summary>
        /// Chép thư mục (đệ quy, chạy ĐỒNG BỘ trong 1 tác vụ nền). Khác bản cũ:
        ///  - mỗi tệp có try/catch riêng (bản cũ: 1 tệp lỗi bỏ dở cả thư mục);
        ///  - gỡ ReadOnly ở đích trước khi ghi đè (tệp trong "Database Backup" bị gắn ReadOnly, bản chép thừa hưởng nên lần cập nhật sau luôn lỗi);
        ///  - chép qua tệp .tmp rồi Move: bị ngắt giữa chừng không để lại exe/dll cụt mà File.Exists vẫn báo "có";
        ///  - không dùng Task.Run lồng nhau + .Wait() (nghẽn ThreadPool).
        /// </summary>
        private static void SaoChepThuMucThongMinh(string nguon, string dich)
        {
            Directory.CreateDirectory(dich);
            foreach (string f in Directory.EnumerateFiles(nguon))
            {
                if (f.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)) continue;
                string d = Path.Combine(dich, Path.GetFileName(f));
                string tam = d + ".tmp";
                try
                {
                    var fs = new FileInfo(f);
                    var fd = new FileInfo(d);
                    bool canChep = !fd.Exists
                                   || (fd.Length == 0 && fs.Length > 0)
                                   || fs.LastWriteTimeUtc > fd.LastWriteTimeUtc;
                    if (!canChep) continue;
                    File.Copy(f, tam, true);
                    File.SetAttributes(tam, FileAttributes.Normal);
                    if (fd.Exists) File.SetAttributes(d, FileAttributes.Normal);
                    File.Move(tam, d, true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Form8] Không chép được '{Path.GetFileName(f)}': {ex.Message}");
                }
                finally
                {
                    try { if (File.Exists(tam)) File.Delete(tam); } catch { }
                }
            }
            foreach (string sub in Directory.EnumerateDirectories(nguon))
            {
                try
                {
                    if ((File.GetAttributes(sub) & FileAttributes.ReparsePoint) != 0) continue;   // chống vòng lặp junction/symlink
                    SaoChepThuMucThongMinh(sub, Path.Combine(dich, Path.GetFileName(sub)));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Form8] Không chép được thư mục '{sub}': {ex.Message}");
                }
            }
        }
        // ===================== NÚT KHÁC =====================
        private void btn_Thoat_Click(object? sender, EventArgs e)
        {
            Module_NhatKy.GhiNhatKy(
                taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                hanhDong: HANH_DONG_NHAT_KY,
                ghiChu: "Hủy tiến trình!"
            );
            Close();
        }
        private void Chex_HienMatKhau_CheckedChanged(object? sender, EventArgs e)
        {
            CapNhatTrangThaiHienMatKhau();
        }
    }
    public static class SessionInfo
    {
        public static string TenTaiKhoan { get; set; } = "";
        private static DateTime _thoiGianDangNhap = DateTime.MinValue;
        public static DateTime ThoiGianDangNhap
        {
            get => _thoiGianDangNhap;
            set
            {
                bool doi = _thoiGianDangNhap != value;
                _thoiGianDangNhap = value;
                // Thanh trạng thái Form4 (dựng sẵn trước khi đăng nhập) ghi lại "Truy cập lúc ..." ngay
                if (doi)
                {
                    try { Module_TrangThaiHeThong.LamMoiTatCa(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[SessionInfo] " + ex.Message); }
                }
            }
        }
    }
}