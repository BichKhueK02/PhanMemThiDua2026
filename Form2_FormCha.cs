using Krypton.Toolkit;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static UIHelper;

namespace PhanMemThiDua2026
{
    public partial class Form2_FormCha : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private readonly string _csdl4Path = Module_DanduongGPS.DuongDanCSDL4;
        private Form? _currentChild;
        private DateTime _lastClick = DateTime.MinValue;
        private SemaphoreSlim _huongDanLock = new(1, 1);
        private System.Windows.Forms.Timer timerTuDongAnMenu;
        private KryptonButton prevButton, currentButton, nextButton;
        private int sidebarWidth;    // chiều rộng ban đầu, sẽ lấy lúc Form Load
        private int sidebarMinWidth = 0;
        private int _switching = 0;
        private bool sidebarExpanded = true;
        private bool isLoaded = false; // xác định Form đã hoàn tất Load
        private bool _loadExecuted = false;
        private bool _isClosing = false;
        private static bool _daMoWelcome = false;
        private static bool _daMoThongTinChungThu = false;
        private bool AllowSwitch()
        {
            var now = DateTime.Now;
            if ((now - _lastClick).TotalMilliseconds < 250)
                return false;
            _lastClick = now;
            return true;
        }
        [DllImport("user32.dll")] //API Windows để mở tệp hướng dẫn html
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        public Form2_FormCha()
        {
            InitializeComponent();
            // === THÊM ĐOẠN CODE NÀY ĐỂ HIỂN THỊ ICON TASKBAR ===
            // 🌟 ĐĂNG KÝ SỰ KIỆN: Cứ mỗi khi chữ của Label1 bị đổi, lập tức chạy hàm kiểm tra màu
            Label1.TextChanged += (s, e) => TuDongDoiMauLabelTieuDe();
            try
            {
                // Sử dụng lại icon ic_PhanMem có sẵn trong Resources của bạn
                using (var ms = new System.IO.MemoryStream(Properties.Resources.ic_PhanMem))
                {
                    this.Icon = new Icon(ms); // Gán icon cho Form 2
                }
            }
            catch (Exception)
            {
                // Bỏ qua nếu lỗi để không làm crash phần mềm
            }
            // Gọi hàm tô màu cho nút mặc định ngay khi khởi tạo
            // Thay 'btnTrangChu' bằng (Name) thực tế của nút trang chủ của bạn
            HighlightNavButton(kryptonButton1_Trangchu);
            this.WindowState = FormWindowState.Maximized;
            this.Text = "Phần mềm phân loại thi đua năm " + Module_HeThong.LayNamHeThong();
            this.KeyPreview = true; // bắt phím
            checkBox1_TuDongAnMenu.CheckedChanged -= checkBox1_TuDongAnMenu_CheckedChanged;
            checkBox1_TuDongAnMenu.CheckedChanged += checkBox1_TuDongAnMenu_CheckedChanged;
            Module_ThoatAnToan.KichHoatESC(this);
            try
            {
                kryptonButton1_HuongDan.Click -= kryptonButton1_HuongDan_Click;
                kryptonButton1_HuongDan.Click += kryptonButton1_HuongDan_Click;
            }
            catch
            {
            }
            this.Load += Form2_Load;
            this.FormClosing += Form2_FormClosing;
            EnableDoubleBuffering(PanelLeft);
            EnableDoubleBuffering(PanelContainer);
            PanelLeft.Dock = DockStyle.Left;
            PanelContainer.Dock = DockStyle.Fill;
            this.Controls.SetChildIndex(PanelContainer, 0);
            this.Controls.SetChildIndex(PanelLeft, 1);
            InitToolTips_GioiThieuVaMenu();
        }
        private async void Form2_Load(object sender, EventArgs e)
        {
            try
            {
                await Form2_LoadCore();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        private async Task Form2_LoadCore()
        {
            if (_loadExecuted) return;
            _loadExecuted = true;
            KhoiTaoDuLieuNguoiDung();
            KhoiTaoGiaoDien();
            checkBox1_TuDongAnMenu.Checked = AppRuntime.TuDongAnMenu;
            KhoiTaoTimerTuDongAnMenu();
            DangKySuKienTuDongAnMenu();
            EnsureSuKienThoatTonTai();
            // ⭐ THÊM VÀO ĐÂY: Thiết lập mũi tên ban đầu khi load Form
            CapNhatMuiTenGiaoDien();
            isLoaded = true;
            await Task.Run(() =>
            {
                try
                {
                    DataLoader.PreloadDanhSach(_csdl2Path);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            });
            await Task.Delay(300);
        }
        // 🌟 BƯỚC 1: Khai báo biến đệm (Cache) lưu trữ năm ở ngoài hàm
        private int _namHienTaiCache = -1;
        // 🌟 BƯỚC 2: Cập nhật lại hàm xử lý
        private void TuDongDoiMauLabelTieuDe()
        {
            if (Label1 == null) return;
            // TỐI ƯU HIỆU NĂNG: Chỉ lấy năm từ CSDL 1 lần duy nhất khi biến đệm chưa có dữ liệu (-1)
            if (_namHienTaiCache == -1)
            {
                _namHienTaiCache = Module_HeThong.LayNamHeThong();
            }
            // Khởi tạo chuỗi gốc làm mốc so sánh sử dụng biến đệm siêu tốc
            string chuoiTrangChu = $"PHẦN MỀM PHÂN LOẠI THI ĐUA \"VÌ ANTQ\" NĂM {_namHienTaiCache}";
            // TỐI ƯU GIAO DIỆN: So sánh chuỗi và chỉ gán màu khi màu thực sự bị lệch (Chống chớp nháy màn hình)
            if (Label1.Text.Trim().Equals(chuoiTrangChu, StringComparison.OrdinalIgnoreCase))
            {
                if (Label1.ForeColor != Color.Green)
                    Label1.ForeColor = Color.Green;
            }
            else
            {
                if (Label1.ForeColor != Color.Blue)
                    Label1.ForeColor = Color.Blue;
            }
        }
        public void KhoiTaoDuLieuNguoiDung()
        {
            Module_TaiKhoan.NapTaiKhoanTuCSDL();
            sidebarWidth = PanelLeft.Width;
            string ten = SessionInfo.TenTaiKhoan;
            DateTime thoiGian = SessionInfo.ThoiGianDangNhap;
            if (string.IsNullOrWhiteSpace(ten))
            {
                Label2.Text = "Xin chào!";
                return;
            }

            // Nếu thời gian không hợp lệ thì dùng DateTime.Now
            if (thoiGian == default)
                thoiGian = DateTime.Now;

            bool laNgayChan = (thoiGian.Day & 1) == 0; // nhanh hơn %

            Label2.Text = laNgayChan
                ? $"Đăng nhập bởi: {ten} vào lúc {thoiGian:HH:mm dd/MM/yyyy}"
                : $"Xin chào! {ten} bạn truy cập lúc {thoiGian:HH:mm dd/MM/yyyy}";
            // 🚀 KHỞI ĐỘNG HỆ THỐNG QUẢN LÝ HÌNH ẢNH TRANG CHỦ THEO CSDL
            KhoiTaoHeThongHinhNenAsync();
        }
        // BỘ NÃO QUẢN LÝ HÌNH ẢNH TRANG CHỦ (CÁ NHÂN HÓA UX THEO CSDL2)
        private System.Windows.Forms.Timer? _timerHinhNen;
        public async void KhoiTaoHeThongHinhNenAsync()
        {
            try
            {
                // 1. Đọc cấu hình từ CSDL (Chạy ngầm không block UI)
                string cheDo = await Task.Run(() => Module_HinhAnhTrangChu.DocCauHinhThoiGian());
                // 2. Nếu là Mặc định -> Tắt Timer, lấy hình cố định
                if (cheDo == "Mặc định")
                {
                    _timerHinhNen?.Stop();
                    Image? hinhMacDinh = await Task.Run(() => Module_HinhAnhTrangChu.LayHinhMacDinh());
                    CapNhatPictureBoxNgayLapTuc(hinhMacDinh);
                    return;
                }

                // 3. Nếu có đổi hình -> Xác định Timer
                int ms = 15000; // Khởi tạo gốc là 15 giây
                if (cheDo == "30 giây") ms = 30000;
                else if (cheDo == "1 phút") ms = 60000;

                // Khởi tạo Timer nếu chưa có
                if (_timerHinhNen == null)
                {
                    _timerHinhNen = new System.Windows.Forms.Timer();
                    _timerHinhNen.Tick += (s, e) => ThucThiDoiAnhNgam();
                }

                _timerHinhNen.Interval = ms;
                _timerHinhNen.Start();

                // Ép lấy ảnh 1 lần ngay lập tức để không bị trống màn hình lúc chờ nhịp Timer đầu
                ThucThiDoiAnhNgam();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khởi tạo hệ thống hình: {ex.Message}");
            }
        }
        // Tách việc xử lý nền
        private int _isChangingImage = 0;
        private async void ThucThiDoiAnhNgam()
        {
            if (Interlocked.Exchange(ref _isChangingImage, 1) == 1)
                return;

            try
            {
                var hinhMoi = await Task.Run(() => Module_HinhAnhTrangChu.LayHinhTiepTheo());
                CapNhatPictureBoxNgayLapTuc(hinhMoi);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                Interlocked.Exchange(ref _isChangingImage, 0);
            }
        }
        // Tách việc xử lý UI (Chống rác RAM)
        private void CapNhatPictureBoxNgayLapTuc(Image? hinhMoi)
        {
            UIHelper.SafeInvoke(this, () =>
            {
                if (hinhMoi != null && PictureBox1 != null && !PictureBox1.IsDisposed)
                {
                    // Lấy hình cũ ra để chờ dọn dẹp
                    Image? hinhCu = PictureBox1.Image;

                    // Gắn hình mới vào và ép vẽ ngay lập tức
                    PictureBox1.Image = hinhMoi;
                    PictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                    PictureBox1.Update();

                    // 🛑 Kỹ thuật chống tràn RAM
                    if (hinhCu != null && !ReferenceEquals(hinhCu, hinhMoi))
                    {
                        hinhCu.Dispose();
                    }
                }
            });
        }
        private void KhoiTaoGiaoDien()
        {
            int namHienTai = Module_HeThong.LayNamHeThong();
            OpenChildForm<Form4_TrangDauTien>(
                $"PHẦN MỀM PHÂN LOẠI THI ĐUA \"VÌ ANTQ\" NĂM {namHienTai}");

            // 🚀 TỐI ƯU UX: ẨN NÚT KHEN THƯỞNG NẾU LÀ TÂN BINH
            try
            {
                string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
                bool laTanBinh = phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase);

                // Nếu là Tân binh -> Visible = false (Ẩn nút)
                // Nếu là CBCS -> Visible = true (Hiện nút)
                if (kryptonButton1_KhenThuong != null)
                {
                    kryptonButton1_KhenThuong.Visible = !laTanBinh;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi kiểm tra phiên bản để ẩn nút Khen thưởng: {ex.Message}");
            }
        }
        private void CapNhatMuiTenGiaoDien()
        {
            UIHelper.SafeInvoke(this, () =>
            {
                if (PictureBox2 == null || PictureBox2.IsDisposed) return;

                // Xác định hình ảnh mới từ Resources
                Image hinhMoi = sidebarExpanded
                    ? Properties.Resources.SangTrai
                    : Properties.Resources.SangPhai;

                // Cấu hình chuẩn hiển thị
                PictureBox2.Image = hinhMoi;

                // 🛠️ ĐỔI THÀNH ZOOM: Thu phóng ảnh vừa vặn trọn vẹn vào khung mà không bị méo (giữ nguyên tỷ lệ)
                PictureBox2.SizeMode = PictureBoxSizeMode.Zoom;

                PictureBox2.Update();
            });
        }
        private void KhoiTaoTimerTuDongAnMenu()
        {
            if (timerTuDongAnMenu != null) return;

            timerTuDongAnMenu = new System.Windows.Forms.Timer
            {
                Interval = 3000
            };

            timerTuDongAnMenu.Tick += TimerTuDongAnMenu_Tick;
        }
        private void DangKySuKienTuDongAnMenu()
        {
            this.MouseMove -= Form_MouseMove;
            this.KeyDown -= Form_KeyDown;
            PanelContainer.MouseMove -= PanelContainer_MouseMove;

            this.MouseMove += Form_MouseMove;
            this.KeyDown += Form_KeyDown;
            PanelContainer.MouseMove += PanelContainer_MouseMove;
        }
        private void Form_MouseMove(object? sender, MouseEventArgs e)
        {
            ResetTuDongAnMenu();
        }
        private void Form_KeyDown(object? sender, KeyEventArgs e)
        {
            ResetTuDongAnMenu();
        }
        private void PanelContainer_MouseMove(object? sender, MouseEventArgs e)
        {
            ResetTuDongAnMenu();
        }
        private void InitToolTips_GioiThieuVaMenu()
        {
            var toolTip_GT = new System.Windows.Forms.ToolTip
            {
                IsBalloon = true,
                ToolTipTitle = "Gợi ý",
                ToolTipIcon = ToolTipIcon.Info,

                // UX: nhẹ – nhanh – không gây phiền
                InitialDelay = 200,
                AutoPopDelay = 1500,
                ReshowDelay = 100,
                ShowAlways = true
            };

            var tips = new Dictionary<System.Windows.Forms.Control, string>
    {
        { pictureBox3, "Xem giới thiệu về chứng thư số và chữ ký số" },
        { kryptonButton1_MoMenuPhanMem, "Mở menu chức năng của phần mềm" }
    };

            foreach (var tip in tips)
            {
                if (tip.Key != null)
                    toolTip_GT.SetToolTip(tip.Key, tip.Value);
            }
        }
        private void TimerTuDongAnMenu_Tick(object? sender, EventArgs e)
        {
            if (_isClosing) return;

            timerTuDongAnMenu?.Stop();

            if (sidebarExpanded && PanelLeft.IsHandleCreated)
                AnMenu();
        }
        private void ResetTuDongAnMenu()
        {
            if (!checkBox1_TuDongAnMenu.Checked) return;
            if (timerTuDongAnMenu == null) return;

            timerTuDongAnMenu.Stop();
            timerTuDongAnMenu.Start();
        }
        private void AnMenu()
        {
            PanelLeft.Width = sidebarMinWidth;
            sidebarExpanded = false;
            kryptonButton1_MoMenuPhanMem.Text = "Hiện menu";
            // ⭐ THÊM VÀO ĐÂY: Đồng bộ mũi tên khi ẩn menu
            CapNhatMuiTenGiaoDien();
        }
        private void EnableDoubleBuffering(Control ctrl)
        {
            if (!ctrl.IsHandleCreated)
                ctrl.CreateControl();

            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance)
                ?.SetValue(ctrl, true);
        }
        private readonly ConcurrentDictionary<Type, Form> _forms = new();
        public void OpenChildForm<T>(string title = "") where T : Form, new()
        {
            if (_isClosing || IsDisposed) return;

            if (Interlocked.Exchange(ref _switching, 1) == 1)
                return; // chống spam click

            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => OpenChildForm<T>(title)));
                    return;
                }

                var type = typeof(T);

                if (_currentChild != null &&
     !_currentChild.IsDisposed &&
     _currentChild.GetType() == type)
                {
                    return;
                }

                if (!_forms.TryGetValue(type, out var form) || form.IsDisposed)
                    _forms[type] = form = new T();

                _currentChild?.Hide();

                form.TopLevel = false;
                form.FormBorderStyle = FormBorderStyle.None;
                form.Dock = DockStyle.Fill;

                if (!PanelContainer.Controls.Contains(form))
                    PanelContainer.Controls.Add(form);

                form.Show();
                form.BringToFront();

                _currentChild = form;

                if (!string.IsNullOrWhiteSpace(title))
                    Label1.Text = title;
            }
            finally
            {
                Interlocked.Exchange(ref _switching, 0);
            }
        }
        private void MoMenuPhanMem_Click(object sender, EventArgs e)
        {
            ResetTuDongAnMenu();

            if (sidebarExpanded)
            {
                AnMenu();
            }
            else
            {
                PanelLeft.Width = sidebarWidth;
                sidebarExpanded = true;
                kryptonButton1_MoMenuPhanMem.Text = "Ẩn menu";
                // ⭐ THÊM VÀO ĐÂY: Đồng bộ mũi tên khi hiện menu
                CapNhatMuiTenGiaoDien();
            }
        }
        // 1. Thêm từ khóa 'async' vào chữ ký của sự kiện
        private async void Btn_Trangchu_Click(object sender, EventArgs e)
        {
            DongToanBoHuongDanSuDung(); //
            if (!AllowSwitch()) return;
            //ClosePdfIfOpen(); // 🔹 add ở đây

            // Gọi hàm đổi màu và truyền nút hiện tại vào
            HighlightNavButton((KryptonButton)sender);

            // =================================================================
            // BẮT ĐẦU CODE GỐC
            // =================================================================
            // 🔹 Nếu Form31 đang mở, giải phóng PDF trước
            //if (_currentChild is Form32_HuongDanPDF pdfForm)
            //{
            //    pdfForm.DisposePdf();   // giải phóng file PDF
            //    pdfForm.Hide();         // ẩn form
            //    _currentChild = null;   // clear current child
            //}

            int namHienTai = Module_HeThong.LayNamHeThong();

            OpenChildForm<Form4_TrangDauTien>(
                $"PHẦN MỀM PHÂN LOẠI THI ĐUA \"VÌ ANTQ\" NĂM {namHienTai}");

            // 🔹 Load lại dữ liệu (ĐÃ NÂNG CẤP CHUẨN ASYNC)
            if (_forms.TryGetValue(typeof(Form4_TrangDauTien), out var f))
            {
                if (f is Form4_TrangDauTien frm)
                {
                    try
                    {
                        // 2. Thêm 'await' và gọi đúng tên hàm Task
                        await frm.ReloadDuLieuAsync();
                    }
                    catch (Exception ex)
                    {
                        // 3. Bắt buộc phải có try-catch trong async void để chống Crash toàn hệ thống
                        Debug.WriteLine("Lỗi khi gọi ReloadDuLieuAsync từ Form2: " + ex.Message);
                    }
                }
            }
            // =================================================================
        }
        private void kryptonButton1_CaiDatPhanMem_Click(object sender, EventArgs e)
        {
            DongToanBoHuongDanSuDung(); //
            if (!AllowSwitch()) return;
            //ClosePdfIfOpen(); // 🔹 add ở đây
            // Gọi hàm đổi màu và truyền nút hiện tại vào
            HighlightNavButton((KryptonButton)sender);
            OpenChildForm<Form12>("Cài đặt");
        }
        private void SafeReload(Form6_XuLyData frm)
        {
            if (frm == null) return;
            if (frm.IsDisposed) return;
            if (!frm.IsHandleCreated) return;

            try
            {
                // 🔥 tránh block UI + chống giật
                frm.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        frm.ReloadDuLieu();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Reload Form6 lỗi: " + ex.Message);
                    }
                }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SafeReload lỗi: " + ex.Message);
            }
        }
        // Cache form trên RAM
        //private Form39_ThongTinNguoiDung _formThongTinNguoiDung;
        private void Label2_Click(object? sender, EventArgs e)
        {
            try
            {
                Form39_ThongTinNguoiDung form =
                    Form39_ThongTinNguoiDung.GetInstance();

                if (form.WindowState == FormWindowState.Minimized)
                {
                    form.WindowState = FormWindowState.Normal;
                }

                if (!form.Visible)
                {
                    form.Show(this);
                }

                form.BringToFront();
                form.Activate();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Form2] Lỗi mở Form39: {ex}");

                MessageBox.Show(
                    "Không thể mở thông tin người dùng.\n\n" +
                    ex.Message,
                    "UI Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        private bool _tuDongAnMenu;
        private void checkBox1_TuDongAnMenu_CheckedChanged(
    object sender,
    EventArgs e)
        {
            if (!isLoaded)
                return;

            // lưu RAM
            AppRuntime.TuDongAnMenu =
                checkBox1_TuDongAnMenu.Checked;

            if (AppRuntime.TuDongAnMenu)
            {
                ResetTuDongAnMenu();
            }
            else
            {
                timerTuDongAnMenu?.Stop();
            }
        }
        private async void kryptonButton_ThongKe_Click(object sender, EventArgs e)
        {
            DongToanBoHuongDanSuDung();
            if (!AllowSwitch()) return;
            HighlightNavButton(kryptonButton1_ThongKe); // Đảm bảo tên biến nút này đúng với tên nút của bạn

            if (_namHienTaiCache == -1) _namHienTaiCache = Module_HeThong.LayNamHeThong();

            // Tiêu đề này chỉ là hiển thị tạm trong tíc tắc, ngay sau đó Form 53 sẽ tự "hét" lên tiêu đề chính thức của Form 15
            string tieuDeForm = $"Trang Quản lý kết quả thi đua năm {_namHienTaiCache}";

            try
            {
                // 🌟 CHUẨN KỸ SƯ: Mở Form Container 53, bỏ hoàn toàn Form_Loading và logic đếm DB
                // Hàm OpenChildForm của bạn (với ConcurrentDictionary) đã tự lo việc lấy từ RAM hay tạo mới!
                OpenChildForm<Form53_QuanLyKetQuaThiDua>(tieuDeForm);

                // Kích hoạt load dữ liệu ngầm nếu Form 53 mới tinh và chưa nạp data
                if (_currentChild is Form53_QuanLyKetQuaThiDua frm && !frm.DaLoadDuLieu)
                {
                    await frm.ReloadDuLieu();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi tạo trang Quản lý Thi đua: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async void Btn_XuLyData_Click(object sender, EventArgs e)
        {
            DongToanBoHuongDanSuDung();
            if (!AllowSwitch()) return;
            HighlightNavButton((KryptonButton)sender);

            OpenChildForm<Form6_XuLyData>("Trang phân loại thi đua");

            if (_currentChild is Form6_XuLyData frm)
            {
                // 🌟 CHUẨN KỸ SƯ: Chỉ load nếu chưa từng load, và BẮT BUỘC ĐẨY XUỐNG LUỒNG NGẦM
                if (!frm.DaLoadDuLieu)
                {
                    try
                    {
                        await Task.Run(() => frm.ReloadDuLieu());
                        frm.DaLoadDuLieu = true; // Chốt cờ, các lần click tab sau sẽ mượt như lật trang sách
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Lỗi load Form6: " + ex.Message);
                    }
                }
            }
        }
        private async void kryptonButton_NhatKyPhanMem_Click(object sender, EventArgs e)
        {
            DongToanBoHuongDanSuDung();
            if (!AllowSwitch()) return;
            HighlightNavButton(kryptonButton1_NhatKyPhanMem);

            OpenChildForm<Form10_NhatKy>("Nhật ký phần mềm");

            if (_currentChild is Form10_NhatKy frm)
            {
                // 🌟 CHUẨN KỸ SƯ: Chỉ load 1 lần duy nhất, giải phóng UI Thread
                if (!frm.DaLoadDuLieu)
                {
                    try
                    {
                        await Task.Run(() => frm.ReloadDuLieuAsync());
                        frm.DaLoadDuLieu = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Lỗi load Form10: " + ex.Message);
                    }
                }
            }

            Module_NhatKy.GhiNhatKy(
                SessionInfo.TenTaiKhoan,
                "Mở Form Nhật ký phần mềm",
                "Người dùng mở form nhật ký từ Form2"
            );
        }
        private void SafeReloadForm10(Form10_NhatKy frm)
        {
            if (frm == null) return;
            if (frm.IsDisposed) return;
            if (!frm.IsHandleCreated) return;

            try
            {
                frm.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        frm.ReloadDuLieuAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Reload Form10 lỗi: " + ex.Message);
                    }
                }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SafeReload Form10 lỗi: " + ex.Message);
            }
        }
        private void kryptonButton_ThoatPhanMem_Click(object sender, EventArgs e)
        {
            this.Close(); // Form cha đóng → app tự thoát
        }
        private void EnsureSuKienThoatTonTai()
        {
            try
            {
                using var cn = new Microsoft.Data.Sqlite.SqliteConnection(
                    $"Data Source={_csdl2Path}");

                cn.Open();

                using var cmdCreate = cn.CreateCommand();
                cmdCreate.CommandText = @"
        CREATE TABLE IF NOT EXISTS SuKien_ThoatPhanMem (
            ID INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            SuKien_DuọcChon TEXT
        );";
                cmdCreate.ExecuteNonQuery();

                using var cmdCheck = cn.CreateCommand();
                cmdCheck.CommandText =
                    "SELECT COUNT(*) FROM SuKien_ThoatPhanMem WHERE ID = 1";

                long count = (long)cmdCheck.ExecuteScalar();

                if (count == 0)
                {
                    using var cmdInsert = cn.CreateCommand();
                    string maHoa = BaoMatAES.MaHoa("Thoát ngay");
                    cmdInsert.CommandText =
                        "INSERT INTO SuKien_ThoatPhanMem (SuKien_DuọcChon) VALUES (@v)";
                    cmdInsert.Parameters.AddWithValue("@v", maHoa);
                    cmdInsert.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EnsureSuKienThoatTonTai] {ex.Message}");
            }
        }
        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            DongToanBoHuongDanSuDung(); //
            // Nếu đang trong tiến trình đóng rồi thì bỏ qua
            if (_isClosing) return;

            try
            {
                string suKien = LoadSuKienThoat();

                // ==========================================
                // TRƯỜNG HỢP 1: QUAY VỀ FORM LOGIN
                // ==========================================
                if (suKien == "Trở về form đăng nhập")
                {
                    e.Cancel = true; // Chặn sự kiện đóng Form2 hiện tại
                    this.Hide();

                    var login = new Form1
                    {
                        StartPosition = FormStartPosition.CenterScreen
                    };

                    login.FormClosed += (s, args) =>
                    {
                        Environment.Exit(0); // App tắt hoàn toàn nếu tắt Login
                    };

                    login.Show();
                    return;
                }

                // ==========================================
                // TRƯỜNG HỢP 2: THOÁT HOÀN TOÀN HỆ THỐNG
                // ==========================================
                _isClosing = true;

                // 1. Dọn dẹp Timer
                if (timerTuDongAnMenu != null)
                {
                    timerTuDongAnMenu.Stop();
                    timerTuDongAnMenu.Dispose();
                }
                // 👇 Bổ sung dọn dẹp Timer Hình Nền
                if (_timerHinhNen != null)
                {
                    _timerHinhNen.Stop();
                    _timerHinhNen.Dispose();
                }
                // 2. Dọn dẹp Form con đang hiển thị
                if (_currentChild != null && !_currentChild.IsDisposed)
                {
                    _currentChild.Dispose();
                    _currentChild = null;
                }

                // 3. 🔥 CHUẨN KỸ SƯ: Giải phóng triệt để UI Handles trong Cache
                foreach (var form in _forms.Values)
                {
                    if (form != null && !form.IsDisposed)
                    {
                        form.Dispose();
                    }
                }
                _forms.Clear();

                // 4. Giải phóng đối tượng đa luồng (Tránh leak Semaphore)
                _huongDanLock?.Dispose();


                // 6. Đóng tiến trình an toàn và trả mã 0 (Thành công) cho OS
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi nghiêm trọng khi đóng Form: {ex}");

                // Trả mã 1 để báo cho Hệ điều hành biết là App thoát do bị lỗi
                Environment.Exit(1);
            }
        }
        private string LoadSuKienThoat()
        {
            try
            {
                using var cn = new Microsoft.Data.Sqlite.SqliteConnection(
                    $"Data Source={_csdl2Path}");

                cn.Open();

                using var cmd = cn.CreateCommand();
                cmd.CommandText =
                    "SELECT SuKien_DuọcChon FROM SuKien_ThoatPhanMem WHERE ID = 1";

                var result = cmd.ExecuteScalar();

                if (result != null && !string.IsNullOrWhiteSpace(result.ToString()))
                {
                    return BaoMatAES.GiaiMa(result.ToString());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return "Thoát ngay"; // fallback an toàn
        }
        private void PictureBox1_DoubleClick(object? sender, EventArgs e)
        {
            // CHỈ CHO PHÉP MỞ 1 LẦN
            if (_daMoWelcome)
                return;

            try
            {
                _daMoWelcome = true; // Đánh dấu trước để chống double-click liên tiếp

                using (var frm = new FormWelcome())
                {
                    frm.StartPosition = FormStartPosition.CenterScreen;
                    frm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                HienThiFormAo_Loi(
                    "LỖI KHỞI ĐỘNG MÀN HÌNH CHÀO",
                    $"Hệ thống không thể mở màn hình chào.\nChi tiết kỹ thuật:\n{ex.Message}");
            }
        }
        private int _isProcessing = 0; // Thêm biến này vào Form2_FormCha
        private void pictureBox3_Click(object sender, EventArgs e)
        {
            // Kiểm tra nhanh: Nếu đang xử lý hoặc đã mở rồi thì thoát
            if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) != 0)
                return;

            if (_daMoThongTinChungThu)
            {
                Interlocked.Exchange(ref _isProcessing, 0);
                return;
            }

            try
            {
                Module_DatabaseBackup.HienThiThongTinChungThu();
                _daMoThongTinChungThu = true;
            }
            catch (Exception ex)
            {
                // SỬA TẠI ĐÂY: Truyền thêm tham số kích thước new Size(840, 640) cho form thông báo lỗi
                Module_DatabaseBackup.HienThiFormAoChung(
                    "LỖI HỆ THỐNG",
                    $"Hệ thống không thể khởi chạy:\n{ex.Message}",
                    MessageBoxIcon.Error,
                    new Size(840, 640));
            }
            finally
            {
                // Reset lại sau khi xong
                Interlocked.Exchange(ref _isProcessing, 0);
            }
        }
        private void HienThiFormAo_Loi(string tieuDe, string noiDungLoi)
        {
            // Tiêu chuẩn hóa ký tự xuống dòng để đảm bảo không bị lỗi font trên TextBox
            string noiDungChuan = noiDungLoi.Replace("\n", Environment.NewLine);

            using (var formAo = new Krypton.Toolkit.KryptonForm())
            {
                formAo.Text = "Hệ thống ghi nhận sự cố";
                formAo.Size = new System.Drawing.Size(550, 320); // Kích thước rộng hơn để chứa Icon
                formAo.StartPosition = FormStartPosition.CenterParent;
                formAo.FormBorderStyle = FormBorderStyle.FixedDialog;
                formAo.MaximizeBox = false;
                formAo.MinimizeBox = false;
                formAo.ShowIcon = false;

                // --- 1. PANEL TIÊU ĐỀ (Đỏ Thẫm) ---
                var panelTop = new Krypton.Toolkit.KryptonPanel
                {
                    Dock = DockStyle.Top,
                    Height = 65,
                    Padding = new Padding(25, 20, 20, 5)
                };
                panelTop.StateCommon.Color1 = System.Drawing.Color.White;

                var lblTitle = new Krypton.Toolkit.KryptonLabel
                {
                    Text = tieuDe.ToUpper(),
                    Dock = DockStyle.Fill,
                    AutoSize = false
                };
                // Sử dụng màu Đỏ Thẫm (Crimson Red) để báo hiệu sự cố chuyên nghiệp
                lblTitle.StateCommon.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 12F, System.Drawing.FontStyle.Bold);
                lblTitle.StateCommon.ShortText.Color1 = System.Drawing.Color.FromArgb(198, 40, 40);

                // --- 2. ĐƯỜNG KẺ NGANG (Separator) ---
                var separator = new Label
                {
                    Height = 1,
                    Dock = DockStyle.Top,
                    BackColor = System.Drawing.Color.FromArgb(240, 200, 200), // Kẻ ngang màu hồng nhạt
                    Margin = new Padding(0, 5, 0, 10)
                };

                // --- 3. PANEL NỘI DUNG CHÍNH (Chứa Icon và Text) ---
                var panelContent = new Krypton.Toolkit.KryptonPanel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(20, 15, 25, 20)
                };
                panelContent.StateCommon.Color1 = System.Drawing.Color.White;

                // Thêm Icon Lỗi mặc định của Windows
                var picIcon = new PictureBox
                {
                    Image = System.Drawing.SystemIcons.Error.ToBitmap(),
                    SizeMode = PictureBoxSizeMode.CenterImage,
                    Size = new System.Drawing.Size(50, 50),
                    Location = new System.Drawing.Point(20, 15) // Neo bên trái
                };

                var txtContent = new Krypton.Toolkit.KryptonTextBox
                {
                    Text = noiDungChuan,
                    ReadOnly = true,
                    Multiline = true,
                    WordWrap = true,
                    ScrollBars = ScrollBars.Vertical,
                    // Neo TextBox bên phải Icon
                    Location = new System.Drawing.Point(80, 15),
                    Width = formAo.Width - 110,
                    Height = panelContent.Height - 35,
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
                };

                // 🔴 ĐIỂM SÁNG: TextBox Tàng Hình! Giống y hệt Label nhưng có thể bôi đen mã lỗi
                txtContent.StateCommon.Back.Color1 = System.Drawing.Color.White;
                txtContent.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.None;
                txtContent.StateCommon.Content.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 10.5F, System.Drawing.FontStyle.Regular);
                txtContent.StateCommon.Content.Color1 = System.Drawing.Color.FromArgb(40, 40, 40);
                txtContent.StateCommon.Content.Padding = new Padding(0);

                // --- 4. PANEL NÚT BẤM ---
                var panelBottom = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 60,
                    BackColor = System.Drawing.Color.WhiteSmoke
                };

                // Nút Sao chép thông minh
                var btnCopy = new Krypton.Toolkit.KryptonButton
                {
                    Text = "Sao chép chi tiết lỗi",
                    Width = 160,
                    Height = 35
                };
                btnCopy.StateCommon.Content.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 9.5F, System.Drawing.FontStyle.Bold);
                btnCopy.Click += (s, ev) =>
                {
                    try
                    {
                        Clipboard.SetText(noiDungChuan);
                        MessageBox.Show(formAo, "Đã sao chép mã lỗi.\nVui lòng gửi nội dung này cho bộ phận kỹ thuật (Admin: TrungKien) để được hỗ trợ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch { }
                };

                var btnClose = new Krypton.Toolkit.KryptonButton
                {
                    Text = "Đóng",
                    Width = 100,
                    Height = 35,
                    DialogResult = DialogResult.OK
                };
                btnClose.StateCommon.Content.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 9.5F, System.Drawing.FontStyle.Bold);

                // Căn giữa cụm 2 nút bấm
                int totalWidth = btnCopy.Width + 15 + btnClose.Width;
                int startX = (formAo.Width - totalWidth) / 2;

                btnCopy.Location = new System.Drawing.Point(startX, 12);
                btnClose.Location = new System.Drawing.Point(startX + btnCopy.Width + 15, 12);

                panelBottom.Controls.Add(btnCopy);
                panelBottom.Controls.Add(btnClose);

                // --- 5. RÁP LAYER ---
                panelContent.Controls.Add(picIcon);
                panelContent.Controls.Add(txtContent);

                panelContent.Controls.Add(separator);
                panelTop.Controls.Add(lblTitle);

                txtContent.BringToFront();
                picIcon.BringToFront();
                separator.SendToBack();

                formAo.Controls.Add(panelContent);
                formAo.Controls.Add(panelTop);
                formAo.Controls.Add(panelBottom);

                formAo.AcceptButton = btnClose;
                formAo.CancelButton = btnClose;
                formAo.Shown += (s, ev) => btnClose.Focus();

                // Ghi Log ngầm hệ thống
                System.Diagnostics.Debug.WriteLine($"[ERR_UI] {tieuDe} - {noiDungChuan}");

                formAo.ShowDialog(this);
            }
        }
        public void CapNhatGiaoDienTheoPhienBan()
        {
            UIHelper.SafeInvoke(this, () =>
            {
                // 1. Kiểm tra lại phiên bản mới nhất
                string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
                bool laTanBinh = phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase);

                // 2. Ẩn/Hiện nút Khen thưởng
                if (kryptonButton1_KhenThuong != null)
                {
                    kryptonButton1_KhenThuong.Visible = !laTanBinh;
                }

                // 3. QUAN TRỌNG: Đóng các Form dữ liệu cũ đang mở để tránh ghi nhầm dữ liệu
                // Xóa chúng khỏi bộ nhớ Cache (_forms) để khi click lại Menu, chúng sẽ tự tạo mới 100%
                var danhSachFormCanDong = new[] {
                    typeof(Form6_XuLyData),
                    typeof(Form15_ThongKeThiDua),
                    typeof(Form34_ThongKeKhenThuong)
                };

                bool formHienTaiBiDong = false;

                foreach (var type in danhSachFormCanDong)
                {
                    if (_forms.TryGetValue(type, out var formToClose))
                    {
                        // Kiểm tra xem form sắp bị tiêu diệt có trùng với form đang hiển thị không
                        if (_currentChild == formToClose)
                        {
                            formHienTaiBiDong = true;
                        }

                        formToClose?.Dispose(); // Tiêu diệt Form cũ
                        _forms.TryRemove(type, out _);    // Xóa khỏi bộ nhớ đệm
                    }
                }

                // 4. CHỈ chuyển về Trang chủ (Form 4) nếu cái Form đang hiển thị vừa bị tiêu diệt.
                // Vì bạn đang đứng ở Form 12 (Cài đặt) -> Form 12 không nằm trong danh sách tiêu diệt 
                // -> formHienTaiBiDong = false -> Sẽ KHÔNG bị nhảy trang nữa!
                if (formHienTaiBiDong)
                {
                    Btn_Trangchu_Click(this, EventArgs.Empty);
                }
            });
        }
        private async void kryptonButton1_KhenThuong_Click(object sender, EventArgs e)
        {
            DongToanBoHuongDanSuDung();
            if (!AllowSwitch()) return;

            // Gọi hàm đổi màu và truyền nút hiện tại vào
            HighlightNavButton((KryptonButton)sender);

            // =================================================================
            // BẮT ĐẦU CODE ĐÃ ĐỔI SANG Form52_QuanLyKhenThuong
            // =================================================================

            // Tên Form: Quản lý khen thưởng CBCS + năm hệ thống
            string tieuDeForm = "Trang Quản lý khen thưởng CBCS năm " +
                                Module_HeThong.LayNamHeThong();
            OpenChildForm<Form52_QuanLyKhenThuong>(tieuDeForm);

            if (_currentChild is Form52_QuanLyKhenThuong frm)
            {
                // Nếu trước đó tải ngầm chưa xong hoặc chưa tải thì mới await
                if (!frm.DaLoadDuLieu)
                {
                    await frm.ReloadDuLieu();
                }
            }
            // =================================================================
        }
        private void kryptonButton1_ThoatHeThong_Click(object sender, EventArgs e)
        {
            DongToanBoHuongDanSuDung(); //
            Close();
        }
        private KryptonButton? _currentButton = null;
        private void HighlightNavButton(KryptonButton? clickedButton)
        {
            // 1. Guard Clause: Kiểm tra an toàn, ngăn chặn lỗi Null Reference hoặc control đã bị hủy
            if (clickedButton == null || clickedButton.IsDisposed) return;

            // 2. Tối ưu UX/Performance: Nếu click lại chính nút đang được chọn -> Bỏ qua (không làm gì cả)
            if (_currentButton == clickedButton) return;

            // 3. Tắt màu nút cũ (nếu có và chưa bị hủy)
            if (_currentButton != null && !_currentButton.IsDisposed)
            {
                ResetKryptonButton(_currentButton);
            }

            // 4. Cập nhật trạng thái nút hiện tại thành nút vừa click
            _currentButton = clickedButton;

            // 5. Gọi hàm tô màu cho nút mới
            ApplyHighlightColor(_currentButton);
        }
        private async void kryptonButton1_HuongDan_Click(object sender, EventArgs e)
        {
            Label1.Text = "Đang chuẩn bị tài liệu hướng dẫn...";

            try
            {
                string cheDo = Module_HuongDanSuDung.LayCheDoXemHuongDan();

                // =====================================================
                // PDF MODE
                // =====================================================
                if (cheDo == "Chế độ pdf")
                {
                    if (!await _huongDanLock.WaitAsync(0))
                    {
                        Module_NhatKy.GhiNhatKy(
                            taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                            hanhDong: "Mở Hướng dẫn (PDF) thất bại",
                            ghiChu: "Hệ thống đang bận xử lý tài liệu khác."
                        );

                        MessageBox.Show("Tài liệu đang được xử lý, vui lòng đợi.", "Thông báo");
                        return;
                    }

                    try
                    {
                        string pdfPath = Module_HuongDanSuDung.TimFileHuongDanPdf();

                        if (string.IsNullOrWhiteSpace(pdfPath))
                        {
                            Module_NhatKy.GhiNhatKy(
                                taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                                hanhDong: "Mở Hướng dẫn (PDF) thất bại",
                                ghiChu: "Lỗi: Không tìm thấy tệp PDF hướng dẫn trong cơ sở dữ liệu."
                            );

                            MessageBox.Show("Không tìm thấy tài liệu PDF.", "Thông báo");
                            return;
                        }

                        var existingPdf = PanelContainer.Controls.OfType<Form32_HuongDanPDF>().FirstOrDefault();

                        // =========================================
                        // REUSE EXISTING FORM
                        // =========================================
                        if (existingPdf != null && !existingPdf.IsDisposed)
                        {
                            existingPdf.GoiTenEmTrongDem_LoadPdf(pdfPath);
                            existingPdf.Show();
                            existingPdf.BringToFront();

                            _currentChild?.Hide();
                            _currentChild = existingPdf;
                            HighlightNavButton(kryptonButton1_HuongDan);

                            Module_NhatKy.GhiNhatKy(
                                taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                                hanhDong: "Chuyển tab Hướng dẫn (PDF)",
                                ghiChu: "Người dùng tái sử dụng form pdf đã mở."
                            );

                            return;
                        }

                        // =========================================
                        // CREATE NEW FORM
                        // =========================================
                        Form32_HuongDanPDF pdf = new()
                        {
                            TopLevel = false,
                            FormBorderStyle = FormBorderStyle.None,
                            Dock = DockStyle.Fill,
                            Text = "Hướng dẫn sử dụng"
                        };

                        _currentChild?.Hide();
                        PanelContainer.Controls.Add(pdf);
                        _currentChild = pdf;

                        pdf.FormClosed += (s, ev) =>
                        {
                            if (PanelContainer.IsDisposed) return;
                            if (_currentChild == pdf) _currentChild = null;

                            var defaultForm = PanelContainer.Controls.OfType<Form4_TrangDauTien>().FirstOrDefault();
                            if (defaultForm != null && !defaultForm.IsDisposed)
                            {
                                defaultForm.Show();
                                defaultForm.BringToFront();
                                _currentChild = defaultForm;
                            }
                        };

                        pdf.GoiTenEmTrongDem_LoadPdf(pdfPath);
                        pdf.Show();
                        pdf.BringToFront();
                        HighlightNavButton(kryptonButton1_HuongDan);

                        Module_NhatKy.GhiNhatKy(
                            taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                            hanhDong: "Mở Hướng dẫn (PDF) thành công",
                            ghiChu: "Khởi tạo và mở tài liệu PDF thành công từ Form2."
                        );
                    }
                    finally
                    {
                        _huongDanLock.Release();
                    }

                    return;
                }

                // =====================================================
                // WEB MODE
                // =====================================================
                bool opened = await Task.Run(() =>
                {
                    return Module_HuongDanSuDung.MoHuongDanBangWeb();
                });

                if (!opened)
                {
                    Module_NhatKy.GhiNhatKy(
                        taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                        hanhDong: "Mở Hướng dẫn (Web) thất bại",
                        ghiChu: "Không thể đồng bộ hoặc khởi chạy trình duyệt web."
                    );

                    MessageBox.Show("Không thể mở hướng dẫn web.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    Module_NhatKy.GhiNhatKy(
                        taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                        hanhDong: "Mở Hướng dẫn (Web) thành công",
                        ghiChu: "Hiển thị tài liệu bằng trình duyệt web thành công."
                    );
                }
            }
            catch (Exception ex)
            {
                Module_NhatKy.GhiNhatKy(
                    taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                    hanhDong: "Lỗi ngoại lệ: Mở Hướng dẫn sử dụng",
                    ghiChu: $"Lỗi: {ex.Message}"
                );

                MessageBox.Show(ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Label1.Text = "Hướng dẫn sử dụng Phần mềm Thi đua 2026";
            }
        }
        public void CapNhatTieuDe(string text)
        {
            // Cập nhật text cho Label1
            Label1.Text = text;
        }
        private void DongToanBoHuongDanSuDung()
        {
            // 🌟 CHUẨN KỸ SƯ: Dừng ngay nếu form hiện tại KHÔNG PHẢI là Form Hướng Dẫn
            // Việc này giúp cắt bỏ hao phí quét Panel và gọi hàm rác ở các nút Tab khác
            if (!(_currentChild is Form32_HuongDanPDF)) return;

            try
            {
                Module_HuongDanSuDung.DongHuongDan();
                var pdf = PanelContainer.Controls.OfType<Form32_HuongDanPDF>().FirstOrDefault();
                if (pdf != null && !pdf.IsDisposed)
                {
                    pdf.Close();
                    pdf.Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[FORM2 CLOSE ALL ERROR] " + ex.Message);
            }
        }
        private void ApplyHighlightColor(KryptonButton btn)
        {
            if (btn == null || btn.IsDisposed) return;
            Color highlightColor = Color.FromArgb(11, 199, 1);
            btn.StateCommon.Back.Color1 = highlightColor;
            btn.StateCommon.Back.Color2 = highlightColor;
            btn.OverrideDefault.Back.Color1 = highlightColor;
            btn.OverrideDefault.Back.Color2 = highlightColor;

            // 🌟 Thay Refresh (ép vẽ đồng bộ) thành Invalidate (xếp hàng vẽ bất đồng bộ)
            btn.Invalidate();
        }
        private void ResetKryptonButton(KryptonButton btn)
        {
            if (btn == null || btn.IsDisposed) return;
            btn.StateCommon.Back.Color1 = Color.Empty;
            btn.StateCommon.Back.Color2 = Color.Empty;
            btn.OverrideDefault.Back.Color1 = Color.Empty;
            btn.OverrideDefault.Back.Color2 = Color.Empty;

            // 🌟 Thay Refresh thành Invalidate
            btn.Invalidate();
        }
    }
}
public static class UIHelper
{
    public static void SafeInvoke(Control ctrl, Action action)
    {
        if (ctrl == null) return;
        if (ctrl.IsDisposed) return;
        if (!ctrl.IsHandleCreated) return;
        try
        {
            if (ctrl.InvokeRequired)
                ctrl.BeginInvoke(action);
            else
                action();
        }
        catch
        {
        }
    }
    internal static class AppRuntime
    {
        // cache RAM
        public static bool TuDongAnMenu = false;
    }
}
