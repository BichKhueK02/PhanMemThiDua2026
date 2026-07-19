using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
namespace PhanMemThiDua2026
{
    public partial class Form15_ThongKeThiDua : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private readonly string _csdl4Path = Module_DanduongGPS.DuongDanCSDL4;
        private DataTable dtDanhSachGoc;
        //  private bool _daCauHinhGridThongKe = false;
        private List<string> _cotPhatSinhCacheThongKe;
        private bool _dangMoThongKeTapThe = false;
        private const string PLACEHOLDER_TIMKIEM = "Nhập tìm kiếm";
        private bool _dangSetPlaceholder = false;
        private bool _isInitialized = false;
        //private DataTable filteredTable; // khai báo ở ngoài tất cả các hàm, ngay trong class Form                                        // Dùng cho chế độ CBCS thông thường
        //private Dictionary<int, string> _colMap = new Dictionary<int, string>();
        private Dictionary<int, int> _colIndexMap = new Dictionary<int, int>(); // BẢN ĐỒ TỌA ĐỘ CỘT SIÊU NHANH  
        // BẢN ĐỒ TỌA ĐỘ CỘT SIÊU NHANH
        private SolidBrush _rowHeaderBrush = null;
        private StringFormat _rowHeaderFormat = null;
        private System.Windows.Forms.Timer _timKiemTimer;
        //Khai báo thêm bộ nhớ đệm cho dữ liệu thô để tránh phải truy xuất DataTable nhiều lần trong CellValueNeeded
        private List<RawCBCS> _dataCacheCBCS = new List<RawCBCS>();
        private List<RawTanBinh> _dataCacheTanBinh = new List<RawTanBinh>();
        // 2. Thay thế hàm RowPostPaint cũ bằng hàm này
        // 1. Khai báo Class lưu trữ dữ liệu thô cho CBCS
        // 1. Class lưu trữ dữ liệu thô cho CBCS
        private class RawCBCS
        {
            public string ID, HoTenE, SoHieuE, DonViE, TinhTrang, KQ_TD, KQ_XL_CB, KQ_XL_DV, T12;
            public string[] Thang = new string[11]; // T1 -> T11
            public string SauThang, TongKet, L1, L2, L3, L4;
            public string GhiChu;

            // ⭐ HAI BIẾN ĐỂ TRỊ 10 LỖI TRÊN:
            public string HoTenSearch;
            public int SortPriority;

            public Dictionary<string, string> CotPhatSinh = new Dictionary<string, string>();
        }
        // 2. Class lưu trữ dữ liệu thô cho Tân binh
        private class RawTanBinh
        {
            public string ID, HoTenE, SoHieuE, DonViE, TinhTrang, L1, L2, L3, L4;
            public string GhiChu;

            // ⭐ HAI BIẾN ĐỂ TRỊ 10 LỖI TRÊN:
            public string HoTenSearch;
            public int SortPriority;

            public Dictionary<string, string> DuLieuThang = new Dictionary<string, string>();
            public Dictionary<string, string> CotPhatSinh = new Dictionary<string, string>();
        }
        public Form15_ThongKeThiDua()
        {
            InitializeComponent();
            this.Load += Form15_ThongKeThiDua_Load;
            this.FormClosed += Form15_ThongKeThiDua_FormClosed;
            this.KeyPreview = true; // THÊM DÒNG NÀY
            InitToolTips();
        }
        private void Form15_ThongKeThiDua_Load(object sender, EventArgs e)
        {
            if (_isInitialized) return;
          
            DinhDangDataGirdView_HienThi();
            toolStripProgressBar1_LamMoi.Visible = false;
            toolStripProgressBar1_LamMoi.Enabled = false;
            // 1. KIỂM TRA PHIÊN BẢN VÀ ẨN MENU (DÙNG .Available THAY VÌ .Visible)
            string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
            bool laTanBinh = phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase);

            // Dùng Available là lệnh "tuyệt đối" buộc WinForms ẩn mục này đi
            if (xemThongKeThiDuaTapThe != null)
                xemThongKeThiDuaTapThe.Available = !laTanBinh;

            if (tinhPhanLoaiThang_ToolStripMenuItem != null)
                tinhPhanLoaiThang_ToolStripMenuItem.Available = laTanBinh;

            // (TÙY CHỌN) Nếu lưới đang trống trơn khi mở Form, đồng chí cần bật dòng dưới đây lên để nạp dữ liệu:
            // ReloadData(); 

            _isInitialized = true;
            // 2. ÉP FOCUS CHUẨN KỸ SƯ (Dùng ActiveControl chống trượt)
            this.BeginInvoke(new Action(() =>
            {
                if (this.IsDisposed || !this.IsHandleCreated) return;

                // Ưu tiên đưa con trỏ nhấp nháy vào ô tìm kiếm tên
                if (textBox_TimKiemTheoTen != null && textBox_TimKiemTheoTen.Visible)
                {
                    this.ActiveControl = textBox_TimKiemTheoTen; // Lệnh này mạnh hơn Focus()
                }
                else
                {
                    this.ActiveControl = kryptonDataGridView1;
                }

                // Nếu Grid đã có dữ liệu, bôi xanh dòng đầu tiên để "mồi" phím tắt an toàn
                if (kryptonDataGridView1.RowCount > 0 && kryptonDataGridView1.ColumnCount > 0)
                {
                    try
                    {
                        kryptonDataGridView1.ClearSelection();
                        kryptonDataGridView1.CurrentCell = kryptonDataGridView1.Rows[0].Cells[0];
                        kryptonDataGridView1.Rows[0].Selected = true;
                    }
                    catch { } // Bắt lỗi an toàn nếu Cell đang bị ẩn
                }
            }));
            // 1. Tích hợp giao diện xanh lá phẳng Classic từ module dùng chung (Đã tối ưu)
            Module_MenuChuotPhai.TichHopGiaoDienXanhLa(contextMenuStrip1);
        }
        private void Form15_ThongKeThiDua_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Dọn dẹp Timer
            if (_timKiemTimer != null)
            {
                _timKiemTimer.Stop();
                _timKiemTimer.Dispose();
            }

            // Dọn dẹp cọ vẽ và định dạng GDI+
            _rowHeaderBrush?.Dispose();
            _rowHeaderFormat?.Dispose();
            dtDanhSachGoc?.Dispose();
            _filteredIndexes?.Clear();
            // ⭐ THÊM 2 DÒNG NÀY VÀO:
            _dataCacheCBCS?.Clear();
            _dataCacheTanBinh?.Clear();
        }
        private Action SafeAction(Action action)
        {
            return () =>
            {
                if (action == null) return;
                try { action.Invoke(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Lỗi thực thi phím tắt: " + ex.Message); }
            };
        }
        private bool SafeExecute(Action action)
        {
            SafeAction(action).Invoke();
            return true;
        }
        // BẮT PHÍM TẮT TRÊN FORM 15
        private void DinhDangDataGirdView_HienThi()
        {
            Module_DonVi.KhoiTao();

            textBox_TimKiemTheoTen.TextChanged -= textBox_TimKiemTheoTen_TextChanged;
            textBox_TimKiemTheoTen.TextChanged += textBox_TimKiemTheoTen_TextChanged;

            comboBox_TimKiemDonVi.SelectedIndexChanged -= comboBox_TimKiemDonVi_SelectedIndexChanged;
            comboBox_TimKiemDonVi.SelectedIndexChanged += comboBox_TimKiemDonVi_SelectedIndexChanged;

            comboBox1_TinhTrang.SelectedIndexChanged -= comboBox1_TinhTrang_SelectedIndexChanged;
            comboBox1_TinhTrang.SelectedIndexChanged += comboBox1_TinhTrang_SelectedIndexChanged;

            kryptonDataGridView1.CellDoubleClick -= KryptonDataGridView1_CellDoubleClick;
            kryptonDataGridView1.CellDoubleClick += KryptonDataGridView1_CellDoubleClick;

            kryptonDataGridView1.RowHeadersVisible = true;
            kryptonDataGridView1.RowHeadersWidth = 60;
            kryptonDataGridView1.RowPostPaint -= KryptonDataGridView1_RowPostPaint;
            kryptonDataGridView1.RowPostPaint += KryptonDataGridView1_RowPostPaint;
            kryptonDataGridView1.ContextMenuStrip = contextMenuStrip1;

            // ⭐ KÍCH HOẠT VIRTUAL MODE CHUẨN MỰC
            kryptonDataGridView1.VirtualMode = true;
            kryptonDataGridView1.DataSource = null;

            // Chặn tuyệt đối các tính năng tự giãn và thêm/xóa dòng
            kryptonDataGridView1.AllowUserToAddRows = false;
            kryptonDataGridView1.AllowUserToDeleteRows = false;
            kryptonDataGridView1.AllowUserToResizeRows = false; // ⭐ QUAN TRỌNG: FIX LỖI ẨN DÒNG 9999
            kryptonDataGridView1.ReadOnly = true;

            // Chặn Grid tự quét để tính toán kích thước 
            kryptonDataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            kryptonDataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            kryptonDataGridView1.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;

            // Bộ đệm kép chống chớp giật
            typeof(DataGridView).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(kryptonDataGridView1, true, null);

            kryptonDataGridView1.CellValueNeeded -= KryptonDataGridView1_CellValueNeeded;
            kryptonDataGridView1.CellValueNeeded += KryptonDataGridView1_CellValueNeeded;

            InitPlaceholderTimKiem();

            textBox_TimKiemTheoTen.Enter += (s, e) =>
            {
                if (textBox_TimKiemTheoTen.Text == PLACEHOLDER_TIMKIEM)
                {
                    _dangSetPlaceholder = true;
                    textBox_TimKiemTheoTen.Text = "";
                    textBox_TimKiemTheoTen.ForeColor = SystemColors.WindowText;
                    _dangSetPlaceholder = false;
                }
            };

            textBox_TimKiemTheoTen.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBox_TimKiemTheoTen.Text))
                {
                    InitPlaceholderTimKiem();
                }
            };

            _timKiemTimer = new System.Windows.Forms.Timer();
            _timKiemTimer.Interval = 300; // Trễ 300 mili-giây (chuẩn UX)
            _timKiemTimer.Tick += (s, ev) =>
            {
                _timKiemTimer.Stop();
                ApplyFilter(); // Gõ xong mới lọc
            };
            CapNhatPhienBanPhanMem();
        }
        public void LoadThongKe_CBCS()
        {
            if (!File.Exists(_csdl4Path)) return;

            kryptonDataGridView1.SuspendLayout();
            kryptonDataGridView1.DataSource = null;
            kryptonDataGridView1.Columns.Clear();

            var dt = new DataTable();
            _cotPhatSinhCacheThongKe = new List<string>();

            // 1. TẢI DỮ LIỆU THÔ CỰC NHANH
            using (var cn = new SqliteConnection($"Data Source={_csdl4Path}"))
            {
                cn.Open();
                using var cmd = new SqliteCommand("SELECT * FROM ThiDuaThang", cn);
                using var rd = cmd.ExecuteReader();
                dt.Load(rd); // Chớp mắt là xong
            }

            // 2. LẤY GHI CHÚ
            var dictGhiChu = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var cn2 = new SqliteConnection($"Data Source={_csdl2Path}");
                cn2.Open();
                using var cmdG = new SqliteCommand("SELECT SoHieu, GhiChu FROM DanhSach", cn2);
                using var rdG = cmdG.ExecuteReader();
                while (rdG.Read())
                {
                    string sh = SafeDecrypt(rdG["SoHieu"]?.ToString());
                    if (!string.IsNullOrEmpty(sh)) dictGhiChu.TryAdd(sh, SafeDecrypt(rdG["GhiChu"]?.ToString()));
                }
            }
            catch { }

            // 3. KHỞI TẠO CỘT
            string[] baseCols = { "ID", "HoVaTen", "SoHieu", "DonVi", "TinhTrang", "KQ_ThiDua_Nam_Cu", "KQ_XepLoaiCB_Nam_Cu", "KQ_XepLoaiDangVien_Nam_Cu", "Thang_12_Nam_Cu", "Thang_1", "Thang_2", "Thang_3", "Thang_4", "Thang_5", "Sau_Thang_Dau_Nam", "Thang_6", "Thang_7", "Thang_8", "Thang_9", "Thang_10", "Thang_11", "TongKet_Nam", "TS_Loai1", "TS_Loai2", "TS_Loai3", "TS_Loai4" };
            foreach (DataColumn col in dt.Columns)
            {
                if (!baseCols.Contains(col.ColumnName)) _cotPhatSinhCacheThongKe.Add(col.ColumnName);
            }
            dt.Columns.Add("GhiChu", typeof(string));
            dt.Columns.Add("SortPriority", typeof(int)); // Cột ảo hỗ trợ sắp xếp siêu tốc

            // ⭐ CHUYỂN SANG DICTIONARY O(1) ĐỂ TRÁNH QUÉT MẢNG 10.000 LẦN
            string[] donViUuTien = Module_DonVi.LayDanhSachDonViUuTienArray();
            var dicDonViUuTien = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < donViUuTien.Length; i++) dicDonViUuTien.TryAdd(donViUuTien[i], i);

            // 4. GIẢI MÃ ĐA LUỒNG AN TOÀN
            var updates = new (string hoTen, string soHieu, string donVi, string ghiChu, int priority)[dt.Rows.Count];
            Parallel.For(0, dt.Rows.Count, i =>
            {
                DataRow row = dt.Rows[i];
                string shPlain = SafeDecrypt(row["SoHieu"]?.ToString());
                string dvPlain = SafeDecrypt(row["DonVi"]?.ToString());
                string htPlain = SafeDecrypt(row["HoVaTen"]?.ToString());

                int priority = dicDonViUuTien.TryGetValue(dvPlain, out int p) ? p : int.MaxValue;
                string ghiChu = dictGhiChu.TryGetValue(shPlain, out string gc) ? gc : "";

                updates[i] = (htPlain, shPlain, dvPlain, ghiChu, priority);
            });

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dt.Rows[i]["HoVaTen"] = updates[i].hoTen;
                dt.Rows[i]["SoHieu"] = updates[i].soHieu;
                dt.Rows[i]["DonVi"] = updates[i].donVi;
                dt.Rows[i]["GhiChu"] = updates[i].ghiChu;
                dt.Rows[i]["SortPriority"] = updates[i].priority;
            }

            // 5. ZERO-COPY: KHÔNG DÙNG ToTable() MÀ GÁN THẲNG dt
            dt.DefaultView.Sort = "SortPriority ASC, ID ASC";
            dtDanhSachGoc = dt; // Giữ nguyên RAM gốc
            dtDanhSachGoc.PrimaryKey = new DataColumn[] { dtDanhSachGoc.Columns["ID"] };

            // 6. GẮN VÀO GRID
            kryptonDataGridView1.Columns.Clear();
            foreach (DataColumn dc in dtDanhSachGoc.Columns)
            {
                if (dc.ColumnName != "SortPriority") // Giấu cột ảo đi
                    kryptonDataGridView1.Columns.Add(dc.ColumnName, dc.ColumnName);
            }
            ApDungDinhDangGridThongKe();
            _colIndexMap.Clear();
            for (int i = 0; i < kryptonDataGridView1.Columns.Count; i++)
            {
                string colName = kryptonDataGridView1.Columns[i].Name;
                if (dtDanhSachGoc.Columns.Contains(colName)) _colIndexMap[i] = dtDanhSachGoc.Columns.IndexOf(colName);
            }

            if (dtDanhSachGoc.DefaultView.Count == 0) kryptonDataGridView1.Rows.Clear();
            else kryptonDataGridView1.RowCount = dtDanhSachGoc.DefaultView.Count;

            kryptonDataGridView1.ResumeLayout();
            kryptonDataGridView1.Invalidate();
        }
        public void LoadThongKe_TanBinh()
        {
            if (!File.Exists(_csdl4Path)) return;

            kryptonDataGridView1.SuspendLayout();
            kryptonDataGridView1.DataSource = null;
            kryptonDataGridView1.Columns.Clear();

            var dt = new DataTable();
            _cotPhatSinhCacheThongKe = new List<string>();

            using (var cn = new SqliteConnection($"Data Source={_csdl4Path}"))
            {
                cn.Open();
                using var cmd = new SqliteCommand("SELECT * FROM ThiDuaThang_TanBinh", cn);
                using var rd = cmd.ExecuteReader();
                dt.Load(rd);
            }

            var dictGhiChu = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var cn2 = new SqliteConnection($"Data Source={_csdl2Path}");
                cn2.Open();
                using var cmdG = new SqliteCommand("SELECT SoHieu, GhiChu FROM DanhSach", cn2);
                using var rdG = cmdG.ExecuteReader();
                while (rdG.Read())
                {
                    string sh = SafeDecrypt(rdG["SoHieu"]?.ToString());
                    if (!string.IsNullOrEmpty(sh)) dictGhiChu.TryAdd(sh, SafeDecrypt(rdG["GhiChu"]?.ToString()));
                }
            }
            catch { }

            string[] baseCols = { "ID", "HoVaTen", "SoHieu", "DonVi", "TinhTrang", "Tuan_1_T2", "Tuan_2_T2", "Tuan_3_T2", "Tuan_4_T2", "Thang_3", "Tuan_1_T3", "Tuan_2_T3", "Tuan_3_T3", "Tuan_4_T3", "Thang_4", "Tuan_1_T4", "Tuan_2_T4", "Tuan_3_T4", "Tuan_4_T4", "Thang_5", "Tuan_1_T5", "Tuan_2_T5", "Tuan_3_T5", "Tuan_4_T5", "Thang_6", "Tuan_1_T6", "Tuan_2_T6", "Tuan_3_T6", "Tuan_4_T6", "TS_Loai1", "TS_Loai2", "TS_Loai3", "TS_Loai4" };
            foreach (DataColumn col in dt.Columns)
            {
                if (!baseCols.Contains(col.ColumnName)) _cotPhatSinhCacheThongKe.Add(col.ColumnName);
            }
            dt.Columns.Add("GhiChu", typeof(string));
            dt.Columns.Add("SortPriority", typeof(int));

            string[] donViUuTien = Module_DonVi.LayDanhSachDonViUuTienArray();
            var dicDonViUuTien = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < donViUuTien.Length; i++) dicDonViUuTien.TryAdd(donViUuTien[i], i);

            var updates = new (string hoTen, string soHieu, string donVi, string ghiChu, int priority)[dt.Rows.Count];

            Parallel.For(0, dt.Rows.Count, i =>
            {
                DataRow row = dt.Rows[i];
                string shPlain = SafeDecrypt(row["SoHieu"]?.ToString());
                string dvPlain = SafeDecrypt(row["DonVi"]?.ToString());
                string htPlain = SafeDecrypt(row["HoVaTen"]?.ToString());

                int priority = dicDonViUuTien.TryGetValue(dvPlain, out int p) ? p : int.MaxValue;
                string ghiChu = dictGhiChu.TryGetValue(shPlain, out string gc) ? gc : "";

                updates[i] = (htPlain, shPlain, dvPlain, ghiChu, priority);
            });

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dt.Rows[i]["HoVaTen"] = updates[i].hoTen;
                dt.Rows[i]["SoHieu"] = updates[i].soHieu;
                dt.Rows[i]["DonVi"] = updates[i].donVi;
                dt.Rows[i]["GhiChu"] = updates[i].ghiChu;
                dt.Rows[i]["SortPriority"] = updates[i].priority;
            }

            dt.DefaultView.Sort = "SortPriority ASC, ID ASC";
            dtDanhSachGoc = dt; // ZERO COPY
            dtDanhSachGoc.PrimaryKey = new DataColumn[] { dtDanhSachGoc.Columns["ID"] };

            kryptonDataGridView1.Columns.Clear();
            foreach (DataColumn dc in dtDanhSachGoc.Columns)
            {
                if (dc.ColumnName != "SortPriority")
                    kryptonDataGridView1.Columns.Add(dc.ColumnName, dc.ColumnName);
            }
            ApDungDinhDangGridThongKe();
            _colIndexMap.Clear();
            for (int i = 0; i < kryptonDataGridView1.Columns.Count; i++)
            {
                string colName = kryptonDataGridView1.Columns[i].Name;
                if (dtDanhSachGoc.Columns.Contains(colName)) _colIndexMap[i] = dtDanhSachGoc.Columns.IndexOf(colName);
            }

            if (dtDanhSachGoc.DefaultView.Count == 0) kryptonDataGridView1.Rows.Clear();
            else kryptonDataGridView1.RowCount = dtDanhSachGoc.DefaultView.Count;

            kryptonDataGridView1.ResumeLayout();
            kryptonDataGridView1.Invalidate();
        }
        // TRẠM ĐIỀU PHỐI (ROUTER) - Tự động chọn hàm lọc dựa theo số lượng dữ liệu
        public void ApplyFilter()
        {
            // Cờ _isDataMaxMode đã được xác định ở hàm DieuPhoiLoadDuLieuAsync
            if (_isDataMaxMode)
            {
                ApplyFilter_DataMax();
            }
            else
            {
                ApplyFilter_Standard();
            }
        }
        // PHIÊN BẢN 1: STANDARD (Dành cho <= 3000 dòng, ĐÂY CHÍNH LÀ CODE GỐC)
        private void ApplyFilter_Standard()
        {
            if (dtDanhSachGoc == null || dtDanhSachGoc.DefaultView.Count == 0)
            {
                kryptonDataGridView1.RowCount = 0;
                toolStripStatusLabel1.Text = "Tổng cộng: 0 đồng chí";
                return;
            }

            string dvFilter = comboBox_TimKiemDonVi.Text?.Trim() ?? "";
            string tenFilter = textBox_TimKiemTheoTen.Text?.Trim() ?? "";
            string tinhTrangFilter = comboBox1_TinhTrang.Text?.Trim() ?? "";

            // 1. Khai báo biến tenFilterLower ở đây
            string tenFilterLower = tenFilter.ToLowerInvariant();

            if (_dangSetPlaceholder || tenFilterLower == PLACEHOLDER_TIMKIEM.ToLowerInvariant())
            {
                tenFilter = "";
                tenFilterLower = "";
            }

            kryptonDataGridView1.SuspendLayout();

            try
            {
                _filteredIndexes.Clear();

                bool hasTen = !string.IsNullOrWhiteSpace(tenFilterLower);
                bool hasDV = !string.IsNullOrWhiteSpace(dvFilter) && dvFilter != "Tất cả";
                bool hasTT = !string.IsNullOrWhiteSpace(tinhTrangFilter) && tinhTrangFilter != "Tất cả";

                // 2. CHUẨN KỸ SƯ: Kiểm tra an toàn xem DataTable có cột Cache hay không
                bool hasSearchColumn = dtDanhSachGoc.Columns.Contains("HoVaTen_Search");

                // ⭐ DUYỆT TRÊN DEFAULTVIEW ĐỂ GIỮ NGUYÊN THỨ TỰ SẮP XẾP
                DataView view = dtDanhSachGoc.DefaultView;
                int count = view.Count;

                for (int i = 0; i < count; i++)
                {
                    DataRowView rowView = view[i];

                    // Lọc Đơn vị
                    if (hasDV && rowView["DonVi"].ToString() != dvFilter) continue;

                    // Lọc Tình trạng
                    if (hasTT && !string.Equals(rowView["TinhTrang"].ToString().Trim(), tinhTrangFilter, StringComparison.OrdinalIgnoreCase)) continue;

                    // Lọc Họ Tên (Có cơ chế Fallback an toàn tuyệt đối)
                    if (hasTen)
                    {
                        if (hasSearchColumn)
                        {
                            // Tốc độ cao: Dùng thuật toán Contains trên cột chữ thường
                            if (!rowView["HoVaTen_Search"].ToString().Contains(tenFilterLower)) continue;
                        }
                        else
                        {
                            // Dự phòng an toàn: Nếu chưa khởi tạo cột Search, dùng IndexOf trên cột gốc
                            if (rowView["HoVaTen"].ToString().IndexOf(tenFilter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                        }
                    }

                    _filteredIndexes.Add(i);
                }

                kryptonDataGridView1.RowCount = _filteredIndexes.Count;
                toolStripStatusLabel1.Text = $"Tổng cộng: {_filteredIndexes.Count} đồng chí";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi ApplyFilter_Standard: " + ex.Message);
                kryptonDataGridView1.RowCount = 0;
            }
            finally
            {
                kryptonDataGridView1.ResumeLayout();
                kryptonDataGridView1.Invalidate();
            }
        }
        // PHIÊN BẢN 2: MAX DATA (Dành cho > 3000 dòng, Đẩy sang luồng nền, chống treo UI)
        private void KryptonDataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (_filteredIndexes == null || e.RowIndex < 0 || e.RowIndex >= _filteredIndexes.Count) return;

            try
            {
                int actualViewIndex = _filteredIndexes[e.RowIndex];

                if (_isDataMaxMode)
                {
                    string colName = kryptonDataGridView1.Columns[e.ColumnIndex].Name;
                    bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);

                    if (laTanBinh && _dataCacheTanBinh != null && _dataCacheTanBinh.Count > 0)
                    {
                        var data = _dataCacheTanBinh[actualViewIndex];
                        switch (colName)
                        {
                            case "HoVaTen": e.Value = data.HoTenE; break;
                            case "SoHieu": e.Value = data.SoHieuE; break;
                            case "DonVi": e.Value = data.DonViE; break;
                            case "TinhTrang": e.Value = data.TinhTrang; break;
                            case "GhiChu": e.Value = data.GhiChu; break;
                            case "TS_Loai1": e.Value = data.L1; break;
                            case "TS_Loai2": e.Value = data.L2; break;
                            case "TS_Loai3": e.Value = data.L3; break;
                            case "TS_Loai4": e.Value = data.L4; break;
                            default:
                                if (data.DuLieuThang.TryGetValue(colName, out string dThang)) e.Value = dThang;
                                else if (data.CotPhatSinh != null && data.CotPhatSinh.TryGetValue(colName, out string dynVal)) e.Value = dynVal;
                                else e.Value = string.Empty;
                                break;
                        }
                    }
                    else if (!laTanBinh && _dataCacheCBCS != null && _dataCacheCBCS.Count > 0)
                    {
                        var data = _dataCacheCBCS[actualViewIndex];
                        switch (colName)
                        {
                            case "HoVaTen": e.Value = data.HoTenE; break;
                            case "SoHieu": e.Value = data.SoHieuE; break;
                            case "DonVi": e.Value = data.DonViE; break;
                            case "TinhTrang": e.Value = data.TinhTrang; break;
                            case "GhiChu": e.Value = data.GhiChu; break;
                            case "KQ_ThiDua_Nam_Cu": e.Value = data.KQ_TD; break;
                            case "KQ_XepLoaiCB_Nam_Cu": e.Value = data.KQ_XL_CB; break;
                            case "KQ_XepLoaiDangVien_Nam_Cu": e.Value = data.KQ_XL_DV; break;
                            case "Thang_12_Nam_Cu": e.Value = data.T12; break;
                            case "Thang_1": e.Value = data.Thang[0]; break;
                            case "Thang_2": e.Value = data.Thang[1]; break;
                            case "Thang_3": e.Value = data.Thang[2]; break;
                            case "Thang_4": e.Value = data.Thang[3]; break;
                            case "Thang_5": e.Value = data.Thang[4]; break;
                            case "Thang_6": e.Value = data.Thang[5]; break;
                            case "Thang_7": e.Value = data.Thang[6]; break;
                            case "Thang_8": e.Value = data.Thang[7]; break;
                            case "Thang_9": e.Value = data.Thang[8]; break;
                            case "Thang_10": e.Value = data.Thang[9]; break;
                            case "Thang_11": e.Value = data.Thang[10]; break;
                            case "Sau_Thang_Dau_Nam": e.Value = data.SauThang; break;
                            case "TongKet_Nam": e.Value = data.TongKet; break;
                            case "TS_Loai1": e.Value = data.L1; break;
                            case "TS_Loai2": e.Value = data.L2; break;
                            case "TS_Loai3": e.Value = data.L3; break;
                            case "TS_Loai4": e.Value = data.L4; break;
                            default:
                                if (data.CotPhatSinh != null && data.CotPhatSinh.TryGetValue(colName, out string dynVal)) e.Value = dynVal;
                                else e.Value = string.Empty;
                                break;
                        }
                    }
                }
                else // Chế độ <= 3000 dòng (Standard)
                {
                    if (dtDanhSachGoc == null) return;
                    if (!_colIndexMap.TryGetValue(e.ColumnIndex, out int dtIndex)) return;

                    object value = dtDanhSachGoc.DefaultView[actualViewIndex][dtIndex];
                    e.Value = (value == DBNull.Value || value == null) ? string.Empty : value;
                }
            }
            catch { e.Value = string.Empty; }
        }
        private async void ApplyFilter_DataMax()
        {
            bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
            int totalCount = laTanBinh ? (_dataCacheTanBinh?.Count ?? 0) : (_dataCacheCBCS?.Count ?? 0);

            if (totalCount == 0)
            {
                kryptonDataGridView1.RowCount = 0;

                // 🟢 ĐỔI MÀU CHỮ: Màu xanh rêu đậm khi chế độ hiệu năng cao đang hoạt động
                toolStripStatusLabel1.ForeColor = System.Drawing.Color.FromArgb(40, 167, 69);
                toolStripStatusLabel1.Text = "Tổng cộng: 0 đồng chí [Chế độ hiệu năng cao đang hoạt động...]";
                return;
            }

            string dvFilter = comboBox_TimKiemDonVi.Text?.Trim() ?? "";
            string tenFilterLower = textBox_TimKiemTheoTen.Text?.Trim().ToLowerInvariant() ?? "";
            string tinhTrangFilter = comboBox1_TinhTrang.Text?.Trim() ?? "";

            if (_dangSetPlaceholder || tenFilterLower == PLACEHOLDER_TIMKIEM.ToLowerInvariant())
                tenFilterLower = "";

            bool hasTen = !string.IsNullOrWhiteSpace(tenFilterLower);
            bool hasDV = !string.IsNullOrWhiteSpace(dvFilter) && dvFilter != "Tất cả";
            bool hasTT = !string.IsNullOrWhiteSpace(tinhTrangFilter) && tinhTrangFilter != "Tất cả";

            kryptonDataGridView1.SuspendLayout();

            try
            {
                var newFilteredIndexes = await Task.Run(() =>
                {
                    var tempIndexes = new List<int>(totalCount);

                    if (laTanBinh)
                    {
                        for (int i = 0; i < totalCount; i++)
                        {
                            var item = _dataCacheTanBinh[i];
                            if (hasDV && item.DonViE != dvFilter) continue;
                            if (hasTT && !string.Equals(item.TinhTrang, tinhTrangFilter, StringComparison.OrdinalIgnoreCase)) continue;
                            if (hasTen && !item.HoTenSearch.Contains(tenFilterLower)) continue;
                            tempIndexes.Add(i);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < totalCount; i++)
                        {
                            var item = _dataCacheCBCS[i];
                            if (hasDV && item.DonViE != dvFilter) continue;
                            if (hasTT && !string.Equals(item.TinhTrang, tinhTrangFilter, StringComparison.OrdinalIgnoreCase)) continue;
                            if (hasTen && !item.HoTenSearch.Contains(tenFilterLower)) continue;
                            tempIndexes.Add(i);
                        }
                    }
                    return tempIndexes;
                });

                _filteredIndexes = newFilteredIndexes;
                kryptonDataGridView1.RowCount = _filteredIndexes.Count;

                // 🟢 ĐỔI MÀU CHỮ: Áp màu xanh rêu đậm khi hiển thị kết quả lọc hiệu năng cao
                toolStripStatusLabel1.ForeColor = System.Drawing.Color.FromArgb(25, 135, 84);
                toolStripStatusLabel1.Text = $"Tổng cộng: {_filteredIndexes.Count} đồng chí [Chế độ hiệu năng cao]";
            }
            catch
            {
                kryptonDataGridView1.RowCount = 0;
            }
            finally
            {
                kryptonDataGridView1.ResumeLayout();
                kryptonDataGridView1.Invalidate();
            }
        }

        private const int NGUONG_DU_LIEU = 3000;
        private bool _isDataMaxMode = false; // Cờ theo dõi trạng thái đang chạy động cơ nào
        private async Task DieuPhoiLoadDuLieuAsync(bool laTanBinh)
        {
            int tongSo = 0;
            string tableName = laTanBinh ? "ThiDuaThang_TanBinh" : "ThiDuaThang";

            // Đếm nhanh quân số (Rất nhẹ, mất < 1ms)
            try
            {
                using (var cn = new SqliteConnection($"Data Source={_csdl4Path}"))
                {
                    cn.Open();
                    using var cmd = new SqliteCommand($"SELECT COUNT(ID) FROM {tableName}", cn);
                    tongSo = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch { }

            _isDataMaxMode = tongSo > NGUONG_DU_LIEU;

            // Rẽ nhánh
            if (_isDataMaxMode)
            {
                if (laTanBinh) await LoadThongKe_TanBinh_DataMax();
                else await LoadThongKe_CBCS_DataMax();
            }
            else
            {
                if (laTanBinh) LoadThongKe_TanBinh();
                else LoadThongKe_CBCS();
            }
        }
        public async void ReloadData()
        {
            if ((DateTime.Now - _lastReload).TotalMilliseconds < 500) return;
            _lastReload = DateTime.Now;

            bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);

            CapNhatThongKe();

            // SỬA Ở ĐÂY: Dùng hàm điều phối thay cho gọi trực tiếp
            await DieuPhoiLoadDuLieuAsync(laTanBinh);

            LoadComboBoxDonVi();
            comboBox1_TinhTrang.SelectedIndex = 0;
        }
        public async Task LoadThongKe_CBCS_DataMax()
        {
            if (!File.Exists(_csdl4Path)) return;

            kryptonDataGridView1.SuspendLayout();
            kryptonDataGridView1.DataSource = null;
            kryptonDataGridView1.Columns.Clear();

            var ketQua = await Task.Run(() =>
            {
                var tempList = new List<RawCBCS>();
                var schemaDt = new DataTable();
                var cotPhatSinhList = new List<string>();

                using (var cn = new SqliteConnection($"Data Source={_csdl4Path}"))
                {
                    cn.Open();
                    using var cmd = new SqliteCommand("SELECT * FROM ThiDuaThang", cn);
                    using var rd = cmd.ExecuteReader();

                    for (int i = 0; i < rd.FieldCount; i++) schemaDt.Columns.Add(rd.GetName(i), typeof(string));
                    schemaDt.Columns.Add("GhiChu", typeof(string));

                    string[] baseCols = { "ID", "HoVaTen", "SoHieu", "DonVi", "TinhTrang", "KQ_ThiDua_Nam_Cu", "KQ_XepLoaiCB_Nam_Cu", "KQ_XepLoaiDangVien_Nam_Cu", "Thang_12_Nam_Cu", "Thang_1", "Thang_2", "Thang_3", "Thang_4", "Thang_5", "Sau_Thang_Dau_Nam", "Thang_6", "Thang_7", "Thang_8", "Thang_9", "Thang_10", "Thang_11", "TongKet_Nam", "TS_Loai1", "TS_Loai2", "TS_Loai3", "TS_Loai4" };
                    foreach (DataColumn col in schemaDt.Columns)
                    {
                        if (!baseCols.Contains(col.ColumnName) && col.ColumnName != "GhiChu")
                            cotPhatSinhList.Add(col.ColumnName);
                    }

                    while (rd.Read())
                    {
                        var item = new RawCBCS
                        {
                            ID = GetSafe(rd, "ID"),
                            HoTenE = GetSafe(rd, "HoVaTen"),
                            SoHieuE = GetSafe(rd, "SoHieu"),
                            DonViE = GetSafe(rd, "DonVi"),
                            TinhTrang = GetSafe(rd, "TinhTrang"),
                            KQ_TD = GetSafe(rd, "KQ_ThiDua_Nam_Cu"),
                            KQ_XL_CB = GetSafe(rd, "KQ_XepLoaiCB_Nam_Cu"),
                            KQ_XL_DV = GetSafe(rd, "KQ_XepLoaiDangVien_Nam_Cu"),
                            T12 = GetSafe(rd, "Thang_12_Nam_Cu"),
                            SauThang = GetSafe(rd, "Sau_Thang_Dau_Nam"),
                            TongKet = GetSafe(rd, "TongKet_Nam"),
                            L1 = GetSafe(rd, "TS_Loai1"),
                            L2 = GetSafe(rd, "TS_Loai2"),
                            L3 = GetSafe(rd, "TS_Loai3"),
                            L4 = GetSafe(rd, "TS_Loai4")
                        };

                        for (int i = 1; i <= 11; i++) item.Thang[i - 1] = GetSafe(rd, $"Thang_{i}");
                        foreach (var c in cotPhatSinhList) item.CotPhatSinh[c] = GetSafe(rd, c);

                        tempList.Add(item);
                    }
                }

                var dictGhiChu = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    using var cn2 = new SqliteConnection($"Data Source={_csdl2Path}");
                    cn2.Open();
                    using var cmdG = new SqliteCommand("SELECT SoHieu, GhiChu FROM DanhSach", cn2);
                    using var rdG = cmdG.ExecuteReader();
                    while (rdG.Read())
                    {
                        string sh = SafeDecrypt(rdG["SoHieu"]?.ToString());
                        if (!string.IsNullOrEmpty(sh)) dictGhiChu.TryAdd(sh, SafeDecrypt(rdG["GhiChu"]?.ToString()));
                    }
                }
                catch { }

                string[] donViUuTien = Module_DonVi.LayDanhSachDonViUuTienArray();
                var dicDonViUuTien = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < donViUuTien.Length; i++) dicDonViUuTien.TryAdd(donViUuTien[i], i);

                // Đổ thẳng vào Object song song siêu tốc
                Parallel.ForEach(tempList, item =>
                {
                    item.SoHieuE = SafeDecrypt(item.SoHieuE);
                    item.DonViE = SafeDecrypt(item.DonViE);
                    item.HoTenE = SafeDecrypt(item.HoTenE);
                    item.SortPriority = dicDonViUuTien.TryGetValue(item.DonViE, out int p) ? p : int.MaxValue;
                    item.GhiChu = dictGhiChu.TryGetValue(item.SoHieuE, out string gc) ? gc : "";
                    item.HoTenSearch = item.HoTenE.ToLowerInvariant();
                });

                tempList = tempList.OrderBy(x => x.SortPriority).ThenBy(x => x.ID).ToList();
                return (tempList, schemaDt, cotPhatSinhList);
            });

            // Dọn dẹp bộ nhớ đệm cũ trước khi nhận bộ nhớ mới
            if (_dataCacheCBCS != null)
            {
                _dataCacheCBCS.Clear();
            }

            _dataCacheCBCS = ketQua.tempList;

            if (dtDanhSachGoc != null)
            {
                dtDanhSachGoc.Dispose(); // Hủy DataTable schema cũ để tránh rò rỉ tài nguyên Unmanaged
            }
            dtDanhSachGoc = ketQua.schemaDt;
            _cotPhatSinhCacheThongKe = ketQua.cotPhatSinhList;

            kryptonDataGridView1.Columns.Clear();
            foreach (DataColumn dc in dtDanhSachGoc.Columns)
            {
                if (dc.ColumnName != "SortPriority" && dc.ColumnName != "HoVaTen_Search")
                    kryptonDataGridView1.Columns.Add(dc.ColumnName, dc.ColumnName);
            }
            ApDungDinhDangGridThongKe();

            _colIndexMap.Clear();
            for (int i = 0; i < kryptonDataGridView1.Columns.Count; i++)
            {
                string colName = kryptonDataGridView1.Columns[i].Name;
                if (dtDanhSachGoc.Columns.Contains(colName)) _colIndexMap[i] = dtDanhSachGoc.Columns.IndexOf(colName);
            }

            if (_dataCacheCBCS.Count == 0) kryptonDataGridView1.Rows.Clear();
            else kryptonDataGridView1.RowCount = _dataCacheCBCS.Count;

            kryptonDataGridView1.ResumeLayout();
            kryptonDataGridView1.Invalidate();
        }
        public async Task LoadThongKe_TanBinh_DataMax()
        {
            if (!File.Exists(_csdl4Path)) return;

            kryptonDataGridView1.SuspendLayout();
            kryptonDataGridView1.DataSource = null;
            kryptonDataGridView1.Columns.Clear();

            var ketQua = await Task.Run(() =>
            {
                var tempList = new List<RawTanBinh>();
                var schemaDt = new DataTable();
                var cotPhatSinhList = new List<string>();

                using (var cn = new SqliteConnection($"Data Source={_csdl4Path}"))
                {
                    cn.Open();
                    using var cmd = new SqliteCommand("SELECT * FROM ThiDuaThang_TanBinh", cn);
                    using var rd = cmd.ExecuteReader();

                    for (int i = 0; i < rd.FieldCount; i++) schemaDt.Columns.Add(rd.GetName(i), typeof(string));
                    schemaDt.Columns.Add("GhiChu", typeof(string));

                    string[] baseCols = { "ID", "HoVaTen", "SoHieu", "DonVi", "TinhTrang", "Tuan_1_T2", "Tuan_2_T2", "Tuan_3_T2", "Tuan_4_T2", "Thang_3", "Tuan_1_T3", "Tuan_2_T3", "Tuan_3_T3", "Tuan_4_T3", "Thang_4", "Tuan_1_T4", "Tuan_2_T4", "Tuan_3_T4", "Tuan_4_T4", "Thang_5", "Tuan_1_T5", "Tuan_2_T5", "Tuan_3_T5", "Tuan_4_T5", "Thang_6", "Tuan_1_T6", "Tuan_2_T6", "Tuan_3_T6", "Tuan_4_T6", "TS_Loai1", "TS_Loai2", "TS_Loai3", "TS_Loai4" };
                    foreach (DataColumn col in schemaDt.Columns)
                    {
                        if (!baseCols.Contains(col.ColumnName) && col.ColumnName != "GhiChu")
                            cotPhatSinhList.Add(col.ColumnName);
                    }

                    while (rd.Read())
                    {
                        var item = new RawTanBinh
                        {
                            ID = GetSafe(rd, "ID"),
                            HoTenE = GetSafe(rd, "HoVaTen"),
                            SoHieuE = GetSafe(rd, "SoHieu"),
                            DonViE = GetSafe(rd, "DonVi"),
                            TinhTrang = GetSafe(rd, "TinhTrang"),
                            L1 = GetSafe(rd, "TS_Loai1"),
                            L2 = GetSafe(rd, "TS_Loai2"),
                            L3 = GetSafe(rd, "TS_Loai3"),
                            L4 = GetSafe(rd, "TS_Loai4")
                        };

                        for (int m = 2; m <= 6; m++)
                        {
                            for (int t = 1; t <= 4; t++)
                            {
                                string col = $"Tuan_{t}_T{m}";
                                if (schemaDt.Columns.Contains(col)) item.DuLieuThang[col] = GetSafe(rd, col);
                            }
                            string colThang = $"Thang_{m}";
                            if (schemaDt.Columns.Contains(colThang)) item.DuLieuThang[colThang] = GetSafe(rd, colThang);
                        }

                        foreach (var c in cotPhatSinhList) item.CotPhatSinh[c] = GetSafe(rd, c);
                        tempList.Add(item);
                    }
                }

                var dictGhiChu = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    using var cn2 = new SqliteConnection($"Data Source={_csdl2Path}");
                    cn2.Open();
                    using var cmdG = new SqliteCommand("SELECT SoHieu, GhiChu FROM DanhSach", cn2);
                    using var rdG = cmdG.ExecuteReader();
                    while (rdG.Read())
                    {
                        string sh = SafeDecrypt(rdG["SoHieu"]?.ToString());
                        if (!string.IsNullOrEmpty(sh)) dictGhiChu.TryAdd(sh, SafeDecrypt(rdG["GhiChu"]?.ToString()));
                    }
                }
                catch { }

                string[] donViUuTien = Module_DonVi.LayDanhSachDonViUuTienArray();
                var dicDonViUuTien = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < donViUuTien.Length; i++) dicDonViUuTien.TryAdd(donViUuTien[i], i);

                Parallel.ForEach(tempList, item =>
                {
                    item.SoHieuE = SafeDecrypt(item.SoHieuE);
                    item.DonViE = SafeDecrypt(item.DonViE);
                    item.HoTenE = SafeDecrypt(item.HoTenE);
                    item.SortPriority = dicDonViUuTien.TryGetValue(item.DonViE, out int p) ? p : int.MaxValue;
                    item.GhiChu = dictGhiChu.TryGetValue(item.SoHieuE, out string gc) ? gc : "";
                    item.HoTenSearch = item.HoTenE.ToLowerInvariant();
                });

                tempList = tempList.OrderBy(x => x.SortPriority).ThenBy(x => x.ID).ToList();
                return (tempList, schemaDt, cotPhatSinhList);
            });
            // 1. Giải phóng danh sách cũ trong RAM trước khi nhận danh sách mới
            if (_dataCacheTanBinh != null)
            {
                _dataCacheTanBinh.Clear();
            }
            _dataCacheTanBinh = ketQua.tempList;

            // 2. Hủy DataTable cũ để tránh rò rỉ bộ nhớ
            if (dtDanhSachGoc != null)
            {
                dtDanhSachGoc.Dispose();
            }
            dtDanhSachGoc = ketQua.schemaDt;

            _cotPhatSinhCacheThongKe = ketQua.cotPhatSinhList;

            kryptonDataGridView1.Columns.Clear();
            foreach (DataColumn dc in dtDanhSachGoc.Columns)
            {
                if (dc.ColumnName != "SortPriority" && dc.ColumnName != "HoVaTen_Search")
                    kryptonDataGridView1.Columns.Add(dc.ColumnName, dc.ColumnName);
            }
            ApDungDinhDangGridThongKe();

            _colIndexMap.Clear();
            for (int i = 0; i < kryptonDataGridView1.Columns.Count; i++)
            {
                string colName = kryptonDataGridView1.Columns[i].Name;
                if (dtDanhSachGoc.Columns.Contains(colName)) _colIndexMap[i] = dtDanhSachGoc.Columns.IndexOf(colName);
            }

            if (_dataCacheTanBinh.Count == 0) kryptonDataGridView1.Rows.Clear();
            else kryptonDataGridView1.RowCount = _dataCacheTanBinh.Count;

            kryptonDataGridView1.ResumeLayout();
            kryptonDataGridView1.Invalidate();
        }
        // Dùng ConcurrentDictionary vì ta sẽ gọi nó bên trong Parallel.For (Đa luồng)
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _aesCache
            = new System.Collections.Concurrent.ConcurrentDictionary<string, string>(StringComparer.Ordinal);

        // Hàm bọc (Wrapper) thông minh
        private string CachedSafeDecrypt(string encryptedText)
        {
            // 1. Xử lý chuỗi rỗng cực nhanh, không cần lock hay encrypt
            if (string.IsNullOrWhiteSpace(encryptedText))
                return string.Empty;

            // 2. Lấy từ Cache nếu có, chưa có thì gọi hàm giải mã thật (SafeDecrypt) và lưu vào Cache
            return _aesCache.GetOrAdd(encryptedText, text => SafeDecrypt(text));
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            try
            {
                // 1. CHẶN KHI ĐANG XỬ LÝ NẶNG (Tích hợp biến khóa luồng nền của Form 15)
                if (_dangXuLyLuongNen == 1 && keyData != Keys.Escape) return true;

                // 2. GỌI MODULE PHÍM TẮT CHUNG (Cho các phím cơ bản)
                if (Module_PhimTat.XuLy(
                    keyData,
                    actionLamMoi: SafeAction(() => lamMoi_ToolStripMenuItem.PerformClick()), // F5
                    actionXuatExcel: SafeAction(() => xuatDuLieu_ToolStripMenuItem.PerformClick()) // Ctrl + E
                ))
                {
                    return true;
                }

                // 3. PHÍM TẮT FORM 15: QUY HOẠCH ĐẶC THÙ
                Keys key = keyData & Keys.KeyCode;
                Keys modifier = keyData & Keys.Modifiers;

                // --- NHÓM PHÍM F (NGHIỆP VỤ ĐẶC THÙ) ---
                if (modifier == Keys.None)
                {
                    switch (key)
                    {
                        case Keys.F6: return SafeExecute(() => dongBoDuLieu_ToolStripMenuItem.PerformClick());
                        case Keys.F9: return SafeExecute(() => tinhPhanLoaiThang_ToolStripMenuItem.PerformClick());
                    }
                }
                // --- NHÓM CTRL (TÁC VỤ TỆP TIN / LUÂN CHUYỂN DỮ LIỆU) ---
                else if (modifier == Keys.Control)
                {
                    switch (key)
                    {
                        case Keys.O: // Ctrl + O: Nhập dữ liệu
                            return SafeExecute(() => nhapDuLieuTuTepExcel_ExcelToolStripMenuItem.PerformClick());

                        case Keys.T: // Ctrl + T: Xem thống kê (Tập thể)
                                     // Gọi trực tiếp hàm sự kiện vì dựa theo code, tên nút này có thể không có đuôi _ToolStripMenuItem
                            return SafeExecute(() => xemThongKeThiDuaTapThe_Click(null, null));
                    }
                }
                // --- NHÓM CTRL + SHIFT (TÁC VỤ XÓA / NGUY HIỂM) ---
                else if (modifier == (Keys.Control | Keys.Shift))
                {
                    switch (key)
                    {
                        case Keys.M: // Ctrl + Shift + M: Xóa dữ liệu thi đua tháng
                            return SafeExecute(() => xoaDuLieuThiDuaThang_ToolStripMenuItem.PerformClick());

                        case Keys.C: // Ctrl + Shift + C: Xóa dữ liệu CBCS (Xóa 1 dòng)
                            return SafeExecute(() => xoaDuLieu_ToolStripMenuItem.PerformClick());

                        case Keys.Delete: // Ctrl + Shift + Delete: Xóa TOÀN BỘ DỮ LIỆU
                            return SafeExecute(() => xoaTatCaDuLieu_ToolStripMenuItem.PerformClick());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi phím tắt Form 15: " + ex.Message);
                return true; // Tránh crash app nếu có lỗi ngoài ý muốn
            }

            // Trả lại luồng cho Windows xử lý gõ văn bản
            return base.ProcessCmdKey(ref msg, keyData);
        }
        private DateTime _lastReload = DateTime.MinValue;
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Gợi ý thao tác";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            // Đã sửa đổi ở dòng này: dùng System.Windows.Forms.Control
            var tips = new Dictionary<System.Windows.Forms.Control, string>
        {
            // Tìm kiếm
            { textBox_TimKiemTheoTen, "Gõ tên cần tìm (không phân biệt chữ hoa / thường)" },
            { comboBox_TimKiemDonVi, "Chọn đơn vị để tìm kiếm tự động" },
            { comboBox1_TinhTrang, "Chọn tình trạng công tác để tìm kiếm tự động" },
            // Thao tác
            { kryptonButton_LamMoiCacOTimKiem, "Xóa toàn bộ các trường tìm kiếm (F5)" },
            { kryptonButton_CapNhat, "Đồng bộ cơ sở dữ liệu (F6)" },
            { kryptonButton_XuatData, "Xuất dữ liệu ra tệp (Ctrl + E)" }
        };

            foreach (var tip in tips)
            {
                if (tip.Key != null) // an toàn khi ẩn / refactor control
                    toolTip1.SetToolTip(tip.Key, tip.Value);
            }
        }
        private void InitPlaceholderTimKiem()
        {
            _dangSetPlaceholder = true;
            textBox_TimKiemTheoTen.Text = PLACEHOLDER_TIMKIEM;
            textBox_TimKiemTheoTen.ForeColor = System.Drawing.Color.Gray;
            _dangSetPlaceholder = false;
        }
        private void LoadComboBoxDonVi()
        {
            var dsDonVi = Module_DonVi.GetDanhSachDonVi();
            dsDonVi.Insert(0, "Tất cả"); // thêm tùy chọn đầu
            comboBox_TimKiemDonVi.DataSource = dsDonVi;
            comboBox_TimKiemDonVi.SelectedIndex = 0;
        }
        private void KryptonDataGridView1_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            var grid = sender as DataGridView;
            if (grid == null) return;

            // TỐI ƯU 1: Chỉ khởi tạo Cọ vẽ (Brush) và Căn lề (StringFormat) đúng MỘT LẦN DUY NHẤT.
            // Các lần cuộn chuột sau sẽ tái sử dụng lại, không tốn RAM và CPU để tạo mới.
            if (_rowHeaderBrush == null)
            {
                _rowHeaderBrush = new SolidBrush(grid.RowHeadersDefaultCellStyle.ForeColor);
                _rowHeaderFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,      // Căn giữa ngang
                    LineAlignment = StringAlignment.Center   // Căn giữa dọc
                };
            }

            // Lấy số thứ tự
            string stt = (e.RowIndex + 1).ToString();

            // Xác định khu vực vẽ (chỉ vẽ trong cột Header)
            Rectangle headerBounds = new Rectangle(
                e.RowBounds.Left,
                e.RowBounds.Top,
                grid.RowHeadersWidth,
                e.RowBounds.Height
            );

            // TỐI ƯU 2: Sử dụng Graphics.DrawString (GDI+) thay cho TextRenderer
            // Hàm này kết hợp với Brush đã lưu (cache) sẽ cho tốc độ vẽ nhanh như chớp.
            e.Graphics.DrawString(stt, grid.Font, _rowHeaderBrush, headerBounds, _rowHeaderFormat);
        }
        // HÀM 1: CẬP NHẬT TỔNG LOẠI (Đã tối ưu Transaction)
        public void RefreshRowFromDb(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || dtDanhSachGoc == null) return;

            try
            {
                // 1. Xác định phiên bản và bảng dữ liệu
                bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                string tableName = laTanBinh ? "ThiDuaThang_TanBinh" : "ThiDuaThang";

                // 2. Truy vấn dữ liệu mới nhất từ SQLite
                using var cn = new SqliteConnection($"Data Source={_csdl4Path}");
                cn.Open();
                using var cmd = new SqliteCommand($"SELECT * FROM {tableName} WHERE ID=@id", cn);
                cmd.Parameters.AddWithValue("@id", id);
                using var reader = cmd.ExecuteReader();

                if (!reader.Read()) return; // Không tìm thấy bản ghi trong DB thì thoát

                // 3. Cập nhật vào DataTable (RAM)
                DataRow target = dtDanhSachGoc.Rows.Find(id);

                if (target == null)
                {
                    target = dtDanhSachGoc.NewRow();
                    target["ID"] = id;
                    dtDanhSachGoc.Rows.Add(target);
                }

                // --- Bắt đầu đổ dữ liệu vào DataRow ---
                target.BeginEdit();

                // Thông tin cơ bản (Giải mã AES)
                target["HoVaTen"] = SafeDecrypt(GetSafe(reader, "HoVaTen"));
                target["SoHieu"] = SafeDecrypt(GetSafe(reader, "SoHieu"));
                target["DonVi"] = SafeDecrypt(GetSafe(reader, "DonVi"));
                target["TinhTrang"] = GetSafe(reader, "TinhTrang");

                // Cập nhật Cache Tìm kiếm
                if (dtDanhSachGoc.Columns.Contains("HoVaTen_Search"))
                    target["HoVaTen_Search"] = target["HoVaTen"].ToString().ToLowerInvariant();

                // Cập nhật mức độ ưu tiên sắp xếp
                if (dtDanhSachGoc.Columns.Contains("SortPriority"))
                {
                    string[] donViUuTien = Module_DonVi.LayDanhSachDonViUuTienArray();
                    int priority = Array.FindIndex(donViUuTien, x => x.Equals(target["DonVi"].ToString(), StringComparison.OrdinalIgnoreCase));
                    target["SortPriority"] = priority == -1 ? int.MaxValue : priority;
                }

                if (!laTanBinh)
                {
                    // Logic cho CBCS
                    target["KQ_ThiDua_Nam_Cu"] = GetSafe(reader, "KQ_ThiDua_Nam_Cu");
                    target["KQ_XepLoaiCB_Nam_Cu"] = GetSafe(reader, "KQ_XepLoaiCB_Nam_Cu");
                    target["KQ_XepLoaiDangVien_Nam_Cu"] = GetSafe(reader, "KQ_XepLoaiDangVien_Nam_Cu");
                    target["Thang_12_Nam_Cu"] = GetSafe(reader, "Thang_12_Nam_Cu");
                    for (int i = 1; i <= 11; i++) target[$"Thang_{i}"] = GetSafe(reader, $"Thang_{i}");
                    target["Sau_Thang_Dau_Nam"] = GetSafe(reader, "Sau_Thang_Dau_Nam");
                    target["TongKet_Nam"] = GetSafe(reader, "TongKet_Nam");
                }
                else
                {
                    // Logic cho TÂN BINH
                    for (int m = 2; m <= 6; m++)
                    {
                        for (int t = 1; t <= 4; t++)
                        {
                            string col = $"Tuan_{t}_T{m}";
                            if (dtDanhSachGoc.Columns.Contains(col)) target[col] = GetSafe(reader, col);
                        }
                        string colThang = $"Thang_{m}";
                        if (dtDanhSachGoc.Columns.Contains(colThang)) target[colThang] = GetSafe(reader, colThang);
                    }
                }

                // Tổng các loại
                target["TS_Loai1"] = GetSafe(reader, "TS_Loai1");
                target["TS_Loai2"] = GetSafe(reader, "TS_Loai2");
                target["TS_Loai3"] = GetSafe(reader, "TS_Loai3");
                target["TS_Loai4"] = GetSafe(reader, "TS_Loai4");

                // Cập nhật cột phát sinh
                if (_cotPhatSinhCacheThongKe != null)
                {
                    foreach (var c in _cotPhatSinhCacheThongKe)
                        if (dtDanhSachGoc.Columns.Contains(c)) target[c] = GetSafe(reader, c);
                }

                target.EndEdit();
                target.AcceptChanges();

                // ====================================================================
                // ⭐ BẢN VÁ: CẬP NHẬT TRỰC TIẾP LÊN LÕI OBJECT RAM (CHỐNG LỆCH DỮ LIỆU)
                // ====================================================================
                if (_isDataMaxMode)
                {
                    if (!laTanBinh && _dataCacheCBCS != null)
                    {
                        var ramObj = _dataCacheCBCS.FirstOrDefault(x => x.ID == id);
                        bool laNguoiMoi = false;

                        // NẾU LÀ NGƯỜI MỚI TOANH -> TẠO OBJECT MỚI NHÉT VÀO RAM
                        if (ramObj == null)
                        {
                            ramObj = new RawCBCS { ID = id };
                            laNguoiMoi = true;
                        }

                        ramObj.HoTenE = target["HoVaTen"].ToString();
                        ramObj.SoHieuE = target["SoHieu"].ToString();
                        ramObj.DonViE = target["DonVi"].ToString();
                        ramObj.TinhTrang = target["TinhTrang"].ToString();
                        ramObj.HoTenSearch = target["HoVaTen_Search"].ToString();
                        if (int.TryParse(target["SortPriority"].ToString(), out int p)) ramObj.SortPriority = p;

                        ramObj.KQ_TD = target["KQ_ThiDua_Nam_Cu"].ToString();
                        ramObj.KQ_XL_CB = target["KQ_XepLoaiCB_Nam_Cu"].ToString();
                        ramObj.KQ_XL_DV = target["KQ_XepLoaiDangVien_Nam_Cu"].ToString();
                        ramObj.T12 = target["Thang_12_Nam_Cu"].ToString();
                        for (int i = 1; i <= 11; i++) ramObj.Thang[i - 1] = target[$"Thang_{i}"].ToString();
                        ramObj.SauThang = target["Sau_Thang_Dau_Nam"].ToString();
                        ramObj.TongKet = target["TongKet_Nam"].ToString();
                        ramObj.L1 = target["TS_Loai1"].ToString();
                        ramObj.L2 = target["TS_Loai2"].ToString();
                        ramObj.L3 = target["TS_Loai3"].ToString();
                        ramObj.L4 = target["TS_Loai4"].ToString();

                        if (_cotPhatSinhCacheThongKe != null)
                        {
                            foreach (var c in _cotPhatSinhCacheThongKe) ramObj.CotPhatSinh[c] = target[c].ToString();
                        }

                        if (laNguoiMoi) _dataCacheCBCS.Add(ramObj);
                    }
                    else if (laTanBinh && _dataCacheTanBinh != null)
                    {
                        var ramObj = _dataCacheTanBinh.FirstOrDefault(x => x.ID == id);
                        bool laNguoiMoi = false;

                        if (ramObj == null)
                        {
                            ramObj = new RawTanBinh { ID = id };
                            laNguoiMoi = true;
                        }

                        ramObj.HoTenE = target["HoVaTen"].ToString();
                        ramObj.SoHieuE = target["SoHieu"].ToString();
                        ramObj.DonViE = target["DonVi"].ToString();
                        ramObj.TinhTrang = target["TinhTrang"].ToString();
                        ramObj.HoTenSearch = target["HoVaTen_Search"].ToString();
                        if (int.TryParse(target["SortPriority"].ToString(), out int p)) ramObj.SortPriority = p;

                        ramObj.L1 = target["TS_Loai1"].ToString();
                        ramObj.L2 = target["TS_Loai2"].ToString();
                        ramObj.L3 = target["TS_Loai3"].ToString();
                        ramObj.L4 = target["TS_Loai4"].ToString();

                        for (int m = 2; m <= 6; m++)
                        {
                            for (int t = 1; t <= 4; t++)
                            {
                                string col = $"Tuan_{t}_T{m}";
                                ramObj.DuLieuThang[col] = target[col].ToString();
                            }
                            string colThang = $"Thang_{m}";
                            ramObj.DuLieuThang[colThang] = target[colThang].ToString();
                        }

                        if (_cotPhatSinhCacheThongKe != null)
                        {
                            foreach (var c in _cotPhatSinhCacheThongKe) ramObj.CotPhatSinh[c] = target[c].ToString();
                        }

                        if (laNguoiMoi) _dataCacheTanBinh.Add(ramObj);
                    }
                }

                // 4. Đồng bộ hiển thị lên Grid (Chỉ gọi 1 lần duy nhất)
                ApplyFilter();

                // ⭐ TÌM VÀ VẼ LẠI DÒNG TRÊN GRID CỰC KỲ AN TOÀN
                for (int i = 0; i < _filteredIndexes.Count; i++)
                {
                    int actualIndex = _filteredIndexes[i];
                    string rowIdToCheck = "";

                    // RẼ NHÁNH LẤY ID ĐỂ CHỐNG LỖI OUT OF RANGE
                    if (_isDataMaxMode)
                    {
                        if (laTanBinh && actualIndex < _dataCacheTanBinh.Count)
                            rowIdToCheck = _dataCacheTanBinh[actualIndex].ID;
                        else if (!laTanBinh && actualIndex < _dataCacheCBCS.Count)
                            rowIdToCheck = _dataCacheCBCS[actualIndex].ID;
                    }
                    else
                    {
                        if (actualIndex < dtDanhSachGoc.DefaultView.Count)
                            rowIdToCheck = dtDanhSachGoc.DefaultView[actualIndex]["ID"].ToString();
                    }

                    // Vẽ lại đúng vị trí đang hiện thị
                    if (rowIdToCheck == id)
                    {
                        kryptonDataGridView1.InvalidateRow(i);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi RefreshRowFromDb: " + ex.Message);
            }
        }
        private void KryptonDataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            // Nuốt lỗi ComboBox value không hợp lệ
            if (e.Exception is ArgumentException)
            {
                e.ThrowException = false;
            }
        }
        private string SafeDecrypt(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            try
            {
                string result = BaoMatAES.GiaiMa(input);
                // ⭐ Fallback: Nếu AES trả về rỗng (do định dạng cũ), trả về chuỗi gốc
                return string.IsNullOrEmpty(result) ? input : result;
            }
            catch { return input; }
        }
        private static string GiaiMaAnToan(object? value)
        {
            if (value == null || value == DBNull.Value) return string.Empty;
            string s = value.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            try
            {
                string result = BaoMatAES.GiaiMa(s);
                // ⭐ Trả về chuỗi gốc nếu giải mã thất bại
                return string.IsNullOrEmpty(result) ? s : result;
            }
            catch { return s; }
        }
        private static string GetSafe(SqliteDataReader reader, string name)
        {
            try
            {
                return reader[name]?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }
        private List<int> _filteredIndexes = new List<int>();
        public void CapNhatThongKe()
        {
            try
            {
                bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                string tableDich = laTanBinh ? "ThiDuaThang_TanBinh" : "ThiDuaThang";

                // Hàm cục bộ hỗ trợ tạo Khóa so sánh chuẩn (Plaintext)
                // Cắt khoảng trắng và đưa về chữ thường để so sánh tuyệt đối chính xác
                // Hàm cục bộ hỗ trợ tạo Khóa so sánh: Ưu tiên Số Hiệu tuyệt đối
                string TaoKeySoSanh(string ht, string sh, string dv)
                {
                    string safeSh = sh?.Trim().ToLower() ?? "";

                    // Nếu có Số hiệu, CHỈ dùng Số hiệu làm Key so sánh (100% không bao giờ trùng/lệch)
                    if (!string.IsNullOrEmpty(safeSh))
                    {
                        return $"SH_{safeSh}";
                    }

                    // Fallback: Trong trường hợp Tân binh chưa được cấp Số hiệu, 
                    // mới phải bắt buộc dùng Họ Tên + Đơn vị, đồng thời xóa sạch khoảng trắng ẩn.
                    string safeHt = ht?.Replace(" ", "").ToLower() ?? "";
                    string safeDv = dv?.Replace(" ", "").ToLower() ?? "";
                    return $"NAME_{safeHt}|{safeDv}";
                }
                //// =======================================================
                //// BƯỚC 1: Lấy CSDL2 (Gốc) - GIẢI MÃ RA PLAINTEXT LÀM CHUẨN
                //// =======================================================
                //var hashGocCSDL2 = new HashSet<string>();
                //var listCsdl2 = new List<(string Key, string encHt, string encSh, string encDv)>();

                //using (var cn2 = new SqliteConnection($"Data Source={_csdl2Path}"))
                //{
                //    cn2.Open();
                //    using (var cmd = new SqliteCommand("SELECT HoVaTen, SoHieu, DonVi FROM DanhSach", cn2))
                //    using (var rd = cmd.ExecuteReader())
                //    {
                //        while (rd.Read())
                //        {
                //            // Lấy chuỗi mã hóa V2 từ CSDL2
                //            string encHt = rd["HoVaTen"]?.ToString() ?? "";
                //            string encSh = rd["SoHieu"]?.ToString() ?? "";
                //            string encDv = rd["DonVi"]?.ToString() ?? "";

                //            // GIẢI MÃ RA VĂN BẢN GỐC
                //            string plainHt = SafeDecrypt(encHt);
                //            string plainSh = SafeDecrypt(encSh);
                //            string plainDv = SafeDecrypt(encDv);

                //            // Bỏ qua dòng lỗi/trống
                //            if (string.IsNullOrWhiteSpace(plainHt) && string.IsNullOrWhiteSpace(plainSh)) continue;

                //            // Tạo khóa tổ hợp: "nguyễn văn a|123456|đội 1"
                //            string key = TaoKeySoSanh(plainHt, plainSh, plainDv);

                //            hashGocCSDL2.Add(key); // Dùng để đối chiếu cực nhanh
                //            listCsdl2.Add((key, encHt, encSh, encDv)); // Dùng để Insert nếu cần
                //        }
                //    }
                //}
                // =======================================================
                // BƯỚC 1: Lấy CSDL2 (Gốc) - GIẢI MÃ RA PLAINTEXT LÀM CHUẨN
                // =======================================================
                // SỬA DÒNG NÀY: Dùng Dictionary thay cho HashSet để lưu trữ thông tin mã hóa
                // =======================================================
                // BƯỚC 1: Lấy CSDL2 (Gốc) - GIẢI MÃ RA PLAINTEXT LÀM CHUẨN
                // =======================================================
                // SỬA DÒNG NÀY: Dùng Dictionary thay cho HashSet để lưu trữ thông tin mã hóa
                var dictGocCSDL2 = new Dictionary<string, (string encHt, string encSh, string encDv)>();
                var listCsdl2 = new List<(string Key, string encHt, string encSh, string encDv)>();

                using (var cn2 = new SqliteConnection($"Data Source={_csdl2Path}"))
                {
                    cn2.Open();
                    using (var cmd = new SqliteCommand("SELECT HoVaTen, SoHieu, DonVi FROM DanhSach", cn2))
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            string encHt = rd["HoVaTen"]?.ToString() ?? "";
                            string encSh = rd["SoHieu"]?.ToString() ?? "";
                            string encDv = rd["DonVi"]?.ToString() ?? "";

                            string plainHt = SafeDecrypt(encHt);
                            string plainSh = SafeDecrypt(encSh);
                            string plainDv = SafeDecrypt(encDv);

                            if (string.IsNullOrWhiteSpace(plainHt) && string.IsNullOrWhiteSpace(plainSh)) continue;

                            string key = TaoKeySoSanh(plainHt, plainSh, plainDv);

                            // Lưu trữ thông tin đã mã hóa để lát nữa đem đi Update
                            dictGocCSDL2[key] = (encHt, encSh, encDv);
                            listCsdl2.Add((key, encHt, encSh, encDv));
                        }
                    }
                }

                //// =======================================================
                //// BƯỚC 2: Lấy CSDL4 (Đích) - GIẢI MÃ VÀ ĐỐI CHIẾU
                //// =======================================================
                //var hashDichCSDL4 = new HashSet<string>();
                //var idChuyenCongTac = new List<int>();
                //var idDangCongTac = new List<int>();

                //using (var cn4 = new SqliteConnection($"Data Source={_csdl4Path}"))
                //{
                //    cn4.Open();
                //    // SỬA SELECT: Phải lấy cả Họ Tên và Đơn Vị lên để so sánh
                //    using (var cmd = new SqliteCommand($"SELECT ID, HoVaTen, SoHieu, DonVi, TinhTrang FROM {tableDich}", cn4))
                //    using (var rd = cmd.ExecuteReader())
                //    {
                //        while (rd.Read())
                //        {
                //            int id = Convert.ToInt32(rd["ID"]);
                //            string ttHienTai = rd["TinhTrang"]?.ToString()?.Trim() ?? "";

                //            string encHt = rd["HoVaTen"]?.ToString() ?? "";
                //            string encSh = rd["SoHieu"]?.ToString() ?? "";
                //            string encDv = rd["DonVi"]?.ToString() ?? "";

                //            // GIẢI MÃ RA VĂN BẢN GỐC
                //            string plainHt = SafeDecrypt(encHt);
                //            string plainSh = SafeDecrypt(encSh);
                //            string plainDv = SafeDecrypt(encDv);

                //            string key = TaoKeySoSanh(plainHt, plainSh, plainDv);
                //            hashDichCSDL4.Add(key); // Lưu lại để tý nữa lọc danh sách cần Insert

                //            // ⭐ KIỂM TRA SO SÁNH CẢ 3 TRƯỜNG DỰA TRÊN PLAINTEXT KEY
                //            if (hashGocCSDL2.Contains(key))
                //            {
                //                // Cả 3 trường (Họ Tên, Số Hiệu, Đơn Vị) đều khớp chuẩn xác với CSDL2
                //                if (ttHienTai != "Đang công tác")
                //                {
                //                    idDangCongTac.Add(id); // Cập nhật lại thành Đang công tác
                //                }
                //            }
                //            else
                //            {
                //                // Nếu sai lệch 1 trong 3 thông tin, HOẶC đã bị xóa khỏi CSDL2
                //                if (ttHienTai != "Chuyển công tác")
                //                {
                //                    idChuyenCongTac.Add(id); // Ép thành Chuyển công tác
                //                }
                //            }
                //        }
                //    }
                //}


                // =======================================================
                // BƯỚC 2: Lấy CSDL4 (Đích) - GIẢI MÃ VÀ ĐỐI CHIẾU
                // =======================================================
                var hashDichCSDL4 = new HashSet<string>();
                var idChuyenCongTac = new List<int>();
                var idDangCongTac = new List<int>();
                // THÊM DÒNG NÀY: Danh sách lưu các ID cần cập nhật lại Họ Tên / Đơn vị
                var idCanCapNhatThongTin = new List<(int id, string newEncHt, string newEncDv)>();

                using (var cn4 = new SqliteConnection($"Data Source={_csdl4Path}"))
                {
                    cn4.Open();
                    using (var cmd = new SqliteCommand($"SELECT ID, HoVaTen, SoHieu, DonVi, TinhTrang FROM {tableDich}", cn4))
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            int id = Convert.ToInt32(rd["ID"]);
                            string ttHienTai = rd["TinhTrang"]?.ToString()?.Trim() ?? "";

                            string encHt = rd["HoVaTen"]?.ToString() ?? "";
                            string encSh = rd["SoHieu"]?.ToString() ?? "";
                            string encDv = rd["DonVi"]?.ToString() ?? "";

                            string plainHt = SafeDecrypt(encHt);
                            string plainSh = SafeDecrypt(encSh);
                            string plainDv = SafeDecrypt(encDv);

                            string key = TaoKeySoSanh(plainHt, plainSh, plainDv);
                            hashDichCSDL4.Add(key);

                            // SỬA LẠI ĐOẠN IF NÀY
                            if (dictGocCSDL2.TryGetValue(key, out var thongTinGoc))
                            {
                                // 1. Cập nhật lại trạng thái công tác
                                if (ttHienTai != "Đang công tác")
                                {
                                    idDangCongTac.Add(id);
                                }

                                // 2. SO SÁNH: Nếu Họ Tên hoặc Đơn Vị bị lệch so với CSDL2 (so sánh mã hóa cho nhanh)
                                if (encHt != thongTinGoc.encHt || encDv != thongTinGoc.encDv)
                                {
                                    idCanCapNhatThongTin.Add((id, thongTinGoc.encHt, thongTinGoc.encDv));
                                }
                            }
                            else
                            {
                                if (ttHienTai != "Chuyển công tác")
                                {
                                    idChuyenCongTac.Add(id);
                                }
                            }
                        }
                    }
                }

                // =======================================================
                // BƯỚC 3: Xử lý danh sách cần Thêm Mới
                // (Có trong CSDL2 nhưng chưa hề tồn tại trong CSDL4)
                // =======================================================
                var danhSachCanThem = new List<(string encHt, string encSh, string encDv)>();
                foreach (var item in listCsdl2)
                {
                    if (!hashDichCSDL4.Contains(item.Key))
                    {
                        danhSachCanThem.Add((item.encHt, item.encSh, item.encDv));
                    }
                }

                // =======================================================
                // BƯỚC 4: Thực thi UPDATE và INSERT vào CSDL4 (Transaction)
                // =======================================================
                using (var cn4 = new SqliteConnection($"Data Source={_csdl4Path}"))
                {
                    cn4.Open();
                    using (var tran = cn4.BeginTransaction())
                    {
                        try
                        {
                            // A. THÊM MỚI
                            if (danhSachCanThem.Count > 0)
                            {
                                using (var cmdIns = new SqliteCommand($@"
                                INSERT INTO {tableDich} (HoVaTen, SoHieu, DonVi, TinhTrang, TS_Loai1, TS_Loai2, TS_Loai3, TS_Loai4)
                                VALUES (@ht, @sh, @dv, 'Đang công tác', '0', '0', '0', '0')", cn4, tran))
                                {
                                    cmdIns.Parameters.Add("@ht", SqliteType.Text);
                                    cmdIns.Parameters.Add("@sh", SqliteType.Text);
                                    cmdIns.Parameters.Add("@dv", SqliteType.Text);

                                    foreach (var item in danhSachCanThem)
                                    {
                                        cmdIns.Parameters["@ht"].Value = item.encHt;
                                        cmdIns.Parameters["@sh"].Value = item.encSh;
                                        cmdIns.Parameters["@dv"].Value = item.encDv;
                                        cmdIns.ExecuteNonQuery();
                                    }
                                }
                            }

                            // B. CẬP NHẬT: CHUYỂN CÔNG TÁC (Lệch 1 trong 3 trường hoặc bị xóa)
                            if (idChuyenCongTac.Count > 0)
                            {
                                using (var cmdUpd = new SqliteCommand($"UPDATE {tableDich} SET TinhTrang = 'Chuyển công tác' WHERE ID = @id", cn4, tran))
                                {
                                    cmdUpd.Parameters.Add("@id", SqliteType.Integer);
                                    foreach (int id in idChuyenCongTac)
                                    {
                                        cmdUpd.Parameters["@id"].Value = id;
                                        cmdUpd.ExecuteNonQuery();
                                    }
                                }
                            }

                            // C. CẬP NHẬT: ĐANG CÔNG TÁC (Dành cho ai quay lại danh sách gốc hoặc vừa sửa thông tin cho khớp lại)
                            if (idDangCongTac.Count > 0)
                            {
                                using (var cmdUpd = new SqliteCommand($"UPDATE {tableDich} SET TinhTrang = 'Đang công tác' WHERE ID = @id", cn4, tran))
                                {
                                    cmdUpd.Parameters.Add("@id", SqliteType.Integer);
                                    foreach (int id in idDangCongTac)
                                    {
                                        cmdUpd.Parameters["@id"].Value = id;
                                        cmdUpd.ExecuteNonQuery();
                                    }
                                }
                            }
                            // THÊM KHỐI LỆNH NÀY NGAY TRƯỚC tran.Commit();
                            // D. CẬP NHẬT: ĐỒNG BỘ THÔNG TIN (Họ tên, Đơn vị bị thay đổi)
                            if (idCanCapNhatThongTin.Count > 0)
                            {
                                using (var cmdUpdInfo = new SqliteCommand($"UPDATE {tableDich} SET HoVaTen = @ht, DonVi = @dv WHERE ID = @id", cn4, tran))
                                {
                                    cmdUpdInfo.Parameters.Add("@ht", SqliteType.Text);
                                    cmdUpdInfo.Parameters.Add("@dv", SqliteType.Text);
                                    cmdUpdInfo.Parameters.Add("@id", SqliteType.Integer);

                                    foreach (var item in idCanCapNhatThongTin)
                                    {
                                        cmdUpdInfo.Parameters["@ht"].Value = item.newEncHt;
                                        cmdUpdInfo.Parameters["@dv"].Value = item.newEncDv;
                                        cmdUpdInfo.Parameters["@id"].Value = item.id;
                                        cmdUpdInfo.ExecuteNonQuery();
                                    }
                                }
                            }



                            tran.Commit();
                        }
                        catch { tran.Rollback(); throw; }
                    }

                    // =======================================================
                    // BƯỚC 5: Tính lại Tổng loại (Giữ nguyên)
                    // SỬA LẠI ĐOẠN CỐI CỦA BƯỚC 5 TRONG HÀM CapNhatThongKe():
                    using (var cmdLayID = new SqliteCommand($"SELECT ID FROM {tableDich}", cn4))
                    using (var reader = cmdLayID.ExecuteReader())
                    {
                        var ids = new List<int>();
                        while (reader.Read()) ids.Add(Convert.ToInt32(reader["ID"]));
                        reader.Close();

                        // KHỞI TẠO BẢN VÁ TỐC ĐỘ CAO: REUSE COMMAND CHO 10.000 DÒNG
                        using (var tranTong = cn4.BeginTransaction())
                        {
                            string selectCols = laTanBinh
                                ? "Thang_2,Thang_3,Thang_4,Thang_5,Thang_6"
                                : "Thang_12_Nam_Cu,Thang_1,Thang_2,Thang_3,Thang_4,Thang_5,Thang_6,Thang_7,Thang_8,Thang_9,Thang_10,Thang_11";

                            // Tạo sẵn 2 Command dùng chung xuyên suốt Transaction
                            using var cmdSelect = new SqliteCommand($"SELECT {selectCols} FROM {tableDich} WHERE ID=@ID", cn4, tranTong);
                            cmdSelect.Parameters.Add("@ID", SqliteType.Integer);

                            using var cmdUpdate = new SqliteCommand($@"
            UPDATE {tableDich}
            SET TS_Loai1=@L1, TS_Loai2=@L2, TS_Loai3=@L3, TS_Loai4=@L4
            WHERE ID=@ID", cn4, tranTong);
                            cmdUpdate.Parameters.Add("@L1", SqliteType.Text);
                            cmdUpdate.Parameters.Add("@L2", SqliteType.Text);
                            cmdUpdate.Parameters.Add("@L3", SqliteType.Text);
                            cmdUpdate.Parameters.Add("@L4", SqliteType.Text);
                            cmdUpdate.Parameters.Add("@ID", SqliteType.Integer);

                            foreach (int id in ids)
                            {
                                int c1 = 0, c2 = 0, c3 = 0, c4 = 0;

                                // Gán ID để truy vấn nhanh
                                cmdSelect.Parameters["@ID"].Value = id;
                                using (var rdData = cmdSelect.ExecuteReader())
                                {
                                    if (rdData.Read())
                                    {
                                        for (int i = 0; i < rdData.FieldCount; i++)
                                        {
                                            string m = rdData[i]?.ToString()?.Trim() ?? "";
                                            foreach (char ch in m)
                                            {
                                                if (ch == '1') c1++;
                                                else if (ch == '2') c2++;
                                                else if (ch == '3') c3++;
                                                else if (ch == '4') c4++;
                                            }
                                        }
                                    }
                                }

                                // Gán giá trị để Update ngược lại ngay lập tức
                                cmdUpdate.Parameters["@L1"].Value = c1 == 0 ? DBNull.Value : (object)c1.ToString();
                                cmdUpdate.Parameters["@L2"].Value = c2 == 0 ? DBNull.Value : (object)c2.ToString();
                                cmdUpdate.Parameters["@L3"].Value = c3 == 0 ? DBNull.Value : (object)c3.ToString();
                                cmdUpdate.Parameters["@L4"].Value = c4 == 0 ? DBNull.Value : (object)c4.ToString();
                                cmdUpdate.Parameters["@ID"].Value = id;
                                cmdUpdate.Parameters["@L1"].Value = c1 == 0 ? "" : c1.ToString();
                                cmdUpdate.Parameters["@L2"].Value = c2 == 0 ? "" : c2.ToString();
                                cmdUpdate.Parameters["@L3"].Value = c3 == 0 ? "" : c3.ToString();
                                cmdUpdate.Parameters["@L4"].Value = c4 == 0 ? "" : c4.ToString();

                                cmdUpdate.ExecuteNonQuery();
                            }
                            tranTong.Commit();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi đồng bộ: " + ex.Message);
            }
        }
        public void CapNhatTongLoai_Cho(int id, SqliteConnection cn, SqliteTransaction tran = null)
        {
            int c1 = 0, c2 = 0, c3 = 0, c4 = 0;

            // Check trực tiếp từ RAM thay vì query database gây chậm
            bool laTanBinh = Module_TaiKhoan
                .LayPhienBanPhanMem()
                .Contains("tân binh", StringComparison.OrdinalIgnoreCase);

            string table = laTanBinh ? "ThiDuaThang_TanBinh" : "ThiDuaThang";

            string selectCols = laTanBinh
                ? "Thang_2,Thang_3,Thang_4,Thang_5,Thang_6"
                : "Thang_12_Nam_Cu,Thang_1,Thang_2,Thang_3,Thang_4,Thang_5,Thang_6,Thang_7,Thang_8,Thang_9,Thang_10,Thang_11";

            // Truyền tran vào Command
            using (var cmd = new SqliteCommand($"SELECT {selectCols} FROM {table} WHERE ID=@ID", cn, tran))
            {
                cmd.Parameters.AddWithValue("@ID", id);

                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return;

                    for (int i = 0; i < rd.FieldCount; i++)
                    {
                        string m = rd[i]?.ToString()?.Trim() ?? "";

                        foreach (char ch in m)
                        {
                            if (ch == '1') c1++;
                            else if (ch == '2') c2++;
                            else if (ch == '3') c3++;
                            else if (ch == '4') c4++;
                        }
                    }
                }
            }

            object L1 = c1 == 0 ? "" : c1;
            object L2 = c2 == 0 ? "" : c2;
            object L3 = c3 == 0 ? "" : c3;
            object L4 = c4 == 0 ? "" : c4;

            // Truyền tran vào Command update
            using (var cmd = new SqliteCommand($@"
                UPDATE {table}
                SET TS_Loai1=@L1,
                    TS_Loai2=@L2,
                    TS_Loai3=@L3,
                    TS_Loai4=@L4
                WHERE ID=@ID", cn, tran))
            {
                cmd.Parameters.AddWithValue("@L1", L1);
                cmd.Parameters.AddWithValue("@L2", L2);
                cmd.Parameters.AddWithValue("@L3", L3);
                cmd.Parameters.AddWithValue("@L4", L4);
                cmd.Parameters.AddWithValue("@ID", id);

                cmd.ExecuteNonQuery();
            }
        }
        public void ApDungDinhDangGridThongKe()
        {
            if (kryptonDataGridView1 == null || kryptonDataGridView1.Columns.Count == 0)
                return;

            var grid = kryptonDataGridView1;

            typeof(DataGridView)
                .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(grid, true, null);

            grid.SuspendLayout();

            try
            {
                string phienBan = Module_TaiKhoan.LayPhienBanPhanMem();
                bool laTanBinh = !string.IsNullOrWhiteSpace(phienBan) && phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                int namHeThong = Module_NamHeThong.LayNamHeThong();
                int namCu = namHeThong - 1;

                var headerMap = new Dictionary<string, string>
                {
                    ["HoVaTen"] = "Họ và tên",
                    ["SoHieu"] = "Số hiệu",
                    ["DonVi"] = "Đơn vị",
                    ["TinhTrang"] = "Tình trạng",
                    ["GhiChu"] = "Ghi chú" // ⭐ Header cho cột mới
                };

                if (laTanBinh)
                {
                    headerMap["Tuan_1_T2"] = "Tuần 1\nTháng 2"; headerMap["Tuan_2_T2"] = "Tuần 2\nTháng 2"; headerMap["Tuan_3_T2"] = "Tuần 3\nTháng 2"; headerMap["Tuan_4_T2"] = "Tuần 4\nTháng 2"; headerMap["Thang_3"] = "Kết quả\nTháng 3";
                    headerMap["Tuan_1_T3"] = "Tuần 1\nTháng 3"; headerMap["Tuan_2_T3"] = "Tuần 2\nTháng 3"; headerMap["Tuan_3_T3"] = "Tuần 3\nTháng 3"; headerMap["Tuan_4_T3"] = "Tuần 4\nTháng 3"; headerMap["Thang_4"] = "Kết quả\nTháng 4";
                    headerMap["Tuan_1_T4"] = "Tuần 1\nTháng 4"; headerMap["Tuan_2_T4"] = "Tuần 2\nTháng 4"; headerMap["Tuan_3_T4"] = "Tuần 3\nTháng 4"; headerMap["Tuan_4_T4"] = "Tuần 4\nTháng 4"; headerMap["Thang_5"] = "Kết quả\nTháng 5";
                    headerMap["Tuan_1_T5"] = "Tuần 1\nTháng 5"; headerMap["Tuan_2_T5"] = "Tuần 2\nTháng 5"; headerMap["Tuan_3_T5"] = "Tuần 3\nTháng 5"; headerMap["Tuan_4_T5"] = "Tuần 4\nTháng 5"; headerMap["Thang_6"] = "Kết quả\nTháng 6";
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

                // ẨN CÁC CỘT THỪA
                if (grid.Columns.Contains("ID")) grid.Columns["ID"].Visible = false;

                if (laTanBinh)
                {
                    if (grid.Columns.Contains("Thang_2")) grid.Columns["Thang_2"].Visible = false;
                    if (grid.Columns.Contains("Tuan_1_T6")) grid.Columns["Tuan_1_T6"].Visible = false;
                    if (grid.Columns.Contains("Tuan_2_T6")) grid.Columns["Tuan_2_T6"].Visible = false;
                    if (grid.Columns.Contains("Tuan_3_T6")) grid.Columns["Tuan_3_T6"].Visible = false;
                    if (grid.Columns.Contains("Tuan_4_T6")) grid.Columns["Tuan_4_T6"].Visible = false;
                }

                // ==========================================================
                // ⭐ ÁP DỤNG GIAO DIỆN HIỆN ĐẠI (FLUENT DESIGN)
                // ==========================================================
                grid.BackgroundColor = Color.White;
                grid.BorderStyle = BorderStyle.None;
                grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal; // Lưới kẻ ngang
                grid.GridColor = Color.FromArgb(235, 235, 235); // Kẻ ngang màu xám nhạt
                grid.EnableHeadersVisualStyles = false;

                // --- 1. ĐỊNH DẠNG HEADER (TIÊU ĐỀ) ---
                grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
                grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                grid.ColumnHeadersHeight = 65; // Đủ cao để chứa tiêu đề 2 dòng
                grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    BackColor = Color.FromArgb(240, 244, 248),
                    ForeColor = Color.FromArgb(40, 40, 40),
                    SelectionBackColor = Color.FromArgb(240, 244, 248),
                    WrapMode = DataGridViewTriState.True,
                    Padding = new Padding(3)
                };

                // --- 2. ĐỊNH DẠNG ROW (DÒNG DỮ LIỆU) ---
                grid.RowTemplate.Height = 32; // Dòng rộng rãi, dễ đọc
                grid.DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                    ForeColor = Color.FromArgb(45, 45, 45),
                    SelectionBackColor = Color.FromArgb(232, 244, 253),
                    SelectionForeColor = Color.FromArgb(0, 102, 204),
                    Padding = new Padding(2, 0, 2, 0)
                };
                grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 252, 252);
                // ==========================================================
                // ⭐ BỔ SUNG 2 DÒNG CODE VÀO ĐÂY ⭐
                grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                grid.MultiSelect = false;
                // --- 3. ĐỊNH DẠNG ĐỘ RỘNG VÀ CĂN LỀ TỪNG CỘT ---
                if (grid.Columns.Contains("HoVaTen"))
                {
                    var col = grid.Columns["HoVaTen"];
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    col.MinimumWidth = 220;
                    col.FillWeight = 40;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    col.DefaultCellStyle.Padding = new Padding(5, 0, 0, 0); // Thụt lề chữ
                }
                if (grid.Columns.Contains("TinhTrang"))
                {
                    var col = grid.Columns["TinhTrang"];
                    col.Width = 140;
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                if (grid.Columns.Contains("GhiChu"))
                {
                    var col = grid.Columns["GhiChu"];
                    col.Width = 220;
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    col.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                    col.DefaultCellStyle.ForeColor = Color.DarkSlateGray; // Làm mờ nhẹ cột ghi chú
                    col.DefaultCellStyle.Padding = new Padding(5, 0, 0, 0);
                }

                foreach (DataGridViewColumn col in grid.Columns)
                {
                    col.SortMode = DataGridViewColumnSortMode.NotSortable; // Tắt sort Header
                    if (col.Name == "HoVaTen" || col.Name == "ID" || col.Name == "TinhTrang" || col.Name == "GhiChu") continue;

                    col.Width = 90;
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                // --- 4. TÔ MÀU NHẤN MẠNH CỘT KẾT QUẢ/ĐÁNH GIÁ ---
                if (laTanBinh)
                {
                    string[] nhomToMauXanhDuong = {
                        "Tuan_1_T2", "Tuan_2_T2", "Tuan_3_T2", "Tuan_4_T2", "Thang_3",
                        "Tuan_1_T4", "Tuan_2_T4", "Tuan_3_T4", "Tuan_4_T4", "Thang_5"
                    };
                    Color mauXanhDuongNhat = Color.FromArgb(235, 245, 255); // Làm màu dịu lại theo Fluent Design

                    foreach (var name in nhomToMauXanhDuong)
                        if (grid.Columns.Contains(name)) grid.Columns[name].DefaultCellStyle.BackColor = mauXanhDuongNhat;

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
                Color mauXanhLaNhat = Color.FromArgb(240, 252, 240); // Làm xanh dịu lại
                foreach (var name in nhomToMauXanhLa)
                {
                    if (grid.Columns.Contains(name))
                    {
                        grid.Columns[name].DefaultCellStyle.BackColor = mauXanhLaNhat;
                        grid.Columns[name].DefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                    }
                }

                // --- 5. CƯỠNG CHẾ VỊ TRÍ CỘT ---
                int viTri = 0;

                void XepCot(string colName)
                {
                    if (grid.Columns.Contains(colName)) grid.Columns[colName].DisplayIndex = viTri++;
                }

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
                    XepCot("KQ_ThiDua_Nam_Cu");
                    XepCot("KQ_XepLoaiCB_Nam_Cu");
                    XepCot("KQ_XepLoaiDangVien_Nam_Cu");

                    XepCot("Thang_12_Nam_Cu");
                    for (int i = 1; i <= 11; i++) XepCot($"Thang_{i}");

                    XepCot("Sau_Thang_Dau_Nam");
                    XepCot("TongKet_Nam");
                }

                XepCot("TS_Loai1");
                XepCot("TS_Loai2");
                XepCot("TS_Loai3");
                XepCot("TS_Loai4");

                if (_cotPhatSinhCacheThongKe != null)
                {
                    foreach (var cp in _cotPhatSinhCacheThongKe)
                    {
                        if (cp != "GhiChu") XepCot(cp); // Không xếp GhiChu ở khu vực phát sinh
                    }
                }

                // ⭐ CHỐT VỊ TRÍ CUỐI CÙNG TẠI ĐÂY
                XepCot("GhiChu");
            }
            catch (Exception ex) { Debug.WriteLine("Lỗi định dạng: " + ex.Message); }
            finally { grid.ResumeLayout(); grid.Refresh(); }
        }
        private async void kryptonButton_CapNhat_Click(object sender, EventArgs e)
        {
            string textBanDau = kryptonButton_CapNhat.Values.Text;
            Image anhBanDau = kryptonButton_CapNhat.Values.Image;

            // ⭐ 1. KHỞI TẠO FORM LOADING VÀ KHÓA MÀN HÌNH CHÍNH
            Form_Loading frmLoad = new Form_Loading("Đang tải dữ liệu từ CSDL, vui lòng đợi...");

            try
            {
                kryptonButton_CapNhat.Enabled = false;
                kryptonButton_CapNhat.Values.Text = "Đang tải...";
                kryptonButton_CapNhat.Values.Image = null;

                // ⭐ Hiện Form Loading lên 
                this.Enabled = false;
                frmLoad.Show(this);

                // Nhịp nghỉ UX để UI vẽ kịp Form Loading trước khi vào vòng lặp nặng
                await Task.Delay(100);

                // =========================================================================
                // BẮT ĐẦU: XỬ LÝ NGẦM
                // =========================================================================
                bool laTanBinh = false;

                await Task.Run(() =>
                {
                    // 1. Đồng bộ ngầm SQLite (Tốc độ cao)
                    CapNhatThongKe();

                    // Xác định phiên bản
                    laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                });

                // =========================================================================
                // SAU KHI ĐỒNG BỘ DB XONG, MỚI KÉO DỮ LIỆU LÊN UI THREAD
                // =========================================================================
                if (laTanBinh)
                    LoadThongKe_TanBinh();
                else
                    LoadThongKe_CBCS();

                // Cập nhật lại ComboBox phòng trường hợp DB có đơn vị công tác mới tinh
                LoadComboBoxDonVi();

                // Ép lưới chạy thuật toán lọc
                ApplyFilter();

                try { Module_ThongBao.ThanhCong("Cập nhật Thống kê thành công!"); } catch { }
            }
            catch (Exception ex)
            {
                try { Module_ThongBao.Loi("Lỗi cập nhật: " + ex.Message); } catch { }
            }
            finally
            {
                // ⭐ 3. DỌN DẸP CHIẾN TRƯỜNG
                frmLoad.Close();

                // Mở khóa Form trước
                this.Enabled = true;

                // 🔥 ĐIỂM MẤU CHỐT Ở ĐÂY:
                // Bắt buộc gọi Refresh() sau khi form đã được Enabled = true 
                // để ép lưới VirtualMode vẽ lại toàn bộ dữ liệu đang có trên RAM!
                kryptonDataGridView1.Refresh();

                this.Focus();

                kryptonButton_CapNhat.Values.Text = textBanDau;
                kryptonButton_CapNhat.Values.Image = anhBanDau;
                kryptonButton_CapNhat.Enabled = true;
            }
        }
        private void comboBox1_TinhTrang_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFilter();

        }
        private void textBox_TimKiemTheoTen_TextChanged(object sender, EventArgs e)
        {
            // Bỏ qua nếu đang tự động set chữ "Nhập tìm kiếm"
            if (_dangSetPlaceholder) return;
            // Nếu Timer chưa được khởi tạo (phòng hờ lỗi thứ tự khởi tạo), thì bỏ qua
            if (_timKiemTimer == null) return;
            // Reset lại đồng hồ đếm ngược mỗi khi có ký tự mới được gõ
            _timKiemTimer.Stop();
            _timKiemTimer.Start();
        }
        private void comboBox_TimKiemDonVi_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }
        private void KryptonDataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            var dgv = (DataGridView)sender;
            var hit = dgv.HitTest(e.X, e.Y);

            if (hit.RowIndex >= 0)
            {
                dgv.ClearSelection();
                dgv.Rows[hit.RowIndex].Selected = true;
                dgv.CurrentCell = dgv.Rows[hit.RowIndex].Cells[0];
            }
        }
        public async void CapNhatForm15()
        {
            bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
            await DieuPhoiLoadDuLieuAsync(laTanBinh);
        }
        public void ChildRowUpdated(string id)
        {
            if (string.IsNullOrEmpty(id)) return;

            // 1. Cập nhật lại lõi dữ liệu dưới DB và RAM Cache trước
            RefreshRowFromDb(id);

            try
            {
                // 2. Tìm kiếm chỉ số dòng đang hiển thị dựa trên bộ chỉ mục lọc thay vì quét Grid Rows
                bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                int rowIndexOnGrid = -1;

                for (int i = 0; i < _filteredIndexes.Count; i++)
                {
                    int actualIndex = _filteredIndexes[i];
                    string currentId = "";

                    if (_isDataMaxMode)
                    {
                        currentId = laTanBinh ? _dataCacheTanBinh[actualIndex].ID : _dataCacheCBCS[actualIndex].ID;
                    }
                    else
                    {
                        currentId = dtDanhSachGoc.DefaultView[actualIndex]["ID"].ToString();
                    }

                    if (currentId == id)
                    {
                        rowIndexOnGrid = i;
                        break;
                    }
                }

                // 3. Nếu tìm thấy dòng đó đang hiển thị trên màn hình thì Focus chuẩn xác
                if (rowIndexOnGrid >= 0 && rowIndexOnGrid < kryptonDataGridView1.Rows.Count)
                {
                    kryptonDataGridView1.SuspendLayout();
                    kryptonDataGridView1.ClearSelection();

                    // Ép Invalidate để Grid vẽ lại dữ liệu mới của riêng dòng này
                    kryptonDataGridView1.InvalidateRow(rowIndexOnGrid);

                    // Bôi xanh dòng
                    kryptonDataGridView1.Rows[rowIndexOnGrid].Selected = true;

                    // Đưa con trỏ ô hiện tại về ô đầu tiên hiển thị của dòng đó để mồi phím tắt
                    foreach (DataGridViewCell cell in kryptonDataGridView1.Rows[rowIndexOnGrid].Cells)
                    {
                        if (cell.Visible)
                        {
                            kryptonDataGridView1.CurrentCell = cell;
                            break;
                        }
                    }
                    kryptonDataGridView1.ResumeLayout();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi Focus dòng sau cập nhật: " + ex.Message);
            }
        }
        private void KryptonDataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                // 1. Chặn click lỗi (Click vào vùng trống hoặc tiêu đề dòng)
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

                var col = kryptonDataGridView1.Columns[e.ColumnIndex];

                // ⭐ CẬP NHẬT ĐIỀU KIỆN: Chấp nhận cả 4 cột Họ Tên, Số Hiệu, Đơn Vị, Tình Trạng
                if (col.Name != "HoVaTen" && col.Name != "SoHieu" && col.Name != "DonVi" && col.Name != "TinhTrang")
                    return;

                // 🌟 BẬT HIỆU ỨNG CHUỘT CHỜ ĐỂ USER BIẾT APP ĐÃ NHẬN LỆNH
                this.Cursor = Cursors.WaitCursor;

                string id = "";
                string donVi = "";
                string hoTen = "";
                string tinhTrang = "";
                string soHieu = ""; // THÊM DÒNG NÀY
                // =========================================================================
                // 🚀 ĐỌC DỮ LIỆU TỪ RAM CHUẨN VIRTUAL MODE (Không đọc qua Grid.Rows)
                // =========================================================================
                bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);

                int actualIndex = _filteredIndexes[e.RowIndex]; // Lấy index thật đã qua bộ lọc

                if (_isDataMaxMode)
                {
                    // ⚡ NHÁNH 1: Chế độ 10.000+ dòng (Đọc thẳng từ List Cache Object)
                    if (laTanBinh)
                    {
                        var dataObj = _dataCacheTanBinh[actualIndex];
                        id = dataObj.ID;
                        donVi = dataObj.DonViE;
                        hoTen = dataObj.HoTenE;
                        tinhTrang = dataObj.TinhTrang;
                    }
                    else
                    {
                        var dataObj = _dataCacheCBCS[actualIndex];
                        id = dataObj.ID;
                        donVi = dataObj.DonViE;
                        hoTen = dataObj.HoTenE;
                        tinhTrang = dataObj.TinhTrang;
                        soHieu = dataObj.SoHieuE; // THÊM DÒNG NÀY
                    }
                }
                else
                {
                    // ⚡ NHÁNH 2: Chế độ Standard (Đọc thẳng từ DataTable/DataView gốc)
                    DataRowView rowView = dtDanhSachGoc.DefaultView[actualIndex];
                    id = rowView["ID"]?.ToString() ?? "";
                    donVi = rowView["DonVi"]?.ToString() ?? "";
                    hoTen = rowView["HoVaTen"]?.ToString() ?? "";
                    tinhTrang = rowView["TinhTrang"]?.ToString() ?? "";
                    soHieu = rowView["SoHieu"]?.ToString() ?? ""; // THÊM DÒNG NÀY
                }

                if (string.IsNullOrEmpty(id)) return;
                int idInt = int.Parse(id);

                // Trả lại chuột bình thường trước khi mở Form nặng
                this.Cursor = Cursors.Default;

                // =========================================================================
                // HIỂN THỊ FORM CHỈNH SỬA
                // =========================================================================
                if (laTanBinh)
                {
                    using (var frm = new Form30_ChinhSuaDataTanBinh(idInt, donVi))
                    {
                        frm.Owner = this;
                        frm.ShowInTaskbar = false;
                        frm.ShowDialog();
                        if (frm.DialogResult == DialogResult.OK)
                        {
                            using (var cn = new SqliteConnection($"Data Source={_csdl4Path}"))
                            {
                                cn.Open();
                                CapNhatTongLoai_Cho(idInt, cn);
                            }
                            RefreshRowFromDb(id);
                        }
                    }
                }
                else
                {
                    using (var frm22 = new Form22_ChinhSuaDataCBCS())
                    {
                        frm22.ID_CBCS = idInt;
                        frm22.HoVaTen = hoTen;
                        frm22.SoHieu = soHieu;  // Truyền số hiệu vào Form22 Yêu mèo cam
                        frm22.TinhTrang = tinhTrang;
                        frm22.DonVi = donVi;

                        frm22.Owner = this;
                        frm22.ShowInTaskbar = false;
                        frm22.ShowDialog();

                        if (frm22.DialogResult == DialogResult.OK)
                        {
                            using (var cn = new SqliteConnection($"Data Source={_csdl4Path}"))
                            {
                                cn.Open();
                                CapNhatTongLoai_Cho(idInt, cn);
                            }
                            RefreshRowFromDb(id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi mở Form sửa dữ liệu: " + ex.Message);
            }
            finally
            {
                this.Cursor = Cursors.Default; // Đảm bảo luôn tắt con chuột xoay vòng
            }
        }
        private void kryptonButton_LamMoiCacOTimKiem_Click(object sender, EventArgs e)
        {
            _dangSetPlaceholder = true;

            InitPlaceholderTimKiem();

            comboBox_TimKiemDonVi.SelectedIndexChanged -= comboBox_TimKiemDonVi_SelectedIndexChanged;
            comboBox1_TinhTrang.SelectedIndexChanged -= comboBox1_TinhTrang_SelectedIndexChanged;

            comboBox_TimKiemDonVi.SelectedItem = "Tất cả";
            comboBox1_TinhTrang.SelectedItem = "Tất cả";

            comboBox_TimKiemDonVi.SelectedIndexChanged += comboBox_TimKiemDonVi_SelectedIndexChanged;
            comboBox1_TinhTrang.SelectedIndexChanged += comboBox1_TinhTrang_SelectedIndexChanged;

            _dangSetPlaceholder = false;

            ApplyFilter();
        }
        //Menu trip
        private void dongBoDuLieu_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            kryptonButton_CapNhat.PerformClick();
        }
        private void xuatDuLieu_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            kryptonButton_XuatData.PerformClick();
        }
        private void xemThongKeThiDuaTapThe_Click(object sender, EventArgs e)
        {
            // 1. Chống Click đúp
            if (_dangMoThongKeTapThe) return;

            // 2. Chốt chặn an toàn (SỬ DỤNG .Available THAY VÌ .Visible)
            if (!xemThongKeThiDuaTapThe.Available) return;

            _dangMoThongKeTapThe = true;

            try
            {
                // ===== KHOÁ CONTEXT MENU =====
                var ctxBackup = kryptonDataGridView1.ContextMenuStrip;
                kryptonDataGridView1.ContextMenuStrip = null;

                // ===== MỞ FORM =====
                var f = new Form23_ThongKeThiDuaTapThe
                {
                    StartPosition = FormStartPosition.CenterScreen
                };

                f.FormClosed += (s, args) =>
                {
                    kryptonDataGridView1.ContextMenuStrip = ctxBackup;
                    _dangMoThongKeTapThe = false;
                    this.Activate();
                };

                f.Show();
            }
            catch (Exception ex)
            {
                _dangMoThongKeTapThe = false;
                // Ghi log lỗi nếu mở form thất bại
                System.Diagnostics.Debug.WriteLine($"Lỗi mở form thống kê tập thể: {ex.Message}");
            }
        }
        private void xoaCot_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //==================================================
            // XÁC ĐỊNH PHIÊN BẢN
            //==================================================
            string phienBan = Module_TaiKhoan.LayPhienBanPhanMem();

            bool laTanBinh =
                !string.IsNullOrWhiteSpace(phienBan) &&
                phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase);

            string tenBang = laTanBinh
                ? "ThiDuaThang_TanBinh"
                : "ThiDuaThang";

            //==================================================
            // DANH SÁCH CỘT CẤM XÓA
            //==================================================
            string[] cotCamXoaCBCS =
            {
        "ID","HoVaTen","SoHieu","DonVi","TinhTrang",
        "KQ_ThiDua_Nam_Cu","KQ_XepLoaiCB_Nam_Cu","KQ_XepLoaiDangVien_Nam_Cu",
        "Thang_12_Nam_Cu","Thang_1","Thang_2","Thang_3","Thang_4","Thang_5","Sau_Thang_Dau_Nam",
        "Thang_6","Thang_7","Thang_8","Thang_9","Thang_10","Thang_11","TongKet_Nam",
        "TS_Loai1","TS_Loai2","TS_Loai3","TS_Loai4"
    };

            string[] cotCamXoaTanBinh =
            {
        "ID","HoVaTen","SoHieu","DonVi","TinhTrang",
        "Tuan_1_T2","Tuan_2_T2","Tuan_3_T2","Tuan_4_T2","Thang_2",
        "Tuan_1_T3","Tuan_2_T3","Tuan_3_T3","Tuan_4_T3","Thang_3",
        "Tuan_1_T4","Tuan_2_T4","Tuan_3_T4","Tuan_4_T4","Thang_4",
        "Tuan_1_T5","Tuan_2_T5","Tuan_3_T5","Tuan_4_T5","Thang_5",
       "Tuan_1_T6","Tuan_2_T6","Tuan_3_T6","Tuan_4_T6","Thang_6",
        "TS_Loai1","TS_Loai2","TS_Loai3","TS_Loai4"
    };

            var cotCamXoa = laTanBinh ? cotCamXoaTanBinh : cotCamXoaCBCS;

            //==================================================
            // KIỂM TRA CSDL
            //==================================================
            if (!File.Exists(_csdl4Path))
            {
                MessageBox.Show("Không tìm thấy CSDL!",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //==================================================
            // LẤY CỘT ĐANG CHỌN
            //==================================================
            if (kryptonDataGridView1.CurrentCell == null)
            {
                MessageBox.Show("Vui lòng chọn cột cần xóa.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tenCot = kryptonDataGridView1
                .Columns[kryptonDataGridView1.CurrentCell.ColumnIndex]
                .Name;

            //==================================================
            // KIỂM TRA CỘT HỆ THỐNG
            //==================================================
            if (cotCamXoa.Contains(tenCot, StringComparer.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Không được xóa cột hệ thống: {tenCot}",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            //==================================================
            // XÁC NHẬN
            //==================================================
            if (MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa cột:\n\n👉 {tenCot}\n\nHành động này không thể khôi phục!",
                "Xác nhận xóa cột",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                using var cn = new SqliteConnection($"Data Source={_csdl4Path}");
                cn.Open();

                using var cmd = new SqliteCommand(
                    $"ALTER TABLE {tenBang} DROP COLUMN [{tenCot}]",
                    cn);

                cmd.ExecuteNonQuery();

                //==================================================
                // NHẬT KÝ
                //==================================================
                Module_NhatKy.GhiNhatKy(
                    Module_TaiKhoan.TenTaiKhoan_RAM,
                    "Xóa cột CSDL",
                    $"Đã xóa cột: {tenCot} trong bảng {tenBang}"
                );
                //==================================================
                // RELOAD ĐÚNG BẢNG
                //==================================================
                if (laTanBinh)
                {
                    LoadThongKe_TanBinh();
                }
                else
                {
                    CapNhatForm15();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Lỗi khi xóa cột:\n" + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void xoaDuLieuThiDuaThang_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // 'this' ở đây chính là Form15_ThongKeThiDua
                using (Form18_XoaDataThang frm = new Form18_XoaDataThang(this))
                {
                    frm.ShowInTaskbar = false;                    // Không hiển thị icon riêng
                    frm.StartPosition = FormStartPosition.CenterParent; // Hiển thị giữa cha
                    frm.ShowDialog(this);                          // Chỉ định Form cha
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở Form18: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private bool TryGetSelectedRecordId(out int recordId)
        {
            recordId = 0;

            // 1. Kiểm tra lưới và dòng hiện tại có hợp lệ không
            if (kryptonDataGridView1 == null ||
                kryptonDataGridView1.CurrentRow == null ||
                kryptonDataGridView1.CurrentRow.Index < 0)
            {
                return false;
            }

            int gridRowIndex = kryptonDataGridView1.CurrentRow.Index;

            // 2. Nếu đang chạy chế độ dữ liệu lớn DataMax (Virtual Mode không DataSource)
            if (_isDataMaxMode)
            {
                if (_filteredIndexes == null || gridRowIndex >= _filteredIndexes.Count)
                    return false;

                // Lấy chỉ số thực tế trong mảng tổng danh sách
                int actualIndex = _filteredIndexes[gridRowIndex];
                bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);

                string strId = "";
                if (laTanBinh && _dataCacheTanBinh != null && actualIndex < _dataCacheTanBinh.Count)
                {
                    strId = _dataCacheTanBinh[actualIndex].ID;
                }
                else if (!laTanBinh && _dataCacheCBCS != null && actualIndex < _dataCacheCBCS.Count)
                {
                    strId = _dataCacheCBCS[actualIndex].ID;
                }

                return int.TryParse(strId, out recordId) && recordId > 0;
            }
            else
            {
                // 3. Chế độ Standard (<= 3000 dòng): Trả về logic cũ của DataTable DefaultView
                if (_filteredIndexes == null || gridRowIndex >= _filteredIndexes.Count || dtDanhSachGoc == null)
                    return false;

                int actualIndex = _filteredIndexes[gridRowIndex];
                if (actualIndex >= dtDanhSachGoc.DefaultView.Count)
                    return false;

                object cellValue = dtDanhSachGoc.DefaultView[actualIndex]["ID"];
                if (cellValue == null || cellValue == DBNull.Value)
                    return false;

                return int.TryParse(cellValue.ToString(), out recordId) && recordId > 0;
            }
        }
        private void xoaDuLieu_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // =============================
            // 1. LẤY ID BẢN GHI
            // =============================
            if (!TryGetSelectedRecordId(out int recordId))
            {
                MessageBox.Show(
                    "Vui lòng chọn dòng cần xóa.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            bool laTanBinh = Module_TaiKhoan
                .LayPhienBanPhanMem()
                .Contains("tân binh", StringComparison.OrdinalIgnoreCase);

            string tenBang = laTanBinh
                ? "ThiDuaThang_TanBinh"
                : "ThiDuaThang";

            string hoVaTen = "(không xác định)";

            // =============================
            // 2. LẤY HỌ TÊN TỪ DATABASE
            // =============================
            try
            {
                using var cn = new SqliteConnection($"Data Source={_csdl4Path}");
                cn.Open();

                using var cmd = new SqliteCommand(
                    $"SELECT HoVaTen FROM {tenBang} WHERE ID = @id",
                    cn);

                cmd.Parameters.Add("@id", SqliteType.Integer).Value = recordId;

                var scalar = cmd.ExecuteScalar();

                if (scalar != null && scalar != DBNull.Value)
                {
                    string hoVaTenMaHoa = scalar.ToString();
                    hoVaTen = BaoMatAES.GiaiMa(hoVaTenMaHoa);
                }
            }
            catch (Exception ex)
            {
                hoVaTen = "(lỗi lấy họ tên)";

                Module_NhatKy.GhiNhatKy(
                    Module_TaiKhoan.TenTaiKhoan_RAM,
                    "Lỗi lấy HoVaTen khi xóa",
                    ex.Message);
            }

            // =============================
            // 3. XÁC NHẬN NGƯỜI DÙNG
            // =============================
            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa đồng chí:\n\n👉 {hoVaTen}\n\nThao tác này không thể hoàn tác.",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            // =============================
            // 4. XÁC MINH ADMIN
            // =============================
            DialogResult kq;

            using (Form24_XacMinhAdmin frm = new Form24_XacMinhAdmin())
            {
                frm.TopMost = true;
                frm.StartPosition = FormStartPosition.CenterScreen;
                kq = frm.ShowDialog();
            }

            if (kq != DialogResult.OK)
                return;

            // =============================
            // 5. XÓA TRONG DATABASE
            // =============================
            try
            {
                using var cn = new SqliteConnection($"Data Source={_csdl4Path}");
                cn.Open();

                using var tran = cn.BeginTransaction();

                using var cmd = new SqliteCommand(
                    $"DELETE FROM {tenBang} WHERE ID = @id",
                    cn, tran);

                cmd.Parameters.Add("@id", SqliteType.Integer).Value = recordId;

                int affected = cmd.ExecuteNonQuery();

                if (affected != 1)
                    throw new Exception("Không tìm thấy bản ghi để xóa.");

                tran.Commit();

                // Tối ưu database
                using var cmdVacuum = new SqliteCommand("VACUUM;", cn);
                cmdVacuum.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Xóa dữ liệu thất bại:\n" + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // =============================
            // 6. LOAD LẠI DỮ LIỆU
            // =============================
            if (laTanBinh)
                LoadThongKe_TanBinh();
            else
                LoadThongKe_CBCS();

            ApplyFilter();
            int tong = _filteredIndexes?.Count ?? 0; // SỬA LẠI NHƯ THẾ NÀY
            toolStripStatusLabel1.Text = $"Tổng cộng: {tong} đồng chí";
            try
            {
                System.Media.SystemSounds.Exclamation.Play();
            }
            catch { }

            // =============================
            // 7. GHI NHẬT KÝ
            // =============================
            Module_NhatKy.GhiNhatKy(
                Module_TaiKhoan.TenTaiKhoan_RAM,
                "Xóa dòng thống kê",
                $"Đã xóa: {hoVaTen} (ID={recordId})");
        }
        private void CapNhatPhienBanPhanMem()
        {
            try
            {
                string doiTuong = Module_TaiKhoan.LayPhienBanPhanMem();

                if (string.IsNullOrWhiteSpace(doiTuong))
                    doiTuong = "Phần mềm: (không xác định)";

                // Khi thiết lập StatusStrip
                toolStripStatusLabel2.Spring = true;      // chiếm phần còn lại
                toolStripStatusLabel2.TextAlign = ContentAlignment.MiddleRight;
                toolStripStatusLabel2.Text = doiTuong;
            }
            catch
            {
                toolStripStatusLabel2.Text = "Phần mềm: (không xác định)";
            }
        }
        private string XacDinhTenTieuDoan()
        {
            try
            {
                using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT TenTieuDoan FROM ThongTin WHERE ID=1";
                var result = cmd.ExecuteScalar();

                if (result != null && !string.IsNullOrWhiteSpace(result.ToString()))
                {
                    string raw = result.ToString().Trim();
                    // ⭐ Dùng SafeDecrypt để đọc được cả tên cũ và mới
                    return SafeDecrypt(raw);
                }
                return "Không xác định";
            }
            catch { return "Không xác định"; }
        }
        private void kryptonButton1_ThemKhenThuong_Click(object sender, EventArgs e)
        {
            // gọi Form36_ThongKeKhenThuong, nếu đã có sẵn trong form thì gọi lôi ra, tối ưu RAM
            // 1. Tìm Form cha đang mở
            var formCha = Application.OpenForms
                .OfType<Form2_FormCha>()
                .FirstOrDefault();

            if (formCha == null) return;

            // 2. Tìm PanelContainer chứa các form con
            var panel = formCha.Controls
                .Find("PanelContainer", true)
                .FirstOrDefault() as Panel;

            if (panel == null) return;

            // 3. Ẩn tất cả các form hiện hành trong panel để nhường chỗ cho form mới
            foreach (System.Windows.Forms.Control ctl in panel.Controls)
            {
                if (ctl is Form frm)
                    frm.Hide();
            }

            // 4. Tìm Form36 xem đã được tạo và nằm trong panel chưa (Tối ưu RAM/CPU)
            var form36 = panel.Controls
                .OfType<Form34_ThongKeKhenThuong>()
                .FirstOrDefault();

            // Nếu chưa có, tiến hành khởi tạo lần đầu
            if (form36 == null)
            {
                form36 = new Form34_ThongKeKhenThuong
                {
                    TopLevel = false,
                    FormBorderStyle = FormBorderStyle.None,
                    Dock = DockStyle.Fill,
                    Text = "Thống kê khen thưởng" // Đặt tiêu đề nếu cần
                };

                // ⭐ Sự kiện: Khi form36 bị đóng, bạn muốn gọi lại form nào? 
                // Ở đây mình ví dụ gọi lại form hiện tại (tức là form chứa nút click này, thay "FormHienTaiCuaBan" bằng tên Form thực tế, ví dụ Form6_XuLyData)
                form36.FormClosed += (s, ev) =>
                {
                    if (panel.IsDisposed) return;

                    // Tìm lại form bạn muốn hiển thị sau khi Form36 đóng
                    var formQuayLai = panel.Controls
                        .OfType<Form34_ThongKeKhenThuong>() // <-- ĐỔI TÊN FORM BẠN MUỐN HIỂN THỊ LẠI Ở ĐÂY
                        .FirstOrDefault();

                    if (formQuayLai != null && !formQuayLai.IsDisposed)
                    {
                        formQuayLai.Dock = DockStyle.Fill;
                        formQuayLai.Show();
                        formQuayLai.BringToFront();
                    }
                };
                // Thêm vào panel
                panel.Controls.Add(form36);
            }
            // 5. Nếu đã có sẵn (hoặc vừa tạo xong), chỉ cần lôi ra và hiển thị
            form36.Show();
            form36.BringToFront();
        }
        private void tinhPhanLoaiThang_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // ── 1. XÁC ĐỊNH PHIÊN BẢN ────────────────────────────────────────────────
            string phienBan = Module_TaiKhoan.LayPhienBanPhanMem();
            bool laTanBinh = !string.IsNullOrWhiteSpace(phienBan) &&
                             phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase);

            // ── 2. KIỂM TRA DỮ LIỆU VÀ CHUẨN BỊ DataTable ĐỂ TRUYỀN ────────────────
            // Mục tiêu: Form36 LUÔN nhận được một DataTable có dữ liệu thực
            // Không bao giờ truyền null hoặc DataTable rỗng sang Form36
            DataTable dtTruyenSang = null;

            if (_isDataMaxMode)
            {
                // Chế độ DataMax: dữ liệu nằm trong List<object> cache, không phải DataTable
                bool khongCoDuLieu =
     laTanBinh
         ? (_dataCacheTanBinh == null || _dataCacheTanBinh.Count == 0)
         : (_dataCacheCBCS == null || _dataCacheCBCS.Count == 0);

                if (khongCoDuLieu)
                {
                    MessageBox.Show(
                        "Chưa có dữ liệu để tính toán. Vui lòng tải dữ liệu hoặc bấm nút Cập nhật (F6).",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }
                // ✅ Truyền null có chủ đích → Form36 sẽ tự load từ CSDL4
                // (Đây là cách sạch nhất: Form36 biết cách tự lo cho mình)
                dtTruyenSang = null;
            }
            else
            {
                // Chế độ thường: kiểm tra DataTable gốc
                if (dtDanhSachGoc == null || dtDanhSachGoc.Rows.Count == 0)
                {
                    MessageBox.Show(
                        "Chưa có dữ liệu để tính toán. Vui lòng tải dữ liệu hoặc bấm nút Cập nhật (F6).",
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // ✅ Truyền bản Copy để Form36 không làm ảnh hưởng dữ liệu gốc Form15
                dtTruyenSang = dtDanhSachGoc.Clone();

                foreach (DataRow r in dtDanhSachGoc.Rows)
                    dtTruyenSang.ImportRow(r);
            }

            // ── 3. TÌM FORM CHA VÀ PANEL CHỨA ───────────────────────────────────────
            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            if (formCha == null) return;

            var panel = formCha.Controls.Find("PanelContainer", true).FirstOrDefault() as Panel;
            if (panel == null) return;

            // ── 4. KIỂM TRA FORM 36 ĐÃ MỞ CHƯA ─────────────────────────────────────
            var f36Existing = panel.Controls.OfType<Form36_TinhPhanLoaiThang>().FirstOrDefault();

            if (f36Existing != null)
            {
                // Form đã mở: chỉ BringToFront, không tạo mới (tiết kiệm RAM)
                f36Existing.BringToFront();
                return;
            }

            // ── 5. TẠO FORM 36 MỚI VÀ TRUYỀN DỮ LIỆU ────────────────────────────────
            var f36 = new Form36_TinhPhanLoaiThang(dtTruyenSang, laTanBinh)
            {
                Text = laTanBinh
                                   ? "Tính phân loại thi đua tháng - Tân Binh"
                                   : "Tính phân loại thi đua - CBCS",
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill
            };

            panel.Controls.Add(f36);
            f36.Show();
            f36.BringToFront();

            // ── 6. XỬ LÝ KHI ĐÓNG FORM 36 → QUAY LẠI FORM 15 ───────────────────────
            var form15Ref = this; // Capture tham chiếu tường minh, tránh memory leak
            f36.FormClosed += (s, ev) =>
            {
                if (form15Ref == null || form15Ref.IsDisposed) return;
                form15Ref.Dock = DockStyle.Fill;
                form15Ref.Show();
                form15Ref.BringToFront();
            };
        }
        private async void kryptonButton_XuatData_Click(object sender, EventArgs e)
        {
            // =========================================================
            // LỚP VỎ UX: LƯU TRẠNG THÁI GỐC CỦA NÚT
            // =========================================================
            string textBanDau = kryptonButton_XuatData.Values.Text;
            Image anhBanDau = kryptonButton_XuatData.Values.Image;

            Form_Loading frmLoad = new Form_Loading("Đang tạo tệp excel, vui lòng đợi...");
            bool isLoadShown = false;
            frmLoad.Icon = this.Icon;
            try
            {
                kryptonButton_XuatData.Enabled = false;
                kryptonButton_XuatData.Values.Text = "Đang tạo...";
                kryptonButton_XuatData.Values.Image = null;
                await Task.Delay(100);

                if (kryptonDataGridView1.Rows.Count == 0 ||
                    (kryptonDataGridView1.Rows.Count == 1 && kryptonDataGridView1.AllowUserToAddRows))
                {
                    MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                try
                {
                    string dbPath = _csdl4Path;
                    if (!File.Exists(dbPath))
                    {
                        MessageBox.Show("Không tìm thấy CSDL!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    string phienBan = Module_TaiKhoan.LayPhienBanPhanMem();
                    bool laTanBinh = !string.IsNullOrWhiteSpace(phienBan) && phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                    string tenBang = laTanBinh ? "ThiDuaThang_TanBinh" : "ThiDuaThang";

                    var cotTrongBang = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    using (var cn = new SqliteConnection($"Data Source={dbPath}"))
                    {
                        cn.Open();
                        using var cmd = new SqliteCommand($"PRAGMA table_info({tenBang})", cn);
                        using var rd = cmd.ExecuteReader();
                        while (rd.Read())
                        {
                            string cot = rd["name"].ToString();
                            if (!cot.Equals("ID", StringComparison.OrdinalIgnoreCase))
                                cotTrongBang.Add(cot);
                        }
                    }

                    var exportCols = kryptonDataGridView1.Columns
                        .Cast<DataGridViewColumn>()
                        .Where(c => cotTrongBang.Contains(c.Name))
                        .Where(c =>
                        {
                            if (laTanBinh)
                            {
                                if (c.Name.Equals("Thang_2", StringComparison.OrdinalIgnoreCase)) return false;
                                if (c.Name.StartsWith("Tuan_") && c.Name.EndsWith("_T6")) return false;
                            }
                            return true;
                        })
                        .ToList();

                    var normalCols = exportCols.Where(c => !c.Name.StartsWith("TS_Loai")).ToList();
                    var totalCols = exportCols.Where(c => c.Name.StartsWith("TS_Loai")).OrderBy(c => c.Name).ToList();
                    exportCols = normalCols.Concat(totalCols).ToList();

                    bool hasSTT = kryptonDataGridView1.Columns
                        .Cast<DataGridViewColumn>()
                        .Any(c => c.Name.Equals("STT", StringComparison.OrdinalIgnoreCase));
                    int rowCount = kryptonDataGridView1.AllowUserToAddRows ? kryptonDataGridView1.Rows.Count - 1 : kryptonDataGridView1.Rows.Count;
                    int colCount = exportCols.Count + (hasSTT ? 0 : 1);

                    using var sfd = new SaveFileDialog
                    {
                        Title = "Chọn nơi lưu file Excel thống kê",
                        Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                        FileName = $"ThongKe_ThiDua_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    };

                    if (sfd.ShowDialog() != DialogResult.OK) return;
                    string filePath = sfd.FileName;

                    this.Enabled = false;
                    frmLoad.Show(this);
                    isLoadShown = true;
                    await Task.Delay(50);

                    Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Xuất Excel thống kê", $"Bảng: {tenBang} | {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
                    // ⭐ ĐỔ DỮ LIỆU SIÊU TỐC VÀ CHÍNH XÁC (Tương thích cả 2 chế độ DataMax và Standard)
                    var dataArray = new object[rowCount, colCount];
                    for (int r = 0; r < rowCount; r++)
                    {
                        int cIndex = 0;
                        if (!hasSTT) dataArray[r, cIndex++] = r + 1;

                        // Lấy đúng Index thực tế đang hiển thị trên Grid sau khi lọc
                        int actualIndex = _filteredIndexes[r];

                        foreach (var col in exportCols)
                        {
                            string cellValue = "";

                            // NHÁNH 1: Chạy chế độ Dữ liệu Lớn -> Lấy thẳng từ RAM Object
                            if (_isDataMaxMode)
                            {
                                if (laTanBinh && _dataCacheTanBinh != null && actualIndex < _dataCacheTanBinh.Count)
                                {
                                    var dataObj = _dataCacheTanBinh[actualIndex];
                                    if (col.Name == "HoVaTen") cellValue = dataObj.HoTenE;
                                    else if (col.Name == "SoHieu") cellValue = dataObj.SoHieuE;
                                    else if (col.Name == "DonVi") cellValue = dataObj.DonViE;
                                    else if (col.Name == "TinhTrang") cellValue = dataObj.TinhTrang;
                                    else if (col.Name == "GhiChu") cellValue = dataObj.GhiChu;
                                    else if (col.Name == "TS_Loai1") cellValue = dataObj.L1;
                                    else if (col.Name == "TS_Loai2") cellValue = dataObj.L2;
                                    else if (col.Name == "TS_Loai3") cellValue = dataObj.L3;
                                    else if (col.Name == "TS_Loai4") cellValue = dataObj.L4;
                                    else if (dataObj.DuLieuThang.TryGetValue(col.Name, out string dThang)) cellValue = dThang;
                                    else if (dataObj.CotPhatSinh != null && dataObj.CotPhatSinh.TryGetValue(col.Name, out string dynVal)) cellValue = dynVal;
                                }
                                else if (!laTanBinh && _dataCacheCBCS != null && actualIndex < _dataCacheCBCS.Count)
                                {
                                    var dataObj = _dataCacheCBCS[actualIndex];
                                    if (col.Name == "HoVaTen") cellValue = dataObj.HoTenE;
                                    else if (col.Name == "SoHieu") cellValue = dataObj.SoHieuE;
                                    else if (col.Name == "DonVi") cellValue = dataObj.DonViE;
                                    else if (col.Name == "TinhTrang") cellValue = dataObj.TinhTrang;
                                    else if (col.Name == "GhiChu") cellValue = dataObj.GhiChu;
                                    else if (col.Name == "KQ_ThiDua_Nam_Cu") cellValue = dataObj.KQ_TD;
                                    else if (col.Name == "KQ_XepLoaiCB_Nam_Cu") cellValue = dataObj.KQ_XL_CB;
                                    else if (col.Name == "KQ_XepLoaiDangVien_Nam_Cu") cellValue = dataObj.KQ_XL_DV;
                                    else if (col.Name == "Thang_12_Nam_Cu") cellValue = dataObj.T12;
                                    else if (col.Name == "Sau_Thang_Dau_Nam") cellValue = dataObj.SauThang;
                                    else if (col.Name == "TongKet_Nam") cellValue = dataObj.TongKet;
                                    else if (col.Name == "TS_Loai1") cellValue = dataObj.L1;
                                    else if (col.Name == "TS_Loai2") cellValue = dataObj.L2;
                                    else if (col.Name == "TS_Loai3") cellValue = dataObj.L3;
                                    else if (col.Name == "TS_Loai4") cellValue = dataObj.L4;
                                    else if (col.Name.StartsWith("Thang_"))
                                    {
                                        if (int.TryParse(col.Name.Replace("Thang_", ""), out int tIdx) && tIdx >= 1 && tIdx <= 11)
                                            cellValue = dataObj.Thang[tIdx - 1];
                                    }
                                    else if (dataObj.CotPhatSinh != null && dataObj.CotPhatSinh.TryGetValue(col.Name, out string dynVal)) cellValue = dynVal;
                                }
                            }
                            // NHÁNH 2: Chế độ bình thường (<= 3000 dòng) -> Lấy từ DataView cũ
                            else
                            {
                                if (dtDanhSachGoc != null && dtDanhSachGoc.Columns.Contains(col.Name) && actualIndex < dtDanhSachGoc.DefaultView.Count)
                                    cellValue = dtDanhSachGoc.DefaultView[actualIndex][col.Name]?.ToString() ?? "";
                            }

                            dataArray[r, cIndex++] = cellValue;
                        }
                    }
                    await Task.Run(() =>
                    {
                        using var wb = new XLWorkbook();
                        var ws = wb.Worksheets.Add("ThongKeThiDua");

                        ws.Cell("A1").Value = "DANH SÁCH";
                        ws.Range(1, 1, 1, colCount).Merge().Style.Font.SetBold().Font.SetFontSize(16).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                        string tenTieuDoan = XacDinhTenTieuDoan();
                        ws.Cell("A2").Value = $"THỐNG KÊ PHÂN LOẠI THI ĐUA CỦA CBCS {tenTieuDoan}";
                        ws.Range(2, 1, 2, colCount).Merge().Style.Font.SetBold().Font.SetFontSize(12).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                        int excelStartRow = 4;
                        int excelCol = 1;

                        if (!hasSTT)
                        {
                            var cell = ws.Cell(excelStartRow, excelCol);
                            cell.Value = "STT";
                            cell.Style.Font.Bold = true;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            excelCol++;
                        }

                        foreach (var col in exportCols)
                        {
                            var cell = ws.Cell(excelStartRow, excelCol);
                            cell.Value = string.IsNullOrWhiteSpace(col.HeaderText) ? col.Name.Replace("_", " ") : col.HeaderText;
                            cell.Style.Font.Bold = true;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            cell.Style.Alignment.WrapText = true;
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                            if (laTanBinh && (
                                col.Name == "Tuan_1_T2" || col.Name == "Tuan_2_T2" || col.Name == "Tuan_3_T2" || col.Name == "Tuan_4_T2" || col.Name == "Thang_3" ||
                                col.Name == "Tuan_1_T4" || col.Name == "Tuan_2_T4" || col.Name == "Tuan_3_T4" || col.Name == "Tuan_4_T4" || col.Name == "Thang_5"
                            ))
                            {
                                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(220, 235, 255);
                            }
                            else if (col.Name.StartsWith("TS_Loai"))
                            {
                                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(230, 250, 230);
                            }
                            else
                            {
                                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                            }

                            if (laTanBinh && col.Name.StartsWith("Thang_"))
                            {
                                cell.Style.Font.FontColor = XLColor.Blue;
                            }
                            excelCol++;
                        }

                        int hoTenColIndex = exportCols.FindIndex(c => c.Name.Equals("HoVaTen", StringComparison.OrdinalIgnoreCase));
                        if (!hasSTT) hoTenColIndex += 1;

                        // ⭐ ĐỔ DỮ LIỆU SIÊU TỐC (Chỉ gán Value, không gán Style ở đây)
                        for (int r = 0; r < rowCount; r++)
                        {
                            for (int c = 0; c < colCount; c++)
                            {
                                ws.Cell(r + excelStartRow + 1, c + 1).Value = dataArray[r, c]?.ToString() ?? "";
                            }
                        }

                        // ⭐ ĐỊNH DẠNG HÀNG LOẠT (BULK FORMATTING) CỰC NHANH
                        if (rowCount > 0)
                        {
                            var dataRange = ws.Range(excelStartRow + 1, 1, excelStartRow + rowCount, colCount);
                            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                            dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            dataRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            // Ghi đè căn trái cho cột STT và Họ Tên
                            if (!hasSTT)
                            {
                                ws.Range(excelStartRow + 1, 1, excelStartRow + rowCount, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                            }
                            ws.Range(excelStartRow + 1, hoTenColIndex + 1, excelStartRow + rowCount, hoTenColIndex + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        }

                        int tongDong = rowCount + excelStartRow + 1;
                        var totalCell = ws.Cell(tongDong, 1);
                        totalCell.Value = $"Tổng cộng: {rowCount} đồng chí./.";
                        totalCell.Style.Font.SetBold().Font.SetItalic();
                        ws.Range(tongDong, 1, tongDong, colCount).Merge();
                        var wsVersion = wb.Worksheets.Add("Phiên bản");
                        var cellA1 = wsVersion.Cell("A1");
                        cellA1.Value = "2026 Competition Software developed by TrungKien";
                        cellA1.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9EAD3");
                        cellA1.Style.Font.Bold = true;
                        cellA1.Style.Font.FontColor = XLColor.Black;
                        string textPhienBan = laTanBinh ? "Phần mềm dành cho Tân binh" : "Phần mềm dành cho CBCS";
                        wsVersion.Cell("B1").Value = BaoMatAES.MaHoa(textPhienBan);
                        wsVersion.Columns().AdjustToContents();
                        wsVersion.Hide();
                        ws.Columns().AdjustToContents();
                        Module_BanQuyen.DongDauExcel(wb);
                        wb.SaveAs(filePath);

                        // 🔥 UX MỚI: Chỉ Highlight file, không mở bừa bãi cửa sổ mới
                        Module_XuatNhapDuLieuThiDua.MoVaChonTepTrongExplorer(filePath);
                    });
                    Module_ThongBao.ThanhCong("Xuất Excel thành công!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi xuất dữ liệu:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                if (isLoadShown)
                {
                    frmLoad.Close();
                    this.Enabled = true;
                    this.Focus();
                }
                kryptonButton_XuatData.Values.Text = textBanDau;
                kryptonButton_XuatData.Values.Image = anhBanDau;
                kryptonButton_XuatData.Enabled = true;
            }
        }
        //Phiên bản nhập file excel tương thích phần mềm software củ và mới
        private async void nhapDuLieuTuTepExcel_ExcelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult kq;
            using (Form24_XacMinhAdmin frm = new Form24_XacMinhAdmin())
            {
                frm.TopMost = true;
                frm.StartPosition = FormStartPosition.CenterScreen;
                kq = frm.ShowDialog();
            }

            if (kq != DialogResult.OK) return;

            using OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Chọn tệp Excel dữ liệu thi đua",
                //Filter = "Excel Files (*.xlsx)|*.xlsx",
                // 👇 ĐÃ SỬA: Bổ sung thêm *.xlsm vào bộ lọc (cách nhau bằng dấu chấm phẩy)
                Filter = "Excel Files (*.xlsx, *.xlsm)|*.xlsx;*.xlsm",
                Multiselect = false
            };

            if (ofd.ShowDialog() != DialogResult.OK) return;
            string filePath = ofd.FileName;

            string phienBan = Module_TaiKhoan.LayPhienBanPhanMem();
            bool laTanBinh = !string.IsNullOrWhiteSpace(phienBan) && phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase);
            string tenBang = laTanBinh ? "ThiDuaThang_TanBinh" : "ThiDuaThang";

            string[] tatCaCotDb = laTanBinh
                ? new string[] { "ID", "HoVaTen", "SoHieu", "DonVi", "TinhTrang", "Tuan_1_T2", "Tuan_2_T2", "Tuan_3_T2", "Tuan_4_T2", "Thang_2", "Tuan_1_T3", "Tuan_2_T3", "Tuan_3_T3", "Tuan_4_T3", "Thang_3", "Tuan_1_T4", "Tuan_2_T4", "Tuan_3_T4", "Tuan_4_T4", "Thang_4", "Tuan_1_T5", "Tuan_2_T5", "Tuan_3_T5", "Tuan_4_T5", "Thang_5", "Tuan_1_T6", "Tuan_2_T6", "Tuan_3_T6", "Tuan_4_T6", "Thang_6", "TS_Loai1", "TS_Loai2", "TS_Loai3", "TS_Loai4" }
                : new string[] { "ID", "HoVaTen", "SoHieu", "DonVi", "TinhTrang", "KQ_ThiDua_Nam_Cu", "KQ_XepLoaiCB_Nam_Cu", "KQ_XepLoaiDangVien_Nam_Cu", "Thang_12_Nam_Cu", "Thang_1", "Thang_2", "Thang_3", "Thang_4", "Thang_5", "Sau_Thang_Dau_Nam", "Thang_6", "Thang_7", "Thang_8", "Thang_9", "Thang_10", "Thang_11", "TongKet_Nam", "TS_Loai1", "TS_Loai2", "TS_Loai3", "TS_Loai4" };

            int namCu = Module_NamHeThong.LayNamHeThong() - 1;
            var headerMap = new Dictionary<string, string>
            {
                ["HoVaTen"] = "Họ và tên",
                ["SoHieu"] = "Số hiệu",
                ["DonVi"] = "Đơn vị",
                ["TinhTrang"] = "Tình trạng",
                ["TS_Loai1"] = "Tổng\nLoại 1",
                ["TS_Loai2"] = "Tổng\nLoại 2",
                ["TS_Loai3"] = "Tổng\nLoại 3",
                ["TS_Loai4"] = "Tổng\nLoại 4"
            };

            if (laTanBinh)
            {
                for (int m = 2; m <= 6; m++)
                {
                    for (int t = 1; t <= 4; t++) headerMap[$"Tuan_{t}_T{m}"] = $"Tuần {t}\nTháng {m}";
                    headerMap[$"Thang_{m}"] = $"Kết quả\nTháng {m}";
                }
            }
            else
            {
                headerMap["KQ_ThiDua_Nam_Cu"] = $"KQ thi đua\nnăm {namCu}";
                headerMap["KQ_XepLoaiCB_Nam_Cu"] = $"Xếp loại CBCS\nnăm {namCu}";
                headerMap["KQ_XepLoaiDangVien_Nam_Cu"] = $"XL đảng viên\nnăm {namCu}";
                headerMap["Thang_12_Nam_Cu"] = $"Tháng 12\n{namCu}";
                headerMap["Sau_Thang_Dau_Nam"] = "6 tháng\nđầu năm";
                headerMap["TongKet_Nam"] = "Tổng kết\nnăm";
                for (int i = 1; i <= 11; i++) headerMap[$"Thang_{i}"] = $"Tháng {i}";
            }

            string ChuanHoa(string input) => input.Replace("\r", "").Replace("\n", "").Replace(" ", "").ToLower();

            string LấyTênHeaderMongMuốn(string colName)
            {
                if (colName.Equals("ID", StringComparison.OrdinalIgnoreCase)) return "ID";
                if (headerMap.TryGetValue(colName, out string vnName)) return vnName;
                return colName.Replace("_", " ");
            }

            List<string> danhSachCotTrenUI = kryptonDataGridView1.Columns
                .Cast<DataGridViewColumn>()
                .Select(c => c.Name)
                .ToList();

            Form_Loading frmLoad = new Form_Loading("Đang kiểm tra tính hợp lệ và nạp dữ liệu...");
            bool isLoadShown = false;

            // ⭐ HÀM CỤC BỘ: Dùng để đóng form loading an toàn và gọn gàng
            void DongFormLoadingAnToan()
            {
                if (isLoadShown)
                {
                    frmLoad.Close();
                    this.Enabled = true;
                    this.Focus();
                    isLoadShown = false; // Đảm bảo chỉ gọi đóng 1 lần duy nhất
                }
            }

            try
            {
                this.Enabled = false;
                frmLoad.Show(this);
                isLoadShown = true;
                await Task.Delay(50);

                int importedRows = 0;
                bool chiNapCotTrenUI = false;
                bool coDuLieuKhongHopLeTanBinh = false;

                await Task.Run(() =>
                {
                    using var wb = new XLWorkbook(filePath);

                    if (!wb.Worksheets.TryGetWorksheet("ThongKeThiDua", out IXLWorksheet ws))
                        throw new Exception("Không tìm thấy Sheet 'ThongKeThiDua'.");

                    int lastExcelCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
                    Dictionary<int, string> mappingCot = new Dictionary<int, string>();
                    int excelHeaderRow = -1;

                    for (int rowTest = 1; rowTest <= 10; rowTest++)
                    {
                        mappingCot.Clear();
                        for (int colExcel = 1; colExcel <= lastExcelCol; colExcel++)
                        {
                            string rawExcelHeader = ws.Cell(rowTest, colExcel).GetString();
                            string cleanExcel = ChuanHoa(rawExcelHeader);

                            if (cleanExcel == "stt") continue;

                            foreach (string dbColName in tatCaCotDb)
                            {
                                string expectedHeader = LấyTênHeaderMongMuốn(dbColName);
                                if (cleanExcel == ChuanHoa(expectedHeader))
                                {
                                    mappingCot[colExcel] = dbColName;
                                    break;
                                }
                            }
                        }

                        if (mappingCot.Count >= 5)
                        {
                            excelHeaderRow = rowTest;
                            break;
                        }
                    }

                    if (excelHeaderRow == -1 || mappingCot.Count == 0)
                    {
                        throw new Exception("Không tìm thấy cấu trúc bảng hợp lệ. Tệp này không khớp với cả phiên bản cũ và mới.");
                    }

                    if (excelHeaderRow == 1)
                    {
                        chiNapCotTrenUI = true;
                    }
                    else
                    {
                        bool hasValidVersion = false;
                        string decryptedVersionText = "";

                        // ƯU TIÊN 1: KIỂM TRA CHUẨN MỚI (Ô B9 TRONG SHEET THONG_TIN)
                        if (wb.Worksheets.TryGetWorksheet("THONG_TIN", out IXLWorksheet wsThongTin))
                        {
                            string encryptedV2 = wsThongTin.Cell("B9").GetString().Trim();
                            if (!string.IsNullOrEmpty(encryptedV2))
                            {
                                try
                                {
                                    decryptedVersionText = BaoMatAES.GiaiMa(encryptedV2);
                                    hasValidVersion = true;
                                }
                                catch { /* Nuốt lỗi để tự động chuyển sang cách cũ */ }
                            }
                        }

                        // ƯU TIÊN 2: NẾU CHUẨN MỚI THẤT BẠI -> QUÉT CHUẨN CŨ (SHEET PHIÊN BẢN)
                        if (!hasValidVersion && wb.Worksheets.TryGetWorksheet("Phiên bản", out IXLWorksheet wsVersion))
                        {
                            string encryptedV1 = wsVersion.Cell("B1").GetString().Trim();
                            if (!string.IsNullOrEmpty(encryptedV1))
                            {
                                try
                                {
                                    decryptedVersionText = BaoMatAES.GiaiMa(encryptedV1);
                                    hasValidVersion = true;
                                }
                                catch { }
                            }
                        }

                        // KIỂM SOÁT KẾT QUẢ
                        if (hasValidVersion)
                        {
                            bool isExcelTanBinh = decryptedVersionText.Contains("Tân binh", StringComparison.OrdinalIgnoreCase);
                            bool isExcelCBCS = decryptedVersionText.Contains("CBCS", StringComparison.OrdinalIgnoreCase);

                            if (laTanBinh && !isExcelTanBinh) throw new Exception($"Bạn đang cố nạp sai File! Tệp Excel này là: {decryptedVersionText}");
                            if (!laTanBinh && !isExcelCBCS) throw new Exception($"Bạn đang cố nạp sai File! Tệp Excel này là: {decryptedVersionText}");
                        }
                        else
                        {
                            // Nếu file Excel bị xóa sạch các sheet bảo vệ -> Lùi về chế độ Nạp Tương Thích (Quét tên cột)
                            chiNapCotTrenUI = true;
                        }
                    }

                    if (chiNapCotTrenUI)
                    {
                        var tempMapping = new Dictionary<int, string>();
                        foreach (var kvp in mappingCot)
                        {
                            if (danhSachCotTrenUI.Contains(kvp.Value, StringComparer.OrdinalIgnoreCase))
                            {
                                tempMapping.Add(kvp.Key, kvp.Value);
                            }
                        }
                        mappingCot = tempMapping;

                        if (mappingCot.Count == 0)
                        {
                            throw new Exception("File Excel không chứa dữ liệu của các cột đang hiển thị trên màn hình hiện tại.");
                        }
                    }

                    using var cn = new SqliteConnection($"Data Source={_csdl4Path}");
                    cn.Open();
                    using (var cmdPragma = new SqliteCommand("PRAGMA foreign_keys = OFF;", cn)) { cmdPragma.ExecuteNonQuery(); }
                    using var tran = cn.BeginTransaction();

                    try
                    {
                        using (var cmdDelete = new SqliteCommand($"DELETE FROM {tenBang}", cn, tran)) { cmdDelete.ExecuteNonQuery(); }
                        using (var cmdResetSeq = new SqliteCommand($"DELETE FROM sqlite_sequence WHERE name='{tenBang}';", cn, tran)) { cmdResetSeq.ExecuteNonQuery(); }

                        var thucTeCoTrongExcel = mappingCot.Values.ToList();
                        string columnsString = string.Join(", ", thucTeCoTrongExcel);
                        string paramsString = string.Join(", ", thucTeCoTrongExcel.Select(c => "@" + c));
                        string insertQuery = $"INSERT INTO {tenBang} ({columnsString}) VALUES ({paramsString})";

                        using (var cmdInsert = new SqliteCommand(insertQuery, cn, tran))
                        {
                            foreach (var col in thucTeCoTrongExcel)
                            {
                                cmdInsert.Parameters.Add(new SqliteParameter("@" + col, DBNull.Value));
                            }

                            int lastRow = ws.LastRowUsed()?.RowNumber() ?? excelHeaderRow;
                            int dongTrongLienTiep = 0;

                            for (int r = excelHeaderRow + 1; r <= lastRow; r++)
                            {
                                string oDauTien = ws.Cell(r, 1).GetString().Trim();

                                if (oDauTien.StartsWith("Tổng cộng:", StringComparison.OrdinalIgnoreCase)) break;

                                var hoTenEntry = mappingCot.FirstOrDefault(x => x.Value.Equals("HoVaTen", StringComparison.OrdinalIgnoreCase));
                                bool hoTenTrong = hoTenEntry.Key != 0 && string.IsNullOrWhiteSpace(ws.Cell(r, hoTenEntry.Key).GetString());

                                if (string.IsNullOrWhiteSpace(oDauTien) && hoTenTrong)
                                {
                                    dongTrongLienTiep++;
                                    if (dongTrongLienTiep >= 5) break;
                                    continue;
                                }
                                dongTrongLienTiep = 0;

                                if (hoTenTrong) continue;

                                foreach (var kvp in mappingCot)
                                {
                                    int excelColIndex = kvp.Key;
                                    string dbColName = kvp.Value;
                                    string cellValue = ws.Cell(r, excelColIndex).GetString().Trim();

                                    if (laTanBinh && (dbColName.StartsWith("Tuan_", StringComparison.OrdinalIgnoreCase) || dbColName.StartsWith("Thang_", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        if (!string.IsNullOrEmpty(cellValue) && cellValue != "1" && cellValue != "2" && cellValue != "3" && cellValue != "4")
                                        {
                                            cellValue = "";
                                            coDuLieuKhongHopLeTanBinh = true;
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(cellValue) &&
                                        (dbColName.Equals("HoVaTen", StringComparison.OrdinalIgnoreCase) ||
                                         dbColName.Equals("SoHieu", StringComparison.OrdinalIgnoreCase) ||
                                         dbColName.Equals("DonVi", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        // Cắt bỏ mọi khoảng trắng vô hình ở đầu/cuối từ Excel trước khi xử lý
                                        cellValue = cellValue.Trim();

                                        if (!cellValue.StartsWith("v2|", StringComparison.Ordinal))
                                        {
                                            cellValue = BaoMatAES.MaHoa(cellValue);
                                        }
                                    }
                                    cmdInsert.Parameters["@" + dbColName].Value = string.IsNullOrEmpty(cellValue) ? DBNull.Value : (object)cellValue;
                                }

                                cmdInsert.ExecuteNonQuery();
                                importedRows++;
                            }
                        }

                        tran.Commit();
                        using (var cmdVacuum = new SqliteCommand("VACUUM;", cn)) { cmdVacuum.ExecuteNonQuery(); }
                    }
                    catch (Exception exTran)
                    {
                        tran.Rollback();
                        throw new Exception("Lỗi khi ghi dữ liệu vào SQLite: " + exTran.Message);
                    }
                });

                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Nạp dữ liệu từ Excel", $"Đã nạp {importedRows} dòng vào bảng {tenBang}");
                await DieuPhoiLoadDuLieuAsync(laTanBinh);
                ApplyFilter();

                // ⭐ ĐÃ SỬA UX: ĐÓNG FORM LOADING TRƯỚC KHI HIỂN THỊ THÔNG BÁO
                DongFormLoadingAnToan();

                if (coDuLieuKhongHopLeTanBinh)
                {
                    MessageBox.Show("Phát hiện dữ liệu không hợp lệ tại các cột Đánh giá của Tân binh (chỉ chấp nhận 1, 2, 3, 4 hoặc rỗng).\n\nHệ thống đã tự động dọn dẹp và chuyển các ô nhập sai thành rỗng!", "Cảnh báo dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                string thongBao = chiNapCotTrenUI
                    ? $"Nạp thành công {importedRows} dòng!\n(Chế độ tương thích: Đã lọc bỏ các cột không sử dụng)"
                    : $"Nạp thành công {importedRows} dòng dữ liệu từ Excel vào phần mềm!";

                Module_ThongBao.ThanhCong(thongBao);
            }
            catch (Exception ex)
            {
                // ⭐ ĐÃ SỬA UX: ĐÓNG FORM LOADING TRƯỚC KHI BÁO LỖI
                // gọi là thừa - DongFormLoadingAnToan();
                MessageBox.Show(ex.Message, "Lỗi Nạp Dữ Liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // ⭐ Dự phòng: Đề phòng trường hợp có lỗi văng ra trước khi kịp gọi DongFormLoadingAnToan
                DongFormLoadingAnToan();
            }
        }
        private async void xoaTatCaDuLieu_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // =========================================================================
            // 0. KIỂM TRA CÓ DỮ LIỆU ĐỂ XÓA HAY KHÔNG TRƯỚC KHI GỌI FORM
            // =========================================================================
            try
            {
                if (!System.IO.File.Exists(_csdl4Path))
                {
                    MessageBox.Show("Không tìm thấy tệp cơ sở dữ liệu!", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
                bool laTanBinh = phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                string tenBang = laTanBinh ? "ThiDuaThang_TanBinh" : "ThiDuaThang";
                int tongSoDong = 0;

                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_csdl4Path}"))
                {
                    conn.Open();
                    using (var cmd = new Microsoft.Data.Sqlite.SqliteCommand($"SELECT COUNT(*) FROM {tenBang}", conn))
                    {
                        tongSoDong = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }

                // NẾU KHÔNG CÓ DỮ LIỆU -> BÁO LỖI VÀ THOÁT LUÔN
                if (tongSoDong == 0)
                {
                    MessageBox.Show("Hiện tại không có dữ liệu thống kê nào để xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kiểm tra dữ liệu trước khi xóa: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // =========================================================================
            // 1. Xác minh quyền Admin (Giữ nguyên lớp bảo vệ này)
            // =========================================================================
            using (Form24_XacMinhAdmin frmXacMinh = new Form24_XacMinhAdmin())
            {
                frmXacMinh.TopMost = true;
                frmXacMinh.StartPosition = FormStartPosition.CenterScreen;
                if (frmXacMinh.ShowDialog() != DialogResult.OK) return;
            }

            // =========================================================================
            // 2. Khởi tạo Form Xóa Nâng Cao (Form 37)
            // =========================================================================
            // Truyền chính Form15 (this) vào để Form 37 có thể gọi các hàm ReloadData, ApplyFilter...
            using (Form37_XoaThongKeNangCao frm37 = new Form37_XoaThongKeNangCao(this))
            {
                frm37.StartPosition = FormStartPosition.CenterParent;
                frm37.ShowDialog(); // Mở form xác nhận các tùy chọn xóa
            }
        }
        // 1. Cờ trạng thái an toàn tuyệt đối (Thread-safe Core)
        private int _dangXuLyLuongNen = 0;
        private int _currentProgress = 0;
        // 2. Hàm Progress SIÊU TỐC (Đã tăng tốc gấp ~6 lần)
        private async Task TangTienDoAsync(IProgress<int> progress, int from, int to)
        {
            if (progress == null) return;

            from = Math.Max(_currentProgress, Math.Max(0, Math.Min(100, from)));
            to = Math.Max(from, Math.Min(100, to));

            // Đổi i += 2 thành i += 5, và Delay 15ms thành 5ms. 
            // Tốc độ chạy sẽ cực nhanh nhưng mắt người vẫn thấy mượt.
            for (int i = from; i <= to; i += 5)
            {
                _currentProgress = i;
                progress.Report(i);
                await Task.Delay(5);
            }

            // Đảm bảo chốt hạ đúng số % mục tiêu (vì i+=5 có thể bị lệch số lẻ)
            if (_currentProgress != to)
            {
                _currentProgress = to;
                progress.Report(to);
            }
        }
        // 3. Logic làm mới bộ lọc 
        private void LamMoiBoLocCore()
        {
            _dangSetPlaceholder = true;
            InitPlaceholderTimKiem();

            comboBox_TimKiemDonVi.SelectedIndexChanged -= comboBox_TimKiemDonVi_SelectedIndexChanged;
            comboBox1_TinhTrang.SelectedIndexChanged -= comboBox1_TinhTrang_SelectedIndexChanged;

            comboBox_TimKiemDonVi.SelectedItem = "Tất cả";
            comboBox1_TinhTrang.SelectedItem = "Tất cả";

            comboBox_TimKiemDonVi.SelectedIndexChanged += comboBox_TimKiemDonVi_SelectedIndexChanged;
            comboBox1_TinhTrang.SelectedIndexChanged += comboBox1_TinhTrang_SelectedIndexChanged;

            _dangSetPlaceholder = false;
            ApplyFilter();
        }
        // 4. Core xử lý Data: CHẠY SONG SONG UI VÀ DB
        private async Task<bool> DongBoVaTaiDuLieuCoreAsync(IProgress<int> progress = null)
        {
            bool laTanBinh = false;

            // 🔥 KỸ THUẬT SONG SONG: 
            // Lệnh này bắt đầu vẽ Progress Bar từ 0 -> 30% NHƯNG KHÔNG BỊ CHẶN LẠI (không có await ở đây)
            Task uiTask = TangTienDoAsync(progress, 0, 30);

            // Lệnh này đồng thời lao đi query DB
            Task dbTask = Task.Run(() =>
            {
                try
                {
                    CapNhatThongKe();
                    laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    throw new Exception("Lỗi khi đồng bộ dữ liệu nền với CSDL.", ex);
                }
            });

            // Ép hệ thống gom 2 luồng lại: Chờ CẢ DB VÀ Progress vẽ xong 30% mới đi tiếp
            // (Nếu DB cực nhanh, nó sẽ đợi UI vẽ cho xong 30% để nhìn cho đẹp. Nếu DB chậm, UI vẽ xong 30% sẽ đứng im đợi DB).
            await Task.WhenAll(uiTask, dbTask);

            // DB đã xong, kéo nhanh Progress lên 70%
            await TangTienDoAsync(progress, 30, 70);

            return laTanBinh;
        }
        // 5. BỘ ĐIỀU PHỐI (Controller)
        private async void lamMoi_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref _dangXuLyLuongNen, 1) == 1) return;

            try
            {
                _currentProgress = 0;
                toolStripProgressBar1_LamMoi.Minimum = 0;
                toolStripProgressBar1_LamMoi.Maximum = 100;
                toolStripProgressBar1_LamMoi.Value = 0;
                // ⭐ ÉP KÍCH THƯỚC CỐ ĐỊNH TRƯỚC KHI HIỂN THỊ (CHỐNG PHÌNH TO)
                toolStripProgressBar1_LamMoi.AutoSize = false;
                toolStripProgressBar1_LamMoi.Size = new Size(150, 18); // Cố định chiều rộng 150px, cao 18px (tùy chỉnh cho vừa mắt)
                // Nếu ProgressBar nằm trên StatusStrip và muốn nó nằm gọn gàng, có thể dùng thêm Margin
                // toolStripProgressBar1_LamMoi.Margin = new Padding(1, 2, 1, 1); 
                toolStripProgressBar1_LamMoi.Visible = true;
                this.UseWaitCursor = true;
                toolStripStatusLabel1.Text = "Đang làm mới dữ liệu...";

                // Khóa lưới
                kryptonDataGridView1.SuspendLayout();
                kryptonDataGridView1.Enabled = false;
                IProgress<int> progressReporter = new Progress<int>(percent =>
                {
                    toolStripProgressBar1_LamMoi.Value = Math.Max(0, Math.Min(100, percent));
                });
                progressReporter.Report(5);
                LamMoiBoLocCore();
                // Lấy Data
                bool laTanBinh = await DongBoVaTaiDuLieuCoreAsync(progressReporter);
                // Vẽ UI
                await TangTienDoAsync(progressReporter, 70, 100);
                await DieuPhoiLoadDuLieuAsync(laTanBinh);
                LoadComboBoxDonVi();
                // Hoàn tất
                ApplyFilter(); // ⭐ ĐỒNG CHÍ THÊM DÒNG NÀY VÀO NHÉ (Nó báo cho Grid biết để cập nhật số lượng dòng)
                await TangTienDoAsync(progressReporter, 85, 100);
                toolStripStatusLabel1.Text = $"Tổng cộng: {_filteredIndexes?.Count ?? 0} đồng chí";

                // Giảm delay chốt hạ từ 300ms xuống 150ms để user không cảm thấy bị "kẹt" ở lúc xong
                await Task.Delay(150);
            }
            catch (Exception ex)
            {
                toolStripProgressBar1_LamMoi.Value = 0;
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Lỗi Làm mới (F5)", ex.ToString());

                MessageBox.Show(
                    "Đã xảy ra lỗi hệ thống khi làm mới dữ liệu!\nChi tiết đã được ghi vào Nhật ký.",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                toolStripProgressBar1_LamMoi.Visible = false;
                toolStripProgressBar1_LamMoi.Value = 0;
                _currentProgress = 0;
                kryptonDataGridView1.Enabled = true;
                kryptonDataGridView1.ResumeLayout();
                this.UseWaitCursor = false;
                Interlocked.Exchange(ref _dangXuLyLuongNen, 0);
            }
        }
    }
} /// Ngoài luồng