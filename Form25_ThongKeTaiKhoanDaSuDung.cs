using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
using System.Reflection;

namespace PhanMemThiDua2026
{
    public partial class Form25_ThongKeTaiKhoanDaSuDung : Form
    {
        private readonly string _csdl3Path = Module_DanduongGPS.DuongDanCSDL3;
        private bool _gridInitialized = false;
        // ⭐ KHAI BÁO ẢNH TRÊN RAM (Tối ưu hiệu suất, load 1 lần dùng mãi mãi)
        private readonly Image _iconActiveUser = Properties.Resources.client_account_template;
        private readonly Image _iconSystemUser = Properties.Resources.ic_default; // Thay bằng icon system của bạn nếu có
        private readonly Image _iconNormalUser = Properties.Resources.ic_user;

        // ⭐ CHUẨN KỸ SƯ 1: Dùng List Model cho Virtual Mode, tuyệt đối không dùng DataTable
        private List<ThongKeTaiKhoanModel> _cacheThongKe = new List<ThongKeTaiKhoanModel>();
        private bool _isLoading = false;
        private bool _dangDongForm = false;
        public Form25_ThongKeTaiKhoanDaSuDung()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            InitToolTips();

            // ⭐ CHUẨN KỸ SƯ 2: Đăng ký Virtual Mode trọn gói
            if (kryptonDataGridView1 != null)
            {
                kryptonDataGridView1.VirtualMode = true; // Kích hoạt Virtual Mode
                kryptonDataGridView1.CellValueNeeded += KryptonDataGridView1_CellValueNeeded; // Bơm dữ liệu O(1)
                kryptonDataGridView1.CellFormatting += KryptonDataGridView1_CellFormatting;   // Tô màu nền O(1)
                kryptonDataGridView1.CellPainting += KryptonDataGridView1_CellPainting;       // Vẽ Icon
            }
        }

        private async void Form25_ThongKeTaiKhoanDaSuDung_Load(object sender, EventArgs e)
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

                _isLoading = true;
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

                    // ⭐ 1. KHAI BÁO MẢNG ĐỊNH DẠNG NGÀY GIỜ ĐỂ TRÁNH LỖI PARSE
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
                            string raw = rd["TaiKhoan"]?.ToString()?.Trim();
                            if (string.IsNullOrWhiteSpace(raw)) continue;

                            string userDisplay = "";
                            try
                            {
                                // ⭐ 2. KIỂM TRA VÀ CẮT BỎ TIỀN TỐ "AES:" TRƯỚC KHI GIẢI MÃ (NẾU CÓ)
                                if (raw.StartsWith("AES:"))
                                {
                                    userDisplay = BaoMatAES.GiaiMa(raw.Substring(4))?.Trim();
                                }
                                else
                                {
                                    userDisplay = BaoMatAES.GiaiMa(raw)?.Trim();
                                }
                            }
                            catch
                            {
                                continue; // Nếu giải mã lỗi, bỏ qua
                            }

                            if (string.IsNullOrWhiteSpace(userDisplay)) continue;

                            // ⭐ 3. ÉP KIỂU NGÀY THÁNG ĐA ĐỊNH DẠNG (Khắc phục hoàn toàn lỗi mất data)
                            string thoiGianRaw = rd["ThoiGian"]?.ToString()?.Trim();
                            DateTime time;

                            if (!DateTime.TryParseExact(thoiGianRaw, cacDinhDangNgay, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out time))
                            {
                                if (!DateTime.TryParse(thoiGianRaw, out time))
                                {
                                    continue; // Bỏ qua nếu hoàn toàn không đọc được ngày
                                }
                            }

                            // Tích lũy số liệu
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
                        else tinhTrang = "Đã lưu";

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
                _isLoading = false;
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
        private readonly Color _headerBackColor = Color.FromArgb(225, 232, 241);
        private readonly Color _headerForeColor = Color.FromArgb(30, 30, 30);
        private readonly Color _gridLineColor = Color.FromArgb(210, 210, 210);
        private readonly Color _selectionBackColor = Color.FromArgb(210, 228, 255);
        private readonly Color _selectionForeColor = Color.Black;
        private readonly Color _alternateRowColor = Color.FromArgb(248, 250, 252);
        // ========================= FONT =========================
        private readonly Font _headerFont = new Font("Segoe UI", 10F, FontStyle.Bold);
        private readonly Font _cellFont = new Font("Segoe UI", 10F, FontStyle.Regular);
        private readonly Font _cellBoldFont = new Font("Segoe UI", 10F, FontStyle.Bold);
        private void KhoiTaoGrid()
        {
            if (_gridInitialized)
                return;

            var dgv = kryptonDataGridView1;

            dgv.SuspendLayout();

            // =====================================================
            // CẤU HÌNH CƠ BẢN
            // =====================================================

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

            // =====================================================
            // HEADER
            // =====================================================

            dgv.ColumnHeadersHeight = 42;

            dgv.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgv.ColumnHeadersBorderStyle =
                DataGridViewHeaderBorderStyle.Single;

            dgv.ColumnHeadersDefaultCellStyle.BackColor =
                _headerBackColor;

            dgv.ColumnHeadersDefaultCellStyle.ForeColor =
                _headerForeColor;

            dgv.ColumnHeadersDefaultCellStyle.Font =
                _headerFont;

            dgv.ColumnHeadersDefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleCenter;

            dgv.ColumnHeadersDefaultCellStyle.WrapMode =
                DataGridViewTriState.False;

            // =====================================================
            // CELL STYLE
            // =====================================================

            dgv.DefaultCellStyle.Font =
                _cellFont;

            dgv.DefaultCellStyle.BackColor =
                Color.White;

            dgv.DefaultCellStyle.ForeColor =
                Color.Black;

            dgv.DefaultCellStyle.SelectionBackColor =
                _selectionBackColor;

            dgv.DefaultCellStyle.SelectionForeColor =
                _selectionForeColor;

            dgv.DefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleLeft;

            dgv.DefaultCellStyle.Padding =
                new Padding(4, 0, 4, 0);

            dgv.AlternatingRowsDefaultCellStyle.BackColor =
                _alternateRowColor;

            dgv.RowTemplate.Height = 34;

            // =====================================================
            // AUTO SIZE
            // =====================================================

            dgv.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;

            // =====================================================
            // TẠO CỘT
            // =====================================================

            dgv.Columns.Add("STT", "STT");
            dgv.Columns.Add("TenTaiKhoan", "Tên tài khoản");
            dgv.Columns.Add("ThoiGianLanDau", "Sử dụng lần đầu");
            dgv.Columns.Add("ThoiGianLanCuoi", "Sử dụng lần cuối");
            dgv.Columns.Add("SoLuotHanhDong", "Tổng hành động");
            dgv.Columns.Add("TinhTrang", "Tình trạng");

            // =====================================================
            // KÍCH THƯỚC CỘT
            // =====================================================

            dgv.Columns["STT"].FillWeight = 40;

            dgv.Columns["TenTaiKhoan"].FillWeight = 180;

            dgv.Columns["ThoiGianLanDau"].FillWeight = 140;

            dgv.Columns["ThoiGianLanCuoi"].FillWeight = 140;

            dgv.Columns["SoLuotHanhDong"].FillWeight = 90;

            dgv.Columns["TinhTrang"].FillWeight = 90;

            // =====================================================
            // CĂN GIỮA
            // =====================================================

            dgv.Columns["STT"].DefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleCenter;

            dgv.Columns["ThoiGianLanDau"].DefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleCenter;

            dgv.Columns["ThoiGianLanCuoi"].DefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleCenter;

            dgv.Columns["SoLuotHanhDong"].DefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleCenter;

            dgv.Columns["TinhTrang"].DefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleCenter;

            // =====================================================
            // FORMAT NGÀY GIỜ
            // =====================================================

            dgv.Columns["ThoiGianLanDau"].DefaultCellStyle.Format =
                "dd/MM/yyyy HH:mm:ss";

            dgv.Columns["ThoiGianLanCuoi"].DefaultCellStyle.Format =
                "dd/MM/yyyy HH:mm:ss";

            // =====================================================
            // FONT ĐẬM CHO CỘT QUAN TRỌNG
            // =====================================================

            dgv.Columns["TinhTrang"].DefaultCellStyle.Font =
                _cellBoldFont;

            dgv.Columns["SoLuotHanhDong"].DefaultCellStyle.Font =
                _cellBoldFont;

            dgv.ResumeLayout();

            _gridInitialized = true;
        }
        private void SetupStatusStrip()
        {
            if (statusStrip1 == null)
                return;

            statusStrip1.SuspendLayout();

            statusStrip1.SizingGrip = false;
            statusStrip1.AutoSize = false;

            toolStripStatusLabel1.Spring = true;

            toolStripStatusLabel1.TextAlign =
                ContentAlignment.MiddleLeft;

            toolStripStatusLabel2.TextAlign =
                ContentAlignment.MiddleRight;

            toolStripStatusLabel1.Text =
                $"Phiên bản: {Module_PhienBan.SoftwareVersion}";

            toolStripStatusLabel2.Text =
                Module_PhienBan.NgayThangNamCapNhat;

            statusStrip1.ResumeLayout();
        }
        private void KryptonDataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
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
        // ⭐ VIRTUAL MODE: TÔ MÀU THEO ĐIỀU KIỆN (Thay thế vòng lặp foreach cũ)
        private readonly Color _systemBackColor = Color.FromArgb(253, 240, 240);
        private readonly Color _systemForeColor = Color.DarkRed;
        private void KryptonDataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_cacheThongKe == null || e.RowIndex < 0 || e.RowIndex >= _cacheThongKe.Count) return;

            var item = _cacheThongKe[e.RowIndex];

            if (item.IsSystem)
            {
                if (e.CellStyle.BackColor != _systemBackColor)
                {
                    e.CellStyle.BackColor = _systemBackColor;
                    e.CellStyle.ForeColor = _systemForeColor;
                }
            }
        }
        // =====================================================================
        // ⭐ VẼ ICON TÙY CHỈNH KẾT HỢP VỚI VIRTUAL MODE
        // =====================================================================
        private readonly Font _fontRegular =
     new Font("Segoe UI", 9F, FontStyle.Regular);

        private readonly Font _fontBold =
            new Font("Segoe UI", 9F, FontStyle.Bold);
        private void KryptonDataGridView1_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
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
                    e.Graphics.DrawImage(iconToDraw, new Rectangle(xIcon, yIcon, iconSize, iconSize));
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
                Color textColor;
                if ((e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected)
                {
                    textColor = e.CellStyle.SelectionForeColor;
                }
                else
                {
                    if (item.IsSystem) textColor = Color.DarkRed;
                    else if (item.IsCurrentUser) textColor = Color.FromArgb(0, 102, 204);
                    else textColor = e.CellStyle.ForeColor;
                }

                // 6. Vẽ Text (System và User hiện tại in Đậm)
                FontStyle style = (item.IsSystem || item.IsCurrentUser) ? FontStyle.Bold : FontStyle.Regular;
                Font cellFont =
    (item.IsSystem || item.IsCurrentUser)
    ? _fontBold
    : _fontRegular;
                {
                    TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;
                    TextRenderer.DrawText(e.Graphics, item.TenTaiKhoan, cellFont, textBounds, textColor, flags);
                }

                // Báo cho DataGridView biết mình đã vẽ xong, không cần vẽ đè lên nữa
                e.Handled = true;
            }
        }
        private void kryptonButton_Dong_Click(object sender, EventArgs e)
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
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Thao tác";
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
        public string TenTaiKhoan { get; set; }
        public DateTime ThoiGianLanDau { get; set; }
        public DateTime ThoiGianLanCuoi { get; set; }
        public int SoLuotHanhDong { get; set; }
        public string TinhTrang { get; set; }
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