///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using ClosedXML.Excel;
using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form34_ThongKeKhenThuong : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private readonly string _csdl4Path = Module_DanduongGPS.DuongDanCSDL4;
        private ToolStripMenuItem? _mnuChonTatCa;
        private ToolStripMenuItem? _mnuXoaDongDaChon;
        private bool _dangXoaNhieu;
        // Biến phục vụ chức năng lọc mượt mà (Debounce)
        private System.Windows.Forms.Timer timKiemTimer;
        public bool DaLoadDuLieu { get; private set; } = false; // Đường dẫn CSDL
        // Biến lưu trữ dữ liệu gốc để lọc trên RAM  
        private bool isUpdatingCombo = false;
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lParam);
        private const int WM_SETREDRAW = 11;
        // 🔥 THÊM BIẾN CACHE HÌNH ẢNH Ở ĐÂY
        private readonly Image _iconKhenThuong = Properties.Resources.ic_khenthuong;
        // Đặt class này trong namespace hoặc ngay trong class Form34_ThongKeKhenThuong
        private int _namHienTaiCache = -1;
        private Font _fontBold;
        private bool _gridDaDinhDang;               // định dạng lưới chỉ làm 1 lần
        private bool _canTaiLai;                    // có yêu cầu reload đến trong lúc đang tải
        private Task? _taiHienTai;                  // tác vụ tải đang chạy (gộp các lần gọi chồng)
        private CancellationTokenSource? _ctsTai;   // hủy tải khi form đóng
        private Bitmap? _iconNho;                   // icon khen thưởng đã co sẵn đúng kích thước
        private int _iconNhoSize;
        // Chỉ số cột cố định (khớp thứ tự cột tạo trong DinhDangDataGridHienDai)
        private const int COT_HOTEN = 0, COT_SOHIEU = 1, COT_DONVI = 2, COT_TINHTRANG = 3, COT_SOLUONG = 4, COT_GHICHU = 5;
        private sealed record KetQuaTai(List<ThongTinKhenThuongCBCS> DanhSach, List<string> DonViChuan);
        public class ThongTinKhenThuongCBCS
        {
            public long ID { get; set; }
            public string HoVaTen { get; set; }
            public string SoHieu { get; set; }
            public string DonVi { get; set; }
            public string TinhTrang { get; set; }
            public int SoLuong_Khen { get; set; }
            public string GhiChu_Khen { get; set; }
            public string GhiChu_DanhSach { get; set; }
            public string DanhSachDVKhen_An { get; set; }
            public HashSet<string> TapDVKhen { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }
        public class KhenThuongExportDTO
        {
            public int STT { get; set; }
            public string HoVaTen { get; set; }
            public string SoHieu { get; set; }
            public string NamSinh { get; set; }
            public string CapBac { get; set; }
            public string ChucVu { get; set; }
            public string QueQuan { get; set; }
            public string DonVi { get; set; }
            public string TinhTrang { get; set; }
            public string HinhThuc_Khen { get; set; }
            public string QuyetDinh_Khen { get; set; }
            public string NgayCapQD_Khen { get; set; }
            public string DonVi_Khen { get; set; }
            public string VeViec_Khen { get; set; }
            public string GhiChu_Khen { get; set; }
        }
        private List<ThongTinKhenThuongCBCS> _danhSachGoc = new List<ThongTinKhenThuongCBCS>();
        private List<ThongTinKhenThuongCBCS> _danhSachHienThi = new List<ThongTinKhenThuongCBCS>();
        // PHẦN 2 — COMBOBOX: CHỮA TẠM "BẤM 2-3 LẦN MỚI XỔ"
        // Form34 là form con (TopLevel=false) nằm trong PanelContainer của Form2_FormCha, bị Hide()/Show()
        // qua lại. Nếu cú bấm đầu bị nuốt (focus/activation bị chuyển đi), đoạn này ép combo xổ ra.
        // Gọi ở constructor (đã thêm ở PHẦN 1). Đây là workaround, nguyên nhân gốc cần xem Form2/Form52.
        private void DangKyComboXoLanDau(params ComboBox?[] dsCombo)
        {
            foreach (var cb in dsCombo)
            {
                if (cb == null) continue;
                long lucDong = 0;
                cb.DropDownClosed += (s, e) => lucDong = Environment.TickCount64;
                cb.MouseDown += (s, e) =>
                {
                    if (e.Button != MouseButtons.Left || cb.DroppedDown) return;
                    // Vừa đóng bởi chính cú bấm này (bấm vào combo để thu danh sách) -> không mở lại
                    if (Environment.TickCount64 - lucDong < 250) return;
                    cb.BeginInvoke(new Action(() =>
                    {
                        if (cb.IsDisposed || !cb.IsHandleCreated || cb.DroppedDown) return;
                        if (!cb.Focused) cb.Focus();
                        cb.DroppedDown = true;
                    }));
                };
            }
        }
        // ⭐ KỸ THUẬT GIAO TIẾP CROSS-FORM AN TOÀN VÀ HIỆU SUẤT CAO
        public string LayTinhTrangCongTacAnToan(int rowIndex)
        {
            // Kiểm tra tính toàn vẹn của danh sách RAM và Index
            if (_danhSachHienThi == null || rowIndex < 0 || rowIndex >= _danhSachHienThi.Count)
            {
                return "Không xác định";
            }
            // O(1) Access - Trích xuất siêu tốc thẳng từ RAM mà không cần vòng lặp
            return _danhSachHienThi[rowIndex].TinhTrang ?? "Không xác định";
        }
        public Form34_ThongKeKhenThuong()
        {
            InitializeComponent();
            DoubleBuffered = true;
            DangKyBoLoc();
            DangKyComboXoLanDau(comboBox_TimKiemDonVi, comboBox_TinhTrangCongTac, comboBox1_DonViKhenThuong);
            DangKySuKienGrid();
            KhoiTaoXoaNhieu();
            KhoiTaoTimerTimKiem();
            DangKyHieuUngVienTextBox();
            Shown += Form34_ThongKeKhenThuong_Shown;
            Disposed += (_, _) => GiaiPhongTaiNguyen();
        }
        // Giải phóng tài nguyên GDI / Timer / tác vụ nền (trước đây Timer, Font không bao giờ được Dispose)
        private void GiaiPhongTaiNguyen()
        {
            try { _ctsTai?.Cancel(); } catch { }
            try { timKiemTimer?.Stop(); timKiemTimer?.Dispose(); } catch { }
            _fontBold?.Dispose(); _fontBold = null;
            _iconNho?.Dispose(); _iconNho = null;
        }
        /// Đăng ký sự kiện cho các control lọc/tìm kiếm.
        /// LƯU Ý: "x?.Event += Handler;" (null-conditional assignment) chỉ có từ
        /// C# 14 trở lên. Với C# 12, phải dùng "if (x != null) x.Event += ...;".
        /// // ============================================================
        // 1. KHỞI TẠO MENU XÓA NHIỀU VÀ CHỌN TẤT CẢ TRÊN CONTEXTMENUSTRIP
        // ============================================================
        private void KhoiTaoXoaNhieu()
        {
            var grid = kryptonDataGridView1_DanhSachCBCS;
            if (grid == null || contextMenuStrip1 == null) return;
            _mnuChonTatCa = new ToolStripMenuItem("Chọn tất cả các dòng đang hiển thị")
            {
                Name = "chonTatCaCacDong_ToolStripMenuItem",
                ShortcutKeyDisplayString = "Ctrl+A"
            };
            _mnuChonTatCa.Click += (s, e) => ChonTatCaDongDangHienThi();
            _mnuXoaDongDaChon = new ToolStripMenuItem("Xóa các CBCS đã chọn khỏi cơ sở dữ liệu")
            {
                Name = "xoaKhenThuongCacDongDaChon_ToolStripMenuItem",
                ShortcutKeyDisplayString = "Delete",
                Image = xoaToanBoDuLieu_ToolStripMenuItem?.Image // Dùng chung icon thùng rác
            };
            _mnuXoaDongDaChon.Click += MnuXoaDongDaChon_Click;
            var items = contextMenuStrip1.Items;
            // "Chọn tất cả" nằm ngay dưới "Xóa tìm kiếm"
            int viTriTimKiem = xoaTimKiem_ToolStripMenuItem != null ? items.IndexOf(xoaTimKiem_ToolStripMenuItem) : -1;
            items.Insert(viTriTimKiem >= 0 ? viTriTimKiem + 1 : 0, _mnuChonTatCa);
            // "Xóa các dòng đã chọn" nằm ngay trên "Xóa toàn bộ dữ liệu"
            int viTriXoaTatCa = xoaToanBoDuLieu_ToolStripMenuItem != null ? items.IndexOf(xoaToanBoDuLieu_ToolStripMenuItem) : -1;
            if (viTriXoaTatCa >= 0) items.Insert(viTriXoaTatCa, _mnuXoaDongDaChon);
            else items.Add(_mnuXoaDongDaChon);
            contextMenuStrip1.Opening += ContextMenuStrip1_Opening_XoaNhieu;
            grid.KeyDown += Grid_KeyDown_XoaNhieu;
        }
        private void ContextMenuStrip1_Opening_XoaNhieu(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            var grid = kryptonDataGridView1_DanhSachCBCS;
            if (grid == null || grid.IsDisposed || _mnuXoaDongDaChon == null || _mnuChonTatCa == null) return;
            // 🚀 SỬA TẠI ĐÂY: Dùng HashSet để đếm số dòng độc lập đang được chọn qua SelectedCells
            // Cách này chạy cực nhanh, an toàn tuyệt đối và tương thích 100% với KryptonDataGridView ở chế độ VirtualMode.
            int soDong = 0;
            var selectedRowIndices = new HashSet<int>();
            foreach (DataGridViewCell cell in grid.SelectedCells)
            {
                selectedRowIndices.Add(cell.RowIndex);
            }
            soDong = selectedRowIndices.Count;
            _mnuXoaDongDaChon.Enabled = soDong > 0 && !_dangXoaNhieu;
            _mnuXoaDongDaChon.Text = soDong > 0
                ? $"Xóa {soDong:N0} CBCS đã chọn khỏi cơ sở dữ liệu"
                : "Xóa các CBCS đã chọn khỏi cơ sở dữ liệu";
            _mnuChonTatCa.Enabled = _danhSachHienThi != null && _danhSachHienThi.Count > 0;
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
            var grid = kryptonDataGridView1_DanhSachCBCS;
            if (grid == null || grid.IsDisposed || grid.RowCount == 0) return;
            grid.Focus();
            grid.SelectAll();
        }
        private async void MnuXoaDongDaChon_Click(object? sender, EventArgs e)
        {
            await XoaCacDongDaChonAsync();
        }
        // ---- Chọn nhiều dòng: KHÔNG dùng grid.SelectedRows ----
        // SelectedRows tạo (unshare) một DataGridViewRow cho từng dòng được chọn. Chọn tất cả vài nghìn người
        // = vài nghìn đối tượng Row, phình RAM và đứng hình. GetRowState / GetRowCount không tạo đối tượng.
        private HashSet<string> LaySoHieuCacDongDaChon()
        {
            var ketQua = new HashSet<string>(StringComparer.Ordinal);
            var grid = kryptonDataGridView1_DanhSachCBCS;
            var ds = _danhSachHienThi;
            if (grid == null || grid.IsDisposed || ds == null) return ketQua;
            int n = Math.Min(grid.RowCount, ds.Count);
            for (int i = 0; i < n; i++)
            {
                if ((grid.Rows.GetRowState(i) & DataGridViewElementStates.Selected) == 0) continue;
                string sh = ds[i].SoHieu;
                if (!string.IsNullOrWhiteSpace(sh)) ketQua.Add(sh);
            }
            return ketQua;
        }
        private async Task XoaCacDongDaChonAsync()
        {
            if (_dangXoaNhieu || IsDisposed || Disposing) return;
            var grid = kryptonDataGridView1_DanhSachCBCS;
            if (grid == null || grid.IsDisposed) return;
            try
            {
                if (string.IsNullOrWhiteSpace(_csdl4Path) || !File.Exists(_csdl4Path))
                {
                    MessageBox.Show(this, "Không tìm thấy CSDL khen thưởng (csdl4.db)!", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                HashSet<string> soHieuCanXoa = LaySoHieuCacDongDaChon();
                if (soHieuCanXoa.Count == 0)
                {
                    MessageBox.Show(this,
                        "Vui lòng chọn ít nhất một dòng CBCS cần xóa.",
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var xacNhan = MessageBox.Show(this,
                    $"Bạn sắp xóa vĩnh viễn {soHieuCanXoa.Count:N0} cán bộ, chiến sĩ đã chọn khỏi cơ sở dữ liệu thi đua/khen thưởng.\n\n" +
                    "• Dữ liệu sẽ bị xóa trực tiếp trong CSDL (dù có hoặc không có dữ liệu khen thưởng, ví dụ CBCS đã chuyển đi).\n" +
                    "• Thao tác này không thể hoàn tác.\n\n" +
                    "Bạn có chắc chắn muốn tiếp tục?",
                    "Xác nhận xóa dữ liệu CBCS",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (xacNhan != DialogResult.Yes) return;
                DialogResult kqAdmin;
                using (var frm = new Form24_XacMinhAdmin())
                {
                    frm.TopMost = true;
                    frm.StartPosition = FormStartPosition.CenterScreen;
                    kqAdmin = frm.ShowDialog(this);
                }
                if (kqAdmin != DialogResult.OK) return;
                _dangXoaNhieu = true;
                Cursor.Current = Cursors.WaitCursor;
                if (toolStripProgressBar1_LamMoi != null && !toolStripProgressBar1_LamMoi.IsDisposed)
                {
                    toolStripProgressBar1_LamMoi.AutoSize = false;
                    toolStripProgressBar1_LamMoi.Size = new Size(150, 16);
                    toolStripProgressBar1_LamMoi.Style = ProgressBarStyle.Marquee;
                    toolStripProgressBar1_LamMoi.Visible = true;
                }
                var ketQua = await Task.Run(() => XoaCBCSVaKhenThuongTheoSoHieu(soHieuCanXoa));
                try
                {
                    Module_NhatKy.GhiNhatKy(
                        Module_TaiKhoan.TenTaiKhoan_RAM,
                        "Xóa CBCS hàng loạt từ Form34",
                        $"Xóa {soHieuCanXoa.Count} CBCS (Bảng thi đua: {ketQua.SoDongThiDua}, Thống kê: {ketQua.SoDongThongKe}, Chi tiết: {ketQua.SoDongChiTiet}) lúc {DateTime.Now:HH:mm:ss}");
                }
                catch { }
                if (IsDisposed || Disposing) return;
                await ReloadDuLieu();
                if (!IsDisposed && !Disposing)
                {
                    ClearCacTextBoxChiTiet();
                }
                MessageBox.Show(this,
                    $"Đã xóa thành công {soHieuCanXoa.Count:N0} CBCS khỏi hệ thống!\n" +
                    $"- Bảng thi đua chính: {ketQua.SoDongThiDua:N0} bản ghi.\n" +
                    $"- Bảng thống kê khen: {ketQua.SoDongThongKe:N0} bản ghi.\n" +
                    $"- Bảng chi tiết giấy khen: {ketQua.SoDongChiTiet:N0} bản ghi.",
                    "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi xóa hàng loạt: " + ex);
                if (!IsDisposed && !Disposing)
                {
                    MessageBox.Show(this,
                        "Không thể xóa dữ liệu (đã hoàn tác an toàn):\n" + ex.Message,
                        "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                _dangXoaNhieu = false;
                Cursor.Current = Cursors.Default;
                if (toolStripProgressBar1_LamMoi != null && !toolStripProgressBar1_LamMoi.IsDisposed)
                    toolStripProgressBar1_LamMoi.Visible = false;
            }
        }
        private (int SoDongThiDua, int SoDongThongKe, int SoDongChiTiet) XoaCBCSVaKhenThuongTheoSoHieu(HashSet<string> soHieuCanXoa)
        {
            bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
            string tableThiDua = laTanBinh ? "ThiDuaThang_TanBinh" : "ThiDuaThang";
            using var conn = new SqliteConnection($"Data Source={_csdl4Path}");
            conn.Open();
            using var tran = conn.BeginTransaction();
            try
            {
                int soThiDua = XoaTheoBang(conn, tran, tableThiDua, soHieuCanXoa);
                int soThongKe = XoaTheoBang(conn, tran, "ThongKeCBCS_DuocKhenThuong", soHieuCanXoa);
                int soChiTiet = XoaTheoBang(conn, tran, "ThongKe_GiayKhen", soHieuCanXoa);
                tran.Commit();
                return (soThiDua, soThongKe, soChiTiet);
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }
        private int XoaTheoBang(SqliteConnection conn, SqliteTransaction tran, string tenBang, HashSet<string> soHieuCanXoa)
        {
            if (!TonTaiBang(conn, tran, tenBang)) return 0;
            var rowIds = new List<long>();
            using (var cmdDoc = conn.CreateCommand())
            {
                cmdDoc.Transaction = tran;
                cmdDoc.CommandText = $"SELECT rowid, SoHieu FROM {tenBang};";
                using var rd = cmdDoc.ExecuteReader();
                while (rd.Read())
                {
                    string raw = rd.IsDBNull(1) ? "" : (Convert.ToString(rd.GetValue(1)) ?? "");
                    string sh = SafeDecrypt(raw);
                    if (sh.Length > 0 && soHieuCanXoa.Contains(sh))
                        rowIds.Add(rd.GetInt64(0));
                }
            }
            if (rowIds.Count == 0) return 0;
            using var cmdXoa = conn.CreateCommand();
            cmdXoa.Transaction = tran;
            cmdXoa.CommandText = $"DELETE FROM {tenBang} WHERE rowid = $id;";
            var pId = cmdXoa.Parameters.Add("$id", SqliteType.Integer);
            int tong = 0;
            foreach (long id in rowIds)
            {
                pId.Value = id;
                tong += cmdXoa.ExecuteNonQuery();
            }
            return tong;
        }
        private static bool TonTaiBang(SqliteConnection conn, SqliteTransaction tran, string tenBang)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tran;
            cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $ten LIMIT 1;";
            cmd.Parameters.AddWithValue("$ten", tenBang);
            return cmd.ExecuteScalar() != null;
        }
        private void DangKyBoLoc()
        {
            if (textBox_TimKiemTheoTen != null)
                textBox_TimKiemTheoTen.TextChanged += TextBox_TimKiemTheoTen_TextChanged;
            if (comboBox_TimKiemDonVi != null)
                comboBox_TimKiemDonVi.SelectedIndexChanged += BoLoc_ValueChanged;
            if (comboBox_TinhTrangCongTac != null)
                comboBox_TinhTrangCongTac.SelectedIndexChanged += BoLoc_ValueChanged;
            if (comboBox1_DonViKhenThuong != null)
                comboBox1_DonViKhenThuong.SelectedIndexChanged += BoLoc_ValueChanged;
        }
        /// Đăng ký toàn bộ sự kiện + cấu hình liên quan tới DataGridView chính.
        /// Tách riêng để dễ đọc, dễ bảo trì khi cần thêm/bớt sự kiện của grid.
        private void DangKySuKienGrid()
        {
            var grid = kryptonDataGridView1_DanhSachCBCS;
            if (grid == null) return;
            grid.CellClick += KryptonDataGridView1_DanhSachCBCS_CellClick;
            grid.RowPostPaint += KryptonDataGridView1_DanhSachCBCS_RowPostPaint;
            grid.CellDoubleClick += KryptonDataGridView1_DanhSachCBCS_CellDoubleClick;
            grid.ContextMenuStrip = contextMenuStrip1;
            grid.MouseDown += KryptonDataGridView1_DanhSachCBCS_MouseDown;
            // Đã bỏ dòng "-=" trước "+=" của bản gốc: trong constructor, sự kiện
            // CellPainting CHƯA TỪNG được đăng ký, nên "-=" là code chết, không
            // có tác dụng gì ngoài gây hiểu nhầm khi đọc lại code.
            grid.CellPainting += KryptonDataGridView1_DanhSachCBCS_CellPainting;
        }
        private void KhoiTaoTimerTimKiem()
        {
            timKiemTimer = new System.Windows.Forms.Timer { Interval = 300 };
            timKiemTimer.Tick += TimKiemTimer_Tick;
        }
        // Trong Dispose(bool disposing) của Form, nhớ giải phóng Timer để tránh rò rỉ handle:
        // if (disposing) timKiemTimer?.Dispose();
        private void TextBox_TimKiemTheoTen_TextChanged(object? sender, EventArgs e)
        {
            // Reset lại timer mỗi khi người dùng gõ phím
            if (timKiemTimer != null)
            {
                timKiemTimer.Stop();
                timKiemTimer.Start();
            }
        }
        private void TimKiemTimer_Tick(object? sender, EventArgs e)
        {
            timKiemTimer.Stop();
            ApDungBoLoc();
        }
        // PHẦN 3 — LOAD FORM / TIMER DEBOUNCE
        private async void Form34_ThongKeKhenThuong_Load(object? sender, EventArgs e)
        {
            // async void: bắt hết lỗi, nếu không một exception sẽ làm sập cả phần mềm
            try
            {
                Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1);
                CauHinhStatusStrip();
                LoadComboBoxTinhTrang();
                KhoaCacTextBox();
                InitToolTips();              // trước đây nằm SAU await -> tooltip chỉ có khi tải xong
                DinhDangDataGridHienDai();   // 1 lần duy nhất (hàm tự chặn gọi lại)
                await ReloadDuLieu();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Form34_Load] " + ex);
            }
        }
        private void Form34_ThongKeKhenThuong_Shown(object? sender, EventArgs e)
        {
            textBox_TimKiemTheoTen.Focus();
        }
        private void InitToolTips()
        {
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
                GanToolTipAnToan(
                    kryptonButton1_QuanLyKhenThuongTapThe,
                    "Mở quản lý danh sách khen thưởng tập thể");
                GanToolTipAnToan(
                    kryptonButton_LamMoiCacOTimKiem,
                    "Làm mới (xóa) bộ lọc tìm kiếm hiện tại");
                GanToolTipAnToan(
                    kryptonButton_GoiFromChiTietKhenThuong,
                    "Xem chi tiết thông tin khen thưởng");
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
        // Thiết lập chế độ Chỉ Đọc và đổ màu xanh nhạt nhận diện bị khóa
        private void KhoaCacTextBox()
        {
            var mauXanhNhat = Color.FromArgb(230, 255, 230); // Xanh pastel nhạt dịu mắt
            Krypton.Toolkit.KryptonTextBox[] danhSachKhoa =
            {
                kryptonTextBox1_STT,
                kryptonTextBox1_HoVaTen,
                kryptonTextBox1_SoHieu,
                kryptonTextBox1_DonVi,
                kryptonTextBox1_TinhTrang,
                kryptonTextBox1_SoLuong
            };
            foreach (var ktb in danhSachKhoa)
            {
                if (ktb == null) continue;
                ktb.ReadOnly = true;
                ktb.StateCommon.Back.Color1 = mauXanhNhat;
            }
        }
        private void GanToolTipAnToan(Control control, string noiDung)
        {
            // 1. Control không tồn tại
            if (control == null)
                return;
            // 2. Control đã được giải phóng hoặc đang giải phóng
            if (control.IsDisposed || control.Disposing)
                return;
            // 3. Nội dung Tooltip không hợp lệ
            if (string.IsNullOrWhiteSpace(noiDung))
                return;
            // 4. ToolTip chưa được khởi tạo
            if (toolTip1 == null)
                return;
            try
            {
                // 5. Gán Tooltip
                toolTip1.SetToolTip(control, noiDung);
            }
            catch (ObjectDisposedException)
            {
                // Control đã bị giải phóng đúng thời điểm thao tác.
            }
            catch (InvalidOperationException)
            {
                // Control đang ở trạng thái không phù hợp.
            }
        }
        // Thiết lập chế độ Chỉ Đọc (Không cho phép sửa chữa)
        // HÀM BẢO VỆ DELEGATE (CHỐNG CRASH) 
        private Action SafeAction(Action action)
        {
            return () =>
            {
                if (action == null) return;
                try { action.Invoke(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Lỗi thực thi phím tắt: " + ex.Message); }
            };
        }
        private bool SafeExecute(Action action)
        {
            SafeAction(action).Invoke();
            return true;
        }
        // BẮT PHÍM TẮT TRÊN FORM
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            try
            {
                // 1. GỌI MODULE PHÍM TẮT CHUNG 
                if (Module_PhimTat.XuLy(
                    keyData,
                    actionLamMoi: SafeAction(() => lamMoi_ToolStripMenuItem.PerformClick()), // F5
                    actionLuu_TinhToan: SafeAction(() => capNhatThongTinKhenThuong_ToolStripMenuItem.PerformClick()) // Ctrl + S
                ))
                {
                    return true;
                }
                // 2. PHÍM TẮT ĐẶC THÙ RIÊNG CỦA FORM NÀY
                Keys key = keyData & Keys.KeyCode;
                Keys modifier = keyData & Keys.Modifiers;
                if (modifier == Keys.None)
                {
                    switch (key)
                    {
                        case Keys.F4: // F4: Xóa tìm kiếm
                            return SafeExecute(() => xoaTimKiem_ToolStripMenuItem.PerformClick());
                    }
                }
                else if (modifier == Keys.Control)
                {
                    switch (key)
                    {
                        case Keys.P: // Ctrl + P: Phân tích khen thưởng
                            return SafeExecute(() => phanTichKhenThuong_ToolStripMenuItem.PerformClick());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi phím tắt: " + ex.Message);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        private string SafeDecrypt(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            input = input.Trim();
            string decoded = Module_BaoMatAES.GiaiMa(input);
            // Nếu giải mã thành công
            if (!string.IsNullOrEmpty(decoded)) return decoded;
            // Nếu thất bại (trả về rỗng) mà có mác v2| thì báo lỗi
            if (input.StartsWith("v2|")) return "[LỖI GIẢI MÃ]";
            // Nếu không có mác v2|, có thể là dữ liệu thô cũ
            return input;
        }
        public Task ReloadDuLieu()
        {
            if (IsDisposed || Disposing) return Task.CompletedTask;
            if (_taiHienTai is { IsCompleted: false })
            {
                _canTaiLai = true;
                return _taiHienTai;
            }
            _taiHienTai = ReloadVongLapAsync();
            return _taiHienTai;
        }
        /// Gọi trên luồng UI. Nếu đang tải thì KHÔNG chạy chồng: đánh dấu "cần tải lại" và chờ chính tác vụ đang chạy.
        /// (Trước đây cờ chặn là isUpdatingCombo nên trong lúc await vẫn chạy chồng được.)
        private async Task ReloadVongLapAsync()
        {
            do
            {
                _canTaiLai = false;
                await TaiVaApDungMotLanAsync();
            }
            while (_canTaiLai && !IsDisposed && !Disposing);
        }
        private async Task TaiVaApDungMotLanAsync()
        {
            var cts = new CancellationTokenSource();
            _ctsTai = cts;
            HienTienTrinh(true);
            try
            {
                // Lưới KHÔNG bị đóng băng trong lúc đọc DB (trước đây đóng băng suốt thời gian await)
                KetQuaTai kq = await Task.Run(() => TaiDanhSachTuCSDL(cts.Token), cts.Token);
                if (IsDisposed || Disposing || cts.IsCancellationRequested) return;
                ApDungDuLieuMoi(kq);
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi tải Form 34: " + ex);
                if (!IsDisposed && !Disposing)
                    MessageBox.Show(this, "Lỗi tải dữ liệu: " + ex.Message, "Lỗi hệ thống",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                HienTienTrinh(false);
                if (ReferenceEquals(_ctsTai, cts)) _ctsTai = null;
                cts.Dispose();
            }
        }
        private void HienTienTrinh(bool hien)
        {
            var pb = toolStripProgressBar1_LamMoi;
            if (pb == null || pb.IsDisposed) return;
            if (hien)
            {
                pb.AutoSize = false;                 // chống phình to bề ngang
                pb.Size = new Size(150, 16);
                pb.Style = ProgressBarStyle.Marquee;
                pb.MarqueeAnimationSpeed = 30;
                pb.Visible = true;
            }
            else
            {
                pb.Style = ProgressBarStyle.Blocks;  // dừng animation marquee
                pb.Visible = false;
            }
        }
        private KetQuaTai TaiDanhSachTuCSDL(CancellationToken ct)
        {
            bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem()
                .Contains("tân binh", StringComparison.OrdinalIgnoreCase);
            string tableThiDua = laTanBinh ? "ThiDuaThang_TanBinh" : "ThiDuaThang";
            // 1. Danh sách gốc (CSDL2)
            var dictDanhSach = new Dictionary<string, (string HoTen, string DonVi, string GhiChu)>(StringComparer.Ordinal);
            if (File.Exists(_csdl2Path))
            {
                using var cn2 = new SqliteConnection($"Data Source={_csdl2Path}");
                cn2.Open();
                using var cmd = new SqliteCommand("SELECT SoHieu, HoVaTen, DonVi, GhiChu FROM DanhSach", cn2);
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    ct.ThrowIfCancellationRequested();
                    string sh = SafeDecrypt(DocChuoi(rd, 0));
                    if (sh.Length == 0) continue;
                    dictDanhSach[sh] = (SafeDecrypt(DocChuoi(rd, 1)),
                                        SafeDecrypt(DocChuoi(rd, 2)),
                                        SafeDecrypt(DocChuoi(rd, 3)));
                }
            }
            // 2. CSDL4: DÙNG CHUNG 1 kết nối (trước đây mở 3 kết nối) và KHÔNG "CREATE TABLE IF NOT EXISTS"
            //    ở luồng đọc. Trước đây form này tạo ThongKe_GiayKhen với schema 2 cột (SoHieu, DonVi_Khen)
            //    trong khi hàm xuất Excel tạo schema đầy đủ -> bảng nào được tạo trước sẽ làm hàm kia lỗi
            //    "no such column". Giờ chỉ kiểm tra tồn tại: bảng chưa có = chưa có dữ liệu.
            using var cn4 = new SqliteConnection($"Data Source={_csdl4Path}");
            cn4.Open();
            var dictKhen = new Dictionary<string, (int SoLuong, string GhiChu)>(StringComparer.Ordinal);
            if (BangTonTai(cn4, "ThongKeCBCS_DuocKhenThuong"))
            {
                using var cmd = new SqliteCommand("SELECT SoHieu, SoLuong_Khen, GhiChu_Khen FROM ThongKeCBCS_DuocKhenThuong", cn4);
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    ct.ThrowIfCancellationRequested();
                    string sh = SafeDecrypt(DocChuoi(rd, 0));
                    if (sh.Length == 0) continue;
                    int.TryParse(DocChuoi(rd, 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int sl);
                    dictKhen[sh] = (sl, DocChuoi(rd, 2));
                }
            }
            var dictDVKhen = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            if (BangTonTai(cn4, "ThongKe_GiayKhen"))
            {
                using var cmd = new SqliteCommand("SELECT SoHieu, DonVi_Khen FROM ThongKe_GiayKhen", cn4);
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    ct.ThrowIfCancellationRequested();
                    string sh = SafeDecrypt(DocChuoi(rd, 0));
                    string dv = SafeDecrypt(DocChuoi(rd, 1)).Trim();
                    if (sh.Length == 0 || dv.Length == 0) continue;
                    if (!dictDVKhen.TryGetValue(sh, out var tap))
                    {
                        tap = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        dictDVKhen[sh] = tap;
                    }
                    tap.Add(dv);
                }
            }
            // 3. Danh sách chính
            var list = new List<ThongTinKhenThuongCBCS>(Math.Max(dictDanhSach.Count, 256));
            if (BangTonTai(cn4, tableThiDua))
            {
                using var cmd = new SqliteCommand($"SELECT ID, HoVaTen, SoHieu, DonVi FROM {tableThiDua}", cn4);
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    ct.ThrowIfCancellationRequested();
                    string sh = SafeDecrypt(DocChuoi(rd, 2));
                    if (sh.Length == 0) continue;
                    bool coGoc = dictDanhSach.TryGetValue(sh, out var goc);
                    dictKhen.TryGetValue(sh, out var khen);
                    dictDVKhen.TryGetValue(sh, out var tapDV);
                    list.Add(new ThongTinKhenThuongCBCS
                    {
                        ID = Convert.ToInt64(rd.GetValue(0), CultureInfo.InvariantCulture),
                        HoVaTen = coGoc ? goc.HoTen : SafeDecrypt(DocChuoi(rd, 1)),
                        SoHieu = sh,
                        DonVi = coGoc ? goc.DonVi : SafeDecrypt(DocChuoi(rd, 3)),
                        // Nội suy: có trong danh sách gốc = đang công tác, ngược lại = chuyển công tác
                        TinhTrang = coGoc ? Module_HeThong.TT_DANG_CONG_TAC : Module_HeThong.TT_CHUYEN_CONG_TAC,
                        SoLuong_Khen = khen.SoLuong,
                        GhiChu_Khen = khen.GhiChu ?? "",
                        GhiChu_DanhSach = coGoc ? goc.GhiChu : "",
                        TapDVKhen = tapDV ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                        DanhSachDVKhen_An = tapDV != null ? string.Join(", ", tapDV) : ""
                    });
                }
            }
            // 4. Sắp xếp theo thứ tự ưu tiên đơn vị.
            //    Dùng TryAdd + OrdinalIgnoreCase: ToDictionary cũ sẽ NÉM LỖI (làm hỏng cả lần tải)
            //    nếu danh sách ưu tiên có 2 đơn vị trùng tên.
            var uuTien = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var dv in Module_DonVi.LayDanhSachDonViUuTienArray())
                if (!string.IsNullOrWhiteSpace(dv)) uuTien.TryAdd(dv, uuTien.Count);
            var daSapXep = list
                .OrderBy(r => uuTien.TryGetValue(r.DonVi ?? "", out int idx) ? idx : int.MaxValue)
                .ThenBy(r => r.ID)
                .ToList();
            // Lấy luôn danh sách đơn vị chuẩn ở luồng nền (trước đây gọi DB ngay trên luồng UI)
            var donViChuan = Module_DonVi.GetDanhSachDonVi() ?? new List<string>();
            return new KetQuaTai(daSapXep, donViChuan);
        }
        //Luồng UI. Đóng băng lưới CHỈ trong đoạn cập nhật ngắn này.</summary>
        private void ApDungDuLieuMoi(KetQuaTai kq)
        {
            var grid = kryptonDataGridView1_DanhSachCBCS;
            if (grid == null || grid.IsDisposed) return;
            _danhSachGoc = kq.DanhSach;
            bool dongBang = grid.IsHandleCreated;
            if (dongBang) SendMessage(grid.Handle, WM_SETREDRAW, 0, null);
            try
            {
                DinhDangDataGridHienDai();            // no-op sau lần đầu (không dựng lại cột mỗi lần reload)
                CapNhatComboBoxTuList(kq.DonViChuan);
                ApDungBoLoc();                        // LUÔN áp lại bộ lọc đang chọn -> lưới và combo luôn khớp nhau
            }
            finally
            {
                if (dongBang && grid.IsHandleCreated) SendMessage(grid.Handle, WM_SETREDRAW, 1, null);
                grid.Invalidate();
            }
            DaLoadDuLieu = true;
        }
        private static void NapItemsVaKhoiPhuc(ComboBox cb, List<string> items)
        {
            string luuCu = cb.Text?.Trim() ?? "";
            cb.BeginUpdate();
            try
            {
                cb.Items.Clear();
                cb.Items.Add(Module_HeThong.Tat_Ca);
                if (items.Count > 0) cb.Items.AddRange(items.Cast<object>().ToArray());
                int idx = luuCu.Length > 0 ? cb.FindStringExact(luuCu) : -1;
                cb.SelectedIndex = idx >= 0 ? idx : 0;
            }
            finally
            {
                cb.EndUpdate();
            }
        }
        private static bool BangTonTai(SqliteConnection conn, string tenBang)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $ten LIMIT 1;";
            cmd.Parameters.AddWithValue("$ten", tenBang);
            return cmd.ExecuteScalar() != null;
        }
        private void ApDungBoLoc()
        {
            if (isUpdatingCombo || IsDisposed || Disposing) return;
            timKiemTimer?.Stop();   // hủy debounce đang chờ -> không lọc lần 2 vô ích
            try
            {
                var goc = _danhSachGoc;
                if (goc == null) return;
                string ten = (textBox_TimKiemTheoTen?.Text ?? "").Trim();
                string dvCongTac = LayGiaTriLoc(comboBox_TimKiemDonVi);
                string tinhTrang = LayGiaTriLoc(comboBox_TinhTrangCongTac);
                string dvKhen = LayGiaTriLoc(comboBox1_DonViKhenThuong);
                var ketQua = new List<ThongTinKhenThuongCBCS>(goc.Count);
                foreach (var x in goc)
                {
                    if (ten.Length > 0 &&
                        (x.HoVaTen ?? "").IndexOf(ten, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (dvCongTac.Length > 0 &&
                        !string.Equals(x.DonVi, dvCongTac, StringComparison.OrdinalIgnoreCase)) continue;
                    if (tinhTrang.Length > 0 &&
                        !string.Equals(x.TinhTrang, tinhTrang, StringComparison.OrdinalIgnoreCase)) continue;
                    // So khớp CHÍNH XÁC theo tập (bản cũ dùng IndexOf nên "Sư đoàn" khớp nhầm "Sư đoàn 10")
                    if (dvKhen.Length > 0 && x.TapDVKhen?.Contains(dvKhen) != true) continue;
                    ketQua.Add(x);
                }
                _danhSachHienThi = ketQua;
                var grid = kryptonDataGridView1_DanhSachCBCS;
                if (grid != null && !grid.IsDisposed && grid.Columns.Count > 0)
                {
                    grid.RowCount = ketQua.Count;
                    grid.Invalidate();
                }
                CapNhatThongKeQuanSo();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi lọc: " + ex);
            }
        }
        private void BoLoc_ValueChanged(object? sender, EventArgs e) => ApDungBoLoc();
        private static string LayGiaTriLoc(ComboBox? cb)
        {
            string v = cb?.Text?.Trim() ?? "";
            return string.Equals(v, Module_HeThong.Tat_Ca, StringComparison.Ordinal) ? "" : v;
        }
        private static string DocChuoi(SqliteDataReader rd, int i)=> rd.IsDBNull(i) ? "" : (Convert.ToString(rd.GetValue(i), CultureInfo.InvariantCulture) ?? "");
        // PHẦN 6 — LƯỚI: ĐỊNH DẠNG MỘT LẦN, VẼ NHANH, CHỌN NHIỀU DÒNG KHÔNG TỐN RAM
        private void DinhDangDataGridHienDai()
        {
            var grid = kryptonDataGridView1_DanhSachCBCS;
            if (grid == null || grid.IsDisposed) return;
            if (_gridDaDinhDang) return;     // Trước đây: mỗi lần reload lại xóa + tạo lại cả 6 cột + đặt lại toàn bộ style
            grid.SuspendLayout();
            try
            {
                _fontBold ??= new Font(grid.Font, FontStyle.Bold);
                typeof(DataGridView).GetProperty("DoubleBuffered",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(grid, true, null);
                grid.EnableHeadersVisualStyles = false;
                grid.BackgroundColor = Color.White;
                grid.StateCommon.Background.Color1 = Color.White;
                grid.StateCommon.DataCell.Back.Color1 = Color.White;
                grid.BorderStyle = BorderStyle.None;
                grid.StateCommon.DataCell.Border.Color1 = Color.FromArgb(235, 235, 235);
                grid.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
                grid.StateCommon.DataCell.Border.Width = 1;
                grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                grid.ColumnHeadersHeight = grid.Font.Height + 36;
                grid.StateCommon.HeaderColumn.Back.Color1 = Color.FromArgb(180, 210, 240);
                grid.StateCommon.HeaderColumn.Back.Color2 = Color.FromArgb(180, 210, 240);
                grid.StateCommon.HeaderColumn.Content.Color1 = Color.FromArgb(30, 30, 30);
                grid.StateCommon.HeaderColumn.Content.Font = _fontBold;
                grid.StateCommon.HeaderColumn.Border.Color1 = Color.FromArgb(150, 180, 210);
                grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                grid.StateSelected.DataCell.Back.Color1 = Color.FromArgb(232, 244, 253);
                grid.StateSelected.DataCell.Back.Color2 = Color.FromArgb(232, 244, 253);
                grid.StateSelected.DataCell.Content.Color1 = Color.FromArgb(0, 102, 204);
                grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
                grid.AllowUserToResizeRows = false;
                grid.AllowUserToOrderColumns = false;
                grid.RowTemplate.Height = grid.Font.Height + 14;
                grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
                grid.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
                grid.ReadOnly = true;
                grid.MultiSelect = true;
                grid.StandardTab = true;
                grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                grid.EditMode = DataGridViewEditMode.EditProgrammatically;
                grid.AllowUserToAddRows = false;
                grid.AllowUserToDeleteRows = false;
                grid.RowHeadersVisible = true;
                grid.RowHeadersWidth = 50;
                grid.VirtualMode = true;
                if (grid.DataSource != null) grid.DataSource = null;
                if (grid.Columns.Count > 0) grid.Columns.Clear();
                // THỨ TỰ CỘT PHẢI KHỚP các hằng COT_* ở trên
                var cauHinhCot = new[]
                {
            (Ten: "HoVaTen",         TieuDe: "Họ và tên",            Rong: 350, Fill: DataGridViewAutoSizeColumnMode.None, CanLe: DataGridViewContentAlignment.MiddleLeft,   InDam: false, MauChu: Color.Empty),
            (Ten: "SoHieu",          TieuDe: "Số hiệu",              Rong: 120, Fill: DataGridViewAutoSizeColumnMode.None, CanLe: DataGridViewContentAlignment.MiddleCenter, InDam: false, MauChu: Color.Empty),
            (Ten: "DonVi",           TieuDe: "Đơn vị",               Rong: 180, Fill: DataGridViewAutoSizeColumnMode.None, CanLe: DataGridViewContentAlignment.MiddleCenter, InDam: false, MauChu: Color.Empty),
            (Ten: "TinhTrang",       TieuDe: "Tình trạng",           Rong: 160, Fill: DataGridViewAutoSizeColumnMode.None, CanLe: DataGridViewContentAlignment.MiddleCenter, InDam: false, MauChu: Color.Empty),
            (Ten: "SoLuong_Khen",    TieuDe: "Tổng số khen thưởng",  Rong: 180, Fill: DataGridViewAutoSizeColumnMode.None, CanLe: DataGridViewContentAlignment.MiddleCenter, InDam: true,  MauChu: Color.FromArgb(0, 102, 204)),
            (Ten: "GhiChu_DanhSach", TieuDe: "Ghi chú",              Rong: 120, Fill: DataGridViewAutoSizeColumnMode.Fill, CanLe: DataGridViewContentAlignment.MiddleLeft,   InDam: false, MauChu: Color.DarkSlateGray)
        };
                foreach (var cot in cauHinhCot)
                {
                    var col = new DataGridViewTextBoxColumn
                    {
                        Name = cot.Ten,
                        HeaderText = cot.TieuDe,
                        Width = cot.Rong,
                        AutoSizeMode = cot.Fill
                    };
                    if (cot.Fill == DataGridViewAutoSizeColumnMode.Fill) col.MinimumWidth = cot.Rong;
                    col.DefaultCellStyle.Alignment = cot.CanLe;
                    if (cot.Ten == "GhiChu_DanhSach") col.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                    if (cot.InDam || cot.MauChu != Color.Empty)
                    {
                        col.DefaultCellStyle.Font = cot.InDam ? _fontBold : grid.Font;
                        if (cot.MauChu != Color.Empty) col.DefaultCellStyle.ForeColor = cot.MauChu;
                    }
                    grid.Columns.Add(col);
                }
                grid.CellValueNeeded -= Grid_CellValueNeeded;
                grid.CellValueNeeded += Grid_CellValueNeeded;
                _gridDaDinhDang = true;
            }
            finally
            {
                grid.ResumeLayout();
            }
        }
        private void Grid_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            var ds = _danhSachHienThi;
            if (ds == null || (uint)e.RowIndex >= (uint)ds.Count) return;
            var c = ds[e.RowIndex];
            // Switch theo chỉ số cột (bản cũ tra Columns[i].Name rồi so chuỗi cho MỖI ô được vẽ)
            switch (e.ColumnIndex)
            {
                case COT_HOTEN: e.Value = c.HoVaTen; break;
                case COT_SOHIEU: e.Value = c.SoHieu; break;
                case COT_DONVI: e.Value = c.DonVi; break;
                case COT_TINHTRANG: e.Value = c.TinhTrang; break;
                case COT_SOLUONG: e.Value = c.SoLuong_Khen; break;
                case COT_GHICHU: e.Value = c.GhiChu_DanhSach ?? ""; break;
            }
        }
        // Icon đã co sẵn 1 lần theo DPI (bản cũ DrawImage ảnh gốc co về 16px cho MỖI ô, MỖI lần vẽ -> giật khi cuộn)
        private Bitmap? LayIconNho(int size)
        {
            if (_iconKhenThuong == null) return null;
            if (_iconNho != null && _iconNhoSize == size) return _iconNho;
            _iconNho?.Dispose();
            var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.DrawImage(_iconKhenThuong, 0, 0, size, size);
            }
            _iconNho = bmp;
            _iconNhoSize = size;
            return bmp;
        }
        // 🔥 HÀM TỰ VẼ ICON VÀO CỘT HỌ VÀ TÊN DỰA VÀO ĐIỀU KIỆN SỐ LƯỢNG KHEN THƯỞNG
        // 🔥 HÀM TỰ VẼ ICON VÀ CHỮ - ĐÃ XÓA SẠCH ĐƯỜNG KẺ VIỀN CỦA CỘT
        private void KryptonDataGridView1_DanhSachCBCS_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != COT_HOTEN) return;   // so int thay vì so chuỗi tên cột
            e.Paint(e.CellBounds, DataGridViewPaintParts.Background |
                                  DataGridViewPaintParts.SelectionBackground |
                                  DataGridViewPaintParts.Focus);
            string hoVaTen = e.Value?.ToString() ?? "";
            var ds = _danhSachHienThi;
            int soLuongKhen = (ds != null && e.RowIndex < ds.Count) ? ds[e.RowIndex].SoLuong_Khen : 0;
            int iconSize = (int)Math.Round(16f * DeviceDpi / 96f);
            int paddingLeft = 6, paddingIconText = 6;
            int xIcon = e.CellBounds.X + paddingLeft;
            int yIcon = e.CellBounds.Y + (e.CellBounds.Height - iconSize) / 2;
            int textStartX = xIcon;
            if (soLuongKhen > 0)
            {
                var icon = LayIconNho(iconSize);
                if (icon != null)
                {
                    e.Graphics.DrawImageUnscaled(icon, xIcon, yIcon);
                    textStartX = xIcon + iconSize + paddingIconText;
                }
            }
            if (hoVaTen.Length > 0)
            {
                var textBounds = new Rectangle(textStartX, e.CellBounds.Y,
                    Math.Max(0, e.CellBounds.Width - (textStartX - e.CellBounds.X)), e.CellBounds.Height);
                bool dangChon = (e.State & DataGridViewElementStates.Selected) != 0;
                Color mau = dangChon ? e.CellStyle.SelectionForeColor : e.CellStyle.ForeColor;
                TextRenderer.DrawText(e.Graphics, hoVaTen, e.CellStyle.Font, textBounds, mau,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }
            e.Handled = true;
        }
        private void LoadComboBoxTinhTrang()
        {
            if (comboBox_TinhTrangCongTac == null) return;
            bool cu = isUpdatingCombo;
            isUpdatingCombo = true;
            try
            {
                comboBox_TinhTrangCongTac.BeginUpdate();
                comboBox_TinhTrangCongTac.Items.Clear();
                comboBox_TinhTrangCongTac.Items.Add(Module_HeThong.Tat_Ca);
                comboBox_TinhTrangCongTac.Items.Add(Module_HeThong.TT_DANG_CONG_TAC);
                comboBox_TinhTrangCongTac.Items.Add(Module_HeThong.TT_CHUYEN_CONG_TAC);
                comboBox_TinhTrangCongTac.SelectedIndex = 0;
            }
            finally
            {
                comboBox_TinhTrangCongTac.EndUpdate();
                isUpdatingCombo = cu;   // luôn trả cờ, kể cả khi lỗi (trước đây có thể kẹt true mãi)
            }
        }
        private void KryptonDataGridView1_DanhSachCBCS_RowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
        {
            if (sender is not DataGridView grid) return;
            // Bản cũ: new StringFormat() cho MỖI dòng MỖI lần vẽ và không Dispose -> rò rỉ GDI+, giật khi cuộn
            var bounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);
            TextRenderer.DrawText(e.Graphics, (e.RowIndex + 1).ToString(CultureInfo.InvariantCulture), grid.Font, bounds,
                SystemColors.ControlText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
        // Sự kiện Click vào Grid đổ dữ liệu xuống TextBox
        private void KryptonDataGridView1_DanhSachCBCS_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < _danhSachHienThi.Count)
            {
                var cbcs = _danhSachHienThi[e.RowIndex];
                if (kryptonTextBox1_STT != null) kryptonTextBox1_STT.Text = (e.RowIndex + 1).ToString();
                if (kryptonTextBox1_HoVaTen != null) kryptonTextBox1_HoVaTen.Text = cbcs.HoVaTen;
                if (kryptonTextBox1_SoHieu != null) kryptonTextBox1_SoHieu.Text = cbcs.SoHieu;
                if (kryptonTextBox1_DonVi != null) kryptonTextBox1_DonVi.Text = cbcs.DonVi;
                if (kryptonTextBox1_TinhTrang != null) kryptonTextBox1_TinhTrang.Text = cbcs.TinhTrang;
                if (kryptonTextBox1_SoLuong != null) kryptonTextBox1_SoLuong.Text = cbcs.SoLuong_Khen.ToString();
            }
        }
        /// Hàm đếm và cập nhật thống kê trên StatusStrip dựa vào dữ liệu đang hiển thị trên lưới
        public void CapNhatThongKeQuanSo()
        {
            if (IsDisposed || Disposing) return;
            if (InvokeRequired) { BeginInvoke(new Action(CapNhatThongKeQuanSo)); return; }
            var ds = _danhSachHienThi;
            if (ds == null) return;
            // 1 lượt duy nhất (bản cũ duyệt 4 lượt LINQ)
            int tong = 0, dang = 0, chuyen = 0, duocKhen = 0;
            foreach (var x in ds)
            {
                if (!string.IsNullOrWhiteSpace(x.SoHieu)) tong++;
                if (string.Equals(x.TinhTrang, Module_HeThong.TT_DANG_CONG_TAC, StringComparison.OrdinalIgnoreCase)) dang++;
                else if (string.Equals(x.TinhTrang, Module_HeThong.TT_CHUYEN_CONG_TAC, StringComparison.OrdinalIgnoreCase)) chuyen++;
                if (x.SoLuong_Khen > 0) duocKhen++;
            }
            GanText(toolStripStatusLabel1, $"Tổng cộng: {tong} {Module_HeThong.Tu_dong_chi}");
            GanText(toolStripStatusLabel2, $"{Module_HeThong.TT_DANG_CONG_TAC}: {dang} {Module_HeThong.Tu_dong_chi}");
            GanText(toolStripStatusLabel3, $"{Module_HeThong.TT_CHUYEN_CONG_TAC}: {chuyen} {Module_HeThong.Tu_dong_chi}");
            if (toolStripStatusLabel4 != null)
            {
                if (duocKhen > 0)
                {
                    GanText(toolStripStatusLabel4, $"Tổng số CBCS đã được khen thưởng: {duocKhen} {Module_HeThong.Tu_dong_chi}");
                    if (!toolStripStatusLabel4.Visible) toolStripStatusLabel4.Visible = true;
                }
                else if (toolStripStatusLabel4.Visible)
                {
                    toolStripStatusLabel4.Visible = false;
                }
            }
        }
        // Chỉ gán khi khác -> tránh StatusStrip layout lại vô ích
        private static void GanText(ToolStripItem? item, string text)
        {
            if (item != null && item.Text != text) item.Text = text;
        }
        private void CauHinhStatusStrip()
        {
            // MẶC ĐỊNH ẨN PROGRESS BAR KHI MỚI LOAD FORM
            if (toolStripProgressBar1_LamMoi != null)
            {
                toolStripProgressBar1_LamMoi.Visible = false;
            }
            // Bật Spring (đẩy lò xo) để Label 2 và 3 tự động chiếm không gian trống ở giữa
            if (toolStripStatusLabel2 != null)
            {
                toolStripStatusLabel2.Spring = true;
                toolStripStatusLabel2.TextAlign = ContentAlignment.MiddleCenter;
            }
            if (toolStripStatusLabel3 != null)
            {
                toolStripStatusLabel3.Spring = true;
                toolStripStatusLabel3.TextAlign = ContentAlignment.MiddleCenter;
            }
            // Label 1 và 4 không dùng Spring, sẽ nằm gọn gàng ở 2 đầu
            if (toolStripStatusLabel1 != null)
                toolStripStatusLabel1.TextAlign = ContentAlignment.MiddleLeft;
            if (toolStripStatusLabel4 != null)
                toolStripStatusLabel4.TextAlign = ContentAlignment.MiddleLeft;
        }
        // Sự kiện Click đúp vào lưới để mở thẳng Form 37
        // 1. Khai báo 1 sự kiện (Event) để ném một Form bất kỳ kèm Tiêu đề lên cho Form Cha (Form 52)
        public event Action<Form, string>? YeuCauLongFormVaoPanel;
        // 1. SỰ KIỆN DOUBLE CLICK: Chỉ cần gọi hàm của nút bấm (không cần viết lại logic)
        private void KryptonDataGridView1_DanhSachCBCS_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                kryptonButton_GoiFromChiTietKhenThuong_Click(sender, e);
            }
        }
        // 2. SỰ KIỆN BẤM NÚT MỞ CHI TIẾT
        // PHẦN 7 — MỞ FORM CHI TIẾT: lấy dữ liệu từ DÒNG ĐANG CHỌN, không lấy từ TextBox
        // Bản cũ: hoTen/soHieu lấy từ TextBox (có thể là người CŨ, đã bị lọc mất khỏi lưới) nhưng
        // currentRowIndex lại lấy từ dòng hiện tại của lưới -> Form35 nhận soHieu của người này
        // mà rowIndex của người khác; Form35 dùng rowIndex để gọi LayTinhTrangCongTacAnToan -> sai tình trạng.
        private void kryptonButton_GoiFromChiTietKhenThuong_Click(object? sender, EventArgs e)
        {
            var grid = kryptonDataGridView1_DanhSachCBCS;
            var ds = _danhSachHienThi;
            int rowIndex = grid?.CurrentCell?.RowIndex ?? -1;
            if (ds == null || rowIndex < 0 || rowIndex >= ds.Count || string.IsNullOrWhiteSpace(ds[rowIndex].SoHieu))
            {
                MessageBox.Show(this, "Vui lòng chọn một Cán bộ chiến sĩ từ danh sách trước khi xem chi tiết!",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var handler = YeuCauLongFormVaoPanel;
            if (handler == null) return;     // không có ai hứng -> không tạo Form35 rồi bỏ rơi (rò rỉ)
            var cb = ds[rowIndex];
            var form35 = new Form35_ChiTietKhenThuong();
            form35.NhanDuLieuTuForm34(cb.HoVaTen ?? "", cb.SoHieu, cb.DonVi ?? "", cb.TinhTrang ?? "", this, rowIndex);
            handler.Invoke(form35, "Chi tiết khen thưởng cá nhân");
        }
        private void kryptonButton_LamMoiCacOTimKiem_Click(object? sender, EventArgs e)
        {
            try
            {
                DatLaiGiaTriBoLoc();
                ApDungBoLoc();
                ClearCacTextBoxChiTiet();
                var grid = kryptonDataGridView1_DanhSachCBCS;
                if (grid != null && !grid.IsDisposed)
                {
                    grid.ClearSelection();
                    grid.CurrentCell = null;
                }
                textBox_TimKiemTheoTen?.Focus();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi khi xóa bộ lọc: " + ex);
            }
        }
        // Hàm hỗ trợ xóa sạch dữ liệu trên các TextBox chi tiết cho gọn giao diện
        private void ClearCacTextBoxChiTiet()
        {
            if (kryptonTextBox1_STT != null) kryptonTextBox1_STT.Clear();
            if (kryptonTextBox1_HoVaTen != null) kryptonTextBox1_HoVaTen.Clear();
            if (kryptonTextBox1_SoHieu != null) kryptonTextBox1_SoHieu.Clear();
            if (kryptonTextBox1_DonVi != null) kryptonTextBox1_DonVi.Clear();
            if (kryptonTextBox1_TinhTrang != null) kryptonTextBox1_TinhTrang.Clear();
            if (kryptonTextBox1_SoLuong != null) kryptonTextBox1_SoLuong.Clear();
        }
        private async void lamMoi_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            // Bản cũ: "sender as Control" luôn null với ToolStripMenuItem -> không khóa được, bấm F5 liên tục chạy chồng
            void DatTrangThai(bool batLai)
            {
                if (sender is ToolStripItem ti) ti.Enabled = batLai;
                else if (sender is Control c) c.Enabled = batLai;
            }
            DatTrangThai(false);
            try
            {
                await ThucHienLamMoiGiaoDienAsync();   // đã bỏ Task.Delay(400) giả lập "đang xử lý"
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi làm mới: " + ex);
            }
            finally
            {
                DatTrangThai(true);
            }
        }
        // XỬ LÝ CLICK CHUỘT PHẢI TỰ ĐỘNG CHỌN DÒNG
        private void KryptonDataGridView1_DanhSachCBCS_MouseDown(object sender, MouseEventArgs e)
        {
            // Chỉ xử lý nếu người dùng nhấn chuột phải
            if (e.Button == MouseButtons.Right)
            {
                var grid = sender as DataGridView;
                if (grid == null) return;
                // Xác định tọa độ chuột đang nằm trên Cell nào
                var hit = grid.HitTest(e.X, e.Y);
                // Nếu chuột nằm trên một dòng hợp lệ (bỏ qua Header)
                if (hit.RowIndex >= 0)
                {
                    // Chỉ chọn lại khi chuột phải vào dòng CHƯA được chọn (giống Excel/Explorer)
                    bool daChon = (grid.Rows.GetRowState(hit.RowIndex) & DataGridViewElementStates.Selected) != 0;
                    if (!daChon)
                    {
                        grid.ClearSelection();
                        grid.Rows[hit.RowIndex].Selected = true;
                        grid.CurrentCell = grid.Rows[hit.RowIndex].Cells[hit.ColumnIndex >= 0 ? hit.ColumnIndex : 0];
                    }
                    KryptonDataGridView1_DanhSachCBCS_CellClick(grid, new DataGridViewCellEventArgs(hit.ColumnIndex, hit.RowIndex));
                }
            }
        }
        private void xoaTimKiem_ToolStripMenuItem_Click(object sender, EventArgs e) => kryptonButton_LamMoiCacOTimKiem.PerformClick();
        private void capNhatThongTinKhenThuong_ToolStripMenuItem_Click(object sender, EventArgs e) => kryptonButton_GoiFromChiTietKhenThuong.PerformClick();
        private void DangKyHieuUngVienTextBox()
        {
            // Chỉ đăng ký hiệu ứng cho ô tìm kiếm (ô cho phép nhập liệu)
            var danhSachTextBox = new List<Control>
            {
                textBox_TimKiemTheoTen
            };
            Color mauFocus = Color.FromArgb(0, 120, 215);
            Color mauHover = Color.FromArgb(100, 180, 255);
            Color mauMacDinh = Color.FromArgb(180, 180, 180);
            foreach (var control in danhSachTextBox)
            {
                if (control == null) continue;
                if (control is Krypton.Toolkit.KryptonTextBox kTextbox)
                {
                    kTextbox.StateCommon.Border.Rounding = 3;
                    kTextbox.StateCommon.Border.Width = 1;
                    kTextbox.StateCommon.Border.Color1 = mauMacDinh;
                    kTextbox.MouseEnter += (s, e) =>
                    {
                        if (!kTextbox.Focused)
                        {
                            kTextbox.StateCommon.Border.Color1 = mauHover;
                            kTextbox.StateCommon.Border.Width = 1;
                        }
                    };
                    kTextbox.MouseLeave += (s, e) =>
                    {
                        if (!kTextbox.Focused)
                        {
                            kTextbox.StateCommon.Border.Color1 = mauMacDinh;
                            kTextbox.StateCommon.Border.Width = 1;
                        }
                    };
                    kTextbox.Enter += (s, e) =>
                    {
                        kTextbox.StateCommon.Border.Color1 = mauFocus;
                        kTextbox.StateCommon.Border.Width = 2;
                    };
                    kTextbox.Leave += (s, e) =>
                    {
                        kTextbox.StateCommon.Border.Color1 = mauMacDinh;
                        kTextbox.StateCommon.Border.Width = 1;
                    };
                }
                else if (control is System.Windows.Forms.TextBox stdTextbox)
                {
                    Color nenFocus = Color.FromArgb(240, 248, 255);
                    Color nenMacDinh = Color.White;
                    stdTextbox.Enter += (s, e) => stdTextbox.BackColor = nenFocus;
                    stdTextbox.Leave += (s, e) => stdTextbox.BackColor = nenMacDinh;
                }
            }
        }
        /// Hàm phân tích chi tiết khen thưởng dựa trên dữ liệu đang hiển thị trên DataGridView (đã qua bộ lọc)
        // (khai báo event: thêm dấu ? cho đúng nullable)
      //public event Action<Form, string>? YeuCauLongFormVaoPanel;
        // PHẦN 8 — PHÂN TÍCH / THỐNG KÊ: dùng TapDVKhen thay vì Split(',')
        public void HienThiPhanTichKhenThuong()
        {
            var ds = _danhSachHienThi;
            if (ds == null || ds.Count == 0)
            {
                MessageBox.Show(this, "Hiện không có dữ liệu nào trên danh sách để phân tích!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int tongSoGiayKhen = 0;
            var thongKeDonVi = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cbcs in ds)
            {
                if (string.IsNullOrWhiteSpace(cbcs.SoHieu)) continue;
                tongSoGiayKhen += cbcs.SoLuong_Khen;
                if (cbcs.TapDVKhen == null) continue;
                foreach (var dv in cbcs.TapDVKhen)
                    thongKeDonVi[dv] = thongKeDonVi.TryGetValue(dv, out int c) ? c + 1 : 1;
            }
            var sb = new StringBuilder()
                .AppendLine("KẾT QUẢ PHÂN TÍCH KHEN THƯỞNG")
                .AppendLine("(Dựa trên danh sách đang hiển thị hiện tại)")
                .AppendLine("--------------------------------------------------")
                .AppendLine($"- Tổng số lượng Giấy khen / Bằng khen: {tongSoGiayKhen}")
                .AppendLine($"- Có tổng cộng {thongKeDonVi.Count} đơn vị ra quyết định khen thưởng.")
                .AppendLine("--------------------------------------------------")
                .AppendLine("🏆 CHI TIẾT:");
            foreach (var item in thongKeDonVi.OrderByDescending(x => x.Value))
                sb.AppendLine($"+ {item.Key}: {item.Value} lượt");
            MessageBox.Show(this, sb.ToString(), "Báo cáo phân tích", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void phanTichKhenThuong_ToolStripMenuItem_Click(object? sender, EventArgs e)
            => HienThiPhanTichKhenThuong();
        private void thongKeKhenThuong_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (_danhSachHienThi == null || _danhSachHienThi.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để phân tích.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                var thongKeDonVi = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var cbcs in _danhSachHienThi)
                {
                    if (string.IsNullOrWhiteSpace(cbcs.SoHieu) || cbcs.TapDVKhen == null || cbcs.TapDVKhen.Count == 0) continue;
                    string hoTen = string.IsNullOrWhiteSpace(cbcs.HoVaTen) ? "Không rõ" : cbcs.HoVaTen.Trim();
                    string donViCBCS = cbcs.DonVi?.Trim();
                    string thongTin = string.IsNullOrWhiteSpace(donViCBCS) ? hoTen : $"{hoTen} ({donViCBCS})";
                    foreach (var dv in cbcs.TapDVKhen)
                    {
                        if (!thongKeDonVi.TryGetValue(dv, out var list)) { list = new List<string>(); thongKeDonVi[dv] = list; }
                        list.Add(thongTin);
                    }
                }
                int tongSoLuotThucTe = thongKeDonVi.Values.Sum(x => x.Distinct().Count());
                using var sfd = new SaveFileDialog
                {
                    Title = "Lưu báo cáo thống kê",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx|Text File (*.txt)|*.txt",
                    FileName = $"ThongKe_DonViKhenThuong_{DateTime.Now:ddMMyyyy_HHmm}"
                };
                if (sfd.ShowDialog() != DialogResult.OK) return;
                string filePath = sfd.FileName;
                string ext = Path.GetExtension(filePath).ToLower();
                if (_namHienTaiCache == -1)
                {
                    _namHienTaiCache = Module_HeThong.LayNamHeThong();
                }
                if (ext == ".xlsx")
                {
                    using var wb = new ClosedXML.Excel.XLWorkbook();
                    var ws = wb.Worksheets.Add("Thống kê đơn vị");
                    ws.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                    ws.Style.Font.FontSize = 13;
                    var rangeHeader = ws.Range("A1:D1").Merge();
                    rangeHeader.Value = $"THỐNG KÊ CÔNG TÁC KHEN THƯỞNG NĂM {_namHienTaiCache}";
                    rangeHeader.Style.Font.Bold = true;
                    rangeHeader.Style.Font.FontSize = 14;
                    rangeHeader.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                    ws.Cell("A2").Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                    ws.Range("A2:D2").Merge().Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                    int headerRow = 4;
                    ws.Row(headerRow).Height = 35;
                    string[] columns = { "STT", "Đơn vị ra quyết định khen thưởng", "Số lượt", "Danh sách CBCS được khen" };
                    for (int i = 0; i < columns.Length; i++)
                    {
                        var cell = ws.Cell(headerRow, i + 1);
                        cell.Value = columns[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#E8E8E8");
                        cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                        cell.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                    }
                    int currentRow = headerRow + 1;
                    int sttIdx = 1;
                    foreach (var item in thongKeDonVi.OrderByDescending(x => x.Value.Distinct().Count()))
                    {
                        var dsNguoiKhen = item.Value.Distinct().ToList();
                        ws.Cell(currentRow, 1).Value = sttIdx++;
                        ws.Cell(currentRow, 2).Value = item.Key;
                        ws.Cell(currentRow, 3).Value = dsNguoiKhen.Count;
                        ws.Cell(currentRow, 4).Value = string.Join("; ", dsNguoiKhen);
                        ws.Cell(currentRow, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                        ws.Cell(currentRow, 2).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;
                        ws.Cell(currentRow, 3).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                        ws.Cell(currentRow, 4).Style.Alignment.WrapText = true;
                        currentRow++;
                    }
                    var totalRange = ws.Range(currentRow, 1, currentRow, 2).Merge();
                    totalRange.Value = $"Tổng cộng: {thongKeDonVi.Count} đơn vị";
                    totalRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;
                    totalRange.Style.Font.Italic = true;
                    ws.Cell(currentRow, 3).Value = tongSoLuotThucTe;
                    ws.Cell(currentRow, 3).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                    var rowFooter = ws.Range(currentRow, 1, currentRow, 4);
                    rowFooter.Style.Font.Bold = true;
                    rowFooter.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#F9F9F9");
                    var fullTableRange = ws.Range(headerRow, 1, currentRow, 4);
                    fullTableRange.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                    fullTableRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                    ws.Column(1).Width = 8;
                    ws.Column(2).Width = 45;
                    ws.Column(3).Width = 15;
                    ws.Column(4).Width = 60;
                    Module_BanQuyen.DongDauExcel(wb);
                    wb.SaveAs(filePath);
                }
                else
                {
                    var sb = new StringBuilder()
                        .AppendLine("==================================================")
                        .AppendLine("    BÁO CÁO THỐNG KÊ LƯỢT KHEN THƯỞNG")
                        .AppendLine($"    Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .AppendLine("==================================================")
                        .AppendLine($"Tổng số lượt khen thưởng ghi nhận: {tongSoLuotThucTe}")
                        .AppendLine($"Tổng số cơ quan khen thưởng: {thongKeDonVi.Count}")
                        .AppendLine("--------------------------------------------------\n");
                    foreach (var item in thongKeDonVi.OrderByDescending(x => x.Value.Distinct().Count()))
                    {
                        var dsNguoiKhen = item.Value.Distinct().ToList();
                        sb.AppendLine($"[+] {item.Key.ToUpper()}: {dsNguoiKhen.Count} lượt");
                        sb.AppendLine($"    - CBCS: {string.Join(", ", dsNguoiKhen)}");
                        sb.AppendLine();
                    }
                    System.IO.File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                }
                if (MessageBox.Show("Đã phân tích và xuất báo cáo thành công!\nBạn có muốn mở tệp ngay không?", "Thành công", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
                }
            }
            catch (System.IO.IOException)
            {
                MessageBox.Show("Lỗi lưu file: File Excel có thể đang được mở ở một cửa sổ khác. Vui lòng đóng và thử lại.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi thực thi: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }
        private void xuatDuLieuRaTepExcel_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (_danhSachHienThi == null || _danhSachHienThi.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu nào để xuất ra Excel!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel Workbook (*.xlsx)|*.xlsx";
                sfd.Title = "Lưu file Thống kê số liệu khen thưởng";
                sfd.FileName = $"ThongKe_SoLieuKhenThuong_{DateTime.Now:ddMMyyyy_HHmm}.xlsx";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    Cursor.Current = Cursors.WaitCursor;
                    try
                    {
                        using (var workbook = new ClosedXML.Excel.XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("DS Khen Thuong");
                            // Tìm mảng headers và thêm "Ghi chú" vào cuối
                            string[] headers = { "STT", "Họ và tên", "Số hiệu", "Đơn vị", "Tình trạng công tác", "Tổng số lần khen thưởng", "Ghi chú" };
                            for (int i = 0; i < headers.Length; i++)
                            {
                                var cell = worksheet.Cell(1, i + 1);
                                cell.Value = headers[i];
                                cell.Style.Font.Bold = true;
                                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.AliceBlue;
                                cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                                cell.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                            }
                            int rowIndex = 2;
                            int stt = 1;
                            foreach (var cbcs in _danhSachHienThi) // Đã sửa đọc từ mảng RAM
                            {
                                if (string.IsNullOrWhiteSpace(cbcs.SoHieu)) continue;
                                worksheet.Cell(rowIndex, 1).Value = stt++;
                                worksheet.Cell(rowIndex, 2).Value = cbcs.HoVaTen;
                                worksheet.Cell(rowIndex, 3).Value = cbcs.SoHieu;
                                worksheet.Cell(rowIndex, 4).Value = cbcs.DonVi;
                                worksheet.Cell(rowIndex, 5).Value = cbcs.TinhTrang;
                                worksheet.Cell(rowIndex, 6).Value = cbcs.SoLuong_Khen;
                                // ⭐ BỔ SUNG DÒNG GHI EXCEL CHO GHI CHÚ (Cột số 7)
                                worksheet.Cell(rowIndex, 7).Value = cbcs.GhiChu_DanhSach;
                                for (int i = 1; i <= headers.Length; i++)
                                {
                                    worksheet.Cell(rowIndex, i).Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                                }
                                rowIndex++;
                            }
                            worksheet.Columns().AdjustToContents();
                            // 🔥 GỌI MODULE ĐÓNG DẤU BẢN QUYỀN TẠI ĐÂY (TRƯỚC KHI LƯU) 🔥
                            Module_BanQuyen.DongDauExcel(workbook);
                            workbook.SaveAs(sfd.FileName);
                        }
                        DialogResult openFile = MessageBox.Show("Xuất dữ liệu thành công! Bạn có muốn mở file Excel vừa tạo lên không?", "Hoàn tất", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (openFile == DialogResult.Yes)
                        {
                            Process.Start(new ProcessStartInfo { FileName = sfd.FileName, UseShellExecute = true });
                        }
                    }
                    catch (System.IO.IOException)
                    {
                        MessageBox.Show("Không thể lưu file.\nVui lòng kiểm tra xem file Excel này có đang được mở ở một cửa sổ khác không, hãy đóng nó lại và thử lại!", "Lỗi lưu tệp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Quá trình xuất dữ liệu gặp lỗi:\n{ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        Cursor.Current = Cursors.Default;
                    }
                }
            }
        }
        private async void xoaToanBoDuLieu_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            // 1. KIỂM TRA
            string dbPath = _csdl4Path;
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
            {
                MessageBox.Show("Không tìm thấy CSDL khen thưởng (csdl4.db)!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (kryptonDataGridView1_DanhSachCBCS.Rows.Count == 0)
            {
                MessageBox.Show("Hiện không có dữ liệu để xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            // 2. XÁC MINH ADMIN
            DialogResult kqAdmin;
            using (Form24_XacMinhAdmin frm = new Form24_XacMinhAdmin())
            {
                frm.TopMost = true;
                frm.StartPosition = FormStartPosition.CenterScreen;
                kqAdmin = frm.ShowDialog(this);
            }
            if (kqAdmin != DialogResult.OK) return;
            // 3. BẮT ĐẦU XÓA
            Cursor.Current = Cursors.WaitCursor;
            kryptonDataGridView1_DanhSachCBCS.DataSource = null; // Ép gỡ lưới để nhả file DB
            try
            {
                // 🔥 TUYỆT KỸ: BẮT TASK TRẢ VỀ 2 CON SỐ CÙNG LÚC DÙNG TUPLE (int, int)
                var ketQuaDem = await Task.Run(() =>
                {
                    int demThongKe = 0;
                    int demChiTiet = 0;
                    using (var conn = new SqliteConnection($"Data Source={dbPath}"))
                    {
                        conn.Open();
                        using (var cmdPragma = conn.CreateCommand())
                        {
                            cmdPragma.CommandText = "PRAGMA foreign_keys = OFF;";
                            cmdPragma.ExecuteNonQuery();
                        }
                        using (var tran = conn.BeginTransaction())
                        {
                            try
                            {
                                using (var cmd = conn.CreateCommand())
                                {
                                    cmd.Transaction = tran;
                                    // BƯỚC 1: ĐẾM CHÍNH XÁC SỐ DÒNG ĐANG TỒN TẠI TRONG 2 BẢNG
                                    cmd.CommandText = "SELECT COUNT(*) FROM ThongKeCBCS_DuocKhenThuong;";
                                    demThongKe = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                                    cmd.CommandText = "SELECT COUNT(*) FROM ThongKe_GiayKhen;";
                                    demChiTiet = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                                    // BƯỚC 2: XÓA SẠCH DỮ LIỆU
                                    cmd.CommandText = "DELETE FROM ThongKeCBCS_DuocKhenThuong;";
                                    cmd.ExecuteNonQuery();
                                    cmd.CommandText = "DELETE FROM ThongKe_GiayKhen;";
                                    cmd.ExecuteNonQuery();
                                    // BƯỚC 3: RESET ID VỀ 1
                                    cmd.CommandText = "DELETE FROM sqlite_sequence WHERE name IN ('ThongKeCBCS_DuocKhenThuong', 'ThongKe_GiayKhen');";
                                    cmd.ExecuteNonQuery();
                                }
                                tran.Commit();
                            }
                            catch
                            {
                                tran.Rollback();
                                throw;
                            }
                        }
                        // DỌN DẸP Ổ CỨNG
                        using (var cmdVacuum = conn.CreateCommand())
                        {
                            cmdVacuum.CommandText = "VACUUM;";
                            cmdVacuum.ExecuteNonQuery();
                        }
                    }
                    // TRẢ VỀ KẾT QUẢ CHO LUỒNG UI
                    return (ThongKe: demThongKe, ChiTiet: demChiTiet);
                });
                // 4. LẤY KẾT QUẢ ĐÃ CHẠY XONG
                int soDongXoa_ThongKe = ketQuaDem.ThongKe;
                int soDongXoa_ChiTiet = ketQuaDem.ChiTiet;
                // Nếu cả 2 đều là 0, nghĩa là bảng đã trống từ trước (Lưới hiển thị là do lệnh LEFT JOIN)
                if (soDongXoa_ThongKe == 0 && soDongXoa_ChiTiet == 0)
                {
                    MessageBox.Show("Các bảng khen thưởng hiện tại đã trống sẵn (0 bản ghi).\nHệ thống đã dọn dẹp và reset lại bộ đếm ID thành công!",
                                    "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Đã xóa sạch toàn bộ dữ liệu khen thưởng thành công!\n" +
                                    $"- Bảng thống kê (Người được khen): {soDongXoa_ThongKe} bản ghi.\n" +
                                    $"- Bảng chi tiết (Số lượng giấy khen): {soDongXoa_ChiTiet} bản ghi.",
                                    "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                // 5. GHI NHẬT KÝ
                try
                {
                    Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Xóa sạch dữ liệu khen thưởng", $"Xóa {soDongXoa_ThongKe} dòng Thống kê & {soDongXoa_ChiTiet} dòng Chi tiết lúc {DateTime.Now:HH:mm:ss}");
                }
                catch { }
                // 6. LÀM MỚI GIAO DIỆN (YÊU MÈO CAM)
                if (_danhSachGoc != null) _danhSachGoc.Clear();
                if (_danhSachHienThi != null) _danhSachHienThi.Clear();
                if (kryptonDataGridView1_DanhSachCBCS != null)
                {
                    kryptonDataGridView1_DanhSachCBCS.RowCount = 0;
                    kryptonDataGridView1_DanhSachCBCS.Invalidate();
                }
                kryptonButton_LamMoiCacOTimKiem_Click(null, EventArgs.Empty);
                await ReloadDuLieu();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Đã xảy ra lỗi:\n{ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                await ReloadDuLieu();
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }
        // PHẦN 5 — COMBOBOX ĐƠN VỊ + BỘ LỌC
        private void CapNhatComboBoxTuList(List<string> dsDonViChuan)
        {
            if (IsDisposed || Disposing) return;
            if (comboBox_TimKiemDonVi == null || comboBox1_DonViKhenThuong == null) return;
            // KHÔNG return khi danh sách rỗng như bản cũ (sau khi xóa hết, combo vẫn còn đơn vị cũ)
            var goc = _danhSachGoc ?? new List<ThongTinKhenThuongCBCS>();
            // Đơn vị công tác: thứ tự chuẩn từ CSDL trước, đơn vị phát sinh thêm xếp ABC sau
            var thucTe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in goc)
                if (!string.IsNullOrWhiteSpace(r.DonVi)) thucTe.Add(r.DonVi);
            var dsDonVi = new List<string>(thucTe.Count);
            var daThem = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var dv in dsDonViChuan)
                if (thucTe.Contains(dv) && daThem.Add(dv)) dsDonVi.Add(dv);
            var conLai = thucTe.Where(x => !daThem.Contains(x))
                               .OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase);
            dsDonVi.AddRange(conLai);
            // Đơn vị khen: lấy thẳng từ TapDVKhen (không Split(',') nữa -> tên đơn vị có dấu phẩy không bị cắt đôi)
            var tapKhen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in goc)
                if (r.TapDVKhen != null)
                    foreach (var dv in r.TapDVKhen) tapKhen.Add(dv);
            var dsKhen = tapKhen.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase).ToList();
            bool cu = isUpdatingCombo;
            isUpdatingCombo = true;
            try
            {
                NapItemsVaKhoiPhuc(comboBox_TimKiemDonVi, dsDonVi);
                NapItemsVaKhoiPhuc(comboBox1_DonViKhenThuong, dsKhen);
            }
            finally
            {
                isUpdatingCombo = cu;
            }
        }
        //Lọc trên RAM 1 lượt duy nhất, rồi cập nhật lưới + thống kê.</summary>
        //Đặt lại giá trị bộ lọc nhưng KHÔNG kích hoạt lọc (người gọi tự gọi ApDungBoLoc 1 lần).</summary>
        private void DatLaiGiaTriBoLoc()
        {
            bool cu = isUpdatingCombo;
            isUpdatingCombo = true;
            try
            {
                textBox_TimKiemTheoTen?.Clear();
                foreach (var cb in new[] { comboBox_TimKiemDonVi, comboBox_TinhTrangCongTac, comboBox1_DonViKhenThuong })
                    if (cb != null && cb.Items.Count > 0) cb.SelectedIndex = 0;
            }
            finally
            {
                isUpdatingCombo = cu;       // luôn trả cờ
            }
            timKiemTimer?.Stop();           // Clear() vừa kích hoạt TextChanged -> hủy lần lọc thừa
        }
        // HÀM ĐỌC DỮ LIỆU KẾT HỢP (CSDL2 + CSDL4) NẠP SANG DTO   
        private List<KhenThuongExportDTO> DocDanhSachKhenThuongToanBoDTO()
        {
            var list = new List<KhenThuongExportDTO>();
            // Step 1: Lấy thông tin nhân thân từ CSDL2 (DanhSach)
            var dictThongTinGoc = new Dictionary<string, (string NamSinh, string CapBac, string ChucVu, string QueQuan, string DonVi, string GhiChu)>(StringComparer.Ordinal);
            if (File.Exists(_csdl2Path))
            {
                using var cn2 = new SqliteConnection($"Data Source={_csdl2Path};Pooling=True;");
                cn2.Open();
                using var cmd2 = new SqliteCommand("SELECT SoHieu, NamSinh, CapBac, ChucVu, QueQuan, DonVi, GhiChu FROM DanhSach", cn2);
                using var rd2 = cmd2.ExecuteReader();
                while (rd2.Read())
                {
                    string sh = SafeDecrypt(rd2["SoHieu"]?.ToString());
                    if (!string.IsNullOrEmpty(sh))
                    {
                        dictThongTinGoc[sh] = (
                            SafeDecrypt(rd2["NamSinh"]?.ToString()),
                            SafeDecrypt(rd2["CapBac"]?.ToString()),
                            SafeDecrypt(rd2["ChucVu"]?.ToString()),
                            SafeDecrypt(rd2["QueQuan"]?.ToString()),
                            SafeDecrypt(rd2["DonVi"]?.ToString()),
                            SafeDecrypt(rd2["GhiChu"]?.ToString())
                        );
                    }
                }
            }
            // Step 2: Lấy dữ liệu Khen thưởng từ CSDL4 (ThongKe_GiayKhen)
            using (var cn4 = new SqliteConnection($"Data Source={_csdl4Path};Pooling=True;"))
            {
                cn4.Open();
                using (var cmdCheck = new SqliteCommand("CREATE TABLE IF NOT EXISTS ThongKe_GiayKhen (ID INTEGER PRIMARY KEY AUTOINCREMENT, HoVaTen TEXT, SoHieu TEXT, DonVi TEXT, TinhTrang TEXT, HinhThuc_Khen TEXT, QuyetDinh_Khen TEXT, NgayCapQD_Khen TEXT, DonVi_Khen TEXT, VeViec_Khen TEXT, GhiChu_Khen TEXT);", cn4))
                {
                    cmdCheck.ExecuteNonQuery();
                }
                string query = @"SELECT HoVaTen, SoHieu, DonVi, TinhTrang, 
                                HinhThuc_Khen, QuyetDinh_Khen, NgayCapQD_Khen, 
                                DonVi_Khen, VeViec_Khen, GhiChu_Khen 
                         FROM ThongKe_GiayKhen 
                         ORDER BY ID ASC;";
                using var cmd4 = new SqliteCommand(query, cn4);
                using var rd4 = cmd4.ExecuteReader();
                int stt = 1;
                while (rd4.Read())
                {
                    string sh = SafeDecrypt(rd4["SoHieu"]?.ToString());
                    bool coGoc = !string.IsNullOrEmpty(sh) && dictThongTinGoc.ContainsKey(sh);
                    var infoGoc = coGoc ? dictThongTinGoc[sh] : (NamSinh: "", CapBac: "", ChucVu: "", QueQuan: "", DonVi: "", GhiChu: "");
                    list.Add(new KhenThuongExportDTO
                    {
                        STT = stt++,
                        HoVaTen = SafeDecrypt(rd4["HoVaTen"]?.ToString()),
                        SoHieu = sh,
                        NamSinh = infoGoc.NamSinh,
                        CapBac = infoGoc.CapBac,
                        ChucVu = infoGoc.ChucVu,
                        QueQuan = infoGoc.QueQuan,
                        DonVi = coGoc ? infoGoc.DonVi : SafeDecrypt(rd4["DonVi"]?.ToString()),
                        TinhTrang = SafeDecrypt(rd4["TinhTrang"]?.ToString()),
                        HinhThuc_Khen = SafeDecrypt(rd4["HinhThuc_Khen"]?.ToString()),
                        QuyetDinh_Khen = SafeDecrypt(rd4["QuyetDinh_Khen"]?.ToString()),
                        NgayCapQD_Khen = SafeDecrypt(rd4["NgayCapQD_Khen"]?.ToString()),
                        DonVi_Khen = SafeDecrypt(rd4["DonVi_Khen"]?.ToString()),
                        VeViec_Khen = SafeDecrypt(rd4["VeViec_Khen"]?.ToString()),
                        GhiChu_Khen = SafeDecrypt(rd4["GhiChu_Khen"]?.ToString())
                    });
                }
            }
            return list;
        }
        // HÀM LẤY THÔNG TIN ĐƠN VỊ & NĂM TỪ BẢNG CẤU HÌNH (CSDL2)
        private async void kryptonButton1_QuanLyKhenThuongTapThe_Click(object? sender, EventArgs e)
        {
            // 1. Tìm Form cha đang hoạt động
            var formCha = Application.OpenForms
                .OfType<Form2_FormCha>()
                .FirstOrDefault();
            if (formCha == null || formCha.IsDisposed)
            {
                MessageBox.Show(
                    "Không tìm thấy giao diện chính (Form2_FormCha).\n\nVui lòng kiểm tra lại trạng thái của phần mềm.",
                    "Hệ thống",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            // 2. Tìm PanelContainer
            var panel = formCha.Controls
                .Find("PanelContainer", true)
                .FirstOrDefault() as Panel;
            if (panel == null || panel.IsDisposed)
            {
                MessageBox.Show(
                    "Không tìm thấy vùng hiển thị (PanelContainer).\n\nVui lòng kiểm tra lại giao diện chính.",
                    "Hệ thống",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            try
            {
                // 3. Tìm Form49 đã tồn tại trong PanelContainer
                var form49 = panel.Controls
                    .OfType<Form49_QuanLyKhenThuongTapThe>()
                    .FirstOrDefault();
                // 4. Nếu Form49 chưa tồn tại -> tạo mới đúng một lần
                if (form49 == null || form49.IsDisposed)
                {
                    form49 = new Form49_QuanLyKhenThuongTapThe
                    {
                        TopLevel = false,
                        FormBorderStyle = FormBorderStyle.None,
                        Dock = DockStyle.Fill
                    };
                    // 5. Đưa Form49 vào PanelContainer
                    panel.Controls.Add(form49);
                    // Đưa Form49 lên trước khi hiển thị
                    form49.BringToFront();
                    // Hiển thị Form lần đầu
                    form49.Show();
                }
                else
                {
                    // 6. Nếu Form49 đã tồn tại -> tái sử dụng
                    form49.BringToFront();
                    if (!form49.Visible)
                    {
                        form49.Show();
                    }
                    // 7. Làm mới dữ liệu sau khi tái sử dụng Form
                    form49.ReloadForm49();
                }
                // 8. Ẩn các Form con khác trong PanelContainer
                foreach (Control control in panel.Controls)
                {
                    if (control is Form childForm &&
                        childForm != form49 &&
                        !childForm.IsDisposed)
                    {
                        childForm.Hide();
                    }
                }
                // 9. Đảm bảo Form49 luôn ở lớp trên cùng
                form49.Show();
                form49.BringToFront();
                string tieuDeForm = "Trang Quản lý khen thưởng tập thể năm " +
                            Module_HeThong.LayNamHeThong();
                // 10. Cập nhật tiêu đề Form cha
                formCha.CapNhatTieuDe(tieuDeForm);
            }
            catch (ObjectDisposedException)
            {
                // Form hoặc Control đã bị giải phóng trong lúc chuyển giao diện.
                // Không để lỗi này làm sập toàn bộ phần mềm.
                return;
            }
            catch (InvalidOperationException ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Lỗi chuyển Form49 - InvalidOperationException]: {ex}");
                MessageBox.Show(
                    "Không thể mở trang quản lý khen thưởng tập thể.\n\n" +
                    "Vui lòng thử lại.",
                    "Lỗi giao diện",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Lỗi chuyển Form49]: {ex}");
                MessageBox.Show(
                    "Đã xảy ra lỗi khi mở trang quản lý khen thưởng tập thể.\n\n" +
                    $"Chi tiết: {ex.Message}",
                    "Lỗi hệ thống",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void toolStripMenuItem_QuanLyKhenThuongNamCu_Click(object? sender, EventArgs e)
        {
            // 1. Tìm Form cha (Form2_FormCha) đang chạy để tương tác với PanelContainer
            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            if (formCha == null) return;
            // 2. Tìm vùng chứa PanelContainer trên Form cha
            var panel = formCha.Controls.Find("PanelContainer", true).FirstOrDefault() as Panel;
            if (panel == null) return;
            // ⭐ QUAN TRỌNG 1: Ẩn tất cả các Form con khác ĐANG HIỂN THỊ trong PanelContainer
            // Việc này giúp dọn dẹp không gian, tránh các Form đè lấn lên nhau gây lỗi đồ họa
            foreach (Control control in panel.Controls)
            {
                if (control is Form childForm && !childForm.IsDisposed && childForm.Visible)
                {
                    childForm.Hide();
                }
            }
            // 3. Kiểm tra xem Form50 đã được khởi tạo và tồn tại trong bộ nhớ RAM chưa
            var f50 = Application.OpenForms.OfType<Form50_QuanLyKhenThuongNamCu>().FirstOrDefault();
            if (f50 == null || f50.IsDisposed)
            {
                // 👉 TRƯỜNG HỢP 1: CHƯA TỒN TẠI -> Khởi tạo mới hoàn toàn
                f50 = new Form50_QuanLyKhenThuongNamCu
                {
                    Text = "Thống kê khen thưởng năm cũ",
                    TopLevel = false,
                    FormBorderStyle = FormBorderStyle.None,
                    Dock = DockStyle.Fill
                };
                // Nạp Form50 vào panel
                panel.Controls.Add(f50);
                // Quản lý sự kiện khi Form 50 đóng lại thì hiện lại Form 34 (this)
                var form34 = this;
                f50.FormClosed += (s, ev) =>
                {
                    if (form34 != null && !form34.IsDisposed)
                    {
                        form34.Dock = DockStyle.Fill;
                        form34.Show();
                        form34.BringToFront();
                        var fChaCheck = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
                        if (fChaCheck != null)
                        {
                            int namHienTai = Module_HeThong.LayNamHeThong();
                            fChaCheck.CapNhatTieuDe($"Trang Quản lý khen thưởng CBCS năm {namHienTai}");
                        }
                    }
                };
            }
            // 👉 DÙ LÀ TRƯỜNG HỢP 1 HAY 2 THÌ ĐỀU PHẢI GỌI KHỐI LỆNH NÀY:
            // ⭐ QUAN TRỌNG 2: Bắt buộc phải có Show() để Form hiện hình từ trạng thái Hide
            f50.BringToFront();
            f50.Show();
            f50.Focus(); // Ép hệ thống chuyển sự chú ý vào Form 50 ngay lập tức, chống đơ phím
            // 4. Truyền động tên Text của f50 lên thanh tiêu đề của Form Cha
            formCha.CapNhatTieuDe(f50.Text);
        }
        private async void toolStripMenuItem_LuuKetQuaThiDuaTheoNam_Click(object? sender, EventArgs e)
        {
            // 1. XÁC MINH QUYỀN ADMIN
            DialogResult kq;
            using (Form24_XacMinhAdmin frm = new Form24_XacMinhAdmin())
            {
                frm.TopMost = true;
                frm.StartPosition = FormStartPosition.CenterScreen;
                kq = frm.ShowDialog(this);
            }
            if (kq != DialogResult.OK) return;
            // 2. KHÓA MENU CHỐNG SPAM CLICK
            var menu = toolStripMenuItem_LuuKetQuaThiDuaTheoNam;
            string textGoc = menu.Text;
            try
            {
                menu.Enabled = false;
                menu.Text = "Đang lưu trữ...";
                Cursor.Current = Cursors.WaitCursor;
                // 3. THỰC HIỆN LƯU TRỮ TRÊN LUỒNG NGẦM
                string duongDan = await Task.Run(Module_HoTroLuuDataTheoNamCu.LuuTruDuLieuKhenThuongToanDienNam);
                // [QUAN TRỌNG]: Kiểm tra ngay lập tức nếu người dùng bấm No hoặc đang ở bản Tân binh
                if (string.IsNullOrEmpty(duongDan))
                {
                    return; // Thoát luồng ngay lập tức, không báo thành công, nhường chỗ cho khối finally chạy
                }
                // 4. CẬP NHẬT GIAO DIỆN KHI THÀNH CÔNG
                var f50Check = Application.OpenForms.OfType<Form50_QuanLyKhenThuongNamCu>().FirstOrDefault();
                if (f50Check != null && !f50Check.IsDisposed)
                {
                    f50Check.LoadDanhSachFileLichSu();
                }
                Module_NhatKy.GhiNhatKy(
                    Module_TaiKhoan.TenTaiKhoan_RAM,
                    "Đã sao lưu toàn bộ dữ liệu Khen thưởng (Tập thể và CBCS) sang tệp lịch sử.",
                    $"Lưu trữ thành công tại {duongDan}" // ⭐ Đặt chữ $ ở đây để biến {duongDan} có tác dụng
                );
                MessageBox.Show(
                    $"Đã sao lưu toàn bộ dữ liệu Khen thưởng (Cá nhân & Tập thể) thành công",
                    "Hoàn tất sao lưu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Không thể lưu trữ dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                // Nhả khóa UI an toàn trong mọi trường hợp (Thành công, Lỗi, hay Bấm No từ chối)
                menu.Enabled = true;
                menu.Text = textGoc;
                Cursor.Current = Cursors.Default;
            }
        }
        private (string TenTieuDoan, string NamHienTai) LayThongTinTieuDoan()
        {
            string tenTieuDoan = "";
            string nam = DateTime.Now.Year.ToString(); // Dự phòng nếu CSDL không có
            if (!File.Exists(_csdl2Path)) return (tenTieuDoan, nam);
            try
            {
                using var cn = new SqliteConnection($"Data Source={_csdl2Path};Pooling=True;");
                cn.Open();
                // Bảng ThongTin lưu cấu hình hệ thống thường ở dòng đầu tiên
                using var cmd = new SqliteCommand("SELECT TenTieuDoan, Nam FROM ThongTin LIMIT 1;", cn);
                using var rd = cmd.ExecuteReader();
                if (rd.Read())
                {
                    string ttdDecrypted = SafeDecrypt(rd["TenTieuDoan"]?.ToString());
                    string namDecrypted = SafeDecrypt(rd["Nam"]?.ToString());
                    if (!string.IsNullOrWhiteSpace(ttdDecrypted))
                        tenTieuDoan = ttdDecrypted;
                    if (!string.IsNullOrWhiteSpace(namDecrypted))
                        nam = namDecrypted;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi lấy thông tin Tiểu đoàn]: {ex.Message}");
            }
            return (tenTieuDoan, nam);
        }
        private async void toolStripMenuItem_XuatToanBoDanhSachRaTepExcel_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_csdl4Path) || !File.Exists(_csdl4Path))
            {
                MessageBox.Show("Không tìm thấy CSDL khen thưởng (csdl4.db)!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using var sfd = new SaveFileDialog
            {
                Title = "Chọn nơi lưu tệp Excel thống kê khen thưởng",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"DanhSach_ThongKeKhenThuong_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                AddExtension = true,
                DefaultExt = "xlsx"
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;
            string filePath = sfd.FileName;
            Cursor.Current = Cursors.WaitCursor;
            if (toolStripProgressBar1_LamMoi != null)
            {
                toolStripProgressBar1_LamMoi.AutoSize = false;
                toolStripProgressBar1_LamMoi.Size = new Size(150, 16);
                toolStripProgressBar1_LamMoi.Style = ProgressBarStyle.Marquee;
                toolStripProgressBar1_LamMoi.Visible = true;
            }
            try
            {
                // 1. Đọc & Nạp DTO kết hợp dữ liệu giữa CSDL2 (DanhSach) và CSDL4 (ThongKe_GiayKhen)
                List<KhenThuongExportDTO> dssDTO = await Task.Run(() => DocDanhSachKhenThuongToanBoDTO());
                if (dssDTO == null || dssDTO.Count == 0)
                {
                    MessageBox.Show("Không có dữ liệu khen thưởng để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                // ⭐ ĐỌC THÔNG TIN TIỂU ĐOÀN VÀ NĂM TỪ CSDL ĐỂ LÀM TIÊU ĐỀ
                var thongTinDonVi = await Task.Run(() => LayThongTinTieuDoan());
                // 2. Ghi file Excel với ClosedXML (Truyền thêm Tên Tiểu Đoàn và Năm)
                await Task.Run(() => GhiFileExcelKhenThuongToanBo(filePath, dssDTO, thongTinDonVi.TenTieuDoan, thongTinDonVi.NamHienTai));
                // 3. Highlight file trong Explorer
                try
                {
                    Module_XuatNhapDuLieuThiDua.MoVaChonTepTrongExplorer(filePath);
                }
                catch
                {
                    Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
                }
            }
            catch (IOException)
            {
                MessageBox.Show("File Excel đang được mở bởi chương trình khác. Vui lòng đóng file và thử lại!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Đã xảy ra lỗi khi xuất file Excel:\n{ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
                if (toolStripProgressBar1_LamMoi != null)
                {
                    toolStripProgressBar1_LamMoi.Style = ProgressBarStyle.Blocks;
                    toolStripProgressBar1_LamMoi.Visible = false;
                }
            }
        }
        private void GhiFileExcelKhenThuongToanBo(string filePath, List<KhenThuongExportDTO> dataList, string tenTieuDoan, string namHienTai)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("ChiTiet_ThongKeKhenThuong");
            // XỬ LÝ CHUỖI TÊN ĐƠN VỊ (Ví dụ: "TIỂU ĐOÀN 2" -> "Tiểu đoàn 2")
            string formattedTenTieuDoan = tenTieuDoan?.Trim().ToLower() ?? "";
            if (formattedTenTieuDoan.Length > 0)
            {
                formattedTenTieuDoan = char.ToUpper(formattedTenTieuDoan[0]) + formattedTenTieuDoan.Substring(1);
            }
            // 1. Font chữ mặc định
            ws.Style.Font.SetFontName(Module_HeThong.Font_Times_New_Roman);
            ws.Style.Font.SetFontSize(11);
            // 2. HEADER TIÊU ĐỀ BÁO CÁO (DÒNG 1, 2, 3)
            // Ô A1: DANH SÁCH
            ws.Cell("A1").Value = "DANH SÁCH";
            ws.Range("A1:N1").Merge()
                .Style.Font.SetBold(true)
                .Font.SetFontSize(16)
                .Font.SetFontColor(XLColor.Blue)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            // Ô A2: Về việc
            string tieuDeChinh = $"Về việc thống kê công tác khen thưởng của {formattedTenTieuDoan} năm {namHienTai}";
            ws.Cell("A2").Value = tieuDeChinh;
            ws.Range("A2:N2").Merge()
                .Style.Font.SetBold(true)
                .Font.SetFontSize(14)
                .Font.SetFontColor(XLColor.Blue)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            // Ô A3: Ngày xuất báo cáo
            ws.Cell("A3").Value = $"Thống kê ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Range("A3:N3").Merge()
                .Style.Font.SetItalic(true)
                .Font.SetFontColor(XLColor.Blue)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            // 3. TIÊU ĐỀ CÁC CỘT (HEADER BẢNG)
            // ⭐ DỜI XUỐNG DÒNG 5 ĐỂ CHỪA TRỐNG DÒNG 4 LÀM KHOẢNG ĐỆM
            int headerRow = 5;
            string[] headers = {"STT", "Họ và tên", "Số hiệu", "Năm sinh", "Cấp bậc", "Chức vụ", "Quê quán",
        "Đơn vị", "Tình trạng", "Hình thức khen", "Quyết định khen",
        "Ngày cấp Quyết định", "Đơn vị khen", "Nội dung khen thưởng"};
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8E8E8");
            }
            ws.Row(headerRow).Height = 40;
            // 4. ĐỔ DỮ LIỆU & GỘP Ô THÔNG MINH
            var danhSachNhomCB = dataList
                .GroupBy(x => new { x.SoHieu, x.HoVaTen, x.NamSinh, x.CapBac, x.ChucVu, x.QueQuan, x.DonVi, x.TinhTrang })
                .ToList();
            // ⭐ DỮ LIỆU BẮT ĐẦU TỪ DÒNG 6 NỐI TIẾP HEADER
            int startRow = 6;
            int sttIndex = 1;
            const int EST_CHAR_PER_LINE = 45; // Ước tính số ký tự tối đa trên 1 dòng của cột Nội dung khen
            foreach (var nhom in danhSachNhomCB)
            {
                int countInGroup = nhom.Count();
                int endRow = startRow + countInGroup - 1;
                // --- A. GHI DỮ LIỆU BÊN PHẢI (CÁC DÒNG CHI TIẾT KHEN THƯỞNG) ---
                int currentDetailRow = startRow;
                foreach (var item in nhom)
                {
                    ws.Cell(currentDetailRow, 10).Value = item.HinhThuc_Khen;
                    ws.Cell(currentDetailRow, 11).Value = item.QuyetDinh_Khen;
                    ws.Cell(currentDetailRow, 12).Value = item.NgayCapQD_Khen;
                    ws.Cell(currentDetailRow, 13).Value = item.DonVi_Khen;
                    var cellNoiDung = ws.Cell(currentDetailRow, 14);
                    string noiDungText = item.VeViec_Khen ?? "";
                    cellNoiDung.Value = noiDungText;
                    cellNoiDung.Style.Alignment.WrapText = true; // Ép tự động xuống dòng
                    // ⭐ THUẬT TOÁN TÍNH CHIỀU CAO DÒNG THÔNG MINH
                    double baseRowHeightPt = 18.0; // Mặc định 24 pixel (24 * 0.75 = 18 Point)
                    int lineCount = 1;
                    if (!string.IsNullOrEmpty(noiDungText))
                    {
                        int explicitLines = noiDungText.Split('\n').Length;
                        int wrappedLines = (int)Math.Ceiling((double)noiDungText.Length / EST_CHAR_PER_LINE);
                        lineCount = Math.Max(explicitLines, wrappedLines);
                    }
                    ws.Row(currentDetailRow).Height = lineCount > 1 ? (baseRowHeightPt * lineCount) : baseRowHeightPt;
                    currentDetailRow++;
                }
                // --- B. NẠP DỮ LIỆU VÀ MERGE (GỘP Ô) CHO CÁC CỘT THÔNG TIN CỐ ĐỊNH ---
                ws.Cell(startRow, 1).Value = sttIndex++;
                ws.Cell(startRow, 2).Value = nhom.Key.HoVaTen;
                ws.Cell(startRow, 3).Value = "'" + nhom.Key.SoHieu; // Thêm ' giữ số 0 đầu
                ws.Cell(startRow, 4).Value = nhom.Key.NamSinh;
                ws.Cell(startRow, 5).Value = nhom.Key.CapBac;
                ws.Cell(startRow, 6).Value = nhom.Key.ChucVu;
                var cellQueQuan = ws.Cell(startRow, 7);
                cellQueQuan.Value = nhom.Key.QueQuan;
                cellQueQuan.Style.Alignment.WrapText = true; // Cho phép Quê quán xuống dòng
                ws.Cell(startRow, 8).Value = nhom.Key.DonVi;
                ws.Cell(startRow, 9).Value = nhom.Key.TinhTrang;
                // GỘP Ô NẾU CÓ TỪ 2 LƯỢT KHEN TRỞ LÊN
                if (countInGroup > 1)
                {
                    for (int col = 1; col <= 9; col++)
                    {
                        ws.Range(startRow, col, endRow, col).Merge();
                    }
                }
                startRow = endRow + 1; // Chuyển sang người tiếp theo
            }
            int lastRow = startRow - 1;
            // 5. CĂN LỀ & KẺ KHUNG (BORDER) CHUẨN HỒ SƠ NGHIỆP VỤ
            var dataGridRange = ws.Range(headerRow, 1, lastRow, 14);
            // Tất cả các ô đều căn TRUNG TÂM theo chiều dọc và GIỮA theo chiều ngang
            dataGridRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            dataGridRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            // Ngoại trừ Cột Họ Tên (Cột 2), Quê Quán (Cột 7) và Nội dung khen (Cột 14) căn TRÁI
            ws.Range(headerRow + 1, 2, lastRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            ws.Range(headerRow + 1, 7, lastRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            ws.Range(headerRow + 1, 14, lastRow, 14).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            // Kẻ viền khung (Border) mỏng màu đen
            dataGridRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataGridRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataGridRange.Style.Border.InsideBorderColor = XLColor.Black;
            dataGridRange.Style.Border.OutsideBorderColor = XLColor.Black;
            // 6. DÒNG TỔNG CỘNG CHÂN BẢNG
            int footerRow = lastRow + 1;
            int soDongChi = danhSachNhomCB.Count; // Số lượng CBCS
            int soLuotKhen = dataList.Count;      // Số lượt khen
            ws.Cell(footerRow, 1).Value = $"Tổng cộng: {soDongChi:00} {Module_HeThong.Tu_dong_chi} (trong đó gồm {soLuotKhen:00} lượt khen thưởng)./.";
            ws.Range(footerRow, 1, footerRow, 14).Merge().Style.Font.SetBold(true).Font.SetItalic(true);
            // 7. CĂN CHỈNH ĐỘ RỘNG CỘT
            ws.Columns(1, 14).AdjustToContents();
            // Khống chế bề rộng tối thiểu để văn bản dài tự động xuống dòng đẹp mắt
            if (ws.Column(2).Width < 18) ws.Column(2).Width = 18;   // Họ tên
            if (ws.Column(7).Width < 22) ws.Column(7).Width = 22;   // Quê quán
            if (ws.Column(11).Width < 25) ws.Column(11).Width = 25; // Số Quyết định
            if (ws.Column(13).Width < 25) ws.Column(13).Width = 25; // Đơn vị khen
            ws.Column(14).Width = 45; // Nội dung khen thưởng
            // Đóng dấu bản quyền phần mềm
            Module_BanQuyen.DongDauExcel(wb);
            wb.SaveAs(filePath);
        }
        private readonly Module_SaoLuuKhoiPhucKhenThuong _moduleSaoLuuKhoiPhuc = new Module_SaoLuuKhoiPhucKhenThuong();
        private void saoLuuToanBoDuLieuKhenThuong_toolStripMenuItem_Click(object? sender, EventArgs e)
        {
            // Gọi phương thức sao lưu từ module
            _moduleSaoLuuKhoiPhuc.saoLuuToanBoDuLieuKhenThuong_toolStripMenuItem_Click(sender, e);
        }
        private async void khoiPhucDuLieuKhenThuong_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            using (Form24_XacMinhAdmin frm = new Form24_XacMinhAdmin())
            {
                frm.TopMost = true;
                frm.StartPosition = FormStartPosition.CenterScreen;
                if (frm.ShowDialog(this) != DialogResult.OK)
                    return;
            }
            if (sender is not ToolStripItem item)
                return;
            item.Enabled = false;
            try
            {
                _moduleSaoLuuKhoiPhuc
                    .khoiPhucDuLieuKhenThuong_ToolStripMenuItem_Click(sender, e);
                await ThucHienLamMoiGiaoDienAsync();
            }
            finally
            {
                item.Enabled = true;
            }
        }
        public async Task ThucHienLamMoiGiaoDienAsync()
        {
            try
            {
                DatLaiGiaTriBoLoc();      // reset cả 3 combo + ô tên, không kích hoạt lọc lẻ
                await ReloadDuLieu();     // tự áp lại bộ lọc đã reset + cập nhật thống kê
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi làm mới giao diện: {ex.Message}");
            }
        }
    }
}