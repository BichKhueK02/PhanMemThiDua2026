using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace PhanMemThiDua2026
{
    internal static class Module_TrangThaiHeThong
    {


        // ================== BỘ NHỚ ĐỆM & TÀI NGUYÊN DÙNG CHUNG ==================
        private static readonly ToolTip _sharedToolTip = new ToolTip { AutoPopDelay = 10000, InitialDelay = 500, ReshowDelay = 100, ShowAlways = true };
        private static string _cachedCpuName = string.Empty;

        // 🌟 KẾT NỐI KERNEL32: Đọc RAM siêu tốc, né hoàn toàn Antivirus / EDR
        [StructLayout(LayoutKind.Sequential)] // ĐÃ SỬA: LayoutMode -> LayoutKind
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

  
        public static void CapNhatStatusCSDL(
       StatusStrip status,
       ToolStripStatusLabel label)
        {
            if (status == null ||
                label == null ||
                status.IsDisposed ||
                label.IsDisposed)
            {
                return;
            }

            try
            {
                // Chuyển về UI Thread nếu cần
                if (status.InvokeRequired)
                {
                    try
                    {
                        status.BeginInvoke(new Action(() =>
                            CapNhatStatusCSDL(status, label)));
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                    catch (InvalidOperationException)
                    {
                    }

                    return;
                }

                bool csdlSanSang =
                    Module_DanduongGPS.KiemTraTrangThaiSanSangCuaHeThongCSDL();

                DateTime thoiGianDangNhap =
                    SessionInfo.ThoiGianDangNhap;

                string thoiGianStr =
                    thoiGianDangNhap == default
                    ? "(chưa đăng nhập)"
                    : $"Truy cập lúc {thoiGianDangNhap:hh:mm tt}, ngày {thoiGianDangNhap:dd/M/yyyy}";

                string tenMay = Environment.MachineName;
                string tenUser = Environment.UserName;

                string hienThiNgan =
                    csdlSanSang
                    ? $"Đang kết nối CSDL | User: {tenUser} | {thoiGianStr}"
                    : $"Mất kết nối | User: {tenUser} | {thoiGianStr}";

                string tooltipChiTiet =
                    $"Máy tính: {tenMay}\n" +
                    $"Tài khoản Windows: {tenUser}\n" +
                    $"Phiên kết nối: {thoiGianStr}";

                status.BackColor =
                    csdlSanSang
                    ? Color.FromArgb(220, 248, 198)
                    : Color.FromArgb(255, 224, 224);

                label.Text = hienThiNgan;

                label.ForeColor =
                    csdlSanSang
                    ? Color.DarkGreen
                    : Color.DarkRed;

                _sharedToolTip?.SetToolTip(
                    status,
                    tooltipChiTiet);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"CapNhatStatusCSDL Error: {ex}");
            }
        }
        public static string LayUUIDMayTinh()
        {
            try
            {
                string rawSysInfo = Environment.MachineName + Environment.UserDomainName + Environment.UserName + Environment.ProcessorCount;
                using (SHA256 sha = SHA256.Create())
                {
                    byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawSysInfo));
                    return "SYS-" + BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 16);
                }
            }
            catch { return $"SYS-{Environment.MachineName}"; }
        }
        public static void MoFormNhungVaoPanel(Form formHienTai)
        {
            Panel? panelContainer = formHienTai.Parent as Panel;
            if (panelContainer == null)
            {
                using (var f = new Form_TaskManagerMini()) { f.ShowDialog(); }
                return;
            }

            var fTask = panelContainer.Controls.OfType<Form_TaskManagerMini>().FirstOrDefault();
            if (fTask == null)
            {
                fTask = new Form_TaskManagerMini { TopLevel = false, FormBorderStyle = FormBorderStyle.None, Dock = DockStyle.Fill };
                panelContainer.Controls.Add(fTask);

                fTask.FormClosed += (s, ev) =>
                {
                    if (!formHienTai.IsDisposed) { formHienTai.Show(); formHienTai.BringToFront(); }
                };
            }
            formHienTai.Hide();
            fTask.Show();
            fTask.BringToFront();
        }
        public static void NhungFormVaoTabPage(Control targetContainer)
        {
            if (targetContainer == null) return;

            var fTask = targetContainer.Controls.OfType<Form_TaskManagerMini>().FirstOrDefault();

            if (fTask == null)
            {
                fTask = new Form_TaskManagerMini
                {
                    TopLevel = false,
                    FormBorderStyle = FormBorderStyle.None,
                    Dock = DockStyle.Fill
                };
                targetContainer.Controls.Add(fTask);
            }

            fTask.Show();
            fTask.BringToFront();
        }
        // ================== CÁC HÀM TRUY VẤN ==================
        private static string GetCpuName()
        {
            if (!string.IsNullOrWhiteSpace(_cachedCpuName))
                return _cachedCpuName;

            try
            {
                _cachedCpuName = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Unknown CPU";
            }
            catch
            {
                _cachedCpuName = "Unknown CPU";
            }

            return _cachedCpuName;
        }
        private static string LayDotNetRuntime()
        {
            try { return RuntimeInformation.FrameworkDescription; }
            catch { return ".NET Unknown"; }
        }
        private static string LayWindowsVersionChiTiet()
        {
            try
            {
                // Dùng API an toàn thay vì đọc Registry tốn I/O
                return RuntimeInformation.OSDescription;
            }
            catch { return Environment.OSVersion.ToString(); }
        }
        private static string LayTrangThaiUAC()
        {
            try
            {
                // Kiểm tra bằng quyền hạn Thread thay vì đọc Registry Policies (Né Cảnh báo EDR)
                using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
                {
                    var principal = new System.Security.Principal.WindowsPrincipal(identity);
                    bool isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
                    return isAdmin ? "Đang chạy với quyền Cao nhất (Admin)" : "Tiêu chuẩn (User thường)";
                }
            }
            catch { return "Không xác định"; }
        }
        // =========================================================================================
        // 🌟 CUSTOM CONTROL: Chống nháy (Flickering) An Toàn, không dùng Reflection
        // =========================================================================================
        private class SmoothPanel : Panel
        {
           public SmoothPanel()
            {
                this.DoubleBuffered = true;
                this.ResizeRedraw = true;
                this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            }
        }
        private class SmoothListView : ListView
        {
            public SmoothListView()
            {
                this.DoubleBuffered = true;
                this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            }
        }

        // =========================================================================================
        // 🚀 FORM NỘI BỘ (COMPACT WIDGET LAYOUT)
        // =========================================================================================
        private class Form_TaskManagerMini : Form
        {
            // 🌟 CHUẨN KỸ SƯ: Sử dụng 'volatile' để đồng bộ hóa dữ liệu giữa Background Thread và UI Timer
            // Đảm bảo không bao giờ bị hiện tượng hiển thị dữ liệu ảo (Stale Data)
            private volatile float _bgSysRamPercent = 0;
            private string _bgSysRamText = "0 / 0 GB";
            private volatile float _bgAppRamPercent = 0;
            private string _bgAppRamText = "0 MB";

            private CancellationTokenSource? _monitorCts;


            private SmoothPanel pnlSysRamBar, pnlAppRamBar;
            private Label lblSysRamPercent, lblAppRamPercent;
            private Label lblSysRamDetail, lblAppRamDetail;
            private SmoothListView lvInfo;
            private System.Windows.Forms.Timer updateTimer;

   

            public Form_TaskManagerMini()
            {
                // ĐÃ KHẮC PHỤC: Bỏ InitializeComponent();
                this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

                InitializeModernUI_SplitLayout();

                this.Load += async (s, e) =>
                {
                    await LoadListViewDataAsync();

                    _monitorCts = new CancellationTokenSource();

                    // Kích hoạt luồng ngầm an toàn
                    _ = Task.Run(() => HardwareSafePollingLoop(_monitorCts.Token));

                    updateTimer = new System.Windows.Forms.Timer { Interval = 1000 };
                    updateTimer.Tick += UpdateTimer_Tick;
                    updateTimer.Start();
                };
            }

            private void InitializeModernUI_SplitLayout()
            {
                this.SuspendLayout();

                this.Text = "Thông tin hệ thống";
                this.Size = new Size(1200, 650);
                this.MinimumSize = new Size(950, 550);
                this.StartPosition = FormStartPosition.CenterScreen;
                this.BackColor = Color.FromArgb(245, 246, 250);

                // ĐÃ KHẮC PHỤC: Explicit định danh Font để tránh xung đột SixLabors
                this.Font = new System.Drawing.Font("Segoe UI", 10F);
                this.ShowIcon = false;

                SplitContainer split = new SplitContainer
                {
                    Dock = DockStyle.Fill,
                    Panel1MinSize = 0,
                    Panel2MinSize = 0,
                    FixedPanel = FixedPanel.Panel1,
                    IsSplitterFixed = false,
                    SplitterWidth = 4,
                    BackColor = Color.FromArgb(230, 230, 230)
                };

                Panel pnlLeft = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

                int leftPadding = 25;
                int safeWidth = 250;

                // CỤM RAM
                Label lblRamTitle = new Label { Text = "RAM MÁY TÍNH", Font = new System.Drawing.Font("Segoe UI Semibold", 9F), ForeColor = Color.DarkGray, AutoSize = false, Size = new Size(safeWidth, 20), Location = new Point(leftPadding, 30), TextAlign = ContentAlignment.BottomLeft };
                lblSysRamPercent = new Label { Text = "0%", Font = new System.Drawing.Font("Segoe UI", 32F, System.Drawing.FontStyle.Bold), ForeColor = Color.FromArgb(45, 52, 54), AutoSize = false, Size = new Size(safeWidth, 60), Location = new Point(leftPadding - 3, 55), TextAlign = ContentAlignment.MiddleLeft };
                lblSysRamDetail = new Label { Text = "0,0 / 0,0 GB", Font = new System.Drawing.Font("Segoe UI", 10F), ForeColor = Color.FromArgb(127, 140, 141), AutoSize = false, Size = new Size(safeWidth, 25), Location = new Point(leftPadding, 115), TextAlign = ContentAlignment.MiddleLeft };

                pnlSysRamBar = new SmoothPanel { Height = 10, Location = new Point(leftPadding, 145), Width = 230, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
                pnlSysRamBar.Paint += (s, e) => DrawFlatProgressBar(e.Graphics, pnlSysRamBar.ClientRectangle, _bgSysRamPercent, false);

                // CỤM APP
                Label lblAppTitle = new Label { Text = "APP ĐANG CHIẾM", Font = new System.Drawing.Font("Segoe UI Semibold", 9F), ForeColor = Color.DarkGray, AutoSize = false, Size = new Size(safeWidth, 20), Location = new Point(leftPadding, 210), TextAlign = ContentAlignment.BottomLeft };
                lblAppRamPercent = new Label { Text = "0%", Font = new System.Drawing.Font("Segoe UI", 32F, System.Drawing.FontStyle.Bold), ForeColor = Color.FromArgb(45, 52, 54), AutoSize = false, Size = new Size(safeWidth, 60), Location = new Point(leftPadding - 3, 235), TextAlign = ContentAlignment.MiddleLeft };
                lblAppRamDetail = new Label { Text = "0 MB", Font = new System.Drawing.Font("Segoe UI", 10F), ForeColor = Color.FromArgb(127, 140, 141), AutoSize = false, Size = new Size(safeWidth, 25), Location = new Point(leftPadding, 295), TextAlign = ContentAlignment.MiddleLeft };

                pnlAppRamBar = new SmoothPanel { Height = 10, Location = new Point(leftPadding, 325), Width = 230, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
                pnlAppRamBar.Paint += (s, e) => DrawFlatProgressBar(e.Graphics, pnlAppRamBar.ClientRectangle, _bgAppRamPercent, true);

                pnlLeft.Controls.AddRange(new Control[] { lblRamTitle, lblSysRamPercent, lblSysRamDetail, pnlSysRamBar, lblAppTitle, lblAppRamPercent, lblAppRamDetail, pnlAppRamBar });

                Panel pnlRight = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(245, 246, 250), Padding = new Padding(15) };

                lvInfo = new SmoothListView
                {
                    Dock = DockStyle.Fill,
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines = true,
                    HeaderStyle = ColumnHeaderStyle.Nonclickable,
                    BorderStyle = BorderStyle.None,
                    Font = new System.Drawing.Font("Segoe UI", 10F),
                    BackColor = Color.White
                };

                lvInfo.Columns.Add("Thành phần", 260);
                lvInfo.Columns.Add("Thông tin chi tiết", 500);

                lvInfo.Resize += (s, e) =>
                {
                    if (lvInfo.Columns.Count < 2) return;
                    lvInfo.Columns[1].Width = Math.Max(300, lvInfo.ClientSize.Width - lvInfo.Columns[0].Width - 2);
                };
                pnlRight.Controls.Add(lvInfo);

                Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = Color.White };
                pnlBottom.Paint += (s, e) => { e.Graphics.DrawLine(new Pen(Color.FromArgb(230, 230, 230), 1), 0, 0, pnlBottom.Width, 0); };

                Button btnExport = new Button
                {
                    Text = "📄 Xuất báo cáo",
                    Size = new Size(220, 38),
                    Location = new Point(0, 16),
                    BackColor = Color.FromArgb(9, 132, 227),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnExport.FlatAppearance.BorderSize = 0;
                btnExport.Click += BtnExport_Click;

                pnlBottom.Resize += (s, e) =>
                {
                    btnExport.Location = new Point(pnlBottom.Width - btnExport.Width - 20, 16);
                };
                pnlBottom.Controls.Add(btnExport);

                split.Panel1.Controls.Add(pnlLeft);
                split.Panel2.Controls.Add(pnlRight);
                this.Controls.Add(split);
                this.Controls.Add(pnlBottom);

                this.HandleCreated += (s, e) =>
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            int targetDistance = 300;
                            if (this.Width > targetDistance + 400)
                            {
                                split.SplitterDistance = targetDistance;
                            }
                            split.Panel1MinSize = 260;
                            split.Panel2MinSize = 500;
                        }
                        catch { }
                    }));
                };

                this.ResumeLayout(false);
            }

            private async Task LoadListViewDataAsync()

            {
                lvInfo.Items.Clear();

                // 🌟 BƯỚC 1: Xử lý các tác vụ truy vấn dữ liệu nặng trên Background Thread (Không đụng UI)
                var sysData = await Task.Run(() =>
                {
                    string dinhDangVung = System.Globalization.CultureInfo.CurrentCulture.DisplayName;
                    string dinhDangNgay = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;

                    string appDir = AppContext.BaseDirectory;
                    string rootDrive = Path.GetPathRoot(appDir) ?? "C:\\";
                    string thongTinOChuaApp = "Không xác định";
                    try
                    {
                        DriveInfo dInfo = new DriveInfo(rootDrive);
                        if (dInfo.IsReady)
                        {
                            thongTinOChuaApp = $"Trống {dInfo.AvailableFreeSpace / 1073741824} GB / Tổng {dInfo.TotalSize / 1073741824} GB ({dInfo.DriveFormat})";
                        }
                    }
                    catch { }

                    return new
                    {
                        DinhDangVung = $"{dinhDangVung} ({dinhDangNgay})",
                        WindowsVersion = LayWindowsVersionChiTiet(),
                        Is64BitOS = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit",
                        Is64BitProc = Environment.Is64BitProcess ? "64-bit (Tối ưu)" : "32-bit",
                        UAC = LayTrangThaiUAC(),
                        CpuName = GetCpuName(),
                        ProcCount = $"{Environment.ProcessorCount} Luồng",
                        UUID = LayUUIDMayTinh(),
                        RootDrive = rootDrive,
                        ThongTinOChuaApp = thongTinOChuaApp,
                        DotNetVer = LayDotNetRuntime()
                    };
                });

                // 🌟 BƯỚC 2: Thao tác giao diện (Control, Font, Màu) độc quyền trên luồng UI
                // Đảm bảo không bao giờ bị dính Exception Cross-Thread
                string doPhanGiai = $"{Screen.PrimaryScreen.Bounds.Width} x {Screen.PrimaryScreen.Bounds.Height}";
                int dpiX = 96;
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) { dpiX = (int)g.DpiX; }
                string scaling = $"{Math.Round((dpiX / 96.0) * 100)}%";

                Font boldFont = new System.Drawing.Font(lvInfo.Font, System.Drawing.FontStyle.Bold);

                var items = new[]
                {
                    new ListViewItem(new[] { "Môi trường Windows", "" }) { BackColor = Color.AliceBlue, Font = boldFont },
                    new ListViewItem(new[] { "Hệ điều hành", sysData.WindowsVersion }),
                    new ListViewItem(new[] { "Kiến trúc OS", sysData.Is64BitOS }),
                    new ListViewItem(new[] { "Kiến trúc Ứng dụng", sysData.Is64BitProc }),
                    new ListViewItem(new[] { "Phân quyền ứng dụng", sysData.UAC }),

                    new ListViewItem(new[] { "Cấu hình hiển thị & Vùng", "" }) { BackColor = Color.AliceBlue, Font = boldFont },
                    new ListViewItem(new[] { "Độ phân giải màn hình", doPhanGiai }),
                    new ListViewItem(new[] { "Tỷ lệ thu phóng (Scaling)", scaling }),
                    new ListViewItem(new[] { "Định dạng vùng (Culture)", sysData.DinhDangVung }),

                    new ListViewItem(new[] { "Cấu hình thiết bị", "" }) { BackColor = Color.AliceBlue, Font = boldFont },
                    new ListViewItem(new[] { "Vi xử lý (CPU)", sysData.CpuName }),
                    new ListViewItem(new[] { "Số luồng xử lý", sysData.ProcCount }),
                    new ListViewItem(new[] { "Định danh thiết bị", sysData.UUID }),

                    new ListViewItem(new[] { "Khả năng lưu trữ", "" }) { BackColor = Color.AliceBlue, Font = boldFont },
                    new ListViewItem(new[] { $"Ổ đĩa cài đặt phần mềm [{sysData.RootDrive}]", sysData.ThongTinOChuaApp }),

                    new ListViewItem(new[] { "Phần mềm bổ trợ", "" }) { BackColor = Color.AliceBlue, Font = boldFont },
                    new ListViewItem(new[] { "Môi trường .NET", sysData.DotNetVer })
                };

                lvInfo.Items.AddRange(items);
            }

 
            private void DrawFlatProgressBar(Graphics g, Rectangle bounds, float percent, bool isAppMem)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (GraphicsPath pathBg = CreateRoundedRect(bounds, bounds.Height / 2))
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(235, 235, 235)))
                {
                    g.FillPath(bgBrush, pathBg);
                }

                if (percent <= 0) return;

                int fillWidth = (int)(bounds.Width * (Math.Min(percent, 100) / 100f));
                if (fillWidth < bounds.Height) fillWidth = bounds.Height;

                Rectangle fillRect = new Rectangle(bounds.X, bounds.Y, fillWidth, bounds.Height);

                Color barColor = isAppMem ? Color.FromArgb(0, 184, 148) : Color.FromArgb(9, 132, 227);
                if (percent > 80) barColor = Color.FromArgb(253, 203, 110);
                if (percent > 95) barColor = Color.FromArgb(214, 48, 49);

                using (GraphicsPath pathFill = CreateRoundedRect(fillRect, fillRect.Height / 2))
                using (SolidBrush fgBrush = new SolidBrush(barColor))
                {
                    g.FillPath(fgBrush, pathFill);
                }
            }

            private GraphicsPath CreateRoundedRect(Rectangle rect, int radius)
            {
                GraphicsPath path = new GraphicsPath();
                int d = radius * 2;
                path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                return path;
            }
            private async Task HardwareSafePollingLoop(CancellationToken token)
            {
                // 🌟 CHUẨN KỸ SƯ: Khởi tạo đối tượng Process ĐÚNG 1 LẦN duy nhất bên ngoài vòng lặp.
                // Việc này triệt tiêu hoàn toàn hiện tượng Rò rỉ System Handle (Handle Leak) của HĐH.
                using (Process currentAppProcess = Process.GetCurrentProcess())
                {
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            // --- Đo lường Tổng RAM Hệ thống ---
                            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
                            memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));

                            if (GlobalMemoryStatusEx(ref memStatus))
                            {
                                double totalRamGB = memStatus.ullTotalPhys / 1073741824.0;
                                double availableRamGB = memStatus.ullAvailPhys / 1073741824.0;
                                double usedRamGB = totalRamGB - availableRamGB;

                                _bgSysRamPercent = (float)((usedRamGB / totalRamGB) * 100);
                                _bgSysRamText = $"{usedRamGB:N1} / {totalRamGB:N1} GB";
                            }

                            // --- Đo lường RAM App (Sử dụng Refresh thay vì tạo mới) ---
                            currentAppProcess.Refresh();
                            double appRamMB = currentAppProcess.WorkingSet64 / 1048576.0;
                            _bgAppRamPercent = (float)Math.Min(appRamMB / 1024.0 * 100, 100);
                            _bgAppRamText = $"{appRamMB:N1} MB";
                        }
                        catch { /* Bỏ qua lỗi truy xuất nhất thời để không sập Polling */ }

                        try
                        {
                            await Task.Delay(2000, token);
                        }
                        catch (TaskCanceledException) { break; }
                    }
                }
            }
            private void UpdateTimer_Tick(object sender, EventArgs e)
            {
                lblSysRamPercent.Text = $"{(int)_bgSysRamPercent}%";
                lblSysRamDetail.Text = _bgSysRamText;
                lblSysRamPercent.ForeColor = _bgSysRamPercent > 85 ? Color.FromArgb(214, 48, 49) : Color.FromArgb(9, 132, 227);

                lblAppRamPercent.Text = $"{(int)_bgAppRamPercent}%";
                lblAppRamDetail.Text = _bgAppRamText;

                pnlSysRamBar.Invalidate();
                pnlAppRamBar.Invalidate();
            }
            private void BtnExport_Click(object sender, EventArgs e)
            {
                try
                {
                    using (SaveFileDialog sfd = new SaveFileDialog())
                    {
                        sfd.Filter = "Tệp báo cáo hệ thống (*.report)|*.report|Tệp văn bản (*.txt)|*.txt";
                        sfd.Title = "Xuất thông tin cấu hình";
                        sfd.FileName = $"Thông tin cấu hình máy tính cài đặt - SysInfo_{DateTime.Now:ddMMyyyy_HHmm}_report.txt";

                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine("BÁO CÁO THÔNG TIN CẤU HÌNH");
                            sb.AppendLine($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                            sb.AppendLine(new string('=', 50));

                            foreach (ListViewItem item in lvInfo.Items)
                            {
                                if (string.IsNullOrWhiteSpace(item.SubItems[1].Text))
                                    sb.AppendLine($"\n[{item.Text.ToUpper()}]");
                                else
                                    sb.AppendLine($"{item.Text,-25}: {item.SubItems[1].Text}");
                            }

                            File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);

                            try { Module_XuatNhapDuLieuThiDua.MoVaChonTepTrongExplorer(sfd.FileName); } catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể xuất báo cáo.\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            protected override void OnFormClosing(FormClosingEventArgs e)
            {
                try
                {
                    // Hủy vòng lặp một cách an toàn
                    if (_monitorCts != null && !_monitorCts.IsCancellationRequested)
                    {
                        _monitorCts.Cancel();
                        _monitorCts.Dispose();
                    }

                    if (updateTimer != null)
                    {
                        updateTimer.Stop();
                        updateTimer.Dispose();
                    }
                }
                catch { }

                base.OnFormClosing(e);
            }
        }
    }
}

