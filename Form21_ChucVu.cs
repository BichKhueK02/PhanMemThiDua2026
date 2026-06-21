using Microsoft.Data.Sqlite;
using System.Data;

namespace PhanMemThiDua2026
{
    public partial class Form21_ChucVu : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private int _selectedID = -1;
        private bool isEditing = false;
        // ===== 1. BỔ SUNG BIẾN CHỨA ẢNH TOÀN CỤC =====
        private readonly Image _iconClock = Properties.Resources.clock;
        public Form21_ChucVu()
        {
            InitializeComponent();
            // Ẩn icon trên Taskbar
            this.ShowInTaskbar = false;
            Load += Form19_ChucVu_Load; // Vẫn giữ nguyên tên hàm Load của đồng chí
            kryptonDataGridView1_DanhSach_ChucVu.CellClick += kryptonDataGridView1_DanhSach_ChucVu_CellClick;

            // ===== 2. BỔ SUNG SỰ KIỆN VẼ GIAO DIỆN =====
            kryptonDataGridView1_DanhSach_ChucVu.CellPainting += kryptonDataGridView1_DanhSach_DonVi_CellPainting;

            InitToolTips();
        }
        private void Form19_ChucVu_Load(object? sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.AcceptButton = kryptonButton1_Them; // ✅ Enter = Thêm
            TaoBangNeuChuaCo();
            LoadDanhSachChucVu();
            CapNhatLabelTongCongChucVu();
            ResetInput();
            textBox_TenChucVu.Focus();
        }
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Gợi ý thao tác";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;

            var tips = new Dictionary<Control, string>
            {
                { kryptonButton1_Them, "Thêm chức vụ CBCS mới trong đơn vị" },
                { kryptonButton1_Sua,  "Chỉnh sửa chức vụ CBCS đang được chọn" },
                { kryptonButton1_Xoa,  "Xóa chức vụ CBCS đang được chọn" },
                { textBox_TenChucVu,   "Nhập tên chức vụ CBCS trong đơn vị" }
            };

            foreach (var tip in tips)
            {
                if (tip.Key != null) // an toàn khi refactor / ẩn control
                    toolTip1.SetToolTip(tip.Key, tip.Value);
            }
        }
        private void TaoBangNeuChuaCo()
        {
            if (!File.Exists(_csdl2Path)) return;

            using var cn = new SqliteConnection($"Data Source={_csdl2Path}");
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS DanhSach_ChucVu (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    Ten_ChucVu TEXT,
    ThoiGian TEXT
);";
            cmd.ExecuteNonQuery();
        }
        // Đưa logic giải mã an toàn về một mối, bỏ Convert.FromBase64String
        private string TryGiaiMa(object value)
        {
            if (value == null || value == DBNull.Value) return string.Empty;
            string s = value.ToString()!.Trim();
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;

            try
            {
                // Gọi thẳng bộ giải mã. BaoMatAES tự biết xử lý chuỗi "v2|"
                string decoded = BaoMatAES.GiaiMa(s).Trim();
                return string.IsNullOrEmpty(decoded) ? s : decoded;
            }
            catch
            {
                return s;
            }
        }
        private void LoadDanhSachChucVu(int? selectID = null)
        {
            using var cn = new SqliteConnection($"Data Source={_csdl2Path}");
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT ID, Ten_ChucVu, ThoiGian FROM DanhSach_ChucVu ORDER BY ID ASC";

            var dt = new DataTable();
            dt.Load(cmd.ExecuteReader());

            // Thêm cột STT nếu chưa có
            if (!dt.Columns.Contains("STT"))
                dt.Columns.Add("STT", typeof(string));

            // Tạm ngắt DataSource để tránh Grid tự vẽ lại liên tục khi nạp dữ liệu
            kryptonDataGridView1_DanhSach_ChucVu.DataSource = null;

            int stt = 1;
            foreach (DataRow row in dt.Rows)
            {
                // ===== Giải mã bằng hàm TryGiaiMa đã tối ưu =====
                row["Ten_ChucVu"] = TryGiaiMa(row["Ten_ChucVu"]);

                string tg = TryGiaiMa(row["ThoiGian"]);
                if (DateTime.TryParse(tg, out DateTime dtParsed))
                    row["ThoiGian"] = dtParsed.ToString("dd-MM-yyyy HH:mm:ss");
                else
                    row["ThoiGian"] = tg;

                row["STT"] = stt++.ToString();
            }

            // Gán DataSource sau khi đã map xong dữ liệu sạch
            kryptonDataGridView1_DanhSach_ChucVu.DataSource = dt;

            // Chuyển phần cấu hình UI ra một hàm riêng cho sạch code
            CauHinhGiaoDienGrid(selectID);
        }
        private void CauHinhGiaoDienGrid(int? selectID)
        {
            if (kryptonDataGridView1_DanhSach_ChucVu.Columns.Count == 0) return;

            // ===== Fill và chia tỷ lệ thủ công =====
            kryptonDataGridView1_DanhSach_ChucVu.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            kryptonDataGridView1_DanhSach_ChucVu.Columns["STT"].FillWeight = 20;
            kryptonDataGridView1_DanhSach_ChucVu.Columns["Ten_ChucVu"].FillWeight = 50;
            kryptonDataGridView1_DanhSach_ChucVu.Columns["ThoiGian"].FillWeight = 30;

            // ===== Thiết lập header =====
            kryptonDataGridView1_DanhSach_ChucVu.Columns["STT"].HeaderText = "STT";
            kryptonDataGridView1_DanhSach_ChucVu.Columns["Ten_ChucVu"].HeaderText = "Tên chức vụ";
            kryptonDataGridView1_DanhSach_ChucVu.Columns["ThoiGian"].HeaderText = "Thời gian";

            // 🔥 THÊM DÒNG NÀY ĐỂ SỬA LỖI ĐÈ CHỮ 🔥
            // Thiết lập lề trái (Padding Left) 22 pixel để nhường chỗ cho Icon 16px + lề
            kryptonDataGridView1_DanhSach_ChucVu.Columns["ThoiGian"].DefaultCellStyle.Padding = new Padding(22, 0, 0, 0);

            // ===== FONT HEADER (IN ĐẬM – AN TOÀN MỌI MÁY) =====
            var headerStyle = kryptonDataGridView1_DanhSach_ChucVu.ColumnHeadersDefaultCellStyle;
            headerStyle.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 10f, FontStyle.Bold);
            headerStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // ===== FONT NỘI DUNG =====
            kryptonDataGridView1_DanhSach_ChucVu.DefaultCellStyle.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 9f);

            // ===== Ẩn ID và set DisplayIndex =====
            if (kryptonDataGridView1_DanhSach_ChucVu.Columns.Contains("ID"))
                kryptonDataGridView1_DanhSach_ChucVu.Columns["ID"].Visible = false;

            kryptonDataGridView1_DanhSach_ChucVu.Columns["STT"].DisplayIndex = 0;
            kryptonDataGridView1_DanhSach_ChucVu.Columns["Ten_ChucVu"].DisplayIndex = 1;
            kryptonDataGridView1_DanhSach_ChucVu.Columns["ThoiGian"].DisplayIndex = 2;

            // ===== Các thiết lập chung =====
            kryptonDataGridView1_DanhSach_ChucVu.AllowUserToAddRows = false;
            kryptonDataGridView1_DanhSach_ChucVu.ReadOnly = true;
            kryptonDataGridView1_DanhSach_ChucVu.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // ===== Chọn dòng =====
            kryptonDataGridView1_DanhSach_ChucVu.ClearSelection();
            if (selectID.HasValue)
            {
                foreach (DataGridViewRow row in kryptonDataGridView1_DanhSach_ChucVu.Rows)
                {
                    if (row.Cells["ID"].Value != null && Convert.ToInt32(row.Cells["ID"].Value) == selectID.Value)
                    {
                        row.Selected = true;
                        kryptonDataGridView1_DanhSach_ChucVu.FirstDisplayedScrollingRowIndex = row.Index;
                        break;
                    }
                }
            }
            else if (kryptonDataGridView1_DanhSach_ChucVu.Rows.Count > 0)
            {
                int last = kryptonDataGridView1_DanhSach_ChucVu.Rows.Count - 1;
                kryptonDataGridView1_DanhSach_ChucVu.Rows[last].Selected = true;
                kryptonDataGridView1_DanhSach_ChucVu.FirstDisplayedScrollingRowIndex = last;
            }
        }
        private void kryptonDataGridView1_DanhSach_ChucVu_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = kryptonDataGridView1_DanhSach_ChucVu.Rows[e.RowIndex];
            _selectedID = Convert.ToInt32(row.Cells["ID"].Value);
            textBox_TenChucVu.Text = row.Cells["Ten_ChucVu"].Value?.ToString();

            // UX: Chọn toàn bộ text
            textBox_TenChucVu.Focus();
            textBox_TenChucVu.SelectAll();

            // Đổi nút Sửa thành Lưu
            kryptonButton1_Sua.Text = "Lưu";
            isEditing = true;
            this.AcceptButton = kryptonButton1_Sua; // ✅ Enter = Lưu
        }
        private void ResetInput()
        {
            _selectedID = -1;
            textBox_TenChucVu.Clear();
            kryptonButton1_Sua.Text = "Sửa";
            isEditing = false;
            this.AcceptButton = kryptonButton1_Them; // ✅ Enter = Thêm
        }
        private void CapNhatLabelTongCongChucVu()
        {
            try
            {
                string tenTieuDoan = "";
                using (var cn = new SqliteConnection($"Data Source={_csdl2Path}"))
                {
                    cn.Open();
                    // Lấy TenTieuDoan từ bảng ThongTin
                    using var cmd1 = cn.CreateCommand();
                    cmd1.CommandText = "SELECT TenTieuDoan FROM ThongTin ORDER BY ID DESC LIMIT 1";
                    var result = cmd1.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        string raw = BaoMatAES.GiaiMa(result.ToString().Trim());

                        if (!string.IsNullOrWhiteSpace(raw))
                        {
                            tenTieuDoan = char.ToUpper(raw[0]) + raw.Substring(1).ToLower();
                        }
                    }
                    // Đếm số dòng trong bảng DanhSach_ChucVu
                    using var cmd2 = cn.CreateCommand();
                    cmd2.CommandText = "SELECT COUNT(*) FROM DanhSach_ChucVu";
                    int soLuong = Convert.ToInt32(cmd2.ExecuteScalar());
                    label_tongCongChucVu.Text = string.IsNullOrEmpty(tenTieuDoan)
                        ? $"Tổng cộng: {soLuong} chức vụ"
                        : $"Tổng cộng chức vụ trong {tenTieuDoan}: {soLuong}";
                }
            }
            catch
            {
                label_tongCongChucVu.Text = "Tổng cộng chức vụ: 0 chức vụ";
            }
        }
        private void kryptonButton1_Them_Click(object sender, EventArgs e)
        {
            string tenChucVu = textBox_TenChucVu.Text.Trim();
            if (string.IsNullOrWhiteSpace(tenChucVu))
            {
                MessageBox.Show(
                    "Chưa nhập tên chức vụ để lưu vào cơ sở dữ liệu!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                textBox_TenChucVu.Focus();
                return;
            }

            using var cn = new SqliteConnection($"Data Source={_csdl2Path}");
            cn.Open();

            // Kiểm tra tồn tại
            using var check = cn.CreateCommand();
            check.CommandText = "SELECT ID, Ten_ChucVu, ThoiGian FROM DanhSach_ChucVu";
            using var reader = check.ExecuteReader();
            while (reader.Read())
            {
                string existingTen = TryGiaiMa(reader.GetString(1));
                if (string.Equals(existingTen, tenChucVu, StringComparison.OrdinalIgnoreCase))
                {
                    int existingID = reader.GetInt32(0);
                    string tg = TryGiaiMa(reader.GetString(2));
                    MessageBox.Show(
                        $"Chức vụ '{tenChucVu}' (ID {existingID}) đã được tạo ngày {tg}.\nBạn không thể thêm trùng tên, nhưng có thể sửa tên!",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }
            }
            reader.Close();

            // Thêm mới
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "INSERT INTO DanhSach_ChucVu (Ten_ChucVu, ThoiGian) VALUES (@ten, @time)";
            cmd.Parameters.AddWithValue("@ten", BaoMatAES.MaHoa(tenChucVu));
            cmd.Parameters.AddWithValue("@time", BaoMatAES.MaHoa(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            cmd.ExecuteNonQuery();

            // Load lại dữ liệu và chọn dòng cuối
            LoadDanhSachChucVu();
            CapNhatLabelTongCongChucVu();
            ResetInput();
        }
        private void kryptonButton1_Sua_Click(object sender, EventArgs e)
        {
            if (!isEditing || _selectedID < 0)
            {
                MessageBox.Show("Bạn chưa chọn chức vụ để sửa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tenChucVuMoi = textBox_TenChucVu.Text.Trim();
            if (string.IsNullOrWhiteSpace(tenChucVuMoi))
            {
                MessageBox.Show("Chưa nhập tên chức vụ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var cn = new SqliteConnection($"Data Source={_csdl2Path}");
            cn.Open();

            // Kiểm tra trùng tên
            using var check = cn.CreateCommand();
            check.CommandText = "SELECT ID, Ten_ChucVu FROM DanhSach_ChucVu WHERE ID <> @id";
            check.Parameters.AddWithValue("@id", _selectedID);
            using var reader = check.ExecuteReader();
            while (reader.Read())
            {
                string existingTen = TryGiaiMa(reader.GetString(1));
                if (string.Equals(existingTen, tenChucVuMoi, StringComparison.OrdinalIgnoreCase))
                {
                    int existingID = reader.GetInt32(0);
                    MessageBox.Show(
                        $"Chức vụ '{tenChucVuMoi}' (ID {existingID}) đã tồn tại.\nBạn không thể lưu trùng tên!",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }
            }
            reader.Close();

            // Cập nhật
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "UPDATE DanhSach_ChucVu SET Ten_ChucVu=@ten, ThoiGian=@time WHERE ID=@id";
            cmd.Parameters.AddWithValue("@ten", BaoMatAES.MaHoa(tenChucVuMoi));
            cmd.Parameters.AddWithValue("@time", BaoMatAES.MaHoa(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            cmd.Parameters.AddWithValue("@id", _selectedID);
            cmd.ExecuteNonQuery();

            LoadDanhSachChucVu(_selectedID);
            CapNhatLabelTongCongChucVu();
            ResetInput();
        }
        private void kryptonButton1_Xoa_Click(object sender, EventArgs e)
        {
            if (!isEditing || _selectedID < 0)
            {
                MessageBox.Show("Bạn chưa chọn chức vụ để xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var cn = new SqliteConnection($"Data Source={_csdl2Path}");
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "DELETE FROM DanhSach_ChucVu WHERE ID=@id";
            cmd.Parameters.AddWithValue("@id", _selectedID);
            cmd.ExecuteNonQuery();

            LoadDanhSachChucVu();
            CapNhatLabelTongCongChucVu();
            ResetInput();
        }
        private void kryptonDataGridView1_DanhSach_DonVi_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv == null) return;

            // Chỉ bắt sự kiện vẽ đối với các hàng dữ liệu thuộc cột "ThoiGian"
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgv.Columns[e.ColumnIndex].Name == "ThoiGian")
            {
                // Để Krypton TỰ ĐỘNG VẼ toàn bộ nền, viền và chữ như bình thường.
                // Do ở LoadDanhSachDonVi ta đã thêm "      " (khoảng trắng) vào đầu chuỗi Thời Gian,
                // nên Krypton sẽ tự đẩy chữ sang phải, để lại 1 khoảng trống phía bên trái.

                // Nếu Icon chưa được nạp hoặc không có dữ liệu thì thoát luôn
                if (_iconClock == null || e.Value == null || string.IsNullOrWhiteSpace(e.Value.ToString()))
                    return;

                // TÍNH TOÁN TỌA ĐỘ VÀ ĐÓNG DẤU ICON VÀO KHOẢNG TRỐNG
                int iconSize = 16;
                int paddingLeft = 4; // Căn chỉnh lề trái cho mượt mắt

                int yIcon = e.CellBounds.Y + (e.CellBounds.Height - iconSize) / 2;
                int xIcon = e.CellBounds.X + paddingLeft;

                // In đè cái ảnh lên ô
                e.Graphics.DrawImage(_iconClock, new Rectangle(xIcon, yIcon, iconSize, iconSize));

                // ⚠️ QUAN TRỌNG TỐI CAO: TUYỆT ĐỐI KHÔNG DÙNG e.Handled = true; TẠI ĐÂY
                // Nếu để e.Handled = true, Krypton sẽ tịt luôn, không vẽ viền 3D/Bo góc nữa gây ra lỗi gạch đứt nét.
                // Để trống, Grid sẽ vẽ giao diện của nó TRƯỚC, rồi ta in Icon TRÙNG LÊN TRÊN. Hai bên hòa thuận!
            }
        }

    }
}