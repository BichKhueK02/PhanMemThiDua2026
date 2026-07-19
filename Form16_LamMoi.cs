using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.Sqlite;
using System.Text;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

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
            // Bật tính năng Click 1 chạm chuẩn của WinForms
            checkedListBox1_ChonDonViDeReset.CheckOnClick = true;
            // ⭐ BỔ SUNG CODE KÍCH HOẠT CHẾ ĐỘ ĐỔI MÀU ĐỘNG CHO CHECKEDLISTBOX
            //// ==============================================================
            //checkedListBox1_ChonDonViDeReset.DrawMode = DrawMode.OwnerDrawFixed;
            //// 👇 THÊM ĐÚNG DÒNG NÀY VÀO ĐÂY:
            //checkedListBox1_ChonDonViDeReset.ItemHeight = 22;

            //checkedListBox1_ChonDonViDeReset.DrawItem += CheckedListBox1_DrawItem;

            // Ép hệ thống vẽ lại màu sắc NGAY LẬP TỨC khi tích/bỏ tích
            checkedListBox1_ChonDonViDeReset.ItemCheck += (s, e) =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    if (!checkedListBox1_ChonDonViDeReset.IsDisposed)
                        checkedListBox1_ChonDonViDeReset.Invalidate();
                }));
            };
            // ==============================================================

            this.Load += Form16_LamMoi_Load;
            this.FormClosing += Form16_LamMoi_FormClosing;
            InitCheckBoxEvents();
            InitToolTips();
        }
        private async void Form16_LamMoi_Load(object sender, EventArgs e)
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
            // ⭐ GỌI HÀM NẠP DANH SÁCH ĐƠN VỊ KHI MỞ FORM
            await LoadDanhSachDonViAsync();
        }
        /// <summary>
        /// Nạp danh sách Đơn vị từ CSDL, giải mã AES và Mặc định check TẤT CẢ
        /// </summary>
        private async Task LoadDanhSachDonViAsync()
        {
            var dsDonVi = new List<string>();
            try
            {
                using (var cn = new SqliteConnection($"Data Source={_csdl2Path}"))
                {
                    await cn.OpenAsync();
                    using (var cmd = cn.CreateCommand())
                    {
                        // Truy xuất từ bảng DanhSach_DonVi để lấy danh mục chuẩn
                        cmd.CommandText = "SELECT Ten_DonVi FROM DanhSach_DonVi ORDER BY ID ASC";
                        using (var rd = await cmd.ExecuteReaderAsync())
                        {
                            while (await rd.ReadAsync())
                            {
                                string tenRaw = rd["Ten_DonVi"]?.ToString()?.Trim() ?? "";
                                if (string.IsNullOrWhiteSpace(tenRaw)) continue;
                                try
                                {
                                    string tenDec = BaoMatAES.GiaiMa(tenRaw);
                                    if (!string.IsNullOrWhiteSpace(tenDec)) dsDonVi.Add(tenDec);
                                }
                                catch { } // Bỏ qua nếu lỗi giải mã
                            }
                        }
                    }
                }
                // Sắp xếp theo ưu tiên
                string[] thuTuUuTien = Module_DonVi.LayDanhSachDonViUuTienArray();
                var sortedList = dsDonVi.OrderBy(item =>
                {
                    int idx = Array.IndexOf(thuTuUuTien, item);
                    return idx == -1 ? int.MaxValue : idx;
                }).ToList();

                // 🟢 NẠP VÀO CHECKED LIST BOX
                checkedListBox1_ChonDonViDeReset.SelectedIndexChanged -= checkedListBox1_ChonDonViDeReset_SelectedIndexChanged;
                checkedListBox1_ChonDonViDeReset.BeginUpdate();
                checkedListBox1_ChonDonViDeReset.Items.Clear();

                foreach (var dv in sortedList)
                {
                    // Tham số 'true' giúp MẶC ĐỊNH TÍCH CHỌN tất cả các đơn vị
                    checkedListBox1_ChonDonViDeReset.Items.Add(dv, true);
                }
                checkedListBox1_ChonDonViDeReset.EndUpdate();
                checkedListBox1_ChonDonViDeReset.SelectedIndexChanged += checkedListBox1_ChonDonViDeReset_SelectedIndexChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi nạp đơn vị Form 16: {ex.Message}");
            }
        }
        /// <summary>
        /// UX MƯỢT MÀ: Chỉ xóa bôi xanh dòng để UI phẳng và tinh tế hơn.
        /// (Không tự ý đảo Check vì CheckOnClick = true đã làm việc đó rồi)
        /// </summary>
        private void checkedListBox1_ChonDonViDeReset_SelectedIndexChanged(object sender, EventArgs e)
        {
            checkedListBox1_ChonDonViDeReset.ClearSelected();
        }
        /// <summary>
        /// Hàm tự động vẽ màu sắc cho CheckedListBox: 
        /// Checked = Màu Xanh, Unchecked = Màu Đỏ
        /// </summary>   
        private void Form16_LamMoi_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isProcessing)
            {
                e.Cancel = true;
                if (!_isClosingRequested)
                {
                    _isClosingRequested = true;
                    SetStatus("Đang thoát an toàn...", System.Drawing.Color.Red);
                    _cts?.Cancel();
                }
            }
        }
        #region UI Helpers
        private void SetStatus(string message, System.Drawing.Color color)
        {
            btn_ResetPhanLoai.Text = string.IsNullOrEmpty(message) ? "Thực hiện" : message;
            if (_lblTrangThai != null)
            {
                _lblTrangThai.ForeColor = color;
                _lblTrangThai.Text = message;
            }
        }

        private void InitCheckBoxEvents()
        {
            checkBox1_GuiNguyenLoai1.CheckedChanged += (s, e) => { UpdateLabelColors(); _memLoai1 = checkBox1_GuiNguyenLoai1.Checked; };
            checkBox1_GuiNguyenLoai2.CheckedChanged += (s, e) => { UpdateLabelColors(); };
            checkBox1_GuiNguyenLoai3.CheckedChanged += (s, e) => { UpdateLabelColors(); _memLoai3 = checkBox1_GuiNguyenLoai3.Checked; };
            checkBox1_GuiNguyenLoai4.CheckedChanged += (s, e) => { UpdateLabelColors(); _memLoai4 = checkBox1_GuiNguyenLoai4.Checked; };
            checkBox1_GuiNguyenKhongPhanLoai.CheckedChanged += (s, e) => { UpdateLabelColors(); _memKhongPL = checkBox1_GuiNguyenKhongPhanLoai.Checked; };
        }

        private void UpdateLabelColors()
        {
            label1.ForeColor = checkBox1_GuiNguyenLoai1.Checked ? System.Drawing.Color.Red : System.Drawing.Color.Blue;
            label2.ForeColor = checkBox1_GuiNguyenLoai2.Checked ? System.Drawing.Color.Red : System.Drawing.Color.Blue;
            label3.ForeColor = checkBox1_GuiNguyenLoai3.Checked ? System.Drawing.Color.Red : System.Drawing.Color.Blue;
            label4.ForeColor = checkBox1_GuiNguyenLoai4.Checked ? System.Drawing.Color.Red : System.Drawing.Color.Blue;
            label5.ForeColor = checkBox1_GuiNguyenKhongPhanLoai.Checked ? System.Drawing.Color.Red : System.Drawing.Color.Blue;
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

            // ⭐ ĐÃ SỬA: Khai báo tường minh System.Windows.Forms.Control để tránh xung đột với OpenXml
            var tips = new Dictionary<System.Windows.Forms.Control, string>
    {
        { checkBox1_GuiNguyenLoai1, "Tích chọn để không thay đổi những người đã đạt Loại 1" },
        { btn_ResetPhanLoai, "Nhấn để bắt đầu quá trình làm mới toàn bộ danh sách" }
    };

            foreach (var tip in tips)
            {
                if (tip.Key != null)
                    toolTip1.SetToolTip(tip.Key, tip.Value);
            }
        }
        #endregion
        private async void btn_ResetPhanLoai_Click(object sender, EventArgs e)
        {
            if (_isProcessing) return;

            // 1. QUÉT GIAO DIỆN LẤY DANH SÁCH CHỌN VÀ BỎ QUA
            var danhSachChon = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var danhSachBoQua = new List<string>();

            for (int i = 0; i < checkedListBox1_ChonDonViDeReset.Items.Count; i++)
            {
                string dv = checkedListBox1_ChonDonViDeReset.Items[i].ToString().Trim();
                if (checkedListBox1_ChonDonViDeReset.GetItemChecked(i))
                {
                    danhSachChon.Add(dv);
                }
                else
                {
                    danhSachBoQua.Add(dv);
                }
            }

            if (danhSachChon.Count == 0)
            {
                MessageBox.Show("Vui lòng tích chọn ít nhất một đơn vị để làm mới dữ liệu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ==============================================================================
            // 2. XÁC NHẬN NGƯỜI DÙNG (GỘP BÁO CÁO VÀO 1 MSG DUY NHẤT NHƯ YÊU CẦU)
            // ==============================================================================
            if (!_daXacNhanResetTrongSession)
            {
                string chuoiChon = string.Join("; ", danhSachChon);
                string chuoiBoQua = danhSachBoQua.Count > 0 ? string.Join("; ", danhSachBoQua) : "Không có";

                string msgConfirm = $"Đơn vị đặt lại phân loại: {chuoiChon}\n\n" +
                                    $"Đơn vị bỏ qua: {chuoiBoQua}\n\n" +
                                    "Bạn có muốn tiếp tục ?";

                var confirm = MessageBox.Show(
                    msgConfirm,
                    "Thông báo",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);

                if (confirm != DialogResult.Yes) return;
                _daXacNhanResetTrongSession = true;
            }

            // 3. SETUP TIẾN TRÌNH & KHÓA GIAO DIỆN
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

                // 4. LUỒNG NGẦM XỬ LÝ DATABASE
                var (soDong, tbLoi) = await Task.Run(() => ThucThiResetDuLieu(keepList, danhSachChon, progress, _cts.Token));

                if (tbLoi == "CANCELED") return;

                if (!string.IsNullOrEmpty(tbLoi))
                {
                    MessageBox.Show("Có lỗi trong quá trình cập nhật CSDL.\nChi tiết: " + tbLoi, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // ĐÃ XÓA HÀM HIỂN THỊ BÁO CÁO CHI TIẾT TẠI ĐÂY ĐỂ TRÁNH PHIỀN PHỨC

                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM ?? "Admin", "Reset Phân Loại", $"Số lượng: {soDong} CBCS thuộc {danhSachChon.Count} đơn vị");

                if (Form6_XuLyData.Instance != null && !Form6_XuLyData.Instance.IsDisposed)
                {
                    Form6_XuLyData.Instance.BeginInvoke(new Action(() => Form6_XuLyData.Instance.RefreshCSDL()));
                }

                // 5. CHỈ BÁO HOÀN TẤT LÊN NÚT BẤM VÀ TỰ ĐỘNG ĐÓNG FORM MƯỢT MÀ
                btn_ResetPhanLoai.BackColor = System.Drawing.Color.ForestGreen;
                btn_ResetPhanLoai.ForeColor = System.Drawing.Color.White;
                SetStatus($"Hoàn tất! ({soDong} CBCS)", System.Drawing.Color.ForestGreen);

                await Task.Delay(500);
                this.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex}");
                MessageBox.Show($"Đã xảy ra lỗi hệ thống: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isProcessing = false;
                if (_cts != null)
                {
                    _cts.Dispose();
                    _cts = null;
                }

                if (_isClosingRequested) this.BeginInvoke(new Action(this.Close));
                else if (!this.IsDisposed)
                {
                    btn_ResetPhanLoai.Enabled = true;
                    btn_ResetPhanLoai.BackColor = SystemColors.Control;
                    btn_ResetPhanLoai.ForeColor = SystemColors.ControlText;
                    this.Cursor = Cursors.Default;
                }
            }
        }
        private (int soDongDaCapNhat, string thongBaoLoi) ThucThiResetDuLieu(HashSet<string> keepList, HashSet<string> danhSachDonViDuocChon, IProgress<int> progress, CancellationToken token)
        {
            var idsToUpdate = new List<long>();

            // TỐI ƯU ĐỈNH CAO: RAM Caching chống giải mã trùng lặp
            var cacheDonVi = new Dictionary<string, string>();
            var cachePhanLoai = new Dictionary<string, string>();

            try
            {
                var builder = new SqliteConnectionStringBuilder { DataSource = _csdl2Path, DefaultTimeout = 30 };
                using var conn = new SqliteConnection(builder.ConnectionString);
                conn.Open();

                using (var pragmaCmd = conn.CreateCommand())
                {
                    pragmaCmd.CommandText = "PRAGMA quick_check; PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;";
                    pragmaCmd.ExecuteNonQuery();
                }

                // =========================================================================
                // PHASE 1: ĐỌC VÀ LỌC DỮ LIỆU ĐA TẦNG O(N)
                // =========================================================================
                using (var cmdSelect = conn.CreateCommand())
                {
                    cmdSelect.CommandText = "SELECT ID, PhanLoai, DonVi FROM DanhSach WHERE PhanLoai IS NOT NULL AND PhanLoai <> ''";
                    using var reader = cmdSelect.ExecuteReader();

                    while (reader.Read())
                    {
                        token.ThrowIfCancellationRequested();

                        long dbId = reader.GetInt64(0);
                        string phanLoaiDbMaHoa = reader.GetString(1);
                        string donViDbMaHoa = reader.IsDBNull(2) ? "" : reader.GetString(2);

                        // 1. Dùng RAM Cache để giải mã Đơn Vị siêu tốc
                        if (!cacheDonVi.TryGetValue(donViDbMaHoa, out string donViGoc))
                        {
                            try { donViGoc = BaoMatAES.GiaiMa(donViDbMaHoa)?.Trim() ?? ""; } catch { donViGoc = ""; }
                            cacheDonVi[donViDbMaHoa] = donViGoc;
                        }

                        // ⭐ ĐIỀU KIỆN TIÊN QUYẾT: Chỉ làm việc với đơn vị NẰM TRONG DANH SÁCH CHỌN
                        if (danhSachDonViDuocChon.Contains(donViGoc))
                        {
                            // 2. Dùng RAM Cache để giải mã Phân Loại siêu tốc
                            if (!cachePhanLoai.TryGetValue(phanLoaiDbMaHoa, out string phanLoaiGoc))
                            {
                                try { phanLoaiGoc = BaoMatAES.GiaiMa(phanLoaiDbMaHoa)?.Trim() ?? ""; } catch { phanLoaiGoc = ""; }
                                cachePhanLoai[phanLoaiDbMaHoa] = phanLoaiGoc;
                            }

                            string checkVal = string.IsNullOrEmpty(phanLoaiGoc) ? "Không PL" : phanLoaiGoc;

                            // 3. Đối chiếu danh sách Miễn Trừ (Loại 1, 3, 4, Không PL...)
                            if (!keepList.Contains(checkVal))
                            {
                                idsToUpdate.Add(dbId);
                            }
                        }
                    }
                }

                int total = idsToUpdate.Count;
                if (total == 0) return (0, string.Empty);

                // =========================================================================
                // PHASE 2: CẬP NHẬT BATCH VỚI TRANSACTION & RETRY LOGIC
                // =========================================================================
                string loai2Encrypted = BaoMatAES.MaHoa("Loại 2");
                int rowsAffected = 0;

                using var transaction = conn.BeginTransaction();
                try
                {
                    using var cmdUpdate = conn.CreateCommand();
                    cmdUpdate.Transaction = transaction;
                    cmdUpdate.CommandText = "UPDATE DanhSach SET PhanLoai = @pl WHERE ID = @id";

                    cmdUpdate.Parameters.AddWithValue("@pl", loai2Encrypted);
                    var idParam = cmdUpdate.Parameters.Add("@id", SqliteType.Integer);

                    int reportInterval = Math.Max(1, total / 100);

                    for (int i = 0; i < total; i++)
                    {
                        token.ThrowIfCancellationRequested();
                        idParam.Value = idsToUpdate[i];

                        int maxRetries = 3;
                        for (int retry = 0; retry < maxRetries; retry++)
                        {
                            try
                            {
                                rowsAffected += cmdUpdate.ExecuteNonQuery();
                                break;
                            }
                            catch (SqliteException ex) when (ex.SqliteErrorCode == 5)
                            {
                                if (retry == maxRetries - 1) throw;
                                System.Threading.Thread.Sleep(200);
                            }
                        }

                        if (i % reportInterval == 0 || i == total - 1)
                        {
                            progress?.Report((i + 1) * 100 / total);
                        }
                    }
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }

                // =========================================================================
                // PHASE 3: BẢO TRÌ WAL FILE
                // =========================================================================
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
    }
}