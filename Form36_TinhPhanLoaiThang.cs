using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
using System.Text;

namespace PhanMemThiDua2026
{
    public partial class Form36_TinhPhanLoaiThang : KryptonForm
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        // 1. Biến lưu trữ dữ liệu
        private DataTable _dtGoc;
        private DataTable _dtXuLy;
        private DataView _dvHienThi;
        // Khai báo biến toàn cục
        private DataTable _dtData;
        private bool _isTanBinh;
        // 2. Map Tên hiển thị
        private Dictionary<string, string> _mapCotThoiGian = new Dictionary<string, string>();
        private List<string> _danhSachThoiGianGoc = new List<string>();
        // 3. Cờ trạng thái đa luồng
        private bool _isUpdatingCombos = false;
        private bool _isCalculating = false;
        // 4. Tài nguyên UI
        private Image _iconStar;
        // 🔥 GIA CỐ: Token hủy tiến trình ngầm (Chống Crash)
        private CancellationTokenSource _ctsLoadNgam;
        private readonly object _dataLock = new object();
        private CancellationTokenSource _ctsFilter;
        // HÀM KHỞI TẠO DUY NHẤT (ĐÃ CHUẨN HÓA)
        public Form36_TinhPhanLoaiThang(DataTable dtTruyenTuForm15, bool laTanBinh)
        {
            InitializeComponent();

            _iconStar = Properties.Resources.ic_star;
            _isTanBinh = laTanBinh;

            // ✅ Nhận null an toàn — Form36 sẽ tự load từ DB trong Load event
            if (dtTruyenTuForm15 != null && dtTruyenTuForm15.Rows.Count > 0)
            {
                _dtGoc = dtTruyenTuForm15;
                _dtXuLy = _dtGoc.Copy();
            }
            else
            {
                // Tạo DataTable rỗng đúng cấu trúc — Load event sẽ fill dữ liệu vào
                _dtGoc = new DataTable();
                _dtXuLy = new DataTable();
            }

            _dtData = _dtXuLy;

            if (!_dtXuLy.Columns.Contains("KetQuaTinhToan"))
                _dtXuLy.Columns.Add("KetQuaTinhToan", typeof(string));
            if (!_dtXuLy.Columns.Contains("IsLoai1"))
                _dtXuLy.Columns.Add("IsLoai1", typeof(bool));
            if (!_dtXuLy.Columns.Contains("GhiChu"))
                _dtXuLy.Columns.Add("GhiChu", typeof(string));

            _dvHienThi = new DataView(_dtXuLy);

            // Gắn sự kiện
            this.Load += Form36_TinhPhanLoaiThang_Load;
            this.FormClosing += Form36_TinhPhanLoaiThang_FormClosing;

            kryptonDataGridView1.CellDoubleClick += KryptonDataGridView1_CellDoubleClick;
            kryptonDataGridView1.CellPainting += KryptonDataGridView1_CellPainting;
            // Đăng ký sự kiện tô màu động cho các ô
            kryptonDataGridView1.CellFormatting -= KryptonDataGridView1_CellFormatting;
            kryptonDataGridView1.CellFormatting += KryptonDataGridView1_CellFormatting;

            combobox_GiaTriTuanThang_1.SelectedIndexChanged += ComboboxThoiGian_Changed;
            combobox_GiaTriTuanThang_2.SelectedIndexChanged += ComboboxThoiGian_Changed;
            combobox_GiaTriTuanThang_3.SelectedIndexChanged += ComboboxThoiGian_Changed;
            combobox_GiaTriTuanThang_4.SelectedIndexChanged += ComboboxThoiGian_Changed;

            textBox_TimKiemTheoTen.TextChanged += BoLoc_Changed;
            comboBox_TimKiemDonVi.SelectedIndexChanged += BoLoc_Changed;
            comboBox_XepLoaiThiDua.SelectedIndexChanged += BoLoc_Changed;
            comboBox_TimKiemTinhTrang.SelectedIndexChanged += BoLoc_Changed;

            ThietLapToolTip();
        }
        private async void Form36_TinhPhanLoaiThang_Load(object sender, EventArgs e)
        {
            _isUpdatingCombos = true;
            Module_MenuChuotPhai.TichHopGiaoDienXanhLa(contextMenuStrip1);
            // Nếu không có dữ liệu từ Form15 → tự load từ DB
            // KichHoatLoadDuLieuTuDbAsync() tạo DataTable mới trên background
            // rồi gán vào _dtXuLy/_dvHienThi trên UI thread → KHÔNG race condition
            if (_dtXuLy == null || _dtXuLy.Rows.Count == 0)
            {
                this.Enabled = false;
                await KichHoatLoadDuLieuTuDbAsync(); // Xem hàm đã sửa ở trả lời trước
                this.Enabled = true;
            }

            KhoiTaoMapThoiGian();
            LoadComboboxThoiGian();
            LoadComboboxDonVi();
            CauHinhLuoiGiaoDien();
            CauHinhUI_StatusStrip();

            _isUpdatingCombos = false;

            CapNhatThongKeStatus();
            await KichHoatLoadGhiChuAnToanAsync();
        }
        private void Form36_TinhPhanLoaiThang_FormClosing(object sender, FormClosingEventArgs e)
        {
            _ctsLoadNgam?.Cancel();

            if (_iconStar != null)
            {
                _iconStar.Dispose();
                _iconStar = null;
            }

            kryptonDataGridView1.DataSource = null;
            _dvHienThi?.Dispose();
            _dtXuLy?.Dispose();
            _ctsFilter?.Cancel();
            _ctsFilter?.Dispose();
            _ctsLoadNgam?.Dispose();
        }
        // =========================================================================
        // TẢI DỮ LIỆU NGẦM THREAD-SAFE (CHỐNG CRASH)
        // =========================================================================
        private async Task KichHoatLoadGhiChuAnToanAsync()
        {
            _ctsLoadNgam?.Cancel();
            _ctsLoadNgam = new CancellationTokenSource();
            var token = _ctsLoadNgam.Token;

            try
            {
                await LoadDuLieuGhiChuNgamAsync(token);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Tiến trình tải ghi chú đã bị hủy an toàn.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi nghiêm trọng khi tải ghi chú: {ex.Message}");
            }
        }
        private async Task KichHoatLoadDuLieuTuDbAsync()
        {
            string dbPath = Module_DanduongGPS.DuongDanCSDL4;

            if (!File.Exists(dbPath))
                return;

            string tableName =
                _isTanBinh ? "ThiDuaThang_TanBinh" : "ThiDuaThang";

            DataTable temp = new DataTable();

            await Task.Run(() =>
            {
                using var conn =
                    new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");

                conn.Open();

                using var cmd =
              new SqliteCommand($"SELECT * FROM {tableName}", conn);

                using var reader = cmd.ExecuteReader();

                temp.Load(reader);

                foreach (DataRow r in temp.Rows)
                {
                    r["HoVaTen"] = SafeDecrypt(r["HoVaTen"]);
                    r["SoHieu"] = SafeDecrypt(r["SoHieu"]);
                    r["DonVi"] = SafeDecrypt(r["DonVi"]);
                }
            });

            if (IsDisposed)
                return;

            _dtXuLy = temp;

            if (!_dtXuLy.Columns.Contains("KetQuaTinhToan"))
                _dtXuLy.Columns.Add("KetQuaTinhToan", typeof(string));

            if (!_dtXuLy.Columns.Contains("IsLoai1"))
                _dtXuLy.Columns.Add("IsLoai1", typeof(bool));

            if (!_dtXuLy.Columns.Contains("GhiChu"))
                _dtXuLy.Columns.Add("GhiChu", typeof(string));

            _dvHienThi = new DataView(_dtXuLy);

            kryptonDataGridView1.DataSource = _dvHienThi;
        }
        private async Task LoadDuLieuGhiChuNgamAsync(CancellationToken token)
        {
            if (_dtXuLy == null || _dtXuLy.Rows.Count == 0)
                return;

            int rowCount;

            string[] snapshotSoHieu;

            lock (_dataLock)
            {
                rowCount = _dtXuLy.Rows.Count;

                int idxSoHieu = _dtXuLy.Columns["SoHieu"].Ordinal;

                snapshotSoHieu = new string[rowCount];

                for (int i = 0; i < rowCount; i++)
                {
                    snapshotSoHieu[i] =
                        _dtXuLy.Rows[i][idxSoHieu]?.ToString() ?? "";
                }
            }

            string[] resultGhiChu = new string[rowCount];

            await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();

                var hashSoHieu =
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (string sh in snapshotSoHieu)
                {
                    if (!string.IsNullOrWhiteSpace(sh))
                        hashSoHieu.Add(sh);
                }

                var dictGhiChu =
                    new Dictionary<string, string>(
                        hashSoHieu.Count,
                        StringComparer.OrdinalIgnoreCase);

                if (File.Exists(_csdl2Path))
                {
                    using var conn = new SqliteConnection(
                        $"Data Source={_csdl2Path};Mode=ReadOnly");

                    conn.Open();

                    using var cmd = new SqliteCommand(
                        "SELECT SoHieu, GhiChu FROM DanhSach",
                        conn);

                    using var rd = cmd.ExecuteReader();

                    while (rd.Read())
                    {
                        token.ThrowIfCancellationRequested();

                        try
                        {
                            string sh = SafeDecrypt(rd["SoHieu"]);

                            if (!string.IsNullOrEmpty(sh)
                                && hashSoHieu.Contains(sh))
                            {
                                dictGhiChu[sh] =
                                    SafeDecrypt(rd["GhiChu"]);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    }
                }

                Parallel.For(
                    0,
                    rowCount,
                    new ParallelOptions
                    {
                        CancellationToken = token
                    },
                    i =>
                    {
                        string sh = snapshotSoHieu[i];

                        resultGhiChu[i] =
                            (!string.IsNullOrEmpty(sh)
                            && dictGhiChu.TryGetValue(sh, out string gc))
                            ? gc
                            : "";
                    });

            }, token);

            if (token.IsCancellationRequested ||
                IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                await InvokeAsync(() =>
                {
                    GhiDuLieuGhiChuVaoBang(resultGhiChu);
                });
            }
            else
            {
                GhiDuLieuGhiChuVaoBang(resultGhiChu);
            }
        }
        private static readonly Color COLOR_L1_BACK = Color.FromArgb(200, 255, 200);
        private static readonly Color COLOR_L1_FORE = Color.DarkGreen;
        private static readonly Color COLOR_L2_BACK = Color.FromArgb(255, 255, 200);
        private static readonly Color COLOR_L2_FORE = Color.DarkGoldenrod;
        private static readonly Color COLOR_L34_BACK = Color.FromArgb(255, 200, 200);
        private static readonly Color COLOR_L34_FORE = Color.DarkRed;
        private void KryptonDataGridView1_CellFormatting(
            object sender,
            DataGridViewCellFormattingEventArgs e)
        {
            // ===== VALIDATION CỰC KỸ =====
            if (sender is not DataGridView dgv)
                return;

            if (e.RowIndex < 0 ||
                e.RowIndex >= dgv.Rows.Count)
            {
                return;
            }

            // Không xử lý dòng NewRow
            DataGridViewRow row = dgv.Rows[e.RowIndex];

            if (row.IsNewRow)
                return;

            // ===== TÌM CỘT KẾT QUẢ AN TOÀN =====
            if (!dgv.Columns.Contains("KetQuaPhanLoai"))
                return;

            DataGridViewCell cellKetQua =
                row.Cells["KetQuaPhanLoai"];

            if (cellKetQua == null)
                return;

            object rawValue = cellKetQua.Value;

            if (rawValue == null ||
                rawValue == DBNull.Value)
            {
                return;
            }

            // Không dùng Trim để tránh alloc string liên tục
            string ketQua = rawValue.ToString();

            if (string.IsNullOrEmpty(ketQua))
                return;

            // ===== CHỌN MÀU =====
            Color backColor;
            Color foreColor;

            switch (ketQua)
            {
                case "Loại 1":
                    {
                        backColor = COLOR_L1_BACK;
                        foreColor = COLOR_L1_FORE;
                        break;
                    }

                case "Loại 2":
                    {
                        backColor = COLOR_L2_BACK;
                        foreColor = COLOR_L2_FORE;
                        break;
                    }

                case "Loại 3":
                case "Loại 4":
                    {
                        backColor = COLOR_L34_BACK;
                        foreColor = COLOR_L34_FORE;
                        break;
                    }

                default:
                    return;
            }

            // ===== KHÔNG GHI ĐÈ MÀU SELECT CỦA HỆ THỐNG =====
            DataGridViewElementStates state =
                dgv.Rows[e.RowIndex].State;

            bool isSelected =
                (state & DataGridViewElementStates.Selected)
                == DataGridViewElementStates.Selected;

            if (!isSelected)
            {
                e.CellStyle.BackColor = backColor;
                e.CellStyle.ForeColor = foreColor;
            }
            else
            {
                // Chỉ đổi màu chữ khi selected
                e.CellStyle.SelectionForeColor = foreColor;
            }
        }
        private void GhiDuLieuGhiChuVaoBang(string[] resultGhiChu)
        {
            if (_dtXuLy == null || IsDisposed)
                return;

            lock (_dataLock)
            {
                try
                {
                    _dtXuLy.BeginLoadData();

                    int idxGhiChu =
                        _dtXuLy.Columns["GhiChu"].Ordinal;

                    int rowCount =
                        Math.Min(_dtXuLy.Rows.Count,
                                 resultGhiChu.Length);

                    for (int i = 0; i < rowCount; i++)
                    {
                        DataRow row = _dtXuLy.Rows[i];

                        if (row.RowState == DataRowState.Deleted)
                            continue;

                        string current =
                            row[idxGhiChu]?.ToString() ?? "";

                        string newVal =
                            resultGhiChu[i] ?? "";

                        if (!string.Equals(current,
                                           newVal,
                                           StringComparison.Ordinal))
                        {
                            row[idxGhiChu] = newVal;
                        }
                    }
                }
                finally
                {
                    _dtXuLy.EndLoadData();
                }
            }

            kryptonDataGridView1.Refresh();

            CapNhatThongKeStatus();
        }
        private Task InvokeAsync(Action action)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (IsDisposed || !IsHandleCreated)
            {
                tcs.TrySetCanceled();
                return tcs.Task;
            }

            BeginInvoke(new Action(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }));

            return tcs.Task;
        }
        // Stub để dùng trong try (không cần implement gì)
        private void OnColumnChanged_SuppressNotify(object s, DataColumnChangeEventArgs e) { }
        private string SafeDecrypt(object value)
        {
            if (value == null || value == DBNull.Value) return "";
            string s = value.ToString()?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(s)) return "";

            string decrypted = BaoMatAES.GiaiMa(s);
            return string.IsNullOrEmpty(decrypted) ? s : decrypted;
        }
        private async void kryptonButton1_TinhToan_Click(object sender, EventArgs e)
        {
            var combos = new[] { combobox_GiaTriTuanThang_1.Text, combobox_GiaTriTuanThang_2.Text,
                                 combobox_GiaTriTuanThang_3.Text, combobox_GiaTriTuanThang_4.Text };

            if (combos.Any(string.IsNullOrWhiteSpace))
            {
                MessageBox.Show("Vui lòng chọn đủ 4 giá trị thời gian!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_dtXuLy == null || _dtXuLy.Rows.Count == 0) return;
            if (_isCalculating) return;

            string textBanDau = kryptonButton1_TinhToan.Values.Text;
            Image anhBanDau = kryptonButton1_TinhToan.Values.Image;

            try
            {
                _isCalculating = true;
                kryptonButton1_TinhToan.Enabled = false;
                kryptonButton1_TinhToan.Values.Text = "Đang xử lý...";
                kryptonButton1_TinhToan.Values.Image = null;

                // kryptonDataGridView1.DataSource = null;

                int rowCount = _dtXuLy.Rows.Count;
                string[] dbCols = combos.Select(v => _mapCotThoiGian[v]).ToArray();

                int idxC1 = _dtXuLy.Columns[dbCols[0]].Ordinal;
                int idxC2 = _dtXuLy.Columns[dbCols[1]].Ordinal;
                int idxC3 = _dtXuLy.Columns[dbCols[2]].Ordinal;
                int idxC4 = _dtXuLy.Columns[dbCols[3]].Ordinal;
                int idxHoTen = _dtXuLy.Columns["HoVaTen"].Ordinal;

                string[] resultKetQua = new string[rowCount];
                bool[] resultIsLoai1 = new bool[rowCount];

                await Task.Run(() =>
                {
                    var partitioner = System.Collections.Concurrent.Partitioner.Create(0, rowCount);
                    Parallel.ForEach(partitioner, range =>
                    {
                        for (int i = range.Item1; i < range.Item2; i++)
                        {
                            DataRow row = _dtXuLy.Rows[i];

                            if (row.RowState == DataRowState.Deleted || string.IsNullOrWhiteSpace(row[idxHoTen] as string))
                                continue;

                            string t1 = row[idxC1] as string;
                            string t2 = row[idxC2] as string;
                            string t3 = row[idxC3] as string;
                            string t4 = row[idxC4] as string;

                            string res = TinhLogicPhanLoaiMDK_Fast(t1, t2, t3, t4);
                            resultKetQua[i] = res;
                            resultIsLoai1[i] = (res == "Loại 1");
                        }
                    });
                });

                try
                {
                    _dtXuLy.BeginLoadData();
                    for (int i = 0; i < rowCount; i++)
                    {
                        if (resultKetQua[i] != null)
                        {
                            DataRow row = _dtXuLy.Rows[i];
                            row["KetQuaTinhToan"] = resultKetQua[i];
                            row["IsLoai1"] = resultIsLoai1[i];
                        }
                    }
                }
                finally
                {
                    _dtXuLy.EndLoadData();
                }

                //kryptonDataGridView1.DataSource = _dvHienThi;
                CapNhatThongKeStatus();

                _ = Task.Run(() =>
                {
                    try
                    {
                        string logText = $"Dựa trên: [{combos[0]}], [{combos[1]}], [{combos[2]}], [{combos[3]}]. Quân số: {rowCount}.";
                        Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM ?? "Admin", "Tính toán phân loại", logText);
                    }
                    catch { }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi thuật toán hệ thống: " + ex.Message, "Lỗi Nghiêm Trọng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (kryptonDataGridView1.DataSource == null && !this.IsDisposed)
                    kryptonDataGridView1.DataSource = _dvHienThi;

                kryptonButton1_TinhToan.Values.Text = textBanDau;
                kryptonButton1_TinhToan.Values.Image = anhBanDau;
                kryptonButton1_TinhToan.Enabled = true;
                _isCalculating = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private string TinhLogicPhanLoaiMDK_Fast(string t1, string t2, string t3, string t4)
        {
            int l1 = 0, l2 = 0, l3 = 0, l4 = 0;
            int validCount = 0;

            void Check(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return;

                char digit = s.FirstOrDefault(char.IsDigit);

                if (digit == '1') { l1++; validCount++; }
                else if (digit == '2') { l2++; validCount++; }
                else if (digit == '3') { l3++; validCount++; }
                else if (digit == '4') { l4++; validCount++; }
            }

            Check(t1); Check(t2); Check(t3); Check(t4);

            if (validCount == 0) return "Loại 4";
            if (l1 >= 2 && l3 == 0 && l4 == 0) return "Loại 1";
            if ((l1 + l2) >= 2 && l3 == 0 && l4 == 0) return "Loại 2";
            if ((l1 + l2 + l3) >= 2 && l4 == 0) return "Loại 3";

            return "Loại 4";
        }
        // =========================================================================
        // UI & FILTER
        // =========================================================================
        private async void lamMoi_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _isUpdatingCombos = true;

            _ctsLoadNgam?.Cancel();

            kryptonDataGridView1.DataSource = null;
            _dvHienThi?.Dispose();
            _dtXuLy?.Dispose();

            _dtXuLy = _dtGoc.Copy();

            if (!_dtXuLy.Columns.Contains("KetQuaTinhToan")) _dtXuLy.Columns.Add("KetQuaTinhToan", typeof(string));
            if (!_dtXuLy.Columns.Contains("IsLoai1")) _dtXuLy.Columns.Add("IsLoai1", typeof(bool));
            if (!_dtXuLy.Columns.Contains("GhiChu")) _dtXuLy.Columns.Add("GhiChu", typeof(string));

            _dvHienThi = new DataView(_dtXuLy);

            combobox_GiaTriTuanThang_1.Text = "";
            combobox_GiaTriTuanThang_2.Text = "";
            combobox_GiaTriTuanThang_3.Text = "";
            combobox_GiaTriTuanThang_4.Text = "";

            textBox_TimKiemTheoTen.Text = "";
            if (comboBox_TimKiemDonVi.Items.Count > 0) comboBox_TimKiemDonVi.SelectedIndex = 0;
            if (comboBox_XepLoaiThiDua.Items.Count > 0) comboBox_XepLoaiThiDua.SelectedIndex = 0;
            if (comboBox_TimKiemTinhTrang.Items.Count > 0) comboBox_TimKiemTinhTrang.SelectedIndex = 0;

            _isUpdatingCombos = false;

            kryptonDataGridView1.DataSource = _dvHienThi;

            ComboboxThoiGian_Changed(null, null);
            BoLoc_Changed(null, null);
            CapNhatThongKeStatus();

            await KichHoatLoadGhiChuAnToanAsync();
        }
        private void KhoiTaoMapThoiGian()
        {
            _mapCotThoiGian.Clear();
            _danhSachThoiGianGoc.Clear();

            foreach (DataColumn col in _dtXuLy.Columns)
            {
                string name = col.ColumnName;
                if (string.IsNullOrWhiteSpace(name)) continue;

                if (_isTanBinh)
                {
                    if (name.Equals("Thang_2", StringComparison.OrdinalIgnoreCase) ||
                       (name.StartsWith("Tuan_", StringComparison.OrdinalIgnoreCase) && name.EndsWith("_T6", StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }
                }

                if (name.StartsWith("Tuan_") || name.StartsWith("Thang_"))
                {
                    string tenHienThi = name;

                    if (name.StartsWith("Tuan_"))
                    {
                        string[] p = name.Split('_');
                        if (p.Length == 3)
                        {
                            tenHienThi = $"Tuần {p[1]} Tháng {p[2].Replace("T", "")}";
                        }
                    }
                    else if (name.StartsWith("Thang_"))
                    {
                        string[] p = name.Split('_');
                        if (p.Length >= 2)
                        {
                            tenHienThi = $"Tháng {p[1]}";
                        }
                    }

                    _mapCotThoiGian[tenHienThi] = name;

                    if (!_danhSachThoiGianGoc.Contains(tenHienThi))
                    {
                        _danhSachThoiGianGoc.Add(tenHienThi);
                    }
                }
            }

            _danhSachThoiGianGoc = _danhSachThoiGianGoc.OrderBy(item =>
            {
                int thang = 0;
                int tuan = 0;

                string[] parts = item.Split(' ');
                if (item.StartsWith("Tuần") && parts.Length >= 4)
                {
                    int.TryParse(parts[1], out tuan);
                    int.TryParse(parts[3], out thang);
                }
                else if (item.StartsWith("Tháng") && parts.Length >= 2)
                {
                    int thangKetQua = 0;
                    int.TryParse(parts[1], out thangKetQua);
                    thang = thangKetQua - 1;
                    tuan = 5;
                }

                return (thang * 10) + tuan;
            }).ToList();
        }
        private void LoadComboboxThoiGian()
        {
            _isUpdatingCombos = true;
            ComboBox[] combos = { combobox_GiaTriTuanThang_1, combobox_GiaTriTuanThang_2, combobox_GiaTriTuanThang_3, combobox_GiaTriTuanThang_4 };

            foreach (var cb in combos)
            {
                cb.BeginUpdate();
                cb.Items.Clear();
                cb.Items.Add("");
                cb.Items.AddRange(_danhSachThoiGianGoc.ToArray());
                cb.EndUpdate();
            }
            _isUpdatingCombos = false;
        }
        private void ComboboxThoiGian_Changed(object sender, EventArgs e)
        {
            if (_isUpdatingCombos) return;
            _isUpdatingCombos = true;

            string v1 = combobox_GiaTriTuanThang_1.Text;
            string v2 = combobox_GiaTriTuanThang_2.Text;
            string v3 = combobox_GiaTriTuanThang_3.Text;
            string v4 = combobox_GiaTriTuanThang_4.Text;
            List<string> daChon = new List<string> { v1, v2, v3, v4 }.Where(x => !string.IsNullOrEmpty(x)).ToList();

            void RefreshCombo(ComboBox cb, string valCuaNo)
            {
                cb.BeginUpdate();
                cb.Items.Clear();
                cb.Items.Add("");
                foreach (string item in _danhSachThoiGianGoc)
                {
                    if (!daChon.Contains(item) || item == valCuaNo)
                    {
                        cb.Items.Add(item);
                    }
                }
                cb.Text = valCuaNo;
                cb.EndUpdate();
            }

            RefreshCombo(combobox_GiaTriTuanThang_1, v1);
            RefreshCombo(combobox_GiaTriTuanThang_2, v2);
            RefreshCombo(combobox_GiaTriTuanThang_3, v3);
            RefreshCombo(combobox_GiaTriTuanThang_4, v4);

            _isUpdatingCombos = false;
            MapComboboxToGrid();
        }
        private void MapComboboxToGrid()
        {
            void SetGridColumn(string comboValue, string colNameGrid)
            {
                var col = kryptonDataGridView1.Columns[colNameGrid];
                if (string.IsNullOrEmpty(comboValue))
                {
                    col.HeaderText = "Chưa chọn";
                    col.DataPropertyName = "";
                }
                else
                {
                    col.HeaderText = comboValue;
                    col.DataPropertyName = _mapCotThoiGian[comboValue];
                }
            }

            SetGridColumn(combobox_GiaTriTuanThang_1.Text, "CotGiaTri1");
            SetGridColumn(combobox_GiaTriTuanThang_2.Text, "CotGiaTri2");
            SetGridColumn(combobox_GiaTriTuanThang_3.Text, "CotGiaTri3");
            SetGridColumn(combobox_GiaTriTuanThang_4.Text, "CotGiaTri4");
        }
        private void CauHinhLuoiGiaoDien()
        {
            var dgv = kryptonDataGridView1;

            // 1. Khóa vẽ để tránh nhấp nháy và lỗi đồng bộ
            dgv.SuspendLayout();

            try
            {
                // 2. Tối ưu hiệu năng render (bật DoubleBuffered)
                typeof(DataGridView).GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(dgv, true, null);

                dgv.AutoGenerateColumns = false;
                dgv.Columns.Clear();
                dgv.AllowUserToAddRows = false;
                dgv.ReadOnly = true;
                dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

                dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
                dgv.RowTemplate.Height = 30;
                dgv.RowHeadersVisible = false;
                dgv.EnableHeadersVisualStyles = false;

                // Định dạng Header chuyên nghiệp
                dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgv.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
                dgv.ColumnHeadersHeight = 55;
                dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

                // 3. Định nghĩa các cột
                AddColumn(dgv, "STT", "STT", 45, DataGridViewContentAlignment.MiddleCenter);
                AddColumn(dgv, "HoVaTen", "Họ và tên", 220, DataGridViewContentAlignment.MiddleLeft, "HoVaTen");
                AddColumn(dgv, "SoHieu", "Số hiệu", 60, DataGridViewContentAlignment.MiddleCenter, "SoHieu");
                AddColumn(dgv, "DonVi", "Đơn vị", 60, DataGridViewContentAlignment.MiddleCenter, "DonVi");
                AddColumn(dgv, "TinhTrang", "Tình trạng", 60, DataGridViewContentAlignment.MiddleCenter, "TinhTrang");

                for (int i = 1; i <= 4; i++)
                {
                    AddColumn(dgv, $"CotGiaTri{i}", $"Giá trị {i}", 60, DataGridViewContentAlignment.MiddleCenter);
                }

                AddColumn(dgv, "KetQuaPhanLoai", "Kết quả", 90, DataGridViewContentAlignment.MiddleCenter, "KetQuaTinhToan");
                AddColumn(dgv, "colGhiChu", "Ghi chú", 150, DataGridViewContentAlignment.MiddleLeft, "GhiChu");

                // 4. 🔥 GIẢI PHÁP CHO FORM TRẮNG: Gán DataSource sau khi đã cấu hình xong cấu trúc
                if (_dvHienThi != null)
                {
                    dgv.DataSource = _dvHienThi;
                }

                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
            finally
            {
                dgv.ResumeLayout();
            }
        }

        // Hàm bổ trợ để code ngắn gọn, an toàn, tránh lỗi copy-paste
        private void AddColumn(DataGridView dgv, string name, string header, int weight, DataGridViewContentAlignment align, string dataProp = "")
        {
            var col = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                DataPropertyName = dataProp,
                FillWeight = weight
            };
            col.DefaultCellStyle.Alignment = align;
            dgv.Columns.Add(col);
        }
        private void KryptonDataGridView1_CellPainting(
      object sender,
      DataGridViewCellPaintingEventArgs e)
        {
            try
            {
                // =====================================================
                // VALIDATION
                // =====================================================
                if (sender is not DataGridView dgv)
                    return;

                if (e.RowIndex < 0 ||
                    e.ColumnIndex < 0)
                {
                    return;
                }

                if (e.RowIndex >= dgv.Rows.Count ||
                    e.ColumnIndex >= dgv.Columns.Count)
                {
                    return;
                }

                DataGridViewColumn column =
                    dgv.Columns[e.ColumnIndex];

                if (!column.Visible)
                    return;

                string colName = column.Name;

                bool isSelected =
                    (e.State & DataGridViewElementStates.Selected) != 0;

                // =====================================================
                // CỘT STT
                // =====================================================
                // =====================================================
                // CỘT STT
                // =====================================================
                if (colName == "STT")
                {
                    // KHÔNG VẼ BORDER NỮA
                    // Để DataGridView tự render border mặc định
                    e.Paint(
                        e.CellBounds,
                        DataGridViewPaintParts.Background |
                        DataGridViewPaintParts.SelectionBackground);

                    string sttText =
                        (e.RowIndex + 1).ToString();

                    Color textColor =
                        isSelected
                        ? e.CellStyle.SelectionForeColor
                        : e.CellStyle.ForeColor;

                    TextRenderer.DrawText(
                        e.Graphics,
                        sttText,
                        e.CellStyle.Font,
                        e.CellBounds,
                        textColor,
                        TextFormatFlags.HorizontalCenter |
                        TextFormatFlags.VerticalCenter |
                        TextFormatFlags.EndEllipsis |
                        TextFormatFlags.NoPadding);

                    e.Handled = true;
                    return;
                }

                // =====================================================
                // CỘT HỌ VÀ TÊN
                // =====================================================
                if (colName == "HoVaTen")
                {
                    if (dgv.Rows[e.RowIndex].DataBoundItem
                        is not DataRowView drv)
                    {
                        return;
                    }

                    bool isLoai1 =
                        drv.Row.Table.Columns.Contains("IsLoai1") &&
                        drv.Row.Field<bool?>("IsLoai1") == true;

                    // Không phải loại 1
                    if (!isLoai1)
                        return;

                    Image icon = _iconStar;

                    if (icon == null)
                        return;

                    // =================================================
                    // VẼ NỀN + BORDER
                    // KHÔNG VẼ TEXT MẶC ĐỊNH
                    // =================================================
                    e.Paint(
                        e.CellBounds,
                        DataGridViewPaintParts.Background |
                        //DataGridViewPaintParts.Border |
                        DataGridViewPaintParts.SelectionBackground);

                    // =================================================
                    // ICON
                    // =================================================
                    const int iconSize = 16;
                    const int paddingLeft = 6;
                    const int paddingText = 6;

                    int xIcon =
                        e.CellBounds.X + paddingLeft;

                    int yIcon =
                        e.CellBounds.Y +
                        ((e.CellBounds.Height - iconSize) / 2);

                    e.Graphics.DrawImage(
                        icon,
                        xIcon,
                        yIcon,
                        iconSize,
                        iconSize);

                    // =================================================
                    // TEXT
                    // =================================================
                    string hoTen =
                        drv.Row.Field<string>("HoVaTen") ?? "";

                    if (!string.IsNullOrWhiteSpace(hoTen))
                    {
                        int textX =
                            xIcon + iconSize + paddingText;

                        Rectangle textBounds =
                            new Rectangle(
                                textX,
                                e.CellBounds.Y,
                                e.CellBounds.Width - (textX - e.CellBounds.X),
                                e.CellBounds.Height);

                        Color textColor =
                            isSelected
                            ? e.CellStyle.SelectionForeColor
                            : e.CellStyle.ForeColor;

                        TextRenderer.DrawText(
                            e.Graphics,
                            hoTen,
                            e.CellStyle.Font,
                            textBounds,
                            textColor,
                            TextFormatFlags.Left |
                            TextFormatFlags.VerticalCenter |
                            TextFormatFlags.EndEllipsis |
                            TextFormatFlags.NoPadding);
                    }

                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"CellPainting Error: {ex}");
            }
        }
        private void ThietLapToolTip()
        {
            if (toolTip1 != null)
            {
                toolTip1.ToolTipIcon = ToolTipIcon.Info;
                toolTip1.ToolTipTitle = "Gợi ý chức năng";
                toolTip1.AutoPopDelay = 6000;
                toolTip1.InitialDelay = 400;
                toolTip1.ReshowDelay = 100;

                toolTip1.SetToolTip(kryptonButton1_TinhToan, "Bắt đầu tính toán phân loại thi đua (Loại 1, 2, 3, 4)\ndựa trên 4 mốc thời gian Tuần/Tháng mà đồng chí đã chọn.");
                toolTip1.SetToolTip(kryptonButton_LamMoiCacOTimKiem, "Xóa bỏ các điều kiện tìm kiếm hiện tại để hiển thị lại toàn bộ quân số.");
                toolTip1.SetToolTip(kryptonButton1_Dong, "Đóng cửa sổ làm việc này và quay trở về màn hình trước đó.");
            }
        }
        private void KryptonDataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || kryptonDataGridView1.Rows[e.RowIndex].DataBoundItem is not DataRowView rv)
                return;

            try
            {
                string Get(string col) => rv.Row.Table.Columns.Contains(col) ? rv[col]?.ToString()?.Trim() ?? "" : "";
                string Fix(string v) => string.IsNullOrWhiteSpace(v) ? "0" : v;

                string hoTen = Get("HoVaTen").ToUpper();
                string donVi = Get("DonVi");
                string soHieu = Get("SoHieu");
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Họ và tên: {hoTen}");
                sb.AppendLine($"Số hiệu: {soHieu}  |  Đơn vị: {donVi}");

                bool coDuLieuLogic = false;

                for (int m = 2; m <= 5; m++)
                {
                    int thangKetQua = m + 1;
                    bool coDuLieuThangNay = false;
                    StringBuilder sbThang = new StringBuilder();
                    for (int w = 1; w <= 4; w++)
                    {
                        string val = Get($"Tuan_{w}_T{m}");
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            sbThang.AppendLine($"  + Tuần {w} - Tháng {m}: Loại {val}");
                            coDuLieuThangNay = true;
                        }
                    }

                    string kq = Get($"Thang_{thangKetQua}");
                    if (!string.IsNullOrWhiteSpace(kq))
                    {
                        sbThang.AppendLine($"  => Kết quả thi đua Tháng {thangKetQua}: Loại {kq.ToUpper()}");
                        coDuLieuThangNay = true;
                    }

                    if (coDuLieuThangNay)
                    {
                        sb.Append(sbThang.ToString());
                        coDuLieuLogic = true;
                    }
                }

                if (!coDuLieuLogic)
                {
                    sb.AppendLine("Chưa có dữ liệu đánh giá tuần hoặc kết quả tháng.");
                }

                sb.AppendLine("TỔNG CỘNG CHỈ TIÊU:");
                sb.AppendLine($" - Loại 1: {Fix(Get("TS_Loai1"))} lần");
                sb.AppendLine($" - Loại 2: {Fix(Get("TS_Loai2"))} lần");
                sb.AppendLine($" - Loại 3: {Fix(Get("TS_Loai3"))} lần");
                sb.AppendLine($" - Loại 4: {Fix(Get("TS_Loai4"))} lần");

                MessageBox.Show(sb.ToString(), "Chi tiết hồ sơ thi đua", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi hiển thị chi tiết: {ex.Message}");
            }
        }
        private void kryptonButton1_Dong_Click(object sender, EventArgs e) => this.Close();
        private void UpdateLabels(int tongSo, int l1, int l2, int l3, int l4)
        {
            toolStripStatusLabel1.Text = $"Tổng quân số: {tongSo} đồng chí";

            toolStripStatusLabel2.Visible = (l1 > 0);
            toolStripStatusLabel2.Text = $"Loại 1: {l1} đồng chí";

            toolStripStatusLabel3.Visible = (l2 > 0);
            toolStripStatusLabel3.Text = $"Loại 2: {l2} đồng chí";

            toolStripStatusLabel4.Visible = (l3 > 0);
            toolStripStatusLabel4.Text = $"Loại 3: {l3} đồng chí";

            toolStripStatusLabel5.Visible = (l4 > 0);
            toolStripStatusLabel5.Text = $"Loại 4: {l4} đồng chí";
        }
        private void CauHinhUI_StatusStrip()
        {
            var danhSachLabels = new[] {
                toolStripStatusLabel1,
                toolStripStatusLabel2,
                toolStripStatusLabel3,
                toolStripStatusLabel4,
                toolStripStatusLabel5
            };

            foreach (var lbl in danhSachLabels)
            {
                if (lbl != null)
                {
                    lbl.Font = new Font(lbl.Font, FontStyle.Regular);
                    lbl.Spring = true;
                    lbl.TextAlign = ContentAlignment.MiddleCenter;
                }
            }
        }
        private void LoadComboboxDonVi()
        {
            comboBox_TimKiemDonVi.Items.Clear();
            comboBox_TimKiemDonVi.Items.Add("Tất cả");
            var dsDonVi = _dtXuLy.AsEnumerable().Select(r => r.Field<string>("DonVi")).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x);
            foreach (var dv in dsDonVi) comboBox_TimKiemDonVi.Items.Add(dv);
            comboBox_TimKiemDonVi.SelectedIndex = 0;

            comboBox_XepLoaiThiDua.Items.Clear();
            comboBox_XepLoaiThiDua.Items.AddRange(new string[] { "Tất cả", "Loại 1", "Loại 2", "Loại 3", "Loại 4" });
            comboBox_XepLoaiThiDua.SelectedIndex = 0;

            comboBox_TimKiemTinhTrang.Items.Clear();
            comboBox_TimKiemTinhTrang.Items.AddRange(new string[] { "Tất cả", "Đang công tác", "Chuyển công tác" });
            comboBox_TimKiemTinhTrang.SelectedIndex = 0;
        }
        private string EscapeFilter(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("'", "''")
                        .Replace("[", "[[]")
                        .Replace("]", "[]]")
                        .Replace("*", "[*]")
                        .Replace("%", "[%]");
        }
        private async void BoLoc_Changed(object sender, EventArgs e)
        {
            if (_isUpdatingCombos || _dvHienThi == null) return;

            _ctsFilter?.Cancel();
            _ctsFilter = new CancellationTokenSource();
            var token = _ctsFilter.Token;

            try
            {
                await Task.Delay(300, token);

                string filter = BuildFilterString(); // Hàm tách logic filter

                // GIA CỐ: Tạm dừng đồng bộ của BindingContext để Grid không "nhìn" thấy thay đổi
                this.BindingContext[kryptonDataGridView1.DataSource].SuspendBinding();

                lock (_dataLock)
                {
                    _dvHienThi.RowFilter = filter;
                }

                this.BindingContext[kryptonDataGridView1.DataSource].ResumeBinding();

                CapNhatThongKeStatus();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Debug.WriteLine("Lỗi Filter: " + ex.Message); }
        }
        private string BuildFilterString()
        {
            string ten = EscapeFilter(textBox_TimKiemTheoTen.Text.Trim());
            string dv = comboBox_TimKiemDonVi.Text == "Tất cả" ? "" : EscapeFilter(comboBox_TimKiemDonVi.Text);
            string loai = comboBox_XepLoaiThiDua.Text == "Tất cả" ? "" : EscapeFilter(comboBox_XepLoaiThiDua.Text);
            string tinhTrang = comboBox_TimKiemTinhTrang.Text == "Tất cả" ? "" : EscapeFilter(comboBox_TimKiemTinhTrang.Text);

            return string.Join(" AND ", new List<string> {
        !string.IsNullOrEmpty(ten) ? $"HoVaTen LIKE '%{ten}%'" : "",
        !string.IsNullOrEmpty(dv) ? $"DonVi = '{dv}'" : "",
        !string.IsNullOrEmpty(loai) ? $"KetQuaTinhToan = '{loai}'" : "",
        !string.IsNullOrEmpty(tinhTrang) ? $"TinhTrang = '{tinhTrang}'" : ""
    }.Where(s => !string.IsNullOrEmpty(s)));
        }
        //private async void BoLoc_Changed(object sender, EventArgs e)
        //{
        //    if (_isUpdatingCombos || _dvHienThi == null) return;

        //    _ctsFilter?.Cancel();
        //    _ctsFilter = new CancellationTokenSource();
        //    var token = _ctsFilter.Token;

        //    try
        //    {
        //        await Task.Delay(300, token); // Debounce

        //        string ten = EscapeFilter(textBox_TimKiemTheoTen.Text.Trim());
        //        string dv = comboBox_TimKiemDonVi.Text == "Tất cả" ? "" : EscapeFilter(comboBox_TimKiemDonVi.Text);
        //        string loai = comboBox_XepLoaiThiDua.Text == "Tất cả" ? "" : EscapeFilter(comboBox_XepLoaiThiDua.Text);
        //        string tinhTrang = comboBox_TimKiemTinhTrang.Text == "Tất cả" ? "" : EscapeFilter(comboBox_TimKiemTinhTrang.Text);

        //        string filter = string.Join(" AND ", new List<string> {
        //    !string.IsNullOrEmpty(ten) ? $"HoVaTen LIKE '%{ten}%'" : "",
        //    !string.IsNullOrEmpty(dv) ? $"DonVi = '{dv}'" : "",
        //    !string.IsNullOrEmpty(loai) ? $"KetQuaTinhToan = '{loai}'" : "",
        //    !string.IsNullOrEmpty(tinhTrang) ? $"TinhTrang = '{tinhTrang}'" : ""
        //}.Where(s => !string.IsNullOrEmpty(s)));

        //        // 🔥 CƠ CHẾ AN TOÀN: Đẩy thao tác lên luồng UI và khóa DataSource
        //        this.Invoke(new Action(() =>
        //        {
        //            kryptonDataGridView1.DataSource = null; // Ngắt kết nối để không vẽ tranh trong lúc lọc
        //            lock (_dataLock)
        //            {
        //                _dvHienThi.RowFilter = filter;
        //            }
        //            kryptonDataGridView1.DataSource = _dvHienThi; // Gán lại sau khi lọc xong
        //            CapNhatThongKeStatus();
        //        }));
        //    }
        //    catch (OperationCanceledException) { }
        //    catch (Exception ex) { Debug.WriteLine("Lỗi Filter: " + ex.Message); }
        //}
        private void CapNhatThongKeStatus()
        {
            if (this.IsDisposed || _dvHienThi == null) return;

            // Lấy dữ liệu an toàn ra một List để đếm, không duyệt trực tiếp DataView
            List<string> ketQuaList = new List<string>();

            lock (_dataLock) // Đảm bảo đồng bộ với BoLoc_Changed
            {
                try
                {
                    foreach (DataRowView rowView in _dvHienThi)
                    {
                        // Chỉ lấy những dòng không bị xóa/ẩn
                        if (rowView.Row.RowState == DataRowState.Deleted) continue;

                        string hoTen = rowView["HoVaTen"]?.ToString();
                        if (string.IsNullOrWhiteSpace(hoTen)) continue;

                        ketQuaList.Add(rowView["KetQuaTinhToan"]?.ToString()?.Trim() ?? "");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Lỗi khi đọc DataView: " + ex.Message);
                    return;
                }
            }

            // Đếm trên List đã copy (hoàn toàn an toàn)
            int tongSo = ketQuaList.Count;
            int loai1 = ketQuaList.Count(k => k == "Loại 1");
            int loai2 = ketQuaList.Count(k => k == "Loại 2");
            int loai3 = ketQuaList.Count(k => k == "Loại 3");
            int loai4 = ketQuaList.Count(k => k == "Loại 4");

            // Cập nhật UI
            if (this.InvokeRequired)
                this.Invoke(new Action(() => UpdateLabels(tongSo, loai1, loai2, loai3, loai4)));
            else
                UpdateLabels(tongSo, loai1, loai2, loai3, loai4);
        }
        private void kryptonButton_LamMoiCacOTimKiem_Click(object sender, EventArgs e)
        {
            textBox_TimKiemTheoTen.Text = "";
            if (comboBox_TimKiemDonVi.Items.Count > 0) comboBox_TimKiemDonVi.SelectedIndex = 0;
            if (comboBox_XepLoaiThiDua.Items.Count > 0) comboBox_XepLoaiThiDua.SelectedIndex = 0;
            if (comboBox_TimKiemTinhTrang.Items.Count > 0) comboBox_TimKiemTinhTrang.SelectedIndex = 0;

            if (_dvHienThi != null) _dvHienThi.RowFilter = "";
            CapNhatThongKeStatus();
        }
        private void troVeThongKe_ToolStripMenuItem_Click(object sender, EventArgs e) => kryptonButton1_Dong.PerformClick();
        private void xuatDuLieu_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_dvHienThi == null || _dvHienThi.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool coDuLieuThucTe = false;
            foreach (DataRowView drv in _dvHienThi)
            {
                string hoTen = drv["HoVaTen"]?.ToString();
                if (!string.IsNullOrWhiteSpace(hoTen))
                {
                    coDuLieuThucTe = true;
                    break;
                }
            }

            if (!coDuLieuThucTe)
            {
                MessageBox.Show("Không có dữ liệu hợp lệ để xuất (danh sách trống)!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_isCalculating)
            {
                MessageBox.Show("Hệ thống đang tính toán, vui lòng đợi trong giây lát!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string h1 = combobox_GiaTriTuanThang_1.Text;
            string h2 = combobox_GiaTriTuanThang_2.Text;
            string h3 = combobox_GiaTriTuanThang_3.Text;
            string h4 = combobox_GiaTriTuanThang_4.Text;

            if (string.IsNullOrEmpty(h1) || string.IsNullOrEmpty(h2) || string.IsNullOrEmpty(h3) || string.IsNullOrEmpty(h4))
            {
                var result = MessageBox.Show(
                    "Đồng chí chưa chọn đủ giá trị thi đua (Tuần - Tháng) để đưa vào xuất báo cáo.\n\nĐồng chí có muốn tiếp tục mở giao diện xuất tệp không?",
                    "Xác nhận",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.No) return;

                if (string.IsNullOrEmpty(h1)) h1 = "Kết quả Tuần 1";
                if (string.IsNullOrEmpty(h2)) h2 = "Kết quả Tuần 2";
                if (string.IsNullOrEmpty(h3)) h3 = "Kết quả Tuần 3";
                if (string.IsNullOrEmpty(h4)) h4 = "Kết quả Tuần 4";
            }

            var f = new Form38_XuatExcelTinhToan(_dvHienThi, _mapCotThoiGian, h1, h2, h3, h4);
            f.ShowDialog();
        }
    }
}