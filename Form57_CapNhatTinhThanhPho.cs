using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Globalization;
using System.Text;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form57_CapNhatTinhThanhPho : Form
    {
        // Đọc đường dẫn MỚI NHẤT mỗi lần dùng (sau khi khôi phục/ghi đè CSDL, Module_DanduongGPS.NapLaiDuongDan() đổi giá trị này)
        private static string _csdl2Path => Module_DanduongGPS.DuongDanCSDL2;
        private int _selectedID = -1;
        private string _selectedTen = string.Empty;    // tên GỐC của dòng đang chọn (xác nhận xóa + phát hiện "sửa nhưng không đổi")
        private bool isEditing = false;
        private int _chanChonLuoi;                     // >0: code đang tự nạp / bỏ chọn lưới => bỏ qua SelectionChanged
        private bool _daHienLanDau;                    // false tới khi form hiện xong: bỏ qua mọi SelectionChanged "tự phát" lúc dựng lưới
        private bool _dangBan;                         // đang nhập/xuất Excel: chặn đóng form giữa chừng
        private DataTable? _dtHienTai;
        private readonly Font _fontHeader = new Font(SystemFonts.MessageBoxFont!.FontFamily, 10f, FontStyle.Bold);
        private readonly Font _fontData = new Font(SystemFonts.MessageBoxFont.FontFamily, 9f);
        public Form57_CapNhatTinhThanhPho()
        {
            InitializeComponent();
            Module_BieuTuongNut.ApDung(this);   // biểu tượng nút: Hiện đại = tự vẽ, Cổ điển = ảnh gốc nhúng trong exe
            // Sự kiện Load đã được Designer gắn sẵn => KHÔNG gắn lại ở đây (bản cũ gắn thêm lần 2 làm Load chạy 2 lần: đọc CSDL + bind lưới 2 lần)
            FormBorderStyle = FormBorderStyle.FixedSingle;     // đặt từ constructor để form không "nhảy" khung lúc vừa hiện
            MaximizeBox = false;
            AcceptButton = kryptonButton1_Them;
            Module_GiaoDienForm57Web.ApDung(this);                    // CHỈ trình bày (giao diện kiểu trang web), không có logic nghiệp vụ
            textBox_TenTinhThanhPho.MaxLength = 100;
            kryptonDataGridView1.SelectionChanged += kryptonDataGridView1_SelectionChanged;
            Shown += Form57_CapNhatTinhThanhPho_Shown;
            InitToolTips();
            Disposed += (s, e) =>
            {
                _fontHeader.Dispose();
                _fontData.Dispose();
                _dtHienTai?.Dispose();
                _dtHienTai = null;
            };
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
        // Kết nối dùng chung: Mode=ReadWrite => nếu tệp CSDL bị xóa/đổi tên giữa lúc kiểm tra File.Exists và lúc mở thì báo lỗi,
        // thay vì SQLite lặng lẽ tạo ra một tệp CSDL RỖNG mới (mặc định của chuỗi kết nối cũ là ReadWriteCreate).
        private SqliteConnection TaoKetNoi()
        {
            var sb = new SqliteConnectionStringBuilder
            {
                DataSource = _csdl2Path,
                Mode = SqliteOpenMode.ReadWrite,
                Pooling = false     // Dispose = đóng hẳn tệp. Mặc định có pool => tệp csdl2.db vẫn bị khóa, sao lưu/khôi phục báo "đang được sử dụng"
            };
            return new SqliteConnection(sb.ToString());
        }
        // Báo Form4 nạp lại combobox địa điểm. Lỗi bên Form4 không được làm hỏng thông báo của thao tác ĐÃ lưu thành công bên này.
        private async Task LamMoiDiaDiemForm4Async()
        {
            try
            {
                var form4 = LayForm4TrangDauTien();
                if (form4 != null)
                    await form4.LoadDiaDiemAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Form57] Làm mới địa điểm Form4: " + ex.Message);
            }
        }
        private void Form57_CapNhatTinhThanhPho_Load(object? sender, EventArgs e)
        {
            CauHinhLuoiMotLan();
            try
            {
                TaoBangNeuChuaCo();
                LoadDanhSachTinhVaThanhPho();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "Không thể khởi tạo danh sách Tỉnh / Thành phố.\n\n" + $"Chi tiết: {ex.Message}",
                    "Lỗi khởi tạo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void Form57_CapNhatTinhThanhPho_Shown(
            object? sender,
            EventArgs e)
        {
            textBox_TenTinhThanhPho.Focus();
            // Sau khi lưới vẽ xong lần đầu mới nhận lựa chọn của người dùng (tránh lựa chọn mặc định của lưới biến form thành "chế độ sửa")
            BeginInvoke(new Action(() => _daHienLanDau = true));
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_dangBan && e.CloseReason == CloseReason.UserClosing)
                e.Cancel = true;                  // đang ghi CSDL / Excel: không cho đóng giữa chừng (bản cũ chặn bằng this.Enabled = false)
            base.OnFormClosing(e);
        }
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            var tips = new Dictionary<Control, string>
            {
                { kryptonButton1_Them, "Thêm đơn vị mới" },
                { kryptonButton1_Sua, "Chỉnh sửa tên đơn vị đang chọn" },
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
            using var cn = TaoKetNoi();
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
        // Thời gian hiển thị dd-MM-yyyy HH:mm:ss. Chuỗi lưu trong CSDL luôn là yyyy-MM-dd HH:mm:ss nên đọc bằng định dạng cố định
        // (không phụ thuộc cấu hình vùng của máy); không đọc được thì thử cách cũ rồi giữ nguyên chuỗi.
        private static string DinhDangThoiGian(string thoiGian)
        {
            if (string.IsNullOrEmpty(thoiGian)) return string.Empty;
            if (DateTime.TryParseExact(thoiGian, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime t) ||
                DateTime.TryParse(thoiGian, out t))
                return t.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            return thoiGian;
        }
        private void LoadDanhSachTinhVaThanhPho(int? selectID = null)
        {
            if (!File.Exists(_csdl2Path))
                return;
            try
            {
                var dt = new DataTable();
                using (var cn = TaoKetNoi())
                {
                    cn.Open();
                    using var cmd = cn.CreateCommand();
                    cmd.CommandText = """
                    SELECT ID, TenTinhVaThanhPho, ThoiGian
                    FROM TinhVaThanhPho
                    ORDER BY ID ASC;
                    """;
                    using var reader = cmd.ExecuteReader();
                    dt.Load(reader);
                }
                // CỘT STT: kiểu SỐ (kiểu chuỗi làm lưới sắp xếp sai khi bấm tiêu đề cột: 1, 10, 11, 2, 3...)
                if (!dt.Columns.Contains("STT"))
                    dt.Columns.Add("STT", typeof(int));
                dt.BeginLoadData();
                int stt = 1;
                foreach (DataRow row in dt.Rows)
                {
                    // TÊN + THỜI GIAN: lấy từ CSDL, KHÔNG lấy từ textbox
                    row["TenTinhVaThanhPho"] = ChuanHoaTenTinh(row["TenTinhVaThanhPho"]?.ToString() ?? string.Empty);
                    row["ThoiGian"] = DinhDangThoiGian(row["ThoiGian"]?.ToString()?.Trim() ?? string.Empty);
                    row["STT"] = stt++;
                }
                dt.EndLoadData();
                dt.AcceptChanges();
                var dgv = kryptonDataGridView1;
                int cuonCu = (dgv.Rows.Count > 0 && dgv.IsHandleCreated) ? dgv.FirstDisplayedScrollingRowIndex : -1;
                _chanChonLuoi++;                   // nạp lưới sinh ra SelectionChanged "tự phát": không được coi là người dùng vừa chọn dòng
                try
                {
                    var cu = _dtHienTai;
                    dgv.DataSource = dt;           // gán 1 lần (bản cũ gán null rồi gán lại => lưới dựng lại 2 lần, chớp)
                    _dtHienTai = dt;
                    cu?.Dispose();
                    label_tongCongTinhThanhPho.Text =
                        $"Tổng cộng: {dt.Rows.Count:N0} đơn vị hành chính";
                    CauHinhCotLuoi();
                    CuonDenDong(selectID, cuonCu);
                }
                finally
                {
                    _chanChonLuoi--;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "Không thể tải danh sách Tỉnh / Thành phố.\n\n" +
                    $"Chi tiết: {ex.Message}",
                    "Lỗi tải dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        // Thiết lập chung của lưới (làm 1 lần, đặt trong Load để chiều cao tính sau khi form đã tự co giãn theo DPI như bản cũ).
        // Màu sắc / viền / tiêu đề cột do Module_GiaoDienForm57Web đảm nhiệm.
        private void CauHinhLuoiMotLan()
        {
            var dgv = kryptonDataGridView1;
            int fontHeight = dgv.Font.Height;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersHeight = Math.Max(38, (int)(fontHeight * 2.3));
            dgv.RowTemplate.Height = Math.Max(32, (int)(fontHeight * 1.8));
            dgv.DefaultCellStyle.Font = _fontData;
            var headerStyle = dgv.ColumnHeadersDefaultCellStyle;
            headerStyle.Font = _fontHeader;
            headerStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToResizeRows = false;
            dgv.ReadOnly = true;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.RowHeadersVisible = false;
            dgv.EnableHeadersVisualStyles = false;
            dgv.Padding = new Padding(0);
        }
        // Cột sinh lại sau mỗi lần gán DataSource nên phải đặt lại sau mỗi lần nạp
        private void CauHinhCotLuoi()
        {
            var dgv = kryptonDataGridView1;
            if (dgv.Columns.Count == 0)
                return;
            DinhCot(dgv, "STT", "STT", 0, 18, 60, DataGridViewContentAlignment.MiddleCenter);
            DinhCot(dgv, "TenTinhVaThanhPho", "Tên tỉnh / Thành phố", 1, 52, 180, DataGridViewContentAlignment.MiddleLeft);
            DinhCot(dgv, "ThoiGian", "Thời gian", 2, 30, 150, DataGridViewContentAlignment.MiddleCenter);
            if (dgv.Columns.Contains("ID"))
                dgv.Columns["ID"].Visible = false;
        }
        private static void DinhCot(DataGridView dgv, string ten, string tieuDe, int viTri, float tyTrong, int rongToiThieu, DataGridViewContentAlignment canLe)
        {
            if (!dgv.Columns.Contains(ten)) return;
            var col = dgv.Columns[ten];
            col.HeaderText = tieuDe;
            col.DisplayIndex = viTri;
            col.FillWeight = tyTrong;
            col.MinimumWidth = rongToiThieu;
            col.DefaultCellStyle.Alignment = canLe;
        }
        // Cuộn tới dòng vừa thêm / vừa sửa; các trường hợp khác giữ nguyên vị trí cuộn cũ (lần mở đầu: đầu danh sách).
        // KHÔNG chọn dòng bằng code: chọn dòng = vào chế độ sửa, chỉ do người dùng bấm.
        private void CuonDenDong(int? selectID, int vitriCuonCu)
        {
            var dgv = kryptonDataGridView1;
            if (dgv.Rows.Count == 0 || !dgv.IsHandleCreated)
                return;
            int index = vitriCuonCu >= 0 ? Math.Min(vitriCuonCu, dgv.Rows.Count - 1) : 0;
            if (selectID.HasValue)
            {
                for (int i = 0; i < dgv.Rows.Count; i++)
                {
                    if (dgv.Rows[i].DataBoundItem is DataRowView drv &&
                        int.TryParse(drv["ID"]?.ToString(), out int id) &&
                        id == selectID.Value)
                    {
                        index = i;
                        break;
                    }
                }
            }
            try
            {
                dgv.FirstDisplayedScrollingRowIndex = index;
            }
            catch (InvalidOperationException)
            {
                // lưới chưa có chỗ hiển thị dòng (chưa có kích thước): bỏ qua, không ảnh hưởng dữ liệu
            }
        }
        // Chỉ phản ứng khi NGƯỜI DÙNG chọn dòng. Đọc từ SelectedRows (dòng đang tô sáng), không dùng CurrentRow:
        // khi code gọi ClearSelection / chọn dòng, CurrentRow vẫn là dòng cũ nên bản cũ có thể đổ sai dòng lên ô nhập.
        private void kryptonDataGridView1_SelectionChanged(object? sender, EventArgs e)
        {
            if (_chanChonLuoi > 0 || !_daHienLanDau)
                return;
            var dgv = kryptonDataGridView1;
            if (dgv.SelectedRows.Count != 1)
                return;
            if (dgv.SelectedRows[0].DataBoundItem is not DataRowView drv)
                return;
            if (!int.TryParse(drv["ID"]?.ToString(), out int id))
                return;
            string ten = drv["TenTinhVaThanhPho"]?.ToString()?.Trim() ?? string.Empty;
            _selectedID = id;
            _selectedTen = ten;
            textBox_TenTinhThanhPho.Text = ten;
            isEditing = true;
            kryptonButton1_Sua.Text = "Lưu";
            AcceptButton = kryptonButton1_Sua;
        }
        private void ResetInput()
        {
            _selectedID = -1;
            _selectedTen = string.Empty;
            isEditing = false;
            textBox_TenTinhThanhPho.Clear();
            kryptonButton1_Sua.Text = "Sửa";
            AcceptButton = kryptonButton1_Them;
            _chanChonLuoi++;       // ClearSelection tự bắn SelectionChanged: nếu không chặn, bản cũ bị kéo ngược vào "chế độ sửa" ngay sau khi Reset
            try { kryptonDataGridView1.ClearSelection(); }
            finally { _chanChonLuoi--; }
            textBox_TenTinhThanhPho.Focus();
        }
        // Khóa khi đang nhập/xuất Excel: chỉ khóa lưới + khối nhập liệu, KHÔNG khóa cả form
        // (khóa cả form + mở hộp thoại Admin làm form bị bật lại giữa chừng, người dùng bấm được nút khác khi đang ghi CSDL).
        private void DatTrangThaiBan(bool ban)
        {
            _dangBan = ban;
            UseWaitCursor = ban;
            kryptonDataGridView1.Enabled = !ban;
            tableLayoutPanel2.Enabled = !ban;
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
                DatTrangThaiBan(true);
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
                using var cn = TaoKetNoi();
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
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);   // mặc định "No" = chỉ bổ sung, không xóa gì
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
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2);
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
                    await LamMoiDiaDiemForm4Async();
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
                await LamMoiDiaDiemForm4Async();
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
                DatTrangThaiBan(false);
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
                DatTrangThaiBan(true);
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
                using (var cn = TaoKetNoi())
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
                        Module_ExcelAnToan.LuuAnToan(workbook, filePath);
                    });
                }
                catch (IOException ioEx)
                {
                    // Thường gặp khi tệp đích đang được mở bởi Excel hoặc chương trình khác.
                    MessageBox.Show("Không thể ghi tệp Excel. Tệp có thể đang được mở bởi chương trình khác.\n\n" + $"Chi tiết: {ioEx.Message}", "Lỗi ghi tệp", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Xuất Excel danh mục Tỉnh / Thành phố", $"{danhSachDto.Count} dòng | Tệp: {Path.GetFileName(filePath)}");
                Module_ThongBao.ThanhCong($"Xuất Excel thành công! Đã xuất {danhSachDto.Count:N0} Tỉnh / Thành phố.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể xuất dữ liệu ra Excel.\n\n" + $"Chi tiết: {ex.Message}", "Lỗi xuất dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                DatTrangThaiBan(false);
                kryptonButton1_XuatData.Values.Text = textBanDau;
                kryptonButton1_XuatData.Values.Image = anhBanDau;
                kryptonButton1_XuatData.Enabled = true;
                this.Focus();
            }
        }
        // Tìm tên trùng (không phân biệt hoa/thường, hỗ trợ tiếng Việt có dấu). So sánh trong C# vì hàm LOWER() của SQLite chỉ đổi chữ ASCII:
        // bản cũ coi "Hà Nội" và "HÀ NỘI" là 2 tên khác nhau nên vẫn thêm trùng được. Bảng chỉ vài chục dòng nên đọc hết rất nhanh.
        private bool TimTenTrung(SqliteConnection cn, SqliteTransaction tx, string tenMoi, int? boQuaID, out int idTrung, out string thoiGianTrung)
        {
            idTrung = -1;
            thoiGianTrung = string.Empty;
            using var cmd = cn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "SELECT ID, TenTinhVaThanhPho, ThoiGian FROM TinhVaThanhPho;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                if (boQuaID.HasValue && id == boQuaID.Value)
                    continue;
                string ten = reader.IsDBNull(1) ? string.Empty : ChuanHoaTenTinh(reader.GetString(1));
                if (string.Equals(ten, tenMoi, StringComparison.OrdinalIgnoreCase))
                {
                    idTrung = id;
                    thoiGianTrung = reader.IsDBNull(2) ? string.Empty : reader.GetString(2).Trim();
                    return true;
                }
            }
            return false;
        }
        private void kryptonButton1_Them_Click(object? sender, EventArgs e)
        {
            // 1. LẤY + CHUẨN HÓA DỮ LIỆU
            string tenTinhVaThanhPho =
                ChuanHoaTenTinh(textBox_TenTinhThanhPho.Text);
            if (string.IsNullOrWhiteSpace(tenTinhVaThanhPho))
            {
                MessageBox.Show(
                    this,
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
                    this,
                    "Không tìm thấy cơ sở dữ liệu CSDL2.",
                    "Lỗi cơ sở dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            try
            {
                int newID;
                using (var cn = TaoKetNoi())
                {
                    cn.Open();
                    using var tx = cn.BeginTransaction();      // kiểm tra trùng + thêm mới trong CÙNG 1 giao dịch
                    // 3. KIỂM TRA TRÙNG
                    if (TimTenTrung(cn, tx, tenTinhVaThanhPho, null, out int existingID, out string thoiGian))
                    {
                        MessageBox.Show(
                            this,
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
                    // 4. THÊM MỚI
                    string thoiGianMoi =
                        DateTime.Now.ToString(
                            "yyyy-MM-dd HH:mm:ss",
                            CultureInfo.InvariantCulture);
                    using var cmdInsert = cn.CreateCommand();
                    cmdInsert.Transaction = tx;
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
                    cmdInsert.Parameters.Add("@ten", SqliteType.Text).Value = tenTinhVaThanhPho;
                    cmdInsert.Parameters.Add("@thoigian", SqliteType.Text).Value = thoiGianMoi;
                    newID = Convert.ToInt32(cmdInsert.ExecuteScalar());
                    tx.Commit();
                }
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Thêm Tỉnh / Thành phố", $"ID={newID} | Tên: {tenTinhVaThanhPho}");
                // 5. CẬP NHẬT GIAO DIỆN
                LoadDanhSachTinhVaThanhPho(newID);
                ResetInput();
                // 6. CẬP NHẬT COMBOBOX FORM4
                _ = LamMoiDiaDiemForm4Async();
            }
            catch (SqliteException ex)
            {
                MessageBox.Show(
                    this,
                    "Không thể thêm Tỉnh / Thành phố.\n\n" +
                    $"Chi tiết CSDL: {ex.Message}",
                    "Lỗi cơ sở dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
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
                    this,
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
                    this,
                    "Chưa nhập tên Tỉnh / Thành phố!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                textBox_TenTinhThanhPho.Focus();
                return;
            }
            // Tên không đổi: không ghi CSDL, không đổi "Thời gian" của dòng
            if (string.Equals(tenTinhVaThanhPhoMoi, _selectedTen, StringComparison.Ordinal))
            {
                ResetInput();
                return;
            }
            // 3. KIỂM TRA CSDL
            if (!File.Exists(_csdl2Path))
            {
                MessageBox.Show(
                    this,
                    "Không tìm thấy cơ sở dữ liệu CSDL2.",
                    "Lỗi cơ sở dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            int idDangSua = _selectedID;
            try
            {
                bool khongTimThay;
                using (var cn = TaoKetNoi())
                {
                    cn.Open();
                    using var tx = cn.BeginTransaction();
                    // 4. KIỂM TRA TRÙNG (bỏ qua chính dòng đang sửa)
                    if (TimTenTrung(cn, tx, tenTinhVaThanhPhoMoi, idDangSua, out int existingID, out _))
                    {
                        MessageBox.Show(
                            this,
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
                    // 5. CẬP NHẬT
                    string thoiGianSua =
                        DateTime.Now.ToString(
                            "yyyy-MM-dd HH:mm:ss",
                            CultureInfo.InvariantCulture);
                    using var cmdUpdate = cn.CreateCommand();
                    cmdUpdate.Transaction = tx;
                    cmdUpdate.CommandText = """
                    UPDATE TinhVaThanhPho
                    SET
                        TenTinhVaThanhPho = @ten,
                        ThoiGian = @thoigian
                    WHERE ID = @id;
                    """;
                    cmdUpdate.Parameters.Add("@ten", SqliteType.Text).Value = tenTinhVaThanhPhoMoi;
                    cmdUpdate.Parameters.Add("@thoigian", SqliteType.Text).Value = thoiGianSua;
                    cmdUpdate.Parameters.Add("@id", SqliteType.Integer).Value = idDangSua;
                    khongTimThay = cmdUpdate.ExecuteNonQuery() == 0;
                    if (!khongTimThay)
                        tx.Commit();
                }
                // 6. KIỂM TRA KẾT QUẢ UPDATE
                if (khongTimThay)
                {
                    MessageBox.Show(
                        this,
                        "Không tìm thấy Tỉnh / Thành phố cần cập nhật.\n\n" +
                        "Có thể dữ liệu đã bị thay đổi hoặc xóa.",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    LoadDanhSachTinhVaThanhPho();
                    ResetInput();
                    return;
                }
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Sửa Tỉnh / Thành phố", $"ID={idDangSua} | Tên mới: {tenTinhVaThanhPhoMoi}");
                // 7. LOAD LẠI + CUỘN TỚI DÒNG VỪA SỬA
                LoadDanhSachTinhVaThanhPho(idDangSua);
                ResetInput();
                // 8. CẬP NHẬT COMBOBOX FORM4
                _ = LamMoiDiaDiemForm4Async();
            }
            catch (SqliteException ex)
            {
                MessageBox.Show(
                    this,
                    "Không thể cập nhật Tỉnh / Thành phố.\n\n" +
                    $"Chi tiết CSDL: {ex.Message}",
                    "Lỗi cơ sở dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
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
                    this,
                    "Bạn chưa chọn Tỉnh / Thành phố để xóa!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            // Tên + ID lấy từ DÒNG ĐANG CHỌN (không lấy từ ô nhập: bản cũ hỏi "xóa 'A'" theo chữ vừa gõ trong ô nhưng lại xóa dòng 'B' đang chọn)
            int idCanXoa = _selectedID;
            string tenTinhVaThanhPho = _selectedTen;
            if (string.IsNullOrWhiteSpace(tenTinhVaThanhPho))
            {
                MessageBox.Show(
                    this,
                    "Không xác định được tên Tỉnh / Thành phố cần xóa.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            // 2. XÁC NHẬN XÓA (nút mặc định là "No" để lỡ nhấn Enter không bị xóa)
            DialogResult xacNhan =
                MessageBox.Show(
                    this,
                    $"Bạn có chắc chắn muốn xóa Tỉnh / Thành phố:\n" +
                    $"\"{tenTinhVaThanhPho}\"\n" +
                    $"ID: {idCanXoa}\n" +
                    "Dữ liệu sẽ bị xóa khỏi danh mục Tỉnh / Thành phố.",
                    "Xác nhận xóa",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);
            if (xacNhan != DialogResult.Yes)
                return;
            // 3. KIỂM TRA CSDL
            if (!File.Exists(_csdl2Path))
            {
                MessageBox.Show(
                    this,
                    "Không tìm thấy cơ sở dữ liệu CSDL2.",
                    "Lỗi cơ sở dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            try
            {
                int soDongDaXoa;
                using (var cn = TaoKetNoi())
                {
                    cn.Open();
                    // 4. XÓA
                    using var cmd = cn.CreateCommand();
                    cmd.CommandText = """
                    DELETE FROM TinhVaThanhPho
                    WHERE ID = @id;
                    """;
                    cmd.Parameters.Add("@id", SqliteType.Integer).Value = idCanXoa;
                    soDongDaXoa = cmd.ExecuteNonQuery();
                }
                if (soDongDaXoa == 0)
                {
                    MessageBox.Show(
                        this,
                        "Không tìm thấy dữ liệu cần xóa.\n\n" +
                        "Có thể dữ liệu đã bị xóa trước đó.",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    LoadDanhSachTinhVaThanhPho();
                    ResetInput();
                    return;
                }
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Xóa Tỉnh / Thành phố", $"ID={idCanXoa} | Tên: {tenTinhVaThanhPho}");
                // 5. LOAD LẠI + RESET
                LoadDanhSachTinhVaThanhPho();
                ResetInput();
                // 6. CẬP NHẬT COMBOBOX TRÊN FORM4
                _ = LamMoiDiaDiemForm4Async();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "Không thể xóa Tỉnh / Thành phố.\n\n" +
                    $"Chi tiết: {ex.Message}",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        // Chuẩn hóa tên: Unicode dựng sẵn (NFC) + gộp mọi loại khoảng trắng (kể cả khoảng trắng không ngắt U+00A0 hay gặp khi chép từ Excel/web).
        // Nhờ vậy "Hà Nội" gõ tay và "Hà Nội" từ Excel (dạng tổ hợp) được coi là MỘT tên, kiểm tra trùng mới chính xác.
        private static string ChuanHoaTenTinh(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            string s = value;
            try { s = value.Normalize(NormalizationForm.FormC); }
            catch (ArgumentException) { /* chuỗi Unicode lỗi: giữ nguyên */ }
            return string.Join(
                ' ',
                s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }
    }
    public sealed class TinhVaThanhPhoDto
    {
        public int STT { get; set; }
        public string TenTinhVaThanhPho { get; set; } = string.Empty;
        public string ThoiGian { get; set; } = string.Empty;
    }
}
