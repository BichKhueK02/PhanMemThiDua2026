///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form35_ChiTietKhenThuong : Form
    {
        private readonly string _csdl4Path = Module_DanduongGPS.DuongDanCSDL4;
        // Biến cục bộ để lưu trữ con trỏ Form 34 và vị trí dòng, phục vụ truy xuất O(1)
        private Form34_ThongKeKhenThuong _form34Nguon = null!;
        private int _currentRowIndexForm34 = -1;
        private string _currentSoHieu = "";
        private string _currentHoTen = "";
        private string _currentDonVi = "";
        private string _currentTinhTrang = "";
        private int _currentEditingID = -1;
        /// <summary>true = Form34 giữ form này để DÙNG LẠI: bấm Đóng thì Form52 chỉ cất vào kho, không hủy (xem Form52.FormChiTiet_FormClosing).</summary>
        internal bool DungLai { get; set; }
        // Quản lý Font tập trung dùng biến hệ thống, chống rò rỉ GDI handle
        private static readonly Font _fontGridCell10 = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Regular);
        private static readonly Font _fontGridHeader10Bold = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Bold);
        private static readonly Font _fontRowHeader12Bold = new Font(Module_HeThong.TenFontHeThong, 12F, FontStyle.Bold);
        // Dùng chung 1 StringFormat (bản cũ tạo mới cho MỖI dòng MỖI lần vẽ, không Dispose => rò rỉ GDI+)
        private static readonly StringFormat _canGiua = new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        public Form35_ChiTietKhenThuong()
        {
            InitializeComponent();
            Module_CheDoDoiTuong.ApNhanTheoCheDo(this);   // nhãn "CBCS" -> "Tân binh"/"Học viên" theo chế độ (trước khi dựng giao diện hiện đại)
            Module_BieuTuongNut.ApDung(this);   // biểu tượng nút: Hiện đại = tự vẽ, Cổ điển = ảnh gốc nhúng trong exe
            // 4 nút thao tác cùng 1 bề ngang chuẩn (bản thiết kế mỗi nút 1 cỡ: 120 / 108 / 98 / 98)
            foreach (var b in new[] { kryptonButton_Them, kryptonButton_Sua, kryptonButton_Xoa, kryptonButton3_DongForm })
                if (b != null) b.Width = Module_BoLocKhenThuong.Px(b, Module_BoLocKhenThuong.RongNut);
            kryptonDataGridView1_DanhSachCBCS.CellClick += KryptonDataGridView1_DanhSachCBCS_CellClick;
            kryptonDataGridView1_DanhSachCBCS.RowPostPaint += KryptonDataGridView1_DanhSachCBCS_RowPostPaint;
            // GẮN SỰ KIỆN ĐỊNH DẠNG TIỀN THƯỞNG
            if (kryptonTextBox_TienThuong != null)
            {
                kryptonTextBox_TienThuong.TextChanged += kryptonTextBox_TienThuong_TextChanged;
            }
        }
        private void Form35_ChiTietKhenThuong_Load(object? sender, EventArgs e)
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
        private void kryptonTextBox_TienThuong_TextChanged(object? sender, EventArgs e)
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
            string decrypted = Module_BaoMatAES.GiaiMa(s);
            // Kỹ thuật Fallback: Nếu giải mã rỗng (có thể do lỗi hoặc là text thường), trả về chuỗi ban đầu
            return string.IsNullOrEmpty(decrypted) ? s : decrypted;
        }
        // Lược đồ chỉ cần kiểm 1 lần cho CẢ PHIÊN chạy (bản cũ kiểm lại ở mỗi lần mở Form35: 2 PRAGMA + 2 CREATE mỗi lần).
        // Đường dẫn CSDL đổi (khôi phục / chuyển giao CSDL) thì kiểm lại.
        private static volatile string? _daKiemTraBangCho;
        private bool _daKiemTraBang => string.Equals(_daKiemTraBangCho, _csdl4Path, StringComparison.OrdinalIgnoreCase) && File.Exists(_csdl4Path);
        // ⭐ CHUẨN KỸ SƯ: Bổ sung async void để có thể chờ (await) hàm tải dữ liệu
        // ⭐ CHUẨN KỸ SƯ: Bổ sung formNguon và rowIndex để truy xuất ngược
        public async void NhanDuLieuTuForm34(string hoTen, string soHieu, string donVi, string tinhTrangCu, Form34_ThongKeKhenThuong formNguon, int rowIndex,
            long[]? idGiayKhen = null)
        {
            // 1. Lưu thông tin cơ bản
            _currentHoTen = hoTen;
            _currentSoHieu = soHieu;
            _currentDonVi = donVi;
            _currentTinhTrang = tinhTrangCu; // Tạm gán tình trạng cũ
            // Form34 đã đọc sẵn ID giấy khen (và CSDL chưa đổi từ đó) -> nạp thẳng theo ID, khỏi quét + giải mã cả bảng.
            // null = Form34 không chắc còn đúng -> lần nạp đầu quét cả bảng như cũ.
            _idGiayKhenCuaNguoi = idGiayKhen == null ? null : new HashSet<long>(idGiayKhen);
            _rowIdBangTong = -1;
            // 2. Lưu tham chiếu bộ nhớ để chọc ngược về Form 34
            _form34Nguon = formNguon;
            _currentRowIndexForm34 = rowIndex;
            // 3. Đổ dữ liệu nhân thân lên Nhãn
            if (label1_HoVaTen != null) label1_HoVaTen.Text = Module_HeThong.Tu_Dong_Chi + ": " + hoTen;
            if (label1_SoHieu != null) label1_SoHieu.Text = "Số hiệu: " + soHieu;
            if (label1_DonVi != null) label1_DonVi.Text = "Đơn vị: " + donVi;           
            // 4-5. Tình trạng lấy từ Form34 theo SỐ HIỆU (Form34 đã đối chiếu Form6) rồi đổ lên thanh trạng thái.
            //      Bản cũ lấy theo CHỈ SỐ DÒNG của Form34: sau khi Form34 nạp lại/lọc, chỉ số đó có thể là người khác.
            CapNhatTinhTrangTuForm34();
            if (!_daDangKyHienLai)
            {
                _daDangKyHienLai = true;
                VisibleChanged += Form35_VisibleChanged_DongBoLai;
            }
            // 6. Hoàn thiện quá trình load dữ liệu chi tiết
            XoaTrangGiaoDien();
            _chuKyLucNap = Module_ChuKyCSDL.Lay();
            await ReloadDuLieuGiayKhen_CuaMotNguoiAsync();
        }
        private bool _daDangKyHienLai;
        private string _chuKyLucNap = "";
        /// <summary>
        /// Form35 bị che khi người dùng sang trang khác (vd. Form6 xóa CBCS) rồi quay lại: Form52 nạp lại Form34 bên dưới,
        /// nên khi Form35 hiện lại thì lấy lại tình trạng theo Số hiệu và nạp lại giấy khen nếu CSDL đã đổi ở nơi khác.
        /// </summary>
        private async void Form35_VisibleChanged_DongBoLai(object? sender, EventArgs e)
        {
            if (!Visible || IsDisposed || string.IsNullOrWhiteSpace(_currentSoHieu)) return;
            CapNhatTinhTrangTuForm34();
            string chuKy = Module_ChuKyCSDL.Lay();
            if (string.Equals(chuKy, _chuKyLucNap, StringComparison.Ordinal)) return;
            _chuKyLucNap = chuKy;
            _idGiayKhenCuaNguoi = null;   // CSDL đã đổi: quét lại giấy khen theo Số hiệu
            _rowIdBangTong = -1;
            await ReloadDuLieuGiayKhen_CuaMotNguoiAsync();
        }
        private void CapNhatTinhTrangTuForm34()
        {
            string tinhTrangChinhXac = _currentTinhTrang;
            if (_form34Nguon != null && !_form34Nguon.IsDisposed)
            {
                string? tt = _form34Nguon.LayTinhTrangTheoSoHieu(_currentSoHieu);
                if (!string.IsNullOrEmpty(tt)) tinhTrangChinhXac = tt;
            }
            _currentTinhTrang = tinhTrangChinhXac;
            if (toolStripStatusLabel2_TinhTrangCongTac != null)
            {
                toolStripStatusLabel2_TinhTrangCongTac.Text = $"Tình trạng: {tinhTrangChinhXac}";
                toolStripStatusLabel2_TinhTrangCongTac.ForeColor =
                    string.Equals(tinhTrangChinhXac, Module_HeThong.TT_DANG_CONG_TAC, StringComparison.OrdinalIgnoreCase)
                        ? Color.SeaGreen : Color.IndianRed;
                toolStripStatusLabel2_TinhTrangCongTac.Visible = true;
            }
        }
        /// <summary>
        /// ID các giấy khen của CBCS đang mở. SoHieu mã hóa AES-GCM (nonce ngẫu nhiên) nên không lọc được bằng WHERE:
        /// chỉ lần đầu phải quét + giải mã cả bảng; sau đó Thêm/Xóa cập nhật tập này và nạp lại bằng WHERE ID IN (...).
        /// null = chưa quét. Chỉ đọc/ghi trên luồng UI (luồng nền nhận bản sao).
        /// </summary>
        private HashSet<long>? _idGiayKhenCuaNguoi;
        /// <summary>rowid dòng của CBCS này trong ThongKeCBCS_DuocKhenThuong (-1 = chưa tìm / chưa có).</summary>
        private long _rowIdBangTong = -1;
        private async Task ReloadDuLieuGiayKhen_CuaMotNguoiAsync()
        {
            try
            {
                DataTable dt = new DataTable();
                long[]? idDaBiet = _idGiayKhenCuaNguoi?.ToArray();
                string soHieu = _currentSoHieu;
                var idTimThay = new HashSet<long>();
                // Đẩy toàn bộ quá trình đọc DB và giải mã xuống luồng nền (Background Thread)
                await Task.Run(() =>
                {
                    using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path));
                    conn.Open();
                    if (!_daKiemTraBang) { Module_BangKhenThuong.DamBao(conn); _daKiemTraBangCho = _csdl4Path; }
                    // ID là số nguyên lấy từ chính CSDL -> ghép thẳng vào câu lệnh an toàn; danh sách rỗng thì chỉ lấy cấu trúc cột
                    string sql = idDaBiet == null
                        ? $"SELECT * FROM {Module_CheDoDoiTuong.BangGiayKhen} ORDER BY ID ASC"
                        : idDaBiet.Length == 0
                            ? $"SELECT * FROM {Module_CheDoDoiTuong.BangGiayKhen} WHERE 0"
                            : $"SELECT * FROM {Module_CheDoDoiTuong.BangGiayKhen} WHERE ID IN ({string.Join(",", idDaBiet)}) ORDER BY ID ASC";
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
                    int idxId = reader.GetOrdinal("ID");
                    while (reader.Read())
                    {
                        string soHieuGiaiMa = SafeDecrypt(reader["SoHieu"]);
                        if (string.Equals(soHieuGiaiMa, soHieu, StringComparison.OrdinalIgnoreCase))
                        {
                            idTimThay.Add(Convert.ToInt64(reader.GetValue(idxId)));
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
                if (IsDisposed || !string.Equals(soHieu, _currentSoHieu, StringComparison.Ordinal)) return;
                _idGiayKhenCuaNguoi = idTimThay;   // khớp đúng những gì đang hiện (ID đã bị nơi khác xóa thì tự rơi ra)
                // Cập nhật giao diện sau khi luồng nền đã chạy xong (An toàn 100%)
                GanDuLieuLuoi(dt);
                CapNhatSoLuongKhenThuong();
                _currentEditingID = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải chi tiết: " + ex.Message, "Hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// Ghi số lượng giấy khen về bảng tổng, chạy TRONG CÙNG giao dịch với lệnh Thêm/Xóa (luồng nền).
        /// Số lượng lấy từ tập ID đã biết (không quét + giải mã lại cả ThongKe_GiayKhen như bản cũ);
        /// rowid bảng tổng chỉ phải dò (giải mã) 1 lần cho mỗi CBCS. Trả về rowid bảng tổng (-1 nếu chưa có dòng).
        /// </summary>
        private long GhiSoLuongVeBangTong(SqliteConnection conn, SqliteTransaction tran, long rowIdBiet,
            string hoTen, string soHieu, string donVi, string tinhTrang, int soLuong)
        {
            long rowId = rowIdBiet;
            if (rowId < 0)
            {
                // Bản cũ đồng bộ ở bước riêng và nuốt lỗi: CSDL chưa có bảng tổng thì vẫn thêm/xóa được giấy khen như cũ
                using (var cmdCo = new SqliteCommand($"SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = '{Module_CheDoDoiTuong.BangKhenThuongNguoi}' LIMIT 1", conn, tran))
                    if (cmdCo.ExecuteScalar() == null) return -1;
                using var cmdSelect = new SqliteCommand($"SELECT rowid, SoHieu FROM {Module_CheDoDoiTuong.BangKhenThuongNguoi}", conn, tran);
                using var reader = cmdSelect.ExecuteReader();
                while (reader.Read())
                {
                    if (string.Equals(SafeDecrypt(reader["SoHieu"]), soHieu, StringComparison.OrdinalIgnoreCase))
                    {
                        rowId = reader.GetInt64(0);
                        break;
                    }
                }
            }
            if (rowId >= 0)
            {
                using var cmdUpdate = new SqliteCommand($"UPDATE {Module_CheDoDoiTuong.BangKhenThuongNguoi} SET SoLuong_Khen = @SL WHERE rowid = @RowId", conn, tran);
                cmdUpdate.Parameters.AddWithValue("@SL", soLuong.ToString());
                cmdUpdate.Parameters.AddWithValue("@RowId", rowId);
                if (cmdUpdate.ExecuteNonQuery() > 0) return rowId;
                rowId = -1;   // dòng đã bị nơi khác xóa -> chèn lại bên dưới
            }
            if (soLuong <= 0) return -1;
            using var cmdIn = new SqliteCommand($"INSERT INTO {Module_CheDoDoiTuong.BangKhenThuongNguoi} (HoVaTen, SoHieu, DonVi, TinhTrang, SoLuong_Khen) VALUES (@HT, @SH, @DV, @TT, @SL); SELECT last_insert_rowid();", conn, tran);
            cmdIn.Parameters.AddWithValue("@HT", Module_BaoMatAES.MaHoa(hoTen));
            cmdIn.Parameters.AddWithValue("@SH", Module_BaoMatAES.MaHoa(soHieu));
            cmdIn.Parameters.AddWithValue("@DV", Module_BaoMatAES.MaHoa(donVi));
            cmdIn.Parameters.AddWithValue("@TT", tinhTrang); // Tình trạng Text thường
            cmdIn.Parameters.AddWithValue("@SL", soLuong.ToString());
            return Convert.ToInt64(cmdIn.ExecuteScalar());
        }
        /// <summary>Lần nạp đầu chưa chạy/lỗi thì chưa biết tập ID: quét 1 lần ngay trong giao dịch (luồng nền).</summary>
        private HashSet<long> QuetIdGiayKhen(SqliteConnection conn, SqliteTransaction tran, string soHieu)
        {
            var ds = new HashSet<long>();
            using var cmd = new SqliteCommand($"SELECT ID, SoHieu FROM {Module_CheDoDoiTuong.BangGiayKhen}", conn, tran);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                if (string.Equals(SafeDecrypt(rd["SoHieu"]), soHieu, StringComparison.OrdinalIgnoreCase))
                    ds.Add(Convert.ToInt64(rd.GetValue(0)));
            return ds;
        }
        // ===================== LƯỚI: GẮN 1 LẦN, NẠP NHIỀU LẦN =====================
        // Đo (Release 22/11): lần gán DataSource ĐẦU TIÊN trên 1 Form35 mất ~100 ms (lưới Krypton tự dựng 14 cột) + định dạng ~34 ms;
        // gán lại trên cùng form chỉ ~13 ms. => Giữ 1 DataTable đã gắn vào lưới, mỗi lần nạp chỉ thay DÒNG trong bảng đó.
        private DataTable? _bangLuoi;
        private static bool CungCauTruc(DataTable a, DataTable b)
        {
            if (a.Columns.Count != b.Columns.Count) return false;
            for (int i = 0; i < a.Columns.Count; i++)
                if (a.Columns[i].ColumnName != b.Columns[i].ColumnName || a.Columns[i].DataType != b.Columns[i].DataType) return false;
            return true;
        }
        private void GanDuLieuLuoi(DataTable moi)
        {
            var grid = kryptonDataGridView1_DanhSachCBCS;
            if (_bangLuoi != null && ReferenceEquals((grid.DataSource as DataView)?.Table, _bangLuoi) && CungCauTruc(_bangLuoi, moi))
            {
                _bangLuoi.BeginLoadData();
                try
                {
                    _bangLuoi.Clear();
                    foreach (DataRow r in moi.Rows) _bangLuoi.LoadDataRow(r.ItemArray, true);
                }
                finally { _bangLuoi.EndLoadData(); }
                return;
            }
            _bangLuoi = moi;
            grid.AutoGenerateColumns = true;
            grid.DataSource = moi.DefaultView;
            DinhDangLuoi();
        }
        /// <summary>
        /// Dựng sẵn cột lưới bằng 1 bảng RỖNG cùng cấu trúc ThongKe_GiayKhen (Form34 gọi khi dựng sẵn Form35 lúc rảnh tay):
        /// lần mở thật chỉ còn việc đổ dòng.
        /// </summary>
        internal async Task LamNongLuoiAsync()
        {
            string duongDan = _csdl4Path;
            DataTable rong = await Task.Run(() =>
            {
                var dt = new DataTable();
                if (string.IsNullOrWhiteSpace(duongDan) || !File.Exists(duongDan)) return dt;
                using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(duongDan));
                conn.Open();
                if (!string.Equals(_daKiemTraBangCho, duongDan, StringComparison.OrdinalIgnoreCase))
                { Module_BangKhenThuong.DamBao(conn); _daKiemTraBangCho = duongDan; }
                using var cmd = new SqliteCommand($"SELECT * FROM {Module_CheDoDoiTuong.BangGiayKhen} WHERE 0", conn);
                using var reader = cmd.ExecuteReader();
                for (int i = 0; i < reader.FieldCount; i++)   // đúng kiểu cột như ReloadDuLieuGiayKhen_CuaMotNguoiAsync tạo
                {
                    string ten = reader.GetName(i);
                    dt.Columns.Add(ten, ten == "ID" ? typeof(int) : ten == "TienThuong" ? typeof(long) : typeof(string));
                }
                return dt;
            });
            if (IsDisposed || rong.Columns.Count == 0 || _bangLuoi != null) return;
            GanDuLieuLuoi(rong);
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
            (Ten: "GhiChu_Khen", TieuDe: Module_HeThong.TieuDeCot.GhiChu, TyLe: 14f, CanLe: DataGridViewContentAlignment.MiddleLeft)
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
                    // Chỉ xóa DÒNG, giữ nguyên cột đã dựng (bản cũ gỡ DataSource + xóa cột => lần gán sau lưới Krypton dựng lại 14 cột ~100 ms)
                    _bangLuoi?.Clear();
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
        private void KryptonDataGridView1_DanhSachCBCS_RowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
        {
            var grid = sender as DataGridView;
            if (grid == null) return;
            string rowIdx = (e.RowIndex + 1).ToString();
            var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);
            e.Graphics.DrawString(rowIdx, _fontRowHeader12Bold, SystemBrushes.ControlText, headerBounds, _canGiua);
        }
        private void KryptonDataGridView1_DanhSachCBCS_CellClick(object? sender, DataGridViewCellEventArgs e)
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
        /// <summary>
        /// Đang có thao tác Thêm/Sửa/Xóa (cả chuỗi: ghi -> đồng bộ số lượng -> nạp lại). Chặn bấm chồng: bản cũ có các await ở
        /// giữa chuỗi nên bấm "Thêm" lần 2 trong lúc đang đồng bộ sẽ thêm trùng giấy khen. Chỉ đọc/ghi trên luồng UI.
        /// </summary>
        private bool _dangGhiCSDL;
        /// <summary>Giá trị các ô nhập của 1 giấy khen, ĐỌC TRÊN LUỒNG UI rồi mới đưa xuống luồng nền (luồng nền không đụng control).</summary>
        private sealed record GiayKhenNhap(string HinhThuc, string SoQD, string Ngay, string DVKhen, string VeViec,
                                           string TienThuong, string NgayCapPhat, string CanBoCapPhat, string GhiChu);
        private GiayKhenNhap DocGiayKhenTuGiaoDien()
        {
            string tienThuongStr = kryptonTextBox_TienThuong?.Text.Replace(".", "").Replace(",", "").Trim() ?? "0";
            if (string.IsNullOrWhiteSpace(tienThuongStr)) tienThuongStr = "0";
            return new GiayKhenNhap(
                combobox_HinhThucKhen.Text,
                kryptonTextBox_QuyetDinh.Text,
                kryptonTextBox_NgayQuyDinh.Text,
                comboBox_DonViKhenThuong.Text,
                richTextBox1_VeViec.Text,
                tienThuongStr,
                kryptonTextBox_NgayCapPhat?.Text ?? "",
                kryptonTextBox_CanBoCapPhat?.Text ?? "",
                richTextBox1_GhiChu.Text);
        }
        private async void kryptonButton_Them_Click(object? sender, EventArgs e)
        {
            if (_dangGhiCSDL) return;
            if (string.IsNullOrWhiteSpace(_currentSoHieu)) return;
            if (!KiemTraDuLieuDauVao()) return;
            _dangGhiCSDL = true;
            try
            {
                // Chụp dữ liệu trên luồng UI (form có thể được chuyển sang CBCS khác trong lúc đang ghi)
                var gk = DocGiayKhenTuGiaoDien();
                string csdlPath = _csdl4Path, hoTen = _currentHoTen, soHieu = _currentSoHieu, donVi = _currentDonVi, tinhTrang = _currentTinhTrang;
                HashSet<long>? ids = _idGiayKhenCuaNguoi == null ? null : new HashSet<long>(_idGiayKhenCuaNguoi);
                long rowIdTong = _rowIdBangTong;
                // Microsoft.Data.Sqlite KHÔNG có I/O bất đồng bộ thật (OpenAsync/ExecuteNonQueryAsync chạy đồng bộ trên luồng gọi)
                // => đẩy hẳn cả khối mở kết nối + mã hóa + ghi xuống luồng nền.
                await Task.Run(() =>
                {
                    using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(csdlPath));
                    conn.Open();
                    using var tran = conn.BeginTransaction();
                    try
                    {
                        Module_BangKhenThuong.DamBao(conn, tran);   // CSDL chưa có bảng thì tạo đủ cột rồi mới ghi
                        string sqlInsert = $@"INSERT INTO {Module_CheDoDoiTuong.BangGiayKhen}
                                         (HoVaTen, SoHieu, DonVi, TinhTrang, HinhThuc_Khen, QuyetDinh_Khen, NgayCapQD_Khen, DonVi_Khen, VeViec_Khen, TienThuong, NgayCapPhat, CanBoCapPhat, GhiChu_Khen)
                                         VALUES
                                         (@HT, @SH, @DV, @TT, @HinhThuc, @SoQD, @Ngay, @DVKhen, @VeViec, @TienThuong, @NgayCapPhat, @CanBoCapPhat, @GhiChu)";
                        using (var cmd = new SqliteCommand(sqlInsert, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@HT", Module_BaoMatAES.MaHoa(hoTen));
                            cmd.Parameters.AddWithValue("@SH", Module_BaoMatAES.MaHoa(soHieu));
                            cmd.Parameters.AddWithValue("@DV", Module_BaoMatAES.MaHoa(donVi));
                            cmd.Parameters.AddWithValue("@TT", tinhTrang); // Tình trạng dạng Thuần
                            cmd.Parameters.AddWithValue("@HinhThuc", Module_BaoMatAES.MaHoa(gk.HinhThuc));
                            cmd.Parameters.AddWithValue("@SoQD", Module_BaoMatAES.MaHoa(gk.SoQD));
                            cmd.Parameters.AddWithValue("@Ngay", Module_BaoMatAES.MaHoa(gk.Ngay));
                            cmd.Parameters.AddWithValue("@DVKhen", Module_BaoMatAES.MaHoa(gk.DVKhen));
                            cmd.Parameters.AddWithValue("@VeViec", Module_BaoMatAES.MaHoa(gk.VeViec));
                            cmd.Parameters.AddWithValue("@TienThuong", Module_BaoMatAES.MaHoa(gk.TienThuong));
                            cmd.Parameters.AddWithValue("@NgayCapPhat", Module_BaoMatAES.MaHoa(gk.NgayCapPhat));
                            cmd.Parameters.AddWithValue("@CanBoCapPhat", Module_BaoMatAES.MaHoa(gk.CanBoCapPhat));
                            cmd.Parameters.AddWithValue("@GhiChu", Module_BaoMatAES.MaHoa(gk.GhiChu));
                            cmd.ExecuteNonQuery();
                        }
                        long idMoi;
                        using (var cmdId = new SqliteCommand($"SELECT ID FROM {Module_CheDoDoiTuong.BangGiayKhen} WHERE rowid = last_insert_rowid()", conn, tran))
                            idMoi = Convert.ToInt64(cmdId.ExecuteScalar());
                        ids ??= QuetIdGiayKhen(conn, tran, soHieu);   // QuetId đã thấy luôn dòng vừa thêm
                        ids.Add(idMoi);
                        // Số lượng về bảng tổng: cùng giao dịch -> giấy khen và số lượng không bao giờ lệch nhau
                        rowIdTong = GhiSoLuongVeBangTong(conn, tran, rowIdTong, hoTen, soHieu, donVi, tinhTrang, ids.Count);
                        tran.Commit();
                    }
                    catch { tran.Rollback(); throw; }
                });
                GhiLogHeThong($"Thêm mới dữ liệu khen thưởng: {hoTen} (SH: {soHieu})");
                if (IsDisposed) return;
                if (string.Equals(soHieu, _currentSoHieu, StringComparison.Ordinal)) { _idGiayKhenCuaNguoi = ids; _rowIdBangTong = rowIdTong; }
                XoaTrangGiaoDien();
                await ReloadDuLieuGiayKhen_CuaMotNguoiAsync();
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                    MessageBox.Show("Lỗi thêm giấy khen: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { _dangGhiCSDL = false; }
        }
        private async void kryptonButton_Sua_Click(object? sender, EventArgs e)
        {
            if (_dangGhiCSDL) return;
            if (_currentEditingID == -1)
            {
                MessageBox.Show("Vui lòng chọn một giấy khen từ danh sách bên dưới để Sửa!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!KiemTraDuLieuDauVao()) return;
            _dangGhiCSDL = true;
            try
            {
                // Chụp trên luồng UI: trong lúc ghi, người dùng có thể bấm sang dòng khác (đổi _currentEditingID)
                var gk = DocGiayKhenTuGiaoDien();
                string csdlPath = _csdl4Path, hoTen = _currentHoTen, soHieu = _currentSoHieu;
                int idSua = _currentEditingID;
                await Task.Run(() =>
                {
                    using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(csdlPath));
                    conn.Open();
                    string sqlUpdate = $@"UPDATE {Module_CheDoDoiTuong.BangGiayKhen}
                                     SET HinhThuc_Khen = @HinhThuc, QuyetDinh_Khen = @SoQD, NgayCapQD_Khen = @Ngay,
                                         DonVi_Khen = @DVKhen, VeViec_Khen = @VeViec,
                                         TienThuong = @TienThuong, NgayCapPhat = @NgayCapPhat, CanBoCapPhat = @CanBoCapPhat,
                                         GhiChu_Khen = @GhiChu
                                     WHERE ID = @ID";
                    using var cmd = new SqliteCommand(sqlUpdate, conn);
                    cmd.Parameters.AddWithValue("@HinhThuc", Module_BaoMatAES.MaHoa(gk.HinhThuc));
                    cmd.Parameters.AddWithValue("@SoQD", Module_BaoMatAES.MaHoa(gk.SoQD));
                    cmd.Parameters.AddWithValue("@Ngay", Module_BaoMatAES.MaHoa(gk.Ngay));
                    cmd.Parameters.AddWithValue("@DVKhen", Module_BaoMatAES.MaHoa(gk.DVKhen));
                    cmd.Parameters.AddWithValue("@VeViec", Module_BaoMatAES.MaHoa(gk.VeViec));
                    cmd.Parameters.AddWithValue("@TienThuong", Module_BaoMatAES.MaHoa(gk.TienThuong));
                    cmd.Parameters.AddWithValue("@NgayCapPhat", Module_BaoMatAES.MaHoa(gk.NgayCapPhat));
                    cmd.Parameters.AddWithValue("@CanBoCapPhat", Module_BaoMatAES.MaHoa(gk.CanBoCapPhat));
                    cmd.Parameters.AddWithValue("@GhiChu", Module_BaoMatAES.MaHoa(gk.GhiChu));
                    cmd.Parameters.AddWithValue("@ID", idSua);
                    cmd.ExecuteNonQuery();
                });
                GhiLogHeThong($"Sửa dữ liệu khen thưởng: {hoTen} (SH: {soHieu})");
                if (IsDisposed) return;
                XoaTrangGiaoDien();
                await ReloadDuLieuGiayKhen_CuaMotNguoiAsync();
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                    MessageBox.Show("Lỗi cập nhật giấy khen: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { _dangGhiCSDL = false; }
        }
        private async void kryptonButton_Xoa_Click(object? sender, EventArgs e)
        {
            if (_dangGhiCSDL) return;
            if (_currentEditingID == -1)
            {
                MessageBox.Show("Vui lòng chọn một giấy khen từ danh sách bên dưới để Xóa!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("Bạn có chắc chắn muốn xóa giấy khen này không? Thao tác này không thể hoàn tác.", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                return;
            }
            _dangGhiCSDL = true;
            try
            {
                // Chụp trên luồng UI (sau hộp xác nhận): ID giấy khen + CBCS đang thao tác
                string csdlPath = _csdl4Path, hoTen = _currentHoTen, soHieu = _currentSoHieu, donVi = _currentDonVi, tinhTrang = _currentTinhTrang;
                int idXoa = _currentEditingID;
                HashSet<long>? ids = _idGiayKhenCuaNguoi == null ? null : new HashSet<long>(_idGiayKhenCuaNguoi);
                long rowIdTong = _rowIdBangTong;
                await Task.Run(() =>
                {
                    using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(csdlPath));
                    conn.Open();
                    using var tran = conn.BeginTransaction();
                    try
                    {
                        string sqlDelete = $"DELETE FROM {Module_CheDoDoiTuong.BangGiayKhen} WHERE ID = @ID";
                        using (var cmd = new SqliteCommand(sqlDelete, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@ID", idXoa);
                            cmd.ExecuteNonQuery();
                        }
                        ids ??= QuetIdGiayKhen(conn, tran, soHieu);
                        ids.Remove(idXoa);
                        rowIdTong = GhiSoLuongVeBangTong(conn, tran, rowIdTong, hoTen, soHieu, donVi, tinhTrang, ids.Count);
                        tran.Commit();
                    }
                    catch { tran.Rollback(); throw; }
                });
                GhiLogHeThong($"Xóa dữ liệu khen thưởng: {hoTen} (SH: {soHieu})");
                if (IsDisposed) return;
                if (string.Equals(soHieu, _currentSoHieu, StringComparison.Ordinal)) { _idGiayKhenCuaNguoi = ids; _rowIdBangTong = rowIdTong; }
                XoaTrangGiaoDien();
                await ReloadDuLieuGiayKhen_CuaMotNguoiAsync();
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                    MessageBox.Show("Lỗi xóa giấy khen: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { _dangGhiCSDL = false; }
        }
        private void kryptonButton3_DongForm_Click(object? sender, EventArgs e)
        {
            // Form dùng lại (DungLai): KHÔNG dọn lúc còn đang hiện - dọn làm form vẽ lại 1 lượt trạng thái trống ngay trước khi ẩn
            // (tốn công + lộ 1 khung hình trống). Lần mở sau NhanDuLieuTuForm34 dọn sẵn khi form còn ẩn.
            if (!DungLai) XoaTrangGiaoDien();
            // Rất chuẩn: Form 35 tự sát, không cần biết ai gọi nó ra.
            this.Close();
        }
        // HÀM TIỆN ÍCH TỐI ƯU CỦA RIÊNG FORM 35 (Gọi Log)
        private void GhiLogHeThong(string hanhDong)
        {
            try
            {
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, hanhDong, $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
            }
            catch { }
        }
        private bool KiemTraDuLieuDauVao()
        {
            if (string.IsNullOrWhiteSpace(_currentSoHieu))
            {
                MessageBox.Show("Lỗi hệ thống: Không xác định được Cán bộ chiến sĩ đang được thao tác!", Module_HeThong.TieuDe_Loi, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
    }
}