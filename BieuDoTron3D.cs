using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Biểu đồ tròn 3D kiểu web cho Form4 (tab "Biểu đồ minh họa"): mặt nghiêng + thành dày, đổ bóng mềm, bóng bề mặt,
    /// màu gradient hiện đại, hiệu ứng "mở ra" khi có dữ liệu mới, rê chuột lên lát/dòng chú thích thì lát đó nổi lên.
    ///
    /// - Là một Panel tự vẽ (double buffer), KHÔNG đọc CSDL, KHÔNG biết logic nghiệp vụ: lấy số liệu qua <see cref="NguonDuLieu"/>
    ///   (Form4 trả về pieData). Form4 vẫn gọi piePanel.Invalidate() như cũ khi số liệu đổi -> biểu đồ tự nhận ra và chạy hiệu ứng.
    /// - Bộ hẹn giờ chỉ chạy trong lúc đang có hiệu ứng (khoảng 1 giây) rồi tắt hẳn: không tốn CPU khi đứng yên.
    /// - Không tạo .resx.
    /// </summary>
    internal sealed class BieuDoTron3D : Panel
    {
        /// <summary>Trả về số liệu hiện tại (nhãn -> số lượng). Được hỏi mỗi lần vẽ; chỉ khi nội dung đổi mới chạy lại hiệu ứng.</summary>
        public Func<IReadOnlyDictionary<string, int>?>? NguonDuLieu { get; set; }
        /// <summary>
        /// (MỚI) Nguồn lát đầy đủ: mỗi lát có diện tích, chữ chú thích cột phải ("a/b = x%"), chữ trên lát và màu cố định.
        /// Nếu trả về khác null thì được ưu tiên hơn <see cref="NguonDuLieu"/> (giữ nguyên cho nơi khác còn dùng kiểu cũ).
        /// </summary>
        public Func<IReadOnlyList<LatBieuDo>?>? NguonLat { get; set; }
        /// <summary>(MỚI) Dòng ghi chú nhỏ dưới chú thích (chỉ hiện khi đang dùng <see cref="NguonLat"/>).</summary>
        public string GhiChuChuThich { get; set; } = "";
        // Bảng màu: giữ thứ tự 5 nhóm như bản cũ (xanh lá, hồng, xanh dương, cam, xám) nhưng tông hiện đại
        private static readonly Color[] Mau =
        {
            Color.FromArgb(16, 185, 129),   // emerald
            Color.FromArgb(244, 114, 182),  // pink
            Color.FromArgb(56, 148, 248),   // blue
            Color.FromArgb(251, 146, 60),   // orange
            Color.FromArgb(148, 163, 184),  // slate
            Color.FromArgb(167, 139, 250),  // violet (dự phòng)
        };
        private static readonly Color MauChu = Color.FromArgb(31, 41, 55);
        private static readonly Color MauChuPhu = Color.FromArgb(100, 116, 139);
        private const float K = 0.56f;          // độ nghiêng: chiều cao elip / chiều rộng elip
        private const double ThoiGianMoMs = 800; // thời gian hiệu ứng "mở ra"
        private string _chuKy = "";
        private string[] _nhan = Array.Empty<string>();
        private double[] _giaTri = Array.Empty<double>();
        private double _tong;
        private bool _laLat;                                      // true = đang dùng NguonLat
        private string[] _chuThich = Array.Empty<string>();       // chữ cột phải của chú thích (chế độ NguonLat)
        private string[] _nhanLat = Array.Empty<string>();        // chữ trên lát (chế độ NguonLat)
        private int[] _mauIdx = Array.Empty<int>();               // chỉ số màu cố định theo loại (chế độ NguonLat)
        private float[] _no = Array.Empty<float>();   // mức "nổi lên" của từng lát: 0..1
        private int _hover = -1;
        private double _tienDo = 1;                   // hiệu ứng mở: 0..1
        private readonly Stopwatch _dongHo = new();
        private readonly System.Windows.Forms.Timer _timer = new() { Interval = 15 };
        // Hình học lần vẽ gần nhất (dùng để dò chuột)
        private float _tamX, _tamY, _a, _b, _t;
        private readonly List<RectangleF> _hangChuThich = new();
        private Font? _fNhan, _fDam, _fSo, _fTong, _fGhiChu;
        public BieuDoTron3D()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            BackColor = Color.White;
            _timer.Tick += (s, e) => Nhip();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Dispose();
                _fNhan?.Dispose(); _fDam?.Dispose(); _fSo?.Dispose(); _fTong?.Dispose(); _fGhiChu?.Dispose();
            }
            base.Dispose(disposing);
        }
        // ------------------------------------------------------------------ DỮ LIỆU + HIỆU ỨNG
        private void DongBoDuLieu()
        {
            var lat = NguonLat?.Invoke();
            var src = lat == null ? NguonDuLieu?.Invoke() : null;
            string chuKy = lat != null
                ? "L|" + string.Join("|", lat.Select(x => $"{x.Nhan}={x.GiaTri}~{x.ChuThich}~{x.NhanTrenLat}~{x.MauIndex}")) + "|" + GhiChuChuThich
                : (src == null ? "" : string.Join("|", src.Select(kv => kv.Key + "=" + kv.Value)));
            if (chuKy == _chuKy) return;
            _chuKy = chuKy;
            _laLat = lat != null;
            int soLat = lat?.Count ?? src?.Count ?? 0;
            if (soLat == 0)
            {
                _nhan = Array.Empty<string>(); _giaTri = Array.Empty<double>(); _tong = 0; _no = Array.Empty<float>(); _hover = -1;
                _chuThich = Array.Empty<string>(); _nhanLat = Array.Empty<string>(); _mauIdx = Array.Empty<int>();
                return;
            }
            if (lat != null)
            {
                _nhan = lat.Select(x => x.Nhan).ToArray();
                _giaTri = lat.Select(x => Math.Max(0, x.GiaTri)).ToArray();
                _chuThich = lat.Select(x => x.ChuThich).ToArray();
                _nhanLat = lat.Select(x => x.NhanTrenLat).ToArray();
                _mauIdx = lat.Select(x => x.MauIndex).ToArray();
            }
            else
            {
                _nhan = src!.Keys.ToArray();
                _giaTri = src.Values.Select(v => (double)Math.Max(0, v)).ToArray();
                _chuThich = Array.Empty<string>(); _nhanLat = Array.Empty<string>(); _mauIdx = Array.Empty<int>();
            }
            _tong = _giaTri.Sum();
            if (_no.Length != _nhan.Length) _no = new float[_nhan.Length];
            if (_hover >= _nhan.Length) _hover = -1;
            if (_tong > 0 && Visible && IsHandleCreated)
            {
                _tienDo = 0;
                _dongHo.Restart();
                _timer.Start();
            }
            else _tienDo = 1;
        }
        private void Nhip()
        {
            bool con = false;
            if (_tienDo < 1)
            {
                _tienDo = Math.Min(1, _dongHo.ElapsedMilliseconds / ThoiGianMoMs);
                if (_tienDo < 1) con = true;
            }
            for (int i = 0; i < _no.Length; i++)
            {
                float dich = i == _hover ? 1f : 0f;
                float d = dich - _no[i];
                if (Math.Abs(d) < 0.03f) _no[i] = dich;
                else { _no[i] += Math.Sign(d) * 0.14f; con = true; }
            }
            if (!IsHandleCreated || IsDisposed) { _timer.Stop(); return; }
            Invalidate();
            if (!con) _timer.Stop();
        }
        private static double Ease(double p) => 1 - Math.Pow(1 - p, 3);
        // Màu của lát i: chế độ NguonLat dùng màu cố định theo loại (ẩn Loại 4 không làm "Không PL" đổi màu)
        private Color MauLat(int i) => Mau[(_laLat && i < _mauIdx.Length ? _mauIdx[i] : i) % Mau.Length];
        // ------------------------------------------------------------------ VẼ
        protected override void OnPaint(PaintEventArgs e)
        {
            DongBoDuLieu();
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            _hangChuThich.Clear();
            _a = 0;
            if (_nhan.Length == 0) return;
            _fNhan ??= new Font(Module_HeThong.TenFontHeThong, 9.5f, FontStyle.Regular, GraphicsUnit.Point);
            _fDam ??= new Font(Module_HeThong.TenFontHeThong, 9.5f, FontStyle.Bold, GraphicsUnit.Point);
            _fSo ??= new Font(Module_HeThong.TenFontHeThong, 9f, FontStyle.Bold, GraphicsUnit.Point);
            _fTong ??= new Font(Module_HeThong.TenFontHeThong, 10f, FontStyle.Bold, GraphicsUnit.Point);
            _fGhiChu ??= new Font(Module_HeThong.TenFontHeThong, 8.25f, FontStyle.Italic, GraphicsUnit.Point);
            float s = DeviceDpi / 96f;
            if (_tong <= 0)
            {
                using var sf0 = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using var br0 = new SolidBrush(MauChuPhu);
                g.DrawString("Không có dữ liệu để vẽ.", _fDam, br0, ClientRectangle, sf0);
                return;
            }
            // ---- Bố cục: chú thích bên phải (khung rộng) hoặc bên dưới (khung hẹp, như tab hiện tại)
            int n = _nhan.Length;
            float pad = 14 * s, rh = 28 * s, tieuDe = 30 * s;
            float ghiChuH = _laLat && GhiChuChuThich.Length > 0 ? 38 * s : 0;   // chỗ cho dòng ghi chú dưới chú thích
            float legendH = tieuDe + n * rh + ghiChuH;
            bool rong = Width > Height * 1.3f;
            RectangleF legend, chart;
            if (rong)
            {
                float lw = Math.Clamp(Width * 0.40f, 180 * s, 270 * s);
                legend = new RectangleF(Width - pad - lw, Math.Max(pad, (Height - legendH) / 2f), lw, legendH);
                chart = new RectangleF(pad, pad, Width - lw - 3 * pad, Height - 2 * pad);
            }
            else
            {
                legend = new RectangleF(pad, Math.Max(pad, Height - pad - legendH), Width - 2 * pad, legendH);
                chart = new RectangleF(pad, pad, Width - 2 * pad, Math.Max(0, legend.Y - 2 * pad));
            }
            VeChuThich(g, legend, s, rh, tieuDe);
            if (chart.Width < 60 * s || chart.Height < 60 * s) return;
            // ---- Hình học elip
            float ex = 9 * s;
            float a = Math.Min(chart.Width / 2f - ex, chart.Height / (2 * K + 0.21f + 0.1f));
            if (a < 24 * s) return;
            float b = a * K;
            float T = Math.Max(8 * s, 0.15f * a);
            float cx = chart.X + chart.Width / 2f;
            float cy = chart.Y + (chart.Height - (2 * b + T)) / 2f + b;
            _tamX = cx; _tamY = cy; _a = a; _b = b; _t = T;
            // ---- Góc từng lát (bắt đầu ở 12 giờ, theo chiều kim đồng hồ), co lại theo hiệu ứng mở
            double mo = Ease(_tienDo);
            var bat = new float[n];
            var quet = new float[n];
            float goc = -90f;
            for (int i = 0; i < n; i++)
            {
                bat[i] = goc;
                quet[i] = (float)(_giaTri[i] / _tong * 360.0 * mo);
                goc += quet[i];
            }
            VeBong(g, cx, cy, a, b, T, s);
            // Vẽ lát phía sau trước, phía trước sau
            var thuTu = Enumerable.Range(0, n).Where(i => quet[i] > 0.01f)
                .OrderBy(i => Math.Sin((bat[i] + quet[i] / 2f) * Math.PI / 180.0)).ToList();
            foreach (int i in thuTu)
                VeLat(g, i, bat[i], quet[i], cx, cy, a, b, T, ex, s);
            // Bóng bề mặt (gloss) nhẹ phủ nửa trên
            var gloss = new RectangleF(cx - a, cy - b, 2 * a, 2 * b);
            using (var gp = new GraphicsPath())
            {
                gp.AddEllipse(gloss);
                var oldClip = g.Clip;
                g.SetClip(gp, CombineMode.Intersect);
                using var gb = new LinearGradientBrush(gloss, Color.FromArgb(70, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 90f);
                g.FillEllipse(gb, gloss);
                g.Clip = oldClip;
            }
            // Phần trăm trên lát (chỉ khi hiệu ứng mở xong, lát đủ lớn)
            if (_tienDo >= 1)
            {
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                for (int i = 0; i < n; i++)
                {
                    double ty = _giaTri[i] / _tong;
                    if (ty < 0.07) continue;
                    double mid = (bat[i] + quet[i] / 2f) * Math.PI / 180.0;
                    float ox = (float)(Math.Cos(mid) * ex * _no[i]), oy = (float)(Math.Sin(mid) * ex * K * _no[i]);
                    float px = cx + ox + (float)(0.60 * a * Math.Cos(mid));
                    float py = cy + oy + (float)(0.60 * b * Math.Sin(mid));
                    string txt = _laLat && i < _nhanLat.Length ? _nhanLat[i] : Math.Round(ty * 100) + "%";
                    using var sh = new SolidBrush(Color.FromArgb(90, 15, 23, 42));
                    g.DrawString(txt, _fSo, sh, px + 1, py + 1, sf);
                    g.DrawString(txt, _fSo, Brushes.White, px, py, sf);
                }
            }
        }
        // Bóng đổ mềm dưới đĩa: vài elip mờ chồng nhau
        private static void VeBong(Graphics g, float cx, float cy, float a, float b, float T, float s)
        {
            for (int k = 0; k < 7; k++)
            {
                float grow = k * 3.2f * s;
                using var br = new SolidBrush(Color.FromArgb(13, 15, 23, 42));
                g.FillEllipse(br, cx - a - grow * 0.8f, cy + T * 0.9f - b * 0.55f - grow * 0.1f, 2 * a + grow * 1.6f, 1.15f * b + grow * 0.8f);
            }
        }
        private void VeLat(Graphics g, int i, float batDau, float quet, float cx, float cy, float a, float b, float T, float ex, float s)
        {
            double t0 = batDau * Math.PI / 180.0, t1 = (batDau + quet) * Math.PI / 180.0, mid = (t0 + t1) / 2;
            float no = _no[i];
            float X = cx + (float)(Math.Cos(mid) * ex * no);
            float Y = cy + (float)(Math.Sin(mid) * ex * K * no);
            int buoc = Math.Max(2, (int)Math.Ceiling(quet / 2.0));
            var cung = new PointF[buoc + 1];
            for (int j = 0; j <= buoc; j++)
            {
                double t = t0 + (t1 - t0) * j / buoc;
                cung[j] = new PointF(X + a * (float)Math.Cos(t), Y + b * (float)Math.Sin(t));
            }
            Color goc = MauLat(i);
            Color thanh = Toi(goc, 0.72f);
            Color vach = Toi(goc, 0.60f);
            bool tron = quet >= 359.5f;
            // Hai mặt cắt hướng tâm (chỉ thấy khi lát nổi lên)
            if (!tron && no > 0.02f)
            {
                using var bv = new SolidBrush(vach);
                foreach (var p in new[] { cung[0], cung[buoc] })
                    g.FillPolygon(bv, new[] { new PointF(X, Y), p, new PointF(p.X, p.Y + T), new PointF(X, Y + T) });
            }
            // Thành cong: chỉ phần hướng về người xem (nửa dưới của elip)
            using (var bt = new SolidBrush(thanh))
            {
                List<PointF>? run = null;
                for (int j = 0; j <= buoc; j++)
                {
                    double t = t0 + (t1 - t0) * j / buoc;
                    if (Math.Sin(t) >= -0.0001)
                        (run ??= new List<PointF>()).Add(cung[j]);
                    else if (run != null) { VeThanh(g, bt, run, T); run = null; }
                }
                if (run != null) VeThanh(g, bt, run, T);
            }
            // Mặt trên: gradient sáng -> màu gốc
            PointF[] mat;
            if (tron) mat = cung;
            else
            {
                mat = new PointF[buoc + 2];
                mat[0] = new PointF(X, Y);
                Array.Copy(cung, 0, mat, 1, cung.Length);
            }
            var khung = new RectangleF(X - a, Y - b, 2 * a, 2 * b);
            Color sang = Sang(goc, i == _hover ? 0.42f : 0.30f);
            using (var gb = new LinearGradientBrush(khung, sang, goc, 60f))
                g.FillPolygon(gb, mat);
            using var pen = new Pen(Color.White, 1.6f * s) { LineJoin = LineJoin.Round };
            g.DrawPolygon(pen, mat);
        }
        private static void VeThanh(Graphics g, Brush bt, List<PointF> run, float T)
        {
            if (run.Count < 2) return;
            var poly = new List<PointF>(run.Count * 2);
            poly.AddRange(run);
            for (int k = run.Count - 1; k >= 0; k--) poly.Add(new PointF(run[k].X, run[k].Y + T));
            g.FillPolygon(bt, poly.ToArray());
        }
        // ------------------------------------------------------------------ CHÚ THÍCH
        private void VeChuThich(Graphics g, RectangleF r, float s, float rh, float tieuDe)
        {
            using var chuPhu = new SolidBrush(MauChuPhu);
            using var chu = new SolidBrush(MauChu);
            using var sfTrai = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            using var sfPhai = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
            g.DrawString("Tổng cộng", _fNhan!, chuPhu, new RectangleF(r.X + 4 * s, r.Y, r.Width / 2f, 30 * s), sfTrai);
            g.DrawString(((long)_tong).ToString(), _fTong!, chu, new RectangleF(r.X, r.Y, r.Width - 4 * s, 30 * s), sfPhai);
            using (var pl = new Pen(Color.FromArgb(235, 238, 243)))
                g.DrawLine(pl, r.X, r.Y + tieuDe - 2 * s, r.Right, r.Y + tieuDe - 2 * s);
            // Cột số liệu: chế độ NguonLat có chữ dài ("4800/6000 = 80,00%") nên đo bề rộng thật; chế độ cũ giữ 92
            float wSo = 92 * s;
            if (_laLat)
            {
                float dai = 0;
                foreach (string ct in _chuThich) dai = Math.Max(dai, g.MeasureString(ct, _fDam!).Width);
                wSo = Math.Clamp(dai + 10 * s, 92 * s, Math.Max(92 * s, r.Width * 0.62f));
            }
            for (int i = 0; i < _nhan.Length; i++)
            {
                var hang = new RectangleF(r.X, r.Y + tieuDe + i * rh, r.Width, rh - 2 * s);
                _hangChuThich.Add(hang);
                if (i == _hover)
                {
                    using var gp = HinhBoTron(hang, 7 * s);
                    using var bn = new SolidBrush(Color.FromArgb(241, 245, 249));
                    g.FillPath(bn, gp);
                }
                float d = 12 * s;
                var cham = new RectangleF(hang.X + 8 * s, hang.Y + (hang.Height - d) / 2f, d, d);
                using (var gp = HinhBoTron(cham, 3.5f * s))
                using (var bc = new SolidBrush(MauLat(i)))
                    g.FillPath(bc, gp);
                double ty = _giaTri[i] / _tong;
                string so = _laLat && i < _chuThich.Length ? _chuThich[i] : $"{(long)_giaTri[i]}  ({Math.Round(ty * 100)}%)";
                g.DrawString(_nhan[i], i == _hover ? _fDam! : _fNhan!, chu,
                    new RectangleF(hang.X + 28 * s, hang.Y, Math.Max(10, hang.Width - 28 * s - wSo - 4 * s), hang.Height), sfTrai);
                g.DrawString(so, _fDam!, chu, new RectangleF(hang.Right - wSo - 6 * s, hang.Y, wSo, hang.Height), sfPhai);
            }
            if (_laLat && GhiChuChuThich.Length > 0)
            {
                using var sfGc = new StringFormat { Trimming = StringTrimming.EllipsisWord };
                g.DrawString(GhiChuChuThich, _fGhiChu!, chuPhu,
                    new RectangleF(r.X + 4 * s, r.Y + tieuDe + _nhan.Length * rh + 2 * s, r.Width - 8 * s, 36 * s), sfGc);
            }
        }
        private static GraphicsPath HinhBoTron(RectangleF r, float bk)
        {
            float d = Math.Min(bk * 2, Math.Min(r.Width, r.Height));
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
        private static Color Toi(Color c, float f) => Color.FromArgb(c.A, (int)(c.R * f), (int)(c.G * f), (int)(c.B * f));
        private static Color Sang(Color c, float f) => Color.FromArgb(c.A, (int)(c.R + (255 - c.R) * f), (int)(c.G + (255 - c.G) * f), (int)(c.B + (255 - c.B) * f));
        // ------------------------------------------------------------------ CHUỘT
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            DatHover(DoLat(e.Location));
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            DatHover(-1);
        }
        private void DatHover(int h)
        {
            if (h == _hover) return;
            _hover = h;
            Cursor = h >= 0 ? Cursors.Hand : Cursors.Default;
            if (_no.Length > 0) _timer.Start();
            Invalidate();
        }
        private int DoLat(Point p)
        {
            if (_nhan.Length == 0 || _tong <= 0) return -1;
            for (int i = 0; i < _hangChuThich.Count; i++)
                if (_hangChuThich[i].Contains(p)) return i;
            if (_a <= 0 || _tienDo < 1) return -1;
            // Điểm nằm trong elip (kể cả phần thành dày phía dưới)
            double dx = (p.X - _tamX) / _a;
            double dy = (p.Y - (_tamY + _t / 2.0)) / (_b + _t / 2.0);
            if (dx * dx + dy * dy > 1) return -1;
            double goc = Math.Atan2((p.Y - _tamY) / _b, dx) * 180.0 / Math.PI;   // 0 = 3 giờ, tăng theo chiều kim đồng hồ
            double rel = (goc + 90 + 720) % 360;                                   // 0 = 12 giờ
            double luy = 0;
            for (int i = 0; i < _giaTri.Length; i++)
            {
                luy += _giaTri[i] / _tong * 360.0;
                if (rel < luy) return _giaTri[i] > 0 ? i : -1;
            }
            return -1;
        }
    }
}
