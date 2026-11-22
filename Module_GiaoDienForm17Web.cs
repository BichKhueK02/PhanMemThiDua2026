using Krypton.Toolkit;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Reflection;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Giao diện kiểu trang web cho Form17_XuatDataSangTK ("Xuất dữ liệu lưu thống kê").
    /// CHỈ đổi trình bày: giữ nguyên tên + sự kiện của comboBox1_ChonThangCanXuat, kryptonButton_XuatDuLieuSangThongKe,
    /// statusStrip1, toolStripStatusLabel1_*, toolTip1 nên logic của form không bị ảnh hưởng.
    /// Cách dùng: gọi Module_GiaoDienForm17Web.ApDung(this) ngay sau InitializeComponent().
    /// Module tự chứa (không phụ thuộc Module_GiaoDienForm4Web); thiếu control nào thì bỏ qua và giữ giao diện cũ.
    ///
    /// Bản phẳng (không viền):
    ///  - ComboBox tự vẽ (OwnerDrawFixed) => KHÔNG còn dải xanh Highlight của hệ thống ở ô chọn lẫn danh sách xổ xuống.
    ///  - Không viền thẻ / bóng / đường kẻ / viền ô chọn / viền nút; phân biệt bằng màu nền.
    ///  - Form rộng hơn.
    /// </summary>
    internal static class Module_GiaoDienForm17Web
    {
        // ===== Bảng màu (tím nhạt giống các form kiểu web khác) =====
        private static readonly Color MauNen = Color.FromArgb(245, 243, 255);        // nền form
        private static readonly Color MauTim = Color.FromArgb(124, 92, 196);         // tím nhấn
        private static readonly Color MauTimDam = Color.FromArgb(75, 46, 154);       // chữ tím đậm
        private static readonly Color MauTimNhat = Color.FromArgb(237, 231, 250);    // nền thanh trạng thái / biểu tượng
        private static readonly Color MauChuPhu = Color.FromArgb(110, 104, 140);     // chữ phụ
        // Ô chọn (không viền, chỉ đổi màu nền)
        private static readonly Color MauONen = Color.FromArgb(242, 239, 252);       // bình thường
        private static readonly Color MauONenRe = Color.FromArgb(234, 228, 249);     // rê chuột / có tiêu điểm / đang xổ
        private static readonly Color MauMucRe = Color.FromArgb(237, 231, 250);      // mục đang rê trong danh sách xổ xuống
        // Nút (đặc, không viền: màu viền = màu nền để vẫn giữ góc bo)
        private static readonly Color MauNut = Color.FromArgb(124, 92, 196);
        private static readonly Color MauNutRe = Color.FromArgb(108, 76, 182);
        private static readonly Color MauNutNhan = Color.FromArgb(92, 62, 164);
        private static readonly Color MauNutTat = Color.FromArgb(236, 233, 245);     // nút bị khóa
        private static readonly Color MauChuNutTat = Color.FromArgb(150, 144, 178);
        private const float BanKinhThe = 14f;
        private const string PhuDeThe = "Đồng bộ kết quả phân loại sang bảng thống kê thi đua";
        public static void ApDung(Form form)
        {
            if (form == null || form.IsDisposed) return;
            var tl1 = Tim<TableLayoutPanel>(form, "tableLayoutPanel1");
            var tl2 = Tim<TableLayoutPanel>(form, "tableLayoutPanel2");
            var tl3 = Tim<TableLayoutPanel>(form, "tableLayoutPanel3");
            var nhanChon = Tim<Label>(form, "label1");
            var combo = Tim<ComboBox>(form, "comboBox1_ChonThangCanXuat");
            var nut = Tim<KryptonButton>(form, "kryptonButton_XuatDuLieuSangThongKe");
            var thanhTrangThai = Tim<StatusStrip>(form, "statusStrip1");
            if (tl1 == null || tl2 == null || tl3 == null || nhanChon == null || combo == null || nut == null || thanhTrangThai == null)
            {
                Debug.WriteLine("Module_GiaoDienForm17Web: thiếu control, giữ nguyên giao diện gốc.");
                return;
            }
            var hinhNen = Tim<PictureBox>(form, "pictureBox1");
            var fonts = new List<Font>(6);
            form.Disposed += (_, _) =>
            {
                foreach (var f in fonts) f.Dispose();
                fonts.Clear();
            };
            form.SuspendLayout();
            tl1.SuspendLayout();
            try
            {
                // ---------- 1. Nền form (rộng hơn bản cũ 560x292) ----------
                form.BackColor = MauNen;
                form.ClientSize = new Size(760, 310);
                DatDoubleBuffer(form);
                // ---------- 2. Gỡ khối bố cục cũ (giữ lại các control còn dùng) ----------
                tl3.Controls.Remove(nhanChon);
                tl3.Controls.Remove(combo);
                tl3.Controls.Remove(nut);
                tl2.Controls.Remove(thanhTrangThai);
                if (hinhNen != null)
                {
                    tl2.Controls.Remove(hinhNen);
                    // Ảnh nền cũ bị kéo giãn, vỡ hạt và chiếm bộ nhớ: bỏ hẳn
                    hinhNen.Image?.Dispose();
                    hinhNen.Image = null;
                    hinhNen.Dispose();
                }
                tl1.Controls.Remove(tl2);
                tl2.Dispose(); // kéo theo tl3 (đã rỗng)
                // ---------- 3. Khung gốc ----------
                tl1.ColumnStyles.Clear();
                tl1.RowStyles.Clear();
                tl1.ColumnCount = 1;
                tl1.RowCount = 1;
                tl1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                tl1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                tl1.BackColor = MauNen;
                tl1.Margin = Padding.Empty;
                tl1.Padding = new Padding(16, 16, 16, 12);
                tl1.Dock = DockStyle.Fill;
                DatDoubleBuffer(tl1);
                // ---------- 4. Thẻ bo góc (phẳng) + bố cục bên trong ----------
                var fTieuDe = TaoFont(fonts, "Segoe UI Semibold", 13F);
                var fPhu = TaoFont(fonts, "Segoe UI", 9.5F);
                var fNhan = TaoFont(fonts, "Segoe UI Semibold", 9.5F);
                var fCombo = TaoFont(fonts, "Segoe UI", 11F);
                var fNut = TaoFont(fonts, "Segoe UI Semibold", 10.5F);
                var fTrangThai = TaoFont(fonts, "Segoe UI", 9F);
                var the = new TheWeb { Dock = DockStyle.Fill, Margin = Padding.Empty };
                var trong = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 5,
                    BackColor = Color.White,
                    Margin = Padding.Empty,
                    Padding = new Padding(28, 10, 28, 20)
                };
                trong.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                trong.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));   // tiêu đề thẻ
                trong.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));   // nhãn
                trong.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));   // ô chọn
                trong.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));   // khoảng trống co giãn
                trong.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));   // nút
                DatDoubleBuffer(trong);
                // Tiêu đề thẻ (tự vẽ: không tốn thêm control/ảnh)
                var tieuDe = new TieuDeThe(fTieuDe, fPhu, form.Text, PhuDeThe) { Dock = DockStyle.Fill, Margin = Padding.Empty };
                // Nhãn "Chọn tuần/tháng"
                nhanChon.AutoSize = false;
                nhanChon.Font = fNhan;
                nhanChon.ForeColor = MauTimDam;
                nhanChon.BackColor = Color.White;
                nhanChon.TextAlign = ContentAlignment.BottomLeft;
                nhanChon.Margin = new Padding(2, 0, 0, 4);
                nhanChon.Dock = DockStyle.Fill;
                // Ô chọn phẳng bao quanh ComboBox gốc (ComboBox tự vẽ, không còn dải xanh)
                var oChon = new OBoGoc { Dock = DockStyle.Fill, Margin = Padding.Empty, TabIndex = 0 };
                combo.Font = fCombo;
                combo.FlatStyle = FlatStyle.Flat;
                combo.BackColor = MauONen;
                combo.ForeColor = MauTimDam;
                combo.Margin = Padding.Empty;
                oChon.GanCombo(combo);
                // Nút xuất dữ liệu
                DinhDangNut(nut, fNut);
                nut.TabIndex = 1;
                trong.Controls.Add(tieuDe, 0, 0);
                trong.Controls.Add(nhanChon, 0, 1);
                trong.Controls.Add(oChon, 0, 2);
                trong.Controls.Add(nut, 0, 4);
                the.Controls.Add(trong);
                tl1.Controls.Add(the, 0, 0);
                // ---------- 5. Thanh trạng thái ----------
                form.Controls.Add(thanhTrangThai);        // sau tl1 trong z-order => dock Bottom trước, tl1 lấp phần còn lại
                thanhTrangThai.Dock = DockStyle.Bottom;
                thanhTrangThai.GripStyle = ToolStripGripStyle.Hidden;
                thanhTrangThai.SizingGrip = false;
                thanhTrangThai.BackColor = MauTimNhat;
                thanhTrangThai.ForeColor = MauTimDam;
                thanhTrangThai.Font = fTrangThai;
                thanhTrangThai.Padding = new Padding(10, 0, 10, 0);
                thanhTrangThai.Renderer = new BoVeTrangThai();
                var nhanPhienBan = TimMuc(thanhTrangThai, "toolStripStatusLabel1_PhienBanPhanMem");
                if (nhanPhienBan != null)
                {
                    nhanPhienBan.Spring = true;
                    nhanPhienBan.TextAlign = ContentAlignment.MiddleLeft;
                    nhanPhienBan.ForeColor = MauTimDam;
                }
                var nhanThongBao = TimMuc(thanhTrangThai, "toolStripStatusLabel1_ThongBaoThanhCong");
                if (nhanThongBao != null)
                {
                    nhanThongBao.Alignment = ToolStripItemAlignment.Right;
                    // Chữ đã có dấu ✔/✘ nên bỏ biểu tượng ảnh trùng lặp
                    nhanThongBao.Image?.Dispose();
                    nhanThongBao.Image = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Module_GiaoDienForm17Web.ApDung lỗi: " + ex.Message);
            }
            finally
            {
                tl1.ResumeLayout(false);
                form.ResumeLayout(true);
            }
        }
        // ================= Hàm hỗ trợ =================
        private static T? Tim<T>(Control goc, string ten) where T : Control
            => goc.Controls.Find(ten, true).OfType<T>().FirstOrDefault();
        private static ToolStripStatusLabel? TimMuc(StatusStrip ss, string ten)
            => ss.Items.Find(ten, false).OfType<ToolStripStatusLabel>().FirstOrDefault();
        private static Font TaoFont(List<Font> ds, string ten, float coChu)
        {
            var f = new Font(ten, coChu, FontStyle.Regular, GraphicsUnit.Point);
            ds.Add(f);
            return f;
        }
        // Control.DoubleBuffered là protected => bật bằng reflection (giảm nháy khi vẽ lại)
        private static void DatDoubleBuffer(Control c)
        {
            try
            {
                typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(c, true);
            }
            catch { /* không quan trọng */ }
        }
        // Nút đặc màu tím, không viền (màu viền = màu nền để vẫn giữ được góc bo)
        private static void DinhDangNut(KryptonButton nut, Font fontNut)
        {
            nut.Anchor = AnchorStyles.Right;
            nut.Margin = Padding.Empty;
            nut.Size = new Size(196, 42);
            nut.Cursor = Cursors.Hand;
            nut.TabStop = true;
            nut.StateCommon.Back.ColorStyle = PaletteColorStyle.Solid;
            nut.StateCommon.Border.DrawBorders = PaletteDrawBorders.All;
            nut.StateCommon.Border.Width = 1;
            nut.StateCommon.Border.Rounding = 10F;
            nut.StateCommon.Content.ShortText.Font = fontNut;
            DatMauNut(nut.StateNormal, MauNut, Color.White);
            DatMauNut(nut.StateTracking, MauNutRe, Color.White);
            DatMauNut(nut.StatePressed, MauNutNhan, Color.White);
            // Đang xuất (Enabled = false)
            DatMauNut(nut.StateDisabled, MauNutTat, MauChuNutTat);
            // Có tiêu điểm: giữ đúng màu tím, không để theme Krypton đổi sang xanh
            DatMauNut(nut.OverrideFocus, MauNut, Color.White);
        }
        private static void DatMauNut(PaletteTripleRedirect st, Color nen, Color chu)
        {
            st.Back.Color1 = nen;
            st.Back.Color2 = nen;
            st.Border.Color1 = nen;
            st.Border.Color2 = nen;
            st.Content.ShortText.Color1 = chu;
            st.Content.ShortText.Color2 = chu;
        }
        private static void DatMauNut(PaletteTriple st, Color nen, Color chu)
        {
            st.Back.Color1 = nen;
            st.Back.Color2 = nen;
            st.Border.Color1 = nen;
            st.Border.Color2 = nen;
            st.Content.ShortText.Color1 = chu;
            st.Content.ShortText.Color2 = chu;
        }
        private static GraphicsPath TaoDuongBoGoc(RectangleF r, float banKinh)
        {
            float d = Math.Min(banKinh * 2f, Math.Min(r.Width, r.Height));
            var p = new GraphicsPath();
            if (d <= 1f)
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
        // ================= Thẻ bo góc phẳng (không viền, không bóng) =================
        private sealed class TheWeb : Panel
        {
            private readonly SolidBrush _nen = new(Color.White);
            public TheWeb()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                Padding = new Padding(4); // chừa chỗ cho góc bo, để control con không đè lên góc
            }
            protected override void OnPaintBackground(PaintEventArgs e) { /* vẽ hết trong OnPaint */ }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(Parent?.BackColor ?? SystemColors.Control); // nền ngoài = nền cha => góc bo liền mạch
                if (Width < 16 || Height < 16) return;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var khung = TaoDuongBoGoc(new RectangleF(0f, 0f, Width - 1f, Height - 1f), BanKinhThe);
                g.FillPath(_nen, khung);
            }
            protected override void Dispose(bool disposing)
            {
                if (disposing) _nen.Dispose();
                base.Dispose(disposing);
            }
        }
        // ================= Tiêu đề thẻ: biểu tượng + tiêu đề + mô tả (tự vẽ, không đường kẻ) =================
        private sealed class TieuDeThe : Control
        {
            private readonly Font _fTieuDe;
            private readonly Font _fPhu;
            private readonly string _tieuDe;
            private readonly string _phuDe;
            private readonly SolidBrush _bienTron = new(MauTimNhat);
            private readonly Pen _net = new(MauTim, 2.2f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            public TieuDeThe(Font fTieuDe, Font fPhu, string tieuDe, string phuDe)
            {
                _fTieuDe = fTieuDe;
                _fPhu = fPhu;
                _tieuDe = tieuDe ?? "";
                _phuDe = phuDe ?? "";
                BackColor = Color.White;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                TabStop = false;
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(BackColor);
                int vung = Height;
                if (vung < 10) return;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                float k = Math.Max(1f, DeviceDpi / 96f);   // theo tỉ lệ màn hình (125%, 150%...)
                int d = (int)(48 * k);
                int y = Math.Max(0, (vung - d) / 2);
                g.FillEllipse(_bienTron, 0, y, d, d);
                float cx = d / 2f, cy = y + d / 2f;
                // khay + mũi tên đi lên (xuất dữ liệu)
                g.DrawLines(_net, new[]
                {
                    new PointF(cx - 9f * k, cy + 1f * k), new PointF(cx - 9f * k, cy + 9f * k),
                    new PointF(cx + 9f * k, cy + 9f * k), new PointF(cx + 9f * k, cy + 1f * k)
                });
                g.DrawLine(_net, cx, cy + 3f * k, cx, cy - 10f * k);
                g.DrawLines(_net, new[]
                {
                    new PointF(cx - 5.5f * k, cy - 4.5f * k), new PointF(cx, cy - 10f * k), new PointF(cx + 5.5f * k, cy - 4.5f * k)
                });
                int x = d + (int)(16 * k);
                int hTieuDe = _fTieuDe.Height;
                int hPhu = _fPhu.Height;
                int y0 = Math.Max(0, (vung - (hTieuDe + hPhu + 2)) / 2);
                const TextFormatFlags co = TextFormatFlags.Left | TextFormatFlags.NoPadding |
                                           TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix |
                                           TextFormatFlags.VerticalCenter;
                int rong = Math.Max(10, Width - x);
                TextRenderer.DrawText(g, _tieuDe, _fTieuDe, new Rectangle(x, y0, rong, hTieuDe), MauTimDam, co);
                TextRenderer.DrawText(g, _phuDe, _fPhu, new Rectangle(x, y0 + hTieuDe + 2, rong, hPhu), MauChuPhu, co);
            }
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _bienTron.Dispose();
                    _net.Dispose();
                }
                base.Dispose(disposing);
            }
        }
        // ================= Ô chọn phẳng =================
        // Cách làm:
        //  1) ComboBox chuyển sang OwnerDrawFixed: tự vẽ ô chọn + từng mục => không còn màu xanh Highlight của hệ thống
        //     (cả khi đang chọn lẫn trong danh sách xổ xuống; mục đang rê chuột chỉ tô tím nhạt).
        //  2) ComboBox được đặt trong một khung cắt (_cat) nhỏ hơn nó: viền + nút mũi tên gốc của hệ thống nằm ngoài
        //     khung nên bị cắt mất; mũi tên "v" do chính module vẽ. Giữ nguyên ComboBox nên Items/SelectedIndex/Text/Click
        //     của form không đổi.
        private sealed class OBoGoc : Panel
        {
            private readonly Panel _cat = new() { TabStop = false };
            private ComboBox? _cb;
            private bool _hover, _focus, _mo;
            private int _lech; // số px ComboBox lệch ra ngoài khung cắt (trái)
            public OBoGoc()
            {
                BackColor = Color.White;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                TabStop = false;
                _cat.BackColor = MauONen;
            }
            private Color MauNenHienTai => (_focus || _mo || _hover) ? MauONenRe : MauONen;
            public void GanCombo(ComboBox cb)
            {
                _cb = cb;
                float k = Math.Max(1f, DeviceDpi / 96f);
                cb.Dock = DockStyle.None;
                cb.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                cb.DrawMode = DrawMode.OwnerDrawFixed;
                cb.ItemHeight = cb.Font.Height + (int)(8 * k);   // mục cao hơn cho dễ bấm
                cb.MaxDropDownItems = 8;
                cb.DrawItem += VeMuc;
                Controls.Add(_cat);
                _cat.Controls.Add(cb);
                cb.TabStop = true;
                cb.MouseEnter += (_, _) => CapNhatRe();
                cb.MouseLeave += (_, _) => CapNhatRe();
                cb.GotFocus += (_, _) => { _focus = true; LamMoi(); };
                cb.LostFocus += (_, _) => { _focus = false; LamMoi(); };
                cb.DropDown += (_, _) => { _mo = true; LamMoi(); };
                cb.DropDownClosed += (_, _) => { _mo = false; LamMoi(); };
                MouseEnter += (_, _) => CapNhatRe();
                MouseLeave += (_, _) => CapNhatRe();
                // Bấm vào phần ô nằm ngoài khung cắt (mép trên/dưới) cũng mở danh sách
                MouseDown += (_, _) =>
                {
                    if (_cb == null) return;
                    _cb.Focus();
                    _cb.DroppedDown = true;
                };
                CanGiua();
                LamMoi();
            }
            private void CapNhatRe()
            {
                bool re = IsHandleCreated && ClientRectangle.Contains(PointToClient(Cursor.Position));
                if (re == _hover) return;
                _hover = re;
                LamMoi();
            }
            // Đổi màu nền ô theo trạng thái rồi vẽ lại
            private void LamMoi()
            {
                var m = MauNenHienTai;
                if (_cat.BackColor != m) _cat.BackColor = m;
                if (_cb != null)
                {
                    if (_cb.BackColor != m) _cb.BackColor = m;
                    _cb.Invalidate();
                }
                Invalidate();
            }
            private void CanGiua()
            {
                if (_cb == null) return;
                float k = Math.Max(1f, DeviceDpi / 96f);
                int le = (int)(12 * k);                 // chừa góc bo hai bên
                int bien = Math.Max(2, (int)(2 * k));   // tối thiểu cắt bỏ viền hệ thống ở trên/dưới
                _lech = Math.Max(2, (int)(3 * k));
                int cao = _cb.Height;
                int hCat = Math.Max(10, Math.Min(_cb.ItemHeight, cao - 2 * bien));
                int lechDoc = (cao - hCat) / 2;
                int wCat = Math.Max(40, Width - 2 * le);
                _cat.SetBounds(le, Math.Max(0, (Height - hCat) / 2), wCat, hCat);
                // Rộng thêm phần nút mũi tên gốc để nó nằm ngoài khung cắt
                _cb.SetBounds(-_lech, -lechDoc, wCat + _lech + SystemInformation.VerticalScrollBarWidth + (int)(8 * k), cao);
                _cb.DropDownWidth = wCat;
            }
            // Tự vẽ ô chọn + từng mục: không bao giờ dùng màu xanh Highlight
            private void VeMuc(object? sender, DrawItemEventArgs e)
            {
                if (_cb == null) return;
                var g = e.Graphics;
                float k = Math.Max(1f, DeviceDpi / 96f);
                bool phanNhap = (e.State & DrawItemState.ComboBoxEdit) != 0;
                bool re = !phanNhap && (e.State & DrawItemState.Selected) != 0;
                Color nen = phanNhap ? MauNenHienTai : re ? MauMucRe : Color.White;
                using (var b = new SolidBrush(nen))
                    g.FillRectangle(b, e.Bounds);
                int xChu = _lech + (int)(14 * k);
                int xMuiTen = _cat.Width + _lech - (int)(24 * k);
                if (e.Index >= 0 && e.Index < _cb.Items.Count)
                {
                    string? chu = _cb.GetItemText(_cb.Items[e.Index]);
                    int phai = phanNhap ? xMuiTen - (int)(12 * k) : e.Bounds.Right - (int)(8 * k);
                    var r = new Rectangle(xChu, e.Bounds.Y, Math.Max(10, phai - xChu), e.Bounds.Height);
                    TextRenderer.DrawText(g, chu, _cb.Font, r, MauTimDam,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding |
                        TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                }
                if (phanNhap)
                {
                    // mũi tên "v" (đổi chiều lên khi đang xổ)
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    float cy = e.Bounds.Y + e.Bounds.Height / 2f;
                    float h = 2.6f * k, w = 5.2f * k;
                    float dau = _mo ? h : -h, cuoi = _mo ? -h : h;
                    using var pen = new Pen(MauTim, 2f * k / 1.25f)
                    {
                        StartCap = LineCap.Round,
                        EndCap = LineCap.Round,
                        LineJoin = LineJoin.Round
                    };
                    g.DrawLines(pen, new[]
                    {
                        new PointF(xMuiTen - w, cy + dau), new PointF(xMuiTen, cy + cuoi), new PointF(xMuiTen + w, cy + dau)
                    });
                }
                // KHÔNG gọi e.DrawFocusRectangle(): bỏ khung chấm chấm
            }
            protected override void OnLayout(LayoutEventArgs levent)
            {
                base.OnLayout(levent);
                CanGiua();
            }
            protected override void OnPaintBackground(PaintEventArgs e) { /* vẽ trong OnPaint */ }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(Parent?.BackColor ?? Color.White);
                if (Width < 10 || Height < 10) return;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var p = TaoDuongBoGoc(new RectangleF(0f, 0f, Width - 1f, Height - 1f), 10f);
                using var nen = new SolidBrush(MauNenHienTai);
                g.FillPath(nen, p); // chỉ tô nền, không vẽ viền
                // Vạch nhấn mảnh phía dưới khi có tiêu điểm / đang xổ (dấu hiệu bàn phím, không phải viền)
                if (_focus || _mo)
                {
                    float k = Math.Max(1f, DeviceDpi / 96f);
                    using var pen = new Pen(MauTim, 2f * k / 1.25f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                    float y = Height - 2f * k;
                    g.DrawLine(pen, 14f * k, y, Width - 14f * k, y);
                }
            }
        }
        // ================= Thanh trạng thái phẳng, không vạch kẻ =================
        private sealed class BoVeTrangThai : ToolStripProfessionalRenderer
        {
            public BoVeTrangThai()
            {
                RoundedEdges = false;
            }
            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using var b = new SolidBrush(e.BackColor);
                e.Graphics.FillRectangle(b, e.AffectedBounds);
            }
            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                // Không vẽ viền
            }
        }
    }
}