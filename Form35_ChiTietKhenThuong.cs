using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;

namespace PhanMemThiDua2026
{
    public partial class Form35_ChiTietKhenThuong : Form
    {
        private readonly string _csdl4Path = Module_DanduongGPS.DuongDanCSDL4;
        // Biến cục bộ để lưu trữ con trỏ Form 34 và vị trí dòng, phục vụ truy xuất O(1)
        private Form34_ThongKeKhenThuong _form34Nguon;
        private int _currentRowIndexForm34 = -1;
        private string _currentSoHieu = "";
        private string _currentHoTen = "";
        private string _currentDonVi = "";
        private string _currentTinhTrang = "";
        private int _currentEditingID = -1;
        // Quản lý Font tập trung dùng biến hệ thống, chống rò rỉ GDI handle
        private static readonly Font _fontGridCell10 = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Regular);
        private static readonly Font _fontGridHeader10Bold = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Bold);
        private static readonly Font _fontRowHeader12Bold = new Font(Module_HeThong.TenFontHeThong, 12F, FontStyle.Bold);
        public Form35_ChiTietKhenThuong()
        {
            InitializeComponent();
            kryptonDataGridView1_DanhSachCBCS.CellClick += KryptonDataGridView1_DanhSachCBCS_CellClick;
            kryptonDataGridView1_DanhSachCBCS.RowPostPaint += KryptonDataGridView1_DanhSachCBCS_RowPostPaint;

            // GẮN SỰ KIỆN ĐỊNH DẠNG TIỀN THƯỞNG
            if (kryptonTextBox_TienThuong != null)
            {
                kryptonTextBox_TienThuong.TextChanged += kryptonTextBox_TienThuong_TextChanged;
            }
        }
        private void Form35_ChiTietKhenThuong_Load(object sender, EventArgs e)
        {
            // Cấu hình thanh trạng thái trước
            CauHinhStatusStrip();
            InitToolTips();
        }
        private void InitToolTips()
        {  
            // KHỞI TẠO TOOLTIP - ỔN ĐỊNH CHO HỆ THỐNG NỘI BỘ
            // 1. Kiểm tra ToolTip
            if (toolTip1 == null)
                return;
            try
            {
                // 2. Cấu hình chung
                toolTip1.IsBalloon = true;
                toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
                toolTip1.ToolTipIcon = ToolTipIcon.Info;

                // Thời gian chờ trước khi hiển thị
                toolTip1.InitialDelay = 300;

                // Thời gian Tooltip hiển thị
                toolTip1.AutoPopDelay = 2500;

                // Thời gian chờ khi chuyển sang Control khác
                toolTip1.ReshowDelay = 100;

                // Cho phép hiển thị ngay cả khi Form chưa active
                toolTip1.ShowAlways = true;
                // ==== BỔ SUNG CÁC NÚT MỚI TẠI ĐÂY ====
                GanToolTipAnToan(
                    kryptonButton_Them,
                    "Thêm thông tin khen thưởng mới vào hệ thống");

                GanToolTipAnToan(
                    kryptonButton_Sua,
                    "Cập nhật, chỉnh sửa thông tin đang được chọn");

                GanToolTipAnToan(
                    kryptonButton_Xoa,
                    "Xóa vĩnh viễn dữ liệu khen thưởng đang chọn");

                GanToolTipAnToan(
                    kryptonButton3_DongForm,
                    "Đóng cửa sổ này và quay lại màn hình chính");
            }
            catch (ObjectDisposedException)
            {
                // ToolTip hoặc Control đã được giải phóng trong lúc thao tác.
                // Không để chức năng Tooltip ảnh hưởng đến hoạt động chính.
            }
            catch (InvalidOperationException)
            {
                // Trạng thái WinForms không phù hợp để cấu hình Tooltip.
                // Không để chức năng Tooltip làm Form dừng hoạt động.
            }
        }        
        // CẤU HÌNH GIAO DIỆN THANH TRẠNG THÁI (CHUẨN KỸ SƯ UI/UX)
       
        private void CauHinhStatusStrip()
        {
            // --------------------------------------------------------
            // 1. Label Số lượng - Nằm sát mép trái (Có hiển thị Icon)
            // --------------------------------------------------------
            if (toolStripStatusLabel1 != null && !toolStripStatusLabel1.IsDisposed)
            {
                // Neo vị trí về mép trái
                toolStripStatusLabel1.Alignment = ToolStripItemAlignment.Left;

                // Căn lề cho cả Chữ và Hình ảnh đều bắt đầu từ bên trái
                toolStripStatusLabel1.TextAlign = ContentAlignment.MiddleLeft;
                toolStripStatusLabel1.ImageAlign = ContentAlignment.MiddleLeft;

                // Đảm bảo Hình ảnh (Icon) luôn nằm trước rồi mới đến Chữ
                toolStripStatusLabel1.TextImageRelation = TextImageRelation.ImageBeforeText;

                // Bật Spring (Lò xo) đẩy nhãn này giãn dài ra chiếm toàn bộ không gian ở giữa, 
                // tự động "ép" nhãn số 2 dạt hoàn toàn về góc phải.
                toolStripStatusLabel1.Spring = true;

                // Khoảng đệm (Padding) để Icon không bị dính sát vào mép viền cửa sổ
                toolStripStatusLabel1.Padding = new Padding(5, 0, 0, 0);
            }

            // --------------------------------------------------------
            // 2. Label Tình trạng công tác - Luôn neo cứng ở mép phải
            // --------------------------------------------------------
            if (toolStripStatusLabel2_TinhTrangCongTac != null && !toolStripStatusLabel2_TinhTrangCongTac.IsDisposed)
            {
                // Neo vị trí về mép phải
                toolStripStatusLabel2_TinhTrangCongTac.Alignment = ToolStripItemAlignment.Right;

                // Nội dung chữ bên trong cũng căn phải để thẳng nếp
                toolStripStatusLabel2_TinhTrangCongTac.TextAlign = ContentAlignment.MiddleRight;

                // Tắt lò xo ở nhãn phải để nó chỉ chiếm đúng diện tích chữ của nó
                toolStripStatusLabel2_TinhTrangCongTac.Spring = false;

                // Tạo khoảng thở 10 pixel bên tay phải, giúp chữ không bị lẹm vào viền hay Scrollbar
                toolStripStatusLabel2_TinhTrangCongTac.Padding = new Padding(0, 0, 10, 0);
            }
        }
        private void GanToolTipAnToan(Control control, string noiDung)
        {
            // 1. Control không tồn tại
            if (control == null)
                return;

            // 2. Control đã được giải phóng hoặc đang giải phóng
            if (control.IsDisposed || control.Disposing)
                return;

            // 3. Nội dung Tooltip không hợp lệ
            if (string.IsNullOrWhiteSpace(noiDung))
                return;

            // 4. ToolTip chưa được khởi tạo
            if (toolTip1 == null)
                return;

            try
            {
                // 5. Gán Tooltip
                toolTip1.SetToolTip(control, noiDung);
            }
            catch (ObjectDisposedException)
            {
                // Control đã bị giải phóng đúng thời điểm thao tác.
            }
            catch (InvalidOperationException)
            {
                // Control đang ở trạng thái không phù hợp.
            }
        }
        // HÀM ĐỊNH DẠNG TIỀN THƯỞNG (TỐI ƯU KHÔNG REGEX)
        private void kryptonTextBox_TienThuong_TextChanged(object sender, EventArgs e)
        {
            
            // 1. KIỂM TRA AN TOÀN
            

            if (kryptonTextBox_TienThuong == null ||
                kryptonTextBox_TienThuong.IsDisposed ||
                kryptonTextBox_TienThuong.Disposing)
            {
                return;
            }

            // Người dùng chưa nhập gì -> giữ ô trống.
            // Không tự động hiển thị "0".
            if (string.IsNullOrWhiteSpace(kryptonTextBox_TienThuong.Text))
                return;

            kryptonTextBox_TienThuong.TextChanged -=
                kryptonTextBox_TienThuong_TextChanged;

            try
            {
                string textHienTai = kryptonTextBox_TienThuong.Text.Trim();

                // ====
                // 2. LƯU VỊ TRÍ CON TRỎ
                // ====

                int cursorFromEnd =
                    textHienTai.Length -
                    kryptonTextBox_TienThuong.SelectionStart;

                // ====
                // 3. LOẠI BỎ DẤU PHÂN CÁCH
                // ====

                string rawText = textHienTai
                    .Replace(".", string.Empty)
                    .Replace(",", string.Empty)
                    .Trim();

                // ====
                // 4. CHỈ GIỮ LẠI KÝ TỰ SỐ
                // ====

                string cleanText = new string(
                    rawText.Where(char.IsDigit).ToArray());

                // Nếu sau khi lọc không còn số:
                // giữ TextBox trống thay vì ép thành "0".
                if (string.IsNullOrEmpty(cleanText))
                {
                    kryptonTextBox_TienThuong.Clear();
                    return;
                }

                // ====
                // 5. GIỚI HẠN GIÁ TRỊ LONG
                // ====

                if (!long.TryParse(cleanText, out long tienThuong))
                {
                    // Không thể chuyển đổi -> giữ nguyên nội dung hợp lệ
                    // gần nhất thay vì tự động đưa về "0".
                    return;
                }

                // ====
                // 6. ĐẢM BẢO GIÁ TRỊ KHÔNG ÂM
                // ====

                if (tienThuong < 0)
                    tienThuong = 0;

                // ====
                // 7. ĐỊNH DẠNG TIỀN
                // ====

                string textDaDinhDang =
                    string.Format("{0:#,##0}", tienThuong)
                        .Replace(",", ".");

                kryptonTextBox_TienThuong.Text = textDaDinhDang;

                // ====
                // 8. KHÔI PHỤC VỊ TRÍ CON TRỎ
                // ====

                int newCursorPosition =
                    kryptonTextBox_TienThuong.Text.Length - cursorFromEnd;

                kryptonTextBox_TienThuong.SelectionStart =
                    Math.Max(
                        0,
                        Math.Min(
                            newCursorPosition,
                            kryptonTextBox_TienThuong.Text.Length));
            }
            finally
            {
                // Luôn đăng ký lại sự kiện kể cả khi có lỗi.
                kryptonTextBox_TienThuong.TextChanged +=
                    kryptonTextBox_TienThuong_TextChanged;
            }
        }
        // HÀM BẢO VỆ GIAO DIỆN (LỘT VỎ MÃ HÓA AN TOÀN)
        private string SafeDecrypt(object value)
        {
            if (value == null || value == DBNull.Value) return "";
            string s = value.ToString()?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(s)) return "";

            string decrypted = BaoMatAES.GiaiMa(s);
            // Kỹ thuật Fallback: Nếu giải mã rỗng (có thể do lỗi hoặc là text thường), trả về chuỗi ban đầu
            return string.IsNullOrEmpty(decrypted) ? s : decrypted;
        }
        // ⭐ CHUẨN KỸ SƯ: Bổ sung async void để có thể chờ (await) hàm tải dữ liệu
        // ⭐ CHUẨN KỸ SƯ: Bổ sung formNguon và rowIndex để truy xuất ngược
        public async void NhanDuLieuTuForm34(string hoTen, string soHieu, string donVi, string tinhTrangCu, Form34_ThongKeKhenThuong formNguon, int rowIndex)
        {
            // 1. Lưu thông tin cơ bản
            _currentHoTen = hoTen;
            _currentSoHieu = soHieu;
            _currentDonVi = donVi;
            _currentTinhTrang = tinhTrangCu; // Tạm gán tình trạng cũ
            // 2. Lưu tham chiếu bộ nhớ để chọc ngược về Form 34
            _form34Nguon = formNguon;
            _currentRowIndexForm34 = rowIndex;
            // 3. Đổ dữ liệu nhân thân lên Nhãn
            if (label1_HoVaTen != null) label1_HoVaTen.Text = Module_HeThong.Tu_Dong_Chi + ": " + hoTen;
            if (label1_SoHieu != null) label1_SoHieu.Text = "Số hiệu: " + soHieu;
            if (label1_DonVi != null) label1_DonVi.Text = "Đơn vị: " + donVi;           
            // 4. 🚀 TÍNH NĂNG ĐỘC QUYỀN: LẤY TÌNH TRẠNG CHÍNH XÁC 100% TỪ RAM FORM 34        
            string tinhTrangChinhXac = tinhTrangCu; // Fallback an toàn
            if (_form34Nguon != null && !_form34Nguon.IsDisposed && _currentRowIndexForm34 >= 0)
            {
                // Truy xuất O(1) ngay lập tức
                tinhTrangChinhXac = _form34Nguon.LayTinhTrangCongTacAnToan(_currentRowIndexForm34);
                _currentTinhTrang = tinhTrangChinhXac; // Cập nhật lại biến môi trường bên trong Form 35
            }
           // 5. Đổ lên thanh Status (có trang trí màu sắc - UX)
            if (toolStripStatusLabel2_TinhTrangCongTac != null)
            {
                toolStripStatusLabel2_TinhTrangCongTac.Text = $"Tình trạng: {tinhTrangChinhXac}";

                // Hiệu ứng màu sắc trực quan
                if (string.Equals(tinhTrangChinhXac, Module_HeThong.TT_DANG_CONG_TAC, StringComparison.OrdinalIgnoreCase))
                {
                    toolStripStatusLabel2_TinhTrangCongTac.ForeColor = Color.SeaGreen; // Xanh an tâm
                }
                else
                {
                    toolStripStatusLabel2_TinhTrangCongTac.ForeColor = Color.IndianRed; // Đỏ cảnh báo
                }

                toolStripStatusLabel2_TinhTrangCongTac.Visible = true;
            }
            // 6. Hoàn thiện quá trình load dữ liệu chi tiết
            XoaTrangGiaoDien();
            await ReloadDuLieuGiayKhen_CuaMotNguoiAsync();
        }
        private async Task ReloadDuLieuGiayKhen_CuaMotNguoiAsync()
        {
            try
            {
                DataTable dt = new DataTable();

                // Đẩy toàn bộ quá trình đọc DB và giải mã xuống luồng nền (Background Thread)
                await Task.Run(() =>
                {
                    using var conn = new SqliteConnection($"Data Source={_csdl4Path}");
                    conn.Open();

                    string sql = "SELECT * FROM ThongKe_GiayKhen ORDER BY ID ASC";
                    using var cmd = new SqliteCommand(sql, conn);
                    using var reader = cmd.ExecuteReader();

                    // Kiến tạo cột
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string colName = reader.GetName(i);
                        if (colName == "ID") dt.Columns.Add(colName, typeof(int));
                        else if (colName == "TienThuong") dt.Columns.Add(colName, typeof(long));
                        else dt.Columns.Add(colName, typeof(string));
                    }

                    while (reader.Read())
                    {
                        string soHieuGiaiMa = SafeDecrypt(reader["SoHieu"]);

                        if (string.Equals(soHieuGiaiMa, _currentSoHieu, StringComparison.OrdinalIgnoreCase))
                        {
                            DataRow row = dt.NewRow();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string colName = reader.GetName(i);
                                if (colName == "ID")
                                {
                                    row[i] = Convert.ToInt32(reader[i]);
                                }
                                else if (colName == "TienThuong")
                                {
                                    string tienStr = SafeDecrypt(reader[i]);
                                    long.TryParse(tienStr.Replace(".", "").Replace(",", ""), out long tien);
                                    row[i] = tien;
                                }
                                else
                                {
                                    row[i] = SafeDecrypt(reader[i]);
                                }
                            }
                            dt.Rows.Add(row);
                        }
                    }
                });

                // Cập nhật giao diện sau khi luồng nền đã chạy xong (An toàn 100%)
                kryptonDataGridView1_DanhSachCBCS.AutoGenerateColumns = true;
                kryptonDataGridView1_DanhSachCBCS.DataSource = dt.DefaultView;

                DinhDangLuoi();
                CapNhatSoLuongKhenThuong();
                _currentEditingID = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải chi tiết: " + ex.Message, "Hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // ⭐ HÀM TỐI ƯU SIÊU TỐC: Hợp nhất logic đếm và đồng bộ vào 1 hàm duy nhất
        private async Task DongBoSoLuongVeBangTongAsync(string soHieuTarget)
        {
            if (string.IsNullOrWhiteSpace(soHieuTarget)) return;

            try
            {
                await Task.Run(() =>
                {
                    using var conn = new SqliteConnection($"Data Source={_csdl4Path}");
                    conn.Open();

                    // 1. Quét bảng Giấy Khen để đếm số lượng THỰC TẾ
                    int soLuongThucTe = 0;
                    using (var cmdCount = new SqliteCommand("SELECT SoHieu FROM ThongKe_GiayKhen", conn))
                    using (var reader = cmdCount.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string shGiaiMa = SafeDecrypt(reader["SoHieu"]);
                            if (string.Equals(shGiaiMa, soHieuTarget, StringComparison.OrdinalIgnoreCase))
                            {
                                soLuongThucTe++;
                            }
                        }
                    }

                    // 2. Tìm ID bên bảng Tổng để cập nhật
                    long targetRowId = -1;
                    using (var cmdSelect = new SqliteCommand("SELECT rowid, SoHieu FROM ThongKeCBCS_DuocKhenThuong", conn))
                    using (var reader = cmdSelect.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string shGiaiMa2 = SafeDecrypt(reader["SoHieu"]);
                            if (string.Equals(shGiaiMa2, soHieuTarget, StringComparison.OrdinalIgnoreCase))
                            {
                                targetRowId = reader.GetInt64(0);
                                break;
                            }
                        }
                    }

                    // 3. Thực thi cập nhật hoặc chèn mới
                    if (targetRowId != -1)
                    {
                        string sqlUpdate = "UPDATE ThongKeCBCS_DuocKhenThuong SET SoLuong_Khen = @SL WHERE rowid = @RowId";
                        using var cmdUpdate = new SqliteCommand(sqlUpdate, conn);
                        cmdUpdate.Parameters.AddWithValue("@SL", soLuongThucTe.ToString());
                        cmdUpdate.Parameters.AddWithValue("@RowId", targetRowId);
                        cmdUpdate.ExecuteNonQuery();
                    }
                    else if (soLuongThucTe > 0)
                    {
                        string sqlIn = "INSERT INTO ThongKeCBCS_DuocKhenThuong (HoVaTen, SoHieu, DonVi, TinhTrang, SoLuong_Khen) VALUES (@HT, @SH, @DV, @TT, @SL)";
                        using var cmdIn = new SqliteCommand(sqlIn, conn);
                        cmdIn.Parameters.AddWithValue("@HT", BaoMatAES.MaHoa(_currentHoTen));
                        cmdIn.Parameters.AddWithValue("@SH", BaoMatAES.MaHoa(_currentSoHieu));
                        cmdIn.Parameters.AddWithValue("@DV", BaoMatAES.MaHoa(_currentDonVi));
                        cmdIn.Parameters.AddWithValue("@TT", _currentTinhTrang); // Tình trạng Text thường
                        cmdIn.Parameters.AddWithValue("@SL", soLuongThucTe.ToString());
                        cmdIn.ExecuteNonQuery();
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi Đồng bộ Khen thưởng nội bộ]: {ex.Message}");
            }
        }
        private void DinhDangLuoi()
        {
            var grid = kryptonDataGridView1_DanhSachCBCS;
            if (grid == null || grid.Columns.Count == 0) return;

            grid.SuspendLayout();
            try
            {
                // 1. ẨN CÁC CỘT KHÔNG CẦN THIẾT
                string[] cotCanAn = { "ID", "HoVaTen", "SoHieu", "DonVi", "TinhTrang" };
                foreach (var cot in cotCanAn)
                {
                    if (grid.Columns.Contains(cot)) grid.Columns[cot].Visible = false;
                }

                // 2. CẤU HÌNH CƠ BẢN
                grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
                grid.DefaultCellStyle.Font = _fontGridCell10;
                grid.DefaultCellStyle.Padding = new Padding(4, 6, 4, 6);
                grid.ReadOnly = true;
                grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                grid.AllowUserToAddRows = false;
                grid.RowHeadersVisible = true;
                grid.RowHeadersWidth = 55;
                grid.EnableHeadersVisualStyles = false;

                // ⭐ KHÓA DI CHUYỂN VÀ THAY ĐỔI KÍCH THƯỚC CỘT
                grid.AllowUserToOrderColumns = false;   // Không cho kéo thả đổi vị trí cột
                grid.AllowUserToResizeColumns = false;  // Không cho kéo chuột thay đổi độ rộng cột
                grid.AllowUserToResizeRows = false;     // Khóa luôn thay đổi chiều cao dòng bằng chuột

                // ⭐ THAY ĐỔI QUAN TRỌNG NHẤT Ở ĐÂY:
                // Ép toàn bộ lưới tự động co giãn vừa khít 100% chiều ngang màn hình
                grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                // Chỉ giữ lại thanh cuộn dọc (bỏ qua thanh cuộn ngang vì đã vừa khít)
                grid.ScrollBars = ScrollBars.Vertical;

                // 3. TÙY CHỈNH CHIỀU CAO ĐỘNG (Dynamic Scaling)
                int fontHeight = grid.Font.Height;
                grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                grid.ColumnHeadersHeight = (int)(fontHeight * 2.5);
                grid.RowTemplate.Height = (int)(fontHeight * 1.8);

                // 4. CĂN GIỮA VÀ IN ĐẬM TIÊU ĐỀ
                var style = grid.ColumnHeadersDefaultCellStyle;
                style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                style.Font = _fontGridHeader10Bold;
                style.WrapMode = DataGridViewTriState.True;

                // 5. CẤU HÌNH CHI TIẾT TỪNG CỘT (Dùng "TyLe" thay vì chiều rộng cứng)
                var cauHinhCot = new[]
                {
            (Ten: "HinhThuc_Khen", TieuDe: "Hình thức khen", TyLe: 12f, CanLe: DataGridViewContentAlignment.MiddleLeft),
            (Ten: "QuyetDinh_Khen", TieuDe: "Quyết định số", TyLe: 10f, CanLe: DataGridViewContentAlignment.MiddleCenter),
            (Ten: "NgayCapQD_Khen", TieuDe: "Ngày cấp", TyLe: 9f, CanLe: DataGridViewContentAlignment.MiddleCenter),
            (Ten: "DonVi_Khen", TieuDe: "Đơn vị tặng", TyLe: 15f, CanLe: DataGridViewContentAlignment.MiddleLeft),
            (Ten: "VeViec_Khen", TieuDe: "Về việc", TyLe: 20f, CanLe: DataGridViewContentAlignment.MiddleLeft),
            (Ten: "TienThuong", TieuDe: "Tiền thưởng", TyLe: 10f, CanLe: DataGridViewContentAlignment.MiddleRight),
            (Ten: "NgayCapPhat", TieuDe: "Ngày cấp phát", TyLe: 9f, CanLe: DataGridViewContentAlignment.MiddleCenter),
            (Ten: "CanBoCapPhat", TieuDe: "Cán bộ cấp", TyLe: 11f, CanLe: DataGridViewContentAlignment.MiddleLeft),
            (Ten: "GhiChu_Khen", TieuDe: "Ghi chú", TyLe: 14f, CanLe: DataGridViewContentAlignment.MiddleLeft)
        };

                foreach (var cot in cauHinhCot)
                {
                    if (grid.Columns.Contains(cot.Ten))
                    {
                        var col = grid.Columns[cot.Ten];
                        col.HeaderText = cot.TieuDe;
                        col.DefaultCellStyle.Alignment = cot.CanLe;
                        col.FillWeight = cot.TyLe;

                        // Format hiển thị cho cột tiền thưởng
                        if (cot.Ten == "TienThuong")
                        {
                            col.DefaultCellStyle.Format = "N0";
                        }
                    }
                }

                // ⭐ 6. BỘ GIAO DIỆN HIỆN ĐẠI (OCEAN BLUE THEME)
                grid.StateCommon.Background.Color1 = Color.White;
                grid.StateCommon.DataCell.Back.Color1 = Color.White;

                grid.StateCommon.DataCell.Border.Color1 = Color.FromArgb(235, 235, 235);
                grid.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
                grid.StateCommon.DataCell.Border.Width = 1;

                grid.StateCommon.HeaderColumn.Back.Color1 = Color.FromArgb(180, 210, 240);
                grid.StateCommon.HeaderColumn.Back.Color2 = Color.FromArgb(180, 210, 240);
                grid.StateCommon.HeaderColumn.Content.Color1 = Color.FromArgb(30, 30, 30);
                grid.StateCommon.HeaderColumn.Border.Color1 = Color.FromArgb(150, 180, 210);

                grid.StateSelected.DataCell.Back.Color1 = Color.FromArgb(232, 244, 253);
                grid.StateSelected.DataCell.Back.Color2 = Color.FromArgb(232, 244, 253);
                grid.StateSelected.DataCell.Content.Color1 = Color.FromArgb(0, 102, 204);

                // Cập nhật lại chiều cao cho các dòng ĐÃ CÓ SẴN (nếu có)
                foreach (DataGridViewRow row in grid.Rows)
                {
                    row.Height = grid.RowTemplate.Height;
                }
            }
            finally
            {
                grid.ResumeLayout();
            }
        }
        private void XoaTrangGiaoDien()
        {
            try
            {
                // 1. Xóa dấu vết ID đang thao tác
                _currentEditingID = -1;

                // 2. Xóa các ComboBox (Chỉ gán index = -1 là đủ)
                if (combobox_HinhThucKhen != null) combobox_HinhThucKhen.SelectedIndex = -1;
                if (comboBox_DonViKhenThuong != null) comboBox_DonViKhenThuong.SelectedIndex = -1;

                // 3. Xóa TextBox bằng toán tử hiện đại (Null-conditional operator `?.`)
                kryptonTextBox_QuyetDinh?.Clear();
                kryptonTextBox_NgayQuyDinh?.Clear();
                richTextBox1_VeViec?.Clear();
                richTextBox1_GhiChu?.Clear();

                // XÓA 03 TRƯỜNG MỚI THÊM
                if (kryptonTextBox_TienThuong != null)
                {
                    kryptonTextBox_TienThuong.TextChanged -= kryptonTextBox_TienThuong_TextChanged;
                    kryptonTextBox_TienThuong.Text = "0";
                    kryptonTextBox_TienThuong.TextChanged += kryptonTextBox_TienThuong_TextChanged;
                }
                kryptonTextBox_NgayCapPhat?.Clear();
                kryptonTextBox_CanBoCapPhat?.Clear();

                // 4. Ngắt kết nối dữ liệu lưới an toàn
                if (kryptonDataGridView1_DanhSachCBCS != null)
                {
                    kryptonDataGridView1_DanhSachCBCS.DataSource = null;
                    kryptonDataGridView1_DanhSachCBCS.Columns.Clear();
                }

                // 5. Reset nhãn đếm
                if (toolStripStatusLabel1 != null)
                {
                    toolStripStatusLabel1.Text = "Số lượng khen thưởng: 0";
                    toolStripStatusLabel1.Visible = false; // Ẩn đi khi xóa trắng giao diện
                }

                // 6. TRẢI NGHIỆM NGƯỜI DÙNG (UX): Hoàn trả lại tên nút về trạng thái "Sửa"
                if (kryptonButton_Sua != null)
                {
                    kryptonButton_Sua.Values.Text = "Sửa"; // Chuẩn của KryptonButton
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi XoaTrangGiaoDien]: {ex.Message}");
            }
        }
        public void CapNhatSoLuongKhenThuong()
        {
            // 1. Kiểm tra ToolStripStatusLabel
            if (toolStripStatusLabel1 == null ||
                toolStripStatusLabel1.IsDisposed)
            {
                return;
            }

            // 2. Kiểm tra DataGridView
            if (kryptonDataGridView1_DanhSachCBCS == null ||
                kryptonDataGridView1_DanhSachCBCS.IsDisposed)
            {
                toolStripStatusLabel1.Text = "Số lượng khen thưởng: 0 mục";
                toolStripStatusLabel1.Visible = false;
                return;
            }

            // 3. Lấy số lượng dữ liệu
            int soLuong = kryptonDataGridView1_DanhSachCBCS.Rows.Count;

            // 4. Cập nhật trạng thái
            toolStripStatusLabel1.Text =
                $"Số lượng khen thưởng: {soLuong:N0} mục";

            // 5. Chỉ hiển thị khi có dữ liệu
            toolStripStatusLabel1.Visible = soLuong > 0;
        }
        private void KryptonDataGridView1_DanhSachCBCS_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            var grid = sender as DataGridView;
            if (grid == null) return;
            string rowIdx = (e.RowIndex + 1).ToString();
            var centerFormat = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);
            e.Graphics.DrawString(rowIdx, _fontRowHeader12Bold, SystemBrushes.ControlText, headerBounds, centerFormat);
        }
        private void KryptonDataGridView1_DanhSachCBCS_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // 1. GUARD CLAUSE: Chặn click vào tiêu đề cột (RowIndex = -1) hoặc vùng viền lỗi
            if (e.RowIndex < 0 || sender is not DataGridView grid) return;

            try
            {
                var row = grid.Rows[e.RowIndex];

                // 2. ÉP KIỂU AN TOÀN: Tránh crash nếu cột ID bị NULL hoặc chứa rác
                if (int.TryParse(row.Cells["ID"].Value?.ToString(), out int parsedId))
                {
                    _currentEditingID = parsedId;
                }
                else
                {
                    _currentEditingID = -1; // Fallback an toàn, chặn lưu đè bậy bạ
                }

                // 3. ĐỔ DỮ LIỆU AN TOÀN: Dùng toán tử `?? ""` để bắt chết các giá trị DBNull
                if (combobox_HinhThucKhen != null) combobox_HinhThucKhen.Text = row.Cells["HinhThuc_Khen"].Value?.ToString() ?? "";
                if (kryptonTextBox_QuyetDinh != null) kryptonTextBox_QuyetDinh.Text = row.Cells["QuyetDinh_Khen"].Value?.ToString() ?? "";
                if (kryptonTextBox_NgayQuyDinh != null) kryptonTextBox_NgayQuyDinh.Text = row.Cells["NgayCapQD_Khen"].Value?.ToString() ?? "";
                if (comboBox_DonViKhenThuong != null) comboBox_DonViKhenThuong.Text = row.Cells["DonVi_Khen"].Value?.ToString() ?? "";
                if (richTextBox1_VeViec != null) richTextBox1_VeViec.Text = row.Cells["VeViec_Khen"].Value?.ToString() ?? "";
                if (richTextBox1_GhiChu != null) richTextBox1_GhiChu.Text = row.Cells["GhiChu_Khen"].Value?.ToString() ?? "";

                // GÁN 03 TRƯỜNG MỚI (CHÚ Ý CỘT TIỀN THƯỞNG SẼ TỰ KÍCH HOẠT EVENT FORMAT)
                if (kryptonTextBox_TienThuong != null) kryptonTextBox_TienThuong.Text = row.Cells["TienThuong"].Value?.ToString() ?? "0";
                if (kryptonTextBox_NgayCapPhat != null) kryptonTextBox_NgayCapPhat.Text = row.Cells["NgayCapPhat"].Value?.ToString() ?? "";
                if (kryptonTextBox_CanBoCapPhat != null) kryptonTextBox_CanBoCapPhat.Text = row.Cells["CanBoCapPhat"].Value?.ToString() ?? "";

                // 4. TRẢI NGHIỆM NGƯỜI DÙNG (UX): Đổi tên nút thành "Lưu"
                if (kryptonButton_Sua != null)
                {
                    // Với thư viện Krypton, việc đổi chữ thường nằm ở thuộc tính Values.Text
                    kryptonButton_Sua.Values.Text = "Lưu";
                }
            }
            catch (Exception ex)
            {
                // 5. GHI LOG LỖI MẦM: Không để phần mềm văng hộp thoại lỗi ra mặt người dùng khi click quá nhanh
                System.Diagnostics.Debug.WriteLine($"[Lỗi CellClick Grid Khen Thưởng]: {ex.Message}");
            }
        }
        private async void kryptonButton_Them_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentSoHieu)) return;
            if (!KiemTraDuLieuDauVao()) return;
            try
            {
                string tienThuongStr = kryptonTextBox_TienThuong?.Text.Replace(".", "").Replace(",", "").Trim() ?? "0";
                if (string.IsNullOrWhiteSpace(tienThuongStr)) tienThuongStr = "0";

                using var conn = new SqliteConnection($"Data Source={_csdl4Path}");
                await conn.OpenAsync(); // Mở kết nối bất đồng bộ
                using var tran = conn.BeginTransaction();
                try
                {
                    string sqlInsert = @"INSERT INTO ThongKe_GiayKhen 
                                         (HoVaTen, SoHieu, DonVi, TinhTrang, HinhThuc_Khen, QuyetDinh_Khen, NgayCapQD_Khen, DonVi_Khen, VeViec_Khen, TienThuong, NgayCapPhat, CanBoCapPhat, GhiChu_Khen) 
                                         VALUES 
                                         (@HT, @SH, @DV, @TT, @HinhThuc, @SoQD, @Ngay, @DVKhen, @VeViec, @TienThuong, @NgayCapPhat, @CanBoCapPhat, @GhiChu)";

                    using (var cmd = new SqliteCommand(sqlInsert, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@HT", BaoMatAES.MaHoa(_currentHoTen));
                        cmd.Parameters.AddWithValue("@SH", BaoMatAES.MaHoa(_currentSoHieu));
                        cmd.Parameters.AddWithValue("@DV", BaoMatAES.MaHoa(_currentDonVi));
                        cmd.Parameters.AddWithValue("@TT", _currentTinhTrang); // Tình trạng dạng Thuần

                        cmd.Parameters.AddWithValue("@HinhThuc", BaoMatAES.MaHoa(combobox_HinhThucKhen.Text));
                        cmd.Parameters.AddWithValue("@SoQD", BaoMatAES.MaHoa(kryptonTextBox_QuyetDinh.Text));
                        cmd.Parameters.AddWithValue("@Ngay", BaoMatAES.MaHoa(kryptonTextBox_NgayQuyDinh.Text));
                        cmd.Parameters.AddWithValue("@DVKhen", BaoMatAES.MaHoa(comboBox_DonViKhenThuong.Text));
                        cmd.Parameters.AddWithValue("@VeViec", BaoMatAES.MaHoa(richTextBox1_VeViec.Text));

                        cmd.Parameters.AddWithValue("@TienThuong", BaoMatAES.MaHoa(tienThuongStr));
                        cmd.Parameters.AddWithValue("@NgayCapPhat", BaoMatAES.MaHoa(kryptonTextBox_NgayCapPhat?.Text ?? ""));
                        cmd.Parameters.AddWithValue("@CanBoCapPhat", BaoMatAES.MaHoa(kryptonTextBox_CanBoCapPhat?.Text ?? ""));
                        cmd.Parameters.AddWithValue("@GhiChu", BaoMatAES.MaHoa(richTextBox1_GhiChu.Text));

                        await cmd.ExecuteNonQueryAsync();
                    }
                    tran.Commit();
                }
                catch { tran.Rollback(); throw; }

                // Ghi Log và đồng bộ lại Số lượng cực kỳ an toàn
                GhiLogHeThong($"Thêm mới dữ liệu khen thưởng: {_currentHoTen} (SH: {_currentSoHieu})");
                await DongBoSoLuongVeBangTongAsync(_currentSoHieu);

                XoaTrangGiaoDien();
                await ReloadDuLieuGiayKhen_CuaMotNguoiAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi thêm giấy khen: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async void kryptonButton_Sua_Click(object sender, EventArgs e)
        {
            if (_currentEditingID == -1)
            {
                MessageBox.Show("Vui lòng chọn một giấy khen từ danh sách bên dưới để Sửa!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!KiemTraDuLieuDauVao()) return;

            try
            {
                string tienThuongStr = kryptonTextBox_TienThuong?.Text.Replace(".", "").Replace(",", "").Trim() ?? "0";
                if (string.IsNullOrWhiteSpace(tienThuongStr)) tienThuongStr = "0";

                using var conn = new SqliteConnection($"Data Source={_csdl4Path}");
                await conn.OpenAsync();

                string sqlUpdate = @"UPDATE ThongKe_GiayKhen 
                                     SET HinhThuc_Khen = @HinhThuc, QuyetDinh_Khen = @SoQD, NgayCapQD_Khen = @Ngay, 
                                         DonVi_Khen = @DVKhen, VeViec_Khen = @VeViec, 
                                         TienThuong = @TienThuong, NgayCapPhat = @NgayCapPhat, CanBoCapPhat = @CanBoCapPhat, 
                                         GhiChu_Khen = @GhiChu 
                                     WHERE ID = @ID";

                using (var cmd = new SqliteCommand(sqlUpdate, conn))
                {
                    cmd.Parameters.AddWithValue("@HinhThuc", BaoMatAES.MaHoa(combobox_HinhThucKhen.Text));
                    cmd.Parameters.AddWithValue("@SoQD", BaoMatAES.MaHoa(kryptonTextBox_QuyetDinh.Text));
                    cmd.Parameters.AddWithValue("@Ngay", BaoMatAES.MaHoa(kryptonTextBox_NgayQuyDinh.Text));
                    cmd.Parameters.AddWithValue("@DVKhen", BaoMatAES.MaHoa(comboBox_DonViKhenThuong.Text));
                    cmd.Parameters.AddWithValue("@VeViec", BaoMatAES.MaHoa(richTextBox1_VeViec.Text));

                    cmd.Parameters.AddWithValue("@TienThuong", BaoMatAES.MaHoa(tienThuongStr));
                    cmd.Parameters.AddWithValue("@NgayCapPhat", BaoMatAES.MaHoa(kryptonTextBox_NgayCapPhat?.Text ?? ""));
                    cmd.Parameters.AddWithValue("@CanBoCapPhat", BaoMatAES.MaHoa(kryptonTextBox_CanBoCapPhat?.Text ?? ""));

                    cmd.Parameters.AddWithValue("@GhiChu", BaoMatAES.MaHoa(richTextBox1_GhiChu.Text));
                    cmd.Parameters.AddWithValue("@ID", _currentEditingID);

                    await cmd.ExecuteNonQueryAsync();
                }

                GhiLogHeThong($"Sửa dữ liệu khen thưởng: {_currentHoTen} (SH: {_currentSoHieu})");

                XoaTrangGiaoDien();
                await ReloadDuLieuGiayKhen_CuaMotNguoiAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi cập nhật giấy khen: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async void kryptonButton_Xoa_Click(object sender, EventArgs e)
        {
            if (_currentEditingID == -1)
            {
                MessageBox.Show("Vui lòng chọn một giấy khen từ danh sách bên dưới để Xóa!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Bạn có chắc chắn muốn xóa giấy khen này không? Thao tác này không thể hoàn tác.", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                return;
            }

            try
            {
                using var conn = new SqliteConnection($"Data Source={_csdl4Path}");
                await conn.OpenAsync();
                using var tran = conn.BeginTransaction();
                try
                {
                    string sqlDelete = "DELETE FROM ThongKe_GiayKhen WHERE ID = @ID";
                    using (var cmd = new SqliteCommand(sqlDelete, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@ID", _currentEditingID);
                        await cmd.ExecuteNonQueryAsync();
                    }
                    tran.Commit();
                }
                catch { tran.Rollback(); throw; }

                GhiLogHeThong($"Xóa dữ liệu khen thưởng: {_currentHoTen} (SH: {_currentSoHieu})");

                await DongBoSoLuongVeBangTongAsync(_currentSoHieu);

                XoaTrangGiaoDien();
                await ReloadDuLieuGiayKhen_CuaMotNguoiAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xóa giấy khen: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void kryptonButton3_DongForm_Click(object sender, EventArgs e)
        {
            XoaTrangGiaoDien();
            // Rất chuẩn: Form 35 tự sát, không cần biết ai gọi nó ra.
            this.Close();
        }
        // HÀM TIỆN ÍCH TỐI ƯU CỦA RIÊNG FORM 35 (Gọi Log)
        private void GhiLogHeThong(string hanhDong)
        {
            try
            {
                string tk = string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM) ? "System" : Module_TaiKhoan.TenTaiKhoan_RAM;
                Module_NhatKy.GhiNhatKy(tk, hanhDong, $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
            }
            catch { }
        }
        private void CapNhatSoLuongB2(SqliteConnection conn, SqliteTransaction tran, string soHieu, int valueChange)
        {
            string sqlSelect = "SELECT rowid, SoHieu, SoLuong_Khen FROM ThongKeCBCS_DuocKhenThuong";
            long targetRowId = -1;
            int slHienTai = 0;

            using (var cmdSelect = new SqliteCommand(sqlSelect, conn, tran))
            using (var reader = cmdSelect.ExecuteReader())
            {
                while (reader.Read())
                {
                    string shMaHoa = reader.IsDBNull(1) ? "" : reader.GetString(1);

                    if (string.Equals(SafeDecrypt(shMaHoa), soHieu, StringComparison.OrdinalIgnoreCase))
                    {
                        targetRowId = reader.GetInt64(0);

                        string slStr = reader.IsDBNull(2) ? "0" : reader.GetString(2);
                        int.TryParse(slStr, out slHienTai);

                        break;
                    }
                }
            }

            int slMoi = slHienTai + valueChange;
            if (slMoi < 0) slMoi = 0;

            if (targetRowId != -1)
            {
                string sqlUp = "UPDATE ThongKeCBCS_DuocKhenThuong SET SoLuong_Khen = @SL WHERE rowid = @RowId";
                using var cmdUp = new SqliteCommand(sqlUp, conn, tran);
                cmdUp.Parameters.AddWithValue("@SL", slMoi.ToString());
                cmdUp.Parameters.AddWithValue("@RowId", targetRowId);
                cmdUp.ExecuteNonQuery();
            }
            else if (valueChange > 0)
            {
                string sqlIn = "INSERT INTO ThongKeCBCS_DuocKhenThuong (HoVaTen, SoHieu, DonVi, TinhTrang, SoLuong_Khen) VALUES (@HT, @SH, @DV, @TT, '1')";
                using var cmdIn = new SqliteCommand(sqlIn, conn, tran);
                cmdIn.Parameters.AddWithValue("@HT", BaoMatAES.MaHoa(_currentHoTen));
                cmdIn.Parameters.AddWithValue("@SH", BaoMatAES.MaHoa(_currentSoHieu));
                cmdIn.Parameters.AddWithValue("@DV", BaoMatAES.MaHoa(_currentDonVi));
                cmdIn.Parameters.AddWithValue("@TT", _currentTinhTrang);
                cmdIn.ExecuteNonQuery();
            }
        }
        private bool KiemTraDuLieuDauVao()
        {
            if (string.IsNullOrWhiteSpace(_currentSoHieu))
            {
                MessageBox.Show("Lỗi hệ thống: Không xác định được Cán bộ chiến sĩ đang được thao tác!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(combobox_HinhThucKhen.Text))
            {
                MessageBox.Show("Vui lòng chọn hoặc nhập 'Hình thức khen thưởng'!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                combobox_HinhThucKhen.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(kryptonTextBox_QuyetDinh.Text))
            {
                MessageBox.Show("Vui lòng nhập 'Số Quyết định'!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                kryptonTextBox_QuyetDinh.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(comboBox_DonViKhenThuong.Text))
            {
                MessageBox.Show("Vui lòng chọn hoặc nhập 'Đơn vị khen thưởng'!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                comboBox_DonViKhenThuong.Focus();
                return false;
            }

            return true;
        }
        private void DongBoSoLuongVeBangTong(string soHieu)
        {
            if (string.IsNullOrWhiteSpace(soHieu)) return;

            try
            {
                using var conn = new SqliteConnection($"Data Source={_csdl4Path}");
                conn.Open();

                int soLuongThucTe = 0;
                using (var cmdCount = new SqliteCommand("SELECT SoHieu FROM ThongKe_GiayKhen", conn))
                using (var reader = cmdCount.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string shGiaiMa = reader.IsDBNull(0) ? "" : SafeDecrypt(reader.GetString(0));
                        if (string.Equals(shGiaiMa, soHieu, StringComparison.OrdinalIgnoreCase))
                        {
                            soLuongThucTe++;
                        }
                    }
                }

                long targetRowId = -1;
                using (var cmdSelect = new SqliteCommand("SELECT rowid, SoHieu FROM ThongKeCBCS_DuocKhenThuong", conn))
                using (var reader = cmdSelect.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string shGiaiMa2 = reader.IsDBNull(1) ? "" : SafeDecrypt(reader.GetString(1));
                        if (string.Equals(shGiaiMa2, soHieu, StringComparison.OrdinalIgnoreCase))
                        {
                            targetRowId = reader.GetInt64(0);
                            break;
                        }
                    }
                }

                if (targetRowId != -1)
                {
                    string sqlUpdate = "UPDATE ThongKeCBCS_DuocKhenThuong SET SoLuong_Khen = @SL WHERE rowid = @RowId";
                    using (var cmdUpdate = new SqliteCommand(sqlUpdate, conn))
                    {
                        cmdUpdate.Parameters.AddWithValue("@SL", soLuongThucTe.ToString());
                        cmdUpdate.Parameters.AddWithValue("@RowId", targetRowId);
                        cmdUpdate.ExecuteNonQuery();
                    }
                    System.Diagnostics.Debug.WriteLine($"Đã đồng bộ: {soHieu} có {soLuongThucTe} giấy khen.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi đồng bộ: " + ex.Message);
            }
        }   
    }
}