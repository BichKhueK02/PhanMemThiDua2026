using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CHO Form57_CapNhatTinhThanhPho — CHỈ TRÌNH BÀY, KHÔNG CÓ LOGIC NGHIỆP VỤ
    //  - Lớp static độc lập (không phải form) => không tạo .resx, KHÔNG sửa .csproj, KHÔNG sửa Designer.
    //  - Form57 chỉ thêm 1 dòng trong constructor, ngay SAU InitializeComponent();
    //        Form57:  Module_GiaoDienForm57Web.ApDung(this);
    //  - Tìm control theo Name => không đổi tên/kiểu/sự kiện của control nào; ô nhập, 5 nút, bảng, tooltip giữ nguyên logic.
    //  - Dùng lại TheBoGocF4 (thẻ bo góc) trong Module_GiaoDienForm4Web.cs => file đó phải còn trong project (Form42/44 cũng đang dùng).
    //  - Cùng bảng màu tím nhạt với Form4 / Form10 / Form42 / Form44.
    //  Bố cục:  [ Tiêu đề trang ]
    //           [ Thẻ bảng: STT | Tên tỉnh / Thành phố | Thời gian ]      <- ăn hết phần còn lại
    //           [ Thẻ nhập liệu: tổng cộng, ô tên, 5 nút ]
    // =====================================================================================
    internal static class Module_GiaoDienForm57Web
    {
        private static readonly Color MauNen = Color.FromArgb(244, 246, 250);
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);
        private static readonly Color MauChuPhu = Color.FromArgb(100, 116, 139);
        private static readonly Color MauTieuDeBang = Color.FromArgb(238, 232, 255);
        private static readonly Color MauChuTieuDeBang = Color.FromArgb(91, 33, 182);
        private static readonly Font FontTieuDeTrang = new Font("Segoe UI Semibold", 14F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNhan = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontPhu = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private const int DemKhungBang = 5;
        private const int ChieuCaoTieuDe = 46;
        private const int ChieuCaoTheNhap = 176;     // 36 tiêu đề thẻ + 26 dòng tổng + 46 dòng ô nhập + hàng nút + lề
        // Bộ màu nút (Nền, Hover, Nhấn, Viền, Chữ) — cùng theme tím nhạt với Form4
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutTim =
            (Color.FromArgb(237, 233, 254), Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253), Color.FromArgb(196, 181, 253), Color.FromArgb(109, 40, 217));
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutTimDam =
            (Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253), Color.FromArgb(167, 139, 250), Color.FromArgb(167, 139, 250), Color.FromArgb(91, 33, 182));
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutVien =
            (Color.White, Color.FromArgb(248, 250, 252), Color.FromArgb(241, 245, 249), Color.FromArgb(203, 213, 225), Color.FromArgb(51, 65, 85));
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutNguyHiem =
            (Color.FromArgb(254, 242, 242), Color.FromArgb(254, 226, 226), Color.FromArgb(252, 205, 205), Color.FromArgb(252, 165, 165), Color.FromArgb(185, 28, 28));
        // Mỗi phần chạy riêng trong try/catch: 1 phần lỗi chỉ ghi vào cửa sổ Output của Visual Studio ("[Module_GiaoDienForm57Web] ..."),
        // các phần còn lại vẫn được áp dụng (bản cũ để 1 lỗi làm dừng cả chuỗi, hoặc làm form không mở được).
        private static void Chay(string ten, Action viec)
        {
            try { viec(); Ghi($"OK   {ten}"); }
            catch (Exception ex) { Ghi($"LOI  {ten}: {ex}"); }
        }
        // Ghi log ra tệp giaodien-form57.log cạnh tệp .exe (chạy cả bản Release) để biết helper có chạy không và dừng ở bước nào
        private static void Ghi(string dong)
        {
            System.Diagnostics.Debug.WriteLine("[Module_GiaoDienForm57Web] " + dong);
            try
            {
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "giaodien-form57.log"),
                    $"{DateTime.Now:HH:mm:ss.fff} {dong}{Environment.NewLine}");
            }
            catch { }
        }
        public static void ApDung(Form f)
        {
            Ghi("ApDung BAT DAU, ClientSize=" + f.ClientSize);
            f.SuspendLayout();
            try
            {
                Chay("Nền + kích thước", () =>
                {
                    f.BackColor = MauNen;
                    f.ClientSize = new Size(880, 640);     // đủ chỗ cho ~10 dòng của bảng (bản cũ 802x456 chỉ còn ~4 dòng)
                    if (f.StartPosition == FormStartPosition.WindowsDefaultLocation)
                        f.StartPosition = FormStartPosition.CenterParent;
                });
                Chay("Bố cục", () => DinhBoCuc(f));
                Chay("Tiêu đề trang", () => DinhTieuDeTrang(f));
                Chay("Thẻ nhập liệu", () => BocTheNhapLieu(f));
                Chay("Khối nhập liệu", () => DinhKhoiNhapLieu(f));
                Chay("Khối nút", () => DinhKhoiNut(f));
                Chay("Bảng dữ liệu", () => DinhLuoi(f));
            }
            finally
            {
                f.ResumeLayout(true);
                Ghi("ApDung XONG, ClientSize=" + f.ClientSize);
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
        // ---------- Bố cục tổng: tiêu đề | bảng | thẻ nhập liệu ----------
        private static void DinhBoCuc(Form f)
        {
            var t1 = Tim<TableLayoutPanel>(f, "tableLayoutPanel1");
            if (t1 == null) return;
            t1.BackColor = MauNen;
            t1.Margin = new Padding(0);
            t1.Padding = new Padding(14, 8, 14, 14);
            t1.ColumnStyles.Clear();
            t1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            t1.RowStyles.Clear();
            t1.RowStyles.Add(new RowStyle(SizeType.Absolute, ChieuCaoTieuDe));   // hàng 0: tiêu đề trang
            t1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));              // hàng 1: bảng (ăn hết phần còn lại)
            t1.RowStyles.Add(new RowStyle(SizeType.Absolute, ChieuCaoTheNhap));  // hàng 2: thẻ nhập liệu
        }
        private static void DinhTieuDeTrang(Form f)
        {
            var tieuDe = Tim<Label>(f, "label1");
            if (tieuDe == null) return;
            tieuDe.Font = FontTieuDeTrang;
            tieuDe.ForeColor = MauChuTieuDeBang;
            tieuDe.BackColor = Color.Transparent;
            tieuDe.Anchor = AnchorStyles.Left;                // căn trái như tiêu đề trang web (bản cũ căn giữa, chữ xanh lá)
            tieuDe.Margin = new Padding(4, 0, 0, 0);
        }
        // ---------- Thẻ nhập liệu: bọc khối tableLayoutPanel2 (tổng cộng + ô tên + 5 nút) ----------
        private static void BocTheNhapLieu(Form f)
        {
            var khoi = Tim<TableLayoutPanel>(f, "tableLayoutPanel2");
            if (khoi == null) return;
            khoi.Margin = new Padding(0);
            khoi.ColumnStyles.Clear();
            khoi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            khoi.RowStyles.Clear();
            khoi.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            var the = new TheBoGocF4
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 10, 0, 0),
                TieuDe = "Thông tin Tỉnh / Thành phố",
                Padding = new Padding(12, 36, 12, 10)       // 36 = vùng tiêu đề, nội dung luôn nằm dưới tiêu đề
            };
            ThayBangThe(khoi, the);
        }
        // Thay control bằng thẻ ở đúng ô của TableLayoutPanel cha rồi chuyển nguyên control vào thẻ (mọi control + sự kiện giữ nguyên)
        private static void ThayBangThe(Control noiDung, TheBoGocF4 the)
        {
            var cha = noiDung.Parent;
            if (cha == null) return;
            cha.SuspendLayout();
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
                the.Controls.Add(noiDung);
                noiDung.Dock = DockStyle.Fill;
            }
            finally
            {
                cha.ResumeLayout(true);
            }
        }
        // ---------- Trong thẻ: dòng tổng cộng | dòng ô tên ----------
        private static void DinhKhoiNhapLieu(Form f)
        {
            var t3 = Tim<TableLayoutPanel>(f, "tableLayoutPanel3");
            if (t3 != null)
            {
                t3.Margin = new Padding(0);
                t3.ColumnStyles.Clear();
                t3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                t3.RowStyles.Clear();
                t3.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));     // tổng cộng
                t3.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));     // nhãn + ô tên
                t3.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));     // hàng nút
            }
            var tong = Tim<Label>(f, "label_tongCongTinhThanhPho");
            if (tong != null)
            {
                tong.Font = FontPhu;                       // nội dung chữ do code gốc gán; ở đây chỉ định kiểu
                tong.ForeColor = MauChuPhu;
                tong.Anchor = AnchorStyles.Left;
                tong.Margin = new Padding(2, 0, 0, 0);
            }
            var t4 = Tim<TableLayoutPanel>(f, "tableLayoutPanel4");
            if (t4 != null)
            {
                t4.Margin = new Padding(0);
                t4.ColumnStyles.Clear();
                t4.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                t4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                t4.RowStyles.Clear();
                t4.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            }
            var nhan = Tim<Label>(f, "label2");
            if (nhan != null)
            {
                nhan.Font = FontNhan;
                nhan.ForeColor = MauChuNhan;
                nhan.Anchor = AnchorStyles.Left;
                nhan.Margin = new Padding(2, 0, 14, 0);
            }
            var o = Tim<Krypton.Toolkit.KryptonTextBox>(f, "textBox_TenTinhThanhPho");
            if (o != null)
            {
                o.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                o.Margin = new Padding(0, 4, 0, 4);
                o.StateCommon.Border.Rounding = 6F;
            }
        }
        // ---------- Hàng 5 nút: Nhập | Xuất | Xóa | Sửa | Thêm (chia đều, có khe hở) ----------
        private static void DinhKhoiNut(Form f)
        {
            var t5 = Tim<TableLayoutPanel>(f, "tableLayoutPanel5");
            if (t5 == null) return;
            t5.Margin = new Padding(0, 6, 0, 0);
            t5.ColumnStyles.Clear();
            for (int i = 0; i < t5.ColumnCount; i++) t5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / t5.ColumnCount));
            t5.RowStyles.Clear();
            t5.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            foreach (var b in t5.Controls.OfType<Krypton.Toolkit.KryptonButton>())
            {
                // Thêm = hành động chính (tím đậm); Sửa/Lưu = tím nhạt; Xóa = đỏ nhạt; Nhập/Xuất = nút viền trung tính
                var mau = b.Name switch
                {
                    "kryptonButton1_Them" => NutTimDam,
                    "kryptonButton1_Sua" => NutTim,
                    "kryptonButton1_Xoa" => NutNguyHiem,
                    _ => NutVien
                };
                KieuNut(b, mau);
                int cot = t5.GetColumn(b);
                CanNut(b, new Padding(cot == 0 ? 0 : 8, 0, 0, 0));
            }
        }
        // ---------- Bảng dữ liệu: nằm trong khung bo tròn liền mạch, tiêu đề phẳng tím nhạt ----------
        private static void DinhLuoi(Form f)
        {
            var d = Tim<Krypton.Toolkit.KryptonDataGridView>(f, "kryptonDataGridView1");
            if (d == null || d.Parent == null) return;
            var the = new TheBoGocF4
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                DemKhung = DemKhungBang,
                LuoiBang = d,
                Padding = new Padding(10 + DemKhungBang)
            };
            ThayBangThe(d, the);                            // đặt thẻ đúng ô (hàng 1) của tableLayoutPanel1
            d.BorderStyle = BorderStyle.None;
            d.ColumnHeadersHeightChanged += (s, e) => the.Invalidate();
            d.ColumnHeadersDefaultCellStyleChanged += (s, e) => the.Invalidate();
            d.DataBindingComplete += (s, e) => the.Invalidate();
            d.BackgroundColor = Color.White;
            // Form57 gọi CauHinhLuoiMotLan() trong Load (EnableHeadersVisualStyles = false, đặt font/căn lề tiêu đề...), sau khi helper chạy.
            // Vì vậy chỉ đặt Krypton State thì KHÔNG đủ: tiêu đề cột quay về màu hệ thống. BoDinhDangLuoi đặt thẳng lên DataGridView
            // và tự áp lại mỗi khi form đổi style / bố cục lưới.
            new BoDinhDangLuoi(d, the);
        }
        private static void TieuDePhang(Krypton.Toolkit.PaletteBack back, Krypton.Toolkit.PaletteBorder vien)
        {
            back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            back.Color1 = MauTieuDeBang; back.Color2 = MauTieuDeBang;
            vien.Color1 = MauTieuDeBang; vien.Color2 = MauTieuDeBang;
        }
        // Sau Load / Shown: ô nhập Krypton bo góc đồng nhất
        private static void HoanThien(Form f)
        {
            try
            {
                foreach (var tb in TatCa<Krypton.Toolkit.KryptonTextBox>(f))
                    tb.StateCommon.Border.Rounding = 6F;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[GiaoDienWeb] " + ex.Message);   // giao diện không bao giờ được làm sập form
            }
        }
        // =====================================================================================
        //  ĐỊNH DẠNG LƯỚI — áp kiểu lên lưới và GIỮ nó sau mỗi lần code gốc định dạng lại
        // =====================================================================================
        private static readonly Color MauKeBang = Color.FromArgb(235, 238, 243);       // đường kẻ giữa các dòng
        private static readonly Color MauChuBang = Color.FromArgb(51, 65, 85);         // chữ nội dung ô
        private static readonly Color MauChonDong = Color.FromArgb(239, 246, 255);     // nền dòng đang chọn
        private static readonly Color MauChuChon = Color.FromArgb(29, 78, 216);        // chữ dòng đang chọn
        private static readonly Pen ButVachCot = new Pen(Color.FromArgb(226, 218, 250), 1f);
        private static readonly SolidBrush CoRoDong = new SolidBrush(Color.FromArgb(24, 109, 40, 217));
        private const int CaoDongBang = 36;
        private sealed class BoDinhDangLuoi
        {
            private readonly DataGridView _luoi;
            private readonly TheBoGocF4 _the;
            private bool _dangDap;      // chốt chống lặp: chính việc đặt màu cũng phát sự kiện đổi style
            private bool _kryptonXong;
            private int _dongRo = -1;
            public BoDinhDangLuoi(DataGridView luoi, TheBoGocF4 the)
            {
                _luoi = luoi;
                _the = the;
                _luoi.ColumnHeadersDefaultCellStyleChanged += (_, _) => DapKieu();
                _luoi.DefaultCellStyleChanged += (_, _) => DapKieu();
                _luoi.AlternatingRowsDefaultCellStyleChanged += (_, _) => DapKieu();
                _luoi.ColumnHeadersHeightChanged += (_, _) => _the.Invalidate();
                _luoi.Layout += (_, _) => { if (!DaDungKieu()) DapKieu(); };
                _luoi.DataBindingComplete += (_, _) => DapKieu();     // đổi DataSource sinh lại cột
                _luoi.HandleCreated += (_, _) => DapKieu();
                _luoi.Paint += VeTieuDe;
                _luoi.CellMouseEnter += (_, e) => DatDongRo(e.RowIndex);
                _luoi.MouseLeave += (_, _) => DatDongRo(-1);
                _luoi.Scroll += (_, _) => DatDongRo(-1);
                _luoi.RowPostPaint += VeLopRoDong;
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
                    _luoi.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
                    _luoi.RowTemplate.Height = CaoDongBang;
                    if (!_kryptonXong && _luoi is Krypton.Toolkit.KryptonDataGridView k)
                    {
                        _kryptonXong = true;
                        k.GridStyles.Style = Krypton.Toolkit.DataGridViewStyle.List;
                        k.StateCommon.Background.Color1 = Color.White;
                        k.StateCommon.HeaderRow.Back.Color1 = Color.FromArgb(250, 248, 255);
                        k.StateCommon.HeaderRow.Back.Color2 = Color.FromArgb(250, 248, 255);
                        k.StateCommon.HeaderRow.Border.Color1 = Color.FromArgb(237, 233, 254);
                        TieuDePhang(k.StateCommon.HeaderColumn.Back, k.StateCommon.HeaderColumn.Border);
                        TieuDePhang(k.StateTracking.HeaderColumn.Back, k.StateTracking.HeaderColumn.Border);
                        TieuDePhang(k.StatePressed.HeaderColumn.Back, k.StatePressed.HeaderColumn.Border);
                        k.StateCommon.HeaderColumn.Content.Color1 = MauChuTieuDeBang;
                        k.StateCommon.DataCell.Back.Color1 = Color.White;
                        k.StateCommon.DataCell.Content.Color1 = MauChuBang;
                        k.StateCommon.DataCell.Border.Width = 1;
                        k.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Bottom;   // chỉ kẻ ngang như bảng trên web
                        k.StateCommon.DataCell.Border.Color1 = MauKeBang;
                        k.StateSelected.DataCell.Back.Color1 = MauChonDong;
                        k.StateSelected.DataCell.Back.Color2 = MauChonDong;
                        k.StateSelected.DataCell.Content.Color1 = MauChuChon;
                    }
                    _the.Invalidate();
                }
                finally { _dangDap = false; }
            }
            private bool DaDungKieu() =>
                _luoi.ColumnHeadersDefaultCellStyle.BackColor == MauTieuDeBang &&
                _luoi.DefaultCellStyle.SelectionBackColor == MauChonDong &&
                _luoi.RowTemplate.Height == CaoDongBang;
            // Vạch dọc giữa các cột tiêu đề + đường kẻ dưới hàng tiêu đề (vẽ chồng sau khi lưới tự vẽ xong)
            private void VeTieuDe(object? sender, PaintEventArgs e)
            {
                var g = _luoi;
                if (!g.ColumnHeadersVisible || e.ClipRectangle.Top >= g.ColumnHeadersHeight) return;
                try
                {
                    foreach (DataGridViewColumn c in g.Columns)
                    {
                        if (!c.Visible) continue;
                        var r = g.GetCellDisplayRectangle(c.Index, -1, false);
                        if (r.Width <= 0 || r.Height <= 0) continue;
                        e.Graphics.DrawLine(ButVachCot, r.Right - 1, r.Top + 6, r.Right - 1, r.Bottom - 7);
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[Module_GiaoDienForm57Web.TieuDe] " + ex.Message); }
            }
            private void DatDongRo(int dong)
            {
                if (dong == _dongRo) return;
                int cu = _dongRo;
                _dongRo = dong;
                if (cu >= 0 && cu < _luoi.RowCount) _luoi.InvalidateRow(cu);
                if (dong >= 0 && dong < _luoi.RowCount) _luoi.InvalidateRow(dong);
            }
            private void VeLopRoDong(object? sender, DataGridViewRowPostPaintEventArgs e)
            {
                if (e.RowIndex != _dongRo) return;
                if ((_luoi.Rows.GetRowState(e.RowIndex) & DataGridViewElementStates.Selected) != 0) return;
                e.Graphics.FillRectangle(CoRoDong, e.RowBounds);
            }
        }
        // =====================================================================================
        //  TIỆN ÍCH TRÌNH BÀY
        // =====================================================================================
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
            // Nút đang giữ focus / nút mặc định của form (AcceptButton đổi qua lại giữa Thêm và Sửa/Lưu): ép giống trạng thái thường để chữ không bị trắng trên nền trắng
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