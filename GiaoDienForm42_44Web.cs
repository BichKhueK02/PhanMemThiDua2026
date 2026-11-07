using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CHO Form42_QuanLyThiDuaBaNhat VÀ Form44_SoVangBaNhat — CHỈ TRÌNH BÀY, KHÔNG CÓ LOGIC NGHIỆP VỤ
    //  - Lớp static độc lập (không phải form) => không tạo .resx, KHÔNG sửa .csproj.
    //  - Mỗi form chỉ thêm 1 dòng trong constructor, ngay SAU InitializeComponent();
    //        Form42:  GiaoDienForm42_44Web.ApDung(this);
    //        Form44:  GiaoDienForm42_44Web.ApDung(this);
    //  - Tìm control theo Name => không đổi tên/kiểu/sự kiện của control nào; ô nhập, nút, bảng, menu chuột phải, thanh trạng thái giữ nguyên logic.
    //  - Dùng lại TheBoGocF4 (thẻ bo góc) và OComboWebF4 (combobox bo tròn) trong GiaoDienForm4Web.cs => file đó phải còn trong project.
    //  - Những thứ code gốc tự điều khiển được giữ nguyên: màu xanh nhạt của ô chỉ-đọc (KhoaCacTextBox), màu nhãn số ký tự, màu chữ thanh trạng thái,
    //    font + chiều cao + độ rộng cột của bảng.
    // =====================================================================================
    internal static class GiaoDienForm42_44Web
    {
        private static readonly Color MauNen = Color.FromArgb(244, 246, 250);
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);
        private static readonly Color MauTieuDeBang = Color.FromArgb(238, 232, 255);
        private static readonly Color MauChuTieuDeBang = Color.FromArgb(91, 33, 182);

        private static readonly Font FontNhan = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNutLon = new Font("Segoe UI Semibold", 10F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontDemKyTu = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);

        private const int DemKhungBang = 5;

        // Bộ màu nút (Nền, Hover, Nhấn, Viền, Chữ) — cùng theme tím nhạt với Form4
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutTim =
            (Color.FromArgb(237, 233, 254), Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253), Color.FromArgb(196, 181, 253), Color.FromArgb(109, 40, 217));
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutTimDam =
            (Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253), Color.FromArgb(167, 139, 250), Color.FromArgb(167, 139, 250), Color.FromArgb(91, 33, 182));
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutVien =
            (Color.White, Color.FromArgb(248, 250, 252), Color.FromArgb(241, 245, 249), Color.FromArgb(203, 213, 225), Color.FromArgb(51, 65, 85));
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutNguyHiem =
            (Color.FromArgb(254, 242, 242), Color.FromArgb(254, 226, 226), Color.FromArgb(252, 205, 205), Color.FromArgb(252, 165, 165), Color.FromArgb(185, 28, 28));

        public static void ApDung(Form f)
        {
            f.SuspendLayout();
            try
            {
                f.BackColor = MauNen;
                DinhBoCuc(f);
                BocCacThe(f);
                DinhKhoiTimKiem(f);
                DinhKhoiLenh(f);
                DinhKhoiThongTin(f);
                DinhLuoi(f);
                DinhThanhTrangThai(f);
                BoTronCacCombo(f);
            }
            finally
            {
                f.ResumeLayout(true);
            }

            // Code gốc đặt lại vài thuộc tính sau khi nạp dữ liệu -> áp dụng lại (đăng ký sau nên chạy sau)
            f.Load += (s, e) => HoanThien(f);
            f.Shown += (s, e) => HoanThien(f);
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

        // ---------- Bố cục tổng ----------
        private static void DinhBoCuc(Form f)
        {
            var t1 = Tim<TableLayoutPanel>(f, "tableLayoutPanel1");
            if (t1 != null)
            {
                t1.BackColor = MauNen;
                t1.Padding = new Padding(12, 10, 12, 8);
                t1.ColumnStyles.Clear();
                t1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                t1.RowStyles.Clear();
                t1.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));    // hàng 1: tìm kiếm + lệnh (cố định)
                t1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));    // hàng 2: thông tin + bảng (ăn hết phần còn lại)
                if (t1.RowCount >= 3) t1.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // Form42: thanh trạng thái nằm trong hàng 3
            }

            var t2 = Tim<TableLayoutPanel>(f, "tableLayoutPanel2");
            if (t2 != null)
            {
                t2.BackColor = MauNen;
                t2.Margin = new Padding(0);
                t2.ColumnStyles.Clear();
                t2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
                t2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
                t2.RowStyles.Clear();
                t2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            }

            var sc = Tim<SplitContainer>(f, "splitContainer1");
            if (sc != null)
            {
                sc.BackColor = MauNen;
                sc.Panel1.BackColor = MauNen;
                sc.Panel2.BackColor = MauNen;
                sc.Margin = new Padding(0, 10, 0, 0);
                try
                {
                    sc.SplitterWidth = 10;                 // khe hở giữa 2 thẻ
                    sc.FixedPanel = FixedPanel.Panel1;     // resize cửa sổ chỉ co giãn bảng
                }
                catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException) { }
            }
        }

        // ---------- GroupBox -> thẻ trắng bo góc (hỗ trợ cả GroupBox nằm trong TableLayoutPanel) ----------
        private static void BocCacThe(Form f)
        {
            // Thẻ "Thông tin" (chứa cả thẻ con "Thành tích") bọc trước, thẻ con bọc sau => nội dung đã nằm sẵn trong thẻ lớn
            BocThe(Tim<GroupBox>(f, "groupBox1_TimKiemThongTinCBCS"), new Padding(0, 0, 5, 0));
            BocThe(Tim<GroupBox>(f, "groupBox2_KhoiLenhDieuKhien"), new Padding(5, 0, 0, 0));
            BocThe(Tim<GroupBox>(f, "groupBox3_ThongTinCBCS"), new Padding(0));
            BocThe(Tim<GroupBox>(f, "groupBox1"), new Padding(0, 0, 0, 0));
        }

        private static void BocThe(GroupBox? gb, Padding margin)
        {
            if (gb == null) return;
            var cha = gb.Parent;
            if (cha == null || gb.Controls.Count == 0) return;
            var noiDung = gb.Controls[0];

            var the = new TheBoGocF4
            {
                Dock = DockStyle.Fill,
                Margin = margin,
                TieuDe = gb.Text,
                Padding = new Padding(12, 36, 12, 10)   // 36 = vùng tiêu đề, nội dung luôn nằm dưới tiêu đề
            };

            cha.SuspendLayout();
            try
            {
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
                gb.Visible = false;
                gb.Dock = DockStyle.None;
                the.Controls.Add(noiDung);       // chuyển nguyên nội dung sang thẻ (mọi control + sự kiện giữ nguyên)
                noiDung.Dock = DockStyle.Fill;
                the.Controls.Add(gb);            // GroupBox giữ lại (ẩn): code gốc gán .Text thì tiêu đề thẻ tự cập nhật
                gb.TextChanged += (s, e) => the.TieuDe = gb.Text;
            }
            finally
            {
                cha.ResumeLayout(true);
            }
        }

        // ---------- Thẻ 1: Tìm kiếm ----------
        private static void DinhKhoiTimKiem(Form f)
        {
            var t5 = Tim<TableLayoutPanel>(f, "tableLayoutPanel5");
            if (t5 == null) return;
            t5.Margin = new Padding(0);
            t5.ColumnStyles.Clear();
            if (t5.ColumnCount >= 7)   // Form42: Họ và tên | ô | Đơn vị | combo | Đề nghị | combo | [làm mới]
            {
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29F));
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29F));
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64F));
            }
            else                        // Form44: Họ và tên | ô | Đơn vị | combo | [làm mới]
            {
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64F));
            }
            t5.RowStyles.Clear();
            t5.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            foreach (var l in t5.Controls.OfType<Label>())
            {
                DinhNhan(l, Anchor_Trai, t5.GetColumn(l) == 0 ? 0 : 16);
            }
            foreach (var c in t5.Controls)
            {
                if (c is Krypton.Toolkit.KryptonTextBox tb) DinhOTextBox(tb);
                else if (c is ComboBox cb) { cb.Anchor = AnchorStyles.Left | AnchorStyles.Right; cb.Margin = new Padding(0, 3, 0, 3); }
                else if (c is Krypton.Toolkit.KryptonButton bt) { KieuNut(bt, NutTim); CanNut(bt, new Padding(10, 0, 0, 0)); }
            }
        }

        // ---------- Thẻ 2: Khối lệnh (Làm mới / Thành tích tập thể / Thoát) ----------
        private static void DinhKhoiLenh(Form f)
        {
            var t6 = Tim<TableLayoutPanel>(f, "tableLayoutPanel6");
            if (t6 == null) return;
            t6.Margin = new Padding(0);
            t6.ColumnStyles.Clear();
            for (int i = 0; i < t6.ColumnCount; i++) t6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / t6.ColumnCount));
            t6.RowStyles.Clear();
            t6.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            foreach (var b in t6.Controls.OfType<Krypton.Toolkit.KryptonButton>())
            {
                bool cuoi = t6.GetColumn(b) == t6.ColumnCount - 1;
                KieuNut(b, b.Name == "kryptonButton1_Thoat" ? NutVien : NutTim);
                CanNut(b, new Padding(0, 0, cuoi ? 0 : 8, 0));
            }
        }

        // ---------- Thẻ 3: Thông tin cán bộ (lưới nhãn + ô, thẻ con Thành tích, hàng nút) ----------
        private static void DinhKhoiThongTin(Form f)
        {
            var t3 = Tim<TableLayoutPanel>(f, "tableLayoutPanel3");
            if (t3 != null)
            {
                t3.Margin = new Padding(0);
                t3.RowStyles.Clear();
                t3.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            }
            var t4 = Tim<TableLayoutPanel>(f, "tableLayoutPanel4");
            if (t4 != null)
            {
                t4.Margin = new Padding(0);
                t4.ColumnStyles.Clear();
                t4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
                t4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
                t4.RowStyles.Clear();
                t4.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            }

            // Lưới nhãn + ô: 6 cột (nhãn | ô) x 3, hàng chia đều
            var t10 = Tim<TableLayoutPanel>(f, "tableLayoutPanel10");
            if (t10 != null)
            {
                t10.Margin = new Padding(0, 0, 14, 0);
                t10.ColumnStyles.Clear();
                t10.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                t10.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
                t10.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                t10.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
                t10.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                t10.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
                t10.RowStyles.Clear();
                for (int i = 0; i < t10.RowCount; i++) t10.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / t10.RowCount));

                foreach (var c in t10.Controls)
                {
                    if (c is Label l) DinhNhan(l, Anchor_Phai, t10.GetColumn(l) == 0 ? 0 : 16);
                    else if (c is Krypton.Toolkit.KryptonTextBox tb) DinhOTextBox(tb);
                    else if (c is ComboBox cb) { cb.Anchor = AnchorStyles.Left | AnchorStyles.Right; cb.Margin = new Padding(0, 3, 0, 3); }
                }
            }

            // Cột phải: thẻ con "Thành tích" (ô văn bản nhiều dòng) + hàng nút
            var t8 = Tim<TableLayoutPanel>(f, "tableLayoutPanel8");
            if (t8 != null)
            {
                t8.Margin = new Padding(0);
                t8.ColumnStyles.Clear();
                t8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                t8.RowStyles.Clear();
                t8.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                t8.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            }
            var t9 = Tim<TableLayoutPanel>(f, "tableLayoutPanel9");
            if (t9 != null)
            {
                t9.Margin = new Padding(0, 6, 0, 0);
                t9.ColumnStyles.Clear();
                if (t9.ColumnCount >= 4)   // Form42: [nhãn số ký tự ........] [A-] [A+] [Lưu]
                {
                    t9.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                    t9.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58F));
                    t9.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58F));
                    t9.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
                }
                else                        // Form44: [Xóa] [Lưu] chia đôi
                {
                    t9.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                    t9.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                }
                t9.RowStyles.Clear();
                t9.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                bool nhieuCot = t9.ColumnCount >= 4;
                foreach (var c in t9.Controls)
                {
                    if (c is Krypton.Toolkit.KryptonButton b)
                    {
                        bool luu = b.Name.StartsWith("kryptonButton_Luu", StringComparison.Ordinal);
                        bool xoa = b.Name.IndexOf("Xoa", StringComparison.OrdinalIgnoreCase) >= 0;
                        KieuNut(b, luu ? NutTimDam : xoa ? NutNguyHiem : NutVien);
                        if (luu) b.StateCommon.Content.ShortText.Font = FontNutLon;
                        int cot = t9.GetColumn(b);
                        CanNut(b, nhieuCot ? new Padding(cot == 0 ? 0 : 6, 0, 0, 0)
                                           : new Padding(cot == 0 ? 0 : 6, 0, cot == 0 ? 6 : 0, 0));
                    }
                    else if (c is Label l)   // nhãn đếm số ký tự (màu chữ do logic điều khiển)
                    {
                        l.Font = FontDemKyTu;
                        l.Anchor = AnchorStyles.Left;
                        l.Margin = new Padding(4, 0, 0, 0);
                    }
                }
            }

            var rtb = Tim<RichTextBox>(f, "richTextBox1_ThanhTich");
            if (rtb != null) rtb.BackColor = Color.White;
        }

        // ---------- Bảng dữ liệu: nằm trong khung bo tròn liền mạch, tiêu đề phẳng tím nhạt ----------
        private static void DinhLuoi(Form f)
        {
            var d = Tim<Krypton.Toolkit.KryptonDataGridView>(f, "kryptonDataGridView1");
            var sc = Tim<SplitContainer>(f, "splitContainer1");
            if (d == null || sc == null) return;

            var p2 = d.Parent;
            if (p2 == null) return;
            var the = new TheBoGocF4
            {
                Dock = DockStyle.Fill,
                DemKhung = DemKhungBang,
                LuoiBang = d,
                Padding = new Padding(10 + DemKhungBang)
            };
            p2.SuspendLayout();
            try
            {
                p2.Controls.Add(the);
                the.Controls.Add(d);           // tự gỡ khỏi Panel2; mọi sự kiện/logic của bảng giữ nguyên
                d.Dock = DockStyle.Fill;
                d.BorderStyle = BorderStyle.None;
                d.ColumnHeadersHeightChanged += (s, e) => the.Invalidate();
                d.ColumnHeadersDefaultCellStyleChanged += (s, e) => the.Invalidate();
                d.DataBindingComplete += (s, e) => the.Invalidate();
            }
            finally
            {
                p2.ResumeLayout(true);
            }

            d.BackgroundColor = Color.White;
            d.StateCommon.Background.Color1 = Color.White;
            d.StateCommon.HeaderRow.Back.Color1 = Color.FromArgb(250, 248, 255);
            d.StateCommon.HeaderRow.Back.Color2 = Color.FromArgb(250, 248, 255);
            d.StateCommon.HeaderRow.Border.Color1 = Color.FromArgb(237, 233, 254);
            TieuDePhang(d.StateCommon.HeaderColumn.Back, d.StateCommon.HeaderColumn.Border);
            TieuDePhang(d.StateTracking.HeaderColumn.Back, d.StateTracking.HeaderColumn.Border);
            TieuDePhang(d.StatePressed.HeaderColumn.Back, d.StatePressed.HeaderColumn.Border);
            d.StateCommon.HeaderColumn.Content.Color1 = MauChuTieuDeBang;
            d.StateCommon.DataCell.Back.Color1 = Color.White;
            d.StateCommon.DataCell.Content.Color1 = Color.FromArgb(51, 65, 85);
            MauSauNap(d);

            d.DataBindingComplete += (s, e) => { MauSauNap(d); d.BeginInvoke(new Action(() => MauSauNap(d))); };
        }

        private static void TieuDePhang(Krypton.Toolkit.PaletteBack back, Krypton.Toolkit.PaletteBorder vien)
        {
            back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            back.Color1 = MauTieuDeBang; back.Color2 = MauTieuDeBang;
            vien.Color1 = MauTieuDeBang; vien.Color2 = MauTieuDeBang;
        }

        // Màu dòng kẻ + dòng chọn: code gốc đặt lại (xám 224 / xanh dương cũ) khi nạp dữ liệu -> ghi đè lại sau mỗi lần nạp
        private static void MauSauNap(Krypton.Toolkit.KryptonDataGridView d)
        {
            if (d.IsDisposed) return;
            d.StateCommon.DataCell.Border.Color1 = Color.FromArgb(235, 238, 243);
            d.StateSelected.DataCell.Back.Color1 = Color.FromArgb(239, 246, 255);
            d.StateSelected.DataCell.Back.Color2 = Color.FromArgb(239, 246, 255);
            d.StateSelected.DataCell.Content.Color1 = Color.FromArgb(29, 78, 216);
        }

        // ---------- Thanh trạng thái ----------
        private static void DinhThanhTrangThai(Form f)
        {
            var ss = Tim<StatusStrip>(f, "statusStrip1");
            if (ss == null) return;
            ss.BackColor = Color.White;
            ss.ForeColor = MauChuNhan;      // màu từng nhãn do code gốc đặt (xanh khi đang tìm kiếm...)
            ss.SizingGrip = false;
            ss.Padding = new Padding(10, 0, 10, 0);
            foreach (ToolStripItem it in ss.Items)
                if (it is ToolStripStatusLabel) it.Font = FontNhan;
        }

        // ---------- Mọi ComboBox: ô chọn + danh sách sổ xuống bo tròn (OComboWebF4) ----------
        private static void BoTronCacCombo(Form f)
        {
            foreach (var cb in TatCa<ComboBox>(f).ToList())
                if (cb.GetType() == typeof(ComboBox) && cb.Parent != null)
                    OComboWebF4.Gan(cb);
        }

        // Sau Load / Shown: ô nhập Krypton bo góc đồng nhất
        private static void HoanThien(Form f)
        {
            try
            {
                foreach (var tb in TatCa<Krypton.Toolkit.KryptonTextBox>(f))
                    tb.StateCommon.Border.Rounding = 6F;
                var d = Tim<Krypton.Toolkit.KryptonDataGridView>(f, "kryptonDataGridView1");
                if (d != null) MauSauNap(d);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[GiaoDienWeb] " + ex.Message);   // giao diện không bao giờ được làm sập form
            }
        }

        // =====================================================================================
        //  TIỆN ÍCH TRÌNH BÀY
        // =====================================================================================
        private const AnchorStyles Anchor_Trai = AnchorStyles.Left;
        private const AnchorStyles Anchor_Phai = AnchorStyles.Right;

        private static void DinhNhan(Label lb, AnchorStyles neo, int leTrai)
        {
            lb.Font = FontNhan;
            lb.ForeColor = MauChuNhan;
            lb.Anchor = neo;
            lb.Margin = new Padding(leTrai, 0, 8, 0);
        }

        private static void DinhOTextBox(Krypton.Toolkit.KryptonTextBox tb)
        {
            tb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tb.Margin = new Padding(0, 3, 0, 3);
            tb.StateCommon.Border.Rounding = 6F;
        }

        private static void CanNut(Krypton.Toolkit.KryptonButton b, Padding margin)
        {
            b.Anchor = AnchorStyles.Left | AnchorStyles.Right;
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
            // Nút đang giữ focus / nút mặc định của form (AcceptButton): ép giống trạng thái thường để chữ không bị trắng trên nền trắng
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
        }
    }
}