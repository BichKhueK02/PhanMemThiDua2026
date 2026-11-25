// Phiên bản 3 - Tối ưu: hợp nhất khung gọn của bản 2 với độ mượt khi nạp của bản 1
// TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
namespace PhanMemThiDua2026
{
    public partial class Form15_ThongKeThiDua : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private readonly string _csdl4Path = Module_DanduongGPS.DuongDanCSDL4;
        // Font dùng chung, tránh rò rỉ GDI handle
        private static readonly Font _fontGridHeader = new Font(Module_HeThong.TenFontHeThong, 9.5F, FontStyle.Bold);
        private static readonly Font _fontGridCell = new Font(Module_HeThong.TenFontHeThong, 9.5F, FontStyle.Regular);
        private static readonly Font _fontGridHighlight = new Font(Module_HeThong.TenFontHeThong, 9.5F, FontStyle.Bold);
        //public bool DaLoadDuLieu { get; set; } = false;
        private DataTable? dtDanhSachGoc;
        private List<string> _cotPhatSinhCacheThongKe = new List<string>();
        private bool _dangMoThongKeTapThe = false;
        private const string PLACEHOLDER_TIMKIEM = Module_HeThong.Nhap_Tim_Kiem;
        private bool _dangSetPlaceholder = false;
        private bool _isInitialized = false;
        // Bảng tra theo chỉ số cột, dựng 1 lần sau khi nạp cột: CellValueNeeded không so sánh chuỗi cho từng ô
        private int[] _cotNguon = Array.Empty<int>();
        private Func<RawCBCS, string>[] _layCBCS = Array.Empty<Func<RawCBCS, string>>();
        private Func<RawTanBinh, string>[] _layTanBinh = Array.Empty<Func<RawTanBinh, string>>();
        private SolidBrush? _rowHeaderBrush = null;
        private StringFormat? _rowHeaderFormat = null;
        private System.Windows.Forms.Timer? _timKiemTimer;
        private const int NGUONG_DU_LIEU = 3000;
        private const string GHICHU_TRONG = "(Không có ghi chú)"; // mục đặc biệt trong bộ lọc Ghi chú: lọc các dòng chưa có ghi chú
        private Color _mauChuTrangThai;
        // Các cột "ảo" chỉ tồn tại trong RAM (không hiển thị, không có trong CSDL)
        private const string COT_UU_TIEN = "SortPriority";
        private const string COT_DV_KEY = "DonVi_Key";
        private const string COT_TEN_SEARCH = "HoVaTen_Search";
        private static bool LaCotAo(string ten) => ten == COT_UU_TIEN || ten == COT_DV_KEY || ten == COT_TEN_SEARCH;
        // Cache phiên bản đang hiển thị (CBCS/Tân binh): không gọi lại hàm đọc phiên bản cho từng ô
        private bool? _hienThiTanBinh = null;
        private bool HienThiTanBinh => _hienThiTanBinh ??= LaTanBinhHienTai();
        private static bool LaTanBinhHienTai() => (Module_TaiKhoan.LayPhienBanPhanMem() ?? "").LaCheDoTanBinh();
        // Nhật ký chẩn đoán quá trình nạp (Database\NhatKyNhaPhatTrien\form15-yyyyMMdd.log): ghi từng bước + lỗi bị nuốt
        internal static void NK(string noiDung) =>
            Module_NhatKyNhaPhatTrien.GhiDong("form15", $"[T{Environment.CurrentManagedThreadId}] {noiDung}");
        private bool _isDataMaxMode = false; // Cờ theo dõi đang chạy chế độ thường hay DataMax
        // Bộ nhớ đệm dữ liệu thô cho chế độ DataMax
        private List<RawCBCS> _dataCacheCBCS = new List<RawCBCS>();
        private List<RawTanBinh> _dataCacheTanBinh = new List<RawTanBinh>();
        private int _dangXuLyLuongNen = 0;     // 1 = đang nạp/đồng bộ nặng (chống chạy chồng)
        private volatile bool _canLamMoiLai = false; // có yêu cầu làm mới đến trong lúc đang bận
        private int _tamHoanLoc = 0;           // >0: đang nạp lại danh sách lọc -> combobox không tự kích hoạt lọc
        private int _locSeq = 0;               // số thứ tự lần lọc mới nhất, kết quả lần lọc cũ bị bỏ
        private DateTime _lastReload = DateTime.MinValue;
        private bool _daKhoiTaoToolTip = false;
        private List<int> _filteredIndexes = new List<int>();
        // Cột cơ bản của từng bảng (cột nào không nằm trong danh sách này là cột phát sinh)
        private static readonly string[] CotCoBanCBCS = { "ID", "HoVaTen", "SoHieu", "DonVi", "TinhTrang", "KQ_ThiDua_Nam_Cu", "KQ_XepLoaiCB_Nam_Cu", "KQ_XepLoaiDangVien_Nam_Cu", "Thang_12_Nam_Cu", "Thang_1", "Thang_2", "Thang_3", "Thang_4", "Thang_5", "Sau_Thang_Dau_Nam", "Thang_6", "Thang_7", "Thang_8", "Thang_9", "Thang_10", "Thang_11", "TongKet_Nam", "TS_Loai1", "TS_Loai2", "TS_Loai3", "TS_Loai4" };
        private static readonly HashSet<string> TapCoBanCBCS = new HashSet<string>(CotCoBanCBCS, StringComparer.OrdinalIgnoreCase);
        // Nhóm tuần (Tân binh / Học viên): cột cơ bản = cột chung + mọi cột thời gian của lược đồ (Module_LichXet)
        private static HashSet<string> TapCoBanTanBinh => new HashSet<string>(
            new[] { "ID", "HoVaTen", "SoHieu", "DonVi", "TinhTrang", "TS_Loai1", "TS_Loai2", "TS_Loai3", "TS_Loai4" }
                .Concat(Module_LichXet.CotVatLy(Module_CheDoDoiTuong.Hien)), StringComparer.OrdinalIgnoreCase);
        // Các cột tháng dùng để đếm Tổng loại 1..4
        private static readonly string[] CotThangCBCS = { "Thang_12_Nam_Cu", "Thang_1", "Thang_2", "Thang_3", "Thang_4", "Thang_5", "Thang_6", "Thang_7", "Thang_8", "Thang_9", "Thang_10", "Thang_11" };
        // Nhóm tuần: đếm Tổng loại trên các cột KẾT QUẢ THÁNG của lịch đang dùng
        private static string[] CotThangTanBinh => Module_LichXet.CotKetQua(Module_CheDoDoiTuong.Hien).Select(c => $"[{c}]").ToArray();
        private class RawCBCS
        {
            public string ID = "", HoTenE = "", SoHieuE = "", DonViE = "", TinhTrang = "", KQ_TD = "", KQ_XL_CB = "", KQ_XL_DV = "", T12 = "";
            public string DonViKey = ""; // Khóa đơn vị đã chuẩn hóa dùng riêng cho bộ lọc
            public int IdSo = int.MaxValue; // ID dạng số để sắp xếp đúng (chuỗi "10" < "2")
            public string[] Thang = new string[11]; // T1 -> T11
            public string SauThang = "", TongKet = "", L1 = "", L2 = "", L3 = "", L4 = "";
            public string GhiChu = "";
            public string HoTenSearch = ""; // họ tên chữ thường, dùng cho ô tìm kiếm
            public int SortPriority;
            public Dictionary<string, string> CotPhatSinh = new Dictionary<string, string>();
        }
        private class RawTanBinh
        {
            public string ID = "", HoTenE = "", SoHieuE = "", DonViE = "", TinhTrang = "", L1 = "", L2 = "", L3 = "", L4 = "";
            public string DonViKey = "";
            public int IdSo = int.MaxValue;
            public string GhiChu = "";
            public string HoTenSearch = "";
            public int SortPriority;
            public Dictionary<string, string> DuLieuThang = new Dictionary<string, string>();
            public Dictionary<string, string> CotPhatSinh = new Dictionary<string, string>();
        }
        public Form15_ThongKeThiDua()
        {
            InitializeComponent();
            Module_BieuTuongNut.ApDung(this);   // biểu tượng nút: Hiện đại = tự vẽ, Cổ điển = ảnh gốc nhúng trong exe
            Module_GiaoDienForm15Web.ApDung(this);
            _mauChuTrangThai = toolStripStatusLabel1.ForeColor;
            BatDoubleBufferDeQuy(this);   // giảm chớp khi layout/resize các container lồng nhau
            Module_TaiKhoan.OnPhienBanThayDoi += Module_TaiKhoan_OnPhienBanThayDoi;
            // Cài lại lịch xét Học viên: cột tuần/tháng đổi => nạp lại lưới
            Action khiLichDoi = () => { if (!IsDisposed && IsHandleCreated) BeginInvoke(new Action(() => { _chuKyCot = ""; _lastReload = DateTime.MinValue; _ = ReloadData(); })); };
            Module_LichXet.LichThayDoi += khiLichDoi;
            Disposed += (_, _) => Module_LichXet.LichThayDoi -= khiLichDoi;
            this.KeyPreview = true;
            InitToolTips();
        }
        // ===== Giảm chớp: bật DoubleBuffered cho container (không đụng Krypton/ComboBox) =====
        private static void BatDoubleBufferDeQuy(Control goc)
        {
            foreach (Control c in goc.Controls)
            {
                if (c is TableLayoutPanel || c is GroupBox || c is Panel || c is SplitContainer || c is TabPage)
                {
                    typeof(Control).GetProperty("DoubleBuffered",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                        ?.SetValue(c, true);
                }
                BatDoubleBufferDeQuy(c);
            }
        }
        private bool _daHienLanDau = false;
        private void Form15_ThongKeThiDua_Load(object? sender, EventArgs e)
        {
            if (_daHienLanDau) return;
            _daHienLanDau = true;
            KhoiTaoMotLan();   // đã nạp sẵn lúc form còn ẩn thì bỏ qua
            DatConTroLanDau();
            _ = Module_HoSoThiDua.DungSanAsync(this); // dựng sẵn Form22/Form62 (bấm đúp sửa mở tức thì)
        }
        /// <summary>
        /// Phần khởi tạo không cần form đang hiện (định dạng lưới, menu...). Idempotent.
        /// Form53 gọi khi nạp sẵn dữ liệu lúc form còn ẩn, trước khi gọi ReloadData.
        /// </summary>
        internal void KhoiTaoMotLan()
        {
            if (_isInitialized || IsDisposed) return;
            DinhDangDataGirdView_HienThi();
            // Không dùng thanh tiến trình khi nạp dữ liệu
            toolStripProgressBar1_LamMoi.Visible = false;
            toolStripProgressBar1_LamMoi.Enabled = false;
            Module_HoTroLuuDataTheoNamCu.CapNhatAnHienMenuDongBo(dongBoKetQuaThiDuaNamTruocVeHienTaiToolStripMenuItem);
            Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1);
            contextMenuStrip1.Opening -= contextMenuStrip1_Opening;
            contextMenuStrip1.Opening += contextMenuStrip1_Opening;
            CapNhatQuyenMenuTheoPhienBan();
            _isInitialized = true;
        }
        private void DatConTroLanDau()
        {
            // Đưa con trỏ vào ô tìm kiếm
            this.BeginInvoke(new Action(() =>
            {
                if (this.IsDisposed || !this.IsHandleCreated) return;
                if (textBox_TimKiemTheoTen != null && textBox_TimKiemTheoTen.Visible)
                    this.ActiveControl = textBox_TimKiemTheoTen;
                else
                    this.ActiveControl = kryptonDataGridView1;
                if (kryptonDataGridView1.RowCount > 0 && kryptonDataGridView1.ColumnCount > 0)
                {
                    try
                    {
                        kryptonDataGridView1.ClearSelection();
                        var cot = kryptonDataGridView1.Columns.GetFirstColumn(DataGridViewElementStates.Visible);
                        if (cot != null) kryptonDataGridView1.CurrentCell = kryptonDataGridView1.Rows[0].Cells[cot.Index];
                        kryptonDataGridView1.Rows[0].Selected = true;
                    }
                    catch { }
                }
            }));
        }
        // Dọn dẹp khi đóng Form (hủy đăng ký sự kiện, giải phóng RAM)
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Module_TaiKhoan.OnPhienBanThayDoi -= Module_TaiKhoan_OnPhienBanThayDoi;
            Interlocked.Increment(ref _locSeq); // hủy các lần lọc đang chờ
            if (_timKiemTimer != null)
            {
                _timKiemTimer.Stop();
                _timKiemTimer.Dispose();
                _timKiemTimer = null;
            }
            _rowHeaderBrush?.Dispose();
            _rowHeaderFormat?.Dispose();
            _filteredIndexes = new List<int>();
            _dataCacheCBCS.Clear();
            _dataCacheTanBinh.Clear();
            dtDanhSachGoc?.Dispose();
            dtDanhSachGoc = null;
            base.OnFormClosed(e);
        }
        // Form12 đổi phiên bản -> cập nhật menu rồi nạp lại dữ liệu (luôn chạy trên luồng giao diện)
        private async void Module_TaiKhoan_OnPhienBanThayDoi(object? sender, EventArgs e)
        {
            try
            {
                if (this.IsDisposed || !this.IsHandleCreated) return;
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() => Module_TaiKhoan_OnPhienBanThayDoi(sender, e)));
                    return;
                }
                _sttCache = null; // STT/Ghi chú lấy từ bảng gốc khác (DanhSach <-> DanhSachTanBinh)
                CapNhatQuyenMenuTheoPhienBan();
                Module_HoTroLuuDataTheoNamCu.CapNhatAnHienMenuDongBo(dongBoKetQuaThiDuaNamTruocVeHienTaiToolStripMenuItem);
                await LamMoiDuLieuAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi đổi phiên bản: " + ex.Message);
            }
        }
        private void CapNhatQuyenMenuTheoPhienBan()
        {
            // Chỉ hiển thị MenuItem khi ở chế độ "tân binh"
            if (tinhPhanLoaiThang_ToolStripMenuItem != null)
                tinhPhanLoaiThang_ToolStripMenuItem.Visible = LaTanBinhHienTai();
        }
        private void contextMenuStrip1_Opening(object? sender, CancelEventArgs e)
        {
            CapNhatMenuSuaThongTin();
            CapNhatMenuXoaDuLieu();
            Module_HoTroLuuDataTheoNamCu.CapNhatAnHienMenuDongBo(dongBoKetQuaThiDuaNamTruocVeHienTaiToolStripMenuItem);
        }
        private Action SafeAction(Action action)
        {
            return () =>
            {
                if (action == null) return;
                try { action.Invoke(); }
                catch (Exception ex) { Debug.WriteLine("Lỗi thực thi phím tắt: " + ex.Message); }
            };
        }
        private bool SafeExecute(Action action)
        {
            SafeAction(action).Invoke();
            return true;
        }
        private void DinhDangDataGirdView_HienThi()
        {
            Module_DonVi.KhoiTao();
            textBox_TimKiemTheoTen.TextChanged -= textBox_TimKiemTheoTen_TextChanged;
            textBox_TimKiemTheoTen.TextChanged += textBox_TimKiemTheoTen_TextChanged;
            comboBox_TimKiemDonVi.SelectedIndexChanged -= comboBox_TimKiemDonVi_SelectedIndexChanged;
            comboBox_TimKiemDonVi.SelectedIndexChanged += comboBox_TimKiemDonVi_SelectedIndexChanged;
            comboBox1_TinhTrang.SelectedIndexChanged -= comboBox1_TinhTrang_SelectedIndexChanged;
            comboBox1_TinhTrang.SelectedIndexChanged += comboBox1_TinhTrang_SelectedIndexChanged;
            comboBox1_GhiChu.SelectedIndexChanged -= comboBox1_GhiChu_SelectedIndexChanged;
            comboBox1_GhiChu.SelectedIndexChanged += comboBox1_GhiChu_SelectedIndexChanged;
            kryptonDataGridView1.CellDoubleClick -= KryptonDataGridView1_CellDoubleClick;
            kryptonDataGridView1.CellDoubleClick += KryptonDataGridView1_CellDoubleClick;
            kryptonDataGridView1.RowHeadersVisible = true;
            kryptonDataGridView1.RowHeadersWidth = 60;
            kryptonDataGridView1.RowPostPaint -= KryptonDataGridView1_RowPostPaint;
            kryptonDataGridView1.RowPostPaint += KryptonDataGridView1_RowPostPaint;
            kryptonDataGridView1.ContextMenuStrip = contextMenuStrip1;
            kryptonDataGridView1.MouseDown -= KryptonDataGridView1_MouseDown;
            kryptonDataGridView1.MouseDown += KryptonDataGridView1_MouseDown; // chuột phải chọn đúng dòng/ô vừa bấm
            // VirtualMode: lưới chỉ hỏi dữ liệu cho các ô đang nhìn thấy
            kryptonDataGridView1.VirtualMode = true;
            kryptonDataGridView1.DataSource = null;
            kryptonDataGridView1.AllowUserToAddRows = false;
            kryptonDataGridView1.AllowUserToDeleteRows = false;
            kryptonDataGridView1.AllowUserToResizeRows = false;
            kryptonDataGridView1.ReadOnly = true;
            // Lưới chỉ đọc, không dùng biểu tượng lỗi -> tắt để bớt hàng trăm lượt gọi mỗi khung hình
            kryptonDataGridView1.ShowCellErrors = false;
            kryptonDataGridView1.ShowRowErrors = false;
            kryptonDataGridView1.ShowEditingIcon = false;
            // Cột Ghi chú không xuống dòng -> xem đầy đủ bằng tooltip khi rê chuột
            kryptonDataGridView1.CellToolTipTextNeeded -= KryptonDataGridView1_CellToolTipTextNeeded;
            kryptonDataGridView1.CellToolTipTextNeeded += KryptonDataGridView1_CellToolTipTextNeeded;
            // Chặn Grid tự quét để tính kích thước
            kryptonDataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            kryptonDataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            kryptonDataGridView1.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            // Bộ đệm kép chống chớp giật
            typeof(DataGridView).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(kryptonDataGridView1, true, null);
            kryptonDataGridView1.CellValueNeeded -= KryptonDataGridView1_CellValueNeeded;
            kryptonDataGridView1.CellValueNeeded += KryptonDataGridView1_CellValueNeeded;
            InitPlaceholderTimKiem();
            textBox_TimKiemTheoTen.Enter += (s, e) =>
            {
                if (textBox_TimKiemTheoTen.Text == PLACEHOLDER_TIMKIEM)
                {
                    _dangSetPlaceholder = true;
                    textBox_TimKiemTheoTen.Text = "";
                    textBox_TimKiemTheoTen.ForeColor = SystemColors.WindowText;
                    _dangSetPlaceholder = false;
                }
            };
            textBox_TimKiemTheoTen.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBox_TimKiemTheoTen.Text))
                    InitPlaceholderTimKiem();
            };
            _timKiemTimer = new System.Windows.Forms.Timer();
            _timKiemTimer.Interval = 200; // Trễ 200ms: gõ xong mới lọc
            _timKiemTimer.Tick += (s, ev) =>
            {
                _timKiemTimer?.Stop();
                ApplyFilter();
            };
            CapNhatPhienBanPhanMem();
        }
        // =====================================================================
        //  ĐỌC DANH SÁCH GỐC (CSDL2.DanhSach): GHI CHÚ + STT, CHỈ MỘT LẦN ĐỌC / GIẢI MÃ
        // =====================================================================
        private sealed class DuLieuDanhSach
        {
            public Dictionary<string, string> GhiChu = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // khóa: SoHieu
            public Dictionary<string, int> STT = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);        // khóa: SoHieu (đã Trim)
        }
        // Trả về null nếu đọc thất bại (DB đang bận...). Thử lại 1 lần, nếu vẫn lỗi thì đọc không kèm STT.
        private DuLieuDanhSach? DocDanhSachCSDL2()
        {
            bool dungSTT = true;
            for (int lan = 0; lan < 3; lan++)
            {
                try
                {
                    var raw = new List<(string sh, string gc, int stt)>();
                    using (var cn2 = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl2Path)))
                    {
                        cn2.Open();
                        using var cmd = new SqliteCommand(dungSTT ? $"SELECT SoHieu, GhiChu, STT FROM {Module_TaiKhoan.BangDanhSach}" : $"SELECT SoHieu, GhiChu FROM {Module_TaiKhoan.BangDanhSach}", cn2);
                        using var rd = cmd.ExecuteReader();
                        while (rd.Read())
                        {
                            string sh = rd.IsDBNull(0) ? "" : rd.GetValue(0)?.ToString() ?? "";
                            string gc = rd.IsDBNull(1) ? "" : rd.GetValue(1)?.ToString() ?? "";
                            int stt = int.MaxValue;
                            if (dungSTT && !rd.IsDBNull(2) && int.TryParse(rd.GetValue(2)?.ToString(), out int s)) stt = s;
                            raw.Add((sh, gc, stt));
                        }
                    }
                    var shGiaiMa = new string[raw.Count];
                    var gcGiaiMa = new string[raw.Count];
                    Parallel.For(0, raw.Count, i =>
                    {
                        shGiaiMa[i] = SafeDecrypt(raw[i].sh);
                        gcGiaiMa[i] = SafeDecrypt(raw[i].gc);
                    });
                    var kq = new DuLieuDanhSach();
                    for (int i = 0; i < raw.Count; i++)
                    {
                        string sh = shGiaiMa[i];
                        if (string.IsNullOrEmpty(sh)) continue;
                        kq.GhiChu.TryAdd(sh, gcGiaiMa[i]); // số hiệu trùng thì lấy dòng đầu
                        string shTrim = sh.Trim();
                        if (shTrim.Length > 0) kq.STT.TryAdd(shTrim, raw[i].stt);
                    }
                    return kq;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Lỗi đọc DanhSach CSDL2: " + ex.Message);
                    if (lan == 0) Thread.Sleep(150); // DB có thể đang bị khóa trong tích tắc
                    else dungSTT = false;            // lỗi tiếp -> bỏ cột STT (có thể bảng chưa có cột này)
                }
            }
            return null;
        }
        private volatile Dictionary<string, int>? _sttCache; // STT của DanhSach csdl2
        private Dictionary<string, int>? LayBanDoSTT()
        {
            var c = _sttCache;
            if (c != null) return c;
            var r = DocDanhSachCSDL2();
            if (r != null) { _sttCache = r.STT; return r.STT; }
            return null;
        }
        // =====================================================================
        //  NẠP DỮ LIỆU: CHẾ ĐỘ THƯỜNG (<= NGUONG_DU_LIEU dòng)
        // =====================================================================
        private sealed class GoiThuong
        {
            public DataTable Bang = null!;
            public List<string> CotPhatSinh = null!;
            public bool LaTanBinh;
            public Dictionary<string, int>? STT;
        }
        // Có thể chạy ở luồng nền (không đụng giao diện)
        private GoiThuong? DocGoiThuong(bool laTanBinh)
        {
            if (!File.Exists(_csdl4Path)) return null;
            var dt = new DataTable();
            using (var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path)))
            {
                cn.Open();
                using var cmd = new SqliteCommand($"SELECT * FROM [{(laTanBinh ? Module_CheDoDoiTuong.BangThiDuaThang : Module_CheDoDoiTuong.ThiDuaThang(CheDoDoiTuong.CBCS))}]", cn);
                using var rd = cmd.ExecuteReader();
                dt.Load(rd);
            }
            var dl = DocDanhSachCSDL2(); // không đọc được thì để trống, không làm hỏng việc nạp
            var dictGhiChu = dl?.GhiChu ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var tapCoBan = laTanBinh ? TapCoBanTanBinh : TapCoBanCBCS;
            var phatSinh = new List<string>();
            foreach (DataColumn c in dt.Columns)
                if (!tapCoBan.Contains(c.ColumnName) && c.ColumnName != "GhiChu") phatSinh.Add(c.ColumnName);
            if (!dt.Columns.Contains("GhiChu")) dt.Columns.Add("GhiChu", typeof(string));
            dt.Columns.Add(COT_UU_TIEN, typeof(int));
            dt.Columns.Add(COT_DV_KEY, typeof(string));
            dt.Columns.Add(COT_TEN_SEARCH, typeof(string));
            var dicUuTien = TaoBangUuTienDonVi();
            int oHt = dt.Columns["HoVaTen"]!.Ordinal, oSh = dt.Columns["SoHieu"]!.Ordinal, oDv = dt.Columns["DonVi"]!.Ordinal;
            int n = dt.Rows.Count;
            // Giải mã song song (ở bước này chỉ đọc DataTable)
            var kq = new (string ht, string sh, string dv, string gc, int pri, string dvKey, string search)[n];
            Parallel.For(0, n, i =>
            {
                DataRow row = dt.Rows[i];
                string sh = SafeDecrypt(row[oSh]?.ToString());
                string dv = SafeDecrypt(row[oDv]?.ToString());
                string ht = SafeDecrypt(row[oHt]?.ToString());
                int pri = dicUuTien.TryGetValue(dv, out int p) ? p : int.MaxValue;
                string gc = dictGhiChu.TryGetValue(sh, out string? g) ? g : "";
                kq[i] = (ht, sh, dv, gc, pri, ChuanHoaDonViSoSanh(dv), BoLocDanhSach.ChuanHoaTimKiem(ht));
            });
            int cHt = oHt, cSh = oSh, cDv = oDv;
            int cGc = dt.Columns["GhiChu"]!.Ordinal, cPri = dt.Columns[COT_UU_TIEN]!.Ordinal;
            int cKey = dt.Columns[COT_DV_KEY]!.Ordinal, cSearch = dt.Columns[COT_TEN_SEARCH]!.Ordinal;
            dt.BeginLoadData(); // ghi hàng loạt, không kiểm tra/chỉ mục từng dòng
            for (int i = 0; i < n; i++)
            {
                var r = dt.Rows[i];
                r[cHt] = kq[i].ht;
                r[cSh] = kq[i].sh;
                r[cDv] = kq[i].dv;
                r[cGc] = kq[i].gc;
                r[cPri] = kq[i].pri;
                r[cKey] = kq[i].dvKey;
                r[cSearch] = kq[i].search;
            }
            dt.EndLoadData();
            dt.PrimaryKey = new[] { dt.Columns["ID"]! };
            dt.DefaultView.Sort = COT_UU_TIEN + " ASC, ID ASC";
            return new GoiThuong { Bang = dt, CotPhatSinh = phatSinh, LaTanBinh = laTanBinh, STT = dl?.STT };
        }
        private void ApDungGoiThuong(GoiThuong g)
        {
            ApDungDuLieuVaoLuoi(g.Bang, g.CotPhatSinh, g.LaTanBinh, false, g.STT, () =>
            {
                _dataCacheCBCS = new List<RawCBCS>();      // chế độ thường không dùng bộ đệm DataMax -> trả RAM
                _dataCacheTanBinh = new List<RawTanBinh>();
            });
        }
        // Gắn dữ liệu mới vào lưới (luôn chạy ở luồng giao diện). Chưa lọc: người gọi sẽ lọc sau.
        private void ApDungDuLieuVaoLuoi(DataTable bang, List<string> cotPhatSinh, bool laTanBinh, bool dataMax, Dictionary<string, int>? stt, Action ganBoDem)
        {
            using var _do = Module_DoHieuNang.Doan("Form15: ApDungDuLieuVaoLuoi (gán bảng + cột vào lưới)");
            var grid = kryptonDataGridView1;
            grid.SuspendLayout();
            try
            {
                _hienThiTanBinh = laTanBinh;
                _isDataMaxMode = dataMax;
                Interlocked.Increment(ref _locSeq);   // hủy các lần lọc đang chờ trên dữ liệu cũ
                _filteredIndexes = new List<int>();   // chỉ số lọc cũ không còn đúng
                // Chữ ký cấu trúc cột: nếu không đổi (F5, sửa 1 dòng, đồng bộ...) thì GIỮ NGUYÊN cột + định dạng,
                // chỉ thay dữ liệu -> không xóa/dựng lại ~40 cột, không nháy lưới, giữ vị trí cuộn và cột đang rộng.
                string chuKy = TaoChuKyCot(bang, cotPhatSinh, laTanBinh);
                bool giuCot = grid.Columns.Count > 0 && chuKy == _chuKyCot;
                if (!giuCot)
                {
                    grid.DataSource = null;
                    grid.Columns.Clear();             // xóa cột -> lưới về 0 dòng
                }
                var bangCu = dtDanhSachGoc;
                dtDanhSachGoc = bang;
                bangCu?.Dispose();
                ganBoDem();
                _cotPhatSinhCacheThongKe = cotPhatSinh;
                _sttCache = stt;
                if (!giuCot)
                {
                    ThemCotVaoLuoi(bang, laTanBinh);
                    ApDungDinhDangGridThongKe();
                    _chuKyCot = chuKy;
                }
                DungBanDoCot();
            }
            finally
            {
                grid.ResumeLayout();
            }
        }
        private string _chuKyCot = "";
        // Gồm: loại dữ liệu, năm hệ thống (đổi tiêu đề cột), tên các cột và cột phát sinh
        private static string TaoChuKyCot(DataTable bang, List<string> cotPhatSinh, bool laTanBinh)
        {
            var sb = new System.Text.StringBuilder(512);
            sb.Append(laTanBinh ? 'T' : 'C').Append('|').Append(Module_HeThong.LayNamHeThong()).Append('|');
            foreach (DataColumn c in bang.Columns) sb.Append(c.ColumnName).Append(',');
            sb.Append('|');
            foreach (var p in cotPhatSinh) sb.Append(p).Append(',');
            return sb.ToString();
        }
        // Thêm cột đúng thứ tự hiển thị ngay từ đầu -> không phải chỉnh DisplayIndex từng cột (rất chậm)
        private void ThemCotVaoLuoi(DataTable bang, bool laTanBinh)
        {
            var thuTu = new List<string>(bang.Columns.Count);
            var daCo = new HashSet<string>(StringComparer.Ordinal);
            void Them(string ten)
            {
                if (!LaCotAo(ten) && bang.Columns.Contains(ten) && daCo.Add(ten)) thuTu.Add(ten);
            }
            Them("HoVaTen"); Them("SoHieu"); Them("DonVi"); Them("TinhTrang");
            if (laTanBinh)
            {
                foreach (string cot in Module_LichXet.CotTheoThuTu(Module_CheDoDoiTuong.Hien)) Them(cot);
            }
            else
            {
                Them("KQ_ThiDua_Nam_Cu"); Them("KQ_XepLoaiCB_Nam_Cu"); Them("KQ_XepLoaiDangVien_Nam_Cu"); Them("Thang_12_Nam_Cu");
                for (int i = 1; i <= 5; i++) Them($"Thang_{i}");
                Them("Sau_Thang_Dau_Nam");
                for (int i = 6; i <= 11; i++) Them($"Thang_{i}");
                Them("TongKet_Nam");
            }
            Them("TS_Loai1"); Them("TS_Loai2"); Them("TS_Loai3"); Them("TS_Loai4");
            foreach (var cp in _cotPhatSinhCacheThongKe) if (cp != "GhiChu") Them(cp);
            Them("GhiChu");
            foreach (DataColumn c in bang.Columns) Them(c.ColumnName); // các cột còn lại (ID, Thang_2, Tuan_x_T6...)
            var cols = new DataGridViewColumn[thuTu.Count];
            for (int i = 0; i < cols.Length; i++)
            {
                cols[i] = new DataGridViewTextBoxColumn
                {
                    Name = thuTu[i],
                    HeaderText = thuTu[i],
                    SortMode = DataGridViewColumnSortMode.NotSortable
                };
            }
            kryptonDataGridView1.Columns.AddRange(cols);
        }
        // Dựng bảng tra theo chỉ số cột (gọi sau khi lưới đã có cột)
        private void DungBanDoCot()
        {
            var cols = kryptonDataGridView1.Columns;
            int n = cols.Count;
            var nguon = new int[n];
            var layCB = new Func<RawCBCS, string>[n];
            var layTB = new Func<RawTanBinh, string>[n];
            var dt = dtDanhSachGoc;
            for (int i = 0; i < n; i++)
            {
                string ten = cols[i].Name;
                nguon[i] = dt != null ? dt.Columns.IndexOf(ten) : -1;
                layCB[i] = LayCotCBCS(ten);
                layTB[i] = LayCotTanBinh(ten);
            }
            _cotNguon = nguon;
            _layCBCS = layCB;
            _layTanBinh = layTB;
        }
        // Cách đọc giá trị một cột từ bản ghi thô CBCS
        private static Func<RawCBCS, string> LayCotCBCS(string ten)
        {
            switch (ten)
            {
                case "HoVaTen": return d => d.HoTenE;
                case "SoHieu": return d => d.SoHieuE;
                case "DonVi": return d => d.DonViE;
                case "TinhTrang": return d => d.TinhTrang;
                case "GhiChu": return d => d.GhiChu;
                case "KQ_ThiDua_Nam_Cu": return d => d.KQ_TD;
                case "KQ_XepLoaiCB_Nam_Cu": return d => d.KQ_XL_CB;
                case "KQ_XepLoaiDangVien_Nam_Cu": return d => d.KQ_XL_DV;
                case "Thang_12_Nam_Cu": return d => d.T12;
                case "Sau_Thang_Dau_Nam": return d => d.SauThang;
                case "TongKet_Nam": return d => d.TongKet;
                case "TS_Loai1": return d => d.L1;
                case "TS_Loai2": return d => d.L2;
                case "TS_Loai3": return d => d.L3;
                case "TS_Loai4": return d => d.L4;
            }
            if (ten.StartsWith("Thang_", StringComparison.Ordinal) && int.TryParse(ten.AsSpan(6), out int m) && m >= 1 && m <= 11)
            {
                int vt = m - 1;
                return d => d.Thang[vt];
            }
            return d => d.CotPhatSinh.TryGetValue(ten, out string? v) ? v : string.Empty;
        }
        // Cách đọc giá trị một cột từ bản ghi thô Tân binh
        private static Func<RawTanBinh, string> LayCotTanBinh(string ten)
        {
            switch (ten)
            {
                case "HoVaTen": return d => d.HoTenE;
                case "SoHieu": return d => d.SoHieuE;
                case "DonVi": return d => d.DonViE;
                case "TinhTrang": return d => d.TinhTrang;
                case "GhiChu": return d => d.GhiChu;
                case "TS_Loai1": return d => d.L1;
                case "TS_Loai2": return d => d.L2;
                case "TS_Loai3": return d => d.L3;
                case "TS_Loai4": return d => d.L4;
            }
            return d => d.DuLieuThang.TryGetValue(ten, out string? v) ? v
                      : d.CotPhatSinh.TryGetValue(ten, out string? w) ? w : string.Empty;
        }
        // ----- API công khai giữ nguyên tên: nạp + lọc ngay (đồng bộ) -----
        public void LoadThongKe_TanBinh()
        {
            var g = DocGoiThuong(true);
            if (g == null) return;
            ApDungGoiThuong(g);
            ApplyFilter_Standard();
        }
        private async Task NapCheDoThuongAsync(bool laTanBinh)
        {
            var g = await Task.Run(() => DocGoiThuong(laTanBinh)); // đọc + giải mã ở luồng nền, giao diện không đơ
            if (g == null || IsDisposed) return;
            ApDungGoiThuong(g);
        }
        // =====================================================================
        //  NẠP DỮ LIỆU: CHẾ ĐỘ DATAMAX (> NGUONG_DU_LIEU dòng)
        // =====================================================================
        private static string Doc(SqliteDataReader rd, int o) => o < 0 || rd.IsDBNull(o) ? "" : (rd.GetValue(o)?.ToString() ?? "");
        private (List<RawCBCS> Ds, DataTable Schema, List<string> PhatSinh, Dictionary<string, int>? STT) DocDataMaxCBCS()
        {
            var ds = new List<RawCBCS>();
            var schema = new DataTable();
            var phatSinh = new List<string>();
            using (var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path)))
            {
                cn.Open();
                using var cmd = new SqliteCommand($"SELECT * FROM [{Module_CheDoDoiTuong.ThiDuaThang(CheDoDoiTuong.CBCS)}]", cn);
                using var rd = cmd.ExecuteReader();
                var ord = new Dictionary<string, int>(rd.FieldCount, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < rd.FieldCount; i++)
                {
                    string ten = rd.GetName(i);
                    ord[ten] = i;
                    schema.Columns.Add(ten, typeof(string));
                    if (!TapCoBanCBCS.Contains(ten) && ten != "GhiChu") phatSinh.Add(ten);
                }
                if (!schema.Columns.Contains("GhiChu")) schema.Columns.Add("GhiChu", typeof(string));
                int O(string t) => ord.TryGetValue(t, out int v) ? v : -1;
                int oID = O("ID"), oHt = O("HoVaTen"), oSh = O("SoHieu"), oDv = O("DonVi"), oTT = O("TinhTrang");
                int oKQ = O("KQ_ThiDua_Nam_Cu"), oXLCB = O("KQ_XepLoaiCB_Nam_Cu"), oXLDV = O("KQ_XepLoaiDangVien_Nam_Cu"), oT12 = O("Thang_12_Nam_Cu");
                int oSau = O("Sau_Thang_Dau_Nam"), oTK = O("TongKet_Nam");
                int oL1 = O("TS_Loai1"), oL2 = O("TS_Loai2"), oL3 = O("TS_Loai3"), oL4 = O("TS_Loai4");
                var oThang = new int[11];
                for (int i = 0; i < 11; i++) oThang[i] = O($"Thang_{i + 1}");
                var oPS = phatSinh.Select(t => (ten: t, o: O(t))).ToArray();
                while (rd.Read())
                {
                    var it = new RawCBCS
                    {
                        ID = Doc(rd, oID),
                        HoTenE = Doc(rd, oHt),
                        SoHieuE = Doc(rd, oSh),
                        DonViE = Doc(rd, oDv),
                        TinhTrang = Doc(rd, oTT),
                        KQ_TD = Doc(rd, oKQ),
                        KQ_XL_CB = Doc(rd, oXLCB),
                        KQ_XL_DV = Doc(rd, oXLDV),
                        T12 = Doc(rd, oT12),
                        SauThang = Doc(rd, oSau),
                        TongKet = Doc(rd, oTK),
                        L1 = Doc(rd, oL1),
                        L2 = Doc(rd, oL2),
                        L3 = Doc(rd, oL3),
                        L4 = Doc(rd, oL4)
                    };
                    it.IdSo = int.TryParse(it.ID, out int idSo) ? idSo : int.MaxValue;
                    for (int i = 0; i < 11; i++) it.Thang[i] = Doc(rd, oThang[i]);
                    foreach (var (ten, o) in oPS) it.CotPhatSinh[ten] = Doc(rd, o);
                    ds.Add(it);
                }
            }
            var dl = DocDanhSachCSDL2();
            var gc = dl?.GhiChu;
            var dicUuTien = TaoBangUuTienDonVi();
            Parallel.ForEach(ds, it =>
            {
                it.SoHieuE = SafeDecrypt(it.SoHieuE);
                it.DonViE = SafeDecrypt(it.DonViE);
                it.DonViKey = ChuanHoaDonViSoSanh(it.DonViE);
                it.HoTenE = SafeDecrypt(it.HoTenE);
                it.SortPriority = dicUuTien.TryGetValue(it.DonViE, out int p) ? p : int.MaxValue;
                it.GhiChu = gc != null && gc.TryGetValue(it.SoHieuE, out string? g) ? g : "";
                it.HoTenSearch = BoLocDanhSach.ChuanHoaTimKiem(it.HoTenE);
            });
            ds = ds.OrderBy(x => x.SortPriority).ThenBy(x => x.IdSo).ToList();
            return (ds, schema, phatSinh, dl?.STT);
        }
        private (List<RawTanBinh> Ds, DataTable Schema, List<string> PhatSinh, Dictionary<string, int>? STT) DocDataMaxTanBinh()
        {
            var ds = new List<RawTanBinh>();
            var schema = new DataTable();
            var phatSinh = new List<string>();
            using (var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path)))
            {
                cn.Open();
                using var cmd = new SqliteCommand($"SELECT * FROM [{Module_CheDoDoiTuong.BangThiDuaThang}]", cn);
                using var rd = cmd.ExecuteReader();
                var ord = new Dictionary<string, int>(rd.FieldCount, StringComparer.OrdinalIgnoreCase);
                var cotThang = new List<(string ten, int o)>(); // cột tuần/tháng cơ bản
                for (int i = 0; i < rd.FieldCount; i++)
                {
                    string ten = rd.GetName(i);
                    ord[ten] = i;
                    schema.Columns.Add(ten, typeof(string));
                    if (!TapCoBanTanBinh.Contains(ten))
                    {
                        if (ten != "GhiChu") phatSinh.Add(ten);
                    }
                    else if (ten.StartsWith("Tuan_", StringComparison.OrdinalIgnoreCase) || ten.StartsWith("Thang_", StringComparison.OrdinalIgnoreCase))
                    {
                        cotThang.Add((ten, i));
                    }
                }
                if (!schema.Columns.Contains("GhiChu")) schema.Columns.Add("GhiChu", typeof(string));
                int O(string t) => ord.TryGetValue(t, out int v) ? v : -1;
                int oID = O("ID"), oHt = O("HoVaTen"), oSh = O("SoHieu"), oDv = O("DonVi"), oTT = O("TinhTrang");
                int oL1 = O("TS_Loai1"), oL2 = O("TS_Loai2"), oL3 = O("TS_Loai3"), oL4 = O("TS_Loai4");
                var oPS = phatSinh.Select(t => (ten: t, o: O(t))).ToArray();
                while (rd.Read())
                {
                    var it = new RawTanBinh
                    {
                        ID = Doc(rd, oID),
                        HoTenE = Doc(rd, oHt),
                        SoHieuE = Doc(rd, oSh),
                        DonViE = Doc(rd, oDv),
                        TinhTrang = Doc(rd, oTT),
                        L1 = Doc(rd, oL1),
                        L2 = Doc(rd, oL2),
                        L3 = Doc(rd, oL3),
                        L4 = Doc(rd, oL4)
                    };
                    it.IdSo = int.TryParse(it.ID, out int idSo) ? idSo : int.MaxValue;
                    foreach (var (ten, o) in cotThang) it.DuLieuThang[ten] = Doc(rd, o);
                    foreach (var (ten, o) in oPS) it.CotPhatSinh[ten] = Doc(rd, o);
                    ds.Add(it);
                }
            }
            var dl = DocDanhSachCSDL2();
            var gc = dl?.GhiChu;
            var dicUuTien = TaoBangUuTienDonVi();
            Parallel.ForEach(ds, it =>
            {
                it.SoHieuE = SafeDecrypt(it.SoHieuE);
                it.DonViE = SafeDecrypt(it.DonViE);
                it.DonViKey = ChuanHoaDonViSoSanh(it.DonViE);
                it.HoTenE = SafeDecrypt(it.HoTenE);
                it.SortPriority = dicUuTien.TryGetValue(it.DonViE, out int p) ? p : int.MaxValue;
                it.GhiChu = gc != null && gc.TryGetValue(it.SoHieuE, out string? g) ? g : "";
                it.HoTenSearch = BoLocDanhSach.ChuanHoaTimKiem(it.HoTenE);
            });
            ds = ds.OrderBy(x => x.SortPriority).ThenBy(x => x.IdSo).ToList();
            return (ds, schema, phatSinh, dl?.STT);
        }
        private async Task NapDataMaxCBCSAsync()
        {
            if (!File.Exists(_csdl4Path)) return;
            var kq = await Task.Run(DocDataMaxCBCS);
            if (IsDisposed) return;
            ApDungDuLieuVaoLuoi(kq.Schema, kq.PhatSinh, false, true, kq.STT, () =>
            {
                _dataCacheCBCS = kq.Ds;
                _dataCacheTanBinh = new List<RawTanBinh>();
            });
        }
        private async Task NapDataMaxTanBinhAsync()
        {
            if (!File.Exists(_csdl4Path)) return;
            var kq = await Task.Run(DocDataMaxTanBinh);
            if (IsDisposed) return;
            ApDungDuLieuVaoLuoi(kq.Schema, kq.PhatSinh, true, true, kq.STT, () =>
            {
                _dataCacheTanBinh = kq.Ds;
                _dataCacheCBCS = new List<RawCBCS>();
            });
        }
        // ----- API công khai giữ nguyên tên: nạp + lọc ngay -----
        private async Task DieuPhoiLoadDuLieuAsync(bool laTanBinh)
        {
            string tableName = (laTanBinh ? Module_CheDoDoiTuong.BangThiDuaThang : Module_CheDoDoiTuong.ThiDuaThang(CheDoDoiTuong.CBCS));
            int tongSo = await Task.Run(() =>
            {
                try
                {
                    using var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path));
                    cn.Open();
                    using var cmd = new SqliteCommand($"SELECT COUNT(ID) FROM {tableName}", cn);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
                catch (Exception ex) { NK($"DieuPhoi: LỖI đếm {tableName}: {ex.Message}"); return 0; }
            });
            NK($"DieuPhoi: {tableName} = {tongSo} dòng -> {(tongSo > NGUONG_DU_LIEU ? "DataMax" : "thường")}");
            if (IsDisposed) return;
            if (tongSo > NGUONG_DU_LIEU)
            {
                if (laTanBinh) await NapDataMaxTanBinhAsync();
                else await NapDataMaxCBCSAsync();
            }
            else
            {
                await NapCheDoThuongAsync(laTanBinh);
            }
        }
        // Bảng ưu tiên đơn vị: tên đơn vị -> thứ tự (dùng để sắp xếp)
        private static Dictionary<string, int> TaoBangUuTienDonVi()
        {
            string[] ds = Module_DonVi.LayDanhSachDonViUuTienArray();
            var dic = new Dictionary<string, int>(ds.Length, Module_DonVi.SoSanhBang);   // so chuỗi tên chính xác
            for (int i = 0; i < ds.Length; i++) dic.TryAdd(ds[i], i);
            return dic;
        }
        private static int TinhUuTienDonVi(string? donVi)
        {
            return Module_DonVi.LayThuTu(donVi);
        }
        // =====================================================================
        //  LỌC
        // =====================================================================
        public void ApplyFilter() => _ = LocLaiAsync();
        // Bản chờ được: dùng khi cần đọc _filteredIndexes ngay sau khi lọc xong
        private Task LocLaiAsync()
        {
            int phien = Interlocked.Increment(ref _locSeq); // hủy các lần lọc cũ còn đang chờ
            if (_isDataMaxMode) return ApplyFilter_DataMaxAsync(phien);
            ApplyFilter_Standard();
            return Task.CompletedTask;
        }
        // Nạp lại các combobox lọc rồi lọc đúng 1 lần (các combobox không tự kích hoạt lọc trong lúc nạp)
        private async Task NapLaiBoLocVaLocAsync(bool datLaiTinhTrang)
        {
            _tamHoanLoc++;
            try
            {
                using (Module_DoHieuNang.Doan("Form15: LoadComboBoxGhiChu")) LoadComboBoxGhiChu();
                using (Module_DoHieuNang.Doan("Form15: LoadComboBoxDonVi")) LoadComboBoxDonVi();
                if (datLaiTinhTrang && comboBox1_TinhTrang != null && comboBox1_TinhTrang.Items.Count > 0)
                    comboBox1_TinhTrang.SelectedIndex = 0;
            }
            finally { _tamHoanLoc--; }
            await LocLaiAsync();
        }
        // Đưa các ô lọc về mặc định mà KHÔNG kích hoạt lọc
        private void DatLaiBoLocVeMacDinh()
        {
            _tamHoanLoc++;
            try
            {
                InitPlaceholderTimKiem();
                if (comboBox_TimKiemDonVi.Items.Count > 0) comboBox_TimKiemDonVi.SelectedIndex = 0;
                if (comboBox1_TinhTrang.Items.Count > 0) comboBox1_TinhTrang.SelectedIndex = 0;
                if (comboBox1_GhiChu.Items.Count > 0) comboBox1_GhiChu.SelectedIndex = 0;
            }
            finally { _tamHoanLoc--; }
        }
        // Chuẩn hóa tên đơn vị CHỈ để so sánh bộ lọc (không đổi dữ liệu hiển thị / lưu trữ).
        // Xử lý: Unicode NFC/NFD, khoảng trắng lạ (NBSP...), các loại gạch ngang, khoảng trắng quanh '-' ',' ';', hoa/thường.
        private static string ChuanHoaDonViSoSanh(string? input) => Module_DonVi.KhoaSapXep(input);
        // Trạng thái bộ lọc đọc 1 lần từ giao diện (để luồng nền dùng an toàn)
        private sealed class BoLoc
        {
            public bool CoTen, CoDv, CoTT, CoGC, GCTrong;
            public string TenLower = "", DvKey = "", TinhTrang = "", GCKey = "";
            public string[] Tu = Array.Empty<string>();   // các từ khóa tên đã bỏ dấu; từ nào cũng phải có mặt
            public bool KhopTen(string? s)
            {
                if (Tu.Length == 0) return true;
                s ??= "";
                for (int t = 0; t < Tu.Length; t++)
                    if (s.IndexOf(Tu[t], StringComparison.Ordinal) < 0) return false;
                return true;
            }
        }
        private BoLoc LayBoLoc()
        {
            var b = new BoLoc();
            string tenGoc = (textBox_TimKiemTheoTen.Text ?? "").Trim();
            if (_dangSetPlaceholder || string.Equals(tenGoc, PLACEHOLDER_TIMKIEM, StringComparison.OrdinalIgnoreCase)) tenGoc = "";
            string ten = BoLocDanhSach.ChuanHoaTimKiem(tenGoc);   // bỏ dấu + chữ thường: "nguyen van" khớp "Nguyễn Văn"
            b.TenLower = ten;
            b.Tu = ten.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            b.CoTen = b.Tu.Length > 0;
            string dv = comboBox_TimKiemDonVi.Text?.Trim() ?? "";
            b.CoDv = !string.IsNullOrWhiteSpace(dv) && dv != Module_HeThong.Tat_Ca;
            b.DvKey = b.CoDv ? ChuanHoaDonViSoSanh(dv) : "";
            string tt = comboBox1_TinhTrang.Text?.Trim() ?? "";
            b.CoTT = !string.IsNullOrWhiteSpace(tt) && tt != Module_HeThong.Tat_Ca;
            b.TinhTrang = tt;
            (b.CoGC, b.GCTrong, b.GCKey) = LayBoLocGhiChu();
            return b;
        }
        private void DatTrangThai(int soDong, bool dataMax, bool dangCho = false)
        {
            if (dataMax)
            {
                toolStripStatusLabel1.ForeColor = Color.FromArgb(25, 135, 84);
                toolStripStatusLabel1.Text = $"Tổng cộng: {soDong} {Module_HeThong.Tu_dong_chi} [Chế độ hiệu năng cao{(dangCho ? " đang hoạt động..." : "")}]";
            }
            else
            {
                toolStripStatusLabel1.ForeColor = _mauChuTrangThai;
                toolStripStatusLabel1.Text = $"Tổng cộng: {soDong} {Module_HeThong.Tu_dong_chi}";
            }
        }
        private void XoaLuoi()
        {
            if (kryptonDataGridView1.RowCount > 0) Module_LuoiAo.DatSoDong(kryptonDataGridView1, 0, boChon: true);
        }
        // Áp kết quả lọc lên lưới: tắt vẽ + đổi số dòng theo cách nhanh. Cùng đúng tập dòng (cùng thứ tự) thì không đụng tới lưới.
        // (RowCount giảm từng dòng là nguyên nhân lọc 6000 người xuống ~3800 người mất hàng chục giây.)
        private void CapNhatLuoiSauLoc(List<int> ketQua)
        {
            var grid = kryptonDataGridView1;
            var cu = _filteredIndexes;
            bool giongHet = cu != null && cu.Count == ketQua.Count && grid.RowCount == ketQua.Count;
            if (giongHet)
            {
                for (int i = 0; i < ketQua.Count; i++)
                    if (cu![i] != ketQua[i]) { giongHet = false; break; }
            }
            _filteredIndexes = ketQua;
            if (giongHet) return;
            // Số dòng đổi nhiều = đã sang tập người khác -> bỏ chọn; đổi ít (vd: sửa 1 dòng) thì giữ nguyên dòng đang chọn
            // Lưới ảo tô chọn theo SỐ THỨ TỰ dòng, không theo người: đang chọn nhiều dòng mà tập dòng đổi
            // thì các dòng đang tô đã là người khác -> luôn bỏ chọn để tránh xóa nhầm.
            bool boChon = cu == null || Math.Abs(cu.Count - ketQua.Count) > 64 || ketQua.Count == 0
                || grid.Rows.GetRowCount(DataGridViewElementStates.Selected) > 1;
            using var _ = Module_LuoiAo.TamDungVe(grid);
            Module_LuoiAo.DatSoDong(grid, ketQua.Count, boChon);
        }
        // CHẾ ĐỘ THƯỜNG (<= 3000 dòng): lọc đồng bộ trên DefaultView
        private void ApplyFilter_Standard()
        {
            var grid = kryptonDataGridView1;
            if (grid == null || grid.IsDisposed) return;
            var dt = dtDanhSachGoc;
            if (dt == null || dt.DefaultView.Count == 0)
            {
                _filteredIndexes = new List<int>();
                XoaLuoi();
                DatTrangThai(0, false);
                return;
            }
            var bl = LayBoLoc();
            grid.SuspendLayout();
            try
            {
                var view = dt.DefaultView;
                int count = view.Count;
                int oDv = dt.Columns.IndexOf("DonVi"), oDvKey = dt.Columns.IndexOf(COT_DV_KEY);
                int oTT = dt.Columns.IndexOf("TinhTrang"), oGC = dt.Columns.IndexOf("GhiChu");
                int oTenSearch = dt.Columns.IndexOf(COT_TEN_SEARCH), oTen = dt.Columns.IndexOf("HoVaTen");
                var ketQua = new List<int>(count);
                var kep = new KetQuaBoLocKep();
                for (int i = 0; i < count; i++)
                {
                    var r = view[i];
                    if (bl.CoTT && !string.Equals((r[oTT]?.ToString() ?? "").Trim(), bl.TinhTrang, StringComparison.OrdinalIgnoreCase)) continue;
                    if (bl.CoTen)
                    {
                        string s = oTenSearch >= 0 ? r[oTenSearch]?.ToString() ?? "" : BoLocDanhSach.ChuanHoaTimKiem(r[oTen]?.ToString());
                        if (!bl.KhopTen(s)) continue;
                    }
                    string k = oDvKey >= 0 ? r[oDvKey]?.ToString() ?? "" : ChuanHoaDonViSoSanh(r[oDv]?.ToString());
                    if (kep.Xet(bl, k, oGC >= 0 ? r[oGC]?.ToString() : "")) ketQua.Add(i);
                }
                CapNhatComboBoLocKep(kep);
                if (ketQua.Count > 1) ketQua = new List<int>(SapXepChiSo(ketQua, TaoBoLaySXHienTai()));
                CapNhatLuoiSauLoc(ketQua);
                DatTrangThai(ketQua.Count, false);
            }
            catch (Exception ex)
            {
                NK("LọcThường LỖI: " + ex);
                Debug.WriteLine("Lỗi ApplyFilter_Standard: " + ex.Message);
                _filteredIndexes = new List<int>();
                XoaLuoi();
            }
            finally
            {
                grid.ResumeLayout();
                grid.Invalidate();
            }
        }
        // CHẾ ĐỘ DATAMAX (> 3000 dòng): lọc ở luồng nền để không treo giao diện
        private async Task ApplyFilter_DataMaxAsync(int phienLoc)
        {
            var grid = kryptonDataGridView1;
            if (grid == null || grid.IsDisposed) return;
            bool laTanBinh = HienThiTanBinh;
            // Chụp danh sách tại thời điểm lọc để luồng nền không đọc List đang bị luồng giao diện sửa
            RawCBCS[] dsCB = laTanBinh ? Array.Empty<RawCBCS>() : _dataCacheCBCS.ToArray();
            RawTanBinh[] dsTB = laTanBinh ? _dataCacheTanBinh.ToArray() : Array.Empty<RawTanBinh>();
            int total = laTanBinh ? dsTB.Length : dsCB.Length;
            if (total == 0)
            {
                _filteredIndexes = new List<int>();
                XoaLuoi();
                DatTrangThai(0, true, true);
                return;
            }
            var bl = LayBoLoc();
            try
            {
                var kep = new KetQuaBoLocKep();
                var ketQua = await Task.Run(() =>
                {
                    var tam = new List<int>(total);
                    for (int i = 0; i < total; i++)
                    {
                        string donViKey, tinhTrang, ghiChu, hoTenSearch;
                        if (laTanBinh) { var it = dsTB[i]; donViKey = it.DonViKey; tinhTrang = it.TinhTrang; ghiChu = it.GhiChu; hoTenSearch = it.HoTenSearch; }
                        else { var it = dsCB[i]; donViKey = it.DonViKey; tinhTrang = it.TinhTrang; ghiChu = it.GhiChu; hoTenSearch = it.HoTenSearch; }
                        if (bl.CoTT && !string.Equals(tinhTrang, bl.TinhTrang, StringComparison.OrdinalIgnoreCase)) continue;
                        if (bl.CoTen && !bl.KhopTen(hoTenSearch)) continue;
                        if (kep.Xet(bl, donViKey, ghiChu)) tam.Add(i);
                    }
                    if (tam.Count <= 1) return tam;
                    Func<int, (string, string, string)> lay = laTanBinh
                        ? (i => (dsTB[i].DonViE ?? "", dsTB[i].SoHieuE ?? "", dsTB[i].TinhTrang ?? ""))
                        : (i => (dsCB[i].DonViE ?? "", dsCB[i].SoHieuE ?? "", dsCB[i].TinhTrang ?? ""));
                    return new List<int>(SapXepChiSo(tam, lay));
                });
                // Trong lúc chờ đã có lần lọc/nạp mới hơn (hoặc form đã đóng) -> bỏ kết quả cũ
                if (phienLoc != Volatile.Read(ref _locSeq) || IsDisposed || grid.IsDisposed) { NK($"LọcDataMax: bỏ kết quả cũ ({ketQua.Count} dòng)"); return; }
                NK($"LọcDataMax: {ketQua.Count}/{total} dòng tanBinh={laTanBinh}");
                CapNhatComboBoLocKep(kep);
                using var _do = Module_DoHieuNang.Doan("Form15: cập nhật lưới sau lọc DataMax");
                grid.SuspendLayout();
                try
                {
                    CapNhatLuoiSauLoc(ketQua);
                    DatTrangThai(ketQua.Count, true);
                }
                finally
                {
                    grid.ResumeLayout();
                    grid.Invalidate();
                }
            }
            catch (Exception ex)
            {
                NK("LọcDataMax LỖI: " + ex);
                Debug.WriteLine("Lỗi ApplyFilter_DataMax: " + ex.Message);
                if (!IsDisposed && !grid.IsDisposed)
                {
                    _filteredIndexes = new List<int>();
                    XoaLuoi();
                }
            }
        }
        // Lưới ảo hỏi giá trị từng ô khi vẽ/cuộn: chỉ tra mảng theo chỉ số, không so sánh chuỗi.
        private void KryptonDataGridView1_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            int r = e.RowIndex, c = e.ColumnIndex;
            var chiSo = _filteredIndexes;
            if (c < 0 || (uint)r >= (uint)chiSo.Count) return;
            int idx = chiSo[r];
            try
            {
                if (_isDataMaxMode)
                {
                    if (HienThiTanBinh)
                    {
                        var ds = _dataCacheTanBinh; var lay = _layTanBinh;
                        if ((uint)idx < (uint)ds.Count && (uint)c < (uint)lay.Length) e.Value = lay[c](ds[idx]);
                    }
                    else
                    {
                        var ds = _dataCacheCBCS; var lay = _layCBCS;
                        if ((uint)idx < (uint)ds.Count && (uint)c < (uint)lay.Length) e.Value = lay[c](ds[idx]);
                    }
                }
                else
                {
                    var view = dtDanhSachGoc?.DefaultView;
                    var nguon = _cotNguon;
                    if (view == null || (uint)c >= (uint)nguon.Length || nguon[c] < 0 || (uint)idx >= (uint)view.Count) return;
                    object v = view[idx][nguon[c]];
                    e.Value = v == DBNull.Value ? string.Empty : v;
                }
            }
            catch { e.Value = string.Empty; }
        }
        // =====================================================================
        //  TẢI LẠI TOÀN BỘ
        // =====================================================================
        // Trả về Task để Form 2 có thể 'await' chờ nạp xong
        public async Task ReloadData()
        {
            if (IsDisposed || !IsHandleCreated) return;
            // Chống bấm/gọi liên tục và chống chạy chồng khi đang nạp dở
            if ((DateTime.Now - _lastReload).TotalMilliseconds < 500) { NK("ReloadData: bỏ qua (gọi lại < 500ms)"); return; }
            // Đang bận (lượt đang chạy có thể đã đọc Form6 TRƯỚC khi Form6 đổi): hẹn chạy lại 1 lượt khi lượt hiện tại xong
            if (Interlocked.Exchange(ref _dangXuLyLuongNen, 1) == 1) { NK("ReloadData: đang bận -> hẹn chạy lại"); _canLamMoiLai = true; return; }
            _lastReload = DateTime.Now;
            var sw = Stopwatch.StartNew();
            try
            {
                bool laTanBinh = LaTanBinhHienTai();
                NK($"ReloadData: BẮT ĐẦU tanBinh={laTanBinh} visible={Visible}");
                await Task.Run(CapNhatThongKe); // đồng bộ CSDL ở luồng nền, không đơ giao diện
                NK($"ReloadData: xong CapNhatThongKe {sw.ElapsedMilliseconds}ms");
                if (IsDisposed || !IsHandleCreated) return;
                await DieuPhoiLoadDuLieuAsync(laTanBinh);
                NK($"ReloadData: xong nạp {sw.ElapsedMilliseconds}ms");
                if (IsDisposed || !IsHandleCreated) return;
                await NapLaiBoLocVaLocAsync(datLaiTinhTrang: true);
                NK($"ReloadData: XONG {sw.ElapsedMilliseconds}ms, lưới={kryptonDataGridView1.RowCount} dòng");
            }
            catch (Exception ex)
            {
                NK("ReloadData LỖI: " + ex);
                Debug.WriteLine("Lỗi ReloadData Thống Kê: " + ex.Message);
                MessageBox.Show($"Lỗi nạp dữ liệu thống kê:\n{ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Interlocked.Exchange(ref _dangXuLyLuongNen, 0);
                TiepTucNeuCoYeuCauLamMoi();
            }
        }
        // Có yêu cầu làm mới đến lúc đang bận -> chạy lại đúng 1 lần sau khi rảnh
        private void TiepTucNeuCoYeuCauLamMoi()
        {
            if (!_canLamMoiLai || IsDisposed || !IsHandleCreated) return;
            _canLamMoiLai = false;
            _ = LamMoiDuLieuAsync();
        }
        private async void lamMoi_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            await LamMoiDuLieuAsync();
        }
        // Làm mới (F5): reset bộ lọc -> đồng bộ CSDL -> nạp lại -> lọc 1 lần. Không dùng thanh tiến trình.
        public async Task LamMoiDuLieuAsync()
        {
            if (IsDisposed || !IsHandleCreated) return;
            if (Interlocked.Exchange(ref _dangXuLyLuongNen, 1) == 1)
            {
                NK("LamMoi: đang bận -> hẹn chạy lại");
                _canLamMoiLai = true;
                return;
            }
            var sw = Stopwatch.StartNew();
            try
            {
                NK($"LamMoi: BẮT ĐẦU visible={Visible}");
                UseWaitCursor = true;
                toolStripStatusLabel1.ForeColor = _mauChuTrangThai;
                toolStripStatusLabel1.Text = "Đang làm mới dữ liệu...";
                DatLaiBoLocVeMacDinh();
                bool laTanBinh = await Task.Run(() =>
                {
                    CapNhatThongKe();
                    return LaTanBinhHienTai();
                });
                NK($"LamMoi: xong CapNhatThongKe {sw.ElapsedMilliseconds}ms tanBinh={laTanBinh}");
                if (IsDisposed) return;
                await DieuPhoiLoadDuLieuAsync(laTanBinh);
                NK($"LamMoi: xong nạp {sw.ElapsedMilliseconds}ms");
                if (IsDisposed) return;
                await NapLaiBoLocVaLocAsync(datLaiTinhTrang: false); // lọc xong sẽ tự cập nhật dòng "Tổng cộng"
                NK($"LamMoi: XONG {sw.ElapsedMilliseconds}ms, lưới={kryptonDataGridView1.RowCount} dòng");
            }
            catch (Exception ex)
            {
                NK("LamMoi LỖI: " + ex);
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Lỗi Làm mới (F5)", ex.ToString());
                MessageBox.Show(
                    "Đã xảy ra lỗi hệ thống khi làm mới dữ liệu!\nChi tiết đã được ghi vào Nhật ký.",
                    Module_HeThong.TieuDe_Loi, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!IsDisposed) UseWaitCursor = false;
                Interlocked.Exchange(ref _dangXuLyLuongNen, 0);
                TiepTucNeuCoYeuCauLamMoi();
            }
        }
        // Nạp lại dữ liệu theo đúng chế độ rồi lọc lại bằng bộ lọc hiện tại (giữ lựa chọn của người dùng)
        public async void CapNhatForm15()
        {
            try
            {
                await DieuPhoiLoadDuLieuAsync(LaTanBinhHienTai());
                if (IsDisposed) return;
                LoadComboBoxGhiChu(giuLuaChon: true);
                await LocLaiAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi CapNhatForm15: " + ex.Message);
            }
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            try
            {
                // 1. Chặn khi đang xử lý nặng
                if (_dangXuLyLuongNen == 1 && keyData != Keys.Escape) return true;
                // 2. Phím tắt chung
                if (Module_PhimTat.XuLy(
                    keyData,
                    actionLamMoi: SafeAction(() => lamMoi_ToolStripMenuItem.PerformClick()),      // F5
                    actionXuatExcel: SafeAction(() => xuatDuLieu_ToolStripMenuItem.PerformClick()) // Ctrl + E
                ))
                {
                    return true;
                }
                // 3. Phím tắt đặc thù Form 15
                Keys key = keyData & Keys.KeyCode;
                Keys modifier = keyData & Keys.Modifiers;
                if (modifier == Keys.None)
                {
                    switch (key)
                    {
                        case Keys.F6: return SafeExecute(() => dongBoDuLieu_ToolStripMenuItem.PerformClick());
                        case Keys.F9: return SafeExecute(() => tinhPhanLoaiThang_ToolStripMenuItem.PerformClick());
                    }
                }
                else if (modifier == Keys.Control)
                {
                    switch (key)
                    {
                        case Keys.O: // Ctrl + O: Nhập dữ liệu
                            return SafeExecute(() => nhapDuLieuTuTepExcel_ExcelToolStripMenuItem.PerformClick());
                        case Keys.T: // Ctrl + T: Xem thống kê tập thể
                            return SafeExecute(() => xemThongKeThiDuaTapThe_Click(null, EventArgs.Empty));
                    }
                }
                else if (modifier == (Keys.Control | Keys.Shift))
                {
                    switch (key)
                    {
                        case Keys.M: // Ctrl + Shift + M: Xóa dữ liệu thi đua tháng
                            return SafeExecute(() => xoaDuLieuThiDuaThang_ToolStripMenuItem.PerformClick());
                        case Keys.C: // Ctrl + Shift + C: Xóa dữ liệu CBCS (1 dòng)
                            return SafeExecute(() => xoaDuLieu_ToolStripMenuItem.PerformClick());
                        case Keys.Delete: // Ctrl + Shift + Delete: Xóa toàn bộ
                            return SafeExecute(() => xoaTatCaDuLieu_ToolStripMenuItem.PerformClick());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi phím tắt Form 15: " + ex.Message);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        private void InitToolTips()
        {
            if (_daKhoiTaoToolTip) return;
            if (toolTip1 == null) return;
            try
            {
                toolTip1.IsBalloon = true;
                toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
                toolTip1.ToolTipIcon = ToolTipIcon.Info;
                toolTip1.InitialDelay = 300;
                toolTip1.AutoPopDelay = 2500;
                toolTip1.ReshowDelay = 100;
                toolTip1.ShowAlways = true;
                (Control? control, string noiDung)[] danhSachToolTip = new (Control?, string)[]
                {
                    (textBox_TimKiemTheoTen,          "Gõ tên cần tìm (không phân biệt chữ hoa / thường)"),
                    (comboBox_TimKiemDonVi,           "Chọn đơn vị để tìm kiếm tự động"),
                    (comboBox1_TinhTrang,             "Chọn tình trạng công tác để tìm kiếm tự động"),
                    (comboBox1_GhiChu,                "Lọc theo nội dung Ghi chú (danh sách tự lấy từ dữ liệu, đã bỏ trùng lặp)"),
                    (kryptonButton_LamMoiCacOTimKiem, "Xóa toàn bộ các trường tìm kiếm (F5)"),
                    (kryptonButton_CapNhat,           "Đồng bộ cơ sở dữ liệu (F6)"),
                    (kryptonButton_XuatData,          "Xuất dữ liệu ra tệp (Ctrl + E)")
                };
                foreach (var (control, noiDung) in danhSachToolTip)
                {
                    if (control == null) continue;
                    try
                    {
                        if (control.IsDisposed) continue;
                        toolTip1.SetToolTip(control, noiDung);
                    }
                    catch { }
                }
                _daKhoiTaoToolTip = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi nghiêm trọng tại InitToolTips]: {ex.Message}");
            }
        }
        private void InitPlaceholderTimKiem()
        {
            _dangSetPlaceholder = true;
            textBox_TimKiemTheoTen.Text = PLACEHOLDER_TIMKIEM;
            textBox_TimKiemTheoTen.ForeColor = Color.Gray;
            _dangSetPlaceholder = false;
        }
        private void LoadComboBoxDonVi()
        {
            // Nạp bằng Items (không dùng DataSource) để BỘ LỌC KÉP thu hẹp được danh sách; giữ đơn vị đang chọn nếu còn
            var dsDonVi = Module_DonVi.GetDanhSachDonVi().Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            _dsDonViLoc = dsDonVi;
            _khoaDonViLoc = dsDonVi.Select(ChuanHoaDonViSoSanh).ToArray();
            var cb = comboBox_TimKiemDonVi;
            string dangChon = cb.SelectedIndex > 0 ? cb.Text : "";
            cb.BeginUpdate();
            try
            {
                if (cb.DataSource != null) cb.DataSource = null;
                cb.Items.Clear();
                cb.Items.Add(Module_HeThong.Tat_Ca);
                foreach (var dv in dsDonVi) cb.Items.Add(dv);
                int vt = dangChon.Length > 0 ? dsDonVi.FindIndex(x => string.Equals(x, dangChon, StringComparison.OrdinalIgnoreCase)) : -1;
                cb.SelectedIndex = vt >= 0 ? vt + 1 : 0;
            }
            finally { cb.EndUpdate(); }
        }
        private List<string> _dsDonViLoc = new();
        private string[] _khoaDonViLoc = Array.Empty<string>();
        /// <summary>
        /// Gom BỘ LỌC KÉP trong lúc lọc (an toàn ở luồng nền: chỉ ghi vào tập của chính nó). Dòng đã qua tên + tình trạng:
        /// thỏa ghi chú thì góp đơn vị cho combo Đơn vị, thỏa đơn vị thì góp ghi chú cho combo Ghi chú.
        /// </summary>
        private sealed class KetQuaBoLocKep
        {
            public readonly HashSet<string> DonViKey = new(StringComparer.Ordinal);
            public readonly HashSet<string> GhiChu = new(StringComparer.OrdinalIgnoreCase);
            public bool CoGhiChuTrong;
            /// <summary>Trả về true nếu dòng thỏa cả đơn vị lẫn ghi chú (nằm trong kết quả lọc).</summary>
            public bool Xet(BoLoc bl, string donViKey, string? ghiChu)
            {
                bool okDv = !bl.CoDv || donViKey == bl.DvKey;
                bool okGc = !bl.CoGC || KhopGhiChu(ghiChu, bl.GCTrong, bl.GCKey);
                if (okGc && !string.IsNullOrEmpty(donViKey)) DonViKey.Add(donViKey);
                if (okDv)
                {
                    string n = ChuanHoaGhiChu(ghiChu);
                    if (n.Length == 0) CoGhiChuTrong = true; else GhiChu.Add(n);
                }
                return okDv && okGc;
            }
        }
        /// <summary>
        /// Combo Đơn vị chỉ còn đơn vị có người thỏa tên + tình trạng + ghi chú; combo Ghi chú chỉ còn ghi chú có trong đơn vị
        /// (và tên, tình trạng) đang chọn. Giữ giá trị đang chọn, danh sách không đổi thì không đụng tới combo.
        /// </summary>
        private void CapNhatComboBoLocKep(KetQuaBoLocKep kep)
        {
            if (IsDisposed) return;
            _tamHoanLoc++;
            try
            {
                var cb = comboBox_TimKiemDonVi;
                if (cb != null && !cb.IsDisposed && cb.DataSource == null && _dsDonViLoc.Count == _khoaDonViLoc.Length)
                {
                    var ds = new List<string>(_dsDonViLoc.Count);
                    for (int i = 0; i < _dsDonViLoc.Count; i++)
                        if (kep.DonViKey.Contains(_khoaDonViLoc[i])) ds.Add(_dsDonViLoc[i]);
                    Module_BoLocKhenThuong.NapLaiCombo(cb, ds, Module_HeThong.Tat_Ca);
                }
                var cg = comboBox1_GhiChu;
                if (cg != null && !cg.IsDisposed && cg.Items.Count > 0)
                {
                    var ds = new List<string>(kep.GhiChu.Count + 1);
                    if (kep.CoGhiChuTrong) ds.Add(GHICHU_TRONG);
                    ds.AddRange(Module_BoLocKhenThuong.SapABC(kep.GhiChu));
                    Module_BoLocKhenThuong.NapLaiCombo(cg, ds, Module_HeThong.Tat_Ca);
                }
            }
            catch (Exception ex) { Debug.WriteLine("[Form15.BoLocKep] " + ex.Message); }
            finally { _tamHoanLoc--; }
        }
        private void KryptonDataGridView1_RowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
        {
            var grid = sender as DataGridView;
            if (grid == null) return;
            // Cọ vẽ và căn lề chỉ tạo 1 lần
            if (_rowHeaderBrush == null || _rowHeaderFormat == null)
            {
                _rowHeaderBrush = new SolidBrush(grid.RowHeadersDefaultCellStyle.ForeColor);
                _rowHeaderFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
            }
            Rectangle headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);
            // Khi chỉ vẽ lại vùng ô dữ liệu (cuộn ngang) thì cột số thứ tự không cần vẽ lại
            if (!e.ClipBounds.IntersectsWith(headerBounds)) return;
            // TextRenderer (GDI) nhanh và nét hơn DrawString (GDI+) khi vẽ hàng chục dòng mỗi khung hình
            TextRenderer.DrawText(e.Graphics, (e.RowIndex + 1).ToString(), grid.Font, headerBounds,
                grid.RowHeadersDefaultCellStyle.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
        // =====================================================================
        //  CẬP NHẬT MỘT DÒNG
        // =====================================================================
        // Đọc lại 1 dòng từ CSDL4 rồi vá vào RAM (DataTable hoặc bộ đệm DataMax) và lọc lại lưới.
        // Giữ chữ ký cũ cho các form khác gọi; muốn chờ lọc xong thì dùng RefreshRowFromDbAsync.
        public void RefreshRowFromDb(string id) => _ = RefreshRowFromDbAsync(id);
        public async Task RefreshRowFromDbAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            try
            {
                bool laTanBinh = HienThiTanBinh; // cùng loại với dữ liệu đang nạp trong RAM
                if (!_isDataMaxMode && dtDanhSachGoc == null) return;
                string tenBang = (laTanBinh ? Module_CheDoDoiTuong.BangThiDuaThang : Module_CheDoDoiTuong.ThiDuaThang(CheDoDoiTuong.CBCS));
                // 1. Đọc bản ghi mới nhất: tên cột -> giá trị (các cột AES đã giải mã)
                Dictionary<string, string> giaTri;
                using (var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path)))
                {
                    cn.Open();
                    using var cmd = new SqliteCommand($"SELECT * FROM {tenBang} WHERE ID=@id", cn);
                    cmd.Parameters.AddWithValue("@id", id);
                    using var reader = cmd.ExecuteReader();
                    if (!reader.Read()) return;
                    giaTri = new Dictionary<string, string>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string ten = reader.GetName(i);
                        string v = reader.IsDBNull(i) ? "" : reader.GetValue(i)?.ToString() ?? "";
                        giaTri[ten] = (ten is "HoVaTen" or "SoHieu" or "DonVi") ? SafeDecrypt(v) : v;
                    }
                }
                // 2. Vá vào RAM
                if (_isDataMaxMode) CapNhatBoDemDataMax(id, laTanBinh, giaTri);
                else CapNhatDongCheDoThuong(id, giaTri);
                // 3. Lọc lại (đã vẽ lại toàn lưới nên không cần vẽ riêng từng dòng)
                if (!laTanBinh) AnCotTongLoaiTrongCBCS(kryptonDataGridView1);
                await LocLaiAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi RefreshRowFromDb: " + ex.Message);
            }
        }
        // Chế độ thường: cập nhật (hoặc thêm) dòng trong DataTable theo mọi cột có mặt ở cả bản ghi DB lẫn bảng
        private void CapNhatDongCheDoThuong(string id, Dictionary<string, string> giaTri)
        {
            var dt = dtDanhSachGoc!;
            var cotId = dt.Columns["ID"]!;
            object khoa;
            try { khoa = Convert.ChangeType(id, cotId.DataType, System.Globalization.CultureInfo.InvariantCulture); }
            catch { return; }
            DataRow? dong = dt.Rows.Find(khoa);
            bool moi = dong == null;
            dong ??= dt.NewRow();
            dong.BeginEdit();
            if (moi)
            {
                dong["ID"] = khoa;
                if (dt.Columns.Contains("GhiChu")) dong["GhiChu"] = ""; // ghi chú sẽ được đồng bộ từ CSDL2 sau
            }
            foreach (DataColumn dc in dt.Columns)
            {
                string ten = dc.ColumnName;
                if (ten == "ID" || ten == "GhiChu" || LaCotAo(ten)) continue;
                if (giaTri.TryGetValue(ten, out string? v)) GanGiaTri(dong, dc, v);
            }
            string donVi = giaTri.TryGetValue("DonVi", out string? dv) ? dv : "";
            string hoTen = giaTri.TryGetValue("HoVaTen", out string? ht) ? ht : "";
            if (dt.Columns.Contains(COT_UU_TIEN)) dong[COT_UU_TIEN] = TinhUuTienDonVi(donVi);
            if (dt.Columns.Contains(COT_DV_KEY)) dong[COT_DV_KEY] = ChuanHoaDonViSoSanh(donVi);
            if (dt.Columns.Contains(COT_TEN_SEARCH)) dong[COT_TEN_SEARCH] = BoLocDanhSach.ChuanHoaTimKiem(hoTen);
            dong.EndEdit();
            if (moi) dt.Rows.Add(dong);
            dong.AcceptChanges();
        }
        // Gán chuỗi vào ô theo đúng kiểu của cột (chuỗi rỗng -> NULL với cột không phải chuỗi)
        private static void GanGiaTri(DataRow dong, DataColumn dc, string v)
        {
            if (dc.DataType == typeof(string)) { dong[dc] = v; return; }
            if (v.Length == 0) { dong[dc] = DBNull.Value; return; }
            try { dong[dc] = Convert.ChangeType(v, dc.DataType, System.Globalization.CultureInfo.InvariantCulture); }
            catch { dong[dc] = DBNull.Value; }
        }
        // DataMax: cập nhật (hoặc thêm) đối tượng trong bộ đệm RAM, không đi qua DataTable
        private void CapNhatBoDemDataMax(string id, bool laTanBinh, Dictionary<string, string> giaTri)
        {
            string Lay(string cot) => giaTri.TryGetValue(cot, out string? v) ? v : "";
            if (!laTanBinh)
            {
                var ds = _dataCacheCBCS;
                int vt = ds.FindIndex(x => x.ID == id);
                var o = vt >= 0 ? ds[vt] : new RawCBCS { ID = id, GhiChu = "" };
                o.IdSo = int.TryParse(id, out int idSo) ? idSo : int.MaxValue;
                o.HoTenE = Lay("HoVaTen"); o.SoHieuE = Lay("SoHieu"); o.DonViE = Lay("DonVi");
                o.DonViKey = ChuanHoaDonViSoSanh(o.DonViE);
                o.TinhTrang = Lay("TinhTrang");
                o.HoTenSearch = BoLocDanhSach.ChuanHoaTimKiem(o.HoTenE);
                o.SortPriority = TinhUuTienDonVi(o.DonViE);
                o.KQ_TD = Lay("KQ_ThiDua_Nam_Cu"); o.KQ_XL_CB = Lay("KQ_XepLoaiCB_Nam_Cu"); o.KQ_XL_DV = Lay("KQ_XepLoaiDangVien_Nam_Cu");
                o.T12 = Lay("Thang_12_Nam_Cu");
                for (int i = 1; i <= 11; i++) o.Thang[i - 1] = Lay($"Thang_{i}");
                o.SauThang = Lay("Sau_Thang_Dau_Nam"); o.TongKet = Lay("TongKet_Nam");
                o.L1 = Lay("TS_Loai1"); o.L2 = Lay("TS_Loai2"); o.L3 = Lay("TS_Loai3"); o.L4 = Lay("TS_Loai4");
                foreach (var c in _cotPhatSinhCacheThongKe) o.CotPhatSinh[c] = Lay(c);
                if (vt < 0) ds.Add(o);
            }
            else
            {
                var ds = _dataCacheTanBinh;
                int vt = ds.FindIndex(x => x.ID == id);
                var o = vt >= 0 ? ds[vt] : new RawTanBinh { ID = id, GhiChu = "" };
                o.IdSo = int.TryParse(id, out int idSo) ? idSo : int.MaxValue;
                o.HoTenE = Lay("HoVaTen"); o.SoHieuE = Lay("SoHieu"); o.DonViE = Lay("DonVi");
                o.DonViKey = ChuanHoaDonViSoSanh(o.DonViE);
                o.TinhTrang = Lay("TinhTrang");
                o.HoTenSearch = BoLocDanhSach.ChuanHoaTimKiem(o.HoTenE);
                o.SortPriority = TinhUuTienDonVi(o.DonViE);
                o.L1 = Lay("TS_Loai1"); o.L2 = Lay("TS_Loai2"); o.L3 = Lay("TS_Loai3"); o.L4 = Lay("TS_Loai4");
                foreach (string col in Module_LichXet.CotVatLy(Module_CheDoDoiTuong.Hien))
                    if (giaTri.ContainsKey(col)) o.DuLieuThang[col] = Lay(col);
                foreach (var c in _cotPhatSinhCacheThongKe) o.CotPhatSinh[c] = Lay(c);
                if (vt < 0) ds.Add(o);
            }
        }
        private static string SafeDecrypt(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            try
            {
                string result = Module_BaoMatAES.GiaiMa(input);
                // Fallback: AES trả về rỗng (định dạng cũ) thì dùng chuỗi gốc
                return string.IsNullOrEmpty(result) ? input : result;
            }
            catch { return input; }
        }
        // =====================================================================
        //  ĐỒNG BỘ DANH SÁCH GỐC (CSDL2) -> BẢNG THỐNG Kê (CSDL4)
        // =====================================================================
        // Thêm người mới, đổi tình trạng, chép lại họ tên/đơn vị khi lệch, rồi tính lại Tổng loại 1..4.
        // Toàn bộ ghi trong MỘT giao dịch.
        public void CapNhatThongKe()
        {
            try
            {
                bool laTanBinh = LaTanBinhHienTai();
                string tableDich = (laTanBinh ? Module_CheDoDoiTuong.BangThiDuaThang : Module_CheDoDoiTuong.ThiDuaThang(CheDoDoiTuong.CBCS));
                string ttDang = Module_HeThong.TT_DANG_CONG_TAC, ttChuyen = Module_HeThong.TT_CHUYEN_CONG_TAC;
                // BƯỚC 1: CSDL2 (gốc) theo thứ tự ID. KHÔNG gộp người trùng Số hiệu: gốc có 6.000 dòng thì thống kê phải có 6.000 dòng
                var goc = new List<Module_DongBoThiDua.Nguoi>();
                var gocMaHoa = new List<(string encHt, string encSh, string encDv)>();
                using (var cn2 = Module_DanduongGPS.MoKetNoiDoc(_csdl2Path))
                {
                    using var cmd = new SqliteCommand($"SELECT HoVaTen, SoHieu, DonVi FROM {Module_TaiKhoan.BangDanhSach} ORDER BY ID", cn2);
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        string encHt = rd["HoVaTen"]?.ToString() ?? "", encSh = rd["SoHieu"]?.ToString() ?? "", encDv = rd["DonVi"]?.ToString() ?? "";
                        string plainHt = Module_DongBoThiDua.GiaiMaAnToan(encHt), plainSh = Module_DongBoThiDua.GiaiMaAnToan(encSh);
                        if (Module_DongBoThiDua.BoQua(plainHt, plainSh)) continue;
                        goc.Add(new Module_DongBoThiDua.Nguoi(plainHt, plainSh, Module_DongBoThiDua.GiaiMaAnToan(encDv)));
                        gocMaHoa.Add((encHt, encSh, encDv));
                    }
                }
                // BƯỚC 2: CSDL4 (đích) theo thứ tự ID
                var dich = new List<Module_DongBoThiDua.Nguoi>();
                var dichInfo = new List<(int id, string tt)>();
                using (var cn4 = Module_DanduongGPS.MoKetNoiDoc(_csdl4Path))
                {
                    using var cmd = new SqliteCommand($"SELECT ID, HoVaTen, SoHieu, DonVi, TinhTrang FROM {tableDich} ORDER BY ID", cn4);
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        int id = Convert.ToInt32(rd["ID"]);
                        string tt = rd["TinhTrang"]?.ToString()?.Trim() ?? "";
                        dich.Add(new Module_DongBoThiDua.Nguoi(
                            Module_DongBoThiDua.GiaiMaAnToan(rd["HoVaTen"]?.ToString()),
                            Module_DongBoThiDua.GiaiMaAnToan(rd["SoHieu"]?.ToString()),
                            Module_DongBoThiDua.GiaiMaAnToan(rd["DonVi"]?.ToString())));
                        dichInfo.Add((id, tt));
                    }
                }
                // BƯỚC 3: ghép MỘT-MỘT (cùng quy tắc với Form17) rồi chia việc
                int[] ghep = Module_DongBoThiDua.GhepMotMot(goc, dich, out bool[] daGhep);
                var idChuyenCongTac = new List<int>();
                var idDangCongTac = new List<int>();
                var idCanDongBoThongTin = new List<(int id, string encHt, string encDv)>();
                var canThem = new List<int>(); // chỉ số trong danh sách gốc
                for (int i = 0; i < goc.Count; i++)
                {
                    int j = ghep[i];
                    if (j < 0) { canThem.Add(i); continue; }
                    int id = dichInfo[j].id;
                    if (dichInfo[j].tt != ttDang) idDangCongTac.Add(id); // quay lại danh sách gốc
                    if (!string.Equals(dich[j].Ht, goc[i].Ht, StringComparison.Ordinal) || !string.Equals(dich[j].Dv, goc[i].Dv, StringComparison.Ordinal))
                        idCanDongBoThongTin.Add((id, gocMaHoa[i].encHt, gocMaHoa[i].encDv));
                }
                for (int j = 0; j < dich.Count; j++)
                    if (!daGhep[j] && dichInfo[j].tt != ttChuyen) idChuyenCongTac.Add(dichInfo[j].id); // không còn trong danh sách gốc
                // BƯỚC 4: ghi vào CSDL4 trong một giao dịch
                using var cn = Module_DanduongGPS.MoKetNoiDoc(_csdl4Path);
                using var tran = cn.BeginTransaction();
                try
                {
                    if (canThem.Count > 0)
                    {
                        using var cmdIns = new SqliteCommand($@"INSERT INTO {tableDich} (HoVaTen, SoHieu, DonVi, TinhTrang, TS_Loai1, TS_Loai2, TS_Loai3, TS_Loai4)
                                                              VALUES (@ht, @sh, @dv, @tt, '0', '0', '0', '0')", cn, tran);
                        var pHt = cmdIns.Parameters.Add("@ht", SqliteType.Text);
                        var pSh = cmdIns.Parameters.Add("@sh", SqliteType.Text);
                        var pDv = cmdIns.Parameters.Add("@dv", SqliteType.Text);
                        cmdIns.Parameters.Add("@tt", SqliteType.Text).Value = ttDang;
                        foreach (int i in canThem)
                        {
                            pHt.Value = gocMaHoa[i].encHt;
                            pSh.Value = gocMaHoa[i].encSh;
                            pDv.Value = gocMaHoa[i].encDv;
                            cmdIns.ExecuteNonQuery();
                        }
                    }
                    void CapNhatTinhTrang(List<int> ids, string tinhTrang)
                    {
                        if (ids.Count == 0) return;
                        using var cmdUpd = new SqliteCommand($"UPDATE {tableDich} SET TinhTrang = @tt WHERE ID = @id", cn, tran);
                        cmdUpd.Parameters.Add("@tt", SqliteType.Text).Value = tinhTrang;
                        var pId = cmdUpd.Parameters.Add("@id", SqliteType.Integer);
                        foreach (int id in ids) { pId.Value = id; cmdUpd.ExecuteNonQuery(); }
                    }
                    CapNhatTinhTrang(idChuyenCongTac, ttChuyen);
                    CapNhatTinhTrang(idDangCongTac, ttDang);
                    if (idCanDongBoThongTin.Count > 0)
                    {
                        using var cmdInfo = new SqliteCommand($"UPDATE {tableDich} SET HoVaTen = @ht, DonVi = @dv WHERE ID = @id", cn, tran);
                        var pHt = cmdInfo.Parameters.Add("@ht", SqliteType.Text);
                        var pDv = cmdInfo.Parameters.Add("@dv", SqliteType.Text);
                        var pId = cmdInfo.Parameters.Add("@id", SqliteType.Integer);
                        foreach (var x in idCanDongBoThongTin) { pHt.Value = x.encHt; pDv.Value = x.encDv; pId.Value = x.id; cmdInfo.ExecuteNonQuery(); }
                    }
                    // BƯỚC 5: tính lại Tổng loại bằng MỘT lượt đọc, chỉ ghi những dòng có thay đổi
                    string[] cotThang = laTanBinh ? CotThangTanBinh : CotThangCBCS;
                    var canGhi = new List<(int id, string l1, string l2, string l3, string l4)>();
                    using (var cmdSel = new SqliteCommand($"SELECT ID, TS_Loai1, TS_Loai2, TS_Loai3, TS_Loai4, {string.Join(",", cotThang)} FROM {tableDich}", cn, tran))
                    using (var rd = cmdSel.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            int c1 = 0, c2 = 0, c3 = 0, c4 = 0;
                            for (int i = 5; i < rd.FieldCount; i++)
                            {
                                if (rd.IsDBNull(i)) continue;
                                foreach (char ch in rd.GetValue(i)?.ToString() ?? "")
                                {
                                    if (ch == '1') c1++; else if (ch == '2') c2++; else if (ch == '3') c3++; else if (ch == '4') c4++;
                                }
                            }
                            string n1 = c1 == 0 ? "" : c1.ToString(), n2 = c2 == 0 ? "" : c2.ToString(), n3 = c3 == 0 ? "" : c3.ToString(), n4 = c4 == 0 ? "" : c4.ToString();
                            string Cu(int i) => rd.IsDBNull(i) ? "" : (rd.GetValue(i)?.ToString() ?? "").Trim();
                            if (Cu(1) != n1 || Cu(2) != n2 || Cu(3) != n3 || Cu(4) != n4)
                                canGhi.Add((Convert.ToInt32(rd.GetValue(0)), n1, n2, n3, n4));
                        }
                    }
                    if (canGhi.Count > 0)
                    {
                        using var cmdTong = new SqliteCommand($"UPDATE {tableDich} SET TS_Loai1=@L1, TS_Loai2=@L2, TS_Loai3=@L3, TS_Loai4=@L4 WHERE ID=@ID", cn, tran);
                        var p1 = cmdTong.Parameters.Add("@L1", SqliteType.Text);
                        var p2 = cmdTong.Parameters.Add("@L2", SqliteType.Text);
                        var p3 = cmdTong.Parameters.Add("@L3", SqliteType.Text);
                        var p4 = cmdTong.Parameters.Add("@L4", SqliteType.Text);
                        var pId = cmdTong.Parameters.Add("@ID", SqliteType.Integer);
                        foreach (var x in canGhi)
                        {
                            p1.Value = x.l1; p2.Value = x.l2; p3.Value = x.l3; p4.Value = x.l4; pId.Value = x.id;
                            cmdTong.ExecuteNonQuery();
                        }
                    }
                    tran.Commit();
                }
                catch { tran.Rollback(); throw; }
                NK($"CapNhatThongKe: {tableDich} gốc={goc.Count} đích={dich.Count} thêm={canThem.Count} chuyểnCT={idChuyenCongTac.Count}");
            }
            catch (Exception ex)
            {
                NK("CapNhatThongKe LỖI: " + ex);
                Debug.WriteLine("Lỗi đồng bộ: " + ex.Message);
            }
        }
        // Tính lại Tổng loại 1..4 cho MỘT người (dùng sau khi sửa dữ liệu tháng)
        public void CapNhatTongLoai_Cho(int id, SqliteConnection cn, SqliteTransaction? tran = null)
        {
            bool laTanBinh = LaTanBinhHienTai();
            string table = (laTanBinh ? Module_CheDoDoiTuong.BangThiDuaThang : Module_CheDoDoiTuong.ThiDuaThang(CheDoDoiTuong.CBCS));
            string[] cotThang = laTanBinh ? CotThangTanBinh : CotThangCBCS;
            int c1 = 0, c2 = 0, c3 = 0, c4 = 0;
            using (var cmd = new SqliteCommand($"SELECT {string.Join(",", cotThang)} FROM {table} WHERE ID=@ID", cn, tran))
            {
                cmd.Parameters.AddWithValue("@ID", id);
                using var rd = cmd.ExecuteReader();
                if (!rd.Read()) return;
                for (int i = 0; i < rd.FieldCount; i++)
                {
                    if (rd.IsDBNull(i)) continue;
                    foreach (char ch in rd.GetValue(i)?.ToString() ?? "")
                    {
                        if (ch == '1') c1++; else if (ch == '2') c2++; else if (ch == '3') c3++; else if (ch == '4') c4++;
                    }
                }
            }
            static string Chuoi(int c) => c == 0 ? "" : c.ToString();
            using var upd = new SqliteCommand($"UPDATE {table} SET TS_Loai1=@L1, TS_Loai2=@L2, TS_Loai3=@L3, TS_Loai4=@L4 WHERE ID=@ID", cn, tran);
            upd.Parameters.AddWithValue("@L1", Chuoi(c1));
            upd.Parameters.AddWithValue("@L2", Chuoi(c2));
            upd.Parameters.AddWithValue("@L3", Chuoi(c3));
            upd.Parameters.AddWithValue("@L4", Chuoi(c4));
            upd.Parameters.AddWithValue("@ID", id);
            upd.ExecuteNonQuery();
        }
        // =====================================================================
        //  ĐỊNH DẠNG LƯỚI
        // =====================================================================
        public void ApDungDinhDangGridThongKe()
        {
            if (kryptonDataGridView1 == null || kryptonDataGridView1.Columns.Count == 0)
                return;
            var grid = kryptonDataGridView1;
            grid.SuspendLayout();
            try
            {
                bool laTanBinh = HienThiTanBinh; // theo đúng loại dữ liệu đang nạp
                int namHeThong = Module_HeThong.LayNamHeThong();
                int namCu = namHeThong - 1;
                var headerMap = new Dictionary<string, string>
                {
                    ["HoVaTen"] = Module_HeThong.TieuDeCot.HoVaTen,
                    ["SoHieu"] = Module_HeThong.TieuDeCot.SoHieu,
                    ["DonVi"] = Module_HeThong.TieuDeCot.DonVi,
                    ["TinhTrang"] = "Tình trạng",
                    ["GhiChu"] = Module_HeThong.TieuDeCot.GhiChu
                };
                if (laTanBinh)
                {
                    foreach (string cot in Module_LichXet.CotVatLy(Module_CheDoDoiTuong.Hien))
                        headerMap[cot] = Module_LichXet.NhanCot(Module_CheDoDoiTuong.Hien, cot) ?? cot;
                }
                else
                {
                    headerMap["KQ_ThiDua_Nam_Cu"] = $"KQ thi đua\nnăm {namCu}";
                    headerMap["KQ_XepLoaiCB_Nam_Cu"] = $"Xếp loại CBCS\nnăm {namCu}";
                    headerMap["KQ_XepLoaiDangVien_Nam_Cu"] = $"XL đảng viên\nnăm {namCu}";
                    headerMap["Thang_12_Nam_Cu"] = $"Tháng 12\n{namCu}";
                    for (int i = 1; i <= 11; i++) headerMap[$"Thang_{i}"] = $"Tháng {i}";
                    headerMap["Sau_Thang_Dau_Nam"] = "6 tháng\nđầu năm";
                    headerMap["TongKet_Nam"] = "Tổng kết\nnăm";
                }
                headerMap["TS_Loai1"] = $"Tổng\n{Module_HeThong.Loai_1}";
                headerMap["TS_Loai2"] = $"Tổng\n{Module_HeThong.Loai_2}";
                headerMap["TS_Loai3"] = $"Tổng\n{Module_HeThong.Loai_3}";
                headerMap["TS_Loai4"] = $"Tổng\n{Module_HeThong.Loai_4}";
                foreach (var kv in headerMap)
                    if (grid.Columns.Contains(kv.Key)) grid.Columns[kv.Key].HeaderText = kv.Value;
                // ẨN CÁC CỘT THỪA
                if (grid.Columns.Contains("ID")) grid.Columns["ID"].Visible = false;
                if (laTanBinh)
                {
                    // cột thời gian có trong lược đồ nhưng ngoài lịch đang dùng (Tân binh: Thang_2, Tuan_x_T6; Học viên: đợt chưa dùng)
                    foreach (DataGridViewColumn c in grid.Columns)
                        if (Module_LichXet.LaCotKhongDung(Module_CheDoDoiTuong.Hien, c.Name)) c.Visible = false;
                }
                // GIAO DIỆN HIỆN ĐẠI
                grid.BackgroundColor = Color.White;
                grid.BorderStyle = BorderStyle.None;
                grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
                grid.GridColor = Color.FromArgb(235, 235, 235);
                grid.EnableHeadersVisualStyles = false;
                // 1. HEADER
                grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
                grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                grid.ColumnHeadersHeight = 85;
                grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = _fontGridHeader,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    BackColor = Color.FromArgb(240, 244, 248),
                    ForeColor = Color.FromArgb(40, 40, 40),
                    SelectionBackColor = Color.FromArgb(240, 244, 248),
                    WrapMode = DataGridViewTriState.True,
                    Padding = new Padding(3)
                };
                // 2. ROW
                grid.RowTemplate.Height = 32;
                grid.DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = _fontGridCell,
                    ForeColor = Color.FromArgb(45, 45, 45),
                    SelectionBackColor = Color.FromArgb(232, 244, 253),
                    SelectionForeColor = Color.FromArgb(0, 102, 204),
                    Padding = new Padding(2, 0, 2, 0)
                };
                grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 252, 252);
                grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                grid.MultiSelect = true; // cho phép chọn nhiều dòng (Ctrl/Shift + chuột, Ctrl+A) để xóa hàng loạt
                // 3. ĐỘ RỘNG VÀ CĂN LỀ
                if (grid.Columns.Contains("HoVaTen"))
                {
                    var col = grid.Columns["HoVaTen"];
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    col.MinimumWidth = 220;
                    col.FillWeight = 40;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    col.DefaultCellStyle.Padding = new Padding(5, 0, 0, 0);
                }
                if (grid.Columns.Contains("TinhTrang"))
                {
                    var col = grid.Columns["TinhTrang"];
                    col.Width = 140;
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                if (grid.Columns.Contains("GhiChu"))
                {
                    var col = grid.Columns["GhiChu"];
                    col.Width = 220;
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    col.DefaultCellStyle.WrapMode = DataGridViewTriState.False; // vẽ nhanh hơn; nội dung đầy đủ xem bằng tooltip
                    col.DefaultCellStyle.ForeColor = Color.DarkSlateGray;
                    col.DefaultCellStyle.Padding = new Padding(5, 0, 0, 0);
                }
                foreach (DataGridViewColumn col in grid.Columns)
                {
                    col.SortMode = DataGridViewColumnSortMode.NotSortable;
                    if (col.Name == "HoVaTen" || col.Name == "ID" || col.Name == "TinhTrang" || col.Name == "GhiChu") continue;
                    col.Width = 90;
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                // 4. TÔ MÀU NHẤN MẠNH
                if (laTanBinh)
                {
                    // đợt lẻ (1, 3, 5) tô nền xanh nhạt để phân biệt từng tháng
                    string[] nhomToMauXanhDuong = Module_LichXet.LichXet(Module_CheDoDoiTuong.Hien)
                        .Where(d => d.So % 2 == 1).SelectMany(d => d.TatCaCot).ToArray();
                    Color mauXanhDuongNhat = Color.FromArgb(235, 245, 255);
                    foreach (var name in nhomToMauXanhDuong)
                        if (grid.Columns.Contains(name)) grid.Columns[name].DefaultCellStyle.BackColor = mauXanhDuongNhat;
                    string[] cacCotKetQua = Module_LichXet.CotKetQua(Module_CheDoDoiTuong.Hien);
                    foreach (var name in cacCotKetQua)
                    {
                        if (grid.Columns.Contains(name))
                        {
                            grid.Columns[name].DefaultCellStyle.Font = _fontGridHighlight;
                            grid.Columns[name].DefaultCellStyle.ForeColor = Color.FromArgb(0, 80, 160);
                        }
                    }
                }
                string[] nhomToMauXanhLa = { "TS_Loai1", "TS_Loai2", "TS_Loai3", "TS_Loai4" };
                Color mauXanhLaNhat = Color.FromArgb(240, 252, 240);
                foreach (var name in nhomToMauXanhLa)
                {
                    if (grid.Columns.Contains(name))
                    {
                        grid.Columns[name].DefaultCellStyle.BackColor = mauXanhLaNhat;
                        grid.Columns[name].DefaultCellStyle.Font = _fontGridHighlight;
                    }
                }
                // 5. VỊ TRÍ CỘT: cột đã được thêm đúng thứ tự (ThemCotVaoLuoi) nên thường không phải đổi gì.
                // Chỉ chỉnh khi lệch (ví dụ hàm được gọi công khai trên lưới dựng bằng cách khác).
                int viTri = 0;
                void XepCot(string colName)
                {
                    if (!grid.Columns.Contains(colName)) return;
                    var c = grid.Columns[colName];
                    if (c.DisplayIndex != viTri) c.DisplayIndex = viTri;
                    viTri++;
                }
                XepCot("HoVaTen");
                XepCot("SoHieu");
                XepCot("DonVi");
                XepCot("TinhTrang");
                if (laTanBinh)
                {
                    foreach (string cot in Module_LichXet.CotTheoThuTu(Module_CheDoDoiTuong.Hien)) XepCot(cot);
                }
                else
                {
                    XepCot("KQ_ThiDua_Nam_Cu");
                    XepCot("KQ_XepLoaiCB_Nam_Cu");
                    XepCot("KQ_XepLoaiDangVien_Nam_Cu");
                    XepCot("Thang_12_Nam_Cu");
                    for (int i = 1; i <= 5; i++) XepCot($"Thang_{i}");
                    XepCot("Sau_Thang_Dau_Nam");              // 6 tháng đầu năm nằm trước Tháng 6
                    for (int i = 6; i <= 11; i++) XepCot($"Thang_{i}");
                    XepCot("TongKet_Nam");
                }
                XepCot("TS_Loai1");
                XepCot("TS_Loai2");
                XepCot("TS_Loai3");
                XepCot("TS_Loai4");
                if (_cotPhatSinhCacheThongKe != null)
                {
                    foreach (var cp in _cotPhatSinhCacheThongKe)
                        if (cp != "GhiChu") XepCot(cp);
                }
                XepCot("GhiChu"); // chốt vị trí cuối
                CapDinhDangLuoiCBCS(grid, laTanBinh);
            }
            catch (Exception ex) { Debug.WriteLine("Lỗi định dạng: " + ex.Message); }
            finally { grid.ResumeLayout(); }
        }
        // =====================================================================
        //  NÚT ĐỒNG BỘ (F6)
        // =====================================================================
        private async void kryptonButton_CapNhat_Click(object? sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref _dangXuLyLuongNen, 1) == 1) { NK("DongBo(F6): bỏ qua (đang bận)"); return; }
            NK("DongBo(F6): BẮT ĐẦU");
            string textBanDau = kryptonButton_CapNhat.Values.Text;
            Image? anhBanDau = kryptonButton_CapNhat.Values.Image;
            Form_Loading frmLoad = new Form_Loading("Đang tải dữ liệu từ CSDL, vui lòng đợi...");
            try
            {
                kryptonButton_CapNhat.Enabled = false;
                kryptonButton_CapNhat.Values.Text = "Đang tải...";
                kryptonButton_CapNhat.Values.Image = null;
                this.Enabled = false;
                frmLoad.Show(this);
                // Đồng bộ ngầm SQLite
                bool laTanBinh = await Task.Run(() =>
                {
                    CapNhatThongKe();
                    return LaTanBinhHienTai();
                });
                // Nạp lại đúng chế độ (thường / DataMax), cập nhật combobox lọc rồi lọc 1 lần
                await DieuPhoiLoadDuLieuAsync(laTanBinh);
                await NapLaiBoLocVaLocAsync(datLaiTinhTrang: false);
                try
                {
                    //string noiDungTB = "Đồng bộ dữ liệu csdl2 sang csdl4 thành công!";
                    string noiDungTB = "Đồng bộ csdl2 → csdl4 xong!";
                    Module_ThongBao.ThanhCong(noiDungTB);
                    Module_NhatKy.GhiNhatKy(
                        Module_TaiKhoan.TenTaiKhoan_RAM,
                        noiDungTB,
                        DateTime.Now.ToString(Module_HeThong.DinhDang_NgayGio));
                }
                catch { }
            }
            catch (Exception ex)
            {
                NK("DongBo(F6) LỖI: " + ex);
                try { Module_ThongBao.Loi("Lỗi cập nhật: " + ex.Message); } catch { }
            }
            finally
            {
                NK($"DongBo(F6): KẾT THÚC, lưới={kryptonDataGridView1.RowCount} dòng");
                frmLoad.Close();
                this.Enabled = true;
                kryptonDataGridView1.Invalidate(); // lưới ảo vẽ lại dữ liệu đang có trên RAM
                this.Focus();
                kryptonButton_CapNhat.Values.Text = textBanDau;
                kryptonButton_CapNhat.Values.Image = anhBanDau;
                kryptonButton_CapNhat.Enabled = true;
                Interlocked.Exchange(ref _dangXuLyLuongNen, 0);
                TiepTucNeuCoYeuCauLamMoi();
            }
        }
        private void comboBox1_TinhTrang_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_tamHoanLoc > 0) return;
            ApplyFilter();
        }
        private void textBox_TimKiemTheoTen_TextChanged(object? sender, EventArgs e)
        {
            if (_dangSetPlaceholder) return;   // đang tự set chữ gợi ý
            if (_timKiemTimer == null) return;
            _timKiemTimer.Stop();              // gõ tiếp thì đếm lại từ đầu
            _timKiemTimer.Start();
        }
        private void comboBox_TimKiemDonVi_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_tamHoanLoc > 0) return;
            ApplyFilter();
        }
        // Chuột phải: chọn đúng dòng/ô vừa bấm để các mục menu thao tác đúng đối tượng.
        private void KryptonDataGridView1_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            var dgv = (DataGridView)sender!;
            var hit = dgv.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0 || hit.RowIndex >= dgv.RowCount) return;
            int cot = hit.ColumnIndex >= 0 ? hit.ColumnIndex : dgv.Columns.GetFirstColumn(DataGridViewElementStates.Visible)?.Index ?? -1;
            if (cot < 0) return;
            // Bấm chuột phải vào dòng đang nằm trong vùng chọn nhiều -> giữ nguyên vùng chọn
            if ((dgv.Rows.GetRowState(hit.RowIndex) & DataGridViewElementStates.Selected) != 0
                && dgv.Rows.GetRowCount(DataGridViewElementStates.Selected) > 1) return;
            dgv.ClearSelection();
            dgv.CurrentCell = dgv.Rows[hit.RowIndex].Cells[cot];
            dgv.Rows[hit.RowIndex].Selected = true;
        }
        // =====================================================================
        //  ĐỒNG BỘ GHI CHÚ TỪ FORM6 (CSDL2 - DanhSach) -> FORM15, KHÔNG CẦN LOAD LẠI TOÀN BỘ
        //  Chỉ đọc lại GhiChu + STT rồi vá thẳng vào RAM và vẽ lại lưới.
        // =====================================================================
        private int _dangDongBoGhiChu = 0;
        private volatile bool _canDongBoGhiChuLai = false;
        public async Task CapNhatGhiChuTuCSDL2Async()
        {
            if (IsDisposed || !IsHandleCreated) return;
            // Có thể được gọi từ luồng nền -> chuyển về luồng giao diện
            if (InvokeRequired)
            {
                BeginInvoke(new Action(async () => await CapNhatGhiChuTuCSDL2Async()));
                return;
            }
            // Chưa nạp dữ liệu thống kê -> không cần làm gì (lúc nạp sẽ tự đọc mới nhất)
            if (dtDanhSachGoc == null) return;
            // Đang đồng bộ dở thì chỉ đánh dấu chạy lại 1 lần nữa, không chạy chồng
            if (Interlocked.Exchange(ref _dangDongBoGhiChu, 1) == 1)
            {
                _canDongBoGhiChuLai = true;
                return;
            }
            try
            {
                do
                {
                    _canDongBoGhiChuLai = false;
                    var dl = await Task.Run(DocDanhSachCSDL2);
                    if (IsDisposed || !IsHandleCreated) return;
                    // Đọc lỗi (DB đang bận...) -> giữ nguyên dữ liệu cũ, tuyệt đối không xóa trắng GhiChu
                    if (dl == null) continue;
                    bool sttDoi = !BanDoSTTGiongNhau(_sttCache, dl.STT);
                    _sttCache = dl.STT;
                    bool ghiChuDoi = ApDungGhiChuMoiVaoRam(dl.GhiChu);
                    if (ghiChuDoi)
                    {
                        bool locGhiChuDangBat = comboBox1_GhiChu != null && comboBox1_GhiChu.SelectedIndex > 0;
                        LoadComboBoxGhiChu(giuLuaChon: true); // danh sách lọc cập nhật theo ghi chú mới, giữ nguyên lựa chọn nếu còn
                        if (locGhiChuDangBat || sttDoi) ApplyFilter();
                        else kryptonDataGridView1.Invalidate();
                    }
                    else if (sttDoi)
                    {
                        ApplyFilter(); // STT đổi -> thứ tự hiển thị đổi
                    }
                }
                while (_canDongBoGhiChuLai);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi CapNhatGhiChuTuCSDL2Async: " + ex.Message);
            }
            finally
            {
                Interlocked.Exchange(ref _dangDongBoGhiChu, 0);
            }
        }
        private static bool BanDoSTTGiongNhau(Dictionary<string, int>? a, Dictionary<string, int>? b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null || a.Count != b.Count) return false;
            foreach (var kv in a)
                if (!b.TryGetValue(kv.Key, out int v) || v != kv.Value) return false;
            return true;
        }
        // Vá GhiChu mới vào RAM theo khóa SoHieu. Trả về true nếu có ít nhất 1 dòng thay đổi.
        private bool ApDungGhiChuMoiVaoRam(Dictionary<string, string> banDo)
        {
            bool coThayDoi = false;
            string Lay(string? soHieu) => !string.IsNullOrEmpty(soHieu) && banDo.TryGetValue(soHieu, out var gc) ? gc : "";
            if (_isDataMaxMode)
            {
                foreach (var it in _dataCacheCBCS)
                {
                    string moi = Lay(it.SoHieuE);
                    if (!string.Equals(it.GhiChu ?? "", moi, StringComparison.Ordinal)) { it.GhiChu = moi; coThayDoi = true; }
                }
                foreach (var it in _dataCacheTanBinh)
                {
                    string moi = Lay(it.SoHieuE);
                    if (!string.Equals(it.GhiChu ?? "", moi, StringComparison.Ordinal)) { it.GhiChu = moi; coThayDoi = true; }
                }
            }
            else if (dtDanhSachGoc != null && dtDanhSachGoc.Columns.Contains("GhiChu") && dtDanhSachGoc.Columns.Contains("SoHieu"))
            {
                foreach (DataRow row in dtDanhSachGoc.Rows)
                {
                    if (row.RowState == DataRowState.Deleted) continue;
                    string moi = Lay(row["SoHieu"]?.ToString());
                    string cu = row["GhiChu"] == DBNull.Value ? "" : row["GhiChu"]?.ToString() ?? "";
                    if (!string.Equals(cu, moi, StringComparison.Ordinal)) { row["GhiChu"] = moi; coThayDoi = true; }
                }
                if (coThayDoi) dtDanhSachGoc.AcceptChanges();
            }
            return coThayDoi;
        }
        // =====================================================================
        //  BỘ LỌC GHI CHÚ (comboBox1_GhiChu)
        // =====================================================================
        private void comboBox1_GhiChu_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_tamHoanLoc > 0) return;
            ApplyFilter();
        }
        // Chuẩn hóa Ghi chú CHỈ để so sánh/gom nhóm: bỏ khoảng trắng đầu-cuối, gộp xuống dòng/tab/nhiều dấu cách thành 1 dấu cách.
        private static string ChuanHoaGhiChu(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            bool sach = !char.IsWhiteSpace(input[0]) && !char.IsWhiteSpace(input[^1]);
            if (sach)
            {
                for (int i = 0; i < input.Length; i++)
                {
                    char c = input[i];
                    if (char.IsWhiteSpace(c) && (c != ' ' || char.IsWhiteSpace(input[i + 1]))) { sach = false; break; }
                }
            }
            if (sach) return input;
            var sb = new System.Text.StringBuilder(input.Length);
            bool daGapCach = false;
            foreach (char c in input)
            {
                if (char.IsWhiteSpace(c)) { daGapCach = true; continue; }
                if (daGapCach && sb.Length > 0) sb.Append(' ');
                daGapCach = false;
                sb.Append(c);
            }
            return sb.ToString();
        }
        // Trạng thái bộ lọc Ghi chú hiện tại: (có lọc?, lọc "không có ghi chú"?, khóa so khớp)
        private (bool co, bool trong, string key) LayBoLocGhiChu()
        {
            if (comboBox1_GhiChu == null || comboBox1_GhiChu.SelectedIndex <= 0) return (false, false, "");
            string txt = comboBox1_GhiChu.Text?.Trim() ?? "";
            if (txt.Length == 0 || txt == Module_HeThong.Tat_Ca) return (false, false, "");
            if (txt == GHICHU_TRONG) return (true, true, "");
            return (true, false, ChuanHoaGhiChu(txt));
        }
        private static bool KhopGhiChu(string? ghiChu, bool locTrong, string key)
        {
            string n = ChuanHoaGhiChu(ghiChu);
            return locTrong ? n.Length == 0 : string.Equals(n, key, StringComparison.OrdinalIgnoreCase);
        }
        // Liệt kê các Ghi chú khác nhau (bỏ trùng lặp) từ dữ liệu đang có trong RAM.
        // giuLuaChon = true: cố giữ lại mục đang chọn. Trả về true nếu mục đang chọn không còn tồn tại.
        private bool LoadComboBoxGhiChu(bool giuLuaChon = false)
        {
            if (comboBox1_GhiChu == null || comboBox1_GhiChu.IsDisposed) return false;
            string luaChonCu = giuLuaChon && comboBox1_GhiChu.SelectedIndex > 0 ? comboBox1_GhiChu.Text : "";
            var tapGiaTri = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool coDongTrong = false;
            void Them(string? gc)
            {
                string n = ChuanHoaGhiChu(gc);
                if (n.Length == 0) coDongTrong = true; else tapGiaTri.Add(n);
            }
            if (_isDataMaxMode)
            {
                foreach (var it in _dataCacheCBCS) Them(it.GhiChu);
                foreach (var it in _dataCacheTanBinh) Them(it.GhiChu);
            }
            else if (dtDanhSachGoc != null && dtDanhSachGoc.Columns.Contains("GhiChu"))
            {
                int o = dtDanhSachGoc.Columns.IndexOf("GhiChu");
                foreach (DataRow r in dtDanhSachGoc.Rows)
                {
                    if (r.RowState == DataRowState.Deleted) continue;
                    Them(r[o] == DBNull.Value ? null : r[o]?.ToString());
                }
            }
            var dsGiaTri = tapGiaTri.ToList();
            dsGiaTri.Sort(StringComparer.CurrentCultureIgnoreCase);
            var danhSachMoi = new List<string> { Module_HeThong.Tat_Ca };
            if (coDongTrong) danhSachMoi.Add(GHICHU_TRONG);
            danhSachMoi.AddRange(dsGiaTri);
            bool giongHet = comboBox1_GhiChu.Items.Count == danhSachMoi.Count;
            for (int i = 0; giongHet && i < danhSachMoi.Count; i++)
                if (!string.Equals(comboBox1_GhiChu.Items[i]?.ToString(), danhSachMoi[i], StringComparison.Ordinal)) giongHet = false;
            bool matLuaChon = false;
            comboBox1_GhiChu.SelectedIndexChanged -= comboBox1_GhiChu_SelectedIndexChanged;
            try
            {
                if (!giongHet)
                {
                    comboBox1_GhiChu.BeginUpdate();
                    try
                    {
                        comboBox1_GhiChu.Items.Clear();
                        comboBox1_GhiChu.Items.AddRange(danhSachMoi.ToArray());
                    }
                    finally { comboBox1_GhiChu.EndUpdate(); }
                    // Mở rộng khung xổ xuống cho vừa chữ (tối đa 420px)
                    int rong = comboBox1_GhiChu.Width;
                    foreach (var t in danhSachMoi)
                        rong = Math.Max(rong, TextRenderer.MeasureText(t, comboBox1_GhiChu.Font).Width + 30);
                    comboBox1_GhiChu.DropDownWidth = Math.Min(rong, 420);
                }
                int viTri = 0;
                if (luaChonCu.Length > 0)
                {
                    viTri = danhSachMoi.FindIndex(x => string.Equals(x, luaChonCu, StringComparison.OrdinalIgnoreCase));
                    if (viTri < 0) { viTri = 0; matLuaChon = true; }
                }
                if (comboBox1_GhiChu.SelectedIndex != viTri) comboBox1_GhiChu.SelectedIndex = viTri;
            }
            finally
            {
                comboBox1_GhiChu.SelectedIndexChanged += comboBox1_GhiChu_SelectedIndexChanged;
            }
            return matLuaChon;
        }
        // Rê chuột vào ô Ghi chú bị cắt chữ -> hiện đầy đủ nội dung
        private void KryptonDataGridView1_CellToolTipTextNeeded(object? sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.ColumnIndex >= kryptonDataGridView1.Columns.Count) return;
            if (!string.Equals(kryptonDataGridView1.Columns[e.ColumnIndex].Name, "GhiChu", StringComparison.Ordinal)) return;
            var chiSo = _filteredIndexes;
            if (e.RowIndex >= chiSo.Count) return;
            try
            {
                int idx = chiSo[e.RowIndex];
                string? gc = null;
                if (_isDataMaxMode)
                {
                    if (HienThiTanBinh) { if (idx < _dataCacheTanBinh.Count) gc = _dataCacheTanBinh[idx].GhiChu; }
                    else if (idx < _dataCacheCBCS.Count) gc = _dataCacheCBCS[idx].GhiChu;
                }
                else if (dtDanhSachGoc != null && dtDanhSachGoc.Columns.Contains("GhiChu") && idx < dtDanhSachGoc.DefaultView.Count)
                {
                    gc = dtDanhSachGoc.DefaultView[idx]["GhiChu"]?.ToString();
                }
                if (!string.IsNullOrWhiteSpace(gc)) e.ToolTipText = gc;
            }
            catch { }
        }
        // ID của dòng dữ liệu gốc theo chỉ số (chỉ số trong DefaultView hoặc bộ đệm DataMax)
        private string LayIdTheoChiSo(int actualIndex)
        {
            if (actualIndex < 0) return "";
            if (_isDataMaxMode)
            {
                if (HienThiTanBinh) return actualIndex < _dataCacheTanBinh.Count ? _dataCacheTanBinh[actualIndex].ID : "";
                return actualIndex < _dataCacheCBCS.Count ? _dataCacheCBCS[actualIndex].ID : "";
            }
            var view = dtDanhSachGoc?.DefaultView;
            return view != null && actualIndex < view.Count ? view[actualIndex]["ID"]?.ToString() ?? "" : "";
        }
        // Form con vừa sửa xong: đọc lại dòng đó rồi đưa con trỏ về đúng dòng trên lưới
        private void MoFormSuaThongTin(int rowIndex)
        {
            try
            {
                if (rowIndex < 0) return;
                if (_dangXuLyLuongNen == 1) return; // đang nạp dữ liệu -> không mở form sửa trên dữ liệu cũ
                this.Cursor = Cursors.WaitCursor;
                string id = "", donVi = "", hoTen = "", tinhTrang = "", soHieu = "";
                bool laTanBinh = HienThiTanBinh;
                if (_filteredIndexes == null || rowIndex >= _filteredIndexes.Count) return;
                int actualIndex = _filteredIndexes[rowIndex];
                if (_isDataMaxMode)
                {
                    if (laTanBinh)
                    {
                        if (actualIndex >= _dataCacheTanBinh.Count) return;
                        var dataObj = _dataCacheTanBinh[actualIndex];
                        id = dataObj.ID;
                        donVi = dataObj.DonViE;
                        hoTen = dataObj.HoTenE;
                        tinhTrang = dataObj.TinhTrang;
                    }
                    else
                    {
                        if (actualIndex >= _dataCacheCBCS.Count) return;
                        var dataObj = _dataCacheCBCS[actualIndex];
                        id = dataObj.ID;
                        donVi = dataObj.DonViE;
                        hoTen = dataObj.HoTenE;
                        tinhTrang = dataObj.TinhTrang;
                        soHieu = dataObj.SoHieuE;
                    }
                }
                else
                {
                    if (dtDanhSachGoc == null) return;
                    if (actualIndex < 0 || actualIndex >= dtDanhSachGoc.DefaultView.Count) return;
                    DataRowView rowView = dtDanhSachGoc.DefaultView[actualIndex];
                    id = rowView["ID"]?.ToString() ?? "";
                    donVi = rowView["DonVi"]?.ToString() ?? "";
                    hoTen = rowView["HoVaTen"]?.ToString() ?? "";
                    tinhTrang = rowView["TinhTrang"]?.ToString() ?? "";
                    soHieu = rowView["SoHieu"]?.ToString() ?? "";
                }
                if (string.IsNullOrWhiteSpace(id) || !int.TryParse(id, out int idInt)) return;
                this.Cursor = Cursors.Default;
                // Form22/Form62 được giữ sẵn trong RAM (Module_HoSoThiDua) => mở tức thì. hopThoai: khóa Form chính tới khi
                // đóng (giống ShowDialog cũ); bấm Cập nhật thành công thì đọc lại dòng vừa sửa.
                void SauKhiLuu()
                {
                    if (IsDisposed) return;
                    try
                    {
                        using (var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path)))
                        {
                            cn.Open();
                            CapNhatTongLoai_Cho(idInt, cn);
                        }
                        RefreshRowFromDb(id);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi cập nhật dòng vừa sửa: " + ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                if (laTanBinh)
                    Module_HoSoThiDua.MoFormNhomTuan(this, hopThoai: true, idInt, soHieu, hoTen, donVi, chiXem: false, SauKhiLuu);
                else
                    Module_HoSoThiDua.MoForm22(this, hopThoai: true, idInt, soHieu, hoTen, donVi, tinhTrang, chiXem: false, SauKhiLuu);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi mở Form sửa dữ liệu: " + ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }
        private void KryptonDataGridView1_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                // Chỉ cho phép double click các cột thông tin
                var col = kryptonDataGridView1.Columns[e.ColumnIndex];
                if (col.Name != "HoVaTen" && col.Name != "SoHieu" && col.Name != "DonVi" && col.Name != "TinhTrang")
                    return;
                MoFormSuaThongTin(e.RowIndex);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }
        private void suaThongTin_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (kryptonDataGridView1.RowCount <= 0)
            {
                MessageBox.Show("Hiện tại không có dữ liệu để sửa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (kryptonDataGridView1.CurrentCell == null)
            {
                MessageBox.Show("Vui lòng chọn một dòng thông tin cần sửa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int rowIndex = kryptonDataGridView1.CurrentCell.RowIndex;
            if (rowIndex < 0) return;
            MoFormSuaThongTin(rowIndex);
        }
        // Có dữ liệu -> hiện "Sửa thông tin"; không có -> ẩn
        //TrungKien
        private void CapNhatMenuSuaThongTin()
        {
            try { suaThongTin_ToolStripMenuItem.Visible = kryptonDataGridView1.RowCount > 0; }
            catch { suaThongTin_ToolStripMenuItem.Visible = false; }
        }
        // Hiện số dòng đang chọn trên mục "Xóa dữ liệu CBCS"
        private void CapNhatMenuXoaDuLieu()
        {
            try
            {
                int soChon = kryptonDataGridView1.Rows.GetRowCount(DataGridViewElementStates.Selected);
                xoaDuLieu_ToolStripMenuItem.Text = soChon > 1
                    ? $"Xóa dữ liệu CBCS ({soChon:N0} dòng đã chọn)"
                    : "Xóa dữ liệu CBCS";
            }
            catch { xoaDuLieu_ToolStripMenuItem.Text = "Xóa dữ liệu CBCS"; }
        }
        private void kryptonButton_LamMoiCacOTimKiem_Click(object? sender, EventArgs e)
        {
            DatLaiBoLocVeMacDinh();
            ApplyFilter();
        }
        private void dongBoDuLieu_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            kryptonButton_CapNhat.PerformClick();
        }
        private void xemThongKeThiDuaTapThe_Click(object? sender, EventArgs e)
        {
            if (_dangMoThongKeTapThe) return; // chống click đúp
            // Thống kê tập thể chỉ có ở phiên bản CBCS (Form53 đã ẩn menu; chặn thêm phím tắt Ctrl+T)
            if (LaTanBinhHienTai())
            {
                Module_ThongBao.Info("Thống kê thi đua tập thể chỉ áp dụng cho phiên bản CBCS.");
                return;
            }
            _dangMoThongKeTapThe = true;
            try
            {
                var ctxBackup = kryptonDataGridView1.ContextMenuStrip;
                kryptonDataGridView1.ContextMenuStrip = null;
                var f = new Form23_ThongKeThiDuaTapThe { StartPosition = FormStartPosition.CenterScreen };
                f.FormClosed += (s, args) =>
                {
                    kryptonDataGridView1.ContextMenuStrip = ctxBackup;
                    _dangMoThongKeTapThe = false;
                    this.Activate();
                };
                f.Show();
            }
            catch (Exception ex)
            {
                _dangMoThongKeTapThe = false;
                Debug.WriteLine($"Lỗi mở form thống kê tập thể: {ex.Message}");
            }
        }
        //private void xoaCot_ToolStripMenuItem_Click(object? sender, EventArgs e)
        //{
            //bool laTanBinh = HienThiTanBinh;
            //// Cột cấm xóa: cột cơ bản của bảng + GhiChu (cột ảo lấy từ CSDL2)
        private void xoaDuLieuThiDuaThang_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            try
            {
                using (Form18_XoaDataThang frm = new Form18_XoaDataThang(this))
                {
                    frm.ShowInTaskbar = false;
                    frm.StartPosition = FormStartPosition.CenterParent;
                    frm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở Form18: {ex.Message}", Module_HeThong.TieuDe_Loi, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Lấy (ID, Họ tên) của đúng các dòng đang được TÔ CHỌN trên lưới (không lấy dòng hiện hành chưa tô,
        // vì Ctrl+bấm bỏ chọn vẫn để lại con trỏ ở dòng đó). Duyệt theo trạng thái dòng để không
        // "unshare" hàng nghìn dòng trong VirtualMode.
        private List<(int id, string hoTen)> LayCacDongDangChon()
        {
            var kq = new List<(int id, string hoTen)>();
            var grid = kryptonDataGridView1;
            var ds = _filteredIndexes;
            if (grid == null || ds == null || ds.Count == 0) return kq;
            int soChon = grid.Rows.GetRowCount(DataGridViewElementStates.Selected);
            if (soChon == 0) return kq;
            var layTen = TaoBoLayCot("HoVaTen", HienThiTanBinh);
            var daCo = new HashSet<int>();
            int soDong = Math.Min(grid.RowCount, ds.Count);
            for (int i = 0; i < soDong && kq.Count < soChon; i++)
            {
                if ((grid.Rows.GetRowState(i) & DataGridViewElementStates.Selected) == 0) continue;
                int goc = ds[i];
                if (int.TryParse(LayIdTheoChiSo(goc), out int id) && id > 0 && daCo.Add(id))
                    kq.Add((id, (layTen(goc) ?? "").Trim()));
            }
            return kq;
        }
        private sealed class DuLieuDaThayDoiException : Exception
        {
            public DuLieuDaThayDoiException(string thongBao) : base(thongBao) { }
        }
        // Chạy ở luồng nền. Đối chiếu từng ID với họ tên trong CSDL rồi mới xóa, tất cả trong 1 transaction:
        // chỉ cần 1 dòng không khớp (đã bị xóa / ID đã đổi người) là hủy toàn bộ, không xóa dòng nào.
        private int XoaCacDongTrongCSDL(string tenBang, IReadOnlyList<(int id, string hoTen)> dsXoa)
        {
            using var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path));
            cn.Open();
            int dem = 0;
            using (var tran = cn.BeginTransaction())
            {
                // 1. Đọc lại họ tên hiện có trong CSDL của các ID cần xóa
                var canXoa = new HashSet<int>(dsXoa.Select(x => x.id));
                var tenMaHoa = new Dictionary<int, string>(canXoa.Count);
                using (var cmdDoc = new SqliteCommand($"SELECT ID, HoVaTen FROM {tenBang}", cn, tran))
                using (var rd = cmdDoc.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        if (rd.IsDBNull(0)) continue;
                        if (!int.TryParse(rd.GetValue(0)?.ToString(), out int id)) continue;
                        if (canXoa.Contains(id)) tenMaHoa[id] = rd.IsDBNull(1) ? "" : rd.GetValue(1)?.ToString() ?? "";
                    }
                }
                // 2. Giải mã song song rồi so khớp với họ tên người dùng đã xác nhận
                var ten = new string[dsXoa.Count];
                Parallel.For(0, dsXoa.Count, i =>
                {
                    ten[i] = tenMaHoa.TryGetValue(dsXoa[i].id, out var enc) ? SafeDecrypt(enc).Trim() : "\0";
                });
                var loi = new List<string>();
                for (int i = 0; i < dsXoa.Count; i++)
                {
                    if (ten[i] == "\0") loi.Add($"ID={dsXoa[i].id} ({dsXoa[i].hoTen}): không còn trong CSDL");
                    else if (!string.Equals(ten[i], dsXoa[i].hoTen, StringComparison.Ordinal))
                        loi.Add($"ID={dsXoa[i].id}: trên lưới là \"{dsXoa[i].hoTen}\" nhưng CSDL là \"{ten[i]}\"");
                }
                if (loi.Count > 0)
                {
                    string chiTiet = string.Join("\n", loi.Take(5)) + (loi.Count > 5 ? $"\n... và {loi.Count - 5:N0} dòng khác" : "");
                    throw new DuLieuDaThayDoiException(chiTiet);
                }
                // 3. Xóa
                using var cmd = new SqliteCommand($"DELETE FROM {tenBang} WHERE ID = @id", cn, tran);
                var p = cmd.Parameters.Add("@id", SqliteType.Integer);
                cmd.Prepare();
                foreach (var x in dsXoa)
                {
                    p.Value = x.id;
                    dem += cmd.ExecuteNonQuery();
                }
                if (dem != dsXoa.Count)
                    throw new DuLieuDaThayDoiException($"Chỉ xóa được {dem:N0}/{dsXoa.Count:N0} dòng.");
                tran.Commit();
            }
            // Thu gọn tệp: dữ liệu đã xóa xong, lỗi ở đây (CSDL đang bận...) không được coi là xóa thất bại
            try
            {
                using var cmdVacuum = new SqliteCommand("VACUUM;", cn);
                cmdVacuum.ExecuteNonQuery();
            }
            catch (Exception ex) { NK("Xóa CBCS: VACUUM lỗi (bỏ qua): " + ex.Message); }
            return dem;
        }
        private async void xoaDuLieu_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (_dangXuLyLuongNen == 1) return;
            var dsXoa = LayCacDongDangChon();
            if (dsXoa.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn dòng cần xóa.\n\nMẹo: giữ Ctrl hoặc Shift khi bấm chuột để chọn nhiều dòng, Ctrl + A (khi đang ở lưới) để chọn tất cả các dòng đang hiển thị.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            bool laTanBinh = HienThiTanBinh;
            string tenBang = (laTanBinh ? Module_CheDoDoiTuong.BangThiDuaThang : Module_CheDoDoiTuong.ThiDuaThang(CheDoDoiTuong.CBCS));
            static string TenHienThi(string t) => string.IsNullOrWhiteSpace(t) ? "(không xác định)" : t;
            // Xác nhận: mặc định nút "No" để lỡ bấm Enter không xóa
            string noiDung;
            if (dsXoa.Count == 1)
            {
                noiDung = $"Bạn có chắc chắn muốn xóa {Module_HeThong.Tu_dong_chi}:\n\n👉 {TenHienThi(dsXoa[0].hoTen)}\n\nThao tác này không thể hoàn tác.";
            }
            else
            {
                const int SO_TEN_HIEN = 10;
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Bạn có chắc chắn muốn xóa {dsXoa.Count:N0} {Module_HeThong.Tu_dong_chi} đã chọn:");
                sb.AppendLine();
                foreach (var x in dsXoa.Take(SO_TEN_HIEN)) sb.AppendLine("👉 " + TenHienThi(x.hoTen));
                if (dsXoa.Count > SO_TEN_HIEN) sb.AppendLine($"... và {dsXoa.Count - SO_TEN_HIEN:N0} người khác");
                sb.AppendLine();
                sb.Append("Thao tác này không thể hoàn tác.");
                noiDung = sb.ToString();
            }
            if (MessageBox.Show(noiDung, "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            DialogResult kq;
            using (Form24_XacMinhAdmin frm = new Form24_XacMinhAdmin())
            {
                frm.TopMost = true;
                frm.StartPosition = FormStartPosition.CenterScreen;
                kq = frm.ShowDialog();
            }
            if (kq != DialogResult.OK || IsDisposed) return;
            // Chiếm cờ bận: chặn F5/F6/nạp lại và bấm xóa lần 2 trong lúc đang xóa
            if (Interlocked.Exchange(ref _dangXuLyLuongNen, 1) == 1)
            {
                MessageBox.Show("Phần mềm đang xử lý dữ liệu, vui lòng thử lại sau giây lát.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            bool canNapLai = false;
            try
            {
                UseWaitCursor = true;
                toolStripStatusLabel1.ForeColor = _mauChuTrangThai;
                toolStripStatusLabel1.Text = $"Đang xóa {dsXoa.Count:N0} dòng...";
                int daXoa;
                try
                {
                    daXoa = await Task.Run(() => XoaCacDongTrongCSDL(tenBang, dsXoa));
                }
                catch (DuLieuDaThayDoiException ex)
                {
                    canNapLai = true; // lưới đang lệch với CSDL -> nạp lại để người dùng chọn lại
                    Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Hủy xóa dòng thống kê (dữ liệu đã thay đổi)", ex.Message);
                    MessageBox.Show("ĐÃ HỦY, CHƯA XÓA DÒNG NÀO.\n\nDữ liệu trên lưới không còn khớp với cơ sở dữ liệu:\n\n" + ex.Message +
                        "\n\nDanh sách sẽ được nạp lại, vui lòng chọn lại các dòng cần xóa.", "Hủy xóa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                catch (Exception ex)
                {
                    Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Lỗi xóa dòng thống kê", ex.ToString());
                    MessageBox.Show("Xóa dữ liệu thất bại, chưa xóa dòng nào:\n" + ex.Message, Module_HeThong.TieuDe_Loi, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                canNapLai = true;
                if (dsXoa.Count == 1)
                    Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Xóa dòng thống kê", $"Đã xóa: {TenHienThi(dsXoa[0].hoTen)} (ID={dsXoa[0].id})");
                else
                    Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Xóa nhiều dòng thống kê",
                        $"Đã xóa {daXoa:N0} dòng: " + string.Join("; ", dsXoa.Take(50).Select(x => $"{TenHienThi(x.hoTen)} (ID={x.id})"))
                        + (dsXoa.Count > 50 ? $"; ... và {dsXoa.Count - 50:N0} dòng khác" : ""));
                try { System.Media.SystemSounds.Exclamation.Play(); } catch { }
            }
            finally
            {
                try
                {
                    if (canNapLai && !IsDisposed)
                    {
                        kryptonDataGridView1.ClearSelection();
                        await DieuPhoiLoadDuLieuAsync(laTanBinh); // đúng chế độ thường / DataMax
                        if (!IsDisposed)
                        {
                            LoadComboBoxGhiChu(giuLuaChon: true);
                            await LocLaiAsync();              // lọc xong sẽ tự cập nhật dòng "Tổng cộng"
                        }
                    }
                    else if (!IsDisposed)
                    {
                        DatTrangThai(_filteredIndexes.Count, _isDataMaxMode); // trả lại dòng trạng thái
                    }
                }
                catch (Exception ex)
                {
                    NK("Xóa CBCS: lỗi nạp lại: " + ex);
                }
                finally
                {
                    if (!IsDisposed) UseWaitCursor = false;
                    Interlocked.Exchange(ref _dangXuLyLuongNen, 0);
                    TiepTucNeuCoYeuCauLamMoi();
                }
            }
        }
        private void CapNhatPhienBanPhanMem()
        {
            try
            {
                string doiTuong = Module_TaiKhoan.LayPhienBanPhanMem();
                if (string.IsNullOrWhiteSpace(doiTuong))
                    doiTuong = "Phần mềm: (không xác định)";
                toolStripStatusLabel2.Spring = true;      // chiếm phần còn lại
                toolStripStatusLabel2.TextAlign = ContentAlignment.MiddleRight;
                toolStripStatusLabel2.Text = doiTuong;
            }
            catch
            {
                toolStripStatusLabel2.Text = "Phần mềm: (không xác định)";
            }
        }
        private string XacDinhTenTieuDoan()
        {
            try
            {
                using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl2Path));
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT TenTieuDoan FROM ThongTin WHERE ID=1";
                var result = cmd.ExecuteScalar();
                if (result != null && !string.IsNullOrWhiteSpace(result.ToString()))
                    return SafeDecrypt(result.ToString()!.Trim()); // đọc được cả tên cũ và mới
                return "Không xác định";
            }
            catch { return "Không xác định"; }
        }
        private void tinhPhanLoaiThang_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            // 1. Xác định phiên bản (theo loại dữ liệu đang hiển thị)
            bool laTanBinh = HienThiTanBinh;
            // 2. Kiểm tra dữ liệu và chuẩn bị DataTable truyền sang Form36
            DataTable? dtTruyenSang = null;
            if (_isDataMaxMode)
            {
                bool khongCoDuLieu = laTanBinh
                    ? _dataCacheTanBinh.Count == 0
                    : _dataCacheCBCS.Count == 0;
                if (khongCoDuLieu)
                {
                    MessageBox.Show("Chưa có dữ liệu để tính toán. Vui lòng tải dữ liệu hoặc bấm nút Cập nhật (F6).",
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                // Truyền null có chủ đích -> Form36 tự load từ CSDL4
                dtTruyenSang = null;
            }
            else
            {
                if (dtDanhSachGoc == null || dtDanhSachGoc.Rows.Count == 0)
                {
                    MessageBox.Show("Chưa có dữ liệu để tính toán. Vui lòng tải dữ liệu hoặc bấm nút Cập nhật (F6).",
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                // Truyền bản Copy để Form36 không ảnh hưởng dữ liệu gốc Form15
                dtTruyenSang = dtDanhSachGoc.Copy();
            }
            // 3. Tìm Form cha và Panel chứa
            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            if (formCha == null) return;
            var panel = formCha.Controls.Find("PanelContainer", true).FirstOrDefault() as Panel;
            if (panel == null) return;
            // 4. Form 36 đã mở thì chỉ đưa lên trước
            var f36Existing = panel.Controls.OfType<Form36_TinhPhanLoaiThang>().FirstOrDefault();
            if (f36Existing != null)
            {
                f36Existing.BringToFront();
                return;
            }
            // 5. Tạo Form 36 mới và truyền dữ liệu
            var f36 = new Form36_TinhPhanLoaiThang(dtTruyenSang!, laTanBinh)
            {
                Text = laTanBinh ? $"Tính phân loại thi đua tháng - {Module_TaiKhoan.TenTB_TieuDe}" : "Tính phân loại thi đua - CBCS",
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill
            };
            panel.Controls.Add(f36);
            f36.Show();
            f36.BringToFront();
            // 6. Đóng Form 36 -> quay lại Form 15
            f36.FormClosed += (s, ev) =>
            {
                if (this.IsDisposed) return;
                this.Dock = DockStyle.Fill;
                this.Show();
                this.BringToFront();
            };
        }
        // =====================================================================
        //  XUẤT EXCEL
        // =====================================================================
        private void xuatDuLieu_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            kryptonButton_XuatData.PerformClick();
        }
        // Bộ đọc giá trị 1 cột theo chỉ số dòng gốc (chế độ DataMax hoặc thường). Tạo 1 lần cho mỗi cột.
        private Func<int, string> TaoBoLayCot(string col, bool laTanBinh)
        {
            if (_isDataMaxMode)
            {
                if (laTanBinh)
                {
                    var ds = _dataCacheTanBinh; var lay = LayCotTanBinh(col);
                    return i => (uint)i < (uint)ds.Count ? lay(ds[i]) ?? "" : "";
                }
                else
                {
                    var ds = _dataCacheCBCS; var lay = LayCotCBCS(col);
                    return i => (uint)i < (uint)ds.Count ? lay(ds[i]) ?? "" : "";
                }
            }
            var dt = dtDanhSachGoc;
            if (dt == null) return _ => "";
            int ord = dt.Columns.IndexOf(col);
            if (ord < 0) return _ => "";
            var view = dt.DefaultView;
            return i => (uint)i < (uint)view.Count ? view[i][ord]?.ToString() ?? "" : "";
        }
        private async void kryptonButton_XuatData_Click(object? sender, EventArgs e)
        {
            string textBanDau = kryptonButton_XuatData.Values.Text;
            Image? anhBanDau = kryptonButton_XuatData.Values.Image;
            Form_Loading frmLoad = new Form_Loading("Đang tạo tệp excel, vui lòng đợi...");
            bool isLoadShown = false;
            frmLoad.Icon = this.Icon;
            try
            {
                kryptonButton_XuatData.Enabled = false;
                kryptonButton_XuatData.Values.Text = "Đang tạo...";
                kryptonButton_XuatData.Values.Image = null;
                if (kryptonDataGridView1.Rows.Count == 0 ||
                    (kryptonDataGridView1.Rows.Count == 1 && kryptonDataGridView1.AllowUserToAddRows))
                {
                    MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                try
                {
                    string dbPath = _csdl4Path;
                    if (!File.Exists(dbPath))
                    {
                        MessageBox.Show("Không tìm thấy CSDL!", Module_HeThong.TieuDe_Loi, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    bool laTanBinh = HienThiTanBinh;
                    string tenBang = (laTanBinh ? Module_CheDoDoiTuong.BangThiDuaThang : Module_CheDoDoiTuong.ThiDuaThang(CheDoDoiTuong.CBCS));
                    var cotTrongBang = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    using (var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(dbPath)))
                    {
                        cn.Open();
                        using var cmd = new SqliteCommand($"PRAGMA table_info({tenBang})", cn);
                        using var rd = cmd.ExecuteReader();
                        while (rd.Read())
                        {
                            string cot = rd["name"].ToString() ?? "";
                            if (!cot.Equals("ID", StringComparison.OrdinalIgnoreCase)) cotTrongBang.Add(cot);
                        }
                    }
                    var exportCols = kryptonDataGridView1.Columns
                        .Cast<DataGridViewColumn>()
                        .Where(c => cotTrongBang.Contains(c.Name))
                        .Where(c =>
                        {
                            if (laTanBinh && Module_LichXet.LaCotKhongDung(Module_CheDoDoiTuong.Hien, c.Name)) return false;
                            return true;
                        })
                        .ToList();
                    var normalCols = exportCols.Where(c => !c.Name.StartsWith("TS_Loai") && !c.Name.Equals("GhiChu", StringComparison.OrdinalIgnoreCase)).ToList();
                    var totalCols = exportCols.Where(c => c.Name.StartsWith("TS_Loai")).OrderBy(c => c.Name).ToList();
                    exportCols = normalCols.Concat(totalCols).ToList();
                    if (kryptonDataGridView1.Columns.Contains("GhiChu"))
                        exportCols.Add(kryptonDataGridView1.Columns["GhiChu"]);
                    bool hasSTT = kryptonDataGridView1.Columns.Cast<DataGridViewColumn>()
                        .Any(c => c.Name.Equals("STT", StringComparison.OrdinalIgnoreCase));
                    using var sfd = new SaveFileDialog
                    {
                        Title = "Chọn nơi lưu file Excel thống kê",
                        Filter = Module_HeThong.BoLoc_Excel,
                        FileName = $"ThongKe_ThiDua_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    };
                    if (sfd.ShowDialog() != DialogResult.OK) return;
                    string filePath = sfd.FileName;
                    this.Enabled = false;
                    frmLoad.Show(this);
                    isLoadShown = true;
                    Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Xuất Excel thống kê", $"Bảng: {tenBang} | {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
                    // Thứ tự xuất = đúng thứ tự đã lọc/sắp xếp trên lưới
                    int[] thuTu = _filteredIndexes.ToArray();
                    int rowCount = thuTu.Length;
                    int offset = hasSTT ? 0 : 1; // vị trí cột dữ liệu đầu tiên trong dataArray
                    int colCountFull = exportCols.Count + offset;
                    var colsSnapshot = exportCols;
                    // Bộ đọc mỗi cột tạo 1 lần (không tạo lại cho từng ô)
                    var layCot = colsSnapshot.Select(c => TaoBoLayCot(c.Name, laTanBinh)).ToArray();
                    var dataArray = await Task.Run(() =>
                    {
                        var arr = new object[rowCount, colCountFull];
                        for (int r = 0; r < rowCount; r++)
                        {
                            if (!hasSTT) arr[r, 0] = r + 1;
                            int actualIndex = thuTu[r];
                            for (int c = 0; c < layCot.Length; c++)
                                arr[r, c + offset] = layCot[c](actualIndex);
                        }
                        return arr;
                    });
                    // Bỏ cột "Tổng loại" không có dữ liệu (chỉ CBCS)
                    if (!laTanBinh && rowCount > 0)
                    {
                        var boQua = new HashSet<int>();
                        for (int c = 0; c < exportCols.Count; c++)
                        {
                            if (!exportCols[c].Name.StartsWith("TS_Loai")) continue;
                            bool coDuLieu = false;
                            for (int r = 0; r < rowCount; r++)
                            {
                                string v = dataArray[r, c + offset]?.ToString()?.Trim() ?? "";
                                if (v.Length > 0 && v != "0") { coDuLieu = true; break; }
                            }
                            if (!coDuLieu) boQua.Add(c);
                        }
                        if (boQua.Count > 0)
                        {
                            var colsMoi = new List<DataGridViewColumn>();
                            var viTriGiu = new List<int>();
                            for (int c = 0; c < offset; c++) viTriGiu.Add(c);
                            for (int c = 0; c < exportCols.Count; c++)
                            {
                                if (boQua.Contains(c)) continue;
                                colsMoi.Add(exportCols[c]);
                                viTriGiu.Add(c + offset);
                            }
                            var arrMoi = new object[rowCount, viTriGiu.Count];
                            for (int r = 0; r < rowCount; r++)
                                for (int k = 0; k < viTriGiu.Count; k++)
                                    arrMoi[r, k] = dataArray[r, viTriGiu[k]];
                            dataArray = arrMoi;
                            exportCols = colsMoi;
                        }
                    }
                    int colCount = exportCols.Count + offset;
                    await Task.Run(() =>
                    {
                        using var wb = new XLWorkbook();
                        var ws = wb.Worksheets.Add("ThongKeThiDua");
                        ws.TabColor = XLColor.FromHtml("#92D050");
                        var mauChuTieuDe = XLColor.Green;
                        ws.Cell("A1").Value = "DANH SÁCH";
                        var tieuDe1 = ws.Range(1, 1, 1, colCount);
                        tieuDe1.Merge();
                        tieuDe1.Style.Font.Bold = true;
                        tieuDe1.Style.Font.FontSize = 16;
                        tieuDe1.Style.Font.FontColor = mauChuTieuDe;
                        tieuDe1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        tieuDe1.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        ws.Cell("A2").Value = $"THỐNG KÊ PHÂN LOẠI THI ĐUA CỦA {(laTanBinh ? Module_TaiKhoan.TenTB_InHoa : "CBCS")} {XacDinhTenTieuDoan()}";
                        var tieuDe2 = ws.Range(2, 1, 2, colCount);
                        tieuDe2.Merge();
                        tieuDe2.Style.Font.Bold = true;
                        tieuDe2.Style.Font.FontSize = 12;
                        tieuDe2.Style.Font.FontColor = mauChuTieuDe;
                        tieuDe2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        tieuDe2.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        int excelStartRow = 4;
                        int excelCol = 1;
                        if (!hasSTT)
                        {
                            var cell = ws.Cell(excelStartRow, excelCol);
                            cell.Value = "STT";
                            cell.Style.Font.Bold = true;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            excelCol++;
                        }
                        foreach (var col in exportCols)
                        {
                            var cell = ws.Cell(excelStartRow, excelCol);
                            cell.Value = string.IsNullOrWhiteSpace(col.HeaderText) ? col.Name.Replace("_", " ") : col.HeaderText;
                            cell.Style.Font.Bold = true;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            cell.Style.Alignment.WrapText = true;
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            if (laTanBinh && (Module_LichXet.DotCuaCot(Module_CheDoDoiTuong.Hien, col.Name)?.So ?? 0) % 2 == 1)
                                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(220, 235, 255);
                            else if (col.Name.StartsWith("TS_Loai"))
                                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(230, 250, 230);
                            else
                                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                            if (laTanBinh && col.Name.StartsWith("Thang_")) cell.Style.Font.FontColor = XLColor.Blue;
                            excelCol++;
                        }
                        int hoTenColIndex = exportCols.FindIndex(c => c.Name.Equals("HoVaTen", StringComparison.OrdinalIgnoreCase));
                        if (!hasSTT) hoTenColIndex += 1;
                        // Nạp dữ liệu 1 lần (nhanh hơn gán từng ô rất nhiều)
                        if (rowCount > 0)
                        {
                            ws.Cell(excelStartRow + 1, 1).InsertData(LayCacDongDuLieu(dataArray, rowCount, colCount));
                            var dataRange = ws.Range(excelStartRow + 1, 1, excelStartRow + rowCount, colCount);
                            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                            dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            dataRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            if (!hasSTT)
                                ws.Range(excelStartRow + 1, 1, excelStartRow + rowCount, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                            if (hoTenColIndex >= 0)
                                ws.Range(excelStartRow + 1, hoTenColIndex + 1, excelStartRow + rowCount, hoTenColIndex + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                            int ghiChuIdx = exportCols.FindIndex(x => x.Name.Equals("GhiChu", StringComparison.OrdinalIgnoreCase));
                            if (ghiChuIdx >= 0)
                            {
                                int ghiChuExcelCol = ghiChuIdx + offset + 1;
                                var ghiChuRange = ws.Range(excelStartRow + 1, ghiChuExcelCol, excelStartRow + rowCount, ghiChuExcelCol);
                                ghiChuRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                ghiChuRange.Style.Alignment.WrapText = true;
                            }
                            if (!laTanBinh)
                            {
                                // Ô trống ở 3 cột kết quả năm cũ -> họa tiết sọc chéo (gom vùng liên tiếp để tô 1 lần)
                                string[] cotKetQuaNamCu = { "KQ_ThiDua_Nam_Cu", "KQ_XepLoaiCB_Nam_Cu", "KQ_XepLoaiDangVien_Nam_Cu" };
                                foreach (var tenCot in cotKetQuaNamCu)
                                {
                                    int idx = exportCols.FindIndex(c => c.Name.Equals(tenCot, StringComparison.OrdinalIgnoreCase));
                                    if (idx < 0) continue;
                                    int cIdx = idx + offset;   // chỉ số trong dataArray
                                    int excelC = cIdx + 1;     // cột Excel
                                    int runStart = -1;
                                    for (int r = 0; r <= rowCount; r++)
                                    {
                                        bool trong = r < rowCount && string.IsNullOrWhiteSpace(dataArray[r, cIdx]?.ToString());
                                        if (trong && runStart < 0) runStart = r;
                                        else if (!trong && runStart >= 0)
                                        {
                                            var rg = ws.Range(excelStartRow + 1 + runStart, excelC, excelStartRow + r, excelC);
                                            // Thứ tự quan trọng: màu nền trước, rồi kiểu họa tiết và màu sọc
                                            rg.Style.Fill.BackgroundColor = XLColor.White;
                                            rg.Style.Fill.PatternType = XLFillPatternValues.DarkUp;
                                            rg.Style.Fill.PatternColor = XLColor.Black;
                                            runStart = -1;
                                        }
                                    }
                                }
                            }
                            {
                                // Chỉ tô số 1 (đậm xanh) và số 3 (đậm đỏ); không áp dụng cho cột "Tổng loại".
                                // CBCS: cột Thang_ + Sau_Thang_Dau_Nam; Tân binh: cột Tuan_ + Thang_ (cùng quy tắc màu).
                                var cotToMau = new List<int>();
                                for (int c = 0; c < exportCols.Count; c++)
                                {
                                    string n = exportCols[c].Name;
                                    bool toMau = laTanBinh
                                        ? n.StartsWith("Tuan_") || n.StartsWith("Thang_")
                                        : n.StartsWith("Thang_") || n == "Sau_Thang_Dau_Nam";
                                    if (toMau) cotToMau.Add(c + offset + 1);
                                }
                                int i0 = 0;
                                while (i0 < cotToMau.Count)
                                {
                                    int i1 = i0;
                                    while (i1 + 1 < cotToMau.Count && cotToMau[i1 + 1] == cotToMau[i1] + 1) i1++;
                                    var vung = ws.Range(excelStartRow + 1, cotToMau[i0], excelStartRow + rowCount, cotToMau[i1]);
                                    string o = vung.FirstCell().Address.ToStringRelative();
                                    vung.AddConditionalFormat().WhenIsTrue($"=TRIM({o}&\"\")=\"1\"").Font.SetBold().Font.SetFontColor(XLColor.FromArgb(0, 0, 225));
                                    vung.AddConditionalFormat().WhenIsTrue($"=TRIM({o}&\"\")=\"3\"").Font.SetBold().Font.SetFontColor(XLColor.Red);
                                    i0 = i1 + 1;
                                }
                            }
                        }
                        int tongDong = rowCount + excelStartRow + 1;
                        var totalCell = ws.Cell(tongDong, 1);
                        totalCell.Value = $"Tổng cộng: {rowCount} {Module_HeThong.Tu_dong_chi}./.";
                        totalCell.Style.Font.SetBold().Font.SetItalic();
                        ws.Range(tongDong, 1, tongDong, colCount).Merge();
                        if (!laTanBinh)
                        {
                            var dtTapThe = new DataTable();
                            using (var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path)))
                            {
                                cn.Open();
                                using var cmd = new SqliteCommand("SELECT * FROM ThongKe_PhanLoaiTapThe_CBCS", cn);
                                using var rd = cmd.ExecuteReader();
                                dtTapThe.Load(rd);
                            }
                            if (dtTapThe.Rows.Count > 0)
                            {
                                var wsTapThe = wb.Worksheets.Add("ThongKe_PhanLoaiTapThe");
                                int soCotTT = dtTapThe.Columns.Count;
                                var tieuDeTT = new string?[soCotTT];
                                for (int c = 0; c < soCotTT; c++)
                                {
                                    tieuDeTT[c] = dtTapThe.Columns[c].ColumnName;
                                    wsTapThe.Cell(1, c + 1).Value = tieuDeTT[c];
                                }
                                var vungTieuDeTT = wsTapThe.Range(1, 1, 1, soCotTT);
                                vungTieuDeTT.Style.Font.Bold = true;
                                vungTieuDeTT.Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
                                Module_ExcelAnToan.KeVien(vungTieuDeTT);
                                // Giải mã sẵn thành mảng dòng rồi nạp 1 lần bằng InsertData
                                var dongTT = new List<object?[]>(dtTapThe.Rows.Count);
                                foreach (DataRow dr in dtTapThe.Rows)
                                {
                                    var d = new object?[soCotTT];
                                    for (int c = 0; c < soCotTT; c++)
                                    {
                                        string rawVal = dr[c]?.ToString() ?? "";
                                        d[c] = rawVal.Length == 0 ? null : Module_BaoMatAES.GiaiMa(rawVal);
                                    }
                                    dongTT.Add(d);
                                }
                                Module_ExcelAnToan.KeVien(Module_ExcelAnToan.NapDong(wsTapThe, 2, 1, dongTT));
                                Module_ExcelAnToan.DatDoRongCot(wsTapThe, dongTT, 1, tieuDeTT);
                                wsTapThe.Hide();
                            }
                        }
                        var wsVersion = wb.Worksheets.Add("Phiên bản");
                        var cellA1 = wsVersion.Cell("A1");
                        cellA1.Value = "2026 Competition Software developed by TrungKien";
                        cellA1.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9EAD3");
                        cellA1.Style.Font.Bold = true;
                        cellA1.Style.Font.FontColor = XLColor.Black;
                        string textPhienBan = laTanBinh ? $"Phần mềm dành cho {Module_TaiKhoan.TenTB_Hoa}" : "Phần mềm dành cho CBCS";
                        wsVersion.Cell("B1").Value = Module_BaoMatAES.MaHoa(textPhienBan);
                        wsVersion.Columns().AdjustToContents();
                        wsVersion.Hide();
                        // Tự tính độ rộng cột từ dataArray (thay AdjustToContents: đo từng ô rất chậm với hàng nghìn dòng)
                        int ghiChuIdxRong = exportCols.FindIndex(x => x.Name.Equals("GhiChu", StringComparison.OrdinalIgnoreCase));
                        int cotGhiChuExcelIdx = ghiChuIdxRong >= 0 ? ghiChuIdxRong + offset + 1 : -1;
                        for (int c = 0; c < colCount; c++)
                        {
                            int maxLen = 0;
                            string tieuDeCot = ws.Cell(excelStartRow, c + 1).GetString();
                            foreach (var dong in tieuDeCot.Split('\n'))
                                if (dong.Length > maxLen) maxLen = dong.Length;
                            for (int r = 0; r < rowCount; r++)
                            {
                                int len = dataArray[r, c]?.ToString()?.Length ?? 0;
                                if (len > maxLen) maxLen = len;
                            }
                            double rong = Math.Clamp(maxLen * 1.15 + 2, 6, 60);
                            if (c + 1 == cotGhiChuExcelIdx) rong = Math.Clamp(rong, 20, 40);
                            ws.Column(c + 1).Width = rong;
                        }
                        Module_BanQuyen.DongDauExcel(wb);
                        // Lưu qua tệp tạm rồi thay thế: tránh để lại tệp hỏng nếu lỗi giữa chừng / tệp đang mở trong Excel
                        Module_ExcelAnToan.LuuAnToan(wb, filePath);
                        Module_XuatNhapDuLieuThiDua.MoVaChonTepTrongExplorer(filePath);
                    });
                    Module_ThongBao.ThanhCong("Xuất Excel thành công!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi xuất dữ liệu:\n" + ex.Message, Module_HeThong.TieuDe_Loi, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                if (isLoadShown)
                {
                    frmLoad.Close();
                    this.Enabled = true;
                    this.Focus();
                }
                kryptonButton_XuatData.Values.Text = textBanDau;
                kryptonButton_XuatData.Values.Image = anhBanDau;
                kryptonButton_XuatData.Enabled = true;
            }
        }
        // =====================================================================
        //  SẮP XẾP: Đơn vị (ưu tiên) -> Đang công tác trước -> STT trong DanhSach
        // =====================================================================
        // Khóa sắp xếp: Đơn vị (ưu tiên) -> Đơn vị (chuẩn hóa) -> Đang công tác trước -> STT -> vị trí gốc (ổn định)
        private readonly struct KhoaSapXep : IComparable<KhoaSapXep>
        {
            public readonly int Pri;
            public readonly string Dv;
            public readonly int Rank;
            public readonly int Stt;
            public readonly int Pos;   // vị trí trong danh sách đầu vào, dùng để ổn định
            public readonly int Idx;   // chỉ số dòng dữ liệu gốc
            public KhoaSapXep(int pri, string dv, int rank, int stt, int pos, int idx)
            { Pri = pri; Dv = dv; Rank = rank; Stt = stt; Pos = pos; Idx = idx; }
            public int CompareTo(KhoaSapXep o)
            {
                int c = Pri.CompareTo(o.Pri); if (c != 0) return c;
                c = string.CompareOrdinal(Dv, o.Dv); if (c != 0) return c;
                c = Rank.CompareTo(o.Rank); if (c != 0) return c;
                c = Stt.CompareTo(o.Stt); if (c != 0) return c;
                return Pos.CompareTo(o.Pos);
            }
        }
        // Bộ lấy (Đơn vị, Số hiệu, Tình trạng) của dòng theo dữ liệu đang hiển thị
        private Func<int, (string dv, string sh, string tt)> TaoBoLaySXHienTai()
        {
            if (_isDataMaxMode)
            {
                if (HienThiTanBinh)
                {
                    var ds = _dataCacheTanBinh;
                    return i => (uint)i < (uint)ds.Count ? (ds[i].DonViE ?? "", ds[i].SoHieuE ?? "", ds[i].TinhTrang ?? "") : ("", "", "");
                }
                else
                {
                    var ds = _dataCacheCBCS;
                    return i => (uint)i < (uint)ds.Count ? (ds[i].DonViE ?? "", ds[i].SoHieuE ?? "", ds[i].TinhTrang ?? "") : ("", "", "");
                }
            }
            var dt = dtDanhSachGoc;
            if (dt == null) return _ => ("", "", "");
            var view = dt.DefaultView;
            int oDv = dt.Columns.IndexOf("DonVi"), oSh = dt.Columns.IndexOf("SoHieu"), oTt = dt.Columns.IndexOf("TinhTrang");
            return i =>
            {
                if ((uint)i >= (uint)view.Count) return ("", "", "");
                var r = view[i];
                return (r[oDv]?.ToString() ?? "", r[oSh]?.ToString() ?? "", r[oTt]?.ToString() ?? "");
            };
        }
        // Sắp xếp danh sách chỉ số dòng. Chạy được ở luồng nền (không đụng giao diện).
        private int[] SapXepChiSo(IReadOnlyList<int> chiSo, Func<int, (string dv, string sh, string tt)> lay)
        {
            int n = chiSo.Count;
            var kq = new int[n];
            if (n == 0) return kq;
            if (n == 1) { kq[0] = chiSo[0]; return kq; }
            var banDoSTT = LayBanDoSTT();
            var dicUuTien = TaoBangUuTienDonVi();
            // Mỗi đơn vị chỉ tính (ưu tiên + khóa) MỘT lần
            var dvInfo = new Dictionary<string, (int pri, string key)>(StringComparer.Ordinal);
            var keys = new KhoaSapXep[n];
            for (int i = 0; i < n; i++)
            {
                int idx = chiSo[i];
                var (dv, sh, tt) = lay(idx);
                if (!dvInfo.TryGetValue(dv, out var di))
                {
                    di = (dicUuTien.TryGetValue(dv, out int p) ? p : int.MaxValue, ChuanHoaDonViSoSanh(dv));
                    dvInfo[dv] = di;
                }
                int stt = int.MaxValue;
                if (banDoSTT != null && sh.Length > 0 && banDoSTT.TryGetValue(sh.Trim(), out int s)) stt = s;
                int rank = string.Equals(tt.Trim(), Module_HeThong.TT_DANG_CONG_TAC, StringComparison.OrdinalIgnoreCase) ? 0 : 1;
                keys[i] = new KhoaSapXep(di.pri, di.key, rank, stt, i, idx);
            }
            Array.Sort(keys);
            for (int i = 0; i < n; i++) kq[i] = keys[i].Idx;
            return kq;
        }
        // =====================================================================
        //  TÔ MÀU SỐ 1 / 3 VÀ ẨN CỘT TỔNG LOẠI TRỐNG (CBCS)
        // =====================================================================
        private bool[]? _colToMauSo; // cột tô màu số 1/3
        private static readonly Color _mauSo1 = Color.FromArgb(0, 0, 225);
        // Gọi cuối ApDungDinhDangGridThongKe: dựng cờ cột, gắn sự kiện vẽ, ẩn cột Tổng loại trống
        private void CapDinhDangLuoiCBCS(DataGridView grid, bool laTanBinh)
        {
            grid.CellFormatting -= Grid_CellFormatting_CBCS;
            if (laTanBinh) { _colToMauSo = null; return; }
            int n = grid.Columns.Count;
            var mau = new bool[n];
            for (int i = 0; i < n; i++)
            {
                string name = grid.Columns[i].Name;
                mau[i] = name.StartsWith("Thang_", StringComparison.Ordinal) || name == "Sau_Thang_Dau_Nam";
            }
            _colToMauSo = mau;
            grid.CellFormatting += Grid_CellFormatting_CBCS;
            AnCotTongLoaiTrongCBCS(grid);
        }
        // Số 1 -> đậm xanh (0,0,225); số 3 -> đậm đỏ; còn lại giữ mặc định
        private void Grid_CellFormatting_CBCS(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            var f = _colToMauSo;
            if (f == null || e.RowIndex < 0 || (uint)e.ColumnIndex >= (uint)f.Length || !f[e.ColumnIndex]) return;
            if (e.Value is not string s) return;
            s = s.Trim();
            if (s.Length != 1) return;
            var st = e.CellStyle;
            if (st == null) return;
            if (s[0] == '1')
            {
                st.Font = _fontGridHighlight; st.ForeColor = _mauSo1; st.SelectionForeColor = _mauSo1;
            }
            else if (s[0] == '3')
            {
                st.Font = _fontGridHighlight; st.ForeColor = Color.Red; st.SelectionForeColor = Color.Red;
            }
        }
        // Ẩn cột Tổng loại 1..4 nếu toàn bộ dữ liệu rỗng hoặc "0"
        private void AnCotTongLoaiTrongCBCS(DataGridView grid)
        {
            var co = QuetCotTongLoaiCoDuLieu();
            for (int k = 0; k < 4; k++)
            {
                string name = $"TS_Loai{k + 1}";
                if (grid.Columns.Contains(name) && grid.Columns[name].Visible != co[k])
                    grid.Columns[name].Visible = co[k];
            }
        }
        private bool[] QuetCotTongLoaiCoDuLieu()
        {
            var co = new bool[4];
            static bool Co(string? v) => !string.IsNullOrWhiteSpace(v) && v.Trim() != "0";
            if (_isDataMaxMode)
            {
                var ds = _dataCacheCBCS;
                for (int i = 0; i < ds.Count; i++)
                {
                    var d = ds[i];
                    if (!co[0] && Co(d.L1)) co[0] = true;
                    if (!co[1] && Co(d.L2)) co[1] = true;
                    if (!co[2] && Co(d.L3)) co[2] = true;
                    if (!co[3] && Co(d.L4)) co[3] = true;
                    if (co[0] && co[1] && co[2] && co[3]) break;
                }
            }
            else
            {
                var dt = dtDanhSachGoc;
                if (dt == null) return co;
                var ix = new int[4];
                for (int k = 0; k < 4; k++) ix[k] = dt.Columns.IndexOf($"TS_Loai{k + 1}");
                foreach (DataRow r in dt.Rows)
                {
                    for (int k = 0; k < 4; k++)
                        if (!co[k] && ix[k] >= 0 && Co(r[ix[k]]?.ToString())) co[k] = true;
                    if (co[0] && co[1] && co[2] && co[3]) break;
                }
            }
            return co;
        }
        // Trả từng dòng cho InsertData (chuỗi rỗng -> null để ô thật sự trống)
        private static IEnumerable<object?[]> LayCacDongDuLieu(object[,] a, int rows, int cols)
        {
            for (int r = 0; r < rows; r++)
            {
                var row = new object?[cols];
                for (int c = 0; c < cols; c++)
                {
                    var v = a[r, c];
                    row[c] = (v is string s && s.Length == 0) ? null : v;
                }
                yield return row;
            }
        }
        // =====================================================================
        //  NHẬP TỪ EXCEL
        // =====================================================================
        private async void nhapDuLieuTuTepExcel_ExcelToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (_dangXuLyLuongNen == 1) return;
            DialogResult kq;
            using (Form24_XacMinhAdmin frm = new Form24_XacMinhAdmin())
            {
                frm.TopMost = true;
                frm.StartPosition = FormStartPosition.CenterScreen;
                kq = frm.ShowDialog();
            }
            if (kq != DialogResult.OK) return;
            using OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Chọn tệp Excel dữ liệu thi đua",
                Filter = "Excel Files (*.xlsx, *.xlsm)|*.xlsx;*.xlsm",
                Multiselect = false
            };
            if (ofd.ShowDialog() != DialogResult.OK) return;
            string filePath = ofd.FileName;
            bool laTanBinh = LaTanBinhHienTai();
            string tenBang = (laTanBinh ? Module_CheDoDoiTuong.BangThiDuaThang : Module_CheDoDoiTuong.ThiDuaThang(CheDoDoiTuong.CBCS));
            string[] tatCaCotDb = laTanBinh
                ? new[] { "ID", "HoVaTen", "SoHieu", "DonVi", "TinhTrang" }.Concat(Module_LichXet.CotVatLy(Module_CheDoDoiTuong.Hien))
                    .Concat(new[] { "TS_Loai1", "TS_Loai2", "TS_Loai3", "TS_Loai4" }).ToArray()
                : new string[] { "ID", "HoVaTen", "SoHieu", "DonVi", "TinhTrang", "KQ_ThiDua_Nam_Cu", "KQ_XepLoaiCB_Nam_Cu", "KQ_XepLoaiDangVien_Nam_Cu", "Thang_12_Nam_Cu", "Thang_1", "Thang_2", "Thang_3", "Thang_4", "Thang_5", "Sau_Thang_Dau_Nam", "Thang_6", "Thang_7", "Thang_8", "Thang_9", "Thang_10", "Thang_11", "TongKet_Nam", "TS_Loai1", "TS_Loai2", "TS_Loai3", "TS_Loai4" };
            int namCu = Module_HeThong.LayNamHeThong() - 1;
            var headerMap = new Dictionary<string, string>
            {
                ["HoVaTen"] = Module_HeThong.TieuDeCot.HoVaTen,
                ["SoHieu"] = Module_HeThong.TieuDeCot.SoHieu,
                ["DonVi"] = Module_HeThong.TieuDeCot.DonVi,
                ["TinhTrang"] = "Tình trạng",
                ["TS_Loai1"] = $"Tổng\n{Module_HeThong.Loai_1}",
                ["TS_Loai2"] = $"Tổng\n{Module_HeThong.Loai_2}",
                ["TS_Loai3"] = $"Tổng\n{Module_HeThong.Loai_3}",
                ["TS_Loai4"] = $"Tổng\n{Module_HeThong.Loai_4}"
            };
            if (laTanBinh)
            {
                foreach (string cot in Module_LichXet.CotVatLy(Module_CheDoDoiTuong.Hien))
                    headerMap[cot] = Module_LichXet.NhanCot(Module_CheDoDoiTuong.Hien, cot) ?? cot;
            }
            else
            {
                headerMap["KQ_ThiDua_Nam_Cu"] = $"KQ thi đua\nnăm {namCu}";
                headerMap["KQ_XepLoaiCB_Nam_Cu"] = $"Xếp loại CBCS\nnăm {namCu}";
                headerMap["KQ_XepLoaiDangVien_Nam_Cu"] = $"XL đảng viên\nnăm {namCu}";
                headerMap["Thang_12_Nam_Cu"] = $"Tháng 12\n{namCu}";
                headerMap["Sau_Thang_Dau_Nam"] = "6 tháng\nđầu năm";
                headerMap["TongKet_Nam"] = "Tổng kết\nnăm";
                for (int i = 1; i <= 11; i++) headerMap[$"Thang_{i}"] = $"Tháng {i}";
            }
            string ChuanHoa(string input) => input.Replace("\r", "").Replace("\n", "").Replace(" ", "").ToLower();
            string LayTenHeaderMongMuon(string colName)
            {
                if (colName.Equals("ID", StringComparison.OrdinalIgnoreCase)) return "ID";
                if (headerMap.TryGetValue(colName, out string? vnName)) return vnName;
                return colName.Replace("_", " ");
            }
            List<string> danhSachCotTrenUI = kryptonDataGridView1.Columns
                .Cast<DataGridViewColumn>()
                .Select(c => c.Name)
                .ToList();
            Form_Loading frmLoad = new Form_Loading("Đang kiểm tra tính hợp lệ và nạp dữ liệu...");
            bool isLoadShown = false;
            void DongFormLoadingAnToan()
            {
                if (isLoadShown)
                {
                    frmLoad.Close();
                    this.Enabled = true;
                    this.Focus();
                    isLoadShown = false; // chỉ đóng 1 lần
                }
            }
            try
            {
                this.Enabled = false;
                frmLoad.Show(this);
                isLoadShown = true;
                int importedRows = 0;
                bool chiNapCotTrenUI = false;
                bool coDuLieuKhongHopLeTanBinh = false;
                await Task.Run(() =>
                {
                    using var wb = new XLWorkbook(filePath);
                    if (!wb.Worksheets.TryGetWorksheet("ThongKeThiDua", out IXLWorksheet? ws))
                        throw new Exception("Không tìm thấy Sheet 'ThongKeThiDua'.");
                    int lastExcelCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
                    Dictionary<int, string> mappingCot = new Dictionary<int, string>();
                    int excelHeaderRow = -1;
                    for (int rowTest = 1; rowTest <= 10; rowTest++)
                    {
                        mappingCot.Clear();
                        for (int colExcel = 1; colExcel <= lastExcelCol; colExcel++)
                        {
                            string rawExcelHeader = ws.Cell(rowTest, colExcel).GetString();
                            string cleanExcel = ChuanHoa(rawExcelHeader);
                            if (cleanExcel == "stt") continue;
                            foreach (string dbColName in tatCaCotDb)
                            {
                                string expectedHeader = LayTenHeaderMongMuon(dbColName);
                                if (cleanExcel == ChuanHoa(expectedHeader))
                                {
                                    mappingCot[colExcel] = dbColName;
                                    break;
                                }
                            }
                        }
                        if (mappingCot.Count >= 5)
                        {
                            excelHeaderRow = rowTest;
                            break;
                        }
                    }
                    if (excelHeaderRow == -1 || mappingCot.Count == 0)
                        throw new Exception("Không tìm thấy cấu trúc bảng hợp lệ. Tệp này không khớp với cả phiên bản cũ và mới.");
                    if (excelHeaderRow == 1)
                    {
                        chiNapCotTrenUI = true;
                    }
                    else
                    {
                        bool hasValidVersion = false;
                        string decryptedVersionText = "";
                        // ƯU TIÊN 1: chuẩn mới (ô B9 sheet THONG_TIN)
                        if (wb.Worksheets.TryGetWorksheet("THONG_TIN", out IXLWorksheet? wsThongTin))
                        {
                            string encryptedV2 = wsThongTin.Cell("B9").GetString().Trim();
                            if (!string.IsNullOrEmpty(encryptedV2))
                            {
                                try
                                {
                                    decryptedVersionText = Module_BaoMatAES.GiaiMa(encryptedV2);
                                    hasValidVersion = true;
                                }
                                catch { }
                            }
                        }
                        // ƯU TIÊN 2: chuẩn cũ (sheet "Phiên bản")
                        if (!hasValidVersion && wb.Worksheets.TryGetWorksheet("Phiên bản", out IXLWorksheet? wsVersion))
                        {
                            string encryptedV1 = wsVersion.Cell("B1").GetString().Trim();
                            if (!string.IsNullOrEmpty(encryptedV1))
                            {
                                try
                                {
                                    decryptedVersionText = Module_BaoMatAES.GiaiMa(encryptedV1);
                                    hasValidVersion = true;
                                }
                                catch { }
                            }
                        }
                        if (hasValidVersion)
                        {
                            bool isExcelCBCS = decryptedVersionText.Contains("CBCS", StringComparison.OrdinalIgnoreCase);
                            // Tân binh và Học viên là 2 bộ dữ liệu riêng: tệp của chế độ này không được nạp vào chế độ kia
                            if (laTanBinh && decryptedVersionText.LayCheDo() != Module_CheDoDoiTuong.Hien) throw new Exception($"Bạn đang cố nạp sai File! Tệp Excel này là: {decryptedVersionText}");
                            if (!laTanBinh && !isExcelCBCS) throw new Exception($"Bạn đang cố nạp sai File! Tệp Excel này là: {decryptedVersionText}");
                        }
                        else
                        {
                            // Excel bị xóa các sheet bảo vệ -> lùi về chế độ nạp tương thích (quét tên cột)
                            chiNapCotTrenUI = true;
                        }
                    }
                    if (chiNapCotTrenUI)
                    {
                        var tempMapping = new Dictionary<int, string>();
                        foreach (var kvp in mappingCot)
                        {
                            if (danhSachCotTrenUI.Contains(kvp.Value, StringComparer.OrdinalIgnoreCase))
                                tempMapping.Add(kvp.Key, kvp.Value);
                        }
                        mappingCot = tempMapping;
                        if (mappingCot.Count == 0)
                            throw new Exception("File Excel không chứa dữ liệu của các cột đang hiển thị trên màn hình hiện tại.");
                    }
                    using var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path));
                    cn.Open();
                    using (var cmdPragma = new SqliteCommand("PRAGMA foreign_keys = OFF;", cn)) { cmdPragma.ExecuteNonQuery(); }
                    using var tran = cn.BeginTransaction();
                    try
                    {
                        using (var cmdDelete = new SqliteCommand($"DELETE FROM {tenBang}", cn, tran)) { cmdDelete.ExecuteNonQuery(); }
                        using (var cmdResetSeq = new SqliteCommand($"DELETE FROM sqlite_sequence WHERE name='{tenBang}';", cn, tran)) { cmdResetSeq.ExecuteNonQuery(); }
                        var thucTeCoTrongExcel = mappingCot.Values.ToList();
                        string columnsString = string.Join(", ", thucTeCoTrongExcel);
                        string paramsString = string.Join(", ", thucTeCoTrongExcel.Select(c => "@" + c));
                        string insertQuery = $"INSERT INTO {tenBang} ({columnsString}) VALUES ({paramsString})";
                        using (var cmdInsert = new SqliteCommand(insertQuery, cn, tran))
                        {
                            foreach (var col in thucTeCoTrongExcel)
                                cmdInsert.Parameters.Add(new SqliteParameter("@" + col, DBNull.Value));
                            int lastRow = ws.LastRowUsed()?.RowNumber() ?? excelHeaderRow;
                            int dongTrongLienTiep = 0;
                            var hoTenEntry = mappingCot.FirstOrDefault(x => x.Value.Equals("HoVaTen", StringComparison.OrdinalIgnoreCase));
                            for (int r = excelHeaderRow + 1; r <= lastRow; r++)
                            {
                                string oDauTien = ws.Cell(r, 1).GetString().Trim();
                                if (oDauTien.StartsWith("Tổng cộng:", StringComparison.OrdinalIgnoreCase)) break;
                                bool hoTenTrong = hoTenEntry.Key != 0 && string.IsNullOrWhiteSpace(ws.Cell(r, hoTenEntry.Key).GetString());
                                if (string.IsNullOrWhiteSpace(oDauTien) && hoTenTrong)
                                {
                                    dongTrongLienTiep++;
                                    if (dongTrongLienTiep >= 5) break;
                                    continue;
                                }
                                dongTrongLienTiep = 0;
                                if (hoTenTrong) continue;
                                foreach (var kvp in mappingCot)
                                {
                                    int excelColIndex = kvp.Key;
                                    string dbColName = kvp.Value;
                                    string cellValue = ws.Cell(r, excelColIndex).GetString().Trim();
                                    if (laTanBinh && (dbColName.StartsWith("Tuan_", StringComparison.OrdinalIgnoreCase) || dbColName.StartsWith("Thang_", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        if (!string.IsNullOrEmpty(cellValue) && cellValue != "1" && cellValue != "2" && cellValue != "3" && cellValue != "4")
                                        {
                                            cellValue = "";
                                            coDuLieuKhongHopLeTanBinh = true;
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(cellValue) &&
                                        (dbColName.Equals("HoVaTen", StringComparison.OrdinalIgnoreCase) ||
                                         dbColName.Equals("SoHieu", StringComparison.OrdinalIgnoreCase) ||
                                         dbColName.Equals("DonVi", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        if (!Module_BaoMatAES.LaChuoiMa(cellValue))
                                            cellValue = Module_BaoMatAES.MaHoa(cellValue);
                                    }
                                    cmdInsert.Parameters["@" + dbColName].Value = string.IsNullOrEmpty(cellValue) ? DBNull.Value : (object)cellValue;
                                }
                                cmdInsert.ExecuteNonQuery();
                                importedRows++;
                            }
                        }
                        tran.Commit();
                        using (var cmdVacuum = new SqliteCommand("VACUUM;", cn)) { cmdVacuum.ExecuteNonQuery(); }
                    }
                    catch (Exception exTran)
                    {
                        tran.Rollback();
                        throw new Exception("Lỗi khi ghi dữ liệu vào SQLite: " + exTran.Message);
                    }
                });
                await Module_XuatNhapDuLieuThiDua.KiemTraVaNapDuLieuTapTheAsync(filePath, _csdl4Path);
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Nạp dữ liệu từ Excel", $"Đã nạp {importedRows} dòng vào bảng {tenBang}");
                await DieuPhoiLoadDuLieuAsync(laTanBinh);
                await NapLaiBoLocVaLocAsync(datLaiTinhTrang: false);
                // Đóng form loading TRƯỚC khi hiển thị thông báo
                DongFormLoadingAnToan();
                if (coDuLieuKhongHopLeTanBinh)
                {
                    MessageBox.Show($"Phát hiện dữ liệu không hợp lệ tại các cột Đánh giá của {Module_TaiKhoan.TenTB_Hoa} (chỉ chấp nhận 1, 2, 3, 4 hoặc rỗng).\n\nHệ thống đã tự động dọn dẹp và chuyển các ô nhập sai thành rỗng!", "Cảnh báo dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                string thongBao = chiNapCotTrenUI
                    ? $"Nạp thành công {importedRows} dòng!\n(Chế độ tương thích: Đã lọc bỏ các cột không sử dụng)"
                    : $"Nạp thành công {importedRows} dòng dữ liệu từ Excel vào phần mềm!";
                Module_ThongBao.ThanhCong(thongBao);
            }
            catch (Exception ex)
            {
                DongFormLoadingAnToan(); // đóng loading trước khi báo lỗi (an toàn khi gọi lặp)
                MessageBox.Show(ex.Message, "Lỗi Nạp Dữ Liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                DongFormLoadingAnToan();
            }
        }
        private async void xoaTatCaDuLieu_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            // 0. Kiểm tra có dữ liệu để xóa hay không trước khi gọi form
            try
            {
                if (!File.Exists(_csdl4Path))
                {
                    MessageBox.Show("Không tìm thấy tệp cơ sở dữ liệu!", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                bool laTanBinh = HienThiTanBinh;
                string tenBang = (laTanBinh ? Module_CheDoDoiTuong.BangThiDuaThang : Module_CheDoDoiTuong.ThiDuaThang(CheDoDoiTuong.CBCS));
                int tongSoDong = 0;
                using (var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path)))
                {
                    conn.Open();
                    using var cmd = new SqliteCommand($"SELECT COUNT(*) FROM {tenBang}", conn);
                    tongSoDong = Convert.ToInt32(cmd.ExecuteScalar());
                }
                if (tongSoDong == 0)
                {
                    MessageBox.Show("Hiện tại không có dữ liệu thống kê nào để xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kiểm tra dữ liệu trước khi xóa: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // 1. Xác minh quyền Admin
            using (Form24_XacMinhAdmin frmXacMinh = new Form24_XacMinhAdmin())
            {
                frmXacMinh.TopMost = true;
                frmXacMinh.StartPosition = FormStartPosition.CenterScreen;
                if (frmXacMinh.ShowDialog() != DialogResult.OK) return;
            }
            // 2. Form xóa nâng cao (Form 37): truyền chính Form15 để gọi lại ReloadData, ApplyFilter...
            using (Form37_XoaThongKeNangCao frm37 = new Form37_XoaThongKeNangCao(this))
            {
                frm37.StartPosition = FormStartPosition.CenterParent;
                frm37.ShowDialog();
            }
            await Task.CompletedTask;
        }
        // =====================================================================
        //  LƯU TRỮ THEO NĂM
        // =====================================================================
                //// Chưa tồn tại -> khởi tạo mới
                //// Đã tồn tại
                //f46.LoadDanhSachFileLichSu();
                //f46.BringToFront();
                //formCha.CapNhatTieuDe(f46.Text);
            //}
        //}
        private async void taoBanSaoLuuTruTheoNamToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            // 1. Xác minh quyền Admin
            DialogResult kq;
            using (Form24_XacMinhAdmin frm = new Form24_XacMinhAdmin())
            {
                frm.TopMost = true;
                frm.StartPosition = FormStartPosition.CenterScreen;
                kq = frm.ShowDialog();
            }
            if (kq != DialogResult.OK) return;
            // 2. Kiểm tra tệp lưu trữ cũ: ĐÚNG tệp thi đua của năm đang lưu (ThiDua_..._Nam<năm>.db).
            //    Trước đây lấy tệp đầu tiên của danh sách "*.db" => có thể trúng tệp Khen thưởng hoặc tệp của năm khác và xóa nhầm.
            string? tepCu = Module_HoTroLuuDataTheoNamCu.DuongDanTepThiDuaNamHienTai();
            bool xoaTepCu = false;
            if (!string.IsNullOrWhiteSpace(tepCu) && File.Exists(tepCu))
            {
                DialogResult hoiCapNhat = MessageBox.Show(
                    "Hệ thống đã tồn tại tệp lưu trữ dữ liệu thi đua năm cũ.\n\n" +
                    "Bạn có muốn cập nhật (ghi đè) tệp lưu trữ này không?",
                    "Tệp lưu trữ đã tồn tại", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (hoiCapNhat != DialogResult.Yes) return;
                DialogResult hoiXoa = MessageBox.Show(
                    "Bạn có muốn xóa tệp lưu trữ cũ trước khi tạo bản mới không?\n\n" +
                    "Có    : Xóa tệp cũ rồi tạo bản mới.\n" +
                    "Không : Giữ tệp cũ và để hệ thống cập nhật lại tệp.",
                    "Xử lý tệp cũ", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                xoaTepCu = (hoiXoa == DialogResult.Yes);
            }
            // 3. Khóa menu
            var menu = taoBanSaoLuuTruTheoNamToolStripMenuItem;
            string? textGoc = menu.Text;
            try
            {
                menu.Enabled = false;
                menu.Text = "Đang lưu trữ...";
                // 4. Xóa tệp cũ (xử lý lỗi truy cập file an toàn)
                if (xoaTepCu && !string.IsNullOrWhiteSpace(tepCu) && File.Exists(tepCu))
                {
                    try
                    {
                        SqliteConnection.ClearAllPools();   // xóa mọi kết nối SQLite đang giữ trong Pool
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        await Task.Delay(200);              // chờ Windows nhả hẳn tiến trình giữ file
                        File.Delete(tepCu);
                    }
                    catch (IOException)
                    {
                        MessageBox.Show("Không thể xóa tệp vì tệp đang được mở hoặc sử dụng bởi một tiến trình khác.\n\nVui lòng đóng mọi kết nối hoặc tắt các tab thống kê đang mở trước khi thao tác.",
                            "Lỗi xóa tệp", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        MessageBox.Show("Không có quyền xóa tệp này. Vui lòng chạy phần mềm bằng quyền Quản trị viên (Run as Administrator) hoặc kiểm tra lại thư mục.",
                            "Lỗi quyền truy cập", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    catch (Exception exXoa)
                    {
                        MessageBox.Show($"Lỗi không xác định khi xóa tệp cũ: {exXoa.Message}",
                            "Lỗi xóa tệp", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                // 5. Thực hiện lưu trữ ("Không" xóa tệp cũ => cập nhật lại các bảng thi đua ngay trong tệp đang có)
                bool capNhatTepDaCo = !xoaTepCu && !string.IsNullOrWhiteSpace(tepCu) && File.Exists(tepCu);
                if (capNhatTepDaCo) SqliteConnection.ClearAllPools();   // nhả kết nối đang giữ tệp (Form46/59...) trước khi thay bảng
                string duongDan = await Task.Run(() => Module_HoTroLuuDataTheoNamCu.LuuTruDuLieuThiDuaNam(capNhatTepDaCo));
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Lưu trữ dữ liệu thi đua năm sang hệ thống năm cũ",
                    $"Tệp: {Path.GetFileName(duongDan)}" + (xoaTepCu ? " (đã xóa tệp cũ và tạo lại)" : ""));
                // 6. Form46 đang mở -> nạp lại danh sách file
                var f46Check = Application.OpenForms.OfType<Form46_ThongKeThiDuaNamCu>().FirstOrDefault();
                if (f46Check != null && !f46Check.IsDisposed)
                    f46Check.LoadDanhSachFileLichSu();
                // 7. Thông báo thành công
                DialogResult hoiDongBo = MessageBox.Show(
                    $"Đã lưu trữ dữ liệu thành công sang Hệ thống lưu trữ " +
                    $"dữ liệu thi đua năm cũ.\n\n" +
                    $"Bạn có muốn cập nhật kết quả Tổng kết năm " +
                    $"{Module_HeThong.LayNamHeThong() - 1} " +
                    $"và thống kê cột \"Kết quả thi đua năm cũ\" " +
                    $"trên Trang Thống kê không?",
                    "Lưu trữ dữ liệu thành công", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (hoiDongBo == DialogResult.Yes)
                    dongBoKetQuaThiDuaNamTruocVeHienTaiToolStripMenuItem_Click(null, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Lỗi lưu trữ dữ liệu thi đua năm", ex.Message);
                MessageBox.Show(ex.Message, "Không thể lưu trữ dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 8. Khôi phục menu
                menu.Enabled = true;
                menu.Text = textGoc;
            }
        }
        private void dongBoKetQuaThiDuaNamTruocVeHienTaiToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (!Module_HoTroLuuDataTheoNamCu.LaPhienBanCBCS()) return; // bảng năm cũ (KQ_ThiDua_Nam_Cu) chỉ có ở CBCS
            try
            {
                using var frm = new Form58_DongBoKetQuaThiDuaNamTruocVeNamHienTai { ShowInTaskbar = false };
                frm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chi tiết lỗi: " + ex.Message, "Lỗi Giao Diện", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}