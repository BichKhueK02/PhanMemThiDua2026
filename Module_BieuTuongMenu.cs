using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using Krypton.Toolkit;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Biểu tượng 8 nút menu trái của Form2.
    ///  - "Hiện đại": VẼ bằng GDI+ lúc chạy (nét mảnh kiểu web, khung 24x24 nhân theo DPI) — không giải mã ảnh nào khi dựng Form2.
    ///    Mỗi nút có 2 bản: màu thường + màu trắng (rê chuột / nhấn / đang chọn).
    ///  - "Cổ điển": cũng VẼ (GanBieuTuongVeCoDien): màu chữ tím đậm của sidebar cổ điển, mục đang chọn => trắng.
    /// Nút được nhận ra theo Name (Trangchu, XuLyData, CaiDat, NhatKy, ThongKe, KhenThuong, HuongDan, Thoat).
    /// Mọi lỗi đều bỏ qua: nút chỉ mất biểu tượng, không ảnh hưởng chức năng.
    /// </summary>
    internal static class Module_BieuTuongMenu
    {
        private static readonly (string KhoaTen, string Loai)[] BangTen =
        {
            ("Trangchu", "TrangChu"), ("XuLyData", "DuLieu"), ("CaiDat", "CaiDat"), ("NhatKy", "NhatKy"),
            ("ThongKe", "ThongKe"), ("KhenThuong", "KhenThuong"), ("HuongDan", "HuongDan"), ("Thoat", "Thoat"),
        };
        /// <summary>Màu riêng của từng biểu tượng menu (kiểu Hiện đại, trên nền tối).</summary>
        public static Color MauRieng(string? loai) => loai switch
        {
            "TrangChu" => Color.FromArgb(96, 165, 250),     // xanh dương
            "DuLieu" => Color.FromArgb(34, 211, 238),       // xanh lơ
            "CaiDat" => Color.FromArgb(167, 139, 250),      // tím nhạt
            "NhatKy" => Color.FromArgb(52, 211, 153),       // xanh ngọc
            "ThongKe" => Color.FromArgb(251, 146, 60),      // cam
            "KhenThuong" => Color.FromArgb(250, 204, 21),   // vàng
            "HuongDan" => Color.FromArgb(244, 114, 182),    // hồng
            "Thoat" => Color.FromArgb(251, 113, 133),       // đỏ hồng
            _ => Color.FromArgb(190, 184, 228)
        };
        public static string? LoaiCuaNut(Control b)
        {
            foreach (var (k, loai) in BangTen)
                if (b.Name.Contains(k, StringComparison.OrdinalIgnoreCase)) return loai;
            return null;
        }
        // ======================================================================
        //  HIỆN ĐẠI: biểu tượng vẽ
        // ======================================================================
        private sealed class CapAnh { public Bitmap Thuong = null!, Sang = null!; }
        private static readonly ConditionalWeakTable<KryptonButton, CapAnh> _anhVe = new();
        /// <summary>Gán biểu tượng vẽ cho nút (màu thường + bản trắng khi rê chuột / nhấn).</summary>
        public static void GanBieuTuongVe(KryptonButton b, int coPx, Color mauThuong, Color mauSang)
        {
            try
            {
                string? loai = LoaiCuaNut(b);
                if (loai == null) return;
                var cap = new CapAnh { Thuong = Ve(loai, coPx, mauThuong), Sang = Ve(loai, coPx, mauSang) };
                _anhVe.AddOrUpdate(b, cap);
                b.Values.Image = cap.Thuong;
                b.Values.ImageStates.ImageTracking = cap.Sang;
                b.Values.ImageStates.ImagePressed = cap.Sang;
                b.Values.ImageStates.ImageCheckedTracking = cap.Sang;
                b.Values.ImageStates.ImageCheckedPressed = cap.Sang;
                b.Disposed += (s, e) => { cap.Thuong.Dispose(); cap.Sang.Dispose(); };
            }
            catch (Exception ex) { Debug.WriteLine("[BieuTuongMenu] Vẽ " + b.Name + ": " + ex.Message); }
        }
        /// <summary>Mục đang chọn (nền tím, chữ trắng) => biểu tượng trắng; bỏ chọn => về màu thường.</summary>
        public static void DatDangChon(KryptonButton b, bool dangChon)
        {
            if (!_anhVe.TryGetValue(b, out var cap)) return;
            var anh = dangChon ? cap.Sang : cap.Thuong;
            if (!ReferenceEquals(b.Values.Image, anh)) b.Values.Image = anh;
        }
        /// <summary>Vẽ 1 biểu tượng (khung thiết kế 24x24, nét 1.8) ra ảnh vuông coPx.</summary>
        internal static Bitmap Ve(string loai, int coPx, Color mau)
        {
            coPx = Math.Max(12, coPx);
            var bmp = new Bitmap(coPx, coPx, PixelFormat.Format32bppPArgb);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);
            g.ScaleTransform(coPx / 24f, coPx / 24f);
            using var but = new Pen(mau, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            switch (loai)
            {
                case "TrangChu":   // ngôi nhà
                    g.DrawLines(but, new PointF[] { new(3f, 11.5f), new(12f, 3.8f), new(21f, 11.5f) });
                    g.DrawLines(but, new PointF[] { new(5.5f, 9.6f), new(5.5f, 20.2f), new(18.5f, 20.2f), new(18.5f, 9.6f) });
                    g.DrawLines(but, new PointF[] { new(10f, 20.2f), new(10f, 14.5f), new(14f, 14.5f), new(14f, 20.2f) });
                    break;
                case "DuLieu":     // cơ sở dữ liệu (trụ 3 tầng)
                    g.DrawEllipse(but, 5f, 3.5f, 14f, 5f);
                    g.DrawLine(but, 5f, 6f, 5f, 18f);
                    g.DrawLine(but, 19f, 6f, 19f, 18f);
                    g.DrawArc(but, 5f, 9.5f, 14f, 5f, 0f, 180f);
                    g.DrawArc(but, 5f, 15.5f, 14f, 5f, 0f, 180f);
                    break;
                case "CaiDat":     // bánh răng
                    g.DrawEllipse(but, 12f - 6f, 12f - 6f, 12f, 12f);
                    g.DrawEllipse(but, 12f - 2.6f, 12f - 2.6f, 5.2f, 5.2f);
                    using (var rang = new Pen(mau, 2.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        for (int i = 0; i < 8; i++)
                        {
                            double a = i * Math.PI / 4;
                            float c = (float)Math.Cos(a), s = (float)Math.Sin(a);
                            g.DrawLine(rang, 12f + 6.6f * c, 12f + 6.6f * s, 12f + 8.6f * c, 12f + 8.6f * s);
                        }
                    break;
                case "NhatKy":     // trang nhật ký có dòng chữ
                    using (var p = BoGoc(new RectangleF(5f, 3.5f, 14f, 17f), 2.5f)) g.DrawPath(but, p);
                    g.DrawLine(but, 8.5f, 8.5f, 15.5f, 8.5f);
                    g.DrawLine(but, 8.5f, 12.2f, 15.5f, 12.2f);
                    g.DrawLine(but, 8.5f, 15.9f, 12.8f, 15.9f);
                    break;
                case "ThongKe":    // biểu đồ cột
                    g.DrawLine(but, 3.8f, 20.2f, 20.2f, 20.2f);
                    using (var p = BoGoc(new RectangleF(5.5f, 12.5f, 3.4f, 7.7f), 1f)) g.DrawPath(but, p);
                    using (var p = BoGoc(new RectangleF(10.3f, 8f, 3.4f, 12.2f), 1f)) g.DrawPath(but, p);
                    using (var p = BoGoc(new RectangleF(15.1f, 4f, 3.4f, 16.2f), 1f)) g.DrawPath(but, p);
                    break;
                case "KhenThuong": // huy chương: vòng tròn ngôi sao + 2 dải ruy băng
                    g.DrawLines(but, new PointF[] { new(8.6f, 13.6f), new(7f, 21f), new(12f, 18.6f), new(17f, 21f), new(15.4f, 13.6f) });
                    g.DrawEllipse(but, 12f - 6f, 3f, 12f, 12f);
                    g.DrawPolygon(but, Sao(12f, 9f, 3.1f, 1.35f));
                    break;
                case "HuongDan":   // quyển sách mở
                    g.DrawLines(but, new PointF[] { new(12f, 7f), new(9f, 5f), new(3.5f, 5f), new(3.5f, 18f), new(9f, 18f), new(12f, 20f) });
                    g.DrawLines(but, new PointF[] { new(12f, 7f), new(15f, 5f), new(20.5f, 5f), new(20.5f, 18f), new(15f, 18f), new(12f, 20f) });
                    g.DrawLine(but, 12f, 7f, 12f, 20f);
                    break;
                case "NguoiDung":  // ô tên tài khoản: đầu + vai
                    g.DrawEllipse(but, 8.2f, 3.8f, 7.6f, 7.6f);
                    g.DrawArc(but, 4.5f, 13.2f, 15f, 14f, 200f, 140f);
                    break;
                case "KhoaMK":     // ô mật khẩu: ổ khóa
                    using (var p = BoGoc(new RectangleF(5f, 10.5f, 14f, 10f), 2.2f)) g.DrawPath(but, p);
                    g.DrawArc(but, 8f, 4f, 8f, 10f, 180f, 180f);
                    g.DrawLine(but, 8f, 9f, 8f, 10.5f);
                    g.DrawLine(but, 16f, 9f, 16f, 10.5f);
                    g.DrawLine(but, 12f, 14.3f, 12f, 16.7f);
                    break;
                case "Mat":        // hiện mật khẩu: con mắt
                case "MatTat":     // ẩn mật khẩu: con mắt có gạch chéo
                    g.DrawBezier(but, 2.5f, 12f, 6f, 5.5f, 18f, 5.5f, 21.5f, 12f);
                    g.DrawBezier(but, 2.5f, 12f, 6f, 18.5f, 18f, 18.5f, 21.5f, 12f);
                    g.DrawEllipse(but, 9.2f, 9.2f, 5.6f, 5.6f);
                    if (loai == "MatTat") g.DrawLine(but, 4f, 20f, 20f, 4f);
                    break;
                case "Thoat":      // nút nguồn: vòng tròn hở phía trên + vạch dọc
                    g.DrawArc(but, 4.4f, 5.2f, 15.2f, 15.2f, 270f + 38f, 360f - 76f);
                    g.DrawLine(but, 12f, 2.8f, 12f, 11.6f);
                    break;
            }
            return bmp;
        }
        private static PointF[] Sao(float cx, float cy, float rNgoai, float rTrong)
        {
            var d = new PointF[10];
            for (int i = 0; i < 10; i++)
            {
                double a = -Math.PI / 2 + i * Math.PI / 5;
                float r = i % 2 == 0 ? rNgoai : rTrong;
                d[i] = new PointF(cx + r * (float)Math.Cos(a), cy + r * (float)Math.Sin(a));
            }
            return d;
        }
        private static GraphicsPath BoGoc(RectangleF r, float bk)
        {
            var p = new GraphicsPath();
            float d = bk * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
        // ======================================================================
        //  BIỂU TƯỢNG CHỨNG THƯ SỐ (thanh trên Form2): tấm khiên gradient xanh–tím + dấu tích trắng
        // ======================================================================
        /// <summary>Gán biểu tượng chứng thư số vẽ tay cho ô ảnh (cỡ theo chiều cao ô, nét theo DPI). Lỗi => để trống.</summary>
        public static void GanBieuTuongChungThu(PictureBox pb)
        {
            try
            {
                int co = Math.Max(16, Math.Min(pb.Height, pb.Width) - 2);
                var cu = pb.Image;
                pb.SizeMode = PictureBoxSizeMode.Zoom;
                pb.Image = VeChungThu(co);
                if (cu != null && !ReferenceEquals(cu, pb.Image)) cu.Dispose();
            }
            catch (Exception ex) { Debug.WriteLine("[BieuTuongMenu] Chứng thư: " + ex.Message); }
        }
        internal static Bitmap VeChungThu(int coPx)
        {
            coPx = Math.Max(16, coPx);
            var bmp = new Bitmap(coPx, coPx, PixelFormat.Format32bppPArgb);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);
            g.ScaleTransform(coPx / 24f, coPx / 24f);
            // Tấm khiên
            using var khien = new GraphicsPath();
            khien.AddBezier(12f, 1.6f, 15f, 3.4f, 18.2f, 4.2f, 21f, 4.3f);
            khien.AddLine(21f, 4.3f, 21f, 11f);
            khien.AddBezier(21f, 11f, 21f, 16.6f, 17.2f, 20.4f, 12f, 22.6f);
            khien.AddBezier(12f, 22.6f, 6.8f, 20.4f, 3f, 16.6f, 3f, 11f);
            khien.AddLine(3f, 11f, 3f, 4.3f);
            khien.AddBezier(3f, 4.3f, 5.8f, 4.2f, 9f, 3.4f, 12f, 1.6f);
            khien.CloseFigure();
            using (var to = new LinearGradientBrush(new RectangleF(3f, 1.6f, 18f, 21f), Color.FromArgb(34, 197, 94), Color.FromArgb(21, 128, 61), 60f))   // xanh lá cây
                g.FillPath(to, khien);
            // Ánh sáng nhẹ nửa trái
            using (var sang = new GraphicsPath())
            {
                sang.AddBezier(12f, 1.6f, 9f, 3.4f, 5.8f, 4.2f, 3f, 4.3f);
                sang.AddLine(3f, 4.3f, 3f, 11f);
                sang.AddBezier(3f, 11f, 3f, 16.6f, 6.8f, 20.4f, 12f, 22.6f);
                sang.CloseFigure();
                using var bSang = new SolidBrush(Color.FromArgb(40, Color.White));
                g.FillPath(bSang, sang);
            }
            // Dấu tích trắng
            using var tich = new Pen(Color.White, 2.3f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            g.DrawLines(tich, new PointF[] { new(7.6f, 12.2f), new(10.8f, 15.3f), new(16.6f, 8.8f) });
            return bmp;
        }
        // ======================================================================
        //  CỔ ĐIỂN: cũng vẽ (ảnh gốc đã bỏ khỏi exe)
        // ======================================================================
        /// <summary>
        /// Sidebar cổ điển: nền tím, rê chuột => nền lavender sáng (chữ vẫn tím đậm) nên biểu tượng KHÔNG đổi màu khi rê/nhấn;
        /// chỉ mục đang chọn (viên tím đậm, chữ trắng) mới dùng bản trắng — Module_SidebarCoDien.DatTrangThai gọi DatDangChon.
        /// </summary>
        public static void GanBieuTuongVeCoDien(int coPx, Color mau, params KryptonButton[] cacNut)
        {
            foreach (var b in cacNut)
            {
                if (b == null) continue;
                GanBieuTuongVe(b, coPx, mau, Color.White);
                if (b.Name.Contains("Thoat", StringComparison.OrdinalIgnoreCase)) continue;   // Thoát: rê chuột nền đỏ chữ trắng => giữ bản trắng
                b.Values.ImageStates.ImageTracking = null;
                b.Values.ImageStates.ImagePressed = null;
            }
        }
    }
}
