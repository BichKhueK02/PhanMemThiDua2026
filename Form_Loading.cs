///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    /// <summary>
    /// Form loading hiện đại: thẻ bo góc + đổ bóng, vòng tiến trình gradient, thanh tiến trình có vệt sáng chạy,
    /// fade-in, tự căn giữa theo form cha, không cướp focus, hỗ trợ màn hình DPI cao.
    /// Tối ưu RAM/CPU: font dùng chung (static), toàn bộ Pen/Brush/Path tạo 1 lần và tái sử dụng (không rác mỗi khung hình),
    /// không rò rỉ GDI region.
    /// API giữ nguyên: new Form_Loading(msg), CapNhatThongBao(txt), CapNhatPhanTram(percent)
    /// </summary>
    public class Form_Loading : Form
    {
        // ====== Trạng thái ======
        private readonly System.Windows.Forms.Timer _timer;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private long _lastTicks;
        private string _message;
        private volatile bool _autoSimulate = true;
        private float _targetPercent = 0f;   // giá trị thật / giả lập cần đạt tới
        private float _displayPercent = 0f;  // giá trị hiển thị (đã làm mượt)
        private float _spinAngle = 0f;       // pha của hiệu ứng ánh sáng
        private int _lastPctInt = -1;
        private string _pctText = "0%";
        private readonly float _s;           // hệ số DPI (1.0 = 96 dpi)
        // ====== Giao diện (dùng chung toàn app, KHÔNG Dispose) ======
        private static readonly Color Accent1 = Color.FromArgb(0, 122, 255);
        private static readonly Color Accent2 = Color.FromArgb(0, 210, 255);
        private static readonly Color CardTop = Color.White;
        private static readonly Color CardBottom = Color.FromArgb(246, 248, 252);
        private static readonly Color TrackColor = Color.FromArgb(234, 238, 244);
        private static readonly Color TextColor = Color.FromArgb(55, 65, 81);
        private static readonly Color SubTextColor = Color.FromArgb(130, 140, 155);
        private static readonly Font MessageFont = new Font(Module_HeThong.TenFontHeThong, 10.5F, FontStyle.Regular);
        private static readonly Font PercentFont = new Font(Module_HeThong.TenFontHeThong, 14F, FontStyle.Bold);
        private static readonly Font SmallFont = new Font(Module_HeThong.TenFontHeThong, 8.5F, FontStyle.Regular);
        private const int RingSize = 76;
        private const int RingThickness = 7;
        private const int CornerRadius = 16;
        private const TextFormatFlags FlagsMessage = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
        private const TextFormatFlags FlagsCenter = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
        // ====== Bộ đệm GDI+ (tạo lại chỉ khi đổi kích thước) ======
        private Rectangle _ringRect;
        private RectangleF _barRect;
        private GraphicsPath? _borderPath;
        private Pen? _borderPen, _trackPen, _progPen, _glowPen;
        private LinearGradientBrush? _bgBrush, _progBrush, _barBrush, _shimmerBrush;
        private SolidBrush? _trackBrush, _dotBrush;
        private float _shimmerW;
        // ====== Win32 / DWM ======
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;
        private const int CS_DROPSHADOW = 0x00020000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private bool _dwmThuLai;
        private bool _dwmOk;
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;                       // đổ bóng quanh form
                cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;    // không cướp focus, không hiện trong Alt+Tab
                return cp;
            }
        }
        protected override bool ShowWithoutActivation => true;
        public Form_Loading(string message = "Đang xử lý dữ liệu...")
        {
            _message = message;
            _s = Math.Max(1f, DeviceDpi / 96f);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;
            Opacity = 0; // fade-in khi hiện
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            CapNhatKichThuocForm(message);
            _timer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60 FPS
            _timer.Tick += OnTick;
            FormClosing += (s, e) => _timer.Stop();
        }
        private int S(int v) => (int)Math.Round(v * _s);
        // ====== Vòng đời cửa sổ ======
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _dwmThuLai = false;
            ApDungBoGoc();
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            CanGiuaTheoFormCha();   // đang Opacity = 0 nên dời vị trí không bị thấy
            _lastTicks = _clock.ElapsedTicks;
            _timer.Start();
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            BuildGeometry();
            if (IsHandleCreated && !_dwmOk) ApDungBoGoc();
        }
        // ====== Bo góc: ưu tiên DWM (Win11, mịn), dự phòng Region (Win10) ======
        private void ApDungBoGoc()
        {
            if (!IsHandleCreated) return;
            if (!_dwmThuLai)
            {
                int pref = DWMWCP_ROUND;
                int hr;
                try { hr = DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int)); }
                catch { hr = -1; }
                _dwmOk = hr == 0;
                _dwmThuLai = true;
            }
            if (_dwmOk) return;
            using (var p = RoundRect(new RectangleF(0, 0, Width, Height), S(CornerRadius)))
            {
                var cu = Region;
                Region = new Region(p);   // (bản cũ dùng HRGN không giải phóng -> rò rỉ GDI)
                cu?.Dispose();
            }
        }
        private void CanGiuaTheoFormCha()
        {
            try
            {
                Rectangle vung;
                if (Owner != null && Owner.IsHandleCreated && Owner.WindowState != FormWindowState.Minimized)
                    vung = Owner.RectangleToScreen(Owner.ClientRectangle);   // đúng cả khi form cha là form con nhúng (TopLevel = false)
                else
                    vung = Screen.FromControl(this).WorkingArea;
                var wa = Screen.FromRectangle(vung).WorkingArea;
                int x = vung.X + (vung.Width - Width) / 2;
                int y = vung.Y + (vung.Height - Height) / 2;
                x = Math.Max(wa.Left, Math.Min(x, wa.Right - Width));
                y = Math.Max(wa.Top, Math.Min(y, wa.Bottom - Height));
                Location = new Point(x, y);
            }
            catch { /* giữ vị trí mặc định */ }
        }
        // ====== Tự đo kích thước theo nội dung ======
        private void CapNhatKichThuocForm(string text)
        {
            int tw = TextRenderer.MeasureText(text, MessageFont, new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
            int w = Math.Clamp(tw + S(90), S(280), S(560));
            Size = new Size(w, S(205));   // OnSizeChanged sẽ dựng lại hình học + bo góc
        }
        // ====== Dựng sẵn mọi đối tượng GDI+ (chỉ khi đổi kích thước) ======
        private void BuildGeometry()
        {
            if (Width <= 0 || Height <= 0) return;
            DisposeGeometry();
            int ring = S(RingSize);
            float th = S(RingThickness);
            _ringRect = new Rectangle((Width - ring) / 2, S(26), ring, ring);
            _bgBrush = new LinearGradientBrush(new Rectangle(0, 0, Width, Height), CardTop, CardBottom, LinearGradientMode.Vertical);
            _borderPath = RoundRect(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), S(CornerRadius));
            _borderPen = new Pen(Color.FromArgb(40, 0, 0, 0), 1);
            _trackPen = new Pen(TrackColor, th);
            _progBrush = new LinearGradientBrush(_ringRect, Accent1, Accent2, LinearGradientMode.ForwardDiagonal);
            _progPen = new Pen(_progBrush, th) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            _glowPen = new Pen(Color.FromArgb(150, 255, 255, 255), Math.Max(1f, th - S(3)))
            { StartCap = LineCap.Round, EndCap = LineCap.Round };
            _dotBrush = new SolidBrush(Color.White);
            float bh = Math.Max(3, S(5));
            _barRect = new RectangleF(S(30), Height - S(26), Width - S(60), bh);
            _trackBrush = new SolidBrush(TrackColor);
            _barBrush = new LinearGradientBrush(
                new RectangleF(_barRect.X - 1, _barRect.Y, _barRect.Width + 2, bh), Accent1, Accent2, LinearGradientMode.Horizontal);
            // Vệt sáng chạy trên thanh tiến trình (brush có sẵn, mỗi khung hình chỉ dịch vị trí)
            _shimmerW = S(70);
            _shimmerBrush = new LinearGradientBrush(new RectangleF(0, _barRect.Y, _shimmerW, bh),
                Color.Transparent, Color.Transparent, LinearGradientMode.Horizontal)
            {
                InterpolationColors = new ColorBlend
                {
                    Positions = new[] { 0f, 0.5f, 1f },
                    Colors = new[] { Color.FromArgb(0, 255, 255, 255), Color.FromArgb(120, 255, 255, 255), Color.FromArgb(0, 255, 255, 255) }
                }
            };
        }
        private void DisposeGeometry()
        {
            _borderPath?.Dispose(); _borderPath = null;
            _borderPen?.Dispose(); _borderPen = null;
            _trackPen?.Dispose(); _trackPen = null;
            _progPen?.Dispose(); _progPen = null;
            _glowPen?.Dispose(); _glowPen = null;
            _bgBrush?.Dispose(); _bgBrush = null;
            _progBrush?.Dispose(); _progBrush = null;
            _barBrush?.Dispose(); _barBrush = null;
            _shimmerBrush?.Dispose(); _shimmerBrush = null;
            _trackBrush?.Dispose(); _trackBrush = null;
            _dotBrush?.Dispose(); _dotBrush = null;
        }
        // ====== Vòng lặp animation (độc lập tốc độ máy) ======
        private void OnTick(object? sender, EventArgs e)
        {
            if (IsDisposed || !Visible) return;
            long now = _clock.ElapsedTicks;
            float dt = (now - _lastTicks) / (float)Stopwatch.Frequency; // giây
            _lastTicks = now;
            if (dt > 0.1f) dt = 0.1f;
            _spinAngle = (_spinAngle + 300f * dt) % 360f;
            // Giả lập tiến trình: tiệm cận 99%
            if (_autoSimulate && _targetPercent < 99f)
                _targetPercent += (99f - _targetPercent) * 0.6f * dt;
            // Làm mượt: nội suy hiển thị về target (exponential smoothing)
            float k = 1f - (float)Math.Exp(-8f * dt);
            _displayPercent += (_targetPercent - _displayPercent) * k;
            int p = (int)Math.Clamp(_displayPercent, 0f, 100f);
            if (p != _lastPctInt) { _lastPctInt = p; _pctText = p + "%"; }   // chỉ tạo chuỗi mới khi số đổi
            // Fade-in
            if (Opacity < 1) Opacity = Math.Min(1, Opacity + dt * 6f);
            Invalidate();
        }
        // ====== Vẽ ======
        protected override void OnPaint(PaintEventArgs e)
        {
            if (_borderPath == null) BuildGeometry();
            if (_borderPath == null || _bgBrush == null) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.FillRectangle(_bgBrush, ClientRectangle);
            g.DrawPath(_borderPen!, _borderPath);
            DrawRing(g);
            DrawMessage(g);
            DrawProgressBar(g);
        }
        private void DrawRing(Graphics g)
        {
            float pct = Math.Clamp(_displayPercent, 0f, 100f);
            float sweep = pct * 3.6f;
            // Đường ray
            g.DrawEllipse(_trackPen!, _ringRect);
            // Cung tiến trình (gradient)
            if (sweep > 0.5f)
                g.DrawArc(_progPen!, _ringRect, -90, sweep);
            // Vệt sáng chạy quanh vòng (chỉ trong phần đã hoàn thành)
            if (sweep > 20f)
            {
                float start = -90 + (_spinAngle % sweep);
                float len = Math.Min(24f, sweep - (start + 90));
                if (len > 2f) g.DrawArc(_glowPen!, _ringRect, start, len);
            }
            // Chấm đầu mút
            if (sweep > 2f && pct < 100f)
            {
                double a = (-90 + sweep) * Math.PI / 180.0;
                float cx = _ringRect.X + _ringRect.Width / 2f + (float)Math.Cos(a) * _ringRect.Width / 2f;
                float cy = _ringRect.Y + _ringRect.Height / 2f + (float)Math.Sin(a) * _ringRect.Height / 2f;
                float r = Math.Max(2.5f, S(3));
                g.FillEllipse(_dotBrush!, cx - r / 2f, cy - r / 2f, r, r);
            }
            // Số phần trăm ở giữa
            TextRenderer.DrawText(g, _pctText, PercentFont, _ringRect, TextColor, Color.Transparent, FlagsCenter);
        }
        private void DrawMessage(Graphics g)
        {
            var r1 = new Rectangle(S(16), S(116), Width - S(32), S(28));
            TextRenderer.DrawText(g, _message, MessageFont, r1, TextColor, Color.Transparent, FlagsMessage);
            var r2 = new Rectangle(S(16), S(140), Width - S(32), S(20));
            TextRenderer.DrawText(g, "Vui lòng đợi trong giây lát", SmallFont, r2, SubTextColor, Color.Transparent, FlagsCenter);
        }
        private void DrawProgressBar(Graphics g)
        {
            float h = _barRect.Height;
            using (var p = RoundRect(_barRect, h / 2f))
                g.FillPath(_trackBrush!, p);
            float w = _barRect.Width * Math.Clamp(_displayPercent, 0f, 100f) / 100f;
            if (w < h) return;
            var fill = new RectangleF(_barRect.X, _barRect.Y, w, h);
            using (var p = RoundRect(fill, h / 2f))
            {
                g.FillPath(_barBrush!, p);
                // Vệt sáng chạy qua thanh (cắt gọn trong phần đã đầy)
                var oldClip = g.Clip;
                g.SetClip(p);
                float phase = _spinAngle / 360f;
                float x = _barRect.X + phase * (w + _shimmerW) - _shimmerW;
                _shimmerBrush!.ResetTransform();
                _shimmerBrush.TranslateTransform(x, 0);
                g.FillRectangle(_shimmerBrush, x, _barRect.Y, _shimmerW, h);
                g.Clip = oldClip;
                oldClip.Dispose();
            }
        }
        private static GraphicsPath RoundRect(RectangleF b, float radius)
        {
            float d = radius * 2f;
            var path = new GraphicsPath();
            path.AddArc(b.X, b.Y, d, d, 180, 90);
            path.AddArc(b.Right - d, b.Y, d, d, 270, 90);
            path.AddArc(b.Right - d, b.Bottom - d, d, d, 0, 90);
            path.AddArc(b.X, b.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
        // ====== API công khai (giữ nguyên tên cũ) ======
        public void CapNhatThongBao(string txt)
        {
            if (IsDisposed) return;
            if (IsHandleCreated && InvokeRequired)
            {
                BeginInvoke(new Action(() => CapNhatThongBao(txt)));   // BeginInvoke: không treo luồng gọi nếu UI đang bận
                return;
            }
            _message = txt;
            CapNhatKichThuocForm(txt);
            if (Visible) CanGiuaTheoFormCha();
            Invalidate();
        }
        public void CapNhatPhanTram(int percent)
        {
            _autoSimulate = false;
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            _targetPercent = percent; // hiển thị sẽ tự chạy mượt tới giá trị này
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer?.Dispose();
                DisposeGeometry();
                // Font là static dùng chung -> KHÔNG Dispose ở đây
            }
            base.Dispose(disposing);
        }
    }
}