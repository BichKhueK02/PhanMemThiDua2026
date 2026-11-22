using System.Diagnostics;
using System.Drawing.Drawing2D;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN KIỂU WEB + BIỂU ĐỒ CỘT 3D CHO Form26_PhanTichQuanSo — CHỈ TRÌNH BÀY
    //  - Cùng phong cách Form4/Form16: nền xám xanh, thẻ trắng bo góc, nút tím nhạt.
    //  - Tái dùng lớp TheF16 + BoGoc của Module_GiaoDienForm16Web.cs (cần để file đó trong dự án).
    //  - Dùng: ngay SAU InitializeComponent():  _bieuDo = Module_GiaoDienForm26Web.ApDung(this);
    // =====================================================================================
    internal static class Module_GiaoDienForm26Web
    {
        private static readonly Color MauNen = Color.FromArgb(243, 244, 248);
        private static readonly Color MauChuPhu = Color.FromArgb(100, 116, 139);
        private static readonly Color MauTieuDeBang = Color.FromArgb(245, 243, 255);
        public static BieuDoCot3DF26? ApDung(Form f)
        {
            BieuDoCot3DF26? bieuDo = null;
            f.SuspendLayout();
            try
            {
                f.BackColor = MauNen;
                var t2 = Tim<TableLayoutPanel>(f, "tableLayoutPanel2");
                var t1 = Tim<TableLayoutPanel>(f, "tableLayoutPanel1");
                var luoi = Tim<Krypton.Toolkit.KryptonDataGridView>(f, "kryptonDataGridView1");
                if (t2 == null || t1 == null || luoi == null) return null;
                // Khung tổng: hàng 0 = đầu trang, hàng 1 = thẻ bảng, hàng 2 = thẻ biểu đồ
                t2.BackColor = MauNen;
                t2.Padding = new Padding(14, 10, 14, 8);
                t2.RowStyles.Clear();
                t2.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
                t2.RowStyles.Add(new RowStyle(SizeType.Percent, 46F));
                t2.RowStyles.Add(new RowStyle(SizeType.Percent, 54F));
                DinhDauTrang(f, t1);
                DinhLuoiPhang(luoi);
                TaoThe(t2, luoi, "Thống kê quân số theo cấp bậc", 1, new Padding(0, 4, 0, 6));
                bieuDo = new BieuDoCot3DF26 { Dock = DockStyle.Fill, BackColor = Color.White };
                TaoThe(t2, bieuDo, "Biểu đồ cơ cấu quân số theo đơn vị", 2, new Padding(0, 6, 0, 0));
                DinhThanhTrangThai(f);
            }
            finally { f.ResumeLayout(true); }
            return bieuDo;
        }
        private static T? Tim<T>(Control goc, string ten) where T : Control
            => goc.Controls.Find(ten, true).OfType<T>().FirstOrDefault();
        private static void TaoThe(TableLayoutPanel cha, Control noiDung, string tieuDe, int hang, Padding margin)
        {
            var the = new TheF16 { Dock = DockStyle.Fill, TieuDe = tieuDe, Margin = margin, Padding = new Padding(12, 46, 12, 12) };
            cha.SuspendLayout();
            try
            {
                noiDung.Parent?.Controls.Remove(noiDung);
                the.Controls.Add(noiDung);
                noiDung.Dock = DockStyle.Fill;
                cha.Controls.Add(the, 0, hang);
            }
            finally { cha.ResumeLayout(true); }
        }
        // Đầu trang: biểu tượng | tên đơn vị | nút Đóng
        private static void DinhDauTrang(Form f, TableLayoutPanel t1)
        {
            t1.BackColor = MauNen;
            t1.Margin = new Padding(0);
            t1.ColumnStyles.Clear();
            t1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52F));
            t1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            t1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            t1.RowStyles.Clear();
            t1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            var pic = Tim<PictureBox>(f, "pictureBox1");
            if (pic != null)
            {
                pic.Dock = DockStyle.Fill;
                pic.SizeMode = PictureBoxSizeMode.Zoom;
                pic.Margin = new Padding(0, 4, 8, 4);
                pic.BackColor = MauNen;
            }
            var lb = Tim<Label>(f, "label_DonVi");
            if (lb != null)
            {
                lb.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Regular, GraphicsUnit.Point);
                lb.ForeColor = Color.FromArgb(76, 29, 149);
                lb.Anchor = AnchorStyles.Left;
                lb.Margin = new Padding(0);
            }
            var nut = Tim<Krypton.Toolkit.KryptonButton>(f, "kryptonButton1_Dong");
            if (nut != null)
            {
                KieuNutPhu(nut);
                nut.Anchor = AnchorStyles.Right;
                nut.Margin = new Padding(0);
                nut.Size = new Size(nut.LogicalToDeviceUnits(116), nut.LogicalToDeviceUnits(34));
            }
        }
        // Nút phụ tím nhạt (nền #EDE9FE, viền #C4B5FD, chữ #6D28D9)
        private static void KieuNutPhu(Krypton.Toolkit.KryptonButton b)
        {
            Color nen = Color.FromArgb(237, 233, 254), hover = Color.FromArgb(221, 214, 254), down = Color.FromArgb(196, 181, 253);
            Color vien = Color.FromArgb(196, 181, 253), chu = Color.FromArgb(109, 40, 217);
            b.StateCommon.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            b.StateCommon.Border.Rounding = 10F;
            b.StateCommon.Border.Width = 1;
            b.StateCommon.Content.ShortText.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Regular, GraphicsUnit.Point);
            b.StateCommon.Content.ShortText.Color1 = chu;
            b.StateNormal.Back.Color1 = nen; b.StateNormal.Back.Color2 = nen;
            b.StateNormal.Border.Color1 = vien; b.StateNormal.Border.Color2 = vien;
            b.StateNormal.Content.ShortText.Color1 = chu;
            b.StateTracking.Back.Color1 = hover; b.StateTracking.Back.Color2 = hover;
            b.StateTracking.Border.Color1 = vien; b.StateTracking.Border.Color2 = vien;
            b.StateTracking.Content.ShortText.Color1 = chu;
            b.StatePressed.Back.Color1 = down; b.StatePressed.Back.Color2 = down;
            b.StatePressed.Border.Color1 = vien; b.StatePressed.Border.Color2 = vien;
            b.StatePressed.Content.ShortText.Color1 = chu;
            foreach (var ov in new[] { b.OverrideFocus, b.OverrideDefault })
            {
                ov.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
                ov.Back.Color1 = nen; ov.Back.Color2 = nen;
                ov.Border.Color1 = vien; ov.Border.Color2 = vien;
                ov.Content.ShortText.Color1 = chu;
            }
        }
        // Lưới Krypton: tiêu đề nền phẳng (Krypton mặc định vẽ chuyển sắc), không viền thừa
        private static void DinhLuoiPhang(Krypton.Toolkit.KryptonDataGridView d)
        {
            d.StateCommon.Background.Color1 = Color.White;
            d.StateCommon.DataCell.Border.Color1 = Color.FromArgb(235, 238, 243);
            d.StateCommon.HeaderRow.Back.Color1 = MauTieuDeBang;
            d.StateCommon.HeaderRow.Back.Color2 = MauTieuDeBang;
            // Header Common
            d.StateCommon.HeaderColumn.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            d.StateCommon.HeaderColumn.Back.Color1 = MauTieuDeBang;
            d.StateCommon.HeaderColumn.Back.Color2 = MauTieuDeBang;
            d.StateCommon.HeaderColumn.Border.Color1 = MauTieuDeBang;
            d.StateCommon.HeaderColumn.Border.Color2 = MauTieuDeBang;
            // Header Tracking
            d.StateTracking.HeaderColumn.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            d.StateTracking.HeaderColumn.Back.Color1 = MauTieuDeBang;
            d.StateTracking.HeaderColumn.Back.Color2 = MauTieuDeBang;
            d.StateTracking.HeaderColumn.Border.Color1 = MauTieuDeBang;
            d.StateTracking.HeaderColumn.Border.Color2 = MauTieuDeBang;
            // Header Pressed
            d.StatePressed.HeaderColumn.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            d.StatePressed.HeaderColumn.Back.Color1 = MauTieuDeBang;
            d.StatePressed.HeaderColumn.Back.Color2 = MauTieuDeBang;
            d.StatePressed.HeaderColumn.Border.Color1 = MauTieuDeBang;
            d.StatePressed.HeaderColumn.Border.Color2 = MauTieuDeBang;
            d.StateSelected.DataCell.Back.Color1 = Color.FromArgb(237, 233, 254);
            d.StateSelected.DataCell.Back.Color2 = Color.FromArgb(237, 233, 254);
        }
        private static void DinhThanhTrangThai(Form f)
        {
            var ss = Tim<StatusStrip>(f, "statusStrip1");
            if (ss == null) return;
            ss.BackColor = Color.White;
            ss.ForeColor = MauChuPhu;
            ss.SizingGrip = false;
            ss.Padding = new Padding(10, 0, 10, 0);
            foreach (ToolStripItem it in ss.Items) it.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        }
    }
    /// <summary>
    /// Biểu đồ cột 3D tự vẽ (double-buffer): sàn + tường lưới 3D, cột có mặt trước chuyển sắc / mặt trên / mặt hông,
    /// hiệu ứng mọc lên khi nạp dữ liệu, rê chuột vào cột hiện chú thích. Không phụ thuộc thư viện ngoài.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal sealed class BieuDoCot3DF26 : Control
    {
        public readonly record struct Dong(string Ten, int Sq, int Hsq, int Cs);
        //private static readonly Color[] MauCot =
        //{
        //    Color.FromArgb(59, 130, 246),   // sĩ quan: xanh dương
        //    Color.FromArgb(245, 158, 11),   // hạ sĩ quan: cam
        //    Color.FromArgb(16, 185, 129)    // chiến sĩ: xanh ngọc
        //};
        private static readonly Color[] MauCot =
{
    Color.FromArgb(99, 102, 241),   // Sĩ quan: Xanh Indigo/Tím nhạt hiện đại (Ăn nhập với nút bấm)
    Color.FromArgb(249, 115, 22),   // Hạ sĩ quan: Cam san hô ấm áp
    Color.FromArgb(16, 185, 129)    // Chiến sĩ: Xanh ngọc lục bảo tươi tắn, dịu mắt
};
        private static readonly Font FontNhan = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontSo = new Font("Segoe UI Semibold", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
        private List<Dong> _ds = new();
        private readonly System.Windows.Forms.Timer _timer = new() { Interval = 15 };
        private long _batDau;
        private float _tien = 1f;
        private readonly List<(RectangleF Vung, string Dong1, string Dong2)> _hit = new();
        private int _hover = -1;
        private Point _chuot;
        private const double ThoiGianMs = 520;
        public BieuDoCot3DF26()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            _timer.Tick += (s, e) =>
            {
                double ms = Stopwatch.GetElapsedTime(_batDau).TotalMilliseconds;
                _tien = (float)Math.Min(1.0, ms / ThoiGianMs);
                if (_tien >= 1f) _timer.Stop();
                Invalidate();
            };
        }
        public void DatDuLieu(IEnumerable<Dong> ds, bool hoatHoa = true)
        {
            _ds = ds.ToList();
            _hover = -1;
            if (hoatHoa && _ds.Count > 0)
            {
                _tien = 0f;
                _batDau = Stopwatch.GetTimestamp();
                if (Visible) _timer.Start();
            }
            else _tien = 1f;
            Invalidate();
        }
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible) _timer.Stop();                  // form bị ẩn: dừng hoạt hình, tiết kiệm CPU
            else if (_tien < 1f) { _batDau = Stopwatch.GetTimestamp(); _tien = 0f; _timer.Start(); }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer.Dispose();
            base.Dispose(disposing);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _chuot = e.Location;
            int h = -1;
            for (int i = _hit.Count - 1; i >= 0; i--)
                if (_hit[i].Vung.Contains(e.Location)) { h = i; break; }
            if (h != _hover || h >= 0) { _hover = h; Invalidate(); }
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hover != -1) { _hover = -1; Invalidate(); }
        }
        private static Color Pha(Color c, float k)   // k>0 pha trắng, k<0 pha đen
        {
            int Mix(int v) => (int)Math.Clamp(k >= 0 ? v + (255 - v) * k : v * (1 + k), 0, 255);
            return Color.FromArgb(Mix(c.R), Mix(c.G), Mix(c.B));
        }
        private static int BuocDep(int max)
        {
            int raw = Math.Max(1, (int)Math.Ceiling(max / 5.0));
            int[] dep = { 1, 2, 5, 10, 20, 25, 50, 100, 200, 250, 500, 1000, 2000, 5000 };
            foreach (int d in dep) if (d >= raw) return d;
            return raw;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float s = DeviceDpi / 96f;
            _hit.Clear();
            const TextFormatFlags giua = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
            if (_ds.Count == 0)
            {
                TextRenderer.DrawText(g, "Chưa có dữ liệu để vẽ biểu đồ", FontNhan, ClientRectangle, Color.FromArgb(148, 163, 184), giua);
                return;
            }
            string[] tenCot = { Module_HeThong.Sy_Quan, Module_HeThong.Ha_Sy_Quan, Module_HeThong.Chien_Sy_Nghia_Vu };
            int maxV = 1;
            foreach (var d in _ds) maxV = Math.Max(maxV, Math.Max(d.Sq, Math.Max(d.Hsq, d.Cs)));
            int step = BuocDep(maxV);
            int gridMax = step * 5;
            float trai = 38 * s, phai = 12 * s, tren = 38 * s, duoi = Height - 44 * s;
            float dep = 12 * s;                                   // chiều sâu của "căn phòng" 3D
            float rong = Width - trai - phai - dep;
            float x0 = trai, x1 = trai + rong, yTren = tren + dep, yDuoi = duoi;
            float cao = yDuoi - yTren;
            if (rong < 60 * s || cao < 40 * s) return;
            float Y(float v) => yDuoi - v / gridMax * cao;
            // --- Chú thích (legend) ---
            float lx = x0;
            for (int i = 0; i < 3; i++)
            {
                var o = new RectangleF(lx, 10 * s, 11 * s, 11 * s);
                using (var p = Module_GiaoDienForm16Web.BoGoc(Rectangle.Round(o), (int)(3 * s)))
                using (var b = new SolidBrush(MauCot[i])) g.FillPath(b, p);
                var kt = TextRenderer.MeasureText(tenCot[i], FontNhan);
                TextRenderer.DrawText(g, tenCot[i], FontNhan, new Point((int)(lx + 16 * s), (int)(8 * s)), Color.FromArgb(71, 85, 105), TextFormatFlags.NoPadding);
                lx += 16 * s + kt.Width + 18 * s;
            }
            // --- Sàn + tường 3D ---
            using (var b = new SolidBrush(Color.FromArgb(248, 250, 252)))
                g.FillRectangle(b, x0 + dep, yTren - dep, rong, cao);                                         // tường sau
            using (var b = new SolidBrush(Color.FromArgb(241, 245, 249)))
                g.FillPolygon(b, new[] { new PointF(x0, yTren), new PointF(x0 + dep, yTren - dep), new PointF(x0 + dep, yDuoi - dep), new PointF(x0, yDuoi) });   // tường trái
            using (var b = new SolidBrush(Color.FromArgb(232, 237, 244)))
                g.FillPolygon(b, new[] { new PointF(x0, yDuoi), new PointF(x1, yDuoi), new PointF(x1 + dep, yDuoi - dep), new PointF(x0 + dep, yDuoi - dep) }); // sàn
            using (var penLuoi = new Pen(Color.FromArgb(214, 220, 229), 1f) { DashStyle = DashStyle.Dash })
            using (var penCanh = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                for (int i = 0; i <= 5; i++)
                {
                    float y = Y(step * i);
                    g.DrawLine(penCanh, x0, y, x0 + dep, y - dep);
                    g.DrawLine(penLuoi, x0 + dep, y - dep, x1 + dep, y - dep);
                    if (i > 0)
                        TextRenderer.DrawText(g, (step * i).ToString(), FontNhan, new Rectangle((int)(x0 - 34 * s), (int)(y - 8 * s), (int)(30 * s), (int)(16 * s)),
                            Color.FromArgb(100, 116, 139), TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
            }
            using (var pen = new Pen(Color.FromArgb(176, 186, 200), 1.5f))
                g.DrawLine(pen, x0, yDuoi, x1, yDuoi);
            // --- Cột ---
            int n = _ds.Count;
            float nhom = rong / n;
            float maxCot = (n <= 4 ? 64f : 36f) * s;
            float bw = Math.Clamp(nhom * 0.75f / 3f, 6f * s, maxCot);
            float bd = Math.Min(bw * 0.35f, dep * 0.9f);        // độ sâu của cột
            float lech = (dep - bd) / 2f;                         // đặt cột giữa chiều sâu của sàn
            float khe = bd * 0.6f + 3f * s;
            float ease = 1f - (float)Math.Pow(1f - _tien, 3);
            int stt = 0;
            for (int i = 0; i < n; i++)
            {
                var d = _ds[i];
                int[] gt = { d.Sq, d.Hsq, d.Cs };
                int hoatDong = gt.Count(v => v > 0);
                float gx = x0 + i * nhom;
                if (hoatDong > 0)
                {
                    float tong = hoatDong * bw + (hoatDong - 1) * khe + bd;
                    float cx = gx + (nhom - tong) / 2f + lech;
                    for (int k = 0; k < 3; k++)
                    {
                        if (gt[k] <= 0) continue;
                        float hFull = gt[k] / (float)gridMax * cao;
                        float h = hFull * ease;
                        float yBase = yDuoi - lech;
                        float yTop = yBase - h;
                        var goc = MauCot[k];
                        bool dangTro = stt == _hover;
                        float sang = dangTro ? 0.14f : 0f;
                        _hit.Add((new RectangleF(cx, yBase - hFull - bd, bw + bd, hFull + bd), d.Ten, $"{tenCot[k]}: {gt[k]}"));
                        if (h >= 1f)
                        {
                            var front = new RectangleF(cx, yTop, bw, h);
                            using (var br = new LinearGradientBrush(new RectangleF(cx, yTop - 1, bw, h + 2), Pha(goc, 0.22f + sang), Pha(goc, -0.08f + sang), LinearGradientMode.Vertical))
                                g.FillRectangle(br, front);
                            using (var br = new SolidBrush(Pha(goc, -0.28f + sang)))
                                g.FillPolygon(br, new[] { new PointF(cx + bw, yTop), new PointF(cx + bw + bd, yTop - bd), new PointF(cx + bw + bd, yBase - bd), new PointF(cx + bw, yBase) });
                            using (var br = new SolidBrush(Pha(goc, 0.42f + sang)))
                                g.FillPolygon(br, new[] { new PointF(cx, yTop), new PointF(cx + bw, yTop), new PointF(cx + bw + bd, yTop - bd), new PointF(cx + bd, yTop - bd) });
                            if (_tien > 0.8f)
                                TextRenderer.DrawText(g, gt[k].ToString(), FontSo,
                                    new Rectangle((int)cx - 6, (int)(yTop - bd - 18 * s), (int)(bw + bd) + 12, (int)(17 * s)),
                                    Color.FromArgb(51, 65, 85), TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom | TextFormatFlags.NoPadding);
                        }
                        cx += bw + khe;
                        stt++;
                    }
                }
                // Tên đơn vị
                TextRenderer.DrawText(g, d.Ten, FontNhan, new Rectangle((int)gx, (int)(yDuoi + 6 * s), (int)nhom, (int)(34 * s)),
                    Color.FromArgb(51, 65, 85), TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
            // --- Chú thích khi rê chuột ---
            if (_hover >= 0 && _hover < _hit.Count)
            {
                var (_, l1, l2) = _hit[_hover];
                var k1 = TextRenderer.MeasureText(l1, FontSo);
                var k2 = TextRenderer.MeasureText(l2, FontNhan);
                int w = Math.Max(k1.Width, k2.Width) + (int)(20 * s), h = k1.Height + k2.Height + (int)(14 * s);
                int tx = Math.Min(_chuot.X + (int)(14 * s), Width - w - 4), ty = Math.Max(4, _chuot.Y - h - (int)(10 * s));
                var r = new Rectangle(tx, ty, w, h);
                using (var p = Module_GiaoDienForm16Web.BoGoc(r, (int)(8 * s)))
                using (var b = new SolidBrush(Color.FromArgb(235, 15, 23, 42))) g.FillPath(b, p);
                TextRenderer.DrawText(g, l1, FontSo, new Rectangle(r.X + (int)(10 * s), r.Y + (int)(6 * s), w, k1.Height), Color.White, TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, l2, FontNhan, new Rectangle(r.X + (int)(10 * s), r.Y + (int)(6 * s) + k1.Height, w, k2.Height), Color.FromArgb(203, 213, 225), TextFormatFlags.NoPadding);
            }
        }
    }
}