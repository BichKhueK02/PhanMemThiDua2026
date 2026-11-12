using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
using System.Diagnostics;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CHO Form25_ThongKeTaiKhoanDaSuDung — CHỈ TRÌNH BÀY, KHÔNG CÓ LOGIC NGHIỆP VỤ
    //  Cùng ngôn ngữ thiết kế với Form4 / Form10: nền trang xám nhạt, thẻ trắng bo góc + thanh nhấn xanh ở tiêu đề,
    //  nút theme tím nhạt, bảng có khung bo tròn liền mạch + tiêu đề tím nhạt.
    //
    //  - Dùng lại lớp thẻ TheBoGocF4 trong GiaoDienForm4Web.cs (file đó phải còn trong project).
    //  - Tìm control theo Name, không đổi tên/kiểu/sự kiện của control nào; KHÔNG sửa Designer.
    //  - Mọi bước bọc try/catch: lỗi trình bày không bao giờ làm form không mở được.
    //
    //  Dùng trong Form25:
    //     constructor, ngay SAU InitializeComponent():  GiaoDienForm25Web.ApDung(this);
    //     cuối KhoiTaoGrid(), trước ResumeLayout():     GiaoDienForm25Web.StyleLuoi(dgv);
    // =====================================================================================
    internal static class GiaoDienForm25Web
    {
        // ---------- Bảng màu / font: lấy đúng giá trị của Form4 ----------
        private static readonly Color MauNen = Color.FromArgb(244, 246, 250);
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);
        private static readonly Color MauChuDam = Color.FromArgb(30, 41, 59);
        private static readonly Color MauTieuDeBang = Color.FromArgb(238, 232, 255);
        private static readonly Color MauChuTieuDeBang = Color.FromArgb(75, 20, 130);
        private static readonly Color MauKeBang = Color.FromArgb(235, 238, 243);
        private static readonly Color MauChonDong = Color.FromArgb(239, 246, 255);

        private static readonly Font FontNhan = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontTieuDeTrang = new Font("Segoe UI Semibold", 14F, FontStyle.Regular, GraphicsUnit.Point);

        /// <summary>true = hiện tiêu đề trang (lấy từ Form.Text) ở khoảng trống giữa biểu tượng và nút Đóng.</summary>
        private const bool HienTieuDeTrang = true;

        // Khoảng đệm giữa khung bo tròn và bảng (px thiết kế): đủ để 4 góc vuông của bảng nằm lọt trong cung bo
        private const int DemKhungBang = 5;

        // (Nền, Hover, Nhấn, Viền, Chữ) — theme tím nhạt của Form4
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutTim =
            (Color.FromArgb(237, 233, 254), Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253), Color.FromArgb(196, 181, 253), Color.FromArgb(109, 40, 217));

        // ===================== ĐIỂM VÀO =====================
        public static void ApDung(Form f)
        {
            f.SuspendLayout();
            try
            {
                f.BackColor = MauNen;
                An(() => DinhBoCuc(f), "BoCuc");
                An(() => DinhDauTrang(f), "DauTrang");
                An(() => BocTheLuoi(f), "TheLuoi");
                An(() => DinhThanhTrangThai(f), "TrangThai");
            }
            finally
            {
                f.ResumeLayout(true);
            }
        }

        private static void An(Action viec, string ten)
        {
            try { viec(); }
            catch (Exception ex) { Debug.WriteLine($"[GiaoDienForm25Web.{ten}] {ex.Message}"); }
        }

        private static T? Tim<T>(Control goc, string ten) where T : Control
            => goc.Controls.Find(ten, true).OfType<T>().FirstOrDefault();

        // ===================== BỐ CỤC TỔNG =====================
        private static void DinhBoCuc(Form f)
        {
            // Hàng 0 = đầu trang (cao cố định), hàng 1 = thẻ bảng (ăn hết). Bản gốc chia theo % nên cao thấp theo cửa sổ.
            var t3 = Tim<TableLayoutPanel>(f, "tableLayoutPanel3");
            if (t3 == null) return;
            t3.BackColor = MauNen;
            t3.Margin = new Padding(0);
            t3.Padding = new Padding(12, 10, 12, 8);
            t3.RowStyles.Clear();
            t3.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            t3.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        }

        // ===================== ĐẦU TRANG: biểu tượng | tiêu đề | nút Đóng =====================
        private static void DinhDauTrang(Form f)
        {
            var t4 = Tim<TableLayoutPanel>(f, "tableLayoutPanel4");
            if (t4 == null) return;
            t4.BackColor = MauNen;
            t4.Margin = new Padding(0);
            t4.ColumnStyles.Clear();
            t4.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
            t4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            t4.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));

            var anh = Tim<PictureBox>(f, "pictureBox1");
            if (anh != null)
            {
                anh.Dock = DockStyle.Fill;
                anh.Margin = new Padding(0, 4, 12, 4);
                anh.SizeMode = PictureBoxSizeMode.Zoom;   // trước đây StretchImage -> méo hình
            }

            if (HienTieuDeTrang && t4.Controls.Find("lblTieuDeTrang", false).Length == 0)
            {
                var tieuDe = new Label
                {
                    Name = "lblTieuDeTrang",
                    Text = f.Text,
                    AutoSize = false,
                    AutoEllipsis = true,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = FontTieuDeTrang,
                    ForeColor = MauChuDam,
                    BackColor = MauNen
                };
                t4.Controls.Add(tieuDe, 1, 0);
            }

            var dong = Tim<Krypton.Toolkit.KryptonButton>(f, "kryptonButton_Dong");
            if (dong != null)
            {
                KieuNut(dong, NutTim);
                dong.Anchor = AnchorStyles.Right;
                dong.Margin = new Padding(0);
                dong.Size = new Size(130, 34);
            }
        }

        // ===================== THẺ BẢNG =====================
        // GroupBox (tiêu đề đỏ nghiêng) -> thẻ trắng bo góc; bảng nằm trong khung bo tròn liền mạch (không cắt góc bằng Region).
        private static void BocTheLuoi(Form f)
        {
            var gb = Tim<GroupBox>(f, "groupBox1");
            var luoi = Tim<DataGridView>(f, "kryptonDataGridView1");
            if (gb == null || luoi == null || gb.Parent is not TableLayoutPanel cha) return;

            var vt = cha.GetPositionFromControl(gb);
            const int gap = DemKhungBang;
            var the = new TheBoGocF4
            {
                Name = "the_groupBox1",
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                TieuDe = gb.Text,
                DemKhung = gap,
                Padding = new Padding(12 + gap, 36 + gap, 12 + gap, 10 + gap),
                LuoiBang = luoi
            };

            cha.SuspendLayout();
            the.SuspendLayout();
            try
            {
                cha.Controls.Remove(gb);
                cha.Controls.Add(the, vt.Column, vt.Row);

                // Thẻ vẽ khung theo toạ độ của lưới => lưới phải là con TRỰC TIẾP của thẻ (bản gốc còn bọc thêm 1 bảng bố cục 1 ô)
                luoi.Parent?.Controls.Remove(luoi);
                the.Controls.Add(luoi);
                luoi.Dock = DockStyle.Fill;
                luoi.Margin = new Padding(0);
                luoi.BorderStyle = BorderStyle.None;

                // Màu / chiều cao tiêu đề bảng do code gốc đặt lúc chạy (KhoiTaoGrid) -> thẻ vẽ lại để dải tiêu đề luôn khớp màu
                luoi.ColumnHeadersHeightChanged += (s, e) => the.Invalidate();
                luoi.ColumnHeadersDefaultCellStyleChanged += (s, e) => the.Invalidate();

                gb.Dispose();   // GroupBox + bảng bố cục rỗng bên trong: không còn control nào của code dùng tới
            }
            finally
            {
                the.ResumeLayout(false);
                cha.ResumeLayout(true);
            }
        }

        /// <summary>
        /// Style lưới giống bảng của Form4/Form10: tiêu đề tím nhạt PHẲNG (Krypton mặc định vẽ chuyển sắc + viền hở ở ô đầu),
        /// chữ tiêu đề tím đậm, đường kẻ nhạt, dòng chọn nền xanh rất nhạt, không tô vạch xen kẽ.
        /// Không đụng tới font ô và màu chữ từng dòng (CellFormatting vẫn tô xanh lá / xanh dương / đỏ theo trạng thái tài khoản).
        /// </summary>
        public static void StyleLuoi(DataGridView dgv)
        {
            dgv.ColumnHeadersHeight = 50;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            dgv.GridColor = MauKeBang;
            dgv.BackgroundColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = MauTieuDeBang;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = MauChuTieuDeBang;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = MauTieuDeBang;
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = MauChuTieuDeBang;
            dgv.DefaultCellStyle.SelectionBackColor = MauChonDong;
            dgv.DefaultCellStyle.SelectionForeColor = MauChuDam;

            if (dgv is not Krypton.Toolkit.KryptonDataGridView k) return;
            k.GridStyles.Style = Krypton.Toolkit.DataGridViewStyle.List;
            k.StateCommon.Background.Color1 = Color.White;
            k.StateCommon.HeaderColumn.Content.Color1 = MauChuTieuDeBang;
            k.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            k.StateCommon.DataCell.Border.Color1 = MauKeBang;
            k.StateCommon.DataCell.Border.Width = 1;
            k.StateCommon.HeaderRow.Back.Color1 = Color.FromArgb(250, 248, 255);
            k.StateCommon.HeaderRow.Back.Color2 = Color.FromArgb(250, 248, 255);
            k.StateCommon.HeaderRow.Border.Color1 = Color.FromArgb(237, 233, 254);
            k.StateSelected.DataCell.Back.Color1 = MauChonDong;
            k.StateSelected.DataCell.Back.Color2 = MauChonDong;

            // Hàng tiêu đề: nền phẳng + viền cùng màu nền => khung bo tròn + tiêu đề hợp thành 1 đường viền liền mạch
            TieuDePhang(k.StateCommon.HeaderColumn.Back, k.StateCommon.HeaderColumn.Border);
            TieuDePhang(k.StateTracking.HeaderColumn.Back, k.StateTracking.HeaderColumn.Border);
            TieuDePhang(k.StatePressed.HeaderColumn.Back, k.StatePressed.HeaderColumn.Border);
        }

        private static void TieuDePhang(Krypton.Toolkit.PaletteBack back, Krypton.Toolkit.PaletteBorder vien)
        {
            back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            back.Color1 = MauTieuDeBang; back.Color2 = MauTieuDeBang;
            vien.Color1 = MauTieuDeBang; vien.Color2 = MauTieuDeBang;
        }

        // ===================== THANH TRẠNG THÁI =====================
        private static void DinhThanhTrangThai(Form f)
        {
            var ss = Tim<StatusStrip>(f, "statusStrip1");
            if (ss == null) return;
            ss.BackColor = Color.White;   // bản gốc: xanh lá nhạt
            ss.ForeColor = MauChuNhan;
            ss.SizingGrip = false;
            ss.Padding = new Padding(10, 0, 10, 0);
            foreach (var ten in new[] { "toolStripStatusLabel1", "toolStripStatusLabel2" })
            {
                var it = ss.Items.Find(ten, false).FirstOrDefault();
                if (it != null) { it.Font = FontNhan; it.ForeColor = MauChuNhan; }
            }
        }

        // ===================== NÚT (theme tím nhạt của Form4) =====================
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
            // Nút đang giữ focus / nút mặc định (nút Đóng sẽ là CancelButton): ép giống trạng thái thường để không bị chữ trắng nền trắng
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