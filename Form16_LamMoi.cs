///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
using Microsoft.Data.Sqlite;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Form "Làm mới phân loại": đưa những người chưa thuộc diện "giữ nguyên" về Loại 2 trong các đơn vị được chọn.
    /// Form chỉ trả kết quả qua DialogResult / SoDongDaCapNhat. Nó KHÔNG còn tự gọi Form6.RefreshCSDL():
    /// form cha quyết định có làm mới (và hiện thanh tiến trình) hay không dựa vào DialogResult.
    /// </summary>
    public partial class Form16_LamMoi : Form
    {
        private static string DuongDanCsdl2 => Module_DanduongGPS.DuongDanCSDL2;   // đọc mỗi lần dùng: CSDL có thể bị khôi phục/đổi đường dẫn
        private const string TextNutGoc = "Đặt lại";
        private bool _isProcessing;
        private bool _isClosingRequested;
        private CancellationTokenSource? _cts;
        // Ghi nhớ lựa chọn trong 1 phiên làm việc (RAM)
        private static bool _daXacNhanResetTrongSession;
        private static bool _memLoai1, _memLoai3, _memLoai4, _memKhongPL;
        /// <summary>Số dòng thực sự đã được đổi về Loại 2 (0 nếu người dùng thoát / không có gì để đổi).</summary>
        public int SoDongDaCapNhat { get; private set; }
        public Form16_LamMoi()
        {
            InitializeComponent();   // Designer đã nối sự kiện Load + nút Đặt lại -> KHÔNG nối lại (bản cũ làm Load chạy 2 lần)
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            checkedListBox1_ChonDonViDeReset.CheckOnClick = true;
            FormClosing += Form16_LamMoi_FormClosing;
            InitCheckBoxEvents();
            InitToolTips();
            Module_GiaoDienForm16Web.ApDung(this);   // giao diện kiểu web (chỉ trình bày)
        }

        //  NẠP FORM

        private async void Form16_LamMoi_Load(object? sender, EventArgs e)
        {
            checkBox1_GuiNguyenLoai1.Checked = _memLoai1;
            checkBox1_GuiNguyenLoai3.Checked = _memLoai3;
            checkBox1_GuiNguyenLoai4.Checked = _memLoai4;
            checkBox1_GuiNguyenKhongPhanLoai.Checked = _memKhongPL;
            checkBox1_GuiNguyenLoai2.AutoCheck = false;   // Loại 2 là đích đến: luôn giữ nguyên, không cho đổi
            checkBox1_GuiNguyenLoai2.Checked = true;
            UpdateLabelColors();
            await LoadDanhSachDonViAsync();
        }
        /// <summary>Nạp danh sách đơn vị (giải mã AES, sắp theo thứ tự ưu tiên), mặc định tích TẤT CẢ.</summary>
        private async Task LoadDanhSachDonViAsync()
        {
            try
            {
                string db = DuongDanCsdl2;
                var dsDonVi = await Task.Run(() =>
                {
                    var ds = new List<string>();
                    using var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(db, chiDoc: true));
                    cn.Open();
                    using var cmd = cn.CreateCommand();
                    cmd.CommandText = $"SELECT Ten_DonVi FROM {Module_DonVi.LayTenBangDonVi()} ORDER BY ID ASC";
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        string raw = rd.IsDBNull(0) ? "" : rd.GetString(0).Trim();
                        if (raw.Length == 0) continue;
                        try
                        {
                            string dec = Module_BaoMatAES.GiaiMa(raw);
                            if (!string.IsNullOrWhiteSpace(dec)) ds.Add(dec.Trim());
                        }
                        catch { /* bỏ qua dòng lỗi giải mã */ }
                    }
                    return ds;
                });
                if (IsDisposed) return;
                string[] thuTu = Module_DonVi.LayDanhSachDonViUuTienArray();
                var sorted = dsDonVi.OrderBy(x => { int i = Array.IndexOf(thuTu, x); return i < 0 ? int.MaxValue : i; }).ToList();
                var clb = checkedListBox1_ChonDonViDeReset;
                clb.BeginUpdate();
                try
                {
                    clb.Items.Clear();
                    foreach (var dv in sorted) clb.Items.Add(dv, true);
                }
                finally { clb.EndUpdate(); }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi nạp đơn vị Form 16: {ex.Message}");
            }
        }
        private void Form16_LamMoi_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (!_isProcessing) return;
            e.Cancel = true;                      // đang ghi CSDL: không đóng ngang, yêu cầu dừng an toàn rồi mới đóng
            if (_isClosingRequested) return;
            _isClosingRequested = true;
            btn_ResetPhanLoai.Text = "Đang thoát an toàn...";
            _cts?.Cancel();
        }

        //  GIAO DIỆN PHỤ TRỢ

        private void InitCheckBoxEvents()
        {
            checkBox1_GuiNguyenLoai1.CheckedChanged += (s, e) => { UpdateLabelColors(); _memLoai1 = checkBox1_GuiNguyenLoai1.Checked; };
            checkBox1_GuiNguyenLoai2.CheckedChanged += (s, e) => UpdateLabelColors();
            checkBox1_GuiNguyenLoai3.CheckedChanged += (s, e) => { UpdateLabelColors(); _memLoai3 = checkBox1_GuiNguyenLoai3.Checked; };
            checkBox1_GuiNguyenLoai4.CheckedChanged += (s, e) => { UpdateLabelColors(); _memLoai4 = checkBox1_GuiNguyenLoai4.Checked; };
            checkBox1_GuiNguyenKhongPhanLoai.CheckedChanged += (s, e) => { UpdateLabelColors(); _memKhongPL = checkBox1_GuiNguyenKhongPhanLoai.Checked; };
        }
        // Giữ nguyên = xanh lá (không bị đổi), sẽ bị đặt lại về Loại 2 = xám đậm
        private void UpdateLabelColors()
        {
            Color giu = Color.FromArgb(22, 128, 61), doi = Color.FromArgb(71, 85, 105);
            label1.ForeColor = checkBox1_GuiNguyenLoai1.Checked ? giu : doi;
            label2.ForeColor = checkBox1_GuiNguyenLoai2.Checked ? giu : doi;
            label3.ForeColor = checkBox1_GuiNguyenLoai3.Checked ? giu : doi;
            label4.ForeColor = checkBox1_GuiNguyenLoai4.Checked ? giu : doi;
            label5.ForeColor = checkBox1_GuiNguyenKhongPhanLoai.Checked ? giu : doi;
        }
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
            toolTip1.SetToolTip(checkBox1_GuiNguyenLoai1, "Tích chọn để không thay đổi những người đã đạt Loại 1");
            toolTip1.SetToolTip(btn_ResetPhanLoai, "Đưa những người không được giữ nguyên về Loại 2");
        }
        private HashSet<string> GetKeepList()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (checkBox1_GuiNguyenLoai1.Checked) set.Add(Module_HeThong.Loai_1);
            if (checkBox1_GuiNguyenLoai2.Checked) set.Add(Module_HeThong.Loai_2);
            if (checkBox1_GuiNguyenLoai3.Checked) set.Add(Module_HeThong.Loai_3);
            if (checkBox1_GuiNguyenLoai4.Checked) set.Add(Module_HeThong.Loai_4);
            if (checkBox1_GuiNguyenKhongPhanLoai.Checked) set.Add(Module_HeThong.PL_KHONG_PL);
            return set;
        }

        //  THỰC HIỆN

        private async void btn_ResetPhanLoai_Click(object? sender, EventArgs e)
        {
            if (_isProcessing) return;
            var chon = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var boQua = new List<string>();
            var clb = checkedListBox1_ChonDonViDeReset;
            for (int i = 0; i < clb.Items.Count; i++)
            {
                string dv = clb.Items[i]?.ToString()?.Trim() ?? "";
                if (dv.Length == 0) continue;
                if (clb.GetItemChecked(i)) chon.Add(dv); else boQua.Add(dv);
            }
            if (chon.Count == 0)
            {
                MessageBox.Show("Vui lòng tích chọn ít nhất một đơn vị để làm mới dữ liệu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!_daXacNhanResetTrongSession)
            {
                string msg = "Các đơn vị được chọn để đặt lại phân loại:\n" +
                             $"- Bao gồm: {string.Join("; ", chon)}\n" +
                             $"- Đơn vị bỏ qua: {(boQua.Count > 0 ? string.Join("; ", boQua) : "Không có")}\n\n" +
                             "Bạn có chắc chắn muốn tiếp tục thực hiện không?";
                if (MessageBox.Show(msg, "Xác nhận quyền Admin - Đặt lại phân loại", MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                _daXacNhanResetTrongSession = true;
            }
            var keepList = GetKeepList();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            var progress = new Progress<int>(p => { if (!_isClosingRequested && !IsDisposed) btn_ResetPhanLoai.Text = $"Đang xử lý: {p}%"; });
            bool dongForm = false;
            try
            {
                _isProcessing = true;
                btn_ResetPhanLoai.Enabled = false;
                Cursor = Cursors.WaitCursor;
                var (soDong, loi) = await Task.Run(() => ThucThiResetDuLieu(keepList, chon, progress, token));
                if (loi == "CANCELED") return;
                if (!string.IsNullOrEmpty(loi))
                {
                    MessageBox.Show("Có lỗi trong quá trình cập nhật CSDL.\nChi tiết: " + loi, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (soDong == 0)
                {
                    // Không ai bị đổi -> không cần làm mới form cha, không bật thanh tiến trình
                    Module_ThongBao.ThanhCong("Không có ai cần đặt lại phân loại.");
                    return;
                }
                SoDongDaCapNhat = soDong;
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Reset Phân Loại", $"Số lượng: {soDong} CBCS thuộc {chon.Count} đơn vị");
                btn_ResetPhanLoai.Text = $"Hoàn tất! ({soDong} CBCS)";
                await Task.Delay(400);
                dongForm = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Form16 lỗi: {ex}");
                MessageBox.Show($"Đã xảy ra lỗi hệ thống: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isProcessing = false;
                _cts?.Dispose();
                _cts = null;
                if (!IsDisposed)
                {
                    Cursor = Cursors.Default;
                    if (_isClosingRequested) { DialogResult = SoDongDaCapNhat > 0 ? DialogResult.OK : DialogResult.Cancel; }
                    else if (dongForm) DialogResult = DialogResult.OK;   // chỉ OK khi thật sự có thay đổi -> form cha mới làm mới
                    else
                    {
                        btn_ResetPhanLoai.Text = TextNutGoc;
                        btn_ResetPhanLoai.Enabled = true;
                    }
                }
            }
        }
        private (int soDong, string loi) ThucThiResetDuLieu(HashSet<string> keepList, HashSet<string> donViChon, IProgress<int> progress, CancellationToken token)
        {
            var ids = new List<long>();
            var cacheDonVi = new Dictionary<string, string>();   // chống giải mã AES trùng lặp
            var cachePhanLoai = new Dictionary<string, string>();
            try
            {
                var builder = new SqliteConnectionStringBuilder { DataSource = DuongDanCsdl2, DefaultTimeout = 30 };
                using var conn = new SqliteConnection(builder.ConnectionString);
                conn.Open();
                // PHASE 1: đọc + lọc (không còn quick_check / đổi journal_mode mỗi lần bấm)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT ID, PhanLoai, DonVi FROM {Module_TaiKhoan.BangDanhSach} WHERE PhanLoai IS NOT NULL AND PhanLoai <> ''";
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        token.ThrowIfCancellationRequested();
                        string dvMa = rd.IsDBNull(2) ? "" : rd.GetString(2);
                        if (!cacheDonVi.TryGetValue(dvMa, out string? dv))
                        {
                            try { dv = Module_BaoMatAES.GiaiMa(dvMa)?.Trim() ?? ""; } catch { dv = ""; }
                            cacheDonVi[dvMa] = dv;
                        }
                        if (!donViChon.Contains(dv)) continue;
                        string plMa = rd.GetString(1);
                        if (!cachePhanLoai.TryGetValue(plMa, out string? pl))
                        {
                            try { pl = Module_BaoMatAES.GiaiMa(plMa)?.Trim() ?? ""; } catch { pl = ""; }
                            cachePhanLoai[plMa] = pl;
                        }
                        string check = string.IsNullOrEmpty(pl) ? "Không PL" : pl;
                        if (!keepList.Contains(check)) ids.Add(rd.GetInt64(0));
                    }
                }
                int total = ids.Count;
                if (total == 0) return (0, "");
                // PHASE 2: cập nhật 1 transaction, lệnh dựng sẵn
                string loai2Ma = Module_BaoMatAES.MaHoa(Module_HeThong.Loai_2);
                int rows = 0;
                using var tran = conn.BeginTransaction();
                try
                {
                    using var up = conn.CreateCommand();
                    up.Transaction = tran;
                    up.CommandText = $"UPDATE {Module_TaiKhoan.BangDanhSach} SET PhanLoai = @pl WHERE ID = @id";
                    up.Parameters.AddWithValue("@pl", loai2Ma);
                    var pId = up.Parameters.Add("@id", SqliteType.Integer);
                    int buoc = Math.Max(1, total / 100);
                    for (int i = 0; i < total; i++)
                    {
                        token.ThrowIfCancellationRequested();
                        pId.Value = ids[i];
                        rows += up.ExecuteNonQuery();
                        if (i % buoc == 0 || i == total - 1) progress.Report((i + 1) * 100 / total);
                    }
                    tran.Commit();
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
                return (rows, "");
            }
            catch (OperationCanceledException) { return (0, "CANCELED"); }
            catch (Exception ex) { return (0, ex.Message); }
        }
    }
}
