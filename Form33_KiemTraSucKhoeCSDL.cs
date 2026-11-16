///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
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
        public static readonly string ThuMucCoSoDuLieu = Path.Combine(AppContext.BaseDirectory, "Database", "HuongDanSuDung");
        // ⭐ CHUẨN KỸ SƯ: Sử dụng ConcurrentDictionary để chống Crash khi chạy Đa luồng
        private const long NGUONG_KIEM_TRA_DAY_DU_BYTES = 64L * 1024 * 1024; // CSDL > 64MB: dùng quick_check thay integrity_check
        private readonly ConcurrentDictionary<string, string> _cacheDungLuongOTrang = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // Clone để form này sở hữu bản riêng -> Dispose khi đóng không ảnh hưởng nơi khác
        private readonly Image _iconSafe = (Image)Properties.Resources._true.Clone();
        private readonly Image _iconWarning = (Image)Properties.Resources._false.Clone();
        // Quản lý Font tập trung dùng biến hệ thống, chống rò rỉ GDI handle
        private static readonly Font _fontGridHeader9Bold = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Bold);
        private static readonly Font _fontGridCell9 = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Regular);
        private static readonly Font _fontFormAoTitle12Bold = new Font(Module_HeThong.TenFontHeThong, 12F, FontStyle.Bold);
        private static readonly Font _fontFormAoMoTa105 = new Font(Module_HeThong.TenFontHeThong, 10.5F, FontStyle.Regular);
        private static readonly Font _fontFormAoGrid10 = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Regular);
        private static readonly Font _fontFormAoGrid10Bold = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Bold);
        private static readonly Font _fontBtn95Bold = new Font(Module_HeThong.TenFontHeThong, 9.5F, FontStyle.Bold);
        // Cache kết quả kiểm tra toàn vẹn: chỉ quét lại khi tệp thật sự thay đổi
        private static readonly ConcurrentDictionary<string, (string Key, string Ok)> _cacheToanVen = new(StringComparer.OrdinalIgnoreCase);
        // Cache dung lượng thư mục (10 phút) – nút "Làm mới" sẽ xóa cache này
        private static readonly ConcurrentDictionary<string, (DateTime Time, long Size)> _cacheKichThuocThuMuc = new(StringComparer.OrdinalIgnoreCase);
        private DataTable _dtSQLite = null!;
        private DataTable _dtFilePhu = null!;
        private int _daKhoiChay;       // 0 = chưa chạy lần kiểm tra đầu tiên
        private int _thuTuKiemTra;     // số thứ tự lần quét: lần quét cũ tự bỏ kết quả khi có lần mới hoặc form đóng
        private const string DANG_KIEM_TRA = "Đang kiểm tra...";
        private const string DANG_TINH = "Đang tính...";
        private const string CHO = "…";
        public Form33_KiemTraSucKhoeCSDL()
        {
            InitializeComponent();
            this.Shown += Form33_KiemTraSucKhoeCSDL_Shown;
            _pathCSDL = new[]
            {
                Module_DanduongGPS.DuongDanCSDL1,
                Module_DanduongGPS.DuongDanCSDL2,
                Module_DanduongGPS.DuongDanCSDL3,
                Module_DanduongGPS.DuongDanCSDL4
            };
            _pathsFilePhu = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Database Backup", "CongCuQuanLyCSDL"),
                Module_DanduongGPS.DuongDanCSDL4ex,
                ThuMucCoSoDuLieu
            };
            kryptonDataGridView1.CellPainting += KryptonDataGridView_CellPainting_CanhBao;
            kryptonDataGridView2.CellPainting += KryptonDataGridView_CellPainting_CanhBao;
            // Form đóng/giải phóng: hủy lần quét đang chạy dở (kết quả muộn bị bỏ) và trả icon
            Disposed += (s, e) =>
            {
                Interlocked.Increment(ref _thuTuKiemTra);
                _iconSafe?.Dispose();
                _iconWarning?.Dispose();
            };
            GiaoDienForm33Web.ApDung(this);
        }
        private void Form33_KiemTraSucKhoeCSDL_Load(object? sender, EventArgs e)
        {
            Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1);
            label_TenAdminDangNhap.Text = "Người khai thác: " + SessionInfo.TenTaiKhoan;
            toolStripProgressBar1.Visible = false;
            toolStripStatusLabel1_ThoiGianKhaiThac.Text = "Ngày kiểm tra: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            toolStripStatusLabel1_ThoiGianKhaiThac.ForeColor = GiaoDienForm33Web.MauChuPhu;
            toolStripStatusLabel1_KetLuan.Spring = true;
            toolStripStatusLabel1_KetLuan.TextAlign = ContentAlignment.MiddleRight;
            toolStripStatusLabel1_KetLuan.Text = "Kết luận: Đang kiểm tra...";
            toolStripStatusLabel1_KetLuan.ForeColor = GiaoDienForm33Web.MauChuPhu;
            EnableDoubleBuffer(kryptonDataGridView1);
            EnableDoubleBuffer(kryptonDataGridView2);
            kryptonDataGridView1.CellDoubleClick += KryptonDataGridView1_CellDoubleClick;
            kryptonDataGridView1.CellMouseDown += KryptonDataGridView1_CellMouseDown;
            lable_phienban.Text = "Phiên bản: " + Module_PhienBan.SoftwareVersion + "; Cập nhật: " + Module_PhienBan.NgayThangNam;
            InitToolTips();
            // Dựng sẵn khung 2 bảng với dòng "Đang kiểm tra..." -> người dùng thấy đủ bố cục ngay, số liệu điền dần
            KhoiTaoBang();
            // Chạy kiểm tra ngay khi form vừa dựng xong (không đợi Shown); Shown chỉ là phương án dự phòng
            BeginInvoke(new Action(async () => await ChayLanDauAsync()));
        }
        private void KhoiTaoBang()
        {
            _dtSQLite = TaoBangThuocTinhSQLite();
            _dtFilePhu = TaoBangThuocTinhFilePhu();
            for (int i = 0; i < _tenCSDL.Length; i++) _dtSQLite.Rows.Add(DongCho(i, true));
            for (int i = 0; i < _tenFilePhu.Length; i++) _dtFilePhu.Rows.Add(DongCho(i, false));
            kryptonDataGridView1.DataSource = _dtSQLite;
            kryptonDataGridView2.DataSource = _dtFilePhu;
            TrangDiemChoEm_FormatDataGridView(kryptonDataGridView1);
            TrangDiemChoEm_FormatDataGridView(kryptonDataGridView2);
            ToMauDataBound();
        }
        private object[] DongCho(int i, bool laSQLite) => laSQLite
            ? new object[] { i + 1, _tenCSDL[i], CHO, CHO, CHO, DANG_KIEM_TRA, CHO, CHO, CHO, CHO, CHO, CHO }
            : new object[] { i + 1, _tenFilePhu[i], CHO, CHO, CHO, CHO, CHO, CHO, CHO };
        private void DatLaiBangDangKiemTra()
        {
            if (_dtSQLite == null || _dtFilePhu == null) return;
            for (int i = 0; i < _dtSQLite.Rows.Count; i++) _dtSQLite.Rows[i].ItemArray = DongCho(i, true);
            for (int i = 0; i < _dtFilePhu.Rows.Count; i++) _dtFilePhu.Rows[i].ItemArray = DongCho(i, false);
            ToMauDataBound();
        }
        //Mèo cam ----- Ngoài luồng
        private void GanToolTipAnToan(Control? control, string? noiDung)
        {
            if (control is null || toolTip1 is null)
                return;
            if (control.IsDisposed || control.Disposing)
                return;
            if (string.IsNullOrWhiteSpace(noiDung))
                return;
            try
            {
                toolTip1.SetToolTip(control, noiDung);
            }
            catch (ObjectDisposedException)
            {
                // Control hoặc ToolTip vừa bị giải phóng trong lúc thao tác.
            }
            catch (InvalidOperationException)
            {
                // Control không còn ở trạng thái hợp lệ để gán ToolTip.
            }
        }
        private void InitToolTips()
        {
            // KHỞI TẠO TOOLTIP - ỔN ĐỊNH CHO HỆ THỐNG NỘI BỘ
            // 1. Kiểm tra ToolTip
            if (toolTip1 == null)
                return;
            try
            {
                // 2. Cấu hình chung
                toolTip1.IsBalloon = true;
                toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
                toolTip1.ToolTipIcon = ToolTipIcon.Info;
                // Thời gian chờ trước khi hiển thị
                toolTip1.InitialDelay = 300;
                // Thời gian Tooltip hiển thị
                toolTip1.AutoPopDelay = 2500;
                // Thời gian chờ khi chuyển sang Control khác
                toolTip1.ReshowDelay = 100;
                // Cho phép hiển thị ngay cả khi Form chưa active
                toolTip1.ShowAlways = true;
                // 3. Gán Tooltip cho từng Control
                GanToolTipAnToan(kryptonButton1_KiemTraTaiNguyenLoi, "Kiểm tra tài nguyên lõi của hệ thống");
                GanToolTipAnToan(kryptonButton1_CapNhat, "Làm mới các hàm kiểm tra tài nguyên");
                GanToolTipAnToan(kryptonButton1_CauhinhCSDL, "Phần mềm xem và chỉnh sửa cơ sở dữ liệu");
            }
            catch (ObjectDisposedException)
            {
                // ToolTip hoặc Control đã được giải phóng trong lúc thao tác.
                // Không để chức năng Tooltip ảnh hưởng đến hoạt động chính.
            }
            catch (InvalidOperationException)
            {
                // Trạng thái WinForms không phù hợp để cấu hình Tooltip.
                // Không để chức năng Tooltip làm Form dừng hoạt động.
            }
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
        private Image? GetIconByStatus(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            foreach (var k in GridConfig.WarningKeywords)
                if (value.Contains(k, StringComparison.OrdinalIgnoreCase)) return _iconWarning;
            foreach (var k in GridConfig.SafeKeywords)
                if (value.Contains(k, StringComparison.OrdinalIgnoreCase)) return _iconSafe;
            return null;
        }
        private void KryptonDataGridView_CellPainting_CanhBao(object? sender, DataGridViewCellPaintingEventArgs e)
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
        private void UpdateStatusKetLuan(int mucDoLoi)
        {
            if (mucDoLoi == 2) { toolStripStatusLabel1_KetLuan.Text = "Kết luận: Lỗi nghiêm trọng CSDL!"; toolStripStatusLabel1_KetLuan.ForeColor = GiaoDienForm33Web.MauLoi; }
            else if (mucDoLoi == 1) { toolStripStatusLabel1_KetLuan.Text = "Kết luận: Có tệp phụ cần kiểm tra"; toolStripStatusLabel1_KetLuan.ForeColor = GiaoDienForm33Web.MauCanhBao; }
            else { toolStripStatusLabel1_KetLuan.Text = "Kết luận: Hệ thống Sẵn sàng (Tốt)"; toolStripStatusLabel1_KetLuan.ForeColor = GiaoDienForm33Web.MauOk; }
        }
        private async void Form33_KiemTraSucKhoeCSDL_Shown(object? sender, EventArgs e) => await ChayLanDauAsync();
        private async Task ChayLanDauAsync()
        {
            if (Interlocked.Exchange(ref _daKhoiChay, 1) == 1) return;   // chỉ chạy lần đầu một lần
            await RunCheckAsync();
        }
        private bool DaCu(int thuTu) => IsDisposed || thuTu != Volatile.Read(ref _thuTuKiemTra);
        private int _dangCapNhat = 0; // Cờ khóa re-entry
        private async void kryptonButton1_CapNhat_Click(object? sender, EventArgs e)
        {
            // 1. CHỐNG DOUBLE CLICK / RE-ENTRY BẰNG ATOMIC OPERATION
            if (Interlocked.Exchange(ref _dangCapNhat, 1) == 1)
                return;
            _cacheKichThuocThuMuc.Clear();
            _cacheToanVen.Clear();
            // 2. LƯU GIAO DIỆN GỐC ĐỂ KHÔI PHỤC
            string textGoc = kryptonButton1_CapNhat.Values.Text;
            Image? imageGoc = kryptonButton1_CapNhat.Values.Image;
            try
            {
                if (IsDisposed || !IsHandleCreated)
                    return;
                kryptonButton1_CapNhat.Enabled = false;
                kryptonButton1_CapNhat.Values.Text = "Đang cập nhật...";
                kryptonButton1_CapNhat.Values.Image = null;
                // Không còn hộp "Đang cập nhật" chặn màn hình: bảng giữ nguyên bố cục, từng dòng báo "Đang kiểm tra..." rồi điền dần
                await RunCheckAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi trong quá trình cập nhật:\n\n{ex.Message}",
                    "Lỗi Cập Nhật",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                try
                {
                    if (!IsDisposed && IsHandleCreated)
                    {
                        kryptonButton1_CapNhat.Values.Text = textGoc;
                        kryptonButton1_CapNhat.Values.Image = imageGoc;
                        kryptonButton1_CapNhat.Enabled = true;
                    }
                }
                catch { }
                Interlocked.Exchange(ref _dangCapNhat, 0);
            }
        }
        /// <summary>
        /// Quét theo 2 giai đoạn, mỗi dòng độc lập:
        ///  1) NHANH (mili-giây): tệp tồn tại, ngày tạo, dung lượng, quyền, journal, phân mảnh -> điền bảng ngay.
        ///  2) NẶNG (quick_check, quét dung lượng thư mục): chạy song song, xong dòng nào điền dòng đó.
        /// </summary>
        private async Task RunCheckAsync()
        {
            if (this.IsDisposed) return;
            int thuTu = Interlocked.Increment(ref _thuTuKiemTra);
            try
            {
                _cacheDungLuongOTrang.Clear();
                toolStripStatusLabel1_KetLuan.Text = "Kết luận: Đang xử lý...";
                toolStripStatusLabel1_KetLuan.ForeColor = GiaoDienForm33Web.MauChuPhu;
                StartProgressBar();
                DatLaiBangDangKiemTra();
                var viec = new List<Task>(_pathCSDL.Length + _pathsFilePhu.Length);
                for (int i = 0; i < _pathCSDL.Length; i++) viec.Add(KiemTraMotCSDLAsync(i, thuTu));
                for (int i = 0; i < _pathsFilePhu.Length; i++) viec.Add(KiemTraMotTepPhuAsync(i, thuTu));
                await Task.WhenAll(viec);
                if (DaCu(thuTu)) return;
                UpdateStatusKetLuan(ToMauDataBound());
            }
            catch (Exception ex)
            {
                if (!DaCu(thuTu)) toolStripStatusLabel1_KetLuan.Text = $"Lỗi: {ex.Message}";
            }
            finally
            {
                // Chỉ lần quét mới nhất được tắt thanh tiến trình
                if (!DaCu(thuTu)) StopProgressBar();
            }
        }
        private async Task KiemTraMotCSDLAsync(int i, int thuTu)
        {
            try
            {
                string path = _pathCSDL[i] ?? string.Empty;
                var kq = await Task.Run(() => DocNhanhSQLite(path));
                if (DaCu(thuTu)) return;
                GhiHangSQLite(i, kq);
                if (!kq.CanQuetSau) return;
                string sucKhoe = await Task.Run(() => QuetToanVenSQLite(path, kq.KhoaCache, kq.BiKhoa));
                if (DaCu(thuTu)) return;
                kq.SucKhoe = sucKhoe;
                GhiHangSQLite(i, kq);
            }
            catch (Exception ex) { Debug.WriteLine("[Form33] CSDL " + i + ": " + ex.Message); }
        }
        private async Task KiemTraMotTepPhuAsync(int i, int thuTu)
        {
            try
            {
                string path = _pathsFilePhu[i] ?? string.Empty;
                var kq = await Task.Run(() => Directory.Exists(path) ? CheckFolder(path) : CheckFile(path));
                if (DaCu(thuTu)) return;
                kq.TrangThai = kq.TonTai ? "TRUE" : "FALSE";
                GhiHangFile(i, kq);
                if (!kq.CanQuetSau) return;
                long size = await Task.Run(() => TinhKichThuocThuMuc(path));
                if (DaCu(thuTu)) return;
                kq.DungLuong = size > 0 ? FormatSize(size) : "0 KB";
                GhiHangFile(i, kq);
            }
            catch (Exception ex) { Debug.WriteLine("[Form33] tep phu " + i + ": " + ex.Message); }
        }
        private void GhiHangSQLite(int i, KetQuaFile kq)
        {
            if (_dtSQLite == null || i >= _dtSQLite.Rows.Count) return;
            _dtSQLite.Rows[i].ItemArray = new object[]
            {
                i + 1, _tenCSDL[i], kq.TonTai ? "TRUE" : "FALSE", kq.NgayTao, kq.DungLuong, kq.SucKhoe,
                kq.TocDo, kq.PhanManh, kq.Journal, kq.QuyenDoc, kq.QuyenGhi, kq.OTrong
            };
            ToMauMotHang(kryptonDataGridView1, i, true);
        }
        private void GhiHangFile(int i, KetQuaFileThuong kq)
        {
            if (_dtFilePhu == null || i >= _dtFilePhu.Rows.Count) return;
            _dtFilePhu.Rows[i].ItemArray = new object[]
            {
                i + 1, _tenFilePhu[i], kq.TrangThai, kq.NgayTao, kq.DungLuong, kq.QuyenDoc, kq.QuyenGhi, kq.ODia, kq.OTrong
            };
            ToMauMotHang(kryptonDataGridView2, i, false);
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
                if (_cacheKichThuocThuMuc.TryGetValue(path, out var cached)
                    && (DateTime.UtcNow - cached.Time).TotalMinutes < 10)
                {
                    kq.DungLuong = cached.Size > 0 ? FormatSize(cached.Size) : "0 KB";   // dùng lại kết quả cũ
                }
                else
                {
                    kq.DungLuong = DANG_TINH;     // quét thư mục có thể lâu -> làm ở giai đoạn 2, không chặn bảng
                    kq.CanQuetSau = true;
                }
                kq.OTrong = LayDungLuongOTrang(path);
                try
                {
                    string tempFile = Path.Combine(path, Path.GetRandomFileName());
                    using (new FileStream(tempFile, FileMode.CreateNew, FileAccess.ReadWrite,
                                          FileShare.None, 4096, FileOptions.DeleteOnClose))
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
        private long TinhKichThuocThuMuc(string path)
        {
            long size = GetDirectorySizeSafe(new DirectoryInfo(path));
            _cacheKichThuocThuMuc[path] = (DateTime.UtcNow, size);
            return size;
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
        private void TrangDiemChoEm_FormatDataGridView(DataGridView dgv) => GiaoDienForm33Web.DatCot(dgv);
        /// <summary>Tô màu 1 hàng theo mức độ; trả về mức (0 tốt, 1 cảnh báo, 2 lỗi). Hàng còn "Đang kiểm tra" để màu trung tính.</summary>
        private int ToMauMotHang(DataGridView dgv, int idx, bool laSQLite)
        {
            if (idx < 0 || idx >= dgv.Rows.Count) return 0;
            var row = dgv.Rows[idx];
            string L(string tenCot) => dgv.Columns.Contains(tenCot) ? (row.Cells[tenCot].Value?.ToString() ?? string.Empty) : string.Empty;
            string trangThai = L("Trạng thái");
            int muc;
            if (trangThai == CHO) muc = -1;
            else if (laSQLite)
            {
                string sucKhoe = L("Sức khỏe");
                string quyenGhi = L("Ghi");
                if (sucKhoe == DANG_KIEM_TRA) muc = trangThai.Contains("FALSE") ? 2 : -1;
                else if (trangThai.Contains("FALSE") || sucKhoe.Contains("Lỗi")) muc = 2;
                else if (quyenGhi != "OK" || sucKhoe.Contains("khóa")) muc = 1;
                else muc = 0;
            }
            else
            {
                string quyenGhi = L("Quyền ghi");
                if (trangThai.Contains("FALSE")) muc = 2;
                else if (trangThai.Contains("dự phòng")) muc = 1;
                else if (quyenGhi != "OK") muc = 1;
                else muc = 0;
            }
            var (nen, chu) = GiaoDienForm33Web.MauTheoMuc(muc);
            row.DefaultCellStyle.BackColor = nen;
            row.DefaultCellStyle.ForeColor = chu;
            return Math.Max(0, muc);
        }
        private int ToMauDataBound()
        {
            int mucDoNghiemTrong = 0;
            for (int i = 0; i < kryptonDataGridView1.Rows.Count; i++)
                mucDoNghiemTrong = Math.Max(mucDoNghiemTrong, ToMauMotHang(kryptonDataGridView1, i, true));
            for (int i = 0; i < kryptonDataGridView2.Rows.Count; i++)
                mucDoNghiemTrong = Math.Max(mucDoNghiemTrong, ToMauMotHang(kryptonDataGridView2, i, false));
            return mucDoNghiemTrong;
        }
        private string MoKetNoiChiDoc(string path) => $"Data Source={path};Mode=ReadOnly;Pooling=False;Default Timeout=10;";
        private static string GopSucKhoe(string? ok, bool biKhoa)
            => !string.Equals(ok, "ok", StringComparison.OrdinalIgnoreCase) ? "Lỗi hỏng CSDL"
             : biKhoa ? "Đang bị khóa (Locked)" : "Tốt";
        /// <summary>Giai đoạn 1 (chạy luồng nền, mili-giây): mọi thông tin KHÔNG cần quét toàn tệp.</summary>
        private KetQuaFile DocNhanhSQLite(string path)
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
                // Khóa nhận diện: đổi dung lượng / ngày sửa / dung lượng -wal thì mới quét lại
                long walLen = File.Exists(path + "-wal") ? new FileInfo(path + "-wal").Length : 0;
                kq.KhoaCache = $"{fi.Length}|{fi.LastWriteTimeUtc.Ticks}|{walLen}";
                var sw = Stopwatch.StartNew();
                // Chỉ đọc (ReadOnly) + Timeout ngắn để không khóa DB của người dùng
                using (var conn = new SqliteConnection(MoKetNoiChiDoc(path)))
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    kq.Journal = ExecuteScalarString(cmd, "PRAGMA journal_mode;")?.ToUpper() ?? "-";
                    long.TryParse(ExecuteScalarString(cmd, "PRAGMA page_count;"), out long pageCount);
                    long.TryParse(ExecuteScalarString(cmd, "PRAGMA freelist_count;"), out long freeListCount);
                    if (pageCount > 0)
                    {
                        double tiLePhanManh = Math.Round((double)freeListCount / pageCount * 100, 2);
                        kq.PhanManh = $"{tiLePhanManh}% ({freeListCount} pages rỗng)";
                    }
                    else
                    {
                        kq.PhanManh = "0%";
                    }
                }
                sw.Stop();
                // "Tốc độ phản hồi" = thời gian mở kết nối + truy vấn nhẹ (không còn cộng thời gian quét toàn tệp)
                kq.TocDo = sw.ElapsedMilliseconds + " ms";
                kq.BiKhoa = FileLock(path);
                if (_cacheToanVen.TryGetValue(path, out var cached) && cached.Key == kq.KhoaCache)
                {
                    kq.SucKhoe = GopSucKhoe(cached.Ok, kq.BiKhoa);   // tệp không đổi -> dùng kết quả cũ, tức thì
                }
                else
                {
                    kq.SucKhoe = DANG_KIEM_TRA;
                    kq.CanQuetSau = true;
                }
            }
            catch (Exception)
            {
                kq.SucKhoe = "Lỗi truy cập SQLite";
            }
            return kq;
        }
        /// <summary>Giai đoạn 2 (luồng nền): quick_check dừng ngay khi gặp lỗi đầu tiên; cache theo khóa nhận diện tệp.</summary>
        private string QuetToanVenSQLite(string path, string khoa, bool biKhoa)
        {
            try
            {
                using var conn = new SqliteConnection(MoKetNoiChiDoc(path));
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA quick_check(1);";
                string? ok = cmd.ExecuteScalar()?.ToString();
                if (ok != null) _cacheToanVen[path] = (khoa, ok);
                return GopSucKhoe(ok, biKhoa);
            }
            catch (Exception)
            {
                return "Lỗi truy cập SQLite";
            }
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
        private static long GetDirectorySizeSafe(DirectoryInfo directoryInfo)
        {
            long size = 0;
            var opt = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint   // tránh vòng lặp symlink
            };
            try
            {
                foreach (var fi in directoryInfo.EnumerateFiles("*", opt))
                    size += fi.Length;
            }
            catch { }
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
        private void KryptonDataGridView1_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
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
                lblTitle.StateCommon.ShortText.Font = _fontFormAoTitle12Bold;
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
                lblMoTa.StateCommon.Font = _fontFormAoMoTa105;
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
                grid.StateCommon.DataCell.Content.Font = _fontFormAoGrid10;
                grid.StateCommon.HeaderColumn.Content.Font = _fontFormAoGrid10Bold;
                grid.Columns.Add("Cot1", "Hạng mục kiểm tra");
                grid.Columns.Add("Cot2", "Giá trị / Trạng thái");
                grid.Columns[0].FillWeight = 50;
                grid.Columns[1].FillWeight = 50;
                grid.Columns[0].DefaultCellStyle.Font = _fontFormAoGrid10Bold;
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
                            cellVal.Style.Font = _fontFormAoGrid10Bold;
                        }
                        else if (valLow.Contains("khuyến nghị") || valLow.Contains("nguy cơ") || valLow.Contains("thủ công"))
                        {
                            cellVal.Style.ForeColor = System.Drawing.Color.FromArgb(211, 47, 47); // Đỏ đô
                            cellVal.Style.Font = _fontFormAoGrid10Bold;
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
                btnClose.StateCommon.Content.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 9.5F, System.Drawing.FontStyle.Bold);
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
        private void KryptonDataGridView1_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
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
        // 🚀 HÀM TỐI ƯU CSDL DUY NHẤT (SMART VACUUM - CHỐNG BÀO MÒN SSD & ANTI-CORRUPTION)
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
        // 🚀 SỰ KIỆN GỌI MENU DUY NHẤT
        private async void chayVacuum_ToolStripMenuItem_Click(object? sender, EventArgs e)
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
                var (ThanhCong, ThongBao) = await Task.Run(() => ToiUuHoaCSDLAsync(dbPath, dbName));
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
        private void lamMoi_ToolStripMenuItem_Click(object? sender, EventArgs e) => kryptonButton1_CapNhat.PerformClick();
        private async void kryptonButton1_CauhinhCSDL_Click(object? sender, EventArgs e)
        {
            // 1. LƯU LẠI GIAO DIỆN GỐC CỦA NÚT
            string textGoc = kryptonButton1_CauhinhCSDL.Values.Text;
            Image? imageGoc = kryptonButton1_CauhinhCSDL.Values.Image;
            try
            {
                // 2. KHÓA NÚT VÀ CẬP NHẬT TRẠNG THÁI "ĐANG XỬ LÝ"
                kryptonButton1_CauhinhCSDL.Enabled = false;
                kryptonButton1_CauhinhCSDL.Values.Text = "Đang xử lý...";
                kryptonButton1_CauhinhCSDL.Values.Image = null;
                // Nhường nhịp luồng UI một chút để Windows kịp vẽ lại tên nút
                await Task.Delay(50);
                // 3. THỰC THI TÁC VỤ MỞ FORM
                FormManager.OpenOrBringToFront<Form8_CauHinhCSDL>(this);
            }
            catch (Exception ex)
            {
                Form33_KiemTraSucKhoeCSDL.HienThiFormAo_Loi(
                    this,
                    "KHÔNG THỂ MỞ CẤU HÌNH CSDL",
                    ex.Message);
            }
            finally
            {
                // 4. KHÔI PHỤC LẠI NÚT NHƯ CŨ KHI MỞ FORM XONG HOẶC GẶP LỖI
                if (!IsDisposed && IsHandleCreated)
                {
                    kryptonButton1_CauhinhCSDL.Values.Text = textGoc;
                    kryptonButton1_CauhinhCSDL.Values.Image = imageGoc;
                    kryptonButton1_CauhinhCSDL.Enabled = true;
                }
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
                lblTitle.StateCommon.ShortText.Font = _fontFormAoTitle12Bold;
                lblTitle.StateCommon.ShortText.Color1 = Color.FromArgb(198, 40, 40);
                KryptonRichTextBox txt = new()
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    Text = noiDung
                };
                txt.StateCommon.Border.Draw = InheritBool.False;
                txt.StateCommon.Content.Font = _fontFormAoGrid10;
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
        private void ToolStripMenuItem_ChiTietCSDL_Click(object? sender, EventArgs e)
        {
            // Thêm sự kiện để có thể thay hành động đúp chuột vào DataGridView
            KryptonDataGridView1_CellDoubleClick(sender, new DataGridViewCellEventArgs(0, 0));
        }
        // Quản lý Font tập trung dùng biến hệ thống, chống rò rỉ GDI handle
        private static readonly Font _fontTitle115Bold = new Font(Module_HeThong.TenFontHeThong, 11.5F, FontStyle.Bold);
        private static readonly Font _fontGrid10Regular = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Regular);
        private static readonly Font _fontGrid10Bold = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Bold);
        private async void kryptonButton1_KiemTraTaiNguyenLoi_Click(object? sender, EventArgs e)
        {
            string textGoc = kryptonButton1_KiemTraTaiNguyenLoi.Values.Text;
            Image? imageGoc = kryptonButton1_KiemTraTaiNguyenLoi.Values.Image;
            try
            {
                kryptonButton1_KiemTraTaiNguyenLoi.Enabled = false;
                kryptonButton1_KiemTraTaiNguyenLoi.Values.Text = "Đang kiểm tra...";
                kryptonButton1_KiemTraTaiNguyenLoi.Values.Image = null;
                await Task.Delay(50);
                string ketQuaKiemTra = await Task.Run(() =>
                {
                    StringBuilder sb = new StringBuilder();
                    // Nhóm 1
                    sb.AppendLine("▶ I. Tài nguyên hệ thống (System)");
                    int stt = 1;
                    string thuMucCore = Path.Combine(Module_DanduongGPS.ThuMucCoSoDuLieu, "CongCuQuanLyCSDL", "CoreDatabaseRepository");
                    for (int i = 1; i <= 12; i++)
                    {
                        string tenFile = $"data{i}";
                        string duongDanFile = Path.Combine(thuMucCore, tenFile);
                        GhiNhanTrangThaiFile(sb, ref stt, duongDanFile, tenFile);
                    }
                    // Nhóm 2
                    sb.AppendLine("▶ II. Phần mềm dịch vụ (Services)");
                    stt = 1;
                    string thuMucGoc = AppDomain.CurrentDomain.BaseDirectory;
                    string[] danhSachExe = { "ServiceBackup.exe", "ServiceRestore.exe", "Uninstall_PhanMemThiDua2026.exe" };
                    foreach (string tenExe in danhSachExe)
                    {
                        string duongDanExe = Path.Combine(thuMucGoc, tenExe);
                        GhiNhanTrangThaiFile(sb, ref stt, duongDanExe, tenExe);
                    }
                    return sb.ToString();
                });
                kryptonButton1_KiemTraTaiNguyenLoi.Values.Text = "Kết quả kiểm tra";
                HienThiFormAo_KiemTraCSDL("BÁO CÁO TÀI NGUYÊN HỆ THỐNG", ketQuaKiemTra);
            }
            catch (Exception ex)
            {
                HienThiFormAo_KiemTraCSDL("LỖI KIỂM TRA", $"Có lỗi xảy ra:\n{ex.Message}");
            }
            finally
            {
                if (!IsDisposed && IsHandleCreated)
                {
                    kryptonButton1_KiemTraTaiNguyenLoi.Values.Text = textGoc;
                    kryptonButton1_KiemTraTaiNguyenLoi.Values.Image = imageGoc;
                    kryptonButton1_KiemTraTaiNguyenLoi.Enabled = true;
                }
            }
        }
        private void GhiNhanTrangThaiFile(StringBuilder sb, ref int stt, string duongDan, string tenHienThi)
        {
            if (File.Exists(duongDan))
            {
                long dungLuong = new FileInfo(duongDan).Length;
                // Phân tách 4 giá trị bằng dấu "|" (STT | Tên tài nguyên | Dung lượng | Trạng thái)
                sb.AppendLine($"{stt}|{tenHienThi}|{DinhDangDungLuong(dungLuong)}|Bình thường");
            }
            else
            {
                sb.AppendLine($"{stt}|{tenHienThi}|0 KB|Không tồn tại");
            }
            stt++;
        }
        private void HienThiFormAo_KiemTraCSDL(string tieuDe, string rawData)
        {
            // ⭐ SỬ DỤNG LỚP CƠ SỞ FORMAOBASE ĐỂ TẠO GIAO DIỆN NỔI AN TOÀN VÀ TỰ ĐỘNG DISPOSE
            using (var formAo = new FormAoBase())
            {
                formAo.Text = "Kết quả tra cứu";
                formAo.Size = new System.Drawing.Size(950, 620); // Nới rộng Form đảm bảo không bao giờ bị cắt chữ
                formAo.FormBorderStyle = FormBorderStyle.FixedDialog;
                formAo.MaximizeBox = false;
                formAo.MinimizeBox = false;
                formAo.ShowIcon = false;
                formAo.ShowInTaskbar = false;
                formAo.StartPosition = FormStartPosition.CenterParent;
                // --- 1. PANEL TIÊU ĐỀ (HEADER PANEL) ---
                var panelTop = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(25, 20, 20, 5) };
                panelTop.StateCommon.Color1 = System.Drawing.Color.White;
                var lblTitle = new Krypton.Toolkit.KryptonLabel { Text = tieuDe.ToUpper(), Dock = DockStyle.Fill, AutoSize = false };
                lblTitle.StateCommon.ShortText.Font = _fontTitle115Bold;
                lblTitle.StateCommon.ShortText.Color1 = System.Drawing.Color.FromArgb(0, 82, 155); // Xanh Navy chuẩn doanh nghiệp
                panelTop.Controls.Add(lblTitle);
                // --- 2. ĐƯỜNG KẺ NGANG (SEPARATOR) ---
                var separator = new Label { Height = 1, Dock = DockStyle.Top, BackColor = System.Drawing.Color.FromArgb(220, 220, 220), Margin = new Padding(0, 0, 0, 10) };
                // --- 3. PANEL CHỨA LƯỚI (BODY PANEL) ---
                var panelContent = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Fill, Padding = new Padding(25, 10, 25, 15) };
                panelContent.StateCommon.Color1 = System.Drawing.Color.White;
                // --- 4. KHỞI TẠO LƯỚI GRID HIỆN ĐẠI (KRYPTON DATAGRIDVIEW) ---
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
                    Cursor = Cursors.Hand
                };
                // Tinh chỉnh phong cách lưới
                grid.GridStyles.Style = Krypton.Toolkit.DataGridViewStyle.List;
                grid.StateCommon.Background.Color1 = System.Drawing.Color.White;
                grid.StateCommon.DataCell.Content.Font = _fontGrid10Regular;
                grid.StateCommon.HeaderColumn.Content.Font = _fontGrid10Bold;
                // ⭐ TỐI ƯU CHIỀU CAO: DÒNG TIÊU ĐỀ & DÒNG DỮ LIỆU
                // 1. Tăng chiều cao dòng TIÊU ĐỀ (Header) lên 45px để rộng rãi, rõ nét
                grid.ColumnHeadersHeight = 45;
                grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing; // Khóa cứng, không cho user kéo làm hỏng UI
                // 2. Thêm Padding cho Header để chữ không sát lề trên/dưới
                grid.StateCommon.HeaderColumn.Content.Padding = new Padding(3, 5, 3, 5);
                // 3. Tăng chiều cao dòng DỮ LIỆU (Row) lên 36px cho thoáng mắt
                grid.RowTemplate.Height = 36;
                // ⭐ CẤU HÌNH TỶ LỆ 04 CỘT (CHỐNG CẮT CHỮ TUYỆT ĐỐI)
                grid.Columns.Add("STT", "STT");
                grid.Columns.Add("TenTaiNguyen", "Tên tài nguyên");
                grid.Columns.Add("DungLuong", "Dung lượng");
                grid.Columns.Add("TrangThai", "Trạng thái");
                // Cột 0: STT (Khóa cứng 70px)
                grid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                grid.Columns[0].Width = 70;
                grid.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                grid.Columns[0].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                // Cột 1: Tên tài nguyên (Bật chế độ Fill lấp đầy khoảng trống)
                grid.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                grid.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                grid.Columns[1].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                // Cột 2: Dung lượng (Khóa cứng 150px)
                grid.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                grid.Columns[2].Width = 150;
                grid.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                grid.Columns[2].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                // Cột 3: Trạng thái (Khóa cứng 160px để hiển thị dư dả chữ "Không tồn tại")
                grid.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                grid.Columns[3].Width = 160;
                grid.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                grid.Columns[3].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                // --- 5. LỌC CHUỖI VÀ ĐỔ DỮ LIỆU VÀO LƯỚI ---
                string[] lines = rawData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    string cleanLine = line.Trim();
                    if (string.IsNullOrEmpty(cleanLine)) continue;
                    // Xử lý dòng tiêu đề phân nhóm ("▶ ...")
                    if (cleanLine.StartsWith("▶"))
                    {
                        int rHeader = grid.Rows.Add("", cleanLine.Replace("▶", "").Trim(), "", "");
                        grid.Rows[rHeader].DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(232, 244, 253); // Nền xanh nhạt
                        grid.Rows[rHeader].DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(0, 82, 155); // Chữ xanh Navy
                        grid.Rows[rHeader].Cells[1].Style.Font = _fontGrid10Bold; // In đậm tên Nhóm
                        continue;
                    }
                    // Cắt chuỗi theo dấu "|" 
                    string[] parts = cleanLine.Split('|');
                    if (parts.Length == 4)
                    {
                        string stt = parts[0].Trim();
                        string ten = parts[1].Trim();
                        string dungLuong = parts[2].Trim();
                        string trangThai = parts[3].Trim();
                        int rIdx = grid.Rows.Add(stt, ten, dungLuong, trangThai);
                        var cellSTT = grid.Rows[rIdx].Cells[0];
                        var cellDungLuong = grid.Rows[rIdx].Cells[2];
                        var cellTrangThai = grid.Rows[rIdx].Cells[3];
                        // ⭐ CONDITIONAL FORMATTING (TÔ MÀU NHẬN DIỆN THÔNG MINH)
                        string valLower = trangThai.ToLower();
                        if (valLower.Contains("bình thường") || valLower.Contains("ok") || valLower.Contains("có"))
                        {
                            cellTrangThai.Style.ForeColor = System.Drawing.Color.FromArgb(34, 139, 34); // Forest Green (Xanh lá)
                            cellTrangThai.Style.Font = _fontGrid10Bold;
                        }
                        else if (valLower.Contains("không tồn tại") || valLower.Contains("lỗi"))
                        {
                            // In Đậm Đỏ cả dòng (STT, Dung lượng, Trạng thái) khi có lỗi
                            Color redAlert = System.Drawing.Color.FromArgb(211, 47, 47);
                            cellTrangThai.Style.ForeColor = redAlert;
                            cellTrangThai.Style.Font = _fontGrid10Bold;
                            cellDungLuong.Style.ForeColor = redAlert;
                            cellSTT.Style.ForeColor = redAlert;
                        }
                    }
                    else
                    {
                        // Nếu dòng text tự do không chuẩn 4 cột, ép vào cột Tên
                        grid.Rows.Add("", cleanLine, "", "");
                    }
                }
                // --- 6. PANEL CHỨA NÚT ĐÓNG (FOOTER PANEL) ---
                var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 65, BackColor = System.Drawing.Color.WhiteSmoke };
                var btnClose = new Krypton.Toolkit.KryptonButton { Text = "Đóng", Width = 120, Height = 38, DialogResult = DialogResult.OK };
                btnClose.StateCommon.Content.ShortText.Font = _fontBtn95Bold;
                btnClose.StateCommon.Border.Rounding = 5;
                btnClose.Location = new System.Drawing.Point((formAo.Width - btnClose.Width) / 2, 13);
                panelBottom.Controls.Add(btnClose);
                // --- 7. RÁP LAYER VÀO FORM VÀ HIỂN THỊ ---
                panelContent.Controls.Add(grid);
                formAo.Controls.Add(panelContent);
                formAo.Controls.Add(separator);
                formAo.Controls.Add(panelTop);
                formAo.Controls.Add(panelBottom);
                separator.BringToFront();
                panelContent.BringToFront();
                formAo.AcceptButton = btnClose;
                formAo.CancelButton = btnClose;
                // Bỏ chọn dòng đầu tiên để UI gọn gàng
                formAo.Shown += (s, ev) => grid.ClearSelection();
                formAo.ShowDialog(this);
            }
        }
        private string DinhDangDungLuong(long soByte)
        {
            if (soByte < 1024)
                return $"{soByte} B";
            if (soByte < 1048576) // 1024 * 1024
                return $"{(soByte / 1024.0):F0} KB";
            return $"{(soByte / 1048576.0):F0} MB";
        }
    } //Mèo cam ----- Ngoài luồng
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
        // Điều phối quét 2 giai đoạn
        public bool CanQuetSau { get; set; }
        public bool BiKhoa { get; set; }
        public string KhoaCache { get; set; } = "";
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
        public bool CanQuetSau { get; set; }
    }
}