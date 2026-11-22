using System.Drawing.Drawing2D;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN HIỆN ĐẠI CHO Form16_LamMoi — CHỈ TRÌNH BÀY, KHÔNG CÓ LOGIC NGHIỆP VỤ
    //  - Thẻ trắng bo 14px + viền mảnh, tiêu đề đậm; "Giữ nguyên" là chip bật/tắt (xanh lá khi bật);
    //    danh sách đơn vị tự vẽ ô tích bo tròn tím; nút chính đặc màu tím, nằm giữa.
    //  - Không đổi tên/kiểu/sự kiện control => không sửa Designer. Gọi 1 dòng sau InitializeComponent():
    //        Module_GiaoDienForm16Web.ApDung(this);
    // =====================================================================================
    internal static class Module_GiaoDienForm16Web
    {
        private static readonly Color MauNen = Color.FromArgb(243, 244, 248);
        private static readonly Color MauChu = Color.FromArgb(15, 23, 42);
        private static readonly Color MauChuPhu = Color.FromArgb(100, 116, 139);
        private static readonly Color MauTim = Color.FromArgb(124, 58, 237);
        private static readonly Color MauVien = Color.FromArgb(226, 232, 240);
        private static readonly Color XanhNen = Color.FromArgb(220, 252, 231);
        private static readonly Color XanhVien = Color.FromArgb(134, 239, 172);
        private static readonly Color XanhChu = Color.FromArgb(21, 128, 61);
        private static readonly Font FontNhan = new Font("Segoe UI Semibold", 11F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontChip = new Font("Segoe UI Semibold", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontDong = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontPhu = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new Font("Segoe UI Semibold", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
        public static void ApDung(Form f)
        {
            f.SuspendLayout();
            try
            {
                f.BackColor = MauNen;
                f.ClientSize = new Size(f.LogicalToDeviceUnits(600), f.LogicalToDeviceUnits(372));
                var t1 = Tim<TableLayoutPanel>(f, "tableLayoutPanel1");
                if (t1 != null)
                {
                    t1.BackColor = MauNen;
                    t1.Padding = new Padding(14, 14, 14, 10);
                    t1.ColumnStyles.Clear();
                    t1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                    t1.RowStyles.Clear();
                    t1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                    t1.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
                }
                var t4 = Tim<TableLayoutPanel>(f, "tableLayoutPanel4");
                if (t4 != null)
                {
                    t4.BackColor = MauNen;
                    t4.Margin = new Padding(0);
                    t4.ColumnStyles.Clear();
                    t4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
                    t4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
                    t4.RowStyles.Clear();
                    t4.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                }
                BocThe(Tim<GroupBox>(f, "groupBox1"), "Phân loại giữ lại", 0, new Padding(0, 0, 7, 0), 14);
                var the2 = BocThe(Tim<GroupBox>(f, "groupBox2"), "Đơn vị áp dụng", 1, new Padding(7, 0, 0, 0), 40);
                DinhKhoiPhanLoai(f);
                var clb = Tim<CheckedListBox>(f, "checkedListBox1_ChonDonViDeReset");
                if (clb != null && the2 != null) DinhDanhSachDonVi(clb, the2);
                DinhNut(f);
            }
            finally { f.ResumeLayout(true); }
        }
        private static T? Tim<T>(Control goc, string ten) where T : Control
            => goc.Controls.Find(ten, true).OfType<T>().FirstOrDefault();
        // GroupBox -> thẻ bo góc (GroupBox gốc giữ ẩn trong thẻ)
        private static TheF16? BocThe(GroupBox? gb, string tieuDe, int cot, Padding margin, int demDuoi)
        {
            var cha = gb?.Parent as TableLayoutPanel;
            if (gb == null || cha == null || gb.Controls.Count == 0) return null;
            var noiDung = gb.Controls[0];
            var the = new TheF16 { Dock = DockStyle.Fill, TieuDe = tieuDe, Margin = margin, Padding = new Padding(14, 46, 14, demDuoi) };
            cha.SuspendLayout();
            try
            {
                cha.Controls.Remove(gb);
                cha.Controls.Add(the, cot, 0);
                gb.Visible = false;
                gb.Dock = DockStyle.None;
                the.Controls.Add(noiDung);
                noiDung.Dock = DockStyle.Fill;
                the.Controls.Add(gb);
            }
            finally { cha.ResumeLayout(true); }
            return the;
        }
        // Thẻ 1: 5 hàng "Loại x | chip Giữ nguyên"
        private static void DinhKhoiPhanLoai(Form f)
        {
            var t3 = Tim<TableLayoutPanel>(f, "tableLayoutPanel3");
            if (t3 != null)
            {
                t3.BackColor = Color.White;
                t3.Margin = new Padding(0);
                t3.ColumnStyles.Clear();
                t3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72F));
                t3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                t3.RowStyles.Clear();
                t3.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            }
            var pic = Tim<PictureBox>(f, "pictureBox1");
            if (pic != null)
            {
                pic.Dock = DockStyle.Fill;
                pic.SizeMode = PictureBoxSizeMode.Zoom;
                pic.Margin = new Padding(0, 0, 8, 0);
                pic.BackColor = Color.White;
            }
            var t2 = Tim<TableLayoutPanel>(f, "tableLayoutPanel2");
            if (t2 != null)
            {
                t2.BackColor = Color.White;
                t2.Margin = new Padding(0);
                t2.ColumnStyles.Clear();
                t2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46F));
                t2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54F));
            }
            foreach (var ten in new[] { "label1", "label2", "label3", "label4", "label5" })
            {
                var l = Tim<Label>(f, ten);
                if (l == null) continue;
                l.Font = FontNhan;       // màu do Form16 điều khiển
                l.Anchor = AnchorStyles.Left;
                l.Margin = new Padding(4, 0, 0, 0);
            }
            foreach (var ten in new[] { "checkBox1_GuiNguyenLoai1", "checkBox1_GuiNguyenLoai2", "checkBox1_GuiNguyenLoai3",
                                        "checkBox1_GuiNguyenLoai4", "checkBox1_GuiNguyenKhongPhanLoai" })
                BienThanhChip(Tim<CheckBox>(f, ten));
        }
        // CheckBox -> chip bật/tắt phẳng: tắt = viền xám; bật = nền xanh nhạt + chữ xanh + dấu ✓
        private static void BienThanhChip(CheckBox? c)
        {
            if (c == null) return;
            c.Appearance = Appearance.Button;
            c.FlatStyle = FlatStyle.Flat;
            c.AutoSize = false;
            c.Size = new Size(c.LogicalToDeviceUnits(112), c.LogicalToDeviceUnits(30));
            c.TextAlign = ContentAlignment.MiddleCenter;
            c.UseVisualStyleBackColor = false;
            c.BackColor = Color.White;
            c.Font = FontChip;
            c.Anchor = AnchorStyles.Left;
            c.Margin = new Padding(0);
            c.Cursor = Cursors.Hand;
            var fa = c.FlatAppearance;
            fa.BorderSize = 1;
            fa.BorderColor = MauVien;
            fa.MouseOverBackColor = Color.FromArgb(241, 245, 249);
            fa.MouseDownBackColor = Color.FromArgb(226, 232, 240);
            fa.CheckedBackColor = XanhNen;
            void CapNhat()
            {
                c.Text = c.Checked ? "✓ Giữ nguyên" : "Giữ nguyên";
                c.ForeColor = c.Checked ? XanhChu : MauChuPhu;
                fa.BorderColor = c.Checked ? XanhVien : MauVien;
            }
            c.CheckedChanged += (s, e) => CapNhat();
            CapNhat();
        }
        // Thẻ 2: danh sách đơn vị — ô tích bo tròn tím tự vẽ + chân thẻ "Chọn tất cả | n/m đơn vị"
        private static void DinhDanhSachDonVi(CheckedListBox clb, TheF16 the)
        {
            clb.BorderStyle = BorderStyle.None;
            clb.BackColor = Color.White;
            clb.Font = FontDong;
            clb.DrawMode = DrawMode.OwnerDrawFixed;
            clb.ItemHeight = clb.LogicalToDeviceUnits(30);
            clb.IntegralHeight = false;
            clb.CheckOnClick = true;
            var lnk = new Label { AutoSize = true, Text = "Bỏ chọn tất cả", Font = FontChip, ForeColor = MauTim, Cursor = Cursors.Hand, BackColor = Color.White };
            var dem = new Label { AutoSize = true, Text = "", Font = FontPhu, ForeColor = MauChuPhu, BackColor = Color.White };
            the.Controls.Add(lnk);
            the.Controls.Add(dem);
            lnk.BringToFront(); dem.BringToFront();
            void DatViTri()
            {
                int y = the.Height - the.Padding.Bottom + the.LogicalToDeviceUnits(10);
                lnk.Location = new Point(the.Padding.Left, y);
                dem.Location = new Point(the.Width - the.Padding.Right - dem.Width, y + the.LogicalToDeviceUnits(1));
            }
            the.SizeChanged += (s, e) => DatViTri();
            void CapNhatChan()
            {
                int n = clb.Items.Count, ok = clb.CheckedItems.Count;
                string t = $"{ok}/{n} đơn vị";
                if (dem.Text != t) { dem.Text = t; DatViTri(); }
                string l = (n > 0 && ok == n) ? "Bỏ chọn tất cả" : "Chọn tất cả";
                if (lnk.Text != l) lnk.Text = l;
            }
            lnk.Click += (s, e) =>
            {
                bool chon = lnk.Text == "Chọn tất cả";
                clb.BeginUpdate();
                for (int i = 0; i < clb.Items.Count; i++) clb.SetItemChecked(i, chon);
                clb.EndUpdate();
                clb.ClearSelected();
                clb.Invalidate();
                CapNhatChan();
            };
            DatViTri();
            clb.DrawItem += (s, e) =>
            {
                if (e.Index < 0 || s is not CheckedListBox c) return;
                if (e.Index == 0) CapNhatChan();   // danh sách nạp bất đồng bộ sau khi form mở: cập nhật số đếm ngay lần vẽ đầu
                using (var nen = new SolidBrush(c.BackColor)) e.Graphics.FillRectangle(nen, e.Bounds);
                bool ok = c.GetItemChecked(e.Index);
                int o = c.LogicalToDeviceUnits(18);
                var hop = new Rectangle(e.Bounds.Left + 4, e.Bounds.Top + (e.Bounds.Height - o) / 2, o, o);
                var g = e.Graphics;
                var cu = g.SmoothingMode;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = BoGoc(hop, 5))
                {
                    if (ok)
                    {
                        using var fill = new SolidBrush(MauTim);
                        g.FillPath(fill, path);
                        using var but = new Pen(Color.White, Math.Max(2f, o / 8f)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                        g.DrawLines(but, new[]
                        {
                            new PointF(hop.Left + o * 0.24f, hop.Top + o * 0.54f),
                            new PointF(hop.Left + o * 0.43f, hop.Top + o * 0.72f),
                            new PointF(hop.Left + o * 0.77f, hop.Top + o * 0.30f)
                        });
                    }
                    else
                    {
                        using var vien = new Pen(Color.FromArgb(203, 213, 225), 1.5f);
                        g.DrawPath(vien, path);
                    }
                }
                g.SmoothingMode = cu;
                var vung = new Rectangle(hop.Right + 10, e.Bounds.Top, Math.Max(10, e.Bounds.Width - o - 18), e.Bounds.Height);
                TextRenderer.DrawText(g, c.GetItemText(c.Items[e.Index]), c.Font, vung, ok ? MauChu : Color.FromArgb(148, 163, 184),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            };
            clb.ItemCheck += (s, e) =>
            {
                if (!clb.IsHandleCreated) return;
                clb.BeginInvoke(new Action(() =>
                {
                    if (clb.IsDisposed) return;
                    clb.ClearSelected();
                    clb.Invalidate();
                    CapNhatChan();
                }));
            };
        }
        // Nút chính: đặc màu tím, chữ trắng, nằm giữa hàng đáy
        private static void DinhNut(Form f)
        {
            var b = Tim<Krypton.Toolkit.KryptonButton>(f, "btn_ResetPhanLoai");
            if (b == null) return;
            Color nen = MauTim, hover = Color.FromArgb(109, 40, 217), down = Color.FromArgb(91, 33, 182), chu = Color.White;
            b.StateCommon.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            b.StateCommon.Border.Rounding = 10F;
            b.StateCommon.Border.Width = 0;
            b.StateCommon.Content.ShortText.Font = FontNut;
            b.StateCommon.Content.ShortText.Color1 = chu;
            b.StateNormal.Back.Color1 = nen; b.StateNormal.Back.Color2 = nen;
            b.StateNormal.Border.Color1 = nen; b.StateNormal.Border.Color2 = nen;
            b.StateNormal.Content.ShortText.Color1 = chu;
            b.StateTracking.Back.Color1 = hover; b.StateTracking.Back.Color2 = hover;
            b.StateTracking.Border.Color1 = hover; b.StateTracking.Border.Color2 = hover;
            b.StateTracking.Content.ShortText.Color1 = chu;
            b.StatePressed.Back.Color1 = down; b.StatePressed.Back.Color2 = down;
            b.StatePressed.Border.Color1 = down; b.StatePressed.Border.Color2 = down;
            b.StatePressed.Content.ShortText.Color1 = chu;
            foreach (var ov in new[] { b.OverrideFocus, b.OverrideDefault })
            {
                ov.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
                ov.Back.Color1 = nen; ov.Back.Color2 = nen;
                ov.Border.Color1 = nen; ov.Border.Color2 = nen;
                ov.Content.ShortText.Color1 = chu;
            }
            b.StateDisabled.Back.Color1 = Color.FromArgb(221, 214, 254); b.StateDisabled.Back.Color2 = Color.FromArgb(221, 214, 254);
            b.StateDisabled.Border.Color1 = Color.FromArgb(221, 214, 254); b.StateDisabled.Border.Color2 = Color.FromArgb(221, 214, 254);
            b.StateDisabled.Content.ShortText.Color1 = Color.White;
            b.Anchor = AnchorStyles.None;            // nằm giữa ô, không giãn hết chiều ngang
            b.Margin = new Padding(0);
            b.Size = new Size(b.LogicalToDeviceUnits(200), b.LogicalToDeviceUnits(38));
        }
        internal static GraphicsPath BoGoc(Rectangle r, int bk)
        {
            int d = bk * 2;
            var p = new GraphicsPath();
            p.AddArc(r.Left, r.Top, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
    /// <summary>Thẻ trắng bo góc 14px, viền mảnh, tiêu đề đậm. Chỉ để trình bày.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal sealed class TheF16 : Panel
    {
        private static readonly Font FontTieuDe = new Font("Segoe UI Semibold", 11.5F, FontStyle.Regular, GraphicsUnit.Point);
        private string _tieuDe = "";
        public string TieuDe { get => _tieuDe; set { _tieuDe = value; Invalidate(); } }
        public TheF16()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            using (var nenCha = new SolidBrush(Parent?.BackColor ?? Color.FromArgb(243, 244, 248)))
                g.FillRectangle(nenCha, ClientRectangle);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Module_GiaoDienForm16Web.BoGoc(r, LogicalToDeviceUnits(14)))
            {
                using var fill = new SolidBrush(Color.White);
                g.FillPath(fill, path);
                using var vien = new Pen(Color.FromArgb(226, 232, 240));
                g.DrawPath(vien, path);
            }
            TextRenderer.DrawText(g, _tieuDe, FontTieuDe, new Rectangle(Padding.Left, LogicalToDeviceUnits(12), Width - Padding.Horizontal, LogicalToDeviceUnits(26)),
                Color.FromArgb(15, 23, 42), Color.White, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }
}