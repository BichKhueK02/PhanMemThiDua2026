using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace PhanMemThiDua2026
{
    /// <summary>
    /// Bo tròn giao diện cho cả form chỉ bằng 1 dòng:
    ///     Module_GiaoDienBoTron.ApDung(this);
    /// Tự xử lý đệ quy mọi control con, kể cả control được thêm vào sau (ControlAdded).
    ///
    /// Đối tượng được bo:
    ///  - GroupBox                         : khung bo tròn, tiêu đề vẫn nằm trên viền
    ///  - Panel / TableLayoutPanel "thẻ"   : Dock = None, có viền hoặc nền khác nền cha
    ///  - DataGridView, ListView, TreeView, RichTextBox, ListBox : cắt góc tròn
    ///  - Mọi control Krypton (nút, ô nhập, group...) : đặt StateCommon.Border.Rounding
    ///
    /// Muốn một control KHÔNG bị bo: đặt Tag = "KhongBoGoc".
    /// </summary>
    internal static class Module_GiaoDienBoTron
    {
        // ===== Tuỳ chỉnh nhanh =====
        public static int BanKinhThe { get; set; } = 12;          // GroupBox, Panel thẻ (px ở 96 DPI)
        public static int BanKinhLuoi { get; set; } = 8;          // DataGridView, ListView...
        public static float BanKinhKrypton { get; set; } = 8f;    // KryptonButton, KryptonTextBox...
        public static Color MauVien { get; set; } = Color.FromArgb(176, 188, 210);
        public const string TheKhongBoGoc = "KhongBoGoc";

        public static Color MauVienGroupBox { get; set; } = Color.FromArgb(192, 192, 255); // xanh lá
        public static float DoDayVienGroupBox { get; set; } = 0.5f;                      // px ở 96 DPI (mỏng hơn: 1f, đậm hơn: 2f)
        private static readonly ConditionalWeakTable<Control, object> _daXuLy = new();

        public static void ApDung(Control? goc)
        {
            if (goc == null || goc.IsDisposed) return;
            XuLy(goc);
        }

        // ------------------------------------------------------------------
        private static void XuLy(Control c)
        {
            if (_daXuLy.TryGetValue(c, out _)) return;
            _daXuLy.Add(c, new object());

            // Control thêm vào sau (form con tạo giao diện lúc Load...) cũng được bo
            c.ControlAdded += (s, e) =>
            {
                if (e.Control != null) XuLy(e.Control);
            };

            if (!(c.Tag is string tag && tag == TheKhongBoGoc))
            {
                try { ApDungKieu(c); }
                catch (Exception ex) { Debug.WriteLine("[BoTron] " + c.Name + ": " + ex.Message); }
            }

            foreach (Control con in c.Controls) XuLy(con);
        }

        private static void ApDungKieu(Control c)
        {
            string? ns = c.GetType().Namespace;
            if (ns != null && ns.StartsWith("Krypton", StringComparison.Ordinal))
            {
                DatRoundingKrypton(c);
                if (c is not DataGridView) return;
            }

            switch (c)
            {
                case Form:
                    return;
                case GroupBox gb:
                    BoGocGroupBox(gb);
                    break;
                case DataGridView or ListView or TreeView or RichTextBox or ListBox:
                    BoGocCoVien(c);
                    break;
                case TabPage:
                    break;
                case Panel p when LaPanelThe(p):
                    BoGocPanel(p);
                    break;
            }
        }

        // ------------------------------------------------------------------
        //  KRYPTON  (dùng reflection để không phụ thuộc phiên bản Krypton)
        // ------------------------------------------------------------------
        private static void DatRoundingKrypton(Control c)
        {
            try
            {
                object? stateCommon = c.GetType().GetProperty("StateCommon")?.GetValue(c);
                object? border = stateCommon?.GetType().GetProperty("Border")?.GetValue(stateCommon);
                PropertyInfo? pi = border?.GetType().GetProperty("Rounding");
                if (border != null && pi != null && pi.CanWrite)
                {
                    object val = Convert.ChangeType(BanKinhKrypton, pi.PropertyType);
                    pi.SetValue(border, val);
                }
            }
            catch { /* control không có Border.Rounding thì bỏ qua */ }
        }

        // ------------------------------------------------------------------
        //  GROUPBOX
        // ------------------------------------------------------------------

        private static Size DoTieuDe(GroupBox gb)
        {
            if (string.IsNullOrEmpty(gb.Text)) return Size.Empty;
            return TextRenderer.MeasureText(gb.Text, gb.Font, Size.Empty,
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }

        private static Rectangle RectTieuDe(GroupBox gb, Size sz)
            => new Rectangle(Px(gb, 4), 0, sz.Width + Px(gb, 14), sz.Height + 1);


        private static void CapNhatRegionGroupBox(GroupBox gb)
        {
            if (gb.Width < 12 || gb.Height < 12) return;

            Size sz = DoTieuDe(gb);
            int top = sz.Height / 2;
            using var path = TaoDuong(new RectangleF(0, top, gb.Width, gb.Height - top), Px(gb, BanKinhThe));

            var region = new Region(path);
            if (sz.Height > 0) region.Union(RectTieuDe(gb, sz));
            gb.Region = region;
        }

        private static void BoGocGroupBox(GroupBox gb)
        {
            if (gb.FlatStyle == FlatStyle.System) gb.FlatStyle = FlatStyle.Standard;
            BatDoubleBuffer(gb);

            gb.Region = null; // không dùng Region nữa, tránh cắt cụt góc

            gb.SizeChanged += (s, e) => gb.Invalidate();
            gb.TextChanged += (s, e) => gb.Invalidate();
            gb.FontChanged += (s, e) => gb.Invalidate();
            gb.Paint += VeVienGroupBox;
            gb.Invalidate();
        }

        private static void VeVienGroupBox(object? sender, PaintEventArgs e)
        {
            var gb = (GroupBox)sender!;
            if (gb.Width < 12 || gb.Height < 12) return;

            Size sz = DoTieuDe(gb);
            int top = sz.Height / 2;

            float w = DoDayVienGroupBox * gb.DeviceDpi / 86f;   // độ dày nét
            float inset = w / 2f;
            float radius = Px(gb, BanKinhThe);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Color nenCha = MauNenCha(gb);
            Color nenTrong = gb.BackColor.A == 255 ? gb.BackColor : nenCha;

            // Tiêu đề được dời ra sau đoạn góc bo để không cắt mất cung tròn
            int textX = Px(gb, BanKinhThe) + Px(gb, 6);
            Rectangle khoangHo = sz.Height > 0
                ? new Rectangle(textX - Px(gb, 4), 0, sz.Width + Px(gb, 8), sz.Height + 1)
                : Rectangle.Empty;
            // Vùng chữ tiêu đề cũ do Windows vẽ (x ~ 8px) -> xóa đi
            Rectangle chuCu = sz.Height > 0
                ? new Rectangle(0, 0, Px(gb, 8) + sz.Width + Px(gb, 10), sz.Height + 2)
                : Rectangle.Empty;

            var rect = new RectangleF(inset, top + inset, gb.Width - w, gb.Height - top - w);
            using var duong = TaoDuong(rect, radius);

            using var khung = new GraphicsPath(FillMode.Alternate);
            khung.AddRectangle(new RectangleF(0, 0, gb.Width, gb.Height));
            khung.AddPath(duong, false);

            using var brushTrong = new SolidBrush(nenTrong);
            using var brushCha = new SolidBrush(nenCha);
            using var pen = new Pen(MauVienGroupBox, w) { LineJoin = LineJoin.Round };

            // 1) Xóa chữ cũ
            if (!chuCu.IsEmpty) g.FillRectangle(brushTrong, chuCu);

            // 2) Tô phần ngoài đường cong bằng màu nền cha (xóa viền vuông gốc)
            g.FillPath(brushCha, khung);

            // 3) Vẽ viền xanh lá, chừa khoảng hở đúng chỗ tiêu đề
            GraphicsState st = g.Save();
            if (!khoangHo.IsEmpty) g.SetClip(khoangHo, CombineMode.Exclude);
            g.DrawPath(pen, duong);
            g.Restore(st);

            // 4) Vẽ lại tiêu đề ở vị trí mới
            if (sz.Height > 0)
            {
                TextRenderer.DrawText(g, gb.Text, gb.Font, new Point(textX, 0), gb.ForeColor,
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            }
        }

        // ------------------------------------------------------------------
        //  PANEL "THẺ"
        // ------------------------------------------------------------------
        private static bool LaPanelThe(Panel p)
        {
            if (p.Dock != DockStyle.None) return false;   // panel dính mép form giữ nguyên vuông
            if (p.Parent is SplitContainer or TabControl) return false;
            if (p.AutoScroll) return false;
            if (p.Width < Px(p, 80) || p.Height < Px(p, 40)) return false;

            bool coVien = p.BorderStyle != BorderStyle.None;
            bool khacNen = p.BackColor.A == 255 && p.BackColor != MauNenCha(p);
            return coVien || khacNen;
        }

        private static void BoGocPanel(Panel p)
        {
            bool coVien = p.BorderStyle != BorderStyle.None;
            p.BorderStyle = BorderStyle.None; // viền hệ thống vuông -> tự vẽ viền bo tròn
            BatDoubleBuffer(p);

            p.Paint += (s, e) =>
            {
                if (!coVien || p.Width < 6 || p.Height < 6) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = TaoDuong(new RectangleF(0.5f, 0.5f, p.Width - 1, p.Height - 1), Px(p, BanKinhThe));
                using var pen = new Pen(MauVien, 1.25f);
                e.Graphics.DrawPath(pen, path);
            };

            BoGocRegion(p, () => BanKinhThe);
        }

        // ------------------------------------------------------------------
        //  BẢNG / DANH SÁCH / Ô NHẬP LIỆU: cắt góc + VIỀN BO TRÒN ÔM THEO
        //  Viền cũ (vuông) được tắt, control cha vẽ lại viền bo tròn khử răng cưa
        //  nằm sát mép ngoài control -> giống khung input/table trên web.
        // ------------------------------------------------------------------
        private static readonly ConditionalWeakTable<Control, List<Control>> _vienCon = new();

        private static void BoGocCoVien(Control c)
        {
            bool dock = c.Dock != DockStyle.None;

            // Control dock sát mép cha thì không có chỗ vẽ viền ngoài -> giữ viền gốc
            if (!dock)
            {
                switch (c)
                {
                    case DataGridView d: d.BorderStyle = BorderStyle.None; break;
                    case ListView lv: lv.BorderStyle = BorderStyle.None; break;
                    case TreeView tv: tv.BorderStyle = BorderStyle.None; break;
                    case RichTextBox rt: rt.BorderStyle = BorderStyle.None; break;
                    case ListBox lb: lb.BorderStyle = BorderStyle.None; break;
                }
            }

            BoGocRegion(c, () => BanKinhLuoi);
            if (!dock) GanVienNgoai(c);
        }

        private static void GanVienNgoai(Control c)
        {
            Control? parent = null;

            void DangKy()
            {
                if (parent != null && _vienCon.TryGetValue(parent, out var cu))
                {
                    cu.Remove(c);
                    parent.Invalidate();
                }
                parent = c.Parent;
                if (parent == null) return;

                if (!_vienCon.TryGetValue(parent, out var ds))
                {
                    ds = new List<Control>();
                    _vienCon.Add(parent, ds);
                    parent.Paint += VeVienCon;
                }
                ds.Add(c);
                parent.Invalidate();
            }

            void CapNhat() => c.Parent?.Invalidate();

            c.ParentChanged += (s, e) => DangKy();
            c.SizeChanged += (s, e) => CapNhat();
            c.LocationChanged += (s, e) => CapNhat();
            c.VisibleChanged += (s, e) => CapNhat();
            DangKy();
        }

        private static void VeVienCon(object? sender, PaintEventArgs e)
        {
            var parent = (Control)sender!;
            if (!_vienCon.TryGetValue(parent, out var list)) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            foreach (Control c in list.ToArray())
            {
                if (c.IsDisposed || !c.Visible) continue;
                Rectangle b = c.Bounds;
                if (!e.ClipRectangle.IntersectsWith(Rectangle.Inflate(b, 3, 3))) continue;

                float w = Math.Max(1f, c.DeviceDpi / 96f);
                // Nét vẽ nằm ngay sát ngoài mép control, đồng tâm với góc đã cắt
                var rf = new RectangleF(b.X - w / 2f, b.Y - w / 2f, b.Width + w, b.Height + w);
                using var path = TaoDuong(rf, Px(c, BanKinhLuoi) + w / 2f);
                using var pen = new Pen(MauVien, w);
                g.DrawPath(pen, path);
            }
        }

        // ------------------------------------------------------------------
        //  CẮT GÓC TRÒN BẰNG REGION
        // ------------------------------------------------------------------
        private static void BoGocRegion(Control c, Func<int> banKinh)
        {
            BatDoubleBuffer(c);

            void Cap()
            {
                if (c.Width < 6 || c.Height < 6) return;
                using var path = TaoDuong(new RectangleF(0, 0, c.Width, c.Height), Px(c, banKinh()));
                c.Region = new Region(path);
                c.Invalidate();
            }

            c.SizeChanged += (s, e) => Cap();
            Cap();
        }

        // ------------------------------------------------------------------
        //  TIỆN ÍCH
        // ------------------------------------------------------------------
        private static GraphicsPath TaoDuong(RectangleF r, float radius)
        {
            float rad = Math.Max(1f, Math.Min(radius, Math.Min(r.Width, r.Height) / 2f));
            float d = rad * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        private static int Px(Control c, int v) => (int)Math.Round(v * c.DeviceDpi / 96.0);

        private static Color MauNenCha(Control c)
        {
            for (Control? p = c.Parent; p != null; p = p.Parent)
                if (p.BackColor.A == 255) return p.BackColor;
            return SystemColors.Control;
        }

        private static void BatDoubleBuffer(Control c)
        {
            try
            {
                typeof(Control).GetProperty("DoubleBuffered",
                    BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(c, true);
            }
            catch { }
        }
    }
}