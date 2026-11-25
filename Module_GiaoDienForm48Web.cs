using System.Drawing.Drawing2D;
using System.Reflection;
///Phiên bản 1
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CỦA Form48_XuatTepPdf  —  CHỈ TRÌNH BÀY, KHÔNG CHỨA LOGIC NGHIỆP VỤ
    //  DẠNG MODULE ĐỘC LẬP (không partial) => Visual Studio không sinh file .resx đi kèm.
    //
    //  Cách dùng: cuối constructor Form48_XuatTepPdf có sẵn dòng
    //        Module_GiaoDienForm48Web.ApDung(this);
    //  - Control của form được lấy theo TÊN bằng reflection → không phải sửa Designer / tên / sự kiện.
    //  - Màu chữ trạng thái của 2 nhãn đường dẫn (đỏ = chưa chọn, xanh = đã chọn) và của "Chọn tất cả" do code form điều khiển, giữ nguyên.
    //  - Lỗi trình bày không bao giờ làm form không mở được (bọc try/catch).
    //  - Mọi kích thước (px) viết theo đơn vị thiết kế 96 DPI, quy đổi bằng LogicalToDeviceUnits.
    // =====================================================================================
    internal sealed class Module_GiaoDienForm48Web
    {
        private readonly Form _f;
        private readonly TableLayoutPanel tableLayoutPanel1, tableLayoutPanel2, tableLayoutPanel3, tableLayoutPanel4;
        private readonly Krypton.Toolkit.KryptonButton kryptonButton_XuatTepPdf, kryptonButton1_ChonDuongDanLuuTepPdf, kryptonButton1_ChonDuongDanTepExcel;
        private readonly CheckBox checkBox1_ChonTatCa;
        private readonly PictureBox pictureBox1, pictureBox2;
        private readonly Label label2, label3, label_DuongDanExcel, label_DuongDanPdf;
        private readonly StatusStrip statusStrip1;
        private readonly ToolStripProgressBar toolStripProgressBar1_TienTrinhXuatTep;
        private readonly ToolStripStatusLabel toolStripStatusLabel1_TongCongTepPdf, toolStripStatusLabel1_DangLoad, toolStripStatusLabel1_GioiThieu;
        private readonly GroupBox groupBox1;
        private readonly CheckedListBox checkedListBox1_LietKeTenCacSheet;
        public static void ApDung(Form f)
        {
            try { new Module_GiaoDienForm48Web(f).Chay(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[Module_GiaoDienForm48Web] " + ex.Message); }
        }
        private Module_GiaoDienForm48Web(Form f)
        {
            _f = f;
            tableLayoutPanel1 = Lay<TableLayoutPanel>("tableLayoutPanel1");
            tableLayoutPanel2 = Lay<TableLayoutPanel>("tableLayoutPanel2");
            tableLayoutPanel3 = Lay<TableLayoutPanel>("tableLayoutPanel3");
            tableLayoutPanel4 = Lay<TableLayoutPanel>("tableLayoutPanel4");
            kryptonButton_XuatTepPdf = Lay<Krypton.Toolkit.KryptonButton>("kryptonButton_XuatTepPdf");
            kryptonButton1_ChonDuongDanLuuTepPdf = Lay<Krypton.Toolkit.KryptonButton>("kryptonButton1_ChonDuongDanLuuTepPdf");
            kryptonButton1_ChonDuongDanTepExcel = Lay<Krypton.Toolkit.KryptonButton>("kryptonButton1_ChonDuongDanTepExcel");
            checkBox1_ChonTatCa = Lay<CheckBox>("checkBox1_ChonTatCa");
            pictureBox1 = Lay<PictureBox>("pictureBox1");
            pictureBox2 = Lay<PictureBox>("pictureBox2");
            label2 = Lay<Label>("label2");
            label3 = Lay<Label>("label3");
            label_DuongDanExcel = Lay<Label>("label_DuongDanExcel");
            label_DuongDanPdf = Lay<Label>("label_DuongDanPdf");
            statusStrip1 = Lay<StatusStrip>("statusStrip1");
            toolStripProgressBar1_TienTrinhXuatTep = Lay<ToolStripProgressBar>("toolStripProgressBar1_TienTrinhXuatTep");
            toolStripStatusLabel1_TongCongTepPdf = Lay<ToolStripStatusLabel>("toolStripStatusLabel1_TongCongTepPdf");
            toolStripStatusLabel1_DangLoad = Lay<ToolStripStatusLabel>("toolStripStatusLabel1_DangLoad");
            toolStripStatusLabel1_GioiThieu = Lay<ToolStripStatusLabel>("toolStripStatusLabel1_GioiThieu");
            groupBox1 = Lay<GroupBox>("groupBox1");
            checkedListBox1_LietKeTenCacSheet = Lay<CheckedListBox>("checkedListBox1_LietKeTenCacSheet");
        }
        private T Lay<T>(string ten) where T : class
        {
            var fi = _f.GetType().GetField(ten, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                     ?? throw new MissingFieldException(_f.GetType().Name, ten);
            return (T)fi.GetValue(_f)!;
        }
        // ---------- Bảng màu / font ----------
        private static readonly Color MauNenTrang = Color.FromArgb(244, 246, 250);
        private static readonly Color MauVienThe = Color.FromArgb(226, 232, 240);
        private static readonly Color MauChuDam = Color.FromArgb(30, 41, 59);
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);
        private static readonly Color MauChinh = Color.FromArgb(37, 99, 235);
        private static readonly Color MauODuongDan = Color.FromArgb(248, 250, 252);
        private static readonly Font FontTieuDeThe = new Font(Module_HeThong.TenFontHeThongDam, 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNhanDam = new Font(Module_HeThong.TenFontHeThongDam, 10F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontDuongDan = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontDanhSach = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new Font(Module_HeThong.TenFontHeThongDam, 10.5F, FontStyle.Bold, GraphicsUnit.Point);
        private static readonly Font FontTrangThai = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutVien =
            (Color.White, Color.FromArgb(248, 250, 252), Color.FromArgb(241, 245, 249), Color.FromArgb(203, 213, 225), Color.FromArgb(51, 65, 85));
        private int Px(int v) => _f.LogicalToDeviceUnits(v);
        // =====================================================================================
        //  ĐIỂM VÀO
        // =====================================================================================
        private void Chay()
        {
            _f.SuspendLayout();
            try
            {
                _f.BackColor = MauNenTrang;
                DinhBoCucTong();
                DinhTheDuongDan(tableLayoutPanel4, pictureBox1, label2, kryptonButton1_ChonDuongDanTepExcel, label_DuongDanExcel, new Padding(16, 14, 16, 4));
                DinhTheDuongDan(tableLayoutPanel2, pictureBox2, label3, kryptonButton1_ChonDuongDanLuuTepPdf, label_DuongDanPdf, new Padding(16, 4, 16, 4));
                DinhTheDanhSach();
                DinhThanhHanhDong();
                DinhThanhTrangThai();
            }
            finally { _f.ResumeLayout(true); }
            // Form48_Load đặt lại kích thước thanh tiến trình → áp dụng lại SAU Load (đăng ký sau nên chạy sau)
            _f.Load += (_, _) => HoanThienSauLoad();
        }
        private void DinhBoCucTong()
        {
            var t = tableLayoutPanel1;
            t.BackColor = MauNenTrang;
            t.Padding = Padding.Empty;
            t.RowStyles.Clear();
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, Px(74)));    // thẻ chọn Excel (14 + 56 + 4)
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, Px(64)));    // thẻ chọn thư mục (4 + 56 + 4)
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));       // thẻ danh sách sheet
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, Px(64)));    // thanh hành động
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, Px(31)));    // thanh trạng thái
        }
        // ---------- Thẻ đường dẫn (Excel nguồn / thư mục lưu) ----------
        private void DinhTheDuongDan(TableLayoutPanel noiDung, PictureBox anh, Label tieuDe,
            Krypton.Toolkit.KryptonButton nut, Label duongDan, Padding leThe)
        {
            BocThe(noiDung, leThe, new Padding(12, 6, 12, 6));
            noiDung.BackColor = Color.White;
            noiDung.Margin = Padding.Empty;
            noiDung.ColumnStyles.Clear();
            noiDung.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Px(52)));
            noiDung.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            noiDung.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Px(56)));
            noiDung.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            noiDung.RowStyles.Clear();
            noiDung.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            anh.Anchor = AnchorStyles.Left;
            anh.SizeMode = PictureBoxSizeMode.Zoom;
            anh.Size = new Size(Px(36), Px(36));
            anh.Margin = new Padding(Px(4), 0, Px(8), 0);
            tieuDe.Font = FontNhanDam;
            tieuDe.ForeColor = MauChuDam;
            tieuDe.Anchor = AnchorStyles.Left;
            tieuDe.AutoSize = true;
            tieuDe.Margin = new Padding(0, 0, Px(12), 0);
            nut.Anchor = AnchorStyles.None;
            nut.Size = new Size(Px(42), Px(34));
            nut.Margin = Padding.Empty;
            nut.Cursor = Cursors.Hand;
            KieuNut(nut, FontNut, NutVien);
            // Ô hiển thị đường dẫn: nền slate nhạt, tự cắt "…" khi dài (xem đủ bằng tooltip)
            duongDan.AutoSize = false;
            duongDan.AutoEllipsis = true;
            duongDan.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            duongDan.Height = Px(34);
            duongDan.TextAlign = ContentAlignment.MiddleLeft;
            duongDan.Font = FontDuongDan;
            duongDan.BackColor = MauODuongDan;
            duongDan.Padding = new Padding(Px(10), 0, Px(10), 0);
            duongDan.Margin = new Padding(Px(8), 0, 0, 0);
            duongDan.Cursor = Cursors.Hand;
        }
        // ---------- Thẻ danh sách sheet: thay GroupBox bằng thẻ có tiêu đề ----------
        private void DinhTheDanhSach()
        {
            var clb = checkedListBox1_LietKeTenCacSheet;
            clb.BorderStyle = BorderStyle.None;
            clb.BackColor = Color.White;
            clb.Font = FontDanhSach;
            clb.IntegralHeight = false;
            BocTheTieuDe(groupBox1, clb, new Padding(16, 4, 16, 4));
        }
        // ---------- Thanh hành động: [Chọn tất cả]                         [Tạo tệp (*.pdf)] ----------
        private void DinhThanhHanhDong()
        {
            var t = tableLayoutPanel3;
            t.BackColor = MauNenTrang;
            t.Margin = new Padding(Px(16), 0, Px(16), 0);
            t.ColumnStyles.Clear();
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Px(256)));
            t.RowStyles.Clear();
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            checkBox1_ChonTatCa.Font = FontNhanDam;       // màu chữ đỏ/xanh do form điều khiển
            checkBox1_ChonTatCa.Anchor = AnchorStyles.Left;
            checkBox1_ChonTatCa.Margin = new Padding(Px(6), 0, 0, 0);
            checkBox1_ChonTatCa.Cursor = Cursors.Hand;
            var b = kryptonButton_XuatTepPdf;
            b.Anchor = AnchorStyles.Right;
            b.Size = new Size(Px(248), Px(40));
            b.Margin = Padding.Empty;
            b.Cursor = Cursors.Hand;
            // Màu xanh lá của nút (Designer) giữ nguyên; chỉ bổ sung trạng thái vô hiệu hóa cho dễ nhận biết
            b.StateDisabled.Back.Color1 = Color.FromArgb(241, 245, 249); b.StateDisabled.Back.Color2 = Color.FromArgb(241, 245, 249);
            b.StateDisabled.Border.Color1 = Color.FromArgb(226, 232, 240); b.StateDisabled.Border.Color2 = Color.FromArgb(226, 232, 240);
            b.StateDisabled.Border.Rounding = 8F;
            b.StateDisabled.Content.ShortText.Color1 = Color.FromArgb(148, 163, 184);
        }
        private void DinhThanhTrangThai()
        {
            var s = statusStrip1;
            s.BackColor = Color.White;
            s.ForeColor = MauChuNhan;
            s.SizingGrip = false;
            s.Padding = new Padding(Px(10), 0, Px(10), 0);
            s.Paint += (_, e) =>
            {
                using var p = new Pen(MauVienThe);
                e.Graphics.DrawLine(p, 0, 0, s.Width, 0);
            };
            foreach (var lbl in new[] { toolStripStatusLabel1_TongCongTepPdf, toolStripStatusLabel1_DangLoad, toolStripStatusLabel1_GioiThieu })
                lbl.Font = FontTrangThai;
            toolStripStatusLabel1_GioiThieu.LinkColor = MauChinh;
            toolStripStatusLabel1_GioiThieu.ActiveLinkColor = Color.FromArgb(29, 78, 216);
            toolStripStatusLabel1_GioiThieu.VisitedLinkColor = MauChinh;
        }
        private void HoanThienSauLoad()
        {
            // 1. KIỂM TRA TRẠNG THÁI FORM
            if (_f == null || _f.IsDisposed || _f.Disposing)
                return;
            if (!_f.IsHandleCreated)
                return;
            // 2. CẤU HÌNH THANH TIẾN TRÌNH
            var pb = toolStripProgressBar1_TienTrinhXuatTep;
            if (pb != null && !pb.IsDisposed)
            {
                pb.AutoSize = false;
                pb.Size = new Size(
                    Px(200),
                    Px(12));
                pb.Margin = new Padding(
                    Px(2),
                    Px(5),
                    Px(10),
                    Px(5));
            }
            // 3. KHÔNG THAY ĐỔI KÍCH THƯỚC KHI FORM ĐANG MAXIMIZED
            if (_f.WindowState == FormWindowState.Maximized ||
                _f.WindowState == FormWindowState.Minimized)
            {
                return;
            }
            // 4. XÁC ĐỊNH KÍCH THƯỚC TỐI THIỂU CHO BỐ CỤC
            Size kichThuocHienTai = _f.ClientSize;
            Size kichThuocToiThieu = new Size(
                Px(960),
                Px(600));
            Size kichThuocMoi = new Size(
                Math.Max(kichThuocHienTai.Width, kichThuocToiThieu.Width),
                Math.Max(kichThuocHienTai.Height, kichThuocToiThieu.Height));
            // 5. CHỈ THAY ĐỔI KHI THỰC SỰ CẦN THIẾT
            if (kichThuocMoi == kichThuocHienTai)
                return;
            // 6. XÁC ĐỊNH MÀN HÌNH ĐANG HIỂN THỊ FORM
            Rectangle vungManHinh = Screen.FromControl(_f).WorkingArea;
            // 7. CẬP NHẬT KÍCH THƯỚC FORM
            _f.ClientSize = kichThuocMoi;
            // 8. CĂN GIỮA FORM TRONG VÙNG HIỂN THỊ
            int x = vungManHinh.Left +
                    (vungManHinh.Width - _f.Width) / 2;
            int y = vungManHinh.Top +
                    (vungManHinh.Height - _f.Height) / 2;
            // 9. ĐẢM BẢO FORM KHÔNG VƯỢT QUÁ MÀN HÌNH
            x = Math.Max(vungManHinh.Left,
                Math.Min(x, vungManHinh.Right - _f.Width));
            y = Math.Max(vungManHinh.Top,
                Math.Min(y, vungManHinh.Bottom - _f.Height));
            _f.Location = new Point(x, y);
        }
        // =====================================================================================
        //  TIỆN ÍCH TRÌNH BÀY
        // =====================================================================================
        /// <summary>Bọc một control (nằm trong ô TableLayoutPanel) vào thẻ trắng bo góc, không tiêu đề.</summary>
        private static void BocThe(Control noiDung, Padding leThe, Padding demTrong)
        {
            var cha = noiDung.Parent;
            if (cha == null) return;
            var the = new TheBoGocF48 { Dock = DockStyle.Fill, Margin = leThe, Padding = demTrong };
            cha.SuspendLayout();
            try
            {
                if (cha is TableLayoutPanel tlp)
                {
                    var vt = tlp.GetPositionFromControl(noiDung);
                    tlp.Controls.Remove(noiDung);
                    tlp.Controls.Add(the, vt.Column, vt.Row);
                }
                else { cha.Controls.Remove(noiDung); cha.Controls.Add(the); }
                the.Controls.Add(noiDung);
                noiDung.Dock = DockStyle.Fill;
            }
            finally { cha.ResumeLayout(true); }
        }
        /// <summary>Thay GroupBox bằng thẻ có thanh nhấn + tiêu đề; GroupBox được giữ (ẩn) để mã gán .Text vẫn chạy.</summary>
        private void BocTheTieuDe(GroupBox gb, Control noiDung, Padding leThe)
        {
            var cha = gb.Parent;
            if (cha == null) return;
            var the = new TheBoGocF48
            {
                Dock = DockStyle.Fill,
                Margin = leThe,
                TieuDe = gb.Text,
                Padding = new Padding(Px(12), Px(34), Px(12), Px(10))
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
                else { cha.Controls.Remove(gb); cha.Controls.Add(the); }
                gb.Visible = false;
                gb.Dock = DockStyle.None;
                the.Controls.Add(noiDung);
                noiDung.Dock = DockStyle.Fill;
                the.Controls.Add(gb);
                gb.TextChanged += (_, _) => the.TieuDe = gb.Text;
            }
            finally { cha.ResumeLayout(true); }
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
        //  LỚP PHỤ TRỢ: THẺ TRẮNG BO GÓC
        // =====================================================================================
        internal sealed class TheBoGocF48 : Panel
        {
            private string _tieuDe = "";
            public string TieuDe
            {
                get => _tieuDe;
                set { _tieuDe = value ?? ""; Invalidate(); }
            }
            public TheBoGocF48()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
                BackColor = Color.White;
            }
            protected override void OnPaintBackground(PaintEventArgs e) =>
                e.Graphics.Clear(Parent?.BackColor ?? MauNenTrang);
            protected override void OnPaint(PaintEventArgs e)
            {
                if (Width < 4 || Height < 4) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int d = (int)(20 * DeviceDpi / 96f);
                var r = new Rectangle(0, 0, Width - 1, Height - 1);
                using var path = TaoDuongBoGoc(r, d);
                using var b = new SolidBrush(Color.White);
                using var p = new Pen(MauVienThe);
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
    }
}