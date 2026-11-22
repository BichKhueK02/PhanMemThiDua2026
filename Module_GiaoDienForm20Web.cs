using System.Diagnostics;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CHO Form20_DonVi (Quản lý danh sách đơn vị trực thuộc) — CHỈ TRÌNH BÀY, KHÔNG CÓ LOGIC NGHIỆP VỤ
    //  Cùng ngôn ngữ thiết kế với Form4 (Module_GiaoDienForm4Web): nền trang xám nhạt, thẻ trắng bo góc có tiêu đề,
    //  nút theme tím nhạt, ô nhập bo góc viền tím khi đang gõ, bảng có khung bo tròn liền mạch + tiêu đề tím nhạt.
    //
    //  - Lớp static độc lập, không tạo .resx, KHÔNG cần sửa .csproj, KHÔNG sửa Designer.
    //  - Dùng lại TheBoGocF4 (thẻ bo góc) của Module_GiaoDienForm4Web.cs, chỉ dùng các thuộc tính
    //    Name / Dock / Margin / TieuDe / Padding / DemKhung / LuoiBang giống hệt Module_GiaoDienForm10Web.
    //  - Tìm control theo Name (không phụ thuộc private) => không đổi tên/kiểu/sự kiện của control nào,
    //    CellPainting vẽ icon đồng hồ, CellClick, AcceptButton... của form vẫn chạy như cũ.
    //  - Mọi bước bọc try/catch: lỗi trình bày không bao giờ làm form không mở được.
    //
    //  Dùng trong Form20_DonVi:
    //     constructor, ngay SAU InitializeComponent():   Module_GiaoDienForm20Web.ApDung(this);
    // =====================================================================================
    internal static class Module_GiaoDienForm20Web
    {
        // ---------- Bảng màu / font: lấy đúng giá trị của Form4 / Form10 ----------
        private static readonly Color MauNen = Color.FromArgb(244, 246, 250);
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);
        private static readonly Color MauChuDam = Color.FromArgb(30, 41, 59);
        private static readonly Color MauVienO = Color.FromArgb(203, 213, 225);
        private static readonly Color MauVienChon = Color.FromArgb(139, 92, 246);
        private static readonly Color MauTieuDeBang = Color.FromArgb(238, 232, 255);
        private static readonly Color MauChuTieuDeBang = Color.FromArgb(75, 20, 130);
        private static readonly Font FontTieuDe = new Font("Segoe UI Semibold", 13F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNhan = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNhanDam = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontO = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point);
        // Font lưới: giữ đúng font hệ thống mà Form10 / Form13 vẫn dùng cho lưới
        private static readonly Font FontLuoiTieuDe = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Bold);
        private static readonly Font FontLuoiO = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Regular);
        // Khoảng đệm giữa khung bo tròn và bảng (px thiết kế): đủ để 4 góc vuông của bảng nằm lọt trong cung bo
        private const int DemKhungBang = 5;
        private const int CaoTieuDeTrang = 44;      // hàng chữ "QUẢN LÝ DANH SÁCH ĐƠN VỊ"
        private const int CaoTheNhap = 178;         // thẻ nhập: tổng cộng + ô tên + hàng nút
        // (Nền, Hover, Nhấn, Viền, Chữ) — theme tím nhạt của Form4
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutTim =
            (Color.FromArgb(237, 233, 254), Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253), Color.FromArgb(196, 181, 253), Color.FromArgb(109, 40, 217));
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
                An(() => DinhNhap(f), "Nhap");
                An(() => BocLuoi(f), "KhungLuoi");
                An(() =>
                {
                    var luoi = Tim<DataGridView>(f, "kryptonDataGridView1_DanhSach_DonVi");
                    if (luoi != null) StyleLuoi(luoi);
                }, "StyleLuoi");
                An(() => f.ClientSize = new Size(760, 560), "KichThuoc");
            }
            finally
            {
                f.ResumeLayout(true);
            }
        }
        private static void An(Action viec, string ten)
        {
            try { viec(); }
            catch (Exception ex) { Debug.WriteLine($"[Module_GiaoDienForm20Web.{ten}] {ex.Message}"); }
        }
        private static T? Tim<T>(Control goc, string ten) where T : Control
            => goc.Controls.Find(ten, true).OfType<T>().FirstOrDefault();
        // ===================== BỐ CỤC TỔNG =====================
        // Bản gốc: 3 hàng chia theo % (tiêu đề 9% | lưới 54% | khối nhập 37%) nên cao thấp theo cửa sổ.
        // Mới: hàng tiêu đề + hàng khối nhập cao cố định, lưới ăn hết phần còn lại.
        private static void DinhBoCuc(Form f)
        {
            var t1 = Tim<TableLayoutPanel>(f, "tableLayoutPanel1");
            if (t1 != null)
            {
                t1.BackColor = MauNen;
                t1.Margin = new Padding(0);
                t1.Padding = new Padding(12, 10, 12, 10);
                t1.RowStyles.Clear();
                t1.RowStyles.Add(new RowStyle(SizeType.Absolute, CaoTieuDeTrang));
                t1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                t1.RowStyles.Add(new RowStyle(SizeType.Absolute, CaoTheNhap));
            }
            // Tiêu đề trang: bản gốc chữ xanh lá căn giữa -> chữ tím đậm căn trái như tiêu đề trang web
            var tieuDe = Tim<Label>(f, "label1");
            if (tieuDe != null)
            {
                tieuDe.Font = FontTieuDe;
                tieuDe.ForeColor = MauChuTieuDeBang;
                tieuDe.Anchor = AnchorStyles.Left;
                tieuDe.Margin = new Padding(4, 0, 0, 0);
            }
        }
        // ===================== THẺ NHẬP LIỆU =====================
        private static void BocTheNhap(Form f)
        {
            var t2 = Tim<TableLayoutPanel>(f, "tableLayoutPanel2");
            if (t2 == null) return;
            var the = ThayBangThe(t2, "Thêm / sửa đơn vị trực thuộc", new Padding(12, 36, 12, 10), new Padding(0));   // 36 = vùng tiêu đề thẻ
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
        // ===================== NỘI DUNG THẺ NHẬP: TỔNG CỘNG | TÊN ĐƠN VỊ | XÓA - SỬA - THÊM =====================
        private static void DinhNhap(Form f)
        {
            var t3 = Tim<TableLayoutPanel>(f, "tableLayoutPanel3");
            if (t3 != null)
            {
                t3.Margin = new Padding(0);
                t3.BackColor = Color.White;
                t3.RowStyles.Clear();
                t3.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));     // "Tổng cộng đơn vị trực thuộc ...: n đơn vị"
                t3.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));     // Tên đơn vị + ô nhập
                t3.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));     // hàng nút
            }
            // Dòng tổng cộng (chữ do logic của form điền)
            var tong = Tim<Label>(f, "label_tongCongDonVi");
            if (tong != null)
            {
                tong.Font = FontNhan;
                tong.ForeColor = MauChuNhan;
                tong.Anchor = AnchorStyles.Left;
                tong.Margin = new Padding(2, 0, 0, 0);
            }
            // Tên đơn vị | ô nhập
            DatCot(f, "tableLayoutPanel4", (SizeType.Absolute, 96F), (SizeType.Percent, 100F));
            var nhan = Tim<Label>(f, "label2");
            DinhNhan(nhan, true);
            if (nhan != null) { nhan.Font = FontNhanDam; nhan.ForeColor = MauChuDam; }
            var o = Tim<Krypton.Toolkit.KryptonTextBox>(f, "textBox_TenDonVi");
            if (o != null)
            {
                o.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                o.Margin = new Padding(0, 3, 0, 3);
                o.StateCommon.Back.Color1 = Color.White;
                KieuOKrypton(o.StateCommon.Border, o.StateActive.Border);
                o.StateCommon.Content.Font = FontO;
                o.StateCommon.Content.Color1 = MauChuDam;
            }
            // Xóa | Sửa | Thêm đơn vị: 3 nút lấp đầy ô, cách nhau 12px; "Thêm" là nút chính
            DatCot(f, "tableLayoutPanel5", (SizeType.Percent, 33.34F), (SizeType.Percent, 33.33F), (SizeType.Percent, 33.33F));
            NutTrongO(f, "kryptonButton1_Xoa", NutTim, new Padding(0, 0, 6, 0));
            NutTrongO(f, "kryptonButton1_Sua", NutTim, new Padding(6, 0, 6, 0));
            NutTrongO(f, "kryptonButton1_Them", NutTimDam, new Padding(6, 0, 0, 0));
        }
        // ===================== LƯỚI =====================
        // Bảng nằm gọn trong 1 thẻ có KHUNG BO TRÒN liền mạch (không cắt góc bằng Region): thẻ vẽ khung + dải tiêu đề cùng màu tiêu đề bảng.
        private static void BocLuoi(Form f)
        {
            var luoi = Tim<DataGridView>(f, "kryptonDataGridView1_DanhSach_DonVi");
            if (luoi == null || luoi.Parent is not Control cha) return;
            luoi.BorderStyle = BorderStyle.None;
            luoi.Margin = new Padding(0);
            const int gap = DemKhungBang;
            var the = new TheBoGocF4
            {
                Name = "the_kryptonDataGridView1_DanhSach_DonVi",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 10),        // khe 10px giữa thẻ lưới và thẻ nhập
                TieuDe = "Danh sách đơn vị trực thuộc",
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
                // Code gốc đặt chiều cao / màu tiêu đề bảng lúc chạy (CauHinhGiaoDienGrid) -> thẻ vẽ lại để dải tiêu đề luôn khớp màu
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
        /// Chỉ đổi MÀU / KÍCH THƯỚC; cột, FillWeight, HeaderText, CellPainting vẽ icon... vẫn do form gốc quyết định.
        /// </summary>
        public static void StyleLuoi(DataGridView dgv)
        {
            dgv.RowHeadersVisible = false;           // đã có cột STT; bỏ cột chọn dòng trống bên trái
            dgv.RowTemplate.Height = 36;
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgv.AllowUserToResizeRows = false;
            dgv.ColumnHeadersHeight = 50;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            dgv.GridColor = Color.FromArgb(235, 238, 243);
            dgv.RowsDefaultCellStyle.BackColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
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
        // Đặt kiểu cột cho 1 bảng bố cục (giữ nguyên số hàng và vị trí các control gốc)
        private static TableLayoutPanel? DatCot(Form f, string ten, params (SizeType Kieu, float Gia)[] cacCot)
        {
            var t = Tim<TableLayoutPanel>(f, ten);
            if (t == null) return null;
            t.Margin = new Padding(0);
            t.BackColor = Color.White;
            if (t.ColumnCount < cacCot.Length) t.ColumnCount = cacCot.Length;
            t.ColumnStyles.Clear();
            foreach (var c in cacCot) t.ColumnStyles.Add(new ColumnStyle(c.Kieu, c.Gia));
            return t;
        }
        private static void DinhNhan(Label? lb, bool dauHang = false)
        {
            if (lb == null) return;
            lb.Font = FontNhan;
            lb.ForeColor = MauChuNhan;
            lb.Anchor = AnchorStyles.Left;
            lb.Margin = new Padding(dauHang ? 0 : 12, 0, 6, 0);
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
        private static void NutTrongO(Form f, string ten, (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) m, Padding margin)
        {
            var b = Tim<Krypton.Toolkit.KryptonButton>(f, ten);
            if (b == null) return;
            KieuNut(b, m);
            b.Anchor = AnchorStyles.Left | AnchorStyles.Right;   // nút lấp đầy chiều ngang ô, cao đều 32
            b.Margin = margin;
            b.Height = 32;
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
            // Nút đang giữ focus / nút mặc định (Form20 đổi AcceptButton giữa "Thêm" và "Sửa/Lưu" liên tục):
            // Krypton lấy nền + viền từ theme (thường trắng) nhưng chữ lấy từ trạng thái của nút => dễ chữ trắng trên nền trắng.
            // Ép giống trạng thái thường (đúng cách Form4 đã làm).
            foreach (var ov in new[] { b.OverrideFocus, b.OverrideDefault })
            {
                ov.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
                ov.Back.Color1 = m.Nen; ov.Back.Color2 = m.Nen;
                ov.Border.Color1 = m.Vien; ov.Border.Color2 = m.Vien;
                ov.Content.ShortText.Color1 = m.Chu;
            }
            // Nút bị khóa: tím rất nhạt, chữ tím nhạt vẫn đọc được
            b.StateDisabled.Back.Color1 = Color.FromArgb(245, 243, 255); b.StateDisabled.Back.Color2 = Color.FromArgb(245, 243, 255);
            b.StateDisabled.Border.Color1 = Color.FromArgb(221, 214, 254); b.StateDisabled.Border.Color2 = Color.FromArgb(221, 214, 254);
            b.StateDisabled.Content.ShortText.Color1 = Color.FromArgb(167, 139, 250);
            b.Cursor = Cursors.Hand;
        }
    }
}