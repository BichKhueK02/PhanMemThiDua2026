using System.Drawing.Drawing2D;
using System.Reflection;
///Phiên bản 1
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CỦA Form29_CBCSTrongDienQuanLy  —  CHỈ TRÌNH BÀY, KHÔNG CHỨA LOGIC NGHIỆP VỤ
    //  Module độc lập (theo mẫu Module_GiaoDienForm6Web): không sửa Designer, không đổi tên/kiểu/sự kiện control nào.
    //
    //  Cách dùng: trong constructor Form29_CBCSTrongDienQuanLy, ngay sau InitializeComponent();
    //        Module_GiaoDienForm29Web.ApDung(this);
    //
    //  - Control của form (private trong Designer) được lấy theo TÊN bằng reflection.
    //  - ComboBox gốc vẫn còn nguyên (Items, DataSource, Text, SelectedIndex, sự kiện): chỉ bị ẩn hình và thay bằng
    //    OComboF29 (ô bo tròn, cao 36px bằng nút) + khung danh sách sổ xuống cũng bo tròn (DanhSachF29).
    //    Chọn trên ô mới sẽ gán SelectedIndex của ComboBox gốc nên mọi sự kiện chạy như cũ.
    //  - Ô tìm kiếm Krypton cũng được thay bằng OTimF29 (bo tròn, cao 36px); Text hai bên luôn đồng bộ.
    //  - Module tự chứa toàn bộ lớp phụ trợ (hậu tố F29) -> KHÔNG phụ thuộc Module_GiaoDienForm4Web.
    //  - Lỗi trình bày không bao giờ làm form không mở được (bọc try/catch).
    //  - Mọi kích thước (px) viết theo đơn vị thiết kế 96 DPI.
    // =====================================================================================
    internal sealed class Module_GiaoDienForm29Web
    {
        private readonly Form _f;
        // ---------- Các control của form (lấy theo tên, cùng tên với Designer) ----------
        private readonly TableLayoutPanel tableLayoutPanel1;
        private readonly TableLayoutPanel tableLayoutPanel2;
        private readonly TableLayoutPanel tableLayoutPanel3;
        private readonly TableLayoutPanel tableLayoutPanel4;
        private readonly GroupBox groupBox1;
        private readonly GroupBox groupBox2;
        private readonly Label label1;
        private readonly Label label2;
        private readonly Label label4;
        private readonly Label label5;
        private readonly PictureBox pictureBox3;
        private readonly ComboBox comboBox1_GhiChu;
        private readonly ComboBox comboBox_XepLoaiThiDua;
        private readonly ComboBox comboBox_TimKiemDonVi;
        private readonly Krypton.Toolkit.KryptonTextBox textBoxKyToon_TimTen;
        private readonly Krypton.Toolkit.KryptonButton kryptonButton1_Dong;
        private readonly Krypton.Toolkit.KryptonButton kryptonButton1_TimKiemThongTinCBCSQuanLy;
        private readonly Krypton.Toolkit.KryptonButton kryptonButton_LamMoiCacOTimKiem;
        private readonly Krypton.Toolkit.KryptonDataGridView kryptonDataGridView2;
        private readonly StatusStrip statusStrip1;
        public static void ApDung(Form f)
        {
            try { new Module_GiaoDienForm29Web(f).Chay(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[Module_GiaoDienForm29Web] " + ex.Message); }
        }
        private Module_GiaoDienForm29Web(Form f)
        {
            _f = f;
            tableLayoutPanel1 = Lay<TableLayoutPanel>("tableLayoutPanel1");
            tableLayoutPanel2 = Lay<TableLayoutPanel>("tableLayoutPanel2");
            tableLayoutPanel3 = Lay<TableLayoutPanel>("tableLayoutPanel3");
            tableLayoutPanel4 = Lay<TableLayoutPanel>("tableLayoutPanel4");
            groupBox1 = Lay<GroupBox>("groupBox1");
            groupBox2 = Lay<GroupBox>("groupBox2");
            label1 = Lay<Label>("label1");
            label2 = Lay<Label>("label2");
            label4 = Lay<Label>("label4");
            label5 = Lay<Label>("label5");
            pictureBox3 = Lay<PictureBox>("pictureBox3");
            comboBox1_GhiChu = Lay<ComboBox>("comboBox1_GhiChu");
            comboBox_XepLoaiThiDua = Lay<ComboBox>("comboBox_XepLoaiThiDua");
            comboBox_TimKiemDonVi = Lay<ComboBox>("comboBox_TimKiemDonVi");
            textBoxKyToon_TimTen = Lay<Krypton.Toolkit.KryptonTextBox>("textBoxKyToon_TimTen");
            kryptonButton1_Dong = Lay<Krypton.Toolkit.KryptonButton>("kryptonButton1_Dong");
            kryptonButton1_TimKiemThongTinCBCSQuanLy = Lay<Krypton.Toolkit.KryptonButton>("kryptonButton1_TimKiemThongTinCBCSQuanLy");
            kryptonButton_LamMoiCacOTimKiem = Lay<Krypton.Toolkit.KryptonButton>("kryptonButton_LamMoiCacOTimKiem");
            kryptonDataGridView2 = Lay<Krypton.Toolkit.KryptonDataGridView>("kryptonDataGridView2");
            statusStrip1 = Lay<StatusStrip>("statusStrip1");
        }
        private T Lay<T>(string ten) where T : class
        {
            var fi = _f.GetType().GetField(ten, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                     ?? throw new MissingFieldException(_f.GetType().Name, ten);
            return (T)fi.GetValue(_f)!;
        }
        // ---------- BẢNG MÀU (nền xám nhạt, thẻ trắng, điểm nhấn tím + xanh) ----------
        private static readonly Color MauNen = Color.FromArgb(243, 244, 246);
        private static readonly Color MauVienThe = Color.FromArgb(226, 232, 240);
        private static readonly Color MauChuDam = Color.FromArgb(17, 24, 39);
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);
        private static readonly Color MauChuMo = Color.FromArgb(107, 114, 128);
        private static readonly Color MauVienO = Color.FromArgb(209, 213, 219);
        private static readonly Color MauTim = Color.FromArgb(124, 58, 237);
        private static readonly Color MauTimChu = Color.FromArgb(91, 33, 182);
        private static readonly Color MauTimNen = Color.FromArgb(243, 240, 255);
        private static readonly Color MauTimNenHover = Color.FromArgb(233, 228, 254);
        private static readonly Color MauTimVienNhat = Color.FromArgb(196, 181, 253);
        // Màu ô lưới: dùng chung cho Krypton (bên dưới) và cho CellPainting tự vẽ ở form (ô có huy hiệu) -> hai nơi luôn khớp nhau
        internal static readonly Color MauODong = Color.White;
        internal static readonly Color MauOChu = Color.FromArgb(51, 65, 85);
        internal static readonly Color MauOChon = Color.FromArgb(239, 246, 255);
        internal static readonly Color MauOChonChu = Color.FromArgb(29, 78, 216);
        internal static readonly Color MauKeDong = Color.FromArgb(241, 245, 249);
        private const int CaoO = 36;   // chiều cao ô nhập / ô chọn / nút (đơn vị 96 DPI)
        private static readonly Font FontO = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNhan = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new Font("Segoe UI Semibold", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontTieuDeThe = new Font("Segoe UI Semibold", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontTieuDeTrang = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point);
        private static readonly Font FontMoTa = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        // Bộ màu nút: (Nền, Hover, Nhấn, Viền, Chữ)
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutChinh =
            (Color.FromArgb(37, 99, 235), Color.FromArgb(29, 78, 216), Color.FromArgb(30, 64, 175), Color.FromArgb(37, 99, 235), Color.White);
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutTimNhat =
            (Color.FromArgb(243, 240, 255), Color.FromArgb(233, 228, 254), Color.FromArgb(221, 214, 254), Color.FromArgb(205, 193, 250), Color.FromArgb(91, 33, 182));
        // =====================================================================================
        //  1) ÁP DỤNG GIAO DIỆN
        // =====================================================================================
        private void Chay()
        {
            _f.SuspendLayout();
            try
            {
                _f.BackColor = MauNen;
                DinhBoCuc();
                DinhTheTieuDe();
                DinhKhoiTimKiem();
                DinhKhoiLoc();
                DinhComboBoTron();
                DinhVungLuoi();
                DinhGiaoDienLuoi();
                DinhThanhTrangThai();
            }
            finally
            {
                _f.ResumeLayout(true);
            }
            // Sau Form29_Load (đăng ký sau nên chạy sau): áp lại kiểu lưới
            _f.Load += (s, e) => HoanThienSauLoad();
        }
        // ---------- Bố cục tổng: [tiêu đề] / [2 thẻ tìm kiếm + lọc] / [thẻ lưới co giãn] ----------
        private void DinhBoCuc()
        {
            tableLayoutPanel3.Controls.Remove(kryptonButton1_Dong);   // nút Đóng chuyển lên tiêu đề trang
            var tieuDe = TaoTieuDeTrang();
            tableLayoutPanel2.SuspendLayout();
            try
            {
                tableLayoutPanel2.BackColor = MauNen;
                tableLayoutPanel2.Margin = new Padding(0);
                tableLayoutPanel2.Padding = new Padding(20, 0, 20, 12);
                tableLayoutPanel2.RowStyles.Clear();
                tableLayoutPanel2.RowCount = 3;
                tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, Px(_f, 84)));    // tiêu đề trang
                // 2 thẻ tìm kiếm + lọc: tiêu đề thẻ 34 + ô chọn CaoO + lề ô 6 + đệm dưới 8 + dư 10.
                // PHẢI nhân theo DPI giống ô chọn (OComboF29 cao Px(CaoO)); để số cứng 92 thì laptop 125%/150% bị cắt mất chân ô.
                tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, Px(_f, 34 + CaoO + 6 + 8 + 10)));
                tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));    // thẻ lưới
                tableLayoutPanel2.SetRow(kryptonDataGridView2, 2);
                tableLayoutPanel2.SetRow(tableLayoutPanel3, 1);
                tableLayoutPanel2.Controls.Add(tieuDe, 0, 0);
                tableLayoutPanel3.Margin = new Padding(0);
                tableLayoutPanel3.BackColor = MauNen;
                tableLayoutPanel3.ColumnStyles.Clear();
                tableLayoutPanel3.ColumnCount = 2;
                tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
                tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
            }
            finally
            {
                tableLayoutPanel2.ResumeLayout(true);
            }
        }
        private Panel TaoTieuDeTrang()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0), BackColor = MauNen };
            var pnlNut = new Panel { Dock = DockStyle.Right, Width = 130, BackColor = MauNen, Padding = new Padding(0, 22, 20, 0) };
            KieuNut(kryptonButton1_Dong, NutTimNhat);
            kryptonButton1_Dong.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            kryptonButton1_Dong.Dock = DockStyle.Top;
            kryptonButton1_Dong.Height = CaoO;
            pnlNut.Controls.Add(kryptonButton1_Dong);
            var lblTieuDe = new Label
            {
                AutoSize = true,
                BackColor = MauNen,
                Font = FontTieuDeTrang,
                ForeColor = MauTim,
                Location = new Point(0, 12),
                Text = _f.Text
            };
            var lblMoTa = new Label
            {
                AutoSize = true,
                BackColor = MauNen,
                Font = FontMoTa,
                ForeColor = MauChuMo,
                Location = new Point(3, 54),
                Text = "Tìm kiếm, lọc theo đơn vị – xếp loại – ghi chú và xuất danh sách CBCS thuộc diện quản lý"
            };
            pnl.Controls.Add(lblMoTa);
            pnl.Controls.Add(lblTieuDe);
            pnl.Controls.Add(pnlNut);
            return pnl;
        }
        // ---------- Biến 2 GroupBox thành "thẻ" trắng bo góc có tiêu đề (GroupBox được giữ lại ở dạng ẩn) ----------
        private void DinhTheTieuDe()
        {
            BocTheTieuDe(groupBox2, tableLayoutPanel4, new Padding(0, 0, 10, 0));
            BocTheTieuDe(groupBox1, tableLayoutPanel1, new Padding(0));
        }
        private static void BocTheTieuDe(GroupBox gb, Control noiDung, Padding margin)
        {
            var cha = gb.Parent;
            if (cha == null) return;
            var the = new TheBoGocF29
            {
                Dock = DockStyle.Fill,
                Margin = margin,
                TieuDe = gb.Text,
                // 34 = vùng tiêu đề (thẻ vẽ tiêu đề theo DPI nên đệm cũng phải theo DPI, không thì tiêu đề đè lên ô nhập)
                Padding = new Padding(Px(cha, 12), Px(cha, 34), Px(cha, 12), Px(cha, 8))
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
                the.Controls.Add(noiDung);       // chuyển bảng con vào thẻ (giữ nguyên mọi control bên trong)
                noiDung.Dock = DockStyle.Fill;
                the.Controls.Add(gb);            // giữ GroupBox (ẩn) để code gán .Text vẫn chạy, đồng bộ lên tiêu đề thẻ
                gb.TextChanged += (s, e) => the.TieuDe = gb.Text;
            }
            finally
            {
                cha.ResumeLayout(true);
            }
        }
        // ---------- Thẻ 1: Tìm kiếm  [ô nhập ........][ Tìm kiếm ] ----------
        private void DinhKhoiTimKiem()
        {
            var t = tableLayoutPanel4;
            t.Margin = new Padding(0);
            t.BackColor = Color.White;
            pictureBox3.Visible = false;     // biểu tượng + nhãn "Tên CBCS" không còn cần (đã có chữ gợi ý trong ô)
            label2.Visible = false;
            t.ColumnStyles.Clear();
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124F));
            var vt = t.GetPositionFromControl(textBoxKyToon_TimTen);
            textBoxKyToon_TimTen.Visible = false;   // ô Krypton gốc vẫn giữ Text/sự kiện; ô mới đồng bộ hai chiều
            var o = new OTimF29(textBoxKyToon_TimTen, Module_HeThong.Nhap_Tim_Kiem)
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 3, 10, 3),
                TabIndex = textBoxKyToon_TimTen.TabIndex,
                Name = "oTimF29"
            };
            o.NhanEnter += (s, e) => kryptonButton1_TimKiemThongTinCBCSQuanLy.PerformClick();
            t.Controls.Add(o, vt.Column, vt.Row);
            KieuNut(kryptonButton1_TimKiemThongTinCBCSQuanLy, NutChinh);
            CanNutTheoO(kryptonButton1_TimKiemThongTinCBCSQuanLy, new Padding(0));
        }
        // ---------- Thẻ 2: Lọc  Đơn vị [▼]  Xếp loại [▼]  Ghi chú [▼]  [⟳] ----------
        private void DinhKhoiLoc()
        {
            var t = tableLayoutPanel1;
            t.Margin = new Padding(0);
            t.BackColor = Color.White;
            t.ColumnStyles.Clear();
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 0 nhãn Đơn vị
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));      // 1 Đơn vị
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 2 nhãn Xếp loại
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24F));      // 3 Xếp loại
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 4 nhãn Ghi chú
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46F));      // 5 Ghi chú
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56F));     // 6 nút làm mới
            DinhNhan(label4, true);
            DinhNhan(label5);
            DinhNhan(label1);
            KieuNut(kryptonButton_LamMoiCacOTimKiem, NutTimNhat);
            CanNutTheoO(kryptonButton_LamMoiCacOTimKiem, new Padding(8, 0, 0, 0));
        }
        // ---------- 3 ComboBox -> ô chọn bo tròn + danh sách sổ xuống bo tròn (OComboF29 / DanhSachF29 của module này) ----------
        // ComboBox gốc vẫn còn nguyên (Items, SelectedIndex, sự kiện), chỉ bị ẩn; chọn trên ô mới sẽ gán SelectedIndex của nó.
        private void DinhComboBoTron()
        {
            foreach (var cb in new[] { comboBox_TimKiemDonVi, comboBox_XepLoaiThiDua, comboBox1_GhiChu })
                ThayComboBoTron(cb, new Padding(0, 3, 6, 3));
        }
        // ComboBox gốc -> ẩn (giữ nguyên trong ô lưới để dữ liệu/sự kiện chạy như cũ), đặt OComboF29 vào đúng ô
        private static void ThayComboBoTron(ComboBox cb, Padding margin)
        {
            if (cb.Parent is not TableLayoutPanel tlp) return;
            var vt = tlp.GetPositionFromControl(cb);
            var o = new OComboF29(cb)
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = margin,
                TabIndex = cb.TabIndex,
                Name = cb.Name + "_F29"
            };
            cb.Visible = false;
            tlp.Controls.Add(o, vt.Column, vt.Row);
        }
        // ---------- Lưới dữ liệu nằm trong 1 thẻ trắng bo góc ----------
        private void DinhVungLuoi()
        {
            var the = new TheBoGocF29 { Dock = DockStyle.Fill, Margin = new Padding(0, 10, 0, 0), Padding = new Padding(8) };
            tableLayoutPanel2.Controls.Add(the, 0, 2);
            the.Controls.Add(kryptonDataGridView2);   // tự gỡ khỏi tableLayoutPanel2, giữ nguyên mọi sự kiện/logic của lưới
            kryptonDataGridView2.Dock = DockStyle.Fill;
            kryptonDataGridView2.Margin = new Padding(0);
            kryptonDataGridView2.BorderStyle = BorderStyle.None;
        }
        // Lưới: tiêu đề tím nhạt, chỉ kẻ ngang từng dòng, dòng chọn xanh nhạt, dòng cao 36
        private void DinhGiaoDienLuoi()
        {
            var dgv = kryptonDataGridView2;
            dgv.SuspendLayout();
            try
            {
                dgv.RowTemplate.Height = 36;
                dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
                dgv.AllowUserToResizeRows = false;
                dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                dgv.ColumnHeadersHeight = 52;
                dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgv.GridStyles.Style = Krypton.Toolkit.DataGridViewStyle.List;
                dgv.StateCommon.Background.Color1 = Color.White;
                dgv.StateCommon.HeaderColumn.Content.Padding = new Padding(6, 8, 6, 8);
                dgv.StateCommon.HeaderColumn.Content.Font = new Font(Module_HeThong.TenFontHeThong, 9.5F, FontStyle.Bold);
                dgv.StateCommon.HeaderColumn.Back.Color1 = Color.FromArgb(237, 233, 254);
                dgv.StateCommon.HeaderColumn.Back.Color2 = Color.FromArgb(237, 233, 254);
                dgv.StateCommon.HeaderColumn.Content.Color1 = Color.FromArgb(91, 33, 182);
                dgv.StateCommon.HeaderColumn.Border.Color1 = Color.FromArgb(221, 214, 254);
                dgv.StateCommon.HeaderColumn.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
                dgv.StateCommon.DataCell.Content.Font = new Font(Module_HeThong.TenFontHeThong, 9.5F, FontStyle.Regular);
                dgv.StateCommon.DataCell.Content.Padding = new Padding(6, 8, 6, 8);
                dgv.StateCommon.DataCell.Back.Color1 = MauODong;
                dgv.StateCommon.DataCell.Content.Color1 = MauOChu;
                dgv.StateCommon.DataCell.Border.Color1 = MauKeDong;
                dgv.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Bottom;
                dgv.StateSelected.DataCell.Back.Color1 = MauOChon;
                dgv.StateSelected.DataCell.Back.Color2 = MauOChon;
                dgv.StateSelected.DataCell.Content.Color1 = MauOChonChu;
                // Kiểu ô chuẩn của DataGridView cũng đặt cùng màu: ô tự vẽ (CellPainting) đọc từ đây
                var kieu = dgv.DefaultCellStyle;
                kieu.BackColor = MauODong; kieu.ForeColor = MauOChu;
                kieu.SelectionBackColor = MauOChon; kieu.SelectionForeColor = MauOChonChu;
            }
            finally
            {
                dgv.ResumeLayout();
            }
        }
        // ---------- Thanh trạng thái ----------
        private void DinhThanhTrangThai()
        {
            statusStrip1.BackColor = Color.White;
            statusStrip1.SizingGrip = false;
            statusStrip1.Padding = new Padding(10, 0, 10, 0);
        }
        // =====================================================================================
        //  2) ÁP DỤNG LẠI SAU Form29_Load
        // =====================================================================================
        private void HoanThienSauLoad()
        {
            try
            {
                // Form29_Load đặt lại một số thuộc tính của lưới -> áp lại kiểu lưới sau Load
                DinhGiaoDienLuoi();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[GiaoDienForm29Web] " + ex.Message);
            }
        }
        // =====================================================================================
        //  3) HÀM TIỆN ÍCH TRÌNH BÀY
        // =====================================================================================
        private static void DinhNhan(Label lb, bool dauHang = false)
        {
            lb.Font = FontNhan;
            lb.ForeColor = MauChuNhan;
            lb.BackColor = Color.White;
            lb.Anchor = AnchorStyles.Left;
            lb.Margin = new Padding(dauHang ? 0 : 10, 0, 6, 0);
        }
        // Nút: lấp đầy chiều ngang ô, cao bằng ô nhập/ô chọn
        private static void CanNutTheoO(Krypton.Toolkit.KryptonButton b, Padding margin)
        {
            b.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            b.Margin = margin;
            b.Height = CaoO;
        }
        private static void KieuNut(Krypton.Toolkit.KryptonButton b,
            (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) m)
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
            // Trạng thái có FOCUS (sau khi bấm, nút giữ focus): nếu không đặt sẽ rơi về nền trắng của theme -> nút nhìn như trống.
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
            b.StateDisabled.Back.Color1 = Color.FromArgb(241, 245, 249); b.StateDisabled.Back.Color2 = Color.FromArgb(241, 245, 249);
            b.StateDisabled.Border.Color1 = Color.FromArgb(226, 232, 240); b.StateDisabled.Border.Color2 = Color.FromArgb(226, 232, 240);
            b.StateDisabled.Content.ShortText.Color1 = Color.FromArgb(148, 163, 184);
        }
        private static int Px(Control c, int v) => (int)Math.Round(v * c.DeviceDpi / 96.0);
        // radius = bán kính góc (px)
        private static GraphicsPath TaoDuongBoGoc(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
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
        /// <summary>Thẻ trắng bo góc có viền mảnh, tiêu đề tùy chọn có thanh nhấn tím (nền ngoài lấy màu của control cha).</summary>
        internal sealed class TheBoGocF29 : Panel
        {
            private string _tieuDe = "";
            public string TieuDe
            {
                get => _tieuDe;
                set { _tieuDe = value ?? ""; Invalidate(); }
            }
            public TheBoGocF29()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
                BackColor = Color.White;
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                if (Width < 4 || Height < 4) return;
                var g = e.Graphics;
                g.Clear(Parent?.BackColor ?? MauNen);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = TaoDuongBoGoc(new Rectangle(0, 0, Width - 1, Height - 1), Px(this, 10));
                using var b = new SolidBrush(Color.White);
                using var p = new Pen(MauVienThe);
                g.FillPath(b, path);
                g.DrawPath(p, path);
                if (_tieuDe.Length > 0)
                {
                    int x = Px(this, 14), w = Math.Max(3, Px(this, 3));
                    using (var bar = new SolidBrush(MauTim))
                        g.FillRectangle(bar, x, Px(this, 12), w, Px(this, 14));
                    TextRenderer.DrawText(g, _tieuDe, FontTieuDeThe, new Point(x + w + Px(this, 7), Px(this, 10)), MauChuDam,
                        TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.Left | TextFormatFlags.SingleLine);
                }
            }
        }
        /// <summary>Ô nhập bo tròn: viền xám, hover/focus viền tím. Hop là TextBox bên trong (không viền, có chữ gợi ý).
        /// Text đồng bộ hai chiều với KryptonTextBox gốc (đang ẩn) nên code xử lý tìm kiếm giữ nguyên.</summary>
        internal sealed class OTimF29 : Panel
        {
            public readonly TextBox Hop;
            private readonly Krypton.Toolkit.KryptonTextBox _kt;
            private bool _focus, _hover, _dongBo;
            public event EventHandler? NhanEnter;
            public OTimF29(Krypton.Toolkit.KryptonTextBox kt, string goiY)
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
                BackColor = Color.White;
                Size = new Size(260, CaoO);
                _kt = kt;
                Hop = new TextBox
                {
                    BorderStyle = BorderStyle.None,
                    Font = FontO,
                    BackColor = Color.White,
                    ForeColor = MauChuDam,
                    PlaceholderText = goiY,
                    Text = kt.Text
                };
                Hop.Enter += (s, e) => { _focus = true; Invalidate(); };
                Hop.Leave += (s, e) => { _focus = false; Invalidate(); };
                Hop.MouseEnter += (s, e) => { _hover = true; Invalidate(); };
                Hop.MouseLeave += (s, e) => { _hover = false; Invalidate(); };
                Hop.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; NhanEnter?.Invoke(this, EventArgs.Empty); }
                };
                Hop.TextChanged += (s, e) => DongBo(() => _kt.Text = Hop.Text);
                kt.TextChanged += KtThayDoi;
                Controls.Add(Hop);
                Click += (s, e) => Hop.Focus();
            }
            private void DongBo(Action a)
            {
                if (_dongBo) return;
                _dongBo = true;
                try { a(); } finally { _dongBo = false; }
            }
            private void KtThayDoi(object? s, EventArgs e) => DongBo(() => { if (Hop.Text != _kt.Text) Hop.Text = _kt.Text; });
            protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
                => base.SetBoundsCore(x, y, width, Px(this, CaoO), specified);
            protected override void OnLayout(LayoutEventArgs e)
            {
                base.OnLayout(e);
                if (Hop == null) return;
                int le = Px(this, 14);
                Hop.SetBounds(le, Math.Max(0, (Height - Hop.Height) / 2), Math.Max(10, Width - 2 * le), Hop.Height);
            }
            protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(Parent?.BackColor ?? Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = TaoDuongBoGoc(new Rectangle(0, 0, Width - 1, Height - 1), Px(this, 10));
                using var b = new SolidBrush(Color.White);
                using var p = new Pen(_focus ? MauTim : _hover ? MauTimVienNhat : MauVienO, _focus ? 1.5F : 1F);
                g.FillPath(b, path);
                g.DrawPath(p, path);
            }
            protected override void Dispose(bool disposing)
            {
                if (disposing) _kt.TextChanged -= KtThayDoi;
                base.Dispose(disposing);
            }
        }
        /// <summary>Ô chọn bo tròn thay ComboBox: viền xám, hover/mở viền tím, mũi tên tự vẽ, cao bằng nút.
        /// Danh sách sổ xuống là DanhSachF29 (cũng bo tròn). Dữ liệu lấy từ ComboBox gốc; chọn xong gán SelectedIndex của nó.</summary>
        internal sealed class OComboF29 : Control
        {
            private const TextFormatFlags FlagChu = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
            private readonly ComboBox _cb;
            private readonly EventHandler _dongSuKien;
            private DanhSachF29? _ds;
            private BoLocChuotF29? _boLoc;
            private Form? _formGoc;
            private bool _hover;
            private long _lanDong;
            public OComboF29(ComboBox cb)
            {
                _cb = cb;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
                TabStop = true;
                Cursor = Cursors.Hand;
                Font = FontO;
                BackColor = Color.White;
                Size = new Size(140, CaoO);
                _dongSuKien = (s, e) => DongDanhSach();
                cb.SelectedIndexChanged += CbThayDoi;
                cb.DataSourceChanged += CbThayDoi;
                cb.TextChanged += CbThayDoi;
            }
            private void CbThayDoi(object? s, EventArgs e) => Invalidate();
            protected override Size DefaultSize => new Size(140, CaoO);
            protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
                => base.SetBoundsCore(x, y, width, Px(this, CaoO), specified);
            // ----- Dữ liệu từ ComboBox gốc -----
            internal int Dem { get { try { return _cb.Items.Count; } catch { return 0; } } }
            internal int ChiSoHienTai { get { try { return _cb.SelectedIndex; } catch { return -1; } } }
            internal string Chu(int i) { try { return _cb.GetItemText(_cb.Items[i]) ?? string.Empty; } catch { return string.Empty; } }
            private string ChuHienTai()
            {
                int i = ChiSoHienTai;
                return i >= 0 && i < Dem ? Chu(i) : (_cb.Text ?? string.Empty);
            }
            // ----- Mở / đóng danh sách -----
            private void MoDanhSach()
            {
                if (_ds != null || !Enabled || IsDisposed) return;
                var goc = FindForm();
                int n = Dem;
                if (goc == null || n == 0) return;
                int hang = Px(this, 34), le = Px(this, 6), hien = Math.Min(n, 8), kc = Px(this, 4);
                int rong = Math.Max(Width, Px(this, 140)), cao = hien * hang + 2 * le;
                // Danh sách là CONTROL CON của form (không phải cửa sổ riêng): chuột/focus đi theo đúng đường của form nên không bị mất lựa chọn
                var diem = goc.PointToClient(PointToScreen(new Point(0, Height + kc)));
                if (diem.Y + cao > goc.ClientSize.Height)                  // không đủ chỗ phía dưới -> mở lên trên (nếu phía trên đủ chỗ)
                {
                    int tren = goc.PointToClient(PointToScreen(Point.Empty)).Y - cao - kc;
                    if (tren >= 0) diem.Y = tren;
                }
                if (diem.X + rong > goc.ClientSize.Width) diem.X = Math.Max(0, goc.ClientSize.Width - rong);
                var ds = new DanhSachF29(this, hang, le, hien) { Bounds = new Rectangle(diem, new Size(rong, cao)) };
                _ds = ds;
                _formGoc = goc;
                _boLoc = new BoLocChuotF29(this);
                Application.AddMessageFilter(_boLoc);
                goc.Resize += _dongSuKien;
                goc.Controls.Add(ds);
                ds.BringToFront();
                Invalidate();
            }
            internal void DongDanhSach()
            {
                var ds = _ds;
                if (ds == null) return;
                _ds = null;
                if (_boLoc != null) { Application.RemoveMessageFilter(_boLoc); _boLoc = null; }
                if (_formGoc != null)
                {
                    _formGoc.Resize -= _dongSuKien;
                    _formGoc = null;
                }
                _lanDong = Environment.TickCount64;
                try { ds.Visible = false; ds.Parent?.Controls.Remove(ds); } catch { }
                // Hủy hoãn lại: hàm này có thể đang chạy ngay trong sự kiện chuột của chính danh sách
                if (IsHandleCreated && !IsDisposed) { try { BeginInvoke(new Action(ds.Dispose)); } catch { ds.Dispose(); } }
                else ds.Dispose();
                if (!IsDisposed && !Disposing)
                {
                    _hover = ClientRectangle.Contains(PointToClient(Cursor.Position));
                    Invalidate();
                }
            }
            internal void Chon(int i)
            {
                if (i < 0 || i >= Dem) return;
                DongDanhSach();
                if (ChiSoHienTai != i)
                {
                    try { _cb.SelectedIndex = i; } catch { }
                    // Dự phòng: nếu ComboBox gốc không nhận chỉ số thì chọn theo phần tử
                    if (ChiSoHienTai != i) { try { _cb.SelectedItem = _cb.Items[i]; } catch { } }
                }
                Invalidate();
            }
            private void DoiChiSo(int delta)
            {
                int n = Dem;
                if (n == 0) return;
                int moi = Math.Clamp(ChiSoHienTai + delta, 0, n - 1);
                if (moi != ChiSoHienTai) _cb.SelectedIndex = moi;
            }
            // ----- Chuột / bàn phím -----
            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (e.Button != MouseButtons.Left) return;
                if (!Focused) Focus();
                if (_ds != null) DongDanhSach();
                else if (Environment.TickCount64 - _lanDong > 250) MoDanhSach();   // vừa bị bộ lọc chuột đóng khi bấm vào chính ô này -> không mở lại
            }
            protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnMouseWheel(MouseEventArgs e)
            {
                base.OnMouseWheel(e);
                _ds?.CuonBanh(e.Delta > 0 ? -1 : 1);
            }
            protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
            protected override void OnLostFocus(EventArgs e)
            {
                // Con trỏ đang ở trên danh sách (đang bấm chọn) thì không đóng: để MouseUp của danh sách kịp gán lựa chọn
                var ds = _ds;
                bool trenDanhSach = ds != null && !ds.IsDisposed && ds.ClientRectangle.Contains(ds.PointToClient(Cursor.Position));
                if (!trenDanhSach) DongDanhSach();
                Invalidate();
                base.OnLostFocus(e);
            }
            protected override void OnEnabledChanged(EventArgs e) { DongDanhSach(); Invalidate(); base.OnEnabledChanged(e); }
            protected override void OnVisibleChanged(EventArgs e) { if (!Visible) DongDanhSach(); base.OnVisibleChanged(e); }
            protected override bool IsInputKey(Keys keyData)
            {
                switch (keyData & Keys.KeyCode)
                {
                    case Keys.Up:
                    case Keys.Down:
                    case Keys.Home:
                    case Keys.End:
                    case Keys.Enter:
                    case Keys.Escape: return true;
                }
                return base.IsInputKey(keyData);
            }
            protected override void OnKeyDown(KeyEventArgs e)
            {
                base.OnKeyDown(e);
                var ds = _ds;
                switch (e.KeyCode)
                {
                    case Keys.Down:
                        if (ds != null) ds.DiChuyen(1); else if (e.Alt) MoDanhSach(); else DoiChiSo(1);
                        e.Handled = true; break;
                    case Keys.Up:
                        if (ds != null) ds.DiChuyen(-1); else DoiChiSo(-1);
                        e.Handled = true; break;
                    case Keys.Home:
                        if (ds != null) ds.DiChuyen(int.MinValue / 2); else DoiChiSo(int.MinValue / 2);
                        e.Handled = true; break;
                    case Keys.End:
                        if (ds != null) ds.DiChuyen(int.MaxValue / 2); else DoiChiSo(int.MaxValue / 2);
                        e.Handled = true; break;
                    case Keys.Enter:
                    case Keys.Space:
                        if (ds != null) ds.ChonDangSang(); else MoDanhSach();
                        e.Handled = true; break;
                    case Keys.Escape:
                        if (ds != null) { DongDanhSach(); e.Handled = true; }
                        break;
                }
            }
            // ----- Vẽ ô -----
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(Parent?.BackColor ?? Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                bool moRa = Focused || _ds != null;
                Color vien = !Enabled ? MauVienThe : moRa ? MauTim : _hover ? MauTimVienNhat : MauVienO;
                using (var path = TaoDuongBoGoc(new Rectangle(0, 0, Width - 1, Height - 1), Px(this, 10)))
                using (var b = new SolidBrush(Enabled ? Color.White : MauNen))
                using (var pen = new Pen(vien, moRa ? 1.5F : 1F))
                {
                    g.FillPath(b, path);
                    g.DrawPath(pen, path);
                }
                int le = Px(this, 14), vungTen = Px(this, 36);
                TextRenderer.DrawText(g, ChuHienTai(), Font, new Rectangle(le, 0, Math.Max(0, Width - le - vungTen), Height),
                    Enabled ? MauChuDam : MauChuMo, FlagChu);
                // Mũi tên (lật lên khi đang mở)
                int cx = Width - Px(this, 20), cy = Height / 2, dx = Px(this, 4), dy = Px(this, 2);
                if (_ds != null) dy = -dy;
                using var pc = new Pen(moRa ? MauTim : MauChuMo, 1.8F) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                g.DrawLines(pc, new[] { new Point(cx - dx, cy - dy), new Point(cx, cy + dy), new Point(cx + dx, cy - dy) });
            }
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    DongDanhSach();
                    _cb.SelectedIndexChanged -= CbThayDoi;
                    _cb.DataSourceChanged -= CbThayDoi;
                    _cb.TextChanged -= CbThayDoi;
                }
                base.Dispose(disposing);
            }
            /// <summary>Bấm chuột ở bất kỳ đâu ngoài danh sách thì đóng danh sách (danh sách không giữ focus nên cần bộ lọc này).</summary>
            private sealed class BoLocChuotF29 : IMessageFilter
            {
                private readonly OComboF29 _o;
                public BoLocChuotF29(OComboF29 o) { _o = o; }
                public bool PreFilterMessage(ref Message m)
                {
                    // WM_LBUTTONDOWN, WM_RBUTTONDOWN, WM_MBUTTONDOWN, WM_NCLBUTTONDOWN, WM_NCRBUTTONDOWN
                    if (m.Msg == 0x201 || m.Msg == 0x204 || m.Msg == 0x207 || m.Msg == 0xA1 || m.Msg == 0xA4)
                    {
                        var c = Control.FromHandle(m.HWnd);
                        if (c == null || !ReferenceEquals(c, _o._ds)) _o.DongDanhSach();
                    }
                    return false;
                }
            }
        }
        /// <summary>Khung danh sách sổ xuống: control con nổi trên form (không chiếm focus), bo tròn góc (Region + viền vẽ khử răng cưa),
        /// dòng đang trỏ tô tím nhạt, dòng đang chọn có dấu ✓, cuộn bằng con lăn/phím, có thanh cuộn mảnh khi nhiều mục.</summary>
        internal sealed class DanhSachF29 : Control
        {
            private const TextFormatFlags FlagChu = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
            private readonly OComboF29 _chu;
            private readonly int _hang, _le, _hien;
            private readonly float _k;
            private int _dau, _sang;
            public DanhSachF29(OComboF29 chu, int hang, int le, int hien)
            {
                _chu = chu; _hang = hang; _le = le; _hien = hien;
                _k = chu.DeviceDpi / 96f;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                SetStyle(ControlStyles.Selectable, false);   // bấm vào danh sách KHÔNG được cướp focus của ô chọn (mất focus = ô chọn tự đóng danh sách trước khi nhận lựa chọn)
                TabStop = false;
                BackColor = Color.White;
                Font = FontO;
                Cursor = Cursors.Hand;
                _sang = chu.ChiSoHienTai;
                _dau = Math.Max(0, Math.Min(chu.Dem - _hien, _sang - _hien / 2));
            }
            private int S(int v) => (int)Math.Round(v * _k);
            protected override void OnSizeChanged(EventArgs e)
            {
                base.OnSizeChanged(e);
                if (Width < 4 || Height < 4) return;
                using var path = TaoDuongBoGoc(new Rectangle(0, 0, Width, Height), S(10));
                var cu = Region;
                Region = new Region(path);
                cu?.Dispose();
            }
            // ----- điều khiển từ ô chọn -----
            internal void CuonBanh(int delta)
            {
                int max = Math.Max(0, _chu.Dem - _hien);
                int moi = Math.Clamp(_dau + delta, 0, max);
                if (moi == _dau) return;
                _dau = moi;
                Invalidate();
            }
            internal void DiChuyen(int delta)
            {
                int n = _chu.Dem;
                if (n == 0) return;
                int goc = _sang >= 0 ? _sang : _chu.ChiSoHienTai;
                long moi = (long)goc + delta;
                _sang = (int)Math.Clamp(moi, 0L, (long)(n - 1));
                if (_sang < _dau) _dau = _sang;
                else if (_sang >= _dau + _hien) _dau = _sang - _hien + 1;
                Invalidate();
            }
            internal void ChonDangSang()
            {
                if (_sang >= 0) _chu.Chon(_sang);
            }
            // ----- chuột -----
            private int ViTri(Point p)
            {
                if (p.Y < _le || p.Y >= _le + _hien * _hang) return -1;
                int i = _dau + (p.Y - _le) / _hang;
                return i < _chu.Dem ? i : -1;
            }
            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                int i = ViTri(e.Location);
                if (i >= 0 && i != _sang) { _sang = i; Invalidate(); }
            }
            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                if (e.Button != MouseButtons.Left) return;
                int i = ViTri(e.Location);
                if (i >= 0) _chu.Chon(i);
            }
            protected override void OnMouseWheel(MouseEventArgs e)
            {
                base.OnMouseWheel(e);
                CuonBanh(e.Delta > 0 ? -1 : 1);
            }
            // ----- vẽ -----
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = TaoDuongBoGoc(new Rectangle(0, 0, Width - 1, Height - 1), S(10)))
                using (var vien = new Pen(MauVienThe))
                    g.DrawPath(vien, path);
                int n = _chu.Dem;
                int cuoi = Math.Min(n, _dau + _hien);
                bool coThanh = n > _hien;
                int rong = Width - 2 * _le - (coThanh ? S(8) : 0);
                int hienTai = _chu.ChiSoHienTai;
                for (int i = _dau; i < cuoi; i++)
                {
                    var rc = new Rectangle(_le, _le + (i - _dau) * _hang, rong, _hang);
                    bool laHienTai = i == hienTai, laSang = i == _sang;
                    if (laHienTai || laSang)
                    {
                        using var pr = TaoDuongBoGoc(new Rectangle(rc.X, rc.Y + 1, rc.Width, rc.Height - 2), S(6));
                        using var br = new SolidBrush(laSang ? MauTimNenHover : MauTimNen);
                        g.FillPath(br, pr);
                    }
                    var rt = new Rectangle(rc.X + S(12), rc.Y, Math.Max(0, rc.Width - S(12) - S(30)), rc.Height);
                    TextRenderer.DrawText(g, _chu.Chu(i), Font, rt, laHienTai || laSang ? MauTimChu : MauChuDam, FlagChu);
                    if (laHienTai)
                    {
                        int cx = rc.Right - S(18), cy = rc.Y + rc.Height / 2;
                        using var pc = new Pen(MauTimChu, 1.8F) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                        g.DrawLines(pc, new[] { new Point(cx - S(5), cy), new Point(cx - S(1), cy + S(4)), new Point(cx + S(5), cy - S(4)) });
                    }
                }
                if (coThanh)
                {
                    int htrack = Height - 2 * _le;
                    int hthumb = Math.Max(S(24), htrack * _hien / n);
                    int ythumb = _le + (htrack - hthumb) * _dau / Math.Max(1, n - _hien);
                    var rt = new Rectangle(Width - _le - S(6), ythumb, S(4), hthumb);
                    using var pt = TaoDuongBoGoc(rt, S(2));
                    using var bt = new SolidBrush(Color.FromArgb(203, 213, 225));
                    g.FillPath(bt, pt);
                }
            }
        }
    }
}