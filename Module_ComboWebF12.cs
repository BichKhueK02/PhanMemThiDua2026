using System.ComponentModel;
using System.Drawing.Drawing2D;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  Ô CHỌN "KIỂU WEB" CHO Form12_CaiDat — CHỈ TRÌNH BÀY, KHÔNG ĐỔI LOGIC
    //  - ComboBox gốc vẫn còn nguyên (tên, Items, Text, SelectedIndex, sự kiện của code gốc) nhưng bị ẩn hình
    //    (Region rỗng). Một ô tự vẽ (double-buffer) nằm đè đúng vị trí/kích thước của nó => không lệch bố cục.
    //  - ComboBox kiểu DropDown (cho gõ thêm): bên trong ô có 1 TextBox không viền, đồng bộ 2 chiều với cb.Text
    //    => gõ được, không bị Windows tự bôi xanh chữ, không nhấp nháy khi rê chuột.
    //  - Danh sách sổ xuống tự vẽ, tự lật lên trên / dịch vào trong khi sát mép màn hình.
    //  Gọi 1 lần: Module_ComboWebF12.ApDung(this);
    // =====================================================================================
    internal static class Module_ComboWebF12
    {
        public static void ApDung(Control goc)
        {
            foreach (var cb in TatCa(goc).ToList())
                if (cb.GetType() == typeof(ComboBox))   // đúng ComboBox gốc, bỏ qua lớp con của control khác
                    OComboWebF12.Gan(cb);
        }
        private static IEnumerable<ComboBox> TatCa(Control goc)
        {
            foreach (Control c in goc.Controls)
            {
                if (c is ComboBox cb) yield return cb;
                foreach (var con in TatCa(c)) yield return con;
            }
        }
    }
    [DesignerCategory("Code")]
    internal sealed class OComboWebF12 : Control
    {
        private static readonly Color MauNen = Color.White;
        private static readonly Color MauNenKhoa = Color.FromArgb(246, 247, 249);
        private static readonly Color MauVien = Color.FromArgb(200, 206, 216);
        private static readonly Color MauVienHover = Color.FromArgb(140, 165, 205);
        private static readonly Color MauVienChon = Color.FromArgb(0, 120, 215);
        private static readonly Color MauVienKhoa = Color.FromArgb(222, 226, 232);
        private static readonly Color MauChu = Color.FromArgb(30, 41, 59);
        private static readonly Color MauChuKhoa = Color.FromArgb(150, 160, 175);
        private static readonly Color MauMuiTen = Color.FromArgb(100, 112, 130);
        private const float DuongKinhBo = 10f;   // bo nhẹ (bán kính 5px ở 100%)
        private readonly ComboBox _cb;
        private readonly TextBox? _tb;          // chỉ có khi cb là DropDown (cho gõ)
        private readonly List<Control> _chuoi = new List<Control>();
        private Control? _host;
        private bool _hover, _dongBoChu;
        private PopupDanhSachF12? _popup;
        private long _tDong;
        public static void Gan(ComboBox cb) { _ = new OComboWebF12(cb); }
        private OComboWebF12(ComboBox cb)
        {
            _cb = cb;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.StandardClick |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;   // góc ngoài đường bo lấy đúng nền của khung chứa
            TabStop = false;                 // phím Tab đi theo thứ tự của ComboBox gốc (xem cb.Enter, ProcessDialogKey)
            Font = cb.Font;
            Visible = false;
            if (cb.DropDownStyle == ComboBoxStyle.DropDown)
            {
                _tb = new TextBox
                {
                    BorderStyle = BorderStyle.None,
                    Font = cb.Font,
                    BackColor = MauNen,
                    ForeColor = MauChuCua(cb),
                    Text = cb.Text,
                    TabStop = false,
                };
                _tb.TextChanged += Tb_TextChanged;
                _tb.Enter += (s, e) => { Invalidate(); BeginInvoke(DatConTroCuoi); };
                _tb.Leave += (s, e) => Invalidate();
                _tb.MouseEnter += (s, e) => CapNhatHover();
                _tb.MouseLeave += (s, e) => CapNhatHover();
                _tb.KeyDown += Tb_KeyDown;
                Controls.Add(_tb);
            }
            cb.Region = new Region(Rectangle.Empty);   // ẩn hình ComboBox gốc (vẫn tồn tại + hoạt động)
            // Code gốc gọi cb.Focus() / Tab rơi vào cb (đã ẩn) => chuyển focus sang ô hiển thị
            cb.GotFocus += (s, e) =>
            {
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke(() => { if (!IsDisposed && Visible) (_tb ?? (Control)this).Focus(); });
            };
            cb.ParentChanged += (s, e) => GanChuoi();
            cb.LocationChanged += DongBo;
            cb.SizeChanged += DongBo;
            cb.VisibleChanged += DongBo;
            cb.EnabledChanged += DongBo;
            cb.FontChanged += (s, e) => { Font = cb.Font; if (_tb != null) _tb.Font = cb.Font; DatViTriTb(); Invalidate(); };
            cb.ForeColorChanged += (s, e) => { if (_tb != null) _tb.ForeColor = MauChuCua(cb); Invalidate(); };
            cb.TextChanged += Cb_TextChanged;
            cb.SelectedIndexChanged += Cb_TextChanged;
            cb.DataSourceChanged += (s, e) => Invalidate();
            cb.Disposed += (s, e) => Dispose();
            GanChuoi();
        }
        // ---------- Bám vị trí ComboBox gốc ----------
        // Ô vẽ nằm ở khung chứa (không nằm trong TableLayoutPanel để khỏi chen vào ô lưới)
        private void GanChuoi()
        {
            foreach (var c in _chuoi) { c.LocationChanged -= DongBo; c.SizeChanged -= DongBo; c.VisibleChanged -= DongBo; }
            _chuoi.Clear();
            var p = _cb.Parent;
            while (p != null && (p is TableLayoutPanel || p is FlowLayoutPanel)) { _chuoi.Add(p); p = p.Parent; }
            if (p == null) return;
            if (_host != p)
            {
                _host?.Controls.Remove(this);
                _host = p;
                _host.Controls.Add(this);
            }
            foreach (var c in _chuoi) { c.LocationChanged += DongBo; c.SizeChanged += DongBo; c.VisibleChanged += DongBo; }
            DongBo(null, EventArgs.Empty);
        }
        private void DongBo(object? sender, EventArgs e)
        {
            if (_host == null || IsDisposed) return;
            var pt = _cb.Location;
            for (var c = _cb.Parent; c != null && c != _host; c = c.Parent) pt.Offset(c.Location);
            bool hien = _cb.Visible && _chuoi.TrueForAll(c => c.Visible);
            if (Bounds != new Rectangle(pt, _cb.Size)) SetBounds(pt.X, pt.Y, _cb.Width, _cb.Height);
            if (Enabled != _cb.Enabled)
            {
                Enabled = _cb.Enabled;
                if (_tb != null) _tb.BackColor = _cb.Enabled ? MauNen : MauNenKhoa;
                Invalidate();
            }
            if (Visible != hien) { Visible = hien; if (hien) BringToFront(); }
        }
        protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); DatViTriTb(); }
        protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); DatViTriTb(); }
        private float K => DeviceDpi / 96f;
        private int LeTrai => (int)Math.Round(9 * K);
        private int VungNut => (int)Math.Round(26 * K);
        private void DatViTriTb()
        {
            if (_tb == null) return;
            int w = Math.Max(1, Width - LeTrai - VungNut);
            _tb.SetBounds(LeTrai, Math.Max(0, (Height - _tb.Height) / 2), w, _tb.Height);
        }
        // ---------- Đồng bộ chữ 2 chiều (chỉ ô cho gõ) ----------
        private void Tb_TextChanged(object? sender, EventArgs e)
        {
            if (_dongBoChu || _tb == null) return;
            _dongBoChu = true;
            try { _cb.Text = _tb.Text; }   // kích hoạt TextChanged của code gốc
            finally { _dongBoChu = false; }
        }
        private void Cb_TextChanged(object? sender, EventArgs e)
        {
            if (_tb != null && !_dongBoChu && _tb.Text != _cb.Text)
            {
                _dongBoChu = true;
                try { _tb.Text = _cb.Text; DatConTroCuoi(); }
                finally { _dongBoChu = false; }
            }
            if (_tb == null) Invalidate();
        }
        private void DatConTroCuoi()
        {
            if (_tb == null || _tb.IsDisposed) return;
            _tb.SelectionStart = _tb.TextLength;
            _tb.SelectionLength = 0;
        }
        private void Tb_KeyDown(object? sender, KeyEventArgs e)
        {
            if ((e.KeyCode == Keys.Down && e.Alt) || e.KeyCode == Keys.F4) { MoDanhSach(); e.Handled = e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Down) { Chon(_cb.SelectedIndex + 1); e.Handled = e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Up) { Chon(Math.Max(0, _cb.SelectedIndex - 1)); e.Handled = e.SuppressKeyPress = true; }
        }
        // ---------- Vẽ ----------
        private static Color MauChuCua(ComboBox cb) => cb.ForeColor == SystemColors.WindowText ? MauChu : cb.ForeColor;
        private string LayChu()
        {
            if (_cb.SelectedIndex >= 0 && _cb.SelectedItem != null) return _cb.GetItemText(_cb.SelectedItem) ?? "";
            return _cb.Text ?? "";
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width < 16 || Height < 12) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float k = K;
            bool on = Enabled;
            bool mo = _popup != null;
            bool nhan = on && (mo || ContainsFocus);
            Color vien = !on ? MauVienKhoa : nhan ? MauVienChon : _hover ? MauVienHover : MauVien;
            // 1 nét viền mảnh đều 4 cạnh, chỉ đổi màu theo trạng thái (không đổi độ dày)
            var khung = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (var path = VeHinhF4.BoGoc(khung, DuongKinhBo * k))
            using (var bNen = new SolidBrush(on ? MauNen : MauNenKhoa))
            using (var pen = new Pen(vien, 1f))
            {
                g.FillPath(bNen, path);
                g.DrawPath(pen, path);
            }
            if (_tb == null)
            {
                TextRenderer.DrawText(g, LayChu(), Font, new Rectangle(LeTrai, 0, Math.Max(1, Width - LeTrai - VungNut), Height),
                    on ? MauChuCua(_cb) : MauChuKhoa,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis |
                    TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            }
            // Mũi tên mảnh, lật lên khi đang mở
            float cx = Width - VungNut / 2f - 1f, cy = Height / 2f, h = 3f * k, w = 4.2f * k;
            float dy = mo ? -1f : 1f;
            using var penM = new Pen(on ? MauMuiTen : MauChuKhoa, Math.Max(1.4f, 1.5f * k))
            { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            g.DrawLines(penM, new[] { new PointF(cx - w, cy - dy * h / 2f), new PointF(cx, cy + dy * h / 2f), new PointF(cx + w, cy - dy * h / 2f) });
        }
        // ---------- Trạng thái chuột / focus: chỉ vẽ lại khi thật sự đổi ----------
        private void CapNhatHover()
        {
            bool h = Enabled && ClientRectangle.Contains(PointToClient(Cursor.Position));
            if (h == _hover) return;
            _hover = h;
            Invalidate();
        }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); CapNhatHover(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); CapNhatHover(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || !Enabled) return;
            if (_tb != null)
            {
                _tb.Focus();
                if (e.X < Width - VungNut) { DatConTroCuoi(); return; }   // bấm vào vùng chữ: chỉ để gõ
            }
            else Focus();
            if (Environment.TickCount64 - _tDong < 300) return;   // vừa bấm để đóng danh sách
            MoDanhSach();
        }
        // ---------- Bàn phím ----------
        // Tab / Shift+Tab: đi tiếp theo thứ tự của ComboBox gốc trong form
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if ((keyData & Keys.KeyCode) == Keys.Tab && (keyData & (Keys.Control | Keys.Alt)) == 0)
            {
                var f = _cb.FindForm();
                if (f != null && f.SelectNextControl(_cb, (keyData & Keys.Shift) == 0, true, true, true)) return true;
            }
            return base.ProcessDialogKey(keyData);
        }
        protected override bool IsInputKey(Keys keyData) =>
            (keyData & Keys.KeyCode) is Keys.Up or Keys.Down or Keys.Home or Keys.End || base.IsInputKey(keyData);
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyCode)
            {
                case Keys.F4:
                case Keys.Space:
                    MoDanhSach(); e.Handled = true; break;
                case Keys.Down:
                    if (e.Alt) MoDanhSach(); else Chon(_cb.SelectedIndex + 1);
                    e.Handled = true; break;
                case Keys.Up: Chon(_cb.SelectedIndex - 1); e.Handled = true; break;
                case Keys.Home: Chon(0); e.Handled = true; break;
                case Keys.End: Chon(_cb.Items.Count - 1); e.Handled = true; break;
            }
        }
        protected override void OnKeyPress(KeyPressEventArgs e)   // gõ chữ đầu để nhảy tới mục
        {
            base.OnKeyPress(e);
            if (char.IsControl(e.KeyChar)) return;
            int n = _cb.Items.Count;
            for (int b = 1; b <= n; b++)
            {
                int i = (Math.Max(_cb.SelectedIndex, -1) + b) % n;
                if ((_cb.GetItemText(_cb.Items[i]) ?? "").StartsWith(e.KeyChar.ToString(), StringComparison.CurrentCultureIgnoreCase))
                { Chon(i); e.Handled = true; return; }
            }
        }
        private void Chon(int i)
        {
            if (i < 0 || i >= _cb.Items.Count || i == _cb.SelectedIndex) return;
            _cb.SelectedIndex = i;      // kích hoạt SelectedIndexChanged của code gốc
        }
        // ---------- Danh sách sổ xuống ----------
        private void MoDanhSach()
        {
            if (_popup != null || _cb.Items.Count == 0 || !Enabled) return;
            float k = K;
            var ds = new List<string>();
            int dai = 0;
            foreach (var it in _cb.Items)
            {
                string t = _cb.GetItemText(it) ?? "";
                ds.Add(t);
                dai = Math.Max(dai, TextRenderer.MeasureText(t, Font).Width);
            }
            int chon = _cb.SelectedIndex;
            if (chon < 0 && !string.IsNullOrEmpty(_cb.Text)) chon = _cb.FindStringExact(_cb.Text);
            var wa = Screen.FromControl(this).WorkingArea;
            int rong = Math.Min(Math.Max(Width, dai + (int)(48 * k)), Math.Max(Width, wa.Width / 2));
            var pop = new PopupDanhSachF12(ds, chon, rong, k, Font, Chon);
            int khe = (int)(3 * k);
            var o = PointToScreen(new Point(0, Height + khe));
            if (o.Y + pop.Height > wa.Bottom) o.Y = PointToScreen(Point.Empty).Y - pop.Height - khe;   // sát đáy: lật lên
            if (o.Y < wa.Top) o.Y = wa.Top;
            if (o.X + pop.Width > wa.Right) o.X = wa.Right - pop.Width;                              // sát phải: dịch vào
            if (o.X < wa.Left) o.X = wa.Left;
            pop.Location = o;
            pop.FormClosed += (s, e) =>
            {
                _tDong = Environment.TickCount64;
                _popup = null;
                if (IsDisposed) return;
                Invalidate();
                if (_tb != null) { _tb.Focus(); DatConTroCuoi(); }
            };
            _popup = pop;
            Invalidate();
            if (TopLevelControl is Form f) pop.Show(f); else pop.Show();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _popup?.Close();
                foreach (var c in _chuoi) { c.LocationChanged -= DongBo; c.SizeChanged -= DongBo; c.VisibleChanged -= DongBo; }
            }
            base.Dispose(disposing);
        }
    }
    /// <summary>Danh sách sổ xuống bo góc nhẹ: hover xanh nhạt, mục đang chọn có nền + dấu tick, thanh cuộn mảnh, hỗ trợ bàn phím + lăn chuột.</summary>
    [DesignerCategory("Code")]
    internal sealed class PopupDanhSachF12 : Form
    {
        private const int MaxHienThi = 8;
        private static readonly Color MauVien = Color.FromArgb(200, 206, 216);
        private static readonly Color MauHoverNen = Color.FromArgb(241, 245, 251);
        private static readonly Color MauChonNen = Color.FromArgb(225, 236, 250);
        private static readonly Color MauChonChu = Color.FromArgb(0, 95, 184);
        private static readonly Color MauChu = Color.FromArgb(30, 41, 59);
        private readonly List<string> _ds;
        private readonly Action<int> _chon;
        private readonly Font _fontDam;
        private readonly Font _font;
        private readonly float _k;
        private readonly int _cao, _le, _soHienThi, _dangChon;
        private int _dau, _hover;
        private bool _keo;
        public PopupDanhSachF12(List<string> ds, int dangChon, int rong, float k, Font font, Action<int> chon)
        {
            _ds = ds; _chon = chon; _k = k; _font = font; _dangChon = dangChon;
            _fontDam = new Font(font, FontStyle.Bold);
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.White;
            DoubleBuffered = true;
            _cao = Math.Max((int)Math.Round(26 * k), TextRenderer.MeasureText("Ag", font).Height + (int)(8 * k));
            _le = (int)Math.Round(4 * k);
            _soHienThi = Math.Min(ds.Count, MaxHienThi);
            ClientSize = new Size(rong, _soHienThi * _cao + 2 * _le);
            _hover = dangChon;
            if (dangChon >= _soHienThi) _dau = Math.Max(0, Math.Min(dangChon - _soHienThi / 2, ds.Count - _soHienThi));
            using (var path = VeHinhF4.BoGoc(new RectangleF(0, 0, Width, Height), 12f * k))
                Region = new Region(path);
            Deactivate += (s, e) => Close();
        }
        protected override CreateParams CreateParams
        {
            get { var cp = base.CreateParams; cp.ExStyle |= 0x80; return cp; }   // WS_EX_TOOLWINDOW: không hiện trong Alt+Tab
        }
        private bool CoCuon => _ds.Count > _soHienThi;
        private int VungCuonX => Width - _le - (int)(12 * _k);
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = VeHinhF4.BoGoc(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), 12f * _k))
            using (var pen = new Pen(MauVien))
                g.DrawPath(pen, path);
            int cuonW = CoCuon ? (int)(10 * _k) : 0;
            for (int v = 0; v < _soHienThi; v++)
            {
                int i = _dau + v;
                var r = new Rectangle(_le, _le + v * _cao, Width - 2 * _le - cuonW, _cao);
                bool chon = i == _dangChon, hov = i == _hover;
                if (chon || hov)
                {
                    using var p = VeHinhF4.BoGoc(new RectangleF(r.X, r.Y + 1, r.Width, r.Height - 2), 8f * _k);
                    using var b = new SolidBrush(chon ? MauChonNen : MauHoverNen);
                    g.FillPath(b, p);
                }
                TextRenderer.DrawText(g, _ds[i], chon ? _fontDam : _font,
                    new Rectangle(r.X + (int)(8 * _k), r.Y, r.Width - (int)(30 * _k), r.Height),
                    chon ? MauChonChu : MauChu,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis |
                    TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
                if (chon)   // dấu tick
                {
                    float cx = r.Right - 14f * _k, cy = r.Y + r.Height / 2f, s = 3.6f * _k;
                    using var pt = new Pen(MauChonChu, Math.Max(1.5f, 1.6f * _k)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                    g.DrawLines(pt, new[] { new PointF(cx - s, cy), new PointF(cx - s * 0.3f, cy + s * 0.75f), new PointF(cx + s, cy - s * 0.7f) });
                }
            }
            if (CoCuon)   // thanh cuộn mảnh bo tròn
            {
                int tong = _soHienThi * _cao;
                int thumb = Math.Max((int)(24 * _k), tong * _soHienThi / _ds.Count);
                int y = _le + (tong - thumb) * _dau / Math.Max(1, _ds.Count - _soHienThi);
                var t = new RectangleF(Width - _le - 6f * _k, y, 4f * _k, thumb);
                using var p = VeHinhF4.BoGoc(t, 4f * _k);
                using var b = new SolidBrush(_keo ? Color.FromArgb(148, 163, 184) : Color.FromArgb(203, 213, 225));
                g.FillPath(b, p);
            }
        }
        private int ViTri(Point p)
        {
            if (p.Y < _le || p.Y >= _le + _soHienThi * _cao) return -1;
            if (CoCuon && p.X >= VungCuonX) return -1;
            return _dau + (p.Y - _le) / _cao;
        }
        private void DatDauTheoY(int y)
        {
            float t = Math.Max(0f, Math.Min(1f, (y - _le) / (float)(_soHienThi * _cao)));
            _dau = (int)Math.Round(t * (_ds.Count - _soHienThi));
            Invalidate();
        }
        private void CuonDen(int i)
        {
            if (i < _dau) _dau = i;
            else if (i >= _dau + _soHienThi) _dau = i - _soHienThi + 1;
            _dau = Math.Max(0, Math.Min(_dau, _ds.Count - _soHienThi));
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_keo) { DatDauTheoY(e.Y); return; }
            int i = ViTri(e.Location);
            if (i != _hover) { _hover = i; Invalidate(); }
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            if (CoCuon && e.X >= VungCuonX) { _keo = true; Capture = true; DatDauTheoY(e.Y); return; }
            int i = ViTri(e.Location);
            if (i >= 0) { _chon(i); Close(); }
        }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _keo = false; Capture = false; Invalidate(); }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!CoCuon) return;
            _dau = Math.Max(0, Math.Min(_dau - Math.Sign(e.Delta) * 2, _ds.Count - _soHienThi));
            Invalidate();
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            int n = _ds.Count;
            switch (keyData)
            {
                case Keys.Escape: Close(); return true;
                case Keys.Enter: if (_hover >= 0) _chon(_hover); Close(); return true;
                case Keys.Down: _hover = Math.Min(n - 1, _hover + 1); break;
                case Keys.Up: _hover = Math.Max(0, _hover - 1); break;
                case Keys.Home: _hover = 0; break;
                case Keys.End: _hover = n - 1; break;
                case Keys.PageDown: _hover = Math.Min(n - 1, _hover + _soHienThi); break;
                case Keys.PageUp: _hover = Math.Max(0, _hover - _soHienThi); break;
                default: return base.ProcessCmdKey(ref msg, keyData);
            }
            CuonDen(_hover);
            Invalidate();
            return true;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) _fontDam.Dispose();
            base.Dispose(disposing);
        }
    }
}
