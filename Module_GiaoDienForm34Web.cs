using System.Diagnostics;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CHO Form34_ThongKeKhenThuong — CHỈ TRÌNH BÀY, KHÔNG CÓ LOGIC NGHIỆP VỤ
    //  Cùng ngôn ngữ thiết kế với Form4 / Form10 / Form15 / Form25: nền trang xám nhạt, thẻ trắng bo góc + thanh nhấn xanh ở tiêu đề,
    //  combo bo tròn, nút theme tím nhạt, bảng có khung bo tròn liền mạch + tiêu đề tím nhạt có nhãn "STT" và vạch phân cách cột.
    //
    //  KHÔNG ĐỔI CODE GỐC: Form34 chỉ thêm đúng 1 dòng ở CUỐI constructor (sau DangKyHieuUngVienTextBox();):
    //        Module_GiaoDienForm34Web.ApDung(this);
    //  Đặt ở cuối vì code gốc đặt lại góc bo của ô tìm kiếm trong DangKyHieuUngVienTextBox(); chạy sau thì kiểu mới mới thắng.
    //  - Không sửa Designer, không đổi tên/kiểu/sự kiện của control nào; tìm control theo Name.
    //  - Code gốc tự định dạng LƯỚI trong Load (DinhDangDataGridHienDai: tiêu đề xanh dương, tạo cột). Lớp BoDinhDangLuoi không sửa
    //    hàm đó mà áp lại màu tiêu đề ngay sau khi cột được thêm / lưới layout xong (có chốt chống lặp). Cột, độ rộng, font, màu chữ
    //    từng cột, icon khen thưởng cạnh họ tên và màu chọn dòng vẫn do code gốc quyết định.
    //  - Dùng lại lớp thẻ TheBoGocF4 và ô combo bo tròn OComboWebF4 trong Module_GiaoDienForm4Web.cs (file đó phải còn trong project).
    //  - Mọi bước bọc try/catch: lỗi trình bày không bao giờ làm form không mở được.
    //  - Muốn đổi diện mạo: chỉnh khối "TÙY CHỌN" và "BẢNG MÀU" ngay bên dưới.
    // =====================================================================================
    internal static class Module_GiaoDienForm34Web
    {
        // ===================== TÙY CHỌN =====================
        // true = khung bảng có dòng tiêu đề "3. Danh sách thống kê khen thưởng CBCS"
        private const bool HienTieuDeBang = true;
        // true = dòng dưới con trỏ chuột được tô nhẹ (kiểu bảng trên web)
        private const bool HieuUngRoDong = true;
        private const int DemKhungBang = 5;         // khoảng đệm giữa khung bo tròn và bảng (px thiết kế)
        private const int CaoHangThe = 182;         // chiều cao khối 2 thẻ xếp dọc phía trên (mỗi thẻ ~86px + khe 10px)
        // ===================== BẢNG MÀU =====================
        // --- Trang và chữ ---
        private static readonly Color MauNen = Color.FromArgb(244, 246, 250);          // nền trang
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);        // nhãn, chữ thanh trạng thái
        private static readonly Color MauVienO = Color.FromArgb(203, 213, 225);        // viền ô nhập
        private static readonly Color MauVienChon = Color.FromArgb(139, 92, 246);      // viền ô nhập khi đang gõ
        private static readonly Color MauVienThe = Color.FromArgb(226, 232, 240);      // đường kẻ trên thanh trạng thái
        // --- Bảng ---
        private static readonly Color MauTieuDeBang = Color.FromArgb(238, 232, 255);   // nền tiêu đề cột
        private static readonly Color MauChuTieuDeBang = Color.FromArgb(91, 33, 182);  // chữ tiêu đề cột
        private static readonly Color MauVachCot = Color.FromArgb(208, 198, 247);      // vạch phân cách cột tiêu đề (tím nhạt)
        private static readonly Color MauNenHangSTT = Color.FromArgb(250, 248, 255);   // nền cột số thứ tự
        private static readonly Color MauKeBang = Color.FromArgb(235, 238, 243);       // đường kẻ giữa các dòng
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
        // ===================== ĐIỂM VÀO =====================
        public static void ApDung(Form f)
        {
            f.SuspendLayout();
            try
            {
                f.BackColor = MauNen;
                An(() => DinhBoCuc(f), "BoCuc");
                An(() => BocCacThe(f), "CacThe");
                An(() => DinhTheTimKiem(f), "TheTimKiem");
                An(() => DinhTheChiTiet(f), "TheChiTiet");
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
            catch (Exception ex) { Debug.WriteLine($"[Module_GiaoDienForm34Web.{ten}] {ex.Message}"); }
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
            var goc = Tim<TableLayoutPanel>(f, "tableLayoutPanel1");
            if (goc != null)
            {
                goc.BackColor = MauNen;
                goc.Margin = new Padding(0);
                goc.Padding = new Padding(12, 10, 12, 6);
                goc.RowStyles.Clear();   // bản gốc còn dư 1 kiểu hàng Absolute 20 không có hàng thật
                goc.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            }
            // splitContainer1: khối 2 thẻ phía trên | thẻ bảng
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
                try { sc.SplitterDistance = CaoHangThe; } catch (ArgumentException) { }
            }
            // Hai thẻ xếp dọc: chia đôi chiều cao, khe 10px ở giữa
            var t4 = Tim<TableLayoutPanel>(f, "tableLayoutPanel4");
            if (t4 != null)
            {
                t4.BackColor = MauNen;
                t4.Margin = new Padding(0);
                t4.ColumnStyles.Clear();
                t4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                t4.RowStyles.Clear();
                t4.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
                t4.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            }
        }
        // ===================== THẺ =====================
        private static void BocCacThe(Form f)
        {
            var gb1 = Tim<GroupBox>(f, "groupBox1_TimKiemThongTinCBCS");
            if (gb1 != null) ThayGroupBoxBangThe(gb1, new Padding(0, 0, 0, 5));
            var gb2 = Tim<GroupBox>(f, "groupBox1");
            if (gb2 != null) ThayGroupBoxBangThe(gb2, new Padding(0, 5, 0, 0));
        }
        /// <summary>
        /// Thay GroupBox (tiêu đề đỏ nghiêng) bằng TheBoGocF4 cùng vị trí. Chuyển nguyên các control con (kèm mọi sự kiện/logic)
        /// sang thẻ rồi bỏ GroupBox rỗng; code gốc không dùng tới các field GroupBox này.
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
                Padding = new Padding(12, 36, 12, 8)   // 36 = vùng tiêu đề thẻ
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
        // ===================== THẺ 1: TÌM KIẾM THÔNG TIN CBCS =====================
        private static void DinhTheTimKiem(Form f)
        {
            // Họ và tên | ô tên | Đơn vị | combo | Tình trạng | combo | Đơn vị (khen thưởng) | combo | nút làm mới (chỉ có biểu tượng)
            var t5 = Tim<TableLayoutPanel>(f, "tableLayoutPanel5");
            if (t5 != null)
            {
                t5.Margin = new Padding(0);
                t5.ColumnStyles.Clear();
                // Cột nhãn AutoSize: rộng đúng bằng chữ (kể cả khi Windows phóng to chữ/DPI) nên nhãn không bao giờ rớt dòng.
                // Các cột ô nhập chia phần còn lại theo tỷ lệ.
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 0 Họ và tên
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));      // 1 ô tên
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 2 Đơn vị
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));      // 3 combo đơn vị
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 4 Tình trạng
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16F));      // 5 combo tình trạng
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 6 Đơn vị (khen thưởng)
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));      // 7 combo đơn vị khen thưởng
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56F));     // 8 nút làm mới
            }
            DinhNhan(Tim<Label>(f, "label3"), dauHang: true);   // Họ và tên
            DinhNhan(Tim<Label>(f, "label4"));                  // Đơn vị
            DinhNhan(Tim<Label>(f, "label5"));                  // Tình trạng
            DinhNhan(Tim<Label>(f, "label2"));                  // Đơn vị (khen thưởng)
            var o = Tim<Krypton.Toolkit.KryptonTextBox>(f, "textBox_TimKiemTheoTen");
            if (o != null)
            {
                o.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                o.Margin = new Padding(0);
                o.StateCommon.Back.Color1 = Color.White;
                KieuVienO(o.StateCommon.Border, o.StateActive.Border);
            }
            foreach (var ten in new[] { "comboBox_TimKiemDonVi", "comboBox_TinhTrangCongTac", "comboBox1_DonViKhenThuong" })
            {
                var cb = Tim<ComboBox>(f, ten);
                if (cb == null) continue;
                cb.Font = FontO;                         // phải đặt TRƯỚC khi gắn ô bo tròn (nó chép font của ComboBox)
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
        // ===================== THẺ 2: QUẢN LÝ THÀNH TÍCH KHEN THƯỞNG CBCS =====================
        private static void DinhTheChiTiet(Form f)
        {
            // STT | ô | Họ và tên | ô | Số hiệu | ô | Đơn vị | ô | Tình trạng | ô | Số lượng khen thưởng | ô | nút Cập nhật
            var t8 = Tim<TableLayoutPanel>(f, "tableLayoutPanel8");
            if (t8 != null)
            {
                t8.Margin = new Padding(0);
                t8.ColumnStyles.Clear();
                t8.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));         // 0  STT
                t8.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56F));    // 1  ô STT
                t8.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));         // 2  Họ và tên
                t8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));     // 3  ô họ tên
                t8.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));         // 4  Số hiệu
                t8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16F));     // 5  ô số hiệu
                t8.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));         // 6  Đơn vị
                t8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));     // 7  ô đơn vị
                t8.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));         // 8  Tình trạng
                t8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));     // 9  ô tình trạng
                t8.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));         // 10 Số lượng khen thưởng
                t8.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));    // 11 ô số lượng
                t8.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));   // 12 nút Cập nhật
            }
            DinhNhan(Tim<Label>(f, "label6"), dauHang: true);   // STT
            DinhNhan(Tim<Label>(f, "label7"));                  // Họ và tên
            DinhNhan(Tim<Label>(f, "label8"));                  // Số hiệu
            DinhNhan(Tim<Label>(f, "label9"));                  // Đơn vị
            DinhNhan(Tim<Label>(f, "label10"));                 // Tình trạng
            DinhNhan(Tim<Label>(f, "label1"));                  // Số lượng khen thưởng
            // 6 ô hiển thị thông tin dòng đang chọn: code gốc khóa chỉ-đọc và tô nền xanh nhạt nhận diện -> KHÔNG đụng tới nền, chỉ đổi khung
            foreach (var ten in new[] { "kryptonTextBox1_STT", "kryptonTextBox1_HoVaTen", "kryptonTextBox1_SoHieu",
                                        "kryptonTextBox1_DonVi", "kryptonTextBox1_TinhTrang", "kryptonTextBox1_SoLuong" })
            {
                var ktb = Tim<Krypton.Toolkit.KryptonTextBox>(f, ten);
                if (ktb == null) continue;
                ktb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                ktb.Margin = new Padding(0);
                KieuVienO(ktb.StateCommon.Border, ktb.StateActive.Border);
            }
            NutTrongO(f, "kryptonButton_GoiFromChiTietKhenThuong", NutTimDam, new Padding(12, 0, 0, 0));   // Cập nhật
        }
        // ===================== COMBO BO TRÒN (dùng OComboWebF4 của Form4) =====================
        // Combo gốc vẫn tồn tại + hoạt động (chỉ bị ẩn hình): mọi sự kiện SelectedIndexChanged / DataSource của code gốc chạy như cũ.
        // Cách này cũng tránh luôn lỗi "bấm 2-3 lần mới xổ" mà code gốc đang chữa tạm (DangKyComboXoLanDau) vì danh sách xổ là của ô mới.
        private static void BoTronCacCombo(Form f)
        {
            foreach (var cb in TatCa<ComboBox>(f).ToList())   // ToList: Gan thêm control vào thẻ trong lúc duyệt
                if (cb.GetType() == typeof(ComboBox))
                    OComboWebF4.Gan(cb);
        }
        // ===================== LƯỚI =====================
        // Bảng nằm gọn trong 1 thẻ có KHUNG BO TRÒN liền mạch (không cắt góc bằng Region): thẻ vẽ khung + dải tiêu đề cùng màu tiêu đề bảng.
        private static void BocLuoi(Form f)
        {
            var luoi = Tim<DataGridView>(f, "kryptonDataGridView1_DanhSachCBCS");
            if (luoi == null || luoi.Parent is not Control cha) return;
            luoi.BorderStyle = BorderStyle.None;
            luoi.Margin = new Padding(0);
            var the = new TheBoGocF4
            {
                Name = "the_kryptonDataGridView1_DanhSachCBCS",
                Dock = DockStyle.Fill,
                TieuDe = HienTieuDeBang ? "3. Danh sách thống kê khen thưởng CBCS" : "",
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
            _ = new BoDinhDangLuoi(f, luoi, the);   // tự gắn sự kiện vào lưới/form; không cần giữ tham chiếu
        }
        /// <summary>
        /// Áp màu tiêu đề mới lên lưới Krypton và GIỮ nó sau khi code gốc định dạng lưới trong Load; vẽ nhãn "STT" ở ô góc,
        /// vạch phân cách cột tiêu đề và hiệu ứng rê chuột theo dòng. Chỉ đụng tới phần trình bày, không đọc hay ghi dữ liệu của lưới.
        /// </summary>
        private sealed class BoDinhDangLuoi
        {
            private readonly DataGridView _luoi;
            private readonly Krypton.Toolkit.KryptonDataGridView? _k;
            private readonly TheBoGocF4 _the;
            private bool _dangDap;        // chốt chống lặp
            private int _dongRo = -1;     // dòng đang bị rê chuột
            public BoDinhDangLuoi(Form f, DataGridView luoi, TheBoGocF4 the)
            {
                _luoi = luoi;
                _k = luoi as Krypton.Toolkit.KryptonDataGridView;
                _the = the;
                // Code gốc (Load -> DinhDangDataGridHienDai) đặt màu tiêu đề xanh dương rồi mới thêm cột và ResumeLayout:
                // áp lại ở cả 3 mốc đó là đủ để màu mới luôn là màu cuối cùng.
                _luoi.ColumnAdded += (_, _) => { if (!DaDungKieu()) DapKieu(); };
                _luoi.Layout += (_, _) => { if (!DaDungKieu()) DapKieu(); };
                f.Shown += (_, _) => { if (!DaDungKieu()) DapKieu(); };
                _luoi.ColumnHeadersHeightChanged += (_, _) => _the.Invalidate();
                // Vẽ chồng SAU khi lưới đã tự vẽ xong => không can thiệp vào cách lưới xếp chữ tiêu đề
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
                if (_dangDap || _k == null) return;
                _dangDap = true;
                try
                {
                    StyleKrypton(_k);
                    _the.Invalidate();   // dải tiêu đề của thẻ đọc màu này lúc vẽ
                }
                finally { _dangDap = false; }
            }
            private bool DaDungKieu() => _k != null && _k.StateCommon.HeaderColumn.Back.Color1 == MauTieuDeBang;
            // Phần tiêu đề + đường kẻ của lưới (Krypton). Selection, font, màu chữ từng cột do code gốc đặt nên không đụng tới.
            private static void StyleKrypton(Krypton.Toolkit.KryptonDataGridView k)
            {
                k.StateCommon.HeaderColumn.Content.Color1 = MauChuTieuDeBang;
                k.StateCommon.DataCell.Border.Color1 = MauKeBang;
                k.StateCommon.HeaderRow.Back.Color1 = MauNenHangSTT;
                k.StateCommon.HeaderRow.Back.Color2 = MauNenHangSTT;
                k.StateCommon.HeaderRow.Border.Color1 = Color.FromArgb(237, 233, 254);
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
                            // Font tiêu đề do code gốc đặt trong Krypton (đậm); thiếu thì dùng font của lưới
                            var font = _k?.StateCommon.HeaderColumn.Content.Font ?? g.ColumnHeadersDefaultCellStyle.Font ?? g.Font;
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
                    Debug.WriteLine("[Module_GiaoDienForm34Web.TieuDe] " + ex.Message);   // lỗi vẽ thêm: bỏ qua, lưới vẫn hiển thị bình thường
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
            // Phủ lớp màu mờ lên cả dòng; dòng đang chọn giữ nguyên màu chọn.
            // GetRowState không làm "bung" dòng dùng chung nên an toàn với lưới VirtualMode nhiều dòng.
            private void VeLopRoDong(object? sender, DataGridViewRowPostPaintEventArgs e)
            {
                if (e.RowIndex != _dongRo) return;
                if ((_luoi.Rows.GetRowState(e.RowIndex) & DataGridViewElementStates.Selected) != 0) return;
                e.Graphics.FillRectangle(CoRoDong, e.RowBounds);
            }
        }
        // ===================== THANH TRẠNG THÁI (KryptonStatusStrip) =====================
        // KryptonStatusStrip vẽ theo theme Krypton (chuyển sắc) nên đặt BackColor không đủ: gán renderer phẳng riêng cho riêng thanh này.
        private static void DinhThanhTrangThai(Form f)
        {
            var ss = Tim<StatusStrip>(f, "kryptonStatusStrip1");
            if (ss == null) return;
            ss.Renderer = new TrangThaiPhang();
            ss.BackColor = Color.White;
            ss.ForeColor = MauChuNhan;
            ss.SizingGrip = false;
            ss.Padding = new Padding(10, 0, 10, 0);
            // Chỉ đổi font/màu chữ: Spring, căn lề, hiện/ẩn từng nhãn do code gốc điều khiển
            foreach (var ten in new[] { "toolStripStatusLabel1", "toolStripStatusLabel2", "toolStripStatusLabel3", "toolStripStatusLabel4" })
            {
                var it = ss.Items.Find(ten, false).FirstOrDefault();
                if (it != null) { it.Font = FontNhan; it.ForeColor = MauChuNhan; }
            }
        }
        /// <summary>Thanh trạng thái phẳng: nền trắng + 1 đường kẻ phía trên, không gradient, không đổ bóng.</summary>
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
                e.Graphics.DrawLine(p, 0, 0, e.ToolStrip.Width, 0);   // chỉ 1 đường kẻ phía trên, ngăn với phần nội dung
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
            lb.Margin = new Padding(dauHang ? 0 : 14, 0, 8, 0);
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
            // Nút bị khóa: tím rất nhạt, chữ tím nhạt vẫn đọc được
            b.StateDisabled.Back.Color1 = Color.FromArgb(245, 243, 255); b.StateDisabled.Back.Color2 = Color.FromArgb(245, 243, 255);
            b.StateDisabled.Border.Color1 = Color.FromArgb(221, 214, 254); b.StateDisabled.Border.Color2 = Color.FromArgb(221, 214, 254);
            b.StateDisabled.Content.ShortText.Color1 = Color.FromArgb(167, 139, 250);
            b.Cursor = Cursors.Hand;
        }
    }
}