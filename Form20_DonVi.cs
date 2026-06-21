using Microsoft.Data.Sqlite;
using System.Data;

namespace PhanMemThiDua2026
{
    public partial class Form20_DonVi : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private int _selectedID = -1;
        private bool isEditing = false;
        // BỔ SUNG BIẾN NÀY LÊN ĐẦU CLASS (Chuẩn hóa kiểu Image)
        private readonly Image _iconClock = Properties.Resources.clock;

        public Form20_DonVi()
        {
            InitializeComponent();
            Load += Form20_DonVi_Load;
            kryptonDataGridView1_DanhSach_DonVi.CellClick += kryptonDataGridView1_DanhSach_DonVi_CellClick;

            // Đăng ký sự kiện tự vẽ giao diện cho ô
            kryptonDataGridView1_DanhSach_DonVi.CellPainting += kryptonDataGridView1_DanhSach_DonVi_CellPainting;
            InitToolTips();
        }

        private void Form20_DonVi_Load(object? sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.AcceptButton = kryptonButton1_Them; // ✅ Enter = Thêm
            TaoBangNeuChuaCo();
            LoadDanhSachDonVi();
            ResetInput();
            textBox_TenDonVi.Focus();
        }

        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Gợi ý thao tác";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;

            var tips = new Dictionary<Control, string>
            {
                { kryptonButton1_Them, "Thêm đơn vị mới" },
                { kryptonButton1_Sua, "Chỉnh sửa ten đơn vị đang chọn" },
                { kryptonButton1_Xoa, "Xóa đơn vị đang chọn" },
                { textBox_TenDonVi, "Nhập tên đơn vị trực thuộc" }
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
CREATE TABLE IF NOT EXISTS DanhSach_DonVi (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    Ten_DonVi TEXT,
    ThoiGian TEXT
);";
            cmd.ExecuteNonQuery();
        }

        // Đưa logic giải mã an toàn về một mối
        private string TryGiaiMa(object value)
        {
            if (value == null || value == DBNull.Value) return string.Empty;
            string s = value.ToString()!.Trim();
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;

            try
            {
                string decoded = BaoMatAES.GiaiMa(s).Trim();
                return string.IsNullOrEmpty(decoded) ? s : decoded;
            }
            catch
            {
                return s;
            }
        }

        private void LoadDanhSachDonVi(int? selectID = null)
        {
            using var cn = new SqliteConnection($"Data Source={_csdl2Path}");
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT ID, Ten_DonVi, ThoiGian FROM DanhSach_DonVi ORDER BY ID ASC";

            var dt = new DataTable();
            dt.Load(cmd.ExecuteReader());

            // Thêm cột STT
            if (!dt.Columns.Contains("STT"))
                dt.Columns.Add("STT", typeof(string));

            // Tạm thời gỡ DataSource để tăng tốc độ nạp/giải mã (tránh Grid vẽ lại liên tục)
            kryptonDataGridView1_DanhSach_DonVi.DataSource = null;

            int stt = 1;
            foreach (DataRow row in dt.Rows)
            {
                string tenDonVi = TryGiaiMa(row["Ten_DonVi"]);
                row["Ten_DonVi"] = tenDonVi;

                string thoiGian = TryGiaiMa(row["ThoiGian"]);

                // MẸO: Thêm khoảng trắng đầu chuỗi để tự tạo khoảng trống cho Icon (Tránh bị lệch viền Grid)
                if (DateTime.TryParse(thoiGian, out DateTime dtParsed))
                {
                    row["ThoiGian"] = "      " + dtParsed.ToString("dd-MM-yyyy HH:mm:ss");
                }
                else
                {
                    row["ThoiGian"] = "      " + thoiGian;
                }

                row["STT"] = stt++.ToString();
            }
            // Gán lại DataSource sau khi đã giải mã toàn bộ bảng
            kryptonDataGridView1_DanhSach_DonVi.DataSource = dt;
            // Cập nhật số lượng lên Label ngay tại đây thay vì gọi hàm riêng để đỡ tốn 1 lần truy vấn
            CapNhatLabelTongCongDonVi(dt.Rows.Count);
            // Cấu hình giao diện Grid
            CauHinhGiaoDienGrid(selectID);
        }

        private void CauHinhGiaoDienGrid(int? selectID)
        {
            if (kryptonDataGridView1_DanhSach_DonVi.Columns.Count == 0) return;

            // ===== IN ĐẬM + CĂN GIỮA TIÊU ĐỀ =====
            var headerStyle = kryptonDataGridView1_DanhSach_DonVi.ColumnHeadersDefaultCellStyle;
            headerStyle.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 10f, FontStyle.Bold);
            headerStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // ===== Fill và chia tỷ lệ thủ công =====
            kryptonDataGridView1_DanhSach_DonVi.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            kryptonDataGridView1_DanhSach_DonVi.DefaultCellStyle.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 9f);

            kryptonDataGridView1_DanhSach_DonVi.Columns["STT"].FillWeight = 20;
            kryptonDataGridView1_DanhSach_DonVi.Columns["Ten_DonVi"].FillWeight = 50;
            kryptonDataGridView1_DanhSach_DonVi.Columns["ThoiGian"].FillWeight = 30;

            kryptonDataGridView1_DanhSach_DonVi.Columns["STT"].HeaderText = "STT";
            kryptonDataGridView1_DanhSach_DonVi.Columns["Ten_DonVi"].HeaderText = "Tên đơn vị";
            kryptonDataGridView1_DanhSach_DonVi.Columns["ThoiGian"].HeaderText = "Thời gian";

            // ===== Ẩn ID và set DisplayIndex =====
            if (kryptonDataGridView1_DanhSach_DonVi.Columns.Contains("ID"))
                kryptonDataGridView1_DanhSach_DonVi.Columns["ID"].Visible = false;

            kryptonDataGridView1_DanhSach_DonVi.Columns["STT"].DisplayIndex = 0;
            kryptonDataGridView1_DanhSach_DonVi.Columns["Ten_DonVi"].DisplayIndex = 1;
            kryptonDataGridView1_DanhSach_DonVi.Columns["ThoiGian"].DisplayIndex = 2;

            kryptonDataGridView1_DanhSach_DonVi.AllowUserToAddRows = false;
            kryptonDataGridView1_DanhSach_DonVi.ReadOnly = true;
            kryptonDataGridView1_DanhSach_DonVi.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // ===== Chọn dòng =====
            kryptonDataGridView1_DanhSach_DonVi.ClearSelection();
            if (selectID.HasValue)
            {
                foreach (DataGridViewRow row in kryptonDataGridView1_DanhSach_DonVi.Rows)
                {
                    if (row.Cells["ID"].Value != null && Convert.ToInt32(row.Cells["ID"].Value) == selectID.Value)
                    {
                        row.Selected = true;
                        kryptonDataGridView1_DanhSach_DonVi.FirstDisplayedScrollingRowIndex = row.Index;
                        break;
                    }
                }
            }
            else if (kryptonDataGridView1_DanhSach_DonVi.Rows.Count > 0)
            {
                int last = kryptonDataGridView1_DanhSach_DonVi.Rows.Count - 1;
                kryptonDataGridView1_DanhSach_DonVi.Rows[last].Selected = true;
                kryptonDataGridView1_DanhSach_DonVi.FirstDisplayedScrollingRowIndex = last;
            }
        }

        private void kryptonDataGridView1_DanhSach_DonVi_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = kryptonDataGridView1_DanhSach_DonVi.Rows[e.RowIndex];
            _selectedID = Convert.ToInt32(row.Cells["ID"].Value);

            // Xóa đi các khoảng trắng ở đầu đã chèn vào lúc hiển thị trước khi gán lên ô nhập liệu
            string tenDonVi = row.Cells["Ten_DonVi"].Value?.ToString() ?? "";
            textBox_TenDonVi.Text = tenDonVi.Trim();

            textBox_TenDonVi.Focus();
            textBox_TenDonVi.SelectAll();
            kryptonButton1_Sua.Text = "Lưu";
            isEditing = true;
            this.AcceptButton = kryptonButton1_Sua; // ✅ Enter = Lưu
        }

        private void ResetInput()
        {
            _selectedID = -1;
            textBox_TenDonVi.Clear();
            kryptonButton1_Sua.Text = "Sửa";
            isEditing = false;
            this.AcceptButton = kryptonButton1_Them; // ✅ Enter = Thêm
        }

        private void kryptonButton1_Them_Click(object sender, EventArgs e)
        {
            string tenDonVi = textBox_TenDonVi.Text.Trim();
            if (string.IsNullOrWhiteSpace(tenDonVi))
            {
                MessageBox.Show(
                    "Chưa nhập tên đơn vị để lưu vào cơ sở dữ liệu!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                textBox_TenDonVi.Focus();
                return;
            }

            using var cn = new SqliteConnection($"Data Source={_csdl2Path}");
            cn.Open();

            // Kiểm tra tồn tại
            using var check = cn.CreateCommand();
            check.CommandText = "SELECT ID, Ten_DonVi, ThoiGian FROM DanhSach_DonVi";
            using var reader = check.ExecuteReader();
            while (reader.Read())
            {
                string existingTen = TryGiaiMa(reader.GetString(1));
                if (string.Equals(existingTen, tenDonVi, StringComparison.OrdinalIgnoreCase))
                {
                    int existingID = reader.GetInt32(0);
                    string tg = TryGiaiMa(reader.GetString(2));
                    MessageBox.Show(
                        $"Đơn vị '{tenDonVi}' (ID {existingID}) đã được tạo ngày {tg}.\nBạn không thể thêm trùng tên, nhưng có thể sửa tên!",
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
            cmd.CommandText = "INSERT INTO DanhSach_DonVi (Ten_DonVi, ThoiGian) VALUES (@ten, @time)";
            cmd.Parameters.AddWithValue("@ten", BaoMatAES.MaHoa(tenDonVi));
            cmd.Parameters.AddWithValue("@time", BaoMatAES.MaHoa(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            cmd.ExecuteNonQuery();

            // Load lại dữ liệu và chọn dòng cuối
            LoadDanhSachDonVi();
            ResetInput();
        }

        private void kryptonButton1_Sua_Click(object sender, EventArgs e)
        {
            if (!isEditing || _selectedID < 0)
            {
                MessageBox.Show("Bạn chưa chọn đơn vị để sửa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tenDonViMoi = textBox_TenDonVi.Text.Trim();
            if (string.IsNullOrWhiteSpace(tenDonViMoi))
            {
                MessageBox.Show("Chưa nhập tên đơn vị!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var cn = new SqliteConnection($"Data Source={_csdl2Path}");
            cn.Open();

            // Kiểm tra trùng tên
            using var check = cn.CreateCommand();
            check.CommandText = "SELECT ID, Ten_DonVi FROM DanhSach_DonVi WHERE ID <> @id";
            check.Parameters.AddWithValue("@id", _selectedID);
            using var reader = check.ExecuteReader();
            while (reader.Read())
            {
                string existingTen = TryGiaiMa(reader.GetString(1));
                if (string.Equals(existingTen, tenDonViMoi, StringComparison.OrdinalIgnoreCase))
                {
                    int existingID = reader.GetInt32(0);
                    MessageBox.Show(
                        $"Đơn vị '{tenDonViMoi}' (ID {existingID}) đã tồn tại.\nBạn không thể lưu trùng tên!",
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
            cmd.CommandText = "UPDATE DanhSach_DonVi SET Ten_DonVi=@ten, ThoiGian=@time WHERE ID=@id";
            cmd.Parameters.AddWithValue("@ten", BaoMatAES.MaHoa(tenDonViMoi));
            cmd.Parameters.AddWithValue("@time", BaoMatAES.MaHoa(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            cmd.Parameters.AddWithValue("@id", _selectedID);
            cmd.ExecuteNonQuery();

            // Load lại dữ liệu, cập nhật label và reset input
            LoadDanhSachDonVi(_selectedID);
            ResetInput();
        }

        private void kryptonButton1_Xoa_Click(object sender, EventArgs e)
        {
            if (!isEditing || _selectedID < 0)
            {
                MessageBox.Show("Bạn chưa chọn đơn vị để xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var cn = new SqliteConnection($"Data Source={_csdl2Path}");
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "DELETE FROM DanhSach_DonVi WHERE ID=@id";
            cmd.Parameters.AddWithValue("@id", _selectedID);
            cmd.ExecuteNonQuery();

            LoadDanhSachDonVi();
            ResetInput();
        }

        private void CapNhatLabelTongCongDonVi(int soLuong)
        {
            try
            {
                // Lấy tên tiểu đoàn từ module
                string tenTieuDoan = Module_TaiKhoan.XacDinhTenTieuDoan();
                if (!string.IsNullOrWhiteSpace(tenTieuDoan))
                {
                    tenTieuDoan = char.ToUpper(tenTieuDoan[0]) + tenTieuDoan.Substring(1).ToLower();
                }

                label_tongCongDonVi.Text = $"Tổng cộng đơn vị trực thuộc {tenTieuDoan}: {soLuong} đơn vị";
            }
            catch
            {
                label_tongCongDonVi.Text = $"Tổng cộng đơn vị trực thuộc: {soLuong} đơn vị";
            }
        }
        // XỬ LÝ VẼ IMAGE ĐỒNG HỒ VÀO CỘT THỜI GIAN (PHIÊN BẢN HOÀN HẢO CHO KRYPTON)
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