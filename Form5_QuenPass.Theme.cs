using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace PhanMemThiDua2026
{
    /// <summary>
    /// Phần giao diện (theme) kiểu web cho Form5_QuenPass.
    /// Chỉ chứa code trình bày, không đụng tới logic nghiệp vụ.
    /// </summary>
    public partial class Form5_QuenPass
    {
        // Bảng màu (lấy cảm hứng từ Tailwind)
        private static readonly Color MauChinh = Color.FromArgb(37, 99, 235);   // blue-600
        private static readonly Color MauChinhHover = Color.FromArgb(29, 78, 216);   // blue-700
        private static readonly Color MauChinhNhan = Color.FromArgb(30, 64, 175);   // blue-800
        private static readonly Color MauChu = Color.FromArgb(15, 23, 42);     // slate-900
        private static readonly Color MauVien = Color.FromArgb(203, 213, 225);  // slate-300
        private static readonly Color MauNenReadOnly = Color.FromArgb(241, 245, 249);  // slate-100

        /// <summary>
        /// WS_EX_COMPOSITED: vẽ toàn bộ control con qua một bộ đệm chung,
        /// hết giật/nhấp nháy khi rê chuột hoặc khi form vừa hiện lên.
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }

        /// <summary>Gọi một lần ngay sau InitializeComponent().</summary>
        private void ApplyWebTheme()
        {
            StyleInput(TextBox1_TraLoi1, readOnly: false);
            StyleInput(TextBox2_TraLoi2, readOnly: false);
            StyleInput(Text_TenTaiKhoan, readOnly: true);
            StyleInput(Text_MatKhauLayLai, readOnly: true);
            StylePrimaryButton(Btn_KiemTra);
        }

        private void StyleInput(Krypton.Toolkit.KryptonTextBox tb, bool readOnly)
        {
            tb.StateCommon.Back.Color1 = readOnly ? MauNenReadOnly : Color.White;
            tb.StateCommon.Content.Color1 = MauChu;
            tb.StateCommon.Content.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            tb.StateCommon.Content.Padding = new Padding(6, 5, 6, 5);
            tb.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            tb.StateCommon.Border.Color1 = MauVien;
            tb.StateCommon.Border.Width = 1;
            tb.StateCommon.Border.Rounding = 10F;

            if (!readOnly)
            {
                // Viền xanh khi ô đang được focus (giống :focus trên web)
                tb.StateActive.Border.Color1 = MauChinh;
            }
        }

        private void StylePrimaryButton(Krypton.Toolkit.KryptonButton btn)
        {
            btn.StateCommon.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn.StateCommon.Back.Color1 = MauChinh;
            btn.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            btn.StateCommon.Border.Color1 = MauChinh;
            btn.StateCommon.Border.Width = 1;
            btn.StateCommon.Border.Rounding = 10F;
            btn.StateCommon.Content.ShortText.Color1 = Color.White;
            btn.StateCommon.Content.ShortText.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);

            // Hover / nhấn
            btn.StateTracking.Back.Color1 = MauChinhHover;
            btn.StateTracking.Border.Color1 = MauChinhHover;
            btn.StatePressed.Back.Color1 = MauChinhNhan;
            btn.StatePressed.Border.Color1 = MauChinhNhan;

            // Nút là AcceptButton nên Krypton dùng thêm trạng thái "default"/"focus"
            btn.OverrideDefault.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn.OverrideDefault.Back.Color1 = MauChinh;
            btn.OverrideDefault.Border.Color1 = MauChinh;
            btn.OverrideDefault.Border.Rounding = 10F;
            btn.OverrideFocus.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn.OverrideFocus.Back.Color1 = MauChinh;
            btn.OverrideFocus.Border.Color1 = MauChinh;
            btn.OverrideFocus.Border.Rounding = 10F;
        }
    }

    /// <summary>
    /// Panel bo góc (dùng làm "card" và vòng tròn biểu tượng).
    /// Bo góc bằng Region nên các control con cũng bị cắt theo.
    /// </summary>
    public class WebCardPanel : Panel
    {
        private int _cornerRadius = 16;

        public WebCardPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw, true);
        }

        [DefaultValue(16)]
        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = Math.Max(0, value); UpdateRegion(); }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRegion();
        }

        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0) return;

            var old = Region;
            if (_cornerRadius <= 0)
            {
                Region = null;
            }
            else
            {
                using var path = RoundedRect(new Rectangle(0, 0, Width, Height), _cornerRadius);
                Region = new Region(path);
            }
            old?.Dispose();
        }

        internal static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            var path = new GraphicsPath();
            if (d <= 0)
            {
                path.AddRectangle(r);
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    /// <summary>
    /// Panel nền gradient xanh → tím kèm vài hình tròn mờ trang trí (kiểu hero banner của web).
    /// </summary>
    public class HeroGradientPanel : Panel
    {
        public HeroGradientPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var rc = ClientRectangle;
            if (rc.Width <= 0 || rc.Height <= 0) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var br = new LinearGradientBrush(rc,
                       Color.FromArgb(37, 99, 235),    // blue-600
                       Color.FromArgb(124, 58, 237),   // violet-600
                       125F))
            {
                g.FillRectangle(br, rc);
            }

            // Hình tròn trang trí, trong suốt nhẹ
            using (var soft = new SolidBrush(Color.FromArgb(26, 255, 255, 255)))
            using (var softer = new SolidBrush(Color.FromArgb(16, 255, 255, 255)))
            {
                g.FillEllipse(soft, rc.Right - 150, rc.Y - 90, 240, 240);
                g.FillEllipse(softer, rc.X - 110, rc.Bottom - 190, 300, 300);
                g.FillEllipse(softer, rc.Right - 70, rc.Bottom - 130, 120, 120);
            }
        }
    }

    /// <summary>Panel có double-buffer.</summary>
    public class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            DoubleBuffered = true;
        }
    }

    /// <summary>TableLayoutPanel có double-buffer (mặc định của WinForms bị nháy khi vẽ lại).</summary>
    public class BufferedTableLayoutPanel : TableLayoutPanel
    {
        public BufferedTableLayoutPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            DoubleBuffered = true;
        }
    }

    /// <summary>
    /// Khung ô nhập bo góc, có viền xám và đổi sang viền xanh khi control bên trong đang focus.
    /// Dùng để bọc ComboBox (vẫn là ComboBox gốc nên code cũ không đổi).
    /// Control con được căn giữa theo chiều dọc.
    /// </summary>
    public class WebFieldPanel : Panel
    {
        private static readonly Color MauVien = Color.FromArgb(203, 213, 225);
        private static readonly Color MauFocus = Color.FromArgb(37, 99, 235);

        public WebFieldPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            e.Control!.Enter += (s, a) => Invalidate();
            e.Control.Leave += (s, a) => Invalidate();
            e.Control.GotFocus += (s, a) => Invalidate();
            e.Control.LostFocus += (s, a) => Invalidate();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            foreach (Control c in Controls)
            {
                int w = Math.Max(0, Width - Padding.Horizontal);
                int y = Math.Max(0, (Height - c.Height) / 2);
                c.SetBounds(Padding.Left, y, w, c.Height);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            if (Width < 4 || Height < 4) return;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            bool focus = ContainsFocus;
            using var pen = new Pen(focus ? MauFocus : MauVien, focus ? 1.6F : 1F);
            using var path = WebCardPanel.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 10);
            g.DrawPath(pen, path);
        }
    }
}