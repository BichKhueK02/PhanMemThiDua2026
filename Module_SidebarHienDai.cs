using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using Krypton.Toolkit;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Giao diện chính (Form2) kiểu dashboard web hiện đại:
    ///  - Sidebar nền tím than đậm, mục menu dạng viên bo tròn, hover sáng, mục đang chọn tím nổi chữ trắng.
    ///  - Thu gọn = thanh icon hẹp (chỉ còn biểu tượng + chú thích khi rê chuột) thay vì ẩn hẳn.
    ///  - Nút Thoát tách xuống đáy sidebar, có đường kẻ phân cách.
    ///  - Thanh trên trắng, kẻ mảnh phía dưới; tiêu đề trang bên trái; "chip" người dùng có avatar chữ viết tắt bên phải.
    /// Chỉ đổi kiểu hiển thị lúc chạy: KHÔNG đổi tên / thứ tự / sự kiện của control trong Designer.
    /// </summary>
    internal static class Module_SidebarHienDai
    {
        //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
        // ===== Bảng màu (đồng bộ tông tím của Trang chủ Form4) =====
        public static Color Nen = Color.FromArgb(27, 24, 54);              // nền sidebar
        public static Color ChuThuong = Color.FromArgb(190, 184, 228);     // chữ mục thường
        public static Color ChuMo = Color.FromArgb(132, 125, 178);         // chữ phụ (ô "Tự động thu gọn")
        public static Color DuongKe = Color.FromArgb(52, 47, 92);
        public static Color Hover = Color.FromArgb(46, 41, 88);
        public static Color Pressed = Color.FromArgb(58, 52, 108);
        public static Color Active = Color.FromArgb(124, 58, 237);         // viên đang chọn
        public static Color ActiveHover = Color.FromArgb(139, 92, 246);
        public static Color ActivePressed = Color.FromArgb(109, 40, 217);
        public static Color ChuActive = Color.White;
        public static Color ThoatHover = Color.FromArgb(225, 29, 72);
        public static Color ThoatPressed = Color.FromArgb(190, 18, 60);
        // Thanh trên + vùng nội dung
        public static Color NenNoiDung = Color.FromArgb(244, 246, 250);
        public static Color NenThanhTren = Color.White;
        public static Color VienThanhTren = Color.FromArgb(226, 232, 240);
        public static Color ChuTieuDe = Color.FromArgb(30, 41, 59);
        public static Color ChuTieuDeTrangChu = Color.FromArgb(109, 40, 217);
        public static Color ChuPhu = Color.FromArgb(71, 85, 105);
        public static Color NenChip = Color.FromArgb(248, 250, 252);
        public static Color NenNutGhost = Color.White;
        public static Color HoverNutGhost = Color.FromArgb(241, 245, 249);
        // ===== Kích thước (px ở 96 DPI, tự nhân theo DPI màn hình) =====
        public static int ChieuRongSidebar = 188;
        public static int ChieuRongThuGon = 68;
        public static int ChieuCaoNut = 46;         // đã gồm khoảng cách giữa các nút
        public static int KhoangCach = 3;           // mỗi bên (nút cách nhau 2 x giá trị này)
        public static int BanKinhBo = 10;
        public static int ChieuCaoThanhTren = 56;
        private static readonly Font FontThuong = new Font(Module_HeThong.TenFontHeThong, 10f, FontStyle.Regular);
        private static readonly Font FontDam = new Font(Module_HeThong.TenFontHeThongDam, 10f, FontStyle.Regular);
        private static readonly Font FontTieuDe = new Font(Module_HeThong.TenFontHeThongDam, 12.5f, FontStyle.Regular);
        private static readonly Font FontNguoiDung = new Font(Module_HeThong.TenFontHeThong, 9f, FontStyle.Regular);
        private static readonly Font FontNutGhost = new Font(Module_HeThong.TenFontHeThong, 9f, FontStyle.Regular);
        private static readonly Font FontPhu = new Font(Module_HeThong.TenFontHeThong, 8.5f, FontStyle.Regular);
        private sealed class TrangThaiSidebar
        {
            public readonly List<KryptonButton> Nut = new();
            public readonly Dictionary<KryptonButton, string> ChuGoc = new();
            public PictureBox? Logo;
            public CheckBox? TuDongAn;
            public KryptonButton? NutThoat;
            public ToolTip? GoiY;
            public bool ThuGon;
            public KryptonButton? DangChon;   // mục đang chọn (để tô lại đúng khi đổi bộ màu)
            public int Dpi;                   // DPI đã dùng để đặt kích thước (ApKichThuoc)
        }
        private static readonly ConditionalWeakTable<Panel, TrangThaiSidebar> _trangThai = new();
        private static string _vietTat = "";
        /// <summary>Đổi px ở 96 DPI sang px thật theo DPI của control.</summary>
        public static int S(Control c, int v) => (int)Math.Round(v * (c.DeviceDpi / 96f));
        public static int RongThuGon(Control c) => S(c, ChieuRongThuGon);
        // ======================================================================
        //  SIDEBAR
        // ======================================================================
        public static void KhoiTao(Panel panel, PictureBox? logo, CheckBox? tuDongAn, params KryptonButton[] cacNut)
        {
            var tt = _trangThai.GetValue(panel, _ => new TrangThaiSidebar());
            tt.Logo = logo;
            tt.TuDongAn = tuDongAn;
            panel.BackColor = Nen;
            panel.Width = S(panel, ChieuRongSidebar);
            panel.Padding = new Padding(S(panel, 9), S(panel, 12), S(panel, 9), S(panel, 10));
            tt.GoiY = new ToolTip { InitialDelay = 250, ReshowDelay = 100, AutoPopDelay = 3000 };
            if (logo != null)
            {
                // Khung ảnh trên cùng như "logo/brand" của trang web: có khoảng thở phía dưới
                logo.SizeMode = PictureBoxSizeMode.Zoom;
                logo.BackColor = Nen;
                logo.Cursor = Cursors.Hand;
            }
            if (tuDongAn != null)
            {
                tuDongAn.Tag = Module_GiaoDienBoTron.TheKhongBoGoc;
                tuDongAn.AutoSize = false;
                tuDongAn.Dock = DockStyle.Bottom;
                tuDongAn.Text = "Tự động thu gọn";
                tuDongAn.Font = FontPhu;
                tuDongAn.ForeColor = ChuMo;
                tuDongAn.BackColor = Nen;
                tuDongAn.Cursor = Cursors.Hand;
            }
            foreach (var b in cacNut)
            {
                if (b == null) continue;
                tt.Nut.Add(b);
                tt.ChuGoc[b] = b.Values.Text;
                tt.GoiY.SetToolTip(b, b.Values.Text);
                // Không cho module bo tròn chung ghi đè kiểu của nút menu
                b.Tag = Module_GiaoDienBoTron.TheKhongBoGoc;
                b.Cursor = Cursors.Hand;
                b.StateCommon.Back.ColorStyle = PaletteColorStyle.Solid;
                // Viền dày cùng màu nền sidebar => tạo khoảng cách quanh viên thuốc, như "margin" trên web
                b.StateCommon.Border.DrawBorders = PaletteDrawBorders.All;
                b.StateCommon.Border.ColorStyle = PaletteColorStyle.Solid;
                b.StateCommon.Border.Color1 = Nen;
                b.StateCommon.Border.Color2 = Nen;
                b.StateTracking.Border.Color1 = Nen;
                b.StateTracking.Border.Color2 = Nen;
                b.StatePressed.Border.Color1 = Nen;
                b.StatePressed.Border.Color2 = Nen;
                b.OverrideFocus.Border.Color1 = Nen;
                b.OverrideFocus.Border.Color2 = Nen;
                b.OverrideDefault.Border.Color1 = Nen;   // bỏ viền xanh xám "nút mặc định" của Krypton quanh mục đang chọn
                b.OverrideDefault.Border.Color2 = Nen;
                b.StateNormal.Border.Color1 = Nen;
                b.StateNormal.Border.Color2 = Nen;
                if (b.Name.Contains("Thoat", StringComparison.OrdinalIgnoreCase))
                {
                    // Thoát nằm riêng ở đáy sidebar (trên ô "Tự động thu gọn"), như nút Đăng xuất của trang web
                    tt.NutThoat = b;
                    b.Dock = DockStyle.Bottom;
                }
                DatTrangThai(b, false);
            }
            // Kích thước theo DPI (bề cao nút, bo góc, viền, biểu tượng, lề, logo...) - gọi lại khi đổi DPI (CapNhatTheoDpi)
            ApKichThuoc(panel, tt, panel.DeviceDpi);
            // Đường kẻ mảnh phân cách nhóm nút đáy
            panel.Paint += (s, e) =>
            {
                if (tt.NutThoat == null || !tt.NutThoat.Visible) return;
                int y = tt.NutThoat.Top - S(panel, 6);
                int le = S(panel, 14);
                using var but = new Pen(DuongKe, Math.Max(1, S(panel, 1)));
                e.Graphics.DrawLine(but, le, y, panel.Width - le, y);
                // Bộ màu nền sáng: kẻ mảnh mép phải để tách thanh menu khỏi vùng nội dung
                if (NenSang)
                {
                    using var ke = new Pen(Color.FromArgb(226, 232, 240));
                    e.Graphics.DrawLine(ke, panel.Width - 1, 0, panel.Width - 1, panel.Height);
                }
            };
            panel.Resize += (s, e) => panel.Invalidate();
        }
        /// <summary>
        /// Đặt mọi kích thước phụ thuộc DPI của sidebar theo DPI HIỆN TẠI của panel. WinForms tự co giãn Size/Padding của control
        /// khi đổi DPI, nhưng KHÔNG co giãn giá trị trong bảng màu Krypton (bo góc, viền, lề chữ) và ảnh biểu tượng đã vẽ sẵn
        /// => trước đây đổi 125% -> 100% (hoặc mở phần mềm khi tỷ lệ vừa đổi mà chưa đăng xuất) các viên menu lệch cỡ.
        /// </summary>
        private static void ApKichThuoc(Panel panel, TrangThaiSidebar tt, int dpi)
        {
            tt.Dpi = dpi;
            int P(int v) => SDpi(dpi, v);
            panel.Padding = tt.ThuGon
                ? new Padding(P(6), P(12), P(6), P(10))
                : new Padding(P(9), P(12), P(9), P(10));
            if (tt.Logo != null)
            {
                tt.Logo.Height = P(98);
                tt.Logo.Padding = new Padding(P(4), 0, P(4), P(14));
            }
            if (tt.TuDongAn != null)
            {
                tt.TuDongAn.Height = P(30);
                tt.TuDongAn.Padding = new Padding(P(12), 0, 0, 0);
            }
            foreach (var b in tt.Nut)
            {
                if (b.IsDisposed) continue;
                b.Height = P(ChieuCaoNut);
                b.StateCommon.Border.Rounding = P(BanKinhBo);
                b.StateCommon.Border.Width = P(KhoangCach);
                DatCanLe(b, tt.ThuGon, dpi);
                // Biểu tượng VẼ (không dùng ảnh): nút Thoát = nút nguồn hồng đỏ, các nút khác cùng màu chữ; rê chuột/chọn => trắng
                Module_BieuTuongMenu.GanBieuTuongVe(b, P(22),Module_BieuTuongMenu.MauRieng(Module_BieuTuongMenu.LoaiCuaNut(b)), Color.White);
                if (!ReferenceEquals(tt.NutThoat, b))
                {
                    // Rê chuột giữ nguyên màu riêng của biểu tượng; chỉ mục ĐANG CHỌN (nền tím) mới chuyển trắng.
                    // Nút Thoát: hover nền đỏ => biểu tượng trắng như cũ.
                    b.Values.ImageStates.ImageTracking = null;
                    b.Values.ImageStates.ImagePressed = null;
                    b.Values.ImageStates.ImageCheckedTracking = null;
                    b.Values.ImageStates.ImageCheckedPressed = null;
                }
                Module_BieuTuongMenu.DatDangChon(b, ReferenceEquals(b, tt.DangChon));
            }
        }
        /// <summary>Form2 gọi sau khi DPI đổi (kéo sang màn hình khác tỷ lệ / đổi tỷ lệ Windows khi đang chạy).</summary>
        /// <returns>true nếu đã đặt lại (DPI khác lần trước).</returns>
        /// <param name="dpi">DPI mới của Form2 (DeviceDpi của panel con có thể chưa kịp cập nhật).</param>
        public static bool CapNhatTheoDpi(Panel panel, int dpi)
        {
            if (dpi <= 0 || !_trangThai.TryGetValue(panel, out var tt) || tt.Dpi == dpi) return false;
            panel.SuspendLayout();
            try { ApKichThuoc(panel, tt, dpi); }
            finally { panel.ResumeLayout(true); }
            panel.Invalidate(true);
            return true;
        }
        /// <summary>Bề rộng đầy đủ / thu gọn của sidebar theo DPI cho trước.</summary>
        public static int RongDayDu(int dpi) => SDpi(dpi, ChieuRongSidebar);
        public static int RongThuGon(int dpi) => SDpi(dpi, ChieuRongThuGon);
        private static int SDpi(int dpi, int v) => (int)Math.Round(v * (dpi / 96f));
        // ======================================================================
        //  BỘ MÀU THANH MENU (Form12 > Tab 2 > mục 9 "Chọn màu sắc menu hệ thống", kiểu Hiện đại)
        //  "Mặc định" = màu tím than hiện tại. Đổi bộ màu: ApDungChuDe(panel, tên) => tô lại NGAY, không cần khởi động lại.
        // ======================================================================
        private sealed record ChuDeMenu(Color Nen, Color Hover, Color Pressed, Color Active, Color ActiveHover, Color ActivePressed,
                                        Color Chu, Color ChuHover, Color ChuMo, Color Ke);
        private static readonly Dictionary<string, ChuDeMenu> BangChuDe = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Mặc định"] = new(C(27, 24, 54), C(46, 41, 88), C(58, 52, 108), C(124, 58, 237), C(139, 92, 246), C(109, 40, 217), C(190, 184, 228), Color.White, C(132, 125, 178), C(52, 47, 92)),
            ["Xanh dương"] = new(C(15, 30, 60), C(30, 52, 96), C(37, 64, 115), C(37, 99, 235), C(59, 130, 246), C(29, 78, 216), C(191, 219, 254), Color.White, C(125, 160, 210), C(37, 56, 92)),
            ["Xanh lá"] = new(C(10, 45, 32), C(20, 70, 50), C(25, 85, 60), C(22, 163, 74), C(34, 197, 94), C(21, 128, 61), C(187, 247, 208), Color.White, C(110, 180, 140), C(25, 70, 50)),
            ["Xanh ngọc"] = new(C(4, 47, 46), C(17, 78, 75), C(19, 94, 90), C(13, 148, 136), C(20, 184, 166), C(15, 118, 110), C(204, 251, 241), Color.White, C(120, 190, 180), C(19, 78, 74)),
            ["Hồng cánh sen"] = new(C(76, 5, 25), C(110, 20, 50), C(130, 24, 60), C(219, 39, 119), C(236, 72, 153), C(190, 24, 93), C(252, 205, 227), Color.White, C(220, 140, 175), C(110, 25, 55)),
            ["Cam hoàng hôn"] = new(C(67, 20, 7), C(100, 40, 15), C(124, 45, 18), C(234, 88, 12), C(249, 115, 22), C(194, 65, 12), C(254, 215, 170), Color.White, C(200, 140, 100), C(100, 40, 18)),
            ["Đỏ đô"] = new(C(69, 10, 10), C(100, 22, 22), C(127, 29, 29), C(220, 38, 38), C(239, 68, 68), C(185, 28, 28), C(254, 202, 202), Color.White, C(200, 130, 130), C(100, 25, 25)),
            ["Đen than"] = new(C(24, 24, 27), C(45, 45, 50), C(63, 63, 70), C(14, 165, 233), C(56, 189, 248), C(2, 132, 199), C(212, 212, 216), Color.White, C(140, 140, 150), C(55, 55, 62)),
            // Nền sáng: chữ tối, rê chuột giữ chữ tối
            ["Tím nhạt"] = new(C(237, 233, 254), C(221, 214, 254), C(196, 181, 253), C(124, 58, 237), C(139, 92, 246), C(109, 40, 217), C(76, 29, 149), C(59, 7, 100), C(124, 100, 200), C(221, 214, 254)),
            ["Xám trắng"] = new(C(241, 245, 249), C(226, 232, 240), C(203, 213, 225), C(51, 65, 85), C(71, 85, 105), C(30, 41, 59), C(51, 65, 85), C(15, 23, 42), C(100, 116, 139), C(226, 232, 240)),
            ["Trắng tinh"] = new(Color.White, C(241, 245, 249), C(226, 232, 240), C(37, 99, 235), C(59, 130, 246), C(29, 78, 216), C(51, 65, 85), C(15, 23, 42), C(100, 116, 139), C(226, 232, 240)),
        };
        private static Color C(int r, int g, int b) => Color.FromArgb(r, g, b);
        /// <summary>Tên các bộ màu của kiểu Hiện đại (Form12 nạp vào ô chọn mục 9). Phần tử đầu = mặc định.</summary>
        public static IReadOnlyList<string> DanhSachMau => BangChuDe.Keys.ToList();
        public static Color ChuHover = Color.White;
        private static bool NenSang => (Nen.R * 299 + Nen.G * 587 + Nen.B * 114) / 1000 > 200;
        /// <summary>Đặt bộ màu (chỉ đổi biến màu, chưa tô lại). Tên lạ/trống => "Mặc định". Gọi TRƯỚC KhoiTao lúc dựng Form2.</summary>
        public static void DatChuDe(string? ten)
        {
            if (string.IsNullOrWhiteSpace(ten) || !BangChuDe.TryGetValue(ten.Trim(), out var cd)) cd = BangChuDe["Mặc định"];
            Nen = cd.Nen; Hover = cd.Hover; Pressed = cd.Pressed;
            Active = cd.Active; ActiveHover = cd.ActiveHover; ActivePressed = cd.ActivePressed;
            ChuThuong = cd.Chu; ChuHover = cd.ChuHover; ChuMo = cd.ChuMo; DuongKe = cd.Ke;
        }
        /// <summary>Đổi bộ màu và TÔ LẠI NGAY thanh menu đang hiển thị (nền, logo, ô "Tự động thu gọn", từng nút, mục đang chọn).</summary>
        public static void ApDungChuDe(Panel panel, string? ten)
        {
            DatChuDe(ten);
            if (!_trangThai.TryGetValue(panel, out var tt)) return;
            panel.SuspendLayout();
            try
            {
                panel.BackColor = Nen;
                if (tt.Logo != null) tt.Logo.BackColor = Nen;
                if (tt.TuDongAn != null) { tt.TuDongAn.BackColor = Nen; tt.TuDongAn.ForeColor = ChuMo; }
                foreach (var b in tt.Nut)
                {
                    if (b.IsDisposed) continue;
                    foreach (var vien in new[] { b.StateCommon.Border, b.StateTracking.Border, b.StatePressed.Border })
                    { vien.Color1 = Nen; vien.Color2 = Nen; }
                    b.OverrideFocus.Border.Color1 = Nen; b.OverrideFocus.Border.Color2 = Nen;
                    b.OverrideDefault.Border.Color1 = Nen; b.OverrideDefault.Border.Color2 = Nen;
                    b.StateNormal.Border.Color1 = Nen; b.StateNormal.Border.Color2 = Nen;
                    DatTrangThai(b, ReferenceEquals(b, tt.DangChon));
                }
            }
            finally { panel.ResumeLayout(true); }
            panel.Invalidate(true);
        }
        // Màu biểu tượng nút nguồn (⏻) của nút Thoát (vẽ trong Module_BieuTuongMenu)
        public static Color MauNutNguon = Color.FromArgb(251, 113, 133);   // hồng đỏ nhạt trên nền tối
        private static void DatCanLe(KryptonButton b, bool thuGon, int dpi)
        {
            b.StateCommon.Content.Image!.ImageH = thuGon ? PaletteRelativeAlign.Center : PaletteRelativeAlign.Near;
            b.StateCommon.Content.ShortText.TextH = thuGon ? PaletteRelativeAlign.Center : PaletteRelativeAlign.Near;
            b.StateCommon.Content.Padding = thuGon
                ? new Padding(0, -1, 0, -1)
                : new Padding(SDpi(dpi, 12), -1, SDpi(dpi, 6), -1);
        }
        /// <summary>Chuyển sidebar giữa dạng đầy đủ và dạng thanh icon. Gọi TRƯỚC khi đổi bề rộng panel.</summary>
        public static void DatThuGon(Panel panel, bool thuGon)
        {
            if (!_trangThai.TryGetValue(panel, out var tt) || tt.ThuGon == thuGon) return;
            tt.ThuGon = thuGon;
            int dpi = tt.Dpi > 0 ? tt.Dpi : panel.DeviceDpi;
            panel.Padding = thuGon
                ? new Padding(SDpi(dpi, 6), SDpi(dpi, 12), SDpi(dpi, 6), SDpi(dpi, 10))
                : new Padding(SDpi(dpi, 9), SDpi(dpi, 12), SDpi(dpi, 9), SDpi(dpi, 10));
            if (tt.Logo != null) tt.Logo.Visible = !thuGon;
            if (tt.TuDongAn != null) tt.TuDongAn.Visible = !thuGon;
            foreach (var b in tt.Nut)
            {
                if (b.IsDisposed) continue;
                if (!thuGon && tt.ChuGoc.TryGetValue(b, out var chu)) b.Values.Text = chu;
                else if (thuGon)
                {
                    if (!string.IsNullOrEmpty(b.Values.Text)) tt.ChuGoc[b] = b.Values.Text;
                    b.Values.Text = "";
                }
                DatCanLe(b, thuGon, dpi);
            }
            panel.Invalidate();
        }
        public static void DatTrangThai(KryptonButton b, bool dangChon)
        {
            if (b == null || b.IsDisposed) return;
            bool laThoat = b.Name.Contains("Thoat", StringComparison.OrdinalIgnoreCase);
            Color nen = dangChon ? Active : Nen;
            Color hover = dangChon ? ActiveHover : (laThoat ? ThoatHover : Hover);
            Color nhan = dangChon ? ActivePressed : (laThoat ? ThoatPressed : Pressed);
            Color chu = dangChon ? ChuActive : ChuThuong;
            // Rê chuột: mục đang chọn / nút Thoát (nền đỏ) => chữ trắng; mục thường => màu chữ-khi-rê của bộ màu (nền sáng thì chữ tối)
            Color chuHover = (dangChon || laThoat) ? Color.White : ChuHover;
            if (dangChon && b.Parent is Panel cha && _trangThai.TryGetValue(cha, out var ttCha)) ttCha.DangChon = b;
            b.StateCommon.Back.Color1 = nen;
            b.StateCommon.Back.Color2 = nen;
            b.OverrideDefault.Back.Color1 = nen;
            b.OverrideDefault.Back.Color2 = nen;
            b.OverrideFocus.Back.Color1 = nen;
            b.OverrideFocus.Back.Color2 = nen;
            b.StateTracking.Back.Color1 = hover;
            b.StateTracking.Back.Color2 = hover;
            b.StatePressed.Back.Color1 = nhan;
            b.StatePressed.Back.Color2 = nhan;
            b.StateCommon.Content.ShortText.Color1 = chu;
            b.StateCommon.Content.ShortText.Color2 = chu;
            b.OverrideDefault.Content.ShortText.Color1 = chu;
            b.OverrideDefault.Content.ShortText.Color2 = chu;
            b.OverrideFocus.Content.ShortText.Color1 = chu;
            b.OverrideFocus.Content.ShortText.Color2 = chu;
            b.StateTracking.Content.ShortText.Color1 = chuHover;
            b.StateTracking.Content.ShortText.Color2 = chuHover;
            b.StatePressed.Content.ShortText.Color1 = chuHover;
            b.StatePressed.Content.ShortText.Color2 = chuHover;
            b.StateCommon.Content.ShortText.Font = dangChon ? FontDam : FontThuong;
            Module_BieuTuongMenu.DatDangChon(b, dangChon);   // mục đang chọn: biểu tượng trắng như chữ
            b.Invalidate();
        }
        // ======================================================================
        //  THANH TRÊN
        // ======================================================================
        public static void KhoiTaoThanhTren(Panel panelTop, TableLayoutPanel bang, TableLayoutPanel bangNut,
            KryptonButton nutMenu, PictureBox muiTen, Label tieuDe, Label nguoiDung, PictureBox chungThu, Panel vungNoiDung)
        {
            // Nền panel = màu viền, chừa 1px dưới => đường kẻ mảnh dưới thanh trên (không cần tự vẽ)
            panelTop.Height = S(panelTop, ChieuCaoThanhTren);
            panelTop.BackColor = VienThanhTren;
            panelTop.Padding = new Padding(0, 0, 0, Math.Max(1, S(panelTop, 1)));
            vungNoiDung.BackColor = NenNoiDung;
            bang.SuspendLayout();
            try
            {
                bang.BackColor = NenThanhTren;
                bang.Padding = new Padding(S(bang, 12), 0, S(bang, 18), 0);
                bang.ColumnStyles.Clear();
                bang.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // nút thu gọn menu
                bang.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));     // tiêu đề trang
                bang.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // chứng thư số
                bang.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // chip người dùng
                // Cụm nút thu gọn: bỏ ảnh mũi tên riêng, chỉ còn 1 nút "ghost" gọn gàng
                bangNut.BackColor = NenThanhTren;
                bangNut.AutoSize = true;
                bangNut.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                bangNut.Dock = DockStyle.None;
                bangNut.Anchor = AnchorStyles.Left;
                bangNut.Margin = new Padding(0, 0, S(bang, 14), 0);
                bangNut.ColumnStyles.Clear();
                bangNut.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0));
                bangNut.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                muiTen.Visible = false;
                nutMenu.Tag = Module_GiaoDienBoTron.TheKhongBoGoc;
                nutMenu.Anchor = AnchorStyles.Left;
                nutMenu.Size = new Size(S(nutMenu, 128), S(nutMenu, 34));
                nutMenu.Margin = new Padding(0);
                nutMenu.Cursor = Cursors.Hand;
                nutMenu.StateCommon.Back.ColorStyle = PaletteColorStyle.Solid;
                nutMenu.StateCommon.Back.Color1 = NenNutGhost;
                nutMenu.StateCommon.Back.Color2 = NenNutGhost;
                nutMenu.StateTracking.Back.Color1 = HoverNutGhost;
                nutMenu.StateTracking.Back.Color2 = HoverNutGhost;
                nutMenu.StatePressed.Back.Color1 = VienThanhTren;
                nutMenu.StatePressed.Back.Color2 = VienThanhTren;
                nutMenu.StateCommon.Border.DrawBorders = PaletteDrawBorders.All;
                nutMenu.StateCommon.Border.ColorStyle = PaletteColorStyle.Solid;
                nutMenu.StateCommon.Border.Color1 = VienThanhTren;
                nutMenu.StateCommon.Border.Color2 = VienThanhTren;
                nutMenu.StateCommon.Border.Width = 1;
                nutMenu.StateCommon.Border.Rounding = S(nutMenu, 8);
                nutMenu.StateCommon.Content.ShortText.Font = FontNutGhost;
                nutMenu.StateCommon.Content.ShortText.Color1 = ChuPhu;
                nutMenu.StateCommon.Content.ShortText.Color2 = ChuPhu;
                nutMenu.StateCommon.Content.ShortText.TextH = PaletteRelativeAlign.Center;
                // Tiêu đề trang: căn trái, đậm vừa, như tiêu đề trang web
                tieuDe.Anchor = AnchorStyles.Left;
                tieuDe.Font = FontTieuDe;
                tieuDe.ForeColor = ChuTieuDe;
                tieuDe.BackColor = NenThanhTren;
                tieuDe.Margin = new Padding(0);
                // Chứng thư số (ảnh động nhỏ)
                chungThu.Anchor = AnchorStyles.None;
                chungThu.Size = new Size(S(chungThu, 34), S(chungThu, 34));
                Module_BieuTuongMenu.GanBieuTuongChungThu(chungThu);   // vẽ lại đúng cỡ mới
                chungThu.BackColor = NenThanhTren;
                chungThu.Margin = new Padding(S(bang, 8), 0, S(bang, 12), 0);
                chungThu.Cursor = Cursors.Hand;
                // Chip người dùng: nền + avatar do bảng cha vẽ phía sau (nhãn trong suốt)
                nguoiDung.Anchor = AnchorStyles.Right;
                nguoiDung.AutoSize = true;
                nguoiDung.Font = FontNguoiDung;
                nguoiDung.ForeColor = ChuPhu;
                nguoiDung.BackColor = Color.Transparent;
                nguoiDung.Padding = new Padding(S(bang, 40), S(bang, 7), S(bang, 14), S(bang, 7));
                nguoiDung.Margin = new Padding(0);
                nguoiDung.Cursor = Cursors.Hand;
                nguoiDung.SizeChanged += (s, e) => bang.Invalidate();
                nguoiDung.LocationChanged += (s, e) => bang.Invalidate();
                nguoiDung.TextChanged += (s, e) => bang.Invalidate();
                bang.Paint += (s, e) => VeChipNguoiDung(e.Graphics, nguoiDung);
                // Thứ tự cột mới: 0 nút | 1 tiêu đề | 2 chứng thư | 3 người dùng
                bang.SetColumn(bangNut, 0);
                bang.SetColumn(tieuDe, 1);
                bang.SetColumn(chungThu, 2);
                bang.SetColumn(nguoiDung, 3);
            }
            finally { bang.ResumeLayout(true); }
        }
        /// <summary>Màu tiêu đề: Trang chủ tím nhấn, các trang khác chữ đậm trung tính.</summary>
        public static Color MauTieuDe(bool laTrangChu) => laTrangChu ? ChuTieuDeTrangChu : ChuTieuDe;
        /// <summary>Cập nhật chữ viết tắt trên avatar theo tên tài khoản (vd. "Nguyễn Trung Kiên" -> "TK").</summary>
        public static void DatNguoiDung(Label nguoiDung, string? ten)
        {
            _vietTat = TaoVietTat(ten);
            nguoiDung.Parent?.Invalidate();
        }
        internal static string TaoVietTat(string? ten)
        {
            if (string.IsNullOrWhiteSpace(ten)) return "";
            var tu = ten.Split(new[] { ' ', '_', '.', '-' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(t => char.IsLetterOrDigit(t[0])).ToArray();
            if (tu.Length == 0) return "";
            string kq = tu.Length >= 2
                ? $"{tu[^2][0]}{tu[^1][0]}"
                : tu[0][0].ToString();
            return kq.ToUpperInvariant();
        }
        private static void VeChipNguoiDung(Graphics g, Label nhan)
        {
            if (!nhan.Visible || nhan.Width <= 0 || string.IsNullOrWhiteSpace(nhan.Text) || nhan.Text == "*") return;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(nhan.Left, nhan.Top, nhan.Width - 1, nhan.Height - 1);
            int bk = r.Height / 2;
            using (var duong = TaoHinhBo(r, bk))
            using (var nen = new SolidBrush(NenChip))
            using (var vien = new Pen(VienThanhTren))
            {
                g.FillPath(nen, duong);
                g.DrawPath(vien, duong);
            }
            int d = r.Height - S(nhan, 8);
            var tron = new Rectangle(r.Left + S(nhan, 4), r.Top + (r.Height - d) / 2, d, d);
            using (var to = new LinearGradientBrush(tron, Color.FromArgb(139, 92, 246), Color.FromArgb(91, 33, 182), 45f))
                g.FillEllipse(to, tron);
            string chu = string.IsNullOrEmpty(_vietTat) ? "?" : _vietTat;
            float co = _vietTat.Length > 1 ? 7.5f : 8.5f;
            using var font = new Font(Module_HeThong.TenFontHeThongDam, co, FontStyle.Regular);
            TextRenderer.DrawText(g, chu, font, tron, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
        private static GraphicsPath TaoHinhBo(Rectangle r, int bk)
        {
            var p = new GraphicsPath();
            int d = Math.Max(1, Math.Min(bk * 2, Math.Min(r.Width, r.Height)));
            p.AddArc(r.Left, r.Top, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
