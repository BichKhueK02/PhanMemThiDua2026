using ClosedXML.Excel;
using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System.Data;

namespace PhanMemThiDua2026
{
    public partial class Form23_ThongKeThiDuaTapThe : Form
    {
        private readonly string _csdl4Path = Module_DanduongGPS.DuongDanCSDL4;
        public Form23_ThongKeThiDuaTapThe()
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            // Ẩn icon trên Taskbar
            this.ShowInTaskbar = false;
            InitializeComponent();
            comboBox1_ChonThangCanXuat.SelectedIndexChanged += comboBox1_ChonThangCanXuat_SelectedIndexChanged;
            InitToolTips();
        }
        private void Form23_ThongKeThiDuaTapThe_Load(object sender, EventArgs e)
        {

            this.CenterToScreen();
            this.MaximizeBox = false;
            try
            {
                DamBaoBangThongKeTonTai();
                LoadBangThongKe();
                ChinhTieuDeBangThongKe();
                DatTieuDeForm(); // ✅ ĐẶT TIÊU ĐỀ Ở ĐÂY
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
            comboBox1_ChonLoai.Enabled = false;
        }
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Gợi ý thao tác";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;

            // Thời gian hiển thị – UX dễ chịu
            toolTip1.InitialDelay = 300;
            toolTip1.AutoPopDelay = 2000;
            toolTip1.ReshowDelay = 100;
            toolTip1.ShowAlways = true;

            var tips = new Dictionary<Control, string>
    {
        { comboBox1_ChonThangCanXuat, "Chọn tháng cần thống kê thi đua" },
        { comboBox1_ChonLoai, "Chọn loại thống kê thi đua tập thể" },

        { kryptonButton1_CapNhat, "Cập nhật và đồng bộ dữ liệu thống kê" },
        { kryptonButton_XuatTepExcel, "Xuất kết quả thống kê ra tệp Excel" },
        { kryptonButton_Dong, "Đóng màn hình thống kê" }
    };

            foreach (var tip in tips)
            {
                if (tip.Key != null) // an toàn khi ẩn / refactor control
                    toolTip1.SetToolTip(tip.Key, tip.Value);
            }
        }
        private void comboBox1_ChonThangCanXuat_SelectedIndexChanged(object? sender, EventArgs e)
        {
            comboBox1_ChonLoai.Enabled =
                comboBox1_ChonThangCanXuat.SelectedIndex != -1;
        }
        private void kryptonButton_Dong_Click(object sender, EventArgs e)
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
                $"Phiên bản {Module_PhienBan.SoftwareVersion} {Module_PhienBan.NgayThangNamCapNhat}";
        }
        private void ChinhTieuDeBangThongKe()
        {
            var dgv = kryptonDataGridView1;

            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10);

            var map = new Dictionary<string, string>
            {
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

            var cotHopLe = new List<string>
    {
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
            int nam = Module_NamHeThong.LayNamHeThong();
            this.Text = $"Thống kê phân loại thi đua tập thể - {ten} - Năm {nam}";
        }
        private string LayTenTieuDoan()
        {
            try
            {
                using var cn = new SqliteConnection($"Data Source={Module_DanduongGPS.DuongDanCSDL2}");
                cn.Open();

                using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT TenTieuDoan FROM ThongTin WHERE ID = 1";

                var kq = cmd.ExecuteScalar();
                if (kq != null && kq != DBNull.Value)
                    return BaoMatAES.GiaiMa(kq.ToString()!);
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
                return string.Empty; // Cho phép xóa phân loại

            return giaTriChon switch
            {
                "Loại 1" => "Loại 1",
                "Loại 2" => "Loại 2",
                "Loại 3" => "Loại 3",
                "Loại 4" => "Loại 4",
                "Không PL" => "Không PL",
                _ => string.Empty // Giá trị lạ → bỏ qua an toàn
            };
        }
        private void DamBaoBangThongKeTonTai()
        {
            using var cn = new SqliteConnection($"Data Source={_csdl4Path}");
            cn.Open();

            using var tran = cn.BeginTransaction();

            using var cmd = cn.CreateCommand();
            cmd.Transaction = tran;

            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS ThongKe_PhanLoaiTapThe (
    ID INTEGER PRIMARY KEY,
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
    TongKet_Nam TEXT
);";
            cmd.ExecuteNonQuery();

            // INSERT OR IGNORE giúp tránh trùng ID
            cmd.CommandText = @"
INSERT OR IGNORE INTO ThongKe_PhanLoaiTapThe (ID)
VALUES (1);";

            cmd.ExecuteNonQuery();

            tran.Commit();
        }
        private string GiaiMaAnToan(object value)
        {
            try
            {
                if (value == null || value == DBNull.Value) return "";
                string s = value?.ToString() ?? "";
                return string.IsNullOrWhiteSpace(s) ? "" : BaoMatAES.GiaiMa(s);
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

            int nam = Module_NamHeThong.LayNamHeThong();

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
                var mapTongKetNam = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Loại 1"] = "CSTĐ",
                    ["Loại 2"] = "CSTT",
                    ["Loại 3"] = "HTNV",
                    ["Loại 4"] = "KHTNV",
                    ["Không PL"] = string.Empty
                };

                // ================== 4. TÍNH TOÁN CỘT ==================
                int soCotExcel = kryptonDataGridView1.Columns.Count + 1;
                string cotCuoi = XLHelper.GetColumnLetterFromNumber(soCotExcel);

                // ================== 5. TIÊU ĐỀ ==================
                var titleRange = ws.Range($"A1:{cotCuoi}1");
                titleRange.Merge();
                titleRange.Value = $"KẾT QUẢ PHÂN LOẠI TẬP THỂ {tenDonVi} NĂM {nam}";

                titleRange.Style.Font.Bold = true;
                titleRange.Style.Font.FontName = "Times New Roman";
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
                headerRange.Style.Font.FontName = "Times New Roman";
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
                    if (giaTriXuat is "CSTĐ" or "CSTT")
                    {
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.FontColor = XLColor.DarkGreen;
                    }
                    else if (giaTriXuat == "KHTNV")
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
                allRange.Style.Font.FontName = "Times New Roman";
                allRange.Style.Font.FontSize = 14;

                allRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range($"A3:{cotCuoi}4").Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                ws.Columns($"A:{cotCuoi}").AdjustToContents();
                Module_BanQuyen.DongDauExcel(wb);
                // ================== 10. LƯU FILE ==================
                wb.SaveAs(sfd.FileName);

                MessageBox.Show(
                    "Xuất Excel thành công!",
                    "Hoàn tất",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

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
        private void kryptonButton1_CapNhat_Click(object sender, EventArgs e)
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
                        : BaoMatAES.MaHoa(giaTri));

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
    }
}
