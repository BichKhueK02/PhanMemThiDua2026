using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Reflection;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CHO Form27_TyLeQuyDinhE29 (Cập nhật tỷ lệ theo quy định) — CHỈ TRÌNH BÀY, KHÔNG CÓ LOGIC NGHIỆP VỤ
    //  Cùng ngôn ngữ thiết kế với Form4 / Form10 / Form41: nền trang xám nhạt, thẻ trắng bo góc có tiêu đề,
    //  bảng nhập có khung bo tròn liền mạch + dải tiêu đề tím nhạt, nút theme tím, ô nhập bo góc viền tím khi đang gõ.
    //
    //  - Lớp static độc lập, không tạo .resx, KHÔNG cần sửa .csproj, KHÔNG sửa Designer.
    //  - Dùng lại TheBoGocF4 (thẻ bo góc) của Module_GiaoDienForm4Web.cs, chỉ dùng các thuộc tính
    //    Name / Dock / Margin / TieuDe / Padding giống hệt Module_GiaoDienForm10Web.
    //  - Tìm control theo Name (không phụ thuộc private) => không đổi tên/kiểu/sự kiện của control nào.
    //  - Mọi bước bọc try/catch: lỗi trình bày không bao giờ làm form không mở được.
    //
    //  LƯU Ý RIÊNG CỦA Form27: DoiTenGroupBox() (chạy trong Load, sau khi nạp CSDL) vẫn ghi tên trung đoàn vào
    //  groupBox_TyLeTheoQuyDinh.Text. Vì vậy module KHÔNG xóa GroupBox: chỉ gỡ nó khỏi form (không hiển thị) và
    //  theo dõi sự kiện TextChanged để chép chữ đó lên tiêu đề của thẻ => không phải sửa DoiTenGroupBox.
    //
    //  Dùng trong Form27_TyLeQuyDinhE29:
    //     constructor, ngay SAU InitializeComponent():   Module_GiaoDienForm27Web.ApDung(this);
    //     TextBox_Enter / TextBox_Leave:                  Module_GiaoDienForm27Web.VienFocus(tb, true / false);
    // =====================================================================================
    internal static class Module_GiaoDienForm27Web
    {
        // ---------- Bảng màu / font: lấy đúng giá trị của Form4 / Form10 ----------
        private static readonly Color MauNen = Color.FromArgb(244, 246, 250);
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);
        private static readonly Color MauChuDam = Color.FromArgb(30, 41, 59);
        private static readonly Color MauVienO = Color.FromArgb(203, 213, 225);
        private static readonly Color MauVienChon = Color.FromArgb(139, 92, 246);
        private static readonly Color MauTieuDeBang = Color.FromArgb(238, 232, 255);
        private static readonly Color MauChuTieuDeBang = Color.FromArgb(75, 20, 130);
        private static readonly Color MauVienBang = Color.FromArgb(221, 214, 254);
        private static readonly Color MauKeDong = Color.FromArgb(235, 238, 243);
        private static readonly Font FontNhan = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNhanBang = new Font(Module_HeThong.TenFontHeThongDam, 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontO = new Font(Module_HeThong.TenFontHeThongDam, 10F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new Font(Module_HeThong.TenFontHeThongDam, 9F, FontStyle.Regular, GraphicsUnit.Point);
        private const int BanKinhBang = 10;        // bán kính góc bo của khung bảng nhập
        private const int CaoDauBang = 44;         // chiều cao hàng tiêu đề của bảng nhập
        private const int CaoChanTrang = 56;       // hàng chứa thông báo + nút Lưu
        private const int CaoTrangThai = 34;       // thanh trạng thái
        // Tiêu đề thẻ lúc mới mở (trước khi DoiTenGroupBox điền tên trung đoàn); bản gốc hiện chữ thiết kế "Ten groupbox"
        private const string TieuDeMacDinh = "Tỷ lệ theo quy định";
        // (Nền, Hover, Nhấn, Viền, Chữ) — theme tím của Form4: nút chính (tím đậm một nấc)
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutTimDam =
            (Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253), Color.FromArgb(167, 139, 250), Color.FromArgb(167, 139, 250), Color.FromArgb(91, 33, 182));
        // ===================== ĐIỂM VÀO =====================
        // Trong Module_GiaoDienForm27Web.cs
        public static void ApDung(Form f)
        {
            f.SuspendLayout();
            try
            {
                f.BackColor = MauNen;
                An(() => DinhBoCuc(f), "BoCuc");
                An(() => DinhBangNhap(f), "BangNhap");
                An(() => DinhChanTrang(f), "ChanTrang");
                An(() => DinhThanhTrangThai(f), "TrangThai");
                // Tăng chiều rộng form từ 830 lên 870 để cột cuối có không gian thoải mái
                An(() => f.ClientSize = new Size(870, 352), "KichThuoc");
            }
            finally
            {
                f.ResumeLayout(true);
            }
        }
        private static void DinhBangNhap(Form f)
        {
            var luoi = Tim<TableLayoutPanel>(f, "tableLayoutPanel12");
            if (luoi == null) return;
            BatDoubleBuffer(luoi);
            luoi.BackColor = Color.White;
            luoi.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            // Hàng 0 = tiêu đề cột (cao cố định); các hàng còn lại chia đều
            luoi.RowStyles.Clear();
            luoi.RowStyles.Add(new RowStyle(SizeType.Absolute, CaoDauBang));
            for (int i = 1; i < luoi.RowCount; i++)
                luoi.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / (luoi.RowCount - 1)));
            // ĐIỀU CHỈNH TỶ LỆ CỘT: Cột 0 (nhãn) 22%, các cột Loại 1-4 mỗi cột 13.5%, cột cuối (Không phân loại) 22%
            luoi.ColumnStyles.Clear();
            luoi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22.0F)); // Cột nhãn tập thể/cá nhân
            luoi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13.5F)); // Loại 1
            luoi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13.5F)); // Loại 2
            luoi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13.5F)); // Loại 3
            luoi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13.5F)); // Loại 4
            luoi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24.0F)); // Không phân loại (Kéo rộng ra)
            foreach (Control c in luoi.Controls)
            {
                int hang = luoi.GetRow(c);
                int cot = luoi.GetColumn(c);
                switch (c)
                {
                    case Label lb:
                        lb.AutoSize = true;
                        lb.Font = FontNhanBang;
                        lb.BackColor = hang == 0 ? MauTieuDeBang : Color.White;
                        lb.ForeColor = hang == 0 ? MauChuTieuDeBang : MauChuDam;
                        lb.Anchor = cot == 0 ? AnchorStyles.Left : AnchorStyles.None;
                        lb.Margin = cot == 0 ? new Padding(16, 0, 3, 0) : new Padding(3);
                        break;
                    case Krypton.Toolkit.KryptonTextBox tb:
                        tb.Anchor = AnchorStyles.None;
                        tb.Width = 84;
                        tb.Margin = new Padding(3);
                        tb.StateCommon.Back.Color1 = Color.White;
                        KieuOKrypton(tb.StateCommon.Border, tb.StateActive.Border);
                        tb.StateCommon.Content.Font = FontO;
                        tb.StateCommon.Content.Color1 = MauChuDam;
                        break;
                }
            }
            luoi.CellPaint += VeBangNhap;
            luoi.Invalidate();
        }
        private static void An(Action viec, string ten)
        {
            try { viec(); }
            catch (Exception ex) { Debug.WriteLine($"[Module_GiaoDienForm27Web.{ten}] {ex.Message}"); }
        }
        private static T? Tim<T>(Control goc, string ten) where T : Control
            => goc.Controls.Find(ten, true).OfType<T>().FirstOrDefault();
        // ===================== BỐ CỤC TỔNG =====================
        // Bản gốc: Form > [GroupBox (đỏ, in nghiêng) > tableLayoutPanel1 (3 hàng chia theo %, hàng 3 trống) | statusStrip1 dock đáy].
        // Mới: bảng nhập nằm trong 1 thẻ trắng bo góc (tiêu đề = chữ của GroupBox); hàng nút nằm dưới thẻ;
        // thanh trạng thái vào đúng hàng 3 còn trống của tableLayoutPanel1. Cả 3 hàng có chiều cao cố định / ăn hết,
        // không còn co giãn theo %. Các control gốc được GIỮ NGUYÊN, chỉ đổi chỗ chứa.
        private static void DinhBoCuc(Form f)
        {
            var gb = Tim<GroupBox>(f, "groupBox_TyLeTheoQuyDinh");
            var t1 = Tim<TableLayoutPanel>(f, "tableLayoutPanel1");
            var luoi = Tim<TableLayoutPanel>(f, "tableLayoutPanel12");
            var ss = Tim<StatusStrip>(f, "statusStrip1");
            if (gb == null || t1 == null || luoi == null || ss == null) return;
            var the = new TheBoGocF4
            {
                Name = "the_tableLayoutPanel12",
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                TieuDe = TieuDeMacDinh,
                Padding = new Padding(12, 36, 12, 10)      // 36 = vùng tiêu đề thẻ
            };
            t1.SuspendLayout();
            the.SuspendLayout();
            try
            {
                luoi.Dock = DockStyle.Fill;
                luoi.Margin = new Padding(0);
                the.Controls.Add(luoi);                    // luoi rời khỏi ô (0,0) của t1
                t1.Controls.Add(the, 0, 0);                // thẻ vào đúng ô đó
                ss.Dock = DockStyle.Fill;
                ss.Margin = new Padding(0);
                t1.Controls.Add(ss, 0, 2);                 // thanh trạng thái rời form, vào hàng 3 của t1
                t1.BackColor = MauNen;
                t1.Margin = new Padding(0);
                t1.Padding = new Padding(12, 10, 12, 4);
                t1.Dock = DockStyle.Fill;
                t1.RowStyles.Clear();
                t1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                t1.RowStyles.Add(new RowStyle(SizeType.Absolute, CaoChanTrang));
                t1.RowStyles.Add(new RowStyle(SizeType.Absolute, CaoTrangThai));
                // Gỡ GroupBox khỏi form nhưng KHÔNG hủy: DoiTenGroupBox() còn ghi Text / ForeColor / Font vào nó.
                // Mỗi lần Text đổi thì chép lên tiêu đề thẻ.
                gb.Controls.Remove(t1);
                f.Controls.Add(t1);
                f.Controls.Remove(gb);
                gb.TextChanged += (s, e) =>
                {
                    the.TieuDe = gb.Text;
                    the.Invalidate();
                };
                f.Disposed += (s, e) => gb.Dispose();      // GroupBox không còn cha => tự hủy cùng form
            }
            finally
            {
                the.ResumeLayout(false);
                t1.ResumeLayout(true);
            }
        }
        // ===================== BẢNG NHẬP TỶ LỆ =====================
        // Vẽ dải tiêu đề tím nhạt (bo 2 góc trên), đường kẻ giữa các dòng và khung bo tròn liền mạch quanh bảng.
        // Nền bảng và nền thẻ cùng màu trắng nên 4 góc vuông của bảng không lộ ra ngoài khung bo.
        private static void VeBangNhap(object? sender, TableLayoutCellPaintEventArgs e)
        {
            if (sender is not TableLayoutPanel t) return;
            var g = e.Graphics;
            Rectangle r = e.CellBounds;
            int cuoiCot = t.ColumnCount - 1;
            int cuoiHang = t.RowCount - 1;
            if (e.Row == 0)
            {
                var cu = g.SmoothingMode;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var duong = HinhDauBang(r, e.Column == 0, e.Column == cuoiCot, BanKinhBang);
                using var nen = new SolidBrush(MauTieuDeBang);
                g.FillPath(nen, duong);
                g.SmoothingMode = cu;
            }
            else if (e.Row < cuoiHang)
            {
                using var ke = new Pen(MauKeDong);
                g.DrawLine(ke, r.Left, r.Bottom - 1, r.Right, r.Bottom - 1);
            }
            // Khung ngoài: vẽ lại sau MỖI ô để chắc chắn nằm trên cùng, không phụ thuộc thứ tự vẽ ô của TableLayoutPanel
            var sm = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var khung = BoGoc(new Rectangle(0, 0, t.ClientSize.Width - 1, t.ClientSize.Height - 1), BanKinhBang))
            using (var but = new Pen(MauVienBang))
                g.DrawPath(but, khung);
            g.SmoothingMode = sm;
        }
        private static GraphicsPath BoGoc(Rectangle r, int bk)
        {
            var p = new GraphicsPath();
            int d = Math.Max(1, bk * 2);
            p.AddArc(r.Left, r.Top, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
        // Dải tiêu đề: bo góc trên-trái nếu là ô đầu, trên-phải nếu là ô cuối; các ô giữa vuông
        private static GraphicsPath HinhDauBang(Rectangle r, bool boTrai, bool boPhai, int bk)
        {
            var p = new GraphicsPath();
            int d = bk * 2;
            if (boTrai) p.AddArc(r.Left, r.Top, d, d, 180, 90);
            else p.AddLine(r.Left, r.Top, r.Left + 1, r.Top);
            if (boPhai) p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            else p.AddLine(r.Right - 1, r.Top, r.Right, r.Top);
            p.AddLine(r.Right, r.Bottom, r.Left, r.Bottom);
            p.CloseFigure();
            return p;
        }
        private static void BatDoubleBuffer(Control c)
        {
            try
            {
                typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(c, true);
            }
            catch { /* không quan trọng: chỉ để bớt nháy khi vẽ lại */ }
        }
        // ===================== HÀNG NÚT: THÔNG BÁO + LƯU =====================
        private static void DinhChanTrang(Form f)
        {
            var t2 = Tim<TableLayoutPanel>(f, "tableLayoutPanel2");
            if (t2 != null)
            {
                t2.BackColor = MauNen;
                t2.Margin = new Padding(0);
                t2.ColumnStyles.Clear();
                t2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                t2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            }
            var nut = Tim<Krypton.Toolkit.KryptonButton>(f, "kryptonButton_LuuTyLeBCHE09");
            if (nut != null)
            {
                KieuNut(nut, NutTimDam);
                nut.Anchor = AnchorStyles.Right;
                nut.Size = new Size(184, 34);
                nut.Margin = new Padding(0, 0, 4, 0);
            }
            // Dòng thông báo (chữ + màu do logic của form điền)
            var tb = Tim<Label>(f, "label1_ThongBao");
            if (tb != null)
            {
                tb.Font = FontNhan;
                tb.BackColor = MauNen;
                tb.ForeColor = MauChuNhan;
                tb.Anchor = AnchorStyles.Left;
                tb.Margin = new Padding(6, 0, 6, 0);
                tb.MaximumSize = new Size(600, 0);
            }
        }
        // ===================== THANH TRẠNG THÁI =====================
        private static void DinhThanhTrangThai(Form f)
        {
            var ss = Tim<StatusStrip>(f, "statusStrip1");
            if (ss == null) return;
            ss.BackColor = Color.White;
            ss.ForeColor = MauChuNhan;
            ss.SizingGrip = false;
            ss.Padding = new Padding(10, 0, 10, 0);
            foreach (var ten in new[] { "toolStripStatusLabel1", "toolStripStatusLabel2" })
            {
                var it = ss.Items.Find(ten, false).FirstOrDefault();
                if (it == null) continue;
                it.Font = FontNhan;
                it.ForeColor = MauChuNhan;
            }
        }
        // ===================== Ô NHẬP: VIỀN KHI ĐANG GÕ =====================
        /// <summary>
        /// Viền ô nhập khi vào / rời ô. Gọi từ TextBox_Enter / TextBox_Leave của Form27 để màu viền đồng bộ theme tím
        /// (bản gốc tô xanh dương và đổi góc bo 8 -> 4 nên bị "nhảy" hình).
        /// </summary>
        public static void VienFocus(Krypton.Toolkit.KryptonTextBox tb, bool dangNhap)
        {
            var v = tb.StateCommon.Border;
            v.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            v.Rounding = 8F;
            v.Width = dangNhap ? 2 : 1;
            Color mau = dangNhap ? MauVienChon : MauVienO;
            v.Color1 = mau;
            v.Color2 = mau;
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
            // Nút đang giữ focus / nút mặc định: ép giống trạng thái thường để không bị chữ trắng trên nền trắng (cách Form4 đã làm)
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