using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;

namespace PhanMemThiDua2026
{
    public partial class Form10_NhatKy : Form
    {
        private readonly string _csdl3Path = Module_DanduongGPS.DuongDanCSDL3;
        private const int PAGE_SIZE_DEFAULT = 500;
        private const int PAGE_SIZE_MIN = 100;
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
        // Đổi các biến toàn cục DataTable thành List
        private List<NhatKyModel> _listFull = new List<NhatKyModel>();
        private List<NhatKyModel> _listFiltered = new List<NhatKyModel>();
        private List<NhatKyModel> _listCurrentPage = new List<NhatKyModel>();
        private bool _isPaging = false;
        private string _localIP = "";
        private DateTime? _filterTuNgay = null;
        private DateTime? _filterDenNgay = null;
        private string _filterTaiKhoan = "Tất cả";
        // Thêm biến này ở khu vực khai báo biến đầu class
        private string _chuoiTuDongXoa = "";
        private string GetLocalIP()
        {
            if (!string.IsNullOrEmpty(_localIP)) return _localIP;
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                _localIP = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString() ?? "127.0.0.1";
            }
            catch { _localIP = "127.0.0.1"; }
            return _localIP;
        }
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
            radioButton1_TuAZ.CheckedChanged -= RadioSapXep_CheckedChanged;
            radioButton1_TuAZ.CheckedChanged += RadioSapXep_CheckedChanged;
            radioButton1_TuZA.CheckedChanged -= RadioSapXep_CheckedChanged;
            radioButton1_TuZA.CheckedChanged += RadioSapXep_CheckedChanged;
            // ⭐ THÊM 2 DÒNG NÀY ĐỂ NÚT LỌC HOẠT ĐỘNG:
            kryptonButton1_LocTheoNgayThangNam.Click -= kryptonButton1_LocTheoNgayThangNam_Click;
            kryptonButton1_LocTheoNgayThangNam.Click += kryptonButton1_LocTheoNgayThangNam_Click;

            // Đăng ký sự kiện tự vẽ bảng
            kryptonDataGridView1.CellPainting -= KryptonDataGridView1_CellPainting;
            kryptonDataGridView1.CellPainting += KryptonDataGridView1_CellPainting;
        }
        private bool _isInit = false;
        private void Form10_Load(object sender, EventArgs e)
        {
            if (_isInit) return;
            _isInit = true;
            // 🌟 ĐỊNH DẠNG: Chỉ hiển thị Ngày/Tháng/Năm
            Module_MenuChuotPhai.TichHopGiaoDienXanhLa(contextMenuStrip1);
            kryptonDateTimePicker1_NgayThangNamBatDau.Format = DateTimePickerFormat.Custom;
            kryptonDateTimePicker1_NgayThangNamBatDau.CustomFormat = "dd/MM/yyyy";
            kryptonDateTimePicker1_NgayThangNamKetThuc.Format = DateTimePickerFormat.Custom;
            kryptonDateTimePicker1_NgayThangNamKetThuc.CustomFormat = "dd/MM/yyyy";
            textBox_SoDongHienThi.Text = "100";
            CauHinhCangDeuStatusStrip();
            CaiDatCot();
        }
        private void Form10_Shown(object sender, EventArgs e)
        {
            kryptonDataGridView1.VirtualMode = true;
            kryptonDataGridView1.CellValueNeeded -= KryptonDataGridView1_CellValueNeeded;
            kryptonDataGridView1.CellValueNeeded += KryptonDataGridView1_CellValueNeeded;

            toolStripStatusLabel1.Text = $"Tài khoản: {Module_TaiKhoan.TenTaiKhoan_RAM}";
            _ = Task.Run(() => CapNhatStatusLabelTuDongXoaNgam());

            // 🔥 THÊM TỪ KHÓA async VÀO ĐÂY ĐỂ HẾT LỖI COMPILER
            _ = Task.Run(async () =>
            {
                // Gọi hàm kiểm tra và tạo bảng an toàn dưới luồng ngầm
                string pathCSDLLog = _csdl3Path;
                await KiemTraVaTaoBangNhatKyAsync(pathCSDLLog);

                DamBaoBangTuDongXoaTonTai();
                Module_TaiKhoan.NapTaiKhoanTuCSDL();
            });

            // Kích nạp dữ liệu siêu tốc
            ReloadDuLieu();

            this.Focus();
            if (kryptonDataGridView1.RowCount > 0)
                kryptonDataGridView1.Focus();
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
            bool laTatCa = string.IsNullOrWhiteSpace(_filterTaiKhoan) || _filterTaiKhoan == "Tất cả";

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
            // Mặc định lùi lại 1 tháng
            DateTime ngayMotThangTruoc = DateTime.Now.AddMonths(-1);
            DateTime ngayHienTai = DateTime.Now;

            if (_listFull != null && _listFull.Count > 0)
            {
                // Lấy danh sách các ngày hợp lệ (đã được parsed thành công)
                var danhSachNgay = _listFull
                    .Where(x => x.ThoiGianParsed.HasValue)
                    .Select(x => x.ThoiGianParsed.Value.Date)
                    .ToList();

                if (danhSachNgay.Count > 0)
                {
                    DateTime ngayNhoNhat = danhSachNgay.Min();

                    // Nếu ngày nhỏ nhất là ngày hôm nay (trùng ngày hiện tại)
                    // hoặc dữ liệu chỉ toàn ngày hôm nay
                    if (ngayNhoNhat >= ngayHienTai.Date)
                    {
                        kryptonDateTimePicker1_NgayThangNamBatDau.Value = ngayMotThangTruoc;
                    }
                    else
                    {
                        kryptonDateTimePicker1_NgayThangNamBatDau.Value = ngayNhoNhat;
                    }
                }
                else
                {
                    // Trường hợp có dữ liệu nhưng không parse được ngày nào
                    kryptonDateTimePicker1_NgayThangNamBatDau.Value = ngayMotThangTruoc;
                }
            }
            else
            {
                // CSDL rỗng
                kryptonDateTimePicker1_NgayThangNamBatDau.Value = ngayMotThangTruoc;
            }

            // Ngày kết thúc luôn mặc định là hôm nay cho tiện
            kryptonDateTimePicker1_NgayThangNamKetThuc.Value = ngayHienTai;
        }
        public void ReloadDuLieu()
        {
            if (IsDisposed || !IsHandleCreated) return;
            try
            {
                this.Cursor = Cursors.WaitCursor;
                toolStripStatusLabel2.Text = "Đang tải dữ liệu thô...";

                // 1. Tác vụ DB nặng nề đẩy hết xuống Background, không await chặn UI
                _ = Task.Run(async () =>
                {
                    await TuDongXoaNhatKyNeuCanAsync();
                    await Module_BaoTriCSDL.KiemTraVaVaccumTheoSoDongAsync(Module_DanduongGPS.DuongDanCSDL3);
                });

                // 2. Nạp List thô từ SQLite (Mất chưa tới 5ms)
                LoadNhatKyLenDataGridView_SieuToc();

                if (this.IsDisposed) return;

                // 3. Hiển thị Lưới NGAY LẬP TỨC (Dữ liệu chưa lọc)
                _sortAsc = false;
                radioButton1_TuZA.Checked = true;
                _currentPage = 1;
                CapNhatPhanTrang();
                HienThiTrangHienTai();
                // Gọi nạp dữ liệu từ Module ngay khi mở Form10 lên
                Module_NhatKy.DocVaNapStatusLabelForm10();
                // 4. Bật tiến trình ngầm giải mã 1400 chuỗi AES để chuẩn bị cho Bộ Lọc
                _ = Task.Run(() => ChuanBiDuLieuBoLocNgam());
            }
            catch (Exception ex) { Debug.WriteLine("Reload Form10 lỗi: " + ex.Message); }
            finally { this.Cursor = Cursors.Default; }
        }
        private void ChuanBiDuLieuBoLocNgam()
        {
            try
            {
                if (this.IsHandleCreated)
                    this.Invoke(new Action(() => toolStripStatusLabel2.Text = "Hệ thống đang nạp bộ lọc..."));

                string[] cacDinhDangNgay = { "dd-MM-yyyy HH:mm:ss", "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss", "dd-MM-yyyy", "dd/MM/yyyy", "M/d/yyyy h:mm:ss tt" };
                int maxThreads = Math.Max(1, Environment.ProcessorCount / 2);

                _listFull.AsParallel().WithDegreeOfParallelism(maxThreads).ForAll(item =>
                {
                    if (!item.DaGiaiMaBoLoc)
                    {
                        item.ThoiGian = GiaiMaAnToan(item.ThoiGianRaw);
                        item.TaiKhoan = GiaiMaAnToan(item.TaiKhoanRaw);

                        if (DateTime.TryParseExact(item.ThoiGian.Trim(), cacDinhDangNgay, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime dtLog))
                            item.ThoiGianParsed = dtLog.Date;
                        else if (DateTime.TryParse(item.ThoiGian, out DateTime dtAuto))
                            item.ThoiGianParsed = dtAuto.Date;

                        item.DaGiaiMaBoLoc = true;
                    }
                });

                // Bơm data vào UI an toàn
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        ThietLapKhoangNgayMacDinh();
                        LoadComboBoxTaiKhoan();
                        toolStripStatusLabel2.Text = $"Sẵn sàng. Tổng: {_listFiltered.Count:N0} hành động";
                    }));
                }
            }
            catch (Exception ex) { Debug.WriteLine(ex.Message); }
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
                return;
            }
            int start = (_currentPage - 1) * _pageSize;
            _listCurrentPage = _listFiltered.Skip(start).Take(_pageSize).ToList();

            kryptonDataGridView1.RowCount = _listCurrentPage.Count + 1; // Kích nạp vẽ
            kryptonDataGridView1.Invalidate();

            label_Trang.Text = $"Trang {_currentPage}/{_totalPages}";
            toolStripStatusLabel2.Text = $"Trang {_currentPage}/{_totalPages}";
            toolStripStatusLabel4.Text = $"Tổng: {_listFiltered.Count:N0} hành động";
        }
        private async Task LoadPageAsync()
        {
            if (_listFiltered == null || _listFiltered.Count == 0)
            {
                kryptonDataGridView1.RowCount = 0;
                label_Trang.Text = "Trang 0/0";
                toolStripStatusLabel2.Text = "Trang 0/0";
                return;
            }

            int start = (_currentPage - 1) * _pageSize;
            _listCurrentPage = _listFiltered.Skip(start).Take(_pageSize).ToList();

            // 🔥 TỐI ƯU 3: BỎ HẲN hàm ChuanHoaVaGiaiMaTrangHienTai() ở đây.
            // Không giải mã 500 dòng một lúc nữa. Nhường việc đó cho CellValueNeeded.

            if (this.IsDisposed) return;

            // Gán RowCount để Grid bắt đầu chu trình vẽ (Invalidate)
            kryptonDataGridView1.RowCount = _listCurrentPage.Count + 1;
            kryptonDataGridView1.Invalidate();

            label_Trang.Text = $"Trang {_currentPage}/{_totalPages}";
            toolStripStatusLabel2.Text = $"Trang {_currentPage}/{_totalPages}";
            toolStripStatusLabel4.Text = $"Tổng: {_listFiltered.Count:N0} hành động";
        }
        private void CaiDatCot()
        {
            var dgv = kryptonDataGridView1;

            if (_daCaiDatCot || dgv == null || dgv.IsDisposed)
                return;

            dgv.SuspendLayout();

            try
            {
                // ===================== GRID CONFIG =====================
                CauHinhGridCoBan(dgv);

                // ===================== COLUMN =====================
                dgv.Columns.Clear();
                TaoCot(dgv);

                // ===================== STYLE =====================
                CauHinhStyle(dgv);

                _daCaiDatCot = true;
            }
            finally
            {
                dgv.ResumeLayout(true); // true = refresh layout an toàn hơn false
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

            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            // ⭐ FIX LỖI CẮT DÒNG CUỐI (PIXEL CLIPPING) ⭐
            dgv.ScrollBars = ScrollBars.Both; // Ép WinForms luôn chừa chỗ cho Scrollbar, tránh hiện tượng nhấp nháy tính toán lại
            dgv.BorderStyle = BorderStyle.None; // Bỏ viền ngoài cùng gây sai lệch 1-2 pixel
            dgv.RowTemplate.Height = 30; // BẮT BUỘC có chiều cao dòng cố định để VirtualMode tính toán đúng 100%

            // Giảm từ 70 xuống 40 để ôm sát gọn gàng với Font chữ 10F
            dgv.ColumnHeadersHeight = 40;

            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
        }
        private void TaoCot(DataGridView dgv)
        {
            AddCol(dgv, "ID", "STT", 5);
            AddCol(dgv, "ThoiGian", "Thời gian", 14);
            AddCol(dgv, "TenMay", "Tên máy", 12);
            AddCol(dgv, "IP", "IP Address", 10);
            AddCol(dgv, "ID_CPU", "ID My Computer", 18);
            AddCol(dgv, "TaiKhoan", "Tài khoản", 12);
            AddCol(dgv, "HanhDong", "Hành động", 15, DataGridViewContentAlignment.MiddleLeft);
            AddCol(dgv, "GhiChu", "Ghi chú", 24, DataGridViewContentAlignment.MiddleLeft);

            var colIcon = new DataGridViewTextBoxColumn
            {
                Name = "IconType",
                HeaderText = "IconType",
                Visible = false
            };

            dgv.Columns.Add(colIcon);
        }
        private void AddCol(
    DataGridView dgv,
    string name,
    string header,
    float fillWeight,
    DataGridViewContentAlignment align = DataGridViewContentAlignment.MiddleCenter)
        {
            var col = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = align
                },
                FillWeight = fillWeight
            };

            dgv.Columns.Add(col);
        }
        private void CauHinhStyle(DataGridView dgv)
        {
            dgv.RowsDefaultCellStyle.BackColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(230, 245, 255);
            dgv.GridColor = Color.LightGray;

            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
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

                string[] cacDinhDangNgay = { "dd-MM-yyyy HH:mm:ss", "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss", "dd-MM-yyyy", "dd/MM/yyyy", "M/d/yyyy h:mm:ss tt" };

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
        private void KryptonDataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
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
                { comboBox_LocTaiKhoan, "Chọn tài khoản đã từng đăng nhập sử dụng" }
            };

            foreach (var tip in tips)
            {
                if (tip.Key != null)
                    toolTip_PhanTrang.SetToolTip(tip.Key, tip.Value);
            }
        }
        private bool _isApplyingPage = false; // Cờ chặn click đúp ở cấp độ form
        private async void kryptonButton_ApDungSoTrang_Click(object sender, EventArgs e)
        {
            if (_isApplyingPage) return; // Đang chạy thì chặn luôn, không cho bấm tiếp

            // 1. LƯU LẠI TRẠNG THÁI GỐC
            string textBanDau = kryptonButton_ApDungSoTrang.Values.Text;
            Image anhBanDau = kryptonButton_ApDungSoTrang.Values.Image;

            try
            {
                _isApplyingPage = true;

                // 2. THIẾT LẬP GIAO DIỆN "ĐANG TẢI"
                kryptonButton_ApDungSoTrang.Enabled = false; // Khóa phần cứng
                kryptonButton_ApDungSoTrang.Values.Text = "Đang tải..."; // Báo cho user biết
                kryptonButton_ApDungSoTrang.Values.Image = null;

                // Nhịp nghỉ UX (150ms là đủ để mắt kịp nhìn thấy chữ "Đang tải...")
                await Task.Delay(150);

                // 3. GỌI HÀM XỬ LÝ LOGIC CHÍNH
                await ApDungSoTrang();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải trang: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 4. KHÔI PHỤC GIAO DIỆN CHUẨN (Dù thành công hay văng lỗi)
                kryptonButton_ApDungSoTrang.Values.Text = textBanDau;
                kryptonButton_ApDungSoTrang.Values.Image = anhBanDau;
                kryptonButton_ApDungSoTrang.Enabled = true;
                _isApplyingPage = false;
            }
        }
        private async Task ApDungSoTrang()
        {
            int size = PAGE_SIZE_DEFAULT;

            // 1. VALIDATE DỮ LIỆU ĐẦU VÀO
            if (!int.TryParse(textBox_SoDongHienThi.Text.Trim(), out size) || size < PAGE_SIZE_MIN || size > PAGE_SIZE_MAX)
            {
                await HienThiCanhBaoSoTrang();
                size = PAGE_SIZE_DEFAULT;
                textBox_SoDongHienThi.Text = size.ToString();
            }

            // 2. CẬP NHẬT BIẾN TOÀN CỤC
            _pageSize = size;
            _currentPage = 1; // Đổi size thì bắt buộc phải dội về trang 1

            // 3. THỰC THI TẢI DỮ LIỆU (Cập nhật logic List)
            CapNhatPhanTrang();
            await LoadPageAsync();

            // 4. BÁO CÁO THÀNH CÔNG
            await HienThiThanhCongSoTrang();
        }
        private void CapNhatPhanTrang()
        {
            // Đếm trang dựa trên List thay vì DataTable
            _totalPages = _listFiltered == null || _listFiltered.Count == 0
                ? 1
                : Math.Max(1, (int)Math.Ceiling(_listFiltered.Count / (double)_pageSize));

            _currentPage = Math.Clamp(_currentPage, 1, _totalPages);
        }
        private async void kryptonButton_TiepTheo_Click(object sender, EventArgs e)
        {
            // 1. CHẶN CLICK ĐÚP VÀ KIỂM TRA ĐIỀU KIỆN
            if (_isPaging || _currentPage >= _totalPages) return;

            await ChuyenTrangThucThi(1); // +1 là tiến lên
        }
        private async void kryptonButton_TroLai_Click(object sender, EventArgs e)
        {
            // 1. CHẶN CLICK ĐÚP VÀ KIỂM TRA ĐIỀU KIỆN
            if (_isPaging || _currentPage <= 1) return;

            await ChuyenTrangThucThi(-1); // -1 là lùi lại
        }
        // Hàm cốt lõi xử lý hiệu ứng UI và Logic khi chuyển trang
        private async Task ChuyenTrangThucThi(int step)
        {
            // Lưu trạng thái nút cũ để khôi phục
            var textTiep = kryptonButton_TiepTheo.Values.Text;
            var textLui = kryptonButton_TroLai.Values.Text;

            try
            {
                _isPaging = true;

                // Tắt nút để chặn người dùng thao tác trong lúc đang load
                kryptonButton_TiepTheo.Enabled = false;
                kryptonButton_TroLai.Enabled = false;
                kryptonButton_ApDungSoTrang.Enabled = false; // Tạm khóa luôn ô áp dụng trang

                // Cập nhật trạng thái thanh Status (Để người dùng biết hệ thống đang lấy data)
                toolStripStatusLabel2.Text = "Đang chuyển trang...";
                toolStripStatusLabel2.ForeColor = Color.Blue;

                // Tùy theo hướng (tiến hay lùi) mà đổi text của nút tương ứng
                if (step > 0) kryptonButton_TiepTheo.Values.Text = "...";
                else kryptonButton_TroLai.Values.Text = "...";

                // Nhịp nghỉ UX (Rất quan trọng để UI không bị "giật cụng" khi bấm)
                await Task.Delay(100);

                // Xử lý logic
                _currentPage += step;
                await LoadPageAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi chuyển trang: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Khôi phục mọi thứ bất kể thành công hay thất bại
                kryptonButton_TiepTheo.Values.Text = textTiep;
                kryptonButton_TroLai.Values.Text = textLui;

                // Bật lại các nút (chỉ bật nút Tiếp/Lùi nếu nó chưa chạm giới hạn)
                kryptonButton_TiepTheo.Enabled = _currentPage < _totalPages;
                kryptonButton_TroLai.Enabled = _currentPage > 1;
                kryptonButton_ApDungSoTrang.Enabled = true;

                // Khôi phục màu status
                toolStripStatusLabel2.ForeColor = Color.Black;

                _isPaging = false;
            }
        }
        // 2 SỰ KIỆN TỪ MENU CHUỘT PHẢI
        private void trangTiepTheo_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (kryptonButton_TiepTheo.Enabled) // Đảm bảo nút đang bật thì mới cho click
            {
                kryptonButton_TiepTheo.PerformClick();
            }
        }
        private void trangTroLai_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (kryptonButton_TroLai.Enabled)
            {
                kryptonButton_TroLai.PerformClick();
            }
        }
        private void KryptonDataGridView1_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv == null) return;

            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgv.Columns[e.ColumnIndex].Name == "HanhDong")
            {
                // ⭐ BỎ QUA DÒNG TRỐNG: Chỉ vẽ nền chuẩn, không vẽ Icon hay Text
                if (_listCurrentPage != null && e.RowIndex == _listCurrentPage.Count)
                {
                    e.Paint(e.CellBounds, DataGridViewPaintParts.All);
                    e.Handled = true;
                    return;
                }

                // 1. TÔ NỀN VÀ VIỀN THEO CHUẨN, KHÔNG VẼ TEXT
                e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);

                string hanhDongText = e.Value?.ToString() ?? "";
                int iconType = 0;

                // 2. LẤY ICONTYPE TRONG VIRTUAL MODE
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

                // 3. VẼ ICON
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
            comboBox_LocTaiKhoan.Items.Add("Tất cả");

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
        private void RadioSapXep_CheckedChanged(object sender, EventArgs e)
        {
            if (!((RadioButton)sender).Checked) return;
            // 🔥 ANH THÊM DÒNG NÀY VÀO ĐỂ BÁO CHO BỘ NÃO TRUNG TÂM BIẾT NHÉ:
            _sortAsc = radioButton1_TuAZ.Checked;
            CapNhatMauRadioSapXep();
            CapNhatTrangThaiSapXep();
            ThucThiBoLocToanDienAsync();
        }
        private async void textBox_SoDongHienThi_KeyDown(object sender, KeyEventArgs e)
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
        private async void xuatNhatKy_ToolStripMenuItem_Click(object sender, EventArgs e)
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
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"Nhật ký Phần mềm Thi đua 2026 - {DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
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

                    ws.Cell(2, 1).Value = $"LỊCH SỬ TRUY CẬP PHẦN MỀM THI ĐUA NĂM {DateTime.Now.Year}";
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
        private string GiaiMaAnToan(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            try
            {
                // Gọi module V2
                string result = BaoMatAES.GiaiMa(value);

                // Nếu giải mã lỗi (trả về Empty), giữ nguyên chuỗi mã hóa để Admin còn biết là có dữ liệu
                return string.IsNullOrEmpty(result) ? value : result;
            }
            catch
            {
                return value;
            }
        }
        // 1. HÀM TỰ ĐỘNG XÓA (Đã sửa lỗi tên cột và chuỗi Tiếng Việt)
        private async Task TuDongXoaNhatKyNeuCanAsync()
        {
            try
            {
                _soDongDaXoaTuDong = 0;
                if (string.IsNullOrWhiteSpace(_csdl3Path) || !File.Exists(_csdl3Path)) return;

                string luaChon = "Không xóa"; // Sửa thành tiếng Việt có dấu
                using (var cn = new SqliteConnection($"Data Source={_csdl3Path};Pooling=True;"))
                {
                    await cn.OpenAsync();
                    DamBaoBangTuDongXoaTonTai();
                    using var cmd = cn.CreateCommand();

                    // SỬA LỖI 1: Tên cột phải khớp chính xác với lúc Create Table là Chọn_GiaiTri
                    cmd.CommandText = "SELECT Chọn_GiaiTri FROM TuDong_XoaNhatKy WHERE ID = 1 LIMIT 1;";
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value) luaChon = result.ToString();
                }

                if (luaChon == "Không xóa")
                {
                    await Module_BaoTriCSDL.KiemTraVaVaccumTheoSoDongAsync(Module_DanduongGPS.DuongDanCSDL3);
                    return;
                }

                // SỬA LỖI 2: Chuỗi switch/case phải CÓ DẤU y hệt như giá trị trong ComboBox
                int nguong = luaChon switch
                {
                    "1000 dòng xóa tự động" => 1000,
                    "5000 dòng xóa tự động" => 5000,
                    "10000 dòng xóa tự động" => 10000,
                    _ => 0
                };

                int soDongCanXoa = TinhSoDongCanXoa(nguong);
                if (soDongCanXoa > 0)
                {
                    int daXoa = await Task.Run(() => XoaNhatKyAnToan(soDongCanXoa));
                    _soDongDaXoaTuDong = daXoa;
                }

                await Module_BaoTriCSDL.KiemTraVaVaccumTheoSoDongAsync(Module_DanduongGPS.DuongDanCSDL3);
            }
            catch (Exception ex) { Debug.WriteLine($"[TuDongXoa] {ex.Message}"); }
        }
        // 2. HÀM CẬP NHẬT TRẠNG THÁI GÓC DƯỚI (Đã sửa lỗi tên cột và chuỗi Tiếng Việt)
        // Hàm công khai giúp Module bên ngoài nạp text vào thanh trạng thái một cách an toàn
        public void CapNhatVanBanStatusLabel(string vanBan)
        {
            if (toolStripStatusLabel1 != null)
            {
                toolStripStatusLabel1.Text = vanBan;
            }
        }
        // 3. HÀM ĐẾM SỐ DÒNG (Đã sửa tên bảng chuẩn)
        private static int DemSoDongNhatKy()
        {
            string dbPath = Module_DanduongGPS.DuongDanCSDL3;
            using var cn = new SqliteConnection($"Data Source={dbPath}");
            cn.Open();

            using var cmd = cn.CreateCommand();
            // SỬA LỖI 3: Tên bảng phải là NhatKyUngDung (Khớp với cấu trúc của bác ở hàm XoaToanBo)
            cmd.CommandText = "SELECT COUNT(*) FROM NhatKyUngDung";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
        // 4. HÀM THỰC THI XÓA (Đã sửa tên bảng chuẩn)
        public static int XoaNhatKyAnToan(int soDongCanXoa)
        {
            if (soDongCanXoa <= 0) return 0;

            string dbPath = Module_DanduongGPS.DuongDanCSDL3;
            using var cn = new SqliteConnection($"Data Source={dbPath}");
            cn.Open();

            using var tran = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tran;

                // SỬA LỖI 3: Dùng đúng tên bảng NhatKyUngDung
                cmd.CommandText = @"
        DELETE FROM NhatKyUngDung
        WHERE ID IN (
            SELECT ID FROM NhatKyUngDung
            ORDER BY ID ASC
            LIMIT @limit
        )";
                cmd.Parameters.AddWithValue("@limit", soDongCanXoa);

                int deleted = cmd.ExecuteNonQuery();
                tran.Commit();
                return deleted;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }
        private static int TinhSoDongCanXoa(int nguongToiDa)
        {
            int tongDong = DemSoDongNhatKy();
            if (tongDong <= nguongToiDa) return 0;
            return tongDong - nguongToiDa;
        }
        private void thongKeTaiKhoanDaTungSuDungToolStripMenuItem_Click(object sender, EventArgs e)
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
        private async void xoaToanBoDuLieu_ToolStripMenuItem_Click(object sender, EventArgs e)
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
                ReloadDuLieu();
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
            // 1. CHỐNG CRASH TẦNG 1: Đảm bảo Form và StatusStrip đang tồn tại
            if (IsDisposed || !IsHandleCreated || statusStrip1 == null) return;

            // 2. BẢO VỆ ĐA LUỒNG (Cross-thread): Dùng BeginInvoke mượt hơn Invoke vì không ép chờ
            if (InvokeRequired)
            {
                BeginInvoke(new Action(CauHinhCangDeuStatusStrip));
                return;
            }

            try
            {
                // Khóa vẽ giao diện tạm thời để thiết lập thông số -> Tối ưu CPU/GPU, chống chớp giật
                statusStrip1.SuspendLayout();

                // Đảm bảo StatusStrip xếp ngang chuẩn
                statusStrip1.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
                statusStrip1.GripStyle = ToolStripGripStyle.Hidden; // Giấu cái cục chấm chấm kéo thả ở mép phải (nếu có) cho gọn

                // ---------------------------------------------------------
                // GÓC TRÁI: Tài khoản 
                // -> LÒ XO SỐ 1 (Spring = true): Đẩy nhóm giữa sang phải
                // ---------------------------------------------------------
                if (toolStripStatusLabel1 != null)
                {
                    toolStripStatusLabel1.Spring = true;
                    toolStripStatusLabel1.TextAlign = ContentAlignment.MiddleLeft;
                    toolStripStatusLabel1.BorderSides = ToolStripStatusLabelBorderSides.None;
                    toolStripStatusLabel1.Margin = new Padding(5, 3, 0, 2); // Đệm trái xíu cho đỡ sát mí Form
                }

                // ---------------------------------------------------------
                // KHÚC GIỮA 1: Tổng số hành động
                // ---------------------------------------------------------
                if (toolStripStatusLabel4 != null)
                {
                    toolStripStatusLabel4.Spring = false;
                    toolStripStatusLabel4.TextAlign = ContentAlignment.MiddleCenter;
                    toolStripStatusLabel4.BorderSides = ToolStripStatusLabelBorderSides.None;
                    toolStripStatusLabel4.Margin = new Padding(0, 3, 10, 2); // Đệm phải 10px để tách xa số trang
                }

                // ---------------------------------------------------------
                // KHÚC GIỮA 2: Trang hiện tại
                // ---------------------------------------------------------
                if (toolStripStatusLabel2 != null)
                {
                    toolStripStatusLabel2.Spring = false;
                    toolStripStatusLabel2.TextAlign = ContentAlignment.MiddleCenter;
                    toolStripStatusLabel2.BorderSides = ToolStripStatusLabelBorderSides.None;
                    toolStripStatusLabel2.Margin = new Padding(10, 3, 0, 2); // Đệm trái 10px để tách xa tổng số
                }

                // ---------------------------------------------------------
                // GÓC PHẢI: Trạng thái sắp xếp (A-Z hay Z-A)
                // -> LÒ XO SỐ 2 (Spring = true): Đẩy nhóm giữa sang trái
                // Kết quả: Lò xo 1 và Lò xo 2 sẽ cân bằng, ép Tổng số & Trang nằm chễm chệ ngay tâm Form
                // ---------------------------------------------------------
                if (toolStripStatusLabel3 != null)
                {
                    toolStripStatusLabel3.Spring = true;
                    toolStripStatusLabel3.TextAlign = ContentAlignment.MiddleRight;
                    toolStripStatusLabel3.BorderSides = ToolStripStatusLabelBorderSides.None;
                    toolStripStatusLabel3.Margin = new Padding(0, 3, 5, 2); // Đệm phải 5px cho vừa vặn
                }
            }
            catch (Exception ex)
            {
                // Bắt lỗi ngầm để Admin xem lúc debug, không văng hộp thoại quấy rầy user
                System.Diagnostics.Debug.WriteLine($"[CauHinhStatusStrip] Lỗi: {ex.Message}");
            }
            finally
            {
                // 3. CHỐNG TREO GIAO DIỆN: Bắt buộc nhả SuspendLayout dù code chạy thành công hay báo lỗi
                // Truyền tham số 'true' để ép StatusStrip tính toán lại kích thước và bơm mực vẽ ngay lập tức.
                // Khỏi cần gọi statusStrip1.Refresh() tốn thêm chu kỳ CPU.
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

            // 1. PHÁ GIẢI CÁC BẪY "TÀNG HÌNH" TỪ DESIGNER & WINFORMS
            toolStripStatusLabel3.Visible = true;
            toolStripStatusLabel3.DisplayStyle = ToolStripItemDisplayStyle.Text; // Bắt buộc vẽ Text
            toolStripStatusLabel3.Spring = true; // Ép bung hết không gian thừa

            // Đảm bảo StatusStrip đang dùng đúng Layout để Spring có tác dụng
            statusStrip1.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;

            // 2. GÁN TEXT VÀ MÀU SẮC
            if (radioButton1_TuAZ.Checked)
            {
                toolStripStatusLabel3.Text = "Sắp xếp: Cũ nhất trước (A → Z)";
            }
            else if (radioButton1_TuZA.Checked)
            {
                toolStripStatusLabel3.Text = "Sắp xếp: Mới nhất trước (Z → A)";
            }
            else
            {
                toolStripStatusLabel3.Text = "Sắp xếp: Chưa rõ";
            }

            // 3. FONT CHUẨN (Gán lại font an toàn, tránh lỗi Dispose ngầm)
            toolStripStatusLabel3.Font = new Font("Segoe UI", 9F);
            toolStripStatusLabel3.TextAlign = ContentAlignment.MiddleRight;

            // 4. ÉP UI TÍNH TOÁN VÀ VẼ LẠI NGAY LẬP TỨC
            statusStrip1.PerformLayout();
            statusStrip1.Refresh();
        }
        private async void lamMoi_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 1. CHỐNG RE-ENTRY: Nếu đang xử lý thì không cho bấm tiếp
            if (_isPaging) return;

            try
            {
                _isPaging = true;
                this.Cursor = Cursors.WaitCursor;
                toolStripStatusLabel2.Text = "Đang làm mới...";

                // --- BƯỚC 1: RESET BỘ LỌC VÀ GIAO DIỆN ---
                _filterTuNgay = null;
                _filterDenNgay = null;
                _filterTaiKhoan = "Tất cả";

                // Đưa DateTimePicker về mặc định (Lưu ý: Luồng UI)
                kryptonDateTimePicker1_NgayThangNamBatDau.Value = DateTime.Now;
                kryptonDateTimePicker1_NgayThangNamKetThuc.Value = DateTime.Now;

                // --- BƯỚC 2: THỰC THI LOGIC NGẦM ---
                // ✅ GỌI ĐÚNG HÀM ASYNC: Tự động dọn dẹp nhật ký và chạy Vacuum nếu đạt mốc 1000 dòng
                // Không cần bọc Task.Run vì bản thân hàm này đã xử lý Task.Run bên trong nó rồi.
                await TuDongXoaNhatKyNeuCanAsync();

                // ✅ NẠP LẠI DỮ LIỆU VÀO RAM: Giải mã AES (Parallel) chạy trên luồng phụ để không lag UI
                await Task.Run(() => LoadNhatKyLenDataGridView());

                // --- BƯỚC 3: CẬP NHẬT GIAO DIỆN ---
                // ⭐ CHỐNG CRASH: Kiểm tra Form còn tồn tại không sau các lệnh await kéo dài
                if (this.IsDisposed || !this.IsHandleCreated) return;

                // Nạp danh sách tài khoản vào ComboBox từ dữ liệu RAM mới nhất
                LoadComboBoxTaiKhoan();

                // Tính toán lại tổng số trang
                CapNhatPhanTrang();

                // Hiển thị trang đầu tiên
                await LoadPageAsync();

                toolStripStatusLabel2.Text = "Làm mới thành công!";
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi làm mới: " + ex.Message);
                toolStripStatusLabel2.Text = "Lỗi khi làm mới dữ liệu!";
            }
            finally
            {
                // 4. GIẢI PHÓNG TRẠNG THÁI
                if (!this.IsDisposed)
                {
                    this.Cursor = Cursors.Default;
                    _isPaging = false;
                }
            }
        }
        private void thongTinNguoiDung_toolstrip_Click(object sender, EventArgs e)
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
        private void kryptonButton1_LocTheoNgayThangNam_Click(object sender, EventArgs e)
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
        private async void ThucThiBoLocToanDienAsync()
        {
            if (_listFull == null || _listFull.Count == 0) return;

            try
            {
                this.Cursor = Cursors.WaitCursor;
                toolStripStatusLabel2.Text = "Đang xử lý dữ liệu...";

                // 🔥 BÍ KÍP 1: Chụp lại trạng thái UI hiện tại vào biến cục bộ
                // Tránh việc đang chạy ngầm mà User thay đổi RadioButton gây loạn logic
                bool currentSortAsc = _sortAsc;
                string currentFilterTK = _filterTaiKhoan;
                DateTime? tuNgay = _filterTuNgay;
                DateTime? denNgay = _filterDenNgay;

                await Task.Run(() =>
                {
                    // Sử dụng IEnumerable để Pipeline chạy mượt
                    IEnumerable<NhatKyModel> query = _listFull;

                    // 1. Lọc Tài khoản
                    if (currentFilterTK != "Tất cả")
                    {
                        query = query.Where(x => string.Equals(x.TaiKhoan, currentFilterTK, StringComparison.OrdinalIgnoreCase));
                    }

                    // 2. Lọc Ngày tháng
                    if (tuNgay.HasValue || denNgay.HasValue)
                    {
                        query = query.Where(x =>
                        {
                            if (!x.ThoiGianParsed.HasValue) return false;
                            if (tuNgay.HasValue && x.ThoiGianParsed.Value < tuNgay.Value.Date) return false;
                            if (denNgay.HasValue && x.ThoiGianParsed.Value > denNgay.Value.Date) return false;
                            return true;
                        });
                    }

                    // 3. Sắp xếp (Sử dụng biến đã capture)
                    // Hiệu suất: OrderBy trên List đã nạp RAM rất nhanh (O(n log n))
                    query = currentSortAsc ? query.OrderBy(x => x.ID) : query.OrderByDescending(x => x.ID);

                    // 4. Chốt danh sách
                    _listFiltered = query.ToList();
                });

                // 5. Cập nhật UI
                _currentPage = 1;
                CapNhatPhanTrang();

                // 🔥 BÍ KÍP 2: Ép Grid xóa sạch cache cũ để hiển thị đúng thứ tự mới
                kryptonDataGridView1.RowCount = 0;
                await LoadPageAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi bộ lọc] {ex.Message}");
            }
            finally
            {
                this.Cursor = Cursors.Default;
                toolStripStatusLabel2.Text = "Sẵn sàng";
            }
        }
        private void comboBox_LocTaiKhoan_SelectedIndexChanged(object sender, EventArgs e)
        {
            _filterTaiKhoan = comboBox_LocTaiKhoan.Text;
            _currentPage = 1;

            // ⭐ GỌI HÀM CẬP NHẬT LABEL KHI ĐỔI TÀI KHOẢN LỌC
            CapNhatHienThiTaiKhoanLabel();

            ThucThiBoLocToanDienAsync();
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