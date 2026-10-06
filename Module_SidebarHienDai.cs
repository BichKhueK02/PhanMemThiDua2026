using Krypton.Toolkit;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PhanMemThiDua2026
{
    /// <summary>
    /// Sidebar kiểu web hiện đại: nền chàm đậm phẳng, mục menu dạng "viên thuốc" bo tròn có khoảng cách,
    /// hover sáng lên, mục đang chọn là viên trắng chữ chàm đậm.
    /// Dùng: Module_SidebarHienDai.KhoiTao(PanelLeft, nut1, nut2, ...);
    ///       Module_SidebarHienDai.DatTrangThai(nut, true/false) để bật/tắt mục đang chọn.
    /// </summary>
    internal static class Module_SidebarHienDai
    {
        // ===== Bảng màu: sidebar tím lavender nhạt, mục đang chọn là viên tím đậm chữ trắng =====
        public static Color Nen = Color.FromArgb(150, 120, 235);          // nền sidebar
       // public static Color Nen = Color.FromArgb(232, 226, 253);          // nền sidebar
        public static Color ChuThuong = Color.FromArgb(60, 40, 125);      // chữ mục thường
        public static Color Hover = Color.FromArgb(214, 205, 250);
        public static Color Pressed = Color.FromArgb(197, 185, 245);
        public static Color Active = Color.FromArgb(124, 92, 230);        // viên đang chọn
        public static Color ActiveHover = Color.FromArgb(139, 109, 238);
        public static Color ActivePressed = Color.FromArgb(104, 74, 205);
        public static Color ChuActive = Color.White;
        public static Color ThoatHover = Color.FromArgb(240, 98, 118);
        public static Color ThoatPressed = Color.FromArgb(215, 70, 92);

        public static int ChieuRongSidebar = 172;   // px ở 96 DPI
        public static int ChieuCaoNut = 60;         // đã gồm khoảng cách giữa các nút
        public static int KhoangCach = 5;           // mỗi bên (nút cách nhau 2 x giá trị này)
        public static float BanKinhBo = 16f;

        private static readonly Font FontThuong = new Font(Module_HeThong.TenFontHeThong, 11f, FontStyle.Regular);
        private static readonly Font FontDam = new Font("Segoe UI Semibold", 11f, FontStyle.Regular);

        public static void KhoiTao(Panel panel, params KryptonButton[] cacNut)
        {
            panel.BackColor = Nen;
            panel.Width = ChieuRongSidebar;

            foreach (var b in cacNut)
            {
                if (b == null) continue;

                // Không cho module bo tròn chung ghi đè kiểu của nút menu
                b.Tag = Module_GiaoDienBoTron.TheKhongBoGoc;
                b.Height = ChieuCaoNut;

                b.StateCommon.Back.ColorStyle = PaletteColorStyle.Solid;

                // Viền dày cùng màu nền sidebar => tạo khoảng cách quanh viên thuốc, như "margin" trên web
                b.StateCommon.Border.Rounding = BanKinhBo;
                b.StateCommon.Border.Width = KhoangCach;
                b.StateCommon.Border.Color1 = Nen;
                b.StateCommon.Border.Color2 = Nen;

                b.StateCommon.Content.Padding = new Padding(4, -1, 4, -1);
                b.StateCommon.Content.Image.ImageH = PaletteRelativeAlign.Near;
                b.StateCommon.Content.ShortText.TextH = PaletteRelativeAlign.Near;

                DatTrangThai(b, false);
            }
        }

        public static void DatTrangThai(KryptonButton b, bool dangChon)
        {
            if (b == null || b.IsDisposed) return;

            bool laThoat = b.Name.Contains("Thoat", StringComparison.OrdinalIgnoreCase);

            Color nen = dangChon ? Active : Nen;
            Color hover = dangChon ? ActiveHover : (laThoat ? ThoatHover : Hover);
            Color nhan = dangChon ? ActivePressed : (laThoat ? ThoatPressed : Pressed);
            Color chu = dangChon ? ChuActive : ChuThuong;
            Color chuHover = (dangChon || laThoat) ? Color.White : ChuThuong;

            b.StateCommon.Back.Color1 = nen;
            b.StateCommon.Back.Color2 = nen;
            b.OverrideDefault.Back.Color1 = nen;
            b.OverrideDefault.Back.Color2 = nen;
            b.StateTracking.Back.Color1 = hover;
            b.StateTracking.Back.Color2 = hover;
            b.StatePressed.Back.Color1 = nhan;
            b.StatePressed.Back.Color2 = nhan;

            b.StateCommon.Content.ShortText.Color1 = chu;
            b.StateCommon.Content.ShortText.Color2 = chu;
            b.OverrideDefault.Content.ShortText.Color1 = chu;
            b.OverrideDefault.Content.ShortText.Color2 = chu;
            b.StateTracking.Content.ShortText.Color1 = chuHover;
            b.StateTracking.Content.ShortText.Color2 = chuHover;
            b.StatePressed.Content.ShortText.Color1 = chuHover;
            b.StatePressed.Content.ShortText.Color2 = chuHover;

            b.StateCommon.Content.ShortText.Font = dangChon ? FontDam : FontThuong;

            b.Invalidate();
        }
    }
}
