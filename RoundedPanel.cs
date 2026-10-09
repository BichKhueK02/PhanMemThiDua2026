using System.Drawing.Drawing2D;

namespace PhanMemThiDua2026
{
    public class RoundedPanel : Panel
    {
        public int Radius { get; set; } = 12;
        public int BorderWidth { get; set; } = 1;
        public Color BorderColor { get; set; } = Color.FromArgb(180, 180, 230);
        public Color FillColor { get; set; } = Color.White;

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
            Control p = Parent;
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