using Microsoft.Data.Sqlite;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
 
namespace PhanMemThiDua2026
{
    /// <summary>
    /// ContextMenuStrip phong cách hiện đại (flat, bo góc, hover bo tròn, fade-in mượt).
    /// Cách dùng giữ nguyên: Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1);
    /// </summary>
    internal static class Module_MenuChuotPhai
    {
        // Cache tên màu đang chọn trên RAM để tránh đọc ổ cứng liên tục khi mở menu
        public static string MauHienTai { get; set; } = "Mặc định";

        // ===== Tuỳ chỉnh nhanh =====
        private const int BanKinhBoGoc = 10;     // bo góc khung menu
        private const int BanKinhHover = 6;      // bo góc ô hover
        private const int TocDoFadeMs = 12;      // chu kỳ timer (nhỏ = mượt hơn)
        private const double BuocFade = 0.18;    // mỗi tick tăng bao nhiêu độ mờ
        private static readonly Font FontMenu = new Font(Module_HeThong.TenFontHeThong, 10f, FontStyle.Regular);

        // Đảm bảo mỗi dropdown chỉ gắn hiệu ứng 1 lần
        private static readonly ConditionalWeakTable<ToolStripDropDown, object> _daGan = new();

        public static void TichHopGiaoDien(ContextMenuStrip menu)
        {
            if (menu == null) return;

            GanHieuUng(menu);

            menu.Opening += (s, ev) =>
            {
                if (ev.Cancel) return;
                AreStyle(menu, LayChuDe(MauHienTai));
            };
        }

        // Tự động đọc màu đã lưu từ CSDL2 khi khởi động phần mềm
        public static void KhoiTaoMauTuCSDL(string csdl2Path)
        {
            try
            {
                if (!System.IO.File.Exists(csdl2Path)) return;
                using var conn = new SqliteConnection($"Data Source={csdl2Path};Mode=ReadOnly");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT MauSacNguoiDungChon FROM MauSacMenuHeThong WHERE ID = 1 LIMIT 1;";
                var result = cmd.ExecuteScalar();
                if (result != null && !string.IsNullOrWhiteSpace(result.ToString()))
                {
                    MauHienTai = result.ToString()!.Trim();
                }
            }
            catch { MauHienTai = "Mặc định"; }
        }

        // Giữ lại để tương thích code cũ
        public static ProfessionalColorTable LayBangMauTheoTen(string tenMau) => new ThemeColorTable(LayChuDe(tenMau));


        //  CHỦ ĐỀ MÀU

        public static MenuTheme LayChuDe(string tenMau)
        {
            return tenMau switch
            {
                "Xanh lá" => new MenuTheme(
                    back: Color.FromArgb(232, 247, 236), hover: Color.FromArgb(165, 224, 184),
                    border: Color.FromArgb(110, 190, 140), text: Color.FromArgb(14, 64, 34),
                    disabled: Color.FromArgb(120, 160, 135), separator: Color.FromArgb(190, 228, 203),
                    accent: Color.FromArgb(22, 163, 74)),

                "Xanh dương" => new MenuTheme(
                    back: Color.FromArgb(232, 242, 255), hover: Color.FromArgb(162, 200, 250),
                    border: Color.FromArgb(110, 160, 230), text: Color.FromArgb(12, 40, 90),
                    disabled: Color.FromArgb(125, 150, 190), separator: Color.FromArgb(190, 213, 246),
                    accent: Color.FromArgb(37, 99, 235)),

                "Xám trắng" => new MenuTheme(
                    back: Color.FromArgb(245, 246, 248), hover: Color.FromArgb(214, 218, 226),
                    border: Color.FromArgb(190, 195, 205), text: Color.FromArgb(30, 34, 42),
                    disabled: Color.FromArgb(150, 155, 165), separator: Color.FromArgb(218, 222, 229),
                    accent: Color.FromArgb(71, 85, 105)),

                "Tối" => new MenuTheme(
                    back: Color.FromArgb(24, 26, 33), hover: Color.FromArgb(60, 68, 92),
                    border: Color.FromArgb(70, 76, 96), text: Color.FromArgb(235, 237, 242),
                    disabled: Color.FromArgb(115, 120, 132), separator: Color.FromArgb(50, 54, 66),
                    accent: Color.FromArgb(122, 162, 255)),

                // "Mặc định" hoặc giá trị trống: vàng hổ phách ấm
                _ => new MenuTheme(
                    back: Color.FromArgb(255, 248, 225), hover: Color.FromArgb(255, 224, 130),
                    border: Color.FromArgb(240, 200, 100), text: Color.FromArgb(74, 52, 0),
                    disabled: Color.FromArgb(180, 160, 110), separator: Color.FromArgb(245, 225, 160),
                    accent: Color.FromArgb(245, 158, 11)),
            };
        }

        //  ÁP STYLE + HIỆU ỨNG

        private static void AreStyle(ToolStripDropDown dd, MenuTheme theme)
        {
            dd.RenderMode = ToolStripRenderMode.Professional;
            dd.Renderer = new ModernMenuRenderer(theme);
            dd.Font = FontMenu;
            dd.BackColor = theme.Back;

            if (dd is ToolStripDropDownMenu ddm)
            {
                ddm.ShowCheckMargin = false;
                ddm.ShowImageMargin = ddm.Items.OfType<ToolStripMenuItem>().Any(i => i.Image != null);
            }

            foreach (ToolStripItem item in dd.Items)
            {
                if (item is ToolStripSeparator) continue;
                item.Padding = new Padding(6, 6, 6, 6); // item cao thoáng hơn
                item.ForeColor = item.Enabled ? theme.Text : theme.Disabled;

                if (item is ToolStripMenuItem mi && mi.HasDropDownItems)
                {
                    GanHieuUng(mi.DropDown);
                    AreStyle(mi.DropDown, theme); // đệ quy cho submenu
                }
            }
        }

        private static void GanHieuUng(ToolStripDropDown dd)
        {
            if (_daGan.TryGetValue(dd, out _)) return;
            _daGan.Add(dd, new object());

            var timer = new System.Windows.Forms.Timer { Interval = TocDoFadeMs };
            timer.Tick += (s, e) =>
            {
                double op = dd.Opacity + BuocFade;
                if (op >= 1.0) { dd.Opacity = 1.0; timer.Stop(); }
                else dd.Opacity = op;
            };

            dd.Opening += (s, e) =>
            {
                if (e.Cancel) return;
                timer.Stop();
                dd.Opacity = 0.0; // bắt đầu trong suốt
            };

            dd.Opened += (s, e) =>
            {
                // Bo góc khung menu
                try
                {
                    using var path = RoundRect(new Rectangle(0, 0, dd.Width, dd.Height), BanKinhBoGoc);
                    var old = dd.Region;
                    dd.Region = new Region(path);
                    old?.Dispose();
                }
                catch { }
                timer.Start(); // fade-in
            };

            dd.Closed += (s, e) =>
            {
                timer.Stop();
                dd.Opacity = 1.0;
            };
        }

        internal static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            int d = Math.Max(1, radius * 2);
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }


        //  RENDERER HIỆN ĐẠI

        private sealed class ModernMenuRenderer : ToolStripProfessionalRenderer
        {
            private readonly MenuTheme _t;

            public ModernMenuRenderer(MenuTheme t) : base(new ThemeColorTable(t))
            {
                _t = t;
                RoundedEdges = false;
            }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using var b = new SolidBrush(_t.Back);
                e.Graphics.FillRectangle(b, e.AffectedBounds);
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                if (e.ToolStrip is not ToolStripDropDown) { base.OnRenderToolStripBorder(e); return; }
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                using var path = RoundRect(r, BanKinhBoGoc);
                using var pen = new Pen(_t.Border, 1f);
                g.DrawPath(pen, path);
            }

            // Bỏ cột lề icon kiểu cũ -> phẳng, hiện đại
            protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) { }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (!e.Item.IsOnDropDown) { base.OnRenderMenuItemBackground(e); return; }
                if (!e.Item.Selected || !e.Item.Enabled) return;

                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(5, 2, e.Item.Width - 10, e.Item.Height - 4);
                using var path = RoundRect(r, BanKinhHover);
                using var b = new SolidBrush(_t.Hover);
                g.FillPath(b, path);
            }

            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                int y = e.Item.Height / 2;
                using var pen = new Pen(_t.Separator, 1f);
                e.Graphics.DrawLine(pen, 14, y, e.Item.Width - 14, y);
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = e.Item.Enabled ? _t.Text : _t.Disabled;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                base.OnRenderItemText(e);
            }

            protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
            {
                e.ArrowColor = e.Item.Enabled ? _t.Text : _t.Disabled;
                base.OnRenderArrow(e);
            }

            protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var r = e.ImageRectangle;
                r.Inflate(2, 2);
                using (var path = RoundRect(r, 4))
                using (var b = new SolidBrush(_t.Accent))
                    g.FillPath(b, path);

                using var pen = new Pen(Color.White, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawLines(pen, new[]
                {
                    new Point(r.Left + r.Width / 4, r.Top + r.Height / 2),
                    new Point(r.Left + r.Width * 2 / 5, r.Bottom - r.Height / 3),
                    new Point(r.Right - r.Width / 4, r.Top + r.Height / 3)
                });
            }
        }


        //  MODEL MÀU + BẢNG MÀU (tương thích ProfessionalColorTable)

        public sealed class MenuTheme
        {
            public Color Back, Hover, Border, Text, Disabled, Separator, Accent;
            public MenuTheme(Color back, Color hover, Color border, Color text,
                             Color disabled, Color separator, Color accent)
            {
                Back = back; Hover = hover; Border = border; Text = text;
                Disabled = disabled; Separator = separator; Accent = accent;
            }
        }

        private sealed class ThemeColorTable : ProfessionalColorTable
        {
            private readonly MenuTheme _t;
            public ThemeColorTable(MenuTheme t) { _t = t; UseSystemColors = false; }

            public override Color ImageMarginGradientBegin => _t.Back;
            public override Color ImageMarginGradientMiddle => _t.Back;
            public override Color ImageMarginGradientEnd => _t.Back;
            public override Color ToolStripDropDownBackground => _t.Back;
            public override Color MenuBorder => _t.Border;
            public override Color MenuItemSelected => _t.Hover;
            public override Color MenuItemSelectedGradientBegin => _t.Hover;
            public override Color MenuItemSelectedGradientEnd => _t.Hover;
            public override Color MenuItemBorder => _t.Hover;
            public override Color SeparatorDark => _t.Separator;
            public override Color SeparatorLight => _t.Back;
        }
    }
}

