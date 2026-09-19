using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClosedXML.Excel;
using System.Globalization;
namespace PhanMemThiDua2026
{
    public partial class Form57_CapNhatTinhThanhPho : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private int _selectedID = -1;
        private bool isEditing = false;
        private readonly Font _fontHeader = new Font(SystemFonts.MessageBoxFont.FontFamily, 10f, FontStyle.Bold);
        private readonly Font _fontData = new Font(SystemFonts.MessageBoxFont.FontFamily, 9f);
        public Form57_CapNhatTinhThanhPho()
        {
            InitializeComponent();
            Load += Form57_CapNhatTinhThanhPho_Load;
            Shown += Form57_CapNhatTinhThanhPho_Shown;
            kryptonDataGridView1.SelectionChanged +=
                kryptonDataGridView1_SelectionChanged;
            InitToolTips();
        }
        private Form4_TrangDauTien? LayForm4TrangDauTien()
        {
            foreach (Form form in Application.OpenForms)
            {
                if (form is Form4_TrangDauTien frm &&
                    !frm.IsDisposed &&
                    !frm.Disposing)
                {
                    return frm;
                }
            }
            return null;
        }
        private void Form57_CapNhatTinhThanhPho_Load(object? sender, EventArgs e)
        {
            // 1. CẤU HÌNH FORM   
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.AcceptButton = kryptonButton1_Them;
            // 2. ĐẢM BẢO SỰ KIỆN CELL CLICK ĐƯỢC GẮN
            kryptonDataGridView1.SelectionChanged -= kryptonDataGridView1_SelectionChanged;
            kryptonDataGridView1.SelectionChanged += kryptonDataGridView1_SelectionChanged;
            // 3. TẠO BẢNG + LOAD DỮ LIỆU
            TaoBangNeuChuaCo();
            LoadDanhSachTinhVaThanhPho();  
            // 4. TOOLTIP    
            InitToolTips();
        }
        private void Form57_CapNhatTinhThanhPho_Shown(
            object? sender,
            EventArgs e)
        {
            textBox_TenTinhThanhPho.Focus();
        }
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            var tips = new Dictionary<Control, string>
            {
                { kryptonButton1_Them, "Thêm đơn vị mới" },
                { kryptonButton1_Sua, "Chỉnh sửa ten đơn vị đang chọn" },
                { kryptonButton1_Xoa, "Xóa đơn vị đang chọn" },
                { kryptonButton1_XuatData, "Xuất danh sách Tỉnh/Thành phố" },
                { kryptonButton1_NhapData, "Nhập danh sách Tỉnh/Thành phố" }
            };
            foreach (var tip in tips)
            {
                if (tip.Key != null) // an toàn khi refactor / ẩn control
                    toolTip1.SetToolTip(tip.Key, tip.Value);
            }
        }
        private void TaoBangNeuChuaCo()
        {
            if (!File.Exists(_csdl2Path))
                return;
            using var cn = new SqliteConnection($"Data Source={_csdl2Path}");
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS TinhVaThanhPho (
            ID INTEGER NOT NULL,
            TenTinhVaThanhPho TEXT,
            ThoiGian TEXT,
            PRIMARY KEY (ID)
        );";
            cmd.ExecuteNonQuery();
        }
        private void LoadDanhSachTinhVaThanhPho(int? selectID = null)
        {
            if (!File.Exists(_csdl2Path))
                return;
            try
            {
                using var cn = new SqliteConnection(
                    $"Data Source={_csdl2Path}");
                cn.Open();
                using var cmd = cn.CreateCommand();
                cmd.CommandText = """
            SELECT ID, TenTinhVaThanhPho, ThoiGian
            FROM TinhVaThanhPho
            ORDER BY ID ASC;
            """;
                var dt = new DataTable();
                using var reader = cmd.ExecuteReader();
                dt.Load(reader);
                // THÊM CỘT STT
                if (!dt.Columns.Contains("STT"))
                    dt.Columns.Add("STT", typeof(string));
                int stt = 1;
                foreach (DataRow row in dt.Rows)
                {
                    // TÊN TỈNH / THÀNH PHỐ
                    // LẤY ĐÚNG TỪ CSDL, KHÔNG LẤY TỪ TEXTBOX
                    string tenTinhVaThanhPho =
                        row["TenTinhVaThanhPho"]?
                            .ToString()?
                            .Trim()
                        ?? string.Empty;
                    row["TenTinhVaThanhPho"] =
                        ChuanHoaTenTinh(tenTinhVaThanhPho);
                    // THỜI GIAN
                    string thoiGian =
                        row["ThoiGian"]?
                            .ToString()?
                            .Trim()
                        ?? string.Empty;
                    if (DateTime.TryParse(
                            thoiGian,
                            out DateTime dtParsed))
                    {
                        row["ThoiGian"] =
                            dtParsed.ToString(
                                "dd-MM-yyyy HH:mm:ss");
                    }
                    else
                    {
                        row["ThoiGian"] = thoiGian;
                    }
                    // STT
                    row["STT"] = stt++;
                }
                // GÁN DỮ LIỆU CHO GRID
                kryptonDataGridView1.DataSource = null;
                kryptonDataGridView1.DataSource = dt;
                // TỔNG SỐ
                label_tongCongTinhThanhPho.Text =
                    $"Tổng cộng: {dt.Rows.Count:N0} đơn vị hành chính";
                // CẤU HÌNH GRID + CHỌN DÒNG
                CauHinhGiaoDienGrid(selectID);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể tải danh sách Tỉnh / Thành phố.\n\n" +
                    $"Chi tiết: {ex.Message}",
                    "Lỗi tải dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void CauHinhGiaoDienGrid(int? selectID)
        {
            var dgv = kryptonDataGridView1;
            if (dgv == null || dgv.Columns.Count == 0)
                return;
            dgv.SuspendLayout();
            try
            {
                // 1. CHIỀU CAO
                int fontHeight = dgv.Font.Height;
                dgv.ColumnHeadersHeightSizeMode =
                    DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                dgv.ColumnHeadersHeight = Math.Max(38, (int)(fontHeight * 2.3));
                dgv.RowTemplate.Height = Math.Max(32, (int)(fontHeight * 1.8));
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (!row.IsNewRow)
                        row.Height = dgv.RowTemplate.Height;
                }
                // 2. FONT
                dgv.DefaultCellStyle.Font = _fontData;
                var headerStyle = dgv.ColumnHeadersDefaultCellStyle;
                headerStyle.Font = _fontHeader;
                headerStyle.Alignment =
                    DataGridViewContentAlignment.MiddleCenter;
                // 3. NỀN CHÍNH - SÁNG NHƯ WEB
                dgv.StateCommon.Background.Color1 = Color.White;
                dgv.StateCommon.DataCell.Back.Color1 = Color.White;
                // 4. Ô DỮ LIỆU - VIỀN XÁM RẤT NHẸ
                dgv.StateCommon.DataCell.Border.Color1 =
                    Color.FromArgb(235, 238, 242);
                dgv.StateCommon.DataCell.Border.Width = 1;
                dgv.StateCommon.DataCell.Border.DrawBorders =
                    Krypton.Toolkit.PaletteDrawBorders.All;
                // 5. HEADER - XANH SÁNG, KIỂU WEB
                dgv.StateCommon.HeaderColumn.Back.Color1 =
                    Color.FromArgb(180, 210, 240);
                dgv.StateCommon.HeaderColumn.Back.Color2 =
                    Color.FromArgb(180, 210, 240);
                dgv.StateCommon.HeaderColumn.Content.Color1 =
                    Color.FromArgb(30, 30, 30);
                dgv.StateCommon.HeaderColumn.Border.Color1 =
                    Color.FromArgb(150, 180, 210);
                dgv.StateCommon.HeaderColumn.Border.Width = 1;
                // 6. DÒNG ĐƯỢC CHỌN
                dgv.StateSelected.DataCell.Back.Color1 =
                    Color.FromArgb(232, 244, 253);
                dgv.StateSelected.DataCell.Back.Color2 =
                    Color.FromArgb(232, 244, 253);
                dgv.StateSelected.DataCell.Content.Color1 =
                    Color.FromArgb(0, 102, 204);
                // 7. CẤU HÌNH CHUNG
                dgv.AutoSizeColumnsMode =
                    DataGridViewAutoSizeColumnsMode.Fill;
                dgv.AllowUserToAddRows = false;
                dgv.AllowUserToResizeRows = false;
                dgv.ReadOnly = true;
                dgv.SelectionMode =
                    DataGridViewSelectionMode.FullRowSelect;
                dgv.MultiSelect = false;
                dgv.RowHeadersVisible = false;
                dgv.EnableHeadersVisualStyles = false;
                dgv.Padding = new Padding(0);
                // 8. CỘT STT
                if (dgv.Columns.Contains("STT"))
                {
                    var col = dgv.Columns["STT"];
                    col.HeaderText = "STT";
                    col.DisplayIndex = 0;
                    // Khoảng 12% chiều rộng Grid
                    col.FillWeight = 12;
                    // Không cho cột STT bị quá hẹp
                    col.MinimumWidth = 60;
                    col.DefaultCellStyle.Alignment =
                        DataGridViewContentAlignment.MiddleCenter;
                }
                // 9. CỘT TÊN TỈNH / THÀNH PHỐ
                if (dgv.Columns.Contains("TenTinhVaThanhPho"))
                {
                    var col = dgv.Columns["TenTinhVaThanhPho"];
                    col.HeaderText = "Tên tỉnh / Thành phố";
                    col.DisplayIndex = 1;
                    // Cột chính - chiếm phần lớn giao diện
                    col.FillWeight = 63;
                    col.MinimumWidth = 200;
                    col.DefaultCellStyle.Alignment =
                        DataGridViewContentAlignment.MiddleLeft;
                }
                // 10. CỘT THỜI GIAN
                if (dgv.Columns.Contains("ThoiGian"))
                {
                    var col = dgv.Columns["ThoiGian"];
                    col.HeaderText = "Thời gian";
                    col.DisplayIndex = 2;
                    // Khoảng 25% chiều rộng Grid
                    col.FillWeight = 25;
                    col.MinimumWidth = 150;
                    col.DefaultCellStyle.Alignment =
                        DataGridViewContentAlignment.MiddleCenter;
                }
                // 11. ẨN ID
                if (dgv.Columns.Contains("ID"))
                {
                    dgv.Columns["ID"].Visible = false;
                }                // 8. CỘT STT
                if (dgv.Columns.Contains("STT"))
                {
                    var col = dgv.Columns["STT"];
                    col.HeaderText = "STT";
                    col.DisplayIndex = 0;
                    col.FillWeight = 18;
                    col.MinimumWidth = 60;
                    col.DefaultCellStyle.Alignment =
                        DataGridViewContentAlignment.MiddleCenter;
                }
                // 9. TÊN TỈNH / THÀNH PHỐ
                if (dgv.Columns.Contains("TenTinhVaThanhPho"))
                {
                    var col = dgv.Columns["TenTinhVaThanhPho"];
                    col.HeaderText = "Tên tỉnh / Thành phố";
                    col.DisplayIndex = 1;
                    col.FillWeight = 52;
                    col.MinimumWidth = 180;
                    col.DefaultCellStyle.Alignment =
                        DataGridViewContentAlignment.MiddleLeft;
                }
                // 10. THỜI GIAN
                if (dgv.Columns.Contains("ThoiGian"))
                {
                    var col = dgv.Columns["ThoiGian"];
                    col.HeaderText = "Thời gian";
                    col.DisplayIndex = 2;
                    col.FillWeight = 30;
                    col.MinimumWidth = 150;
                    col.DefaultCellStyle.Alignment =
                        DataGridViewContentAlignment.MiddleCenter;
                }
                // 11. ẨN ID
                if (dgv.Columns.Contains("ID"))
                    dgv.Columns["ID"].Visible = false;
                // 12. XÓA LỰA CHỌN CŨ
                dgv.ClearSelection();
                // 13. CHỌN DÒNG VỪA THÊM / VỪA SỬA
                if (selectID.HasValue &&
                    dgv.Columns.Contains("ID"))
                {
                    foreach (DataGridViewRow row in dgv.Rows)
                    {
                        if (row.IsNewRow)
                            continue;
                        if (row.Cells["ID"].Value != null &&
                            int.TryParse(
                                row.Cells["ID"].Value.ToString(),
                                out int id) &&
                            id == selectID.Value)
                        {
                            row.Selected = true;
                            if (row.Index >= 0 &&
                                row.Index < dgv.RowCount)
                            {
                                dgv.FirstDisplayedScrollingRowIndex =
                                    row.Index;
                            }
                            return;
                        }
                    }
                }
                // 14. KHÔNG CÓ ID → CHỌN DÒNG CUỐI
                if (dgv.Rows.Count > 0)
                {
                    int lastIndex = dgv.Rows.Count - 1;
                    if (!dgv.Rows[lastIndex].IsNewRow)
                    {
                        dgv.Rows[lastIndex].Selected = true;
                        if (lastIndex >= 0 &&
                            lastIndex < dgv.RowCount)
                        {
                            dgv.FirstDisplayedScrollingRowIndex =
                                lastIndex;
                        }
                    }
                }
            }
            finally
            {
                dgv.ResumeLayout();
            }
        }
        private void kryptonDataGridView1_SelectionChanged(
     object? sender,
     EventArgs e)
        {
            var dgv = kryptonDataGridView1;
            if (dgv.CurrentRow == null ||
                dgv.CurrentRow.IsNewRow)
            {
                return;
            }
            if (!dgv.Columns.Contains("ID") ||
                !dgv.Columns.Contains("TenTinhVaThanhPho"))
            {
                return;
            }
            var row = dgv.CurrentRow;
            if (row.Cells["ID"].Value == null ||
                !int.TryParse(
                    row.Cells["ID"].Value.ToString(),
                    out int id))
            {
                return;
            }
            string ten =
                row.Cells["TenTinhVaThanhPho"].Value?
                    .ToString()?
                    .Trim()
                ?? string.Empty;
            _selectedID = id;
            textBox_TenTinhThanhPho.Text = ten;
            isEditing = true;
            kryptonButton1_Sua.Text = "Lưu";
            AcceptButton = kryptonButton1_Sua;
        }
        private void ResetInput()
        {
            _selectedID = -1;
            isEditing = false;
            textBox_TenTinhThanhPho.Clear();
            kryptonButton1_Sua.Text = "Sửa";
            AcceptButton = kryptonButton1_Them;
            kryptonDataGridView1.ClearSelection();
            textBox_TenTinhThanhPho.Focus();
        }
        private async void kryptonButton1_NhapData_Click(object? sender, EventArgs e)
        {
            using var openFileDialog = new OpenFileDialog
            {
                Title = "Chọn tệp Excel chứa dữ liệu Tỉnh và Thành phố",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                DefaultExt = "xlsx",
                AddExtension = true,
                Multiselect = false,
                CheckFileExists = true,
                RestoreDirectory = true
            };
            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;
            string filePath = openFileDialog.FileName;
            int soTrungTrongExcel = 0, soDongRong = 0, soTrungCSDL = 0, soLuongHienCo = 0;
            string textBanDau = kryptonButton1_NhapData.Values.Text;
            Image? anhBanDau = kryptonButton1_NhapData.Values.Image;
            try
            {
                kryptonButton1_NhapData.Enabled = false;
                kryptonButton1_NhapData.Values.Text = "Đang nhập...";
                kryptonButton1_NhapData.Values.Image = null;
                this.Enabled = false;
                await Task.Delay(50);
                if (!File.Exists(filePath))
                {
                    MessageBox.Show(
                        "Tệp Excel không tồn tại hoặc đã bị di chuyển.",
                        "Lỗi tệp Excel",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
                var danhSachExcel = new List<TinhVaThanhPhoDto>();
                using (var workbook = new XLWorkbook(filePath))
                {
                    const string tenSheet = "DuLieuTinhVaThanhPho";
                    if (!workbook.Worksheets.TryGetWorksheet(tenSheet, out IXLWorksheet? ws))
                    {
                        MessageBox.Show(
                            $"Không tìm thấy sheet '{tenSheet}'.\n\nTệp Excel không đúng cấu trúc được tạo bởi chức năng Xuất dữ liệu.",
                            "Sai cấu trúc tệp Excel",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
                    string tieuDeA1 = ws.Cell("A1").GetString().Trim();
                    if (!string.Equals(tieuDeA1, "DANH SÁCH TỈNH VÀ THÀNH PHỐ", StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show(
                            "Tệp Excel không đúng định dạng.\n\nÔ A1 phải là:\nDANH SÁCH TỈNH VÀ THÀNH PHỐ",
                            "Sai cấu trúc tệp Excel",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
                    string tieuDeA2 = ws.Cell("A2").GetString().Trim();
                    string tieuDeB2 = ws.Cell("B2").GetString().Trim();
                    if (!string.Equals(tieuDeA2, "STT", StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(tieuDeB2, "Tên Tỉnh và Thành phố", StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show(
                            "Cấu trúc tệp Excel không đúng.\n\nYêu cầu:\nA2 = STT\nB2 = Tên Tỉnh và Thành phố",
                            "Sai cấu trúc tệp Excel",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
                    int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                    if (lastRow < 3)
                    {
                        MessageBox.Show(
                            "Tệp Excel không có dữ liệu Tỉnh / Thành phố để nhập.",
                            "Thông báo",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        return;
                    }
                    var tenDaCoTrongExcel = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    for (int rowIndex = 3; rowIndex <= lastRow; rowIndex++)
                    {
                        string ten = ws.Cell(rowIndex, 2).GetString().Trim();
                        if (string.IsNullOrWhiteSpace(ten))
                        {
                            soDongRong++;
                            continue;
                        }
                        ten = ChuanHoaTenTinh(ten);
                        if (string.IsNullOrWhiteSpace(ten))
                        {
                            soDongRong++;
                            continue;
                        }
                        if (!tenDaCoTrongExcel.Add(ten))
                        {
                            soTrungTrongExcel++;
                            continue;
                        }
                        danhSachExcel.Add(new TinhVaThanhPhoDto
                        {
                            STT = danhSachExcel.Count + 1,
                            TenTinhVaThanhPho = ten
                        });
                    }
                }
                if (danhSachExcel.Count == 0)
                {
                    MessageBox.Show(
                        "Không tìm thấy Tỉnh / Thành phố hợp lệ trong tệp Excel.\n\n" +
                        $"Dòng trống / không hợp lệ: {soDongRong:N0}\n" +
                        $"Dòng trùng trong Excel: {soTrungTrongExcel:N0}",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }
                DialogResult ketQuaAdmin;
                using (var frm = new Form24_XacMinhAdmin())
                {
                    frm.TopMost = true;
                    frm.StartPosition = FormStartPosition.CenterScreen;
                    ketQuaAdmin = frm.ShowDialog(this);
                }
                if (ketQuaAdmin != DialogResult.OK)
                    return;
                if (!File.Exists(_csdl2Path))
                {
                    MessageBox.Show(
                        "Không tìm thấy cơ sở dữ liệu CSDL2.",
                        "Lỗi cơ sở dữ liệu",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
                using var cn = new SqliteConnection($"Data Source={_csdl2Path}");
                await cn.OpenAsync();
                using (var cmdCount = cn.CreateCommand())
                {
                    cmdCount.CommandText = "SELECT COUNT(*) FROM TinhVaThanhPho;";
                    object? result = await cmdCount.ExecuteScalarAsync();
                    soLuongHienCo = Convert.ToInt32(result);
                }
                DialogResult luaChon;
                if (soLuongHienCo > 0)
                {
                    luaChon = MessageBox.Show(
                        $"Hiện tại phần mềm đang có {soLuongHienCo:N0} Tỉnh / Thành phố trong CSDL.\n\n" +
                        $"Tệp Excel bạn chọn có {danhSachExcel.Count:N0} Tỉnh / Thành phố hợp lệ.\n" +
                        "Bạn muốn thực hiện theo cách nào?\n\n" +
                        "- Chọn Yes để Xóa danh sách hiện tại và thay thế bằng danh sách trong tệp Excel.\n\n" +
                        "- Chọn No để Giữ nguyên danh sách hiện tại và chỉ bổ sung những Tỉnh / Thành phố chưa có.\n\n" +
                        "- Chọn Cancel để Không thực hiện thay đổi nào.",
                        "Nhập danh sách Tỉnh / Thành phố",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);
                    if (luaChon == DialogResult.Cancel)
                        return;
                }
                else
                {
                    luaChon = DialogResult.No;
                }
                if (luaChon == DialogResult.Yes)
                {
                    DialogResult xacNhanXoa = MessageBox.Show(
                        $"BẠN ĐANG CHỌN XÓA {soLuongHienCo:N0} Tỉnh / Thành phố hiện có.\n\n" +
                        $"Sau đó sẽ nạp {danhSachExcel.Count:N0} Tỉnh / Thành phố từ Excel.\n\n" +
                        "Dữ liệu cũ sẽ bị xóa hoàn toàn.\n\n" +
                        "Bạn có chắc chắn muốn thực hiện?",
                        "Xác nhận xóa và nạp lại",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    if (xacNhanXoa != DialogResult.Yes)
                        return;
                    using var transactionXoa = cn.BeginTransaction();
                    try
                    {
                        using (var cmdDelete = cn.CreateCommand())
                        {
                            cmdDelete.Transaction = transactionXoa;
                            cmdDelete.CommandText = "DELETE FROM TinhVaThanhPho;";
                            await cmdDelete.ExecuteNonQueryAsync();
                        }
                        using var cmdInsert = cn.CreateCommand();
                        cmdInsert.Transaction = transactionXoa;
                        cmdInsert.CommandText = """
                    INSERT INTO TinhVaThanhPho
                    (
                        TenTinhVaThanhPho,
                        ThoiGian
                    )
                    VALUES
                    (
                        @ten,
                        @thoigian
                    );
                    """;
                        var parameterTen = cmdInsert.Parameters.Add("@ten", SqliteType.Text);
                        var parameterThoiGian = cmdInsert.Parameters.Add("@thoigian", SqliteType.Text);
                        string thoiGianNhap = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        foreach (var dto in danhSachExcel)
                        {
                            parameterTen.Value = dto.TenTinhVaThanhPho.Trim();
                            parameterThoiGian.Value = thoiGianNhap;
                            await cmdInsert.ExecuteNonQueryAsync();
                        }
                        await transactionXoa.CommitAsync();
                    }
                    catch
                    {
                        try
                        {
                            await transactionXoa.RollbackAsync();
                        }
                        catch
                        {
                        }
                        throw;
                    }
                    LoadDanhSachTinhVaThanhPho();
                    ResetInput();
                    var frmTrangChu = LayForm4TrangDauTien();
                    if (frmTrangChu != null)
                        await frmTrangChu.LoadDiaDiemAsync();
                    string thongBao = $"Nạp lại dữ liệu thành công! Đã xóa {soLuongHienCo:N0} và nạp mới {danhSachExcel.Count:N0} Tỉnh / Thành phố.";
                    Module_ThongBao.ThanhCong(thongBao);
                    Module_NhatKy.GhiNhatKy(
                        taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                        hanhDong: thongBao,
                        ghiChu: "Thành công");
                    return;
                }
                var danhSachDaCo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var cmdCheck = cn.CreateCommand())
                {
                    cmdCheck.CommandText = """
                SELECT TenTinhVaThanhPho
                FROM TinhVaThanhPho
                WHERE TenTinhVaThanhPho IS NOT NULL
                  AND TRIM(TenTinhVaThanhPho) <> '';
                """;
                    using var reader = await cmdCheck.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        string tenHienTai = reader["TenTinhVaThanhPho"]?.ToString()?.Trim() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(tenHienTai))
                            continue;
                        tenHienTai = ChuanHoaTenTinh(tenHienTai);
                        if (!string.IsNullOrWhiteSpace(tenHienTai))
                            danhSachDaCo.Add(tenHienTai);
                    }
                }
                var danhSachNhap = new List<TinhVaThanhPhoDto>();
                foreach (var dto in danhSachExcel)
                {
                    string ten = ChuanHoaTenTinh(dto.TenTinhVaThanhPho);
                    if (string.IsNullOrWhiteSpace(ten))
                        continue;
                    if (!danhSachDaCo.Add(ten))
                    {
                        soTrungCSDL++;
                        continue;
                    }
                    danhSachNhap.Add(new TinhVaThanhPhoDto
                    {
                        STT = danhSachNhap.Count + 1,
                        TenTinhVaThanhPho = ten
                    });
                }
                if (danhSachNhap.Count == 0)
                {
                    MessageBox.Show(
                        "Không có Tỉnh / Thành phố mới để nạp.\n\n" +
                        $"Excel hợp lệ: {danhSachExcel.Count:N0}\n" +
                        $"Đã tồn tại trong CSDL: {soTrungCSDL:N0}\n" +
                        $"Trùng trong Excel: {soTrungTrongExcel:N0}\n" +
                        $"Dòng trống / không hợp lệ: {soDongRong:N0}",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }
                DialogResult xacNhanThem = MessageBox.Show(
                    $"CSDL hiện có: {soLuongHienCo:N0}\n" +
                    $"Excel hợp lệ: {danhSachExcel.Count:N0}\n\n" +
                    $"Sẽ thêm mới: {danhSachNhap.Count:N0}\n" +
                    $"Đã tồn tại CSDL: {soTrungCSDL:N0}\n" +
                    $"Trùng trong Excel: {soTrungTrongExcel:N0}\n\n" +
                    "Bạn có muốn nạp thêm dữ liệu mới không?",
                    "Xác nhận nạp thêm",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (xacNhanThem != DialogResult.Yes)
                    return;
                using var transactionThem = cn.BeginTransaction();
                try
                {
                    using var cmdInsert = cn.CreateCommand();
                    cmdInsert.Transaction = transactionThem;
                    cmdInsert.CommandText = """
                INSERT INTO TinhVaThanhPho
                (
                    TenTinhVaThanhPho,
                    ThoiGian
                )
                VALUES
                (
                    @ten,
                    @thoigian
                );
                """;
                    var parameterTen = cmdInsert.Parameters.Add("@ten", SqliteType.Text);
                    var parameterThoiGian = cmdInsert.Parameters.Add("@thoigian", SqliteType.Text);
                    string thoiGianNhap = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    foreach (var dto in danhSachNhap)
                    {
                        parameterTen.Value = dto.TenTinhVaThanhPho.Trim();
                        parameterThoiGian.Value = thoiGianNhap;
                        await cmdInsert.ExecuteNonQueryAsync();
                    }
                    await transactionThem.CommitAsync();
                }
                catch
                {
                    try
                    {
                        await transactionThem.RollbackAsync();
                    }
                    catch
                    {
                    }
                    throw;
                }
                LoadDanhSachTinhVaThanhPho();
                ResetInput();
                var form4 = LayForm4TrangDauTien();
                if (form4 != null)
                    await form4.LoadDiaDiemAsync();
                Module_ThongBao.ThanhCong(
                    $"Nạp dữ liệu thành công! Đã thêm {danhSachNhap.Count:N0} Tỉnh / Thành phố. Bỏ qua {soTrungCSDL:N0} dòng trùng CSDL.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể nhập dữ liệu từ Excel.\n\n" +
                    $"Chi tiết: {ex.Message}",
                    "Lỗi nhập dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                this.Enabled = true;
                kryptonButton1_NhapData.Values.Text = textBanDau;
                kryptonButton1_NhapData.Values.Image = anhBanDau;
                kryptonButton1_NhapData.Enabled = true;
                this.Focus();
            }
        }
        private async void kryptonButton1_XuatData_Click(object? sender, EventArgs e)
        {
            if (!File.Exists(_csdl2Path))
            {
                MessageBox.Show(
                    "Không tìm thấy cơ sở dữ liệu CSDL2.",
                    "Lỗi cơ sở dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            string fileName = $"DanhSachTinhVaThanhPho_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            using var saveFileDialog = new SaveFileDialog
            {
                Title = "Chọn nơi lưu danh sách Tỉnh / Thành phố",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                DefaultExt = "xlsx",
                AddExtension = true,
                FileName = fileName,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                OverwritePrompt = true
            };
            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;
            string filePath = saveFileDialog.FileName;
            string textBanDau = kryptonButton1_XuatData.Values.Text;
            Image? anhBanDau = kryptonButton1_XuatData.Values.Image;
            try
            {
                kryptonButton1_XuatData.Enabled = false;
                kryptonButton1_XuatData.Values.Text = "Đang xuất...";
                kryptonButton1_XuatData.Values.Image = null;
                this.Enabled = false;
                await Task.Delay(50);
                // Kiểm tra thư mục đích còn tồn tại không (phòng trường hợp ổ đĩa/thư mục bị rút/xóa
                // giữa lúc chọn nơi lưu và lúc thực sự ghi file). Không kiểm tra filePath vì file
                // đích CHƯA được tạo ra ở bước này — nó chỉ được tạo bên trong workbook.SaveAs().
                string? thuMucDich = Path.GetDirectoryName(filePath);
                if (string.IsNullOrEmpty(thuMucDich) || !Directory.Exists(thuMucDich))
                {
                    MessageBox.Show(
                        "Thư mục lưu tệp Excel không tồn tại hoặc đã bị di chuyển.",
                        "Lỗi đường dẫn",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
                var danhSachDto = new List<TinhVaThanhPhoDto>();
                using (var cn = new SqliteConnection($"Data Source={_csdl2Path}"))
                {
                    await cn.OpenAsync();
                    using var cmd = cn.CreateCommand();
                    cmd.CommandText = """
                SELECT ID, TenTinhVaThanhPho
                FROM TinhVaThanhPho
                WHERE TenTinhVaThanhPho IS NOT NULL
                  AND TRIM(TenTinhVaThanhPho) <> ''
                ORDER BY ID ASC;
                """;
                    using var reader = await cmd.ExecuteReaderAsync();
                    int stt = 1;
                    while (await reader.ReadAsync())
                    {
                        string ten = reader["TenTinhVaThanhPho"]?.ToString()?.Trim() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(ten))
                            continue;
                        danhSachDto.Add(new TinhVaThanhPhoDto
                        {
                            STT = stt++,
                            TenTinhVaThanhPho = ChuanHoaTenTinh(ten)
                        });
                    }
                }
                if (danhSachDto.Count == 0)
                {
                    MessageBox.Show(
                        "Cơ sở dữ liệu hiện không có Tỉnh / Thành phố để xuất.",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }
                var dataList = danhSachDto
                    .Select(dto => new object[]
                    {
                dto.STT,
                dto.TenTinhVaThanhPho
                    })
                    .ToList();
                int rowCount = dataList.Count;
                const int colCount = 2;
                try
                {
                    await Task.Run(() =>
                    {
                        using var workbook = new XLWorkbook();
                        var ws = workbook.Worksheets.Add("DuLieuTinhVaThanhPho");
                        ws.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                        ws.Style.Font.FontSize = 11;
                        ws.Cell("A1").Value = "DANH SÁCH TỈNH VÀ THÀNH PHỐ";
                        var titleRange = ws.Range(1, 1, 1, colCount);
                        titleRange.Merge();
                        titleRange.Style.Font.Bold = true;
                        titleRange.Style.Font.FontSize = 14;
                        titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        ws.Row(1).Height = 28;
                        const int headerRow = 2;
                        ws.Cell(headerRow, 1).Value = "STT";
                        ws.Cell(headerRow, 2).Value = "Tên Tỉnh và Thành phố";
                        var headerRange = ws.Range(headerRow, 1, headerRow, colCount);
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(217, 225, 242);
                        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                        ws.Row(headerRow).Height = 30;
                        int dataStartRow = headerRow + 1;
                        ws.Cell(dataStartRow, 1).InsertData(dataList);
                        var dataRange = ws.Range(
                            dataStartRow,
                            1,
                            dataStartRow + rowCount - 1,
                            colCount);
                        dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        dataRange.Style.Alignment.WrapText = true;
                        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                        ws.Range(
                            dataStartRow,
                            1,
                            dataStartRow + rowCount - 1,
                            1)
                            .Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Range(
                            dataStartRow,
                            2,
                            dataStartRow + rowCount - 1,
                            2)
                            .Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        ws.Column(1).Width = 8;
                        ws.Column(2).Width = 45;
                        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
                        ws.PageSetup.PageOrientation = XLPageOrientation.Portrait;
                        ws.PageSetup.FitToPages(1, 0);
                        ws.PageSetup.Margins.Top = 0.5;
                        ws.PageSetup.Margins.Bottom = 0.5;
                        ws.PageSetup.Margins.Left = 0.5;
                        ws.PageSetup.Margins.Right = 0.5;
                        Module_BanQuyen.DongDauExcel(workbook);
                        workbook.SaveAs(filePath);
                    });
                }
                catch (IOException ioEx)
                {
                    // Thường gặp khi tệp đích đang được mở bởi Excel hoặc chương trình khác.
                    MessageBox.Show(
                        "Không thể ghi tệp Excel. Tệp có thể đang được mở bởi chương trình khác.\n\n" +
                        $"Chi tiết: {ioEx.Message}",
                        "Lỗi ghi tệp",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
                // Xác nhận file thực sự đã được tạo ra sau khi SaveAs hoàn tất.
                if (!File.Exists(filePath))
                {
                    MessageBox.Show(
                        "Xuất Excel thất bại: không tìm thấy tệp sau khi lưu.",
                        "Lỗi xuất dữ liệu",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
                try
                {
                    Module_XuatNhapDuLieuThiDua.MoVaChonTepTrongExplorer(filePath);
                }
                catch
                {
                }
                Module_ThongBao.ThanhCong(
                    $"Xuất Excel thành công! Đã xuất {danhSachDto.Count:N0} Tỉnh / Thành phố.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể xuất dữ liệu ra Excel.\n\n" +
                    $"Chi tiết: {ex.Message}",
                    "Lỗi xuất dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                this.Enabled = true;
                kryptonButton1_XuatData.Values.Text = textBanDau;
                kryptonButton1_XuatData.Values.Image = anhBanDau;
                kryptonButton1_XuatData.Enabled = true;
                this.Focus();
            }
        }
        private void kryptonButton1_Them_Click(object? sender, EventArgs e)
                {
                    // 1. LẤY + CHUẨN HÓA DỮ LIỆU
                    string tenTinhVaThanhPho =
                        ChuanHoaTenTinh(textBox_TenTinhThanhPho.Text);
                    if (string.IsNullOrWhiteSpace(tenTinhVaThanhPho))
                    {
                        MessageBox.Show(
                            "Chưa nhập tên Tỉnh / Thành phố để lưu vào cơ sở dữ liệu!",
                            "Thông báo",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        textBox_TenTinhThanhPho.Focus();
                        return;
                    }
                    // 2. KIỂM TRA CSDL
                    if (!File.Exists(_csdl2Path))
                    {
                        MessageBox.Show(
                            "Không tìm thấy cơ sở dữ liệu CSDL2.",
                            "Lỗi cơ sở dữ liệu",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                    try
                    {
                        using var cn =
                            new SqliteConnection($"Data Source={_csdl2Path}");
                        cn.Open();
                        // 3. KIỂM TRA TRÙNG - SQL CHỈ LẤY 1 BẢN GHI
                        using (var cmdCheck = cn.CreateCommand())
                        {
                            cmdCheck.CommandText = """
                        SELECT ID, ThoiGian
                        FROM TinhVaThanhPho
                        WHERE LOWER(TRIM(TenTinhVaThanhPho))
                              = LOWER(TRIM(@ten))
                        LIMIT 1;
                        """;
                            cmdCheck.Parameters.Add(
                                "@ten",
                                SqliteType.Text).Value =
                                    tenTinhVaThanhPho;
                            using var reader = cmdCheck.ExecuteReader();
                            if (reader.Read())
                            {
                                int existingID =
                                    reader.GetInt32(0);
                                string thoiGian =
                                    reader.IsDBNull(1)
                                        ? string.Empty
                                        : reader.GetString(1).Trim();
                                MessageBox.Show(
                                    $"Tỉnh / Thành phố '{tenTinhVaThanhPho}' " +
                                    $"(ID {existingID}) đã tồn tại.\n\n" +
                                    $"Thời gian tạo: {thoiGian}\n\n" +
                                    "Bạn không thể thêm trùng tên.",
                                    "Dữ liệu đã tồn tại",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                                textBox_TenTinhThanhPho.Focus();
                                textBox_TenTinhThanhPho.SelectAll();
                                return;
                            }
                        }
                        // 4. THÊM MỚI
                        string thoiGianMoi =
                            DateTime.Now.ToString(
                                "yyyy-MM-dd HH:mm:ss",
                                CultureInfo.InvariantCulture);
                        using var cmdInsert = cn.CreateCommand();
                        cmdInsert.CommandText = """
                    INSERT INTO TinhVaThanhPho
                    (
                        TenTinhVaThanhPho,
                        ThoiGian
                    )
                    VALUES
                    (
                        @ten,
                        @thoigian
                    );
                    SELECT last_insert_rowid();
                    """;
                        cmdInsert.Parameters.Add(
                            "@ten",
                            SqliteType.Text).Value =
                                tenTinhVaThanhPho;
                        cmdInsert.Parameters.Add(
                            "@thoigian",
                            SqliteType.Text).Value =
                                thoiGianMoi;
                        int newID =
                            Convert.ToInt32(
                                cmdInsert.ExecuteScalar());
                        // 5. CẬP NHẬT GIAO DIỆN
                        LoadDanhSachTinhVaThanhPho(newID);
                        ResetInput();
                        // 6. CẬP NHẬT COMBOBOX FORM4
                        var form4 = LayForm4TrangDauTien();
                        if (form4 != null)
                        {
                            _ = form4.LoadDiaDiemAsync();
                        }
                    }
                    catch (SqliteException ex)
                    {
                        MessageBox.Show(
                            "Không thể thêm Tỉnh / Thành phố.\n\n" +
                            $"Chi tiết CSDL: {ex.Message}",
                            "Lỗi cơ sở dữ liệu",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "Không thể thêm Tỉnh / Thành phố.\n\n" +
                            $"Chi tiết: {ex.Message}",
                            "Lỗi",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
        private void kryptonButton1_Sua_Click(object? sender, EventArgs e)
        {
            // 1. KIỂM TRA TRẠNG THÁI SỬA
            if (!isEditing || _selectedID < 0)
            {
                MessageBox.Show(
                    "Bạn chưa chọn Tỉnh / Thành phố để sửa!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            // 2. LẤY + CHUẨN HÓA TÊN MỚI
            string tenTinhVaThanhPhoMoi =
                ChuanHoaTenTinh(textBox_TenTinhThanhPho.Text);
            if (string.IsNullOrWhiteSpace(tenTinhVaThanhPhoMoi))
            {
                MessageBox.Show(
                    "Chưa nhập tên Tỉnh / Thành phố!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                textBox_TenTinhThanhPho.Focus();
                return;
            }
            // 3. KIỂM TRA CSDL
            if (!File.Exists(_csdl2Path))
            {
                MessageBox.Show(
                    "Không tìm thấy cơ sở dữ liệu CSDL2.",
                    "Lỗi cơ sở dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            try
            {
                using var cn =
                    new SqliteConnection($"Data Source={_csdl2Path}");
                cn.Open();
                // 4. KIỂM TRA TRÙNG
                //    - Không kiểm tra chính ID đang sửa
                //    - SQL xử lý trực tiếp
                //    - Chỉ lấy đúng 1 dòng nếu bị trùng
                using (var cmdCheck = cn.CreateCommand())
                {
                    cmdCheck.CommandText = """
                SELECT ID
                FROM TinhVaThanhPho
                WHERE ID <> @id
                  AND LOWER(TRIM(TenTinhVaThanhPho))
                      = LOWER(TRIM(@ten))
                LIMIT 1;
                """;
                    cmdCheck.Parameters.Add(
                        "@id",
                        SqliteType.Integer).Value =
                            _selectedID;
                    cmdCheck.Parameters.Add(
                        "@ten",
                        SqliteType.Text).Value =
                            tenTinhVaThanhPhoMoi;
                    object? result =
                        cmdCheck.ExecuteScalar();
                    if (result != null &&
                        result != DBNull.Value)
                    {
                        int existingID =
                            Convert.ToInt32(result);
                        MessageBox.Show(
                            $"Tỉnh / Thành phố '{tenTinhVaThanhPhoMoi}' " +
                            $"(ID {existingID}) đã tồn tại.\n\n" +
                            "Bạn không thể lưu trùng tên.",
                            "Dữ liệu đã tồn tại",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        textBox_TenTinhThanhPho.Focus();
                        textBox_TenTinhThanhPho.SelectAll();
                        return;
                    }
                }
                // 5. CẬP NHẬT
                string thoiGianSua =
                    DateTime.Now.ToString(
                        "yyyy-MM-dd HH:mm:ss",
                        CultureInfo.InvariantCulture);
                using var cmdUpdate = cn.CreateCommand();
                cmdUpdate.CommandText = """
            UPDATE TinhVaThanhPho
            SET
                TenTinhVaThanhPho = @ten,
                ThoiGian = @thoigian
            WHERE ID = @id;
            """;
                cmdUpdate.Parameters.Add(
                    "@ten",
                    SqliteType.Text).Value =
                        tenTinhVaThanhPhoMoi;
                cmdUpdate.Parameters.Add(
                    "@thoigian",
                    SqliteType.Text).Value =
                        thoiGianSua;
                cmdUpdate.Parameters.Add(
                    "@id",
                    SqliteType.Integer).Value =
                        _selectedID;
                int affectedRows =
                    cmdUpdate.ExecuteNonQuery();
                // 6. KIỂM TRA KẾT QUẢ UPDATE
                if (affectedRows == 0)
                {
                    MessageBox.Show(
                        "Không tìm thấy Tỉnh / Thành phố cần cập nhật.\n\n" +
                        "Có thể dữ liệu đã bị thay đổi hoặc xóa.",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    LoadDanhSachTinhVaThanhPho();
                    ResetInput();
                    return;
                }
                // 7. LOAD LẠI + GIỮ DÒNG VỪA SỬA
                int selectedID = _selectedID;
                LoadDanhSachTinhVaThanhPho(selectedID);
                ResetInput();
                // 8. CẬP NHẬT COMBOBOX FORM4
                var form4 = LayForm4TrangDauTien();
                if (form4 != null)
                {
                    _ = form4.LoadDiaDiemAsync();
                }
            }
            catch (SqliteException ex)
            {
                MessageBox.Show(
                    "Không thể cập nhật Tỉnh / Thành phố.\n\n" +
                    $"Chi tiết CSDL: {ex.Message}",
                    "Lỗi cơ sở dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể cập nhật Tỉnh / Thành phố.\n\n" +
                    $"Chi tiết: {ex.Message}",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void kryptonButton1_Xoa_Click(object? sender, EventArgs e)
        {
            // 1. KIỂM TRA ĐÃ CHỌN DÒNG
            if (!isEditing || _selectedID < 0)
            {
                MessageBox.Show(
                    "Bạn chưa chọn Tỉnh / Thành phố để xóa!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            string tenTinhVaThanhPho = ChuanHoaTenTinh(textBox_TenTinhThanhPho.Text);
            if (string.IsNullOrWhiteSpace(tenTinhVaThanhPho)){ MessageBox.Show("Không xác định được tên Tỉnh / Thành phố cần xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning); 
                return; }
            // 2. XÁC NHẬN XÓA
            DialogResult xacNhan =
                MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa Tỉnh / Thành phố:\n" +
                    $"\"{tenTinhVaThanhPho}\"\n" +
                    $"ID: {_selectedID}\n" +
                    "Dữ liệu sẽ bị xóa khỏi danh mục Tỉnh / Thành phố.",
                    "Xác nhận xóa",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
            if (xacNhan != DialogResult.Yes)
                return;
            // 3. KIỂM TRA CSDL
            if (!File.Exists(_csdl2Path))
            {
                MessageBox.Show(
                    "Không tìm thấy cơ sở dữ liệu CSDL2.",
                    "Lỗi cơ sở dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            try
            {
                using var cn =
                    new SqliteConnection($"Data Source={_csdl2Path}");
                cn.Open();
                // 4. XÓA
                using var cmd = cn.CreateCommand();
                cmd.CommandText = """
            DELETE FROM TinhVaThanhPho
            WHERE ID = @id;
            """;
                cmd.Parameters.Add(
                    "@id",
                    SqliteType.Integer).Value =
                        _selectedID;
                int soDongDaXoa =
                    cmd.ExecuteNonQuery();
                if (soDongDaXoa == 0)
                {
                    MessageBox.Show(
                        "Không tìm thấy dữ liệu cần xóa.\n\n" +
                        "Có thể dữ liệu đã bị xóa trước đó.",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    LoadDanhSachTinhVaThanhPho();
                    ResetInput();
                    return;
                }
                // 5. LOAD LẠI + RESET
                LoadDanhSachTinhVaThanhPho();
                ResetInput();  
                // 6. CẬP NHẬT COMBOBOX TRÊN FORM4''
                var form4 = LayForm4TrangDauTien();
                if (form4 != null)
                {
                    _ = form4.LoadDiaDiemAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể xóa Tỉnh / Thành phố.\n\n" +
                    $"Chi tiết: {ex.Message}",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private static string ChuanHoaTenTinh(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            return string.Join(
                ' ',
                value.Trim()
                     .Split(
                         ' ',
                         StringSplitOptions.RemoveEmptyEntries));
        }
    }
    public sealed class TinhVaThanhPhoDto
    {
        public int STT { get; set; }
        public string TenTinhVaThanhPho { get; set; } = string.Empty;
        public string ThoiGian { get; set; } = string.Empty;
    }
}
