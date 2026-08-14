using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using Krypton.Toolkit;

namespace PhanMemThiDua2026
{
    public partial class Form49_QuanLyKhenThuongTapThe : Form
    {
        private readonly string _csdl4Path = Module_DanduongGPS.DuongDanCSDL4;
        //Thực tiễn là thước đo tiêu chuẩn để kiểm nghiệm chân lý. - Karl Marx
        //Anh yêu em, nhưng em không yêu anh. Anh đau, nhưng anh vẫn yêu em. - Trịnh Công Sơn
        // Hãy sống như thể bạn sẽ chết vào ngày mai. Hãy học như thể bạn sẽ sống mãi mãi. - Mahatma Gandhi
        // Tôi yêu mèo cam Garfield, vì nó dạy tôi rằng: "Ăn, ngủ, chơi, lười biếng, và vẫn được yêu thương". - Jim Davis
        // Biến lưu trạng thái
        private int _selectedID = -1;
        private bool isEditing = false;
        private List<string> _cachedDonVi = null;
        private System.Threading.CancellationTokenSource _searchCts;
        private int _msgCounter = 0;
        // LỚP CACHE TOÀN CỤC CHỐNG QUÉT DB
        private class DuLieuCache
        {
            public int ID { get; set; }
            public string TenTapThe { get; set; }
            public string DonVi { get; set; }
            public string HinhThucKhenThuong { get; set; } // <--- THÊM DÒNG NÀY
        }
        private List<DuLieuCache> _globalCache = null; // Chứa toàn bộ ID, Tên, Đơn vị đã giải mã
        public Form49_QuanLyKhenThuongTapThe()
        {
            InitializeComponent();

        }
        private async void Form49_QuanLyThiDuaTapThe_Load(object sender, EventArgs e)
        {
            
            // 1. KHỞI TẠO / KIỂM TRA CSDL
            KiemTraVaTaoBangThongKeKhenThuongTapThe();

            // 2. CẤU HÌNH DATAGRIDVIEW  
            CauHinhGridCoBan(kryptonDataGridView1);
            CauHinhStyleWeb(kryptonDataGridView1);
            CauHinhCotGrid(kryptonDataGridView1);

            // 3. CẤU HÌNH Ô STT            
            kryptonTextBox_STT.ReadOnly = true;
            kryptonTextBox_STT.StateCommon.Back.Color1 = Color.LightGreen;

            // 4. GÁN SỰ KIỆN DATAGRIDVIEW
            kryptonDataGridView1.CellClick -= kryptonDataGridView1_CellClick;
            kryptonDataGridView1.CellClick += kryptonDataGridView1_CellClick;

            // 5. GÁN SỰ KIỆN Ô TIỀN THƯỞNG            
            kryptonTextBox_TienThuong.TextChanged -= kryptonTextBox_TienThuong_TextChanged;
            kryptonTextBox_TienThuong.TextChanged += kryptonTextBox_TienThuong_TextChanged;

            // 6. GÁN SỰ KIỆN TÌM KIẾM REAL-TIME            
            if (textBox_TimKiemTheoTen != null)
            {
                textBox_TimKiemTheoTen.TextChanged -= textBox_TimKiemTheoTen_TextChanged;
                textBox_TimKiemTheoTen.TextChanged += textBox_TimKiemTheoTen_TextChanged;
            }
            if (comboBox_TimKiemDonViKhenThuong != null)
            {
                comboBox_TimKiemDonViKhenThuong.TextChanged -= comboBox_TimKiemDonViKhenThuong_TextChanged;
                comboBox_TimKiemDonViKhenThuong.TextChanged += comboBox_TimKiemDonViKhenThuong_TextChanged;
            }
            if (comboBox1_HinhThucKT != null)
            {
                comboBox1_HinhThucKT.TextChanged -= comboBox1_HinhThucKT_TextChanged;
                comboBox1_HinhThucKT.TextChanged += comboBox1_HinhThucKT_TextChanged;
            }

            // 7. KHỞI TẠO TRẠNG THÁI FORM
            ResetInput();

            // 8. TẢI DỮ LIỆU BAN ĐẦU
            await LoadComboBoxDonViAsync();
            await LoadComboBoxHinhThucKTAsync(); // <--- THÊM DÒNG NÀY ĐỂ TẢI BỘ LỌC HÌNH THỨC
            await LoadDataToGridAsync();
            InitToolTips();
            Module_MenuChuotPhai.TichHopGiaoDienXanhLa(contextMenuStrip1);
        }

        private void InitToolTips()
        {
            // ============================================================
            // KHỞI TẠO TOOLTIP - ỔN ĐỊNH CHO HỆ THỐNG NỘI BỘ
            // ============================================================

            // 1. Kiểm tra ToolTip
            if (toolTip1 == null)
                return;

            try
            {
                // 2. Cấu hình chung
                toolTip1.IsBalloon = true;
                toolTip1.ToolTipTitle = "Gợi ý thao tác";
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
                    kryptonButton_LamMoiCacOTimKiem,
                    "Làm mới (xóa) bộ lọc tìm kiếm hiện tại");

                GanToolTipAnToan(
                    kryptonButton1_Thoat,
                    "Đóng cửa sổ hiện tại");

                GanToolTipAnToan(
                    kryptonButton2_GiamCoChuRichText,
                    "Giảm cỡ chữ nội dung đang hiển thị");

                GanToolTipAnToan(
                    kryptonButton2_TangCoChuRichText,
                    "Tăng cỡ chữ nội dung đang hiển thị");

                GanToolTipAnToan(
                    kryptonButton_ThemKhenThuong,
                    "Thêm thông tin khen thưởng mới");

                GanToolTipAnToan(
                    kryptonButton_SuaVaLuuKhenThuong,
                    "Sửa và lưu thông tin khen thưởng");

                GanToolTipAnToan(
                    kryptonButton_XoaKhenThuong,
                    "Xóa thông tin khen thưởng đang chọn");
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
        // HÀM BẢO VỆ GIAO DIỆN (MÃ HÓA & GIẢI MÃ AN TOÀN)
        private async Task BuildGlobalCacheAsync(bool forceReload = false)
        {
            if (_globalCache != null && !forceReload) return;

            _globalCache = new List<DuLieuCache>();
            string connectionString = $@"Data Source={_csdl4Path};";

            using (var conn = new SqliteConnection(connectionString))
            {
                await conn.OpenAsync();
                // Quét 1 lần duy nhất toàn bộ bảng, thêm HinhThuc_KhenThuong vào câu SELECT
                string query = "SELECT ID, TenTapThe, DonVi_CapKhenThuong, HinhThuc_KhenThuong FROM ThongKe_KhenThuongTapThe";
                using (var cmd = new SqliteCommand(query, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        _globalCache.Add(new DuLieuCache
                        {
                            ID = Convert.ToInt32(reader["ID"]),
                            TenTapThe = SafeDecrypt(reader["TenTapThe"]),
                            DonVi = SafeDecrypt(reader["DonVi_CapKhenThuong"]),
                            HinhThucKhenThuong = SafeDecrypt(reader["HinhThuc_KhenThuong"]) // <--- THÊM DÒNG NÀY
                        });
                    }
                }
            }
        }
        private string SafeDecrypt(object value)
        {
            if (value == null || value == DBNull.Value) return "";
            string s = value.ToString()?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(s)) return "";

            try
            {
                string decrypted = BaoMatAES.GiaiMa(s);
                return string.IsNullOrEmpty(decrypted) ? "[Lỗi giải mã]" : decrypted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi SafeDecrypt]: {ex.Message}");
                return "[Lỗi dữ liệu]";
            }
        }
        private string SafeEncrypt(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            return BaoMatAES.MaHoa(input.Trim());
        }
        // LOAD VÀ CACHE DANH SÁCH ĐƠN VỊ VÀO COMBOBOX
        private async Task LoadComboBoxDonViAsync(bool forceReload = false)
        {
            if (comboBox_TimKiemDonViKhenThuong == null) return;

            // Đảm bảo cache đã có sẵn
            await BuildGlobalCacheAsync(forceReload);

            comboBox_TimKiemDonViKhenThuong.TextChanged -= comboBox_TimKiemDonViKhenThuong_TextChanged;
            string currentText = comboBox_TimKiemDonViKhenThuong.Text;

            // Rút trích danh sách Đơn Vị trực tiếp từ RAM (Cực kỳ nhanh)
            var dsDonVi = _globalCache
                .Where(x => !string.IsNullOrWhiteSpace(x.DonVi) && x.DonVi != "[Lỗi giải mã]")
                .Select(x => x.DonVi)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToArray();

            comboBox_TimKiemDonViKhenThuong.Items.Clear();

            // --- THÊM DÒNG NÀY ĐỂ TẠO GIÁ TRỊ RỖNG ---
            comboBox_TimKiemDonViKhenThuong.Items.Add("");

            comboBox_TimKiemDonViKhenThuong.Items.AddRange(dsDonVi);
            comboBox_TimKiemDonViKhenThuong.Text = currentText;

            comboBox_TimKiemDonViKhenThuong.TextChanged += comboBox_TimKiemDonViKhenThuong_TextChanged;
        }
        // SỰ KIỆN TEXT CHANGED & DEBOUNCE TÌM KIẾM
        private void kryptonTextBox_TienThuong_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(kryptonTextBox_TienThuong.Text)) return;

            kryptonTextBox_TienThuong.TextChanged -= kryptonTextBox_TienThuong_TextChanged;

            try
            {
                int cursorPosition = kryptonTextBox_TienThuong.Text.Length - kryptonTextBox_TienThuong.SelectionStart;
                string rawText = kryptonTextBox_TienThuong.Text.Replace(".", "").Replace(",", "").Trim();

                if (long.TryParse(rawText, out long tienThuong))
                {
                    if (tienThuong < 0) tienThuong = 0;
                    kryptonTextBox_TienThuong.Text = string.Format("{0:#,##0}", tienThuong).Replace(",", ".");
                    kryptonTextBox_TienThuong.SelectionStart = Math.Max(0, kryptonTextBox_TienThuong.Text.Length - cursorPosition);
                }
                else
                {
                    // TỐI ƯU LỌC KÝ TỰ SỐ (KHÔNG DÙNG REGEX)
                    string cleanStr = new string(rawText.Where(char.IsDigit).ToArray());

                    if (long.TryParse(cleanStr, out long cleanedTien))
                    {
                        kryptonTextBox_TienThuong.Text = string.Format("{0:#,##0}", cleanedTien).Replace(",", ".");
                        kryptonTextBox_TienThuong.SelectionStart = Math.Max(0, kryptonTextBox_TienThuong.Text.Length - cursorPosition);
                    }
                    else
                    {
                        kryptonTextBox_TienThuong.Text = "0";
                        kryptonTextBox_TienThuong.SelectionStart = kryptonTextBox_TienThuong.Text.Length;
                    }
                }
            }
            finally
            {
                kryptonTextBox_TienThuong.TextChanged += kryptonTextBox_TienThuong_TextChanged;
            }
        }
        private async void textBox_TimKiemTheoTen_TextChanged(object sender, EventArgs e)
        {
            await DebouncedSearchAsync();
        }
        private async void comboBox_TimKiemDonViKhenThuong_TextChanged(object sender, EventArgs e)
        {
            await DebouncedSearchAsync();
        }
        public async void ReloadForm49()
        {
            await LoadDataToGridAsync();
        }
        // LOAD DATA GRID (THUẬT TOÁN 2 CHẶNG)      
        // HÀM DEBOUNCE TÌM KIẾM (CÓ HỖ TRỢ CANCELLATION TOKEN)        
        private async Task DebouncedSearchAsync()
        {
            // Hủy triệt để lệnh tìm kiếm VÀ vòng lặp DB trước đó
            _searchCts?.Cancel();
            _searchCts = new System.Threading.CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                await Task.Delay(350, token); // Đợi 350ms sau phím bấm cuối cùng

                // Truyền Token thẳng vào hàm Load, nếu người dùng gõ tiếp, SQLite sẽ dừng quét ngay lập tức
                if (!token.IsCancellationRequested)
                {
                    await LoadDataToGridAsync(token);
                }
            }
            catch (TaskCanceledException) { /* Nuốt lỗi hủy task do người dùng đang gõ tiếp */ }
        }
        // LOAD DATA GRID (CÓ TOKEN HỦY TÁC VỤ & GIẢI MÃ THÔNG MINH)

        // Bổ sung tham số CancellationToken với giá trị mặc định
        public async Task LoadDataToGridAsync(System.Threading.CancellationToken token = default)
        {
            string keywordTen = textBox_TimKiemTheoTen?.Text.Trim().ToLower() ?? "";
            string keywordDonVi = comboBox_TimKiemDonViKhenThuong?.Text.Trim().ToLower() ?? "";
            string keywordHinhThuc = comboBox1_HinhThucKT?.Text.Trim().ToLower() ?? ""; // <--- THÊM DÒNG NÀY

            await BuildGlobalCacheAsync(); // Đảm bảo RAM đã có dữ liệu

            try
            {
                DataTable dtThuTu = await Task.Run(async () =>
                {
                    if (token.IsCancellationRequested) return null;

                    // 1. LỌC HOÀN TOÀN TRÊN RAM (0 I/O, 0 Giải mã)
                    var query = _globalCache.AsEnumerable();

                    if (!string.IsNullOrEmpty(keywordTen))
                        query = query.Where(x => x.TenTapThe.ToLower().Contains(keywordTen));

                    if (!string.IsNullOrEmpty(keywordDonVi))
                        query = query.Where(x => x.DonVi.ToLower().Contains(keywordDonVi));

                    // <--- THÊM ĐIỀU KIỆN LỌC HÌNH THỨC KHEN THƯỞNG --->
                    if (!string.IsNullOrEmpty(keywordHinhThuc))
                        query = query.Where(x => x.HinhThucKhenThuong.ToLower().Contains(keywordHinhThuc));

                    // Lấy tối đa 500 ID khớp để đưa xuống DB
                    var matchedItems = query.Take(500).ToDictionary(x => x.ID, x => x);

                    if (matchedItems.Count == 0 || token.IsCancellationRequested)
                        return CreateDataTableSchema();

                    // 2. QUERY DB CHỈ LẤY ĐÚNG NHỮNG ID ĐÃ KHỚP (Không quét thừa 1 dòng nào)
                    DataTable dt = CreateDataTableSchema();
                    string connectionString = $@"Data Source={_csdl4Path};";

                    using (SqliteConnection conn = new SqliteConnection(connectionString))
                    {
                        await conn.OpenAsync(token);
                        string inClause = string.Join(",", matchedItems.Keys);
                        string queryFull = $@"SELECT ID, STT, HinhThuc_KhenThuong, SoQuyetDinh, 
                                             NgayQuyetDinh, NguoiKy, NoiDung_KhenThuong, 
                                             TienThuong, NgayCapPhat, CanBoCapPhat, 
                                             NguoiDaiDienNhan, GhiChu 
                                      FROM ThongKe_KhenThuongTapThe 
                                      WHERE ID IN ({inClause}) ORDER BY STT ASC";

                        using (SqliteCommand cmdFull = new SqliteCommand(queryFull, conn))
                        using (SqliteDataReader reader = await cmdFull.ExecuteReaderAsync(token))
                        {
                            while (await reader.ReadAsync(token))
                            {
                                if (token.IsCancellationRequested) return null;

                                int id = Convert.ToInt32(reader["ID"]);
                                var cacheItem = matchedItems[id]; // Lấy Tên, Đơn vị, Hình thức từ RAM, khỏi giải mã lại

                                string tienThuongStr = SafeDecrypt(reader["TienThuong"]);
                                long.TryParse(tienThuongStr.Replace(".", "").Replace(",", ""), out long tien);

                                dt.Rows.Add(
                                    id,
                                    reader["STT"] != DBNull.Value ? Convert.ToInt32(reader["STT"]) : 0,
                                    cacheItem.TenTapThe,
                                    cacheItem.HinhThucKhenThuong, // <--- Lấy thẳng từ Cache cho nhanh
                                    cacheItem.DonVi,
                                    SafeDecrypt(reader["SoQuyetDinh"]),
                                    SafeDecrypt(reader["NgayQuyetDinh"]),
                                    SafeDecrypt(reader["NguoiKy"]),
                                    SafeDecrypt(reader["NoiDung_KhenThuong"]),
                                    tien,
                                    SafeDecrypt(reader["NgayCapPhat"]),
                                    SafeDecrypt(reader["CanBoCapPhat"]),
                                    SafeDecrypt(reader["NguoiDaiDienNhan"]),
                                    SafeDecrypt(reader["GhiChu"])
                                );
                            }
                        }
                    }
                    return dt;
                }, token);

                if (dtThuTu != null && !token.IsCancellationRequested)
                {
                    kryptonDataGridView1.DataSource = dtThuTu;
                    CapNhatNhanTongSo();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private DataTable CreateDataTableSchema()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("STT", typeof(int));
            dt.Columns.Add("TenTapThe", typeof(string));
            dt.Columns.Add("HinhThuc_KhenThuong", typeof(string));
            dt.Columns.Add("DonVi_CapKhenThuong", typeof(string));
            dt.Columns.Add("SoQuyetDinh", typeof(string));
            dt.Columns.Add("NgayQuyetDinh", typeof(string));
            dt.Columns.Add("NguoiKy", typeof(string));
            dt.Columns.Add("NoiDung_KhenThuong", typeof(string));
            dt.Columns.Add("TienThuong", typeof(long));
            dt.Columns.Add("NgayCapPhat", typeof(string));
            dt.Columns.Add("CanBoCapPhat", typeof(string));
            dt.Columns.Add("NguoiDaiDienNhan", typeof(string));
            dt.Columns.Add("GhiChu", typeof(string));
            return dt;
        }
        private async void kryptonButton_LamMoiCacOTimKiem_Click(object sender, EventArgs e)
        {
            // Ngắt event
            if (textBox_TimKiemTheoTen != null) textBox_TimKiemTheoTen.TextChanged -= textBox_TimKiemTheoTen_TextChanged;
            if (comboBox_TimKiemDonViKhenThuong != null) comboBox_TimKiemDonViKhenThuong.TextChanged -= comboBox_TimKiemDonViKhenThuong_TextChanged;
            if (comboBox1_HinhThucKT != null) comboBox1_HinhThucKT.TextChanged -= comboBox1_HinhThucKT_TextChanged; // <--- THÊM

            // Xóa trắng
            if (textBox_TimKiemTheoTen != null) textBox_TimKiemTheoTen.Text = string.Empty;
            if (comboBox_TimKiemDonViKhenThuong != null) comboBox_TimKiemDonViKhenThuong.Text = string.Empty;
            if (comboBox1_HinhThucKT != null) comboBox1_HinhThucKT.SelectedIndex = -1; // <--- THÊM (Trả về rỗng)

            // Bật lại event
            if (textBox_TimKiemTheoTen != null) textBox_TimKiemTheoTen.TextChanged += textBox_TimKiemTheoTen_TextChanged;
            if (comboBox_TimKiemDonViKhenThuong != null) comboBox_TimKiemDonViKhenThuong.TextChanged += comboBox_TimKiemDonViKhenThuong_TextChanged;
            if (comboBox1_HinhThucKT != null) comboBox1_HinhThucKT.TextChanged += comboBox1_HinhThucKT_TextChanged; // <--- THÊM

            await LoadDataToGridAsync();
            _ = ShowHanhDongStatusAsync("✔ Đã làm mới bộ lọc!", true);
        }
        // Thêm hàm này vào khu vực chứa các sự kiện tìm kiếm
        private async void comboBox1_HinhThucKT_TextChanged(object sender, EventArgs e)
        {
            await DebouncedSearchAsync();
        }
        private void ResetInput()
        {
            _selectedID = -1;
            isEditing = false;

            kryptonTextBox_STT.Text = "Tự động";
            kryptonTextBox_TenTapThe?.Clear();
            if (comboBox_HinhThucKhenThuong != null) comboBox_HinhThucKhenThuong.SelectedIndex = -1;
            if (comboBox_DonViKhenThuong != null) comboBox_DonViKhenThuong.SelectedIndex = -1;
            kryptonTextBox_SoQuyetDinh?.Clear();
            kryptonTextBox_NgayQuyetDinh?.Clear();
            kryptonTextBox_NguoiKy?.Clear();
            richTextBox1_NoiDungKhenThuong?.Clear();

            if (kryptonTextBox_TienThuong != null)
            {
                kryptonTextBox_TienThuong.TextChanged -= kryptonTextBox_TienThuong_TextChanged;
                kryptonTextBox_TienThuong.Text = "0";
                kryptonTextBox_TienThuong.TextChanged += kryptonTextBox_TienThuong_TextChanged;
            }
            kryptonTextBox_NgayCapPhat?.Clear();
            kryptonTextBox_CanBoCapPhat?.Clear();
            kryptonTextBox_NguoiDaiDienNhan?.Clear();
            kryptonTextBox_GhiChu?.Clear();

            if (kryptonButton_ThemKhenThuong != null) kryptonButton_ThemKhenThuong.Values.Text = "Thêm";

            if (kryptonButton_SuaVaLuuKhenThuong != null)
            {
                kryptonButton_SuaVaLuuKhenThuong.Enabled = false;
                kryptonButton_SuaVaLuuKhenThuong.Values.Text = "Sửa";
            }
            if (kryptonButton_XoaKhenThuong != null) kryptonButton_XoaKhenThuong.Enabled = false;
        }
        private void kryptonDataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || sender is not DataGridView grid) return;

            try
            {
                var row = grid.Rows[e.RowIndex];
                if (int.TryParse(row.Cells["ID"].Value?.ToString(), out int parsedId))
                {
                    _selectedID = parsedId;
                    isEditing = true;
                }
                else return;

                kryptonTextBox_STT.Text = row.Cells["STT"].Value?.ToString() ?? "";
                kryptonTextBox_TenTapThe.Text = row.Cells["TenTapThe"].Value?.ToString() ?? "";
                if (comboBox_HinhThucKhenThuong != null) comboBox_HinhThucKhenThuong.Text = row.Cells["HinhThuc_KhenThuong"].Value?.ToString() ?? "";
                if (comboBox_DonViKhenThuong != null) comboBox_DonViKhenThuong.Text = row.Cells["DonVi_CapKhenThuong"].Value?.ToString() ?? "";
                kryptonTextBox_SoQuyetDinh.Text = row.Cells["SoQuyetDinh"].Value?.ToString() ?? "";
                kryptonTextBox_NgayQuyetDinh.Text = row.Cells["NgayQuyetDinh"].Value?.ToString() ?? "";
                kryptonTextBox_NguoiKy.Text = row.Cells["NguoiKy"].Value?.ToString() ?? "";
                richTextBox1_NoiDungKhenThuong.Text = row.Cells["NoiDung_KhenThuong"].Value?.ToString() ?? "";
                kryptonTextBox_TienThuong.Text = row.Cells["TienThuong"].Value?.ToString() ?? "0";
                kryptonTextBox_NgayCapPhat.Text = row.Cells["NgayCapPhat"].Value?.ToString() ?? "";
                kryptonTextBox_CanBoCapPhat.Text = row.Cells["CanBoCapPhat"].Value?.ToString() ?? "";
                kryptonTextBox_NguoiDaiDienNhan.Text = row.Cells["NguoiDaiDienNhan"].Value?.ToString() ?? "";
                kryptonTextBox_GhiChu.Text = row.Cells["GhiChu"].Value?.ToString() ?? "";

                if (kryptonButton_SuaVaLuuKhenThuong != null)
                {
                    kryptonButton_SuaVaLuuKhenThuong.Enabled = true;
                    kryptonButton_SuaVaLuuKhenThuong.Values.Text = "Lưu";
                }
                if (kryptonButton_XoaKhenThuong != null) kryptonButton_XoaKhenThuong.Enabled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi CellClick Grid]: {ex.Message}");
            }
        }
        private bool KiemTraDuLieuDauVao()
        {
            if (string.IsNullOrWhiteSpace(kryptonTextBox_TenTapThe.Text))
            {
                MessageBox.Show("Vui lòng nhập 'Tên tập thể' (bắt buộc)!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                kryptonTextBox_TenTapThe.Focus();
                return false;
            }
            return true;
        }
        // CRUD: THÊM, SỬA, XÓA

        // =================================================================
        // 1. HÀM THÊM MỚI
        // =================================================================
        private async void kryptonButton_ThemKhenThuong_Click(object sender, EventArgs e)
        {
            if (kryptonButton_ThemKhenThuong.Values.Text == "Làm mới")
            {
                ResetInput();
                kryptonTextBox_TenTapThe.Focus();
                return;
            }

            if (!KiemTraDuLieuDauVao()) return;

            kryptonButton_ThemKhenThuong.Enabled = false;

            try
            {
                string tienThuongStr = kryptonTextBox_TienThuong.Text.Replace(".", "").Replace(",", "").Trim();
                if (string.IsNullOrWhiteSpace(tienThuongStr)) tienThuongStr = "0";

                string connectionString = $@"Data Source={_csdl4Path};";
                using (var conn = new SqliteConnection(connectionString))
                {
                    await conn.OpenAsync();
                    using var tran = conn.BeginTransaction();
                    try
                    {
                        int newStt = 1;
                        using (var cmdMaxStt = new SqliteCommand("SELECT COALESCE(MAX(STT), 0) FROM ThongKe_KhenThuongTapThe", conn, tran))
                        {
                            var maxSttObj = await cmdMaxStt.ExecuteScalarAsync();
                            if (maxSttObj != null && maxSttObj != DBNull.Value)
                            {
                                newStt = Convert.ToInt32(maxSttObj) + 1;
                            }
                        }

                        string sqlInsert = @"INSERT INTO ThongKe_KhenThuongTapThe 
                                     (STT, TenTapThe, HinhThuc_KhenThuong, DonVi_CapKhenThuong, 
                                      SoQuyetDinh, NgayQuyetDinh, NguoiKy, NoiDung_KhenThuong, 
                                      TienThuong, NgayCapPhat, CanBoCapPhat, NguoiDaiDienNhan, GhiChu) 
                                     VALUES 
                                     (@STT, @TenTapThe, @HinhThuc_KhenThuong, @DonVi_CapKhenThuong, 
                                      @SoQuyetDinh, @NgayQuyetDinh, @NguoiKy, @NoiDung_KhenThuong, 
                                      @TienThuong, @NgayCapPhat, @CanBoCapPhat, @NguoiDaiDienNhan, @GhiChu)";

                        using (var cmd = new SqliteCommand(sqlInsert, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@STT", newStt);
                            cmd.Parameters.AddWithValue("@TenTapThe", SafeEncrypt(kryptonTextBox_TenTapThe.Text));
                            cmd.Parameters.AddWithValue("@HinhThuc_KhenThuong", SafeEncrypt(comboBox_HinhThucKhenThuong?.Text));
                            cmd.Parameters.AddWithValue("@DonVi_CapKhenThuong", SafeEncrypt(comboBox_DonViKhenThuong?.Text));
                            cmd.Parameters.AddWithValue("@SoQuyetDinh", SafeEncrypt(kryptonTextBox_SoQuyetDinh.Text));
                            cmd.Parameters.AddWithValue("@NgayQuyetDinh", SafeEncrypt(kryptonTextBox_NgayQuyetDinh.Text));
                            cmd.Parameters.AddWithValue("@NguoiKy", SafeEncrypt(kryptonTextBox_NguoiKy.Text));
                            cmd.Parameters.AddWithValue("@NoiDung_KhenThuong", SafeEncrypt(richTextBox1_NoiDungKhenThuong.Text));
                            cmd.Parameters.AddWithValue("@TienThuong", SafeEncrypt(tienThuongStr));
                            cmd.Parameters.AddWithValue("@NgayCapPhat", SafeEncrypt(kryptonTextBox_NgayCapPhat.Text));
                            cmd.Parameters.AddWithValue("@CanBoCapPhat", SafeEncrypt(kryptonTextBox_CanBoCapPhat.Text));
                            cmd.Parameters.AddWithValue("@NguoiDaiDienNhan", SafeEncrypt(kryptonTextBox_NguoiDaiDienNhan.Text));
                            cmd.Parameters.AddWithValue("@GhiChu", SafeEncrypt(kryptonTextBox_GhiChu.Text));

                            await cmd.ExecuteNonQueryAsync();
                        }
                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }

                _ = ShowHanhDongStatusAsync("✔ Thêm mới dữ liệu thành công!", true);

                // ================= FIX LỖI Ở ĐÂY =================
                _globalCache = null; // 1. XÓA SẠCH CACHE CŨ TRÊN RAM
                await LoadComboBoxDonViAsync(); // 2. ÉP NẠP LẠI CACHE TỪ DB (Gồm cả dữ liệu vừa thêm)
                await LoadComboBoxHinhThucKTAsync();
                await LoadDataToGridAsync(); // 3. GRID SẼ HIỂN THỊ DỮ LIỆU MỚI NHẤT TỪ CACHE
                ResetInput();
                // =================================================
            }
            catch (Exception ex)
            {
                _ = ShowHanhDongStatusAsync("❌ Lỗi khi thêm mới dữ liệu!", false);
                MessageBox.Show("Lỗi khi thêm dữ liệu:\n" + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                kryptonButton_ThemKhenThuong.Enabled = true;
            }
        }
        private async void kryptonButton_SuaVaLuuKhenThuong_Click(object sender, EventArgs e)
        {
            if (!isEditing || _selectedID < 0) return;
            if (!KiemTraDuLieuDauVao()) return;

            kryptonButton_SuaVaLuuKhenThuong.Enabled = false;

            try
            {
                string tienThuongStr = kryptonTextBox_TienThuong.Text.Replace(".", "").Replace(",", "").Trim();
                if (string.IsNullOrWhiteSpace(tienThuongStr)) tienThuongStr = "0";

                string connectionString = $@"Data Source={_csdl4Path};";
                using (var conn = new SqliteConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = @"UPDATE ThongKe_KhenThuongTapThe SET 
                             TenTapThe = @TenTapThe, 
                             HinhThuc_KhenThuong = @HinhThuc_KhenThuong, 
                             DonVi_CapKhenThuong = @DonVi_CapKhenThuong, SoQuyetDinh = @SoQuyetDinh, 
                             NgayQuyetDinh = @NgayQuyetDinh, NguoiKy = @NguoiKy, 
                             NoiDung_KhenThuong = @NoiDung_KhenThuong, TienThuong = @TienThuong, 
                             NgayCapPhat = @NgayCapPhat, CanBoCapPhat = @CanBoCapPhat, 
                             NguoiDaiDienNhan = @NguoiDaiDienNhan, GhiChu = @GhiChu 
                             WHERE ID = @ID";

                    using (var cmd = new SqliteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", _selectedID);
                        cmd.Parameters.AddWithValue("@TenTapThe", SafeEncrypt(kryptonTextBox_TenTapThe.Text));
                        cmd.Parameters.AddWithValue("@HinhThuc_KhenThuong", SafeEncrypt(comboBox_HinhThucKhenThuong?.Text));
                        cmd.Parameters.AddWithValue("@DonVi_CapKhenThuong", SafeEncrypt(comboBox_DonViKhenThuong?.Text));
                        cmd.Parameters.AddWithValue("@SoQuyetDinh", SafeEncrypt(kryptonTextBox_SoQuyetDinh.Text));
                        cmd.Parameters.AddWithValue("@NgayQuyetDinh", SafeEncrypt(kryptonTextBox_NgayQuyetDinh.Text));
                        cmd.Parameters.AddWithValue("@NguoiKy", SafeEncrypt(kryptonTextBox_NguoiKy.Text));
                        cmd.Parameters.AddWithValue("@NoiDung_KhenThuong", SafeEncrypt(richTextBox1_NoiDungKhenThuong.Text));
                        cmd.Parameters.AddWithValue("@TienThuong", SafeEncrypt(tienThuongStr));
                        cmd.Parameters.AddWithValue("@NgayCapPhat", SafeEncrypt(kryptonTextBox_NgayCapPhat.Text));
                        cmd.Parameters.AddWithValue("@CanBoCapPhat", SafeEncrypt(kryptonTextBox_CanBoCapPhat.Text));
                        cmd.Parameters.AddWithValue("@NguoiDaiDienNhan", SafeEncrypt(kryptonTextBox_NguoiDaiDienNhan.Text));
                        cmd.Parameters.AddWithValue("@GhiChu", SafeEncrypt(kryptonTextBox_GhiChu.Text));

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                _ = ShowHanhDongStatusAsync("✔ Cập nhật dữ liệu thành công!", true);

                // ================= FIX LỖI Ở ĐÂY =================
                _globalCache = null; // XÓA SẠCH CACHE CŨ
                await LoadComboBoxDonViAsync();
                await LoadComboBoxHinhThucKTAsync();
                await LoadDataToGridAsync();
                ResetInput();
                // =================================================
            }
            catch (Exception ex)
            {
                _ = ShowHanhDongStatusAsync("❌ Lỗi khi cập nhật dữ liệu!", false);
                MessageBox.Show("Lỗi khi cập nhật dữ liệu:\n" + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                kryptonButton_SuaVaLuuKhenThuong.Enabled = true;
            }
        }
        private async void kryptonButton_XoaKhenThuong_Click(object sender, EventArgs e)
        {
            if (!isEditing || _selectedID < 0) return;

            string tenTapThe = kryptonTextBox_TenTapThe.Text?.Trim() ?? string.Empty;
            if (MessageBox.Show($"Bạn có chắc chắn muốn xóa bản ghi:\n{tenTapThe}?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
            {
                return;
            }

            kryptonButton_XoaKhenThuong.Enabled = false;

            try
            {
                string connectionString = $@"Data Source={_csdl4Path};";

                using (var conn = new SqliteConnection(connectionString))
                {
                    await conn.OpenAsync();
                    using var tran = conn.BeginTransaction();
                    try
                    {
                        const string query = @"DELETE FROM ""ThongKe_KhenThuongTapThe"" WHERE ID = @ID;";
                        using (var cmd = new SqliteCommand(query, conn, tran))
                        {
                            cmd.Parameters.Add("@ID", SqliteType.Integer).Value = _selectedID;
                            await cmd.ExecuteNonQueryAsync();
                        }
                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }

                await DanhLaiSoThuTuAsync();
                _ = ShowHanhDongStatusAsync("✔ Xóa dữ liệu thành công!", true);

                // ================= FIX LỖI Ở ĐÂY =================
                _globalCache = null; // XÓA SẠCH CACHE BẢN GHI ĐÃ BỊ XÓA
                await LoadComboBoxDonViAsync();
                await LoadComboBoxHinhThucKTAsync();
                await LoadDataToGridAsync();
                ResetInput();
                // =================================================
            }
            catch (Exception ex)
            {
                _ = ShowHanhDongStatusAsync("❌ Lỗi khi xóa dữ liệu!", false);
                MessageBox.Show("Lỗi khi xóa dữ liệu:\n\n" + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                kryptonButton_XoaKhenThuong.Enabled = true;
            }
        }
        // CÁC HÀM TIỆN ÍCH & TỐI ƯU CƠ SỞ DỮ LIỆU
        private async Task DanhLaiSoThuTuAsync()
        {
            string connectionString = $@"Data Source={_csdl4Path};";
            using (var conn = new SqliteConnection(connectionString))
            {
                await conn.OpenAsync();

                // Cú pháp UPDATE ... FROM của SQLite xử lý cực nhanh vì nó join bảng ảo chỉ 1 lần.
                string updateQuery = @"
        WITH RankedData AS (
            SELECT ID, ROW_NUMBER() OVER (ORDER BY STT ASC, ID ASC) AS NewSTT
            FROM ThongKe_KhenThuongTapThe
        )
        UPDATE ThongKe_KhenThuongTapThe
        SET STT = RankedData.NewSTT
        FROM RankedData
        WHERE ThongKe_KhenThuongTapThe.ID = RankedData.ID 
          AND ThongKe_KhenThuongTapThe.STT != RankedData.NewSTT;";

                using (var cmd = new SqliteCommand(updateQuery, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
        private void CapNhatNhanTongSo()
        {
            int total = kryptonDataGridView1.Rows.Count;
            if (toolStripStatusLabel1 != null)
            {
                if (total > 0)
                {
                    toolStripStatusLabel1.Text = $"Tổng cộng: {total} mục khen thưởng";
                    toolStripStatusLabel1.Visible = true;
                }
                else
                {
                    toolStripStatusLabel1.Visible = false;
                }
            }
        }
        private async Task ShowHanhDongStatusAsync(string message, bool isSuccess = true)
        {
            if (toolStripStatusLabel2 == null) return;
            int currentCounter = ++_msgCounter;

            toolStripStatusLabel2.Text = message;
            toolStripStatusLabel2.ForeColor = isSuccess ? Color.SeaGreen : Color.Red;
            toolStripStatusLabel2.Visible = true;

            await Task.Delay(2000);

            if (_msgCounter == currentCounter)
            {
                toolStripStatusLabel2.Visible = false;
            }
        }
        public void KiemTraVaTaoBangThongKeKhenThuongTapThe()
        {
            string connectionString = $@"Data Source={_csdl4Path};";
            using (SqliteConnection conn = new SqliteConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string checkTableQuery = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='ThongKe_KhenThuongTapThe'";
                    using (SqliteCommand cmdCheck = new SqliteCommand(checkTableQuery, conn))
                    {
                        int tableCount = Convert.ToInt32(cmdCheck.ExecuteScalar());
                        if (tableCount == 0)
                        {
                            string createTableQuery = @"
                            CREATE TABLE IF NOT EXISTS ""ThongKe_KhenThuongTapThe"" (
                                ""ID"" INTEGER,
                                ""STT"" INTEGER,
                                ""TenTapThe"" TEXT,
                                ""HinhThuc_KhenThuong"" TEXT,
                                ""DonVi_CapKhenThuong"" TEXT,
                                ""SoQuyetDinh"" TEXT,
                                ""NgayQuyetDinh"" TEXT,
                                ""NguoiKy"" TEXT,
                                ""NoiDung_KhenThuong"" TEXT,
                                ""TienThuong"" TEXT,
                                ""NgayCapPhat"" TEXT,
                                ""CanBoCapPhat"" TEXT,
                                ""NguoiDaiDienNhan"" TEXT,
                                ""GhiChu"" TEXT,
                                PRIMARY KEY(""ID"")
                            );
                            CREATE INDEX IF NOT EXISTS ""idx_kt_tentapthe"" ON ""ThongKe_KhenThuongTapThe"" (""TenTapThe"");
                            ";
                            using (SqliteCommand cmdCreate = new SqliteCommand(createTableQuery, conn)) { cmdCreate.ExecuteNonQuery(); }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Lỗi Khởi tạo DB Form49]: {ex.Message}");
                    MessageBox.Show("Lỗi kết nối cơ sở dữ liệu. Vui lòng kiểm tra phân quyền thư mục hoặc tệp CSDL.\n\nChi tiết: " + ex.Message,
                        "Lỗi nghiêm trọng", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        // CẤU HÌNH GIAO DIỆN
        private void CauHinhGridCoBan(DataGridView dgv)
        {
            dgv.AutoGenerateColumns = false;
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.MultiSelect = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.RowHeadersVisible = false;
            dgv.EnableHeadersVisualStyles = false;
            dgv.ScrollBars = ScrollBars.Both;
            dgv.BorderStyle = BorderStyle.None;

            // --- THÊM CÁC THUỘC TÍNH KHÓA CỘT TẠI ĐÂY ---
            dgv.AllowUserToOrderColumns = false;  // Khóa: Không cho kéo thả đổi vị trí cột
            dgv.AllowUserToResizeColumns = false; // Khóa: Không cho kéo giãn thay đổi độ rộng cột
        }
        private void CauHinhStyleWeb(DataGridView dgv) { dgv.RowTemplate.Height = 36; dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None; dgv.AllowUserToResizeRows = false; dgv.ColumnHeadersHeight = 60; dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing; dgv.CellBorderStyle = DataGridViewCellBorderStyle.Single; dgv.GridColor = Color.FromArgb(224, 224, 224); dgv.RowsDefaultCellStyle.BackColor = Color.White; dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252); dgv.BackgroundColor = Color.White; if (dgv is Krypton.Toolkit.KryptonDataGridView kDgv) { kDgv.GridStyles.Style = Krypton.Toolkit.DataGridViewStyle.List; kDgv.StateCommon.HeaderColumn.Content.Padding = new System.Windows.Forms.Padding(6, 8, 6, 8); kDgv.StateCommon.HeaderColumn.Content.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold); kDgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; kDgv.StateCommon.DataCell.Content.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular); kDgv.StateCommon.DataCell.Content.Padding = new System.Windows.Forms.Padding(6, 8, 6, 8); kDgv.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All; kDgv.StateCommon.DataCell.Border.Color1 = System.Drawing.Color.FromArgb(224, 224, 224); kDgv.StateCommon.DataCell.Border.Width = 1; kDgv.StateSelected.DataCell.Back.Color1 = System.Drawing.Color.FromArgb(232, 244, 253); kDgv.StateSelected.DataCell.Back.Color2 = System.Drawing.Color.FromArgb(232, 244, 253); kDgv.StateSelected.DataCell.Content.Color1 = System.Drawing.Color.FromArgb(0, 102, 204); kDgv.Margin = new Padding(0, 0, 0, 30); } }
        private void CauHinhCotGrid(DataGridView dgv)
        {
            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ID", Name = "ID", Visible = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "STT", Name = "STT", HeaderText = "STT", Width = 50, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TenTapThe", Name = "TenTapThe", HeaderText = "Tên tập thể", Width = 220, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "HinhThuc_KhenThuong", Name = "HinhThuc_KhenThuong", HeaderText = "Hình thức khen thưởng", Width = 150, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DonVi_CapKhenThuong", Name = "DonVi_CapKhenThuong", HeaderText = "Đơn vị khen thưởng", Width = 180, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SoQuyetDinh", Name = "SoQuyetDinh", HeaderText = "Số Quyết định", Width = 100, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NgayQuyetDinh", Name = "NgayQuyetDinh", HeaderText = "Ngày Quyết định", Width = 100, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NguoiKy", Name = "NguoiKy", HeaderText = "Người ký", Width = 130, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NoiDung_KhenThuong", Name = "NoiDung_KhenThuong", HeaderText = "Nội dung khen thưởng", Width = 250, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TienThuong", Name = "TienThuong", HeaderText = "Tiền thưởng (VNĐ)", Width = 140, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N0" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NgayCapPhat", Name = "NgayCapPhat", HeaderText = "Ngày cấp phát", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CanBoCapPhat", Name = "CanBoCapPhat", HeaderText = "Cán bộ cấp", Width = 130, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NguoiDaiDienNhan", Name = "NguoiDaiDienNhan", HeaderText = "Người đại diện nhận", Width = 150, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "GhiChu", Name = "GhiChu", HeaderText = "Ghi chú", Width = 150, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });

            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }
        private void kryptonButton1_Thoat_Click(object sender, EventArgs e)
        {
            Form34_ThongKeKhenThuong form34 = Application.OpenForms.OfType<Form34_ThongKeKhenThuong>().FirstOrDefault();
            if (form34 != null) { form34.Show(); form34.BringToFront(); }
            else { form34 = new Form34_ThongKeKhenThuong(); form34.Show(); }

            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            string tieuDeForm = "Trang Quản lý khen thưởng CBCS năm " +
                            Module_NamHeThong.LayNamHeThong();
            if (formCha != null) { formCha.CapNhatTieuDe(tieuDeForm); }
            this.Close();
        }
        private void kryptonButton2_TangCoChuRichText_Click(object sender, EventArgs e)
        {
            float currentSize = richTextBox1_NoiDungKhenThuong.Font.Size;
            if (currentSize < 36.0f) { richTextBox1_NoiDungKhenThuong.Font = new Font(richTextBox1_NoiDungKhenThuong.Font.FontFamily, currentSize + 2, richTextBox1_NoiDungKhenThuong.Font.Style); }
        }
        private void kryptonButton2_GiamCoChuRichText_Click(object sender, EventArgs e)
        {
            float currentSize = richTextBox1_NoiDungKhenThuong.Font.Size;
            if (currentSize > 8.0f) { richTextBox1_NoiDungKhenThuong.Font = new Font(richTextBox1_NoiDungKhenThuong.Font.FontFamily, currentSize - 2, richTextBox1_NoiDungKhenThuong.Font.Style); }
        }
        // HÀM XÓA TOÀN BỘ DỮ LIỆU
        // TẢI VÀ CACHE DANH SÁCH HÌNH THỨC KHEN THƯỞNG VÀO BỘ LỌC
        private async Task LoadComboBoxHinhThucKTAsync(bool forceReload = false)
        {
            if (comboBox1_HinhThucKT == null) return;

            // Đảm bảo cache RAM đã có sẵn
            await BuildGlobalCacheAsync(forceReload);

            comboBox1_HinhThucKT.TextChanged -= comboBox1_HinhThucKT_TextChanged;
            string currentText = comboBox1_HinhThucKT.Text;

            // Rút trích danh sách Hình thức trực tiếp từ RAM (Cực kỳ nhanh và tự động bỏ trùng)
            var dsHinhThuc = _globalCache
                .Where(x => !string.IsNullOrWhiteSpace(x.HinhThucKhenThuong) && x.HinhThucKhenThuong != "[Lỗi giải mã]")
                .Select(x => x.HinhThucKhenThuong)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToArray();

            comboBox1_HinhThucKT.Items.Clear();
            comboBox1_HinhThucKT.Items.Add(""); // Thêm 1 dòng rỗng ở đầu để người dùng có thể xóa lọc
            comboBox1_HinhThucKT.Items.AddRange(dsHinhThuc);
            comboBox1_HinhThucKT.Text = currentText;

            comboBox1_HinhThucKT.TextChanged += comboBox1_HinhThucKT_TextChanged;
        }
        private async void xoaToanBoDuLieu_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (!System.IO.File.Exists(_csdl4Path))
                {
                    MessageBox.Show("Không tìm thấy tệp cơ sở dữ liệu!", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                int tongSoDong = 0;
                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_csdl4Path}"))
                {
                    await conn.OpenAsync();
                    using (var cmd = new Microsoft.Data.Sqlite.SqliteCommand("SELECT COUNT(*) FROM ThongKe_KhenThuongTapThe", conn))
                    {
                        tongSoDong = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }
                }

                if (tongSoDong == 0)
                {
                    MessageBox.Show("Hiện tại không có dữ liệu thống kê khen thưởng tập thể nào để xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kiểm tra dữ liệu trước khi xóa: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (Form24_XacMinhAdmin frmXacMinh = new Form24_XacMinhAdmin())
            {
                frmXacMinh.TopMost = true;
                frmXacMinh.StartPosition = FormStartPosition.CenterScreen;
                if (frmXacMinh.ShowDialog() != DialogResult.OK) return;
            }

            DialogResult result = MessageBox.Show(
                "CẢNH BÁO NGUY HIỂM:\n\nBạn đang yêu cầu XÓA TOÀN BỘ dữ liệu khen thưởng tập thể.\nHành động này KHÔNG THỂ KHÔI PHỤC!\n\nBạn có chắc chắn muốn XÓA SẠCH?",
                "Xác nhận xóa toàn bộ",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes) return;

            try
            {
                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_csdl4Path}"))
                {
                    await conn.OpenAsync();
                    using (var tran = conn.BeginTransaction())
                    {
                        try
                        {
                            using (var cmd = new SqliteCommand("DELETE FROM ThongKe_KhenThuongTapThe", conn, tran))
                            {
                                await cmd.ExecuteNonQueryAsync();
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
                        string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM) ? "Không xác định" : Module_TaiKhoan.TenTaiKhoan_RAM,
                        "Xóa TOÀN BỘ dữ liệu khen thưởng Tập thể",
                        $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                    );
                }
                catch { }

                _ = ShowHanhDongStatusAsync("✔ Đã xóa sạch toàn bộ dữ liệu!", true);

                await LoadDataToGridAsync();
                await LoadComboBoxHinhThucKTAsync(true); // Ép update combobox
                await LoadComboBoxDonViAsync(true); // Ép update combobox
                ResetInput();
            }
            catch (Exception ex)
            {
                _ = ShowHanhDongStatusAsync("❌ Lỗi khi xóa dữ liệu!", false);
                MessageBox.Show("Lỗi trong quá trình xóa dữ liệu:\n\n" + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void xoaTimKiem_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            kryptonButton_LamMoiCacOTimKiem.PerformClick();
        }
        // HÀM XUẤT EXCEL (TỐI ƯU CLOSEDXML BULK INSERT)       
        private async void xuatDuLieuRaTepExcel_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (kryptonDataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Title = "Chọn nơi lưu file Excel thống kê",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"ThongKe_KhenThuongTapThe_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;
            string filePath = sfd.FileName;

            string textBanDau = xuatDuLieuRaTepExcel_ToolStripMenuItem.Text;
            Form_Loading frmLoad = new Form_Loading("Đang tạo tệp Excel, vui lòng đợi...");
            bool isLoadShown = false;
            frmLoad.Icon = this.Icon;

            try
            {
                this.Enabled = false;
                xuatDuLieuRaTepExcel_ToolStripMenuItem.Text = "Đang xuất...";

                frmLoad.Show(this);
                isLoadShown = true;
                await Task.Delay(50);

                // Lấy data đẩy vào List (Chuẩn bị cho InsertData)
                var exportCols = kryptonDataGridView1.Columns.Cast<DataGridViewColumn>()
                    .Where(c => c.Visible && c.Name != "ID")
                    .ToList();

                int rowCount = kryptonDataGridView1.Rows.Count;
                int colCount = exportCols.Count;

                string[] headerArray = new string[colCount];
                double[] colWidths = new double[colCount];
                var dataList = new List<object[]>(rowCount);

                for (int c = 0; c < colCount; c++)
                {
                    headerArray[c] = exportCols[c].HeaderText;
                    colWidths[c] = exportCols[c].Width / 7.5;
                }

                for (int r = 0; r < rowCount; r++)
                {
                    var rowValues = new object[colCount];
                    for (int c = 0; c < colCount; c++)
                    {
                        rowValues[c] = kryptonDataGridView1.Rows[r].Cells[exportCols[c].Index].Value?.ToString() ?? "";
                    }
                    dataList.Add(rowValues);
                }

                try
                {
                    Module_NhatKy.GhiNhatKy(
                        string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM) ? "Không xác định" : Module_TaiKhoan.TenTaiKhoan_RAM,
                        "Xuất Excel bảng thống kê khen thưởng Tập thể",
                        $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
                }
                catch { }

                await Task.Run(() =>
                {
                    using var wb = new ClosedXML.Excel.XLWorkbook();
                    var ws = wb.Worksheets.Add("ThongKeKhenThuongTapThe");

                    ws.Style.Font.FontName = "Times New Roman";
                    ws.Style.Font.FontSize = 11;

                    ws.Cell("A1").Value = "DANH SÁCH";
                    ws.Range(1, 1, 1, colCount).Merge().Style.Font.SetBold().Font.SetFontSize(14)
                        .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

                    ws.Cell("A2").Value = "THỐNG KÊ KẾT QUẢ KHEN THƯỞNG TẬP THỂ";
                    ws.Range(2, 1, 2, colCount).Merge().Style.Font.SetBold().Font.SetFontSize(13)
                        .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

                    int excelStartRow = 4;

                    for (int c = 0; c < colCount; c++)
                    {
                        var cell = ws.Cell(excelStartRow, c + 1);
                        cell.Value = string.IsNullOrWhiteSpace(headerArray[c]) ? exportCols[c].Name : headerArray[c];
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
                    totalCell.Value = $"Tổng cộng: {rowCount} tập thể được thống kê./.";
                    totalCell.Style.Font.SetBold().Font.SetItalic();
                    ws.Range(tongDong, 1, tongDong, colCount).Merge();

                    ws.PageSetup.PaperSize = ClosedXML.Excel.XLPaperSize.A4Paper;
                    ws.PageSetup.PageOrientation = ClosedXML.Excel.XLPageOrientation.Landscape;
                    ws.PageSetup.FitToPages(1, 0);
                    ws.PageSetup.Margins.Top = 0.5;
                    ws.PageSetup.Margins.Bottom = 0.5;
                    ws.PageSetup.Margins.Left = 0.4;
                    ws.PageSetup.Margins.Right = 0.4;

                    // Module_BanQuyen.DongDauExcel(wb); 

                    wb.SaveAs(filePath);
                });

                try
                {
                    Module_XuatNhapDuLieuThiDua.MoVaChonTepTrongExplorer(filePath);
                }
                catch { }

                _ = ShowHanhDongStatusAsync("✔ Xuất tệp Excel thành công!", true);
            }
            catch (Exception ex)
            {
                _ = ShowHanhDongStatusAsync("❌ Lỗi xuất dữ liệu Excel!", false);
                MessageBox.Show("Lỗi xuất dữ liệu:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (isLoadShown)
                {
                    frmLoad.Close();
                    this.Enabled = true;
                    this.Focus();
                }
                xuatDuLieuRaTepExcel_ToolStripMenuItem.Text = textBanDau;
            }
        }

        private void toolStripMenuItem_QuayLaiTrangTruoc_Click(object sender, EventArgs e)
        {
            kryptonButton1_Thoat.PerformClick();
        }


        
    }
}