using System.Diagnostics;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CHO Form63_CaiDatLichXet — CHỈ TRÌNH BÀY, KHÔNG CÓ LOGIC NGHIỆP VỤ
    //  Cùng ngôn ngữ thiết kế với Form4 / Form20 / Form62: nền trang xám nhạt, thẻ trắng bo góc có tiêu đề,
    //  combo bo tròn, bảng xem trước có khung bo tròn liền mạch + tiêu đề tím nhạt, nút theme tím.
    //
    //  - Lớp static độc lập, không tạo .resx, KHÔNG sửa Designer.
    //  - Dùng lại TheBoGocF4 và OComboWebF4 trong Module_GiaoDienForm4Web.cs (file đó phải còn trong project).
    //  - Tìm control theo Name, không đổi tên/kiểu/sự kiện của control nào; các control gốc được chuyển chỗ chứa chứ không thay mới.
    //  - Mọi bước bọc try/catch: lỗi trình bày không bao giờ làm form không mở được.
    //
    //  Dùng trong Form63: constructor, ngay SAU InitializeComponent():   Module_GiaoDienForm63Web.ApDung(this);
    // =====================================================================================
    internal static class Module_GiaoDienForm63Web
    {
        private static readonly Color MauNen = Color.FromArgb(244, 246, 250);
        private static readonly Color MauChuNhan = Color.FromArgb(100, 116, 139);
        private static readonly Color MauChuDam = Color.FromArgb(30, 41, 59);
        private static readonly Color MauTieuDeBang = Color.FromArgb(238, 232, 255);
        private static readonly Color MauChuTieuDeBang = Color.FromArgb(75, 20, 130);
        private static readonly Color MauKeBang = Color.FromArgb(235, 238, 243);
        private static readonly Font FontNhan = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new Font("Segoe UI Semibold", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        private const int DemKhungBang = 5;
        private const int CaoTheCauHinh = 184;   // 36 tiêu đề thẻ + mô tả 2 dòng + hàng chọn + ô tích
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutTim =
            (Color.FromArgb(237, 233, 254), Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253), Color.FromArgb(196, 181, 253), Color.FromArgb(109, 40, 217));
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutTimDam =
            (Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253), Color.FromArgb(167, 139, 250), Color.FromArgb(167, 139, 250), Color.FromArgb(91, 33, 182));
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutVien =
            (Color.White, Color.FromArgb(248, 250, 252), Color.FromArgb(241, 245, 249), Color.FromArgb(203, 213, 225), Color.FromArgb(51, 65, 85));

        public static void ApDung(Form f)
        {
            f.SuspendLayout();
            try
            {
                f.BackColor = MauNen;
                An(() => DinhBoCuc(f), "BoCuc");
                An(() => BocTheCauHinh(f), "TheCauHinh");
                An(() => DinhONhap(f), "ONhap");
                An(() => BocLuoi(f), "KhungLuoi");
                An(() => StyleLuoi(f), "StyleLuoi");
                An(() => DinhChan(f), "Chan");
                An(() => BoTronCombo(f), "Combo");
                // Thêm chỗ cho thẻ cấu hình + tiêu đề thẻ bảng (lưới 6 đợt, không thanh cuộn, lại còn khối cảnh báo)
                An(() => f.ClientSize = new Size(806, 640), "KichThuoc");
            }
            finally
            {
                f.ResumeLayout(true);
            }
        }
        private static void An(Action viec, string ten)
        {
            try { viec(); }
            catch (Exception ex) { Debug.WriteLine($"[Module_GiaoDienForm63Web.{ten}] {ex.Message}"); }
        }
        private static T? Tim<T>(Control goc, string ten) where T : Control
            => goc.Controls.Find(ten, true).OfType<T>().FirstOrDefault();

        // ===================== BỐ CỤC TỔNG =====================
        private static void DinhBoCuc(Form f)
        {
            var t = Tim<TableLayoutPanel>(f, "tableLayoutPanel_Khung");
            if (t != null)
            {
                t.BackColor = MauNen;
                t.Margin = new Padding(0);
                t.Padding = new Padding(14, 12, 14, 8);
            }
            var tieuDe = Tim<Label>(f, "label_TieuDe");
            if (tieuDe != null) { tieuDe.ForeColor = Color.FromArgb(76, 29, 149); tieuDe.Margin = new Padding(2, 0, 0, 10); }
            var xt = Tim<Label>(f, "label_XemTruoc");
            if (xt != null) xt.Visible = false;   // chữ này chuyển lên tiêu đề của thẻ bảng
        }

        // ===================== THẺ CẤU HÌNH: mô tả | tháng bắt đầu | số tháng | kết quả tháng kế tiếp =====================
        // Các control nằm rải ở hàng 1–3 của bảng khung => gom sang 1 bảng con trong thẻ (cùng control, cùng sự kiện).
        private static void BocTheCauHinh(Form f)
        {
            var khung = Tim<TableLayoutPanel>(f, "tableLayoutPanel_Khung");
            var moTa = Tim<Label>(f, "label_MoTa");
            var lbThang = Tim<Label>(f, "label_ThangBatDau");
            var cbThang = Tim<ComboBox>(f, "comboBox_ThangBatDau");
            var lbSo = Tim<Label>(f, "label_SoThang");
            var nud = Tim<NumericUpDown>(f, "numericUpDown_SoThang");
            var chk = Tim<CheckBox>(f, "checkBox_KetQuaThangSau");
            if (khung == null || moTa == null || lbThang == null || cbThang == null || lbSo == null || nud == null || chk == null) return;

            var trong = new TableLayoutPanel
            {
                Name = "tlp_CauHinh_F63",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0),
                ColumnCount = 4,
                RowCount = 3
            };
            trong.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            trong.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            trong.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            trong.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            for (int i = 0; i < 3; i++) trong.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var the = new TheBoGocF4
            {
                Name = "the_CauHinh_F63",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 10),
                TieuDe = "Cấu hình lịch xét",
                Padding = new Padding(12, 36, 12, 10)
            };
            khung.SuspendLayout();
            the.SuspendLayout();
            try
            {
                foreach (Control c in new Control[] { moTa, lbThang, cbThang, lbSo, nud, chk }) khung.Controls.Remove(c);
                trong.Controls.Add(moTa, 0, 0); trong.SetColumnSpan(moTa, 4);
                trong.Controls.Add(lbThang, 0, 1);
                trong.Controls.Add(cbThang, 1, 1);
                trong.Controls.Add(lbSo, 2, 1);
                trong.Controls.Add(nud, 3, 1);
                trong.Controls.Add(chk, 0, 2); trong.SetColumnSpan(chk, 4);
                the.Controls.Add(trong);
                khung.Controls.Add(the, 0, 1);
                khung.SetColumnSpan(the, 4);
                khung.RowStyles[1] = new RowStyle(SizeType.Absolute, CaoTheCauHinh);
                khung.RowStyles[2] = new RowStyle(SizeType.Absolute, 0F);   // hàng 2–3 đã trống
                khung.RowStyles[3] = new RowStyle(SizeType.Absolute, 0F);
            }
            finally
            {
                the.ResumeLayout(false);
                khung.ResumeLayout(true);
            }
        }

        private static void DinhONhap(Form f)
        {
            var moTa = Tim<Label>(f, "label_MoTa");
            if (moTa != null) { moTa.ForeColor = MauChuNhan; moTa.MaximumSize = new Size(700, 0); }
            foreach (var n in new[] { "label_ThangBatDau", "label_SoThang" })
            {
                var lb = Tim<Label>(f, n);
                if (lb == null) continue;
                lb.Font = FontNhan;
                lb.ForeColor = MauChuDam;
                lb.BackColor = Color.White;
            }
            var cb = Tim<ComboBox>(f, "comboBox_ThangBatDau");
            if (cb != null)
            {
                cb.Font = FontNhan;   // đặt TRƯỚC khi gắn ô bo tròn (nó chép font của ComboBox)
                cb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                cb.Margin = new Padding(0, 3, 24, 3);
            }
            var nud = Tim<NumericUpDown>(f, "numericUpDown_SoThang");
            if (nud != null)
            {
                nud.BorderStyle = BorderStyle.FixedSingle;
                nud.BackColor = Color.White;
                nud.ForeColor = MauChuDam;
                nud.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                nud.Margin = new Padding(0, 3, 0, 3);
            }
            var chk = Tim<CheckBox>(f, "checkBox_KetQuaThangSau");
            if (chk != null)
            {
                chk.BackColor = Color.White;
                chk.ForeColor = MauChuDam;
                chk.Cursor = Cursors.Hand;
                chk.Margin = new Padding(0, 6, 0, 4);
            }
        }

        // ===================== BẢNG XEM TRƯỚC: thay RoundedPanel bằng thẻ có khung bo tròn liền mạch =====================
        private static void BocLuoi(Form f)
        {
            var luoi = Tim<DataGridView>(f, "dataGridView_XemTruoc");
            var vo = Tim<Control>(f, "roundedPanel_XemTruoc");
            if (luoi == null || vo == null || vo.Parent is not TableLayoutPanel cha) return;
            const int gap = DemKhungBang;
            var the = new TheBoGocF4
            {
                Name = "the_dataGridView_XemTruoc",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8),
                TieuDe = "Xem trước lịch xét",
                DemKhung = gap,
                Padding = new Padding(12 + gap, 36 + gap, 12 + gap, 10 + gap),
                LuoiBang = luoi
            };
            cha.SuspendLayout();
            try
            {
                var vt = cha.GetPositionFromControl(vo);
                int span = cha.GetColumnSpan(vo);
                cha.Controls.Remove(vo);
                cha.Controls.Add(the, vt.Column, vt.Row);
                if (span > 1) cha.SetColumnSpan(the, span);
                vo.Controls.Remove(luoi);
                the.Controls.Add(luoi);
                luoi.Dock = DockStyle.Fill;
                luoi.Margin = new Padding(0);
                luoi.BorderStyle = BorderStyle.None;
                luoi.ColumnHeadersHeightChanged += (s, e) => the.Invalidate();
                luoi.ColumnHeadersDefaultCellStyleChanged += (s, e) => the.Invalidate();
                vo.Dispose();   // RoundedPanel rỗng: code của form không dùng tới
            }
            finally
            {
                cha.ResumeLayout(true);
            }
        }

        private static void StyleLuoi(Form f)
        {
            var g = Tim<DataGridView>(f, "dataGridView_XemTruoc");
            if (g == null) return;
            g.BackgroundColor = Color.White;
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            g.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            g.GridColor = MauKeBang;
            g.ColumnHeadersHeight = 40;
            var h = g.ColumnHeadersDefaultCellStyle;
            h.BackColor = MauTieuDeBang;
            h.ForeColor = MauChuTieuDeBang;
            h.SelectionBackColor = MauTieuDeBang;
            h.SelectionForeColor = MauChuTieuDeBang;
        }

        // ===================== CHÂN TRANG: Đặt lại mặc định | Đóng | Lưu (xếp từ phải sang trái) =====================
        private static void DinhChan(Form f)
        {
            var fl = Tim<FlowLayoutPanel>(f, "flowLayoutPanel_Nut");
            if (fl != null) { fl.BackColor = MauNen; fl.Margin = new Padding(0); }
            var canhBao = Tim<Label>(f, "label_CanhBao");
            if (canhBao != null) canhBao.MaximumSize = new Size(760, 0);   // màu chữ do form giữ (đỏ cảnh báo)
            Nut(f, "kryptonButton_Luu", NutTimDam, 120);
            Nut(f, "kryptonButton_Huy", NutVien, 120);
            Nut(f, "kryptonButton_DatLaiMacDinh", NutTim, 170);
        }
        private static void Nut(Form f, string ten, (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) m, int rong)
        {
            var b = Tim<Krypton.Toolkit.KryptonButton>(f, ten);
            if (b == null) return;
            KieuNut(b, m);
            b.Size = new Size(rong, 36);
            b.Margin = new Padding(10, 0, 0, 0);
        }

        // ===================== COMBO BO TRÒN (OComboWebF4 của Form4) =====================
        private static void BoTronCombo(Form f)
        {
            var cb = Tim<ComboBox>(f, "comboBox_ThangBatDau");
            if (cb != null && cb.GetType() == typeof(ComboBox) && cb.Parent != null)
                OComboWebF4.Gan(cb);
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
            // AcceptButton (Lưu) / CancelButton: ép trạng thái focus + mặc định giống trạng thái thường (tránh chữ trắng nền trắng)
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