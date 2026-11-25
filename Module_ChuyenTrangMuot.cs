using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Hiệu ứng chuyển trang mờ dần kiểu web cho Form2 (kiểu giao diện "Hiện đại").
    /// Vì sao cần: chuyển trang = ẩn trang cũ + hiện trang mới + vẽ lại cả trang (đo 23/11/2026: 50–125 ms, chủ yếu vẽ lưới
    /// Krypton) => người dùng thấy "đứng hình rồi nhảy cóc". Không làm nhanh hơn được nhiều, nhưng có thể CHE đi:
    ///  1) BatDau: chụp vùng nội dung đang hiển thị (BitBlt màn hình ~vài ms) và phủ lên đúng chỗ bằng 1 cửa sổ "layered"
    ///     (không nhận chuột, không lấy focus, thuộc Form2);
    ///  2) trang mới dựng + vẽ PHÍA SAU lớp phủ (không thấy cảnh vẽ dở);
    ///  3) KetThuc: lớp phủ mờ dần ~160 ms (chỉ đổi độ trong suốt bằng UpdateLayeredWindow — DWM ghép hình, gần như không tốn CPU)
    ///     rồi tự hủy. Quá 600 ms (luồng giao diện bận) thì hủy ngay, không bao giờ che trang mới lâu.
    /// Mọi lỗi đều bỏ qua => chuyển trang như cũ.
    /// </summary>
    internal sealed class LopChuyenTrang : Form
    {
        private int ThoiGianMoMs = 160;   // ảnh trang CŨ: mờ 160 ms; ảnh trang ĐÍCH (giống trang thật): mờ ngắn hơn
        private const int ToiDaMs = 600;
        private Bitmap? _anh;   // ảnh của NGƯỜI GỌI (chỉ mượn để tạo HBITMAP, không hủy)
        private readonly System.Windows.Forms.Timer _timer = new() { Interval = 15 };
        private readonly Stopwatch _dong = new();
        private readonly Stopwatch _tong = Stopwatch.StartNew();
        private IntPtr _memDc, _hBmp, _cu;
        // Chẩn đoán (đọc bằng công cụ kiểm thử): số lớp phủ đã tạo / đã hủy, thời điểm hủy gần nhất (ms kể từ lúc tạo)
        internal static int DemTao, DemHuy;
        /// <summary>CHỈ để kiểm thử: tắt hẳn hiệu ứng (so sánh có/không).</summary>
        internal static bool TatDeKiemThu;
        internal static string ViTriCuoi = "";   // chẩn đoán: vị trí mong muốn / thực tế của lớp phủ
        internal static long MsTonTaiCuoi;
        private Rectangle _vung;
        /// <summary>
        /// Chụp vùng nội dung đang hiển thị (BitBlt màn hình, vài ms). Trả null nếu không nên/không thể
        /// (form ẩn, thu nhỏ, không phải cửa sổ đang được dùng...). Người gọi giữ và tự hủy ảnh.
        /// </summary>
        public static Bitmap? ChupVung(Control vungNoiDung, out Rectangle vungManHinh)
        {
            vungManHinh = Rectangle.Empty;
            if (TatDeKiemThu) return null;
            try
            {
                var f = vungNoiDung.FindForm();
                if (f == null || !f.Visible || f.WindowState == FormWindowState.Minimized || !vungNoiDung.IsHandleCreated) return null;
                if (!ReferenceEquals(Form.ActiveForm, f)) return null;             // chỉ khi người dùng đang thao tác trên Form2
                if (!ToaDoManHinhTinCay(f)) return null;                           // tọa độ bị Windows "ảo hóa DPI" => chụp lệch chỗ
                Rectangle r = vungNoiDung.RectangleToScreen(vungNoiDung.ClientRectangle);
                r.Intersect(Screen.FromControl(f).Bounds);
                if (r.Width < 40 || r.Height < 40) return null;
                var anh = new Bitmap(r.Width, r.Height, PixelFormat.Format32bppRgb);
                using (var g = Graphics.FromImage(anh)) g.CopyFromScreen(r.Location, Point.Empty, r.Size);
                vungManHinh = r;
                return anh;
            }
            catch (Exception ex) { Debug.WriteLine("[ChuyenTrangMuot] chụp: " + ex.Message); return null; }
        }
        /// <summary>
        /// Phủ ảnh <paramref name="anh"/> lên đúng vùng màn hình (hiện NGAY, DWM ghép hình — không chờ luồng giao diện).
        /// Lớp phủ chép ảnh sang bộ nhớ riêng => người gọi vẫn giữ/hủy ảnh của mình bình thường.
        /// </summary>
        public static LopChuyenTrang? BatDau(Form chu, Bitmap anh, Rectangle vungManHinh, int thoiGianMoMs = 160)
        {
            try
            {
                if (anh.Width != vungManHinh.Width || anh.Height != vungManHinh.Height) return null;
                var lop = new LopChuyenTrang(anh, vungManHinh) { Owner = chu, ThoiGianMoMs = Math.Clamp(thoiGianMoMs, 40, 400) };
                if (!lop.Hien()) { lop.Dispose(); return null; }
                DemTao++;
                return lop;
            }
            catch (Exception ex) { Debug.WriteLine("[ChuyenTrangMuot] " + ex.Message); return null; }
        }
        private LopChuyenTrang(Bitmap anh, Rectangle vung)
        {
            _anh = anh;
            _vung = vung;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Bounds = vung;
            _timer.Tick += (s, e) => BuocMo();
        }
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // layered (ghép hình bằng DWM) | không hiện Alt+Tab | không lấy focus | chuột xuyên qua
                cp.ExStyle |= 0x00080000 | 0x00000080 | 0x08000000 | 0x00000020;
                return cp;
            }
        }
        protected override bool ShowWithoutActivation => true;
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e) { }
        private bool Hien()
        {
            _ = Handle;   // tạo cửa sổ (chưa hiện)
            _memDc = CreateCompatibleDC(IntPtr.Zero);
            _hBmp = _anh!.GetHbitmap();
            _cu = SelectObject(_memDc, _hBmp);
            _anh = null;                                            // đã chép sang HBITMAP riêng: không giữ ảnh của người gọi
            if (!CapNhat(255)) return false;                        // có hình TRƯỚC khi hiện => không chớp
            ShowWindow(Handle, 4 /*SW_SHOWNOACTIVATE*/);
            GetWindowRect(Handle, out var thuc);
            var rThuc = Rectangle.FromLTRB(thuc.L, thuc.T, thuc.R, thuc.B);
            ViTriCuoi = $"muốn {_vung} | thực {rThuc} | Bounds {Bounds}";
            // Lớp phủ không nằm ĐÚNG vùng nội dung (lệch DPI giữa các màn hình...) => gỡ ngay: thà chuyển trang thường còn hơn
            // phủ một mảng ảnh lệch chỗ lên thanh menu / nội dung (người dùng thấy menu trái "thòi ra thụt vào").
            if (rThuc != _vung) { ShowWindow(Handle, 0 /*SW_HIDE*/); return false; }
            return true;
        }
        /// <summary>
        /// true khi tọa độ màn hình của form là px THẬT: luồng chạy ở chế độ Per-Monitor và DPI của cửa sổ = DPI màn hình chứa nó.
        /// Sai một trong hai (đổi tỷ lệ 125% -> 100% chưa đăng xuất, exe bị đặt "Override high DPI scaling" trong Compatibility,
        /// cửa sổ đang vắt giữa 2 màn hình khác tỷ lệ) thì Windows co giãn tọa độ => ảnh chụp và lớp phủ lệch khỏi vùng nội dung.
        /// Máy cũ không có các API này => giữ hành vi cũ.
        /// </summary>
        internal static bool ToaDoManHinhTinCay(Form f)
        {
            try
            {
                if (GetAwarenessFromDpiAwarenessContext(GetThreadDpiAwarenessContext()) != 2 /*DPI_AWARENESS_PER_MONITOR_AWARE*/) return false;
                uint dpiCuaSo = GetDpiForWindow(f.Handle);
                IntPtr man = MonitorFromWindow(f.Handle, 2 /*MONITOR_DEFAULTTONEAREST*/);
                if (dpiCuaSo == 0 || man == IntPtr.Zero || GetDpiForMonitor(man, 0 /*MDT_EFFECTIVE_DPI*/, out uint dx, out _) != 0) return true;
                return dx == dpiCuaSo && f.DeviceDpi == (int)dpiCuaSo;
            }
            catch (EntryPointNotFoundException) { return true; }
            catch (DllNotFoundException) { return true; }
        }
        [DllImport("user32.dll")] private static extern IntPtr GetThreadDpiAwarenessContext();
        [DllImport("user32.dll")] private static extern int GetAwarenessFromDpiAwarenessContext(IntPtr ctx);
        [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
        [DllImport("shcore.dll")] private static extern int GetDpiForMonitor(IntPtr hmon, int type, out uint dpiX, out uint dpiY);
        private bool CapNhat(int alpha)
        {
            var dst = new DiemNative { X = _vung.X, Y = _vung.Y };
            var size = new CoNative { Cx = _vung.Width, Cy = _vung.Height };
            var src = new DiemNative();
            var blend = new TronNative { BlendOp = 0, BlendFlags = 0, SourceConstantAlpha = (byte)Math.Clamp(alpha, 0, 255), AlphaFormat = 0 };
            IntPtr h = _hwnd != IntPtr.Zero ? _hwnd : Handle;   // luồng làm mờ dùng hwnd đã lưu (không gọi Control.Handle khác luồng)
            return UpdateLayeredWindow(h, IntPtr.Zero, ref dst, ref size, _memDc, ref src, 0, ref blend, 2 /*ULW_ALPHA*/);
        }
        /// <summary>Trang mới đã dựng + vẽ xong phía sau: bắt đầu mờ dần rồi tự hủy.</summary>
        public void KetThuc()
        {
            if (IsDisposed) return;
            if (_tong.ElapsedMilliseconds > ToiDaMs) { Huy(); return; }
            _hwnd = Handle;
            // Làm mờ trên LUỒNG RIÊNG: ngay sau khi chuyển, trang mới thường nạp dữ liệu làm luồng giao diện bận => bộ đếm của
            // luồng giao diện không chạy được và lớp phủ đứng yên che trang mới (đo: tới 1,2 giây). UpdateLayeredWindow chỉ đổi độ
            // trong suốt (không gửi thông điệp vào cửa sổ) nên gọi từ luồng khác được; hủy cửa sổ vẫn chuyển về luồng giao diện.
            var luong = new Thread(LamMoNen) { IsBackground = true, Priority = ThreadPriority.AboveNormal, Name = "ChuyenTrangMuot" };
            luong.Start();
        }
        private IntPtr _hwnd;
        private volatile bool _daMoXong;
        private void LamMoNen()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                while (!IsDisposed)
                {
                    double t = sw.ElapsedMilliseconds / (double)ThoiGianMoMs;
                    if (t >= 1) break;
                    double e = 1 - Math.Pow(1 - t, 2);              // chậm dần cuối
                    CapNhat((int)(255 * (1 - e)));
                    Thread.Sleep(12);
                }
                if (!IsDisposed) CapNhat(0);                        // trong suốt hẳn (vô hình, chuột xuyên qua)
            }
            catch { }
            _daMoXong = true;
            try { if (!IsDisposed && IsHandleCreated) BeginInvoke(new Action(Huy)); } catch { }
        }
        private void BuocMo()
        {
            // (Dự phòng — không dùng nữa: làm mờ chạy ở luồng riêng LamMoNen)
            if (IsDisposed || !_daMoXong) return;
            Huy();
        }
        private void Huy()
        {
            if (IsDisposed) return;
            DemHuy++;
            MsTonTaiCuoi = _tong.ElapsedMilliseconds;
            _timer.Stop();
            try { Close(); } catch { }
            Dispose();
        }
        protected override void Dispose(bool disposing)
        {
            // Luồng làm mờ còn chạy (vd. Form2 đóng giữa chừng): chờ tối đa ~ThoiGianMoMs để nó xong trước khi xóa DC/ảnh nó đang dùng
            if (disposing && _hwnd != IntPtr.Zero && !_daMoXong)
            {
                var sw = Stopwatch.StartNew();
                while (!_daMoXong && sw.ElapsedMilliseconds < ThoiGianMoMs + 100) Thread.Sleep(5);
            }
            if (disposing)
            {
                _timer.Stop(); _timer.Dispose();
            }
            if (_memDc != IntPtr.Zero)
            {
                if (_cu != IntPtr.Zero) SelectObject(_memDc, _cu);
                DeleteDC(_memDc); _memDc = IntPtr.Zero;
            }
            if (_hBmp != IntPtr.Zero) { DeleteObject(_hBmp); _hBmp = IntPtr.Zero; }
            base.Dispose(disposing);
        }
        // ====== Win32 ======
        [StructLayout(LayoutKind.Sequential)] private struct DiemNative { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)] private struct CoNative { public int Cx, Cy; }
        [StructLayout(LayoutKind.Sequential)] private struct TronNative { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref DiemNative pptDst, ref CoNative psize,
            IntPtr hdcSrc, ref DiemNative pprSrc, int crKey, ref TronNative pblend, int dwFlags);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [StructLayout(LayoutKind.Sequential)] private struct ChuNhatNative { public int L, T, R, B; }
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr h, out ChuNhatNative r);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr h);
    }
}
