///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using System.Data;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form29_CBCSTrongDienQuanLy : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private readonly string _connStr;
        // RAM: List với Model độc lập (không đụng hàng với project gốc)
        private List<CBCSQuanLyModel> _dataGoc = new List<CBCSQuanLyModel>();
        private List<CBCSQuanLyModel> _viewData = new List<CBCSQuanLyModel>();
        private const string PLACEHOLDER_TIMTEN = Module_HeThong.Nhap_Tim_Kiem;
        // Chống treo UI đa luồng
        private int _isUpdatingUI = 0;
        private int _isLoading = 0;
        // Anti-zombie task: hủy an toàn khi đóng form
        private CancellationTokenSource _cts = new CancellationTokenSource();
        // Cột lưới: thứ tự cố định (cột 0 là STT). CellValueNeeded dùng chỉ số cột -> không tra tên mỗi ô.
        private static readonly (string Name, string Header, float Weight, bool Trai)[] CotLuoi =
        {
            ("HoVaTen",     "Họ và tên", 3f, true),
            ("SoHieu",      "Số hiệu",   1f, false),
            ("NamSinh",     "Năm sinh",  1f, false),
            ("QueQuan",     "Quê quán",  3f, true),
            ("NgayVaoCAND", "Vào CAND",  1f, false),
            ("CapBac",      "Cấp bậc",   1f, false),
            ("ChucVu",      "Chức vụ",   2f, false),
            ("DonVi",       "Đơn vị",    2f, false),
            ("PhanLoai",    "Phân loại", 1f, false),
            ("GhiChu",      "Ghi chú",   3f, true),
        };
        private System.Windows.Forms.Timer? _searchTimer;
        private Bitmap? _iconLoai1;
        private int _hoverRow = -1;
        // Dấu vân tay CSDL2 (độ dài + thời điểm sửa của .db và -wal): đổi nghĩa là Form6 đã thêm/xóa/sửa dữ liệu
        private string _dauVanTay = string.Empty;
        private bool _daTaiLanDau;
        private static readonly Font THONGKE_FONT = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Regular);
        private static readonly Font FONT_LUOI = new Font(Module_HeThong.TenFontHeThong, 9.5F, FontStyle.Regular);
        private static readonly Font FONT_TIEUDE_LUOI = new Font(Module_HeThong.TenFontHeThong, 9.5F, FontStyle.Bold);
        // Màu thống kê
        private static readonly Color COLOR_LOAI_1 = Color.FromArgb(75, 85, 99);
        private static readonly Color COLOR_LOAI_2 = Color.FromArgb(75, 85, 99);
        private static readonly Color COLOR_LOAI_3 = Color.FromArgb(75, 85, 99);
        private static readonly Color COLOR_LOAI_4 = Color.FromArgb(75, 85, 99);
        private static readonly Color COLOR_KHONG_PL = Color.FromArgb(75, 85, 99);
        private int _isUpdatingThongKe = 0;
        // Ô nhập thật nằm bên trong ô bo tròn oTimBoGoc (Designer chỉ tạo oTimBoGoc). Khai báo dạng property để trình thiết kế không làm mất liên kết.
        private TextBox textBoxKyToon_TimTen => oTimBoGoc.Hop;

        public Form29_CBCSTrongDienQuanLy()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            ShowInTaskbar = false;
            _connStr = $"Data Source={_csdl2Path};Pooling=True;";
            // Icon 16x16: tạo 1 lần
            if (Properties.Resources.ic_khenthuong != null)
                _iconLoai1 = new Bitmap(Properties.Resources.ic_khenthuong, new Size(16, 16));
            // Lưới + toàn bộ cột dựng NGAY trong constructor (rẻ, đồng bộ) -> form hiện ra là có khung hoàn chỉnh, dữ liệu đổ vào sau
            TaoCotChoGridView();
            InitPlaceHolder();
            InitSearchTimer();
            toolTip1.SetToolTip(kryptonButton_LamMoiCacOTimKiem, "Xóa bộ lọc (F4)");
            toolTip1.SetToolTip(kryptonButton1_TimKiemThongTinCBCSQuanLy, "Tìm theo họ tên hoặc số hiệu (Enter)");
            var g = kryptonDataGridView2;
            g.VirtualMode = true;
            g.CellValueNeeded += KryptonDataGridView2_CellValueNeeded;
            g.CellPainting += KryptonDataGridView2_CellPainting;
            g.CellFormatting += Luoi_CellFormatting;
            g.CellMouseEnter += Luoi_CellMouseEnter;
            g.MouseLeave += Luoi_MouseLeave;
        }

        private async void Form29_CBCSTrongDienQuanLy_Load(object? sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref _isLoading, 1) == 1)
                return;
            try
            {
                toolStripStatusLabel4.Text = "Đang tải dữ liệu...";
                // Tải + giải mã AES hoàn toàn trên luồng nền (bản cũ chạy phần giải mã trên luồng UI nên giật)
                if (_dataGoc.Count == 0)
                {
                    string key = LayDauVanTayCSDL();
                    _dataGoc = await DocDanhSachNenAsync(_cts.Token);
                    _dauVanTay = key;
                    if (IsDisposed || _cts.IsCancellationRequested)
                        return;
                }
                LoadComboDonVi();
                LoadComboPhanLoai();
                LoadComboGhiChu();
                InitFilters();
                _daTaiLanDau = true;
                ApplyFilter_Virtual();
                int tongSo = _dataGoc.Count;
                toolStripStatusLabel4.Visible = tongSo > 0;
                if (tongSo > 0)
                    toolStripStatusLabel4.Text = $"Tổng cộng {tongSo:N0} {Module_HeThong.Tu_dong_chi}";
                toolStripStatusLabel4.ForeColor = Color.FromArgb(75, 85, 99);
                toolStripStatusLabel4.Font = THONGKE_FONT;
                // Việc phụ làm SAU khi dữ liệu đã hiện
                Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                LogError(nameof(Form29_CBCSTrongDienQuanLy_Load), ex);
                MessageBox.Show(
                    $"Không thể tải dữ liệu.\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                Interlocked.Exchange(ref _isLoading, 0);
            }
        }

        // Form được Show lại sau khi Form6 thêm/xóa dữ liệu -> tự nạp lại (âm thầm) nếu CSDL2 đã đổi
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && _daTaiLanDau && !IsDisposed)
                _ = NapLaiNeuCSDLDaDoiAsync();
        }

        private async Task NapLaiNeuCSDLDaDoiAsync()
        {
            try
            {
                if (Volatile.Read(ref _isLoading) == 1) return;
                if (LayDauVanTayCSDL() == _dauVanTay) return;
                if (Interlocked.Exchange(ref _isLoading, 1) == 1) return;
                try
                {
                    string key = LayDauVanTayCSDL();
                    var data = await DocDanhSachNenAsync(_cts.Token);
                    if (IsDisposed || _cts.IsCancellationRequested) return;
                    _dataGoc = data;
                    _dauVanTay = key;
                    ReloadComboSafe(giuLuaChon: true);
                    ApplyFilter_Virtual();
                    toolStripStatusLabel4.Text = $"Đã cập nhật {_dataGoc.Count:N0} dữ liệu mới nhất";
                }
                finally
                {
                    Interlocked.Exchange(ref _isLoading, 0);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { LogError(nameof(NapLaiNeuCSDLDaDoiAsync), ex); }
        }

        private string LayDauVanTayCSDL()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                foreach (var p in new[] { _csdl2Path, _csdl2Path + "-wal" })
                {
                    var fi = new FileInfo(p);
                    if (fi.Exists) sb.Append(fi.Length).Append('|').Append(fi.LastWriteTimeUtc.Ticks).Append(';');
                    else sb.Append("0;");
                }
                return sb.ToString();
            }
            catch { return Guid.NewGuid().ToString(); }
        }

        // ===== LÕI DỮ LIỆU: chạy toàn bộ trên luồng nền (đọc SQLite + giải mã AES + gộp chuỗi) =====
        private Task<List<CBCSQuanLyModel>> DocDanhSachNenAsync(CancellationToken token) =>
            Task.Run(() => DocDanhSach(token), token);

        private List<CBCSQuanLyModel> DocDanhSach(CancellationToken token)
        {
            var list = new List<CBCSQuanLyModel>(5000);
            if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path)) return list;
            const string sql = @"SELECT HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu
                         FROM DanhSach
                         WHERE LENGTH(TRIM(IFNULL(GhiChu, ''))) > 0";
            var aesCache = new Dictionary<string, string>(10000, StringComparer.Ordinal);
            var stringPool = new Dictionary<string, string>(5000, StringComparer.Ordinal);
            using var conn = new SqliteConnection(_connStr);
            conn.Open();
            using var cmd = new SqliteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            string GiaiMaVaGop(int index)
            {
                if (reader.IsDBNull(index)) return string.Empty;
                string raw;
                try { raw = reader.GetString(index); } catch { return string.Empty; }
                if (string.IsNullOrEmpty(raw)) return string.Empty;
                if (!aesCache.TryGetValue(raw, out string? decrypted))
                {
                    try { decrypted = Module_BaoMatAES.GiaiMa(raw)?.Trim() ?? string.Empty; } catch { decrypted = string.Empty; }
                    aesCache[raw] = decrypted;
                }
                if (decrypted.Length == 0) return string.Empty;
                if (!stringPool.TryGetValue(decrypted, out string? pooled))
                {
                    pooled = decrypted;
                    stringPool[pooled] = pooled;
                }
                return pooled;
            }
            int recordCount = 0;
            while (reader.Read())
            {
                token.ThrowIfCancellationRequested();
                if (++recordCount > 500000) throw new Exception("Database quá lớn (>500k).");
                list.Add(new CBCSQuanLyModel
                {
                    HoVaTen = GiaiMaVaGop(0),
                    SoHieu = GiaiMaVaGop(1),
                    NamSinh = GiaiMaVaGop(2),
                    QueQuan = GiaiMaVaGop(3),
                    NgayVaoCAND = GiaiMaVaGop(4),
                    CapBac = GiaiMaVaGop(5),
                    ChucVu = GiaiMaVaGop(6),
                    DonVi = GiaiMaVaGop(7),
                    PhanLoai = GiaiMaVaGop(8),
                    GhiChu = GiaiMaVaGop(9)
                });
            }
            return list;
        }

        // ===== BỘ LỌC + HIỂN THỊ VIRTUAL MODE =====
        private void ApplyFilter_Virtual()
        {
            if (IsDisposed || !IsHandleCreated)
                return;
            try
            {
                IEnumerable<CBCSQuanLyModel> query = _dataGoc;
                string keyword = textBoxKyToon_TimTen.Text.Trim();
                string donVi = comboBox_TimKiemDonVi.Text?.Trim() ?? "";
                string phanLoai = comboBox_XepLoaiThiDua.Text?.Trim() ?? "";
                string ghiChu = comboBox1_GhiChu.Text?.Trim() ?? "";
                if (keyword.Length > 0)
                {
                    query = query.Where(x =>
                        (!string.IsNullOrWhiteSpace(x.HoVaTen) &&
                         x.HoVaTen.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        ||
                        (!string.IsNullOrWhiteSpace(x.SoHieu) &&
                         x.SoHieu.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
                }
                if (!string.IsNullOrWhiteSpace(donVi) && donVi != Module_HeThong.Tat_Ca)
                    query = query.Where(x => x.DonVi == donVi);
                if (!string.IsNullOrWhiteSpace(phanLoai) && phanLoai != Module_HeThong.Tat_Ca)
                {
                    if (phanLoai == Module_HeThong.PL_KHONG_PL)
                        query = query.Where(x => string.IsNullOrWhiteSpace(x.PhanLoai));
                    else
                        query = query.Where(x => x.PhanLoai == phanLoai);
                }
                if (!string.IsNullOrWhiteSpace(ghiChu) && ghiChu != Module_HeThong.Tat_Ca)
                    query = query.Where(x => x.GhiChu == ghiChu);
                _viewData = query.ToList();
                var grid = kryptonDataGridView2;
                _hoverRow = -1;
                grid.CurrentCell = null;
                grid.RowCount = _viewData.Count;   // virtual mode: chỉ báo số dòng, ô nào hiện mới lấy dữ liệu
                grid.Invalidate();
                lblKhongCoDuLieu.Visible = _viewData.Count == 0 && _daTaiLanDau;
                int soDonVi = _viewData
                    .Where(x => !string.IsNullOrWhiteSpace(x.DonVi))
                    .Select(x => x.DonVi)
                    .Distinct(StringComparer.Ordinal)
                    .Count();
                CapNhatThongKeDonVi(_viewData.Count, soDonVi);
                UpdateThongKePhanLoai();
            }
            catch (Exception ex)
            {
                LogError(nameof(ApplyFilter_Virtual), ex);
            }
        }

        private void KryptonDataGridView2_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            int r = e.RowIndex;
            if (r < 0 || r >= _viewData.Count) { e.Value = string.Empty; return; }
            var item = _viewData[r];
            e.Value = e.ColumnIndex switch
            {
                0 => r + 1,
                1 => item.HoVaTen,
                2 => item.SoHieu,
                3 => item.NamSinh,
                4 => item.QueQuan,
                5 => item.NgayVaoCAND,
                6 => item.CapBac,
                7 => item.ChucVu,
                8 => item.DonVi,
                9 => item.PhanLoai,
                10 => item.GhiChu,
                _ => string.Empty
            };
        }

        // Tô sáng dòng đang trỏ chuột (hiệu ứng hover như bảng web)
        private void Luoi_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex == _hoverRow && e.RowIndex != kryptonDataGridView2.CurrentCellAddress.Y && e.CellStyle != null)
                e.CellStyle.BackColor = Mau.DongHover;
        }

        private void Luoi_CellMouseEnter(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == _hoverRow) return;
            DatHoverRow(e.RowIndex);
        }

        private void Luoi_MouseLeave(object? sender, EventArgs e) => DatHoverRow(-1);

        private void DatHoverRow(int row)
        {
            var g = kryptonDataGridView2;
            int old = _hoverRow;
            _hoverRow = row;
            if (old >= 0 && old < g.RowCount) g.InvalidateRow(old);
            if (row >= 0 && row < g.RowCount) g.InvalidateRow(row);
        }

        // Vẽ icon khen thưởng trước tên người Loại 1
        private void KryptonDataGridView2_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 1 || _iconLoai1 == null) return;
            if (e.RowIndex >= _viewData.Count) return;
            if (!string.Equals(_viewData[e.RowIndex].PhanLoai, Module_HeThong.Loai_1, StringComparison.OrdinalIgnoreCase)) return;
            e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.SelectionBackground | DataGridViewPaintParts.Border); // có Border để giữ đường kẻ ngang mảnh, bỏ Focus để không bị gạch đen
            int iconSize = 16;
            int xIcon = e.CellBounds.X + 8;
            int yIcon = e.CellBounds.Y + (e.CellBounds.Height - iconSize) / 2;
            e.Graphics!.DrawImageUnscaled(_iconLoai1, xIcon, yIcon);
            string hoTen = e.Value?.ToString() ?? string.Empty;
            if (hoTen.Length > 0)
            {
                int textStartX = xIcon + iconSize + 6;
                var textBounds = new Rectangle(textStartX, e.CellBounds.Y, e.CellBounds.Right - textStartX - 2, e.CellBounds.Height);
                bool chon = (e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected;
                Color textColor = chon ? e.CellStyle!.SelectionForeColor : e.CellStyle!.ForeColor;
                const TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
                TextRenderer.DrawText(e.Graphics, hoTen, e.CellStyle.Font, textBounds, textColor, flags);
            }
            e.Handled = true;
        }

        // ===== XUẤT EXCEL ASYNC (không treo UI) =====
        private async void xuatDuLieu_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (_viewData == null || _viewData.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int soLuong = _viewData.Count;
            string ngay = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            string tenFileGoiY = $"Danh sách {soLuong} CBCS trong diện quản lý - {ngay}.xlsx";
            using SaveFileDialog save = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = tenFileGoiY,
                Title = "Xuất Excel"
            };
            if (save.ShowDialog() != DialogResult.OK) return;
            string filePath = save.FileName;
            var exportData = _viewData.ToList(); // Clone an toàn
            Form_Loading frmLoad = new Form_Loading("Đang kết xuất dữ liệu Excel...");
            this.Enabled = false;
            frmLoad.Show(this);
            try
            {
                await Task.Run(() => ExportExcelToPath(filePath, exportData, soLuong));
                try
                {
                    Module_XuatNhapDuLieuThiDua.MoVaChonTepTrongExplorer(filePath);
                }
                catch (Exception ex)
                {
                    LogError("Lỗi mở File Explorer bằng Shell API", ex);
                }
            }
            catch (Exception ex)
            {
                LogError("Lỗi xuất Excel", ex);
                MessageBox.Show($"Xuất Excel thất bại.\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!frmLoad.IsDisposed) frmLoad.Close();
                this.Enabled = true;
                this.Focus();
            }
        }

        private void ExportExcelToPath(string filePath, List<CBCSQuanLyModel> data, int soLuong)
        {
            var dataProjection = data.Select((row, i) => new object[] {
        i + 1, row.HoVaTen, row.SoHieu, row.NamSinh, row.QueQuan,
        row.NgayVaoCAND, row.CapBac, row.ChucVu, row.DonVi, row.PhanLoai, row.GhiChu
    });
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("DanhSach");
            ws.Style.Font.SetFontName(Module_HeThong.Font_Times_New_Roman);
            ws.Style.Font.SetFontSize(12);
            ws.Range("A1:K1").Merge().Value = "DANH SÁCH";
            ws.Cell("A1").Style.Font.SetBold(true).Font.SetFontSize(16).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Range("A2:K2").Merge().Value = $"CBCS TRONG DIỆN QUẢN LÝ THÁNG {DateTime.Now.Month}/{DateTime.Now.Year}";
            ws.Cell("A2").Style.Font.SetBold(true).Font.SetItalic(true).Font.SetFontSize(13).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Cell(5, 1).InsertData(dataProjection);
            int headerRow = 4;
            string[] headerTitles = { "STT", "Họ và tên", "Số hiệu", "Năm sinh", "Quê quán", "Vào CAND", "Cấp bậc", "Chức vụ", "Đơn vị", "Phân loại", "Ghi chú" };
            for (int i = 0; i < headerTitles.Length; i++)
            {
                ws.Cell(headerRow, i + 1).Value = headerTitles[i];
            }
            ws.Row(headerRow).Height = 25;
            double[] colWidths = { 5, 25, 12, 10, 30, 12, 14, 15, 15, 12, 40 };
            for (int i = 0; i < colWidths.Length; i++)
            {
                int colIdx = i + 1;
                var col = ws.Column(colIdx);
                col.Width = colWidths[i];
                if (new[] { 1, 3, 4, 6, 7, 8, 9, 10 }.Contains(colIdx))
                {
                    col.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    col.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                else
                {
                    col.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    if (colIdx == 5 || colIdx == 11)
                    {
                        col.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                        col.Style.Alignment.SetWrapText(true);
                    }
                    else
                    {
                        col.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    }
                }
            }
            int lastDataRow = 5 + data.Count - 1;
            var tableRange = ws.Range(headerRow, 1, lastDataRow, 11);
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            var footerCell = ws.Cell(lastDataRow + 1, 1);
            footerCell.Value = $"Tổng cộng: {soLuong} {Module_HeThong.Tu_dong_chi}/.";
            footerCell.Style.Font.SetBold(true).Font.SetItalic(true).Font.SetFontSize(12);
            footerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            var rangeHeaderTable = ws.Range("A4:K4");
            rangeHeaderTable.Style.Font.Bold = true;
            rangeHeaderTable.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8E8E8");
            rangeHeaderTable.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            rangeHeaderTable.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            Module_BanQuyen.DongDauExcel(wb);
            wb.SaveAs(filePath);
        }

        // ===== TÌM KIẾM (debounce) =====
        private void InitSearchTimer()
        {
            if (_searchTimer != null)
                return;
            _searchTimer = new System.Windows.Forms.Timer { Interval = 250 };
            _searchTimer.Tick += SearchTimer_Tick;
        }

        private void SearchTimer_Tick(object? sender, EventArgs e)
        {
            _searchTimer?.Stop();
            ApplyFilter_Virtual();
        }

        private Action SafeAction(Action action)
        {
            return () => { if (action != null) try { action.Invoke(); } catch (Exception ex) { LogError("Lỗi phím tắt", ex); } };
        }

        private bool SafeExecute(Action action) { SafeAction(action).Invoke(); return true; }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            try
            {
                if (Module_PhimTat.XuLy(keyData, actionLamMoi: SafeAction(() => lamMoi_ToolStripMenuItem.PerformClick()), actionXuatExcel: SafeAction(() => xuatDuLieu_ToolStripMenuItem.PerformClick())))
                    return true;
                Keys key = keyData & Keys.KeyCode;
                Keys modifier = keyData & Keys.Modifiers;
                if (modifier == Keys.None && key == Keys.F4) return SafeExecute(() => xoaTimKiem_ToolStripMenuItem.PerformClick());
                else if (modifier == Keys.Control && key == Keys.Q) return SafeExecute(() => quayLaiTrangXuLyData_ToolStripMenuItem.PerformClick());
            }
            catch (Exception ex) { LogError("Lỗi CmdKey", ex); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void InitFilters()
        {
            comboBox_TimKiemDonVi.SelectedIndexChanged -= ComboFilter_Changed;
            comboBox_XepLoaiThiDua.SelectedIndexChanged -= ComboFilter_Changed;
            comboBox1_GhiChu.SelectedIndexChanged -= ComboFilter_Changed;
            comboBox_TimKiemDonVi.SelectedIndexChanged += ComboFilter_Changed;
            comboBox_XepLoaiThiDua.SelectedIndexChanged += ComboFilter_Changed;
            comboBox1_GhiChu.SelectedIndexChanged += ComboFilter_Changed;
        }

        private void ComboFilter_Changed(object? sender, EventArgs e) => ApplyFilter_Virtual();

        // Placeholder dùng PlaceholderText của TextBox (không còn đổi Text/ForeColor bằng tay)
        private void InitPlaceHolder()
        {
            textBoxKyToon_TimTen.PlaceholderText = PLACEHOLDER_TIMTEN;
            textBoxKyToon_TimTen.TextChanged += (s, e) =>
            {
                if (Interlocked.CompareExchange(ref _isUpdatingUI, 0, 0) == 1) return;
                _searchTimer?.Stop();
                _searchTimer?.Start();
            };
            textBoxKyToon_TimTen.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    _searchTimer?.Stop();
                    ApplyFilter_Virtual();
                }
            };
        }

        private void kryptonButton1_TimKiemThongTinCBCSQuanLy_Click(object? sender, EventArgs e)
        {
            _searchTimer?.Stop();
            ApplyFilter_Virtual();
        }

        // ===== LƯỚI: định dạng + cột dựng 1 lần ở constructor =====
        private void TaoCotChoGridView()
        {
            var grid = kryptonDataGridView2;
            grid.SuspendLayout();

            try
            {
                grid.Columns.Clear();
                grid.AutoGenerateColumns = false;
                grid.AllowUserToAddRows = false;
                grid.AllowUserToDeleteRows = false;
                grid.AllowUserToResizeRows = false;
                grid.AllowUserToOrderColumns = false;
                grid.ReadOnly = true;
                grid.RowHeadersVisible = false;
                grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                grid.MultiSelect = false;
                grid.EnableHeadersVisualStyles = false;
                grid.BackgroundColor = Color.White;
                grid.BorderStyle = BorderStyle.None;
                grid.GridColor = Mau.VienNhat;
                grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
                grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

                // Kích thước tiêu đề
                grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                grid.ColumnHeadersHeight = 52;

                // Kích thước dòng dữ liệu
                grid.RowTemplate.Height = 36;
                grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
                grid.ScrollBars = ScrollBars.Vertical;
                grid.Font = FONT_LUOI;

                // Định dạng nội dung
                var cell = grid.DefaultCellStyle;
                cell.Font = FONT_LUOI;
                cell.BackColor = Color.White;
                cell.ForeColor = Mau.ChuDam;
                cell.SelectionBackColor = Mau.DongChon;
                cell.SelectionForeColor = Mau.XanhNhan;
                cell.Padding = new Padding(6, 0, 6, 0);

                grid.AlternatingRowsDefaultCellStyle.BackColor = Mau.DongLe;
                grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Mau.DongChon;
                grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Mau.XanhNhan;

                // Định dạng tiêu đề: tím nhạt
                var head = grid.ColumnHeadersDefaultCellStyle;
                head.Font = FONT_TIEUDE_LUOI;
                head.BackColor = Color.FromArgb(237, 233, 254); // #EDE9FE
                head.ForeColor = Color.FromArgb(91, 33, 182);   // #5B21B6
                head.SelectionBackColor = head.BackColor;
                head.SelectionForeColor = head.ForeColor;
                head.Alignment = DataGridViewContentAlignment.MiddleCenter;
                head.Padding = new Padding(6, 2, 6, 2);

                // Tạo các cột
                var cacCot = new DataGridViewColumn[CotLuoi.Length + 1];

                cacCot[0] = new DataGridViewTextBoxColumn
                {
                    Name = "STT",
                    HeaderText = "STT",
                    Width = 56,
                    ReadOnly = true,
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        Alignment = DataGridViewContentAlignment.MiddleCenter
                    }
                };

                for (int i = 0; i < CotLuoi.Length; i++)
                {
                    var d = CotLuoi[i];

                    cacCot[i + 1] = new DataGridViewTextBoxColumn
                    {
                        Name = d.Name,
                        HeaderText = d.Header,
                        ReadOnly = true,
                        SortMode = DataGridViewColumnSortMode.NotSortable,
                        AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                        FillWeight = d.Weight * 100f,
                        MinimumWidth = 60,
                        DefaultCellStyle = new DataGridViewCellStyle
                        {
                            Alignment = d.Trai
                                ? DataGridViewContentAlignment.MiddleLeft
                                : DataGridViewContentAlignment.MiddleCenter
                        }
                    };
                }

                grid.Columns.AddRange(cacCot);
                grid.RowCount = 0;
            }
            finally
            {
                grid.ResumeLayout();
            }
        }
        private void LoadComboPhanLoai()
        {
            var list = _dataGoc.Select(x => x.PhanLoai).Distinct().OrderBy(x => x).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            list.Insert(0, Module_HeThong.Tat_Ca);
            list.Add(Module_HeThong.PL_KHONG_PL);
            comboBox_XepLoaiThiDua.DataSource = list;
        }

        private void LoadComboDonVi()
        {
            var list = _dataGoc.Select(x => x.DonVi).Distinct().OrderBy(x => x).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            list.Insert(0, Module_HeThong.Tat_Ca);
            comboBox_TimKiemDonVi.DataSource = list;
        }

        private void LoadComboGhiChu()
        {
            var list = _dataGoc.Select(x => x.GhiChu).Distinct().OrderBy(x => x).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            list.Insert(0, Module_HeThong.Tat_Ca);
            comboBox1_GhiChu.DataSource = list;
        }

        private void ReloadComboSafe(bool giuLuaChon = false)
        {
            string cu1 = comboBox_TimKiemDonVi.Text, cu2 = comboBox_XepLoaiThiDua.Text, cu3 = comboBox1_GhiChu.Text;
            comboBox_TimKiemDonVi.SelectedIndexChanged -= ComboFilter_Changed;
            comboBox_XepLoaiThiDua.SelectedIndexChanged -= ComboFilter_Changed;
            comboBox1_GhiChu.SelectedIndexChanged -= ComboFilter_Changed;
            LoadComboDonVi();
            LoadComboPhanLoai();
            LoadComboGhiChu();
            if (giuLuaChon)
            {
                KhoiPhucLuaChon(comboBox_TimKiemDonVi, cu1);
                KhoiPhucLuaChon(comboBox_XepLoaiThiDua, cu2);
                KhoiPhucLuaChon(comboBox1_GhiChu, cu3);
            }
            comboBox_TimKiemDonVi.SelectedIndexChanged += ComboFilter_Changed;
            comboBox_XepLoaiThiDua.SelectedIndexChanged += ComboFilter_Changed;
            comboBox1_GhiChu.SelectedIndexChanged += ComboFilter_Changed;
        }

        private static void KhoiPhucLuaChon(ComboBox cb, string giaTriCu)
        {
            int i = string.IsNullOrEmpty(giaTriCu) ? -1 : cb.FindStringExact(giaTriCu);
            if (i >= 0) cb.SelectedIndex = i;
            else if (cb.Items.Count > 0) cb.SelectedIndex = 0;
        }

        private void ResetUI()
        {
            Interlocked.Exchange(ref _isUpdatingUI, 1);
            try
            {
                textBoxKyToon_TimTen.Text = string.Empty;
                if (comboBox_TimKiemDonVi.Items.Count > 0) comboBox_TimKiemDonVi.SelectedIndex = 0;
                if (comboBox_XepLoaiThiDua.Items.Count > 0) comboBox_XepLoaiThiDua.SelectedIndex = 0;
                if (comboBox1_GhiChu.Items.Count > 0) comboBox1_GhiChu.SelectedIndex = 0;
            }
            finally
            {
                Interlocked.Exchange(ref _isUpdatingUI, 0);
            }
        }

        private void CapNhatThongKeDonVi(int soDongChis, int soDonVi)
        {
            toolStripStatusLabel4.Text = $"Kết quả: {soDongChis} {Module_HeThong.Tu_dong_chi} - {soDonVi} đơn vị";
        }

        private async void lamMoi_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref _isLoading, 1) == 1)
                return;
            using Form_Loading frmLoad = new Form_Loading("Đang làm mới dữ liệu...");
            try
            {
                toolStripStatusLabel4.Text = "Đang kết nối CSDL...";
                Enabled = false;
                frmLoad.Show(this);
                ResetUI();
                if (_cts.IsCancellationRequested)
                {
                    _cts.Dispose();
                    _cts = new CancellationTokenSource();
                }
                string key = LayDauVanTayCSDL();
                _dataGoc = await DocDanhSachNenAsync(_cts.Token);
                _dauVanTay = key;
                frmLoad.CapNhatThongBao("Đang cập nhật giao diện...");
                ReloadComboSafe();
                ApplyFilter_Virtual();
                toolStripStatusLabel4.Text = $"Đã tải {_dataGoc.Count:N0} dữ liệu";
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                toolStripStatusLabel4.Text = "Lỗi";
                LogError(nameof(lamMoi_ToolStripMenuItem_Click), ex);
                MessageBox.Show(
                    $"Không thể làm mới dữ liệu.\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                Enabled = true;
                if (!IsDisposed)
                    Focus();
                Interlocked.Exchange(ref _isLoading, 0);
            }
        }

        private void quayLaiTrangXuLyData_ToolStripMenuItem_Click(object? sender, EventArgs e) => this.Close();

        private void kryptonButton1_Dong_Click(object? sender, EventArgs e)
        {
            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            if (formCha != null)
            {
                formCha.CapNhatTieuDe("Trang phân loại thi đua");
            }
            this.Close();
        }

        private void xoaTimKiem_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            _searchTimer?.Stop();
            ResetUI();
            kryptonDataGridView2.Focus();
            ApplyFilter_Virtual();
        }

        private void LogError(string context, Exception ex)
        {
            try { System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] [ERROR] {context}: {ex.Message}"); } catch { }
        }

        // ===== DỌN DẸP KHI ĐÓNG FORM =====
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                _cts?.Cancel();
                if (_searchTimer != null)
                {
                    _searchTimer.Stop();
                    _searchTimer.Tick -= SearchTimer_Tick;
                    _searchTimer.Dispose();
                    _searchTimer = null;
                }
                var g = kryptonDataGridView2;
                if (g != null)
                {
                    g.CellValueNeeded -= KryptonDataGridView2_CellValueNeeded;
                    g.CellPainting -= KryptonDataGridView2_CellPainting;
                    g.CellFormatting -= Luoi_CellFormatting;
                    g.CellMouseEnter -= Luoi_CellMouseEnter;
                    g.MouseLeave -= Luoi_MouseLeave;
                }
                _iconLoai1?.Dispose();
                _iconLoai1 = null;
            }
            catch
            {
            }
            base.OnFormClosing(e);
        }

        private static string NormalizePhanLoai(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Không PL";
            value = value.Trim();
            return value switch
            {
                "Loai 1" => Module_HeThong.Loai_1,
                "Loai 2" => Module_HeThong.Loai_2,
                "Loai 3" => Module_HeThong.Loai_3,
                "Loai 4" => Module_HeThong.Loai_4,
                _ => value
            };
        }

        private void UpdateThongKePhanLoai()
        {
            if (Interlocked.Exchange(ref _isUpdatingThongKe, 1) == 1)
                return;
            try
            {
                if (IsDisposed || !IsHandleCreated)
                    return;
                if (_viewData == null || _viewData.Count == 0)
                {
                    ResetThongKeLabels();
                    return;
                }
                int loai1 = 0, loai2 = 0, loai3 = 0, loai4 = 0, khongPL = 0;
                foreach (var item in _viewData)
                {
                    string pl = NormalizePhanLoai(item?.PhanLoai);
                    switch (pl)
                    {
                        case Module_HeThong.Loai_1: loai1++; break;
                        case Module_HeThong.Loai_2: loai2++; break;
                        case Module_HeThong.Loai_3: loai3++; break;
                        case Module_HeThong.Loai_4: loai4++; break;
                        default: khongPL++; break;
                    }
                }
                UpdateThongKeLabel(toolStripStatusLabel_TongSoLoai1, loai1, Module_HeThong.Loai_1, COLOR_LOAI_1);
                UpdateThongKeLabel(toolStripStatusLabel_TongSoLoai2, loai2, Module_HeThong.Loai_2, COLOR_LOAI_2);
                UpdateThongKeLabel(toolStripStatusLabel_TongSoLoai3, loai3, Module_HeThong.Loai_3, COLOR_LOAI_3);
                UpdateThongKeLabel(toolStripStatusLabel_TongSoLoai4, loai4, Module_HeThong.Loai_4, COLOR_LOAI_4);
                UpdateThongKeLabel(toolStripStatusLabel_TongSoKhongPhanLoai, khongPL, Module_HeThong.PL_KHONG_PL, COLOR_KHONG_PL);
            }
            catch (Exception ex)
            {
                LogError("Lỗi UpdateThongKePhanLoai", ex);
            }
            finally
            {
                Interlocked.Exchange(ref _isUpdatingThongKe, 0);
            }
        }

        private static void UpdateThongKeLabel(ToolStripStatusLabel label, int count, string title, Color color)
        {
            if (label == null)
                return;
            if (count <= 0)
            {
                label.Visible = false;
                return;
            }
            label.Visible = true;
            label.Text = $"{title}: {count:N0} {Module_HeThong.Tu_dong_chi}";
            label.ForeColor = color;
            label.Font = THONGKE_FONT;
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.AutoSize = false;
            label.Width = 160;
        }

        private void ResetThongKeLabels()
        {
            toolStripStatusLabel_TongSoLoai1.Visible = false;
            toolStripStatusLabel_TongSoLoai2.Visible = false;
            toolStripStatusLabel_TongSoLoai3.Visible = false;
            toolStripStatusLabel_TongSoLoai4.Visible = false;
            toolStripStatusLabel_TongSoKhongPhanLoai.Visible = false;
        }

        private void kryptonButton_LamMoiCacOTimKiem_Click(object? sender, EventArgs e)
        {
            _searchTimer?.Stop();
            ResetUI();
            kryptonDataGridView2.Focus();
            ApplyFilter_Virtual();
        }
    }

    //Lê Trung Kiên
    // Đã đổi tên thành CBCSQuanLyModel để tránh xung đột (ambiguity) với các form khác
    public class CBCSQuanLyModel
    {
        public string HoVaTen { get; set; } = string.Empty;
        public string SoHieu { get; set; } = string.Empty;
        public string NamSinh { get; set; } = string.Empty;
        public string QueQuan { get; set; } = string.Empty;
        public string NgayVaoCAND { get; set; } = string.Empty;
        public string CapBac { get; set; } = string.Empty;
        public string ChucVu { get; set; } = string.Empty;
        public string DonVi { get; set; } = string.Empty;
        public string PhanLoai { get; set; } = string.Empty;
        public string GhiChu { get; set; } = string.Empty;
    }
}