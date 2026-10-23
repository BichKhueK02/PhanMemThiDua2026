///PHIÊN BẢN 4 - THÊM NỀN KÍNH MỜ (BLUR) KIỂU EDGE / WINDOWS 11
///
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
//TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
namespace PhanMemThiDua2026
{
    /// <summary>
    /// ContextMenuStrip phong cách hiện đại (flat, bo góc, hover bo tròn, fade-in mượt, nền kính mờ).
    /// Cách dùng giữ nguyên: Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1);
    ///
    /// BẢN 3 (thêm so với bản 2):
    ///  - 2 chủ đề mới "Kính tối" và "Kính sáng": nền menu là ẢNH CHỤP phần màn hình phía sau menu,
    ///    được làm mờ + phủ màu (giống acrylic của Edge/Windows 11). Chụp 1 lần lúc mở, giải phóng khi đóng.
    ///  - Chữ phím tắt (Ctrl+R, Ctrl+S...) được làm mờ hơn chữ chính cho mọi chủ đề.
    ///  - BÓNG ĐỔ mềm quanh menu (ánh sáng chiếu từ trên-trái nên bóng lệch xuống dưới-phải), vẽ bằng 1 cửa sổ
    ///    phụ trong suốt nằm ngay dưới menu, tạo khi menu mở và hủy khi menu đóng (không tốn RAM khi không dùng).
    ///  - API công khai không đổi (MenuTheme chỉ thêm tham số tùy chọn doMo).
    /// </summary>
    internal static class Module_MenuChuotPhai
    {
        // Cache tên màu đang chọn trên RAM để tránh đọc ổ cứng liên tục khi mở menu
        public static string MauHienTai { get; set; } = "Mặc định";
        // ===== Tuỳ chỉnh nhanh =====
        private const int BanKinhBoGoc = 10;     // bo góc khung menu
        private const int BanKinhHover = 6;      // bo góc ô hover
        private const int ThoiGianFadeMs = 110;  // tổng thời gian fade-in
        private const int ChuKyTimerMs = 15;     // ~ độ phân giải timer của Windows (nhỏ hơn cũng không mượt hơn)
        private const int HeSoLamMo = 8;         // thu nhỏ ảnh nền 1/8 rồi phóng lại: số càng lớn càng mờ (nên 6 - 12)
                                                 // ===== Bóng đổ (đơn vị px ở 96 DPI, tự nhân theo DPI màn hình) =====
        private const int BanKinhBong = 8;
        private const int LechBongX = -4;
        private const int LechBongY = 8;
        private const int DoDamBong = 50;
        private const int DoDamVien = 24;
        private const int LeBong = 24;
        /// <summary>Tắt/bật hiệu ứng mờ dần. Mặc định theo cài đặt Windows (tắt khi Remote Desktop / tắt UI effects).</summary>
        public static bool BatHieuUngMo { get; set; } =
            SystemInformation.UIEffectsEnabled && !SystemInformation.TerminalServerSession;
        /// <summary>
        /// Tắt/bật nền kính mờ của chủ đề "Kính tối"/"Kính sáng". Tắt thì các chủ đề này tự dùng nền đặc.
        /// Mặc định theo cài đặt Windows (tắt khi Remote Desktop / tắt UI effects để máy yếu không bị chậm).
        /// </summary>
        public static bool BatNenMo { get; set; } =
            SystemInformation.UIEffectsEnabled && !SystemInformation.TerminalServerSession;
        /// <summary>
        /// Tắt/bật bóng đổ quanh menu. Mặc định theo cài đặt Windows (tắt khi Remote Desktop / tắt UI effects).
        /// </summary>
        public static bool BatBongDo { get; set; } =
            SystemInformation.UIEffectsEnabled && !SystemInformation.TerminalServerSession;
        private static readonly Font FontMenu = new Font(Module_HeThong.TenFontHeThong, 10f, FontStyle.Regular);
        private static readonly Padding PaddingItem = new Padding(6, 6, 6, 6); // item cao thoáng hơn
        // ===== Trạng thái gắn kèm từng dropdown (thay cho _daGan + closure + Timer riêng) =====
        private sealed class TrangThai
        {
            public bool DaGanFade, DaGanStyle;
            public int RgnW = -1, RgnH = -1;            // kích thước đã bo góc gần nhất
            public Bitmap? Nen;                         // ảnh nền mờ của lần mở hiện tại (null = dùng nền đặc)
            public CuaSoBong? Bong;                     // cửa sổ bóng đổ của lần mở hiện tại
        }
        private static readonly ConditionalWeakTable<ToolStripDropDown, TrangThai> _trangThai = new();
        private static readonly ConditionalWeakTable<ToolStripDropDown, TrangThai>.CreateValueCallback _taoTrangThai = _ => new TrangThai();
        private static TrangThai LayTrangThai(ToolStripDropDown dd) => _trangThai.GetValue(dd, _taoTrangThai);
        public static void TichHopGiaoDien(ContextMenuStrip menu)
        {
            if (menu == null) return;
            GanHieuUng(menu);
            var tt = LayTrangThai(menu);
            if (tt.DaGanStyle) return;                  // gọi 2 lần không bị gắn trùng sự kiện
            tt.DaGanStyle = true;
            menu.Opening += Menu_OpeningStyle;
        }
        private static void Menu_OpeningStyle(object? sender, CancelEventArgs e)
        {
            if (e.Cancel || sender is not ToolStripDropDown dd) return;
            AreStyle(dd, LayGoiChuDe(MauHienTai));
        }
        // Tự động đọc màu đã lưu từ CSDL2 khi khởi động phần mềm
        public static void KhoiTaoMauTuCSDL(string csdl2Path)
        {
            try
            {
                if (!System.IO.File.Exists(csdl2Path)) return;
                // Pooling=False: không giữ lại kết nối trong pool => không khóa tệp csdl2.db sau khi đọc xong
                using var conn = new SqliteConnection($"Data Source={csdl2Path};Mode=ReadOnly;Pooling=False");
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
        // API cũ: mỗi lần gọi trả về 1 MenuTheme mới (code ngoài có thể sửa thoải mái mà không ảnh hưởng cache nội bộ).
        public static MenuTheme LayChuDe(string tenMau) => TaoChuDe(ChiSoChuDe(tenMau));
        private static int ChiSoChuDe(string? tenMau) => tenMau switch
        {
            "Xanh lá" => 1,
            "Xanh dương" => 2,
            "Xám trắng" => 3,
            "Tối" => 4,
            "Kính tối" => 5,
            "Kính sáng" => 6,
            _ => 0,    // "Mặc định" hoặc giá trị trống
        };
        private static MenuTheme TaoChuDe(int chiSo)
        {
            return chiSo switch
            {
                1 => new MenuTheme(
                    back: Color.FromArgb(232, 247, 236), hover: Color.FromArgb(165, 224, 184),
                    border: Color.FromArgb(110, 190, 140), text: Color.FromArgb(14, 64, 34),
                    disabled: Color.FromArgb(120, 160, 135), separator: Color.FromArgb(190, 228, 203),
                    accent: Color.FromArgb(22, 163, 74)),
                2 => new MenuTheme(
                    back: Color.FromArgb(232, 242, 255), hover: Color.FromArgb(162, 200, 250),
                    border: Color.FromArgb(110, 160, 230), text: Color.FromArgb(12, 40, 90),
                    disabled: Color.FromArgb(125, 150, 190), separator: Color.FromArgb(190, 213, 246),
                    accent: Color.FromArgb(37, 99, 235)),
                3 => new MenuTheme(
                    back: Color.FromArgb(245, 246, 248), hover: Color.FromArgb(214, 218, 226),
                    border: Color.FromArgb(190, 195, 205), text: Color.FromArgb(30, 34, 42),
                    disabled: Color.FromArgb(150, 155, 165), separator: Color.FromArgb(218, 222, 229),
                    accent: Color.FromArgb(71, 85, 105)),
                4 => new MenuTheme(
                    back: Color.FromArgb(24, 26, 33), hover: Color.FromArgb(60, 68, 92),
                    border: Color.FromArgb(70, 76, 96), text: Color.FromArgb(235, 237, 242),
                    disabled: Color.FromArgb(115, 120, 132), separator: Color.FromArgb(50, 54, 66),
                    accent: Color.FromArgb(122, 162, 255)),
                // KÍNH TỐI: giống menu Edge trong ảnh mẫu. doMo = độ đặc của lớp phủ màu (0-255):
                // càng nhỏ càng thấy rõ nền phía sau, càng lớn càng tối/đặc.
                5 => new MenuTheme(
                    back: Color.FromArgb(32, 32, 32), hover: Color.FromArgb(36, 255, 255, 255),
                    border: Color.FromArgb(60, 255, 255, 255), text: Color.FromArgb(245, 245, 245),
                    disabled: Color.FromArgb(120, 120, 120), separator: Color.FromArgb(34, 255, 255, 255),
                    accent: Color.FromArgb(96, 165, 250), doMo: 215),
                // KÍNH SÁNG: bản sáng của acrylic Windows 11
                6 => new MenuTheme(
                    back: Color.FromArgb(250, 250, 252), hover: Color.FromArgb(26, 0, 0, 0),
                    border: Color.FromArgb(46, 0, 0, 0), text: Color.FromArgb(28, 28, 32),
                    disabled: Color.FromArgb(150, 150, 158), separator: Color.FromArgb(26, 0, 0, 0),
                    accent: Color.FromArgb(37, 99, 235), doMo: 205),
                // Mặc định: vàng hổ phách ấm
                _ => new MenuTheme(
                    back: Color.FromArgb(255, 248, 225), hover: Color.FromArgb(255, 224, 130),
                    border: Color.FromArgb(240, 200, 100), text: Color.FromArgb(74, 52, 0),
                    disabled: Color.FromArgb(180, 160, 110), separator: Color.FromArgb(245, 225, 160),
                    accent: Color.FromArgb(245, 158, 11)),
            };
        }
        // Mỗi chủ đề chỉ có đúng 1 bộ (MenuTheme + Renderer) trong suốt vòng đời ứng dụng (tối đa 7 bộ, vài GDI object nhỏ).
        private sealed class GoiChuDe
        {
            public readonly MenuTheme Theme;
            public readonly ModernMenuRenderer Renderer;
            public GoiChuDe(MenuTheme t) { Theme = t; Renderer = new ModernMenuRenderer(t); }
        }
        private static readonly GoiChuDe?[] _goiChuDe = new GoiChuDe?[7];
        private static GoiChuDe LayGoiChuDe(string? tenMau)
        {
            int i = ChiSoChuDe(tenMau);
            return _goiChuDe[i] ??= new GoiChuDe(TaoChuDe(i));
        }
        // Trộn 2 màu: tyLeA = phần của màu a (0..1). Dùng để làm mờ chữ phím tắt.
        private static Color Tron(Color a, Color b, double tyLeA)
        {
            int Mix(int x, int y) => (int)(x * tyLeA + y * (1.0 - tyLeA) + 0.5);
            return Color.FromArgb(Mix(a.R, b.R), Mix(a.G, b.G), Mix(a.B, b.B));
        }
        //  ÁP STYLE
        // Chỉ gán khi khác giá trị hiện tại => mở menu lần 2 trở đi gần như không tốn gì.
        private static void AreStyle(ToolStripDropDown dd, GoiChuDe goi)
        {
            MenuTheme theme = goi.Theme;
            if (!ReferenceEquals(dd.Renderer, goi.Renderer)) dd.Renderer = goi.Renderer; // (không gán RenderMode trước: tránh tạo renderer thừa)
            if (!ReferenceEquals(dd.Font, FontMenu)) dd.Font = FontMenu;
            if (dd.BackColor != theme.Back) dd.BackColor = theme.Back;
            bool coAnh = false;
            var items = dd.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ToolStripItem item = items[i];
                if (item is ToolStripSeparator) continue;
                if (item.Padding != PaddingItem) item.Padding = PaddingItem;
                Color mau = item.Enabled ? theme.Text : theme.Disabled;
                if (item.ForeColor != mau) item.ForeColor = mau;
                if (item is ToolStripMenuItem mi)
                {
                    if (mi.Image != null) coAnh = true;
                    if (mi.HasDropDownItems)
                    {
                        GanHieuUng(mi.DropDown);
                        AreStyle(mi.DropDown, goi); // đệ quy cho submenu
                    }
                }
            }
            if (dd is ToolStripDropDownMenu ddm)
            {
                if (ddm.ShowCheckMargin) ddm.ShowCheckMargin = false;
                if (ddm.ShowImageMargin != coAnh) ddm.ShowImageMargin = coAnh;
            }
        }
        //  HIỆU ỨNG: bo góc + nền mờ + fade-in (1 Timer dùng chung)
        private static void GanHieuUng(ToolStripDropDown dd)
        {
            var tt = LayTrangThai(dd);
            if (tt.DaGanFade) return;                   // mỗi dropdown chỉ gắn 1 lần
            tt.DaGanFade = true;
            // Bóng mặc định của Windows (CS_DROPSHADOW) là hình chữ nhật, không theo góc bo -> tắt để dùng bóng riêng
            if (BatBongDo && dd.DropShadowEnabled) dd.DropShadowEnabled = false;
            dd.Opening += Fade_Opening;
            dd.Opened += Fade_Opened;
            dd.Closed += Fade_Closed;
        }
        // Chủ đề hiện tại có dùng nền kính mờ và tính năng đang bật không
        private static bool CanNenMo() => BatNenMo && LayGoiChuDe(MauHienTai).Theme.DoMo > 0;
        private static void Fade_Opening(object? sender, CancelEventArgs e)
        {
            if (e.Cancel || sender is not ToolStripDropDown dd) return;
            HuyFade(dd);
            GiaiPhongNen(LayTrangThai(dd));
            // Menu phải TRONG SUỐT lúc chụp màn hình, nếu không ảnh chụp sẽ dính luôn chính menu.
            if (BatHieuUngMo || CanNenMo()) dd.Opacity = 0.0;
        }
        private static void Fade_Opened(object? sender, EventArgs e)
        {
            if (sender is not ToolStripDropDown dd) return;
            CapNhatBoGoc(dd);
            if (CanNenMo()) TaoNenMo(dd, LayGoiChuDe(MauHienTai).Theme);   // chụp + làm mờ khi menu còn trong suốt
            if (BatBongDo) HienBong(dd);                                    // sau khi chụp nền để ảnh chụp không dính bóng
            if (BatHieuUngMo) BatDauFade(dd);
            else DatDoMo(dd, 1.0);
        }
        private static void Fade_Closed(object? sender, ToolStripDropDownClosedEventArgs e)
        {
            if (sender is not ToolStripDropDown dd) return;
            HuyFade(dd);
            if (dd.Opacity < 1.0) dd.Opacity = 1.0;
            var tt = LayTrangThai(dd);
            GiaiPhongNen(tt);                           // trả RAM ngay khi menu đóng
            HuyBong(tt);
        }
        private static void GiaiPhongNen(TrangThai tt)
        {
            Bitmap? n = tt.Nen;
            tt.Nen = null;
            n?.Dispose();
        }
        // Chụp vùng màn hình ngay phía sau menu -> thu nhỏ rồi phóng lại (làm mờ) -> phủ màu của chủ đề.
        // Chi phí: vài ms, 2 bitmap tạm cỡ menu (~0,5 MB), bitmap nhỏ được hủy ngay, bitmap nền hủy khi menu đóng.
        private static void TaoNenMo(ToolStripDropDown dd, MenuTheme theme)
        {
            var tt = LayTrangThai(dd);
            GiaiPhongNen(tt);
            int w = dd.Width, h = dd.Height;
            if (w < 8 || h < 8) return;
            Bitmap? nen = null, nho = null;
            try
            {
                nen = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
                using (var g = Graphics.FromImage(nen))
                    g.CopyFromScreen(dd.Left, dd.Top, 0, 0, new Size(w, h), CopyPixelOperation.SourceCopy);
                int nw = Math.Max(1, w / HeSoLamMo), nh = Math.Max(1, h / HeSoLamMo);
                nho = new Bitmap(nw, nh, PixelFormat.Format32bppPArgb);
                using (var g = Graphics.FromImage(nho))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(nen, new Rectangle(0, 0, nw, nh), 0, 0, w, h, GraphicsUnit.Pixel);
                }
                using (var g = Graphics.FromImage(nen))
                using (var ia = new ImageAttributes())
                {
                    ia.SetWrapMode(WrapMode.TileFlipXY);          // tránh viền sáng/tối ở mép khi phóng lại
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(nho, new Rectangle(0, 0, w, h), 0, 0, nw, nh, GraphicsUnit.Pixel, ia);
                    using var phuMau = new SolidBrush(Color.FromArgb(theme.DoMo, theme.Back));
                    g.FillRectangle(phuMau, 0, 0, w, h);
                }
                tt.Nen = nen;
                nen = null;                                        // quyền sở hữu đã chuyển cho TrangThai
                dd.Invalidate();
            }
            catch
            {
                // Không chụp được (vd: màn hình bảo mật/UAC) -> renderer tự dùng nền đặc, menu vẫn dùng bình thường
            }
            finally
            {
                nho?.Dispose();
                nen?.Dispose();
            }
        }
        // Đặt độ mờ menu và bóng của nó cùng lúc để hai thứ hiện ra đồng bộ
        private static void DatDoMo(ToolStripDropDown dd, double v)
        {
            dd.Opacity = v;
            LayTrangThai(dd).Bong?.DatDoMo(v);
        }
        //  BÓNG ĐỔ
        // Bóng là 1 cửa sổ phụ trong suốt, xuyên chuột, nằm ngay DƯỚI menu trong thứ tự z, vẽ bằng ảnh có alpha.
        // Ảnh bóng được vẽ ở độ phân giải 1/3 rồi phóng lại => viền mềm tự nhiên mà gần như không tốn CPU (~1ms).
        private static void HienBong(ToolStripDropDown dd)
        {
            var tt = LayTrangThai(dd);
            HuyBong(tt);
            try
            {
                float dpi = Math.Max(1f, dd.DeviceDpi / 96f);
                int le = (int)Math.Ceiling(LeBong * dpi);
                using Bitmap anh = TaoAnhBong(dd.Width, dd.Height, le, dpi);
                var cs = new CuaSoBong();
                tt.Bong = cs;                                    // gán trước để lỗi giữa chừng vẫn được dọn
                byte alpha = (byte)Math.Clamp((int)Math.Round(dd.Opacity * 255.0), 0, 255);
                cs.Hien(anh, new Point(dd.Left - le, dd.Top - le), dd.Handle, alpha);
            }
            catch
            {
                HuyBong(tt);                                     // lỗi -> menu vẫn dùng bình thường, chỉ không có bóng
            }
        }
        private static void HuyBong(TrangThai tt)
        {
            CuaSoBong? b = tt.Bong;
            tt.Bong = null;
            b?.Dispose();
        }
     
        private static Bitmap TaoAnhBong(int mw, int mh, int le, float dpi)
        {
            const int K = 3;
            int W = mw + 2 * le, H = mh + 2 * le;
            int sw = Math.Max(1, (W + K - 1) / K), sh = Math.Max(1, (H + K - 1) / K);
            float bk = BanKinhBoGoc * dpi;
            using var nho = new Bitmap(sw, sh, PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(nho))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                g.ScaleTransform(1f / K, 1f / K);
                // Bóng chính: lệch trái và xuống dưới.
                VeBong(g,
                    new RectangleF(le + LechBongX * dpi, le + LechBongY * dpi, mw, mh),
                    BanKinhBong * dpi, DoDamBong, 9, bk);
                // Bóng viền mỏng, giảm độ đậm để cạnh trên và phải nhẹ hơn.
                VeBong(g,
                    new RectangleF(le - 1 * dpi, le + 2 * dpi, mw, mh),
                    2 * dpi, DoDamVien, 4, bk);
            }
            var kq = new Bitmap(W, H, PixelFormat.Format32bppArgb);
            try
            {
                using var g = Graphics.FromImage(kq);
                using var ia = new ImageAttributes();
                ia.SetWrapMode(WrapMode.TileFlipXY);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(nho, new Rectangle(0, 0, W, H), 0, 0, sw, sh, GraphicsUnit.Pixel, ia);
                return kq;
            }
            catch
            {
                kq.Dispose();
                throw;
            }
        }
        // Xếp chồng nhiều lớp bo góc mỏng, lớp ngoài to hơn lớp trong => độ đậm giảm dần ra ngoài (giống bóng mờ Gaussian).
        // Công thức alpha từng lớp để khi chồng hết soLop lớp thì đạt đúng alphaToiDa ở sát mép.
        private static void VeBong(Graphics g, RectangleF r, float banKinh, int alphaToiDa, int soLop, float bkGoc)
        {
            int a = (int)Math.Round(255.0 * (1.0 - Math.Pow(1.0 - alphaToiDa / 255.0, 1.0 / soLop)));
            if (a < 1) a = 1;
            using var br = new SolidBrush(Color.FromArgb(a, 0, 0, 0));
            for (int j = 0; j < soLop; j++)
            {
                float mo = banKinh * (1f - (float)j / soLop);
                RectangleF rr = RectangleF.Inflate(r, mo, mo);
                using var path = RoundRectF(rr, bkGoc + mo);
                g.FillPath(br, path);
            }
        }
        private static GraphicsPath RoundRectF(RectangleF r, float radius)
        {
            float d = Math.Max(1f, Math.Min(radius * 2f, Math.Min(r.Width, r.Height)));
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
        // Cửa sổ bóng: layered + transparent (xuyên chuột) + noactivate + toolwindow (không hiện trên taskbar/Alt+Tab).
        private sealed class CuaSoBong : NativeWindow, IDisposable
        {
            private const int WS_POPUP = unchecked((int)0x80000000);
            private const int WS_EX_TOPMOST = 0x00000008, WS_EX_TRANSPARENT = 0x00000020, WS_EX_TOOLWINDOW = 0x00000080,
                              WS_EX_LAYERED = 0x00080000, WS_EX_NOACTIVATE = 0x08000000;
            private const int WM_NCHITTEST = 0x0084, WM_MOUSEACTIVATE = 0x0021;
            private const uint SWP_NOACTIVATE = 0x0010, SWP_SHOWWINDOW = 0x0040;
            private const int ULW_ALPHA = 0x00000002;
            private byte _alpha = 255;
            public CuaSoBong()
            {
                CreateHandle(new CreateParams
                {
                    ClassName = "Static",
                    Style = WS_POPUP,
                    ExStyle = WS_EX_TOPMOST | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_LAYERED | WS_EX_NOACTIVATE,
                    X = 0,
                    Y = 0,
                    Width = 1,
                    Height = 1
                });
            }
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_NCHITTEST) { m.Result = (IntPtr)(-1); return; }      // HTTRANSPARENT: chuột đi xuyên qua
                if (m.Msg == WM_MOUSEACTIVATE) { m.Result = (IntPtr)3; return; }     // MA_NOACTIVATE
                base.WndProc(ref m);
            }
            public void Hien(Bitmap anh, Point viTri, IntPtr duoiCuaSo, byte alpha)
            {
                _alpha = alpha;
                IntPtr dcMan = Nat.GetDC(IntPtr.Zero);
                IntPtr dcBo = Nat.CreateCompatibleDC(dcMan);
                IntPtr hBmp = IntPtr.Zero, cu = IntPtr.Zero;
                try
                {
                    hBmp = anh.GetHbitmap(Color.FromArgb(0));
                    cu = Nat.SelectObject(dcBo, hBmp);
                    var kichThuoc = new Nat.SIZE { CX = anh.Width, CY = anh.Height };
                    var nguon = new Nat.POINT();
                    var dich = new Nat.POINT { X = viTri.X, Y = viTri.Y };
                    var blend = new Nat.BLENDFUNCTION { BlendOp = 0, BlendFlags = 0, SourceConstantAlpha = _alpha, AlphaFormat = 1 };
                    Nat.UpdateLayeredWindow(Handle, dcMan, ref dich, ref kichThuoc, dcBo, ref nguon, 0, ref blend, ULW_ALPHA);
                    // Đặt ngay DƯỚI menu trong thứ tự z, không giành focus
                    Nat.SetWindowPos(Handle, duoiCuaSo, viTri.X, viTri.Y, anh.Width, anh.Height, SWP_NOACTIVATE | SWP_SHOWWINDOW);
                }
                finally
                {
                    if (cu != IntPtr.Zero) Nat.SelectObject(dcBo, cu);
                    if (hBmp != IntPtr.Zero) Nat.DeleteObject(hBmp);
                    Nat.DeleteDC(dcBo);
                    Nat.ReleaseDC(IntPtr.Zero, dcMan);
                }
            }
            // Chỉ đổi độ mờ chung (không vẽ lại ảnh) -> dùng cho hiệu ứng fade-in cùng menu
            public void DatDoMo(double v)
            {
                if (Handle == IntPtr.Zero) return;
                byte a = (byte)Math.Clamp((int)Math.Round(v * 255.0), 0, 255);
                if (a == _alpha) return;
                _alpha = a;
                var blend = new Nat.BLENDFUNCTION { BlendOp = 0, BlendFlags = 0, SourceConstantAlpha = a, AlphaFormat = 1 };
                Nat.UpdateLayeredWindowAlpha(Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, ref blend, ULW_ALPHA);
            }
            public void Dispose()
            {
                if (Handle != IntPtr.Zero) DestroyHandle();
            }
        }
        private static class Nat
        {
            [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
            [StructLayout(LayoutKind.Sequential)] public struct SIZE { public int CX, CY; }
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }
            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize,
                IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);
            [DllImport("user32.dll", EntryPoint = "UpdateLayeredWindow", SetLastError = true)]
            public static extern bool UpdateLayeredWindowAlpha(IntPtr hwnd, IntPtr hdcDst, IntPtr pptDst, IntPtr psize,
                IntPtr hdcSrc, IntPtr pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);
            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
            [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr hWnd);
            [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
            [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
            [DllImport("gdi32.dll")] public static extern bool DeleteDC(IntPtr hdc);
            [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
            [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr hObject);
        }
        // Bo góc khung menu: chỉ tạo lại Region khi kích thước đổi (menu mở lại cùng kích thước => bỏ qua)
        private static void CapNhatBoGoc(ToolStripDropDown dd)
        {
            var tt = LayTrangThai(dd);
            int w = dd.Width, h = dd.Height;
            if (w == tt.RgnW && h == tt.RgnH && dd.Region != null) return;
            try
            {
                using var path = RoundRect(new Rectangle(0, 0, w, h), BanKinhBoGoc);
                var old = dd.Region;
                dd.Region = new Region(path);
                old?.Dispose();
                tt.RgnW = w; tt.RgnH = h;
            }
            catch { }
        }
        private sealed class MucFade
        {
            public ToolStripDropDown DD = null!;
            public long Bat;
        }
        private static readonly List<MucFade> _dangFade = new(4);
        private static System.Windows.Forms.Timer? _timerFade;
        private static void BatDauFade(ToolStripDropDown dd)
        {
            HuyFade(dd);
            _dangFade.Add(new MucFade { DD = dd, Bat = Stopwatch.GetTimestamp() });
            if (_timerFade == null)
            {
                _timerFade = new System.Windows.Forms.Timer { Interval = ChuKyTimerMs };
                _timerFade.Tick += Fade_Tick;
            }
            if (!_timerFade.Enabled) _timerFade.Start();
        }
        private static void HuyFade(ToolStripDropDown dd)
        {
            for (int i = _dangFade.Count - 1; i >= 0; i--)
                if (ReferenceEquals(_dangFade[i].DD, dd)) _dangFade.RemoveAt(i);
        }
        // Độ mờ tính theo THỜI GIAN THỰC (ease-out): máy chậm bị rớt khung thì vẫn kết thúc đúng ~110ms, không kéo dài.
        private static void Fade_Tick(object? sender, EventArgs e)
        {
            long now = Stopwatch.GetTimestamp();
            for (int i = _dangFade.Count - 1; i >= 0; i--)
            {
                var m = _dangFade[i];
                var dd = m.DD;
                if (dd.IsDisposed) { _dangFade.RemoveAt(i); continue; }
                if (!dd.Visible) { _dangFade.RemoveAt(i); dd.Opacity = 1.0; continue; }
                double t = (now - m.Bat) * 1000.0 / Stopwatch.Frequency / ThoiGianFadeMs;
                if (t >= 1.0) { DatDoMo(dd, 1.0); _dangFade.RemoveAt(i); }
                else { double nghich = 1.0 - t; DatDoMo(dd, 1.0 - nghich * nghich); }
            }
            if (_dangFade.Count == 0) _timerFade?.Stop();
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
        // Được tạo 1 lần cho mỗi chủ đề và dùng chung mọi menu => giữ sẵn Brush/Pen/Path, không tạo lại khi vẽ.
        private sealed class ModernMenuRenderer : ToolStripProfessionalRenderer
        {
            private readonly MenuTheme _t;
            private readonly SolidBrush _brBack, _brHover, _brAccent;
            private readonly Pen _penBorder, _penSep, _penTick;
            private readonly Color _mauPhimTat;               // màu chữ phím tắt: mờ hơn chữ chính
            private GraphicsPath? _pathBorder;                 // khung menu (cache theo kích thước)
            private int _bw = -1, _bh = -1;
            private GraphicsPath? _pathHover;                  // ô hover (cache theo kích thước item)
            private int _hw = -1, _hh = -1;
            public ModernMenuRenderer(MenuTheme t) : base(new ThemeColorTable(t))
            {
                _t = t;
                RoundedEdges = false;
                _brBack = new SolidBrush(Color.FromArgb(255, t.Back));
                _brHover = new SolidBrush(t.Hover);            // Hover có thể có alpha (chủ đề kính) -> tự hòa vào nền
                _brAccent = new SolidBrush(t.Accent);
                _penBorder = new Pen(t.Border, 1f);
                _penSep = new Pen(t.Separator, 1f);
                _penTick = new Pen(Color.White, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                _mauPhimTat = Tron(t.Text, t.Back, 0.6);
            }
            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                // Nền kính mờ: vẽ ảnh đã chụp + làm mờ (chỉ khi còn khớp kích thước menu)
                if (e.ToolStrip is ToolStripDropDown dd)
                {
                    Bitmap? nen = LayTrangThai(dd).Nen;
                    if (nen != null && nen.Width == dd.Width && nen.Height == dd.Height)
                    {
                        Rectangle r = e.AffectedBounds;
                        e.Graphics.DrawImage(nen, r, r.X, r.Y, r.Width, r.Height, GraphicsUnit.Pixel);
                        return;
                    }
                }
                e.Graphics.FillRectangle(_brBack, e.AffectedBounds);
            }
            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                if (e.ToolStrip is not ToolStripDropDown) { base.OnRenderToolStripBorder(e); return; }
                int w = e.ToolStrip.Width - 1, h = e.ToolStrip.Height - 1;
                if (_pathBorder == null || w != _bw || h != _bh)
                {
                    _pathBorder?.Dispose();
                    _pathBorder = RoundRect(new Rectangle(0, 0, w, h), BanKinhBoGoc);
                    _bw = w; _bh = h;
                }
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawPath(_penBorder, _pathBorder);
            }
            // Bỏ cột lề icon kiểu cũ -> phẳng, hiện đại
            protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) { }
            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (!e.Item.IsOnDropDown) { base.OnRenderMenuItemBackground(e); return; }
                if (!e.Item.Selected || !e.Item.Enabled) return;
                int w = e.Item.Width - 10, h = e.Item.Height - 4;
                if (_pathHover == null || w != _hw || h != _hh)
                {
                    _pathHover?.Dispose();
                    _pathHover = RoundRect(new Rectangle(5, 2, w, h), BanKinhHover);
                    _hw = w; _hh = h;
                }
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillPath(_brHover, _pathHover);
            }
            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                int y = e.Item.Height / 2;
                e.Graphics.DrawLine(_penSep, 14, y, e.Item.Width - 14, y);
            }
            // Text vẽ ra không phải chữ chính mà là chữ phím tắt (Ctrl+R...)?
            private static bool LaChuPhimTat(ToolStripItemTextRenderEventArgs e) =>
                e.Item is ToolStripMenuItem mi
                && mi.ShowShortcutKeys
                && (mi.ShortcutKeys != Keys.None || !string.IsNullOrEmpty(mi.ShortcutKeyDisplayString))
                && e.Text != mi.Text;
            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                Color mau = e.Item.Enabled ? _t.Text : _t.Disabled;
                if (e.Item.Enabled && LaChuPhimTat(e)) mau = _mauPhimTat;   // phím tắt mờ hơn, kiểu Edge
                e.TextColor = mau;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                // Căn giữa chữ (cả chữ chính lẫn phím tắt) theo CHIỀU CAO CẢ Ô,
                // giữ nguyên X/Width mà WinForms đã tính -> thẳng hàng với icon
                if (e.Item.IsOnDropDown)
                {
                    Rectangle r = e.TextRectangle;
                    e.TextRectangle = new Rectangle(r.X, 0, r.Width, e.Item.Height);
                    e.TextFormat = (e.TextFormat & ~TextFormatFlags.Bottom)
                                   | TextFormatFlags.VerticalCenter
                                   | TextFormatFlags.SingleLine;
                }
                base.OnRenderItemText(e);
            }
            // Icon cũng căn giữa theo chiều cao cả ô (cùng trục với chữ)
            protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
            {
                if (e.Item.IsOnDropDown && e.Image != null)
                {
                    Rectangle r = e.ImageRectangle;
                    r.Y = (e.Item.Height - r.Height) / 2;
                    base.OnRenderItemImage(new ToolStripItemImageRenderEventArgs(e.Graphics, e.Item, e.Image, r));
                    return;
                }
                base.OnRenderItemImage(e);
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
                    g.FillPath(_brAccent, path);
                g.DrawLines(_penTick, new[]
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
            /// <summary>Độ đặc lớp phủ màu trên nền kính mờ (0 = không dùng kính mờ, 255 = đặc hoàn toàn).</summary>
            public int DoMo;
            public MenuTheme(Color back, Color hover, Color border, Color text,
                             Color disabled, Color separator, Color accent, int doMo = 0)
            {
                Back = back; Hover = hover; Border = border; Text = text;
                Disabled = disabled; Separator = separator; Accent = accent;
                DoMo = Math.Clamp(doMo, 0, 255);
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