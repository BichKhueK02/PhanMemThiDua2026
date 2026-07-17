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
        }
        private void Form37_ChiTietKhenThuong_Load(object sender, EventArgs e)
        {
        }
        // ======================================================================
        // HÀM BẢO VỆ GIAO DIỆN (LỘT VỎ MÃ HÓA AN TOÀN)
        // ======================================================================
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
            if (label1_HoVaTen != null) label1_HoVaTen.Text = hoTen;
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
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dt.Columns.Add(reader.GetName(i));
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
                            // 🔥 CHUẨN KỸ SƯ: Lột vỏ toàn bộ dữ liệu trước khi nạp vào DataTable
                            // Việc này đảm bảo Grid luôn cầm Data sạch, click vào đổ ra TextBox không bị dính mã V2
                            row[i] = SafeDecrypt(reader[i]);
                        }
                        dt.Rows.Add(row);
                    }
                }

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
            string[] cotCanAn = { "ID", "HoVaTen", "SoHieu", "DonVi", "TinhTrang" };
            foreach (var cot in cotCanAn)
            {
                if (grid.Columns.Contains(cot)) grid.Columns[cot].Visible = false;
            }
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 11F, FontStyle.Regular);
            grid.DefaultCellStyle.Padding = new Padding(4, 6, 4, 6);
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            var cauHinhCot = new[]
            {
                (Ten: "HinhThuc_Khen", TieuDe: "Hình thức khen", Rong: 220, CanLe: DataGridViewContentAlignment.MiddleLeft, Fill: DataGridViewAutoSizeColumnMode.NotSet),
                (Ten: "QuyetDinh_Khen", TieuDe: "Quyết định số", Rong: 220, CanLe: DataGridViewContentAlignment.MiddleCenter, Fill: DataGridViewAutoSizeColumnMode.NotSet),
                (Ten: "NgayCapQD_Khen", TieuDe: "Ngày cấp", Rong: 170, CanLe: DataGridViewContentAlignment.MiddleCenter, Fill: DataGridViewAutoSizeColumnMode.NotSet),
                (Ten: "DonVi_Khen", TieuDe: "Đơn vị tặng", Rong: 240, CanLe: DataGridViewContentAlignment.MiddleLeft, Fill: DataGridViewAutoSizeColumnMode.NotSet),
                (Ten: "VeViec_Khen", TieuDe: "Về việc", Rong: 0, CanLe: DataGridViewContentAlignment.MiddleLeft, Fill: DataGridViewAutoSizeColumnMode.Fill),
                (Ten: "GhiChu_Khen", TieuDe: "Ghi chú", Rong: 250, CanLe: DataGridViewContentAlignment.MiddleLeft, Fill: DataGridViewAutoSizeColumnMode.NotSet)
            };

            foreach (var cot in cauHinhCot)
            {
                if (grid.Columns.Contains(cot.Ten))
                {
                    var col = grid.Columns[cot.Ten];
                    col.HeaderText = cot.TieuDe;
                    col.DefaultCellStyle.Alignment = cot.CanLe;
                    col.AutoSizeMode = cot.Fill;
                    if (cot.Rong > 0) col.Width = cot.Rong;
                }
            }
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AllowUserToAddRows = false;
            grid.RowHeadersVisible = true;
            grid.RowHeadersWidth = 55;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(220, 230, 240);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 60;
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

                // 4. Ngắt kết nối dữ liệu lưới an toàn
                if (kryptonDataGridView1_DanhSachCBCS != null)
                {
                    kryptonDataGridView1_DanhSachCBCS.DataSource = null;
                    // Xóa sạch Row rác còn đọng lại nếu grid không ràng buộc dữ liệu trực tiếp
                    kryptonDataGridView1_DanhSachCBCS.Rows.Clear();
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
                                                           // kryptonButton_Sua.Text = "Sửa";
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

                // 4. TRẢI NGHIỆM NGƯỜI DÙNG (UX): Đổi tên nút thành "Lưu"
                if (kryptonButton_Sua != null)
                {
                    // Với thư viện Krypton, việc đổi chữ thường nằm ở thuộc tính Values.Text
                    kryptonButton_Sua.Values.Text = "Lưu";

                    // Nếu đồng chí cấu hình đặc biệt, có thể mở khóa thêm dòng dưới:
                    // kryptonButton_Sua.Text = "Lưu";
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
                using var conn = new SqliteConnection($"Data Source={_csdl4Path}");
                conn.Open();
                using var tran = conn.BeginTransaction();
                try
                {
                    string sqlInsert = @"INSERT INTO ThongKe_GiayKhen (HoVaTen, SoHieu, DonVi, TinhTrang, HinhThuc_Khen, QuyetDinh_Khen, NgayCapQD_Khen, DonVi_Khen, VeViec_Khen, GhiChu_Khen) 
                                         VALUES (@HT, @SH, @DV, @TT, @HinhThuc, @SoQD, @Ngay, @DVKhen, @VeViec, @GhiChu)";
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
                using var conn = new SqliteConnection($"Data Source={_csdl4Path}");
                conn.Open();

                string sqlUpdate = @"UPDATE ThongKe_GiayKhen 
                                     SET HinhThuc_Khen = @HinhThuc, QuyetDinh_Khen = @SoQD, NgayCapQD_Khen = @Ngay, 
                                         DonVi_Khen = @DVKhen, VeViec_Khen = @VeViec, GhiChu_Khen = @GhiChu 
                                     WHERE ID = @ID";

                using (var cmd = new SqliteCommand(sqlUpdate, conn))
                {
                    // 🔥 CHUẨN KỸ SƯ: Bọc mã hóa đồng nhất với lúc INSERT
                    cmd.Parameters.AddWithValue("@HinhThuc", BaoMatAES.MaHoa(combobox_HinhThucKhen.Text));
                    cmd.Parameters.AddWithValue("@SoQD", BaoMatAES.MaHoa(kryptonTextBox_QuyetDinh.Text));
                    cmd.Parameters.AddWithValue("@Ngay", BaoMatAES.MaHoa(kryptonTextBox_NgayQuyDinh.Text));
                    cmd.Parameters.AddWithValue("@DVKhen", BaoMatAES.MaHoa(comboBox_DonViKhenThuong.Text));
                    cmd.Parameters.AddWithValue("@VeViec", BaoMatAES.MaHoa(richTextBox1_VeViec.Text));
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
            // Cột 0: rowid | Cột 1: SoHieu | Cột 2: SoLuong_Khen
            string sqlSelect = "SELECT rowid, SoHieu, SoLuong_Khen FROM ThongKeCBCS_DuocKhenThuong";
            long targetRowId = -1;
            int slHienTai = 0;

            using (var cmdSelect = new SqliteCommand(sqlSelect, conn, tran))
            using (var reader = cmdSelect.ExecuteReader())
            {
                while (reader.Read())
                {
                    // Chuẩn kỹ sư: Kiểm tra NULL trước khi đọc chuỗi ở cột 1 (SoHieu)
                    string shMaHoa = reader.IsDBNull(1) ? "" : reader.GetString(1);

                    if (string.Equals(SafeDecrypt(shMaHoa), soHieu, StringComparison.OrdinalIgnoreCase))
                    {
                        // ✅ Đọc rowid bằng GetInt64(0) triệt để lỗi ArgumentOutOfRangeException
                        targetRowId = reader.GetInt64(0);

                        // Chuẩn kỹ sư: Đọc số lượng bằng cột 2 an toàn
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

            var form36 = panel.Controls.OfType<Form34_ThongKeKhenThuong>().FirstOrDefault();
            if (form36 != null && !form36.IsDisposed)
            {
                form36.Show();
                form36.BringToFront();
                form36.ReloadDuLieu();
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
                // Cột 0: SoHieu
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
                // Cột 0: rowid | Cột 1: SoHieu
                using (var cmdSelect = new SqliteCommand("SELECT rowid, SoHieu FROM ThongKeCBCS_DuocKhenThuong", conn))
                using (var reader = cmdSelect.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string shGiaiMa2 = reader.IsDBNull(1) ? "" : SafeDecrypt(reader.GetString(1));
                        if (string.Equals(shGiaiMa2, soHieu, StringComparison.OrdinalIgnoreCase))
                        {
                            // ✅ Fix lỗi lấy rowid
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