using System.Diagnostics;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CHO Form45_TyLeBaNhat (Cài đặt tỷ lệ % biểu dương thi đua Ba nhất) — CHỈ TRÌNH BÀY, KHÔNG CÓ LOGIC NGHIỆP VỤ
    //  Cùng ngôn ngữ thiết kế với Form4 / Form10: nền trang xám nhạt, thẻ trắng bo góc có tiêu đề,
    //  ô nhập bo góc viền tím khi đang gõ, nút theme tím.
    //
    //  - Lớp static độc lập, không tạo .resx, KHÔNG cần sửa .csproj, KHÔNG sửa Designer.
    //  - Dùng lại TheBoGocF4 (thẻ bo góc) của Module_GiaoDienForm4Web.cs, chỉ dùng các thuộc tính
    //    Name / Dock / Margin / TieuDe / Padding giống hệt Module_GiaoDienForm10Web.
    //  - Tìm control theo Name (không phụ thuộc private) => không đổi tên/kiểu/sự kiện của control nào.
    //  - Mọi bước bọc try/catch: lỗi trình bày không bao giờ làm form không mở được.
    //
    //  Dùng trong Form45_TyLeBaNhat:
    //     constructor, ngay SAU InitializeComponent():   Module_GiaoDienForm45Web.ApDung(this);
    // =====================================================================================
    internal static class Module_GiaoDienForm45Web
    {
        // ---------- Bảng màu / font: lấy đúng giá trị của Form4 / Form10 ----------
        private static readonly Color MauNen = Color.FromArgb(244, 246, 250);
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);
        private static readonly Color MauChuDam = Color.FromArgb(30, 41, 59);
        private static readonly Color MauVienO = Color.FromArgb(203, 213, 225);
        private static readonly Color MauVienChon = Color.FromArgb(139, 92, 246);
        private static readonly Font FontNhan = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontO = new Font("Segoe UI Semibold", 10F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point);
        // (Nền, Hover, Nhấn, Viền, Chữ) — theme tím của Form4: nút chính (tím đậm một nấc)
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutTimDam =
            (Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253), Color.FromArgb(167, 139, 250), Color.FromArgb(167, 139, 250), Color.FromArgb(91, 33, 182));
        // ===================== ĐIỂM VÀO =====================
        public static void ApDung(Form f)
        {
            f.SuspendLayout();
            try
            {
                f.BackColor = MauNen;
                An(() => DinhBoCuc(f), "BoCuc");
                An(() => BocCacThe(f), "CacThe");
                An(() => DinhHangNhap(f), "HangNhap");
                An(() => DinhNutLuu(f), "NutLuu");
                An(() => f.ClientSize = new Size(580, 172), "KichThuoc");
            }
            finally
            {
                f.ResumeLayout(true);
            }
        }
        private static void An(Action viec, string ten)
        {
            try { viec(); }
            catch (Exception ex) { Debug.WriteLine($"[Module_GiaoDienForm45Web.{ten}] {ex.Message}"); }
        }
        private static T? Tim<T>(Control goc, string ten) where T : Control
            => goc.Controls.Find(ten, true).OfType<T>().FirstOrDefault();
        // ===================== BỐ CỤC TỔNG =====================
        // Bản gốc: tableLayoutPanel6 chia 27% | 73% theo cỡ cửa sổ. Mới: cột ảnh cố định, cột nhập ăn hết phần còn lại,
        // mỗi cột là 1 thẻ trắng bo góc nằm trên nền trang xám nhạt.
        private static void DinhBoCuc(Form f)
        {
            var t6 = Tim<TableLayoutPanel>(f, "tableLayoutPanel6");
            if (t6 != null)
            {
                t6.BackColor = MauNen;
                t6.Margin = new Padding(0);
                t6.Padding = new Padding(12, 10, 12, 10);
                t6.ColumnStyles.Clear();
                t6.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
                t6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            }
            // Bảng chứa "hàng nhập" + nút Lưu: hàng nhập cao cố định, nút Lưu lấy phần còn lại
            var t1 = Tim<TableLayoutPanel>(f, "tableLayoutPanel1");
            if (t1 != null)
            {
                t1.BackColor = Color.White;
                t1.Margin = new Padding(0);
                t1.ColumnStyles.Clear();
                t1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                t1.RowStyles.Clear();
                t1.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
                t1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            }
        }
        // ===================== THẺ =====================
        private static void BocCacThe(Form f)
        {
            // Ảnh đại diện -> thẻ không tiêu đề
            var anh = Tim<PictureBox>(f, "pictureBox2_AnhDaiDienAdmin");
            if (anh != null)
            {
                var the = ThayBangThe(anh, "", new Padding(10));
                if (the != null)
                {
                    anh.Dock = DockStyle.Fill;
                    anh.Margin = new Padding(0);
                    anh.BackColor = Color.White;
                    anh.SizeMode = PictureBoxSizeMode.Zoom;    // trước đây StretchImage -> méo hình
                }
            }
            // Khối nhập tỷ lệ + nút Lưu -> thẻ có tiêu đề
            var t1 = Tim<TableLayoutPanel>(f, "tableLayoutPanel1");
            if (t1 != null)
            {
                var the = ThayBangThe(t1, "Tỷ lệ % biểu dương thi đua Ba nhất", new Padding(12, 36, 12, 10));   // 36 = vùng tiêu đề thẻ
                if (the != null)
                {
                    t1.Dock = DockStyle.Fill;
                    t1.Margin = new Padding(0);
                }
            }
        }
        /// <summary>
        /// Thay 1 control nằm trong ô TableLayoutPanel bằng TheBoGocF4 (thẻ trắng bo góc của Form4) đúng ô đó,
        /// rồi đặt chính control đó vào thẻ (không đổi tên, không đổi sự kiện).
        /// </summary>
        private static TheBoGocF4? ThayBangThe(Control nguon, string tieuDe, Padding padding)
        {
            if (nguon.Parent is not TableLayoutPanel cha) return null;
            var vt = cha.GetPositionFromControl(nguon);
            int spanCot = cha.GetColumnSpan(nguon), spanHang = cha.GetRowSpan(nguon);
            bool cotCuoi = vt.Column + spanCot >= cha.ColumnCount;
            var the = new TheBoGocF4
            {
                Name = "the_" + nguon.Name,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, cotCuoi ? 0 : 10, 0),   // khe 10px giữa các thẻ trong hàng
                TieuDe = tieuDe,
                Padding = padding
            };
            cha.SuspendLayout();
            the.SuspendLayout();
            try
            {
                cha.Controls.Remove(nguon);
                cha.Controls.Add(the, vt.Column, vt.Row);
                the.Controls.Add(nguon);
                if (spanCot > 1) cha.SetColumnSpan(the, spanCot);
                if (spanHang > 1) cha.SetRowSpan(the, spanHang);
            }
            finally
            {
                the.ResumeLayout(false);
                cha.ResumeLayout(true);
            }
            return the;
        }
        // ===================== HÀNG NHẬP: "Đặt tỷ lệ" | ô số | "%/ Số lượng Loại 1" =====================
        private static void DinhHangNhap(Form f)
        {
            var t2 = Tim<TableLayoutPanel>(f, "tableLayoutPanel2");
            if (t2 != null)
            {
                t2.BackColor = Color.White;
                t2.Margin = new Padding(0);
                t2.ColumnStyles.Clear();
                t2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84F));
                t2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104F));
                t2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            }
            DinhNhan(Tim<Label>(f, "label2_ChonTep"), true);    // Đặt tỷ lệ
            DinhNhan(Tim<Label>(f, "label1"), false);           // %/ Số lượng Loại 1
            var o = Tim<Krypton.Toolkit.KryptonTextBox>(f, "textBoxKrypton_TyLePhanTramBaNhat");
            if (o != null)
            {
                o.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                o.Margin = new Padding(0, 3, 0, 3);
                o.StateCommon.Back.Color1 = Color.White;
                KieuOKrypton(o.StateCommon.Border, o.StateActive.Border);
                o.StateCommon.Content.Font = FontO;
                o.StateCommon.Content.Color1 = MauChuDam;
            }
        }
        private static void DinhNhan(Label? lb, bool dauHang)
        {
            if (lb == null) return;
            lb.Font = FontNhan;
            lb.ForeColor = MauChuNhan;
            lb.Anchor = AnchorStyles.Left;
            lb.Margin = new Padding(dauHang ? 0 : 8, 0, 6, 0);
        }
        // ===================== NÚT LƯU =====================
        private static void DinhNutLuu(Form f)
        {
            var b = Tim<Krypton.Toolkit.KryptonButton>(f, "btnLuu");
            if (b == null) return;
            KieuNut(b, NutTimDam);
            b.Anchor = AnchorStyles.Right;
            b.Size = new Size(180, 34);
            b.Margin = new Padding(0);
        }
        // Ô nhập Krypton: nền trắng, viền xám bo góc 8, khi đang gõ viền chuyển tím
        private static void KieuOKrypton(Krypton.Toolkit.PaletteBorder vien, Krypton.Toolkit.PaletteBorder vienActive)
        {
            vien.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            vien.Color1 = MauVienO; vien.Color2 = MauVienO;
            vien.Rounding = 8F;
            vien.Width = 1;
            vienActive.Color1 = MauVienChon; vienActive.Color2 = MauVienChon;
        }
        private static void KieuNut(Krypton.Toolkit.KryptonButton b, (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) m)
        {
            b.StateCommon.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            b.StateCommon.Border.Rounding = 8F;
            b.StateCommon.Border.Width = 1;
            b.StateCommon.Content.ShortText.Font = FontNut;
            b.StateCommon.Content.ShortText.Color1 = m.Chu;
            b.StateNormal.Back.Color1 = m.Nen; b.StateNormal.Back.Color2 = m.Nen;
            b.StateNormal.Border.Color1 = m.Vien; b.StateNormal.Border.Color2 = m.Vien;
            b.StateNormal.Content.ShortText.Color1 = m.Chu;
            b.StateTracking.Back.Color1 = m.Hover; b.StateTracking.Back.Color2 = m.Hover;
            b.StateTracking.Border.Color1 = m.Vien; b.StateTracking.Border.Color2 = m.Vien;
            b.StateTracking.Border.Rounding = 8F;
            b.StateTracking.Content.ShortText.Color1 = m.Chu;
            b.StatePressed.Back.Color1 = m.Down; b.StatePressed.Back.Color2 = m.Down;
            b.StatePressed.Border.Color1 = m.Vien; b.StatePressed.Border.Color2 = m.Vien;
            b.StatePressed.Content.ShortText.Color1 = m.Chu;
            // Form45 đặt AcceptButton = btnLuu => nút luôn là "nút mặc định" và thường giữ focus:
            // ép giống trạng thái thường để không bị chữ trắng trên nền trắng (cách Form4 đã làm)
            foreach (var ov in new[] { b.OverrideFocus, b.OverrideDefault })
            {
                ov.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
                ov.Back.Color1 = m.Nen; ov.Back.Color2 = m.Nen;
                ov.Border.Color1 = m.Vien; ov.Border.Color2 = m.Vien;
                ov.Content.ShortText.Color1 = m.Chu;
            }
            // Nút bị khóa (lúc đang lưu: chữ "Đang lưu..."): tím rất nhạt, chữ tím nhạt vẫn đọc được
            b.StateDisabled.Back.Color1 = Color.FromArgb(245, 243, 255); b.StateDisabled.Back.Color2 = Color.FromArgb(245, 243, 255);
            b.StateDisabled.Border.Color1 = Color.FromArgb(221, 214, 254); b.StateDisabled.Border.Color2 = Color.FromArgb(221, 214, 254);
            b.StateDisabled.Content.ShortText.Color1 = Color.FromArgb(167, 139, 250);
            b.Cursor = Cursors.Hand;
        }
    }
}