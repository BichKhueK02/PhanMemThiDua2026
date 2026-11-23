///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    /// <summary>
    /// Form loading 3D: cửa sổ "layered" vẽ từng điểm ảnh có độ trong suốt (per-pixel alpha).
    ///  - Chỉ còn MỘT đường viền duy nhất (không còn viền DWM của Win11 + viền tự vẽ chồng nhau).
    ///  - Bo góc mịn, đổ bóng mềm nhiều lớp (không dùng Region, không dùng CS_DROPSHADOW) -> Win10/Win11 giống hệt nhau.
    ///  - Thẻ nổi (gradient + vệt sáng), rãnh lõm + vòng tiến trình dạng ống, nút tròn nổi ở giữa, chữ khắc nổi,
    ///    thanh tiến trình dạng ống bóng có vệt sáng chạy.
    /// Tối ưu RAM/CPU: toàn bộ lớp nền tĩnh (bóng, thẻ, rãnh, chữ) vẽ 1 lần vào bitmap nền; mỗi khung hình chỉ chép nền (memcpy)
    /// rồi vẽ phần động (cung tiến trình, %, thanh) thẳng vào vùng nhớ DIB và đẩy lên màn hình bằng UpdateLayeredWindow.
    /// Không cấp phát đối tượng GDI mới mỗi khung hình, giải phóng đầy đủ DIB/DC khi đóng.
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
        private float _fade = 0f;            // 0..1: độ mờ khi hiện (fade-in)
        private int _lastPctInt = -1;
        private string _pctText = "0%";
        private float _s;                    // hệ số DPI (1.0 = 96 dpi)
        // ====== Kích thước logic (đơn vị 96 dpi, nhân _s ra điểm ảnh thật) ======
        private const float MarginX = 28f;        // lề trái/phải chứa bóng
        private const float MarginTop = 22f;      // lề trên
        private const float MarginBottom = 38f;   // lề dưới (bóng đổ xuống nhiều hơn)
        // Kiểu giao diện (Form12 > Tab 2 > mục 10): "Hiện đại" = thẻ ngang phẳng kiểu web; "Cổ điển" = thẻ 3D như cũ
        private readonly bool _hienDai;
        private float CardH => _hienDai ? 116f : 210f;          // chiều cao thẻ
        private float CardRadius => _hienDai ? 14f : 20f;
        private const float RingSize = 80f;
        private const float RingThickness = 8f;
        private float CardW => Width / _s - 2f * MarginX;
        // ====== Giao diện (dùng chung toàn app, KHÔNG Dispose) ======
        private static readonly Color Accent1 = Color.FromArgb(0, 122, 255);
        private static readonly Color Accent2 = Color.FromArgb(0, 210, 255);
        private static readonly Color AccentDark1 = Color.FromArgb(0, 84, 196);
        private static readonly Color AccentDark2 = Color.FromArgb(0, 148, 214);
        private static readonly Color TextColor = Color.FromArgb(40, 52, 71);
        private static readonly Color SubTextColor = Color.FromArgb(120, 132, 150);
        private static readonly Font MessageFont = new Font(Module_HeThong.TenFontHeThong, 10.5F, FontStyle.Regular);
        private static readonly Font PercentFont = new Font(Module_HeThong.TenFontHeThong, 15F, FontStyle.Bold);
        private static readonly Font SmallFont = new Font(Module_HeThong.TenFontHeThong, 8.5F, FontStyle.Regular);
        private static readonly StringFormat SfCenter = new StringFormat(StringFormatFlags.NoWrap)
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter
        };
        // Bộ đo chữ dùng chung (GDI+ để kích thước đo khớp với nét vẽ)
        private static readonly Bitmap MeasureBmp;
        private static readonly Graphics MeasureGfx;
        static Form_Loading()
        {
            MeasureBmp = new Bitmap(1, 1, PixelFormat.Format32bppPArgb);
            MeasureBmp.SetResolution(96f, 96f);
            MeasureGfx = Graphics.FromImage(MeasureBmp);
            MeasureGfx.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        }
        // ====== Bề mặt vẽ: DIB 32bpp (premultiplied alpha) + bitmap nền tĩnh ======
        private IntPtr _memDc, _hDib, _oldObj;
        private Bitmap? _surface;     // bitmap GDI+ trỏ thẳng vào vùng nhớ DIB
        private Graphics? _g;
        private Bitmap? _bg;          // lớp nền tĩnh (bóng + thẻ + rãnh + chữ)
        private int _pxW, _pxH;
        private bool _surfaceDirty = true;
        private bool _staticDirty = true;
        // ====== Bộ đệm GDI+ phần động (tạo lại chỉ khi đổi kích thước) ======
        private RectangleF _ringRect, _hiRect, _discRect, _barRect;
        private float _shimmerW;
        private Pen? _arcShadowPen, _arcEdgePen, _arcMainPen, _arcHiPen, _glowPen;
        private LinearGradientBrush? _arcEdgeBrush, _arcMainBrush;
        private LinearGradientBrush? _barBrush, _barGlossBrush, _shimmerBrush;
        private SolidBrush? _dotBrush, _pctBrush, _pctEmbossBrush;
        // ====== Win32 ======
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int ULW_ALPHA = 0x00000002;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_DONOTROUND = 1;
        private const int DWMWA_BORDER_COLOR = 34;
        private const int DWMWA_COLOR_NONE = unchecked((int)0xFFFFFFFE);
        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeSize { public int Cx, Cy; }
        [StructLayout(LayoutKind.Sequential)]
        private struct BlendFunction
        {
            public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct BitmapInfoHeader
        {
            public uint biSize;
            public int biWidth, biHeight;
            public ushort biPlanes, biBitCount;
            public uint biCompression, biSizeImage;
            public int biXPelsPerMeter, biYPelsPerMeter;
            public uint biClrUsed, biClrImportant;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct BitmapInfo
        {
            public BitmapInfoHeader bmiHeader;
            public uint bmiColors;
        }
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref NativePoint pptDst, ref NativeSize psize,
            IntPtr hdcSrc, ref NativePoint pprSrc, int crKey, ref BlendFunction pblend, int dwFlags);
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr ho);
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateDIBSection(IntPtr hdc, [In] ref BitmapInfo pbmi, uint usage, out IntPtr ppvBits,
            IntPtr hSection, uint offset);
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE; // layered: per-pixel alpha; không cướp focus; không hiện Alt+Tab
                return cp;
            }
        }
        protected override bool ShowWithoutActivation => true;
        public Form_Loading(string message = "Đang xử lý dữ liệu...")
        {
            _message = message ?? string.Empty;
            _s = Math.Max(1f, DeviceDpi / 96f);
            try { _hienDai = !Module_KieuGiaoDien.LaCoDien(Module_KieuGiaoDien.DangApDung ?? Module_KieuGiaoDien.DocChiDoc()); }
            catch { _hienDai = true; }
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            TopMost = true;
            CapNhatKichThuocForm(_message);
            _timer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60 FPS
            _timer.Tick += OnTick;
            FormClosing += (s, e) => _timer.Stop();
        }
        // ====== Vòng đời cửa sổ ======
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Win11: tắt viền hệ thống + bo góc hệ thống (nguyên nhân gây "2 viền"); Win10 trả về lỗi -> bỏ qua
            try
            {
                int noRound = DWMWCP_DONOTROUND;
                DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref noRound, sizeof(int));
                int none = DWMWA_COLOR_NONE;
                DwmSetWindowAttribute(Handle, DWMWA_BORDER_COLOR, ref none, sizeof(int));
            }
            catch { /* hệ điều hành cũ: không có dwmapi thuộc tính này */ }
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            CanGiuaTheoFormCha();   // đang _fade = 0 (chưa hiện) nên dời vị trí không bị thấy
            _lastTicks = _clock.ElapsedTicks;
            RenderFrame();
            _timer.Start();
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            _surfaceDirty = true;
            _staticDirty = true;
        }
        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            _s = Math.Max(1f, e.DeviceDpiNew / 96f);
            CapNhatKichThuocForm(_message);
            _surfaceDirty = true;
            _staticDirty = true;
        }
        // Cửa sổ layered tự vẽ qua UpdateLayeredWindow -> chặn mọi vẽ nền/vẽ mặc định của WinForms
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e) { }
        // ====== Căn giữa: căn theo THẺ (không tính phần lề bóng) ======
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
                int cardL = (int)Math.Round(MarginX * _s);
                int cardT = (int)Math.Round(MarginTop * _s);
                int cardWpx = (int)Math.Round(CardW * _s);
                int cardHpx = (int)Math.Round(CardH * _s);
                int x = vung.X + (vung.Width - cardWpx) / 2 - cardL;
                int y = vung.Y + (vung.Height - cardHpx) / 2 - cardT;
                x = Math.Max(wa.Left - cardL, Math.Min(x, wa.Right - cardL - cardWpx));
                y = Math.Max(wa.Top - cardT, Math.Min(y, wa.Bottom - cardT - cardHpx));
                Location = new Point(x, y);
            }
            catch { /* giữ vị trí mặc định */ }
        }
        // ====== Tự đo kích thước theo nội dung ======
        private static float DoRongChu(string text)
        {
            lock (MeasureGfx)
                return MeasureGfx.MeasureString(text ?? string.Empty, MessageFont, new SizeF(10000f, 100f), SfCenter).Width;
        }
        private void CapNhatKichThuocForm(string text)
        {
            float cardW = _hienDai
                ? Math.Clamp(DoRongChu(text) + HdChuX + 36f, 360f, 620f)     // vòng quay bên trái + chữ bên phải
                : Math.Clamp(DoRongChu(text) + 90f, 280f, 560f);
            Size = new Size((int)Math.Round((cardW + 2f * MarginX) * _s),
                            (int)Math.Round((CardH + MarginTop + MarginBottom) * _s));   // OnSizeChanged sẽ đánh dấu dựng lại
        }
        // ====== Bề mặt DIB ======
        private void ReleaseSurface()
        {
            _g?.Dispose(); _g = null;
            _surface?.Dispose(); _surface = null;
            if (_memDc != IntPtr.Zero)
            {
                if (_oldObj != IntPtr.Zero) SelectObject(_memDc, _oldObj);
                DeleteDC(_memDc);
            }
            if (_hDib != IntPtr.Zero) DeleteObject(_hDib);
            _memDc = _hDib = _oldObj = IntPtr.Zero;
        }
        private void TaoLaiBeMat()
        {
            _surfaceDirty = false;
            ReleaseSurface();
            _pxW = Math.Max(1, Width);
            _pxH = Math.Max(1, Height);
            var bmi = new BitmapInfo
            {
                bmiHeader = new BitmapInfoHeader
                {
                    biSize = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
                    biWidth = _pxW,
                    biHeight = -_pxH,          // âm = top-down, khớp với Bitmap GDI+
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = 0          // BI_RGB
                }
            };
            _hDib = CreateDIBSection(IntPtr.Zero, ref bmi, 0, out IntPtr bits, IntPtr.Zero, 0);
            if (_hDib == IntPtr.Zero || bits == IntPtr.Zero) { _hDib = IntPtr.Zero; return; }
            _memDc = CreateCompatibleDC(IntPtr.Zero);
            _oldObj = SelectObject(_memDc, _hDib);
            _surface = new Bitmap(_pxW, _pxH, _pxW * 4, PixelFormat.Format32bppPArgb, bits);
            _surface.SetResolution(96f, 96f);
            _g = Graphics.FromImage(_surface);
            _g.SmoothingMode = SmoothingMode.AntiAlias;
            _g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            _g.InterpolationMode = InterpolationMode.NearestNeighbor;   // chép nền 1:1
            _g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            TaoDoiTuongDong();
            _staticDirty = true;
        }
        // ====== Dựng sẵn mọi đối tượng GDI+ của phần động ======
        private void TaoDoiTuongDong()
        {
            DisposeDoiTuongDong();
            if (_hienDai) { TaoDoiTuongHienDai(); return; }
            float cardW = CardW;
            float th = RingThickness;
            _ringRect = new RectangleF((cardW - RingSize) / 2f, 24f, RingSize, RingSize);
            _hiRect = RectangleF.Inflate(_ringRect, th * 0.2f, th * 0.2f);
            _discRect = RectangleF.Inflate(_ringRect, -(th / 2f + 4f), -(th / 2f + 4f));
            _barRect = new RectangleF(30f, CardH - 30f, cardW - 60f, 8f);
            // Cung tiến trình dạng ống: bóng đổ + viền tối + thân sáng + vệt sáng
            _arcEdgeBrush = new LinearGradientBrush(_ringRect, AccentDark1, AccentDark2, LinearGradientMode.ForwardDiagonal);
            _arcMainBrush = new LinearGradientBrush(_ringRect, Accent1, Accent2, LinearGradientMode.ForwardDiagonal);
            _arcShadowPen = new Pen(Color.FromArgb(58, 0, 40, 110), th + 1f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            _arcEdgePen = new Pen(_arcEdgeBrush, th) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            _arcMainPen = new Pen(_arcMainBrush, th * 0.68f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            _arcHiPen = new Pen(Color.FromArgb(170, 255, 255, 255), th * 0.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            _glowPen = new Pen(Color.FromArgb(110, 255, 255, 255), th * 0.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            _dotBrush = new SolidBrush(Color.White);
            _pctBrush = new SolidBrush(TextColor);
            _pctEmbossBrush = new SolidBrush(Color.FromArgb(230, 255, 255, 255));
            // Thanh tiến trình dạng ống bóng
            float bh = _barRect.Height;
            _barBrush = new LinearGradientBrush(
                new RectangleF(_barRect.X - 1f, _barRect.Y, _barRect.Width + 2f, bh), Accent1, Accent2, LinearGradientMode.Horizontal);
            _barGlossBrush = new LinearGradientBrush(new RectangleF(0f, _barRect.Y, 10f, bh),
                Color.White, Color.White, LinearGradientMode.Vertical)
            {
                InterpolationColors = new ColorBlend
                {
                    Positions = new[] { 0f, 0.5f, 1f },
                    Colors = new[] { Color.FromArgb(125, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), Color.FromArgb(70, 0, 50, 140) }
                }
            };
            // Vệt sáng chạy trên thanh (brush có sẵn, mỗi khung hình chỉ dịch vị trí)
            _shimmerW = 70f;
            _shimmerBrush = new LinearGradientBrush(new RectangleF(0f, _barRect.Y, _shimmerW, bh),
                Color.Transparent, Color.Transparent, LinearGradientMode.Horizontal)
            {
                InterpolationColors = new ColorBlend
                {
                    Positions = new[] { 0f, 0.5f, 1f },
                    Colors = new[] { Color.FromArgb(0, 255, 255, 255), Color.FromArgb(120, 255, 255, 255), Color.FromArgb(0, 255, 255, 255) }
                }
            };
        }
        private void DisposeDoiTuongDong()
        {
            _arcShadowPen?.Dispose(); _arcShadowPen = null;
            _arcEdgePen?.Dispose(); _arcEdgePen = null;
            _arcMainPen?.Dispose(); _arcMainPen = null;
            _arcHiPen?.Dispose(); _arcHiPen = null;
            _glowPen?.Dispose(); _glowPen = null;
            _arcEdgeBrush?.Dispose(); _arcEdgeBrush = null;
            _arcMainBrush?.Dispose(); _arcMainBrush = null;
            _barBrush?.Dispose(); _barBrush = null;
            _barGlossBrush?.Dispose(); _barGlossBrush = null;
            _shimmerBrush?.Dispose(); _shimmerBrush = null;
            _dotBrush?.Dispose(); _dotBrush = null;
            _pctBrush?.Dispose(); _pctBrush = null;
            _pctEmbossBrush?.Dispose(); _pctEmbossBrush = null;
            if (_hdButSaoChoi != null) { foreach (var p in _hdButSaoChoi) p.Dispose(); _hdButSaoChoi = null; }
        }
        // ====== Lớp nền tĩnh (vẽ 1 lần khi đổi kích thước / đổi thông báo) ======
        private void VeNenTinh()
        {
            if (_hienDai) { VeNenTinhHienDai(); return; }
            _staticDirty = false;
            _bg?.Dispose();
            _bg = new Bitmap(_pxW, _pxH, PixelFormat.Format32bppPArgb);
            _bg.SetResolution(96f, 96f);
            using var g = Graphics.FromImage(_bg);
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            float cardW = CardW;
            float th = RingThickness;
            // --- 1) Bóng đổ mềm 3 lớp (toạ độ cửa sổ, đơn vị logic) ---
            var card = new RectangleF(MarginX, MarginTop, cardW, CardH);
            Color shadowColor(int a) => Color.FromArgb(a, 24, 44, 96);
            VeBongMem(g, new RectangleF(card.X + 2f, card.Y + 14f, card.Width - 4f, card.Height - 4f), CardRadius, shadowColor(62), 6);
            VeBongMem(g, new RectangleF(card.X, card.Y + 6f, card.Width, card.Height - 2f), CardRadius, shadowColor(50), 3);
            VeBongMem(g, new RectangleF(card.X + 1f, card.Y + 1.5f, card.Width - 2f, card.Height - 1f), CardRadius, shadowColor(36), 2);
            // --- Từ đây vẽ theo toạ độ thẻ (gốc = góc trên-trái thẻ) ---
            g.ResetTransform();
            g.ScaleTransform(_s, _s);
            g.TranslateTransform(MarginX, MarginTop);
            // --- 2) Thẻ nổi: gradient thân + vệt sáng phía trên + MỘT viền mảnh duy nhất ---
            var cardRect = new RectangleF(0f, 0f, cardW, CardH);
            using (var cardPath = RoundRect(cardRect, CardRadius))
            using (var body = new LinearGradientBrush(cardRect, Color.White, Color.FromArgb(230, 236, 247), LinearGradientMode.Vertical)
            {
                InterpolationColors = new ColorBlend
                {
                    Positions = new[] { 0f, 0.5f, 1f },
                    Colors = new[] { Color.FromArgb(255, 255, 255), Color.FromArgb(244, 248, 253), Color.FromArgb(230, 236, 247) }
                }
            })
            {
                g.FillPath(body, cardPath);
            }
            var glossRect = new RectangleF(4f, 4f, cardW - 8f, CardH * 0.42f);
            using (var glossPath = RoundRect(glossRect, CardRadius - 3f))
            using (var gloss = new LinearGradientBrush(glossRect, Color.FromArgb(150, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), LinearGradientMode.Vertical))
            {
                g.FillPath(gloss, glossPath);
            }
            using (var borderPath = RoundRect(new RectangleF(0.5f, 0.5f, cardW - 1f, CardH - 1f), CardRadius))
            using (var borderBrush = new LinearGradientBrush(cardRect, Color.FromArgb(34, 60, 90, 150), Color.FromArgb(78, 60, 90, 150), LinearGradientMode.Vertical))
            using (var borderPen = new Pen(borderBrush, 1f))
            {
                g.DrawPath(borderPen, borderPath);
            }
            // --- 3) Rãnh lõm của vòng tiến trình ---
            using (var rimPen = new Pen(Color.FromArgb(30, 40, 70, 130), th + 1.2f))
                g.DrawEllipse(rimPen, _ringRect);
            using (var trackBrush = new LinearGradientBrush(_ringRect, Color.FromArgb(203, 211, 227), Color.FromArgb(246, 248, 252), LinearGradientMode.ForwardDiagonal))
            using (var trackPen = new Pen(trackBrush, th))
                g.DrawEllipse(trackPen, _ringRect);
            // --- 4) Nút tròn nổi ở giữa (bóng + thân + vệt sáng) ---
            for (int i = 0; i < 6; i++)
            {
                var r = RectangleF.Inflate(_discRect, i * 1.1f, i * 1.1f);
                r.Offset(0f, 3f);
                using var sb = new SolidBrush(Color.FromArgb(9, 30, 50, 100));
                g.FillEllipse(sb, r);
            }
            using (var discBrush = new LinearGradientBrush(_discRect, Color.White, Color.FromArgb(228, 235, 246), LinearGradientMode.Vertical))
                g.FillEllipse(discBrush, _discRect);
            var discGloss = new RectangleF(_discRect.X + _discRect.Width * 0.14f, _discRect.Y + 2f, _discRect.Width * 0.72f, _discRect.Height * 0.42f);
            using (var dg = new LinearGradientBrush(discGloss, Color.FromArgb(190, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), LinearGradientMode.Vertical))
                g.FillEllipse(dg, discGloss);
            // --- 5) Chữ khắc nổi ---
            using (var textBrush = new SolidBrush(TextColor))
            using (var subBrush = new SolidBrush(SubTextColor))
            using (var emboss = new SolidBrush(Color.FromArgb(230, 255, 255, 255)))
            {
                VeChu(g, _message, MessageFont, new RectangleF(16f, 114f, cardW - 32f, 28f), textBrush, emboss);
                VeChu(g, "Vui lòng đợi trong giây lát", SmallFont, new RectangleF(16f, 142f, cardW - 32f, 20f), subBrush, emboss);
            }
            // --- 6) Rãnh lõm của thanh tiến trình ---
            using (var barPath = RoundRect(_barRect, _barRect.Height / 2f))
            using (var barTrack = new LinearGradientBrush(_barRect, Color.FromArgb(204, 212, 227), Color.FromArgb(240, 243, 250), LinearGradientMode.Vertical))
                g.FillPath(barTrack, barPath);
        }
        /// <summary>Bóng mềm: vẽ hình bo góc vào bitmap thu nhỏ rồi phóng to nội suy bicubic -> làm mờ gần như miễn phí. g phải ở toạ độ điểm ảnh (không biến đổi).</summary>
        private void VeBongMem(Graphics g, RectangleF rect, float radius, Color color, int down)
        {
            int sw = Math.Max(1, (int)Math.Ceiling(_pxW / (float)down));
            int sh = Math.Max(1, (int)Math.Ceiling(_pxH / (float)down));
            using var small = new Bitmap(sw, sh, PixelFormat.Format32bppPArgb);
            small.SetResolution(96f, 96f);
            using (var sg = Graphics.FromImage(small))
            {
                sg.SmoothingMode = SmoothingMode.AntiAlias;
                sg.ScaleTransform(_s / down, _s / down);
                using var path = RoundRect(rect, radius);
                using var br = new SolidBrush(color);
                sg.FillPath(br, path);
            }
            var old = g.InterpolationMode;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(small, new Rectangle(0, 0, sw * down, sh * down), 0, 0, sw, sh, GraphicsUnit.Pixel);
            g.InterpolationMode = old;
        }
        private static void VeChu(Graphics g, string text, Font font, RectangleF rect, Brush main, Brush? emboss)
        {
            if (emboss != null)
            {
                var r = rect;
                r.Offset(0f, 1f);
                g.DrawString(text, font, emboss, r, SfCenter);   // lớp sáng lệch 1px xuống dưới -> chữ như khắc nổi
            }
            g.DrawString(text, font, main, rect, SfCenter);
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
            if (_fade < 1f) _fade = Math.Min(1f, _fade + dt * 6f);
            RenderFrame();
        }
        // ====== Vẽ 1 khung hình ======
        private void RenderFrame()
        {
            if (!IsHandleCreated || IsDisposed) return;
            if (_surfaceDirty) TaoLaiBeMat();
            if (_staticDirty) VeNenTinh();
            var g = _g;
            var bg = _bg;
            if (g == null || bg == null || _memDc == IntPtr.Zero) return;
            // Chép nền tĩnh (SourceCopy = ghi đè hoàn toàn, kể cả kênh alpha)
            g.ResetTransform();
            g.CompositingMode = CompositingMode.SourceCopy;
            g.DrawImage(bg, new Rectangle(0, 0, _pxW, _pxH), 0, 0, _pxW, _pxH, GraphicsUnit.Pixel);
            g.CompositingMode = CompositingMode.SourceOver;
            // Phần động (toạ độ thẻ)
            g.ScaleTransform(_s, _s);
            g.TranslateTransform(MarginX, MarginTop);
            if (_hienDai) VeDongHienDai(g);
            else
            {
                VeVong(g);
                VeThanhTienTrinh(g);
            }
            DayLenManHinh();
        }
        // =====================================================================================
        //  KIỂU "HIỆN ĐẠI": thẻ ngang phẳng kiểu web (vòng quay mảnh bên trái, chữ + thanh tiến trình mảnh bên phải)
        //  Cùng cơ chế tối ưu: nền tĩnh vẽ 1 lần, mỗi khung chỉ vẽ cung quay + thanh + % (đối tượng GDI+ dựng sẵn).
        // =====================================================================================
        private const float HdVongX = 26f, HdVongY = 26f, HdVongCo = 46f, HdVongDay = 4.5f;
        private const float HdChuX = HdVongX + HdVongCo + 20f;
        private static readonly Color HdTim = Color.FromArgb(124, 58, 237);
        private static readonly Color HdXanh = Color.FromArgb(59, 130, 246);
        private static readonly Color HdChuDam = Color.FromArgb(15, 23, 42);
        private static readonly Color HdChuPhu = Color.FromArgb(100, 116, 139);
        private static readonly Font HdFontThongBao = new Font("Segoe UI Semibold", 10.5F, FontStyle.Regular);
        private static readonly Font HdFontPhu = new Font(Module_HeThong.TenFontHeThong, 8.5F, FontStyle.Regular);
        private static readonly Font HdFontPhanTram = new Font("Segoe UI Semibold", 9F, FontStyle.Regular);
        private static readonly StringFormat HdSfTrai = new StringFormat(StringFormatFlags.NoWrap)
        { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        private static readonly StringFormat HdSfPhai = new StringFormat(StringFormatFlags.NoWrap)
        { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
        private RectangleF _hdPctRect;
        private const int HdSoDoan = 24;
        private Pen[]? _hdButSaoChoi;
        private void TaoDoiTuongHienDai()
        {
            float cardW = CardW;
            _ringRect = new RectangleF(HdVongX, HdVongY, HdVongCo, HdVongCo);
            float barY = CardH - 30f;
            _barRect = new RectangleF(HdChuX, barY, cardW - HdChuX - 26f - 44f, 6f);
            _hdPctRect = new RectangleF(_barRect.Right + 6f, barY - 7f, 38f, 20f);
            // Vòng quay "sao chổi": HdSoDoan đoạn cung nối nhau, đuôi trong suốt -> đầu đậm, màu chuyển tím -> xanh.
            // Bút dựng sẵn 1 lần (mỗi khung hình chỉ vẽ, không cấp phát).
            _hdButSaoChoi = new Pen[HdSoDoan];
            for (int i = 0; i < HdSoDoan; i++)
            {
                float t = (i + 1f) / HdSoDoan;                       // 0 = đuôi, 1 = đầu
                Color c = Color.FromArgb((int)(255 * t * t),
                    (int)(HdTim.R + (HdXanh.R - HdTim.R) * t), (int)(HdTim.G + (HdXanh.G - HdTim.G) * t), (int)(HdTim.B + (HdXanh.B - HdTim.B) * t));
                _hdButSaoChoi[i] = new Pen(c, HdVongDay) { StartCap = LineCap.Flat, EndCap = i == HdSoDoan - 1 ? LineCap.Round : LineCap.Flat };
            }
            _pctBrush = new SolidBrush(HdTim);
            _barBrush = new LinearGradientBrush(new RectangleF(_barRect.X - 1f, _barRect.Y, _barRect.Width + 2f, _barRect.Height),
                HdTim, HdXanh, LinearGradientMode.Horizontal);
            _shimmerW = 60f;
            _shimmerBrush = new LinearGradientBrush(new RectangleF(0f, _barRect.Y, _shimmerW, _barRect.Height),
                Color.Transparent, Color.Transparent, LinearGradientMode.Horizontal)
            {
                InterpolationColors = new ColorBlend
                {
                    Positions = new[] { 0f, 0.5f, 1f },
                    Colors = new[] { Color.FromArgb(0, 255, 255, 255), Color.FromArgb(150, 255, 255, 255), Color.FromArgb(0, 255, 255, 255) }
                }
            };
        }
        private void VeNenTinhHienDai()
        {
            _staticDirty = false;
            _bg?.Dispose();
            _bg = new Bitmap(_pxW, _pxH, PixelFormat.Format32bppPArgb);
            _bg.SetResolution(96f, 96f);
            using var g = Graphics.FromImage(_bg);
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            float cardW = CardW;
            // Bóng đổ mềm, nhẹ (kiểu web)
            var card = new RectangleF(MarginX, MarginTop, cardW, CardH);
            VeBongMem(g, new RectangleF(card.X + 4f, card.Y + 12f, card.Width - 8f, card.Height - 6f), CardRadius, Color.FromArgb(46, 15, 23, 42), 6);
            VeBongMem(g, new RectangleF(card.X, card.Y + 3f, card.Width, card.Height - 1f), CardRadius, Color.FromArgb(30, 15, 23, 42), 2);
            g.ResetTransform();
            g.ScaleTransform(_s, _s);
            g.TranslateTransform(MarginX, MarginTop);
            // Thẻ trắng phẳng + viền mảnh
            var cardRect = new RectangleF(0f, 0f, cardW, CardH);
            using (var p = RoundRect(cardRect, CardRadius))
            using (var nen = new SolidBrush(Color.White))
                g.FillPath(nen, p);
            using (var p = RoundRect(new RectangleF(0.5f, 0.5f, cardW - 1f, CardH - 1f), CardRadius))
            using (var vien = new Pen(Color.FromArgb(226, 232, 240), 1f))
                g.DrawPath(vien, p);
            // (Không vẽ vòng nền / chấm giữa: vòng quay "sao chổi" vẽ ở phần động, nhìn gọn kiểu web)
            // Chữ
            float rongChu = cardW - HdChuX - 26f;
            using (var b1 = new SolidBrush(HdChuDam))
                g.DrawString(_message, HdFontThongBao, b1, new RectangleF(HdChuX, 22f, rongChu, 24f), HdSfTrai);
            using (var b2 = new SolidBrush(HdChuPhu))
                g.DrawString("Vui lòng đợi trong giây lát…", HdFontPhu, b2, new RectangleF(HdChuX, 46f, rongChu, 18f), HdSfTrai);
            // Rãnh thanh tiến trình
            using (var p = RoundRect(_barRect, _barRect.Height / 2f))
            using (var ranhThanh = new SolidBrush(Color.FromArgb(241, 245, 249)))
                g.FillPath(ranhThanh, p);
        }
        private void VeDongHienDai(Graphics g)
        {
            // Vòng quay "sao chổi": cung dài 270°, đuôi mờ dần, đầu tròn đậm, quay đều
            if (_hdButSaoChoi != null)
            {
                const float tongDai = 270f;
                float moiDoan = tongDai / HdSoDoan;
                float dau = (_spinAngle * 1.25f) % 360f;            // vị trí đầu sao chổi
                for (int i = 0; i < HdSoDoan; i++)
                {
                    float batDau = dau - tongDai + i * moiDoan;
                    g.DrawArc(_hdButSaoChoi[i], _ringRect, batDau, moiDoan + 0.6f);   // +0.6°: nối liền, không hở khe
                }
            }
            // Thanh tiến trình mảnh + vệt sáng chạy
            float h = _barRect.Height;
            float w = _barRect.Width * Math.Clamp(_displayPercent, 0f, 100f) / 100f;
            if (w >= h)
            {
                var fill = new RectangleF(_barRect.X, _barRect.Y, w, h);
                using var p = RoundRect(fill, h / 2f);
                g.FillPath(_barBrush!, p);
                g.SetClip(p);
                float x = _barRect.X + (_spinAngle / 360f) * (w + _shimmerW) - _shimmerW;
                _shimmerBrush!.ResetTransform();
                _shimmerBrush.TranslateTransform(x, 0f);
                g.FillRectangle(_shimmerBrush, x, _barRect.Y, _shimmerW, h);
                g.ResetClip();
            }
            g.DrawString(_pctText, HdFontPhanTram, _pctBrush!, _hdPctRect, HdSfPhai);
        }
        private void DayLenManHinh()
        {
            var dst = new NativePoint { X = Left, Y = Top };
            var size = new NativeSize { Cx = _pxW, Cy = _pxH };
            var src = new NativePoint();
            var blend = new BlendFunction
            {
                BlendOp = 0,   // AC_SRC_OVER
                BlendFlags = 0,
                SourceConstantAlpha = (byte)Math.Clamp((int)(_fade * 255f), 0, 255),
                AlphaFormat = 1   // AC_SRC_ALPHA: dùng alpha từng điểm ảnh (premultiplied)
            };
            UpdateLayeredWindow(Handle, IntPtr.Zero, ref dst, ref size, _memDc, ref src, 0, ref blend, ULW_ALPHA);
        }
        private void VeVong(Graphics g)
        {
            float pct = Math.Clamp(_displayPercent, 0f, 100f);
            float sweep = pct * 3.6f;
            if (sweep > 0.5f)
            {
                var sr = _ringRect;
                sr.Offset(0f, 1.6f);
                g.DrawArc(_arcShadowPen!, sr, -90f, sweep);          // bóng nhẹ dưới ống
                g.DrawArc(_arcEdgePen!, _ringRect, -90f, sweep);      // viền tối của ống
                g.DrawArc(_arcMainPen!, _ringRect, -90f, sweep);      // thân ống (gradient)
                g.DrawArc(_arcHiPen!, _hiRect, -90f, sweep);          // vệt sáng mép ngoài
            }
            // Vệt sáng chạy quanh vòng (chỉ trong phần đã hoàn thành)
            if (sweep > 20f)
            {
                float start = -90f + (_spinAngle % sweep);
                float len = Math.Min(24f, sweep - (start + 90f));
                if (len > 2f) g.DrawArc(_glowPen!, _ringRect, start, len);
            }
            // Chấm đầu mút
            if (sweep > 2f && pct < 100f)
            {
                double a = (-90f + sweep) * Math.PI / 180.0;
                float cx = _ringRect.X + _ringRect.Width / 2f + (float)Math.Cos(a) * _ringRect.Width / 2f;
                float cy = _ringRect.Y + _ringRect.Height / 2f + (float)Math.Sin(a) * _ringRect.Height / 2f;
                const float d = 3.4f;
                g.FillEllipse(_dotBrush!, cx - d / 2f, cy - d / 2f, d, d);
            }
            // Số phần trăm ở giữa nút nổi
            VeChu(g, _pctText, PercentFont, _discRect, _pctBrush!, _pctEmbossBrush);
        }
        private void VeThanhTienTrinh(Graphics g)
        {
            float h = _barRect.Height;
            float w = _barRect.Width * Math.Clamp(_displayPercent, 0f, 100f) / 100f;
            if (w < h) return;
            var fill = new RectangleF(_barRect.X, _barRect.Y, w, h);
            using var p = RoundRect(fill, h / 2f);
            g.FillPath(_barBrush!, p);
            g.FillPath(_barGlossBrush!, p);   // vệt sáng trên + bóng tối dưới -> dạng ống
            // Vệt sáng chạy qua thanh (cắt gọn trong phần đã đầy)
            g.SetClip(p);
            float phase = _spinAngle / 360f;
            float x = _barRect.X + phase * (w + _shimmerW) - _shimmerW;
            _shimmerBrush!.ResetTransform();
            _shimmerBrush.TranslateTransform(x, 0f);
            g.FillRectangle(_shimmerBrush, x, _barRect.Y, _shimmerW, h);
            g.ResetClip();
        }
        private static GraphicsPath RoundRect(RectangleF b, float radius)
        {
            float d = Math.Min(radius * 2f, Math.Min(b.Width, b.Height));
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
            _message = txt ?? string.Empty;
            CapNhatKichThuocForm(_message);
            _staticDirty = true;                  // chữ nằm trong lớp nền tĩnh -> vẽ lại (kể cả khi kích thước không đổi)
            if (Visible)
            {
                CanGiuaTheoFormCha();
                RenderFrame();
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer?.Dispose();
                DisposeDoiTuongDong();
                _bg?.Dispose(); _bg = null;
                ReleaseSurface();
                // Font/StringFormat là static dùng chung -> KHÔNG Dispose ở đây
            }
            base.Dispose(disposing);
        }
    }
}
/////TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026