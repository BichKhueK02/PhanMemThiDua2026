///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
#nullable enable
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Text;
using Mf40 = PhanMemThiDua2026.Module_GiaoDienForm40Web;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// GIAO DIỆN KIỂU TRANG WEB cho Form40_BoQuaDonViCanhBaoTyLe.
    /// - Không sửa Designer/resx: module tìm điều khiển theo Name, dựng trang mới rồi chuyển các điều khiển cũ sang.
    /// - Gọi ApDung(this) ở ĐẦU sự kiện Load của form (sau khi AutoScale đã chạy xong nên kích thước
    ///   đúng theo DPI, không bị nhân đôi tỷ lệ, và người dùng không thấy form vẽ dở).
    /// - Danh sách đơn vị là điều khiển tự vẽ (DanhSachDonViWebF40): 1 chạm để bật/tắt, tìm không dấu,
    ///   cuộn mượt bằng chuột/bàn phím, không dùng CheckedListBox (không giật, không bôi xanh dòng).
    /// </summary>
    internal static class Module_GiaoDienForm40Web
    {
        // ===== BẢNG MÀU (đồng bộ theme tím nhạt của Form4/Form10) =====
        internal static readonly Color MauNenTrang = Color.FromArgb(246, 244, 251);
        internal static readonly Color MauVienThe = Color.FromArgb(228, 222, 244);
        internal static readonly Color MauTim = Color.FromArgb(124, 58, 237);
        internal static readonly Color MauTimDam = Color.FromArgb(91, 33, 182);
        internal static readonly Color MauTimNhat = Color.FromArgb(239, 233, 253);
        internal static readonly Color MauTimRatNhat = Color.FromArgb(247, 244, 255);
        internal static readonly Color MauChuDam = Color.FromArgb(31, 27, 46);
        internal static readonly Color MauChuPhu = Color.FromArgb(107, 103, 133);
        internal static readonly Color MauXanh = Color.FromArgb(21, 128, 61);
        internal static readonly Color MauXanhNhat = Color.FromArgb(220, 252, 231);
        /// <summary>Quy đổi pixel thiết kế (96 DPI) sang pixel thực theo DPI hiện tại của điều khiển.</summary>
        internal static int Px(Control c, int v) => (int)Math.Round(v * c.DeviceDpi / 96.0);
        internal static GraphicsPath TaoDuongBoGoc(RectangleF r, float banKinh)
        {
            var p = new GraphicsPath();
            float d = Math.Min(banKinh * 2f, Math.Min(r.Width, r.Height));
            if (d <= 0.5f)
            {
                p.AddRectangle(r);
                return p;
            }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
        /// <summary>Màu nền thật của vùng chứa phía sau (để vẽ góc bo không bị đen/viền lạ).</summary>
        internal static Color MauNenCha(Control c)
        {
            for (Control? p = c.Parent; p != null; p = p.Parent)
            {
                if (p.BackColor.A == 255) return p.BackColor;
            }
            return MauNenTrang;
        }
        private static T TimDieuKhien<T>(Form f, string ten) where T : Control
        {
            return f.Controls.Find(ten, true).OfType<T>().FirstOrDefault()
                   ?? throw new InvalidOperationException($"Form40: không tìm thấy điều khiển '{ten}' để áp dụng giao diện.");
        }
        // =====================================================================================
        //  ÁP DỤNG GIAO DIỆN
        // =====================================================================================
        /// <summary>
        /// Dựng giao diện web cho form và trả về điều khiển danh sách để form gốc nạp/đọc dữ liệu.
        /// Gọi lại lần 2 sẽ trả về chính điều khiển đã tạo (an toàn, không dựng trùng).
        /// </summary>
        public static DanhSachDonViWebF40 ApDung(Form f)
        {
            if (f == null) throw new ArgumentNullException(nameof(f));
            var daCo = f.Controls.Find("dsDonViWebF40", true).OfType<DanhSachDonViWebF40>().FirstOrDefault();
            if (daCo != null) return daCo;
            var nutDong = TimDieuKhien<Krypton.Toolkit.KryptonButton>(f, "kryptonButton1_DongFrom");
            var anhHuongDan = TimDieuKhien<PictureBox>(f, "pictureBox1");
            var nhanMoTa = TimDieuKhien<Label>(f, "label9");
            var khungCu = TimDieuKhien<TableLayoutPanel>(f, "tableLayoutPanel2");
            int P(int v) => Px(f, v);
            // Tài nguyên tự tạo (font, tooltip) → hủy theo vòng đời form
            var tuHuy = new List<IDisposable>();
            Font TaoFont(float co, FontStyle kieu = FontStyle.Regular)
            {
                var ft = new Font(Module_HeThong.TenFontHeThong, co, kieu, GraphicsUnit.Point);
                tuHuy.Add(ft);
                return ft;
            }
            f.Disposed += (_, _) =>
            {
                foreach (var d in tuHuy) d.Dispose();
                tuHuy.Clear();
            };
            var goiY = new ToolTip { InitialDelay = 300, ReshowDelay = 100, AutoPopDelay = 6000 };
            tuHuy.Add(goiY);
            f.SuspendLayout();
            try
            {
                // ---------- Cửa sổ ----------
                f.BackColor = MauNenTrang;
                var vung = Screen.FromControl(f).WorkingArea;
                f.ClientSize = new Size(Math.Min(P(700), vung.Width - P(40)), Math.Min(P(640), vung.Height - P(90)));
                f.MinimumSize = new Size(Math.Min(P(560), f.Width), Math.Min(P(480), f.Height));
                // ---------- Khung trang ----------
                var trang = new Panel
                {
                    Name = "pnlTrangF40",
                    Dock = DockStyle.Fill,
                    BackColor = MauNenTrang,
                    Padding = new Padding(P(20), P(18), P(20), P(12))
                };
                var luoi = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 4,
                    BackColor = MauNenTrang,
                    Margin = Padding.Empty
                };
                luoi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                luoi.RowStyles.Add(new RowStyle(SizeType.Absolute, P(92)));   // tiêu đề
                luoi.RowStyles.Add(new RowStyle(SizeType.Absolute, P(54)));   // tìm kiếm + nút
                luoi.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));     // danh sách
                luoi.RowStyles.Add(new RowStyle(SizeType.Absolute, P(64)));   // chân trang
                // ---------- Hàng 0: thẻ tiêu đề ----------
                var theDau = new TheBoGocF40
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0, 0, 0, P(12)),
                    Padding = new Padding(P(16), P(10), P(16), P(10))
                };
                var luoiDau = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 1,
                    BackColor = Color.White,
                    Margin = Padding.Empty
                };
                luoiDau.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, P(68)));
                luoiDau.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                luoiDau.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                var khungIcon = new TheBoGocF40
                {
                    BanKinh = 12,
                    MauNen = MauTimNhat,
                    CoVien = false,
                    CoBong = false,
                    Anchor = AnchorStyles.Left,
                    Size = new Size(P(52), P(52)),
                    Margin = Padding.Empty,
                    Padding = new Padding(P(6))
                };
                anhHuongDan.Dock = DockStyle.Fill;
                anhHuongDan.SizeMode = PictureBoxSizeMode.Zoom;
                anhHuongDan.BackColor = MauTimNhat;
                anhHuongDan.Cursor = Cursors.Hand;
                anhHuongDan.Margin = Padding.Empty;
                khungIcon.Controls.Add(anhHuongDan);
                goiY.SetToolTip(anhHuongDan, "Bấm để xem hướng dẫn nghiệp vụ");
                var luoiChu = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2,
                    BackColor = Color.White,
                    Margin = Padding.Empty
                };
                luoiChu.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                luoiChu.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                luoiChu.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                var tieuDe = new Label
                {
                    Text = f.Text,
                    AutoSize = true,
                    Font = TaoFont(13.5F, FontStyle.Bold),
                    ForeColor = MauChuDam,
                    BackColor = Color.White,
                    Margin = new Padding(0, 0, 0, P(2))
                };
                nhanMoTa.AutoSize = false;
                nhanMoTa.Dock = DockStyle.Fill;
                nhanMoTa.Font = TaoFont(9.5F);
                nhanMoTa.ForeColor = MauChuPhu;
                nhanMoTa.BackColor = Color.White;
                nhanMoTa.TextAlign = ContentAlignment.TopLeft;
                nhanMoTa.Margin = Padding.Empty;
                luoiChu.Controls.Add(tieuDe, 0, 0);
                luoiChu.Controls.Add(nhanMoTa, 0, 1);
                luoiDau.Controls.Add(khungIcon, 0, 0);
                luoiDau.Controls.Add(luoiChu, 1, 0);
                theDau.Controls.Add(luoiDau);
                // ---------- Hàng 1: tìm kiếm + nút chọn nhanh ----------
                var luoiCongCu = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 3,
                    RowCount = 1,
                    BackColor = MauNenTrang,
                    Margin = Padding.Empty
                };
                luoiCongCu.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                luoiCongCu.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, P(128)));
                luoiCongCu.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, P(104)));
                luoiCongCu.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                var oTim = new OTimKiemWebF40
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0, 0, 0, P(10)),
                    TabIndex = 0
                };
                var nutTatCa = new NutWebF40
                {
                    Text = "Chọn tất cả",
                    Dock = DockStyle.Fill,
                    Margin = new Padding(P(10), 0, 0, P(10)),
                    TabIndex = 1
                };
                var nutBoChon = new NutWebF40
                {
                    Text = "Bỏ chọn",
                    Dock = DockStyle.Fill,
                    Margin = new Padding(P(10), 0, 0, P(10)),
                    TabIndex = 2
                };
                goiY.SetToolTip(nutTatCa, "Chọn tất cả các đơn vị đang hiển thị");
                goiY.SetToolTip(nutBoChon, "Bỏ chọn tất cả các đơn vị đang hiển thị");
                luoiCongCu.Controls.Add(oTim, 0, 0);
                luoiCongCu.Controls.Add(nutTatCa, 1, 0);
                luoiCongCu.Controls.Add(nutBoChon, 2, 0);
                // ---------- Hàng 2: thẻ danh sách ----------
                var theDS = new TheBoGocF40
                {
                    Dock = DockStyle.Fill,
                    Margin = Padding.Empty,
                    Padding = new Padding(P(8))
                };
                var luoiDS = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2,
                    BackColor = Color.White,
                    Margin = Padding.Empty
                };
                luoiDS.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                luoiDS.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                luoiDS.RowStyles.Add(new RowStyle(SizeType.Absolute, P(30)));
                var ds = new DanhSachDonViWebF40
                {
                    Name = "dsDonViWebF40",
                    Dock = DockStyle.Fill,
                    Margin = Padding.Empty,
                    TabIndex = 3
                };
                var nhanTrangThai = new Label
                {
                    Dock = DockStyle.Fill,
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = TaoFont(9F),
                    ForeColor = MauChuPhu,
                    BackColor = Color.White,
                    Padding = new Padding(P(10), 0, 0, 0),
                    Margin = Padding.Empty
                };
                luoiDS.Controls.Add(ds, 0, 0);
                luoiDS.Controls.Add(nhanTrangThai, 0, 1);
                theDS.Controls.Add(luoiDS);
                // ---------- Hàng 3: chân trang ----------
                var luoiCuoi = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 1,
                    BackColor = MauNenTrang,
                    Margin = Padding.Empty
                };
                luoiCuoi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                luoiCuoi.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, P(176)));
                luoiCuoi.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                var nhanGoiY = new Label
                {
                    Text = "Thay đổi chỉ được áp dụng khi bấm “Đóng và lưu”.",
                    Dock = DockStyle.Fill,
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = TaoFont(9F),
                    ForeColor = MauChuPhu,
                    BackColor = MauNenTrang,
                    Margin = new Padding(P(4), P(12), 0, 0)
                };
                KieuNutChinh(nutDong, P(10), TaoFont(10.5F, FontStyle.Bold));
                nutDong.Dock = DockStyle.Fill;
                nutDong.Margin = new Padding(0, P(14), 0, P(4));
                nutDong.TabIndex = 4;
                luoiCuoi.Controls.Add(nhanGoiY, 0, 0);
                luoiCuoi.Controls.Add(nutDong, 1, 0);
                // ---------- Ghép trang ----------
                luoi.Controls.Add(theDau, 0, 0);
                luoi.Controls.Add(luoiCongCu, 0, 1);
                luoi.Controls.Add(theDS, 0, 2);
                luoi.Controls.Add(luoiCuoi, 0, 3);
                trang.Controls.Add(luoi);
                f.Controls.Add(trang);
                // Bỏ giao diện cũ (kéo theo CheckedListBox cũ bị hủy). Các điều khiển cần dùng đã được chuyển sang trang mới.
                f.Controls.Remove(khungCu);
                khungCu.Dispose();
                // ---------- Nối sự kiện nội bộ ----------
                void CapNhatTrangThai()
                {
                    if (nhanTrangThai.IsDisposed) return;
                    if (ds.DangTai)
                    {
                        nhanTrangThai.Text = "Đang tải danh sách đơn vị…";
                    }
                    else
                    {
                        string s = $"Đã chọn {ds.SoDuocChon}/{ds.SoTong} đơn vị";
                        if (ds.DangLoc) s += $"   ·   Đang hiển thị {ds.SoHienThi} kết quả";
                        nhanTrangThai.Text = s;
                    }
                    bool coThe = !ds.DangTai && ds.SoHienThi > 0;
                    nutTatCa.Enabled = coThe;
                    nutBoChon.Enabled = coThe;
                }
                ds.TrangThaiThayDoi += (_, _) => CapNhatTrangThai();
                oTim.Txt.TextChanged += (_, _) => ds.LocTheo(oTim.Txt.Text);
                oTim.YeuCauXuongDanhSach += (_, _) => ds.Focus();
                nutTatCa.Click += (_, _) => ds.DatChoCacDongDangHien(true);
                nutBoChon.Click += (_, _) => ds.DatChoCacDongDangHien(false);
                CapNhatTrangThai();
                // Căn giữa lại sau khi đổi kích thước
                f.Location = new Point(vung.Left + (vung.Width - f.Width) / 2, vung.Top + (vung.Height - f.Height) / 2);
                return ds;
            }
            finally
            {
                f.ResumeLayout(true);
            }
        }
        /// <summary>Nút Krypton "Đóng và lưu": nền tím đặc, chữ trắng, bo góc. Bọc try để lệch phiên bản Krypton không làm hỏng form.</summary>
        private static void KieuNutChinh(Krypton.Toolkit.KryptonButton b, int banKinh, Font font)
        {
            try
            {
                var tim = MauTim;
                var timDam = MauTimDam;
                var nhan = Color.FromArgb(76, 29, 149);
                var khoa = Color.FromArgb(196, 181, 253);
                b.Values.Text = "Đóng và lưu";
                // Trạng thái chung (kế thừa cho Normal)
                b.StateCommon.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
                b.StateCommon.Back.Color1 = tim;
                b.StateCommon.Back.Color2 = tim;
                b.StateCommon.Border.Color1 = tim;
                b.StateCommon.Border.Color2 = tim;
                b.StateCommon.Border.Width = 1;
                b.StateCommon.Border.Rounding = banKinh;
                b.StateCommon.Content.ShortText.Font = font;
                b.StateCommon.Content.ShortText.Color1 = Color.White;
                b.StateCommon.Content.ShortText.Color2 = Color.White;
                // Rê chuột
                b.StateTracking.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
                b.StateTracking.Back.Color1 = timDam;
                b.StateTracking.Back.Color2 = timDam;
                b.StateTracking.Border.Color1 = timDam;
                b.StateTracking.Border.Color2 = timDam;
                b.StateTracking.Content.ShortText.Color1 = Color.White;
                b.StateTracking.Content.ShortText.Color2 = Color.White;
                // Nhấn
                b.StatePressed.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
                b.StatePressed.Back.Color1 = nhan;
                b.StatePressed.Back.Color2 = nhan;
                b.StatePressed.Border.Color1 = nhan;
                b.StatePressed.Border.Color2 = nhan;
                b.StatePressed.Content.ShortText.Color1 = Color.White;
                b.StatePressed.Content.ShortText.Color2 = Color.White;
                // Đang lưu (nút bị khóa)
                b.StateDisabled.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
                b.StateDisabled.Back.Color1 = khoa;
                b.StateDisabled.Back.Color2 = khoa;
                b.StateDisabled.Border.Color1 = khoa;
                b.StateDisabled.Border.Color2 = khoa;
                b.StateDisabled.Content.ShortText.Color1 = Color.White;
                b.StateDisabled.Content.ShortText.Color2 = Color.White;
                b.Cursor = Cursors.Hand;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Form40: không đổi được kiểu nút Krypton: " + ex.Message);
            }
        }
    }
    // =========================================================================================
    //  THẺ BO GÓC (card): nền + viền + bóng nhẹ, tự vẽ, double-buffer
    // =========================================================================================
    internal class TheBoGocF40 : Panel
    {
        private Color _mauNen = Color.White;
        public int BanKinh { get; set; } = 14;
        public Color MauVien { get; set; } = Mf40.MauVienThe;
        public bool CoVien { get; set; } = true;
        public bool CoBong { get; set; } = true;
        public Color MauNen
        {
            get => _mauNen;
            set
            {
                _mauNen = value;
                BackColor = value; // điều khiển con không đặt màu sẽ kế thừa đúng màu thẻ
                Invalidate();
            }
        }
        public TheBoGocF40()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = _mauNen;
        }
        protected virtual Color LayMauVien() => MauVien;
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using var b = new SolidBrush(Mf40.MauNenCha(this));
            e.Graphics.FillRectangle(b, e.ClipRectangle);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float k = DeviceDpi / 96f;
            float bk = BanKinh * k;
            float bong = CoBong ? Math.Max(2f, 2f * k) : 0f;
            var rc = new RectangleF(0.5f, 0.5f, Width - 2f, Height - bong - 2f);
            if (rc.Width <= 2 || rc.Height <= 2) return;
            if (CoBong)
            {
                var rcBong = new RectangleF(rc.X + 1f, rc.Y + bong, rc.Width - 2f, rc.Height);
                using var pb = Mf40.TaoDuongBoGoc(rcBong, bk);
                using var bb = new SolidBrush(Color.FromArgb(18, 91, 33, 182));
                g.FillPath(bb, pb);
            }
            using var p = Mf40.TaoDuongBoGoc(rc, bk);
            using (var b = new SolidBrush(_mauNen)) g.FillPath(b, p);
            if (CoVien)
            {
                using var pen = new Pen(LayMauVien(), 1f);
                g.DrawPath(pen, p);
            }
        }
    }
    // =========================================================================================
    //  Ô TÌM KIẾM: thẻ bo góc + TextBox không viền + biểu tượng kính lúp
    // =========================================================================================
    internal sealed class OTimKiemWebF40 : TheBoGocF40
    {
        private bool _dangFocus;
        private readonly Font _font = new(Module_HeThong.TenFontHeThong, 10.5F, FontStyle.Regular, GraphicsUnit.Point);
        public TextBox Txt { get; }
        public event EventHandler? YeuCauXuongDanhSach;
        public OTimKiemWebF40()
        {
            BanKinh = 12;
            CoBong = false;
            MauNen = Color.White;
            Txt = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = Mf40.MauChuDam,
                Font = _font,
                PlaceholderText = "Tìm đơn vị (gõ không dấu cũng được)…",
                MaxLength = 100,
                TabStop = true
            };
            Controls.Add(Txt);
            Txt.GotFocus += (_, _) => { _dangFocus = true; Invalidate(); };
            Txt.LostFocus += (_, _) => { _dangFocus = false; Invalidate(); };
            Txt.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Escape && Txt.TextLength > 0)
                {
                    Txt.Clear();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Down)
                {
                    YeuCauXuongDanhSach?.Invoke(this, EventArgs.Empty);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            Click += (_, _) => Txt.Focus();
        }
        protected override Color LayMauVien() => _dangFocus ? Mf40.MauTim : MauVien;
        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            int trai = Mf40.Px(this, 42), phai = Mf40.Px(this, 14);
            Txt.Width = Math.Max(20, Width - trai - phai);
            Txt.Location = new Point(trai, Math.Max(0, (Height - Txt.Height) / 2));
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float k = DeviceDpi / 96f;
            float cx = 24f * k, cy = Height / 2f - 1f * k, r = 6.5f * k;
            using var pen = new Pen(_dangFocus ? Mf40.MauTim : Mf40.MauChuPhu, Math.Max(1.8f, 1.8f * k))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            g.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);
            g.DrawLine(pen, cx + r * 0.72f, cy + r * 0.72f, cx + r * 0.72f + 5f * k, cy + r * 0.72f + 5f * k);
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) _font.Dispose();
        }
    }
    // =========================================================================================
    //  NÚT PHỤ (tím nhạt, viền tím, chữ tím đậm) – tự vẽ
    // =========================================================================================
    internal sealed class NutWebF40 : Control
    {
        private bool _hover, _nhan;
        private readonly Font _font = new(Module_HeThong.TenFontHeThong, 9.5F, FontStyle.Bold, GraphicsUnit.Point);
        public NutWebF40()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            TabStop = true;
            Cursor = Cursors.Hand;
            Font = _font;
        }
        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _nhan = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { _nhan = true; Invalidate(); }
            base.OnMouseDown(e);
        }
        protected override void OnMouseUp(MouseEventArgs e) { _nhan = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Cursor = Enabled ? Cursors.Hand : Cursors.Default; Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { _nhan = false; Invalidate(); base.OnLostFocus(e); }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                _nhan = true;
                Invalidate();
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }
        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (_nhan && (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter))
            {
                _nhan = false;
                Invalidate();
                if (Enabled) OnClick(EventArgs.Empty);
                e.Handled = true;
            }
            base.OnKeyUp(e);
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using var b = new SolidBrush(Mf40.MauNenCha(this));
            e.Graphics.FillRectangle(b, e.ClipRectangle);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color nen, vien, chu;
            if (!Enabled)
            {
                nen = Color.FromArgb(244, 242, 249);
                vien = Color.FromArgb(222, 218, 235);
                chu = Color.FromArgb(160, 156, 180);
            }
            else
            {
                nen = _nhan ? Color.FromArgb(219, 207, 248) : _hover ? Color.FromArgb(229, 220, 251) : Mf40.MauTimNhat;
                vien = Mf40.MauTim;
                chu = Mf40.MauTimDam;
            }
            var rc = new RectangleF(0.5f, 0.5f, Width - 2f, Height - 2f);
            if (rc.Width <= 2 || rc.Height <= 2) return;
            using (var p = Mf40.TaoDuongBoGoc(rc, Mf40.Px(this, 12)))
            {
                using (var b = new SolidBrush(nen)) g.FillPath(b, p);
                using var pen = new Pen(vien, 1f);
                g.DrawPath(pen, p);
            }
            if (Focused && ShowFocusCues && Enabled)
            {
                var rf = RectangleF.Inflate(rc, -3f, -3f);
                using var pf = Mf40.TaoDuongBoGoc(rf, Mf40.Px(this, 9));
                using var pen = new Pen(Color.FromArgb(150, Mf40.MauTim), 1.5f);
                g.DrawPath(pen, pf);
            }
            TextRenderer.DrawText(g, Text, Font, Rectangle.Round(rc), chu,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) _font.Dispose();
        }
    }
    // =========================================================================================
    //  DANH SÁCH ĐƠN VỊ KIỂU WEB (thay CheckedListBox)
    //  - 1 chạm vào dòng là bật/tắt; tìm không dấu; cuộn chuột/bàn phím; thanh cuộn mảnh tự vẽ.
    //  - Chỉ vẽ các dòng nằm trong vùng cần vẽ → mượt kể cả khi danh sách dài.
    // =========================================================================================
    internal sealed class DanhSachDonViWebF40 : Control
    {
        private sealed class Muc
        {
            public Muc(string ten, bool chon)
            {
                Ten = ten;
                Chon = chon;
                Khoa = ChuanHoaTimKiem(ten);
            }
            public string Ten { get; }
            public string Khoa { get; }
            public bool Chon { get; set; }
        }
        private const string ChuPill = "Bỏ qua cảnh báo";
        private readonly List<Muc> _tatCa = new();
        private readonly List<Muc> _hienThi = new();
        private readonly HashSet<string> _goc = new(StringComparer.OrdinalIgnoreCase); // trạng thái đã tích lúc nạp
        private readonly Font _fontTen = new(Module_HeThong.TenFontHeThong, 10.5F, FontStyle.Regular, GraphicsUnit.Point);
        private readonly Font _fontPill = new(Module_HeThong.TenFontHeThong, 8.5F, FontStyle.Bold, GraphicsUnit.Point);
        private string _tuKhoa = "";
        private int _cuon;
        private int _hover = -1;
        private int _focus = -1;
        private int _keoDy;
        private int _rongPill = -1;
        private int _rongPillDpi;
        private bool _dangTai;
        private bool _keoThumb;
        private bool _hoverThumb;
        /// <summary>Báo cho form khi số lượng chọn / trạng thái tải / bộ lọc thay đổi.</summary>
        public event EventHandler? TrangThaiThayDoi;
        public DanhSachDonViWebF40()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            TabStop = true;
            BackColor = Color.White;
        }
        // ---------- API công khai ----------
        public bool DangTai
        {
            get => _dangTai;
            set
            {
                if (_dangTai == value) return;
                _dangTai = value;
                _hover = -1;
                Invalidate();
                OnTrangThai();
            }
        }
        public bool DangLoc => _tuKhoa.Length > 0;
        public int SoTong => _tatCa.Count;
        public int SoHienThi => _hienThi.Count;
        public int SoDuocChon
        {
            get
            {
                int n = 0;
                foreach (var m in _tatCa) if (m.Chon) n++;
                return n;
            }
        }
        /// <summary>Nạp toàn bộ danh sách (tự bỏ tên trống/trùng) và chốt trạng thái gốc để so sánh thay đổi.</summary>
        public void NapDuLieu(IEnumerable<(string Ten, bool Chon)> nguon)
        {
            _tatCa.Clear();
            _goc.Clear();
            var daCo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (ten, chon) in nguon)
            {
                string t = (ten ?? "").Trim();
                if (t.Length == 0 || !daCo.Add(t)) continue;
                _tatCa.Add(new Muc(t, chon));
                if (chon) _goc.Add(t);
            }
            _dangTai = false;
            LamMoiHienThi();
            OnTrangThai();
        }
        /// <summary>Danh sách tên đang được tích, theo đúng thứ tự gốc (không phụ thuộc bộ lọc).</summary>
        public List<string> LayDanhSachDuocChon()
        {
            var ds = new List<string>();
            foreach (var m in _tatCa) if (m.Chon) ds.Add(m.Ten);
            return ds;
        }
        /// <summary>true nếu trạng thái tích hiện tại khác lúc nạp.</summary>
        public bool CoThayDoi()
        {
            foreach (var m in _tatCa)
            {
                if (m.Chon != _goc.Contains(m.Ten)) return true;
            }
            return false;
        }
        public void LocTheo(string tuKhoa)
        {
            string q = ChuanHoaTimKiem((tuKhoa ?? "").Trim());
            if (q == _tuKhoa) return;
            _tuKhoa = q;
            LamMoiHienThi();
            OnTrangThai();
        }
        public void DatChoCacDongDangHien(bool chon)
        {
            bool doi = false;
            foreach (var m in _hienThi)
            {
                if (m.Chon != chon) { m.Chon = chon; doi = true; }
            }
            if (!doi) return;
            Invalidate();
            OnTrangThai();
        }
        // ---------- Xử lý dữ liệu ----------
        private void OnTrangThai() => TrangThaiThayDoi?.Invoke(this, EventArgs.Empty);
        /// <summary>Bỏ dấu + hạ chữ thường để tìm "trung doi 1" khớp "TRUNG ĐỘI 1".</summary>
        private static string ChuanHoaTimKiem(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            string d = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(d.Length);
            foreach (char c in d)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                sb.Append(c == 'đ' || c == 'Đ' ? 'd' : char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }
        private void LamMoiHienThi()
        {
            _hienThi.Clear();
            if (_tuKhoa.Length == 0)
            {
                _hienThi.AddRange(_tatCa);
            }
            else
            {
                foreach (var m in _tatCa)
                {
                    if (m.Khoa.Contains(_tuKhoa, StringComparison.Ordinal)) _hienThi.Add(m);
                }
            }
            _hover = -1;
            _hoverThumb = false;
            _focus = _hienThi.Count > 0 ? 0 : -1;
            _cuon = 0;
            Invalidate();
        }
        private void Dao(int viTri)
        {
            if ((uint)viTri >= (uint)_hienThi.Count) return;
            var m = _hienThi[viTri];
            m.Chon = !m.Chon;
            Invalidate();
            OnTrangThai();
        }
        // ---------- Kích thước & cuộn ----------
        private int Px(int v) => Mf40.Px(this, v);
        private int DongCao => Px(46);
        private int TongCao => _hienThi.Count * DongCao + Px(8);
        private int MaxCuon => Math.Max(0, TongCao - Height);
        private bool CoThanhCuon => MaxCuon > 0;
        private void KepCuon() => _cuon = Math.Min(Math.Max(0, _cuon), MaxCuon);
        private void DatCuon(int giaTri)
        {
            int cu = _cuon;
            _cuon = giaTri;
            KepCuon();
            if (_cuon != cu) Invalidate();
        }
        private void HienDong(int viTri)
        {
            if ((uint)viTri >= (uint)_hienThi.Count) return;
            int dong = DongCao, le = Px(4);
            int tren = le + viTri * dong;
            if (tren < _cuon) DatCuon(tren - le);
            else if (tren + dong > _cuon + Height) DatCuon(tren + dong - Height + le);
        }
        private Rectangle LayThumb(out int range)
        {
            int cao = Height, le = Px(4);
            int th = Math.Max(Px(36), (int)((long)cao * cao / Math.Max(1, TongCao)));
            th = Math.Min(th, Math.Max(Px(12), cao - 2 * le));
            range = Math.Max(1, cao - th - 2 * le);
            int ty = le + (int)((long)range * _cuon / Math.Max(1, MaxCuon));
            return new Rectangle(Width - Px(10), ty, Px(6), th);
        }
        private int ViTriDong(int y)
        {
            int v = y + _cuon - Px(4);
            if (v < 0) return -1;
            int i = v / DongCao;
            return i < _hienThi.Count ? i : -1;
        }
        // ---------- Vẽ ----------
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            using (var nen = new SolidBrush(BackColor)) g.FillRectangle(nen, e.ClipRectangle);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (_dangTai) { VeKhungXuong(g); return; }
            if (_hienThi.Count == 0) { VeTrong(g); return; }
            int dong = DongCao, le = Px(4);
            int dau = Math.Max(0, (_cuon + e.ClipRectangle.Top - le) / dong);
            int cuoi = Math.Min(_hienThi.Count - 1, (_cuon + e.ClipRectangle.Bottom - le) / dong);
            for (int i = dau; i <= cuoi; i++) VeDong(g, i, le + i * dong - _cuon);
            VeThanhCuon(g);
        }
        private int RongPill()
        {
            if (_rongPill < 0 || _rongPillDpi != DeviceDpi)
            {
                _rongPill = TextRenderer.MeasureText(ChuPill, _fontPill).Width + Px(22);
                _rongPillDpi = DeviceDpi;
            }
            return _rongPill;
        }
        private void VeDong(Graphics g, int i, int y)
        {
            var m = _hienThi[i];
            int rongVung = Width - Px(8) - (CoThanhCuon ? Px(12) : 0);
            var rc = new Rectangle(Px(4), y + Px(2), Math.Max(10, rongVung), DongCao - Px(4));
            bool hover = i == _hover;
            bool focus = i == _focus && Focused && ShowFocusCues;
            if (m.Chon || hover || focus)
            {
                Color nen = m.Chon ? Color.FromArgb(243, 238, 255) : Mf40.MauTimRatNhat;
                using var p = Mf40.TaoDuongBoGoc(rc, Px(10));
                using (var b = new SolidBrush(nen)) g.FillPath(b, p);
                if (focus)
                {
                    using var pen = new Pen(Color.FromArgb(200, Mf40.MauTim), 1.5f);
                    g.DrawPath(pen, p);
                }
            }
            // Ô tích
            int cb = Px(20);
            var rcCb = new Rectangle(rc.X + Px(14), rc.Y + (rc.Height - cb) / 2, cb, cb);
            using (var p = Mf40.TaoDuongBoGoc(rcCb, Px(6)))
            {
                if (m.Chon)
                {
                    using var b = new SolidBrush(Mf40.MauTim);
                    g.FillPath(b, p);
                }
                else
                {
                    using var b = new SolidBrush(Color.White);
                    g.FillPath(b, p);
                    using var pen = new Pen(hover ? Mf40.MauTim : Color.FromArgb(178, 170, 206), 1.6f);
                    g.DrawPath(pen, p);
                }
            }
            if (m.Chon)
            {
                using var pen = new Pen(Color.White, Math.Max(2f, 2.2f * DeviceDpi / 96f))
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                };
                g.DrawLines(pen, new[]
                {
                    new PointF(rcCb.X + cb * 0.25f, rcCb.Y + cb * 0.52f),
                    new PointF(rcCb.X + cb * 0.43f, rcCb.Y + cb * 0.70f),
                    new PointF(rcCb.X + cb * 0.76f, rcCb.Y + cb * 0.32f)
                });
            }
            int trai = rcCb.Right + Px(12);
            int phai = rc.Right - Px(12);
            // Nhãn "Bỏ qua cảnh báo" (chỉ hiện nếu còn đủ chỗ cho tên)
            if (m.Chon && phai - trai > RongPill() + Px(140))
            {
                int w = RongPill(), h = Px(24);
                var rcP = new Rectangle(phai - w, rc.Y + (rc.Height - h) / 2, w, h);
                using (var pp = Mf40.TaoDuongBoGoc(rcP, h / 2f))
                using (var bp = new SolidBrush(Mf40.MauXanhNhat))
                {
                    g.FillPath(bp, pp);
                }
                TextRenderer.DrawText(g, ChuPill, _fontPill, rcP, Mf40.MauXanh,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
                phai = rcP.Left - Px(10);
            }
            var rcChu = new Rectangle(trai, rc.Y, Math.Max(0, phai - trai), rc.Height);
            TextRenderer.DrawText(g, m.Ten, _fontTen, rcChu, m.Chon ? Mf40.MauTimDam : Mf40.MauChuDam,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        }
        private void VeThanhCuon(Graphics g)
        {
            if (!CoThanhCuon) return;
            var t = LayThumb(out _);
            using var p = Mf40.TaoDuongBoGoc(t, t.Width / 2f);
            using var b = new SolidBrush(_keoThumb || _hoverThumb ? Color.FromArgb(167, 139, 250) : Color.FromArgb(214, 205, 240));
            g.FillPath(b, p);
        }
        private void VeKhungXuong(Graphics g)
        {
            int dong = DongCao;
            int n = Math.Max(1, Math.Min(8, Height / dong));
            float[] doDai = { 0.55f, 0.40f, 0.62f, 0.48f, 0.58f, 0.36f, 0.50f, 0.44f };
            using var nen = new SolidBrush(Color.FromArgb(240, 237, 248));
            for (int i = 0; i < n; i++)
            {
                int y = Px(4) + i * dong;
                int cb = Px(20);
                var rcCb = new Rectangle(Px(18), y + (dong - cb) / 2, cb, cb);
                using (var p = Mf40.TaoDuongBoGoc(rcCb, Px(6))) g.FillPath(nen, p);
                int x = rcCb.Right + Px(12);
                int w = (int)((Width - x - Px(24)) * doDai[i % doDai.Length]);
                var rcBar = new Rectangle(x, y + (dong - Px(12)) / 2, Math.Max(Px(40), w), Px(12));
                using (var p = Mf40.TaoDuongBoGoc(rcBar, Px(6))) g.FillPath(nen, p);
            }
        }
        private void VeTrong(Graphics g)
        {
            string s = _tatCa.Count == 0 ? "Chưa có đơn vị nào trong danh mục." : "Không tìm thấy đơn vị phù hợp.";
            TextRenderer.DrawText(g, s, _fontTen, ClientRectangle, Mf40.MauChuPhu,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        }
        // ---------- Chuột ----------
        private void CapNhatHover(Point p)
        {
            int dong = -1;
            bool thumb = false;
            if (!_dangTai)
            {
                if (CoThanhCuon && p.X >= Width - Px(16))
                {
                    var t = LayThumb(out _);
                    thumb = p.Y >= t.Y && p.Y <= t.Bottom;
                }
                else
                {
                    dong = ViTriDong(p.Y);
                }
            }
            if (dong == _hover && thumb == _hoverThumb) return;
            _hover = dong;
            _hoverThumb = thumb;
            Cursor = dong >= 0 ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            // Để lăn chuột là cuộn được ngay, trừ khi người dùng đang gõ ở ô tìm kiếm
            var f = FindForm();
            if (!Focused && f != null && f == Form.ActiveForm && f.ActiveControl is not TextBoxBase) Focus();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_keoThumb)
            {
                LayThumb(out int range);
                int ty = e.Y - _keoDy - Px(4);
                DatCuon((int)((long)ty * MaxCuon / Math.Max(1, range)));
                return;
            }
            CapNhatHover(e.Location);
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_keoThumb) return;
            if (_hover != -1 || _hoverThumb)
            {
                _hover = -1;
                _hoverThumb = false;
                Cursor = Cursors.Default;
                Invalidate();
            }
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || _dangTai) return;
            if (!Focused) Focus();
            if (CoThanhCuon && e.X >= Width - Px(16))
            {
                var t = LayThumb(out _);
                if (e.Y >= t.Y && e.Y <= t.Bottom)
                {
                    _keoThumb = true;
                    _keoDy = e.Y - t.Y;
                    Capture = true;
                    Invalidate();
                }
                else
                {
                    DatCuon(_cuon + (e.Y < t.Y ? -1 : 1) * Math.Max(DongCao, Height - DongCao));
                }
                return;
            }
            int dong = ViTriDong(e.Y);
            if (dong >= 0)
            {
                _focus = dong;
                Dao(dong); // 1 chạm là đảo trạng thái ngay
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_keoThumb)
            {
                _keoThumb = false;
                Capture = false;
                CapNhatHover(e.Location);
                Invalidate();
            }
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!CoThanhCuon) return;
            DatCuon(_cuon - e.Delta * DongCao * 2 / 120);
            CapNhatHover(PointToClient(Cursor.Position));
        }
        // ---------- Bàn phím ----------
        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Home:
                case Keys.End:
                    return true;
            }
            return base.IsInputKey(keyData);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (_dangTai || _hienThi.Count == 0) return;
            int trang = Math.Max(1, Height / DongCao - 1);
            int moi = _focus < 0 ? 0 : _focus;
            switch (e.KeyCode)
            {
                case Keys.Down: moi = Math.Min(_hienThi.Count - 1, moi + 1); break;
                case Keys.Up: moi = Math.Max(0, moi - 1); break;
                case Keys.PageDown: moi = Math.Min(_hienThi.Count - 1, moi + trang); break;
                case Keys.PageUp: moi = Math.Max(0, moi - trang); break;
                case Keys.Home: moi = 0; break;
                case Keys.End: moi = _hienThi.Count - 1; break;
                case Keys.Space:
                    _focus = moi;
                    Dao(moi);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                case Keys.A when e.Control:
                    DatChoCacDongDangHien(true);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                default:
                    return;
            }
            e.Handled = true;
            _focus = moi;
            HienDong(moi);
            Invalidate();
        }
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            if (_focus < 0 && _hienThi.Count > 0) _focus = 0;
            Invalidate();
        }
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            KepCuon();
        }
        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            _rongPill = -1;
            KepCuon();
            Invalidate();
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _fontTen.Dispose();
                _fontPill.Dispose();
            }
        }
    }
}