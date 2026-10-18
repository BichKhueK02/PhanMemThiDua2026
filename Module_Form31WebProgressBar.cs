using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
namespace PhanMemThiDua2026
{
    public enum TrangThaiTienTrinh
    {
        DangChay,
        ThanhCong,
        CanhBao,
        Loi
    }
    /// <summary>
    /// Thanh tiến trình phong cách web: dạng viên thuốc (pill), gradient, vệt sáng chạy (shimmer),
    /// chữ đổi màu theo vùng đã chạy. Tự vẽ bằng GDI+, hoạt ảnh tính theo thời gian thực
    /// nên mượt và không phụ thuộc tốc độ khung hình.
    /// Mọi phương thức công khai đều an toàn khi gọi từ luồng nền.
    /// </summary>
    [DefaultProperty(nameof(Value))]
    [DesignerCategory("Code")]
    public sealed class Module_Form31WebProgressBar : Control
    {
        // ===== Tham số hoạt ảnh =====
        private const int KHOANG_TICK_MS = 16;                 // ~60 FPS
        private const float HE_SO_BAT_KIP = 9f;                // càng lớn càng bắt kịp mục tiêu nhanh
        private const float HE_SO_BO_CHAM = 0.45f;             // tốc độ "bò" chậm tới trần khi đang xử lý
        private const float CHU_KY_SHIMMER_GIAY = 1.6f;
        private const float CHU_KY_KHONG_XAC_DINH_GIAY = 1.5f;
        private const float DO_RONG_KHONG_XAC_DINH = 0.38f;    // tỉ lệ chiều rộng thanh trượt
        // ===== Bảng màu (phong cách Tailwind) =====
        private static readonly Color MauChuToi = Color.FromArgb(55, 65, 81);
        private static readonly Color MauChuSang = Color.White;
        private static readonly Color MauVien = Color.FromArgb(40, 0, 0, 0);
        private readonly System.Windows.Forms.Timer _timer;
        private readonly Stopwatch _dongHo = Stopwatch.StartNew();
        private readonly StringFormat _sfTrai;
        private readonly StringFormat _sfPhai;
        private Font? _fontDam;
        private Color _mauNen = Color.FromArgb(229, 231, 235);
        private float _hienThi;      // giá trị đang vẽ (0..100)
        private float _mucTieu;      // giá trị cần đạt
        private float _tran;         // trần để "bò" chậm khi bước hiện tại chưa xong
        private float _phaseShimmer; // 0..1
        private float _phaseKxd;     // 0..1
        private double _thoiDiemTruoc;
        private bool _dangChay;
        private bool _khongXacDinh;
        private bool _hienPhanTram = true;
        private string _chuThich = string.Empty;
        private TrangThaiTienTrinh _trangThai = TrangThaiTienTrinh.DangChay;
        public Module_Form31WebProgressBar()
        {
            SetStyle(ControlStyles.UserPaint
                     | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
            AccessibleRole = AccessibleRole.ProgressBar;
            _sfTrai = new StringFormat(StringFormatFlags.NoWrap)
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter
            };
            _sfPhai = new StringFormat(StringFormatFlags.NoWrap)
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.None
            };
            _timer = new System.Windows.Forms.Timer { Interval = KHOANG_TICK_MS };
            _timer.Tick += Timer_Tick;
        }
        protected override Size DefaultSize => new(300, 24);
        // ===== Thuộc tính (hiển thị trong Designer) =====
        /// <summary>Giá trị 0..100. Dùng BaoTienDo() nếu muốn kèm trần "bò" và chú thích.</summary>
        [Category("Behavior"), DefaultValue(0)]
        public int Value
        {
            get => (int)Math.Round(_mucTieu);
            set => BaoTienDo(value);
        }
        [Category("Appearance"), DefaultValue(true)]
        public bool HienPhanTram
        {
            get => _hienPhanTram;
            set { _hienPhanTram = value; Invalidate(); }
        }
        [Category("Appearance"), DefaultValue(typeof(Color), "229, 231, 235")]
        public Color MauNen
        {
            get => _mauNen;
            set { _mauNen = value; Invalidate(); }
        }
        // ===== API điều khiển =====
        /// <summary>Bắt đầu một phiên mới (về 0%). khongXacDinh = thanh trượt qua lại khi chưa biết tổng khối lượng.</summary>
        public void BatDau(bool khongXacDinh = false)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => BatDau(khongXacDinh))); return; }
            _hienThi = _mucTieu = _tran = 0f;
            _phaseShimmer = _phaseKxd = 0f;
            _khongXacDinh = khongXacDinh;
            _trangThai = TrangThaiTienTrinh.DangChay;
            _chuThich = string.Empty;
            _dangChay = true;
            KhoiDongTimer();
            Invalidate();
        }
        /// <summary>
        /// Báo tiến độ. phanTram là mục tiêu thật; tiemCanDen (nếu có) là trần để thanh tự "bò" chậm
        /// trong lúc bước hiện tại chưa xong, giúp thanh không bao giờ đứng yên.
        /// </summary>
        public void BaoTienDo(float phanTram, float? tiemCanDen = null, string? chuThich = null)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => BaoTienDo(phanTram, tiemCanDen, chuThich))); return; }
            _khongXacDinh = false;
            _mucTieu = Math.Max(_mucTieu, Math.Clamp(phanTram, 0f, 100f));
            _tran = Math.Clamp(tiemCanDen ?? _mucTieu, _mucTieu, 100f);
            if (chuThich != null) _chuThich = chuThich;
            KhoiDongTimer();
            Invalidate();
        }
        /// <summary>Kết thúc phiên: đổi màu theo trạng thái, dừng vệt sáng. ThanhCong luôn chạy tới 100%.</summary>
        public void HoanTat(TrangThaiTienTrinh trangThai, bool chayDen100 = true, string? chuThich = null)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => HoanTat(trangThai, chayDen100, chuThich))); return; }
            _dangChay = false;
            _khongXacDinh = false;
            _trangThai = trangThai;
            if (trangThai == TrangThaiTienTrinh.ThanhCong || chayDen100) _mucTieu = 100f;
            _tran = _mucTieu;
            if (chuThich != null) _chuThich = chuThich;
            KhoiDongTimer();
            Invalidate();
        }
        /// <summary>Đưa về trạng thái ban đầu (0%, màu mặc định, dừng timer).</summary>
        public void DatLai()
        {
            if (InvokeRequired) { BeginInvoke(new Action(DatLai)); return; }
            _hienThi = _mucTieu = _tran = 0f;
            _dangChay = false;
            _khongXacDinh = false;
            _trangThai = TrangThaiTienTrinh.DangChay;
            _chuThich = string.Empty;
            _timer.Stop();
            Invalidate();
        }
        // ===== Timer & vòng đời =====
        private bool DangThietKe =>
            LicenseManager.UsageMode == LicenseUsageMode.Designtime || DesignMode;
        private void KhoiDongTimer()
        {
            if (DangThietKe || IsDisposed || !IsHandleCreated || !Visible || _timer.Enabled) return;
            _thoiDiemTruoc = _dongHo.Elapsed.TotalSeconds;
            _timer.Start();
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_dangChay || Math.Abs(_mucTieu - _hienThi) > 0.02f) KhoiDongTimer();
        }
        protected override void OnHandleDestroyed(EventArgs e)
        {
            _timer.Stop();
            base.OnHandleDestroyed(e);
        }
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible)
            {
                if (_dangChay || Math.Abs(_mucTieu - _hienThi) > 0.02f) KhoiDongTimer();
            }
            else
            {
                _timer.Stop();
            }
        }
        protected override void OnFontChanged(EventArgs e)
        {
            _fontDam?.Dispose();
            _fontDam = null;
            base.OnFontChanged(e);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer.Dispose();
                _fontDam?.Dispose();
                _sfTrai.Dispose();
                _sfPhai.Dispose();
            }
            base.Dispose(disposing);
        }
        private void Timer_Tick(object? sender, EventArgs e)
        {
            double bayGio = _dongHo.Elapsed.TotalSeconds;
            float dt = (float)Math.Min(bayGio - _thoiDiemTruoc, 0.1); // chặn nhảy vọt khi UI bị treo ngắn
            _thoiDiemTruoc = bayGio;
            float chenhLech = _mucTieu - _hienThi;
            if (chenhLech > 0.02f)
            {
                // Bắt kịp mục tiêu kiểu ease-out, độc lập FPS
                _hienThi += chenhLech * (1f - MathF.Exp(-HE_SO_BAT_KIP * dt));
            }
            else if (_dangChay && !_khongXacDinh && _tran - _hienThi > 0.05f)
            {
                // Đã chạm mục tiêu nhưng bước hiện tại chưa xong: bò chậm về phía trần
                _hienThi += (_tran - _hienThi) * (1f - MathF.Exp(-HE_SO_BO_CHAM * dt));
            }
            else if (!_dangChay)
            {
                _hienThi = Math.Max(_hienThi, _mucTieu);
            }
            if (_dangChay)
            {
                _phaseShimmer = (_phaseShimmer + dt / CHU_KY_SHIMMER_GIAY) % 1f;
                _phaseKxd = (_phaseKxd + dt / CHU_KY_KHONG_XAC_DINH_GIAY) % 1f;
            }
            Invalidate();
            // Dừng timer khi không còn gì chuyển động -> 0% CPU lúc rảnh
            if (!_dangChay && _mucTieu - _hienThi <= 0.02f)
            {
                _hienThi = _mucTieu;
                _timer.Stop();
            }
        }
        // ===== Vẽ =====
        private Font FontDam => _fontDam ??= new Font(Font, FontStyle.Bold);
        private static (Color Dau, Color Cuoi) MauTheoTrangThai(TrangThaiTienTrinh t) => t switch
        {
            TrangThaiTienTrinh.ThanhCong => (Color.FromArgb(74, 222, 128), Color.FromArgb(22, 163, 74)),
            TrangThaiTienTrinh.CanhBao => (Color.FromArgb(251, 191, 36), Color.FromArgb(245, 158, 11)),
            TrangThaiTienTrinh.Loi => (Color.FromArgb(248, 113, 113), Color.FromArgb(220, 38, 38)),
            _ => (Color.FromArgb(129, 140, 248), Color.FromArgb(59, 130, 246))
        };
        /// <summary>Hình viên thuốc: hai đầu bán nguyệt. Yêu cầu rộng >= cao.</summary>
        private static GraphicsPath VienThuoc(RectangleF rc)
        {
            var path = new GraphicsPath();
            float d = rc.Height;
            if (d <= 0f || rc.Width < d)
            {
                path.AddRectangle(rc);
                return path;
            }
            path.AddArc(rc.X, rc.Y, d, d, 90, 180);
            path.AddArc(rc.Right - d, rc.Y, d, d, 270, 180);
            path.CloseFigure();
            return path;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            var rc = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            if (rc.Width < rc.Height || rc.Height < 4f) return;
            float pct = Math.Clamp(_hienThi, 0f, 100f);
            bool kxd = _khongXacDinh && _dangChay;
            // 1) Vùng đã chạy
            RectangleF vungChay;
            if (kxd)
            {
                float w = rc.Width * DO_RONG_KHONG_XAC_DINH;
                float t = 0.5f - 0.5f * MathF.Cos(MathF.PI * _phaseKxd); // ease-in-out
                vungChay = new RectangleF(rc.X - w + (rc.Width + w) * t, rc.Y, w, rc.Height);
            }
            else
            {
                float w = rc.Width * pct / 100f;
                vungChay = new RectangleF(rc.X, rc.Y, w, rc.Height);
            }
            bool coVungChay = vungChay.Width > 0.5f;
            // Vùng quá hẹp vẫn giữ hình viên thuốc: đẩy sang trái, phần thừa bị cắt bởi nền
            RectangleF vungVe = vungChay;
            if (coVungChay && vungVe.Width < rc.Height)
                vungVe = new RectangleF(vungChay.Right - rc.Height, rc.Y, rc.Height, rc.Height);
            using GraphicsPath nen = VienThuoc(rc);
            using GraphicsPath? chay = coVungChay ? VienThuoc(vungVe) : null;
            // 2) Nền + viền mảnh
            using (var coNen = new SolidBrush(_mauNen))
                g.FillPath(coNen, nen);
            if (chay != null)
            {
                g.SetClip(nen, CombineMode.Replace);
                var (mauDau, mauCuoi) = MauTheoTrangThai(_trangThai);
                using (var gradient = new LinearGradientBrush(vungVe, mauDau, mauCuoi, LinearGradientMode.Horizontal))
                    g.FillPath(gradient, chay);
                g.SetClip(chay, CombineMode.Replace);
                // Lớp bóng mờ nửa trên cho cảm giác "nổi"
                using (var bong = new SolidBrush(Color.FromArgb(34, 255, 255, 255)))
                    g.FillRectangle(bong, vungVe.X, vungVe.Y, vungVe.Width, vungVe.Height / 2f);
                // Vệt sáng chạy qua khi đang xử lý
                if (_dangChay && !kxd && vungVe.Width > 12f)
                {
                    float bandW = Math.Max(rc.Height * 2.5f, vungVe.Width * 0.35f);
                    float x = vungVe.X - bandW + (vungVe.Width + bandW) * _phaseShimmer;
                    var bandRc = new RectangleF(x, vungVe.Y, bandW, vungVe.Height);
                    using var shimmer = new LinearGradientBrush(bandRc, Color.Transparent, Color.Transparent, LinearGradientMode.Horizontal)
                    {
                        InterpolationColors = new ColorBlend(3)
                        {
                            Colors = new[]
                            {
                                Color.FromArgb(0, 255, 255, 255),
                                Color.FromArgb(110, 255, 255, 255),
                                Color.FromArgb(0, 255, 255, 255)
                            },
                            Positions = new[] { 0f, 0.5f, 1f }
                        }
                    };
                    g.FillRectangle(shimmer, bandRc);
                }
                g.ResetClip();
            }
            using (var butVien = new Pen(MauVien, 1f))
                g.DrawPath(butVien, nen);
            // 3) Chữ: tối trên nền xám, trắng trên vùng đã chạy
            string chuPhai = _hienPhanTram && !kxd ? $"{(int)MathF.Floor(pct)}%" : string.Empty;
            if (_chuThich.Length == 0 && chuPhai.Length == 0) return;
            float le = rc.Height / 2f;
            var vungChu = new RectangleF(rc.X + le, rc.Y, rc.Width - le * 2f, rc.Height);
            // 3a) Chữ tối: chỉ phần nằm ngoài vùng đã chạy
            using (var vungToi = new Region(nen))
            {
                if (chay != null) vungToi.Exclude(chay);
                g.SetClip(vungToi, CombineMode.Replace);
                VeChu(g, _chuThich, chuPhai, vungChu, MauChuToi);
            }
            // 3b) Chữ trắng: chỉ phần nằm trong vùng đã chạy (và trong nền)
            if (chay != null)
            {
                g.SetClip(nen, CombineMode.Replace);
                g.SetClip(chay, CombineMode.Intersect);
                VeChu(g, _chuThich, chuPhai, vungChu, MauChuSang);
            }
            g.ResetClip();
        }
        private void VeChu(Graphics g, string chuTrai, string chuPhai, RectangleF vung, Color mau)
        {
            using var co = new SolidBrush(mau);
            if (chuTrai.Length > 0) g.DrawString(chuTrai, Font, co, vung, _sfTrai);
            if (chuPhai.Length > 0) g.DrawString(chuPhai, FontDam, co, vung, _sfPhai);
        }
    }
}