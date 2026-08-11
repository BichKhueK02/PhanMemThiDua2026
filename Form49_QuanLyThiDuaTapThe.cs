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
    public partial class Form49_QuanLyThiDuaTapThe : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private readonly string _csdl4Path = Module_DanduongGPS.DuongDanCSDL4;

        // Biến lưu trạng thái
        private int _selectedID = -1;
        private bool isEditing = false;

        public Form49_QuanLyThiDuaTapThe()
        {
            InitializeComponent();
        }

        private async void Form49_QuanLyThiDuaTapThe_Load(object sender, EventArgs e)
        {
            // 1. Kiểm tra bảng
            KiemTraVaTaoBangThongKeKhenThuongTapThe();

            // 2. Cấu hình giao diện
            CauHinhGridCoBan(kryptonDataGridView1);
            CauHinhStyleWeb(kryptonDataGridView1);
            CauHinhCotGrid(kryptonDataGridView1);

            // Cấu hình ô STT: Không cho nhập, chỉ hiển thị, nền xanh lá nhạt
            kryptonTextBox_STT.ReadOnly = true;
            kryptonTextBox_STT.StateCommon.Back.Color1 = Color.LightGreen;

            // 3. Gán sự kiện
            kryptonDataGridView1.CellClick -= kryptonDataGridView1_CellClick;
            kryptonDataGridView1.CellClick += kryptonDataGridView1_CellClick;

            kryptonTextBox_TienThuong.TextChanged -= kryptonTextBox_TienThuong_TextChanged;
            kryptonTextBox_TienThuong.TextChanged += kryptonTextBox_TienThuong_TextChanged;

            // 4. Khởi tạo
            ResetInput();
            await LoadDataToGridAsync();
        }

        // =========================================================
        // CODE FORMAT TIỀN TỆ CỦA BẠN (Đã tối ưu)
        // =========================================================
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
                    System.Text.RegularExpressions.Regex digitsOnly = new System.Text.RegularExpressions.Regex(@"[^\d]");
                    string cleanStr = digitsOnly.Replace(rawText, "");

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

        public async void ReloadForm49()
        {
            await LoadDataToGridAsync();
        }

        public async Task LoadDataToGridAsync()
        {
            string connectionString = $@"Data Source={_csdl4Path};";
            try
            {
                DataTable dtThuTu = await Task.Run(async () =>
                {
                    DataTable dt = new DataTable();
                    dt.Columns.Add("ID", typeof(int));
                    dt.Columns.Add("STT", typeof(int));
                    dt.Columns.Add("TenTapThe", typeof(string));
                    dt.Columns.Add("TrangThai_DonVi", typeof(string));
                    dt.Columns.Add("HinhThuc_KhenThuong", typeof(string));
                    dt.Columns.Add("LoaiKhenThuong", typeof(string));
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

                    using (SqliteConnection conn = new SqliteConnection(connectionString))
                    {
                        await conn.OpenAsync();
                        // Ưu tiên sắp xếp theo STT
                        string query = @"SELECT 
                                        ID, STT, TenTapThe, TrangThai_DonVi, HinhThuc_KhenThuong, LoaiKhenThuong, 
                                        DonVi_CapKhenThuong, SoQuyetDinh, NgayQuyetDinh, NguoiKy, NoiDung_KhenThuong, 
                                        TienThuong, NgayCapPhat, CanBoCapPhat, NguoiDaiDienNhan, GhiChu 
                                     FROM ThongKe_KhenThuongTapThe ORDER BY CAST(STT AS INTEGER) ASC";

                        using (SqliteCommand cmd = new SqliteCommand(query, conn))
                        using (SqliteDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int idDB = reader["ID"] != DBNull.Value ? Convert.ToInt32(reader["ID"]) : 0;
                                int sttDB = reader["STT"] != DBNull.Value ? Convert.ToInt32(reader["STT"]) : 0;
                                long tien = 0;
                                if (reader["TienThuong"] != DBNull.Value && !string.IsNullOrWhiteSpace(reader["TienThuong"].ToString()))
                                    long.TryParse(reader["TienThuong"].ToString().Replace(".", "").Replace(",", ""), out tien);

                                dt.Rows.Add(
                                    idDB, sttDB,
                                    reader["TenTapThe"]?.ToString() ?? "",
                                    reader["TrangThai_DonVi"]?.ToString() ?? "",
                                    reader["HinhThuc_KhenThuong"]?.ToString() ?? "",
                                    reader["LoaiKhenThuong"]?.ToString() ?? "",
                                    reader["DonVi_CapKhenThuong"]?.ToString() ?? "",
                                    reader["SoQuyetDinh"]?.ToString() ?? "",
                                    reader["NgayQuyetDinh"]?.ToString() ?? "",
                                    reader["NguoiKy"]?.ToString() ?? "",
                                    reader["NoiDung_KhenThuong"]?.ToString() ?? "",
                                    tien,
                                    reader["NgayCapPhat"]?.ToString() ?? "",
                                    reader["CanBoCapPhat"]?.ToString() ?? "",
                                    reader["NguoiDaiDienNhan"]?.ToString() ?? "",
                                    reader["GhiChu"]?.ToString() ?? ""
                                );
                            }
                        }
                    }
                    return dt;
                });
                kryptonDataGridView1.DataSource = dtThuTu;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =========================================================
        // HÀM RESET GIAO DIỆN (TRẠNG THÁI CHỜ THÊM MỚI)
        // =========================================================
        private void ResetInput()
        {
            _selectedID = -1;
            isEditing = false;

            kryptonTextBox_STT.Text = "Tự động"; // Người dùng ko cần nhập
            kryptonTextBox_TenTapThe.Text = string.Empty;
            comboBox_TrangThaiDonVi.Text = string.Empty;
            comboBox_HinhThucKhenThuong.Text = string.Empty;
            comboBox_LoaiKhenThuong.Text = string.Empty;
            comboBox_DonViKhenThuong.Text = string.Empty;
            kryptonTextBox_SoQuyetDinh.Text = string.Empty;
            kryptonTextBox_NgayQuyetDinh.Text = string.Empty;
            kryptonTextBox_NguoiKy.Text = string.Empty;
            richTextBox1_NoiDungKhenThuong.Text = string.Empty;
            kryptonTextBox_TienThuong.Text = "0";
            kryptonTextBox_NgayCapPhat.Text = string.Empty;
            kryptonTextBox_CanBoCapPhat.Text = string.Empty;
            kryptonTextBox_NguoiDaiDienNhan.Text = string.Empty;
            kryptonTextBox_GhiChu.Text = string.Empty;

            // Nút Thêm ở trạng thái Sẵn sàng
            kryptonButton_ThemKhenThuong.Text = "Thêm";
            kryptonButton_ThemKhenThuong.StateCommon.Back.Color1 = Color.SeaGreen;

            // Nút Sửa, Xóa bị vô hiệu hóa
            kryptonButton_SuaVaLuuKhenThuong.Enabled = false;
            kryptonButton_SuaVaLuuKhenThuong.Text = "Sửa";

            kryptonButton_XoaKhenThuong.Enabled = false;
        }

        // =========================================================
        // CHỌN DỮ LIỆU TRÊN LƯỚI
        // =========================================================
        private void kryptonDataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= kryptonDataGridView1.Rows.Count) return;
            DataGridViewRow row = kryptonDataGridView1.Rows[e.RowIndex];
            if (row.Cells["ID"].Value == null || row.Cells["ID"].Value == DBNull.Value) return;

            _selectedID = Convert.ToInt32(row.Cells["ID"].Value);
            isEditing = true;

            kryptonTextBox_STT.Text = row.Cells["STT"].Value?.ToString() ?? "";
            kryptonTextBox_TenTapThe.Text = row.Cells["TenTapThe"].Value?.ToString() ?? "";
            comboBox_TrangThaiDonVi.Text = row.Cells["TrangThai_DonVi"].Value?.ToString() ?? "";
            comboBox_HinhThucKhenThuong.Text = row.Cells["HinhThuc_KhenThuong"].Value?.ToString() ?? "";
            comboBox_LoaiKhenThuong.Text = row.Cells["LoaiKhenThuong"].Value?.ToString() ?? "";
            comboBox_DonViKhenThuong.Text = row.Cells["DonVi_CapKhenThuong"].Value?.ToString() ?? "";
            kryptonTextBox_SoQuyetDinh.Text = row.Cells["SoQuyetDinh"].Value?.ToString() ?? "";
            kryptonTextBox_NgayQuyetDinh.Text = row.Cells["NgayQuyetDinh"].Value?.ToString() ?? "";
            kryptonTextBox_NguoiKy.Text = row.Cells["NguoiKy"].Value?.ToString() ?? "";
            richTextBox1_NoiDungKhenThuong.Text = row.Cells["NoiDung_KhenThuong"].Value?.ToString() ?? "";
            kryptonTextBox_TienThuong.Text = row.Cells["TienThuong"].Value?.ToString() ?? "0";
            kryptonTextBox_NgayCapPhat.Text = row.Cells["NgayCapPhat"].Value?.ToString() ?? "";
            kryptonTextBox_CanBoCapPhat.Text = row.Cells["CanBoCapPhat"].Value?.ToString() ?? "";
            kryptonTextBox_NguoiDaiDienNhan.Text = row.Cells["NguoiDaiDienNhan"].Value?.ToString() ?? "";
            kryptonTextBox_GhiChu.Text = row.Cells["GhiChu"].Value?.ToString() ?? "";

            // Nút Thêm biến thành nút "Làm mới Form" để người dùng hủy lựa chọn
            kryptonButton_ThemKhenThuong.Text = "Làm sạch";
            kryptonButton_ThemKhenThuong.StateCommon.Back.Color1 = Color.White;

            // Bật nút Sửa và Xóa
            kryptonButton_SuaVaLuuKhenThuong.Enabled = true;
            kryptonButton_XoaKhenThuong.Enabled = true;
        }

        private object GetSafeValue(string input)
        {
            return string.IsNullOrWhiteSpace(input) ? DBNull.Value : (object)input.Trim();
        }

        // =========================================================
        // CHỨC NĂNG THÊM (1 click)
        // =========================================================
        private async void kryptonButton_ThemKhenThuong_Click(object sender, EventArgs e)
        {
            if (isEditing)
            {
                // Nếu đang ở chế độ chọn dòng, bấm nút này để làm sạch form
                ResetInput();
                kryptonTextBox_TenTapThe.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(kryptonTextBox_TenTapThe.Text))
            {
                MessageBox.Show("Vui lòng nhập 'Tên tập thể' (bắt buộc)!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                kryptonTextBox_TenTapThe.Focus();
                return;
            }

            try
            {
                string tienThuongStr = kryptonTextBox_TienThuong.Text.Replace(".", "").Replace(",", "").Trim();
                if (string.IsNullOrWhiteSpace(tienThuongStr)) tienThuongStr = "0";

                string connectionString = $@"Data Source={_csdl4Path};";
                using (var conn = new SqliteConnection(connectionString))
                {
                    await conn.OpenAsync();

                    // Thuật toán đếm ID thủ công
                    int newId = 1;
                    using (var cmdMaxId = new SqliteCommand("SELECT MAX(CAST(ID AS INTEGER)) FROM ThongKe_KhenThuongTapThe", conn))
                    {
                        var maxIdObj = await cmdMaxId.ExecuteScalarAsync();
                        if (maxIdObj != null && maxIdObj != DBNull.Value)
                            newId = Convert.ToInt32(maxIdObj) + 1;
                    }

                    // Tự động đếm STT lớn nhất hiện có và + 1
                    int newStt = 1;
                    using (var cmdMaxStt = new SqliteCommand("SELECT MAX(CAST(STT AS INTEGER)) FROM ThongKe_KhenThuongTapThe", conn))
                    {
                        var maxSttObj = await cmdMaxStt.ExecuteScalarAsync();
                        if (maxSttObj != null && maxSttObj != DBNull.Value)
                            newStt = Convert.ToInt32(maxSttObj) + 1;
                    }

                    string query = @"INSERT INTO ThongKe_KhenThuongTapThe 
                                     (ID, STT, TenTapThe, TrangThai_DonVi, HinhThuc_KhenThuong, LoaiKhenThuong, DonVi_CapKhenThuong, 
                                      SoQuyetDinh, NgayQuyetDinh, NguoiKy, NoiDung_KhenThuong, 
                                      TienThuong, NgayCapPhat, CanBoCapPhat, NguoiDaiDienNhan, GhiChu) 
                                      VALUES 
                                     (@ID, @STT, @TenTapThe, @TrangThai_DonVi, @HinhThuc_KhenThuong, @LoaiKhenThuong, @DonVi_CapKhenThuong, 
                                      @SoQuyetDinh, @NgayQuyetDinh, @NguoiKy, @NoiDung_KhenThuong, 
                                      @TienThuong, @NgayCapPhat, @CanBoCapPhat, @NguoiDaiDienNhan, @GhiChu)";

                    using (var cmd = new SqliteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", newId);
                        cmd.Parameters.AddWithValue("@STT", newStt); // Lưu số thứ tự tự động
                        cmd.Parameters.AddWithValue("@TenTapThe", kryptonTextBox_TenTapThe.Text.Trim());
                        cmd.Parameters.AddWithValue("@TrangThai_DonVi", GetSafeValue(comboBox_TrangThaiDonVi.Text));
                        cmd.Parameters.AddWithValue("@HinhThuc_KhenThuong", GetSafeValue(comboBox_HinhThucKhenThuong.Text));
                        cmd.Parameters.AddWithValue("@LoaiKhenThuong", GetSafeValue(comboBox_LoaiKhenThuong.Text));
                        cmd.Parameters.AddWithValue("@DonVi_CapKhenThuong", GetSafeValue(comboBox_DonViKhenThuong.Text));
                        cmd.Parameters.AddWithValue("@SoQuyetDinh", GetSafeValue(kryptonTextBox_SoQuyetDinh.Text));
                        cmd.Parameters.AddWithValue("@NgayQuyetDinh", GetSafeValue(kryptonTextBox_NgayQuyetDinh.Text));
                        cmd.Parameters.AddWithValue("@NguoiKy", GetSafeValue(kryptonTextBox_NguoiKy.Text));
                        cmd.Parameters.AddWithValue("@NoiDung_KhenThuong", GetSafeValue(richTextBox1_NoiDungKhenThuong.Text));
                        cmd.Parameters.AddWithValue("@TienThuong", tienThuongStr);
                        cmd.Parameters.AddWithValue("@NgayCapPhat", GetSafeValue(kryptonTextBox_NgayCapPhat.Text));
                        cmd.Parameters.AddWithValue("@CanBoCapPhat", GetSafeValue(kryptonTextBox_CanBoCapPhat.Text));
                        cmd.Parameters.AddWithValue("@NguoiDaiDienNhan", GetSafeValue(kryptonTextBox_NguoiDaiDienNhan.Text));
                        cmd.Parameters.AddWithValue("@GhiChu", GetSafeValue(kryptonTextBox_GhiChu.Text));

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                MessageBox.Show("Đã thêm dữ liệu thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadDataToGridAsync();
                ResetInput();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi thêm dữ liệu:\n" + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =========================================================
        // CHỨC NĂNG SỬA (1 click)
        // =========================================================
        private async void kryptonButton_SuaVaLuuKhenThuong_Click(object sender, EventArgs e)
        {
            if (!isEditing || _selectedID < 0) return;

            if (string.IsNullOrWhiteSpace(kryptonTextBox_TenTapThe.Text))
            {
                MessageBox.Show("Vui lòng nhập 'Tên tập thể'!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                kryptonTextBox_TenTapThe.Focus();
                return;
            }

            try
            {
                string tienThuongStr = kryptonTextBox_TienThuong.Text.Replace(".", "").Replace(",", "").Trim();
                if (string.IsNullOrWhiteSpace(tienThuongStr)) tienThuongStr = "0";

                string connectionString = $@"Data Source={_csdl4Path};";
                using (var conn = new SqliteConnection(connectionString))
                {
                    await conn.OpenAsync();
                    // KHÔNG sửa cột STT, giữ nguyên vị trí cũ
                    string query = @"UPDATE ThongKe_KhenThuongTapThe SET 
                                         TenTapThe = @TenTapThe, TrangThai_DonVi = @TrangThai_DonVi, 
                                         HinhThuc_KhenThuong = @HinhThuc_KhenThuong, LoaiKhenThuong = @LoaiKhenThuong, 
                                         DonVi_CapKhenThuong = @DonVi_CapKhenThuong, SoQuyetDinh = @SoQuyetDinh, 
                                         NgayQuyetDinh = @NgayQuyetDinh, NguoiKy = @NguoiKy, 
                                         NoiDung_KhenThuong = @NoiDung_KhenThuong, TienThuong = @TienThuong, 
                                         NgayCapPhat = @NgayCapPhat, CanBoCapPhat = @CanBoCapPhat, 
                                         NguoiDaiDienNhan = @NguoiDaiDienNhan, GhiChu = @GhiChu 
                                     WHERE CAST(ID AS INTEGER) = @ID";

                    using (var cmd = new SqliteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", _selectedID);
                        cmd.Parameters.AddWithValue("@TenTapThe", kryptonTextBox_TenTapThe.Text.Trim());
                        cmd.Parameters.AddWithValue("@TrangThai_DonVi", GetSafeValue(comboBox_TrangThaiDonVi.Text));
                        cmd.Parameters.AddWithValue("@HinhThuc_KhenThuong", GetSafeValue(comboBox_HinhThucKhenThuong.Text));
                        cmd.Parameters.AddWithValue("@LoaiKhenThuong", GetSafeValue(comboBox_LoaiKhenThuong.Text));
                        cmd.Parameters.AddWithValue("@DonVi_CapKhenThuong", GetSafeValue(comboBox_DonViKhenThuong.Text));
                        cmd.Parameters.AddWithValue("@SoQuyetDinh", GetSafeValue(kryptonTextBox_SoQuyetDinh.Text));
                        cmd.Parameters.AddWithValue("@NgayQuyetDinh", GetSafeValue(kryptonTextBox_NgayQuyetDinh.Text));
                        cmd.Parameters.AddWithValue("@NguoiKy", GetSafeValue(kryptonTextBox_NguoiKy.Text));
                        cmd.Parameters.AddWithValue("@NoiDung_KhenThuong", GetSafeValue(richTextBox1_NoiDungKhenThuong.Text));
                        cmd.Parameters.AddWithValue("@TienThuong", tienThuongStr);
                        cmd.Parameters.AddWithValue("@NgayCapPhat", GetSafeValue(kryptonTextBox_NgayCapPhat.Text));
                        cmd.Parameters.AddWithValue("@CanBoCapPhat", GetSafeValue(kryptonTextBox_CanBoCapPhat.Text));
                        cmd.Parameters.AddWithValue("@NguoiDaiDienNhan", GetSafeValue(kryptonTextBox_NguoiDaiDienNhan.Text));
                        cmd.Parameters.AddWithValue("@GhiChu", GetSafeValue(kryptonTextBox_GhiChu.Text));

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                MessageBox.Show("Cập nhật dữ liệu thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadDataToGridAsync();
                ResetInput();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi cập nhật dữ liệu:\n" + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =========================================================
        // CHỨC NĂNG XÓA
        // =========================================================
        private async void kryptonButton_XoaKhenThuong_Click(object sender, EventArgs e)
        {
            if (!isEditing || _selectedID < 0) return;

            string tenTapThe = kryptonTextBox_TenTapThe.Text?.Trim() ?? string.Empty;

            DialogResult result = MessageBox.Show(
                $"Hành động này không thể hoàn tác!\n\nBạn có chắc chắn muốn xóa bản ghi:\n{tenTapThe}",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                string connectionString = $@"Data Source={_csdl4Path};";
                int affectedRows;

                using (var conn = new SqliteConnection(connectionString))
                {
                    await conn.OpenAsync();
                    const string query = @"DELETE FROM ""ThongKe_KhenThuongTapThe"" WHERE CAST(ID AS INTEGER) = @ID;";

                    using (var cmd = new SqliteCommand(query, conn))
                    {
                        cmd.Parameters.Add("@ID", SqliteType.Integer).Value = _selectedID;
                        affectedRows = await cmd.ExecuteNonQueryAsync();
                    }
                }

                if (affectedRows > 0)
                {
                    // Xóa xong thì gọi luôn hàm dồn lại STT
                    await DanhLaiSoThuTuAsync();

                    MessageBox.Show("Đã xóa thành công và cập nhật lại số thứ tự.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await LoadDataToGridAsync();
                    ResetInput();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xóa dữ liệu:\n\n" + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ============================================================
        // HÀM CHUYÊN ĐÁNH LẠI SỐ THỨ TỰ SAU KHI XÓA
        // ============================================================
        private async Task DanhLaiSoThuTuAsync()
        {
            string connectionString = $@"Data Source={_csdl4Path};";
            using (var conn = new SqliteConnection(connectionString))
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Lấy tất cả ID lên, xếp theo STT cũ
                        string selectQuery = @"SELECT ID FROM ThongKe_KhenThuongTapThe ORDER BY CAST(STT AS INTEGER) ASC";
                        List<int> listIds = new List<int>();

                        using (var cmdSelect = new SqliteCommand(selectQuery, conn, transaction))
                        using (var reader = await cmdSelect.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                listIds.Add(Convert.ToInt32(reader["ID"]));
                            }
                        }

                        // Cập nhật lại STT từ 1 -> n
                        string updateQuery = @"UPDATE ThongKe_KhenThuongTapThe SET STT = @STT WHERE CAST(ID AS INTEGER) = @ID";
                        using (var cmdUpdate = new SqliteCommand(updateQuery, conn, transaction))
                        {
                            cmdUpdate.Parameters.Add("@STT", SqliteType.Integer);
                            cmdUpdate.Parameters.Add("@ID", SqliteType.Integer);

                            for (int i = 0; i < listIds.Count; i++)
                            {
                                cmdUpdate.Parameters["@STT"].Value = i + 1;
                                cmdUpdate.Parameters["@ID"].Value = listIds[i];
                                await cmdUpdate.ExecuteNonQueryAsync();
                            }
                        }
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        // =========================================================
        // CÁC HÀM CẤU HÌNH GIAO DIỆN & CSDL
        // =========================================================
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
                                ""TenTapThe"" TEXT NOT NULL,
                                ""TrangThai_DonVi"" TEXT,
                                ""HinhThuc_KhenThuong"" TEXT,
                                ""LoaiKhenThuong"" TEXT,
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
                                ""CreatedAt"" TEXT NOT NULL DEFAULT (datetime('now', 'localtime')),
                                ""UpdatedAt"" TEXT NOT NULL DEFAULT (datetime('now', 'localtime')),
                                PRIMARY KEY(""ID"",""TenTapThe"")
                            );
                            CREATE INDEX IF NOT EXISTS ""idx_kt_tentapthe"" ON ""ThongKe_KhenThuongTapThe"" (""TenTapThe"");
                            ";
                            using (SqliteCommand cmdCreate = new SqliteCommand(createTableQuery, conn)) { cmdCreate.ExecuteNonQuery(); }
                        }
                    }
                }
                catch { }
            }
        }

        private void CauHinhGridCoBan(DataGridView dgv) { dgv.AutoGenerateColumns = false; dgv.ReadOnly = true; dgv.AllowUserToAddRows = false; dgv.AllowUserToDeleteRows = false; dgv.MultiSelect = false; dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect; dgv.RowHeadersVisible = false; dgv.EnableHeadersVisualStyles = false; dgv.ScrollBars = ScrollBars.Both; dgv.BorderStyle = BorderStyle.None; }

        private void CauHinhStyleWeb(DataGridView dgv) { dgv.RowTemplate.Height = 32; dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None; dgv.AllowUserToResizeRows = false; dgv.ColumnHeadersHeight = 45; dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing; dgv.CellBorderStyle = DataGridViewCellBorderStyle.Single; dgv.GridColor = Color.FromArgb(224, 224, 224); dgv.RowsDefaultCellStyle.BackColor = Color.White; dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252); dgv.BackgroundColor = Color.White; if (dgv is Krypton.Toolkit.KryptonDataGridView kDgv) { kDgv.GridStyles.Style = Krypton.Toolkit.DataGridViewStyle.List; kDgv.StateCommon.HeaderColumn.Content.Padding = new System.Windows.Forms.Padding(6, 8, 6, 8); kDgv.StateCommon.HeaderColumn.Content.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold); kDgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; kDgv.StateCommon.DataCell.Content.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular); kDgv.StateCommon.DataCell.Content.Padding = new System.Windows.Forms.Padding(6, 8, 6, 8); kDgv.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All; kDgv.StateCommon.DataCell.Border.Color1 = System.Drawing.Color.FromArgb(224, 224, 224); kDgv.StateCommon.DataCell.Border.Width = 1; kDgv.StateSelected.DataCell.Back.Color1 = System.Drawing.Color.FromArgb(232, 244, 253); kDgv.StateSelected.DataCell.Back.Color2 = System.Drawing.Color.FromArgb(232, 244, 253); kDgv.StateSelected.DataCell.Content.Color1 = System.Drawing.Color.FromArgb(0, 102, 204); kDgv.Margin = new Padding(0, 0, 0, 30); } }

        private void CauHinhCotGrid(DataGridView dgv)
        {
            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ID", Name = "ID", Visible = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "STT", Name = "STT", HeaderText = "STT", Width = 50, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TenTapThe", Name = "TenTapThe", HeaderText = "Tên tập thể", Width = 220, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TrangThai_DonVi", Name = "TrangThai_DonVi", HeaderText = "Trạng thái đơn vị", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "HinhThuc_KhenThuong", Name = "HinhThuc_KhenThuong", HeaderText = "Hình Thức khen thưởng", Width = 150, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "LoaiKhenThuong", Name = "LoaiKhenThuong", HeaderText = "Loại khen thưởng", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DonVi_CapKhenThuong", Name = "DonVi_CapKhenThuong", HeaderText = "Đơn vị khen thưởng", Width = 180, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SoQuyetDinh", Name = "SoQuyetDinh", HeaderText = "Số Quyết định", Width = 100, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NgayQuyetDinh", Name = "NgayQuyetDinh", HeaderText = "Ngày Quyết định", Width = 100, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NguoiKy", Name = "NguoiKy", HeaderText = "Người ký", Width = 130, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NoiDung_KhenThuong", Name = "NoiDung_KhenThuong", HeaderText = "Nội Dung khen thưởng", Width = 250, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });

            // Format N0 để hiển thị tiền kiểu xxx.xxx.xxx trên lưới
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TienThuong", Name = "TienThuong", HeaderText = "Tiền thưởng (VNĐ)", Width = 140, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N0" } });

            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NgayCapPhat", Name = "NgayCapPhat", HeaderText = "Ngày cấp phát", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CanBoCapPhat", Name = "CanBoCapPhat", HeaderText = "Cán bộ cấp", Width = 130, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NguoiDaiDienNhan", Name = "NguoiDaiDienNhan", HeaderText = "Người đại diện nhận", Width = 150, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "GhiChu", Name = "GhiChu", HeaderText = "Ghi chú", Width = 150, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
        }

        private void kryptonButton1_Thoat_Click(object sender, EventArgs e)
        {
            Form34_ThongKeKhenThuong form34 = Application.OpenForms.OfType<Form34_ThongKeKhenThuong>().FirstOrDefault();
            if (form34 != null) { form34.Show(); form34.BringToFront(); }
            else { form34 = new Form34_ThongKeKhenThuong(); form34.Show(); }

            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            if (formCha != null) { formCha.CapNhatTieuDe("Trang Thống kê khen thưởng"); }
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
    }
}