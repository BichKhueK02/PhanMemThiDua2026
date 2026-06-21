using Microsoft.Data.Sqlite;

namespace PhanMemThiDua2026
{
    public partial class Form30_ChinhSuaDataTanBinh : Form
    {

        private readonly string _csdl4Path = Module_DanduongGPS.DuongDanCSDL4; // Giữ lại vì đường dẫn DB không đổi
        private int _id = -1;               // 👈 Đã xóa readonly
        private string _donVi = "";         // 👈 Đã xóa readonly
        private Dictionary<string, ComboBox> _cboMapping;
        // 2. BIẾN BỔ SUNG CHO TÍNH NĂNG XEM TỪ FORM 6
        private string _soHieuTimKiem = ""; // 👈 Đã xóa readonly
        private string _hoTenTimKiem = "";  // 👈 Đã xóa readonly
        private readonly bool _isViewOnly = false; // Giữ lại vì cờ ẩn nút không thay đổi
        // CONSTRUCTOR 1: BẢN GỐC (Form khác gọi để SỬA)
        public Form30_ChinhSuaDataTanBinh(int id, string donVi)
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            _id = id;
            _donVi = donVi;
        }
        // ======================================================
        // CONSTRUCTOR 2: BẢN MỚI (Form 6 gọi để XEM THEO SỐ HIỆU)
        // ======================================================
        public Form30_ChinhSuaDataTanBinh(string soHieu, string hoTen, string donVi, bool isViewOnly)
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            _id = -1; // Cố tình để -1 để hàm LoadData biết đường rẽ nhánh
            _soHieuTimKiem = soHieu;
            _hoTenTimKiem = hoTen;
            _donVi = donVi;
            _isViewOnly = isViewOnly;
        }

        private void Form30_ChinhSuaDataTanBinh_Load(object sender, EventArgs e)
        {
            TaoDanhSachCombo();
            KhoiTaoGiaTriCombo();

            LoadData(); // Chạy hàm truy xuất dữ liệu

            label_DonVi.Text = "Đơn vị: " + _donVi;

            // XỬ LÝ CHẾ ĐỘ CHỈ XEM (KHI ĐƯỢC GỌI TỪ FORM 6)
            if (_isViewOnly)
            {
                kryptonButton1_CapNhat.Visible = false; // Tàng hình nút cập nhật

                // Khóa tất cả ComboBox để tránh người dùng sửa màu
                if (_cboMapping != null)
                {
                    foreach (var c in _cboMapping.Values)
                    {
                        c.Enabled = false;
                    }
                }
            }
        }
        // ======================================================
        // HÀM NHẬN DỮ LIỆU MỚI TỪ FORM 6 ĐỂ VẼ LẠI GIAO DIỆN (SINGLETON)
        // ======================================================
        public void CapNhatDuLieuMoi(string soHieu, string hoTen, string donVi)
        {
            // 1. Cập nhật lại các biến tìm kiếm
            _id = -1; // Ép LoadData quét theo Số Hiệu thay vì quét theo ID
            _soHieuTimKiem = soHieu;
            _hoTenTimKiem = hoTen;
            _donVi = donVi;

            // 2. Xóa trắng dữ liệu cũ trên các ComboBox
            if (_cboMapping != null)
            {
                foreach (var c in _cboMapping.Values)
                {
                    c.Text = "";
                    SetComboBoxColor(c); // Đưa màu về mặc định (trắng)
                }
            }

            // 3. Load lại dữ liệu từ DB lên giao diện
            LoadData();

            // 4. Cập nhật lại Tiêu đề form và Đơn vị
            this.Text = $"Hồ sơ thi đua - {hoTen}";
            label_DonVi.Text = "Đơn vị: " + _donVi;
        }
        private void TaoDanhSachCombo()
        {
            _cboMapping = new Dictionary<string, ComboBox>()
            {
                { "Tuan_1_T2", combobox_tuan_1_Thang2 },
                { "Tuan_2_T2", combobox_tuan_2_Thang2 },
                { "Tuan_3_T2", combobox_tuan_3_Thang2 },
                { "Tuan_4_T2", combobox_tuan_4_Thang2 },
                { "Thang_3", tk_Thang3 },

                { "Tuan_1_T3", combobox_tuan_1_Thang3 },
                { "Tuan_2_T3", combobox_tuan_2_Thang3 },
                { "Tuan_3_T3", combobox_tuan_3_Thang3 },
                { "Tuan_4_T3", combobox_tuan_4_Thang3 },
                { "Thang_4", tk_Thang4 },

                { "Tuan_1_T4", combobox_tuan_1_Thang4 },
                { "Tuan_2_T4", combobox_tuan_2_Thang4 },
                { "Tuan_3_T4", combobox_tuan_3_Thang4 },
                { "Tuan_4_T4", combobox_tuan_4_Thang4 },
                { "Thang_5", tk_Thang5 },

                { "Tuan_1_T5", combobox_tuan_1_Thang5 },
                { "Tuan_2_T5", combobox_tuan_2_Thang5 },
                { "Tuan_3_T5", combobox_tuan_3_Thang5 },
                { "Tuan_4_T5", combobox_tuan_4_Thang5 },
                { "Thang_6", tk_Thang6 }
            };
        }
        private void KhoiTaoGiaTriCombo()
        {
            string[] loai = { "", "Loại 1", "Loại 2", "Loại 3", "Loại 4" };

            foreach (var c in _cboMapping.Values)
            {
                c.Items.Clear();
                c.Items.AddRange(loai);
                c.SelectedIndexChanged -= ComboBox_ThayDoiMauSac;
                c.TextChanged -= ComboBox_ThayDoiMauSac;
                c.SelectedIndexChanged += ComboBox_ThayDoiMauSac;
                c.TextChanged += ComboBox_ThayDoiMauSac;
            }
        }
        private void ComboBox_ThayDoiMauSac(object sender, EventArgs e)
        {
            if (sender is ComboBox cb) SetComboBoxColor(cb);
        }
        private void SetComboBoxColor(ComboBox cb)
        {
            if (string.IsNullOrEmpty(cb.Text))
            {
                cb.BackColor = SystemColors.Window;
                cb.ForeColor = SystemColors.WindowText;
                return;
            }

            switch (cb.Text)
            {
                case "Loại 1":
                    cb.BackColor = Color.ForestGreen;
                    cb.ForeColor = Color.White;
                    break;
                case "Loại 2":
                    cb.BackColor = Color.LightGreen;
                    cb.ForeColor = Color.Black;
                    break;
                case "Loại 3":
                    cb.BackColor = Color.Yellow;
                    cb.ForeColor = Color.Black;
                    break;
                case "Loại 4":
                    cb.BackColor = Color.Red;
                    cb.ForeColor = Color.White;
                    break;
                default:
                    cb.BackColor = SystemColors.Window;
                    cb.ForeColor = SystemColors.WindowText;
                    break;
            }
        }
        private void LoadData()
        {
            try
            {
                using var cn = new SqliteConnection($"Data Source={_csdl4Path}");
                cn.Open();

                // NHÁNH 1: Load bằng ID (Giữ nguyên logic cũ của bạn)
                if (_id != -1)
                {
                    using var cmd = new SqliteCommand("SELECT * FROM ThiDuaThang_TanBinh WHERE ID=@id", cn);
                    cmd.Parameters.AddWithValue("@id", _id);
                    using var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        HienThiDuLieuLenForm(reader);
                    }
                }
                // NHÁNH 2: Load bằng Số Hiệu (Khi Form 6 gọi)
                else if (!string.IsNullOrEmpty(_soHieuTimKiem))
                {
                    using var cmd = new SqliteCommand("SELECT * FROM ThiDuaThang_TanBinh", cn);
                    using var reader = cmd.ExecuteReader();
                    bool daTimThay = false;

                    // Vì Số hiệu bị mã hóa nên phải lấy hết ra, giải mã rồi so sánh
                    while (reader.Read())
                    {
                        string dbSoHieu = GiaiMaSafe(reader["SoHieu"]?.ToString()).Trim();
                        if (string.Equals(dbSoHieu, _soHieuTimKiem, StringComparison.OrdinalIgnoreCase))
                        {
                            HienThiDuLieuLenForm(reader);
                            daTimThay = true;
                            break;
                        }
                    }

                    if (!daTimThay)
                    {
                        // Không hiện thông báo báo lỗi nữa, chỉ cần đổ thông tin cơ bản lên Label cho đẹp giao diện
                        label1_HoVaTen.Text = _hoTenTimKiem;
                        label1_ID_Tanbinh.Text = "Số hiệu: " + _soHieuTimKiem;
                        return; // Kết thúc hàm
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi load dữ liệu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Tách phần gán giao diện ra 1 hàm nhỏ cho sạch sẽ, dùng chung cho cả 2 nhánh Sửa & Xem
        private void HienThiDuLieuLenForm(SqliteDataReader reader)
        {
            string dbHoTen = GiaiMaSafe(reader["HoVaTen"]?.ToString());
            label1_HoVaTen.Text = string.IsNullOrEmpty(dbHoTen) ? _hoTenTimKiem : dbHoTen;
            label1_ID_Tanbinh.Text = "Số hiệu: " + GiaiMaSafe(reader["SoHieu"]?.ToString());

            foreach (var item in _cboMapping)
            {
                var val = reader[item.Key];
                if (val == DBNull.Value || val == null)
                    item.Value.Text = "";
                else
                    item.Value.Text = $"Loại {val}";

                SetComboBoxColor(item.Value);
            }
        }
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
        // ==============================================================
        // HÀM LƯU DỮ LIỆU (KHÔNG BỊ TÁC ĐỘNG KHI CHẠY Ở CHẾ ĐỘ XEM)
        // ==============================================================
        private void kryptonButton1_CapNhat_Click(object sender, EventArgs e)
        {
            if (LuuDanhGiaThiDua())
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
        private bool LuuDanhGiaThiDua()
        {
            try
            {
                using var cn = new SqliteConnection($"Data Source={_csdl4Path}");
                cn.Open();

                List<string> setClauses = new();
                foreach (var k in _cboMapping.Keys)
                {
                    setClauses.Add($"{k}=@{k}");
                }

                string sql = $"UPDATE ThiDuaThang_TanBinh SET {string.Join(", ", setClauses)} WHERE ID=@id";
                using var cmd = new SqliteCommand(sql, cn);
                cmd.Parameters.AddWithValue("@id", _id);

                foreach (var item in _cboMapping)
                {
                    string txt = item.Value.Text;
                    if (string.IsNullOrWhiteSpace(txt))
                    {
                        cmd.Parameters.AddWithValue("@" + item.Key, DBNull.Value);
                    }
                    else
                    {
                        string numberPart = txt.Replace("Loại ", "").Trim();
                        if (int.TryParse(numberPart, out int so))
                            cmd.Parameters.AddWithValue("@" + item.Key, so);
                        else
                            cmd.Parameters.AddWithValue("@" + item.Key, DBNull.Value);
                    }
                }

                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lưu dữ liệu thi đua: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}