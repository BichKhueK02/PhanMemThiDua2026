using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Runtime.InteropServices; // Thêm để dùng SendMessage

namespace PhanMemThiDua2026
{
    public partial class Form22_ChinhSuaDataCBCS : Form
    {
        private readonly string _csdl4Path = Module_DanduongGPS.DuongDanCSDL4;

        // CÁC PROPERTIES GỐC CỦA BẠN
        public int ID_CBCS { get; set; } = -1; // Cố tình gán -1 để hàm LoadData biết đường rẽ nhánh tìm theo Số Hiệu
        public string HoVaTen { get; set; } = "";
        public string SoHieu { get; set; } = "";
        public string TinhTrang { get; set; } = "";
        public string DonVi { get; set; } = "";
        // 👇 BỔ SUNG CỜ ẨN NÚT CHO TÍNH NĂNG CHỈ XEM TỪ FORM 6
        public bool IsViewOnly { get; set; } = false;

        // =========================================================
        // KỸ THUẬT NGƯNG VẼ ĐỒ HỌA (CHỐNG GIẬT)
        // =========================================================
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);
        private const int WM_SETREDRAW = 11;
        public Form22_ChinhSuaDataCBCS()
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = false;
            InitializeComponent();

            // ⭐ Bật DoubleBuffered để Form vẽ mượt hơn
            this.DoubleBuffered = true;

            // ⭐ SỬA LỖI Ở ĐÂY: Kết nối sự kiện Load để Form nhận lệnh in dữ liệu lên Label
            this.Load += Form22_ChinhSuaDataCBCS_Load;

            // Chuyển toàn bộ logic nặng sang sự kiện Shown
            this.Shown += Form22_ChinhSuaDataCBCS_Shown;
        }
        // Sự kiện Load giờ chỉ làm những việc KHÔNG tốn thời gian
        private void Form22_ChinhSuaDataCBCS_Load(object sender, EventArgs e)
        {
            // ⭐ Kỹ thuật ngưng vẽ đồ họa để tối ưu tốc độ hiển thị
            SendMessage(this.Handle, WM_SETREDRAW, false, 0);

            try
            {
                // 1. Cấu hình thuộc tính Form (UI Metadata)
                SetupFormAppearance();
                // 2. Đồng bộ dữ liệu lên nhãn hiển thị (Data Binding)
                BindHeaderInformation();
                // 3. Xử lý trạng thái công tác (Business Logic hiển thị)
                SetStatusDisplay();
                // 4. Kiểm tra quyền hạn và chế độ xem (Authorization/Mode)
                ApplyViewOnlyMode();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Form22 Load Error] {ex.Message}");
            }
            finally
            {
                // ⭐ Bật lại vẽ đồ họa và làm mới Form
                SendMessage(this.Handle, WM_SETREDRAW, true, 0);
                this.Refresh();
            }
        }
        /// <summary>
        /// Thiết lập ngoại hình và cấu hình cơ bản cho Form
        /// </summary>
        private void SetupFormAppearance()
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.AcceptButton = kryptonButton1_CapNhat;
            // Tránh việc DialogResult tự đóng form khi dùng Krypton
            try { kryptonButton1_CapNhat.DialogResult = DialogResult.None; } catch { }
        }
        /// <summary>
        /// Gán thông tin Họ tên, Số hiệu lên tiêu đề
        /// </summary>
        private void BindHeaderInformation()
        {
            label1_ID_HoVaTen.Text = this.HoVaTen;
            //label_ID_SoHieu.Text = $"Số hiệu CAND: {this.SoHieu}";
            label_ID_SoHieu.Text = this.SoHieu;
        }
        /// <summary>
        /// Thiết lập logic hiển thị trạng thái công tác (Mặc định: Đang công tác)
        /// </summary>
        private void SetStatusDisplay()
        {
            // Logic: Nếu là "Chuyển công tác" thì hiện thông tin đơn vị cũ, ngược lại mặc định là Đang công tác
            if (!string.IsNullOrWhiteSpace(TinhTrang) &&
                TinhTrang.Equals("Chuyển công tác", StringComparison.OrdinalIgnoreCase))
            {
                toolStripLabel1.Text = $"Tình trạng công tác: {TinhTrang} - Đơn vị công tác cũ là {LayTenDonViHienThi(DonVi)}";
            }
            else
            {
                // ⭐ Đáp ứng yêu cầu: Luôn hiển thị Đang công tác khi mở từ danh sách Form 6
                toolStripLabel1.Text = "Tình trạng: Đang công tác";
            }
        }
        /// <summary>
        /// Áp dụng chế độ chỉ xem nếu được gọi từ Form 6
        /// </summary>
        private void ApplyViewOnlyMode()
        {
            if (this.IsViewOnly)
            {
                kryptonButton1_CapNhat.Visible = false; // Ẩn nút Cập nhật
                KhoaGiaoDienNhapLieu(this);            // Khóa toàn bộ các ComboBox
            }
        }
        // =========================================================
        // 👇 HÀM BỔ SUNG ĐỂ FORM 6 "BƠM" DỮ LIỆU MỚI VÀO RAM 
        // =========================================================
        public async void CapNhatDuLieuMoi(string soHieu, string hoTen, string donVi, string tinhTrang)
        {
            // 1. Cập nhật Properties
            ID_CBCS = -1; // Reset để ép hàm Load quét tìm theo Số hiệu
            SoHieu = soHieu;
            HoVaTen = hoTen;
            DonVi = donVi;
            TinhTrang = tinhTrang;

            // 2. Cập nhật Text cơ bản trên Form
            label1_ID_HoVaTen.Text = HoVaTen;
            //label_ID_SoHieu.Text = "Số hiệu CAND: " + SoHieu;
            //this.Text = $"Hồ sơ thi đua - {HoVaTen}";// SỬA LẠI DÒNG DƯỚI ĐÂY: Bỏ chữ "Số hiệu CAND: " đi
            label_ID_SoHieu.Text = SoHieu;

            this.Text = $"Hồ sơ thi đua - {HoVaTen}";


            if (!string.IsNullOrWhiteSpace(TinhTrang) &&
                TinhTrang.Equals("Chuyển công tác", StringComparison.OrdinalIgnoreCase))
            {
                toolStripLabel1.Text = $"Tình trạng công tác: {TinhTrang} - Đơn vị công tác cũ là {LayTenDonViHienThi(DonVi)}";
            }
            else
            {
                toolStripLabel1.Text = $"Tình trạng công tác: {TinhTrang}";
            }

            // 3. Tẩy trắng ComboBox cũ
            XoaTrangComboBox(this);

            // 4. Tải lại Data mới
            await LoadDataAsync();
        }
        // =========================================================
        // CÁC HÀM TIỆN ÍCH HỖ TRỢ XỬ LÝ GIAO DIỆN HÀNG LOẠT
        // =========================================================
        private void KhoaGiaoDienNhapLieu(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is ComboBox cb) cb.Enabled = false;
                if (c.HasChildren) KhoaGiaoDienNhapLieu(c);
            }
        }
        private void XoaTrangComboBox(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is ComboBox cb) cb.Text = "";
                if (c.HasChildren) XoaTrangComboBox(c);
            }
        }
        // Hàm giải mã an toàn dùng chung cho toàn bộ Form
        public static string GiaiMaSafe(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            try
            {
                string result = BaoMatAES.GiaiMa(input);
                return string.IsNullOrWhiteSpace(result) ? input : result;
            }
            catch { return input; }
        }
        // ⭐ TỐI ƯU 1: Xử lý dữ liệu nặng ở đây. Form đã hiện lên màn hình rồi mới nạp
        private async void Form22_ChinhSuaDataCBCS_Shown(object sender, EventArgs e)
        {
            try
            {
                // Cho Windows 1 nhịp nghỉ nhỏ xíu (10ms) để nó vẽ xong cái vỏ Form
                await Task.Delay(10);

                // Dừng toàn bộ việc vẽ lại các thành phần con để nạp dữ liệu 1 lượt
                SendMessage(this.Handle, WM_SETREDRAW, false, 0);

                var namHienTai = Module_NamHeThong.LayNamHeThong();
                var namThiDuaTruoc = namHienTai - 1;
                groupBox1_ThongTinNamCu.Text = $"1. Thông tin thi đua năm cũ (Năm {namThiDuaTruoc})";
                groupBox2_ThongTinThiDuaKhenThuongNamHienTai.Text = $"2. Thông tin thi đua - khen thưởng năm {namHienTai}";

                if (!string.IsNullOrWhiteSpace(TinhTrang) &&
                    TinhTrang.Equals("Chuyển công tác", StringComparison.OrdinalIgnoreCase))
                {
                    toolStripLabel1.Text = $"Tình trạng công tác: {TinhTrang} - Đơn vị công tác cũ là {LayTenDonViHienThi(DonVi)}";
                }
                else
                {
                    toolStripLabel1.Text = $"Tình trạng công tác: {TinhTrang}";
                }

                label_Thang_12_NamCu.Text = $"Tháng 12/{namThiDuaTruoc}";
                for (int i = 1; i <= 11; i++)
                {
                    var labelMonth = Controls.Find($"label_Thang_{i}", true);
                    if (labelMonth.Length > 0)
                        labelMonth[0].Text = $"Tháng {i}/{namHienTai}";
                }

                // ⭐ TỐI ƯU 2: Tải CSDL ngầm
                await LoadDataAsync();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi tạo giao diện: " + ex.Message);
            }
            finally
            {
                // Cho phép Windows vẽ lại Form. Lúc này tất cả Control đã có đầy đủ data.
                SendMessage(this.Handle, WM_SETREDRAW, true, 0);
                this.Refresh();
            }
        }
        private async Task LoadDataAsync()
        {
            try
            {
                using (var cn = new SqliteConnection($"Data Source={_csdl4Path}"))
                {
                    await cn.OpenAsync();

                    // NHÁNH 1: SỬA TỪ FORM CŨ (Đã có ID)
                    if (ID_CBCS > 0)
                    {
                        using (var cmd = new SqliteCommand("SELECT * FROM ThiDuaThang WHERE ID = @id", cn))
                        {
                            cmd.Parameters.AddWithValue("@id", ID_CBCS);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    DoDuLieuVaoComboBox(reader);
                                }
                            }
                        }
                    }
                    // NHÁNH 2: XEM TỪ FORM 6 (Quét tìm theo Số Hiệu)
                    else if (!string.IsNullOrEmpty(SoHieu))
                    {
                        using (var cmd = new SqliteCommand("SELECT * FROM ThiDuaThang", cn))
                        {
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                bool found = false;
                                while (await reader.ReadAsync())
                                {
                                    string dbSoHieu = GiaiMaSafe(reader["SoHieu"]?.ToString()).Trim();

                                    if (string.Equals(dbSoHieu, SoHieu, StringComparison.OrdinalIgnoreCase))
                                    {
                                        ID_CBCS = Convert.ToInt32(reader["ID"]); // Cất ID chuẩn vào túi
                                        DoDuLieuVaoComboBox(reader);
                                        found = true;
                                        break;
                                    }
                                }

                                if (!found)
                                {
                                    MessageBox.Show($"Chưa có kết quả thi đua của đồng chí mang Số hiệu: {SoHieu} trong cơ sở dữ liệu.", "Chưa có dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi Database: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Tách riêng hàm đổ dữ liệu để dùng chung cho cả 2 nhánh
        private void DoDuLieuVaoComboBox(SqliteDataReader reader)
        {
            comboBox_KQXepLoaiCBCS.Text = reader["KQ_XepLoaiCB_Nam_Cu"]?.ToString() ?? "";
            comboBox_QKXepLoaiThiDua.Text = reader["KQ_ThiDua_Nam_Cu"]?.ToString() ?? "";
            comboBox_KQXepLoaiDangVien.Text = reader["KQ_XepLoaiDangVien_Nam_Cu"]?.ToString() ?? "";

            var mapThang = new Dictionary<string, ComboBox>
            {
                { "Thang_12_Nam_Cu", comboBox_KQPhanLoaiThang_12_NamCu },
                { "Thang_1", comboBox_KQPhanLoaiThang_1 },
                { "Thang_2", comboBox_KQPhanLoaiThang_2 },
                { "Thang_3", comboBox_KQPhanLoaiThang_3 },
                { "Thang_4", comboBox_KQPhanLoaiThang_4 },
                { "Thang_5", comboBox_KQPhanLoaiThang_5 },
                { "Sau_Thang_Dau_Nam", comboBox_KQPhanLoai6_Thang_Dau_Nam },
                { "Thang_6", comboBox_KQPhanLoaiThang_6 },
                { "Thang_7", comboBox_KQPhanLoaiThang_7 },
                { "Thang_8", comboBox_KQPhanLoaiThang_8 },
                { "Thang_9", comboBox_KQPhanLoaiThang_9 },
                { "Thang_10", comboBox_KQPhanLoaiThang_10 },
                { "Thang_11", comboBox_KQPhanLoaiThang_11 },
                { "TongKet_Nam", comboBox_KQPhanLoaiTongKet_Nam }
            };

            foreach (var item in mapThang)
            {
                item.Value.Text = reader[item.Key]?.ToString() ?? "";
            }
        }
        // =========================================================
        // GIỮ NGUYÊN HOÀN TOÀN CÁC HÀM BÊN DƯỚI CỦA BẠN
        // =========================================================
        public void ApplyFilter()
        {
            try
            {
                if (this.Owner is Form15_ThongKeThiDua parent)
                {
                    parent.ApplyFilter();
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi áp dụng bộ lọc: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void kryptonButton1_CapNhat_Click(object sender, EventArgs e)
        {
            try
            {
                using var cn = new SqliteConnection($"Data Source={_csdl4Path}");
                cn.Open();

                using (var cmdCheck = new SqliteCommand("SELECT 1 FROM ThiDuaThang WHERE ID = @id LIMIT 1", cn))
                {
                    cmdCheck.Parameters.AddWithValue("@id", ID_CBCS);
                    if (cmdCheck.ExecuteScalar() == null)
                    {
                        MessageBox.Show("Không tìm thấy ID cần cập nhật!");
                        return;
                    }
                }

                using var cmd = new SqliteCommand(@"
                UPDATE ThiDuaThang SET
                    [KQ_XepLoaiCB_Nam_Cu] = @cbcs,
                    [KQ_ThiDua_Nam_Cu] = @td,
                    [KQ_XepLoaiDangVien_Nam_Cu] = @dangvien,
                    [Thang_12_Nam_Cu] = @t12,
                    [Thang_1] = @t1,
                    [Thang_2] = @t2,
                    [Thang_3] = @t3,
                    [Thang_4] = @t4,
                    [Thang_5] = @t5,
                    [Sau_Thang_Dau_Nam] = @sauThang,
                    [Thang_6] = @t6,
                    [Thang_7] = @t7,
                    [Thang_8] = @t8,
                    [Thang_9] = @t9,
                    [Thang_10] = @t10,
                    [Thang_11] = @t11,
                    [TongKet_Nam] = @tongket
                WHERE ID = @id
                ", cn);

                var comboValues = new[]
                {
                    comboBox_KQXepLoaiCBCS.Text,
                    comboBox_QKXepLoaiThiDua.Text,
                    comboBox_KQXepLoaiDangVien.Text,
                    comboBox_KQPhanLoaiThang_12_NamCu.Text,
                    comboBox_KQPhanLoaiThang_1.Text,
                    comboBox_KQPhanLoaiThang_2.Text,
                    comboBox_KQPhanLoaiThang_3.Text,
                    comboBox_KQPhanLoaiThang_4.Text,
                    comboBox_KQPhanLoaiThang_5.Text,
                    comboBox_KQPhanLoai6_Thang_Dau_Nam.Text,
                    comboBox_KQPhanLoaiThang_6.Text,
                    comboBox_KQPhanLoaiThang_7.Text,
                    comboBox_KQPhanLoaiThang_8.Text,
                    comboBox_KQPhanLoaiThang_9.Text,
                    comboBox_KQPhanLoaiThang_10.Text,
                    comboBox_KQPhanLoaiThang_11.Text,
                    comboBox_KQPhanLoaiTongKet_Nam.Text
                };

                string[] paramNames =
                {
                    "@cbcs", "@td", "@dangvien",
                    "@t12", "@t1", "@t2", "@t3", "@t4", "@t5", "@sauThang",
                    "@t6", "@t7", "@t8", "@t9", "@t10", "@t11", "@tongket"
                };

                for (int i = 0; i < paramNames.Length; i++)
                {
                    cmd.Parameters.AddWithValue(paramNames[i], comboValues[i]);
                }

                cmd.Parameters.AddWithValue("@id", ID_CBCS);

                int rows = cmd.ExecuteNonQuery();
                if (rows > 0)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Không có dữ liệu nào được cập nhật!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi cập nhật dữ liệu: " + ex.Message);
            }
        }
        private string LayTenDonViHienThi(string donVi)
        {
            if (string.IsNullOrWhiteSpace(donVi))
                return "";

            return donVi == "BCH"
                ? "Tiểu đoàn 2 (Ban Chỉ huy D)"
                : donVi;
        }
    }
}