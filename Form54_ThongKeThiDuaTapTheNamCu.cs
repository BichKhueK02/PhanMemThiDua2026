using ClosedXML.Excel;
using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace PhanMemThiDua2026
{
    public partial class Form54_ThongKeThiDuaTapTheNamCu : Form
    {
        private string _duongDanCSDL = string.Empty;
        private string _namHienThi = string.Empty;
        // 🔥 BIẾN CACHE FONT TĨNH (Tối ưu RAM & Handle GDI+)
        private static readonly Font _fontGridHeader = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Bold);
        private static readonly Font _fontGridCell = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Regular);
        public Form54_ThongKeThiDuaTapTheNamCu()
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = false;
            InitializeComponent();
            comboBox1_ChonThangCanXuat.SelectedIndexChanged += comboBox1_ChonThangCanXuat_SelectedIndexChanged;
            InitToolTips();
        }
        private void Form54_ThongKeThiDuaTapTheNamCu_Load(object? sender, EventArgs e)
        {
            this.CenterToScreen();
            this.MaximizeBox = false;
            CauHinhGridBanDau();
            comboBox1_ChonLoai.Enabled = false;
        }
        private void InitToolTips()
        {
            if (toolTip1 == null) return;
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            toolTip1.InitialDelay = 300;
            toolTip1.AutoPopDelay = 2000;
            toolTip1.ReshowDelay = 100;
            toolTip1.ShowAlways = true;
            var tips = new Dictionary<Control, string> {
                { comboBox1_ChonThangCanXuat, "Chọn tháng cần thống kê thi đua" },
                { comboBox1_ChonLoai, "Chọn loại thống kê thi đua tập thể" },
                { kryptonButton1_CapNhat, "Cập nhật và đồng bộ dữ liệu thống kê năm cũ" },
                { kryptonButton_XuatTepExcel, "Xuất kết quả thống kê ra tệp Excel" },
                { kryptonButton_Dong, "Đóng màn hình thống kê" }
            };
            foreach (var tip in tips)
            {
                if (tip.Key != null && !tip.Key.IsDisposed)
                    toolTip1.SetToolTip(tip.Key, tip.Value);
            }
        }
        private void comboBox1_ChonThangCanXuat_SelectedIndexChanged(object? sender, EventArgs e)
        {
            comboBox1_ChonLoai.Enabled = comboBox1_ChonThangCanXuat.SelectedIndex != -1;
        }
        // ⭐ HÀM NHẬN DỮ LIỆU ĐỘNG TỪ FORM 46 (Tái sử dụng RAM)
        public void CapNhatDuLieuNamCu(string namHienThi, string duongDanCSDL)
        {
            _namHienThi = namHienThi;
            _duongDanCSDL = duongDanCSDL;
            DatTieuDeForm();
            LoadBangThongKe();
            ChinhTieuDeBangThongKe();
            CapNhatTrangThaiKetNoi();
        }
        private void CauHinhGridBanDau()
        {
            var dgv = kryptonDataGridView1;
            dgv.EnableHeadersVisualStyles = false;
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToResizeRows = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.BackgroundColor = Color.White;
            dgv.BorderStyle = BorderStyle.None;
            dgv.RowHeadersVisible = false;
            dgv.RowTemplate.Height = 36;
        }
        private void DatTieuDeForm()
        {
            string tenDonVi = LayTenTieuDoan();
            if (string.IsNullOrWhiteSpace(tenDonVi)) tenDonVi = "ĐƠN VỊ";
            this.Text = $"Thống kê phân loại thi đua tập thể - {tenDonVi} - {_namHienThi}";
        }
        private string LayTenTieuDoan()
        {
            try
            {
                // Đọc trực tiếp từ CSDL2 chung của hệ thống
                using var cn = new SqliteConnection($"Data Source={Module_DanduongGPS.DuongDanCSDL2}");
                cn.Open();
                using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT TenTieuDoan FROM ThongTin WHERE ID = 1";
                var kq = cmd.ExecuteScalar();
                if (kq != null && kq != DBNull.Value)
                    return Module_BaoMatAES.GiaiMa(kq.ToString()!);
            }
            catch { }
            return string.Empty;
        }
        private void LoadBangThongKe()
        {
            kryptonDataGridView1.DataSource = null;
            if (string.IsNullOrEmpty(_duongDanCSDL) || !File.Exists(_duongDanCSDL)) return;
            try
            {
                using var cn = new SqliteConnection($"Data Source={_duongDanCSDL}");
                cn.Open();
                // 1. Kiểm tra cấu trúc cột thực tế có trong tệp CSDL năm cũ
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
                    "Thang_12_Nam_Cu", "Thang_1", "Thang_2", "Thang_3", "Thang_4", "Thang_5",
                    "Sau_Thang_Dau_Nam", "Thang_6", "Thang_7", "Thang_8", "Thang_9",
                    "Thang_10", "Thang_11", "TongKet_Nam"
                };
                var cotTonTai = cotHopLe.Where(c => tatCaCot.Contains(c)).ToList();
                if (cotTonTai.Count == 0) return;
                string sql = $"SELECT {string.Join(",", cotTonTai.Select(c => $"\"{c}\""))} FROM ThongKe_PhanLoaiTapThe WHERE ID = 1";
                using var cmd = cn.CreateCommand();
                cmd.CommandText = sql;
                using var reader = cmd.ExecuteReader();
                DataTable dt = new DataTable();
                dt.Load(reader);
                // 2. Giải mã AES các ô dữ liệu
                foreach (DataRow row in dt.Rows)
                {
                    foreach (DataColumn col in dt.Columns)
                    {
                        row[col] = GiaiMaAnToan(row[col]);
                    }
                }
                kryptonDataGridView1.DataSource = dt;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi LoadBangThongKe Form54]: {ex.Message}");
            }
        }
        private void ChinhTieuDeBangThongKe()
        {
            var dgv = kryptonDataGridView1;
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.ColumnHeadersDefaultCellStyle.Font = _fontGridHeader;
            dgv.DefaultCellStyle.Font = _fontGridCell;
            dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
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
            {
                if (dgv.Columns.Contains(kv.Key))
                    dgv.Columns[kv.Key].HeaderText = kv.Value;
            }
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }
        private void CapNhatTrangThaiKetNoi()
        {
            if (toolStripLabel1 == null) return;
            if (!string.IsNullOrEmpty(_duongDanCSDL) && File.Exists(_duongDanCSDL))
            {
                toolStripLabel1.Text = $"Đang đọc dữ liệu lịch sử: {Path.GetFileName(_duongDanCSDL)}";
                toolStripLabel1.ForeColor = Color.DarkGreen;
            }
            else
            {
                toolStripLabel1.Text = "Không tìm thấy tệp CSDL năm cũ";
                toolStripLabel1.ForeColor = Color.Red;
            }
        }
        private string LayTenCotTheoThang()
        {
            if (comboBox1_ChonThangCanXuat.SelectedItem == null) return string.Empty;
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
            if (comboBox1_ChonLoai.SelectedItem == null) return string.Empty;
            string giaTriChon = comboBox1_ChonLoai.SelectedItem.ToString()!;
            if (string.IsNullOrWhiteSpace(giaTriChon)) return string.Empty;
            return giaTriChon switch
            {
               Module_HeThong.Loai_1 => Module_HeThong.Loai_1,
                Module_HeThong.Loai_2 => Module_HeThong.Loai_2,
                Module_HeThong.Loai_3 => Module_HeThong.Loai_3,
                Module_HeThong.Loai_4 => Module_HeThong.Loai_4,
                Module_HeThong.PL_KHONG_PL => Module_HeThong.PL_KHONG_PL,
                _ => string.Empty
            };
        }
        private string GiaiMaAnToan(object value)
        {
            try
            {
                if (value == null || value == DBNull.Value) return "";
                string s = value.ToString() ?? "";
                return string.IsNullOrWhiteSpace(s) ? "" : Module_BaoMatAES.GiaiMa(s);
            }
            catch
            {
                return "";
            }
        }
        private void kryptonButton_Dong_Click(object? sender, EventArgs e)
        {
            this.Close();
        }
        private void kryptonButton1_CapNhat_Click(object? sender, EventArgs e)
        {
            // 1. KIỂM TRA CSDL LỊCH SỬ
            if (string.IsNullOrWhiteSpace(_duongDanCSDL) ||
                !File.Exists(_duongDanCSDL))
            {
                MessageBox.Show(
                    "Không tìm thấy tệp CSDL năm cũ để cập nhật.",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            // 2. KIỂM TRA DỮ LIỆU ĐẦU VÀO
            string tenCot = LayTenCotTheoThang();
            if (string.IsNullOrWhiteSpace(tenCot))
            {
                MessageBox.Show(
                    "Chưa chọn tháng.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            if (comboBox1_ChonLoai.SelectedItem == null)
            {
                MessageBox.Show(
                    "Chưa chọn loại phân loại.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            string giaTri = LayGiaTriLoai();
            // 3. XÁC MINH QUYỀN ADMIN
            DialogResult ketQuaXacMinh;
            using (Form24_XacMinhAdmin frm = new Form24_XacMinhAdmin())
            {
                frm.TopMost = true;
                frm.StartPosition = FormStartPosition.CenterScreen;
                ketQuaXacMinh = frm.ShowDialog(this);
            }
            if (ketQuaXacMinh != DialogResult.OK)
                return;
            // 4. CẬP NHẬT CSDL
            try
            {
                using var cn = new SqliteConnection(
                    $"Data Source={_duongDanCSDL}");
                cn.Open();
                using var tran = cn.BeginTransaction();
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tran;
                cmd.CommandText = $@"
UPDATE ""ThongKe_PhanLoaiTapThe""
SET ""{tenCot}"" = @gt
WHERE ID = 1;";
                var parameter = cmd.Parameters.Add(
                    "@gt",
                    SqliteType.Text);
                parameter.Value = string.IsNullOrEmpty(giaTri)
                    ? DBNull.Value
                    : Module_BaoMatAES.MaHoa(giaTri);
                int rowsAffected = cmd.ExecuteNonQuery();
                // Không cho phép báo thành công nếu không có
                // bản ghi nào thực sự được cập nhật.
                if (rowsAffected != 1)
                {
                    throw new InvalidOperationException(
                        "Không tìm thấy bản ghi thống kê tập thể cần cập nhật.");
                }
                tran.Commit();
                // 5. LÀM MỚI GIAO DIỆN
                LoadBangThongKe();
                ChinhTieuDeBangThongKe();
                CapNhatTrangThaiKetNoi();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Lỗi khi cập nhật dữ liệu năm cũ:\n\n" + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void kryptonButton_XuatTepExcel_Click(object? sender, EventArgs e)
        {
            if (kryptonDataGridView1 == null ||
                kryptonDataGridView1.Columns.Count == 0 ||
                kryptonDataGridView1.Rows.Cast<DataGridViewRow>().All(r => r.IsNewRow))
            {
                KryptonMessageBox.Show("Không có dữ liệu hợp lệ để xuất Excel.", "Thông báo",
                                       KryptonMessageBoxButtons.OK, KryptonMessageBoxIcon.Warning);
                return;
            }
            string tenDonVi = LayTenTieuDoan();
            if (string.IsNullOrWhiteSpace(tenDonVi)) tenDonVi = "ĐƠN VỊ";
            string tenHienThi = char.ToUpperInvariant(tenDonVi[0]) + tenDonVi.Substring(1).ToLowerInvariant();
            string tenFile = $"BẢNG THỐNG KÊ PHÂN LOẠI THI ĐUA TẬP THỂ {tenDonVi} {_namHienThi.ToUpper().Replace(" ", "_")}_{DateTime.Now:HHmmss}.xlsx";
            using SaveFileDialog sfd = new SaveFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                FileName = tenFile,
                Filter = "Excel (*.xlsx)|*.xlsx",
                AddExtension = true,
                OverwritePrompt = false
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;
            try
            {
                using var wb = new ClosedXML.Excel.XLWorkbook();
                var ws = wb.Worksheets.Add("ThongKe");
                // 🔥 ĐÃ SỬA: Bảng ánh xạ danh hiệu dành cho TẬP THỂ (ĐVQT, ĐVTT, HTNV, KHTNV, Không PL)
                var mapTongKetNam = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [Module_HeThong.Loai_1] = Module_HeThong.XLDV_DVQT, // "ĐVQT"
                    [Module_HeThong.Loai_2] = Module_HeThong.XLDV_DVTT, // "ĐVTT"
                    [Module_HeThong.Loai_3] = Module_HeThong.XLDV_HTNV, // "HTNV"
                    [Module_HeThong.Loai_4] = Module_HeThong.XLDV_KHTNV, // "KHTNV"
                    [Module_HeThong.PL_KHONG_PL] = Module_HeThong.XLDV_KHONG_XET // "Không PL"
                };
                int soCotExcel = kryptonDataGridView1.Columns.Count + 1;
                string cotCuoi = XLHelper.GetColumnLetterFromNumber(soCotExcel);
                // 1. Tiêu đề
                var titleRange = ws.Range($"A1:{cotCuoi}1");
                titleRange.Merge();
                titleRange.Value = $"KẾT QUẢ PHÂN LOẠI TẬP THỂ {tenDonVi} - {_namHienThi.ToUpper()}";
                titleRange.Style.Font.Bold = true;
                titleRange.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                titleRange.Style.Font.FontSize = 14;
                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                // 2. Header
                ws.Cell(3, 1).Value = "Đơn vị";
                for (int i = 0; i < kryptonDataGridView1.Columns.Count; i++)
                {
                    ws.Cell(3, i + 2).Value = kryptonDataGridView1.Columns[i].HeaderText;
                }
                var headerRange = ws.Range($"A3:{cotCuoi}3");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                headerRange.Style.Font.FontSize = 14;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(217, 234, 211);
                // 3. Dữ liệu (1 dòng)
                ws.Cell(4, 1).Value = tenHienThi;
                DataGridViewRow rowNguon = kryptonDataGridView1.Rows
                    .Cast<DataGridViewRow>()
                    .First(r => !r.IsNewRow);
                for (int i = 0; i < kryptonDataGridView1.Columns.Count; i++)
                {
                    string giaTriGoc = rowNguon.Cells[i].Value?.ToString()?.Trim() ?? string.Empty;
                    bool laTongKetNam = kryptonDataGridView1.Columns[i].HeaderText.Contains("Tổng kết", StringComparison.OrdinalIgnoreCase);
                    string giaTriXuat = laTongKetNam && mapTongKetNam.TryGetValue(giaTriGoc, out string danhHieu)
                        ? danhHieu
                        : giaTriGoc;
                    var cell = ws.Cell(4, i + 2);
                    cell.Value = giaTriXuat;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Alignment.WrapText = true;
                    if (giaTriXuat is Module_HeThong.XLDV_DVQT or Module_HeThong.XLDV_DVTT)
                    {
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.FontColor = XLColor.DarkGreen;
                    }
                    else if (giaTriXuat == Module_HeThong.XLDV_KHTNV)
                    {
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.FontColor = XLColor.DarkRed;
                    }
                }
                ws.Row(4).Height = 36;
                var row4Range = ws.Range($"A4:{cotCuoi}4");
                row4Range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                row4Range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                row4Range.Style.Alignment.WrapText = true;
                var allRange = ws.Range($"A1:{cotCuoi}4");
                allRange.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                allRange.Style.Font.FontSize = 14;
                allRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range($"A3:{cotCuoi}4").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                ws.Columns($"A:{cotCuoi}").AdjustToContents();
                Module_BanQuyen.DongDauExcel(wb);
                wb.SaveAs(sfd.FileName);
                MessageBox.Show("Xuất Excel thành công!", "Hoàn tất",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                Module_NhatKy.GhiNhatKy(
                     taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                     hanhDong: "Xuất Excel thống kê thi đua tập thể năm cũ tên tệp tin là" + sfd.FileName,
                     ghiChu: "Thành công");
                if (File.Exists(sfd.FileName))
                {
                    Module_XuatNhapDuLieuThiDua.MoVaChonTepTrongExplorer(sfd.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể xuất Excel.\nChi tiết: " + ex.Message, "Lỗi",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}