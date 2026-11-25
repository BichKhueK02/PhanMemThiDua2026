using System.Diagnostics;
using System.Runtime.InteropServices;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Thu gọn / mở rộng menu trái của Form2 MƯỢT kiểu web (menu trượt ra/vào, nội dung trôi theo).
    /// Vì sao không chỉ đổi PanelLeft.Width dần dần: mỗi lần đổi bề rộng, cả trang con (lưới Krypton...) phải dàn lại + vẽ lại
    /// (đo 23/11: đổi rộng ~75 ms + vẽ ~80 ms) => chạy từng bước còn giật hơn đổi một phát. Cách làm (giống LopChuyenTrang):
    ///  1) chụp vùng menu + nội dung (ảnh TRƯỚC), phủ đúng chỗ bằng cửa sổ "layered" (DWM ghép hình, không nhận chuột/focus)
    ///     và cho luồng riêng bắt đầu trượt NGAY (phản hồi tức thì như web);
    ///  2) cùng lúc luồng giao diện đổi bề rộng thật 1 lần DUY NHẤT + vẽ xong phía sau lớp phủ, rồi chụp lại màn hình (ảnh SAU).
    ///     Lớp phủ đặt WDA_EXCLUDEFROMCAPTURE (Win10 2004+) => chụp màn hình bỏ qua nó, lấy được giao diện thật nằm dưới (vài ms).
    ///     (Không kèm CAPTUREBLT vẫn chụp dính cửa sổ layered trên Win11 - đã thử.) Máy cũ: PrintWindow (~100 ms, vẽ lại cả cửa sổ);
    ///  3) mỗi khung (khớp nhịp màn hình bằng DwmFlush): dải menu rộng w(t) + nội dung đặt ngay sau mép menu, w chạy theo
    ///     đường cong Material. Chưa có ảnh SAU thì ghép từ ảnh TRƯỚC (kéo dài nền menu / mép phải nội dung); ảnh SAU về thì
    ///     nội dung dùng ngay bản đã dàn lại, dải menu HÒA DẦN sang (đổi phắt menu giữa chừng làm người dùng thấy "khựng");
    ///  4) khung cuối trùng khít giao diện thật => gỡ lớp phủ (mờ rất ngắn).
    /// Mọi lỗi / điều kiện không chắc chắn (form không active, DPI ảo hóa, vùng ra ngoài màn hình...) => đổi bề rộng tức thì như cũ.
    /// </summary>
    internal sealed class LopTruotMenu : Form
    {
        private const int ThoiGianTruotMs = 260;
        private const int ThoiGianHoaMs = 110;   // hòa từ ảnh ghép tạm sang ảnh SAU khi ảnh sau về tới
        private const int ThoiGianMoMs = 50;
        private const int ToiDaMs = 1500;   // chốt chặn: không bao giờ che giao diện lâu hơn
        /// <summary>Đang có hiệu ứng chạy (Form2 bỏ qua bấm Thu gọn/Mở rộng liên tiếp trong lúc này).</summary>
        internal static bool DangChay;
        /// <summary>Chẩn đoán (công cụ kiểm thử đọc): thời gian từng bước của lần chạy gần nhất.</summary>
        internal static string DoCuoi = "";
        /// <summary>CHỈ để kiểm thử: khác null => lưu mọi khung ghép (ms kể từ lúc bấm, ảnh) - lớp phủ không có trong ảnh chụp màn hình.</summary>
        internal static List<(long ms, Bitmap anh)>? KiemThuKhung;
        private readonly Rectangle _vung;
        private readonly int _rongTruoc, _rongSau;
        private IntPtr _dcTruoc, _bmpTruoc, _cuTruoc;   // ảnh TRƯỚC
        private IntPtr _dcSau, _bmpSau, _cuSau;         // ảnh SAU (giao diện thật sau khi đổi bề rộng)
        private IntPtr _dcKhung, _bmpKhung, _cuKhung;   // khung hình đang hiện trên lớp phủ
        private IntPtr _dcTam, _bmpTam, _cuTam;         // khung tạm (ảnh sau) để hòa trộn
        private IntPtr _hwnd;
        private volatile bool _coAnhSau, _huyBo, _luongXong = true;
        private bool _anKhoiChup;   // lớp phủ đã được loại khỏi ảnh chụp màn hình (WDA_EXCLUDEFROMCAPTURE)
        private readonly Stopwatch _tuLucBam = Stopwatch.StartNew();
        /// <summary>
        /// Đổi bề rộng menu (thành <paramref name="rongMoi"/>) có hiệu ứng trượt. <paramref name="doiRong"/> (đổi bề rộng thật)
        /// LUÔN được gọi đúng 1 lần, kể cả khi không làm được hiệu ứng.
        /// </summary>
        public static void Chay(Form chu, Control menu, Control noiDung, int rongMoi, Action doiRong)
        {
            LopTruotMenu? lop = null;
            if (!DangChay)
            {
                try { lop = PhuAnhTruoc(chu, menu, noiDung, rongMoi); }
                catch (Exception ex) { Debug.WriteLine("[TruotMenu] phủ: " + ex.Message); lop = null; }
            }
            if (lop == null) { doiRong(); return; }
            var sw = Stopwatch.StartNew();
            try { doiRong(); }
            catch { lop._huyBo = true; lop.Huy(); throw; }
            long msDoi = sw.ElapsedMilliseconds;
            try { lop.ChupAnhSau(chu, menu); }
            catch (Exception ex) { Debug.WriteLine("[TruotMenu] chụp: " + ex.Message); lop._huyBo = true; }
            DoCuoi = $"đổi rộng {msDoi} ms | vẽ + chụp {sw.ElapsedMilliseconds - msDoi} ms | có ảnh sau: {lop._coAnhSau} | loại khỏi chụp: {lop._anKhoiChup}";
        }
        /// <summary>
        /// Biên dịch sẵn (JIT) mã hiệu ứng ở nền: lần bấm ĐẦU TIÊN không phải chờ biên dịch (cảm giác "nút không nhận").
        /// </summary>
        public static void ChuanBiNen()
        {
            Task.Run(() =>
            {
                try
                {
                    const System.Reflection.BindingFlags bf = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static |
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly;
                    foreach (var m in typeof(LopTruotMenu).GetMethods(bf))
                        if (!m.IsAbstract && !m.ContainsGenericParameters)
                            try { System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(m.MethodHandle); } catch { }
                    DuongCong(0.5);
                }
                catch { }
            });
        }
        private static LopTruotMenu? PhuAnhTruoc(Form chu, Control menu, Control noiDung, int rongMoi)
        {
            if (!chu.Visible || chu.WindowState == FormWindowState.Minimized || !chu.IsHandleCreated) return null;
            if (!menu.IsHandleCreated || !noiDung.IsHandleCreated || !menu.Visible) return null;
            if (!ReferenceEquals(Form.ActiveForm, chu)) return null;          // chỉ khi người dùng đang thao tác trên Form2
            if (!LopChuyenTrang.ToaDoManHinhTinCay(chu)) return null;         // tọa độ bị ảo hóa DPI => chụp lệch chỗ
            Rectangle rMenu = menu.RectangleToScreen(menu.ClientRectangle);
            Rectangle rNoiDung = noiDung.RectangleToScreen(noiDung.ClientRectangle);
            Rectangle r = Rectangle.Union(rMenu, rNoiDung);
            // Bố cục phải đúng kiểu "menu bên trái, nội dung bên phải" (Dock Left + Fill)
            if (rMenu.Left != r.Left || rMenu.Top != r.Top || rMenu.Height != r.Height) return null;
            if (r.Width < 80 || r.Height < 40 || rongMoi < 0 || rongMoi >= r.Width || rongMoi == menu.Width) return null;
            if (!Screen.FromControl(chu).Bounds.Contains(r)) return null;    // phần ngoài màn hình chụp ra đen
            var lop = new LopTruotMenu(r, menu.Width, rongMoi) { Owner = chu };
            if (!lop.Hien()) { lop.Dispose(); return null; }
            DangChay = true;
            lop._luongXong = false;
            new Thread(lop.LamTruot) { IsBackground = true, Priority = ThreadPriority.Highest, Name = "TruotMenuMuot" }.Start();
            return lop;
        }
        private LopTruotMenu(Rectangle vung, int rongTruoc, int rongSau)
        {
            _vung = vung;
            _rongTruoc = rongTruoc;
            _rongSau = rongSau;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Bounds = vung;
        }
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // layered | không hiện Alt+Tab | không lấy focus | chuột xuyên qua
                cp.ExStyle |= 0x00080000 | 0x00000080 | 0x08000000 | 0x00000020;
                return cp;
            }
        }
        protected override bool ShowWithoutActivation => true;
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e) { }
        private bool Hien()
        {
            _hwnd = Handle;
            IntPtr man = GetDC(IntPtr.Zero);
            try
            {
                TaoDc(man, _vung.Width, _vung.Height, out _dcTruoc, out _bmpTruoc, out _cuTruoc);
                TaoDc(man, _vung.Width, _vung.Height, out _dcSau, out _bmpSau, out _cuSau);
                TaoDc(man, _vung.Width, _vung.Height, out _dcKhung, out _bmpKhung, out _cuKhung);
                TaoDc(man, _vung.Width, _vung.Height, out _dcTam, out _bmpTam, out _cuTam);
                if (!BitBlt(_dcTruoc, 0, 0, _vung.Width, _vung.Height, man, _vung.X, _vung.Y, SRCCOPY)) return false;
            }
            finally { ReleaseDC(IntPtr.Zero, man); }
            BitBlt(_dcKhung, 0, 0, _vung.Width, _vung.Height, _dcTruoc, 0, 0, SRCCOPY);
            if (!CapNhat(255)) return false;                        // có hình TRƯỚC khi hiện => không chớp
            try { _anKhoiChup = SetWindowDisplayAffinity(_hwnd, 0x11 /*WDA_EXCLUDEFROMCAPTURE*/); } catch { _anKhoiChup = false; }
            ShowWindow(_hwnd, 4 /*SW_SHOWNOACTIVATE*/);
            GetWindowRect(_hwnd, out var thuc);
            if (Rectangle.FromLTRB(thuc.L, thuc.T, thuc.R, thuc.B) != _vung) { ShowWindow(_hwnd, 0); return false; }
            return true;
        }
        /// <summary>Bề rộng thật đã đổi xong phía sau lớp phủ: vẽ xong ngay rồi chụp màn hình (bỏ qua lớp phủ) làm ảnh SAU.</summary>
        private void ChupAnhSau(Form chu, Control menu)
        {
            if (IsDisposed || chu.IsDisposed) { _huyBo = true; return; }
            Rectangle rMenu = menu.RectangleToScreen(menu.ClientRectangle);
            if (menu.Width != _rongSau || rMenu.Left != _vung.Left || rMenu.Top != _vung.Top) { _huyBo = true; return; }
            // Vẽ đồng bộ toàn bộ rồi chờ DWM ghép xong khung đó => màn hình (dưới lớp phủ) đã là giao diện mới
            RedrawWindow(chu.Handle, IntPtr.Zero, IntPtr.Zero, RDW_INVALIDATE | RDW_ERASE | RDW_ALLCHILDREN | RDW_UPDATENOW);
            try { DwmFlush(); } catch { }
            bool ok;
            if (_anKhoiChup)
            {
                // Lớp phủ đã loại khỏi ảnh chụp => chụp màn hình ra đúng giao diện thật bên dưới
                IntPtr man = GetDC(IntPtr.Zero);
                try { ok = BitBlt(_dcSau, 0, 0, _vung.Width, _vung.Height, man, _vung.X, _vung.Y, SRCCOPY); }
                finally { ReleaseDC(IntPtr.Zero, man); }
            }
            else ok = ChupBangPrintWindow(chu);
            GdiFlush();
            if (ok) _coAnhSau = true; else _huyBo = true;
        }
        /// <summary>Máy không hỗ trợ WDA_EXCLUDEFROMCAPTURE: vẽ cả vùng khách của Form2 qua DWM rồi cắt đúng vùng.</summary>
        private bool ChupBangPrintWindow(Form chu)
        {
            Size khach = chu.ClientSize;
            Point goc = chu.PointToScreen(Point.Empty);
            int lx = _vung.X - goc.X, ly = _vung.Y - goc.Y;
            if (lx < 0 || ly < 0 || lx + _vung.Width > khach.Width || ly + _vung.Height > khach.Height) return false;
            IntPtr man = GetDC(IntPtr.Zero);
            IntPtr dc = IntPtr.Zero, bmp = IntPtr.Zero, cu = IntPtr.Zero;
            try
            {
                TaoDc(man, khach.Width, khach.Height, out dc, out bmp, out cu);
                if (!PrintWindow(chu.Handle, dc, 0x1 /*PW_CLIENTONLY*/ | 0x2 /*PW_RENDERFULLCONTENT*/)) return false;
                return BitBlt(_dcSau, 0, 0, _vung.Width, _vung.Height, dc, lx, ly, SRCCOPY);
            }
            finally { XoaDc(ref dc, ref bmp, cu); ReleaseDC(IntPtr.Zero, man); }
        }
        private void LuuKhungKiemThu()
        {
            var ds = KiemThuKhung;
            if (ds == null) return;
            var anh = new Bitmap(_vung.Width, _vung.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            using (var g = Graphics.FromImage(anh)) { IntPtr d = g.GetHdc(); BitBlt(d, 0, 0, _vung.Width, _vung.Height, _dcKhung, 0, 0, SRCCOPY); g.ReleaseHdc(d); }
            lock (ds) ds.Add((_tuLucBam.ElapsedMilliseconds, anh));
        }
        private void LamTruot()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                bool moRong = _rongSau > _rongTruoc;
                // Thanh menu đang ẩn hẳn (kiểu Cổ điển) thì không có nền menu để kéo dài => chờ ảnh SAU rồi mới trượt
                if (_rongTruoc < 8)
                    while (!_coAnhSau && !_huyBo && !IsDisposed && sw.ElapsedMilliseconds < ToiDaMs) ChoKhungSau();
                // Trượt + chuyển dần ảnh: mép menu chạy theo đường cong chuẩn Material (0.4, 0, 0.2, 1) - khởi động êm, dừng êm.
                // Ảnh SAU về tới (giữa chừng) thì KHÔNG đổi phắt mà hòa dần từ ảnh ghép tạm sang ảnh sau trong ThoiGianHoaMs
                // => không có cú "nhảy" thanh icon -> menu đầy đủ / chữ -> icon. Khung cuối trùng khít giao diện thật.
                var swTruot = Stopwatch.StartNew();
                long tCoSau = -1;
                while (!_huyBo && !IsDisposed && sw.ElapsedMilliseconds < ToiDaMs)
                {
                    long ms = swTruot.ElapsedMilliseconds;
                    double t = Math.Min(1, ms / (double)ThoiGianTruotMs);
                    int w = (int)Math.Round(_rongTruoc + (_rongSau - _rongTruoc) * DuongCong(t));
                    double k = 0;
                    if (_coAnhSau)
                    {
                        if (tCoSau < 0) tCoSau = _rongTruoc < 8 ? -ThoiGianHoaMs : ms;   // đã chờ sẵn ảnh sau => dùng luôn
                        k = Math.Min(1, (ms - tCoSau) / (double)ThoiGianHoaMs);
                    }
                    VeKhung(w, moRong, k);
                    CapNhat(255);
                    LuuKhungKiemThu();
                    if (t >= 1 && k >= 1) break;
                    ChoKhungSau();
                }
                // Khung cuối = giao diện thật => mờ rất nhanh rồi gỡ (chỉ để che nếu giao diện vừa tự đổi gì đó)
                var swMo = Stopwatch.StartNew();
                while (!_huyBo && !IsDisposed && sw.ElapsedMilliseconds < ToiDaMs + ThoiGianMoMs)
                {
                    double t = Math.Min(1, swMo.ElapsedMilliseconds / (double)ThoiGianMoMs);
                    CapNhat((int)(255 * (1 - t)));
                    if (t >= 1) break;
                    ChoKhungSau();
                }
                if (!IsDisposed) CapNhat(0);
            }
            catch { }
            _luongXong = true;
            try { if (!IsDisposed && IsHandleCreated) BeginInvoke(new Action(Huy)); } catch { }
        }
        /// <summary>cubic-bezier(0.4, 0, 0.2, 1) (đường cong "standard" của Material, như transition CSS của các trang web).</summary>
        private static double DuongCong(double x)
        {
            if (x <= 0) return 0;
            if (x >= 1) return 1;
            const double x1 = 0.4, y1 = 0, x2 = 0.2, y2 = 1;
            static double B(double u, double a, double b) => 3 * (1 - u) * (1 - u) * u * a + 3 * (1 - u) * u * u * b + u * u * u;
            double lo = 0, hi = 1, s = x;
            for (int i = 0; i < 24; i++) { s = (lo + hi) / 2; if (B(s, x1, x2) < x) lo = s; else hi = s; }
            return B(s, y1, y2);
        }
        /// <summary>
        /// Ghép 1 khung bề rộng menu w. Nội dung: có ảnh SAU (k &gt; 0) là dùng ngay nội dung đã dàn lại - KHÔNG hòa trộn, vì
        /// 2 bố cục khác nhau chồng lên thành "bóng ma" (nút/cột nhân đôi, đã thử). Dải menu: hòa dần ảnh tạm -> ảnh sau theo
        /// <paramref name="k"/> (chữ mờ dần thành icon / icon thành menu đầy đủ).
        /// </summary>
        private void VeKhung(int w, bool moRong, double k)
        {
            int W = _vung.Width, H = _vung.Height;
            w = Math.Clamp(w, 0, W);
            GhepNoiDung(_dcKhung, w, k > 0);
            if (w > 0)
            {
                if (k >= 1) GhepMenu(_dcKhung, w, moRong, true);
                else
                {
                    GhepMenu(_dcKhung, w, moRong, false);
                    if (k > 0)
                    {
                        GhepMenu(_dcTam, w, moRong, true);
                        var blend = new TronNative { BlendOp = 0, BlendFlags = 0, SourceConstantAlpha = (byte)Math.Clamp((int)(k * 255), 0, 255), AlphaFormat = 0 };
                        GdiAlphaBlend(_dcKhung, 0, 0, w, H, _dcTam, 0, 0, w, H, blend);
                    }
                }
            }
            GdiFlush();
        }
        /// <summary>Dải menu rộng w từ ảnh TRƯỚC hoặc ảnh SAU.</summary>
        private void GhepMenu(IntPtr dich, int w, bool moRong, bool sau)
        {
            int H = _vung.Height;
            IntPtr nguon = sau ? _dcSau : _dcTruoc;
            int rNguon = sau ? _rongSau : _rongTruoc;            // bề rộng menu trong ảnh nguồn
            bool laMenuDayDu = moRong == sau;                    // ảnh nguồn là menu ĐẦY ĐỦ (rộng) hay thanh icon (hẹp)
            if (laMenuDayDu) BitBlt(dich, 0, 0, w, H, nguon, 0, 0, SRCCOPY);   // menu đầy đủ bị cắt / lộ dần theo mép
            else if (rNguon < 8)
            {
                // Menu ẩn hẳn (Cổ điển): không có thanh icon => dùng menu đầy đủ của ảnh kia, cắt theo mép
                BitBlt(dich, 0, 0, w, H, sau ? _dcTruoc : _dcSau, 0, 0, SRCCOPY);
            }
            else
            {
                // Thanh icon + kéo dài nền menu (cột trong phần đệm sát mép phải), giữ đường mép
                int mep = Math.Min(2, rNguon);
                BitBlt(dich, 0, 0, Math.Min(w, rNguon), H, nguon, 0, 0, SRCCOPY);
                if (w > rNguon)
                {
                    StretchBlt(dich, rNguon - mep, 0, w - rNguon, H, nguon, Math.Max(0, rNguon - 4), 0, 1, H, SRCCOPY);
                    BitBlt(dich, w - mep, 0, mep, H, nguon, rNguon - mep, 0, SRCCOPY);
                }
            }
        }
        /// <summary>Nội dung đặt từ mép menu w; thừa thì cắt bên phải, thiếu thì kéo dài cột mép phải.</summary>
        private void GhepNoiDung(IntPtr dich, int w, bool sau)
        {
            int W = _vung.Width, H = _vung.Height;
            int con = W - w;
            if (con <= 0) return;
            IntPtr nguon = sau ? _dcSau : _dcTruoc;
            int rNguon = sau ? _rongSau : _rongTruoc;
            int rongNd = W - rNguon;
            BitBlt(dich, w, 0, Math.Min(con, rongNd), H, nguon, rNguon, 0, SRCCOPY);
            int thieu = con - rongNd;
            if (thieu > 0) StretchBlt(dich, w + rongNd, 0, thieu, H, nguon, W - 1, 0, 1, H, SRCCOPY);
        }
        private static void ChoKhungSau()
        {
            // Khớp nhịp làm tươi màn hình (60/120/144 Hz); DWM không chạy => ngủ ~1 khung
            try { if (DwmFlush() == 0) return; } catch { }
            Thread.Sleep(8);
        }
        private bool CapNhat(int alpha)
        {
            var dst = new DiemNative { X = _vung.X, Y = _vung.Y };
            var size = new CoNative { Cx = _vung.Width, Cy = _vung.Height };
            var src = new DiemNative();
            var blend = new TronNative { BlendOp = 0, BlendFlags = 0, SourceConstantAlpha = (byte)Math.Clamp(alpha, 0, 255), AlphaFormat = 0 };
            return UpdateLayeredWindow(_hwnd, IntPtr.Zero, ref dst, ref size, _dcKhung, ref src, 0, ref blend, 2 /*ULW_ALPHA*/);
        }
        private void Huy()
        {
            if (IsDisposed) return;
            DangChay = false;
            try { Close(); } catch { }
            Dispose();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_luongXong)
            {
                // Luồng trượt còn chạy (vd. Form2 đóng giữa chừng): báo dừng và chờ nó xong trước khi xóa DC/ảnh nó đang dùng
                _huyBo = true;
                var sw = Stopwatch.StartNew();
                while (!_luongXong && sw.ElapsedMilliseconds < 500) Thread.Sleep(5);
            }
            if (disposing) DangChay = false;
            if (_luongXong)
            {
                XoaDc(ref _dcTruoc, ref _bmpTruoc, _cuTruoc);
                XoaDc(ref _dcSau, ref _bmpSau, _cuSau);
                XoaDc(ref _dcKhung, ref _bmpKhung, _cuKhung);
                XoaDc(ref _dcTam, ref _bmpTam, _cuTam);
            }
            base.Dispose(disposing);
        }
        private static void TaoDc(IntPtr man, int w, int h, out IntPtr dc, out IntPtr bmp, out IntPtr cu)
        {
            dc = CreateCompatibleDC(man);
            bmp = CreateCompatibleBitmap(man, w, h);
            cu = SelectObject(dc, bmp);
        }
        private static void XoaDc(ref IntPtr dc, ref IntPtr bmp, IntPtr cu)
        {
            if (dc != IntPtr.Zero)
            {
                if (cu != IntPtr.Zero) SelectObject(dc, cu);
                DeleteDC(dc); dc = IntPtr.Zero;
            }
            if (bmp != IntPtr.Zero) { DeleteObject(bmp); bmp = IntPtr.Zero; }
        }
        // ====== Win32 ======
        private const int SRCCOPY = 0x00CC0020;
        private const uint RDW_INVALIDATE = 0x1, RDW_ERASE = 0x4, RDW_ALLCHILDREN = 0x80, RDW_UPDATENOW = 0x100;
        [StructLayout(LayoutKind.Sequential)] private struct DiemNative { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)] private struct CoNative { public int Cx, Cy; }
        [StructLayout(LayoutKind.Sequential)] private struct TronNative { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }
        [StructLayout(LayoutKind.Sequential)] private struct ChuNhatNative { public int L, T, R, B; }
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref DiemNative pptDst, ref CoNative psize,
            IntPtr hdcSrc, ref DiemNative pprSrc, int crKey, ref TronNative pblend, int dwFlags);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr h, out ChuNhatNative r);
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);
        [DllImport("user32.dll")] private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);
        [DllImport("dwmapi.dll")] private static extern int DwmFlush();
        [DllImport("user32.dll")] private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint affinity);
        [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr h);
        [DllImport("gdi32.dll")] private static extern bool GdiFlush();
        [DllImport("gdi32.dll")]
        private static extern bool GdiAlphaBlend(IntPtr hdcDest, int x, int y, int w, int h, IntPtr hdcSrc, int sx, int sy, int sw, int sh, TronNative blend);
        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int x, int y, int w, int h, IntPtr hdcSrc, int sx, int sy, int rop);
        [DllImport("gdi32.dll")]
        private static extern bool StretchBlt(IntPtr hdcDest, int x, int y, int w, int h, IntPtr hdcSrc, int sx, int sy, int sw, int sh, int rop);
    }
}
