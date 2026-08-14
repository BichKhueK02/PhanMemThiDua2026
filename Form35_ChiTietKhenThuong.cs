using Microsoft.Data.Sqlite;
using System.Data;

namespace PhanMemThiDua2026
{
    public partial class Form35_ChiTietKhenThuong : Form
    {
        private string _currentSoHieu = "";
        private string _currentHoTen = "";
        private string _currentDonVi = "";
        private string _currentTinhTrang = "";
        private int _currentEditingID = -1;
        private readonly string _csdl4Path = Module_DanduongGPS.DuongDanCSDL4;
        public Form35_ChiTietKhenThuong()
        {
            InitializeComponent();
            kryptonDataGridView1_DanhSachCBCS.CellClick += KryptonDataGridView1_DanhSachCBCS_CellClick;
            kryptonDataGridView1_DanhSachCBCS.RowPostPaint += KryptonDataGridView1_DanhSachCBCS_RowPostPaint;

            // GẮN SỰ KIỆN ĐỊNH DẠNG TIỀN THƯỞNG
            if (kryptonTextBox_TienThuong != null)
            {
                kryptonTextBox_TienThuong.TextChanged += kryptonTextBox_TienThuong_TextChanged;
            }
        }
        private void Form37_ChiTietKhenThuong_Load(object sender, EventArgs e)
        {
            InitToolTips();
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
                // ================= BỔ SUNG CÁC NÚT MỚI TẠI ĐÂY =================
                GanToolTipAnToan(
                    kryptonButton_Them,
                    "Thêm thông tin khen thưởng mới vào hệ thống");

                GanToolTipAnToan(
                    kryptonButton_Sua,
                    "Cập nhật, chỉnh sửa thông tin đang được chọn");

                GanToolTipAnToan(
                    kryptonButton_Xoa,
                    "Xóa vĩnh viễn dữ liệu khen thưởng đang chọn");

                GanToolTipAnToan(
                    kryptonButton3_DongForm,
                    "Đóng cửa sổ này và quay lại màn hình chính");
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
        // HÀM ĐỊNH DẠNG TIỀN THƯỞNG (TỐI ƯU KHÔNG REGEX)
        private void kryptonTextBox_TienThuong_TextChanged(object sender, EventArgs e)
        {
            // ============================================================
            // 1. KIỂM TRA AN TOÀN
            // ============================================================

            if (kryptonTextBox_TienThuong == null ||
                kryptonTextBox_TienThuong.IsDisposed ||
                kryptonTextBox_TienThuong.Disposing)
            {
                return;
            }

            // Người dùng chưa nhập gì -> giữ ô trống.
            // Không tự động hiển thị "0".
            if (string.IsNullOrWhiteSpace(kryptonTextBox_TienThuong.Text))
                return;

            kryptonTextBox_TienThuong.TextChanged -=
                kryptonTextBox_TienThuong_TextChanged;

            try
            {
                string textHienTai = kryptonTextBox_TienThuong.Text.Trim();

                // ========================================================
                // 2. LƯU VỊ TRÍ CON TRỎ
                // ========================================================

                int cursorFromEnd =
                    textHienTai.Length -
                    kryptonTextBox_TienThuong.SelectionStart;

                // ========================================================
                // 3. LOẠI BỎ DẤU PHÂN CÁCH
                // ========================================================

                string rawText = textHienTai
                    .Replace(".", string.Empty)
                    .Replace(",", string.Empty)
                    .Trim();

                // ========================================================
                // 4. CHỈ GIỮ LẠI KÝ TỰ SỐ
                // ========================================================

                string cleanText = new string(
                    rawText.Where(char.IsDigit).ToArray());

                // Nếu sau khi lọc không còn số:
                // giữ TextBox trống thay vì ép thành "0".
                if (string.IsNullOrEmpty(cleanText))
                {
                    kryptonTextBox_TienThuong.Clear();
                    return;
                }

                // ========================================================
                // 5. GIỚI HẠN GIÁ TRỊ LONG
                // ========================================================

                if (!long.TryParse(cleanText, out long tienThuong))
                {
                    // Không thể chuyển đổi -> giữ nguyên nội dung hợp lệ
                    // gần nhất thay vì tự động đưa về "0".
                    return;
                }

                // ========================================================
                // 6. ĐẢM BẢO GIÁ TRỊ KHÔNG ÂM
                // ========================================================

                if (tienThuong < 0)
                    tienThuong = 0;

                // ========================================================
                // 7. ĐỊNH DẠNG TIỀN
                // ========================================================

                string textDaDinhDang =
                    string.Format("{0:#,##0}", tienThuong)
                        .Replace(",", ".");

                kryptonTextBox_TienThuong.Text = textDaDinhDang;

                // ========================================================
                // 8. KHÔI PHỤC VỊ TRÍ CON TRỎ
                // ========================================================

                int newCursorPosition =
                    kryptonTextBox_TienThuong.Text.Length - cursorFromEnd;

                kryptonTextBox_TienThuong.SelectionStart =
                    Math.Max(
                        0,
                        Math.Min(
                            newCursorPosition,
                            kryptonTextBox_TienThuong.Text.Length));
            }
            finally
            {
                // Luôn đăng ký lại sự kiện kể cả khi có lỗi.
                kryptonTextBox_TienThuong.TextChanged +=
                    kryptonTextBox_TienThuong_TextChanged;
            }
        }
        // HÀM BẢO VỆ GIAO DIỆN (LỘT VỎ MÃ HÓA AN TOÀN)
        private string SafeDecrypt(object value)
        {
            if (value == null || value == DBNull.Value) return "";
            string s = value.ToString()?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(s)) return "";

            string decrypted = BaoMatAES.GiaiMa(s);
            // Kỹ thuật Fallback: Nếu giải mã rỗng (có thể do lỗi hoặc là text thường), trả về chuỗi ban đầu
            return string.IsNullOrEmpty(decrypted) ? s : decrypted;
        }
        public void NhanDuLieuTuForm36(string hoTen, string soHieu, string donVi, string tinhTrang)
        {
            _currentHoTen = hoTen;
            _currentSoHieu = soHieu;
            _currentDonVi = donVi;
            _currentTinhTrang = tinhTrang;
            if (label1_HoVaTen != null) label1_HoVaTen.Text = "Đồng chí: " + hoTen;
            if (label1_SoHieu != null) label1_SoHieu.Text = "Số hiệu: " + soHieu;
            if (label1_DonVi != null) label1_DonVi.Text = "Đơn vị: " + donVi;
            XoaTrangGiaoDien();
            ReloadDuLieuGiayKhen_CuaMotNguoi();
        }
        private void ReloadDuLieuGiayKhen_CuaMotNguoi()
        {
            try
            {
                using var conn = new SqliteConnection($"Data Source={_csdl4Path}");
                conn.Open();

                // Quét toàn bộ để xử lý trên RAM
                string sql = "SELECT * FROM ThongKe_GiayKhen ORDER BY ID ASC";
                using var cmd = new SqliteCommand(sql, conn);
                using var reader = cmd.ExecuteReader();

                DataTable dt = new DataTable();

                // KIẾN TẠO CỘT: Ép kiểu 'long' cho cột Tiền thưởng để Grid format được chữ số
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string colName = reader.GetName(i);
                    if (colName == "ID")
                        dt.Columns.Add(colName, typeof(int));
                    else if (colName == "TienThuong")
                        dt.Columns.Add(colName, typeof(long));
                    else
                        dt.Columns.Add(colName, typeof(string));
                }

                while (reader.Read())
                {
                    // Lấy số hiệu ra để kiểm tra
                    string soHieuGiaiMa = SafeDecrypt(reader["SoHieu"]);

                    // Chỉ hốt những dòng khớp đúng số hiệu
                    if (string.Equals(soHieuGiaiMa, _currentSoHieu, StringComparison.OrdinalIgnoreCase))
                    {
                        DataRow row = dt.NewRow();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string colName = reader.GetName(i);
                            if (colName == "ID")
                            {
                                row[i] = Convert.ToInt32(reader[i]);
                            }
                            else if (colName == "TienThuong")
                            {
                                string tienStr = SafeDecrypt(reader[i]);
                                long.TryParse(tienStr.Replace(".", "").Replace(",", ""), out long tien);
                                row[i] = tien; // Nạp kiểu số thực sự vào DataTable
                            }
                            else
                            {
                                // Lột vỏ dữ liệu
                                row[i] = SafeDecrypt(reader[i]);
                            }
                        }
                        dt.Rows.Add(row);
                    }
                }

                // 🔥 FIX LỖI KHÔNG LOAD GRID: Bật AutoGenerateColumns = true vì hàm XóaTrắng đã xóa sạch cột
                kryptonDataGridView1_DanhSachCBCS.AutoGenerateColumns = true;
                kryptonDataGridView1_DanhSachCBCS.DataSource = dt.DefaultView;

                DinhDangLuoi();
                CapNhatSoLuongKhenThuong();

                _currentEditingID = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải chi tiết: " + ex.Message);
            }
        }
        private void DinhDangLuoi()
        {
            var grid = kryptonDataGridView1_DanhSachCBCS;
            if (grid == null || grid.Columns.Count == 0) return;

            grid.SuspendLayout();
            try
            {
                // 1. ẨN CÁC CỘT KHÔNG CẦN THIẾT
                string[] cotCanAn = { "ID", "HoVaTen", "SoHieu", "DonVi", "TinhTrang" };
                foreach (var cot in cotCanAn)
                {
                    if (grid.Columns.Contains(cot)) grid.Columns[cot].Visible = false;
                }

                // 2. CẤU HÌNH CƠ BẢN
                grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
                grid.DefaultCellStyle.Font = new Font("Segoe UI", 11F, FontStyle.Regular);
                grid.DefaultCellStyle.Padding = new Padding(4, 6, 4, 6);
                grid.ReadOnly = true;
                grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                grid.AllowUserToAddRows = false;
                grid.RowHeadersVisible = true;
                grid.RowHeadersWidth = 55;
                grid.EnableHeadersVisualStyles = false;

                // ⭐ THAY ĐỔI QUAN TRỌNG NHẤT Ở ĐÂY:
                // Ép toàn bộ lưới tự động co giãn vừa khít 100% chiều ngang màn hình
                grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                // Chỉ giữ lại thanh cuộn dọc (bỏ qua thanh cuộn ngang vì đã vừa khít)
                grid.ScrollBars = ScrollBars.Vertical;

                // 3. TÙY CHỈNH CHIỀU CAO ĐỘNG (Dynamic Scaling)
                int fontHeight = grid.Font.Height;
                grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                grid.ColumnHeadersHeight = (int)(fontHeight * 2.5);
                grid.RowTemplate.Height = (int)(fontHeight * 1.8);

                // 4. CĂN GIỮA VÀ IN ĐẬM TIÊU ĐỀ
                var style = grid.ColumnHeadersDefaultCellStyle;
                style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                style.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
                style.WrapMode = DataGridViewTriState.True;

                // 5. CẤU HÌNH CHI TIẾT TỪNG CỘT (Dùng "TyLe" thay vì chiều rộng cứng)
                // Hệ số TyLe đóng vai trò như phần trăm chia đất cho từng cột
                var cauHinhCot = new[]
                {
            (Ten: "HinhThuc_Khen", TieuDe: "Hình thức khen", TyLe: 12f, CanLe: DataGridViewContentAlignment.MiddleLeft),
            (Ten: "QuyetDinh_Khen", TieuDe: "Quyết định số", TyLe: 10f, CanLe: DataGridViewContentAlignment.MiddleCenter),
            (Ten: "NgayCapQD_Khen", TieuDe: "Ngày cấp", TyLe: 9f, CanLe: DataGridViewContentAlignment.MiddleCenter),
            (Ten: "DonVi_Khen", TieuDe: "Đơn vị tặng", TyLe: 15f, CanLe: DataGridViewContentAlignment.MiddleLeft),
            
            // Cột Về Việc được cấp tỷ lệ cao nhất để ưu tiên chiếm nhiều diện tích
            (Ten: "VeViec_Khen", TieuDe: "Về việc", TyLe: 20f, CanLe: DataGridViewContentAlignment.MiddleLeft),

            (Ten: "TienThuong", TieuDe: "Tiền thưởng", TyLe: 10f, CanLe: DataGridViewContentAlignment.MiddleRight),
            (Ten: "NgayCapPhat", TieuDe: "Ngày cấp phát", TyLe: 9f, CanLe: DataGridViewContentAlignment.MiddleCenter),
            (Ten: "CanBoCapPhat", TieuDe: "Cán bộ cấp", TyLe: 11f, CanLe: DataGridViewContentAlignment.MiddleLeft),
            (Ten: "GhiChu_Khen", TieuDe: "Ghi chú", TyLe: 14f, CanLe: DataGridViewContentAlignment.MiddleLeft)
        };

                foreach (var cot in cauHinhCot)
                {
                    if (grid.Columns.Contains(cot.Ten))
                    {
                        var col = grid.Columns[cot.Ten];
                        col.HeaderText = cot.TieuDe;
                        col.DefaultCellStyle.Alignment = cot.CanLe;

                        // Gán tỷ lệ phần trăm giãn cột
                        col.FillWeight = cot.TyLe;

                        // Thêm format hiển thị cho cột tiền thưởng trên Grid
                        if (cot.Ten == "TienThuong")
                        {
                            col.DefaultCellStyle.Format = "N0";
                        }
                    }
                }

                // ⭐ 6. BỘ GIAO DIỆN HIỆN ĐẠI (OCEAN BLUE THEME)

                // Nền lưới trắng tinh khôi
                grid.StateCommon.Background.Color1 = Color.White;
                grid.StateCommon.DataCell.Back.Color1 = Color.White;

                // Viền lưới thanh mảnh, xám nhạt
                grid.StateCommon.DataCell.Border.Color1 = Color.FromArgb(235, 235, 235);
                grid.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
                grid.StateCommon.DataCell.Border.Width = 1;

                // Tiêu đề cột (Header) - Màu Xanh Nước Biển (Đậm và rõ nét hơn)
                grid.StateCommon.HeaderColumn.Back.Color1 = Color.FromArgb(180, 210, 240);
                grid.StateCommon.HeaderColumn.Back.Color2 = Color.FromArgb(180, 210, 240);
                grid.StateCommon.HeaderColumn.Content.Color1 = Color.FromArgb(30, 30, 30);
                grid.StateCommon.HeaderColumn.Border.Color1 = Color.FromArgb(150, 180, 210);

                // Nền khi chọn: Xanh dương siêu nhạt (Alice Blue) - Rất dịu mắt
                grid.StateSelected.DataCell.Back.Color1 = Color.FromArgb(232, 244, 253);
                grid.StateSelected.DataCell.Back.Color2 = Color.FromArgb(232, 244, 253);
                grid.StateSelected.DataCell.Content.Color1 = Color.FromArgb(0, 102, 204);

                // Cập nhật lại chiều cao cho các dòng ĐÃ CÓ SẴN (nếu có)
                foreach (DataGridViewRow row in grid.Rows)
                {
                    row.Height = grid.RowTemplate.Height;
                }
            }
            finally
            {
                grid.ResumeLayout();
            }
        }
        private void XoaTrangGiaoDien()
        {
            try
            {
                // 1. Xóa dấu vết ID đang thao tác
                _currentEditingID = -1;

                // 2. Xóa các ComboBox (Chỉ gán index = -1 là đủ)
                if (combobox_HinhThucKhen != null) combobox_HinhThucKhen.SelectedIndex = -1;
                if (comboBox_DonViKhenThuong != null) comboBox_DonViKhenThuong.SelectedIndex = -1;

                // 3. Xóa TextBox bằng toán tử hiện đại (Null-conditional operator `?.`)
                kryptonTextBox_QuyetDinh?.Clear();
                kryptonTextBox_NgayQuyDinh?.Clear();
                richTextBox1_VeViec?.Clear();
                richTextBox1_GhiChu?.Clear();

                // XÓA 03 TRƯỜNG MỚI THÊM
                if (kryptonTextBox_TienThuong != null)
                {
                    kryptonTextBox_TienThuong.TextChanged -= kryptonTextBox_TienThuong_TextChanged;
                    kryptonTextBox_TienThuong.Text = "0";
                    kryptonTextBox_TienThuong.TextChanged += kryptonTextBox_TienThuong_TextChanged;
                }
                kryptonTextBox_NgayCapPhat?.Clear();
                kryptonTextBox_CanBoCapPhat?.Clear();

                // 4. Ngắt kết nối dữ liệu lưới an toàn
                if (kryptonDataGridView1_DanhSachCBCS != null)
                {
                    kryptonDataGridView1_DanhSachCBCS.DataSource = null;
                    kryptonDataGridView1_DanhSachCBCS.Columns.Clear();
                }

                // 5. Reset nhãn đếm
                if (toolStripStatusLabel1 != null)
                {
                    toolStripStatusLabel1.Text = "Số lượng khen thưởng: 0";
                    toolStripStatusLabel1.Visible = false; // Ẩn đi khi xóa trắng giao diện
                }

                // 6. TRẢI NGHIỆM NGƯỜI DÙNG (UX): Hoàn trả lại tên nút về trạng thái "Sửa"
                if (kryptonButton_Sua != null)
                {
                    kryptonButton_Sua.Values.Text = "Sửa"; // Chuẩn của KryptonButton
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi XoaTrangGiaoDien]: {ex.Message}");
            }
        }
        public void CapNhatSoLuongKhenThuong()
        {
            if (toolStripStatusLabel1 != null)
            {
                if (kryptonDataGridView1_DanhSachCBCS != null)
                {
                    int soLuong = kryptonDataGridView1_DanhSachCBCS.Rows.Count;
                    toolStripStatusLabel1.Text = $"Số lượng khen thưởng: {soLuong}";

                    // Logic ẩn/hiện nhãn theo số lượng
                    if (soLuong == 0)
                    {
                        toolStripStatusLabel1.Visible = false; // Ẩn luôn khi bằng 0
                    }
                    else
                    {
                        toolStripStatusLabel1.Visible = true;  // Lớn hơn 0 thì mở lại
                    }
                }
                else
                {
                    // Trường hợp GridView bị null hoặc chưa nạp, mặc định ẩn để an toàn giao diện
                    toolStripStatusLabel1.Text = "Số lượng khen thưởng: 0";
                    toolStripStatusLabel1.Visible = false;
                }
            }
        }
        private void KryptonDataGridView1_DanhSachCBCS_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            var grid = sender as DataGridView;
            if (grid == null) return;
            string rowIdx = (e.RowIndex + 1).ToString();
            var centerFormat = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);
            e.Graphics.DrawString(rowIdx, new Font("Segoe UI", 12F, FontStyle.Bold), SystemBrushes.ControlText, headerBounds, centerFormat);
        }
        private void KryptonDataGridView1_DanhSachCBCS_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // 1. GUARD CLAUSE: Chặn click vào tiêu đề cột (RowIndex = -1) hoặc vùng viền lỗi
            if (e.RowIndex < 0 || sender is not DataGridView grid) return;

            try
            {
                var row = grid.Rows[e.RowIndex];

                // 2. ÉP KIỂU AN TOÀN: Tránh crash nếu cột ID bị NULL hoặc chứa rác
                if (int.TryParse(row.Cells["ID"].Value?.ToString(), out int parsedId))
                {
                    _currentEditingID = parsedId;
                }
                else
                {
                    _currentEditingID = -1; // Fallback an toàn, chặn lưu đè bậy bạ
                }

                // 3. ĐỔ DỮ LIỆU AN TOÀN: Dùng toán tử `?? ""` để bắt chết các giá trị DBNull
                if (combobox_HinhThucKhen != null) combobox_HinhThucKhen.Text = row.Cells["HinhThuc_Khen"].Value?.ToString() ?? "";
                if (kryptonTextBox_QuyetDinh != null) kryptonTextBox_QuyetDinh.Text = row.Cells["QuyetDinh_Khen"].Value?.ToString() ?? "";
                if (kryptonTextBox_NgayQuyDinh != null) kryptonTextBox_NgayQuyDinh.Text = row.Cells["NgayCapQD_Khen"].Value?.ToString() ?? "";
                if (comboBox_DonViKhenThuong != null) comboBox_DonViKhenThuong.Text = row.Cells["DonVi_Khen"].Value?.ToString() ?? "";
                if (richTextBox1_VeViec != null) richTextBox1_VeViec.Text = row.Cells["VeViec_Khen"].Value?.ToString() ?? "";
                if (richTextBox1_GhiChu != null) richTextBox1_GhiChu.Text = row.Cells["GhiChu_Khen"].Value?.ToString() ?? "";

                // GÁN 03 TRƯỜNG MỚI (CHÚ Ý CỘT TIỀN THƯỞNG SẼ TỰ KÍCH HOẠT EVENT FORMAT)
                if (kryptonTextBox_TienThuong != null) kryptonTextBox_TienThuong.Text = row.Cells["TienThuong"].Value?.ToString() ?? "0";
                if (kryptonTextBox_NgayCapPhat != null) kryptonTextBox_NgayCapPhat.Text = row.Cells["NgayCapPhat"].Value?.ToString() ?? "";
                if (kryptonTextBox_CanBoCapPhat != null) kryptonTextBox_CanBoCapPhat.Text = row.Cells["CanBoCapPhat"].Value?.ToString() ?? "";

                // 4. TRẢI NGHIỆM NGƯỜI DÙNG (UX): Đổi tên nút thành "Lưu"
                if (kryptonButton_Sua != null)
                {
                    // Với thư viện Krypton, việc đổi chữ thường nằm ở thuộc tính Values.Text
                    kryptonButton_Sua.Values.Text = "Lưu";
                }
            }
            catch (Exception ex)
            {
                // 5. GHI LOG LỖI MẦM: Không để phần mềm văng hộp thoại lỗi ra mặt người dùng khi click quá nhanh
                System.Diagnostics.Debug.WriteLine($"[Lỗi CellClick Grid Khen Thưởng]: {ex.Message}");
            }
        }
        private void kryptonButton_Them_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentSoHieu)) return;
            if (!KiemTraDuLieuDauVao()) return;
            try
            {
                // XỬ LÝ LỌC TIỀN THƯỞNG: Lấy số nguyên chất không chứa dấu . hay ,
                string tienThuongStr = kryptonTextBox_TienThuong?.Text.Replace(".", "").Replace(",", "").Trim() ?? "0";
                if (string.IsNullOrWhiteSpace(tienThuongStr)) tienThuongStr = "0";

                using var conn = new SqliteConnection($"Data Source={_csdl4Path}");
                conn.Open();
                using var tran = conn.BeginTransaction();
                try
                {
                    // THÊM 3 TRƯỜNG MỚI VÀO CÂU LỆNH SQL
                    string sqlInsert = @"INSERT INTO ThongKe_GiayKhen 
                                         (HoVaTen, SoHieu, DonVi, TinhTrang, HinhThuc_Khen, QuyetDinh_Khen, NgayCapQD_Khen, DonVi_Khen, VeViec_Khen, TienThuong, NgayCapPhat, CanBoCapPhat, GhiChu_Khen) 
                                         VALUES 
                                         (@HT, @SH, @DV, @TT, @HinhThuc, @SoQD, @Ngay, @DVKhen, @VeViec, @TienThuong, @NgayCapPhat, @CanBoCapPhat, @GhiChu)";

                    using (var cmd = new SqliteCommand(sqlInsert, conn, tran))
                    {
                        // 🔥 CHUẨN KỸ SƯ: Mã hóa toàn bộ các thông tin chi tiết trước khi cất xuống CSDL
                        cmd.Parameters.AddWithValue("@HT", BaoMatAES.MaHoa(_currentHoTen));
                        cmd.Parameters.AddWithValue("@SH", BaoMatAES.MaHoa(_currentSoHieu));
                        cmd.Parameters.AddWithValue("@DV", BaoMatAES.MaHoa(_currentDonVi));
                        cmd.Parameters.AddWithValue("@TT", _currentTinhTrang); // Tình trạng thường là Plaintext

                        cmd.Parameters.AddWithValue("@HinhThuc", BaoMatAES.MaHoa(combobox_HinhThucKhen.Text));
                        cmd.Parameters.AddWithValue("@SoQD", BaoMatAES.MaHoa(kryptonTextBox_QuyetDinh.Text));
                        cmd.Parameters.AddWithValue("@Ngay", BaoMatAES.MaHoa(kryptonTextBox_NgayQuyDinh.Text));
                        cmd.Parameters.AddWithValue("@DVKhen", BaoMatAES.MaHoa(comboBox_DonViKhenThuong.Text));
                        cmd.Parameters.AddWithValue("@VeViec", BaoMatAES.MaHoa(richTextBox1_VeViec.Text));

                        // MÃ HÓA 03 TRƯỜNG MỚI
                        cmd.Parameters.AddWithValue("@TienThuong", BaoMatAES.MaHoa(tienThuongStr));
                        cmd.Parameters.AddWithValue("@NgayCapPhat", BaoMatAES.MaHoa(kryptonTextBox_NgayCapPhat?.Text ?? ""));
                        cmd.Parameters.AddWithValue("@CanBoCapPhat", BaoMatAES.MaHoa(kryptonTextBox_CanBoCapPhat?.Text ?? ""));

                        cmd.Parameters.AddWithValue("@GhiChu", BaoMatAES.MaHoa(richTextBox1_GhiChu.Text));

                        cmd.ExecuteNonQuery();
                    }
                    CapNhatSoLuongB2(conn, tran, _currentSoHieu, 1);
                    tran.Commit();

                    try
                    {
                        string hoTenLog = string.IsNullOrWhiteSpace(_currentHoTen) ? "Chưa rõ tên" : _currentHoTen;
                        string soHieuLog = string.IsNullOrWhiteSpace(_currentSoHieu) ? "Chưa rõ SH" : _currentSoHieu;
                        Module_NhatKy.GhiNhatKy(
                            taiKhoan: string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM) ? "Không xác định" : Module_TaiKhoan.TenTaiKhoan_RAM,
                            hanhDong: $"Thêm mới dữ liệu khen thưởng: {hoTenLog} (SH: {soHieuLog})",
                            ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                        );
                    }
                    catch (Exception logEx) { System.Diagnostics.Debug.WriteLine("Lỗi ghi nhật ký: " + logEx.Message); }

                    XoaTrangGiaoDien();
                    ReloadDuLieuGiayKhen_CuaMotNguoi();
                }
                catch { tran.Rollback(); throw; }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi thêm giấy khen: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void kryptonButton_Sua_Click(object sender, EventArgs e)
        {
            if (_currentEditingID == -1)
            {
                MessageBox.Show("Vui lòng chọn một giấy khen từ danh sách bên dưới để Sửa!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!KiemTraDuLieuDauVao()) return;

            try
            {
                // XỬ LÝ LỌC TIỀN THƯỞNG SẠCH
                string tienThuongStr = kryptonTextBox_TienThuong?.Text.Replace(".", "").Replace(",", "").Trim() ?? "0";
                if (string.IsNullOrWhiteSpace(tienThuongStr)) tienThuongStr = "0";

                using var conn = new SqliteConnection($"Data Source={_csdl4Path}");
                conn.Open();

                // CẬP NHẬT 3 TRƯỜNG MỚI VÀO CÂU LỆNH SQL
                string sqlUpdate = @"UPDATE ThongKe_GiayKhen 
                                     SET HinhThuc_Khen = @HinhThuc, QuyetDinh_Khen = @SoQD, NgayCapQD_Khen = @Ngay, 
                                         DonVi_Khen = @DVKhen, VeViec_Khen = @VeViec, 
                                         TienThuong = @TienThuong, NgayCapPhat = @NgayCapPhat, CanBoCapPhat = @CanBoCapPhat, 
                                         GhiChu_Khen = @GhiChu 
                                     WHERE ID = @ID";

                using (var cmd = new SqliteCommand(sqlUpdate, conn))
                {
                    // 🔥 CHUẨN KỸ SƯ: Bọc mã hóa đồng nhất với lúc INSERT
                    cmd.Parameters.AddWithValue("@HinhThuc", BaoMatAES.MaHoa(combobox_HinhThucKhen.Text));
                    cmd.Parameters.AddWithValue("@SoQD", BaoMatAES.MaHoa(kryptonTextBox_QuyetDinh.Text));
                    cmd.Parameters.AddWithValue("@Ngay", BaoMatAES.MaHoa(kryptonTextBox_NgayQuyDinh.Text));
                    cmd.Parameters.AddWithValue("@DVKhen", BaoMatAES.MaHoa(comboBox_DonViKhenThuong.Text));
                    cmd.Parameters.AddWithValue("@VeViec", BaoMatAES.MaHoa(richTextBox1_VeViec.Text));

                    // TRƯỜNG MỚI MÃ HÓA
                    cmd.Parameters.AddWithValue("@TienThuong", BaoMatAES.MaHoa(tienThuongStr));
                    cmd.Parameters.AddWithValue("@NgayCapPhat", BaoMatAES.MaHoa(kryptonTextBox_NgayCapPhat?.Text ?? ""));
                    cmd.Parameters.AddWithValue("@CanBoCapPhat", BaoMatAES.MaHoa(kryptonTextBox_CanBoCapPhat?.Text ?? ""));

                    cmd.Parameters.AddWithValue("@GhiChu", BaoMatAES.MaHoa(richTextBox1_GhiChu.Text));
                    cmd.Parameters.AddWithValue("@ID", _currentEditingID);
                    cmd.ExecuteNonQuery();
                }

                string hoTen = label1_HoVaTen?.Text ?? "Chưa rõ tên";
                string soHieu = _currentSoHieu ?? "Chưa rõ SH";

                try
                {
                    Module_NhatKy.GhiNhatKy(
                        taiKhoan: string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM) ? "Không xác định" : Module_TaiKhoan.TenTaiKhoan_RAM,
                        hanhDong: $"Sửa dữ liệu khen thưởng: {hoTen} (SH: {soHieu})",
                        ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                    );
                }
                catch (Exception logEx) { System.Diagnostics.Debug.WriteLine("Lỗi ghi nhật ký: " + logEx.Message); }

                XoaTrangGiaoDien();
                ReloadDuLieuGiayKhen_CuaMotNguoi();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi cập nhật giấy khen: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void kryptonButton_Xoa_Click(object sender, EventArgs e)
        {
            if (_currentEditingID == -1)
            {
                MessageBox.Show("Vui lòng chọn một giấy khen từ danh sách bên dưới để Xóa!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Bạn có chắc chắn muốn xóa giấy khen này không? Thao tác này không thể hoàn tác.", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                return;
            }

            try
            {
                string hoTen = label1_HoVaTen?.Text ?? "Chưa rõ tên";
                string soHieu = _currentSoHieu ?? "Chưa rõ SH";

                using var conn = new SqliteConnection($"Data Source={_csdl4Path}");
                conn.Open();
                using var tran = conn.BeginTransaction();
                try
                {
                    string sqlDelete = "DELETE FROM ThongKe_GiayKhen WHERE ID = @ID";
                    using (var cmd = new SqliteCommand(sqlDelete, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@ID", _currentEditingID);
                        cmd.ExecuteNonQuery();
                    }

                    CapNhatSoLuongB2(conn, tran, _currentSoHieu, -1);
                    tran.Commit();

                    try
                    {
                        Module_NhatKy.GhiNhatKy(
                            taiKhoan: string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM) ? "Không xác định" : Module_TaiKhoan.TenTaiKhoan_RAM,
                            hanhDong: $"Xóa dữ liệu khen thưởng: {hoTen} (SH: {soHieu})",
                            ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                        );
                    }
                    catch (Exception logEx) { System.Diagnostics.Debug.WriteLine("Lỗi ghi nhật ký: " + logEx.Message); }

                    XoaTrangGiaoDien();
                    ReloadDuLieuGiayKhen_CuaMotNguoi();
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xóa giấy khen: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void CapNhatSoLuongB2(SqliteConnection conn, SqliteTransaction tran, string soHieu, int valueChange)
        {
            string sqlSelect = "SELECT rowid, SoHieu, SoLuong_Khen FROM ThongKeCBCS_DuocKhenThuong";
            long targetRowId = -1;
            int slHienTai = 0;

            using (var cmdSelect = new SqliteCommand(sqlSelect, conn, tran))
            using (var reader = cmdSelect.ExecuteReader())
            {
                while (reader.Read())
                {
                    string shMaHoa = reader.IsDBNull(1) ? "" : reader.GetString(1);

                    if (string.Equals(SafeDecrypt(shMaHoa), soHieu, StringComparison.OrdinalIgnoreCase))
                    {
                        targetRowId = reader.GetInt64(0);

                        string slStr = reader.IsDBNull(2) ? "0" : reader.GetString(2);
                        int.TryParse(slStr, out slHienTai);

                        break;
                    }
                }
            }

            int slMoi = slHienTai + valueChange;
            if (slMoi < 0) slMoi = 0;

            if (targetRowId != -1)
            {
                string sqlUp = "UPDATE ThongKeCBCS_DuocKhenThuong SET SoLuong_Khen = @SL WHERE rowid = @RowId";
                using var cmdUp = new SqliteCommand(sqlUp, conn, tran);
                cmdUp.Parameters.AddWithValue("@SL", slMoi.ToString());
                cmdUp.Parameters.AddWithValue("@RowId", targetRowId);
                cmdUp.ExecuteNonQuery();
            }
            else if (valueChange > 0)
            {
                string sqlIn = "INSERT INTO ThongKeCBCS_DuocKhenThuong (HoVaTen, SoHieu, DonVi, TinhTrang, SoLuong_Khen) VALUES (@HT, @SH, @DV, @TT, '1')";
                using var cmdIn = new SqliteCommand(sqlIn, conn, tran);
                cmdIn.Parameters.AddWithValue("@HT", BaoMatAES.MaHoa(_currentHoTen));
                cmdIn.Parameters.AddWithValue("@SH", BaoMatAES.MaHoa(_currentSoHieu));
                cmdIn.Parameters.AddWithValue("@DV", BaoMatAES.MaHoa(_currentDonVi));
                cmdIn.Parameters.AddWithValue("@TT", _currentTinhTrang);
                cmdIn.ExecuteNonQuery();
            }
        }
        private void kryptonButton3_DongForm_Click(object sender, EventArgs e)
        {
            DongBoSoLuongVeBangTong(_currentSoHieu);
            XoaTrangGiaoDien();
            this.Hide();

            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            if (formCha == null) return;

            var panel = formCha.Controls.Find("PanelContainer", true).FirstOrDefault() as Panel;
            if (panel == null) return;

            var form34 = panel.Controls.OfType<Form34_ThongKeKhenThuong>().FirstOrDefault();
            if (form34 != null && !form34.IsDisposed)
            {
                form34.Show();
                form34.BringToFront();
                form34.ReloadDuLieu();
            }
        }
        private bool KiemTraDuLieuDauVao()
        {
            if (string.IsNullOrWhiteSpace(_currentSoHieu))
            {
                MessageBox.Show("Lỗi hệ thống: Không xác định được Cán bộ chiến sĩ đang được thao tác!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(combobox_HinhThucKhen.Text))
            {
                MessageBox.Show("Vui lòng chọn hoặc nhập 'Hình thức khen thưởng'!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                combobox_HinhThucKhen.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(kryptonTextBox_QuyetDinh.Text))
            {
                MessageBox.Show("Vui lòng nhập 'Số Quyết định'!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                kryptonTextBox_QuyetDinh.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(comboBox_DonViKhenThuong.Text))
            {
                MessageBox.Show("Vui lòng chọn hoặc nhập 'Đơn vị khen thưởng'!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                comboBox_DonViKhenThuong.Focus();
                return false;
            }

            return true;
        }
        private void DongBoSoLuongVeBangTong(string soHieu)
        {
            if (string.IsNullOrWhiteSpace(soHieu)) return;

            try
            {
                using var conn = new SqliteConnection($"Data Source={_csdl4Path}");
                conn.Open();

                int soLuongThucTe = 0;
                using (var cmdCount = new SqliteCommand("SELECT SoHieu FROM ThongKe_GiayKhen", conn))
                using (var reader = cmdCount.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string shGiaiMa = reader.IsDBNull(0) ? "" : SafeDecrypt(reader.GetString(0));
                        if (string.Equals(shGiaiMa, soHieu, StringComparison.OrdinalIgnoreCase))
                        {
                            soLuongThucTe++;
                        }
                    }
                }

                long targetRowId = -1;
                using (var cmdSelect = new SqliteCommand("SELECT rowid, SoHieu FROM ThongKeCBCS_DuocKhenThuong", conn))
                using (var reader = cmdSelect.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string shGiaiMa2 = reader.IsDBNull(1) ? "" : SafeDecrypt(reader.GetString(1));
                        if (string.Equals(shGiaiMa2, soHieu, StringComparison.OrdinalIgnoreCase))
                        {
                            targetRowId = reader.GetInt64(0);
                            break;
                        }
                    }
                }

                if (targetRowId != -1)
                {
                    string sqlUpdate = "UPDATE ThongKeCBCS_DuocKhenThuong SET SoLuong_Khen = @SL WHERE rowid = @RowId";
                    using (var cmdUpdate = new SqliteCommand(sqlUpdate, conn))
                    {
                        cmdUpdate.Parameters.AddWithValue("@SL", soLuongThucTe.ToString());
                        cmdUpdate.Parameters.AddWithValue("@RowId", targetRowId);
                        cmdUpdate.ExecuteNonQuery();
                    }
                    System.Diagnostics.Debug.WriteLine($"Đã đồng bộ: {soHieu} có {soLuongThucTe} giấy khen.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi đồng bộ: " + ex.Message);
            }
        }
    }
}