using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using System;
using Krypton.Toolkit;
namespace PhanMemThiDua2026
{
    public partial class Form10_NhatKy : Form
    {
        public bool DaLoadDuLieu { get; set; } = false;
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

        private List<NhatKyModel> _listFull = new List<NhatKyModel>();
        private List<NhatKyModel> _listFiltered = new List<NhatKyModel>();
        private List<NhatKyModel> _listCurrentPage = new List<NhatKyModel>();
        private string _localIP = "";
        private DateTime? _filterTuNgay = null;
        private DateTime? _filterDenNgay = null;
        private string _filterTaiKhoan = "Tất cả";
        private string _chuoiTuDongXoa = "";
        private bool _isInit = false;
        private bool _isApplyingPage = false;
        private bool _isPaging = false;
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
            EnableDoubleBuffered(kryptonDataGridView1);

            this.Load += Form10_Load;
            this.Shown += Form10_Shown;
            this.KeyPreview = true;
            kryptonDataGridView1.ContextMenuStrip = contextMenuStrip1;
            InitToolTips();

            textBox_SoDongHienThi.KeyDown += textBox_SoDongHienThi_KeyDown;
            kryptonButton_TiepTheo.Click += kryptonButton_TiepTheo_Click;
            kryptonButton_TroLai.Click += kryptonButton_TroLai_Click;
            kryptonButton_ApDungSoTrang.Click += kryptonButton_ApDungSoTrang_Click;
            comboBox_LocTaiKhoan.SelectedIndexChanged += comboBox_LocTaiKhoan_SelectedIndexChanged;
            radioButton1_TuAZ.CheckedChanged += RadioSapXep_CheckedChanged;
            radioButton1_TuZA.CheckedChanged += RadioSapXep_CheckedChanged;
            kryptonButton1_LocTheoNgayThangNam.Click += kryptonButton1_LocTheoNgayThangNam_Click;

            kryptonDataGridView1.CellPainting += KryptonDataGridView1_CellPainting;
        }
        private void Form10_Load(object sender, EventArgs e)
        {
            if (_isInit) return;
            _isInit = true;

            Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1);
            kryptonDateTimePicker1_NgayThangNamBatDau.Format = DateTimePickerFormat.Custom;
            kryptonDateTimePicker1_NgayThangNamBatDau.CustomFormat = "dd/MM/yyyy";
            kryptonDateTimePicker1_NgayThangNamKetThuc.Format = DateTimePickerFormat.Custom;
            kryptonDateTimePicker1_NgayThangNamKetThuc.CustomFormat = "dd/MM/yyyy";
            textBox_SoDongHienThi.Text = PAGE_SIZE_DEFAULT.ToString();

            CauHinhCangDeuStatusStrip();
            CaiDatCot();
        }
        private void Form10_Shown(object sender, EventArgs e)
        {
            kryptonDataGridView1.VirtualMode = true;
            kryptonDataGridView1.CellValueNeeded += KryptonDataGridView1_CellValueNeeded;

            toolStripStatusLabel1.Text = $"Tài khoản: {Module_TaiKhoan.TenTaiKhoan_RAM}";
            _ = Task.Run(() => CapNhatStatusLabelTuDongXoaNgam());

            _ = Task.Run(async () =>
            {
                await KiemTraVaTaoBangNhatKyAsync(_csdl3Path);
                DamBaoBangTuDongXoaTonTai();
                Module_TaiKhoan.NapTaiKhoanTuCSDL();
            });

            ReloadDuLieu();

            this.Focus();
            if (kryptonDataGridView1.RowCount > 0)
                kryptonDataGridView1.Focus();
        }
        // ===================================================================================
        // 🚀 CỤM ĐỘNG CƠ XỬ LÝ DỮ LIỆU SIÊU TỐC (ĐÃ ĐƯỢC TỐI ƯU HÓA HOÀN TOÀN)
        // ===================================================================================
        private void comboBox_LocTaiKhoan_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_LocTaiKhoan.SelectedItem == null) return;

            _filterTaiKhoan = comboBox_LocTaiKhoan.Text;
            _currentPage = 1;

            // ⭐ Cập nhật nhãn trạng thái UI dưới góc phải
            CapNhatHienThiTaiKhoanLabel();

            // Kích hoạt bộ lọc
            ThucThiBoLocToanDienAsync();
        }
        public void ReloadDuLieu()
        {
            if (IsDisposed || !IsHandleCreated) return;

            try
            {
                UIHelper.SafeInvoke(this, () =>
                {
                    this.Cursor = Cursors.WaitCursor;
                    toolStripStatusLabel2.Text = "Đang tải dữ liệu thô...";
                });

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await TuDongXoaNhatKyNeuCanAsync();
                        await Module_BaoTriCSDL.KiemTraVaVaccumTheoSoDongAsync(Module_DanduongGPS.DuongDanCSDL3);
                    }
                    catch (Exception exDb) { Debug.WriteLine("Lỗi bảo trì CSDL nền: " + exDb.Message); }
                });

                // Gọi động cơ kéo dữ liệu trực tiếp từ SQLite vào RAM
                LoadNhatKyLenDataGridView_SieuToc();

                if (this.IsDisposed) return;

                UIHelper.SafeInvoke(this, () =>
                {
                    if (IsDisposed || !IsHandleCreated) return;

                    _sortAsc = false;
                    if (radioButton1_TuZA != null) radioButton1_TuZA.Checked = true;
                    _currentPage = 1;

                    CapNhatPhanTrang();
                    HienThiTrangHienTai();
                    Module_NhatKy.DocVaNapStatusLabelForm10();
                });

                _ = Task.Run(() =>
                {
                    try { ChuanBiDuLieuBoLocNgam(); }
                    catch (Exception ex) { Debug.WriteLine("Lỗi giải mã ngầm: " + ex.Message); }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Reload Form10 lỗi: " + ex.Message);
                UIHelper.SafeInvoke(this, () => MessageBox.Show($"Lỗi load dữ liệu nhật ký:\n{ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error));
            }
            finally
            {
                UIHelper.SafeInvoke(this, () => { if (!IsDisposed) this.Cursor = Cursors.Default; });
            }
        }
        public void LoadNhatKyLenDataGridView_SieuToc()
        {
            try
            {
                // 1. Kéo dữ liệu từ Database lên DataTable (giữ nguyên logic gốc của bạn)
                var dtMaHoa = Module_NhatKy.LoadTatCaNhatKy();
                if (dtMaHoa == null || dtMaHoa.Rows.Count == 0) return;

                var listRaw = new List<NhatKyModel>(dtMaHoa.Rows.Count);
                string currentIP = GetLocalIP();

                // 2. CHUYỂN DỮ LIỆU THÔ VÀO MẢNG TRƯỚC (Thao tác trên RAM cực nhanh)
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
                        IP = currentIP,
                        DaGiaiMa = false,
                        DaGiaiMaBoLoc = false
                    });
                }

                // 3. THỰC HIỆN GIẢI MÃ AES ĐỒNG LOẠT BẰNG ĐA LUỒNG (Không làm treo Form)
                // Cắt 50% số nhân CPU để giải mã, 50% còn lại giữ cho Winform mượt mà
                int maxThreads = Math.Max(1, Environment.ProcessorCount / 2);
                string[] cacDinhDangNgay = { "dd-MM-yyyy HH:mm:ss", "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss", "dd-MM-yyyy", "dd/MM/yyyy", "M/d/yyyy h:mm:ss tt" };

                Parallel.ForEach(listRaw, new ParallelOptions { MaxDegreeOfParallelism = maxThreads }, item =>
                {
                    // Hàm nội bộ bao bọc BaoMatAES.GiaiMa để chống crash nếu chuỗi rác
                    string GiaiMa(string val)
                    {
                        if (string.IsNullOrWhiteSpace(val)) return "";
                        string safeVal = val.Trim(); // Gọt khoảng trắng dư thừa
                        try
                        {
                            string res = BaoMatAES.GiaiMa(safeVal);
                            return string.IsNullOrEmpty(res) ? safeVal : res;
                        }
                        catch { return safeVal; }
                    }

                    // 👉 GỌI HÀM GIẢI MÃ NGAY TẠI ĐÂY
                    item.ThoiGian = GiaiMa(item.ThoiGianRaw);
                    item.TenMay = GiaiMa(item.TenMayRaw);
                    item.ID_CPU = GiaiMa(item.ID_CPURaw);
                    item.TaiKhoan = GiaiMa(item.TaiKhoanRaw);
                    item.HanhDong = GiaiMa(item.HanhDongRaw);
                    item.GhiChu = GiaiMa(item.GhiChuRaw);

                    // 👉 GÁN ICON THEO HÀNH ĐỘNG
                    string hd = item.HanhDong ?? "";
                    if (hd.Contains("Đăng nhập")) item.IconType = 1;
                    else if (hd.Contains("Dữ liệu") || hd.Contains("CSDL")) item.IconType = 2;
                    else if (hd.Contains("Lưu") || hd.Contains("Thêm") || hd.Contains("Cập nhật")) item.IconType = 3;
                    else if (hd.Contains("Xuất")) item.IconType = 4;
                    else if (hd.Contains("Trợ giúp")) item.IconType = 5;
                    else if (hd.Contains("Xóa")) item.IconType = 6;
                    else if (hd.Contains("Cảnh báo") || hd.Contains("Lỗi")) item.IconType = 7;
                    else item.IconType = 0;

                    // 👉 PARSE NGÀY THÁNG ĐỂ PHỤC VỤ LỌC NGÀY SAU NÀY
                    if (DateTime.TryParseExact(item.ThoiGian.Trim(), cacDinhDangNgay, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime dtLog))
                        item.ThoiGianParsed = dtLog.Date;
                    else if (DateTime.TryParse(item.ThoiGian, out DateTime dtAuto))
                        item.ThoiGianParsed = dtAuto.Date;

                    // Chốt cờ hoàn tất (Khi cuộn lưới sẽ không bao giờ phải giải mã lại nữa)
                    item.DaGiaiMa = true;
                    item.DaGiaiMaBoLoc = true;
                });

                // 4. Giao dữ liệu sạch sẽ, trọn vẹn cho lưới
                _listFull = listRaw;
                _listFiltered = new List<NhatKyModel>(_listFull);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi LoadNhatKyLenDataGridView_SieuToc: {ex.Message}");
            }
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
        private async void ThucThiBoLocToanDienAsync()
        {
            if (_listFull == null || _listFull.Count == 0) return;

            try
            {
                this.Cursor = Cursors.WaitCursor;
                toolStripStatusLabel2.Text = "Đang xử lý dữ liệu...";

                bool currentSortAsc = _sortAsc;
                string currentFilterTK = _filterTaiKhoan;
                DateTime? tuNgay = _filterTuNgay;
                DateTime? denNgay = _filterDenNgay;

                await Task.Run(() =>
                {
                    IEnumerable<NhatKyModel> query = _listFull;

                    if (currentFilterTK != "Tất cả")
                        query = query.Where(x => string.Equals(x.TaiKhoan, currentFilterTK, StringComparison.OrdinalIgnoreCase));

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

                    query = currentSortAsc ? query.OrderBy(x => x.ID) : query.OrderByDescending(x => x.ID);
                    _listFiltered = query.ToList();
                });

                _currentPage = 1;
                CapNhatPhanTrang();
                kryptonDataGridView1.RowCount = 0;
                await LoadPageAsync();
            }
            catch (Exception ex) { Debug.WriteLine($"[Lỗi bộ lọc] {ex.Message}"); }
            finally
            {
                this.Cursor = Cursors.Default;
                toolStripStatusLabel2.Text = "Sẵn sàng";
            }
        }
        // ===================================================================================
        // VIRTUAL MODE VÀ GIAO DIỆN (UI/UX)
        // ===================================================================================
        private void KryptonDataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (_listCurrentPage == null || e.RowIndex < 0 || e.RowIndex >= _listCurrentPage.Count)
            {
                e.Value = ""; // Dòng trống
                return;
            }

            var item = _listCurrentPage[e.RowIndex];

            // 1. Đảm bảo 2 cột lọc được giải mã nếu luồng ngầm chạy chưa kịp tới
            if (!item.DaGiaiMaBoLoc)
            {
                item.ThoiGian = GiaiMaAnToan(item.ThoiGianRaw);
                item.TaiKhoan = GiaiMaAnToan(item.TaiKhoanRaw);
                item.DaGiaiMaBoLoc = true;
            }

            // 2. Giải mã các cột còn lại để hiển thị
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

            // 3. ⭐ MAPPING THEO TÊN CỘT ĐẢM BẢO CHUẨN XÁC 100%
            string colName = kryptonDataGridView1.Columns[e.ColumnIndex].Name;
            switch (colName)
            {
                case "ID": e.Value = item.ID; break;
                case "ThoiGian": e.Value = item.ThoiGian; break;
                case "TenMay": e.Value = item.TenMay; break;
                case "IP": e.Value = item.IP; break;
                case "ID_CPU": e.Value = item.ID_CPU; break;
                case "TaiKhoan": e.Value = item.TaiKhoan; break;
                case "HanhDong": e.Value = item.HanhDong; break;
                case "GhiChu": e.Value = item.GhiChu; break;
                case "IconType": e.Value = item.IconType; break;
                default: e.Value = ""; break;
            }
        }
        private void KryptonDataGridView1_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv == null) return;

            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgv.Columns[e.ColumnIndex].Name == "HanhDong")
            {
                var paintParts = DataGridViewPaintParts.Background | DataGridViewPaintParts.SelectionBackground;

                if (_listCurrentPage != null && e.RowIndex == _listCurrentPage.Count)
                {
                    e.Paint(e.CellBounds, paintParts);
                    e.Handled = true;
                    return;
                }

                e.Paint(e.CellBounds, paintParts);
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
        // ===================================================================================
        // CÁC THAO TÁC CƠ SỞ DỮ LIỆU CÒN LẠI VÀ MENU (Giữ nguyên gốc)
        // ===================================================================================
        private async Task KiemTraVaTaoBangNhatKyAsync(string csdl3Path)
        {
            if (string.IsNullOrWhiteSpace(csdl3Path)) return;
            try
            {
                string folder = Path.GetDirectoryName(csdl3Path);
                if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder)) Directory.CreateDirectory(folder);

                using var conn = new SqliteConnection($"Data Source={csdl3Path};");
                await conn.OpenAsync();

                using (var cmdWal = new SqliteCommand("PRAGMA journal_mode=WAL;", conn))
                    await cmdWal.ExecuteNonQueryAsync();

                string query = @"CREATE TABLE IF NOT EXISTS ""NhatKyUngDung"" (
                    ""ID""        INTEGER NOT NULL,
                    ""ThoiGian""  TEXT,
                    ""TenMay""    TEXT,
                    ""ID_CPU""    TEXT,
                    ""TaiKhoan""  TEXT,
                    ""HanhDong""  TEXT,
                    ""GhiChu""    TEXT,
                    PRIMARY KEY(""ID"" AUTOINCREMENT));";

                using var cmd = new SqliteCommand(query, conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[LỖI DB NHẬT KÝ]: {ex.Message}"); }
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
                        using (var cmd = conn.CreateCommand()) { cmd.CommandText = "PRAGMA foreign_keys = OFF;"; cmd.ExecuteNonQuery(); }
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
                        using (var cmd = conn.CreateCommand()) { cmd.CommandText = "VACUUM;"; cmd.ExecuteNonQuery(); }
                    }
                });

                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, $"Xóa toàn bộ nhật ký hệ thống chứa ({soDongDaXoa} hành động)", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
                MessageBox.Show($"Đã xóa {soDongDaXoa} dòng dữ liệu.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ReloadDuLieu();
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi khi xóa dữ liệu:\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally
            {
                this.Cursor = Cursors.Default;
                toolStripStatusLabel2.ForeColor = Color.Black;
            }
        }
        private async void lamMoi_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_isPaging) return;

            try
            {
                _isPaging = true;
                this.Cursor = Cursors.WaitCursor;
                toolStripStatusLabel2.Text = "Đang làm mới...";

                _filterTuNgay = null;
                _filterDenNgay = null;
                _filterTaiKhoan = "Tất cả";

                kryptonDateTimePicker1_NgayThangNamBatDau.Value = DateTime.Now;
                kryptonDateTimePicker1_NgayThangNamKetThuc.Value = DateTime.Now;

                await TuDongXoaNhatKyNeuCanAsync();

                // ⭐ SỬA LẠI GỌI HÀM MỚI SIÊU TỐC
                await Task.Run(() => LoadNhatKyLenDataGridView_SieuToc());

                if (this.IsDisposed || !this.IsHandleCreated) return;

                LoadComboBoxTaiKhoan();
                CapNhatPhanTrang();
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
                if (!this.IsDisposed) { this.Cursor = Cursors.Default; _isPaging = false; }
            }
        }
        private async void xuatNhatKy_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
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
                Filter = "Excel Files (*.xlsx, *.xlsm)|*.xlsx;*.xlsm",
                FileName = $"NhatKy_PhanMemThiDua2026_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                AddExtension = true,
                DefaultExt = "xlsx"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;
            string fullPath = sfd.FileName;

            try
            {
                this.Cursor = Cursors.WaitCursor;
                toolStripStatusLabel2.Text = "Đang xử lý dữ liệu xuất...";

                var listExport = _listFiltered.ToList();
                string currentIP = GetLocalIP();

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

                await Task.Run(() =>
                {
                    using var wb = new XLWorkbook();
                    var ws = wb.Worksheets.Add("Nhật ký phần mềm");

                    ws.Cell(1, 1).Value = "THỐNG KÊ";
                    ws.Range(1, 1, 1, 8).Merge().Style.Font.SetBold().Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    ws.Cell(2, 1).Value = $"LỊCH SỬ TRUY CẬP PHẦN MỀM THI ĐUA NĂM {DateTime.Now.Year}";
                    ws.Range(2, 1, 2, 8).Merge().Style.Font.SetBold().Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    string[] headers = { "ID", "Thời gian", "Tên máy", "IP Address", "ID CPU", "Tài khoản", "Hành động", "Ghi chú" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = ws.Cell(4, i + 1);
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }

                    var dataToInsert = listExport.Select(x => new { x.ID, x.ThoiGian, x.TenMay, x.IP, x.ID_CPU, x.TaiKhoan, x.HanhDong, x.GhiChu });
                    ws.Cell(5, 1).InsertData(dataToInsert);

                    var tableRange = ws.Range(4, 1, listExport.Count + 4, 8);
                    tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    tableRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    ws.Range(5, 1, listExport.Count + 4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    ws.Range(5, 6, listExport.Count + 4, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                    int tongCongRow = listExport.Count + 5;
                    ws.Cell(tongCongRow, 1).Value = $"Tổng cộng: {listExport.Count} hành động./.";
                    ws.Range(tongCongRow, 1, tongCongRow, 8).Merge().Style.Font.SetBold().Font.SetItalic().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

                    ws.Columns(1, 8).AdjustToContents();
                    Module_BanQuyen.DongDauExcel(wb);
                    wb.SaveAs(fullPath);
                });
                Module_XuatNhapDuLieuThiDua.MoVaChonTepTrongExplorer(fullPath);
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi khi xuất tệp Excel: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally
            {
                this.Cursor = Cursors.Default;
                toolStripStatusLabel2.Text = $"Tổng: {_listFiltered.Count:N0} hành động";
            }
        }
        // ===================================================================================
        // CÁC HÀM TIỆN ÍCH VÀ THÔNG SỐ GIAO DIỆN (UI) KHÁC
        // ===================================================================================
        private string GiaiMaAnToan(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";

            // ⭐ Bắt buộc Trim() khoảng trắng từ SQLite trước khi ném vào AES
            string safeValue = value.Trim();

            try
            {
                string result = BaoMatAES.GiaiMa(safeValue);
                // Nếu giải mã lỗi, giữ nguyên chuỗi ban đầu để Admin kiểm tra
                return string.IsNullOrEmpty(result) ? safeValue : result;
            }
            catch
            {
                return safeValue;
            }
        }
        public void CapNhatVanBanStatusLabel(string vanBan)
        {
            if (toolStripStatusLabel1 != null) toolStripStatusLabel1.Text = vanBan;
        }
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
                    cmd.CommandText = "SELECT Chọn_GiaiTri FROM TuDong_XoaNhatKy WHERE ID = 1 LIMIT 1;";
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value) luaChon = result.ToString();
                }

                if (luaChon == "Không xóa")
                {
                    await Module_BaoTriCSDL.KiemTraVaVaccumTheoSoDongAsync(Module_DanduongGPS.DuongDanCSDL3);
                    return;
                }

                int nguong = luaChon switch { "1000 dòng xóa tự động" => 1000, "5000 dòng xóa tự động" => 5000, "10000 dòng xóa tự động" => 10000, _ => 0 };

                if (nguong > 0)
                {
                    int daXoa = await Task.Run(() => ThucHienXoaVaGiuLai(nguong));
                    _soDongDaXoaTuDong = daXoa;
                    if (daXoa > 0) Debug.WriteLine($"[TuDongXoa] Đã dọn dẹp {daXoa} dòng nhật ký cũ.");
                }

                await Module_BaoTriCSDL.KiemTraVaVaccumTheoSoDongAsync(Module_DanduongGPS.DuongDanCSDL3);
            }
            catch (Exception ex) { Debug.WriteLine($"[TuDongXoa] Lỗi: {ex.Message}"); }
        }
        private int ThucHienXoaVaGiuLai(int soDongMuonGiu)
        {
            try
            {
                using var cn = new SqliteConnection($"Data Source={_csdl3Path};Pooling=True;");
                cn.Open();
                using var tran = cn.BeginTransaction();
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tran;
                cmd.CommandText = @"DELETE FROM NhatKyUngDung WHERE ID NOT IN (SELECT ID FROM NhatKyUngDung ORDER BY ID DESC LIMIT @Nguong);";
                cmd.Parameters.AddWithValue("@Nguong", soDongMuonGiu);
                int deleted = cmd.ExecuteNonQuery();
                tran.Commit();
                return deleted;
            }
            catch (Exception ex) { Debug.WriteLine($"[ThucHienXoaVaGiuLai] Lỗi: {ex.Message}"); return 0; }
        }
        private void thongKeTaiKhoanDaTungSuDungToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            if (formCha == null) return;
            var f = Application.OpenForms.OfType<Form25_ThongKeTaiKhoanDaSuDung>().FirstOrDefault();

            if (f == null)
            {
                f = new Form25_ThongKeTaiKhoanDaSuDung { TopLevel = false, FormBorderStyle = FormBorderStyle.None, Dock = DockStyle.Fill };
                var panel = formCha.Controls.Find("PanelContainer", true).FirstOrDefault() as Panel;
                if (panel == null) return;
                panel.Controls.Add(f);
                f.Show();
                f.BringToFront();
            }
            else { f.BringToFront(); _ = f.LoadDuLieuTaiKhoanDaSuDungAsync(true); }
        }
        private void DamBaoBangTuDongXoaTonTai()
        {
            using var cn = new SqliteConnection($"Data Source={_csdl3Path}");
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = @"CREATE TABLE IF NOT EXISTS TuDong_XoaNhatKy (ID INTEGER PRIMARY KEY AUTOINCREMENT, Chọn_GiaiTri TEXT);";
            cmd.ExecuteNonQuery();
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

                int soDongTuCsdl = luaChon switch { "1000 dòng xóa tự động" => 1000, "5000 dòng xóa tự động" => 5000, "10000 dòng xóa tự động" => 10000, _ => 0 };
                _chuoiTuDongXoa = soDongTuCsdl > 0 ? $" | Tự động xóa khi đạt {soDongTuCsdl} dòng" : "";
                CapNhatHienThiTaiKhoanLabel();
            }
            catch { }
        }
        private void CapNhatHienThiTaiKhoanLabel()
        {
            bool laTatCa = string.IsNullOrWhiteSpace(_filterTaiKhoan) || _filterTaiKhoan == "Tất cả";
            string tenHienThi = laTatCa ? Module_TaiKhoan.TenTaiKhoan_RAM : _filterTaiKhoan;
            string tienTo = laTatCa ? "Tài khoản: " : "Đang lọc dữ liệu của: ";

            if (this.IsHandleCreated && !this.IsDisposed)
                this.BeginInvoke(new Action(() => { toolStripStatusLabel1.Text = $"{tienTo}{tenHienThi}{_chuoiTuDongXoa}"; }));
        }
        private void EnableDoubleBuffered(Control control)
        {
            typeof(Control).InvokeMember("DoubleBuffered", System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, control, new object[] { true });
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_isPaging) { if (keyData == Keys.Escape) this.Close(); return true; }
            bool daXuLyPhimTat = Module_PhimTat.XuLy(keyData: keyData, actionLamMoi: () => lamMoi_ToolStripMenuItem.PerformClick(), actionXuatExcel: () => xuatNhatKy_ToolStripMenuItem.PerformClick(), actionXoa: () => xoaToanBoDuLieu_ToolStripMenuItem.PerformClick());
            if (daXuLyPhimTat) return true;

            Control activeCtrl = this.ActiveControl;
            bool dangNhapLieu = activeCtrl is TextBoxBase || activeCtrl is ComboBox || (activeCtrl != null && activeCtrl.GetType().Name.Contains("TextBox"));
            bool dangThaoTacGrid = activeCtrl is DataGridView || (activeCtrl != null && activeCtrl.GetType().Name.Contains("DataGridView"));

            switch (keyData)
            {
                case Keys.Control | Keys.T: thongKeTaiKhoanDaTungSuDungToolStripMenuItem.PerformClick(); return true;
                case Keys.Right: case Keys.PageDown: if (dangNhapLieu || dangThaoTacGrid) return base.ProcessCmdKey(ref msg, keyData); trangTiepTheo_ToolStripMenuItem.PerformClick(); return true;
                case Keys.Left: case Keys.PageUp: if (dangNhapLieu || dangThaoTacGrid) return base.ProcessCmdKey(ref msg, keyData); trangTroLai_ToolStripMenuItem.PerformClick(); return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        private void ThietLapKhoangNgayMacDinh()
        {
            DateTime ngayMotThangTruoc = DateTime.Now.AddMonths(-1);
            DateTime ngayHienTai = DateTime.Now;

            if (_listFull != null && _listFull.Count > 0)
            {
                var danhSachNgay = _listFull.Where(x => x.ThoiGianParsed.HasValue).Select(x => x.ThoiGianParsed.Value.Date).ToList();
                if (danhSachNgay.Count > 0)
                {
                    DateTime ngayNhoNhat = danhSachNgay.Min();
                    kryptonDateTimePicker1_NgayThangNamBatDau.Value = ngayNhoNhat >= ngayHienTai.Date ? ngayMotThangTruoc : ngayNhoNhat;
                }
                else kryptonDateTimePicker1_NgayThangNamBatDau.Value = ngayMotThangTruoc;
            }
            else kryptonDateTimePicker1_NgayThangNamBatDau.Value = ngayMotThangTruoc;
            kryptonDateTimePicker1_NgayThangNamKetThuc.Value = ngayHienTai;
        }
        private void HienThiTrangHienTai()
        {
            if (_listFiltered == null || _listFiltered.Count == 0)
            {
                kryptonDataGridView1.RowCount = 0;
                label_Trang.Text = "Đồng chí đang ở trang số 0/0";
                return;
            }

            int start = (_currentPage - 1) * _pageSize;
            _listCurrentPage = _listFiltered.Skip(start).Take(_pageSize).ToList();
            kryptonDataGridView1.RowCount = _listCurrentPage.Count + 1;
            kryptonDataGridView1.Invalidate();

            // ⭐ SỬA TẠI ĐÂY:
            label_Trang.Text = $"Đồng chí đang ở trang số {_currentPage}/{_totalPages}";
            toolStripStatusLabel2.Text = $"Trang {_currentPage}/{_totalPages}";
            toolStripStatusLabel4.Text = $"Tổng: {_listFiltered.Count:N0} hành động";
        }
        private async Task LoadPageAsync()
        {
            if (_listFiltered == null || _listFiltered.Count == 0)
            {
                kryptonDataGridView1.RowCount = 0;
                label_Trang.Text = "Đồng chí đang ở trang số 0/0";
                toolStripStatusLabel2.Text = "Trang 0/0";
                return;
            }

            int start = (_currentPage - 1) * _pageSize;
            _listCurrentPage = _listFiltered.Skip(start).Take(_pageSize).ToList();
            if (this.IsDisposed) return;

            kryptonDataGridView1.RowCount = _listCurrentPage.Count + 1;
            kryptonDataGridView1.Invalidate();

            // ⭐ SỬA TẠI ĐÂY:
            label_Trang.Text = $"Đồng chí đang ở trang số {_currentPage}/{_totalPages}";
            toolStripStatusLabel2.Text = $"Trang {_currentPage}/{_totalPages}";
            toolStripStatusLabel4.Text = $"Tổng: {_listFiltered.Count:N0} hành động";
        }
        private void CaiDatCot()
        {
            var dgv = kryptonDataGridView1;
            if (_daCaiDatCot || dgv == null || dgv.IsDisposed) return;
            dgv.SuspendLayout();
            try
            {
                CauHinhStyleWeb(dgv);
                CauHinhGridCoBan(dgv);
                dgv.Columns.Clear();
                TaoCot(dgv);
                _daCaiDatCot = true;
            }
            finally { dgv.ResumeLayout(true); }
        }
        private void CauHinhStyleWeb(DataGridView dgv)
        {
            dgv.RowTemplate.Height = 36;
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgv.AllowUserToResizeRows = false;
            dgv.ColumnHeadersHeight = 45;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            dgv.GridColor = Color.FromArgb(224, 224, 224);
            dgv.RowsDefaultCellStyle.BackColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.BackgroundColor = Color.White;

            if (dgv is KryptonDataGridView kDgv)
            {
                kDgv.GridStyles.Style = Krypton.Toolkit.DataGridViewStyle.List;
                kDgv.StateCommon.HeaderColumn.Content.Padding = new System.Windows.Forms.Padding(6, 8, 6, 8);
                kDgv.StateCommon.HeaderColumn.Content.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
                kDgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                kDgv.StateCommon.DataCell.Content.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
                kDgv.StateCommon.DataCell.Content.Padding = new System.Windows.Forms.Padding(6, 8, 6, 8);
                kDgv.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
                kDgv.StateCommon.DataCell.Border.Color1 = System.Drawing.Color.FromArgb(224, 224, 224);
                kDgv.StateCommon.DataCell.Border.Width = 1;
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
            if (dgv == null || dgv.IsDisposed || dgv.Disposing) return;
            try
            {
                dgv.Columns.Clear();
                AddCol(dgv, "ID", "STT", 12);
                AddCol(dgv, "ThoiGian", "Thời gian", 10);
                AddCol(dgv, "TenMay", "Tên máy", 10);
                AddCol(dgv, "IP", "IP Address", 10);
                AddCol(dgv, "ID_CPU", "ID My Computer", 16);
                AddCol(dgv, "TaiKhoan", "Tài khoản", 12);
                AddCol(dgv, "HanhDong", "Hành động", 15, DataGridViewContentAlignment.MiddleLeft);
                AddCol(dgv, "GhiChu", "Ghi chú", 24, DataGridViewContentAlignment.MiddleLeft);
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "IconType", HeaderText = "IconType", Visible = false });

                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (col == null) continue;
                    if (col.Name.Equals("ID", StringComparison.OrdinalIgnoreCase)) { col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; col.Width = 80; }
                    else if (col.Visible) col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }
            }
            catch { }
        }
        private void AddCol(DataGridView dgv, string name, string header, float fillWeight, DataGridViewContentAlignment align = DataGridViewContentAlignment.MiddleCenter)
        {
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = name, HeaderText = header, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = new DataGridViewCellStyle { Alignment = align }, FillWeight = fillWeight });
        }
        private void InitToolTips()
        {
            var toolTip_PhanTrang = new System.Windows.Forms.ToolTip { IsBalloon = true, ToolTipTitle = "Nhật ký phần mềm", ToolTipIcon = ToolTipIcon.Info, InitialDelay = 200, AutoPopDelay = 1200, ReshowDelay = 100, ShowAlways = true };
            var tips = new Dictionary<System.Windows.Forms.Control, string> { { kryptonButton_ApDungSoTrang, "Áp dụng số trang hiển thị" }, { kryptonButton_TroLai, "Quay lại trang trước" }, { kryptonButton_TiepTheo, "Chuyển sang trang tiếp theo" }, { kryptonButton1_LocTheoNgayThangNam, "Lọc theo ngày tháng năm" }, { comboBox_LocTaiKhoan, "Chọn tài khoản đã từng đăng nhập sử dụng" } };
            foreach (var tip in tips) { if (tip.Key != null) toolTip_PhanTrang.SetToolTip(tip.Key, tip.Value); }
        }
       //Trung Kiên
        private void trangTiepTheo_ToolStripMenuItem_Click(object sender, EventArgs e) { if (kryptonButton_TiepTheo.Enabled) kryptonButton_TiepTheo.PerformClick(); }
        private void trangTroLai_ToolStripMenuItem_Click(object sender, EventArgs e) { if (kryptonButton_TroLai.Enabled) kryptonButton_TroLai.PerformClick(); }
        //===================================================================================
        private async void kryptonButton_ApDungSoTrang_Click(object sender, EventArgs e)
        {
            if (_isApplyingPage || _isPaging)
                return;

            string textBanDau = kryptonButton_ApDungSoTrang.Values.Text;
            Image anhBanDau = kryptonButton_ApDungSoTrang.Values.Image;

            try
            {
                _isApplyingPage = true;

                kryptonButton_ApDungSoTrang.Enabled = false;
                kryptonButton_TiepTheo.Enabled = false;
                kryptonButton_TroLai.Enabled = false;

                kryptonButton_ApDungSoTrang.Values.Text = "Đang tải...";
                kryptonButton_ApDungSoTrang.Values.Image = null;

                await ApDungSoTrang();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Phân trang] Lỗi áp dụng số trang: {ex}");

                MessageBox.Show(
                    $"Lỗi khi áp dụng số dòng hiển thị:\r\n{ex.Message}",
                    "Lỗi hệ thống",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _isApplyingPage = false;

                kryptonButton_ApDungSoTrang.Values.Text = textBanDau;
                kryptonButton_ApDungSoTrang.Values.Image = anhBanDau;

                CapNhatTrangThaiNutPhanTrang();
            }
        }
        private async Task ApDungSoTrang()
        {
            int size;

            if (!int.TryParse(textBox_SoDongHienThi.Text.Trim(), out size) ||
                size < PAGE_SIZE_MIN ||
                size > PAGE_SIZE_MAX)
            {
                await HienThiCanhBaoSoTrang();

                size = PAGE_SIZE_DEFAULT;
                textBox_SoDongHienThi.Text = size.ToString();
            }

            _pageSize = size;
            _currentPage = 1;

            CapNhatPhanTrang();

            LoadPage();

            await HienThiThanhCongSoTrang();
        }
        private void CapNhatPhanTrang()
        {
            int totalRecords = _listFiltered?.Count ?? 0;

            if (_pageSize < PAGE_SIZE_MIN || _pageSize > PAGE_SIZE_MAX)
                _pageSize = PAGE_SIZE_DEFAULT;

            _totalPages = totalRecords == 0
                ? 1
                : (int)Math.Ceiling(totalRecords / (double)_pageSize);

            if (_totalPages < 1)
                _totalPages = 1;

            _currentPage = Math.Clamp(
                _currentPage,
                1,
                _totalPages);
        }
        private async void kryptonButton_TiepTheo_Click(object sender, EventArgs e)
        {
            await ChuyenTrangThucThi(1);
        }
        private async void kryptonButton_TroLai_Click(object sender, EventArgs e)
        {
            await ChuyenTrangThucThi(-1);
        }
        private async Task ChuyenTrangThucThi(int step)
        {
            if (step != 1 && step != -1)
                return;

            if (_isPaging || _isApplyingPage)
                return;

            // Luôn tính lại trạng thái trước khi chuyển.
            CapNhatPhanTrang();

            int trangHienTai = _currentPage;
            int trangDich = trangHienTai + step;

            // Không cho vượt biên.
            if (trangDich < 1 || trangDich > _totalPages)
            {
                CapNhatTrangThaiNutPhanTrang();
                return;
            }

            // ⭐ BƯỚC 1: BẢO LƯU TRẠNG THÁI NÚT TRƯỚC KHI THAO TÁC
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

                if (step > 0)
                    kryptonButton_TiepTheo.Values.Text = "...";
                else
                    kryptonButton_TroLai.Values.Text = "...";

                // ⭐ BỔ SUNG: Nhường CPU 50ms để WinForms kịp vẽ chữ "..." lên nút trước khi LoadPage khóa luồng
                await Task.Delay(50);

                // Chỉ thay đổi trang sau khi đã xác nhận trang đích hợp lệ.
                _currentPage = trangDich;

                try
                {
                    LoadPage();
                }
                catch
                {
                    // Nếu hiển thị lỗi, quay lại trang cũ.
                    _currentPage = trangHienTai;
                    throw;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Phân trang] Lỗi chuyển trang: {ex}");

                MessageBox.Show(
                    $"Lỗi khi chuyển trang:\r\n{ex.Message}",
                    "Lỗi hệ thống",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _isPaging = false;

                // ⭐ BƯỚC 2: PHỤC HỒI TÊN NÚT BẤT KỂ THÀNH CÔNG HAY LỖI
                // Có cơ chế fallback an toàn nếu lỡ biến chụp bị kẹt chữ "..." do click quá nhanh
                kryptonButton_TiepTheo.Values.Text = textTiep == "..." ? "Tiếp theo" : textTiep;
                kryptonButton_TroLai.Values.Text = textLui == "..." ? "Trở lại" : textLui;

                CapNhatPhanTrang();
                CapNhatTrangThaiNutPhanTrang();

                toolStripStatusLabel2.ForeColor = Color.Black;
            }
        }
        private void LoadPage()
        {
            if (IsDisposed || kryptonDataGridView1 == null || kryptonDataGridView1.IsDisposed)
                return;

            if (_listFiltered == null || _listFiltered.Count == 0)
            {
                _listCurrentPage = new List<NhatKyModel>();
                kryptonDataGridView1.RowCount = 0;

                label_Trang.Text = "Đồng chí đang ở trang số 0/0";
                toolStripStatusLabel2.Text = "Trang 0/0";
                toolStripStatusLabel4.Text = "Tổng: 0 hành động";
                return;
            }

            CapNhatPhanTrang();

            int startIndex = (_currentPage - 1) * _pageSize;
            if (startIndex < 0) startIndex = 0;
            if (startIndex >= _listFiltered.Count)
            {
                _currentPage = _totalPages;
                startIndex = (_currentPage - 1) * _pageSize;
            }

            _listCurrentPage = _listFiltered
                .Skip(startIndex)
                .Take(_pageSize)
                .ToList();

            kryptonDataGridView1.RowCount = _listCurrentPage.Count;
            kryptonDataGridView1.Invalidate();

            // ⭐ SỬA TẠI ĐÂY:
            label_Trang.Text = $"Đồng chí đang ở trang số {_currentPage}/{_totalPages}";
            toolStripStatusLabel2.Text = $"Trang {_currentPage}/{_totalPages}";
            toolStripStatusLabel4.Text = $"Tổng: {_listFiltered.Count:N0} hành động";
        }
        private void CapNhatTrangThaiNutPhanTrang()
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            CapNhatPhanTrang();

            bool dangBan =
                _isPaging ||
                _isApplyingPage;

            kryptonButton_TiepTheo.Enabled =
                !dangBan &&
                _currentPage < _totalPages;

            kryptonButton_TroLai.Enabled =
                !dangBan &&
                _currentPage > 1;

            kryptonButton_ApDungSoTrang.Enabled =
                !dangBan;
        }
        //===================================================================================
        private void LoadComboBoxTaiKhoan()
        {
            comboBox_LocTaiKhoan.Items.Clear();
            comboBox_LocTaiKhoan.Items.Add("Tất cả");
            if (_listFull != null && _listFull.Count > 0)
            {
                var danhSachDaLoc = _listFull.Select(x => x.TaiKhoan).Where(tk => !string.IsNullOrWhiteSpace(tk)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
                comboBox_LocTaiKhoan.Items.AddRange(danhSachDaLoc);
            }
            if (comboBox_LocTaiKhoan.Items.Count > 0) comboBox_LocTaiKhoan.SelectedIndex = 0;
        }
        private void RadioSapXep_CheckedChanged(object sender, EventArgs e)
        {
            if (!((RadioButton)sender).Checked) return;
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
                try { await ApDungSoTrang(); }
                catch (Exception ex) { MessageBox.Show($"Lỗi khi áp dụng số trang: {ex.Message}"); }
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
                if (subItem is ToolStripMenuItem tsmi && tsmi.DropDownItems.Count > 0) SetSubMenuFont(tsmi.DropDownItems, font);
            }
        }
        private void CauHinhCangDeuStatusStrip()
        {
            if (IsDisposed || !IsHandleCreated || statusStrip1 == null) return;
            if (InvokeRequired) { BeginInvoke(new Action(CauHinhCangDeuStatusStrip)); return; }
            try
            {
                statusStrip1.SuspendLayout();
                statusStrip1.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
                statusStrip1.GripStyle = ToolStripGripStyle.Hidden;

                if (toolStripStatusLabel1 != null) { toolStripStatusLabel1.Spring = true; toolStripStatusLabel1.TextAlign = ContentAlignment.MiddleLeft; toolStripStatusLabel1.BorderSides = ToolStripStatusLabelBorderSides.None; toolStripStatusLabel1.Margin = new Padding(5, 3, 0, 2); }
                if (toolStripStatusLabel4 != null) { toolStripStatusLabel4.Spring = false; toolStripStatusLabel4.TextAlign = ContentAlignment.MiddleCenter; toolStripStatusLabel4.BorderSides = ToolStripStatusLabelBorderSides.None; toolStripStatusLabel4.Margin = new Padding(0, 3, 10, 2); }
                if (toolStripStatusLabel2 != null) { toolStripStatusLabel2.Spring = false; toolStripStatusLabel2.TextAlign = ContentAlignment.MiddleCenter; toolStripStatusLabel2.BorderSides = ToolStripStatusLabelBorderSides.None; toolStripStatusLabel2.Margin = new Padding(10, 3, 0, 2); }
                if (toolStripStatusLabel3 != null) { toolStripStatusLabel3.Spring = true; toolStripStatusLabel3.TextAlign = ContentAlignment.MiddleRight; toolStripStatusLabel3.BorderSides = ToolStripStatusLabelBorderSides.None; toolStripStatusLabel3.Margin = new Padding(0, 3, 5, 2); }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[CauHinhStatusStrip] Lỗi: {ex.Message}"); }
            finally { statusStrip1.ResumeLayout(true); }
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
            statusStrip1.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;

            if (radioButton1_TuAZ.Checked) toolStripStatusLabel3.Text = "Sắp xếp: Cũ nhất trước (A → Z)";
            else if (radioButton1_TuZA.Checked) toolStripStatusLabel3.Text = "Sắp xếp: Mới nhất trước (Z → A)";
            else toolStripStatusLabel3.Text = "Sắp xếp: Chưa rõ";

            toolStripStatusLabel3.Font = new Font("Segoe UI", 9F);
            toolStripStatusLabel3.TextAlign = ContentAlignment.MiddleRight;
            statusStrip1.PerformLayout();
            statusStrip1.Refresh();
        }
        private void thongTinNguoiDung_toolstrip_Click(object sender, EventArgs e)
        {
            try
            {
                var frm = Form39_ThongTinNguoiDung.GetInstance();
                if (!frm.IsHandleCreated) frm.CreateControl();
                if (frm.WindowState == FormWindowState.Minimized) frm.WindowState = FormWindowState.Normal;
                if (!frm.Visible) frm.Show();
                frm.BringToFront();
                frm.Activate();
                frm.Focus();
            }
            catch (ObjectDisposedException) { try { var frm = Form39_ThongTinNguoiDung.GetInstance(); frm.Show(); frm.Activate(); } catch { } }
            catch (Exception ex) { Debug.WriteLine($"[ThongTinNguoiDung] {ex}"); }
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
            public bool DaGiaiMaBoLoc { get; set; } = false;
            public DateTime? ThoiGianParsed { get; set; }
        }
    }
}