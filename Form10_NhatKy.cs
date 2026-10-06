using ClosedXML.Excel;
using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace PhanMemThiDua2026
{
    public partial class Form10_NhatKy : Form
    {
        // Thêm biến này vào Form6_XuLyData, Form10_NhatKy, Form15_ThongKeThiDua
        public bool DaLoadDuLieu { get; set; } = false;
        // Lê Trung Kiên -  Yêu mèo cam
        private readonly string _csdl3Path = Module_DanduongGPS.DuongDanCSDL3;
        private const int PAGE_SIZE_DEFAULT = 500;
        private const int PAGE_SIZE_MIN = 1;
        private const int PAGE_SIZE_MAX = 1000;
        private int _pageSize = PAGE_SIZE_DEFAULT;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private bool _daCaiDatCot = false;
        private int _soDongDaXoaTuDong = 0;
        private bool _sortAsc = false;
        // 🔥 BIẾN CACHE HÌNH ẢNH (Load 1 lần để tối ưu RAM)
        private readonly Image _iconLogin = Properties.Resources.ic_login;
        private readonly Image _iconDb = Properties.Resources.ic_database;
        private readonly Image _iconSave = Properties.Resources.ic_save;
        private readonly Image _iconExport = Properties.Resources.ic_export;
        private readonly Image _iconHelp = Properties.Resources.ic_help;
        private readonly Image _iconDelete = Properties.Resources.ic_delete;
        private readonly Image _iconWarning = Properties.Resources.ic_warning;
        private readonly Image _iconDefault = Properties.Resources.ic_default;
        // 🔥 BIẾN CACHE FONT TĨNH (Tối ưu RAM & Handle GDI+, dùng chung biến tên Font từ Module_HeThong)
        private static readonly Font _fontGridHeader = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Bold);
        private static readonly Font _fontGridCell = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Regular);
        private static readonly Font _fontStatus = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Regular);
        // Đổi các biến toàn cục DataTable thành List
        private List<NhatKyModel> _listFull = new List<NhatKyModel>();
        private List<NhatKyModel> _listFiltered = new List<NhatKyModel>();
        private List<NhatKyModel> _listCurrentPage = new List<NhatKyModel>();
        private bool _isPaging = false;
        private DateTime? _filterTuNgay = null;
        private DateTime? _filterDenNgay = null;
        private string _filterTaiKhoan = Module_HeThong.Tat_Ca;
        private string _filterMayTinh = Module_HeThong.Tat_Ca; // Mặc định là Module_HeThong.Tat_Ca
        // Thêm biến này ở khu vực khai báo biến đầu class
        private string _chuoiTuDongXoa = "";
        private bool _dangLamMoiBoLoc = false; // Chặn bấm dồn dập / re-entry
        // Form đã hoàn tất quá trình khởi tạo ban đầu
        private bool _daKhoiTaoHoanTat = false;
        // Đang chương trình nạp lại ComboBox
        private bool _dangNapComboBox = false;
        // Đang thực hiện thao tác lọc
        private bool _dangLocDuLieu = false;
        private bool _isInit = false;
        private int _reloadDangChay = 0;
        private string? _localIP;
        private static readonly object _localIpLock = new();
        // THÊM
        private bool _reloadChoXuLy = false;
        private static readonly string[] _cacDinhDangNgay = { "dd-MM-yyyy HH:mm:ss", "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss", "dd-MM-yyyy", Module_HeThong.Ngay_Thang_Nam, "M/d/yyyy h:mm:ss tt" };
        public Form10_NhatKy()
        {
            InitializeComponent();
            // BẬT DOUBLE BUFFERING NGAY TẠI ĐÂY
            EnableDoubleBuffered(kryptonDataGridView1);
            this.Load += Form10_Load;
            this.Shown += Form10_Shown;
            this.KeyPreview = true; // THÊM DÒNG NÀY
            // GẮN MENU CHUỘT PHẢI VÀO DATAGRIDVIEW Ở ĐÂY NÈ
            kryptonDataGridView1.ContextMenuStrip = contextMenuStrip1;
            InitToolTips();
            textBox_SoDongHienThi.KeyDown -= textBox_SoDongHienThi_KeyDown;
            textBox_SoDongHienThi.KeyDown += textBox_SoDongHienThi_KeyDown;
            kryptonButton_TiepTheo.Click -= kryptonButton_TiepTheo_Click;
            kryptonButton_TiepTheo.Click += kryptonButton_TiepTheo_Click;
            kryptonButton_TroLai.Click -= kryptonButton_TroLai_Click;
            kryptonButton_TroLai.Click += kryptonButton_TroLai_Click;
            kryptonButton_ApDungSoTrang.Click -= kryptonButton_ApDungSoTrang_Click;
            kryptonButton_ApDungSoTrang.Click += kryptonButton_ApDungSoTrang_Click;
            comboBox_LocTaiKhoan.SelectedIndexChanged -= comboBox_LocTaiKhoan_SelectedIndexChanged;
            comboBox_LocTaiKhoan.SelectedIndexChanged += comboBox_LocTaiKhoan_SelectedIndexChanged;
            comboBox1_MayTinh.SelectedIndexChanged -= comboBox1_MayTinh_SelectedIndexChanged;
            comboBox1_MayTinh.SelectedIndexChanged += comboBox1_MayTinh_SelectedIndexChanged;
            radioButton1_TuAZ.CheckedChanged -= RadioSapXep_CheckedChanged;
            radioButton1_TuAZ.CheckedChanged += RadioSapXep_CheckedChanged;
            radioButton1_TuZA.CheckedChanged -= RadioSapXep_CheckedChanged;
            radioButton1_TuZA.CheckedChanged += RadioSapXep_CheckedChanged;
            // ⭐ THÊM 2 DÒNG NÀY ĐỂ NÚT LỌC HOẠT ĐỘNG:
            kryptonButton1_LocTheoNgayThangNam.Click -= kryptonButton1_LocTheoNgayThangNam_Click;
            kryptonButton1_LocTheoNgayThangNam.Click += kryptonButton1_LocTheoNgayThangNam_Click;
            kryptonButton1_LamMoiBoLoc.Click -= kryptonButton1_LamMoiBoLoc_Click;
            kryptonButton1_LamMoiBoLoc.Click += kryptonButton1_LamMoiBoLoc_Click;
            // Đăng ký sự kiện tự vẽ bảng
            kryptonDataGridView1.CellPainting -= KryptonDataGridView1_CellPainting;
            kryptonDataGridView1.CellPainting += KryptonDataGridView1_CellPainting;
            // 
            // SỰ KIỆN DOUBLE CLICK DATAGRID - MỞ CHI TIẾT NHẬT KÝ
            // 
            kryptonDataGridView1.CellDoubleClick -= KryptonDataGridView1_CellDoubleClick;
            kryptonDataGridView1.CellDoubleClick += KryptonDataGridView1_CellDoubleClick;
            // 1. Đăng ký sự kiện lắng nghe khi CSDL Nhật ký thay đổi
            Module_NhatKy.OnNhatKyDaThayDoi += Form10_NhatKy_OnNhatKyDaThayDoi;
            // Thêm vào constructor Form10_NhatKy()
            this.Activated += Form10_NhatKy_Activated;
            // 2. Đăng ký sự kiện FormClosed để gỡ bỏ lắng nghe khi đóng Form
            this.FormClosed += Form10_NhatKy_FormClosed;
        }
        private void Form10_Load(object? sender, EventArgs e)
        {
            if (_isInit) return;
            _isInit = true;
            Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1);
            // CẤU HÌNH DATE TIME PICKER
            kryptonDateTimePicker1_NgayThangNamBatDau.Format = DateTimePickerFormat.Custom;
            kryptonDateTimePicker1_NgayThangNamBatDau.CustomFormat = Module_HeThong.Ngay_Thang_Nam;
            kryptonDateTimePicker1_NgayThangNamKetThuc.Format = DateTimePickerFormat.Custom;
            kryptonDateTimePicker1_NgayThangNamKetThuc.CustomFormat = Module_HeThong.Ngay_Thang_Nam;
            // ⭐ GIÁ TRỊ MẶC ĐỊNH
            // Ngày bắt đầu = hôm nay - 1 tháng
            // Ngày kết thúc = hôm nay
            DateTime ngayHienTai = DateTime.Today;
            kryptonDateTimePicker1_NgayThangNamBatDau.Value = ngayHienTai.AddMonths(-1);
            kryptonDateTimePicker1_NgayThangNamKetThuc.Value = ngayHienTai;
            textBox_SoDongHienThi.Text = PAGE_SIZE_DEFAULT.ToString();
            CauHinhCangDeuStatusStrip();
            CaiDatCot();
        }
        private async void Form10_Shown(object? sender, EventArgs e)
        {
            if (_daKhoiTaoHoanTat)
                return;
            _daKhoiTaoHoanTat = true;
            try
            {
                kryptonDataGridView1.VirtualMode = true;
                kryptonDataGridView1.CellValueNeeded -= KryptonDataGridView1_CellValueNeeded;
                kryptonDataGridView1.CellValueNeeded += KryptonDataGridView1_CellValueNeeded;
                toolStripStatusLabel1.Text = $"Tài khoản: {Module_TaiKhoan.TenTaiKhoan_RAM}";
                // 1. KHỞI TẠO DATABASE
                await KiemTraVaTaoBangNhatKyAsync(_csdl3Path);
                if (IsDisposed)
                    return;
                // 2. CHUẨN BỊ DATABASE
                await Task.Run(() =>
                {
                    try
                    {
                        DamBaoBangTuDongXoaTonTai();
                        Module_TaiKhoan.NapTaiKhoanTuCSDL();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(
                            $"[Form10] Lỗi chuẩn bị DB: {ex.Message}");
                    }
                });
                if (IsDisposed)
                    return;
                // 3. LOAD TOÀN BỘ DỮ LIỆU
                await ReloadDuLieuAsync();
                // 4. STATUS XÓA NGẦM
                _ = Task.Run(() =>
                {
                    try
                    {
                        CapNhatStatusLabelTuDongXoaNgam();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(
                            $"[Form10] Lỗi status xóa tự động: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[Form10_Shown] {ex}");
            }
        }
        private void Form10_NhatKy_FormClosed(object sender, FormClosedEventArgs e)
        {
            // 2. Hủy đăng ký khi đóng Form để giải phóng tài nguyên
            Module_NhatKy.OnNhatKyDaThayDoi -= Form10_NhatKy_OnNhatKyDaThayDoi;
        }
        private bool _needsReloadOnVisible = false;
        private void Form10_NhatKy_OnNhatKyDaThayDoi()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(Form10_NhatKy_OnNhatKyDaThayDoi));
                return;
            }
            if (this.Visible)
            {
                //// ✅ CHÍNH XÁC: Reset cờ reload và nạp lại CSDL ngay lập tức
                Interlocked.Exchange(ref _reloadDangChay, 0);
                _ = ReloadDuLieuAsync();
                // Sử dụng PerformClick() nếu muốn bấm nút trên UI
                kryptonButton1_LamMoiBoLoc.PerformClick();
            }
            else
            {
                _needsReloadOnVisible = true;
            }
        }
        private async void Form10_NhatKy_Activated(object? sender, EventArgs e)
        {
            if (_needsReloadOnVisible)
            {
                _needsReloadOnVisible = false;
                Interlocked.Exchange(ref _reloadDangChay, 0);
                await ReloadDuLieuAsync();
            }
        }
        private async void Form10_NhatKy_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible && _needsReloadOnVisible)
            {
                _needsReloadOnVisible = false;
                // Đặt lại cờ khóa và nạp lại toàn bộ dữ liệu từ CSDL
                Interlocked.Exchange(ref _reloadDangChay, 0);
                await ReloadDuLieuAsync();
            }
        }
        // 🔴 Đảm bảo hủy đăng ký khi đóng Form để tránh rò rỉ bộ nhớ (Memory Leak)
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Module_NhatKy.OnNhatKyDaThayDoi -= Form10_NhatKy_OnNhatKyDaThayDoi;
            base.OnFormClosed(e);
        }
        public void ThemNhatKyTucThoi(NhatKyModel itemMoi)
        {
            if (itemMoi == null) return;
            UIHelper.SafeInvoke(this, () =>
            {
                // 1. Giải mã và gán thuộc tính ngay cho bản ghi mới
                itemMoi.ThoiGian = GiaiMaAnToan(itemMoi.ThoiGianRaw);
                itemMoi.TaiKhoan = GiaiMaAnToan(itemMoi.TaiKhoanRaw);
                itemMoi.TenMay = GiaiMaAnToan(itemMoi.TenMayRaw);
                itemMoi.DaGiaiMaBoLoc = true;
                // Chuẩn hóa DateTime để bộ lọc ngày nhận diện được ngay
                string[] cacDinhDangNgay = { "dd-MM-yyyy HH:mm:ss", "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss", "dd-MM-yyyy", Module_HeThong.Ngay_Thang_Nam };
                if (DateTime.TryParseExact(itemMoi.ThoiGian?.Trim(), cacDinhDangNgay, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime dtLog))
                {
                    itemMoi.ThoiGianParsed = dtLog.Date;
                }
                else if (DateTime.TryParse(itemMoi.ThoiGian, out DateTime dtAuto))
                {
                    itemMoi.ThoiGianParsed = dtAuto.Date;
                }
                // 2. Thêm vào đầu danh sách RAM
                _listFull.Insert(0, itemMoi);
                // 3. Tải lại bộ lọc và vẽ lại Grid
                ThucThiBoLocToanDienAsync();
            });
        }
        private async Task KiemTraVaTaoBangNhatKyAsync(string csdl3Path)
        {
            // 1. Kiểm tra đường dẫn file CSDL
            if (string.IsNullOrWhiteSpace(csdl3Path)) return;
            try
            {
                // Nếu thư mục chứa file CSDL chưa tồn tại thì tạo mới thư mục
                string folder = Path.GetDirectoryName(csdl3Path);
                if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                // 2. Chuỗi kết nối tới file database
                string connectionString = $"Data Source={csdl3Path};";
                using var conn = new SqliteConnection(connectionString);
                await conn.OpenAsync();
                // 🔥 TỐI ƯU: Bật chế độ WAL để Form Nhật ký đọc ghi siêu tốc, chống lock DB
                using (var cmdWal = new SqliteCommand("PRAGMA journal_mode=WAL;", conn))
                {
                    await cmdWal.ExecuteNonQueryAsync();
                }
                // 3. Sử dụng từ khóa IF NOT EXISTS để SQLite tự kiểm tra và tạo nếu chưa có
                string query = @"
        CREATE TABLE IF NOT EXISTS ""NhatKyUngDung"" (
            ""ID""        INTEGER NOT NULL,
            ""ThoiGian""  TEXT,
            ""TenMay""    TEXT,
            ""ID_CPU""    TEXT,
            ""TaiKhoan""  TEXT,
            ""HanhDong""  TEXT,
            ""GhiChu""    TEXT,
            PRIMARY KEY(""ID"" AUTOINCREMENT)
        );";
                using var cmd = new SqliteCommand(query, conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                // Theo dõi lỗi an toàn trong cửa sổ Output (Debug) khi dev phần mềm
                System.Diagnostics.Debug.WriteLine($"[LỖI KHỞI TẠO DB NHẬT KÝ]: {ex.Message}");
            }
        }
        private void KryptonDataGridView1_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;
            if (_listCurrentPage == null || e.RowIndex >= _listCurrentPage.Count)
                return;
            try
            {
                NhatKyModel data = _listCurrentPage[e.RowIndex];
                if (data == null)
                    return;
                if (!data.DaGiaiMaBoLoc)
                {
                    data.ThoiGian = GiaiMaAnToan(data.ThoiGianRaw);
                    data.TaiKhoan = GiaiMaAnToan(data.TaiKhoanRaw);
                    data.DaGiaiMaBoLoc = true;
                }
                if (!data.DaGiaiMa)
                {
                    data.TenMay = GiaiMaAnToan(data.TenMayRaw);
                    data.IP = GetLocalIP();
                    data.ID_CPU = GiaiMaAnToan(data.ID_CPURaw);
                    data.HanhDong = GiaiMaAnToan(data.HanhDongRaw);
                    data.GhiChu = GiaiMaAnToan(data.GhiChuRaw);
                    data.DaGiaiMa = true;
                }
                HienThiFormAo_ChiTietNhatKy(data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Form10] Lỗi mở chi tiết nhật ký: {ex}");
                MessageBox.Show(
                    "Không thể mở thông tin chi tiết nhật ký.\n\n" + ex.Message,
                    "Lỗi giao diện",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void HienThiFormAo_ChiTietNhatKy(NhatKyModel data)
        {
            if (data == null)
                return;
            Color mauNen = Color.White;
            Color mauNhan = Color.FromArgb(245, 248, 252);
            Color mauFooter = Color.FromArgb(248, 249, 250);
            Color mauHanhDong = Color.FromArgb(0, 102, 204);
            Color mauGhiChu = Color.FromArgb(80, 80, 80);
            using (var formAo = new FormAoBase())
            {
                formAo.Text = "Chi tiết nhật ký phần mềm";
                formAo.Size = new System.Drawing.Size(620, 560);
                formAo.FormBorderStyle = FormBorderStyle.FixedDialog;
                formAo.MaximizeBox = false;
                formAo.MinimizeBox = false;
                formAo.ShowIcon = false;
                formAo.ShowInTaskbar = false;
                formAo.StartPosition = FormStartPosition.CenterParent;
                formAo.BackColor = mauNen;
                var panelBottom = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 60,
                    BackColor = mauFooter
                };
                var btnClose = new KryptonButton
                {
                    Text = "Đóng",
                    Width = 120,
                    Height = 38,
                    DialogResult = DialogResult.OK,
                    Anchor = AnchorStyles.None
                };
                btnClose.StateCommon.Content.ShortText.Font = _fontGridHeader;
                btnClose.StateCommon.Border.Rounding = 5;
                btnClose.Location = new Point((formAo.ClientSize.Width - btnClose.Width) / 2, 11);
                panelBottom.Controls.Add(btnClose);
                var panelContent = new KryptonPanel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(20, 20, 20, 10)
                };
                panelContent.StateCommon.Color1 = mauNen;
                var grid = new KryptonDataGridView
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    AllowUserToResizeRows = false,
                    AllowUserToResizeColumns = false,
                    RowHeadersVisible = false,
                    ColumnHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    MultiSelect = false,
                    BackgroundColor = mauNen,
                    BorderStyle = BorderStyle.None,
                    AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
                };
                grid.GridStyles.Style = DataGridViewStyle.List;
                grid.StateCommon.Background.Color1 = mauNen;
                grid.StateCommon.DataCell.Content.Font = _fontGridCell;
                grid.RowTemplate.MinimumHeight = 42;
                grid.Columns.Add("Ten", "Thông tin");
                var cotTen = grid.Columns[0];
                cotTen.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                cotTen.Width = 210;
                cotTen.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                cotTen.DefaultCellStyle.Font = _fontGridHeader;
                cotTen.DefaultCellStyle.BackColor = mauNhan;
                cotTen.DefaultCellStyle.Padding = new Padding(15, 8, 10, 8);
                grid.Columns.Add("GiaTri", "Nội dung");
                var cotGiaTri = grid.Columns[1];
                cotGiaTri.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                cotGiaTri.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                cotGiaTri.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                cotGiaTri.DefaultCellStyle.Padding = new Padding(10, 8, 15, 8);
                int ThemDong(string ten, string giaTri, Color? mauGiaTri = null, Font fontGiaTri = null)
                {
                    int rowIndex = grid.Rows.Add(
                        string.IsNullOrWhiteSpace(ten) ? "" : ten.Trim(),
                        string.IsNullOrWhiteSpace(giaTri) ? "—" : giaTri.Trim());
                    var rowData = grid.Rows[rowIndex];
                    if (mauGiaTri.HasValue)
                        rowData.Cells[1].Style.ForeColor = mauGiaTri.Value;
                    if (fontGiaTri != null)
                        rowData.Cells[1].Style.Font = fontGiaTri;
                    return rowIndex;
                }
                ThemDong("ID", data.ID.ToString("N0"));
                ThemDong("Thời gian", data.ThoiGian);
                ThemDong("Tên máy", data.TenMay);
                ThemDong("IP Address", data.IP);
                ThemDong("ID My Computer", data.ID_CPU);
                ThemDong("Tài khoản", data.TaiKhoan, Color.Green, _fontGridHeader);
                ThemDong("Hành động", data.HanhDong, mauHanhDong, _fontGridHeader);
                ThemDong("Ghi chú", data.GhiChu, mauGhiChu);
                panelContent.Controls.Add(grid);
                formAo.Controls.Add(panelContent);
                formAo.Controls.Add(panelBottom);
                formAo.AcceptButton = btnClose;
                formAo.CancelButton = btnClose;
                formAo.Shown += (s, e) =>
                {
                    grid.ClearSelection();
                    if (grid.Rows.Count > 0)
                        grid.FirstDisplayedScrollingRowIndex = 0;
                    btnClose.Focus();
                };
                formAo.ShowDialog(this);
            }
        }
        private void CapNhatStatusLabelTuDongXoaNgam()
        {
            try
            {
                string luaChon = "Không xóa";
                using (var cn = new SqliteConnection($"Data Source={_csdl3Path}"))
                {
                    cn.Open();
                    using var cmdSelect = cn.CreateCommand();
                    cmdSelect.CommandText = "SELECT Chọn_GiaiTri FROM TuDong_XoaNhatKy WHERE ID = 1";
                    var result = cmdSelect.ExecuteScalar();
                    if (result != null && result != DBNull.Value) luaChon = result.ToString();
                }
                int soDongTuCsdl = luaChon switch
                {
                    "1000 dòng xóa tự động" => 1000,
                    "5000 dòng xóa tự động" => 5000,
                    "10000 dòng xóa tự động" => 10000,
                    _ => 0
                };
                // ⭐ LƯU THÔNG TIN XÓA NGẦM VÀ GỌI HÀM CẬP NHẬT CHUNG
                _chuoiTuDongXoa = soDongTuCsdl > 0 ? $" | Tự động xóa khi đạt {soDongTuCsdl} dòng" : "";
                CapNhatHienThiTaiKhoanLabel();
            }
            catch { }
        }
        // Thêm hàm này vào trong class Form10_NhatKy
        private void CapNhatHienThiTaiKhoanLabel()
        {
            // Xác định đang xem tất cả hay xem một người cụ thể
            bool laTatCa = string.IsNullOrWhiteSpace(_filterTaiKhoan) || _filterTaiKhoan == Module_HeThong.Tat_Ca;
            string tenHienThi = laTatCa ? Module_TaiKhoan.TenTaiKhoan_RAM : _filterTaiKhoan;
            string tienTo = laTatCa ? "Tài khoản: " : "Đang lọc dữ liệu của: ";
            if (this.IsHandleCreated && !this.IsDisposed)
            {
                this.BeginInvoke(new Action(() =>
                {
                    // Nối 3 mảnh: Tiền tố + Tên + Hậu tố tự động xóa
                    toolStripStatusLabel1.Text = $"{tienTo}{tenHienThi}{_chuoiTuDongXoa}";
                }));
            }
        }
        private void EnableDoubleBuffered(Control control)
        {
            typeof(Control).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic,
                null, control, new object[] { true });
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // 1. BẢO VỆ HIỆU SUẤT & CHỐNG CRASH
            if (_isPaging)
            {
                if (keyData == Keys.Escape) this.Close();
                return true;
            }
            // 2. GỌI MODULE PHÍM TẮT DÙNG CHUNG
            bool daXuLyPhimTat = Module_PhimTat.XuLy(
                keyData: keyData,
                actionLamMoi: () => lamMoi_ToolStripMenuItem.PerformClick(),
                actionXuatExcel: () => xuatNhatKy_ToolStripMenuItem.PerformClick(),
                actionXoa: () => xoaToanBoDuLieu_ToolStripMenuItem.PerformClick()
            );
            if (daXuLyPhimTat) return true;
            // BỔ SUNG: KIỂM TRA TRẠNG THÁI FOCUS CỦA CONTROL (ACTIVE CONTROL)
            // Lấy control đang được focus hiện tại
            Control activeCtrl = this.ActiveControl;
            // Nếu bạn dùng Krypton, ActiveControl đôi khi là KryptonTextBox bọc bên ngoài một TextBox thực sự.
            bool dangNhapLieu = activeCtrl is TextBoxBase ||
                                activeCtrl is ComboBox ||
                                (activeCtrl != null && activeCtrl.GetType().Name.Contains("TextBox"));
            bool dangThaoTacGrid = activeCtrl is DataGridView ||
                                   (activeCtrl != null && activeCtrl.GetType().Name.Contains("DataGridView"));
            // 3. XỬ LÝ PHÍM TẮT ĐẶC THÙ RIÊNG CỦA FORM 10
            switch (keyData)
            {
                // 🔹 Ctrl + T -> Mở Form thống kê tài khoản (Phím này không đụng chạm TextBox nên cứ chạy bình thường)
                case Keys.Control | Keys.T:
                    thongKeTaiKhoanDaTungSuDungToolStripMenuItem.PerformClick();
                    return true;
                // 🔹 Phím Mũi tên Phải hoặc PageDown
                case Keys.Right:
                case Keys.PageDown:
                    // Nếu đang gõ chữ hoặc đang xem Grid -> Nhả phím ra cho Control tự xử lý
                    if (dangNhapLieu || dangThaoTacGrid)
                        return base.ProcessCmdKey(ref msg, keyData);
                    trangTiepTheo_ToolStripMenuItem.PerformClick();
                    return true;
                // 🔹 Phím Mũi tên Trái hoặc PageUp
                case Keys.Left:
                case Keys.PageUp:
                    // Nếu đang gõ chữ hoặc đang xem Grid -> Nhả phím ra cho Control tự xử lý
                    if (dangNhapLieu || dangThaoTacGrid)
                        return base.ProcessCmdKey(ref msg, keyData);
                    trangTroLai_ToolStripMenuItem.PerformClick();
                    return true;
            }
            // 4. TRẢ LẠI CHO WINDOWS
            return base.ProcessCmdKey(ref msg, keyData);
        }
        private void ThietLapKhoangNgayMacDinh()
        {
            // 
            // ⭐ KHOẢNG NGÀY MẶC ĐỊNH
            // Ngày bắt đầu = hôm nay - 1 tháng
            // Ngày kết thúc = hôm nay
            // 
            DateTime ngayHienTai = DateTime.Today;
            kryptonDateTimePicker1_NgayThangNamBatDau.Value =
                ngayHienTai.AddMonths(-1);
            kryptonDateTimePicker1_NgayThangNamKetThuc.Value =
                ngayHienTai;
        }
        private void ChuanBiDuLieuBoLocNgam()
        {
            try
            {
                if (_listFull == null || _listFull.Count == 0)
                    return;
                string[] cacDinhDangNgay =
                {
            "dd-MM-yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss",
            "dd-MM-yyyy",
            Module_HeThong.Ngay_Thang_Nam,
            "M/d/yyyy h:mm:ss tt"
        };
                int maxThreads = Math.Max(1, Environment.ProcessorCount / 2);
                _listFull
                    .AsParallel()
                    .WithDegreeOfParallelism(maxThreads)
                    .ForAll(item =>
                    {
                        if (item.DaGiaiMaBoLoc)
                            return;
                        // Giải mã dữ liệu phục vụ bộ lọc
                        item.ThoiGian = GiaiMaAnToan(item.ThoiGianRaw);
                        item.TaiKhoan = GiaiMaAnToan(item.TaiKhoanRaw);
                        item.TenMay = GiaiMaAnToan(item.TenMayRaw);
                        // Chuẩn hóa ngày để lọc nhanh
                        if (DateTime.TryParseExact(
                            item.ThoiGian?.Trim(),
                            cacDinhDangNgay,
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None,
                            out DateTime dtLog))
                        {
                            item.ThoiGianParsed = dtLog.Date;
                        }
                        else if (DateTime.TryParse(
                            item.ThoiGian,
                            out DateTime dtAuto))
                        {
                            item.ThoiGianParsed = dtAuto.Date;
                        }
                        item.DaGiaiMaBoLoc = true;
                    });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChuanBiDuLieuBoLocNgam] {ex.Message}");
            }
        }
        public void LoadNhatKyLenDataGridView_SieuToc()
        {
            var dtMaHoa = Module_NhatKy.LoadTatCaNhatKy();
            if (dtMaHoa == null || dtMaHoa.Rows.Count == 0) return;
            var listRaw = new List<NhatKyModel>(dtMaHoa.Rows.Count);
            foreach (DataRow row in dtMaHoa.Rows)
            {
                // CHỈ COPY THÔ VÀO RAM, TUYỆT ĐỐI KHÔNG GIẢI MÃ Ở ĐÂY!
                listRaw.Add(new NhatKyModel
                {
                    ID = row["ID"] != DBNull.Value ? Convert.ToInt64(row["ID"]) : 0,
                    ThoiGianRaw = row["ThoiGian"]?.ToString() ?? "",
                    TenMayRaw = row["TenMay"]?.ToString() ?? "",
                    ID_CPURaw = row["ID_CPU"]?.ToString() ?? "",
                    TaiKhoanRaw = row["TaiKhoan"]?.ToString() ?? "",
                    HanhDongRaw = row["HanhDong"]?.ToString() ?? "",
                    GhiChuRaw = row["GhiChu"]?.ToString() ?? "",
                    DaGiaiMa = false,
                    DaGiaiMaBoLoc = false
                });
            }
            _listFull = listRaw;
            _listFiltered = new List<NhatKyModel>(_listFull);
        }
        private void HienThiTrangHienTai()
        {
            if (_listFiltered == null || _listFiltered.Count == 0)
            {
                kryptonDataGridView1.RowCount = 0;
                label_Trang.Text = toolStripStatusLabel2.Text = "Trang 0 / 0";
                toolStripStatusLabel4.Text = "Không tìm thấy dữ liệu phù hợp";
                return;
            }
            int start = (_currentPage - 1) * _pageSize;
            int count = Math.Min(_pageSize, _listFiltered.Count - start);
            _listCurrentPage = _listFiltered.GetRange(start, count);
            kryptonDataGridView1.RowCount = count + 1;
            kryptonDataGridView1.Invalidate();
            label_Trang.Text = $"             Trang {_currentPage} / {_totalPages}";
            toolStripStatusLabel2.Text = $"{_currentPage} / {_totalPages} trang";
            toolStripStatusLabel4.Text = $"Hiển thị {count:N0} / {_listFiltered.Count:N0} hành động";
        }
        private void CaiDatCot()
        {
            var dgv = kryptonDataGridView1;
            if (_daCaiDatCot || dgv == null || dgv.IsDisposed)
                return;
            dgv.SuspendLayout();
            try
            {
                // 1. Áp dụng chuẩn Web Design (Khoảng trắng, Màu sắc, Viền phẳng)
                CauHinhStyleWeb(dgv);
                // 2. Cấu hình DataGridView cơ bản (Chống nhấp nháy, Ẩn header trái...)
                CauHinhGridCoBan(dgv);
                // 3. Xóa và tạo cột (Giữ nguyên FillWeight gốc của bạn)
                dgv.Columns.Clear();
                TaoCot(dgv);
                _daCaiDatCot = true;
            }
            finally
            {
                dgv.ResumeLayout(true);
            }
        }
        // 🌟 HÀM MỚI: Tách riêng phần định dạng giao diện "Web Design"
        private void CauHinhStyleWeb(DataGridView dgv)
        {
            // ÁP DỤNG PHONG CÁCH "WEB DESIGN": CÓ VIỀN MỜ (FLAT BORDER)
            // 1. Chiều cao và khoảng trống
            dgv.RowTemplate.Height = 36;
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgv.AllowUserToResizeRows = false;
            dgv.ColumnHeadersHeight = 45;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            // 2. BẬT LẠI VIỀN CHUẨN CỦA DATAGRIDVIEW
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            dgv.GridColor = Color.FromArgb(224, 224, 224); // Màu xám nhạt tinh tế
            // Màu nền xen kẽ cực nhạt để dễ đọc
            dgv.RowsDefaultCellStyle.BackColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.BackgroundColor = Color.White;
            // 3. BẬT LẠI VIỀN CỦA KRYPTON
            if (dgv is Krypton.Toolkit.KryptonDataGridView kDgv)
            {
                kDgv.GridStyles.Style = Krypton.Toolkit.DataGridViewStyle.List;
                kDgv.StateCommon.HeaderColumn.Content.Padding = new System.Windows.Forms.Padding(6, 8, 6, 8);
                kDgv.StateCommon.HeaderColumn.Content.Font = _fontGridHeader;
                kDgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                kDgv.StateCommon.DataCell.Content.Font = _fontGridCell;
                kDgv.StateCommon.DataCell.Content.Padding = new System.Windows.Forms.Padding(6, 8, 6, 8);
                // ⭐ BẬT VẼ VIỀN (BORDER) VÀ SET MÀU XÁM NHẠT
                kDgv.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
                kDgv.StateCommon.DataCell.Border.Color1 = System.Drawing.Color.FromArgb(224, 224, 224);
                kDgv.StateCommon.DataCell.Border.Width = 1;
                // Màu chọn dòng (Selection State)
                kDgv.StateSelected.DataCell.Back.Color1 = System.Drawing.Color.FromArgb(232, 244, 253);
                kDgv.StateSelected.DataCell.Back.Color2 = System.Drawing.Color.FromArgb(232, 244, 253);
                kDgv.StateSelected.DataCell.Content.Color1 = System.Drawing.Color.FromArgb(0, 102, 204);
                kDgv.Margin = new Padding(0, 0, 0, 30);
            }
        }
        private void CauHinhGridCoBan(DataGridView dgv)
        {
            dgv.AutoGenerateColumns = false;
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.MultiSelect = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.RowHeadersVisible = false;
            dgv.EnableHeadersVisualStyles = false;
            dgv.ScrollBars = ScrollBars.Both;
            dgv.BorderStyle = BorderStyle.None;
        }
        private void TaoCot(DataGridView dgv)
        {
            if (dgv == null || dgv.IsDisposed) return;
            // 1. TẠO CÁC CỘT
            AddCol(dgv, "ID", "STT", 65);
            AddCol(dgv, "ThoiGian", "Thời gian", 12);
            AddCol(dgv, "TenMay", "Tên máy", 12);
            AddCol(dgv, "IP", "IP Address", 10);
            AddCol(dgv, "ID_CPU", "ID My Computer", 18);
            AddCol(dgv, "TaiKhoan", "Tài khoản", 12);
            AddCol(dgv, "HanhDong", "Hành động", 17, DataGridViewContentAlignment.MiddleLeft);
            AddCol(dgv, "GhiChu", "Ghi chú", 24, DataGridViewContentAlignment.MiddleLeft);
            // 2. CỘT PHỤ DÙNG CHO LOGIC / ICON
            var colIcon = new DataGridViewTextBoxColumn { Name = "IconType", HeaderText = "IconType", Visible = false, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable };
            dgv.Columns.Add(colIcon);
            // 3. CẤU HÌNH KÍCH THƯỚC
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (!col.Visible) continue;
                // Không cho DataGridView tự đo nội dung.
                // Tránh AutoSize gây tốn CPU khi có nhiều dòng.
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
            // 4. CỘT STT: KÍCH THƯỚC CỐ ĐỊNH
            if (dgv.Columns["ID"] is DataGridViewColumn colID)
            {
                colID.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                colID.Width = 65;
                colID.MinimumWidth = 65;
                colID.Resizable = DataGridViewTriState.False;
            }
            // 5. GIỚI HẠN ĐỘ RỘNG MỘT SỐ CỘT
            DatagridSetWidth(dgv, "ThoiGian", 120, 180);
            DatagridSetWidth(dgv, "TenMay", 100, 180);
            DatagridSetWidth(dgv, "IP", 100, 160);
            DatagridSetWidth(dgv, "ID_CPU", 140, 260);
            DatagridSetWidth(dgv, "TaiKhoan", 100, 180);
            DatagridSetWidth(dgv, "HanhDong", 140, 280);
            DatagridSetWidth(dgv, "GhiChu", 180, 400);
        }
        private static void DatagridSetWidth(DataGridView dgv, string columnName, int minimumWidth, int maximumWidth)
        {
            if (dgv.Columns[columnName] is not DataGridViewColumn col)
                return;
            col.MinimumWidth = minimumWidth;
            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            // FillWeight kiểm soát tỷ lệ chiếm không gian.
            col.FillWeight = Math.Max(1, maximumWidth);
        }
        private void AddCol(DataGridView dgv, string name, string header, float fillWeight, DataGridViewContentAlignment align = DataGridViewContentAlignment.MiddleCenter)
        {
            var col = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = align },
                FillWeight = fillWeight
            };
            dgv.Columns.Add(col);
        }
        public void LoadNhatKyLenDataGridView()
        {
            try
            {
                var dtMaHoa = Module_NhatKy.LoadTatCaNhatKy();
                if (dtMaHoa == null || dtMaHoa.Rows.Count == 0) return;
                var listRaw = new List<NhatKyModel>(dtMaHoa.Rows.Count);
                foreach (DataRow row in dtMaHoa.Rows)
                {
                    listRaw.Add(new NhatKyModel
                    {
                        ID = row["ID"] != DBNull.Value ? Convert.ToInt64(row["ID"]) : 0,
                        ThoiGianRaw = row["ThoiGian"]?.ToString() ?? "",
                        TenMayRaw = row["TenMay"]?.ToString() ?? "",
                        ID_CPURaw = row["ID_CPU"]?.ToString() ?? "",
                        TaiKhoanRaw = row["TaiKhoan"]?.ToString() ?? "",
                        HanhDongRaw = row["HanhDong"]?.ToString() ?? "",
                        GhiChuRaw = row["GhiChu"]?.ToString() ?? "",
                        DaGiaiMa = false
                    });
                }
                string[] cacDinhDangNgay = { "dd-MM-yyyy HH:mm:ss", "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss", "dd-MM-yyyy", Module_HeThong.Ngay_Thang_Nam, "M/d/yyyy h:mm:ss tt" };
                // 🔥 CHỈ GIẢI MÃ 2 CỘT QUAN TRỌNG ĐỂ PHỤC VỤ CHỨC NĂNG LỌC (Filter)
                // Thay vì dùng Parallel (dễ đụng chạm), ta có thể dùng vòng lặp thường hoặc Partitioner để kiểm soát số luồng.
                // Tối ưu nhất là dùng AsParallel().WithDegreeOfParallelism()
                int maxThreads = Environment.ProcessorCount / 2;
                if (maxThreads < 1) maxThreads = 1;
                listRaw.AsParallel().WithDegreeOfParallelism(maxThreads).ForAll(item =>
                {
                    // Chỉ giải mã Thời gian và Tài khoản
                    item.ThoiGian = GiaiMaAnToan(item.ThoiGianRaw);
                    item.TaiKhoan = GiaiMaAnToan(item.TaiKhoanRaw);
                    if (DateTime.TryParseExact(item.ThoiGian.Trim(), cacDinhDangNgay,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out DateTime dtLog))
                    {
                        item.ThoiGianParsed = dtLog.Date;
                    }
                    else if (DateTime.TryParse(item.ThoiGian, out DateTime dtAuto))
                    {
                        item.ThoiGianParsed = dtAuto.Date;
                    }
                });
                _listFull = listRaw;
                _listFiltered = new List<NhatKyModel>(_listFull);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi load dữ liệu nhật ký: {ex.Message}");
            }
        }
        /// <summary>
        /// Lấy địa chỉ IPv4 nội bộ (LAN) đang được hệ điều hành sử dụng để định tuyến ra mạng.
        /// Ưu tiên kỹ thuật UDP-socket (chuẩn xác nhất), có fallback sang DNS nếu thất bại.
        /// Kết quả được cache lại sau lần gọi đầu tiên thành công.
        /// </summary>
        private string GetLocalIP()
        {
            // Đọc nhanh nếu đã cache (double-checked locking, tránh race-condition khi đa luồng)
            if (!string.IsNullOrEmpty(_localIP)) return _localIP;

            lock (_localIpLock)
            {
                if (!string.IsNullOrEmpty(_localIP)) return _localIP;

                _localIP = TryGetIPViaSocket()
                           ?? TryGetIPViaDns()
                           ?? "127.0.0.1";

                return _localIP;
            }
        }
        /// <summary>
        /// Cách 1 (ưu tiên): Mở UDP socket "giả" tới một IP ngoài (không gửi gói tin thật,
        /// không cần Internet) để hệ điều hành tự động chọn đúng NIC/IP dùng để định tuyến.
        /// Đây là cách đáng tin cậy nhất, tránh nhầm với IP ảo (VPN, Hyper-V, VMware...).
        /// </summary>
        private string? TryGetIPViaSocket()
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                // 8.8.8.8:65530 chỉ dùng để "Connect" ảo (UDP không bắt tay thật),
                // mục đích duy nhất là buộc OS chọn route/interface phù hợp.
                socket.Connect("8.8.8.8", 65530);

                if (socket.LocalEndPoint is IPEndPoint endPoint &&
                    !IPAddress.IsLoopback(endPoint.Address))
                {
                    return endPoint.Address.ToString();
                }
            }
            catch
            {
                // Không có mạng, bị chặn firewall, hoặc môi trường sandbox không cho mở socket.
                // Bỏ qua để rơi xuống phương án dự phòng (fallback).
            }
            return null;
        }
        /// <summary>
        /// Cách 2 (dự phòng): Duyệt danh sách IP từ tên máy qua DNS, loại bỏ các IP không
        /// hợp lệ cho mạng LAN thực (loopback, APIPA 169.254.x.x).
        /// </summary>
        private string? TryGetIPViaDns()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());

                return host.AddressList
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                    .Where(ip => !IPAddress.IsLoopback(ip))
                    .Select(ip => ip.ToString())
                    .FirstOrDefault(ip => !ip.StartsWith("169.254."));
            }
            catch
            {
                // Không resolve được hostname (hiếm gặp, thường do cấu hình mạng bất thường).
            }
            return null;
        }
        private void KryptonDataGridView1_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            if (_listCurrentPage == null || e.RowIndex < 0) return;
            if (e.RowIndex >= _listCurrentPage.Count)
            {
                e.Value = ""; // Dòng trống
                return;
            }
            var item = _listCurrentPage[e.RowIndex];
            // Đảm bảo 2 cột lọc được giải mã nếu luồng ngầm chạy chưa kịp tới dòng này
            if (!item.DaGiaiMaBoLoc)
            {
                item.ThoiGian = GiaiMaAnToan(item.ThoiGianRaw);
                item.TaiKhoan = GiaiMaAnToan(item.TaiKhoanRaw);
                item.DaGiaiMaBoLoc = true;
            }
            // Giải mã các cột còn lại để vẽ
            if (!item.DaGiaiMa)
            {
                item.TenMay = GiaiMaAnToan(item.TenMayRaw);
                item.IP = GetLocalIP();
                item.ID_CPU = GiaiMaAnToan(item.ID_CPURaw);
                item.HanhDong = GiaiMaAnToan(item.HanhDongRaw);
                item.GhiChu = GiaiMaAnToan(item.GhiChuRaw);
                string hd = item.HanhDong ?? "";
                if (hd.Contains("Đăng nhập")) item.IconType = 1;
                else if (hd.Contains("Dữ liệu") || hd.Contains("CSDL")) item.IconType = 2;
                else if (hd.Contains("Lưu") || hd.Contains("Thêm") || hd.Contains("Cập nhật")) item.IconType = 3;
                else if (hd.Contains("Xuất")) item.IconType = 4;
                else if (hd.Contains("Trợ giúp")) item.IconType = 5;
                else if (hd.Contains("Xóa")) item.IconType = 6;
                else if (hd.Contains("Cảnh báo") || hd.Contains("Lỗi")) item.IconType = 7;
                else item.IconType = 0;
                item.DaGiaiMa = true;
            }
            switch (e.ColumnIndex)
            {
                case 0: e.Value = item.ID; break;
                case 1: e.Value = item.ThoiGian; break;
                case 2: e.Value = item.TenMay; break;
                case 3: e.Value = item.IP; break;
                case 4: e.Value = item.ID_CPU; break;
                case 5: e.Value = item.TaiKhoan; break;
                case 6: e.Value = item.HanhDong; break;
                case 7: e.Value = item.GhiChu; break;
                case 8: e.Value = item.IconType; break;
            }
        }
        private void InitToolTips()
        {
            var toolTip_PhanTrang = new System.Windows.Forms.ToolTip
            {
                IsBalloon = true,
                ToolTipTitle = "Nhật ký phần mềm",
                ToolTipIcon = ToolTipIcon.Info,
                InitialDelay = 200,
                AutoPopDelay = 1200,
                ReshowDelay = 100,
                ShowAlways = true
            };
            var tips = new Dictionary<System.Windows.Forms.Control, string>
            {
                { kryptonButton_ApDungSoTrang, "Áp dụng số trang hiển thị" },
                { kryptonButton_TroLai, "Quay lại trang trước" },
                { kryptonButton_TiepTheo, "Chuyển sang trang tiếp theo" },
                { kryptonButton1_LocTheoNgayThangNam, "Lọc theo ngày tháng năm" },
                { kryptonButton1_LamMoiBoLoc, "Đặt lại bộ lọc" },
                { comboBox_LocTaiKhoan, "Chọn tài khoản đã từng đăng nhập sử dụng" }
            };
            foreach (var tip in tips)
            {
                if (tip.Key != null)
                    toolTip_PhanTrang.SetToolTip(tip.Key, tip.Value);
            }
        }
        private int _totalRecords = 0;     // Tổng số dòng dữ liệu trong Database
        private bool _isApplyingPage = false; // Cờ chặn click đúp ở cấp độ form
        private async void kryptonButton_TiepTheo_Click(object? sender, EventArgs e)
        {
            // 1. CHẶN CLICK ĐÚP VÀ KIỂM TRA ĐIỀU KIỆN
            if (_isPaging || _currentPage >= _totalPages) return;
            await ChuyenTrangThucThi(1); // +1 là tiến lên
        }
        private async void kryptonButton_TroLai_Click(object? sender, EventArgs e)
        {
            // 1. CHẶN CLICK ĐÚP VÀ KIỂM TRA ĐIỀU KIỆN
            if (_isPaging || _currentPage <= 1) return;
            await ChuyenTrangThucThi(-1); // -1 là lùi lại
        }
        // Hàm cốt lõi xử lý hiệu ứng UI và Logic khi chuyển trang
  
        // 2 SỰ KIỆN TỪ MENU CHUỘT PHẢI
        private void trangTiepTheo_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (kryptonButton_TiepTheo.Enabled) // Đảm bảo nút đang bật thì mới cho click
            {
                kryptonButton_TiepTheo.PerformClick();
            }
        }
        private void trangTroLai_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (kryptonButton_TroLai.Enabled)
            {
                kryptonButton_TroLai.PerformClick();
            }
        }
        private void KryptonDataGridView1_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv == null) return;
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgv.Columns[e.ColumnIndex].Name == "HanhDong")
            {
                // ⭐ BÍ QUYẾT: CHỈ VẼ NỀN (TỪ CHỐI VẼ BORDER ĐỂ ĐỒNG BỘ 100% TOÀN BẢNG)
                var paintParts = DataGridViewPaintParts.Background | DataGridViewPaintParts.SelectionBackground;
                // Xử lý dòng trống dưới cùng (nếu có)
                if (_listCurrentPage != null && e.RowIndex == _listCurrentPage.Count)
                {
                    e.Paint(e.CellBounds, paintParts);
                    e.Handled = true;
                    return;
                }
                // 1. TÔ NỀN SẠCH SẼ
                e.Paint(e.CellBounds, paintParts);
                // KHÔNG CÒN DRAWLINE TỰ TẠO NỮA, MỌI THỨ SẼ PHẲNG TỰ NHIÊN
                // 2. LOGIC VẼ ICON VÀ TEXT GỐC CỦA BẠN ĐƯỢC BẢO TOÀN
                string hanhDongText = e.Value?.ToString() ?? "";
                int iconType = 0;
                try
                {
                    if (dgv.Columns.Contains("IconType"))
                    {
                        var objIcon = dgv.Rows[e.RowIndex].Cells["IconType"].Value;
                        if (objIcon != null && objIcon.ToString() != "")
                            iconType = Convert.ToInt32(objIcon);
                    }
                }
                catch { }
                Image iconToDraw = iconType switch
                {
                    1 => _iconLogin,
                    2 => _iconDb,
                    3 => _iconSave,
                    4 => _iconExport,
                    5 => _iconHelp,
                    6 => _iconDelete,
                    7 => _iconWarning,
                    _ => _iconDefault
                };
                int iconSize = 16;
                int paddingLeft = 6;
                int paddingIconText = 6;
                int yIcon = e.CellBounds.Y + (e.CellBounds.Height - iconSize) / 2;
                int xIcon = e.CellBounds.X + paddingLeft;
                if (iconToDraw != null)
                {
                    e.Graphics.DrawImage(iconToDraw, new Rectangle(xIcon, yIcon, iconSize, iconSize));
                }
                if (!string.IsNullOrEmpty(hanhDongText))
                {
                    int textStartX = xIcon + iconSize + paddingIconText;
                    Rectangle textBounds = new Rectangle(textStartX, e.CellBounds.Y, e.CellBounds.Width - (textStartX - e.CellBounds.X), e.CellBounds.Height);
                    Color textColor = (e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected
                                      ? e.CellStyle.SelectionForeColor
                                      : e.CellStyle.ForeColor;
                    TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;
                    TextRenderer.DrawText(e.Graphics, hanhDongText, e.CellStyle.Font, textBounds, textColor, flags);
                }
                e.Handled = true;
            }
        }
        private void LoadComboBoxTaiKhoan()
        {
            comboBox_LocTaiKhoan.Items.Clear();
            comboBox_LocTaiKhoan.Items.Add(Module_HeThong.Tat_Ca);
            // 🔥 TỐI ƯU 4: Không chọc CSDL nữa, lấy luôn data từ RAM đã giải mã
            if (_listFull != null && _listFull.Count > 0)
            {
                var danhSachDaLoc = _listFull
                    .Select(x => x.TaiKhoan)
                    .Where(tk => !string.IsNullOrWhiteSpace(tk))
                    .Distinct(StringComparer.OrdinalIgnoreCase) // Distinct loại bỏ trùng lặp siêu tốc
                    .OrderBy(x => x)
                    .ToArray();
                comboBox_LocTaiKhoan.Items.AddRange(danhSachDaLoc);
            }
            if (comboBox_LocTaiKhoan.Items.Count > 0)
            {
                comboBox_LocTaiKhoan.SelectedIndex = 0;
            }
        }
        private void RadioSapXep_CheckedChanged(object? sender, EventArgs e)
        {
            if (!((RadioButton)sender).Checked) return;
            // 🔥 ANH THÊM DÒNG NÀY VÀO ĐỂ BÁO CHO BỘ NÃO TRUNG TÂM BIẾT NHÉ:
            _sortAsc = radioButton1_TuAZ.Checked;
            CapNhatMauRadioSapXep();
            CapNhatTrangThaiSapXep();
            ThucThiBoLocToanDienAsync();
        }
        private async void textBox_SoDongHienThi_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                try
                {
                    await ApDungSoTrang();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi áp dụng số trang: {ex.Message}");
                }
            }
        }
        private async Task HienThiCanhBaoSoTrang()
        {
            Color origBack = textBox_SoDongHienThi.StateCommon.Back.Color1;
            Color origFore = textBox_SoDongHienThi.StateCommon.Content.Color1;
            textBox_SoDongHienThi.StateCommon.Back.Color1 = Color.LightPink;
            textBox_SoDongHienThi.StateCommon.Content.Color1 = Color.DarkRed;
            kryptonButton_ApDungSoTrang.StateCommon.Back.Color1 = Color.LightCoral;
            kryptonButton_ApDungSoTrang.StateCommon.Content.ShortText.Color1 = Color.White;
            await Task.Delay(700);
            textBox_SoDongHienThi.StateCommon.Back.Color1 = origBack;
            textBox_SoDongHienThi.StateCommon.Content.Color1 = origFore;
            kryptonButton_ApDungSoTrang.StateCommon.Back.Color1 = Color.Empty;
            kryptonButton_ApDungSoTrang.StateCommon.Content.ShortText.Color1 = Color.Empty;
            textBox_SoDongHienThi.Text = PAGE_SIZE_DEFAULT.ToString();
        }
        private async Task HienThiThanhCongSoTrang()
        {
            Color origTxtBack = textBox_SoDongHienThi.BackColor;
            Color origTxtFore = textBox_SoDongHienThi.ForeColor;
            Color origBtnBack = kryptonButton_ApDungSoTrang.BackColor;
            Color origBtnFore = kryptonButton_ApDungSoTrang.ForeColor;
            textBox_SoDongHienThi.BackColor = Color.LightGreen;
            textBox_SoDongHienThi.ForeColor = Color.DarkGreen;
            kryptonButton_ApDungSoTrang.BackColor = Color.LightGreen;
            kryptonButton_ApDungSoTrang.ForeColor = Color.DarkGreen;
            await Task.Delay(700);
            textBox_SoDongHienThi.BackColor = origTxtBack;
            textBox_SoDongHienThi.ForeColor = origTxtFore;
            kryptonButton_ApDungSoTrang.BackColor = origBtnBack;
            kryptonButton_ApDungSoTrang.ForeColor = origBtnFore;
        }
        private void SetSubMenuFont(ToolStripItemCollection items, Font font)
        {
            foreach (ToolStripItem subItem in items)
            {
                subItem.Font = font;
                if (subItem is ToolStripMenuItem tsmi && tsmi.DropDownItems.Count > 0)
                    SetSubMenuFont(tsmi.DropDownItems, font);
            }
        }
        private async void xuatNhatKy_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            // 1. Chỉ xuất danh sách đã được lọc (hiển thị trên lưới)
            if (_listFiltered == null || _listFiltered.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu nhật ký để xuất.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            using var sfd = new SaveFileDialog
            {
                Title = "Chọn nơi lưu file Excel",
                InitialDirectory = desktopPath,
                // Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                Filter = "Excel Files (*.xlsx, *.xlsm)|*.xlsx;*.xlsm",
                FileName = $"NhatKyHoatDong_PhanMemThiDua2026_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                AddExtension = true,
                DefaultExt = "xlsx"
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;
            string fullPath = sfd.FileName;
            try
            {
                this.Cursor = Cursors.WaitCursor;
                toolStripStatusLabel2.Text = "Đang xử lý dữ liệu xuất...";
                // Copy list ra để tránh xung đột luồng UI
                var listExport = _listFiltered.ToList();
                string currentIP = GetLocalIP();
                // 2. GIẢI MÃ ĐA LUỒNG BẢO VỆ MÁY YẾU (Khóa 50% CPU)
                await Task.Run(() =>
                {
                    var options = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) };
                    Parallel.ForEach(listExport, options, item =>
                    {
                        if (!item.DaGiaiMa)
                        {
                            item.ThoiGian = GiaiMaAnToan(item.ThoiGianRaw);
                            item.TenMay = GiaiMaAnToan(item.TenMayRaw);
                            item.IP = currentIP;
                            item.ID_CPU = GiaiMaAnToan(item.ID_CPURaw);
                            item.TaiKhoan = GiaiMaAnToan(item.TaiKhoanRaw);
                            item.HanhDong = GiaiMaAnToan(item.HanhDongRaw);
                            item.GhiChu = GiaiMaAnToan(item.GhiChuRaw);
                            item.DaGiaiMa = true;
                        }
                    });
                });
                toolStripStatusLabel2.Text = "Đang ghi file Excel...";
                // 3. XUẤT EXCEL SIÊU TỐC VỚI CLOSEDXML (InsertData 1 chạm)
                await Task.Run(() =>
                {
                    using var wb = new XLWorkbook();
                    var ws = wb.Worksheets.Add("Nhật ký phần mềm");
                    // --- Vẽ Header ---
                    ws.Cell(1, 1).Value = "THỐNG KÊ";
                    ws.Range(1, 1, 1, 8).Merge().Style.Font.SetBold().Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    ws.Cell(2, 1).Value = $"NHẬT KÝ HOẠT ĐỘNG PHẦN MỀM THI ĐUA NĂM {DateTime.Now.Year}";
                    ws.Range(2, 1, 2, 8).Merge().Style.Font.SetBold().Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    // --- Vẽ Tiêu đề cột ---
                    string[] headers = { "ID", "Thời gian", "Tên máy", "IP Address", "ID CPU", "Tài khoản", "Hành động", "Ghi chú" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = ws.Cell(4, i + 1);
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    // --- ĐỔ DỮ LIỆU 1 CHẠM (Không dùng vòng lặp for) ---
                    var dataToInsert = listExport.Select(x => new { x.ID, x.ThoiGian, x.TenMay, x.IP, x.ID_CPU, x.TaiKhoan, x.HanhDong, x.GhiChu });
                    ws.Cell(5, 1).InsertData(dataToInsert);
                    // --- ĐỊNH DẠNG KHUNG 1 LẦN DUY NHẤT CHO TOÀN BỘ BẢNG ---
                    var tableRange = ws.Range(4, 1, listExport.Count + 4, 8);
                    tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    tableRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    // Canh trái riêng cho các cột ID, Tài khoản, Hành động, Ghi chú
                    ws.Range(5, 1, listExport.Count + 4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    ws.Range(5, 6, listExport.Count + 4, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    // --- Dòng Tổng cộng ---
                    int tongCongRow = listExport.Count + 5;
                    ws.Cell(tongCongRow, 1).Value = $"Tổng cộng: {listExport.Count} hành động./.";
                    ws.Range(tongCongRow, 1, tongCongRow, 8).Merge().Style.Font.SetBold().Font.SetItalic().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
                    // Căn chỉnh độ rộng cột
                    ws.Columns(1, 8).AdjustToContents();
                    Module_BanQuyen.DongDauExcel(wb);
                    wb.SaveAs(fullPath);
                });
                Module_XuatNhapDuLieuThiDua.MoVaChonTepTrongExplorer(fullPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xuất tệp Excel: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                toolStripStatusLabel2.Text = $"Tổng: {_listFiltered.Count:N0} hành động";
                toolStripStatusLabel2.ForeColor = Color.Black;
            }
        }
        private static class CryptoPrefixes
        {
            public const string AES_V2 = "AES:v2|";
            public const string AES_V4 = "AES:v4|";
        }
        private static string GiaiMaAnToan(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;
            try
            {
                string result = string.Empty;
                // Swich / If theo tiền tố thực tế hiển thị trên GridView
                if (value.StartsWith(Module_BaoMatAES.PREFIX_STEALTH_V4, StringComparison.Ordinal))
                {
                    // Truyền nguyên chuỗi "AES:v4|..." vào vì GiaiMa_NhatKy tự bóc tách
                    result = Module_BaoMatAES.GiaiMa_NhatKy(value);
                }
                else if (value.StartsWith(Module_BaoMatAES.PREFIX_STEALTH_V2, StringComparison.Ordinal))
                {
                    // Nếu hàm GiaiMa cũ không tự bóc "AES:v2|", ta cắt trước khi truyền
                    string payloadOnly = value.Substring(Module_BaoMatAES.PREFIX_STEALTH_V2.Length);
                    result = Module_BaoMatAES.GiaiMa(payloadOnly);
                }
                else
                {
                    // Dữ liệu thô chưa mã hóa -> Giữ nguyên
                    return value;
                }
                // Nếu giải mã thành công thì trả về kết quả, nếu lỗi trả về chuỗi mã hóa gốc
                return string.IsNullOrEmpty(result) ? value : result;
            }
            catch
            {
                return value;
            }
        }
        // 1. HÀM TỰ ĐỘNG XÓA (Đã sửa lỗi tên cột và chuỗi Tiếng Việt)  
        // Hàm công khai giúp Module bên ngoài nạp text vào thanh trạng thái một cách an toàn
        public void CapNhatVanBanStatusLabel(string vanBan)
        {
            if (toolStripStatusLabel1 != null)
            {
                toolStripStatusLabel1.Text = vanBan;
            }
        }
        // 1. HÀM TỰ ĐỘNG XÓA (Đã tối ưu lại bằng 1 câu lệnh SQLite duy nhất)
        private async Task TuDongXoaNhatKyNeuCanAsync()
        {
            try
            {
                _soDongDaXoaTuDong = 0;
                if (string.IsNullOrWhiteSpace(_csdl3Path) || !File.Exists(_csdl3Path)) return;
                string luaChon = "Không xóa";
                using (var cn = new SqliteConnection($"Data Source={_csdl3Path};Pooling=True;"))
                {
                    await cn.OpenAsync();
                    DamBaoBangTuDongXoaTonTai();
                    using var cmd = cn.CreateCommand();
                    // ĐÃ SỬA TÊN CỘT THÀNH "Chon_GiaTri" CHO KHỚP VỚI HÀM TẠO BẢNG
                    cmd.CommandText = "SELECT Chon_GiaTri FROM TuDong_XoaNhatKy WHERE ID = 1 LIMIT 1;";
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value) luaChon = result.ToString();
                }
                if (luaChon == "Không xóa")
                {
                    await Module_BaoVeDuLieu.KiemTraVaVaccumTheoSoDongAsync(Module_DanduongGPS.DuongDanCSDL3);
                    return;
                }
                int nguong = luaChon switch
                {
                    "1000 dòng xóa tự động" => 1000,
                    "5000 dòng xóa tự động" => 5000,
                    "10000 dòng xóa tự động" => 10000,
                    _ => 0
                };
                if (nguong > 0)
                {
                    // Gọi hàm thực thi SQL trực tiếp, bỏ qua bước đếm dòng
                    int daXoa = await Task.Run(() => ThucHienXoaVaGiuLai(nguong));
                    _soDongDaXoaTuDong = daXoa;
                    if (daXoa > 0)
                    {
                        Debug.WriteLine($"[TuDongXoa] Đã dọn dẹp {daXoa} dòng nhật ký cũ.");
                    }
                }
                await Module_BaoVeDuLieu.KiemTraVaVaccumTheoSoDongAsync(Module_DanduongGPS.DuongDanCSDL3);
            }
            catch (Exception ex) { Debug.WriteLine($"[TuDongXoa] Lỗi: {ex.Message}"); }
        }
        // Hàm lõi xử lý xóa gọn gàng bằng SQLite (Lấy đúng tên bảng NhatKyUngDung)
        private int ThucHienXoaVaGiuLai(int soDongMuonGiu)
        {
            try
            {
                using var cn = new SqliteConnection($"Data Source={_csdl3Path};Pooling=True;");
                cn.Open();
                using var tran = cn.BeginTransaction();
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tran;
                // Câu lệnh "Thần thánh": Xóa tất cả, ngoại trừ [soDongMuonGiu] dòng mới nhất
                cmd.CommandText = @"
            DELETE FROM NhatKyUngDung
            WHERE ID NOT IN (
                SELECT ID FROM NhatKyUngDung
                ORDER BY ID DESC
                LIMIT @Nguong
            );";
                cmd.Parameters.AddWithValue("@Nguong", soDongMuonGiu);
                int deleted = cmd.ExecuteNonQuery();
                tran.Commit();
                return deleted;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThucHienXoaVaGiuLai] Lỗi: {ex.Message}");
                return 0;
            }
        }
        private void thongKeTaiKhoanDaTungSuDungToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            if (formCha == null) return;
            var f = Application.OpenForms.OfType<Form25_ThongKeTaiKhoanDaSuDung>().FirstOrDefault();
            if (f == null)
            {
                f = new Form25_ThongKeTaiKhoanDaSuDung
                {
                    TopLevel = false,
                    FormBorderStyle = FormBorderStyle.None,
                    Dock = DockStyle.Fill
                };
                var panel = formCha.Controls.Find("PanelContainer", true).FirstOrDefault() as Panel;
                if (panel == null) return;
                panel.Controls.Add(f);
                f.Show();
                f.BringToFront();
            }
            else
            {
                f.BringToFront();
                // 🔥 GỌI HÀM NẠP LẠI DỮ LIỆU KHI FORM ĐÃ TỒN TẠI (Ép forceReload = true)
                // Lưu ý: Cần đổi từ khoá private -> public cho hàm LoadDuLieuTaiKhoanDaSuDungAsync ở Form25
                _ = f.LoadDuLieuTaiKhoanDaSuDungAsync(true);
            }
        }
        private void DamBaoBangTuDongXoaTonTai()
        {
            using var cn = new SqliteConnection($"Data Source={_csdl3Path}");
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS TuDong_XoaNhatKy (
            ID INTEGER PRIMARY KEY AUTOINCREMENT,
            Chon_GiaTri TEXT
        );";
            cmd.ExecuteNonQuery();
        }
        private async void xoaToanBoDuLieu_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            string dbPath = _csdl3Path;
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
            {
                MessageBox.Show("Không tìm thấy csdl3.db!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            using (Form24_XacMinhAdmin frm = new Form24_XacMinhAdmin())
            {
                frm.TopMost = true;
                frm.StartPosition = FormStartPosition.CenterScreen;
                if (frm.ShowDialog() != DialogResult.OK) return;
            }
            // ⭐ SỬA Ở ĐÂY: Dọn dẹp List thay vì DataTable
            if (_listFull != null)
            {
                _listFull.Clear();
                _listFiltered?.Clear();
                _listCurrentPage?.Clear();
                kryptonDataGridView1.RowCount = 0;
                kryptonDataGridView1.Refresh();
            }
            int soDongDaXoa = 0;
            toolStripStatusLabel2.Text = "Đang xóa dữ liệu...";
            toolStripStatusLabel2.ForeColor = Color.Orange;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                await Task.Run(() =>
                {
                    using (var conn = new SqliteConnection($"Data Source={dbPath}"))
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "PRAGMA foreign_keys = OFF;";
                            cmd.ExecuteNonQuery();
                        }
                        using (var tran = conn.BeginTransaction())
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tran;
                            cmd.CommandText = "DELETE FROM NhatKyUngDung;";
                            soDongDaXoa = cmd.ExecuteNonQuery();
                            cmd.CommandText = "DELETE FROM sqlite_sequence WHERE name='NhatKyUngDung';";
                            cmd.ExecuteNonQuery();
                            tran.Commit();
                        }
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "VACUUM;";
                            cmd.ExecuteNonQuery();
                        }
                    }
                });
                Module_NhatKy.GhiNhatKy(
                    Module_TaiKhoan.TenTaiKhoan_RAM,
                    $"Xóa toàn bộ nhật ký hệ thống chứa ({soDongDaXoa} hành động)",
                    DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")
                );
                MessageBox.Show($"Đã xóa {soDongDaXoa} dòng dữ liệu.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Gọi lại tải dữ liệu
                await ReloadDuLieuAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa dữ liệu:\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                toolStripStatusLabel2.ForeColor = Color.Black;
            }
        }
        private void CauHinhCangDeuStatusStrip()
        {
            if (IsDisposed || !IsHandleCreated || statusStrip1 == null)
                return;
            if (InvokeRequired)
            {
                BeginInvoke(CauHinhCangDeuStatusStrip);
                return;
            }
            try
            {
                statusStrip1.SuspendLayout();
                statusStrip1.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
                statusStrip1.GripStyle = ToolStripGripStyle.Hidden;
                if (toolStripStatusLabel1 != null)
                {
                    toolStripStatusLabel1.Spring = true;
                    toolStripStatusLabel1.TextAlign = ContentAlignment.MiddleLeft;
                    toolStripStatusLabel1.BorderSides = ToolStripStatusLabelBorderSides.None;
                    toolStripStatusLabel1.Margin = new Padding(5, 3, 0, 2);
                }
                if (toolStripStatusLabel4 != null)
                {
                    toolStripStatusLabel4.Spring = false;
                    toolStripStatusLabel4.TextAlign = ContentAlignment.MiddleCenter;
                    toolStripStatusLabel4.BorderSides = ToolStripStatusLabelBorderSides.None;
                    toolStripStatusLabel4.Margin = new Padding(0, 3, 10, 2);
                }
                if (toolStripStatusLabel2 != null)
                {
                    toolStripStatusLabel2.Spring = false;
                    toolStripStatusLabel2.TextAlign = ContentAlignment.MiddleCenter;
                    toolStripStatusLabel2.BorderSides = ToolStripStatusLabelBorderSides.None;
                    toolStripStatusLabel2.Margin = new Padding(10, 3, 0, 2);
                }
                if (toolStripStatusLabel3 != null)
                {
                    toolStripStatusLabel3.Spring = true;
                    toolStripStatusLabel3.TextAlign = ContentAlignment.MiddleRight;
                    toolStripStatusLabel3.BorderSides = ToolStripStatusLabelBorderSides.None;
                    toolStripStatusLabel3.Margin = new Padding(0, 3, 5, 2);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CauHinhStatusStrip] {ex.Message}");
            }
            finally
            {
                statusStrip1.ResumeLayout(true);
            }
        }
        private void CapNhatMauRadioSapXep()
        {
            radioButton1_TuAZ.ForeColor = radioButton1_TuAZ.Checked ? Color.Blue : Color.Red;
            radioButton1_TuZA.ForeColor = radioButton1_TuZA.Checked ? Color.Blue : Color.Red;
        }
        private void CapNhatTrangThaiSapXep()
        {
            if (toolStripStatusLabel3 == null || statusStrip1 == null) return;
            toolStripStatusLabel3.Visible = true;
            toolStripStatusLabel3.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripStatusLabel3.Spring = true;
            if (statusStrip1.LayoutStyle != ToolStripLayoutStyle.HorizontalStackWithOverflow)
                statusStrip1.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
            toolStripStatusLabel3.Text = radioButton1_TuAZ.Checked
                ? "             Sắp xếp: Cũ nhất trước (A → Z)"
                : radioButton1_TuZA.Checked
                    ? "             Sắp xếp: Mới nhất trước (Z → A)"
                    : "             Sắp xếp: Chưa rõ";
            if (toolStripStatusLabel3.Font != _fontStatus)
                toolStripStatusLabel3.Font = _fontStatus;
            toolStripStatusLabel3.TextAlign = ContentAlignment.MiddleRight;
            statusStrip1.PerformLayout();
        }
        private void thongTinNguoiDung_toolstrip_Click(object? sender, EventArgs e)
        {
            try
            {
                var frm = Form39_ThongTinNguoiDung.GetInstance();
                // 🛡️ Handle chưa tạo
                if (!frm.IsHandleCreated)
                {
                    frm.CreateControl();
                }
                // 🚀 Nếu đang minimize -> khôi phục
                if (frm.WindowState == FormWindowState.Minimized)
                {
                    frm.WindowState = FormWindowState.Normal;
                }
                // 🚀 Nếu đang ẩn -> hiện lại
                if (!frm.Visible)
                {
                    frm.Show();
                }
                // 🌟 Đưa lên trước
                frm.BringToFront();
                frm.Activate();
                frm.Focus();
            }
            catch (ObjectDisposedException)
            {
                try
                {
                    var frm = Form39_ThongTinNguoiDung.GetInstance();
                    frm.Show();
                    frm.Activate();
                }
                catch { }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThongTinNguoiDung] {ex}");
            }
        }
        private void kryptonButton1_LocTheoNgayThangNam_Click(object? sender, EventArgs e)
        {
            DateTime tuNgay = kryptonDateTimePicker1_NgayThangNamBatDau.Value.Date;
            DateTime denNgay = kryptonDateTimePicker1_NgayThangNamKetThuc.Value.Date;
            if (tuNgay > denNgay)
            {
                MessageBox.Show("Ngày bắt đầu không được lớn hơn ngày kết thúc!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _filterTuNgay = tuNgay;
            _filterDenNgay = denNgay;
            // Chỉ cần gọi đầu não trung tâm, nó sẽ tự xử lý luồng phụ và cập nhật UI mượt mà
            ThucThiBoLocToanDienAsync();
        }
        private void comboBox_LocTaiKhoan_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_dangNapComboBox) return;
            if (!_daKhoiTaoHoanTat) return;
            if (_dangLocDuLieu) return;
            string taiKhoanMoi = comboBox_LocTaiKhoan.SelectedItem?.ToString() ?? Module_HeThong.Tat_Ca;
            if (string.Equals(_filterTaiKhoan, taiKhoanMoi, StringComparison.OrdinalIgnoreCase)) return;
            _filterTaiKhoan = taiKhoanMoi;
            _currentPage = 1;
            CapNhatHienThiTaiKhoanLabel();
            ThucThiBoLocToanDienAsync();
        }
        private void comboBox1_MayTinh_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // 🛡️ Không xử lý trong lúc chương trình tự nạp ComboBox
            if (_dangNapComboBox) return;
            // 🛡️ Không xử lý khi Form chưa khởi tạo xong
            if (!_daKhoiTaoHoanTat) return;
            // 🛡️ Không cho re-entry
            if (_dangLocDuLieu) return;
            string mayTinhMoi = comboBox1_MayTinh.SelectedItem?.ToString() ?? Module_HeThong.Tat_Ca;
            // Không thay đổi thực sự -> bỏ qua
            if (string.Equals(_filterMayTinh, mayTinhMoi, StringComparison.OrdinalIgnoreCase)) return;
            _filterMayTinh = mayTinhMoi;
            _currentPage = 1;
            bool laTatCa = _filterMayTinh == Module_HeThong.Tat_Ca;
            toolStripStatusLabel3.Text = laTatCa ? "Sắp xếp: " + (_sortAsc ? "Cũ nhất trước (A → Z)" : "Mới nhất trước (Z → A)") : $"Đang lọc máy tính: {_filterMayTinh}";
            ThucThiBoLocToanDienAsync();
        }
        private void LoadComboBoxMayTinh()
        {
            if (comboBox1_MayTinh == null ||
                comboBox1_MayTinh.IsDisposed)
                return;
            if (_dangNapComboBox)
                return;
            _dangNapComboBox = true;
            try
            {
                comboBox1_MayTinh.SelectedIndexChanged -=
                    comboBox1_MayTinh_SelectedIndexChanged;
                comboBox1_MayTinh.BeginUpdate();
                comboBox1_MayTinh.Items.Clear();
                // Luôn có Module_HeThong.Tat_Ca
                comboBox1_MayTinh.Items.Add(Module_HeThong.Tat_Ca);
                if (_listFull != null && _listFull.Count > 0)
                {
                    var danhSachMay = _listFull
                        .Select(x => x.TenMay)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    Debug.WriteLine(
                        $"[Form10] _listFull = {_listFull.Count:N0} dòng");
                    Debug.WriteLine(
                        $"[Form10] Tên máy tìm được = {danhSachMay.Count:N0}");
                    foreach (var may in danhSachMay)
                    {
                        Debug.WriteLine(
                            $"[Form10] Máy: [{may}]");
                    }
                    if (danhSachMay.Count > 0)
                    {
                        comboBox1_MayTinh.Items.AddRange(
                            danhSachMay.Cast<object>().ToArray());
                    }
                }
                else
                {
                    Debug.WriteLine(
                        "[Form10] _listFull đang rỗng khi LoadComboBoxMayTinh()");
                }
                comboBox1_MayTinh.SelectedIndex = 0;
                _filterMayTinh = Module_HeThong.Tat_Ca;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[LoadComboBoxMayTinh] {ex}");
            }
            finally
            {
                comboBox1_MayTinh.EndUpdate();
                comboBox1_MayTinh.SelectedIndexChanged +=
                    comboBox1_MayTinh_SelectedIndexChanged;
                _dangNapComboBox = false;
            }
        } 
        private async void pictureBox1_Click(object? sender, EventArgs e)
        {
            // Gọi hàm giới thiệu từ Module_NhatKy
            Module_NhatKy.HienThiHuongDanFormNhatKy();
        }
        private int _filterVersion = 0;
        private void CapNhatTrangThaiNutPhanTrang()
        {
            if (IsDisposed || !IsHandleCreated) return;
            kryptonButton_TroLai.Enabled = !_isPaging && _currentPage > 1;
            kryptonButton_TiepTheo.Enabled = !_isPaging && _currentPage < _totalPages;
        }
        private void CapNhatPhanTrang()
        {
            _totalPages = (_listFiltered == null || _listFiltered.Count == 0)
                ? 1
                : Math.Max(1, (int)Math.Ceiling(_listFiltered.Count / (double)_pageSize));
            _currentPage = Math.Clamp(_currentPage, 1, _totalPages);
        }
        // Điểm duy nhất vẽ lại 1 trang + đồng bộ nút. Mọi luồng đều đi qua đây.
        private Task LoadPageAsync()
        {
            if (IsDisposed) return Task.CompletedTask;

            CapNhatPhanTrang(); // luôn tính lại tổng trang, tránh _currentPage lệch

            if (_listFiltered == null || _listFiltered.Count == 0)
            {
                _listCurrentPage = new List<NhatKyModel>();
                kryptonDataGridView1.RowCount = 0;
                label_Trang.Text = "Trang 0/0";
                toolStripStatusLabel2.Text = "Trang 0/0";
                toolStripStatusLabel4.Text = "Không tìm thấy dữ liệu phù hợp";
                CapNhatTrangThaiNutPhanTrang();
                return Task.CompletedTask;
            }

            int start = Math.Max(0, (_currentPage - 1) * _pageSize);
            int count = Math.Max(0, Math.Min(_pageSize, _listFiltered.Count - start));
            _listCurrentPage = _listFiltered.GetRange(start, count);

            kryptonDataGridView1.RowCount = _listCurrentPage.Count + 1; // +1 dòng trống cuối như thiết kế cũ
            kryptonDataGridView1.Invalidate();

            label_Trang.Text = $"Trang {_currentPage}/{_totalPages}";
            toolStripStatusLabel2.Text = $"Trang {_currentPage}/{_totalPages}";
            toolStripStatusLabel4.Text = $"             Tổng: {_listFiltered.Count:N0} hành động";

            CapNhatTrangThaiNutPhanTrang();
            return Task.CompletedTask;
        }
        private async Task ChuyenTrangThucThi(int step)
        {
            if (_isPaging) return;

            string textTiep = kryptonButton_TiepTheo.Values.Text;
            string textLui = kryptonButton_TroLai.Values.Text;

            try
            {
                _isPaging = true;
                kryptonButton_TiepTheo.Enabled = false;
                kryptonButton_TroLai.Enabled = false;
                kryptonButton_ApDungSoTrang.Enabled = false;

                toolStripStatusLabel2.Text = "Đang chuyển trang...";
                toolStripStatusLabel2.ForeColor = Color.Blue;
                if (step > 0) kryptonButton_TiepTheo.Values.Text = "...";
                else kryptonButton_TroLai.Values.Text = "...";

                await Task.Delay(100); // nhịp UX
                if (IsDisposed) return;

                _currentPage = Math.Clamp(_currentPage + step, 1, _totalPages); // chỉ cộng 1 lần
                await LoadPageAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChuyenTrang] {ex}");
                MessageBox.Show($"Lỗi khi chuyển trang: {ex.Message}", "Lỗi hệ thống",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isPaging = false; // hạ cờ TRƯỚC khi sync nút
                if (!IsDisposed)
                {
                    kryptonButton_TiepTheo.Values.Text = textTiep;
                    kryptonButton_TroLai.Values.Text = textLui;
                    kryptonButton_ApDungSoTrang.Enabled = true;
                    toolStripStatusLabel2.ForeColor = Color.Black;
                    CapNhatTrangThaiNutPhanTrang();
                }
            }
        }
        private async Task ApDungSoTrang()
        {
            if (_isApplyingPage || _isPaging) return; // cờ nằm TRONG hàm, nên Enter và nút đều được chặn
            _isApplyingPage = true;
            try
            {
                if (!int.TryParse(textBox_SoDongHienThi.Text.Trim(), out int size)
                    || size < PAGE_SIZE_MIN || size > PAGE_SIZE_MAX)
                {
                    await HienThiCanhBaoSoTrang();
                    size = PAGE_SIZE_DEFAULT;
                    textBox_SoDongHienThi.Text = size.ToString();
                }

                _pageSize = size;
                _currentPage = 1;
                await LoadPageAsync();
                await HienThiThanhCongSoTrang();
            }
            finally
            {
                _isApplyingPage = false;
            }
        }
        private async void kryptonButton_ApDungSoTrang_Click(object? sender, EventArgs e)
        {
            if (_isApplyingPage || _isPaging) return;

            string textBanDau = kryptonButton_ApDungSoTrang.Values.Text;
            Image anhBanDau = kryptonButton_ApDungSoTrang.Values.Image;
            try
            {
                kryptonButton_ApDungSoTrang.Enabled = false;
                kryptonButton_ApDungSoTrang.Values.Text = "Đang tải...";
                kryptonButton_ApDungSoTrang.Values.Image = null;
                await ApDungSoTrang();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải trang: " + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!IsDisposed)
                {
                    kryptonButton_ApDungSoTrang.Values.Text = textBanDau;
                    kryptonButton_ApDungSoTrang.Values.Image = anhBanDau;
                    kryptonButton_ApDungSoTrang.Enabled = true;
                }
            }
        }
        private async Task ThucThiBoLocToanDienAsync()
        {
            if (IsDisposed || !IsHandleCreated) return;

            int version = Interlocked.Increment(ref _filterVersion);

            // Chụp snapshot + tham số trên UI thread
            var snapshot = _listFull.ToList();
            string tk = _filterTaiKhoan;
            string mt = _filterMayTinh;
            bool asc = _sortAsc;
            DateTime? tuNgay = _filterTuNgay;
            DateTime? denNgay = _filterDenNgay;

            bool locTK = !string.IsNullOrWhiteSpace(tk) &&
                         !string.Equals(tk, Module_HeThong.Tat_Ca, StringComparison.OrdinalIgnoreCase);
            bool locMT = !string.IsNullOrWhiteSpace(mt) &&
                         !string.Equals(mt, Module_HeThong.Tat_Ca, StringComparison.OrdinalIgnoreCase);

            try
            {
                Cursor = Cursors.WaitCursor;
                kryptonButton_TroLai.Enabled = false;   // khóa nút trong lúc lọc
                kryptonButton_TiepTheo.Enabled = false;
                toolStripStatusLabel2.Text = "Đang xử lý dữ liệu...";
                toolStripStatusLabel2.ForeColor = Color.Blue;

                var ketQua = await Task.Run(() =>
                {
                    IEnumerable<NhatKyModel> query = snapshot;

                    if (locTK)
                        query = query.Where(x => string.Equals(x.TaiKhoan, tk, StringComparison.OrdinalIgnoreCase));

                    if (locMT)
                        query = query.Where(x => string.Equals(x.TenMay, mt, StringComparison.OrdinalIgnoreCase));

                    if (tuNgay.HasValue || denNgay.HasValue)
                    {
                        query = query.Where(x =>
                        {
                            if (!x.ThoiGianParsed.HasValue) return false;
                            var d = x.ThoiGianParsed.Value;
                            if (tuNgay.HasValue && d < tuNgay.Value.Date) return false;
                            if (denNgay.HasValue && d > denNgay.Value.Date) return false;
                            return true;
                        });
                    }

                    return (asc ? query.OrderBy(x => x.ID) : query.OrderByDescending(x => x.ID)).ToList();
                });

                // Có lần lọc mới hơn đã được kích hoạt -> bỏ kết quả cũ
                if (version != Volatile.Read(ref _filterVersion) || IsDisposed) return;

                _listFiltered = ketQua;
                _currentPage = 1;
                kryptonDataGridView1.RowCount = 0;
                await LoadPageAsync(); // tự tính lại tổng trang và sync nút

                _ = HieuUngKetQuaLoc(ketQua.Count, version); // không giữ khóa 900ms
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi bộ lọc] {ex}");
                if (!IsDisposed)
                {
                    toolStripStatusLabel2.Text = "Lỗi khi lọc dữ liệu!";
                    toolStripStatusLabel2.ForeColor = Color.Red;
                }
            }
            finally
            {
                // Chỉ lần lọc mới nhất mới dọn dẹp trạng thái
                if (version == Volatile.Read(ref _filterVersion) && !IsDisposed)
                {
                    Cursor = Cursors.Default;
                    CapNhatTrangThaiNutPhanTrang();
                }
            }
        }
        private async Task HieuUngKetQuaLoc(int soDongPhuHop, int version)
        {
            if (IsDisposed) return;

            bool coDuLieu = soDongPhuHop > 0;
            Color mau = coDuLieu ? Color.FromArgb(0, 140, 60) : Color.OrangeRed;

            toolStripStatusLabel2.Text = coDuLieu
                ? $"Đã lọc xong: {soDongPhuHop:N0} dòng phù hợp"
                : "⚠ Không có dòng nào phù hợp với bộ lọc";
            toolStripStatusLabel2.ForeColor = mau;
            toolStripStatusLabel4.ForeColor = mau;

            await Task.Delay(900);

            // Có lần lọc khác chen vào -> để lần đó tự phục hồi
            if (IsDisposed || version != Volatile.Read(ref _filterVersion)) return;

            toolStripStatusLabel2.Text = $"Trang {_currentPage}/{_totalPages}";
            toolStripStatusLabel2.ForeColor = Color.Black; // màu cố định, không "nhớ" màu cũ
            toolStripStatusLabel4.ForeColor = Color.Black;
        }
        private static void GiaiMaChoBoLoc(NhatKyModel item)
        {
            if (item.DaGiaiMaBoLoc) return;

            item.ThoiGian = GiaiMaAnToan(item.ThoiGianRaw);
            item.TaiKhoan = GiaiMaAnToan(item.TaiKhoanRaw);
            item.TenMay = GiaiMaAnToan(item.TenMayRaw);

            string tg = item.ThoiGian?.Trim();
            if (DateTime.TryParseExact(tg, _cacDinhDangNgay,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime dt))
                item.ThoiGianParsed = dt.Date;
            else if (DateTime.TryParse(tg, out DateTime dt2))
                item.ThoiGianParsed = dt2.Date;

            item.DaGiaiMaBoLoc = true;
        }
        // Chạy hoàn toàn ở luồng nền, trả về list MỚI để UI thread hoán đổi 1 lần
        private List<NhatKyModel> TaiVaGiaiMaNhatKy()
        {
            var dt = Module_NhatKy.LoadTatCaNhatKy();
            if (dt == null || dt.Rows.Count == 0) return new List<NhatKyModel>();

            var list = new List<NhatKyModel>(dt.Rows.Count);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new NhatKyModel
                {
                    ID = row["ID"] != DBNull.Value ? Convert.ToInt64(row["ID"]) : 0,
                    ThoiGianRaw = row["ThoiGian"]?.ToString() ?? "",
                    TenMayRaw = row["TenMay"]?.ToString() ?? "",
                    ID_CPURaw = row["ID_CPU"]?.ToString() ?? "",
                    TaiKhoanRaw = row["TaiKhoan"]?.ToString() ?? "",
                    HanhDongRaw = row["HanhDong"]?.ToString() ?? "",
                    GhiChuRaw = row["GhiChu"]?.ToString() ?? ""
                });
            }

            int maxThreads = Math.Max(1, Environment.ProcessorCount / 2);
            list.AsParallel().WithDegreeOfParallelism(maxThreads).ForAll(GiaiMaChoBoLoc);
            return list;
        }
        public async Task ReloadDuLieuAsync()
        {
            if (IsDisposed || !IsHandleCreated) return;

            // Gọi từ luồng khác -> chuyển về UI thread (không chờ kết quả)
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => _ = ReloadDuLieuAsync()));
                return;
            }

            // Đang reload -> đánh dấu cần chạy lại 1 lần nữa, KHÔNG làm mất yêu cầu
            if (Interlocked.Exchange(ref _reloadDangChay, 1) == 1)
            {
                _reloadChoXuLy = true;
                return;
            }

            try
            {
                do
                {
                    _reloadChoXuLy = false;
                    await ThucHienReloadMotLanAsync();
                }
                while (_reloadChoXuLy && !IsDisposed);
            }
            finally
            {
                Interlocked.Exchange(ref _reloadDangChay, 0);
                if (!IsDisposed)
                {
                    Cursor = Cursors.Default;
                    CapNhatTrangThaiNutPhanTrang();
                }
            }
        }
        private async Task ThucHienReloadMotLanAsync()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                toolStripStatusLabel2.Text = "Đang tải dữ liệu nhật ký...";
                toolStripStatusLabel2.ForeColor = Color.Blue;

                // 1. Bảo trì DB xong rồi mới đọc (không đọc song song với lúc xóa)
                await Task.Run(() => TuDongXoaNhatKyNeuCanAsync());
                if (IsDisposed) return;

                // 2. Đọc + giải mã ở luồng nền, tạo list mới
                var duLieuMoi = await Task.Run(() => TaiVaGiaiMaNhatKy());
                if (IsDisposed) return;

                // 3. Hoán đổi trên UI thread, nạp combo, reset bộ lọc, lọc 1 lần
                _listFull = duLieuMoi;
                LoadComboBoxTaiKhoan();
                LoadComboBoxMayTinh();
                DatLaiBoLocVeMacDinh();
                await ThucThiBoLocToanDienAsync();

                Module_NhatKy.DocVaNapStatusLabelForm10();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ReloadDuLieuAsync] {ex}");
                if (!IsDisposed)
                    MessageBox.Show($"Lỗi load dữ liệu nhật ký:\n{ex.Message}", "Lỗi hệ thống",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Một chỗ duy nhất reset trạng thái bộ lọc + UI. Dùng cờ _dangNapComboBox
        // để các handler bỏ qua, không cần gỡ/gắn event thủ công nữa.
        private void DatLaiBoLocVeMacDinh()
        {
            bool cu = _dangNapComboBox;
            _dangNapComboBox = true;
            try
            {
                _filterTuNgay = null;
                _filterDenNgay = null;
                _filterTaiKhoan = Module_HeThong.Tat_Ca;
                _filterMayTinh = Module_HeThong.Tat_Ca;
                _sortAsc = false; // Z → A: mới nhất trước

                if (comboBox_LocTaiKhoan.Items.Count > 0) comboBox_LocTaiKhoan.SelectedIndex = 0;
                if (comboBox1_MayTinh.Items.Count > 0) comboBox1_MayTinh.SelectedIndex = 0;

                radioButton1_TuAZ.Checked = false;
                radioButton1_TuZA.Checked = true;

                ThietLapKhoangNgayMacDinh();
            }
            finally
            {
                _dangNapComboBox = cu;
            }

            CapNhatMauRadioSapXep();
            CapNhatTrangThaiSapXep();
            CapNhatHienThiTaiKhoanLabel();
        }
        private async void lamMoi_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            try
            {
                // Reload tự reset bộ lọc, tự chống chạy chồng
                await ReloadDuLieuAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi làm mới: " + ex.Message);
                if (!IsDisposed)
                {
                    toolStripStatusLabel2.Text = "Lỗi khi làm mới dữ liệu!";
                    toolStripStatusLabel2.ForeColor = Color.Red;
                }
            }
        }
        private async void kryptonButton1_LamMoiBoLoc_Click(object? sender, EventArgs e)
        {
            if (_dangLamMoiBoLoc || Volatile.Read(ref _reloadDangChay) == 1) return;

            _dangLamMoiBoLoc = true;
            try
            {
                kryptonButton1_LamMoiBoLoc.Enabled = false;
                DatLaiBoLocVeMacDinh();
                await ThucThiBoLocToanDienAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi làm mới bộ lọc: " + ex.Message);
                MessageBox.Show($"Lỗi khi đặt lại bộ lọc: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!IsDisposed) kryptonButton1_LamMoiBoLoc.Enabled = true;
                _dangLamMoiBoLoc = false;
            }
        }
        public class NhatKyModel
        {
            public long ID { get; set; }
            public string ThoiGianRaw { get; set; }
            public string TenMayRaw { get; set; }
            public string ID_CPURaw { get; set; }
            public string TaiKhoanRaw { get; set; }
            public string HanhDongRaw { get; set; }
            public string GhiChuRaw { get; set; }
            public string ThoiGian { get; set; }
            public string TenMay { get; set; }
            public string IP { get; set; }
            public string ID_CPU { get; set; }
            public string TaiKhoan { get; set; }
            public string HanhDong { get; set; }
            public string GhiChu { get; set; }
            public int IconType { get; set; }
            public bool DaGiaiMa { get; set; } = false;
            public bool DaGiaiMaBoLoc { get; set; } = false; // THÊM CỜ NÀY
            public DateTime? ThoiGianParsed { get; set; }
        }
    }
}