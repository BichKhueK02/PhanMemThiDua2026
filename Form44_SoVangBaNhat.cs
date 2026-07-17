using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Microsoft.Data.Sqlite;
using Krypton.Toolkit;
using ClosedXML.Excel; // Nhớ thêm using này
using System.Diagnostics; // Để mở thư mục

namespace PhanMemThiDua2026
{
    public partial class Form44_SoVangBaNhat : Form
    {
        // Sử dụng chung đường dẫn CSDL2 từ module dẫn đường
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;   
        public Form44_SoVangBaNhat()
        {
            InitializeComponent();
            // Trong sự kiện Load hoặc Constructor
            kryptonTextBox1_TimKiemTheoTen.TextChanged += (s, e) => LocDuLieu();
            comboBox_TimKiemDonVi.SelectedIndexChanged += (s, e) => LocDuLieu();
            // ⭐ ĐĂNG KÝ SỰ KIỆN THEO DÕI ẨN HÀN NÚT Ở ĐÂY
            kryptonTextBox1_SoHieu.TextChanged += (s, e) => CapNhatTrangThaiNut();
            kryptonTextBox1_HoVaTen.TextChanged += (s, e) => CapNhatTrangThaiNut();
        }
        // 🌟 THÊM THUỘC TÍNH ĐỘNG: Tự động chọn bảng theo phiên bản hệ thống
        private string TenBangHienTai
        {
            get
            {
                string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
                return phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase)
                    ? "DanhSachTanBinh_SoVangBaNhat"
                    : "DanhSach_SoVangBaNhat";
            }
        }
        private async void Form44_SoVangBaNhat_Load(object sender, EventArgs e)
        {
            Module_MenuChuotPhai.TichHopGiaoDienXanhLa(contextMenuStrip1);
            kryptonTextBox1_SoHieu.ReadOnly = true;
            // 2. Sử dụng StateCommon để thiết lập màu sắc (dùng cho mọi trạng thái)
            kryptonTextBox1_SoHieu.StateCommon.Back.Color1 = Color.LightGreen;
            kryptonTextBox1_SoHieu.StateCommon.Content.Color1 = Color.DarkGreen;
            // Đảm bảo không bị ghi đè bởi Theme mặc định
            kryptonTextBox1_SoHieu.StateCommon.Content.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            kryptonDataGridView1.CellFormatting += kryptonDataGridView1_CellFormatting;
            kryptonDataGridView1.ContextMenuStrip = contextMenuStrip1;
            kryptonDataGridView1.CellMouseClick += kryptonDataGridView1_CellMouseClick;
            // ⭐ THÊM DÒNG NÀY: Đăng ký sự kiện Click vào lưới
            kryptonDataGridView1.CellClick += kryptonDataGridView1_CellClick;
            this.FormClosed += Form44_SoVangBaNhat_FormClosed; // ⭐ Thêm dòng này
            await LoadDuLieuSoVangBaNhatAsync();
            // GỌI Ở ĐÂY: Dữ liệu đã có trong lưới, giờ ta mới trích xuất đơn vị
            CapNhatDanhSachDonVi();
            CapNhatThongKeSoLuong();
            // ⭐ 2. Gọi kiểm tra lúc ban đầu khi load form
            CapNhatTrangThaiNut();
            InitToolTips();
        }
        private void kryptonDataGridView1_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            // Chỉ xử lý nếu là chuột phải và click vào một dòng hợp lệ
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                // 1. Chọn dòng mà người dùng vừa chuột phải
                kryptonDataGridView1.ClearSelection();
                kryptonDataGridView1.Rows[e.RowIndex].Selected = true;

                // 2. Di chuyển con trỏ (CurrentCell) về ô đó
                kryptonDataGridView1.CurrentCell = kryptonDataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];

                // 3. Đổ dữ liệu dùng hàm chung
                FillDataToControls(kryptonDataGridView1.Rows[e.RowIndex]);
            }
        }
        private void FillDataToControls(DataGridViewRow row)
        {
            // ⭐ THEO Ý TƯỞNG CỦA BẠN: Ép RichTextBox về null ngay lập tức để diệt tận gốc chữ cũ
            richTextBox1_ThanhTich.Text = null;

            // Lưu ID để cập nhật/xóa
            kryptonTextBox1_STT.Tag = row.Cells["ID"].Value;

            // Đổ dữ liệu vào đúng các control của Form 44 (dùng ?? "" để ép rỗng nếu null)
            kryptonTextBox1_STT.Text = row.Cells["STT"].Value?.ToString() ?? "";
            kryptonTextBox1_HoVaTen.Text = row.Cells["HoVaTen"].Value?.ToString() ?? "";
            kryptonTextBox1_SoHieu.Text = row.Cells["SoHieu"].Value?.ToString() ?? "";
            kryptonTextBox1_NamSinh.Text = row.Cells["NamSinh"].Value?.ToString() ?? "";
            kryptonTextBox1_QueQuan.Text = row.Cells["QueQuan"].Value?.ToString() ?? "";
            kryptonTextBox1_NgayVaoCAND.Text = row.Cells["NgayVaoCAND"].Value?.ToString() ?? "";
            kryptonTextBox1_CapBac.Text = row.Cells["CapBac"].Value?.ToString() ?? "";
            kryptonTextBox1_ChucVu.Text = row.Cells["ChucVu"].Value?.ToString() ?? "";
            kryptonTextBox1_DonVi.Text = row.Cells["DonVi"].Value?.ToString() ?? "";
            kryptonTextBox1_PhanLoai.Text = row.Cells["PhanLoai"].Value?.ToString() ?? "";
            kryptonTextBox1_GhiChu.Text = row.Cells["GhiChu"].Value?.ToString() ?? "";

            // Đổ dữ liệu vào các control còn lại
            kryptonTextBox1_ThongBaoTrungDoan.Text = row.Cells["ThongBaoTrungDoan"].Value?.ToString() ?? "";
            kryptonTextBox1_SoTTTrongSo.Text = row.Cells["SoTTTrongSo"].Value?.ToString() ?? "";
            kryptonTextBox1_ThangCongNhan.Text = row.Cells["ThangCongNhan"].Value?.ToString() ?? "";

            // ⭐ CHỈ ĐIỀN THÀNH TÍCH MỚI NẾU DÒNG ĐÓ THỰC SỰ CÓ CHỮ
            string thanhTichMoi = row.Cells["ThanhTich"].Value?.ToString() ?? "";
            if (!string.IsNullOrEmpty(thanhTichMoi))
            {
                richTextBox1_ThanhTich.Text = thanhTichMoi;
            }
        }
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Gợi ý thao tác";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;

            // UX: phản hồi nhanh – không gây khó chịu khi rê chuột qua
            toolTip1.InitialDelay = 300;
            toolTip1.AutoPopDelay = 2500;
            toolTip1.ReshowDelay = 100;
            toolTip1.ShowAlways = true;

            // Bản đồ ánh xạ các nút điều khiển và nội dung hướng dẫn tương ứng
            var tips = new Dictionary<Control, string>
    {
        { kryptonButton_LamMoiCacOTimKiem, "Xóa nội dung tìm kiếm và đặt lại các bộ lọc về trạng thái mặc định" },
        { kryptonButton_RefershCSDL, "Tải lại toàn bộ dữ liệu mới nhất từ cơ sở dữ liệu và làm mới trang" },
        { kryptonButton1_XoaCBCS, "Xóa thông tin cán bộ chiến sĩ đang chọn khỏi danh sách Sổ vàng Ba Nhất" },
        { kryptonButton_LuuDataSoVang, "Lưu hoặc cập nhật thông tin dữ liệu Sổ vàng của cán bộ vào cơ sở dữ liệu" },
        { kryptonButton1_Thoat, "Thoát khỏi trang quản lý Sổ vàng và quay trở về giao diện trước đó" }
    };

            foreach (var tip in tips)
            {
                // Kiểm tra an toàn để tránh lỗi NullReference nếu nút chưa được khởi tạo hoặc bị hủy
                if (tip.Key != null && !tip.Key.IsDisposed)
                {
                    toolTip1.SetToolTip(tip.Key, tip.Value);
                }
            }
        }
        public async Task LoadDuLieuSoVangBaNhatAsync()
        {
            if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path)) return;

            try
            {
                if (kryptonDataGridView1 != null && kryptonDataGridView1.DataSource != null)
                {
                    kryptonDataGridView1.DataSource = null;
                }

                DataTable dtSoVang = await Task.Run(async () =>
                {
                    DataTable dt = new DataTable();
                    dt.Columns.Add("ID", typeof(int)); dt.Columns.Add("STT", typeof(int));
                    dt.Columns.Add("HoVaTen", typeof(string)); dt.Columns.Add("SoHieu", typeof(string));
                    dt.Columns.Add("NamSinh", typeof(string)); dt.Columns.Add("QueQuan", typeof(string));
                    dt.Columns.Add("NgayVaoCAND", typeof(string)); dt.Columns.Add("CapBac", typeof(string));
                    dt.Columns.Add("ChucVu", typeof(string)); dt.Columns.Add("DonVi", typeof(string));
                    dt.Columns.Add("PhanLoai", typeof(string)); dt.Columns.Add("GhiChu", typeof(string));
                    dt.Columns.Add("ThanhTich", typeof(string)); dt.Columns.Add("ThongBaoTrungDoan", typeof(string));
                    dt.Columns.Add("SoTTTrongSo", typeof(string)); dt.Columns.Add("ThangCongNhan", typeof(string));

                    using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                    await conn.OpenAsync();

                    using var cmd = conn.CreateCommand();
                    // 🌟 SỬA: Chuyển sang nạp bảng động TenBangHienTai
                    cmd.CommandText = $"SELECT ID, STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu, ThanhTich, ThongBaoTrungDoan, SoTTTrongSo, ThangCongNhan FROM [{TenBangHienTai}] ORDER BY STT ASC";

                    using var reader = await cmd.ExecuteReaderAsync();
                    int sttTuDong = 1;

                    while (await reader.ReadAsync())
                    {
                        dt.Rows.Add(
                            reader.GetInt32(0),
                            sttTuDong++,
                            SafeGiaiMa(reader["HoVaTen"]?.ToString()),
                            SafeGiaiMa(reader["SoHieu"]?.ToString()),
                            SafeGiaiMa(reader["NamSinh"]?.ToString()),
                            SafeGiaiMa(reader["QueQuan"]?.ToString()),
                            SafeGiaiMa(reader["NgayVaoCAND"]?.ToString()),
                            SafeGiaiMa(reader["CapBac"]?.ToString()),
                            SafeGiaiMa(reader["ChucVu"]?.ToString()),
                            SafeGiaiMa(reader["DonVi"]?.ToString()),
                            SafeGiaiMa(reader["PhanLoai"]?.ToString()),
                            SafeGiaiMa(reader["GhiChu"]?.ToString()),
                            SafeGiaiMa(reader["ThanhTich"]?.ToString()),
                            SafeGiaiMa(reader["ThongBaoTrungDoan"]?.ToString()),
                            SafeGiaiMa(reader["SoTTTrongSo"]?.ToString()),
                            SafeGiaiMa(reader["ThangCongNhan"]?.ToString())
                        );
                    }
                    return dt;
                });

                if (kryptonDataGridView1 != null && !kryptonDataGridView1.IsDisposed)
                {
                    kryptonDataGridView1.SuspendLayout();
                    kryptonDataGridView1.DataSource = dtSoVang;
                    DinhDangGiaoDienDataGridSoVang();
                    kryptonDataGridView1.ResumeLayout();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi nạp bảng {TenBangHienTai}: " + ex.Message);
            }
        }
        private void kryptonDataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value != null && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                string columnName = kryptonDataGridView1.Columns[e.ColumnIndex].Name;

                // Giới hạn 25 ký tự cho cột Thành tích
                if (columnName == "ThanhTich")
                {
                    string fullText = e.Value.ToString();
                    int maxLength = 25;

                    if (fullText.Length > maxLength)
                    {
                        e.Value = fullText.Substring(0, maxLength) + " ...";
                        e.FormattingApplied = true;
                    }
                }
            }
        }
        private void DinhDangGiaoDienDataGridSoVang()
        {
            if (kryptonDataGridView1 == null) return;

            // Bật DoubleBuffer giúp chống nháy (Flicker) khi cuộn chuột
            typeof(DataGridView).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(kryptonDataGridView1, true, null);

            // Cấu hình chung
            kryptonDataGridView1.AllowUserToAddRows = false;
            kryptonDataGridView1.AllowUserToDeleteRows = false;
            kryptonDataGridView1.AllowUserToResizeRows = false;
            kryptonDataGridView1.RowHeadersVisible = false;
            kryptonDataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            kryptonDataGridView1.MultiSelect = false;
            kryptonDataGridView1.ReadOnly = true; // Khóa lưới chỉ để xem
            kryptonDataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            // 2. Chế độ Fill: Các cột sẽ tự động co giãn để lấp đầy grid
            kryptonDataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Khung lưới và Header
            kryptonDataGridView1.GridStyles.Style = Krypton.Toolkit.DataGridViewStyle.List;
            kryptonDataGridView1.RowTemplate.Height = 36;
            kryptonDataGridView1.ColumnHeadersHeight = 50;
            kryptonDataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            kryptonDataGridView1.StateCommon.HeaderColumn.Content.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            kryptonDataGridView1.StateCommon.DataCell.Content.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            kryptonDataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // Màu sắc và Border
            kryptonDataGridView1.StateCommon.DataCell.Border.Color1 = Color.FromArgb(224, 224, 224);
            kryptonDataGridView1.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            kryptonDataGridView1.StateCommon.DataCell.Border.Width = 1;
            kryptonDataGridView1.StateSelected.DataCell.Back.Color1 = Color.FromArgb(232, 244, 253);
            kryptonDataGridView1.StateSelected.DataCell.Content.Color1 = Color.FromArgb(0, 102, 204);

            if (kryptonDataGridView1.Columns.Count == 0) return;

            foreach (DataGridViewColumn col in kryptonDataGridView1.Columns)
                col.SortMode = DataGridViewColumnSortMode.NotSortable;

            // ==========================================
            // ĐỊNH DẠNG CHI TIẾT TỪNG CỘT
            // ==========================================
            if (kryptonDataGridView1.Columns["ID"] != null)
                kryptonDataGridView1.Columns["ID"].Visible = false;

            if (kryptonDataGridView1.Columns["STT"] != null)
            {
                kryptonDataGridView1.Columns["STT"].HeaderText = "STT";
                kryptonDataGridView1.Columns["STT"].Width = 40;
                kryptonDataGridView1.Columns["STT"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            if (kryptonDataGridView1.Columns["HoVaTen"] != null)
            {
                kryptonDataGridView1.Columns["HoVaTen"].HeaderText = "Họ và tên";
                kryptonDataGridView1.Columns["HoVaTen"].Width = 180;
                kryptonDataGridView1.Columns["HoVaTen"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }

            if (kryptonDataGridView1.Columns["SoHieu"] != null)
            {
                kryptonDataGridView1.Columns["SoHieu"].HeaderText = "Số hiệu";
                kryptonDataGridView1.Columns["SoHieu"].Width = 90;
                kryptonDataGridView1.Columns["SoHieu"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            if (kryptonDataGridView1.Columns["CapBac"] != null)
            {
                kryptonDataGridView1.Columns["CapBac"].HeaderText = "Cấp bậc";
                kryptonDataGridView1.Columns["CapBac"].Width = 90;
                kryptonDataGridView1.Columns["CapBac"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            if (kryptonDataGridView1.Columns["ChucVu"] != null)
            {
                kryptonDataGridView1.Columns["ChucVu"].HeaderText = "Chức vụ";
                kryptonDataGridView1.Columns["ChucVu"].Width = 110;
                kryptonDataGridView1.Columns["ChucVu"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            if (kryptonDataGridView1.Columns["DonVi"] != null)
            {
                kryptonDataGridView1.Columns["DonVi"].HeaderText = "Đơn vị";
                kryptonDataGridView1.Columns["DonVi"].Width = 100;
                kryptonDataGridView1.Columns["DonVi"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            // ⭐ MỞ LẠI CÁC CỘT ĐÃ BỊ ẨN
            if (kryptonDataGridView1.Columns["NamSinh"] != null)
            {
                kryptonDataGridView1.Columns["NamSinh"].Visible = true;
                kryptonDataGridView1.Columns["NamSinh"].HeaderText = "Năm sinh";
                kryptonDataGridView1.Columns["NamSinh"].Width = 85;
                kryptonDataGridView1.Columns["NamSinh"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            if (kryptonDataGridView1.Columns["QueQuan"] != null)
            {
                kryptonDataGridView1.Columns["QueQuan"].Visible = true;
                kryptonDataGridView1.Columns["QueQuan"].HeaderText = "Quê quán";
                kryptonDataGridView1.Columns["QueQuan"].Width = 150;
                kryptonDataGridView1.Columns["QueQuan"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }

            if (kryptonDataGridView1.Columns["NgayVaoCAND"] != null)
            {
                kryptonDataGridView1.Columns["NgayVaoCAND"].Visible = true;
                kryptonDataGridView1.Columns["NgayVaoCAND"].HeaderText = "Vào CAND";
                kryptonDataGridView1.Columns["NgayVaoCAND"].Width = 100;
                kryptonDataGridView1.Columns["NgayVaoCAND"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            if (kryptonDataGridView1.Columns["PhanLoai"] != null)
            {
                kryptonDataGridView1.Columns["PhanLoai"].Visible = true;
                kryptonDataGridView1.Columns["PhanLoai"].HeaderText = "Phân loại";
                kryptonDataGridView1.Columns["PhanLoai"].Width = 90;
                kryptonDataGridView1.Columns["PhanLoai"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            if (kryptonDataGridView1.Columns["GhiChu"] != null)
            {
                kryptonDataGridView1.Columns["GhiChu"].Visible = true;
                kryptonDataGridView1.Columns["GhiChu"].HeaderText = "Ghi chú";
                kryptonDataGridView1.Columns["GhiChu"].Width = 120;
                kryptonDataGridView1.Columns["GhiChu"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }

            if (kryptonDataGridView1.Columns["ThangCongNhan"] != null)
            {
                kryptonDataGridView1.Columns["ThangCongNhan"].HeaderText = "Tháng công nhận";
                kryptonDataGridView1.Columns["ThangCongNhan"].Width = 120;
                kryptonDataGridView1.Columns["ThangCongNhan"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            if (kryptonDataGridView1.Columns["ThongBaoTrungDoan"] != null)
            {
                kryptonDataGridView1.Columns["ThongBaoTrungDoan"].HeaderText = "Thông báo E";
                kryptonDataGridView1.Columns["ThongBaoTrungDoan"].Width = 120;
                kryptonDataGridView1.Columns["ThongBaoTrungDoan"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            if (kryptonDataGridView1.Columns["SoTTTrongSo"] != null)
            {
                kryptonDataGridView1.Columns["SoTTTrongSo"].HeaderText = "STT Ghi sổ";
                kryptonDataGridView1.Columns["SoTTTrongSo"].Width = 90;
                kryptonDataGridView1.Columns["SoTTTrongSo"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            // ⭐ CỘT THÀNH TÍCH (Giới hạn độ rộng, không dùng Fill)
            if (kryptonDataGridView1.Columns["ThanhTich"] != null)
            {
                kryptonDataGridView1.Columns["ThanhTich"].HeaderText = "Thành tích";

                // Thay vì Fill (chiếm hết lưới), ta đặt Width cố định
                kryptonDataGridView1.Columns["ThanhTich"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                kryptonDataGridView1.Columns["ThanhTich"].Width = 180; // Bạn có thể tùy chỉnh con số này

                kryptonDataGridView1.Columns["ThanhTich"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }
        }
        private void kryptonButton1_Thoat_Click(object sender, EventArgs e)
        {
            // 6. Đóng form hiện tại
            this.Close();
            // 2. Tìm Form2 đang chạy và cập nhật tiêu đề
            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            if (formCha != null)
            {
                formCha.CapNhatTieuDe("Quản lý phong trào thi đua Ba Nhất");
            }
        }
        private async void kryptonButton_LuuDataSoVang_Click(object sender, EventArgs e)
        {
            if (kryptonTextBox1_STT.Tag == null || !int.TryParse(kryptonTextBox1_STT.Tag.ToString(), out int id))
            {
                MessageBox.Show("Vui lòng chọn một cán bộ trong danh sách trước khi lưu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int soKyTu = richTextBox1_ThanhTich.TextLength;
            if (soKyTu > Module_BaNhat.GioiHanToiDa)
            {
                MessageBox.Show($"Nội dung thành tích hiện có {soKyTu} ký tự,\nvượt quá giới hạn cho phép ({Module_BaNhat.GioiHanToiDa} ký tự).\nVui lòng rút gọn nội dung trước khi lưu.", "Không thể lưu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                richTextBox1_ThanhTich.Focus();
                return;
            }
            kryptonButton_LuuDataSoVang.Enabled = false;
            kryptonButton_LuuDataSoVang.Text = "Đang lưu...";

            try
            {
                using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                await conn.OpenAsync();

                // 🌟 SỬA: Thay thế tên bảng động trong chuỗi lệnh UPDATE
                string sqlUpdate = $@"
                UPDATE [{TenBangHienTai}] SET
                    HoVaTen = @HoVaTen,
                    SoHieu = @SoHieu,
                    NamSinh = @NamSinh,
                    QueQuan = @QueQuan,
                    NgayVaoCAND = @NgayVaoCAND,
                    CapBac = @CapBac,
                    ChucVu = @ChucVu,
                    DonVi = @DonVi,
                    PhanLoai = @PhanLoai,
                    GhiChu = @GhiChu,
                    ThanhTich = @ThanhTich,
                    ThongBaoTrungDoan = @TB,
                    SoTTTrongSo = @SoTT,
                    ThangCongNhan = @TCN
                WHERE ID = @ID";

                using var cmd = new SqliteCommand(sqlUpdate, conn);
                cmd.Parameters.AddWithValue("@ID", id);

                (string Name, string Value)[] parameters =
                {
                    ("@HoVaTen", kryptonTextBox1_HoVaTen.Text),
                    ("@SoHieu", kryptonTextBox1_SoHieu.Text),
                    ("@NamSinh", kryptonTextBox1_NamSinh.Text),
                    ("@QueQuan", kryptonTextBox1_QueQuan.Text),
                    ("@NgayVaoCAND", kryptonTextBox1_NgayVaoCAND.Text),
                    ("@CapBac", kryptonTextBox1_CapBac.Text),
                    ("@ChucVu", kryptonTextBox1_ChucVu.Text),
                    ("@DonVi", kryptonTextBox1_DonVi.Text),
                    ("@PhanLoai", kryptonTextBox1_PhanLoai.Text),
                    ("@GhiChu", kryptonTextBox1_GhiChu.Text),
                    ("@ThanhTich", richTextBox1_ThanhTich.Text),
                    ("@TB", kryptonTextBox1_ThongBaoTrungDoan.Text),
                    ("@SoTT", kryptonTextBox1_SoTTTrongSo.Text),
                    ("@TCN", kryptonTextBox1_ThangCongNhan.Text)
                };

                foreach (var p in parameters)
                {
                    cmd.Parameters.AddWithValue(p.Name, BaoMatAES.MaHoa(p.Value.Trim()));
                }

                cmd.Prepare();
                await cmd.ExecuteNonQueryAsync();

                await LoadDuLieuSoVangBaNhatAsync();
                toolStripStatusLabel1_ThongBao.Text = $"Đã lưu thông tin đồng chí {kryptonTextBox1_HoVaTen.Text.Trim()} thành công!";
                await Task.Delay(300);
                CapNhatThongKeSoLuong();
                CapNhatDanhSachDonVi();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu dữ liệu: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                kryptonButton_LuuDataSoVang.Enabled = true;
                kryptonButton_LuuDataSoVang.Text = "Lưu dữ liệu";
            }
        }
        private async void kryptonButton1_XoaCBCS_Click(object sender, EventArgs e)
        {
            if (kryptonTextBox1_STT.Tag == null || !int.TryParse(kryptonTextBox1_STT.Tag.ToString(), out int idXoa))
            {
                MessageBox.Show("Vui lòng chọn một cán bộ trong danh sách để xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string hoTen = kryptonTextBox1_HoVaTen.Text.Trim();
            string soHieu = kryptonTextBox1_SoHieu.Text.Trim();
            string donVi = kryptonTextBox1_DonVi.Text.Trim();

            string msg = $"Bạn có thực sự muốn xóa đồng chí:\n\n" +
                         $"Họ và tên: {hoTen}\n" +
                         $"Số hiệu: {soHieu}\n" +
                         $"Đơn vị: {donVi}\n\n" +
                         "Hành động này không thể hoàn tác!";

            var result = MessageBox.Show(msg, "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            try
            {
                using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                await conn.OpenAsync();

                using var transaction = conn.BeginTransaction();
                try
                {
                    // 🌟 SỬA 3 LỆNH SQL: Đưa biến TenBangHienTai vào xử lý xóa và sắp xếp lại
                    string sqlDelete = $"DELETE FROM [{TenBangHienTai}] WHERE ID = @ID";
                    using (var cmdDelete = new SqliteCommand(sqlDelete, conn, transaction))
                    {
                        cmdDelete.Parameters.AddWithValue("@ID", idXoa);
                        await cmdDelete.ExecuteNonQueryAsync();
                    }

                    string sqlSelect = $"SELECT ID FROM [{TenBangHienTai}] ORDER BY STT ASC";
                    List<int> danhSachID = new List<int>();
                    using (var cmdSelect = new SqliteCommand(sqlSelect, conn, transaction))
                    {
                        using var reader = await cmdSelect.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            danhSachID.Add(reader.GetInt32(0));
                        }
                    }

                    string sqlUpdateSTT = $"UPDATE [{TenBangHienTai}] SET STT = @NewSTT WHERE ID = @ID";
                    for (int i = 0; i < danhSachID.Count; i++)
                    {
                        using var cmdUpdate = new SqliteCommand(sqlUpdateSTT, conn, transaction);
                        cmdUpdate.Parameters.AddWithValue("@NewSTT", i + 1);
                        cmdUpdate.Parameters.AddWithValue("@ID", danhSachID[i]);
                        await cmdUpdate.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();

                    await LoadDuLieuSoVangBaNhatAsync();
                    ClearTextBoxes();

                    toolStripStatusLabel1_ThongBao.Text = $"Đã xóa đồng chí {hoTen} thành công!";
                    await Task.Delay(300);
                    CapNhatThongKeSoLuong();
                    CapNhatDanhSachDonVi();
                    Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, $"Xóa và xếp lại STT bảng {TenBangHienTai}: {hoTen}", DateTime.Now.ToString());
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi thực hiện: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ClearTextBoxes()
        {
            kryptonTextBox1_STT.Tag = null;
            kryptonTextBox1_STT.Clear();
            kryptonTextBox1_HoVaTen.Clear();
            kryptonTextBox1_SoHieu.Clear();
            kryptonTextBox1_NamSinh.Clear();
            kryptonTextBox1_QueQuan.Clear();
            kryptonTextBox1_NgayVaoCAND.Clear();
            kryptonTextBox1_CapBac.Clear();
            kryptonTextBox1_ChucVu.Clear();
            kryptonTextBox1_DonVi.Clear();
            kryptonTextBox1_PhanLoai.Clear();
            kryptonTextBox1_GhiChu.Clear();
            richTextBox1_ThanhTich.Clear();
            kryptonTextBox1_ThongBaoTrungDoan.Clear();
            kryptonTextBox1_SoTTTrongSo.Clear();
            kryptonTextBox1_ThangCongNhan.Clear();
        }
        private void CapNhatThongKeSoLuong()
        {
            // Kiểm tra số lượng dòng thực tế đang hiển thị trên lưới
            // Lưu ý: Nếu DataGridView có thuộc tính AllowUserToAddRows = true, 
            // Rows.Count sẽ bao gồm cả dòng trống cuối cùng, nên ta cần trừ đi 1.
            int soLuong = kryptonDataGridView1.Rows.Count;
            if (kryptonDataGridView1.AllowUserToAddRows)
            {
                soLuong--;
            }

            if (soLuong > 0)
            {
                // Trường hợp có dữ liệu
                toolStripStatusLabel1_ThongBao.Text = $"Tổng cộng: {soLuong} đồng chí";
                toolStripStatusLabel1_ThongBao.ForeColor = Color.DarkBlue; // Màu chữ cho đẹp
            }
            else
            {
                // Trường hợp không có dữ liệu
                toolStripStatusLabel1_ThongBao.Text = "Sổ vàng hiện chưa có đồng chí nào đề nghị biểu dương phong trào thi đua \"Ba Nhất\" trong CAND";
                toolStripStatusLabel1_ThongBao.ForeColor = Color.Red; // Màu đỏ cho nổi bật
            }
        }
        // Thêm sự kiện này vào Form44
        private void Form44_SoVangBaNhat_FormClosed(object sender, FormClosedEventArgs e)
        {
            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            if (formCha == null) return;

            var panel = formCha.Controls.Find("PanelContainer", true).FirstOrDefault() as Panel;
            if (panel == null) return;

            // 1. Ẩn tất cả các Form khác (để tránh chồng lấn)
            foreach (Control ctl in panel.Controls)
            {
                if (ctl is Form frm) frm.Hide();
            }

            // 2. Tìm Form42 (Nếu chưa có thì tạo mới)
            var form42 = panel.Controls.OfType<Form42_QuanLyThiDuaBaNhat>().FirstOrDefault();

            if (form42 == null)
            {
                form42 = new Form42_QuanLyThiDuaBaNhat
                {
                    TopLevel = false,
                    FormBorderStyle = FormBorderStyle.None,
                    Dock = DockStyle.Fill
                };
                panel.Controls.Add(form42);
            }

            // 3. Hiển thị lại Form42
            form42.Show();
            form42.BringToFront();
        }
        private async Task ShowTemporaryStatus(string message, int durationMs = 1000)
        {
            // 1. Hiển thị thông báo
            toolStripStatusLabel1_ThongBao.Text = message;
            toolStripStatusLabel1_ThongBao.ForeColor = Color.DarkBlue;

            // 2. Chờ một khoảng thời gian (200ms - 1000ms tùy bạn chọn)
            await Task.Delay(durationMs);

            // 3. Quay lại trạng thái hiển thị tổng quân số
            CapNhatThongKeSoLuong();
        }
        private void kryptonDataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                FillDataToControls(kryptonDataGridView1.Rows[e.RowIndex]);
            }
        }
        private async void ToolStripMenuItem_XuatDanhSach_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Excel Files|*.xlsx";
            saveFileDialog.FileName = "SoVangBaNhat_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".xlsx";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = saveFileDialog.FileName;

                try
                {
                    DataTable dtData = await LayDuLieuGiaiMaAsync();

                    // =========================================================================
                    // ⭐ ĐOẠN ĐỌC DỮ LIỆU ĐỘNG VÀ GIẢI MÃ TỪ BẢNG KyHieu_DonVi
                    // =========================================================================
                    string kyHieuTrungDoan = "E08"; // Giá trị mặc định phòng trường hợp lỗi hoặc CSDL trống
                    try
                    {
                        using (var conn = new SqliteConnection($"Data Source={_csdl2Path}"))
                        {
                            await conn.OpenAsync();
                            using (var cmdKyHieu = conn.CreateCommand())
                            {
                                cmdKyHieu.CommandText = "SELECT KyHieu_TrungDoan FROM KyHieu_DonVi WHERE ID = 1";
                                var result = await cmdKyHieu.ExecuteScalarAsync();
                                if (result != null && result != DBNull.Value)
                                {
                                    // Gọi hàm SafeGiaiMa của anh để giải mã chuỗi AES
                                    string giaiMa = SafeGiaiMa(result.ToString());
                                    if (!string.IsNullOrWhiteSpace(giaiMa))
                                    {
                                        kyHieuTrungDoan = giaiMa;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Lỗi đọc ký hiệu Trung đoàn: " + ex.Message);
                    }

                    // Tạo chuỗi tiêu đề động cho cột số 3
                    string tieuDeThongBao = $"Thông báo của {kyHieuTrungDoan}";
                    // =========================================================================

                    using (var wb = new XLWorkbook())
                    {
                        var ws = wb.Worksheets.Add("Danh sách");
                        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
                        ws.PageSetup.PageOrientation = XLPageOrientation.Portrait;

                        // --- TIÊU ĐỀ ---
                        ws.Range("A1:E1").Merge().Value = "SỔ VÀNG";
                        ws.Range("A1:E1").Style.Font.SetBold(true).Font.SetFontName("Times New Roman").Font.SetFontSize(14);
                        ws.Range("A1:E1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Range("A1:E1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        ws.Range("A2:E2").Merge().Value = "BIỂU DƯƠNG GƯƠNG ĐIỂN HÌNH TRONG THỰC HIỆN PHONG TRÀO THI ĐUA BA NHẤT";
                        ws.Range("A2:E2").Style.Font.SetBold(true).Font.SetFontName("Times New Roman").Font.SetFontSize(12);
                        ws.Range("A2:E2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Range("A2:E2").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        // --- ĐỊNH DẠNG HEADER (Dòng 4) ---
                        ws.Row(4).Height = 24;

                        // --- HEADER (Dòng 4) ---
                        // ⭐ ĐÃ THAY THẾ: Sử dụng biến tieuDeThongBao động thay cho chữ "E29" cứng
                        string[] headers = { "STT", "Họ và tên", tieuDeThongBao, "Vào sổ vàng số", "Ghi chú" };

                        for (int i = 0; i < headers.Length; i++)
                        {
                            var cell = ws.Cell(4, i + 1);
                            cell.Value = headers[i];
                            cell.Style.Font.SetBold(true).Font.SetFontName("Times New Roman");
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            cell.Style.Alignment.WrapText = true;
                            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                        }

                        // --- ĐỘ RỘNG CỘT ---
                        ws.Column(1).Width = 5;
                        ws.Column(2).Width = 30;
                        ws.Column(3).Width = 40;
                        ws.Column(4).Width = 15;
                        ws.Column(5).Width = 15;

                        // --- ĐỔ DỮ LIỆU ---
                        int rowIdx = 5;
                        int sttCount = 1;
                        foreach (DataRow row in dtData.Rows)
                        {
                            ws.Cell(rowIdx, 1).Value = sttCount++;
                            ws.Cell(rowIdx, 2).Value = row["HoVaTen"].ToString();
                            ws.Cell(rowIdx, 3).Value = row["ThongBaoTrungDoan"].ToString();
                            ws.Cell(rowIdx, 4).Value = row["SoTTTrongSo"].ToString();
                            ws.Cell(rowIdx, 5).Value = row["ThangCongNhan"].ToString();

                            var rngRow = ws.Range(rowIdx, 1, rowIdx, 5);
                            rngRow.Style.Font.SetFontName("Times New Roman");
                            rngRow.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            rngRow.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                            rngRow.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            rngRow.Style.Alignment.WrapText = true;

                            ws.Cell(rowIdx, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            ws.Cell(rowIdx, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                            ws.Cell(rowIdx, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                            ws.Cell(rowIdx, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            ws.Cell(rowIdx, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            rowIdx++;
                        }

                        Module_BanQuyen.DongDauExcel(wb);
                        wb.SaveAs(filePath);
                    }

                    if (File.Exists(filePath))
                    {
                        Process.Start("explorer.exe", "/select, \"" + filePath + "\"");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi xuất Excel: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private string SafeGiaiMa(string cipherText)
        {
            try
            {
                if (string.IsNullOrEmpty(cipherText)) return "";
                return BaoMatAES.GiaiMa(cipherText).Trim();
            }
            catch
            {
                return "[Lỗi giải mã]";
            }
        }
        private async Task<DataTable> LayDuLieuGiaiMaAsync()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("STT");
            dt.Columns.Add("HoVaTen");
            dt.Columns.Add("ThongBaoTrungDoan");
            dt.Columns.Add("SoTTTrongSo");
            dt.Columns.Add("ThangCongNhan");

            using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
            await conn.OpenAsync();

            // 🌟 SỬA: Cập nhật gọi tên bảng động cho câu lệnh xuất báo cáo Excel
            using var cmd = new SqliteCommand($"SELECT STT, HoVaTen, ThongBaoTrungDoan, SoTTTrongSo, ThangCongNhan FROM [{TenBangHienTai}] ORDER BY STT ASC", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                dt.Rows.Add(
                    reader["STT"].ToString(),
                    SafeGiaiMa(reader["HoVaTen"].ToString()),
                    SafeGiaiMa(reader["ThongBaoTrungDoan"].ToString()),
                    SafeGiaiMa(reader["SoTTTrongSo"].ToString()),
                    SafeGiaiMa(reader["ThangCongNhan"].ToString())
                );
            }
            return dt;
        }
        private void LocDuLieu()
        {
            var dt = kryptonDataGridView1.DataSource as DataTable;
            if (dt == null) return;

            kryptonDataGridView1.BindingContext[dt].SuspendBinding();
            try
            {
                string filter = "";
                string ten = kryptonTextBox1_TimKiemTheoTen.Text.Replace("'", "''");

                if (!string.IsNullOrWhiteSpace(ten))
                    filter += $"HoVaTen LIKE '%{ten}%'";

                string donVi = comboBox_TimKiemDonVi.Text;
                if (!string.IsNullOrWhiteSpace(donVi) && donVi != "Tất cả")
                {
                    if (filter.Length > 0) filter += " AND ";
                    filter += $"DonVi = '{donVi.Replace("'", "''")}'";
                }

                dt.DefaultView.RowFilter = filter;
                CapNhatThongKeSoLuong();
                // XÓA DÒNG CapNhatDanhSachDonVi(); Ở ĐÂY NẾU CÒN
            }
            finally
            {
                kryptonDataGridView1.BindingContext[dt].ResumeBinding();
            }
        }
        private void kryptonButton_LamMoiCacOTimKiem_Click(object sender, EventArgs e)
        {
            kryptonTextBox1_TimKiemTheoTen.TextChanged -= (s, ev) => LocDuLieu(); // Tạm ngắt sự kiện để tránh lọc thừa

            kryptonTextBox1_TimKiemTheoTen.Clear();
            if (comboBox_TimKiemDonVi.Items.Count > 0)
                comboBox_TimKiemDonVi.SelectedIndex = 0;

            var dt = kryptonDataGridView1.DataSource as DataTable;
            if (dt != null) dt.DefaultView.RowFilter = string.Empty;

            CapNhatThongKeSoLuong();

            kryptonTextBox1_TimKiemTheoTen.TextChanged += (s, ev) => LocDuLieu(); // Bật lại sự kiện
        }
        private void CapNhatDanhSachDonVi()
        {
            var dt = kryptonDataGridView1.DataSource as DataTable;
            if (dt == null) return;

            // 1. Lưu lại giá trị hiện tại
            string currentSelection = comboBox_TimKiemDonVi.Text;

            // 2. Bỏ bộ lọc tạm thời để lấy TOÀN BỘ danh sách đơn vị có trong Grid
            var dataView = dt.DefaultView;
            var currentFilter = dataView.RowFilter;
            dataView.RowFilter = string.Empty;

            // 3. Trích xuất các đơn vị thực tế ĐANG CÓ trong dữ liệu (HashSet giúp tìm kiếm cực nhanh)
            var distinctTable = dataView.ToTable(true, "DonVi");
            HashSet<string> cacDonViCoTrongData = new HashSet<string>();

            foreach (DataRow row in distinctTable.Rows)
            {
                if (row["DonVi"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["DonVi"].ToString()))
                {
                    cacDonViCoTrongData.Add(row["DonVi"].ToString().Trim());
                }
            }

            // 4. Lấy danh sách chuẩn (đã sắp xếp đúng thứ tự) từ Module_DonVi
            List<string> danhSachChuan = Module_DonVi.GetDanhSachDonVi();

            // 5. Nạp vào ComboBox
            comboBox_TimKiemDonVi.Items.Clear();
            comboBox_TimKiemDonVi.Items.Add("Tất cả");

            // BƯỚC QUAN TRỌNG: Duyệt theo danh sách CHUẨN. 
            // Nếu đơn vị chuẩn nào có xuất hiện trong Data thì mới thêm vào ComboBox.
            // Việc này đảm bảo: Chỉ hiện đơn vị có dữ liệu VÀ giữ đúng thứ tự của Module_DonVi.
            foreach (string donViChuan in danhSachChuan)
            {
                if (cacDonViCoTrongData.Contains(donViChuan))
                {
                    comboBox_TimKiemDonVi.Items.Add(donViChuan);
                    cacDonViCoTrongData.Remove(donViChuan); // Xóa đi để đánh dấu là đã xử lý
                }
            }

            // (Tùy chọn an toàn): Nhỡ có đơn vị nào bị gõ sai tên trong CSDL (không khớp danh sách chuẩn)
            // Ta vẫn thêm nó vào cuối ComboBox để người dùng không bị mất dữ liệu lọc
            foreach (string donViThua in cacDonViCoTrongData)
            {
                comboBox_TimKiemDonVi.Items.Add(donViThua);
            }

            // 6. Trả lại trạng thái cũ
            dataView.RowFilter = currentFilter;

            if (comboBox_TimKiemDonVi.Items.Contains(currentSelection))
                comboBox_TimKiemDonVi.SelectedItem = currentSelection;
            else
                comboBox_TimKiemDonVi.SelectedIndex = 0;
        }
        private async void kryptonButton_RefershCSDL_Click(object sender, EventArgs e)
        {
            // 1. Tạm khóa nút để tránh người dùng nhấn liên tục
            kryptonButton_RefershCSDL.Enabled = false;
            string originalText = kryptonButton_RefershCSDL.Text;
            kryptonButton_RefershCSDL.Text = "Đang tải...";

            try
            {
                // 2. Làm sạch các ô tìm kiếm để reset lại trạng thái xem toàn bộ
                kryptonTextBox1_TimKiemTheoTen.TextChanged -= (s, ev) => LocDuLieu(); // Tạm ngắt sự kiện để tránh trigger không cần thiết
                kryptonTextBox1_TimKiemTheoTen.Clear();

                if (comboBox_TimKiemDonVi.Items.Count > 0)
                    comboBox_TimKiemDonVi.SelectedIndex = 0; // Chọn "Tất cả"

                kryptonTextBox1_TimKiemTheoTen.TextChanged += (s, ev) => LocDuLieu(); // Bật lại sự kiện

                // 3. Nạp lại dữ liệu từ CSDL
                await LoadDuLieuSoVangBaNhatAsync();

                // 4. Xóa trắng các ô thông tin chi tiết (để tránh hiển thị dữ liệu cũ)
                ClearTextBoxes();

                // 5. Cập nhật lại các thành phần phụ trợ
                CapNhatDanhSachDonVi();
                CapNhatThongKeSoLuong();

                // 6. Thông báo thành công
                toolStripStatusLabel1_ThongBao.Text = "Đã làm mới dữ liệu từ cơ sở dữ liệu.";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi làm mới dữ liệu: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 7. Khôi phục lại trạng thái nút
                kryptonButton_RefershCSDL.Enabled = true;
                kryptonButton_RefershCSDL.Text = originalText;
            }
        }
        private void lamMoiHeThong_Click(object sender, EventArgs e)
        {
            kryptonButton_RefershCSDL.PerformClick();
        }
        private void xoaTimKiem_Click(object sender, EventArgs e)
        {
            kryptonButton_LamMoiCacOTimKiem.PerformClick();
        }
        private void toolStripMenuItem_ThoatTrang_Click(object sender, EventArgs e)
        {
            kryptonButton1_Thoat.PerformClick();
        }
        private async void toolStripMenuItem_XoaChonTatCa_Click(object sender, EventArgs e)
        {
            // 1. Kiểm tra đường dẫn CSDL
            string dbPath = _csdl2Path;
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
            {
                MessageBox.Show("Không tìm thấy tệp cơ sở dữ liệu!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // 2. Kiểm tra dữ liệu trước để báo người dùng
                int soDongTrongBang = 0;
                using (var conn = new SqliteConnection($"Data Source={dbPath}"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        // 🌟 SỬA ĐỊNH TUYẾN 1: Đếm số dòng trên bảng động
                        cmd.CommandText = $"SELECT COUNT(*) FROM [{TenBangHienTai}];";
                        soDongTrongBang = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }

                if (soDongTrongBang == 0)
                {
                    MessageBox.Show($"Danh sách Sổ vàng hệ thống [{TenBangHienTai}] hiện đang trống.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 4. Gọi form xác minh quyền admin
                DialogResult kq;
                using (Form24_XacMinhAdmin frm = new Form24_XacMinhAdmin())
                {
                    frm.TopMost = true;
                    frm.StartPosition = FormStartPosition.CenterScreen;
                    kq = frm.ShowDialog();
                }

                if (kq != DialogResult.OK)
                    return;

                // 5. Thực hiện xóa
                int soDongDaXoa = 0;
                using (var conn = new SqliteConnection($"Data Source={dbPath}"))
                {
                    conn.Open();

                    // Transaction đảm bảo an toàn tuyệt đối
                    using (var tran = conn.BeginTransaction())
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tran;

                        // 🌟 SỬA ĐỊNH TUYẾN 2: Xóa toàn bộ dữ liệu trên bảng động
                        cmd.CommandText = $"DELETE FROM [{TenBangHienTai}];";
                        soDongDaXoa = cmd.ExecuteNonQuery();

                        // 🌟 SỬA ĐỊNH TUYẾN 3: Reset lại chỉ số AutoIncrement của ID về 1 cho đúng bảng
                        cmd.CommandText = $"DELETE FROM sqlite_sequence WHERE name='{TenBangHienTai}';";
                        cmd.ExecuteNonQuery();

                        tran.Commit();
                    }

                    // Dọn file database (giảm dung lượng file)
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "VACUUM;";
                        cmd.ExecuteNonQuery();
                    }
                }

                // 6. Ghi nhật ký hệ thống
                Module_NhatKy.GhiNhatKy(
                    Module_TaiKhoan.TenTaiKhoan_RAM,
                    $"Xóa toàn bộ dữ liệu Sổ vàng Ba Nhất bảng [{TenBangHienTai}] ({soDongDaXoa} dòng)",
                    DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")
                );

                MessageBox.Show($"Đã xóa thành công {soDongDaXoa} dòng dữ liệu.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 7. Cập nhật lại giao diện
                kryptonDataGridView1.DataSource = null; // Xóa nguồn dữ liệu
                ClearTextBoxes();                       // Xóa sạch ô nhập
                await LoadDuLieuSoVangBaNhatAsync();    // Nạp lại danh sách trống
                CapNhatThongKeSoLuong();                // Cập nhật nhãn thống kê
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa dữ liệu:\n{ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void CapNhatTrangThaiNut()
        {
            // Kiểm tra xem cả 2 TextBox có dữ liệu (không rỗng hoặc khoảng trắng) hay không
            bool coDuLieu = !string.IsNullOrWhiteSpace(kryptonTextBox1_SoHieu.Text) &&
                            !string.IsNullOrWhiteSpace(kryptonTextBox1_HoVaTen.Text);

            // Điều khiển thuộc tính Visible của 2 nút
            kryptonButton1_XoaCBCS.Visible = coDuLieu;
            kryptonButton_LuuDataSoVang.Visible = coDuLieu;
        }
    }
}