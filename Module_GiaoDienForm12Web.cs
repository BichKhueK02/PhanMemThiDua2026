using Krypton.Toolkit;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Giao diện kiểu web cho Form12 (Cài đặt) - CHỈ trình bày, không đụng logic / Designer.
    /// Dùng đúng 1 dòng ở cuối constructor của Form12:   GiaoDienForm12Web.ApDung(this);
    ///
    /// Gồm:
    ///  1) Thanh tab kiểu web cho 4 tab (chữ xanh đậm + gạch chân khi chọn, chữ xám khi không chọn, hover, đường kẻ nền)
    ///     - Tab thật (tabControl1) vẫn giữ nguyên: mọi sự kiện SelectedIndexChanged cũ (nạp Form33, nhúng Form cấu hình...) vẫn chạy.
    ///     - Phần vẽ nằm ở Module_TabWeb.cs (dùng lại được cho TabControl khác).
    ///  2) Nền trang tab phẳng, nhãn/ô chọn/nút theo bảng màu hiện đại.
    /// Mỗi phần có try/catch riêng: một phần lỗi không làm hỏng phần còn lại (log: Debug output).
    /// </summary>
    internal static class Module_GiaoDienForm12Web
    {
        // ===== Bảng màu =====
        private static readonly Color MauNen = Color.FromArgb(250, 250, 252);
        private static readonly Color MauChu = Color.FromArgb(55, 65, 81);
        private static readonly Color MauChuTieuDe = Color.FromArgb(31, 41, 55);
        private static readonly Color MauXanh = Color.FromArgb(59, 111, 240);
        private static readonly Color MauXanhDam = Color.FromArgb(43, 88, 214);
        private static readonly Color MauXanhNhat = Color.FromArgb(238, 242, 255);
        private static readonly Color MauXanhHover = Color.FromArgb(224, 231, 255);
        private static readonly Color MauXanhNhan = Color.FromArgb(199, 210, 254);
        private static readonly Color MauVien = Color.FromArgb(199, 210, 254);
        private static readonly Color MauChuXanhDam = Color.FromArgb(55, 48, 163);
        private const float BanKinhNut = 8f;
        private const int ChieuCaoThanhTab = 46;   // px ở 96 DPI
        private static readonly Dictionary<(string, float, FontStyle), Font> _fonts = new();
        private static Font LayFont(string ten, float co, FontStyle kieu)
        {
            lock (_fonts)
            {
                var k = (ten, co, kieu);
                if (!_fonts.TryGetValue(k, out var f))
                    _fonts[k] = f = new Font(ten, co, kieu, GraphicsUnit.Point);
                return f;
            }
        }
        public static void ApDung(Form? f)
        {
            if (f == null) return;
            Chay("thanh tab", () => DungThanhTab(f));
            Chay("nen trang", () => ChinhNenCacTrang(f));
            Chay("khung", () => ChinhKhung(f));
            Chay("nhan", () => ChinhNhan(f));
            Chay("nut", () => ChinhNut(f));
        }
        private static void Chay(string ten, Action a)
        {
            try { a(); }
            catch (Exception ex) { Debug.WriteLine("[GiaoDienForm12Web] " + ten + ": " + ex.Message); }
        }
        private static T? Tim<T>(Control goc, string ten) where T : Control
            => goc.Controls.Find(ten, true).OfType<T>().FirstOrDefault();
        // ------------------------------------------------------------------
        //  1) THANH TAB KIỂU WEB
        // ------------------------------------------------------------------
        private static void DungThanhTab(Form f)
        {
            var tc = Tim<TabControl>(f, "tabControl1");
            if (tc == null) return;
            // Vẽ lại ngay chính TabControl (xem Module_TabWeb.cs): giữ nguyên bố cục, sự kiện, TabPages
            Module_TabWeb.ApDung(tc, MauNen);
        }
        // ------------------------------------------------------------------
        //  2) NỀN TRANG + KHUNG + NHÃN + NÚT
        // ------------------------------------------------------------------
        private static void ChinhNenCacTrang(Form f)
        {
            var tc = Tim<TabControl>(f, "tabControl1");
            if (tc == null) return;
            foreach (TabPage p in tc.TabPages)
            {
                p.UseVisualStyleBackColor = false;
                p.BackColor = MauNen;
                p.ForeColor = MauChu;
                if (p.Padding.All == 0 || p.Padding == new Padding(2, 4, 2, 4))
                    p.Padding = new Padding(10, 10, 10, 6);
            }
        }
        private static void ChinhKhung(Form f)
        {
            string tenFont = Module_HeThong.TenFontHeThong;
            foreach (var gb in LayTatCa<GroupBox>(f))
            {
                gb.BackColor = Color.White;
                gb.ForeColor = MauChuTieuDe;
                gb.Font = LayFont(tenFont, 10.5f, FontStyle.Bold);
            }
        }
        private static void ChinhNhan(Form f)
        {
            string tenFont = Module_HeThong.TenFontHeThong;
            foreach (var lb in LayTatCa<Label>(f))
            {
                // Chỉ đổi nhãn tiêu đề trường (chữ xanh lá nghiêng); nhãn thông báo/phiên bản... giữ nguyên
                if (lb.ForeColor.ToArgb() != Color.Green.ToArgb()) continue;
                lb.ForeColor = MauChu;
                lb.Font = LayFont(tenFont, Math.Max(9.5f, lb.Font.Size), FontStyle.Regular);
            }
            foreach (var cb in LayTatCa<CheckBox>(f))
            {
                cb.ForeColor = MauChu;
                cb.Font = LayFont(tenFont, Math.Max(9.5f, cb.Font.Size), FontStyle.Regular);
            }
        }
        private static void ChinhNut(Form f)
        {
            foreach (var b in LayTatCa<KryptonButton>(f))
            {
                bool chinh = LaNutChinh(b);
                DatKieuNut(b, chinh);
            }
        }
        // Chỉ 2 nút "Lưu thông tin" và "Lưu cấu hình" là nút chính (xanh đặc). "Sao lưu CSDL" và các nút khác: kiểu nhạt.
        private static bool LaNutChinh(KryptonButton b)
        {
            string chu = (b.Values.Text ?? "").Trim();
            if (chu.Equals("Lưu thông tin", StringComparison.OrdinalIgnoreCase)
                || chu.Equals("Lưu cấu hình", StringComparison.OrdinalIgnoreCase)) return true;
            // Dự phòng theo tên điều khiển (không tính "SaoLuu"), phòng khi nút đang đổi chữ tạm thời
            string ten = b.Name;
            if (ten.Contains("SaoLuu", StringComparison.OrdinalIgnoreCase)) return false;
            return ten.Contains("LuuThongTin", StringComparison.OrdinalIgnoreCase)
                || ten.Contains("LuuCauHinh", StringComparison.OrdinalIgnoreCase);
        }
        internal static void DatKieuNut(KryptonButton b, bool chinh)
        {
            Color nen = chinh ? MauXanh : MauXanhNhat;
            Color hover = chinh ? MauXanhDam : MauXanhHover;
            Color nhan = chinh ? Color.FromArgb(34, 70, 180) : MauXanhNhan;
            Color chu = chinh ? Color.White : MauChuXanhDam;
            Color vien = chinh ? MauXanh : MauVien;
            b.StateCommon.Back.ColorStyle = PaletteColorStyle.Solid;
            b.StateCommon.Back.Color1 = nen;
            b.StateCommon.Back.Color2 = nen;
            b.StateCommon.Border.Rounding = BanKinhNut;
            b.StateCommon.Border.Width = 1;
            b.StateCommon.Border.Color1 = vien;
            b.StateCommon.Border.Color2 = vien;
            b.StateCommon.Content.ShortText.Color1 = chu;
            b.StateCommon.Content.ShortText.Color2 = chu;
            b.OverrideDefault.Back.Color1 = nen;
            b.OverrideDefault.Back.Color2 = nen;
            b.StateTracking.Back.Color1 = hover;
            b.StateTracking.Back.Color2 = hover;
            b.StatePressed.Back.Color1 = nhan;
            b.StatePressed.Back.Color2 = nhan;
            b.StateTracking.Content.ShortText.Color1 = chu;
            b.StateTracking.Content.ShortText.Color2 = chu;
            b.StatePressed.Content.ShortText.Color1 = chu;
            b.StatePressed.Content.ShortText.Color2 = chu;
        }
        private static IEnumerable<T> LayTatCa<T>(Control goc) where T : Control
        {
            foreach (Control c in goc.Controls)
            {
                if (c is T t) yield return t;
                foreach (var con in LayTatCa<T>(c)) yield return con;
            }
        }
    }
}