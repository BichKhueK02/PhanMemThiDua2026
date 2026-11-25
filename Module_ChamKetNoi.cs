using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Chấm tròn trạng thái kết nối CSDL ở thanh trạng thái (tự vẽ bằng GDI+, không dùng ảnh):
    ///  - Còn kết nối: chấm tròn xanh lá nhấp nháy HIỆN rồi ẨN (chuyển mềm), có quầng sáng nhẹ khi hiện.
    ///  - Mất kết nối (chữ nhãn bắt đầu bằng "Mất kết nối"): chấm đỏ đứng yên.
    /// Vẽ lại TRÊN CÙNG 1 Bitmap rồi Invalidate nhãn => không gán Image mới mỗi nhịp, thanh trạng thái không bị layout lại.
    /// </summary>
    internal static class Module_ChamKetNoi
    {
        private const int ChuKyMs = 1200;   // 1 lần hiện + ẩn
        private static readonly ConditionalWeakTable<ToolStripStatusLabel, BoVe> _daGan = new();
        public static void GanCho(ToolStripStatusLabel? nhan)
        {
            if (nhan == null || _daGan.TryGetValue(nhan, out _)) return;
            try { _daGan.Add(nhan, new BoVe(nhan)); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[ChamKetNoi] " + ex.Message); }
        }
        private sealed class BoVe
        {
            private static readonly Color XanhLa = Color.FromArgb(34, 197, 94);
            private static readonly Color XanhLaDam = Color.FromArgb(21, 128, 61);
            private static readonly Color Do = Color.FromArgb(239, 68, 68);
            private static readonly Color DoDam = Color.FromArgb(185, 28, 28);
            private readonly ToolStripStatusLabel _nhan;
            private readonly System.Windows.Forms.Timer _timer = new() { Interval = 50 };
            private readonly System.Diagnostics.Stopwatch _dongHo = System.Diagnostics.Stopwatch.StartNew();
            private Bitmap? _anh;
            private bool? _daVeMatKetNoi;   // đã vẽ khung tĩnh "mất kết nối" => khỏi vẽ lại
            public BoVe(ToolStripStatusLabel nhan)
            {
                _nhan = nhan;
                _nhan.ImageScaling = ToolStripItemImageScaling.None;   // giữ đúng điểm ảnh, không bị co giãn mờ
                _nhan.ImageAlign = ContentAlignment.MiddleLeft;
                _nhan.TextImageRelation = TextImageRelation.ImageBeforeText;
                TaoAnh();
                VeKhung();
                _timer.Tick += (s, e) => Nhip();
                _timer.Start();
                _nhan.Disposed += (s, e) => { _timer.Stop(); _timer.Dispose(); };
            }
            private float HeSoDpi => Math.Max(1f, (_nhan.Owner?.DeviceDpi ?? 96) / 96f);
            private void TaoAnh()
            {
                int co = (int)Math.Round(16 * HeSoDpi);
                if (_anh != null && _anh.Width == co) return;
                var cu = _anh;
                _anh = new Bitmap(co, co, PixelFormat.Format32bppPArgb);
                _nhan.Image = _anh;   // gán 1 lần (hoặc khi đổi DPI) => chỉ lúc này mới layout lại
                cu?.Dispose();
            }
            private bool MatKetNoi => (_nhan.Text ?? "").StartsWith("Mất kết nối", StringComparison.Ordinal);
            private void Nhip()
            {
                try
                {
                    if (_nhan.IsDisposed || _nhan.Owner == null || _nhan.Owner.IsDisposed) { _timer.Stop(); return; }
                    if (!_nhan.Visible || !_nhan.Owner.Visible) return;
                    var f = _nhan.Owner.FindForm();
                    if (f != null && f.WindowState == FormWindowState.Minimized) return;   // thu nhỏ: khỏi vẽ
                    TaoAnh();
                    bool mat = MatKetNoi;
                    if (mat && _daVeMatKetNoi == true) return;   // chấm đỏ tĩnh đã vẽ rồi
                    VeKhung();
                    _nhan.Invalidate();
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[ChamKetNoi] " + ex.Message); }
            }
            private void VeKhung()
            {
                if (_anh == null) return;
                bool mat = MatKetNoi;
                _daVeMatKetNoi = mat;
                float t = (_dongHo.ElapsedMilliseconds % ChuKyMs) / (float)ChuKyMs;   // 0..1
                // Nhấp nháy HIỆN rồi ẨN: sáng rõ ~45% chu kỳ, tắt hẳn ~35%, chuyển mềm 2 đầu (10% mỗi đầu)
                float hien = mat ? 1f : DoHien(t);
                int co = _anh.Width;
                float tam = co / 2f;
                float rLoi = co * 0.30f;                     // bán kính chấm
                Color mau = mat ? Do : XanhLa;
                Color mauDam = mat ? DoDam : XanhLaDam;
                using var g = Graphics.FromImage(_anh);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);
                if (hien <= 0.01f) return;                   // pha ẩn: để trống
                int A(int goc) => (int)Math.Round(goc * hien);
                // Quầng sáng mềm quanh chấm (chỉ khi đang kết nối)
                if (!mat)
                {
                    float rQuang = co / 2f - 0.5f;
                    using var quang = new GraphicsPath();
                    quang.AddEllipse(tam - rQuang, tam - rQuang, 2 * rQuang, 2 * rQuang);
                    using var brQ = new PathGradientBrush(quang)
                    {
                        CenterColor = Color.FromArgb(A(110), mau),
                        SurroundColors = new[] { Color.FromArgb(0, mau) }
                    };
                    g.FillPath(brQ, quang);
                }
                // Chấm tròn: đổ màu, đốm sáng góc trên-trái cho có khối, viền đậm
                var vung = new RectangleF(tam - rLoi, tam - rLoi, 2 * rLoi, 2 * rLoi);
                using (var duong = new GraphicsPath())
                {
                    duong.AddEllipse(vung);
                    using var br = new PathGradientBrush(duong)
                    {
                        CenterPoint = new PointF(tam - rLoi * 0.3f, tam - rLoi * 0.3f),
                        CenterColor = Color.FromArgb(A(255), Pha(mau, Color.White, 0.45f)),
                        SurroundColors = new[] { Color.FromArgb(A(255), Pha(mauDam, mau, 0.6f)) }
                    };
                    g.FillPath(br, duong);
                }
                using (var vien = new Pen(Color.FromArgb(A(210), mauDam), Math.Max(1f, co / 20f)))
                    g.DrawEllipse(vien, vung);
            }
            // Độ hiện 0..1 theo vị trí trong chu kỳ: lên mềm -> sáng -> xuống mềm -> tắt
            private static float DoHien(float t)
            {
                float x;
                if (t < 0.10f) x = t / 0.10f;
                else if (t < 0.55f) x = 1f;
                else if (t < 0.65f) x = 1f - (t - 0.55f) / 0.10f;
                else x = 0f;
                return x * x * (3f - 2f * x);                // smoothstep cho mượt
            }
            private static Color Pha(Color a, Color b, float t)
            {
                t = Math.Clamp(t, 0f, 1f);
                return Color.FromArgb(
                    (int)Math.Round(a.R + (b.R - a.R) * t),
                    (int)Math.Round(a.G + (b.G - a.G) * t),
                    (int)Math.Round(a.B + (b.B - a.B) * t));
            }
        }
    }
}
