

using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhanMemThiDua2026
{
      
    public partial class Form47_DonViTrucThuoc : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private int _selectedId = -1; // Lưu ID dòng đang chọn trên DataGridView
        // ⭐ KHO CHỨA DỮ LIỆU CHO VIRTUAL MODE
        private List<DonViDTO> _danhSachDonVi = new List<DonViDTO>();
        public Form47_DonViTrucThuoc()
        {
            InitializeComponent();

            // Cấu hình chống giật lag lưới
            typeof(DataGridView).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(kryptonDataGridView1, true, null);
            // ⭐ 2. BẬT VIRTUAL MODE VÀ GẮN SỰ KIỆN CELLVALUENEEDED
            kryptonDataGridView1.VirtualMode = true;
            kryptonDataGridView1.CellValueNeeded += KryptonDataGridView1_CellValueNeeded;
        }

        private async void Form47_DonViTrucThuoc_Load(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;

            try
            {
                // Chỉ gọi 1 lần khi Form mở
                CauHinhGiaoDienGrid();

                await KhoiTaoBangAndDuLieuMauAsync();

                await LoadDuLieuLenGridAsync();

                kryptonDataGridView1.SelectionChanged -= KryptonDataGridView1_SelectionChanged;
                kryptonDataGridView1.SelectionChanged += KryptonDataGridView1_SelectionChanged;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }
        // ⭐ 4. HÀM CUNG CẤP DỮ LIỆU ĐỘNG CHO LƯỚI
        private void KryptonDataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            // Kiểm tra an toàn
            if (e.RowIndex < 0 || e.RowIndex >= _danhSachDonVi.Count) return;

            var item = _danhSachDonVi[e.RowIndex];
            string colName = kryptonDataGridView1.Columns[e.ColumnIndex].Name;

            switch (colName)
            {
                case "ID": e.Value = item.ID; break;
                case "STT": e.Value = item.STT; break;
                case "TenDonVi": e.Value = item.TenDonVi; break;
                case "KyHieu": e.Value = item.KyHieu; break;
                case "ThoiGian": e.Value = item.ThoiGian; break;
            }
        }
        #region KHOI TAO DATABASE & DU LIEU MAU
        private async Task LoadDuLieuLenGridAsync()
        {
            if (!File.Exists(_csdl2Path)) return;

            var dsMoi = new List<DonViDTO>();

            await Task.Run(() =>
            {
                using var conn = new SqliteConnection($"Data Source={_csdl2Path};Mode=ReadOnly");
                conn.Open();

                using var cmd = new SqliteCommand("SELECT ID, STT, TenDonVi, KyHieu, ThoiGian FROM DanhSachDonVi_CapTrucThuoc ORDER BY STT ASC, ID ASC", conn);
                using var rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    dsMoi.Add(new DonViDTO
                    {
                        ID = rd.GetInt32(0),
                        STT = rd.IsDBNull(1) ? 0 : rd.GetInt32(1),
                        TenDonVi = GiaiMaSafe(rd.IsDBNull(2) ? "" : rd.GetString(2)),
                        KyHieu = GiaiMaSafe(rd.IsDBNull(3) ? "" : rd.GetString(3)),
                        ThoiGian = rd.IsDBNull(4) ? "" : rd.GetString(4)
                    });
                }
            });

            // Gán dữ liệu vào kho RAM
            _danhSachDonVi = dsMoi;

            kryptonDataGridView1.SuspendLayout();

            // Nếu lưới chưa có cột, ta tạo cột cho nó
            if (kryptonDataGridView1.Columns.Count == 0)
            {
                kryptonDataGridView1.Columns.Add("ID", "ID");
                kryptonDataGridView1.Columns.Add("STT", "STT");
                kryptonDataGridView1.Columns.Add("TenDonVi", "Tên đơn vị trực thuộc");
                kryptonDataGridView1.Columns.Add("KyHieu", "Ký hiệu");
                kryptonDataGridView1.Columns.Add("ThoiGian", "Thời gian");

                kryptonDataGridView1.Columns["ID"].Visible = false;

                CauHinhCotGrid("STT", "STT", 60, DataGridViewContentAlignment.MiddleCenter);
                CauHinhCotGrid("TenDonVi", "Tên đơn vị trực thuộc", 250, DataGridViewContentAlignment.MiddleLeft);
                CauHinhCotGrid("KyHieu", "Ký hiệu", 120, DataGridViewContentAlignment.MiddleCenter);
                CauHinhCotGrid("ThoiGian", "Thời gian", 150, DataGridViewContentAlignment.MiddleCenter);

                kryptonDataGridView1.Columns["STT"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                kryptonDataGridView1.Columns["KyHieu"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                kryptonDataGridView1.Columns["ThoiGian"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                kryptonDataGridView1.Columns["TenDonVi"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            // ⭐ CỐT LÕI CỦA VIRTUAL MODE: Báo cho lưới biết có bao nhiêu dòng để nó vẽ
            kryptonDataGridView1.RowCount = _danhSachDonVi.Count;

            kryptonDataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            kryptonDataGridView1.MultiSelect = false;
            kryptonDataGridView1.ReadOnly = true;

            kryptonDataGridView1.ResumeLayout();
            kryptonDataGridView1.Invalidate(); // Ép vẽ lại toàn bộ
            kryptonDataGridView1.ClearSelection();

            XoaTrangOInput();
            CapNhatTongCongDonViLabel();
        }
        #endregion

        #region LOAD DỮ LIỆU LÊN LƯỚI
        private async Task KhoiTaoBangAndDuLieuMauAsync()
        {
            if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path))
                return;

            await Task.Run(() =>
            {
                using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                conn.Open();

                const string sqlCreate = @"
                CREATE TABLE IF NOT EXISTS DanhSachDonVi_CapTrucThuoc
                (
                    ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    STT INTEGER,
                    TenDonVi TEXT,
                    KyHieu TEXT,
                    ThoiGian TEXT
                );";

                using var cmd = new SqliteCommand(sqlCreate, conn);
                cmd.ExecuteNonQuery();
            });
        }
        private void CauHinhCotGrid(
        string colName,
        string headerText,
        int width,
        DataGridViewContentAlignment align)
        {
            if (!kryptonDataGridView1.Columns.Contains(colName))
                return;

            var col = kryptonDataGridView1.Columns[colName];

            col.HeaderText = headerText;

            // Chiều rộng cố định
            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            col.Width = width;
            col.MinimumWidth = width;

            // Căn giữa Header
            col.HeaderCell.Style.Alignment =
                DataGridViewContentAlignment.MiddleCenter;

            // In đậm Header
            col.HeaderCell.Style.Font =
                new Font("Segoe UI", 10F, FontStyle.Bold);

            // Căn dữ liệu
            col.DefaultCellStyle.Alignment = align;

            // Font dữ liệu
            col.DefaultCellStyle.Font =
                new Font("Segoe UI", 10F);

            // Không cho Resize nếu muốn giao diện ổn định
            col.Resizable = DataGridViewTriState.False;
        }
        private void CauHinhGiaoDienGrid()
        {
            var dgv = kryptonDataGridView1;


            // Cấu hình chung

            dgv.SuspendLayout();

            dgv.EnableHeadersVisualStyles = false;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

            dgv.RowHeadersVisible = false;
            dgv.AllowUserToResizeRows = false;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;

            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;

            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgv.RowTemplate.Height = 34;


            // Header

            dgv.ColumnHeadersHeight = 58;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgv.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 10F, FontStyle.Bold);
            // 🟢 Thay bằng màu xám/xanh nhạt rất sạch sẽ giống các website hiện đại
            dgv.ColumnHeadersDefaultCellStyle.BackColor =
                Color.FromArgb(240, 244, 248);
            // 🟢 Chữ đổi thành màu đen than đậm, sắc nét, đọc cực kỳ rõ
            dgv.ColumnHeadersDefaultCellStyle.ForeColor =
                Color.FromArgb(40, 40, 40);
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor =
                Color.FromArgb(240, 244, 248);
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor =
                Color.FromArgb(40, 40, 40);

            // Cell

            dgv.DefaultCellStyle.Font =
                new Font("Segoe UI", 10F, FontStyle.Regular);

            dgv.DefaultCellStyle.BackColor = Color.White;

            dgv.DefaultCellStyle.ForeColor = Color.Black;

            dgv.DefaultCellStyle.SelectionBackColor =
                Color.FromArgb(220, 240, 255);

            dgv.DefaultCellStyle.SelectionForeColor =
                Color.Black;

            dgv.DefaultCellStyle.Padding =
                new Padding(3);


            // Xen kẽ màu dòng

            dgv.AlternatingRowsDefaultCellStyle.BackColor =
                Color.FromArgb(248, 250, 252);


            // GridLine

            dgv.GridColor =
                Color.FromArgb(225, 230, 235);

            dgv.BackgroundColor = Color.White;

            dgv.ResumeLayout();
        }
        #endregion
        #region SỰ KIỆN CHỌN DÒNG TRÊN LƯỚI & ĐỔ DỮ LIỆU RA 2 TEXTBOX
  
        private void textBox_TenDonVi_TextChanged(object sender, EventArgs e)
        {
            int pos = textBox_TenDonVi.SelectionStart;
            textBox_TenDonVi.Text = textBox_TenDonVi.Text.ToUpper();
            textBox_TenDonVi.SelectionStart = pos;
        }

        private void kryptonTextBox_KyHieuDonVi_TextChanged(object sender, EventArgs e)
        {
            int pos = kryptonTextBox_KyHieuDonVi.SelectionStart;
            kryptonTextBox_KyHieuDonVi.Text = kryptonTextBox_KyHieuDonVi.Text.ToUpper();
            kryptonTextBox_KyHieuDonVi.SelectionStart = pos;
        }


        #endregion

        #region THAO TÁC C.R.U.D (THÊM - SỬA/LƯU - XÓA)
        // 🟢 THÊM MỚI ĐƠN VỊ
        // 🟢 THÊM MỚI ĐƠN VỊ
        private async void kryptonButton1_Them_Click(object sender, EventArgs e)
        {
            string tenDonVi = textBox_TenDonVi.Text.Trim();
            string kyHieu = kryptonTextBox_KyHieuDonVi.Text.Trim();

            if (string.IsNullOrWhiteSpace(tenDonVi))
            {
                MessageBox.Show("Vui lòng nhập tên đơn vị cần thêm!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox_TenDonVi.Focus();
                return;
            }

            // ⭐ CHỐT CHẶN RÀNG BUỘC TRÙNG LẶP: 
            // idBoQua = -1 nghĩa là quét toàn bộ danh sách hiện có, nếu trùng là Return chặn lại luôn.
            if (KiemTraTrungLap(tenDonVi, kyHieu, -1))
            {
                return;
            }

            try
            {
                await Task.Run(() =>
                {
                    using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                    conn.Open();

                    int maxStt = 0;
                    using (var cmdStt = new SqliteCommand("SELECT MAX(STT) FROM DanhSachDonVi_CapTrucThuoc", conn))
                    {
                        var res = cmdStt.ExecuteScalar();
                        if (res != DBNull.Value && res != null) maxStt = Convert.ToInt32(res);
                    }

                    string sqlInsert = @"INSERT INTO DanhSachDonVi_CapTrucThuoc (STT, TenDonVi, KyHieu, ThoiGian) 
                                         VALUES (@stt, @ten, @kyhieu, @thoigian)";

                    using var cmd = new SqliteCommand(sqlInsert, conn);
                    cmd.Parameters.AddWithValue("@stt", maxStt + 1);
                    cmd.Parameters.AddWithValue("@ten", BaoMatAES.MaHoa(tenDonVi));
                    cmd.Parameters.AddWithValue("@kyhieu", BaoMatAES.MaHoa(kyHieu));
                    cmd.Parameters.AddWithValue("@thoigian", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));

                    cmd.ExecuteNonQuery();
                });

                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Thêm đơn vị trực thuộc", tenDonVi);
                await LoadDuLieuLenGridAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi thêm đơn vị: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // 🟡 SỬA / LƯU CẬP NHẬT ĐƠN VỊ
        // 🟡 SỬA / LƯU CẬP NHẬT ĐƠN VỊ
        private async void kryptonButton1_Sua_Click(object sender, EventArgs e)
        {
            if (_selectedId <= 0)
            {
                MessageBox.Show("Vui lòng chọn đơn vị cần xóa trên danh sách!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tenDonViMoi = textBox_TenDonVi.Text.Trim();
            string kyHieuMoi = kryptonTextBox_KyHieuDonVi.Text.Trim();

            if (string.IsNullOrWhiteSpace(tenDonViMoi))
            {
                MessageBox.Show("Tên đơn vị không được để trống!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox_TenDonVi.Focus();
                return;
            }

            // ⭐ CHỐT CHẶN RÀNG BUỘC TRÙNG LẶP: 
            // Truyền _selectedId vào để thuật toán BỎ QUA chính cái dòng đang sửa (tránh việc nó báo trùng với chính nó).
            if (KiemTraTrungLap(tenDonViMoi, kyHieuMoi, _selectedId))
            {
                return;
            }

            try
            {
                await Task.Run(() =>
                {
                    using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                    conn.Open();

                    string sqlUpdate = @"UPDATE DanhSachDonVi_CapTrucThuoc 
                                         SET TenDonVi = @ten, KyHieu = @kyhieu, ThoiGian = @thoigian 
                                         WHERE ID = @id";

                    using var cmd = new SqliteCommand(sqlUpdate, conn);
                    cmd.Parameters.AddWithValue("@ten", BaoMatAES.MaHoa(tenDonViMoi));
                    cmd.Parameters.AddWithValue("@kyhieu", BaoMatAES.MaHoa(kyHieuMoi));
                    cmd.Parameters.AddWithValue("@thoigian", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                    cmd.Parameters.AddWithValue("@id", _selectedId);

                    cmd.ExecuteNonQuery();
                });

                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Cập nhật đơn vị trực thuộc", tenDonViMoi);
                await LoadDuLieuLenGridAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi cập nhật đơn vị: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 🔴 XÓA ĐƠN VỊ
        private void DoiTenNutSua(bool laDangChonSua)
        {
            if (kryptonButton1_Sua != null)
            {
                kryptonButton1_Sua.Values.Text = laDangChonSua ? "Lưu" : "Sửa";
            }
        }

        // 1. TỐI ƯU HÀM CHỌN DÒNG
        private void KryptonDataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (kryptonDataGridView1.CurrentRow != null && kryptonDataGridView1.CurrentRow.Index >= 0)
            {
                int idx = kryptonDataGridView1.CurrentRow.Index;
                if (idx < _danhSachDonVi.Count) // Truy xuất trực tiếp từ RAM
                {
                    var item = _danhSachDonVi[idx];
                    _selectedId = item.ID;

                    textBox_TenDonVi.Text = item.TenDonVi;
                    kryptonTextBox_KyHieuDonVi.Text = item.KyHieu;

                    DoiTenNutSua(laDangChonSua: true);
                    return;
                }
            }
            XoaTrangOInput();
        }

        // 2. TỐI ƯU HÀM KIỂM TRA TRÙNG LẶP (Chạy trong chớp mắt vì quét trên RAM)
        private bool KiemTraTrungLap(string tenDonVi, string kyHieu, int idBoQua = -1)
        {
            foreach (var item in _danhSachDonVi)
            {
                if (item.ID == idBoQua) continue;

                if (item.TenDonVi.Equals(tenDonVi.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show($"Tên đơn vị [{tenDonVi.Trim()}] đã tồn tại trong hệ thống!\nVui lòng nhập tên khác.",
                                    "Cảnh báo trùng lặp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox_TenDonVi.Focus();
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(kyHieu) &&
                    !string.IsNullOrWhiteSpace(item.KyHieu) &&
                    item.KyHieu.Equals(kyHieu.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show($"Ký hiệu [{kyHieu.Trim()}] đã được sử dụng cho đơn vị khác!\nVui lòng nhập ký hiệu khác.",
                                    "Cảnh báo trùng lặp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    kryptonTextBox_KyHieuDonVi.Focus();
                    return true;
                }
            }
            return false;
        }

        // 3. TỐI ƯU HÀM CẬP NHẬT LABEL ĐẾM
        private void CapNhatTongCongDonViLabel()
        {
            if (label_tongCongDonViTrucThuoc == null) return;

            int count = _danhSachDonVi.Count; // Khỏi đếm trên lưới, đếm trên List

            if (count > 0)
            {
                label_tongCongDonViTrucThuoc.Text = $"Tổng các đơn vị trực thuộc: {count} đơn vị";
                label_tongCongDonViTrucThuoc.Visible = true;
            }
            else
            {
                label_tongCongDonViTrucThuoc.Text = string.Empty;
                label_tongCongDonViTrucThuoc.Visible = false;
            }
        }
        private async void kryptonButton1_Xoa_Click(object sender, EventArgs e)
        {
            if (_selectedId <= 0)
            {
                MessageBox.Show("Vui lòng chọn đơn vị cần xóa trên danh sách!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tenDonVi = textBox_TenDonVi.Text.Trim();

            var dr = MessageBox.Show($"Bạn có chắc chắn muốn xóa đơn vị [{tenDonVi}] khỏi hệ thống không?",
                                     "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr != DialogResult.Yes) return;

            try
            {
                await Task.Run(() =>
                {
                    using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                    conn.Open();

                    using var cmd = new SqliteCommand("DELETE FROM DanhSachDonVi_CapTrucThuoc WHERE ID = @id", conn);
                    cmd.Parameters.AddWithValue("@id", _selectedId);
                    cmd.ExecuteNonQuery();
                });

                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Xóa đơn vị trực thuộc", tenDonVi);
                await LoadDuLieuLenGridAsync();
                // MessageBox.Show("Đã xóa đơn vị thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xóa đơn vị: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region HÀM TIỆN ÍCH
        private void XoaTrangOInput()
        {
            _selectedId = -1;
            textBox_TenDonVi.Clear();
            kryptonTextBox_KyHieuDonVi.Clear();
            DoiTenNutSua(laDangChonSua: false);
        }
        private string GiaiMaSafe(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return "";
            try
            {
                string dec = BaoMatAES.GiaiMa(val);
                return string.IsNullOrEmpty(dec) ? val : dec;
            }
            catch { return val; }
        }

        #endregion
    }

    // ⭐ 1. TẠO LỚP DTO LƯU TRỮ DỮ LIỆU
    public class DonViDTO
    {
        public int ID { get; set; }
        public int STT { get; set; }
        public string TenDonVi { get; set; } = string.Empty;
        public string KyHieu { get; set; } = string.Empty;
        public string ThoiGian { get; set; } = string.Empty;
    }
}
