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
    ///  - GroupBox                         : khung bo tròn, viền liền mạch, tiêu đề nằm trên viền
    ///  - Panel / TableLayoutPanel "thẻ"   : Dock = None, có viền hoặc nền khác nền cha
    ///  - DataGridView, ListView, TreeView, RichTextBox, ListBox : cắt góc tròn + viền bo do control cha vẽ
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
        public static Color MauVien { get; set; } = Color.FromArgb(176, 188, 210);   // viền Panel thẻ
        public const string TheKhongBoGoc = "KhongBoGoc";

        // GroupBox (độ dày < 1px sẽ bị mờ/đứt nét nên code tự nâng lên tối thiểu 1px)
        public static Color MauVienGroupBox { get; set; } = Color.FromArgb(192, 192, 255); // xanh lá: Color.FromArgb(46, 160, 67)
        public static float DoDayVienGroupBox { get; set; } = 1f;                         // px ở 96 DPI

        // DataGridView, ListView, TreeView...
        public static Color MauVienLuoi { get; set; } = Color.FromArgb(176, 188, 210);
        public static float DoDayVienLuoi { get; set; } = 1.25f;                           // px ở 96 DPI

        // Control Dock sát mép Panel/TabPage: tự chừa lề 3px để có chỗ vẽ viền bo
        public static bool ChenLeChoLuoiDock { get; set; } = true;

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
        //  Không dùng Region. Mỗi lần Paint:
        //   1) xóa chữ tiêu đề cũ + viền "khắc nổi" 2px do Windows vẽ (nguyên nhân gây đứt nét ở góc)
        //   2) tô phần ngoài đường cong bằng màu nền cha (khử răng cưa)
        //   3) vẽ viền bo liền mạch, chừa khoảng hở đúng chỗ tiêu đề
        //   4) vẽ lại tiêu đề, dời ra sau đoạn góc bo
        // ------------------------------------------------------------------
        private static Size DoTieuDe(GroupBox gb)
        {
            if (string.IsNullOrEmpty(gb.Text)) return Size.Empty;
            return TextRenderer.MeasureText(gb.Text, gb.Font, Size.Empty,
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }

        private static void BoGocGroupBox(GroupBox gb)
        {
            if (gb.FlatStyle == FlatStyle.System) gb.FlatStyle = FlatStyle.Standard;
            BatDoubleBuffer(gb);
            gb.Region = null;

            gb.SizeChanged += (s, e) => gb.Invalidate();
            gb.TextChanged += (s, e) => gb.Invalidate();
            gb.FontChanged += (s, e) => gb.Invalidate();
            gb.BackColorChanged += (s, e) => gb.Invalidate();
            gb.ParentChanged += (s, e) => gb.Invalidate();
            gb.EnabledChanged += (s, e) => gb.Invalidate();
            gb.Paint += VeVienGroupBox;
            gb.Invalidate();
        }

        private static void VeVienGroupBox(object? sender, PaintEventArgs e)
        {
            var gb = (GroupBox)sender!;
            if (gb.Width < 12 || gb.Height < 12) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            float scale = gb.DeviceDpi / 96f;
            float w = Math.Max(1f, DoDayVienGroupBox * scale);   // tối thiểu 1px, mỏng hơn sẽ bị mờ/đứt
            float inset = w / 2f;
            float radius = Px(gb, BanKinhThe);

            Size sz = DoTieuDe(gb);
            int top = sz.Height / 2;

            Color nenCha = MauNenCha(gb);
            Color nenTrong = gb.BackColor.A == 255 ? gb.BackColor : nenCha;

            // Tiêu đề dời ra sau đoạn góc bo để không cắt mất cung tròn
            int textX = Px(gb, BanKinhThe) + Px(gb, 6);
            Rectangle khoangHo = sz.Height > 0
                ? new Rectangle(textX - Px(gb, 4), 0, sz.Width + Px(gb, 8), sz.Height + 1)
                : Rectangle.Empty;
            // Vùng chữ tiêu đề cũ do Windows vẽ (x ~ 8px)
            Rectangle chuCu = sz.Height > 0
                ? new Rectangle(0, 0, Px(gb, 8) + sz.Width + Px(gb, 10), sz.Height + 2)
                : Rectangle.Empty;

            var rect = new RectangleF(inset, top + inset, gb.Width - w, gb.Height - top - w);
            using var duong = TaoDuong(rect, radius);

            using var khung = new GraphicsPath(FillMode.Alternate);
            khung.AddRectangle(new RectangleF(-1, -1, gb.Width + 2, gb.Height + 2));
            khung.AddPath(duong, false);

            using var brushTrong = new SolidBrush(nenTrong);
            using var brushCha = new SolidBrush(nenCha);
            // Nét dày ~8px đè lên viền hệ thống 2px chạy dọc các cạnh thẳng
            using var penXoa = new Pen(nenTrong, 8f * scale) { LineJoin = LineJoin.Round };
            using var pen = new Pen(MauVienGroupBox, w) { LineJoin = LineJoin.Round };

            // 1) Xóa chữ cũ và viền hệ thống
            if (!chuCu.IsEmpty) g.FillRectangle(brushTrong, chuCu);
            g.DrawPath(penXoa, duong);

            // 2) Tô phần ngoài đường cong bằng màu nền cha
            g.FillPath(brushCha, khung);

            // 3) Viền liền mạch, chừa khoảng hở cho tiêu đề
            GraphicsState st = g.Save();
            if (!khoangHo.IsEmpty) g.SetClip(khoangHo, CombineMode.Exclude);
            g.DrawPath(pen, duong);
            g.Restore(st);

            // 4) Tiêu đề
            if (sz.Height > 0)
            {
                g.FillRectangle(brushTrong, khoangHo);
                TextRenderer.DrawText(g, gb.Text, gb.Font, new Point(textX, 0),
                    gb.Enabled ? gb.ForeColor : SystemColors.GrayText,
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
        //  BẢNG / DANH SÁCH / Ô NHẬP LIỆU
        //  - Control: tắt viền vuông gốc, cắt góc bằng Region.
        //  - Viền bo tròn do CONTROL CHA vẽ ngay sát mép ngoài control
        //    (không vẽ lên bề mặt control nên cuộn dòng không để lại vệt).
        //  - Áp dụng cả control Dock: chừa lề cho cha để có chỗ vẽ viền.
        // ------------------------------------------------------------------
        private static readonly ConditionalWeakTable<Control, List<Control>> _vienCon = new();

        private static void BoGocCoVien(Control c)
        {
            switch (c)
            {
                case DataGridView d: d.BorderStyle = BorderStyle.None; break;
                case ListView lv: lv.BorderStyle = BorderStyle.None; break;
                case TreeView tv: tv.BorderStyle = BorderStyle.None; break;
                case RichTextBox rt: rt.BorderStyle = BorderStyle.None; break;
                case ListBox lb: lb.BorderStyle = BorderStyle.None; break;
            }

            DamBaoKhoangTrong(c);
            BoGocRegion(c, () => BanKinhLuoi);
            GanVienNgoai(c);
        }

        /// <summary>Control Dock sát mép Panel/TabPage thì nới Padding của cha để có chỗ vẽ viền.</summary>
        private static void DamBaoKhoangTrong(Control c)
        {
            if (!ChenLeChoLuoiDock || c.Dock == DockStyle.None) return;

            void Lam()
            {
                Control? p = c.Parent;
                if (p == null) return;
                if (p is TableLayoutPanel or FlowLayoutPanel) return;   // các loại này dùng Margin của control
                if (p is not (Panel or TabPage or UserControl)) return; // GroupBox đã có lề 3px sẵn

                int can = Px(p, 3);
                Padding pd = p.Padding;
                var np = new Padding(Math.Max(pd.Left, can), Math.Max(pd.Top, can),
                                     Math.Max(pd.Right, can), Math.Max(pd.Bottom, can));
                if (np != pd) p.Padding = np;
            }

            c.ParentChanged += (s, e) => Lam();
            Lam();
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
                if (!ds.Contains(c)) ds.Add(c);
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
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            foreach (Control c in list.ToArray())
            {
                if (c.IsDisposed || !c.Visible) continue;
                Rectangle b = c.Bounds;
                if (!e.ClipRectangle.IntersectsWith(Rectangle.Inflate(b, 6, 6))) continue;

                float w = Math.Max(1f, DoDayVienLuoi * c.DeviceDpi / 96f);   // độ dày nhìn thấy
                // Nét vẽ nằm trên đúng mép Region: nửa trong bị control che, nửa ngoài lộ ra
                using var path = TaoDuong(new RectangleF(b.X, b.Y, b.Width, b.Height), Px(c, BanKinhLuoi));
                using var pen = new Pen(MauVienLuoi, w * 2f) { LineJoin = LineJoin.Round };
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