using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace PhanMemThiDua2026
{
    public partial class Form50_QuanLyKhenThuongNamCu : Form
    {        
        // KHAI BÁO DTO VÀ BIẾN TOÀN CỤC CHO KHEN THƯỞNG CÁ NHÂN
        private class HistoryGiayKhenDTO
        {
            public int ID { get; set; }
            public int STT { get; set; }
            public string HoVaTen { get; set; }
            public string HoVaTen_Search { get; set; }
            public string SoHieu { get; set; }
            public string DonVi { get; set; }
            public string TinhTrang { get; set; }
            public string HinhThuc_Khen { get; set; }
            public string QuyetDinh_Khen { get; set; }
            public string NgayCapQD_Khen { get; set; }
            public string DonVi_Khen { get; set; }
            public string VeViec_Khen { get; set; }
            public string GhiChu_Khen { get; set; }
            public int SortPriority { get; set; }
        }
        private List<HistoryGiayKhenDTO> _dataCacheGiayKhen = new List<HistoryGiayKhenDTO>();
        private List<int> _filteredIndexes = new List<int>();
        private const string PLACEHOLDER_TIMKIEM = "Nhập họ và tên để tìm kiếm...";
        private bool _dangSetPlaceholder = false;
        private bool _isInitialized = false;
        private SolidBrush _rowHeaderBrush = null;
        private StringFormat _rowHeaderFormat = null;
        private System.Windows.Forms.Timer _timKiemTimer;
        public Form50_QuanLyKhenThuongNamCu()
        {
            InitializeComponent();
            this.KeyPreview = true;
            this.VisibleChanged += Form50_VisibleChanged;

            kryptonDataGridView1.VirtualMode = true;
            kryptonDataGridView1.DataSource = null;

            kryptonDataGridView1.AllowUserToAddRows = false;
            kryptonDataGridView1.AllowUserToDeleteRows = false;
            kryptonDataGridView1.AllowUserToResizeRows = false;
            kryptonDataGridView1.AllowUserToOrderColumns = false;
            kryptonDataGridView1.ReadOnly = true;

            // ⭐ THÊM TẠI ĐÂY: Bật chế độ bấm vào là chọn nguyên cả dòng
            kryptonDataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // ⭐ THÊM TẠI ĐÂY: Chỉ cho phép chọn 1 dòng duy nhất tại 1 thời điểm (Tắt tính năng quét chọn nhiều dòng)
            kryptonDataGridView1.MultiSelect = false;

            // ⭐ SỬA TẠI ĐÂY: Chuyển từ Fill thành None
            kryptonDataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            kryptonDataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            kryptonDataGridView1.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;

            typeof(DataGridView).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(kryptonDataGridView1, true, null);

            kryptonDataGridView1.CellValueNeeded += KryptonDataGridView1_CellValueNeeded;
            kryptonDataGridView1.RowPostPaint += KryptonDataGridView1_RowPostPaint;
        }
        private void Form50_ThongKeKhenThuongNamCu_Load(object sender, EventArgs e)
        {
            if (_isInitialized) return;

            Module_DonVi.KhoiTao();
            CauHinhGridCoBan(kryptonDataGridView1);
            CauHinhCotGrid(kryptonDataGridView1);

            kryptonDataGridView1.ContextMenuStrip = contextMenuStrip1;
            Module_MenuChuotPhai.TichHopGiaoDienXanhLa(contextMenuStrip1);

            if (comboBox_ChonCSDLNam != null)
            {
                comboBox_ChonCSDLNam.SelectedIndexChanged -= comboBox_ChonCSDLNam_SelectedIndexChanged;
                comboBox_ChonCSDLNam.SelectedIndexChanged += comboBox_ChonCSDLNam_SelectedIndexChanged;
            }

            if (comboBox_TimKiemDonVi != null)
                comboBox_TimKiemDonVi.SelectedIndexChanged -= ComboBoxFilter_SelectedIndexChanged;
            comboBox_TimKiemDonVi.SelectedIndexChanged += ComboBoxFilter_SelectedIndexChanged;
            if (comboBox1_HinhThucKT != null)
                comboBox1_HinhThucKT.SelectedIndexChanged -= ComboBoxFilter_SelectedIndexChanged;
            comboBox1_HinhThucKT.SelectedIndexChanged += ComboBoxFilter_SelectedIndexChanged;

            InitPlaceholderTimKiem();
            if (textBox_TimKiemTheoTen != null)
            {
                textBox_TimKiemTheoTen.TextChanged += textBox_TimKiemTheoTen_TextChanged;
                textBox_TimKiemTheoTen.Enter += TextBox_TimKiemTheoTen_Enter;
                textBox_TimKiemTheoTen.Leave += TextBox_TimKiemTheoTen_Leave;
            }

            _timKiemTimer = new System.Windows.Forms.Timer();
            _timKiemTimer.Interval = 300;
            _timKiemTimer.Tick += (s, ev) =>
            {
                _timKiemTimer.Stop();
                ApplyFilter();
            };

            LoadDanhSachFileLichSu();
            InitToolTips();
            _isInitialized = true;
        }
        private void InitToolTips()
        {
            // ============================================================
            // KHỞI TẠO TOOLTIP - ỔN ĐỊNH CHO HỆ THỐNG NỘI BỘ
            // ============================================================

            // 1. Kiểm tra ToolTip
            if (toolTip1 == null)
                return;

            try
            {
                // 2. Cấu hình chung
                toolTip1.IsBalloon = true;
                toolTip1.ToolTipTitle = "Gợi ý thao tác";
                toolTip1.ToolTipIcon = ToolTipIcon.Info;

                // Thời gian chờ trước khi hiển thị
                toolTip1.InitialDelay = 300;

                // Thời gian Tooltip hiển thị
                toolTip1.AutoPopDelay = 2500;

                // Thời gian chờ khi chuyển sang Control khác
                toolTip1.ReshowDelay = 100;

                // Cho phép hiển thị ngay cả khi Form chưa active
                toolTip1.ShowAlways = true;

                // 3. Gán Tooltip cho từng Control

                GanToolTipAnToan(
                    kryptonButton1_TraCuuKetQuaKhenThuongNamCu,
                    "Tra cứu kết quả khen thưởng năm cũ");

                GanToolTipAnToan(
                    kryptonButton_LamMoiCacOTimKiem,
                    "Xóa bộ lọc tìm kiếm hiện tại");

                GanToolTipAnToan(
                    kryptonButton_CapNhat,
                    "Tải lại dữ liệu lịch sử");

                GanToolTipAnToan(
                    kryptonButton_XuatData,
                    "Xuất dữ liệu ra tệp Excel");

                GanToolTipAnToan(
                    kryptonButton_Dong,
                    "Đóng màn hình hiện tại");
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
        private void Form50_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible && _isInitialized)
            {
                LoadDanhSachFileLichSu();
                if (comboBox_ChonCSDLNam.Items.Count > 0)
                {
                    _ = ThucHienTaiDuLieuLichSuAsync();
                }
            }
        }
        private string SafeDecrypt(object value)
        {
            if (value == null || value == DBNull.Value) return "";
            string s = value.ToString()?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(s)) return "";

            try
            {
                string decrypted = BaoMatAES.GiaiMa(s);
                return string.IsNullOrEmpty(decrypted) ? s : decrypted;
            }
            catch { return s; }
        }
        public void LoadDanhSachFileLichSu()
        {
            if (comboBox_ChonCSDLNam == null) return;
            comboBox_ChonCSDLNam.SelectedIndexChanged -= comboBox_ChonCSDLNam_SelectedIndexChanged;

            // ⭐ GỌI HÀM MỚI: CHỈ LẤY FILE KHEN THƯỞNG CÁ NHÂN ⭐
            var danhSach = Module_HoTroLuuDataTheoNamCu.LayDanhSachFileLichSu_KhenThuongCaNhan();

            if (danhSach != null && danhSach.Count > 0)
            {
                var dictTrungTen = new Dictionary<string, int>();
                foreach (var file in danhSach)
                {
                    if (dictTrungTen.ContainsKey(file.TenHienThi))
                    {
                        dictTrungTen[file.TenHienThi]++;
                        file.TenHienThi = $"{file.TenHienThi} ({dictTrungTen[file.TenHienThi]})";
                    }
                    else
                    {
                        dictTrungTen.Add(file.TenHienThi, 1);
                    }
                }
            }

            comboBox_ChonCSDLNam.DataSource = danhSach;
            comboBox_ChonCSDLNam.DisplayMember = "TenHienThi";
            comboBox_ChonCSDLNam.ValueMember = "DuongDan";

            comboBox_ChonCSDLNam.SelectedIndexChanged += comboBox_ChonCSDLNam_SelectedIndexChanged;

            if (danhSach == null || danhSach.Count == 0)
            {
                kryptonDataGridView1.RowCount = 0;
                _filteredIndexes.Clear();
                _dataCacheGiayKhen.Clear();
                CapNhatTrangThaiHienThi();
            }
            CapNhatTieuDeTheoNamDuocChon();
        }
        private async Task ThucHienTaiDuLieuLichSuAsync()
        {
            if (comboBox_ChonCSDLNam.SelectedItem == null)
            {
                kryptonDataGridView1.RowCount = 0;
                _filteredIndexes.Clear();
                CapNhatTrangThaiHienThi();
                return;
            }

            if (comboBox_ChonCSDLNam.SelectedItem is FileLichSuDTO selectedFile)
            {
                Form_Loading frmLoad = new Form_Loading("Đang giải mã và nạp dữ liệu năm cũ...");
                frmLoad.Icon = this.Icon;
                frmLoad.Show(this);
                this.Enabled = false;

                try
                {
                    _dataCacheGiayKhen.Clear();
                    string[] donViUuTien = Module_DonVi.LayDanhSachDonViUuTienArray();
                    var dicDonViUuTien = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < donViUuTien.Length; i++) dicDonViUuTien.TryAdd(donViUuTien[i], i);

                    await Task.Run(async () =>
                    {
                        using (var cn = new SqliteConnection($"Data Source={selectedFile.DuongDan};Mode=ReadOnly"))
                        {
                            await cn.OpenAsync();

                            string checkTableSql = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='ThongKe_GiayKhen'";
                            using (var cmdCheck = new SqliteCommand(checkTableSql, cn))
                            {
                                long count = (long)await cmdCheck.ExecuteScalarAsync();
                                if (count == 0) return;
                            }

                            string query = "SELECT * FROM ThongKe_GiayKhen";
                            using (var cmd = new SqliteCommand(query, cn))
                            using (var rd = await cmd.ExecuteReaderAsync())
                            {
                                var availableColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                for (int i = 0; i < rd.FieldCount; i++)
                                    availableColumns.Add(rd.GetName(i));

                                Func<string, string> GetStringVal = (colName) =>
                                {
                                    if (!availableColumns.Contains(colName)) return "";
                                    int idx = rd.GetOrdinal(colName);
                                    return rd.IsDBNull(idx) ? "" : SafeDecrypt(rd.GetValue(idx));
                                };

                                Func<string, string> GetRawStringVal = (colName) =>
                                {
                                    if (!availableColumns.Contains(colName)) return "";
                                    int idx = rd.GetOrdinal(colName);
                                    return rd.IsDBNull(idx) ? "" : rd.GetValue(idx).ToString();
                                };

                                Func<string, int> GetIntVal = (colName) =>
                                {
                                    if (!availableColumns.Contains(colName)) return 0;
                                    int idx = rd.GetOrdinal(colName);
                                    return rd.IsDBNull(idx) ? 0 : Convert.ToInt32(rd.GetValue(idx));
                                };

                                int currentSTT = 1;
                                while (await rd.ReadAsync())
                                {
                                    var item = new HistoryGiayKhenDTO
                                    {
                                        ID = GetIntVal("ID"),
                                        STT = currentSTT++,
                                        HoVaTen = GetStringVal("HoVaTen"),
                                        SoHieu = GetStringVal("SoHieu"),
                                        DonVi = GetStringVal("DonVi"),
                                        TinhTrang = GetRawStringVal("TinhTrang"), // Dữ liệu thuần
                                        HinhThuc_Khen = GetStringVal("HinhThuc_Khen"),
                                        QuyetDinh_Khen = GetStringVal("QuyetDinh_Khen"),
                                        NgayCapQD_Khen = GetStringVal("NgayCapQD_Khen"),
                                        DonVi_Khen = GetStringVal("DonVi_Khen"),
                                        VeViec_Khen = GetStringVal("VeViec_Khen"),
                                        GhiChu_Khen = GetStringVal("GhiChu_Khen")
                                    };
                                    item.HoVaTen_Search = item.HoVaTen.ToLowerInvariant();
                                    item.SortPriority = dicDonViUuTien.TryGetValue(item.DonVi, out int p) ? p : int.MaxValue;
                                    _dataCacheGiayKhen.Add(item);
                                }
                            }
                        }

                        _dataCacheGiayKhen = _dataCacheGiayKhen.OrderBy(x => x.SortPriority).ThenBy(x => x.ID).ToList();
                        for (int i = 0; i < _dataCacheGiayKhen.Count; i++) _dataCacheGiayKhen[i].STT = i + 1;
                    });

                    KhoiTaoBoLocComboBoxTuRAM();
                    ApplyFilter();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Lỗi đọc CSDL Khen thưởng cá nhân năm cũ: " + ex.Message);
                    MessageBox.Show("Có lỗi xảy ra khi đọc tệp dữ liệu năm cũ:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    frmLoad.Close();
                    this.Enabled = true;
                    this.Focus();
                }
            }
        }
        private void KhoiTaoBoLocComboBoxTuRAM()
        {
            if (comboBox_TimKiemDonVi != null) comboBox_TimKiemDonVi.SelectedIndexChanged -= ComboBoxFilter_SelectedIndexChanged;
            if (comboBox1_HinhThucKT != null) comboBox1_HinhThucKT.SelectedIndexChanged -= ComboBoxFilter_SelectedIndexChanged;

            var dsDonVi = _dataCacheGiayKhen
                .Where(x => !string.IsNullOrWhiteSpace(x.DonVi) && x.DonVi != "[Lỗi giải mã]")
                .Select(x => x.DonVi)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();
            dsDonVi.Insert(0, "Tất cả");

            if (comboBox_TimKiemDonVi != null)
            {
                comboBox_TimKiemDonVi.Items.Clear();
                comboBox_TimKiemDonVi.Items.AddRange(dsDonVi.ToArray());
                if (comboBox_TimKiemDonVi.Items.Count > 0) comboBox_TimKiemDonVi.SelectedIndex = 0;
            }

            var dsHinhThuc = _dataCacheGiayKhen
                .Where(x => !string.IsNullOrWhiteSpace(x.HinhThuc_Khen) && x.HinhThuc_Khen != "[Lỗi giải mã]")
                .Select(x => x.HinhThuc_Khen)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();
            dsHinhThuc.Insert(0, "Tất cả");

            if (comboBox1_HinhThucKT != null)
            {
                comboBox1_HinhThucKT.Items.Clear();
                comboBox1_HinhThucKT.Items.AddRange(dsHinhThuc.ToArray());
                if (comboBox1_HinhThucKT.Items.Count > 0) comboBox1_HinhThucKT.SelectedIndex = 0;
            }

            if (comboBox_TimKiemDonVi != null) comboBox_TimKiemDonVi.SelectedIndexChanged += ComboBoxFilter_SelectedIndexChanged;
            if (comboBox1_HinhThucKT != null) comboBox1_HinhThucKT.SelectedIndexChanged += ComboBoxFilter_SelectedIndexChanged;
        }
        private void ApplyFilter()
        {
            if (_dataCacheGiayKhen == null) { kryptonDataGridView1.RowCount = 0; return; }

            string dvFilter = comboBox_TimKiemDonVi?.Text?.Trim() ?? "";
            string htFilter = comboBox1_HinhThucKT?.Text?.Trim() ?? "";
            string tenFilter = textBox_TimKiemTheoTen?.Text?.Trim() ?? "";
            string tenFilterLower = tenFilter.ToLowerInvariant();

            if (_dangSetPlaceholder || tenFilterLower == PLACEHOLDER_TIMKIEM.ToLowerInvariant())
            {
                tenFilter = ""; tenFilterLower = "";
            }

            kryptonDataGridView1.SuspendLayout();
            try
            {
                _filteredIndexes.Clear();
                bool hasTen = !string.IsNullOrWhiteSpace(tenFilterLower);
                bool hasDV = !string.IsNullOrWhiteSpace(dvFilter) && dvFilter != "Tất cả";
                bool hasHT = !string.IsNullOrWhiteSpace(htFilter) && htFilter != "Tất cả";

                int totalCount = _dataCacheGiayKhen.Count;
                for (int i = 0; i < totalCount; i++)
                {
                    var item = _dataCacheGiayKhen[i];
                    if (hasDV && !string.Equals(item.DonVi, dvFilter, StringComparison.OrdinalIgnoreCase)) continue;
                    if (hasHT && !string.Equals(item.HinhThuc_Khen, htFilter, StringComparison.OrdinalIgnoreCase)) continue;
                    if (hasTen && !item.HoVaTen_Search.Contains(tenFilterLower)) continue;

                    _filteredIndexes.Add(i);
                }

                kryptonDataGridView1.RowCount = _filteredIndexes.Count;
                CapNhatTrangThaiHienThi();
            }
            catch (Exception ex) { Debug.WriteLine("Lỗi ApplyFilter RAM: " + ex.Message); }
            finally { kryptonDataGridView1.ResumeLayout(); kryptonDataGridView1.Invalidate(); }
        }
        private void KryptonDataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (_filteredIndexes == null || e.RowIndex < 0 || e.RowIndex >= _filteredIndexes.Count) return;

            try
            {
                int actualViewIndex = _filteredIndexes[e.RowIndex];
                var data = _dataCacheGiayKhen[actualViewIndex];
                string colName = kryptonDataGridView1.Columns[e.ColumnIndex].Name;

                switch (colName)
                {
                    case "ID": e.Value = data.ID; break;
                    case "STT": e.Value = data.STT; break;
                    case "HoVaTen": e.Value = data.HoVaTen; break;
                    case "SoHieu": e.Value = data.SoHieu; break;
                    case "DonVi": e.Value = data.DonVi; break;
                    case "TinhTrang": e.Value = data.TinhTrang; break;
                    case "HinhThuc_Khen": e.Value = data.HinhThuc_Khen; break;
                    case "QuyetDinh_Khen": e.Value = data.QuyetDinh_Khen; break;
                    case "NgayCapQD_Khen": e.Value = data.NgayCapQD_Khen; break;
                    case "DonVi_Khen": e.Value = data.DonVi_Khen; break;
                    case "VeViec_Khen": e.Value = data.VeViec_Khen; break;
                    case "GhiChu_Khen": e.Value = data.GhiChu_Khen; break;
                    default: e.Value = string.Empty; break;
                }
            }
            catch { e.Value = string.Empty; }
        }
        private void KryptonDataGridView1_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            var grid = sender as DataGridView;
            if (grid == null) return;

            if (_rowHeaderBrush == null)
            {
                _rowHeaderBrush = new SolidBrush(grid.RowHeadersDefaultCellStyle.ForeColor);
                _rowHeaderFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            }

            string stt = (e.RowIndex + 1).ToString();
            Rectangle headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);
            e.Graphics.DrawString(stt, grid.Font, _rowHeaderBrush, headerBounds, _rowHeaderFormat);
        }
        private void CapNhatTrangThaiHienThi()
        {
            if (comboBox_ChonCSDLNam.Items.Count == 0 || comboBox_ChonCSDLNam.SelectedItem == null)
            {
                if (toolStripStatusLabel1 != null) toolStripStatusLabel1.Text = "Không có tệp lịch sử khen thưởng nào.";
                return;
            }
            if (toolStripStatusLabel1 != null) toolStripStatusLabel1.Text = $"Tổng cộng: {_filteredIndexes.Count} mục khen thưởng";
        }
        private void comboBox_ChonCSDLNam_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!this.IsHandleCreated) return;
            _ = ThucHienTaiDuLieuLichSuAsync();
            CapNhatTieuDeTheoNamDuocChon();
        }
        private void ComboBoxFilter_SelectedIndexChanged(object sender, EventArgs e) => ApplyFilter();
        private void textBox_TimKiemTheoTen_TextChanged(object sender, EventArgs e)
        {
            if (_dangSetPlaceholder) return;
            if (_timKiemTimer == null) return;
            _timKiemTimer.Stop();
            _timKiemTimer.Start();
        }
        private void TextBox_TimKiemTheoTen_Enter(object sender, EventArgs e)
        {
            if (textBox_TimKiemTheoTen.Text == PLACEHOLDER_TIMKIEM)
            {
                _dangSetPlaceholder = true;
                textBox_TimKiemTheoTen.Text = "";
                textBox_TimKiemTheoTen.ForeColor = SystemColors.WindowText;
                _dangSetPlaceholder = false;
            }
        }
        private void TextBox_TimKiemTheoTen_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox_TimKiemTheoTen.Text)) InitPlaceholderTimKiem();
        }
        private void InitPlaceholderTimKiem()
        {
            if (textBox_TimKiemTheoTen == null) return;
            _dangSetPlaceholder = true;
            textBox_TimKiemTheoTen.Text = PLACEHOLDER_TIMKIEM;
            textBox_TimKiemTheoTen.ForeColor = Color.Gray;
            _dangSetPlaceholder = false;
        }
        private void kryptonButton_LamMoiCacOTimKiem_Click(object sender, EventArgs e)
        {
            _dangSetPlaceholder = true;
            InitPlaceholderTimKiem();

            if (comboBox_TimKiemDonVi != null) comboBox_TimKiemDonVi.SelectedIndexChanged -= ComboBoxFilter_SelectedIndexChanged;
            if (comboBox1_HinhThucKT != null) comboBox1_HinhThucKT.SelectedIndexChanged -= ComboBoxFilter_SelectedIndexChanged;

            if (comboBox_TimKiemDonVi != null && comboBox_TimKiemDonVi.Items.Count > 0) comboBox_TimKiemDonVi.SelectedIndex = 0;
            if (comboBox1_HinhThucKT != null && comboBox1_HinhThucKT.Items.Count > 0) comboBox1_HinhThucKT.SelectedIndex = 0;

            if (comboBox_TimKiemDonVi != null) comboBox_TimKiemDonVi.SelectedIndexChanged += ComboBoxFilter_SelectedIndexChanged;
            if (comboBox1_HinhThucKT != null) comboBox1_HinhThucKT.SelectedIndexChanged += ComboBoxFilter_SelectedIndexChanged;

            _dangSetPlaceholder = false;
            ApplyFilter();
        }
        private void kryptonButton_CapNhat_Click(object sender, EventArgs e)
        {
            _ = ThucHienTaiDuLieuLichSuAsync();
        }
        private void kryptonButton_Dong_Click(object sender, EventArgs e)
        {
            // 1. Tìm Form34 (Quản lý/Thống kê khen thưởng) trong RAM để đưa lên bề mặt
            var f34 = Application.OpenForms.OfType<Form34_ThongKeKhenThuong>().FirstOrDefault();
            if (f34 != null)
            {
                f34.Show();
                f34.BringToFront();
            }

            // 2. Tìm Form2 (Form cha) để đặt lại tiêu đề
            var fCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            if (fCha != null)
            {
                // Tạo chuỗi tiêu đề theo năm hệ thống như bạn yêu cầu
                string tieuDeForm = "Trang Quản lý khen thưởng CBCS năm " + Module_NamHeThong.LayNamHeThong();

                // Gọi hàm cập nhật tiêu đề trên Form cha
                fCha.CapNhatTieuDe(tieuDeForm);
            }

            // 3. Đóng Form50 hiện tại
            this.Close();
        }
        private async void kryptonButton_XuatData_Click(object sender, EventArgs e)
        {
            if (_filteredIndexes.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedFile = comboBox_ChonCSDLNam.SelectedItem as FileLichSuDTO;
            if (selectedFile == null) return;

            string[] parts = selectedFile.TenHienThi.Split(' ');
            string nam = (parts.Length > 1) ? parts[1] : DateTime.Now.Year.ToString();
            string fileName = $"LichSu_KhenThuongCaNhan_Nam{nam}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            using var sfd = new SaveFileDialog
            {
                Title = "Chọn nơi lưu file Excel thống kê",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = fileName,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;
            string filePath = sfd.FileName;

            string textBanDau = kryptonButton_XuatData.Values.Text;
            Image anhBanDau = kryptonButton_XuatData.Values.Image;
            Form_Loading frmLoad = new Form_Loading("Đang tạo tệp Excel từ dữ liệu lịch sử...");
            frmLoad.Icon = this.Icon;
            bool isLoadShown = false;

            try
            {
                kryptonButton_XuatData.Enabled = false;
                kryptonButton_XuatData.Values.Text = "Đang tạo...";
                kryptonButton_XuatData.Values.Image = null;
                this.Enabled = false;

                frmLoad.Show(this);
                isLoadShown = true;
                await Task.Delay(50);

                var exportCols = kryptonDataGridView1.Columns.Cast<DataGridViewColumn>()
                    .Where(c => c.Visible && c.Name != "ID")
                    .ToList();

                int rowCount = _filteredIndexes.Count;
                int colCount = exportCols.Count;

                string[] headerArray = new string[colCount];
                double[] colWidths = new double[colCount];
                var dataList = new List<object[]>(rowCount);

                for (int c = 0; c < colCount; c++)
                {
                    headerArray[c] = exportCols[c].HeaderText;
                    colWidths[c] = exportCols[c].Width / 7.5;
                }

                for (int r = 0; r < rowCount; r++)
                {
                    var dto = _dataCacheGiayKhen[_filteredIndexes[r]];
                    var rowValues = new object[colCount];
                    for (int c = 0; c < colCount; c++)
                    {
                        string cName = exportCols[c].Name;
                        switch (cName)
                        {
                            case "STT": rowValues[c] = dto.STT; break;
                            case "HoVaTen": rowValues[c] = dto.HoVaTen; break;
                            case "SoHieu": rowValues[c] = dto.SoHieu; break;
                            case "DonVi": rowValues[c] = dto.DonVi; break;
                            case "TinhTrang": rowValues[c] = dto.TinhTrang; break;
                            case "HinhThuc_Khen": rowValues[c] = dto.HinhThuc_Khen; break;
                            case "QuyetDinh_Khen": rowValues[c] = dto.QuyetDinh_Khen; break;
                            case "NgayCapQD_Khen": rowValues[c] = dto.NgayCapQD_Khen; break;
                            case "DonVi_Khen": rowValues[c] = dto.DonVi_Khen; break;
                            case "VeViec_Khen": rowValues[c] = dto.VeViec_Khen; break;
                            case "GhiChu_Khen": rowValues[c] = dto.GhiChu_Khen; break;
                            default: rowValues[c] = ""; break;
                        }
                    }
                    dataList.Add(rowValues);
                }

                await Task.Run(() =>
                {
                    using var wb = new ClosedXML.Excel.XLWorkbook();
                    var ws = wb.Worksheets.Add("LichSuKhenThuongCaNhan");

                    ws.Style.Font.FontName = "Times New Roman";
                    ws.Style.Font.FontSize = 11;

                    ws.Cell("A1").Value = "DANH SÁCH LỊCH SỬ";
                    ws.Range(1, 1, 1, colCount).Merge().Style.Font.SetBold().Font.SetFontSize(14)
                        .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

                    ws.Cell("A2").Value = $"THỐNG KÊ KẾT QUẢ KHEN THƯỞNG CÁ NHÂN NĂM {nam}";
                    ws.Range(2, 1, 2, colCount).Merge().Style.Font.SetBold().Font.SetFontSize(13)
                        .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

                    int excelStartRow = 4;
                    for (int c = 0; c < colCount; c++)
                    {
                        var cell = ws.Cell(excelStartRow, c + 1);
                        cell.Value = string.IsNullOrWhiteSpace(headerArray[c]) ? exportCols[c].Name : headerArray[c];
                        cell.Style.Font.Bold = true;
                        cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                        cell.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                        cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(217, 225, 242);
                        ws.Column(c + 1).Width = colWidths[c];
                    }
                    ws.Row(excelStartRow).Height = 35;

                    if (rowCount > 0)
                    {
                        ws.Cell(excelStartRow + 1, 1).InsertData(dataList);
                        var dataRange = ws.Range(excelStartRow, 1, excelStartRow + rowCount, colCount);
                        dataRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                        dataRange.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

                        var bodyRange = ws.Range(excelStartRow + 1, 1, excelStartRow + rowCount, colCount);
                        bodyRange.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                        bodyRange.Style.Alignment.WrapText = true;

                        ws.Range(excelStartRow + 1, 1, excelStartRow + rowCount, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                    }

                    int tongDong = rowCount + excelStartRow + 1;
                    var totalCell = ws.Cell(tongDong, 1);
                    totalCell.Value = $"Tổng cộng: {rowCount} cá nhân được thống kê./.";
                    totalCell.Style.Font.SetBold().Font.SetItalic();
                    ws.Range(tongDong, 1, tongDong, colCount).Merge();

                    ws.PageSetup.PaperSize = ClosedXML.Excel.XLPaperSize.A4Paper;
                    ws.PageSetup.PageOrientation = ClosedXML.Excel.XLPageOrientation.Landscape;
                    ws.PageSetup.FitToPages(1, 0);
                    ws.PageSetup.Margins.Top = 0.5;
                    ws.PageSetup.Margins.Bottom = 0.5;
                    ws.PageSetup.Margins.Left = 0.4;
                    ws.PageSetup.Margins.Right = 0.4;

                    wb.SaveAs(filePath);
                });

                try { Module_XuatNhapDuLieuThiDua.MoVaChonTepTrongExplorer(filePath); } catch { }
                Module_ThongBao.ThanhCong("Xuất Excel thành công!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xuất dữ liệu Excel:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (isLoadShown) { frmLoad.Close(); this.Enabled = true; }
                kryptonButton_XuatData.Values.Text = textBanDau;
                kryptonButton_XuatData.Values.Image = anhBanDau;
                kryptonButton_XuatData.Enabled = true;
                this.Focus();
            }
        }
        private void toolStripMenuItem_XuatDuLieu_Click(object sender, EventArgs e)
        {
            kryptonButton_XuatData.PerformClick();
        }
        private void CauHinhGridCoBan(DataGridView dgv)
        {
            dgv.RowHeadersVisible = true;
            dgv.RowHeadersWidth = 60;
            dgv.BackgroundColor = Color.White;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = Color.FromArgb(235, 235, 235);
            dgv.EnableHeadersVisualStyles = false;

            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersHeight = 60;
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(240, 244, 248),
                ForeColor = Color.FromArgb(40, 40, 40),
                WrapMode = DataGridViewTriState.True,
                Padding = new Padding(3)
            };

            dgv.RowTemplate.Height = 36;
            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(45, 45, 45),
                SelectionBackColor = Color.FromArgb(232, 244, 253),
                SelectionForeColor = Color.FromArgb(0, 102, 204),
                Padding = new Padding(2, 0, 2, 0)
            };
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 252, 252);
        }
        private void CauHinhCotGrid(DataGridView dgv)
        {
            dgv.Columns.Clear();
            // 0. Cột ID (Ẩn)
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ID", Visible = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "STT", Visible = false });
            // 1. STT (Ngắn)
            // dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "STT", HeaderText = "STT", Width = 45, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            // 2. Họ và tên
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "HoVaTen", HeaderText = "Họ và tên", Width = 160, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            // 3. Số hiệu
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoHieu", HeaderText = "Số hiệu", Width = 90, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            // 4. Đơn vị công tác (Thu hẹp lại)
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "DonVi", HeaderText = "Đơn vị công tác", Width = 130, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            // 5. Tình trạng
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TinhTrang", HeaderText = "Tình trạng", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            // 6. Hình thức khen
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "HinhThuc_Khen", HeaderText = "Hình thức khen", Width = 140, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            // 7. Số Quyết định
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "QuyetDinh_Khen", HeaderText = "Số Quyết định", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            // 8. Ngày cấp QĐ
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "NgayCapQD_Khen", HeaderText = "Ngày cấp QĐ", Width = 95, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            // 9. Đơn vị khen (Thu hẹp lại)
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "DonVi_Khen", HeaderText = "Đơn vị khen", Width = 150, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });
            // 10. Về việc (Nội dung) - Rộng ra và tự động chiếm không gian còn lại
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "VeViec_Khen",
                HeaderText = "Về việc (Nội dung)",
                Width = 250,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, // Tự động lấp đầy khoảng trống
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
            });

            // 11. Ghi chú (Mở rộng ra)
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "GhiChu_Khen", HeaderText = "Ghi chú", Width = 200, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft } });

            // Tắt tính năng tự động Sort khi click vào tiêu đề cột (do đang xài Virtual Mode)
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }
        private void CapNhatTieuDeTheoNamDuocChon()
        {
            if (comboBox_ChonCSDLNam.SelectedItem is FileLichSuDTO selectedFile)
            {
                string tieuDeMoi = $"Thống kê khen thưởng CBCS năm cũ - {selectedFile.TenHienThi}";
                this.Text = tieuDeMoi;

                var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
                if (formCha != null) formCha.CapNhatTieuDe(tieuDeMoi);
            }
        }
        private void toolStripMenuItem_LamMoi_Click(object sender, EventArgs e)
        {
            kryptonButton_CapNhat.PerformClick();
        }
        private void toolStripMenuItem_QuanLyKhenThuong_Click(object sender, EventArgs e)
        {
            kryptonButton1_TraCuuKetQuaKhenThuongNamCu.PerformClick();
        }
        private void toolStripMenuItem_Dong_Click(object sender, EventArgs e)
        {
            kryptonButton_Dong.PerformClick();
        }
        private void kryptonButton1_TraCuuKetQuaKhenThuongNamCu_Click(object sender, EventArgs e)
        {
            // 1. Tìm Form cha đang hoạt động
            var formCha = Application.OpenForms
                .OfType<Form2_FormCha>()
                .FirstOrDefault();

            if (formCha == null || formCha.IsDisposed)
            {
                MessageBox.Show(
                    "Không tìm thấy giao diện chính (Form2_FormCha).\n\nVui lòng kiểm tra lại trạng thái của phần mềm.",
                    "Hệ thống",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            // 2. Tìm PanelContainer
            var panel = formCha.Controls
                .Find("PanelContainer", true)
                .FirstOrDefault() as Panel;

            if (panel == null || panel.IsDisposed)
            {
                MessageBox.Show(
                    "Không tìm thấy vùng hiển thị (PanelContainer).\n\nVui lòng kiểm tra lại giao diện chính.",
                    "Hệ thống",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            try
            {
                // 3. Tìm Form51 đã tồn tại trong PanelContainer
                var form51 = panel.Controls
                    .OfType<Form51_QuanLyKhenThuongTapTheNamCu>()
                    .FirstOrDefault();

                // 4. Nếu Form51 chưa tồn tại -> tạo mới đúng một lần
                if (form51 == null || form51.IsDisposed)
                {
                    form51 = new Form51_QuanLyKhenThuongTapTheNamCu
                    {
                        TopLevel = false,
                        FormBorderStyle = FormBorderStyle.None,
                        Dock = DockStyle.Fill
                    };

                    // 5. Đưa Form51 vào PanelContainer
                    panel.Controls.Add(form51);

                    // Đưa Form51 lên trước khi hiển thị
                    form51.BringToFront();

                    // Hiển thị Form lần đầu
                    form51.Show();
                }
                else
                {
                    // 6. Nếu Form51 đã tồn tại -> tái sử dụng
                    form51.BringToFront();

                    if (!form51.Visible)
                    {
                        form51.Show();
                    }
                }

                // 7. Ẩn các Form con khác trong PanelContainer
                foreach (Control control in panel.Controls)
                {
                    if (control is Form childForm &&
                        childForm != form51 &&
                        !childForm.IsDisposed)
                    {
                        childForm.Hide();
                    }
                }

                // 8. Đảm bảo Form51 luôn ở lớp trên cùng
                form51.Show();
                form51.BringToFront();

                // 9. Cập nhật tiêu đề Form cha
                string tieuDeForm =
                    "Trang tra cứu kết quả khen thưởng tập thể năm cũ " +
                    Module_NamHeThong.LayNamHeThong();

                formCha.CapNhatTieuDe(tieuDeForm);
            }
            catch (ObjectDisposedException)
            {
                // Form hoặc Control đã bị giải phóng trong lúc chuyển giao diện.
                // Không để lỗi này làm sập toàn bộ phần mềm.
                return;
            }
            catch (InvalidOperationException ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Lỗi chuyển Form51 - InvalidOperationException]: {ex}");

                MessageBox.Show(
                    "Không thể mở trang tra cứu kết quả khen thưởng tập thể năm cũ.\n\n" +
                    "Vui lòng thử lại.",
                    "Lỗi giao diện",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Lỗi chuyển Form51]: {ex}");

                MessageBox.Show(
                    "Đã xảy ra lỗi khi mở trang tra cứu kết quả khen thưởng tập thể năm cũ.\n\n" +
                    $"Chi tiết: {ex.Message}",
                    "Lỗi hệ thống",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private async void toolStripMenuItem_XoaDuLieuNam_Click(object sender, EventArgs e)
        {
            // 1. KIỂM TRA XEM ĐÃ CHỌN TỆP NĂM CŨ CHƯA
            if (comboBox_ChonCSDLNam.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn tệp CSDL năm cũ trên danh sách trước khi thao tác!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Lấy đường dẫn chính xác của tệp lịch sử đang chọn
            var selectedFile = (FileLichSuDTO)comboBox_ChonCSDLNam.SelectedItem;
            string dbPathLichSu = selectedFile.DuongDan;

            if (!File.Exists(dbPathLichSu))
            {
                MessageBox.Show("Tệp CSDL không tồn tại trên ổ đĩa!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // 2. KIỂM TRA NHANH XEM CÓ DỮ LIỆU ĐỂ XÓA KHÔNG
                int tongSoDong = 0;
                using (var conn = new SqliteConnection($"Data Source={dbPathLichSu}")) // Trỏ đúng vào DB lịch sử
                {
                    await conn.OpenAsync();

                    // Kiểm tra xem bảng có tồn tại không trước khi đếm (chống lỗi văng khi DB bị hỏng)
                    using (var cmdCheck = new SqliteCommand("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='ThongKe_GiayKhen'", conn))
                    {
                        if (Convert.ToInt32(await cmdCheck.ExecuteScalarAsync()) == 0)
                        {
                            MessageBox.Show("Tệp CSDL này không chứa cấu trúc bảng Giấy khen!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }

                    using (var cmd = new SqliteCommand("SELECT COUNT(*) FROM ThongKe_GiayKhen", conn))
                    {
                        tongSoDong = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }
                }

                if (tongSoDong == 0)
                {
                    MessageBox.Show($"Hiện tại không có dữ liệu giấy khen nào trong tệp {selectedFile.TenHienThi} để xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi truy xuất kiểm tra dữ liệu: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 3. XÁC MINH QUYỀN ADMIN (Bảo vệ tính mạng CSDL)
            using (Form24_XacMinhAdmin frmXacMinh = new Form24_XacMinhAdmin())
            {
                frmXacMinh.TopMost = true;
                frmXacMinh.StartPosition = FormStartPosition.CenterScreen;
                if (frmXacMinh.ShowDialog(this) != DialogResult.OK) return;
            }

            // 4. CẢNH BÁO NGUY HIỂM LẦN CUỐI (UX Chống click nhầm)
            DialogResult result = MessageBox.Show(
                $"CẢNH BÁO NGUY HIỂM:\n\nBạn đang yêu cầu XÓA TOÀN BỘ dữ liệu chi tiết giấy khen của: {selectedFile.TenHienThi}.\nHành động này KHÔNG THỂ KHÔI PHỤC!\n\nBạn có chắc chắn muốn XÓA SẠCH?",
                "Xác nhận xóa toàn bộ",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2); // Đặt mặc định focus ở nút NO cho an toàn

            if (result != DialogResult.Yes) return;

            // 5. THỰC THI XÓA VÀ ĐỒNG BỘ TRÊN LUỒNG BẢO VỆ TRANSACTION
            try
            {
                using (var conn = new SqliteConnection($"Data Source={dbPathLichSu}")) // Trỏ đúng vào DB lịch sử
                {
                    await conn.OpenAsync();
                    using (var tran = conn.BeginTransaction())
                    {
                        try
                        {
                            // A. Xóa sạch mọi dòng trong bảng ThongKe_GiayKhen
                            using (var cmdDelete = new SqliteCommand("DELETE FROM ThongKe_GiayKhen", conn, tran))
                            {
                                await cmdDelete.ExecuteNonQueryAsync();
                            }

                            // B. ⭐ GIẢI QUYẾT LỖI "no such table": Kiểm tra bảng sqlite_sequence trước khi Reset
                            using (var cmdCheckSeq = new SqliteCommand("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='sqlite_sequence'", conn, tran))
                            {
                                int hasSequenceTable = Convert.ToInt32(await cmdCheckSeq.ExecuteScalarAsync());
                                if (hasSequenceTable > 0)
                                {
                                    using (var cmdResetSeq = new SqliteCommand("UPDATE sqlite_sequence SET seq = 0 WHERE name = 'ThongKe_GiayKhen'", conn, tran))
                                    {
                                        await cmdResetSeq.ExecuteNonQueryAsync();
                                    }
                                }
                            }

                            // C. ⭐ CHUẨN KỸ SƯ: Đồng bộ hóa trả toàn bộ số lượng khen thưởng ở bảng TỔNG về số 0
                            using (var cmdSync = new SqliteCommand("UPDATE ThongKeCBCS_DuocKhenThuong SET SoLuong_Khen = '0'", conn, tran))
                            {
                                await cmdSync.ExecuteNonQueryAsync();
                            }

                            // Chốt lệnh lưu xuống ổ cứng
                            tran.Commit();
                        }
                        catch
                        {
                            // Có bất kỳ lỗi gì thì hoàn tác toàn bộ thao tác, không cho hư hỏng CSDL
                            tran.Rollback();
                            throw;
                        }
                    }
                }

                // 6. GHI LOG NHẬT KÝ HỆ THỐNG
                try
                {
                    Module_NhatKy.GhiNhatKy(
                        string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM) ? "Không xác định" : Module_TaiKhoan.TenTaiKhoan_RAM,
                        $"Xóa TOÀN BỘ dữ liệu chi tiết giấy khen ({selectedFile.TenHienThi})",
                        $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                    );
                }
                catch (Exception logEx) { System.Diagnostics.Debug.WriteLine("Lỗi ghi nhật ký: " + logEx.Message); }

                // 7. CẬP NHẬT LẠI GIAO DIỆN
                MessageBox.Show($"✔ Đã xóa sạch toàn bộ dữ liệu giấy khen của {selectedFile.TenHienThi} thành công!", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Gọi hàm làm mới dữ liệu lưới nội bộ (Thay vì tải lại toàn bộ ComboBox)
                _ = ThucHienTaiDuLieuLichSuAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi trong quá trình xóa dữ liệu:\n\n" + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}