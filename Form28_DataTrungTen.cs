using Microsoft.Data.Sqlite;
using System.Data;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace PhanMemThiDua2026
{
    public partial class Form28_DataTrungTen : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private readonly List<TrungTenModel> _data = new();
        private List<TrungTenModel> _viewData = new();
        private readonly ToolStripStatusLabel _rightSpacer = new();
        private const int EM_SETCUEBANNER = 0x1501;
        private const int WM_SETREDRAW = 11;

        // ⭐ CHUẨN KỸ SƯ: Khởi tạo luồng an toàn bằng Interlocked
        private int _isProcessing = 0;
        private CancellationTokenSource _cts;

        private Font _headerFont;
        private Font _boldFont;
        private string _donViDuocChon = "";
        private volatile bool _isTreeSelecting;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, string lParam);

        private List<TrungTenDetailModel> _viewDataDetail = new();
        private Krypton.Toolkit.KryptonTreeView kryptonTreeView_DieuHuong;
        private Krypton.Toolkit.KryptonSplitContainer splitContainer;
        private static readonly char[] _splitChars = new[] { ';' };

        public Form28_DataTrungTen()
        {
            InitializeComponent();
            KhoiTaoGiaoDienSplit();
            SetupPlaceholder();

            kryptonButton_TimKiem.DialogResult = DialogResult.None;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            AcceptButton = kryptonButton_TimKiem;

            Load += Form28_DataTrungTen_Load;
            KhoiTaoDataGridView();
            InitToolTips();
            SetupStatusStrip();
        }

        private async void Form28_DataTrungTen_Load(object sender, EventArgs e)
        {
            
            kryptonDataGridView_TrungTen.ContextMenuStrip = contextMenuStrip1;
            Module_MenuChuotPhai.TichHopGiaoDienXanhLa(contextMenuStrip1);
            XacDinhPhienBan();

            if (splitContainer != null)
            {
                splitContainer.SplitterDistance = 280;
            }

            await TaiDuLieuHeThongAsync();
        }

        /// <summary>
        /// ⭐ CHUẨN KỸ SƯ: Đóng gói luồng nạp dữ liệu bất đồng bộ, tự động tái tạo Token phòng thủ rò rỉ
        /// </summary>
        private async Task TaiDuLieuHeThongAsync()
        {
            if (Interlocked.Exchange(ref _isProcessing, 1) == 1) return;

            toolStripStatusLabel1.Text = "Đang phân tích dữ liệu...";
            BatProgress();

            // Tái tạo CancellationTokenSource sạch cho phiên làm việc mới
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                var result = await Task.Run(() => PhanTichDuLieu(token), token);

                if (IsDisposed || token.IsCancellationRequested) return;

                _data.Clear();
                if (result != null) _data.AddRange(result);
                _viewData = _data;

                HienThiTreeView();

                _viewDataDetail.Clear();
                HienThiGrid();

                CapNhatStatus();
                textBoxKyToon_TimTen.Focus();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                toolStripStatusLabel1.Text = "Lỗi khi phân tích dữ liệu.";
                System.Diagnostics.Debug.WriteLine($"[Form28 Load Lỗi]: {ex}");
                MessageBox.Show("Có lỗi nghiêm trọng xảy ra trong quá trình kết nối và phân tích dữ liệu CSDL.", "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                TatProgress();
                Interlocked.Exchange(ref _isProcessing, 0);
            }
        }

        #region KHOI TAO GIAO DIEN & LOAD DU LIEU
        private void KhoiTaoGiaoDienSplit()
        {
            splitContainer = new Krypton.Toolkit.KryptonSplitContainer
            {
                Dock = DockStyle.Fill,
                Cursor = Cursors.Default,
                FixedPanel = FixedPanel.Panel1,
                Panel1MinSize = 250
            };

            kryptonTreeView_DieuHuong = new Krypton.Toolkit.KryptonTreeView
            {
                Dock = DockStyle.Fill,
                ItemHeight = 30,
                ShowLines = true,
                ShowPlusMinus = true
            };

            kryptonTreeView_DieuHuong.BeforeExpand += KryptonTreeView_DieuHuong_BeforeExpand;
            kryptonTreeView_DieuHuong.AfterSelect += KryptonTreeView_DieuHuong_AfterSelect;

            splitContainer.Panel1.Controls.Add(kryptonTreeView_DieuHuong);

            tableLayoutPanel2.Controls.Remove(kryptonDataGridView_TrungTen);
            splitContainer.Panel2.Controls.Add(kryptonDataGridView_TrungTen);

            tableLayoutPanel2.Controls.Add(splitContainer, 0, 1);
        }

        private void KhoiTaoDataGridView()
        {
            string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
            bool laTanBinh = phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase);

            var grid = kryptonDataGridView_TrungTen;

            grid.SuspendLayout();
            grid.Columns.Clear();
            grid.Dock = DockStyle.Fill;

            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;

            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.EnableHeadersVisualStyles = false;

            // Kích hoạt VirtualMode chuẩn kỹ sư
            grid.VirtualMode = true;

            // ⭐ LŨY ĐẲNG EVENT: Hủy liên kết cũ trước khi gán mới để tránh nhân bản bộ vẽ nền gây lag
            grid.CellValueNeeded -= KryptonDataGridView_TrungTen_Detail_CellValueNeeded;
            grid.CellValueNeeded += KryptonDataGridView_TrungTen_Detail_CellValueNeeded;

            grid.RowPrePaint -= KryptonDataGridView_TrungTen_Detail_RowPrePaint;
            grid.RowPrePaint += KryptonDataGridView_TrungTen_Detail_RowPrePaint;

            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 45;

            _headerFont?.Dispose();
            _headerFont = new Font(grid.Font, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Font = _headerFont;
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            _boldFont?.Dispose();
            _boldFont = new Font(grid.Font, FontStyle.Bold);

            // PHỐI MÀU UX CHUẨN LỰC LƯỢNG
            grid.RowsDefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(225, 250, 225);
            grid.RowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(144, 238, 144);
            grid.RowsDefaultCellStyle.SelectionForeColor = Color.Black;

            grid.GridColor = Color.LightGray;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "STT", HeaderText = "STT", FillWeight = 6, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "HoTen", HeaderText = laTanBinh ? "Họ và tên tân binh" : "Họ và tên CBCS", FillWeight = 26 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "NamSinh", HeaderText = "Năm sinh", FillWeight = 12, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CapBac", HeaderText = "Cấp bậc", FillWeight = 14, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DonVi", HeaderText = "Đơn vị", FillWeight = 30 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoHieu", HeaderText = "Số hiệu", FillWeight = 12, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GhiChu", HeaderText = "Ghi chú", FillWeight = 20 });

            // ⭐ TRIỆT TIÊU ĐỘ TRỄ RENDERING: Khóa cứng layout dòng, vô hiệu hóa tự động co giãn ô text dài
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.RowTemplate.Height = 35;

            grid.ResumeLayout();
        }

        private void KryptonDataGridView_TrungTen_Detail_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (_viewDataDetail == null || e.RowIndex < 0 || e.RowIndex >= _viewDataDetail.Count) return;

            var grid = kryptonDataGridView_TrungTen;
            if (grid == null || grid.IsDisposed) return;

            try
            {
                var rowStyle = grid.Rows[e.RowIndex].DefaultCellStyle;
                string donViCuaDong = _viewDataDetail[e.RowIndex].DonVi;

                bool laDonViDuocChon = !string.IsNullOrEmpty(_donViDuocChon) &&
                                       string.Equals(donViCuaDong, _donViDuocChon, StringComparison.OrdinalIgnoreCase);

                if (laDonViDuocChon)
                {
                    rowStyle.BackColor = Color.FromArgb(152, 251, 152); // Xanh lá cây nhạt
                    rowStyle.Font = _boldFont;
                }
                else
                {
                    // Trả lại màu đồng bộ chuẩn cho các hàng không được nhấp chọn
                    rowStyle.BackColor = (e.RowIndex % 2 == 0)
                        ? Color.FromArgb(240, 255, 240)
                        : Color.FromArgb(225, 250, 225);
                    rowStyle.Font = grid.Font;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi vẽ dòng RowPrePaint]: {ex.Message}");
            }
        }

        private void KryptonDataGridView_TrungTen_Detail_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (_viewDataDetail == null || e.RowIndex < 0 || e.RowIndex >= _viewDataDetail.Count) return;

            var item = _viewDataDetail[e.RowIndex];
            switch (e.ColumnIndex)
            {
                case 0: e.Value = e.RowIndex + 1; break;
                case 1: e.Value = item.HoTen; break;
                case 2: e.Value = item.NamSinh; break;
                case 3: e.Value = item.CapBac; break;
                case 4: e.Value = item.DonVi; break;
                case 5: e.Value = item.SoHieu; break;
                case 6: e.Value = item.GhiChu; break;
            }
        }
        #endregion

        #region XU LY DU LIEU TỐI ƯU & TREEVIEW
        private List<TrungTenModel> PhanTichDuLieu(CancellationToken token)
        {
            var dict = new Dictionary<string, Dictionary<string, List<long>>>(10000, StringComparer.OrdinalIgnoreCase);
            var aesCacheTen = new Dictionary<string, string>(10000);
            var aesCacheDonVi = new Dictionary<string, string>(2000);
            var textInfo = CultureInfo.CurrentCulture.TextInfo;

            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = _csdl2Path,
                Mode = SqliteOpenMode.ReadOnly,
                DefaultTimeout = 5,
                Pooling = true,
                Cache = SqliteCacheMode.Private
            };

            using var conn = new SqliteConnection(builder.ConnectionString);
            conn.Open();

            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA journal_mode = WAL; PRAGMA synchronous = NORMAL; PRAGMA temp_store = MEMORY;";
                pragmaCmd.ExecuteNonQuery();
            }

            using var cmd = new SqliteCommand("SELECT rowid, HoVaTen, DonVi FROM DanhSach", conn);
            using var reader = cmd.ExecuteReader();

            int recordCount = 0;

            while (reader.Read())
            {
                token.ThrowIfCancellationRequested();
                if (++recordCount > 500000) throw new Exception("Vượt quá giới hạn bộ nhớ an toàn của phân hệ.");

                if (reader.IsDBNull(1)) continue;
                long rowId = reader.GetInt64(0);
                string maHoaTen = reader.GetString(1);
                if (string.IsNullOrWhiteSpace(maHoaTen)) continue;
                // ⭐ ZERO-ALLOCATION PATTERN: Sử dụng TryGetValue loại bỏ hoàn toàn khối try-catch thừa thãi
                if (!aesCacheTen.TryGetValue(maHoaTen, out string hoTen))
                {
                    hoTen = BaoMatAES.GiaiMa(maHoaTen)?.Trim() ?? "";
                    if (string.IsNullOrEmpty(hoTen)) continue;
                    hoTen = textInfo.ToTitleCase(hoTen.ToLower());
                    aesCacheTen[maHoaTen] = hoTen;
                }
                string donVi = "";
                if (!reader.IsDBNull(2))
                {
                    string maHoaDonVi = reader.GetString(2);
                    if (!string.IsNullOrEmpty(maHoaDonVi) && !aesCacheDonVi.TryGetValue(maHoaDonVi, out donVi))
                    {
                        donVi = BaoMatAES.GiaiMa(maHoaDonVi)?.Trim() ?? "";
                        donVi = donVi.ToUpperInvariant();
                        aesCacheDonVi[maHoaDonVi] = donVi;
                    }
                }
                if (!dict.TryGetValue(hoTen, out var dvDict))
                {
                    dvDict = new Dictionary<string, List<long>>(2, StringComparer.OrdinalIgnoreCase);
                    dict[hoTen] = dvDict;
                }
                if (!dvDict.TryGetValue(donVi, out var listRowIds))
                {
                    listRowIds = new List<long>(4);
                    dvDict[donVi] = listRowIds;
                }
                listRowIds.Add(rowId);
            }

            var result = new List<TrungTenModel>(dict.Count);
            var sb = new StringBuilder(128);

            foreach (var item in dict)
            {
                token.ThrowIfCancellationRequested();
                int tong = 0;
                var allRowIds = new List<long>();

                foreach (var kvp in item.Value)
                {
                    tong += kvp.Value.Count;
                    allRowIds.AddRange(kvp.Value);
                }

                if (tong <= 1) continue;

                sb.Clear();
                foreach (var d in item.Value)
                {
                    if (sb.Length > 0) sb.Append("; ");
                    sb.Append(d.Key).Append(" (").Append(d.Value.Count).Append(')');
                }

                result.Add(new TrungTenModel
                {
                    HoTen = item.Key,
                    SoLan = tong,
                    DonVi = sb.ToString(),
                    RowIds = allRowIds
                });
            }

            result.Sort((a, b) => b.SoLan.CompareTo(a.SoLan));
            return result;
        }

        private List<TrungTenDetailModel> TruyVanChiTietSQLite(List<long> targetRowIds, CancellationToken token)
        {
            var result = new List<TrungTenDetailModel>(targetRowIds.Count);
            if (targetRowIds == null || targetRowIds.Count == 0) return result;

            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = _csdl2Path,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = true,
                Cache = SqliteCacheMode.Private
            };

            using var conn = new SqliteConnection(builder.ConnectionString);
            conn.Open();

            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA journal_mode = WAL; PRAGMA temp_store = MEMORY;";
                pragmaCmd.ExecuteNonQuery();
            }

            // ⭐ CHUẨN AN TOÀN: Gom cụm định danh ROWID, ngăn chặn lỗi biên dịch SQLite Command vượt ngưỡng
            string inClause = string.Join(",", targetRowIds);
            string query = $"SELECT HoVaTen, NamSinh, CapBac, DonVi, SoHieu, GhiChu FROM DanhSach WHERE rowid IN ({inClause})";

            using var cmd = new SqliteCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                token.ThrowIfCancellationRequested();

                string hoTenGiaiMa = "";
                if (!reader.IsDBNull(0))
                {
                    hoTenGiaiMa = BaoMatAES.GiaiMa(reader.GetString(0))?.Trim() ?? "";
                    hoTenGiaiMa = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(hoTenGiaiMa.ToLower());
                }

                result.Add(new TrungTenDetailModel
                {
                    HoTen = hoTenGiaiMa,
                    NamSinh = !reader.IsDBNull(1) ? (BaoMatAES.GiaiMa(reader.GetString(1)) ?? "") : "",
                    CapBac = !reader.IsDBNull(2) ? (BaoMatAES.GiaiMa(reader.GetString(2)) ?? "") : "",
                    DonVi = !reader.IsDBNull(3) ? (BaoMatAES.GiaiMa(reader.GetString(3))?.ToUpperInvariant() ?? "") : "",
                    SoHieu = !reader.IsDBNull(4) ? (BaoMatAES.GiaiMa(reader.GetString(4)) ?? "") : "",
                    GhiChu = !reader.IsDBNull(5) ? (BaoMatAES.GiaiMa(reader.GetString(5)) ?? "") : ""
                });
            }
            return result;
        }

        private void HienThiTreeView()
        {
            if (kryptonTreeView_DieuHuong == null || kryptonTreeView_DieuHuong.IsDisposed) return;

            // ⭐ KHÓA ĐÓNG BĂNG ĐỒ HỌA OS: Chặn đứng tình trạng chớp, giật khựng khung hình của bộ Krypton UI
            SendMessage(kryptonTreeView_DieuHuong.Handle, WM_SETREDRAW, 0, "");
            kryptonTreeView_DieuHuong.BeginUpdate();
            kryptonTreeView_DieuHuong.Nodes.Clear();

            var nodesArray = new TreeNode[_viewData.Count];
            for (int i = 0; i < _viewData.Count; i++)
            {
                var item = _viewData[i];
                TreeNode rootNode = new TreeNode($"{item.HoTen} ({item.SoLan})") { Tag = item };
                rootNode.Nodes.Add(new TreeNode("Đang tải..."));
                nodesArray[i] = rootNode;
            }

            kryptonTreeView_DieuHuong.Nodes.AddRange(nodesArray);
            kryptonTreeView_DieuHuong.EndUpdate();

            SendMessage(kryptonTreeView_DieuHuong.Handle, WM_SETREDRAW, 1, "");
            kryptonTreeView_DieuHuong.Refresh();

            SafeSelectFirstTreeNode();
        }

        private void SafeSelectFirstTreeNode()
        {
            try
            {
                if (IsDisposed || Disposing) return;
                if (kryptonTreeView_DieuHuong == null || kryptonTreeView_DieuHuong.IsDisposed || !kryptonTreeView_DieuHuong.IsHandleCreated) return;

                if (kryptonTreeView_DieuHuong.Nodes.Count == 0)
                {
                    kryptonTreeView_DieuHuong.SelectedNode = null;
                    return;
                }

                TreeNode firstNode = kryptonTreeView_DieuHuong.Nodes[0];
                if (ReferenceEquals(kryptonTreeView_DieuHuong.SelectedNode, firstNode)) return;

                BeginInvoke(new MethodInvoker(() =>
                {
                    try
                    {
                        if (IsDisposed || Disposing || kryptonTreeView_DieuHuong == null || kryptonTreeView_DieuHuong.IsDisposed) return;
                        if (kryptonTreeView_DieuHuong.Nodes.Count > 0)
                        {
                            kryptonTreeView_DieuHuong.SelectedNode = kryptonTreeView_DieuHuong.Nodes[0];
                        }
                    }
                    catch (ObjectDisposedException) { }
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SafeSelectFirstTreeNode]: {ex.Message}");
            }
        }

        private void KryptonTreeView_DieuHuong_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            TreeNode expandingNode = e.Node;

            if (expandingNode.Nodes.Count == 1 && expandingNode.Nodes[0].Text == "Đang tải...")
            {
                expandingNode.Nodes.Clear();

                if (expandingNode.Tag is TrungTenModel model)
                {
                    var donViArray = model.DonVi.Split(_splitChars, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var dv in donViArray)
                    {
                        TreeNode childNode = new TreeNode(dv.Trim())
                        {
                            Tag = new { HoTen = model.HoTen, DonViStr = dv.Trim() }
                        };
                        expandingNode.Nodes.Add(childNode);
                    }
                }
            }
        }

        private async void KryptonTreeView_DieuHuong_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (_isTreeSelecting) return;
            if (e.Node == null || e.Node.Tag == null) return;

            try
            {
                _isTreeSelecting = true;
                List<long> listRowIdCanTim = null;
                _donViDuocChon = "";

                if (e.Node.Tag is TrungTenModel modelCha)
                {
                    listRowIdCanTim = modelCha.RowIds;
                }
                else
                {
                    dynamic tagData = e.Node.Tag;
                    string parentName = tagData.HoTen;

                    var parentModel = _viewData.FirstOrDefault(x => x.HoTen == parentName);
                    if (parentModel != null)
                    {
                        listRowIdCanTim = parentModel.RowIds;
                    }

                    string rawDonVi = tagData.DonViStr;
                    int idx = rawDonVi.LastIndexOf(" (");
                    if (idx > 0) _donViDuocChon = rawDonVi.Substring(0, idx).Trim();
                    else _donViDuocChon = rawDonVi.Trim();
                }

                // ⭐ CẬP NHẬT MÀU SẮC LẬP TỨC: Ép lưới xóa vết vẽ của đồng chí cũ, chuyển vùng highlight mượt mà
                if (kryptonDataGridView_TrungTen != null && !kryptonDataGridView_TrungTen.IsDisposed)
                {
                    kryptonDataGridView_TrungTen.Invalidate();
                }

                if (listRowIdCanTim == null || listRowIdCanTim.Count == 0) return;

                if (Interlocked.Exchange(ref _isProcessing, 1) == 1) return;
                BatProgress();

                try
                {
                    var token = _cts.Token;
                    _viewDataDetail = await Task.Run(() => TruyVanChiTietSQLite(listRowIdCanTim, token), token);
                    HienThiGrid();
                }
                finally
                {
                    TatProgress();
                    Interlocked.Exchange(ref _isProcessing, 0);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TreeView AfterSelect Lỗi]: {ex.Message}");
            }
            finally
            {
                _isTreeSelecting = false;
            }
        }

        private void HienThiGrid()
        {
            var grid = kryptonDataGridView_TrungTen;
            if (grid == null || grid.IsDisposed) return;

            grid.SuspendLayout();
            try
            {
                grid.RowCount = _viewDataDetail != null ? _viewDataDetail.Count : 0;
                if (grid.RowCount > 0)
                {
                    grid.ClearSelection();
                    grid.CurrentCell = null;
                }
                grid.Invalidate();
            }
            finally
            {
                grid.ResumeLayout();
            }
        }
        #endregion

        #region CHUC NANG (TIM KIEM, LAM MOI)
        private async void TimKiem()
        {
            if (Interlocked.Exchange(ref _isProcessing, 1) == 1) return;

            BatProgress();
            string keyword = textBoxKyToon_TimTen.Text.Trim();

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                var newViewData = await Task.Run(() =>
                {
                    if (string.IsNullOrEmpty(keyword)) return _data;
                    return _data.Where(x => x.HoTen.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
                }, token);

                _viewData = newViewData;

                HienThiTreeView();
                _viewDataDetail.Clear();
                HienThiGrid();
                CapNhatStatus();
            }
            catch (OperationCanceledException) { }
            finally
            {
                TatProgress();
                Interlocked.Exchange(ref _isProcessing, 0);
            }
        }

        private void kryptonButton_TimKiem_Click(object sender, EventArgs e) => TimKiem();

        private async void kryptonButton_LamMoi_Click(object sender, EventArgs e)
        {
            textBoxKyToon_TimTen.Clear();
            await TaiDuLieuHeThongAsync();
        }

        private void kryptonButton_Dong_Click(object sender, EventArgs e)
        {
            // 1. Tìm Form cha đang mở
            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            // 2. Nếu tìm thấy Form cha thì cập nhật lại tiêu đề
            if (formCha != null)
            {
                formCha.CapNhatTieuDe("Trang phân loại thi đua");
            }
            // 3. Đóng form hiện tại
            this.Close();
        }

        private void textBoxKyToon_TimTen_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                TimKiem();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                textBoxKyToon_TimTen.Clear();
                TimKiem();
            }
        }
        #endregion

        #region TIEN ICH UI (PHIM TAT, STATUS STRIP)
        private Action SafeAction(Action action)
        {
            return () =>
            {
                if (action == null || Interlocked.CompareExchange(ref _isProcessing, 0, 0) == 1) return;
                try { action.Invoke(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Lỗi thực thi phím tắt: " + ex.Message); }
            };
        }

        private bool SafeExecute(Action action)
        {
            SafeAction(action).Invoke();
            return true;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (Interlocked.CompareExchange(ref _isProcessing, 0, 0) == 1) return base.ProcessCmdKey(ref msg, keyData);

            try
            {
                if (Module_PhimTat.XuLy(keyData, actionLamMoi: SafeAction(() => lamMoi_ToolStripMenuItem_Click(null, null))))
                {
                    return true;
                }

                Keys key = keyData & Keys.KeyCode;
                Keys modifier = keyData & Keys.Modifiers;

                if (modifier == Keys.None && key == Keys.F4)
                    return SafeExecute(() => xoaTimKiem_ToolStripMenuItem_Click(null, null));

                if (modifier == Keys.Control && key == Keys.Q)
                    return SafeExecute(() => quayLaiTrangXuLyDuLieu_ToolStripMenuItem_Click(null, null));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi phím tắt: " + ex.Message);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void BatProgress()
        {
            toolStripProgressBar1.Visible = true;
            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
        }

        private void TatProgress()
        {
            toolStripProgressBar1.Visible = false;
        }

        private void CapNhatStatus()
        {
            int total = _data.Count;
            int filtered = _viewData.Count;

            if (total == 0)
            {
                toolStripStatusLabel1.Text = "Không phát hiện tên trùng.";
                return;
            }

            if (filtered == total)
                toolStripStatusLabel1.Text = $"Số CBCS có tên trùng nhau: {total} đồng chí";
            else
                toolStripStatusLabel1.Text = $"Kết quả tìm kiếm: {filtered}/{total} CBCS trùng tên";
        }

        private void InitToolTips()
        {
            if (toolTip1 == null) return;
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Chức năng";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            toolTip1.SetToolTip(kryptonButton_TimKiem, "Bấm nút để tìm kiếm");
            toolTip1.SetToolTip(kryptonButton_LamMoi, "Làm mới dữ liệu");
            toolTip1.SetToolTip(kryptonButton_Dong, "Thoát trang");
        }

        private void SetupStatusStrip()
        {
            if (statusStrip1 == null || statusStrip1.IsDisposed) return;
            if (!statusStrip1.Items.Contains(_rightSpacer))
            {
                _rightSpacer.Spring = true;
                _rightSpacer.Text = "";
                _rightSpacer.AutoSize = false;
                int index = statusStrip1.Items.IndexOf(toolStripStatusLabel2);
                if (index >= 0) statusStrip1.Items.Insert(index, _rightSpacer);
                else statusStrip1.Items.Add(_rightSpacer);
            }
            toolStripStatusLabel2.Alignment = ToolStripItemAlignment.Right;
            toolStripStatusLabel2.TextAlign = ContentAlignment.MiddleRight;
        }

        private void XacDinhPhienBan()
        {
            bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
            toolStripStatusLabel2.Text = laTanBinh ? "Phiên bản phần mềm dành cho tân binh" : "Phiên bản phần mềm dành cho CBCS";
        }

        private void SetupPlaceholder()
        {
            if (textBoxKyToon_TimTen != null && !textBoxKyToon_TimTen.IsDisposed)
                SendMessage(textBoxKyToon_TimTen.Handle, EM_SETCUEBANNER, 0, "Nhập họ và tên CBCS để tìm kiếm");
        }

        private void lamMoi_ToolStripMenuItem_Click(object sender, EventArgs e) => kryptonButton_LamMoi.PerformClick();
        private void quayLaiTrangXuLyDuLieu_ToolStripMenuItem_Click(object sender, EventArgs e) => kryptonButton_Dong.PerformClick();
        private void xoaTimKiem_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBoxKyToon_TimTen.Clear();
            textBoxKyToon_TimTen.Focus();
            TimKiem();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // ⭐ AN TOÀN TUYỆT ĐỐI: Ngắt toàn bộ tiến trình chạy ngầm lập tức khi tắt form
            _cts?.Cancel();

            var grid = kryptonDataGridView_TrungTen;
            if (grid != null)
            {
                grid.CellValueNeeded -= KryptonDataGridView_TrungTen_Detail_CellValueNeeded;
                grid.RowPrePaint -= KryptonDataGridView_TrungTen_Detail_RowPrePaint;
            }

            if (kryptonTreeView_DieuHuong != null)
            {
                kryptonTreeView_DieuHuong.BeforeExpand -= KryptonTreeView_DieuHuong_BeforeExpand;
                kryptonTreeView_DieuHuong.AfterSelect -= KryptonTreeView_DieuHuong_AfterSelect;
            }

            // Giải phóng triệt để tài nguyên đồ họa GDI+ tránh treo RAM hệ điều hành
            _headerFont?.Dispose();
            _boldFont?.Dispose();
            _cts?.Dispose();

            base.OnFormClosing(e);
        }
        #endregion
    }

    public class TrungTenModel
    {
        public string HoTen { get; set; } = "";
        public int SoLan { get; set; }
        public string DonVi { get; set; } = "";
        public List<long> RowIds { get; set; } = new();
    }

    public class TrungTenDetailModel
    {
        public string HoTen { get; set; } = "";
        public string NamSinh { get; set; } = "";
        public string CapBac { get; set; } = "";
        public string DonVi { get; set; } = "";
        public string SoHieu { get; set; } = "";
        public string GhiChu { get; set; } = "";
    }
}
