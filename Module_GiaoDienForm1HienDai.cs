using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Krypton.Toolkit;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Màn hình đăng nhập (Form1) kiểu "Hiện đại" — chọn ở Form12 > Tab 2 > mục 10 (cùng lựa chọn với Form2).
    /// CHỈ TRÌNH BÀY: không đổi tên / sự kiện / logic đăng nhập; kiểu "Cổ điển" giữ nguyên Designer.
    ///  - Bên trái: nền gradient tím–chàm tự vẽ + vòng tròn trang trí mờ, chữ trắng (Tân binh: xanh lá–ngọc).
    ///  - Bên phải: tiêu đề đậm, ô nhập bo tròn có biểu tượng người / ổ khóa, ô mật khẩu có nút "con mắt" hiện/ẩn
    ///    (bật/tắt chính ô tick "Hiện mật khẩu" đã ẩn đi => trạng thái vẫn được lưu như cũ), viền tím khi đang nhập.
    ///  - Nút Đăng nhập tím đậm, đổi màu tức thì khi rê/nhấn; nút Thoát dạng viền mảnh.
    /// Vị trí tính theo đơn vị thiết kế (Designer) nhân hệ số tự co giãn hiện tại => đúng trên mọi DPI.
    /// Mọi phần có try/catch riêng: lỗi trình bày không bao giờ chặn đăng nhập.
    /// </summary>
    internal static class Module_GiaoDienForm1HienDai
    {
        // ===== Bảng màu (đồng bộ Form2 Hiện đại) =====
        public static readonly Color MauVienThuong = Color.FromArgb(203, 213, 225);
        public static readonly Color MauVienFocus = Color.FromArgb(37, 99, 235);
        public static readonly Color MauNenLoi = Color.FromArgb(254, 226, 226);   // ô bỏ trống khi bấm Đăng nhập
        private static readonly Color MauChinh = Color.FromArgb(37, 99, 235);       // xanh dương (bỏ tím)
        private static readonly Color MauChinhHover = Color.FromArgb(29, 78, 216);
        private static readonly Color MauChinhNhan = Color.FromArgb(30, 64, 175);
        private static readonly Color MauChinhKhoa = Color.FromArgb(147, 197, 253);
        private static readonly Color MauTieuDe = Color.FromArgb(15, 23, 42);
        private static readonly Color MauPhu = Color.FromArgb(100, 116, 139);
        private static readonly Color MauNhan = Color.FromArgb(51, 65, 85);
        private static readonly Color MauBieuTuong = Color.FromArgb(148, 163, 184);
        private static readonly Color MauVienNhat = Color.FromArgb(226, 232, 240);
        private static bool _tanBinh;
        private static Bitmap? _anhMat, _anhMatTat;
        public static void ApDung(Form f, TableLayoutPanel trai, PictureBox logo, Label chaoMung, Label moTa,
            Panel phai, Label tieuDe, Label phuDe, Label nhanTen, KryptonTextBox oTen, Label nhanMk, KryptonTextBox oMk,
            CheckBox hienMk, LinkLabel quenMk, KryptonButton dangNhap, KryptonButton thoat, LinkLabel dangKy,
            Label thongBaoPhienBan, Label phienBan, Label dong, Label thuNho)
        {
            // Hệ số co giãn thực tế (Designer: ô nhập rộng 400) => đặt vị trí theo đơn vị thiết kế mà vẫn đúng DPI
            float k = oTen.Width > 0 ? oTen.Width / 400f : f.DeviceDpi / 96f;
            int P(float v) => (int)Math.Round(v * k);
            f.SuspendLayout();
            phai.SuspendLayout();
            try
            {
                Chay("khung", () =>
                {
                    f.Padding = new Padding(0);   // màu đổ sát mép form (trước: viền 1px để lộ khe sáng quanh phần màu)
                    f.BackColor = Color.White;
                    phai.BackColor = Color.White;
                    dong.BackColor = Color.White; thuNho.BackColor = Color.White;
                    trai.Width = P(300);           // phần trang trí bên trái hẹp lại, phần đăng nhập rộng rãi hơn
                });
                Chay("ben trai", () => DinhBenTrai(trai, logo, chaoMung, moTa, k));
                Chay("vach chay", () => new VachChayF1(trai, k).GanVaoForm(f));
                // Khung đăng nhập rộng 400 (thiết kế), căn giữa phần trắng bên phải
                int x0 = Math.Max(P(24), (f.ClientSize.Width - trai.Width - P(400)) / 2);
                Point D(float y) => new Point(x0, P(y));
                Chay("chu", () =>
                {
                    tieuDe.Font = new Font(Module_HeThong.TenFontHeThongDam, 17F, FontStyle.Regular);
                    tieuDe.ForeColor = MauTieuDe;
                    tieuDe.Location = new Point(x0 - P(2), P(62));
                    phuDe.Font = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Regular);
                    phuDe.ForeColor = MauPhu;
                    phuDe.Location = D(100);
                    foreach (var (nhan, y) in new[] { (nhanTen, 146f), (nhanMk, 226f) })
                    {
                        nhan.Font = new Font(Module_HeThong.TenFontHeThongDam, 9.5F, FontStyle.Regular);
                        nhan.ForeColor = MauNhan;
                        nhan.Location = D(y);
                    }
                });
                Chay("o nhap", () =>
                {
                    DinhONhap(oTen, "NguoiDung", k);
                    DinhONhap(oMk, "KhoaMK", k);
                    oTen.Location = D(168);
                    oMk.Location = D(248);
                    GanNutMat(oMk, hienMk, k);
                });
                Chay("lien ket", () =>
                {
                    hienMk.Visible = false;   // thay bằng nút con mắt trong ô mật khẩu (vẫn bật/tắt chính ô tick này)
                    foreach (var lk in new[] { quenMk, dangKy })
                    {
                        lk.LinkColor = MauChinh; lk.VisitedLinkColor = MauChinh; lk.ActiveLinkColor = MauChinhNhan;
                        lk.Font = new Font(Module_HeThong.TenFontHeThongDam, 9.5F, FontStyle.Regular);
                    }
                    quenMk.Location = new Point(x0 + P(400) - quenMk.Width, P(300));
                    dangKy.Location = D(450);
                    dangKy.Width = P(400);
                });
                Chay("nut", () =>
                {
                    NutChinh(dangNhap, k);
                    dangNhap.Location = D(334);
                    dangNhap.Size = new Size(P(400), P(46));
                    NutVien(thoat, k);
                    thoat.Location = D(392);
                    thoat.Size = new Size(P(400), P(42));
                });
                Chay("phien ban", () =>
                {
                    thongBaoPhienBan.Location = D(474);
                    phienBan.Location = D(490);
                    phienBan.ForeColor = MauBieuTuong;
                });
            }
            finally
            {
                phai.ResumeLayout(true);
                f.ResumeLayout(true);
            }
        }
        private static void Chay(string ten, Action a)
        {
            try { a(); }
            catch (Exception ex) { Debug.WriteLine("[GiaoDienForm1HienDai] " + ten + ": " + ex.Message); }
        }
        // ---------------- Bên trái: nền tối (cùng tông menu Form2) + quầng sáng sau logo + lưới chấm mờ ----------------
        private static void DinhBenTrai(TableLayoutPanel trai, PictureBox logo, Label chaoMung, Label moTa, float k)
        {
            logo.BackColor = Color.Transparent;
            chaoMung.BackColor = Color.Transparent;
            chaoMung.ForeColor = Color.White;
            chaoMung.Font = new Font(Module_HeThong.TenFontHeThongDam, 16F, FontStyle.Regular);
            moTa.BackColor = Color.Transparent;
            moTa.ForeColor = Color.FromArgb(160, 170, 200);
            moTa.Font = new Font(Module_HeThong.TenFontHeThong, 9.75F, FontStyle.Regular);
            trai.Paint += (s, e) => VeNenTrai(e.Graphics, trai.ClientRectangle, logo.Visible ? logo.Bounds : Rectangle.Empty, k);
            trai.Resize += (s, e) => trai.Invalidate();
        }
        /// <summary>Form1 gọi khi biết phiên bản (Tân binh: tông xanh lá; còn lại: tông xanh dương).</summary>
        public static void DatTanBinh(Control trai, bool tanBinh)
        {
            _tanBinh = tanBinh;
            trai.Invalidate(true);
        }
        private static void VeNenTrai(Graphics g, Rectangle r, Rectangle oLogo, float k)
        {
            if (r.Width < 2 || r.Height < 2) return;
            Color tren = _tanBinh ? Color.FromArgb(10, 46, 38) : Color.FromArgb(27, 24, 54);    // cùng màu menu trái Form2
            Color duoi = _tanBinh ? Color.FromArgb(4, 28, 24) : Color.FromArgb(14, 12, 32);
            Color sang = _tanBinh ? Color.FromArgb(16, 185, 129) : Color.FromArgb(59, 130, 246); // màu quầng sáng
            using (var to = new LinearGradientBrush(r, tren, duoi, 90f))
                g.FillRectangle(to, r);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            // Quầng sáng tròn mềm phía sau logo
            if (oLogo.Width > 0)
            {
                float d = Math.Max(oLogo.Width, oLogo.Height) * 1.55f;
                var vung = new RectangleF(oLogo.X + oLogo.Width / 2f - d / 2f, oLogo.Y + oLogo.Height / 2f - d / 2f, d, d);
                using var duong = new GraphicsPath();
                duong.AddEllipse(vung);
                using var qs = new PathGradientBrush(duong)
                {
                    CenterColor = Color.FromArgb(95, sang),
                    SurroundColors = new[] { Color.FromArgb(0, sang) }
                };
                g.FillEllipse(qs, vung);
            }
            // Lưới chấm mờ góc trên-phải và dưới-trái (họa tiết kiểu web)
            using (var cham = new SolidBrush(Color.FromArgb(38, Color.White)))
            {
                float buoc = 14 * k, rc = Math.Max(1.2f, 1.5f * k);
                for (int hang = 0; hang < 5; hang++)
                    for (int cot = 0; cot < 6; cot++)
                    {
                        g.FillEllipse(cham, r.Right - 20 * k - cot * buoc, 18 * k + hang * buoc, rc * 2, rc * 2);
                        g.FillEllipse(cham, r.Left + 20 * k + cot * buoc, r.Bottom - 22 * k - hang * buoc, rc * 2, rc * 2);
                    }
            }
            // (Vạch nhấn dưới lời chào: control riêng VachChayF1 — có hiệu ứng chạy, chỉ vẽ lại vùng nhỏ của nó)
        }
        private static Color MauNenTaiDo(float tiLeY)
        {
            Color tren = _tanBinh ? Color.FromArgb(10, 46, 38) : Color.FromArgb(27, 24, 54);
            Color duoi = _tanBinh ? Color.FromArgb(4, 28, 24) : Color.FromArgb(14, 12, 32);
            float t = Math.Clamp(tiLeY, 0f, 1f);
            return Color.FromArgb((int)(tren.R + (duoi.R - tren.R) * t), (int)(tren.G + (duoi.G - tren.G) * t), (int)(tren.B + (duoi.B - tren.B) * t));
        }
        private static Color MauSang => _tanBinh ? Color.FromArgb(16, 185, 129) : Color.FromArgb(59, 130, 246);
        private const float ViTriVach = 0.70f;   // theo chiều cao phần trái (ngay dưới "Chào mừng trở lại")
        /// <summary>
        /// Vạch nhấn dưới "Chào mừng trở lại" có 1 vệt sáng chạy từ trái sang phải.
        /// NHẸ: control riêng ~96x3 px (mỗi khung chỉ vẽ lại đúng vùng này, không vẽ lại nền/logo), ~30 khung/giây,
        /// vị trí tính theo đồng hồ (mượt dù bộ đếm bị trễ), bộ đếm CHỈ chạy khi cửa sổ đăng nhập đang hiện & không thu nhỏ
        /// (đăng nhập xong form ẩn => dừng hẳn; đóng form => hủy).
        /// </summary>
        [System.ComponentModel.DesignerCategory("Code")]
        private sealed class VachChayF1 : Control
        {
            private const int ChuKyMs = 1800;
            private readonly Control _trai;
            private readonly System.Windows.Forms.Timer _timer = new() { Interval = 33 };
            private readonly long _goc = Environment.TickCount64;
            public VachChayF1(Control trai, float k)
            {
                _trai = trai;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.Opaque, true);
                SetStyle(ControlStyles.Selectable, false);
                TabStop = false;
                Size = new Size(Math.Max(24, (int)Math.Round(96 * k)), Math.Max(2, (int)Math.Round(3 * k)));
                _timer.Tick += (s, e) => Invalidate();
                trai.SizeChanged += (s, e) => DatViTri();
                trai.LocationChanged += (s, e) => DatViTri();
            }
            public void GanVaoForm(Form f)
            {
                f.Controls.Add(this);
                BringToFront();
                DatViTri();
                f.VisibleChanged += (s, e) => CapNhatChay(f);
                f.Resize += (s, e) => CapNhatChay(f);           // thu nhỏ / phục hồi
                f.FormClosed += (s, e) => _timer.Stop();
                CapNhatChay(f);
            }
            private void CapNhatChay(Form f)
            {
                bool chay = f.Visible && f.WindowState != FormWindowState.Minimized && !IsDisposed;
                if (chay && !_timer.Enabled) _timer.Start();
                else if (!chay && _timer.Enabled) _timer.Stop();
            }
            private void DatViTri()
            {
                Location = new Point(_trai.Left + (_trai.Width - Width) / 2, _trai.Top + (int)(_trai.Height * ViTriVach));
            }
            protected override void Dispose(bool disposing)
            {
                if (disposing) { _timer.Stop(); _timer.Dispose(); }
                base.Dispose(disposing);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                Color sang = MauSang;
                Color nen = MauNenTaiDo(_trai.Height > 0 ? (float)(Top - _trai.Top) / _trai.Height : ViTriVach);
                // Rãnh: màu nhấn hòa 35% vào nền
                Color ranh = Color.FromArgb(nen.R + (sang.R - nen.R) * 35 / 100, nen.G + (sang.G - nen.G) * 35 / 100, nen.B + (sang.B - nen.B) * 35 / 100);
                g.Clear(ranh);
                // Vệt sáng dài 45% chạy từ ngoài mép trái sang hết mép phải, nhanh dần rồi chậm dần
                double t = ((Environment.TickCount64 - _goc) % ChuKyMs) / (double)ChuKyMs;
                double e2 = t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2;
                float dai = Width * 0.45f;
                float x = (float)(-dai + (Width + dai) * e2);
                var vung = new RectangleF(x, 0, dai, Height);
                using var to = new LinearGradientBrush(new RectangleF(x - 1, 0, dai + 2, Height), ranh, ranh, 0f)
                {
                    InterpolationColors = new ColorBlend
                    {
                        Colors = new[] { ranh, sang, Color.FromArgb(Math.Min(255, sang.R + 60), Math.Min(255, sang.G + 60), Math.Min(255, sang.B + 40)), sang, ranh },
                        Positions = new[] { 0f, 0.35f, 0.5f, 0.65f, 1f }
                    }
                };
                g.FillRectangle(to, vung);
            }
        }
        // ---------------- Ô nhập ----------------
        private static void DinhONhap(KryptonTextBox o, string bieuTuong, float k)
        {
            o.StateCommon.Back.Color1 = Color.White;
            o.StateCommon.Border.DrawBorders = PaletteDrawBorders.All;
            o.StateCommon.Border.Color1 = MauVienThuong;
            o.StateCommon.Border.Color2 = MauVienThuong;
            o.StateCommon.Border.Rounding = 10F;
            o.StateCommon.Content.Font = new Font(Module_HeThong.TenFontHeThong, 11F, FontStyle.Regular);
            o.StateCommon.Content.Color1 = MauTieuDe;
            o.StateCommon.Content.Padding = new Padding((int)(4 * k), (int)(8 * k), (int)(8 * k), (int)(8 * k));
            o.Width = (int)Math.Round(400 * k);
            var bt = new ButtonSpecAny
            {
                Image = Module_BieuTuongMenu.Ve(bieuTuong, (int)Math.Round(18 * k), MauBieuTuong),
                Edge = PaletteRelativeEdgeAlign.Near,
                Style = PaletteButtonStyle.Inherit,
                Enabled = ButtonEnabled.False           // chỉ để trang trí, không bấm được
            };
            bt.ImageStates.ImageDisabled = bt.Image;     // giữ màu biểu tượng khi "khóa"
            o.ButtonSpecs.Add(bt);
        }
        /// <summary>
        /// Nút "con mắt" TỰ VẼ nằm đè mép phải ô mật khẩu (không dùng ButtonSpec của Krypton vì nó vẽ khung vuông):
        /// không viền, rê chuột hiện nền tròn mờ. Ô mật khẩu chừa lề phải để chữ không chạy xuống dưới nút.
        /// </summary>
        private static void GanNutMat(KryptonTextBox oMk, CheckBox hienMk, float k)
        {
            int co = (int)Math.Round(18 * k);
            _anhMat ??= Module_BieuTuongMenu.Ve("Mat", co, MauBieuTuong);
            _anhMatTat ??= Module_BieuTuongMenu.Ve("MatTat", co, MauBieuTuong);
            var pad = oMk.StateCommon.Content.Padding;
            oMk.StateCommon.Content.Padding = new Padding(pad.Left, pad.Top, (int)Math.Round(40 * k), pad.Bottom);
            var nut = new NutMatF1(oMk) { Size = new Size((int)Math.Round(30 * k), (int)Math.Round(30 * k)) };
            void CapNhat() { nut.Anh = hienMk.Checked ? _anhMatTat : _anhMat; }
            CapNhat();
            // Bấm con mắt = bấm ô tick "Hiện mật khẩu" (đã ẩn): CheckedChanged gốc đổi ký tự che + lưu CSDL như cũ
            nut.Click += (s, e) => { hienMk.Checked = !hienMk.Checked; oMk.Focus(); };
            hienMk.CheckedChanged += (s, e) => CapNhat();
        }
        /// <summary>Nút con mắt tự vẽ: nền cùng màu nền ô nhập (kể cả khi ô tô đỏ vì bỏ trống), hover = tròn xám nhạt.</summary>
        [System.ComponentModel.DesignerCategory("Code")]
        private sealed class NutMatF1 : Control
        {
            private readonly KryptonTextBox _o;
            private bool _hover;
            private Image? _anh;
            public Image? Anh { get => _anh; set { _anh = value; Invalidate(); } }
            public NutMatF1(KryptonTextBox o)
            {
                _o = o;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                SetStyle(ControlStyles.Selectable, false);
                TabStop = false;
                Cursor = Cursors.Hand;
                new ToolTip().SetToolTip(this, "Hiện / ẩn mật khẩu");
                o.LocationChanged += (s, e) => DatViTri();
                o.SizeChanged += (s, e) => DatViTri();
                o.VisibleChanged += (s, e) => Visible = o.Visible;
                o.TextChanged += (s, e) => Invalidate();     // nền ô đổi (tô đỏ khi bỏ trống / trả trắng) => vẽ lại theo
                o.ParentChanged += (s, e) => GanCha();
                GanCha();
            }
            private void GanCha()
            {
                if (_o.Parent == null || Parent == _o.Parent) { DatViTri(); return; }
                Parent?.Controls.Remove(this);
                _o.Parent.Controls.Add(this);
                DatViTri();
            }
            private void DatViTri()
            {
                if (Parent == null) return;
                int le = (int)Math.Round(6 * DeviceDpi / 96f);
                Location = new Point(_o.Right - Width - le, _o.Top + (_o.Height - Height) / 2);
                BringToFront();
            }
            // Kích thước được gán SAU constructor (và đổi theo DPI) => tính lại vị trí mỗi lần đổi cỡ
            protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); DatViTri(); }
            protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = true; Invalidate(); }
            protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = false; Invalidate(); }
            protected override void OnPaintBackground(PaintEventArgs e)
            {
                Color nen = _o.StateCommon.Back.Color1;
                e.Graphics.Clear(nen.IsEmpty || nen.A == 0 ? Color.White : nen);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                if (_hover)
                {
                    using var b = new SolidBrush(Color.FromArgb(241, 245, 249));
                    g.FillEllipse(b, 0, 0, Width - 1, Height - 1);
                }
                if (_anh != null)
                    g.DrawImage(_anh, (Width - _anh.Width) / 2, (Height - _anh.Height) / 2, _anh.Width, _anh.Height);
            }
        }
        // ---------------- Nút ----------------
        private static void NutChinh(KryptonButton b, float k)
        {
            var font = new Font(Module_HeThong.TenFontHeThongDam, 11F, FontStyle.Regular);
            Dat(b.StateCommon.Back, b.StateCommon.Border, b.StateCommon.Content, MauChinh, MauChinh, Color.White, font);
            Dat(b.StateNormal.Back, b.StateNormal.Border, b.StateNormal.Content, MauChinh, MauChinh, Color.White, font);
            Dat(b.OverrideDefault.Back, b.OverrideDefault.Border, b.OverrideDefault.Content, MauChinh, MauChinh, Color.White, font);
            Dat(b.OverrideFocus.Back, b.OverrideFocus.Border, b.OverrideFocus.Content, MauChinh, MauChinh, Color.White, font);
            Dat(b.StateTracking.Back, b.StateTracking.Border, b.StateTracking.Content, MauChinhHover, MauChinhHover, Color.White, font);
            Dat(b.StatePressed.Back, b.StatePressed.Border, b.StatePressed.Content, MauChinhNhan, MauChinhNhan, Color.White, font);
            // Đang xác thực (nút bị khóa): tím nhạt, chữ trắng "Đang đăng nhập..." vẫn đọc rõ (Krypton mặc định xám)
            Dat(b.StateDisabled.Back, b.StateDisabled.Border, b.StateDisabled.Content, MauChinhKhoa, MauChinhKhoa, Color.White, font);
            b.StateCommon.Border.Rounding = 10F;
            b.StateCommon.Border.Width = 1;
            b.StateCommon.Border.DrawBorders = PaletteDrawBorders.All;
        }
        private static void NutVien(KryptonButton b, float k)
        {
            var font = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Regular);
            Color chu = Color.FromArgb(71, 85, 105);
            Dat(b.StateCommon.Back, b.StateCommon.Border, b.StateCommon.Content, Color.White, MauVienNhat, chu, font);
            Dat(b.StateNormal.Back, b.StateNormal.Border, b.StateNormal.Content, Color.White, MauVienNhat, chu, font);
            Dat(b.OverrideDefault.Back, b.OverrideDefault.Border, b.OverrideDefault.Content, Color.White, MauVienNhat, chu, font);
            Dat(b.OverrideFocus.Back, b.OverrideFocus.Border, b.OverrideFocus.Content, Color.White, MauVienNhat, chu, font);
            Dat(b.StateTracking.Back, b.StateTracking.Border, b.StateTracking.Content, Color.FromArgb(248, 250, 252), MauVienThuong, chu, font);
            Dat(b.StatePressed.Back, b.StatePressed.Border, b.StatePressed.Content, Color.FromArgb(241, 245, 249), MauVienThuong, chu, font);
            b.StateCommon.Border.Rounding = 10F;
            b.StateCommon.Border.Width = 1;
            b.StateCommon.Border.DrawBorders = PaletteDrawBorders.All;
        }
        private static void Dat(PaletteBack back, PaletteBorder vien, PaletteContent noiDung, Color mauNen, Color mauVien, Color mauChu, Font font)
        {
            back.ColorStyle = PaletteColorStyle.Solid;
            back.Color1 = mauNen; back.Color2 = mauNen;
            vien.Color1 = mauVien; vien.Color2 = mauVien;
            noiDung.ShortText.Color1 = mauChu; noiDung.ShortText.Color2 = mauChu;
            noiDung.ShortText.Font = font;
        }
    }
}
