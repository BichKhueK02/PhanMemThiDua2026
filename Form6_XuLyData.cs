///Phiên bản 2
///
using DocumentFormat.OpenXml;
using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
namespace PhanMemThiDua2026
{
    public partial class Form6_XuLyData : Form
    {
        //Hệ thống production:
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        public bool DaLoadDuLieu { get; set; } = false;
        private DataTable? dtDanhSachGoc; // có thể null
        private Dictionary<string, ToolStripStatusLabel> labelsPhanLoai = new();
        //private bool _dangXuLyHuongDan = false;
        private int _dangRefresh = 0;
        private CancellationTokenSource? _refreshCts;
        private static readonly Dictionary<string, string> _tenCotTiengViet = new(StringComparer.Ordinal)
        {
            ["STT"] = "STT",
            ["HoVaTen"] = "Họ và tên",
            ["SoHieu"] = "Số hiệu",
            ["NamSinh"] = "Năm sinh",
            ["QueQuan"] = "Quê quán",
            ["NgayVaoCAND"] = "Vào CAND",
            ["CapBac"] = "Cấp bậc",
            ["ChucVu"] = "Chức vụ",
            ["DonVi"] = "Đơn vị",
            ["PhanLoai"] = "Phân loại",
            ["GhiChu"] = "Ghi chú"
        };
        // Khai báo biến toàn cục ở đầu Class (bạn có thể đã thêm rồi)
        private readonly Image _iconStar = Properties.Resources.ic_star;
        public static Form6_XuLyData? Instance { get; private set; }
        private System.Windows.Forms.Timer? timerLocDuLieu;
        private const string PLACEHOLDER_TIMKIEM = Module_HeThong.Nhap_Tim_Kiem;
        private bool _dangSetPlaceholder = false;
        private bool _isUpdatingCombo = false; // Cờ chặn sự kiện khi đang nạp dữ liệu cho ComboBox
        private const int EM_SETCUEBANNER = 0x1501;
        private bool _isInit = false;
        private const string COT_HO_TEN = "HoVaTen";
        private const string COT_PHAN_LOAI = "PhanLoai";
        private readonly Action _actionLamMoi;
        private readonly Action _actionLuuTinhToan;
        private readonly Action _actionXuatExcel;
        //private const string CHUOI_KHONG_PL = "Không PL";
        private Form30_ChinhSuaDataTanBinh? _frm30ViewInstance = null;
        private Form22_ChinhSuaDataCBCS? _frm22ViewInstance = null;
        // 🟢 1. CỜ CHỐNG SPAM CLICK (Chặn mở form lặp lại)
        private bool _isOpeningProfile = false;
        // 🟢 2. CACHE PHIÊN BẢN PHẦN MỀM VÀO RAM (Tránh đọc ổ cứng liên tục)
        private bool? _isTanBinhCached = null;
        private const int WM_SETREDRAW = 11;
        private bool _daKhoiTaoToolTip = false;
        private System.Windows.Forms.ToolTip _toolTipMain = null!;
        private int _yeuCauNhapExcel;
        // Hàm SendMessage bạn đã khai báo sẵn ở trên rồi, ta sẽ tận dụng lại
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string? lParam);
        // 1. KHAI BÁO HẰNG SỐ CHUẨN KỸ SƯ (Quản lý tập trung)
        private const bool GIAI_MA_SONG_SONG = false;
        public Form6_XuLyData()
        {
            InitializeComponent();
            Module_GiaoDienForm6Web.ApDung(this);                  // giao diện kiểu web (module GiaoDienForm6Web.cs) - chỉ trình bày
            BatDoubleBufferDeQuy(this);                    // giảm chớp khi layout/resize các khối lồng nhau
            kryptonDataGridView1.EnableDoubleBuffered(true);   // bật sớm (trước đây chỉ bật trong Load)
            this.KeyPreview = true; // THÊM DÒNG NÀY
            // 2. KHỞI TẠO BIẾN / TIMER
            timerLocDuLieu = new System.Windows.Forms.Timer
            {
                Interval = 200 // Delay 0.2s sau khi ngừng gõ (lọc giờ chỉ mất vài ms nên có thể phản hồi nhanh hơn)
            };
            // 3. ĐĂNG KÝ SỰ KIỆN (EVENTS)
            // Form events
            this.Shown += Form6_Show;
            this.VisibleChanged += Form6_XuLyData_VisibleChanged;
            timerLocDuLieu.Tick += TimerLocDuLieu_Tick;
            kryptonDataGridView1.RowPostPaint += kryptonDataGridView1_RowPostPaint;
            kryptonDataGridView1.CellPainting += KryptonDataGridView1_CellPainting;
            // 4. CÁC HÀM KHỞI TẠO BỔ SUNG
            InitToolTips();
            DangKyHieuUngVienTextBox();
            DangKyBaoVeDuLieuKhiTimKiem(); // 👉 THÊM DÒNG NÀY VÀO ĐÂY
            _actionLamMoi = SafeAction(() => kryptonButton_RefershCSDL.PerformClick());
            _actionLuuTinhToan = SafeAction(() => kryptonButton_LuuDataCapNhat.PerformClick());
            _actionXuatExcel = SafeAction(() => xuatDuLieuThiDuaRaTepExcel_ToolStripMenuItem.PerformClick());
        }
        private void TimerLocDuLieu_Tick(object? sender, EventArgs e)
        {
            timerLocDuLieu?.Stop();
            ApplyFilter(); // Chạy bộ lọc
        }
        private bool _daHienLanDau = false;
        private Task? _tacVuNapDau;
        private void Form6_Load(object sender, EventArgs e)
        {
            if (_daHienLanDau) return;
            _daHienLanDau = true;
            KhoiTaoMotLan();   // đã nạp sẵn lúc form còn ẩn (Form2) thì bỏ qua -> lưới có dữ liệu ngay, không nạp lại
            // XỬ LÝ UI SAU KHI FORM ĐÃ HIỂN THỊ
            BeginInvoke(new Action(SauKhiHienLanDau));
        }
        /// <summary>
        /// Form2 gọi khi form còn ẩn (sau đăng nhập, lúc người dùng rảnh): khởi tạo + nạp dữ liệu sẵn,
        /// để lần bấm mở trang Phân loại đầu tiên hiện ngay dữ liệu. Gọi nhiều lần trả về cùng một tác vụ nạp lần đầu.
        /// Phải gọi trên luồng giao diện.
        /// </summary>
        internal Task NapTruocDuLieuAsync()
        {
            if (IsDisposed || !IsHandleCreated) return Task.CompletedTask;
            KhoiTaoMotLan();
            return _tacVuNapDau ?? Task.CompletedTask;
        }
        private async Task NapDauAsync()
        {
            try { await ReloadDuLieu(); }
            catch (Exception ex) { Debug.WriteLine("[Form6.NapDau] " + ex.Message); }
            DaLoadDuLieu = true;
        }
        private void KhoiTaoMotLan()
        {
            if (_isInit) return;
            _isInit = true;
            // 1. LIÊN KẾT CONTROL CƠ BẢN
            kryptonDataGridView1.ContextMenuStrip = contextMenuStrip1;
            KhoiTaoXoaNhieu();
            CapNhatLabelTheoPhienBan();
            // 2. TRẠNG THÁI CONTROL
            textBox_STT.ReadOnly = true;
            textBox_SoHieu.ReadOnly = true;
            // 3. KHỞI TẠO LABEL PHÂN LOẠI
            var cauHinhLabels = new Dictionary<string, (ToolStripStatusLabel Label, System.Drawing.Color ForeColor)>
                    {
                        { Module_HeThong.Loai_1, (toolStripStatusLabel2_Loai1, System.Drawing.Color.FromArgb(40, 167, 69)) },
                        { Module_HeThong.Loai_2, (toolStripStatusLabel2_Loai2, System.Drawing.Color.FromArgb(0, 122, 204)) },
                        { Module_HeThong.Loai_3, (toolStripStatusLabel2_Loai3, System.Drawing.Color.FromArgb(220, 53, 69)) },
                        { Module_HeThong.Loai_4, (toolStripStatusLabel2_Loai4, System.Drawing.Color.FromArgb(220, 53, 69)) },
                        { Module_HeThong.PL_KHONG_PL, (toolStripStatusLabel2_KhongPL, System.Drawing.Color.FromArgb(128, 0, 128)) }
                    };
            foreach (var item in cauHinhLabels)
            {
                var lbl = item.Value.Label;
                labelsPhanLoai[item.Key] = lbl;
                lbl.ForeColor = item.Value.ForeColor;
                lbl.Spring = false;
                lbl.AutoSize = true;
                lbl.TextAlign = ContentAlignment.MiddleCenter;
            }
            // 4. KHỞI TẠO DATAGRIDVIEW
            DinhDangDataGridFrom6();
            DinhDangDataGridViewHienDai();
            kryptonDataGridView1.RowHeadersVisible = true;
            kryptonDataGridView1.RowHeadersWidth = 55;
            kryptonDataGridView1.AllowUserToAddRows = false;
            kryptonDataGridView1.EnableDoubleBuffered(true);
            // 5. KHỞI TẠO BỘ LỌC / TÌM KIẾM
            InitFilters();
            SetCueBanner(textBox_TimKiemTheoTen, PLACEHOLDER_TIMKIEM);
            // 6. KHỞI TẠO DANH MỤC TĨNH
            KhoiTaoDanhSachCapBac();
            NapDanhSachDonVi();
            // 7. ĐĂNG KÝ EVENT
            kryptonDataGridView1.Resize += kryptonDataGridView1_Resize;
            Module_HeThong.SuKienThayDoiCheDoXetThiDua += OnCheDoThayDoi;
            Module_HeThong.SuKienThayDoiThoiGianHeThong += OnCheDoThayDoi;
            textBox_STT.TextChanged += (s, ev) => KiemTraVaCapNhatThongTin();
            textBox_HoVaTen.TextChanged += (s, ev) => KiemTraVaCapNhatThongTin();
            // 8. BẮT ĐẦU NẠP DỮ LIỆU CHÍNH (1 lần duy nhất; lần mở trang chỉ chờ tác vụ này chứ không nạp thêm)
            _tacVuNapDau = NapDauAsync();
        }
        private void SauKhiHienLanDau()
        {
            {
                if (IsDisposed || !IsHandleCreated) return;
                ClearThongTin();
                if (textBox_TimKiemTheoTen != null && textBox_TimKiemTheoTen.Visible)
                {
                    textBox_TimKiemTheoTen.Focus();
                    textBox_TimKiemTheoTen.Select();
                }
                else
                {
                    kryptonDataGridView1.Focus();
                }
                // (Không chọn sẵn dòng 0: bản cũ chạy lúc dữ liệu chưa nạp xong nên thực tế chưa bao giờ chọn dòng nào;
                //  nay dữ liệu đã nạp sẵn, chọn dòng 0 sẽ tự đổ thông tin vào các ô nhập -> giữ đúng trạng thái trống như cũ.)
            }
        }
        // ⭐ [MỚI] Bộ đệm AES giúp tăng tốc Virtual Mode
        private void Form6_Show(object? sender, EventArgs e)
        {
            Module_ThoatAnToan.KichHoatESC(this);
            // Gọi hàm khởi tạo ProgressBar
            KhoiTaoProgressBarLamMoi();
        }
        private void KhoiTaoDanhSachCapBac()
        {
            // Kiểm tra nếu chưa có mục rỗng ở đầu thì chèn thêm vào vị trí Index 0
            if (textBox_CapBac.Items.Count > 0 && !string.IsNullOrEmpty(textBox_CapBac.Items[0]?.ToString()))
            {
                textBox_CapBac.Items.Insert(0, "");
            }
        }
        private void KiemTraVaCapNhatThongTin()
        {
            bool sttRong = string.IsNullOrWhiteSpace(textBox_STT.Text);
            bool hoTenRong = string.IsNullOrWhiteSpace(textBox_HoVaTen.Text);
            // Nếu STT hoặc Họ và Tên trống -> Tự động đưa Đơn vị và Cấp bậc về giá trị rỗng
            if (sttRong || hoTenRong)
            {
                if (textBox_CapBac.Text != "")
                    textBox_CapBac.SelectedIndex = 0; // Chọn mục rỗng ở đầu danh sách
                if (textBox_DonVi.Text != "")
                    textBox_DonVi.SelectedIndex = 0;
            }
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            try
            {
                if (Module_PhimTat.XuLy(keyData, _actionLamMoi, _actionLuuTinhToan, _actionXuatExcel)) return true;
                Keys key = keyData & Keys.KeyCode;
                Keys modifier = keyData & Keys.Modifiers;
                if (modifier == Keys.None)
                {
                    return key switch
                    {
                        Keys.F7 => SafeExecute(kiemTraKetNoi, () => kiemTraKetNoi.PerformClick()),
                        Keys.F8 => SafeExecute(kryptonButton_XoaKetQuaPhanLoai, () => kryptonButton_XoaKetQuaPhanLoai.PerformClick()),
                        Keys.F9 => SafeExecute(kryptonButton1_ThiDuaBaNhat, () => kryptonButton1_ThiDuaBaNhat.PerformClick()),
                        Keys.F10 => SafeExecute(phanTichDuLieuTrungTen_ToolStripMenuItem, () => phanTichDuLieuTrungTen_ToolStripMenuItem.PerformClick()),
                        Keys.F11 => SafeExecute(cBCSTrongDienQuanLy_ToolStripMenuItem, () => cBCSTrongDienQuanLy_ToolStripMenuItem.PerformClick()),
                        Keys.F12 => SafeExecute(nhapKetQuaPhanLoaiTuDonVi_ToolStripMenuItem, () => nhapKetQuaPhanLoaiTuDonVi_ToolStripMenuItem.PerformClick()),
                        _ => false
                    };
                }
                if (modifier == Keys.Control && XuLyPhimTatGanPhanLoai(key)) return true;
                if (modifier == Keys.Control)
                {
                    return key switch
                    {
                        Keys.O => SafeExecute(themDuLieuTuFileExce_ToolStripMenuItem, () => themDuLieuTuFileExce_ToolStripMenuItem.PerformClick()),
                        Keys.T => SafeExecute(xuatDuLieuSangThongKe_ToolStripMenuItem, () => xuatDuLieuSangThongKe_ToolStripMenuItem.PerformClick()),
                        _ => false
                    };
                }
                if (modifier == (Keys.Control | Keys.Shift) && key == Keys.Delete)
                    return SafeExecute(xoaToanBoDuLieuDSCBCS_ToolStripMenuItem, () => xoaToanBoDuLieuDSCBCS_ToolStripMenuItem.PerformClick());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi phím tắt toàn cục: {ex}");
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        private bool _isCheDoNam = false;
        // ---- BỘ LỌC MỚI: không dùng DataView.RowFilter nữa ----
        // _boLoc   : chỉ mục chuỗi đã chuẩn hóa sẵn (dựng ở luồng nền lúc nạp dữ liệu)
        // _hienThi : chỉ số dòng của dtDanhSachGoc đang hiển thị trên lưới (tăng dần); null = hiển thị mọi dòng
        private BoLocDanhSach? _boLoc;
        private int[]? _hienThi;
        private int SoDongHienThi => _hienThi?.Length ?? dtDanhSachGoc?.Rows.Count ?? 0;
        // Dòng dữ liệu ứng với vị trí viTri trên lưới (null nếu ngoài phạm vi). Thay cho DefaultView[viTri].
        private DataRow? LayDongHienThi(int viTri)
        {
            var dt = dtDanhSachGoc;
            if (dt == null || viTri < 0) return null;
            var hs = _hienThi;
            int nguon;
            if (hs == null) nguon = viTri;
            else
            {
                if (viTri >= hs.Length) return null;
                nguon = hs[viTri];
            }
            return (uint)nguon < (uint)dt.Rows.Count ? dt.Rows[nguon] : null;
        }
        // Đặt số dòng cho lưới ảo. RowCount giảm từng dòng rất chậm (mỗi lần xóa 1 dòng lưới tính lại bố cục),
        // nên khi thay đổi lớn: về 0 (xóa 1 phát) rồi đặt lại đúng số cần. Tắt tự đo chiều cao dòng (đo cả nghìn dòng = đứng máy).
        private void DatSoDongLuoi(int soDong)
        {
            var g = kryptonDataGridView1;
            if (g == null || g.IsDisposed || g.RowCount == soDong) return;
            var cheDoHang = g.AutoSizeRowsMode;
            bool tat = cheDoHang != DataGridViewAutoSizeRowsMode.None;
            try
            {
                if (tat) g.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
                if (Math.Abs(g.RowCount - soDong) > 64)
                {
                    g.CurrentCell = null;
                    g.RowCount = 0;
                }
                g.RowCount = soDong;
            }
            finally
            {
                // Chế độ đo "mọi ô" ở lưới ảo là thảm họa hiệu năng -> giữ tắt; chế độ chỉ đo dòng đang hiện thì trả lại
                if (tat && cheDoHang != DataGridViewAutoSizeRowsMode.AllCells && cheDoHang != DataGridViewAutoSizeRowsMode.AllHeaders)
                    g.AutoSizeRowsMode = cheDoHang;
            }
        }
        // 🟢 [BỔ SUNG HÀM NÀY VÀO FORM6]: Hàm xử lý khi nhận sự kiện thay đổi từ Form4
        private async void OnCheDoThayDoi()
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;
            // Ép buộc xóa Cache cũ để ReloadDuLieu bắt buộc đọc từ CSDL mới
            // (Nếu DataCache có hàm Clear, hãy gọi: DataCache.Clear();)
            // Hoặc đặt lại cờ loaded:
            // DataCache.IsLoaded = false; 
            // Thực hiện nạp lại dữ liệu trên luồng UI an toàn
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(async () => await ReloadDuLieu()));
            }
            else
            {
                await ReloadDuLieu();
            }
        }
        // Hủy đăng ký sự kiện khi đóng Form để tránh leak bộ nhớ
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _aesCache = new System.Collections.Concurrent.ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        // -------------------------------------------------------------------------------------
        // [A] THÊM FIELD (đặt cạnh các field khác ở đầu class, ví dụ sau dòng _refreshCts)
        // -------------------------------------------------------------------------------------
        private readonly SemaphoreSlim _reloadGate = new SemaphoreSlim(1, 1);
        // ---- Tạm dừng vẽ lưới có ĐẾM LỒNG NHAU ----
        // Trước đây ReloadDuLieuNoiBo tắt vẽ rồi ApplyFilter (gọi bên trong) lại bật vẽ ở giữa chừng
        // -> lưới vẽ nửa vời ngay khi còn đang dựng cột. Bộ đếm đảm bảo chỉ bật lại khi lớp ngoài cùng xong.
        private int _veTamDung;
        private void TamDungVeLuoi()
        {
            var g = kryptonDataGridView1;
            if (g == null || g.IsDisposed) return;
            if (_veTamDung++ == 0 && g.IsHandleCreated) SendMessage(g.Handle, WM_SETREDRAW, 0, null);
        }
        private void MoLaiVeLuoi()
        {
            if (_veTamDung <= 0) return;
            if (--_veTamDung != 0) return;
            var g = kryptonDataGridView1;
            if (g == null || g.IsDisposed || !g.IsHandleCreated) return;
            SendMessage(g.Handle, WM_SETREDRAW, 1, null);
            g.Invalidate();
        }
        // Bộ nhớ đệm giải mã tăng dần theo thời gian (10 năm dùng) -> chặn trần để không phình RAM
        private const int TRAN_CACHE_AES = 150_000;
        // Nạp nóng cache giải mã cho các cột KHÔNG giải mã sẵn (số hiệu, quê quán, chức vụ...) ở luồng nền,
        // để lúc cuộn lưới luồng giao diện chỉ tra cache, không phải chạy AES.
        private void NapNongCacheGiaiMaNen()
        {
            var dt = dtDanhSachGoc;
            var ord = _mapOrdinalCot;
            var daGm = _cotDaGiaiMa;
            if (dt == null || ord == null || daGm == null) return;
            var chuoi = new HashSet<string>(StringComparer.Ordinal);
            int n = dt.Rows.Count;
            for (int ci = 0; ci < ord.Length && ci < daGm.Length; ci++)
            {
                if (daGm[ci] || ord[ci] < 0) continue;
                int o = ord[ci];
                for (int i = 0; i < n; i++)
                {
                    string s = dt.Rows[i][o]?.ToString() ?? "";
                    if (s.Length > 0 && !_aesCache.ContainsKey(s)) chuoi.Add(s);
                }
            }
            if (chuoi.Count == 0) return;
            var ds = new List<string>(chuoi);
            _ = Task.Run(() =>
            {
                try
                {
                    Parallel.ForEach(ds, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) },
                        s => GiaiMaSafeCoCache(s));
                }
                catch (Exception ex) { Debug.WriteLine("NapNongCache: " + ex.Message); }
            });
        }
        private static string GiaiMaSafeCoCache(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            return _aesCache.GetOrAdd(input, key =>
            {
                try
                {
                    // Gọi Module V2
                    string result = Module_BaoMatAES.GiaiMa(key);
                    // Trả về chuỗi gốc (key) nếu giải mã ra rỗng. 
                    // Điều này cứu sống các cột đang lưu dạng plain-text (chữ thường)
                    return string.IsNullOrWhiteSpace(result) ? key : result;
                }
                catch
                {
                    // Nếu lỗi (do không phải chuỗi mã hóa) -> Trả về nguyên văn bản gốc
                    return key;
                }
            });
        }
        // HÀM BẢO VỆ DELEGATE (TRÁNH LỖI CRASH KHI GỌI UI)
        private Action SafeAction(Action action)
        {
            return () =>
            {
                if (IsDisposed || Disposing)
                    return;
                if (action == null)
                    return;
                try
                {
                    action.Invoke();
                }
                catch (ObjectDisposedException)
                {
                    // Form/control đã bị hủy
                }
                catch (InvalidOperationException ex)
                {
                    Debug.WriteLine(
                        "Lỗi trạng thái UI: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "Lỗi thực thi phím tắt: " + ex);
                }
            };
        }
        private bool SafeExecute(Component component, Action action)
        {
            if (component == null) return false;
            if (component is ToolStripItem menuItem)
            {
                if (!menuItem.Available || !menuItem.Enabled) return false;
            }
            if (component is Control control)
            {
                if (control.IsDisposed || !control.Visible || !control.Enabled) return false;
            }
            try
            {
                action?.Invoke();
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine("Lỗi trạng thái UI: " + ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi thực thi phím tắt: " + ex);
                return false;
            }
        }
        private bool KiemTraCheDoXetThiDuaNam()
        {
            try
            {
                // Tân binh không có xét thi đua năm: cờ "Năm" còn lưu từ lúc dùng CBCS không được áp sang
                if (Module_TaiKhoan.LaPhienBanTanBinh()) return false;
                if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path))
                    return false;
                using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl2Path));
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT ChoPhepCheDoXetThiDuaNam FROM CheDo_XetThiDuaNam WHERE ID = 1 LIMIT 1";
                object? result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    string cheDo = result.ToString()!.Trim();
                    return string.Equals(cheDo, "Năm", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi đọc CheDo_XetThiDuaNam: " + ex.Message);
            }
            return false;
        }
        // HIỆU ỨNG UI: VIỀN XANH ĐẬM KHI HOVER VÀ FOCUS
        private void KiemTraDuLieu()
        {
            bool coDuLieu =
                dtDanhSachGoc?
                .AsEnumerable()
                .Any(r =>
                    !string.IsNullOrWhiteSpace(
                        r.Field<string>("HoVaTen")))
                == true;
            CapNhatMenuDuLieu(coDuLieu);
        }
        private void CapNhatMenuDuLieu(bool visible)
        {
            cBCSTrongDienQuanLy_ToolStripMenuItem.Visible = visible;
            phanTichDuLieuTrungTen_ToolStripMenuItem.Visible = visible;
            xoaToanBoDuLieuDSCBCS_ToolStripMenuItem.Visible = visible;
            nhapKetQuaPhanLoaiTuDonVi_ToolStripMenuItem.Visible = visible;
            xuatDuLieuSangThongKe_ToolStripMenuItem.Visible = visible;
            contextMenuStrip1?.Invalidate();
            contextMenuStrip1?.Update();
            contextMenuStrip1?.Refresh();
        }
        private void DangKyHieuUngVienTextBox()
        {
            var danhSachControl = new List<System.Windows.Forms.Control>
        {
            textBox_TimKiemTheoTen,
            textBox_STT,
            textBox_HoVaTen,
            textBox_SoHieu,
            textBox_NamSinh,
            textBox_QueQuan,
            textBox_NgayVaoCAND,
            textBox_CapBac,
            comboBox_ChucVu,
            textBox_DonVi,
            comboBox_PhanLoai,
            textBox_GhiChu
        };
            // Khai báo tường minh để tránh lỗi với thư viện Excel (OpenXml)
            System.Drawing.Color mauFocus = System.Drawing.Color.FromArgb(0, 120, 215);     // Xanh dương đậm
            System.Drawing.Color mauHover = System.Drawing.Color.FromArgb(100, 180, 255);   // Xanh nhạt
            System.Drawing.Color mauMacDinh = System.Drawing.Color.FromArgb(180, 180, 180); // Xám chuẩn
            foreach (var control in danhSachControl)
            {
                if (control == null) continue;
                // 1. DÀNH CHO KRYPTON TEXTBOX
                if (control is Krypton.Toolkit.KryptonTextBox kTextbox)
                {
                    kTextbox.StateCommon.Border.Rounding = 3;
                    kTextbox.StateCommon.Border.Width = 1;
                    kTextbox.StateCommon.Border.Color1 = mauMacDinh;
                    kTextbox.MouseEnter += (s, e) => { if (!kTextbox.Focused) { kTextbox.StateCommon.Border.Color1 = mauHover; kTextbox.StateCommon.Border.Width = 1; } };
                    kTextbox.MouseLeave += (s, e) => { if (!kTextbox.Focused) { kTextbox.StateCommon.Border.Color1 = mauMacDinh; kTextbox.StateCommon.Border.Width = 1; } };
                    kTextbox.Enter += (s, e) => { kTextbox.StateCommon.Border.Color1 = mauFocus; kTextbox.StateCommon.Border.Width = 1; };
                    kTextbox.Leave += (s, e) => { kTextbox.StateCommon.Border.Color1 = mauMacDinh; kTextbox.StateCommon.Border.Width = 1; };
                }
                // 2. DÀNH CHO KRYPTON COMBOBOX (Bổ sung để không bị lọt lưới)
                else if (control is Krypton.Toolkit.KryptonComboBox kCombobox)
                {
                    kCombobox.StateCommon.ComboBox.Border.Rounding = 3;
                    kCombobox.StateCommon.ComboBox.Border.Width = 1;
                    kCombobox.StateCommon.ComboBox.Border.Color1 = mauMacDinh;
                    kCombobox.MouseEnter += (s, e) => { if (!kCombobox.Focused) { kCombobox.StateCommon.ComboBox.Border.Color1 = mauHover; kCombobox.StateCommon.ComboBox.Border.Width = 1; } };
                    kCombobox.MouseLeave += (s, e) => { if (!kCombobox.Focused) { kCombobox.StateCommon.ComboBox.Border.Color1 = mauMacDinh; kCombobox.StateCommon.ComboBox.Border.Width = 1; } };
                    kCombobox.Enter += (s, e) => { kCombobox.StateCommon.ComboBox.Border.Color1 = mauFocus; kCombobox.StateCommon.ComboBox.Border.Width = 1; };
                    kCombobox.Leave += (s, e) => { kCombobox.StateCommon.ComboBox.Border.Color1 = mauMacDinh; kCombobox.StateCommon.ComboBox.Border.Width = 1; };
                }
                // 3. DÀNH CHO COMBOBOX / TEXTBOX MẶC ĐỊNH CỦA WINDOWS FORMS
                else if (control is System.Windows.Forms.TextBox || control is System.Windows.Forms.ComboBox)
                {
                    System.Drawing.Color nenFocus = System.Drawing.Color.FromArgb(240, 248, 255);
                    System.Drawing.Color nenMacDinh = System.Drawing.Color.White;
                    control.Enter += (s, e) => control.BackColor = nenFocus;
                    control.Leave += (s, e) => control.BackColor = nenMacDinh;
                }
            }
        }
        private volatile bool _isDirtyFilter = false;
        private void Form6_XuLyData_VisibleChanged(object? sender, EventArgs e)
        {
            if (!Visible || !_isInit || IsDisposed || Disposing || !_isDirtyFilter) return;
            _isDirtyFilter = false;
            try
            {
                ApplyFilter();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine("Lỗi ApplyFilter: " + ex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("VisibleChanged Error: " + ex);
            }
        }
        private void SetCueBanner(Control control, string placeholder)
        {
            // Kiểm tra an toàn, tránh crash nếu control chưa được vẽ lên giao diện
            if (control == null || !control.IsHandleCreated) return;
            // Trường hợp 1: Nếu nó là TextBox mặc định của Windows Forms
            if (control is System.Windows.Forms.TextBox txtBox)
            {
                SendMessage(txtBox.Handle, EM_SETCUEBANNER, 0, placeholder);
            }
            // Trường hợp 2: Nếu nó là KryptonTextBox
            else if (control is Krypton.Toolkit.KryptonTextBox kTxtBox)
            {
                // KHÔNG dùng CueHint của Krypton: ở ô hẹp, Krypton vẽ chữ mờ xuống 2 dòng, bị cắt nửa và để lại
                // vệt kẻ xám trong ô (trông như viền TextBox mặc định của Windows chưa ẩn).
                // Dùng chữ mờ gốc của Windows trên TextBox con bên trong: luôn 1 dòng, tự cắt gọn.
                // wParam = 1: vẫn hiện chữ mờ khi ô đang được chọn (form mở ra là ô này nhận focus ngay).
                kTxtBox.CueHint.CueHintText = string.Empty;
                var oNhapTrong = kTxtBox.TextBox;
                if (oNhapTrong == null) return;
                if (oNhapTrong.IsHandleCreated)
                    SendMessage(oNhapTrong.Handle, EM_SETCUEBANNER, 1, placeholder);
                oNhapTrong.HandleCreated += (_, __) => SendMessage(oNhapTrong.Handle, EM_SETCUEBANNER, 1, placeholder);
            }
        }
        // 🌟 TỐI ƯU BỘ NHỚ: Khai báo ở cấp Class để tái sử dụng, chống GDI Leak, yêu mèo cam Trung Kiên
        private void InitToolTips()
        {
            // Chống gọi lặp gây lãng phí chu kỳ CPU
            if (_daKhoiTaoToolTip) return;
            try
            {
                // Khởi tạo ToolTip duy nhất 1 lần trong suốt vòng đời của Form
                if (_toolTipMain == null)
                {
                    _toolTipMain = new System.Windows.Forms.ToolTip
                    {
                        ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac,
                        ToolTipIcon = ToolTipIcon.Info,
                        // 🔧 FIX: Balloon + hiệu ứng animation là nguyên nhân chính
                        // khiến click đầu tiên bị "nuốt" để đóng tooltip thay vì bắn Click.
                        IsBalloon = false,
                        UseAnimation = false,
                        UseFading = false,
                        InitialDelay = 500,   // tăng delay để tooltip không tranh chấp với thao tác click nhanh
                        AutoPopDelay = 5000,
                        ReshowDelay = 100,
                        // 🔧 FIX: false để tooltip chỉ hiện khi form đang active,
                        // tránh việc nó bật lên "canh" đúng lúc user rê chuột tới bấm.
                        ShowAlways = false
                    };
                }
                (System.Windows.Forms.Control control, string noiDung)[] danhSachToolTip = new (System.Windows.Forms.Control, string)[]
                {
                    (textBox_TimKiemTheoTen,             "Nhập họ và tên CBCS cần tìm (có thể nhập một phần)"),
                    (comboBox_TimKiemDonVi,              "Lọc danh sách theo đơn vị công tác"),
                    (comboBox_XepLoaiThiDua,             "Lọc theo kết quả xếp loại thi đua"),
                    (kryptonButton_LamMoiCacOTimKiem,    "Xóa toàn bộ điều kiện tìm kiếm (Ctrl + D)"),
                    (kryptonButton_RefershCSDL,          "Nạp lại dữ liệu từ CSDL gốc (F5)"),
                    (kryptonButton_XoaKetQuaPhanLoai,    "Xóa kết quả phân loại (F8)"),
                    (kryptonButton_XoaDataCBCS,          "Xóa dữ liệu CBCS (cần xác nhận)"),
                    (kryptonButton1_HuongDanThemDuLieu,  KiemTraPhienBanTanBinh()
                        ? $"Thêm {Module_TaiKhoan.TenTB_Thuong} vào phần mềm (nhập tay hoặc từ tệp Excel)"
                        : "Thêm CBCS vào phần mềm (nhập tay hoặc từ tệp Excel)"),
                    (kryptonButton1_ThiDuaBaNhat,        "Quản lý phong trào thi đua Ba Nhất"),
                    (kryptonButton_LamMoiCacOTimKiem,    "Làm mới bộ lọc"),
                    (kryptonButton_LuuDataCapNhat,       "Lưu toàn bộ dữ liệu đã chỉnh sửa (Ctrl + S)"),
                    (comboBox_PhanLoai,                  "Chọn nhiều dòng (Ctrl/Shift + chuột) rồi chọn phân loại ở đây để lưu ngay cho cả nhóm.\nPhím tắt trên lưới: Ctrl+1..Ctrl+5, Ctrl+0 = để trống")
                };
                int soLoi = 0;
                foreach (var (control, noiDung) in danhSachToolTip)
                {
                    if (control == null)
                    {
                        soLoi++;
                        continue;
                    }
                    try
                    {
                        if (control.IsDisposed) continue;
                        _toolTipMain.SetToolTip(control, noiDung);
                        // 🔧 FIX bổ sung: chủ động ẩn tooltip ngay khi chuột nhấn xuống,
                        // đảm bảo click luôn được xử lý ngay cả nếu tooltip đang hiện.
                        control.MouseDown -= Control_MouseDown_HideTooltip; // tránh đăng ký trùng nếu gọi lại
                        control.MouseDown += Control_MouseDown_HideTooltip;
                    }
                    catch
                    {
                        soLoi++;
                    }
                }
#if DEBUG
        if (soLoi > 0)
            System.Diagnostics.Debug.WriteLine(
                $"[InitToolTips] Hệ thống bỏ qua {soLoi} control do chưa khởi tạo hoặc bị null.");
#endif
                _daKhoiTaoToolTip = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi nghiêm trọng tại InitToolTips]: {ex.Message}");
            }
        }
        // Đảm bảo tooltip không "chặn" sự kiện Click của lần bấm đầu tiên
        private void Control_MouseDown_HideTooltip(object? sender, MouseEventArgs e)
        {
            if (sender is System.Windows.Forms.Control control && !control.IsDisposed)
                _toolTipMain?.Hide(control);
        }
        private void EnsureLastRowVisible(DataGridView dgv)
        {
            if (dgv.Rows.Count == 0) return;
            // Chọn dòng đầu tiên để tránh lỗi CurrentCell
            dgv.CurrentCell = null;
            //// Cuộn xuống dòng cuối
            //dgv.FirstDisplayedScrollingRowIndex = dgv.Rows.Count - 1;
        }
        private void CapNhatLabelTheoPhienBan()
        {
            try
            {
                bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem()
                    .LaCheDoTanBinh();
                string doiTuong = laTanBinh ? Module_TaiKhoan.TenTB_Hoa : "CBCS";
                kryptonButton1_HuongDanThemDuLieu.Values.Text =
                    laTanBinh ? $"Thêm {Module_TaiKhoan.TenTB_Thuong}" : "Thêm CBCS";
                groupBox1_TimKiemThongTinCBCS.Text =
                    $"1. Tìm kiếm thông tin {doiTuong}";
                groupBox3_ThongTinCBCS.Text =
                    $"3. Cập nhật thông tin {doiTuong}";
                textBox_STT.ReadOnly = true;
                textBox_SoHieu.ReadOnly = true;
                Color mauNen = Color.FromArgb(198, 239, 206);
                if (textBox_STT is Krypton.Toolkit.KryptonTextBox kStt)
                    kStt.StateCommon.Back.Color1 = mauNen;
                else
                    textBox_STT.BackColor = mauNen;
                if (textBox_SoHieu is Krypton.Toolkit.KryptonTextBox kSoHieu)
                    kSoHieu.StateCommon.Back.Color1 = mauNen;
                else
                    textBox_SoHieu.BackColor = mauNen;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Lỗi khi cập nhật giao diện theo phiên bản:\n" + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        //private void O_CanhBao_MouseEnter(object? sender, EventArgs e)
        //{
            //if (sender is System.Windows.Forms.Control ctrl)
            //{
                //// Đổi nền màu xanh lá
                //ctrl.BackColor = System.Drawing.Color.LightGreen;
                //// Bổ sung riệng cho KryptonTextBox (vì Krypton đôi khi ưu tiên Back.Color1 hơn BackColor thường)
                //// Khi chuột đi ra, trả về màu xám mặc định của ô ReadOnly
        private void TaoCauTrucCotGrid()
        {
            if (kryptonDataGridView1 == null || kryptonDataGridView1.IsDisposed) return;
            kryptonDataGridView1.SuspendLayout();
            try
            {
                kryptonDataGridView1.Columns.Clear();
                string[] cols = { "ID", "STT", "HoVaTen", "SoHieu", "NamSinh", "QueQuan", "NgayVaoCAND", "CapBac", "ChucVu", "DonVi", "PhanLoai", "GhiChu" };
                var leftAlignCols = new HashSet<string>(StringComparer.Ordinal) { "HoVaTen", "QueQuan", "GhiChu" };
                var centerStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter };
                var leftStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleLeft };
                kryptonDataGridView1.ColumnCount = 0;
                foreach (string c in cols)
                {
                    string header = _tenCotTiengViet != null && _tenCotTiengViet.TryGetValue(c, out string? val) ? val : c;
                    var col = new DataGridViewTextBoxColumn
                    {
                        Name = c,
                        HeaderText = header,
                        SortMode = DataGridViewColumnSortMode.NotSortable,
                        DefaultCellStyle = leftAlignCols.Contains(c) ? leftStyle : centerStyle
                    };
                    kryptonDataGridView1.Columns.Add(col);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TaoCauTrucCotGrid Error] {ex.Message}");
            }
            finally
            {
                kryptonDataGridView1.ResumeLayout();
            }
        }
        private void DinhDangDataGridFrom6()
        {
            kryptonDataGridView1.ContextMenuStrip = contextMenuStrip1;
            kryptonDataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            kryptonDataGridView1.MultiSelect = true; // cho phép chọn nhiều dòng (Ctrl/Shift + chuột, Ctrl+A) để xóa hàng loạt
            kryptonDataGridView1.ReadOnly = true;
            kryptonDataGridView1.AllowUserToAddRows = false;
            // ⭐ KHÓA KHÔNG CHO NGƯỜI DÙNG KÉO THẢ DI CHUYỂN CỘT
            kryptonDataGridView1.AllowUserToOrderColumns = false;
            // Thêm 2 dòng này để bật Virtual Mode
            kryptonDataGridView1.VirtualMode = true;
            kryptonDataGridView1.CellValueNeeded += KryptonDataGridView1_CellValueNeeded;
            kryptonDataGridView1.CurrentCell = null;
            kryptonDataGridView1.MouseDown += kryptonDataGridView1_MouseDown;
            kryptonDataGridView1.CellClick += KryptonDataGridView1_CellClick;
            kryptonDataGridView1.CellDoubleClick += kryptonDataGridView1_CellDoubleClick;
            // 🔥 Tắt vĩnh viễn hộp thoại báo lỗi mặc định của DataGridView
            kryptonDataGridView1.DataError += (s, ev) => { ev.ThrowException = false; };
        }
        private void InitFilters()
        {
            // Đã xóa phần gán Module_HeThong.Tat_Ca ở đây để chuyển xuống dưới
            // Các sự kiện cũ giữ nguyên
            comboBox_TimKiemDonVi.SelectedIndexChanged += (_, __) => ApplyFilter();
            comboBox_XepLoaiThiDua.SelectedIndexChanged += (_, __) => ApplyFilter();
            // TextChanged thì dùng Timer để mượt
            textBox_TimKiemTheoTen.TextChanged += (_, __) =>
            {
                timerLocDuLieu?.Stop();
                timerLocDuLieu?.Start();
            };
        }
        /// Hàm 2: Chỉ làm đúng nhiệm vụ Đổi Tên Cột (Gọi sau khi đã gán DataSource)
        /// </summary>
        private void DinhDangDataGridViewHienDai()
        {
            var dgv = kryptonDataGridView1;
            if (dgv == null) return;
            dgv.SuspendLayout();
            try
            {
                // 1. Tùy chỉnh chiều cao động (Dynamic Scaling)
                int fontHeight = dgv.Font.Height;
                dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                dgv.ColumnHeadersHeight = (int)(fontHeight * 2.5);
                dgv.RowTemplate.Height = (int)(fontHeight * 1.8);
                // 2. CĂN GIỮA VÀ IN ĐẬM TIÊU ĐỀ
                var style = dgv.ColumnHeadersDefaultCellStyle;
                if (style.Alignment != DataGridViewContentAlignment.MiddleCenter)
                {
                    style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                if (style.Font == null || !style.Font.Bold)
                {
                    style.Font = new Font(dgv.Font, FontStyle.Bold);
                }
                // ⭐ 3. BỘ GIAO DIỆN HIỆN ĐẠI (OCEAN BLUE THEME)
                // Nền lưới trắng tinh khôi
                dgv.StateCommon.Background.Color1 = Color.White;
                dgv.StateCommon.DataCell.Back.Color1 = Color.White;
                // Viền lưới thanh mảnh, xám nhạt
                dgv.StateCommon.DataCell.Border.Color1 = Color.FromArgb(235, 235, 235);
                dgv.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
                dgv.StateCommon.DataCell.Border.Width = 1;
                // Tiêu đề cột (Header) - Màu Xanh Nước Biển (Đậm và rõ nét hơn)
                dgv.StateCommon.HeaderColumn.Back.Color1 = Color.FromArgb(237, 233, 254); // tím nhạt
                dgv.StateCommon.HeaderColumn.Back.Color2 = Color.FromArgb(237, 233, 254);
                // Chữ màu đen than tuyền để tương phản mạnh với nền xanh
                dgv.StateCommon.HeaderColumn.Content.Color1 = Color.FromArgb(91, 33, 182);
                // Viền header cùng tông xanh nhưng đậm hơn một chút để tạo khối viền
                dgv.StateCommon.HeaderColumn.Border.Color1 = Color.FromArgb(221, 214, 254);
                // ⭐ KHI NGƯỜI DÙNG CLICK CHỌN DÒNG (SELECTED)
                // Nền khi chọn: Xanh dương siêu nhạt (Alice Blue) - Rất dịu mắt
                dgv.StateSelected.DataCell.Back.Color1 = Color.FromArgb(232, 244, 253);
                dgv.StateSelected.DataCell.Back.Color2 = Color.FromArgb(232, 244, 253); // Ép Color 2 để tắt chế độ bóng mờ gradient cũ
                // Chữ khi chọn: Xanh nước biển đậm, sắc nét (Microsoft Fluent Blue)
                dgv.StateSelected.DataCell.Content.Color1 = Color.FromArgb(0, 102, 204);
                // Cập nhật lại chiều cao cho các dòng ĐÃ CÓ SẴN
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    row.Height = dgv.RowTemplate.Height;
                }
            }
            finally
            {
                dgv.ResumeLayout();
            }
        }
        private void DoiTenCotTiengViet()
        {
            var dgv = kryptonDataGridView1;
            // Kiểm tra an toàn tối đa (Guard Clauses)
            if (dgv == null || dgv.Columns.Count == 0 || _tenCotTiengViet == null || _tenCotTiengViet.Count == 0)
                return;
            dgv.SuspendLayout();
            try
            {
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (string.IsNullOrEmpty(col.Name)) continue;
                    if (_tenCotTiengViet.TryGetValue(col.Name, out string? header))
                    {
                        // Chỉ gán lại nếu thực sự có sự thay đổi để tránh vẽ lại UI không cần thiết
                        if (!string.Equals(col.HeaderText, header, StringComparison.Ordinal))
                        {
                            col.HeaderText = header;
                        }
                    }
                }
            }
            finally
            {
                dgv.ResumeLayout();
            }
        }
        // 1. HÀM TẢI VÀ GIẢI MÃ DỮ LIỆU (Chạy tuần tự, không bị rớt chữ)
        // 2. HÀM CĂN CHỈNH BẢNG (Xóa Padding để không bị cắt chữ)
        private void CanChinhBang(bool boChonO = true)
        {
            var dgv = kryptonDataGridView1;
            if (dgv.Columns.Count == 0) return;
            dgv.SuspendLayout();
            try
            {
                // 1️⃣ Ẩn cột kỹ thuật
                foreach (var colName in new[] { "ID", "STT" })
                    if (dgv.Columns.Contains(colName))
                        dgv.Columns[colName].Visible = false;
                // 🔹 2. XÁC ĐỊNH "FillWeight" CHO TỪNG CỘT
                var fillWeights = new Dictionary<string, int>
                {
                    { "STT", 50 }, { "HoVaTen", 150 }, { "SoHieu", 80 }, { "NamSinh", 70 },
                    { "QueQuan", 190 }, { "NgayVaoCAND", 100 }, { "CapBac", 80 },
                    { "ChucVu", 80 }, { "DonVi", 70 }, { "PhanLoai", 100 }, { "GhiChu", 200 }
                };
                foreach (var key in fillWeights.Keys)
                {
                    if (!dgv.Columns.Contains(key)) dgv.Columns.Add(key, key);
                }
                int totalWeight = fillWeights.Where(k => dgv.Columns.Contains(k.Key)).Sum(k => k.Value);
                if (totalWeight == 0) totalWeight = 1;
                int availableWidth = dgv.ClientSize.Width - dgv.RowHeadersWidth - 2;
                foreach (var kvp in fillWeights)
                {
                    if (!dgv.Columns.Contains(kvp.Key)) continue;
                    var col = dgv.Columns[kvp.Key];
                    col.MinimumWidth = 50;
                    if (kvp.Key == "GhiChu")
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    else
                        col.Width = (int)((kvp.Value / (double)totalWeight) * availableWidth);
                }
                // 🔹 6. ẨN CÁC CỘT KỸ THUẬT (nếu có)
                string[] hiddenCols = { "SomeInternalColumn" };
                foreach (var colName in hiddenCols)
                {
                    if (dgv.Columns.Contains(colName)) dgv.Columns[colName].Visible = false;
                }
                // ⭐ FIX LỖI CẮT CHỮ: Xóa lớp đệm ảo, trả lại khung hiển thị chuẩn
                if (dgv.Padding != Padding.Empty) dgv.Padding = new Padding(0);
                // Chỉ bỏ chọn ô khi nạp lại dữ liệu; khi chỉ đổi kích thước cửa sổ thì giữ nguyên dòng đang chọn
                if (boChonO) dgv.CurrentCell = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("CanChinhBang lỗi: " + ex.Message);
            }
            finally
            {
                dgv.ResumeLayout();
            }
        }
        private bool KiemTraPhienBanTanBinh()
        {
            if (_isTanBinhCached.HasValue) return _isTanBinhCached.Value;
            try
            {
                _isTanBinhCached = Module_TaiKhoan.LayPhienBanPhanMem().LaCheDoTanBinh();
            }
            catch
            {
                _isTanBinhCached = false;
            }
            return _isTanBinhCached.Value;
        }
        private async void kryptonDataGridView1_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            // 1. Chặn click vào Header, lưới rỗng, hoặc ĐANG MỞ FORM TRƯỚC ĐÓ (Chống Spam Click)
            if (e.RowIndex < 0 || sender is not DataGridView grid || _isOpeningProfile) return;
            // Lưu lại trạng thái Status cũ để khôi phục (Tránh việc gán cứng chữ "Sẵn sàng")
            string? statusCu = toolStripStatusLabel1.Text;
            try
            {
                _isOpeningProfile = true; // Khóa cổng, không cho click phát thứ 2
                // 🟢 TỐI ƯU HIỆU NĂNG: Bắt lấy Row 1 lần duy nhất
                DataGridViewRow row = grid.Rows[e.RowIndex];
                // 2. Lấy dữ liệu an toàn
                string hoTen = row.Cells["HoVaTen"]?.Value?.ToString() ?? "";
                // Nếu click trúng dòng ảo (dòng trống dưới cùng) thì bỏ qua
                if (string.IsNullOrWhiteSpace(hoTen)) return;
                // LẤY SỐ HIỆU VÀ CÁC THÔNG TIN CƠ BẢN
                string soHieu = row.Cells["SoHieu"]?.Value?.ToString() ?? textBox_SoHieu.Text.Trim();
                string donVi = row.Cells["DonVi"]?.Value?.ToString() ?? "Không xác định";
                const string tinhTrang = Module_HeThong.TT_DANG_CONG_TAC;
                if (string.IsNullOrEmpty(soHieu))
                {
                    MessageBox.Show($"⚠ Không lấy được Số hiệu của {Module_HeThong.Tu_dong_chi} này!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                // UX: Hiện thông báo chờ mượt mà
                toolStripStatusLabel1.Text = $"Đang mở hồ sơ thi đua của {hoTen}...";
                toolStripStatusLabel1.ForeColor = Color.Blue;
                this.Cursor = Cursors.WaitCursor;
                // Nhịp nghỉ ngắn để giao diện kịp cập nhật chữ Status trước khi load Form nặng
                await Task.Delay(50);
                // 3. KIỂM TRA PHIÊN BẢN (Đọc từ Cache RAM siêu tốc O(1))
                bool laTanBinh = KiemTraPhienBanTanBinh();
                if (laTanBinh)
                {
                    // NHÁNH A: DÀNH CHO TÂN BINH (Sử dụng Form 30)
                    if (_frm30ViewInstance == null || _frm30ViewInstance.IsDisposed)
                    {
                        _frm30ViewInstance = new Form30_ChinhSuaDataTanBinh(soHieu, hoTen, donVi, true)
                        {
                            Text = $"Hồ sơ thi đua - {hoTen}"
                        };
                        _frm30ViewInstance.Show(this);
                    }
                    else
                    {
                        _frm30ViewInstance.CapNhatDuLieuMoi(soHieu, hoTen, donVi);
                        if (_frm30ViewInstance.WindowState == FormWindowState.Minimized)
                            _frm30ViewInstance.WindowState = FormWindowState.Normal;
                        _frm30ViewInstance.BringToFront();
                        _frm30ViewInstance.Activate(); // Ép hệ điều hành focus vào form
                    }
                }
                else
                {
                    // NHÁNH B: DÀNH CHO CÁN BỘ CHIẾN SĨ (Sử dụng Form 22)
                    if (_frm22ViewInstance == null || _frm22ViewInstance.IsDisposed)
                    {
                        _frm22ViewInstance = new Form22_ChinhSuaDataCBCS()
                        {
                            SoHieu = soHieu,
                            HoVaTen = hoTen,
                            DonVi = donVi,
                            TinhTrang = tinhTrang,
                            IsViewOnly = true,
                            Text = $"Hồ sơ thi đua - {hoTen}"
                        };
                        _frm22ViewInstance.Show(this);
                    }
                    else
                    {
                        _frm22ViewInstance.CapNhatDuLieuMoi(soHieu, hoTen, donVi, tinhTrang);
                        if (_frm22ViewInstance.WindowState == FormWindowState.Minimized)
                            _frm22ViewInstance.WindowState = FormWindowState.Normal;
                        _frm22ViewInstance.BringToFront();
                        _frm22ViewInstance.Activate(); // Ép hệ điều hành focus vào form
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi truy xuất hệ thống:\n{ex.Message}", "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Trả lại trạng thái UI ban đầu cực kỳ an toàn
                toolStripStatusLabel1.Text = statusCu;
                toolStripStatusLabel1.ForeColor = Color.Black;
                this.Cursor = Cursors.Default;
                _isOpeningProfile = false; // Mở khóa cổng cho lần click tiếp theo
            }
        }
        private void kryptonDataGridView1_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            if (sender is not DataGridView dgv) return;
            var hit = dgv.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0 || hit.ColumnIndex < 0) return;
            var row = dgv.Rows[hit.RowIndex];
            if (!row.Visible) return; // quan trọng: chỉ set CurrentCell khi row hiển thị
            // Bấm chuột phải lên dòng ĐANG nằm trong vùng chọn nhiều -> giữ nguyên vùng chọn để xóa hàng loạt
            if ((dgv.Rows.GetRowState(hit.RowIndex) & DataGridViewElementStates.Selected) != 0 &&
                dgv.Rows.GetRowCount(DataGridViewElementStates.Selected) > 1) return;
            dgv.ClearSelection();
            row.Selected = true;
            // Chỉ set CurrentCell nếu có cột hiển thị
            DataGridViewCell? firstVisibleCell = null;
            foreach (DataGridViewCell cell in row.Cells)
            {
                if (cell.Visible)
                {
                    firstVisibleCell = cell;
                    break;
                }
            }
            dgv.CurrentCell = firstVisibleCell;
        }
        // ======================================================================
        //  XÓA DỮ LIỆU CBCS (đã tối ưu)
        //  - Bỏ VACUUM khỏi đường xóa (nút chặn chính). Chỉ nén nền khi thật sự cần.
        //  - Đánh lại STT bằng 1 câu UPDATE ... FROM O(n log n), chỉ ghi dòng có STT đổi
        //    (câu cũ dùng subquery tương quan: O(n^2), rất chậm khi STT là khóa chính).
        //  - Đồng bộ RAM: BeginLoadData/EndLoadData + chỉ ghi ô STT thật sự thay đổi
        //    (trước đây mỗi lần gán row["STT"] đều bắn sự kiện + dựng lại DataView).
        //  - Tác vụ phụ (nhật ký, form khác, biểu đồ) chạy SAU khi lưới đã vẽ xong.
        // ======================================================================
        private bool _dangXuLyXoa = false;
        // Tuần tự hóa mọi thao tác ghi CSDL2 từ Form6 (xóa / nén nền) -> không bị "database is locked"
        private static readonly SemaphoreSlim _khoaGhiCSDL2 = new(1, 1);
        // Chỉ nén (VACUUM) khi số trang trống đủ lớn: >= 500 trang và >= 25% tổng số trang
        private const int NGUONG_TRANG_TRONG_DE_NEN = 500;
        private const double TY_LE_TRANG_TRONG_DE_NEN = 0.25;
        private async void kryptonButton_XoaDataCBCS_Click(object? sender, EventArgs e)
        {
            // ===== 1. CHỐNG SPAM VÀ VALIDATE UI (Chạy trên UI Thread) =====
            if (_dangXuLyXoa || _dangGanPhanLoai) return;
            if (string.IsNullOrWhiteSpace(textBox_STT.Text) ||
                !int.TryParse(textBox_STT.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int stt))
            {
                MessageBox.Show("Chưa xác định được dữ liệu dòng cần xử lý.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string hoTen = string.IsNullOrWhiteSpace(textBox_HoVaTen.Text) ? "[Không rõ]" : textBox_HoVaTen.Text.Trim();
            string soHieu = string.IsNullOrWhiteSpace(textBox_SoHieu.Text) ? "[Không rõ]" : textBox_SoHieu.Text.Trim();
            string donVi = string.IsNullOrWhiteSpace(textBox_DonVi.Text) ? "[Không rõ]" : textBox_DonVi.Text.Trim();
            string thongBao =
                $"Bạn có chắc chắn muốn xóa thông tin của {Module_HeThong.Tu_dong_chi}:\n\n" +
                $"Họ và tên: {hoTen}\n" +
                $"Số hiệu: {soHieu}\n" +
                $"Đơn vị: {donVi}\n\n" +
                $"Lưu ý: Hành động này không thể hoàn tác!";
            if (MessageBox.Show(thongBao, "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            string? csdlPath = _csdl2Path;
            if (string.IsNullOrWhiteSpace(csdlPath) || !File.Exists(csdlPath))
            {
                MessageBox.Show("Không thể truy cập dữ liệu hệ thống.", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            _dangXuLyXoa = true;
            string? statusCu = toolStripStatusLabel1.Text;
            toolStripStatusLabel1.Text = "Đang xử lý xóa dữ liệu...";
            this.Cursor = Cursors.WaitCursor;
            kryptonButton_XoaDataCBCS.Enabled = false; // phản hồi tức thì, chặn bấm lặp
            bool xoaThanhCong = false;
            try
            {
                // ===== 2. XÓA DATABASE TRÊN LUỒNG NGẦM (nhanh: không còn VACUUM) =====
                var kq = await Task.Run(() => ThucThiXoaDatabase(csdlPath, stt));
                // Sau await, Form có thể đã bị đóng -> luôn kiểm tra trước khi đụng vào UI
                if (this.IsDisposed || !this.IsHandleCreated) return;
                if (!kq.DaXoa)
                {
                    toolStripStatusLabel1.Text = statusCu;
                    MessageBox.Show("Dữ liệu không tồn tại hoặc đã bị thay đổi bởi người khác.", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                xoaThanhCong = true;
                // ===== 3. ĐỒNG BỘ RAM + LƯỚI NGAY LẬP TỨC =====
                DongBoRamSauKhiXoa(stt);
                ClearThongTin();
                CapNhatThongKeToanBoQuanSo();
                toolStripStatusLabel1.Text = "Xóa dữ liệu thành công.";
                kryptonDataGridView1.Update(); // vẽ xong lưới trước khi làm các việc phụ bên dưới
                // ===== 4. CÁC TÁC VỤ PHỤ (không chặn người dùng) =====
                _ = Task.Run(() =>
                {
                    try
                    {
                        Module_NhatKy.GhiNhatKy(
                            taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                            hanhDong: $"Xóa dữ liệu: {hoTen} (Số hiệu: {soHieu})",
                            ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                        );
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Lỗi ghi log: {ex.Message}");
                    }
                });
                DataCache.Clear(); // Đánh dấu cache hết hạn để các form khác tự lấy lại data mới
                ThongBaoForm4CapNhatLoaiDeXuat();
                Module_DanduongGPS.OnDatabaseChanged?.Invoke();
                _ = Module_BieuDoTronTrangChu.CapNhatBieuDoForm4Async();
                // Nén CSDL ở nền, chỉ khi file thực sự phình do nhiều trang trống
                if (kq.CanNen) _ = NenCSDLNenAsync(csdlPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi xóa dữ liệu CBCS: {ex}");
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    // DB đã xóa nhưng RAM chưa đồng bộ -> nạp lại để lưới không lệch với DB
                    if (xoaThanhCong)
                    {
                        try { kryptonButton_RefershCSDL.PerformClick(); } catch { /* bỏ qua */ }
                    }
                    MessageBox.Show("Có lỗi xảy ra trong quá trình xử lý dữ liệu.\nChi tiết: " + ex.Message,
                        "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                _dangXuLyXoa = false;
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    kryptonButton_XoaDataCBCS.Enabled = true;
                    this.Cursor = Cursors.Default;
                }
            }
        }
        /// <summary>
        /// Xóa dòng khỏi DataTable gốc + đánh lại STT trên RAM + cập nhật lưới ảo (Virtual Mode)
        /// với số lần vẽ lại và số sự kiện DataTable tối thiểu. Phải gọi trên UI thread.
        /// </summary>
        private void DongBoRamSauKhiXoa(int stt)
        {
            var dt = dtDanhSachGoc;
            var grid = kryptonDataGridView1;
            if (dt == null || grid == null || grid.IsDisposed) return;
            int ordStt = dt.Columns.IndexOf("STT");
            int ordTen = dt.Columns.IndexOf("HoVaTen");
            if (ordStt < 0) return;
            bool redrawSuspended = false;
            try
            {
                if (grid.IsHandleCreated)
                {
                    SendMessage(grid.Handle, WM_SETREDRAW, 0, null!);
                    redrawSuspended = true;
                }
                // Tắt thông báo/ràng buộc/chỉ mục của DataTable trong lúc sửa hàng loạt
                dt.BeginLoadData();
                try
                {
                    // Tìm theo số (không cấp phát chuỗi cho từng dòng như bản cũ)
                    int viTriXoa = -1;
                    var rows = dt.Rows;
                    for (int i = 0; i < rows.Count; i++)
                    {
                        if (DocSoNguyen(rows[i][ordStt]) == stt) { viTriXoa = i; break; }
                    }
                    if (viTriXoa >= 0) rows.RemoveAt(viTriXoa);
                    // Đánh lại STT: bỏ qua dòng ảo (HoVaTen rỗng), CHỈ ghi ô nào thật sự đổi.
                    // Các dòng đứng trước vị trí xóa không bị ghi gì cả.
                    int newStt = 1;
                    for (int i = 0; i < rows.Count; i++)
                    {
                        var row = rows[i];
                        if (ordTen >= 0)
                        {
                            object ten = row[ordTen];
                            if (ten == DBNull.Value || string.IsNullOrWhiteSpace(ten as string ?? Convert.ToString(ten, CultureInfo.InvariantCulture)))
                                continue;
                        }
                        if (DocSoNguyen(row[ordStt]) != newStt) row[ordStt] = newStt;
                        newStt++;
                    }
                }
                finally
                {
                    dt.EndLoadData(); // dựng lại chỉ mục DataView đúng 1 lần
                }
                dt.AcceptChanges();
                // Vị trí các dòng đã dịch chuyển -> dựng lại chỉ mục lọc rồi áp lại bộ lọc hiện hành
                // (hàm này tự quản lý việc tắt/bật vẽ nên gọi thẳng phần lõi, không gọi ApplyFilter)
                _boLoc = null;
                ApDungBoLoc();
            }
            finally
            {
                // Bắt buộc bật lại chế độ vẽ để lưới không bao giờ bị đơ vĩnh viễn
                if (redrawSuspended && grid.IsHandleCreated && !grid.IsDisposed)
                {
                    SendMessage(grid.Handle, WM_SETREDRAW, 1, null!);
                    grid.Invalidate();
                }
            }
        }
        // Đọc STT dạng số an toàn (int/long/string/DBNull) -> -1 nếu không đọc được
        private static int DocSoNguyen(object? v)
        {
            switch (v)
            {
                case null: return -1;
                case DBNull: return -1;
                case int i: return i;
                case long l: return l is >= int.MinValue and <= int.MaxValue ? (int)l : -1;
                case string str: return int.TryParse(str.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int r) ? r : -1;
                default:
                    try { return Convert.ToInt32(v, CultureInfo.InvariantCulture); }
                    catch { return -1; }
            }
        }
        private readonly record struct KetQuaXoaDb(bool DaXoa, bool CanNen);
        // Hàm xử lý DB cô lập hoàn toàn khỏi UI (chạy ngầm an toàn)
        private KetQuaXoaDb ThucThiXoaDatabase(string csdlPath, int stt)
        {
            _khoaGhiCSDL2.Wait();
            try
            {
                using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(csdlPath));
                conn.Open();
                bool daXoa;
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new SqliteCommand($"DELETE FROM {Module_TaiKhoan.BangDanhSach} WHERE STT = @STT", conn, transaction))
                        {
                            cmd.Parameters.Add("@STT", SqliteType.Integer).Value = stt;
                            daXoa = cmd.ExecuteNonQuery() > 0;
                        }
                        if (!daXoa)
                        {
                            transaction.Rollback();
                            return new KetQuaXoaDb(false, false);
                        }
                        DanhLaiSoThuTu(conn, transaction);
                        transaction.Commit();
                    }
                    catch
                    {
                        try { transaction.Rollback(); } catch { /* transaction đã hỏng/đóng */ }
                        throw;
                    }
                }
                // Quyết định có cần nén hay không (rất rẻ: chỉ đọc 2 PRAGMA)
                bool canNen = false;
                try
                {
                    long freelist = LayGiaTriPragma(conn, "freelist_count");
                    long pageCount = LayGiaTriPragma(conn, "page_count");
                    canNen = pageCount > 0
                             && freelist >= NGUONG_TRANG_TRONG_DE_NEN
                             && (double)freelist / pageCount >= TY_LE_TRANG_TRONG_DE_NEN;
                }
                catch { /* không quan trọng */ }
                return new KetQuaXoaDb(true, canNen);
            }
            finally
            {
                _khoaGhiCSDL2.Release();
            }
        }
        // ======================================================================
        //  CHỌN NHIỀU / XÓA NHIỀU CBCS (menu chuột phải, giống Form34)
        // ======================================================================
        private ToolStripMenuItem? _mnuChonTatCa;
        private ToolStripMenuItem? _mnuXoaDongDaChon;
        private void KhoiTaoXoaNhieu()
        {
            var grid = kryptonDataGridView1;
            if (grid == null || contextMenuStrip1 == null || _mnuXoaDongDaChon != null) return;
            _mnuChonTatCa = new ToolStripMenuItem("Chọn tất cả các dòng đang hiển thị")
            {
                Name = "chonTatCaCacDong_ToolStripMenuItem",
                ShortcutKeyDisplayString = "Ctrl+A"
            };
            _mnuChonTatCa.Click += (s, e) => ChonTatCaDongDangHienThi();
            _mnuXoaDongDaChon = new ToolStripMenuItem("Xóa các CBCS đã chọn")
            {
                Name = "xoaCacCBCSDaChon_ToolStripMenuItem",
                ShortcutKeyDisplayString = "Delete",
                Image = xoaToanBoDuLieuDSCBCS_ToolStripMenuItem?.Image // dùng chung icon thùng rác
            };
            _mnuXoaDongDaChon.Click += async (s, e) => await XoaCacDongDaChonAsync();
            KhoiTaoGanPhanLoaiNhanh(); // phải tạo menu "Gán phân loại" TRƯỚC khi sắp xếp menu
            SapXepMenuChuotPhai();
            contextMenuStrip1.Opening += ContextMenuStrip1_Opening_XoaNhieu;
            grid.KeyDown += Grid_KeyDown_XoaNhieu;
        }
        // Sắp xếp menu chuột phải theo nhóm chức năng, đi từ thao tác thường dùng -> nhập/xuất -> phân tích/quản lý,
        // nhóm XÓA (nguy hiểm) luôn nằm cuối cùng, tách riêng để tránh bấm nhầm.
        private void SapXepMenuChuotPhai()
        {
            var menu = contextMenuStrip1;
            if (menu == null) return;
            // Sửa lỗi chính tả trên Designer ("KIểm tra kết nối")
            if (kiemTraKetNoi != null) kiemTraKetNoi.Text = "Kiểm tra kết nối";
            ToolStripSeparator VachNgan(ToolStripSeparator? coSan, string ten) => coSan ?? new ToolStripSeparator { Name = ten };
            var thuTu = new ToolStripItem?[]
            {
                // 1. Hệ thống
                lamMoiHeThong,
                kiemTraKetNoi,
                VachNgan(toolStripMenuItem1, "vachNgan_TimKiem"),
                // 2. Tìm kiếm / chọn dòng
                xoaTimKiem,
                _mnuChonTatCa,
                _mnuGanPhanLoai,
                VachNgan(toolStripMenuItem2, "vachNgan_Nhap"),
                // 3. Nhập dữ liệu
                themDuLieuTuFileExce_ToolStripMenuItem,
                nhapKetQuaPhanLoaiTuDonVi_ToolStripMenuItem,
                VachNgan(toolStripMenuItem3, "vachNgan_Xuat"),
                // 4. Xuất dữ liệu
                xuatDuLieuSangThongKe_ToolStripMenuItem,
                xuatDuLieuThiDuaRaTepExcel_ToolStripMenuItem,
                VachNgan(toolStripMenuItem4, "vachNgan_PhanTich"),
                // 5. Phân tích
                toolStripMenuItem_PhanTichQuanSo,
                phanTichDuLieuTrungTen_ToolStripMenuItem,
                new ToolStripSeparator { Name = "vachNgan_QuanLy" },
                // 6. Quản lý
                cBCSTrongDienQuanLy_ToolStripMenuItem,
                ToolStripMenuItem_QuanLyThiDuaBaNhat,
                new ToolStripSeparator { Name = "vachNgan_Xoa" },
                // 7. Xóa dữ liệu (luôn ở cuối)
                _mnuXoaDongDaChon,
                xoaToanBoDuLieuDSCBCS_ToolStripMenuItem,
            };
            // Mục nào do code khác thêm vào (không có trong danh sách trên) thì giữ lại, đặt trước nhóm Xóa
            var daXep = new HashSet<ToolStripItem>(thuTu.Where(x => x != null)!);
            var mucKhac = menu.Items.Cast<ToolStripItem>().Where(x => !daXep.Contains(x)).ToList();
            menu.SuspendLayout();
            try
            {
                menu.Items.Clear();
                foreach (var muc in thuTu)
                {
                    if (muc == null) continue;
                    if (muc.Name == "vachNgan_Xoa" && mucKhac.Count > 0)
                    {
                        menu.Items.Add(new ToolStripSeparator());
                        foreach (var k in mucKhac) menu.Items.Add(k);
                    }
                    menu.Items.Add(muc);
                }
            }
            finally
            {
                menu.ResumeLayout();
            }
            AnVachNganThua(menu);
        }
        // Ẩn vạch ngăn ở đầu/cuối menu hoặc nằm liền nhau (khi cả nhóm bị ẩn) -> menu không bị hở khoảng trống
        private static void AnVachNganThua(ToolStrip menu)
        {
            ToolStripItem? truocDo = null; // mục hiển thị gần nhất
            foreach (ToolStripItem muc in menu.Items)
            {
                if (muc is ToolStripSeparator vach)
                {
                    vach.Available = true;
                    if (truocDo == null || truocDo is ToolStripSeparator) vach.Available = false;
                    else truocDo = vach;
                    continue;
                }
                if (muc.Available) truocDo = muc;
            }
            if (truocDo is ToolStripSeparator cuoi) cuoi.Available = false;
        }
        private void ContextMenuStrip1_Opening_XoaNhieu(object? sender, CancelEventArgs e)
        {
            var grid = kryptonDataGridView1;
            if (grid == null || grid.IsDisposed || _mnuXoaDongDaChon == null || _mnuChonTatCa == null) return;
            // Lưới không có CBCS thật nào (trống hoặc chỉ còn dòng đệm) -> ẩn 2 mục chọn/xóa nhiều
            bool coDuLieu = LuoiCoDuLieuThat();
            // Dùng Available (không dùng Visible): Visible luôn trả false khi menu chưa hiện
            _mnuChonTatCa.Available = coDuLieu;
            _mnuXoaDongDaChon.Available = coDuLieu;
            CapNhatMenuGanPhanLoai(coDuLieu);
            AnVachNganThua(contextMenuStrip1);
            if (!coDuLieu) return;
            // GetRowCount không dựng đối tượng Row (an toàn cho Virtual Mode khi chọn vài nghìn dòng)
            int soDong = grid.Rows.GetRowCount(DataGridViewElementStates.Selected);
            _mnuXoaDongDaChon.Enabled = soDong > 0 && !_dangXuLyXoa;
            _mnuXoaDongDaChon.Text = soDong > 1 ? $"Xóa {soDong:N0} CBCS đã chọn" : "Xóa các CBCS đã chọn";
            _mnuChonTatCa.Enabled = grid.RowCount > 0;
        }
        // Có ít nhất 1 dòng ĐANG HIỂN THỊ có họ tên (bỏ qua dòng đệm trống). Dừng ngay khi gặp dòng đầu tiên -> rất rẻ.
        private bool LuoiCoDuLieuThat()
        {
            var grid = kryptonDataGridView1;
            var dt = dtDanhSachGoc;
            if (grid == null || grid.IsDisposed || dt == null || grid.RowCount == 0) return false;
            int ordTen = dt.Columns.IndexOf("HoVaTen");
            if (ordTen < 0) return false;
            for (int i = 0; i < grid.RowCount; i++)
            {
                var row = LayDongHienThi(i);
                if (row == null) continue;
                object v = row[ordTen];
                if (v != DBNull.Value && !string.IsNullOrWhiteSpace(v as string ?? Convert.ToString(v, CultureInfo.InvariantCulture)))
                    return true;
            }
            return false;
        }
        private async void Grid_KeyDown_XoaNhieu(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Delete || e.Modifiers != Keys.None) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            await XoaCacDongDaChonAsync();
        }
        private void ChonTatCaDongDangHienThi()
        {
            var grid = kryptonDataGridView1;
            if (grid == null || grid.IsDisposed || grid.RowCount == 0) return;
            grid.Focus();
            grid.SelectAll();
        }
        // Lấy STT + họ tên các dòng đang chọn (bỏ dòng đệm trống). KHÔNG dùng SelectedRows (dựng Row cho từng dòng -> phình RAM).
        private List<(int Stt, string HoTen)> LayCacDongDaChon()
        {
            var ketQua = new List<(int Stt, string HoTen)>();
            var grid = kryptonDataGridView1;
            var dt = dtDanhSachGoc;
            if (grid == null || grid.IsDisposed || dt == null) return ketQua;
            int ordStt = dt.Columns.IndexOf("STT");
            int ordTen = dt.Columns.IndexOf("HoVaTen");
            if (ordStt < 0 || ordTen < 0) return ketQua;
            var daCo = new HashSet<int>();
            for (int i = 0; i < grid.RowCount; i++)
            {
                if ((grid.Rows.GetRowState(i) & DataGridViewElementStates.Selected) == 0) continue;
                var row = LayDongHienThi(i);
                if (row == null) continue;
                object giaTriTen = row[ordTen];
                string ten = giaTriTen as string ?? (giaTriTen == DBNull.Value ? "" : Convert.ToString(giaTriTen, CultureInfo.InvariantCulture) ?? "");
                if (string.IsNullOrWhiteSpace(ten)) continue;
                int stt = DocSoNguyen(row[ordStt]);
                if (stt > 0 && daCo.Add(stt)) ketQua.Add((stt, ten.Trim()));
            }
            return ketQua;
        }
        private async Task XoaCacDongDaChonAsync()
        {
            if (_dangXuLyXoa || _dangGanPhanLoai || IsDisposed || Disposing) return;
            var dsChon = LayCacDongDaChon();
            if (dsChon.Count == 0)
            {
                MessageBox.Show(this, "Vui lòng chọn ít nhất một dòng CBCS cần xóa.\n(Giữ Ctrl/Shift và bấm chuột để chọn nhiều, Ctrl+A để chọn tất cả)",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string? csdlPath = _csdl2Path;
            if (string.IsNullOrWhiteSpace(csdlPath) || !File.Exists(csdlPath))
            {
                MessageBox.Show(this, "Không tìm thấy csdl2.db!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            const int SO_TEN_HIEN = 10;
            var sb = new StringBuilder();
            foreach (var d in dsChon.Take(SO_TEN_HIEN)) sb.AppendLine($"  • {d.HoTen}");
            if (dsChon.Count > SO_TEN_HIEN) sb.AppendLine($"  ... và {dsChon.Count - SO_TEN_HIEN:N0} người khác");
            var xacNhan = MessageBox.Show(this,
                $"Bạn sắp xóa vĩnh viễn {dsChon.Count:N0} cán bộ, chiến sĩ đã chọn khỏi danh sách:\n\n{sb}\n" +
                "Thao tác này không thể hoàn tác. Bạn có chắc chắn muốn tiếp tục?",
                "Xác nhận xóa nhiều CBCS", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (xacNhan != DialogResult.Yes) return;
            // Xóa từ 2 người trở lên phải xác minh quyền quản trị (giống Form34 / Xóa toàn bộ)
            if (dsChon.Count > 1)
            {
                DialogResult kqAdmin;
                using (var frm = new Form24_XacMinhAdmin())
                {
                    frm.TopMost = true;
                    frm.StartPosition = FormStartPosition.CenterScreen;
                    kqAdmin = frm.ShowDialog(this);
                }
                if (kqAdmin != DialogResult.OK) return;
            }
            _dangXuLyXoa = true;
            string? statusCu = toolStripStatusLabel1.Text;
            toolStripStatusLabel1.Text = $"Đang xóa {dsChon.Count:N0} CBCS...";
            this.Enabled = false;
            this.UseWaitCursor = true;
            bool xoaThanhCong = false;
            int soDongDaXoa = 0;
            try
            {
                var dsStt = dsChon.Select(d => d.Stt).ToList();
                var kq = await Task.Run(() => ThucThiXoaNhieuDatabase(csdlPath, dsStt));
                if (IsDisposed || !IsHandleCreated) return;
                soDongDaXoa = kq.SoDong;
                xoaThanhCong = true;
                DataCache.Clear();
                ClearThongTin();
                await ReloadDuLieu();
                if (IsDisposed || !IsHandleCreated) return;
                CapNhatThongKeToanBoQuanSo();
                ThongBaoForm4CapNhatLoaiDeXuat();
                KiemTraDuLieu();
                toolStripStatusLabel1.Text = $"Đã xóa {soDongDaXoa:N0} CBCS.";
                string tomTat = string.Join(", ", dsChon.Take(20).Select(d => d.HoTen)) + (dsChon.Count > 20 ? ", ..." : "");
                _ = Task.Run(() =>
                {
                    try
                    {
                        Module_NhatKy.GhiNhatKy(
                            Module_TaiKhoan.TenTaiKhoan_RAM,
                            $"Xóa nhiều CBCS ({soDongDaXoa} người): {tomTat}",
                            DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
                    }
                    catch (Exception ex) { Debug.WriteLine($"Lỗi ghi log: {ex.Message}"); }
                });
                Module_DanduongGPS.OnDatabaseChanged?.Invoke();
                _ = Module_BieuDoTronTrangChu.CapNhatBieuDoForm4Async();
                YeuCauForm15DongBoLai();
                if (kq.CanNen) _ = NenCSDLNenAsync(csdlPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi xóa nhiều CBCS: {ex}");
                toolStripStatusLabel1.Text = statusCu;
                if (!IsDisposed && IsHandleCreated)
                {
                    // DB đã xóa nhưng RAM chưa đồng bộ -> nạp lại để lưới không lệch với DB
                    if (xoaThanhCong)
                    {
                        try { kryptonButton_RefershCSDL.PerformClick(); } catch { /* bỏ qua */ }
                    }
                    this.UseWaitCursor = false;
                    this.Enabled = true;
                    MessageBox.Show(this,
                        (xoaThanhCong ? "Đã xóa nhưng làm mới giao diện bị lỗi:\n" : "Không thể xóa dữ liệu (đã hoàn tác an toàn):\n") + ex.Message,
                        "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }
            finally
            {
                _dangXuLyXoa = false;
                if (!IsDisposed && IsHandleCreated)
                {
                    this.UseWaitCursor = false;
                    this.Enabled = true;
                }
            }
            if (!IsDisposed && IsHandleCreated)
                MessageBox.Show(this, $"Đã xóa thành công {soDongDaXoa:N0} CBCS.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        // ======================================================================
        //  GÁN PHÂN LOẠI NHANH CHO NHIỀU CBCS (chọn là lưu luôn)
        //  Cách dùng (đều lưu ngay vào CSDL, không cần bấm nút Lưu):
        //   - Chuột phải lên các dòng đã chọn -> "Gán phân loại" -> chọn loại.
        //   - Phím tắt khi đang ở lưới: Ctrl+1..Ctrl+5 (theo thứ tự ô Phân loại), Ctrl+0 = để trống.
        //   - Đang chọn từ 2 dòng trở lên mà chọn ô "Phân loại" bên dưới -> áp cho cả nhóm.
        //  An toàn:
        //   - Chỉ ghi DUY NHẤT cột PhanLoai, các cột khác không bị đụng tới.
        //   - 1 transaction cho cả nhóm: kiểm tra từng STT trong CSDL vẫn đúng người (so họ tên đã giải mã)
        //     rồi mới ghi; chỉ cần 1 dòng lệch là hoàn tác toàn bộ, không ghi gì.
        //   - Giữ _reloadGate (không cho Làm mới chen giữa ghi CSDL và vá RAM) + _khoaGhiCSDL2 (không chồng lệnh xóa).
        //   - Mã hóa lỗi (khóa chưa nạp) -> dừng, không bao giờ ghi chuỗi rỗng đè lên phân loại thật.
        // ======================================================================
        private ToolStripMenuItem? _mnuGanPhanLoai;
        private bool _dangGanPhanLoai = false;
        private const string GIA_TRI_DE_TRONG = "";
        private readonly record struct DongGanPhanLoai(DataRow Dong, int Stt, string HoTen);
        private readonly record struct KetQuaGanPhanLoaiDb(int SoDong, string? LoiKhongKhop);
        private void KhoiTaoGanPhanLoaiNhanh()
        {
            if (_mnuGanPhanLoai != null) return;
            _mnuGanPhanLoai = new ToolStripMenuItem("Gán phân loại cho CBCS đã chọn")
            {
                Name = "ganPhanLoaiNhanh_ToolStripMenuItem"
            };
            // Cần ít nhất 1 mục con để mũi tên menu con hiện ra; danh sách thật được dựng lại mỗi lần mở menu
            _mnuGanPhanLoai.DropDownItems.Add(new ToolStripMenuItem("(đang nạp...)") { Enabled = false });
            if (comboBox_PhanLoai != null)
            {
                comboBox_PhanLoai.SelectionChangeCommitted += ComboBoxPhanLoai_SelectionChangeCommitted;
            }
        }
        // Danh sách phân loại chuẩn theo đúng thứ tự trên ô Phân loại (5 mục đầu do LoadComboBoxPhanLoai nạp).
        // Bỏ qua mục "lạ" mà CellClick có thể thêm tạm vào cuối danh sách.
        private List<ComboboxItem> LayCacPhanLoaiChuan()
        {
            var hopLe = new HashSet<string>(StringComparer.Ordinal)
            {
                Module_HeThong.Loai_1, Module_HeThong.Loai_2, Module_HeThong.Loai_3, Module_HeThong.Loai_4, Module_HeThong.PL_KHONG_PL
            };
            var ds = new List<ComboboxItem>();
            if (comboBox_PhanLoai == null) return ds;
            foreach (object o in comboBox_PhanLoai.Items)
            {
                if (o is ComboboxItem it && hopLe.Remove(it.Value)) ds.Add(it);
                if (ds.Count == 5) break;
            }
            return ds;
        }
        // Dựng lại menu con mỗi lần mở menu chuột phải (đúng chế độ hiển thị CSTĐ/CSTT... hoặc Loại 1/2...)
        private void CapNhatMenuGanPhanLoai(bool coDuLieu)
        {
            var mnu = _mnuGanPhanLoai;
            var grid = kryptonDataGridView1;
            if (mnu == null || grid == null || grid.IsDisposed) return;
            mnu.Available = coDuLieu;
            if (!coDuLieu) return;
            int soDong = grid.Rows.GetRowCount(DataGridViewElementStates.Selected);
            mnu.Text = soDong > 1 ? $"Gán phân loại cho {soDong:N0} CBCS đã chọn" : "Gán phân loại cho CBCS đã chọn";
            mnu.Enabled = soDong > 0 && !_dangGanPhanLoai && !_dangXuLyXoa;
            var cu = mnu.DropDownItems.Cast<ToolStripItem>().ToList();
            mnu.DropDownItems.Clear();
            foreach (var c in cu) c.Dispose();
            var ds = LayCacPhanLoaiChuan();
            for (int i = 0; i < ds.Count; i++)
            {
                var pl = ds[i];
                var muc = new ToolStripMenuItem(pl.Text) { ShortcutKeyDisplayString = $"Ctrl+{i + 1}" };
                muc.Click += async (s, e) => await GanPhanLoaiChoCacDongDaChonAsync(pl.Value, pl.Text);
                mnu.DropDownItems.Add(muc);
            }
            mnu.DropDownItems.Add(new ToolStripSeparator());
            var mucTrong = new ToolStripMenuItem("Để trống phân loại") { ShortcutKeyDisplayString = "Ctrl+0" };
            mucTrong.Click += async (s, e) => await GanPhanLoaiChoCacDongDaChonAsync(GIA_TRI_DE_TRONG, "(để trống)");
            mnu.DropDownItems.Add(mucTrong);
        }
        // Ctrl+1..5 / Ctrl+0 (cả phím số hàng trên và bàn phím số) – CHỈ khi con trỏ đang ở lưới,
        // để không bao giờ kích hoạt nhầm khi đang gõ trong ô nhập liệu.
        private bool XuLyPhimTatGanPhanLoai(Keys key)
        {
            var grid = kryptonDataGridView1;
            if (grid == null || grid.IsDisposed || !grid.ContainsFocus) return false;
            int so = key switch
            {
                >= Keys.D0 and <= Keys.D9 => key - Keys.D0,
                >= Keys.NumPad0 and <= Keys.NumPad9 => key - Keys.NumPad0,
                _ => -1
            };
            if (so < 0 || so > 5) return false;
            if (_dangGanPhanLoai || _dangXuLyXoa) return true; // nuốt phím, đang bận
            if (so == 0)
            {
                _ = GanPhanLoaiChoCacDongDaChonAsync(GIA_TRI_DE_TRONG, "(để trống)");
                return true;
            }
            var ds = LayCacPhanLoaiChuan();
            if (so > ds.Count) return true;
            var pl = ds[so - 1];
            _ = GanPhanLoaiChoCacDongDaChonAsync(pl.Value, pl.Text);
            return true;
        }
        // Người dùng tự chọn ô Phân loại (không bắn khi code gán SelectedItem) trong lúc đang chọn >= 2 dòng -> áp cả nhóm.
        // Chỉ chọn 1 dòng thì giữ nguyên cách cũ (sửa nhiều ô rồi bấm Lưu).
        private void ComboBoxPhanLoai_SelectionChangeCommitted(object? sender, EventArgs e)
        {
            var grid = kryptonDataGridView1;
            if (grid == null || grid.IsDisposed) return;
            if (grid.Rows.GetRowCount(DataGridViewElementStates.Selected) < 2) return; // lọc nhanh, rẻ
            if (LayCacDongDaChonDeGan().Count < 2) return; // đếm đúng số CBCS thật (bỏ dòng đệm trống)
            if (comboBox_PhanLoai.SelectedItem is not ComboboxItem pl) return;
            // Chờ ô chọn đóng hẳn rồi mới xử lý (tránh hộp thoại xác nhận bật lên khi danh sách còn mở)
            BeginInvoke(new Action(async () =>
            {
                try
                {
                    await GanPhanLoaiChoCacDongDaChonAsync(pl.Value, pl.Text);
                }
                finally
                {
                    // Dù lưu / hủy / lỗi: ô Phân loại luôn trả về ĐÚNG giá trị thật (trong RAM) của người đang hiện ở
                    // các ô chi tiết -> nút Lưu (1 người) không bao giờ lưu nhầm giá trị vừa chọn cho cả nhóm.
                    HienThiPhanLoaiCuaDongDangXem();
                }
            }));
        }
        // Đặt ô Phân loại theo phân loại hiện có trong RAM của người có STT ở ô textBox_STT
        private void HienThiPhanLoaiCuaDongDangXem()
        {
            try
            {
                if (IsDisposed || comboBox_PhanLoai == null || comboBox_PhanLoai.IsDisposed) return;
                var dt = dtDanhSachGoc;
                if (dt == null) return;
                if (!int.TryParse(textBox_STT.Text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int stt) || stt <= 0)
                {
                    comboBox_PhanLoai.SelectedIndex = -1;
                    comboBox_PhanLoai.Text = "";
                    return;
                }
                int ordStt = dt.Columns.IndexOf("STT");
                int ordPl = dt.Columns.IndexOf("PhanLoai");
                if (ordStt < 0 || ordPl < 0) return;
                DataRow? dong = null;
                foreach (DataRow r in dt.Rows)
                {
                    if (r.RowState != DataRowState.Deleted && r.RowState != DataRowState.Detached && DocSoNguyen(r[ordStt]) == stt)
                    { dong = r; break; }
                }
                if (dong == null) return;
                object v = dong[ordPl];
                string pl = (v as string ?? (v == DBNull.Value ? "" : Convert.ToString(v, CultureInfo.InvariantCulture) ?? "")).Trim();
                if (pl.Length == 0)
                {
                    comboBox_PhanLoai.SelectedIndex = -1;
                    comboBox_PhanLoai.Text = "";
                    return;
                }
                // Cùng quy tắc khớp với CellClick (Value hoặc Text, không phân biệt hoa thường)
                var muc = comboBox_PhanLoai.Items.OfType<ComboboxItem>()
                    .FirstOrDefault(x => string.Equals(x.Value, pl, StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(x.Text, pl, StringComparison.OrdinalIgnoreCase));
                if (muc == null)
                {
                    muc = new ComboboxItem(pl, pl);
                    comboBox_PhanLoai.Items.Add(muc);
                }
                comboBox_PhanLoai.SelectedItem = muc;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[HienThiPhanLoaiCuaDongDangXem] " + ex.Message);
            }
        }
        // Lấy các dòng thật đang chọn kèm DataRow (bỏ dòng đệm trống, bỏ trùng STT)
        private List<DongGanPhanLoai> LayCacDongDaChonDeGan()
        {
            var kq = new List<DongGanPhanLoai>();
            var grid = kryptonDataGridView1;
            var dt = dtDanhSachGoc;
            if (grid == null || grid.IsDisposed || dt == null) return kq;
            int ordStt = dt.Columns.IndexOf("STT");
            int ordTen = dt.Columns.IndexOf("HoVaTen");
            if (ordStt < 0 || ordTen < 0) return kq;
            var daCo = new HashSet<int>();
            for (int i = 0; i < grid.RowCount; i++)
            {
                if ((grid.Rows.GetRowState(i) & DataGridViewElementStates.Selected) == 0) continue;
                var row = LayDongHienThi(i);
                if (row == null || row.RowState == DataRowState.Deleted || row.RowState == DataRowState.Detached) continue;
                object v = row[ordTen];
                string ten = v as string ?? (v == DBNull.Value ? "" : Convert.ToString(v, CultureInfo.InvariantCulture) ?? "");
                if (string.IsNullOrWhiteSpace(ten)) continue;
                int stt = DocSoNguyen(row[ordStt]);
                if (stt > 0 && daCo.Add(stt)) kq.Add(new DongGanPhanLoai(row, stt, ten.Trim()));
            }
            return kq;
        }
        /// <summary>Gán 1 phân loại cho mọi CBCS đang chọn và lưu ngay. Trả về true nếu đã lưu (hoặc không có gì cần đổi).</summary>
        private async Task<bool> GanPhanLoaiChoCacDongDaChonAsync(string giaTriGoc, string tenHienThi)
        {
            if (_dangGanPhanLoai || _dangXuLyXoa || IsDisposed || Disposing) return false;
            // Nút Lưu (1 người) đang chạy -> không chen vào, kẻo nó ghi đè phân loại vừa gán bằng giá trị cũ đã đọc từ ô
            if (kryptonButton_LuuDataCapNhat != null && !kryptonButton_LuuDataCapNhat.Enabled)
            {
                toolStripStatusLabel1.Text = "Đang lưu dữ liệu, vui lòng thử gán phân loại lại sau giây lát.";
                return false;
            }
            var dt = dtDanhSachGoc;
            var grid = kryptonDataGridView1;
            if (dt == null || grid == null || grid.IsDisposed) return false;
            int ordPl = dt.Columns.IndexOf("PhanLoai");
            if (ordPl < 0)
            {
                MessageBox.Show(this, "Lỗi cấu trúc dữ liệu (thiếu cột Phân loại).", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            var dsChon = LayCacDongDaChonDeGan();
            if (dsChon.Count == 0)
            {
                MessageBox.Show(this, "Vui lòng chọn ít nhất một CBCS.\n(Giữ Ctrl/Shift và bấm chuột để chọn nhiều, Ctrl+A để chọn tất cả)",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            string LayPlRam(DataRow r) => (r[ordPl] as string ?? (r[ordPl] == DBNull.Value ? "" : Convert.ToString(r[ordPl], CultureInfo.InvariantCulture) ?? "")).Trim();
            // Người đã đúng phân loại này thì bỏ qua (không ghi thừa)
            var dsCanGhi = dsChon.Where(d => !string.Equals(LayPlRam(d.Dong), giaTriGoc, StringComparison.Ordinal)).ToList();
            if (dsCanGhi.Count == 0)
            {
                toolStripStatusLabel1.Text = dsChon.Count == 1
                    ? $"{dsChon[0].HoTen} đã có phân loại {tenHienThi}, không cần lưu."
                    : $"Cả {dsChon.Count:N0} CBCS đã chọn đều đã có phân loại {tenHienThi}, không cần lưu.";
                return true;
            }
            // Từ 2 người trở lên: xác nhận 1 lần (Enter = Đồng ý) để tránh bấm nhầm cả nhóm
            if (dsChon.Count > 1)
            {
                const int SO_TEN_HIEN = 10;
                var sb = new StringBuilder();
                foreach (var d in dsCanGhi.Take(SO_TEN_HIEN)) sb.AppendLine($"  • {d.HoTen}");
                if (dsCanGhi.Count > SO_TEN_HIEN) sb.AppendLine($"  ... và {dsCanGhi.Count - SO_TEN_HIEN:N0} người khác");
                int soGiuNguyen = dsChon.Count - dsCanGhi.Count;
                string ghiChuGiu = soGiuNguyen > 0 ? $"\n({soGiuNguyen:N0} người đã có sẵn phân loại này sẽ giữ nguyên)\n" : "";
                var xn = MessageBox.Show(this,
                    $"Gán phân loại \"{tenHienThi}\" cho {dsCanGhi.Count:N0} CBCS:\n\n{sb}{ghiChuGiu}\nLưu ngay vào cơ sở dữ liệu?",
                    "Xác nhận gán phân loại", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                if (xn != DialogResult.Yes) return false;
            }
            string csdlPath = _csdl2Path;
            if (string.IsNullOrWhiteSpace(csdlPath) || !File.Exists(csdlPath))
            {
                MessageBox.Show(this, "Không tìm thấy CSDL2!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            _dangGanPhanLoai = true;
            string? statusCu = toolStripStatusLabel1.Text;
            toolStripStatusLabel1.Text = $"Đang lưu phân loại cho {dsCanGhi.Count:N0} CBCS...";
            UseWaitCursor = true;
            bool dangGiuCong = false;
            bool daGhiCsdl = false;
            bool canNapLai = false;
            bool thanhCong = false;
            string? thongBaoLoi = null;
            try
            {
                await _reloadGate.WaitAsync();
                dangGiuCong = true;
                if (IsDisposed) return false;
                // Trong lúc chờ cổng, bảng có thể đã bị nạp lại / dòng bị xóa -> STT cũ không còn đáng tin: dừng
                int ordSttKt = dt.Columns.IndexOf("STT");
                if (!ReferenceEquals(dt, dtDanhSachGoc) || ordSttKt < 0 ||
                    dsCanGhi.Any(d => d.Dong.RowState == DataRowState.Deleted || d.Dong.RowState == DataRowState.Detached ||
                                      DocSoNguyen(d.Dong[ordSttKt]) != d.Stt)) // STT bị đánh lại (vừa xóa người khác)
                {
                    thongBaoLoi = "Danh sách vừa được làm mới trong lúc lưu. Chưa có thay đổi nào được ghi, vui lòng chọn lại các CBCS.";
                    return false;
                }
                var thamSo = dsCanGhi.Select(d => (d.Stt, d.HoTen)).ToList();
                var kq = await Task.Run(() => ThucThiGanPhanLoaiDb(csdlPath, thamSo, giaTriGoc));
                if (IsDisposed) return false;
                if (kq.LoiKhongKhop != null)
                {
                    thongBaoLoi = kq.LoiKhongKhop;
                    canNapLai = true; // RAM lệch với CSDL -> nạp lại cho khớp
                    return false;
                }
                daGhiCsdl = true;
                DataCache.Clear();
                // Vá RAM: chỉ cột PhanLoai (RAM lưu dạng đã giải mã, giống CapNhatDongTaiCho)
                bool nhieu = dsCanGhi.Count > 50;
                if (nhieu) dt.BeginLoadData();
                try
                {
                    foreach (var d in dsCanGhi) d.Dong[ordPl] = giaTriGoc;
                }
                finally
                {
                    if (nhieu) dt.EndLoadData();
                }
                // Làm tươi chỉ mục lọc: ít dòng -> vá từng dòng; nhiều dòng -> dựng lại 1 lần (rẻ hơn IndexOf lặp)
                if (nhieu || _boLoc == null) _boLoc = null;
                else
                {
                    foreach (var d in dsCanGhi)
                    {
                        int viTri = dt.Rows.IndexOf(d.Dong);
                        if (viTri >= 0) _boLoc.CapNhatDong(dt, viTri);
                    }
                }
                _reloadGate.Release();
                dangGiuCong = false;
                int[]? hienThiTruoc = _hienThi;
                int tongDongTruoc = dt.Rows.Count;
                LoadComboBoxXepLoaiThiDua();
                ApplyFilter();
                // Vùng chọn của lưới ảo giữ theo VỊ TRÍ dòng. Nếu tập dòng hiển thị đổi (vd đang lọc "Loại 1" mà vừa gán
                // sang "Loại 2" -> các dòng đó biến mất) thì vị trí cũ giờ trỏ sang NGƯỜI KHÁC: bỏ chọn hết, kể cả khi chỉ
                // còn 1 dòng chọn, để lần bấm phím tắt tiếp theo không bao giờ gán nhầm người.
                if (!BoLocDanhSach.CungTap(hienThiTruoc, _hienThi, tongDongTruoc) || dt.Rows.Count != tongDongTruoc)
                {
                    grid.CurrentCell = null;
                    grid.ClearSelection();
                }
                grid.Invalidate();
                // Dòng đang hiện ở ô chi tiết nằm trong nhóm vừa gán -> cập nhật ô Phân loại để nút Lưu không ghi đè giá trị cũ
                DongBoOPhanLoaiSauKhiGan(dsCanGhi, giaTriGoc);
                ThongBaoForm4CapNhatLoaiDeXuat();
                toolStripStatusLabel1.Text = dsCanGhi.Count == 1
                    ? $"Đã lưu phân loại {tenHienThi} cho {dsCanGhi[0].HoTen}."
                    : $"Đã lưu phân loại {tenHienThi} cho {dsCanGhi.Count:N0} CBCS.";
                thanhCong = true;
                string tomTat = string.Join(", ", dsCanGhi.Take(20).Select(d => d.HoTen)) + (dsCanGhi.Count > 20 ? ", ..." : "");
                int soLuong = dsCanGhi.Count;
                _ = Task.Run(() =>
                {
                    try
                    {
                        Module_NhatKy.GhiNhatKy(
                            Module_TaiKhoan.TenTaiKhoan_RAM,
                            $"Gán phân loại {tenHienThi} cho {soLuong} CBCS: {tomTat}",
                            DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
                    }
                    catch (Exception ex) { Debug.WriteLine($"Lỗi ghi log: {ex.Message}"); }
                });
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi gán phân loại hàng loạt: {ex}");
                canNapLai = daGhiCsdl;
                thongBaoLoi = daGhiCsdl
                    ? "Phân loại ĐÃ được lưu vào CSDL nhưng cập nhật giao diện bị lỗi, phần mềm sẽ tự làm mới:\n" + ex.Message
                    : "Không lưu được phân loại (chưa có thay đổi nào được ghi):\n" + ex.Message;
                return daGhiCsdl;
            }
            finally
            {
                if (dangGiuCong) _reloadGate.Release();
                _dangGanPhanLoai = false;
                if (!IsDisposed && IsHandleCreated)
                {
                    UseWaitCursor = false;
                    if (!thanhCong && !daGhiCsdl) toolStripStatusLabel1.Text = statusCu;
                    if (thongBaoLoi != null)
                    {
                        // Sau khi đã nhả cổng: nạp lại (nếu cần) rồi mới báo, tránh khóa chéo với Làm mới
                        BeginInvoke(new Action(() =>
                        {
                            if (IsDisposed) return;
                            if (canNapLai)
                            {
                                try { kryptonButton_RefershCSDL.PerformClick(); } catch { /* bỏ qua */ }
                            }
                            MessageBox.Show(this, thongBaoLoi, "Gán phân loại", MessageBoxButtons.OK,
                                canNapLai || daGhiCsdl ? MessageBoxIcon.Warning : MessageBoxIcon.Error);
                        }));
                    }
                }
            }
        }
        private void DongBoOPhanLoaiSauKhiGan(List<DongGanPhanLoai> dsDaGan, string giaTriGoc)
        {
            if (comboBox_PhanLoai == null || comboBox_PhanLoai.IsDisposed) return;
            if (!int.TryParse(textBox_STT.Text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int sttDangXem)) return;
            if (!dsDaGan.Any(d => d.Stt == sttDangXem)) return;
            // Đọc lại từ RAM (đã vá) -> luôn khớp CSDL, kể cả khi giá trị chưa có sẵn trong danh sách ô chọn
            HienThiPhanLoaiCuaDongDangXem();
        }
        // Chạy luồng nền. 1 transaction: (1) kiểm tra mọi STT vẫn đúng người, (2) chỉ ghi cột PhanLoai.
        // Lệch bất kỳ dòng nào -> hoàn tác toàn bộ và trả về lý do (không ném lỗi).
        private KetQuaGanPhanLoaiDb ThucThiGanPhanLoaiDb(string csdlPath, List<(int Stt, string HoTen)> ds, string giaTriGoc)
        {
            // Mã hóa trước, ngoài khóa ghi. Mỗi dòng 1 chuỗi mã riêng (IV/nonce riêng) như khi lưu từng người.
            var banMa = new string[ds.Count];
            for (int i = 0; i < ds.Count; i++)
            {
                string m = Module_BaoMatAES.MaHoa(giaTriGoc);
                if (giaTriGoc.Length > 0 && string.IsNullOrEmpty(m))
                    throw new InvalidOperationException("Không mã hóa được dữ liệu (khóa bảo mật chưa sẵn sàng).");
                banMa[i] = m;
            }
            _khoaGhiCSDL2.Wait();
            try
            {
                using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(csdlPath));
                conn.Open();
                using var tran = conn.BeginTransaction();
                try
                {
                    using (var cmdDoc = new SqliteCommand($"SELECT HoVaTen FROM {Module_TaiKhoan.BangDanhSach} WHERE STT = @STT", conn, tran))
                    {
                        var p = cmdDoc.Parameters.Add("@STT", SqliteType.Integer);
                        foreach (var d in ds)
                        {
                            p.Value = d.Stt;
                            object? v = cmdDoc.ExecuteScalar();
                            string tenDb = v == null || v == DBNull.Value ? "" : GiaiMaSafeCoCache(Convert.ToString(v, CultureInfo.InvariantCulture)).Trim(); // cùng hàm giải mã lúc nạp lưới
                            if (!string.Equals(tenDb, d.HoTen, StringComparison.Ordinal))
                            {
                                tran.Rollback();
                                return new KetQuaGanPhanLoaiDb(0,
                                    $"Dữ liệu trong CSDL đã thay đổi (STT {d.Stt} không còn là \"{d.HoTen}\").\n" +
                                    "Chưa có thay đổi nào được ghi. Danh sách sẽ được làm mới, vui lòng chọn lại.");
                            }
                        }
                    }
                    int soDong = 0;
                    using (var cmdGhi = new SqliteCommand($"UPDATE {Module_TaiKhoan.BangDanhSach} SET PhanLoai = @PhanLoai WHERE STT = @STT", conn, tran))
                    {
                        var pPl = cmdGhi.Parameters.Add("@PhanLoai", SqliteType.Text);
                        var pStt = cmdGhi.Parameters.Add("@STT", SqliteType.Integer);
                        for (int i = 0; i < ds.Count; i++)
                        {
                            pPl.Value = banMa[i];
                            pStt.Value = ds[i].Stt;
                            int n = cmdGhi.ExecuteNonQuery();
                            if (n != 1)
                            {
                                tran.Rollback();
                                return new KetQuaGanPhanLoaiDb(0,
                                    $"STT {ds[i].Stt} không khớp đúng 1 dòng trong CSDL ({n} dòng).\nChưa có thay đổi nào được ghi.");
                            }
                            soDong += n;
                        }
                    }
                    tran.Commit();
                    return new KetQuaGanPhanLoaiDb(soDong, null);
                }
                catch
                {
                    try { tran.Rollback(); } catch { /* transaction đã hỏng/đóng */ }
                    throw;
                }
            }
            finally
            {
                _khoaGhiCSDL2.Release();
            }
        }
        // Xóa nhiều dòng theo STT trong 1 transaction + đánh lại STT đúng 1 lần (chạy luồng nền)
        private KetQuaXoaTatCa ThucThiXoaNhieuDatabase(string csdlPath, List<int> dsStt)
        {
            _khoaGhiCSDL2.Wait();
            try
            {
                using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(csdlPath));
                conn.Open();
                int soDong = 0;
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new SqliteCommand($"DELETE FROM {Module_TaiKhoan.BangDanhSach} WHERE STT = @STT", conn, tran))
                        {
                            var p = cmd.Parameters.Add("@STT", SqliteType.Integer);
                            foreach (int stt in dsStt)
                            {
                                p.Value = stt;
                                soDong += cmd.ExecuteNonQuery();
                            }
                        }
                        if (soDong > 0) DanhLaiSoThuTu(conn, tran);
                        tran.Commit();
                    }
                    catch
                    {
                        try { tran.Rollback(); } catch { /* transaction đã hỏng/đóng */ }
                        throw;
                    }
                }
                bool canNen = false;
                try
                {
                    long freelist = LayGiaTriPragma(conn, "freelist_count");
                    long pageCount = LayGiaTriPragma(conn, "page_count");
                    canNen = pageCount > 0
                             && freelist >= NGUONG_TRANG_TRONG_DE_NEN
                             && (double)freelist / pageCount >= TY_LE_TRANG_TRONG_DE_NEN;
                }
                catch { /* không quan trọng */ }
                return new KetQuaXoaTatCa(soDong, canNen);
            }
            finally
            {
                _khoaGhiCSDL2.Release();
            }
        }
        private static long LayGiaTriPragma(SqliteConnection conn, string ten)
        {
            using var cmd = new SqliteCommand($"PRAGMA {ten};", conn);
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }
        /// <summary>
        /// Nén CSDL (VACUUM) ở nền, không bao giờ chặn UI và không chạy chồng với thao tác xóa khác.
        /// Lỗi (vd. file đang bị tiến trình khác khóa) chỉ ghi Debug, lần xóa sau sẽ thử lại.
        /// </summary>
        private static async Task NenCSDLNenAsync(string csdlPath)
        {
            try
            {
                await Task.Run(() =>
                {
                    // Không chờ lâu: nếu đang có thao tác ghi khác thì bỏ qua lượt này
                    if (!_khoaGhiCSDL2.Wait(TimeSpan.FromSeconds(1))) return;
                    try
                    {
                        using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(csdlPath));
                        conn.Open();
                        using var cmd = new SqliteCommand("VACUUM;", conn);
                        cmd.ExecuteNonQuery();
                    }
                    finally
                    {
                        _khoaGhiCSDL2.Release();
                    }
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi nén CSDL nền: {ex.Message}");
            }
        }
        /// Xóa trắng các ô thông tin chi tiết
        private void ClearThongTin()
        {
            textBox_STT?.Clear();
            textBox_HoVaTen?.Clear();
            textBox_SoHieu?.Clear();
            textBox_NamSinh?.Clear();
            textBox_QueQuan?.Clear();
            textBox_NgayVaoCAND?.Clear();
            textBox_GhiChu?.Clear();
            if (comboBox_ChucVu != null) comboBox_ChucVu.SelectedIndex = -1;
            comboBox_PhanLoai.Text = "";
        }
        // Đánh lại STT = 1..N theo thứ tự STT hiện có.
        // Nhận Transaction từ ngoài vào; nếu truyền null thì tự tạo/commit transaction riêng.
        // Dùng UPDATE ... FROM (SQLite >= 3.33) + chỉ cập nhật dòng có STT thay đổi => O(n log n).
        // Nếu SQLite quá cũ không hỗ trợ UPDATE FROM thì tự động dùng câu lệnh cũ (chậm hơn nhưng đúng).
        private void DanhLaiSoThuTu(SqliteConnection conn, SqliteTransaction? externalTran = null)
        {
            string sqlNhanh = $@"
UPDATE {Module_TaiKhoan.BangDanhSach}
SET STT = o.NewSTT
FROM (SELECT rowid AS rid, ROW_NUMBER() OVER (ORDER BY STT ASC) AS NewSTT FROM {Module_TaiKhoan.BangDanhSach}) AS o
WHERE {Module_TaiKhoan.BangDanhSach}.rowid = o.rid AND {Module_TaiKhoan.BangDanhSach}.STT <> o.NewSTT;";
            string sqlCu = $@"
WITH Ordered AS (
    SELECT rowid, ROW_NUMBER() OVER (ORDER BY STT ASC) AS NewSTT
    FROM {Module_TaiKhoan.BangDanhSach}
)
UPDATE {Module_TaiKhoan.BangDanhSach}
SET STT = (SELECT NewSTT FROM Ordered WHERE Ordered.rowid = {Module_TaiKhoan.BangDanhSach}.rowid);";
            bool isLocalTran = externalTran == null;
            var tran = externalTran ?? conn.BeginTransaction();
            try
            {
                try
                {
                    using var cmd = new SqliteCommand(sqlNhanh, conn, tran);
                    cmd.ExecuteNonQuery();
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 1) // SQLITE_ERROR: cú pháp không được hỗ trợ
                {
                    using var cmd = new SqliteCommand(sqlCu, conn, tran);
                    cmd.ExecuteNonQuery();
                }
                if (isLocalTran) tran.Commit();
            }
            catch
            {
                if (isLocalTran) tran.Rollback();
                throw;
            }
            finally
            {
                if (isLocalTran) tran.Dispose();
            }
        }
        private void kryptonButton_LamMoiCacOTimKiem_Click(object? sender, EventArgs e)
        {
            // Cờ khóa nhằm chặn sự kiện SelectedIndexChanged/TextChanged
            // phát sinh dồn dập trong lúc reset UI (tránh ApplyFilter() bị gọi nhiều lần thừa).
            _isUpdatingCombo = true;
            try
            {
                ResetCacOTimKiem();
            }
            catch (Exception ex)
            {
                // TODO: ghi log thực tế (Serilog/NLog...) thay vì chỉ show message
                MessageBox.Show(
                    $"Lỗi khi làm mới bộ lọc tìm kiếm: {ex.Message}",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // UI chưa sạch thì không nên chạy ApplyFilter tiếp
            }
            finally
            {
                // Đảm bảo cờ khóa LUÔN được nhả — kể cả khi ResetCacOTimKiem() ném exception.
                // Đây là lỗi nghiêm trọng nhất của bản gốc: nếu exception xảy ra giữa
                // dòng "= true" và "= false", cờ sẽ bị kẹt ở true mãi mãi,
                // khiến mọi filter sau đó im lặng không chạy.
                _isUpdatingCombo = false;
            }
            try
            {
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi áp dụng bộ lọc: {ex.Message}",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            textBox_TimKiemTheoTen?.Focus();
        }
        /// <summary>
        /// Đưa toàn bộ ô nhập/combobox tìm kiếm về trạng thái mặc định.
        /// Tách riêng để có thể unit-test và tái sử dụng độc lập với sự kiện Click.
        /// </summary>
        private void ResetCacOTimKiem()
        {
            textBox_TimKiemTheoTen?.Clear();
            if (comboBox_TimKiemDonVi != null)
            {
                comboBox_TimKiemDonVi.SelectedIndex = -1;
                if (comboBox_TimKiemDonVi.Items.Contains(Module_HeThong.Tat_Ca))
                    comboBox_TimKiemDonVi.SelectedItem = Module_HeThong.Tat_Ca;
            }
            if (comboBox_XepLoaiThiDua != null)
            {
                // Chọn phần tử đầu tiên (thường là "Tất cả").
                comboBox_XepLoaiThiDua.SelectedIndex = comboBox_XepLoaiThiDua.Items.Count > 0 ? 0 : -1;
            }
        }
        private bool KiemTraSqliteMoDuoc(string pathDb)
        {
            try
            {
                using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(pathDb));
                conn.Open();
                using var cmd = new SqliteCommand("SELECT 1", conn);
                cmd.ExecuteScalar();
                return true;
            }
            catch
            {
                return false;
            }
        }
        private bool KiemTraFileBiKhoa(string path)
        {
            try
            {
                using (FileStream fs = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.None))
                {
                    // Mở được → không bị khóa
                    return false;
                }
            }
            catch
            {
                // Mở không được → đang bị khóa
                return true;
            }
        }
        private string CheckSQLiteDatabaseStatus(string tenCSDL, string duongDan)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"▶ CSDL: {tenCSDL}");
            // ===== 1. Kiểm tra đường dẫn =====
            if (string.IsNullOrWhiteSpace(duongDan))
            {
                sb.AppendLine("  Trạng thái: Đường dẫn rỗng");
                sb.AppendLine();
                return sb.ToString();
            }
            if (!File.Exists(duongDan))
            {
                sb.AppendLine("  Trạng thái: Không tìm thấy tệp CSDL");
                sb.AppendLine();
                return sb.ToString();
            }
            // ===== 2. Thông tin file =====
            FileInfo fi;
            try
            {
                fi = new FileInfo(duongDan);
                sb.AppendLine($"  Dung lượng: {fi.Length:N0} bytes ({fi.Length / 1024.0:N2} KB)");
                sb.AppendLine($"  Ngày tạo: {fi.CreationTime:dd/MM/yyyy HH:mm:ss}");
                sb.AppendLine($"  Ngày sửa đổi: {fi.LastWriteTime:dd/MM/yyyy HH:mm:ss}");
                sb.AppendLine($"  Truy cập cuối: {fi.LastAccessTime:dd/MM/yyyy HH:mm:ss}");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"  Lỗi đọc thông tin file: {ex.Message}");
                sb.AppendLine();
                return sb.ToString(); // không cố làm tiếp
            }
            // ===== 3. Kiểm tra quyền truy cập (AN TOÀN) =====
            bool coQuyenDoc = false;
            bool coQuyenGhi = false;
            try
            {
                using (File.Open(duongDan, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    coQuyenDoc = true;
                }
            }
            catch { }
            try
            {
                using (File.Open(duongDan, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
                {
                    coQuyenGhi = true;
                }
            }
            catch { }
            sb.AppendLine(coQuyenDoc ? "  Quyền đọc: Có" : "  Quyền đọc: Không");
            sb.AppendLine(coQuyenGhi ? "  Quyền ghi: Có" : "  Quyền ghi: Không");
            // ===== 4. Kiểm tra file có đang bị khóa =====
            bool biKhoa;
            try
            {
                biKhoa = KiemTraFileBiKhoa(duongDan);
            }
            catch
            {
                biKhoa = true; // không xác định → coi như đang bị khóa
            }
            sb.AppendLine(
                biKhoa
                ? "  Trạng thái tệp: Đang bị khóa / đang được sử dụng"
                : "  Trạng thái tệp: Bình thường"
            );
            // ===== 5. Kiểm tra SQLite (READ-ONLY, KHÔNG TÁC ĐỘNG) =====
            bool ketNoiOK = false;
            try
            {
                var cs = new SqliteConnectionStringBuilder
                {
                    DataSource = duongDan,
                    Mode = SqliteOpenMode.ReadOnly,
                    Cache = SqliteCacheMode.Shared
                }.ToString();
                using var cn = new Microsoft.Data.Sqlite.SqliteConnection(cs);
                cn.Open();
                ketNoiOK = true;
                using var cmd = cn.CreateCommand();
                cmd.CommandText = "PRAGMA user_version;";
                var version = cmd.ExecuteScalar();
                sb.AppendLine($"  Phiên bản CSDL: {version}");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"  Lỗi SQLite: {ex.Message}");
            }
            sb.AppendLine(
                ketNoiOK
                ? "  Trạng thái kết nối: OK"
                : "  Trạng thái kết nối: Không thể kết nối"
            );
            sb.AppendLine();
            return sb.ToString();
        }
        private async void kryptonButton_LuuDataCapNhat_Click(object? sender, EventArgs e)
        {
            // Đang gán phân loại hàng loạt -> chờ xong (tránh ghi đè phân loại vừa gán bằng giá trị cũ trên ô)
            if (_dangGanPhanLoai)
            {
                toolStripStatusLabel1.Text = "Đang lưu phân loại cho nhóm CBCS đã chọn, vui lòng bấm Lưu lại sau giây lát.";
                return;
            }
            // 1. VALIDATE DỮ LIỆU ĐẦU VÀO CƠ BẢN
            if (textBox_STT == null || kryptonDataGridView1 == null || dtDanhSachGoc == null)
            {
                MessageBox.Show("Hệ thống chưa sẵn sàng để lưu dữ liệu.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string stt = textBox_STT.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(stt) || !int.TryParse(stt, out int sttSo))
            {
                MessageBox.Show("STT không hợp lệ. Vui lòng chọn đúng dòng cần lưu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!kryptonDataGridView1.Columns.Contains("STT") || !dtDanhSachGoc.Columns.Contains("STT"))
            {
                MessageBox.Show("Lỗi cấu trúc lưới dữ liệu (Thiếu cột STT).", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string csdlPath = _csdl2Path;
            if (string.IsNullOrWhiteSpace(csdlPath) || !System.IO.File.Exists(csdlPath))
            {
                MessageBox.Show("Không tìm thấy CSDL2!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // 2. LẤY DỮ LIỆU VÀ KIỂM TRA RÀNG BUỘC RỖNG
            string hoVaTenTxt = textBox_HoVaTen.Text.Trim();
            string soHieuTxt = textBox_SoHieu.Text.Trim();
            string namSinhTxt = textBox_NamSinh.Text.Trim();
            string queQuanTxt = textBox_QueQuan.Text.Trim();
            string ngayVaoCANDTxt = textBox_NgayVaoCAND.Text.Trim();
            string capBacTxt = textBox_CapBac.Text.Trim();
            string donViTxt = textBox_DonVi.Text.Trim();
            string ghiChuTxt = textBox_GhiChu.Text.Trim();
            string chucVuLuu = string.IsNullOrEmpty(comboBox_ChucVu.Text.Trim())
                                ? (kryptonDataGridView1.CurrentRow?.Cells["ChucVu"].Value?.ToString() ?? "")
                                : comboBox_ChucVu.Text.Trim();
            // 🎯 CHUYỂN ĐỔI CHUẨN XÁC: Lấy GIÁ TRỊ GỐC (Loại 1, Loại 2, ...) để lưu DB
            string phanLoaiGoc = "";
            if (comboBox_PhanLoai.SelectedItem is ComboboxItem itemPL)
            {
                phanLoaiGoc = itemPL.Value; // Lấy đúng Value gốc ("Loại 1", "Loại 2",...) nếu chọn từ danh sách
            }
            else
            {
                // Nhập tay hoặc Text thuần: Chỉ ánh xạ duy nhất các mã viết tắt gọn
                string textPL = comboBox_PhanLoai.Text.Trim();
                phanLoaiGoc = textPL switch
                {
                    Module_HeThong.PL_CSTD => Module_HeThong.Loai_1,
                    Module_HeThong.PL_CSTT => Module_HeThong.Loai_2,
                    Module_HeThong.PL_HTNV => Module_HeThong.Loai_3,
                    Module_HeThong.PL_KHTNV => Module_HeThong.Loai_4,
                    _ => textPL // Giữ nguyên các chuỗi khác (ví dụ: "Không PL", "Loại 1",...)
                };
            }
            List<string> thieuThongTin = new List<string>();
            if (string.IsNullOrEmpty(hoVaTenTxt)) thieuThongTin.Add("- Họ và tên");
            if (string.IsNullOrEmpty(soHieuTxt)) thieuThongTin.Add("- Số hiệu");
            if (string.IsNullOrEmpty(namSinhTxt)) thieuThongTin.Add("- Năm sinh");
            if (string.IsNullOrEmpty(queQuanTxt)) thieuThongTin.Add("- Quê quán");
            if (string.IsNullOrEmpty(ngayVaoCANDTxt)) thieuThongTin.Add("- Ngày vào CAND");
            if (string.IsNullOrEmpty(capBacTxt)) thieuThongTin.Add("- Cấp bậc");
            if (string.IsNullOrEmpty(donViTxt)) thieuThongTin.Add("- Đơn vị");
            if (string.IsNullOrEmpty(chucVuLuu)) thieuThongTin.Add("- Chức vụ");
            if (thieuThongTin.Count > 0)
            {
                MessageBox.Show("Vui lòng điền đầy đủ các thông tin bắt buộc sau:\n\n" + string.Join("\n", thieuThongTin),
                                "Cảnh báo thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string textBanDau = kryptonButton_LuuDataCapNhat.Values.Text;
            Image? anhBanDau = kryptonButton_LuuDataCapNhat.Values.Image;
            bool dangGiuCong = false;  // true khi đang giữ _reloadGate (chỉ được nhả đúng 1 lần)
            bool daGhiCsdl = false;    // true khi lệnh UPDATE đã commit xong
            try
            {
                kryptonButton_LuuDataCapNhat.Enabled = false;
                kryptonButton_LuuDataCapNhat.Values.Text = "Đang lưu...";
                kryptonButton_LuuDataCapNhat.Values.Image = null;
                // Mã hóa trên luồng nền (đúng GIÁ TRỊ GỐC phanLoaiGoc). Trong lúc await, UI vẫn tự vẽ lại
                // chữ "Đang lưu..." nên không cần Task.Delay(50) như trước.
                var banGhi = await Task.Run(() => new BanGhiDaMaHoa
                {
                    HoVaTen = Module_BaoMatAES.MaHoa(hoVaTenTxt),
                    SoHieu = Module_BaoMatAES.MaHoa(soHieuTxt),
                    NamSinh = Module_BaoMatAES.MaHoa(namSinhTxt),
                    QueQuan = Module_BaoMatAES.MaHoa(queQuanTxt),
                    NgayVaoCAND = Module_BaoMatAES.MaHoa(ngayVaoCANDTxt),
                    CapBac = Module_BaoMatAES.MaHoa(capBacTxt),
                    ChucVu = Module_BaoMatAES.MaHoa(chucVuLuu),
                    DonVi = Module_BaoMatAES.MaHoa(donViTxt),
                    PhanLoai = Module_BaoMatAES.MaHoa(phanLoaiGoc), // 🔒 Mã hóa "Loại 1", "Loại 2"... đúng chuẩn CSDL
                    GhiChu = Module_BaoMatAES.MaHoa(ghiChuTxt)
                });
                if (this.IsDisposed) return;
                // Giữ cổng để ReloadDuLieu() (nút Làm mới, đổi chế độ xét thi đua...) không chen vào
                // giữa lúc "ghi CSDL" và "cập nhật RAM" -> tránh vá nhầm vào bảng đã bị thay.
                await _reloadGate.WaitAsync();
                dangGiuCong = true;
                if (this.IsDisposed) return;
                // 3. LƯU DATABASE
                if (!await LuuDongVaoCsdlAsync(csdlPath, sttSo, banGhi))
                {
                    MessageBox.Show("Không tìm thấy dữ liệu để cập nhật.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                daGhiCsdl = true;
                DataCache.Clear(); // các form khác (Form4/15/34...) sẽ tự đọc lại dữ liệu mới khi cần
                // 4. CẬP NHẬT TẠI CHỖ: chỉ sửa đúng 1 dòng trong RAM, KHÔNG dựng lại lưới
                DataRow? dongDaVa = CapNhatDongTaiCho(sttSo, hoVaTenTxt, donViTxt, phanLoaiGoc, banGhi,
                                                      out bool donViDoi, out bool phanLoaiDoi);
                _reloadGate.Release();
                dangGiuCong = false;
                string thongBao = "Đã lưu thay đổi thành công!";
                if (dongDaVa != null)
                {
                    if (donViDoi || phanLoaiDoi)
                    {
                        // Có thể xuất hiện đơn vị / phân loại mới (hoặc cái cũ hết người) -> nạp lại danh sách
                        // của 2 ô lọc, giữ nguyên mục đang chọn nếu nó vẫn còn.
                        LoadComboBoxDonVi();
                        LoadComboBoxXepLoaiThiDua();
                    }
                    // Áp lại bộ lọc hiện tại (chỉ đổi RowFilter khi thật sự khác) + cập nhật thống kê.
                    // Không đụng tới dòng đang chọn, vị trí cuộn, nội dung ô tìm kiếm, các ô thông tin bên dưới.
                    ApplyFilter();
                    if (TimViTriTrongLuoi(dongDaVa) < 0)
                    {
                        // Dòng vừa sửa không còn khớp bộ lọc hiện tại (vd: đổi đơn vị / phân loại).
                        // Bỏ chọn để lưới không trỏ nhầm sang người khác.
                        kryptonDataGridView1.CurrentCell = null;
                        kryptonDataGridView1.ClearSelection();
                        thongBao = "Đã lưu thành công! Dòng vừa sửa không còn nằm trong bộ lọc hiện tại, hãy bỏ lọc để xem lại.";
                    }
                    ThongBaoForm15CapNhatGhiChu();
                }
                else
                {
                    // Dự phòng (hiếm gặp): không vá được dòng trong RAM -> nạp lại toàn bộ như cách cũ để dữ liệu luôn đúng
                    await ReloadDuLieu();
                    ApplyFilter();
                    CapNhatThongKeToanBoQuanSo();
                }
                ThongBaoForm4CapNhatLoaiDeXuat();
                toolStripStatusLabel1.Text = thongBao;
            }
            catch (Exception ex)
            {
                string tienTo = daGhiCsdl
                    ? "Dữ liệu ĐÃ được lưu vào CSDL nhưng có lỗi khi cập nhật giao diện (hãy bấm Làm mới):\n"
                    : "Lỗi khi lưu dữ liệu:\n";
                MessageBox.Show(tienTo + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (dangGiuCong) _reloadGate.Release();
                if (!this.IsDisposed)
                {
                    kryptonButton_LuuDataCapNhat.Values.Text = textBanDau;
                    kryptonButton_LuuDataCapNhat.Values.Image = anhBanDau;
                    kryptonButton_LuuDataCapNhat.Enabled = true;
                }
            }
        }
        // Gói 10 trường đã mã hóa (đúng dạng đang nằm trong CSDL2) để truyền giữa các hàm
        private sealed class BanGhiDaMaHoa
        {
            public string HoVaTen { get; set; } = "";
            public string SoHieu { get; set; } = "";
            public string NamSinh { get; set; } = "";
            public string QueQuan { get; set; } = "";
            public string NgayVaoCAND { get; set; } = "";
            public string CapBac { get; set; } = "";
            public string ChucVu { get; set; } = "";
            public string DonVi { get; set; } = "";
            public string PhanLoai { get; set; } = "";
            public string GhiChu { get; set; } = "";
        }
        // Ghi 1 dòng vào CSDL2 trong 1 transaction. Trả về false nếu không có dòng nào bị ảnh hưởng.
        private static async Task<bool> LuuDongVaoCsdlAsync(string csdlPath, int stt, BanGhiDaMaHoa b)
        {
            using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(csdlPath));
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();
            string sql = $@"UPDATE {Module_TaiKhoan.BangDanhSach} SET HoVaTen = @HoVaTen, SoHieu = @SoHieu, NamSinh = @NamSinh, QueQuan = @QueQuan, NgayVaoCAND = @NgayVaoCAND, CapBac = @CapBac, ChucVu = @ChucVu, DonVi = @DonVi, PhanLoai = @PhanLoai, GhiChu = @GhiChu WHERE STT = @STT";
            using var cmd = new SqliteCommand(sql, conn, transaction);
            cmd.CommandTimeout = 5;
            cmd.Parameters.Add(new SqliteParameter("@HoVaTen", b.HoVaTen));
            cmd.Parameters.Add(new SqliteParameter("@SoHieu", b.SoHieu));
            cmd.Parameters.Add(new SqliteParameter("@NamSinh", b.NamSinh));
            cmd.Parameters.Add(new SqliteParameter("@QueQuan", b.QueQuan));
            cmd.Parameters.Add(new SqliteParameter("@NgayVaoCAND", b.NgayVaoCAND));
            cmd.Parameters.Add(new SqliteParameter("@CapBac", b.CapBac));
            cmd.Parameters.Add(new SqliteParameter("@ChucVu", b.ChucVu));
            cmd.Parameters.Add(new SqliteParameter("@DonVi", b.DonVi));
            cmd.Parameters.Add(new SqliteParameter("@PhanLoai", b.PhanLoai));
            cmd.Parameters.Add(new SqliteParameter("@GhiChu", b.GhiChu));
            cmd.Parameters.Add(new SqliteParameter("@STT", stt));
            if (await cmd.ExecuteNonQueryAsync() <= 0)
            {
                transaction.Rollback();
                return false;
            }
            transaction.Commit();
            return true;
        }
        // Vá đúng 1 dòng trong dtDanhSachGoc theo STT (chạy trên luồng UI).
        // Quy ước cột trong RAM (giống lúc nạp): HoVaTen/DonVi/PhanLoai là chữ thường (đã giải mã),
        // các cột còn lại giữ dạng mã hóa và được giải mã lười khi vẽ ô.
        // Trả về dòng đã vá, hoặc null nếu không vá được (khi đó hàm gọi sẽ nạp lại toàn bộ cho chắc).
        private DataRow? CapNhatDongTaiCho(int stt, string hoVaTen, string donVi, string phanLoai,
                                           BanGhiDaMaHoa b, out bool donViDoi, out bool phanLoaiDoi)
        {
            donViDoi = false;
            phanLoaiDoi = false;
            try
            {
                DataTable? dt = dtDanhSachGoc;
                if (dt == null || kryptonDataGridView1 == null || kryptonDataGridView1.IsDisposed) return null;
                int oStt = dt.Columns.IndexOf("STT");
                int oDonVi = dt.Columns.IndexOf("DonVi");
                int oPhanLoai = dt.Columns.IndexOf("PhanLoai");
                if (oStt < 0) return null;
                DataRow? dong = null;
                // Đường nhanh: dòng đang được chọn trên lưới thường chính là dòng cần vá
                int viTriDangChon = kryptonDataGridView1.CurrentCell?.RowIndex ?? -1;
                DataRow? dongDangChon = LayDongHienThi(viTriDangChon);
                if (dongDangChon != null && CungSTT(dongDangChon, oStt, stt)) dong = dongDangChon;
                // Đường dự phòng: người dùng vừa bấm sang dòng khác trong lúc đang lưu -> tìm theo STT
                if (dong == null)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        if (r.RowState != DataRowState.Deleted && CungSTT(r, oStt, stt))
                        {
                            dong = r;
                            break;
                        }
                    }
                }
                if (dong == null) return null;
                string donViCu = oDonVi >= 0 ? (dong[oDonVi]?.ToString() ?? "") : "";
                string phanLoaiCu = oPhanLoai >= 0 ? (dong[oPhanLoai]?.ToString() ?? "") : "";
                donViDoi = !string.Equals(donViCu, donVi, StringComparison.Ordinal);
                phanLoaiDoi = !string.Equals(phanLoaiCu, phanLoai, StringComparison.Ordinal);
                // BeginEdit/EndEdit: DataView chỉ tính lại bộ lọc 1 lần cho cả dòng
                dong.BeginEdit();
                try
                {
                    GanChuoi(dong, "HoVaTen", hoVaTen);
                    GanChuoi(dong, "DonVi", donVi);
                    GanChuoi(dong, "PhanLoai", phanLoai);
                    GanChuoi(dong, "SoHieu", b.SoHieu);
                    GanChuoi(dong, "NamSinh", b.NamSinh);
                    GanChuoi(dong, "QueQuan", b.QueQuan);
                    GanChuoi(dong, "NgayVaoCAND", b.NgayVaoCAND);
                    GanChuoi(dong, "CapBac", b.CapBac);
                    GanChuoi(dong, "ChucVu", b.ChucVu);
                    GanChuoi(dong, "GhiChu", b.GhiChu);
                    dong.EndEdit();
                }
                catch
                {
                    dong.CancelEdit();
                    throw;
                }
                // Làm tươi chỉ mục lọc của đúng dòng vừa sửa (tên / đơn vị / phân loại có thể đã đổi)
                int viTriNguon = dt.Rows.IndexOf(dong);
                if (viTriNguon >= 0) _boLoc?.CapNhatDong(dt, viTriNguon);
                return dong;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[CapNhatDongTaiCho] " + ex.Message);
                return null;
            }
        }
        private static void GanChuoi(DataRow r, string tenCot, string giaTri)
        {
            int o = r.Table.Columns.IndexOf(tenCot);
            if (o >= 0) r[o] = giaTri;
        }
        private static bool CungSTT(DataRow r, int ordStt, int stt)
        {
            if (r.RowState == DataRowState.Deleted || ordStt < 0) return false;
            object v = r[ordStt];
            if (v == DBNull.Value) return false; // dòng đệm ảo cuối bảng không có STT
            return long.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture),
                                 NumberStyles.Integer, CultureInfo.InvariantCulture, out long n) && n == stt;
        }
        // Vị trí hiện tại của dòng trong lưới (đã lọc); -1 nếu dòng đang bị bộ lọc ẩn đi
        private int TimViTriTrongLuoi(DataRow dong)
        {
            var dt = dtDanhSachGoc;
            if (dt == null || dong == null) return -1;
            int cur = kryptonDataGridView1.CurrentCell?.RowIndex ?? -1;
            if (ReferenceEquals(LayDongHienThi(cur), dong)) return cur;
            int nguon = dt.Rows.IndexOf(dong);
            if (nguon < 0) return -1;
            return BoLocDanhSach.ViTriCuaNguon(_hienThi, nguon, dt.Rows.Count);
        }
        private void lamMoiHeThong_Click(object? sender, EventArgs e)
        {
            kryptonButton_RefershCSDL.PerformClick();
        }
        private void kiemTraKetNoi_Click(object? sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Đang kiểm tra cơ sở dữ liệu...";
            try
            {
                string csdl2Path = _csdl2Path;
                if (string.IsNullOrWhiteSpace(csdl2Path))
                    throw new Exception("Đường dẫn CSDL2 không hợp lệ.");
                // Lấy chuỗi kết quả từ hàm xử lý lõi của bạn
                string rawResult = CheckSQLiteDatabaseStatus("", csdl2Path);
                // Gọi hàm Render Form ảo để hiển thị (Truyền tiêu đề in hoa cho đẹp)
                HienThiFormAo_KiemTraCSDL("CHI TIẾT TRẠNG THÁI CSDL 2", rawResult);
                toolStripStatusLabel1.Text = "Hoàn tất kiểm tra cơ sở dữ liệu.";
            }
            catch (Exception ex)
            {
                // Tái sử dụng form để hiển thị lỗi
                HienThiFormAo_KiemTraCSDL("LỖI KIỂM TRA CSDL", $"Lỗi hệ thống:\n{ex.Message}");
                toolStripStatusLabel1.Text = "Lỗi kiểm tra CSDL!";
                //// Ghi nhật ký thành công
                Module_NhatKy.GhiNhatKy(
                    taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                    hanhDong: "Kiểm tra kết nối CSDL 1 và CSDL 2 thất bại",
                    ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
                System.Diagnostics.Debug.WriteLine($"[kiemTraKetNoi_Click Lỗi] {ex.Message}");
            }
        }
        private void HienThiFormAo_KiemTraCSDL(string tieuDe, string rawData)
        {
            // ⭐ SỬ DỤNG LỚP CƠ SỞ FORMAOBASE
            using (var formAo = new FormAoBase())
            {
                formAo.Text = "Trạng thái Cơ sở dữ liệu";
                formAo.Size = new System.Drawing.Size(790, 590); // Nới form rộng ra chút để lưới Grid không bị ép
                formAo.FormBorderStyle = FormBorderStyle.FixedDialog;
                formAo.MaximizeBox = false;
                formAo.MinimizeBox = false;
                formAo.ShowIcon = false;
                formAo.ShowInTaskbar = false;
                // --- 1. PANEL TIÊU ĐỀ ---
                var panelTop = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(25, 20, 20, 5) };
                panelTop.StateCommon.Color1 = System.Drawing.Color.White;
                var lblTitle = new Krypton.Toolkit.KryptonLabel { Text = tieuDe.ToUpper(), Dock = DockStyle.Fill, AutoSize = false };
                lblTitle.StateCommon.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 11.5F, System.Drawing.FontStyle.Bold);
                lblTitle.StateCommon.ShortText.Color1 = System.Drawing.Color.FromArgb(0, 82, 155); // Xanh Navy
                panelTop.Controls.Add(lblTitle);
                // --- 2. ĐƯỜNG KẺ NGANG (Separator) ---
                var separator = new Label { Height = 1, Dock = DockStyle.Top, BackColor = System.Drawing.Color.FromArgb(220, 220, 220), Margin = new Padding(0, 0, 0, 10) };
                // --- 3. PANEL CHỨA LƯỚI ---
                var panelContent = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Fill, Padding = new Padding(25, 10, 25, 15) };
                panelContent.StateCommon.Color1 = System.Drawing.Color.White;
                // --- 4. TẠO GRID HIỆN ĐẠI ---
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
                // Tinh chỉnh giao diện lưới
                grid.GridStyles.Style = Krypton.Toolkit.DataGridViewStyle.List;
                grid.StateCommon.Background.Color1 = System.Drawing.Color.White;
                grid.StateCommon.DataCell.Content.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 10F, System.Drawing.FontStyle.Regular);
                grid.StateCommon.HeaderColumn.Content.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 10F, System.Drawing.FontStyle.Bold);
                grid.Columns.Add("TieuChi", "Chỉ số kiểm tra");
                grid.Columns.Add("TrangThai", "Trạng thái / Giá trị");
                grid.Columns[0].FillWeight = 45;
                grid.Columns[1].FillWeight = 55;
                grid.Columns[0].DefaultCellStyle.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 10F, System.Drawing.FontStyle.Bold);
                grid.Columns[0].DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(60, 60, 60);
                // --- 5. PHÂN TÍCH CHUỖI VÀ ĐỔ VÀO GRID ---
                string[] lines = rawData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    string cleanLine = line.Trim();
                    if (string.IsNullOrEmpty(cleanLine)) continue;
                    // Xử lý dòng tiêu đề chính ("▶ CSDL: ...")
                    if (cleanLine.StartsWith("▶"))
                    {
                        int rHeader = grid.Rows.Add(cleanLine, "");
                        grid.Rows[rHeader].DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(240, 248, 255); // Nền xanh nhạt
                        grid.Rows[rHeader].DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(0, 82, 155); // Chữ xanh Navy
                        continue;
                    }
                    // Cắt chuỗi theo dấu ":"
                    int splitIndex = cleanLine.IndexOf(':');
                    if (splitIndex > 0)
                    {
                        string key = cleanLine.Substring(0, splitIndex).Trim();
                        string val = cleanLine.Substring(splitIndex + 1).Trim();
                        int rIdx = grid.Rows.Add(key, val);
                        var cellVal = grid.Rows[rIdx].Cells[1];
                        // CONDITIONAL FORMATTING (Tô màu tự động chuẩn Enterprise)
                        string valLower = val.ToLower();
                        if (valLower.Contains("ok") || valLower.Contains("bình thường") || valLower == "có")
                        {
                            cellVal.Style.ForeColor = System.Drawing.Color.FromArgb(34, 139, 34); // Forest Green (Xanh lá trầm)
                            cellVal.Style.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 10F, System.Drawing.FontStyle.Bold);
                        }
                        else if (valLower.Contains("không") || valLower.Contains("lỗi") || valLower.Contains("bị khóa"))
                        {
                            cellVal.Style.ForeColor = System.Drawing.Color.FromArgb(211, 47, 47); // Red (Đỏ đô)
                            cellVal.Style.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 10F, System.Drawing.FontStyle.Bold);
                        }
                        else
                        {
                            cellVal.Style.ForeColor = System.Drawing.Color.FromArgb(45, 45, 45); // Chữ thường màu xám đậm
                        }
                    }
                    else
                    {
                        // Dòng text tự do (như thông báo lỗi ngoại lệ)
                        grid.Rows.Add("Thông tin", cleanLine);
                    }
                }
                // --- 6. PANEL CHỨA NÚT ĐÓNG ---
                var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 65, BackColor = System.Drawing.Color.WhiteSmoke };
                var btnClose = new Krypton.Toolkit.KryptonButton { Text = "Đóng", Width = 120, Height = 38, DialogResult = DialogResult.OK };
                btnClose.StateCommon.Content.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 9.5F, System.Drawing.FontStyle.Bold);
                btnClose.StateCommon.Border.Rounding = 5;
                btnClose.Location = new System.Drawing.Point((formAo.Width - btnClose.Width) / 2, 13);
                panelBottom.Controls.Add(btnClose);
                // --- 7. RÁP LAYER VÀO FORM ---
                panelContent.Controls.Add(grid);
                formAo.Controls.Add(panelContent);
                formAo.Controls.Add(separator);
                formAo.Controls.Add(panelTop);
                formAo.Controls.Add(panelBottom);
                separator.BringToFront();
                panelContent.BringToFront();
                formAo.AcceptButton = btnClose;
                formAo.CancelButton = btnClose;
                // Tự động bỏ chọn dòng đầu tiên để UI gọn gàng khi mở lên
                formAo.Shown += (s, ev) => grid.ClearSelection();
                formAo.ShowDialog(this);
            }
        }
        private void xoaTimKiem_Click(object? sender, EventArgs e)
        {
            kryptonButton_LamMoiCacOTimKiem.PerformClick();
        }
        public void RefreshCSDL()
        {
            // Nội dung mà kryptonButton_RefershCSDL_Click thực hiện/ nhận tín hiệu truyền từ form16
            kryptonButton_RefershCSDL.PerformClick();
        }
        private void xuatDuLieuSangThongKe_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!KiemTraDuLieuSanSang("xuất dữ liệu sang thống kê")) return;
            // BƯỚC 1: KIỂM TRA XEM CSDL CÓ DỮ LIỆU KHÔNG
            if (dtDanhSachGoc == null) return;
            // 2. Lọc dữ liệu thực tế và ép kiểu sang List ngay
            var dsThucTe = dtDanhSachGoc.AsEnumerable()
                .Where(r => !string.IsNullOrWhiteSpace(r.Field<string>("HoVaTen")))
                .ToList();
            // BƯỚC 2: KIỂM TRA XEM ĐÃ CÓ AI ĐƯỢC PHÂN LOẠI CHƯA
            // Tạo tập hợp các giá trị hợp lệ
            var cacLoaiHopLe = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Module_HeThong.Loai_1, Module_HeThong.Loai_2, Module_HeThong.Loai_3, Module_HeThong.Loai_4, Module_HeThong.PL_KHONG_PL
            };
            // Dùng LINQ Any để quét siêu tốc: Chỉ cần TÌM THẤY ÍT NHẤT 1 NGƯỜI có Phân loại hợp lệ là true
            bool coDuLieuPhanLoai = dsThucTe.Any(r =>
            {
                string phanLoai = r.Field<string>("PhanLoai")?.Trim() ?? "";
                return cacLoaiHopLe.Contains(phanLoai);
            });
            if (!coDuLieuPhanLoai)
            {
                MessageBox.Show($"Chưa có {Module_HeThong.Tu_dong_chi} nào được đánh giá Phân loại (Loại 1, 2, 3, 4 hoặc Không PL).\nVui lòng cập nhật kết quả phân loại trước khi xuất sang Thống kê!",
                    "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // BƯỚC 3: QUA ĐƯỢC BÀI KIỂM TRA THÌ MỚI MỞ FORM 17
            // Sử dụng khối using để tự động giải phóng bộ nhớ khi Form 17 đóng
            using (Form17_XuatDataSangTK frm17 = new Form17_XuatDataSangTK())
            {
                // Hiển thị form dưới dạng modal (ngăn người dùng thao tác form khác)
                frm17.ShowDialog(this); // Thêm (this) để đảm bảo Form 17 luôn đè lên đúng giữa Form 6
            }
            // <--- frm17 sẽ tự động bị hủy (Dispose) và trả lại RAM tại đây
            // Form 15 (nếu đang mở) phải nạp lại, nếu không lưới vẫn hiện dữ liệu cũ
            YeuCauForm15DongBoLai();
        }
        private void YeuCauForm15DongBoLai()
        {
            if (IsDisposed || !IsHandleCreated) return;
            try
            {
                var form15 = Application.OpenForms.OfType<Form15_ThongKeThiDua>().FirstOrDefault(f => !f.IsDisposed);
                if (form15 == null)
                {
                    var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
                    var panel = formCha?.Controls.Find("PanelContainer", true).FirstOrDefault() as Panel;
                    form15 = panel?.Controls.OfType<Form15_ThongKeThiDua>().FirstOrDefault(f => !f.IsDisposed);
                }
                if (form15 != null) _ = form15.LamMoiDuLieuAsync(); // đồng bộ + nạp lại + lọc 1 lần (tự chống chạy chồng)
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi báo Form15 nạp lại sau khi xuất: " + ex.Message);
            }
        }
        private void nhapKetQuaPhanLoaiTuDonVi_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!KiemTraDuLieuSanSang("nhập kết quả phân loại từ đơn vị")) return;
            // 2. Lọc dữ liệu thực tế và ép kiểu sang List ngay
            var dsThucTe = dtDanhSachGoc!.AsEnumerable()
        .Where(r => !string.IsNullOrWhiteSpace(r.Field<string>("HoVaTen")))
        .ToList();
            // 3. Chỉ cần 1 cái if này là đủ cho mọi trường hợp trống
            if (dsThucTe.Count == 0)
            {
                MessageBox.Show("Cơ sở dữ liệu trống hoặc không có thông tin CBCS hợp lệ để xử lý!",
                    "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var f19 = Application.OpenForms
                .OfType<Form19_NapKQPLTuDonVi>()
                .FirstOrDefault();
            if (f19 == null)
            {
                f19 = new Form19_NapKQPLTuDonVi(this); // Truyền Form6
                f19.Show();
            }
            else
            {
                f19.BringToFront();
                f19.WindowState = FormWindowState.Normal;
            }
        }
        //public async void RefershCSDL_TuFormKhac()
        //{
            //// Cực kỳ quan trọng: Xóa cache để ép hệ thống đọc lại từ DB (nếu form con có thay đổi dữ liệu)
            //DataCache.Clear();
            //// Gọi trực tiếp hàm ReloadDuLieu (không thông qua nút bấm)
            //await ReloadDuLieu();
            //// Đảm bảo thống kê được cập nhật dựa trên dữ liệu mới
            //CapNhatThongKeToanBoQuanSo();
        //}
        private bool IsFileLocked(string filePath)
        {
            try
            {
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException) { return true; }
            return false;
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Instance = this;
            LenLichChuanBiForm60(1500); // dựng sẵn khung form 60 lúc rảnh để nút "Thêm CBCS/tân binh" mở tức thì
        }
        // NÚT MENU GỐC: Gọi luồng xuất bình thường
        private void xuatDuLieuThiDuaRaTepExcel_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            Module_XuatNhapDuLieuThiDua.ThucThiXuatExcel(this, false, dtDanhSachGoc);
        }
        // ===== MỞ FORM 60 TỨC THÌ =====
        // Ý tưởng: dựng sẵn khung form 60 (ẩn) lúc ứng dụng rảnh -> bấm nút chỉ còn Show().
        // Dữ liệu KHÔNG nạp sẵn: Form60 tự nạp nền ngay khi hiện (luôn mới nhất, không để bản sao dữ liệu thô nằm sẵn).
        private Panel? LayPanelContainerForm60(out Form2_FormCha? formCha)
        {
            formCha = this.ParentForm as Form2_FormCha ?? Application.OpenForms["Form2_FormCha"] as Form2_FormCha;
            if (formCha == null) return null;
            return formCha.Controls["PanelContainer"] as Panel
                   ?? formCha.Controls.Find("PanelContainer", false).FirstOrDefault() as Panel;
        }
        private static Form60_NhapLieuCBCS? TimForm60TrongPanel(Panel panel)
        {
            for (int i = 0; i < panel.Controls.Count; i++)
            {
                if (panel.Controls[i] is Form60_NhapLieuCBCS f60) return f60;
            }
            return null;
        }
        /// <summary>Tạo form 60 ở trạng thái ẨN trong panel và dựng sẵn giao diện của nó.</summary>
        private Form60_NhapLieuCBCS TaoForm60An(Panel panel)
        {
            var f = new Form60_NhapLieuCBCS
            {
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill,
                Text = @"Nhập liệu CBCS"
            };
            // Đăng ký Event bằng phương thức tách biệt để TRÁNH Memory Leak
            f.FormClosed += Form60_OnFormClosed;
            panel.Controls.Add(f); // đang ẩn nên chưa vẽ gì cả
            return f;
        }
        private void ChuanBiTruocForm60()
        {
            try
            {
                if (this.IsDisposed || !this.IsHandleCreated) return;
                var panel = LayPanelContainerForm60(out _);
                if (panel == null || panel.IsDisposed) return;
                if (TimForm60TrongPanel(panel) != null) return; // đã có sẵn
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var f = TaoForm60An(panel);
                // Tạo handle của các control theo từng phần nhỏ (mỗi lần <= 10 ms) để không làm khựng người dùng
                int soLan = 0;
                var tm = new System.Windows.Forms.Timer { Interval = 30 };
                tm.Tick += (s, ev) =>
                {
                    bool conViec = false;
                    try { conViec = !f.IsDisposed && !f.Visible && f.ChuanBiGiaoDienTungPhan(10); }
                    catch (Exception ex2) { System.Diagnostics.Debug.WriteLine("[ChuanBiGiaoDienTungPhan] " + ex2.Message); }
                    soLan++;
                    if (!conViec)
                    {
                        tm.Stop();
                        tm.Dispose();
                    }
                };
                tm.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[ChuanBiTruocForm60] " + ex.Message);
            }
        }
        /// <summary>Hẹn giờ rồi đợi ứng dụng rảnh mới dựng sẵn form 60 (không làm khựng thao tác của người dùng).</summary>
        private void LenLichChuanBiForm60(int doTreMs)
        {
            var timer = new System.Windows.Forms.Timer { Interval = Math.Max(50, doTreMs) };
            timer.Tick += (s, ev) =>
            {
                timer.Stop();
                timer.Dispose();
                if (this.IsDisposed) return;
                EventHandler? khiRanh = null;
                khiRanh = (s2, ev2) =>
                {
                    Application.Idle -= khiRanh;
                    if (!this.IsDisposed) ChuanBiTruocForm60();
                };
                Application.Idle += khiRanh;
            };
            timer.Start();
        }
        private void kryptonButton1_HuongDanThemDuLieu_Click(object? sender, EventArgs e)
        {
            var swBam = System.Diagnostics.Stopwatch.StartNew();
            var panel = LayPanelContainerForm60(out var formCha);
            if (formCha == null || panel == null) return;
            try
            {
                // Có sẵn (đã dựng trước) -> dùng luôn; chưa kịp thì tạo ngay bây giờ
                var form60 = TimForm60TrongPanel(panel) ?? TaoForm60An(panel);
                // Hiện Form 60 trước, rồi mới ẩn các control khác (không bị chớp nền trống giữa 2 form)
                // Đổi trang trong 1 khung hình: khóa vẽ panel, dựng sẵn bố cục Form60, ẩn Form6 + hiện Form60, rồi vẽ 1 lần.
                // (Form60 tự nạp dữ liệu ở luồng nền khi Load, giao diện không bị chặn)
                Module_ChuyenTrangMuot.Chuyen(panel, form60, form60.ChuanBiGiaoDien);
                bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().LaCheDoTanBinh();
                string doiTuong = laTanBinh ? Module_TaiKhoan.TenTB_Thuong : "CBCS";
                formCha.CapNhatTieuDe($"Trang thêm thông tin {doiTuong} vào phần mềm");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở Form nhập liệu CBCS: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// Xử lý đóng Form 60 và khôi phục Form 6 siêu tốc
        /// </summary>
        // -------------------------------------------------------------------------------------
        private void Form60_OnFormClosed(object? sender, FormClosedEventArgs e)
        {
            if (sender is not Form60_NhapLieuCBCS form60) return;
            // Hủy đăng ký sự kiện ngay lập tức để giải phóng RAM cho GC
            form60.FormClosed -= Form60_OnFormClosed;
            var formCha = Application.OpenForms["Form2_FormCha"] as Form2_FormCha;
            var panel = formCha?.Controls["PanelContainer"] as Panel;
            if (panel != null)
            {
                Form6_XuLyData? form6 = null;
                for (int i = 0; i < panel.Controls.Count; i++)
                {
                    if (panel.Controls[i] is Form6_XuLyData f6)
                    {
                        form6 = f6;
                        break;
                    }
                }
                if (form6 != null && !form6.IsDisposed)
                {
                    // Hiện lại Form6 trong 1 khung hình (Form60 đã đóng nên không còn gì chồng lên)
                    Module_ChuyenTrangMuot.Chuyen(panel, form6);
                    // Báo người dùng biết lưới đang được làm mới (Form60 sẽ nạp xong rồi tự cập nhật)
                    form6.toolStripStatusLabel1.Text = "Đang cập nhật danh sách...";
                }
            }
            // Ép thu hồi tài nguyên đồ họa (GDI Handles) lập tức
            form60.Dispose();
            // Dựng sẵn khung form 60 cho lần bấm tiếp theo
            LenLichChuanBiForm60(1500);
        }
        /// <summary>
        /// Dựng Form ảo chuyên hiển thị hộp thoại Xác Nhận Yes/No (Màu Xanh Navy).
        /// Trả về True nếu Đồng ý, False nếu Hủy.
        /// </summary>
        /// <summary>
        /// Dựng Form ảo chuyên hiển thị Cảnh Báo Validation (Màu Cam Đất).
        /// </summary>
        /// Dựng Form ảo chuyên hiển thị Lỗi Hệ Thống (Màu Đỏ Thẫm).
        /// Tích hợp nút Copy Lỗi.
        private void cBCSTrongDienQuanLy_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (!KiemTraDuLieuSanSang("quản lý")) return;
            // 2. Lọc dữ liệu thực tế và ép kiểu sang List ngay
            var dsThucTe = dtDanhSachGoc!.AsEnumerable()
                .Where(r => !string.IsNullOrWhiteSpace(r.Field<string>("HoVaTen")))
                .ToList();
            // 3. Chỉ cần 1 cái if này là đủ cho mọi trường hợp trống
            if (dsThucTe.Count == 0)
            {
                MessageBox.Show("Cơ sở dữ liệu trống hoặc không có thông tin CBCS hợp lệ để xử lý!",
                    "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var formCha = Application.OpenForms
                .OfType<Form2_FormCha>()
                .FirstOrDefault();
            if (formCha == null) return;
            // kiểm tra Form29 đã mở chưa
            var f = Application.OpenForms
                .OfType<Form29_CBCSTrongDienQuanLy>()
                .FirstOrDefault();
            if (f == null)
            {
                f = new Form29_CBCSTrongDienQuanLy
                {
                    Text = "CBCS trong diện quản lý",
                    TopLevel = false,
                    FormBorderStyle = FormBorderStyle.None,
                    Dock = DockStyle.Fill
                };
                var panel = formCha.Controls.Find("PanelContainer", true)
                                            .FirstOrDefault() as Panel;
                if (panel == null) return;
                panel.Controls.Add(f);
                f.Show();
                f.BringToFront();
                // khi đóng form29 thì hiện lại form6
                var form6 = Application.OpenForms
                    .OfType<Form6_XuLyData>()
                    .FirstOrDefault();
                f.FormClosed += (s, ev) =>
                {
                    if (form6 != null && !form6.IsDisposed)
                    {
                        form6.Dock = DockStyle.Fill;
                        form6.Show();
                        form6.BringToFront();
                    }
                };
                // 2. Tìm Form2 đang chạy và cập nhật tiêu đề
                // var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
                if (formCha != null)
                {
                    formCha.CapNhatTieuDe("Trang CBCS trong diện quản lý");
                }
            }
            else
            {
                f.BringToFront(); // nếu đã mở thì chỉ đưa lên trước
            }
        }
        private void phanTichDuLieuTrungTen_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (!KiemTraDuLieuSanSang("phân tích dữ liệu trùng tên")) return;
            var dsThucTe = dtDanhSachGoc!.AsEnumerable()
                .Where(r => !string.IsNullOrWhiteSpace(r.Field<string>("HoVaTen")))
                .ToList();
            if (dsThucTe.Count == 0)
            {
                MessageBox.Show("Cơ sở dữ liệu trống hoặc không có thông tin CBCS hợp lệ để xử lý!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            if (formCha == null) return;
            var panel = formCha.Controls.Find("PanelContainer", true).FirstOrDefault() as Panel;
            if (panel == null) return;
            var form6 = panel.Controls.OfType<Form6_XuLyData>().FirstOrDefault();
            foreach (System.Windows.Forms.Control ctl in panel.Controls)
                if (ctl is Form frm) frm.Hide();
            var form28 = panel.Controls.OfType<Form28_DataTrungTen>().FirstOrDefault();
            if (form28 == null)
            {
                form28 = new Form28_DataTrungTen
                {
                    TopLevel = false,
                    FormBorderStyle = FormBorderStyle.None,
                    Dock = DockStyle.Fill,
                    Text = "Phân tích dữ liệu trùng tên"
                };
                form28.FormClosed += (s, ev) =>
                {
                    if (panel.IsDisposed) return;
                    var f6 = panel.Controls.OfType<Form6_XuLyData>().FirstOrDefault();
                    if (f6 != null && !f6.IsDisposed)
                    {
                        f6.Dock = DockStyle.Fill;
                        f6.Show();
                        f6.BringToFront();
                    }
                };
                panel.Controls.Add(form28);
            }
            if (formCha != null) formCha.CapNhatTieuDe("Trang quản lý CBCS trùng tên");
            form28.Show();
            form28.BringToFront();
        }
        // Xóa toàn bộ DanhSach: DB chạy ở luồng nền (UI không treo), KHÔNG VACUUM trực tiếp (chỉ nén nền khi file thật sự phình)
        private readonly record struct KetQuaXoaTatCa(int SoDong, bool CanNen);
        private KetQuaXoaTatCa ThucThiXoaToanBoDatabase(string csdlPath)
        {
            _khoaGhiCSDL2.Wait(); // tuần tự hóa với các thao tác ghi CSDL2 khác -> không bị "database is locked"
            try
            {
                using var conn = Module_DanduongGPS.MoKetNoiDoc(csdlPath); // Pooling=false: đóng là nhả hẳn tệp
                using (var cmd = new SqliteCommand("PRAGMA foreign_keys = OFF;", conn)) cmd.ExecuteNonQuery();
                int soDong;
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new SqliteCommand($"DELETE FROM \"{Module_TaiKhoan.BangDanhSach}\";", conn, tran))
                            soDong = cmd.ExecuteNonQuery();
                        using (var cmd = new SqliteCommand($"DELETE FROM sqlite_sequence WHERE name='{Module_TaiKhoan.BangDanhSach}';", conn, tran))
                            cmd.ExecuteNonQuery();
                        tran.Commit();
                    }
                    catch
                    {
                        try { tran.Rollback(); } catch { /* transaction đã hỏng/đóng */ }
                        throw;
                    }
                }
                bool canNen = false;
                try
                {
                    long freelist = LayGiaTriPragma(conn, "freelist_count");
                    long pageCount = LayGiaTriPragma(conn, "page_count");
                    canNen = pageCount > 0
                             && freelist >= NGUONG_TRANG_TRONG_DE_NEN
                             && (double)freelist / pageCount >= TY_LE_TRANG_TRONG_DE_NEN;
                }
                catch { /* không quan trọng */ }
                return new KetQuaXoaTatCa(soDong, canNen);
            }
            finally
            {
                _khoaGhiCSDL2.Release();
            }
        }
        private async void xoaToanBoDuLieuDSCBCS_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (_dangXuLyXoa) return;
            if (!KiemTraDuLieuSanSang("xóa toàn bộ danh sách CBCS")) return;
            string dbPath = _csdl2Path;
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
            {
                MessageBox.Show("Không tìm thấy csdl2.db!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // Đếm số dòng trực tiếp từ RAM (dtDanhSachGoc) thay vì chọc CSDL
            int soDongTrongBang = 0;
            if (dtDanhSachGoc != null)
                soDongTrongBang = dtDanhSachGoc.AsEnumerable().Count(r => !string.IsNullOrWhiteSpace(r.Field<string>("HoVaTen")));
            if (soDongTrongBang == 0)
            {
                MessageBox.Show("Cơ sở dữ liệu hiện không có dữ liệu CBCS nào để xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            // Gọi form xác minh quyền admin
            DialogResult kq;
            using (Form24_XacMinhAdmin frm = new Form24_XacMinhAdmin())
            {
                frm.TopMost = true;
                frm.StartPosition = FormStartPosition.CenterScreen;
                kq = frm.ShowDialog();
            }
            if (kq != DialogResult.OK) return;
            _dangXuLyXoa = true;
            string? statusCu = toolStripStatusLabel1.Text;
            toolStripStatusLabel1.Text = "Đang xóa toàn bộ dữ liệu...";
            this.Enabled = false;
            this.UseWaitCursor = true;
            int soDongDaXoa = 0;
            bool xoaThanhCong = false;
            bool canNen = false;
            try
            {
                // 1. XÓA DATABASE Ở LUỒNG NỀN
                var ketQua = await Task.Run(() => ThucThiXoaToanBoDatabase(dbPath));
                if (this.IsDisposed || !this.IsHandleCreated) return;
                soDongDaXoa = ketQua.SoDong;
                canNen = ketQua.CanNen;
                xoaThanhCong = true;
                // 2. LÀM MỚI RAM + GIAO DIỆN
                DataCache.Clear();            // xóa sạch RAM để tránh lấy nhầm dữ liệu cũ
                ClearThongTin();              // xóa trắng các TextBox nhập liệu
                await ReloadDuLieu();         // lưới vẽ lại với dòng đệm ảo, CSDL trống
                CapNhatThongKeToanBoQuanSo(); // nhãn tổng quân số về 0
                ThongBaoForm4CapNhatLoaiDeXuat();
                KiemTraDuLieu();
                // 3. TÁC VỤ PHỤ (không chặn người dùng)
                _ = Task.Run(() =>
                {
                    try
                    {
                        Module_NhatKy.GhiNhatKy(
                            Module_TaiKhoan.TenTaiKhoan_RAM,
                            $"Xóa toàn bộ thông tin CBCS ({soDongDaXoa} dòng)",
                            DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
                    }
                    catch (Exception ex) { Debug.WriteLine($"Lỗi ghi log: {ex.Message}"); }
                });
                Module_DanduongGPS.OnDatabaseChanged?.Invoke();
                _ = Module_BieuDoTronTrangChu.CapNhatBieuDoForm4Async();
                YeuCauForm15DongBoLai();      // Form15 phải đồng bộ lại với CSDL2 vừa trống
                if (canNen) _ = NenCSDLNenAsync(dbPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi xóa toàn bộ dữ liệu: {ex}");
                toolStripStatusLabel1.Text = statusCu;
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    // DB đã xóa nhưng RAM chưa đồng bộ -> nạp lại để lưới không lệch với DB
                    if (xoaThanhCong)
                    {
                        try { kryptonButton_RefershCSDL.PerformClick(); } catch { /* bỏ qua */ }
                    }
                    this.UseWaitCursor = false;
                    this.Enabled = true;
                    MessageBox.Show($"Lỗi khi xóa dữ liệu:\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }
            finally
            {
                _dangXuLyXoa = false;
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    this.UseWaitCursor = false;
                    this.Enabled = true;
                }
            }
            // Thông báo SAU KHI giao diện đã mở khóa
            if (!this.IsDisposed && this.IsHandleCreated)
                MessageBox.Show(this, $"Đã xóa {soDongDaXoa} dòng dữ liệu.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void ThemDongTrongAnToan(DataTable dt)
        {
            if (dt == null) return;
            // 1. Xóa dòng ảo cũ trước khi thêm (để không bao giờ bị cộng dồn thành nhiều dòng trống)
            for (int i = dt.Rows.Count - 1; i >= 0; i--)
            {
                var val = dt.Rows[i]["HoVaTen"];
                if (val == DBNull.Value || string.IsNullOrWhiteSpace(val?.ToString()))
                {
                    dt.Rows.RemoveAt(i);
                }
            }
            // 2. Mở khóa cột
            foreach (DataColumn col in dt.Columns)
            {
                col.AllowDBNull = true;
                col.ReadOnly = false;
            }
            // 3. Thêm duy nhất 1 dòng ảo mới tinh vào cuối bảng
            DataRow newRow = dt.NewRow();
            if (dt.Columns.Contains("HoVaTen")) newRow["HoVaTen"] = "";
            dt.Rows.Add(newRow);
        }
        /// <summary>
        /// Hàm này tìm Form 4 (Trang Chủ) đang mở ngầm và yêu cầu nó cập nhật lại ComboBox
        /// </summary>
        /// 
        private void ThongBaoForm4CapNhatLoaiDeXuat()
        {
            // Tìm Form 4 đang được mở trong phần mềm
            var form4 = Application.OpenForms.OfType<Form4_TrangDauTien>().FirstOrDefault();
            if (form4 != null && !form4.IsDisposed)
            {
                form4.CapNhatDanhSachPhanLoaiDeXuat();
            }
        }
        /// <summary>
        /// Báo cho Form 15 (Thống kê thi đua) đọc lại cột Ghi chú ngay khi dữ liệu Form 6 thay đổi.
        /// Form 15 chưa mở / chưa nạp dữ liệu thì bỏ qua (lúc mở sẽ tự đọc GhiChu mới nhất).
        /// </summary>
        private void ThongBaoForm15CapNhatGhiChu()
        {
            if (IsDisposed || !IsHandleCreated) return;
            // ReloadDuLieu có thể được gọi từ luồng nền -> đưa về luồng giao diện cho an toàn
            if (InvokeRequired)
            {
                BeginInvoke(new Action(ThongBaoForm15CapNhatGhiChu));
                return;
            }
            try
            {
                var form15 = Application.OpenForms.OfType<Form15_ThongKeThiDua>().FirstOrDefault(f => !f.IsDisposed);
                if (form15 == null)
                {
                    var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
                    var panel = formCha?.Controls.Find("PanelContainer", true).FirstOrDefault() as Panel;
                    form15 = panel?.Controls.OfType<Form15_ThongKeThiDua>().FirstOrDefault(f => !f.IsDisposed);
                }
                if (form15 != null) _ = form15.CapNhatGhiChuTuCSDL2Async();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi báo Form15 cập nhật GhiChu: " + ex.Message);
            }
        }
        private static string GiaiMaSafe(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            try
            {
                string result = Module_BaoMatAES.GiaiMa(input);
                if (string.IsNullOrWhiteSpace(result)) return input; // Fallback nếu AES nuốt lỗi
                return result;
            }
            catch { return input; }
        }
        public static List<string> LayDanhSachPhanLoaiThucTe()
        {
            var uniqueList = new HashSet<string>();
            if (DataCache.IsLoaded)
            {
                var dt = DataCache.GetDanhSach();
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        string phanLoai = row["PhanLoai"]?.ToString()?.Trim() ?? "";
                        if (!string.IsNullOrWhiteSpace(phanLoai)) uniqueList.Add(phanLoai);
                    }
                }
            }
            else
            {
                string dbPath = Module_DanduongGPS.DuongDanCSDL2;
                if (System.IO.File.Exists(dbPath))
                {
                    try
                    {
                        using var conn = new Microsoft.Data.Sqlite.SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(dbPath));
                        conn.Open();
                        using var cmd = new Microsoft.Data.Sqlite.SqliteCommand($"SELECT PhanLoai FROM {Module_TaiKhoan.BangDanhSach} WHERE PhanLoai IS NOT NULL AND PhanLoai <> ''", conn);
                        using var reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            string raw = reader.GetString(0);
                            string phanLoai = GiaiMaSafe(raw).Trim(); // Dùng hàm an toàn
                            if (!string.IsNullOrWhiteSpace(phanLoai)) uniqueList.Add(phanLoai);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Lỗi đọc phân loại thực tế: " + ex.Message);
                    }
                }
            }
            List<string> thuTuChuan = new List<string> { Module_HeThong.Loai_1, Module_HeThong.Loai_2, Module_HeThong.Loai_3, Module_HeThong.Loai_4, Module_HeThong.PL_KHONG_PL };
            var danhSachDaLoc = uniqueList.OrderBy(x =>
            {
                int index = thuTuChuan.IndexOf(x);
                return index == -1 ? int.MaxValue : index;
            }).ToList();
            return danhSachDaLoc;
        }
        private bool KiemTraDuLieuSanSang(string tenTacVu = "xử lý")
        {
            // 1. Kiểm tra bảng có tồn tại trên RAM không
            if (dtDanhSachGoc == null || dtDanhSachGoc.Rows.Count == 0)
            {
                MessageBox.Show($"Cơ sở dữ liệu hiện đang trống. Không có thông tin để {tenTacVu}!",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            // 2. Kiểm tra "Nhạy": Quét xem có ai có tên không (Bỏ qua dòng ảo cuối lưới)
            // Dùng .Any() để tối ưu tốc độ xử lý hàng vạn dòng
            bool coDuLieuThucTe = dtDanhSachGoc.AsEnumerable()
                .Any(r => !string.IsNullOrWhiteSpace(r.Field<string>("HoVaTen")));
            if (!coDuLieuThucTe)
            {
                MessageBox.Show($"Danh sách chưa có thông tin CBCS hợp lệ để {tenTacVu}!",
                    "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true; // Dữ liệu đã sẵn sàng
        }
        // TỰ ĐỘNG XÓA TRẮNG FORM NHẬP LIỆU KHI TƯƠNG TÁC VỚI KHU VỰC TÌM KIẾM
        // Quản lý trạng thái nhập liệu
        private void DangKyBaoVeDuLieuKhiTimKiem()
        {
            var danhSachTriggers = new List<System.Windows.Forms.Control>
    {
        textBox_TimKiemTheoTen,
        comboBox_TimKiemDonVi,
        comboBox_XepLoaiThiDua,
        kryptonButton_LamMoiCacOTimKiem,
        kryptonButton1_ThiDuaBaNhat,
        kryptonButton_RefershCSDL
    };
            foreach (var ctrl in danhSachTriggers)
            {
                if (ctrl == null) continue;
                ctrl.Enter += (s, e) =>
                {
                    // Quét thấy form đang có dữ liệu thì âm thầm xóa trắng luôn, không hỏi nhiều
                    if (CoDuLieuNhap())
                    {
                        ClearThongTin();
                        kryptonDataGridView1.ClearSelection(); // Nhả bôi đen trên lưới cho an toàn
                    }
                };
            }
        }
        private bool CoDuLieuNhap()
        {
            return !string.IsNullOrWhiteSpace(textBox_STT.Text) ||
                   !string.IsNullOrWhiteSpace(textBox_HoVaTen.Text) ||
                   !string.IsNullOrWhiteSpace(textBox_SoHieu.Text) ||
                   !string.IsNullOrWhiteSpace(textBox_NamSinh.Text) ||
                   !string.IsNullOrWhiteSpace(textBox_QueQuan.Text) ||
                   !string.IsNullOrWhiteSpace(textBox_NgayVaoCAND.Text) ||
                   !string.IsNullOrWhiteSpace(textBox_CapBac.Text) ||
                   !string.IsNullOrWhiteSpace(textBox_DonVi.Text) ||
                   !string.IsNullOrWhiteSpace(textBox_GhiChu.Text) ||
                   comboBox_ChucVu.SelectedIndex != -1 ||
                   comboBox_PhanLoai.SelectedIndex != -1;
        }
        // 1. KHAI BÁO HẰNG SỐ CHUẨN KỸ SƯ (Quản lý tập trung)
        private static readonly string[] DANH_SACH_PHAN_LOAI = { Module_HeThong.Loai_1, Module_HeThong.Loai_2, Module_HeThong.Loai_3, Module_HeThong.Loai_4, Module_HeThong.PL_KHONG_PL };
        // 2. LÕI TÍNH TOÁN (Đếm siêu tốc O(N) - Chặn rác mã hóa)
        // 3. HÀM CẬP NHẬT GIAO DIỆN (Đã thêm lại chữ "đồng chí")
        private void CapNhatGiaoDienThongKe(int tongSo, Dictionary<string, int> chiTiet)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => CapNhatGiaoDienThongKe(tongSo, chiTiet)));
                return;
            }
            if (toolStripStatusLabel1 != null)
            {
                toolStripStatusLabel1.Text = $"Tổng cộng: {tongSo} {Module_HeThong.Tu_dong_chi}";
                toolStripStatusLabel1.Spring = false; // Vẫn giữ tắt co giãn để tránh bị đẩy ra ngoài rìa
            }
            foreach (var loai in DANH_SACH_PHAN_LOAI)
            {
                if (labelsPhanLoai.TryGetValue(loai, out var lbl) && lbl != null)
                {
                    int count = chiTiet[loai];
                    if (count > 0)
                    {
                        // Thêm lại chữ "đồng chí" và giữ format gọn gàng {Module_HeThong.Tu_dong_chi} 
                        lbl.Text = $"     |     {loai}: {count} {Module_HeThong.Tu_dong_chi}";
                        lbl.Visible = true;
                        lbl.Spring = false;
                    }
                    else
                    {
                        lbl.Visible = false; // Tự động tàng hình nếu bằng 0
                    }
                }
            }
        }
        private void CapNhatThongKePhanLoai(DataView? dv)
        {
            var data = ThucThiThongKe(dv);
            CapNhatGiaoDienThongKe(data.TongSo, data.ChiTiet);
        }
        //private void CapNhatThongKeSieuToc(DataView dv)
        //{
            //var data = ThucThiThongKe(dv);
            //CapNhatGiaoDienThongKe(data.TongSo, data.ChiTiet);
        //}
        public void CapNhatThongKeToanBoQuanSo()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(CapNhatThongKeToanBoQuanSo));
                return;
            }
            if (dtDanhSachGoc != null)
            {
                var data = ThucThiThongKeHang(dtDanhSachGoc, _hienThi);
                CapNhatGiaoDienThongKe(data.TongSo, data.ChiTiet);
            }
        }
        private void toolStripMenuItem_PhanTichQuanSo_Click(object? sender, EventArgs e)
        {
            if (!KiemTraDuLieuSanSang("phân tích quân số")) return;
            try
            {
                var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
                if (formCha == null) return;
                var panel = formCha.Controls.Find("PanelContainer", true).FirstOrDefault() as Panel;
                if (panel == null) return;
                var form6 = panel.Controls.OfType<Form6_XuLyData>().FirstOrDefault();
                // Ẩn tất cả form đang mở trong Panel để nhường chỗ
                foreach (Form frm in panel.Controls.OfType<Form>())
                {
                    frm.Hide();
                }
                // Tìm xem Form 26 đã tồn tại chưa
                var form26 = panel.Controls.OfType<Form26_PhanTichQuanSo>().FirstOrDefault();
                if (form26 == null)
                {
                    // Nếu chưa có -> Tạo mới (Không cần truyền dữ liệu gì qua nữa)
                    form26 = new Form26_PhanTichQuanSo()
                    {
                        TopLevel = false,
                        FormBorderStyle = FormBorderStyle.None,
                        Dock = DockStyle.Fill,
                        Text = "Phân tích quân số"
                    };
                    form26.FormClosed += (s, ev) =>
                    {
                        if (form6 != null && !form6.IsDisposed)
                        {
                            form6.Dock = DockStyle.Fill;
                            form6.Show();
                            form6.BringToFront();
                        }
                    };
                    panel.Controls.Add(form26);
                }
                // ĐIỂM MẤU CHỐT: Bắt Form 26 tự chọc vào DB để lấy dữ liệu tươi nhất
                form26.LoadDataTuDatabase();
                // Hiển thị
                form26.Show();
                form26.BringToFront();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Có lỗi xảy ra khi mở tính năng Phân tích: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async void ToolStripMenuItem_QuanLyThiDuaBaNhat_Click(object? sender, EventArgs e)
        {
            kryptonButton1_ThiDuaBaNhat.PerformClick();
        }
        /// <summary>
        /// 
        /// Kiểm tra bảng DanhSach có dữ liệu thực tế hay không
        /// để tự động ẩn/hiện các chức năng phân tích.
        /// </summary>
        private async void kryptonButton1_ThiDuaBaNhat_Click(object? sender, EventArgs e)
        {
            // 1. Kiểm tra an toàn
            if (!KiemTraDuLieuSanSang("quản lý thi đua Ba Nhất"))
            {
                return;
            }
            // 2. Tìm Form cha (Form2) và Panel trung gian
            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            if (formCha == null || formCha.IsDisposed) return;
            var panel = formCha.Controls.Find("PanelContainer", true).FirstOrDefault() as Panel;
            if (panel == null || panel.IsDisposed) return;
            // 3. Ẩn tất cả các Form hiện tại trong PanelContainer
            foreach (Control ctl in panel.Controls)
            {
                if (ctl is Form frm) frm.Hide();
            }
            // 4. KIỂM TRA TRONG RAM: Tìm Form55 đã tồn tại chưa
            var form55 = panel.Controls.OfType<Form55_QuanLyHeThongThiDuaBaNhat>().FirstOrDefault();
            if (form55 == null || form55.IsDisposed)
            {
                // Chưa có -> Khởi tạo lần đầu
                form55 = new Form55_QuanLyHeThongThiDuaBaNhat
                {
                    TopLevel = false,
                    FormBorderStyle = FormBorderStyle.None,
                    Dock = DockStyle.Fill
                };
                panel.Controls.Add(form55);
                form55.Show();
            }
            else
            {
                // Đã có sẵn trong RAM -> Chỉ việc hiện lại và đồng bộ nhẹ
                form55.Dock = DockStyle.Fill;
                form55.Show();
                form55.BringToFront();
                await form55.MoForm42Async(); // Kích hoạt nạp lại Tab Form 42 mượt mà
            }
            // 5. Cập nhật tiêu đề lớn của phần mềm
            if (formCha.Label1 != null)
            {
                formCha.Label1.Text = "Hệ thống quản lý phong trào thi đua Ba Nhất";
            }
        }
        private void KryptonDataGridView1_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            // 1. Chặn click vào Header (RowIndex = -1) hoặc lỗi ngớ ngẩn
            if (e.RowIndex < 0 || sender is not DataGridView grid || dtDanhSachGoc == null) return;
            // 2. Chặn lỗi lệch pha khi danh sách hiển thị đang được lọc ngầm
            if (e.RowIndex >= SoDongHienThi) return;
            // 3. Tách việc lấy dữ liệu khỏi luồng vẽ UI để tránh giật lag cục bộ
            this.BeginInvoke(new Action(() =>
            {
                try
                {
                    DataRow? rowView = LayDongHienThi(e.RowIndex);
                    if (rowView == null) return;
                    string checkHoTen = rowView["HoVaTen"]?.ToString()?.Trim() ?? "";
                    // Nếu click vào dòng trống (Dòng ảo dưới cùng), thì Clear Form
                    if (string.IsNullOrEmpty(checkHoTen))
                    {
                        ClearThongTin();
                        return;
                    }
                    // Hàm đọc an toàn nội bộ (Đã sửa lại để bắt lỗi NullReference)
                    string GetCellValue(string colName)
                    {
                        try
                        {
                            string raw = rowView[colName]?.ToString()?.Trim() ?? "";
                            if (colName == "HoVaTen" || colName == "DonVi" || colName == "PhanLoai" || colName == "STT") return raw;
                            return GiaiMaSafeCoCache(raw);
                        }
                        catch { return ""; } // An toàn tuyệt đối
                    }
                    // 4. Bơm dữ liệu lên TextBox chuẩn
                    textBox_STT.Text = GetCellValue("STT");
                    textBox_HoVaTen.Text = checkHoTen;
                    textBox_SoHieu.Text = GetCellValue("SoHieu");
                    textBox_NamSinh.Text = GetCellValue("NamSinh");
                    textBox_QueQuan.Text = GetCellValue("QueQuan");
                    textBox_NgayVaoCAND.Text = GetCellValue("NgayVaoCAND");
                    textBox_CapBac.Text = GetCellValue("CapBac");
                    textBox_DonVi.Text = GetCellValue("DonVi");
                    textBox_GhiChu.Text = GetCellValue("GhiChu");
                    // 5. XỬ LÝ COMBOBOX CHỨC VỤ & PHÂN LOẠI
                    // Xử lý Chức Vụ
                    string chucVuGrid = GetCellValue("ChucVu");
                    if (!string.IsNullOrEmpty(chucVuGrid))
                    {
                        if (!comboBox_ChucVu.Items.Contains(chucVuGrid))
                        {
                            comboBox_ChucVu.Items.Add(chucVuGrid);
                        }
                        comboBox_ChucVu.SelectedItem = chucVuGrid;
                    }
                    else
                    {
                        comboBox_ChucVu.SelectedIndex = -1;
                    }
                    // 🎯 XỬ LÝ PHÂN LOẠI (Lấy giá trị gốc từ CSDL chọn đúng Item trong ComboBox)
                    string phanLoaiGrid = GetCellValue("PhanLoai"); // Giá trị gốc trong CSDL (VD: "Loại 1")
                    if (!string.IsNullOrEmpty(phanLoaiGrid))
                    {
                        // Tìm ComboboxItem trong danh sách có Value (hoặc Text) khớp với giá trị gốc
                        ComboboxItem? itemMatch = comboBox_PhanLoai.Items
                            .OfType<ComboboxItem>()
                            .FirstOrDefault(x => string.Equals(x.Value, phanLoaiGrid, StringComparison.OrdinalIgnoreCase) ||
                                                 string.Equals(x.Text, phanLoaiGrid, StringComparison.OrdinalIgnoreCase));
                        if (itemMatch != null)
                        {
                            comboBox_PhanLoai.SelectedItem = itemMatch;
                            comboBox_PhanLoai.Text = itemMatch.Text; // Đảm bảo KryptonComboBox hiển thị đúng Text
                        }
                        else
                        {
                            // Trường hợp dữ liệu lạ chưa có trong danh sách -> Thêm tạm
                            ComboboxItem newItem = new ComboboxItem(phanLoaiGrid, phanLoaiGrid);
                            comboBox_PhanLoai.Items.Add(newItem);
                            comboBox_PhanLoai.SelectedItem = newItem;
                            comboBox_PhanLoai.Text = newItem.Text;
                        }
                    }
                    else
                    {
                        comboBox_PhanLoai.SelectedIndex = -1;
                        comboBox_PhanLoai.Text = "";
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CellClick Lỗi] {ex.Message}");
                }
            }));
        }
        //private void Form6_FormClosing(object? sender, FormClosingEventArgs e)
        //{
            //// 3. 🎯 Hủy đăng ký sự kiện khi đóng Form6 để giải phóng tài nguyên
            //Module_HeThong.SuKienThayDoiCheDoXetThiDua -= OnCheDoThayDoi;
        //}
        private async void themDuLieuTuFileExce_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await ChayNhapDuLieuTuFileExcelAsync();
        }
        public void YeuCauNhapDuLieuTuFileExcel()
        {
            if (IsDisposed || Disposing || !IsHandleCreated)
                return;
            Interlocked.Exchange(ref _yeuCauNhapExcel, 1);
            BeginInvoke(new Action(async () =>
            {
                if (IsDisposed || Disposing)
                    return;
                if (Interlocked.Exchange(ref _yeuCauNhapExcel, 0) != 1)
                    return;
                await ChayNhapDuLieuTuFileExcelAsync();
            }));
        }
        private async Task ChayNhapDuLieuTuFileExcelAsync()
        {
            bool laTanBinh = Module_TaiKhoan
                .LayPhienBanPhanMem()
                .LaCheDoTanBinh();
            //region BƯỚC 1: TƯƠNG TÁC NGƯỜI DÙNG & KIỂM TRA MÔI TRƯỜNG (UI Thread)
            using OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx, *.xlsm)|*.xlsx;*.xlsm",
                Title = "Chọn tệp Excel để nạp dữ liệu"
            };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;
            string excelPath = ofd.FileName;
            // 1.1 Validate File Size (Chặn file quá lớn gây sập RAM - Giới hạn ví dụ: 50MB)
            var fi = new FileInfo(excelPath);
            if (fi.Length > 50 * 1024 * 1024)
            {
                MessageBox.Show(this, "Tệp Excel quá lớn (Vượt quá 50MB). Hệ thống từ chối nạp để bảo vệ bộ nhớ.", "Cảnh báo an toàn", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // 1.2 Validate File Lock (Chặn lỗi sập ngầm do tệp đang mở)
            if (IsFileLocked(excelPath))
            {
                MessageBox.Show(this, "Tệp Excel này đang được mở bởi một phần mềm khác.\nVui lòng đóng tệp lại trước khi nạp dữ liệu!", "Tệp đang bị khóa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            //endregion
            // Biến trạng thái để giao tiếp giữa Background và UI
            string requiredSheet = laTanBinh ? "DSTanBinh_PhanMemThiDua2026" : "DSCBCS_PhanMemThiDua2026";
            string dataChuanDeLuu = string.Empty;
            int trangThaiBaoCao = 0;
            int tongSoDongDuLieu = 0;
            // 🔴 KHÓA GIAO DIỆN VÀ BẬT WAIT CURSOR
            this.Enabled = false;
            this.UseWaitCursor = true;
            this.Update(); // chỉ vẽ lại trạng thái khóa (không DoEvents: tránh tái nhập sự kiện)
            try
            {
                //region BƯỚC 2: PHÂN TÍCH FILE EXCEL (Background Thread)
                bool dungFile = await Task.Run(() =>
                {
                    try
                    {
                        using var wb = new ClosedXML.Excel.XLWorkbook(excelPath);
                        // Check sheet chính và ĐẾM SỐ DÒNG
                        var wsData = wb.Worksheets.FirstOrDefault(ws => ws.Name == requiredSheet);
                        if (wsData == null) return false;
                        tongSoDongDuLieu = wsData.LastRowUsed()?.RowNumber() ?? 0;
                        // Check sheet cấu hình
                        var wsTongHop = wb.Worksheets.FirstOrDefault(x => x.Name.Equals("BAO_CAO_TONG_HOP", StringComparison.OrdinalIgnoreCase));
                        if (wsTongHop != null)
                        {
                            string a1 = wsTongHop.Cell("A1").GetValue<string>()?.Trim() ?? string.Empty;
                            string b1 = wsTongHop.Cell("B1").GetValue<string>()?.Trim() ?? string.Empty;
                            if (a1 == "Chuỗi mã hóa" && !string.IsNullOrWhiteSpace(b1))
                            {
                                try
                                {
                                    string dec = Module_BaoMatAES.GiaiMaNhanDang(b1);
                                    if (!string.IsNullOrWhiteSpace(dec) && dec != b1)
                                    {
                                        dataChuanDeLuu = dec;
                                        trangThaiBaoCao = 1;
                                    }
                                    else trangThaiBaoCao = -1;
                                }
                                catch { trangThaiBaoCao = -1; }
                            }
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Lỗi Background Excel] {ex.Message}");
                        throw;
                    }
                });
                if (!dungFile)
                {
                    this.UseWaitCursor = false; this.Enabled = true; this.Activate();
                    MessageBox.Show(this, "Sai tệp Excel! Phiên bản phần mềm không hỗ trợ tệp này!", "CẢNH BÁO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                //endregion
                //region BƯỚC 3: XỬ LÝ LOGIC NGƯỜI DÙNG (UI Thread)
                // 🟢 MỞ KHÓA TẠM THỜI ĐỂ TƯƠNG TÁC
                this.UseWaitCursor = false;
                this.Enabled = true;
                this.Activate();
                // 3.1 Cảnh báo hoặc cập nhật báo cáo tóm tắt
                if (trangThaiBaoCao == -1)
                {
                    MessageBox.Show(this, "Không thể giải mã chuỗi dữ liệu cấu hình!\nDữ liệu có thể đã bị chỉnh sửa...", "Lỗi bảo mật", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else if (trangThaiBaoCao == 1)
                {
                    if (MessageBox.Show(this, "Phát hiện kết quả tóm tắt thi đua trong tháng đi kèm.\nBạn có muốn cập nhật Báo cáo tổng hợp từ file này không?", "Xác nhận cập nhật", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        this.Enabled = false; this.UseWaitCursor = true; this.Update(); // chỉ vẽ lại (không DoEvents: tránh tái nhập sự kiện)
                        await Task.Run(async () =>
                        {
                            using var conn = new Microsoft.Data.Sqlite.SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl2Path));
                            await conn.OpenAsync();
                            using var tran = (SqliteTransaction)await conn.BeginTransactionAsync();
                            try
                            {
                                using var cmd = conn.CreateCommand(); cmd.Transaction = tran;
                                cmd.CommandText = "UPDATE ThongTin SET TomTatGhiChu = @data WHERE ID = 1";
                                cmd.Parameters.AddWithValue("@data", dataChuanDeLuu);
                                await cmd.ExecuteNonQueryAsync(); await tran.CommitAsync();
                            }
                            catch { await tran.RollbackAsync(); throw; }
                        });
                        this.UseWaitCursor = false; this.Enabled = true; this.Activate();
                    }
                }
                // 3.2 XỬ LÝ ĐIỀU HƯỚNG THÔNG MINH CHO XÓA DỮ LIỆU
                bool xoaDuLieuCu = false;
                if (tongSoDongDuLieu > 1500)
                {
                    // Tệp lớn: giữ quy ước cũ (xóa sạch rồi nạp lại) nhưng PHẢI được người dùng đồng ý,
                    // bản trước tự xóa toàn bộ danh sách hiện có mà không hỏi => dễ mất dữ liệu ngoài ý muốn.
                    var xacNhanXoa = MessageBox.Show(this,
                        $"Tệp Excel có {tongSoDongDuLieu:N0} dòng (trên 1.500 dòng).\n\n" +
                        "Với tệp lớn, hệ thống sẽ XÓA TOÀN BỘ danh sách hiện có rồi nạp lại từ tệp này.\n\n" +
                        "Bạn có chắc chắn muốn tiếp tục?",
                        "Xác nhận thay thế dữ liệu", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                    if (xacNhanXoa != DialogResult.Yes) return;   // finally bên dưới tự mở khóa giao diện
                    xoaDuLieuCu = true;
                    string tenHienTai = Module_TaiKhoan.TenTaiKhoan_RAM;
                    string thongDiepNhatKy = "Hệ thống nạp dữ liệu excel tự động kích hoạt FastDataReader và làm sạch dữ liệu cũ (Tệp > 1500 dòng).";
                    Module_NhatKy.GhiNhatKy(
                         taiKhoan: tenHienTai,
                         hanhDong: thongDiepNhatKy,
                         ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                     );
                }
                else
                {
                    if (MessageBox.Show(this, "Bạn có muốn xóa toàn bộ dữ liệu cũ trước khi nạp không?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        xoaDuLieuCu = true;
                    }
                }
                // 🔴 KHÓA LẠI UI ĐỂ CHẠY BACKGROUND THREAD
                this.Enabled = false;
                this.UseWaitCursor = true;
                this.Update(); // chỉ vẽ lại trạng thái khóa (không DoEvents: tránh tái nhập sự kiện)
                //endregion
                //region BƯỚC 4: NẠP DỮ LIỆU CHÍNH (Background Thread)
                List<string> danhSachTrung = new List<string>();
                await Task.Run(() =>
                {
                    if (tongSoDongDuLieu > 1500)
                    {
                        Debug.WriteLine($"[Tối Ưu] Kích hoạt FastDataReader cho {tongSoDongDuLieu} dòng.");
                        // Giữ kết quả dòng trùng (bản trước bỏ qua giá trị trả về => người dùng không biết dòng nào bị loại)
                        if (laTanBinh)
                            danhSachTrung = Module_XuatNhapDuLieuThiDua.NhapDanhSachExcelTanBinhFastDataReader(excelPath, xoaDuLieuCu);
                        else
                            danhSachTrung = Module_XuatNhapDuLieuThiDua.NhapDanhSachExcelCBCSFastDataReader(excelPath, xoaDuLieuCu);
                    }
                    else
                    {
                        Debug.WriteLine($"[Tiêu Chuẩn] Kích hoạt ClosedXML cho {tongSoDongDuLieu} dòng.");
                        if (laTanBinh)
                            danhSachTrung = Module_XuatNhapDuLieuThiDua.NhapDanhSachExcelTanBinh(excelPath, xoaDuLieuCu);
                        else
                            danhSachTrung = Module_XuatNhapDuLieuThiDua.NhapDanhSachExcelCBCS(excelPath, xoaDuLieuCu);
                    }
                    // ✅ Tự động chạy trích xuất và đồng bộ Sheet Sổ Vàng Ba Nhất
                    Module_BaNhat.NhapDuLieuVaoBangQuanLyBaNhat(excelPath, _csdl2Path);
                });
                //endregion
                //region BƯỚC 5: ĐỒNG BỘ VÀ CLEANUP (UI Thread)
                DataCache.Clear();
                await ReloadDuLieu();
                ApplyFilter();
                CapNhatThongKeToanBoQuanSo();
                KiemTraDuLieu();
                YeuCauForm15DongBoLai(); // Form15 chưa tự biết CSDL2 vừa nạp thêm người -> bắt đồng bộ + nạp lại
                await Module_BieuDoTronTrangChu.CapNhatBieuDoForm4Async();
                var frm12 = Application.OpenForms.OfType<Form12>().FirstOrDefault();
                if (frm12 != null && frm12.IsHandleCreated && !frm12.IsDisposed)
                    frm12.BeginInvoke(new Action(() => frm12.LoadFromSQLite()));
                string nhomDoiTuong = laTanBinh ? Module_TaiKhoan.TenTB_Hoa : "CBCS";
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, $"Nạp thành công dữ liệu thi đua {nhomDoiTuong} từ tệp Excel", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
                this.UseWaitCursor = false; this.Enabled = true; this.Activate();
                // 🔥 XỬ LÝ KẾT QUẢ HIỂN THỊ TRÊN UI THREAD
                if (danhSachTrung != null && danhSachTrung.Count > 0)
                {
                    string msg = $"Nạp dữ liệu thành công!\nĐã quét tổng cộng: {tongSoDongDuLieu} hồ sơ.\n\nTuy nhiên, các dòng sau bị trùng số hiệu và đã bị bỏ qua:\n\n{string.Join("\n", danhSachTrung.Take(10))}";
                    if (danhSachTrung.Count > 10) msg += $"\n...và {danhSachTrung.Count - 10} dòng khác.";
                    MessageBox.Show(this, msg, "Cảnh báo trùng dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                //endregion
            }
            catch (Exception mainEx)
            {
                this.UseWaitCursor = false; this.Enabled = true; this.Activate();
                MessageBox.Show(this, "Có sự cố xảy ra trong quá trình nạp dữ liệu!\nChi tiết: " + mainEx.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // LUÔN LUÔN DỌN DẸP DÙ THÀNH CÔNG HAY LỖI
                this.UseWaitCursor = false;
                this.Enabled = true;
                this.Focus();
            }
        }
        // Đặt cùng khối khai báo biến ở đầu class Form6_XuLyData
        //private bool _isRefreshingTask = false; // Cờ khóa chống gọi trùng lặp tiến trình Làm mới
        /// <summary>
        /// Cấu hình thuộc tính ban đầu và kiểu dáng giao diện cho ProgressBar Làm mới.
        /// </summary>
        private void KhoiTaoProgressBarLamMoi()
        {
            if (toolStripProgressBar1_LamMoi == null) return;
            Action action = () =>
            {
                toolStripProgressBar1_LamMoi.Visible = false;           // Ẩn mặc định khi chưa dùng
                toolStripProgressBar1_LamMoi.AutoSize = false;          // Khóa chế độ tự co giãn
                toolStripProgressBar1_LamMoi.Width = 120;               // Chiều rộng cố định
                toolStripProgressBar1_LamMoi.Height = 14;               // Chiều cao cố định
                toolStripProgressBar1_LamMoi.Minimum = 0;
                toolStripProgressBar1_LamMoi.Maximum = 100;
                toolStripProgressBar1_LamMoi.Value = 0;
                toolStripProgressBar1_LamMoi.Style = ProgressBarStyle.Continuous; // Màu mượt liền khối
                // Cấu hình màu xanh lá cây đậm hiện đại
                toolStripProgressBar1_LamMoi.ForeColor = Color.FromArgb(40, 167, 69); // Xanh lá cây (Success Green)
                toolStripProgressBar1_LamMoi.BackColor = Color.FromArgb(230, 230, 230);
            };
            // Kiểm tra và thực thi an toàn trên UI Thread
            if (this.InvokeRequired)
            {
                this.Invoke(action);
            }
            else
            {
                action();
            }
        }
        // PHẦN BỔ SUNG 1: FIELD + CLASS MỚI (đặt gần các field đầu class)
        // Bật true để giải mã AES song song (CHỈ bật sau khi kiểm tra Module_BaoMatAES.GiaiMa
        // an toàn đa luồng: không dùng chung 1 đối tượng Aes/MemoryStream static).
        // Gói dữ liệu nạp sẵn trên luồng nền, UI chỉ việc gán vào control.
        private sealed class LoadSnapshot
        {
            public string[]? UuTien;              // Module_DonVi.LayDanhSachDonViUuTienArray()
            public List<string>? DsDonViModule;   // Module_DonVi.GetDanhSachDonVi()
            public List<string>? DsChucVuGiaiMa;  // Module_ChucVu.GetDanhSachChucVu() đã giải mã
            public bool? IsAnhXa;                 // Module_HeThong.IsKichHoatAnhXa(true)
            public List<string> DonViTrongCsdl = new();
            public List<string> PhanLoaiDistinct = new();
            public int SoLuongThucTe;
            public BoLocDanhSach? BoLoc;          // chỉ mục lọc dựng sẵn ở luồng nền
        }
        private LoadSnapshot? _snap;
        // Bản đồ cột Grid -> ordinal của DataTable (tính 1 lần / lần reload, thay cho tra tên chuỗi mỗi ô)
        private int[] _mapOrdinalCot = Array.Empty<int>();
        private bool[] _cotDaGiaiMa = Array.Empty<bool>();
        private int _gridColHoTen = -1, _gridColPhanLoai = -1;
        private int _ordHoVaTen = -1, _ordPhanLoai = -1;
        private bool _menuDaTichHop = false;
        private System.Windows.Forms.Timer? _timerResize;
        // PHẦN BỔ SUNG 2: HÀM MỚI
        private void XayDungBanDoCot()
        {
            var dgv = kryptonDataGridView1;
            var dt = dtDanhSachGoc;
            int n = dgv.Columns.Count;
            var map = new int[n];
            var daGiaiMa = new bool[n];
            _gridColHoTen = -1;
            _gridColPhanLoai = -1;
            for (int i = 0; i < n; i++)
            {
                string name = dgv.Columns[i].Name;
                map[i] = dt == null ? -1 : dt.Columns.IndexOf(name);
                daGiaiMa[i] = name == "HoVaTen" || name == "DonVi" || name == "PhanLoai" || name == "STT" || name == "ID";
                if (name == "HoVaTen") _gridColHoTen = i;
                if (name == "PhanLoai") _gridColPhanLoai = i;
            }
            _mapOrdinalCot = map;
            _cotDaGiaiMa = daGiaiMa;
            _ordHoVaTen = dt?.Columns.IndexOf("HoVaTen") ?? -1;
            _ordPhanLoai = dt?.Columns.IndexOf("PhanLoai") ?? -1;
        }
        // Chạy trên luồng nền: gom mọi thứ từng bị đọc rải rác trên UI thread vào 1 lần.
        private LoadSnapshot TaoSnapshot(DataTable dt)
        {
            var s = new LoadSnapshot();
            try { s.UuTien = Module_DonVi.LayDanhSachDonViUuTienArray(); } catch { }
            try { s.DsDonViModule = Module_DonVi.GetDanhSachDonVi().ToList(); } catch { }
            try { s.IsAnhXa = Module_HeThong.IsKichHoatAnhXa(true); } catch { }
            try
            {
                var ds = Module_ChucVu.GetDanhSachChucVu();
                if (ds != null)
                {
                    var list = new List<string>();
                    foreach (var cvMaHoa in ds)
                    {
                        if (string.IsNullOrWhiteSpace(cvMaHoa)) continue;
                        string cv = GiaiMaSafe(cvMaHoa);
                        if (!string.IsNullOrWhiteSpace(cv)) list.Add(cv);
                    }
                    s.DsChucVuGiaiMa = list;
                }
            }
            catch { }
            // 1 lượt duyệt duy nhất thay cho 4-5 lượt LINQ AsEnumerable trước đây
            int oH = dt.Columns.IndexOf("HoVaTen");
            int oD = dt.Columns.IndexOf("DonVi");
            int oP = dt.Columns.IndexOf("PhanLoai");
            var setDv = new HashSet<string>();
            var setPl = new HashSet<string>();
            foreach (DataRow r in dt.Rows)
            {
                string? hoTen = oH >= 0 ? r.Field<string>(oH) : null;
                string? donVi = oD >= 0 ? r.Field<string>(oD) : null;
                if (!string.IsNullOrWhiteSpace(donVi) && setDv.Add(donVi))
                    s.DonViTrongCsdl.Add(donVi);
                if (string.IsNullOrWhiteSpace(hoTen)) continue;
                s.SoLuongThucTe++;
                string? pl = oP >= 0 ? r.Field<string>(oP)?.Trim() : null;
                string plFinal = string.IsNullOrWhiteSpace(pl) ? Module_HeThong.PL_KHONG_PL : pl;
                if (setPl.Add(plFinal)) s.PhanLoaiDistinct.Add(plFinal);
            }
            return s;
        }
        public async Task ReloadDuLieu()
        {
            if (IsDisposed || !IsHandleCreated) return;
            await _reloadGate.WaitAsync();
            try
            {
                if (IsDisposed || !IsHandleCreated) return;
                await ReloadDuLieuNoiBo();
            }
            finally
            {
                _reloadGate.Release();
            }
        }
        private async Task ReloadDuLieuNoiBo()
        {
            try
            {
                // 1. CẬP NHẬT GIAO DIỆN TRƯỚC KHI TẢI
                UIHelper.SafeInvoke(this, () =>
                {
                    toolStripStatusLabel1.Text = "Đang tải và xử lý dữ liệu...";
                    kryptonButton_RefershCSDL.Enabled = false;
                });
                DataTable dtKetQua;
                bool isCheDoNam = false;
                // 2. LẤY DỮ LIỆU & ĐỌC CẤU HÌNH
                if (DataCache.IsLoaded)
                {
                    // Đọc cấu hình chạy song song với việc Copy() bảng
                    var taskCheDo = Task.Run(() => KiemTraCheDoXetThiDuaNam());
                    // DataCache.GetDanhSach() ĐÃ trả bản sao (Copy trong khóa) => sửa/sắp xếp thoải mái, không làm bẩn bộ đệm.
                    // Bản trước gọi thêm .Copy() lần 2 => chép trùng cả bảng ~6.000 dòng mỗi lần nạp Form6. Bộ đệm bị xóa giữa chừng => báo lỗi rõ ràng (bản trước: NullReferenceException), không hiện nhầm lưới trống.
                    // ★ Luôn sắp theo STT: cache có thể không theo thứ tự STT -> đây là nguyên nhân Form6 "load sai"
                    // Copy + sắp xếp chạy ở luồng nền (trước đây chạy ngay trên luồng giao diện -> khựng khi dữ liệu lớn)
                    var bangCache = DataCache.GetDanhSach() ?? throw new InvalidOperationException("Bộ đệm danh sách vừa được làm mới, vui lòng tải lại.");
                    dtKetQua = await Task.Run(() => SapXepTheoSTT(bangCache));
                    isCheDoNam = await taskCheDo;
                }
                else
                {
                    dtKetQua = await Task.Run(() =>
                    {
                        isCheDoNam = KiemTraCheDoXetThiDuaNam();
                        return XuLyDuLieuNgam(_csdl2Path);
                    });
                }
                _isCheDoNam = isCheDoNam;
                if (dtKetQua == null) dtKetQua = new DataTable();
                // BẢO KÊ DÒNG ẢO + nạp sẵn mọi dữ liệu phụ ngay trên luồng nền (không chặn UI)
                var snap = await Task.Run(() =>
                {
                    ThemDongTrongAnToan(dtKetQua);
                    var sn = TaoSnapshot(dtKetQua);
                    sn.BoLoc = BoLocDanhSach.TaoTu(dtKetQua);   // chuẩn hóa tên/đơn vị/phân loại 1 lần cho mọi lần lọc sau
                    return sn;
                });
                int soLuongThucTe = snap.SoLuongThucTe;
                // 3. ĐỔ DỮ LIỆU LÊN GIAO DIỆN
                UIHelper.SafeInvoke(this, () =>
                {
                    bool daTamDungVe = false;
                    try
                    {
                        // ★ Gán bảng mới NGAY TRÊN LUỒNG UI, cùng khối với việc đặt RowCount.
                        //   (Cũ: gán ở luồng nền trước khi lưới đổi RowCount -> lưới ảo (virtual) vẽ giữa chừng
                        //   bằng bảng mới nhưng số dòng cũ => dòng lệch/sai.)
                        dtDanhSachGoc = dtKetQua;
                        _boLoc = snap.BoLoc;
                        _hienThi = null;   // bảng mới: hiển thị đủ mọi dòng cho tới khi ApplyFilter chạy ở cuối khối này
                        _snap = snap;
                        TamDungVeLuoi();
                        daTamDungVe = true;
                        if (_aesCache.Count > TRAN_CACHE_AES) _aesCache.Clear();
                        if (kryptonDataGridView1.Columns.Contains("GhiChu"))
                            kryptonDataGridView1.Columns["GhiChu"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        kryptonDataGridView1.DataSource = null;
                        TaoCauTrucCotGrid();
                        XayDungBanDoCot(); // phải có TRƯỚC khi gán RowCount (CellValueNeeded dùng bản đồ này)
                        DatSoDongLuoi(SoDongHienThi);
                        DoiTenCotTiengViet();
                        CanChinhBang();
                        LoadComboBoxPhanLoai();
                        if (!_menuDaTichHop)
                        {
                            Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1);
                            _menuDaTichHop = true;
                        }
                        // Xử lý chống dồn ứ sự kiện (Memory Leak)
                        Module_HeThong.SuKienThayDoiCheDoXetThiDua -= OnCheDoThayDoi;
                        Module_HeThong.SuKienThayDoiCheDoXetThiDua += OnCheDoThayDoi;
                        if (kryptonDataGridView1.Columns.Contains("GhiChu"))
                            kryptonDataGridView1.Columns["GhiChu"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                        kryptonDataGridView1.CurrentCell = null;
                        toolStripStatusLabel1.Text = $"Tổng cộng: {soLuongThucTe} {Module_HeThong.Tu_dong_chi}";
                        HoanTatLoadGiaoDien();
                        ClearThongTin();
                        _isUpdatingCombo = false;
                        ApplyFilter();   // đã tự cập nhật thống kê bên trong
                        KiemTraDuLieu();
                        NapNongCacheGiaiMaNen();   // giải mã trước các cột còn lại ở luồng nền
                    }
                    finally
                    {
                        _snap = null; // tránh dùng dữ liệu cũ ở lần gọi khác
                        if (daTamDungVe) MoLaiVeLuoi();
                    }
                });
                // Dữ liệu DanhSach đã đổi -> Form15 nhận Ghi chú mới ngay
                ThongBaoForm15CapNhatGhiChu();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ReloadDuLieu lỗi: " + ex.Message);
                UIHelper.SafeInvoke(this, () =>
                {
                    MessageBox.Show($"Lỗi load dữ liệu:\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            }
            finally
            {
                // 4. MỞ KHÓA BẢN VẼ SAU CÙNG
                UIHelper.SafeInvoke(this, () =>
                {
                    if (!IsDisposed && IsHandleCreated)
                    {
                        kryptonDataGridView1.Invalidate(); // Refresh() ép vẽ đồng bộ -> thay bằng Invalidate
                        kryptonButton_RefershCSDL.Enabled = true;
                    }
                });
            }
        }
        /// <summary>
        /// Sắp xếp thống nhất theo STT ASC, ID ASC (khớp đúng "STT ASC, ID ASC" của Form60).
        /// Dùng cho cả đường đọc DB lẫn đường lấy từ DataCache.
        /// </summary>
        private static DataTable SapXepTheoSTT(DataTable? dt)
        {
            if (dt == null) return new DataTable();
            if (dt.Rows.Count < 2) return dt;
            int oStt = dt.Columns.IndexOf("STT");
            if (oStt < 0) return dt;
            int oId = dt.Columns.IndexOf("ID");
            static long DocSo(DataRow r, int ord)
            {
                if (ord < 0) return 0;
                object v = r[ord];
                if (v == DBNull.Value) return 0;
                string s = Convert.ToString(v, CultureInfo.InvariantCulture) ?? "";
                return long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long n) ? n : 0;
            }
            return dt.AsEnumerable()
                .OrderBy(r => DocSo(r, oStt))
                .ThenBy(r => DocSo(r, oId))
                .CopyToDataTable();
        }
        private DataTable XuLyDuLieuNgam(string csdlPath)
        {
            if (string.IsNullOrWhiteSpace(csdlPath) || !File.Exists(csdlPath))
                throw new Exception("Không tìm thấy CSDL2!");
            using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(csdlPath));
            conn.Open();
            using var cmd = new SqliteCommand($"SELECT * FROM {Module_TaiKhoan.BangDanhSach}", conn);
            using var reader = cmd.ExecuteReader();
            DataTable dtRaw = new DataTable();
            dtRaw.Load(reader);
            string danhSachCot = string.Join(", ",
                dtRaw.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            System.Diagnostics.Debug.WriteLine($"[DEBUG] CSDL có các cột: {danhSachCot}");
            if (dtRaw.Rows.Count == 0)
                return dtRaw;
            // CHỈ GIẢI MÃ NHỮNG CỘT CẦN THIẾT
            string[] cotCanGiaiMaNgay = { "HoVaTen", "DonVi", "PhanLoai" };
            int n = dtRaw.Rows.Count;
            dtRaw.BeginLoadData();
            foreach (var c in cotCanGiaiMaNgay)
            {
                int ord = dtRaw.Columns.IndexOf(c);
                if (ord < 0)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[CẢNH BÁO] Không tìm thấy cột: {c}. Đã bỏ qua giải mã.");
                    continue;
                }
                // Đọc ra mảng 1 lần (tránh tra cột theo tên cho từng dòng)
                var raw = new string[n];
                for (int i = 0; i < n; i++)
                    raw[i] = dtRaw.Rows[i][ord]?.ToString() ?? "";
                if (GIAI_MA_SONG_SONG)
                {
                    Parallel.For(0, n, i =>
                    {
                        if (raw[i].Length > 0)
                            raw[i] = GiaiMaSafeCoCache(raw[i]);
                    });
                }
                else
                {
                    for (int i = 0; i < n; i++)
                        if (raw[i].Length > 0)
                            raw[i] = GiaiMaSafeCoCache(raw[i]);
                }
                for (int i = 0; i < n; i++)
                    if (raw[i].Length > 0)
                        dtRaw.Rows[i][ord] = raw[i];
            }
            dtRaw.EndLoadData();
            // Sắp xếp STT theo ID, thống nhất với Form60.
            return SapXepTheoSTT(dtRaw);
        }
        // [E] HÀM MỚI: Form60 gọi sau khi đã ghi xong CSDL2 (gọi từ luồng UI)
        // -------------------------------------------------------------------------------------
        public async Task NapLaiSauKhiForm60DongAsync()
        {
            DataCache.Clear(); // luôn bỏ cache cũ, kể cả khi form đang đóng/ẩn
            if (IsDisposed || !IsHandleCreated) return;
            await ReloadDuLieu();
            if (IsDisposed || !IsHandleCreated) return;
            CapNhatThongKeToanBoQuanSo();
            try
            {
                await Module_BieuDoTronTrangChu.CapNhatBieuDoForm4Async();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi cập nhật biểu đồ Form4: " + ex.Message);
            }
        }
        private void HoanTatLoadGiaoDien()
        {
            try
            {
                LoadComboBoxDonVi();
                NapDanhSachDonVi();
                LoadComboBoxChucVu();
                LoadComboBoxXepLoaiThiDua();
                // Thống kê sẽ do ApplyFilter() (chạy ngay sau trong ReloadDuLieu) cập nhật -> bỏ lượt đếm trùng
                if (dtDanhSachGoc != null && dtDanhSachGoc.DefaultView.Count > 0)
                {
                    EnsureLastRowVisible(kryptonDataGridView1);
                }
                else
                {
                    CapNhatThongKePhanLoai(null);
                }
            }
            finally
            {
                kryptonDataGridView1.ResumeLayout();
            }
        }
        private void LoadComboBoxDonVi()
        {
            if (dtDanhSachGoc == null || dtDanhSachGoc.Rows.Count == 0) return;
            if (!dtDanhSachGoc.Columns.Contains("DonVi")) return;
            _isUpdatingCombo = true;
            comboBox_TimKiemDonVi.BeginUpdate();
            try
            {
                string giaTriCu = comboBox_TimKiemDonVi.Text;
                comboBox_TimKiemDonVi.Items.Clear();
                string[] thuTuUuTien = (_snap?.UuTien ?? Module_DonVi.LayDanhSachDonViUuTienArray())
                                           .Where(s => !string.IsNullOrWhiteSpace(s))
                                           .ToArray();
                List<string> dsDonViTrongCSDL = _snap?.DonViTrongCsdl ?? dtDanhSachGoc.AsEnumerable()
                    .Select(r => r.Field<string>("DonVi"))
                    .Where(val => !string.IsNullOrWhiteSpace(val))
                    .Distinct()
                    .ToList()!;
                var setCsdl = new HashSet<string>(dsDonViTrongCSDL);
                var dsFinal = thuTuUuTien.Where(x => setCsdl.Contains(x)).ToList();
                var dsConLai = dsDonViTrongCSDL.Except(dsFinal).OrderBy(x => x);
                dsFinal.AddRange(dsConLai);
                var items = new List<object>(dsFinal.Count + 1) { Module_HeThong.Tat_Ca };
                items.AddRange(dsFinal);
                comboBox_TimKiemDonVi.Items.AddRange(items.ToArray());
                if (!string.IsNullOrEmpty(giaTriCu) && comboBox_TimKiemDonVi.Items.Contains(giaTriCu))
                {
                    comboBox_TimKiemDonVi.SelectedItem = giaTriCu;
                    comboBox_TimKiemDonVi.Text = giaTriCu; // Fix lỗi hiển thị Krypton
                }
                else
                {
                    comboBox_TimKiemDonVi.SelectedIndex = 0;
                }
            }
            finally
            {
                comboBox_TimKiemDonVi.EndUpdate();
                _isUpdatingCombo = false;
            }
        }
        private void NapDanhSachDonVi()
        {
            IEnumerable<string> dsDonVi = _snap?.DsDonViModule ?? (IEnumerable<string>)Module_DonVi.GetDanhSachDonVi();
            var thuTuUuTien = _snap?.UuTien ?? Module_DonVi.LayDanhSachDonViUuTienArray();
            // 1. Sắp xếp danh sách đơn vị theo thứ tự ưu tiên
            var sortedList = dsDonVi.OrderBy(item =>
            {
                int index = Array.IndexOf(thuTuUuTien, item);
                return index == -1 ? int.MaxValue : index;
            }).ToList();
            // 2. Chèn chuỗi rỗng "" vào đầu danh sách
            sortedList.Insert(0, "");
            // 3. Nếu STT hoặc Họ và tên rỗng -> Ép chọn giá trị rỗng ""
            bool sttRong = string.IsNullOrWhiteSpace(textBox_STT.Text);
            bool hoTenRong = string.IsNullOrWhiteSpace(textBox_HoVaTen.Text);
            var currentValue = (sttRong || hoTenRong) ? "" : textBox_DonVi.Text;
            // 4. Gán DataSource
            textBox_DonVi.DataSource = null;
            textBox_DonVi.DataSource = sortedList;
            // 5. Gán lại giá trị được chọn
            textBox_DonVi.Text = sortedList.Contains(currentValue) ? currentValue : "";
        }
        private void LoadComboBoxChucVu()
        {
            if (comboBox_ChucVu == null) return;
            comboBox_ChucVu.BeginUpdate();
            try
            {
                comboBox_ChucVu.Items.Clear();
                if (_snap?.DsChucVuGiaiMa != null)
                {
                    // Đã đọc + giải mã sẵn trên luồng nền
                    comboBox_ChucVu.Items.AddRange(_snap.DsChucVuGiaiMa.Cast<object>().ToArray());
                    return;
                }
                var ds = Module_ChucVu.GetDanhSachChucVu();
                if (ds == null) return;
                var list = new List<object>();
                foreach (var cvMaHoa in ds)
                {
                    if (string.IsNullOrWhiteSpace(cvMaHoa)) continue;
                    string cvGiaiMa = GiaiMaSafe(cvMaHoa);
                    if (!string.IsNullOrWhiteSpace(cvGiaiMa)) list.Add(cvGiaiMa);
                }
                comboBox_ChucVu.Items.AddRange(list.ToArray());
            }
            finally
            {
                comboBox_ChucVu.EndUpdate();
            }
        }
        private void LoadComboBoxXepLoaiThiDua()
        {
            if (dtDanhSachGoc == null || dtDanhSachGoc.Rows.Count == 0 || comboBox_XepLoaiThiDua == null || comboBox_XepLoaiThiDua.IsDisposed) return;
            _isUpdatingCombo = true;
            try
            {
                string? giaTriCuValue = null;
                if (comboBox_XepLoaiThiDua.SelectedItem is ComboboxItem itemCu) giaTriCuValue = itemCu.Value;
                else if (comboBox_XepLoaiThiDua.SelectedValue != null) giaTriCuValue = comboBox_XepLoaiThiDua.SelectedValue.ToString();
                else if (!string.IsNullOrWhiteSpace(comboBox_XepLoaiThiDua.Text)) giaTriCuValue = comboBox_XepLoaiThiDua.Text.Trim();
                List<string> dsPhanLoai = _snap?.PhanLoaiDistinct ?? dtDanhSachGoc.AsEnumerable()
                    .Where(r => !string.IsNullOrWhiteSpace(r.Field<string>("HoVaTen")))
                    .Select(r =>
                    {
                        string? phanLoai = r.Field<string>("PhanLoai")?.Trim();
                        return string.IsNullOrWhiteSpace(phanLoai) ? Module_HeThong.PL_KHONG_PL : phanLoai;
                    })
                    .Distinct()
                    .ToList();
                var thuTuChuan = new List<string>
        {
            Module_HeThong.Loai_1,
            Module_HeThong.Loai_2,
            Module_HeThong.Loai_3,
            Module_HeThong.Loai_4,
            Module_HeThong.PL_KHONG_PL
        };
                var dsFinal = dsPhanLoai.OrderBy(x =>
                {
                    int index = thuTuChuan.IndexOf(x);
                    return index >= 0 ? index : int.MaxValue;
                }).ToList();
                string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? string.Empty;
                bool laTanBinh = phienBan.LaCheDoTanBinh();
                bool isCBCSAndNam = !laTanBinh && _isCheDoNam;
                string LayTenHienThi(string value)
                {
                    if (value == Module_HeThong.Tat_Ca) return Module_HeThong.Tat_Ca;
                    if (!isCBCSAndNam) return value;
                    if (value == Module_HeThong.Loai_1) return Module_HeThong.PL_CSTD;
                    if (value == Module_HeThong.Loai_2) return Module_HeThong.PL_CSTT;
                    if (value == Module_HeThong.Loai_3) return Module_HeThong.PL_HTNV;
                    if (value == Module_HeThong.Loai_4) return Module_HeThong.PL_KHTNV;
                    return value;
                }
                comboBox_XepLoaiThiDua.BeginUpdate();
                try
                {
                    comboBox_XepLoaiThiDua.Items.Clear();
                    var items = new List<object>();
                    if (dsFinal.Count > 1)
                        items.Add(new ComboboxItem(LayTenHienThi(Module_HeThong.Tat_Ca), Module_HeThong.Tat_Ca));
                    foreach (string phanLoai in dsFinal)
                        items.Add(new ComboboxItem(LayTenHienThi(phanLoai), phanLoai));
                    comboBox_XepLoaiThiDua.Items.AddRange(items.ToArray());
                }
                finally
                {
                    comboBox_XepLoaiThiDua.EndUpdate();
                }
                ComboboxItem? itemCanChon = null;
                if (!string.IsNullOrWhiteSpace(giaTriCuValue))
                {
                    itemCanChon = comboBox_XepLoaiThiDua.Items.OfType<ComboboxItem>().FirstOrDefault(x =>
                        string.Equals(x.Value, giaTriCuValue, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x.Text, giaTriCuValue, StringComparison.OrdinalIgnoreCase));
                }
                if (itemCanChon != null) comboBox_XepLoaiThiDua.SelectedItem = itemCanChon;
                else if (comboBox_XepLoaiThiDua.Items.Count > 0) comboBox_XepLoaiThiDua.SelectedIndex = 0;
                else comboBox_XepLoaiThiDua.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi tại LoadComboBoxXepLoaiThiDua]: {ex}");
            }
            finally
            {
                _isUpdatingCombo = false;
            }
        }
        private void LoadComboBoxPhanLoai()
        {
            comboBox_PhanLoai.Items.Clear();
            // Đã đọc (ép đọc lại CSDL2, không cache) sẵn trên luồng nền
            bool isAnhXa = _snap?.IsAnhXa ?? Module_HeThong.IsKichHoatAnhXa(true);
            if (isAnhXa)
            {
                comboBox_PhanLoai.Items.Add(new ComboboxItem(Module_HeThong.PL_CSTD, Module_HeThong.Loai_1));
                comboBox_PhanLoai.Items.Add(new ComboboxItem(Module_HeThong.PL_CSTT, Module_HeThong.Loai_2));
                comboBox_PhanLoai.Items.Add(new ComboboxItem(Module_HeThong.PL_HTNV, Module_HeThong.Loai_3));
                comboBox_PhanLoai.Items.Add(new ComboboxItem(Module_HeThong.PL_KHTNV, Module_HeThong.Loai_4));
                comboBox_PhanLoai.Items.Add(new ComboboxItem("Không PL", "Không PL"));
            }
            else
            {
                comboBox_PhanLoai.Items.Add(new ComboboxItem(Module_HeThong.Loai_1, Module_HeThong.Loai_1));
                comboBox_PhanLoai.Items.Add(new ComboboxItem(Module_HeThong.Loai_2, Module_HeThong.Loai_2));
                comboBox_PhanLoai.Items.Add(new ComboboxItem(Module_HeThong.Loai_3, Module_HeThong.Loai_3));
                comboBox_PhanLoai.Items.Add(new ComboboxItem(Module_HeThong.Loai_4, Module_HeThong.Loai_4));
                comboBox_PhanLoai.Items.Add(new ComboboxItem("Không PL", "Không PL"));
            }
            comboBox_PhanLoai.SelectedIndex = -1;
            comboBox_PhanLoai.Text = "";
        }
        // Virtual Mode: hàm này chạy cho TỪNG Ô khi vẽ/cuộn -> phải cực nhẹ
        private void KryptonDataGridView1_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            DataRow? dongDl = LayDongHienThi(e.RowIndex);
            if (dongDl == null) return;
            int ci = e.ColumnIndex;
            if ((uint)ci >= (uint)_mapOrdinalCot.Length) return;
            try
            {
                int ord = _mapOrdinalCot[ci];
                if (ord < 0)
                {
                    e.Value = "[Thiếu cột trong DB]";
                    return;
                }
                string rawValue = dongDl[ord]?.ToString() ?? "";
                if (_cotDaGiaiMa[ci])
                {
                    // ÁNH XẠ HIỂN THỊ CỘT PHÂN LOẠI KHI Ở CHẾ ĐỘ "NĂM"
                    if (ci == _gridColPhanLoai && _isCheDoNam)
                    {
                        e.Value = rawValue switch
                        {
                            Module_HeThong.Loai_1 => Module_HeThong.PL_CSTD,
                            Module_HeThong.Loai_2 => Module_HeThong.PL_CSTT,
                            Module_HeThong.Loai_3 => Module_HeThong.PL_HTNV,
                            Module_HeThong.Loai_4 => Module_HeThong.PL_KHTNV,
                            Module_HeThong.PL_KHONG_PL => Module_HeThong.PL_KHONG_PL,
                            _ => rawValue
                        };
                    }
                    else
                    {
                        e.Value = rawValue;
                    }
                }
                else
                {
                    e.Value = GiaiMaSafeCoCache(rawValue);
                }
            }
            catch { e.Value = "..."; }
        }
        private void KryptonDataGridView1_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            // So sánh chỉ số int thay vì tra tên cột (chuỗi) cho mỗi ô
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.ColumnIndex != _gridColHoTen)
                return;
            if (sender is not DataGridView)
                return;
            if (dtDanhSachGoc == null || _ordPhanLoai < 0)
                return;
            int rowIndex = e.RowIndex;
            DataRow? dongDl = LayDongHienThi(rowIndex);
            if (dongDl == null)
                return;
            string phanLoai;
            try
            {
                object value = dongDl[_ordPhanLoai];
                phanLoai = value?.ToString()?.Trim() ?? string.Empty;
            }
            catch
            {
                return;
            }
            if (!string.Equals(phanLoai, Module_HeThong.Loai_1, StringComparison.OrdinalIgnoreCase))
                return;
            if (_iconStar == null)
                return;
            e.Paint(
                e.CellBounds,
                DataGridViewPaintParts.Background |
                DataGridViewPaintParts.SelectionBackground |
                DataGridViewPaintParts.Focus);
            const int iconSize = 16;
            const int paddingLeft = 6;
            const int paddingText = 6;
            int iconX = e.CellBounds.X + paddingLeft;
            int iconY = e.CellBounds.Y + ((e.CellBounds.Height - iconSize) / 2);
            try
            {
                e.Graphics!.DrawImage(_iconStar, iconX, iconY, iconSize, iconSize);
            }
            catch
            {
                return;
            }
            string hoTen = Convert.ToString(e.Value) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(hoTen))
            {
                int textX = iconX + iconSize + paddingText;
                Rectangle textBounds = new Rectangle(textX, e.CellBounds.Y, e.CellBounds.Right - textX, e.CellBounds.Height);
                Color textColor =
                    (e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected
                    ? e.CellStyle!.SelectionForeColor
                    : e.CellStyle!.ForeColor;
                TextRenderer.DrawText(
                    e.Graphics, hoTen, e.CellStyle.Font, textBounds, textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding |
                    TextFormatFlags.PreserveGraphicsClipping);
            }
            e.Handled = true;
        }
        private void kryptonDataGridView1_RowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv == null) return;
            // QUAN TRỌNG: KHÔNG dùng dgv.Rows[e.RowIndex] (làm "unshare" dòng -> tốn RAM + chậm)
            // và KHÔNG dùng Cells["HoVaTen"] (tra tên + kích hoạt CellValueNeeded). Đọc thẳng từ DataView.
            DataRow? dongDl = LayDongHienThi(e.RowIndex);
            if (dongDl == null) return;
            bool laDongAo = false;
            if (_ordHoVaTen >= 0)
            {
                object? giaTriHoTen = dongDl[_ordHoVaTen];
                if (giaTriHoTen == null || string.IsNullOrWhiteSpace(giaTriHoTen.ToString()))
                    laDongAo = true;
            }
            if (!laDongAo)
            {
                string stt = (e.RowIndex + 1).ToString();
                var bounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, dgv.RowHeadersWidth, e.RowBounds.Height);
                TextRenderer.DrawText(e.Graphics, stt, dgv.Font, bounds, dgv.RowHeadersDefaultCellStyle.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
        // Resize: gom nhiều sự kiện liên tiếp thành 1 lần căn chỉnh
        private void kryptonDataGridView1_Resize(object? sender, EventArgs e)
        {
            if (_timerResize == null)
            {
                _timerResize = new System.Windows.Forms.Timer { Interval = 90 };
                _timerResize.Tick += (s, ev) =>
                {
                    _timerResize!.Stop();
                    if (!IsDisposed && IsHandleCreated && Visible) CanChinhBang(false);
                };
            }
            _timerResize.Stop();
            _timerResize.Start();
        }
        // ===== Giảm chớp: bật DoubleBuffered cho container (không đụng TabControl/Krypton) =====
        private static void BatDoubleBufferDeQuy(Control goc)
        {
            foreach (Control c in goc.Controls)
            {
                if (c is TableLayoutPanel || c is GroupBox || c is Panel || c is SplitContainer || c is TabPage)
                {
                    typeof(Control).GetProperty("DoubleBuffered",
                        BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(c, true);
                }
                BatDoubleBufferDeQuy(c);   // SplitContainer.Controls gồm 2 SplitterPanel (là Panel) nên được phủ hết
            }
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Module_HeThong.SuKienThayDoiCheDoXetThiDua -= OnCheDoThayDoi;
            Module_HeThong.SuKienThayDoiThoiGianHeThong -= OnCheDoThayDoi;
            if (timerLocDuLieu != null)
            {
                timerLocDuLieu.Stop();
                timerLocDuLieu.Dispose();
                timerLocDuLieu = null;
            }
            if (_timerResize != null)
            {
                _timerResize.Stop();
                _timerResize.Dispose();
                _timerResize = null;
            }
            Instance = null;
            base.OnFormClosed(e);
        }
        // ===== BỘ LỌC =====
        // ApplyFilter: vỏ ngoài (tắt vẽ lưới + bắt lỗi). Phần việc thật nằm ở ApDungBoLoc.
        private void ApplyFilter()
        {
            if (dtDanhSachGoc == null || _isUpdatingCombo)
                return;
            bool daTamDungVe = false;
            try
            {
                if (kryptonDataGridView1.IsDisposed)
                    return;
                TamDungVeLuoi();
                daTamDungVe = true;
                ApDungBoLoc();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Lỗi ApplyFilter", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.WriteLine(ex);
            }
            finally
            {
                try
                {
                    if (daTamDungVe) MoLaiVeLuoi();   // chỉ bật vẽ lại khi lớp ngoài cùng xong (bộ đếm lồng nhau)
                }
                catch
                {
                }
            }
        }
        // Lõi lọc: đọc điều kiện từ giao diện -> lọc trên mảng chuỗi chuẩn hóa sẵn -> cập nhật số dòng lưới + thống kê.
        private void ApDungBoLoc()
        {
            var dt = dtDanhSachGoc;
            var grid = kryptonDataGridView1;
            if (dt == null || grid == null || grid.IsDisposed) return;
            foreach (string col in new[] { "HoVaTen", "DonVi", "PhanLoai" })
            {
                if (!dt.Columns.Contains(col))
                {
                    throw new Exception(
                        $"Thiếu cột '{col}' trong dtDanhSachGoc.\n\n" +
                        $"Các cột hiện có:\n" +
                        string.Join(", ", dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName)));
                }
            }
            // Chỉ mục chưa có / lệch số dòng so với bảng (bảng vừa đổi) -> dựng lại (vài chục mili-giây cho hàng chục nghìn dòng)
            var boLoc = _boLoc;
            if (boLoc == null || boLoc.SoDong != dt.Rows.Count)
                _boLoc = boLoc = BoLocDanhSach.TaoTu(dt);
            // ---- Đọc điều kiện ----
            string tuKhoa = "";
            if (!_dangSetPlaceholder && textBox_TimKiemTheoTen.Text != PLACEHOLDER_TIMKIEM)
                tuKhoa = (textBox_TimKiemTheoTen.Text ?? "").Trim();
            string donViFilter = comboBox_TimKiemDonVi?.Text?.Trim() ?? "";
            string phanLoaiFilter = comboBox_XepLoaiThiDua?.Text?.Trim() ?? "";
            string? donVi = null;
            if (donViFilter.Length > 0 && !donViFilter.Equals(Module_HeThong.Tat_Ca, StringComparison.OrdinalIgnoreCase))
                donVi = donViFilter;
            string? phanLoaiDb = null;
            bool laKhongPL = false;
            if (phanLoaiFilter.Length > 0 && !phanLoaiFilter.Equals(Module_HeThong.Tat_Ca, StringComparison.OrdinalIgnoreCase))
            {
                // Chế độ "Năm": ô lọc hiển thị CSTĐ/CSTT/... nhưng CSDL lưu Loại 1/2/... (dùng cờ đã nạp sẵn, khớp với cột hiển thị trên lưới)
                string giaTri = phanLoaiFilter;
                if (_isCheDoNam)
                {
                    if (giaTri.Equals(Module_HeThong.PL_CSTD, StringComparison.OrdinalIgnoreCase)) giaTri = Module_HeThong.Loai_1;
                    else if (giaTri.Equals(Module_HeThong.PL_CSTT, StringComparison.OrdinalIgnoreCase)) giaTri = Module_HeThong.Loai_2;
                    else if (giaTri.Equals(Module_HeThong.PL_HTNV, StringComparison.OrdinalIgnoreCase)) giaTri = Module_HeThong.Loai_3;
                    else if (giaTri.Equals(Module_HeThong.PL_KHTNV, StringComparison.OrdinalIgnoreCase)) giaTri = Module_HeThong.Loai_4;
                }
                if (giaTri.Equals("Không PL", StringComparison.OrdinalIgnoreCase) ||
                    giaTri.Equals(Module_HeThong.PL_KHONG_PL, StringComparison.OrdinalIgnoreCase))
                    laKhongPL = true;
                else
                    phanLoaiDb = giaTri;
            }
            // ---- Lọc (1 vòng for trên mảng chuỗi) ----
            int[]? moi = boLoc.Loc(tuKhoa, donVi, phanLoaiDb, laKhongPL, new[] { "Không PL", Module_HeThong.PL_KHONG_PL });
            int[]? cu = _hienThi;
            bool doi = !BoLocDanhSach.CungTap(cu, moi, dt.Rows.Count);
            // Dòng đang chọn (nếu có) thuộc nguồn nào trước khi đổi -> sau khi đổi mà vị trí đó trỏ sang người khác thì bỏ chọn
            int viTriChon = grid.CurrentCell?.RowIndex ?? -1;
            int nguonTruoc = -1;
            if (viTriChon >= 0)
                nguonTruoc = cu == null ? viTriChon : (viTriChon < cu.Length ? cu[viTriChon] : -1);
            _hienThi = moi;
            int soDong = SoDongHienThi;
            if (grid.RowCount != soDong) DatSoDongLuoi(soDong);
            if (doi && viTriChon >= 0 && grid.CurrentCell != null)
            {
                int nguonSau = moi == null ? viTriChon : (viTriChon < moi.Length ? moi[viTriChon] : -1);
                if (nguonSau != nguonTruoc)
                {
                    grid.CurrentCell = null;
                    grid.ClearSelection();
                }
            }
            // Vùng chọn nhiều giữ theo VỊ TRÍ dòng -> sau khi lọc lại sẽ trỏ sang người khác: bỏ chọn cho an toàn
            if (doi && grid.Rows.GetRowCount(DataGridViewElementStates.Selected) > 1)
            {
                grid.CurrentCell = null;
                grid.ClearSelection();
            }
            if (doi) grid.Invalidate();
            var tk = ThucThiThongKeHang(dt, moi);
            CapNhatGiaoDienThongKe(tk.TongSo, tk.ChiTiet);
        }
        private async void kryptonButton_RefershCSDL_Click(object? sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref _dangRefresh, 1) == 1)
                return;
            _refreshCts?.Cancel();
            _refreshCts?.Dispose();
            _refreshCts = new CancellationTokenSource();
            CancellationToken token = _refreshCts.Token;
            Form_Loading? frmLoad = null;
            string textGoc = kryptonButton_RefershCSDL.Values.Text;
            Image? imageGoc = kryptonButton_RefershCSDL.Values.Image;
            Action<int> updateProgressBarSmooth = (val) =>
            {
                if (!IsDisposed && IsHandleCreated && toolStripProgressBar1_LamMoi != null)
                {
                    int clamped = Math.Clamp(val, 0, 100);
                    if (clamped == 100)
                    {
                        toolStripProgressBar1_LamMoi.Value = 100;
                        toolStripProgressBar1_LamMoi.Value = 99;
                        toolStripProgressBar1_LamMoi.Value = 100;
                    }
                    else
                    {
                        toolStripProgressBar1_LamMoi.Value = clamped;
                    }
                }
            };
            var progress = new Progress<int>(percent => updateProgressBarSmooth(percent));
            int mucTieuTienTrinh = 0;
            CancellationTokenSource? progressCts = null;
            Task? progressTask = null;
            try
            {
                if (IsDisposed || !IsHandleCreated)
                    return;
                bool coDuLieu = dtDanhSachGoc?.AsEnumerable()
                    .Any(r => !string.IsNullOrWhiteSpace(r.Field<string>("HoVaTen"))) == true;
                if (!coDuLieu && _isInit)
                {
                    toolStripStatusLabel1.Text = "Danh sách hiện đang trống";
                    return;
                }
                int tongDong = dtDanhSachGoc?.Rows.Count ?? 0;
                bool laDuLieuLon = tongDong >= 600 || dtDanhSachGoc == null;
                if (toolStripProgressBar1_LamMoi != null)
                {
                    toolStripProgressBar1_LamMoi.Minimum = 0;
                    toolStripProgressBar1_LamMoi.Maximum = 100;
                    toolStripProgressBar1_LamMoi.Value = 0;
                    toolStripProgressBar1_LamMoi.Visible = true;
                }
                kryptonButton_RefershCSDL.Enabled = false;
                kryptonButton_RefershCSDL.Values.Text = "Đang tải...";
                kryptonButton_RefershCSDL.Values.Image = null;
                toolStripStatusLabel1.Text = "Đang kiểm tra cơ sở dữ liệu...";
                // ANIMATION: Task.Delay(4) thực tế trên Windows ~15ms nhưng vẫn "bơm" tin nhắn dồn dập
                // vào UI thread. Dùng nhịp 16ms và CHỈ Report khi giá trị thay đổi -> tốc độ y hệt, UI nhẹ hơn.
                progressCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                progressTask = Task.Run(async () =>
                {
                    int hienTai = 0;
                    int daBao = -1;
                    while (!progressCts.Token.IsCancellationRequested)
                    {
                        int mucTieu = Volatile.Read(ref mucTieuTienTrinh);
                        if (hienTai < mucTieu)
                        {
                            hienTai = Math.Min(hienTai + 3, mucTieu);
                        }
                        else if (hienTai >= 50 && hienTai < 90 && mucTieu >= 50)
                        {
                            hienTai = Math.Min(hienTai + 2, 90);
                        }
                        if (hienTai != daBao)
                        {
                            daBao = hienTai;
                            ((IProgress<int>)progress).Report(hienTai);
                        }
                        await Task.Delay(16, progressCts.Token).ConfigureAwait(false);
                    }
                }, progressCts.Token);
                Volatile.Write(ref mucTieuTienTrinh, 25);
                if (laDuLieuLon)
                {
                    frmLoad = new Form_Loading("Đang kiểm tra cơ sở dữ liệu...");
                    frmLoad.Show(this);
                    await Task.Yield();
                }
                token.ThrowIfCancellationRequested();
                string csdl2 = _csdl2Path;
                bool dbHopLe = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();
                    if (string.IsNullOrWhiteSpace(csdl2))
                        return false;
                    if (!File.Exists(csdl2))
                        return false;
                    return KiemTraSqliteMoDuoc(csdl2);
                }, token);
                token.ThrowIfCancellationRequested();
                if (!dbHopLe)
                {
                    toolStripStatusLabel1.Text = "CSDL lỗi hoặc không tồn tại";
                    MessageBox.Show("Không thể kết nối cơ sở dữ liệu.", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Volatile.Write(ref mucTieuTienTrinh, 45);
                toolStripStatusLabel1.Text = "Đang nạp dữ liệu...";
                frmLoad?.CapNhatThongBao("Đang nạp lại dữ liệu, vui lòng đợi...");
                DataCache.Clear();
                token.ThrowIfCancellationRequested();
                Volatile.Write(ref mucTieuTienTrinh, 75);
                await ReloadDuLieu();
                if (IsDisposed || !IsHandleCreated)
                    return;
                token.ThrowIfCancellationRequested();
                Volatile.Write(ref mucTieuTienTrinh, 90);
                CapNhatThongKeToanBoQuanSo();
                KiemTraDuLieu();
                textBox_TimKiemTheoTen.Clear();
                if (comboBox_TimKiemDonVi.Items.Count > 0)
                    comboBox_TimKiemDonVi.SelectedIndex = 0;
                if (comboBox_XepLoaiThiDua.Items.Count > 0)
                    comboBox_XepLoaiThiDua.SelectedIndex = 0;
                Volatile.Write(ref mucTieuTienTrinh, 100);
                while (!IsDisposed && IsHandleCreated && toolStripProgressBar1_LamMoi != null && toolStripProgressBar1_LamMoi.Value < 100)
                {
                    await Task.Delay(16, token);
                }
                await Task.Delay(30, token);
                toolStripStatusLabel1.Text = "Đã làm mới dữ liệu";
            }
            catch (OperationCanceledException)
            {
                toolStripStatusLabel1.Text = "Đã hủy thao tác";
            }
            catch (IOException ex)
            {
                toolStripStatusLabel1.Text = "Lỗi truy cập file";
                MessageBox.Show($"Không thể truy cập file dữ liệu.\n\n{ex.Message}", "Lỗi File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (UnauthorizedAccessException ex)
            {
                toolStripStatusLabel1.Text = "Không đủ quyền truy cập";
                MessageBox.Show($"Không đủ quyền truy cập dữ liệu.\n\n{ex.Message}", "Lỗi quyền hạn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                toolStripStatusLabel1.Text = "Lỗi hệ thống";
                MessageBox.Show($"Lỗi khi làm mới dữ liệu:\n\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try
                {
                    progressCts?.Cancel();
                    if (progressTask != null)
                        await progressTask;
                }
                catch { }
                finally
                {
                    progressCts?.Dispose();
                }
                try
                {
                    if (frmLoad != null && !frmLoad.IsDisposed)
                    {
                        frmLoad.Close();
                        frmLoad.Dispose();
                    }
                }
                catch { }
                try
                {
                    if (!IsDisposed && IsHandleCreated)
                    {
                        if (toolStripProgressBar1_LamMoi != null)
                        {
                            toolStripProgressBar1_LamMoi.Value = 0;
                            toolStripProgressBar1_LamMoi.Visible = false;
                        }
                        kryptonButton_RefershCSDL.Values.Text = textGoc;
                        kryptonButton_RefershCSDL.Values.Image = imageGoc;
                        kryptonButton_RefershCSDL.Enabled = true;
                    }
                }
                catch { }
                Interlocked.Exchange(ref _dangRefresh, 0);
            }
        }
        // Thống kê trực tiếp trên DataRow theo danh sách chỉ số (idx = null: mọi dòng). Cùng cách phân loại với ThucThiThongKe.
        private (int TongSo, Dictionary<string, int> ChiTiet) ThucThiThongKeHang(DataTable? dt, int[]? idx)
        {
            var ketQua = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var loai in DANH_SACH_PHAN_LOAI) ketQua[loai] = 0;
            int tongSo = 0;
            if (dt == null) return (tongSo, ketQua);
            int oH = dt.Columns.IndexOf(COT_HO_TEN);
            int oP = dt.Columns.IndexOf(COT_PHAN_LOAI);
            int n = idx?.Length ?? dt.Rows.Count;
            int tong = dt.Rows.Count;
            for (int k = 0; k < n; k++)
            {
                int i = idx == null ? k : idx[k];
                if ((uint)i >= (uint)tong) continue;
                var r = dt.Rows[i];
                string hoTen = (oH >= 0 ? r[oH] : null)?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(hoTen)) continue;
                tongSo++;
                string phanLoai = (oP >= 0 ? r[oP] : null)?.ToString()?.Trim() ?? "";
                if (phanLoai.Equals(Module_HeThong.Loai_1, StringComparison.OrdinalIgnoreCase)) ketQua[Module_HeThong.Loai_1]++;
                else if (phanLoai.Equals(Module_HeThong.Loai_2, StringComparison.OrdinalIgnoreCase)) ketQua[Module_HeThong.Loai_2]++;
                else if (phanLoai.Equals(Module_HeThong.Loai_3, StringComparison.OrdinalIgnoreCase)) ketQua[Module_HeThong.Loai_3]++;
                else if (phanLoai.Equals(Module_HeThong.Loai_4, StringComparison.OrdinalIgnoreCase)) ketQua[Module_HeThong.Loai_4]++;
                else ketQua[Module_HeThong.PL_KHONG_PL]++;
            }
            return (tongSo, ketQua);
        }
        private (int TongSo, Dictionary<string, int> ChiTiet) ThucThiThongKe(DataView? dv)
        {
            var ketQua = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var loai in DANH_SACH_PHAN_LOAI) ketQua[loai] = 0;
            int tongSo = 0;
            if (dv == null || dv.Count == 0) return (tongSo, ketQua);
            // Tra ordinal 1 lần thay vì tra tên cột cho từng dòng
            int oH = dv.Table?.Columns.IndexOf(COT_HO_TEN) ?? -1;
            int oP = dv.Table?.Columns.IndexOf(COT_PHAN_LOAI) ?? -1;
            foreach (DataRowView row in dv)
            {
                string hoTen = (oH >= 0 ? row[oH] : row[COT_HO_TEN])?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(hoTen)) continue;
                tongSo++;
                string phanLoai = (oP >= 0 ? row[oP] : row[COT_PHAN_LOAI])?.ToString()?.Trim() ?? "";
                if (phanLoai.Equals(Module_HeThong.Loai_1, StringComparison.OrdinalIgnoreCase)) ketQua[Module_HeThong.Loai_1]++;
                else if (phanLoai.Equals(Module_HeThong.Loai_2, StringComparison.OrdinalIgnoreCase)) ketQua[Module_HeThong.Loai_2]++;
                else if (phanLoai.Equals(Module_HeThong.Loai_3, StringComparison.OrdinalIgnoreCase)) ketQua[Module_HeThong.Loai_3]++;
                else if (phanLoai.Equals(Module_HeThong.Loai_4, StringComparison.OrdinalIgnoreCase)) ketQua[Module_HeThong.Loai_4]++;
                else ketQua[Module_HeThong.PL_KHONG_PL]++;
            }
            return (tongSo, ketQua);
        }
        private async void kryptonButton_XoaKetQuaPhanLoai_Click(object? sender, EventArgs e)
        {
            if (!KiemTraDuLieuSanSang("xóa kết quả phân loại")) return;
            DialogResult kq;
            int soDong;
            using (var frm16 = new Form16_LamMoi())
            {
                kq = frm16.ShowDialog(this);
                soDong = frm16.SoDongDaCapNhat;
            }
            // Form16 chỉ trả OK khi CSDL thật sự bị đổi. Người dùng đóng form / không có ai cần đặt lại
            // -> KHÔNG làm mới, không bật thanh tiến trình toolStripProgressBar1_LamMoi, không vẽ lại biểu đồ.
            if (kq != DialogResult.OK || soDong <= 0 || IsDisposed) return;
            kryptonButton_RefershCSDL.PerformClick();   // đã gồm CapNhatThongKeToanBoQuanSo() bên trong
            while (!IsDisposed && Volatile.Read(ref _dangRefresh) == 1) await Task.Delay(30);   // chờ làm mới xong rồi mới vẽ biểu đồ
            await Module_BieuDoTronTrangChu.CapNhatBieuDoForm4Async();
        }
    } // Ngoai luong
}
public static class DataGridViewExtensions
{
    public static void EnableDoubleBuffered(this DataGridView dgv, bool setting = true)
    {
        Type dgvType = dgv.GetType();
        PropertyInfo? pi = dgvType.GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
        if (pi != null)
        {
            pi.SetValue(dgv, setting, null);
        }
    }
}
public class ComboboxItem
{
    public string Text { get; set; }   // Chữ hiển thị lên UI (VD: Module_HeThong.PL_CSTD hoặc "Loại 1")
    public string Value { get; set; }  // Giá trị thực tế bên dưới (VD: "Loại 1")
    public ComboboxItem(string text, string value)
    {
        Text = text;
        Value = value;
    }
    public override string ToString()
    {
        return Text; // WinForms & KryptonComboBox sẽ dùng chuỗi này để vẽ lên giao diện
    }
}