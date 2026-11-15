using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
using System.Diagnostics;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CHO Form10_NhatKy (Nhật ký phần mềm) — CHỈ TRÌNH BÀY, KHÔNG CÓ LOGIC NGHIỆP VỤ
    //  Cùng ngôn ngữ thiết kế với Form4 (Module_GiaoDienForm4Web): nền trang xám nhạt, thẻ trắng bo góc + thanh nhấn xanh ở tiêu đề,
    //  combo bo tròn, nút theme tím nhạt, bảng có khung bo tròn liền mạch + tiêu đề tím nhạt.
    //
    //  - Lớp static độc lập, không tạo .resx, KHÔNG cần sửa .csproj, KHÔNG sửa Designer.
    //  - Dùng lại các lớp vẽ của Module_GiaoDienForm4Web.cs: TheBoGocF4 (thẻ bo góc), OComboWebF4 (combo bo tròn).
    //    => file Module_GiaoDienForm4Web.cs phải còn trong project (Form4 đang dùng nó nên chắc chắn còn).
    //  - Tìm control theo Name (không phụ thuộc private) => không đổi tên/kiểu/sự kiện của control nào.
    //  - Mọi bước bọc try/catch: lỗi trình bày không bao giờ làm form không mở được.
    //
    //  Dùng trong Form10_NhatKy:
    //     constructor, ngay SAU InitializeComponent():   Module_GiaoDienForm10Web.ApDung(this);
    //     CauHinhStyleWeb(dgv):                          Module_GiaoDienForm10Web.StyleLuoi(dgv);
    //     CapNhatMauRadioSapXep():                       Module_GiaoDienForm10Web.CapNhatRadio(radio);
    // =====================================================================================
    internal static class Module_GiaoDienForm10Web
    {
        // ---------- Bảng màu / font: lấy đúng giá trị của Form4 ----------
        private static readonly Color MauNen = Color.FromArgb(244, 246, 250);
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);
        private static readonly Color MauChuDam = Color.FromArgb(30, 41, 59);
        private static readonly Color MauTim = Color.FromArgb(109, 40, 217);
        private static readonly Color MauVienO = Color.FromArgb(203, 213, 225);
        private static readonly Color MauVienChon = Color.FromArgb(139, 92, 246);

        private static readonly Font FontNhan = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNhanDam = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontO = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point);
        // Font lưới: giữ đúng font hệ thống mà Form10 vẫn dùng cho lưới
        private static readonly Font FontLuoiTieuDe = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Bold);
        private static readonly Font FontLuoiO = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Regular);

        // Khoảng đệm giữa khung bo tròn và bảng (px thiết kế): đủ để 4 góc vuông của bảng nằm lọt trong cung bo
        private const int DemKhungBang = 5;
        private const int CaoHangThe = 192;      // chiều cao hàng thẻ phía trên (Bộ lọc | Phân trang | Sắp xếp)

        private static readonly Color MauTieuDeBang = Color.FromArgb(238, 232, 255);
        private static readonly Color MauChuTieuDeBang = Color.FromArgb(75, 20, 130);

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
                An(() => BocCacThe(f), "CacThe");
                An(() => DinhBoLoc(f), "BoLoc");
                An(() => DinhPhanTrang(f), "PhanTrang");
                An(() => DinhSapXep(f), "SapXep");
                An(() => BoTronCacCombo(f), "Combo");
                An(() => BocLuoi(f), "KhungLuoi");
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
            catch (Exception ex) { Debug.WriteLine($"[Module_GiaoDienForm10Web.{ten}] {ex.Message}"); }
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

        // ===================== BỐ CỤC TỔNG =====================
        private static void DinhBoCuc(Form f)
        {
            // Hàng 0 = nội dung (ăn hết), hàng 1 = thanh trạng thái cao cố định (bản gốc chia theo % nên cao thấp theo cửa sổ)
            var t1 = Tim<TableLayoutPanel>(f, "tableLayoutPanel1");
            if (t1 != null)
            {
                t1.BackColor = MauNen;
                t1.Padding = new Padding(12, 10, 12, 4);
                t1.RowStyles.Clear();
                t1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                t1.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            }

            var sc = Tim<SplitContainer>(f, "splitContainer1");
            if (sc != null)
            {
                sc.BackColor = MauNen;
                sc.Panel1.BackColor = MauNen;
                sc.Panel2.BackColor = MauNen;
                sc.Margin = new Padding(0);
                sc.IsSplitterFixed = true;              // trang web không có thanh kéo chia vùng
                sc.FixedPanel = FixedPanel.Panel1;      // đổi cỡ cửa sổ chỉ co giãn khối lưới
                try { sc.SplitterWidth = 10; } catch (ArgumentException) { }      // khe hở giữa hàng thẻ và thẻ lưới
                try { sc.SplitterDistance = CaoHangThe; } catch (ArgumentException) { }
            }

            var t3 = Tim<TableLayoutPanel>(f, "tableLayoutPanel3");
            if (t3 != null)
            {
                t3.BackColor = MauNen;
                t3.Margin = new Padding(0);
                // Cột "Chọn sắp xếp" rộng cố định: nới ra để tiêu đề thẻ không bị cắt chữ
                if (t3.ColumnStyles.Count >= 4 && t3.ColumnStyles[3].SizeType == SizeType.Absolute)
                    t3.ColumnStyles[3].Width = 180F;
            }
        }

        // ===================== THẺ =====================
        private static void BocCacThe(Form f)
        {
            // Ảnh hướng dẫn (bấm để xem hướng dẫn) -> thẻ không tiêu đề
            var anh = Tim<PictureBox>(f, "pictureBox1");
            if (anh != null)
            {
                var the = ThayBangThe(anh, "", new Padding(10));
                if (the != null)
                {
                    anh.Dock = DockStyle.Fill;
                    anh.Margin = new Padding(0);
                    anh.SizeMode = PictureBoxSizeMode.Zoom;   // trước đây StretchImage -> méo hình
                    anh.Cursor = Cursors.Hand;
                }
            }

            foreach (var ten in new[] { "groupBox1", "groupBox4", "groupBox3" })
            {
                var gb = Tim<GroupBox>(f, ten);
                if (gb != null) ThayBangThe(gb, gb.Text, new Padding(12, 36, 12, 10));   // 36 = vùng tiêu đề thẻ
            }
        }

        /// <summary>
        /// Thay GroupBox / PictureBox nằm trong ô TableLayoutPanel bằng TheBoGocF4 (thẻ trắng bo góc của Form4) đúng ô đó.
        /// GroupBox: chuyển nguyên các control con (bảng bố cục + mọi sự kiện/logic) sang thẻ rồi bỏ GroupBox rỗng.
        /// </summary>
        private static TheBoGocF4? ThayBangThe(Control nguon, string tieuDe, Padding padding)
        {
            if (nguon.Parent is not TableLayoutPanel cha) return null;

            var vt = cha.GetPositionFromControl(nguon);
            int spanCot = cha.GetColumnSpan(nguon), spanHang = cha.GetRowSpan(nguon);
            bool cotCuoi = vt.Column + spanCot >= cha.ColumnCount;

            var the = new TheBoGocF4
            {
                Name = "the_" + nguon.Name,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, cotCuoi ? 0 : 10, 0),   // khe 10px giữa các thẻ trong hàng
                TieuDe = tieuDe,
                Padding = padding
            };

            cha.SuspendLayout();
            the.SuspendLayout();
            try
            {
                if (nguon is GroupBox gb)
                {
                    var con = gb.Controls.Cast<Control>().ToArray();
                    cha.Controls.Remove(gb);
                    cha.Controls.Add(the, vt.Column, vt.Row);
                    foreach (var c in con)
                    {
                        gb.Controls.Remove(c);
                        c.Dock = DockStyle.Fill;
                        the.Controls.Add(c);
                    }
                    gb.Dispose();   // GroupBox đã rỗng; code của form không dùng tới các field groupBox*
                }
                else
                {
                    cha.Controls.Remove(nguon);
                    cha.Controls.Add(the, vt.Column, vt.Row);
                    the.Controls.Add(nguon);
                }
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

        // ===================== THẺ 1: BỘ LỌC NÂNG CAO =====================
        private static void DinhBoLoc(Form f)
        {
            // Cột nhãn đầu hàng cố định -> ô nhập của 2 hàng thẳng cột với nhau
            // Hàng Máy tính | combo | Tài khoản | combo
            DinhHang(f, "tableLayoutPanel9", (SizeType.Absolute, 84F), (SizeType.Percent, 50F), (SizeType.Absolute, 96F), (SizeType.Percent, 50F));
            // Hàng Từ | ngày | đến | ngày
            DinhHang(f, "tableLayoutPanel10", (SizeType.Absolute, 84F), (SizeType.Percent, 50F), (SizeType.Absolute, 96F), (SizeType.Percent, 50F));

            DinhNhan(Tim<Label>(f, "label4"), true);    // Máy tính
            DinhNhan(Tim<Label>(f, "label2"));          // Tài khoản
            DinhNhan(Tim<Label>(f, "label3"), true);    // Từ
            DinhNhan(Tim<Label>(f, "label5"));          // đến

            DinhOCombo(Tim<ComboBox>(f, "comboBox1_MayTinh"));
            DinhOCombo(Tim<ComboBox>(f, "comboBox_LocTaiKhoan"));

            foreach (var ten in new[] { "kryptonDateTimePicker1_NgayThangNamBatDau", "kryptonDateTimePicker1_NgayThangNamKetThuc" })
            {
                var d = Tim<Krypton.Toolkit.KryptonDateTimePicker>(f, ten);
                if (d == null) continue;
                d.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                d.Margin = new Padding(0, 3, 0, 3);
                d.StateCommon.Back.Color1 = Color.White;
                KieuOKrypton(d.StateCommon.Border, d.StateActive.Border);
            }

            // Hàng nút: 2 nút lấp đầy nửa ô, cách nhau 12px
            DatCot(f, "tableLayoutPanel6", (SizeType.Percent, 50F), (SizeType.Percent, 50F));
            NutTrongO(f, "kryptonButton1_LocTheoNgayThangNam", NutTimDam, new Padding(0, 0, 6, 0));
            NutTrongO(f, "kryptonButton1_LamMoiBoLoc", NutTim, new Padding(6, 0, 0, 0));
        }

        // ===================== THẺ 2: PHÂN TRANG ĐIỀU HƯỚNG =====================
        private static void DinhPhanTrang(Form f)
        {
            var t4 = Tim<TableLayoutPanel>(f, "tableLayoutPanel4");
            if (t4 != null)
            {
                t4.RowStyles.Clear();
                t4.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));    // nhập số trang + Áp dụng
                t4.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));    // Trở lại | Tiếp theo
                t4.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));    // "Trang x / y"
            }

            // Nhập số trang | ô số | Áp dụng
            DatCot(f, "tableLayoutPanel7", (SizeType.Absolute, 116F), (SizeType.Absolute, 84F), (SizeType.Percent, 100F));
            DinhNhan(Tim<Label>(f, "label1"), true);

            var o = Tim<Krypton.Toolkit.KryptonTextBox>(f, "textBox_SoDongHienThi");
            if (o != null)
            {
                o.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                o.Margin = new Padding(0, 0, 0, 0);
                o.StateCommon.Back.Color1 = Color.White;
                KieuOKrypton(o.StateCommon.Border, o.StateActive.Border);
                o.StateCommon.Content.Font = FontO;
                o.StateCommon.Content.Color1 = MauChuDam;
            }
            NutTrongO(f, "kryptonButton_ApDungSoTrang", NutTimDam, new Padding(12, 0, 0, 0));

            // Trở lại | Tiếp theo
            DatCot(f, "tableLayoutPanel8", (SizeType.Percent, 50F), (SizeType.Percent, 50F));
            NutTrongO(f, "kryptonButton_TroLai", NutTim, new Padding(0, 0, 6, 0));
            NutTrongO(f, "kryptonButton_TiepTheo", NutTim, new Padding(6, 0, 0, 0));

            // "Trang x / y" (chữ do logic điền)
            var trang = Tim<Label>(f, "label_Trang");
            if (trang != null)
            {
                trang.Font = FontNut;
                trang.ForeColor = MauTim;
                trang.Anchor = AnchorStyles.None;
            }
        }

        // ===================== THẺ 3: CHỌN SẮP XẾP =====================
        private static void DinhSapXep(Form f)
        {
            var t5 = Tim<TableLayoutPanel>(f, "tableLayoutPanel5");
            if (t5 != null)
            {
                t5.RowStyles.Clear();
                t5.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
                t5.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
                t5.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            }
            foreach (var ten in new[] { "radioButton1_TuAZ", "radioButton1_TuZA" })
            {
                var rb = Tim<RadioButton>(f, ten);
                if (rb == null) continue;
                rb.Anchor = AnchorStyles.Left;
                rb.Margin = new Padding(6, 0, 0, 0);
                rb.Cursor = Cursors.Hand;
                CapNhatRadio(rb);
            }
        }

        /// <summary>Radio sắp xếp: mục đang chọn = chữ tím đậm, mục còn lại = chữ xám (bản gốc tô xanh/đỏ).</summary>
        public static void CapNhatRadio(RadioButton rb)
        {
            rb.Font = rb.Checked ? FontNut : FontNhan;
            rb.ForeColor = rb.Checked ? MauTim : MauChuNhan;
        }

        // ===================== COMBO BO TRÒN (dùng OComboWebF4 của Form4) =====================
        private static void BoTronCacCombo(Form f)
        {
            foreach (var cb in TatCa<ComboBox>(f).ToList())   // ToList: Gan thêm control vào thẻ trong lúc duyệt
                if (cb.GetType() == typeof(ComboBox))
                    OComboWebF4.Gan(cb);
        }

        private static void DinhOCombo(ComboBox? c)
        {
            if (c == null) return;
            c.Font = FontO;                     // phải đặt TRƯỚC khi gắn ô bo tròn (nó chép font của ComboBox)
            c.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            c.Margin = new Padding(0, 3, 0, 3);
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
                TieuDe = "4. Danh sách nhật ký phần mềm",
                DemKhung = gap,
                Padding = new Padding(12 + gap, 36 + gap, 12 + gap, 10 + gap),
                LuoiBang = luoi
            };
            cha.SuspendLayout();
            try
            {
                cha.Controls.Remove(luoi);
                cha.Controls.Add(the);
                the.Controls.Add(luoi);
                luoi.Dock = DockStyle.Fill;
                // Code gốc đặt màu / chiều cao tiêu đề bảng lúc chạy (CaiDatCot) -> thẻ vẽ lại để dải tiêu đề luôn khớp màu
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
        /// Style lưới nhật ký giống 2 bảng của Form4: tiêu đề tím nhạt PHẲNG (Krypton mặc định vẽ chuyển sắc + viền hở ở ô đầu),
        /// chữ tiêu đề tím đậm, đường kẻ dòng nhạt, dòng chọn nền xanh rất nhạt. Gọi từ Form10.CauHinhStyleWeb.
        /// </summary>
        public static void StyleLuoi(DataGridView dgv)
        {
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

        // ===================== THANH TRẠNG THÁI =====================
        private static void DinhThanhTrangThai(Form f)
        {
            var ss = Tim<StatusStrip>(f, "statusStrip1");
            if (ss == null) return;
            ss.BackColor = Color.White;
            ss.ForeColor = MauChuNhan;
            ss.SizingGrip = false;
            ss.Padding = new Padding(10, 0, 10, 0);
            foreach (var ten in new[] { "toolStripStatusLabel1", "toolStripStatusLabel2", "toolStripStatusLabel3", "toolStripStatusLabel4" })
            {
                var it = ss.Items.Find(ten, false).FirstOrDefault();
                if (it != null) it.Font = FontNhan;
            }
            // Liên kết "Thông tin người dùng": màu tím cùng theme
            if (ss.Items.Find("thongTinNguoiDung_toolstrip", false).FirstOrDefault() is ToolStripStatusLabel lk)
            {
                lk.Font = FontNhan;
                lk.LinkColor = MauTim;
                lk.VisitedLinkColor = MauTim;
                lk.ActiveLinkColor = Color.FromArgb(220, 38, 38);
            }
        }

        // ===================== TIỆN ÍCH TRÌNH BÀY =====================
        // Đặt kiểu cột cho 1 bảng bố cục (giữ nguyên số hàng và vị trí các control gốc)
        private static void DinhHang(Form f, string ten, params (SizeType Kieu, float Gia)[] cacCot)
        {
            var t = DatCot(f, ten, cacCot);
            if (t != null) t.Margin = new Padding(0);
        }

        private static TableLayoutPanel? DatCot(Form f, string ten, params (SizeType Kieu, float Gia)[] cacCot)
        {
            var t = Tim<TableLayoutPanel>(f, ten);
            if (t == null) return null;
            t.Margin = new Padding(0);
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

        // Ô nhập Krypton: nền trắng, viền xám bo góc 8, khi đang gõ viền chuyển tím (cùng màu viền combo đang chọn)
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

        /// <summary>Nút chính (tím đậm một nấc) — dùng cho cả nút "Đóng" của hộp chi tiết nhật ký.</summary>
        public static void KieuNutChinh(Krypton.Toolkit.KryptonButton b) => KieuNut(b, NutTimDam);

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
            // Nút bị khóa (vd. hết trang để lùi/tiến): tím rất nhạt, chữ tím nhạt vẫn đọc được
            b.StateDisabled.Back.Color1 = Color.FromArgb(245, 243, 255); b.StateDisabled.Back.Color2 = Color.FromArgb(245, 243, 255);
            b.StateDisabled.Border.Color1 = Color.FromArgb(221, 214, 254); b.StateDisabled.Border.Color2 = Color.FromArgb(221, 214, 254);
            b.StateDisabled.Content.ShortText.Color1 = Color.FromArgb(167, 139, 250);
            b.Cursor = Cursors.Hand;
        }
    }
}