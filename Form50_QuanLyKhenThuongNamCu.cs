///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
///Phiên bản 2
///
///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form50_QuanLyKhenThuongNamCu : Form
    {
        // KHAI BÁO DTO VÀ BIẾN TOÀN CỤC CHO KHEN THƯỞNG CÁ NHÂN
        private class HistoryGiayKhenDTO
        {
            public int ID { get; set; }
            public int STT { get; set; }
            public string HoVaTen { get; set; } = null!;
            public string HoVaTen_Search { get; set; } = null!;
            public string SoHieu { get; set; } = null!;
            public string DonVi { get; set; } = null!;
            public string TinhTrang { get; set; } = null!;
            public string HinhThuc_Khen { get; set; } = null!;
            public string QuyetDinh_Khen { get; set; } = null!;
            public string NgayCapQD_Khen { get; set; } = null!;
            public string DonVi_Khen { get; set; } = null!;
            public string VeViec_Khen { get; set; } = null!;
            public string GhiChu_Khen { get; set; } = null!;
            public int SortPriority { get; set; }
        }
        //Hệ thống production:
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private List<HistoryGiayKhenDTO> _dataCacheGiayKhen = new List<HistoryGiayKhenDTO>();
        private List<int> _filteredIndexes = new List<int>();
        private const string PLACEHOLDER_TIMKIEM = "Nhập họ và tên để tìm kiếm...";
        private bool _dangSetPlaceholder = false;
        private bool _isInitialized = false;
        private SolidBrush? _rowHeaderBrush = null;
        private StringFormat? _rowHeaderFormat = null;
        private System.Windows.Forms.Timer _timKiemTimer = null!;
        // Mỗi lượt tải +1: lượt cũ về muộn (đổi năm liên tục) thì bỏ, không ghi đè dữ liệu của năm vừa chọn
        private int _phienTai;
        // Tệp + dấu vết (tệp năm cũ + CSDL hiện tại) của lần tải xong gần nhất: mở lại trang mà không có gì đổi thì KHÔNG đọc lại
        private string _duongDanDaTai = "";
        private string _dauVetDaTai = "";
        private bool _dangNapCombo;                       // đang nạp lại combo lọc -> bỏ qua SelectedIndexChanged
        private string[] _thuTuDonVi = Array.Empty<string>();
        // Quản lý Font tập trung dùng biến hệ thống, chống rò rỉ GDI handle
        private static readonly Font _fontGridHeader95Bold = new Font(Module_HeThong.TenFontHeThong, 9.5F, FontStyle.Bold);
        private static readonly Font _fontGridCell95Regular = new Font(Module_HeThong.TenFontHeThong, 9.5F, FontStyle.Regular);
        // FONT DÙNG CHUNG CHO FORM ẢO CHI TIẾT
        //private static readonly Font _fontTitle115Bold = new Font(Module_HeThong.TenFontHeThong, 11.5F, FontStyle.Bold);
        private static readonly Font _fontGrid10Regular = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Regular);
        private static readonly Font _fontGrid10Bold = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Bold);
        // private static readonly Font _fontBtn95Bold = new Font(Module_HeThong.TenFontHeThong, 9.5F, FontStyle.Bold);
        //const string ROW_GROUP = "__GROUP__";
        public Form50_QuanLyKhenThuongNamCu()
        {
            InitializeComponent();
            Module_CheDoDoiTuong.ApNhanTheoCheDo(this);   // nhãn "CBCS" -> "Tân binh"/"Học viên" theo chế độ (trước khi dựng giao diện hiện đại)
            Module_BieuTuongNut.ApDung(this);   // biểu tượng nút: Hiện đại = tự vẽ, Cổ điển = ảnh gốc nhúng trong exe
            Module_GiaoDienForm50Web.ApDung(this);   // giao diện kiểu web như Form4 - chỉ đổi trình bày (đây là dòng DUY NHẤT thêm vào code gốc)
            this.KeyPreview = true;
            this.VisibleChanged += Form50_VisibleChanged;
            kryptonDataGridView1.VirtualMode = true;
            kryptonDataGridView1.DataSource = null;
            kryptonDataGridView1.AllowUserToAddRows = false;
            kryptonDataGridView1.AllowUserToDeleteRows = false;
            kryptonDataGridView1.AllowUserToResizeRows = false;
            kryptonDataGridView1.AllowUserToOrderColumns = false;
            kryptonDataGridView1.ReadOnly = true;
            // ⭐ THÊM TẠI ĐÂY: Bật chế độ bấm vào là chọn nguyên cả dòng
            kryptonDataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            // ⭐ THÊM TẠI ĐÂY: Chỉ cho phép chọn 1 dòng duy nhất tại 1 thời điểm (Tắt tính năng quét chọn nhiều dòng)
            kryptonDataGridView1.MultiSelect = false;
            // ⭐ SỬA TẠI ĐÂY: Chuyển từ Fill thành None
            kryptonDataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            kryptonDataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            kryptonDataGridView1.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            typeof(DataGridView).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(kryptonDataGridView1, true, null);
            kryptonDataGridView1.CellValueNeeded += KryptonDataGridView1_CellValueNeeded;
            kryptonDataGridView1.RowPostPaint += KryptonDataGridView1_RowPostPaint;
            kryptonDataGridView1.CellDoubleClick -= KryptonDataGridView1_CellDoubleClick;
            kryptonDataGridView1.CellDoubleClick += KryptonDataGridView1_CellDoubleClick;
            // Form51 (tập thể) / Form34 (lưu năm) xóa hoặc ghi đè tệp năm cũ -> danh sách năm + lưới ở đây khớp theo
            Module_BoLocKhenThuong.KhoNamCuThayDoi += KhiKhoNamCuThayDoi;
            Disposed += (_, _) => Module_BoLocKhenThuong.KhoNamCuThayDoi -= KhiKhoNamCuThayDoi;
        }
        private void KhiKhoNamCuThayDoi(string duongDan)
        {
            if (IsDisposed || !_isInitialized) return;
            _dauVetDaTai = "";               // ép đọc lại ở lần làm mới kế tiếp
            if (Visible) LamMoiTheoKho();    // đang ẩn: lần hiện sau (VisibleChanged) tự quét lại
        }
        private static string DauVetHienTai(string duongDan)
            => Module_BoLocKhenThuong.DauVetTep(duongDan) + "|" + Module_ChuKyCSDL.Lay();
        /// <summary>
        /// Quét lại danh sách năm (giữ năm đang chọn nếu còn) rồi chỉ tải lại khi năm đổi hoặc tệp / CSDL hiện tại đã đổi.
        /// Bản cũ: mỗi lần hiện trang là đọc + đồng bộ + giải mã lại cả tệp năm cũ kèm cửa sổ chờ, dù không có gì thay đổi.
        /// </summary>
        private void LamMoiTheoKho()
        {
            LoadDanhSachFileLichSu();
            string dang = (comboBox_ChonCSDLNam.SelectedItem as FileLichSuDTO)?.DuongDan ?? "";
            if (dang.Length == 0) return;    // không còn năm nào: LoadDanhSachFileLichSu đã dọn lưới
            if (!string.Equals(dang, _duongDanDaTai, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(DauVetHienTai(dang), _dauVetDaTai, StringComparison.Ordinal))
                _ = ThucHienTaiDuLieuLichSuAsync();
        }
        private void Form50_ThongKeKhenThuongNamCu_Load(object? sender, EventArgs e)
        {
            if (_isInitialized) return;
            CauHinhStatusStrip(); // ⭐ GỌI CẤU HÌNH STATUSSTRIP TẠI ĐÂY
            Module_DonVi.KhoiTao();
            CauHinhGridCoBan(kryptonDataGridView1);
            CauHinhCotGrid(kryptonDataGridView1);
            kryptonDataGridView1.ContextMenuStrip = contextMenuStrip1;
            Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1);
            if (comboBox_ChonCSDLNam != null)
            {
                comboBox_ChonCSDLNam.SelectedIndexChanged -= comboBox_ChonCSDLNam_SelectedIndexChanged;
                comboBox_ChonCSDLNam.SelectedIndexChanged += comboBox_ChonCSDLNam_SelectedIndexChanged;
            }
            // Bản trước: "+=" nằm NGOÀI if (thụt lề đánh lừa) => gom "-=" / "+=" vào cùng khối như ý định gốc.
            if (comboBox_TimKiemDonVi != null)
            {
                comboBox_TimKiemDonVi.SelectedIndexChanged -= ComboBoxFilter_SelectedIndexChanged;
                comboBox_TimKiemDonVi.SelectedIndexChanged += ComboBoxFilter_SelectedIndexChanged;
            }
            if (comboBox1_HinhThucKT != null)
            {
                comboBox1_HinhThucKT.SelectedIndexChanged -= ComboBoxFilter_SelectedIndexChanged;
                comboBox1_HinhThucKT.SelectedIndexChanged += ComboBoxFilter_SelectedIndexChanged;
            }
            InitPlaceholderTimKiem();
            if (textBox_TimKiemTheoTen != null)
            {
                textBox_TimKiemTheoTen.TextChanged += textBox_TimKiemTheoTen_TextChanged;
                textBox_TimKiemTheoTen.Enter += TextBox_TimKiemTheoTen_Enter;
                textBox_TimKiemTheoTen.Leave += TextBox_TimKiemTheoTen_Leave;
            }
            _timKiemTimer = new System.Windows.Forms.Timer();
            _timKiemTimer.Interval = 300;
            _timKiemTimer.Tick += (s, ev) =>
            {
                _timKiemTimer.Stop();
                ApplyFilter();
            };
            LoadDanhSachFileLichSu();
            InitToolTips();
            _isInitialized = true;
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
                GanToolTipAnToan(
                    kryptonButton_LamMoiCacOTimKiem,
                    "Xóa bộ lọc tìm kiếm hiện tại");
                GanToolTipAnToan(
                    kryptonButton_CapNhat,
                    "Tải lại dữ liệu lịch sử");
                GanToolTipAnToan(
                    kryptonButton_XuatData,
                    "Xuất dữ liệu ra tệp Excel");
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
        private void Form50_VisibleChanged(object? sender, EventArgs e)
        {
            if (this.Visible && _isInitialized) LamMoiTheoKho();
        }
        private string SafeDecrypt(object? value)
        {
            if (value == null || value == DBNull.Value) return "";
            string s = value.ToString()?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(s)) return "";
            try
            {
                string decrypted = Module_BaoMatAES.GiaiMa(s);
                return string.IsNullOrEmpty(decrypted) ? s : decrypted;
            }
            catch { return s; }
        }
        public void LoadDanhSachFileLichSu()
        {
            if (comboBox_ChonCSDLNam == null) return;
            string namDangChon = (comboBox_ChonCSDLNam.SelectedItem as FileLichSuDTO)?.DuongDan ?? "";
            comboBox_ChonCSDLNam.SelectedIndexChanged -= comboBox_ChonCSDLNam_SelectedIndexChanged;
            // Lấy danh sách thô
            var danhSach = Module_HoTroLuuDataTheoNamCu.LayDanhSachFileLichSu_KhenThuongCaNhan();
            // ⭐ CHUẨN KỸ SƯ: Lọc bỏ ngay các tệp không có bảng ThongKe_GiayKhen hoặc đã bị xóa sạch (0 dòng)
            if (danhSach != null && danhSach.Count > 0)
            {
                danhSach = danhSach.Where(f => KiemTraTonTaiDuLieuKhenThuong(f.DuongDan)).ToList();
            }
            if (danhSach != null && danhSach.Count > 0)
            {
                var dictTrungTen = new Dictionary<string, int>();
                foreach (var file in danhSach)
                {
                    if (dictTrungTen.ContainsKey(file.TenHienThi))
                    {
                        dictTrungTen[file.TenHienThi]++;
                        file.TenHienThi = $"{file.TenHienThi} ({dictTrungTen[file.TenHienThi]})";
                    }
                    else
                    {
                        dictTrungTen.Add(file.TenHienThi, 1);
                    }
                }
            }
            comboBox_ChonCSDLNam.DisplayMember = "TenHienThi";
            comboBox_ChonCSDLNam.ValueMember = "DuongDan";
            comboBox_ChonCSDLNam.DataSource = danhSach;
            // Giữ nguyên năm đang xem nếu tệp đó vẫn còn (bản cũ: quét lại là nhảy về năm đầu danh sách)
            if (danhSach != null && namDangChon.Length > 0)
            {
                int idx = danhSach.FindIndex(x => string.Equals(x.DuongDan, namDangChon, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0 && comboBox_ChonCSDLNam.SelectedIndex != idx) comboBox_ChonCSDLNam.SelectedIndex = idx;
            }
            comboBox_ChonCSDLNam.SelectedIndexChanged += comboBox_ChonCSDLNam_SelectedIndexChanged;
            if (danhSach == null || danhSach.Count == 0)
            {
                _phienTai++;                  // hủy lượt tải đang dở (nếu có) của tệp vừa biến mất
                _duongDanDaTai = _dauVetDaTai = "";
                _filteredIndexes = new List<int>();
                _dataCacheGiayKhen = new List<HistoryGiayKhenDTO>();
                Module_LuoiAo.DatSoDong(kryptonDataGridView1, 0, boChon: true);
                ApplyFilter();                // combo lọc về "Tất cả" rỗng, nhãn trạng thái cập nhật
            }
            CapNhatTieuDeTheoNamDuocChon();
        }
        private async void toolStripMenuItem_XoaDuLieuNam_Click(object? sender, EventArgs e)
        {
            // 1. KIỂM TRA XEM ĐÃ CHỌN TỆP NĂM CŨ CHƯA
            if (comboBox_ChonCSDLNam.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn tệp CSDL năm cũ trên danh sách trước khi thao tác!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var selectedFile = (FileLichSuDTO)comboBox_ChonCSDLNam.SelectedItem;
            string dbPathLichSu = selectedFile.DuongDan;
            if (!File.Exists(dbPathLichSu))
            {
                MessageBox.Show("Tệp CSDL không tồn tại trên ổ đĩa!", Module_HeThong.TieuDe_Loi, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                int tongSoDong = 0;
                using (var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(dbPathLichSu)))
                {
                    await conn.OpenAsync();
                    using (var cmdCheck = new SqliteCommand($"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{Module_CheDoDoiTuong.BangGiayKhen}'", conn))
                    {
                        if (Convert.ToInt32(await cmdCheck.ExecuteScalarAsync()) == 0)
                        {
                            MessageBox.Show("Tệp CSDL này không chứa cấu trúc bảng Giấy khen!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }
                    using (var cmd = new SqliteCommand($"SELECT COUNT(*) FROM {Module_CheDoDoiTuong.BangGiayKhen}", conn))
                    {
                        tongSoDong = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }
                }
                if (tongSoDong == 0)
                {
                    MessageBox.Show($"Hiện tại không có dữ liệu giấy khen nào trong tệp {selectedFile.TenHienThi} để xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi truy xuất kiểm tra dữ liệu: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            using (Form24_XacMinhAdmin frmXacMinh = new Form24_XacMinhAdmin())
            {
                frmXacMinh.TopMost = true;
                frmXacMinh.StartPosition = FormStartPosition.CenterScreen;
                if (frmXacMinh.ShowDialog(this) != DialogResult.OK) return;
            }
            DialogResult result = MessageBox.Show(
            $"Xóa toàn bộ dữ liệu giấy khen của: {selectedFile.TenHienThi}?\nLưu ý: Hành động này không thể khôi phục!",
            "Cảnh báo xóa (Quyền Admin)",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes) return;
            try
            {
                using (var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(dbPathLichSu)))
                {
                    await conn.OpenAsync();
                    using (var tran = conn.BeginTransaction())
                    {
                        try
                        {
                            using (var cmdDelete = new SqliteCommand($"DELETE FROM {Module_CheDoDoiTuong.BangGiayKhen}", conn, tran))
                            {
                                await cmdDelete.ExecuteNonQueryAsync();
                            }
                            using (var cmdCheckSeq = new SqliteCommand("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='sqlite_sequence'", conn, tran))
                            {
                                int hasSequenceTable = Convert.ToInt32(await cmdCheckSeq.ExecuteScalarAsync());
                                if (hasSequenceTable > 0)
                                {
                                    using (var cmdResetSeq = new SqliteCommand($"UPDATE sqlite_sequence SET seq = 0 WHERE name = '{Module_CheDoDoiTuong.BangGiayKhen}'", conn, tran))
                                    {
                                        await cmdResetSeq.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                            // Tệp năm cũ không có bảng tổng thì bỏ qua (bản cũ: lệnh UPDATE lỗi => hoàn tác luôn cả lệnh xóa giấy khen)
                            using (var cmdCoBang = new SqliteCommand($"SELECT 1 FROM sqlite_master WHERE type='table' AND name='{Module_CheDoDoiTuong.BangKhenThuongNguoi}' LIMIT 1", conn, tran))
                            {
                                if (await cmdCoBang.ExecuteScalarAsync() != null)
                                {
                                    using var cmdSync = new SqliteCommand($"UPDATE {Module_CheDoDoiTuong.BangKhenThuongNguoi} SET SoLuong_Khen = '0'", conn, tran);
                                    await cmdSync.ExecuteNonQueryAsync();
                                }
                            }
                            tran.Commit();
                        }
                        catch
                        {
                            tran.Rollback();
                            throw;
                        }
                    }
                }
                try
                {
                    Module_NhatKy.GhiNhatKy(
                        Module_TaiKhoan.TenTaiKhoan_RAM,
                        $"Xóa TOÀN BỘ dữ liệu chi tiết giấy khen ({selectedFile.TenHienThi})",
                        $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                    );
                }
                catch (Exception logEx) { System.Diagnostics.Debug.WriteLine("Lỗi ghi nhật ký: " + logEx.Message); }
                //  MessageBox.Show($"✔ Đã xóa sạch toàn bộ dữ liệu giấy khen của {selectedFile.TenHienThi} thành công!", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // ⭐ THAY ĐỔI QUAN TRỌNG: Gọi lại hàm LoadDanhSachFileLichSu để ComboBox quét và vứt bỏ tệp vừa xóa sạch (0 dòng)
                // Báo chung cho cả Form50 + Form51 (cùng tệp năm cũ): trang này quét lại danh sách năm (năm vừa xóa sạch bị loại,
                // tự chuyển sang năm còn dữ liệu hoặc dọn lưới), Form51 đọc lại tệp đó ở lần hiện kế tiếp.
                // Bảng tập thể (Form51) của năm này cũng trống => xóa hẳn tệp, không để rác trong LuuTruThiDua_LichSu
                Module_HoTroLuuDataTheoNamCu.XoaTepLichSuNeuRong(dbPathLichSu);
                Module_BoLocKhenThuong.BaoKhoNamCuThayDoi(dbPathLichSu);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi trong quá trình xóa dữ liệu:\n\n" + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Năm còn giấy khen cá nhân? (cùng hàm Form51 dùng cho bảng tập thể => 2 trang ẩn năm theo đúng 1 quy tắc)
        private bool KiemTraTonTaiDuLieuKhenThuong(string dbPath)
            => Module_BoLocKhenThuong.CoDuLieuBang(dbPath, Module_CheDoDoiTuong.BangGiayKhen);
        private async Task ThucHienTaiDuLieuLichSuAsync()
        {
            if (comboBox_ChonCSDLNam.SelectedItem == null)
            {
                _phienTai++;
                _filteredIndexes = new List<int>();
                Module_LuoiAo.DatSoDong(kryptonDataGridView1, 0, boChon: true);
                CapNhatTrangThaiHienThi();
                return;
            }
            if (comboBox_ChonCSDLNam.SelectedItem is FileLichSuDTO selectedFile)
            {
                int phien = ++_phienTai;
                Form_Loading frmLoad = new Form_Loading("Đang đồng bộ và nạp dữ liệu năm cũ...");
                frmLoad.Icon = this.Icon;
                frmLoad.Show(this);
                this.Enabled = false;
                try
                {
                    // ⭐ CHUẨN KỸ SƯ: GỌI ĐỒNG BỘ TRẠNG THÁI TRƯỚC KHI ĐỌC VÀO RAM
                    // Hệ thống sẽ so khớp với CSDL hiện tại và lưu chữ "Đang công tác / Chuyển công tác" 
                    // xuống file SQLite năm cũ trước khi ta quét nó lên lưới.
                    await DongBoTinhTrangGiayKhenNamCuAsync(selectedFile.DuongDan);
                    if (phien != _phienTai || IsDisposed) return;   // đã chọn năm khác trong lúc đồng bộ
                    // Đọc vào danh sách MỚI: lưới vẫn vẽ từ danh sách cũ cho tới khi đổi sang (bản cũ xóa danh sách đang hiển thị
                    // rồi mới đọc => lưới vẽ dở trỏ vào chỉ số không còn)
                    var moi = new List<HistoryGiayKhenDTO>();
                    string[] donViUuTien = Module_DonVi.LayDanhSachDonViUuTienArray();
                    var dicDonViUuTien = new Dictionary<string, int>(Module_DonVi.SoSanhBang);
                    for (int i = 0; i < donViUuTien.Length; i++) dicDonViUuTien.TryAdd(donViUuTien[i], i);
                    // Đẩy quá trình đọc DB và giải mã AES xuống luồng nền (Background Thread) để không đơ UI
                    await Task.Run(async () =>
                    {
                        using (var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(selectedFile.DuongDan, chiDoc: true)))
                        {
                            await cn.OpenAsync();
                            // Kiểm tra an toàn xem bảng có tồn tại không trước khi Query
                            string checkTableSql = $"SELECT count(*) FROM sqlite_master WHERE type='table' AND name='{Module_CheDoDoiTuong.BangGiayKhen}'";
                            using (var cmdCheck = new SqliteCommand(checkTableSql, cn))
                            {
                                long count = (long)(await cmdCheck.ExecuteScalarAsync())!;
                                if (count == 0) return;
                            }
                            string query = $"SELECT * FROM {Module_CheDoDoiTuong.BangGiayKhen}";
                            using (var cmd = new SqliteCommand(query, cn))
                            using (var rd = await cmd.ExecuteReaderAsync())
                            {
                                var availableColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                for (int i = 0; i < rd.FieldCount; i++)
                                    availableColumns.Add(rd.GetName(i));
                                // Lambda giải mã an toàn
                                Func<string, string> GetStringVal = (colName) =>
                                {
                                    if (!availableColumns.Contains(colName)) return "";
                                    int idx = rd.GetOrdinal(colName);
                                    return rd.IsDBNull(idx) ? "" : SafeDecrypt(rd.GetValue(idx));
                                };
                                // Lambda lấy chuỗi thuần (Dành cho Tình trạng)
                                Func<string, string> GetRawStringVal = (colName) =>
                                {
                                    if (!availableColumns.Contains(colName)) return "";
                                    int idx = rd.GetOrdinal(colName);
                                    return rd.IsDBNull(idx) ? "" : rd.GetValue(idx).ToString()!;
                                };
                                // Lambda lấy số nguyên
                                Func<string, int> GetIntVal = (colName) =>
                                {
                                    if (!availableColumns.Contains(colName)) return 0;
                                    int idx = rd.GetOrdinal(colName);
                                    return rd.IsDBNull(idx) ? 0 : Convert.ToInt32(rd.GetValue(idx));
                                };
                                int currentSTT = 1;
                                while (await rd.ReadAsync())
                                {
                                    var item = new HistoryGiayKhenDTO
                                    {
                                        ID = GetIntVal("ID"),
                                        STT = currentSTT++,
                                        HoVaTen = GetStringVal("HoVaTen"),
                                        SoHieu = GetStringVal("SoHieu"),
                                        DonVi = GetStringVal("DonVi"),
                                        TinhTrang = GetRawStringVal("TinhTrang"), // Đọc Tình trạng thuần (đã đồng bộ ở trên)
                                        HinhThuc_Khen = GetStringVal("HinhThuc_Khen"),
                                        QuyetDinh_Khen = GetStringVal("QuyetDinh_Khen"),
                                        NgayCapQD_Khen = GetStringVal("NgayCapQD_Khen"),
                                        DonVi_Khen = GetStringVal("DonVi_Khen"),
                                        VeViec_Khen = GetStringVal("VeViec_Khen"),
                                        GhiChu_Khen = GetStringVal("GhiChu_Khen")
                                    };
                                    item.HoVaTen_Search = BoLocDanhSach.ChuanHoaTimKiem(item.HoVaTen);   // bỏ dấu: "nguyen" khớp "Nguyễn"
                                    item.SortPriority = dicDonViUuTien.TryGetValue(item.DonVi, out int p) ? p : int.MaxValue;
                                    moi.Add(item);
                                }
                            }
                        }
                        // Sắp xếp lại dữ liệu trên RAM
                        moi = moi.OrderBy(x => x.SortPriority).ThenBy(x => x.ID).ToList();
                        for (int i = 0; i < moi.Count; i++) moi[i].STT = i + 1;
                    });
                    if (phien != _phienTai || IsDisposed) return;   // lượt tải cũ về muộn: bỏ
                    _dataCacheGiayKhen = moi;
                    _filteredIndexes = new List<int>();             // buộc ApplyFilter đặt lại số dòng theo dữ liệu mới
                    _duongDanDaTai = selectedFile.DuongDan;
                    _dauVetDaTai = DauVetHienTai(selectedFile.DuongDan);
                    KhoiTaoBoLocComboBoxTuRAM();
                    ApplyFilter();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Lỗi đọc CSDL Khen thưởng cá nhân năm cũ: " + ex.Message);
                    MessageBox.Show("Có lỗi xảy ra khi đọc tệp dữ liệu năm cũ:\n" + ex.Message, Module_HeThong.TieuDe_Loi, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    frmLoad.Close();
                    this.Enabled = true;
                    this.Focus();
                }
            }
        }
        // Năm mới nạp: giá trị lọc đang chọn không còn trong năm đó thì về "Tất cả" (còn thì giữ). Danh sách từng combo do
        // ApplyFilter dựng theo BỘ LỌC KÉP. Thứ tự đơn vị: theo thứ tự ưu tiên của hệ thống trước, đơn vị phát sinh xếp ABC sau.
        private void KhoiTaoBoLocComboBoxTuRAM()
        {
            _thuTuDonVi = Module_DonVi.LayDanhSachDonViUuTienArray()
                                      .Where(s => !string.IsNullOrWhiteSpace(s))
                                      .ToArray();
            _dangNapCombo = true;
            try
            {
                VeTatCaNeuKhongCon(comboBox_TimKiemDonVi, _dataCacheGiayKhen.Select(x => x.DonVi));
                VeTatCaNeuKhongCon(comboBox1_HinhThucKT, _dataCacheGiayKhen.Select(x => x.HinhThuc_Khen));
            }
            finally { _dangNapCombo = false; }
        }
        private static void VeTatCaNeuKhongCon(ComboBox? cb, IEnumerable<string> giaTriCuaNam)
        {
            string dang = Module_BoLocKhenThuong.GiaTriLoc(cb);
            if (cb == null || dang.Length == 0) return;
            if (!giaTriCuaNam.Any(v => string.Equals(v, dang, StringComparison.OrdinalIgnoreCase))) cb.SelectedIndex = 0;
        }
        private static bool GiaTriHopLe(string? s) => !string.IsNullOrWhiteSpace(s) && s != "[Lỗi giải mã]";
        /// <summary>
        /// Lọc trên RAM 1 lượt duy nhất (họ tên không dấu nhiều từ + đơn vị + hình thức) và đồng thời dựng BỘ LỌC KÉP:
        /// combo Đơn vị chỉ còn đơn vị có giấy khen thuộc hình thức đang chọn, combo Hình thức chỉ còn hình thức đơn vị đang chọn có.
        /// Tập dòng không đổi thì không đụng tới lưới (giữ dòng đang chọn + vị trí cuộn).
        /// </summary>
        private void ApplyFilter()
        {
            if (_dangNapCombo || IsDisposed) return;
            _timKiemTimer?.Stop();   // hủy lượt gõ đang chờ -> không lọc 2 lần
            var ds = _dataCacheGiayKhen ?? new List<HistoryGiayKhenDTO>();
            string dvFilter = Module_BoLocKhenThuong.GiaTriLoc(comboBox_TimKiemDonVi);
            string htFilter = Module_BoLocKhenThuong.GiaTriLoc(comboBox1_HinhThucKT);
            string tenRaw = textBox_TimKiemTheoTen?.Text ?? "";
            if (_dangSetPlaceholder || string.Equals(tenRaw, PLACEHOLDER_TIMKIEM, StringComparison.Ordinal)) tenRaw = "";
            string[] tu = Module_BoLocKhenThuong.TachTuKhoa(tenRaw);
            try
            {
                var ketQua = new List<int>(ds.Count);
                var conDonVi = new HashSet<string>(Module_DonVi.SoSanhBang);
                var conHinhThuc = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string khoaDv = Module_DonVi.GiaTriSoKhop(dvFilter);
                for (int i = 0; i < ds.Count; i++)
                {
                    var item = ds[i];
                    if (tu.Length > 0 && !Module_BoLocKhenThuong.KhopTuKhoa(item.HoVaTen_Search ?? "", tu)) continue;
                    bool okDv = dvFilter.Length == 0 || string.Equals(Module_DonVi.GiaTriSoKhop(item.DonVi), khoaDv, StringComparison.Ordinal);
                    bool okHt = htFilter.Length == 0 || string.Equals(item.HinhThuc_Khen, htFilter, StringComparison.OrdinalIgnoreCase);
                    if (okHt && GiaTriHopLe(item.DonVi)) conDonVi.Add(item.DonVi);
                    if (okDv && GiaTriHopLe(item.HinhThuc_Khen)) conHinhThuc.Add(item.HinhThuc_Khen);
                    if (okDv && okHt) ketQua.Add(i);
                }
                _dangNapCombo = true;
                try
                {
                    Module_BoLocKhenThuong.NapLaiCombo(comboBox_TimKiemDonVi,
                        Module_BoLocKhenThuong.SapTheoThuTuChuan(conDonVi, _thuTuDonVi), Module_HeThong.Tat_Ca);
                    Module_BoLocKhenThuong.NapLaiCombo(comboBox1_HinhThucKT,
                        Module_BoLocKhenThuong.SapABC(conHinhThuc), Module_HeThong.Tat_Ca);
                }
                finally { _dangNapCombo = false; }
                var grid = kryptonDataGridView1;
                bool giongHet = _filteredIndexes != null && grid.RowCount == ketQua.Count
                                && _filteredIndexes.Count == ketQua.Count && _filteredIndexes.SequenceEqual(ketQua);
                if (!giongHet)
                {
                    _filteredIndexes = ketQua;
                    // Tắt vẽ + đổi số dòng nhanh (giảm RowCount từng dòng là nguyên nhân khựng khi dữ liệu nhiều)
                    using var _ = Module_LuoiAo.TamDungVe(grid);
                    Module_LuoiAo.DatSoDong(grid, ketQua.Count, boChon: true);
                }
                grid.Invalidate();
                CapNhatTrangThaiHienThi();
            }
            catch (Exception ex) { Debug.WriteLine("Lỗi ApplyFilter RAM: " + ex.Message); }
        }
        private void KryptonDataGridView1_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            if (_filteredIndexes == null || e.RowIndex < 0 || e.RowIndex >= _filteredIndexes.Count) return;
            try
            {
                int actualViewIndex = _filteredIndexes[e.RowIndex];
                var data = _dataCacheGiayKhen[actualViewIndex];
                string colName = kryptonDataGridView1.Columns[e.ColumnIndex].Name;
                switch (colName)
                {
                    case "ID": e.Value = data.ID; break;
                    case "STT": e.Value = data.STT; break;
                    case "HoVaTen": e.Value = data.HoVaTen; break;
                    case "SoHieu": e.Value = data.SoHieu; break;
                    case "DonVi": e.Value = data.DonVi; break;
                    case "TinhTrang": e.Value = data.TinhTrang; break;
                    case "HinhThuc_Khen": e.Value = data.HinhThuc_Khen; break;
                    case "QuyetDinh_Khen": e.Value = data.QuyetDinh_Khen; break;
                    case "NgayCapQD_Khen": e.Value = data.NgayCapQD_Khen; break;
                    case "DonVi_Khen": e.Value = data.DonVi_Khen; break;
                    case "VeViec_Khen": e.Value = data.VeViec_Khen; break;
                    case "GhiChu_Khen": e.Value = data.GhiChu_Khen; break;
                    default: e.Value = string.Empty; break;
                }
            }
            catch { e.Value = string.Empty; }
        }
        private void KryptonDataGridView1_RowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
        {
            var grid = sender as DataGridView;
            if (grid == null) return;
            if (_rowHeaderBrush == null)
            {
                _rowHeaderBrush = new SolidBrush(grid.RowHeadersDefaultCellStyle.ForeColor);
                _rowHeaderFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            }
            string stt = (e.RowIndex + 1).ToString();
            Rectangle headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);
            e.Graphics.DrawString(stt, grid.Font, _rowHeaderBrush, headerBounds, _rowHeaderFormat);
        }
        // CẤU HÌNH GIAO DIỆN THANH TRẠNG THÁI (CHUẨN HỆ THỐNG)
        private void CauHinhStatusStrip()
        {
            // 1. Nhãn số lượng mục khen thưởng (Bên trái, có Icon)
            if (toolStripStatusLabel1 != null && !toolStripStatusLabel1.IsDisposed)
            {
                toolStripStatusLabel1.Alignment = ToolStripItemAlignment.Left;
                toolStripStatusLabel1.TextAlign = ContentAlignment.MiddleLeft;
                toolStripStatusLabel1.ImageAlign = ContentAlignment.MiddleLeft;
                toolStripStatusLabel1.TextImageRelation = TextImageRelation.ImageBeforeText;
                toolStripStatusLabel1.Spring = false; // Tắt Spring để nhãn chỉ chiếm đúng diện tích chữ
                toolStripStatusLabel1.Padding = new Padding(5, 0, 10, 0);
            }
            // 2. Nhãn tổng số CBCS được khen thưởng (Neo cứng mép phải)
            if (toolStripStatusLabel2_TongSoCBCSDuocKhenThuong != null && !toolStripStatusLabel2_TongSoCBCSDuocKhenThuong.IsDisposed)
            {
                toolStripStatusLabel2_TongSoCBCSDuocKhenThuong.Alignment = ToolStripItemAlignment.Right;
                toolStripStatusLabel2_TongSoCBCSDuocKhenThuong.TextAlign = ContentAlignment.MiddleRight;
                toolStripStatusLabel2_TongSoCBCSDuocKhenThuong.Spring = false;
                toolStripStatusLabel2_TongSoCBCSDuocKhenThuong.Padding = new Padding(0, 0, 15, 0);
                toolStripStatusLabel2_TongSoCBCSDuocKhenThuong.Visible = true;
            }
        }
        private void CapNhatTrangThaiHienThi()
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;
            // Kiểm tra tính hợp lệ của tệp được chọn
            if (comboBox_ChonCSDLNam.Items.Count == 0 || comboBox_ChonCSDLNam.SelectedItem == null)
            {
                if (toolStripStatusLabel1 != null)
                    toolStripStatusLabel1.Text = "Không có tệp lịch sử khen thưởng nào.";
                if (toolStripStatusLabel2_TongSoCBCSDuocKhenThuong != null)
                    toolStripStatusLabel2_TongSoCBCSDuocKhenThuong.Visible = false;
                return;
            }
            // 1. Cập nhật nhãn bên trái: Tổng số mục giấy khen
            int tongSoMuc = _filteredIndexes?.Count ?? 0;
            if (toolStripStatusLabel1 != null)
            {
                toolStripStatusLabel1.Text = $"Tổng cộng: {tongSoMuc:N0} mục khen thưởng";
            }
            // 2. Cập nhật nhãn bên phải: Lọc trùng lặp CBCS qua HashSet
            if (toolStripStatusLabel2_TongSoCBCSDuocKhenThuong != null)
            {
                if (tongSoMuc > 0 && _dataCacheGiayKhen != null && _dataCacheGiayKhen.Count > 0)
                {
                    var tapHopSoHieu = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < tongSoMuc; i++)
                    {
                        int actualIdx = _filteredIndexes![i];
                        if (actualIdx >= 0 && actualIdx < _dataCacheGiayKhen.Count)
                        {
                            string sh = _dataCacheGiayKhen[actualIdx].SoHieu;
                            if (!string.IsNullOrWhiteSpace(sh))
                            {
                                tapHopSoHieu.Add(sh.Trim());
                            }
                        }
                    }
                    int tongSoCBCS = tapHopSoHieu.Count;
                    toolStripStatusLabel2_TongSoCBCSDuocKhenThuong.Text = $"Tổng số: {tongSoCBCS:N0} {Module_HeThong.Tu_dong_chi}";
                    toolStripStatusLabel2_TongSoCBCSDuocKhenThuong.Visible = true;
                }
                else
                {
                    toolStripStatusLabel2_TongSoCBCSDuocKhenThuong.Text = $"Tổng số: 0 {Module_HeThong.Tu_dong_chi}";
                    toolStripStatusLabel2_TongSoCBCSDuocKhenThuong.Visible = true;
                }
                // Ép toàn bộ StatusStrip vẽ lại ngay lập tức
                var parentStrip = toolStripStatusLabel2_TongSoCBCSDuocKhenThuong.GetCurrentParent();
                if (parentStrip != null && !parentStrip.IsDisposed)
                {
                    parentStrip.PerformLayout();
                    parentStrip.Invalidate();
                    parentStrip.Update();
                }
            }
        }
        private void comboBox_ChonCSDLNam_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!this.IsHandleCreated) return;
            _ = ThucHienTaiDuLieuLichSuAsync();
            CapNhatTieuDeTheoNamDuocChon();
        }
        private void ComboBoxFilter_SelectedIndexChanged(object? sender, EventArgs e) => ApplyFilter();
        private void textBox_TimKiemTheoTen_TextChanged(object? sender, EventArgs e)
        {
            if (_dangSetPlaceholder) return;
            if (_timKiemTimer == null) return;
            _timKiemTimer.Stop();
            _timKiemTimer.Start();
        }
        private void TextBox_TimKiemTheoTen_Enter(object? sender, EventArgs e)
        {
            if (textBox_TimKiemTheoTen.Text == PLACEHOLDER_TIMKIEM)
            {
                _dangSetPlaceholder = true;
                textBox_TimKiemTheoTen.Text = "";
                textBox_TimKiemTheoTen.ForeColor = SystemColors.WindowText;
                _dangSetPlaceholder = false;
            }
        }
        private void TextBox_TimKiemTheoTen_Leave(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox_TimKiemTheoTen.Text)) InitPlaceholderTimKiem();
        }
        private void InitPlaceholderTimKiem()
        {
            if (textBox_TimKiemTheoTen == null) return;
            _dangSetPlaceholder = true;
            textBox_TimKiemTheoTen.Text = PLACEHOLDER_TIMKIEM;
            textBox_TimKiemTheoTen.ForeColor = Color.Gray;
            _dangSetPlaceholder = false;
        }
        private void kryptonButton_LamMoiCacOTimKiem_Click(object? sender, EventArgs e)
        {
            _dangSetPlaceholder = true;
            InitPlaceholderTimKiem();
            if (comboBox_TimKiemDonVi != null) comboBox_TimKiemDonVi.SelectedIndexChanged -= ComboBoxFilter_SelectedIndexChanged;
            if (comboBox1_HinhThucKT != null) comboBox1_HinhThucKT.SelectedIndexChanged -= ComboBoxFilter_SelectedIndexChanged;
            if (comboBox_TimKiemDonVi != null && comboBox_TimKiemDonVi.Items.Count > 0) comboBox_TimKiemDonVi.SelectedIndex = 0;
            if (comboBox1_HinhThucKT != null && comboBox1_HinhThucKT.Items.Count > 0) comboBox1_HinhThucKT.SelectedIndex = 0;
            if (comboBox_TimKiemDonVi != null) comboBox_TimKiemDonVi.SelectedIndexChanged += ComboBoxFilter_SelectedIndexChanged;
            if (comboBox1_HinhThucKT != null) comboBox1_HinhThucKT.SelectedIndexChanged += ComboBoxFilter_SelectedIndexChanged;
            _dangSetPlaceholder = false;
            ApplyFilter();
        }
        private void kryptonButton_CapNhat_Click(object? sender, EventArgs e)
        {
            _ = ThucHienTaiDuLieuLichSuAsync();
        }
        //private void kryptonButton_Dong_Click(object? sender, EventArgs e)
        //{
            //// 1. Tìm Form34 (Quản lý/Thống kê khen thưởng) trong RAM để đưa lên bề mặt
            //// 2. Tìm Form2 (Form cha) để đặt lại tiêu đề
            //var fCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            //if (fCha != null)
            //{
                //// Tạo chuỗi tiêu đề theo năm hệ thống như bạn yêu cầu
                //string tieuDeForm = "Trang Quản lý khen thưởng CBCS năm " + Module_HeThong.LayNamHeThong();
                //// Gọi hàm cập nhật tiêu đề trên Form cha
                //fCha.CapNhatTieuDe(tieuDeForm);
            //}
            //// 3. Đóng Form50 hiện tại
            //this.Close();
        //}
        private async void kryptonButton_XuatData_Click(object? sender, EventArgs e)
        {
            if (_filteredIndexes.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var selectedFile = comboBox_ChonCSDLNam.SelectedItem as FileLichSuDTO;
            if (selectedFile == null) return;
            string[] parts = selectedFile.TenHienThi.Split(' ');
            string nam = (parts.Length > 1) ? parts[1] : DateTime.Now.Year.ToString();
            string fileName = $"ThongKe_KhenThuong{Module_CheDoDoiTuong.MaTep(Module_CheDoDoiTuong.Hien)}_Nam{nam}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            using var sfd = new SaveFileDialog
            {
                Title = "Chọn nơi lưu file Excel thống kê",
                Filter = Module_HeThong.BoLoc_Excel,
                FileName = fileName,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;
            string filePath = sfd.FileName;
            string textBanDau = kryptonButton_XuatData.Values.Text;
            Image? anhBanDau = kryptonButton_XuatData.Values.Image;
            try
            {
                kryptonButton_XuatData.Enabled = false;
                kryptonButton_XuatData.Values.Text = "Đang tạo...";
                kryptonButton_XuatData.Values.Image = null;
                this.Enabled = false;
                await Task.Delay(50);
                // 🌟 1. LOẠI BỎ CỘT STT CŨ (NẾU CÓ) ĐỂ TRÁNH TRÙNG LẶP
                var exportCols = kryptonDataGridView1.Columns.Cast<DataGridViewColumn>()
                    .Where(c => c.Visible && c.Name != "ID" && c.Name != "STT")
                    .ToList();
                int rowCount = _filteredIndexes.Count;
                // 🌟 2. TĂNG TỔNG SỐ CỘT LÊN 1 ĐỂ CHỨA CỘT STT MỚI TẠO
                int colCount = exportCols.Count + 1;
                string[] headerArray = new string[colCount];
                double[] colWidths = new double[colCount];
                var dataList = new List<object[]>(rowCount);
                // 🌟 3. THIẾT LẬP TIÊU ĐỀ VÀ ĐỘ RỘNG CHO CỘT STT (CỘT ĐẦU TIÊN)
                headerArray[0] = "STT";
                colWidths[0] = 6;
                // 🌟 4. ĐẨY CÁC CỘT DỮ LIỆU GỐC LÙI VỀ PHÍA SAU 1 VỊ TRÍ (c + 1)
                for (int c = 0; c < exportCols.Count; c++)
                {
                    headerArray[c + 1] = string.IsNullOrWhiteSpace(exportCols[c].HeaderText) ? exportCols[c].Name : exportCols[c].HeaderText;
                    colWidths[c + 1] = exportCols[c].Width / 7.5;
                }
                for (int r = 0; r < rowCount; r++)
                {
                    var dto = _dataCacheGiayKhen[_filteredIndexes[r]];
                    var rowValues = new object[colCount];
                    // 🌟 5. GÁN SỐ THỨ TỰ TỰ ĐỘNG TĂNG DẦN VÀO CỘT 0
                    rowValues[0] = r + 1;
                    for (int c = 0; c < exportCols.Count; c++)
                    {
                        string cName = exportCols[c].Name;
                        switch (cName)
                        {
                            // Đẩy toàn bộ dữ liệu vào index [c + 1]
                            case "HoVaTen": rowValues[c + 1] = dto.HoVaTen; break;
                            case "SoHieu": rowValues[c + 1] = dto.SoHieu; break;
                            case "DonVi": rowValues[c + 1] = dto.DonVi; break;
                            case "TinhTrang": rowValues[c + 1] = dto.TinhTrang; break;
                            case "HinhThuc_Khen": rowValues[c + 1] = dto.HinhThuc_Khen; break;
                            case "QuyetDinh_Khen": rowValues[c + 1] = dto.QuyetDinh_Khen; break;
                            case "NgayCapQD_Khen": rowValues[c + 1] = dto.NgayCapQD_Khen; break;
                            case "DonVi_Khen": rowValues[c + 1] = dto.DonVi_Khen; break;
                            case "VeViec_Khen": rowValues[c + 1] = dto.VeViec_Khen; break;
                            case "GhiChu_Khen": rowValues[c + 1] = dto.GhiChu_Khen; break;
                            default: rowValues[c + 1] = ""; break;
                        }
                    }
                    dataList.Add(rowValues);
                }
                await Task.Run(() =>
                {
                    using var wb = new ClosedXML.Excel.XLWorkbook();
                    var ws = wb.Worksheets.Add("LichSuKhenThuongCaNhan");
                    ws.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                    ws.Style.Font.FontSize = 11;
                    ws.Cell("A1").Value = "DANH SÁCH LỊCH SỬ";
                    ws.Range(1, 1, 1, colCount).Merge().Style.Font.SetBold().Font.SetFontSize(14)
                        .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);
                    ws.Cell("A2").Value = $"THỐNG KÊ KẾT QUẢ KHEN THƯỞNG CÁ NHÂN NĂM {nam}";
                    ws.Range(2, 1, 2, colCount).Merge().Style.Font.SetBold().Font.SetFontSize(13)
                        .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);
                    int excelStartRow = 4;
                    for (int c = 0; c < colCount; c++)
                    {
                        var cell = ws.Cell(excelStartRow, c + 1);
                        // 🌟 6. ÁP DỤNG TRỰC TIẾP MẢNG headerArray ĐÃ ĐƯỢC XỬ LÝ (Tránh lỗi Index)
                        cell.Value = headerArray[c];
                        cell.Style.Font.Bold = true;
                        cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                        cell.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                        cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(217, 225, 242);
                        ws.Column(c + 1).Width = colWidths[c];
                    }
                    ws.Row(excelStartRow).Height = 35;
                    if (rowCount > 0)
                    {
                        ws.Cell(excelStartRow + 1, 1).InsertData(dataList);
                        var dataRange = ws.Range(excelStartRow, 1, excelStartRow + rowCount, colCount);
                        dataRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                        dataRange.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                        var bodyRange = ws.Range(excelStartRow + 1, 1, excelStartRow + rowCount, colCount);
                        bodyRange.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                        bodyRange.Style.Alignment.WrapText = true;
                        ws.Range(excelStartRow + 1, 1, excelStartRow + rowCount, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                    }
                    int tongDong = rowCount + excelStartRow + 1;
                    var totalCell = ws.Cell(tongDong, 1);
                    totalCell.Value = $"Tổng cộng: {rowCount} cá nhân được thống kê./.";
                    totalCell.Style.Font.SetBold().Font.SetItalic();
                    ws.Range(tongDong, 1, tongDong, colCount).Merge();
                    ws.PageSetup.PaperSize = ClosedXML.Excel.XLPaperSize.A4Paper;
                    ws.PageSetup.PageOrientation = ClosedXML.Excel.XLPageOrientation.Landscape;
                    ws.PageSetup.FitToPages(1, 0);
                    ws.PageSetup.Margins.Top = 0.5;
                    ws.PageSetup.Margins.Bottom = 0.5;
                    ws.PageSetup.Margins.Left = 0.4;
                    ws.PageSetup.Margins.Right = 0.4;
                    Module_BanQuyen.DongDauExcel(wb);
                    Module_ExcelAnToan.LuuAnToan(wb, filePath);
                });
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Xuất Excel giấy khen cá nhân (năm cũ)",
                    $"{rowCount} dòng | Tệp: {Path.GetFileName(filePath)}");
                try { Module_XuatNhapDuLieuThiDua.MoVaChonTepTrongExplorer(filePath); } catch { }
                Module_ThongBao.ThanhCong("Xuất Excel thành công!");
            }
            catch (Exception ex)
            {
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Lỗi xuất Excel giấy khen cá nhân (năm cũ)", ex.Message);
                MessageBox.Show("Lỗi khi xuất dữ liệu Excel:\n" + ex.Message, Module_HeThong.TieuDe_Loi, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Enabled = true;
                kryptonButton_XuatData.Values.Text = textBanDau;
                kryptonButton_XuatData.Values.Image = anhBanDau;
                kryptonButton_XuatData.Enabled = true;
                this.Focus();
            }
        }
        private void toolStripMenuItem_XuatDuLieu_Click(object? sender, EventArgs e)
        {
            kryptonButton_XuatData.PerformClick();
        }
        private void CauHinhGridCoBan(DataGridView dgv)
        {
            dgv.RowHeadersVisible = true;
            dgv.RowHeadersWidth = 60;
            dgv.BackgroundColor = Color.White;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = Color.FromArgb(235, 235, 235);
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersHeight = 60;
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Font = _fontGridHeader95Bold,
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(240, 244, 248),
                ForeColor = Color.FromArgb(40, 40, 40),
                WrapMode = DataGridViewTriState.True,
                Padding = new Padding(3)
            };
            dgv.RowTemplate.Height = 36;
            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = _fontGridCell95Regular,
                ForeColor = Color.FromArgb(45, 45, 45),
                SelectionBackColor = Color.FromArgb(232, 244, 253),
                SelectionForeColor = Color.FromArgb(0, 102, 204),
                Padding = new Padding(2, 0, 2, 0)
            };
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 252, 252);
        }
        private void CauHinhCotGrid(DataGridView dgv)
        {
            dgv.Columns.Clear();
            // 0. Cột ID (Ẩn)
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ID", Visible = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "STT", Visible = false });
            // 1. STT (Ngắn)
            // dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "STT", HeaderText = "STT", Width = 45, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            // 2. Họ và tên
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "HoVaTen", HeaderText = Module_HeThong.TieuDeCot.HoVaTen, Width = 160, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            // 3. Số hiệu
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoHieu", HeaderText = Module_HeThong.TieuDeCot.SoHieu, Width = 90, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            // 4. Đơn vị công tác (Thu hẹp lại)
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "DonVi", HeaderText = "Đơn vị công tác", Width = 130, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            // 5. Tình trạng
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TinhTrang", HeaderText = "Tình trạng", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            // 6. Hình thức khen
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "HinhThuc_Khen", HeaderText = "Hình thức khen", Width = 140, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            // 7. Số Quyết định
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "QuyetDinh_Khen", HeaderText = "Số Quyết định", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            // 8. Ngày cấp QĐ
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "NgayCapQD_Khen", HeaderText = "Ngày ban hành", Width = 95, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            // 9. Đơn vị khen (Thu hẹp lại)
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "DonVi_Khen", HeaderText = "Đơn vị khen", Width = 150, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            // 10. Về việc (Nội dung) - Rộng ra và tự động chiếm không gian còn lại
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "VeViec_Khen",
                HeaderText = "Về việc (Nội dung)",
                Width = 250,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, // Tự động lấp đầy khoảng trống
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
            });
            // 11. Ghi chú (Mở rộng ra)
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "GhiChu_Khen", HeaderText = Module_HeThong.TieuDeCot.GhiChu, Width = 200, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            // Tắt tính năng tự động Sort khi click vào tiêu đề cột (do đang xài Virtual Mode)
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }
        private void CapNhatTieuDeTheoNamDuocChon()
        {
            if (comboBox_ChonCSDLNam.SelectedItem is FileLichSuDTO selectedFile)
            {
                string tieuDeMoi = $"Thống kê khen thưởng {Module_CheDoDoiTuong.TenDoiTuongHienTai} năm cũ - {selectedFile.TenHienThi}";
                this.Text = tieuDeMoi;
                var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
                if (formCha != null) formCha.CapNhatTieuDe(tieuDeMoi);
            }
        }
        private void toolStripMenuItem_LamMoi_Click(object? sender, EventArgs e)
        {
            kryptonButton_CapNhat.PerformClick();
        }
        //private void toolStripMenuItem_Dong_Click(object? sender, EventArgs e)
        //{
            //// kryptonButton_Dong.PerformClick();
        //}
        //private void kryptonButton1_TraCuuKetQuaKhenThuongNamCu_Click(object? sender, EventArgs e)
        //{
            //// 1. Tìm Form cha đang hoạt động
            //// 2. Tìm PanelContainer
                //// 3. Tìm Form51 đã tồn tại trong PanelContainer
                //var form51 = panel.Controls
                    //.OfType<Form51_QuanLyKhenThuongTapTheNamCu>()
                    //.FirstOrDefault();
                //// 4. Nếu Form51 chưa tồn tại -> tạo mới đúng một lần
                    //// 5. Đưa Form51 vào PanelContainer
                    //panel.Controls.Add(form51);
                    //// Đưa Form51 lên trước khi hiển thị
                    //form51.BringToFront();
                    //// Hiển thị Form lần đầu
                    //form51.Show();
                //}
                //else
                //{
                    //// 6. Nếu Form51 đã tồn tại -> tái sử dụng
                //// 7. Ẩn các Form con khác trong PanelContainer
                //// 8. Đảm bảo Form51 luôn ở lớp trên cùng
                //form51.Show();
                //form51.BringToFront();
                //// 9. Cập nhật tiêu đề Form cha
                //// Form hoặc Control đã bị giải phóng trong lúc chuyển giao diện.
                //// Không để lỗi này làm sập toàn bộ phần mềm.
        private async Task DongBoTinhTrangGiayKhenNamCuAsync(string dbPathLichSu)
        {
            if (string.IsNullOrEmpty(dbPathLichSu) || !File.Exists(dbPathLichSu)) return;
            if (!File.Exists(_csdl2Path)) return;
            try
            {
                // 1. Quét CSDL hiện tại (csdl2.db) để lấy toàn bộ Số Hiệu đang công tác
                // Dùng HashSet để tốc độ tìm kiếm đạt O(1) siêu tốc
                var hashSoHieuHienTai = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var cn2 = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl2Path, chiDoc: true)))
                {
                    await cn2.OpenAsync();
                    using var cmd = new SqliteCommand($"SELECT SoHieu FROM {Module_TaiKhoan.BangDanhSach} WHERE SoHieu IS NOT NULL AND SoHieu <> ''", cn2);
                    using var rd = await cmd.ExecuteReaderAsync();
                    while (await rd.ReadAsync())
                    {
                        string rawSh = rd.GetString(0);
                        string plainSh = SafeDecrypt(rawSh).Trim(); // Giải mã số hiệu hiện tại
                        if (!string.IsNullOrEmpty(plainSh))
                        {
                            hashSoHieuHienTai.Add(plainSh);
                        }
                    }
                }
                if (hashSoHieuHienTai.Count == 0) return;
                // 2. Quét CSDL Lịch sử và đối chiếu để cập nhật
                var updateQueue = new List<(int id, string ttMoi)>();
                using (var cnCu = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(dbPathLichSu)))
                {
                    await cnCu.OpenAsync();
                    // Kiểm tra xem bảng ThongKe_GiayKhen có tồn tại không
                    using (var cmdCheck = new SqliteCommand($"SELECT 1 FROM sqlite_master WHERE type='table' AND name='{Module_CheDoDoiTuong.BangGiayKhen}'", cnCu))
                    {
                        if (await cmdCheck.ExecuteScalarAsync() == null) return;
                    }
                    // Đọc ID, SoHieu (Mã hóa), TinhTrang (Thuần) từ năm cũ
                    using (var cmdSelect = new SqliteCommand($"SELECT ID, SoHieu, TinhTrang FROM {Module_CheDoDoiTuong.BangGiayKhen}", cnCu))
                    using (var rd = await cmdSelect.ExecuteReaderAsync())
                    {
                        while (await rd.ReadAsync())
                        {
                            int id = rd.GetInt32(0);
                            string rawSh = rd.IsDBNull(1) ? "" : rd.GetString(1);
                            string ttCu = rd.IsDBNull(2) ? "" : rd.GetString(2).Trim(); // Text thuần
                            string plainSh = SafeDecrypt(rawSh).Trim(); // Giải mã số hiệu năm cũ
                            // Logic: Nếu số hiệu năm cũ có nằm trong CSDL hiện tại -> Đang công tác. Ngược lại -> Chuyển công tác
                            string ttMoi = (!string.IsNullOrEmpty(plainSh) && hashSoHieuHienTai.Contains(plainSh))
                                ? Module_HeThong.TT_DANG_CONG_TAC
                                : Module_HeThong.TT_CHUYEN_CONG_TAC;
                            // Chỉ đưa vào danh sách cập nhật nếu có sự sai lệch trạng thái
                            if (!string.Equals(ttCu, ttMoi, StringComparison.OrdinalIgnoreCase))
                            {
                                updateQueue.Add((id, ttMoi));
                            }
                        }
                    }
                    // 3. Tiến hành cập nhật bằng Transaction (Cực nhanh và an toàn)
                    if (updateQueue.Count > 0)
                    {
                        using var tran = cnCu.BeginTransaction();
                        try
                        {
                            // LƯU Ý: TinhTrang được truyền thẳng (Text thuần), KHÔNG MÃ HÓA
                            using var cmdUpd = new SqliteCommand($"UPDATE {Module_CheDoDoiTuong.BangGiayKhen} SET TinhTrang = @tt WHERE ID = @id", cnCu, tran);
                            var pTt = cmdUpd.Parameters.Add("@tt", SqliteType.Text);
                            var pId = cmdUpd.Parameters.Add("@id", SqliteType.Integer);
                            cmdUpd.Prepare();
                            foreach (var item in updateQueue)
                            {
                                pTt.Value = item.ttMoi;
                                pId.Value = item.id;
                                await cmdUpd.ExecuteNonQueryAsync();
                            }
                            tran.Commit();
                        }
                        catch
                        {
                            tran.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi đồng bộ Tình trạng Form50: {ex.Message}");
            }
        }
        private void KryptonDataGridView1_CellDoubleClick(
        object? sender,
        DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;
            if (_filteredIndexes == null ||
                e.RowIndex >= _filteredIndexes.Count)
                return;
            try
            {
                int actualIndex = _filteredIndexes[e.RowIndex];
                if (actualIndex < 0 ||
                    actualIndex >= _dataCacheGiayKhen.Count)
                    return;
                HistoryGiayKhenDTO data =
                    _dataCacheGiayKhen[actualIndex];
                if (data == null)
                    return;
                HienThiFormAo_ChiTietKhenThuong(data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[Form50] Lỗi mở chi tiết khen thưởng: {ex}");
                MessageBox.Show(
                    "Không thể mở thông tin chi tiết.\n\n" +
                    ex.Message,
                    "Lỗi giao diện",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void HienThiFormAo_ChiTietKhenThuong(HistoryGiayKhenDTO data)
        {
            if (data == null) return;
            // 1. BẢNG MÀU GIAO DIỆN
            Color mauNen = Color.White;
            Color mauNhan = Color.FromArgb(245, 248, 252);
            Color mauFooter = Color.FromArgb(248, 249, 250);
            Color mauDangCongTac = Color.FromArgb(34, 139, 34);
            Color mauChuyenCongTac = Color.FromArgb(211, 47, 47);
            // 2. LẤY NĂM TỪ COMBOBOX
            string nam = DateTime.Now.Year.ToString();
            string giaTriNam = comboBox_ChonCSDLNam?.Text?.Trim() ?? "";
            if (!string.IsNullOrWhiteSpace(giaTriNam))
            {
                System.Text.RegularExpressions.Match match =
                    System.Text.RegularExpressions.Regex.Match(giaTriNam, @"\b(19|20)\d{2}\b");
                if (match.Success)
                    nam = match.Value;
            }
            // 3. KHOANH VÙNG FORM ẢO
            using (var formAo = new FormAoBase())
            {
                formAo.Text = $"Chi tiết khen thưởng năm {nam}";
                // Mở rộng Form lên 740x600 để có không gian thoải mái cho chữ dãn dòng
                formAo.Size = new System.Drawing.Size(740, 600);
                formAo.FormBorderStyle = FormBorderStyle.FixedDialog;
                formAo.MaximizeBox = false;
                formAo.MinimizeBox = false;
                formAo.ShowIcon = false;
                formAo.ShowInTaskbar = false;
                formAo.StartPosition = FormStartPosition.CenterParent;
                formAo.BackColor = mauNen;
                // 4. FOOTER PANEL & NÚT ĐÓNG
                var panelBottom = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 60,
                    BackColor = mauFooter
                };
                var btnClose = new KryptonButton
                {
                    Text = "Đóng",
                    Width = 120,
                    Height = 38,
                    DialogResult = DialogResult.OK,
                    Anchor = AnchorStyles.None
                };
                btnClose.StateCommon.Content.ShortText.Font = _fontGrid10Bold;
                btnClose.StateCommon.Border.Rounding = 5;
                btnClose.Location = new Point((formAo.ClientSize.Width - btnClose.Width) / 2, 11);
                panelBottom.Controls.Add(btnClose);
                // 5. PANEL NỘI DUNG CHÍNH
                var panelContent = new KryptonPanel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(20, 20, 20, 10)
                };
                panelContent.StateCommon.Color1 = mauNen;
                // 6. DATAGRIDVIEW CẤU HÌNH (TỰ ĐỘNG DÃN DÒNG)
                var grid = new KryptonDataGridView
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    AllowUserToResizeRows = false,
                    AllowUserToResizeColumns = false,
                    RowHeadersVisible = false,
                    ColumnHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    MultiSelect = false,
                    BackgroundColor = mauNen,
                    BorderStyle = BorderStyle.None,
                    AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
                };
                grid.GridStyles.Style = DataGridViewStyle.List;
                grid.StateCommon.Background.Color1 = mauNen;
                grid.StateCommon.DataCell.Content.Font = _fontGrid10Regular;
                grid.RowTemplate.MinimumHeight = 42;
                // Cột 0: Tên thông tin
                grid.Columns.Add("Ten", "Thông tin");
                var cotTen = grid.Columns[0];
                cotTen.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                cotTen.Width = 200;
                cotTen.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                cotTen.DefaultCellStyle.Font = _fontGrid10Bold;
                cotTen.DefaultCellStyle.BackColor = mauNhan;
                cotTen.DefaultCellStyle.Padding = new Padding(15, 8, 10, 8);
                // Cột 1: Giá trị nội dung
                grid.Columns.Add("GiaTri", "Nội dung");
                var cotGiaTri = grid.Columns[1];
                cotGiaTri.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                cotGiaTri.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                cotGiaTri.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                cotGiaTri.DefaultCellStyle.Padding = new Padding(10, 8, 15, 8);
                // 7. HÀM THÊM DÒNG DỮ LIỆU
                int ThemDong(string ten, string giaTri, Color? mauGiaTri = null, Font? fontGiaTri = null)
                {
                    int rowIndex = grid.Rows.Add(
                        string.IsNullOrWhiteSpace(ten) ? "" : ten.Trim(),
                        string.IsNullOrWhiteSpace(giaTri) ? "—" : giaTri.Trim());
                    var rowData = grid.Rows[rowIndex];
                    if (mauGiaTri.HasValue)
                        rowData.Cells[1].Style.ForeColor = mauGiaTri.Value;
                    if (fontGiaTri != null)
                        rowData.Cells[1].Style.Font = fontGiaTri;
                    return rowIndex;
                }
                // 8. NẠP DỮ LIỆU VÀO GRID
                ThemDong(Module_HeThong.TieuDeCot.HoVaTen, data.HoVaTen, Color.Green, _fontGrid10Bold);
                ThemDong(Module_HeThong.TieuDeCot.SoHieu, data.SoHieu);
                ThemDong("Đơn vị công tác", data.DonVi);
                // Tình trạng công tác
                string tinhTrang = data.TinhTrang?.Trim() ?? "";
                Color? mauTinhTrang = null;
                Font? fontTinhTrang = null;
                if (tinhTrang.Equals(Module_HeThong.TT_DANG_CONG_TAC, StringComparison.OrdinalIgnoreCase))
                {
                    mauTinhTrang = mauDangCongTac;
                    fontTinhTrang = _fontGrid10Bold;
                }
                else if (tinhTrang.Equals(Module_HeThong.TT_CHUYEN_CONG_TAC, StringComparison.OrdinalIgnoreCase))
                {
                    mauTinhTrang = mauChuyenCongTac;
                    fontTinhTrang = _fontGrid10Bold;
                }
                ThemDong("Tình trạng", tinhTrang, mauTinhTrang, fontTinhTrang);
                // Chi tiết khen thưởng
                ThemDong("Hình thức khen", data.HinhThuc_Khen);
                ThemDong("Số Quyết định", data.QuyetDinh_Khen);
                ThemDong("Ngày ban hành", data.NgayCapQD_Khen);
                ThemDong("Đơn vị khen", data.DonVi_Khen);
                ThemDong("Về việc", data.VeViec_Khen);
                ThemDong(Module_HeThong.TieuDeCot.GhiChu, data.GhiChu_Khen);
                // 9. BỐ TRÍ VÀ THÊM DỮ LIỆU VÀO FORM
                panelContent.Controls.Add(grid);
                formAo.Controls.Add(panelContent);
                formAo.Controls.Add(panelBottom);
                formAo.AcceptButton = btnClose;
                formAo.CancelButton = btnClose;
                // 10. UX KHI SHOW FORM
                formAo.Shown += (s, e) =>
                {
                    grid.ClearSelection();
                    if (grid.Rows.Count > 0)
                        grid.FirstDisplayedScrollingRowIndex = 0;
                    btnClose.Focus();
                };
                // 11. HIỂN THỊ DIALOG
                formAo.ShowDialog(this);
            }
        }
    }
}