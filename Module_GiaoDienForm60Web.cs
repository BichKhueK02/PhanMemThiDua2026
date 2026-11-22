using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;
///Phiên bản 1
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CỦA Form60_NhapLieuCBCS — CHỈ TRÌNH BÀY, KHÔNG CHỨA LOGIC NGHIỆP VỤ
    //  Cùng cách làm với Module_GiaoDienForm6Web: lấy control theo TÊN bằng reflection nên không phải sửa Designer.
    //  Cách dùng: trong constructor Form60_NhapLieuCBCS, ngay sau DinhDangDataGridViewHienDai();
    //        Module_GiaoDienForm60Web.ApDung(this);
    //  Cần còn trong project: Module_GiaoDienForm4Web.cs (OComboWebF4).
    //  Lỗi trình bày không bao giờ làm form không mở được (bọc try/catch).
    // =====================================================================================
    internal sealed class Module_GiaoDienForm60Web
    {
        private readonly Form _f;
        private readonly ComboBox comboBox_ChucVu, comboBox_PhanLoai, comboBox_TimKiemDonVi, comboBox_DonVi, textBox_CapBac;
        private readonly ContextMenuStrip contextMenuStrip1;
        private readonly GroupBox groupBox1, groupBox1_TimKiemThongTinCBCS, groupBox2_KhoiLenhDieuKhien, groupBox3_ThongTinCBCS;
        private readonly Krypton.Toolkit.KryptonButton btnThemMoi, kryptonButton1_DiChuyenDataCBCSLen, kryptonButton1_DiChuyenDataCBCSXuong,
                                                       kryptonButton1_DongForm, kryptonButton_LamMoiCacOTimKiem, kryptonButton_RefershCSDL;
        private readonly DataGridView kryptonDataGridView1;   // thực tế là LuoiDuLieuNhe : DataGridView (không phải Krypton)
        private readonly Label label3, label4, label7, label8, label9, label10, label11, label12, label13, label14, label15, label16;
        private readonly PictureBox pictureBox1;
        private readonly StatusStrip statusStrip1;
        private readonly TableLayoutPanel tableLayoutPanel1, tableLayoutPanel2, tableLayoutPanel3, tableLayoutPanel4, tableLayoutPanel5,
                                          tableLayoutPanel6, tableLayoutPanel7, tableLayoutPanel8, tableLayoutPanel9;
        private readonly Krypton.Toolkit.KryptonTextBox textBox_GhiChu, textBox_HoVaTen, textBox_NamSinh, textBox_NgayVaoCAND,
                                                        textBox_QueQuan, textBox_SoHieu, textBox_TimKiemTheoTen;
        private readonly ToolStripStatusLabel toolStripStatusLabel1;
        public static string? LoiGanGanNhat;
        public static void ApDung(Form f)
        {
            try { new Module_GiaoDienForm60Web(f).Chay(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Module_GiaoDienForm60Web] " + ex);
                LoiGanGanNhat = ex.ToString();   // xem bằng debugger nếu giao diện không áp dụng
            }
        }
        private Module_GiaoDienForm60Web(Form f)
        {
            _f = f;
            comboBox_ChucVu = Lay<ComboBox>("comboBox_ChucVu");
            comboBox_PhanLoai = Lay<ComboBox>("comboBox_PhanLoai");
            comboBox_TimKiemDonVi = Lay<ComboBox>("comboBox_TimKiemDonVi");
            comboBox_DonVi = Lay<ComboBox>("comboBox_DonVi");
            textBox_CapBac = Lay<ComboBox>("textBox_CapBac");
            contextMenuStrip1 = Lay<ContextMenuStrip>("contextMenuStrip1");
            groupBox1 = Lay<GroupBox>("groupBox1");
            groupBox1_TimKiemThongTinCBCS = Lay<GroupBox>("groupBox1_TimKiemThongTinCBCS");
            groupBox2_KhoiLenhDieuKhien = Lay<GroupBox>("groupBox2_KhoiLenhDieuKhien");
            groupBox3_ThongTinCBCS = Lay<GroupBox>("groupBox3_ThongTinCBCS");
            btnThemMoi = Lay<Krypton.Toolkit.KryptonButton>("btnThemMoi");
            kryptonButton1_DiChuyenDataCBCSLen = Lay<Krypton.Toolkit.KryptonButton>("kryptonButton1_DiChuyenDataCBCSLen");
            kryptonButton1_DiChuyenDataCBCSXuong = Lay<Krypton.Toolkit.KryptonButton>("kryptonButton1_DiChuyenDataCBCSXuong");
            kryptonButton1_DongForm = Lay<Krypton.Toolkit.KryptonButton>("kryptonButton1_DongForm");
            kryptonButton_LamMoiCacOTimKiem = Lay<Krypton.Toolkit.KryptonButton>("kryptonButton_LamMoiCacOTimKiem");
            kryptonButton_RefershCSDL = Lay<Krypton.Toolkit.KryptonButton>("kryptonButton_RefershCSDL");
            kryptonDataGridView1 = Lay<DataGridView>("kryptonDataGridView1");
            label3 = Lay<Label>("label3"); label4 = Lay<Label>("label4"); label7 = Lay<Label>("label7"); label8 = Lay<Label>("label8");
            label9 = Lay<Label>("label9"); label10 = Lay<Label>("label10"); label11 = Lay<Label>("label11"); label12 = Lay<Label>("label12");
            label13 = Lay<Label>("label13"); label14 = Lay<Label>("label14"); label15 = Lay<Label>("label15"); label16 = Lay<Label>("label16");
            pictureBox1 = Lay<PictureBox>("pictureBox1");
            statusStrip1 = Lay<StatusStrip>("statusStrip1");
            tableLayoutPanel1 = Lay<TableLayoutPanel>("tableLayoutPanel1"); tableLayoutPanel2 = Lay<TableLayoutPanel>("tableLayoutPanel2");
            tableLayoutPanel3 = Lay<TableLayoutPanel>("tableLayoutPanel3"); tableLayoutPanel4 = Lay<TableLayoutPanel>("tableLayoutPanel4");
            tableLayoutPanel5 = Lay<TableLayoutPanel>("tableLayoutPanel5"); tableLayoutPanel6 = Lay<TableLayoutPanel>("tableLayoutPanel6");
            tableLayoutPanel7 = Lay<TableLayoutPanel>("tableLayoutPanel7"); tableLayoutPanel8 = Lay<TableLayoutPanel>("tableLayoutPanel8");
            tableLayoutPanel9 = Lay<TableLayoutPanel>("tableLayoutPanel9");
            textBox_GhiChu = Lay<Krypton.Toolkit.KryptonTextBox>("textBox_GhiChu");
            textBox_HoVaTen = Lay<Krypton.Toolkit.KryptonTextBox>("textBox_HoVaTen");
            textBox_NamSinh = Lay<Krypton.Toolkit.KryptonTextBox>("textBox_NamSinh");
            textBox_NgayVaoCAND = Lay<Krypton.Toolkit.KryptonTextBox>("textBox_NgayVaoCAND");
            textBox_QueQuan = Lay<Krypton.Toolkit.KryptonTextBox>("textBox_QueQuan");
            textBox_SoHieu = Lay<Krypton.Toolkit.KryptonTextBox>("textBox_SoHieu");
            textBox_TimKiemTheoTen = Lay<Krypton.Toolkit.KryptonTextBox>("textBox_TimKiemTheoTen");
            toolStripStatusLabel1 = Lay<ToolStripStatusLabel>("toolStripStatusLabel1");
        }
        private T Lay<T>(string ten) where T : class
        {
            var fi = _f.GetType().GetField(ten, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                     ?? throw new MissingFieldException(_f.GetType().Name, ten);
            return (T)fi.GetValue(_f)!;
        }
        // ---------- BẢNG MÀU (giống Form6) ----------
        private static readonly Color MauNenTrang = Color.FromArgb(244, 246, 250);
        private static readonly Color MauChuDam = Color.FromArgb(30, 41, 59);
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);
        private static readonly Color MauChuPhu = Color.FromArgb(100, 116, 139);
        private static readonly Color MauChinh = Color.FromArgb(37, 99, 235);
        private static readonly Font FontTieuDeThe = new Font("Segoe UI Semibold", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNhan = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontTrangThai = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontMenu = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutXanhLa =
            (Color.FromArgb(22, 163, 74), Color.FromArgb(21, 128, 61), Color.FromArgb(22, 101, 52), Color.FromArgb(22, 163, 74), Color.White);
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutVien =
            (Color.White, Color.FromArgb(248, 250, 252), Color.FromArgb(241, 245, 249), Color.FromArgb(203, 213, 225), Color.FromArgb(51, 65, 85));
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutNguyHiem =
            (Color.FromArgb(254, 242, 242), Color.FromArgb(254, 226, 226), Color.FromArgb(252, 205, 205), Color.FromArgb(252, 165, 165), Color.FromArgb(185, 28, 28));
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutXanhNhat =
            (Color.FromArgb(239, 246, 255), Color.FromArgb(219, 234, 254), Color.FromArgb(191, 219, 254), Color.FromArgb(191, 219, 254), Color.FromArgb(29, 78, 216));
        // =====================================================================================
        private void Chay()
        {
            _f.SuspendLayout();
            try
            {
                _f.BackColor = MauNenTrang;
                DinhBoCucTong();
                BocTheTieuDe(groupBox1_TimKiemThongTinCBCS, tableLayoutPanel5, new Padding(0, 0, 10, 0));
                BocTheTieuDe(groupBox2_KhoiLenhDieuKhien, tableLayoutPanel6, new Padding(0));
                BocTheTieuDe(groupBox3_ThongTinCBCS, tableLayoutPanel3, new Padding(0, 8, 0, 0));
                BocTheTieuDe(groupBox1, kryptonDataGridView1, new Padding(0, 8, 0, 0), new Padding(12, 34, 12, 12));
                DinhKhoiTimKiem();
                DinhKhoiLenh();
                DinhKhoiNhap();
                DinhComboBoTron();
                DinhLuoi();
                DinhThanhTrangThai();
                contextMenuStrip1.Font = FontMenu;
                contextMenuStrip1.Renderer = new ToolStripProfessionalRenderer(new BangMauMenuF60()) { RoundedEdges = false };
            }
            finally
            {
                _f.ResumeLayout(true);
            }
            _f.SizeChanged += (s, e) => CanGiuaNoiDung();
            CanGiuaNoiDung();
        }
        // ---------- Bố cục tổng ----------
        private void DinhBoCucTong()
        {
            tableLayoutPanel1.BackColor = MauNenTrang;
            tableLayoutPanel1.Padding = new Padding(12, 10, 12, 8);
            tableLayoutPanel1.RowStyles.Clear();
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));  // tìm kiếm + lệnh
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 228F));  // thẻ nhập liệu (8 + 34 tiêu đề + 3 hàng + hàng nút)
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));   // lưới ăn hết phần còn lại
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));   // thanh trạng thái
            tableLayoutPanel2.Margin = new Padding(0);
            tableLayoutPanel2.ColumnStyles.Clear();
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
            pictureBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.Margin = new Padding(0, 8, 14, 8);
        }
        // ---------- Thẻ trắng bo góc thay GroupBox (GroupBox giữ lại dạng ẩn để code gốc vẫn gán .Text) ----------
        private static void BocTheTieuDe(GroupBox gb, Control noiDung, Padding margin, Padding? padding = null)
        {
            var cha = gb.Parent;
            if (cha == null) return;
            var the = new TheBoGocF60
            {
                Dock = DockStyle.Fill,
                Margin = margin,
                ForeColor = MauChuDam,
                TieuDe = gb.Text,
                Padding = padding ?? new Padding(12, 34, 12, 8)
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
                the.Controls.Add(noiDung);
                noiDung.Dock = DockStyle.Fill;
                the.Controls.Add(gb);
                gb.TextChanged += (s, e) => the.TieuDe = gb.Text;
            }
            finally
            {
                cha.ResumeLayout(true);
            }
        }
        private const int RongToiDa = 1440;
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
        private void DinhKhoiTimKiem()
        {
            tableLayoutPanel5.Margin = new Padding(0);
            tableLayoutPanel5.ColumnStyles.Clear();
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50F));
            DinhNhan(label3, true); DinhNhan(label4);
            DinhODauVao(textBox_TimKiemTheoTen); DinhODauVao(comboBox_TimKiemDonVi);
            KieuNut(kryptonButton_LamMoiCacOTimKiem, NutVien);
            CanNutTheoO(kryptonButton_LamMoiCacOTimKiem, new Padding(8, 0, 0, 0));
        }
        // ---------- Thẻ 2: Khối lệnh ----------
        private void DinhKhoiLenh()
        {
            tableLayoutPanel6.Margin = new Padding(0);
            tableLayoutPanel6.ColumnStyles.Clear();
            tableLayoutPanel6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            KieuNut(kryptonButton_RefershCSDL, NutXanhNhat);
            KieuNut(kryptonButton1_DongForm, NutNguyHiem);
            CanNutTheoO(kryptonButton_RefershCSDL, new Padding(0, 0, 8, 0));
            CanNutTheoO(kryptonButton1_DongForm, new Padding(0));
        }
        // ---------- Thẻ 3: Form nhập (3 hàng ô + 1 hàng nút) ----------
        private void DinhKhoiNhap()
        {
            foreach (var t in new[] { tableLayoutPanel3, tableLayoutPanel4, tableLayoutPanel7, tableLayoutPanel8, tableLayoutPanel9 })
                t.Margin = new Padding(0);
            tableLayoutPanel9.ForeColor = MauChuDam;   // (trước đây xanh đậm 0,0,192)
            tableLayoutPanel3.RowStyles.Clear();
            for (int i = 0; i < 3; i++) tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            // Cột nhãn đầu mỗi hàng rộng bằng nhau -> các ô nhập thẳng hàng giữa 3 hàng
            DatCot(tableLayoutPanel8, (86, true), (30, false), (0, false), (14, false), (0, false), (12, false), (0, false), (44, false));
            DatCot(tableLayoutPanel4, (86, true), (20, false), (0, false), (16, false), (0, false), (32, false), (0, false), (32, false));
            DatCot(tableLayoutPanel9, (86, true), (22, false), (0, false), (78, false));
            // Hàng 1
            DinhNhan(label7, true); DinhNhan(label8); DinhNhan(label9); DinhNhan(label10);
            // Hàng 2
            DinhNhan(label12, true); DinhNhan(label13); DinhNhan(label14); DinhNhan(label11);
            // Hàng 3
            DinhNhan(label16, true); DinhNhan(label15);
            foreach (Control c in new Control[] { textBox_HoVaTen, textBox_SoHieu, textBox_NamSinh, textBox_QueQuan, textBox_NgayVaoCAND,
                                                  textBox_CapBac, comboBox_ChucVu, comboBox_DonVi, comboBox_PhanLoai, textBox_GhiChu })
                DinhODauVao(c);
            foreach (var t in new[] { textBox_HoVaTen, textBox_SoHieu, textBox_NamSinh, textBox_QueQuan, textBox_NgayVaoCAND, textBox_GhiChu, textBox_TimKiemTheoTen })
                t.StateCommon.Border.Rounding = 6F;
            // Hàng nút: [ trống ........ ][Lên][Xuống][Thêm mới]
            tableLayoutPanel7.ColumnStyles.Clear();
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 184F));
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 184F));
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 184F));
            KieuNut(kryptonButton1_DiChuyenDataCBCSLen, NutVien);
            KieuNut(kryptonButton1_DiChuyenDataCBCSXuong, NutVien);
            KieuNut(btnThemMoi, NutXanhLa);
            CanNutTheoO(kryptonButton1_DiChuyenDataCBCSLen, new Padding(0, 0, 8, 0));
            CanNutTheoO(kryptonButton1_DiChuyenDataCBCSXuong, new Padding(0, 0, 8, 0));
            CanNutTheoO(btnThemMoi, new Padding(0));
        }
        // (độ rộng, tuyệt đối?) — 0 = tự co theo chữ
        private static void DatCot(TableLayoutPanel t, params (int Rong, bool TuyetDoi)[] cot)
        {
            t.ColumnStyles.Clear();
            foreach (var c in cot)
            {
                if (c.Rong == 0) t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                else t.ColumnStyles.Add(c.TuyetDoi ? new ColumnStyle(SizeType.Absolute, c.Rong) : new ColumnStyle(SizeType.Percent, c.Rong));
            }
        }
        private void DinhComboBoTron()
        {
            foreach (var cb in new[] { comboBox_TimKiemDonVi, textBox_CapBac, comboBox_ChucVu, comboBox_DonVi, comboBox_PhanLoai })
                if (cb != null && cb.Parent != null) OComboWebF4.Gan(cb);
        }
        // ---------- Lưới nằm trong thẻ (đã bọc ở Chay) — DataGridView thường nên chỉnh bằng CellStyle ----------
        private void DinhLuoi()
        {
            var dgv = kryptonDataGridView1;
            dgv.SuspendLayout();
            try
            {
                dgv.EnableHeadersVisualStyles = false;
                dgv.BorderStyle = BorderStyle.None;
                dgv.BackgroundColor = Color.White;
                dgv.GridColor = Color.FromArgb(241, 245, 249);
                dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
                dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
                dgv.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
                var head = dgv.ColumnHeadersDefaultCellStyle;
                head.BackColor = Color.FromArgb(237, 233, 254);
                head.ForeColor = Color.FromArgb(91, 33, 182);
                head.SelectionBackColor = head.BackColor;
                head.SelectionForeColor = head.ForeColor;
                var rowHead = dgv.RowHeadersDefaultCellStyle;
                rowHead.BackColor = Color.FromArgb(250, 248, 255);
                rowHead.ForeColor = MauChuPhu;
                rowHead.SelectionBackColor = rowHead.BackColor;
                rowHead.SelectionForeColor = rowHead.ForeColor;
                var cell = dgv.DefaultCellStyle;
                cell.BackColor = Color.White;
                cell.ForeColor = Color.FromArgb(51, 65, 85);
                cell.SelectionBackColor = Color.FromArgb(239, 246, 255);
                cell.SelectionForeColor = Color.FromArgb(29, 78, 216);
                dgv.CellPainting -= VeOGocSTT;
                dgv.CellPainting += VeOGocSTT;
            }
            finally
            {
                dgv.ResumeLayout();
            }
        }
        private void VeOGocSTT(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex != -1 || e.ColumnIndex != -1 || sender is not DataGridView dgv) return;
            var o = e.CellBounds;
            using (var nen = new SolidBrush(Color.FromArgb(237, 233, 254))) e.Graphics!.FillRectangle(nen, o);
            using (var vien = new Pen(Color.FromArgb(221, 214, 254)))
            {
                e.Graphics.DrawLine(vien, o.Left, o.Bottom - 1, o.Right - 1, o.Bottom - 1);
                e.Graphics.DrawLine(vien, o.Right - 1, o.Top, o.Right - 1, o.Bottom - 1);
            }
            var font = dgv.ColumnHeadersDefaultCellStyle.Font ?? dgv.Font;
            TextRenderer.DrawText(e.Graphics, "STT", font, o, Color.FromArgb(91, 33, 182),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            e.Handled = true;
        }
        private void DinhThanhTrangThai()
        {
            statusStrip1.BackColor = Color.White;
            statusStrip1.ForeColor = MauChuNhan;
            statusStrip1.SizingGrip = false;
            statusStrip1.Margin = new Padding(0);
            statusStrip1.Padding = new Padding(10, 0, 10, 0);
            toolStripStatusLabel1.Font = FontTrangThai;
        }
        // =====================================================================================
        //  TIỆN ÍCH TRÌNH BÀY
        // =====================================================================================
        private static void DinhNhan(Label lb, bool dauHang = false)
        {
            lb.Font = FontNhan;
            lb.ForeColor = MauChuNhan;
            lb.Anchor = AnchorStyles.Left;
            lb.Margin = new Padding(dauHang ? 0 : 10, 0, 6, 0);
        }
        private static void DinhODauVao(Control c)
        {
            c.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            c.Margin = new Padding(0, 3, 0, 3);
        }
        private static void CanNutTheoO(Krypton.Toolkit.KryptonButton b, Padding margin)
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
            // Trạng thái có FOCUS (sau khi bấm): nếu không đặt sẽ rơi về nền trắng của theme
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
        /// <summary>Thẻ trắng bo góc có viền mảnh + tiêu đề có thanh nhấn xanh.</summary>
        internal sealed class TheBoGocF60 : Panel
        {
            private string _tieuDe = "";
            public string TieuDe { get => _tieuDe; set { _tieuDe = value ?? ""; Invalidate(); } }
            public TheBoGocF60()
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
                int d = (int)(20 * DeviceDpi / 96f);
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
                    using (var bar = new SolidBrush(MauChinh)) g.FillRectangle(bar, x, (int)(12 * k), w, (int)(14 * k));
                    TextRenderer.DrawText(g, _tieuDe, FontTieuDeThe, new Point(x + w + (int)(7 * k), (int)(10 * k)), MauChuDam,
                        TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.Left | TextFormatFlags.SingleLine);
                }
            }
        }
        internal sealed class BangMauMenuF60 : ProfessionalColorTable
        {
            public BangMauMenuF60() { UseSystemColors = false; }
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
    // =====================================================================================
    //  CHUYỂN TRANG MƯỢT: tắt vẽ của panel chứa trong lúc ẩn/hiện -> màn hình đổi đúng 1 khung hình,
    //  không còn cảnh 2 form chồng nhau, nền trống hay từng control hiện dần.
    //  Dùng chung cho Form6 <-> Form60 (và các form con khác trong PanelContainer).
    // =====================================================================================
    internal static class Module_ChuyenTrangMuot
    {
        private const int WM_SETREDRAW = 0x000B;
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        /// <summary>Hiện <paramref name="hien"/> (Dock Fill) và ẩn các control còn lại của panel trong 1 lượt vẽ.</summary>
        public static void Chuyen(Control panel, Control hien, Action? truocKhiHien = null)
        {
            bool khoaVe = panel.IsHandleCreated && panel.Visible;
            if (khoaVe) SendMessage(panel.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            panel.SuspendLayout();
            try
            {
                truocKhiHien?.Invoke();                 // vd: dựng sẵn bố cục form đích khi nó còn ẩn
                hien.Dock = DockStyle.Fill;
                for (int i = panel.Controls.Count - 1; i >= 0; i--)
                {
                    Control c = panel.Controls[i];
                    if (c != hien && c.Visible) c.Visible = false;
                }
                hien.Visible = true;
                hien.BringToFront();
            }
            finally
            {
                panel.ResumeLayout(true);
                if (khoaVe)
                {
                    SendMessage(panel.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
                    panel.Invalidate(true);
                    panel.Update();                     // vẽ ngay 1 lần, không đợi vòng lặp thông điệp
                }
            }
        }
    }
}