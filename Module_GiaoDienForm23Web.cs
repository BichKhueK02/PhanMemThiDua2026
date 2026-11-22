using System.Diagnostics;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CHO Form23_ThongKeThiDuaTapThe — CHỈ TRÌNH BÀY, KHÔNG CÓ LOGIC NGHIỆP VỤ
    //  Cùng ngôn ngữ thiết kế với Form4 / Form15 / Form34: nền trang xám nhạt, thẻ trắng bo góc, combo bo tròn,
    //  nút theme tím nhạt (nút "Xóa tất cả" màu đỏ nhạt), bảng có khung bo tròn + tiêu đề tím nhạt có vạch phân cách cột.
    //
    //  KHÔNG ĐỔI CODE GỐC: Form23 chỉ thêm đúng 1 dòng ở CUỐI constructor (sau InitToolTips();):
    //        Module_GiaoDienForm23Web.ApDung(this);
    //  - Không sửa Designer, không đổi tên/kiểu/sự kiện của control nào; tìm control theo Name.
    //  - Tiêu đề thẻ "1. Cập nhật phân loại/danh hiệu thi đua <tên đơn vị>" do code gốc đổi động (CapNhatTieuDeGroupBox):
    //    GroupBox gốc được giữ lại (không hiển thị) và thẻ mới tự cập nhật theo mỗi khi chữ của nó thay đổi.
    //  - Code gốc chỉnh lưới SAU khi nạp dữ liệu (chiều cao tiêu đề 82, dòng 62, cột Fill); helper không đụng tới các thông số đó,
    //    chỉ áp lại màu tiêu đề/đường kẻ khi cần.
    //  - Dùng lại TheBoGocF4 và OComboWebF4 trong Module_GiaoDienForm4Web.cs (file đó phải còn trong project).
    //  - Mọi bước bọc try/catch: lỗi trình bày không bao giờ làm form không mở được.
    // =====================================================================================
    internal static class Module_GiaoDienForm23Web
    {
        // ===================== TÙY CHỌN =====================
        private const int DemKhungBang = 5;   // khoảng đệm giữa khung bo tròn và bảng
        private const int CaoHangThe = 96;    // chiều cao hàng 2 thẻ phía dưới (px thiết kế, được nhân theo DPI)
        // ===================== BẢNG MÀU =====================
        private static readonly Color MauNen = Color.FromArgb(244, 246, 250);
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);
        private static readonly Color MauChuDuLieu = Color.FromArgb(30, 41, 59);
        private static readonly Color MauVienThe = Color.FromArgb(226, 232, 240);
        private static readonly Color MauTieuDeBang = Color.FromArgb(238, 232, 255);
        private static readonly Color MauChuTieuDeBang = Color.FromArgb(91, 33, 182);
        private static readonly Color MauVachCot = Color.FromArgb(208, 198, 247);
        private static readonly Color MauKeBang = Color.FromArgb(235, 238, 243);
        private static readonly Color MauDongChon = Color.FromArgb(245, 241, 255);
        private static readonly Pen ButVachCot = new Pen(MauVachCot, 1f);
        private static readonly Font FontNhan = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontO = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new Font("Segoe UI Semibold", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        private readonly record struct BoMauNut(Color Nen, Color Hover, Color Down, Color Vien, Color Chu);
        private static readonly BoMauNut NutTim = new(
            Color.FromArgb(237, 233, 254), Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253),
            Color.FromArgb(196, 181, 253), Color.FromArgb(109, 40, 217));
        private static readonly BoMauNut NutTimDam = new(
            Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253), Color.FromArgb(167, 139, 250),
            Color.FromArgb(167, 139, 250), Color.FromArgb(91, 33, 182));
        private static readonly BoMauNut NutDo = new(
            Color.FromArgb(254, 242, 242), Color.FromArgb(254, 226, 226), Color.FromArgb(254, 202, 202),
            Color.FromArgb(252, 165, 165), Color.FromArgb(185, 28, 28));
        // ===================== ĐIỂM VÀO =====================
        public static void ApDung(Form f)
        {
            f.SuspendLayout();
            try
            {
                f.BackColor = MauNen;
                An(() => BatDoubleBuffer(f), "DoubleBuffer");
                An(() => DinhBoCuc(f), "BoCuc");
                An(() => BocCacThe(f), "CacThe");
                An(() => DinhTheCapNhat(f), "TheCapNhat");
                An(() => DinhTheDieuHuong(f), "TheDieuHuong");
                An(() => BoTronCacCombo(f), "Combo");
                An(() => BocLuoi(f), "Luoi");
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
            catch (Exception ex) { Debug.WriteLine($"[Module_GiaoDienForm23Web.{ten}] {ex.Message}"); }
        }
        private static T? Tim<T>(Control goc, string ten) where T : Control
            => goc.Controls.Find(ten, true).OfType<T>().FirstOrDefault();
        private static IEnumerable<T> TatCa<T>(Control goc) where T : Control
        {
            foreach (Control c in goc.Controls)
            {
                if (c is T t) yield return t;
                foreach (var con in TatCa<T>(c)) yield return con;
            }
        }
        private static int Px(Control c, int v) => (int)Math.Round(v * c.DeviceDpi / 96f);
        private static readonly System.Reflection.PropertyInfo? PropDoubleBuffered =
            typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        // TableLayoutPanel không bật DoubleBuffered mặc định -> chớp nháy khi co giãn; bật 1 lần bằng reflection
        private static void BatDoubleBuffer(Form f)
        {
            if (PropDoubleBuffered == null) return;
            foreach (var ten in new[] { "tableLayoutPanel1", "tableLayoutPanel2", "tableLayoutPanel3", "tableLayoutPanel4" })
            {
                var c = Tim<Control>(f, ten);
                if (c != null) PropDoubleBuffered.SetValue(c, true);
            }
        }
        // ===================== BỐ CỤC TỔNG =====================
        // Trên: thẻ bảng (co giãn)  |  Dưới: 2 thẻ cạnh nhau (cao cố định)
        private static void DinhBoCuc(Form f)
        {
            var t1 = Tim<TableLayoutPanel>(f, "tableLayoutPanel1");
            if (t1 != null)
            {
                t1.BackColor = MauNen;
                t1.Margin = new Padding(0);
                t1.Padding = new Padding(12, 10, 12, 6);
                t1.RowStyles.Clear();
                t1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                t1.RowStyles.Add(new RowStyle(SizeType.Absolute, Px(f, CaoHangThe)));
            }
            var t2 = Tim<TableLayoutPanel>(f, "tableLayoutPanel2");
            if (t2 != null)
            {
                t2.BackColor = MauNen;
                t2.Margin = new Padding(0);
                t2.ColumnStyles.Clear();   // bản gốc còn dư 6 cột Absolute 20 không có control
                t2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
                t2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            }
        }
        // ===================== THẺ =====================
        private static void BocCacThe(Form f)
        {
            var gb1 = Tim<GroupBox>(f, "groupBox1");
            if (gb1 != null) ThayGroupBoxBangThe(gb1, new Padding(0, 6, 6, 0));
            var gb2 = Tim<GroupBox>(f, "groupBox2");
            if (gb2 != null) ThayGroupBoxBangThe(gb2, new Padding(6, 6, 0, 0));
        }
        /// <summary>
        /// Thay GroupBox bằng TheBoGocF4 cùng vị trí, chuyển nguyên các control con sang thẻ.
        /// GroupBox KHÔNG bị hủy: code gốc còn gán groupBox1.Text (tên đơn vị) -> theo dõi TextChanged để thẻ hiện đúng chữ mới.
        /// </summary>
        private static void ThayGroupBoxBangThe(GroupBox gb, Padding margin)
        {
            var cha = gb.Parent;
            if (cha == null) return;
            var the = new TheBoGocF4
            {
                Name = "the_" + gb.Name,
                Dock = DockStyle.Fill,
                Margin = margin,
                TieuDe = gb.Text,
                Padding = new Padding(12, 36, 12, 8)
            };
            gb.TextChanged += (_, _) =>
            {
                if (the.IsDisposed) return;
                the.TieuDe = gb.Text;
                the.Invalidate();
            };
            cha.SuspendLayout();
            the.SuspendLayout();
            try
            {
                var con = gb.Controls.Cast<Control>().ToArray();
                if (cha is TableLayoutPanel tlp)
                {
                    var vt = tlp.GetPositionFromControl(gb);
                    tlp.Controls.Remove(gb);
                    tlp.Controls.Add(the, vt.Column, vt.Row);
                }
                else
                {
                    cha.Controls.Remove(gb);
                    cha.Controls.Add(the);
                }
                foreach (var c in con)
                {
                    gb.Controls.Remove(c);
                    c.Dock = DockStyle.Fill;
                    the.Controls.Add(c);
                }
            }
            finally
            {
                the.ResumeLayout(false);
                cha.ResumeLayout(true);
            }
        }
        // ===================== THẺ 1: CẬP NHẬT PHÂN LOẠI =====================
        private static void DinhTheCapNhat(Form f)
        {
            // Chọn | combo tháng | Chọn loại | combo loại | nút Cập nhật
            var t3 = Tim<TableLayoutPanel>(f, "tableLayoutPanel3");
            if (t3 != null)
            {
                t3.Margin = new Padding(0);
                t3.ColumnStyles.Clear();
                t3.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 0 Chọn
                t3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));      // 1 combo tháng
                t3.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 2 Chọn loại
                t3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));      // 3 combo loại
                t3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Px(f, 140)));   // 4 nút Cập nhật
            }
            DinhNhan(Tim<Label>(f, "label_DuongDan"), dauHang: true);   // Chọn
            DinhNhan(Tim<Label>(f, "label1"));                          // Chọn loại
            foreach (var ten in new[] { "comboBox1_ChonThangCanXuat", "comboBox1_ChonLoai" })
            {
                var cb = Tim<ComboBox>(f, ten);
                if (cb == null) continue;
                cb.Font = FontO;                         // đặt TRƯỚC khi gắn ô bo tròn (nó chép font của ComboBox)
                cb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                cb.Margin = new Padding(0);
            }
            NutTrongO(f, "kryptonButton1_CapNhat", NutTimDam, new Padding(14, 0, 0, 0));
        }
        // ===================== THẺ 2: LỆNH ĐIỀU HƯỚNG =====================
        private static void DinhTheDieuHuong(Form f)
        {
            var t4 = Tim<TableLayoutPanel>(f, "tableLayoutPanel4");
            if (t4 != null)
            {
                t4.Margin = new Padding(0);
                t4.ColumnStyles.Clear();
                t4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                t4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            }
            NutTrongO(f, "kryptonButton1_XoaTatCaDataPhanLoaiTapThe", NutDo, new Padding(0, 0, 6, 0));   // Xóa tất cả
            NutTrongO(f, "kryptonButton_XuatTepExcel", NutTim, new Padding(6, 0, 0, 0));                   // Xuất tệp excel
        }
        // ===================== COMBO BO TRÒN =====================
        private static void BoTronCacCombo(Form f)
        {
            foreach (var cb in TatCa<ComboBox>(f).ToList())
                if (cb.GetType() == typeof(ComboBox))
                    OComboWebF4.Gan(cb);
        }
        // ===================== LƯỚI =====================
        private static void BocLuoi(Form f)
        {
            var luoi = Tim<DataGridView>(f, "kryptonDataGridView1");
            if (luoi == null || luoi.Parent is not Control cha) return;
            luoi.BorderStyle = BorderStyle.None;
            luoi.Margin = new Padding(0);
            luoi.RowHeadersVisible = false;    // bảng chỉ có 1 dòng dữ liệu: cột số thứ tự trống là thừa
            var the = new TheBoGocF4
            {
                Name = "the_kryptonDataGridView1",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 6),
                TieuDe = "Bảng thống kê phân loại thi đua tập thể",
                DemKhung = DemKhungBang,
                Padding = new Padding(12 + DemKhungBang, 36 + DemKhungBang, 12 + DemKhungBang, 10 + DemKhungBang),
                LuoiBang = luoi
            };
            cha.SuspendLayout();
            try
            {
                if (cha is TableLayoutPanel tlp)
                {
                    var vt = tlp.GetPositionFromControl(luoi);
                    tlp.Controls.Remove(luoi);
                    tlp.Controls.Add(the, vt.Column, vt.Row);
                }
                else
                {
                    cha.Controls.Remove(luoi);
                    cha.Controls.Add(the);
                }
                the.Controls.Add(luoi);
                luoi.Dock = DockStyle.Fill;
            }
            finally
            {
                cha.ResumeLayout(true);
            }
            _ = new BoDinhDangLuoi(f, luoi, the);
        }
        private sealed class BoDinhDangLuoi
        {
            private readonly DataGridView _luoi;
            private readonly Krypton.Toolkit.KryptonDataGridView? _k;
            private readonly TheBoGocF4 _the;
            private bool _dangDap;
            public BoDinhDangLuoi(Form f, DataGridView luoi, TheBoGocF4 the)
            {
                _luoi = luoi;
                _k = luoi as Krypton.Toolkit.KryptonDataGridView;
                _the = the;
                // Nạp lại dữ liệu (DataSource mới) có thể làm Krypton áp lại palette -> kiểm tra rẻ rồi áp lại nếu cần
                _luoi.ColumnAdded += (_, _) => { if (!DaDungKieu()) DapKieu(); };
                _luoi.Layout += (_, _) => { if (!DaDungKieu()) DapKieu(); };
                _luoi.DataBindingComplete += (_, _) => { if (!DaDungKieu()) DapKieu(); };
                f.Shown += (_, _) => { if (!DaDungKieu()) DapKieu(); };
                _luoi.ColumnHeadersHeightChanged += (_, _) => _the.Invalidate();
                // Vẽ chồng SAU khi lưới tự vẽ xong => không can thiệp cách lưới xếp chữ tiêu đề
                _luoi.Paint += VeTieuDe;
                DapKieu();
            }
            private void DapKieu()
            {
                if (_dangDap || _k == null) return;
                _dangDap = true;
                try
                {
                    StyleKrypton(_k);
                    _the.Invalidate();
                }
                finally { _dangDap = false; }
            }
            private bool DaDungKieu() => _k != null && _k.StateCommon.HeaderColumn.Back.Color1 == MauTieuDeBang;
            private static void StyleKrypton(Krypton.Toolkit.KryptonDataGridView k)
            {
                k.StateCommon.Background.Color1 = Color.White;
                k.StateCommon.DataCell.Back.Color1 = Color.White;
                k.StateCommon.DataCell.Content.Color1 = MauChuDuLieu;
                k.StateCommon.DataCell.Border.Color1 = MauKeBang;
                k.StateCommon.HeaderColumn.Content.Color1 = MauChuTieuDeBang;
                k.StateSelected.DataCell.Back.Color1 = MauDongChon;
                k.StateSelected.DataCell.Back.Color2 = MauDongChon;
                k.StateSelected.DataCell.Content.Color1 = MauChuTieuDeBang;
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
            // Vạch kẻ dọc giữa các cột tiêu đề + đường kẻ dưới hàng tiêu đề
            private void VeTieuDe(object? sender, PaintEventArgs e)
            {
                var g = _luoi;
                if (!g.ColumnHeadersVisible || e.ClipRectangle.Top >= g.ColumnHeadersHeight) return;
                try
                {
                    var gr = e.Graphics;
                    foreach (DataGridViewColumn c in g.Columns)
                    {
                        if (!c.Visible) continue;
                        var r = g.GetCellDisplayRectangle(c.Index, -1, false);
                        if (r.Width <= 0 || r.Height <= 0) continue;
                        gr.DrawLine(ButVachCot, r.Right - 1, r.Top, r.Right - 1, r.Bottom - 1);
                    }
                    int y = g.ColumnHeadersHeight - 1;
                    gr.DrawLine(ButVachCot, 0, y, g.ClientSize.Width, y);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[Module_GiaoDienForm23Web.TieuDe] " + ex.Message);
                }
            }
        }
        // ===================== THANH TRẠNG THÁI (ToolStrip) =====================
        private static void DinhThanhTrangThai(Form f)
        {
            var ts = Tim<ToolStrip>(f, "toolStrip1");
            if (ts == null) return;
            ts.Renderer = new TrangThaiPhang();
            ts.BackColor = Color.White;
            ts.GripStyle = ToolStripGripStyle.Hidden;
            ts.Padding = new Padding(10, 0, 10, 0);
            // toolStripLabel1: code gốc tự đổi màu chữ theo trạng thái kết nối (xanh/đỏ) -> chỉ đổi font
            foreach (var ten in new[] { "toolStripLabel1", "toolStripLabel2", "toolStripLabel3" })
            {
                var it = ts.Items.Find(ten, false).FirstOrDefault();
                if (it == null) continue;
                it.Font = FontNhan;
                if (ten != "toolStripLabel1") it.ForeColor = MauChuNhan;
            }
        }
        private sealed class TrangThaiPhang : ToolStripProfessionalRenderer
        {
            public TrangThaiPhang() { RoundedEdges = false; }
            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using var b = new SolidBrush(Color.White);
                e.Graphics.FillRectangle(b, e.AffectedBounds);
            }
            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                using var p = new Pen(MauVienThe, 1f);
                e.Graphics.DrawLine(p, 0, 0, e.ToolStrip.Width, 0);
            }
        }
        // ===================== TIỆN ÍCH TRÌNH BÀY =====================
        private static void DinhNhan(Label? lb, bool dauHang = false)
        {
            if (lb == null) return;
            lb.Font = FontNhan;                       // bản gốc: chữ nghiêng đỏ
            lb.ForeColor = MauChuNhan;
            lb.AutoSize = true;
            lb.MaximumSize = Size.Empty;              // không giới hạn bề rộng => không rớt dòng
            lb.TextAlign = ContentAlignment.MiddleLeft;
            lb.UseMnemonic = false;
            lb.Anchor = AnchorStyles.Left;
            lb.Margin = new Padding(dauHang ? 0 : 16, 0, 8, 0);
        }
        private static void NutTrongO(Form f, string ten, BoMauNut m, Padding margin)
        {
            var b = Tim<Krypton.Toolkit.KryptonButton>(f, ten);
            if (b == null) return;
            KieuNut(b, m);
            b.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            b.Margin = margin;
            b.Height = 34;
        }
        private static void KieuNut(Krypton.Toolkit.KryptonButton b, BoMauNut m)
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