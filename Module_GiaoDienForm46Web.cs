using System.Diagnostics;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CHO Form46_ThongKeThiDuaNamCu — CHỈ TRÌNH BÀY, KHÔNG CÓ LOGIC NGHIỆP VỤ
    //  Cùng ngôn ngữ thiết kế với Form15 / Form34 / Form4: nền trang xám nhạt, thẻ trắng bo góc, combo bo tròn,
    //  nút theme tím nhạt, bảng có khung bo tròn + tiêu đề tím nhạt + nhãn "STT" + vạch phân cách cột.
    //
    //  KHÔNG ĐỔI CODE GỐC: Form46 chỉ thêm đúng 1 dòng, ở CUỐI constructor (sau dòng đăng ký RowPostPaint):
    //        Module_GiaoDienForm46Web.ApDung(this);
    //  - Không sửa Designer, không đổi tên/kiểu/sự kiện của control nào; tìm control theo Name.
    //  - ApDungDinhDangGridThongKe (code gốc) tạo lại style lưới mỗi lần nạp -> lớp BoDinhDangLuoi lắng nghe sự kiện đổi style
    //    và áp lại màu (có chốt chống lặp). Chiều cao tiêu đề 85, độ rộng cột, màu nhấn từng cột vẫn do code gốc quyết định.
    //  - comboBox1_PhanLoaiThiDuaNamCu bị code gốc bật/tắt Visible nên GIỮ combo gốc (chỉ đổi font), không bọc bằng OComboWebF4.
    //  - Dùng lại TheBoGocF4 và OComboWebF4 trong Module_GiaoDienForm4Web.cs (file đó phải còn trong project).
    //  - Mọi bước bọc try/catch: lỗi trình bày không bao giờ làm form không mở được.
    // =====================================================================================
    internal static class Module_GiaoDienForm46Web
    {
        // ===================== TÙY CHỌN =====================
        private const bool HienTieuDeBang = false;   // true = khung bảng có dòng tiêu đề "4. Bảng thống kê..."
        private const bool HieuUngRoDong = true;     // dòng dưới chuột được tô nhẹ
        private const int CaoHangDieuKhien = 92;     // hàng thẻ trên: logo | Chọn CSDL | Khối điều khiển
        private const int CaoHangTimKiem = 76;       // hàng thẻ Tìm kiếm
        private const int KheHang = 8;               // khe giữa 2 hàng thẻ
        private const int RongLogo = 76;
        private const int RongTheCSDL = 340;
        private const int CaoDongBang = 36;
        private const int DemKhungBang = 5;
        // ===================== BẢNG MÀU =====================
        // --- Trang và chữ ---
        private static readonly Color MauNen = Color.FromArgb(244, 246, 250);          // nền trang
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);        // nhãn, chữ thanh trạng thái
        private static readonly Color MauVienO = Color.FromArgb(203, 213, 225);        // viền ô nhập
        private static readonly Color MauVienChon = Color.FromArgb(139, 92, 246);      // viền ô nhập khi đang gõ
        private static readonly Color MauVienTrangThai = Color.FromArgb(226, 232, 240);
        // --- Bảng ---
        private static readonly Color MauTieuDeBang = Color.FromArgb(238, 232, 255);   // nền tiêu đề cột
        private static readonly Color MauChuTieuDeBang = Color.FromArgb(91, 33, 182);  // chữ tiêu đề cột
        private static readonly Color MauVachCot = Color.FromArgb(208, 198, 247);      // vạch phân cách cột tiêu đề (tím nhạt)
        private static readonly Color MauNenHangSTT = Color.FromArgb(250, 248, 255);   // nền cột số thứ tự
        private static readonly Color MauChuSTT = Color.FromArgb(100, 116, 139);       // số thứ tự
        private static readonly Color MauChuBang = Color.FromArgb(51, 65, 85);         // chữ nội dung ô
        private static readonly Color MauKeBang = Color.FromArgb(235, 238, 243);       // đường kẻ giữa các dòng
        private static readonly Color MauChonDong = Color.FromArgb(239, 246, 255);     // nền dòng đang chọn
        private static readonly Color MauChuChon = Color.FromArgb(29, 78, 216);        // chữ dòng đang chọn
        private static readonly Color MauRoDong = Color.FromArgb(22, 109, 40, 217);    // lớp phủ mờ khi rê chuột qua dòng (có alpha)
        // --- Công cụ vẽ dùng chung (tạo 1 lần cho cả chương trình) ---
        private static readonly Pen ButVachCot = new Pen(MauVachCot, 1f);
        private static readonly SolidBrush CoRoDong = new SolidBrush(MauRoDong);
        private static readonly Font FontNhan = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontO = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point);
        // Bộ màu một nút: nền thường / rê chuột / nhấn / viền / chữ
        private readonly record struct BoMauNut(Color Nen, Color Hover, Color Down, Color Vien, Color Chu);
        private static readonly BoMauNut NutTim = new(
            Color.FromArgb(237, 233, 254), Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253),
            Color.FromArgb(196, 181, 253), Color.FromArgb(109, 40, 217));
        private static readonly BoMauNut NutTimDam = new(
            Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253), Color.FromArgb(167, 139, 250),
            Color.FromArgb(167, 139, 250), Color.FromArgb(91, 33, 182));
        private static void An(Action viec, string ten)
        {
            try { viec(); }
            catch (Exception ex) { Debug.WriteLine($"[Module_GiaoDienForm46Web.{ten}] {ex.Message}"); }
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
        // ===================== ĐIỂM VÀO =====================
        public static void ApDung(Form f)
        {
            f.SuspendLayout();
            try
            {
                f.BackColor = MauNen;
                An(() => DinhBoCuc(f), "BoCuc");
                An(() => BocCacThe(f), "CacThe");
                An(() => DinhLogo(f), "Logo");
                An(() => DinhBoLoc(f), "BoLoc");
                An(() => DinhKhoiDieuKhien(f), "KhoiDieuKhien");
                An(() => BoTronCacCombo(f), "Combo");
                An(() => BocLuoi(f), "Luoi");
                An(() => DinhThanhTrangThai(f), "TrangThai");
            }
            finally
            {
                f.ResumeLayout(true);
            }
        }
        private static int Px(Control c, int v) => (int)Math.Round(v * c.DeviceDpi / 96.0);
        // ===================== BỐ CỤC TỔNG =====================
        private static void DinhBoCuc(Form f)
        {
            BatDoubleBuffer(Tim<TableLayoutPanel>(f, "tableLayoutPanel1"));
            var goc = Tim<TableLayoutPanel>(f, "tableLayoutPanel1");
            if (goc != null)
            {
                goc.BackColor = MauNen;
                goc.Padding = new Padding(12, 10, 12, 4);
                goc.RowStyles.Clear();
                goc.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));     // nội dung
                goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));     // thanh trạng thái
            }
            var sc = Tim<SplitContainer>(f, "splitContainer1");
            if (sc != null)
            {
                sc.BackColor = MauNen;
                sc.Panel1.BackColor = MauNen;
                sc.Panel2.BackColor = MauNen;
                sc.Margin = new Padding(0);
                sc.IsSplitterFixed = true;
                sc.FixedPanel = FixedPanel.Panel1;
                try { sc.SplitterWidth = 10; } catch (ArgumentException) { }
                try { sc.SplitterDistance = Px(sc, CaoHangDieuKhien + KheHang + CaoHangTimKiem + 2); } catch (ArgumentException) { }
                BatDoubleBuffer(sc);
            }
            // Panel1: [hàng điều khiển (cố định) | hàng tìm kiếm (còn lại)]
            var t2 = Tim<TableLayoutPanel>(f, "tableLayoutPanel2");
            if (t2 != null)
            {
                t2.Margin = new Padding(0);
                t2.RowStyles.Clear();
                t2.RowStyles.Add(new RowStyle(SizeType.Absolute, Px(t2, CaoHangDieuKhien + KheHang)));
                t2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                BatDoubleBuffer(t2);
            }
            // tableLayoutPanel4: logo | thẻ Chọn CSDL | thẻ Khối điều khiển
            var t4 = Tim<TableLayoutPanel>(f, "tableLayoutPanel4");
            if (t4 != null)
            {
                t4.Margin = new Padding(0, 0, 0, KheHang);
                t4.ColumnStyles.Clear();
                t4.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Px(t4, RongLogo)));
                t4.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Px(t4, RongTheCSDL)));
                t4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                BatDoubleBuffer(t4);
            }
        }
        private static void BatDoubleBuffer(Control? c)
        {
            if (c == null) return;
            try
            {
                typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(c, true, null);
            }
            catch { }
        }
        private static void BocCacThe(Form f)
        {
            foreach (var ten in new[] { "groupBox3", "groupBox2", "groupBox1" })
            {
                var gb = Tim<GroupBox>(f, ten);
                if (gb != null) ThayGroupBoxBangThe(gb);
            }
            // Khe 10px giữa thẻ "Chọn CSDL" và thẻ "Khối điều khiển"
            var the3 = Tim<Control>(f, "the_groupBox3");
            if (the3 != null) the3.Margin = new Padding(0, 0, Px(the3, 10), 0);
        }
        /// <summary>
        /// Thay GroupBox (tiêu đề đỏ nghiêng) bằng TheBoGocF4 cùng vị trí. Chuyển nguyên các control con (kèm mọi sự kiện/logic)
        /// sang thẻ rồi bỏ GroupBox rỗng; code gốc không dùng tới các field groupBox*.
        /// </summary>
        private static void ThayGroupBoxBangThe(GroupBox gb)
        {
            var cha = gb.Parent;
            if (cha == null) return;
            var the = new TheBoGocF4
            {
                Name = "the_" + gb.Name,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                TieuDe = gb.Text,
                Padding = new Padding(12, 36, 12, 10)   // 36 = vùng tiêu đề thẻ
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
                gb.Dispose();
            }
            finally
            {
                the.ResumeLayout(false);
                cha.ResumeLayout(true);
            }
        }
        // ===================== LOGO =====================
        private static void DinhLogo(Form f)
        {
            var pb = Tim<PictureBox>(f, "pictureBox1");
            if (pb == null) return;
            pb.SizeMode = PictureBoxSizeMode.Zoom;      // bản gốc StretchImage làm méo logo khi đổi cỡ
            pb.BackColor = Color.Transparent;
            pb.Margin = new Padding(0, 0, 8, 0);
            pb.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        }
        // ===================== THẺ 1: CHỌN CSDL =====================
        // (nằm trong DinhKhoiDieuKhien)
        // ===================== THẺ 3: TÌM KIẾM THÔNG TIN CBCS =====================
        private static void DinhBoLoc(Form f)
        {
            // Họ và tên | ô tên | Đơn vị | combo | Tình trạng | combo | Phân loại | combo | nút làm mới
            var t5 = Tim<TableLayoutPanel>(f, "tableLayoutPanel5");
            if (t5 != null)
            {
                t5.Margin = new Padding(0);
                t5.ColumnStyles.Clear();
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // Họ và tên
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));      // ô tên
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // Đơn vị
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));      // combo đơn vị
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // Tình trạng
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));      // combo tình trạng
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // Phân loại
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24F));      // combo phân loại
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Px(t5, 56)));   // nút làm mới
                BatDoubleBuffer(t5);
            }
            DinhNhan(Tim<Label>(f, "label3"), dauHang: true);   // Họ và tên
            DinhNhan(Tim<Label>(f, "label4"));                  // Đơn vị
            DinhNhan(Tim<Label>(f, "label1"));                  // Tình trạng
            DinhNhan(Tim<Label>(f, "label2_PhanLoai"));         // Phân loại
            // Ô tìm tên: code gốc dùng Text + ForeColor xám làm chữ gợi ý => KHÔNG đụng tới màu/font chữ, chỉ đổi khung
            var o = Tim<Krypton.Toolkit.KryptonTextBox>(f, "textBox_TimKiemTheoTen");
            if (o != null)
            {
                o.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                o.Margin = new Padding(0);
                o.StateCommon.Back.Color1 = Color.White;
                KieuVienO(o.StateCommon.Border, o.StateActive.Border);
            }
            foreach (var ten in new[] { "comboBox_TimKiemDonVi", "comboBox1_TinhTrang", "comboBox1_PhanLoaiThiDuaNamCu", "comboBox_ChonCSDLNam" })
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
                lamMoi.Size = new Size(Px(lamMoi, 40), Px(lamMoi, 32));
            }
        }
        // ===================== THẺ 1 + 2: CHỌN CSDL, KHỐI ĐIỀU KHIỂN =====================
        private static void DinhKhoiDieuKhien(Form f)
        {
            // Thẻ "1. Chọn CSDL đã lưu theo năm": nhãn (AutoSize) | combo (co giãn)
            var t6 = Tim<TableLayoutPanel>(f, "tableLayoutPanel6");
            if (t6 != null)
            {
                t6.Margin = new Padding(0);
                t6.ColumnStyles.Clear();
                t6.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                t6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                BatDoubleBuffer(t6);
            }
            DinhNhan(Tim<Label>(f, "label5"), dauHang: true);
            var cs = Tim<ComboBox>(f, "comboBox_ChonCSDLNam");
            if (cs != null)
            {
                cs.Font = FontO;
                cs.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                cs.Margin = new Padding(0);
            }
            // Thẻ "2. Khối điều khiển": 3 nút xếp ngang
            var t3 = Tim<TableLayoutPanel>(f, "tableLayoutPanel3");
            if (t3 != null)
            {
                t3.Margin = new Padding(0);
                t3.ColumnStyles.Clear();
                t3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44F));   // Xem kết quả thi đua tập thể (chữ dài nhất)
                t3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));   // Xuất dữ liệu
                t3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));   // Làm mới
                BatDoubleBuffer(t3);
            }
            NutTrongO(f, "kryptonButton1_XemKetQuaThiDuaTapThe", NutTimDam, new Padding(0, 0, 6, 0));
            NutTrongO(f, "kryptonButton_XuatData", NutTim, new Padding(3, 0, 3, 0));
            NutTrongO(f, "kryptonButton_CapNhat", NutTimDam, new Padding(6, 0, 0, 0));
        }
        // ===================== COMBO BO TRÒN (dùng OComboWebF4 của Form4) =====================
        private static void BoTronCacCombo(Form f)
        {
            foreach (var cb in TatCa<ComboBox>(f).ToList())   // ToList: Gan thêm control vào thẻ trong lúc duyệt
            {
                if (cb.GetType() != typeof(ComboBox) || cb.DropDownStyle != ComboBoxStyle.DropDownList) continue;
                if (cb.Name == "comboBox1_PhanLoaiThiDuaNamCu") GanComboAnHien(cb);   // combo bị code gốc bật/tắt Visible
                else OComboWebF4.Gan(cb);
            }
        }
        /// <summary>
        /// Bo tròn combo mà code gốc hay bật/tắt Visible (Form46_... đặt Visible = false/true theo loại CSDL).
        /// Ô bo tròn là control bọc/đi kèm combo, nên khi combo ẩn thì ô bo tròn phải ẩn theo. Không dùng Visible của ô bọc
        /// (đang ẩn thì code gốc đặt combo.Visible = true sẽ không có sự kiện nào báo lại) mà dùng Region rỗng: ô bọc luôn
        /// "hiện" nhưng không vẽ gì và không nhận chuột; combo.VisibleChanged đổi Region theo trạng thái thật của combo.
        /// </summary>
        private static void GanComboAnHien(ComboBox cb)
        {
            var chaCu = cb.Parent;
            var truoc = chaCu?.Controls.Cast<Control>().ToHashSet() ?? new HashSet<Control>();
            OComboWebF4.Gan(cb);
            Control? vo = cb.Parent != chaCu ? cb.Parent                                  // ô bo tròn bọc combo
                        : chaCu?.Controls.Cast<Control>().FirstOrDefault(c => !truoc.Contains(c));   // hoặc ô bo tròn thêm cạnh combo
            if (vo == null || vo == cb) { Debug.WriteLine("[Module_GiaoDienForm46Web] Không tìm thấy ô bo tròn của " + cb.Name); return; }
            var rong = new Region(Rectangle.Empty);
            void DongBo()
            {
                bool hien = cb.Visible;                    // Visible hiệu dụng: cũng false khi cả form đang ẩn -> tự phục hồi khi hiện lại
                vo.Region = hien ? null : rong;
                vo.Enabled = hien;
            }
            cb.VisibleChanged += (_, _) => DongBo();
            cb.ParentChanged += (_, _) => DongBo();
            DongBo();
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
                TieuDe = HienTieuDeBang ? "4. Bảng thống kê kết quả thi đua năm cũ" : "",
                DemKhung = DemKhungBang,
                Padding = HienTieuDeBang
                    ? new Padding(12 + DemKhungBang, 36 + DemKhungBang, 12 + DemKhungBang, 10 + DemKhungBang)
                    : new Padding(10 + DemKhungBang),
                LuoiBang = luoi
            };
            cha.SuspendLayout();
            try
            {
                cha.Controls.Remove(luoi);
                cha.Controls.Add(the);
                the.Controls.Add(luoi);
                luoi.Dock = DockStyle.Fill;
            }
            finally
            {
                cha.ResumeLayout(true);
            }
            _ = new BoDinhDangLuoi(luoi, the);
        }
        /// <summary>
        /// Áp màu/kiểu mới lên lưới và GIỮ nó sau mỗi lần code gốc định dạng lại; vẽ nhãn "STT", vạch phân cách cột tiêu đề
        /// và hiệu ứng rê chuột theo dòng. Chỉ đụng tới phần trình bày, không đọc hay ghi dữ liệu của lưới.
        /// </summary>
        private sealed class BoDinhDangLuoi
        {
            private readonly DataGridView _luoi;
            private readonly TheBoGocF4 _the;
            private bool _dangDap;        // chốt chống lặp: chính việc đặt màu cũng phát sự kiện đổi style
            private bool _kryptonXong;    // phần Krypton chỉ cần đặt 1 lần
            private int _dongRo = -1;     // dòng đang bị rê chuột
            public BoDinhDangLuoi(DataGridView luoi, TheBoGocF4 the)
            {
                _luoi = luoi;
                _the = the;
                _luoi.ColumnHeadersDefaultCellStyleChanged += (_, _) => DapKieu();
                _luoi.DefaultCellStyleChanged += (_, _) => DapKieu();
                _luoi.AlternatingRowsDefaultCellStyleChanged += (_, _) => DapKieu();
                _luoi.RowHeadersDefaultCellStyleChanged += (_, _) => DapKieu();
                _luoi.ColumnHeadersHeightChanged += (_, _) => _the.Invalidate();
                // Chốt cuối: code gốc kết thúc ApDungDinhDangGridThongKe bằng ResumeLayout() -> sự kiện Layout; chỉ áp lại khi bị ghi đè
                _luoi.Layout += (_, _) => { if (!DaDungKieu()) DapKieu(); };
                // Vẽ chồng SAU khi lưới đã tự vẽ xong => không can thiệp vào cách lưới xếp chữ tiêu đề nhiều dòng
                _luoi.Paint += VeTieuDe;
                if (HieuUngRoDong)
                {
                    _luoi.CellMouseEnter += (_, e) => DatDongRo(e.RowIndex);
                    _luoi.MouseLeave += (_, _) => DatDongRo(-1);
                    _luoi.Scroll += (_, _) => DatDongRo(-1);   // cuộn bằng bánh xe: chuột đứng yên nhưng dòng dưới chuột đã đổi
                    _luoi.RowPostPaint += VeLopRoDong;
                }
                DapKieu();
            }
            private void DapKieu()
            {
                if (_dangDap) return;
                _dangDap = true;
                try
                {
                    var h = _luoi.ColumnHeadersDefaultCellStyle;
                    h.BackColor = MauTieuDeBang;
                    h.ForeColor = MauChuTieuDeBang;
                    h.SelectionBackColor = MauTieuDeBang;
                    h.SelectionForeColor = MauChuTieuDeBang;
                    _luoi.GridColor = MauKeBang;
                    _luoi.DefaultCellStyle.ForeColor = MauChuBang;
                    _luoi.DefaultCellStyle.SelectionBackColor = MauChonDong;
                    _luoi.DefaultCellStyle.SelectionForeColor = MauChuChon;
                    _luoi.AlternatingRowsDefaultCellStyle.BackColor = Color.White;   // bản gốc 252,252,252: không tô vạch xen kẽ như Form4
                    _luoi.RowHeadersDefaultCellStyle.BackColor = MauNenHangSTT;
                    _luoi.RowHeadersDefaultCellStyle.ForeColor = MauChuSTT;          // số thứ tự do RowPostPaint của form vẽ bằng màu này
                    _luoi.RowTemplate.Height = CaoDongBang;                          // đặt SAU khi code gốc đặt 32, và trước khi các dòng được tạo
                    if (!_kryptonXong && _luoi is Krypton.Toolkit.KryptonDataGridView k)
                    {
                        _kryptonXong = true;
                        StyleKrypton(k);
                    }
                    _the.Invalidate();   // dải tiêu đề của thẻ đọc màu này lúc vẽ
                }
                finally { _dangDap = false; }
            }
            private bool DaDungKieu() =>
                _luoi.ColumnHeadersDefaultCellStyle.BackColor == MauTieuDeBang &&
                _luoi.DefaultCellStyle.SelectionBackColor == MauChonDong &&
                _luoi.AlternatingRowsDefaultCellStyle.BackColor == Color.White &&
                _luoi.RowTemplate.Height == CaoDongBang;
            // Phần Krypton của lưới: tiêu đề phẳng, đường kẻ nhạt. Chỉ đặt 1 lần (code gốc không đụng tới các state này).
            private static void StyleKrypton(Krypton.Toolkit.KryptonDataGridView k)
            {
                k.GridStyles.Style = Krypton.Toolkit.DataGridViewStyle.List;
                k.StateCommon.Background.Color1 = Color.White;
                k.StateCommon.HeaderColumn.Content.Color1 = MauChuTieuDeBang;
                k.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
                k.StateCommon.DataCell.Border.Color1 = MauKeBang;
                k.StateCommon.DataCell.Border.Width = 1;
                k.StateCommon.DataCell.Content.Color1 = MauChuBang;
                k.StateCommon.HeaderRow.Back.Color1 = MauNenHangSTT;
                k.StateCommon.HeaderRow.Back.Color2 = MauNenHangSTT;
                k.StateCommon.HeaderRow.Border.Color1 = Color.FromArgb(237, 233, 254);
                k.StateSelected.DataCell.Back.Color1 = MauChonDong;
                k.StateSelected.DataCell.Back.Color2 = MauChonDong;
                k.StateSelected.DataCell.Content.Color1 = MauChuChon;
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
            // Nhãn "STT" ở ô góc + vạch kẻ dọc giữa các cột tiêu đề + đường kẻ dưới hàng tiêu đề.
            // Chỉ chạy khi vùng cần vẽ chạm hàng tiêu đề (cuộn dọc không đụng tới) và chỉ duyệt các cột đang hiện.
            private void VeTieuDe(object? sender, PaintEventArgs e)
            {
                var g = _luoi;
                if (!g.ColumnHeadersVisible || e.ClipRectangle.Top >= g.ColumnHeadersHeight) return;
                try
                {
                    var gr = e.Graphics;
                    if (g.RowHeadersVisible)
                    {
                        var goc = g.GetCellDisplayRectangle(-1, -1, false);   // ô góc, phía trên cột số thứ tự
                        if (goc.Width > 0 && goc.Height > 0)
                        {
                            var font = g.ColumnHeadersDefaultCellStyle.Font ?? g.Font;
                            TextRenderer.DrawText(gr, "STT", font, goc, MauChuTieuDeBang,
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
                            gr.DrawLine(ButVachCot, goc.Right - 1, goc.Top, goc.Right - 1, goc.Bottom - 1);
                        }
                    }
                    foreach (DataGridViewColumn c in g.Columns)
                    {
                        if (!c.Visible) continue;
                        var r = g.GetCellDisplayRectangle(c.Index, -1, false);   // false: giữ mép phải thật kể cả khi cột bị cuộn lấp một phần
                        if (r.Width <= 0 || r.Height <= 0) continue;              // cột đang nằm ngoài vùng nhìn thấy
                        gr.DrawLine(ButVachCot, r.Right - 1, r.Top, r.Right - 1, r.Bottom - 1);
                    }
                    int y = g.ColumnHeadersHeight - 1;
                    gr.DrawLine(ButVachCot, 0, y, g.ClientSize.Width, y);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[Module_GiaoDienForm46Web.TieuDe] " + ex.Message);   // lỗi vẽ thêm: bỏ qua, lưới vẫn hiển thị bình thường
                }
            }
            // ----- Hiệu ứng rê chuột theo dòng -----
            private void DatDongRo(int dong)
            {
                if (dong == _dongRo) return;
                int cu = _dongRo;
                _dongRo = dong;
                if (cu >= 0 && cu < _luoi.RowCount) _luoi.InvalidateRow(cu);
                if (dong >= 0 && dong < _luoi.RowCount) _luoi.InvalidateRow(dong);
            }
            // Phủ lớp màu mờ lên cả dòng (kể cả cột có nền xanh riêng); dòng đang chọn giữ nguyên màu chọn.
            // GetRowState không làm "bung" dòng dùng chung nên an toàn với lưới VirtualMode nhiều dòng.
            private void VeLopRoDong(object? sender, DataGridViewRowPostPaintEventArgs e)
            {
                if (e.RowIndex != _dongRo) return;
                if ((_luoi.Rows.GetRowState(e.RowIndex) & DataGridViewElementStates.Selected) != 0) return;
                e.Graphics.FillRectangle(CoRoDong, e.RowBounds);
            }
        }
        // ===================== THANH TRẠNG THÁI =====================
        private static void DinhThanhTrangThai(Form f)
        {
            var ss = Tim<StatusStrip>(f, "statusStrip1");
            if (ss == null) return;
            ss.Renderer = new TrangThaiPhang();
            ss.BackColor = Color.White;           // bản gốc: xanh lá nhạt
            ss.ForeColor = MauChuNhan;
            ss.SizingGrip = false;
            ss.Padding = new Padding(10, 0, 10, 0);
            ss.Margin = new Padding(0, 6, 0, 0);
            var it = ss.Items.Find("toolStripStatusLabel1", false).FirstOrDefault();
            if (it != null) { it.Font = FontNhan; it.ForeColor = MauChuNhan; }
        }
        // Thanh trạng thái phẳng (chỉ vẽ nền + viền); không đụng tới nhãn, thanh tiến trình hay logic của form
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
                using var p = new Pen(MauVienTrangThai, 1f);
                e.Graphics.DrawRectangle(p, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            }
        }
        // ===================== TIỆN ÍCH TRÌNH BÀY =====================
        private static void DinhNhan(Label? lb, bool dauHang = false)
        {
            if (lb == null) return;
            lb.Font = FontNhan;                       // bản gốc: chữ nghiêng xanh đậm
            lb.ForeColor = MauChuNhan;
            lb.AutoSize = true;                       // rộng đúng bằng chữ, đi cùng cột AutoSize
            lb.MaximumSize = Size.Empty;              // không giới hạn bề rộng => không tự xuống dòng
            lb.TextAlign = ContentAlignment.MiddleLeft;
            lb.UseMnemonic = false;
            lb.Anchor = AnchorStyles.Left;
            lb.Margin = new Padding(dauHang ? 0 : 16, 0, 8, 0);
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
        private static void NutTrongO(Form f, string ten, BoMauNut m, Padding margin)
        {
            var b = Tim<Krypton.Toolkit.KryptonButton>(f, ten);
            if (b == null) return;
            KieuNut(b, m);
            b.Anchor = AnchorStyles.Left | AnchorStyles.Right;   // nút lấp đầy chiều ngang ô, cao đều 32
            b.Margin = margin;
            b.Height = 32;
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
            // Nút đang giữ focus / nút mặc định: ép giống trạng thái thường để không bị chữ trắng nền trắng
            foreach (var ov in new[] { b.OverrideFocus, b.OverrideDefault })
            {
                ov.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
                ov.Back.Color1 = m.Nen; ov.Back.Color2 = m.Nen;
                ov.Border.Color1 = m.Vien; ov.Border.Color2 = m.Vien;
                ov.Content.ShortText.Color1 = m.Chu;
            }
            // Nút bị khóa (vd. đang đồng bộ): tím rất nhạt, chữ tím nhạt vẫn đọc được
            b.StateDisabled.Back.Color1 = Color.FromArgb(245, 243, 255); b.StateDisabled.Back.Color2 = Color.FromArgb(245, 243, 255);
            b.StateDisabled.Border.Color1 = Color.FromArgb(221, 214, 254); b.StateDisabled.Border.Color2 = Color.FromArgb(221, 214, 254);
            b.StateDisabled.Content.ShortText.Color1 = Color.FromArgb(167, 139, 250);
            b.Cursor = Cursors.Hand;
        }
    }
}