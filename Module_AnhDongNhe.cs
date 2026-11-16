using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Làm "nhẹ" PictureBox đang chứa ảnh GIF động nặng (vd. pictureBox3 của Form2: 139 khung 640x480
    /// nhưng chỉ hiển thị trong ô 82x35).
    ///
    /// Vấn đề gốc: WinForms để ImageAnimator tự phát GIF -> mỗi khung (~25 lần/giây) phải co ảnh 640x480
    /// xuống 82x35 rồi vẽ lại, suốt thời gian chạy phần mềm => giao diện chính "giật, vẽ liên tục".
    ///
    /// Cách làm: giữ NGUYÊN hiệu ứng động, nhưng
    ///   1) co sẵn từng khung về đúng cỡ ô hiển thị (làm ở luồng nền, không chặn giao diện);
    ///   2) giảm số khung hình/giây (mặc định 12) mà vẫn giữ đúng tốc độ phát;
    ///   3) CHỈ chạy khi ô đang nhìn thấy, cửa sổ không thu nhỏ và ứng dụng đang được dùng;
    ///   4) giải phóng ảnh GIF gốc (~vài chục MB bộ nhớ khi giải mã).
    /// Dùng 1 dòng sau InitializeComponent():  Module_AnhDongNhe.ApDung(pictureBox3);
    /// </summary>
    internal static class Module_AnhDongNhe
    {
        /// <summary>true: giữ hiệu ứng động (nhẹ). false: chỉ hiện khung đầu, hoàn toàn không vẽ lại.</summary>
        public static bool GiuHieuUngDong { get; set; } = true;
        public static void ApDung(PictureBox? pb, int khungMoiGiay = 12)
        {
            if (pb == null || pb.IsDisposed) return;
            Image? goc = pb.Image;
            if (goc == null) return;
            bool dong;
            try { dong = ImageAnimator.CanAnimate(goc); }
            catch { return; }
            if (!dong) return;
            try
            {
                // Kích thước đích = 2x ô hiển thị (đủ nét cho màn hình DPI cao, vẫn rất nhỏ)
                int tw = Math.Max(16, pb.Width * 2), th = Math.Max(16, pb.Height * 2);
                int soKhung = goc.GetFrameCount(FrameDimension.Time);
                int doTreMs = DocDoTreMs(goc);
                int buoc = Math.Max(1, (int)Math.Round(1000.0 / Math.Max(1, doTreMs) / Math.Max(1, khungMoiGiay)));
                int chuKyMs = Math.Max(40, doTreMs * buoc);
                // Khung đầu: vẽ ngay (đồng bộ, vài ms) để ô có hình ngay, không chờ luồng nền
                goc.SelectActiveFrame(FrameDimension.Time, 0);
                var khungDau = CoKhung(goc, tw, th);
                pb.Image = khungDau;   // thay ảnh động bằng ảnh tĩnh -> WinForms ngừng tự phát GIF
                if (!GiuHieuUngDong || soKhung <= 1)
                {
                    goc.Dispose();
                    return;
                }
                var ban = (Image)goc.Clone();
                var khung = new List<Bitmap>(Math.Min(soKhung, 128)) { khungDau };
                var trangThai = new TrangThai(pb, khung, chuKyMs);
                pb.Disposed += (s, e) => trangThai.Huy();
                _ = Task.Run(() =>
                {
                    try
                    {
                        for (int i = buoc; i < soKhung && khung.Count < 128; i += buoc)
                        {
                            if (trangThai.DaHuy) break;
                            ban.SelectActiveFrame(FrameDimension.Time, i);
                            khung.Add(CoKhung(ban, tw, th));
                        }
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[AnhDongNhe] " + ex.Message); }
                    finally
                    {
                        try { ban.Dispose(); } catch { }
                        try { goc.Dispose(); } catch { }
                    }
                    if (trangThai.DaHuy || pb.IsDisposed) return;
                    try
                    {
                        if (pb.IsHandleCreated) pb.BeginInvoke(new Action(trangThai.BatDau));
                        else pb.HandleCreated += (s, e) => trangThai.BatDau();
                    }
                    catch (Exception) { /* ô đã đóng */ }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[AnhDongNhe] " + ex.Message);
            }
        }
        // Độ trễ mỗi khung (đơn vị 1/100 giây trong thẻ 0x5100), đổi sang ms
        private static int DocDoTreMs(Image gif)
        {
            try
            {
                var p = gif.GetPropertyItem(0x5100);
                if (p?.Value != null && p.Value.Length >= 4)
                {
                    int cs = BitConverter.ToInt32(p.Value, 0);
                    if (cs > 0) return cs * 10;
                }
            }
            catch { }
            return 40;
        }
        private static Bitmap CoKhung(Image nguon, int w, int h)
        {
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
            using var g = Graphics.FromImage(bmp);
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.CompositingQuality = CompositingQuality.HighSpeed;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(nguon, new Rectangle(0, 0, w, h));
            return bmp;
        }
        private sealed class TrangThai
        {
            private readonly PictureBox _pb;
            private readonly List<Bitmap> _khung;
            private readonly int _chuKyMs;
            private System.Windows.Forms.Timer? _timer;
            private int _chiSo;
            private volatile bool _daHuy;
            public bool DaHuy => _daHuy;
            public TrangThai(PictureBox pb, List<Bitmap> khung, int chuKyMs)
            {
                _pb = pb; _khung = khung; _chuKyMs = chuKyMs;
            }
            public void BatDau()
            {
                if (_daHuy || _pb.IsDisposed || _khung.Count < 2) return;
                _timer = new System.Windows.Forms.Timer { Interval = _chuKyMs };
                _timer.Tick += (s, e) => Chay();
                _timer.Start();
            }
            private void Chay()
            {
                if (_daHuy || _pb.IsDisposed) { Huy(); return; }
                // Tạm dừng khi không ai nhìn thấy: ô bị ẩn, cửa sổ thu nhỏ, hoặc ứng dụng không còn được dùng
                if (!_pb.IsHandleCreated || !_pb.Visible) return;
                var f = _pb.FindForm();
                if (f == null || f.WindowState == FormWindowState.Minimized) return;
                if (Form.ActiveForm == null) return;
                _chiSo = (_chiSo + 1) % _khung.Count;
                _pb.Image = _khung[_chiSo];   // ảnh đã co sẵn đúng cỡ -> vẽ rất nhẹ
            }
            public void Huy()
            {
                _daHuy = true;
                var t = _timer; _timer = null;
                if (t != null) { t.Stop(); t.Dispose(); }
            }
        }
    }
}