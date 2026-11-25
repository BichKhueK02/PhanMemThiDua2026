using System.Diagnostics;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CHO Form62_ChinhSuaDataNhomTuan — CHỈ TRÌNH BÀY, KHÔNG CÓ LOGIC NGHIỆP VỤ
    //  Cùng ngôn ngữ thiết kế với Form4 / Form20 / Form21: nền trang xám nhạt, thẻ trắng bo góc,
    //  bảng có khung bo tròn liền mạch + tiêu đề tím nhạt, nút theme tím.
    //
    //  - Lớp static độc lập, không tạo .resx, KHÔNG sửa Designer.
    //  - Dùng lại TheBoGocF4 (thẻ bo góc) trong Module_GiaoDienForm4Web.cs (file đó phải còn trong project).
    //  - Tìm control theo Name, không đổi tên/kiểu/sự kiện của control nào. Không đụng tới font các nhãn đầu trang
    //    (Form62.VuaKhung tự thu nhỏ cỡ chữ theo font của Designer) và CellFormatting / CellPainting của lưới.
    //  - Mọi bước bọc try/catch: lỗi trình bày không bao giờ làm form không mở được.
    //
    //  Dùng trong Form62: constructor, ngay SAU InitializeComponent():   Module_GiaoDienForm62Web.ApDung(this);
    // =====================================================================================
    internal static class Module_GiaoDienForm62Web
    {
        private static readonly Color MauNen = Color.FromArgb(244, 246, 250);
        private static readonly Color MauChuNhan = Color.FromArgb(100, 116, 139);
        private static readonly Color MauChuDam = Color.FromArgb(30, 41, 59);
        private static readonly Color MauTim = Color.FromArgb(76, 29, 149);
        private static readonly Color MauTieuDeBang = Color.FromArgb(238, 232, 255);
        private static readonly Color MauChuTieuDeBang = Color.FromArgb(75, 20, 130);
        private static readonly Color MauKeBang = Color.FromArgb(235, 238, 243);
        private static readonly Font FontNut = new Font("Segoe UI Semibold", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        private const int DemKhungBang = 5;
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutTimDam =
            (Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253), Color.FromArgb(167, 139, 250), Color.FromArgb(167, 139, 250), Color.FromArgb(91, 33, 182));

        public static void ApDung(Form f)
        {
            f.SuspendLayout();
            try
            {
                f.BackColor = MauNen;
                An(() => DinhBoCuc(f), "BoCuc");
                An(() => BocDauTrang(f), "DauTrang");
                An(() => BocLuoi(f), "KhungLuoi");
                An(() => StyleLuoi(f), "StyleLuoi");
                An(() => DinhChan(f), "Chan");
            }
            finally
            {
                f.ResumeLayout(true);
            }
        }
        private static void An(Action viec, string ten)
        {
            try { viec(); }
            catch (Exception ex) { Debug.WriteLine($"[Module_GiaoDienForm62Web.{ten}] {ex.Message}"); }
        }
        private static T? Tim<T>(Control goc, string ten) where T : Control
            => goc.Controls.Find(ten, true).OfType<T>().FirstOrDefault();

        // ===================== BỐ CỤC TỔNG =====================
        private static void DinhBoCuc(Form f)
        {
            var t = Tim<TableLayoutPanel>(f, "tableLayoutPanel_Khung");
            if (t == null) return;
            t.BackColor = MauNen;
            t.Margin = new Padding(0);
            t.Padding = new Padding(12, 10, 12, 8);
        }

        // ===================== ĐẦU TRANG: thẻ trắng (logo | họ tên | số hiệu | đơn vị) =====================
        private static void BocDauTrang(Form f)
        {
            var t = Tim<TableLayoutPanel>(f, "tableLayoutPanel_DauTrang");
            if (t == null) return;
            if (ThayBangThe(t, "", new Padding(12, 6, 12, 6), new Padding(0, 0, 0, 10)) == null) return;
            t.BackColor = Color.White;
            t.Margin = new Padding(0);
            // Chỉ đổi màu chữ; font do Form62.VuaKhung quản lý (thu nhỏ khi tên dài)
            var ten = Tim<Label>(f, "label_HoTen");
            if (ten != null) ten.ForeColor = MauTim;
            foreach (var n in new[] { "label_SoHieu", "label_DonVi" })
            {
                var lb = Tim<Label>(f, n);
                if (lb != null) lb.ForeColor = MauChuDam;
            }
            var anh = Tim<PictureBox>(f, "pictureBox1");
            if (anh != null) anh.BackColor = Color.White;
        }

        // ===================== LƯỚI: thay RoundedPanel bằng thẻ có khung bo tròn liền mạch =====================
        private static void BocLuoi(Form f)
        {
            var luoi = Tim<DataGridView>(f, "dataGridView_LichXet");
            var vo = Tim<Control>(f, "roundedPanel_Luoi");
            if (luoi == null || vo == null || vo.Parent is not TableLayoutPanel cha) return;
            const int gap = DemKhungBang;
            var the = new TheBoGocF4
            {
                Name = "the_dataGridView_LichXet",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8),
                TieuDe = "Kết quả thi đua theo tuần",
                DemKhung = gap,
                Padding = new Padding(12 + gap, 36 + gap, 12 + gap, 10 + gap),
                LuoiBang = luoi
            };
            cha.SuspendLayout();
            try
            {
                var vt = cha.GetPositionFromControl(vo);
                cha.Controls.Remove(vo);
                cha.Controls.Add(the, vt.Column, vt.Row);
                vo.Controls.Remove(luoi);
                the.Controls.Add(luoi);
                luoi.Dock = DockStyle.Fill;
                luoi.Margin = new Padding(0);
                luoi.BorderStyle = BorderStyle.None;
                luoi.ColumnHeadersHeightChanged += (s, e) => the.Invalidate();
                luoi.ColumnHeadersDefaultCellStyleChanged += (s, e) => the.Invalidate();
                luoi.DataBindingComplete += (s, e) => the.Invalidate();
                vo.Dispose();   // RoundedPanel rỗng: code của form không dùng tới
            }
            finally
            {
                cha.ResumeLayout(true);
            }
        }

        // Chỉ đổi MÀU / KÍCH THƯỚC; màu chữ + nền từng ô (Loại 1–4) vẫn do Luoi_CellFormatting của form quyết định.
        private static void StyleLuoi(Form f)
        {
            var g = Tim<DataGridView>(f, "dataGridView_LichXet");
            if (g == null) return;
            g.BackgroundColor = Color.White;
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            g.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            g.GridColor = MauKeBang;
            g.RowTemplate.Height = 36;   // đặt trước khi form thêm hàng
            g.ColumnHeadersHeight = 46;
            var h = g.ColumnHeadersDefaultCellStyle;
            h.BackColor = MauTieuDeBang;
            h.ForeColor = MauChuTieuDeBang;
            h.SelectionBackColor = MauTieuDeBang;
            h.SelectionForeColor = MauChuTieuDeBang;
        }

        // ===================== CHÂN TRANG: hướng dẫn | Cập nhật =====================
        private static void DinhChan(Form f)
        {
            var t = Tim<TableLayoutPanel>(f, "tableLayoutPanel_Nut");
            if (t != null) { t.BackColor = MauNen; t.Margin = new Padding(0); }
            var hd = Tim<Label>(f, "label_HuongDan");
            if (hd != null) hd.ForeColor = MauChuNhan;
            var ct = Tim<Label>(f, "label_ChuThich");
            if (ct != null) ct.ForeColor = MauChuNhan;
            var b = Tim<Krypton.Toolkit.KryptonButton>(f, "kryptonButton_CapNhat");
            if (b != null)
            {
                KieuNut(b, NutTimDam);
                b.Anchor = AnchorStyles.Right;
                b.Size = new Size(170, 36);
                b.Margin = new Padding(0);
            }
        }

        private static TheBoGocF4? ThayBangThe(Control nguon, string tieuDe, Padding padding, Padding margin)
        {
            if (nguon.Parent is not TableLayoutPanel cha) return null;
            var vt = cha.GetPositionFromControl(nguon);
            int spanCot = cha.GetColumnSpan(nguon), spanHang = cha.GetRowSpan(nguon);
            var the = new TheBoGocF4
            {
                Name = "the_" + nguon.Name,
                Dock = DockStyle.Fill,
                Margin = margin,
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
            // AcceptButton của form: ép trạng thái focus / mặc định giống trạng thái thường (tránh chữ trắng nền trắng)
            foreach (var ov in new[] { b.OverrideFocus, b.OverrideDefault })
            {
                ov.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
                ov.Back.Color1 = m.Nen; ov.Back.Color2 = m.Nen;
                ov.Border.Color1 = m.Vien; ov.Border.Color2 = m.Vien;
                ov.Content.ShortText.Color1 = m.Chu;
            }
            b.StateDisabled.Back.Color1 = Color.FromArgb(245, 243, 255); b.StateDisabled.Back.Color2 = Color.FromArgb(245, 243, 255);
            b.StateDisabled.Border.Color1 = Color.FromArgb(221, 214, 254); b.StateDisabled.Border.Color2 = Color.FromArgb(221, 214, 254);
            b.StateDisabled.Content.ShortText.Color1 = Color.FromArgb(167, 139, 250);
            b.Cursor = Cursors.Hand;
        }
    }
}