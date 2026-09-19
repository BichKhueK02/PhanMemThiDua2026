using Microsoft.Data.Sqlite;
using System;
using System.Drawing;
using System.Windows.Forms;
namespace PhanMemThiDua2026
{
    internal static class Module_MenuChuotPhai
    {
        // Cache tên màu đang chọn trên RAM để tránh đọc ổ cứng liên tục khi mở menu
        public static string MauHienTai { get; set; } = "Mặc định";
        public static void TichHopGiaoDien(ContextMenuStrip menu)
        {
            if (menu == null) return;
            menu.Opening += (s, ev) =>
            {
                menu.RenderMode = ToolStripRenderMode.Professional;
                menu.Renderer = new ToolStripProfessionalRenderer(LayBangMauTheoTen(MauHienTai));
                menu.ShowImageMargin = true;
            };
        }
        // Tự động đọc màu đã lưu từ CSDL2 khi khởi động phần mềm
        public static void KhoiTaoMauTuCSDL(string csdl2Path)
        {
            try
            {
                if (!System.IO.File.Exists(csdl2Path)) return;
                using var conn = new SqliteConnection($"Data Source={csdl2Path};Mode=ReadOnly");
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
        // Factory tạo bảng màu chuẩn UX
        public static ProfessionalColorTable LayBangMauTheoTen(string tenMau)
        {
            return tenMau switch
            {
                "Xanh lá" => new GreenMenuColorTable(),
                "Xanh dương" => new BlueMenuColorTable(),
                "Xám trắng" => new GrayMenuColorTable(),
                _ => new YellowMenuColorTable() // "Mặc định" hoặc giá trị trống sẽ dùng màu vàng nhạt
            };
        }
    }
    // --- BẢNG MÀU VÀNG NHẠT (MẶC ĐỊNH MỚI) ---
    public class YellowMenuColorTable : ProfessionalColorTable
    {
        private readonly Color _margin = Color.FromArgb(250, 245, 225); // Vàng kem nhạt cho lề icon
        public override Color ImageMarginGradientBegin => _margin;
        public override Color ImageMarginGradientMiddle => _margin;
        public override Color ImageMarginGradientEnd => _margin;
        public override Color ToolStripDropDownBackground => Color.FromArgb(255, 253, 245); // Nền vàng kem sáng cực nhẹ
        public override Color MenuBorder => Color.FromArgb(210, 190, 140); // Viền vàng nâu nhạt
        public override Color MenuItemSelected => Color.FromArgb(255, 245, 205); // Vàng ấm khi hover
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(255, 245, 205);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(255, 245, 205);
        public override Color MenuItemBorder => Color.FromArgb(230, 205, 140);
        public override Color SeparatorDark => Color.FromArgb(235, 220, 180);
        public override Color SeparatorLight => Color.White;
    }
    // --- BẢNG MÀU XANH LÁ ---
    public class GreenMenuColorTable : ProfessionalColorTable
    {
        private readonly Color _margin = Color.FromArgb(215, 240, 215);
        public override Color ImageMarginGradientBegin => _margin;
        public override Color ImageMarginGradientMiddle => _margin;
        public override Color ImageMarginGradientEnd => _margin;
        public override Color ToolStripDropDownBackground => Color.FromArgb(240, 252, 240);
        public override Color MenuBorder => Color.FromArgb(120, 170, 120);
        public override Color MenuItemSelected => Color.FromArgb(200, 235, 200);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(200, 235, 200);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(200, 235, 200);
        public override Color MenuItemBorder => Color.FromArgb(140, 190, 140);
        public override Color SeparatorDark => Color.FromArgb(180, 215, 180);
        public override Color SeparatorLight => Color.White;
    }
    // --- BẢNG MÀU XANH DƯƠNG ---
    public class BlueMenuColorTable : ProfessionalColorTable
    {
        private readonly Color _margin = Color.FromArgb(215, 230, 250);
        public override Color ImageMarginGradientBegin => _margin;
        public override Color ImageMarginGradientMiddle => _margin;
        public override Color ImageMarginGradientEnd => _margin;
        public override Color ToolStripDropDownBackground => Color.FromArgb(242, 247, 255);
        public override Color MenuBorder => Color.FromArgb(120, 160, 210);
        public override Color MenuItemSelected => Color.FromArgb(205, 225, 250);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(205, 225, 250);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(205, 225, 250);
        public override Color MenuItemBorder => Color.FromArgb(140, 180, 220);
        public override Color SeparatorDark => Color.FromArgb(180, 205, 235);
        public override Color SeparatorLight => Color.White;
    }
    // --- BẢNG MÀU XÁM TRẮNG (OFFICE CLASSIC) ---
    public class GrayMenuColorTable : ProfessionalColorTable
    {
        private readonly Color _margin = Color.FromArgb(238, 238, 238);
        public override Color ImageMarginGradientBegin => _margin;
        public override Color ImageMarginGradientMiddle => _margin;
        public override Color ImageMarginGradientEnd => _margin;
        public override Color ToolStripDropDownBackground => Color.White;
        public override Color MenuBorder => Color.FromArgb(180, 180, 180);
        public override Color MenuItemSelected => Color.FromArgb(230, 230, 230);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(230, 230, 230);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(230, 230, 230);
        public override Color MenuItemBorder => Color.FromArgb(170, 170, 170);
        public override Color SeparatorDark => Color.FromArgb(210, 210, 210);
        public override Color SeparatorLight => Color.White;
    }
}