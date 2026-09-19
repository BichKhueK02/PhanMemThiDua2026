using ClosedXML.Excel;
using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Globalization;
namespace PhanMemThiDua2026
{
    public partial class Form23_ThongKeThiDuaTapThe : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private readonly string _csdl4Path = Module_DanduongGPS.DuongDanCSDL4;
        // 🌟 4 biến hằng số danh hiệu thi đua dùng chung toàn Form
        public const string DanhHieu_Loai1 = Module_HeThong.XLDV_DVQT; // "TĐ"
        public const string DanhHieu_Loai2 = Module_HeThong.XLDV_DVTT; // "ĐVTT"
        public const string DanhHieu_Loai3 = Module_HeThong.XLDV_HTNV; // "HTNV"
        public const string DanhHieu_Loai4 = Module_HeThong.XLDV_KHTNV; // "KHTNV"
        private static readonly Font _fontHeaderBangThongKe = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Bold);
        public Form23_ThongKeThiDuaTapThe()
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = false;
            InitializeComponent();
            comboBox1_ChonThangCanXuat.SelectedIndexChanged += comboBox1_ChonThangCanXuat_SelectedIndexChanged;
            // Đăng ký sự kiện lắng nghe khi tên đơn vị thay đổi từ Module_HeThong
            Module_HeThong.SuKienThayDoiTenDonVi += CapNhatTieuDeGroupBox;
            InitToolTips();
        }
        private void Form23_ThongKeThiDuaTapThe_Load(object? sender, EventArgs e)
        {
            this.CenterToScreen();
            this.MaximizeBox = false;
            Font fontThuong = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Regular);
            CapNhatTieuDeGroupBox();
            comboBox1_ChonThangCanXuat.Font = fontThuong;
            comboBox1_ChonLoai.Font = fontThuong;
            try
            {
                DamBaoBangThongKeTonTai();
                LoadBangThongKe();
                ChinhTieuDeBangThongKe();
                DatTieuDeForm();
            }
            catch (Exception ex)
            {
                KryptonMessageBox.Show(
                    "Không thể khởi tạo dữ liệu.\n" + ex.Message,
                    "Lỗi hệ thống",
                    KryptonMessageBoxButtons.OK,
                    KryptonMessageBoxIcon.Error);
            }
            CapNhatTrangThaiKetNoi();
            HienThiPhienBan();
            ChonThangHienTaiTuDong();
        }
        private void CapNhatTieuDeGroupBox()
        {
            string tenDonVi = Module_HeThong.LayTenDonViChuan();
            groupBox1.Text = string.IsNullOrWhiteSpace(tenDonVi)
                ? "1. Cập nhật phân loại/danh hiệu thi đua"
                : $"1. Cập nhật phân loại/danh hiệu thi đua {tenDonVi}";
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Module_HeThong.SuKienThayDoiTenDonVi -= CapNhatTieuDeGroupBox;
            base.OnFormClosed(e);
        }
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            // Thời gian hiển thị – UX dễ chịu
            toolTip1.InitialDelay = 300;
            toolTip1.AutoPopDelay = 2000;
            toolTip1.ReshowDelay = 100;
            toolTip1.ShowAlways = true;
            var tips = new Dictionary<Control, string>{
        { comboBox1_ChonThangCanXuat, "Chọn tháng cần thống kê thi đua" },
        { comboBox1_ChonLoai, "Chọn loại thống kê thi đua tập thể" },
        { kryptonButton1_CapNhat, "Cập nhật và đồng bộ dữ liệu thống kê" },
        { kryptonButton_XuatTepExcel, "Xuất kết quả thống kê ra tệp Excel" },
        { kryptonButton1_XoaTatCaDataPhanLoaiTapThe, "Xóa tất cả dữ liệu trong bảng" }
    };
            foreach (var tip in tips)
            {
                if (tip.Key != null) // an toàn khi ẩn / refactor control
                    toolTip1.SetToolTip(tip.Key, tip.Value);
            }
        }
        private void comboBox1_ChonThangCanXuat_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Luôn xóa sạch items cũ để nạp bộ danh mục mới
            comboBox1_ChonLoai.Items.Clear();
            if (comboBox1_ChonThangCanXuat.SelectedItem == null)
                return;
            string thangChon = comboBox1_ChonThangCanXuat.SelectedItem.ToString() ?? string.Empty;
            if (thangChon.Equals("Tổng kết năm", StringComparison.OrdinalIgnoreCase))
            {
                // Nạp danh hiệu thi đua tổng kết năm
                comboBox1_ChonLoai.Items.AddRange(new object[]
                {
            "",
            DanhHieu_Loai1,            // Module_HeThong.XLDV_DVQT
            DanhHieu_Loai2,            // Module_HeThong.XLDV_DVTT
            DanhHieu_Loai3,            // Module_HeThong.XLDV_HTNV
            DanhHieu_Loai4,            // Module_HeThong.XLDV_KHTNV
            Module_HeThong.PL_KHONG_PL
                });
            }
            else
            {
                // Nạp phân loại tháng/định kỳ thông thường
                comboBox1_ChonLoai.Items.AddRange(new object[]
                {
            "",
            Module_HeThong.Loai_1,
            Module_HeThong.Loai_2,
            Module_HeThong.Loai_3,
            Module_HeThong.Loai_4,
            Module_HeThong.PL_KHONG_PL
                });
            }
            // Tự động chọn mục đầu tiên để tối ưu thao tác
            if (comboBox1_ChonLoai.Items.Count > 0)
            {
                comboBox1_ChonLoai.SelectedIndex = 0;
            }
        }
        private void kryptonButton_Dong_Click(object? sender, EventArgs e)
        {
            this.Close();
        }
        private void CapNhatTrangThaiKetNoi()
        {
            try
            {
                using var cn = new SqliteConnection($"Data Source={_csdl4Path}");
                cn.Open();
                toolStripLabel1.Text = "Đang kết nối Cơ sở dữ liệu 4";
                toolStripLabel1.ForeColor = Color.DarkGreen;
            }
            catch
            {
                toolStripLabel1.Text = "Mất kết nối Cơ sở dữ liệu 4";
                toolStripLabel1.ForeColor = Color.Red;
            }
        }
        private void HienThiPhienBan()
        {
            toolStripLabel2.Alignment = ToolStripItemAlignment.Right;
            toolStripLabel2.Text =
                $"Phiên bản phần mềm {Module_PhienBan.SoftwareVersion} {Module_PhienBan.NgayThangNamCapNhat}";
        }
        private void ChinhTieuDeBangThongKe()
        {
            var dgv = kryptonDataGridView1;
            dgv.AllowUserToResizeColumns = false;
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.ColumnHeadersDefaultCellStyle.Font = _fontHeaderBangThongKe;
            var map = new Dictionary<string, string>{
                ["Thang_12_Nam_Cu"] = "Tháng 12 (Năm cũ)",
                ["Thang_1"] = "Tháng 1",
                ["Thang_2"] = "Tháng 2",
                ["Thang_3"] = "Tháng 3",
                ["Thang_4"] = "Tháng 4",
                ["Thang_5"] = "Tháng 5",
                ["Sau_Thang_Dau_Nam"] = "6 Tháng đầu năm",
                ["Thang_6"] = "Tháng 6",
                ["Thang_7"] = "Tháng 7",
                ["Thang_8"] = "Tháng 8",
                ["Thang_9"] = "Tháng 9",
                ["Thang_10"] = "Tháng 10",
                ["Thang_11"] = "Tháng 11",
                ["TongKet_Nam"] = "Tổng kết năm"
            };
            foreach (var kv in map)
                if (dgv.Columns.Contains(kv.Key))
                    dgv.Columns[kv.Key].HeaderText = kv.Value;
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }
        private void LoadBangThongKe()
        {
            kryptonDataGridView1.DataSource = null;
            string dbPath = _csdl4Path;
            if (!File.Exists(dbPath)) return;
            using var cn = new SqliteConnection($"Data Source={dbPath}");
            cn.Open();
            // ===== 1. Lấy toàn bộ cột 1 lần duy nhất =====
            var tatCaCot = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var cmdPragma = cn.CreateCommand())
            {
                cmdPragma.CommandText = "PRAGMA table_info(ThongKe_PhanLoaiTapThe)";
                using var rd = cmdPragma.ExecuteReader();
                while (rd.Read())
                    tatCaCot.Add(rd["name"].ToString()!);
            }
            var cotHopLe = new List<string> {
        "Thang_12_Nam_Cu","Thang_1","Thang_2","Thang_3","Thang_4","Thang_5",
        "Sau_Thang_Dau_Nam","Thang_6","Thang_7","Thang_8","Thang_9",
        "Thang_10","Thang_11","TongKet_Nam"
    };
            var cotTonTai = cotHopLe.Where(c => tatCaCot.Contains(c)).ToList();
            if (cotTonTai.Count == 0) return;
            string sql =
                $"SELECT {string.Join(",", cotTonTai.Select(c => $"\"{c}\""))} " +
                "FROM ThongKe_PhanLoaiTapThe WHERE ID = 1";
            using var cmd = cn.CreateCommand();
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();
            DataTable dt = new();
            dt.Load(reader);
            // ===== 2. Giải mã an toàn =====
            foreach (DataRow row in dt.Rows)
            {
                foreach (DataColumn col in dt.Columns)
                {
                    row[col] = GiaiMaAnToan(row[col]);
                }
            }
            kryptonDataGridView1.DataSource = dt;
            kryptonDataGridView1.ReadOnly = true;
            kryptonDataGridView1.AllowUserToAddRows = false;
            kryptonDataGridView1.AllowUserToDeleteRows = false;
            kryptonDataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        }
        private void DatTieuDeForm()
        {
            string ten = LayTenTieuDoan();
            if (string.IsNullOrWhiteSpace(ten))
                ten = "ĐƠN VỊ";
            int nam = Module_HeThong.LayNamHeThong();
            this.Text = $"Thống kê phân loại thi đua tập thể - {ten} - Năm {nam}";
        }
        private string LayTenTieuDoan()
        {
            try
            {
                using var cn = new SqliteConnection($"Data Source={_csdl2Path}");
                cn.Open();
                using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT TenTieuDoan FROM ThongTin WHERE ID = 1";
                var kq = cmd.ExecuteScalar();
                if (kq != null && kq != DBNull.Value)
                    return Module_BaoMatAES.GiaiMa(kq.ToString()!);
            }
            catch
            {
                // tránh crash UI
            }
            return string.Empty;
        }
        private string LayTenCotTheoThang()
        {
            if (comboBox1_ChonThangCanXuat.SelectedItem == null)
                return string.Empty;
            string thangChon = comboBox1_ChonThangCanXuat.SelectedItem.ToString()!;
            return thangChon switch
            {
                "Tháng 12 (Năm cũ)" => "Thang_12_Nam_Cu",
                "Tháng 1" => "Thang_1",
                "Tháng 2" => "Thang_2",
                "Tháng 3" => "Thang_3",
                "Tháng 4" => "Thang_4",
                "Tháng 5" => "Thang_5",
                "6 Tháng đầu năm" => "Sau_Thang_Dau_Nam",
                "Tháng 6" => "Thang_6",
                "Tháng 7" => "Thang_7",
                "Tháng 8" => "Thang_8",
                "Tháng 9" => "Thang_9",
                "Tháng 10" => "Thang_10",
                "Tháng 11" => "Thang_11",
                "Tổng kết năm" => "TongKet_Nam",
                _ => string.Empty
            };
        }
        private string LayGiaTriLoai()
        {
            if (comboBox1_ChonLoai.SelectedItem == null)
                return string.Empty; // Chưa chọn → không xử lý
            string giaTriChon = comboBox1_ChonLoai.SelectedItem.ToString()!;
            if (string.IsNullOrWhiteSpace(giaTriChon))
                return string.Empty;
            // Kiểm tra tính hợp lệ của giá trị (chấp nhận cả 2 bộ danh hiệu)
            if (giaTriChon == Module_HeThong.Loai_1 ||
                giaTriChon == Module_HeThong.Loai_2 ||
                giaTriChon == Module_HeThong.Loai_3 ||
                giaTriChon == Module_HeThong.Loai_4 ||
                giaTriChon == DanhHieu_Loai1 ||
                giaTriChon == DanhHieu_Loai2 ||
                giaTriChon == DanhHieu_Loai3 ||
                giaTriChon == DanhHieu_Loai4 ||
                giaTriChon == Module_HeThong.PL_KHONG_PL)
            {
                return giaTriChon;
            }
            return string.Empty; // Giá trị không hợp lệ
        }
        private void DamBaoBangThongKeTonTai()
        {
            using var connection = new SqliteConnection($"Data Source={_csdl4Path}");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
        CREATE TABLE IF NOT EXISTS ThongKe_PhanLoaiTapThe
        (
            ID INTEGER NOT NULL,
            Thang_12_Nam_Cu TEXT,
            Thang_1 TEXT,
            Thang_2 TEXT,
            Thang_3 TEXT,
            Thang_4 TEXT,
            Thang_5 TEXT,
            Sau_Thang_Dau_Nam TEXT,
            Thang_6 TEXT,
            Thang_7 TEXT,
            Thang_8 TEXT,
            Thang_9 TEXT,
            Thang_10 TEXT,
            Thang_11 TEXT,
            TongKet_Nam TEXT,
            PRIMARY KEY(ID AUTOINCREMENT)
        );
        INSERT OR IGNORE INTO ThongKe_PhanLoaiTapThe (ID)
        VALUES (1);
    ";
            command.ExecuteNonQuery();
        }
        private string GiaiMaAnToan(object? value)
        {
            try
            {
                if (value == null || value == DBNull.Value) return "";
                string s = value?.ToString() ?? "";
                return string.IsNullOrWhiteSpace(s) ? "" : Module_BaoMatAES.GiaiMa(s);
            }
            catch
            {
                return "";
            }
        }
        private void kryptonButton_XuatTepExcel_Click(object? sender, EventArgs e)
        {
            // ================== 1. KIỂM TRA AN TOÀN ==================
            if (kryptonDataGridView1 == null ||
                kryptonDataGridView1.Columns.Count == 0 ||
                kryptonDataGridView1.Rows.Cast<DataGridViewRow>().All(r => r.IsNewRow))
            {
                KryptonMessageBox.Show(
                    "Không có dữ liệu hợp lệ để xuất Excel.",
                    "Thông báo",
                    KryptonMessageBoxButtons.OK,
                    KryptonMessageBoxIcon.Warning);
                return;
            }
            // ================== 2. THÔNG TIN CƠ BẢN ==================
            string tenDonVi = LayTenTieuDoan();
            if (string.IsNullOrWhiteSpace(tenDonVi))
                tenDonVi = "ĐƠN VỊ";
            string tenHienThi =
                char.ToUpperInvariant(tenDonVi[0]) +
                tenDonVi.Substring(1).ToLowerInvariant();
            int nam = Module_HeThong.LayNamHeThong();
            string tenFile =
                $"BẢNG THỐNG KÊ PHÂN LOẠI THI ĐUA TẬP THỂ {tenDonVi} NĂM {nam}_{DateTime.Now:HHmmss}.xlsx";
            using SaveFileDialog sfd = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                FileName = tenFile,
                Filter = "Excel (*.xlsx)|*.xlsx",
                AddExtension = true,
                OverwritePrompt = false
            };
            if (sfd.ShowDialog() != DialogResult.OK)
                return;
            try
            {
                using var wb = new ClosedXML.Excel.XLWorkbook();
                var ws = wb.Worksheets.Add("ThongKe");
                // ================== 3. MAP NGHIỆP VỤ (DUY NHẤT 1 CHỖ) ==================
                var mapTongKetNam = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase){
                    [Module_HeThong.Loai_1] = DanhHieu_Loai1,
                    [Module_HeThong.Loai_2] = DanhHieu_Loai2,
                    [Module_HeThong.Loai_3] = DanhHieu_Loai3,
                    [Module_HeThong.Loai_4] = DanhHieu_Loai4,
                    [Module_HeThong.PL_KHONG_PL] = string.Empty
                };
                // ================== 4. TÍNH TOÁN CỘT ==================
                int soCotExcel = kryptonDataGridView1.Columns.Count + 1;
                string cotCuoi = XLHelper.GetColumnLetterFromNumber(soCotExcel);
                // ================== 5. TIÊU ĐỀ ==================
                var titleRange = ws.Range($"A1:{cotCuoi}1");
                titleRange.Merge();
                titleRange.Value = $"KẾT QUẢ PHÂN LOẠI TẬP THỂ {tenDonVi} NĂM {nam}";
                titleRange.Style.Font.Bold = true;
                titleRange.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                titleRange.Style.Font.FontSize = 14;
                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                // ================== 6. HEADER ==================
                ws.Cell(3, 1).Value = "Đơn vị";
                for (int i = 0; i < kryptonDataGridView1.Columns.Count; i++)
                {
                    ws.Cell(3, i + 2).Value =
                        kryptonDataGridView1.Columns[i].HeaderText;
                }
                var headerRange = ws.Range($"A3:{cotCuoi}3");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                headerRange.Style.Font.FontSize = 14;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(217, 234, 211);
                // ================== 7. DỮ LIỆU (CHỈ 1 DÒNG TỔNG) ==================
                ws.Cell(4, 1).Value = tenHienThi;
                DataGridViewRow rowNguon =
                    kryptonDataGridView1.Rows
                        .Cast<DataGridViewRow>()
                        .First(r => !r.IsNewRow);
                for (int i = 0; i < kryptonDataGridView1.Columns.Count; i++)
                {
                    string giaTriGoc =
                        rowNguon.Cells[i].Value?.ToString()?.Trim() ?? string.Empty;
                    bool laTongKetNam =
                        kryptonDataGridView1.Columns[i].HeaderText
                            .Contains("Tổng kết", StringComparison.OrdinalIgnoreCase);
                    string giaTriXuat =
                        laTongKetNam && mapTongKetNam.TryGetValue(giaTriGoc, out string danhHieu)
                            ? danhHieu
                            : giaTriGoc;
                    var cell = ws.Cell(4, i + 2);
                    cell.Value = giaTriXuat;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Alignment.WrapText = true;
                    // Tô màu theo danh hiệu
                    if (giaTriXuat is DanhHieu_Loai1 or DanhHieu_Loai2)
                    {
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.FontColor = XLColor.DarkGreen;
                    }
                    else if (giaTriXuat == DanhHieu_Loai4)
                    {
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.FontColor = XLColor.DarkRed;
                    }
                }
                // ================== 8. A4 – GIÃN DÒNG – CĂN GIỮA TUYỆT ĐỐI ==================
                ws.Row(4).Height = 36;
                var row4Range = ws.Range($"A4:{cotCuoi}4");
                row4Range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                row4Range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                row4Range.Style.Alignment.WrapText = true;
                // ================== 9. ĐỊNH DẠNG CHUNG ==================
                var allRange = ws.Range($"A1:{cotCuoi}4");
                allRange.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                allRange.Style.Font.FontSize = 14;
                allRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range($"A3:{cotCuoi}4").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                ws.Columns($"A:{cotCuoi}").AdjustToContents();
                Module_BanQuyen.DongDauExcel(wb);
                // ================== 10. LƯU FILE ==================
                wb.SaveAs(sfd.FileName);
                Module_NhatKy.GhiNhatKy(
                     Module_TaiKhoan.TenTaiKhoan_RAM,
                     "Xuất dữ liệu Excel",
                     $"Xuất Excel thành công | {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
                if (File.Exists(sfd.FileName))
                {
                    Module_XuatNhapDuLieuThiDua.MoVaChonTepTrongExplorer(sfd.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể xuất Excel.\nChi tiết: " + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void kryptonButton1_CapNhat_Click(object? sender, EventArgs e)
        {
            string tenCot = LayTenCotTheoThang();
            string giaTri = LayGiaTriLoai();
            if (string.IsNullOrEmpty(tenCot))
            {
                MessageBox.Show("Chưa chọn tháng.");
                return;
            }
            if (comboBox1_ChonLoai.SelectedItem == null)
            {
                MessageBox.Show("Chưa chọn loại phân loại.");
                return;
            }
            using var cn = new SqliteConnection($"Data Source={_csdl4Path}");
            cn.Open();
            using var tran = cn.BeginTransaction();
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tran;
                cmd.CommandText = $@"
UPDATE ThongKe_PhanLoaiTapThe
SET ""{tenCot}"" = @gt
WHERE ID = 1";
                cmd.Parameters.AddWithValue("@gt",
                    string.IsNullOrEmpty(giaTri)
                        ? DBNull.Value
                        : Module_BaoMatAES.MaHoa(giaTri));
                cmd.ExecuteNonQuery();
                tran.Commit();
            }
            catch
            {
                tran.Rollback();
                throw;
            }
            LoadBangThongKe();
            ChinhTieuDeBangThongKe();
            CapNhatTrangThaiKetNoi();
            HienThiPhienBan();
        }
        private void ChonThangHienTaiTuDong()
        {
            // Lấy tháng hiện tại của hệ thống (1 -> 12)
            int thangHienTai = DateTime.Now.Month;
            string tenThangCanTim = $"Tháng {thangHienTai}";
            // Tìm index của "Tháng X" trong comboBox1_ChonThangCanXuat
            for (int i = 0; i < comboBox1_ChonThangCanXuat.Items.Count; i++)
            {
                string itemText = comboBox1_ChonThangCanXuat.Items[i]?.ToString() ?? string.Empty;
                if (itemText.Equals(tenThangCanTim, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox1_ChonThangCanXuat.SelectedIndex = i;
                    return;
                }
            }
        }
        private void kryptonButton1_XoaTatCaDataPhanLoaiTapThe_Click(object? sender, EventArgs e)
        {
            // 1. Xác minh quyền Admin
            using (Form24_XacMinhAdmin frmXacMinh = new Form24_XacMinhAdmin())
            {
                frmXacMinh.TopMost = true;
                frmXacMinh.StartPosition = FormStartPosition.CenterScreen;
                if (frmXacMinh.ShowDialog() != DialogResult.OK)
                    return;
            }
            // 2. Xác nhận lần cuối trước khi xóa
            DialogResult confirm = MessageBox.Show(
                "Bạn có chắc chắn muốn XÓA TOÀN BỘ dữ liệu phân loại thi đua tập thể không?\n\n" +
                "Toàn bộ dữ liệu hiện tại sẽ bị xóa và không thể hoàn tác.",
                "Xác nhận xóa toàn bộ dữ liệu",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes)
                return;
            try
            {
                // 3. Kiểm tra CSDL
                string dbPath = _csdl4Path;
                if (string.IsNullOrWhiteSpace(dbPath))
                {
                    MessageBox.Show(
                        "Đường dẫn CSDL không hợp lệ!",
                        "Lỗi",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
                if (!File.Exists(dbPath))
                {
                    MessageBox.Show(
                        "Không tìm thấy tệp CSDL!\n\n" + dbPath,
                        "Lỗi",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
                // 4. Xóa dữ liệu và tạo lại bản ghi ID = 1
                using (var cn = new SqliteConnection($"Data Source={dbPath}"))
                {
                    cn.Open();
                    using var transaction = cn.BeginTransaction();
                    try
                    {
                        // Xóa toàn bộ dữ liệu
                        using (var cmdDelete = new SqliteCommand(
                            "DELETE FROM ThongKe_PhanLoaiTapThe",
                            cn,
                            transaction))
                        {
                            cmdDelete.ExecuteNonQuery();
                        }
                        // Tạo lại bản ghi gốc ID = 1
                        using (var cmdInsert = new SqliteCommand(
                            "INSERT INTO ThongKe_PhanLoaiTapThe (ID) VALUES (1)",
                            cn,
                            transaction))
                        {
                            cmdInsert.ExecuteNonQuery();
                        }
                        // Hoàn tất transaction
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
                // 5. Tải lại giao diện
                LoadBangThongKe();
                ChinhTieuDeBangThongKe();
                // 6. Ghi nhật ký
                Module_NhatKy.GhiNhatKy(
                    Module_TaiKhoan.TenTaiKhoan_RAM,
                    "Xóa dữ liệu thi đua tập thể",
                    $"Xóa toàn bộ dữ liệu bảng ThongKe_PhanLoaiTapThe và tạo lại ID=1 | " +
                    $"{DateTime.Now:dd-MM-yyyy HH:mm:ss}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Lỗi khi xóa dữ liệu tập thể:\n\n" + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}