//Phiên bản 3]
using Microsoft.Data.Sqlite;

namespace PhanMemThiDua2026
{
    public partial class Form16_LamMoi : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private bool _isProcessing = false;
        private bool _isClosingRequested = false;
        private CancellationTokenSource _cts;
        // 1. TỐI ƯU UI: Cache control để tránh reflection lookup (Controls.Find) tốn CPU nếu chạy dài hạn
        private Label _lblTrangThai;
        // ⭐ CHUẨN ERP: Ghi nhớ trạng thái trong một phiên làm việc (Session - RAM)
        private static bool _daXacNhanResetTrongSession = false;
        private static bool _memLoai1 = false;
        private static bool _memLoai3 = false;
        private static bool _memLoai4 = false;
        private static bool _memKhongPL = false;

        public Form16_LamMoi()
        {
            InitializeComponent();

            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.ShowInTaskbar = false;
            this.Load += Form16_LamMoi_Load;
            this.FormClosing += Form16_LamMoi_FormClosing;
            InitCheckBoxEvents();
            InitToolTips();
        }

        private void Form16_LamMoi_Load(object sender, EventArgs e)
        {
            // Tìm và cache Label trạng thái ngay từ đầu
            _lblTrangThai = Controls.Find("label_TrangThai", true).FirstOrDefault() as Label;
            // Phục hồi trạng thái từ RAM
            checkBox1_GuiNguyenLoai1.Checked = _memLoai1;
            checkBox1_GuiNguyenLoai3.Checked = _memLoai3;
            checkBox1_GuiNguyenLoai4.Checked = _memLoai4;
            checkBox1_GuiNguyenKhongPhanLoai.Checked = _memKhongPL;
            // Loại 2 luôn được set mặc định và không cho phép thay đổi
            checkBox1_GuiNguyenLoai2.AutoCheck = false;
            checkBox1_GuiNguyenLoai2.Checked = true;
            UpdateLabelColors();
            SetStatus("", this.ForeColor);
        }

        private void Form16_LamMoi_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isProcessing)
            {
                e.Cancel = true;
                if (!_isClosingRequested)
                {
                    _isClosingRequested = true;
                    SetStatus("Đang thoát an toàn...", Color.Red);
                    _cts?.Cancel();
                }
            }
        }

        #region UI Helpers
        private void SetStatus(string message, Color color)
        {
            btn_ResetPhanLoai.Text = string.IsNullOrEmpty(message) ? "Thực hiện" : message;
            // Sử dụng control đã cache thay vì tìm kiếm lại
            if (_lblTrangThai != null)
            {
                _lblTrangThai.ForeColor = color;
                _lblTrangThai.Text = message;
            }
        }
        #endregion
        private async void btn_ResetPhanLoai_Click(object sender, EventArgs e)
        {
            if (_isProcessing) return;

            if (!_daXacNhanResetTrongSession)
            {
                var confirm = MessageBox.Show(
                    "Đặt lại toàn bộ phân loại về [Loại 2]?\n\n" +
                    "Chỉ yêu cầu một lần trong phiên làm việc.",
                    "Xác nhận",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);

                if (confirm != DialogResult.Yes) return;
                _daXacNhanResetTrongSession = true;
            }

            _cts = new CancellationTokenSource();
            var progress = new Progress<int>(percent =>
            {
                if (!_isClosingRequested) btn_ResetPhanLoai.Text = $"Đang xử lý: {percent}%";
            });
            try
            {
                _isProcessing = true;
                btn_ResetPhanLoai.Enabled = false;
                this.Cursor = Cursors.WaitCursor;

                var keepList = GetKeepList();
                var (soDong, tbLoi) = await Task.Run(() => ThucThiResetDuLieu(keepList, progress, _cts.Token));

                if (tbLoi == "CANCELED") return;

                if (!string.IsNullOrEmpty(tbLoi))
                {
                    MessageBox.Show("Có lỗi trong quá trình cập nhật. Vui lòng kiểm tra lại kết nối cơ sở dữ liệu.\nChi tiết: " + tbLoi,
                                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                // GHI NHẬT KÝ & REFRESH UI
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Reset Phân Loại", $"Số lượng: {soDong}");

                if (Form6_XuLyData.Instance != null && !Form6_XuLyData.Instance.IsDisposed)
                {
                    Form6_XuLyData.Instance.BeginInvoke(new Action(() => Form6_XuLyData.Instance.RefreshCSDL()));
                }

                btn_ResetPhanLoai.BackColor = Color.ForestGreen;
                btn_ResetPhanLoai.ForeColor = Color.White;
                SetStatus($"Hoàn tất! ({soDong} CBCS)", Color.ForestGreen);

                await Task.Delay(700);
                this.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex}");
                MessageBox.Show("Đã xảy ra lỗi không xác định. Thao tác bị hủy.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isProcessing = false;
                // 2. CHUẨN ENTERPRISE: Giải phóng CancellationTokenSource triệt để, chống leak Handle dài hạn
                if (_cts != null)
                {
                    _cts.Dispose();
                    _cts = null;
                }

                if (_isClosingRequested) this.Close();
                else if (!this.IsDisposed)
                {
                    btn_ResetPhanLoai.Enabled = true;
                    btn_ResetPhanLoai.BackColor = SystemColors.Control;
                    btn_ResetPhanLoai.ForeColor = SystemColors.ControlText;
                    this.Cursor = Cursors.Default;
                }
            }
        }

        private (int soDongDaCapNhat, string thongBaoLoi) ThucThiResetDuLieu(HashSet<string> keepList, IProgress<int> progress, CancellationToken token)
        {
            // Thay rowid bằng ID thật (Primary Key)
            var idsToUpdate = new List<long>();
            try
            {
                var builder = new SqliteConnectionStringBuilder { DataSource = _csdl2Path, DefaultTimeout = 30 };
                using var conn = new SqliteConnection(builder.ConnectionString);
                conn.Open();

                using (var pragmaCmd = conn.CreateCommand())
                {
                    // 3. AN TOÀN DỮ LIỆU: Kiểm tra toàn vẹn nhẹ trước khi chạy và đảm bảo WAL mode
                    pragmaCmd.CommandText = "PRAGMA quick_check; PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;";
                    pragmaCmd.ExecuteNonQuery();
                }

                // PHASE 1: Thu thập ID dựa trên Khóa chính (Tránh rủi ro đổi rowid khi VACUUM DB)
                using (var cmdSelect = conn.CreateCommand())
                {
                    // LƯU Ý: Đảm bảo bảng DanhSach của bạn có cột [ID] làm Khóa Chính (Primary Key). 
                    // Nếu cột tên khác (ví dụ: MaCBCS), hãy sửa lại chữ ID ở câu SQL dưới.
                    cmdSelect.CommandText = "SELECT ID, PhanLoai FROM DanhSach WHERE PhanLoai IS NOT NULL AND PhanLoai <> ''";
                    using var reader = cmdSelect.ExecuteReader();
                    while (reader.Read())
                    {
                        token.ThrowIfCancellationRequested();
                        long dbId = reader.GetInt64(0);
                        string phanLoaiDbMaHoa = reader.GetString(1);

                        string phanLoaiGoc = "";
                        try { phanLoaiGoc = BaoMatAES.GiaiMa(phanLoaiDbMaHoa)?.Trim() ?? ""; }
                        catch { phanLoaiGoc = ""; }

                        string checkVal = string.IsNullOrEmpty(phanLoaiGoc) ? "Không PL" : phanLoaiGoc;
                        if (!keepList.Contains(checkVal)) idsToUpdate.Add(dbId);
                    }
                }

                int total = idsToUpdate.Count;
                if (total == 0) return (0, string.Empty);

                // PHASE 2: Tối ưu 10 năm với Prepared Statement + Retry Logic
                string loai2Encrypted = BaoMatAES.MaHoa("Loại 2");
                int rowsAffected = 0;

                using var transaction = conn.BeginTransaction();
                try
                {
                    using var cmdUpdate = conn.CreateCommand();
                    cmdUpdate.Transaction = transaction;

                    // Command tĩnh, compile đúng 1 lần
                    cmdUpdate.CommandText = "UPDATE DanhSach SET PhanLoai = @pl WHERE ID = @id";

                    cmdUpdate.Parameters.AddWithValue("@pl", loai2Encrypted);
                    var idParam = cmdUpdate.Parameters.Add("@id", SqliteType.Integer);

                    int reportInterval = Math.Max(1, total / 100);

                    for (int i = 0; i < total; i++)
                    {
                        token.ThrowIfCancellationRequested();
                        idParam.Value = idsToUpdate[i];

                        // 4. CHỐNG CRASH LUỒNG GHI: Retry 3 lần nếu gặp SQLITE_BUSY (Mã lỗi 5)
                        int maxRetries = 3;
                        for (int retry = 0; retry < maxRetries; retry++)
                        {
                            try
                            {
                                rowsAffected += cmdUpdate.ExecuteNonQuery();
                                break; // Thành công thì thoát vòng lặp retry
                            }
                            catch (SqliteException ex) when (ex.SqliteErrorCode == 5)
                            {
                                if (retry == maxRetries - 1) throw; // Nếu đã thử 3 lần vẫn lỗi thì throw
                                Thread.Sleep(200); // Ngủ 200ms chờ DB nhả lock (do Antivirus, thao tác ngầm...)
                            }
                        }

                        if (i % reportInterval == 0 || i == total - 1)
                        {
                            progress?.Report((i + 1) * 100 / total);
                        }
                    }
                    transaction.Commit();
                }
                catch { transaction.Rollback(); throw; }

                // 5. BẢO TRÌ TỰ ĐỘNG: Checkpoint TRUNCATE để dọn dẹp không cho file .db-wal phình to
                using (var checkpointCmd = conn.CreateCommand())
                {
                    checkpointCmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                    checkpointCmd.ExecuteNonQuery();
                }

                return (rowsAffected, string.Empty);
            }
            catch (OperationCanceledException) { return (0, "CANCELED"); }
            catch (Exception ex) { return (0, ex.Message); }
        }

        #region Events & Init
        private void InitCheckBoxEvents()
        {
            // Cập nhật giao diện và đồng thời lưu biến tĩnh vào RAM ngay khi user click
            checkBox1_GuiNguyenLoai1.CheckedChanged += (s, e) => { UpdateLabelColors(); _memLoai1 = checkBox1_GuiNguyenLoai1.Checked; };
            checkBox1_GuiNguyenLoai2.CheckedChanged += (s, e) => { UpdateLabelColors(); }; // Không cần lưu Loại 2 vì nó mặc định
            checkBox1_GuiNguyenLoai3.CheckedChanged += (s, e) => { UpdateLabelColors(); _memLoai3 = checkBox1_GuiNguyenLoai3.Checked; };
            checkBox1_GuiNguyenLoai4.CheckedChanged += (s, e) => { UpdateLabelColors(); _memLoai4 = checkBox1_GuiNguyenLoai4.Checked; };
            checkBox1_GuiNguyenKhongPhanLoai.CheckedChanged += (s, e) => { UpdateLabelColors(); _memKhongPL = checkBox1_GuiNguyenKhongPhanLoai.Checked; };
        }

        private void UpdateLabelColors()
        {
            label1.ForeColor = checkBox1_GuiNguyenLoai1.Checked ? Color.Red : Color.Blue;
            label2.ForeColor = checkBox1_GuiNguyenLoai2.Checked ? Color.Red : Color.Blue;
            label3.ForeColor = checkBox1_GuiNguyenLoai3.Checked ? Color.Red : Color.Blue;
            label4.ForeColor = checkBox1_GuiNguyenLoai4.Checked ? Color.Red : Color.Blue;
            label5.ForeColor = checkBox1_GuiNguyenKhongPhanLoai.Checked ? Color.Red : Color.Blue;
        }

        private HashSet<string> GetKeepList()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (checkBox1_GuiNguyenLoai1.Checked) set.Add("Loại 1");
            if (checkBox1_GuiNguyenLoai2.Checked) set.Add("Loại 2");
            if (checkBox1_GuiNguyenLoai3.Checked) set.Add("Loại 3");
            if (checkBox1_GuiNguyenLoai4.Checked) set.Add("Loại 4");
            if (checkBox1_GuiNguyenKhongPhanLoai.Checked) set.Add("Không PL");
            return set;
        }

        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Hướng dẫn";
            var tips = new Dictionary<Control, string>
            {
                { checkBox1_GuiNguyenLoai1, "Tích chọn để không thay đổi những người đã đạt Loại 1" },
                { btn_ResetPhanLoai, "Nhấn để bắt đầu quá trình làm mới toàn bộ danh sách" }
            };
            foreach (var tip in tips) if (tip.Key != null) toolTip1.SetToolTip(tip.Key, tip.Value);
        }
        #endregion
    }
}
