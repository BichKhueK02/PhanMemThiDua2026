///Phiên bản 2
///
///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
using System.Reflection;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form25_ThongKeTaiKhoanDaSuDung : Form
    {
        private readonly string _csdl3Path = Module_DanduongGPS.DuongDanCSDL3;
        private readonly Image _iconActiveUser = Properties.Resources.client_account_template;
        private readonly Image _iconSystemUser = Properties.Resources.ic_default; // Thay bằng icon system của bạn nếu có
        private readonly Image _iconNormalUser = Properties.Resources.ic_user;
        private readonly Color _headerBackColor = Color.FromArgb(238, 232, 255);
        private readonly Color _headerForeColor = Color.FromArgb(75, 20, 130);
        private readonly Color _gridLineColor = Color.FromArgb(235, 238, 243);
        private readonly Color _selectionBackColor = Color.FromArgb(239, 246, 255);
        private readonly Color _selectionForeColor = Color.FromArgb(30, 41, 59);
        private readonly Color _alternateRowColor = Color.White;   // không tô vạch xen kẽ, giống lưới Form4
        private static readonly Font _headerFont = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Bold);
        private static readonly Font _cellFont = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Regular);
        private static readonly Font _cellBoldFont = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Bold);
        private static readonly Font _fontRegular = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Regular);
        private static readonly Font _fontBold = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Bold);
        private bool _gridInitialized = false;  // ⭐ KHAI BÁO ẢNH TRÊN RAM (Tối ưu hiệu suất, load 1 lần dùng mãi mãi)
        // ⭐ CHUẨN KỸ SƯ 1: Dùng List Model cho Virtual Mode, tuyệt đối không dùng DataTable
        private List<ThongKeTaiKhoanModel> _cacheThongKe = new List<ThongKeTaiKhoanModel>();
        //private bool _isLoading = false;
        private bool _dangDongForm = false;
        // ⭐ VIRTUAL MODE: TÔ MÀU THEO ĐIỀU KIỆN (Thay thế vòng lặp foreach cũ)
        private readonly Color _systemBackColor = Color.FromArgb(253, 240, 240);
        private readonly Color _systemForeColor = Color.DarkRed;
        public Form25_ThongKeTaiKhoanDaSuDung()
        {
            InitializeComponent();
            Module_GiaoDienForm25Web.ApDung(this);          // giao diện kiểu web như Form4/Form10 (thẻ, nút tím, khung lưới) - chỉ đổi trình bày
            this.CancelButton = kryptonButton_Dong;  // tooltip của nút Đóng ghi "Phím tắt: ESC" nhưng trước đây chưa gắn phím này
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            InitToolTips();
            Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1);
            // ⭐ CHUẨN KỸ SƯ 2: Đăng ký Virtual Mode trọn gói
            if (kryptonDataGridView1 != null)
            {
                kryptonDataGridView1.VirtualMode = true; // Kích hoạt Virtual Mode
                kryptonDataGridView1.CellValueNeeded += KryptonDataGridView1_CellValueNeeded; // Bơm dữ liệu O(1)
                kryptonDataGridView1.CellFormatting += KryptonDataGridView1_CellFormatting;   // Tô màu nền O(1)
                kryptonDataGridView1.CellPainting += KryptonDataGridView1_CellPainting;       // Vẽ Icon
            }
        }
        private async void Form25_ThongKeTaiKhoanDaSuDung_Load(object? sender, EventArgs e)
        {
            // Tắt Focus mặc định để tránh viền xanh xấu xí ở ô đầu tiên
            this.ActiveControl = null;
            await LoadDuLieuTaiKhoanDaSuDungAsync();
            SetupStatusStrip();
        }
        private readonly SemaphoreSlim _loadLock = new(1, 1);
        // Đổi private thành public để Form10 có thể gọi
        public async Task LoadDuLieuTaiKhoanDaSuDungAsync(bool forceReload = false)
        {
            if (!await _loadLock.WaitAsync(0))
                return;
            try
            {
                if (IsDisposed || !IsHandleCreated)
                    return;
                //_isLoading = true;
                kryptonDataGridView1.Enabled = false;
                if (!forceReload && _cacheThongKe != null && _cacheThongKe.Count > 0)
                {
                    HienThiLenGrid();
                    return;
                }
                string currentUser = Module_TaiKhoan.TenTaiKhoan_RAM?.Trim() ?? string.Empty;
                string csdlPath = _csdl3Path;
                if (string.IsNullOrWhiteSpace(csdlPath) || !File.Exists(csdlPath))
                {
                    throw new FileNotFoundException("Không tìm thấy CSDL thống kê.", csdlPath);
                }
                List<ThongKeTaiKhoanModel> resultList = await Task.Run(() =>
                {
                    var map = new Dictionary<string, (DateTime LanDau, DateTime LanCuoi, int SoLuot)>(StringComparer.OrdinalIgnoreCase);
                    int errorCount = 0;
                    const int MAX_ERROR_LOG = 10;
                    // ⭐ KHAI BÁO MẢNG ĐỊNH DẠNG NGÀY GIỜ ĐỂ TRÁNH LỖI PARSE
                    string[] cacDinhDangNgay = { "yyyy-MM-dd HH:mm:ss", "dd-MM-yyyy HH:mm:ss", "dd/MM/yyyy HH:mm:ss", "dd-MM-yyyy", "dd/MM/yyyy", "M/d/yyyy h:mm:ss tt" };
                    var builder = new SqliteConnectionStringBuilder
                    {
                        DataSource = csdlPath,
                        Mode = SqliteOpenMode.ReadOnly,
                        DefaultTimeout = 5
                    };
                    using var cn = new SqliteConnection(builder.ConnectionString);
                    cn.Open();
                    using var cmd = cn.CreateCommand();
                    cmd.CommandText = @"
                SELECT TaiKhoan, ThoiGian
                FROM NhatKyUngDung
                WHERE TaiKhoan IS NOT NULL AND TaiKhoan <> ''";
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        try
                        {
                            string raw = rd["TaiKhoan"]?.ToString()?.Trim() ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(raw)) continue;
                            string userDisplay = string.Empty;
                            // ⭐ 1. XỬ LÝ GIẢI MÃ (HỖ TRỢ chuẩn V4 VÀ TƯƠNG THÍCH NGUỢC)
                            try
                            {
                                // Thử giải mã trực tiếp bằng chuẩn V4
                                userDisplay = Module_BaoMatAES.GiaiMa_NhatKy(raw)?.Trim() ?? string.Empty;
                                if (string.IsNullOrWhiteSpace(userDisplay))
                                {
                                    // Nếu không có tiền tố "AES:" -> Xem như dữ liệu chưa mã hóa (Plaintext)
                                    if (!raw.StartsWith("AES:", StringComparison.OrdinalIgnoreCase))
                                    {
                                        userDisplay = raw;
                                    }
                                    else
                                    {
                                        // Không thể giải mã dữ liệu mã hóa cũ/lỗi -> Bỏ qua
                                        continue;
                                    }
                                }
                            }
                            catch
                            {
                                continue; // Lỗi giải mã -> Bỏ qua dòng này
                            }
                            if (string.IsNullOrWhiteSpace(userDisplay)) continue;
                            // ⭐ 2. ÉP KIỂU NGÀY THÁNG ĐA ĐỊNH DẠNG
                            string thoiGianRaw = rd["ThoiGian"]?.ToString()?.Trim() ?? string.Empty;
                            DateTime time;
                            if (!DateTime.TryParseExact(thoiGianRaw, cacDinhDangNgay, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out time))
                            {
                                if (!DateTime.TryParse(thoiGianRaw, out time))
                                {
                                    continue; // Bỏ qua nếu hoàn toàn không đọc được ngày
                                }
                            }
                            // ⭐ 3. TÍCH LŨY SỐ LIỆU TÀI KHOẢN
                            if (!map.TryGetValue(userDisplay, out var existing))
                            {
                                map[userDisplay] = (time, time, 1);
                            }
                            else
                            {
                                DateTime minTime = time < existing.LanDau ? time : existing.LanDau;
                                DateTime maxTime = time > existing.LanCuoi ? time : existing.LanCuoi;
                                map[userDisplay] = (minTime, maxTime, existing.SoLuot + 1);
                            }
                        }
                        catch (Exception exRow)
                        {
                            if (errorCount < MAX_ERROR_LOG)
                            {
                                Debug.WriteLine($"[Row Error] {exRow.Message}");
                                errorCount++;
                            }
                            continue;
                        }
                    }
                    // ⭐ 4. CHUYỂN ĐỔI MAP SANG DẠNG LIST DÙNG CHO VIRTUAL MODE
                    var list = new List<ThongKeTaiKhoanModel>(map.Count);
                    int stt = 1;
                    foreach (var item in map.OrderByDescending(x => x.Value.LanCuoi))
                    {
                        string userName = item.Key;
                        bool isSystem = string.Equals(userName, "System (Quyền cao nhất)", StringComparison.OrdinalIgnoreCase) || userName.Contains("System");
                        bool isCurrent = string.Equals(userName, currentUser, StringComparison.OrdinalIgnoreCase);
                        string tinhTrang;
                        if (isCurrent) tinhTrang = "Đang sử dụng";
                        else if (isSystem) tinhTrang = "Hệ thống";
                        else tinhTrang = "Ngừng sử dụng";
                        list.Add(new ThongKeTaiKhoanModel
                        {
                            STT = stt++,
                            TenTaiKhoan = userName,
                            ThoiGianLanDau = item.Value.LanDau,
                            ThoiGianLanCuoi = item.Value.LanCuoi,
                            SoLuotHanhDong = item.Value.SoLuot,
                            TinhTrang = tinhTrang,
                            IsCurrentUser = isCurrent,
                            IsSystem = isSystem,
                            IsDeleted = !isCurrent && !isSystem
                        });
                    }
                    return list;
                });
                if (IsDisposed || !IsHandleCreated) return;
                _cacheThongKe = resultList;
                HienThiLenGrid();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoadDuLieuTaiKhoanDaSuDungAsync] Lỗi: {ex}");
            }
            finally
            {
                if (!IsDisposed) kryptonDataGridView1.Enabled = true;
                //_isLoading = false;
                _loadLock.Release();
            }
        }
        private void HienThiLenGrid()
        {
            if (IsDisposed || !IsHandleCreated)
                return;
            KhoiTaoGrid();
            kryptonDataGridView1.SuspendLayout();
            kryptonDataGridView1.RowCount =
                _cacheThongKe?.Count ?? 0;
            kryptonDataGridView1.ClearSelection();
            kryptonDataGridView1.CurrentCell = null;
            kryptonDataGridView1.ResumeLayout();
            kryptonDataGridView1.Invalidate();
        }
        // ⭐ VIRTUAL MODE: BƠM DỮ LIỆU ĐỘNG VÀO Ô
        // ========================= COLOR PALETTE =========================
        private void KhoiTaoGrid()
        {
            if (_gridInitialized)
                return;
            var dgv = kryptonDataGridView1;
            dgv.SuspendLayout();
            // CẤU HÌNH CƠ BẢN
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToResizeRows = false;
            dgv.MultiSelect = false;
            dgv.RowHeadersVisible = false;
            dgv.SelectionMode =
                DataGridViewSelectionMode.FullRowSelect;
            dgv.AutoGenerateColumns = false;
            dgv.VirtualMode = true;
            dgv.BorderStyle = BorderStyle.None;
            dgv.BackgroundColor = Color.White;
            dgv.GridColor = _gridLineColor;
            dgv.EnableHeadersVisualStyles = false;
            dgv.DoubleBuffered(true);
            // HEADER
            dgv.ColumnHeadersHeight = 58;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = _headerBackColor;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = _headerForeColor;
            dgv.ColumnHeadersDefaultCellStyle.Font = _headerFont;
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            // CELL STYLE
            dgv.DefaultCellStyle.Font = _cellFont;
            dgv.DefaultCellStyle.BackColor = Color.White;
            dgv.DefaultCellStyle.ForeColor = Color.Black;
            dgv.DefaultCellStyle.SelectionBackColor = _selectionBackColor;
            dgv.DefaultCellStyle.SelectionForeColor = _selectionForeColor;
            dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgv.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = _alternateRowColor;
            dgv.RowTemplate.Height = 38;
            // AUTO SIZE
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;            // TẠO CỘT
            dgv.Columns.Add("STT", "STT");
            dgv.Columns.Add("TenTaiKhoan", "Tên tài khoản");
            dgv.Columns.Add("ThoiGianLanDau", "Sử dụng lần đầu");
            dgv.Columns.Add("ThoiGianLanCuoi", "Sử dụng lần cuối");
            dgv.Columns.Add("SoLuotHanhDong", "Tổng hành động");
            dgv.Columns.Add("TinhTrang", "Tình trạng");
            // KÍCH THƯỚC CỘT
            dgv.Columns["STT"].FillWeight = 40;
            dgv.Columns["TenTaiKhoan"].FillWeight = 180;
            dgv.Columns["ThoiGianLanDau"].FillWeight = 140;
            dgv.Columns["ThoiGianLanCuoi"].FillWeight = 140;
            dgv.Columns["SoLuotHanhDong"].FillWeight = 90;
            dgv.Columns["TinhTrang"].FillWeight = 90;
            // CĂN GIỮA
            dgv.Columns["STT"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns["ThoiGianLanDau"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns["ThoiGianLanCuoi"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns["SoLuotHanhDong"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns["TinhTrang"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            // FORMAT NGÀY GIỜ
            dgv.Columns["ThoiGianLanDau"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm:ss";
            dgv.Columns["ThoiGianLanCuoi"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm:ss";
            // FONT ĐẬM CHO CỘT QUAN TRỌNG
            dgv.Columns["TinhTrang"].DefaultCellStyle.Font = _cellBoldFont;
            dgv.Columns["SoLuotHanhDong"].DefaultCellStyle.Font = _cellBoldFont;
            Module_GiaoDienForm25Web.StyleLuoi(dgv);   // tiêu đề tím nhạt phẳng + đường kẻ nhạt + dòng chọn xanh nhạt (giống Form4/Form10)
            dgv.ResumeLayout();
            _gridInitialized = true;
        }
        private void SetupStatusStrip()
        {
            if (statusStrip1 == null) return;
            statusStrip1.SuspendLayout();
            statusStrip1.SizingGrip = false;
            statusStrip1.AutoSize = false;
            // 1. Tắt chế độ Spring để Label không tự động co giãn đẩy Label 2 sang phải
            toolStripStatusLabel1.Spring = false;
            toolStripStatusLabel2.Spring = false;
            // 2. Căn chữ và Icon (nếu có) về sát bên trái
            toolStripStatusLabel1.TextAlign = ContentAlignment.MiddleLeft;
            toolStripStatusLabel1.ImageAlign = ContentAlignment.MiddleLeft;
            toolStripStatusLabel1.TextImageRelation = TextImageRelation.ImageBeforeText;
            toolStripStatusLabel2.TextAlign = ContentAlignment.MiddleLeft;
            toolStripStatusLabel2.ImageAlign = ContentAlignment.MiddleLeft;
            toolStripStatusLabel2.TextImageRelation = TextImageRelation.ImageBeforeText;
            // 3. (Tùy chọn) Thêm khoảng cách lề giữa 2 label cho đẹp mắt (VD: cách nhau 15px)
            toolStripStatusLabel1.Margin = new Padding(0, 3, 15, 2);
            toolStripStatusLabel2.Margin = new Padding(0, 3, 0, 2);
            // Gán dữ liệu
            toolStripStatusLabel1.Text = $"Phiên bản: {Module_PhienBan.SoftwareVersion}";
            toolStripStatusLabel2.Text = $"                                            " + Module_PhienBan.NgayThangNamCapNhat;
            statusStrip1.ResumeLayout();
        }
        private void KryptonDataGridView1_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            if (_cacheThongKe == null || e.RowIndex < 0 || e.RowIndex >= _cacheThongKe.Count) return;
            var item = _cacheThongKe[e.RowIndex];
            string colName = kryptonDataGridView1.Columns[e.ColumnIndex].Name;
            switch (colName)
            {
                case "STT": e.Value = item.STT; break;
                case "TenTaiKhoan": e.Value = item.TenTaiKhoan; break;
                case "ThoiGianLanDau": e.Value = item.ThoiGianLanDau; break;
                case "ThoiGianLanCuoi": e.Value = item.ThoiGianLanCuoi; break;
                case "SoLuotHanhDong": e.Value = item.SoLuotHanhDong; break;
                case "TinhTrang": e.Value = item.TinhTrang; break;
            }
        }
        private void KryptonDataGridView1_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_cacheThongKe == null || e.RowIndex < 0 || e.RowIndex >= _cacheThongKe.Count) return;
            var item = _cacheThongKe[e.RowIndex];
            if (item.IsSystem)
            {
                if (e.CellStyle!.BackColor != _systemBackColor)
                {
                    e.CellStyle.BackColor = _systemBackColor;
                    e.CellStyle.ForeColor = _systemForeColor; // Hệ thống: Màu đỏ đậm
                }
            }
            else if (item.IsCurrentUser)
            {
                // ⭐ "Đang sử dụng": Tô chữ màu Xanh Lá Cây (ForestGreen/DarkGreen)
                e.CellStyle!.ForeColor = Color.ForestGreen;
            }
            else
            {
                // ⭐ "Ngừng sử dụng": Tô chữ màu Xanh Nước Biển (RoyalBlue/MediumBlue)
                e.CellStyle!.ForeColor = Color.RoyalBlue;
            }
        }
        // ⭐ VẼ ICON TÙY CHỈNH KẾT HỢP VỚI VIRTUAL MODE
        private void KryptonDataGridView1_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv == null || e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_cacheThongKe == null || e.RowIndex >= _cacheThongKe.Count) return;
            if (dgv.Columns[e.ColumnIndex].Name == "TenTaiKhoan")
            {
                // Lấy đối tượng từ List RAM, KHÔNG chọc vào e.Value để parse chuỗi (rất chậm)
                var item = _cacheThongKe[e.RowIndex];
                // 1. Chỉ vẽ nền và khung chọn, tự mình sẽ vẽ Icon và Text sau
                DataGridViewPaintParts parts = DataGridViewPaintParts.Background |
                                               DataGridViewPaintParts.SelectionBackground |
                                               DataGridViewPaintParts.Focus;
                e.Paint(e.CellBounds, parts);
                // 2. Chọn Icon
                Image iconToDraw;
                if (item.IsSystem) iconToDraw = _iconSystemUser;
                else if (item.IsCurrentUser) iconToDraw = _iconActiveUser;
                else iconToDraw = _iconNormalUser;
                // 3. Tính toán vị trí Icon
                int iconSize = 16;
                int paddingLeft = 6;
                int yIcon = e.CellBounds.Y + (e.CellBounds.Height - iconSize) / 2;
                int xIcon = e.CellBounds.X + paddingLeft;
                if (iconToDraw != null)
                {
                    e.Graphics!.DrawImage(iconToDraw, new Rectangle(xIcon, yIcon, iconSize, iconSize));
                }
                // 4. Tính toán vị trí Text
                int paddingIconText = 6;
                int textStartX = xIcon + iconSize + paddingIconText;
                Rectangle textBounds = new Rectangle(
                    textStartX,
                    e.CellBounds.Y,
                    e.CellBounds.Width - (textStartX - e.CellBounds.X),
                    e.CellBounds.Height);
                // 5. Xác định màu Text
                // 5. Xác định màu Text
                Color textColor;
                if ((e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected)
                {
                    textColor = e.CellStyle!.SelectionForeColor;
                }
                else
                {
                    if (item.IsSystem)
                    {
                        textColor = Color.DarkRed; // Hệ thống: Đỏ đậm
                    }
                    else if (item.IsCurrentUser)
                    {
                        textColor = Color.ForestGreen; // ⭐ Đang sử dụng: Xanh lá cây
                    }
                    else
                    {
                        textColor = Color.RoyalBlue; // ⭐ Ngừng sử dụng: Xanh nước biển
                    }
                }
                // 6. Vẽ Text (System và User hiện tại in Đậm)
                Font cellFont =
    (item.IsSystem || item.IsCurrentUser)
    ? _fontBold
    : _fontRegular;
                {
                    TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;
                    TextRenderer.DrawText(e.Graphics!, item.TenTaiKhoan, cellFont, textBounds, textColor, flags);
                }
                // Báo cho DataGridView biết mình đã vẽ xong, không cần vẽ đè lên nữa
                e.Handled = true;
            }
        }
        private void kryptonButton_Dong_Click(object? sender, EventArgs e)
        {
            if (_dangDongForm) return;
            _dangDongForm = true;
            kryptonButton_Dong.Enabled = false;
            try
            {
                this.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi khi đóng Form: " + ex.Message);
                kryptonButton_Dong.Enabled = true;
                _dangDongForm = false;
            }
        }
        // ===================== MENU CHUỘT PHẢI =====================
        private async void lamMoi_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            lamMoi_ToolStripMenuItem.Enabled = false;
            try
            {
                await LoadDuLieuTaiKhoanDaSuDungAsync(forceReload: true);
            }
            finally
            {
                if (!IsDisposed) lamMoi_ToolStripMenuItem.Enabled = true;
            }
        }
        private void dongForm_ToolStripMenuItem_Click(object? sender, EventArgs e) => kryptonButton_Dong.PerformClick();
        private async void xuatDuLieuExcel_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            var duLieu = _cacheThongKe?.ToList() ?? new List<ThongKeTaiKhoanModel>();   // chụp bản sao để luồng nền không đụng list đang hiển thị
            if (duLieu.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu trên lưới để xuất tệp!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var sfd = new SaveFileDialog
            {
                Title = "Chọn nơi lưu tệp excel thống kê tài khoản",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"ThongKeTaiKhoanDaSuDung_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;
            string duongDan = sfd.FileName;
            xuatDuLieuExcel_ToolStripMenuItem.Enabled = false;
            Cursor = Cursors.WaitCursor;
            try
            {
                await Task.Run(() => XuatExcelCore(duongDan, duLieu));
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Xuất Excel thống kê tài khoản đã sử dụng",
                    $"{duLieu.Count} tài khoản | Tệp: {Path.GetFileName(duongDan)}");
                if (IsDisposed) return;
                Cursor = Cursors.Default;
                if (MessageBox.Show($"Đã xuất {duLieu.Count} tài khoản ra tệp excel:\n{duongDan}\n\nBạn có muốn mở tệp ngay không?",
                        "Xuất dữ liệu thành công", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    try { Process.Start(new ProcessStartInfo(duongDan) { UseShellExecute = true }); }
                    catch (Exception exMo) { Debug.WriteLine($"[Form25] Không mở được tệp: {exMo.Message}"); }
                }
            }
            catch (Exception ex)
            {
                if (!IsDisposed) Cursor = Cursors.Default;
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Lỗi xuất Excel thống kê tài khoản đã sử dụng", ex.Message);
                MessageBox.Show("Không thể xuất tệp excel:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!IsDisposed)
                {
                    Cursor = Cursors.Default;
                    xuatDuLieuExcel_ToolStripMenuItem.Enabled = true;
                }
            }
        }
        private static void XuatExcelCore(string duongDan, List<ThongKeTaiKhoanModel> duLieu)
        {
            Module_ExcelAnToan.KiemTraCoTheGhi(duongDan);   // báo sớm nếu tệp đích đang mở trong Excel
            string[] tieuDe = { "STT", "Tên tài khoản", "Sử dụng lần đầu", "Sử dụng lần cuối", "Tổng hành động", "Tình trạng" };
            int soCot = tieuDe.Length;
            var dong = new List<object?[]>(duLieu.Count);
            foreach (var x in duLieu)
                dong.Add(new object?[] { x.STT, x.TenTaiKhoan, x.ThoiGianLanDau, x.ThoiGianLanCuoi, x.SoLuotHanhDong, x.TinhTrang });
            using var wb = new ClosedXML.Excel.XLWorkbook();
            var ws = wb.Worksheets.Add("TaiKhoanDaSuDung");
            ws.Cell(1, 1).Value = "THỐNG KÊ TÀI KHOẢN ĐÃ ĐĂNG KÝ SỬ DỤNG PHẦN MỀM";
            ws.Range(1, 1, 1, soCot).Merge().Style.Font.SetBold().Font.SetFontSize(14)
                .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);
            ws.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            ws.Range(2, 1, 2, soCot).Merge().Style.Font.SetItalic()
                .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);
            const int hangTieuDe = 4;
            for (int c = 0; c < soCot; c++) ws.Cell(hangTieuDe, c + 1).Value = tieuDe[c];
            var vungTieuDe = ws.Range(hangTieuDe, 1, hangTieuDe, soCot);
            vungTieuDe.Style.Font.Bold = true;
            vungTieuDe.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(238, 232, 255);
            vungTieuDe.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            vungTieuDe.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
            Module_ExcelAnToan.NapDong(ws, hangTieuDe + 1, 1, dong);
            int hangCuoi = hangTieuDe + dong.Count;
            ws.Range(hangTieuDe + 1, 3, hangCuoi, 4).Style.NumberFormat.Format = "dd/MM/yyyy HH:mm:ss";
            foreach (int c in new[] { 1, 3, 4, 5, 6 })
                ws.Range(hangTieuDe + 1, c, hangCuoi, c).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            Module_ExcelAnToan.KeVien(ws.Range(hangTieuDe, 1, hangCuoi, soCot));
            Module_ExcelAnToan.DatDoRongCot(ws, dong, 1, tieuDe);
            ws.Column(3).Width = ws.Column(4).Width = 22;   // độ rộng cho ngày giờ (DatDoRongCot đo DateTime theo chuỗi mặc định)
            ws.SheetView.FreezeRows(hangTieuDe);
            Module_BanQuyen.DongDauExcel(wb);   // đóng dấu bản quyền + metadata sau khi dựng xong nội dung, ngay trước khi lưu (giống Form10)
            Module_ExcelAnToan.LuuAnToan(wb, duongDan);
        }
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = Module_HeThong.Thao_Tac;
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            if (kryptonButton_Dong != null)
            {
                toolTip1.SetToolTip(kryptonButton_Dong, "Đóng cửa sổ này và quay lại màn hình trước (Phím tắt: ESC)");
            }
        }
    }
    // ⭐ CHUẨN KỸ SƯ 6: LỚP ĐỐI TƯỢNG (MODEL) CHO VIRTUAL MODE
    public class ThongKeTaiKhoanModel
    {
        public int STT { get; set; }
        public string TenTaiKhoan { get; set; } = null!;
        public DateTime ThoiGianLanDau { get; set; }
        public DateTime ThoiGianLanCuoi { get; set; }
        public int SoLuotHanhDong { get; set; }
        public string TinhTrang { get; set; } = null!;
        // Các cờ bool (Cực kỳ quan trọng để hàm Vẽ UI không phải so sánh chuỗi gây chậm máy)
        public bool IsSystem { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsCurrentUser { get; set; }
    }
    public static class DataGridViewExtension
    {
        public static void DoubleBuffered(
            this DataGridView dgv,
            bool setting)
        {
            typeof(DataGridView)
                .GetProperty(
                    "DoubleBuffered",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic)
                ?.SetValue(dgv, setting, null);
        }
    }
}