using DocumentFormat.OpenXml;
using ExcelDataReader;
using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Media;
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
        private bool _dangXuLyHuongDan = false;
        private int _dangRefresh = 0;
        private CancellationTokenSource? _refreshCts;
        private static readonly Dictionary<string, string> _tenCotTiengViet = new(StringComparer.Ordinal) {
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
        public static Form6_XuLyData Instance { get; private set; }
        private System.Windows.Forms.Timer timerLocDuLieu;
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
        private System.Windows.Forms.ToolTip _toolTipMain;
        private int _yeuCauNhapExcel;
        // Hàm SendMessage bạn đã khai báo sẵn ở trên rồi, ta sẽ tận dụng lại
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);
        // 1. KHAI BÁO HẰNG SỐ CHUẨN KỸ SƯ (Quản lý tập trung)
        public Form6_XuLyData()
        {
            InitializeComponent();
            this.KeyPreview = true; // THÊM DÒNG NÀY
            // 2. KHỞI TẠO BIẾN / TIMER
            timerLocDuLieu = new System.Windows.Forms.Timer
            {
                Interval = 300 // Delay 0.3s sau khi ngừng gõ
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
        private void TimerLocDuLieu_Tick(object sender, EventArgs e)
        {
            timerLocDuLieu.Stop();
            ApplyFilter(); // Chạy bộ lọc
        }
        private void Form6_Load(object sender, EventArgs e)
                {
                    if (_isInit) return;
                    _isInit = true;
                    // 1. LIÊN KẾT CONTROL CƠ BẢN
                    kryptonDataGridView1.ContextMenuStrip = contextMenuStrip1;
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
                    // 8. BẮT ĐẦU NẠP DỮ LIỆU CHÍNH
                    _ = ReloadDuLieu();
                    // 9. XỬ LÝ UI SAU KHI FORM ĐÃ HIỂN THỊ
                    BeginInvoke(new Action(() =>
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
                        if (kryptonDataGridView1.Rows.Count > 0)
                        {
                            try
                            {
                                kryptonDataGridView1.CurrentCell =
                                    kryptonDataGridView1.Rows[0].Cells[0];
                            }
                            catch
                            {
                            }
                        }
                    }));
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
                // 1. PHÍM TẮT CHUNG CỦA HỆ THỐNG
                if (Module_PhimTat.XuLy(
                    keyData,
                    _actionLamMoi,
                    _actionLuuTinhToan,
                    _actionXuatExcel))
                {
                    return true;
                }
                // 2. PHÂN TÁCH PHÍM VÀ PHÍM BỔ TRỢ
                Keys key = keyData & Keys.KeyCode;
                Keys modifier = keyData & Keys.Modifiers;
                // 3. F7 → F12
                if (modifier == Keys.None)
                {
                    return key switch
                    {
                        Keys.F7 => SafeExecute(
                            kiemTraKetNoi,
                            () => kiemTraKetNoi.PerformClick()),
                        Keys.F8 => SafeExecute(
                            kryptonButton_XoaKetQuaPhanLoai,
                            () => kryptonButton_XoaKetQuaPhanLoai.PerformClick()),
                        Keys.F9 => SafeExecute(
                            kryptonButton1_ThiDuaBaNhat, () => kryptonButton1_ThiDuaBaNhat.PerformClick()),
                        Keys.F10 => SafeExecute(
                            phanTichDuLieuTrungTen_ToolStripMenuItem,
                            () => phanTichDuLieuTrungTen_ToolStripMenuItem.PerformClick()),
                        Keys.F11 => SafeExecute(
                            cBCSTrongDienQuanLy_ToolStripMenuItem,
                            () => cBCSTrongDienQuanLy_ToolStripMenuItem.PerformClick()),
                        Keys.F12 => SafeExecute(
                            nhapKetQuaPhanLoaiTuDonVi_ToolStripMenuItem,
                            () => nhapKetQuaPhanLoaiTuDonVi_ToolStripMenuItem.PerformClick()),
                        _ => false
                    };
                }
                // 4. CTRL + O / CTRL + T
                if (modifier == Keys.Control)
                {
                    return key switch
                    {
                        Keys.O => SafeExecute(
                            themDuLieuTuFileExce_ToolStripMenuItem,
                            () => themDuLieuTuFileExce_ToolStripMenuItem.PerformClick()),
                        Keys.T => SafeExecute(
                            xuatDuLieuSangThongKe_ToolStripMenuItem,
                            () => xuatDuLieuSangThongKe_ToolStripMenuItem.PerformClick()),
                        _ => false
                    };
                }
                // 5. CTRL + SHIFT + DELETE
                if (modifier == (Keys.Control | Keys.Shift) &&
                    key == Keys.Delete)
                {
                    return SafeExecute(
                        xoaToanBoDuLieuDSCBCS_ToolStripMenuItem,
                        () => xoaToanBoDuLieuDSCBCS_ToolStripMenuItem.PerformClick());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Lỗi phím tắt toàn cục: {ex}");
            }
            // 6. KHÔNG PHẢI PHÍM TẮT CỦA ỨNG DỤNG
            return base.ProcessCmdKey(ref msg, keyData);
        }
        private bool _isCheDoNam = false;
        public async Task ReloadDuLieu()
        {
            if (IsDisposed || !IsHandleCreated) return;
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
                // 2. LẤY DỮ LIỆU & ĐỌC CẤU HÌNH (Đã xóa bỏ khối trùng lặp)
                if (DataCache.IsLoaded)
                {
                    // Bắt buộc phải có .Copy() để không làm bẩn DataCache
                    dtKetQua = DataCache.GetDanhSach().Copy();
                    isCheDoNam = await Task.Run(() => KiemTraCheDoXetThiDuaNam());
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
                // Tiền xử lý dữ liệu ngay trên RAM
                dtDanhSachGoc = dtKetQua;
                // BẢO KÊ DÒNG ẢO
                ThemDongTrongAnToan(dtDanhSachGoc);
                // Đếm người thật
                int soLuongThucTe = dtDanhSachGoc.AsEnumerable().Count(r => !string.IsNullOrWhiteSpace(r.Field<string>("HoVaTen")));
                // 3. ĐỔ DỮ LIỆU LÊN GIAO DIỆN
                UIHelper.SafeInvoke(this, () =>
                {
                    SendMessage(kryptonDataGridView1.Handle, WM_SETREDRAW, 0, null);
                    if (kryptonDataGridView1.Columns.Contains("GhiChu"))
                        kryptonDataGridView1.Columns["GhiChu"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    kryptonDataGridView1.DataSource = null;
                    TaoCauTrucCotGrid();
                    kryptonDataGridView1.RowCount = dtDanhSachGoc.DefaultView.Count;
                    DoiTenCotTiengViet();
                    CanChinhBang();
                    // LƯU Ý: Các hàm như LoadComboBoxPhanLoai() hoặc TichHopGiaoDien() 
                    // nên cân nhắc chuyển sang Form_Load để đỡ phải chạy lại mỗi khi Refresh data
                    LoadComboBoxPhanLoai();
                    Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1);
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
                    ApplyFilter();
                    KiemTraDuLieu();
                });
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
                        SendMessage(kryptonDataGridView1.Handle, WM_SETREDRAW, 1, null);
                        kryptonDataGridView1.Refresh();
                        kryptonButton_RefershCSDL.Enabled = true;
                    }
                });
            }
        }
        private DataTable XuLyDuLieuNgam(string csdlPath)
        {
            if (string.IsNullOrWhiteSpace(csdlPath) || !File.Exists(csdlPath))
                throw new Exception("Không tìm thấy CSDL2!");
            using var conn = new SqliteConnection($"Data Source={csdlPath}");
            conn.Open();
            using var cmd = new SqliteCommand("SELECT * FROM DanhSach", conn);
            using var reader = cmd.ExecuteReader();
            DataTable dtRaw = new DataTable();
            dtRaw.Load(reader);
            // --- ĐOẠN DEBUG QUAN TRỌNG ---
            // Giúp bạn thấy chính xác cột nào đang tồn tại trong DataTable
            string danhSachCot = string.Join(", ", dtRaw.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            System.Diagnostics.Debug.WriteLine($"[DEBUG] CSDL có các cột: {danhSachCot}");
            // -----------------------------
            if (dtRaw.Rows.Count == 0) return dtRaw;
            dtRaw.BeginLoadData();
            // CHỈ GIẢI MÃ NHỮNG CỘT CẦN THIẾT
            string[] cotCanGiaiMaNgay = { "HoVaTen", "DonVi", "PhanLoai" };
            foreach (DataRow row in dtRaw.Rows)
            {
                foreach (var c in cotCanGiaiMaNgay)
                {
                    // BỔ SUNG: Kiểm tra cột có tồn tại không trước khi truy cập
                    if (dtRaw.Columns.Contains(c))
                    {
                        string val = row[c]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(val))
                        {
                            row[c] = GiaiMaSafeCoCache(val);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[CẢNH BÁO] Không tìm thấy cột: {c}. Đã bỏ qua giải mã.");
                    }
                }
            }
            dtRaw.EndLoadData();
            // Sắp xếp an toàn
            string[] thuTuUuTien = Module_DonVi.LayDanhSachDonViUuTienArray().Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            var uuTienMap = thuTuUuTien.Select((dv, index) => new { dv, index }).ToDictionary(x => x.dv, x => x.index);
            return dtRaw.AsEnumerable()
                .OrderBy(r =>
                {
                    // Kiểm tra cột DonVi trước khi dùng Field
                    string donVi = dtRaw.Columns.Contains("DonVi") ? (r.Field<string>("DonVi") ?? "") : "";
                    return uuTienMap.TryGetValue(donVi, out int idx) ? idx : int.MaxValue;
                })
                .ThenBy(r =>
                {
                    var sttVal = r["STT"];
                    return (sttVal == DBNull.Value || string.IsNullOrWhiteSpace(sttVal.ToString())) ? 0 : Convert.ToInt64(sttVal);
                })
                .CopyToDataTable();
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
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // 1. Hủy đăng ký các sự kiện hệ thống (Tránh memory leak / lãng phí tài nguyên)
            Module_HeThong.SuKienThayDoiCheDoXetThiDua -= OnCheDoThayDoi;
            Module_HeThong.SuKienThayDoiThoiGianHeThong -= OnCheDoThayDoi;
            // 2. Dọn dẹp và giải phóng Timer
            if (timerLocDuLieu != null)
            {
                timerLocDuLieu.Stop();
                timerLocDuLieu.Dispose();
                timerLocDuLieu = null; // Đảm bảo giải phóng hoàn toàn
            }
            // 3. Giải phóng biến Singleton Instance
            Instance = null;
            // 4. Gọi hàm OnFormClosed của lớp cơ sở (Bắt buộc)
            base.OnFormClosed(e);
        }
        // Hủy đăng ký sự kiện khi đóng Form để tránh leak bộ nhớ
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _aesCache = new System.Collections.Concurrent.ConcurrentDictionary<string, string>(StringComparer.Ordinal);
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
        private bool SafeExecute(
       Component component,
       Action action)
        {
            // VALIDATE COMPONENT
            if (component == null)
                return false;
            // TOOLSTRIP ITEM
            if (component is ToolStripItem menuItem)
            {
                if (!menuItem.Available)
                    return false;
                if (!menuItem.Enabled)
                    return false;
            }
            // CONTROL
            if (component is Control control)
            {
                if (control.IsDisposed)
                    return false;
                if (!control.Visible)
                    return false;
                if (!control.Enabled)
                    return false;
            }
            // EXECUTE
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
                Debug.WriteLine(
                    "Lỗi trạng thái UI: " + ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "Lỗi thực thi phím tắt: " + ex);
                return false;
            }
        }
        private bool KiemTraCheDoXetThiDuaNam()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path))
                    return false;
                using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT ChoPhepCheDoXetThiDuaNam FROM CheDo_XetThiDuaNam WHERE ID = 1 LIMIT 1";
                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    string cheDo = result.ToString().Trim();
                    return string.Equals(cheDo, "Năm", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi đọc CheDo_XetThiDuaNam: " + ex.Message);
            }
            return false;
        }
        private void KryptonDataGridView1_CellPainting(
           object? sender,
           DataGridViewCellPaintingEventArgs e)
        {
            // VALIDATE CƠ BẢN
            if (sender is not DataGridView dgv)
                return;
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            if (!string.Equals(
                    dgv.Columns[e.ColumnIndex].Name,
                    "HoVaTen",
                    StringComparison.Ordinal))
            {
                return;
            }
            // VALIDATE DATASOURCE / DEFAULTVIEW
            DataView view = dtDanhSachGoc?.DefaultView;
            if (view == null)
                return;
            // CHỐNG OUT-OF-RANGE
            // (Rất quan trọng khi Filter/Search realtime)
            int rowIndex = e.RowIndex;
            if ((uint)rowIndex >= (uint)view.Count)
                return;
            // LẤY PHÂN LOẠI AN TOÀN
            string phanLoai;
            try
            {
                object value = view[rowIndex]["PhanLoai"];
                phanLoai = value?.ToString()?.Trim() ?? string.Empty;
            }
            catch
            {
                // DataView có thể vừa refresh/filter
                // tránh crash UI thread
                return;
            }
            // CHỈ CUSTOM DRAW CHO "LOẠI 1"
            if (!string.Equals(
                    phanLoai,
                    Module_HeThong.Loai_1,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            // VALIDATE ICON
            if (_iconStar == null)
                return;
            // VẼ NỀN (KHÔNG VẼ BORDER)
            e.Paint(
                e.CellBounds,
                DataGridViewPaintParts.Background |
                DataGridViewPaintParts.SelectionBackground |
                DataGridViewPaintParts.Focus);
            // THÔNG SỐ RENDER
            const int iconSize = 16;
            const int paddingLeft = 6;
            const int paddingText = 6;
            int iconX = e.CellBounds.X + paddingLeft;
            int iconY =
                e.CellBounds.Y +
                ((e.CellBounds.Height - iconSize) / 2);
            // VẼ ICON
            try
            {
                e.Graphics.DrawImage(
                    _iconStar,
                    iconX,
                    iconY,
                    iconSize,
                    iconSize);
            }
            catch
            {
                // Tránh crash nếu image bị dispose
                return;
            }
            // LẤY TEXT
            string hoTen =
                Convert.ToString(e.Value) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(hoTen))
            {
                int textX =
                    iconX +
                    iconSize +
                    paddingText;
                Rectangle textBounds = new Rectangle(
                    textX,
                    e.CellBounds.Y,
                    e.CellBounds.Right - textX,
                    e.CellBounds.Height);
                Color textColor =
                    (e.State & DataGridViewElementStates.Selected)
                    == DataGridViewElementStates.Selected
                    ? e.CellStyle.SelectionForeColor
                    : e.CellStyle.ForeColor;
                TextRenderer.DrawText(
                    e.Graphics,
                    hoTen,
                    e.CellStyle.Font,
                    textBounds,
                    textColor,
                    TextFormatFlags.Left |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis |
                    TextFormatFlags.NoPadding |
                    TextFormatFlags.PreserveGraphicsClipping);
            }
            // BÁO ĐÃ HANDLE XONG
            e.Handled = true;
        }
        private void HoanTatLoadGiaoDien()
        {
            try
            {
                LoadComboBoxDonVi();
                NapDanhSachDonVi();
                LoadComboBoxChucVu();
                //ApplyColumnColoring();
                // ⭐ GỌI HÀM TIỆN ÍCH COMBOBOX XẾP LOẠI VÀO NHỊP VẼ NÀY
                LoadComboBoxXepLoaiThiDua();
                // ⭐ SỬA LỖI: Lấy DataView từ dtDanhSachGoc thay vì Grid.DataSource
                if (dtDanhSachGoc != null && dtDanhSachGoc.DefaultView.Count > 0)
                {
                    CapNhatThongKePhanLoai(dtDanhSachGoc.DefaultView);
                    EnsureLastRowVisible(kryptonDataGridView1);
                }
                else
                {
                    CapNhatThongKePhanLoai(null);
                }
            }
            finally
            {
                // Cho phép Grid vẽ lại sau khi đã tính toán và gán xong xuôi
                kryptonDataGridView1.ResumeLayout();
            }
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
            cBCSTrongDienQuanLy_ToolStripMenuItem.Visible =
                visible;
            phanTichDuLieuTrungTen_ToolStripMenuItem.Visible =
                visible;
            xoaToanBoDuLieuDSCBCS_ToolStripMenuItem.Visible =
                visible;
            nhapKetQuaPhanLoaiTuDonVi_ToolStripMenuItem.Visible =
                visible;
            xuatDuLieuSangThongKe_ToolStripMenuItem.Visible =
                visible;
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
                    kTextbox.Enter += (s, e) => { kTextbox.StateCommon.Border.Color1 = mauFocus; kTextbox.StateCommon.Border.Width = 2; };
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
                    kCombobox.Enter += (s, e) => { kCombobox.StateCommon.ComboBox.Border.Color1 = mauFocus; kCombobox.StateCommon.ComboBox.Border.Width = 2; };
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
        private void Form6_XuLyData_VisibleChanged(
    object? sender,
    EventArgs e)
        {
            // CHỈ XỬ LÝ KHI FORM HIỆN
            if (!Visible)
                return;
            // FORM CHƯA INIT XONG
            if (!_isInit)
                return;
            // FORM ĐÃ DISPOSE
            if (IsDisposed || Disposing)
                return;
            // KHÔNG CÓ GÌ THAY ĐỔI
            if (!_isDirtyFilter)
                return;
            // RESET FLAG TRƯỚC
            // TRÁNH LOOP NẾU ApplyFilter GÂY EVENT
            _isDirtyFilter = false;
            // APPLY FILTER AN TOÀN
            try
            {
                ApplyFilter();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine(
                    "Lỗi ApplyFilter: " + ex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "VisibleChanged Error: " + ex);
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
                // Cách 1: Dùng thuộc tính mờ (Watermark) chuẩn xịn do chính Krypton hỗ trợ (Khuyên dùng)
                // Hầu hết các bản Krypton Toolkit hiện tại đều có thuộc tính này.
                try
                {
                    kTxtBox.CueHint.CueHintText = placeholder;
                }
                catch
                {
                    // Cách 2: Nếu bản Krypton của ông quá cũ không có CueHint, 
                    // ta sẽ "chọc" thẳng vào cái TextBox con bị giấu bên trong nó để gửi API
                    if (kTxtBox.TextBox != null && kTxtBox.TextBox.IsHandleCreated)
                    {
                        SendMessage(kTxtBox.TextBox.Handle, EM_SETCUEBANNER, 0, placeholder);
                    }
                }
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
            (kryptonButton1_HuongDanThemDuLieu,  "Hướng dẫn tạo tệp excel để nhập dữ liệu vào phần mềm"),
            (kryptonButton1_ThiDuaBaNhat,        "Quản lý phong trào thi đua Ba Nhất"),
            (kryptonButton_LamMoiCacOTimKiem,    "Làm mới bộ lọc"),
            (kryptonButton_LuuDataCapNhat,       "Lưu toàn bộ dữ liệu đã chỉnh sửa (Ctrl + S)")
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
        private void NapDanhSachDonVi()
        {
            var dsDonVi = Module_DonVi.GetDanhSachDonVi(); // List<string>
            var thuTuUuTien = Module_DonVi.LayDanhSachDonViUuTienArray();
            // 1. Sắp xếp danh sách đơn vị theo thứ tự ưu tiên
            var sortedList = dsDonVi.OrderBy(item =>
            {
                int index = Array.IndexOf(thuTuUuTien, item);
                return index == -1 ? int.MaxValue : index;
            }).ToList();
            // 2. Chèn chuỗi rỗng "" vào đầu danh sách (Vị trí Index 0)
            sortedList.Insert(0, "");
            // 3. Kiểm tra điều kiện: Nếu STT hoặc Họ và tên rỗng -> Ép chọn giá trị rỗng ""
            bool sttRong = string.IsNullOrWhiteSpace(textBox_STT.Text);
            bool hoTenRong = string.IsNullOrWhiteSpace(textBox_HoVaTen.Text);
            var currentValue = (sttRong || hoTenRong) ? "" : textBox_DonVi.Text;
            // 4. Gán DataSource
            textBox_DonVi.DataSource = null;
            textBox_DonVi.DataSource = sortedList;
            // 5. Gán lại giá trị được chọn
            textBox_DonVi.Text = sortedList.Contains(currentValue) ? currentValue : "";
        }
        private void CapNhatLabelTheoPhienBan()
        {
            try
            {
                bool laTanBinh = Module_TaiKhoan
                    .LayPhienBanPhanMem()
                    .Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                string doiTuong = laTanBinh ? "Tân binh" : "CBCS";
                // Cập nhật tiêu đề
                groupBox1_TimKiemThongTinCBCS.Text =
                    $"1. Tìm kiếm thông tin {doiTuong}";
                groupBox3_ThongTinCBCS.Text =
                    $"3. Cập nhật thông tin {doiTuong}";
                // STT và Số hiệu: chỉ đọc
                textBox_STT.ReadOnly = true;
                textBox_SoHieu.ReadOnly = true;
                // Màu xanh lá cây nhạt kiểu Web
                Color mauNen = Color.FromArgb(198, 239, 206); // //C6EFCE
                // Áp dụng màu nền
                if (textBox_STT is Krypton.Toolkit.KryptonTextBox kStt)
                {
                    kStt.StateCommon.Back.Color1 = mauNen;
                }
                else
                {
                    textBox_STT.BackColor = mauNen;
                }
                if (textBox_SoHieu is Krypton.Toolkit.KryptonTextBox kSoHieu)
                {
                    kSoHieu.StateCommon.Back.Color1 = mauNen;
                }
                else
                {
                    textBox_SoHieu.BackColor = mauNen;
                }
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
        private void O_CanhBao_MouseEnter(object? sender, EventArgs e)
        {
            if (sender is System.Windows.Forms.Control ctrl)
            {
                // Đổi nền màu xanh lá
                ctrl.BackColor = System.Drawing.Color.LightGreen;
                // Bổ sung riệng cho KryptonTextBox (vì Krypton đôi khi ưu tiên Back.Color1 hơn BackColor thường)
                if (ctrl is Krypton.Toolkit.KryptonTextBox kTxt)
                {
                    kTxt.StateCommon.Back.Color1 = System.Drawing.Color.LightGreen;
                }
            }
        }
        private void O_CanhBao_MouseLeave(object? sender, EventArgs e)
        {
            if (sender is System.Windows.Forms.Control ctrl)
            {
                // Khi chuột đi ra, trả về màu xám mặc định của ô ReadOnly
                ctrl.BackColor = System.Drawing.Color.LightGray;
                if (ctrl is Krypton.Toolkit.KryptonTextBox kTxt)
                {
                    kTxt.StateCommon.Back.Color1 = System.Drawing.Color.LightGray;
                }
            }
        }
        private void KryptonDataGridView1_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            var dv = kryptonDataGridView1.DataSource as DataView;
            CapNhatThongKePhanLoai(dv);
        }
        private void LoadComboBoxDonVi()
        {
            if (dtDanhSachGoc == null || dtDanhSachGoc.Rows.Count == 0) return;
            // 🔥 CHỐNG CRASH: Tránh gọi LINQ r.Field<string>("DonVi") nếu cột không tồn tại
            if (!dtDanhSachGoc.Columns.Contains("DonVi")) return;
            _isUpdatingCombo = true;
            comboBox_TimKiemDonVi.BeginUpdate();
            // ⭐ BƯỚC MỚI: Lưu lại giá trị đang hiển thị trước khi Clear
            string giaTriCu = comboBox_TimKiemDonVi.Text;
            comboBox_TimKiemDonVi.Items.Clear();
            // (Giữ nguyên logic của bạn)
            string[] thuTuUuTien = Module_DonVi.LayDanhSachDonViUuTienArray()
                                               .Where(s => !string.IsNullOrWhiteSpace(s))
                                               .ToArray();
            var dsDonViTrongCSDL = dtDanhSachGoc.AsEnumerable()
                .Select(r => r.Field<string>("DonVi"))
                .Where(val => !string.IsNullOrWhiteSpace(val))
                .Distinct()
                .ToList();
            var dsFinal = thuTuUuTien.Where(x => dsDonViTrongCSDL.Contains(x)).ToList();
            var dsConLai = dsDonViTrongCSDL.Except(dsFinal).OrderBy(x => x);
            dsFinal.AddRange(dsConLai);
            comboBox_TimKiemDonVi.Items.Add(Module_HeThong.Tat_Ca);
            foreach (var dv in dsFinal)
            {
                comboBox_TimKiemDonVi.Items.Add(dv);
            }
            // ⭐ BƯỚC MỚI: Khôi phục lại giá trị cũ nếu nó vẫn còn hợp lệ
            if (!string.IsNullOrEmpty(giaTriCu) && comboBox_TimKiemDonVi.Items.Contains(giaTriCu))
            {
                comboBox_TimKiemDonVi.SelectedItem = giaTriCu;
                comboBox_TimKiemDonVi.Text = giaTriCu; // Fix lỗi hiển thị Krypton
            }
            else
            {
                comboBox_TimKiemDonVi.SelectedIndex = 0;
            }
            comboBox_TimKiemDonVi.EndUpdate();
            _isUpdatingCombo = false;
        }
        private void TaoCauTrucCotGrid()
        {
            // 🚀 ENGINEERING GRID COLUMN INITIALIZATION
            if (kryptonDataGridView1 == null || kryptonDataGridView1.IsDisposed)
                return;
            kryptonDataGridView1.SuspendLayout();
            try
            {
                kryptonDataGridView1.Columns.Clear();
                string[] cols =
                {
            "ID",
            "STT",
            "HoVaTen",
            "SoHieu",
            "NamSinh",
            "QueQuan",
            "NgayVaoCAND",
            "CapBac",
            "ChucVu",
            "DonVi",
            "PhanLoai",
            "GhiChu"
        };
                // 🚀 HASHSET O(1)
                var leftAlignCols = new HashSet<string>(StringComparer.Ordinal)
        {
            "HoVaTen",
            "QueQuan",
            "GhiChu"
        };
                // 🚀 CACHE STYLE - TRÁNH TẠO HÀNG LOẠT OBJECT STYLE
                var centerStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                };
                var leftStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleLeft
                };
                // 🚀 PRE-ALLOCATE COLUMN CAPACITY
                kryptonDataGridView1.ColumnCount = 0;
                foreach (string c in cols)
                {
                    string header =
                        _tenCotTiengViet != null &&
                        _tenCotTiengViet.TryGetValue(c, out string val)
                            ? val
                            : c;
                    var col = new DataGridViewTextBoxColumn
                    {
                        Name = c,
                        HeaderText = header,
                        SortMode = DataGridViewColumnSortMode.NotSortable,
                        // ⭐ DÙNG STYLE CACHE
                        DefaultCellStyle = leftAlignCols.Contains(c)
                            ? leftStyle
                            : centerStyle
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
        private void KryptonDataGridView1_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            if (dtDanhSachGoc == null || e.RowIndex < 0 || e.RowIndex >= dtDanhSachGoc.DefaultView.Count) return;
            try
            {
                DataRowView rowView = dtDanhSachGoc.DefaultView[e.RowIndex];
                string colName = kryptonDataGridView1.Columns[e.ColumnIndex].Name;
                // 🔥 CHỐNG CRASH: Kiểm tra xem DB thực sự có nạp cột này lên không
                if (!rowView.DataView.Table.Columns.Contains(colName))
                {
                    e.Value = "[Thiếu cột trong DB]"; // Báo hiệu trực quan lên giao diện
                    return;
                }
                string rawValue = rowView[colName]?.ToString() ?? "";
                // Các cột này ĐÃ ĐƯỢC GIẢI MÃ LÚC LOAD
                if (colName == "HoVaTen" || colName == "DonVi" || colName == "PhanLoai" || colName == "STT" || colName == "ID")
                {
                    // ⭐ ÁNH XẠ HIỂN THỊ CỘT PHÂN LOẠI KHI Ở CHẾ ĐỘ "NĂM"
                    if (colName == "PhanLoai" && _isCheDoNam)
                    {
                        e.Value = rawValue switch
                        {
                            "Loại 1" => Module_HeThong.PL_CSTD,
                            "Loại 2" => Module_HeThong.PL_CSTT,
                            "Loại 3" => Module_HeThong.PL_HTNV,
                            "Loại 4" => Module_HeThong.PL_KHTNV,
                            "Không PL" => "Không PL",
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
                    // Các cột khác ĐANG MÃ HÓA -> Giải mã ngay lúc hiện
                    e.Value = GiaiMaSafeCoCache(rawValue);
                }
            }
            catch { e.Value = "..."; }
        }
        private void DinhDangDataGridFrom6()
        {
            kryptonDataGridView1.ContextMenuStrip = contextMenuStrip1;
            kryptonDataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            kryptonDataGridView1.MultiSelect = false;
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
                timerLocDuLieu.Stop();
                timerLocDuLieu.Start();
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
                dgv.StateCommon.HeaderColumn.Back.Color1 = Color.FromArgb(180, 210, 240); // Xanh biển đậm hơn một tông
                dgv.StateCommon.HeaderColumn.Back.Color2 = Color.FromArgb(180, 210, 240);
                // Chữ màu đen than tuyền để tương phản mạnh với nền xanh
                dgv.StateCommon.HeaderColumn.Content.Color1 = Color.FromArgb(30, 30, 30);
                // Viền header cùng tông xanh nhưng đậm hơn một chút để tạo khối viền
                dgv.StateCommon.HeaderColumn.Border.Color1 = Color.FromArgb(150, 180, 210);
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
                    if (_tenCotTiengViet.TryGetValue(col.Name, out string header))
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
        private void CanChinhBang()
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
                dgv.Padding = new Padding(0);
                dgv.CurrentCell = null;
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
        private void ApplyFilter()
        {
            if (dtDanhSachGoc == null || _isUpdatingCombo)
                return;
            try
            {
                if (kryptonDataGridView1.IsDisposed)
                    return;
                SendMessage(kryptonDataGridView1.Handle, WM_SETREDRAW, 0, null);
                // KIỂM TRA CỘT BẮT BUỘC
                string[] requiredColumns = { "HoVaTen", "DonVi", "PhanLoai" };
                foreach (string col in requiredColumns)
                {
                    if (!dtDanhSachGoc.Columns.Contains(col))
                    {
                        throw new Exception(
                            $"Thiếu cột '{col}' trong dtDanhSachGoc.\n\n" +
                            $"Các cột hiện có:\n" +
                            string.Join(", ", dtDanhSachGoc.Columns.Cast<DataColumn>().Select(c => c.ColumnName)));
                    }
                }
                string tenFilter = "";
                if (!_dangSetPlaceholder && textBox_TimKiemTheoTen.Text != PLACEHOLDER_TIMKIEM)
                {
                    tenFilter = (textBox_TimKiemTheoTen.Text ?? "").Trim().Replace("'", "''");
                }
                string donViFilter = comboBox_TimKiemDonVi?.Text?.Trim() ?? "";
                string phanLoaiFilter = comboBox_XepLoaiThiDua?.Text?.Trim() ?? "";
                List<string> filters = new();
                // HỌ VÀ TÊN
                if (!string.IsNullOrWhiteSpace(tenFilter))
                {
                    filters.Add($"Convert(HoVaTen,'System.String') LIKE '%{tenFilter}%'");
                }
                // ĐƠN VỊ
                if (!string.IsNullOrWhiteSpace(donViFilter) &&
                    !donViFilter.Equals(Module_HeThong.Tat_Ca, StringComparison.OrdinalIgnoreCase))
                {
                    filters.Add($"Convert(DonVi,'System.String') = '{donViFilter.Replace("'", "''")}'");
                }
                // PHÂN LOẠI (XỬ LÝ ÁNH XẠ NGHƯỢC VỀ DỮ LIỆU THỰC TẾ)
                if (!string.IsNullOrWhiteSpace(phanLoaiFilter) &&
                    !phanLoaiFilter.Equals(Module_HeThong.Tat_Ca, StringComparison.OrdinalIgnoreCase))
                {
                    bool laXetNam = Module_HeThong.IsCheDoXetThiDuaNam();
                    // Nếu đang ở chế độ XÉT NĂM, quy đổi văn bản giao diện (CSTĐ, CSTT,...) 
                    // về đúng chuỗi lưu giữ trong DataTable (Loại 1, Loại 2,...)
                    string giaTriThucTeInDb = phanLoaiFilter;
                    if (laXetNam)
                    {
                        if (phanLoaiFilter.Equals(Module_HeThong.PL_CSTD, StringComparison.OrdinalIgnoreCase))
                            giaTriThucTeInDb = Module_HeThong.Loai_1; // "Loại 1"
                        else if (phanLoaiFilter.Equals(Module_HeThong.PL_CSTT, StringComparison.OrdinalIgnoreCase))
                            giaTriThucTeInDb = Module_HeThong.Loai_2; // "Loại 2"
                        else if (phanLoaiFilter.Equals(Module_HeThong.PL_HTNV, StringComparison.OrdinalIgnoreCase))
                            giaTriThucTeInDb = Module_HeThong.Loai_3; // "Loại 3"
                        else if (phanLoaiFilter.Equals(Module_HeThong.PL_KHTNV, StringComparison.OrdinalIgnoreCase))
                            giaTriThucTeInDb = Module_HeThong.Loai_4; // "Loại 4"
                    }
                    // Tiến hành xây dựng điều kiện lọc theo giaTriThucTeInDb
                    if (giaTriThucTeInDb.Equals("Không PL", StringComparison.OrdinalIgnoreCase) ||
                        giaTriThucTeInDb.Equals(Module_HeThong.PL_KHONG_PL, StringComparison.OrdinalIgnoreCase))
                    {
                        filters.Add("(PhanLoai IS NULL OR PhanLoai = '' OR PhanLoai = 'Không PL')");
                    }
                    else
                    {
                        // Lọc chính xác hoặc lọc tương đối theo giá trị thực tế trong DB
                        filters.Add($"(Convert(PhanLoai,'System.String') = '{giaTriThucTeInDb.Replace("'", "''")}' OR Convert(PhanLoai,'System.String') LIKE '%{giaTriThucTeInDb.Replace("'", "''")}%')");
                    }
                }
                string finalFilter = filters.Count > 0 ? string.Join(" AND ", filters) : string.Empty;
                dtDanhSachGoc.DefaultView.RowFilter = finalFilter;
                kryptonDataGridView1.RowCount = dtDanhSachGoc.DefaultView.Count;
                CapNhatThongKeSieuToc(dtDanhSachGoc.DefaultView);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "Lỗi ApplyFilter",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Debug.WriteLine(ex);
            }
            finally
            {
                try
                {
                    if (!kryptonDataGridView1.IsDisposed)
                    {
                        SendMessage(kryptonDataGridView1.Handle, WM_SETREDRAW, 1, null);
                        kryptonDataGridView1.Invalidate();
                        kryptonDataGridView1.Refresh();
                    }
                }
                catch
                {
                }
            }
        }
        private bool KiemTraPhienBanTanBinh()
        {
            if (_isTanBinhCached.HasValue) return _isTanBinhCached.Value;
            try
            {
                _isTanBinhCached = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
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
            string statusCu = toolStripStatusLabel1.Text;
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
                    // -----------------------------------------------------------------
                    // NHÁNH A: DÀNH CHO TÂN BINH (Sử dụng Form 30)
                    // -----------------------------------------------------------------
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
                    // -----------------------------------------------------------------
                    // NHÁNH B: DÀNH CHO CÁN BỘ CHIẾN SĨ (Sử dụng Form 22)
                    // -----------------------------------------------------------------
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
        private void kryptonDataGridView1_RowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv == null) return;
            // KIỂM TRA DÒNG ẢO
            bool laDongAo = false;
            if (dgv.Columns.Contains("HoVaTen"))
            {
                var giaTriHoTen = dgv.Rows[e.RowIndex].Cells["HoVaTen"].Value;
                if (giaTriHoTen == null || string.IsNullOrWhiteSpace(giaTriHoTen.ToString()))
                {
                    laDongAo = true;
                }
            }
            // NẾU LÀ DỮ LIỆU THẬT THÌ MỚI VẼ STT
            if (!laDongAo)
            {
                string stt = (e.RowIndex + 1).ToString();
                var bounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, dgv.RowHeadersWidth, e.RowBounds.Height);
                TextRenderer.DrawText(e.Graphics, stt, dgv.Font, bounds, dgv.RowHeadersDefaultCellStyle.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
        private void kryptonDataGridView1_Resize(object sender, EventArgs e)
        {
            CanChinhBang();
        }
        private void kryptonDataGridView1_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            if (sender is not DataGridView dgv) return;
            var hit = dgv.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0 || hit.ColumnIndex < 0) return;
            var row = dgv.Rows[hit.RowIndex];
            if (!row.Visible) return; // quan trọng: chỉ set CurrentCell khi row hiển thị
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
        private bool _dangXuLyXoa = false;
        private async void kryptonButton_XoaDataCBCS_Click(object? sender, EventArgs e)
        {
            // ===== 1. CHỐNG SPAM VÀ VALIDATE UI (Chạy trên UI Thread) =====
            if (_dangXuLyXoa) return;
            if (string.IsNullOrWhiteSpace(textBox_STT.Text) || !int.TryParse(textBox_STT.Text.Trim(), out int stt))
            {
                MessageBox.Show("Chưa xác định được dữ liệu dòng cần xử lý.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string hoTen = string.IsNullOrWhiteSpace(textBox_HoVaTen.Text) ? "[Không rõ]" : textBox_HoVaTen.Text.Trim();
            string soHieu = string.IsNullOrWhiteSpace(textBox_SoHieu.Text) ? "[Không rõ]" : textBox_SoHieu.Text.Trim();
            string donVi = string.IsNullOrWhiteSpace(textBox_DonVi.Text) ? "[Không rõ]" : textBox_DonVi.Text.Trim();
            string thongBao = $"Bạn có chắc chắn muốn xóa thông tin của {Module_HeThong.Tu_dong_chi}:\n\n" +
                              $"Họ và tên: {hoTen}\n" +
                              $"Số hiệu: {soHieu}\n" +
                              $"Đơn vị: {donVi}\n\n" +
                              $"Lưu ý: Hành động này không thể hoàn tác!";
            if (MessageBox.Show(thongBao, "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            string csdlPath = _csdl2Path;
            if (string.IsNullOrWhiteSpace(csdlPath) || !File.Exists(csdlPath))
            {
                MessageBox.Show("Không thể truy cập dữ liệu hệ thống.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            _dangXuLyXoa = true;
            toolStripStatusLabel1.Text = "Đang xử lý xóa dữ liệu...";
            this.Cursor = Cursors.WaitCursor;
            try
            {
                // ===== 2. THỰC THI DATABASE TRÊN LUỒNG NGẦM (Không làm đơ Form) =====
                bool dbSuccess = await Task.Run(() => ThucThiXoaDatabase(csdlPath, stt));
                if (!dbSuccess)
                {
                    MessageBox.Show("Dữ liệu không tồn tại hoặc đã bị thay đổi bởi người khác.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                // ===== 3. GHI NHẬT KÝ (Fire and Forget - Không block luồng chính) =====
                Task.Run(() =>
                {
                    try
                    {
                        Module_NhatKy.GhiNhatKy(
                            taiKhoan: string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM) ? "Không xác định" : Module_TaiKhoan.TenTaiKhoan_RAM,
                            hanhDong: $"Xóa dữ liệu: {hoTen} (Số hiệu: {soHieu})",
                            ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                        );
                    }
                    catch (Exception ex) { Debug.WriteLine($"Lỗi ghi log: {ex.Message}"); }
                });
                // ===== 4. ĐỒNG BỘ RAM (Tối ưu cực độ - Không gọi ReloadDuLieu) =====
                if (dtDanhSachGoc != null)
                {
                    // Tạm dừng vẽ Grid
                    SendMessage(kryptonDataGridView1.Handle, WM_SETREDRAW, 0, null);
                    // 4.1 Xóa dòng khỏi bộ nhớ
                    var rowToDelete = dtDanhSachGoc.AsEnumerable().FirstOrDefault(r => r["STT"].ToString() == stt.ToString());
                    if (rowToDelete != null)
                    {
                        dtDanhSachGoc.Rows.Remove(rowToDelete);
                    }
                    // 4.2 Đánh lại STT trực tiếp trên RAM để khớp với Database
                    int newStt = 1;
                    foreach (DataRow row in dtDanhSachGoc.Rows)
                    {
                        // Bỏ qua dòng ảo dưới cùng (nếu có)
                        if (!string.IsNullOrWhiteSpace(row["HoVaTen"]?.ToString()))
                        {
                            row["STT"] = newStt++;
                        }
                    }
                    dtDanhSachGoc.AcceptChanges();
                    // 4.3 Cập nhật hiển thị
                    kryptonDataGridView1.RowCount = dtDanhSachGoc.DefaultView.Count;
                    SendMessage(kryptonDataGridView1.Handle, WM_SETREDRAW, 1, null);
                    kryptonDataGridView1.Invalidate();
                }
                // ===== 5. LÀM SẠCH UI VÀ THỐNG KÊ =====
                ClearThongTin();
                DataCache.Clear(); // Đánh dấu cache hết hạn để các form khác tự lấy lại data mới
                CapNhatThongKeToanBoQuanSo();
                ThongBaoForm4CapNhatLoaiDeXuat();
                toolStripStatusLabel1.Text = "Xóa dữ liệu thành công.";
                // Phát tín hiệu: "Dữ liệu đã thay đổi rồi nhé!"
                Module_DanduongGPS.OnDatabaseChanged?.Invoke();
                await Module_BieuDoTronTrangChu.CapNhatBieuDoForm4Async();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Có lỗi xảy ra trong quá trình xử lý dữ liệu.\nChi tiết: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _dangXuLyXoa = false;
                this.Cursor = Cursors.Default;
            }
        }
        // Hàm xử lý DB cô lập hoàn toàn khỏi UI
        private bool ThucThiXoaDatabase(string csdlPath, int stt)
        {
            using var conn = new SqliteConnection($"Data Source={csdlPath}");
            conn.Open();
            using var transaction = conn.BeginTransaction();
            try
            {
                using (var cmd = new SqliteCommand("DELETE FROM DanhSach WHERE STT = @STT", conn, transaction))
                {
                    cmd.Parameters.Add("@STT", SqliteType.Integer).Value = stt;
                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0)
                    {
                        transaction.Rollback();
                        return false; // Không có gì để xóa
                    }
                }
                // Truyền thẳng transaction vào để tái sử dụng
                DanhLaiSoThuTu(conn, transaction);
                transaction.Commit();
                // Dọn dẹp DB sau khi xóa
                using (var cmdVacuum = new SqliteCommand("VACUUM;", conn))
                {
                    cmdVacuum.ExecuteNonQuery();
                }
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        private void ClearThongTin()
        {
            textBox_STT?.Clear();
            textBox_HoVaTen?.Clear();
            textBox_SoHieu?.Clear();
            textBox_NamSinh?.Clear();
            textBox_QueQuan?.Clear();
            textBox_NgayVaoCAND?.Clear();
            textBox_GhiChu?.Clear();
            if (comboBox_CapBac != null) comboBox_CapBac.SelectedIndex = -1;
            if (comboBox_ChucVu != null) comboBox_ChucVu.SelectedIndex = -1;
            if (comboBox_DonVi != null) comboBox_DonVi.SelectedIndex = -1;
            comboBox_PhanLoai.Text = "";
        }
        // Nâng cấp: Cho phép nhận Transaction từ ngoài vào. Nếu truyền null, nó tự tạo Transaction.
        private void DanhLaiSoThuTu(SqliteConnection conn, SqliteTransaction externalTran = null)
        {
            bool isLocalTran = externalTran == null;
            var tran = isLocalTran ? conn.BeginTransaction() : externalTran;
            try
            {
                using var cmd = new SqliteCommand(@"
        WITH Ordered AS (
            SELECT rowid, ROW_NUMBER() OVER (ORDER BY STT ASC) AS NewSTT
            FROM DanhSach
        )
        UPDATE DanhSach
        SET STT = (
            SELECT NewSTT FROM Ordered 
            WHERE Ordered.rowid = DanhSach.rowid
        );", conn, tran);
                cmd.ExecuteNonQuery();
                // Chỉ Commit nếu hàm này tự tạo Transaction
                if (isLocalTran) tran.Commit();
            }
            catch
            {
                if (isLocalTran) tran.Rollback();
                throw;
            }
        }
        private async void kryptonButton_RefershCSDL_Click(object? sender, EventArgs e)
        {
            // 1. CHỐNG DOUBLE CLICK / RE-ENTRY
            if (Interlocked.Exchange(ref _dangRefresh, 1) == 1)
                return;
            // 2. HỦY TASK CŨ NẾU CÒN
            _refreshCts?.Cancel();
            _refreshCts?.Dispose();
            _refreshCts = new CancellationTokenSource();
            CancellationToken token = _refreshCts.Token;
            Form_Loading? frmLoad = null;
            // 3. LƯU UI GỐC
            string textGoc = kryptonButton_RefershCSDL.Values.Text;
            Image? imageGoc = kryptonButton_RefershCSDL.Values.Image;
            // TỐI ƯU PROGRESSBAR: Loại bỏ hiệu ứng lag/trễ của Windows ProgressBar
            Action<int> updateProgressBarSmooth = (val) =>
            {
                if (!IsDisposed && IsHandleCreated && toolStripProgressBar1_LamMoi != null)
                {
                    int clamped = Math.Clamp(val, 0, 100);
                    if (clamped == 100)
                    {
                        // Mẹo WinForms: Ép WinForms vẽ ngay 100% không bị hiệu ứng trôi
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
                // 4. KIỂM TRA FORM CÒN TỒN TẠI
                if (IsDisposed || !IsHandleCreated)
                    return;
                // 5. KIỂM TRA DỮ LIỆU THỰC TẾ
                bool coDuLieu = dtDanhSachGoc?.AsEnumerable()
                    .Any(r => !string.IsNullOrWhiteSpace(r.Field<string>("HoVaTen"))) == true;
                if (!coDuLieu && _isInit)
                {
                    toolStripStatusLabel1.Text = "Danh sách hiện đang trống";
                    return;
                }
                int tongDong = dtDanhSachGoc?.Rows.Count ?? 0;
                bool laDuLieuLon = tongDong >= 600 || dtDanhSachGoc == null;
                // ===== BỔ SUNG: KHỞI TẠO VÀ HIỂN THỊ PROGRESSBAR =====
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
                // ===== ANIMATION LUỒNG RIÊNG: TĂNG TỐC ĐỘ CHẠY TIẾN TRÌNH =====
                progressCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                progressTask = Task.Run(async () =>
                {
                    int hienTai = 0;
                    while (!progressCts.Token.IsCancellationRequested)
                    {
                        int mucTieu = Volatile.Read(ref mucTieuTienTrinh);
                        if (hienTai < mucTieu)
                        {
                            // ĐÃ TĂNG TỐC: Nhảy mỗi lần +3% (thay vì +1%) để chạy nhanh hơn nhưng vẫn mượt
                            hienTai = Math.Min(hienTai + 3, mucTieu);
                            ((IProgress<int>)progress).Report(hienTai);
                        }
                        else if (hienTai >= 50 && hienTai < 90 && mucTieu >= 50)
                        {
                            // Nhích +2% mỗi nhịp khi chờ tải nặng
                            hienTai = Math.Min(hienTai + 2, 90);
                            ((IProgress<int>)progress).Report(hienTai);
                        }
                        // Delay 4ms giúp animation rất nhạy và lướt nhanh
                        await Task.Delay(4, progressCts.Token).ConfigureAwait(false);
                    }
                }, progressCts.Token);
                // MỐC 1: Bắt đầu chạy lên 25%
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
                // MỐC 2: Kiểm tra CSDL xong → Tăng lên 45%
                Volatile.Write(ref mucTieuTienTrinh, 45);
                toolStripStatusLabel1.Text = "Đang nạp dữ liệu...";
                frmLoad?.CapNhatThongBao("Đang nạp lại dữ liệu, vui lòng đợi...");
                // XÓA CACHE
                DataCache.Clear();
                token.ThrowIfCancellationRequested();
                // MỐC 3: Bắt đầu tải dữ liệu → Cho phép tăng tới 75%
                Volatile.Write(ref mucTieuTienTrinh, 75);
                // LOAD DỮ LIỆU
                await ReloadDuLieu();
                if (IsDisposed || !IsHandleCreated)
                    return;
                token.ThrowIfCancellationRequested();
                // MỐC 4: Reload xong → Tăng tiếp tới 90%
                Volatile.Write(ref mucTieuTienTrinh, 90);
                // CẬP NHẬT UI KHÁC
                CapNhatThongKeToanBoQuanSo();
                KiemTraDuLieu();
                // RESET FILTER
                textBox_TimKiemTheoTen.Clear();
                if (comboBox_TimKiemDonVi.Items.Count > 0)
                    comboBox_TimKiemDonVi.SelectedIndex = 0;
                if (comboBox_XepLoaiThiDua.Items.Count > 0)
                    comboBox_XepLoaiThiDua.SelectedIndex = 0;
                // MỐC 5: Đẩy tiến trình lên 100%
                Volatile.Write(ref mucTieuTienTrinh, 100);
                // Đợi ngắn để đảm bảo dải màu xanh chạy chạm đúng mốc 100%
                while (!IsDisposed && IsHandleCreated && toolStripProgressBar1_LamMoi != null && toolStripProgressBar1_LamMoi.Value < 100)
                {
                    await Task.Delay(5, token);
                }
                // ĐÃ TỐI ƯU: Giảm thời gian chờ chớp mắt từ 100ms xuống 30ms trước khi đóng
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
                // DỪNG LUỒNG ANIMATION PROGRESSBAR
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
                // KHÔI PHỤC UI
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
                // MỞ KHÓA RE-ENTRY
                Interlocked.Exchange(ref _dangRefresh, 0);
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
                using var conn = new SqliteConnection($"Data Source={pathDb}");
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
            bool biKhoa = false;
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
            // 1. VALIDATE DỮ LIỆU ĐẦU VÀO CƠ BẢN
            if (textBox_STT == null || kryptonDataGridView1 == null || dtDanhSachGoc == null)
            {
                MessageBox.Show("Hệ thống chưa sẵn sàng để lưu dữ liệu.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string stt = textBox_STT.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(stt) || !int.TryParse(stt, out _))
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
            Image anhBanDau = kryptonButton_LuuDataCapNhat.Values.Image;
            try
            {
                kryptonButton_LuuDataCapNhat.Enabled = false;
                kryptonButton_LuuDataCapNhat.Values.Text = "Đang lưu...";
                kryptonButton_LuuDataCapNhat.Values.Image = null;
                await Task.Delay(50);
                if (this.IsDisposed) return;
                // Mã hóa đúng GIÁ TRỊ GỐC (phanLoaiGoc) trước khi đưa vào Database
                var encryptedData = await Task.Run(() => new
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
                // 3. LƯU DATABASE
                using var conn = new SqliteConnection("Data Source=" + csdlPath);
                await conn.OpenAsync();
                using var transaction = conn.BeginTransaction();
                string sql = @"UPDATE DanhSach SET HoVaTen = @HoVaTen, SoHieu = @SoHieu, NamSinh = @NamSinh, QueQuan = @QueQuan, NgayVaoCAND = @NgayVaoCAND, CapBac = @CapBac, ChucVu = @ChucVu, DonVi = @DonVi, PhanLoai = @PhanLoai, GhiChu = @GhiChu WHERE STT = @STT";
                using var cmd = new SqliteCommand(sql, conn, transaction);
                cmd.CommandTimeout = 5;
                cmd.Parameters.Add(new SqliteParameter("@HoVaTen", encryptedData.HoVaTen));
                cmd.Parameters.Add(new SqliteParameter("@SoHieu", encryptedData.SoHieu));
                cmd.Parameters.Add(new SqliteParameter("@NamSinh", encryptedData.NamSinh));
                cmd.Parameters.Add(new SqliteParameter("@QueQuan", encryptedData.QueQuan));
                cmd.Parameters.Add(new SqliteParameter("@NgayVaoCAND", encryptedData.NgayVaoCAND));
                cmd.Parameters.Add(new SqliteParameter("@CapBac", encryptedData.CapBac));
                cmd.Parameters.Add(new SqliteParameter("@ChucVu", encryptedData.ChucVu));
                cmd.Parameters.Add(new SqliteParameter("@DonVi", encryptedData.DonVi));
                cmd.Parameters.Add(new SqliteParameter("@PhanLoai", encryptedData.PhanLoai));
                cmd.Parameters.Add(new SqliteParameter("@GhiChu", encryptedData.GhiChu));
                cmd.Parameters.Add(new SqliteParameter("@STT", Convert.ToInt32(stt)));
                if (await cmd.ExecuteNonQueryAsync() <= 0)
                {
                    transaction.Rollback();
                    MessageBox.Show("Không tìm thấy dữ liệu để cập nhật.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                transaction.Commit();
                DataCache.Clear();
                await ReloadDuLieu();
                ApplyFilter();
                CapNhatThongKeToanBoQuanSo();
                ThongBaoForm4CapNhatLoaiDeXuat();
                toolStripStatusLabel1.Text = "Đã lưu thay đổi thành công!";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu dữ liệu:\n" + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!this.IsDisposed)
                {
                    kryptonButton_LuuDataCapNhat.Values.Text = textBanDau;
                    kryptonButton_LuuDataCapNhat.Values.Image = anhBanDau;
                    kryptonButton_LuuDataCapNhat.Enabled = true;
                }
            }
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
                    taiKhoan: string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM) ? "Hệ thống" : Module_TaiKhoan.TenTaiKhoan_RAM,
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
        private void tableLayoutPanel9_Paint(object? sender, PaintEventArgs e)
        {
        }
        private async void kryptonButton_XoaKetQuaPhanLoai_Click(object? sender, EventArgs e)
        {
            if (!KiemTraDuLieuSanSang("xóa kết quả phân loại")) return;
            // Bọc khởi tạo Form vào khối using để tự động giải phóng bộ nhớ khi đóng form
            using (Form16_LamMoi frm16 = new Form16_LamMoi { ShowInTaskbar = false })
            {
                if (frm16.ShowDialog(this) == DialogResult.OK)
                {
                    // Chỉ cập nhật UI nếu Form 16 thực sự thực hiện thay đổi dữ liệu
                    kryptonButton_RefershCSDL.PerformClick();
                    CapNhatThongKeToanBoQuanSo();
                }
            }
            // <--- Tại vị trí này (kết thúc ngoặc nhọn), Form 16 đã được xả sạch khỏi RAM
            // THÊM 2 DÒNG NÀY ĐỂ UI CẬP NHẬT NGAY LẬP TỨC SAU KHI FORM 16 ĐÓNG
            kryptonButton_RefershCSDL.PerformClick(); // Load lại data từ DB (nếu form 16 xóa data gốc)
            CapNhatThongKeToanBoQuanSo(); // Cập nhật lại toàn bộ StatusLabel
            await Module_BieuDoTronTrangChu.CapNhatBieuDoForm4Async();
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
        }
        private void nhapKetQuaPhanLoaiTuDonVi_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!KiemTraDuLieuSanSang("nhập kết quả phân loại từ đơn vị")) return;
            // 2. Lọc dữ liệu thực tế và ép kiểu sang List ngay
            var dsThucTe = dtDanhSachGoc.AsEnumerable()
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
        public async void RefershCSDL_TuFormKhac()
        {
            // Cực kỳ quan trọng: Xóa cache để ép hệ thống đọc lại từ DB (nếu form con có thay đổi dữ liệu)
            DataCache.Clear();
            // Gọi trực tiếp hàm ReloadDuLieu (không thông qua nút bấm)
            await ReloadDuLieu();
            // Đảm bảo thống kê được cập nhật dựa trên dữ liệu mới
            CapNhatThongKeToanBoQuanSo();
        }
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
        }
        // NÚT MENU GỐC: Gọi luồng xuất bình thường
        private void xuatDuLieuThiDuaRaTepExcel_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
          Module_XuatNhapDuLieuThiDua.ThucThiXuatExcel(this, false, dtDanhSachGoc);
        }
        private void kryptonButton1_HuongDanThemDuLieu_Click(object? sender, EventArgs e)
        {
           // if (!KiemTraDuLieuSanSang("thêm thông tin CBCS")) return;
            // 1. Lấy nhanh FormCha & PanelContainer (Ưu tiên cast trực tiếp, không dùng đệ quy)
            var formCha = this.ParentForm as Form2_FormCha ?? Application.OpenForms["Form2_FormCha"] as Form2_FormCha;
            if (formCha == null) return;
            var panel = formCha.Controls["PanelContainer"] as Panel
                        ?? formCha.Controls.Find("PanelContainer", false).FirstOrDefault() as Panel;
            if (panel == null) return;
            try
            {
                // 2. KHÓA VẼ GIAO DIỆN: Giúp chuyển Form tức thì trong 0ms, không bị nháy hình (flicker)
                panel.SuspendLayout();
                // 3. Tìm Form 60 đã có trong Panel chưa
                Form60_NhapLieuCBCS? form60 = null;
                for (int i = 0; i < panel.Controls.Count; i++)
                {
                    if (panel.Controls[i] is Form60_NhapLieuCBCS f60)
                    {
                        form60 = f60;
                        break;
                    }
                }
                // 4. Nếu chưa tồn tại -> Khởi tạo mới
                if (form60 == null)
                {
                    form60 = new Form60_NhapLieuCBCS
                    {
                        TopLevel = false,
                        FormBorderStyle = FormBorderStyle.None,
                        Dock = DockStyle.Fill,
                        Text = @"Nhập liệu CBCS"
                    };
                    // Đăng ký Event bằng phương thức tĩnh/tách biệt để TRÁNH Memory Leak
                    form60.FormClosed += Form60_OnFormClosed;
                    panel.Controls.Add(form60);
                }
                // 5. Ẩn tất cả các control khác nhanh chóng bằng vòng lặp index (Nhanh hơn LINQ OfType)
                for (int i = 0; i < panel.Controls.Count; i++)
                {
                    Control ctrl = panel.Controls[i];
                    if (ctrl != form60 && ctrl.Visible)
                    {
                        ctrl.Hide();
                    }
                }
                // 6. Hiển thị Form 60 và cập nhật Tiêu đề
                form60.Show();
                form60.BringToFront();
                bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem() .Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                string doiTuong = laTanBinh ? "tân binh" : "CBCS";
                formCha.CapNhatTieuDe($"Trang thêm thông tin {doiTuong} vào phần mềm");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở Form nhập liệu CBCS: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // MỞ KHÓA VẼ GIAO DIỆN
                panel.ResumeLayout(true);
            }
        }
        /// <summary>
        /// Xử lý đóng Form 60 và khôi phục Form 6 siêu tốc
        /// </summary>
        private void Form60_OnFormClosed(object? sender, FormClosedEventArgs e)
        {
            if (sender is not Form60_NhapLieuCBCS form60) return;
            // Hủy đăng ký sự kiện ngay lập tức để giải phóng RAM cho GC (Garbage Collector)
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
                    panel.SuspendLayout();
                    form6.Dock = DockStyle.Fill;
                    form6.Show();
                    form6.BringToFront();
                    panel.ResumeLayout(true);
                    // TÁC VỤ NẠP DỮ LIỆU BẤT ĐỒNG BỘ (Background Update)
                    // Giúp Form 6 hiện ra NGAY LẬP TỨC mà không bị khựng chờ CSDL
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await form6.ReloadDuLieu();
                            // Cập nhật lại UI sau khi nạp xong dữ liệu ngầm
                            form6.BeginInvoke(new Action(() =>
                            {
                                form6.CapNhatThongKeToanBoQuanSo();
                            }));
                            await Module_BieuDoTronTrangChu.CapNhatBieuDoForm4Async();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Lỗi reload ngầm: {ex.Message}");
                        }
                    });
                }
            }
            // Ép thu hồi tài nguyên đồ họa (GDI Handles) lập tức
            form60.Dispose();
        }
        /// <summary>
        /// Dựng Form ảo chuyên hiển thị hộp thoại Xác Nhận Yes/No (Màu Xanh Navy).
        /// Trả về True nếu Đồng ý, False nếu Hủy.
        /// </summary>
        private bool HienThiFormAo_XacNhan(string tieuDe, string noiDung)
        {
            string noiDungChuan = noiDung.Replace("\n", Environment.NewLine);
            bool ketQuaDongY = false;
            using (var formAo = new FormAoBase())
            {
                formAo.Text = "Xác nhận thao tác";
                formAo.Size = new System.Drawing.Size(1000, 500);
                formAo.FormBorderStyle = FormBorderStyle.FixedDialog;
                formAo.MaximizeBox = false; formAo.MinimizeBox = false; formAo.ShowIcon = false;
                formAo.ShowInTaskbar = false;
                var panelTop = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Top, Height = 70, Padding = new Padding(30, 25, 20, 5) };
                panelTop.StateCommon.Color1 = System.Drawing.Color.White;
                var lblTitle = new Krypton.Toolkit.KryptonLabel { Text = tieuDe.ToUpper(), Dock = DockStyle.Fill, AutoSize = false };
                lblTitle.StateCommon.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 13F, System.Drawing.FontStyle.Bold);
                lblTitle.StateCommon.ShortText.Color1 = System.Drawing.Color.FromArgb(0, 82, 155);
                panelTop.Controls.Add(lblTitle);
                var separator = new Label { Height = 1, Dock = DockStyle.Top, BackColor = System.Drawing.Color.FromArgb(200, 220, 240), Margin = new Padding(0, 5, 0, 10) };
                var panelContent = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Fill, Padding = new Padding(25, 15, 30, 20) };
                panelContent.StateCommon.Color1 = System.Drawing.Color.White;
                var picIcon = new PictureBox { Image = System.Drawing.SystemIcons.Question.ToBitmap(), SizeMode = PictureBoxSizeMode.CenterImage, Size = new System.Drawing.Size(50, 50), Location = new System.Drawing.Point(25, 15) };
                var txtContent = new Krypton.Toolkit.KryptonTextBox
                {
                    Text = noiDungChuan,
                    ReadOnly = true,
                    Multiline = true,
                    WordWrap = true,
                    ScrollBars = ScrollBars.Vertical,
                    Location = new System.Drawing.Point(90, 15),
                    Width = formAo.Width - 130,
                    Height = panelContent.Height - 35,
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
                };
                txtContent.StateCommon.Back.Color1 = System.Drawing.Color.White;
                txtContent.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.None;
                txtContent.StateCommon.Content.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 11.5F, System.Drawing.FontStyle.Regular);
                txtContent.StateCommon.Content.Color1 = System.Drawing.Color.FromArgb(40, 40, 40);
                txtContent.StateCommon.Content.Padding = new Padding(0);
                var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 75, BackColor = System.Drawing.Color.WhiteSmoke };
                var btnNo = new Krypton.Toolkit.KryptonButton { Text = "Hủy bỏ", Width = 140, Height = 42, DialogResult = DialogResult.No };
                btnNo.StateCommon.Content.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 10.5F, System.Drawing.FontStyle.Bold);
                btnNo.StateCommon.Border.Rounding = 6;
                btnNo.Click += (s, ev) => ketQuaDongY = false;
                var btnYes = new Krypton.Toolkit.KryptonButton { Text = "Đồng ý", Width = 140, Height = 42, DialogResult = DialogResult.Yes };
                btnYes.StateCommon.Content.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 10.5F, System.Drawing.FontStyle.Bold);
                btnYes.StateCommon.Border.Rounding = 6;
                btnYes.Click += (s, ev) => ketQuaDongY = true;
                int totalWidth = btnYes.Width + 20 + btnNo.Width;
                int startX = (formAo.Width - totalWidth) / 2;
                btnYes.Location = new System.Drawing.Point(startX, 16);
                btnNo.Location = new System.Drawing.Point(startX + btnYes.Width + 20, 16);
                panelBottom.Controls.Add(btnYes); panelBottom.Controls.Add(btnNo);
                panelContent.Controls.Add(picIcon); panelContent.Controls.Add(txtContent); panelContent.Controls.Add(separator);
                txtContent.BringToFront(); picIcon.BringToFront(); separator.SendToBack();
                formAo.Controls.Add(panelContent); formAo.Controls.Add(panelTop); formAo.Controls.Add(panelBottom);
                formAo.AcceptButton = btnYes; formAo.CancelButton = btnNo;
                formAo.Shown += (s, ev) => btnNo.Focus(); // An toàn: Focus Hủy bỏ
                formAo.ShowDialog(this);
            }
            return ketQuaDongY;
        }
        /// <summary>
        /// Dựng Form ảo chuyên hiển thị Cảnh Báo Validation (Màu Cam Đất).
        /// </summary>
        /// Dựng Form ảo chuyên hiển thị Lỗi Hệ Thống (Màu Đỏ Thẫm).
        /// Tích hợp nút Copy Lỗi.
        private void cBCSTrongDienQuanLy_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (!KiemTraDuLieuSanSang("quản lý")) return;
            // 2. Lọc dữ liệu thực tế và ép kiểu sang List ngay
            var dsThucTe = dtDanhSachGoc.AsEnumerable()
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
            // 2. Lọc dữ liệu thực tế và ép kiểu sang List ngay
            var dsThucTe = dtDanhSachGoc.AsEnumerable()
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
            var panel = formCha.Controls
                .Find("PanelContainer", true)
                .FirstOrDefault() as Panel;
            if (panel == null) return;
            // tìm form6
            var form6 = panel.Controls
                .OfType<Form6_XuLyData>()
                .FirstOrDefault();
            // Ẩn tất cả form hiện tại
            foreach (System.Windows.Forms.Control ctl in panel.Controls)
            {
                if (ctl is Form frm)
                    frm.Hide();
            }
            // kiểm tra form28 đã tồn tại chưa
            var form28 = panel.Controls
                .OfType<Form28_DataTrungTen>()
                .FirstOrDefault();
            if (form28 == null)
            {
                form28 = new Form28_DataTrungTen
                {
                    TopLevel = false,
                    FormBorderStyle = FormBorderStyle.None,
                    Dock = DockStyle.Fill,
                    Text = "Phân tích dữ liệu trùng tên"
                };
                // ⭐ Khi form28 đóng -> mở lại form6
                form28.FormClosed += (s, ev) =>
                {
                    if (panel.IsDisposed) return;
                    var f6 = panel.Controls
                        .OfType<Form6_XuLyData>()
                        .FirstOrDefault();
                    if (f6 != null && !f6.IsDisposed)
                    {
                        f6.Dock = DockStyle.Fill;
                        f6.Show();
                        f6.BringToFront();
                    }
                };
                panel.Controls.Add(form28);
            }
            // var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            if (formCha != null)
            {
                formCha.CapNhatTieuDe("Trang quản lý CBCS trùng tên");
            }
            form28.Show();
            form28.BringToFront();
        }
        private async void xoaToanBoDuLieuDSCBCS_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (!KiemTraDuLieuSanSang("xóa toàn bộ danh sách CBCS")) return;
            string dbPath = _csdl2Path;
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
            {
                MessageBox.Show("Không tìm thấy csdl2.db!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                // ⭐ TỐI ƯU CỰC ĐỘ: Đếm số dòng trực tiếp từ RAM (dtDanhSachGoc) thay vì chọc CSDL
                int soDongTrongBang = 0;
                if (dtDanhSachGoc != null)
                {
                    soDongTrongBang = dtDanhSachGoc.AsEnumerable().Count(r => !string.IsNullOrWhiteSpace(r.Field<string>("HoVaTen")));
                }
                if (soDongTrongBang == 0)
                {
                    MessageBox.Show("Cơ sở dữ liệu hiện không có dữ liệu CBCS nào để xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                // 🔹 Gọi form xác minh quyền admin
                DialogResult kq;
                using (Form24_XacMinhAdmin frm = new Form24_XacMinhAdmin())
                {
                    frm.TopMost = true;
                    frm.StartPosition = FormStartPosition.CenterScreen;
                    kq = frm.ShowDialog();
                }
                if (kq != DialogResult.OK) return;
                int soDongDaXoa = 0;
                // 🔹 Báo cho Virtual Mode biết lưới đã trống để nhả bộ nhớ lập tức
                kryptonDataGridView1.RowCount = 0;
                kryptonDataGridView1.Refresh();
                using (var conn = new SqliteConnection($"Data Source={dbPath}"))
                {
                    conn.Open();
                    // 🔹 Tắt foreign key
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA foreign_keys = OFF;";
                        cmd.ExecuteNonQuery();
                    }
                    // 🔹 Transaction xóa dữ liệu
                    using (var tran = conn.BeginTransaction())
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tran;
                        cmd.CommandText = "DELETE FROM \"DanhSach\";";
                        soDongDaXoa = cmd.ExecuteNonQuery();
                        cmd.CommandText = "DELETE FROM sqlite_sequence WHERE name='DanhSach';";
                        cmd.ExecuteNonQuery();
                        tran.Commit();
                    }
                    // 🔹 Dọn database
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "VACUUM;";
                        cmd.ExecuteNonQuery();
                    }
                }
                // 🔹 Ghi nhật ký
                Module_NhatKy.GhiNhatKy(
                    Module_TaiKhoan.TenTaiKhoan_RAM,
                    $"Xóa toàn bộ thông tin CBCS ({soDongDaXoa} dòng)",
                    DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")
                );
                MessageBox.Show($"Đã xóa {soDongDaXoa} dòng dữ liệu.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // ⭐ BỔ SUNG: LÀM MỚI BỘ NHỚ VÀ VẼ LẠI GIAO DIỆN SAU KHI XÓA
                DataCache.Clear();            // 1. Xóa sạch RAM để tránh lấy nhầm dữ liệu cũ
                ClearThongTin();              // 2. Xóa trắng các TextBox nhập liệu bên trên
                await ReloadDuLieu();         // 3. Load lại DB (lúc này lưới sẽ vẽ ra dòng đệm ảo và báo CSDL trống)
                CapNhatThongKeToanBoQuanSo(); // 4. Cập nhật nhãn tổng quân số về 0
                ThongBaoForm4CapNhatLoaiDeXuat();
                KiemTraDuLieu();
                await Module_BieuDoTronTrangChu.CapNhatBieuDoForm4Async();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa dữ liệu:\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
        private void ThongBaoForm4CapNhatLoaiDeXuat()
        {
            // Tìm Form 4 đang được mở trong phần mềm
            var form4 = Application.OpenForms.OfType<Form4_TrangDauTien>().FirstOrDefault();
            if (form4 != null && !form4.IsDisposed)
            {
                form4.CapNhatDanhSachPhanLoaiDeXuat();
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
        private void LoadComboBoxChucVu()
        {
            if (comboBox_ChucVu == null) return;
            comboBox_ChucVu.BeginUpdate();
            try
            {
                comboBox_ChucVu.Items.Clear();
                var ds = Module_ChucVu.GetDanhSachChucVu();
                if (ds == null) return;
                foreach (var cvMaHoa in ds)
                {
                    if (string.IsNullOrWhiteSpace(cvMaHoa))
                        continue;
                    // Dùng hàm an toàn
                    string cvGiaiMa = GiaiMaSafe(cvMaHoa);
                    if (!string.IsNullOrWhiteSpace(cvGiaiMa))
                    {
                        comboBox_ChucVu.Items.Add(cvGiaiMa);
                    }
                }
            }
            finally
            {
                comboBox_ChucVu.EndUpdate();
            }
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
                        using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
                        conn.Open();
                        using var cmd = new Microsoft.Data.Sqlite.SqliteCommand("SELECT PhanLoai FROM DanhSach WHERE PhanLoai IS NOT NULL AND PhanLoai <> ''", conn);
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
        private void LoadComboBoxXepLoaiThiDua()
                {
                    // 1. KIỂM TRA AN TOÀN
                    if (dtDanhSachGoc == null ||
                        dtDanhSachGoc.Rows.Count == 0 ||
                        comboBox_XepLoaiThiDua == null ||
                        comboBox_XepLoaiThiDua.IsDisposed)
                    {
                        return;
                    }
                    // 2. KHÓA EVENT TRONG THỜI GIAN NẠP LẠI COMBOBOX
                    _isUpdatingCombo = true;
                    try
                    {
                        // --------------------------------------------------------
                        // Lưu lại VALUE hiện tại
                        // --------------------------------------------------------
                        string giaTriCuValue = null;
                        if (comboBox_XepLoaiThiDua.SelectedItem is ComboboxItem itemCu)
                        {
                            giaTriCuValue = itemCu.Value;
                        }
                        else if (comboBox_XepLoaiThiDua.SelectedValue != null)
                        {
                            giaTriCuValue = comboBox_XepLoaiThiDua.SelectedValue.ToString();
                        }
                        else if (!string.IsNullOrWhiteSpace(comboBox_XepLoaiThiDua.Text))
                        {
                            giaTriCuValue = comboBox_XepLoaiThiDua.Text.Trim();
                        }
                        // 3. LẤY DANH SÁCH PHÂN LOẠI THỰC TẾ
                        var dsPhanLoai = dtDanhSachGoc.AsEnumerable()
                            .Where(r =>
                            {
                                string hoVaTen = r.Field<string>("HoVaTen");
                                return !string.IsNullOrWhiteSpace(hoVaTen);
                            })
                            .Select(r =>
                            {
                                string phanLoai = r.Field<string>("PhanLoai")?.Trim();
                                return string.IsNullOrWhiteSpace(phanLoai)
                                    ? Module_HeThong.PL_KHONG_PL
                                    : phanLoai;
                            })
                            .Distinct()
                            .ToList();
                        // 4. SẮP XẾP THEO THỨ TỰ CHUẨN
                        var thuTuChuan = new List<string>
                {
                    Module_HeThong.Loai_1,
                    Module_HeThong.Loai_2,
                    Module_HeThong.Loai_3,
                    Module_HeThong.Loai_4,
                    Module_HeThong.PL_KHONG_PL
                };
                        var dsFinal = dsPhanLoai
                            .OrderBy(x =>
                            {
                                int index = thuTuChuan.IndexOf(x);
                                return index >= 0
                                    ? index
                                    : int.MaxValue;
                            })
                            .ToList();
                        // 5. XÁC ĐỊNH CHẾ ĐỘ
                        string phienBan =
                            Module_TaiKhoan.LayPhienBanPhanMem()
                            ?? string.Empty;
                        bool laTanBinh =
                            phienBan.Contains(
                                "tân binh",
                                StringComparison.OrdinalIgnoreCase);
                        bool isCBCSAndNam =
                            !laTanBinh && _isCheDoNam;
                        // 6. HÀM LẤY TÊN HIỂN THỊ
                        string LayTenHienThi(string value)
                        {
                            if (value == Module_HeThong.Tat_Ca)
                                return Module_HeThong.Tat_Ca;
                            if (!isCBCSAndNam)
                                return value;
                            if (value == Module_HeThong.Loai_1)
                                return Module_HeThong.PL_CSTD;
                            if (value == Module_HeThong.Loai_2)
                                return Module_HeThong.PL_CSTT;
                            if (value == Module_HeThong.Loai_3)
                                return Module_HeThong.PL_HTNV;
                            if (value == Module_HeThong.Loai_4)
                                return Module_HeThong.PL_KHTNV;
                            return value;
                        }
                        // 7. BẮT ĐẦU CẬP NHẬT COMBOBOX
                        comboBox_XepLoaiThiDua.BeginUpdate();
                        try
                        {
                            comboBox_XepLoaiThiDua.Items.Clear();
                            // Nếu có từ 2 loại trở lên -> thêm "Tất cả"
                            if (dsFinal.Count > 1)
                            {
                                comboBox_XepLoaiThiDua.Items.Add(
                                    new ComboboxItem(
                                        LayTenHienThi(Module_HeThong.Tat_Ca),
                                        Module_HeThong.Tat_Ca));
                            }
                            // Đổ danh sách
                            foreach (string phanLoai in dsFinal)
                            {
                                comboBox_XepLoaiThiDua.Items.Add(
                                    new ComboboxItem(
                                        LayTenHienThi(phanLoai),
                                        phanLoai));
                            }
                        }
                        finally
                        {
                            // Quan trọng:
                            // Không để ComboBox bị giữ trạng thái BeginUpdate.
                            comboBox_XepLoaiThiDua.EndUpdate();
                        }
                        // 8. KHÔNG GÁN TEXT THỦ CÔNG
                        // Đây là điểm quan trọng nhất.
                        //
                        // KHÔNG dùng:
                        //
                        // comboBox_XepLoaiThiDua.Text = ...
                        //
                        // Chỉ thay đổi SelectedItem.
                        ComboboxItem itemCanChon = null;
                        if (!string.IsNullOrWhiteSpace(giaTriCuValue))
                        {
                            itemCanChon = comboBox_XepLoaiThiDua.Items
                                .OfType<ComboboxItem>()
                                .FirstOrDefault(x =>
                                    string.Equals(
                                        x.Value,
                                        giaTriCuValue,
                                        StringComparison.OrdinalIgnoreCase)
                                    ||
                                    string.Equals(
                                        x.Text,
                                        giaTriCuValue,
                                        StringComparison.OrdinalIgnoreCase));
                        }
                        // 9. KHÔI PHỤC GIÁ TRỊ CŨ
                        if (itemCanChon != null)
                        {
                            comboBox_XepLoaiThiDua.SelectedItem = itemCanChon;
                        }
                        // 10. KHÔNG CÓ GIÁ TRỊ CŨ -> CHỌN ITEM ĐẦU TIÊN
                        else if (comboBox_XepLoaiThiDua.Items.Count > 0)
                        {
                            comboBox_XepLoaiThiDua.SelectedIndex = 0;
                        }
                        else
                        {
                            comboBox_XepLoaiThiDua.SelectedIndex = -1;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[Lỗi tại LoadComboBoxXepLoaiThiDua]: {ex}");
                    }
                    finally
                    {
                        // 11. MỞ KHÓA EVENT
                        _isUpdatingCombo = false;
                    }
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
        // 1. KHAI BÁO HẰNG SỐ CHUẨN KỸ SƯ (Quản lý tập trung)
        private static readonly string[] DANH_SACH_PHAN_LOAI = { Module_HeThong.Loai_1, Module_HeThong.Loai_2, Module_HeThong.Loai_3, Module_HeThong.Loai_4, Module_HeThong.PL_KHONG_PL };
        // 2. LÕI TÍNH TOÁN (Đếm siêu tốc O(N) - Chặn rác mã hóa)
        private (int TongSo, Dictionary<string, int> ChiTiet) ThucThiThongKe(DataView? dv)
        {
            var ketQua = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var loai in DANH_SACH_PHAN_LOAI) ketQua[loai] = 0;
            int tongSo = 0;
            if (dv == null || dv.Count == 0) return (tongSo, ketQua);
            foreach (DataRowView row in dv)
            {
                // Dùng ToString() an toàn nhất để tránh DBNull
                string hoTen = row[COT_HO_TEN]?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(hoTen)) continue; // Bỏ qua dòng ảo dưới cùng
                tongSo++; // Tăng quân số hợp lệ
                string phanLoai = row[COT_PHAN_LOAI]?.ToString()?.Trim() ?? "";
                // TƯ DUY WHITE-LIST: Đúng chữ thì đếm, còn lại (rỗng, rác mã hóa) ném vào "Không PL"
                if (phanLoai.Equals(Module_HeThong.Loai_1, StringComparison.OrdinalIgnoreCase)) ketQua[Module_HeThong.Loai_1]++;
                else if (phanLoai.Equals(Module_HeThong.Loai_2, StringComparison.OrdinalIgnoreCase)) ketQua[Module_HeThong.Loai_2]++;
                else if (phanLoai.Equals(Module_HeThong.Loai_3, StringComparison.OrdinalIgnoreCase)) ketQua[Module_HeThong.Loai_3]++;
                else if (phanLoai.Equals(Module_HeThong.Loai_4, StringComparison.OrdinalIgnoreCase)) ketQua[Module_HeThong.Loai_4]++;
                else ketQua[Module_HeThong.PL_KHONG_PL]++;
            }
            return (tongSo, ketQua);
        }
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
        private void CapNhatThongKeSieuToc(DataView dv)
        {
            var data = ThucThiThongKe(dv);
            CapNhatGiaoDienThongKe(data.TongSo, data.ChiTiet);
        }
        public void CapNhatThongKeToanBoQuanSo()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(CapNhatThongKeToanBoQuanSo));
                return;
            }
            if (dtDanhSachGoc != null)
            {
                var data = ThucThiThongKe(dtDanhSachGoc.DefaultView);
                CapNhatGiaoDienThongKe(data.TongSo, data.ChiTiet);
            }
        }
        // Class kết quả (Có thể tái sử dụng cho các chức năng khác)
        public sealed class ImportResult
        {
            public bool IsSuccess { get; set; }
            public int SuccessRows { get; set; }
            public int DeletedRows { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
            public TimeSpan Duration { get; set; }
            public string EngineUsed { get; set; } = string.Empty;
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
        private void LoadComboBoxPhanLoai()
        {
            comboBox_PhanLoai.Items.Clear();
            // 🎯 THÊM 'true' để ép đọc lại từ CSDL2, không lấy Cache cũ trong RAM
            bool isAnhXa = Module_HeThong.IsKichHoatAnhXa(true);
            if (isAnhXa)
            {
                // Danh sách Ánh xạ cho Chế độ NĂM của CBCS
                comboBox_PhanLoai.Items.Add(new ComboboxItem(Module_HeThong.PL_CSTD, Module_HeThong.Loai_1));     // Value = "Loại 1"
                comboBox_PhanLoai.Items.Add(new ComboboxItem(Module_HeThong.PL_CSTT, Module_HeThong.Loai_2));     // Value = "Loại 2"
                comboBox_PhanLoai.Items.Add(new ComboboxItem(Module_HeThong.PL_HTNV, Module_HeThong.Loai_3));     // Value = "Loại 3"
                comboBox_PhanLoai.Items.Add(new ComboboxItem(Module_HeThong.PL_KHTNV, Module_HeThong.Loai_4));    // Value = "Loại 4"
                comboBox_PhanLoai.Items.Add(new ComboboxItem("Không PL", "Không PL")); // Value = "Không PL"
            }
            else
            {
                // Danh sách Gốc cho Tân binh HOẶC CBCS Chế độ THÁNG
                comboBox_PhanLoai.Items.Add(new ComboboxItem(Module_HeThong.Loai_1, Module_HeThong.Loai_1));
                comboBox_PhanLoai.Items.Add(new ComboboxItem(Module_HeThong.Loai_2, Module_HeThong.Loai_2));
                comboBox_PhanLoai.Items.Add(new ComboboxItem(Module_HeThong.Loai_3, Module_HeThong.Loai_3));
                comboBox_PhanLoai.Items.Add(new ComboboxItem(Module_HeThong.Loai_4, Module_HeThong.Loai_4));
                comboBox_PhanLoai.Items.Add(new ComboboxItem("Không PL", "Không PL"));
            }
            comboBox_PhanLoai.SelectedIndex = -1;
            comboBox_PhanLoai.Text = "";
        }
        private void KryptonDataGridView1_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            // 1. Chặn click vào Header (RowIndex = -1) hoặc lỗi ngớ ngẩn
            if (e.RowIndex < 0 || sender is not DataGridView grid || dtDanhSachGoc == null) return;
            // 2. Chặn lỗi lệch pha khi DataView đang được sort/filter ngầm
            if (e.RowIndex >= dtDanhSachGoc.DefaultView.Count) return;
            // 3. Tách việc lấy dữ liệu khỏi luồng vẽ UI để tránh giật lag cục bộ
            this.BeginInvoke(new Action(() =>
            {
                try
                {
                    DataRowView rowView = dtDanhSachGoc.DefaultView[e.RowIndex];
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
                        ComboboxItem itemMatch = comboBox_PhanLoai.Items
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
        private void Form6_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // 3. 🎯 Hủy đăng ký sự kiện khi đóng Form6 để giải phóng tài nguyên
            Module_HeThong.SuKienThayDoiCheDoXetThiDua -= OnCheDoThayDoi;
        }
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
                .Contains("tân binh", StringComparison.OrdinalIgnoreCase);
            //region BƯỚC 1: TƯƠNG TÁC NGƯỜI DÙNG & KIỂM TRA MÔI TRƯỜNG (UI Thread)
            using OpenFileDialog ofd = new OpenFileDialog
            {
                //Filter = "Excel Files (*.xlsx)|*.xlsx",
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
            Application.DoEvents();
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
                        this.Enabled = false; this.UseWaitCursor = true; Application.DoEvents();
                        await Task.Run(async () =>
                        {
                            using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_csdl2Path}");
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
                    // Đối với FastDataReader, Module của bạn đã cấu hình mặc định là xóa CSDL.
                    // Đã bỏ MessageBox và thay bằng Ghi Nhật Ký ngầm theo yêu cầu
                    xoaDuLieuCu = true;
                    string tenHienTai = Module_TaiKhoan.TenTaiKhoan_RAM;
                    string thongDiepNhatKy = "Hệ thống nạp dữ liệu excel tự động kích hoạt FastDataReader và làm sạch dữ liệu cũ (Tệp > 1500 dòng).";
                    // Nếu hệ thống của bạn có sử dụng class SessionInfo ở form này thì mở comment 2 dòng dưới:
                    // SessionInfo.TenTaiKhoan = tenHienTai;
                    // SessionInfo.ThoiGianDangNhap = DateTime.Now;
                    Module_NhatKy.GhiNhatKy(
                         taiKhoan: tenHienTai,
                         hanhDong: thongDiepNhatKy,
                         ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                     );
                }
                else
                {
                    // Dưới 1500 dòng, cho phép người dùng lựa chọn nạp nối tiếp hay xóa
                    if (MessageBox.Show(this, "Bạn có muốn xóa toàn bộ dữ liệu cũ trước khi nạp không?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        xoaDuLieuCu = true;
                    }
                }
                // 🔴 KHÓA LẠI UI ĐỂ CHẠY BACKGROUND THREAD
                this.Enabled = false;
                this.UseWaitCursor = true;
                Application.DoEvents();
                //endregion
                //region BƯỚC 4: NẠP DỮ LIỆU CHÍNH (Background Thread)
                List<string> danhSachTrung = new List<string>();
                await Task.Run(() =>
                {
                    if (tongSoDongDuLieu > 1500)
                    {
                        // SỬA LỖI Ở ĐÂY: Truyền thêm tham số xoaDuLieuCu vào hàm
                        Debug.WriteLine($"[Tối Ưu] Kích hoạt FastDataReader cho {tongSoDongDuLieu} dòng.");
                        if (laTanBinh)
                            Module_XuatNhapDuLieuThiDua.NhapDanhSachExcelTanBinhFastDataReader(excelPath, xoaDuLieuCu);
                        else
                            Module_XuatNhapDuLieuThiDua.NhapDanhSachExcelCBCSFastDataReader(excelPath, xoaDuLieuCu);
                    }
                    else
                    {
                        // Dưới 1500 dòng (ClosedXML)
                        Debug.WriteLine($"[Tiêu Chuẩn] Kích hoạt ClosedXML cho {tongSoDongDuLieu} dòng.");
                        if (laTanBinh)
                            danhSachTrung = Module_XuatNhapDuLieuThiDua.NhapDanhSachExcelTanBinh(excelPath, xoaDuLieuCu);
                        else
                            danhSachTrung = Module_XuatNhapDuLieuThiDua.NhapDanhSachExcelCBCS(excelPath, xoaDuLieuCu);
                    }
                    // ✅ CHÈN VÀO ĐÂY: Tự động chạy trích xuất và đồng bộ Sheet Sổ Vàng Ba Nhất
                    Module_BaNhat.NhapDuLieuVaoBangQuanLyBaNhat(excelPath, _csdl2Path);
                    //
                });
                //endregion
                //region BƯỚC 5: ĐỒNG BỘ VÀ CLEANUP (UI Thread)
                DataCache.Clear();
                await ReloadDuLieu();
                ApplyFilter();
                //ApplyColumnColoring();
                CapNhatThongKeToanBoQuanSo();
                KiemTraDuLieu();
                await Module_BieuDoTronTrangChu.CapNhatBieuDoForm4Async();
                var frm12 = Application.OpenForms.OfType<Form12>().FirstOrDefault();
                if (frm12 != null && frm12.IsHandleCreated && !frm12.IsDisposed)
                    frm12.BeginInvoke(new Action(() => frm12.LoadFromSQLite()));
                string nhomDoiTuong = laTanBinh ? "Tân binh" : "CBCS";
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, $"Nạp thành công dữ liệu thi đua {nhomDoiTuong} từ tệp Excel", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
                this.UseWaitCursor = false; this.Enabled = true; this.Activate();
                // 🔥 XỬ LÝ KẾT QUẢ HIỂN THỊ TRÊN UI THREAD
                if (danhSachTrung != null && danhSachTrung.Count > 0)
                {
                    string msg = $"Nạp dữ liệu thành công!\nĐã quét tổng cộng: {tongSoDongDuLieu} hồ sơ.\n\nTuy nhiên, các dòng sau bị trùng số hiệu và đã bị bỏ qua:\n\n{string.Join("\n", danhSachTrung.Take(10))}";
                    if (danhSachTrung.Count > 10) msg += $"\n...và {danhSachTrung.Count - 10} dòng khác.";
                    MessageBox.Show(this, msg, "Cảnh báo trùng dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    //MessageBox.Show(this, $"Nạp dữ liệu từ tệp Excel thành công!\nĐã xử lý tổng cộng: {tongSoDongDuLieu} dòng.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        // Đặt cùng khối khai báo biến ở đầu class Form6_XuLyData
        private bool _isRefreshingTask = false; // Cờ khóa chống gọi trùng lặp tiến trình Làm mới
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
    } // Ngoai luong
}
public static class DataGridViewExtensions  
{
    public static void EnableDoubleBuffered(this DataGridView dgv, bool setting = true)
    {
        Type dgvType = dgv.GetType();
        PropertyInfo pi = dgvType.GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
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