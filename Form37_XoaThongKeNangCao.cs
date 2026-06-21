using Microsoft.Data.Sqlite;

namespace PhanMemThiDua2026
{
    public partial class Form37_XoaThongKeNangCao : Form
    {
        private Form15_ThongKeThiDua _formCha;
        private string _csdl4Path = Module_DanduongGPS.DuongDanCSDL4;

        public Form37_XoaThongKeNangCao(Form15_ThongKeThiDua parent)
        {
            InitializeComponent();
            this._formCha = parent;
            this.MaximizeBox = false; // Mờ nút Phóng to (Maximize) trên thanh tiêu đề
            this.FormBorderStyle = FormBorderStyle.FixedSingle; // Khóa viền, ngăn dùng chuột kéo giãn Form
        }

        private void Form37_XoaThongKeNangCao_Load(object sender, EventArgs e)
        {
            ThietLapGiaoDienTheoPhienBan();
        }

        private void ThietLapGiaoDienTheoPhienBan()
        {
            // 1. Xác định phiên bản
            string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
            bool laTanBinh = phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase);
            string loaiDoiTuong = laTanBinh ? "tân binh" : "CBCS";
            string tenBang = laTanBinh ? "ThiDuaThang_TanBinh" : "ThiDuaThang";

            // 2. Đếm số lượng thực tế từ Database
            int tong = 0, dangCongTac = 0, chuyenCongTac = 0;

            try
            {
                using (var conn = new SqliteConnection($"Data Source={_csdl4Path}"))
                {
                    conn.Open();
                    // Đếm tổng
                    using (var cmd = new SqliteCommand($"SELECT COUNT(*) FROM {tenBang}", conn))
                        tong = Convert.ToInt32(cmd.ExecuteScalar());

                    // Đếm Đang công tác
                    using (var cmd = new SqliteCommand($"SELECT COUNT(*) FROM {tenBang} WHERE TinhTrang = 'Đang công tác'", conn))
                        dangCongTac = Convert.ToInt32(cmd.ExecuteScalar());

                    // Đếm Chuyển công tác
                    using (var cmd = new SqliteCommand($"SELECT COUNT(*) FROM {tenBang} WHERE TinhTrang = 'Chuyển công tác'", conn))
                        chuyenCongTac = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi truy xuất dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btn_XoaDuLieuThongKe.Enabled = false;
                return;
            }

            // 3. Gán Text cho các RadioButton để hiện rõ số lượng
            radioButton1_XoaTatCaDuLieuThongKe.Text = $"Xóa tất cả dữ liệu ({tong} đồng chí {loaiDoiTuong})";
            radioButton2_XoaCBCSDangCongTac.Text = $"Xóa danh sách đang công tác ({dangCongTac} đ/c {loaiDoiTuong})";
            radioButton3_XoaCBCSChuyenCongTac.Text = $"Xóa danh sách chuyển công tác ({chuyenCongTac} đ/c {loaiDoiTuong})";

            // ========================================================
            // 4. 🔥 LOGIC UX MỚI THEO YÊU CẦU
            // ========================================================

            // Luôn luôn hiển thị "Xóa tất cả" nếu có dữ liệu
            radioButton1_XoaTatCaDuLieuThongKe.Visible = (tong > 0);

            // Kịch bản: Cả 2 loại tình trạng đều > 0 (Có cả người Đang công tác và Chuyển công tác)
            if (dangCongTac > 0 && chuyenCongTac > 0)
            {
                // Hiện TẤT CẢ các tùy chọn để người dùng tự do chọn lọc
                radioButton2_XoaCBCSDangCongTac.Visible = true;
                radioButton3_XoaCBCSChuyenCongTac.Visible = true;

                // Mặc định tick vào "Xóa tất cả"
                radioButton1_XoaTatCaDuLieuThongKe.Checked = true;
            }
            // Kịch bản: Dữ liệu bị đơn điệu (Chỉ có 1 trong 2 loại tình trạng, hoặc không có ai)
            else
            {
                // Ẩn 2 lựa chọn con đi, chỉ giữ lại lựa chọn "Xóa tất cả"
                radioButton2_XoaCBCSDangCongTac.Visible = false;
                radioButton3_XoaCBCSChuyenCongTac.Visible = false;

                // Nếu có dữ liệu thì tự tick vào "Xóa tất cả"
                if (tong > 0)
                {
                    radioButton1_XoaTatCaDuLieuThongKe.Checked = true;
                }
            }

            // ========================================================
            // 5. Khóa nút nếu không có dữ liệu
            // ========================================================
            btn_XoaDuLieuThongKe.Enabled = (tong > 0);
            if (tong == 0)
            {
                btn_XoaDuLieuThongKe.Text = "Không có dữ liệu để xóa";
            }
        }

        private async void btn_XoaDuLieuThongKe_Click(object sender, EventArgs e)
        {
            // 1. Kiểm tra an toàn: Phải có ít nhất 1 RadioButton hiển thị và được chọn
            if ((!radioButton1_XoaTatCaDuLieuThongKe.Visible || !radioButton1_XoaTatCaDuLieuThongKe.Checked) &&
                (!radioButton2_XoaCBCSDangCongTac.Visible || !radioButton2_XoaCBCSDangCongTac.Checked) &&
                (!radioButton3_XoaCBCSChuyenCongTac.Visible || !radioButton3_XoaCBCSChuyenCongTac.Checked))
            {
                return;
            }

            string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
            bool laTanBinh = phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase);
            string tenBang = laTanBinh ? "ThiDuaThang_TanBinh" : "ThiDuaThang";

            string sqlQuery = "";
            string thongBaoLog = "";

            // 2. Xác định điều kiện xóa
            if (radioButton1_XoaTatCaDuLieuThongKe.Checked)
            {
                sqlQuery = $"DELETE FROM {tenBang}";
                thongBaoLog = "Xóa toàn bộ dữ liệu thống kê";
            }
            else if (radioButton2_XoaCBCSDangCongTac.Checked)
            {
                sqlQuery = $"DELETE FROM {tenBang} WHERE TinhTrang = 'Đang công tác'";
                thongBaoLog = "Xóa danh sách đang công tác";
            }
            else if (radioButton3_XoaCBCSChuyenCongTac.Checked)
            {
                sqlQuery = $"DELETE FROM {tenBang} WHERE TinhTrang = 'Chuyển công tác'";
                thongBaoLog = "Xóa danh sách chuyển công tác";
            }

            if (string.IsNullOrEmpty(sqlQuery)) return;

            try
            {
                // 3. Khóa UI
                this.Cursor = Cursors.WaitCursor;
                btn_XoaDuLieuThongKe.Enabled = false;
                int rowsAffected = 0;

                // 4. Xóa ngầm
                await Task.Run(() =>
                {
                    using (var conn = new SqliteConnection($"Data Source={_csdl4Path}"))
                    {
                        conn.Open();
                        using (var tran = conn.BeginTransaction())
                        {
                            try
                            {
                                using (var cmd = new SqliteCommand(sqlQuery, conn, tran))
                                {
                                    rowsAffected = cmd.ExecuteNonQuery();
                                }

                                if (radioButton1_XoaTatCaDuLieuThongKe.Checked)
                                {
                                    using (var cmdSeq = new SqliteCommand($"DELETE FROM sqlite_sequence WHERE name='{tenBang}'", conn, tran))
                                        cmdSeq.ExecuteNonQuery();
                                }

                                tran.Commit();
                            }
                            catch
                            {
                                tran.Rollback();
                                throw;
                            }
                        }

                        using (var cmdVac = new SqliteCommand("VACUUM", conn))
                            cmdVac.ExecuteNonQuery();
                    }
                });

                // 5. Ghi Log
                Module_NhatKy.GhiNhatKy(
                    Module_TaiKhoan.TenTaiKhoan_RAM,
                    "Xóa nâng cao",
                    $"{thongBaoLog} ({rowsAffected} dòng)"
                );

                // 6. Cập nhật và Đóng
                if (_formCha != null && !_formCha.IsDisposed)
                {
                    _formCha.ReloadData();
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống khi xóa dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                if (!this.IsDisposed) btn_XoaDuLieuThongKe.Enabled = true;
            }
        }
    }
}