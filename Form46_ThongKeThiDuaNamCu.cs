using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PhanMemThiDua2026
{
    public partial class Form46_ThongKeThiDuaNamCu : Form
    {
        private DataTable _dtHienTai;
        private List<int> _filteredIndexes = new List<int>(); // Lưu trữ chỉ số dòng đã qua bộ lọc (Virtual Mode Core)
        private Dictionary<int, int> _colIndexMap = new Dictionary<int, int>(); // Bản đồ tọa độ cột siêu tốc

        private List<HistoryCBCSDTO> _dataCacheCBCS = new List<HistoryCBCSDTO>();
        private List<HistoryTanBinhDTO> _dataCacheTanBinh = new List<HistoryTanBinhDTO>();
        private List<string> _cotPhatSinhCacheThongKe;
        private bool _isDataMaxMode = false;
        private const int NGUONG_DU_LIEU = 3000;

        private const string PLACEHOLDER_TIMKIEM = "Nhập tìm kiếm";
        private bool _dangSetPlaceholder = false;
        private bool _isInitialized = false;
        private SolidBrush _rowHeaderBrush = null;
        private StringFormat _rowHeaderFormat = null;
        private System.Windows.Forms.Timer _timKiemTimer;
        public Form46_ThongKeThiDuaNamCu()
        {
            InitializeComponent();
            this.KeyPreview = true;
            this.VisibleChanged += Form46_VisibleChanged;
            // ⭐ KÍCH HOẠT VIRTUAL MODE CHUẨN MỰC GIỐNG FORM 15
            kryptonDataGridView1.VirtualMode = true;
            kryptonDataGridView1.DataSource = null;

            // Chặn các thuộc tính tự co giãn gây lag đồ họa lưới khi cuộn chuột
            kryptonDataGridView1.AllowUserToAddRows = false;
            kryptonDataGridView1.AllowUserToDeleteRows = false;
            kryptonDataGridView1.AllowUserToResizeRows = false;
            kryptonDataGridView1.ReadOnly = true;
            kryptonDataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            kryptonDataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            kryptonDataGridView1.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;

            // Kích hoạt bộ đệm kép chống chớp giật lưới
            typeof(DataGridView).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(kryptonDataGridView1, true, null);

            // Đăng ký sự kiện nạp ô và vẽ số thứ tự cột Header
            kryptonDataGridView1.CellValueNeeded += KryptonDataGridView1_CellValueNeeded;
            kryptonDataGridView1.RowPostPaint += KryptonDataGridView1_RowPostPaint;
        }
        private void Form46_ThongKeThiDuaNamCu_Load(object sender, EventArgs e)
        {
            if (_isInitialized) return;

            Module_DonVi.KhoiTao();
            LoadDanhSachFileLichSu();
            DinhDangGridBanDau();
            kryptonDataGridView1.ContextMenuStrip = contextMenuStrip1;
            Module_MenuChuotPhai.TichHopGiaoDienXanhLa(contextMenuStrip1);
            // 1. Đồng bộ sự kiện phản hồi lập tức cho các ComboBox bộ lọc
            // ⭐ CHÈN BỔ SUNG 2 DÒNG NÀY VÀO ĐÂY: Ép gán cứng sự kiện chọn tệp CSDL năm cũ
            comboBox_ChonCSDLNam.SelectedIndexChanged -= comboBox_ChonCSDLNam_SelectedIndexChanged;
            comboBox_ChonCSDLNam.SelectedIndexChanged += comboBox_ChonCSDLNam_SelectedIndexChanged;
            comboBox_TimKiemDonVi.SelectedIndexChanged += ComboBoxFilter_SelectedIndexChanged;
            comboBox1_TinhTrang.SelectedIndexChanged += ComboBoxFilter_SelectedIndexChanged;
            comboBox1_PhanLoaiThiDuaNamCu.SelectedIndexChanged += ComboBoxFilter_SelectedIndexChanged;

            // 2. Tích hợp hiệu ứng Placeholder chuẩn cho ô tìm kiếm tên
            InitPlaceholderTimKiem();
            textBox_TimKiemTheoTen.TextChanged += textBox_TimKiemTheoTen_TextChanged;
            textBox_TimKiemTheoTen.Enter += TextBox_TimKiemTheoTen_Enter;
            textBox_TimKiemTheoTen.Leave += TextBox_TimKiemTheoTen_Leave;

            // 3. ⏱️ THIẾT LẬP TIMER TRỄ 300MS CHUẨN UX (Gõ xong mới lọc - Tránh đơ giao diện)
            _timKiemTimer = new System.Windows.Forms.Timer();
            _timKiemTimer.Interval = 300;
            _timKiemTimer.Tick += (s, ev) =>
            {
                _timKiemTimer.Stop();
                ApplyFilter();
            };

            // Ép nạp dữ liệu năm cũ lên lưới ngay khi mở màn hình
            ThucHienTaiDuLieuLichSu();
            _isInitialized = true;
        }
        private void Form46_VisibleChanged(object sender, EventArgs e)
        {
            // Mỗi khi Form 46 được hiển thị lên màn hình (chuyển tab/chuyển trang)
            if (this.Visible && _isInitialized)
            {
                // ⭐ BƯỚC 1: Bắt buộc nạp lại danh sách File lịch sử.
                // Hàm này sẽ tự kiểm tra xem phần mềm đang ở chế độ Tân binh hay CBCS
                // để đưa đúng danh sách tệp .db vào ComboBox.
                LoadDanhSachFileLichSu();

                // ⭐ BƯỚC 2: Nếu có tệp dữ liệu, tự động chạy cập nhật để load lưới
                if (comboBox_ChonCSDLNam.Items.Count > 0)
                {
                    // Tự động mô phỏng thao tác bấm nút "Cập nhật" của người dùng
                    // (Quá trình đồng bộ Tình trạng và load lưới sẽ diễn ra mượt mà qua Form Loading)
                    kryptonButton_CapNhat.PerformClick();
                }
            }
        }
        private void ComboBoxFilter_SelectedIndexChanged(object sender, EventArgs e) => ApplyFilter();
        private void textBox_TimKiemTheoTen_TextChanged(object sender, EventArgs e)
        {
            if (_dangSetPlaceholder) return;
            if (_timKiemTimer == null) return;
            _timKiemTimer.Stop();
            _timKiemTimer.Start(); // Người dùng đang gõ thì reset bộ đếm ngược
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
            if (string.IsNullOrWhiteSpace(textBox_TimKiemTheoTen.Text))
            {
                InitPlaceholderTimKiem();
            }
        }
        private void InitPlaceholderTimKiem()
        {
            _dangSetPlaceholder = true;
            textBox_TimKiemTheoTen.Text = PLACEHOLDER_TIMKIEM;
            textBox_TimKiemTheoTen.ForeColor = Color.Gray;
            _dangSetPlaceholder = false;
        }
        private void kryptonButton_LamMoiCacOTimKiem_Click(object sender, EventArgs e)
        {
            _dangSetPlaceholder = true;
            InitPlaceholderTimKiem();

            // Ngắt sự kiện tạm thời để tránh ép lưới tính toán lặp đi lặp lại nhiều lần
            comboBox_TimKiemDonVi.SelectedIndexChanged -= ComboBoxFilter_SelectedIndexChanged;
            comboBox1_TinhTrang.SelectedIndexChanged -= ComboBoxFilter_SelectedIndexChanged;
            comboBox1_PhanLoaiThiDuaNamCu.SelectedIndexChanged -= ComboBoxFilter_SelectedIndexChanged;

            if (comboBox_TimKiemDonVi.Items.Count > 0) comboBox_TimKiemDonVi.SelectedIndex = 0;
            if (comboBox1_TinhTrang.Items.Count > 0) comboBox1_TinhTrang.SelectedIndex = 0;
            if (comboBox1_PhanLoaiThiDuaNamCu.Items.Count > 0) comboBox1_PhanLoaiThiDuaNamCu.SelectedIndex = 0;

            comboBox_TimKiemDonVi.SelectedIndexChanged += ComboBoxFilter_SelectedIndexChanged;
            comboBox1_TinhTrang.SelectedIndexChanged += ComboBoxFilter_SelectedIndexChanged;
            comboBox1_PhanLoaiThiDuaNamCu.SelectedIndexChanged += ComboBoxFilter_SelectedIndexChanged;

            _dangSetPlaceholder = false;
            ApplyFilter();
        }
        public void LoadDanhSachFileLichSu()
        {
            // Ngắt sự kiện tạm thời để tránh lặp luồng dồn dập
            comboBox_ChonCSDLNam.SelectedIndexChanged -= comboBox_ChonCSDLNam_SelectedIndexChanged;

            var danhSach = Module_HoTroLuuDataTheoNamCu.LayDanhSachFileLichSu();
            comboBox_ChonCSDLNam.DataSource = danhSach;
            comboBox_ChonCSDLNam.DisplayMember = "TenHienThi";
            comboBox_ChonCSDLNam.ValueMember = "DuongDan";

            // Khôi phục lại sự kiện
            comboBox_ChonCSDLNam.SelectedIndexChanged += comboBox_ChonCSDLNam_SelectedIndexChanged;

            // ⭐ CHỐT HẠ: Cập nhật ngay trạng thái nếu không tìm thấy tệp CSDL nào
            if (danhSach == null || danhSach.Count == 0)
            {
                kryptonDataGridView1.RowCount = 0;
                kryptonDataGridView1.DataSource = null;
                kryptonDataGridView1.Columns.Clear();
                _filteredIndexes.Clear();
                _dtHienTai = null;
                CapNhatTrangThaiHienThi();
            }
        }
        private void KhoiTaoBoLocComboBox(bool laTanBinh)
        {
            // Ngắt sự kiện tạm thời để chặn tuyệt đối hiện tượng lặp luồng (Loop Event) gây lag lưới
            comboBox_TimKiemDonVi.SelectedIndexChanged -= ComboBoxFilter_SelectedIndexChanged;
            comboBox1_PhanLoaiThiDuaNamCu.SelectedIndexChanged -= ComboBoxFilter_SelectedIndexChanged;

            try
            {
                // 1. Nạp và sắp xếp danh sách Đơn vị từ Module_DonVi theo trạng thái bình thường
                var dsDonVi = Module_DonVi.GetDanhSachDonVi();
                if (dsDonVi == null) dsDonVi = new List<string>();

                if (!dsDonVi.Contains("Tất cả"))
                    dsDonVi.Insert(0, "Tất cả");

                comboBox_TimKiemDonVi.DataSource = null;
                comboBox_TimKiemDonVi.DataSource = dsDonVi;
                comboBox_TimKiemDonVi.SelectedIndex = -1;
                comboBox_TimKiemDonVi.SelectedIndex = 0;

                // 2. Phân nhánh xử lý Giao diện bộ lọc theo phiên bản dữ liệu năm cũ
                if (laTanBinh)
                {
                    // Ẩn hoàn toàn ComboBox phân loại và Label tiêu đề tương ứng trên Form
                    if (comboBox1_PhanLoaiThiDuaNamCu != null) comboBox1_PhanLoaiThiDuaNamCu.Visible = false;
                    if (label2_PhanLoai != null) label2_PhanLoai.Visible = false;
                }
                else
                {
                    // Hiển thị lại bộ lọc khi chọn tệp CBCS
                    if (comboBox1_PhanLoaiThiDuaNamCu != null) comboBox1_PhanLoaiThiDuaNamCu.Visible = true;
                    if (label2_PhanLoai != null) label2_PhanLoai.Visible = true;

                    // Nạp cứng tập giá trị danh mục phân loại thi đua chuẩn của cán bộ chiến sĩ
                    comboBox1_PhanLoaiThiDuaNamCu.Items.Clear();
                    comboBox1_PhanLoaiThiDuaNamCu.Items.AddRange(new object[] { "Tất cả", "CSTĐ", "CSTT", "HTNV", "KHTNV", "Không PL" });
                    if (comboBox1_PhanLoaiThiDuaNamCu.Items.Count > 0) comboBox1_PhanLoaiThiDuaNamCu.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi khởi tạo danh mục bộ lọc ComboBox: " + ex.Message);
            }
            finally
            {
                // Khôi phục lại ủy quyền sự kiện sau khi nạp xong trạng thái an toàn
                comboBox_TimKiemDonVi.SelectedIndexChanged += ComboBoxFilter_SelectedIndexChanged;
                comboBox1_PhanLoaiThiDuaNamCu.SelectedIndexChanged += ComboBoxFilter_SelectedIndexChanged;
            }
        }
        private void comboBox_ChonCSDLNam_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!this.IsHandleCreated) return;
            ThucHienTaiDuLieuLichSu();
        }
        // ⭐ TRÁI TIM BỘ LỌC ĐA TẦNG ĐƯỢC SAO CHÉP NGUYÊN BẢN VÀ PHÁT TRIỂN TỪ FORM 15
        private void ApplyFilter()
        {
            if (_dtHienTai == null) { kryptonDataGridView1.RowCount = 0; return; }

            string dvFilter = comboBox_TimKiemDonVi.Text?.Trim() ?? "";
            string tenFilter = textBox_TimKiemTheoTen.Text?.Trim() ?? "";
            string tinhTrangFilter = comboBox1_TinhTrang.Text?.Trim() ?? "";
            string plFilter = comboBox1_PhanLoaiThiDuaNamCu.Text?.Trim() ?? "";
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
                bool hasTT = !string.IsNullOrWhiteSpace(tinhTrangFilter) && tinhTrangFilter != "Tất cả";
                bool hasPL = !string.IsNullOrWhiteSpace(plFilter) && plFilter != "Tất cả";

                var selectedFile = (FileLichSuDTO)comboBox_ChonCSDLNam.SelectedItem;

                if (_isDataMaxMode)
                {
                    if (selectedFile.LaTanBinh)
                    {
                        int totalCount = _dataCacheTanBinh.Count;
                        for (int i = 0; i < totalCount; i++)
                        {
                            var item = _dataCacheTanBinh[i];
                            if (hasDV && item.DonVi != dvFilter) continue;
                            if (hasTT && !string.Equals(item.TinhTrang, tinhTrangFilter, StringComparison.OrdinalIgnoreCase)) continue;
                            if (hasTen && !item.HoVaTen_Search.Contains(tenFilterLower)) continue;
                            _filteredIndexes.Add(i);
                        }
                    }
                    else
                    {
                        int totalCount = _dataCacheCBCS.Count;
                        for (int i = 0; i < totalCount; i++)
                        {
                            var item = _dataCacheCBCS[i];
                            if (hasDV && item.DonVi != dvFilter) continue;
                            if (hasTT && !string.Equals(item.TinhTrang, tinhTrangFilter, StringComparison.OrdinalIgnoreCase)) continue;
                            if (hasPL && !string.Equals(item.TongKet_Nam, plFilter, StringComparison.OrdinalIgnoreCase)) continue;
                            if (hasTen && !item.HoVaTen_Search.Contains(tenFilterLower)) continue;
                            _filteredIndexes.Add(i);
                        }
                    }
                }
                else
                {
                    // Chế độ Standard: Lọc trên DefaultView của DataTable phát sinh cũ
                    bool hasSearchColumn = _dtHienTai.Columns.Contains("HoVaTen_Search");
                    bool hasTongKetColumn = _dtHienTai.Columns.Contains("TongKet_Nam");
                    DataView view = _dtHienTai.DefaultView;
                    int count = view.Count;

                    for (int i = 0; i < count; i++)
                    {
                        DataRowView rowView = view[i];
                        if (hasDV && rowView["DonVi"].ToString() != dvFilter) continue;
                        if (hasTT && !string.Equals(rowView["TinhTrang"].ToString().Trim(), tinhTrangFilter, StringComparison.OrdinalIgnoreCase)) continue;
                        if (hasPL && hasTongKetColumn && !string.Equals(rowView["TongKet_Nam"].ToString().Trim(), plFilter, StringComparison.OrdinalIgnoreCase)) continue;
                        if (hasTen)
                        {
                            if (hasSearchColumn) { if (!rowView["HoVaTen_Search"].ToString().Contains(tenFilterLower)) continue; }
                            else { if (rowView["HoVaTen"].ToString().IndexOf(tenFilter, StringComparison.OrdinalIgnoreCase) < 0) continue; }
                        }
                        _filteredIndexes.Add(i);
                    }
                }

                kryptonDataGridView1.RowCount = _filteredIndexes.Count;
                CapNhatTrangThaiHienThi(); // ⭐ Tự động cập nhật linh hoạt theo trạng thái CSDL
            }
            catch (Exception ex) { Debug.WriteLine("Lỗi ApplyFilter RAM: " + ex.Message); }
            finally { kryptonDataGridView1.ResumeLayout(); kryptonDataGridView1.Invalidate(); }
        }
        private void CapNhatTrangThaiHienThi()
        {
            // 1. CHỐT CHẶN: Kiểm tra nếu không có tệp CSDL nào trong danh sách
            if (comboBox_ChonCSDLNam.Items.Count == 0 || comboBox_ChonCSDLNam.SelectedItem == null)
            {
                toolStripStatusLabel1.Text = "Hệ thống không phát hiện hồ sơ lưu trữ thi đua các năm trước";
                return;
            }

            // 2. Nếu có CSDL thì hiển thị số lượng bản ghi sau khi lọc
            toolStripStatusLabel1.Text = _isDataMaxMode
                ? $"Tổng cộng: {_filteredIndexes.Count} đồng chí [Chế độ hiệu năng cao]"
                : $"Tổng cộng: {_filteredIndexes.Count} đồng chí";
        }
        private void KryptonDataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (_filteredIndexes == null || e.RowIndex < 0 || e.RowIndex >= _filteredIndexes.Count) return;

            try
            {
                int actualViewIndex = _filteredIndexes[e.RowIndex];
                if (_isDataMaxMode)
                {
                    string colName = kryptonDataGridView1.Columns[e.ColumnIndex].Name;
                    bool laTanBinh = ((FileLichSuDTO)comboBox_ChonCSDLNam.SelectedItem).LaTanBinh;

                    if (laTanBinh)
                    {
                        var data = _dataCacheTanBinh[actualViewIndex];
                        switch (colName)
                        {
                            case "HoVaTen": e.Value = data.HoVaTen; break;
                            case "SoHieu": e.Value = data.SoHieu; break;
                            case "DonVi": e.Value = data.DonVi; break;
                            case "TinhTrang": e.Value = data.TinhTrang; break;
                            case "GhiChu": e.Value = data.GhiChu; break;
                            case "TS_Loai1": e.Value = data.TS_Loai1; break;
                            case "TS_Loai2": e.Value = data.TS_Loai2; break;
                            case "TS_Loai3": e.Value = data.TS_Loai3; break;
                            case "TS_Loai4": e.Value = data.TS_Loai4; break;
                            default:
                                // SỬA LỖI TÊN BIẾN TẠI ĐÂY
                                if (data.DuLieuThang.TryGetValue(colName, out string valT)) e.Value = valT;
                                else if (data.CotPhatSinh != null && data.CotPhatSinh.TryGetValue(colName, out string valP)) e.Value = valP;
                                else e.Value = string.Empty;
                                break;
                        }
                    }
                    else
                    {
                        var data = _dataCacheCBCS[actualViewIndex];
                        switch (colName)
                        {
                            case "HoVaTen": e.Value = data.HoVaTen; break;
                            case "SoHieu": e.Value = data.SoHieu; break;
                            case "DonVi": e.Value = data.DonVi; break;
                            case "TinhTrang": e.Value = data.TinhTrang; break;
                            case "GhiChu": e.Value = data.GhiChu; break;
                            case "KQ_ThiDua_Nam_Cu": e.Value = data.KQ_TD; break;
                            case "KQ_XepLoaiCB_Nam_Cu": e.Value = data.KQ_XL_CB; break;
                            case "KQ_XepLoaiDangVien_Nam_Cu": e.Value = data.KQ_XL_DV; break;
                            case "Thang_12_Nam_Cu": e.Value = data.T12; break;
                            case "Sau_Thang_Dau_Nam": e.Value = data.Sau_Thang_Dau_Nam; break;
                            case "TongKet_Nam": e.Value = data.TongKet_Nam; break;
                            case "TS_Loai1": e.Value = data.TS_Loai1; break;
                            case "TS_Loai2": e.Value = data.TS_Loai2; break;
                            case "TS_Loai3": e.Value = data.TS_Loai3; break;
                            case "TS_Loai4": e.Value = data.TS_Loai4; break;
                            default:
                                if (colName.StartsWith("Thang_") && int.TryParse(colName.Replace("Thang_", ""), out int tIdx) && tIdx >= 1 && tIdx <= 11)
                                    e.Value = data.Thang[tIdx - 1];
                                // SỬA LỖI TÊN BIẾN TẠI ĐÂY
                                else if (data.CotPhatSinh != null && data.CotPhatSinh.TryGetValue(colName, out string valP)) e.Value = valP;
                                else e.Value = string.Empty;
                                break;
                        }
                    }
                }
                else
                {
                    if (_dtHienTai == null) return;
                    if (!_colIndexMap.TryGetValue(e.ColumnIndex, out int dtIndex)) return;
                    object value = _dtHienTai.DefaultView[actualViewIndex][dtIndex];
                    e.Value = (value == DBNull.Value || value == null) ? string.Empty : value;
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
                _rowHeaderFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
            }

            string stt = (e.RowIndex + 1).ToString();
            Rectangle headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);
            e.Graphics.DrawString(stt, grid.Font, _rowHeaderBrush, headerBounds, _rowHeaderFormat);
        }
        private void DinhDangGridBanDau()
        {
            kryptonDataGridView1.RowHeadersVisible = true;
            kryptonDataGridView1.RowHeadersWidth = 60;
        }
        public void ApDungDinhDangGridThongKe(bool laTanBinh)
        {
            var grid = kryptonDataGridView1;
            int namHeThong = Module_NamHeThong.LayNamHeThong();
            int namCu = namHeThong - 1;

            var headerMap = new Dictionary<string, string>
            {
                ["HoVaTen"] = "Họ và tên",
                ["SoHieu"] = "Số hiệu",
                ["DonVi"] = "Đơn vị",
                ["TinhTrang"] = "Tình trạng",
                ["GhiChu"] = "Ghi chú"
            };

            if (laTanBinh)
            {
                headerMap["Tuan_1_T2"] = "Tuần 1\nTháng 2"; headerMap["Tuan_2_T2"] = "Tuần 2\nTháng 2"; headerMap["Tuan_3_T2"] = "Tuần 3\nTháng 2"; headerMap["Tuan_4_T2"] = "Tuần 4\nTháng 2"; headerMap["Thang_2"] = "Kết quả\nTháng 2";
                headerMap["Tuan_1_T3"] = "Tuần 1\nTháng 3"; headerMap["Tuan_2_T3"] = "Tuần 2\nTháng 3"; headerMap["Tuan_3_T3"] = "Tuần 3\nTháng 3"; headerMap["Tuan_4_T3"] = "Tuần 4\nTháng 3"; headerMap["Thang_3"] = "Kết quả\nTháng 3";
                headerMap["Tuan_1_T4"] = "Tuần 1\nTháng 4"; headerMap["Tuan_2_T4"] = "Tuần 2\nTháng 4"; headerMap["Tuan_3_T4"] = "Tuần 3\nTháng 4"; headerMap["Tuan_4_T4"] = "Tuần 4\nTháng 4"; headerMap["Thang_4"] = "Kết quả\nTháng 4";
                headerMap["Tuan_1_T5"] = "Tuần 1\nTháng 5"; headerMap["Tuan_2_T5"] = "Tuần 2\nTháng 5"; headerMap["Tuan_3_T5"] = "Tuần 3\nTháng 5"; headerMap["Tuan_4_T5"] = "Tuần 4\nTháng 5"; headerMap["Thang_5"] = "Kết quả\nTháng 5";
                headerMap["Tuan_1_T6"] = "Tuần 1\nTháng 6"; headerMap["Tuan_2_T6"] = "Tuần 2\nTháng 6"; headerMap["Tuan_3_T6"] = "Tuần 3\nTháng 6"; headerMap["Tuan_4_T6"] = "Tuần 4\nTháng 6"; headerMap["Thang_6"] = "Kết quả\nTháng 6";
            }
            else
            {
                headerMap["KQ_ThiDua_Nam_Cu"] = $"KQ thi đua\nnăm {namCu}";
                headerMap["KQ_XepLoaiCB_Nam_Cu"] = $"Xếp loại CBCS\nnăm {namCu}";
                headerMap["KQ_XepLoaiDangVien_Nam_Cu"] = $"XL đảng viên\nnăm {namCu}";
                headerMap["Thang_12_Nam_Cu"] = $"Tháng 12\n{namCu}";
                for (int i = 1; i <= 11; i++) headerMap[$"Thang_{i}"] = $"Tháng {i}";
                headerMap["Sau_Thang_Dau_Nam"] = "6 tháng\nđầu năm";
                headerMap["TongKet_Nam"] = "Tổng kết\nnăm";
            }

            headerMap["TS_Loai1"] = "Tổng\nLoại 1";
            headerMap["TS_Loai2"] = "Tổng\nLoại 2";
            headerMap["TS_Loai3"] = "Tổng\nLoại 3";
            headerMap["TS_Loai4"] = "Tổng\nLoại 4";

            foreach (var kv in headerMap)
                if (grid.Columns.Contains(kv.Key)) grid.Columns[kv.Key].HeaderText = kv.Value;

            if (grid.Columns.Contains("ID")) grid.Columns["ID"].Visible = false;

            if (laTanBinh)
            {
                if (grid.Columns.Contains("Thang_2")) grid.Columns["Thang_2"].Visible = false;
                if (grid.Columns.Contains("Tuan_1_T6")) grid.Columns["Tuan_1_T6"].Visible = false;
                if (grid.Columns.Contains("Tuan_2_T6")) grid.Columns["Tuan_2_T6"].Visible = false;
                if (grid.Columns.Contains("Tuan_3_T6")) grid.Columns["Tuan_3_T6"].Visible = false;
                if (grid.Columns.Contains("Tuan_4_T6")) grid.Columns["Tuan_4_T6"].Visible = false;
            }

            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Color.FromArgb(235, 235, 235);
            grid.EnableHeadersVisualStyles = false;

            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 85;
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(240, 244, 248),
                ForeColor = Color.FromArgb(40, 40, 40),
                WrapMode = DataGridViewTriState.True,
                Padding = new Padding(3)
            };

            grid.RowTemplate.Height = 32;
            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(45, 45, 45),
                SelectionBackColor = Color.FromArgb(232, 244, 253),
                SelectionForeColor = Color.FromArgb(0, 102, 204),
                Padding = new Padding(2, 0, 2, 0)
            };
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 252, 252);
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;

            if (grid.Columns.Contains("HoVaTen"))
            {
                var col = grid.Columns["HoVaTen"];

                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                col.MinimumWidth = 220;
                col.FillWeight = 40;

                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                col.DefaultCellStyle.Padding = new Padding(5, 0, 0, 0);
            }
            if (grid.Columns.Contains("TinhTrang"))
            {
                var col = grid.Columns["TinhTrang"];
                col.Width = 140;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            if (grid.Columns.Contains("GhiChu"))
            {
                var col = grid.Columns["GhiChu"];
                col.Width = 220;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                col.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                col.DefaultCellStyle.ForeColor = Color.DarkSlateGray;
                col.DefaultCellStyle.Padding = new Padding(5, 0, 0, 0);
            }

            foreach (DataGridViewColumn col in grid.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
                if (col.Name == "HoVaTen" || col.Name == "ID" || col.Name == "TinhTrang" || col.Name == "GhiChu") continue;
                col.Width = 90;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            if (laTanBinh)
            {
                string[] nhomToMauXanhDuong = { "Tuan_1_T2", "Tuan_2_T2", "Tuan_3_T2", "Tuan_4_T2", "Thang_3", "Tuan_1_T4", "Tuan_2_T4", "Tuan_3_T4", "Tuan_4_T4", "Thang_5" };
                foreach (var name in nhomToMauXanhDuong)
                    if (grid.Columns.Contains(name)) grid.Columns[name].DefaultCellStyle.BackColor = Color.FromArgb(235, 245, 255);

                string[] cacCotKetQua = { "Thang_3", "Thang_4", "Thang_5", "Thang_6" };
                foreach (var name in cacCotKetQua)
                {
                    if (grid.Columns.Contains(name))
                    {
                        grid.Columns[name].DefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                        grid.Columns[name].DefaultCellStyle.ForeColor = Color.FromArgb(0, 80, 160);
                    }
                }
            }

            string[] nhomToMauXanhLa = { "TS_Loai1", "TS_Loai2", "TS_Loai3", "TS_Loai4" };
            foreach (var name in nhomToMauXanhLa)
            {
                if (grid.Columns.Contains(name))
                {
                    grid.Columns[name].DefaultCellStyle.BackColor = Color.FromArgb(240, 252, 240);
                    grid.Columns[name].DefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                }
            }

            int viTri = 0;
            Action<string> XepCot = (colName) => { if (grid.Columns.Contains(colName)) grid.Columns[colName].DisplayIndex = viTri++; };

            XepCot("HoVaTen");
            XepCot("SoHieu");
            XepCot("DonVi");
            XepCot("TinhTrang");

            if (laTanBinh)
            {
                XepCot("Tuan_1_T2"); XepCot("Tuan_2_T2"); XepCot("Tuan_3_T2"); XepCot("Tuan_4_T2"); XepCot("Thang_3");
                XepCot("Tuan_1_T3"); XepCot("Tuan_2_T3"); XepCot("Tuan_3_T3"); XepCot("Tuan_4_T3"); XepCot("Thang_4");
                XepCot("Tuan_1_T4"); XepCot("Tuan_2_T4"); XepCot("Tuan_3_T4"); XepCot("Tuan_4_T4"); XepCot("Thang_5");
                XepCot("Tuan_1_T5"); XepCot("Tuan_2_T5"); XepCot("Tuan_3_T5"); XepCot("Tuan_4_T5"); XepCot("Thang_6");
            }
            else
            {
                XepCot("KQ_ThiDua_Nam_Cu"); XepCot("KQ_XepLoaiCB_Nam_Cu"); XepCot("KQ_XepLoaiDangVien_Nam_Cu"); XepCot("Thang_12_Nam_Cu");
                for (int i = 1; i <= 11; i++) XepCot($"Thang_{i}");
                XepCot("Sau_Thang_Dau_Nam"); XepCot("TongKet_Nam");
            }
            XepCot("TS_Loai1"); XepCot("TS_Loai2"); XepCot("TS_Loai3"); XepCot("TS_Loai4");
            XepCot("GhiChu");
        }
        private void kryptonButton_Dong_Click(object sender, EventArgs e)
        {
            int namHienTai = Module_NamHeThong.LayNamHeThong();
            string tieuDeForm = $"Thống kê kết quả phân loại thi đua \"VÌ ANTQ\" năm {namHienTai}";

            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            if (formCha != null)
            {
                var panel = formCha.Controls.Find("PanelContainer", true).FirstOrDefault() as Panel;
                if (panel != null)
                {
                    var form15 = panel.Controls.OfType<Form15_ThongKeThiDua>().FirstOrDefault();
                    if (form15 == null)
                    {
                        form15 = new Form15_ThongKeThiDua { TopLevel = false, FormBorderStyle = FormBorderStyle.None, Dock = DockStyle.Fill };
                        panel.Controls.Add(form15);
                    }

                    foreach (Control ctl in panel.Controls) if (ctl is Form f && f != form15) f.Hide();

                    form15.Show();
                    form15.BringToFront();
                    formCha.CapNhatTieuDe(tieuDeForm);
                    form15.ReloadData();
                }
            }
            this.Close();
        }
        private void Form46_ThongKeThiDuaNamCu_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_timKiemTimer != null)
            {
                _timKiemTimer.Stop();
                _timKiemTimer.Dispose();
            }
            _rowHeaderBrush?.Dispose();
            _rowHeaderFormat?.Dispose();
            _dtHienTai?.Dispose();
            _filteredIndexes?.Clear();
        }
        private async void kryptonButton_CapNhat_Click(object sender, EventArgs e)
        {
            // ⭐ ĐỌC DỮ LIỆU TỪ GIAO DIỆN (UI THREAD) TRƯỚC KHI VÀO TASK.RUN
            var selectedFile = comboBox_ChonCSDLNam.SelectedItem as FileLichSuDTO;

            // =========================================================
            // 1. LỚP VỎ UX: LƯU TRẠNG THÁI GỐC ĐỂ PHỤC HỒI SAU KHI XONG
            // =========================================================
            string textBanDau = kryptonButton_CapNhat.Values.Text;
            Image anhBanDau = kryptonButton_CapNhat.Values.Image;

            Form_Loading frmLoad = new Form_Loading("Đang làm mới dữ liệu từ cơ sở dữ liệu...");
            frmLoad.Icon = this.Icon;
            bool isLoadShown = false;

            try
            {
                kryptonButton_CapNhat.Enabled = false;
                kryptonButton_CapNhat.Values.Text = "Đang tải...";
                kryptonButton_CapNhat.Values.Image = null;

                this.Enabled = false;
                frmLoad.Show(this);
                isLoadShown = true;

                await Task.Delay(100);

                // ⭐ NẾU CÓ CHỌN TỆP THÌ MỚI CHẠY ĐỐI CHIẾU
                if (selectedFile != null)
                {
                    string pathCsdl2 = Module_DanduongGPS.DuongDanCSDL2;
                    string pathCsdlNamCu = selectedFile.DuongDan;
                    bool laTanBinh = selectedFile.LaTanBinh;

                    // Chạy ngầm an toàn vì các biến pathCsdl2, pathCsdlNamCu là biến chữ (string) độc lập
                    await Task.Run(() =>
                    {
                        Module_HoTroLuuDataTheoNamCu.CapNhatTinhTrangThiDuaNamCu(pathCsdl2, pathCsdlNamCu, laTanBinh);
                    });
                }

                // Nạp lại dữ liệu hiển thị lên lưới
                ThucHienTaiDuLieuLichSu();

                try { Module_ThongBao.ThanhCong("Đã cập nhật dữ liệu mới nhất!"); } catch { }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi cập nhật dữ liệu F46: {ex.Message}");
                MessageBox.Show($"Có lỗi xảy ra khi làm mới dữ liệu:\n{ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (isLoadShown)
                {
                    frmLoad.Close();
                    this.Enabled = true;
                    this.Focus();
                }

                kryptonButton_CapNhat.Values.Text = textBanDau;
                kryptonButton_CapNhat.Values.Image = anhBanDau;
                kryptonButton_CapNhat.Enabled = true;

                kryptonDataGridView1.Refresh();
            }
        }
        private async void kryptonButton_XuatData_Click(object sender, EventArgs e)
        {
            // =========================================================
            // 1. LỚP VỎ UX: LƯU TRẠNG THÁI GỐC CHỐNG CLICK TRÙNG LUỒNG
            // =========================================================
            string textBanDau = kryptonButton_XuatData.Values.Text;
            Image anhBanDau = kryptonButton_XuatData.Values.Image;

            // Chốt chặn kiểm tra dữ liệu hiển thị
            if (kryptonDataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu trên lưới để xuất tệp!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (comboBox_ChonCSDLNam.SelectedItem == null) return;
            var selectedFile = (FileLichSuDTO)comboBox_ChonCSDLNam.SelectedItem;

            // =========================================================
            // ⭐ XỬ LÝ TÊN TỆP ĐỘNG THEO YÊU CẦU
            // =========================================================
            string loai = selectedFile.LaTanBinh ? "TanBinh" : "CBCS";
            // Trích xuất năm từ chuỗi "Năm 2026 - CBCS" -> lấy số 2026
            string[] parts = selectedFile.TenHienThi.Split(' ');
            string nam = (parts.Length > 1) ? parts[1] : DateTime.Now.Year.ToString();
            string fileName = $"ThongKeThiDua_{loai}_Nam{nam}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            // =========================================================
            // 2. KHỞI TẠO HỘP THOẠI LƯU TỆP EXCEL
            // =========================================================
            using var sfd = new SaveFileDialog
            {
                Title = "Chọn nơi lưu file Excel thống kê",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = fileName, // Tên tệp động đã cấu hình
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;
            string targetExcelPath = sfd.FileName;

            // Khởi tạo màn hình Loading
            Form_Loading frmLoad = new Form_Loading("Đang khởi tạo tệp cấu trúc excel lịch sử, vui lòng đợi...");
            frmLoad.Icon = this.Icon;
            bool isLoadShown = false;

            try
            {
                kryptonButton_XuatData.Enabled = false;
                kryptonButton_XuatData.Values.Text = "Đang tạo...";
                kryptonButton_XuatData.Values.Image = null;
                await Task.Delay(100);

                // =========================================================
                // 3. ĐỌC TIÊN QUYẾT TIÊU CHÍ CỘT THỰC TẾ TRONG FILE SQLITE
                // =========================================================
                var cotTrongBang = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string tenBang = selectedFile.LaTanBinh ? "ThiDuaThang_TanBinh" : "ThiDuaThang";

                using (var cn = new SqliteConnection($"Data Source={selectedFile.DuongDan}"))
                {
                    cn.Open();
                    using var cmd = new SqliteCommand($"PRAGMA table_info([{tenBang}])", cn);
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        string cot = rd["name"].ToString();
                        if (!cot.Equals("ID", StringComparison.OrdinalIgnoreCase))
                            cotTrongBang.Add(cot);
                    }
                }

                // =========================================================================
                // 4. TRÍCH XUẤT SIÊU DỮ LIỆU CỘT
                // =========================================================================
                var columnsMeta = kryptonDataGridView1.Columns
                    .Cast<DataGridViewColumn>()
                    .Where(c => cotTrongBang.Contains(c.Name))
                    .Where(c =>
                    {
                        if (selectedFile.LaTanBinh)
                        {
                            if (c.Name.Equals("Thang_2", StringComparison.OrdinalIgnoreCase)) return false;
                            if (c.Name.StartsWith("Tuan_") && c.Name.EndsWith("_T6")) return false;
                        }
                        return true;
                    })
                    .Select(c => new ColumnExportMeta { Name = c.Name, HeaderText = c.HeaderText })
                    .ToList();

                // Đẩy thứ tự các nhóm cột tổng hợp (TS_Loai) ra phía sau
                var normalCols = columnsMeta.Where(c => !c.Name.StartsWith("TS_Loai")).ToList();
                var totalCols = columnsMeta.Where(c => c.Name.StartsWith("TS_Loai")).OrderBy(c => c.Name).ToList();
                columnsMeta = normalCols.Concat(totalCols).ToList();

                this.Enabled = false;
                frmLoad.Show(this);
                isLoadShown = true;
                await Task.Delay(50);

                string tenTieuDoan = XacDinhTenTieuDoan();

                // =========================================================================
                // 5. KÍCH HOẠT TIẾN TRÌNH LUỒNG NỀN
                // =========================================================================
                await Task.Run(() =>
                {
                    Module_HoTroLuuDataTheoNamCu.XuatExcelLichSuCore(
                        targetExcelPath,
                        selectedFile.LaTanBinh,
                        _isDataMaxMode,
                        columnsMeta,
                        _filteredIndexes,
                        _dtHienTai,
                        _dataCacheCBCS,
                        _dataCacheTanBinh,
                        tenTieuDoan
                    );
                });

                // 6. GHI LOG
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Xuất Excel năm cũ", $"Tệp lịch sử: {Path.GetFileName(selectedFile.DuongDan)} | Số dòng: {_filteredIndexes.Count}");

                if (isLoadShown) { frmLoad.Close(); this.Enabled = true; isLoadShown = false; }
                Module_XuatNhapDuLieuThiDua.MoVaChonTepTrongExplorer(targetExcelPath);
                // MessageBox.Show("Xuất tệp báo cáo dữ liệu năm cũ ra Excel thành công!", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gặp lỗi nghiêm trọng khi xuất báo cáo năm cũ:\n" + ex.Message, "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        private string SafeDecrypt(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            try
            {
                string result = BaoMatAES.GiaiMa(input);
                return string.IsNullOrEmpty(result) ? input : result;
            }
            catch { return input; }
        }
        private string XacDinhTenTieuDoan()
        {
            try
            {
                using var conn = new SqliteConnection($"Data Source={Module_DanduongGPS.DuongDanCSDL2}");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT TenTieuDoan FROM ThongTin WHERE ID=1";
                var result = cmd.ExecuteScalar();
                return (result != null) ? SafeDecrypt(result.ToString().Trim()) : "Không xác định";
            }
            catch { return "Không xác định"; }
        }
        private void lamMoiTrangToolStripMenuItem_Click(object sender, EventArgs e)
       => kryptonButton_CapNhat.PerformClick();
        private void ThucHienTaiDuLieuLichSu()
        {
            // ⭐ CHÈN BỔ SUNG KHỐI NÀY VÀO ĐẦU HÀM:
            if (comboBox_ChonCSDLNam.SelectedItem == null)
            {
                kryptonDataGridView1.RowCount = 0;
                _filteredIndexes.Clear();
                CapNhatTrangThaiHienThi();
                return;
            }
            if (comboBox_ChonCSDLNam.SelectedItem is FileLichSuDTO selectedFile)
            {
                this.Cursor = Cursors.WaitCursor;
                // ⭐ ĐỒNG BỘ TÌNH TRẠNG NGAY TRƯỚC KHI LOAD
                Module_HoTroLuuDataTheoNamCu.CapNhatTinhTrangLichSuTuDanhSachGoc(
                    Module_DanduongGPS.DuongDanCSDL2,
                    selectedFile.DuongDan,
                    selectedFile.LaTanBinh
                );
                kryptonDataGridView1.SuspendLayout();
                try
                {
                    string tenBang = selectedFile.LaTanBinh ? "ThiDuaThang_TanBinh" : "ThiDuaThang";
                    // 1. Kiểm tra nhanh số lượng quân số để kích hoạt động cơ tương ứng
                    int tongSo = 0;
                    using (var cn = new SqliteConnection($"Data Source={selectedFile.DuongDan}"))
                    {
                        cn.Open();
                        using var cmd = new SqliteCommand($"SELECT COUNT(ID) FROM [{tenBang}]", cn);
                        tongSo = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    _isDataMaxMode = tongSo > NGUONG_DU_LIEU;

                    // 2. Chạy rẽ nhánh động cơ nạp
                    if (_isDataMaxMode)
                    {
                        _dataCacheCBCS.Clear();
                        _dataCacheTanBinh.Clear();
                        var schemaDt = new DataTable();
                        _cotPhatSinhCacheThongKe = new List<string>();

                        using (var cn = new SqliteConnection($"Data Source={selectedFile.DuongDan}"))
                        {
                            cn.Open();
                            using var cmd = new SqliteCommand($"SELECT * FROM [{tenBang}]", cn);
                            using var rd = cmd.ExecuteReader();

                            for (int i = 0; i < rd.FieldCount; i++) schemaDt.Columns.Add(rd.GetName(i), typeof(string));
                            schemaDt.Columns.Add("GhiChu", typeof(string));

                            string[] baseColsCBCS = { "ID", "HoVaTen", "SoHieu", "DonVi", "TinhTrang", "KQ_ThiDua_Nam_Cu", "KQ_XepLoaiCB_Nam_Cu", "KQ_XepLoaiDangVien_Nam_Cu", "Thang_12_Nam_Cu", "Thang_1", "Thang_2", "Thang_3", "Thang_4", "Thang_5", "Sau_Thang_Dau_Nam", "Thang_6", "Thang_7", "Thang_8", "Thang_9", "Thang_10", "Thang_11", "TongKet_Nam", "TS_Loai1", "TS_Loai2", "TS_Loai3", "TS_Loai4" };
                            string[] baseColsTB = { "ID", "HoVaTen", "SoHieu", "DonVi", "TinhTrang", "Tuan_1_T2", "Tuan_2_T2", "Tuan_3_T2", "Tuan_4_T2", "Thang_2", "Tuan_1_T3", "Tuan_2_T3", "Tuan_3_T3", "Tuan_4_T3", "Thang_3", "Tuan_1_T4", "Tuan_2_T4", "Tuan_3_T4", "Tuan_4_T4", "Thang_4", "Tuan_1_T5", "Tuan_2_T5", "Tuan_3_T5", "Tuan_4_T5", "Thang_5", "Tuan_1_T6", "Tuan_2_T6", "Tuan_3_T6", "Tuan_4_T6", "Thang_6", "TS_Loai1", "TS_Loai2", "TS_Loai3", "TS_Loai4" };
                            var baseCols = selectedFile.LaTanBinh ? baseColsTB : baseColsCBCS;

                            foreach (DataColumn col in schemaDt.Columns)
                            {
                                if (!baseCols.Contains(col.ColumnName) && col.ColumnName != "GhiChu")
                                    _cotPhatSinhCacheThongKe.Add(col.ColumnName);
                            }

                            string[] donViUuTien = Module_DonVi.LayDanhSachDonViUuTienArray();
                            var dicDonViUuTien = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                            for (int i = 0; i < donViUuTien.Length; i++) dicDonViUuTien.TryAdd(donViUuTien[i], i);

                            while (rd.Read())
                            {
                                if (selectedFile.LaTanBinh)
                                {
                                    var item = new HistoryTanBinhDTO
                                    {
                                        ID = rd["ID"]?.ToString() ?? "",
                                        HoVaTen = SafeDecrypt(rd["HoVaTen"]?.ToString()),
                                        SoHieu = SafeDecrypt(rd["SoHieu"]?.ToString()),
                                        DonVi = SafeDecrypt(rd["DonVi"]?.ToString()),
                                        TinhTrang = rd["TinhTrang"]?.ToString() ?? "",
                                        TS_Loai1 = rd["TS_Loai1"]?.ToString() ?? "",
                                        TS_Loai2 = rd["TS_Loai2"]?.ToString() ?? "",
                                        TS_Loai3 = rd["TS_Loai3"]?.ToString() ?? "",
                                        TS_Loai4 = rd["TS_Loai4"]?.ToString() ?? "",
                                        GhiChu = ""
                                    };
                                    item.HoVaTen_Search = item.HoVaTen.ToLowerInvariant();
                                    item.SortPriority = dicDonViUuTien.TryGetValue(item.DonVi, out int p) ? p : int.MaxValue;

                                    for (int m = 2; m <= 6; m++)
                                    {
                                        for (int t = 1; t <= 4; t++)
                                        {
                                            string cName = $"Tuan_{t}_T{m}";
                                            if (schemaDt.Columns.Contains(cName)) item.DuLieuThang[cName] = rd[cName]?.ToString() ?? "";
                                        }
                                        string cThang = $"Thang_{m}";
                                        if (schemaDt.Columns.Contains(cThang)) item.DuLieuThang[cThang] = rd[cThang]?.ToString() ?? "";
                                    }
                                    foreach (var c in _cotPhatSinhCacheThongKe) item.CotPhatSinh[c] = rd[c]?.ToString() ?? "";
                                    _dataCacheTanBinh.Add(item);
                                }
                                else
                                {
                                    var item = new HistoryCBCSDTO
                                    {
                                        ID = rd["ID"]?.ToString() ?? "",
                                        HoVaTen = SafeDecrypt(rd["HoVaTen"]?.ToString()),
                                        SoHieu = SafeDecrypt(rd["SoHieu"]?.ToString()),
                                        DonVi = SafeDecrypt(rd["DonVi"]?.ToString()),
                                        TinhTrang = rd["TinhTrang"]?.ToString() ?? "",
                                        KQ_TD = rd["KQ_ThiDua_Nam_Cu"]?.ToString() ?? "",
                                        KQ_XL_CB = rd["KQ_XepLoaiCB_Nam_Cu"]?.ToString() ?? "",
                                        KQ_XL_DV = rd["KQ_XepLoaiDangVien_Nam_Cu"]?.ToString() ?? "",
                                        T12 = rd["Thang_12_Nam_Cu"]?.ToString() ?? "",
                                        Sau_Thang_Dau_Nam = rd["Sau_Thang_Dau_Nam"]?.ToString() ?? "",
                                        TongKet_Nam = rd["TongKet_Nam"]?.ToString() ?? "",
                                        TS_Loai1 = rd["TS_Loai1"]?.ToString() ?? "",
                                        TS_Loai2 = rd["TS_Loai2"]?.ToString() ?? "",
                                        TS_Loai3 = rd["TS_Loai3"]?.ToString() ?? "",
                                        TS_Loai4 = rd["TS_Loai4"]?.ToString() ?? "",
                                        GhiChu = ""
                                    };
                                    item.HoVaTen_Search = item.HoVaTen.ToLowerInvariant();
                                    item.SortPriority = dicDonViUuTien.TryGetValue(item.DonVi, out int p) ? p : int.MaxValue;

                                    for (int i = 1; i <= 11; i++) item.Thang[i - 1] = rd[$"Thang_{i}"]?.ToString() ?? "";
                                    foreach (var c in _cotPhatSinhCacheThongKe) item.CotPhatSinh[c] = rd[c]?.ToString() ?? "";
                                    _dataCacheCBCS.Add(item);
                                }
                            }
                        }

                        if (selectedFile.LaTanBinh) _dataCacheTanBinh = _dataCacheTanBinh.OrderBy(x => x.SortPriority).ThenBy(x => int.Parse(x.ID)).ToList();
                        else _dataCacheCBCS = _dataCacheCBCS.OrderBy(x => x.SortPriority).ThenBy(x => int.Parse(x.ID)).ToList();

                        _dtHienTai = schemaDt;
                    }
                    else
                    {
                        // Chế độ Standard (<= 3000 dòng): Giữ nguyên cơ chế DataTable gốc
                        _dtHienTai = Module_HoTroLuuDataTheoNamCu.LoadDataFromHistoryDB(selectedFile.DuongDan, tenBang);
                    }

                    KhoiTaoBoLocComboBox(selectedFile.LaTanBinh);

                    kryptonDataGridView1.DataSource = null;
                    kryptonDataGridView1.Columns.Clear();
                    foreach (DataColumn dc in _dtHienTai.Columns)
                    {
                        if (dc.ColumnName != "SortPriority" && dc.ColumnName != "HoVaTen_Search")
                            kryptonDataGridView1.Columns.Add(dc.ColumnName, dc.ColumnName);
                    }

                    ApDungDinhDangGridThongKe(selectedFile.LaTanBinh);

                    _colIndexMap.Clear();
                    for (int i = 0; i < kryptonDataGridView1.Columns.Count; i++)
                    {
                        string colName = kryptonDataGridView1.Columns[i].Name;
                        if (_dtHienTai.Columns.Contains(colName)) _colIndexMap[i] = _dtHienTai.Columns.IndexOf(colName);
                    }

                    ApplyFilter();
                }
                catch (Exception ex) { Debug.WriteLine("Lỗi nạp RAM DTO lịch sử: " + ex.Message); }
                finally
                {
                    kryptonDataGridView1.ResumeLayout(); this.Cursor = Cursors.Default;
                    BeginInvoke(new Action(() =>
                    {
                        if (comboBox_TimKiemDonVi.Items.Count > 0)
                            comboBox_TimKiemDonVi.SelectedIndex = 0;

                        if (comboBox1_TinhTrang.Items.Count > 0)
                            comboBox1_TinhTrang.SelectedIndex = 0;

                        if (comboBox1_PhanLoaiThiDuaNamCu.Visible &&
                            comboBox1_PhanLoaiThiDuaNamCu.Items.Count > 0)
                            comboBox1_PhanLoaiThiDuaNamCu.SelectedIndex = 0;
                    }));
                }
            }

        }
        private void xoaTimKiem_ToolStripMenuItem_Click(object sender, EventArgs e) => kryptonButton_LamMoiCacOTimKiem.PerformClick();
        private void xuatDuLieuTepExcel_ToolStripMenuItem_Click(object sender, EventArgs e) => kryptonButton_XuatData.PerformClick();
        private void xoaCSDL_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // =========================================================
            // 1. KIỂM TRA TỆP CSDL LỊCH SỬ ĐANG ĐƯỢC CHỌN
            // =========================================================
            if (comboBox_ChonCSDLNam.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn tệp CSDL năm cũ cần xóa trên danh sách!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var selectedFile = (FileLichSuDTO)comboBox_ChonCSDLNam.SelectedItem;
            string fileToDelete = selectedFile.DuongDan;

            if (string.IsNullOrEmpty(fileToDelete) || !File.Exists(fileToDelete))
            {
                MessageBox.Show("Tệp CSDL không tồn tại trên ổ đĩa hoặc đã bị xóa trước đó!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoadDanhSachFileLichSu(); // Rà soát lại danh sách tệp
                return;
            }

            // =========================================================
            // 2. BẢO VỆ MẤT DỮ LIỆU: HỎI XÁC NHẬN CẢNH BÁO NGUY HIỂM
            // =========================================================
            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn Xóa vĩnh viễn tệp CSDL lưu trữ lịch sử:\n👉 {selectedFile.TenHienThi}\n⚠️ Cảnh báo: Tệp này sẽ bị xóa khỏi ổ cứng và không thể khôi phục!",
                "Xác nhận xóa tệp CSDL năm cũ",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirm != DialogResult.Yes)
                return;

            // =========================================================
            // 3. XÁC MINH QUYỀN ADMIN (FORM 24)
            // =========================================================
            DialogResult kq;
            using (Form24_XacMinhAdmin frm = new Form24_XacMinhAdmin())
            {
                frm.TopMost = true;
                frm.StartPosition = FormStartPosition.CenterScreen;
                kq = frm.ShowDialog();
            }

            if (kq != DialogResult.OK)
                return;

            // =========================================================
            // 4. ⭐ QUY TRÌNH BẢO AN: CẮT LIÊN KẾT GRID VÀ DỌN SẠCH RAM TRƯỚC
            // =========================================================
            try
            {
                // 4.1. Ngắt tạm thời sự kiện ComboBox để tránh tự động gọi ThucHienTaiDuLieuLichSu() dồn dập
                comboBox_ChonCSDLNam.SelectedIndexChanged -= comboBox_ChonCSDLNam_SelectedIndexChanged;

                // 4.2. Khóa vẽ giao diện & Đưa RowCount của Virtual Mode về 0 lập tức (Tránh CellValueNeeded đòi dữ liệu)
                kryptonDataGridView1.SuspendLayout();
                kryptonDataGridView1.RowCount = 0;
                kryptonDataGridView1.DataSource = null;
                kryptonDataGridView1.Columns.Clear();

                // 4.3. Giải phóng hoàn toàn các mảng đệm RAM & Hủy DataTable đang kết nối CSDL
                _filteredIndexes.Clear();
                _dataCacheCBCS.Clear();
                _dataCacheTanBinh.Clear();
                _colIndexMap.Clear();

                if (_dtHienTai != null)
                {
                    _dtHienTai.Dispose();
                    _dtHienTai = null;
                }

                kryptonDataGridView1.ResumeLayout();

                // 4.4. ⭐ THÁO KHÓA SQLITE: Xóa toàn bộ Pool kết nối và ép Garbage Collector thu hồi bộ nhớ ngầm
                SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // =========================================================
                // 5. THỰC HIỆN XÓA TỆP TRÊN Ổ ĐĨA
                // =========================================================
                File.Delete(fileToDelete);

                // Ghi nhật ký thao tác hệ thống
                try
                {
                    Module_NhatKy.GhiNhatKy(
                        Module_TaiKhoan.TenTaiKhoan_RAM,
                        "Xóa tệp CSDL năm cũ",
                        $"Đã xóa tệp lịch sử: {selectedFile.TenHienThi} ({Path.GetFileName(fileToDelete)})");
                }
                catch { }
                //CapNhatTrangThaiHienThi();
                MessageBox.Show($"Đã xóa vĩnh viễn tệp CSDL lịch sử:\n👉 {selectedFile.TenHienThi}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // 6. NẠP LẠI DANH SÁCH & CẬP NHẬT TỰ ĐỘNG GIAO DIỆN
                // Quét lại thư mục CSDL lịch sử
                LoadDanhSachFileLichSu();
                // Khôi phục lại ủy quyền sự kiện cho ComboBox
                comboBox_ChonCSDLNam.SelectedIndexChanged += comboBox_ChonCSDLNam_SelectedIndexChanged;

                if (comboBox_ChonCSDLNam.Items.Count > 0)
                {
                    // Nếu vẫn còn tệp khác -> Tự động chọn tệp đầu tiên và load lên Grid
                    comboBox_ChonCSDLNam.SelectedIndex = 0;
                    ThucHienTaiDuLieuLichSu();
                }
                else
                {
                    // Nếu đã xóa hết tệp -> Xóa trắng các ComboBox tìm kiếm và báo quân số = 0
                    if (comboBox_TimKiemDonVi.Items.Count > 0) comboBox_TimKiemDonVi.SelectedIndex = 0;
                    if (comboBox1_TinhTrang.Items.Count > 0) comboBox1_TinhTrang.SelectedIndex = 0;
                    if (comboBox1_PhanLoaiThiDuaNamCu.Items.Count > 0) comboBox1_PhanLoaiThiDuaNamCu.SelectedIndex = 0;

                    toolStripStatusLabel1.Text = "Tổng cộng: 0 đồng chí";
                    kryptonDataGridView1.Refresh();
                }
            }
            catch (Exception ex)
            {
                // Khôi phục lại sự kiện nếu xảy ra ngoại lệ
                comboBox_ChonCSDLNam.SelectedIndexChanged -= comboBox_ChonCSDLNam_SelectedIndexChanged;
                comboBox_ChonCSDLNam.SelectedIndexChanged += comboBox_ChonCSDLNam_SelectedIndexChanged;

                MessageBox.Show($"Không thể xóa tệp CSDL do tệp đang bị ứng dụng khác chiếm dụng hoặc lỗi hệ thống:\n\n{ex.Message}", "Lỗi xóa tệp", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void dong_ToolStripMenuItem_Click(object sender, EventArgs e) => kryptonButton_Dong.PerformClick();     
    }
        public class ColumnExportMeta
    {
        public string Name { get; set; }
        public string HeaderText { get; set; }
    }
}