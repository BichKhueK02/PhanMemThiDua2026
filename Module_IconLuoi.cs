using System.Collections.Concurrent;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Biểu tượng nhỏ TỰ VẼ (thay ảnh Properties.Resources cũ: true/false, ic_star, ic_khenthuong, ic_login, ic_database,
    /// ic_save, ic_export, ic_help, ic_delete, ic_warning, ic_default) — vẽ lại ĐÚNG hình dáng ảnh cũ bằng GDI+.
    ///  - Resources mỗi lần gọi đều GIẢI MÃ PNG thành ảnh mới, rồi DrawImage co/giãn ảnh cho MỖI ô MỖI lần vẽ.
    ///  - Ở đây: vẽ 1 lần theo (loại, cỡ px), lưu Bitmap 32bppPArgb DÙNG CHUNG toàn app => ô lưới chỉ còn DrawImageUnscaled,
    ///    sắc nét ở mọi DPI (vẽ vector, không phóng ảnh 16px).
    ///  - Ảnh trả về là của bộ đệm: KHÔNG Dispose (cần ảnh riêng, vd gán cho ToolStripItem, thì dùng BanSao).
    /// Dùng trong CellPainting: e.Paint(e.CellBounds, PhanNenO) rồi VeTrongO(...) — tự căn giữa dọc, không lấn ra mép ô.
    /// </summary>
    internal static class Module_IconLuoi
    {
        internal enum Loai
        {
            Dat, ChuaDat, Sao, KhenThuong,
            DangNhap, CSDL, Luu, Xuat, TroGiup, Xoa, CanhBao, MacDinh
        }
        // Phần nền ô khi tự vẽ icon: KHÔNG gồm Border (đường kẻ ô) và Focus (khung chấm chấm) => ô có icon không bị vẽ viền riêng
        internal const DataGridViewPaintParts PhanNenO = DataGridViewPaintParts.Background | DataGridViewPaintParts.SelectionBackground;
        private static readonly ConcurrentDictionary<int, Bitmap> _kho = new();
        /// <summary>Cỡ icon theo DPI (16px ở 96 DPI).</summary>
        internal static int CoTheoDpi(Control c, int coGoc = 16) => Math.Max(10, (int)Math.Round(coGoc * c.DeviceDpi / 96f));
        internal static Bitmap Lay(Loai loai, int coPx)
        {
            coPx = Math.Clamp(coPx, 8, 128);
            int khoa = ((int)loai << 8) | coPx;
            return _kho.GetOrAdd(khoa, _ => Ve(loai, coPx));
        }
        /// <summary>Bản sao riêng (người nhận được phép Dispose) — dùng cho menu/nút.</summary>
        internal static Bitmap BanSao(Loai loai, int coPx) => new Bitmap(Lay(loai, coPx));
        /// <summary>Vẽ icon tại x, căn giữa dọc ô; tự thu nhỏ nếu ô thấp. Trả về mép phải của icon.</summary>
        internal static int VeTrongO(Graphics g, Loai loai, Rectangle o, int x, int coPx)
        {
            int co = Math.Min(coPx, Math.Max(8, o.Height - 4));   // chừa 2px trên/dưới: không đè đường kẻ ngang
            int y = o.Y + (o.Height - co) / 2;
            g.DrawImageUnscaled(Lay(loai, co), x, y);
            return x + co;
        }

        // ---------------- Bảng màu (lấy theo ảnh cũ) ----------------
        private static readonly Color LaSang = Color.FromArgb(190, 230, 70), LaVua = Color.FromArgb(120, 185, 30), LaDam = Color.FromArgb(62, 120, 18);
        private static readonly Color CamSang = Color.FromArgb(255, 214, 160), CamVua = Color.FromArgb(240, 150, 80), CamDam = Color.FromArgb(185, 95, 35);
        private static readonly Color XamSang = Color.FromArgb(240, 240, 240), XamVua = Color.FromArgb(185, 185, 185), XamDam = Color.FromArgb(110, 110, 110);
        private static readonly Color LamSang = Color.FromArgb(150, 195, 245), LamVua = Color.FromArgb(55, 125, 215), LamDam = Color.FromArgb(25, 75, 150);

        // ---------------- Vẽ (khung thiết kế 16x16, đúng khung ảnh cũ) ----------------
        private static Bitmap Ve(Loai loai, int coPx)
        {
            var bmp = new Bitmap(coPx, coPx, PixelFormat.Format32bppPArgb);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);
            g.ScaleTransform(coPx / 16f, coPx / 16f);
            switch (loai)
            {
                case Loai.Dat: VeDauTich(g); break;
                case Loai.ChuaDat: VeTamGiacDo(g); break;
                case Loai.Sao: VeHuyHieuSao(g); break;
                case Loai.KhenThuong: VeHuyChuong(g); break;
                case Loai.DangNhap: VeHaiNguoi(g); break;
                case Loai.CSDL:
                    VeTruCSDL(g);
                    VeMuiTenVong(g);
                    break;
                case Loai.Luu:
                    VeTruCSDL(g);
                    VeDia(g);
                    break;
                case Loai.Xuat: VeXuatExcel(g); break;
                case Loai.TroGiup: VeTroGiup(g); break;
                case Loai.Xoa: VeBangXoa(g); break;
                case Loai.CanhBao: VeThoiCanhBao(g); break;
                default: VeDungCu(g); break;
            }
            return bmp;
        }
        // true.png: dấu tích xanh lá dày, viền xanh đậm
        private static void VeDauTich(Graphics g)
        {
            var pts = new PointF[] { new(0.8f, 8.6f), new(3.4f, 6f), new(6.2f, 8.8f), new(12.6f, 2.2f), new(15.2f, 4.8f), new(6.2f, 13.8f) };
            using var to = new LinearGradientBrush(new RectangleF(0, 2, 16, 12), LaSang, LaVua, 90f);
            using var vien = new Pen(LaDam, 0.9f) { LineJoin = LineJoin.Round };
            g.FillPolygon(to, pts);
            g.DrawPolygon(vien, pts);
        }
        // false.png: tam giác viền đỏ, lòng vàng, dấu chấm than đen
        private static void VeTamGiacDo(Graphics g)
        {
            using (var p = TamGiacBo(new PointF(8f, 0.8f), new PointF(15.4f, 14.6f), new PointF(0.6f, 14.6f), 1.2f))
            using (var to = new LinearGradientBrush(new RectangleF(0, 0, 16, 16), Color.FromArgb(250, 90, 70), Color.FromArgb(205, 30, 25), 90f))
            using (var vien = new Pen(Color.FromArgb(150, 20, 15), 0.8f))
            {
                g.FillPath(to, p);
                g.DrawPath(vien, p);
            }
            using (var p = TamGiacBo(new PointF(8f, 4.2f), new PointF(12.9f, 13.1f), new PointF(3.1f, 13.1f), 0.6f))
            using (var vang = new SolidBrush(Color.FromArgb(255, 220, 60)))
                g.FillPath(vang, p);
            using var den = new SolidBrush(Color.FromArgb(30, 30, 30));
            g.FillRectangle(den, 7.25f, 6.4f, 1.5f, 3.9f);
            g.FillRectangle(den, 7.25f, 11f, 1.5f, 1.4f);
        }
        // ic_star.png: dải băng xanh lá chữ V ở trên + ngôi sao cam bên dưới
        private static void VeHuyHieuSao(Graphics g)
        {
            using (var la = new LinearGradientBrush(new RectangleF(0, 0, 16, 8), LaSang, LaVua, 90f))
            using (var vien = new Pen(LaDam, 0.8f) { LineJoin = LineJoin.Round })
            {
                var bang = new PointF[] { new(2.6f, 0.6f), new(13.4f, 0.6f), new(13.4f, 2.6f), new(9.6f, 7.4f), new(6.4f, 7.4f), new(2.6f, 2.6f) };
                g.FillPolygon(la, bang);
                g.DrawPolygon(vien, bang);
                using var lom = new SolidBrush(LaDam);
                g.FillPolygon(lom, new PointF[] { new(5.6f, 2.6f), new(10.4f, 2.6f), new(8f, 5.6f) });
            }
            using var p = NgoiSao(8f, 10.6f, 5.3f, 2.3f);
            using var to = new LinearGradientBrush(new RectangleF(2, 5, 12, 11), CamSang, CamVua, 90f);
            using var vs = new Pen(CamDam, 0.8f) { LineJoin = LineJoin.Round };
            g.FillPath(to, p);
            g.DrawPath(vs, p);
        }
        // ic_khenthuong.png: huy chương tròn cam + 2 đuôi băng xanh lá chéo xuống hai bên
        private static void VeHuyChuong(Graphics g)
        {
            using (var la = new LinearGradientBrush(new RectangleF(0, 6, 16, 10), LaSang, LaVua, 90f))
            using (var vien = new Pen(LaDam, 0.8f) { LineJoin = LineJoin.Round })
            {
                var trai = new PointF[] { new(5f, 8.6f), new(7.6f, 10.6f), new(4.2f, 15.4f), new(3f, 13.6f), new(0.8f, 14.2f) };
                var phai = new PointF[] { new(11f, 8.6f), new(8.4f, 10.6f), new(11.8f, 15.4f), new(13f, 13.6f), new(15.2f, 14.2f) };
                g.FillPolygon(la, trai); g.DrawPolygon(vien, trai);
                g.FillPolygon(la, phai); g.DrawPolygon(vien, phai);
            }
            var o = new RectangleF(2.6f, 0.8f, 10.8f, 10.8f);
            using (var to = new LinearGradientBrush(o, CamSang, CamVua, 90f))
            using (var vien = new Pen(CamDam, 0.9f))
            {
                g.FillEllipse(to, o);
                g.DrawEllipse(vien, o);
            }
            using var vong = new Pen(Color.FromArgb(160, 205, 115, 45), 0.8f);
            g.DrawEllipse(vong, 4.7f, 2.9f, 6.6f, 6.6f);
        }
        // ic_login.png: hai người (tóc nâu, áo xanh) + mũi tên xanh lá chỉ sang phải ở góc dưới phải
        private static void VeHaiNguoi(Graphics g)
        {
            VeNguoi(g, 10.6f, 0.6f, Color.FromArgb(120, 170, 90), Color.FromArgb(70, 115, 50));   // người sau
            VeNguoi(g, 5.2f, 3.4f, Color.FromArgb(80, 140, 220), Color.FromArgb(35, 80, 160));    // người trước
            var mt = new PointF[] { new(7.6f, 10.4f), new(10.8f, 10.4f), new(10.8f, 7.8f), new(15.4f, 12.1f), new(10.8f, 16f - 0.4f), new(10.8f, 13.8f), new(7.6f, 13.8f) };
            using var to = new LinearGradientBrush(new RectangleF(7, 7, 9, 9), LaSang, LaVua, 90f);
            using var vien = new Pen(LaDam, 0.8f) { LineJoin = LineJoin.Round };
            g.FillPolygon(to, mt);
            g.DrawPolygon(vien, mt);
        }
        private static void VeNguoi(Graphics g, float cx, float y, Color ao, Color aoDam)
        {
            using (var to = new LinearGradientBrush(new RectangleF(cx - 4.6f, y + 5.4f, 9.2f, 7.2f), ao, aoDam, 90f))
            using (var vien = new Pen(aoDam, 0.7f))
            using (var p = new GraphicsPath())
            {
                p.AddArc(cx - 4.4f, y + 5.6f, 8.8f, 7f, 180f, 180f);
                p.AddLine(cx + 4.4f, y + 9.1f, cx + 4.4f, y + 12.4f);
                p.AddLine(cx + 4.4f, y + 12.4f, cx - 4.4f, y + 12.4f);
                p.CloseFigure();
                g.FillPath(to, p);
                g.DrawPath(vien, p);
            }
            using (var da = new SolidBrush(Color.FromArgb(245, 200, 160)))
            using (var vienDa = new Pen(Color.FromArgb(175, 120, 80), 0.6f))
            {
                g.FillEllipse(da, cx - 2.6f, y + 0.6f, 5.2f, 5.6f);
                g.DrawEllipse(vienDa, cx - 2.6f, y + 0.6f, 5.2f, 5.6f);
            }
            using var toc = new SolidBrush(Color.FromArgb(120, 70, 30));
            g.FillPie(toc, cx - 2.8f, y + 0.3f, 5.6f, 4.4f, 180f, 180f);
        }
        // Trụ CSDL xám (dùng chung cho ic_database và ic_save)
        private static void VeTruCSDL(Graphics g)
        {
            const float x = 0.8f, w = 11.4f, tren = 1f, cao = 12.6f, e = 3.6f;
            using var than = new GraphicsPath();
            than.AddArc(x, tren, w, e, 180f, 180f);
            than.AddLine(x + w, tren + e / 2, x + w, tren + cao - e / 2);
            than.AddArc(x, tren + cao - e, w, e, 0f, 180f);
            than.CloseFigure();
            using (var to = new LinearGradientBrush(new RectangleF(x, 0, w, 16), XamSang, XamVua, 0f))
            {
                var cb = new ColorBlend { Colors = new[] { XamVua, XamSang, XamVua, Color.FromArgb(150, 150, 150) }, Positions = new[] { 0f, 0.35f, 0.75f, 1f } };
                to.InterpolationColors = cb;
                g.FillPath(to, than);
            }
            using var vien = new Pen(XamDam, 0.8f);
            g.DrawPath(vien, than);
            using (var mat = new SolidBrush(Color.FromArgb(225, 225, 225))) g.FillEllipse(mat, x, tren, w, e);
            g.DrawEllipse(vien, x, tren, w, e);
            using var ke = new Pen(Color.FromArgb(140, 140, 140), 0.6f);
            g.DrawArc(ke, x, tren + 3.6f, w, e, 0f, 180f);
            g.DrawArc(ke, x, tren + 7.2f, w, e, 0f, 180f);
        }
        // ic_database.png: 2 mũi tên vòng xanh lá ở góc dưới phải
        private static void VeMuiTenVong(Graphics g)
        {
            using var nen = new Pen(LaDam, 3.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var la = new Pen(LaSang, 1.7f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            var o = new RectangleF(7.4f, 7.4f, 7.2f, 7.2f);
            foreach (var but in new[] { nen, la })
            {
                g.DrawArc(but, o, 200f, 110f);
                g.DrawArc(but, o, 20f, 110f);
            }
            using var dau = new SolidBrush(LaVua);
            using var vien = new Pen(LaDam, 0.6f) { LineJoin = LineJoin.Round };
            var d1 = new PointF[] { new(13.2f, 6.2f), new(15.8f, 8.8f), new(12.2f, 9.6f) };
            var d2 = new PointF[] { new(8.8f, 15.8f), new(6.2f, 13.2f), new(9.8f, 12.4f) };
            g.FillPolygon(dau, d1); g.DrawPolygon(vien, d1);
            g.FillPolygon(dau, d2); g.DrawPolygon(vien, d2);
        }
        // ic_save.png: đĩa mềm xanh dương ở góc dưới phải
        private static void VeDia(Graphics g)
        {
            var o = new RectangleF(7.2f, 7.2f, 8.4f, 8.4f);
            using (var to = new LinearGradientBrush(o, LamSang, LamVua, 90f))
            using (var vien = new Pen(LamDam, 0.8f))
            {
                g.FillRectangle(to, o);
                g.DrawRectangle(vien, o.X, o.Y, o.Width, o.Height);
            }
            using var trang = new SolidBrush(Color.White);
            g.FillRectangle(trang, 9f, 7.6f, 4.8f, 3f);
            g.FillRectangle(trang, 9.2f, 12f, 4.4f, 3.2f);
            using var lam = new SolidBrush(LamVua);
            g.FillRectangle(lam, 10.2f, 12.6f, 1.3f, 2.2f);
        }
        // ic_export.png: tờ giấy xanh nhạt (mép cuộn) + ô Excel xanh lá có chữ X ở góc dưới phải
        private static void VeXuatExcel(Graphics g)
        {
            using (var p = new GraphicsPath())
            {
                p.AddLine(2.6f, 1f, 14.4f, 1f);
                p.AddLine(14.4f, 1f, 14.4f, 3f);
                p.AddLine(12.4f, 3f, 12.4f, 12.6f);
                p.AddLine(12.4f, 12.6f, 0.8f, 12.6f);
                p.AddLine(0.8f, 12.6f, 2.6f, 10.6f);
                p.CloseFigure();
                using var to = new LinearGradientBrush(new RectangleF(0, 0, 16, 13), Color.FromArgb(240, 248, 255), Color.FromArgb(200, 225, 250), 0f);
                using var vien = new Pen(LamVua, 0.8f) { LineJoin = LineJoin.Round };
                g.FillPath(to, p);
                g.DrawPath(vien, p);
            }
            var o = new RectangleF(6.6f, 6.6f, 8.8f, 8.8f);
            using (var trang = new SolidBrush(Color.White)) g.FillRectangle(trang, o);
            using (var xanh = new Pen(Color.FromArgb(30, 125, 55), 1.3f))
                g.DrawRectangle(xanh, o.X, o.Y, o.Width, o.Height);
            using var x = new Pen(Color.FromArgb(30, 125, 55), 1.5f) { StartCap = LineCap.Square, EndCap = LineCap.Square };
            g.DrawLine(x, 8.8f, 8.8f, 13.2f, 13.2f);
            g.DrawLine(x, 13.2f, 8.8f, 8.8f, 13.2f);
        }
        // ic_help.png: hình tròn xanh dương có vòng trắng bên trong và dấu hỏi trắng
        private static void VeTroGiup(Graphics g)
        {
            var o = new RectangleF(0.7f, 0.7f, 14.6f, 14.6f);
            using (var to = new LinearGradientBrush(o, LamSang, LamVua, 90f))
            using (var vien = new Pen(LamDam, 0.9f))
            {
                g.FillEllipse(to, o);
                g.DrawEllipse(vien, o);
            }
            using (var vong = new Pen(Color.FromArgb(220, 255, 255, 255), 0.8f))
                g.DrawEllipse(vong, 2.3f, 2.3f, 11.4f, 11.4f);
            using var but = new Pen(Color.White, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            using (var p = new GraphicsPath())
            {
                p.AddArc(5.6f, 4f, 4.8f, 4.4f, 190f, 250f);
                p.AddLine(9.3f, 8.1f, 8f, 9.2f);
                p.AddLine(8f, 9.2f, 8f, 9.8f);
                g.DrawPath(but, p);
            }
            using var cham = new SolidBrush(Color.White);
            g.FillEllipse(cham, 7f, 11f, 2f, 2f);
        }
        // ic_delete.png: bảng (tiêu đề xanh, ô trắng kẻ xanh nhạt) + hình tròn đỏ có gạch trắng ở góc dưới phải
        private static void VeBangXoa(Graphics g)
        {
            var o = new RectangleF(0.6f, 1f, 14f, 12f);
            using (var trang = new SolidBrush(Color.White)) g.FillRectangle(trang, o);
            using (var tieuDe = new LinearGradientBrush(new RectangleF(o.X, o.Y, o.Width, 3f), LamSang, LamVua, 90f))
                g.FillRectangle(tieuDe, o.X, o.Y, o.Width, 3f);
            using (var ke = new Pen(Color.FromArgb(170, 200, 235), 0.6f))
            {
                foreach (float xk in new[] { 4.2f, 7.8f, 11.2f }) g.DrawLine(ke, xk, o.Y + 3f, xk, o.Bottom);
                foreach (float yk in new[] { 6.6f, 9.8f }) g.DrawLine(ke, o.X, yk, o.Right, yk);
            }
            using (var vien = new Pen(LamVua, 0.8f)) g.DrawRectangle(vien, o.X, o.Y, o.Width, o.Height);
            var tron = new RectangleF(8.2f, 8.2f, 7.4f, 7.4f);
            using (var to = new LinearGradientBrush(tron, Color.FromArgb(250, 100, 90), Color.FromArgb(200, 25, 25), 90f))
            using (var vienDo = new Pen(Color.FromArgb(140, 15, 15), 0.8f))
            {
                g.FillEllipse(to, tron);
                g.DrawEllipse(vienDo, tron);
            }
            using var gach = new SolidBrush(Color.White);
            g.FillRectangle(gach, 9.8f, 11.2f, 4.2f, 1.4f);
        }
        // ic_warning.png: hình thoi vàng cam, chấm than xanh đậm
        private static void VeThoiCanhBao(Graphics g)
        {
            using (var p = new GraphicsPath())
            {
                p.AddPolygon(new PointF[] { new(8f, 0.6f), new(15.4f, 8f), new(8f, 15.4f), new(0.6f, 8f) });
                using var to = new LinearGradientBrush(new RectangleF(0, 0, 16, 16), Color.FromArgb(255, 235, 140), Color.FromArgb(245, 175, 30), 90f);
                using var vien = new Pen(Color.FromArgb(200, 125, 10), 0.9f) { LineJoin = LineJoin.Round };
                g.FillPath(to, p);
                g.DrawPath(vien, p);
            }
            using var cham = new SolidBrush(Color.FromArgb(30, 40, 120));
            g.FillRectangle(cham, 7.2f, 3.6f, 1.6f, 6.2f);
            g.FillRectangle(cham, 7.2f, 11f, 1.6f, 1.6f);
        }
        // ic_default.png: cờ lê xám + tua vít cán xanh bắt chéo
        private static void VeDungCu(Graphics g)
        {
            using (var vien = new Pen(XamDam, 3.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (var than = new Pen(Color.FromArgb(205, 205, 205), 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                // cờ lê: thân chéo từ trên-trái xuống dưới-phải
                g.DrawLine(vien, 3.4f, 3.4f, 11.6f, 11.6f);
                g.DrawLine(than, 3.4f, 3.4f, 11.6f, 11.6f);
                // tua vít: thân kim loại chéo từ trên-phải
                g.DrawLine(vien, 14.4f, 1.6f, 8.6f, 7.4f);
                g.DrawLine(than, 14.4f, 1.6f, 8.6f, 7.4f);
            }
            // đầu cờ lê: vòng xám có khe
            using (var dau = new SolidBrush(Color.FromArgb(200, 200, 200)))
            using (var vien = new Pen(XamDam, 0.8f))
            {
                g.FillEllipse(dau, 0.6f, 0.6f, 5.6f, 5.6f);
                g.DrawEllipse(vien, 0.6f, 0.6f, 5.6f, 5.6f);
            }
            using (var khe = new SolidBrush(Color.Transparent))
            {
                var cu = g.CompositingMode;
                g.CompositingMode = CompositingMode.SourceCopy;
                g.FillPolygon(khe, new PointF[] { new(0.2f, 2.6f), new(2.6f, 0.2f), new(4.2f, 1.8f), new(1.8f, 4.2f) });
                g.CompositingMode = cu;
            }
            // cán xanh dương của 2 dụng cụ
            using var canVien = new Pen(LamDam, 3.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var can = new Pen(Color.FromArgb(80, 160, 235), 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(canVien, 11.6f, 11.6f, 14.2f, 14.2f);
            g.DrawLine(can, 11.6f, 11.6f, 14.2f, 14.2f);
            g.DrawLine(canVien, 8f, 8f, 2.4f, 13.6f);
            g.DrawLine(can, 8f, 8f, 2.4f, 13.6f);
        }
        // ---------------- Tiện ích hình ----------------
        private static GraphicsPath NgoiSao(float cx, float cy, float rNgoai, float rTrong)
        {
            var pts = new PointF[10];
            for (int i = 0; i < 10; i++)
            {
                double goc = -Math.PI / 2 + i * Math.PI / 5;
                float r = (i % 2 == 0) ? rNgoai : rTrong;
                pts[i] = new PointF(cx + (float)(r * Math.Cos(goc)), cy + (float)(r * Math.Sin(goc)));
            }
            var p = new GraphicsPath();
            p.AddPolygon(pts);
            return p;
        }
        private static GraphicsPath TamGiacBo(PointF a, PointF b, PointF c, float bk)
        {
            // Tam giác bo nhẹ góc: nối các điểm lùi vào từ đỉnh bằng đường cong Bezier qua đỉnh
            var dinh = new[] { a, b, c };
            var p = new GraphicsPath();
            for (int i = 0; i < 3; i++)
            {
                PointF truoc = dinh[(i + 2) % 3], d = dinh[i], sau = dinh[(i + 1) % 3];
                PointF p1 = LuiVe(d, truoc, bk), p2 = LuiVe(d, sau, bk);
                p.AddBezier(p1, d, d, p2);
            }
            p.CloseFigure();
            return p;
        }
        private static PointF LuiVe(PointF tu, PointF den, float d)
        {
            float dx = den.X - tu.X, dy = den.Y - tu.Y;
            float l = MathF.Sqrt(dx * dx + dy * dy);
            return l < 0.001f ? tu : new PointF(tu.X + dx / l * d, tu.Y + dy / l * d);
        }
    }
}
