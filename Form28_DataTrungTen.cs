/////TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
//using Microsoft.Data.Sqlite;
//using System.ComponentModel;
//using System.Drawing.Drawing2D;
//using System.Globalization;
//using System.Reflection;
//using System.Runtime.InteropServices;
//using System.Text;
//namespace PhanMemThiDua2026
//{
//    public partial class Form28_DataTrungTen : Form
//    {
//        #region Hằng số & trường dữ liệu
//        private const int EM_SETCUEBANNER = 0x1501;
//        private const int MaxRecords = 500_000;
//        private const int MaxDetailCache = 200;
//        private const int SqlChunkSize = 900;
//        private const string TenChuaCoDonVi = "(Chưa có đơn vị)";
//        private static readonly TextInfo _textInfo = CultureInfo.CurrentCulture.TextInfo;
//        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
//        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, string lParam);
//        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
//        private string? _connectionString;
//        private List<TrungTenModel> _data = new();
//        private List<TrungTenModel> _viewData = new();
//        private List<TrungTenDetailModel> _viewDataDetail = new();
//        private TrungTenModel? _modelDangHienThi;
//        private readonly Dictionary<string, List<TrungTenDetailModel>> _detailCache = new(StringComparer.OrdinalIgnoreCase);
//        private int _isLoading;
//        private int _busyCount;
//        private int _soBanGhiLoi;
//        private bool _boQuaSelection;
//        private bool _daKhoiTaoToolTip;
//        private readonly bool _laTanBinh;
//        private string _donViDuocChon = "";
//        private CancellationTokenSource? _loadCts;
//        private CancellationTokenSource? _detailCts;
//        private readonly System.Windows.Forms.Timer _searchTimer = new() { Interval = 250 };
//        private readonly System.Windows.Forms.Timer _selectTimer = new() { Interval = 120 };
//        private readonly List<Font> _ownedFonts = new();
//        // --- Controls ---
//        private Panel _header = null!;
//        private ModernButton _btnLamMoi = null!;
//        private ModernButton _btnDong = null!;
//        private ModernButton _btnTimKiem = null!;
//        private Button _btnXoaTimKiem = null!;
//        private TextBox _txtSearch = null!;
//        private Label _kpiTen = null!;
//        private Label _kpiHoSo = null!;
//        private Label _kpiMax = null!;
//        private Label _lblMasterCount = null!;
//        private Label _lblEmptyMaster = null!;
//        private Label _lblDetailTitle = null!;
//        private Label _lblDetailSub = null!;
//        private Label _lblChipHint = null!;
//        private Label _lblEmptyDetail = null!;
//        private TableLayoutPanel _tlpDetail = null!;
//        private FlowLayoutPanel _chipPanel = null!;
//        private DataGridView _gridMaster = null!;
//        private DataGridView _gridDetail = null!;
//        private StatusStrip _statusStrip = null!;
//        private ToolStripProgressBar _progress = null!;
//        private ToolStripStatusLabel _lblStatus = null!;
//        private ToolStripStatusLabel _lblVersion = null!;
//        private ContextMenuStrip _ctxMenu = null!;
//        private ToolStripMenuItem _mnuLamMoi = null!;
//        private ToolStripMenuItem _mnuXoaTimKiem = null!;
//        private ToolStripMenuItem _mnuDong = null!;
//        private ToolTip _toolTip = null!;
//        private Font _boldFont = null!;
//        private Font _chipFont = null!;
//        #endregion
//        #region Khởi tạo
//        public Form28_DataTrungTen()
//        {
//            components = new Container();
//            _laTanBinh = LaTanBinh();
//            BuildUi();
//            FormBorderStyle = FormBorderStyle.FixedDialog;
//            StartPosition = FormStartPosition.CenterScreen;
//            MaximizeBox = false;
//            AcceptButton = _btnTimKiem;
//            _searchTimer.Tick += (_, _) => { _searchTimer.Stop(); ApDungBoLoc(); };
//            _selectTimer.Tick += (_, _) => { _selectTimer.Stop(); TaiChiTietAsync(); };
//            Load += Form28_DataTrungTen_Load;
//        }
//        private static bool LaTanBinh()
//        {
//            string pb = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
//            return pb.Contains("tân binh", StringComparison.OrdinalIgnoreCase);
//        }
//        private async void Form28_DataTrungTen_Load(object? sender, EventArgs e)
//        {
//            Module_MenuChuotPhai.TichHopGiaoDien(_ctxMenu);
//            XacDinhPhienBan();
//            SetupPlaceholder();
//            InitToolTips();
//            await TaiDuLieuHeThongAsync();
//        }
//        private Font NewFont(string family, float size, FontStyle style = FontStyle.Regular)
//        {
//            var f = new Font(family, size, style, GraphicsUnit.Point);
//            _ownedFonts.Add(f);
//            return f;
//        }
//        private Image? ResImg(string name)
//        {
//            try
//            {
//                var rm = new ComponentResourceManager(typeof(Form28_DataTrungTen));
//                return rm.GetObject(name) as Image;
//            }
//            catch { return null; }
//        }
//        private void GiaiPhongTaiNguyenTuyChinh()
//        {
//            _searchTimer.Dispose();
//            _selectTimer.Dispose();
//            _loadCts?.Dispose();
//            _detailCts?.Dispose();
//            foreach (var f in _ownedFonts) f.Dispose();
//            _ownedFonts.Clear();
//        }
//        #endregion
//        #region Dựng giao diện (kiểu web / card)
//        private void BuildUi()
//        {
//            SuspendLayout();
//            AutoScaleDimensions = new SizeF(7F, 15F);
//            AutoScaleMode = AutoScaleMode.Font;
//            ClientSize = new Size(1264, 681);
//            Font = NewFont("Segoe UI", 9.75F);
//            BackColor = UiColors.Bg;
//            Text = "Phân tích CBCS trùng tên";
//            ShowInTaskbar = false;
//            try
//            {
//                var icon = new ComponentResourceManager(typeof(Form28_DataTrungTen)).GetObject("$this.Icon") as Icon;
//                if (icon != null) Icon = icon;
//            }
//            catch { /* icon không bắt buộc */ }
//            var fTitle = NewFont("Segoe UI Semibold", 15F);
//            var fCaption = NewFont("Segoe UI Semibold", 8.25F);
//            var fKpi = NewFont("Segoe UI Semibold", 20F);
//            var fKpiText = NewFont("Segoe UI Semibold", 12F);
//            var fCardTitle = NewFont("Segoe UI Semibold", 11.5F);
//            var fSmall = NewFont("Segoe UI", 9F);
//            var fBtn = NewFont("Segoe UI Semibold", 9.75F);
//            var fInput = NewFont("Segoe UI", 11F);
//            var fIcon = NewFont("Segoe MDL2 Assets", 12F);
//            _boldFont = NewFont("Segoe UI Semibold", 9.75F);
//            _chipFont = NewFont("Segoe UI", 9F);
//            // ===== Context menu =====
//            _ctxMenu = new ContextMenuStrip(components) { Font = new Font("Segoe UI", 9F), ImageScalingSize = new Size(21, 21) };
//            _ownedFonts.Add(_ctxMenu.Font);
//            _mnuLamMoi = new ToolStripMenuItem("Làm mới trang") { ShortcutKeys = Keys.F5, Image = ResImg("lamMoi_ToolStripMenuItem.Image") };
//            _mnuXoaTimKiem = new ToolStripMenuItem("Xóa tìm kiếm") { ShortcutKeys = Keys.F4, Image = ResImg("xoaTimKiem_ToolStripMenuItem.Image") };
//            _mnuDong = new ToolStripMenuItem("Đóng trang") { ShortcutKeys = Keys.Control | Keys.Q, Image = ResImg("quayLaiTrangXuLyDuLieu_ToolStripMenuItem.Image") };
//            _mnuLamMoi.Click += lamMoi_ToolStripMenuItem_Click;
//            _mnuXoaTimKiem.Click += xoaTimKiem_ToolStripMenuItem_Click;
//            _mnuDong.Click += quayLaiTrangXuLyDuLieu_ToolStripMenuItem_Click;
//            _ctxMenu.Items.AddRange(new ToolStripItem[] { _mnuLamMoi, new ToolStripSeparator(), _mnuXoaTimKiem, _mnuDong });
//            ContextMenuStrip = _ctxMenu;
//            // ===== Status strip =====
//            _statusStrip = new StatusStrip { Dock = DockStyle.Bottom, BackColor = Color.White, SizingGrip = false, ImageScalingSize = new Size(20, 20) };
//            _progress = new ToolStripProgressBar { Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 30, Size = new Size(90, 16), Visible = false };
//            _lblStatus = new ToolStripStatusLabel("Sẵn sàng") { Spring = true, TextAlign = ContentAlignment.MiddleLeft, Image = ResImg("toolStripStatusLabel1.Image"), ForeColor = UiColors.Text };
//            _lblVersion = new ToolStripStatusLabel { Alignment = ToolStripItemAlignment.Right, ForeColor = UiColors.Muted, TextAlign = ContentAlignment.MiddleRight };
//            _statusStrip.Items.AddRange(new ToolStripItem[] { _progress, _lblStatus, _lblVersion });
//            // ===== Header =====
//            _header = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Color.White };
//            _header.Paint += (s, e) =>
//            {
//                using var p = new Pen(UiColors.Border);
//                e.Graphics.DrawLine(p, 0, _header.Height - 1, _header.Width, _header.Height - 1);
//            };
//            var badge = new RoundedPanel { Location = new Point(24, 15), Size = new Size(42, 42), Radius = 12, FillColor = UiColors.GreenSoft, BorderColor = UiColors.GreenSoft };
//            badge.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "\uE716", Font = fIcon, ForeColor = UiColors.GreenDark, TextAlign = ContentAlignment.MiddleCenter });
//            var lblTitle = new Label { AutoSize = true, Text = "Phân tích CBCS trùng tên", Font = fTitle, ForeColor = UiColors.Text, Location = new Point(78, 10) };
//            var lblSub = new Label { AutoSize = true, Text = "Phát hiện và đối chiếu các họ tên trùng nhau trong cơ sở dữ liệu", Font = fSmall, ForeColor = UiColors.Muted, Location = new Point(80, 42) };
//            //_btnLamMoi = new ModernButton { Text = "Làm mới", Glyph = "\uE72C", Font = fBtn, Size = new Size(120, 38) };
//            //_btnDong = new ModernButton { Text = "Thoát", Glyph = "\uE7E8", Font = fBtn, Size = new Size(110, 38) };
//            _btnLamMoi = new ModernButton { Text = "Làm mới", Glyph = "\uE72C", Font = fBtn, Size = new Size(120, 38), IsPurple = true };
//            _btnDong = new ModernButton { Text = "Thoát", Glyph = "\uE7E8", Font = fBtn, Size = new Size(110, 38), IsPurple = true };
//            _btnLamMoi.Click += kryptonButton_LamMoi_Click;
//            _btnDong.Click += kryptonButton_Dong_Click;
//            _header.Controls.AddRange(new Control[] { badge, lblTitle, lblSub, _btnLamMoi, _btnDong });
//            _header.Resize += (_, _) => DatViTriNutHeader();
//            DatViTriNutHeader();
//            // ===== Body =====
//            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 16, 20, 12), BackColor = UiColors.Bg };
//            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = UiColors.Bg };
//            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
//            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 84F));
//            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
//            body.Controls.Add(layout);
//            // --- Toolbar: tìm kiếm + KPI ---
//            var toolbar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = UiColors.Bg, Margin = Padding.Empty };
//            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
//            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));
//            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));
//            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270F));
//            toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
//            var cardSearch = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 12), Padding = new Padding(16, 8, 10, 8) };
//            var tlpSearch = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2, BackColor = Color.White };
//            tlpSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28F));
//            tlpSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
//            tlpSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32F));
//            tlpSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 122F));
//            tlpSearch.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
//            tlpSearch.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
//            var lblCap = new Label { Text = _laTanBinh ? "HỌ VÀ TÊN TÂN BINH" : "HỌ VÀ TÊN CBCS", Font = fCaption, ForeColor = UiColors.Muted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
//            tlpSearch.Controls.Add(lblCap, 0, 0);
//            tlpSearch.SetColumnSpan(lblCap, 4);
//            tlpSearch.Controls.Add(new Label { Text = "\uE721", Font = fIcon, ForeColor = UiColors.Muted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
//            _txtSearch = new TextBox { BorderStyle = BorderStyle.None, Font = fInput, Anchor = AnchorStyles.Left | AnchorStyles.Right, BackColor = Color.White, ForeColor = UiColors.Text };
//            _txtSearch.TextChanged += TxtSearch_TextChanged;
//            _txtSearch.KeyDown += textBoxKyToon_TimTen_KeyDown;
//            tlpSearch.Controls.Add(_txtSearch, 1, 1);
//            _btnXoaTimKiem = new Button
//            {
//                Text = "\uE711",
//                Font = NewFont("Segoe MDL2 Assets", 9F),
//                FlatStyle = FlatStyle.Flat,
//                Size = new Size(26, 26),
//                Anchor = AnchorStyles.None,
//                ForeColor = UiColors.Muted,
//                BackColor = Color.White,
//                Cursor = Cursors.Hand,
//                Visible = false,
//                TabStop = false
//            };
//            _btnXoaTimKiem.FlatAppearance.BorderSize = 0;
//            _btnXoaTimKiem.FlatAppearance.MouseOverBackColor = UiColors.Hover;
//            _btnXoaTimKiem.Click += (_, _) => xoaTimKiem_ToolStripMenuItem_Click(null, EventArgs.Empty);
//            tlpSearch.Controls.Add(_btnXoaTimKiem, 2, 1);
//            _btnTimKiem = new ModernButton { Text = "Tìm kiếm", Glyph = "\uE721", Primary = true, Font = fBtn, Size = new Size(112, 34), Anchor = AnchorStyles.None };
//            _btnTimKiem.Click += kryptonButton_TimKiem_Click;
//            tlpSearch.Controls.Add(_btnTimKiem, 3, 1);
//            cardSearch.Controls.Add(tlpSearch);
//            toolbar.Controls.Add(cardSearch, 0, 0);
//            toolbar.Controls.Add(TaoKpi("TÊN TRÙNG", fCaption, fKpi, out _kpiTen, new Padding(0, 0, 12, 12)), 1, 0);
//            toolbar.Controls.Add(TaoKpi("HỒ SƠ LIÊN QUAN", fCaption, fKpi, out _kpiHoSo, new Padding(0, 0, 12, 12)), 2, 0);
//            toolbar.Controls.Add(TaoKpi("TRÙNG NHIỀU NHẤT", fCaption, fKpiText, out _kpiMax, new Padding(0, 0, 0, 12)), 3, 0);
//            layout.Controls.Add(toolbar, 0, 0);
//            // --- Hai card chính ---
//            var tlpBody = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = UiColors.Bg, Margin = Padding.Empty };
//            tlpBody.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340F));
//            tlpBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
//            tlpBody.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
//            // Card trái: danh sách tên trùng
//            var cardMaster = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0), Padding = new Padding(14, 12, 14, 12) };
//            var tlpMaster = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = Color.White };
//            tlpMaster.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
//            tlpMaster.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
//            tlpMaster.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
//            tlpMaster.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
//            tlpMaster.Controls.Add(new Label { Text = "Danh sách tên trùng", Font = fCardTitle, ForeColor = UiColors.Text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
//            _lblMasterCount = new Label { Text = "0", Font = _boldFont, ForeColor = UiColors.GreenDark, BackColor = UiColors.GreenSoft, AutoSize = true, Padding = new Padding(8, 2, 8, 2), Anchor = AnchorStyles.Right };
//            tlpMaster.Controls.Add(_lblMasterCount, 1, 0);
//            var masterHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
//            _gridMaster = TaoGrid();
//            _gridMaster.Columns.Add(new DataGridViewTextBoxColumn { Name = "HoTen", HeaderText = "Họ và tên", FillWeight = 100, MinimumWidth = 120, SortMode = DataGridViewColumnSortMode.NotSortable });
//            _gridMaster.Columns.Add(new DataGridViewTextBoxColumn
//            {
//                Name = "SoLan",
//                HeaderText = "Số lần",
//                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
//                Width = 76,
//                SortMode = DataGridViewColumnSortMode.NotSortable,
//                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
//            });
//            _gridMaster.CellValueNeeded += GridMaster_CellValueNeeded;
//            _gridMaster.CellFormatting += GridMaster_CellFormatting;
//            _gridMaster.SelectionChanged += GridMaster_SelectionChanged;
//            _lblEmptyMaster = TaoNhanTrong(fSmall);
//            masterHost.Controls.Add(_gridMaster);
//            masterHost.Controls.Add(_lblEmptyMaster);
//            tlpMaster.Controls.Add(masterHost, 0, 1);
//            tlpMaster.SetColumnSpan(masterHost, 2);
//            cardMaster.Controls.Add(tlpMaster);
//            // Card phải: chi tiết
//            var cardDetail = new RoundedPanel { Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = new Padding(18, 14, 18, 12) };
//            _tlpDetail = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, BackColor = Color.White };
//            _tlpDetail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
//            _tlpDetail.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
//            _tlpDetail.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
//            _tlpDetail.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
//            _tlpDetail.RowStyles.Add(new RowStyle(SizeType.AutoSize));
//            _tlpDetail.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
//            _lblDetailTitle = new Label { Text = "Chi tiết hồ sơ", Font = fCardTitle, ForeColor = UiColors.Text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
//            _lblDetailSub = new Label { Text = "", Font = fSmall, ForeColor = UiColors.Muted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft };
//            _lblChipHint = new Label { Text = "Đơn vị — nhấn để làm nổi bật các dòng thuộc đơn vị đó", Font = fSmall, ForeColor = UiColors.Muted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Visible = false };
//            _chipPanel = new FlowLayoutPanel
//            {
//                Dock = DockStyle.Fill,
//                AutoSize = true,
//                AutoSizeMode = AutoSizeMode.GrowAndShrink,
//                WrapContents = true,
//                FlowDirection = FlowDirection.LeftToRight,
//                Margin = Padding.Empty,
//                BackColor = Color.White,
//                MaximumSize = new Size(0, 100),
//                AutoScroll = true,
//                Visible = false
//            };
//            _tlpDetail.Controls.Add(_lblDetailTitle, 0, 0);
//            _tlpDetail.Controls.Add(_lblDetailSub, 0, 1);
//            _tlpDetail.Controls.Add(_lblChipHint, 0, 2);
//            _tlpDetail.Controls.Add(_chipPanel, 0, 3);
//            var detailHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0, 6, 0, 0) };
//            _gridDetail = TaoGrid();
//            _gridDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "STT", HeaderText = "STT", FillWeight = 6, MinimumWidth = 44, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
//            _gridDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "HoTen", HeaderText = _laTanBinh ? "Họ và tên tân binh" : "Họ và tên CBCS", FillWeight = 26, MinimumWidth = 120, SortMode = DataGridViewColumnSortMode.NotSortable });
//            _gridDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "NamSinh", HeaderText = "Năm sinh", FillWeight = 12, MinimumWidth = 70, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
//            _gridDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "CapBac", HeaderText = "Cấp bậc", FillWeight = 14, MinimumWidth = 70, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
//            _gridDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "DonVi", HeaderText = "Đơn vị", FillWeight = 30, MinimumWidth = 120, SortMode = DataGridViewColumnSortMode.NotSortable });
//            _gridDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoHieu", HeaderText = "Số hiệu", FillWeight = 12, MinimumWidth = 70, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
//            _gridDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "GhiChu", HeaderText = "Ghi chú", FillWeight = 20, MinimumWidth = 80, SortMode = DataGridViewColumnSortMode.NotSortable });
//            _gridDetail.CellValueNeeded += GridDetail_CellValueNeeded;
//            _gridDetail.CellFormatting += GridDetail_CellFormatting;
//            _lblEmptyDetail = TaoNhanTrong(fSmall);
//            _lblEmptyDetail.Text = "Chọn một họ tên ở danh sách bên trái để xem chi tiết hồ sơ.";
//            _lblEmptyDetail.Visible = true;
//            _gridDetail.Visible = false;
//            detailHost.Controls.Add(_gridDetail);
//            detailHost.Controls.Add(_lblEmptyDetail);
//            _tlpDetail.Controls.Add(detailHost, 0, 4);
//            cardDetail.Controls.Add(_tlpDetail);
//            tlpBody.Controls.Add(cardMaster, 0, 0);
//            tlpBody.Controls.Add(cardDetail, 1, 0);
//            layout.Controls.Add(tlpBody, 0, 1);
//            // Thứ tự Add: Fill trước, rồi Bottom, rồi Top
//            Controls.Add(body);
//            Controls.Add(_statusStrip);
//            Controls.Add(_header);
//            _gridMaster.ContextMenuStrip = _ctxMenu;
//            _gridDetail.ContextMenuStrip = _ctxMenu;
//            ResumeLayout(false);
//            PerformLayout();
//        }
//        private void DatViTriNutHeader()
//        {
//            if (_header == null || _btnDong == null || _btnLamMoi == null) return;
//            int top = (_header.Height - _btnDong.Height) / 2;
//            _btnDong.Location = new Point(_header.Width - 24 - _btnDong.Width, top);
//            _btnLamMoi.Location = new Point(_btnDong.Left - 10 - _btnLamMoi.Width, top);
//        }
//        private RoundedPanel TaoKpi(string caption, Font fCap, Font fVal, out Label value, Padding margin)
//        {
//            var card = new RoundedPanel { Dock = DockStyle.Fill, Margin = margin, Padding = new Padding(16, 10, 12, 8) };
//            value = new Label { Dock = DockStyle.Fill, Text = "0", Font = fVal, ForeColor = UiColors.GreenDark, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
//            var cap = new Label { Dock = DockStyle.Top, Height = 20, Text = caption, Font = fCap, ForeColor = UiColors.Muted, TextAlign = ContentAlignment.MiddleLeft };
//            card.Controls.Add(value);
//            card.Controls.Add(cap);
//            return card;
//        }
//        private static Label TaoNhanTrong(Font f) => new()
//        {
//            Dock = DockStyle.Fill,
//            TextAlign = ContentAlignment.MiddleCenter,
//            ForeColor = UiColors.Muted,
//            BackColor = Color.White,
//            Font = f,
//            Visible = false
//        };
//        private DataGridView TaoGrid()
//        {
//            var g = new DataGridView
//            {
//                Dock = DockStyle.Fill,
//                BackgroundColor = Color.White,
//                BorderStyle = BorderStyle.None,
//                AllowUserToAddRows = false,
//                AllowUserToDeleteRows = false,
//                AllowUserToResizeRows = false,
//                AllowUserToOrderColumns = false,
//                ReadOnly = true,
//                MultiSelect = false,
//                RowHeadersVisible = false,
//                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
//                EnableHeadersVisualStyles = false,
//                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
//                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
//                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
//                ColumnHeadersHeight = 38,
//                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
//                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
//                GridColor = UiColors.GridLine,
//                ShowCellToolTips = false,
//                VirtualMode = true,
//                StandardTab = true
//            };
//            g.RowTemplate.Height = 38;
//            g.ColumnHeadersDefaultCellStyle.BackColor = UiColors.HeaderBack;
//            g.ColumnHeadersDefaultCellStyle.ForeColor = UiColors.Muted;
//            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = UiColors.HeaderBack;
//            g.ColumnHeadersDefaultCellStyle.SelectionForeColor = UiColors.Muted;
//            g.ColumnHeadersDefaultCellStyle.Font = _boldFont;
//            g.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
//            g.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
//            g.DefaultCellStyle.BackColor = Color.White;
//            g.DefaultCellStyle.ForeColor = UiColors.Text;
//            g.DefaultCellStyle.SelectionBackColor = UiColors.GreenSoft;
//            g.DefaultCellStyle.SelectionForeColor = UiColors.GreenDark;
//            g.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
//            g.AlternatingRowsDefaultCellStyle.BackColor = UiColors.ZebraRow;
//            g.AlternatingRowsDefaultCellStyle.SelectionBackColor = UiColors.GreenSoft;
//            g.AlternatingRowsDefaultCellStyle.SelectionForeColor = UiColors.GreenDark;
//            BatDoubleBuffer(g);
//            return g;
//        }
//        private static void BatDoubleBuffer(Control c) =>
//            typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(c, true);
//        #endregion
//        #region Nạp & phân tích dữ liệu
//        private string LayChuoiKetNoi()
//        {
//            return _connectionString ??= new SqliteConnectionStringBuilder
//            {
//                DataSource = _csdl2Path,
//                Mode = SqliteOpenMode.ReadOnly,
//                DefaultTimeout = 5,
//                Pooling = true,
//                Cache = SqliteCacheMode.Private
//            }.ConnectionString;
//        }
//        private static void ApDungPragmaDoc(SqliteConnection conn)
//        {
//            // Kết nối ReadOnly: không đổi journal_mode (vô nghĩa/gây lỗi). Chỉ tăng cache + mmap để đọc nhanh hơn.
//            using var cmd = conn.CreateCommand();
//            cmd.CommandText = "PRAGMA temp_store = MEMORY; PRAGMA cache_size = -65536; PRAGMA mmap_size = 268435456;";
//            cmd.ExecuteNonQuery();
//        }
//        private async Task TaiDuLieuHeThongAsync()
//        {
//            if (Interlocked.Exchange(ref _isLoading, 1) == 1) return;
//            SetStatus("Đang phân tích dữ liệu...");
//            BatProgress();
//            _detailCts?.Cancel();
//            _loadCts?.Cancel();
//            _loadCts?.Dispose();
//            _loadCts = new CancellationTokenSource();
//            var token = _loadCts.Token;
//            try
//            {
//                var result = await Task.Run(() => PhanTichDuLieu(token), token);
//                if (IsDisposed || token.IsCancellationRequested) return;
//                _data = result ?? new List<TrungTenModel>();
//                _detailCache.Clear();
//                _modelDangHienThi = null;
//                CapNhatKpi();
//                ApDungBoLoc();
//                _txtSearch.Focus();
//            }
//            catch (OperationCanceledException) { }
//            catch (Exception ex)
//            {
//                if (!IsDisposed) SetStatus("Lỗi khi phân tích dữ liệu.");
//                System.Diagnostics.Debug.WriteLine($"[Form28 Load Lỗi]: {ex}");
//                if (!IsDisposed)
//                    MessageBox.Show("Có lỗi nghiêm trọng xảy ra trong quá trình kết nối và phân tích dữ liệu CSDL.", "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
//            }
//            finally
//            {
//                TatProgress();
//                Interlocked.Exchange(ref _isLoading, 0);
//            }
//        }
//        /// <summary>
//        /// Quét 1 lượt toàn bộ bảng. Dùng danh sách phẳng + liên kết "prev" thay cho Dictionary lồng nhau,
//        /// nên tên không trùng (đa số) gần như không tốn thêm đối tượng nào → nhanh và nhẹ RAM hơn nhiều.
//        /// </summary>
//        private List<TrungTenModel> PhanTichDuLieu(CancellationToken token)
//        {
//            var head = new Dictionary<string, int>(16384, StringComparer.Ordinal);
//            var rowIds = new List<long>(16384);
//            var units = new List<string>(16384);
//            var prev = new List<int>(16384);
//            var cacheTen = new Dictionary<string, string>(16384, StringComparer.Ordinal);
//            var cacheDv = new Dictionary<string, string>(2048, StringComparer.Ordinal);
//            int loi = 0;
//            using (var conn = new SqliteConnection(LayChuoiKetNoi()))
//            {
//                conn.Open();
//                ApDungPragmaDoc(conn);
//                using var cmd = conn.CreateCommand();
//                cmd.CommandText = "SELECT rowid, HoVaTen, DonVi FROM DanhSach WHERE HoVaTen IS NOT NULL";
//                using var reader = cmd.ExecuteReader();
//                int count = 0;
//                while (reader.Read())
//                {
//                    if ((++count & 1023) == 0) token.ThrowIfCancellationRequested();
//                    if (count > MaxRecords) throw new InvalidOperationException("Vượt quá giới hạn bộ nhớ an toàn của phân hệ.");
//                    string maHoaTen = reader.GetString(1);
//                    if (string.IsNullOrWhiteSpace(maHoaTen)) continue;
//                    if (!cacheTen.TryGetValue(maHoaTen, out var hoTen))
//                    {
//                        try { hoTen = ChuanHoaTen(Module_BaoMatAES.GiaiMa(maHoaTen) ?? ""); }
//                        catch { hoTen = ""; loi++; }
//                        if (cacheTen.Count < 200_000) cacheTen[maHoaTen] = hoTen;
//                    }
//                    if (hoTen.Length == 0) continue;
//                    string donVi = "";
//                    if (!reader.IsDBNull(2))
//                    {
//                        string maHoaDv = reader.GetString(2);
//                        if (maHoaDv.Length > 0)
//                        {
//                            if (!cacheDv.TryGetValue(maHoaDv, out var dv))
//                            {
//                                try { dv = (Module_BaoMatAES.GiaiMa(maHoaDv) ?? "").Trim().ToUpperInvariant(); }
//                                catch { dv = ""; loi++; }
//                                cacheDv[maHoaDv] = dv;
//                            }
//                            donVi = dv;
//                        }
//                    }
//                    long rowId = reader.GetInt64(0);
//                    int idx = rowIds.Count;
//                    rowIds.Add(rowId);
//                    units.Add(donVi);
//                    ref int slot = ref CollectionsMarshal.GetValueRefOrAddDefault(head, hoTen, out bool exists);
//                    prev.Add(exists ? slot : -1);
//                    slot = idx;
//                }
//            }
//            var result = new List<TrungTenModel>();
//            var chain = new List<int>(8);
//            var unitNames = new List<string>(4);
//            var unitCounts = new List<int>(4);
//            var sb = new StringBuilder(128);
//            int n = 0;
//            foreach (var kv in head)
//            {
//                if ((++n & 511) == 0) token.ThrowIfCancellationRequested();
//                int last = kv.Value;
//                if (prev[last] < 0) continue; // tên chỉ xuất hiện 1 lần
//                chain.Clear(); unitNames.Clear(); unitCounts.Clear();
//                for (int i = last; i >= 0; i = prev[i]) chain.Add(i);
//                chain.Reverse();
//                var ids = new List<long>(chain.Count);
//                foreach (int i in chain)
//                {
//                    ids.Add(rowIds[i]);
//                    string u = units[i];
//                    int k = -1;
//                    for (int j = 0; j < unitNames.Count; j++)
//                        if (string.Equals(unitNames[j], u, StringComparison.OrdinalIgnoreCase)) { k = j; break; }
//                    if (k < 0) { unitNames.Add(u); unitCounts.Add(1); }
//                    else unitCounts[k]++;
//                }
//                sb.Clear();
//                for (int j = 0; j < unitNames.Count; j++)
//                {
//                    if (j > 0) sb.Append("; ");
//                    sb.Append(unitNames[j].Length == 0 ? TenChuaCoDonVi : unitNames[j]).Append(" (").Append(unitCounts[j]).Append(')');
//                }
//                result.Add(new TrungTenModel
//                {
//                    HoTen = kv.Key,
//                    SoLan = chain.Count,
//                    DonVi = sb.ToString(),
//                    RowIds = ids,
//                    KeyTimKiem = BoDau(kv.Key)
//                });
//            }
//            result.Sort((a, b) =>
//            {
//                int c = b.SoLan.CompareTo(a.SoLan);
//                return c != 0 ? c : string.Compare(a.HoTen, b.HoTen, StringComparison.CurrentCultureIgnoreCase);
//            });
//            _soBanGhiLoi = loi;
//            return result;
//        }
//        private List<TrungTenDetailModel> TruyVanChiTietSQLite(List<long> ids, CancellationToken token)
//        {
//            if (ids == null || ids.Count == 0) return new List<TrungTenDetailModel>();
//            var result = new List<TrungTenDetailModel>(ids.Count);
//            using var conn = new SqliteConnection(LayChuoiKetNoi());
//            conn.Open();
//            ApDungPragmaDoc(conn);
//            var sb = new StringBuilder(SqlChunkSize * 8 + 128);
//            for (int start = 0; start < ids.Count; start += SqlChunkSize)
//            {
//                token.ThrowIfCancellationRequested();
//                int end = Math.Min(start + SqlChunkSize, ids.Count);
//                sb.Clear();
//                sb.Append("SELECT HoVaTen, NamSinh, CapBac, DonVi, SoHieu, GhiChu FROM DanhSach WHERE rowid IN (");
//                for (int i = start; i < end; i++)
//                {
//                    if (i > start) sb.Append(',');
//                    sb.Append(ids[i]); // long → an toàn, không thể chèn SQL
//                }
//                sb.Append(')');
//                using var cmd = conn.CreateCommand();
//                cmd.CommandText = sb.ToString();
//                using var reader = cmd.ExecuteReader();
//                while (reader.Read())
//                {
//                    token.ThrowIfCancellationRequested();
//                    result.Add(new TrungTenDetailModel
//                    {
//                        HoTen = ChuanHoaTen(GiaiMaCot(reader, 0)),
//                        NamSinh = GiaiMaCot(reader, 1).Trim(),
//                        CapBac = GiaiMaCot(reader, 2).Trim(),
//                        DonVi = GiaiMaCot(reader, 3).Trim().ToUpperInvariant(),
//                        SoHieu = GiaiMaCot(reader, 4).Trim(),
//                        GhiChu = GiaiMaCot(reader, 5).Trim()
//                    });
//                }
//            }
//            // Sắp theo đơn vị để các bản ghi cùng đơn vị đứng cạnh nhau (OrderBy là sắp xếp ổn định)
//            return result.OrderBy(x => x.DonVi, StringComparer.CurrentCultureIgnoreCase).ToList();
//        }
//        private static string GiaiMaCot(SqliteDataReader r, int i)
//        {
//            if (r.IsDBNull(i)) return "";
//            try { return Module_BaoMatAES.GiaiMa(r.GetString(i)) ?? ""; }
//            catch { return ""; }
//        }
//        private static string ChuanHoaTen(string raw)
//        {
//            if (string.IsNullOrWhiteSpace(raw)) return "";
//            var sb = new StringBuilder(raw.Length);
//            bool space = false;
//            foreach (char c in raw.Trim())
//            {
//                if (char.IsWhiteSpace(c)) { space = true; continue; }
//                if (space) { sb.Append(' '); space = false; }
//                sb.Append(c);
//            }
//            return _textInfo.ToTitleCase(sb.ToString().ToLowerInvariant()).Normalize(NormalizationForm.FormC);
//        }
//        private static string BoDau(string s)
//        {
//            if (string.IsNullOrEmpty(s)) return "";
//            string d = s.Normalize(NormalizationForm.FormD);
//            var sb = new StringBuilder(d.Length);
//            foreach (char c in d)
//            {
//                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
//                sb.Append(c == 'đ' || c == 'Đ' ? 'd' : char.ToLowerInvariant(c));
//            }
//            return sb.ToString();
//        }
//        #endregion
//        #region Hiển thị danh sách (master) & chi tiết
//        private void ApDungBoLoc()
//        {
//            string kw = _txtSearch.Text.Trim();
//            if (kw.Length == 0) _viewData = _data;
//            else
//            {
//                string key = BoDau(kw); // tìm không phân biệt dấu: "nguyen" khớp "Nguyễn"
//                _viewData = _data.FindAll(x => x.KeyTimKiem.Contains(key, StringComparison.Ordinal));
//            }
//            CapNhatMasterGrid();
//        }
//        private void CapNhatMasterGrid()
//        {
//            var truoc = _modelDangHienThi;
//            _boQuaSelection = true;
//            try
//            {
//                bool has = _viewData.Count > 0;
//                _gridMaster.Visible = has;
//                _lblEmptyMaster.Visible = !has;
//                _lblEmptyMaster.Text = _data.Count == 0 ? "Không phát hiện tên trùng." : "Không tìm thấy kết quả phù hợp.";
//                _gridMaster.RowCount = 0;
//                _gridMaster.RowCount = _viewData.Count;
//                if (has)
//                {
//                    int idx = truoc != null ? _viewData.IndexOf(truoc) : -1;
//                    if (idx < 0) idx = 0;
//                    _gridMaster.CurrentCell = _gridMaster.Rows[idx].Cells[0];
//                    try { _gridMaster.FirstDisplayedScrollingRowIndex = Math.Max(0, idx - 3); } catch { }
//                }
//            }
//            finally { _boQuaSelection = false; }
//            _lblMasterCount.Text = _viewData.Count == _data.Count ? _data.Count.ToString("N0") : $"{_viewData.Count:N0}/{_data.Count:N0}";
//            CapNhatStatus();
//            if (_viewData.Count == 0) XoaChiTiet();
//            else { _selectTimer.Stop(); _selectTimer.Start(); }
//        }
//        private void GridMaster_SelectionChanged(object? sender, EventArgs e)
//        {
//            if (_boQuaSelection) return;
//            // Debounce: giữ phím mũi tên không bắn hàng chục truy vấn liên tiếp
//            _selectTimer.Stop();
//            _selectTimer.Start();
//        }
//        private TrungTenModel? LayModelDangChon()
//        {
//            int r = _gridMaster.CurrentCell?.RowIndex ?? -1;
//            return r >= 0 && r < _viewData.Count ? _viewData[r] : null;
//        }
//        private async void TaiChiTietAsync()
//        {
//            var model = LayModelDangChon();
//            if (model == null) { XoaChiTiet(); return; }
//            _detailCts?.Cancel();
//            if (ReferenceEquals(model, _modelDangHienThi)) return;
//            _detailCts?.Dispose();
//            var cts = _detailCts = new CancellationTokenSource();
//            var token = cts.Token;
//            if (_detailCache.TryGetValue(model.HoTen, out var cached))
//            {
//                HienThiChiTiet(model, cached);
//                return;
//            }
//            BatProgress();
//            SetStatus($"Đang tải chi tiết: {model.HoTen}...");
//            try
//            {
//                var rows = await Task.Run(() => TruyVanChiTietSQLite(model.RowIds, token), token);
//                if (IsDisposed || token.IsCancellationRequested) return;
//                if (_detailCache.Count >= MaxDetailCache) _detailCache.Clear();
//                _detailCache[model.HoTen] = rows;
//                HienThiChiTiet(model, rows);
//            }
//            catch (OperationCanceledException) { }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine($"[TaiChiTiet Lỗi]: {ex.Message}");
//                if (!IsDisposed) SetStatus("Lỗi khi tải chi tiết hồ sơ.");
//            }
//            finally
//            {
//                TatProgress();
//                if (!IsDisposed && !token.IsCancellationRequested) CapNhatStatus();
//            }
//        }
//        private void HienThiChiTiet(TrungTenModel model, List<TrungTenDetailModel> rows)
//        {
//            _modelDangHienThi = model;
//            _viewDataDetail = rows;
//            _donViDuocChon = "";
//            int soDonVi = XayDungChips(rows);
//            _lblDetailTitle.Text = model.HoTen;
//            _lblDetailSub.Text = $"{rows.Count} hồ sơ trùng tên • {soDonVi} đơn vị";
//            _gridDetail.RowCount = 0;
//            _gridDetail.RowCount = rows.Count;
//            bool has = rows.Count > 0;
//            _gridDetail.Visible = has;
//            _lblEmptyDetail.Visible = !has;
//            _lblEmptyDetail.Text = "Không có hồ sơ để hiển thị.";
//            if (has) { _gridDetail.ClearSelection(); _gridDetail.CurrentCell = null; }
//            _gridDetail.Invalidate();
//            CapNhatStatus();
//        }
//        private void XoaChiTiet()
//        {
//            _detailCts?.Cancel();
//            _modelDangHienThi = null;
//            _viewDataDetail = new List<TrungTenDetailModel>();
//            _donViDuocChon = "";
//            XayDungChips(_viewDataDetail);
//            _lblDetailTitle.Text = "Chi tiết hồ sơ";
//            _lblDetailSub.Text = "";
//            _gridDetail.RowCount = 0;
//            _gridDetail.Visible = false;
//            _lblEmptyDetail.Visible = true;
//            _lblEmptyDetail.Text = _data.Count == 0
//                ? "Không có dữ liệu để hiển thị."
//                : "Chọn một họ tên ở danh sách bên trái để xem chi tiết hồ sơ.";
//        }
//        private int XayDungChips(List<TrungTenDetailModel> rows)
//        {
//            _chipPanel.SuspendLayout();
//            foreach (var c in _chipPanel.Controls.Cast<Control>().ToArray())
//            {
//                _chipPanel.Controls.Remove(c);
//                c.Dispose();
//            }
//            var groups = rows.GroupBy(r => r.DonVi, StringComparer.OrdinalIgnoreCase)
//                             .Select(g => (Key: g.Key, Count: g.Count()))
//                             .OrderByDescending(g => g.Count)
//                             .ThenBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase)
//                             .ToList();
//            foreach (var (key, count) in groups)
//            {
//                var chip = new ChipButton
//                {
//                    Font = _chipFont,
//                    Text = $"{(key.Length == 0 ? TenChuaCoDonVi : key)}  ·  {count}",
//                    Tag = key,
//                    Margin = new Padding(0, 0, 8, 8)
//                };
//                chip.Click += Chip_Click;
//                _chipPanel.Controls.Add(chip);
//            }
//            _chipPanel.ResumeLayout(true);
//            bool has = groups.Count > 0;
//            _chipPanel.Visible = has;
//            _lblChipHint.Visible = has;
//            _tlpDetail.RowStyles[2].Height = has ? 24F : 0F;
//            return groups.Count;
//        }
//        private void Chip_Click(object? sender, EventArgs e)
//        {
//            if (sender is not ChipButton chip) return;
//            string dv = chip.Tag as string ?? "";
//            if (dv.Length == 0) return;
//            bool boChon = string.Equals(_donViDuocChon, dv, StringComparison.OrdinalIgnoreCase);
//            _donViDuocChon = boChon ? "" : dv;
//            foreach (var c in _chipPanel.Controls.OfType<ChipButton>())
//                c.Checked = !boChon && string.Equals(c.Tag as string, dv, StringComparison.OrdinalIgnoreCase);
//            _gridDetail.Invalidate();
//        }
//        private void GridMaster_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
//        {
//            if (e.RowIndex < 0 || e.RowIndex >= _viewData.Count) return;
//            var item = _viewData[e.RowIndex];
//            e.Value = e.ColumnIndex == 0 ? item.HoTen : item.SoLan;
//        }
//        private void GridMaster_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
//        {
//            if (e.ColumnIndex != 1 || e.RowIndex < 0 || e.RowIndex >= _viewData.Count) return;
//            int n = _viewData[e.RowIndex].SoLan;
//            Color c = n >= 5 ? UiColors.Danger : n >= 3 ? UiColors.Warn : UiColors.GreenDark;
//            e.CellStyle!.ForeColor = c;
//            e.CellStyle.SelectionForeColor = c;
//            e.CellStyle.Font = _boldFont;
//        }
//        private void GridDetail_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
//        {
//            if (e.RowIndex < 0 || e.RowIndex >= _viewDataDetail.Count) return;
//            var item = _viewDataDetail[e.RowIndex];
//            switch (e.ColumnIndex)
//            {
//                case 0: e.Value = e.RowIndex + 1; break;
//                case 1: e.Value = item.HoTen; break;
//                case 2: e.Value = item.NamSinh; break;
//                case 3: e.Value = item.CapBac; break;
//                case 4: e.Value = item.DonVi; break;
//                case 5: e.Value = item.SoHieu; break;
//                case 6: e.Value = item.GhiChu; break;
//            }
//        }
//        // Dùng CellFormatting (style tạm cho từng ô) thay vì sửa DefaultCellStyle của từng dòng trong RowPrePaint
//        private void GridDetail_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
//        {
//            if (_donViDuocChon.Length == 0 || e.RowIndex < 0 || e.RowIndex >= _viewDataDetail.Count) return;
//            if (!string.Equals(_viewDataDetail[e.RowIndex].DonVi, _donViDuocChon, StringComparison.OrdinalIgnoreCase)) return;
//            e.CellStyle!.BackColor = UiColors.Highlight;
//            e.CellStyle.SelectionBackColor = UiColors.HighlightSel;
//            e.CellStyle.Font = _boldFont;
//        }
//        #endregion
//        #region Tìm kiếm, làm mới, đóng
//        private void TxtSearch_TextChanged(object? sender, EventArgs e)
//        {
//            _btnXoaTimKiem.Visible = _txtSearch.TextLength > 0;
//            _searchTimer.Stop();
//            _searchTimer.Start(); // tìm khi người dùng ngừng gõ 250ms
//        }
//        private void TimKiem()
//        {
//            _searchTimer.Stop();
//            ApDungBoLoc();
//        }
//        private void kryptonButton_TimKiem_Click(object? sender, EventArgs e) => TimKiem();
//        private async void kryptonButton_LamMoi_Click(object? sender, EventArgs e)
//        {
//            _txtSearch.Clear();
//            _searchTimer.Stop();
//            await TaiDuLieuHeThongAsync();
//        }
//        private void kryptonButton_Dong_Click(object? sender, EventArgs e)
//        {
//            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
//            formCha?.CapNhatTieuDe("Trang phân loại thi đua");
//            Close();
//        }
//        private void textBoxKyToon_TimTen_KeyDown(object? sender, KeyEventArgs e)
//        {
//            if (e.KeyCode == Keys.Enter)
//            {
//                e.SuppressKeyPress = true;
//                TimKiem();
//            }
//            else if (e.KeyCode == Keys.Escape)
//            {
//                e.SuppressKeyPress = true;
//                _txtSearch.Clear();
//                TimKiem();
//            }
//            else if (e.KeyCode == Keys.Down && _gridMaster.Visible)
//            {
//                e.SuppressKeyPress = true;
//                _gridMaster.Focus();
//            }
//        }
//        private void lamMoi_ToolStripMenuItem_Click(object? sender, EventArgs e) => _btnLamMoi.PerformClick();
//        private void quayLaiTrangXuLyDuLieu_ToolStripMenuItem_Click(object? sender, EventArgs e) => _btnDong.PerformClick();
//        private void xoaTimKiem_ToolStripMenuItem_Click(object? sender, EventArgs e)
//        {
//            _txtSearch.Clear();
//            _txtSearch.Focus();
//            TimKiem();
//        }
//        #endregion
//        #region Phím tắt, trạng thái, tiện ích UI
//        private Action SafeAction(Action action)
//        {
//            return () =>
//            {
//                if (action == null || Interlocked.CompareExchange(ref _isLoading, 0, 0) == 1) return;
//                try { action.Invoke(); }
//                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Lỗi thực thi phím tắt: " + ex.Message); }
//            };
//        }
//        private bool SafeExecute(Action action)
//        {
//            SafeAction(action).Invoke();
//            return true;
//        }
//        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
//        {
//            if (Interlocked.CompareExchange(ref _isLoading, 0, 0) == 1) return base.ProcessCmdKey(ref msg, keyData);
//            try
//            {
//                if (Module_PhimTat.XuLy(keyData, actionLamMoi: SafeAction(() => lamMoi_ToolStripMenuItem_Click(null, EventArgs.Empty))))
//                    return true;
//                Keys key = keyData & Keys.KeyCode;
//                Keys modifier = keyData & Keys.Modifiers;
//                if (modifier == Keys.None && key == Keys.F4)
//                    return SafeExecute(() => xoaTimKiem_ToolStripMenuItem_Click(null, EventArgs.Empty));
//                if (modifier == Keys.Control && key == Keys.Q)
//                    return SafeExecute(() => quayLaiTrangXuLyDuLieu_ToolStripMenuItem_Click(null, EventArgs.Empty));
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine("Lỗi phím tắt: " + ex.Message);
//                return true;
//            }
//            return base.ProcessCmdKey(ref msg, keyData);
//        }
//        private void SetStatus(string text)
//        {
//            if (!IsDisposed) _lblStatus.Text = text;
//        }
//        private void BatProgress()
//        {
//            if (Interlocked.Increment(ref _busyCount) == 1 && !IsDisposed) _progress.Visible = true;
//        }
//        private void TatProgress()
//        {
//            if (Interlocked.Decrement(ref _busyCount) <= 0)
//            {
//                Interlocked.Exchange(ref _busyCount, 0);
//                if (!IsDisposed) _progress.Visible = false;
//            }
//        }
//        private void CapNhatKpi()
//        {
//            long tongHoSo = 0;
//            foreach (var x in _data) tongHoSo += x.SoLan;
//            _kpiTen.Text = _data.Count.ToString("N0");
//            _kpiHoSo.Text = tongHoSo.ToString("N0");
//            _kpiMax.Text = _data.Count == 0 ? "—" : $"{_data[0].HoTen} ({_data[0].SoLan})";
//        }
//        private void CapNhatStatus()
//        {
//            int total = _data.Count;
//            int filtered = _viewData.Count;
//            string loi = _soBanGhiLoi > 0 ? $"  •  {_soBanGhiLoi} bản ghi không giải mã được" : "";
//            if (total == 0) SetStatus("Không phát hiện tên trùng." + loi);
//            else if (filtered == total) SetStatus($"Số CBCS có tên trùng nhau: {total} {Module_HeThong.Tu_dong_chi}{loi}");
//            else SetStatus($"Kết quả tìm kiếm: {filtered}/{total} CBCS trùng tên{loi}");
//        }
//        private void InitToolTips()
//        {
//            if (_daKhoiTaoToolTip) return;
//            try
//            {
//                _toolTip = new ToolTip(components)
//                {
//                    IsBalloon = true,
//                    ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac,
//                    ToolTipIcon = ToolTipIcon.Info,
//                    InitialDelay = 300,
//                    AutoPopDelay = 2500,
//                    ReshowDelay = 100,
//                    ShowAlways = true
//                };
//                _toolTip.SetToolTip(_btnTimKiem, "Bấm nút để tìm kiếm (Enter)");
//                _toolTip.SetToolTip(_btnLamMoi, "Làm mới dữ liệu (F5)");
//                _toolTip.SetToolTip(_btnDong, "Thoát trang (Ctrl+Q)");
//                _toolTip.SetToolTip(_btnXoaTimKiem, "Xóa nội dung tìm kiếm (F4)");
//                _daKhoiTaoToolTip = true;
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine($"[InitToolTips]: {ex.Message}");
//            }
//        }
//        private void XacDinhPhienBan()
//        {
//            _lblVersion.Text = _laTanBinh ? "Phiên bản phần mềm dành cho tân binh" : "Phiên bản phần mềm dành cho CBCS";
//        }
//        private void SetupPlaceholder()
//        {
//            if (_txtSearch != null && !_txtSearch.IsDisposed)
//                SendMessage(_txtSearch.Handle, EM_SETCUEBANNER, 1, _laTanBinh ? "Nhập họ và tên tân binh để tìm kiếm (không cần gõ dấu)" : "Nhập họ và tên CBCS để tìm kiếm (không cần gõ dấu)");
//        }
//        protected override void OnFormClosing(FormClosingEventArgs e)
//        {
//            _searchTimer.Stop();
//            _selectTimer.Stop();
//            _loadCts?.Cancel();
//            _detailCts?.Cancel();
//            base.OnFormClosing(e);
//        }
//        #endregion
//        #region Control tự vẽ (nút, thẻ, chip) & bảng màu
//        private static class UiColors
//        {
//            public static readonly Color Bg = Color.FromArgb(244, 246, 248);
//            public static readonly Color Border = Color.FromArgb(229, 231, 235);
//            public static readonly Color GridLine = Color.FromArgb(241, 243, 245);
//            public static readonly Color HeaderBack = Color.FromArgb(249, 250, 251);
//            public static readonly Color ZebraRow = Color.FromArgb(251, 252, 253);
//            public static readonly Color Hover = Color.FromArgb(243, 244, 246);
//            public static readonly Color Text = Color.FromArgb(17, 24, 39);
//            public static readonly Color Muted = Color.FromArgb(107, 114, 128);
//            public static readonly Color Green = Color.FromArgb(22, 163, 74);
//            public static readonly Color GreenHover = Color.FromArgb(21, 128, 61);
//            public static readonly Color GreenPressed = Color.FromArgb(20, 83, 45);
//            public static readonly Color GreenDark = Color.FromArgb(21, 128, 61);
//            public static readonly Color GreenSoft = Color.FromArgb(220, 252, 231);
//            public static readonly Color Highlight = Color.FromArgb(254, 249, 195);
//            public static readonly Color HighlightSel = Color.FromArgb(253, 230, 138);
//            public static readonly Color Danger = Color.FromArgb(185, 28, 28);
//            public static readonly Color Warn = Color.FromArgb(180, 83, 9);
//            public static readonly Color Purple = Color.FromArgb(237, 233, 254);      // Màu tím nhạt nền
//            public static readonly Color PurpleHover = Color.FromArgb(221, 214, 254); // Màu tím nhạt khi rê chuột
//            public static readonly Color PurplePressed = Color.FromArgb(196, 181, 253);// Màu tím nhạt khi nhấn
//            public static readonly Color PurpleDark = Color.FromArgb(109, 40, 217);   // Màu chữ/icon tím đậm cho tương phản
//        }
//        private static GraphicsPath RoundedRect(Rectangle r, int radius)
//        {
//            int d = Math.Max(1, radius * 2);
//            var path = new GraphicsPath();
//            path.AddArc(r.X, r.Y, d, d, 180, 90);
//            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
//            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
//            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
//            path.CloseFigure();
//            return path;
//        }
//        private sealed class RoundedPanel : Panel
//        {
//            private Color _fill = Color.White;
//            public int Radius { get; set; } = 12;
//            public Color BorderColor { get; set; } = UiColors.Border;
//            public Color FillColor
//            {
//                get => _fill;
//                set { _fill = value; BackColor = value; Invalidate(); }
//            }
//            public RoundedPanel()
//            {
//                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
//                BackColor = Color.White;
//            }
//            protected override void OnPaintBackground(PaintEventArgs e) =>
//                e.Graphics.Clear(Parent?.BackColor ?? SystemColors.Control);
//            protected override void OnPaint(PaintEventArgs e)
//            {
//                var g = e.Graphics;
//                g.SmoothingMode = SmoothingMode.AntiAlias;
//                using var path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Radius);
//                using var b = new SolidBrush(_fill);
//                using var p = new Pen(BorderColor);
//                g.FillPath(b, path);
//                g.DrawPath(p, path);
//            }
//        }
//        private sealed class ModernButton : Button
//        {
//            private static readonly Font IconFont = new("Segoe MDL2 Assets", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
//            private bool _hover, _down;
//            public string Glyph { get; set; } = "";
//            public bool Primary { get; set; }
//            public bool IsPurple { get; set; } // Thêm thuộc tính nhận diện nút màu tím
//            public int Radius { get; set; } = 8;
//            public ModernButton()
//            {
//                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
//                FlatStyle = FlatStyle.Flat;
//                FlatAppearance.BorderSize = 0;
//                UseVisualStyleBackColor = false;
//                Cursor = Cursors.Hand;
//            }
//            protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
//            protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
//            protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
//            protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }
//            protected override void OnPaint(PaintEventArgs e)
//            {
//                var g = e.Graphics;
//                g.Clear(Parent?.BackColor ?? SystemColors.Control);
//                g.SmoothingMode = SmoothingMode.AntiAlias;
//                Color fill, border, fore;
//                if (Primary)
//                {
//                    fill = _down ? UiColors.GreenPressed : _hover ? UiColors.GreenHover : UiColors.Green;
//                    border = fill;
//                    fore = Color.White;
//                }
//                else if (IsPurple) // Xử lý riêng cho nút màu tím nhạt
//                {
//                    fill = _down ? UiColors.PurplePressed : _hover ? UiColors.PurpleHover : UiColors.Purple;
//                    border = Color.FromArgb(196, 181, 253);
//                    fore = UiColors.PurpleDark;
//                }
//                else
//                {
//                    fill = _down ? Color.FromArgb(229, 231, 235) : _hover ? UiColors.Hover : Color.White;
//                    border = Color.FromArgb(209, 213, 219);
//                    fore = UiColors.Text;
//                }
//                if (!Enabled) { fill = Color.FromArgb(243, 244, 246); fore = Color.FromArgb(156, 163, 175); }
//                using (var path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Radius))
//                using (var b = new SolidBrush(fill))
//                using (var p = new Pen(border))
//                {
//                    g.FillPath(b, path);
//                    g.DrawPath(p, path);
//                }
//                const TextFormatFlags fl = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.Left;
//                var huge = new Size(int.MaxValue, int.MaxValue);
//                Size gs = string.IsNullOrEmpty(Glyph) ? Size.Empty : TextRenderer.MeasureText(g, Glyph, IconFont, huge, fl);
//                Size ts = string.IsNullOrEmpty(Text) ? Size.Empty : TextRenderer.MeasureText(g, Text, Font, huge, fl);
//                int gap = gs.Width > 0 && ts.Width > 0 ? 8 : 0;
//                int x = (Width - (gs.Width + gap + ts.Width)) / 2;
//                if (gs.Width > 0)
//                {
//                    TextRenderer.DrawText(g, Glyph, IconFont, new Rectangle(x, 0, gs.Width + 2, Height), fore, fl);
//                    x += gs.Width + gap;
//                }
//                if (ts.Width > 0)
//                    TextRenderer.DrawText(g, Text, Font, new Rectangle(x, 0, ts.Width + 2, Height), fore, fl);
//            }
//        }
//        private sealed class ChipButton : Control
//        {
//            private bool _hover, _checked;
//            public bool Checked
//            {
//                get => _checked;
//                set { if (_checked != value) { _checked = value; Invalidate(); } }
//            }
//            public ChipButton()
//            {
//                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
//                SetStyle(ControlStyles.Selectable, false);
//                TabStop = false;
//                Cursor = Cursors.Hand;
//                AutoSize = true;
//                Height = 30;
//            }
//            public override Size GetPreferredSize(Size proposedSize)
//            {
//                var ts = TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
//                return new Size(Math.Min(ts.Width + 28, 380), 30);
//            }
//            protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
//            protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
//            protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
//            protected override void OnPaintBackground(PaintEventArgs e) =>
//                e.Graphics.Clear(Parent?.BackColor ?? SystemColors.Control);
//            protected override void OnPaint(PaintEventArgs e)
//            {
//                var g = e.Graphics;
//                g.SmoothingMode = SmoothingMode.AntiAlias;
//                Color fill = _checked ? UiColors.HighlightSel : _hover ? Color.FromArgb(229, 231, 235) : UiColors.Hover;
//                Color border = _checked ? Color.FromArgb(234, 179, 8) : UiColors.Border;
//                Color fore = _checked ? Color.FromArgb(113, 63, 18) : UiColors.Text;
//                using (var path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Height / 2))
//                using (var b = new SolidBrush(fill))
//                using (var p = new Pen(border))
//                {
//                    g.FillPath(b, path);
//                    g.DrawPath(p, path);
//                }
//                TextRenderer.DrawText(g, Text, Font, new Rectangle(12, 0, Width - 24, Height), fore,
//                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
//            }
//        }
//        #endregion
//    }
//    public class TrungTenModel
//    {
//        public string HoTen { get; set; } = "";
//        public int SoLan { get; set; }
//        public string DonVi { get; set; } = "";
//        public string KeyTimKiem { get; set; } = "";
//        public List<long> RowIds { get; set; } = new();
//    }
//    public class TrungTenDetailModel
//    {
//        public string HoTen { get; set; } = "";
//        public string NamSinh { get; set; } = "";
//        public string CapBac { get; set; } = "";
//        public string DonVi { get; set; } = "";
//        public string SoHieu { get; set; } = "";
//        public string GhiChu { get; set; } = "";
//    }
//}
///////TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
////using Microsoft.Data.Sqlite;
////using System.Data;
////using System.Globalization;
////using System.Runtime.InteropServices;
////using System.Text;
////namespace PhanMemThiDua2026
////{
////    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
////    public partial class Form28_DataTrungTen : Form
////    {
////        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
////        private readonly List<TrungTenModel> _data = new();
////        private List<TrungTenModel> _viewData = new();
////        private readonly ToolStripStatusLabel _rightSpacer = new();
////        private const int EM_SETCUEBANNER = 0x1501;
////        private const int WM_SETREDRAW = 11;
////        // ⭐ CHUẨN KỸ SƯ: Khởi tạo luồng an toàn bằng Interlocked
////        private int _isProcessing = 0;
////        private CancellationTokenSource _cts;
////        private Font _headerFont;
////        private Font _boldFont;
////        private string _donViDuocChon = "";
////        private volatile bool _isTreeSelecting;
////        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
////        private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, string lParam);
////        private List<TrungTenDetailModel> _viewDataDetail = new();
////        private Krypton.Toolkit.KryptonTreeView kryptonTreeView_DieuHuong;
////        private Krypton.Toolkit.KryptonSplitContainer splitContainer;
////        private static readonly char[] _splitChars = new[] { ';' };
////        public Form28_DataTrungTen()
////        {
////            InitializeComponent();
////            KhoiTaoGiaoDienSplit();
////            SetupPlaceholder();
////            kryptonButton_TimKiem.DialogResult = DialogResult.None;
////            FormBorderStyle = FormBorderStyle.FixedDialog;
////            StartPosition = FormStartPosition.CenterScreen;
////            MaximizeBox = false;
////            AcceptButton = kryptonButton_TimKiem;
////            Load += Form28_DataTrungTen_Load;
////            KhoiTaoDataGridView();
////            InitToolTips();
////            SetupStatusStrip();
////        }
////        private async void Form28_DataTrungTen_Load(object? sender, EventArgs e)
////        {
////            kryptonDataGridView_TrungTen.ContextMenuStrip = contextMenuStrip1;
////            Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1);
////            XacDinhPhienBan();
////            if (splitContainer != null)
////            {
////                splitContainer.SplitterDistance = 280;
////            }
////            await TaiDuLieuHeThongAsync();
////        }
////        /// <summary>
////        /// ⭐ CHUẨN KỸ SƯ: Đóng gói luồng nạp dữ liệu bất đồng bộ, tự động tái tạo Token phòng thủ rò rỉ
////        /// </summary>
////        private async Task TaiDuLieuHeThongAsync()
////        {
////            if (Interlocked.Exchange(ref _isProcessing, 1) == 1) return;
////            toolStripStatusLabel1.Text = "Đang phân tích dữ liệu...";
////            BatProgress();
////            // Tái tạo CancellationTokenSource sạch cho phiên làm việc mới
////            _cts?.Cancel();
////            _cts?.Dispose();
////            _cts = new CancellationTokenSource();
////            var token = _cts.Token;
////            try
////            {
////                var result = await Task.Run(() => PhanTichDuLieu(token), token);
////                if (IsDisposed || token.IsCancellationRequested) return;
////                _data.Clear();
////                if (result != null) _data.AddRange(result);
////                _viewData = _data;
////                HienThiTreeView();
////                _viewDataDetail.Clear();
////                HienThiGrid();
////                CapNhatStatus();
////                textBoxKyToon_TimTen.Focus();
////            }
////            catch (OperationCanceledException) { }
////            catch (Exception ex)
////            {
////                toolStripStatusLabel1.Text = "Lỗi khi phân tích dữ liệu.";
////                System.Diagnostics.Debug.WriteLine($"[Form28 Load Lỗi]: {ex}");
////                MessageBox.Show("Có lỗi nghiêm trọng xảy ra trong quá trình kết nối và phân tích dữ liệu CSDL.", "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
////            }
////            finally
////            {
////                TatProgress();
////                Interlocked.Exchange(ref _isProcessing, 0);
////            }
////        }
////        //region KHOI TAO GIAO DIEN & LOAD DU LIEU
////        private void KhoiTaoGiaoDienSplit()
////        {
////            splitContainer = new Krypton.Toolkit.KryptonSplitContainer
////            {
////                Dock = DockStyle.Fill,
////                Cursor = Cursors.Default,
////                FixedPanel = FixedPanel.Panel1,
////                Panel1MinSize = 250
////            };
////            kryptonTreeView_DieuHuong = new Krypton.Toolkit.KryptonTreeView
////            {
////                Dock = DockStyle.Fill,
////                ItemHeight = 30,
////                ShowLines = true,
////                ShowPlusMinus = true
////            };
////            kryptonTreeView_DieuHuong.BeforeExpand += KryptonTreeView_DieuHuong_BeforeExpand;
////            kryptonTreeView_DieuHuong.AfterSelect += KryptonTreeView_DieuHuong_AfterSelect;
////            splitContainer.Panel1.Controls.Add(kryptonTreeView_DieuHuong);
////            tableLayoutPanel2.Controls.Remove(kryptonDataGridView_TrungTen);
////            splitContainer.Panel2.Controls.Add(kryptonDataGridView_TrungTen);
////            tableLayoutPanel2.Controls.Add(splitContainer, 0, 1);
////        }
////        private void KhoiTaoDataGridView()
////        {
////            string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
////            bool laTanBinh = phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase);
////            var grid = kryptonDataGridView_TrungTen;
////            grid.SuspendLayout();
////            grid.Columns.Clear();
////            grid.Dock = DockStyle.Fill;
////            grid.RowHeadersVisible = false;
////            grid.AllowUserToAddRows = false;
////            grid.AllowUserToDeleteRows = false;
////            grid.AllowUserToResizeRows = false;
////            grid.ReadOnly = true;
////            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
////            grid.EnableHeadersVisualStyles = false;
////            // Kích hoạt VirtualMode chuẩn kỹ sư
////            grid.VirtualMode = true;
////            // ⭐ LŨY ĐẲNG EVENT: Hủy liên kết cũ trước khi gán mới để tránh nhân bản bộ vẽ nền gây lag
////            grid.CellValueNeeded -= KryptonDataGridView_TrungTen_Detail_CellValueNeeded;
////            grid.CellValueNeeded += KryptonDataGridView_TrungTen_Detail_CellValueNeeded;
////            grid.RowPrePaint -= KryptonDataGridView_TrungTen_Detail_RowPrePaint;
////            grid.RowPrePaint += KryptonDataGridView_TrungTen_Detail_RowPrePaint;
////            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
////            grid.ColumnHeadersHeight = 45;
////            _headerFont?.Dispose();
////            _headerFont = new Font(grid.Font, FontStyle.Bold);
////            grid.ColumnHeadersDefaultCellStyle.Font = _headerFont;
////            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
////            _boldFont?.Dispose();
////            _boldFont = new Font(grid.Font, FontStyle.Bold);
////            // PHỐI MÀU UX CHUẨN LỰC LƯỢNG
////            grid.RowsDefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
////            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(225, 250, 225);
////            grid.RowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(144, 238, 144);
////            grid.RowsDefaultCellStyle.SelectionForeColor = Color.Black;
////            grid.GridColor = Color.LightGray;
////            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
////            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
////            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "STT", HeaderText = "STT", FillWeight = 6, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
////            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "HoTen", HeaderText = laTanBinh ? "Họ và tên tân binh" : "Họ và tên CBCS", FillWeight = 26 });
////            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "NamSinh", HeaderText = "Năm sinh", FillWeight = 12, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
////            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CapBac", HeaderText = "Cấp bậc", FillWeight = 14, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
////            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DonVi", HeaderText = "Đơn vị", FillWeight = 30 });
////            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoHieu", HeaderText = "Số hiệu", FillWeight = 12, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
////            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GhiChu", HeaderText = "Ghi chú", FillWeight = 20 });
////            // ⭐ TRIỆT TIÊU ĐỘ TRỄ RENDERING: Khóa cứng layout dòng, vô hiệu hóa tự động co giãn ô text dài
////            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
////            grid.RowTemplate.Height = 35;
////            grid.ResumeLayout();
////        }
////        private void KryptonDataGridView_TrungTen_Detail_RowPrePaint(object? sender, DataGridViewRowPrePaintEventArgs e)
////        {
////            if (_viewDataDetail == null || e.RowIndex < 0 || e.RowIndex >= _viewDataDetail.Count) return;
////            var grid = kryptonDataGridView_TrungTen;
////            if (grid == null || grid.IsDisposed) return;
////            try
////            {
////                var rowStyle = grid.Rows[e.RowIndex].DefaultCellStyle;
////                string donViCuaDong = _viewDataDetail[e.RowIndex].DonVi;
////                bool laDonViDuocChon = !string.IsNullOrEmpty(_donViDuocChon) &&
////                                       string.Equals(donViCuaDong, _donViDuocChon, StringComparison.OrdinalIgnoreCase);
////                if (laDonViDuocChon)
////                {
////                    rowStyle.BackColor = Color.FromArgb(152, 251, 152); // Xanh lá cây nhạt
////                    rowStyle.Font = _boldFont;
////                }
////                else
////                {
////                    // Trả lại màu đồng bộ chuẩn cho các hàng không được nhấp chọn
////                    rowStyle.BackColor = (e.RowIndex % 2 == 0)
////                        ? Color.FromArgb(240, 255, 240)
////                        : Color.FromArgb(225, 250, 225);
////                    rowStyle.Font = grid.Font;
////                }
////            }
////            catch (Exception ex)
////            {
////                System.Diagnostics.Debug.WriteLine($"[Lỗi vẽ dòng RowPrePaint]: {ex.Message}");
////            }
////        }
////        private void KryptonDataGridView_TrungTen_Detail_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
////        {
////            if (_viewDataDetail == null || e.RowIndex < 0 || e.RowIndex >= _viewDataDetail.Count) return;
////            var item = _viewDataDetail[e.RowIndex];
////            switch (e.ColumnIndex)
////            {
////                case 0: e.Value = e.RowIndex + 1; break;
////                case 1: e.Value = item.HoTen; break;
////                case 2: e.Value = item.NamSinh; break;
////                case 3: e.Value = item.CapBac; break;
////                case 4: e.Value = item.DonVi; break;
////                case 5: e.Value = item.SoHieu; break;
////                case 6: e.Value = item.GhiChu; break;
////            }
////        }
////        //endregion
////        //region XU LY DU LIEU TỐI ƯU & TREEVIEW
////        private List<TrungTenModel> PhanTichDuLieu(CancellationToken token)
////        {
////            var dict = new Dictionary<string, Dictionary<string, List<long>>>(10000, StringComparer.OrdinalIgnoreCase);
////            var aesCacheTen = new Dictionary<string, string>(10000);
////            var aesCacheDonVi = new Dictionary<string, string>(2000);
////            var textInfo = CultureInfo.CurrentCulture.TextInfo;
////            var builder = new SqliteConnectionStringBuilder
////            {
////                DataSource = _csdl2Path,
////                Mode = SqliteOpenMode.ReadOnly,
////                DefaultTimeout = 5,
////                Pooling = true,
////                Cache = SqliteCacheMode.Private
////            };
////            using var conn = new SqliteConnection(builder.ConnectionString);
////            conn.Open();
////            using (var pragmaCmd = conn.CreateCommand())
////            {
////                pragmaCmd.CommandText = "PRAGMA journal_mode = WAL; PRAGMA synchronous = NORMAL; PRAGMA temp_store = MEMORY;";
////                pragmaCmd.ExecuteNonQuery();
////            }
////            using var cmd = new SqliteCommand("SELECT rowid, HoVaTen, DonVi FROM DanhSach", conn);
////            using var reader = cmd.ExecuteReader();
////            int recordCount = 0;
////            while (reader.Read())
////            {
////                token.ThrowIfCancellationRequested();
////                if (++recordCount > 500000) throw new Exception("Vượt quá giới hạn bộ nhớ an toàn của phân hệ.");
////                if (reader.IsDBNull(1)) continue;
////                long rowId = reader.GetInt64(0);
////                string maHoaTen = reader.GetString(1);
////                if (string.IsNullOrWhiteSpace(maHoaTen)) continue;
////                // ⭐ ZERO-ALLOCATION PATTERN: Sử dụng TryGetValue loại bỏ hoàn toàn khối try-catch thừa thãi
////                if (!aesCacheTen.TryGetValue(maHoaTen, out string hoTen))
////                {
////                    hoTen = Module_BaoMatAES.GiaiMa(maHoaTen)?.Trim() ?? "";
////                    if (string.IsNullOrEmpty(hoTen)) continue;
////                    hoTen = textInfo.ToTitleCase(hoTen.ToLower());
////                    aesCacheTen[maHoaTen] = hoTen;
////                }
////                string donVi = "";
////                if (!reader.IsDBNull(2))
////                {
////                    string maHoaDonVi = reader.GetString(2);
////                    if (!string.IsNullOrEmpty(maHoaDonVi) && !aesCacheDonVi.TryGetValue(maHoaDonVi, out donVi))
////                    {
////                        donVi = Module_BaoMatAES.GiaiMa(maHoaDonVi)?.Trim() ?? "";
////                        donVi = donVi.ToUpperInvariant();
////                        aesCacheDonVi[maHoaDonVi] = donVi;
////                    }
////                }
////                if (!dict.TryGetValue(hoTen, out var dvDict))
////                {
////                    dvDict = new Dictionary<string, List<long>>(2, StringComparer.OrdinalIgnoreCase);
////                    dict[hoTen] = dvDict;
////                }
////                if (!dvDict.TryGetValue(donVi, out var listRowIds))
////                {
////                    listRowIds = new List<long>(4);
////                    dvDict[donVi] = listRowIds;
////                }
////                listRowIds.Add(rowId);
////            }
////            var result = new List<TrungTenModel>(dict.Count);
////            var sb = new StringBuilder(128);
////            foreach (var item in dict)
////            {
////                token.ThrowIfCancellationRequested();
////                int tong = 0;
////                var allRowIds = new List<long>();
////                foreach (var kvp in item.Value)
////                {
////                    tong += kvp.Value.Count;
////                    allRowIds.AddRange(kvp.Value);
////                }
////                if (tong <= 1) continue;
////                sb.Clear();
////                foreach (var d in item.Value)
////                {
////                    if (sb.Length > 0) sb.Append("; ");
////                    sb.Append(d.Key).Append(" (").Append(d.Value.Count).Append(')');
////                }
////                result.Add(new TrungTenModel
////                {
////                    HoTen = item.Key,
////                    SoLan = tong,
////                    DonVi = sb.ToString(),
////                    RowIds = allRowIds
////                });
////            }
////            result.Sort((a, b) => b.SoLan.CompareTo(a.SoLan));
////            return result;
////        }
////        private List<TrungTenDetailModel> TruyVanChiTietSQLite(List<long> targetRowIds, CancellationToken token)
////        {
////            var result = new List<TrungTenDetailModel>(targetRowIds.Count);
////            if (targetRowIds == null || targetRowIds.Count == 0) return result;
////            var builder = new SqliteConnectionStringBuilder
////            {
////                DataSource = _csdl2Path,
////                Mode = SqliteOpenMode.ReadOnly,
////                Pooling = true,
////                Cache = SqliteCacheMode.Private
////            };
////            using var conn = new SqliteConnection(builder.ConnectionString);
////            conn.Open();
////            using (var pragmaCmd = conn.CreateCommand())
////            {
////                pragmaCmd.CommandText = "PRAGMA journal_mode = WAL; PRAGMA temp_store = MEMORY;";
////                pragmaCmd.ExecuteNonQuery();
////            }
////            // ⭐ CHUẨN AN TOÀN: Gom cụm định danh ROWID, ngăn chặn lỗi biên dịch SQLite Command vượt ngưỡng
////            string inClause = string.Join(",", targetRowIds);
////            string query = $"SELECT HoVaTen, NamSinh, CapBac, DonVi, SoHieu, GhiChu FROM DanhSach WHERE rowid IN ({inClause})";
////            using var cmd = new SqliteCommand(query, conn);
////            using var reader = cmd.ExecuteReader();
////            while (reader.Read())
////            {
////                token.ThrowIfCancellationRequested();
////                string hoTenGiaiMa = "";
////                if (!reader.IsDBNull(0))
////                {
////                    hoTenGiaiMa = Module_BaoMatAES.GiaiMa(reader.GetString(0))?.Trim() ?? "";
////                    hoTenGiaiMa = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(hoTenGiaiMa.ToLower());
////                }
////                result.Add(new TrungTenDetailModel
////                {
////                    HoTen = hoTenGiaiMa,
////                    NamSinh = !reader.IsDBNull(1) ? (Module_BaoMatAES.GiaiMa(reader.GetString(1)) ?? "") : "",
////                    CapBac = !reader.IsDBNull(2) ? (Module_BaoMatAES.GiaiMa(reader.GetString(2)) ?? "") : "",
////                    DonVi = !reader.IsDBNull(3) ? (Module_BaoMatAES.GiaiMa(reader.GetString(3))?.ToUpperInvariant() ?? "") : "",
////                    SoHieu = !reader.IsDBNull(4) ? (Module_BaoMatAES.GiaiMa(reader.GetString(4)) ?? "") : "",
////                    GhiChu = !reader.IsDBNull(5) ? (Module_BaoMatAES.GiaiMa(reader.GetString(5)) ?? "") : ""
////                });
////            }
////            return result;
////        }
////        private void HienThiTreeView()
////        {
////            if (kryptonTreeView_DieuHuong == null || kryptonTreeView_DieuHuong.IsDisposed) return;
////            // ⭐ KHÓA ĐÓNG BĂNG ĐỒ HỌA OS: Chặn đứng tình trạng chớp, giật khựng khung hình của bộ Krypton UI
////            SendMessage(kryptonTreeView_DieuHuong.Handle, WM_SETREDRAW, 0, "");
////            kryptonTreeView_DieuHuong.BeginUpdate();
////            kryptonTreeView_DieuHuong.Nodes.Clear();
////            var nodesArray = new TreeNode[_viewData.Count];
////            for (int i = 0; i < _viewData.Count; i++)
////            {
////                var item = _viewData[i];
////                TreeNode rootNode = new TreeNode($"{item.HoTen} ({item.SoLan})") { Tag = item };
////                rootNode.Nodes.Add(new TreeNode("Đang tải..."));
////                nodesArray[i] = rootNode;
////            }
////            kryptonTreeView_DieuHuong.Nodes.AddRange(nodesArray);
////            kryptonTreeView_DieuHuong.EndUpdate();
////            SendMessage(kryptonTreeView_DieuHuong.Handle, WM_SETREDRAW, 1, "");
////            kryptonTreeView_DieuHuong.Refresh();
////            SafeSelectFirstTreeNode();
////        }
////        private void SafeSelectFirstTreeNode()
////        {
////            try
////            {
////                if (IsDisposed || Disposing) return;
////                if (kryptonTreeView_DieuHuong == null || kryptonTreeView_DieuHuong.IsDisposed || !kryptonTreeView_DieuHuong.IsHandleCreated) return;
////                if (kryptonTreeView_DieuHuong.Nodes.Count == 0)
////                {
////                    kryptonTreeView_DieuHuong.SelectedNode = null;
////                    return;
////                }
////                TreeNode firstNode = kryptonTreeView_DieuHuong.Nodes[0];
////                if (ReferenceEquals(kryptonTreeView_DieuHuong.SelectedNode, firstNode)) return;
////                BeginInvoke(new MethodInvoker(() =>
////                {
////                    try
////                    {
////                        if (IsDisposed || Disposing || kryptonTreeView_DieuHuong == null || kryptonTreeView_DieuHuong.IsDisposed) return;
////                        if (kryptonTreeView_DieuHuong.Nodes.Count > 0)
////                        {
////                            kryptonTreeView_DieuHuong.SelectedNode = kryptonTreeView_DieuHuong.Nodes[0];
////                        }
////                    }
////                    catch (ObjectDisposedException) { }
////                }));
////            }
////            catch (Exception ex)
////            {
////                System.Diagnostics.Debug.WriteLine($"[SafeSelectFirstTreeNode]: {ex.Message}");
////            }
////        }
////        private void KryptonTreeView_DieuHuong_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
////        {
////            TreeNode expandingNode = e.Node;
////            if (expandingNode.Nodes.Count == 1 && expandingNode.Nodes[0].Text == "Đang tải...")
////            {
////                expandingNode.Nodes.Clear();
////                if (expandingNode.Tag is TrungTenModel model)
////                {
////                    var donViArray = model.DonVi.Split(_splitChars, StringSplitOptions.RemoveEmptyEntries);
////                    foreach (var dv in donViArray)
////                    {
////                        TreeNode childNode = new TreeNode(dv.Trim())
////                        {
////                            Tag = new { HoTen = model.HoTen, DonViStr = dv.Trim() }
////                        };
////                        expandingNode.Nodes.Add(childNode);
////                    }
////                }
////            }
////        }
////        private async void KryptonTreeView_DieuHuong_AfterSelect(object? sender, TreeViewEventArgs e)
////        {
////            if (_isTreeSelecting) return;
////            if (e.Node == null || e.Node.Tag == null) return;
////            try
////            {
////                _isTreeSelecting = true;
////                List<long> listRowIdCanTim = null;
////                _donViDuocChon = "";
////                if (e.Node.Tag is TrungTenModel modelCha)
////                {
////                    listRowIdCanTim = modelCha.RowIds;
////                }
////                else
////                {
////                    dynamic tagData = e.Node.Tag;
////                    string parentName = tagData.HoTen;
////                    var parentModel = _viewData.FirstOrDefault(x => x.HoTen == parentName);
////                    if (parentModel != null)
////                    {
////                        listRowIdCanTim = parentModel.RowIds;
////                    }
////                    string rawDonVi = tagData.DonViStr;
////                    int idx = rawDonVi.LastIndexOf(" (");
////                    if (idx > 0) _donViDuocChon = rawDonVi.Substring(0, idx).Trim();
////                    else _donViDuocChon = rawDonVi.Trim();
////                }
////                // ⭐ CẬP NHẬT MÀU SẮC LẬP TỨC: Ép lưới xóa vết vẽ của đồng chí cũ, chuyển vùng highlight mượt mà
////                if (kryptonDataGridView_TrungTen != null && !kryptonDataGridView_TrungTen.IsDisposed)
////                {
////                    kryptonDataGridView_TrungTen.Invalidate();
////                }
////                if (listRowIdCanTim == null || listRowIdCanTim.Count == 0) return;
////                if (Interlocked.Exchange(ref _isProcessing, 1) == 1) return;
////                BatProgress();
////                try
////                {
////                    var token = _cts.Token;
////                    _viewDataDetail = await Task.Run(() => TruyVanChiTietSQLite(listRowIdCanTim, token), token);
////                    HienThiGrid();
////                }
////                finally
////                {
////                    TatProgress();
////                    Interlocked.Exchange(ref _isProcessing, 0);
////                }
////            }
////            catch (Exception ex)
////            {
////                System.Diagnostics.Debug.WriteLine($"[TreeView AfterSelect Lỗi]: {ex.Message}");
////            }
////            finally
////            {
////                _isTreeSelecting = false;
////            }
////        }
////        private void HienThiGrid()
////        {
////            var grid = kryptonDataGridView_TrungTen;
////            if (grid == null || grid.IsDisposed) return;
////            grid.SuspendLayout();
////            try
////            {
////                grid.RowCount = _viewDataDetail != null ? _viewDataDetail.Count : 0;
////                if (grid.RowCount > 0)
////                {
////                    grid.ClearSelection();
////                    grid.CurrentCell = null;
////                }
////                grid.Invalidate();
////            }
////            finally
////            {
////                grid.ResumeLayout();
////            }
////        }
////        //endregion
////        //region CHUC NANG (TIM KIEM, LAM MOI)
////        private async void TimKiem()
////        {
////            if (Interlocked.Exchange(ref _isProcessing, 1) == 1) return;
////            BatProgress();
////            string keyword = textBoxKyToon_TimTen.Text.Trim();
////            _cts?.Cancel();
////            _cts = new CancellationTokenSource();
////            var token = _cts.Token;
////            try
////            {
////                var newViewData = await Task.Run(() =>
////                {
////                    if (string.IsNullOrEmpty(keyword)) return _data;
////                    return _data.Where(x => x.HoTen.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
////                }, token);
////                _viewData = newViewData;
////                HienThiTreeView();
////                _viewDataDetail.Clear();
////                HienThiGrid();
////                CapNhatStatus();
////            }
////            catch (OperationCanceledException) { }
////            finally
////            {
////                TatProgress();
////                Interlocked.Exchange(ref _isProcessing, 0);
////            }
////        }
////        private void kryptonButton_TimKiem_Click(object? sender, EventArgs e) => TimKiem();
////        private async void kryptonButton_LamMoi_Click(object? sender, EventArgs e)
////        {
////            textBoxKyToon_TimTen.Clear();
////            await TaiDuLieuHeThongAsync();
////        }
////        private void kryptonButton_Dong_Click(object? sender, EventArgs e)
////        {
////            // 1. Tìm Form cha đang mở
////            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
////            // 2. Nếu tìm thấy Form cha thì cập nhật lại tiêu đề
////            if (formCha != null)
////            {
////                formCha.CapNhatTieuDe("Trang phân loại thi đua");
////            }
////            // 3. Đóng form hiện tại
////            this.Close();
////        }
////        private void textBoxKyToon_TimTen_KeyDown(object? sender, KeyEventArgs e)
////        {
////            if (e.KeyCode == Keys.Enter)
////            {
////                e.SuppressKeyPress = true;
////                TimKiem();
////            }
////            else if (e.KeyCode == Keys.Escape)
////            {
////                e.SuppressKeyPress = true;
////                textBoxKyToon_TimTen.Clear();
////                TimKiem();
////            }
////        }
////        //endregion
////        //region TIEN ICH UI (PHIM TAT, STATUS STRIP)
////        private Action SafeAction(Action action)
////        {
////            return () =>
////            {
////                if (action == null || Interlocked.CompareExchange(ref _isProcessing, 0, 0) == 1) return;
////                try { action.Invoke(); }
////                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Lỗi thực thi phím tắt: " + ex.Message); }
////            };
////        }
////        private bool SafeExecute(Action action)
////        {
////            SafeAction(action).Invoke();
////            return true;
////        }
////        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
////        {
////            if (Interlocked.CompareExchange(ref _isProcessing, 0, 0) == 1) return base.ProcessCmdKey(ref msg, keyData);
////            try
////            {
////                if (Module_PhimTat.XuLy(keyData, actionLamMoi: SafeAction(() => lamMoi_ToolStripMenuItem_Click(null, null))))
////                {
////                    return true;
////                }
////                Keys key = keyData & Keys.KeyCode;
////                Keys modifier = keyData & Keys.Modifiers;
////                if (modifier == Keys.None && key == Keys.F4)
////                    return SafeExecute(() => xoaTimKiem_ToolStripMenuItem_Click(null, null));
////                if (modifier == Keys.Control && key == Keys.Q)
////                    return SafeExecute(() => quayLaiTrangXuLyDuLieu_ToolStripMenuItem_Click(null, null));
////            }
////            catch (Exception ex)
////            {
////                System.Diagnostics.Debug.WriteLine("Lỗi phím tắt: " + ex.Message);
////                return true;
////            }
////            return base.ProcessCmdKey(ref msg, keyData);
////        }
////        private void BatProgress()
////        {
////            toolStripProgressBar1.Visible = true;
////            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
////        }
////        private void TatProgress()
////        {
////            toolStripProgressBar1.Visible = false;
////        }
////        private void CapNhatStatus()
////        {
////            int total = _data.Count;
////            int filtered = _viewData.Count;
////            if (total == 0)
////            {
////                toolStripStatusLabel1.Text = "Không phát hiện tên trùng.";
////                return;
////            }
////            if (filtered == total)
////                toolStripStatusLabel1.Text = $"Số CBCS có tên trùng nhau: {total} {Module_HeThong.Tu_dong_chi}";
////            else
////                toolStripStatusLabel1.Text = $"Kết quả tìm kiếm: {filtered}/{total} CBCS trùng tên";
////        }
////        // 🌟 TỐI ƯU HIỆU SUẤT: Cờ chặn chống gọi hàm lặp lại gây tốn CPU
////        private bool _daKhoiTaoToolTip = false;
////        private void InitToolTips()
////        {
////            // Chống gọi lại nhiều lần không cần thiết
////            if (_daKhoiTaoToolTip) return;
////            // An toàn từ gốc: Kiểm tra ToolTip có tồn tại không
////            if (toolTip1 == null) return;
////            try
////            {
////                // ================= CẤU HÌNH CHUNG =================
////                toolTip1.IsBalloon = true;
////                toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
////                toolTip1.ToolTipIcon = ToolTipIcon.Info;
////                // UX: Phản hồi nhanh – không gây khó chịu khi rê chuột qua
////                toolTip1.InitialDelay = 300;
////                toolTip1.AutoPopDelay = 2500;
////                toolTip1.ReshowDelay = 100;
////                toolTip1.ShowAlways = true;
////                // 🌟 TỐI ƯU CẤU TRÚC RAM: Dùng mảng ValueTuple giúp gom nhóm gọn gàng
////                // Dễ dàng bảo trì, mở rộng sau này mà không xả rác bộ nhớ (Heap Allocation).
////                (System.Windows.Forms.Control? control, string noiDung)[] danhSachToolTip = new (System.Windows.Forms.Control?, string)[]
////                {
////                    (kryptonButton_TimKiem, "Bấm nút để tìm kiếm"),
////                    (kryptonButton_LamMoi,  "Làm mới dữ liệu"),
////                    (kryptonButton_Dong,    "Thoát trang")
////                };
////                // 🌟 XỬ LÝ LỖI PHÂN MẢNH (ISOLATED EXCEPTION)
////                int soLoi = 0;
////                foreach (var (control, noiDung) in danhSachToolTip)
////                {
////                    // Bỏ qua an toàn nếu control chưa kịp render hoặc bị null
////                    if (control == null)
////                    {
////                        soLoi++;
////                        continue;
////                    }
////                    try
////                    {
////                        // Kiểm tra vòng đời của Control trước khi gán API
////                        if (control.IsDisposed) continue;
////                        toolTip1.SetToolTip(control, noiDung);
////                    }
////                    catch
////                    {
////                        // Bẫy lỗi cục bộ: Lỗi ở 1 nút không làm sập vòng lặp gán của các nút khác
////                        soLoi++;
////                    }
////                }
//////if DEBUG
////                // Hệ thống cảnh báo nội bộ dành riêng cho Lập trình viên (Không hiện ở bản Release)
////                if (soLoi > 0)
////                    System.Diagnostics.Debug.WriteLine($"[InitToolTips] Hệ thống bỏ qua {soLoi} control do chưa khởi tạo hoặc bị null.");
//////endif
////                // Đánh dấu hoàn tất để khóa cổng
////                _daKhoiTaoToolTip = true;
////            }
////            catch (Exception ex)
////            {
////                // Bắt lỗi tổng và in ra Output để Lập trình viên theo dõi
////                System.Diagnostics.Debug.WriteLine($"[Lỗi nghiêm trọng tại InitToolTips]: {ex.Message}");
////            }
////        }
////        private void SetupStatusStrip()
////        {
////            if (statusStrip1 == null || statusStrip1.IsDisposed) return;
////            if (!statusStrip1.Items.Contains(_rightSpacer))
////            {
////                _rightSpacer.Spring = true;
////                _rightSpacer.Text = "";
////                _rightSpacer.AutoSize = false;
////                int index = statusStrip1.Items.IndexOf(toolStripStatusLabel2);
////                if (index >= 0) statusStrip1.Items.Insert(index, _rightSpacer);
////                else statusStrip1.Items.Add(_rightSpacer);
////            }
////            toolStripStatusLabel2.Alignment = ToolStripItemAlignment.Right;
////            toolStripStatusLabel2.TextAlign = ContentAlignment.MiddleRight;
////        }
////        private void XacDinhPhienBan()
////        {
////            bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
////            toolStripStatusLabel2.Text = laTanBinh ? "Phiên bản phần mềm dành cho tân binh" : "Phiên bản phần mềm dành cho CBCS";
////        }
////        private void SetupPlaceholder()
////        {
////            if (textBoxKyToon_TimTen != null && !textBoxKyToon_TimTen.IsDisposed)
////                SendMessage(textBoxKyToon_TimTen.Handle, EM_SETCUEBANNER, 0, "Nhập họ và tên CBCS để tìm kiếm");
////        }
////        private void lamMoi_ToolStripMenuItem_Click(object? sender, EventArgs e) => kryptonButton_LamMoi.PerformClick();
////        private void quayLaiTrangXuLyDuLieu_ToolStripMenuItem_Click(object? sender, EventArgs e) => kryptonButton_Dong.PerformClick();
////        private void xoaTimKiem_ToolStripMenuItem_Click(object? sender, EventArgs e)
////        {
////            textBoxKyToon_TimTen.Clear();
////            textBoxKyToon_TimTen.Focus();
////            TimKiem();
////        }
////        protected override void OnFormClosing(FormClosingEventArgs e)
////        {
////            // ⭐ AN TOÀN TUYỆT ĐỐI: Ngắt toàn bộ tiến trình chạy ngầm lập tức khi tắt form
////            _cts?.Cancel();
////            var grid = kryptonDataGridView_TrungTen;
////            if (grid != null)
////            {
////                grid.CellValueNeeded -= KryptonDataGridView_TrungTen_Detail_CellValueNeeded;
////                grid.RowPrePaint -= KryptonDataGridView_TrungTen_Detail_RowPrePaint;
////            }
////            if (kryptonTreeView_DieuHuong != null)
////            {
////                kryptonTreeView_DieuHuong.BeforeExpand -= KryptonTreeView_DieuHuong_BeforeExpand;
////                kryptonTreeView_DieuHuong.AfterSelect -= KryptonTreeView_DieuHuong_AfterSelect;
////            }
////            // Giải phóng triệt để tài nguyên đồ họa GDI+ tránh treo RAM hệ điều hành
////            _headerFont?.Dispose();
////            _boldFont?.Dispose();
////            _cts?.Dispose();
////            base.OnFormClosing(e);
////        }
////        //endregion
////    }
////    public class TrungTenModel
////    {
////        public string HoTen { get; set; } = "";
////        public int SoLan { get; set; }
////        public string DonVi { get; set; } = "";
////        public List<long> RowIds { get; set; } = new();
////    }
////    public class TrungTenDetailModel
////    {
////        public string HoTen { get; set; } = "";
////        public string NamSinh { get; set; } = "";
////        public string CapBac { get; set; } = "";
////        public string DonVi { get; set; } = "";
////        public string SoHieu { get; set; } = "";
////        public string GhiChu { get; set; } = "";
////    }
////}
///Phiên bản 2
///
///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
using Microsoft.Data.Sqlite;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
namespace PhanMemThiDua2026
{
    public partial class Form28_DataTrungTen : Form
    {
        #region Hằng số & trường dữ liệu
        private const int EM_SETCUEBANNER = 0x1501;
        private const int MaxRecords = 500_000;
        private const int MaxDetailCache = 200;
        private const int SqlChunkSize = 900;
        private const string TenChuaCoDonVi = "(Chưa có đơn vị)";
        private static readonly TextInfo _textInfo = CultureInfo.CurrentCulture.TextInfo;
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, string lParam);
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private string? _connectionString;
        private List<TrungTenModel> _data = new();
        private List<TrungTenModel> _viewData = new();
        private List<TrungTenDetailModel> _viewDataDetail = new();
        private TrungTenModel? _modelDangHienThi;
        private readonly Dictionary<string, List<TrungTenDetailModel>> _detailCache = new(StringComparer.OrdinalIgnoreCase);
        private int _isLoading;
        // "Dấu vân tay" của file CSDL2 tại lần nạp gần nhất -> biết CSDL có bị Form6 thêm/xóa/sửa sau đó hay không
        private string _dauVanTayCSDL = "";
        private bool _daTaiLanDau;
        private string? _tenCanChonLai;
        private int _busyCount;
        private int _soBanGhiLoi;
        private bool _boQuaSelection;
        private bool _daKhoiTaoToolTip;
        private readonly bool _laTanBinh;
        private string _donViDuocChon = "";
        private CancellationTokenSource? _loadCts;
        private CancellationTokenSource? _detailCts;
        private readonly System.Windows.Forms.Timer _searchTimer = new() { Interval = 250 };
        private readonly System.Windows.Forms.Timer _selectTimer = new() { Interval = 120 };
        private readonly List<Font> _ownedFonts = new();
        // --- Controls ---
        private Panel _header = null!;
        private ModernButton _btnLamMoi = null!;
        private ModernButton _btnDong = null!;
        private ModernButton _btnTimKiem = null!;
        private Button _btnXoaTimKiem = null!;
        private TextBox _txtSearch = null!;
        private Label _kpiTen = null!;
        private Label _kpiHoSo = null!;
        private Label _kpiMax = null!;
        private Label _lblMasterCount = null!;
        private Label _lblEmptyMaster = null!;
        private Label _lblDetailTitle = null!;
        private Label _lblDetailSub = null!;
        private Label _lblChipHint = null!;
        private Label _lblEmptyDetail = null!;
        private TableLayoutPanel _tlpDetail = null!;
        private FlowLayoutPanel _chipPanel = null!;
        private DataGridView _gridMaster = null!;
        private DataGridView _gridDetail = null!;
        private StatusStrip _statusStrip = null!;
        private ToolStripProgressBar _progress = null!;
        private ToolStripStatusLabel _lblStatus = null!;
        private ToolStripStatusLabel _lblVersion = null!;
        private ContextMenuStrip _ctxMenu = null!;
        private ToolStripMenuItem _mnuLamMoi = null!;
        private ToolStripMenuItem _mnuXoaTimKiem = null!;
        private ToolStripMenuItem _mnuDong = null!;
        private ToolTip _toolTip = null!;
        private Font _boldFont = null!;
        private Font _chipFont = null!;
        #endregion
        #region Khởi tạo
        public Form28_DataTrungTen()
        {
            components = new Container();
            _laTanBinh = LaTanBinh();
            BuildUi();
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            AcceptButton = _btnTimKiem;
            _searchTimer.Tick += (_, _) => { _searchTimer.Stop(); ApDungBoLoc(); };
            _selectTimer.Tick += (_, _) => { _selectTimer.Stop(); TaiChiTietAsync(); };
            Load += Form28_DataTrungTen_Load;
        }
        private static bool LaTanBinh()
        {
            string pb = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
            return pb.Contains("tân binh", StringComparison.OrdinalIgnoreCase);
        }
        private async void Form28_DataTrungTen_Load(object? sender, EventArgs e)
        {
            Module_MenuChuotPhai.TichHopGiaoDien(_ctxMenu);
            XacDinhPhienBan();
            SetupPlaceholder();
            InitToolTips();
            await TaiDuLieuHeThongAsync();
        }
        private Font NewFont(string family, float size, FontStyle style = FontStyle.Regular)
        {
            var f = new Font(family, size, style, GraphicsUnit.Point);
            _ownedFonts.Add(f);
            return f;
        }
        private Image? ResImg(string name)
        {
            try
            {
                var rm = new ComponentResourceManager(typeof(Form28_DataTrungTen));
                return rm.GetObject(name) as Image;
            }
            catch { return null; }
        }
        private void GiaiPhongTaiNguyenTuyChinh()
        {
            _searchTimer.Dispose();
            _selectTimer.Dispose();
            _loadCts?.Dispose();
            _detailCts?.Dispose();
            foreach (var f in _ownedFonts) f.Dispose();
            _ownedFonts.Clear();
        }
        #endregion
        #region Dựng giao diện (kiểu web / card)
        private void BuildUi()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1264, 681);
            Font = NewFont("Segoe UI", 9.75F);
            BackColor = UiColors.Bg;
            Text = "Phân tích CBCS trùng tên";
            ShowInTaskbar = false;
            try
            {
                var icon = new ComponentResourceManager(typeof(Form28_DataTrungTen)).GetObject("$this.Icon") as Icon;
                if (icon != null) Icon = icon;
            }
            catch { /* icon không bắt buộc */ }
            var fTitle = NewFont("Segoe UI Semibold", 15F);
            var fCaption = NewFont("Segoe UI Semibold", 8.25F);
            var fKpi = NewFont("Segoe UI Semibold", 20F);
            var fKpiText = NewFont("Segoe UI Semibold", 12F);
            var fCardTitle = NewFont("Segoe UI Semibold", 11.5F);
            var fSmall = NewFont("Segoe UI", 9F);
            var fBtn = NewFont("Segoe UI Semibold", 9.75F);
            var fInput = NewFont("Segoe UI", 11F);
            var fIcon = NewFont("Segoe MDL2 Assets", 12F);
            _boldFont = NewFont("Segoe UI Semibold", 9.75F);
            _chipFont = NewFont("Segoe UI", 9F);
            // ===== Context menu =====
            _ctxMenu = new ContextMenuStrip(components) { Font = new Font("Segoe UI", 9F), ImageScalingSize = new Size(21, 21) };
            _ownedFonts.Add(_ctxMenu.Font);
            _mnuLamMoi = new ToolStripMenuItem("Làm mới trang") { ShortcutKeys = Keys.F5, Image = ResImg("lamMoi_ToolStripMenuItem.Image") };
            _mnuXoaTimKiem = new ToolStripMenuItem("Xóa tìm kiếm") { ShortcutKeys = Keys.F4, Image = ResImg("xoaTimKiem_ToolStripMenuItem.Image") };
            _mnuDong = new ToolStripMenuItem("Đóng trang") { ShortcutKeys = Keys.Control | Keys.Q, Image = ResImg("quayLaiTrangXuLyDuLieu_ToolStripMenuItem.Image") };
            _mnuLamMoi.Click += lamMoi_ToolStripMenuItem_Click;
            _mnuXoaTimKiem.Click += xoaTimKiem_ToolStripMenuItem_Click;
            _mnuDong.Click += quayLaiTrangXuLyDuLieu_ToolStripMenuItem_Click;
            _ctxMenu.Items.AddRange(new ToolStripItem[] { _mnuLamMoi, new ToolStripSeparator(), _mnuXoaTimKiem, _mnuDong });
            ContextMenuStrip = _ctxMenu;
            // ===== Status strip =====
            _statusStrip = new StatusStrip { Dock = DockStyle.Bottom, BackColor = Color.White, SizingGrip = false, ImageScalingSize = new Size(20, 20) };
            _progress = new ToolStripProgressBar { Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 30, Size = new Size(90, 16), Visible = false };
            _lblStatus = new ToolStripStatusLabel("Sẵn sàng") { Spring = true, TextAlign = ContentAlignment.MiddleLeft, Image = ResImg("toolStripStatusLabel1.Image"), ForeColor = UiColors.Text };
            _lblVersion = new ToolStripStatusLabel { Alignment = ToolStripItemAlignment.Right, ForeColor = UiColors.Muted, TextAlign = ContentAlignment.MiddleRight };
            _statusStrip.Items.AddRange(new ToolStripItem[] { _progress, _lblStatus, _lblVersion });
            // ===== Header =====
            _header = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Color.White };
            _header.Paint += (s, e) =>
            {
                using var p = new Pen(UiColors.Border);
                e.Graphics.DrawLine(p, 0, _header.Height - 1, _header.Width, _header.Height - 1);
            };
            var badge = new RoundedPanel { Location = new Point(24, 15), Size = new Size(42, 42), Radius = 12, FillColor = UiColors.GreenSoft, BorderColor = UiColors.GreenSoft };
            badge.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "\uE716", Font = fIcon, ForeColor = UiColors.GreenDark, TextAlign = ContentAlignment.MiddleCenter });
            var lblTitle = new Label { AutoSize = true, Text = "Phân tích CBCS trùng tên", Font = fTitle, ForeColor = UiColors.Text, Location = new Point(78, 10) };
            var lblSub = new Label { AutoSize = true, Text = "Phát hiện và đối chiếu các họ tên trùng nhau trong cơ sở dữ liệu", Font = fSmall, ForeColor = UiColors.Muted, Location = new Point(80, 42) };
            //_btnLamMoi = new ModernButton { Text = "Làm mới", Glyph = "\uE72C", Font = fBtn, Size = new Size(120, 38) };
            //_btnDong = new ModernButton { Text = "Thoát", Glyph = "\uE7E8", Font = fBtn, Size = new Size(110, 38) };
            _btnLamMoi = new ModernButton { Text = "Làm mới", Glyph = "\uE72C", Font = fBtn, Size = new Size(120, 38), IsPurple = true };
            _btnDong = new ModernButton { Text = "Thoát", Glyph = "\uE7E8", Font = fBtn, Size = new Size(110, 38), IsPurple = true };
            _btnLamMoi.Click += kryptonButton_LamMoi_Click;
            _btnDong.Click += kryptonButton_Dong_Click;
            _header.Controls.AddRange(new Control[] { badge, lblTitle, lblSub, _btnLamMoi, _btnDong });
            _header.Resize += (_, _) => DatViTriNutHeader();
            DatViTriNutHeader();
            // ===== Body =====
            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 16, 20, 12), BackColor = UiColors.Bg };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = UiColors.Bg };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 84F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            body.Controls.Add(layout);
            // --- Toolbar: tìm kiếm + KPI ---
            var toolbar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = UiColors.Bg, Margin = Padding.Empty };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270F));
            toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            var cardSearch = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 12), Padding = new Padding(16, 8, 10, 8) };
            var tlpSearch = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2, BackColor = Color.White };
            tlpSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28F));
            tlpSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32F));
            tlpSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 122F));
            tlpSearch.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tlpSearch.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            var lblCap = new Label { Text = _laTanBinh ? "HỌ VÀ TÊN TÂN BINH" : "HỌ VÀ TÊN CBCS", Font = fCaption, ForeColor = UiColors.Muted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
            tlpSearch.Controls.Add(lblCap, 0, 0);
            tlpSearch.SetColumnSpan(lblCap, 4);
            tlpSearch.Controls.Add(new Label { Text = "\uE721", Font = fIcon, ForeColor = UiColors.Muted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
            _txtSearch = new TextBox { BorderStyle = BorderStyle.None, Font = fInput, Anchor = AnchorStyles.Left | AnchorStyles.Right, BackColor = Color.White, ForeColor = UiColors.Text };
            _txtSearch.TextChanged += TxtSearch_TextChanged;
            _txtSearch.KeyDown += textBoxKyToon_TimTen_KeyDown;
            tlpSearch.Controls.Add(_txtSearch, 1, 1);
            _btnXoaTimKiem = new Button
            {
                Text = "\uE711",
                Font = NewFont("Segoe MDL2 Assets", 9F),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(26, 26),
                Anchor = AnchorStyles.None,
                ForeColor = UiColors.Muted,
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                Visible = false,
                TabStop = false
            };
            _btnXoaTimKiem.FlatAppearance.BorderSize = 0;
            _btnXoaTimKiem.FlatAppearance.MouseOverBackColor = UiColors.Hover;
            _btnXoaTimKiem.Click += (_, _) => xoaTimKiem_ToolStripMenuItem_Click(null, EventArgs.Empty);
            tlpSearch.Controls.Add(_btnXoaTimKiem, 2, 1);
            _btnTimKiem = new ModernButton { Text = "Tìm kiếm", Glyph = "\uE721", Primary = true, Font = fBtn, Size = new Size(112, 34), Anchor = AnchorStyles.None };
            _btnTimKiem.Click += kryptonButton_TimKiem_Click;
            tlpSearch.Controls.Add(_btnTimKiem, 3, 1);
            cardSearch.Controls.Add(tlpSearch);
            toolbar.Controls.Add(cardSearch, 0, 0);
            toolbar.Controls.Add(TaoKpi("TÊN TRÙNG", fCaption, fKpi, out _kpiTen, new Padding(0, 0, 12, 12)), 1, 0);
            toolbar.Controls.Add(TaoKpi("HỒ SƠ LIÊN QUAN", fCaption, fKpi, out _kpiHoSo, new Padding(0, 0, 12, 12)), 2, 0);
            toolbar.Controls.Add(TaoKpi("TRÙNG NHIỀU NHẤT", fCaption, fKpiText, out _kpiMax, new Padding(0, 0, 0, 12)), 3, 0);
            layout.Controls.Add(toolbar, 0, 0);
            // --- Hai card chính ---
            var tlpBody = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = UiColors.Bg, Margin = Padding.Empty };
            tlpBody.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340F));
            tlpBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpBody.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            // Card trái: danh sách tên trùng
            var cardMaster = new RoundedPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0), Padding = new Padding(14, 12, 14, 12) };
            var tlpMaster = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = Color.White };
            tlpMaster.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMaster.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpMaster.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tlpMaster.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMaster.Controls.Add(new Label { Text = "Danh sách tên trùng", Font = fCardTitle, ForeColor = UiColors.Text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            _lblMasterCount = new Label { Text = "0", Font = _boldFont, ForeColor = UiColors.GreenDark, BackColor = UiColors.GreenSoft, AutoSize = true, Padding = new Padding(8, 2, 8, 2), Anchor = AnchorStyles.Right };
            tlpMaster.Controls.Add(_lblMasterCount, 1, 0);
            var masterHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            _gridMaster = TaoGrid();
            _gridMaster.Columns.Add(new DataGridViewTextBoxColumn { Name = "HoTen", HeaderText = "Họ và tên", FillWeight = 100, MinimumWidth = 120, SortMode = DataGridViewColumnSortMode.NotSortable });
            _gridMaster.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SoLan",
                HeaderText = "Số lần",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 76,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            _gridMaster.CellValueNeeded += GridMaster_CellValueNeeded;
            _gridMaster.CellFormatting += GridMaster_CellFormatting;
            _gridMaster.SelectionChanged += GridMaster_SelectionChanged;
            _lblEmptyMaster = TaoNhanTrong(fSmall);
            masterHost.Controls.Add(_gridMaster);
            masterHost.Controls.Add(_lblEmptyMaster);
            tlpMaster.Controls.Add(masterHost, 0, 1);
            tlpMaster.SetColumnSpan(masterHost, 2);
            cardMaster.Controls.Add(tlpMaster);
            // Card phải: chi tiết
            var cardDetail = new RoundedPanel { Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = new Padding(18, 14, 18, 12) };
            _tlpDetail = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, BackColor = Color.White };
            _tlpDetail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _tlpDetail.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            _tlpDetail.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            _tlpDetail.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
            _tlpDetail.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _tlpDetail.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _lblDetailTitle = new Label { Text = "Chi tiết hồ sơ", Font = fCardTitle, ForeColor = UiColors.Text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
            _lblDetailSub = new Label { Text = "", Font = fSmall, ForeColor = UiColors.Muted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft };
            _lblChipHint = new Label { Text = "Đơn vị — nhấn để làm nổi bật các dòng thuộc đơn vị đó", Font = fSmall, ForeColor = UiColors.Muted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Visible = false };
            _chipPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = Padding.Empty,
                BackColor = Color.White,
                MaximumSize = new Size(0, 100),
                AutoScroll = true,
                Visible = false
            };
            _tlpDetail.Controls.Add(_lblDetailTitle, 0, 0);
            _tlpDetail.Controls.Add(_lblDetailSub, 0, 1);
            _tlpDetail.Controls.Add(_lblChipHint, 0, 2);
            _tlpDetail.Controls.Add(_chipPanel, 0, 3);
            var detailHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0, 6, 0, 0) };
            _gridDetail = TaoGrid();
            _gridDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "STT", HeaderText = "STT", FillWeight = 6, MinimumWidth = 44, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            _gridDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "HoTen", HeaderText = _laTanBinh ? "Họ và tên tân binh" : "Họ và tên CBCS", FillWeight = 26, MinimumWidth = 120, SortMode = DataGridViewColumnSortMode.NotSortable });
            _gridDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "NamSinh", HeaderText = "Năm sinh", FillWeight = 12, MinimumWidth = 70, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            _gridDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "CapBac", HeaderText = "Cấp bậc", FillWeight = 14, MinimumWidth = 70, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            _gridDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "DonVi", HeaderText = "Đơn vị", FillWeight = 30, MinimumWidth = 120, SortMode = DataGridViewColumnSortMode.NotSortable });
            _gridDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoHieu", HeaderText = "Số hiệu", FillWeight = 12, MinimumWidth = 70, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            _gridDetail.Columns.Add(new DataGridViewTextBoxColumn { Name = "GhiChu", HeaderText = "Ghi chú", FillWeight = 20, MinimumWidth = 80, SortMode = DataGridViewColumnSortMode.NotSortable });
            _gridDetail.CellValueNeeded += GridDetail_CellValueNeeded;
            _gridDetail.CellFormatting += GridDetail_CellFormatting;
            _lblEmptyDetail = TaoNhanTrong(fSmall);
            _lblEmptyDetail.Text = "Chọn một họ tên ở danh sách bên trái để xem chi tiết hồ sơ.";
            _lblEmptyDetail.Visible = true;
            _gridDetail.Visible = false;
            detailHost.Controls.Add(_gridDetail);
            detailHost.Controls.Add(_lblEmptyDetail);
            _tlpDetail.Controls.Add(detailHost, 0, 4);
            cardDetail.Controls.Add(_tlpDetail);
            tlpBody.Controls.Add(cardMaster, 0, 0);
            tlpBody.Controls.Add(cardDetail, 1, 0);
            layout.Controls.Add(tlpBody, 0, 1);
            // Thứ tự Add: Fill trước, rồi Bottom, rồi Top
            Controls.Add(body);
            Controls.Add(_statusStrip);
            Controls.Add(_header);
            _gridMaster.ContextMenuStrip = _ctxMenu;
            _gridDetail.ContextMenuStrip = _ctxMenu;
            ResumeLayout(false);
            PerformLayout();
        }
        private void DatViTriNutHeader()
        {
            if (_header == null || _btnDong == null || _btnLamMoi == null) return;
            int top = (_header.Height - _btnDong.Height) / 2;
            _btnDong.Location = new Point(_header.Width - 24 - _btnDong.Width, top);
            _btnLamMoi.Location = new Point(_btnDong.Left - 10 - _btnLamMoi.Width, top);
        }
        private RoundedPanel TaoKpi(string caption, Font fCap, Font fVal, out Label value, Padding margin)
        {
            var card = new RoundedPanel { Dock = DockStyle.Fill, Margin = margin, Padding = new Padding(16, 10, 12, 8) };
            value = new Label { Dock = DockStyle.Fill, Text = "0", Font = fVal, ForeColor = UiColors.GreenDark, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
            var cap = new Label { Dock = DockStyle.Top, Height = 20, Text = caption, Font = fCap, ForeColor = UiColors.Muted, TextAlign = ContentAlignment.MiddleLeft };
            card.Controls.Add(value);
            card.Controls.Add(cap);
            return card;
        }
        private static Label TaoNhanTrong(Font f) => new()
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = UiColors.Muted,
            BackColor = Color.White,
            Font = f,
            Visible = false
        };
        private DataGridView TaoGrid()
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToOrderColumns = false,
                ReadOnly = true,
                MultiSelect = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EnableHeadersVisualStyles = false,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 38,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                GridColor = UiColors.GridLine,
                ShowCellToolTips = false,
                VirtualMode = true,
                StandardTab = true
            };
            g.RowTemplate.Height = 38;
            g.ColumnHeadersDefaultCellStyle.BackColor = UiColors.HeaderBack;
            g.ColumnHeadersDefaultCellStyle.ForeColor = UiColors.Muted;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = UiColors.HeaderBack;
            g.ColumnHeadersDefaultCellStyle.SelectionForeColor = UiColors.Muted;
            g.ColumnHeadersDefaultCellStyle.Font = _boldFont;
            g.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            g.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            g.DefaultCellStyle.BackColor = Color.White;
            g.DefaultCellStyle.ForeColor = UiColors.Text;
            g.DefaultCellStyle.SelectionBackColor = UiColors.GreenSoft;
            g.DefaultCellStyle.SelectionForeColor = UiColors.GreenDark;
            g.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            g.AlternatingRowsDefaultCellStyle.BackColor = UiColors.ZebraRow;
            g.AlternatingRowsDefaultCellStyle.SelectionBackColor = UiColors.GreenSoft;
            g.AlternatingRowsDefaultCellStyle.SelectionForeColor = UiColors.GreenDark;
            BatDoubleBuffer(g);
            return g;
        }
        private static void BatDoubleBuffer(Control c) =>
            typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(c, true);
        #endregion
        #region Nạp & phân tích dữ liệu
        private string LayChuoiKetNoi()
        {
            return _connectionString ??= new SqliteConnectionStringBuilder
            {
                DataSource = _csdl2Path,
                Mode = SqliteOpenMode.ReadOnly,
                DefaultTimeout = 5,
                Pooling = true,
                Cache = SqliteCacheMode.Private
            }.ConnectionString;
        }
        private static void ApDungPragmaDoc(SqliteConnection conn)
        {
            // Kết nối ReadOnly: không đổi journal_mode (vô nghĩa/gây lỗi). Chỉ tăng cache + mmap để đọc nhanh hơn.
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA temp_store = MEMORY; PRAGMA cache_size = -65536; PRAGMA mmap_size = 268435456;";
            cmd.ExecuteNonQuery();
        }
        /// <summary>
        /// Dấu vân tay file CSDL2 (thời gian ghi + dung lượng của file .db và file -wal).
        /// Mọi thao tác thêm/xóa/sửa của Form6 đều làm đổi dấu này. Không đọc được thì trả giá trị luôn khác để buộc nạp lại.
        /// </summary>
        private string LayDauVanTayCSDL()
        {
            try
            {
                var sb = new StringBuilder(96);
                foreach (string p in new[] { _csdl2Path, _csdl2Path + "-wal" })
                {
                    var fi = new FileInfo(p);
                    if (fi.Exists) sb.Append(fi.LastWriteTimeUtc.Ticks).Append(':').Append(fi.Length).Append('|');
                    else sb.Append("-|");
                }
                return sb.ToString();
            }
            catch
            {
                return Guid.NewGuid().ToString();
            }
        }
        /// <summary>
        /// Form được Form6 giữ lại trong panel (chỉ Hide/Show chứ không tạo mới) nên Load chỉ chạy đúng 1 lần.
        /// Mỗi lần form hiện lại: nếu CSDL đã đổi so với lần nạp trước thì nạp lại ngay, nếu không đổi thì giữ nguyên (khỏi tốn công).
        /// </summary>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && _daTaiLanDau && IsHandleCreated && !IsDisposed)
                _ = NapLaiNeuCSDLDaDoiAsync();
        }
        /// <summary>Có thể gọi từ nơi khác (vd. Form6) để ép form kiểm tra và cập nhật dữ liệu mới nhất.</summary>
        public async Task NapLaiNeuCSDLDaDoiAsync()
        {
            if (IsDisposed || !_daTaiLanDau) return;
            if (Volatile.Read(ref _isLoading) == 1) return;
            if (LayDauVanTayCSDL() == _dauVanTayCSDL) return;
            await TaiDuLieuHeThongAsync();
        }
        private async Task TaiDuLieuHeThongAsync()
        {
            if (Interlocked.Exchange(ref _isLoading, 1) == 1) return;
            // Chụp dấu vân tay TRƯỚC khi đọc: nếu CSDL đổi trong lúc đang nạp thì lần hiện form sau sẽ tự nạp lại
            string dauVanTayMoi = LayDauVanTayCSDL();
            SetStatus("Đang phân tích dữ liệu...");
            BatProgress();
            _detailCts?.Cancel();
            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = new CancellationTokenSource();
            var token = _loadCts.Token;
            try
            {
                var result = await Task.Run(() => PhanTichDuLieu(token), token);
                if (IsDisposed || token.IsCancellationRequested) return;
                _data = result ?? new List<TrungTenModel>();
                _detailCache.Clear();
                _tenCanChonLai = _modelDangHienThi?.HoTen; // nạp lại xong vẫn giữ tên đang xem (nếu còn trùng)
                _modelDangHienThi = null;
                CapNhatKpi();
                ApDungBoLoc();
                _tenCanChonLai = null;
                _dauVanTayCSDL = dauVanTayMoi;
                _daTaiLanDau = true;
                _txtSearch.Focus();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!IsDisposed) SetStatus("Lỗi khi phân tích dữ liệu.");
                System.Diagnostics.Debug.WriteLine($"[Form28 Load Lỗi]: {ex}");
                if (!IsDisposed)
                    MessageBox.Show("Có lỗi nghiêm trọng xảy ra trong quá trình kết nối và phân tích dữ liệu CSDL.", "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                TatProgress();
                Interlocked.Exchange(ref _isLoading, 0);
            }
        }
        /// <summary>
        /// Quét 1 lượt toàn bộ bảng. Dùng danh sách phẳng + liên kết "prev" thay cho Dictionary lồng nhau,
        /// nên tên không trùng (đa số) gần như không tốn thêm đối tượng nào → nhanh và nhẹ RAM hơn nhiều.
        /// </summary>
        private List<TrungTenModel> PhanTichDuLieu(CancellationToken token)
        {
            var head = new Dictionary<string, int>(16384, StringComparer.Ordinal);
            var rowIds = new List<long>(16384);
            var units = new List<string>(16384);
            var prev = new List<int>(16384);
            var cacheTen = new Dictionary<string, string>(16384, StringComparer.Ordinal);
            var cacheDv = new Dictionary<string, string>(2048, StringComparer.Ordinal);
            int loi = 0;
            using (var conn = new SqliteConnection(LayChuoiKetNoi()))
            {
                conn.Open();
                ApDungPragmaDoc(conn);
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT rowid, HoVaTen, DonVi FROM DanhSach WHERE HoVaTen IS NOT NULL";
                using var reader = cmd.ExecuteReader();
                int count = 0;
                while (reader.Read())
                {
                    if ((++count & 1023) == 0) token.ThrowIfCancellationRequested();
                    if (count > MaxRecords) throw new InvalidOperationException("Vượt quá giới hạn bộ nhớ an toàn của phân hệ.");
                    string maHoaTen = reader.GetString(1);
                    if (string.IsNullOrWhiteSpace(maHoaTen)) continue;
                    if (!cacheTen.TryGetValue(maHoaTen, out var hoTen))
                    {
                        try { hoTen = ChuanHoaTen(Module_BaoMatAES.GiaiMa(maHoaTen) ?? ""); }
                        catch { hoTen = ""; loi++; }
                        if (cacheTen.Count < 200_000) cacheTen[maHoaTen] = hoTen;
                    }
                    if (hoTen.Length == 0) continue;
                    string donVi = "";
                    if (!reader.IsDBNull(2))
                    {
                        string maHoaDv = reader.GetString(2);
                        if (maHoaDv.Length > 0)
                        {
                            if (!cacheDv.TryGetValue(maHoaDv, out var dv))
                            {
                                try { dv = (Module_BaoMatAES.GiaiMa(maHoaDv) ?? "").Trim().ToUpperInvariant(); }
                                catch { dv = ""; loi++; }
                                cacheDv[maHoaDv] = dv;
                            }
                            donVi = dv;
                        }
                    }
                    long rowId = reader.GetInt64(0);
                    int idx = rowIds.Count;
                    rowIds.Add(rowId);
                    units.Add(donVi);
                    ref int slot = ref CollectionsMarshal.GetValueRefOrAddDefault(head, hoTen, out bool exists);
                    prev.Add(exists ? slot : -1);
                    slot = idx;
                }
            }
            var result = new List<TrungTenModel>();
            var chain = new List<int>(8);
            var unitNames = new List<string>(4);
            var unitCounts = new List<int>(4);
            var sb = new StringBuilder(128);
            int n = 0;
            foreach (var kv in head)
            {
                if ((++n & 511) == 0) token.ThrowIfCancellationRequested();
                int last = kv.Value;
                if (prev[last] < 0) continue; // tên chỉ xuất hiện 1 lần
                chain.Clear(); unitNames.Clear(); unitCounts.Clear();
                for (int i = last; i >= 0; i = prev[i]) chain.Add(i);
                chain.Reverse();
                var ids = new List<long>(chain.Count);
                foreach (int i in chain)
                {
                    ids.Add(rowIds[i]);
                    string u = units[i];
                    int k = -1;
                    for (int j = 0; j < unitNames.Count; j++)
                        if (string.Equals(unitNames[j], u, StringComparison.OrdinalIgnoreCase)) { k = j; break; }
                    if (k < 0) { unitNames.Add(u); unitCounts.Add(1); }
                    else unitCounts[k]++;
                }
                sb.Clear();
                for (int j = 0; j < unitNames.Count; j++)
                {
                    if (j > 0) sb.Append("; ");
                    sb.Append(unitNames[j].Length == 0 ? TenChuaCoDonVi : unitNames[j]).Append(" (").Append(unitCounts[j]).Append(')');
                }
                result.Add(new TrungTenModel
                {
                    HoTen = kv.Key,
                    SoLan = chain.Count,
                    DonVi = sb.ToString(),
                    RowIds = ids,
                    KeyTimKiem = BoDau(kv.Key)
                });
            }
            result.Sort((a, b) =>
            {
                int c = b.SoLan.CompareTo(a.SoLan);
                return c != 0 ? c : string.Compare(a.HoTen, b.HoTen, StringComparison.CurrentCultureIgnoreCase);
            });
            _soBanGhiLoi = loi;
            return result;
        }
        private List<TrungTenDetailModel> TruyVanChiTietSQLite(List<long> ids, CancellationToken token)
        {
            if (ids == null || ids.Count == 0) return new List<TrungTenDetailModel>();
            var result = new List<TrungTenDetailModel>(ids.Count);
            using var conn = new SqliteConnection(LayChuoiKetNoi());
            conn.Open();
            ApDungPragmaDoc(conn);
            var sb = new StringBuilder(SqlChunkSize * 8 + 128);
            for (int start = 0; start < ids.Count; start += SqlChunkSize)
            {
                token.ThrowIfCancellationRequested();
                int end = Math.Min(start + SqlChunkSize, ids.Count);
                sb.Clear();
                sb.Append("SELECT HoVaTen, NamSinh, CapBac, DonVi, SoHieu, GhiChu FROM DanhSach WHERE rowid IN (");
                for (int i = start; i < end; i++)
                {
                    if (i > start) sb.Append(',');
                    sb.Append(ids[i]); // long → an toàn, không thể chèn SQL
                }
                sb.Append(')');
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sb.ToString();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    token.ThrowIfCancellationRequested();
                    result.Add(new TrungTenDetailModel
                    {
                        HoTen = ChuanHoaTen(GiaiMaCot(reader, 0)),
                        NamSinh = GiaiMaCot(reader, 1).Trim(),
                        CapBac = GiaiMaCot(reader, 2).Trim(),
                        DonVi = GiaiMaCot(reader, 3).Trim().ToUpperInvariant(),
                        SoHieu = GiaiMaCot(reader, 4).Trim(),
                        GhiChu = GiaiMaCot(reader, 5).Trim()
                    });
                }
            }
            // Sắp theo đơn vị để các bản ghi cùng đơn vị đứng cạnh nhau (OrderBy là sắp xếp ổn định)
            return result.OrderBy(x => x.DonVi, StringComparer.CurrentCultureIgnoreCase).ToList();
        }
        private static string GiaiMaCot(SqliteDataReader r, int i)
        {
            if (r.IsDBNull(i)) return "";
            try { return Module_BaoMatAES.GiaiMa(r.GetString(i)) ?? ""; }
            catch { return ""; }
        }
        private static string ChuanHoaTen(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var sb = new StringBuilder(raw.Length);
            bool space = false;
            foreach (char c in raw.Trim())
            {
                if (char.IsWhiteSpace(c)) { space = true; continue; }
                if (space) { sb.Append(' '); space = false; }
                sb.Append(c);
            }
            return _textInfo.ToTitleCase(sb.ToString().ToLowerInvariant()).Normalize(NormalizationForm.FormC);
        }
        private static string BoDau(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            string d = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(d.Length);
            foreach (char c in d)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                sb.Append(c == 'đ' || c == 'Đ' ? 'd' : char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }
        #endregion
        #region Hiển thị danh sách (master) & chi tiết
        private void ApDungBoLoc()
        {
            string kw = _txtSearch.Text.Trim();
            if (kw.Length == 0) _viewData = _data;
            else
            {
                string key = BoDau(kw); // tìm không phân biệt dấu: "nguyen" khớp "Nguyễn"
                _viewData = _data.FindAll(x => x.KeyTimKiem.Contains(key, StringComparison.Ordinal));
            }
            CapNhatMasterGrid();
        }
        private void CapNhatMasterGrid()
        {
            var truoc = _modelDangHienThi;
            _boQuaSelection = true;
            try
            {
                bool has = _viewData.Count > 0;
                _gridMaster.Visible = has;
                _lblEmptyMaster.Visible = !has;
                _lblEmptyMaster.Text = _data.Count == 0 ? "Không phát hiện tên trùng." : "Không tìm thấy kết quả phù hợp.";
                _gridMaster.RowCount = 0;
                _gridMaster.RowCount = _viewData.Count;
                if (has)
                {
                    int idx = truoc != null ? _viewData.IndexOf(truoc) : -1;
                    if (idx < 0 && _tenCanChonLai != null)
                        idx = _viewData.FindIndex(x => string.Equals(x.HoTen, _tenCanChonLai, StringComparison.OrdinalIgnoreCase));
                    if (idx < 0) idx = 0;
                    _gridMaster.CurrentCell = _gridMaster.Rows[idx].Cells[0];
                    try { _gridMaster.FirstDisplayedScrollingRowIndex = Math.Max(0, idx - 3); } catch { }
                }
            }
            finally { _boQuaSelection = false; }
            _lblMasterCount.Text = _viewData.Count == _data.Count ? _data.Count.ToString("N0") : $"{_viewData.Count:N0}/{_data.Count:N0}";
            CapNhatStatus();
            if (_viewData.Count == 0) XoaChiTiet();
            else { _selectTimer.Stop(); _selectTimer.Start(); }
        }
        private void GridMaster_SelectionChanged(object? sender, EventArgs e)
        {
            if (_boQuaSelection) return;
            // Debounce: giữ phím mũi tên không bắn hàng chục truy vấn liên tiếp
            _selectTimer.Stop();
            _selectTimer.Start();
        }
        private TrungTenModel? LayModelDangChon()
        {
            int r = _gridMaster.CurrentCell?.RowIndex ?? -1;
            return r >= 0 && r < _viewData.Count ? _viewData[r] : null;
        }
        private async void TaiChiTietAsync()
        {
            var model = LayModelDangChon();
            if (model == null) { XoaChiTiet(); return; }
            _detailCts?.Cancel();
            if (ReferenceEquals(model, _modelDangHienThi)) return;
            _detailCts?.Dispose();
            var cts = _detailCts = new CancellationTokenSource();
            var token = cts.Token;
            if (_detailCache.TryGetValue(model.HoTen, out var cached))
            {
                HienThiChiTiet(model, cached);
                return;
            }
            BatProgress();
            SetStatus($"Đang tải chi tiết: {model.HoTen}...");
            try
            {
                var rows = await Task.Run(() => TruyVanChiTietSQLite(model.RowIds, token), token);
                if (IsDisposed || token.IsCancellationRequested) return;
                if (_detailCache.Count >= MaxDetailCache) _detailCache.Clear();
                _detailCache[model.HoTen] = rows;
                HienThiChiTiet(model, rows);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TaiChiTiet Lỗi]: {ex.Message}");
                if (!IsDisposed) SetStatus("Lỗi khi tải chi tiết hồ sơ.");
            }
            finally
            {
                TatProgress();
                if (!IsDisposed && !token.IsCancellationRequested) CapNhatStatus();
            }
        }
        private void HienThiChiTiet(TrungTenModel model, List<TrungTenDetailModel> rows)
        {
            _modelDangHienThi = model;
            _viewDataDetail = rows;
            _donViDuocChon = "";
            int soDonVi = XayDungChips(rows);
            _lblDetailTitle.Text = model.HoTen;
            _lblDetailSub.Text = $"{rows.Count} hồ sơ trùng tên • {soDonVi} đơn vị";
            _gridDetail.RowCount = 0;
            _gridDetail.RowCount = rows.Count;
            bool has = rows.Count > 0;
            _gridDetail.Visible = has;
            _lblEmptyDetail.Visible = !has;
            _lblEmptyDetail.Text = "Không có hồ sơ để hiển thị.";
            if (has) { _gridDetail.ClearSelection(); _gridDetail.CurrentCell = null; }
            _gridDetail.Invalidate();
            CapNhatStatus();
        }
        private void XoaChiTiet()
        {
            _detailCts?.Cancel();
            _modelDangHienThi = null;
            _viewDataDetail = new List<TrungTenDetailModel>();
            _donViDuocChon = "";
            XayDungChips(_viewDataDetail);
            _lblDetailTitle.Text = "Chi tiết hồ sơ";
            _lblDetailSub.Text = "";
            _gridDetail.RowCount = 0;
            _gridDetail.Visible = false;
            _lblEmptyDetail.Visible = true;
            _lblEmptyDetail.Text = _data.Count == 0
                ? "Không có dữ liệu để hiển thị."
                : "Chọn một họ tên ở danh sách bên trái để xem chi tiết hồ sơ.";
        }
        private int XayDungChips(List<TrungTenDetailModel> rows)
        {
            _chipPanel.SuspendLayout();
            foreach (var c in _chipPanel.Controls.Cast<Control>().ToArray())
            {
                _chipPanel.Controls.Remove(c);
                c.Dispose();
            }
            var groups = rows.GroupBy(r => r.DonVi, StringComparer.OrdinalIgnoreCase)
                             .Select(g => (Key: g.Key, Count: g.Count()))
                             .OrderByDescending(g => g.Count)
                             .ThenBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase)
                             .ToList();
            foreach (var (key, count) in groups)
            {
                var chip = new ChipButton
                {
                    Font = _chipFont,
                    Text = $"{(key.Length == 0 ? TenChuaCoDonVi : key)}  ·  {count}",
                    Tag = key,
                    Margin = new Padding(0, 0, 8, 8)
                };
                chip.Click += Chip_Click;
                _chipPanel.Controls.Add(chip);
            }
            _chipPanel.ResumeLayout(true);
            bool has = groups.Count > 0;
            _chipPanel.Visible = has;
            _lblChipHint.Visible = has;
            _tlpDetail.RowStyles[2].Height = has ? 24F : 0F;
            return groups.Count;
        }
        private void Chip_Click(object? sender, EventArgs e)
        {
            if (sender is not ChipButton chip) return;
            string dv = chip.Tag as string ?? "";
            if (dv.Length == 0) return;
            bool boChon = string.Equals(_donViDuocChon, dv, StringComparison.OrdinalIgnoreCase);
            _donViDuocChon = boChon ? "" : dv;
            foreach (var c in _chipPanel.Controls.OfType<ChipButton>())
                c.Checked = !boChon && string.Equals(c.Tag as string, dv, StringComparison.OrdinalIgnoreCase);
            _gridDetail.Invalidate();
        }
        private void GridMaster_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _viewData.Count) return;
            var item = _viewData[e.RowIndex];
            e.Value = e.ColumnIndex == 0 ? item.HoTen : item.SoLan;
        }
        private void GridMaster_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex != 1 || e.RowIndex < 0 || e.RowIndex >= _viewData.Count) return;
            int n = _viewData[e.RowIndex].SoLan;
            Color c = n >= 5 ? UiColors.Danger : n >= 3 ? UiColors.Warn : UiColors.GreenDark;
            e.CellStyle!.ForeColor = c;
            e.CellStyle.SelectionForeColor = c;
            e.CellStyle.Font = _boldFont;
        }
        private void GridDetail_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _viewDataDetail.Count) return;
            var item = _viewDataDetail[e.RowIndex];
            switch (e.ColumnIndex)
            {
                case 0: e.Value = e.RowIndex + 1; break;
                case 1: e.Value = item.HoTen; break;
                case 2: e.Value = item.NamSinh; break;
                case 3: e.Value = item.CapBac; break;
                case 4: e.Value = item.DonVi; break;
                case 5: e.Value = item.SoHieu; break;
                case 6: e.Value = item.GhiChu; break;
            }
        }
        // Dùng CellFormatting (style tạm cho từng ô) thay vì sửa DefaultCellStyle của từng dòng trong RowPrePaint
        private void GridDetail_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_donViDuocChon.Length == 0 || e.RowIndex < 0 || e.RowIndex >= _viewDataDetail.Count) return;
            if (!string.Equals(_viewDataDetail[e.RowIndex].DonVi, _donViDuocChon, StringComparison.OrdinalIgnoreCase)) return;
            e.CellStyle!.BackColor = UiColors.Highlight;
            e.CellStyle.SelectionBackColor = UiColors.HighlightSel;
            e.CellStyle.Font = _boldFont;
        }
        #endregion
        #region Tìm kiếm, làm mới, đóng
        private void TxtSearch_TextChanged(object? sender, EventArgs e)
        {
            _btnXoaTimKiem.Visible = _txtSearch.TextLength > 0;
            _searchTimer.Stop();
            _searchTimer.Start(); // tìm khi người dùng ngừng gõ 250ms
        }
        private void TimKiem()
        {
            _searchTimer.Stop();
            ApDungBoLoc();
        }
        private void kryptonButton_TimKiem_Click(object? sender, EventArgs e) => TimKiem();
        private async void kryptonButton_LamMoi_Click(object? sender, EventArgs e)
        {
            _txtSearch.Clear();
            _searchTimer.Stop();
            await TaiDuLieuHeThongAsync();
        }
        private void kryptonButton_Dong_Click(object? sender, EventArgs e)
        {
            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            formCha?.CapNhatTieuDe("Trang phân loại thi đua");
            Close();
        }
        private void textBoxKyToon_TimTen_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                TimKiem();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                _txtSearch.Clear();
                TimKiem();
            }
            else if (e.KeyCode == Keys.Down && _gridMaster.Visible)
            {
                e.SuppressKeyPress = true;
                _gridMaster.Focus();
            }
        }
        private void lamMoi_ToolStripMenuItem_Click(object? sender, EventArgs e) => _btnLamMoi.PerformClick();
        private void quayLaiTrangXuLyDuLieu_ToolStripMenuItem_Click(object? sender, EventArgs e) => _btnDong.PerformClick();
        private void xoaTimKiem_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            _txtSearch.Clear();
            _txtSearch.Focus();
            TimKiem();
        }
        #endregion
        #region Phím tắt, trạng thái, tiện ích UI
        private Action SafeAction(Action action)
        {
            return () =>
            {
                if (action == null || Interlocked.CompareExchange(ref _isLoading, 0, 0) == 1) return;
                try { action.Invoke(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Lỗi thực thi phím tắt: " + ex.Message); }
            };
        }
        private bool SafeExecute(Action action)
        {
            SafeAction(action).Invoke();
            return true;
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (Interlocked.CompareExchange(ref _isLoading, 0, 0) == 1) return base.ProcessCmdKey(ref msg, keyData);
            try
            {
                if (Module_PhimTat.XuLy(keyData, actionLamMoi: SafeAction(() => lamMoi_ToolStripMenuItem_Click(null, EventArgs.Empty))))
                    return true;
                Keys key = keyData & Keys.KeyCode;
                Keys modifier = keyData & Keys.Modifiers;
                if (modifier == Keys.None && key == Keys.F4)
                    return SafeExecute(() => xoaTimKiem_ToolStripMenuItem_Click(null, EventArgs.Empty));
                if (modifier == Keys.Control && key == Keys.Q)
                    return SafeExecute(() => quayLaiTrangXuLyDuLieu_ToolStripMenuItem_Click(null, EventArgs.Empty));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi phím tắt: " + ex.Message);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        private void SetStatus(string text)
        {
            if (!IsDisposed) _lblStatus.Text = text;
        }
        private void BatProgress()
        {
            if (Interlocked.Increment(ref _busyCount) == 1 && !IsDisposed) _progress.Visible = true;
        }
        private void TatProgress()
        {
            if (Interlocked.Decrement(ref _busyCount) <= 0)
            {
                Interlocked.Exchange(ref _busyCount, 0);
                if (!IsDisposed) _progress.Visible = false;
            }
        }
        private void CapNhatKpi()
        {
            long tongHoSo = 0;
            foreach (var x in _data) tongHoSo += x.SoLan;
            _kpiTen.Text = _data.Count.ToString("N0");
            _kpiHoSo.Text = tongHoSo.ToString("N0");
            _kpiMax.Text = _data.Count == 0 ? "—" : $"{_data[0].HoTen} ({_data[0].SoLan})";
        }
        private void CapNhatStatus()
        {
            int total = _data.Count;
            int filtered = _viewData.Count;
            string loi = _soBanGhiLoi > 0 ? $"  •  {_soBanGhiLoi} bản ghi không giải mã được" : "";
            if (total == 0) SetStatus("Không phát hiện tên trùng." + loi);
            else if (filtered == total) SetStatus($"Số CBCS có tên trùng nhau: {total} {Module_HeThong.Tu_dong_chi}{loi}");
            else SetStatus($"Kết quả tìm kiếm: {filtered}/{total} CBCS trùng tên{loi}");
        }
        private void InitToolTips()
        {
            if (_daKhoiTaoToolTip) return;
            try
            {
                _toolTip = new ToolTip(components)
                {
                    IsBalloon = true,
                    ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac,
                    ToolTipIcon = ToolTipIcon.Info,
                    InitialDelay = 300,
                    AutoPopDelay = 2500,
                    ReshowDelay = 100,
                    ShowAlways = true
                };
                _toolTip.SetToolTip(_btnTimKiem, "Bấm nút để tìm kiếm (Enter)");
                _toolTip.SetToolTip(_btnLamMoi, "Làm mới dữ liệu (F5)");
                _toolTip.SetToolTip(_btnDong, "Thoát trang (Ctrl+Q)");
                _toolTip.SetToolTip(_btnXoaTimKiem, "Xóa nội dung tìm kiếm (F4)");
                _daKhoiTaoToolTip = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InitToolTips]: {ex.Message}");
            }
        }
        private void XacDinhPhienBan()
        {
            _lblVersion.Text = _laTanBinh ? "Phiên bản phần mềm dành cho tân binh" : "Phiên bản phần mềm dành cho CBCS";
        }
        private void SetupPlaceholder()
        {
            if (_txtSearch != null && !_txtSearch.IsDisposed)
                SendMessage(_txtSearch.Handle, EM_SETCUEBANNER, 1, _laTanBinh ? "Nhập họ và tên tân binh để tìm kiếm (không cần gõ dấu)" : "Nhập họ và tên CBCS để tìm kiếm (không cần gõ dấu)");
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _searchTimer.Stop();
            _selectTimer.Stop();
            _loadCts?.Cancel();
            _detailCts?.Cancel();
            base.OnFormClosing(e);
        }
        #endregion
        #region Control tự vẽ (nút, thẻ, chip) & bảng màu
        private static class UiColors
        {
            public static readonly Color Bg = Color.FromArgb(244, 246, 248);
            public static readonly Color Border = Color.FromArgb(229, 231, 235);
            public static readonly Color GridLine = Color.FromArgb(241, 243, 245);
            public static readonly Color HeaderBack = Color.FromArgb(249, 250, 251);
            public static readonly Color ZebraRow = Color.FromArgb(251, 252, 253);
            public static readonly Color Hover = Color.FromArgb(243, 244, 246);
            public static readonly Color Text = Color.FromArgb(17, 24, 39);
            public static readonly Color Muted = Color.FromArgb(107, 114, 128);
            public static readonly Color Green = Color.FromArgb(22, 163, 74);
            public static readonly Color GreenHover = Color.FromArgb(21, 128, 61);
            public static readonly Color GreenPressed = Color.FromArgb(20, 83, 45);
            public static readonly Color GreenDark = Color.FromArgb(21, 128, 61);
            public static readonly Color GreenSoft = Color.FromArgb(220, 252, 231);
            public static readonly Color Highlight = Color.FromArgb(254, 249, 195);
            public static readonly Color HighlightSel = Color.FromArgb(253, 230, 138);
            public static readonly Color Danger = Color.FromArgb(185, 28, 28);
            public static readonly Color Warn = Color.FromArgb(180, 83, 9);
            public static readonly Color Purple = Color.FromArgb(237, 233, 254);      // Màu tím nhạt nền
            public static readonly Color PurpleHover = Color.FromArgb(221, 214, 254); // Màu tím nhạt khi rê chuột
            public static readonly Color PurplePressed = Color.FromArgb(196, 181, 253);// Màu tím nhạt khi nhấn
            public static readonly Color PurpleDark = Color.FromArgb(109, 40, 217);   // Màu chữ/icon tím đậm cho tương phản
        }
        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = Math.Max(1, radius * 2);
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
        private sealed class RoundedPanel : Panel
        {
            private Color _fill = Color.White;
            public int Radius { get; set; } = 12;
            public Color BorderColor { get; set; } = UiColors.Border;
            public Color FillColor
            {
                get => _fill;
                set { _fill = value; BackColor = value; Invalidate(); }
            }
            public RoundedPanel()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
                BackColor = Color.White;
            }
            protected override void OnPaintBackground(PaintEventArgs e) =>
                e.Graphics.Clear(Parent?.BackColor ?? SystemColors.Control);
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Radius);
                using var b = new SolidBrush(_fill);
                using var p = new Pen(BorderColor);
                g.FillPath(b, path);
                g.DrawPath(p, path);
            }
        }
        private sealed class ModernButton : Button
        {
            private static readonly Font IconFont = new("Segoe MDL2 Assets", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
            private bool _hover, _down;
            public string Glyph { get; set; } = "";
            public bool Primary { get; set; }
            public bool IsPurple { get; set; } // Thêm thuộc tính nhận diện nút màu tím
            public int Radius { get; set; } = 8;
            public ModernButton()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
                FlatStyle = FlatStyle.Flat;
                FlatAppearance.BorderSize = 0;
                UseVisualStyleBackColor = false;
                Cursor = Cursors.Hand;
            }
            protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
            protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(Parent?.BackColor ?? SystemColors.Control);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color fill, border, fore;
                if (Primary)
                {
                    fill = _down ? UiColors.GreenPressed : _hover ? UiColors.GreenHover : UiColors.Green;
                    border = fill;
                    fore = Color.White;
                }
                else if (IsPurple) // Xử lý riêng cho nút màu tím nhạt
                {
                    fill = _down ? UiColors.PurplePressed : _hover ? UiColors.PurpleHover : UiColors.Purple;
                    border = Color.FromArgb(196, 181, 253);
                    fore = UiColors.PurpleDark;
                }
                else
                {
                    fill = _down ? Color.FromArgb(229, 231, 235) : _hover ? UiColors.Hover : Color.White;
                    border = Color.FromArgb(209, 213, 219);
                    fore = UiColors.Text;
                }
                if (!Enabled) { fill = Color.FromArgb(243, 244, 246); fore = Color.FromArgb(156, 163, 175); }
                using (var path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Radius))
                using (var b = new SolidBrush(fill))
                using (var p = new Pen(border))
                {
                    g.FillPath(b, path);
                    g.DrawPath(p, path);
                }
                const TextFormatFlags fl = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.Left;
                var huge = new Size(int.MaxValue, int.MaxValue);
                Size gs = string.IsNullOrEmpty(Glyph) ? Size.Empty : TextRenderer.MeasureText(g, Glyph, IconFont, huge, fl);
                Size ts = string.IsNullOrEmpty(Text) ? Size.Empty : TextRenderer.MeasureText(g, Text, Font, huge, fl);
                int gap = gs.Width > 0 && ts.Width > 0 ? 8 : 0;
                int x = (Width - (gs.Width + gap + ts.Width)) / 2;
                if (gs.Width > 0)
                {
                    TextRenderer.DrawText(g, Glyph, IconFont, new Rectangle(x, 0, gs.Width + 2, Height), fore, fl);
                    x += gs.Width + gap;
                }
                if (ts.Width > 0)
                    TextRenderer.DrawText(g, Text, Font, new Rectangle(x, 0, ts.Width + 2, Height), fore, fl);
            }
        }
        private sealed class ChipButton : Control
        {
            private bool _hover, _checked;
            public bool Checked
            {
                get => _checked;
                set { if (_checked != value) { _checked = value; Invalidate(); } }
            }
            public ChipButton()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
                SetStyle(ControlStyles.Selectable, false);
                TabStop = false;
                Cursor = Cursors.Hand;
                AutoSize = true;
                Height = 30;
            }
            public override Size GetPreferredSize(Size proposedSize)
            {
                var ts = TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                return new Size(Math.Min(ts.Width + 28, 380), 30);
            }
            protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
            protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnPaintBackground(PaintEventArgs e) =>
                e.Graphics.Clear(Parent?.BackColor ?? SystemColors.Control);
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color fill = _checked ? UiColors.HighlightSel : _hover ? Color.FromArgb(229, 231, 235) : UiColors.Hover;
                Color border = _checked ? Color.FromArgb(234, 179, 8) : UiColors.Border;
                Color fore = _checked ? Color.FromArgb(113, 63, 18) : UiColors.Text;
                using (var path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Height / 2))
                using (var b = new SolidBrush(fill))
                using (var p = new Pen(border))
                {
                    g.FillPath(b, path);
                    g.DrawPath(p, path);
                }
                TextRenderer.DrawText(g, Text, Font, new Rectangle(12, 0, Width - 24, Height), fore,
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            }
        }
        #endregion
    }
    public class TrungTenModel
    {
        public string HoTen { get; set; } = "";
        public int SoLan { get; set; }
        public string DonVi { get; set; } = "";
        public string KeyTimKiem { get; set; } = "";
        public List<long> RowIds { get; set; } = new();
    }
    public class TrungTenDetailModel
    {
        public string HoTen { get; set; } = "";
        public string NamSinh { get; set; } = "";
        public string CapBac { get; set; } = "";
        public string DonVi { get; set; } = "";
        public string SoHieu { get; set; } = "";
        public string GhiChu { get; set; } = "";
    }
}
/////TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
//using Microsoft.Data.Sqlite;
//using System.Data;
//using System.Globalization;
//using System.Runtime.InteropServices;
//using System.Text;
//namespace PhanMemThiDua2026
//{
//    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
//    public partial class Form28_DataTrungTen : Form
//    {
//        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
//        private readonly List<TrungTenModel> _data = new();
//        private List<TrungTenModel> _viewData = new();
//        private readonly ToolStripStatusLabel _rightSpacer = new();
//        private const int EM_SETCUEBANNER = 0x1501;
//        private const int WM_SETREDRAW = 11;
//        // ⭐ CHUẨN KỸ SƯ: Khởi tạo luồng an toàn bằng Interlocked
//        private int _isProcessing = 0;
//        private CancellationTokenSource _cts;
//        private Font _headerFont;
//        private Font _boldFont;
//        private string _donViDuocChon = "";
//        private volatile bool _isTreeSelecting;
//        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
//        private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, string lParam);
//        private List<TrungTenDetailModel> _viewDataDetail = new();
//        private Krypton.Toolkit.KryptonTreeView kryptonTreeView_DieuHuong;
//        private Krypton.Toolkit.KryptonSplitContainer splitContainer;
//        private static readonly char[] _splitChars = new[] { ';' };
//        public Form28_DataTrungTen()
//        {
//            InitializeComponent();
//            KhoiTaoGiaoDienSplit();
//            SetupPlaceholder();
//            kryptonButton_TimKiem.DialogResult = DialogResult.None;
//            FormBorderStyle = FormBorderStyle.FixedDialog;
//            StartPosition = FormStartPosition.CenterScreen;
//            MaximizeBox = false;
//            AcceptButton = kryptonButton_TimKiem;
//            Load += Form28_DataTrungTen_Load;
//            KhoiTaoDataGridView();
//            InitToolTips();
//            SetupStatusStrip();
//        }
//        private async void Form28_DataTrungTen_Load(object? sender, EventArgs e)
//        {
//            kryptonDataGridView_TrungTen.ContextMenuStrip = contextMenuStrip1;
//            Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1);
//            XacDinhPhienBan();
//            if (splitContainer != null)
//            {
//                splitContainer.SplitterDistance = 280;
//            }
//            await TaiDuLieuHeThongAsync();
//        }
//        /// <summary>
//        /// ⭐ CHUẨN KỸ SƯ: Đóng gói luồng nạp dữ liệu bất đồng bộ, tự động tái tạo Token phòng thủ rò rỉ
//        /// </summary>
//        private async Task TaiDuLieuHeThongAsync()
//        {
//            if (Interlocked.Exchange(ref _isProcessing, 1) == 1) return;
//            toolStripStatusLabel1.Text = "Đang phân tích dữ liệu...";
//            BatProgress();
//            // Tái tạo CancellationTokenSource sạch cho phiên làm việc mới
//            _cts?.Cancel();
//            _cts?.Dispose();
//            _cts = new CancellationTokenSource();
//            var token = _cts.Token;
//            try
//            {
//                var result = await Task.Run(() => PhanTichDuLieu(token), token);
//                if (IsDisposed || token.IsCancellationRequested) return;
//                _data.Clear();
//                if (result != null) _data.AddRange(result);
//                _viewData = _data;
//                HienThiTreeView();
//                _viewDataDetail.Clear();
//                HienThiGrid();
//                CapNhatStatus();
//                textBoxKyToon_TimTen.Focus();
//            }
//            catch (OperationCanceledException) { }
//            catch (Exception ex)
//            {
//                toolStripStatusLabel1.Text = "Lỗi khi phân tích dữ liệu.";
//                System.Diagnostics.Debug.WriteLine($"[Form28 Load Lỗi]: {ex}");
//                MessageBox.Show("Có lỗi nghiêm trọng xảy ra trong quá trình kết nối và phân tích dữ liệu CSDL.", "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
//            }
//            finally
//            {
//                TatProgress();
//                Interlocked.Exchange(ref _isProcessing, 0);
//            }
//        }
//        //region KHOI TAO GIAO DIEN & LOAD DU LIEU
//        private void KhoiTaoGiaoDienSplit()
//        {
//            splitContainer = new Krypton.Toolkit.KryptonSplitContainer
//            {
//                Dock = DockStyle.Fill,
//                Cursor = Cursors.Default,
//                FixedPanel = FixedPanel.Panel1,
//                Panel1MinSize = 250
//            };
//            kryptonTreeView_DieuHuong = new Krypton.Toolkit.KryptonTreeView
//            {
//                Dock = DockStyle.Fill,
//                ItemHeight = 30,
//                ShowLines = true,
//                ShowPlusMinus = true
//            };
//            kryptonTreeView_DieuHuong.BeforeExpand += KryptonTreeView_DieuHuong_BeforeExpand;
//            kryptonTreeView_DieuHuong.AfterSelect += KryptonTreeView_DieuHuong_AfterSelect;
//            splitContainer.Panel1.Controls.Add(kryptonTreeView_DieuHuong);
//            tableLayoutPanel2.Controls.Remove(kryptonDataGridView_TrungTen);
//            splitContainer.Panel2.Controls.Add(kryptonDataGridView_TrungTen);
//            tableLayoutPanel2.Controls.Add(splitContainer, 0, 1);
//        }
//        private void KhoiTaoDataGridView()
//        {
//            string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
//            bool laTanBinh = phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase);
//            var grid = kryptonDataGridView_TrungTen;
//            grid.SuspendLayout();
//            grid.Columns.Clear();
//            grid.Dock = DockStyle.Fill;
//            grid.RowHeadersVisible = false;
//            grid.AllowUserToAddRows = false;
//            grid.AllowUserToDeleteRows = false;
//            grid.AllowUserToResizeRows = false;
//            grid.ReadOnly = true;
//            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
//            grid.EnableHeadersVisualStyles = false;
//            // Kích hoạt VirtualMode chuẩn kỹ sư
//            grid.VirtualMode = true;
//            // ⭐ LŨY ĐẲNG EVENT: Hủy liên kết cũ trước khi gán mới để tránh nhân bản bộ vẽ nền gây lag
//            grid.CellValueNeeded -= KryptonDataGridView_TrungTen_Detail_CellValueNeeded;
//            grid.CellValueNeeded += KryptonDataGridView_TrungTen_Detail_CellValueNeeded;
//            grid.RowPrePaint -= KryptonDataGridView_TrungTen_Detail_RowPrePaint;
//            grid.RowPrePaint += KryptonDataGridView_TrungTen_Detail_RowPrePaint;
//            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
//            grid.ColumnHeadersHeight = 45;
//            _headerFont?.Dispose();
//            _headerFont = new Font(grid.Font, FontStyle.Bold);
//            grid.ColumnHeadersDefaultCellStyle.Font = _headerFont;
//            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
//            _boldFont?.Dispose();
//            _boldFont = new Font(grid.Font, FontStyle.Bold);
//            // PHỐI MÀU UX CHUẨN LỰC LƯỢNG
//            grid.RowsDefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
//            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(225, 250, 225);
//            grid.RowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(144, 238, 144);
//            grid.RowsDefaultCellStyle.SelectionForeColor = Color.Black;
//            grid.GridColor = Color.LightGray;
//            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
//            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
//            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "STT", HeaderText = "STT", FillWeight = 6, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
//            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "HoTen", HeaderText = laTanBinh ? "Họ và tên tân binh" : "Họ và tên CBCS", FillWeight = 26 });
//            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "NamSinh", HeaderText = "Năm sinh", FillWeight = 12, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
//            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CapBac", HeaderText = "Cấp bậc", FillWeight = 14, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
//            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DonVi", HeaderText = "Đơn vị", FillWeight = 30 });
//            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoHieu", HeaderText = "Số hiệu", FillWeight = 12, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
//            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GhiChu", HeaderText = "Ghi chú", FillWeight = 20 });
//            // ⭐ TRIỆT TIÊU ĐỘ TRỄ RENDERING: Khóa cứng layout dòng, vô hiệu hóa tự động co giãn ô text dài
//            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
//            grid.RowTemplate.Height = 35;
//            grid.ResumeLayout();
//        }
//        private void KryptonDataGridView_TrungTen_Detail_RowPrePaint(object? sender, DataGridViewRowPrePaintEventArgs e)
//        {
//            if (_viewDataDetail == null || e.RowIndex < 0 || e.RowIndex >= _viewDataDetail.Count) return;
//            var grid = kryptonDataGridView_TrungTen;
//            if (grid == null || grid.IsDisposed) return;
//            try
//            {
//                var rowStyle = grid.Rows[e.RowIndex].DefaultCellStyle;
//                string donViCuaDong = _viewDataDetail[e.RowIndex].DonVi;
//                bool laDonViDuocChon = !string.IsNullOrEmpty(_donViDuocChon) &&
//                                       string.Equals(donViCuaDong, _donViDuocChon, StringComparison.OrdinalIgnoreCase);
//                if (laDonViDuocChon)
//                {
//                    rowStyle.BackColor = Color.FromArgb(152, 251, 152); // Xanh lá cây nhạt
//                    rowStyle.Font = _boldFont;
//                }
//                else
//                {
//                    // Trả lại màu đồng bộ chuẩn cho các hàng không được nhấp chọn
//                    rowStyle.BackColor = (e.RowIndex % 2 == 0)
//                        ? Color.FromArgb(240, 255, 240)
//                        : Color.FromArgb(225, 250, 225);
//                    rowStyle.Font = grid.Font;
//                }
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine($"[Lỗi vẽ dòng RowPrePaint]: {ex.Message}");
//            }
//        }
//        private void KryptonDataGridView_TrungTen_Detail_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
//        {
//            if (_viewDataDetail == null || e.RowIndex < 0 || e.RowIndex >= _viewDataDetail.Count) return;
//            var item = _viewDataDetail[e.RowIndex];
//            switch (e.ColumnIndex)
//            {
//                case 0: e.Value = e.RowIndex + 1; break;
//                case 1: e.Value = item.HoTen; break;
//                case 2: e.Value = item.NamSinh; break;
//                case 3: e.Value = item.CapBac; break;
//                case 4: e.Value = item.DonVi; break;
//                case 5: e.Value = item.SoHieu; break;
//                case 6: e.Value = item.GhiChu; break;
//            }
//        }
//        //endregion
//        //region XU LY DU LIEU TỐI ƯU & TREEVIEW
//        private List<TrungTenModel> PhanTichDuLieu(CancellationToken token)
//        {
//            var dict = new Dictionary<string, Dictionary<string, List<long>>>(10000, StringComparer.OrdinalIgnoreCase);
//            var aesCacheTen = new Dictionary<string, string>(10000);
//            var aesCacheDonVi = new Dictionary<string, string>(2000);
//            var textInfo = CultureInfo.CurrentCulture.TextInfo;
//            var builder = new SqliteConnectionStringBuilder
//            {
//                DataSource = _csdl2Path,
//                Mode = SqliteOpenMode.ReadOnly,
//                DefaultTimeout = 5,
//                Pooling = true,
//                Cache = SqliteCacheMode.Private
//            };
//            using var conn = new SqliteConnection(builder.ConnectionString);
//            conn.Open();
//            using (var pragmaCmd = conn.CreateCommand())
//            {
//                pragmaCmd.CommandText = "PRAGMA journal_mode = WAL; PRAGMA synchronous = NORMAL; PRAGMA temp_store = MEMORY;";
//                pragmaCmd.ExecuteNonQuery();
//            }
//            using var cmd = new SqliteCommand("SELECT rowid, HoVaTen, DonVi FROM DanhSach", conn);
//            using var reader = cmd.ExecuteReader();
//            int recordCount = 0;
//            while (reader.Read())
//            {
//                token.ThrowIfCancellationRequested();
//                if (++recordCount > 500000) throw new Exception("Vượt quá giới hạn bộ nhớ an toàn của phân hệ.");
//                if (reader.IsDBNull(1)) continue;
//                long rowId = reader.GetInt64(0);
//                string maHoaTen = reader.GetString(1);
//                if (string.IsNullOrWhiteSpace(maHoaTen)) continue;
//                // ⭐ ZERO-ALLOCATION PATTERN: Sử dụng TryGetValue loại bỏ hoàn toàn khối try-catch thừa thãi
//                if (!aesCacheTen.TryGetValue(maHoaTen, out string hoTen))
//                {
//                    hoTen = Module_BaoMatAES.GiaiMa(maHoaTen)?.Trim() ?? "";
//                    if (string.IsNullOrEmpty(hoTen)) continue;
//                    hoTen = textInfo.ToTitleCase(hoTen.ToLower());
//                    aesCacheTen[maHoaTen] = hoTen;
//                }
//                string donVi = "";
//                if (!reader.IsDBNull(2))
//                {
//                    string maHoaDonVi = reader.GetString(2);
//                    if (!string.IsNullOrEmpty(maHoaDonVi) && !aesCacheDonVi.TryGetValue(maHoaDonVi, out donVi))
//                    {
//                        donVi = Module_BaoMatAES.GiaiMa(maHoaDonVi)?.Trim() ?? "";
//                        donVi = donVi.ToUpperInvariant();
//                        aesCacheDonVi[maHoaDonVi] = donVi;
//                    }
//                }
//                if (!dict.TryGetValue(hoTen, out var dvDict))
//                {
//                    dvDict = new Dictionary<string, List<long>>(2, StringComparer.OrdinalIgnoreCase);
//                    dict[hoTen] = dvDict;
//                }
//                if (!dvDict.TryGetValue(donVi, out var listRowIds))
//                {
//                    listRowIds = new List<long>(4);
//                    dvDict[donVi] = listRowIds;
//                }
//                listRowIds.Add(rowId);
//            }
//            var result = new List<TrungTenModel>(dict.Count);
//            var sb = new StringBuilder(128);
//            foreach (var item in dict)
//            {
//                token.ThrowIfCancellationRequested();
//                int tong = 0;
//                var allRowIds = new List<long>();
//                foreach (var kvp in item.Value)
//                {
//                    tong += kvp.Value.Count;
//                    allRowIds.AddRange(kvp.Value);
//                }
//                if (tong <= 1) continue;
//                sb.Clear();
//                foreach (var d in item.Value)
//                {
//                    if (sb.Length > 0) sb.Append("; ");
//                    sb.Append(d.Key).Append(" (").Append(d.Value.Count).Append(')');
//                }
//                result.Add(new TrungTenModel
//                {
//                    HoTen = item.Key,
//                    SoLan = tong,
//                    DonVi = sb.ToString(),
//                    RowIds = allRowIds
//                });
//            }
//            result.Sort((a, b) => b.SoLan.CompareTo(a.SoLan));
//            return result;
//        }
//        private List<TrungTenDetailModel> TruyVanChiTietSQLite(List<long> targetRowIds, CancellationToken token)
//        {
//            var result = new List<TrungTenDetailModel>(targetRowIds.Count);
//            if (targetRowIds == null || targetRowIds.Count == 0) return result;
//            var builder = new SqliteConnectionStringBuilder
//            {
//                DataSource = _csdl2Path,
//                Mode = SqliteOpenMode.ReadOnly,
//                Pooling = true,
//                Cache = SqliteCacheMode.Private
//            };
//            using var conn = new SqliteConnection(builder.ConnectionString);
//            conn.Open();
//            using (var pragmaCmd = conn.CreateCommand())
//            {
//                pragmaCmd.CommandText = "PRAGMA journal_mode = WAL; PRAGMA temp_store = MEMORY;";
//                pragmaCmd.ExecuteNonQuery();
//            }
//            // ⭐ CHUẨN AN TOÀN: Gom cụm định danh ROWID, ngăn chặn lỗi biên dịch SQLite Command vượt ngưỡng
//            string inClause = string.Join(",", targetRowIds);
//            string query = $"SELECT HoVaTen, NamSinh, CapBac, DonVi, SoHieu, GhiChu FROM DanhSach WHERE rowid IN ({inClause})";
//            using var cmd = new SqliteCommand(query, conn);
//            using var reader = cmd.ExecuteReader();
//            while (reader.Read())
//            {
//                token.ThrowIfCancellationRequested();
//                string hoTenGiaiMa = "";
//                if (!reader.IsDBNull(0))
//                {
//                    hoTenGiaiMa = Module_BaoMatAES.GiaiMa(reader.GetString(0))?.Trim() ?? "";
//                    hoTenGiaiMa = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(hoTenGiaiMa.ToLower());
//                }
//                result.Add(new TrungTenDetailModel
//                {
//                    HoTen = hoTenGiaiMa,
//                    NamSinh = !reader.IsDBNull(1) ? (Module_BaoMatAES.GiaiMa(reader.GetString(1)) ?? "") : "",
//                    CapBac = !reader.IsDBNull(2) ? (Module_BaoMatAES.GiaiMa(reader.GetString(2)) ?? "") : "",
//                    DonVi = !reader.IsDBNull(3) ? (Module_BaoMatAES.GiaiMa(reader.GetString(3))?.ToUpperInvariant() ?? "") : "",
//                    SoHieu = !reader.IsDBNull(4) ? (Module_BaoMatAES.GiaiMa(reader.GetString(4)) ?? "") : "",
//                    GhiChu = !reader.IsDBNull(5) ? (Module_BaoMatAES.GiaiMa(reader.GetString(5)) ?? "") : ""
//                });
//            }
//            return result;
//        }
//        private void HienThiTreeView()
//        {
//            if (kryptonTreeView_DieuHuong == null || kryptonTreeView_DieuHuong.IsDisposed) return;
//            // ⭐ KHÓA ĐÓNG BĂNG ĐỒ HỌA OS: Chặn đứng tình trạng chớp, giật khựng khung hình của bộ Krypton UI
//            SendMessage(kryptonTreeView_DieuHuong.Handle, WM_SETREDRAW, 0, "");
//            kryptonTreeView_DieuHuong.BeginUpdate();
//            kryptonTreeView_DieuHuong.Nodes.Clear();
//            var nodesArray = new TreeNode[_viewData.Count];
//            for (int i = 0; i < _viewData.Count; i++)
//            {
//                var item = _viewData[i];
//                TreeNode rootNode = new TreeNode($"{item.HoTen} ({item.SoLan})") { Tag = item };
//                rootNode.Nodes.Add(new TreeNode("Đang tải..."));
//                nodesArray[i] = rootNode;
//            }
//            kryptonTreeView_DieuHuong.Nodes.AddRange(nodesArray);
//            kryptonTreeView_DieuHuong.EndUpdate();
//            SendMessage(kryptonTreeView_DieuHuong.Handle, WM_SETREDRAW, 1, "");
//            kryptonTreeView_DieuHuong.Refresh();
//            SafeSelectFirstTreeNode();
//        }
//        private void SafeSelectFirstTreeNode()
//        {
//            try
//            {
//                if (IsDisposed || Disposing) return;
//                if (kryptonTreeView_DieuHuong == null || kryptonTreeView_DieuHuong.IsDisposed || !kryptonTreeView_DieuHuong.IsHandleCreated) return;
//                if (kryptonTreeView_DieuHuong.Nodes.Count == 0)
//                {
//                    kryptonTreeView_DieuHuong.SelectedNode = null;
//                    return;
//                }
//                TreeNode firstNode = kryptonTreeView_DieuHuong.Nodes[0];
//                if (ReferenceEquals(kryptonTreeView_DieuHuong.SelectedNode, firstNode)) return;
//                BeginInvoke(new MethodInvoker(() =>
//                {
//                    try
//                    {
//                        if (IsDisposed || Disposing || kryptonTreeView_DieuHuong == null || kryptonTreeView_DieuHuong.IsDisposed) return;
//                        if (kryptonTreeView_DieuHuong.Nodes.Count > 0)
//                        {
//                            kryptonTreeView_DieuHuong.SelectedNode = kryptonTreeView_DieuHuong.Nodes[0];
//                        }
//                    }
//                    catch (ObjectDisposedException) { }
//                }));
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine($"[SafeSelectFirstTreeNode]: {ex.Message}");
//            }
//        }
//        private void KryptonTreeView_DieuHuong_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
//        {
//            TreeNode expandingNode = e.Node;
//            if (expandingNode.Nodes.Count == 1 && expandingNode.Nodes[0].Text == "Đang tải...")
//            {
//                expandingNode.Nodes.Clear();
//                if (expandingNode.Tag is TrungTenModel model)
//                {
//                    var donViArray = model.DonVi.Split(_splitChars, StringSplitOptions.RemoveEmptyEntries);
//                    foreach (var dv in donViArray)
//                    {
//                        TreeNode childNode = new TreeNode(dv.Trim())
//                        {
//                            Tag = new { HoTen = model.HoTen, DonViStr = dv.Trim() }
//                        };
//                        expandingNode.Nodes.Add(childNode);
//                    }
//                }
//            }
//        }
//        private async void KryptonTreeView_DieuHuong_AfterSelect(object? sender, TreeViewEventArgs e)
//        {
//            if (_isTreeSelecting) return;
//            if (e.Node == null || e.Node.Tag == null) return;
//            try
//            {
//                _isTreeSelecting = true;
//                List<long> listRowIdCanTim = null;
//                _donViDuocChon = "";
//                if (e.Node.Tag is TrungTenModel modelCha)
//                {
//                    listRowIdCanTim = modelCha.RowIds;
//                }
//                else
//                {
//                    dynamic tagData = e.Node.Tag;
//                    string parentName = tagData.HoTen;
//                    var parentModel = _viewData.FirstOrDefault(x => x.HoTen == parentName);
//                    if (parentModel != null)
//                    {
//                        listRowIdCanTim = parentModel.RowIds;
//                    }
//                    string rawDonVi = tagData.DonViStr;
//                    int idx = rawDonVi.LastIndexOf(" (");
//                    if (idx > 0) _donViDuocChon = rawDonVi.Substring(0, idx).Trim();
//                    else _donViDuocChon = rawDonVi.Trim();
//                }
//                // ⭐ CẬP NHẬT MÀU SẮC LẬP TỨC: Ép lưới xóa vết vẽ của đồng chí cũ, chuyển vùng highlight mượt mà
//                if (kryptonDataGridView_TrungTen != null && !kryptonDataGridView_TrungTen.IsDisposed)
//                {
//                    kryptonDataGridView_TrungTen.Invalidate();
//                }
//                if (listRowIdCanTim == null || listRowIdCanTim.Count == 0) return;
//                if (Interlocked.Exchange(ref _isProcessing, 1) == 1) return;
//                BatProgress();
//                try
//                {
//                    var token = _cts.Token;
//                    _viewDataDetail = await Task.Run(() => TruyVanChiTietSQLite(listRowIdCanTim, token), token);
//                    HienThiGrid();
//                }
//                finally
//                {
//                    TatProgress();
//                    Interlocked.Exchange(ref _isProcessing, 0);
//                }
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine($"[TreeView AfterSelect Lỗi]: {ex.Message}");
//            }
//            finally
//            {
//                _isTreeSelecting = false;
//            }
//        }
//        private void HienThiGrid()
//        {
//            var grid = kryptonDataGridView_TrungTen;
//            if (grid == null || grid.IsDisposed) return;
//            grid.SuspendLayout();
//            try
//            {
//                grid.RowCount = _viewDataDetail != null ? _viewDataDetail.Count : 0;
//                if (grid.RowCount > 0)
//                {
//                    grid.ClearSelection();
//                    grid.CurrentCell = null;
//                }
//                grid.Invalidate();
//            }
//            finally
//            {
//                grid.ResumeLayout();
//            }
//        }
//        //endregion
//        //region CHUC NANG (TIM KIEM, LAM MOI)
//        private async void TimKiem()
//        {
//            if (Interlocked.Exchange(ref _isProcessing, 1) == 1) return;
//            BatProgress();
//            string keyword = textBoxKyToon_TimTen.Text.Trim();
//            _cts?.Cancel();
//            _cts = new CancellationTokenSource();
//            var token = _cts.Token;
//            try
//            {
//                var newViewData = await Task.Run(() =>
//                {
//                    if (string.IsNullOrEmpty(keyword)) return _data;
//                    return _data.Where(x => x.HoTen.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
//                }, token);
//                _viewData = newViewData;
//                HienThiTreeView();
//                _viewDataDetail.Clear();
//                HienThiGrid();
//                CapNhatStatus();
//            }
//            catch (OperationCanceledException) { }
//            finally
//            {
//                TatProgress();
//                Interlocked.Exchange(ref _isProcessing, 0);
//            }
//        }
//        private void kryptonButton_TimKiem_Click(object? sender, EventArgs e) => TimKiem();
//        private async void kryptonButton_LamMoi_Click(object? sender, EventArgs e)
//        {
//            textBoxKyToon_TimTen.Clear();
//            await TaiDuLieuHeThongAsync();
//        }
//        private void kryptonButton_Dong_Click(object? sender, EventArgs e)
//        {
//            // 1. Tìm Form cha đang mở
//            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
//            // 2. Nếu tìm thấy Form cha thì cập nhật lại tiêu đề
//            if (formCha != null)
//            {
//                formCha.CapNhatTieuDe("Trang phân loại thi đua");
//            }
//            // 3. Đóng form hiện tại
//            this.Close();
//        }
//        private void textBoxKyToon_TimTen_KeyDown(object? sender, KeyEventArgs e)
//        {
//            if (e.KeyCode == Keys.Enter)
//            {
//                e.SuppressKeyPress = true;
//                TimKiem();
//            }
//            else if (e.KeyCode == Keys.Escape)
//            {
//                e.SuppressKeyPress = true;
//                textBoxKyToon_TimTen.Clear();
//                TimKiem();
//            }
//        }
//        //endregion
//        //region TIEN ICH UI (PHIM TAT, STATUS STRIP)
//        private Action SafeAction(Action action)
//        {
//            return () =>
//            {
//                if (action == null || Interlocked.CompareExchange(ref _isProcessing, 0, 0) == 1) return;
//                try { action.Invoke(); }
//                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Lỗi thực thi phím tắt: " + ex.Message); }
//            };
//        }
//        private bool SafeExecute(Action action)
//        {
//            SafeAction(action).Invoke();
//            return true;
//        }
//        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
//        {
//            if (Interlocked.CompareExchange(ref _isProcessing, 0, 0) == 1) return base.ProcessCmdKey(ref msg, keyData);
//            try
//            {
//                if (Module_PhimTat.XuLy(keyData, actionLamMoi: SafeAction(() => lamMoi_ToolStripMenuItem_Click(null, null))))
//                {
//                    return true;
//                }
//                Keys key = keyData & Keys.KeyCode;
//                Keys modifier = keyData & Keys.Modifiers;
//                if (modifier == Keys.None && key == Keys.F4)
//                    return SafeExecute(() => xoaTimKiem_ToolStripMenuItem_Click(null, null));
//                if (modifier == Keys.Control && key == Keys.Q)
//                    return SafeExecute(() => quayLaiTrangXuLyDuLieu_ToolStripMenuItem_Click(null, null));
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine("Lỗi phím tắt: " + ex.Message);
//                return true;
//            }
//            return base.ProcessCmdKey(ref msg, keyData);
//        }
//        private void BatProgress()
//        {
//            toolStripProgressBar1.Visible = true;
//            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
//        }
//        private void TatProgress()
//        {
//            toolStripProgressBar1.Visible = false;
//        }
//        private void CapNhatStatus()
//        {
//            int total = _data.Count;
//            int filtered = _viewData.Count;
//            if (total == 0)
//            {
//                toolStripStatusLabel1.Text = "Không phát hiện tên trùng.";
//                return;
//            }
//            if (filtered == total)
//                toolStripStatusLabel1.Text = $"Số CBCS có tên trùng nhau: {total} {Module_HeThong.Tu_dong_chi}";
//            else
//                toolStripStatusLabel1.Text = $"Kết quả tìm kiếm: {filtered}/{total} CBCS trùng tên";
//        }
//        // 🌟 TỐI ƯU HIỆU SUẤT: Cờ chặn chống gọi hàm lặp lại gây tốn CPU
//        private bool _daKhoiTaoToolTip = false;
//        private void InitToolTips()
//        {
//            // Chống gọi lại nhiều lần không cần thiết
//            if (_daKhoiTaoToolTip) return;
//            // An toàn từ gốc: Kiểm tra ToolTip có tồn tại không
//            if (toolTip1 == null) return;
//            try
//            {
//                // ================= CẤU HÌNH CHUNG =================
//                toolTip1.IsBalloon = true;
//                toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
//                toolTip1.ToolTipIcon = ToolTipIcon.Info;
//                // UX: Phản hồi nhanh – không gây khó chịu khi rê chuột qua
//                toolTip1.InitialDelay = 300;
//                toolTip1.AutoPopDelay = 2500;
//                toolTip1.ReshowDelay = 100;
//                toolTip1.ShowAlways = true;
//                // 🌟 TỐI ƯU CẤU TRÚC RAM: Dùng mảng ValueTuple giúp gom nhóm gọn gàng
//                // Dễ dàng bảo trì, mở rộng sau này mà không xả rác bộ nhớ (Heap Allocation).
//                (System.Windows.Forms.Control? control, string noiDung)[] danhSachToolTip = new (System.Windows.Forms.Control?, string)[]
//                {
//                    (kryptonButton_TimKiem, "Bấm nút để tìm kiếm"),
//                    (kryptonButton_LamMoi,  "Làm mới dữ liệu"),
//                    (kryptonButton_Dong,    "Thoát trang")
//                };
//                // 🌟 XỬ LÝ LỖI PHÂN MẢNH (ISOLATED EXCEPTION)
//                int soLoi = 0;
//                foreach (var (control, noiDung) in danhSachToolTip)
//                {
//                    // Bỏ qua an toàn nếu control chưa kịp render hoặc bị null
//                    if (control == null)
//                    {
//                        soLoi++;
//                        continue;
//                    }
//                    try
//                    {
//                        // Kiểm tra vòng đời của Control trước khi gán API
//                        if (control.IsDisposed) continue;
//                        toolTip1.SetToolTip(control, noiDung);
//                    }
//                    catch
//                    {
//                        // Bẫy lỗi cục bộ: Lỗi ở 1 nút không làm sập vòng lặp gán của các nút khác
//                        soLoi++;
//                    }
//                }
////if DEBUG
//                // Hệ thống cảnh báo nội bộ dành riêng cho Lập trình viên (Không hiện ở bản Release)
//                if (soLoi > 0)
//                    System.Diagnostics.Debug.WriteLine($"[InitToolTips] Hệ thống bỏ qua {soLoi} control do chưa khởi tạo hoặc bị null.");
////endif
//                // Đánh dấu hoàn tất để khóa cổng
//                _daKhoiTaoToolTip = true;
//            }
//            catch (Exception ex)
//            {
//                // Bắt lỗi tổng và in ra Output để Lập trình viên theo dõi
//                System.Diagnostics.Debug.WriteLine($"[Lỗi nghiêm trọng tại InitToolTips]: {ex.Message}");
//            }
//        }
//        private void SetupStatusStrip()
//        {
//            if (statusStrip1 == null || statusStrip1.IsDisposed) return;
//            if (!statusStrip1.Items.Contains(_rightSpacer))
//            {
//                _rightSpacer.Spring = true;
//                _rightSpacer.Text = "";
//                _rightSpacer.AutoSize = false;
//                int index = statusStrip1.Items.IndexOf(toolStripStatusLabel2);
//                if (index >= 0) statusStrip1.Items.Insert(index, _rightSpacer);
//                else statusStrip1.Items.Add(_rightSpacer);
//            }
//            toolStripStatusLabel2.Alignment = ToolStripItemAlignment.Right;
//            toolStripStatusLabel2.TextAlign = ContentAlignment.MiddleRight;
//        }
//        private void XacDinhPhienBan()
//        {
//            bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
//            toolStripStatusLabel2.Text = laTanBinh ? "Phiên bản phần mềm dành cho tân binh" : "Phiên bản phần mềm dành cho CBCS";
//        }
//        private void SetupPlaceholder()
//        {
//            if (textBoxKyToon_TimTen != null && !textBoxKyToon_TimTen.IsDisposed)
//                SendMessage(textBoxKyToon_TimTen.Handle, EM_SETCUEBANNER, 0, "Nhập họ và tên CBCS để tìm kiếm");
//        }
//        private void lamMoi_ToolStripMenuItem_Click(object? sender, EventArgs e) => kryptonButton_LamMoi.PerformClick();
//        private void quayLaiTrangXuLyDuLieu_ToolStripMenuItem_Click(object? sender, EventArgs e) => kryptonButton_Dong.PerformClick();
//        private void xoaTimKiem_ToolStripMenuItem_Click(object? sender, EventArgs e)
//        {
//            textBoxKyToon_TimTen.Clear();
//            textBoxKyToon_TimTen.Focus();
//            TimKiem();
//        }
//        protected override void OnFormClosing(FormClosingEventArgs e)
//        {
//            // ⭐ AN TOÀN TUYỆT ĐỐI: Ngắt toàn bộ tiến trình chạy ngầm lập tức khi tắt form
//            _cts?.Cancel();
//            var grid = kryptonDataGridView_TrungTen;
//            if (grid != null)
//            {
//                grid.CellValueNeeded -= KryptonDataGridView_TrungTen_Detail_CellValueNeeded;
//                grid.RowPrePaint -= KryptonDataGridView_TrungTen_Detail_RowPrePaint;
//            }
//            if (kryptonTreeView_DieuHuong != null)
//            {
//                kryptonTreeView_DieuHuong.BeforeExpand -= KryptonTreeView_DieuHuong_BeforeExpand;
//                kryptonTreeView_DieuHuong.AfterSelect -= KryptonTreeView_DieuHuong_AfterSelect;
//            }
//            // Giải phóng triệt để tài nguyên đồ họa GDI+ tránh treo RAM hệ điều hành
//            _headerFont?.Dispose();
//            _boldFont?.Dispose();
//            _cts?.Dispose();
//            base.OnFormClosing(e);
//        }
//        //endregion
//    }
//    public class TrungTenModel
//    {
//        public string HoTen { get; set; } = "";
//        public int SoLan { get; set; }
//        public string DonVi { get; set; } = "";
//        public List<long> RowIds { get; set; } = new();
//    }
//    public class TrungTenDetailModel
//    {
//        public string HoTen { get; set; } = "";
//        public string NamSinh { get; set; } = "";
//        public string CapBac { get; set; } = "";
//        public string DonVi { get; set; } = "";
//        public string SoHieu { get; set; } = "";
//        public string GhiChu { get; set; } = "";
//    }
//}