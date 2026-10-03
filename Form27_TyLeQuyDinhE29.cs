using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System.Diagnostics;   // Cho Debug.WriteLine
using System.Data;          // Cho CommandBehavior

namespace PhanMemThiDua2026
{
    public partial class Form27_TyLeQuyDinhE29 : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private readonly Color _focusColor = Color.FromArgb(0, 120, 215); // xanh chuẩn Win
        private readonly Color _normalColor = Color.FromArgb(200, 200, 200);
        public static event Action? OnQuyDinhChanged;
        // 1. Thêm biến Timer để quản lý thông báo
        private CancellationTokenSource? _ctsLuuDuLieu;
        private static readonly object _logLock = new object(); // Dùng cho việc khóa file log
        private System.Windows.Forms.Timer _timerThongBao;
        public Dictionary<string, int[]> DeNghiMapping { get; private set; }
            = new Dictionary<string, int[]>();
        private KryptonTextBox[,] _txtGrid;
        private void InitTextBoxGrid()
        {
            // Cấu trúc: _txtGrid[row, column]
            _txtGrid = new KryptonTextBox[3, 5]
            {
        { textBox_A1, textBox_B1, textBox_C1, textBox_D1, textBox_E1 },
        { textBox_A2, textBox_B2, textBox_C2, textBox_D2, textBox_E2 },
        { textBox_A3, textBox_B3, textBox_C3, textBox_D3, textBox_E3 }
            };
        }
        public Form27_TyLeQuyDinhE29()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            this.ShowInTaskbar = false;
            // 2. Cấu hình Label và Timer ban đầu
            if (label1_ThongBao != null)
            {
                label1_ThongBao.Visible = false; // Ẩn label lúc mới mở
            }
            _timerThongBao = new System.Windows.Forms.Timer();
            _timerThongBao.Interval = 3000; // Hiển thị trong 3 giây
            _timerThongBao.Tick += _timerThongBao_Tick;
        }
        private void _timerThongBao_Tick(object? sender, EventArgs e)
        {
            _timerThongBao.Stop();
            if (label1_ThongBao != null)
            {
                label1_ThongBao.Visible = false;
            }
        }
        // ĐÃ NÂNG CẤP: Thêm tham số Color để hỗ trợ hiện nhiều loại thông báo
        private void HienThiThongBao(string noiDung, Color mauChu)
        {
            if (label1_ThongBao == null) return;
            // --- BƯỚC 1: XÓA CÁC LỆNH ÉP VỊ TRÍ ---
            // Không dùng label1_ThongBao.Parent = this;
            // Không dùng label1_ThongBao.Location = new Point(20, 20);
            // Bạn có thể giữ lại AutoSize và Font (nếu chưa thiết lập trong Designer)
            // Tốt nhất là cấu hình Font, Màu Nền ở Designer để code gọn gàng hơn.
            label1_ThongBao.AutoSize = true;
            // Nếu muốn đổi màu nền nổi bật khi có thông báo:
            // label1_ThongBao.BackColor = Color.LightYellow; 
            // --- BƯỚC 2: CẬP NHẬT NỘI DUNG ---
            string thoiGian = DateTime.Now.ToString("HH:mm:ss");
            label1_ThongBao.Text = $"[{thoiGian}] {noiDung}";
            label1_ThongBao.ForeColor = mauChu;
            // --- BƯỚC 3: HIỂN THỊ VÀ CẬP NHẬT GIAO DIỆN ---
            label1_ThongBao.Visible = true;
            label1_ThongBao.BringToFront(); // Đảm bảo label luôn nổi lên trên cùng (không bị control khác đè lên)
            label1_ThongBao.Refresh();
            // --- BƯỚC 4: KÍCH HOẠT TIMER ---
            _timerThongBao.Stop();
            _timerThongBao.Start();
        }
        private async void Form27_TyLeQuyDinhE29_Load(object? sender, EventArgs e)
        {
            InitTextBoxGrid(); // khởi tạo mảng TextBox trước
            await LoadQuyDinhTyLeAsync(); // gọi hàm async
            GanSuKienFocusTextBox();
            DoiTenGroupBox();
            InitToolTips();
            SetupStatusStrip();
        }
        // Nhớ gọi hàm này trong Constructor (Public Form...) ngay dưới InitializeComponent()
        private void SetupStatusStrip()
        {
            try
            {
                // Tắt resize linh tinh
                statusStrip1.SizingGrip = false;
                statusStrip1.AutoSize = false;
                // Label trái
                toolStripStatusLabel1.Text = $"Phiên bản: {Module_PhienBan.SoftwareVersion}";
                toolStripStatusLabel1.Alignment = ToolStripItemAlignment.Left;
                // Label phải
                toolStripStatusLabel2.Text = Module_PhienBan.NgayThangNamCapNhat;
                toolStripStatusLabel2.Alignment = ToolStripItemAlignment.Right;
                // Label đệm
                var springLabel = new ToolStripStatusLabel
                {
                    Spring = true
                };
                // Set lại layout
                statusStrip1.SuspendLayout();
                statusStrip1.Items.Clear();
                statusStrip1.Items.Add(toolStripStatusLabel1);
                statusStrip1.Items.Add(springLabel);
                statusStrip1.Items.Add(toolStripStatusLabel2);
                statusStrip1.ResumeLayout();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi SetupStatusStrip: " + ex.Message);
            }
        }
        private void InitToolTips()
        {
            // Cấu hình chung
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = Module_HeThong.Thao_Tac;
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            // Gán ToolTip
            if (kryptonButton_LuuTyLeBCHE09 != null)
            {
                toolTip1.SetToolTip(kryptonButton_LuuTyLeBCHE09, "Lưu quy định tỷ lệ vào cơ sở dữ liệu");
            }
        }
        // THÊM: BIẾN ĐỂ XÁC ĐỊNH BẢNG THEO CHẾ ĐỘ (TÂN BINH / CBCS)    
        private string TenBangHienTai
        {
            get
            {
                string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
                return phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase)
                    ? "QuyDinhTyLe_TanBinh"
                    : "QuyDinhTyLe";
            }
        }
        //private async Task LoadQuyDinhTyLeAsync()
        //{
        //    if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path)) return;
        //    try
        //    {
        //        using var conn = new SqliteConnection($"Data Source={_csdl2Path};Mode=ReadWriteCreate");
        //        await conn.OpenAsync();
        //        string tableName = TenBangHienTai;
        //        // Tự động tạo bảng chống lỗi văng form
        //        using (var cmdCreate = conn.CreateCommand())
        //        {
        //            cmdCreate.CommandText = $@"
        //CREATE TABLE IF NOT EXISTS [{tableName}] (
        //    ID INTEGER NOT NULL,
        //    TenLoaiTapThe TEXT,
        //    Loai_1 TEXT,
        //    Loai_2 TEXT,
        //    Loai_3 TEXT,
        //    Loai_4 TEXT,
        //    Khong_PL TEXT,
        //    PRIMARY KEY(ID AUTOINCREMENT)
        //);
        //INSERT OR IGNORE INTO [{tableName}] (ID, TenLoaiTapThe, Loai_1, Loai_2, Loai_3, Loai_4, Khong_PL)
        //VALUES
        //(1, '{Module_HeThong.Loai_1}', '0', '0', '0', '0', '0'),
        //(2, '{Module_HeThong.Loai_2}', '0', '0', '0', '0', '0'),
        //(3, '{Module_HeThong.Loai_3}', '0', '0', '0', '0', '0');";

        //            await cmdCreate.ExecuteNonQueryAsync();
        //        }
        //        DeNghiMapping.Clear();
        //        int rows = _txtGrid.GetLength(0);
        //        int cols = _txtGrid.GetLength(1);
        //        for (int id = 1; id <= rows; id++)
        //        {
        //            int[] values = new int[cols];
        //            using var cmd = conn.CreateCommand();
        //            cmd.CommandText = $@"SELECT Loai_1, Loai_2, Loai_3, Loai_4, Khong_PL 
        //                        FROM [{tableName}] WHERE ID=@id";
        //            cmd.Parameters.AddWithValue("@id", id);
        //            using var reader = await cmd.ExecuteReaderAsync();
        //            if (await reader.ReadAsync())
        //            {
        //                for (int i = 0; i < cols; i++)
        //                {
        //                    string valStr = reader[i]?.ToString() ?? "0";
        //                    values[i] = int.TryParse(valStr, out int parsed) ? parsed : 0;
        //                    _txtGrid[id - 1, i].Text = values[i].ToString();
        //                }
        //            }
        //            DeNghiMapping[$"Loại {id}"] = values;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Lỗi khi load dữ liệu tỷ lệ!\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //        System.Diagnostics.Debug.WriteLine(ex);
        //    }
        //}
        private static readonly string[] CotDuLieuTyLe = { "Loai_1", "Loai_2", "Loai_3", "Loai_4", "Khong_PL" };
        private static readonly System.Text.RegularExpressions.Regex QuyTacTenBangHopLe =
            new(@"^[A-Za-z0-9_]+$", System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>
        /// Tải quy định tỷ lệ phân loại tập thể từ CSDL2 lên lưới nhập liệu (_txtGrid).
        /// Tự động khởi tạo bảng + dữ liệu mặc định nếu chưa tồn tại.
        /// An toàn giao dịch (transaction), chống SQL Injection, và tối ưu 1 lần truy vấn duy nhất.
        /// </summary>
        private async Task LoadQuyDinhTyLeAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path))
                return;

            string tableName = TenBangHienTai;

            // ⭐ CHỐNG SQL INJECTION QUA TÊN BẢNG (tên bảng không thể tham số hóa trong SQL)
            if (!QuyTacTenBangHopLe.IsMatch(tableName))
            {
                Debug.WriteLine($"[LoadQuyDinhTyLeAsync] Tên bảng không hợp lệ: '{tableName}'");
                MessageBox.Show("Tên bảng dữ liệu không hợp lệ, không thể tải quy định tỷ lệ.",
                    "Lỗi cấu hình", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_txtGrid == null || _txtGrid.GetLength(1) != CotDuLieuTyLe.Length)
            {
                Debug.WriteLine("[LoadQuyDinhTyLeAsync] Lưới _txtGrid chưa khởi tạo hoặc sai số cột.");
                return;
            }

            try
            {
                await using var conn = new SqliteConnection($"Data Source={_csdl2Path};Mode=ReadWriteCreate");
                await conn.OpenAsync(cancellationToken);

                // 🌟 GỘP TẠO BẢNG + ĐỌC DỮ LIỆU TRONG 1 TRANSACTION DUY NHẤT
                // -> Đảm bảo tính toàn vẹn: nếu lỗi giữa chừng, không để lại trạng thái nửa vời
                await using var transaction = (SqliteTransaction)await conn.BeginTransactionAsync(cancellationToken);

                try
                {
                    await TaoBangQuyDinhTyLeNeuChuaCoAsync(conn, transaction, tableName, cancellationToken);

                    int soHang = _txtGrid.GetLength(0);
                    var duLieuDoc = await DocDuLieuTyLeAsync(conn, transaction, tableName, soHang, cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    // ⭐ CHỈ CẬP NHẬT UI SAU KHI ĐỌC + COMMIT THÀNH CÔNG HOÀN TOÀN
                    ApDungDuLieuTyLeLenGiaoDien(duLieuDoc, soHang);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[LoadQuyDinhTyLeAsync] Thao tác bị hủy.");
            }
            catch (SqliteException sqlEx)
            {
                Debug.WriteLine("[LoadQuyDinhTyLeAsync] Lỗi SQLite: " + sqlEx);
                MessageBox.Show("Lỗi truy vấn cơ sở dữ liệu tỷ lệ!\n" + sqlEx.Message,
                    "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[LoadQuyDinhTyLeAsync] Lỗi không xác định: " + ex);
                MessageBox.Show("Lỗi khi load dữ liệu tỷ lệ!\n" + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Tạo bảng quy định tỷ lệ nếu chưa tồn tại, và chèn 3 dòng mặc định (bỏ qua nếu đã có).
        /// Dùng tham số hóa hoàn toàn cho phần giá trị để tránh lỗi cú pháp / SQL Injection.
        /// </summary>
        private static async Task TaoBangQuyDinhTyLeNeuChuaCoAsync(
            SqliteConnection conn, SqliteTransaction transaction, string tableName, CancellationToken cancellationToken)
        {
            using var cmdCreate = conn.CreateCommand();
            cmdCreate.Transaction = transaction;
            cmdCreate.CommandText = $@"
CREATE TABLE IF NOT EXISTS [{tableName}] (
    ID INTEGER NOT NULL,
    TenLoaiTapThe TEXT,
    Loai_1 TEXT,
    Loai_2 TEXT,
    Loai_3 TEXT,
    Loai_4 TEXT,
    Khong_PL TEXT,
    PRIMARY KEY(ID AUTOINCREMENT)
);
INSERT OR IGNORE INTO [{tableName}] (ID, TenLoaiTapThe, Loai_1, Loai_2, Loai_3, Loai_4, Khong_PL)
VALUES
(1, @tenLoai1, '0', '0', '0', '0', '0'),
(2, @tenLoai2, '0', '0', '0', '0', '0'),
(3, @tenLoai3, '0', '0', '0', '0', '0');";

            cmdCreate.Parameters.AddWithValue("@tenLoai1", Module_HeThong.Loai_1);
            cmdCreate.Parameters.AddWithValue("@tenLoai2", Module_HeThong.Loai_2);
            cmdCreate.Parameters.AddWithValue("@tenLoai3", Module_HeThong.Loai_3);

            await cmdCreate.ExecuteNonQueryAsync(cancellationToken);
        }

        /// <summary>
        /// Đọc toàn bộ dữ liệu tỷ lệ cho các ID từ 1..soHang bằng DUY NHẤT 1 câu truy vấn
        /// (tránh vấn đề N+1 query gây chậm khi soHang lớn).
        /// Dùng tên cột (GetOrdinal) thay vì vị trí số, tránh lỗi ngầm nếu đổi thứ tự SELECT sau này.
        /// </summary>
        /// <returns>Dictionary: ID -> mảng giá trị theo đúng thứ tự CotDuLieuTyLe.</returns>
        private static async Task<Dictionary<int, int[]>> DocDuLieuTyLeAsync(
            SqliteConnection conn, SqliteTransaction transaction, string tableName, int soHang,
            CancellationToken cancellationToken)
        {
            var ketQua = new Dictionary<int, int[]>(soHang);

            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = $@"
SELECT ID, Loai_1, Loai_2, Loai_3, Loai_4, Khong_PL
FROM [{tableName}]
WHERE ID BETWEEN 1 AND @soHang";
            cmd.Parameters.AddWithValue("@soHang", soHang);

            using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

            int idxId = reader.GetOrdinal("ID");
            int[] idxCot = CotDuLieuTyLe.Select(reader.GetOrdinal).ToArray();

            while (await reader.ReadAsync(cancellationToken))
            {
                int id = reader.GetInt32(idxId);
                var values = new int[CotDuLieuTyLe.Length];

                for (int i = 0; i < idxCot.Length; i++)
                {
                    string valStr = reader.IsDBNull(idxCot[i]) ? "0" : reader.GetString(idxCot[i]);
                    values[i] = int.TryParse(valStr, out int parsed) ? parsed : 0;
                }

                ketQua[id] = values;
            }

            return ketQua;
        }

        /// <summary>
        /// Ghi dữ liệu đã đọc lên lưới _txtGrid và cập nhật DeNghiMapping.
        /// Với các ID không có trong CSDL (dữ liệu thiếu), điền mặc định toàn 0 để tránh lỗi NullReference.
        /// </summary>
        private void ApDungDuLieuTyLeLenGiaoDien(Dictionary<int, int[]> duLieuDoc, int soHang)
        {
            DeNghiMapping.Clear();

            for (int id = 1; id <= soHang; id++)
            {
                int[] values = duLieuDoc.TryGetValue(id, out var gia_tri)
                    ? gia_tri
                    : new int[CotDuLieuTyLe.Length]; // Mặc định toàn 0 nếu thiếu dòng dữ liệu

                for (int i = 0; i < values.Length; i++)
                {
                    _txtGrid[id - 1, i].Text = values[i].ToString();
                }

                DeNghiMapping[$"Loại {id}"] = values;
            }
        }

        /// Hàm Save dữ liệu từ TextBox về SQLite, chuẩn async + transaction + tối ưu
        // Truyền CancellationToken vào hàm
        private async Task SaveQuyDinhTyLeAsync(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path)) return;
            using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
            await conn.OpenAsync(ct);
            using var tran = conn.BeginTransaction();
            try
            {
                int rows = _txtGrid.GetLength(0);
                int cols = _txtGrid.GetLength(1);
                string tableName = TenBangHienTai;
                var tempMapping = new Dictionary<string, int[]>();
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tran;
                cmd.CommandText = $@"UPDATE [{tableName}] 
                            SET Loai_1=@l1, Loai_2=@l2, Loai_3=@l3,
                                Loai_4=@l4, Khong_PL=@kpl
                            WHERE ID=@id";
                // Sử dụng SqliteType.Text để đồng bộ 100% với cấu trúc bảng SQLite 
                cmd.Parameters.Add("@l1", SqliteType.Text);
                cmd.Parameters.Add("@l2", SqliteType.Text);
                cmd.Parameters.Add("@l3", SqliteType.Text);
                cmd.Parameters.Add("@l4", SqliteType.Text);
                cmd.Parameters.Add("@kpl", SqliteType.Text);
                cmd.Parameters.Add("@id", SqliteType.Integer);
                const int COL_LOAI1 = 0, COL_LOAI2 = 1, COL_LOAI3 = 2, COL_LOAI4 = 3, COL_KHONGPL = 4;
                for (int id = 1; id <= rows; id++)
                {
                    ct.ThrowIfCancellationRequested();
                    int[] values = new int[cols];
                    for (int i = 0; i < cols; i++)
                    {
                        if (int.TryParse(_txtGrid[id - 1, i].Text, out int val))
                        {
                            values[i] = Math.Clamp(val, 0, 100);
                        }
                        else
                        {
                            values[i] = 0;
                        }
                    }
                    // Ép kiểu chuỗi để tương thích với Type "TEXT" trong Database
                    cmd.Parameters["@l1"].Value = values[COL_LOAI1].ToString();
                    cmd.Parameters["@l2"].Value = values[COL_LOAI2].ToString();
                    cmd.Parameters["@l3"].Value = values[COL_LOAI3].ToString();
                    cmd.Parameters["@l4"].Value = values[COL_LOAI4].ToString();
                    cmd.Parameters["@kpl"].Value = values[COL_KHONGPL].ToString();
                    cmd.Parameters["@id"].Value = id;
                    await cmd.ExecuteNonQueryAsync(ct);
                    tempMapping[$"Loại {id}"] = values;
                }
                tran.Commit();
                DeNghiMapping = tempMapping;
            }
            catch (OperationCanceledException)
            {
                tran.Rollback();
            }
            catch (Exception ex)
            {
                tran.Rollback();
                string logPath = Path.Combine(Application.StartupPath, "LoiHeThong.txt");
                string noiDungLoi = $"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] Lỗi lưu E29: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}";
                try
                {
                    lock (_logLock)
                    {
                        File.AppendAllText(logPath, noiDungLoi);
                    }
                }
                catch { /* Bỏ qua nếu mất quyền truy cập file */ }
                throw;
            }
        }
        private async void kryptonButton_LuuTyLeBCHE09_Click(object? sender, EventArgs e)
        {
            if (!kryptonButton_LuuTyLeBCHE09.Enabled || IsDisposed || Disposing)
                return;
            // Hủy và dọn dẹp CancellationTokenSource cũ nếu có
            _ctsLuuDuLieu?.Cancel();
            _ctsLuuDuLieu?.Dispose();
            _ctsLuuDuLieu = new CancellationTokenSource();
            var token = _ctsLuuDuLieu.Token;
            string textBanDau = kryptonButton_LuuTyLeBCHE09.Values.Text;
            Image? anhBanDau = kryptonButton_LuuTyLeBCHE09.Values.Image;
            try
            {
                // 1. Cập nhật Trạng thái UI (Lock Control)
                kryptonButton_LuuTyLeBCHE09.Enabled = false;
                kryptonButton_LuuTyLeBCHE09.Values.Text = "Đang lưu...";
                kryptonButton_LuuTyLeBCHE09.Values.Image = null;
                HienThiThongBao("Hệ thống đang thực hiện lưu quy định tỷ lệ...", Color.Black);
                // 2. Thực thi Lưu CSDL bất đồng bộ
                await SaveQuyDinhTyLeAsync(token);
                token.ThrowIfCancellationRequested();
                if (IsDisposed || Disposing)
                    return;
                // 3. Cập nhật Memory Cache & Phát Event hệ thống
                Module_QuyDinhTyLe.ReloadData();
                OnQuyDinhChanged?.Invoke();
                const string thongBao = "✔ Đã lưu quy định tỷ lệ thành công!";
                HienThiThongBao(thongBao, Color.DarkGreen);
                Module_NhatKy.GhiNhatKy(
                 taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                 hanhDong: thongBao,
                 ghiChu: "Thành công");
                // 5. Delay 300ms trải nghiệm người dùng (UX) trước khi đóng Form
                await Task.Delay(300, token);
                if (!IsDisposed && !Disposing)
                {
                    Close();
                }
            }
            catch (OperationCanceledException)
            {
                // Luồng bị hủy chủ động - Không xử lý UI Exception
            }
            catch (Exception ex)
            {
                if (IsDisposed || Disposing)
                    return;
                HienThiThongBao("✘ Lỗi lưu quy định tỷ lệ!", Color.Red);
                MessageBox.Show(
                    $"Đã xảy ra lỗi khi lưu dữ liệu vào CSDL:\n\n{ex.Message}",
                    "Lỗi hệ thống",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                // Khôi phục trạng thái Nút bấm nếu Form vẫn còn sống (Trường hợp lưu lỗi)
                if (!IsDisposed && !Disposing && kryptonButton_LuuTyLeBCHE09 != null && !kryptonButton_LuuTyLeBCHE09.IsDisposed)
                {
                    kryptonButton_LuuTyLeBCHE09.Values.Text = textBanDau;
                    kryptonButton_LuuTyLeBCHE09.Values.Image = anhBanDau;
                    kryptonButton_LuuTyLeBCHE09.Enabled = true;
                }
            }
        }
        private void DoiTenGroupBox()
        {
            try
            {
                using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                conn.Open();
                string sql = @"SELECT TenTrungDoan, textBox1_TenTrungDoanDong1 
                       FROM ThongTin LIMIT 1";
                using var cmd = new SqliteCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    string tenTrungDoan = Module_BaoMatAES.GiaiMa(reader["TenTrungDoan"]?.ToString() ?? "");
                    string dong1 = Module_BaoMatAES.GiaiMa(reader["textBox1_TenTrungDoanDong1"]?.ToString() ?? "");
                    string tenDayDu = $"{dong1} {tenTrungDoan}".Trim();
                    //groupBox_TyLeTheoQuyDinh.Text = $"{tenDayDu} QUY ĐỊNH";
                    groupBox_TyLeTheoQuyDinh.Text =
    TenBangHienTai == "QuyDinhTyLe_TanBinh"
        ? $"{tenDayDu} QUY ĐỊNH - TỶ LỆ THI ĐUA TÂN BINH"
        : $"{tenDayDu} QUY ĐỊNH";
                }
                // định dạng chữ
                groupBox_TyLeTheoQuyDinh.ForeColor = Color.Red;
                groupBox_TyLeTheoQuyDinh.Font = new Font(
                    groupBox_TyLeTheoQuyDinh.Font,
                    FontStyle.Bold | FontStyle.Italic
                );
            }
            catch
            {
                groupBox_TyLeTheoQuyDinh.Text = "* Tỷ lệ theo quy định";
            }
        }
        private void TextBox_Enter(object? sender, EventArgs e)
        {
            if (sender is not KryptonTextBox tb) return;
            tb.StateCommon.Border.DrawBorders = PaletteDrawBorders.All;
            tb.StateCommon.Border.Rounding = 4; // bo góc nhẹ nhìn chuyên nghiệp
            tb.StateCommon.Border.Width = 2;
            tb.StateCommon.Border.Color1 = _focusColor;
        }
        private void TextBox_Leave(object? sender, EventArgs e)
        {
            if (sender is not KryptonTextBox tb) return;
            tb.StateCommon.Border.Width = 1;
            tb.StateCommon.Border.Color1 = _normalColor;
        }
        private void GanSuKienFocusTextBox()
        {
            foreach (Control ctrl in this.Controls)
            {
                GanDeQuyTextBox(ctrl);
            }
        }
        private void GanDeQuyTextBox(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                if (ctrl is KryptonTextBox tb)
                {
                    tb.Enter += TextBox_Enter;
                    tb.Leave += TextBox_Leave;
                }
                if (ctrl.HasChildren)
                    GanDeQuyTextBox(ctrl);
            }
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Báo hiệu hủy mọi Task đang chạy dùng token này
            _ctsLuuDuLieu?.Cancel();
            _ctsLuuDuLieu?.Dispose();
            _timerThongBao?.Stop();
            _timerThongBao?.Dispose();
            base.OnFormClosed(e);
        }
    }
}