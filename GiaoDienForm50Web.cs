using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
using System.Diagnostics;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CHO Form50_QuanLyKhenThuongNamCu — CHỈ TRÌNH BÀY, KHÔNG CÓ LOGIC NGHIỆP VỤ
    //  Cùng ngôn ngữ thiết kế với Form4 / Form10 / Form15 / Form25 / Form34: nền trang xám nhạt, thẻ trắng bo góc + thanh nhấn xanh
    //  ở tiêu đề, combo bo tròn, nút theme tím nhạt, bảng có khung bo tròn liền mạch + tiêu đề tím nhạt.
    //
    //  KHÔNG ĐỔI CODE GỐC: Form50 chỉ thêm đúng 1 dòng trong constructor (ngay sau InitializeComponent):
    //        GiaoDienForm50Web.ApDung(this);
    //  - Không sửa Designer, không đổi tên/kiểu/sự kiện của control nào; tìm control theo Name.
    //  - Code gốc định dạng lưới trong Load (CauHinhGridCoBan tạo lại style tiêu đề/dòng, CauHinhCotGrid tạo cột). Helper không sửa
    //    các hàm đó mà lắng nghe sự kiện đổi style / thêm cột / layout của lưới và áp lại màu ngay sau đó (có chốt chống lặp),
    //    nên màu mới luôn là màu cuối cùng. Cột, độ rộng, font, chế độ ảo (VirtualMode) vẫn do code gốc quyết định.
    //  - Ô tìm tên: code gốc dùng chính Text + ForeColor xám làm chữ gợi ý nên helper KHÔNG đụng tới màu/font chữ của ô này.
    //  - Dùng lại lớp thẻ TheBoGocF4 và ô combo bo tròn OComboWebF4 trong GiaoDienForm4Web.cs (file đó phải còn trong project).
    //  - Mọi bước bọc try/catch: lỗi trình bày không bao giờ làm form không mở được.
    // =====================================================================================
    internal static class GiaoDienForm50Web
    {
        // ---------- Bảng màu / font: lấy đúng giá trị của Form4 ----------
        private static readonly Color MauNen = Color.FromArgb(244, 246, 250);
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);
        private static readonly Color MauChuDam = Color.FromArgb(30, 41, 59);
        private static readonly Color MauVienO = Color.FromArgb(203, 213, 225);
        private static readonly Color MauVienChon = Color.FromArgb(139, 92, 246);
        private static readonly Color MauTieuDeBang = Color.FromArgb(238, 232, 255);
        private static readonly Color MauChuTieuDeBang = Color.FromArgb(75, 20, 130);
        private static readonly Color MauKeBang = Color.FromArgb(235, 238, 243);
        private static readonly Color MauChonDong = Color.FromArgb(239, 246, 255);

        private static readonly Font FontNhan = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontO = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point);

        private const int DemKhungBang = 5;         // khoảng đệm giữa khung bo tròn và bảng (px thiết kế)
        private const int CaoKhoiTren = 196;        // chiều cao khối 2 hàng thẻ phía trên (mỗi hàng ~98px gồm cả khe 5px)

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
                An(() => DinhTheChonCsdl(f), "TheChonCsdl");
                An(() => DinhTheDieuKhien(f), "TheDieuKhien");
                An(() => DinhTheTimKiem(f), "TheTimKiem");
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
            catch (Exception ex) { Debug.WriteLine($"[GiaoDienForm50Web.{ten}] {ex.Message}"); }
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
            // Hàng 0 = khối thẻ phía trên (cao cố định), hàng 1 = thẻ bảng (ăn hết), hàng 2 = thanh trạng thái (cao cố định).
            // Bản gốc chia theo % (24% / 72% / 4%) nên mọi thứ cao thấp theo cỡ cửa sổ.
            var t1 = Tim<TableLayoutPanel>(f, "tableLayoutPanel1");
            if (t1 != null)
            {
                t1.BackColor = MauNen;
                t1.Margin = new Padding(0);
                t1.Padding = new Padding(12, 10, 12, 4);
                t1.RowStyles.Clear();
                t1.RowStyles.Add(new RowStyle(SizeType.Absolute, CaoKhoiTren));
                t1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                t1.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            }

            // 2 hàng thẻ chia đôi khối trên: [ảnh | Chọn CSDL | Khối điều khiển] rồi [Tìm kiếm]
            var t2 = Tim<TableLayoutPanel>(f, "tableLayoutPanel2");
            if (t2 != null)
            {
                t2.BackColor = MauNen;
                t2.Margin = new Padding(0);
                t2.ColumnStyles.Clear();
                t2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                t2.RowStyles.Clear();
                t2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                t2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            }

            var t4 = Tim<TableLayoutPanel>(f, "tableLayoutPanel4");
            if (t4 != null)
            {
                t4.BackColor = MauNen;
                t4.Margin = new Padding(0);
                t4.ColumnStyles.Clear();
                t4.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124F));   // ảnh
                t4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));     // Chọn CSDL
                t4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));     // Khối điều khiển
            }
        }

        // ===================== THẺ =====================
        private static void BocCacThe(Form f)
        {
            // Hàng thẻ trên: ảnh | 1. Chọn CSDL | 2. Khối điều khiển  (khe 10px ngang, 5px dọc)
            var anh = Tim<PictureBox>(f, "pictureBox1");
            if (anh != null)
            {
                var the = ThayBangThe(anh, "", new Padding(8));
                if (the != null)
                {
                    anh.Dock = DockStyle.Fill;
                    anh.Margin = new Padding(0);
                    anh.SizeMode = PictureBoxSizeMode.Zoom;   // trước đây StretchImage -> méo hình
                }
            }
            var gb3 = Tim<GroupBox>(f, "groupBox3");
            if (gb3 != null) ThayBangThe(gb3, gb3.Text, new Padding(12, 36, 12, 8));
            var gb2 = Tim<GroupBox>(f, "groupBox2");
            if (gb2 != null) ThayBangThe(gb2, gb2.Text, new Padding(12, 36, 12, 8));

            // Hàng thẻ dưới: 3. Tìm kiếm thông tin CBCS (cả bề ngang)
            var gb1 = Tim<GroupBox>(f, "groupBox1");
            if (gb1 != null) ThayBangThe(gb1, gb1.Text, new Padding(12, 36, 12, 8));
        }

        /// <summary>
        /// Thay GroupBox / PictureBox nằm trong ô TableLayoutPanel bằng TheBoGocF4 (thẻ trắng bo góc của Form4) đúng ô đó.
        /// GroupBox: chuyển nguyên các control con (kèm mọi sự kiện/logic) sang thẻ rồi bỏ GroupBox rỗng
        /// (code gốc không dùng tới các field groupBox*). PictureBox: đưa chính nó vào thẻ.
        /// </summary>
        private static TheBoGocF4? ThayBangThe(Control nguon, string tieuDe, Padding padding)
        {
            if (nguon.Parent is not TableLayoutPanel cha) return null;

            var vt = cha.GetPositionFromControl(nguon);
            int spanCot = cha.GetColumnSpan(nguon), spanHang = cha.GetRowSpan(nguon);
            bool cotCuoi = vt.Column + spanCot >= cha.ColumnCount;
            bool hangTren = vt.Row == 0;

            var the = new TheBoGocF4
            {
                Name = "the_" + nguon.Name,
                Dock = DockStyle.Fill,
                // Khe 10px giữa các thẻ cùng hàng; khe 5px + 5px giữa 2 hàng thẻ (hàng trên cách dưới 5, hàng dưới cách trên 5)
                Margin = new Padding(0, 0, cotCuoi ? 0 : 10, 0),
                TieuDe = tieuDe,
                Padding = padding
            };
            if (cha.Name == "tableLayoutPanel4") the.Margin = new Padding(0, 0, cotCuoi ? 0 : 10, 5);   // hàng thẻ phía trên
            else if (cha.Name == "tableLayoutPanel2" && !hangTren) the.Margin = new Padding(0, 5, 0, 0); // hàng thẻ tìm kiếm

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
                    gb.Dispose();
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

        // ===================== THẺ 1: CHỌN CSDL KHEN THƯỞNG ĐÃ LƯU THEO NĂM =====================
        private static void DinhTheChonCsdl(Form f)
        {
            var t6 = Tim<TableLayoutPanel>(f, "tableLayoutPanel6");
            if (t6 != null)
            {
                t6.Margin = new Padding(0);
                t6.ColumnStyles.Clear();   // bản gốc còn dư 1 kiểu cột Absolute 20 không có cột thật
                t6.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84F));
                t6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            }
            DinhNhan(Tim<Label>(f, "label5"), true);   // Chọn CSDL

            var cb = Tim<ComboBox>(f, "comboBox_ChonCSDLNam");
            if (cb != null)
            {
                cb.Font = FontO;                       // phải đặt TRƯỚC khi gắn ô bo tròn (nó chép font của ComboBox)
                cb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                cb.Margin = new Padding(0);
            }
        }

        // ===================== THẺ 2: KHỐI ĐIỀU KHIỂN =====================
        private static void DinhTheDieuKhien(Form f)
        {
            // Nút canh trái theo cột cố định thay vì kéo giãn nửa bề ngang thẻ (thẻ này rộng hơn 600px)
            var t3 = Tim<TableLayoutPanel>(f, "tableLayoutPanel3");
            if (t3 != null)
            {
                t3.Margin = new Padding(0);
                t3.ColumnStyles.Clear();
                t3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 156F));
                t3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
                t3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            }
            NutTrongO(f, "kryptonButton_CapNhat", NutTimDam, new Padding(0, 0, 12, 0));   // Làm mới
            NutTrongO(f, "kryptonButton_XuatData", NutTim, new Padding(0));                 // Xuất dữ liệu
        }

        // ===================== THẺ 3: TÌM KIẾM THÔNG TIN CBCS =====================
        private static void DinhTheTimKiem(Form f)
        {
            // Họ và tên | ô tên | Đơn vị | combo | Hình thức | combo | nút làm mới (chỉ có biểu tượng)
            var t5 = Tim<TableLayoutPanel>(f, "tableLayoutPanel5");
            if (t5 != null)
            {
                t5.Margin = new Padding(0);
                t5.ColumnStyles.Clear();   // bản gốc còn dư 2 kiểu cột Absolute 20 không có cột thật
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76F));
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84F));
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26F));
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56F));
            }

            DinhNhan(Tim<Label>(f, "label3"), true);   // Họ và tên
            DinhNhan(Tim<Label>(f, "label4"));         // Đơn vị
            DinhNhan(Tim<Label>(f, "label1"));         // Hình thức

            // Ô tìm tên: chỉ đổi khung (không đụng màu/font chữ vì code gốc dùng nó làm chữ gợi ý xám)
            var o = Tim<Krypton.Toolkit.KryptonTextBox>(f, "textBox_TimKiemTheoTen");
            if (o != null)
            {
                o.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                o.Margin = new Padding(0);
                o.StateCommon.Back.Color1 = Color.White;
                KieuVienO(o.StateCommon.Border, o.StateActive.Border);
            }

            foreach (var ten in new[] { "comboBox_TimKiemDonVi", "comboBox1_HinhThucKT" })
            {
                var cb = Tim<ComboBox>(f, ten);
                if (cb == null) continue;
                cb.Font = FontO;
                cb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                cb.Margin = new Padding(0);
            }

            // Nút làm mới các ô tìm kiếm: chỉ có biểu tượng -> nút nhỏ bo góc
            var lamMoi = Tim<Krypton.Toolkit.KryptonButton>(f, "kryptonButton_LamMoiCacOTimKiem");
            if (lamMoi != null)
            {
                KieuNut(lamMoi, NutTim);
                lamMoi.Anchor = AnchorStyles.None;
                lamMoi.Margin = new Padding(0);
                lamMoi.Size = new Size(40, 32);
            }
        }

        // ===================== COMBO BO TRÒN (dùng OComboWebF4 của Form4) =====================
        // Combo gốc vẫn tồn tại + hoạt động (chỉ bị ẩn hình): DataSource, SelectedIndexChanged và mọi code gốc chạy như cũ.
        private static void BoTronCacCombo(Form f)
        {
            foreach (var cb in TatCa<ComboBox>(f).ToList())   // ToList: Gan thêm control vào thẻ trong lúc duyệt
                if (cb.GetType() == typeof(ComboBox))
                    OComboWebF4.Gan(cb);
        }

        // ===================== LƯỚI =====================
        // GroupBox "4. Thông tin khen thưởng năm cũ của CBCS" -> thẻ; bảng nằm trong khung bo tròn liền mạch (không cắt góc bằng Region):
        // thẻ vẽ khung + dải tiêu đề cùng màu tiêu đề bảng.
        private static void BocLuoi(Form f)
        {
            var gb = Tim<GroupBox>(f, "groupBox5");
            var luoi = Tim<DataGridView>(f, "kryptonDataGridView1");
            if (gb == null || luoi == null || gb.Parent is not TableLayoutPanel cha) return;

            var vt = cha.GetPositionFromControl(gb);
            const int gap = DemKhungBang;
            var the = new TheBoGocF4
            {
                Name = "the_groupBox5",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 10, 0, 0),
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
                gb.Dispose();   // GroupBox + bảng bố cục rỗng bên trong
            }
            finally
            {
                the.ResumeLayout(false);
                cha.ResumeLayout(true);
            }

            // ---- Áp màu mới lên lưới, và GIỮ nó sau mỗi lần code gốc định dạng lại (Load) ----
            bool dangDap = false;
            bool kryptonXong = false;
            void DapKieu()
            {
                if (dangDap) return;                 // chốt chống lặp: chính việc đặt màu cũng phát sự kiện đổi style
                dangDap = true;
                try
                {
                    var h = luoi.ColumnHeadersDefaultCellStyle;
                    h.BackColor = MauTieuDeBang;
                    h.ForeColor = MauChuTieuDeBang;
                    h.SelectionBackColor = MauTieuDeBang;
                    h.SelectionForeColor = MauChuTieuDeBang;
                    luoi.GridColor = MauKeBang;
                    luoi.DefaultCellStyle.SelectionBackColor = MauChonDong;
                    luoi.DefaultCellStyle.SelectionForeColor = MauChuDam;
                    luoi.AlternatingRowsDefaultCellStyle.BackColor = Color.White;   // bản gốc 252,252,252: không tô vạch xen kẽ như Form4
                    luoi.RowHeadersDefaultCellStyle.BackColor = MauTieuDeBang;
                    luoi.RowHeadersDefaultCellStyle.ForeColor = MauChuTieuDeBang;
                    if (!kryptonXong && luoi is Krypton.Toolkit.KryptonDataGridView k)
                    {
                        kryptonXong = true;
                        StyleKryptonLuoi(k);
                    }
                    the.Invalidate();                // dải tiêu đề của thẻ đọc màu này lúc vẽ
                }
                finally { dangDap = false; }
            }
            bool DaDungKieu() =>
                luoi.ColumnHeadersDefaultCellStyle.BackColor == MauTieuDeBang &&
                luoi.DefaultCellStyle.SelectionBackColor == MauChonDong &&
                luoi.AlternatingRowsDefaultCellStyle.BackColor == Color.White;

            luoi.ColumnHeadersDefaultCellStyleChanged += (s, e) => DapKieu();
            luoi.DefaultCellStyleChanged += (s, e) => DapKieu();
            luoi.AlternatingRowsDefaultCellStyleChanged += (s, e) => DapKieu();
            luoi.RowHeadersDefaultCellStyleChanged += (s, e) => DapKieu();
            luoi.ColumnHeadersHeightChanged += (s, e) => the.Invalidate();
            // Chốt cuối: sau khi code gốc tạo cột / layout xong, chỉ áp lại khi bị ghi đè
            luoi.ColumnAdded += (s, e) => { if (!DaDungKieu()) DapKieu(); };
            luoi.Layout += (s, e) => { if (!DaDungKieu()) DapKieu(); };
            f.Shown += (s, e) => { if (!DaDungKieu()) DapKieu(); };
            DapKieu();
        }

        // Phần Krypton của lưới: tiêu đề phẳng, đường kẻ nhạt. Chỉ đặt 1 lần (code gốc không đụng tới các state này).
        private static void StyleKryptonLuoi(Krypton.Toolkit.KryptonDataGridView k)
        {
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
            ss.Margin = new Padding(0, 6, 0, 0);   // chừa khe giữa thẻ bảng và thanh trạng thái
            // Chỉ đổi font/màu chữ: căn lề, padding, hiện/ẩn nhãn do code gốc điều khiển (CauHinhStatusStrip / CapNhatTrangThaiHienThi)
            foreach (var ten in new[] { "toolStripStatusLabel1", "toolStripStatusLabel2_TongSoCBCSDuocKhenThuong" })
            {
                var it = ss.Items.Find(ten, false).FirstOrDefault();
                if (it != null) { it.Font = FontNhan; it.ForeColor = MauChuNhan; }
            }
        }

        // ===================== TIỆN ÍCH TRÌNH BÀY =====================
        private static void DinhNhan(Label? lb, bool dauHang = false)
        {
            if (lb == null) return;
            lb.Font = FontNhan;                       // bản gốc: chữ nghiêng xanh đậm
            lb.ForeColor = MauChuNhan;
            lb.Anchor = AnchorStyles.Left;
            lb.Margin = new Padding(dauHang ? 0 : 12, 0, 6, 0);
        }

        // Viền ô nhập Krypton: xám bo góc 8, khi đang gõ chuyển tím (cùng màu viền combo đang chọn)
        private static void KieuVienO(Krypton.Toolkit.PaletteBorder vien, Krypton.Toolkit.PaletteBorder vienActive)
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
            // Nút đang giữ focus / nút mặc định: ép giống trạng thái thường để không bị chữ trắng nền trắng
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