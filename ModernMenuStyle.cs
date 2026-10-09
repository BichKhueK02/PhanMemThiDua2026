using System.Drawing.Drawing2D;

namespace PhanMemThiDua2026
{
    /// <summary>
    /// Giao diện phẳng, hiện đại (kiểu thanh điều hướng của trang web) cho MenuStrip.
    /// Cách dùng:
    ///   1) Sau InitializeComponent():   ModernMenuStyle.Apply(menuStrip1);
    ///   2) Khi chọn một mục, đánh dấu "tab đang mở":
    ///          ModernMenuStyle.SetActive(menuStrip1, quanLyThiDuaBaNhat_ToolStripMenuItem);
    /// Có thể dùng lại cho mọi MenuStrip khác trong phần mềm.
    /// </summary>
    internal static class ModernMenuStyle
    {
        // ---- Bảng màu (đổi tại đây để đổi toàn bộ phần mềm) ----
        public static readonly Color Surface = Color.White;
        public static readonly Color BorderColor = Color.FromArgb(229, 231, 235);
        public static readonly Color TextColor = Color.FromArgb(55, 65, 81);
        public static readonly Color TextDisabled = Color.FromArgb(156, 163, 175);
        public static readonly Color Accent = Color.FromArgb(79, 70, 229);
        public static readonly Color AccentSoft = Color.FromArgb(238, 242, 255);
        public static readonly Color AccentSoftHover = Color.FromArgb(224, 231, 255);
        public static readonly Color HoverFill = Color.FromArgb(243, 244, 246);

        public const float MenuFontSize = 10.5F;
        public const int MenuHeight = 44;

        /// <summary>Áp giao diện hiện đại cho một MenuStrip (gọi sau InitializeComponent).</summary>
        public static void Apply(MenuStrip menu)
        {
            ArgumentNullException.ThrowIfNull(menu);

            menu.Renderer = new ModernMenuRenderer();
            menu.GripStyle = ToolStripGripStyle.Hidden;
            menu.BackColor = Surface;
            menu.ForeColor = TextColor;
            menu.Font = new Font("Segoe UI", MenuFontSize, FontStyle.Regular, GraphicsUnit.Point);
            menu.Padding = new Padding(12, 0, 12, 0);
            menu.AutoSize = false;
            menu.Height = MenuHeight;

            foreach (ToolStripItem item in menu.Items)
            {
                if (item is ToolStripMenuItem mi)
                {
                    mi.Font = menu.Font;
                    mi.ForeColor = TextColor;
                    mi.Padding = new Padding(12, 8, 12, 8);
                    mi.Margin = new Padding(2, 0, 2, 0);
                    mi.AutoSize = true;
                    mi.TextImageRelation = TextImageRelation.ImageBeforeText;
                    mi.ImageScaling = ToolStripItemImageScaling.SizeToFit;
                }
            }
        }

        /// <summary>Đánh dấu mục đang mở (tô nền nhạt + chữ màu nhấn). Truyền null để bỏ chọn tất cả.</summary>
        public static void SetActive(MenuStrip menu, ToolStripMenuItem? active)
        {
            ArgumentNullException.ThrowIfNull(menu);

            foreach (ToolStripItem item in menu.Items)
            {
                if (item is ToolStripMenuItem mi)
                    mi.Checked = ReferenceEquals(mi, active);
            }
            menu.Invalidate();
        }

        // =====================================================================
        //  RENDERER
        // =====================================================================
        private sealed class ModernMenuRenderer : ToolStripProfessionalRenderer
        {
            public ModernMenuRenderer()
            {
                RoundedEdges = false;
            }

            private static bool IsActive(ToolStripItem item) =>
                item is ToolStripMenuItem { Checked: true };

            private static GraphicsPath RoundedRect(Rectangle r, int radius)
            {
                int d = Math.Max(1, radius * 2);
                var path = new GraphicsPath();
                path.AddArc(r.X, r.Y, d, d, 180, 90);
                path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                return path;
            }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using var brush = new SolidBrush(Surface);
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                using var pen = new Pen(BorderColor);
                if (e.ToolStrip is ToolStripDropDown)
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                }
                else
                {
                    // Đường kẻ mảnh dưới thanh menu, giống header của trang web.
                    int y = e.ToolStrip.Height - 1;
                    e.Graphics.DrawLine(pen, 0, y, e.ToolStrip.Width, y);
                }
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                ToolStripItem item = e.Item;
                if (!item.Enabled) return;

                bool active = IsActive(item);
                bool hot = item.Selected || item.Pressed;
                if (!active && !hot) return;

                Color fill = active
                    ? (hot ? AccentSoftHover : AccentSoft)
                    : (item.Pressed ? AccentSoftHover : HoverFill);

                float scale = e.ToolStrip?.DeviceDpi / 96f ?? 1f;
                int radius = (int)Math.Round(8 * scale);

                var g = e.Graphics;
                var oldMode = g.SmoothingMode;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle rect = item.IsOnDropDown
                    ? new Rectangle(4, 1, item.Width - 8, item.Height - 2)
                    : new Rectangle(1, 4, item.Width - 3, item.Height - 9);

                using GraphicsPath path = RoundedRect(rect, radius);
                using var brush = new SolidBrush(fill);
                g.FillPath(brush, path);

                g.SmoothingMode = oldMode;
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                if (!e.Item.Enabled)
                    e.TextColor = TextDisabled;
                else if (IsActive(e.Item) || e.Item.Selected || e.Item.Pressed)
                    e.TextColor = Accent;
                else
                    e.TextColor = TextColor;

                base.OnRenderItemText(e);
            }

            // Không vẽ dấu tick/khung quanh icon khi Checked: trạng thái đã thể hiện bằng nền.
            protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e) { }

            protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) { }

            protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
            {
                e.ArrowColor = e.Item.Enabled ? TextColor : TextDisabled;
                base.OnRenderArrow(e);
            }

            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                using var pen = new Pen(BorderColor);
                Rectangle b = e.Item.ContentRectangle;

                if (e.Vertical)
                {
                    // Vạch đứng ngắn, căn giữa theo chiều cao thanh menu.
                    int inset = Math.Max(6, b.Height / 4);
                    int x = b.Width / 2;
                    e.Graphics.DrawLine(pen, x, b.Top + inset, x, b.Bottom - inset);
                }
                else
                {
                    int y = b.Height / 2;
                    e.Graphics.DrawLine(pen, b.Left + 8, y, b.Right - 8, y);
                }
            }
        }
    }
}