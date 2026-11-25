using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Cách vẽ danh sách "Thông tin nhanh" (Form4) ở kiểu giao diện "Hiện đại". Chỉ trình bày: dữ liệu, thứ tự, dòng ghim,
    /// giới hạn số dòng... vẫn do Module_ThongBao quản lý như cũ (kiểu "Cổ điển" giữ nguyên cách vẽ cũ).
    ///  - Dòng ghim "  Nhãn: giá trị"  => chấm màu + nhãn xám + giá trị chữ đậm.
    ///  - Dòng "-----"                 => đường kẻ mảnh.
    ///  - Dòng thông báo "HH:mm:ss ✔ ..." => biểu tượng tròn TỰ VẼ theo loại (không dùng emoji bị vỡ font) + nội dung + giờ mờ bên phải.
    ///  - Chữ vẽ bằng TextRenderer (nền đặc => ClearType sắc nét), chiều cao dòng nhân theo DPI (trước: cố định 23 px nên bị cắt ở 125–150%).
    /// </summary>
    internal static class Module_ThongBaoHienDai
    {
        private static readonly Font FontNhan = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Regular);
        private static readonly Font FontGiaTri = new Font(Module_HeThong.TenFontHeThongDam, 9F, FontStyle.Regular);
        private static readonly Font FontNoiDung = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Regular);
        private static readonly Font FontGio = new Font(Module_HeThong.TenFontHeThong, 8.25F, FontStyle.Regular);
        private static readonly Color MauNhan = Color.FromArgb(100, 116, 139);
        private static readonly Color MauGiaTri = Color.FromArgb(15, 23, 42);
        private static readonly Color MauNoiDung = Color.FromArgb(30, 41, 59);
        private static readonly Color MauGio = Color.FromArgb(148, 163, 184);
        private static readonly Color MauKe = Color.FromArgb(226, 232, 240);
        private static readonly Color MauChon = Color.FromArgb(245, 243, 255);
        private static readonly Color MauVienChon = Color.FromArgb(221, 214, 254);
        private static readonly Regex MauGioDau = new(@"^(\d{2}:\d{2}:\d{2})\s*", RegexOptions.Compiled);
        private const TextFormatFlags CoChu = TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter |
                                              TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.Left;
        // Dòng gọn (trước 30 px/96 DPI => quá thưa, ít dòng hiện được): 22 px vừa chữ 9pt + biểu tượng 14 px, vẫn nhân theo DPI
        public static int ChieuCaoDong(Control c) => Math.Max(18, (int)Math.Round(22 * c.DeviceDpi / 96f));
        public static void VeDong(ListBox lb, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= lb.Items.Count) return;
            var g = e.Graphics;
            float k = lb.DeviceDpi / 96f;
            int P(float v) => (int)Math.Round(v * k);
            var r = e.Bounds;
            bool chon = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color nen = lb.BackColor;
            using (var bNen = new SolidBrush(nen)) g.FillRectangle(bNen, r);
            if (lb.Items[e.Index] is not DongThongBao item) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (chon)
            {
                var o = new Rectangle(r.X + P(4), r.Y + P(1), r.Width - P(8), r.Height - P(2));
                using var p = BoGoc(o, P(6));
                using var bChon = new SolidBrush(MauChon);
                using var vien = new Pen(MauVienChon);
                g.FillPath(bChon, p);
                g.DrawPath(vien, p);
                nen = MauChon;
            }
            string text = item.Text ?? "";
            // ---- Dòng kẻ phân cách ----
            if (text.Length > 0 && text.Trim().All(ch => ch == '-'))
            {
                using var but = new Pen(MauKe, Math.Max(1f, k));
                int y = r.Y + r.Height / 2;
                g.DrawLine(but, r.X + P(12), y, r.Right - P(12), y);
                return;
            }
            int x = r.X + P(12);
            // ---- Dòng ghim: "Nhãn: giá trị" ----
            if (item.LaGhim)
            {
                string t = text.Trim();
                bool loi = item.Mau.ToArgb() == Color.Red.ToArgb();
                Color nhan = loi ? Color.FromArgb(220, 38, 38) : (item.Mau.ToArgb() == Color.Gray.ToArgb() ? Color.FromArgb(148, 163, 184) : Color.FromArgb(124, 58, 237));
                int d = P(6);
                using (var bc = new SolidBrush(nhan)) g.FillEllipse(bc, x, r.Y + (r.Height - d) / 2, d, d);
                x += d + P(10);
                int hai = t.IndexOf(": ", StringComparison.Ordinal);
                var vung = new Rectangle(x, r.Y, r.Right - x - P(10), r.Height);
                if (hai > 0 && !loi)
                {
                    string phanNhan = t.Substring(0, hai + 1) + " ";
                    string giaTri = t.Substring(hai + 2);
                    int rongNhan = TextRenderer.MeasureText(g, phanNhan, FontNhan, new Size(int.MaxValue, r.Height), CoChu).Width;
                    TextRenderer.DrawText(g, phanNhan, FontNhan, vung, MauNhan, nen, CoChu);
                    var vungGt = new Rectangle(vung.X + rongNhan, vung.Y, Math.Max(1, vung.Width - rongNhan), vung.Height);
                    TextRenderer.DrawText(g, giaTri, FontGiaTri, vungGt, MauGiaTri, nen, CoChu);
                }
                else TextRenderer.DrawText(g, t, loi ? FontGiaTri : FontNhan, vung, loi ? nhan : MauNhan, nen, CoChu);
                return;
            }
            // ---- Dòng thông báo: "HH:mm:ss <ký hiệu> nội dung" ----
            string gio = "";
            var m = MauGioDau.Match(text);
            if (m.Success) { gio = m.Groups[1].Value; text = text.Substring(m.Length); }
            text = text.TrimStart('✔', '⏳', '❌', '✅', '⚠', ' ', '️');
            string loai = LoaiTheoMau(item.Mau);
            int dIcon = P(14);
            VeIcon(g, loai, item.Mau, new Rectangle(x, r.Y + (r.Height - dIcon) / 2, dIcon, dIcon));
            x += dIcon + P(8);
            int rongGio = gio.Length > 0 ? TextRenderer.MeasureText(g, gio, FontGio, new Size(int.MaxValue, r.Height), CoChu).Width + P(12) : 0;
            var vungNd = new Rectangle(x, r.Y, Math.Max(1, r.Right - x - rongGio - P(6)), r.Height);
            TextRenderer.DrawText(g, text, FontNoiDung, vungNd, loai == "Loi" ? Color.FromArgb(185, 28, 28) : MauNoiDung, nen, CoChu);
            if (rongGio > 0)
                TextRenderer.DrawText(g, gio, FontGio, new Rectangle(r.Right - rongGio, r.Y, rongGio - P(10), r.Height), MauGio, nen,
                    (CoChu & ~TextFormatFlags.Left) | TextFormatFlags.Right);
        }
        private static string LoaiTheoMau(Color c)
        {
            int a = c.ToArgb();
            if (a == Color.ForestGreen.ToArgb()) return "ThanhCong";
            if (a == Color.Red.ToArgb()) return "Loi";
            if (a == Color.DarkOrange.ToArgb()) return "DangXuLy";
            return "ThongTin";
        }
        private static void VeIcon(Graphics g, string loai, Color mauGoc, Rectangle o)
        {
            Color mau = loai switch
            {
                "ThanhCong" => Color.FromArgb(22, 163, 74),
                "Loi" => Color.FromArgb(220, 38, 38),
                "DangXuLy" => Color.FromArgb(234, 88, 12),
                _ => Color.FromArgb(124, 58, 237)
            };
            using (var nen = new SolidBrush(Color.FromArgb(36, mau))) g.FillEllipse(nen, o);
            float s = o.Width / 16f;
            float cx = o.X + o.Width / 2f, cy = o.Y + o.Height / 2f;
            using var but = new Pen(mau, Math.Max(1.3f, 1.6f * s)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            switch (loai)
            {
                case "ThanhCong":
                    g.DrawLines(but, new PointF[] { new(cx - 3.6f * s, cy + 0.2f * s), new(cx - 1f * s, cy + 2.6f * s), new(cx + 3.8f * s, cy - 2.6f * s) });
                    break;
                case "Loi":
                    g.DrawLine(but, cx - 3f * s, cy - 3f * s, cx + 3f * s, cy + 3f * s);
                    g.DrawLine(but, cx + 3f * s, cy - 3f * s, cx - 3f * s, cy + 3f * s);
                    break;
                case "DangXuLy":
                    g.DrawLines(but, new PointF[] { new(cx, cy - 3.6f * s), new(cx, cy), new(cx + 2.6f * s, cy + 1.6f * s) });
                    break;
                default:
                    g.DrawLine(but, cx, cy - 0.6f * s, cx, cy + 3.4f * s);
                    g.DrawLine(but, cx, cy - 3.4f * s, cx, cy - 3.3f * s);
                    break;
            }
        }
        private static GraphicsPath BoGoc(Rectangle r, int bk)
        {
            var p = new GraphicsPath();
            int d = Math.Max(2, Math.Min(bk * 2, Math.Min(r.Width, r.Height)));
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
