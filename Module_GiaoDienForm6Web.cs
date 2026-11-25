using System.Drawing.Drawing2D;
using System.Reflection;
///Phiên bản 4
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CỦA Form6_XuLyData  —  CHỈ TRÌNH BÀY, KHÔNG CHỨA LOGIC NGHIỆP VỤ
    //  DẠNG MODULE ĐỘC LẬP (không còn là partial class của form) => Visual Studio không còn coi file này là
    //  form phụ nên KHÔNG tự sinh file .resx đi kèm. Có thể xóa file Form6_XuLyData.GiaoDienWeb.resx.
    //
    //  Cách dùng: trong constructor Form6_XuLyData, thay dòng
    //        ApDungGiaoDienWeb();
    //  bằng
    //        Module_GiaoDienForm6Web.ApDung(this);
    //  - Các control của form (private trong Designer) được lấy theo TÊN bằng reflection, nên không phải sửa Designer
    //    hay đổi tên/kiểu/sự kiện control nào => code nghiệp vụ giữ nguyên 100%.
    //  - Một số thuộc tính (màu lưới, bo góc ô nhập...) bị Form6_Load đặt lại nên phần HoanThienGiaoDienSauLoad
    //    được móc vào sự kiện Load SAU Form6_Load (đăng ký sau nên chạy sau).
    //  - Dùng lại OComboWebF4 trong Module_GiaoDienForm4Web.cs (file đó phải còn trong project).
    //  - Lỗi trình bày không bao giờ làm form không mở được (bọc try/catch).
    //  - Mọi kích thước (px) viết theo đơn vị thiết kế 96 DPI.
    // =====================================================================================
    internal sealed class Module_GiaoDienForm6Web
    {
        private readonly Form _f;
        // ---------- Các control của form (lấy theo tên, cùng tên với Designer) ----------
        private readonly ComboBox comboBox_ChucVu;
        private readonly ComboBox comboBox_PhanLoai;
        private readonly ComboBox comboBox_TimKiemDonVi;
        private readonly ComboBox comboBox_XepLoaiThiDua;
        private readonly ComboBox comboBox_LocGhiChu;      // tạo bằng code trong Form6 (TaoBoLocGhiChu)
        private readonly Label label_LocGhiChu;
        private readonly ContextMenuStrip contextMenuStrip1;
        private readonly GroupBox groupBox1_TimKiemThongTinCBCS;
        private readonly GroupBox groupBox2_KhoiLenhDieuKhien;
        private readonly GroupBox groupBox3_ThongTinCBCS;
        private readonly Krypton.Toolkit.KryptonButton kryptonButton1_HuongDanThemDuLieu;
        private readonly Krypton.Toolkit.KryptonButton kryptonButton1_ThiDuaBaNhat;
        private readonly Krypton.Toolkit.KryptonButton kryptonButton_LamMoiCacOTimKiem;
        private readonly Krypton.Toolkit.KryptonButton kryptonButton_LuuDataCapNhat;
        private readonly Krypton.Toolkit.KryptonButton kryptonButton_RefershCSDL;
        private readonly Krypton.Toolkit.KryptonButton kryptonButton_XoaDataCBCS;
        private readonly Krypton.Toolkit.KryptonButton kryptonButton_XoaKetQuaPhanLoai;
        private readonly Krypton.Toolkit.KryptonDataGridView kryptonDataGridView1;
        private readonly Label label10;
        private readonly Label label11;
        private readonly Label label12;
        private readonly Label label13;
        private readonly Label label14;
        private readonly Label label15;
        private readonly Label label16;
        private readonly Label label3;
        private readonly Label label4;
        private readonly Label label5;
        private readonly Label label6;
        private readonly Label label7;
        private readonly Label label8;
        private readonly Label label9;
        private readonly PictureBox pictureBox1;
        private readonly SplitContainer splitContainer1;
        private readonly StatusStrip statusStrip1;
        private readonly TableLayoutPanel tableLayoutPanel1;
        private readonly TableLayoutPanel tableLayoutPanel2;
        private readonly TableLayoutPanel tableLayoutPanel3;
        private readonly TableLayoutPanel tableLayoutPanel5;
        private readonly TableLayoutPanel tableLayoutPanel6;
        private readonly TableLayoutPanel tableLayoutPanel8;
        private readonly TableLayoutPanel tableLayoutPanel9;
        private readonly ComboBox textBox_CapBac;
        private readonly ComboBox textBox_DonVi;
        private readonly Krypton.Toolkit.KryptonTextBox textBox_GhiChu;
        private readonly Krypton.Toolkit.KryptonTextBox textBox_HoVaTen;
        private readonly Krypton.Toolkit.KryptonTextBox textBox_NamSinh;
        private readonly Krypton.Toolkit.KryptonTextBox textBox_NgayVaoCAND;
        private readonly Krypton.Toolkit.KryptonTextBox textBox_QueQuan;
        private readonly Krypton.Toolkit.KryptonTextBox textBox_STT;
        private readonly Krypton.Toolkit.KryptonTextBox textBox_SoHieu;
        private readonly Krypton.Toolkit.KryptonTextBox textBox_TimKiemTheoTen;
        private readonly ToolStripStatusLabel toolStripStatusLabel1;
        private readonly ToolStripStatusLabel toolStripStatusLabel2_KhongPL;
        private readonly ToolStripStatusLabel toolStripStatusLabel2_Loai1;
        private readonly ToolStripStatusLabel toolStripStatusLabel2_Loai2;
        private readonly ToolStripStatusLabel toolStripStatusLabel2_Loai3;
        private readonly ToolStripStatusLabel toolStripStatusLabel2_Loai4;
        public static void ApDung(Form f)
        {
            try { new Module_GiaoDienForm6Web(f).Chay(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[Module_GiaoDienForm6Web] " + ex.Message); }
        }
        private Module_GiaoDienForm6Web(Form f)
        {
            _f = f;
            comboBox_ChucVu = Lay<ComboBox>("comboBox_ChucVu");
            comboBox_PhanLoai = Lay<ComboBox>("comboBox_PhanLoai");
            comboBox_TimKiemDonVi = Lay<ComboBox>("comboBox_TimKiemDonVi");
            comboBox_XepLoaiThiDua = Lay<ComboBox>("comboBox_XepLoaiThiDua");
            comboBox_LocGhiChu = Lay<ComboBox>("comboBox_LocGhiChu");
            label_LocGhiChu = Lay<Label>("label_LocGhiChu");
            contextMenuStrip1 = Lay<ContextMenuStrip>("contextMenuStrip1");
            groupBox1_TimKiemThongTinCBCS = Lay<GroupBox>("groupBox1_TimKiemThongTinCBCS");
            groupBox2_KhoiLenhDieuKhien = Lay<GroupBox>("groupBox2_KhoiLenhDieuKhien");
            groupBox3_ThongTinCBCS = Lay<GroupBox>("groupBox3_ThongTinCBCS");
            kryptonButton1_HuongDanThemDuLieu = Lay<Krypton.Toolkit.KryptonButton>("kryptonButton1_HuongDanThemDuLieu");
            kryptonButton1_ThiDuaBaNhat = Lay<Krypton.Toolkit.KryptonButton>("kryptonButton1_ThiDuaBaNhat");
            kryptonButton_LamMoiCacOTimKiem = Lay<Krypton.Toolkit.KryptonButton>("kryptonButton_LamMoiCacOTimKiem");
            kryptonButton_LuuDataCapNhat = Lay<Krypton.Toolkit.KryptonButton>("kryptonButton_LuuDataCapNhat");
            kryptonButton_RefershCSDL = Lay<Krypton.Toolkit.KryptonButton>("kryptonButton_RefershCSDL");
            kryptonButton_XoaDataCBCS = Lay<Krypton.Toolkit.KryptonButton>("kryptonButton_XoaDataCBCS");
            kryptonButton_XoaKetQuaPhanLoai = Lay<Krypton.Toolkit.KryptonButton>("kryptonButton_XoaKetQuaPhanLoai");
            kryptonDataGridView1 = Lay<Krypton.Toolkit.KryptonDataGridView>("kryptonDataGridView1");
            label10 = Lay<Label>("label10");
            label11 = Lay<Label>("label11");
            label12 = Lay<Label>("label12");
            label13 = Lay<Label>("label13");
            label14 = Lay<Label>("label14");
            label15 = Lay<Label>("label15");
            label16 = Lay<Label>("label16");
            label3 = Lay<Label>("label3");
            label4 = Lay<Label>("label4");
            label5 = Lay<Label>("label5");
            label6 = Lay<Label>("label6");
            label7 = Lay<Label>("label7");
            label8 = Lay<Label>("label8");
            label9 = Lay<Label>("label9");
            pictureBox1 = Lay<PictureBox>("pictureBox1");
            splitContainer1 = Lay<SplitContainer>("splitContainer1");
            statusStrip1 = Lay<StatusStrip>("statusStrip1");
            tableLayoutPanel1 = Lay<TableLayoutPanel>("tableLayoutPanel1");
            tableLayoutPanel2 = Lay<TableLayoutPanel>("tableLayoutPanel2");
            tableLayoutPanel3 = Lay<TableLayoutPanel>("tableLayoutPanel3");
            tableLayoutPanel5 = Lay<TableLayoutPanel>("tableLayoutPanel5");
            tableLayoutPanel6 = Lay<TableLayoutPanel>("tableLayoutPanel6");
            tableLayoutPanel8 = Lay<TableLayoutPanel>("tableLayoutPanel8");
            tableLayoutPanel9 = Lay<TableLayoutPanel>("tableLayoutPanel9");
            textBox_CapBac = Lay<ComboBox>("textBox_CapBac");
            textBox_DonVi = Lay<ComboBox>("textBox_DonVi");
            textBox_GhiChu = Lay<Krypton.Toolkit.KryptonTextBox>("textBox_GhiChu");
            textBox_HoVaTen = Lay<Krypton.Toolkit.KryptonTextBox>("textBox_HoVaTen");
            textBox_NamSinh = Lay<Krypton.Toolkit.KryptonTextBox>("textBox_NamSinh");
            textBox_NgayVaoCAND = Lay<Krypton.Toolkit.KryptonTextBox>("textBox_NgayVaoCAND");
            textBox_QueQuan = Lay<Krypton.Toolkit.KryptonTextBox>("textBox_QueQuan");
            textBox_STT = Lay<Krypton.Toolkit.KryptonTextBox>("textBox_STT");
            textBox_SoHieu = Lay<Krypton.Toolkit.KryptonTextBox>("textBox_SoHieu");
            textBox_TimKiemTheoTen = Lay<Krypton.Toolkit.KryptonTextBox>("textBox_TimKiemTheoTen");
            toolStripStatusLabel1 = Lay<ToolStripStatusLabel>("toolStripStatusLabel1");
            toolStripStatusLabel2_KhongPL = Lay<ToolStripStatusLabel>("toolStripStatusLabel2_KhongPL");
            toolStripStatusLabel2_Loai1 = Lay<ToolStripStatusLabel>("toolStripStatusLabel2_Loai1");
            toolStripStatusLabel2_Loai2 = Lay<ToolStripStatusLabel>("toolStripStatusLabel2_Loai2");
            toolStripStatusLabel2_Loai3 = Lay<ToolStripStatusLabel>("toolStripStatusLabel2_Loai3");
            toolStripStatusLabel2_Loai4 = Lay<ToolStripStatusLabel>("toolStripStatusLabel2_Loai4");
        }
        private T Lay<T>(string ten) where T : class
        {
            var fi = _f.GetType().GetField(ten, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                     ?? throw new MissingFieldException(_f.GetType().Name, ten);
            return (T)fi.GetValue(_f)!;
        }
        // ---------- BẢNG MÀU (tông Slate + Blue, giống các web admin hiện đại) ----------
        private static readonly Color MauNenTrang = Color.FromArgb(244, 246, 250);   // nền trang
        private static readonly Color MauChuDam = Color.FromArgb(30, 41, 59);        // tiêu đề / chữ chính
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);      // nhãn ô nhập
        private static readonly Color MauChuPhu = Color.FromArgb(100, 116, 139);     // chữ phụ
        private static readonly Color MauChinh = Color.FromArgb(37, 99, 235);        // xanh chủ đạo
        private static readonly Font FontTieuDeThe = new Font(Module_HeThong.TenFontHeThongDam, 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNhan = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new Font(Module_HeThong.TenFontHeThongDam, 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontTrangThai = new Font(Module_HeThong.TenFontHeThongDam, 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontMenu = new Font(Module_HeThong.TenFontHeThong, 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        // Bộ màu nút: (Nền, Hover, Nhấn, Viền, Chữ)
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutChinh =
            (Color.FromArgb(37, 99, 235), Color.FromArgb(29, 78, 216), Color.FromArgb(30, 64, 175), Color.FromArgb(37, 99, 235), Color.White);
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutXanhLa =
            (Color.FromArgb(22, 163, 74), Color.FromArgb(21, 128, 61), Color.FromArgb(22, 101, 52), Color.FromArgb(22, 163, 74), Color.White);
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutXanhNhat =
            (Color.FromArgb(239, 246, 255), Color.FromArgb(219, 234, 254), Color.FromArgb(191, 219, 254), Color.FromArgb(191, 219, 254), Color.FromArgb(29, 78, 216));
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutVien =
            (Color.White, Color.FromArgb(248, 250, 252), Color.FromArgb(241, 245, 249), Color.FromArgb(203, 213, 225), Color.FromArgb(51, 65, 85));
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutNguyHiem =
            (Color.FromArgb(254, 242, 242), Color.FromArgb(254, 226, 226), Color.FromArgb(252, 205, 205), Color.FromArgb(252, 165, 165), Color.FromArgb(185, 28, 28));
        // =====================================================================================
        //  1) ÁP DỤNG GIAO DIỆN (gọi trong constructor, ngay sau InitializeComponent)
        // =====================================================================================
        private void Chay()
        {
            // Tạm dừng bố cục CẢ CÂY (không chỉ form gốc) để các khung con không dàn lại sau từng thay đổi - xem Module_TamDungBoCuc
            Module_TamDungBoCuc.ChayKhongDanTrang(_f, () =>
            {
                _f.BackColor = MauNenTrang;
                DinhBoCucTong();
                DinhTheGroupBox();
                DinhKhoiTimKiem();
                DinhKhoiLenh();
                DinhKhoiCapNhat();
                DinhComboBoTron();
                DinhVungLuoi();
                DinhThanhTrangThai();
                DinhMenuNguCanh();
            });
            // Các phần bị Form6_Load đặt lại -> áp dụng lại SAU Form6_Load (đăng ký sau nên chạy sau).
            _f.Load += (s, e) => { HoanThienGiaoDienSauLoad(); CanGiuaNoiDung(); CanHangTren(); };
            _f.SizeChanged += (s, e) => CanGiuaNoiDung();
            // Hàng trên canh lại SAU lượt dàn trang (YeuCauCanHangTren) - không canh ngay trong SizeChanged
            tableLayoutPanel2.SizeChanged += (s, e) => YeuCauCanHangTren();                // đổi rộng / thu nhỏ - phóng to
            textBox_TimKiemTheoTen.SizeChanged += (s, e) => YeuCauCanHangTren();           // ô nhập đổi cao (font/DPI) => nút theo
            _f.DpiChanged += (s, e) => YeuCauCanHangTren();                                // Form6 mở riêng (cửa sổ độc lập)
            _f.DpiChangedAfterParent += (s, e) => YeuCauCanHangTren();                     // Form6 nằm trong Form2: đổi tỷ lệ 100/125/150%
            _f.VisibleChanged += (s, e) => { if (_f.Visible) YeuCauCanHangTren(); };      // quay lại trang sau khi cửa sổ đổi cỡ lúc ẩn
            kryptonButton1_HuongDanThemDuLieu.TextChanged += (s, e) => YeuCauCanHangTren(); // "Thêm CBCS" <-> "Thêm tân binh"
        }
        // ---------- Bố cục tổng: nền trang + khoảng đệm + hàng đầu cố định, phần dưới co giãn ----------
        private void DinhBoCucTong()
        {
            tableLayoutPanel1.BackColor = MauNenTrang;
            tableLayoutPanel1.Padding = new Padding(12, 10, 12, 8);
            tableLayoutPanel1.RowStyles.Clear();
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F)); // hàng tìm kiếm + lệnh
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // form cập nhật + lưới
            tableLayoutPanel2.Margin = new Padding(0);
            tableLayoutPanel2.ColumnStyles.Clear();
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));   // thẻ Tìm kiếm: phần còn lại
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 520F));  // thẻ Khối lệnh: CanHangTren đặt đúng theo nút
            // Logo: giữ đúng tỉ lệ ảnh (trước đây kéo giãn làm méo hình)
            pictureBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.Margin = new Padding(0, 8, 14, 8);
            // Vùng dưới: thẻ "Cập nhật" cố định chiều cao, lưới ăn hết phần còn lại khi resize
            splitContainer1.Margin = new Padding(0, 8, 0, 0);
            splitContainer1.BackColor = MauNenTrang;
            splitContainer1.Panel1.BackColor = MauNenTrang;
            splitContainer1.Panel2.BackColor = MauNenTrang;
            try
            {
                splitContainer1.SplitterWidth = 10;                 // khe hở giữa 2 thẻ
                splitContainer1.Panel1MinSize = 110;
                splitContainer1.Panel2MinSize = 120;
                splitContainer1.FixedPanel = FixedPanel.Panel1;     // resize cửa sổ chỉ co giãn lưới
                splitContainer1.SplitterDistance = 134;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                // Kích thước container chưa đủ: bỏ qua, giữ giá trị mặc định của Designer
            }
        }
        // ---------- Biến 3 GroupBox thành "thẻ" trắng bo góc (vẽ đè bằng sự kiện Paint) ----------
        // GroupBox tự vẽ chữ/viền nên không thể "vẽ đè" ổn định (bị chồng 2 tiêu đề).
        // => Chuyển bảng con sang 1 thẻ tự vẽ (TheBoGocF6) thay chỗ GroupBox; GroupBox được giữ lại ở dạng
        //    ẩn để code gốc vẫn gán groupBoxN.Text như cũ, và Text đó được đồng bộ lên tiêu đề của thẻ.
        private void DinhTheGroupBox()
        {
            _theTimKiem = BocTheTieuDe(groupBox1_TimKiemThongTinCBCS, tableLayoutPanel5, new Padding(0, 0, 10, 0));
            _theLenh = BocTheTieuDe(groupBox2_KhoiLenhDieuKhien, tableLayoutPanel6, new Padding(0));
            _theCapNhat = BocTheTieuDe(groupBox3_ThongTinCBCS, tableLayoutPanel3, new Padding(0));
        }
        private TheBoGocF6? _theTimKiem, _theLenh, _theCapNhat;
        private static TheBoGocF6? BocTheTieuDe(GroupBox gb, Control noiDung, Padding margin)
        {
            var cha = gb.Parent;
            if (cha == null) return null;
            var the = new TheBoGocF6
            {
                Dock = DockStyle.Fill,
                Margin = margin,
                ForeColor = MauChuDam,
                TieuDe = gb.Text,
                Padding = new Padding(12, 34, 12, 8)   // 34 = vùng tiêu đề, tự tính -> không bao giờ đè lên ô nhập
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
                the.Controls.Add(noiDung);       // chuyển bảng con (giữ nguyên toàn bộ control bên trong)
                noiDung.Dock = DockStyle.Fill;
                the.Controls.Add(gb);            // giữ GroupBox (ẩn) để logic gán .Text không lỗi
                gb.TextChanged += (s, e) => the.TieuDe = gb.Text;
            }
            finally
            {
                cha.ResumeLayout(true);
            }
            return the;
        }
        // ---------- Giữ bố cục gọn trên màn hình rộng: nội dung tối đa RongToiDa, căn giữa ----------
        private const int RongToiDa = 1440; // đơn vị thiết kế (96 DPI). Muốn rộng hơn/hẹp hơn chỉ cần sửa số này.
        private void CanGiuaNoiDung()
        {
            if (_f.IsDisposed || tableLayoutPanel1 == null) return;
            int toiDa = _f.LogicalToDeviceUnits(RongToiDa);
            int leToiThieu = _f.LogicalToDeviceUnits(12);
            int le = Math.Max(leToiThieu, (_f.ClientSize.Width - toiDa) / 2);
            var p = tableLayoutPanel1.Padding;
            if (p.Left == le && p.Right == le) return;
            tableLayoutPanel1.Padding = new Padding(le, p.Top, le, p.Bottom);
        }
        // ---------- Thẻ 1: Tìm kiếm ----------
        // Kiểu thanh lọc web: NHÃN NẰM TRÊN Ô (4 ô lọc + nút làm mới bộ lọc chung 1 hàng) => ô lọc rộng gần gấp đôi so với nhãn đứng cạnh.
        //   hàng 0: Họ và tên | Đơn vị | Xếp loại | Ghi chú |
        //   hàng 1: [ô tên]   | [combo]| [combo]  | [combo] | [nút vuông]
        // Cột ô chia tỉ lệ (tên dài nhất); cột Xếp loại, cao hàng nhãn, cột nút do CanHangTren đặt theo DPI + nội dung.
        private void DinhKhoiTimKiem()
        {
            var t = tableLayoutPanel5;
            t.SuspendLayout();
            try
            {
                t.ColumnStyles.Clear();
                t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));      // 0 Họ và tên
                t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23F));      // 1 Đơn vị
                t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19F));      // 2 Xếp loại
                t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26F));      // 3 Ghi chú
                t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32F));     // 4 nút làm mới bộ lọc
                t.RowStyles.Clear();
                t.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));           // 0 nhãn
                t.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));           // 1 ô nhập
                t.RowCount = 2;
                t.Margin = new Padding(0);
                var cot = new (Label Nhan, Control O)[]
                {
                    (label3, textBox_TimKiemTheoTen), (label4, comboBox_TimKiemDonVi),
                    (label5, comboBox_XepLoaiThiDua), (label_LocGhiChu, comboBox_LocGhiChu)
                };
                for (int i = 0; i < cot.Length; i++)
                {
                    var (nhan, o) = cot[i];
                    DinhNhan(nhan, true);
                    nhan.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;   // sát ngay trên ô của nó
                    t.SetCellPosition(nhan, new TableLayoutPanelCellPosition(i, 0));
                    DinhODauVao(o);
                    t.SetCellPosition(o, new TableLayoutPanelCellPosition(i, 1));
                }
                t.SetCellPosition(kryptonButton_LamMoiCacOTimKiem, new TableLayoutPanelCellPosition(4, 1));
                t.ColumnCount = 5;   // sau khi đã dời hết control về 5 cột đầu
                KieuNut(kryptonButton_LamMoiCacOTimKiem, FontNut, NutVien);
                kryptonButton_LamMoiCacOTimKiem.Anchor = AnchorStyles.Right;   // vuông, sát mép phải, căn giữa theo chiều dọc
                kryptonButton_LamMoiCacOTimKiem.Margin = new Padding(0);
            }
            finally { t.ResumeLayout(false); }
        }
        // ---------- Thẻ 2: Khối lệnh ----------
        // Mỗi nút rộng ĐÚNG theo chữ dài nhất nó có thể hiện + biểu tượng (CanHangTren) => không bao giờ bị cắt chữ / bể biểu tượng;
        // thẻ Tìm kiếm ăn phần còn lại.
        private void DinhKhoiLenh()
        {
            tableLayoutPanel6.Margin = new Padding(0);
            tableLayoutPanel6.ColumnStyles.Clear();
            for (int i = 0; i < 4; i++) tableLayoutPanel6.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            // Cùng khung 2 hàng với thẻ Tìm kiếm (hàng 0 trống = chỗ nhãn) => nút lệnh thẳng hàng với ô lọc
            tableLayoutPanel6.RowStyles.Clear();
            tableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel6.RowCount = 2;
            for (int i = 0; i < NutLenh.Length; i++)
                tableLayoutPanel6.SetCellPosition(NutLenh[i], new TableLayoutPanelCellPosition(i, 1));
            KieuNut(kryptonButton1_ThiDuaBaNhat, FontNut, NutXanhNhat);
            KieuNut(kryptonButton_RefershCSDL, FontNut, NutVien);
            KieuNut(kryptonButton_XoaKetQuaPhanLoai, FontNut, NutNguyHiem);
            KieuNut(kryptonButton1_HuongDanThemDuLieu, FontNut, NutChinh);
            foreach (var b in NutLenh)
            {
                b.AutoSize = false;
                b.Anchor = AnchorStyles.Left | AnchorStyles.Right;   // lấp đầy cột (cột đã rộng đúng nội dung)
                b.Margin = new Padding(0);
            }
            // Biểu tượng vẽ lại theo chiều cao mới (không kéo giãn bitmap => không bể hình)
            foreach (var b in NutLenh) Module_BieuTuongNut.TuCoGianBieuTuong(b);
            Module_BieuTuongNut.TuCoGianBieuTuong(kryptonButton_LamMoiCacOTimKiem);
        }
        private Krypton.Toolkit.KryptonButton[] NutLenh => new[]
            { kryptonButton1_ThiDuaBaNhat, kryptonButton_RefershCSDL, kryptonButton_XoaKetQuaPhanLoai, kryptonButton1_HuongDanThemDuLieu };
        // Chữ dài nhất mỗi nút lệnh có thể hiện (code nghiệp vụ đổi chữ lúc chạy) => bề rộng cố định, thẻ không "nhảy" khi đổi chữ
        private static IEnumerable<string> ChuCoTheHien(Krypton.Toolkit.KryptonButton b)
        {
            yield return b.Values.Text ?? "";
            switch (b.Name)
            {
                case "kryptonButton_RefershCSDL": yield return "Đang tải..."; break;
                case "kryptonButton1_HuongDanThemDuLieu": yield return "Thêm CBCS"; yield return "Thêm tân binh"; yield return "Thêm học viên"; break;
            }
        }
        private int _dpiDaChia;
        private bool _choCanHangTren;
        /// <summary>
        /// Xin canh lại hàng trên. Mọi sự kiện (đổi cỡ, đổi DPI, đổi chữ nút...) đi qua đây: dồn nhiều yêu cầu thành 1 lần
        /// và chạy SAU KHI lượt dàn trang hiện tại xong (BeginInvoke). Trước đây canh ngay trong SizeChanged - tức là giữa lúc
        /// khung cha đang dàn trang - nên đổi cột chỉ ăn một phần: thu nhỏ/khôi phục xong thẻ Lệnh kẹt ở bề rộng dạng gọn,
        /// thẻ Tìm kiếm bị kéo giãn.
        /// </summary>
        private void YeuCauCanHangTren()
        {
            if (_choCanHangTren || _f.IsDisposed || !_f.IsHandleCreated) return;
            _choCanHangTren = true;
            _f.BeginInvoke(new Action(() => { _choCanHangTren = false; CanHangTren(); }));
        }
        // Nhãn xếp loại có thể hiện (cố định, KHÔNG lấy từ dữ liệu: giá trị lạ/dài trong CSDL không được kéo giãn bố cục)
        private static IEnumerable<string> ChuXepLoai() => new[]
        {
            Module_HeThong.Tat_Ca, "Không PL", Module_HeThong.PL_KHONG_PL, Module_HeThong.Loai_1, Module_HeThong.Loai_2,
            Module_HeThong.Loai_3, Module_HeThong.Loai_4, Module_HeThong.PL_CSTD, Module_HeThong.PL_CSTT,
            Module_HeThong.PL_HTNV, Module_HeThong.PL_KHTNV
        };
        /// <summary>
        /// Kích thước hàng trên (thẻ Tìm kiếm + Khối lệnh) + lề 3 thẻ theo DPI THẬT (100/125/150%...): cao hàng, cao/rộng nút,
        /// cột Xếp loại, cột nút làm mới bộ lọc. KHÔNG giữ trạng thái: kết quả chỉ phụ thuộc bề rộng + DPI hiện tại, nên
        /// thu nhỏ/phóng to/kéo hẹp-rộng bao nhiêu lần cũng về đúng bố cục mặc định.
        /// Hẹp dần: (1) ẩn logo; (2) nút lệnh chỉ còn biểu tượng (không bao giờ co nhỏ hơn biểu tượng => không bể hình).
        /// </summary>
        private void CanHangTren()
        {
            if (_f.IsDisposed || !_f.IsHandleCreated) return;
            // Đang thu nhỏ / chưa có bề rộng thật (khung chứa đang 0 px khi Form2 thu nhỏ): KHÔNG đổi gì, giữ bố cục cũ
            var goc = _f.TopLevel ? _f : _f.TopLevelControl as Form;
            if (goc != null && goc.WindowState == FormWindowState.Minimized) return;
            float k = Math.Max(1f, _f.DeviceDpi / 96f);
            int P(float v) => (int)Math.Round(v * k);
            int rongHang = tableLayoutPanel2.ClientSize.Width;
            if (rongHang < P(400)) return;
            var cacBang = new[] { tableLayoutPanel1, tableLayoutPanel2, tableLayoutPanel5, tableLayoutPanel6 };
            foreach (var t in cacBang) t.SuspendLayout();
            try
            {
                // Cao nút = cao ô nhập (cùng hàng nhìn thẳng đều), tối thiểu 30 px thiết kế => biểu tượng 18 px luôn đủ chỗ
                int cao = Math.Max(textBox_TimKiemTheoTen.Height, P(30));
                // Hàng trên: vùng tiêu đề thẻ + hàng nhãn + ô/nút + khoảng thở. Lề thẻ (cả thẻ Cập nhật) nhân DPI như chữ tiêu đề.
                var vungThe = new Padding(P(12), P(34), P(12), P(8));
                int caoNhan = label3.PreferredHeight + P(3);
                foreach (var t in new[] { tableLayoutPanel5, tableLayoutPanel6 })
                    DatHang(t, 0, caoNhan);
                var leO = new Padding(0, P(3), P(10), P(3));   // khe giữa các ô lọc
                var leNhan = new Padding(P(1), 0, P(10), P(2));
                foreach (var c in new Control[] { textBox_TimKiemTheoTen, comboBox_TimKiemDonVi, comboBox_XepLoaiThiDua, comboBox_LocGhiChu })
                    if (c.Margin != leO) c.Margin = leO;
                foreach (var lb in new[] { label3, label4, label5, label_LocGhiChu })
                    if (lb.Margin != leNhan) lb.Margin = leNhan;
                DatHang(tableLayoutPanel1, 0, vungThe.Vertical + caoNhan + cao + P(10));
                foreach (var the in new[] { _theTimKiem, _theLenh, _theCapNhat })
                    if (the != null && the.Padding != vungThe) the.Padding = vungThe;
                var leThe = new Padding(0, 0, P(10), 0);
                if (_theTimKiem != null && _theTimKiem.Margin != leThe) _theTimKiem.Margin = leThe;
                // Nút làm mới bộ lọc: vuông, cao bằng ô nhập
                var nutLoc = kryptonButton_LamMoiCacOTimKiem;
                var coNutLoc = new Size(cao, cao);
                if (nutLoc.MinimumSize != coNutLoc) nutLoc.MinimumSize = coNutLoc;
                if (nutLoc.MaximumSize != coNutLoc) nutLoc.MaximumSize = coNutLoc;
                if (nutLoc.Size != coNutLoc) nutLoc.Size = coNutLoc;
                DatCot(tableLayoutPanel5, 4, cao);
                // Cột Xếp loại: đủ hiện trọn nhãn dài nhất + mũi tên; 3 ô còn lại chia phần dư theo tỉ lệ
                int rongXL = 0;
                foreach (var t in ChuXepLoai())
                    rongXL = Math.Max(rongXL, TextRenderer.MeasureText(t ?? "", comboBox_XepLoaiThiDua.Font).Width);
                int cotXL = rongXL + P(40) + leO.Horizontal;
                DatCot(tableLayoutPanel5, 2, cotXL);
                // Nút lệnh: đầy đủ = chữ dài nhất + biểu tượng + lề trong; gọn = chỉ biểu tượng
                int khe = P(8);
                int coAnh = Math.Max(12, Math.Min(P(18), cao - P(10)));
                var nut = NutLenh;
                var rongDu = new int[nut.Length];
                var rongGon = new int[nut.Length];
                int tongDu = 0, tongGon = 0;
                for (int i = 0; i < nut.Length; i++)
                {
                    var font = nut[i].StateCommon.Content.ShortText.Font ?? nut[i].Font;
                    int chu = 0;
                    foreach (var t in ChuCoTheHien(nut[i])) chu = Math.Max(chu, TextRenderer.MeasureText(t, font).Width);
                    int kheSau = i < nut.Length - 1 ? khe : 0;
                    rongDu[i] = chu + coAnh + P(28) + kheSau;
                    rongGon[i] = cao + P(12) + kheSau;
                    tongDu += rongDu[i];
                    tongGon += rongGon[i];
                }
                // Chỗ còn cho 3 ô co giãn (tên, đơn vị, ghi chú) với logo/nút lệnh cho trước
                int ChoO(int logo, int lenh) => rongHang - logo - (lenh + vungThe.Horizontal + 2) - leThe.Horizontal - vungThe.Horizontal
                                                - 3 * leO.Horizontal - cao - cotXL;
                // Ngưỡng = phần chung tối thiểu để MỌI ô co giãn còn đủ chỗ: combo hiện trọn "Tất cả" + mũi tên, ô tên >= 110 px thiết kế.
                // Phần chung chia theo tỉ lệ cột (32/23/26) => ô nào hụt trước quyết định ngưỡng.
                int minCombo = TextRenderer.MeasureText(Module_HeThong.Tat_Ca, comboBox_TimKiemDonVi.Font).Width + P(48);   // (khe giữa ô đã trừ trong ChoO)
                int minTen = P(110);
                float tong = 0f;
                foreach (int i in new[] { 0, 1, 3 }) tong += tableLayoutPanel5.ColumnStyles[i].Width;
                int nguong = (int)Math.Ceiling(Math.Max(minTen * tong / tableLayoutPanel5.ColumnStyles[0].Width,
                             Math.Max(minCombo * tong / tableLayoutPanel5.ColumnStyles[1].Width, minCombo * tong / tableLayoutPanel5.ColumnStyles[3].Width)));
                bool anLogo = ChoO(P(88), tongDu) < nguong;
                bool gon = anLogo && ChoO(0, tongDu) < nguong;
                int tongLenh = 0;
                for (int i = 0; i < nut.Length; i++)
                {
                    var b = nut[i];
                    int kheSau = i < nut.Length - 1 ? khe : 0;
                    int r = gon ? rongGon[i] : rongDu[i];
                    DatCot(tableLayoutPanel6, i, r);
                    var m = new Padding(0, 0, kheSau, 0);
                    if (b.Margin != m) b.Margin = m;
                    var coMin = new Size(0, cao);
                    if (b.MinimumSize != coMin) b.MinimumSize = coMin;
                    if (b.MaximumSize != coMin) b.MaximumSize = coMin;
                    if (b.Height != cao) b.Height = cao;
                    // Dạng gọn: KHÔNG đổi chữ của nút (code nghiệp vụ còn đọc/ghi Values.Text) - chỉ để Krypton ẩn chữ không vừa,
                    // biểu tượng căn giữa. Chữ đầy đủ vẫn ở tooltip.
                    var trim = gon ? Krypton.Toolkit.PaletteTextTrim.Hide : Krypton.Toolkit.PaletteTextTrim.Inherit;
                    if (b.StateCommon.Content.ShortText.Trim != trim) b.StateCommon.Content.ShortText.Trim = trim;
                    var canAnh = gon ? Krypton.Toolkit.PaletteRelativeAlign.Center : Krypton.Toolkit.PaletteRelativeAlign.Inherit;
                    if (b.StateCommon.Content.Image.ImageH != canAnh) b.StateCommon.Content.Image.ImageH = canAnh;
                    tongLenh += r;
                }
                DatCot(tableLayoutPanel2, 2, tongLenh + vungThe.Horizontal + 2);
                if (pictureBox1.Visible == anLogo) pictureBox1.Visible = !anLogo;
                DatCot(tableLayoutPanel2, 0, anLogo ? 0 : P(88));
                var leLogo = new Padding(0, P(8), P(14), P(8));
                if (pictureBox1.Margin != leLogo) pictureBox1.Margin = leLogo;
                // Thẻ Cập nhật (2 hàng ô nhập): cao theo DPI, đặt 1 lần cho mỗi mức DPI (người dùng vẫn kéo thanh chia được)
                if (_dpiDaChia != _f.DeviceDpi)
                {
                    int caoO = Math.Max(textBox_QueQuan.Height, textBox_GhiChu.Height);
                    int chia = Math.Max(P(134), vungThe.Vertical + 2 * (caoO + P(12)));
                    var sc = splitContainer1;
                    if (sc.Height >= chia + sc.Panel2MinSize + sc.SplitterWidth)
                    {
                        sc.Panel1MinSize = Math.Min(sc.Panel1MinSize, chia);   // hạ trước (đổi DPI xuống) để gán khoảng chia không lỗi
                        sc.SplitterDistance = chia;
                        sc.Panel1MinSize = Math.Min(P(110), chia);
                        _dpiDaChia = _f.DeviceDpi;
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[Form6Web] CanHangTren: " + ex.Message); }
            finally
            {
                // Mở lại từ trong ra ngoài, rồi dàn lại cả cây MỘT lần với các kiểu cột/hàng mới
                for (int i = cacBang.Length - 1; i >= 0; i--) cacBang[i].ResumeLayout(false);
                tableLayoutPanel1.PerformLayout();
                tableLayoutPanel2.PerformLayout();
                tableLayoutPanel5.PerformLayout();
                tableLayoutPanel6.PerformLayout();
            }
        }
        private static void DatHang(TableLayoutPanel t, int hang, int cao)
        {
            if (hang >= t.RowStyles.Count) return;
            var st = t.RowStyles[hang];
            if (st.SizeType != SizeType.Absolute || Math.Abs(st.Height - cao) > 0.5f)
                t.RowStyles[hang] = new RowStyle(SizeType.Absolute, cao);
        }
        private static void DatCot(TableLayoutPanel t, int cot, int rong)
        {
            if (cot >= t.ColumnStyles.Count) return;
            var st = t.ColumnStyles[cot];
            if (st.SizeType != SizeType.Absolute || Math.Abs(st.Width - rong) > 0.5f)
                t.ColumnStyles[cot] = new ColumnStyle(SizeType.Absolute, rong);
        }
        // ---------- Thẻ 3: Form cập nhật thông tin (2 hàng ô nhập) ----------
        private void DinhKhoiCapNhat()
        {
            tableLayoutPanel3.Margin = new Padding(0);
            tableLayoutPanel8.Margin = new Padding(0);
            tableLayoutPanel9.Margin = new Padding(0);
            tableLayoutPanel9.ForeColor = MauChuDam; // (trước đây xanh đậm 0,0,192 -> ô nhập bị chữ xanh)
            // Hàng 1: STT | Họ và tên | Số hiệu | Năm sinh | Quê quán | [Xóa]
            tableLayoutPanel8.ColumnStyles.Clear();
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 0  nhãn STT
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 7F));       // 1  STT
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 2  nhãn Họ và tên
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31F));      // 3  Họ và tên
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 4  nhãn Số hiệu
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13F));      // 5  Số hiệu
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 6  nhãn Năm sinh
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11F));      // 7  Năm sinh
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 8  nhãn Quê quán
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));      // 9  Quê quán
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));    // 10 nút Xóa
            // Hàng 2: Vào CAND | Cấp bậc | Chức vụ | Đơn vị | Phân loại | Ghi chú | [Cập nhật]
            tableLayoutPanel9.ColumnStyles.Clear();
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 0  nhãn Vào CAND
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9F));       // 1  Vào CAND
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 2  nhãn Cấp bậc
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11F));      // 3  Cấp bậc
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 4  nhãn Chức vụ
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));      // 5  Chức vụ
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 6  nhãn Đơn vị
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));      // 7  Đơn vị
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 8  nhãn Phân loại
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));      // 9  Phân loại
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 10 nhãn Ghi chú
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));      // 11 Ghi chú
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));    // 12 nút Cập nhật
            DinhNhan(label6, true); DinhNhan(label7); DinhNhan(label8); DinhNhan(label9); DinhNhan(label10);
            DinhNhan(label12, true); DinhNhan(label13); DinhNhan(label14); DinhNhan(label11); DinhNhan(label16); DinhNhan(label15);
            DinhODauVao(textBox_STT);
            DinhODauVao(textBox_HoVaTen);
            DinhODauVao(textBox_SoHieu);
            DinhODauVao(textBox_NamSinh);
            DinhODauVao(textBox_QueQuan);
            DinhODauVao(textBox_NgayVaoCAND);
            DinhODauVao(textBox_CapBac);
            DinhODauVao(comboBox_ChucVu);
            DinhODauVao(textBox_DonVi);
            DinhODauVao(comboBox_PhanLoai);
            DinhODauVao(textBox_GhiChu);
            KieuNut(kryptonButton_XoaDataCBCS, FontNut, NutNguyHiem);
            KieuNut(kryptonButton_LuuDataCapNhat, FontNut, NutXanhLa);
            CanNutTheoO(kryptonButton_XoaDataCBCS, new Padding(10, 0, 0, 0));
            CanNutTheoO(kryptonButton_LuuDataCapNhat, new Padding(10, 0, 0, 0));
            // Cột nút trước đây cố định 120px (không nhân DPI) => ở 125-150% chữ + biểu tượng bị cắt ("bể nút").
            // Nay bề rộng cột / chiều cao nút tính theo DPI + chữ dài nhất mà nút có thể hiện, nên cả 2 nút luôn rộng bằng nhau.
            var nutCotCuoi = new[] { (tableLayoutPanel8, kryptonButton_XoaDataCBCS), (tableLayoutPanel9, kryptonButton_LuuDataCapNhat) };
            void CanCotNut()
            {
                try
                {
                    float k = Math.Max(1f, _f.DeviceDpi / 96f);
                    int rong = (int)Math.Round(120 * k);
                    foreach (var (_, b) in nutCotCuoi)
                    {
                        var font = b.StateCommon.Content.ShortText.Font ?? b.Font;
                        foreach (var chu in new[] { b.Values.Text ?? "", "Cập nhật", "Đang lưu..." })
                        {
                            // chữ + biểu tượng (~18px) + khoảng cách + lề trong nút + margin trái 10px
                            int can = TextRenderer.MeasureText(chu, font).Width + (int)Math.Round((18 + 8 + 24 + 10) * k);
                            rong = Math.Max(rong, can);
                        }
                    }
                    // Nút cao đúng bằng ô nhập Quê quán / Ghi chú (cao THẬT lúc chạy, đã theo DPI + font) => hai hàng thẳng đều
                    int cao = Math.Max(textBox_QueQuan.Height, textBox_GhiChu.Height);
                    if (cao <= 0) cao = (int)Math.Round(32 * k);
                    foreach (var (bang, b) in nutCotCuoi)
                    {
                        int cot = bang.ColumnStyles.Count - 1;
                        var st = bang.ColumnStyles[cot];
                        if (st.SizeType != SizeType.Absolute || Math.Abs(st.Width - rong) > 0.5f)
                            bang.ColumnStyles[cot] = new ColumnStyle(SizeType.Absolute, rong);
                        b.MinimumSize = new Size(0, cao);
                        b.Height = cao;
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[Form6Web] CanCotNut: " + ex.Message); }
            }
            CanCotNut();
            // Biểu tượng trên 2 nút vẽ lại theo chiều cao mới (không kéo giãn bitmap => không bể hình)
            Module_BieuTuongNut.TuCoGianBieuTuong(kryptonButton_XoaDataCBCS);
            Module_BieuTuongNut.TuCoGianBieuTuong(kryptonButton_LuuDataCapNhat);
            textBox_QueQuan.SizeChanged += (_, _) => CanCotNut();        // ô nhập đổi cao (font/DPI) => nút theo
            textBox_GhiChu.SizeChanged += (_, _) => CanCotNut();
            _f.Load += (_, _) => CanCotNut();                             // lúc Load DeviceDpi / font đã chắc chắn
            _f.DpiChanged += (_, _) => _f.BeginInvoke(new Action(CanCotNut)); // kéo cửa sổ sang màn hình khác DPI
            _f.DpiChangedAfterParent += (_, _) => { if (_f.IsHandleCreated) _f.BeginInvoke(new Action(CanCotNut)); }; // nằm trong Form2
        }
        // ---------- 4 ComboBox: ô chọn + danh sách sổ xuống bo tròn (dùng chung OComboWebF4 / PopupDanhSachF4 trong Module_GiaoDienForm4Web.cs) ----------
        // ComboBox gốc vẫn còn nguyên (Items, Text, SelectedIndex, sự kiện, Enabled/Visible do logic điều khiển); chỉ được ẩn hình và
        // thay bằng ô vẽ tay đặt đúng vị trí. Chọn trên ô mới sẽ gán SelectedIndex của ComboBox gốc nên mọi sự kiện chạy như cũ.
        private void DinhComboBoTron()
        {
            foreach (var cb in new[] { comboBox_TimKiemDonVi, comboBox_XepLoaiThiDua, comboBox_LocGhiChu, comboBox_ChucVu, textBox_CapBac, textBox_DonVi, comboBox_PhanLoai })
                if (cb != null && cb.Parent != null) OComboWebF4.Gan(cb);
        }
        // ---------- Lưới dữ liệu nằm trong 1 thẻ trắng bo góc ----------
        private void DinhVungLuoi()
        {
            var the = new TheBoGocF6 { Dock = DockStyle.Fill, Padding = new Padding(10) };
            var p2 = splitContainer1.Panel2;
            p2.SuspendLayout();
            try
            {
                p2.Controls.Add(the);
                the.Controls.Add(kryptonDataGridView1);   // tự gỡ khỏi Panel2, giữ nguyên mọi sự kiện/logic của lưới
                kryptonDataGridView1.Dock = DockStyle.Fill;
                kryptonDataGridView1.BorderStyle = BorderStyle.None;
                kryptonDataGridView1.CellPainting -= VeOGocSTT;
                kryptonDataGridView1.CellPainting += VeOGocSTT;   // đặt tên cho ô góc trên-trái (cột số thứ tự)
            }
            finally
            {
                p2.ResumeLayout(true);
            }
        }
        // Ô góc trên-trái của lưới (phía trên cột số thứ tự) mặc định để trống -> vẽ chữ "STT" cùng tông với tiêu đề cột.
        // Tự vẽ nền + chữ nên không phụ thuộc cách Krypton vẽ ô này; chỉ trình bày, không đụng dữ liệu.
        private void VeOGocSTT(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex != -1 || e.ColumnIndex != -1 || sender is not DataGridView dgv) return;
            var o = e.CellBounds;
            using (var nen = new SolidBrush(Color.FromArgb(237, 233, 254)))
                e.Graphics!.FillRectangle(nen, o);
            using (var vien = new Pen(Color.FromArgb(221, 214, 254)))
            {
                e.Graphics.DrawLine(vien, o.Left, o.Bottom - 1, o.Right - 1, o.Bottom - 1);
                e.Graphics.DrawLine(vien, o.Right - 1, o.Top, o.Right - 1, o.Bottom - 1);
            }
            var font = dgv.ColumnHeadersDefaultCellStyle.Font ?? dgv.Font;   // cùng font đậm với tiêu đề cột
            TextRenderer.DrawText(e.Graphics, "STT", font, o, Color.FromArgb(91, 33, 182),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            e.Handled = true;
        }
        // ---------- Thanh trạng thái ----------
        private void DinhThanhTrangThai()
        {
            statusStrip1.BackColor = Color.White;               // (trước đây xanh lá nhạt)
            statusStrip1.ForeColor = MauChuNhan;
            statusStrip1.SizingGrip = false;
            statusStrip1.Padding = new Padding(10, 0, 10, 0);
            toolStripStatusLabel1.Font = FontTrangThai;
            foreach (var lbl in new[] { toolStripStatusLabel2_Loai1, toolStripStatusLabel2_Loai2, toolStripStatusLabel2_Loai3,
                                        toolStripStatusLabel2_Loai4, toolStripStatusLabel2_KhongPL })
            {
                lbl.Font = FontTrangThai;                       // màu chữ từng loại do Form6_Load đặt, giữ nguyên
                lbl.Margin = new Padding(6, 3, 10, 2);
            }
        }
        // ---------- Menu chuột phải ----------
        private void DinhMenuNguCanh()
        {
            contextMenuStrip1.Font = FontMenu;
            contextMenuStrip1.Renderer = new ToolStripProfessionalRenderer(new BangMauMenuF6()) { RoundedEdges = false };
        }
        // =====================================================================================
        //  2) ÁP DỤNG LẠI SAU Form6_Load (các thuộc tính bị code gốc đặt lại trong Load)
        // =====================================================================================
        private void HoanThienGiaoDienSauLoad()
        {
            try
            {
                // Ô nhập Krypton: bo góc đồng nhất (DangKyHieuUngVienTextBox của code gốc đặt = 3)
                foreach (var t in new[] { textBox_TimKiemTheoTen, textBox_STT, textBox_HoVaTen, textBox_SoHieu, textBox_NamSinh,
                                          textBox_QueQuan, textBox_NgayVaoCAND, textBox_GhiChu })
                {
                    t.StateCommon.Border.Rounding = 6F;
                }
                DinhGiaoDienLuoi();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[GiaoDienWeb] " + ex.Message); // giao diện không bao giờ được làm sập form
            }
        }
        // Lưới: tiêu đề xám nhạt, chỉ kẻ ngang từng dòng, dòng chọn xanh nhạt (ghi đè tông tím của DinhDangDataGridViewHienDai)
        private void DinhGiaoDienLuoi()
        {
            var dgv = kryptonDataGridView1;
            dgv.SuspendLayout();
            try
            {
                dgv.StateCommon.Background.Color1 = Color.White;
                // Tiêu đề cột: nền tím nhạt + chữ tím đậm (đúng tông yêu cầu)
                dgv.StateCommon.HeaderColumn.Back.Color1 = Color.FromArgb(237, 233, 254);
                dgv.StateCommon.HeaderColumn.Back.Color2 = Color.FromArgb(237, 233, 254);
                dgv.StateCommon.HeaderColumn.Content.Color1 = Color.FromArgb(91, 33, 182);
                dgv.StateCommon.HeaderColumn.Border.Color1 = Color.FromArgb(221, 214, 254);
                dgv.StateCommon.HeaderColumn.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
                // Cột số thứ tự bên trái: tím rất nhạt cho liền tông
                dgv.StateCommon.HeaderRow.Back.Color1 = Color.FromArgb(250, 248, 255);
                dgv.StateCommon.HeaderRow.Back.Color2 = Color.FromArgb(250, 248, 255);
                dgv.StateCommon.HeaderRow.Border.Color1 = Color.FromArgb(237, 233, 254);
                dgv.RowHeadersDefaultCellStyle.ForeColor = MauChuPhu;   // số thứ tự do RowPostPaint vẽ
                dgv.StateCommon.DataCell.Back.Color1 = Color.White;
                dgv.StateCommon.DataCell.Content.Color1 = Color.FromArgb(51, 65, 85);
                dgv.StateCommon.DataCell.Border.Color1 = Color.FromArgb(241, 245, 249);
                dgv.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Bottom;
                dgv.StateSelected.DataCell.Back.Color1 = Color.FromArgb(239, 246, 255);
                dgv.StateSelected.DataCell.Back.Color2 = Color.FromArgb(239, 246, 255);
                dgv.StateSelected.DataCell.Content.Color1 = Color.FromArgb(29, 78, 216);
            }
            finally
            {
                dgv.ResumeLayout();
            }
        }
        // =====================================================================================
        //  3) HÀM TIỆN ÍCH TRÌNH BÀY
        // =====================================================================================
        // Nhãn: chữ thường, màu slate (bỏ in nghiêng + xanh đậm cũ), căn trái, có khoảng cách với ô nhập
        private static void DinhNhan(Label lb, bool dauHang = false)
        {
            lb.Font = FontNhan;
            lb.ForeColor = MauChuNhan;
            lb.Anchor = AnchorStyles.Left;
            lb.Margin = new Padding(dauHang ? 0 : 10, 0, 6, 0);
        }
        // Ô nhập (TextBox Krypton / ComboBox): lấp đầy ô lưới, khoảng đệm dọc đều
        private static void DinhODauVao(Control c)
        {
            c.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            c.Margin = new Padding(0, 3, 0, 3);
        }
        // Nút: lấp đầy chiều ngang ô, cao đều 32px -> tự co giãn đẹp khi đổi kích thước cửa sổ
        private static void CanNutTheoO(Krypton.Toolkit.KryptonButton b, Padding margin)
        {
            b.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            b.Margin = margin;
            b.Height = 32;
        }
        private static void KieuNut(Krypton.Toolkit.KryptonButton b, Font font,
            (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) m)
        {
            b.StateCommon.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            b.StateCommon.Border.Rounding = 8F;
            b.StateCommon.Border.Width = 1;
            b.StateCommon.Content.ShortText.Font = font;
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
            // Trạng thái có FOCUS (sau khi bấm nút, nút giữ focus; khi form ẩn/hiện lại Krypton vẽ bằng OverrideFocus/OverrideDefault).
            // Nếu không đặt, nó rơi về nền trắng của theme -> chữ trắng trên nền trắng, nhìn như nút trống.
            foreach (var ov in new[] { b.OverrideFocus, b.OverrideDefault })
            {
                ov.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
                ov.Back.Color1 = m.Nen; ov.Back.Color2 = m.Nen;
                ov.Border.Color1 = m.Vien; ov.Border.Color2 = m.Vien;
                ov.Border.Rounding = 8F;
                ov.Border.Width = 1;
                ov.Content.ShortText.Color1 = m.Chu;
                ov.Content.ShortText.Color2 = m.Chu;
            }
            // Trạng thái vô hiệu hóa (khi đang làm mới/xử lý): xám nhạt, vẫn đọc được chữ
            b.StateDisabled.Back.Color1 = Color.FromArgb(241, 245, 249); b.StateDisabled.Back.Color2 = Color.FromArgb(241, 245, 249);
            b.StateDisabled.Border.Color1 = Color.FromArgb(226, 232, 240); b.StateDisabled.Border.Color2 = Color.FromArgb(226, 232, 240);
            b.StateDisabled.Content.ShortText.Color1 = Color.FromArgb(148, 163, 184);
        }
        private static GraphicsPath TaoDuongBoGoc(Rectangle r, int d)
        {
            var path = new GraphicsPath();
            if (d < 2) { path.AddRectangle(r); return path; }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
        // =====================================================================================
        //  4) LỚP PHỤ TRỢ TRÌNH BÀY
        // =====================================================================================
        /// <summary>Thẻ trắng bo góc có viền mảnh (nền ngoài lấy màu của control cha).</summary>
        internal sealed class TheBoGocF6 : Panel
        {
            private string _tieuDe = "";
            /// <summary>Tiêu đề vẽ ở góc trên trái thẻ (có thanh nhấn xanh). Rỗng = không vẽ.</summary>
            public string TieuDe
            {
                get => _tieuDe;
                set { _tieuDe = value ?? ""; Invalidate(); }
            }
            public TheBoGocF6()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
                BackColor = Color.White;
            }
            protected override void OnPaintBackground(PaintEventArgs e) =>
                e.Graphics.Clear(Parent?.BackColor ?? Color.FromArgb(243, 244, 246));
            protected override void OnPaint(PaintEventArgs e)
            {
                if (Width < 4 || Height < 4) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int d = (int)(20 * DeviceDpi / 96f); // bán kính 10 (scale theo DPI)
                var r = new Rectangle(0, 0, Width - 1, Height - 1);
                using var path = TaoDuongBoGoc(r, d);
                using var b = new SolidBrush(Color.White);
                using var p = new Pen(Color.FromArgb(226, 232, 240));
                g.FillPath(b, path);
                g.DrawPath(p, path);
                if (_tieuDe.Length > 0)
                {
                    float k = DeviceDpi / 96f;
                    int x = (int)(14 * k), w = Math.Max(3, (int)(3 * k));
                    using (var bar = new SolidBrush(MauChinh))
                        g.FillRectangle(bar, x, (int)(12 * k), w, (int)(14 * k));
                    TextRenderer.DrawText(g, _tieuDe, FontTieuDeThe, new Point(x + w + (int)(7 * k), (int)(10 * k)), MauChuDam,
                        TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.Left | TextFormatFlags.SingleLine);
                }
            }
        }
        /// <summary>Bảng màu menu chuột phải: nền trắng, hover xanh nhạt, viền mảnh.</summary>
        internal sealed class BangMauMenuF6 : ProfessionalColorTable
        {
            public BangMauMenuF6() { UseSystemColors = false; }
            public override Color ToolStripDropDownBackground => Color.White;
            public override Color ImageMarginGradientBegin => Color.White;
            public override Color ImageMarginGradientMiddle => Color.White;
            public override Color ImageMarginGradientEnd => Color.White;
            public override Color MenuBorder => Color.FromArgb(226, 232, 240);
            public override Color MenuItemBorder => Color.FromArgb(191, 219, 254);
            public override Color MenuItemSelected => Color.FromArgb(239, 246, 255);
            public override Color MenuItemSelectedGradientBegin => Color.FromArgb(239, 246, 255);
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(239, 246, 255);
            public override Color SeparatorDark => Color.FromArgb(226, 232, 240);
            public override Color SeparatorLight => Color.White;
        }
    }
}