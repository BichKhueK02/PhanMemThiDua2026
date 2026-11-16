using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
using System.Diagnostics;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CHO Form13_DSChiHuy (Danh sách chỉ huy đơn vị) — CHỈ TRÌNH BÀY, KHÔNG CÓ LOGIC NGHIỆP VỤ
    //  Cùng ngôn ngữ thiết kế với Form4 (Module_GiaoDienForm4Web): nền trang xám nhạt, thẻ trắng bo góc có tiêu đề,
    //  nút theme tím nhạt, ô nhập bo góc viền tím khi đang gõ, bảng có khung bo tròn liền mạch + tiêu đề tím nhạt.
    //
    //  - Lớp static độc lập, không tạo .resx, KHÔNG cần sửa .csproj, KHÔNG sửa Designer.
    //  - Dùng lại TheBoGocF4 (thẻ bo góc) của Module_GiaoDienForm4Web.cs, chỉ dùng các thuộc tính
    //    Name / Dock / Margin / TieuDe / Padding / DemKhung / LuoiBang giống hệt Module_GiaoDienForm10Web.
    //  - Tìm control theo Name (không phụ thuộc private) => không đổi tên/kiểu/sự kiện của control nào
    //    (khóa/mở ô theo từng dòng, in hoa chức vụ, Enter = Cập nhật... vẫn do form gốc xử lý).
    //  - Mọi bước bọc try/catch: lỗi trình bày không bao giờ làm form không mở được.
    //
    //  Dùng trong Form13_DSChiHuy:
    //     CUỐI constructor (sau InitToolTips()):   Module_GiaoDienForm13Web.ApDung(this);
    //     DinhDangGrid(), cuối khối try:           Module_GiaoDienForm13Web.StyleLuoi(grid);
    //     3 màu viền ô nhập (_focusColor / _hoverColor / _normalColor) lấy từ module để đồng bộ theme tím.
    // =====================================================================================
    internal static class Module_GiaoDienForm13Web
    {
        // ---------- Màu viền ô nhập: Form13 tự đổi viền khi focus / hover / rời ô nên lấy chung từ đây ----------
        public static readonly Color MauVienO = Color.FromArgb(203, 213, 225);        // viền thường
        public static readonly Color MauVienChon = Color.FromArgb(139, 92, 246);      // đang gõ (tím)
        public static readonly Color MauVienHover = Color.FromArgb(196, 181, 253);    // rê chuột (tím nhạt)

        // ---------- Bảng màu / font: lấy đúng giá trị của Form4 / Form10 ----------
        private static readonly Color MauNen = Color.FromArgb(244, 246, 250);
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);
        private static readonly Color MauChuDam = Color.FromArgb(30, 41, 59);

        private static readonly Color MauTieuDeBang = Color.FromArgb(238, 232, 255);
        private static readonly Color MauChuTieuDeBang = Color.FromArgb(75, 20, 130);

        private static readonly Font FontNhan = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNhanDam = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontO = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point);
        // Font lưới: giữ đúng font hệ thống mà Form13 / Form10 vẫn dùng cho lưới
        private static readonly Font FontLuoiTieuDe = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Bold);
        private static readonly Font FontLuoiO = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Regular);

        // Khoảng đệm giữa khung bo tròn và bảng (px thiết kế): đủ để 4 góc vuông của bảng nằm lọt trong cung bo
        private const int DemKhungBang = 5;
        private const int CaoTheNhap = 340;         // thẻ nhập 6 dòng (mỗi dòng ~49px cho thoáng) + khe 10px phía dưới
        private const int CaoChanTrang = 52;        // hàng thông báo + nút Cập nhật
        private const int CaoDauBang = 42;          // tiêu đề cột của lưới
        private const int CaoDongBang = 30;         // mỗi dòng của lưới (6 dòng chỉ huy)

        // (Nền, Hover, Nhấn, Viền, Chữ) — theme tím nhạt của Form4: nút chính (tím đậm một nấc)
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
                An(() => BocTheNhap(f), "TheNhap");
                An(() => DinhBangNhap(f), "BangNhap");
                An(() => DinhChanTrang(f), "ChanTrang");
                An(() => BocLuoi(f), "KhungLuoi");
                An(() =>
                {
                    var luoi = Tim<DataGridView>(f, "kryptonDataGridView1");
                    if (luoi != null) StyleLuoi(luoi);
                }, "StyleLuoi");
                An(() => f.ClientSize = new Size(860, 704), "KichThuoc");
            }
            finally
            {
                f.ResumeLayout(true);
            }
        }

        private static void An(Action viec, string ten)
        {
            try { viec(); }
            catch (Exception ex) { Debug.WriteLine($"[Module_GiaoDienForm13Web.{ten}] {ex.Message}"); }
        }

        private static T? Tim<T>(Control goc, string ten) where T : Control
            => goc.Controls.Find(ten, true).OfType<T>().FirstOrDefault();

        // ===================== BỐ CỤC TỔNG =====================
        // Bản gốc: 3 hàng chia theo % (lưới 40% | nhập 50% | nút 10%) nên cao thấp theo cửa sổ.
        // Mới: thẻ nhập + hàng nút cao cố định, thẻ lưới ăn hết phần còn lại.
        private static void DinhBoCuc(Form f)
        {
            var t1 = Tim<TableLayoutPanel>(f, "TableLayoutPanel1");
            if (t1 == null) return;
            t1.BackColor = MauNen;
            t1.Margin = new Padding(0);
            t1.Padding = new Padding(12, 10, 12, 10);
            t1.RowStyles.Clear();
            t1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            t1.RowStyles.Add(new RowStyle(SizeType.Absolute, CaoTheNhap));
            t1.RowStyles.Add(new RowStyle(SizeType.Absolute, CaoChanTrang));
        }

        // ===================== THẺ NHẬP LIỆU =====================
        private static void BocTheNhap(Form f)
        {
            var t2 = Tim<TableLayoutPanel>(f, "TableLayoutPanel2");
            if (t2 == null) return;
            var the = ThayBangThe(t2, "Cập nhật thông tin chỉ huy", new Padding(12, 36, 12, 10), new Padding(0, 0, 0, 10));   // 36 = vùng tiêu đề thẻ
            if (the == null) return;
            t2.Dock = DockStyle.Fill;
            t2.Margin = new Padding(0);
            t2.BackColor = Color.White;
        }

        /// <summary>
        /// Thay 1 control nằm trong ô TableLayoutPanel bằng TheBoGocF4 (thẻ trắng bo góc của Form4) đúng ô đó,
        /// rồi đặt chính control đó vào thẻ (không đổi tên, không đổi sự kiện).
        /// </summary>
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

        // ===================== NỘI DUNG THẺ NHẬP: 6 DÒNG "n. Đồng chí | Họ tên | Chức vụ | Chức vụ" =====================
        private static void DinhBangNhap(Form f)
        {
            var t2 = Tim<TableLayoutPanel>(f, "TableLayoutPanel2");
            if (t2 == null) return;

            // Cột nhãn cố định -> 6 dòng thẳng cột; cột nhập chia 55 : 45 (họ tên dài hơn chức vụ)
            t2.ColumnStyles.Clear();
            t2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96F));     // "n. Đồng chí"
            t2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));      // họ tên
            t2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));     // "Chức vụ"
            t2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));      // chức vụ
            t2.RowStyles.Clear();
            for (int i = 0; i < t2.RowCount; i++)
                t2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / t2.RowCount));

            foreach (Control c in t2.Controls)
            {
                int cot = t2.GetColumn(c);
                switch (c)
                {
                    case Label lb:
                        lb.AutoSize = true;
                        lb.Anchor = AnchorStyles.Left;
                        if (cot == 0)       // "n. Đồng chí": nhãn đầu hàng, đậm hơn
                        {
                            lb.Font = FontNhanDam;
                            lb.ForeColor = MauChuDam;
                            lb.Margin = new Padding(0, 0, 6, 0);
                        }
                        else                // "Chức vụ"
                        {
                            lb.Font = FontNhan;
                            lb.ForeColor = MauChuNhan;
                            lb.Margin = new Padding(12, 0, 6, 0);
                        }
                        break;

                    case Krypton.Toolkit.KryptonTextBox tb:
                        tb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                        tb.Margin = new Padding(0, 7, 0, 7);   // khe dọc giữa các ô ~14px (trước 6px)
                        tb.StateCommon.Back.Color1 = Color.White;
                        KieuOKrypton(tb.StateCommon.Border, tb.StateActive.Border);
                        tb.StateCommon.Content.Font = FontO;
                        tb.StateCommon.Content.Color1 = MauChuDam;
                        // Form13 khóa ô của dòng sau khi dòng trước còn trống: ô bị khóa phải nhìn rõ là "đang khóa"
                        tb.StateDisabled.Back.Color1 = Color.FromArgb(248, 250, 252);
                        tb.StateDisabled.Border.Color1 = Color.FromArgb(226, 232, 240);
                        tb.StateDisabled.Border.Color2 = Color.FromArgb(226, 232, 240);
                        tb.StateDisabled.Content.Color1 = Color.FromArgb(148, 163, 184);
                        break;
                }
            }
        }

        // ===================== HÀNG THÔNG BÁO + NÚT CẬP NHẬT =====================
        private static void DinhChanTrang(Form f)
        {
            var t8 = Tim<TableLayoutPanel>(f, "tableLayoutPanel8");
            if (t8 != null)
            {
                t8.BackColor = MauNen;
                t8.Margin = new Padding(0);
                t8.ColumnStyles.Clear();
                t8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                t8.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            }

            var nut = Tim<Krypton.Toolkit.KryptonButton>(f, "kryptonButton1_Btn_Capnhat");
            if (nut != null)
            {
                KieuNut(nut, NutTimDam);
                nut.Anchor = AnchorStyles.Right;
                nut.Size = new Size(184, 34);
                nut.Margin = new Padding(0, 0, 4, 0);
            }

            // Dòng thông báo (chữ + màu do logic của form điền: đang lưu / thành công / lỗi)
            var tb = Tim<Label>(f, "label13");
            if (tb != null)
            {
                tb.Font = FontNhan;
                tb.BackColor = MauNen;
                tb.ForeColor = MauChuNhan;
                tb.Anchor = AnchorStyles.Left;
                tb.Margin = new Padding(6, 0, 6, 0);
            }
        }

        // ===================== LƯỚI =====================
        // Bảng nằm gọn trong 1 thẻ có KHUNG BO TRÒN liền mạch (không cắt góc bằng Region): thẻ vẽ khung + dải tiêu đề cùng màu tiêu đề bảng.
        private static void BocLuoi(Form f)
        {
            var luoi = Tim<DataGridView>(f, "kryptonDataGridView1");
            if (luoi == null || luoi.Parent is not Control cha) return;

            luoi.BorderStyle = BorderStyle.None;
            luoi.Margin = new Padding(0);
            const int gap = DemKhungBang;
            var the = new TheBoGocF4
            {
                Name = "the_kryptonDataGridView1",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 10),        // khe 10px giữa thẻ lưới và thẻ nhập
                TieuDe = "Danh sách chỉ huy hiện tại",
                DemKhung = gap,
                Padding = new Padding(12 + gap, 36 + gap, 12 + gap, 10 + gap),
                LuoiBang = luoi
            };
            cha.SuspendLayout();
            try
            {
                if (cha is TableLayoutPanel t)
                {
                    var vt = t.GetPositionFromControl(luoi);   // giữ đúng ô (cột, hàng) của lưới
                    t.Controls.Remove(luoi);
                    t.Controls.Add(the, vt.Column, vt.Row);
                }
                else
                {
                    cha.Controls.Remove(luoi);
                    cha.Controls.Add(the);
                }
                the.Controls.Add(luoi);
                luoi.Dock = DockStyle.Fill;
                // Form13.DinhDangGrid đặt chiều cao / màu tiêu đề bảng lúc chạy -> thẻ vẽ lại để dải tiêu đề luôn khớp màu
                luoi.ColumnHeadersHeightChanged += (s, e) => the.Invalidate();
                luoi.ColumnHeadersDefaultCellStyleChanged += (s, e) => the.Invalidate();
                luoi.DataBindingComplete += (s, e) => the.Invalidate();
            }
            finally
            {
                cha.ResumeLayout(true);
            }
        }

        /// <summary>
        /// Style lưới giống 2 bảng của Form4: tiêu đề tím nhạt PHẲNG (Krypton mặc định vẽ chuyển sắc + viền hở ở ô đầu),
        /// chữ tiêu đề tím đậm, đường kẻ dòng nhạt, dòng chọn nền xanh rất nhạt.
        /// Chỉ đổi MÀU / KÍCH THƯỚC; cột, FillWeight, HeaderText, căn lề... vẫn do Form13.DinhDangGrid quyết định.
        /// Phải gọi ở CUỐI DinhDangGrid (hàm đó đặt lại màu tiêu đề / dòng mỗi lần nạp dữ liệu).
        /// </summary>
        public static void StyleLuoi(DataGridView dgv)
        {
            dgv.RowTemplate.Height = CaoDongBang;
            foreach (DataGridViewRow row in dgv.Rows) row.Height = CaoDongBang;   // dòng đã nạp không ăn theo RowTemplate mới
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgv.AllowUserToResizeRows = false;
            dgv.ColumnHeadersHeight = CaoDauBang;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            dgv.GridColor = Color.FromArgb(235, 238, 243);
            dgv.RowsDefaultCellStyle.BackColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
            dgv.DefaultCellStyle.ForeColor = MauChuDam;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(239, 246, 255);
            dgv.DefaultCellStyle.SelectionForeColor = MauChuDam;
            dgv.BackgroundColor = Color.White;       // vùng trống dưới dòng cuối: trắng liền với thẻ, không lộ nền xám
            dgv.Margin = new Padding(0);
            dgv.ColumnHeadersDefaultCellStyle.BackColor = MauTieuDeBang;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = MauChuTieuDeBang;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = MauTieuDeBang;
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = MauChuTieuDeBang;
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            if (dgv is not Krypton.Toolkit.KryptonDataGridView k) return;
            k.GridStyles.Style = Krypton.Toolkit.DataGridViewStyle.List;
            k.StateCommon.Background.Color1 = Color.White;
            k.StateCommon.HeaderColumn.Content.Padding = new Padding(6, 8, 6, 8);
            k.StateCommon.HeaderColumn.Content.Font = FontLuoiTieuDe;
            k.StateCommon.HeaderColumn.Content.Color1 = MauChuTieuDeBang;
            k.StateCommon.DataCell.Content.Font = FontLuoiO;
            k.StateCommon.DataCell.Content.Color1 = MauChuDam;
            k.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            k.StateCommon.DataCell.Border.Color1 = Color.FromArgb(235, 238, 243);
            k.StateCommon.DataCell.Border.Width = 1;
            k.StateCommon.HeaderRow.Back.Color1 = Color.FromArgb(250, 248, 255);
            k.StateCommon.HeaderRow.Back.Color2 = Color.FromArgb(250, 248, 255);
            k.StateCommon.HeaderRow.Border.Color1 = Color.FromArgb(237, 233, 254);
            k.StateSelected.DataCell.Back.Color1 = Color.FromArgb(239, 246, 255);
            k.StateSelected.DataCell.Back.Color2 = Color.FromArgb(239, 246, 255);
            k.StateSelected.DataCell.Content.Color1 = MauChuDam;

            // Hàng tiêu đề: nền phẳng + viền cùng màu nền => khung bo tròn + tiêu đề hợp thành 1 đường viền liền mạch
            TieuDePhang(k.StateCommon.HeaderColumn.Back, k.StateCommon.HeaderColumn.Border);
            TieuDePhang(k.StateTracking.HeaderColumn.Back, k.StateTracking.HeaderColumn.Border);   // rê chuột lên tiêu đề cũng không đổi sang chuyển sắc
            TieuDePhang(k.StatePressed.HeaderColumn.Back, k.StatePressed.HeaderColumn.Border);
        }

        private static void TieuDePhang(Krypton.Toolkit.PaletteBack back, Krypton.Toolkit.PaletteBorder vien)
        {
            back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            back.Color1 = MauTieuDeBang; back.Color2 = MauTieuDeBang;
            vien.Color1 = MauTieuDeBang; vien.Color2 = MauTieuDeBang;
        }

        // ===================== TIỆN ÍCH TRÌNH BÀY =====================
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
            // Nút đang giữ focus / nút mặc định: Krypton lấy nền + viền từ theme (thường trắng) nhưng chữ lấy từ trạng thái của nút
            // => dễ bị chữ trắng trên nền trắng. Ép giống trạng thái thường (đúng cách Form4 đã làm).
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