using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Krypton.Toolkit;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Biểu tượng của Form12 (Cài đặt): ảnh động bánh răng ở đầu trang (pictureBox1) + 15 nút có ảnh.
    /// Cả 2 kiểu giao diện (Hiện đại / Cổ điển) đều VẼ bằng GDI+ lúc dựng form, mỗi nút một màu (nút chính Lưu: biểu tượng trắng);
    /// bánh răng tĩnh có màu => không giải mã ảnh nào, không còn ảnh GIF động; ảnh gốc đã bỏ khỏi exe.
    /// Mọi lỗi đều bỏ qua (nút chỉ mất biểu tượng, không ảnh hưởng chức năng).
    /// </summary>
    internal static class Module_BieuTuongForm12
    {
        private static readonly Color Trang = Color.White;
        private static readonly (string Ten, string Loai, Color Mau)[] BangNut =
        {
            ("kryptonButton_LuuThongTin", "Luu", Trang),
            ("kryptonButton_LuuCauHinh", "Luu", Trang),
            ("kryptonButton_CapNhatDanhSachChiHuyD", "NhomNguoi", Color.FromArgb(124, 58, 237)),
            ("kryptonButton1_CapNhatChucVu", "TheChucVu", Color.FromArgb(217, 119, 6)),
            ("kryptonButton1_CapNhatDanhSachDonVi", "ToaNha", Color.FromArgb(37, 99, 235)),
            ("kryptonButton1_Khoiphuc", "KhoiPhuc", Color.FromArgb(234, 88, 12)),
            ("kryptonButton1_SaoLuu", "SaoLuu", Color.FromArgb(22, 163, 74)),
            ("kryptonButton1_CapNhatTinhThanhPho", "GhimBanDo", Color.FromArgb(220, 38, 38)),
            ("kryptonButton1_BoQuaKiemTraTyLeDoViDacBiet", "NgoiSao", Color.FromArgb(202, 138, 4)),
            ("kryptonButton1_CaiDatFileExcel", "TepTin", Color.FromArgb(13, 148, 136)),
            ("kryptonButton2_ChuyenGiaoDuLieu", "ChuyenDoi", Color.FromArgb(8, 145, 178)),
            ("kryptonButton1_CaiDatTyLePhanTramBCH", "BieuDoTron", Color.Empty),
            ("kryptonButton1_CaiDatTyLePhanTramE29", "BieuDoTron", Color.Empty),
            ("kryptonButton_TyLePhanTramBaNhat", "BieuDoTron", Color.Empty),
            ("kryptonButton1_TyLePhanTramXetThiDuaNam", "BieuDoTron", Color.Empty),
        };
        // 5 mục menu trên cùng (ToolStripMenuItem)
        private static readonly (string Ten, string Loai, Color Mau)[] BangMenu =
        {
            ("quenMatKhauAd_ToolStripMenuItem", "ChiaKhoa", Color.FromArgb(217, 119, 6)),
            ("taoTaiKhoan_ToolStripMenuItem", "ThemNguoi", Color.FromArgb(37, 99, 235)),
            ("goCaiDat_ToolStripMenuItem", "ThungRac", Color.FromArgb(220, 38, 38)),
            ("chuKySo_ToolStripMenuItem", "DanhThiep", Color.FromArgb(22, 163, 74)),
            ("thongTin_ToolStripMenuItem", "ThongTin", Color.FromArgb(37, 99, 235)),
        };
        public static void ApDung(Form f)
        {
            float k = f.DeviceDpi / 96f;
            foreach (var (ten, loai, mau) in BangNut)
            {
                try
                {
                    if (f.Controls.Find(ten, true).FirstOrDefault() is not KryptonButton b) continue;
                    var anh = Ve(loai, (int)Math.Round(18 * k), mau);
                    b.Values.Image = anh;
                    b.Disposed += (s, e) => anh.Dispose();
                }
                catch (Exception ex) { Debug.WriteLine("[BieuTuongForm12] " + ten + ": " + ex.Message); }
            }
            try
            {
                if (f.Controls.Find("menuStrip1", true).FirstOrDefault() is MenuStrip ms)
                {
                    int coMenu = ms.ImageScalingSize.Height > 0 ? ms.ImageScalingSize.Height : (int)Math.Round(21 * k);
                    foreach (var (ten, loai, mau) in BangMenu)
                    {
                        try
                        {
                            if (ms.Items.Find(ten, true).FirstOrDefault() is not ToolStripItem it) continue;
                            it.Image = Ve(loai, coMenu, mau);
                        }
                        catch (Exception ex) { Debug.WriteLine("[BieuTuongForm12] " + ten + ": " + ex.Message); }
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine("[BieuTuongForm12] menu: " + ex.Message); }
            try
            {
                if (f.Controls.Find("pictureBox1", true).FirstOrDefault() is PictureBox pb)
                {
                    pb.SizeMode = PictureBoxSizeMode.Zoom;
                    pb.Image = VeBanhRang((int)Math.Round(56 * k));
                }
            }
            catch (Exception ex) { Debug.WriteLine("[BieuTuongForm12] pictureBox1: " + ex.Message); }
        }
        // ---------------- Vẽ (khung 24x24, nét 1.8) ----------------
        internal static Bitmap Ve(string loai, int coPx, Color mau)
        {
            coPx = Math.Max(12, coPx);
            var bmp = new Bitmap(coPx, coPx, PixelFormat.Format32bppPArgb);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);
            g.ScaleTransform(coPx / 24f, coPx / 24f);
            using var but = new Pen(mau.IsEmpty ? Color.Gray : mau, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            switch (loai)
            {
                case "Luu":         // đĩa lưu
                    using (var p = BoGoc(new RectangleF(4f, 4f, 16f, 16f), 2.4f)) g.DrawPath(but, p);
                    g.DrawLines(but, new PointF[] { new(8f, 4f), new(8f, 8.6f), new(15f, 8.6f), new(15f, 4f) });
                    using (var p = BoGoc(new RectangleF(7.6f, 13f, 8.8f, 7f), 1f)) g.DrawPath(but, p);
                    break;
                case "NhomNguoi":   // nhóm người (danh sách chỉ huy)
                    g.DrawEllipse(but, 5f, 4.5f, 6.4f, 6.4f);
                    g.DrawArc(but, 2.2f, 13.4f, 12f, 11f, 200f, 140f);
                    g.DrawEllipse(but, 14f, 6f, 5f, 5f);
                    g.DrawArc(but, 13f, 13.4f, 9f, 9f, 215f, 110f);
                    break;
                case "TheChucVu":   // thẻ chức vụ
                    using (var p = BoGoc(new RectangleF(3f, 5.5f, 18f, 13f), 2.2f)) g.DrawPath(but, p);
                    g.DrawEllipse(but, 6f, 8.6f, 4.2f, 4.2f);
                    g.DrawLine(but, 5.8f, 15.6f, 10.4f, 15.6f);
                    g.DrawLine(but, 13.2f, 10f, 18f, 10f);
                    g.DrawLine(but, 13.2f, 13.6f, 16.6f, 13.6f);
                    break;
                case "ToaNha":      // tòa nhà (đơn vị)
                    using (var p = BoGoc(new RectangleF(5f, 3.5f, 14f, 17f), 1.6f)) g.DrawPath(but, p);
                    foreach (float y in new[] { 7.5f, 11f })
                    {
                        g.DrawLine(but, 9f, y, 9.4f, y);
                        g.DrawLine(but, 14.6f, y, 15f, y);
                    }
                    g.DrawLines(but, new PointF[] { new(10.3f, 20.5f), new(10.3f, 15.5f), new(13.7f, 15.5f), new(13.7f, 20.5f) });
                    break;
                case "KhoiPhuc":    // khôi phục: mũi tên vòng ngược + kim đồng hồ
                    g.DrawArc(but, 4f, 4f, 16f, 16f, 200f, 290f);
                    g.DrawLines(but, new PointF[] { new(3.6f, 5.6f), new(4.6f, 10.2f), new(9.2f, 9.2f) });
                    g.DrawLines(but, new PointF[] { new(12f, 8.2f), new(12f, 12.2f), new(14.8f, 13.8f) });
                    break;
                case "SaoLuu":      // sao lưu: khay + mũi tên lên
                    g.DrawLines(but, new PointF[] { new(4f, 14.5f), new(4f, 20f), new(20f, 20f), new(20f, 14.5f) });
                    g.DrawLine(but, 12f, 15.5f, 12f, 4f);
                    g.DrawLines(but, new PointF[] { new(7.8f, 8.2f), new(12f, 4f), new(16.2f, 8.2f) });
                    break;
                case "GhimBanDo":   // ghim bản đồ (tỉnh thành)
                    g.DrawArc(but, 5.5f, 3f, 13f, 13f, 135f, 270f);
                    g.DrawLine(but, 7.4f, 14.1f, 12f, 21f);
                    g.DrawLine(but, 16.6f, 14.1f, 12f, 21f);
                    g.DrawEllipse(but, 9.8f, 7.3f, 4.4f, 4.4f);
                    break;
                case "NgoiSao":     // đơn vị đặc biệt: ngôi sao tô nhạt + viền
                    var sao = Sao(12f, 12.6f, 9.4f, 4f);
                    using (var to = new SolidBrush(Color.FromArgb(60, mau))) g.FillPolygon(to, sao);
                    g.DrawPolygon(but, sao);
                    break;
                case "TepTin":      // tệp có góc gấp
                    g.DrawLines(but, new PointF[] { new(14f, 3f), new(6f, 3f), new(6f, 21f), new(19f, 21f), new(19f, 8f), new(14f, 3f), new(14f, 8f), new(19f, 8f) });
                    g.DrawLine(but, 9f, 13f, 16f, 13f);
                    g.DrawLine(but, 9f, 16.5f, 14f, 16.5f);
                    break;
                case "ChuyenDoi":   // chuyển giao: 2 mũi tên ngược chiều
                    g.DrawLine(but, 4f, 8f, 19.5f, 8f);
                    g.DrawLines(but, new PointF[] { new(15.5f, 4f), new(19.5f, 8f), new(15.5f, 12f) });
                    g.DrawLine(but, 20f, 16f, 4.5f, 16f);
                    g.DrawLines(but, new PointF[] { new(8.5f, 12f), new(4.5f, 16f), new(8.5f, 20f) });
                    break;
                case "ChiaKhoa":    // quên mật khẩu: chìa khóa
                    g.DrawEllipse(but, 3.5f, 9f, 8f, 8f);
                    g.DrawLine(but, 11.5f, 13f, 20.5f, 13f);
                    g.DrawLine(but, 17.5f, 13f, 17.5f, 16.5f);
                    g.DrawLine(but, 20.5f, 13f, 20.5f, 16f);
                    break;
                case "ThemNguoi":   // tạo tài khoản: người + dấu cộng
                    g.DrawEllipse(but, 5f, 4f, 7f, 7f);
                    g.DrawArc(but, 1.8f, 13.6f, 13.4f, 12f, 200f, 140f);
                    g.DrawLine(but, 18.5f, 8f, 18.5f, 14f);
                    g.DrawLine(but, 15.5f, 11f, 21.5f, 11f);
                    break;
                case "ThungRac":    // gỡ cài đặt: thùng rác
                    g.DrawLine(but, 4f, 6.5f, 20f, 6.5f);
                    g.DrawLines(but, new PointF[] { new(9.5f, 6.5f), new(9.5f, 3.8f), new(14.5f, 3.8f), new(14.5f, 6.5f) });
                    g.DrawLines(but, new PointF[] { new(6f, 6.5f), new(7f, 20.5f), new(17f, 20.5f), new(18f, 6.5f) });
                    g.DrawLine(but, 10.2f, 10.5f, 10.2f, 16.8f);
                    g.DrawLine(but, 13.8f, 10.5f, 13.8f, 16.8f);
                    break;
                case "DanhThiep":   // thông tin và liên hệ: danh thiếp
                    using (var p = BoGoc(new RectangleF(2.5f, 5f, 19f, 14f), 2.2f)) g.DrawPath(but, p);
                    g.DrawEllipse(but, 5.6f, 8.2f, 4.4f, 4.4f);
                    g.DrawLine(but, 5.2f, 15.8f, 10.4f, 15.8f);
                    g.DrawLine(but, 13f, 9.6f, 18.6f, 9.6f);
                    g.DrawLine(but, 13f, 13f, 17.2f, 13f);
                    break;
                case "ThongTin":    // phiên bản: chữ i trong vòng tròn (tô màu, chữ trắng)
                    using (var nen = new SolidBrush(mau)) g.FillEllipse(nen, 2.5f, 2.5f, 19f, 19f);
                    using (var chu = new Pen(Color.White, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        g.DrawLine(chu, 12f, 11f, 12f, 17f);
                        g.DrawLine(chu, 12f, 7.4f, 12f, 7.5f);
                    }
                    break;
                case "BieuDoTron":  // tỷ lệ %: biểu đồ tròn 3 màu
                    var r = new RectangleF(3f, 3f, 18f, 18f);
                    using (var b1 = new SolidBrush(Color.FromArgb(37, 99, 235))) g.FillPie(b1, r.X, r.Y, r.Width, r.Height, -90f, 216f);
                    using (var b2 = new SolidBrush(Color.FromArgb(22, 163, 74))) g.FillPie(b2, r.X, r.Y, r.Width, r.Height, 126f, 108f);
                    using (var b3 = new SolidBrush(Color.FromArgb(245, 158, 11))) g.FillPie(b3, r.X, r.Y, r.Width, r.Height, 234f, 36f);
                    using (var ke = new Pen(Color.White, 1.1f))
                    {
                        foreach (float goc in new[] { -90f, 126f, 234f })
                        {
                            double a = goc * Math.PI / 180;
                            g.DrawLine(ke, 12f, 12f, 12f + 9f * (float)Math.Cos(a), 12f + 9f * (float)Math.Sin(a));
                        }
                    }
                    break;
            }
            return bmp;
        }
        /// <summary>Bánh răng lớn có màu ở đầu trang Cài đặt (thay ảnh GIF động).</summary>
        internal static Bitmap VeBanhRang(int coPx)
        {
            coPx = Math.Max(16, coPx);
            var bmp = new Bitmap(coPx, coPx, PixelFormat.Format32bppPArgb);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);
            g.ScaleTransform(coPx / 24f, coPx / 24f);
            var khung = new RectangleF(1f, 1f, 22f, 22f);
            using var to = new LinearGradientBrush(khung, Color.FromArgb(59, 130, 246), Color.FromArgb(79, 70, 229), 45f);
            using (var rang = new Pen(to, 3.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                for (int i = 0; i < 8; i++)
                {
                    double a = i * Math.PI / 4 + Math.PI / 8;
                    float c = (float)Math.Cos(a), s = (float)Math.Sin(a);
                    g.DrawLine(rang, 12f + 6.5f * c, 12f + 6.5f * s, 12f + 9.6f * c, 12f + 9.6f * s);
                }
            g.FillEllipse(to, 12f - 7.6f, 12f - 7.6f, 15.2f, 15.2f);
            using (var lo = new SolidBrush(Color.White)) g.FillEllipse(lo, 12f - 3.1f, 12f - 3.1f, 6.2f, 6.2f);
            using (var sang = new SolidBrush(Color.FromArgb(45, Color.White))) g.FillPie(sang, 12f - 7.6f, 12f - 7.6f, 15.2f, 15.2f, 180f, 120f);
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
    }
}
