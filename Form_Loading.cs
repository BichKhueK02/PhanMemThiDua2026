//using System.Drawing.Drawing2D;
//using System.Runtime.InteropServices;
//namespace PhanMemThiDua2026
//{
//    public class Form_Loading : Form
//    {
//        private Label lblMessage;
//        private System.Windows.Forms.Timer animationTimer;
//        private int currentAngle = 0;
//        private int _currentPercent = 0;
//        private bool _autoSimulate = true;
//        private float _virtualProgress = 0f;
//        // Cấu hình màu sắc UI
//        private readonly Color ThemeColor = Color.FromArgb(41, 128, 185); // Xanh dương
//        private readonly Color TrackColor = Color.FromArgb(220, 220, 220); // Xám nhạt
//        private readonly Color TextColor = Color.FromArgb(64, 64, 64);     // Xám đậm
//        private readonly Font MessageFont = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Regular); // Font chữ thông báo
//        // Khai báo hàm API Windows để bo góc Form
//        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
//        private static extern IntPtr CreateRoundRectRgn(
//            int nLeftRect,
//            int nTopRect,
//            int nRightRect,
//            int nBottomRect,
//            int nWidthEllipse, // Độ rộng của góc bo (Đường kính)
//            int nHeightEllipse // Chiều cao của góc bo (Đường kính)
//        );
//        public Form_Loading(string message = "Đang xử lý dữ liệu...")
//        {
//            // 1. Cấu hình Form
//            this.FormBorderStyle = FormBorderStyle.None;
//            this.StartPosition = FormStartPosition.CenterScreen;
//            this.BackColor = Color.WhiteSmoke;
//            this.ShowInTaskbar = false;
//            this.TopMost = true;
//            this.DoubleBuffered = true;
//            // 2. Khởi tạo Label
//            lblMessage = new Label
//            {
//                Text = message,
//                AutoSize = false,
//                TextAlign = ContentAlignment.MiddleCenter,
//                Font = MessageFont,
//                ForeColor = TextColor,
//                BackColor = Color.Transparent
//            };
//            this.Controls.Add(lblMessage);
//            // 3. Tính toán kích thước tự động (Auto-Size)
//            CapNhatKichThuocForm(message);
//            // 4. Khởi tạo Timer
//            animationTimer = new System.Windows.Forms.Timer { Interval = 15 };
//            animationTimer.Tick += (s, e) =>
//            {
//                currentAngle = (currentAngle + 8) % 360;
//                if (_autoSimulate && _currentPercent < 99)
//                {
//                    _virtualProgress += (99f - _virtualProgress) * 0.015f;
//                    _currentPercent = (int)_virtualProgress;
//                }
//                this.Invalidate();
//            };
//            this.Shown += (s, e) => animationTimer.Start();
//            this.FormClosing += (s, e) => animationTimer.Stop();
//        }
//        // HÀM TỰ ĐỘNG ĐO VÀ ĐIỀU CHỈNH KÍCH THƯỚC FORM
//        private void CapNhatKichThuocForm(string text)
//        {
//            using (Graphics g = this.CreateGraphics())
//            {
//                SizeF textSize = g.MeasureString(text, MessageFont);
//                int newWidth = Math.Max(240, (int)textSize.Width + 80);
//                this.Size = new Size(newWidth, 160);
//                lblMessage.Size = new Size(this.Width - 20, 40);
//                lblMessage.Location = new Point(10, 110);
//                // ⭐ SỬA LỖI 2 VIỀN: Truyền tham số đường kính bo góc là 20
//                this.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, 20, 20));
//            }
//        }
//        protected override void OnPaint(PaintEventArgs e)
//        {
//            base.OnPaint(e);
//            Graphics g = e.Graphics;
//            g.SmoothingMode = SmoothingMode.AntiAlias; // Bật khử răng cưa
//            // ⭐ SỬA LỖI 2 VIỀN: Truyền tham số Bán kính (Radius) là 10 (Đường kính = 20, khớp 100% với hàm Region ở trên)
//            using (Pen borderPen = new Pen(Color.LightGray, 1))
//            {
//                g.DrawPath(borderPen, GetRoundRectPath(new Rectangle(0, 0, this.Width - 1, this.Height - 1), 10));
//            }
//            // --- VẼ VÒNG TRÒN ---
//            int spinnerSize = 65;
//            int penThickness = 6;
//            Rectangle spinnerRect = new Rectangle((this.Width - spinnerSize) / 2, 25, spinnerSize, spinnerSize);
//            // Đường ray (màu xám nhạt)
//            using (Pen trackPen = new Pen(TrackColor, penThickness))
//            {
//                g.DrawEllipse(trackPen, spinnerRect);
//            }
//            // Spinner (vòng cung chạy)
//            using (Pen spinnerPen = new Pen(ThemeColor, penThickness))
//            {
//                spinnerPen.StartCap = LineCap.Round;
//                spinnerPen.EndCap = LineCap.Round;
//                g.DrawArc(spinnerPen, spinnerRect, currentAngle, 100);
//            }
//            // --- VẼ PHẦN TRĂM ---
//            string percentText = $"{_currentPercent}%";
//            using (Font percentFont = new Font(Module_HeThong.TenFontHeThong, 11F, FontStyle.Bold))
//            using (Brush percentBrush = new SolidBrush(ThemeColor))
//            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
//            {
//                g.DrawString(percentText, percentFont, percentBrush, spinnerRect, sf);
//            }
//        }
//        private GraphicsPath GetRoundRectPath(Rectangle bounds, int radius)
//        {
//            GraphicsPath path = new GraphicsPath();
//            int diameter = radius * 2;
//            Size size = new Size(diameter, diameter);
//            Rectangle arc = new Rectangle(bounds.Location, size);
//            path.AddArc(arc, 180, 90);
//            arc.X = bounds.Right - diameter;
//            path.AddArc(arc, 270, 90);
//            arc.Y = bounds.Bottom - diameter;
//            path.AddArc(arc, 0, 90);
//            arc.X = bounds.Left;
//            path.AddArc(arc, 90, 90);
//            path.CloseFigure();
//            return path;
//        }
//        public void CapNhatThongBao(string txt)
//        {
//            if (this.InvokeRequired)
//            {
//                this.Invoke(new Action(() =>
//                {
//                    lblMessage.Text = txt;
//                    CapNhatKichThuocForm(txt);
//                }));
//            }
//            else
//            {
//                lblMessage.Text = txt;
//                CapNhatKichThuocForm(txt);
//            }
//        }
//        public void CapNhatPhanTram(int percent)
//        {
//            _autoSimulate = false;
//            if (percent < 0) percent = 0;
//            if (percent > 100) percent = 100;
//            if (this.InvokeRequired) this.Invoke(new Action(() => { _currentPercent = percent; }));
//            else _currentPercent = percent;
//        }
//    }
//}

using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace PhanMemThiDua2026
{
    /// <summary>
    /// Form loading hiện đại: thẻ trắng bo góc + đổ bóng, vòng tiến trình gradient mượt,
    /// thanh tiến trình mảnh, fade-in. Giữ nguyên API cũ:
    ///   new Form_Loading(msg), CapNhatThongBao(txt), CapNhatPhanTram(percent)
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
        private float _spinAngle = 0f;       // góc quay của hiệu ứng ánh sáng

        // ====== Giao diện ======
        private readonly Color Accent1 = Color.FromArgb(0, 122, 255);   // xanh dương
        private readonly Color Accent2 = Color.FromArgb(0, 210, 255);   // xanh cyan
        private readonly Color CardColor = Color.White;
        private readonly Color TrackColor = Color.FromArgb(234, 238, 244);
        private readonly Color TextColor = Color.FromArgb(55, 65, 81);
        private readonly Color SubTextColor = Color.FromArgb(130, 140, 155);

        private readonly Font MessageFont = new Font(Module_HeThong.TenFontHeThong, 10.5F, FontStyle.Regular);
        private readonly Font PercentFont = new Font(Module_HeThong.TenFontHeThong, 14F, FontStyle.Bold);
        private readonly Font SmallFont = new Font(Module_HeThong.TenFontHeThong, 8.5F, FontStyle.Regular);

        private const int RingSize = 76;
        private const int RingThickness = 7;
        private const int CornerRadius = 16;

        // ====== Win32 / DWM ======
        [DllImport("Gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int l, int t, int r, int b, int w, int h);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;
        private const int CS_DROPSHADOW = 0x00020000;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW; // đổ bóng nhẹ quanh form
                return cp;
            }
        }

        public Form_Loading(string message = "Đang xử lý dữ liệu...")
        {
            _message = message;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = CardColor;
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
            Shown += (s, e) => { _lastTicks = _clock.ElapsedTicks; _timer.Start(); };
            FormClosing += (s, e) => _timer.Stop();
        }

        // ====== Bo góc: ưu tiên DWM (Win11, mịn), dự phòng Region ======
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApDungBoGoc();
        }

        private void ApDungBoGoc()
        {
            if (!IsHandleCreated) return;

            int pref = DWMWCP_ROUND;
            int hr;
            try { hr = DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int)); }
            catch { hr = -1; }

            if (hr != 0) // Windows 10 trở xuống
            {
                int d = CornerRadius * 2;
                Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width + 1, Height + 1, d, d));
            }
        }

        // ====== Tự đo kích thước theo nội dung ======
        private void CapNhatKichThuocForm(string text)
        {
            using (Graphics g = CreateGraphics())
            {
                SizeF ts = g.MeasureString(text, MessageFont);
                int w = Math.Clamp((int)ts.Width + 90, 280, 560);
                Size = new Size(w, 205);
            }
            ApDungBoGoc();
        }

        // ====== Vòng lặp animation (độc lập tốc độ máy) ======
        private void OnTick(object sender, EventArgs e)
        {
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

            // Fade-in
            if (Opacity < 1) Opacity = Math.Min(1, Opacity + dt * 6f);

            Invalidate();
        }

        // ====== Vẽ ======
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Nền thẻ + viền mảnh
            using (var bg = new SolidBrush(CardColor))
                g.FillRectangle(bg, ClientRectangle);
            using (var path = RoundRect(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), CornerRadius))
            using (var border = new Pen(Color.FromArgb(40, 0, 0, 0), 1))
                g.DrawPath(border, path);

            DrawRing(g);
            DrawMessage(g);
            DrawProgressBar(g);
        }

        private void DrawRing(Graphics g)
        {
            var rect = new Rectangle((Width - RingSize) / 2, 26, RingSize, RingSize);
            float pct = Math.Max(0f, Math.Min(100f, _displayPercent));
            float sweep = pct * 3.6f;

            // Đường ray
            using (var track = new Pen(TrackColor, RingThickness))
                g.DrawEllipse(track, rect);

            // Cung tiến trình (gradient)
            if (sweep > 0.5f)
            {
                using (var lg = new LinearGradientBrush(rect, Accent1, Accent2, LinearGradientMode.ForwardDiagonal))
                using (var pen = new Pen(lg, RingThickness) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawArc(pen, rect, -90, sweep);
            }

            // Vệt sáng chạy quanh vòng cho cảm giác "đang xử lý"
            using (var glow = new Pen(Color.FromArgb(150, 255, 255, 255), RingThickness - 3)
            { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                if (sweep > 20f)
                {
                    // chỉ chạy trong phạm vi phần đã hoàn thành
                    float start = -90 + (_spinAngle % sweep);
                    float len = Math.Min(24f, sweep - (start + 90));
                    if (len > 2f) g.DrawArc(glow, rect, start, len);
                }
            }

            // Chấm đầu mút
            if (sweep > 2f && pct < 100f)
            {
                double a = (-90 + sweep) * Math.PI / 180.0;
                float cx = rect.X + rect.Width / 2f + (float)Math.Cos(a) * rect.Width / 2f;
                float cy = rect.Y + rect.Height / 2f + (float)Math.Sin(a) * rect.Height / 2f;
                using (var dot = new SolidBrush(Color.White))
                    g.FillEllipse(dot, cx - 2.5f, cy - 2.5f, 5, 5);
            }

            // Số phần trăm ở giữa
            using (var br = new SolidBrush(TextColor))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString($"{(int)pct}%", PercentFont, br, rect, sf);
        }

        private void DrawMessage(Graphics g)
        {
            var r = new RectangleF(16, 116, Width - 32, 28);
            using (var br = new SolidBrush(TextColor))
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            })
                g.DrawString(_message, MessageFont, br, r, sf);

            using (var br = new SolidBrush(SubTextColor))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString("Vui lòng đợi trong giây lát", SmallFont, br, new RectangleF(16, 140, Width - 32, 20), sf);
        }

        private void DrawProgressBar(Graphics g)
        {
            const int h = 5;
            var bar = new RectangleF(30, Height - 26, Width - 60, h);

            using (var p = RoundRect(bar, h / 2f))
            using (var track = new SolidBrush(TrackColor))
                g.FillPath(track, p);

            float w = bar.Width * Math.Max(0f, Math.Min(100f, _displayPercent)) / 100f;
            if (w >= h)
            {
                var fill = new RectangleF(bar.X, bar.Y, w, h);
                using (var p = RoundRect(fill, h / 2f))
                using (var lg = new LinearGradientBrush(
                           new RectangleF(bar.X - 1, bar.Y, Math.Max(w, 2), h), Accent1, Accent2, LinearGradientMode.Horizontal))
                    g.FillPath(lg, p);
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
            if (InvokeRequired) { Invoke(new Action(() => CapNhatThongBao(txt))); return; }
            _message = txt;
            CapNhatKichThuocForm(txt);
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
                MessageFont.Dispose();
                PercentFont.Dispose();
                SmallFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}