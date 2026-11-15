using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CHO Form47_DonViTrucThuoc — CHỈ TRÌNH BÀY, KHÔNG CÓ LOGIC NGHIỆP VỤ
    //  - Lớp static độc lập (không phải form) => không tạo .resx, KHÔNG sửa .csproj, KHÔNG sửa Designer.
    //  - Form47 chỉ thêm 1 dòng trong constructor, ngay SAU InitializeComponent();
    //        Form47:  Module_GiaoDienForm47Web.ApDung(this);
    //  - Tìm control theo Name => không đổi tên/kiểu/sự kiện của control nào; 2 ô nhập, 3 nút, bảng (VirtualMode), thanh trạng thái giữ nguyên logic.
    //  - Dùng lại TheBoGocF4 (thẻ bo góc) trong Module_GiaoDienForm4Web.cs => file đó phải còn trong project (Form42/44/57 cũng đang dùng).
    //  - Cùng bảng màu tím nhạt với Form4 / Form10 / Form42 / Form44 / Form57.
    //  Bố cục:  [ Thẻ "1. Cập nhật thông tin và ký hiệu đơn vị": Tên đơn vị | Ký hiệu đơn vị ... Xóa | Sửa | Thêm ]
    //           [ Tiêu đề "2. Danh sách đơn vị trực thuộc" ]
    //           [ Thẻ bảng: STT | Tên đơn vị trực thuộc | Ký hiệu | Thời gian ]    <- ăn hết phần còn lại
    //           [ Thanh trạng thái: tổng số đơn vị ]
    //  Những thứ code gốc tự đặt lúc Load (CauHinhGiaoDienGrid: màu tiêu đề cột, màu dòng chọn, xen kẽ dòng, chiều cao tiêu đề/dòng) và lúc vào/ra ô nhập
    //  (TextBox_Enter/Leave đổi màu viền) được xử lý để giao diện web luôn là kết quả cuối cùng.
    // =====================================================================================
    internal static class Module_GiaoDienForm47Web
    {
        private static readonly Color MauNen = Color.FromArgb(244, 246, 250);
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);
        private static readonly Color MauTieuDeBang = Color.FromArgb(238, 232, 255);
        private static readonly Color MauChuTieuDeBang = Color.FromArgb(91, 33, 182);
        private static readonly Color MauVienONhap = Color.FromArgb(203, 213, 225);     // viền ô nhập khi không focus
        private static readonly Color MauVienOFocus = Color.FromArgb(167, 139, 250);    // viền ô nhập khi đang focus

        private static readonly Font FontNhan = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontTieuMuc = new Font("Segoe UI Semibold", 10.5F, FontStyle.Regular, GraphicsUnit.Point);

        private const int DemKhungBang = 5;
        private const int ChieuCaoTheNhap = 160;      // 36 tiêu đề thẻ + 54 hàng ô nhập + hàng nút + lề
        private const int ChieuCaoTieuMuc = 30;
        private const int CaoTieuDeBang = 44;
        private const int CaoDongBang = 36;

        // Bộ màu nút (Nền, Hover, Nhấn, Viền, Chữ) — cùng theme tím nhạt với Form4
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutTim =
            (Color.FromArgb(237, 233, 254), Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253), Color.FromArgb(196, 181, 253), Color.FromArgb(109, 40, 217));
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutTimDam =
            (Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253), Color.FromArgb(167, 139, 250), Color.FromArgb(167, 139, 250), Color.FromArgb(91, 33, 182));
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutNguyHiem =
            (Color.FromArgb(254, 242, 242), Color.FromArgb(254, 226, 226), Color.FromArgb(252, 205, 205), Color.FromArgb(252, 165, 165), Color.FromArgb(185, 28, 28));

        // Mỗi phần chạy riêng trong try/catch: 1 phần lỗi chỉ ghi vào cửa sổ Output của Visual Studio, các phần còn lại vẫn được áp dụng,
        // và giao diện không bao giờ được làm sập form (không ghi tệp log để khỏi bị phần mềm diệt virus để ý).
        private static void Chay(string ten, Action viec)
        {
            try { viec(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Module_GiaoDienForm47Web] {ten}: {ex}"); }
        }

        public static void ApDung(Form f)
        {
            f.SuspendLayout();
            try
            {
                Chay("Nền", () => f.BackColor = MauNen);
                Chay("Bố cục", () => DinhBoCuc(f));
                Chay("Thẻ thông tin", () => BocTheThongTin(f));
                Chay("Khối ô nhập", () => DinhKhoiONhap(f));
                Chay("Khối nút", () => DinhKhoiNut(f));
                Chay("Danh sách + bảng", () => DinhDanhSach(f));
                Chay("Thanh trạng thái", () => DinhThanhTrangThai(f));
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

        // ---------- Bố cục tổng: thẻ thông tin | danh sách | thanh trạng thái ----------
        private static void DinhBoCuc(Form f)
        {
            var t1 = Tim<TableLayoutPanel>(f, "tableLayoutPanel1");
            if (t1 == null) return;
            t1.BackColor = MauNen;
            t1.Margin = new Padding(0);
            t1.Padding = new Padding(12, 10, 12, 0);
            t1.ColumnStyles.Clear();
            t1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            t1.RowStyles.Clear();
            t1.RowStyles.Add(new RowStyle(SizeType.Absolute, ChieuCaoTheNhap));   // hàng 0: thẻ thông tin
            t1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));               // hàng 1: danh sách (ăn hết phần còn lại)
            t1.RowStyles.Add(new RowStyle(SizeType.AutoSize));                    // hàng 2: thanh trạng thái
        }

        // ---------- GroupBox -> thẻ trắng bo góc (cùng cách làm với Form42/44) ----------
        private static void BocTheThongTin(Form f)
        {
            var gb = Tim<GroupBox>(f, "groupBox_ThongTin");
            if (gb == null) return;
            var cha = gb.Parent;
            if (cha == null || gb.Controls.Count == 0) return;
            var noiDung = gb.Controls[0];

            var the = new TheBoGocF4
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 10),
                TieuDe = gb.Text,
                Padding = new Padding(12, 36, 12, 10)       // 36 = vùng tiêu đề, nội dung luôn nằm dưới tiêu đề
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
                gb.TextChanged += (s, e) => the.TieuDe = gb.Text;   // GroupBox còn sống (ẩn): code gốc gán .Text thì tiêu đề thẻ tự cập nhật
            }
            finally
            {
                cha.ResumeLayout(true);
            }
        }

        // ---------- Thẻ thông tin: hàng ô nhập (Tên đơn vị | Ký hiệu đơn vị) ----------
        private static void DinhKhoiONhap(Form f)
        {
            var t3 = Tim<TableLayoutPanel>(f, "tableLayoutPanel3");
            if (t3 != null)
            {
                t3.Margin = new Padding(0);
                t3.ColumnStyles.Clear();
                t3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                t3.RowStyles.Clear();
                t3.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));      // hàng ô nhập
                t3.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));      // hàng nút
            }

            var t4 = Tim<TableLayoutPanel>(f, "tableLayoutPanel4");
            if (t4 != null)
            {
                t4.Margin = new Padding(0);
                t4.ColumnStyles.Clear();
                t4.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // nhãn "Tên đơn vị"
                t4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));      // ô tên
                t4.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // nhãn "Ký hiệu đơn vị"
                t4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));      // ô ký hiệu
                t4.RowStyles.Clear();
                t4.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            }

            DinhNhan(Tim<Label>(f, "label2"), 0);
            DinhNhan(Tim<Label>(f, "label3"), 18);
            DinhOTextBox(Tim<Krypton.Toolkit.KryptonTextBox>(f, "textBox_TenDonVi"));
            DinhOTextBox(Tim<Krypton.Toolkit.KryptonTextBox>(f, "kryptonTextBox_KyHieuDonVi"));
        }

        // ---------- Hàng nút: Xóa | Sửa | Thêm đơn vị (gom về bên phải, nút chính nằm ngoài cùng) ----------
        private static void DinhKhoiNut(Form f)
        {
            var t5 = Tim<TableLayoutPanel>(f, "tableLayoutPanel5");
            if (t5 == null) return;
            var xoa = Tim<Krypton.Toolkit.KryptonButton>(f, "kryptonButton1_Xoa");
            var sua = Tim<Krypton.Toolkit.KryptonButton>(f, "kryptonButton1_Sua");
            var them = Tim<Krypton.Toolkit.KryptonButton>(f, "kryptonButton1_Them");
            if (xoa == null || sua == null || them == null) return;

            t5.Margin = new Padding(0, 6, 0, 0);
            t5.ColumnStyles.Clear();
            t5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));    // khoảng trống bên trái
            t5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            t5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            t5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            t5.ColumnCount = 4;
            t5.RowStyles.Clear();
            t5.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            t5.SetColumn(them, 3);      // dời từ phải sang trái để các ô không đè nhau
            t5.SetColumn(sua, 2);
            t5.SetColumn(xoa, 1);

            KieuNut(xoa, NutNguyHiem); CanNut(xoa, new Padding(0));
            KieuNut(sua, NutTim); CanNut(sua, new Padding(8, 0, 0, 0));
            KieuNut(them, NutTimDam); CanNut(them, new Padding(8, 0, 0, 0));
        }

        // ---------- Danh sách: tiêu đề mục + bảng nằm trong khung bo tròn liền mạch ----------
        private static void DinhDanhSach(Form f)
        {
            var gb = Tim<GroupBox>(f, "groupBox_DanhSach");
            var d = Tim<Krypton.Toolkit.KryptonDataGridView>(f, "kryptonDataGridView1");
            if (gb == null || d == null) return;
            var cha = gb.Parent;
            if (cha == null) return;

            var tieuMuc = new Label
            {
                Text = gb.Text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = FontTieuMuc,
                ForeColor = MauChuTieuDeBang,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(4, 0, 0, 0)
            };
            var the = new TheBoGocF4
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                DemKhung = DemKhungBang,
                LuoiBang = d,
                Padding = new Padding(10 + DemKhungBang)
            };
            var khung = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0),
                BackColor = MauNen
            };
            khung.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            khung.RowStyles.Add(new RowStyle(SizeType.Absolute, ChieuCaoTieuMuc));
            khung.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            khung.Controls.Add(tieuMuc, 0, 0);
            khung.Controls.Add(the, 0, 1);

            cha.SuspendLayout();
            try
            {
                if (cha is TableLayoutPanel tlp)
                {
                    var vt = tlp.GetPositionFromControl(gb);
                    tlp.Controls.Remove(gb);
                    tlp.Controls.Add(khung, vt.Column, vt.Row);
                }
                else
                {
                    cha.Controls.Remove(gb);
                    cha.Controls.Add(khung);
                }
                gb.Visible = false;
                gb.TextChanged += (s, e) => tieuMuc.Text = gb.Text;
                the.Controls.Add(d);               // tự gỡ khỏi GroupBox; mọi sự kiện/logic của bảng (VirtualMode, CellValueNeeded...) giữ nguyên
                d.Dock = DockStyle.Fill;
                d.BorderStyle = BorderStyle.None;
                d.ColumnHeadersHeightChanged += (s, e) => the.Invalidate();
                d.ColumnHeadersDefaultCellStyleChanged += (s, e) => the.Invalidate();
            }
            finally
            {
                cha.ResumeLayout(true);
            }

            DinhKieuKrypton(d);
            new BoGiuKieuLuoi(d);
        }

        // Màu cấp Krypton: bảng Krypton vẽ tiêu đề cột / dòng chọn theo palette này
        private static void DinhKieuKrypton(Krypton.Toolkit.KryptonDataGridView d)
        {
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
            d.StateCommon.DataCell.Border.Width = 1;
            d.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Bottom;   // chỉ kẻ ngang như bảng trên web
            MauSauNap(d);
        }

        private static void TieuDePhang(Krypton.Toolkit.PaletteBack back, Krypton.Toolkit.PaletteBorder vien)
        {
            back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            back.Color1 = MauTieuDeBang; back.Color2 = MauTieuDeBang;
            vien.Color1 = MauTieuDeBang; vien.Color2 = MauTieuDeBang;
        }

        private static void MauSauNap(Krypton.Toolkit.KryptonDataGridView d)
        {
            if (d.IsDisposed) return;
            d.StateCommon.DataCell.Border.Color1 = Color.FromArgb(235, 238, 243);
            d.StateSelected.DataCell.Back.Color1 = Color.FromArgb(239, 246, 255);
            d.StateSelected.DataCell.Back.Color2 = Color.FromArgb(239, 246, 255);
            d.StateSelected.DataCell.Content.Color1 = Color.FromArgb(29, 78, 216);
        }

        // Màu cấp DataGridView: code gốc (CauHinhGiaoDienGrid, chạy lúc Load) đặt thẳng lên lưới màu tiêu đề cột (xám xanh), màu dòng chọn (xanh dương),
        // dòng xen kẽ, chiều cao tiêu đề 58 / dòng 34. Lớp này đặt lại giá trị web mỗi khi lưới bị đổi style, có chốt chống lặp.
        private sealed class BoGiuKieuLuoi
        {
            private readonly DataGridView _luoi;
            private bool _dangDap;

            public BoGiuKieuLuoi(DataGridView luoi)
            {
                _luoi = luoi;
                _luoi.ColumnHeadersDefaultCellStyleChanged += (_, _) => DapKieu();
                _luoi.DefaultCellStyleChanged += (_, _) => DapKieu();
                _luoi.AlternatingRowsDefaultCellStyleChanged += (_, _) => DapKieu();
                _luoi.ColumnHeadersHeightSizeModeChanged += (_, _) => DapKieu();
                _luoi.HandleCreated += (_, _) => DapKieu();
                _luoi.Layout += (_, _) => { if (!DaDungKieu()) DapKieu(); };
                DapKieu();
            }

            private bool DaDungKieu() =>
                _luoi.ColumnHeadersDefaultCellStyle.BackColor == MauTieuDeBang &&
                _luoi.DefaultCellStyle.SelectionBackColor == Color.FromArgb(239, 246, 255) &&
                _luoi.RowTemplate.Height == CaoDongBang &&
                (_luoi.ColumnHeadersHeightSizeMode != DataGridViewColumnHeadersHeightSizeMode.DisableResizing || _luoi.ColumnHeadersHeight == CaoTieuDeBang);

            private void DapKieu()
            {
                if (_dangDap || _luoi.IsDisposed) return;
                _dangDap = true;
                try
                {
                    var h = _luoi.ColumnHeadersDefaultCellStyle;
                    h.BackColor = MauTieuDeBang;
                    h.ForeColor = MauChuTieuDeBang;
                    h.SelectionBackColor = MauTieuDeBang;
                    h.SelectionForeColor = MauChuTieuDeBang;
                    _luoi.GridColor = Color.FromArgb(235, 238, 243);
                    _luoi.DefaultCellStyle.BackColor = Color.White;
                    _luoi.DefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
                    _luoi.DefaultCellStyle.SelectionBackColor = Color.FromArgb(239, 246, 255);
                    _luoi.DefaultCellStyle.SelectionForeColor = Color.FromArgb(29, 78, 216);
                    _luoi.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
                    _luoi.RowTemplate.Height = CaoDongBang;
                    // Chỉ đặt chiều cao tiêu đề khi code gốc đã khóa kiểu co giãn (DisableResizing); đang AutoSize thì không được gán
                    if (_luoi.ColumnHeadersHeightSizeMode == DataGridViewColumnHeadersHeightSizeMode.DisableResizing &&
                        _luoi.ColumnHeadersHeight != CaoTieuDeBang)
                        _luoi.ColumnHeadersHeight = CaoTieuDeBang;
                }
                finally { _dangDap = false; }
            }
        }

        // ---------- Thanh trạng thái ----------
        private static void DinhThanhTrangThai(Form f)
        {
            var ss = Tim<StatusStrip>(f, "statusStrip1");
            if (ss == null) return;
            ss.BackColor = Color.White;               // Designer để xanh lá; web dùng nền trắng + chữ xám xanh
            ss.ForeColor = MauChuNhan;
            ss.SizingGrip = false;
            ss.Padding = new Padding(10, 0, 10, 0);
            foreach (ToolStripItem it in ss.Items)
                if (it is ToolStripStatusLabel) it.Font = FontNhan;
        }

        // Sau Load / Shown: ô nhập Krypton bo góc đồng nhất + màu bảng
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
                System.Diagnostics.Debug.WriteLine("[Module_GiaoDienForm47Web] " + ex.Message);   // giao diện không bao giờ được làm sập form
            }
        }

        // =====================================================================================
        //  TIỆN ÍCH TRÌNH BÀY
        // =====================================================================================
        private static void DinhNhan(Label? lb, int leTrai)
        {
            if (lb == null) return;
            lb.Font = FontNhan;
            lb.ForeColor = MauChuNhan;
            lb.Anchor = AnchorStyles.Left;
            lb.Margin = new Padding(leTrai, 0, 10, 0);
        }

        // Ô nhập: code gốc đổi màu viền ở StateCommon mỗi lần vào/ra ô (TextBox_Enter -> DodgerBlue, TextBox_Leave -> Gray).
        // Ở đây đặt màu web (xám xanh / tím khi focus) và đăng ký thêm Enter/Leave; việc áp màu được hoãn bằng BeginInvoke nên luôn chạy SAU
        // mọi handler của code gốc, bất kể thứ tự đăng ký sự kiện.
        private static void DinhOTextBox(Krypton.Toolkit.KryptonTextBox? tb)
        {
            if (tb == null) return;
            tb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tb.Margin = new Padding(0, 4, 0, 4);
            tb.StateCommon.Border.Rounding = 6F;
            DatVienONhap(tb, MauVienONhap);
            tb.Enter += (s, e) => HoanDatVien(tb, MauVienOFocus);
            tb.Leave += (s, e) => HoanDatVien(tb, MauVienONhap);
        }

        private static void DatVienONhap(Krypton.Toolkit.KryptonTextBox tb, Color mau)
        {
            if (tb.IsDisposed) return;
            tb.StateCommon.Border.Color1 = mau;
            tb.StateCommon.Border.Color2 = mau;
        }

        private static void HoanDatVien(Krypton.Toolkit.KryptonTextBox tb, Color mau)
        {
            if (tb.IsDisposed) return;
            if (tb.IsHandleCreated)
                tb.BeginInvoke(new Action(() => DatVienONhap(tb, mau)));
            else
                DatVienONhap(tb, mau);
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
            // Nút đang giữ focus / nút mặc định của form: ép giống trạng thái thường để chữ không bị trắng trên nền trắng
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