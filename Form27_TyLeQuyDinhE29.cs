using Krypton.Toolkit;
using Microsoft.Data.Sqlite;

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
        private async void Form27_TyLeQuyDinhE29_Load(object sender, EventArgs e)
        {
            InitTextBoxGrid(); // khởi tạo mảng TextBox trước
            await LoadQuyDinhTyLeAsync(); // gọi hàm async
            GanSuKienFocusTextBox();
            DoiTenGroupBox();
            InitToolTips();
            SetupStatusStrip();
        }
        // Hàm Load dữ liệu SQLite lên TextBox và mapping Dictionary
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
            toolTip1.ToolTipTitle = "Thao tác";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;

            // Gán ToolTip
            if (kryptonButton_LuuE29 != null)
            {
                toolTip1.SetToolTip(kryptonButton_LuuE29, "Lưu quy định tỷ lệ vào cơ sở dữ liệu");
            }
        }
        private async Task LoadQuyDinhTyLeAsync()
        {
            if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path)) return;

            try
            {
                using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                await conn.OpenAsync();

                DeNghiMapping.Clear();

                int rows = _txtGrid.GetLength(0);
                int cols = _txtGrid.GetLength(1);

                for (int id = 1; id <= rows; id++)
                {
                    int[] values = new int[cols];

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"SELECT Loai_1, Loai_2, Loai_3, Loai_4, Khong_PL 
                                FROM QuyDinhTyLe WHERE ID=@id";
                    cmd.Parameters.AddWithValue("@id", id);

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        for (int i = 0; i < cols; i++)
                        {
                            int val = reader.IsDBNull(i) ? 0 : reader.GetInt32(i);
                            values[i] = val;
                            _txtGrid[id - 1, i].Text = val.ToString();
                        }
                    }

                    DeNghiMapping[$"Loại {id}"] = values;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi load dữ liệu!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine(ex); // Có thể ghi log ra file
            }
        }
        // Hàm Save dữ liệu từ TextBox về SQLite, chuẩn async + transaction + tối ưu
        // Truyền CancellationToken vào hàm
        private async Task SaveQuyDinhTyLeAsync(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path)) return;

            using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
            await conn.OpenAsync(ct); // Truyền token vào kết nối

            using var tran = conn.BeginTransaction();
            try
            {
                int rows = _txtGrid.GetLength(0);
                int cols = _txtGrid.GetLength(1);

                var tempMapping = new Dictionary<string, int[]>();

                using var cmd = conn.CreateCommand();
                cmd.Transaction = tran;
                cmd.CommandText = @"UPDATE QuyDinhTyLe 
                            SET Loai_1=@l1, Loai_2=@l2, Loai_3=@l3,
                                Loai_4=@l4, Khong_PL=@kpl
                            WHERE ID=@id";

                cmd.Parameters.Add("@l1", SqliteType.Integer);
                cmd.Parameters.Add("@l2", SqliteType.Integer);
                cmd.Parameters.Add("@l3", SqliteType.Integer);
                cmd.Parameters.Add("@l4", SqliteType.Integer);
                cmd.Parameters.Add("@kpl", SqliteType.Integer);
                cmd.Parameters.Add("@id", SqliteType.Integer);

                // NÂNG CẤP: Định nghĩa Hằng số để tránh Hardcode index (Magic Numbers)
                const int COL_LOAI1 = 0;
                const int COL_LOAI2 = 1;
                const int COL_LOAI3 = 2;
                const int COL_LOAI4 = 3;
                const int COL_KHONGPL = 4;

                for (int id = 1; id <= rows; id++)
                {
                    // Kiểm tra xem có lệnh hủy từ user không trước khi chạy mỗi vòng lặp
                    ct.ThrowIfCancellationRequested();

                    int[] values = new int[cols];
                    for (int i = 0; i < cols; i++)
                    {
                        // NÂNG CẤP: Validate dữ liệu đầu vào (Chống số âm và giới hạn 100 nếu là phần trăm)
                        // Nếu quy định tỷ lệ của bạn là % (từ 0 đến 100):
                        if (int.TryParse(_txtGrid[id - 1, i].Text, out int val))
                        {
                            values[i] = Math.Clamp(val, 0, 100); // Ép giới hạn: nhỏ hơn 0 thành 0, lớn hơn 100 thành 100
                        }
                        else
                        {
                            values[i] = 0;
                        }
                    }

                    // Gán giá trị Parameters bằng hằng số
                    cmd.Parameters["@l1"].Value = values[COL_LOAI1];
                    cmd.Parameters["@l2"].Value = values[COL_LOAI2];
                    cmd.Parameters["@l3"].Value = values[COL_LOAI3];
                    cmd.Parameters["@l4"].Value = values[COL_LOAI4];
                    cmd.Parameters["@kpl"].Value = values[COL_KHONGPL];
                    cmd.Parameters["@id"].Value = id;

                    await cmd.ExecuteNonQueryAsync(ct); // Truyền token vào truy vấn
                    tempMapping[$"Loại {id}"] = values;
                }

                tran.Commit();
                DeNghiMapping = tempMapping;
            }
            catch (OperationCanceledException)
            {
                // Bắt lỗi riêng nếu user hủy (đóng form)
                tran.Rollback();
                // Không cần báo lỗi ầm ĩ nếu chính user là người đóng form
            }
            catch (Exception ex)
            {
                tran.Rollback();

                // NÂNG CẤP: Chống đụng độ luồng (Race Condition) khi ghi log
                string logPath = Path.Combine(Application.StartupPath, "LoiHeThong.txt");
                string noiDungLoi = $"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] Lỗi lưu E29: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}";

                try
                {
                    lock (_logLock) // Xếp hàng nếu có nhiều luồng cùng báo lỗi 1 lúc
                    {
                        File.AppendAllText(logPath, noiDungLoi);
                    }
                }
                catch { /* Bỏ qua nếu mất quyền truy cập file */ }

                throw;
            }
        }
        private async void kryptonButton_LuuE29_Click(object sender, EventArgs e)
        {
            if (!kryptonButton_LuuE29.Enabled) return;

            // Khởi tạo mới Token Source cho lần bấm này
            _ctsLuuDuLieu?.Dispose();
            _ctsLuuDuLieu = new CancellationTokenSource();
            var token = _ctsLuuDuLieu.Token;

            string textBanDau = kryptonButton_LuuE29.Values.Text;
            Image anhBanDau = kryptonButton_LuuE29.Values.Image;

            try
            {
                kryptonButton_LuuE29.Enabled = false;
                kryptonButton_LuuE29.Values.Text = "Đang lưu...";
                kryptonButton_LuuE29.Values.Image = null;

                HienThiThongBao("Hệ thống đang thực hiện lưu quy định tỷ lệ...", Color.Black);

                await Task.Delay(250, token); // Bỏ token vào Delay để có thể ngắt ngay lập tức

                // Truyền token vào hàm Save
                await SaveQuyDinhTyLeAsync(token);

                Module_QuyDinhTyLe.Reload();
                OnQuyDinhChanged?.Invoke();

                HienThiThongBao("✔ Đã lưu quy định tỷ lệ thành công!", Color.DarkGreen);
            }
            catch (OperationCanceledException)
            {
                // Form bị đóng ngang, luồng bị hủy an toàn, không cần làm gì thêm.
            }
            catch (Exception ex)
            {
                HienThiThongBao("✘ Lỗi lưu quy định tỷ lệ!", Color.Red);
                MessageBox.Show("Đã xảy ra lỗi khi lưu dữ liệu vào CSDL:\n\n" + ex.Message,
                                "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!this.IsDisposed) // Chỉ phục hồi nút nếu Form chưa bị hủy
                {
                    kryptonButton_LuuE29.Values.Text = textBanDau;
                    kryptonButton_LuuE29.Values.Image = anhBanDau;
                    kryptonButton_LuuE29.Enabled = true;
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
                    string tenTrungDoan = BaoMatAES.GiaiMa(reader["TenTrungDoan"]?.ToString() ?? "");
                    string dong1 = BaoMatAES.GiaiMa(reader["textBox1_TenTrungDoanDong1"]?.ToString() ?? "");

                    string tenDayDu = $"{dong1} {tenTrungDoan}".Trim();

                    groupBox_TyLeTheoQuyDinh.Text = $"{tenDayDu} QUY ĐỊNH";
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
        private void TextBox_Enter(object sender, EventArgs e)
        {
            if (sender is not KryptonTextBox tb) return;

            tb.StateCommon.Border.DrawBorders = PaletteDrawBorders.All;
            tb.StateCommon.Border.Rounding = 4; // bo góc nhẹ nhìn chuyên nghiệp
            tb.StateCommon.Border.Width = 2;
            tb.StateCommon.Border.Color1 = _focusColor;
        }
        private void TextBox_Leave(object sender, EventArgs e)
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