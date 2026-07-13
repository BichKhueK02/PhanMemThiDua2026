using System;
using System.Drawing;
using System.Windows.Forms;

namespace PhanMemThiDua2026
{
    internal static class Module_MenuChuotPhai
    {
        /// <summary>
        /// Hàm dùng chung để kích hoạt dải lề xanh lá cây Classic cho bất kỳ ContextMenuStrip nào trong hệ thống, 
        /// giúp chặn hoàn toàn cơ chế tự động ghi đè giao diện của Krypton Toolkit.
        /// </summary>
        /// <param name="menu">Đối tượng ContextMenuStrip cần cấu hình</param>
        public static void TichHopGiaoDienXanhLa(ContextMenuStrip menu)
        {
            if (menu == null) return;

            // Phục kích ngay tại sự kiện Opening của chính Menu đó
            menu.Opening += (s, ev) =>
            {
                // 🌟 THẦN CHÚ KHÓA CHỐT: Ép RenderMode về Professional.
                // Lệnh này sẽ bẻ gãy hoàn toàn sự can thiệp giao diện toàn cục của Krypton 
                // hoặc các thuộc tính 'System' đang bị khóa trong file Designer.cs của Form6.
                menu.RenderMode = ToolStripRenderMode.Professional;

                // Sau đó mới gán bộ vẽ kết hợp bảng màu xanh lá cây vào
                menu.Renderer = new ToolStripProfessionalRenderer(new GreenMenuColorTable());

                // Khóa cứng bắt buộc hiển thị khoảng trống chứa Icon bên trái
                menu.ShowImageMargin = true;
            };
        }
    }

    // ==============================================================================
    // BẢNG MÀU DÙNG CHUNG TOÀN HỆ THỐNG (Giữ nguyên 100%)
    // ==============================================================================
    public class GreenMenuColorTable : ProfessionalColorTable
    {
        private readonly Color _greenMargin = Color.FromArgb(215, 240, 215);

        // 1. Dải lề bên trái (Nền xanh lá nhẹ)
        public override Color ImageMarginGradientBegin => _greenMargin;
        public override Color ImageMarginGradientMiddle => _greenMargin;
        public override Color ImageMarginGradientEnd => _greenMargin;

        public override Color ImageMarginRevealedGradientBegin => _greenMargin;
        public override Color ImageMarginRevealedGradientMiddle => _greenMargin;
        public override Color ImageMarginRevealedGradientEnd => _greenMargin;

        // 2. Màu nền vùng chứa chữ bên phải (Trắng tinh tế)
        public override Color ToolStripDropDownBackground => Color.White;

        // 3. Đường viền bao quanh toàn bộ Menu
        public override Color MenuBorder => Color.FromArgb(120, 170, 120);

        // 4. Hiệu ứng khi lướt chuột qua (Hover)
        public override Color MenuItemSelected => Color.FromArgb(230, 245, 230);
        public override Color MenuItemBorder => Color.FromArgb(150, 200, 150);

        // 5. Đường phân tách các nhóm chức năng (Separator)
        public override Color SeparatorDark => Color.FromArgb(190, 220, 190);
        public override Color SeparatorLight => Color.White;
    }
}    

//using System;
 //using System.Drawing;
 //using System.Windows.Forms;

//namespace PhanMemThiDua2026
//{
//    internal static class Module_MenuChuotPhai
//    {
//        /// <summary>
//        /// Hàm dùng chung để kích hoạt dải lề xanh lá cây Classic cho bất kỳ ContextMenuStrip nào trong hệ thống, 
//        /// giúp chặn hoàn toàn cơ chế tự động ghi đè giao diện của Krypton Toolkit.
//        /// </summary>
//        /// <param name="menu">Đối tượng ContextMenuStrip cần cấu hình</param>
//        public static void TichHopGiaoDienXanhLa(ContextMenuStrip menu)
//        {
//            if (menu == null) return;

//            // Phục kích ngay tại sự kiện Opening của chính Menu đó
//            menu.Opening += (s, ev) =>
//            {
//                // Ép menu sử dụng bộ vẽ Professional kết hợp bảng màu xanh lá cây
//                menu.Renderer = new ToolStripProfessionalRenderer(new GreenMenuColorTable());

//                // Khóa cứng bắt buộc hiển thị khoảng trống chứa Icon bên trái
//                menu.ShowImageMargin = true;
//            };
//        }
//    }

//    // ==============================================================================
//    // BẢNG MÀU DÙNG CHUNG TOÀN HỆ THỐNG (Đặt chung trong file để quản lý tập trung)
//    // ==============================================================================
//    public class GreenMenuColorTable : ProfessionalColorTable
//    {
//        private readonly Color _greenMargin = Color.FromArgb(215, 240, 215);

//        // 1. Dải lề bên trái (Nền xanh lá nhẹ)
//        public override Color ImageMarginGradientBegin => _greenMargin;
//        public override Color ImageMarginGradientMiddle => _greenMargin;
//        public override Color ImageMarginGradientEnd => _greenMargin;

//        public override Color ImageMarginRevealedGradientBegin => _greenMargin;
//        public override Color ImageMarginRevealedGradientMiddle => _greenMargin;
//        public override Color ImageMarginRevealedGradientEnd => _greenMargin;

//        // 2. Màu nền vùng chứa chữ bên phải (Trắng tinh tế)
//        public override Color ToolStripDropDownBackground => Color.White;

//        // 3. Đường viền bao quanh toàn bộ Menu
//        public override Color MenuBorder => Color.FromArgb(120, 170, 120);

//        // 4. Hiệu ứng khi lướt chuột qua (Hover)
//        public override Color MenuItemSelected => Color.FromArgb(230, 245, 230);
//        public override Color MenuItemBorder => Color.FromArgb(150, 200, 150);

//        // 5. Đường phân tách các nhóm chức năng (Separator)
//        public override Color SeparatorDark => Color.FromArgb(190, 220, 190);
//        public override Color SeparatorLight => Color.White;
//    }
//}