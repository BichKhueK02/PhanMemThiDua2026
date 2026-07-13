using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PhanMemThiDua2026
{
    public partial class Form33_KiemTraSucKhoeCSDL : Form
    {
        // ================== HÀM API WINDOWS CHO PROGRESS BAR ==================
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        private const int PBM_SETSTATE = 0x0410;
        private const int PBST_NORMAL = 0x0001;

        // ================== ARRAYS & CACHE (TỐI ƯU RAM) ==================
        private readonly string[] _tenCSDL = { "Database 1 - User", "Database 2 - System", "Database 3 - Log", "Database 4 - Statistics" };
        //private readonly string[] _tenFilePhu = { "Thư mục Công Cụ", "Tệp nhúng (EX)", "Tệp hướng dẫn (DP)", "Thư viện (PX64)", "Thư viện (PX86)" };
        private readonly string[] _tenFilePhu = { "Thư mục Công Cụ", "Tệp nhúng (EX)", "Thư mục (Hướng dẫn SD)" };
        private readonly string[] _pathCSDL;
        private readonly string[] _pathsFilePhu;


        public static readonly string ThuMucCoSoDuLieu =
        Path.Combine(
            AppContext.BaseDirectory,
            "Database",
            "HuongDanSuDung");
        // ⭐ CHUẨN KỸ SƯ: Sử dụng ConcurrentDictionary để chống Crash khi chạy Đa luồng
        private readonly ConcurrentDictionary<string, string> _cacheDungLuongOTrang = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Image _iconSafe = Properties.Resources._true;
        private readonly Image _iconWarning = Properties.Resources._false;
        public Form33_KiemTraSucKhoeCSDL()
        {
            InitializeComponent();
            this.Shown += Form33_KiemTraSucKhoeCSDL_Shown;

            _pathCSDL = new[] {
                Module_DanduongGPS.DuongDanCSDL1,
                Module_DanduongGPS.DuongDanCSDL2,
                Module_DanduongGPS.DuongDanCSDL3,
                Module_DanduongGPS.DuongDanCSDL4
            };

            _pathsFilePhu = new[]
           {
    Path.Combine(
        AppContext.BaseDirectory,
        "Database Backup",
        "CongCuQuanLyCSDL"),

    Module_DanduongGPS.DuongDanCSDL4ex,

    ThuMucCoSoDuLieu
};

            kryptonDataGridView1.CellPainting += KryptonDataGridView_CellPainting_CanhBao;
            kryptonDataGridView2.CellPainting += KryptonDataGridView_CellPainting_CanhBao;
        }
        private void Form33_KiemTraSucKhoeCSDL_Load(object sender, EventArgs e)
        {
            Module_MenuChuotPhai.TichHopGiaoDienXanhLa(contextMenuStrip1);
            label_TenAdminDangNhap.Text = "Người khai thác: " + SessionInfo.TenTaiKhoan;
            toolStripProgressBar1.Visible = false;
            toolStripStatusLabel1_ThoiGianKhaiThac.Text = "Ngày kiểm tra: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            toolStripStatusLabel1_ThoiGianKhaiThac.ForeColor = Color.Blue;
            toolStripStatusLabel1_KetLuan.Spring = true;
            toolStripStatusLabel1_KetLuan.TextAlign = ContentAlignment.MiddleRight;
            toolStripStatusLabel1_KetLuan.Text = "Kết luận: Đang kiểm tra...";

            EnableDoubleBuffer(kryptonDataGridView1);
            EnableDoubleBuffer(kryptonDataGridView2);

            kryptonDataGridView1.CellDoubleClick += KryptonDataGridView1_CellDoubleClick;
            kryptonDataGridView1.CellMouseDown += KryptonDataGridView1_CellMouseDown;

            lable_phienban.Text = "Phiên bản: " + Module_PhienBan.SoftwareVersion + "; Cập nhật: " + Module_PhienBan.NgayThangNam;
        }
        // --- CÁC HẰNG SỐ CẤU HÌNH ---
        private static class GridConfig
        {
            public const int IconSize = 16;
            public const int Padding = 6;
            public static readonly HashSet<string> StatusColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Trạng thái", "Mức độ phân mảnh"
            };
            public static readonly string[] WarningKeywords = { "cảnh báo", "lỗi", "nguy hiểm", "false", "cao", "locked" };
            public static readonly string[] SafeKeywords = { "bình thường", "tốt", "ổn định", "true", "thấp", "đang sử dụng", "ok" };
        }
        private Image GetIconByStatus(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string lowerValue = value.ToLower();

            if (GridConfig.WarningKeywords.Any(k => lowerValue.Contains(k))) return _iconWarning;
            if (GridConfig.SafeKeywords.Any(k => lowerValue.Contains(k))) return _iconSafe;
            return null;
        }
        private void KryptonDataGridView_CellPainting_CanhBao(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (!(sender is DataGridView dgv)) return;
            if (!GridConfig.StatusColumns.Contains(dgv.Columns[e.ColumnIndex].Name)) return;

            string cellValue = e.Value?.ToString() ?? string.Empty;
            Image icon = GetIconByStatus(cellValue);

            DataGridViewPaintParts paintParts = DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground & ~DataGridViewPaintParts.Border;
            e.Paint(e.CellBounds, paintParts);

            Rectangle cellRect = e.CellBounds;
            int xIcon = cellRect.X + GridConfig.Padding;
            int yIcon = cellRect.Y + (cellRect.Height - GridConfig.IconSize) / 2;

            if (icon != null)
            {
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                e.Graphics.DrawImage(icon, xIcon, yIcon, GridConfig.IconSize, GridConfig.IconSize);
            }

            if (!string.IsNullOrEmpty(cellValue))
            {
                int textOffset = (icon != null) ? (GridConfig.IconSize + GridConfig.Padding) : 0;
                Rectangle textBounds = new Rectangle(xIcon + textOffset, cellRect.Y, cellRect.Width - textOffset - GridConfig.Padding, cellRect.Height);
                Color textColor = (e.State & DataGridViewElementStates.Selected) != 0 ? e.CellStyle.SelectionForeColor : e.CellStyle.ForeColor;
                TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;
                TextRenderer.DrawText(e.Graphics, cellValue, e.CellStyle.Font, textBounds, textColor, flags);
            }
            e.Handled = true;
        }
        private async Task RunCheckAsync()
        {
            if (this.IsDisposed) return;
            try
            {
                _cacheDungLuongOTrang.Clear();
                toolStripStatusLabel1_KetLuan.Text = "Kết luận: Đang xử lý...";
                toolStripStatusLabel1_KetLuan.ForeColor = Color.Black;
                StartProgressBar();

                DataTable dtSQLite = TaoBangThuocTinhSQLite();
                DataTable dtFilePhu = TaoBangThuocTinhFilePhu();

                var sqliteTasks = _pathCSDL.Select(path => CheckSQLiteAsync(path ?? string.Empty)).ToList();
                var fileTasks = _pathsFilePhu
                    .Select(path =>
                    {
                        string safePath = path ?? string.Empty;

                        return Directory.Exists(safePath)
                            ? Task.Run(() => CheckFolder(safePath))
                            : Task.Run(() => CheckFile(safePath));
                    })
                    .ToList();

                await Task.WhenAll(sqliteTasks.Concat(fileTasks.Cast<Task>()));
                if (this.IsDisposed) return;

                for (int i = 0; i < sqliteTasks.Count; i++)
                {
                    var kq = sqliteTasks[i].Result;
                    dtSQLite.Rows.Add(i + 1, _tenCSDL[i], kq.TonTai ? "TRUE" : "FALSE", kq.NgayTao, kq.DungLuong, kq.SucKhoe, kq.TocDo, kq.PhanManh, kq.Journal, kq.QuyenDoc, kq.QuyenGhi, kq.OTrong);
                }

                for (int i = 0; i < fileTasks.Count; i++)
                {
                    var kq = fileTasks[i].Result;

                    kq.TrangThai = kq.TonTai ? "TRUE" : "FALSE";

                    dtFilePhu.Rows.Add(
                        i + 1,
                        _tenFilePhu[i],
                        kq.TrangThai,
                        kq.NgayTao,
                        kq.DungLuong,
                        kq.QuyenDoc,
                        kq.QuyenGhi,
                        kq.ODia,
                        kq.OTrong
                    );
                }

                kryptonDataGridView1.DataSource = dtSQLite;
                kryptonDataGridView2.DataSource = dtFilePhu;

                TrangDiemChoEm_FormatDataGridView(kryptonDataGridView1);
                TrangDiemChoEm_FormatDataGridView(kryptonDataGridView2);

                int mucDoLoi = ToMauDataBound();
                UpdateStatusKetLuan(mucDoLoi);
            }
            catch (Exception ex)
            {
                if (!this.IsDisposed) toolStripStatusLabel1_KetLuan.Text = $"Lỗi: {ex.Message}";
            }
            finally { if (!this.IsDisposed) StopProgressBar(); }
        }
        private void UpdateStatusKetLuan(int mucDoLoi)
        {
            if (mucDoLoi == 2) { toolStripStatusLabel1_KetLuan.Text = "Kết luận: Lỗi nghiêm trọng CSDL!"; toolStripStatusLabel1_KetLuan.ForeColor = Color.Red; }
            else if (mucDoLoi == 1) { toolStripStatusLabel1_KetLuan.Text = "Kết luận: Có tệp phụ cần kiểm tra"; toolStripStatusLabel1_KetLuan.ForeColor = Color.OrangeRed; }
            else { toolStripStatusLabel1_KetLuan.Text = "Kết luận: Hệ thống Sẵn sàng (Tốt)"; toolStripStatusLabel1_KetLuan.ForeColor = Color.Green; }
        }
        private async void Form33_KiemTraSucKhoeCSDL_Shown(object sender, EventArgs e) => await RunCheckAsync();
        private async void kryptonButton1_CapNhat_Click(object sender, EventArgs e)
        {
            kryptonButton1_CapNhat.Enabled = false;
            kryptonDataGridView1.DataSource = null;
            kryptonDataGridView2.DataSource = null;
            await RunCheckAsync();
            kryptonButton1_CapNhat.Enabled = true;
        }
        private void StartProgressBar()
        {
            if (toolStripProgressBar1 == null) return;
            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
            toolStripProgressBar1.MarqueeAnimationSpeed = 30;
            if (toolStripProgressBar1.ProgressBar.IsHandleCreated) SendMessage(toolStripProgressBar1.ProgressBar.Handle, PBM_SETSTATE, (IntPtr)PBST_NORMAL, IntPtr.Zero);
            toolStripProgressBar1.Visible = true;
        }
        private void StopProgressBar()
        {
            if (toolStripProgressBar1 == null) return;
            toolStripProgressBar1.MarqueeAnimationSpeed = 0;
            toolStripProgressBar1.Style = ProgressBarStyle.Blocks;
            toolStripProgressBar1.Visible = false;
        }
        private DataTable TaoBangThuocTinhSQLite()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("STT", typeof(int));
            dt.Columns.Add("Đơn vị");
            dt.Columns.Add("Trạng thái");
            dt.Columns.Add("Thời gian tạo");
            dt.Columns.Add("Dung lượng");
            dt.Columns.Add("Sức khỏe");
            dt.Columns.Add("Tốc độ phản hồi");
            dt.Columns.Add("Mức độ phân mảnh");
            dt.Columns.Add("Journal/WA");
            dt.Columns.Add("Đọc");
            dt.Columns.Add("Ghi");
            dt.Columns.Add("Ổ đĩa trống");
            return dt;
        }
        private DataTable TaoBangThuocTinhFilePhu()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("STT", typeof(int));
            dt.Columns.Add("Đơn vị");
            dt.Columns.Add("Trạng thái");
            dt.Columns.Add("Thời gian tạo");
            dt.Columns.Add("Dung lượng");
            dt.Columns.Add("Quyền đọc");
            dt.Columns.Add("Quyền ghi");
            dt.Columns.Add("Ổ đĩa");
            dt.Columns.Add("Ổ đĩa trống");
            return dt;
        }
        private void TrangDiemChoEm_FormatDataGridView(DataGridView dgv)
        {
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToOrderColumns = false;
            dgv.RowHeadersVisible = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.BackgroundColor = Color.White;
            dgv.BorderStyle = BorderStyle.None;
            dgv.EnableHeadersVisualStyles = false;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.ScrollBars = ScrollBars.Both;

            var headerStyle = dgv.ColumnHeadersDefaultCellStyle;
            headerStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            headerStyle.BackColor = Color.White;
            headerStyle.ForeColor = Color.Black;
            headerStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            headerStyle.WrapMode = DataGridViewTriState.True;
            headerStyle.Padding = new Padding(4, 6, 4, 6);

            dgv.ColumnHeadersHeight = 60;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(210, 230, 250);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(44, 62, 80);
            dgv.DefaultCellStyle.Padding = new Padding(3, 0, 3, 0);

            dgv.RowsDefaultCellStyle.BackColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);

            if (dgv.Columns.Count > 0)
            {
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    col.SortMode = DataGridViewColumnSortMode.NotSortable;

                    if (col.Name == "STT")
                    {
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    }
                    else
                    {
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    }
                }
                if (dgv.Columns.Contains("STT")) dgv.Columns["STT"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }
            dgv.RowTemplate.Height = 30;
        }
        private int ToMauDataBound()
        {
            int mucDoNghiemTrong = 0;

            string LayGiaTriCot(DataGridViewRow row, string tenCot)
            {
                if (row.DataGridView.Columns.Contains(tenCot) && row.Cells[tenCot].Value != null)
                {
                    return row.Cells[tenCot].Value.ToString();
                }
                return string.Empty;
            }

            foreach (DataGridViewRow row in kryptonDataGridView1.Rows)
            {
                if (row.IsNewRow) continue;
                string trangThai = LayGiaTriCot(row, "Trạng thái");
                string sucKhoe = LayGiaTriCot(row, "Sức khỏe");
                string quyenGhi = LayGiaTriCot(row, "Ghi");

                if (trangThai.Contains("FALSE") || sucKhoe.Contains("Lỗi"))
                {
                    row.DefaultCellStyle.BackColor = Color.Crimson;
                    row.DefaultCellStyle.ForeColor = Color.White;
                    mucDoNghiemTrong = Math.Max(mucDoNghiemTrong, 2);
                }
                else if (quyenGhi != "OK" || sucKhoe.Contains("khóa"))
                {
                    row.DefaultCellStyle.BackColor = Color.Khaki;
                    row.DefaultCellStyle.ForeColor = Color.Black;
                    mucDoNghiemTrong = Math.Max(mucDoNghiemTrong, 1);
                }
                else
                {
                    row.DefaultCellStyle.BackColor = Color.LightGreen;
                    row.DefaultCellStyle.ForeColor = Color.Black;
                }
            }

            foreach (DataGridViewRow row in kryptonDataGridView2.Rows)
            {
                if (row.IsNewRow) continue;
                string trangThai = LayGiaTriCot(row, "Trạng thái");
                string quyenGhi = LayGiaTriCot(row, "Quyền ghi");

                if (trangThai.Contains("FALSE"))
                {
                    row.DefaultCellStyle.BackColor = Color.Crimson;
                    row.DefaultCellStyle.ForeColor = Color.White;
                    mucDoNghiemTrong = Math.Max(mucDoNghiemTrong, 2);
                }
                else if (trangThai.Contains("dự phòng"))
                {
                    row.DefaultCellStyle.BackColor = Color.PeachPuff;
                    row.DefaultCellStyle.ForeColor = Color.Black;
                    mucDoNghiemTrong = Math.Max(mucDoNghiemTrong, 1);
                }
                else if (quyenGhi != "OK")
                {
                    row.DefaultCellStyle.BackColor = Color.Khaki;
                    row.DefaultCellStyle.ForeColor = Color.Black;
                    mucDoNghiemTrong = Math.Max(mucDoNghiemTrong, 1);
                }
                else
                {
                    row.DefaultCellStyle.BackColor = Color.LightGreen;
                    row.DefaultCellStyle.ForeColor = Color.Black;
                }
            }

            return mucDoNghiemTrong;
        }
        private async Task<KetQuaFile> CheckSQLiteAsync(string path)
        {
            var kq = new KetQuaFile();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                kq.TonTai = false; kq.SucKhoe = "Không tồn tại";
                return kq;
            }
            try
            {
                var fi = new FileInfo(path);
                kq.TonTai = true;
                kq.NgayTao = fi.CreationTime.ToString("dd/MM/yyyy HH:mm:ss");
                kq.DungLuong = FormatSize(fi.Length);
                kq.QuyenDoc = CoDoc(path);
                kq.QuyenGhi = CoGhi(path);
                kq.OTrong = LayDungLuongOTrang(path);

                var sw = Stopwatch.StartNew();
                // ⭐ Ép SQLite chỉ được đọc (ReadOnly) và Timeout ngắn để không khóa DB của User
                using (var conn = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False;Default Timeout=10;"))
                {
                    await conn.OpenAsync();
                    using var cmd = conn.CreateCommand();

                    cmd.CommandText = "PRAGMA integrity_check;";
                    var ok = (await cmd.ExecuteScalarAsync())?.ToString();

                    cmd.CommandText = "PRAGMA journal_mode;";
                    kq.Journal = (await cmd.ExecuteScalarAsync())?.ToString()?.ToUpper();

                    cmd.CommandText = "PRAGMA page_count;";
                    long.TryParse((await cmd.ExecuteScalarAsync())?.ToString(), out long pageCount);

                    cmd.CommandText = "PRAGMA freelist_count;";
                    long.TryParse((await cmd.ExecuteScalarAsync())?.ToString(), out long freeListCount);

                    if (pageCount > 0)
                    {
                        double tiLePhanManh = Math.Round((double)freeListCount / pageCount * 100, 2);
                        kq.PhanManh = $"{tiLePhanManh}% ({freeListCount} pages rỗng)";
                    }
                    else
                    {
                        kq.PhanManh = "0%";
                    }

                    kq.SucKhoe = ok?.ToLower() == "ok" ? "Tốt" : "Lỗi hỏng CSDL";
                }
                sw.Stop();
                kq.TocDo = sw.ElapsedMilliseconds + " ms";
                if (FileLock(path)) kq.SucKhoe = "Đang bị khóa (Locked)";
            }
            catch (Exception)
            {
                kq.SucKhoe = "Lỗi truy cập SQLite";
            }
            return kq;
        }
        private KetQuaFileThuong CheckFile(string path)
        {
            var kq = new KetQuaFileThuong();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                kq.TonTai = false;
                return kq;
            }
            try
            {
                var fi = new FileInfo(path);
                kq.TonTai = true;
                kq.ODia = Path.GetPathRoot(Path.GetFullPath(path));
                kq.NgayTao = fi.CreationTime.ToString("dd/MM/yyyy HH:mm:ss");
                kq.DungLuong = FormatSize(fi.Length);
                kq.QuyenDoc = CoDoc(path);
                kq.QuyenGhi = CoGhi(path);
                kq.OTrong = LayDungLuongOTrang(path);
            }
            catch
            {
                kq.TonTai = false;
            }
            return kq;
        }
        private KetQuaFileThuong CheckFolder(string path)
        {
            var kq = new KetQuaFileThuong();
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                kq.TonTai = false;
                return kq;
            }
            try
            {
                var di = new DirectoryInfo(path);
                kq.TonTai = true;
                kq.ODia = Path.GetPathRoot(Path.GetFullPath(path));
                kq.NgayTao = di.CreationTime.ToString("dd/MM/yyyy HH:mm:ss");

                long folderSize = GetDirectorySizeSafe(di);
                kq.DungLuong = folderSize > 0 ? FormatSize(folderSize) : "0 KB";
                kq.OTrong = LayDungLuongOTrang(path);

                try
                {
                    string tempFile = Path.Combine(path, Path.GetRandomFileName());
                    // ⭐ CHUẨN KỸ SƯ: Sử dụng DeleteOnClose để HĐH tự xóa rác nếu App bị Crash ngang
                    using (new FileStream(tempFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose))
                    {
                    }
                    kq.QuyenGhi = "OK";
                    kq.QuyenDoc = "OK";
                }
                catch
                {
                    kq.QuyenGhi = "Không";
                    kq.QuyenDoc = "Không rõ";
                }
            }
            catch
            {
                kq.TonTai = false;
            }
            return kq;
        }
        private long GetDirectorySizeSafe(DirectoryInfo directoryInfo)
        {
            long size = 0;
            try
            {
                FileInfo[] files = directoryInfo.GetFiles();
                foreach (FileInfo fi in files) size += fi.Length;

                DirectoryInfo[] subDirectories = directoryInfo.GetDirectories();
                foreach (DirectoryInfo subDi in subDirectories) size += GetDirectorySizeSafe(subDi);
            }
            catch (UnauthorizedAccessException) { }
            catch (Exception) { }
            return size;
        }
        private string FormatSize(long b) => b > 1024 * 1024 ? (b / 1024.0 / 1024.0).ToString("F2") + " MB" : (b / 1024.0).ToString("F2") + " KB";
        private string CoDoc(string p)
        {
            if (string.IsNullOrWhiteSpace(p) || !File.Exists(p)) return "-";
            try { using (File.Open(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) { } return "OK"; }
            catch { return "Không"; }
        }
        private string CoGhi(string p)
        {
            if (string.IsNullOrWhiteSpace(p) || !File.Exists(p)) return "-";
            try { using (File.Open(p, FileMode.Open, FileAccess.Write, FileShare.ReadWrite)) { } return "OK"; }
            catch { return "Không"; }
        }
        private bool FileLock(string p)
        {
            if (string.IsNullOrWhiteSpace(p)) return false;
            try { using (File.Open(p, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) return false; }
            catch { return true; }
        }
        private void EnableDoubleBuffer(DataGridView dgv)
        {
            typeof(DataGridView)
                .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(dgv, true);
        }
        private string LayDungLuongOTrang(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "-";
            try
            {
                string root = Path.GetPathRoot(Path.GetFullPath(path));

                if (_cacheDungLuongOTrang.TryGetValue(root, out string cachedValue))
                {
                    return cachedValue;
                }

                DriveInfo drive = new DriveInfo(root);
                if (drive.IsReady)
                {
                    long b = drive.AvailableFreeSpace;
                    string dungLuongStr = b > 1024L * 1024 * 1024 ?
                        (b / 1024.0 / 1024.0 / 1024.0).ToString("F2") + " GB" :
                        (b / 1024.0 / 1024.0).ToString("F2") + " MB";

                    _cacheDungLuongOTrang.TryAdd(root, dungLuongStr);
                    return dungLuongStr;
                }
            }
            catch { }
            return "-";
        }
        private void KryptonDataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _pathCSDL.Length) return;

            string dbPath = _pathCSDL[e.RowIndex];
            string dbName = _tenCSDL[e.RowIndex];

            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
            {
                MessageBox.Show($"Cơ sở dữ liệu [{dbName}] không tồn tại hoặc sai đường dẫn.", "Lỗi truy xuất", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Lấy chuỗi thông tin kỹ thuật siêu dài
            string thongTinKiemTra = LayThongTinChuyenMonSQLite(dbPath);

            // Lời mào đầu
            string moTa = $"Dưới đây là thông số kỹ thuật nội hàm được trích xuất trực tiếp từ tệp tin [{dbName}].";

            // Gọi Form Ảo DataGrid thay vì MessageBox
            HienThiFormAo_ThongBaoLuoi($"CHI TIẾT CHUYÊN MÔN CSDL", moTa, thongTinKiemTra);
        }

        #region BỘ THƯ VIỆN FORM ẢO CHỐNG GIẬT (DÙNG CHO FORM 33)

        /// <summary>
        /// Dựng Form ảo kế thừa FormAoBase. 
        /// Tự động tách chuỗi thành Bảng lưới (DataGridView) và Tự động tô màu Xanh/Đỏ.
        /// </summary>
        private void HienThiFormAo_ThongBaoLuoi(string tieuDe, string moTa, string duLieuRaw)
        {
            using (var formAo = new FormAoBase())
            {
                formAo.Text = "Hệ thống thông báo";
                formAo.Size = new System.Drawing.Size(1280, 850); // Nới form to ra để chứa đủ thông số DB
                formAo.FormBorderStyle = FormBorderStyle.FixedDialog;
                formAo.MaximizeBox = false; formAo.MinimizeBox = false;
                formAo.ShowIcon = false; formAo.ShowInTaskbar = false;

                // --- 1. PANEL TIÊU ĐỀ ---
                var panelTop = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(25, 20, 20, 5) };
                panelTop.StateCommon.Color1 = System.Drawing.Color.White;

                var lblTitle = new Krypton.Toolkit.KryptonLabel { Text = tieuDe.ToUpper(), Dock = DockStyle.Fill, AutoSize = false };
                lblTitle.StateCommon.ShortText.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
                lblTitle.StateCommon.ShortText.Color1 = System.Drawing.Color.FromArgb(0, 82, 155);
                panelTop.Controls.Add(lblTitle);

                // --- 2. PANEL NỘI DUNG ---
                var panelContent = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Fill, Padding = new Padding(25, 10, 25, 15) };
                panelContent.StateCommon.Color1 = System.Drawing.Color.White;

                // Đoạn mô tả mào đầu
                var lblMoTa = new Krypton.Toolkit.KryptonWrapLabel
                {
                    Text = moTa,
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    Margin = new Padding(0, 0, 0, 10)
                };
                lblMoTa.StateCommon.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Regular);
                lblMoTa.StateCommon.TextColor = System.Drawing.Color.FromArgb(50, 50, 50);

                // Tạo Grid siêu mượt
                var grid = new Krypton.Toolkit.KryptonDataGridView
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    AllowUserToResizeRows = false,
                    RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    MultiSelect = false,
                    BackgroundColor = System.Drawing.Color.White,
                    BorderStyle = BorderStyle.None,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    Cursor = Cursors.Hand
                };

                grid.GridStyles.Style = Krypton.Toolkit.DataGridViewStyle.List;
                grid.StateCommon.DataCell.Content.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular);
                grid.StateCommon.HeaderColumn.Content.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);

                grid.Columns.Add("Cot1", "Hạng mục kiểm tra");
                grid.Columns.Add("Cot2", "Giá trị / Trạng thái");
                grid.Columns[0].FillWeight = 50;
                grid.Columns[1].FillWeight = 50;
                grid.Columns[0].DefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);

                // --- BỘ NÃO PHÂN TÍCH CHUỖI VÀ TÔ MÀU ---
                string[] lines = duLieuRaw.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    int splitIdx = line.IndexOf(':');
                    if (splitIdx > 0)
                    {
                        string key = line.Substring(0, splitIdx).Trim();
                        // Nếu chuỗi key có chứa gạch đầu dòng "-", ta bỏ nó đi cho đẹp
                        if (key.StartsWith("- ")) key = key.Substring(2);

                        string val = line.Substring(splitIdx + 1).Trim();

                        int rIdx = grid.Rows.Add(key, val);
                        var cellVal = grid.Rows[rIdx].Cells[1];

                        // 🔥 ĐỘNG CƠ TÔ MÀU THÔNG MINH
                        string valLow = val.ToLower();
                        if (valLow.Contains("tối ưu") || valLow.Contains("bật (an toàn") || valLow.Contains("full - đảm bảo"))
                        {
                            cellVal.Style.ForeColor = System.Drawing.Color.FromArgb(34, 139, 34); // Xanh lá đậm
                            cellVal.Style.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
                        }
                        else if (valLow.Contains("khuyến nghị") || valLow.Contains("nguy cơ") || valLow.Contains("thủ công"))
                        {
                            cellVal.Style.ForeColor = System.Drawing.Color.FromArgb(211, 47, 47); // Đỏ đô
                            cellVal.Style.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
                        }
                        else
                        {
                            cellVal.Style.ForeColor = System.Drawing.Color.FromArgb(45, 45, 45); // Xám đậm
                        }
                    }
                    else
                    {
                        // Dòng Header (=== THÔNG SỐ... ===)
                        string headerText = line.Replace("===", "").Trim();
                        int rIdx = grid.Rows.Add(headerText, "");
                        grid.Rows[rIdx].DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(240, 248, 255);
                        grid.Rows[rIdx].DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(0, 82, 155);
                    }
                }

                // --- 3. PANEL NÚT ĐÓNG ---
                var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 65, BackColor = System.Drawing.Color.WhiteSmoke };
                var btnClose = new Krypton.Toolkit.KryptonButton { Text = "Đóng", Width = 120, Height = 38, DialogResult = DialogResult.OK };
                btnClose.StateCommon.Content.ShortText.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
                btnClose.StateCommon.Border.Rounding = 5;
                btnClose.Location = new System.Drawing.Point((formAo.Width - btnClose.Width) / 2, 13);
                panelBottom.Controls.Add(btnClose);

                // --- 4. RÁP LAYER LÊN FORM ---
                panelContent.Controls.Add(grid);
                if (!string.IsNullOrWhiteSpace(moTa))
                {
                    panelContent.Controls.Add(lblMoTa);
                    lblMoTa.SendToBack();
                }
                grid.BringToFront();

                formAo.Controls.Add(panelContent);
                formAo.Controls.Add(panelTop);
                formAo.Controls.Add(panelBottom);

                formAo.AcceptButton = btnClose;
                formAo.CancelButton = btnClose;

                formAo.Shown += (s, ev) => grid.ClearSelection();

                formAo.ShowDialog(this);
            }
        }

        #endregion
        private void KryptonDataGridView1_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                kryptonDataGridView1.ClearSelection();
                kryptonDataGridView1.Rows[e.RowIndex].Selected = true;
                kryptonDataGridView1.CurrentCell = kryptonDataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
            }
        }
        private string LayThongTinChuyenMonSQLite(string path)
        {
            try
            {
                using (var conn = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False;Default Timeout=10;"))
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();

                    string pageSize = ExecuteScalarString(cmd, "PRAGMA page_size;") ?? "0";
                    string pageCount = ExecuteScalarString(cmd, "PRAGMA page_count;") ?? "0";
                    string freeList = ExecuteScalarString(cmd, "PRAGMA freelist_count;") ?? "0";
                    string journalMode = ExecuteScalarString(cmd, "PRAGMA journal_mode;") ?? "?";
                    string syncMode = ExecuteScalarString(cmd, "PRAGMA synchronous;") ?? "?";
                    string autoVacuum = ExecuteScalarString(cmd, "PRAGMA auto_vacuum;") ?? "?";
                    string userVersion = ExecuteScalarString(cmd, "PRAGMA user_version;") ?? "?";
                    string encoding = ExecuteScalarString(cmd, "PRAGMA encoding;") ?? "?";
                    string foreignKeys = ExecuteScalarString(cmd, "PRAGMA foreign_keys;") ?? "0";
                    string cacheSize = ExecuteScalarString(cmd, "PRAGMA cache_size;") ?? "0";
                    string busyTimeout = ExecuteScalarString(cmd, "PRAGMA busy_timeout;") ?? "0";

                    string countTables = ExecuteScalarString(cmd, "SELECT COUNT(*) FROM sqlite_master WHERE type='table';") ?? "0";
                    string countIndexes = ExecuteScalarString(cmd, "SELECT COUNT(*) FROM sqlite_master WHERE type='index';") ?? "0";
                    string countViews = ExecuteScalarString(cmd, "SELECT COUNT(*) FROM sqlite_master WHERE type='view';") ?? "0";
                    string countTriggers = ExecuteScalarString(cmd, "SELECT COUNT(*) FROM sqlite_master WHERE type='trigger';") ?? "0";

                    long.TryParse(pageSize, out long ps);
                    long.TryParse(pageCount, out long pc);
                    long.TryParse(freeList, out long fl);

                    long sizeBytes = ps * pc;
                    long wasteBytes = ps * fl;
                    double phanManhPhanTram = pc > 0 ? Math.Round((double)fl / pc * 100, 2) : 0;

                    string canhBaoVaccum = phanManhPhanTram > 10 ? " ⚠️ (Khuyến nghị chạy VACUUM)" : " ✔️ (Tối ưu)";
                    string fkStatus = foreignKeys == "1" ? "Bật (An toàn toàn vẹn dữ liệu)" : "Tắt ⚠️ (Nguy cơ rác quan hệ)";

                    string walPath = path + "-wal";
                    string shmPath = path + "-shm";
                    string thongTinFileDem = "";

                    if (journalMode.ToLower() == "wal")
                    {
                        long walSize = File.Exists(walPath) ? new FileInfo(walPath).Length : 0;
                        long shmSize = File.Exists(shmPath) ? new FileInfo(shmPath).Length : 0;
                        thongTinFileDem = $"\n- Tệp Write-Ahead Log (-wal): {FormatSize(walSize)}" +
                                          $"\n- Tệp Shared Memory (-shm): {FormatSize(shmSize)}\n";
                    }

                    string syncStr = syncMode == "0" ? "0 (OFF - Ưu tiên tốc độ tối đa)" :
                                     syncMode == "1" ? "1 (NORMAL - Cân bằng mặc định)" :
                                     syncMode == "2" ? "2 (FULL - Đảm bảo an toàn tuyệt đối)" : syncMode;

                    string vacStr = autoVacuum == "0" ? "0 (NONE - Thủ công)" :
                                    autoVacuum == "1" ? "1 (FULL - Tự động toàn phần)" :
                                    autoVacuum == "2" ? "2 (INCREMENTAL - Tự động từng phần)" : autoVacuum;

                    return $"=== THÔNG SỐ VẬT LÝ & KIẾN TRÚC ===\n" +
                           $"- Bộ mã hóa (Encoding): {encoding}\n" +
                           $"- Kích thước 1 Page: {pageSize} bytes\n" +
                           $"- Tổng số Page: {pageCount}\n" +
                           $"- Dung lượng Core lý thuyết: {FormatSize(sizeBytes)}\n" +
                           $"- Mức độ phân mảnh: {phanManhPhanTram}% ({FormatSize(wasteBytes)} rác){canhBaoVaccum}\n" +
                           thongTinFileDem +
                           $"\n=== CẤU HÌNH VẬN HÀNH (TUNING) ===\n" +
                           $"- Phiên bản App (User Version): {userVersion}\n" +
                           $"- Ràng buộc khóa ngoại (Foreign Keys): {fkStatus}\n" +
                           $"- Cơ chế ghi nhật ký (Journal Mode): {journalMode.ToUpper()}\n" +
                           $"- Đồng bộ ổ cứng (Synchronous): {syncStr}\n" +
                           $"- Cơ chế dọn rác (Auto Vacuum): {vacStr}\n" +
                           $"- Kích thước Cache đệm (Cache Size): {cacheSize} pages\n" +
                           $"- Thời gian chờ khóa (Busy Timeout): {busyTimeout} ms\n" +
                           $"\n=== CẤU TRÚC LƯU TRỮ (SCHEMA) ===\n" +
                           $"- Bảng dữ liệu (Tables): {countTables}\n" +
                           $"- Chỉ mục tối ưu (Indexes): {countIndexes}\n" +
                           $"- Khung nhìn ảo (Views): {countViews}\n" +
                           $"- Triggers tự động: {countTriggers}\n";
                }
            }
            catch (Exception ex)
            {
                return $"Không thể trích xuất thông tin kỹ thuật sâu.\nCSDL có thể đang bị khóa bởi tiến trình khác.\n\nMã lỗi: {ex.Message}";
            }
        }
        private string ExecuteScalarString(SqliteCommand cmd, string sql)
        {
            cmd.CommandText = sql;
            return cmd.ExecuteScalar()?.ToString();
        }
        // ====================================================================================
        // 🚀 HÀM TỐI ƯU CSDL DUY NHẤT (SMART VACUUM - CHỐNG BÀO MÒN SSD & ANTI-CORRUPTION)
        // ====================================================================================
        private async Task<(bool ThanhCong, string ThongBao)> ToiUuHoaCSDLAsync(string dbPath, string dbName)
        {
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
            {
                return (false, $"File CSDL {dbName} không tồn tại!");
            }

            try
            {
                // Khai báo biến đếm rác ở ngoài khối using để có thể sử dụng ở lệnh return cuối hàm
                long freeListCount = 0;
                double tiLeRac = 0.0;

                var builder = new SqliteConnectionStringBuilder
                {
                    DataSource = dbPath,
                    Mode = SqliteOpenMode.ReadWrite,
                    Pooling = false,
                    DefaultTimeout = 30
                };

                using (var conn = new SqliteConnection(builder.ConnectionString))
                {
                    await conn.OpenAsync();

                    // 1. KIỂM TRA SỨC KHỎE (INTEGRITY CHECK) TRƯỚC KHI LÀM
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA integrity_check;";
                        var integrityResult = (await cmd.ExecuteScalarAsync())?.ToString();

                        if (!string.Equals(integrityResult, "ok", StringComparison.OrdinalIgnoreCase))
                        {
                            return (false, "CSDL đang bị lỗi cấu trúc (Corrupt). TUYỆT ĐỐI KHÔNG DỌN DẸP để bảo vệ dữ liệu còn lại!");
                        }
                    }

                    // 2. ĐO LƯỜNG TỶ LỆ RÁC TRONG DB
                    long pageCount = 0;
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA page_count;";
                        pageCount = Convert.ToInt64(await cmd.ExecuteScalarAsync() ?? 0);

                        cmd.CommandText = "PRAGMA freelist_count;";
                        freeListCount = Convert.ToInt64(await cmd.ExecuteScalarAsync() ?? 0);
                    }

                    if (pageCount == 0) return (true, "Cơ sở dữ liệu trống, không cần tối ưu.");

                    tiLeRac = (freeListCount * 100.0) / pageCount;

                    // 3. CHÍNH SÁCH BẢO VỆ SSD (Chỉ Vacuum khi rác > 15% hoặc số trang rác quá lớn)
                    if (tiLeRac < 15.0 && freeListCount < 5000)
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "PRAGMA optimize;";
                            await cmd.ExecuteNonQueryAsync();
                        }
                        return (true, $"Mức độ phân mảnh rất thấp ({tiLeRac:F2}%). Hệ thống tự động BỎ QUA lệnh VACUUM và chỉ chạy tối ưu nhẹ (Optimize) để bảo vệ tuổi thọ ổ cứng.");
                    }

                    // 4. THỰC THI VACUUM TOÀN PHẦN (Khi thực sự cần thiết)
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                        await cmd.ExecuteNonQueryAsync();

                        cmd.CommandText = "PRAGMA optimize;";
                        await cmd.ExecuteNonQueryAsync();

                        cmd.CommandText = "VACUUM;";
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return (true, $"Dọn dẹp thành công!\nHệ thống đã thu hồi {freeListCount} trang rác ({tiLeRac:F2}%) trả lại không gian tối đa cho ổ cứng.");
            }
            catch (SqliteException sqlEx) when (sqlEx.SqliteErrorCode == 5 || sqlEx.SqliteErrorCode == 6)
            {
                return (false, $"CSDL đang bị khóa bởi tiến trình khác (SQLITE_BUSY).\nVui lòng đảm bảo không ai đang ghi dữ liệu khi chạy dọn dẹp.\n\nChi tiết: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi không xác định khi dọn dẹp: {ex.Message}");
            }
        }
        // ====================================================================================
        // 🚀 SỰ KIỆN GỌI MENU DUY NHẤT
        // ====================================================================================
        private async void chayVacuum_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (kryptonDataGridView1.CurrentRow == null || kryptonDataGridView1.CurrentRow.Index < 0)
            {
                MessageBox.Show("Vui lòng chọn một Cơ sở dữ liệu cần dọn dẹp!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int rowIndex = kryptonDataGridView1.CurrentRow.Index;
            if (rowIndex >= _pathCSDL.Length) return;

            string dbPath = _pathCSDL[rowIndex];
            string dbName = _tenCSDL[rowIndex];

            var xacNhan = MessageBox.Show($"Bạn có chắc chắn muốn phân tích và dọn dẹp cho:\n[{dbName}]?\n\nLưu ý: Hệ thống sẽ tự động đo lường và quyết định xem có cần thiết dọn dẹp hay không để bảo vệ ổ cứng.",
                                          "Xác nhận dọn dẹp thông minh", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (xacNhan != DialogResult.Yes) return;

            try
            {
                chayVacuum_ToolStripMenuItem.Enabled = false;

                StartProgressBar();
                toolStripStatusLabel1_KetLuan.Text = $"Đang phân tích và tối ưu: {dbName} ...";
                toolStripStatusLabel1_KetLuan.ForeColor = Color.Blue;

                var (ThanhCong, ThongBao) = await ToiUuHoaCSDLAsync(dbPath, dbName);

                if (ThanhCong)
                {
                    MessageBox.Show(ThongBao, "Hoàn tất tối ưu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await RunCheckAsync();
                }
                else
                {
                    MessageBox.Show(ThongBao, "Không thể dọn dẹp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    toolStripStatusLabel1_KetLuan.Text = "Tối ưu hóa bị hủy do lỗi hoặc cảnh báo.";
                    toolStripStatusLabel1_KetLuan.ForeColor = Color.Red;
                }
            }
            finally
            {
                chayVacuum_ToolStripMenuItem.Enabled = true;
                StopProgressBar();
            }
        }
        private void lamMoi_ToolStripMenuItem_Click(object sender, EventArgs e) => kryptonButton1_CapNhat.PerformClick();
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            kryptonDataGridView1.CellPainting -= KryptonDataGridView_CellPainting_CanhBao;
            kryptonDataGridView2.CellPainting -= KryptonDataGridView_CellPainting_CanhBao;

            _iconSafe?.Dispose();
            _iconWarning?.Dispose();

            base.OnFormClosing(e);
        }
        private void kryptonButton1_CauhinhCSDL_Click(
       object sender,
       EventArgs e)
        {
            try
            {
                FormManager.OpenOrBringToFront<Form8_CauHinhCSDL>(this);
            }
            catch (Exception ex)
            {
                Form33_KiemTraSucKhoeCSDL.HienThiFormAo_Loi(
                    this,
                    "KHÔNG THỂ MỞ CẤU HÌNH CSDL",
                    ex.Message);
            }
        }
        public static void HienThiFormAo_Loi(
       IWin32Window? owner,
       string tieuDe,
       string noiDung)
        {
            try
            {
                using FormAoBase frm = new();

                frm.Text = "Lỗi hệ thống";
                frm.Size = new Size(620, 320);
                frm.FormBorderStyle = FormBorderStyle.FixedDialog;
                frm.MaximizeBox = false;
                frm.MinimizeBox = false;
                frm.ShowInTaskbar = false;

                KryptonPanel panel = new()
                {
                    Dock = DockStyle.Fill
                };

                KryptonLabel lblTitle = new()
                {
                    Dock = DockStyle.Top,
                    Height = 60,
                    Text = tieuDe
                };

                lblTitle.StateCommon.ShortText.Font =
                    new Font("Segoe UI", 12F, FontStyle.Bold);

                lblTitle.StateCommon.ShortText.Color1 =
                    Color.FromArgb(198, 40, 40);

                KryptonRichTextBox txt = new()
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    Text = noiDung
                };

                txt.StateCommon.Border.Draw =
                    InheritBool.False;

                txt.StateCommon.Content.Font =
                    new Font("Segoe UI", 10F);

                KryptonButton btn = new()
                {
                    Text = "Đóng",
                    Width = 120,
                    Height = 38
                };

                btn.Location = new Point(
                    (frm.ClientSize.Width - btn.Width) / 2,
                    240);

                btn.Click += (_, _) => frm.Close();

                panel.Controls.Add(txt);
                panel.Controls.Add(lblTitle);
                panel.Controls.Add(btn);

                frm.Controls.Add(panel);

                if (owner != null)
                    frm.ShowDialog(owner);
                else
                    frm.ShowDialog();
            }
            catch
            {
                // Silent
            }
        }
        private void ToolStripMenuItem_ChiTietCSDL_Click(object sender, EventArgs e)
        {
            // Thêm sự kiện để có thể thay hành động đúp chuột vào DataGridView
            KryptonDataGridView1_CellDoubleClick(sender, new DataGridViewCellEventArgs(0, 0));
        }
    }
    class KetQuaFile
    {
        public bool TonTai { get; set; }
        public string NgayTao { get; set; } = "-";
        public string DungLuong { get; set; } = "-";
        public string SucKhoe { get; set; } = "-";
        public string TocDo { get; set; } = "-";
        public string PhanManh { get; set; } = "-";
        public string Journal { get; set; } = "-";
        public string QuyenDoc { get; set; } = "-";
        public string QuyenGhi { get; set; } = "-";
        public string OTrong { get; set; } = "-";
    }
    class KetQuaFileThuong
    {
        public bool TonTai { get; set; }
        public string TrangThai { get; set; } = "-";
        public string ODia { get; set; } = "-";
        public string NgayTao { get; set; } = "-";
        public string DungLuong { get; set; } = "-";
        public string QuyenDoc { get; set; } = "-";
        public string QuyenGhi { get; set; } = "-";
        public string OTrong { get; set; } = "-";
    }

}