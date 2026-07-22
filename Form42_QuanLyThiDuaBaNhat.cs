using ClosedXML.Excel; // Nhớ thêm thư viện này
using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhanMemThiDua2026
{
    public partial class Form42_QuanLyThiDuaBaNhat : Form
    {
        // Đường dẫn cơ sở dữ liệu csdl2.db của hệ thống
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private int _tongQuanSoGoc = 0; // Biến lưu trữ tổng số quân để đối chiếu khi tìm kiếm
        private string TenBangSoVangHienTai
        {
            get
            {
                string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
                return phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase)
                    ? "DanhSachTanBinh_SoVangBaNhat"
                    : "DanhSach_SoVangBaNhat";
            }
        }
        private string TenBangDanhSachBaNhat
        {
            get
            {
                string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
                return phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase)
                    ? "DanhSachBaNhat_TanBinh"
                    : "DanhSachBaNhat";
            }
        }
        public Form42_QuanLyThiDuaBaNhat()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterScreen;
            // ⭐ GÁN BỘ VẼ CLASSIC CHO CONTEXT MENU TẠI ĐÂY
            // ⭐ SỬA TẠI ĐÂY: Gộp chung gọi 2 hàm khi nội dung thay đổi
            richTextBox1_ThanhTich.TextChanged += (s, e) =>
            {
                KiemTraHienThiNutCoChu();
                CapNhatSoLuongKyTu(); // <--- Anh bị thiếu dòng gọi hàm này
            };
            // ⭐ ĐĂNG KÝ SỰ KIỆN THEO DÕI ẨN HÀN NÚT Ở ĐÂY
            textBox_SoHieu.TextChanged += (s, e) => CapNhatTrangThaiNut();
            textBox_HoVaTen.TextChanged += (s, e) => CapNhatTrangThaiNut();
        }
        private async void Form42_QuanLyThiDuaBaNhat_Load(object sender, EventArgs e)
        {             
            kryptonDataGridView1.CellClick += kryptonDataGridView1_CellClick;
            kryptonDataGridView1.CellFormatting += kryptonDataGridView1_CellFormatting;
            kryptonDataGridView1.ContextMenuStrip = contextMenuStrip1;
            kryptonDataGridView1.CellMouseClick += kryptonDataGridView1_CellMouseClick;
            // ⭐ BỔ SUNG DÒNG NÀY: Lắng nghe sự kiện khi gõ vào ô Tóm tắt thành tích
            richTextBox1_ThanhTich.TextChanged += richTextBox1_ThanhTich_TextChanged;
            // ⭐ TUYỆT CHIÊU TRỊ KRYPTON: Ép bảng màu ngay trước tích tắc Menu mở lên
              Module_MenuChuotPhai.TichHopGiaoDienXanhLa(contextMenuStrip1);
            // THÊM DÒNG NÀY VÀO CUỐI HÀM LOAD:
            await Module_BaNhat.TinhToanVaHienThiTyLeBaNhatAsync(toolStripStatusLabel2_TyLeBaNhat);
            // ⭐ GỌI HÀM Ở ĐÂY ĐỂ TÍNH TOÁN TÌNH TRẠNG TRƯỚC KHI LOAD LÊN LƯỚI
            await Module_BaNhat.CapNhatTinhTrangSoVangAsync();
            // toolStripProgressBar1_LamMoi.Value = 0; // Reset về 0 cho lần bấm sau
            toolStripProgressBar1_LamMoi.Visible = false;
            // Đăng ký sự kiện cho bộ lọc
            if (textBox_TimKiemTheoTen != null)
                textBox_TimKiemTheoTen.TextChanged += (s, ev) => ThucHienLocDuLieu();
            if (comboBox_TimKiemDonVi != null)
                comboBox_TimKiemDonVi.SelectedIndexChanged += (s, ev) => ThucHienLocDuLieu();
            // ⭐ Đăng ký sự kiện cho Combobox DeNghi mới
            if (comboBox1_DeNghi != null)
                comboBox1_DeNghi.SelectedIndexChanged += (s, ev) => ThucHienLocDuLieu();
            this.AcceptButton = kryptonButton_LuuDataDeNghi;

            await DongBoDuLieuLoai1SangBaNhatAsync();
            await LoadDuLieuToanBoDanhSachBaNhatAsync();
            KiemTraHienThiNutCoChu();
            KhoaCacTextBox();
            CapNhatTrangThaiNut();
            CapNhatTrangThaiDuoiNen();
            CapNhatSoLuongKyTu();
            InitToolTips();

        }
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Gợi ý thao tác";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            // UX: phản hồi nhanh – không gây khó chịu
            toolTip1.InitialDelay = 300;
            toolTip1.AutoPopDelay = 2500;
            toolTip1.ReshowDelay = 100;
            toolTip1.ShowAlways = true;
            var tips = new Dictionary<Control, string>
{
             //THÔNG TIN ĐƠN VỊ / ĐỊA ĐIỂM ==
        // Thêm vào nhóm LƯU / KIỂM TRA / CƠ SỞ DỮ LIỆU
        { kryptonButton_LuuDataDeNghi, "Lưu dữ liệu đề nghị biểu dương ba nhất" },
        { kryptonButton_RefershCSDL, "Tải lại và cập nhật dữ liệu mới nhất từ cơ sở dữ liệu" },
        // Thêm vào nhóm THÀNH TÍCH / KHEN THƯỞNG (hoặc nhóm phù hợp)
        { kryptonButton1_ThanhTichTapThe, "Xem và quản lý bảng thành tích của tập thể đơn vị" },
        // Thêm vào cuối cùng (Nhóm HỆ THỐNG)
        { kryptonButton1_MoSoVang, "Mở trang quản lý Sổ vàng" },
        { kryptonButton1_Thoat, "Thoát trang này, trở về trang dữ liệu" },
         { kryptonButton2_GiamCoChuRichText, "Giảm cỡ chữ" },
        { kryptonButton2_TangCoChuRichText, "Tăng cỡ chữ" }
        };
            foreach (var tip in tips)
            {
                if (tip.Key != null) // an toàn khi control bị ẩn / đổi tên
                    toolTip1.SetToolTip(tip.Key, tip.Value);
            }
        }
        private void kryptonDataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // Kiểm tra xem ô hiện tại có dữ liệu hay không
            if (e.Value != null && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                // Lấy tên của cột đang được vẽ
                string columnName = kryptonDataGridView1.Columns[e.ColumnIndex].Name;

                // Chỉ áp dụng rút gọn cho cột Ghi Chú và Thành Tích
                if (columnName == "GhiChu" || columnName == "ThanhTich")
                {
                    string fullText = e.Value.ToString();
                    int maxLength = 25; // Số ký tự tối đa bạn muốn hiển thị trên lưới

                    // Nếu chữ dài hơn mức cho phép thì tiến hành cắt
                    if (fullText.Length > maxLength)
                    {
                        // Cắt lấy 60 ký tự đầu tiên và nối thêm " ..."
                        e.Value = fullText.Substring(0, maxLength) + " ...";
                        e.FormattingApplied = true; // Báo cho Grid biết là đã format xong, đừng tự hiển thị cái cũ nữa
                    }
                }
                // Bổ sung xử lý riêng cho cột Quê Quán
                else if (columnName == "QueQuan")
                {
                    string fullText = e.Value.ToString();
                    int maxLength = 25; // Giới hạn riêng 25 ký tự cho Quê quán

                    // Nếu chữ dài hơn mức cho phép thì tiến hành cắt
                    if (fullText.Length > maxLength)
                    {
                        e.Value = fullText.Substring(0, maxLength) + " ...";
                        e.FormattingApplied = true;
                    }
                }
            }
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

                // 3. Đổ dữ liệu lên TextBox như hành vi CellClick
                // Gọi lại hàm đổ dữ liệu bạn đã viết sẵn (hoặc có thể trích xuất hàm đó ra riêng)
                DataGridViewRow row = kryptonDataGridView1.Rows[e.RowIndex];

                textBox_SoHieu.Tag = row.Cells["ID"].Value;
                textBox_STT.Text = row.Cells["STT"].Value?.ToString();
                textBox_HoVaTen.Text = row.Cells["HoVaTen"].Value?.ToString();
                textBox_SoHieu.Text = row.Cells["SoHieu"].Value?.ToString();
                textBox_NamSinh.Text = row.Cells["NamSinh"].Value?.ToString();
                textBox_QueQuan.Text = row.Cells["QueQuan"].Value?.ToString();
                textBox_NgayVaoCAND.Text = row.Cells["NgayVaoCAND"].Value?.ToString();
                kryptonTextBox1_CapBac.Text = row.Cells["CapBac"].Value?.ToString();
                kryptonTextBox1_ChucVu.Text = row.Cells["ChucVu"].Value?.ToString();
                kryptonTextBox1_DonVi.Text = row.Cells["DonVi"].Value?.ToString();
                kryptonTextBox1_PhanLoai.Text = row.Cells["PhanLoai"].Value?.ToString();
                textBox_GhiChu.Text = row.Cells["GhiChu"].Value?.ToString();

                string deNghiDb = row.Cells["DeNghi"].Value?.ToString() ?? "";
                comboBox_DeNghiBaNhat.Text = deNghiDb.Trim().Equals("X", StringComparison.OrdinalIgnoreCase) ? "Đề nghị" : "";
                richTextBox1_ThanhTich.Text = row.Cells["ThanhTich"].Value?.ToString();
            }
        }
        private void ThongKeSoLuongBaNhat(bool hienThongBaoLamMoi = false)
        {
            int tongCong = 0;
            int soDeNghi = 0;
            // Kiểm tra nếu lưới có dữ liệu thì mới tiến hành đếm
            if (kryptonDataGridView1 != null && kryptonDataGridView1.Rows.Count > 0)
            {
                tongCong = kryptonDataGridView1.Rows.Count;

                // Quét từng dòng để đếm số lượng người được đề nghị (có chữ "X")
                foreach (DataGridViewRow row in kryptonDataGridView1.Rows)
                {
                    string giaTriDeNghi = row.Cells["DeNghi"].Value?.ToString() ?? "";
                    if (giaTriDeNghi.Trim().Equals("X", StringComparison.OrdinalIgnoreCase))
                    {
                        soDeNghi++;
                    }
                }
            }
            // ⭐ XỬ LÝ LOGIC HIỂN THỊ CÂU THỐNG KÊ
            string cauThongKe;
            if (soDeNghi > 0)
            {
                cauThongKe = $"Tổng cộng: {tongCong} đồng chí, được đề nghị biểu dương {soDeNghi} đồng chí.";
            }
            else
            {
                cauThongKe = $"Tổng cộng: {tongCong} đồng chí.";
            }

            //// Cập nhật lên thanh trạng thái dưới cùng của Form
            //if (toolStripStatusLabel1 != null)
            //{
            //    if (hienThongBaoLamMoi)
            //    {
            //        toolStripStatusLabel1.Text = $"Làm mới thành công! {cauThongKe}";
            //    }
            //    else
            //    {
            //        toolStripStatusLabel1.Text = cauThongKe;
            //    }
            //}
        }
        private void KhoaCacTextBox()
        {
            // Cài đặt màu xám hệ thống (Màu xám nhạt chuẩn Windows)
            var grayColor = System.Drawing.SystemColors.Control;

            // Khai báo một mảng chứa toàn bộ các KryptonTextBox
            Krypton.Toolkit.KryptonTextBox[] allKryptonTextBoxes = {
        textBox_STT,
        textBox_HoVaTen,
        textBox_SoHieu,
        textBox_NamSinh,
        textBox_QueQuan,
        textBox_NgayVaoCAND,
        textBox_GhiChu,
        kryptonTextBox1_CapBac,
        kryptonTextBox1_ChucVu,
        kryptonTextBox1_DonVi,
        kryptonTextBox1_PhanLoai
    };
            // Duyệt qua mảng để khóa và đổi màu
            foreach (var ktb in allKryptonTextBoxes)
            {
                ktb.ReadOnly = true;
                // Đối với Krypton, sử dụng StateCommon.Back.Color1 để đè lên Theme/Palette
                //ktb.StateCommon.Back.Color1 = Color.LightGreen;
                // ktb.StateCommon.Back.Color1 = Color.Honeydew;
                // Màu xanh pastel nhạt dịu mắt (Đỏ: 230, Xanh lá: 255, Xanh dương: 230)
                ktb.StateCommon.Back.Color1 = Color.FromArgb(230, 255, 230);
            }
        }
        public async Task DongBoDuLieuLoai1SangBaNhatAsync()
        {
            if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path)) return;

            try
            {
                using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                await conn.OpenAsync();

                var danhSachLoai1 = new List<Dictionary<string, object>>();
                var dsSoHieuLoai1 = new List<string>();

                using (var cmdSelect = conn.CreateCommand())
                {
                    cmdSelect.CommandText = "SELECT STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu FROM DanhSach";
                    using var reader = await cmdSelect.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        string phanLoaiEnc = reader["PhanLoai"]?.ToString() ?? "";
                        string phanLoai = BaoMatAES.GiaiMa(phanLoaiEnc).Trim();
                        string soHieuEnc = reader["SoHieu"]?.ToString() ?? "";

                        if (phanLoai.Equals("Loại 1", StringComparison.OrdinalIgnoreCase))
                        {
                            dsSoHieuLoai1.Add(soHieuEnc);
                            var row = new Dictionary<string, object>
                    {
                        {"STT", Convert.ToInt32(reader["STT"])},
                        {"HoVaTen", reader["HoVaTen"]?.ToString() ?? ""},
                        {"SoHieu", soHieuEnc},
                        {"NamSinh", reader["NamSinh"]?.ToString() ?? ""},
                        {"QueQuan", reader["QueQuan"]?.ToString() ?? ""},
                        {"NgayVaoCAND", reader["NgayVaoCAND"]?.ToString() ?? ""},
                        {"CapBac", reader["CapBac"]?.ToString() ?? ""},
                        {"ChucVu", reader["ChucVu"]?.ToString() ?? ""},
                        {"DonVi", reader["DonVi"]?.ToString() ?? ""},
                        {"PhanLoai", phanLoaiEnc},
                        {"GhiChu", reader["GhiChu"]?.ToString() ?? ""}
                    };
                            danhSachLoai1.Add(row);
                        }
                    }
                }

                using var transaction = conn.BeginTransaction();
                foreach (var row in danhSachLoai1)
                {
                    using var cmdCheck = conn.CreateCommand();
                    cmdCheck.Transaction = transaction;
                    cmdCheck.CommandText = $"SELECT EXISTS(SELECT 1 FROM [{TenBangDanhSachBaNhat}] WHERE SoHieu = @soHieu)";
                    cmdCheck.Parameters.AddWithValue("@soHieu", row["SoHieu"]);

                    bool daTonTai = Convert.ToInt64(await cmdCheck.ExecuteScalarAsync()) > 0;

                    if (!daTonTai)
                    {
                        using var cmdInsert = conn.CreateCommand();
                        cmdInsert.Transaction = transaction;

                        // ⭐ MỚI: Thêm TinhTrang vào câu lệnh INSERT (Gán rỗng ban đầu)
                        cmdInsert.CommandText = $@"INSERT INTO [{TenBangDanhSachBaNhat}]
                (STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu, DeNghi, ThanhTich, TinhTrang)
                VALUES
                (@stt, @hoVaTen, @soHieu, @namSinh, @queQuan, @ngayVaoCAND, @capBac, @chucVu, @donVi, @phanLoai, @ghiChu, @deNghi, @thanhTich, @tinhTrang)";

                        cmdInsert.Parameters.AddWithValue("@stt", row["STT"]);
                        cmdInsert.Parameters.AddWithValue("@hoVaTen", row["HoVaTen"]);
                        cmdInsert.Parameters.AddWithValue("@soHieu", row["SoHieu"]);
                        cmdInsert.Parameters.AddWithValue("@namSinh", row["NamSinh"]);
                        cmdInsert.Parameters.AddWithValue("@queQuan", row["QueQuan"]);
                        cmdInsert.Parameters.AddWithValue("@ngayVaoCAND", row["NgayVaoCAND"]);
                        cmdInsert.Parameters.AddWithValue("@capBac", row["CapBac"]);
                        cmdInsert.Parameters.AddWithValue("@chucVu", row["ChucVu"]);
                        cmdInsert.Parameters.AddWithValue("@donVi", row["DonVi"]);
                        cmdInsert.Parameters.AddWithValue("@phanLoai", row["PhanLoai"]);
                        cmdInsert.Parameters.AddWithValue("@ghiChu", row["GhiChu"]);
                        cmdInsert.Parameters.AddWithValue("@deNghi", BaoMatAES.MaHoa(""));
                        cmdInsert.Parameters.AddWithValue("@thanhTich", BaoMatAES.MaHoa(""));
                        cmdInsert.Parameters.AddWithValue("@tinhTrang", BaoMatAES.MaHoa("")); // Khởi tạo rỗng

                        await cmdInsert.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        using var cmdUpdate = conn.CreateCommand();
                        cmdUpdate.Transaction = transaction;
                        cmdUpdate.CommandText = $@"UPDATE [{TenBangDanhSachBaNhat}] SET
                HoVaTen = @hoVaTen, NamSinh = @namSinh, QueQuan = @queQuan,
                NgayVaoCAND = @ngayVaoCAND, CapBac = @capBac, ChucVu = @chucVu,
                DonVi = @donVi, PhanLoai = @phanLoai, GhiChu = @ghiChu
                WHERE SoHieu = @soHieu";

                        cmdUpdate.Parameters.AddWithValue("@hoVaTen", row["HoVaTen"]);
                        cmdUpdate.Parameters.AddWithValue("@soHieu", row["SoHieu"]);
                        cmdUpdate.Parameters.AddWithValue("@namSinh", row["NamSinh"]);
                        cmdUpdate.Parameters.AddWithValue("@queQuan", row["QueQuan"]);
                        cmdUpdate.Parameters.AddWithValue("@ngayVaoCAND", row["NgayVaoCAND"]);
                        cmdUpdate.Parameters.AddWithValue("@capBac", row["CapBac"]);
                        cmdUpdate.Parameters.AddWithValue("@chucVu", row["ChucVu"]);
                        cmdUpdate.Parameters.AddWithValue("@donVi", row["DonVi"]);
                        cmdUpdate.Parameters.AddWithValue("@phanLoai", row["PhanLoai"]);
                        cmdUpdate.Parameters.AddWithValue("@ghiChu", row["GhiChu"]);
                        // Cột TinhTrang, DeNghi, ThanhTich không bị ghi đè để bảo toàn dữ liệu user đã nhập
                        await cmdUpdate.ExecuteNonQueryAsync();
                    }
                }

                using (var cmdSelectBaNhat = conn.CreateCommand())
                {
                    cmdSelectBaNhat.Transaction = transaction;
                    cmdSelectBaNhat.CommandText = $"SELECT SoHieu FROM [{TenBangDanhSachBaNhat}]";
                    using var readerBaNhat = await cmdSelectBaNhat.ExecuteReaderAsync();
                    var soHieuCanXoa = new List<string>();

                    while (await readerBaNhat.ReadAsync())
                    {
                        string soHieuBaNhatEnc = readerBaNhat["SoHieu"]?.ToString() ?? "";
                        if (!dsSoHieuLoai1.Contains(soHieuBaNhatEnc))
                        {
                            soHieuCanXoa.Add(soHieuBaNhatEnc);
                        }
                    }
                    await readerBaNhat.CloseAsync();

                    foreach (string shXoa in soHieuCanXoa)
                    {
                        using var cmdDelete = conn.CreateCommand();
                        cmdDelete.Transaction = transaction;
                        cmdDelete.CommandText = $"DELETE FROM [{TenBangDanhSachBaNhat}] WHERE SoHieu = @soHieu";
                        cmdDelete.Parameters.AddWithValue("@soHieu", shXoa);
                        await cmdDelete.ExecuteNonQueryAsync();
                    }
                }
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi đồng bộ chuyển bảng dữ liệu Ba Nhất: " + ex.Message);
            }
        }
        public async Task LoadDuLieuToanBoDanhSachBaNhatAsync()
        {
            if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path)) return;

            try
            {
                if (kryptonDataGridView1 != null) kryptonDataGridView1.DataSource = null;

                DataTable dtBaNhat = new DataTable();

                await using (var conn = new SqliteConnection($"Data Source={_csdl2Path}"))
                {
                    await conn.OpenAsync();

                    await using var cmd = conn.CreateCommand();
                    // ⭐ MỚI: Thêm TinhTrang vào câu SELECT
                    cmd.CommandText = $"SELECT ID, STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu, DeNghi, ThanhTich, TinhTrang FROM [{TenBangDanhSachBaNhat}] ORDER BY STT ASC;";

                    await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
                    dtBaNhat.Columns.Add("ID", typeof(int));
                    dtBaNhat.Columns.Add("STT", typeof(int));
                    dtBaNhat.Columns.Add("HoVaTen", typeof(string));
                    dtBaNhat.Columns.Add("SoHieu", typeof(string));
                    dtBaNhat.Columns.Add("NamSinh", typeof(string));
                    dtBaNhat.Columns.Add("QueQuan", typeof(string));
                    dtBaNhat.Columns.Add("NgayVaoCAND", typeof(string));
                    dtBaNhat.Columns.Add("CapBac", typeof(string));
                    dtBaNhat.Columns.Add("ChucVu", typeof(string));
                    dtBaNhat.Columns.Add("DonVi", typeof(string));
                    dtBaNhat.Columns.Add("PhanLoai", typeof(string));
                    dtBaNhat.Columns.Add("GhiChu", typeof(string));
                    dtBaNhat.Columns.Add("DeNghi", typeof(string));
                    dtBaNhat.Columns.Add("ThanhTich", typeof(string));
                    dtBaNhat.Columns.Add("TinhTrang", typeof(string)); // ⭐ MỚI

                    dtBaNhat.BeginLoadData();

                    int idxID = reader.GetOrdinal("ID");
                    int idxHoTen = reader.GetOrdinal("HoVaTen");
                    int idxSoHieu = reader.GetOrdinal("SoHieu");
                    int idxNamSinh = reader.GetOrdinal("NamSinh");
                    int idxQueQuan = reader.GetOrdinal("QueQuan");
                    int idxNgayVao = reader.GetOrdinal("NgayVaoCAND");
                    int idxCapBac = reader.GetOrdinal("CapBac");
                    int idxChucVu = reader.GetOrdinal("ChucVu");
                    int idxDonVi = reader.GetOrdinal("DonVi");
                    int idxPhanLoai = reader.GetOrdinal("PhanLoai");
                    int idxGhiChu = reader.GetOrdinal("GhiChu");
                    int idxDeNghi = reader.GetOrdinal("DeNghi");
                    int idxThanhTich = reader.GetOrdinal("ThanhTich");
                    int idxTinhTrang = reader.GetOrdinal("TinhTrang"); // ⭐ MỚI

                    int sttTuDong = 1;

                    while (await reader.ReadAsync())
                    {
                        object[] row =
                        {
                    reader.GetInt32(idxID),
                    sttTuDong++,
                    SafeGiaiMa(reader.IsDBNull(idxHoTen) ? null : reader.GetString(idxHoTen)),
                    SafeGiaiMa(reader.IsDBNull(idxSoHieu) ? null : reader.GetString(idxSoHieu)),
                    SafeGiaiMa(reader.IsDBNull(idxNamSinh) ? null : reader.GetString(idxNamSinh)),
                    SafeGiaiMa(reader.IsDBNull(idxQueQuan) ? null : reader.GetString(idxQueQuan)),
                    SafeGiaiMa(reader.IsDBNull(idxNgayVao) ? null : reader.GetString(idxNgayVao)),
                    SafeGiaiMa(reader.IsDBNull(idxCapBac) ? null : reader.GetString(idxCapBac)),
                    SafeGiaiMa(reader.IsDBNull(idxChucVu) ? null : reader.GetString(idxChucVu)),
                    SafeGiaiMa(reader.IsDBNull(idxDonVi) ? null : reader.GetString(idxDonVi)),
                    SafeGiaiMa(reader.IsDBNull(idxPhanLoai) ? null : reader.GetString(idxPhanLoai)),
                    SafeGiaiMa(reader.IsDBNull(idxGhiChu) ? null : reader.GetString(idxGhiChu)),
                    SafeGiaiMa(reader.IsDBNull(idxDeNghi) ? null : reader.GetString(idxDeNghi)),
                    SafeGiaiMa(reader.IsDBNull(idxThanhTich) ? null : reader.GetString(idxThanhTich)),
                    SafeGiaiMa(reader.IsDBNull(idxTinhTrang) ? null : reader.GetString(idxTinhTrang)) // ⭐ MỚI
                };
                        dtBaNhat.Rows.Add(row);
                    }
                    dtBaNhat.EndLoadData();
                }

                if (kryptonDataGridView1 != null && !kryptonDataGridView1.IsDisposed)
                {
                    kryptonDataGridView1.SuspendLayout();
                    kryptonDataGridView1.DataSource = dtBaNhat;
                    DinhDangGiaoDienDataGridBaNhat();
                    NapDuLieuBoLoc(dtBaNhat);
                    ThongKeSoLuongBaNhat(false);
                    _tongQuanSoGoc = dtBaNhat.AsEnumerable().Count(r => r["ID"] != DBNull.Value && Convert.ToInt32(r["ID"]) != -1);
                    kryptonDataGridView1.ResumeLayout();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi nạp [{TenBangDanhSachBaNhat}]: {ex}");
            }
        }
        private string SafeGiaiMa(string cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText)) return "";
            try { return BaoMatAES.GiaiMa(cipherText).Trim(); }
            catch { return "[Lỗi giải mã]"; }
        }
        private void NapDuLieuBoLoc(DataTable dt)
        {
            // --- 1. Xử lý cho comboBox_TimKiemDonVi (Dữ liệu động) ---
            if (comboBox_TimKiemDonVi != null)
            {
                var donViSet = new HashSet<string>();
                foreach (DataRow row in dt.Rows)
                {
                    // Bỏ qua dòng đệm ảo (nếu bạn vẫn giữ cách dùng dòng ID = -1)
                    if (row["ID"] != DBNull.Value && Convert.ToInt32(row["ID"]) == -1) continue;

                    string donVi = row["DonVi"]?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(donVi))
                    {
                        donViSet.Add(donVi);
                    }
                }

                comboBox_TimKiemDonVi.SelectedIndexChanged -= comboBox_TimKiemDonVi_SelectedIndexChanged;
                comboBox_TimKiemDonVi.Items.Clear();
                comboBox_TimKiemDonVi.Items.Add("--- Tất cả ---");
                foreach (var dv in donViSet) comboBox_TimKiemDonVi.Items.Add(dv);
                comboBox_TimKiemDonVi.SelectedIndex = 0;
                comboBox_TimKiemDonVi.SelectedIndexChanged += comboBox_TimKiemDonVi_SelectedIndexChanged;
            }

            // --- 2. Xử lý cho comboBox1_DeNghi (Dữ liệu tĩnh) ---
            if (comboBox1_DeNghi != null)
            {
                // Giả sử bạn dùng chung hoặc có hàm sự kiện riêng (comboBox1_DeNghi_SelectedIndexChanged)
                // Nếu dùng chung hàm ThucHienLocDuLieu, bạn gán sự kiện tương tự ở đây
                comboBox1_DeNghi.SelectedIndexChanged -= comboBox_TimKiemDonVi_SelectedIndexChanged; // Nếu dùng chung event lọc

                comboBox1_DeNghi.Items.Clear();
                comboBox1_DeNghi.Items.AddRange(new object[] { "--- Tất cả ---", "Đề nghị", "Không" });
                comboBox1_DeNghi.SelectedIndex = 0;

                comboBox1_DeNghi.SelectedIndexChanged += comboBox_TimKiemDonVi_SelectedIndexChanged; // Gán lại event
            }
        } 
        private void ThucHienLocDuLieu()
        {
            if (kryptonDataGridView1.DataSource is DataTable dt)
            {
                List<string> dsDieuKien = new List<string>();

                // Lọc Tên
                string tenTimKiem = textBox_TimKiemTheoTen.Text.Trim().Replace("'", "''");
                if (!string.IsNullOrEmpty(tenTimKiem))
                    dsDieuKien.Add($"HoVaTen LIKE '%{tenTimKiem}%'");

                // Lọc Đơn Vị
                if (comboBox_TimKiemDonVi != null)
                {
                    string donViTimKiem = comboBox_TimKiemDonVi.Text.Trim();
                    if (!string.IsNullOrEmpty(donViTimKiem) && donViTimKiem != "--- Tất cả ---")
                        dsDieuKien.Add($"DonVi = '{donViTimKiem.Replace("'", "''")}'");
                }

                // Lọc Đề Nghị
                if (comboBox1_DeNghi != null)
                {
                    string deNghiLoc = comboBox1_DeNghi.Text.Trim();
                    if (deNghiLoc == "Đề nghị")
                        dsDieuKien.Add("DeNghi = 'X'");
                    else if (deNghiLoc == "Không")
                        dsDieuKien.Add("(DeNghi IS NULL OR DeNghi = '')");
                }

                // Áp dụng bộ lọc
                dt.DefaultView.RowFilter = string.Join(" AND ", dsDieuKien);

                // ==========================================================
                // ⭐ XỬ LÝ CẬP NHẬT TRẠNG THÁI TÌM KIẾM
                // ==========================================================
                if (toolStripStatusLabel1 != null)
                {
                    // Lấy số lượng thực tế đang hiển thị trên lưới (bỏ dòng mới cuối cùng)
                    int soKetQuaTimKiem = kryptonDataGridView1.Rows.Cast<DataGridViewRow>()
                                          .Count(r => !r.IsNewRow);

                    // Kiểm tra: Có đang áp dụng bộ lọc nào không?
                    bool dangLoc = dsDieuKien.Count > 0;

                    if (dangLoc)
                    {
                        // Nếu ĐANG lọc, hiển thị: Tổng quân số | Kết quả tìm kiếm
                        toolStripStatusLabel1.Text = $"Tổng quân số: {_tongQuanSoGoc} đồng chí | Kết quả tìm kiếm: {soKetQuaTimKiem} đồng chí";
                        toolStripStatusLabel1.ForeColor = System.Drawing.Color.Blue; // Đổi màu để làm nổi bật trạng thái tìm kiếm (tuỳ chọn)
                    }
                    else
                    {
                        // Nếu KHÔNG lọc (ô TextBox rỗng, Combobox chọn "Tất cả")
                        // Gọi lại hàm mặc định để trả về trạng thái hiển thị "Tổng cộng: ... đề nghị: ..."
                        toolStripStatusLabel1.ForeColor = System.Drawing.SystemColors.ControlText; // Trả lại màu mặc định
                        CapNhatTrangThaiDuoiNen();
                    }
                }

                // Vẫn gọi hàm này để cập nhật ngầm nếu bạn có dùng biến thống kê nào khác
                ThongKeSoLuongBaNhat(false);
            }
        }
        private void textBox_TimKiemTheoTen_TextChanged(object sender, EventArgs e)
        {
            ThucHienLocDuLieu();
        }
        // 4. Sự kiện Chọn tìm kiếm Đơn Vị
        private void comboBox_TimKiemDonVi_SelectedIndexChanged(object sender, EventArgs e)
        {
            ThucHienLocDuLieu();
        }
        // 5. Nút Làm mới/Xóa các ô tìm kiếm
        private void richTextBox1_ThanhTich_TextChanged(object sender, EventArgs e)
        {
            // Bắt buộc phải có Focused: Chỉ kích hoạt khi người dùng trực tiếp gõ vào ô.
            // Bỏ qua nếu sự kiện TextChanged phát sinh do hàm CellClick đổ dữ liệu lên.
            if (richTextBox1_ThanhTich.Focused)
            {
                if (!string.IsNullOrWhiteSpace(richTextBox1_ThanhTich.Text))
                {
                    // Nếu có gõ chữ (thành tích), tự động chọn Đề nghị
                    comboBox_DeNghiBaNhat.SelectedIndex = 1; // Item 1 tương đương "Đề nghị"
                }
                else
                {
                    // Tiện ích mở rộng: Nếu xóa sạch chữ, tự động trả Combobox về rỗng
                    comboBox_DeNghiBaNhat.SelectedIndex = 0; // Item 0 tương đương rỗng/không đề nghị
                }
            }
        }
        private void kryptonButton_LamMoiCacOTimKiem_Click(object sender, EventArgs e)
        {
            // 1. Xóa nội dung ô tìm kiếm tên
            if (textBox_TimKiemTheoTen != null)
                textBox_TimKiemTheoTen.Text = "";

            // 2. Đưa combobox Đơn vị về "--- Tất cả ---"
            if (comboBox_TimKiemDonVi != null && comboBox_TimKiemDonVi.Items.Count > 0)
                comboBox_TimKiemDonVi.SelectedIndex = 0;

            // ⭐ 3. Đưa combobox Đề nghị về "--- Tất cả ---"
            // Khi gán SelectedIndex = 0, nó sẽ tự động kích hoạt sự kiện SelectedIndexChanged 
            // và chạy hàm ThucHienLocDuLieu() giúp lưới hiển thị lại toàn bộ.
            if (comboBox1_DeNghi != null && comboBox1_DeNghi.Items.Count > 0)
                comboBox1_DeNghi.SelectedIndex = 0;
        }
        private void CapNhatSoLuongKyTu()
        {
            if (richTextBox1_ThanhTich == null || label17_SoLuongKyTu == null)
                return;

            int soKyTu = richTextBox1_ThanhTich.Text.Length;

            // 1. Rỗng (0 ký tự): Ẩn luôn Label
            if (soKyTu == 0)
            {
                label17_SoLuongKyTu.Visible = false;
            }
            else
            {
                // 2. Có chữ (> 0 ký tự): Hiển thị Label và cập nhật số lượng
                label17_SoLuongKyTu.Visible = true;
                label17_SoLuongKyTu.Text = $"Số lượng: {soKyTu} /3000 ký tự...";

                // 3. Xử lý màu sắc dựa trên mốc 3000
                if (soKyTu > 3000)
                {
                    // Vượt quá 3000 ký tự -> Màu Đỏ
                    label17_SoLuongKyTu.ForeColor = System.Drawing.Color.Red;
                }
                else
                {
                    // Từ 1 đến 3000 ký tự -> Màu Xanh (Dùng Color.Green để dễ đọc trên nền sáng)
                    label17_SoLuongKyTu.ForeColor = System.Drawing.Color.Green;
                }
            }
        }
        private void CapNhatTrangThaiDuoiNen()
        {
            // 1. Kiểm tra an toàn
            if (kryptonDataGridView1 == null || toolStripStatusLabel1 == null)
                return;

            // 2. Đếm tổng số dòng
            int tongSoDong = kryptonDataGridView1.Rows.Count;
            int soDeNghi = 0;

            // 3. Duyệt qua các dòng để đếm số lượng người có chữ "X" ở cột DeNghi
            foreach (DataGridViewRow row in kryptonDataGridView1.Rows)
            {
                // Vì dữ liệu trong Grid đã giải mã nên chỉ cần check bằng "X"
                string val = row.Cells["DeNghi"].Value?.ToString() ?? "";
                if (val.Trim().Equals("X", StringComparison.OrdinalIgnoreCase))
                {
                    soDeNghi++;
                }
            }

            // 4. Cập nhật lên thanh trạng thái theo điều kiện phân nhánh (MỚI)
            if (tongSoDong == 0)
            {
                toolStripStatusLabel1.Text = "Chưa có cán bộ, chiến sĩ nào được đề nghị biểu dương trong phong trào thi đua Ba Nhất";
            }
            else if (soDeNghi == 0)
            {
                toolStripStatusLabel1.Text = $"Tổng cộng: {tongSoDong} đồng chí.";
            }
            else
            {
                toolStripStatusLabel1.Text = $"Tổng cộng: {tongSoDong} đồng chí, đề nghị: {soDeNghi} đồng chí.";
            }
            // THÊM DÒNG NÀY VÀO ĐỂ TÍNH LẠI TỶ LỆ KHI CÓ NGƯỜI MỚI ĐƯỢC CHUYỂN SANG LOẠI 1:
            _ = Module_BaNhat.TinhToanVaHienThiTyLeBaNhatAsync(toolStripStatusLabel2_TyLeBaNhat);
        }
        private void DinhDangGiaoDienDataGridBaNhat()
        {
            if (kryptonDataGridView1 == null) return;

            Type dgvType = kryptonDataGridView1.GetType();
            System.Reflection.PropertyInfo pi = dgvType.GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            pi?.SetValue(kryptonDataGridView1, true, null);

            kryptonDataGridView1.AllowUserToAddRows = false;
            kryptonDataGridView1.AllowUserToDeleteRows = false;
            kryptonDataGridView1.AllowUserToResizeRows = false;
            kryptonDataGridView1.RowHeadersVisible = false;
            kryptonDataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            kryptonDataGridView1.MultiSelect = false;

            kryptonDataGridView1.GridStyles.Style = Krypton.Toolkit.DataGridViewStyle.List;

            kryptonDataGridView1.RowTemplate.Height = 36;
            kryptonDataGridView1.ColumnHeadersHeight = 60;
            kryptonDataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            kryptonDataGridView1.StateCommon.HeaderColumn.Content.Padding = new System.Windows.Forms.Padding(6, 8, 6, 8);
            kryptonDataGridView1.StateCommon.HeaderColumn.Content.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);

            kryptonDataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            kryptonDataGridView1.StateCommon.HeaderRow.Content.Padding = new System.Windows.Forms.Padding(6, 8, 6, 8);
            kryptonDataGridView1.StateCommon.HeaderRow.Content.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);

            kryptonDataGridView1.StateCommon.DataCell.Content.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);

            kryptonDataGridView1.StateCommon.DataCell.Border.Color1 = System.Drawing.Color.FromArgb(224, 224, 224);
            kryptonDataGridView1.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            kryptonDataGridView1.StateCommon.DataCell.Border.Width = 1;

            kryptonDataGridView1.StateSelected.DataCell.Back.Color1 = System.Drawing.Color.FromArgb(232, 244, 253);
            kryptonDataGridView1.StateSelected.DataCell.Back.Color2 = System.Drawing.Color.FromArgb(232, 244, 253);
            kryptonDataGridView1.StateSelected.DataCell.Content.Color1 = System.Drawing.Color.FromArgb(0, 102, 204);
            //kryptonDataGridView1.Padding = new Padding(0, 0, 0, 30); // Tạo khoảng đệm 30px dưới đáy lưới độc lập với dòng dữ liệu

            // Thay bằng dòng này (nếu cần khoảng trống):
            kryptonDataGridView1.Margin = new Padding(0, 0, 0, 30);
            if (kryptonDataGridView1.Columns.Count == 0) return;

            foreach (DataGridViewColumn col in kryptonDataGridView1.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            if (kryptonDataGridView1.Columns["ID"] != null) kryptonDataGridView1.Columns["ID"].Visible = false;

            if (kryptonDataGridView1.Columns["STT"] != null)
            {
                kryptonDataGridView1.Columns["STT"].Visible = true;
                kryptonDataGridView1.Columns["STT"].HeaderText = "STT";
                kryptonDataGridView1.Columns["STT"].Width = 35;
                kryptonDataGridView1.Columns["STT"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                kryptonDataGridView1.Columns["STT"].ReadOnly = true;
            }

            if (kryptonDataGridView1.Columns["HoVaTen"] != null)
            {
                kryptonDataGridView1.Columns["HoVaTen"].HeaderText = "Họ và tên";
                kryptonDataGridView1.Columns["HoVaTen"].Width = 215;
                kryptonDataGridView1.Columns["HoVaTen"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                kryptonDataGridView1.Columns["HoVaTen"].ReadOnly = true;
            }

            if (kryptonDataGridView1.Columns["SoHieu"] != null)
            {
                kryptonDataGridView1.Columns["SoHieu"].HeaderText = "Số hiệu";
                kryptonDataGridView1.Columns["SoHieu"].Width = 90;
                kryptonDataGridView1.Columns["SoHieu"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                kryptonDataGridView1.Columns["SoHieu"].ReadOnly = true;
            }

            if (kryptonDataGridView1.Columns["NamSinh"] != null)
            {
                kryptonDataGridView1.Columns["NamSinh"].HeaderText = "Năm sinh";
                kryptonDataGridView1.Columns["NamSinh"].Width = 80;
                kryptonDataGridView1.Columns["NamSinh"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                kryptonDataGridView1.Columns["NamSinh"].ReadOnly = true;
            }

            if (kryptonDataGridView1.Columns["QueQuan"] != null)
            {
                kryptonDataGridView1.Columns["QueQuan"].HeaderText = "Quê quán";
                kryptonDataGridView1.Columns["QueQuan"].Width = 210;
                kryptonDataGridView1.Columns["QueQuan"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                kryptonDataGridView1.Columns["QueQuan"].ReadOnly = true;
            }

            if (kryptonDataGridView1.Columns["NgayVaoCAND"] != null)
            {
                kryptonDataGridView1.Columns["NgayVaoCAND"].HeaderText = "Vào CAND";
                kryptonDataGridView1.Columns["NgayVaoCAND"].Width = 120;
                kryptonDataGridView1.Columns["NgayVaoCAND"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                kryptonDataGridView1.Columns["NgayVaoCAND"].ReadOnly = true;
            }

            if (kryptonDataGridView1.Columns["CapBac"] != null)
            {
                kryptonDataGridView1.Columns["CapBac"].HeaderText = "Cấp bậc";
                kryptonDataGridView1.Columns["CapBac"].Width = 90;
                kryptonDataGridView1.Columns["CapBac"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                kryptonDataGridView1.Columns["CapBac"].ReadOnly = true;
            }

            if (kryptonDataGridView1.Columns["ChucVu"] != null)
            {
                kryptonDataGridView1.Columns["ChucVu"].HeaderText = "Chức vụ";
                kryptonDataGridView1.Columns["ChucVu"].Width = 110;
                kryptonDataGridView1.Columns["ChucVu"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                kryptonDataGridView1.Columns["ChucVu"].ReadOnly = true;
            }
            // Bổ sung vào bên trong hàm DinhDangGiaoDienDataGridBaNhat()
            // Căn giữa cột Cấp bậc
            if (kryptonDataGridView1.Columns.Contains("CapBac"))
            {
                // Căn giữa nội dung của các ô dữ liệu
                kryptonDataGridView1.Columns["CapBac"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                // Căn giữa chữ trên tiêu đề cột (Header)
                kryptonDataGridView1.Columns["CapBac"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            // Căn giữa cột Chức vụ
            if (kryptonDataGridView1.Columns.Contains("ChucVu"))
            {
                // Căn giữa nội dung của các ô dữ liệu
                kryptonDataGridView1.Columns["ChucVu"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                // Căn giữa chữ trên tiêu đề cột (Header)
                kryptonDataGridView1.Columns["ChucVu"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            if (kryptonDataGridView1.Columns["DonVi"] != null)
            {
                kryptonDataGridView1.Columns["DonVi"].HeaderText = "Đơn vị";
                kryptonDataGridView1.Columns["DonVi"].Width = 85;
                kryptonDataGridView1.Columns["DonVi"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                kryptonDataGridView1.Columns["DonVi"].ReadOnly = true;
            }
            if (kryptonDataGridView1.Columns["PhanLoai"] != null)
            {
                kryptonDataGridView1.Columns["PhanLoai"].HeaderText = "Phân loại";
                kryptonDataGridView1.Columns["PhanLoai"].Width = 90;
                kryptonDataGridView1.Columns["PhanLoai"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                kryptonDataGridView1.Columns["PhanLoai"].ReadOnly = true;
            }
            if (kryptonDataGridView1.Columns["GhiChu"] != null)
            {
                kryptonDataGridView1.Columns["GhiChu"].HeaderText = "Ghi chú";
                kryptonDataGridView1.Columns["GhiChu"].ReadOnly = true;
                kryptonDataGridView1.Columns["GhiChu"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                kryptonDataGridView1.Columns["GhiChu"].FillWeight = 50;
                kryptonDataGridView1.Columns["GhiChu"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                kryptonDataGridView1.Columns["GhiChu"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            }

            if (kryptonDataGridView1.Columns["DeNghi"] != null)
            {
                kryptonDataGridView1.Columns["DeNghi"].HeaderText = "Đề nghị";
                kryptonDataGridView1.Columns["DeNghi"].Width = 70;
                kryptonDataGridView1.Columns["DeNghi"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                kryptonDataGridView1.Columns["DeNghi"].ReadOnly = false;
            }

            if (kryptonDataGridView1.Columns["ThanhTich"] != null)
            {
                kryptonDataGridView1.Columns["ThanhTich"].HeaderText = "Thành tích";
                kryptonDataGridView1.Columns["ThanhTich"].ReadOnly = false;
                kryptonDataGridView1.Columns["ThanhTich"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                kryptonDataGridView1.Columns["ThanhTich"].FillWeight = 50;
                kryptonDataGridView1.Columns["ThanhTich"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                kryptonDataGridView1.Columns["ThanhTich"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                kryptonDataGridView1.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                //kryptonDataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
                // Thay thế AutoSizeRowsMode = AllCells bằng:
                kryptonDataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
                kryptonDataGridView1.RowTemplate.Height = 36;
            }
            // ⭐ MỚI: Hiển thị cột Tình trạng
            if (kryptonDataGridView1.Columns["TinhTrang"] != null)
            {
                kryptonDataGridView1.Columns["TinhTrang"].HeaderText = "Tình trạng";
                kryptonDataGridView1.Columns["TinhTrang"].Width = 120;
                kryptonDataGridView1.Columns["TinhTrang"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                kryptonDataGridView1.Columns["TinhTrang"].ReadOnly = true;
            }
        }
        private void kryptonDataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = kryptonDataGridView1.Rows[e.RowIndex];

            textBox_SoHieu.Tag = row.Cells["ID"].Value;

            textBox_STT.Text = row.Cells["STT"].Value?.ToString();
            textBox_HoVaTen.Text = row.Cells["HoVaTen"].Value?.ToString();
            textBox_SoHieu.Text = row.Cells["SoHieu"].Value?.ToString();
            textBox_NamSinh.Text = row.Cells["NamSinh"].Value?.ToString();
            textBox_QueQuan.Text = row.Cells["QueQuan"].Value?.ToString();
            textBox_NgayVaoCAND.Text = row.Cells["NgayVaoCAND"].Value?.ToString();
            kryptonTextBox1_CapBac.Text = row.Cells["CapBac"].Value?.ToString();
            kryptonTextBox1_ChucVu.Text = row.Cells["ChucVu"].Value?.ToString();
            kryptonTextBox1_DonVi.Text = row.Cells["DonVi"].Value?.ToString();
            kryptonTextBox1_PhanLoai.Text = row.Cells["PhanLoai"].Value?.ToString();
            textBox_GhiChu.Text = row.Cells["GhiChu"].Value?.ToString();
            richTextBox1_ThanhTich.Text = row.Cells["ThanhTich"].Value?.ToString();
            // (Bổ sung vào gần chỗ lấy GhiChu, ThanhTich)
            string tinhTrangDb = row.Cells["TinhTrang"].Value?.ToString() ?? "";
            // Nếu bạn có ô TextBox cho Tình Trạng, mở khóa dòng bên dưới:
            // textBox_TinhTrang.Text = tinhTrangDb;
            string deNghiDb = row.Cells["DeNghi"].Value?.ToString() ?? "";
            if (deNghiDb.Trim().Equals("X", StringComparison.OrdinalIgnoreCase))
            {
                comboBox_DeNghiBaNhat.Text = "Đề nghị";
            }
            else
            {
                comboBox_DeNghiBaNhat.Text = "";
            }
        }
        private async void kryptonButton_LuuDataDeNghi_Click(object sender, EventArgs e)
        {
            // Kiểm tra đã chọn CBCS chưa
            if (textBox_SoHieu.Tag == null ||
                !int.TryParse(textBox_SoHieu.Tag.ToString(), out int idBaNhat))
            {
                MessageBox.Show(
                    "Vui lòng chọn một CBCS trong danh sách trước khi lưu!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            // Kiểm tra giới hạn số lượng ký tự trước khi lưu
            int soKyTu = richTextBox1_ThanhTich.TextLength;

            if (soKyTu > Module_BaNhat.GioiHanToiDa)
            {
                MessageBox.Show(
                    $"Nội dung thành tích hiện có {soKyTu} ký tự,\nvượt quá giới hạn cho phép ({Module_BaNhat.GioiHanToiDa} ký tự).\nVui lòng rút gọn nội dung trước khi lưu.",
                    "Không thể lưu dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                richTextBox1_ThanhTich.Focus();
                return;
            }
            // ⭐ LƯU TÊN GỐC, ĐỔI TÊN NÚT VÀ ÉP VẼ LẠI GIAO DIỆN
            string tenGoc = kryptonButton_LuuDataDeNghi.Text;
            kryptonButton_LuuDataDeNghi.Text = "Đang lưu...";
            kryptonButton_LuuDataDeNghi.Enabled = false; // Tạm khóa nút để tránh click đúp
            kryptonButton_LuuDataDeNghi.Refresh();       // Ép giao diện vẽ lại tên nút ngay lập tức
            try
            {

                // CHUẨN BỊ DỮ LIỆU & TỐI ƯU UX


                // ⭐ NẾU COMBOBOX RỖNG -> XÓA SẠCH TEXTBOX THÀNH TÍCH TRƯỚC KHI LƯU
                if (string.IsNullOrWhiteSpace(comboBox_DeNghiBaNhat.Text))
                {
                    richTextBox1_ThanhTich.Clear(); // Xóa trực tiếp trên giao diện UI
                }

                string deNghi = comboBox_DeNghiBaNhat.Text.Trim()
                    .Equals("Đề nghị", StringComparison.OrdinalIgnoreCase)
                    ? "X"
                    : "";

                // Nếu RichTextBox vừa bị Clear() ở bước trên, biến này sẽ tự động nhận chuỗi rỗng ""
                string thanhTich = richTextBox1_ThanhTich.Text.Trim();


                // Mã hóa dữ liệu

                string deNghiEnc = BaoMatAES.MaHoa(deNghi);
                string thanhTichEnc = BaoMatAES.MaHoa(thanhTich);


                // Cập nhật CSDL

                using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                //    cmd.CommandText = @"
                //UPDATE DanhSachBaNhat 
                //SET 
                //    DeNghi = @DeNghi, 
                //    ThanhTich = @ThanhTich 
                //WHERE ID = @ID;";
                cmd.CommandText = $@"
    UPDATE [{TenBangDanhSachBaNhat}]
    SET
        DeNghi = @DeNghi,
        ThanhTich = @ThanhTich
    WHERE ID = @ID;";
                cmd.Parameters.AddWithValue("@DeNghi", deNghiEnc);
                cmd.Parameters.AddWithValue("@ThanhTich", thanhTichEnc);
                cmd.Parameters.AddWithValue("@ID", idBaNhat);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    MessageBox.Show(
                        "Không tìm thấy CBCS cần cập nhật.",
                        "Lỗi",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }


                // Đồng bộ DataGridView

                foreach (DataGridViewRow row in kryptonDataGridView1.Rows)
                {
                    if (row.IsNewRow || row.Cells["ID"].Value == null)
                        continue;

                    if (Convert.ToInt32(row.Cells["ID"].Value) == idBaNhat)
                    {
                        row.Cells["DeNghi"].Value = deNghi;
                        row.Cells["ThanhTich"].Value = thanhTich; // Đồng bộ luôn thành tích (có thể đã rỗng) lên DataGrid
                        break;
                    }
                }


                // Cập nhật thống kê và StatusLabel

                if (toolStripStatusLabel1 != null)
                {
                    toolStripStatusLabel1.Text = "Đã lưu thành công!";
                    // Ép vẽ lại thanh trạng thái
                    if (toolStripStatusLabel1.Owner != null)
                        toolStripStatusLabel1.Owner.Refresh();
                }
                // Dừng 200 mili-giây để người dùng kịp đọc thông báo "Đã lưu thành công"
                await Task.Delay(200);
                // Gọi lại hàm thống kê để trả dòng chữ Tổng cộng về bình thường
                ThongKeSoLuongBaNhat(false);
                CapNhatTrangThaiDuoiNen();
                // THÊM DÒNG NÀY VÀO ĐỂ TÍNH LẠI TỶ LỆ KHI CÓ NGƯỜI MỚI ĐƯỢC CHUYỂN SANG LOẠI 1:
                _ = Module_BaNhat.TinhToanVaHienThiTyLeBaNhatAsync(toolStripStatusLabel2_TyLeBaNhat);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);

                MessageBox.Show(
                    "Lỗi khi lưu dữ liệu:\n\n" + ex.Message,
                    "Lỗi hệ thống",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                // ⭐ TRẢ LẠI TÊN GỐC VÀ MỞ KHÓA NÚT DÙ THÀNH CÔNG HAY THẤT BẠI
                kryptonButton_LuuDataDeNghi.Text = tenGoc;
                kryptonButton_LuuDataDeNghi.Enabled = true;
                kryptonButton_LuuDataDeNghi.Refresh();
            }
        }
        private void kryptonButton1_Thoat_Click(object sender, EventArgs e)
        {
            // 1. Tìm Form6 (để hiển thị lên)
            Form6_XuLyData form6 = Application.OpenForms.OfType<Form6_XuLyData>().FirstOrDefault();

            if (form6 != null)
            {
                form6.Show();
                form6.BringToFront();
            }
            else
            {
                form6 = new Form6_XuLyData();
                form6.Show();
            }

            // 2. Tìm Form2 đang chạy và cập nhật tiêu đề
            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            if (formCha != null)
            {
                formCha.CapNhatTieuDe("Trang phân loại thi đua");
            }

            // 3. Đóng form hiện tại
            this.Close();
        }
        // Cờ kiểm soát tiến trình làm mới dữ liệu
        private bool _dangLamMoiCSDL = false;
        private async void kryptonButton_RefershCSDL_Click(object sender, EventArgs e)
        {
            // Chặn click kép khi đang chạy
            if (_dangLamMoiCSDL)
                return;

            _dangLamMoiCSDL = true;

            string tenNutGoc = kryptonButton_RefershCSDL.Text;

            kryptonButton_RefershCSDL.Text = "Đang chạy...";
            kryptonButton_RefershCSDL.Enabled = false;

            if (toolStripStatusLabel1 != null)
            {
                toolStripStatusLabel1.Text =
                    "Đang làm mới dữ liệu, vui lòng chờ...";

                toolStripStatusLabel1.Owner?.Update();
            }
            CancellationTokenSource cts = new CancellationTokenSource();
            try
            {
                // CẤU HÌNH PROGRESS BAR
                if (toolStripProgressBar1_LamMoi != null)
                {
                    toolStripProgressBar1_LamMoi.AutoSize = false;
                    toolStripProgressBar1_LamMoi.Size =
                        new Size(150, 16);
                    toolStripProgressBar1_LamMoi.Visible = true;
                    toolStripProgressBar1_LamMoi.Value = 0;
                }
                Cursor.Current = Cursors.WaitCursor;

                // CHẠY HIỆU ỨNG TIẾN TRÌNH (ĐANG CHỜ DỮ LIỆU)
                IProgress<int> progressReporter = new Progress<int>(val =>
                {
                    if (toolStripProgressBar1_LamMoi != null && !toolStripProgressBar1_LamMoi.IsDisposed)
                    {
                        toolStripProgressBar1_LamMoi.Value = val;
                    }
                });
                Task progressTask = Task.Run(async () =>
                {
                    int value = 0;
                    while (!cts.Token.IsCancellationRequested &&
                           value < 90)
                    {
                        // Giảm Delay và bước nhảy để thanh progress chạy mượt (Smooth) hơn
                        await Task.Delay(50);
                        value += 2;

                        if (value > 90)
                            value = 90;
                        // Đẩy tín hiệu về UI Thread an toàn
                        progressReporter.Report(value);
                    }

                }, cts.Token);

                // XỬ LÝ DỮ LIỆU CHÍNH
                await DongBoDuLieuLoai1SangBaNhatAsync();
                // Bổ sung dòng gọi hàm đối chiếu ở giữa:
                await Module_BaNhat.CapNhatTinhTrangSoVangAsync();
                await LoadDuLieuToanBoDanhSachBaNhatAsync();
                // Hủy Task lặp nền để chuyển sang hiệu ứng chốt hạ
                cts.Cancel();

                // 🌟 HIỆU ỨNG TRƯỢT MƯỢT MÀ LÊN 100% (SMOOTH FINISH)
                if (toolStripProgressBar1_LamMoi != null)
                {
                    int hienTai = toolStripProgressBar1_LamMoi.Value;
                    // Chạy nốt từ điểm hiện tại lên 100 rất nhanh để tạo cảm giác mượt mắt
                    for (int i = hienTai; i <= 100; i += 3)
                    {
                        toolStripProgressBar1_LamMoi.Value = Math.Min(100, i);
                        await Task.Delay(10); // Khựng cực ngắn để mắt người kịp thấy nó chạy
                    }
                    toolStripProgressBar1_LamMoi.Value = 100; // Chốt hạ cứng ở 100%
                }

                Cursor.Current = Cursors.Default;
                ThongKeSoLuongBaNhat(true);
                CapNhatTrangThaiDuoiNen();

                // Giữ lại trạng thái 100% một nhịp để user nhìn thấy sự trọn vẹn trước khi ẩn đi
                await Task.Delay(300);

                ThongKeSoLuongBaNhat(false);
            }
            catch (Exception ex)
            {
                Cursor.Current = Cursors.Default;

                if (toolStripStatusLabel1 != null)
                {
                    toolStripStatusLabel1.Text =
                        "Lỗi khi làm mới dữ liệu!";
                }
                MessageBox.Show(
                    "Lỗi khi làm mới dữ liệu:\n\n" + ex.Message,
                    "Lỗi hệ thống",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                cts.Dispose();
                Cursor.Current = Cursors.Default;
                if (toolStripProgressBar1_LamMoi != null)
                {
                    toolStripProgressBar1_LamMoi.Visible = false;
                    toolStripProgressBar1_LamMoi.Value = 0;
                }
                kryptonButton_RefershCSDL.Text = tenNutGoc;
                kryptonButton_RefershCSDL.Enabled = true;
                _dangLamMoiCSDL = false;
                // Đồng bộ lại trạng thái cuối cùng
                ThongKeSoLuongBaNhat(false);
                CapNhatTrangThaiDuoiNen();
                // THÊM DÒNG NÀY VÀO ĐỂ TÍNH LẠI TỶ LỆ KHI CÓ NGƯỜI MỚI ĐƯỢC CHUYỂN SANG LOẠI 1:
                _ = Module_BaNhat.TinhToanVaHienThiTyLeBaNhatAsync(toolStripStatusLabel2_TyLeBaNhat);
            }
        }
        private void lamMoiHeThong_Click(object sender, EventArgs e)
       => kryptonButton_RefershCSDL.PerformClick();
        private void xoaTimKiem_Click(object sender, EventArgs e)
            => kryptonButton_LamMoiCacOTimKiem.PerformClick();
        private void kryptonButton1_ThanhTichTapThe_Click(object sender, EventArgs e)
        {
            const string tenGocCuaNut = "Thành tích tập thể";
            // 1. Hiển thị trạng thái NGAY LẬP TỨC và TỐI ƯU HIỆU SUẤT
            if (toolStripStatusLabel1 != null)
            {
                toolStripStatusLabel1.Text = "Đang mở trang báo cáo tóm tắt kết quả thi đua tập thể phong trào Ba Nhất...";

                // Thay Refresh() bằng Invalidate + Update để tránh Repaint toàn bộ
                toolStripStatusLabel1.Owner?.Invalidate();
                toolStripStatusLabel1.Owner?.Update();
            }
            // 2. Kiểm tra Form đã tồn tại chưa
            Form frm = Application.OpenForms["Form43_TomTatThanhTichBaNhat"];

            if (frm != null)
            {
                kryptonButton1_ThanhTichTapThe.Text = "Trang đang mở...";

                if (frm.WindowState == FormWindowState.Minimized)
                    frm.WindowState = FormWindowState.Normal;

                frm.Activate();
                frm.Focus();
            }
            else
            {
                kryptonButton1_ThanhTichTapThe.Text = "Trang đang mở...";
                var newForm = new Form43_TomTatThanhTichBaNhat
                {
                    StartPosition = FormStartPosition.CenterScreen
                };
                // 3. LẬP TRÌNH HƯỚNG SỰ KIỆN: Khi form này đóng lại, tự động phục hồi UI
                newForm.FormClosed += (s, ev) =>
                {
                    // Trả lại tên nút ban đầu
                    kryptonButton1_ThanhTichTapThe.Text = tenGocCuaNut;

                    // TRẢ LẠI THÔNG TIN SỐ LƯỢNG CHO STATUS STRIP NGAY KHI ĐÓNG FORM
                    ThongKeSoLuongBaNhat(false);
                    CapNhatTrangThaiDuoiNen();
                };
                // Mở form lên ngay lập tức
                //newForm.Show();
                // Mở dạng hộp thoại Modal, khóa Form nền
                newForm.ShowDialog(this);
            }
            // (Đã xóa bỏ hoàn toàn await Task.Delay để không cản trở luồng xử lý)
        }
        private void toolStripMenuItem_ThiDuaTapThe_Click(object sender, EventArgs e)
        {
            kryptonButton1_ThanhTichTapThe.PerformClick();
        }
        private void toolStripMenuItem_ThoatTrang_Click(object sender, EventArgs e)
        {
            kryptonButton1_Thoat.PerformClick();
        }
        private async void toolStripMenuItem_XoaChonTatCa_Click(object sender, EventArgs e)
        {
            // 1. GỌI FORM XÁC MINH QUYỀN ADMIN TẠI ĐÂY
            DialogResult kq;
            using (Form24_XacMinhAdmin frm = new Form24_XacMinhAdmin())
            {
                frm.TopMost = true;
                frm.StartPosition = FormStartPosition.CenterScreen;
                kq = frm.ShowDialog();
            }
            if (kq != DialogResult.OK)
                return; // Dừng tiến trình nếu nhập sai mật khẩu hoặc bấm Hủy
            try
            {
                // 3. Mở kết nối và thực thi lệnh xóa
                using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                // Xóa toàn bộ các dòng trong bảng
                //cmd.CommandText = "DELETE FROM DanhSachBaNhat;";
                cmd.CommandText = $"DELETE FROM [{TenBangDanhSachBaNhat}];";
                await cmd.ExecuteNonQueryAsync();
                // Reset lại bộ đếm ID (AUTOINCREMENT) về 0
                // Dùng DELETE an toàn và chuẩn xác hơn UPDATE theo đúng logic mẫu của bạn
                //cmd.CommandText = "DELETE FROM sqlite_sequence WHERE name='DanhSachBaNhat';";
                cmd.CommandText = $"DELETE FROM sqlite_sequence WHERE name='{TenBangDanhSachBaNhat}';";
                try
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                catch
                {
                    // Bỏ qua lỗi nếu bảng chưa từng có dữ liệu nên chưa sinh ra sequence
                }

                // Dọn dẹp không gian DB (Kế thừa từ logic chuẩn của bạn)
                cmd.CommandText = "VACUUM;";
                try { await cmd.ExecuteNonQueryAsync(); } catch { }

                // 4. Gọi lại 2 hàm đồng bộ và nạp dữ liệu như bạn yêu cầu
                await DongBoDuLieuLoai1SangBaNhatAsync();
                await LoadDuLieuToanBoDanhSachBaNhatAsync();
                if (toolStripStatusLabel1 != null)
                {
                    toolStripStatusLabel1.Text = "Đã xóa và đồng bộ lại dữ liệu thành công!";
                    // Ép vẽ lại thanh trạng thái
                    if (toolStripStatusLabel1.Owner != null)
                        toolStripStatusLabel1.Owner.Refresh();
                }
                // Dừng 1.5 giây để người dùng kịp đọc thông báo "Đã lưu thành công"
                await Task.Delay(200);
                // Gọi lại hàm thống kê để trả dòng chữ Tổng cộng về bình thường
                ThongKeSoLuongBaNhat(false);
                CapNhatTrangThaiDuoiNen();      
                //MessageBox.Show("Đã xóa và đồng bộ lại dữ liệu thành công!", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // 5. Ghi nhật ký hành động xóa
                Module_NhatKy.GhiNhatKy(
                    taiKhoan: string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM) ? "Không xác định" : Module_TaiKhoan.TenTaiKhoan_RAM,
                    hanhDong: "Xóa toàn bộ dữ liệu bảng Danh sách Ba Nhất",
                    ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                );
                lamMoiHeThong.PerformClick();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi khi xóa toàn bộ dữ liệu Ba Nhất: " + ex.Message);
                MessageBox.Show("Đã xảy ra lỗi khi xóa dữ liệu:\n" + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async void toolStripMenuItem_luuVaoSoVang_Click(object sender, EventArgs e)
        {
            try
            {
                using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                await conn.OpenAsync();

                // 1. LẤY TẤT CẢ DỮ LIỆU (KHÔNG LỌC Ở SQL NỮA)
                //string sqlSelect = "SELECT * FROM DanhSachBaNhat";
                string sqlSelect = $"SELECT * FROM [{TenBangDanhSachBaNhat}]";
                DataTable dtTatCa = new DataTable();
                using (var cmdSelect = new SqliteCommand(sqlSelect, conn))
                {
                    using var reader = await cmdSelect.ExecuteReaderAsync();
                    dtTatCa.Load(reader);
                }

                // 2. LỌC DỮ LIỆU TRÊN BỘ NHỚ RAM (ĐÃ GIẢI MÃ)
                List<DataRow> dsDeNghi = new List<DataRow>();
                foreach (DataRow row in dtTatCa.Rows)
                {
                    // Giải mã cột DeNghi để kiểm tra
                    string rawDeNghi = row["DeNghi"]?.ToString() ?? "";
                    string decodedDeNghi = string.IsNullOrWhiteSpace(rawDeNghi) ? "" : BaoMatAES.GiaiMa(rawDeNghi).Trim();

                    if (decodedDeNghi.Equals("X", StringComparison.OrdinalIgnoreCase))
                    {
                        dsDeNghi.Add(row);
                    }
                }
                // 3. THÔNG BÁO CHO NGƯỜI DÙNG
                int count = dsDeNghi.Count;
                if (count == 0)
                {
                    MessageBox.Show("Không tìm thấy đồng chí nào được đánh dấu 'Đề nghị' (X)!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"Tìm thấy {count} đồng chí được đề nghị vào Sổ vàng.\nBạn có muốn tiếp tục ghi tên vào Sổ vàng?",
                    "Xác nhận",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes) return;

                // 4. THỰC HIỆN VÒNG LẶP XỬ LÝ
                int countSuccess = 0;
                foreach (DataRow row in dsDeNghi)
                {
                    // Lấy dữ liệu mã hóa từ dòng đã lọc
                    string soHieuEnc = row["SoHieu"].ToString();

                    // 🌟 SỬA ĐỊNH TUYẾN 1: Kiểm tra tồn tại trong Sổ vàng bảng động
                    string sqlCheck = $"SELECT COUNT(*) FROM [{TenBangSoVangHienTai}] WHERE SoHieu = @SoHieu";
                    using var cmdCheck = new SqliteCommand(sqlCheck, conn);
                    cmdCheck.Parameters.AddWithValue("@SoHieu", soHieuEnc);
                    long tonTai = (long)(await cmdCheck.ExecuteScalarAsync() ?? 0);

                    if (tonTai > 0)
                    {
                        // 🌟 SỬA ĐỊNH TUYẾN 2: Cập nhật bảng động
                        string sqlUpdate = $@"UPDATE [{TenBangSoVangHienTai}] SET 
                            HoVaTen=@HoVaTen, NamSinh=@NamSinh, QueQuan=@QueQuan, NgayVaoCAND=@NgayVaoCAND, 
                            CapBac=@CapBac, ChucVu=@ChucVu, DonVi=@DonVi, PhanLoai=@PhanLoai, 
                            GhiChu=@GhiChu, ThanhTich=@ThanhTich 
                            WHERE SoHieu = @SoHieu";

                        using var cmdUpdate = new SqliteCommand(sqlUpdate, conn);
                        cmdUpdate.Parameters.AddWithValue("@HoVaTen", row["HoVaTen"]);
                        cmdUpdate.Parameters.AddWithValue("@NamSinh", row["NamSinh"]);
                        cmdUpdate.Parameters.AddWithValue("@QueQuan", row["QueQuan"]);
                        cmdUpdate.Parameters.AddWithValue("@NgayVaoCAND", row["NgayVaoCAND"]);
                        cmdUpdate.Parameters.AddWithValue("@CapBac", row["CapBac"]);
                        cmdUpdate.Parameters.AddWithValue("@ChucVu", row["ChucVu"]);
                        cmdUpdate.Parameters.AddWithValue("@DonVi", row["DonVi"]);
                        cmdUpdate.Parameters.AddWithValue("@PhanLoai", row["PhanLoai"]);
                        cmdUpdate.Parameters.AddWithValue("@GhiChu", row["GhiChu"]);
                        cmdUpdate.Parameters.AddWithValue("@ThanhTich", row["ThanhTich"]);
                        cmdUpdate.Parameters.AddWithValue("@SoHieu", soHieuEnc);
                        await cmdUpdate.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        // 🌟 SỬA ĐỊNH TUYẾN 3: Thêm mới bảng động
                        string sqlInsert = $@"INSERT INTO [{TenBangSoVangHienTai}] 
                            (STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu, ThanhTich, ThongBaoTrungDoan, SoTTTrongSo, ThangCongNhan) 
                            VALUES ((SELECT IFNULL(MAX(STT),0)+1 FROM [{TenBangSoVangHienTai}]), 
                            @HoVaTen, @SoHieu, @NamSinh, @QueQuan, @NgayVaoCAND, @CapBac, @ChucVu, @DonVi, @PhanLoai, @GhiChu, @ThanhTich, @TB, @STT, @TCN)";

                        using var cmdInsert = new SqliteCommand(sqlInsert, conn);
                        cmdInsert.Parameters.AddWithValue("@HoVaTen", row["HoVaTen"]);
                        cmdInsert.Parameters.AddWithValue("@SoHieu", soHieuEnc);
                        cmdInsert.Parameters.AddWithValue("@NamSinh", row["NamSinh"]);
                        cmdInsert.Parameters.AddWithValue("@QueQuan", row["QueQuan"]);
                        cmdInsert.Parameters.AddWithValue("@NgayVaoCAND", row["NgayVaoCAND"]);
                        cmdInsert.Parameters.AddWithValue("@CapBac", row["CapBac"]);
                        cmdInsert.Parameters.AddWithValue("@ChucVu", row["ChucVu"]);
                        cmdInsert.Parameters.AddWithValue("@DonVi", row["DonVi"]);
                        cmdInsert.Parameters.AddWithValue("@PhanLoai", row["PhanLoai"]);
                        cmdInsert.Parameters.AddWithValue("@GhiChu", row["GhiChu"]);
                        cmdInsert.Parameters.AddWithValue("@ThanhTich", row["ThanhTich"]);
                        cmdInsert.Parameters.AddWithValue("@TB", BaoMatAES.MaHoa(""));
                        cmdInsert.Parameters.AddWithValue("@STT", BaoMatAES.MaHoa(""));
                        cmdInsert.Parameters.AddWithValue("@TCN", BaoMatAES.MaHoa(""));

                        await cmdInsert.ExecuteNonQueryAsync();
                    }
                    countSuccess++;
                }

                if (toolStripStatusLabel1 != null)
                {
                    toolStripStatusLabel1.Text = $"Đã chuyển thành công {countSuccess} đồng chí vào Sổ vàng [{TenBangSoVangHienTai}]!";
                    // Ép vẽ lại thanh trạng thái
                    if (toolStripStatusLabel1.Owner != null)
                        toolStripStatusLabel1.Owner.Refresh();
                }
                await Module_BaNhat.CapNhatTinhTrangSoVangAsync();
                // Dừng 1.5 giây để người dùng kịp đọc thông báo "Đã lưu thành công"
                await Task.Delay(800);
                // Gọi lại hàm thống kê để trả dòng chữ Tổng cộng về bình thường
                ThongKeSoLuongBaNhat(false);
                Module_NhatKy.GhiNhatKy("System", $"Xuất hàng loạt {countSuccess} CBCS vào Sổ vàng ({TenBangSoVangHienTai})", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }
        private async void toolStripMenuItem_MoSoVang_Click(object sender, EventArgs e)
        {
            kryptonButton1_MoSoVang.PerformClick();
        }
        private async void kryptonButton1_MoSoVang_Click(object sender, EventArgs e)
        {
            // Tùy chọn: Chặn nếu dữ liệu chưa sẵn sàng (giống Form42)
            // if (!KiemTraDuLieuSanSang("quản lý Sổ vàng Ba Nhất")) return;
            // 1. Tìm Form cha (Form2_FormCha) đang mở trong bộ nhớ ứng dụng
            var formCha = Application.OpenForms
                .OfType<Form2_FormCha>()
                .FirstOrDefault();

            if (formCha == null) return;

            // 2. Tìm Panel trung gian chứa các Form con (PanelContainer) trên Form cha
            var panel = formCha.Controls
                .Find("PanelContainer", true)
                .FirstOrDefault() as Panel;

            if (panel == null) return;

            // 3. Ẩn tất cả các Form con hiện tại đang hiển thị trong panel để giải phóng vùng nhìn
            foreach (System.Windows.Forms.Control ctl in panel.Controls)
            {
                if (ctl is Form frm)
                    frm.Hide();
            }

            // 4. KIỂM TRA: Xem Form44_SoVangBaNhat đã từng được nhúng vào Panel này chưa
            var form44 = panel.Controls
                .OfType<Form44_SoVangBaNhat>()
                .FirstOrDefault();

            // 5. Nếu chưa từng tồn tại -> Khởi tạo và "ép" nó thành Control con
            if (form44 == null)
            {
                form44 = new Form44_SoVangBaNhat
                {
                    TopLevel = false, // RẤT QUAN TRỌNG: Loại bỏ tính chất cửa sổ độc lập
                    FormBorderStyle = FormBorderStyle.None, // Bỏ viền Form
                    Dock = DockStyle.Fill, // Phóng to lấp đầy Panel
                    Text = "Quản lý Sổ vàng Ba Nhất"
                };

                // XỬ LÝ SỰ KIỆN ĐÓNG: Trả lại giao diện làm việc mặc định (Form6)
                form44.FormClosed += (s, ev) =>
                {
                    if (panel.IsDisposed) return;

                    var f6 = panel.Controls
                        .OfType<Form6_XuLyData>()
                        .FirstOrDefault();

                    if (f6 != null && !f6.IsDisposed)
                    {
                        f6.Dock = DockStyle.Fill;
                        f6.Show();
                        f6.BringToFront();
                    }
                };

                // Gắn Form44 vào Panel
                panel.Controls.Add(form44);
            }
            // 6. CẬP NHẬT TÊN TRANG TRÊN LABEL1 (Code mới thêm)
            formCha.Label1.Text = "Sổ vàng thi đua phong trào Ba Nhất";
            // 6. Hiển thị Form44 lên mặt trên cùng của Panel
            form44.Show();
            form44.BringToFront();

            // 7. Gọi hàm tải dữ liệu (Sử dụng hàm đã viết ở phần trước)
            // Lưu ý: Phải khai báo 'async' ở tên sự kiện Click thì mới dùng được 'await'
            await form44.LoadDuLieuSoVangBaNhatAsync();
        }
        private const float RichText_MinFontSize = 7f;
        private const float RichText_MaxFontSize = 30f;
        private const float RichText_FontStep = 1f;
        private void ThayDoiCoChuRichText(float delta)
        {
            if (richTextBox1_ThanhTich == null)
                return;
            Font fontHienTai = richTextBox1_ThanhTich.Font;

            if (fontHienTai == null)
                return;
            float kichThuocMoi = fontHienTai.Size + delta;
            if (kichThuocMoi < RichText_MinFontSize)
                kichThuocMoi = RichText_MinFontSize;
            if (kichThuocMoi > RichText_MaxFontSize)
                kichThuocMoi = RichText_MaxFontSize;
            // Không tạo Font mới nếu không thay đổi
            if (Math.Abs(kichThuocMoi - fontHienTai.Size) < 0.01f)
                return;
            richTextBox1_ThanhTich.SuspendLayout();
            try
            {
                richTextBox1_ThanhTich.Font =
                    new Font(
                        fontHienTai.FontFamily,
                        kichThuocMoi,
                        fontHienTai.Style,
                        GraphicsUnit.Point);

                richTextBox1_ThanhTich.Focus();
            }
            finally
            {
                richTextBox1_ThanhTich.ResumeLayout();
            }
        }
        private void kryptonButton2_TangCoChuRichText_Click(object sender, EventArgs e)
        {
            ThayDoiCoChuRichText(RichText_FontStep);
        }
        private void kryptonButton2_GiamCoChuRichText_Click(object sender, EventArgs e)
        {
            ThayDoiCoChuRichText(-RichText_FontStep);
        }
        private void KiemTraHienThiNutCoChu()
        {
            // Kiểm tra xem RichTextBox có nội dung không (bỏ qua khoảng trắng/xuống dòng thừa)
            bool coNoiDung = !string.IsNullOrWhiteSpace(richTextBox1_ThanhTich.Text);

            // Ẩn/Hiện 2 nút dựa trên kết quả kiểm tra
            kryptonButton2_TangCoChuRichText.Visible = coNoiDung;
            kryptonButton2_GiamCoChuRichText.Visible = coNoiDung;
        }
        private void CapNhatTrangThaiNut()
        {
            bool coDuLieu = !string.IsNullOrWhiteSpace(textBox_SoHieu.Text) &&
                            !string.IsNullOrWhiteSpace(textBox_HoVaTen.Text);

            kryptonButton_LuuDataDeNghi.Visible = coDuLieu;
        }
        public static string VietHoaChuDau(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";

            // 1. Chuyển toàn bộ chuỗi về chữ thường (để đảm bảo "ĐOÀN" thành "đoàn")
            string s = input.Trim().ToLower();

            // 2. Viết hoa chữ cái đầu tiên và nối với phần còn lại đã ở dạng chữ thường
            return char.ToUpper(s[0]) + s.Substring(1);
        }
        private async void toolStripMenuItem_NhapDuLieu_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Chọn tệp Excel danh sách gốc";
                ofd.Filter = "Excel Files (*.xlsx, *.xlsm)|*.xlsx;*.xlsm";
                ofd.Multiselect = false;
                ofd.CheckFileExists = true;
                ofd.CheckPathExists = true;

                if (ofd.ShowDialog() != DialogResult.OK)
                    return;

                // Gọi thẳng hàm trong Module và nhận lại số lượng cập nhật thành công (truyền chữ 'this' để Module biết form nào gọi)
                int soDongThanhCong = await Module_BaNhat.NhapDuLieuTuTepExcelVaoCSDL(this, ofd.FileName, _csdl2Path);

                // Hiển thị thông báo lên Form 42
                if (toolStripStatusLabel1 != null)
                {
                    toolStripStatusLabel1.Text = $"Nhập thành công: {soDongThanhCong} đồng chí vào CSDL hệ thống";
                    if (toolStripStatusLabel1.Owner != null)
                        toolStripStatusLabel1.Owner.Refresh();
                }

                // Tự động tải lại lưới
                await Task.Delay(200);
                ThongKeSoLuongBaNhat(false);
                CapNhatTrangThaiDuoiNen();

                // 🌟 BỔ SUNG LÀM MỚI TÌNH TRẠNG SỔ VÀNG
                await Module_BaNhat.CapNhatTinhTrangSoVangAsync();
                await LoadDuLieuToanBoDanhSachBaNhatAsync();
            }
        }
        private async void toolStripMenuItem5_XuatDanhSachGoc_Click(object sender, EventArgs e)
        {
            // =========================================================
            // LỚP VỎ UX: LƯU TRẠNG THÁI GỐC CỦA NÚT MENU
            // =========================================================
            string textBanDau = toolStripMenuItem5_XuatDanhSachGoc.Text;
            Image anhBanDau = toolStripMenuItem5_XuatDanhSachGoc.Image;

            // Kiểm tra file mẫu theo logic gốc (Dù gốc khởi tạo Workbook mới nhưng vẫn kiểm tra)
            string templatePath = Module_DanduongGPS.DuongDanCSDL4ex;
            if (!File.Exists(templatePath))
            {
                MessageBox.Show("Không tìm thấy file mẫu Excel tại: " + templatePath, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Yêu cầu chọn nơi lưu TRƯỚC KHI hiện Loading
            using var sfd = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = $"DanhSach_CSDL_BaNhat_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                Title = "Chọn nơi lưu file Danh sách gốc"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;
            string filePathLuu = sfd.FileName;

            // Khởi tạo Form Loading
            Form_Loading frmLoad = new Form_Loading("Đang đọc và tạo tệp excel, vui lòng đợi...");
            bool isLoadShown = false;
            if (this.FindForm() != null) frmLoad.Icon = this.FindForm().Icon;

            try
            {
                // Khóa Menu và Form
                toolStripMenuItem5_XuatDanhSachGoc.Enabled = false;
                toolStripMenuItem5_XuatDanhSachGoc.Text = "Đang xử lý...";
                toolStripMenuItem5_XuatDanhSachGoc.Image = null;

                this.Enabled = false;
                frmLoad.Show(this);
                isLoadShown = true;
                await Task.Delay(50); // Nhường luồng cho UI vẽ form loading mượt

                // =========================================================================
                // TỐI ƯU: ĐỌC VÀ GIẢI MÃ MỘT LẦN VÀO LIST DTO TRÊN LUỒNG BẤT ĐỒNG BỘ
                // =========================================================================
                List<DanhSachGocBaNhatDTO> danhSachGoc = new List<DanhSachGocBaNhatDTO>();

                using (var conn = new SqliteConnection($"Data Source={_csdl2Path}"))
                {
                    await conn.OpenAsync();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"SELECT * FROM [{TenBangDanhSachBaNhat}] ORDER BY STT ASC";
                    using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        int id = reader["ID"] != DBNull.Value ? Convert.ToInt32(reader["ID"]) : 0;

                        // Logic gốc: Bỏ qua ID = -1
                        if (id == -1) continue;

                        danhSachGoc.Add(new DanhSachGocBaNhatDTO
                        {
                            ID = id,
                            HoVaTen = BaoMatAES.GiaiMa(reader["HoVaTen"]?.ToString() ?? ""),
                            SoHieu = BaoMatAES.GiaiMa(reader["SoHieu"]?.ToString() ?? ""),
                            NamSinh = BaoMatAES.GiaiMa(reader["NamSinh"]?.ToString() ?? ""),
                            QueQuan = BaoMatAES.GiaiMa(reader["QueQuan"]?.ToString() ?? ""),
                            NgayVaoCAND = BaoMatAES.GiaiMa(reader["NgayVaoCAND"]?.ToString() ?? ""),
                            CapBac = BaoMatAES.GiaiMa(reader["CapBac"]?.ToString() ?? ""),
                            ChucVu = BaoMatAES.GiaiMa(reader["ChucVu"]?.ToString() ?? ""),
                            DonVi = BaoMatAES.GiaiMa(reader["DonVi"]?.ToString() ?? ""),
                            PhanLoai = BaoMatAES.GiaiMa(reader["PhanLoai"]?.ToString() ?? ""),
                            GhiChu = BaoMatAES.GiaiMa(reader["GhiChu"]?.ToString() ?? ""),
                            DeNghi = BaoMatAES.GiaiMa(reader["DeNghi"]?.ToString() ?? ""),
                            ThanhTich = BaoMatAES.GiaiMa(reader["ThanhTich"]?.ToString() ?? "")
                        });
                    }
                }

                // Truyền thẳng list data sạch và đường dẫn qua hàm xuất
                // await XuatDuLieuRaTepExcelTuCSDL_ToiUu(danhSachGoc, filePathLuu);
                // Thay bằng dòng này:
                await Module_BaNhat.XuatDuLieuRaTepExcelTuCSDL_ToiUu(danhSachGoc, filePathLuu);
                
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống khi xuất file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // PHỤC HỒI TRẠNG THÁI
                if (isLoadShown) frmLoad.Close();
                this.Enabled = true;
                this.Focus();

                toolStripMenuItem5_XuatDanhSachGoc.Text = textBanDau;
                toolStripMenuItem5_XuatDanhSachGoc.Image = anhBanDau;
                toolStripMenuItem5_XuatDanhSachGoc.Enabled = true;
            }
        }
        private async void ToolStripMenuItem_XuatDanhSach_Click(object sender, EventArgs e)
        {
            // =========================================================
            // LỚP VỎ UX: LƯU TRẠNG THÁI GỐC CỦA NÚT MENU
            // =========================================================
            string textBanDau = ToolStripMenuItem_XuatDanhSach.Text;
            Image anhBanDau = ToolStripMenuItem_XuatDanhSach.Image;

            try
            {
                ToolStripMenuItem_XuatDanhSach.Enabled = false;
                ToolStripMenuItem_XuatDanhSach.Text = "Đang xử lý...";
                ToolStripMenuItem_XuatDanhSach.Image = null;
                await Task.Delay(100);

                List<DeNghiBaNhatDTO> danhSachDeNghi = new List<DeNghiBaNhatDTO>();
                int tongSoLoai1 = 0;
                int tyLeQuyDinh = 0;

                // =========================================================
                // THỰC HIỆN CÁC TÁC VỤ DỮ LIỆU ĐỒNG BỘ MỘT LẦN
                // =========================================================
                await Task.Run(() =>
                {
                    using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                    conn.Open();

                    // 1. Lấy Tỷ lệ quy định
                    using (var cmdTyLe = conn.CreateCommand())
                    {
                        cmdTyLe.CommandText = "SELECT TyLe FROM QuyDinhTyLe_BaNhat WHERE ID = 1";
                        var res = cmdTyLe.ExecuteScalar();
                        if (res != null) int.TryParse(BaoMatAES.GiaiMa(res.ToString()), out tyLeQuyDinh);
                    }

                    // 2. Đếm tổng số "Loại 1" từ bảng DanhSach
                    using (var cmdDem = conn.CreateCommand())
                    {
                        cmdDem.CommandText = "SELECT PhanLoai FROM DanhSach";
                        using var rd = cmdDem.ExecuteReader();
                        while (rd.Read())
                        {
                            if (BaoMatAES.GiaiMa(rd["PhanLoai"]?.ToString() ?? "").Trim().Equals("Loại 1", StringComparison.OrdinalIgnoreCase))
                                tongSoLoai1++;
                        }
                    }

                    // 3. Lấy dữ liệu đề nghị (Đề nghị == X)
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"SELECT * FROM [{TenBangDanhSachBaNhat}] ORDER BY STT ASC";
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        int id = reader["ID"] != DBNull.Value ? Convert.ToInt32(reader["ID"]) : 0;
                        string deNghiEnc = reader["DeNghi"]?.ToString() ?? "";
                        string deNghiDec = BaoMatAES.GiaiMa(deNghiEnc).Trim();

                        if (id != -1 && deNghiDec.Equals("X", StringComparison.OrdinalIgnoreCase))
                        {
                            danhSachDeNghi.Add(new DeNghiBaNhatDTO
                            {
                                ID = id,
                                HoVaTen = BaoMatAES.GiaiMa(reader["HoVaTen"]?.ToString() ?? ""),
                                SoHieu = BaoMatAES.GiaiMa(reader["SoHieu"]?.ToString() ?? ""),
                                NamSinh = BaoMatAES.GiaiMa(reader["NamSinh"]?.ToString() ?? ""),
                                NgayVaoCAND = BaoMatAES.GiaiMa(reader["NgayVaoCAND"]?.ToString() ?? ""),
                                CapBac = BaoMatAES.GiaiMa(reader["CapBac"]?.ToString() ?? ""),
                                ChucVu = BaoMatAES.GiaiMa(reader["ChucVu"]?.ToString() ?? ""),
                                DonVi = BaoMatAES.GiaiMa(reader["DonVi"]?.ToString() ?? ""),
                                PhanLoai = BaoMatAES.GiaiMa(reader["PhanLoai"]?.ToString() ?? ""),
                                ThanhTich = BaoMatAES.GiaiMa(reader["ThanhTich"]?.ToString() ?? "")
                            });
                        }
                    }
                });

                // =========================================================
                // LOGIC KIỂM TRA TỶ LỆ & CẢNH BÁO
                // =========================================================
                int soLuongChiTieu = (int)Math.Round(tongSoLoai1 * (tyLeQuyDinh / 100.0), MidpointRounding.AwayFromZero);

                if (danhSachDeNghi.Count < soLuongChiTieu)
                {
                    var dr = MessageBox.Show(
                        $"Chưa đạt tỷ lệ quy định!\nChỉ tiêu: {soLuongChiTieu} đồng chí ({tyLeQuyDinh}%).\nHiện tại chỉ có: {danhSachDeNghi.Count} đồng chí.\n\nBạn có muốn tiếp tục xuất báo cáo không?",
                        "Cảnh báo tỷ lệ", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (dr == DialogResult.No) return;
                }

                // Kiểm tra danh sách rỗng
                if (danhSachDeNghi.Count == 0)
                {
                    var result = MessageBox.Show(
                        "Hiện tại không có CBCS nào được đề nghị biểu dương.\nBạn có chắc chắn muốn xuất tệp Excel trống?",
                        "Cảnh báo dữ liệu trống", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (result != DialogResult.Yes) return;
                }

                // =========================================================
                // XUẤT EXCEL
                // =========================================================
                //await XuatDanhSachBaNhatToExcelAsync(danhSachDeNghi);
                // Thay bằng dòng này (Lưu ý truyền thêm từ khóa 'this' để Module biết Form nào đang chạy):
                await Module_BaNhat.XuatDanhSachBaNhatToExcelAsync(this, danhSachDeNghi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // PHỤC HỒI TRẠNG THÁI GỐC
                ToolStripMenuItem_XuatDanhSach.Text = textBanDau;
                ToolStripMenuItem_XuatDanhSach.Image = anhBanDau;
                ToolStripMenuItem_XuatDanhSach.Enabled = true;
            }
        }
    }
}