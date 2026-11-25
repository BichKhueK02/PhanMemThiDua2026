using System.Drawing.Drawing2D;
namespace PhanMemThiDua2026
{
    public class RoundedPanel : Panel
    {
        public int Radius { get; set; } = 12;
        public int BorderWidth { get; set; } = 1;
        public Color BorderColor { get; set; } = Color.FromArgb(180, 180, 230);
        public Color FillColor { get; set; } = Color.White;
        /// <summary>
        /// true: cắt bo góc cả control con nằm trong (vd lưới Dock = Fill) theo bán kính của khung => góc lưới tròn thật,
        /// không lòi 4 góc vuông của tiêu đề cột. Mặc định false (các form cũ không đổi).
        /// </summary>
        [System.ComponentModel.DefaultValue(false)]
        public bool BoGocControlCon { get; set; }
        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (!BoGocControlCon) return;
            foreach (Control c in Controls)
            {
                if (c.Width <= 0 || c.Height <= 0) continue;
                // bán kính trong = bán kính khung - khoảng lề (con nằm sát trong viền)
                int r = Math.Max(2, Radius - Math.Max(Padding.Left, Padding.Top));
                int d = Math.Min(r * 2, Math.Min(c.Width, c.Height));
                using var p = new GraphicsPath();
                p.AddArc(0, 0, d, d, 180, 90);
                p.AddArc(c.Width - d, 0, d, d, 270, 90);
                p.AddArc(c.Width - d, c.Height - d, d, d, 0, 90);
                p.AddArc(0, c.Height - d, d, d, 90, 90);
                p.CloseFigure();
                var cu = c.Region;
                c.Region = new Region(p);
                cu?.Dispose();
            }
        }
        public RoundedPanel()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     //ControlStyles.AllPaintingInBackground |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            Padding = new Padding(10);
        }
        // Lấy màu nền đặc đầu tiên của chuỗi control cha
        private Color LayMauNenCha()
        {
            Control? p = Parent;
            while (p != null)
            {
                if (p.BackColor.A == 255) return p.BackColor;
                p = p.Parent;
            }
            return SystemColors.Control;
        }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(LayMauNenCha());
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int d = Math.Min(Radius * 2, Math.Min(rect.Width, rect.Height));
            using var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            using var fill = new SolidBrush(FillColor);
            using var pen = new Pen(BorderColor, BorderWidth);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(pen, path);
        }
    }
}