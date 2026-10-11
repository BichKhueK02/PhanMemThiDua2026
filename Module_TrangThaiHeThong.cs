//Yêu mèo cam
#nullable enable
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;
namespace PhanMemThiDua2026
{
    internal static class Module_TrangThaiHeThong
    {
        private static readonly ToolTip _sharedToolTip = new ToolTip {
                                                                        AutoPopDelay = 10000,
                                                                        InitialDelay = 500,
                                                                        ReshowDelay = 100,
                                                                        ShowAlways = true
                                                                    };
        private static string _cachedCpuName = string.Empty;
        private static readonly Color MauNen = Color.FromArgb(244, 247, 251);
        private static readonly Color MauThe = Color.White;
        private static readonly Color MauChu = Color.FromArgb(35, 47, 62);
        private static readonly Color MauPhu = Color.FromArgb(105, 119, 138);
        private static readonly Color MauXanh = Color.FromArgb(37, 99, 235);
        private static readonly Color MauXanhLa = Color.FromArgb(5, 150, 105);
        private static readonly Color MauVien = Color.FromArgb(225, 232, 241);
        private static string FontUI
        {
            get
            {
                try
                {
                    string tenFont = Module_HeThong.TenFontHeThong;
                    if (!string.IsNullOrWhiteSpace(tenFont))
                        return tenFont;
                }
                catch { }
                return "Segoe UI";
            }
        }
        public static void CapNhatStatusCSDL(StatusStrip status, ToolStripStatusLabel label)
        {
            if (status == null || label == null ||
                status.IsDisposed || label.IsDisposed)
                return;
            try
            {
                if (status.InvokeRequired)
                {
                    try
                    {
                        status.BeginInvoke(new Action(
                            () => CapNhatStatusCSDL(status, label)));
                    }
                    catch (ObjectDisposedException) { }
                    catch (InvalidOperationException) { }
                    return;
                }
                bool csdlSanSang =
                    Module_DanduongGPS.KiemTraTrangThaiSanSangCuaHeThongCSDL();
                DateTime thoiGianDangNhap = SessionInfo.ThoiGianDangNhap;
                string thoiGianStr = thoiGianDangNhap == default
                    ? "(chưa đăng nhập)"
                    : $"Truy cập lúc {thoiGianDangNhap:hh:mm tt}, ngày {thoiGianDangNhap:dd/M/yyyy}";
                string tenMay = Environment.MachineName;
                string tenUser = Environment.UserName;
                label.Text = csdlSanSang
                    ? $"Đang kết nối CSDL | User: {tenUser} | {thoiGianStr}"
                    : $"Mất kết nối | User: {tenUser} | {thoiGianStr}";
                label.ForeColor = csdlSanSang ? Color.DarkGreen : Color.DarkRed;
                status.BackColor = csdlSanSang
                    ? Color.FromArgb(220, 248, 198)
                    : Color.FromArgb(255, 224, 224);
                _sharedToolTip.SetToolTip(
                    status,
                    $"Máy tính: {tenMay}\n" +
                    $"Tài khoản Windows: {tenUser}\n" +
                    $"Phiên kết nối: {thoiGianStr}");
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
            catch (Exception ex)
            {
                Debug.WriteLine($"CapNhatStatusCSDL Error: {ex}");
            }
        }
        private static GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
        public static string LayUUIDMayTinh()
        {
            try
            {
                string rawSysInfo =
                    Environment.MachineName +
                    Environment.UserDomainName +
                    Environment.UserName +
                    Environment.ProcessorCount;
                using (SHA256 sha = SHA256.Create())
                {
                    byte[] hashBytes =
                        sha.ComputeHash(Encoding.UTF8.GetBytes(rawSysInfo));
                    return "SYS-" +
                        BitConverter.ToString(hashBytes)
                            .Replace("-", "")
                            .Substring(0, 16);
                }
            }
            catch
            {
                return $"SYS-{Environment.MachineName}";
            }
        }
        public static void MoFormNhungVaoPanel(Form formHienTai)
        {
            if (formHienTai == null || formHienTai.IsDisposed)
                return;
            Control? container = formHienTai.Parent;
            while (container != null &&
                   container is not Panel &&
                   container is not TabPage)
            {
                container = container.Parent;
            }
            if (container == null || container.IsDisposed)
            {
                Debug.WriteLine(
                    "MoFormNhungVaoPanel: Không tìm thấy Panel hoặc TabPage cha.");
                MessageBox.Show(
                    formHienTai,
                    "Không tìm thấy Panel hoặc TabPage để nhúng giao diện.\n" +
                    "Nếu đã có Panel đích, hãy gọi NhungFormVaoTabPage(panel).",
                    "Không thể hiển thị giao diện",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            NhungFormVaoTabPage(container);
        }
        public static void NhungFormVaoTabPage(Control targetContainer)
        {
            if (targetContainer == null ||
                targetContainer.IsDisposed ||
                targetContainer.Disposing)
                return;
            try
            {
                Form_TaskManagerMini? taskForm =
                    targetContainer.Controls
                        .OfType<Form_TaskManagerMini>()
                        .FirstOrDefault(f => !f.IsDisposed);
                if (taskForm == null)
                {
                    taskForm = new Form_TaskManagerMini
                    {
                        TopLevel = false,
                        FormBorderStyle = FormBorderStyle.None,
                        Dock = DockStyle.Fill,
                        ShowInTaskbar = false,
                        StartPosition = FormStartPosition.Manual
                    };
                    targetContainer.SuspendLayout();
                    try
                    {
                        targetContainer.Controls.Add(taskForm);
                        taskForm.Dock = DockStyle.Fill;
                        taskForm.Show();
                        taskForm.BringToFront();
                    }
                    finally
                    {
                        targetContainer.ResumeLayout(true);
                    }
                }
                else
                {
                    taskForm.Dock = DockStyle.Fill;
                    if (!ReferenceEquals(taskForm.Parent, targetContainer))
                    {
                        taskForm.Parent?.Controls.Remove(taskForm);
                        targetContainer.Controls.Add(taskForm);
                    }
                    if (!taskForm.Visible)
                        taskForm.Show();
                    taskForm.BringToFront();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NhungFormVaoTabPage Error: {ex}");
                MessageBox.Show(
                    targetContainer.FindForm(),
                    "Không thể hiển thị giao diện giám sát hệ thống.\n\n" +
                    ex.GetType().Name + ": " + ex.Message,
                    "Lỗi giao diện",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private static string LayUngDungDocExcel()
        {
            try
            {
                using (var key =
                    Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(".xlsx"))
                {
                    if (key != null)
                    {
                        string? progId = key.GetValue("")?.ToString();
                        if (!string.IsNullOrEmpty(progId))
                        {
                            if (progId.IndexOf(
                                "Excel", StringComparison.OrdinalIgnoreCase) >= 0)
                                return "Microsoft Office - Microsoft Excel";
                            if (progId.IndexOf(
                                "WPS", StringComparison.OrdinalIgnoreCase) >= 0)
                                return "WPS Office";
                            if (progId.IndexOf(
                                    "opendocument.spreadsheet",
                                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                                progId.IndexOf(
                                    "LibreOffice",
                                    StringComparison.OrdinalIgnoreCase) >= 0)
                                return "LibreOffice / OpenOffice";
                            using (var progIdKey =
                                Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(progId))
                            {
                                string? appName = progIdKey?.GetValue("")?.ToString();
                                if (!string.IsNullOrWhiteSpace(appName))
                                    return appName;
                            }
                            return progId;
                        }
                    }
                }
                return "Chưa cài đặt phần mềm đọc Excel";
            }
            catch
            {
                return "Không thể xác định (Khóa quyền truy cập Registry)";
            }
        }
        private static string GetCpuName()
        {
            if (!string.IsNullOrWhiteSpace(_cachedCpuName))
                return _cachedCpuName;
            try
            {
                _cachedCpuName =
                    Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")
                    ?? "Unknown CPU";
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
            try { return RuntimeInformation.OSDescription; }
            catch { return Environment.OSVersion.ToString(); }
        }
        private static string LayTrangThaiUAC()
        {
            try
            {
                using (var identity =
                    System.Security.Principal.WindowsIdentity.GetCurrent())
                {
                    var principal =
                        new System.Security.Principal.WindowsPrincipal(identity);
                    return principal.IsInRole(
                        System.Security.Principal.WindowsBuiltInRole.Administrator)
                        ? "Đang chạy với quyền Cao nhất (Admin)"
                        : "Tiêu chuẩn (User thường)";
                }
            }
            catch
            {
                return "Không xác định";
            }
        }
        public static string LayLoaiMayTinh()
        {
            try
            {
                PowerStatus powerStatus = SystemInformation.PowerStatus;
                return powerStatus.BatteryChargeStatus ==
                       BatteryChargeStatus.NoSystemBattery
                    ? "Máy bàn"
                    : "Laptop";
            }
            catch
            {
                return "Không xác định";
            }
        }
        private class SmoothPanel : Panel
        {
            public SmoothPanel()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw,
                    true);
            }
        }
        private sealed class SmoothListView : ListView
        {
            public SmoothListView()
            {
                DoubleBuffered = true;
                SetStyle(
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.ResizeRedraw,
                    true);
            }
        }
        // 🌟 BIỂU ĐỒ DONUT SỐ MÀU TÍM
        // 🌟 BIỂU ĐỒ DONUT CÓ HIỆU ỨNG QUÉT ĐỘNG

        // 🌟 BIỂU ĐỒ DONUT CÓ VỆT QUÉT CHẠY TRONG PHẠM VI VIỀN XANH
         // 🌟 BIỂU ĐỒ DONUT HỖ TRỢ CẢ 2 CHẾ ĐỘ QUÉT (QUÉT TRONG PHẦN TRĂM HOẶC QUÉT 360 ĐỘ)
        private sealed class RamDonutPanel : SmoothPanel
        {
            private float _percent;
            private Color _accentColor = MauXanh;
            private float _scanOffset = 0f;
            private System.Windows.Forms.Timer? _animTimer;

            // Thuộc tính để bật/tắt chế độ chạy full 360 độ
            public bool FullCircleScan { get; set; } = false;

            public RamDonutPanel()
            {
                _animTimer = new System.Windows.Forms.Timer { Interval = 30 };
                _animTimer.Tick += (s, e) =>
                {
                    if (!IsDisposed && Visible)
                    {
                        if (FullCircleScan)
                        {
                            // Chạy liên tục 360 độ quanh toàn bộ vòng tròn
                            _scanOffset = (_scanOffset + 5f) % 360f;
                        }
                        else
                        {
                            // Chạy co giãn trong giới hạn phần trăm RAM hiện tại
                            float sweepAngle = (_percent / 100f) * 360f;
                            if (sweepAngle > 5f)
                            {
                                _scanOffset = (_scanOffset + 2.5f) % sweepAngle;
                            }
                            else
                            {
                                _scanOffset = 0f;
                            }
                        }
                        Invalidate();
                    }
                };
                _animTimer.Start();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _animTimer?.Stop();
                    _animTimer?.Dispose();
                }
                base.Dispose(disposing);
            }

            public float Percent
            {
                get => _percent;
                set { _percent = Math.Clamp(value, 0, 100); Invalidate(); }
            }

            public Color AccentColor
            {
                get => _accentColor;
                set { _accentColor = value; Invalidate(); }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                int w = ClientSize.Width;
                int h = ClientSize.Height;
                if (w < 20 || h < 20) return;

                int size = (int)(Math.Min(w, h) * 0.75f);
                if (size < 20) size = 20;

                int x = (w - size) / 2;
                int y = (h - size) / 2;
                Rectangle rect = new Rectangle(x, y, size, size);

                int strokeWidth = Math.Max(3, size / 16);
                Rectangle arcRect = new Rectangle(
                    rect.X + strokeWidth / 2,
                    rect.Y + strokeWidth / 2,
                    rect.Width - strokeWidth,
                    rect.Height - strokeWidth);

                // 1. Vẽ vòng nền xám nhạt
                using (var pen = new Pen(Color.FromArgb(226, 232, 240), strokeWidth))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawEllipse(pen, arcRect);
                }

                // 2. Vẽ phần trăm RAM chính (viền xanh)
                float sweepAngle = (_percent / 100f) * 360f;
                if (sweepAngle > 0)
                {
                    using (var pen = new Pen(_accentColor, strokeWidth))
                    {
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                        g.DrawArc(pen, arcRect, -90, sweepAngle);
                    }
                }

                // 3. Vẽ hiệu ứng vệt quét tùy theo chế độ
                if (FullCircleScan)
                {
                    // Chế độ chạy quét 360 độ liên tục (Dành cho RAM Ứng dụng)
                    using (var scanPen = new Pen(Color.FromArgb(220, 255, 255, 255), Math.Max(2, strokeWidth)))
                    {
                        scanPen.StartCap = LineCap.Round;
                        scanPen.EndCap = LineCap.Round;
                        g.DrawArc(scanPen, arcRect, -90f + _scanOffset, 45); // Vệt sáng dài 45 độ quét vòng quanh
                    }
                }
                else
                {
                    // Chế độ chạy co giãn trong phạm vi viền xanh (Dành cho RAM Toàn hệ thống)
                    if (sweepAngle > 15f)
                    {
                        float scanLength = Math.Min(18f, sweepAngle * 0.4f);
                        float currentScanAngle = -90f + _scanOffset;

                        using (var scanPen = new Pen(Color.FromArgb(220, 255, 255, 255), Math.Max(2, strokeWidth)))
                        {
                            scanPen.StartCap = LineCap.Round;
                            scanPen.EndCap = LineCap.Round;
                            g.DrawArc(scanPen, arcRect, currentScanAngle, scanLength);
                        }
                    }
                }

                // 4. Vẽ chữ số phần trăm ở giữa
                string text = $"{_percent:0}%";
                using (var font = new Font(FontUI, Math.Max(8f, size / 6.0f), FontStyle.Bold))
                using (var brush = new SolidBrush(Color.FromArgb(165, 156, 228)))
                {
                    SizeF sz = g.MeasureString(text, font);
                    g.DrawString(text, font, brush, rect.X + (rect.Width - sz.Width) / 2f, rect.Y + (rect.Height - sz.Height) / 2f);
                }
            }
        }

        private sealed class Form_TaskManagerMini : Form
        {
            private volatile float _bgSysRamPercent;
            private string _bgSysRamText = "0 / 0 GB";
            private volatile float _bgAppRamPercent;
            private string _bgAppRamText = "0 MB";
            private CancellationTokenSource? _monitorCts;
            private Task? _monitorTask;
            private System.Windows.Forms.Timer? updateTimer;
            private bool _daKhoiTao;
            private bool _dangDong;
            private RamDonutPanel chartSysRam = null!;
            private RamDonutPanel chartAppRam = null!;
            private SmoothLabel lblSysRamDetail = null!;
            private SmoothLabel lblAppRamDetail = null!;
            private Label lblSystemStatus = null!;
            private SmoothListView lvInfo = null!;
            private SplitContainer split = null!;
            private Button btnExport = null!;
            public Form_TaskManagerMini()
            {
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw,
                    true);
                InitializeModernUI_SplitLayout();
                Load += Form_TaskManagerMini_Load;
            }
            private async void Form_TaskManagerMini_Load(object? sender, EventArgs e)
            {
                if (_daKhoiTao || IsDisposed)
                    return;
                _daKhoiTao = true;
                try
                {
                    await LoadListViewDataAsync();
                    if (IsDisposed || !IsHandleCreated)
                        return;
                    _monitorCts = new CancellationTokenSource();
                    CancellationToken token = _monitorCts.Token;
                    _monitorTask = Task.Run(
                        () => HardwareSafePollingLoop(token), token);
                    updateTimer = new System.Windows.Forms.Timer
                    {
                        Interval = 1000
                    };
                    updateTimer.Tick += UpdateTimer_Tick;
                    updateTimer.Start();
                    lblSystemStatus.Text = "● Đang giám sát tài nguyên";
                    lblSystemStatus.ForeColor = Color.FromArgb(22, 128, 72);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Khởi tạo giám sát hệ thống: {ex}");
                    if (!IsDisposed && lblSystemStatus != null)
                    {
                        lblSystemStatus.Text = "● Chưa thể khởi tạo giám sát";
                        lblSystemStatus.ForeColor = Color.Firebrick;
                    }
                }
            }
            private void InitializeModernUI_SplitLayout()
            {
                SuspendLayout();
                Text = "Trung tâm giám sát hệ thống";
                Size = new Size(1200, 720);
                MinimumSize = new Size(650, 400);
                StartPosition = FormStartPosition.Manual;
                FormBorderStyle = FormBorderStyle.None;
                ShowIcon = false;
                ShowInTaskbar = false;
                BackColor = MauNen;
                ForeColor = MauChu;
                Font = new Font(FontUI, 9.5F);
                AutoScaleMode = AutoScaleMode.Font;
                var root = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 3,
                    BackColor = MauNen,
                    Padding = new Padding(16, 12, 16, 10),
                    Margin = Padding.Empty
                };
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
                var header = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = MauNen,
                    Margin = Padding.Empty
                };
                var lblTitle = new Label
                {
                    Text = "Tổng quan hệ thống",
                    Font = new Font(FontUI, 16F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(140, 140, 215),
                    AutoSize = true,
                    Location = new Point(0, 0)
                };
                var lblSubtitle = new Label
                {
                    Text = "Theo dõi tài nguyên và cấu hình thiết bị",
                    Font = new Font(FontUI, 8F),
                    ForeColor = MauPhu,
                    AutoSize = true,
                    Location = new Point(2, 37)
                };
                lblSystemStatus = new Label
                {
                    Text = "● Đang khởi tạo",
                    AutoSize = false,
                    Width = 250,
                    Height = 28,
                    TextAlign = ContentAlignment.MiddleRight,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    Font = new Font(FontUI, 9F, FontStyle.Bold),
                    ForeColor = MauPhu
                };
                header.Controls.AddRange(new Control[]
                {
                    lblTitle, lblSubtitle, lblSystemStatus
                });
                header.Resize += (s, e) =>
                {
                    lblSystemStatus.Location = new Point(
                        Math.Max(0, header.ClientSize.Width -
                                   lblSystemStatus.Width - 2), 8);
                };
                split = new SplitContainer
                {
                    Name = "splitSystemOverview",
                    Dock = DockStyle.Fill,
                    Orientation = Orientation.Vertical,
                    SplitterWidth = 7,
                    FixedPanel = FixedPanel.Panel1,
                    IsSplitterFixed = false,
                    Panel1MinSize = 0,
                    Panel2MinSize = 0,
                    BackColor = MauNen,
                    Margin = Padding.Empty,
                    TabStop = false
                };
                split.Panel1.BackColor = MauNen;
                split.Panel2.BackColor = MauNen;
                var left = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 3,
                    BackColor = MauNen,
                    Padding = new Padding(0, 0, 10, 0),
                    Margin = Padding.Empty
                };
                left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                left.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
                left.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
                left.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
                var lblResourceTitle = new Label
                {
                    Text = "TÀI NGUYÊN",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font(FontUI, 9F, FontStyle.Bold),
                    ForeColor = MauPhu
                };
                left.Controls.Add(lblResourceTitle, 0, 0);
                Panel cardSys = CreateRamCardDonut(
                    "RAM TOÀN HỆ THỐNG",
                    out chartSysRam,
                    out lblSysRamDetail,
                    false);
                Panel cardApp = CreateRamCardDonut(
                    "RAM ỨNG DỤNG",
                    out chartAppRam,
                    out lblAppRamDetail,
                    true);
                left.Controls.Add(cardSys, 0, 1);
                left.Controls.Add(cardApp, 0, 2);
                split.Panel1.Controls.Add(left);
                var right = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2,
                    BackColor = MauNen,
                    Padding = new Padding(4, 0, 0, 0),
                    Margin = Padding.Empty
                };
                right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                right.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
                right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                var lblInfoTitle = new Label
                {
                    Text = "CHI TIẾT CẤU HÌNH THIẾT BỊ",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font(FontUI, 9F, FontStyle.Bold),
                    ForeColor = MauPhu
                };
                var infoCard = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = MauThe,
                    Padding = new Padding(10),
                    Margin = Padding.Empty
                };
                infoCard.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path = GetRoundedRectanglePath(new Rectangle(0, 0, infoCard.Width - 1, infoCard.Height - 1), 8);
                    using var brush = new SolidBrush(infoCard.BackColor);
                    using var pen = new Pen(MauVien, 1);
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                };
                lvInfo = new SmoothListView
                {
                    Dock = DockStyle.Fill,
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines = true,
                    HeaderStyle = ColumnHeaderStyle.Nonclickable,
                    BorderStyle = BorderStyle.None,
                    Font = new Font(FontUI, 9.5F),
                    BackColor = MauThe,
                    ForeColor = MauChu,
                    HideSelection = false,
                    MultiSelect = false,
                    UseCompatibleStateImageBehavior = false,
                    OwnerDraw = true
                };
                lvInfo.Columns.Add("Thành phần", 220);
                lvInfo.Columns.Add("Thông tin chi tiết", 440);
                lvInfo.DrawColumnHeader += (s, e) => e.DrawDefault = true;
                lvInfo.DrawSubItem += LvInfo_DrawSubItem;
                lvInfo.Resize += (s, e) =>
                {
                    if (lvInfo.Columns.Count < 2)
                        return;
                    int firstWidth = Math.Min(
                        240, Math.Max(150, lvInfo.ClientSize.Width / 3));
                    lvInfo.Columns[0].Width = firstWidth;
                    lvInfo.Columns[1].Width = Math.Max(
                        80, lvInfo.ClientSize.Width - firstWidth - 4);
                };
                infoCard.Controls.Add(lvInfo);
                right.Controls.Add(lblInfoTitle, 0, 0);
                right.Controls.Add(infoCard, 0, 1);
                split.Panel2.Controls.Add(right);
                var footer = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = MauNen,
                    Margin = Padding.Empty
                };
                footer.Paint += (s, e) =>
                {
                    using var pen = new Pen(MauVien);
                    e.Graphics.DrawLine(pen, 0, 0, footer.ClientSize.Width, 0);
                };
                var lblFooter = new Label
                {
                    Text = "Trình giám sát tài nguyên",
                    AutoSize = false,
                    Dock = DockStyle.Left,
                    Width = 440,
                    TextAlign = ContentAlignment.MiddleLeft,
                    ForeColor = MauPhu,
                    Font = new Font(FontUI, 8.5F, FontStyle.Bold)
                };
                btnExport = new Button
                {
                    Text = "Xuất báo cáo",
                    Size = new Size(155, 36),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    BackColor = MauXanh,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font(FontUI, 9.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    UseVisualStyleBackColor = false
                };
                btnExport.FlatAppearance.BorderSize = 0;
                btnExport.FlatAppearance.MouseOverBackColor =
                    Color.FromArgb(29, 78, 216);
                btnExport.FlatAppearance.MouseDownBackColor =
                    Color.FromArgb(30, 64, 175);
                btnExport.Click += BtnExport_Click;
                footer.Controls.Add(lblFooter);
                footer.Controls.Add(btnExport);
                footer.Resize += (s, e) =>
                {
                    btnExport.Location = new Point(
                        Math.Max(0, footer.ClientSize.Width -
                                    btnExport.Width - 2),
                        Math.Max(0, (footer.ClientSize.Height -
                                   btnExport.Height) / 2));
                };
                root.Controls.Add(header, 0, 0);
                root.Controls.Add(split, 0, 1);
                root.Controls.Add(footer, 0, 2);
                Controls.Add(root);
                Shown += (s, e) => DatKhoangChiaAnToan();
                SizeChanged += (s, e) => DatKhoangChiaAnToan();
                ResumeLayout(true);
            }
            private void LvInfo_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
            {
                if (e.Item == null || e.Graphics == null) return;
                bool isGroupHeader = e.Item.Tag is bool b && b;
                if (isGroupHeader)
                {
                    using var bgBrush = new SolidBrush(Color.FromArgb(241, 245, 249));
                    e.Graphics.FillRectangle(bgBrush, e.Bounds);
                    using var pen = new Pen(Color.FromArgb(226, 232, 240));
                    e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                    if (e.ColumnIndex == 0)
                    {
                        using var fontHeader = new Font(FontUI, 9.5F, FontStyle.Bold);
                        using var brushHeader = new SolidBrush(Color.FromArgb(30, 41, 59));
                        var textRect = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
                        e.Graphics.DrawString(e.Item.Text, fontHeader, brushHeader, textRect, new StringFormat { LineAlignment = StringAlignment.Center });
                    }
                }
                else
                {
                    e.DrawDefault = true;
                }
            }
            private Panel CreateRamCardDonut(
                string title,
                out RamDonutPanel donutPanel,
                out SmoothLabel detailLabel,
                bool isApp)
            {
                var card = new SmoothPanel
                {
                    Dock = DockStyle.Fill,
                    BackColor = MauThe,
                    Margin = new Padding(0, 4, 0, 8),
                    Padding = new Padding(8, 6, 8, 6)
                };
                card.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path = GetRoundedRectanglePath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
                    using var brush = new SolidBrush(card.BackColor);
                    using var pen = new Pen(MauVien, 1);
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                };

                var lblTitle = new Label
                {
                    Text = title,
                    Dock = DockStyle.Top,
                    Height = 22,
                    Font = new Font(FontUI, 8.5F, FontStyle.Bold),
                    ForeColor = MauPhu,
                    BackColor = Color.Transparent,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                detailLabel = new SmoothLabel
                {
                    Text = "Đang thu thập dữ liệu...",
                    Dock = DockStyle.Bottom,
                    Height = 22,
                    Font = new Font(FontUI, 8.5F),
                    ForeColor = MauPhu,
                    BackColor = Color.Transparent,
                    TextAlign = ContentAlignment.MiddleCenter
                };

                donutPanel = new RamDonutPanel
                {
                    Dock = DockStyle.Fill,
                    AccentColor = isApp ? MauXanhLa : MauXanh,
                    Percent = 0,
                    FullCircleScan = isApp // Nếu là RAM ứng dụng (isApp = true), vệt quét sẽ chạy trọn vẹn 360 độ
                };

                card.Controls.Add(donutPanel);
                card.Controls.Add(detailLabel);
                card.Controls.Add(lblTitle);

                return card;
            }
            private void DatKhoangChiaAnToan()
            {
                if (split == null || split.IsDisposed ||
                    !split.IsHandleCreated || split.ClientSize.Width <= 0)
                    return;
                try
                {
                    int minLeft = Math.Min(260, split.ClientSize.Width / 3);
                    int targetDistance = Math.Max(320, minLeft);
                    if (targetDistance < split.Panel1MinSize)
                        targetDistance = split.Panel1MinSize;
                    int maxAllowed = split.ClientSize.Width - split.Panel2MinSize - split.SplitterWidth;
                    if (targetDistance > maxAllowed)
                        targetDistance = Math.Max(split.Panel1MinSize, maxAllowed);
                    if (targetDistance > 0 && targetDistance < split.ClientSize.Width)
                    {
                        split.SplitterDistance = targetDistance;
                    }
                }
                catch { }
            }
            private async Task LoadListViewDataAsync()
            {
                lvInfo.Items.Clear();
                var listItems = await Task.Run(() =>
                {
                    var items = new System.Collections.Generic.List<ListViewItem>();
                    void AddGroupHeader(string groupName)
                    {
                        var lvi = new ListViewItem(groupName) { Tag = true };
                        items.Add(lvi);
                    }
                    void AddRow(string title, string value)
                    {
                        var lvi = new ListViewItem(title) { Tag = false };
                        lvi.SubItems.Add(string.IsNullOrWhiteSpace(value) ? "Không xác định" : value);
                        items.Add(lvi);
                    }
                    // 1. Môi trường Windows
                    AddGroupHeader("Môi trường Windows");
                    AddRow("Hệ điều hành", LayWindowsVersionChiTiet());
                    AddRow("Kiến trúc OS", LayLoaiMayTinh());
                    AddRow("Kiến trúc Ứng dụng", Environment.Is64BitProcess ? "64-bit (Tối ưu)" : "32-bit");
                    AddRow("Phân quyền ứng dụng", LayTrangThaiUAC());
                    // 2. Cấu hình hiển thị & Vùng
                    AddGroupHeader("Cấu hình hiển thị & Vùng");
                    try
                    {
                        var screen = Screen.PrimaryScreen;
                        if (screen != null)
                        {
                            AddRow("Độ phân giải màn hình", $"{screen.Bounds.Width} x {screen.Bounds.Height}");
                        }
                    }
                    catch { }
                    try
                    {
                        using var g = CreateGraphics();
                        AddRow("Tỷ lệ thu phóng (Scaling)", $"{g.DpiX / 96d * 100d:0}%");
                    }
                    catch { }
                    AddRow("Định dạng vùng (Culture)", $"{System.Globalization.CultureInfo.CurrentCulture.DisplayName} (dd/MM/yyyy)");
                    // 3. Cấu hình thiết bị
                    AddGroupHeader("Cấu hình thiết bị");
                    AddRow("Vi xử lý (CPU)", GetCpuName());
                    AddRow("Số luồng xử lý", $"{Environment.ProcessorCount} Luồng");
                    AddRow("Định danh thiết bị", LayUUIDMayTinh());
                    // 4. Khả năng lưu trữ
                    AddGroupHeader("Khả năng lưu trữ");
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        try
                        {
                            if (!drive.IsReady) continue;
                            double totalGB = drive.TotalSize / 1024d / 1024d / 1024d;
                            double freeGB = drive.AvailableFreeSpace / 1024d / 1024d / 1024d;
                            AddRow($"Ổ đĩa cài đặt phần mềm [{drive.Name.TrimEnd('\\')}]", $"Trống {freeGB:0} GB / Tổng {totalGB:0} GB ({drive.DriveFormat})");
                        }
                        catch { }
                    }
                    // 5. Môi trường triển khai
                    AddGroupHeader("Môi trường triển khai");
                    AddRow("Môi trường .NET", $"{LayDotNetRuntime()} (Phát hành bởi Microsoft)");
                    // 6. Phần mềm hỗ trợ
                    AddGroupHeader("Phần mềm hỗ trợ");
                    AddRow("Ứng dụng xử lý Excel", LayUngDungDocExcel());
                    return items;
                });
                if (IsDisposed || !IsHandleCreated) return;
                lvInfo.BeginUpdate();
                lvInfo.Items.AddRange(listItems.ToArray());
                lvInfo.EndUpdate();
            }
            private void HardwareSafePollingLoop(CancellationToken token)
            {
                var computerInfo = new ComputerInfo();
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        ulong total = computerInfo.TotalPhysicalMemory;
                        ulong available = computerInfo.AvailablePhysicalMemory;
                        double totalGB = total / 1024d / 1024d / 1024d;
                        double freeGB = available / 1024d / 1024d / 1024d;
                        if (totalGB > 0)
                        {
                            freeGB = Math.Clamp(freeGB, 0, totalGB);
                            double usedGB = Math.Clamp(totalGB - freeGB, 0, totalGB);
                            float sysPercent = (float)(usedGB / totalGB * 100f);
                            _bgSysRamPercent = sysPercent;
                            _bgSysRamText = $"{usedGB:0.00} GB / {totalGB:0.00} GB";
                            long appPrivateBytes = GC.GetTotalMemory(false);
                            try
                            {
                                using var curProc = Process.GetCurrentProcess();
                                appPrivateBytes = curProc.WorkingSet64;
                            }
                            catch { }
                            double appMB = appPrivateBytes / 1024d / 1024d;
                            float appPercent = (float)Math.Clamp(appMB / (totalGB * 1024d) * 100f, 0, 100f);
                            _bgAppRamPercent = appPercent;
                            _bgAppRamText = appMB >= 1024
                                ? $"{appMB / 1024d:0.00} GB"
                                : $"{appMB:0.0} MB";
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch { }
                    try
                    {
                        Thread.Sleep(1000);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            private void UpdateTimer_Tick(object? sender, EventArgs e)
            {
                if (_dangDong || IsDisposed || !IsHandleCreated)
                    return;
                try
                {
                    chartSysRam.Percent = _bgSysRamPercent;
                    lblSysRamDetail.Text = _bgSysRamText;
                    chartAppRam.Percent = _bgAppRamPercent;
                    lblAppRamDetail.Text = $"Đang sử dụng: {_bgAppRamText}";
                }
                catch { }
            }
            private void BtnExport_Click(object? sender, EventArgs e)
            {
                try
                {
                    using var sfd = new SaveFileDialog
                    {
                        Title = "Xuất báo cáo trạng thái hệ thống",
                        Filter = "Tệp văn bản (*.txt)|*.txt",
                        FileName = $"BaoCaoHeThong_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                        OverwritePrompt = true
                    };
                    if (sfd.ShowDialog(this) != DialogResult.OK)
                        return;
                    var sb = new StringBuilder();
                    sb.AppendLine("BÁO CÁO TRẠNG THÁI VÀ CẤU HÌNH HỆ THỐNG");
                    sb.AppendLine($"Thời gian xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                    sb.AppendLine(new string('=', 60));
                    sb.AppendLine($"RAM Toàn hệ thống: {_bgSysRamText} ({_bgSysRamPercent:0.0}%)");
                    sb.AppendLine($"RAM Ứng dụng: {_bgAppRamText} ({_bgAppRamPercent:0.0}%)");
                    sb.AppendLine(new string('-', 60));
                    foreach (ListViewItem item in lvInfo.Items)
                    {
                        bool isHeader = item.Tag is bool b && b;
                        if (isHeader)
                        {
                            sb.AppendLine();
                            sb.AppendLine($"[{item.Text}]");
                        }
                        else
                        {
                            string val = item.SubItems.Count > 1 ? item.SubItems[1].Text : "";
                            sb.AppendLine($"  {item.Text,-25}: {val}");
                        }
                    }
                    File.WriteAllText(sfd.FileName, sb.ToString(), new UTF8Encoding(true));
                    MessageBox.Show(
                        this,
                        "Xuất báo cáo hệ thống thành công!",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        this,
                        $"Không thể xuất báo cáo.\nChi tiết lỗi: {ex.Message}",
                        "Lỗi",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            protected override void OnFormClosed(FormClosedEventArgs e)
            {
                _dangDong = true;
                try
                {
                    updateTimer?.Stop();
                    updateTimer?.Dispose();
                    _monitorCts?.Cancel();
                    _monitorCts?.Dispose();
                }
                catch { }
                base.OnFormClosed(e);
            }
        }
        private sealed class SmoothLabel : Label
        {
            public SmoothLabel()
            {
                SetStyle(
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.ResizeRedraw,
                    true);
                UpdateStyles();
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                using var bgBrush = new SolidBrush(BackColor);
                e.Graphics.FillRectangle(bgBrush, ClientRectangle);
                using var textBrush = new SolidBrush(ForeColor);
                var sf = new StringFormat
                {
                    Alignment = GetStringAlignment(TextAlign),
                    LineAlignment = GetStringLineAlignment(TextAlign)
                };
                var rect = new Rectangle(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);
                e.Graphics.DrawString(Text, Font, textBrush, rect, sf);
            }
            private static StringAlignment GetStringAlignment(ContentAlignment align) => align switch
            {
                ContentAlignment.TopLeft or ContentAlignment.MiddleLeft or ContentAlignment.BottomLeft => StringAlignment.Near,
                ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter => StringAlignment.Center,
                _ => StringAlignment.Far,
            };
            private static StringAlignment GetStringLineAlignment(ContentAlignment align) => align switch
            {
                ContentAlignment.TopLeft or ContentAlignment.TopCenter or ContentAlignment.TopRight => StringAlignment.Near,
                ContentAlignment.MiddleCenter or ContentAlignment.MiddleLeft or ContentAlignment.MiddleRight => StringAlignment.Center,
                _ => StringAlignment.Far, // Thay Bottom thành Far ở đây
            };
        }
    }
}