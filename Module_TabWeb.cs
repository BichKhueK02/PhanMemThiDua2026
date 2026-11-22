using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Biến TabControl chuẩn của WinForms thành thanh tab kiểu web: chữ phẳng, tab đang chọn chữ đậm màu xanh
    /// có gạch chân, tab còn lại chữ xám (đậm lên khi rê chuột), không viền 3D. Dùng được cho BẤT KỲ TabControl nào:
    ///     Module_TabWeb.ApDung(tabControl1);
    /// Cách làm: giữ nguyên TabControl (nên SelectedIndexChanged, TabPages, nhúng form... không đổi gì) và chỉ
    /// thay phần VẼ bằng cách chặn WM_PAINT của chính nó.
    /// </summary>
    internal static class Module_TabWeb
    {
        public static readonly Color Xanh = Color.FromArgb(59, 111, 240);
        public static readonly Color ChuXam = Color.FromArgb(100, 110, 125);
        public static readonly Color ChuHover = Color.FromArgb(40, 50, 65);
        public static readonly Color DuongKe = Color.FromArgb(226, 229, 235);
        public static readonly Color NenMacDinh = Color.FromArgb(247, 248, 250);
        private static readonly ConditionalWeakTable<TabControl, TabVe> _daAp = new();
        public static void ApDung(TabControl? tc, Color? nen = null, int cao = 40)
        {
            if (tc == null || tc.IsDisposed) return;
            if (_daAp.TryGetValue(tc, out _)) return;
            Color mauNen = nen ?? NenMacDinh;
            // 1) Các trang không tự đặt Font đang KẾ THỪA font của TabControl -> chốt font hiện tại cho từng trang
            //    trước khi đổi font tab, để nội dung bên trong không bị đổi chữ theo.
            foreach (TabPage p in tc.TabPages)
            {
                p.Font = p.Font;
                p.UseVisualStyleBackColor = false;
                p.BackColor = mauNen;
            }
            // 2) Thanh tab: cao hơn, font đậm vừa (để độ rộng tab đủ chứa chữ đậm), khoảng cách ngang rộng
            tc.Appearance = TabAppearance.Normal;
            tc.SizeMode = TabSizeMode.Normal;
            tc.Multiline = false;
            var fontDam = new Font("Segoe UI Semibold", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
            tc.Font = fontDam;
            tc.Padding = new Point(tc.LogicalToDeviceUnits(18), tc.LogicalToDeviceUnits(6));
            tc.ItemSize = new Size(0, tc.LogicalToDeviceUnits(cao));
            var ve = new TabVe(tc, mauNen, fontDam);
            _daAp.Add(tc, ve);
        }
        private sealed class TabVe : NativeWindow
        {
            private const int WM_PAINT = 0x000F, WM_ERASEBKGND = 0x0014, WM_SETCURSOR = 0x0020, WM_PRINTCLIENT = 0x0318;
            [StructLayout(LayoutKind.Sequential)]
            private struct PAINTSTRUCT
            {
                public IntPtr hdc; public bool fErase; public RECT rcPaint; public bool fRestore; public bool fIncUpdate;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] rgbReserved;
            }
            [StructLayout(LayoutKind.Sequential)]
            private struct RECT { public int Left, Top, Right, Bottom; }
            [DllImport("user32.dll")] private static extern IntPtr BeginPaint(IntPtr hWnd, out PAINTSTRUCT ps);
            [DllImport("user32.dll")] private static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT ps);
            [DllImport("user32.dll")] private static extern IntPtr SetCursor(IntPtr hCursor);
            private readonly TabControl _tc;
            private readonly Color _nen;
            private readonly Font _fontDam, _fontThuong;
            private int _hover = -1;
            public TabVe(TabControl tc, Color nen, Font fontDam)
            {
                _tc = tc; _nen = nen; _fontDam = fontDam;
                _fontThuong = new Font(fontDam, FontStyle.Regular);
                tc.HandleCreated += (s, e) => Gan();
                tc.HandleDestroyed += (s, e) => ReleaseHandle();
                tc.MouseMove += (s, e) => DatHover(TimTab(e.Location));
                tc.MouseLeave += (s, e) => DatHover(-1);
                tc.Disposed += (s, e) => { ReleaseHandle(); _fontDam.Dispose(); _fontThuong.Dispose(); };
                if (tc.IsHandleCreated) Gan();
            }
            private void Gan()
            {
                if (Handle != IntPtr.Zero) ReleaseHandle();
                AssignHandle(_tc.Handle);
                _tc.Invalidate();
            }
            private int TimTab(Point p)
            {
                for (int i = 0; i < _tc.TabCount; i++)
                    if (_tc.GetTabRect(i).Contains(p)) return i;
                return -1;
            }
            private void DatHover(int i)
            {
                if (i == _hover) return;
                _hover = i;
                if (_tc.IsHandleCreated && _tc.TabCount > 0)
                    _tc.Invalidate(new Rectangle(0, 0, _tc.ClientSize.Width, _tc.GetTabRect(0).Bottom + 4));
            }
            protected override void WndProc(ref Message m)
            {
                switch (m.Msg)
                {
                    case WM_ERASEBKGND:
                        m.Result = (IntPtr)1;     // không xóa nền: tự vẽ bằng bộ đệm -> không chớp
                        return;
                    case WM_PAINT:
                        {
                            if (m.WParam != IntPtr.Zero) { Ve(m.WParam); m.Result = IntPtr.Zero; return; }
                            var ps = new PAINTSTRUCT { rgbReserved = new byte[32] };
                            IntPtr hdc = BeginPaint(m.HWnd, out ps);
                            try { if (hdc != IntPtr.Zero) Ve(hdc); }
                            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[TabWeb] " + ex.Message); }
                            finally { EndPaint(m.HWnd, ref ps); }
                            m.Result = IntPtr.Zero;
                            return;
                        }
                    case WM_PRINTCLIENT:
                        Ve(m.WParam);
                        m.Result = IntPtr.Zero;
                        return;
                    case WM_SETCURSOR:
                        {
                            var p = _tc.PointToClient(Cursor.Position);
                            if (TimTab(p) >= 0)
                            {
                                SetCursor(Cursors.Hand.Handle);
                                m.Result = (IntPtr)1;
                                return;
                            }
                            break;
                        }
                }
                base.WndProc(ref m);
            }
            private void Ve(IntPtr hdc)
            {
                var rc = _tc.ClientRectangle;
                if (rc.Width <= 0 || rc.Height <= 0) return;
                using var bo = BufferedGraphicsManager.Current.Allocate(hdc, rc);
                var g = bo.Graphics;
                g.Clear(_nen);
                if (_tc.TabCount > 0)
                {
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    int day = _tc.GetTabRect(0).Bottom;
                    using (var pen = new Pen(DuongKe, 1f))
                        g.DrawLine(pen, 0, day, rc.Width, day);
                    for (int i = 0; i < _tc.TabCount; i++)
                    {
                        var r = _tc.GetTabRect(i);
                        bool chon = i == _tc.SelectedIndex;
                        var mau = chon ? Xanh : (i == _hover ? ChuHover : ChuXam);
                        TextRenderer.DrawText(g, _tc.TabPages[i].Text, chon ? _fontDam : _fontThuong, r, mau,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                            TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
                        if (chon)
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            using var br = new SolidBrush(Xanh);
                            int h = Math.Max(2, _tc.LogicalToDeviceUnits(3));
                            g.FillRectangle(br, new Rectangle(r.Left + 4, day - h + 1, Math.Max(1, r.Width - 8), h));
                        }
                    }
                }
                bo.Render(hdc);
            }
        }
    }
}