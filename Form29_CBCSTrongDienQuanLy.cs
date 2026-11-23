///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
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
        // Chống treo UI đa luồng
        private int _isUpdatingUI = 0;
        // Điều kiện lọc của lần trước: nếu chỉ gõ thêm chữ vào ô tìm kiếm thì chỉ cần lọc tiếp trên kết quả cũ (nhanh gấp nhiều lần)
        private bool _coKetQuaLoc = false;
        private string _locTenCu = "", _locDonViCu = "", _locPhanLoaiCu = "", _locGhiChuCu = "";
        private int _isLoading = 0;
        // Anti-zombie task: hủy an toàn khi đóng form
        private CancellationTokenSource _cts = new CancellationTokenSource();
        // Cột lưới: thứ tự cố định (cột 0 là STT). CellValueNeeded dùng chỉ số cột -> không tra tên cột mỗi ô.
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
        private static readonly Font THONGKE_FONT = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Bold);
        // Màu thống kê
        private static readonly Color COLOR_LOAI_1 = Color.FromArgb(0, 102, 204);   // xanh dương
        private static readonly Color COLOR_LOAI_2 = Color.FromArgb(204, 153, 0);   // vàng đậm dễ đọc
        private static readonly Color COLOR_LOAI_3 = Color.FromArgb(220, 53, 69);   // đỏ
        private static readonly Color COLOR_LOAI_4 = Color.FromArgb(180, 0, 0);     // đỏ đậm
        private static readonly Color COLOR_KHONG_PL = Color.Black;
        public Form29_CBCSTrongDienQuanLy()
        {
            InitializeComponent();
            Module_BieuTuongNut.ApDung(this);   // biểu tượng nút: Hiện đại = tự vẽ, Cổ điển = ảnh gốc nhúng trong exe
            // Giao diện kiểu web nằm trọn trong module riêng (không đụng Designer, không đổi tên control/sự kiện)
            Module_GiaoDienForm29Web.ApDung(this);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            ShowInTaskbar = false;
            _connStr = Module_KetNoiCSDL.ChuoiKetNoi(_csdl2Path);
            // Icon 16x16: tạo 1 lần
            if (Properties.Resources.ic_khenthuong != null)
                _iconLoai1 = new Bitmap(Properties.Resources.ic_khenthuong, new Size(16, 16));
            // DoubleBuffered cho lưới (giảm nháy khi cuộn)
            try
            {
                typeof(DataGridView).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                  ?.SetValue(kryptonDataGridView2, true, null);
            }
            catch { }
            // Lưới + cột + timer + sự kiện dựng NGAY trong constructor (rẻ, đồng bộ) -> form hiện ra là có khung hoàn chỉnh, dữ liệu đổ vào sau
            TaoCotChoGridView();
            InitSearchTimer();
            InitTimKiem();
            toolTip1.SetToolTip(kryptonButton_LamMoiCacOTimKiem, "Xóa bộ lọc (F4)");
            toolTip1.SetToolTip(kryptonButton1_TimKiemThongTinCBCSQuanLy, "Tìm theo họ tên hoặc số hiệu (Enter)");
            var g = kryptonDataGridView2;
            g.VirtualMode = true;
            g.CellValueNeeded += KryptonDataGridView2_CellValueNeeded;
            g.CellPainting += KryptonDataGridView2_CellPainting;
        }
        private async void Form29_CBCSTrongDienQuanLy_Load(object? sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref _isLoading, 1) == 1)
                return;
            try
            {
                Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1);
                toolStripStatusLabel4.Text = "Đang tải dữ liệu...";
                // Đọc SQLite + giải mã AES hoàn toàn trên luồng nền (không chặn UI)
                if (_dataGoc.Count == 0)
                {
                    _dataGoc = await DocDanhSachNenAsync(_cts.Token);
                    if (IsDisposed || _cts.IsCancellationRequested)
                        return;
                    _coKetQuaLoc = false;
                }
                NapCombo();
                InitFilters();
                ApplyFilter_Virtual();
                int tongSo = _dataGoc.Count;
                toolStripStatusLabel4.Visible = tongSo > 0;
                if (tongSo > 0)
                    toolStripStatusLabel4.Text = $"Tổng cộng {tongSo:N0} {Module_HeThong.Tu_dong_chi}";
                toolStripStatusLabel4.ForeColor = Color.FromArgb(25, 135, 84);
                toolStripStatusLabel4.Font = THONGKE_FONT;
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
        // ===== LÕI DỮ LIỆU: chạy toàn bộ trên luồng nền (đọc SQLite + giải mã AES + gộp chuỗi) =====
        private Task<List<CBCSQuanLyModel>> DocDanhSachNenAsync(CancellationToken token) =>
            Task.Run(() => DocDanhSach(token), token);
        private List<CBCSQuanLyModel> DocDanhSach(CancellationToken token)
        {
            var list = new List<CBCSQuanLyModel>(5000);
            if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path)) return list;
            string sql = $@"SELECT HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu
                         FROM {Module_TaiKhoan.BangDanhSach}
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
                string hoTen = GiaiMaVaGop(0);
                string soHieu = GiaiMaVaGop(1);
                list.Add(new CBCSQuanLyModel
                {
                    HoVaTen = hoTen,
                    SoHieu = soHieu,
                    // Khóa tìm kiếm viết thường dựng 1 lần: lọc chỉ còn 1 phép Contains/dòng, không phải đổi chữ hoa-thường mỗi lần gõ
                    // Họ tên + số hiệu đã BỎ DẤU, chữ thường ("nguyen van" khớp "Nguyễn Văn"); \u0001 chặn khớp vắt ngang 2 trường
                    KhoaTim = BoLocDanhSach.ChuanHoaTimKiem(hoTen) + "\u0001" + BoLocDanhSach.ChuanHoaTimKiem(soHieu),
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
        // ===== BỘ LỌC (1 lượt duyệt) + HIỂN THỊ VIRTUAL MODE =====
        // Giá trị đang chọn của combo, lấy theo CHỈ SỐ chọn chứ không đọc .Text: khi chọn bằng code (ô bo tròn gán SelectedIndex của ComboBox
        // gốc đang ẩn), .Text của ComboBox cập nhật chậm hơn sự kiện SelectedIndexChanged -> bộ lọc đọc phải giá trị cũ nên "không lọc được gì".
        private static string GiaTriChon(ComboBox cb)
        {
            int i = cb.SelectedIndex;
            string? v = (i >= 0 && i < cb.Items.Count) ? cb.GetItemText(cb.Items[i]) : (cb.Text ?? string.Empty);
            return v?.Trim() ?? string.Empty;
        }
        private string LayPhanLoaiLoc(CBCSQuanLyModel x) => string.IsNullOrWhiteSpace(x.PhanLoai) ? Module_HeThong.PL_KHONG_PL : x.PhanLoai;
        private void ApplyFilter_Virtual()
        {
            if (IsDisposed || !IsHandleCreated)
                return;
            try
            {
                string keyword = textBoxKyToon_TimTen.Text.Trim();
                string donVi = GiaTriChon(comboBox_TimKiemDonVi);
                string phanLoai = GiaTriChon(comboBox_XepLoaiThiDua);
                string ghiChu = GiaTriChon(comboBox1_GhiChu);
                if (donVi == Module_HeThong.Tat_Ca) donVi = "";
                if (phanLoai == Module_HeThong.Tat_Ca) phanLoai = "";
                if (ghiChu == Module_HeThong.Tat_Ca) ghiChu = "";
                bool cungCombo = donVi == _locDonViCu && phanLoai == _locPhanLoaiCu && ghiChu == _locGhiChuCu;
                // Điều kiện y hệt lần trước -> kết quả y hệt, không làm gì thêm (vd: gõ xong đợi 300ms rồi bấm Enter / nút Tìm)
                if (_coKetQuaLoc && cungCombo && keyword == _locTenCu)
                    return;
                // Tên/số hiệu KHÔNG phân biệt dấu, hoa/thường; nhiều từ thì từ nào cũng phải có
                string[] tu = Module_BoLocKhenThuong.TachTuKhoa(keyword);
                // BỘ LỌC KÉP (1 lượt): mỗi combo chỉ còn giá trị có trong các dòng thỏa mọi bộ lọc khác
                var tcDv = new Module_BoLocKhenThuong.TieuChiKep<CBCSQuanLyModel>(donVi, x => x.DonVi);
                var tcPl = new Module_BoLocKhenThuong.TieuChiKep<CBCSQuanLyModel>(phanLoai, LayPhanLoaiLoc);
                var tcGc = new Module_BoLocKhenThuong.TieuChiKep<CBCSQuanLyModel>(ghiChu, x => x.GhiChu);
                var chiSo = Module_BoLocKhenThuong.LocKep(_dataGoc,
                    tu.Length == 0 ? null : x => Module_BoLocKhenThuong.KhopTuKhoa(x.KhoaTim, tu), tcDv, tcPl, tcGc);
                var ketQua = new List<CBCSQuanLyModel>(chiSo.Count);
                var cacDonVi = new HashSet<string>(StringComparer.Ordinal);
                int loai1 = 0, loai2 = 0, loai3 = 0, loai4 = 0, khongPL = 0;
                foreach (int i in chiSo)
                {
                    var x = _dataGoc[i];
                    ketQua.Add(x);
                    if (!string.IsNullOrWhiteSpace(x.DonVi)) cacDonVi.Add(x.DonVi);
                    switch (NormalizePhanLoai(x.PhanLoai))
                    {
                        case Module_HeThong.Loai_1: loai1++; break;
                        case Module_HeThong.Loai_2: loai2++; break;
                        case Module_HeThong.Loai_3: loai3++; break;
                        case Module_HeThong.Loai_4: loai4++; break;
                        default: khongPL++; break;
                    }
                }
                Interlocked.Exchange(ref _isUpdatingUI, 1);   // thu hẹp combo không được bắn lọc lại
                try
                {
                    Module_BoLocKhenThuong.NapLaiCombo(comboBox_TimKiemDonVi, Module_BoLocKhenThuong.SapABC(tcDv.Con), Module_HeThong.Tat_Ca);
                    bool coKhongPL = tcPl.Con.Remove(Module_HeThong.PL_KHONG_PL);
                    var dsPl = Module_BoLocKhenThuong.SapABC(tcPl.Con);
                    if (coKhongPL) dsPl.Add(Module_HeThong.PL_KHONG_PL);
                    Module_BoLocKhenThuong.NapLaiCombo(comboBox_XepLoaiThiDua, dsPl, Module_HeThong.Tat_Ca);
                    Module_BoLocKhenThuong.NapLaiCombo(comboBox1_GhiChu, Module_BoLocKhenThuong.SapABC(tcGc.Con), Module_HeThong.Tat_Ca);
                }
                finally { Interlocked.Exchange(ref _isUpdatingUI, 0); }
                bool giongHet = _coKetQuaLoc && _viewData.Count == ketQua.Count && kryptonDataGridView2.RowCount == ketQua.Count;
                for (int i = 0; giongHet && i < ketQua.Count; i++)
                    if (!ReferenceEquals(_viewData[i], ketQua[i])) giongHet = false;
                _viewData = ketQua;
                _coKetQuaLoc = true;
                _locTenCu = keyword; _locDonViCu = donVi; _locPhanLoaiCu = phanLoai; _locGhiChuCu = ghiChu;
                if (!giongHet) CapNhatLuoi(_viewData.Count);   // cùng tập người -> giữ nguyên lưới + vị trí cuộn
                CapNhatThongKeDonVi(_viewData.Count, cacDonVi.Count);
                HienThiThongKe(_viewData.Count, loai1, loai2, loai3, loai4, khongPL);
            }
            catch (Exception ex)
            {
                LogError(nameof(ApplyFilter_Virtual), ex);
            }
        }
        // Đổi số dòng của lưới VirtualMode. Giảm số dòng bằng "RowCount = nhỏ hơn" sẽ gỡ TỪNG dòng một (mỗi dòng phát sinh sự kiện + tính lại
        // thanh cuộn) -> rất chậm với hàng nghìn dòng. Rows.Clear() gỡ một lần; sau đó thêm lại bằng RowCount (thêm hàng loạt, rất nhanh).
        private void CapNhatLuoi(int soDong)
        {
            var grid = kryptonDataGridView2;
            grid.SuspendLayout();
            try
            {
                grid.CurrentCell = null;
                if (soDong < grid.RowCount) grid.Rows.Clear();
                grid.RowCount = soDong;
                if (soDong > 0 && grid.FirstDisplayedScrollingRowIndex > 0) grid.FirstDisplayedScrollingRowIndex = 0;
            }
            finally
            {
                grid.ResumeLayout();
            }
            grid.Invalidate();
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
        // Vẽ icon khen thưởng trước tên người Loại 1.
        // Tự vẽ trọn ô (nền + đường kẻ dưới + icon + chữ) và KHÔNG gọi e.Paint(..., Border): phần Border chuẩn của DataGridView vẽ viền
        // đen theo GridColor (gạch dưới + gạch phải) làm ô này khác các ô còn lại.
        private static readonly SolidBrush _coNen = new SolidBrush(Module_GiaoDienForm29Web.MauODong);
        private static readonly SolidBrush _coNenChon = new SolidBrush(Module_GiaoDienForm29Web.MauOChon);
        private static readonly SolidBrush _coKe = new SolidBrush(Module_GiaoDienForm29Web.MauKeDong);
        private void KryptonDataGridView2_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 1 || _iconLoai1 == null) return;
            if (e.RowIndex >= _viewData.Count) return;
            if (!string.Equals(_viewData[e.RowIndex].PhanLoai, Module_HeThong.Loai_1, StringComparison.OrdinalIgnoreCase)) return;
            var g = e.Graphics!;
            var o = e.CellBounds;
            bool chon = (e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected;
            g.FillRectangle(chon ? _coNenChon : _coNen, o);
            g.FillRectangle(_coKe, o.X, o.Bottom - 1, o.Width, 1);
            int iconSize = 16;
            int xIcon = o.X + 8;
            int yIcon = o.Y + (o.Height - iconSize) / 2;
            g.DrawImageUnscaled(_iconLoai1, xIcon, yIcon);
            string hoTen = e.Value?.ToString() ?? string.Empty;
            if (hoTen.Length > 0)
            {
                int textStartX = xIcon + iconSize + 6;
                var textBounds = new Rectangle(textStartX, o.Y, o.Right - textStartX - 2, o.Height - 1);
                Color textColor = chon ? e.CellStyle!.SelectionForeColor : e.CellStyle!.ForeColor;
                const TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
                TextRenderer.DrawText(g, hoTen, e.CellStyle.Font, textBounds, textColor, flags);
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
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Xuất Excel CBCS trong diện quản lý",
                    $"{soLuong} dòng | Tệp: {Path.GetFileName(filePath)}");
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
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Lỗi xuất Excel CBCS trong diện quản lý", ex.Message);
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
            Module_ExcelAnToan.LuuAnToan(wb, filePath);
        }
        // ===== TÌM KIẾM (debounce) =====
        private void InitSearchTimer()
        {
            if (_searchTimer != null)
                return;
            _searchTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _searchTimer.Tick += SearchTimer_Tick;
        }
        private void SearchTimer_Tick(object? sender, EventArgs e)
        {
            _searchTimer?.Stop();
            ApplyFilter_Virtual();
        }
        // Ô tìm kiếm: gõ -> đợi 300ms rồi lọc; Enter -> lọc ngay. (Chữ gợi ý "Nhập tìm kiếm" do module giao diện vẽ.)
        private void InitTimKiem()
        {
            textBoxKyToon_TimTen.TextChanged += (s, e) =>
            {
                if (Volatile.Read(ref _isUpdatingUI) == 1) return;
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
            foreach (var cb in new[] { comboBox_TimKiemDonVi, comboBox_XepLoaiThiDua, comboBox1_GhiChu })
            {
                cb.SelectedIndexChanged -= ComboFilter_Changed;
                cb.SelectedValueChanged -= ComboFilter_Changed;
                cb.SelectedIndexChanged += ComboFilter_Changed;
                // Dự phòng: nếu ComboBox gốc (đang ẩn) chỉ phát SelectedValueChanged thì vẫn lọc; trùng điều kiện thì ApplyFilter_Virtual tự bỏ qua
                cb.SelectedValueChanged += ComboFilter_Changed;
            }
        }
        private void ComboFilter_Changed(object? sender, EventArgs e)
        {
            if (Volatile.Read(ref _isUpdatingUI) == 1) return;   // đang đặt lại hàng loạt: người gọi sẽ lọc đúng 1 lần
            ApplyFilter_Virtual();
        }
        // ===== LƯỚI: định dạng cột dựng 1 lần ở constructor (màu sắc/kiểu dáng do Module_GiaoDienForm29Web đảm nhiệm) =====
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
                grid.AllowUserToOrderColumns = false;
                grid.ReadOnly = true;
                grid.RowHeadersVisible = false;
                grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                grid.MultiSelect = false;
                grid.ScrollBars = ScrollBars.Vertical;
                var cacCot = new DataGridViewColumn[CotLuoi.Length + 1];
                cacCot[0] = new DataGridViewTextBoxColumn
                {
                    Name = "STT",
                    HeaderText = "STT",
                    Width = 56,
                    ReadOnly = true,
                    Frozen = true,
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
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
                            Alignment = d.Trai ? DataGridViewContentAlignment.MiddleLeft : DataGridViewContentAlignment.MiddleCenter
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
        // ===== COMBO LỌC =====
        private List<string> TaoDanhSachLoc(Func<CBCSQuanLyModel, string> chon, string? them)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var x in _dataGoc)
            {
                string v = chon(x);
                if (!string.IsNullOrWhiteSpace(v)) set.Add(v);
            }
            var list = set.OrderBy(v => v).ToList();
            list.Insert(0, Module_HeThong.Tat_Ca);
            if (them != null) list.Add(them);
            return list;
        }
        // Nạp lại 3 combo; đặt cờ _isUpdatingUI để việc nạp không kích hoạt lọc lặp.
        // Nạp bằng Items (KHÔNG dùng DataSource): ComboBox gốc đang bị ẩn dưới ô bo tròn, và ComboBox gắn DataSource giữ vị trí riêng trong
        // CurrencyManager -> khi ô bo tròn gán SelectedIndex bằng code, vị trí này kéo lựa chọn về dòng đầu ("Tất cả") và bộ lọc không đổi.
        private void NapCombo()
        {
            Interlocked.Exchange(ref _isUpdatingUI, 1);
            try
            {
                NapDanhSachCombo(comboBox_TimKiemDonVi, TaoDanhSachLoc(x => x.DonVi, null));
                NapDanhSachCombo(comboBox_XepLoaiThiDua, TaoDanhSachLoc(x => x.PhanLoai, Module_HeThong.PL_KHONG_PL));
                NapDanhSachCombo(comboBox1_GhiChu, TaoDanhSachLoc(x => x.GhiChu, null));
            }
            finally
            {
                Interlocked.Exchange(ref _isUpdatingUI, 0);
            }
        }
        private static void NapDanhSachCombo(ComboBox cb, List<string> ds)
        {
            cb.BeginUpdate();
            try
            {
                cb.DataSource = null;
                cb.Items.Clear();
                cb.Items.AddRange(ds.ToArray());
            }
            finally
            {
                cb.EndUpdate();
            }
            // Chọn lại dòng đầu theo 2 bước để chắc chắn phát sự kiện -> ô bo tròn vẽ lại theo danh sách mới (bộ lọc đang bị cờ chặn)
            cb.SelectedIndex = -1;
            if (cb.Items.Count > 0) cb.SelectedIndex = 0;
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
        private void kryptonButton1_TimKiemThongTinCBCSQuanLy_Click(object? sender, EventArgs e)
        {
            _searchTimer?.Stop();
            ApplyFilter_Virtual();
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
                _searchTimer?.Stop();
                ResetUI();
                if (_cts.IsCancellationRequested)
                {
                    _cts.Dispose();
                    _cts = new CancellationTokenSource();
                }
                _dataGoc = await DocDanhSachNenAsync(_cts.Token);
                if (IsDisposed) return;
                _coKetQuaLoc = false;   // dữ liệu mới -> bắt buộc lọc lại từ đầu
                frmLoad.CapNhatThongBao("Đang cập nhật giao diện...");
                NapCombo();
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
                if (!IsDisposed)
                {
                    Enabled = true;
                    Focus();
                }
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
        private void xoaTimKiem_ToolStripMenuItem_Click(object? sender, EventArgs e) => XoaBoLoc();
        private void kryptonButton_LamMoiCacOTimKiem_Click(object? sender, EventArgs e) => XoaBoLoc();
        // Xóa bộ lọc: dừng timer, đặt lại ô nhập + combo về "Tất cả", lọc lại đúng 1 lần
        private void XoaBoLoc()
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
                }
                _iconLoai1?.Dispose();
                _iconLoai1 = null;
            }
            catch
            {
            }
            base.OnFormClosing(e);
        }
        // ===== THỐNG KÊ PHÂN LOẠI (đếm cùng lúc với lọc, không duyệt lại) =====
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
        private void HienThiThongKe(int tong, int loai1, int loai2, int loai3, int loai4, int khongPL)
        {
            statusStrip1.SuspendLayout();   // 5 nhãn đổi cùng lúc -> chỉ bố cục lại thanh trạng thái 1 lần
            try
            {
                if (tong == 0)
                {
                    ResetThongKeLabels();
                    return;
                }
                UpdateThongKeLabel(toolStripStatusLabel_TongSoLoai1, loai1, Module_HeThong.Loai_1, COLOR_LOAI_1);
                UpdateThongKeLabel(toolStripStatusLabel_TongSoLoai2, loai2, Module_HeThong.Loai_2, COLOR_LOAI_2);
                UpdateThongKeLabel(toolStripStatusLabel_TongSoLoai3, loai3, Module_HeThong.Loai_3, COLOR_LOAI_3);
                UpdateThongKeLabel(toolStripStatusLabel_TongSoLoai4, loai4, Module_HeThong.Loai_4, COLOR_LOAI_4);
                UpdateThongKeLabel(toolStripStatusLabel_TongSoKhongPhanLoai, khongPL, Module_HeThong.PL_KHONG_PL, COLOR_KHONG_PL);
            }
            finally
            {
                statusStrip1.ResumeLayout();
            }
        }
        private static void UpdateThongKeLabel(ToolStripStatusLabel label, int count, string title, Color color)
        {
            if (count <= 0)
            {
                label.Visible = false;
                return;
            }
            label.Visible = true;
            label.Text = $"{title}: {count:N0} {Module_HeThong.Tu_dong_chi}";
            if (label.ForeColor != color) label.ForeColor = color;
            if (!ReferenceEquals(label.Font, THONGKE_FONT)) label.Font = THONGKE_FONT;
            if (label.TextAlign != ContentAlignment.MiddleCenter) label.TextAlign = ContentAlignment.MiddleCenter;
            if (label.AutoSize) label.AutoSize = false;
            if (label.Width != 160) label.Width = 160;
        }
        private void ResetThongKeLabels()
        {
            toolStripStatusLabel_TongSoLoai1.Visible = false;
            toolStripStatusLabel_TongSoLoai2.Visible = false;
            toolStripStatusLabel_TongSoLoai3.Visible = false;
            toolStripStatusLabel_TongSoLoai4.Visible = false;
            toolStripStatusLabel_TongSoKhongPhanLoai.Visible = false;
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
        // Khóa tìm kiếm (họ tên + số hiệu, viết thường) dựng sẵn lúc đọc dữ liệu
        public string KhoaTim { get; set; } = string.Empty;
    }
}