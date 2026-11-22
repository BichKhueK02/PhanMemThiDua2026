using System.Diagnostics;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CHO Form49_QuanLyKhenThuongTapThe — CHỈ TRÌNH BÀY, KHÔNG CÓ LOGIC NGHIỆP VỤ
    //  Cùng ngôn ngữ thiết kế với Form4 / Form15 / Form23 / Form34 / Form51: nền trang xám nhạt, thẻ trắng bo góc,
    //  combo bo tròn, nút theme tím nhạt (nút "Xóa" đỏ nhạt), bảng khung bo tròn + tiêu đề tím nhạt có vạch phân cách cột.
    //
    //  KHÔNG ĐỔI CODE GỐC: Form49 chỉ thêm đúng 1 dòng ở CUỐI constructor (sau Module_HeThong.BocBoTronGiaoDien(richTextBox1_NoiDungKhenThuong);):
    //        Module_GiaoDienForm49Web.ApDung(this);
    //  - Không sửa Designer, không đổi tên/kiểu/sự kiện của control nào; tìm control theo Name.
    //  - Code gốc định dạng lưới trong Load (CauHinhGridCoBan / CauHinhStyleWeb / CauHinhCotGrid, chạy 1 lần): màu chọn dòng, font, đường kẻ ô.
    //    Helper áp lại màu của mình ngay khi form hiện (sự kiện Shown, sau Load) và khi lưới layout lại; cột, độ rộng, chiều cao,
    //    màu dòng xen kẽ vẫn do code gốc quyết định.
    //  - Ô STT được code gốc khóa chỉ-đọc và tô nền xanh nhạt khi Load -> helper KHÔNG đụng tới nền, chỉ đổi khung.
    //  - 2 combo "Hình thức" / "Đơn vị khen thưởng" ở khung nhập là combo gõ được (DropDown): giữ nguyên combo gốc,
    //    chỉ đổi font; các combo tìm kiếm (DropDownList) được thay bằng ô combo bo tròn OComboWebF4.
    //  - Dùng lại TheBoGocF4 và OComboWebF4 trong Module_GiaoDienForm4Web.cs (file đó phải còn trong project).
    //  - Mọi bước bọc try/catch: lỗi trình bày không bao giờ làm form không mở được.
    // =====================================================================================
    internal static class Module_GiaoDienForm49Web
    {
        // ===================== TÙY CHỌN =====================
        private const int DemKhungBang = 5;     // khoảng đệm giữa khung bo tròn và bảng
        private const int CaoHangTimKiem = 84;  // chiều cao hàng logo + thẻ tìm kiếm (px thiết kế, nhân theo DPI)
        private const int CaoHangNhapLieu = 296;// chiều cao hàng 2 thẻ nhập liệu
        // ===================== BẢNG MÀU =====================
        private static readonly Color MauNen = Color.FromArgb(244, 246, 250);
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);
        private static readonly Color MauChuDuLieu = Color.FromArgb(30, 41, 59);
        private static readonly Color MauVienO = Color.FromArgb(203, 213, 225);
        private static readonly Color MauVienChon = Color.FromArgb(139, 92, 246);
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
        private static readonly Font FontTrangThai = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
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
                An(() => DinhTheTimKiem(f), "TheTimKiem");
                An(() => DinhTheNhapLieu(f), "TheNhapLieu");
                An(() => DinhNutThaoTac(f), "NutThaoTac");
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
            catch (Exception ex) { Debug.WriteLine($"[Module_GiaoDienForm49Web.{ten}] {ex.Message}"); }
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
        private static void BatDoubleBuffer(Form f)
        {
            if (PropDoubleBuffered == null) return;
            foreach (var ten in new[] { "tableLayoutPanel1", "tableLayoutPanel2", "tableLayoutPanel3", "tableLayoutPanel4", "tableLayoutPanel5",
                                        "tableLayoutPanel6", "tableLayoutPanel7", "tableLayoutPanel8", "tableLayoutPanel9", "tableLayoutPanel10", "splitContainer1" })
            {
                var c = Tim<Control>(f, ten);
                if (c == null) continue;
                PropDoubleBuffered.SetValue(c, true);
                if (c is SplitContainer sc) { PropDoubleBuffered.SetValue(sc.Panel1, true); PropDoubleBuffered.SetValue(sc.Panel2, true); }
            }
        }
        // ===================== BỐ CỤC TỔNG =====================
        // SplitContainer ngang: Panel1 = [logo + thẻ tìm kiếm] + [2 thẻ nhập liệu] | Panel2 = thẻ bảng (co giãn)
        private static void DinhBoCuc(Form f)
        {
            var t1 = Tim<TableLayoutPanel>(f, "tableLayoutPanel1");
            if (t1 != null)
            {
                t1.BackColor = MauNen;
                t1.Margin = new Padding(0);
                t1.Padding = new Padding(12, 10, 12, 6);
                t1.RowStyles.Clear();    // bản gốc có 3 kiểu hàng nhưng chỉ 1 control
                t1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                t1.RowCount = 1;
            }
            var t7 = Tim<TableLayoutPanel>(f, "tableLayoutPanel7");
            if (t7 != null)
            {
                t7.BackColor = MauNen;
                t7.Margin = new Padding(0);
                t7.RowStyles.Clear();
                t7.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            }
            var sc = Tim<SplitContainer>(f, "splitContainer1");
            if (sc != null)
            {
                sc.BackColor = MauNen;
                sc.Panel1.BackColor = MauNen;
                sc.Panel2.BackColor = MauNen;
                sc.Margin = new Padding(0);
                sc.IsSplitterFixed = true;              // trang web không có thanh kéo chia vùng
                sc.FixedPanel = FixedPanel.Panel1;      // đổi cỡ cửa sổ chỉ co giãn thẻ bảng
                try { sc.SplitterWidth = 10; } catch (ArgumentException) { }
                try { sc.SplitterDistance = Px(f, CaoHangTimKiem + CaoHangNhapLieu); } catch (ArgumentException) { }
            }
            // Panel1: hàng logo + tìm kiếm | hàng 2 thẻ nhập liệu
            var t6 = Tim<TableLayoutPanel>(f, "tableLayoutPanel6");
            if (t6 != null)
            {
                t6.BackColor = MauNen;
                t6.Margin = new Padding(0);
                t6.RowStyles.Clear();
                t6.RowStyles.Add(new RowStyle(SizeType.Absolute, Px(f, CaoHangTimKiem)));
                t6.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            }
            // Logo + thẻ tìm kiếm
            var t2 = Tim<TableLayoutPanel>(f, "tableLayoutPanel2");
            if (t2 != null)
            {
                t2.BackColor = MauNen;
                t2.Margin = new Padding(0);
                t2.ColumnStyles.Clear();   // bản gốc còn dư 1 cột Absolute 18 không có control
                t2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Px(f, 84)));
                t2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            }
            var logo = Tim<PictureBox>(f, "pictureBox1");
            if (logo != null)
            {
                logo.BackColor = Color.Transparent;
                logo.SizeMode = PictureBoxSizeMode.Zoom;    // giữ đúng tỷ lệ ảnh (bản gốc kéo giãn)
                logo.Anchor = AnchorStyles.None;
                logo.Size = new Size(Px(f, 70), Px(f, 62));
            }
            var t3 = Tim<TableLayoutPanel>(f, "tableLayoutPanel3");
            if (t3 != null)
            {
                t3.BackColor = MauNen;
                t3.Margin = new Padding(0);
                t3.ColumnStyles.Clear();
                t3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            }
            // Khu nhập liệu: 2 thẻ cạnh nhau
            var t4 = Tim<TableLayoutPanel>(f, "tableLayoutPanel4");
            if (t4 != null)
            {
                t4.BackColor = MauNen;
                t4.ForeColor = MauChuNhan;     // bản gốc: chữ xanh đậm cho cả bảng
                t4.Margin = new Padding(0);
                t4.ColumnStyles.Clear();
                t4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
                t4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            }
        }
        // ===================== THẺ =====================
        private static void BocCacThe(Form f)
        {
            // 1. Tìm kiếm
            var gbTim = Tim<GroupBox>(f, "groupBox1_TimKiemThongTinCBCS");
            if (gbTim != null) ThayGroupBoxBangThe(gbTim, new Padding(0));
            // 3. Cập nhật thông tin khen thưởng: bỏ khung GroupBox ngoài, tách thành 2 thẻ: [form nhập] | [nội dung + nút]
            var gb3 = Tim<GroupBox>(f, "groupBox1");
            var t4 = Tim<TableLayoutPanel>(f, "tableLayoutPanel4");
            var t7 = gb3?.Parent as TableLayoutPanel;   // ô chứa GroupBox "3." (ở Form49 là tableLayoutPanel6)
            string tieuDe3 = gb3?.Text ?? "3. Cập nhật thông tin khen thưởng";
            if (gb3 != null && t4 != null && t7 != null && gb3.Parent == t7)
            {
                var vt = t7.GetPositionFromControl(gb3);
                t7.SuspendLayout();
                try
                {
                    gb3.Controls.Remove(t4);
                    t7.Controls.Remove(gb3);
                    t4.Dock = DockStyle.Fill;
                    t7.Controls.Add(t4, vt.Column, vt.Row);   // gb3 giữ lại (không hiển thị), không Dispose
                }
                finally { t7.ResumeLayout(true); }
            }
            var t10 = Tim<TableLayoutPanel>(f, "tableLayoutPanel10");
            if (t10 != null) BocControlBangThe(t10, tieuDe3, new Padding(0, 6, 6, 0));
            // Nội dung khen thưởng: groupBox2 (chỉ chứa ô soạn thảo) -> bỏ, ô soạn thảo đặt thẳng vào ô lưới, rồi bọc cả cột thành 1 thẻ
            var gb2 = Tim<GroupBox>(f, "groupBox2");
            var t8 = Tim<TableLayoutPanel>(f, "tableLayoutPanel8");
            string tieuDeNoiDung = gb2?.Text ?? "Nội dung khen thưởng";
            if (gb2 != null && t8 != null && gb2.Parent == t8)
            {
                var vt = t8.GetPositionFromControl(gb2);
                t8.SuspendLayout();
                try
                {
                    var con = gb2.Controls.Cast<Control>().ToArray();   // ô soạn thảo (có thể đã nằm trong khung bo của BocBoTronGiaoDien)
                    t8.Controls.Remove(gb2);
                    foreach (var c in con)
                    {
                        gb2.Controls.Remove(c);
                        c.Dock = DockStyle.Fill;
                        t8.Controls.Add(c, vt.Column, vt.Row);
                    }
                }
                finally { t8.ResumeLayout(true); }
            }
            if (t8 != null)
            {
                t8.Dock = DockStyle.Fill;
                BocControlBangThe(t8, tieuDeNoiDung, new Padding(6, 6, 0, 0));
            }
        }
        private static TheBoGocF4? TaoThe(string tenGoc, string tieuDe, Padding margin) => new TheBoGocF4
        {
            Name = "the_" + tenGoc,
            Dock = DockStyle.Fill,
            Margin = margin,
            TieuDe = tieuDe,
            Padding = new Padding(12, 36, 12, 8)
        };
        /// <summary>Thay GroupBox bằng thẻ cùng vị trí, chuyển nguyên control con sang thẻ (GroupBox giữ lại, không Dispose).</summary>
        private static void ThayGroupBoxBangThe(GroupBox gb, Padding margin)
        {
            var cha = gb.Parent;
            if (cha == null) return;
            var the = TaoThe(gb.Name, gb.Text, margin)!;
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
        /// <summary>Bọc 1 control (đang nằm trong ô của TableLayoutPanel) vào 1 thẻ có tiêu đề, cùng ô.</summary>
        private static void BocControlBangThe(Control noiDung, string tieuDe, Padding margin)
        {
            var cha = noiDung.Parent;
            if (cha == null) return;
            var the = TaoThe(noiDung.Name, tieuDe, margin)!;
            cha.SuspendLayout();
            the.SuspendLayout();
            try
            {
                if (cha is TableLayoutPanel tlp)
                {
                    var vt = tlp.GetPositionFromControl(noiDung);
                    tlp.Controls.Remove(noiDung);
                    tlp.Controls.Add(the, vt.Column, vt.Row);
                }
                else
                {
                    cha.Controls.Remove(noiDung);
                    cha.Controls.Add(the);
                }
                noiDung.Dock = DockStyle.Fill;
                the.Controls.Add(noiDung);
            }
            finally
            {
                the.ResumeLayout(false);
                cha.ResumeLayout(true);
            }
        }
        // ===================== THẺ 1: TÌM KIẾM =====================
        private static void DinhTheTimKiem(Form f)
        {
            // Tên tập thể | ô tìm | Đơn vị khen thưởng | combo | Hình thức | combo | nút làm mới
            var t5 = Tim<TableLayoutPanel>(f, "tableLayoutPanel5");
            if (t5 != null)
            {
                t5.Margin = new Padding(0);
                t5.ColumnStyles.Clear();
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));           // 0 Tên tập thể
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));       // 1 ô tìm
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));           // 2 Đơn vị khen thưởng
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));       // 3
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));           // 4 Hình thức
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));       // 5
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Px(f, 56)));// 6 nút làm mới
            }
            DinhNhan(Tim<Label>(f, "label3"), dauHang: true);   // Tên tập thể
            DinhNhan(Tim<Label>(f, "label2"));                  // Đơn vị khen thưởng
            DinhNhan(Tim<Label>(f, "label8"));                  // Hình thức
            var o = Tim<Krypton.Toolkit.KryptonTextBox>(f, "textBox_TimKiemTheoTen");
            if (o != null)
            {
                o.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                o.Margin = new Padding(0);
                o.StateCommon.Back.Color1 = Color.White;
                KieuVienO(o.StateCommon.Border, o.StateActive.Border);
            }
            foreach (var ten in new[] { "comboBox_TimKiemDonViKhenThuong", "comboBox1_HinhThucKT" })
            {
                var cb = Tim<ComboBox>(f, ten);
                if (cb == null) continue;
                cb.Font = FontO;                         // đặt TRƯỚC khi gắn ô bo tròn (nó chép font của ComboBox)
                cb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                cb.Margin = new Padding(0);
            }
            var lamMoi = Tim<Krypton.Toolkit.KryptonButton>(f, "kryptonButton_LamMoiCacOTimKiem");
            if (lamMoi != null)
            {
                KieuNut(lamMoi, NutTim);
                lamMoi.Anchor = AnchorStyles.None;
                lamMoi.Margin = new Padding(0);
                lamMoi.Size = new Size(40, 32);
            }
        }
        // ===================== THẺ 3: FORM NHẬP LIỆU =====================
        private static void DinhTheNhapLieu(Form f)
        {
            // Cột: nhãn | ô | nhãn | ô   (6 hàng)
            var t10 = Tim<TableLayoutPanel>(f, "tableLayoutPanel10");
            if (t10 != null)
            {
                t10.Margin = new Padding(0);
                t10.ColumnStyles.Clear();
                t10.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                t10.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                t10.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                t10.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            }
            // Nhãn cột trái (đầu hàng) và cột phải
            foreach (var ten in new[] { "label6", "label7", "label9", "label12", "label13", "label14" })
                DinhNhan(Tim<Label>(f, ten), dauHang: true);
            foreach (var ten in new[] { "label4", "label1", "label15", "label5", "label17", "label11" })
                DinhNhan(Tim<Label>(f, ten));
            foreach (var ten in new[] { "kryptonTextBox_STT", "kryptonTextBox_TenTapThe", "kryptonTextBox_SoQuyetDinh",
                                        "kryptonTextBox_NgayQuyetDinh", "kryptonTextBox_NguoiKy", "kryptonTextBox_NgayCapPhat",
                                        "kryptonTextBox_TienThuong", "kryptonTextBox_CanBoCapPhat",
                                        "kryptonTextBox_NguoiDaiDienNhan", "kryptonTextBox_GhiChu" })
            {
                var ktb = Tim<Krypton.Toolkit.KryptonTextBox>(f, ten);
                if (ktb == null) continue;
                ktb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                ktb.Margin = new Padding(0, 0, 0, 0);
                KieuVienO(ktb.StateCommon.Border, ktb.StateActive.Border);   // KHÔNG đụng tới nền (ô STT bị code gốc tô xanh nhạt)
            }
            // 2 combo gõ được (DropDown): giữ combo gốc, chỉ chỉnh font + co giãn
            foreach (var ten in new[] { "comboBox_HinhThucKhenThuong", "comboBox_DonViKhenThuong" })
            {
                var cb = Tim<ComboBox>(f, ten);
                if (cb == null) continue;
                cb.Font = FontO;
                cb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                cb.Margin = new Padding(0);
            }
            // Ô soạn thảo nội dung khen thưởng: phẳng, không viền, nền trắng (thẻ đã có khung)
            var rtb = Tim<RichTextBox>(f, "richTextBox1_NoiDungKhenThuong");
            if (rtb != null) { rtb.BorderStyle = BorderStyle.None; rtb.BackColor = Color.White; }
        }
        // ===================== NÚT THAO TÁC (Thêm / Sửa / Xóa / cỡ chữ) =====================
        private static void DinhNutThaoTac(Form f)
        {
            var t9 = Tim<TableLayoutPanel>(f, "tableLayoutPanel9");
            if (t9 != null)
            {
                t9.Margin = new Padding(0, 8, 0, 0);
                t9.RowStyles.Clear();
                t9.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                t9.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            }
            NutTrongO(f, "kryptonButton_ThemKhenThuong", NutTimDam, new Padding(0, 0, 6, 0));
            NutTrongO(f, "kryptonButton_SuaVaLuuKhenThuong", NutTim, new Padding(3, 0, 3, 0));
            NutTrongO(f, "kryptonButton_XoaKhenThuong", NutDo, new Padding(6, 0, 0, 0));
            // 2 nút tăng/giảm cỡ chữ (chỉ có biểu tượng; code gốc tự ẩn/hiện theo nội dung)
            foreach (var ten in new[] { "kryptonButton2_TangCoChuRichText", "kryptonButton2_GiamCoChuRichText" })
            {
                var b = Tim<Krypton.Toolkit.KryptonButton>(f, ten);
                if (b == null) continue;
                KieuNut(b, NutTim);
                b.Anchor = AnchorStyles.None;
                b.Margin = new Padding(0);
                b.Size = new Size(44, 30);
            }
            // Nút GỌN: bản gốc chia 3 cột 33% nên Thêm/Sửa/Xóa dài theo cả thẻ => 3 cột bề ngang cố định dồn phải
            Module_BoLocKhenThuong.XepNutThaoTacGon(t9,
                Tim<Control>(f, "kryptonButton_ThemKhenThuong"), Tim<Control>(f, "kryptonButton_SuaVaLuuKhenThuong"),
                Tim<Control>(f, "kryptonButton_XoaKhenThuong"), Tim<Control>(f, "kryptonButton2_GiamCoChuRichText"),
                Tim<Control>(f, "kryptonButton2_TangCoChuRichText"));
        }
        // ===================== COMBO BO TRÒN (chỉ combo DropDownList) =====================
        private static void BoTronCacCombo(Form f)
        {
            foreach (var cb in TatCa<ComboBox>(f).ToList())
                if (cb.GetType() == typeof(ComboBox) && cb.DropDownStyle == ComboBoxStyle.DropDownList)
                    OComboWebF4.Gan(cb);
        }
        // ===================== LƯỚI =====================
        private static void BocLuoi(Form f)
        {
            var luoi = Tim<DataGridView>(f, "kryptonDataGridView1");
            if (luoi == null || luoi.Parent is not Control cha) return;
            luoi.BorderStyle = BorderStyle.None;
            luoi.Margin = new Padding(0);
            var the = new TheBoGocF4
            {
                Name = "the_kryptonDataGridView1",
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                TieuDe = "2. Danh sách khen thưởng tập thể",
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
                // Code gốc đặt màu chọn dòng / viền ô trong Load (sau constructor) -> áp lại khi form hiện (Shown, sau Load)
                f.Shown += (_, _) => DapKieu();
                _luoi.ColumnAdded += (_, _) => { if (!DaDungKieu()) DapKieu(); };
                _luoi.Layout += (_, _) => { if (!DaDungKieu()) DapKieu(); };
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
            // Font tiêu đề / font ô / padding do code gốc đặt -> không đụng tới
            private static void StyleKrypton(Krypton.Toolkit.KryptonDataGridView k)
            {
                k.StateCommon.Background.Color1 = Color.White;
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
                    Debug.WriteLine("[Module_GiaoDienForm49Web.TieuDe] " + ex.Message);
                }
            }
        }
        // ===================== THANH TRẠNG THÁI (KryptonStatusStrip) =====================
        private static void DinhThanhTrangThai(Form f)
        {
            var ss = Tim<StatusStrip>(f, "kryptonStatusStrip1");
            if (ss == null) return;
            ss.Renderer = new TrangThaiPhang();
            ss.BackColor = Color.White;
            ss.ForeColor = MauChuNhan;
            ss.SizingGrip = false;
            ss.Padding = new Padding(10, 0, 10, 0);
            // toolStripStatusLabel2: code gốc tự đổi màu chữ xanh/đỏ theo kết quả thao tác -> chỉ đổi font
            var l1 = ss.Items.Find("toolStripStatusLabel1", false).FirstOrDefault();
            if (l1 != null) { l1.Font = FontTrangThai; l1.ForeColor = MauChuNhan; }
            var l2 = ss.Items.Find("toolStripStatusLabel2", false).FirstOrDefault();
            if (l2 != null) l2.Font = FontTrangThai;
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
            lb.Font = FontNhan;                       // bản gốc: chữ nghiêng xanh đậm
            lb.ForeColor = MauChuNhan;
            lb.AutoSize = true;
            lb.MaximumSize = Size.Empty;              // không giới hạn bề rộng => không rớt dòng
            lb.TextAlign = ContentAlignment.MiddleLeft;
            lb.UseMnemonic = false;
            lb.Anchor = AnchorStyles.Left;
            lb.Margin = new Padding(dauHang ? 0 : 16, 0, 10, 0);
        }
        private static void KieuVienO(Krypton.Toolkit.PaletteBorder vien, Krypton.Toolkit.PaletteBorder vienActive)
        {
            vien.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            vien.Color1 = MauVienO; vien.Color2 = MauVienO;
            vien.Rounding = 8F;
            vien.Width = 1;
            vienActive.Color1 = MauVienChon; vienActive.Color2 = MauVienChon;
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