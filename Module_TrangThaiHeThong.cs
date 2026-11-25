//Yêu mèo cam
#nullable enable
using Microsoft.VisualBasic.Devices;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
namespace PhanMemThiDua2026
{
    internal static class Module_TrangThaiHeThong
    {
        private static readonly ToolTip _sharedToolTip = new ToolTip
        {
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
                return Module_HeThong.TenFontHeThong;
            }
        }
        // Thanh trạng thái đã từng ghi (Form4 dựng sẵn TRƯỚC khi đăng nhập => nhãn chưa có giờ truy cập).
        // Đăng nhập xong (SessionInfo.ThoiGianDangNhap đổi) thì LamMoiTatCa() ghi lại các nhãn này.
        private static readonly List<(WeakReference<StatusStrip> Status, WeakReference<ToolStripStatusLabel> Label)> _daGhi = new();
        public static void LamMoiTatCa()
        {
            List<(StatusStrip, ToolStripStatusLabel)> ds = new();
            lock (_daGhi)
            {
                _daGhi.RemoveAll(x => !x.Status.TryGetTarget(out var s) || s.IsDisposed || !x.Label.TryGetTarget(out _));
                foreach (var x in _daGhi)
                    if (x.Status.TryGetTarget(out var s) && x.Label.TryGetTarget(out var l)) ds.Add((s, l));
            }
            foreach (var (s, l) in ds)
            {
                if (!s.IsHandleCreated) continue;   // chưa có handle: lần ghi đầu tiên (khi hiện) sẽ tự đúng
                CapNhatStatusCSDL(s, l);
            }
        }
        public static void CapNhatStatusCSDL(StatusStrip status, ToolStripStatusLabel label)
        {
            if (status == null || label == null ||
                status.IsDisposed || label.IsDisposed)
                return;
            lock (_daGhi)
            {
                bool daCo = _daGhi.Exists(x => x.Status.TryGetTarget(out var s) && ReferenceEquals(s, status)
                                              && x.Label.TryGetTarget(out var l) && ReferenceEquals(l, label));
                if (!daCo) _daGhi.Add((new WeakReference<StatusStrip>(status), new WeakReference<ToolStripStatusLabel>(label)));
            }
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
                // Chưa đăng nhập: bỏ hẳn phần thời gian (không ghi "(chưa đăng nhập)")
                string thoiGianStr = thoiGianDangNhap == default
                    ? ""
                    : $"Truy cập lúc {thoiGianDangNhap:hh:mm tt}, ngày {thoiGianDangNhap:dd/M/yyyy}";
                string phanThoiGian = thoiGianStr.Length > 0 ? " | " + thoiGianStr : "";
                string tenMay = Environment.MachineName;
                string tenUser = Environment.UserName;
                label.Text = csdlSanSang
                    ? $"Đang kết nối CSDL | User: {tenUser}{phanThoiGian}"
                    : $"Mất kết nối | User: {tenUser}{phanThoiGian}";
                label.ForeColor = csdlSanSang ? Color.DarkGreen : Color.DarkRed;
                status.BackColor = csdlSanSang
                    ? Color.FromArgb(220, 248, 198)
                    : Color.FromArgb(255, 224, 224);
                _sharedToolTip.SetToolTip(
                    status,
                    $"Máy tính: {tenMay}\n" +
                    $"Tài khoản Windows: {tenUser}" +
                    (thoiGianStr.Length > 0 ? "\nPhiên kết nối: " + thoiGianStr : ""));
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
        // Pha trộn màu a -> b theo tỷ lệ t (0..1), giữ nguyên độ trong suốt của a
        private static Color PhaMau(Color a, Color b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return Color.FromArgb(
                a.A,
                (int)Math.Round(a.R + (b.R - a.R) * t),
                (int)Math.Round(a.G + (b.G - a.G) * t),
                (int)Math.Round(a.B + (b.B - a.B) * t));
        }
        // Vẽ "thẻ" kiểu web: nền phẳng, bóng đổ mềm phía dưới, bo góc 14px, viền mảnh.
        // Dùng chung cho 2 thẻ RAM và thẻ danh sách cấu hình (gọi trong Paint của thẻ).
        private static void VeTheWeb(Graphics g, int w, int h)
        {
            if (w < 24 || h < 30) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var nen = new SolidBrush(MauNen))
                g.FillRectangle(nen, 0, 0, w, h);
            var the = new Rectangle(3, 2, w - 7, h - 9);
            for (int k = 3; k >= 1; k--)
            {
                var rs = new Rectangle(the.X - k, the.Y - k + 2, the.Width + 2 * k, the.Height + 2 * k);
                using (var duongBong = GetRoundedRectanglePath(rs, 14 + k))
                using (var bong = new SolidBrush(Color.FromArgb(12 - 2 * k, 15, 23, 42)))
                    g.FillPath(bong, duongBong);
            }
            using (var duongThe = GetRoundedRectanglePath(the, 14))
            using (var nenThe = new SolidBrush(MauThe))
            using (var vien = new Pen(MauVien, 1f))
            {
                g.FillPath(nenThe, duongThe);
                g.DrawPath(vien, duongThe);
            }
        }
        // Ẩn thanh cuộn ngang của ListView (cột đã được co vừa khung nên không cần)
        [DllImport("user32.dll")]
        private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);
        private const int SB_HORZ = 0;
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
                // Chỉ bật DoubleBuffered (WinForms tự map sang LVS_EX_DOUBLEBUFFER).
                // KHÔNG SetStyle(AllPaintingInWmPaint/UserPaint...) cho ListView: control gốc Win32
                // sẽ bỏ qua bước vẽ nền/dòng -> dữ liệu bị ẩn, chỉ hiện khi người dùng bấm vào.
                DoubleBuffered = true;
            }
        }
        // 🌟 BIỂU ĐỒ DONUT 3D: khối vòng nhìn nghiêng từ trên xuống, có độ dày, đổ bóng xuống mặt phẳng.
        //    - Phần đã dùng (cung màu) nổi cao hơn phần nền xám; có thành trước, thành trong, mặt cắt đầu cung.
        //    - Thân khối (bóng, thành, mặt trên) vẽ 1 lần vào bộ đệm Bitmap, chỉ dựng lại khi đổi %, kích thước, màu.
        //    - Mỗi nhịp timer chỉ dán bộ đệm + vẽ vệt sáng quét + chữ số => nhẹ, không giật.
        //    - FullCircleScan = true : vệt sáng chạy trọn 360 độ (RAM ứng dụng); false : chạy trong phạm vi cung màu.
        private sealed class RamDonutPanel : SmoothPanel
        {
            private const float DoNghieng = 0.56f;   // RyO / RxO : độ nghiêng khi nhìn từ trên xuống
            private const float TiLeLo = 0.60f;      // bán kính lỗ / bán kính ngoài
            private const float TiLeCao = 0.15f;     // chiều cao phần nổi / RxO
            private const float BuocGoc = 3f;        // độ mịn của đa giác (độ)
            // Đổ bóng trụ: tối ở 2 mép, sáng nhẹ ở khoảng 40% chiều ngang (ánh sáng từ phía trên-trái)
            private static readonly ColorBlend OverlayTru = new ColorBlend(5)
            {
                Positions = new[] { 0f, 0.20f, 0.42f, 0.70f, 1f },
                Colors = new[]
                {
                    Color.FromArgb(120, 0, 0, 0),
                    Color.FromArgb(30, 0, 0, 0),
                    Color.FromArgb(70, 255, 255, 255),
                    Color.FromArgb(25, 0, 0, 0),
                    Color.FromArgb(125, 0, 0, 0)
                }
            };
            // Hình học của khối (tâm elip nằm trên mặt phẳng đáy)
            private readonly struct HinhHoc
            {
                public readonly float Cx, Cy, RxO, RyO, RxI, RyI, Ha, Ht;
                public HinhHoc(float cx, float cy, float rxO, float ryO, float rxI, float ryI, float ha, float ht)
                {
                    Cx = cx; Cy = cy; RxO = rxO; RyO = ryO; RxI = rxI; RyI = ryI; Ha = ha; Ht = ht;
                }
            }
            private float _percent;
            private Color _accentColor = MauXanh;
            private float _scanOffset = 0f;
            private System.Windows.Forms.Timer? _animTimer;
            private Bitmap? _boNho;
            private Size _boNhoKichThuoc;
            private float _boNhoSweep = -1f;
            private Color _boNhoMau;
            private Font? _fontSo;
            private float _fontCo;
            // Thuộc tính để bật/tắt chế độ chạy full 360 độ
            public bool FullCircleScan { get; set; } = false;
            public RamDonutPanel()
            {
                _animTimer = new System.Windows.Forms.Timer { Interval = 33 };
                _animTimer.Tick += (s, e) =>
                {
                    if (IsDisposed || !Visible) return;
                    float sweep = SweepGoc;
                    if (FullCircleScan)
                        _scanOffset = (_scanOffset + 5f) % 360f;            // chạy liên tục 360 độ
                    else if (sweep > 5f)
                        _scanOffset = (_scanOffset + 2.5f) % sweep;         // chạy co giãn trong cung màu
                    else
                        _scanOffset = 0f;
                    Invalidate();
                };
                _animTimer.Start();
            }
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _animTimer?.Stop();
                    _animTimer?.Dispose();
                    _animTimer = null;
                    _boNho?.Dispose();
                    _boNho = null;
                    _fontSo?.Dispose();
                    _fontSo = null;
                }
                base.Dispose(disposing);
            }
            public float Percent
            {
                get => _percent;
                set
                {
                    float v = Math.Clamp(value, 0f, 100f);
                    if (Math.Abs(v - _percent) < 0.01f) return;
                    _percent = v;
                    Invalidate();
                }
            }
            public Color AccentColor
            {
                get => _accentColor;
                set
                {
                    if (_accentColor == value) return;
                    _accentColor = value;
                    Invalidate();
                }
            }
            // Góc quét của cung màu (độ). Có phần trăm > 0 thì luôn thấy được tối thiểu 3 độ.
            private float SweepGoc
            {
                get
                {
                    float s = _percent / 100f * 360f;
                    if (s <= 0f) return 0f;
                    return Math.Clamp(s, 3f, 359.8f);
                }
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                int w = ClientSize.Width;
                int h = ClientSize.Height;
                if (w < 60 || h < 60) return;
                var g = e.Graphics;
                float sweep = SweepGoc;
                HinhHoc hh = TinhHinhHoc(w, h);
                if (_boNho == null
                    || _boNhoKichThuoc != ClientSize
                    || Math.Abs(_boNhoSweep - sweep) > 0.05f
                    || _boNhoMau != _accentColor)
                {
                    DungBoNho(hh, w, h, sweep);
                }
                if (_boNho != null)
                    g.DrawImageUnscaled(_boNho, 0, 0);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                VeVetQuet(g, hh, sweep);
                VeChuSo(g, hh);
            }
            // ------------------------------------------------------------------
            //  HÌNH HỌC
            // ------------------------------------------------------------------
            private static HinhHoc TinhHinhHoc(int w, int h)
            {
                float rx = Math.Min(w * 0.44f, (h - 16f) / (2f * DoNghieng + TiLeCao));
                rx = Math.Max(24f, rx);
                float ry = rx * DoNghieng;
                float ha = rx * TiLeCao;              // chiều cao phần cung (nổi)
                float ht = ha * 0.42f;                // chiều cao phần nền (thấp)
                float tong = 2f * ry + ha;            // tổng chiều cao khối
                float cyDay = (h - tong) / 2f + ha + ry - 2f;
                return new HinhHoc(w / 2f, cyDay, rx, ry, rx * TiLeLo, ry * TiLeLo, ha, ht);
            }
            // Điểm trên elip; góc tính theo độ, 0 = đỉnh, quay theo chiều kim đồng hồ
            private static PointF Diem(float cx, float cy, float rx, float ry, float goc)
            {
                double t = (goc - 90.0) * Math.PI / 180.0;
                return new PointF(cx + (float)(rx * Math.Cos(t)), cy + (float)(ry * Math.Sin(t)));
            }
            private static int SoDiem(float a0, float a1) => Math.Max(2, (int)Math.Ceiling((a1 - a0) / BuocGoc) + 1);
            // Mặt trên của 1 cung vành khuyên ở độ cao h
            private static PointF[] CungMat(HinhHoc hh, float h, float a0, float a1)
            {
                int n = SoDiem(a0, a1);
                var p = new PointF[2 * n];
                float cy = hh.Cy - h;
                for (int i = 0; i < n; i++)
                {
                    float a = a0 + (a1 - a0) * i / (n - 1);
                    p[i] = Diem(hh.Cx, cy, hh.RxO, hh.RyO, a);                 // cạnh ngoài a0 -> a1
                    p[2 * n - 1 - i] = Diem(hh.Cx, cy, hh.RxI, hh.RyI, a);     // cạnh trong a1 -> a0
                }
                return p;
            }
            // Thành đứng (ngoài hoặc trong) từ độ cao h xuống mặt đáy
            private static PointF[] ThanhDa(HinhHoc hh, bool ngoai, float h, float a0, float a1)
            {
                float rx = ngoai ? hh.RxO : hh.RxI;
                float ry = ngoai ? hh.RyO : hh.RyI;
                int n = SoDiem(a0, a1);
                var p = new PointF[2 * n];
                for (int i = 0; i < n; i++)
                {
                    float a = a0 + (a1 - a0) * i / (n - 1);
                    p[i] = Diem(hh.Cx, hh.Cy - h, rx, ry, a);                  // mép trên
                    p[2 * n - 1 - i] = Diem(hh.Cx, hh.Cy, rx, ry, a);          // mép đáy
                }
                return p;
            }
            private static PointF[] ElipDiem(float cx, float cy, float rx, float ry)
            {
                int n = (int)(360f / BuocGoc);
                var p = new PointF[n];
                for (int i = 0; i < n; i++)
                    p[i] = Diem(cx, cy, rx, ry, i * BuocGoc);
                return p;
            }
            // Tách đoạn góc [lo, hi] thành phần thuộc cung màu (< sweep) và phần thuộc nền (>= sweep)
            private static void Chia(float lo, float hi, float sweep, Action<float, float, bool> viec)
            {
                float cungCuoi = Math.Min(hi, sweep);
                if (cungCuoi - lo > 0.01f) viec(lo, cungCuoi, true);
                float nenDau = Math.Max(lo, sweep);
                if (hi - nenDau > 0.01f) viec(nenDau, hi, false);
            }
            // ------------------------------------------------------------------
            //  BỘ ĐỆM THÂN KHỐI
            // ------------------------------------------------------------------
            private void DungBoNho(HinhHoc hh, int w, int h, float sweep)
            {
                _boNho?.Dispose();
                _boNho = null;
                var bmp = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
                try
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.Clear(Color.Transparent);
                        VeBong(g, hh);
                        VeKhoi(g, hh, sweep);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"RamDonutPanel.DungBoNho: {ex.Message}");
                    bmp.Dispose();
                    return;
                }
                _boNho = bmp;
                _boNhoKichThuoc = ClientSize;
                _boNhoSweep = sweep;
                _boNhoMau = _accentColor;
            }
            private static void VeBong(Graphics g, HinhHoc hh)
            {
                // Bóng đổ mềm quanh chân khối; khoét đúng phần lỗ để mặt sàn trong lỗ sáng, sạch
                var lo = new RectangleF(hh.Cx - hh.RxI, hh.Cy - hh.RyI, 2f * hh.RxI, 2f * hh.RyI);
                using (var br = new SolidBrush(Color.FromArgb(8, 30, 41, 59)))
                {
                    for (int i = 0; i < 8; i++)
                    {
                        float e = 14f - i * 1.75f;
                        var ngoai = new RectangleF(
                            hh.Cx - hh.RxO - e,
                            hh.Cy - hh.RyO + 5f - e * DoNghieng,
                            2f * (hh.RxO + e),
                            2f * (hh.RyO + e * DoNghieng));
                        using (var duong = new GraphicsPath(FillMode.Alternate))
                        {
                            duong.AddEllipse(ngoai);
                            duong.AddEllipse(lo);
                            g.FillPath(br, duong);
                        }
                    }
                }
            }
            private void VeKhoi(Graphics g, HinhHoc hh, float sweep)
            {
                Color cung = _accentColor;
                Color cungMat1 = PhaMau(cung, Color.White, 0.34f);
                Color cungMat2 = cung;
                Color cungThanh = PhaMau(cung, Color.Black, 0.26f);
                Color cungTrong = PhaMau(cung, Color.Black, 0.46f);
                Color cungCat = PhaMau(cung, Color.Black, 0.14f);
                Color nenMat1 = Color.FromArgb(243, 246, 251);
                Color nenMat2 = Color.FromArgb(214, 221, 233);
                Color nenThanh = Color.FromArgb(190, 199, 215);
                Color nenTrong = Color.FromArgb(168, 178, 197);
                bool coCung = sweep > 0.05f;
                bool coNen = sweep < 359.5f;
                // 1) Thành trong, nhìn xuyên qua lỗ: nửa sau của elip trong (góc 270..360 và 0..90)
                VeThanh(g, hh, false, 0f, 90f, sweep, cungTrong, nenTrong);
                VeThanh(g, hh, false, 270f, 360f, sweep, cungTrong, nenTrong);
                // 2) Mặt trên phần nền (thấp)
                if (coNen)
                {
                    using (var duong = new GraphicsPath(FillMode.Alternate))
                    {
                        if (!coCung)
                        {
                            duong.AddPolygon(ElipDiem(hh.Cx, hh.Cy - hh.Ht, hh.RxO, hh.RyO));
                            duong.AddPolygon(ElipDiem(hh.Cx, hh.Cy - hh.Ht, hh.RxI, hh.RyI));
                        }
                        else
                        {
                            duong.AddPolygon(CungMat(hh, hh.Ht, sweep, 360f));
                        }
                        var rc = new RectangleF(hh.Cx - hh.RxO, hh.Cy - hh.Ht - hh.RyO, 2f * hh.RxO, 2f * hh.RyO);
                        using (var br = new LinearGradientBrush(rc, nenMat1, nenMat2, LinearGradientMode.Vertical))
                            g.FillPath(br, duong);
                        using (var vien = new Pen(Color.FromArgb(160, 255, 255, 255), 1f))
                            g.DrawPath(vien, duong);
                    }
                }
                // 3) Thành ngoài (mặt trước): đoạn nền thấp, đoạn cung cao
                VeThanh(g, hh, true, 90f, 270f, sweep, cungThanh, nenThanh);
                // 4) Mặt cắt ở cuối cung (chỉ nhìn thấy khi cung ngắn hơn nửa vòng)
                if (coCung && coNen && sweep < 180f)
                {
                    var cat = new[]
                    {
                        Diem(hh.Cx, hh.Cy - hh.Ha, hh.RxI, hh.RyI, sweep),
                        Diem(hh.Cx, hh.Cy - hh.Ha, hh.RxO, hh.RyO, sweep),
                        Diem(hh.Cx, hh.Cy - hh.Ht, hh.RxO, hh.RyO, sweep),
                        Diem(hh.Cx, hh.Cy - hh.Ht, hh.RxI, hh.RyI, sweep)
                    };
                    using (var br = new SolidBrush(cungCat))
                        g.FillPolygon(br, cat);
                }
                // 5) Mặt trên phần cung (nổi cao)
                if (coCung)
                {
                    PointF[] pts = CungMat(hh, hh.Ha, 0f, sweep);
                    var rc = new RectangleF(hh.Cx - hh.RxO, hh.Cy - hh.Ha - hh.RyO, 2f * hh.RxO, 2f * hh.RyO);
                    using (var br = new LinearGradientBrush(rc, cungMat1, cungMat2, LinearGradientMode.Vertical))
                        g.FillPolygon(br, pts);
                    using (var vien = new Pen(Color.FromArgb(130, 255, 255, 255), 1f))
                        g.DrawPolygon(vien, pts);
                }
            }
            private static void VeThanh(Graphics g, HinhHoc hh, bool ngoai, float lo, float hi, float sweep, Color mauCung, Color mauNen)
            {
                using (var hop = new GraphicsPath(FillMode.Winding))
                {
                    void Them(float a0, float a1, bool laCung)
                    {
                        PointF[] pts = ThanhDa(hh, ngoai, laCung ? hh.Ha : hh.Ht, a0, a1);
                        using (var br = new SolidBrush(laCung ? mauCung : mauNen))
                            g.FillPolygon(br, pts);
                        hop.AddPolygon(pts);
                    }
                    Chia(lo, hi, sweep, Them);
                    if (hop.PointCount == 0) return;
                    var rc = new RectangleF(hh.Cx - hh.RxO, hh.Cy - hh.RyO - hh.Ha, 2f * hh.RxO, 2f * hh.RyO + hh.Ha);
                    using (var lg = new LinearGradientBrush(rc, Color.Black, Color.Black, LinearGradientMode.Horizontal))
                    {
                        lg.InterpolationColors = OverlayTru;
                        g.FillPath(lg, hop);
                    }
                }
            }
            // ------------------------------------------------------------------
            //  VỆT QUÉT + CHỮ SỐ (vẽ mỗi nhịp timer, rất nhẹ)
            // ------------------------------------------------------------------
            private void VeVetQuet(Graphics g, HinhHoc hh, float sweep)
            {
                float dai;
                if (FullCircleScan)
                {
                    dai = 70f;
                }
                else
                {
                    if (sweep < 14f) return;
                    dai = Math.Min(26f, sweep * 0.4f);
                }
                const int soLat = 12;
                float buoc = dai / soLat;
                for (int i = 0; i < soLat; i++)
                {
                    float a0 = _scanOffset + buoc * i;
                    float a1 = a0 + buoc + 0.6f;                  // chồng nhẹ để không lộ đường nối
                    if (!FullCircleScan) a1 = Math.Min(a1, sweep);
                    int alpha = 8 + (int)(105f * (i + 1) / soLat);   // sáng dần về đầu vệt
                    using (var br = new SolidBrush(Color.FromArgb(alpha, 255, 255, 255)))
                        VeLat(g, hh, br, a0, a1, sweep);
                }
            }
            private static void VeLat(Graphics g, HinhHoc hh, Brush br, float a0, float a1, float sweep)
            {
                float dai = a1 - a0;
                if (dai <= 0.05f) return;
                a0 %= 360f;
                a1 = a0 + dai;
                if (a1 <= 360f)
                {
                    VeLatDon(g, hh, br, a0, a1, sweep);
                }
                else
                {
                    VeLatDon(g, hh, br, a0, 360f, sweep);
                    VeLatDon(g, hh, br, 0f, a1 - 360f, sweep);
                }
            }
            private static void VeLatDon(Graphics g, HinhHoc hh, Brush br, float a0, float a1, float sweep)
            {
                if (a1 - a0 < 0.05f) return;
                float cao = ((a0 + a1) * 0.5f < sweep) ? hh.Ha : hh.Ht;
                g.FillPolygon(br, CungMat(hh, cao, a0, a1));
            }
            private void VeChuSo(Graphics g, HinhHoc hh)
            {
                float co = Math.Max(11f, hh.RxI * 0.46f);
                if (_fontSo == null || Math.Abs(_fontCo - co) > 0.5f)
                {
                    _fontSo?.Dispose();
                    _fontSo = new Font(FontUI, co, FontStyle.Bold, GraphicsUnit.Pixel);
                    _fontCo = co;
                }
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                string text = $"{_percent:0}%";
                SizeF sz = g.MeasureString(text, _fontSo);
                float x = hh.Cx - sz.Width / 2f;
                float y = hh.Cy - hh.Ha * 0.5f - sz.Height / 2f;
                using (var bong = new SolidBrush(Color.FromArgb(46, 70, 60, 150)))
                    g.DrawString(text, _fontSo, bong, x + 1.5f, y + 2.5f);
                using (var chu = new SolidBrush(Color.FromArgb(165, 156, 228)))
                    g.DrawString(text, _fontSo, chu, x, y);
            }
        }
        // 🌟 NÚT BO TRÒN 4 GÓC (vẽ khử răng cưa, không dùng Region nên mép không bị lởm chởm)
        private sealed class NutBoTron : Button
        {
            private readonly int _banKinh;
            private bool _hover;
            private bool _nhan;
            public NutBoTron(int banKinh)
            {
                _banKinh = banKinh;
                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw,
                    true);
                FlatStyle = FlatStyle.Flat;
                UseVisualStyleBackColor = false;
            }
            protected override void OnMouseEnter(EventArgs e)
            {
                _hover = true;
                Invalidate();
                base.OnMouseEnter(e);
            }
            protected override void OnMouseLeave(EventArgs e)
            {
                _hover = false;
                _nhan = false;
                Invalidate();
                base.OnMouseLeave(e);
            }
            protected override void OnMouseDown(MouseEventArgs mevent)
            {
                if (mevent.Button == MouseButtons.Left)
                {
                    _nhan = true;
                    Invalidate();
                }
                base.OnMouseDown(mevent);
            }
            protected override void OnMouseUp(MouseEventArgs mevent)
            {
                _nhan = false;
                Invalidate();
                base.OnMouseUp(mevent);
            }
            protected override void OnEnabledChanged(EventArgs e)
            {
                Invalidate();
                base.OnEnabledChanged(e);
            }
            protected override void OnPaint(PaintEventArgs pevent)
            {
                var g = pevent.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                // Tô nền ngoài theo màu của nơi chứa để 4 góc bo trông liền mạch
                using (var nenNgoai = new SolidBrush(Parent?.BackColor ?? MauNen))
                    g.FillRectangle(nenNgoai, ClientRectangle);
                int w = Width - 1;
                int h = Height - 1;
                if (w < 8 || h < 8) return;
                Color mau = !Enabled ? Color.FromArgb(203, 213, 225)
                          : _nhan ? Color.FromArgb(30, 64, 175)
                          : _hover ? Color.FromArgb(29, 78, 216)
                          : MauXanh;
                int r = Math.Min(_banKinh, h / 2);
                using (var duong = GetRoundedRectanglePath(new Rectangle(0, 0, w, h), r))
                {
                    using (var br = new SolidBrush(mau))
                        g.FillPath(br, duong);
                    // Ánh sáng nhẹ ở nửa trên cho nút có chiều sâu
                    if (Enabled && !_nhan)
                    {
                        using (var lg = new LinearGradientBrush(
                            new Rectangle(0, 0, w, Math.Max(2, h)),
                            Color.FromArgb(40, 255, 255, 255),
                            Color.FromArgb(0, 255, 255, 255),
                            LinearGradientMode.Vertical))
                            g.FillPath(lg, duong);
                    }
                }
                using (var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                })
                using (var chu = new SolidBrush(Enabled ? ForeColor : Color.White))
                {
                    var vung = new RectangleF(0, _nhan ? 1f : 0f, Width, Height);
                    g.DrawString(Text, Font, chu, vung, sf);
                }
                if (Focused && ShowFocusCues && w > 12 && h > 12)
                {
                    using (var duongFocus = GetRoundedRectanglePath(new Rectangle(3, 3, w - 6, h - 6), Math.Max(2, r - 3)))
                    using (var pen = new Pen(Color.FromArgb(170, 255, 255, 255), 1.5f))
                        g.DrawPath(pen, duongFocus);
                }
            }
        }
        // 🌟 NHÃN TRẠNG THÁI DẠNG VIÊN THUỐC: nền/viền tự lấy từ ForeColor nên đổi màu chữ trong code cũ là đổi cả nền
        private sealed class NhanTrangThai : Label
        {
            public NhanTrangThai()
            {
                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw,
                    true);
            }
            protected override void OnForeColorChanged(EventArgs e)
            {
                Invalidate();
                base.OnForeColorChanged(e);
            }
            protected override void OnTextChanged(EventArgs e)
            {
                Invalidate();
                base.OnTextChanged(e);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                using (var nenNgoai = new SolidBrush(Parent?.BackColor ?? MauNen))
                    g.FillRectangle(nenNgoai, ClientRectangle);
                int w = Width - 1;
                int h = Height - 1;
                if (w < 12 || h < 12) return;
                using (var duong = GetRoundedRectanglePath(new Rectangle(0, 0, w, h), h / 2))
                using (var nen = new SolidBrush(PhaMau(ForeColor, Color.White, 0.90f)))
                using (var vien = new Pen(PhaMau(ForeColor, Color.White, 0.72f), 1f))
                {
                    g.FillPath(nen, duong);
                    g.DrawPath(vien, duong);
                }
                using (var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                })
                using (var chu = new SolidBrush(ForeColor))
                    g.DrawString(Text, Font, chu, new RectangleF(0, 0, Width, Height), sf);
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
            private Font? _fontDam;
            private Font? _fontTieuDe;
            private ImageList? _anhChieuCaoDong;
            private bool _dangCanCot;
            public Form_TaskManagerMini()
            {
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw,
                    true);
                InitializeModernUI_SplitLayout();
                // Đọc RAM ngay (rất nhanh: GlobalMemoryStatusEx + WorkingSet) để lần vẽ đầu tiên
                // đã có số liệu, không phải chờ nạp xong bảng cấu hình + 1 nhịp timer mới hiện.
                DocSoLieuRam(new ComputerInfo());
                ApSoLieuLenBieuDo();
                Load += Form_TaskManagerMini_Load;
            }
            private async void Form_TaskManagerMini_Load(object? sender, EventArgs e)
            {
                if (_daKhoiTao || IsDisposed)
                    return;
                _daKhoiTao = true;
                try
                {
                    // Bắt đầu giám sát RAM ngay, song song với việc nạp bảng cấu hình (Registry, ổ đĩa, UUID...)
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
                    await LoadListViewDataAsync();
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
                lblSystemStatus = new NhanTrangThai
                {
                    Text = "● Đang khởi tạo",
                    AutoSize = false,
                    Width = 240,
                    Height = 30,
                    TextAlign = ContentAlignment.MiddleCenter,
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
                var infoCard = new SmoothPanel
                {
                    Dock = DockStyle.Fill,
                    BackColor = MauThe,
                    Padding = new Padding(12, 10, 12, 14),
                    Margin = Padding.Empty
                };
                infoCard.Paint += (s, e) => VeTheWeb(e.Graphics, infoCard.Width, infoCard.Height);
                _fontDam = new Font(FontUI, 9F, FontStyle.Bold);
                _fontTieuDe = new Font(FontUI, 9F, FontStyle.Bold);
                // Ảnh 1 x N chỉ để nới chiều cao dòng của ListView (dòng thoáng như bảng trên web)
                _anhChieuCaoDong = new ImageList
                {
                    ImageSize = new Size(1, LogicalToDeviceUnits(22)),
                    ColorDepth = ColorDepth.Depth32Bit
                };
                lvInfo = new SmoothListView
                {
                    Dock = DockStyle.Fill,
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines = false,
                    HeaderStyle = ColumnHeaderStyle.Nonclickable,
                    BorderStyle = BorderStyle.None,
                    Font = new Font(FontUI, 9F),
                    BackColor = MauThe,
                    ForeColor = MauChu,
                    HideSelection = false,
                    MultiSelect = false,
                    UseCompatibleStateImageBehavior = false,
                    OwnerDraw = true,
                    SmallImageList = _anhChieuCaoDong
                };
                lvInfo.Columns.Add("Thành phần", 220);
                lvInfo.Columns.Add("Thông tin chi tiết", 440);
                lvInfo.DrawColumnHeader += LvInfo_DrawColumnHeader;
                lvInfo.DrawItem += LvInfo_DrawItem;
                lvInfo.DrawSubItem += LvInfo_DrawSubItem;
                lvInfo.Resize += (s, e) => CanChinhCot();
                lvInfo.ClientSizeChanged += (s, e) => CanChinhCot();
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
                // Nút bo tròn 4 góc (bán kính 12px). Màu hover/nhấn do chính nút tự vẽ.
                btnExport = new NutBoTron(12)
                {
                    Text = "Xuất báo cáo",
                    Size = new Size(155, 36),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    BackColor = MauXanh,
                    ForeColor = Color.White,
                    Font = new Font(FontUI, 9.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
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
            // ---- Lưới cấu hình kiểu bảng web: tiêu đề nhạt, dòng thoáng, đường kẻ ngang mảnh, không kẻ dọc ----
            private void LvInfo_DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
            {
                var g = e.Graphics;
                using (var nen = new SolidBrush(Color.FromArgb(248, 250, 252)))
                    g.FillRectangle(nen, e.Bounds);
                using (var pen = new Pen(MauVien))
                    g.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                var vung = new Rectangle(e.Bounds.X + 14, e.Bounds.Y, Math.Max(0, e.Bounds.Width - 18), e.Bounds.Height);
                TextRenderer.DrawText(
                    g, e.Header?.Text ?? string.Empty, _fontTieuDe ?? lvInfo.Font, vung, MauPhu,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            }
            // Nền cả dòng: dòng nhóm có thanh nhấn màu xanh bên trái; dòng thường có đường kẻ mảnh; dòng chọn nền xanh nhạt
            private void LvInfo_DrawItem(object? sender, DrawListViewItemEventArgs e)
            {
                if (e.Item == null) return;
                bool laNhom = e.Item.Tag is bool b && b;
                Rectangle r = e.Bounds;
                Color nen = laNhom ? Color.FromArgb(243, 245, 255)
                          : e.Item.Selected ? Color.FromArgb(238, 242, 255)
                          : MauThe;
                using (var br = new SolidBrush(nen))
                    e.Graphics.FillRectangle(br, r);
                if (laNhom)
                {
                    using (var thanh = new SolidBrush(MauXanh))
                        e.Graphics.FillRectangle(thanh, r.X, r.Y + 6, 3, Math.Max(2, r.Height - 12));
                }
                using (var pen = new Pen(Color.FromArgb(241, 245, 249)))
                    e.Graphics.DrawLine(pen, r.Left, r.Bottom - 1, r.Right, r.Bottom - 1);
                // Vẽ luôn chữ của cả dòng tại đây (không phụ thuộc DrawSubItem): Windows có lúc chỉ gọi
                // DrawItem (đổi chọn, hover, cuộn) -> nếu chữ vẽ ở DrawSubItem sẽ bị nền đè mất.
                const TextFormatFlags co = TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix |
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
                if (laNhom)
                {
                    int x = r.X + 16;
                    var vung = new Rectangle(x, r.Y, Math.Max(0, r.Right - x - 8), r.Height - 1);
                    TextRenderer.DrawText(e.Graphics, e.Item.Text, _fontDam ?? lvInfo.Font,
                        vung, Color.FromArgb(30, 41, 59), co);
                }
                else
                {
                    int left = r.X;
                    for (int i = 0; i < lvInfo.Columns.Count && i < e.Item.SubItems.Count; i++)
                    {
                        int w = lvInfo.Columns[i].Width;
                        var vung = new Rectangle(left + 14, r.Y, Math.Max(0, w - 18), r.Height - 1);
                        TextRenderer.DrawText(e.Graphics, e.Item.SubItems[i].Text, lvInfo.Font,
                            vung, i == 0 ? MauPhu : MauChu, co);
                        left += w;
                    }
                }
                e.DrawDefault = false;
            }
            // Mọi thứ đã vẽ ở DrawItem; vẫn phải gắn handler để ListView không tự vẽ mặc định đè lên
            private void LvInfo_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
            {
                e.DrawDefault = false;
            }
            // Co 2 cột vừa khít khung (không để lộ thanh cuộn ngang khi thanh cuộn dọc xuất hiện)
            private void CanChinhCot()
            {
                if (_dangCanCot || lvInfo == null || lvInfo.IsDisposed || lvInfo.Columns.Count < 2)
                    return;
                int tong = lvInfo.ClientSize.Width;
                if (tong <= 0) return;
                _dangCanCot = true;
                try
                {
                    int cot1 = Math.Min(240, Math.Max(150, tong / 3));
                    int cot2 = Math.Max(80, tong - cot1 - 1);
                    if (lvInfo.Columns[0].Width != cot1) lvInfo.Columns[0].Width = cot1;
                    if (lvInfo.Columns[1].Width != cot2) lvInfo.Columns[1].Width = cot2;
                    if (lvInfo.IsHandleCreated)
                        ShowScrollBar(lvInfo.Handle, SB_HORZ, false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"CanChinhCot: {ex.Message}");
                }
                finally
                {
                    _dangCanCot = false;
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
                    Padding = new Padding(12, 10, 12, 12)
                };
                card.Paint += (s, e) => VeTheWeb(e.Graphics, card.Width, card.Height);
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
                CanChinhCot();   // thêm dòng có thể làm hiện thanh cuộn dọc -> co lại cột cho vừa khung
                lvInfo.Invalidate();   // vẽ lại ngay, không đợi người dùng bấm/hover
            }
            private void HardwareSafePollingLoop(CancellationToken token)
            {
                var computerInfo = new ComputerInfo();
                while (!token.IsCancellationRequested)
                {
                    // Số liệu đầu tiên đã đọc trong constructor -> chờ trước rồi mới đọc lại
                    try
                    {
                        if (token.WaitHandle.WaitOne(1000))
                            break;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    DocSoLieuRam(computerInfo);
                }
            }
            private void DocSoLieuRam(ComputerInfo computerInfo)
            {
                try
                {
                    ulong total = computerInfo.TotalPhysicalMemory;
                    ulong available = computerInfo.AvailablePhysicalMemory;
                    double totalGB = total / 1024d / 1024d / 1024d;
                    double freeGB = available / 1024d / 1024d / 1024d;
                    if (totalGB <= 0)
                        return;
                    freeGB = Math.Clamp(freeGB, 0, totalGB);
                    double usedGB = Math.Clamp(totalGB - freeGB, 0, totalGB);
                    _bgSysRamPercent = (float)(usedGB / totalGB * 100f);
                    _bgSysRamText = $"{usedGB:0.00} GB / {totalGB:0.00} GB";
                    long appPrivateBytes = GC.GetTotalMemory(false);
                    try
                    {
                        using var curProc = Process.GetCurrentProcess();
                        appPrivateBytes = curProc.WorkingSet64;
                    }
                    catch { }
                    double appMB = appPrivateBytes / 1024d / 1024d;
                    _bgAppRamPercent = (float)Math.Clamp(appMB / (totalGB * 1024d) * 100f, 0, 100f);
                    _bgAppRamText = appMB >= 1024
                        ? $"{appMB / 1024d:0.00} GB"
                        : $"{appMB:0.0} MB";
                }
                catch { }
            }
            private void ApSoLieuLenBieuDo()
            {
                try
                {
                    chartSysRam.Percent = _bgSysRamPercent;
                    lblSysRamDetail.Text = _bgSysRamText;
                    chartAppRam.Percent = _bgAppRamPercent;
                    lblAppRamDetail.Text = $"Đang sử dụng: {_bgAppRamText}";
                }
                catch { }
            }
            private void UpdateTimer_Tick(object? sender, EventArgs e)
            {
                if (_dangDong || IsDisposed || !IsHandleCreated)
                    return;
                ApSoLieuLenBieuDo();
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
                    Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Xuất báo cáo trạng thái hệ thống", $"Tệp: {Path.GetFileName(sfd.FileName)}");
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
                        Module_HeThong.TieuDe_Loi,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);   // giải phóng ListView trước rồi mới giải phóng font/ImageList nó đang dùng
                if (disposing)
                {
                    _fontDam?.Dispose();
                    _fontDam = null;
                    _fontTieuDe?.Dispose();
                    _fontTieuDe = null;
                    _anhChieuCaoDong?.Dispose();
                    _anhChieuCaoDong = null;
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
///Yêu mèo cam
//#nullable enable