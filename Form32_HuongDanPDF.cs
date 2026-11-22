///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using PdfiumViewer;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form32_HuongDanPDF : Form
    {
        // ====== CẤU HÌNH ======
        /// <summary>
        /// Form bị ẩn quá N phút thì giải phóng tài liệu PDF khỏi RAM (hiện lại sẽ tự nạp lại đúng trang đang xem).
        /// 0 = tắt, luôn giữ trong RAM.
        /// </summary>
        private const int TuGiaiPhongSauPhut = 3;
        private PdfViewer pdfViewer = null!;
        private readonly Label _lblDangTai;
        // Font tạo lười (lần đầu cần mới tạo, trên luồng UI) để gọi static DamBaoPdfium() từ luồng nền
        // không kéo theo việc khởi tạo Font / Module_HeThong.
        private static Font? _fontDangTai;
        private static Font LayFontDangTai()
            => _fontDangTai ??= new Font(Module_HeThong.TenFontHeThong, 12F, FontStyle.Italic);
        // ====== Nạp thư viện PDFium (dùng chung toàn app, thread-safe) ======
        private static readonly object _khoaPdfium = new();
        private static bool _daDangKyPdfium = false;
        //private static IntPtr _pdfiumHandle = IntPtr.Zero;
        private static string? _loiPdfium;
        // ====== Trạng thái tài liệu đang mở ======
        private int _loadVersion = 0;               // tăng mỗi lần nạp/đóng -> hủy các lần nạp cũ đang chạy nền
        private string? _pathDangMo;
        private long _kichThuocDangMo;
        private DateTime _ngaySuaDangMo;
        private Task<bool>? _taskDangNap;           // lần nạp đang chạy (gộp các lần gọi trùng cùng 1 tệp)
        private string? _pathDangNap;
        private string? _pathNho;                   // tệp nạp thành công gần nhất (để tự nạp lại sau khi giải phóng RAM)
        private int _trangKhoiPhuc;                 // trang đang xem lúc giải phóng RAM
        private System.Windows.Forms.Timer? _timerGiaiPhong;
        public Form32_HuongDanPDF()
        {
            InitializeComponent();
            SuspendLayout();
            pdfViewer = new PdfViewer
            {
                Dock = DockStyle.Fill,
                ShowToolbar = true,
                ZoomMode = PdfViewerZoomMode.FitWidth   // mở ra là vừa chiều rộng, đọc ngay không phải phóng to
            };
            Controls.Add(pdfViewer);
            // Nhãn "Đang tải..." phủ lên viewer trong lúc PDF nạp ở luồng nền -> form hiện ra NGAY, không khựng
            _lblDangTai = new Label
            {
                Text = "Đang tải tài liệu hướng dẫn, vui lòng đợi trong giây lát...",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = LayFontDangTai(),
                ForeColor = MauChuPhu,
                BackColor = MauNenTrang,
                Visible = false
            };
            Controls.Add(_lblDangTai);
            _lblDangTai.BringToFront();
            TrangTriGiaoDien();
            ResumeLayout(false);
            FormClosed += (s, e) =>
            {
                _timerGiaiPhong?.Stop();
                _timerGiaiPhong?.Dispose();
                _timerGiaiPhong = null;
                DisposePdf();
                _khoiDem?.Dispose();
                _khoiDem = null;
            };
        }
        // ================= GIAO DIỆN =================
        private static readonly Color MauNenTrang = Color.FromArgb(226, 230, 236);   // nền xám nhạt quanh trang PDF
        private static readonly Color MauThanhCongCu = Color.White;
        private static readonly Color MauVien = Color.FromArgb(218, 222, 229);
        private static readonly Color MauNhan = Color.FromArgb(37, 99, 235);           // xanh nhấn (hover/chọn)
        private static readonly Color MauNhanNhat = Color.FromArgb(232, 240, 254);
        private static readonly Color MauNhanDam = Color.FromArgb(207, 222, 252);
        private static readonly Color MauChuChinh = Color.FromArgb(31, 41, 55);
        private static readonly Color MauChuPhu = Color.FromArgb(100, 116, 139);
        private static readonly Color MauNenMucLuc = Color.FromArgb(248, 250, 252);
        private static Font? _fontGiaoDien, _fontTieuDeMucLuc;
        private static Font LayFontGiaoDien()
            => _fontGiaoDien ??= new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Regular);
        private static Font LayFontTieuDeMucLuc()
            => _fontTieuDeMucLuc ??= new Font(Module_HeThong.TenFontHeThong, 10.5F, FontStyle.Bold);
        private ToolStripLabel? _lblTrang, _lblZoom;
        private TreeView? _cayMucLuc;
        // ================= MỤC LỤC TỰ VẼ =================
        // Chỉ 2 màu cho gọn mắt: mục mẹ (cấp 0) xanh dương đậm, mục con xám than.
        private static readonly Color MauMucMe = Color.FromArgb(29, 78, 216);
        private static readonly Color MauMucCon = Color.FromArgb(51, 65, 85);
        // Bút/cọ dựng sẵn 1 lần, dùng chung: vẽ mỗi dòng không cấp phát GDI+ mới (đỡ RAM, đỡ giật khi cuộn/sổ mục)
        private static readonly SolidBrush ButNenMucLuc = new(MauNenMucLuc);
        private static readonly SolidBrush ButNenChon = new(Pha(MauNenMucLuc, MauMucMe, 0.13f));
        private static readonly SolidBrush ButNenHover = new(Pha(MauNenMucLuc, MauMucMe, 0.06f));
        private static readonly SolidBrush ButVachChon = new(MauMucMe);
        private static readonly SolidBrush ButChamCon = new(Pha(MauNenMucLuc, MauMucCon, 0.45f));
        private static readonly Pen ButMuiTenMe = TaoButMuiTen(MauMucMe);
        private static readonly Pen ButMuiTenCon = TaoButMuiTen(Pha(MauNenMucLuc, MauMucCon, 0.7f));
        private static Pen TaoButMuiTen(Color c) => new(c, 1.8f)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round,
            LineJoin = System.Drawing.Drawing2D.LineJoin.Round
        };
        private readonly Point[] _diemMuiTen = new Point[3];
        // Khối bo góc nền chọn/hover: mọi dòng cùng cỡ -> dựng 1 lần, chỉ dựng lại khi đổi chiều rộng mục lục
        private System.Drawing.Drawing2D.GraphicsPath? _khoiDem;
        private Size _coKhoiDem;
        private static Font? _fontMucMe;
        private static Font LayFontMucMe()
            => _fontMucMe ??= new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Bold);
        private TreeNode? _nodeHover;
        private static Color Pha(Color a, Color b, float t)
            => Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        private const int TVM_SETEXTENDEDSTYLE = 0x1100 + 44;
        private const int TVS_EX_DOUBLEBUFFER = 0x0004;
        private const int TVS_EX_FADEINOUTEXPANDOS = 0x0040;
        /// <summary>
        /// Đệm kép ngay trong control gốc (hết giật khi sổ/thu mục) + tắt hiệu ứng mờ dần của nút sổ.
        /// </summary>
        private static void BatDemKep(TreeView tv)
        {
            if (!tv.IsHandleCreated) return;
            SendMessage(tv.Handle, TVM_SETEXTENDEDSTYLE,
                (IntPtr)(TVS_EX_DOUBLEBUFFER | TVS_EX_FADEINOUTEXPANDOS), (IntPtr)TVS_EX_DOUBLEBUFFER);
        }
        private void GanVeMucLuc(TreeView tv)
        {
            tv.DrawMode = TreeViewDrawMode.OwnerDrawAll;   // tự vẽ cả dòng: không còn nền xanh hệ thống khi chọn/sổ
            tv.Cursor = Cursors.Hand;
            BatDemKep(tv);
            tv.HandleCreated += (s, e) => BatDemKep(tv);
            tv.DrawNode += VeMucLuc;
            tv.MouseMove += (s, e) =>
            {
                var n = tv.GetNodeAt(e.Location);
                if (ReferenceEquals(n, _nodeHover)) return;
                VeLaiDong(tv, _nodeHover);
                _nodeHover = n;
                VeLaiDong(tv, n);
            };
            tv.MouseLeave += (s, e) =>
            {
                var cu = _nodeHover;
                _nodeHover = null;
                VeLaiDong(tv, cu);
            };
            // Đổi tài liệu / thu gọn nút đang hover -> bỏ tham chiếu cũ
            tv.AfterCollapse += (s, e) => _nodeHover = null;
        }
        private static void VeLaiDong(TreeView tv, TreeNode? n)
        {
            if (n == null || n.TreeView != tv) return;
            var b = n.Bounds;
            if (b.Height > 0) tv.Invalidate(new Rectangle(0, b.Y, tv.ClientSize.Width, b.Height));
        }
        private void VeMucLuc(object? sender, DrawTreeNodeEventArgs e)
        {
            if (sender is not TreeView tv || e.Node == null || e.Bounds.Height <= 0 || e.Bounds.Width <= 0) return;
            var node = e.Node;
            var g = e.Graphics;
            var hang = new Rectangle(0, e.Bounds.Y, tv.ClientSize.Width, e.Bounds.Height);
            bool chon = ReferenceEquals(tv.SelectedNode, node);
            bool hover = !chon && ReferenceEquals(_nodeHover, node);
            bool laMe = node.Level == 0;
            g.FillRectangle(ButNenMucLuc, hang);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            // ---- Nền chọn / hover: khối bo góc dựng sẵn, có vạch xanh bên trái khi chọn ----
            if (chon || hover)
            {
                var co = new Size(hang.Width - 10, hang.Height - 4);
                if (_khoiDem == null || _coKhoiDem != co)
                {
                    _khoiDem?.Dispose();
                    _khoiDem = KhoiBoGoc(new Rectangle(Point.Empty, co), 6);
                    _coKhoiDem = co;
                }
                var tt = g.Save();
                g.TranslateTransform(hang.X + 4, hang.Y + 2);
                g.FillPath(chon ? ButNenChon : ButNenHover, _khoiDem);
                if (chon) g.FillRectangle(ButVachChon, 0, 5, 3, co.Height - 10);
                g.Restore(tt);
            }
            // ---- Mũi tên sổ/thu (nằm đúng ô nút +/- gốc -> bấm vẫn sổ/thu như cũ) hoặc chấm cho mục lá ----
            int cx = node.Bounds.Left - tv.Indent / 2;
            int cy = hang.Y + hang.Height / 2;
            if (node.Nodes.Count > 0)
            {
                var p = _diemMuiTen;
                if (node.IsExpanded)
                { p[0] = new(cx - 4, cy - 2); p[1] = new(cx, cy + 2); p[2] = new(cx + 4, cy - 2); }
                else
                { p[0] = new(cx - 2, cy - 4); p[1] = new(cx + 2, cy); p[2] = new(cx - 2, cy + 4); }
                g.DrawLines(laMe ? ButMuiTenMe : ButMuiTenCon, p);
            }
            else
            {
                int r = laMe ? 3 : 2;
                g.FillEllipse(laMe ? ButVachChon : ButChamCon, cx - r, cy - r, r * 2, r * 2);
            }
            // ---- Chữ: mục mẹ đậm xanh; mục con xám than (đang chọn thì xanh) ----
            Color mauChu = laMe || chon ? MauMucMe : MauMucCon;
            var vungChu = new Rectangle(node.Bounds.Left + 2, hang.Y, Math.Max(0, hang.Right - node.Bounds.Left - 10), hang.Height);
            TextRenderer.DrawText(g, node.Text, laMe ? LayFontMucMe() : tv.Font, vungChu, mauChu,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
        private static System.Drawing.Drawing2D.GraphicsPath KhoiBoGoc(Rectangle r, int ban)
        {
            int d = ban * 2;
            var p = new System.Drawing.Drawing2D.GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
        private sealed class BangMauThanhCongCu : ProfessionalColorTable
        {
            public override Color ToolStripGradientBegin => MauThanhCongCu;
            public override Color ToolStripGradientMiddle => MauThanhCongCu;
            public override Color ToolStripGradientEnd => MauThanhCongCu;
            public override Color ToolStripBorder => MauVien;
            public override Color ButtonSelectedHighlight => MauNhanNhat;
            public override Color ButtonSelectedGradientBegin => MauNhanNhat;
            public override Color ButtonSelectedGradientMiddle => MauNhanNhat;
            public override Color ButtonSelectedGradientEnd => MauNhanNhat;
            public override Color ButtonSelectedBorder => MauNhan;
            public override Color ButtonPressedGradientBegin => MauNhanDam;
            public override Color ButtonPressedGradientMiddle => MauNhanDam;
            public override Color ButtonPressedGradientEnd => MauNhanDam;
            public override Color ButtonPressedBorder => MauNhan;
            public override Color SeparatorDark => MauVien;
            public override Color SeparatorLight => MauThanhCongCu;
        }
        /// <summary>Chỉnh thanh công cụ, mục lục, nền trang của PdfViewer: gọn, sáng, tiếng Việt.</summary>
        private void TrangTriGiaoDien()
        {
            try
            {
                BackColor = MauNenTrang;
                var r = pdfViewer.Renderer;
                r.BackColor = MauNenTrang;
                // ---- Thanh công cụ ----
                var ts = pdfViewer.Controls.OfType<ToolStrip>().FirstOrDefault();
                if (ts != null)
                {
                    ts.SuspendLayout();
                    ts.GripStyle = ToolStripGripStyle.Hidden;
                    ts.Renderer = new ToolStripProfessionalRenderer(new BangMauThanhCongCu()) { RoundedEdges = false };
                    ts.BackColor = MauThanhCongCu;
                    ts.Font = LayFontGiaoDien();
                    ts.Padding = new Padding(8, 4, 8, 4);
                    ts.ImageScalingSize = new Size(20, 20);
                    ts.ShowItemToolTips = true;
                    // Nút gốc của PdfiumViewer: [Lưu][In] | [Phóng to][Thu nhỏ] -> tooltip tiếng Việt
                    var nutGoc = ts.Items.OfType<ToolStripButton>().ToList();
                    string[] tip = { "Lưu bản sao tài liệu", "In tài liệu", "Phóng to (Ctrl + lăn chuột)", "Thu nhỏ (Ctrl + lăn chuột)" };
                    for (int i = 0; i < nutGoc.Count && i < tip.Length; i++)
                    {
                        nutGoc[i].ToolTipText = tip[i];
                        nutGoc[i].Margin = new Padding(2, 1, 2, 1);
                    }
                    ts.Items.Add(new ToolStripSeparator());
                    ts.Items.Add(TaoNut("↔  Vừa chiều rộng", "Hiển thị trang vừa khít chiều rộng khung",
                        () => DatCheDoVua(PdfViewerZoomMode.FitWidth)));
                    ts.Items.Add(TaoNut("▣  Vừa trang", "Hiển thị trọn một trang trong khung",
                        () => DatCheDoVua(PdfViewerZoomMode.FitBest)));
                    ts.Items.Add(new ToolStripSeparator());
                    ts.Items.Add(TaoNut("◀", "Trang trước (Page Up)", () => ChuyenTrang(-1)));
                    _lblTrang = new ToolStripLabel("Trang – / –") { ForeColor = MauChuChinh, Margin = new Padding(6, 1, 6, 1) };
                    ts.Items.Add(_lblTrang);
                    ts.Items.Add(TaoNut("▶", "Trang sau (Page Down)", () => ChuyenTrang(+1)));
                    _lblZoom = new ToolStripLabel("100%")
                    {
                        Alignment = ToolStripItemAlignment.Right,
                        ForeColor = MauChuPhu,
                        ToolTipText = "Mức thu phóng hiện tại"
                    };
                    ts.Items.Add(_lblZoom);
                    ts.ResumeLayout(true);
                }
                // ---- Mục lục (bookmark) bên trái ----
                _splitMucLuc ??= TimSplitContainer(pdfViewer);
                if (_splitMucLuc != null)
                {
                    _splitMucLuc.BackColor = MauVien;   // màu đường chia giữa mục lục và trang
                    _splitMucLuc.SplitterWidth = 1;
                    var p1 = _splitMucLuc.Panel1;
                    p1.BackColor = MauNenMucLuc;
                    _splitMucLuc.Panel2.BackColor = MauNenTrang;
                    _cayMucLuc = p1.Controls.OfType<TreeView>().FirstOrDefault();
                    if (_cayMucLuc != null)
                    {
                        var tv = _cayMucLuc;
                        tv.BorderStyle = BorderStyle.None;
                        tv.BackColor = MauNenMucLuc;
                        tv.ForeColor = MauChuChinh;
                        tv.Font = LayFontGiaoDien();
                        tv.ItemHeight = Math.Max(tv.ItemHeight, (int)(LayFontGiaoDien().Height * 1.9));
                        tv.ShowLines = false;
                        tv.FullRowSelect = true;
                        tv.HideSelection = false;
                        tv.HotTracking = false;   // tự vẽ hover, không gạch chân kiểu mặc định
                        tv.Indent = 20;
                        GanVeMucLuc(tv);
                        var tieuDe = new Label
                        {
                            Text = "MỤC LỤC",
                            Dock = DockStyle.Top,
                            Height = 36,
                            Padding = new Padding(12, 0, 0, 0),
                            TextAlign = ContentAlignment.MiddleLeft,
                            Font = LayFontTieuDeMucLuc(),
                            ForeColor = MauNhan,
                            BackColor = MauNenMucLuc
                        };
                        var dem = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6, 0, 0, 6), BackColor = MauNenMucLuc };
                        p1.SuspendLayout();
                        p1.Controls.Remove(tv);
                        dem.Controls.Add(tv);
                        p1.Controls.Add(dem);
                        p1.Controls.Add(tieuDe);
                        dem.BringToFront();   // Fill dock sau cùng -> chiếm phần còn lại dưới tiêu đề
                        p1.ResumeLayout(true);
                    }
                }
                // ---- Cập nhật "Trang x / y" và mức thu phóng ----
                r.Scroll += (s, e) => CapNhatThanhTrangThai();
                r.ZoomChanged += (s, e) => CapNhatThanhTrangThai();
                r.DisplayRectangleChanged += (s, e) => CapNhatThanhTrangThai();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[TrangTriGiaoDien] " + ex.Message);   // lỗi trang trí không được chặn việc xem PDF
            }
        }
        private static ToolStripButton TaoNut(string chu, string tip, Action bam)
        {
            var b = new ToolStripButton(chu)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                ToolTipText = tip,
                ForeColor = MauChuChinh,
                Margin = new Padding(2, 1, 2, 1),
                Padding = new Padding(4, 0, 4, 0)
            };
            b.Click += (s, e) => bam();
            return b;
        }
        private void DatCheDoVua(PdfViewerZoomMode cheDo)
        {
            try
            {
                if (pdfViewer.Document == null) return;
                int trang = pdfViewer.Renderer.Page;
                pdfViewer.ZoomMode = cheDo;
                pdfViewer.Renderer.Zoom = 1;   // Zoom = 1 nghĩa là đúng bằng chế độ "vừa" đang chọn
                pdfViewer.Renderer.Page = trang;
            }
            catch (Exception ex) { Debug.WriteLine("[DatCheDoVua] " + ex.Message); }
        }
        private void ChuyenTrang(int buoc)
        {
            try
            {
                var doc = pdfViewer.Document;
                if (doc == null) return;
                pdfViewer.Renderer.Page = Math.Clamp(pdfViewer.Renderer.Page + buoc, 0, doc.PageCount - 1);
                pdfViewer.Renderer.Focus();   // để Page Up/Down, lăn chuột tiếp tục tác dụng lên trang
            }
            catch (Exception ex) { Debug.WriteLine("[ChuyenTrang] " + ex.Message); }
        }
        private int _trangHienThi = -2, _zoomHienThi = -1;
        private void CapNhatThanhTrangThai()
        {
            try
            {
                if (IsDisposed || _lblTrang == null || _lblZoom == null) return;
                var doc = pdfViewer.Document;
                int trang = doc == null ? -1 : pdfViewer.Renderer.Page;
                int zoom = (int)Math.Round(pdfViewer.Renderer.Zoom * 100);
                // Scroll bắn liên tục: chỉ đổi chữ khi giá trị đổi, tránh vẽ lại thanh công cụ vô ích
                if (trang != _trangHienThi)
                {
                    _trangHienThi = trang;
                    _lblTrang.Text = doc == null ? "Trang – / –" : $"Trang {trang + 1} / {doc.PageCount}";
                }
                if (zoom != _zoomHienThi)
                {
                    _zoomHienThi = zoom;
                    _lblZoom.Text = $"{zoom}%";
                }
            }
            catch { }
        }
        /// <summary>
        /// Nạp trước thư viện PDFium (DLL ~vài chục MB, lần nạp đầu là chỗ chậm nhất). Gọi từ luồng nền khi rảnh.
        /// An toàn gọi nhiều lần / từ nhiều luồng.
        /// </summary>
        public static bool DamBaoPdfium()
        {
            lock (_khoaPdfium)
            {
                if (_daDangKyPdfium) return true;
                IntPtr daNap = IntPtr.Zero;
                try
                {
                    // Phần mềm chỉ phát hành x64
                    string pdfPath = Path.Combine(Module_DanduongGPS.ThuMucHuongDan, "pdfium-x64.dll");
                    if (!File.Exists(pdfPath))
                    {
                        _loiPdfium = $"Lỗi bộ cài: Không tìm thấy thư viện PDFium:\n\n{pdfPath}\n\n" +
                                     "Vui lòng kiểm tra lại thư mục HuongDanSuDung.";
                        return false;
                    }
                    // Nạp DLL ngay (nặng nhất) rồi mới đăng ký resolver trả về chính handle này
                    daNap = NativeLibrary.Load(pdfPath);
                    IntPtr handle = daNap;
                    NativeLibrary.SetDllImportResolver(
                        typeof(PdfDocument).Assembly,
                        (name, assembly, path) =>
                            string.Equals(name, "pdfium.dll", StringComparison.OrdinalIgnoreCase) ? handle : IntPtr.Zero);
                    //_pdfiumHandle = handle;
                    _daDangKyPdfium = true;
                    _loiPdfium = null;
                    return true;
                }
                catch (Exception ex)
                {
                    _loiPdfium = $"Lỗi nạp thư viện PDFium:\n\n{ex.Message}";
                    // Nạp DLL được mà đăng ký resolver lỗi: trả handle lại, không để rò
                    if (daNap != IntPtr.Zero && !_daDangKyPdfium)
                    {
                        try { NativeLibrary.Free(daNap); } catch { }
                    }
                    return false;
                }
            }
        }
        /// <summary>Nạp sẵn PDFium ở nền, im lặng (Form2 gọi bằng Task.Run sau khi form chính nạp xong).</summary>
        public static void NapTruocThuVien()
        {
            try { DamBaoPdfium(); }
            catch (Exception ex) { Debug.WriteLine("[NapTruocThuVien] " + ex.Message); }
        }
        /// <summary>
        /// Nạp PDF: DLL + parse tài liệu chạy ở luồng nền, UI luôn mượt. Bỏ qua nếu đang mở đúng tệp này (chưa đổi).
        /// Gọi trùng cùng 1 tệp khi đang nạp thì dùng chung 1 lần nạp (không parse 2 lần).
        /// </summary>
        public async Task<bool> LoadPdfAsync(string path)
        {
            if (IsDisposed) return false;
            if (!File.Exists(path))
            {
                MessageBox.Show("File PDF không tồn tại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            string full = Path.GetFullPath(path);
            var fi = new FileInfo(full);
            // Đã mở đúng tệp này và tệp không đổi -> không nạp lại
            if (pdfViewer.Document != null &&
                string.Equals(_pathDangMo, full, StringComparison.OrdinalIgnoreCase) &&
                _kichThuocDangMo == fi.Length && _ngaySuaDangMo == fi.LastWriteTimeUtc)
            {
                _lblDangTai.Visible = false;
                return true;
            }
            // Đang nạp đúng tệp này rồi -> chờ chung kết quả
            if (_taskDangNap != null && string.Equals(_pathDangNap, full, StringComparison.OrdinalIgnoreCase))
            {
                return await _taskDangNap;
            }
            int ver = ++_loadVersion;
            _pathDangNap = full;
            Task<bool> job = NapAsync(full, fi.Length, fi.LastWriteTimeUtc, ver);
            _taskDangNap = job;
            try
            {
                return await job;
            }
            finally
            {
                if (ReferenceEquals(_taskDangNap, job))
                {
                    _taskDangNap = null;
                    _pathDangNap = null;
                }
            }
        }
        private async Task<bool> NapAsync(string full, long kichThuoc, DateTime ngaySuaUtc, int ver)
        {
            _lblDangTai.Visible = true;
            _lblDangTai.BringToFront();
            PdfDocument? doc = null;
            try
            {
                doc = await Task.Run(() =>
                {
                    if (!DamBaoPdfium())
                        throw new InvalidOperationException(_loiPdfium ?? "Không nạp được thư viện PDFium.");
                    return PdfDocument.Load(full);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi load PDF: {ex.Message}");
                if (IsDisposed) return false;
                _lblDangTai.Visible = false;
                if (!string.IsNullOrEmpty(_loiPdfium))
                    MessageBox.Show(_loiPdfium, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Fallback: Mở bằng ứng dụng mặc định hệ thống
                TaTimThayEm_OpenPdfExternally(full);
                Close();
                return false;
            }
            // Form đã đóng / có lần nạp mới hơn trong lúc chờ -> bỏ kết quả này
            if (IsDisposed || ver != _loadVersion)
            {
                doc.Dispose();
                return false;
            }
            var cu = pdfViewer.Document;
            // Gom việc dựng cây mục lục thành 1 lần vẽ (không vẽ lại sau từng nút)
            _cayMucLuc?.BeginUpdate();
            try { pdfViewer.Document = doc; }   // gán tài liệu mới TRƯỚC rồi mới giải phóng tài liệu cũ
            finally { _cayMucLuc?.EndUpdate(); }
            try { cu?.Dispose(); } catch { }
            _daChinhMucLuc = false;
            BeginInvoke(new Action(DatDoRongMucLuc));   // chờ viewer dựng xong mục lục rồi mới chỉnh
            // Nạp lại sau khi giải phóng RAM -> trả về đúng trang đang xem (chỉ khi cùng tệp)
            if (_trangKhoiPhuc > 0 && string.Equals(_pathNho, full, StringComparison.OrdinalIgnoreCase))
            {
                try { pdfViewer.Renderer.Page = _trangKhoiPhuc; } catch { }
            }
            _trangKhoiPhuc = 0;
            _pathDangMo = full;
            _pathNho = full;
            _kichThuocDangMo = kichThuoc;
            _ngaySuaDangMo = ngaySuaUtc;
            _lblDangTai.Visible = false;
            CapNhatThanhTrangThai();
            return true;
        }
        private void TaTimThayEm_OpenPdfExternally(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở file: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // ================= GIẢI PHÓNG RAM KHI ẨN LÂU =================
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (IsDisposed || pdfViewer == null) return;
            if (Visible)
            {
                _timerGiaiPhong?.Stop();
                // Form vừa hiện mà mục lục chưa được chỉnh độ rộng -> chỉnh ngay
                if (!_daChinhMucLuc && pdfViewer.Document != null)
                    BeginInvoke(new Action(DatDoRongMucLuc));
                // Hiện lại sau khi đã giải phóng RAM mà chưa ai gọi nạp -> tự nạp lại tệp cũ
                if (pdfViewer.Document == null && _taskDangNap == null
                    && _pathNho != null && File.Exists(_pathNho))
                {
                    _ = LoadPdfAsync(_pathNho);
                }
            }
            else if (TuGiaiPhongSauPhut > 0 && pdfViewer.Document != null)
            {
                _timerGiaiPhong ??= TaoTimerGiaiPhong();
                _timerGiaiPhong.Stop();
                _timerGiaiPhong.Start();
            }
        }
        private System.Windows.Forms.Timer TaoTimerGiaiPhong()
        {
            var t = new System.Windows.Forms.Timer { Interval = TuGiaiPhongSauPhut * 60_000 };
            t.Tick += (s, e) =>
            {
                t.Stop();
                if (!IsDisposed && !Visible && pdfViewer.Document != null)
                {
                    try { _trangKhoiPhuc = pdfViewer.Renderer.Page; } catch { _trangKhoiPhuc = 0; }
                    GiaiPhongTaiLieu(giuDuongDan: true);
                }
            };
            return t;
        }
        // ====== Độ rộng panel Mục lục (bookmark) bên trái ======
        private const double TyLeMucLuc = 0.33;   // 0.33 = 33% chiều rộng form. Muốn rộng hơn thì tăng (vd 0.40)
        private SplitContainer? _splitMucLuc;
        private bool _daChinhMucLuc = false;
        private static SplitContainer? TimSplitContainer(Control goc)
        {
            foreach (Control c in goc.Controls)
            {
                if (c is SplitContainer sc) return sc;
                var con = TimSplitContainer(c);
                if (con != null) return con;
            }
            return null;
        }
        private void DatDoRongMucLuc()
        {
            try
            {
                if (IsDisposed || pdfViewer == null) return;
                _splitMucLuc ??= TimSplitContainer(pdfViewer);
                var sc = _splitMucLuc;
                if (sc == null || sc.Panel1Collapsed || sc.Width <= 0) return;
                int mong = (int)(sc.Width * TyLeMucLuc);
                int toiDa = sc.Width - sc.Panel2MinSize - sc.SplitterWidth;
                int d = Math.Max(sc.Panel1MinSize, Math.Min(mong, toiDa));
                if (d > 0 && sc.SplitterDistance != d)
                    sc.SplitterDistance = d;
                _daChinhMucLuc = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[DatDoRongMucLuc] " + ex.Message);
            }
        }
        private void GiaiPhongTaiLieu(bool giuDuongDan)
        {
            _loadVersion++;   // hủy lần nạp nền đang chờ (nếu có)
            _pathDangMo = null;
            if (!giuDuongDan)
            {
                _pathNho = null;
                _trangKhoiPhuc = 0;
            }
            try
            {
                if (!_lblDangTai.IsDisposed) _lblDangTai.Visible = false;
                if (pdfViewer?.Document != null)
                {
                    var doc = pdfViewer.Document;
                    pdfViewer.Document = null;
                    doc.Dispose();
                }
                CapNhatThanhTrangThai();
            }
            catch { }
        }
        /// <summary>Đóng hẳn tài liệu và quên đường dẫn (dùng khi đóng form).</summary>
        public void DisposePdf() => GiaiPhongTaiLieu(giuDuongDan: false);
    }
}