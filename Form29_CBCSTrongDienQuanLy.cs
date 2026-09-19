using ClosedXML.Excel;
using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Runtime.InteropServices;
namespace PhanMemThiDua2026
{
    public partial class Form29_CBCSTrongDienQuanLy : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private readonly string _connStr;
        // ⭐ 1. TỐI ƯU RAM: Dùng List với Model Độc lập (Không đụng hàng với project gốc)
        private List<CBCSQuanLyModel> _dataGoc = new List<CBCSQuanLyModel>();
        private List<CBCSQuanLyModel> _viewData = new List<CBCSQuanLyModel>();
        private const string PLACEHOLDER_TIMTEN = Module_HeThong.Nhap_Tim_Kiem;
        private bool _isPlaceholderActive = true;
        // ⭐ 2. CHỐNG TREO UI ĐA LUỒNG: Dùng Interlocked thay cho bool
        private int _isUpdatingUI = 0;
        private int _isLoading = 0;
        // ⭐ 3. ANTI-ZOMBIE TASK: Hủy an toàn khi đóng form
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Font _headerFont = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Bold);
        private readonly Dictionary<string, string> _tenCotTiengViet = new Dictionary<string, string>{
            {"HoVaTen","Họ và tên"}, {"SoHieu","Số hiệu"}, {"NamSinh","Năm sinh"},
            {"QueQuan","Quê quán"}, {"NgayVaoCAND","Vào CAND"}, {"CapBac","Cấp bậc"},
            {"ChucVu","Chức vụ"}, {"DonVi","Đơn vị"}, {"PhanLoai","Phân loại"}, {"GhiChu","Ghi chú"}
        };
        private System.Windows.Forms.Timer _searchTimer;
        // ⭐ 4. TỐI ƯU GDI+: Khởi tạo & Resize ảnh 1 lần duy nhất
        private readonly Bitmap _iconLoai1;
        // ⭐ 5. ANTI-FLICKER: Tắt vẽ màn hình lúc nạp dữ liệu lớn
        private const int WM_SETREDRAW = 11;
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, bool wParam, int lParam);
        private static readonly Font THONGKE_FONT = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Bold);
        // CACHE MÀU THỐNG KÊ
        private static readonly Color COLOR_LOAI_1 = Color.FromArgb(0, 102, 204); // xanh dương
        private static readonly Color COLOR_LOAI_2 = Color.FromArgb(204, 153, 0); // vàng đậm dễ đọc
        private static readonly Color COLOR_LOAI_3 = Color.FromArgb(220, 53, 69); // đỏ
        private static readonly Color COLOR_LOAI_4 = Color.FromArgb(180, 0, 0);   // đỏ đậm
        private static readonly Color COLOR_KHONG_PL = Color.Black;
        // CHỐNG UPDATE UI CHỒNG CHÉO
        private int _isUpdatingThongKe = 0;
        public Form29_CBCSTrongDienQuanLy()
        {
            InitializeComponent();
            // Kích hoạt DoubleBuffered qua Reflection an toàn
            try
            {
                typeof(DataGridView).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                  ?.SetValue(kryptonDataGridView2, true, null);
            }
            catch { }
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            ShowInTaskbar = false;
            kryptonDataGridView2.Dock = DockStyle.Fill;
            kryptonDataGridView2.VirtualMode = true;
            kryptonDataGridView2.CellValueNeeded += KryptonDataGridView2_CellValueNeeded;
            kryptonDataGridView2.CellPainting += KryptonDataGridView2_CellPainting;
            // ⭐ BỔ SUNG 2 DÒNG NÀY ĐỂ CHỌN CẢ DÒNG HÀNG NGANG KHI NGƯỜI DÙNG CLICK
            kryptonDataGridView2.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            kryptonDataGridView2.MultiSelect = false; // Chặn chọn nhiều dòng cùng lúc nếu không cần thiết
            _connStr = $"Data Source={_csdl2Path};Pooling=True;";
            if (Properties.Resources.ic_khenthuong != null)
            {
                _iconLoai1 = new Bitmap(Properties.Resources.ic_khenthuong, new Size(16, 16));
            }
        }
        // Lê Trung Kiên -  Yêu mèo cam
        // Cache Font dùng cho trạng thái "tải thành công" - tránh tạo mới đối tượng Font
        // (giữ GDI handle) mỗi lần Load chạy lại, chỉ tạo 1 lần và tái sử dụng.
        private Font _fontTrangThaiThanhCong;
        private async void Form29_CBCSTrongDienQuanLy_Load(object? sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref _isLoading, 1) == 1)
                return;
            try
            {
                // BƯỚC 1 — UI THUẦN, KHÔNG phụ thuộc dữ liệu (_dataGoc).
                // Chạy ngay lập tức, đồng bộ, rẻ -> form phản hồi nhanh nhất có thể
                // ngay khi Load, thay vì phải đợi hết chuỗi xử lý phía sau.
                // (Giả định TaoCotChoGridView() chỉ định nghĩa cấu trúc cột cố định,
                // KHÔNG phụ thuộc nội dung dữ liệu thực tế - nếu không đúng, dời lại
                // xuống sau BƯỚC 2.)
                Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1);
                InitPlaceHolder();
                InitSearchTimer();
                TaoCotChoGridView();
                toolStripStatusLabel4.Text = "Đang tải dữ liệu...";
                // BƯỚC 2 — TẢI DỮ LIỆU (I/O, tốn thời gian). Chỉ tải khi cache rỗng.
                if (_dataGoc.Count == 0)
                {
                    _dataGoc = await LoadDanhSachAsync(_cts.Token);
                    // Thoát sớm nếu form đã đóng / bị hủy trong lúc chờ - tránh
                    // thao tác trên control đã Dispose (ObjectDisposedException).
                    if (IsDisposed || _cts.IsCancellationRequested)
                        return;
                }
                // BƯỚC 3 — CÁC THAO TÁC PHỤ THUỘC DỮ LIỆU, phải chạy SAU await,
                // đúng thứ tự phụ thuộc: Combo (cần _dataGoc) -> Filter (cần Combo)
                // -> ApplyFilter_Virtual (render, phải chạy SAU CÙNG và chỉ 1 lần).
                //
                // Gom trong SuspendLayout/ResumeLayout để tránh grid vẽ lại nhiều
                // lần khi vừa nạp combo vừa bind dữ liệu -> mượt hơn, đỡ giật/nháy.
                kryptonDataGridView2.SuspendLayout();
                try
                {
                    LoadComboDonVi();
                    LoadComboPhanLoai();
                    LoadComboGhiChu();
                    InitFilters();
                    ApplyFilter_Virtual();
                }
                finally
                {
                    kryptonDataGridView2.ResumeLayout();
                }
                // BƯỚC 4 — CẬP NHẬT LABEL THỐNG KÊ, thực hiện SAU CÙNG vì phụ
                // thuộc kết quả cuối (tongSo) sau khi đã lọc/hiển thị xong.
                int tongSo = _dataGoc.Count; // bỏ "?." thừa: _dataGoc đã được đảm bảo non-null ở BƯỚC 2
                toolStripStatusLabel4.Visible = tongSo > 0;
                if (tongSo > 0)
                {
                    toolStripStatusLabel4.Text = $"Tổng cộng {tongSo:N0} {Module_HeThong.Tu_dong_chi}";
                }
                toolStripStatusLabel4.ForeColor = Color.FromArgb(25, 135, 84); // xanh lá đẹp
                _fontTrangThaiThanhCong ??= new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Bold);
                toolStripStatusLabel4.Font = _fontTrangThaiThanhCong;
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
        // Trong Dispose(bool disposing) của Form, nhớ thêm:
        // if (disposing) _fontTrangThaiThanhCong?.Dispose();
        // ⭐ LÕI DỮ LIỆU DATA ACCESS (CACHE AES VÀ STRING POOL)
        private async Task<List<CBCSQuanLyModel>> LoadDanhSachAsync(CancellationToken token)
        {
            var list = new List<CBCSQuanLyModel>(5000);
            if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path)) return list;
            const string sql = @"
                SELECT HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu 
                FROM DanhSach 
                WHERE LENGTH(TRIM(IFNULL(GhiChu, ''))) > 0";
            var aesCache = new Dictionary<string, string>(10000, StringComparer.Ordinal);
            var stringPool = new Dictionary<string, string>(5000, StringComparer.Ordinal);
            await using var conn = new SqliteConnection(_connStr);
            await conn.OpenAsync(token);
            await using var cmd = new SqliteCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(token);
            int recordCount = 0;
            while (await reader.ReadAsync(token))
            {
                token.ThrowIfCancellationRequested();
                if (++recordCount > 500000) throw new Exception("Database quá lớn (>500k).");
                string DecryptAndPool(int index)
                {
                    if (reader.IsDBNull(index)) return string.Empty;
                    string raw = string.Empty;
                    try { raw = reader.GetString(index); } catch { return string.Empty; }
                    if (string.IsNullOrEmpty(raw)) return string.Empty;
                    if (!aesCache.TryGetValue(raw, out string decrypted))
                    {
                        try { decrypted = Module_BaoMatAES.GiaiMa(raw)?.Trim() ?? string.Empty; } catch { decrypted = string.Empty; }
                        aesCache[raw] = decrypted;
                    }
                    if (decrypted.Length > 0)
                    {
                        if (!stringPool.TryGetValue(decrypted, out string pooled))
                        {
                            pooled = decrypted;
                            stringPool[pooled] = pooled;
                        }
                        return pooled;
                    }
                    return string.Empty;
                }
                list.Add(new CBCSQuanLyModel
                {
                    HoVaTen = DecryptAndPool(0),
                    SoHieu = DecryptAndPool(1),
                    NamSinh = DecryptAndPool(2),
                    QueQuan = DecryptAndPool(3),
                    NgayVaoCAND = DecryptAndPool(4),
                    CapBac = DecryptAndPool(5),
                    ChucVu = DecryptAndPool(6),
                    DonVi = DecryptAndPool(7),
                    PhanLoai = DecryptAndPool(8),
                    GhiChu = DecryptAndPool(9)
                });
            }
            aesCache.Clear();
            stringPool.Clear();
            return list;
        }
        // ⭐ BỘ LỌC LINQ VÀ HIỂN THỊ VIRTUAL MODE
        private void ApplyFilter_Virtual()
        {
            if (IsDisposed || !IsHandleCreated)
                return;
            try
            {
                IEnumerable<CBCSQuanLyModel> query = _dataGoc;
                string keyword = (!_isPlaceholderActive &&
                                 !string.IsNullOrWhiteSpace(textBoxKyToon_TimTen.Text))
                                 ? textBoxKyToon_TimTen.Text.Trim()
                                 : string.Empty;
                string donVi = comboBox_TimKiemDonVi.Text?.Trim() ?? "";
                string phanLoai = comboBox_XepLoaiThiDua.Text?.Trim() ?? "";
                string ghiChu = comboBox1_GhiChu.Text?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    query = query.Where(x =>
                        (!string.IsNullOrWhiteSpace(x.HoVaTen) &&
                         x.HoVaTen.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        ||
                        (!string.IsNullOrWhiteSpace(x.SoHieu) &&
                         x.SoHieu.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
                }
                if (!string.IsNullOrWhiteSpace(donVi) &&
                    donVi != Module_HeThong.Tat_Ca)
                {
                    query = query.Where(x => x.DonVi == donVi);
                }
                if (!string.IsNullOrWhiteSpace(phanLoai) &&
                    phanLoai != Module_HeThong.Tat_Ca)
                {
                    if (phanLoai == "Không PL")
                    {
                        query = query.Where(x =>
                            string.IsNullOrWhiteSpace(x.PhanLoai));
                    }
                    else
                    {
                        query = query.Where(x => x.PhanLoai == phanLoai);
                    }
                }
                if (!string.IsNullOrWhiteSpace(ghiChu) &&
                    ghiChu != Module_HeThong.Tat_Ca)
                {
                    query = query.Where(x => x.GhiChu == ghiChu);
                }
                _viewData = query.ToList();
                SendMessage(kryptonDataGridView2.Handle, WM_SETREDRAW, false, 0);
                try
                {
                    kryptonDataGridView2.SuspendLayout();
                    kryptonDataGridView2.CurrentCell = null;
                    kryptonDataGridView2.RowCount = _viewData.Count;
                }
                finally
                {
                    kryptonDataGridView2.ResumeLayout();
                    SendMessage(
                        kryptonDataGridView2.Handle,
                        WM_SETREDRAW,
                        true,
                        0);
                    kryptonDataGridView2.Refresh();
                }
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
            try
            {
                if (_viewData == null || e.RowIndex < 0 || e.RowIndex >= _viewData.Count)
                {
                    e.Value = string.Empty; return;
                }
                if (e.ColumnIndex == 0) { e.Value = e.RowIndex + 1; return; }
                var item = _viewData[e.RowIndex];
                string colName = kryptonDataGridView2.Columns[e.ColumnIndex].Name;
                e.Value = colName switch
                {
                    "HoVaTen" => item.HoVaTen,
                    "SoHieu" => item.SoHieu,
                    "NamSinh" => item.NamSinh,
                    "QueQuan" => item.QueQuan,
                    "NgayVaoCAND" => item.NgayVaoCAND,
                    "CapBac" => item.CapBac,
                    "ChucVu" => item.ChucVu,
                    "DonVi" => item.DonVi,
                    "PhanLoai" => item.PhanLoai,
                    "GhiChu" => item.GhiChu,
                    _ => string.Empty
                };
            }
            catch { e.Value = "..."; }
        }
        private void KryptonDataGridView2_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv == null || e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgv.Columns[e.ColumnIndex].Name == "HoVaTen")
            {
                if (_viewData == null || e.RowIndex >= _viewData.Count) return;
                string phanLoai = _viewData[e.RowIndex].PhanLoai ?? string.Empty;
                if (string.Equals(phanLoai, Module_HeThong.Loai_1, StringComparison.OrdinalIgnoreCase) && _iconLoai1 != null)
                {
                    DataGridViewPaintParts parts = DataGridViewPaintParts.Background | DataGridViewPaintParts.SelectionBackground | DataGridViewPaintParts.Focus;
                    e.Paint(e.CellBounds, parts);
                    int iconSize = 16;
                    int xIcon = e.CellBounds.X + 4;
                    int yIcon = e.CellBounds.Y + (e.CellBounds.Height - iconSize) / 2;
                    e.Graphics.DrawImageUnscaled(_iconLoai1, xIcon, yIcon);
                    string hoTen = e.Value?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(hoTen))
                    {
                        int textStartX = xIcon + iconSize + 4;
                        Rectangle textBounds = new Rectangle(textStartX, e.CellBounds.Y, e.CellBounds.Width - (textStartX - e.CellBounds.X), e.CellBounds.Height);
                        Color textColor = (e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected ? e.CellStyle.SelectionForeColor : e.CellStyle.ForeColor;
                        TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;
                        TextRenderer.DrawText(e.Graphics, hoTen, e.CellStyle.Font, textBounds, textColor, flags);
                    }
                    e.Handled = true;
                }
            }
        }
        // ⭐ XUẤT EXCEL ASYNC BẢO VỆ UI (Chống treo Not Responding)
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
                // 🔥 GỌI HÀM UX MỚI: Tự động highlight file, không mở bừa bãi cửa sổ
                // (Hàm này bên trong đã tự check File.Exists rồi nên bạn không cần check lại nữa)
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
            // 1. TỐI ƯU RAM: Sử dụng IEnumerable để nạp dữ liệu kiểu luồng (Stream)
            var dataProjection = data.Select((row, i) => new object[] {
        i + 1, row.HoVaTen, row.SoHieu, row.NamSinh, row.QueQuan,
        row.NgayVaoCAND, row.CapBac, row.ChucVu, row.DonVi, row.PhanLoai, row.GhiChu
    });
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("DanhSach");
            // ⭐ BƯỚC QUAN TRỌNG: THIẾT LẬP FONT MẶC ĐỊNH CHO TOÀN BỘ TRANG TÍNH
            ws.Style.Font.SetFontName(Module_HeThong.Font_Times_New_Roman);
            ws.Style.Font.SetFontSize(12); // Kích cỡ chuẩn 12 cho nội dung
            // 2. TIÊU ĐỀ BÁO CÁO (A1, A2)
            ws.Range("A1:K1").Merge().Value = "DANH SÁCH";
            ws.Cell("A1").Style.Font.SetBold(true).Font.SetFontSize(16).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Range("A2:K2").Merge().Value = $"CBCS TRONG DIỆN QUẢN LÝ THÁNG {DateTime.Now.Month}/{DateTime.Now.Year}";
            ws.Cell("A2").Style.Font.SetBold(true).Font.SetItalic(true).Font.SetFontSize(13).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            // 3. NẠP DỮ LIỆU CHI TIẾT (Bắt đầu từ dòng 5)
            ws.Cell(5, 1).InsertData(dataProjection);
            // 4. THIẾT LẬP NỘI DUNG DÒNG TIÊU ĐỀ (Dòng 4)
            int headerRow = 4;
            string[] headerTitles = { "STT", "Họ và tên", "Số hiệu", "Năm sinh", "Quê quán", "Vào CAND", "Cấp bậc", "Chức vụ", "Đơn vị", "Phân loại", "Ghi chú" };
            for (int i = 0; i < headerTitles.Length; i++)
            {
                ws.Cell(headerRow, i + 1).Value = headerTitles[i];
            }
            ws.Row(headerRow).Height = 25;
            // 5. ĐỊNH DẠNG ĐỘ RỘNG VÀ CĂN LỀ CHI TIẾT
            double[] colWidths = { 5, 25, 12, 10, 30, 12, 14, 15, 15, 12, 40 };
            for (int i = 0; i < colWidths.Length; i++)
            {
                int colIdx = i + 1;
                var col = ws.Column(colIdx);
                col.Width = colWidths[i];
                // Căn giữa cho các cột số liệu ngắn
                if (new[] { 1, 3, 4, 6, 7, 8, 9, 10 }.Contains(colIdx))
                {
                    col.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    col.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                else // Căn trái cho nội dung dài
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
            // 6. KẺ KHUNG CHO TOÀN BẢNG DỮ LIỆU
            int lastDataRow = 5 + data.Count - 1;
            var tableRange = ws.Range(headerRow, 1, lastDataRow, 11);
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            // 7. DÒNG TỔNG CỘNG
            var footerCell = ws.Cell(lastDataRow + 1, 1);
            footerCell.Value = $"Tổng cộng: {soLuong} {Module_HeThong.Tu_dong_chi}/.";
            footerCell.Style.Font.SetBold(true).Font.SetItalic(true).Font.SetFontSize(12);
            footerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            // ⭐ BƯỚC CUỐI CÙNG: OVERRIDE TIÊU ĐỀ BẢNG (ÉP CHUẨN TRUNG TÂM)
            var rangeHeaderTable = ws.Range("A4:K4");
            rangeHeaderTable.Style.Font.Bold = true;
            rangeHeaderTable.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8E8E8");
            rangeHeaderTable.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // Căn giữa ngang
            rangeHeaderTable.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;   // Căn giữa dọc
            // 🔥 GỌI MODULE ĐÓNG DẤU BẢN QUYỀN TẠI ĐÂY (TRƯỚC KHI LƯU) 🔥
            Module_BanQuyen.DongDauExcel(wb);
            // LƯU FILE
            wb.SaveAs(filePath);
        }
        private void InitSearchTimer()
        {
            if (_searchTimer != null)
                return;
            _searchTimer = new System.Windows.Forms.Timer
            {
                Interval = 300
            };
            _searchTimer.Tick += SearchTimer_Tick;
        }
        private void SearchTimer_Tick(object? sender, EventArgs e)
        {
            _searchTimer.Stop();
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
        private void InitPlaceHolder()
        {
            textBoxKyToon_TimTen.Text = PLACEHOLDER_TIMTEN;
            textBoxKyToon_TimTen.ForeColor = Color.Gray;
            textBoxKyToon_TimTen.Enter += (s, e) =>
            {
                if (_isPlaceholderActive || textBoxKyToon_TimTen.Text == PLACEHOLDER_TIMTEN)
                {
                    textBoxKyToon_TimTen.Text = "";
                    textBoxKyToon_TimTen.ForeColor = Color.Black;
                    _isPlaceholderActive = false;
                }
            };
            textBoxKyToon_TimTen.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBoxKyToon_TimTen.Text))
                {
                    textBoxKyToon_TimTen.Text = PLACEHOLDER_TIMTEN;
                    textBoxKyToon_TimTen.ForeColor = Color.Gray;
                    _isPlaceholderActive = true;
                }
            };
            textBoxKyToon_TimTen.TextChanged += (s, e) =>
            {
                _searchTimer.Stop();
                _searchTimer.Start();
            };
        }
        private void kryptonButton1_TimKiemThongTinCBCSQuanLy_Click(object sender, EventArgs e) => ApplyFilter_Virtual();
        private void TaoCotChoGridView()
        {
            var grid = kryptonDataGridView2;
            grid.SuspendLayout();
            try
            {
                grid.Columns.Clear();
                // 🌟 1. ÁP DỤNG PHONG CÁCH "WEB DESIGN": KHOẢNG TRẮNG, MÀU SẮC, FONT
                // 1.1 Chiều cao và khoảng trống (Whitespace)
                grid.RowTemplate.Height = 36; // Dòng cao thoáng đãng
                grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None; // Tối ưu cuộn
                grid.AllowUserToResizeRows = false;
                grid.ColumnHeadersHeight = 60; // Tiêu đề to, rộng như web
                grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                // 1.2 Font và Padding cho Tiêu đề (Header)
                grid.StateCommon.HeaderColumn.Content.Padding = new System.Windows.Forms.Padding(6, 8, 6, 8);
                grid.StateCommon.HeaderColumn.Content.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 9F, System.Drawing.FontStyle.Bold);
                grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                // 1.3 Font và Padding cho Dữ liệu (Cell)
                grid.StateCommon.DataCell.Content.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 9F, System.Drawing.FontStyle.Regular);
                grid.StateCommon.DataCell.Content.Padding = new System.Windows.Forms.Padding(6, 8, 6, 8); // Đệm chữ vào giữa ô
                // 1.4 Viền phẳng (Flat Border) tinh tế màu xám nhạt
                grid.StateCommon.DataCell.Border.Color1 = System.Drawing.Color.FromArgb(224, 224, 224);
                grid.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
                grid.StateCommon.DataCell.Border.Width = 1;
                // 1.5 Màu chọn dòng (Selection State) dịu mắt: Nền xanh nhạt + Chữ xanh dương
                grid.StateSelected.DataCell.Back.Color1 = System.Drawing.Color.FromArgb(232, 244, 253);
                grid.StateSelected.DataCell.Back.Color2 = System.Drawing.Color.FromArgb(232, 244, 253);
                grid.StateSelected.DataCell.Content.Color1 = System.Drawing.Color.FromArgb(0, 102, 204);
                // 1.6 Cấu hình Grid cơ bản
                grid.GridStyles.Style = Krypton.Toolkit.DataGridViewStyle.List;
                grid.RowHeadersVisible = false;
                grid.AllowUserToAddRows = false;
                grid.ScrollBars = ScrollBars.Vertical;
                // Padding đáy nếu cần không gian thừa
                grid.Margin = new Padding(0, 0, 0, 30);
                // 🌟 2. TẠO CỘT VÀ TỐI ƯU VỊ TRÍ
                // Cột STT
                var colSTT = new DataGridViewTextBoxColumn
                {
                    Name = "STT",
                    HeaderText = "STT",
                    Width = 50, // Chuẩn Form Web (Gọn gàng)
                    ReadOnly = true,
                    Frozen = true,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter },
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                };
                grid.Columns.Add(colSTT);
                // Danh sách cột canh trái
                var leftAlignCols = new HashSet<string>(StringComparer.Ordinal) { "HoVaTen", "QueQuan", "GhiChu", "DonVi", "ChucVu" };
                foreach (var kvp in _tenCotTiengViet)
                {
                    var col = new DataGridViewTextBoxColumn
                    {
                        Name = kvp.Key,
                        HeaderText = kvp.Value,
                        ReadOnly = true,
                        DefaultCellStyle = new DataGridViewCellStyle
                        {
                            Alignment = leftAlignCols.Contains(kvp.Key)
                                        ? DataGridViewContentAlignment.MiddleLeft
                                        : DataGridViewContentAlignment.MiddleCenter
                        },
                        // Chống sắp xếp khi click tiêu đề
                        SortMode = DataGridViewColumnSortMode.NotSortable
                    };
                    // Chia tỉ lệ (FillWeight) cho các cột dài
                    if (kvp.Key == "HoVaTen" || kvp.Key == "QueQuan" || kvp.Key == "GhiChu")
                        col.FillWeight = 3f;
                    else if (kvp.Key == "DonVi" || kvp.Key == "ChucVu")
                        col.FillWeight = 2f;
                    else
                        col.FillWeight = 1f;
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    grid.Columns.Add(col);
                }
                // 🌟 3. KHÓA CHẶT THỨ TỰ HIỂN THỊ (DISPLAYINDEX)
                string[] columnOrder = {
            "STT", "HoVaTen", "SoHieu", "NamSinh", "QueQuan",
            "NgayVaoCAND", "CapBac", "ChucVu", "DonVi", "PhanLoai", "GhiChu"
        };
                for (int i = 0; i < columnOrder.Length; i++)
                {
                    if (grid.Columns.Contains(columnOrder[i]))
                    {
                        grid.Columns[columnOrder[i]].DisplayIndex = i;
                    }
                }
                // Kích hoạt Virtual Mode sau khi vẽ cột (nếu RowCount đang > 0)
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
        private void ReloadComboSafe()
        {
            comboBox_TimKiemDonVi.SelectedIndexChanged -= ComboFilter_Changed;
            comboBox_XepLoaiThiDua.SelectedIndexChanged -= ComboFilter_Changed;
            comboBox1_GhiChu.SelectedIndexChanged -= ComboFilter_Changed;
            LoadComboDonVi();
            LoadComboPhanLoai();
            LoadComboGhiChu();
            comboBox_TimKiemDonVi.SelectedIndexChanged += ComboFilter_Changed;
            comboBox_XepLoaiThiDua.SelectedIndexChanged += ComboFilter_Changed;
            comboBox1_GhiChu.SelectedIndexChanged += ComboFilter_Changed;
        }
        private void ResetUI()
        {
            Interlocked.Exchange(ref _isUpdatingUI, 1);
            textBoxKyToon_TimTen.Text = PLACEHOLDER_TIMTEN;
            textBoxKyToon_TimTen.ForeColor = Color.Gray;
            _isPlaceholderActive = true;
            if (comboBox_TimKiemDonVi.Items.Count > 0) comboBox_TimKiemDonVi.SelectedIndex = 0;
            if (comboBox_XepLoaiThiDua.Items.Count > 0) comboBox_XepLoaiThiDua.SelectedIndex = 0;
            if (comboBox1_GhiChu.Items.Count > 0) comboBox1_GhiChu.SelectedIndex = 0;
            Interlocked.Exchange(ref _isUpdatingUI, 0);
        }
        private void CapNhatThongKeDonVi(int soDongChis, int soDonVi)
        {
            toolStripStatusLabel4.Text = $"Kết quả: {soDongChis} {Module_HeThong.Tu_dong_chi} - {soDonVi} đơn vị";
        }
        private async void lamMoi_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref _isLoading, 1) == 1)
                return;
            using Form_Loading frmLoad =
                new Form_Loading("Đang làm mới dữ liệu...");
            try
            {
                toolStripStatusLabel4.Text = "Đang kết nối CSDL...";
                Enabled = false;
                frmLoad.Show(this);
                await Task.Delay(50);
                ResetUI();
                if (_cts.IsCancellationRequested)
                {
                    _cts.Dispose();
                    _cts = new CancellationTokenSource();
                }
                _dataGoc = await LoadDanhSachAsync(_cts.Token);
                frmLoad.CapNhatThongBao("Đang cập nhật giao diện...");
                ReloadComboSafe();
                ApplyFilter_Virtual();
                toolStripStatusLabel4.Text =
                    $"Đã tải {_dataGoc.Count:N0} dữ liệu";
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
            // 1. Tìm Form cha đang mở
            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            // 2. Nếu tìm thấy Form cha thì cập nhật lại tiêu đề
            if (formCha != null)
            {
                formCha.CapNhatTieuDe("Trang phân loại thi đua");
            }
            // 3. Đóng form hiện tại
            this.Close();
        }
        private void xoaTimKiem_ToolStripMenuItem_Click(object sender, EventArgs e)
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
        // ⭐ DỌN DẸP RÁC KHI ĐÓNG FORM
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
                if (kryptonDataGridView2 != null)
                {
                    kryptonDataGridView2.CellValueNeeded -= KryptonDataGridView2_CellValueNeeded;
                    kryptonDataGridView2.CellPainting -= KryptonDataGridView2_CellPainting;
                }
                _iconLoai1?.Dispose();
                _headerFont?.Dispose();
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
            // chống update chồng
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
                int loai1 = 0;
                int loai2 = 0;
                int loai3 = 0;
                int loai4 = 0;
                int khongPL = 0;
                // DUYỆT 1 LẦN DUY NHẤT -> HIỆU NĂNG CAO
                foreach (var item in _viewData)
                {
                    string pl = NormalizePhanLoai(item?.PhanLoai);
                    switch (pl)
                    {
                        case Module_HeThong.Loai_1:
                            loai1++;
                            break;
                        case Module_HeThong.Loai_2:
                            loai2++;
                            break;
                        case Module_HeThong.Loai_3:
                            loai3++;
                            break;
                        case Module_HeThong.Loai_4:
                            loai4++;
                            break;
                        default:
                            khongPL++;
                            break;
                    }
                }
                // cập nhật UI
                UpdateThongKeLabel(
                    toolStripStatusLabel_TongSoLoai1,
                    loai1,
                    Module_HeThong.Loai_1,
                    COLOR_LOAI_1);
                UpdateThongKeLabel(
                    toolStripStatusLabel_TongSoLoai2,
                    loai2,
                    Module_HeThong.Loai_2,
                    COLOR_LOAI_2);
                UpdateThongKeLabel(
                    toolStripStatusLabel_TongSoLoai3,
                    loai3,
                    Module_HeThong.Loai_3,
                    COLOR_LOAI_3);
                UpdateThongKeLabel(
                    toolStripStatusLabel_TongSoLoai4,
                    loai4,
                  Module_HeThong.Loai_4,
                    COLOR_LOAI_4);
                UpdateThongKeLabel(
                    toolStripStatusLabel_TongSoKhongPhanLoai,
                    khongPL,
                    Module_HeThong.PL_KHONG_PL,
                    COLOR_KHONG_PL);
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
        private static void UpdateThongKeLabel(
    ToolStripStatusLabel label,
    int count,
    string title,
    Color color)
        {
            if (label == null)
                return;
            // ẨN nếu = 0
            if (count <= 0)
            {
                label.Visible = false;
                return;
            }
            label.Visible = true;
            // text chuẩn
            label.Text = $"{title}: {count:N0} {Module_HeThong.Tu_dong_chi}";
            // màu
            label.ForeColor = color;
            // font đậm dễ nhìn
            label.Font = THONGKE_FONT;
            // căn giữa
            label.TextAlign = ContentAlignment.MiddleCenter;
            // chống rung layout
            label.AutoSize = false;
            // width cố định
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
            // 1. Dừng timer tìm kiếm đang chạy ngầm (nếu có) để tránh xung đột luồng
            _searchTimer?.Stop();
            // 2. Khôi phục các ComboBox về Module_HeThong.Tat_Ca và TextBox về Placeholder (Sử dụng hàm ResetUI an toàn đã có sẵn)
            ResetUI();
            // 3. Trả Focus về lại lưới dữ liệu để UX mượt mà, tránh tình trạng con trỏ nháy bị kẹt
            kryptonDataGridView2.Focus();
            // 4. Áp dụng lại màng lọc (lúc này các tiêu chí đã bị xóa sạch nên sẽ tự động hiển thị lại 100% dữ liệu)
            ApplyFilter_Virtual();
        }
    }
    //Lê Trung Kiên
    // ⭐ ĐÃ ĐỔI TÊN THÀNH CBCSQuanLyModel ĐỂ TRÁNH XUNG ĐỘT (AMBIGUITY ERROR) VỚI CÁC FORM KHÁC
    public class CBCSQuanLyModel
    {
        public string HoVaTen { get; set; }
        public string SoHieu { get; set; }
        public string NamSinh { get; set; }
        public string QueQuan { get; set; }
        public string NgayVaoCAND { get; set; }
        public string CapBac { get; set; }
        public string ChucVu { get; set; }
        public string DonVi { get; set; }
        public string PhanLoai { get; set; }
        public string GhiChu { get; set; }
    }
}