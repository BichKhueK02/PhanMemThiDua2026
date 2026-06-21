using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;

namespace PhanMemThiDua2026
{
    public partial class Form26_PhanTichQuanSo : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;

        private int _tongQuanSo;
        private DataTable _dtBieuDo;
        private int _syQuan;
        private int _haSyQuan;
        private int _chienSi;
        public Form26_PhanTichQuanSo()
        {
            InitializeComponent();
            this.Load += Form26_PhanTichQuanSo_Load;

            // THÊM DÒNG NÀY ĐỂ KÍCH HOẠT SỰ KIỆN VẼ BIỂU ĐỒ:
            tableLayoutPanel2.Paint += tableLayoutPanel2_Paint;

            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ShowInTaskbar = false;
            InitToolTips();

            typeof(TableLayoutPanel).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(tableLayoutPanel2, true, null);
        }
        private void Form26_PhanTichQuanSo_Load(object sender, EventArgs e)
        {
            // kryptonDataGridView1.Dock = DockStyle.Fill; <--- BỎ DÒNG NÀY ĐI
            ChuanHoaDataGridView();
            CapNhatPhienBanPhanMem();
            label_DonVi.Text = "ĐƠN VỊ: " + Module_TaiKhoan.XacDinhTenTieuDoan();
            statusStrip1.SizingGrip = false;
        }
        // HÀM NÀY ĐƯỢC FORM 6 GỌI ĐỂ ÉP LẤY DỮ LIỆU TƯƠI TỪ DB
        public void LoadDataTuDatabase()
        {
            _tongQuanSo = 0; _syQuan = 0; _haSyQuan = 0; _chienSi = 0;
            DataTable dtKetQua = TaoBangPhanTich();

            if (!File.Exists(_csdl2Path)) return;

            using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
            conn.Open();

            var thongKeDic = new Dictionary<string, (int tong, int syQuan, int haSyQuan, int chienSi)>(StringComparer.OrdinalIgnoreCase);

            using var cmd = new SqliteCommand("SELECT DonVi, CapBac FROM DanhSach", conn);
            using var rd = cmd.ExecuteReader();

            while (rd.Read())
            {
                string rawDonVi = rd.IsDBNull(0) ? "" : rd.GetString(0);
                string rawCapBac = rd.IsDBNull(1) ? "" : rd.GetString(1);

                string tenDonVi = BaoMatAES.GiaiMa(rawDonVi).Trim();
                if (string.IsNullOrEmpty(tenDonVi) && !string.IsNullOrEmpty(rawDonVi)) tenDonVi = rawDonVi.Trim();

                string capBac = BaoMatAES.GiaiMa(rawCapBac).Trim().ToUpper();
                if (string.IsNullOrEmpty(capBac) && !string.IsNullOrEmpty(rawCapBac)) capBac = rawCapBac.Trim().ToUpper();

                if (string.IsNullOrEmpty(tenDonVi)) continue;

                if (!thongKeDic.ContainsKey(tenDonVi))
                    thongKeDic[tenDonVi] = (0, 0, 0, 0);

                var data = thongKeDic[tenDonVi];
                data.tong++;

                // Phân loại cấp bậc (Bao gồm H, B, T, U, Đ)
                if (capBac is "H1" or "B1" or "B2")
                    data.chienSi++;
                else if (capBac is "H2" or "H3")
                    data.haSyQuan++;
                else if (capBac.StartsWith("T") || capBac.StartsWith("U"))
                    data.syQuan++;

                thongKeDic[tenDonVi] = data;
            }

            // Đổ vào bảng hiển thị
            List<string> danhSachDonVi = Module_DonVi.GetDanhSachDonVi();
            int stt = 1;

            foreach (string donVi in danhSachDonVi)
            {
                if (thongKeDic.TryGetValue(donVi, out var kq) && kq.tong > 0)
                {
                    _tongQuanSo += kq.tong;
                    _syQuan += kq.syQuan;
                    _haSyQuan += kq.haSyQuan;
                    _chienSi += kq.chienSi;

                    dtKetQua.Rows.Add(stt++, donVi, kq.tong, kq.syQuan, kq.haSyQuan, kq.chienSi);
                }
            }

            ThemDongTongCong(dtKetQua);

            // Gán lại cho lưới
            kryptonDataGridView1.DataSource = null;
            kryptonDataGridView1.DataSource = dtKetQua;
            ChuanHoaDataGridView();
            // Cập nhật nhãn
            toolStripStatusLabel1.Text = $"Tổng quân số: {_tongQuanSo} đồng chí.";
            // TRUYỀN DỮ LIỆU VÀ YÊU CẦU VẼ LẠI BẢNG GÓC DƯỚI
            _dtBieuDo = dtKetQua;
            tableLayoutPanel2.Invalidate();
        }
        private DataTable TaoBangPhanTich()
        {
            return new DataTable
            {
                Columns =
                {
                    new DataColumn("STT", typeof(int)),
                    new DataColumn("DonVi", typeof(string)),
                    new DataColumn("TongQuanSo", typeof(int)),
                    new DataColumn("SyQuan", typeof(int)),
                    new DataColumn("HaSyQuan", typeof(int)),
                    new DataColumn("ChienSiNghiaVu", typeof(int))
                }
            };
        }
        private void ThemDongTongCong(DataTable dt)
        {
            var r = dt.NewRow();
            r["STT"] = DBNull.Value;
            r["DonVi"] = "Tổng cộng";
            r["TongQuanSo"] = _tongQuanSo;
            r["SyQuan"] = _syQuan;
            r["HaSyQuan"] = _haSyQuan;
            r["ChienSiNghiaVu"] = _chienSi;
            dt.Rows.Add(r);
        }
        private static readonly Font HeaderFont = new Font("Segoe UI", 11F, FontStyle.Bold);
        private static readonly Font CellFont = new Font("Segoe UI", 10F, FontStyle.Regular);
        private void ChuanHoaDataGridView()
        {
            var grid = kryptonDataGridView1;

            if (grid == null || grid.IsDisposed)
                return;
            // =========================================================
            // 🚀 TĂNG TỐC RENDER & GIẢM GIẬT
            // =========================================================
            grid.SuspendLayout();

            try
            {
                // -----------------------------------------------------
                // DOUBLE BUFFER (Giảm giật khi scroll)
                // -----------------------------------------------------

                typeof(DataGridView)
                    .GetProperty(
                        "DoubleBuffered",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(grid, true, null);

                // =====================================================
                // CẤU HÌNH CHUNG
                // =====================================================

                grid.ReadOnly = true;

                grid.AllowUserToAddRows = false;
                grid.AllowUserToDeleteRows = false;
                grid.AllowUserToResizeRows = false;

                grid.MultiSelect = false;

                grid.SelectionMode =
                    DataGridViewSelectionMode.FullRowSelect;

                grid.AutoSizeColumnsMode =
                    DataGridViewAutoSizeColumnsMode.Fill;

                grid.BorderStyle = BorderStyle.None;

                grid.CellBorderStyle =
                    DataGridViewCellBorderStyle.SingleHorizontal;

                grid.GridColor = Color.LightGray;

                grid.BackgroundColor = Color.White;

                // =====================================================
                // HEADER
                // =====================================================

                grid.EnableHeadersVisualStyles = false;

                grid.ColumnHeadersBorderStyle =
                    DataGridViewHeaderBorderStyle.Single;

                // 🚀 Header cao đẹp hơn
                grid.ColumnHeadersHeightSizeMode =
                    DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

                grid.ColumnHeadersHeight = 52;

                var headerStyle = grid.ColumnHeadersDefaultCellStyle;

                headerStyle.Font = HeaderFont;

                headerStyle.Alignment =
                    DataGridViewContentAlignment.MiddleCenter;

                headerStyle.BackColor =
                    Color.FromArgb(245, 247, 250);

                headerStyle.ForeColor = Color.Black;

                // 🚀 Padding giúp tiêu đề "thở"
                headerStyle.Padding = new Padding(8, 4, 8, 4);

                headerStyle.WrapMode =
                    DataGridViewTriState.True;

                // =====================================================
                // CELL STYLE
                // =====================================================

                var cellStyle = grid.DefaultCellStyle;

                cellStyle.Font = CellFont;

                cellStyle.ForeColor = Color.Black;

                cellStyle.BackColor = Color.White;

                cellStyle.Alignment =
                    DataGridViewContentAlignment.MiddleCenter;

                cellStyle.SelectionBackColor =
                    Color.FromArgb(220, 235, 252);

                cellStyle.SelectionForeColor =
                    Color.Black;

                cellStyle.Padding = new Padding(3);

                // =====================================================
                // DÒNG XEN KẼ
                // =====================================================

                grid.AlternatingRowsDefaultCellStyle.BackColor =
                    Color.FromArgb(248, 250, 252);

                // =====================================================
                // CHIỀU CAO DÒNG
                // =====================================================

                grid.RowTemplate.Height = 34;

                // =====================================================
                // TẮT SORT
                // =====================================================

                foreach (DataGridViewColumn col in grid.Columns)
                {
                    col.SortMode =
                        DataGridViewColumnSortMode.NotSortable;

                    col.DefaultCellStyle.Font = CellFont;
                }

                // =====================================================
                // CẤU HÌNH CỘT
                // =====================================================

                if (grid.Columns.Contains("STT"))
                {
                    var col = grid.Columns["STT"];

                    col.Width = 60;

                    col.MinimumWidth = 60;

                    col.AutoSizeMode =
                        DataGridViewAutoSizeColumnMode.None;

                    col.HeaderText = "STT";
                }

                if (grid.Columns.Contains("DonVi"))
                {
                    grid.Columns["DonVi"].HeaderText = "Đơn vị";

                    grid.Columns["DonVi"]
                        .DefaultCellStyle.Alignment =
                        DataGridViewContentAlignment.MiddleLeft;
                }

                if (grid.Columns.Contains("TongQuanSo"))
                    grid.Columns["TongQuanSo"].HeaderText =
                        "Tổng quân số";

                if (grid.Columns.Contains("SyQuan"))
                    grid.Columns["SyQuan"].HeaderText =
                        "Sỹ quan";

                if (grid.Columns.Contains("HaSyQuan"))
                    grid.Columns["HaSyQuan"].HeaderText =
                        "Hạ sỹ quan";

                if (grid.Columns.Contains("ChienSiNghiaVu"))
                    grid.Columns["ChienSiNghiaVu"].HeaderText =
                        "Chiến sĩ nghĩa vụ";
            }
            finally
            {
                grid.ResumeLayout();
            }
        }
        private void btnThoat_Click(object sender, EventArgs e)
        {
            DongVaQuayLaiForm6();
        }
        private void kryptonButton1_Dong_Click(object sender, EventArgs e)
        {
            DongVaQuayLaiForm6();
        }
        private void DongVaQuayLaiForm6()
        {
            var panel = this.Parent as Panel;
            if (panel != null)
            {
                var form6 = panel.Controls.OfType<Form6_XuLyData>().FirstOrDefault();
                if (form6 != null)
                {
                    form6.Show();
                    form6.BringToFront();
                }
            }
            this.Hide();
        }
        private void CapNhatPhienBanPhanMem()
        {
            // =======================================================
            // 1. KIỂM TRA NHANH GIAO DIỆN (Early Exit)
            // =======================================================
            if (this.IsDisposed || toolStripStatusLabel2 == null || toolStripStatusLabel2.IsDisposed)
                return;

            string doiTuongDaGiaiMa = string.Empty;

            // =======================================================
            // 2. XỬ LÝ DỮ LIỆU ĐỘC LẬP VỚI GIAO DIỆN
            // =======================================================
            try
            {
                string fileCSDL2 = _csdl2Path?.Trim() ?? string.Empty;

                if (!string.IsNullOrEmpty(fileCSDL2) && File.Exists(fileCSDL2))
                {
                    using (var cn = new SqliteConnection($"Data Source={fileCSDL2};Mode=ReadOnly;Cache=Shared;"))
                    {
                        cn.Open();
                        using var cmd = cn.CreateCommand();

                        // Tối ưu hóa truy vấn
                        cmd.CommandText = "SELECT DoiTuong FROM PhienBan_DoiTuong WHERE ID = 1 LIMIT 1;";
                        object? result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            string duLieuMaHoa = result.ToString()?.Trim() ?? string.Empty;

                            if (!string.IsNullOrEmpty(duLieuMaHoa))
                            {
                                try
                                {
                                    doiTuongDaGiaiMa = BaoMatAES.GiaiMa(duLieuMaHoa)?.Trim() ?? string.Empty;
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"[Lỗi giải mã AES]: {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Gộp chung Catch để log lỗi, không can thiệp UI ở đây
                Debug.WriteLine($"[Lỗi truy xuất CSDL CapNhatPhienBanPhanMem]: {ex.Message}");
            }

            // =======================================================
            // 3. CHUẨN HÓA DỮ LIỆU ĐẦU RA (Fallback & Truncate)
            // =======================================================
            if (string.IsNullOrWhiteSpace(doiTuongDaGiaiMa))
            {
                doiTuongDaGiaiMa = "(không xác định)";
            }
            else if (doiTuongDaGiaiMa.Length > 80)
            {
                // Chuẩn UX: Cắt chuỗi và thêm dấu "..." để hiển thị chuyên nghiệp hơn
                doiTuongDaGiaiMa = doiTuongDaGiaiMa.Substring(0, 77) + "...";
            }

            string finalString = $"Phần mềm: {doiTuongDaGiaiMa}";

            // =======================================================
            // 4. CẬP NHẬT GIAO DIỆN AN TOÀN (Thread-Safe & Lifecycle-Safe)
            // =======================================================
            SafeUpdateStatusLabel(finalString);
        }
        // Hàm hỗ trợ chuyên dụng để cập nhật UI an toàn
        private void SafeUpdateStatusLabel(string text)
        {
            // Kéo chốt an toàn lần 2: Tránh trường hợp Form bị đóng ngay khi SQLite vừa chạy xong
            if (this.IsDisposed || toolStripStatusLabel2 == null || toolStripStatusLabel2.IsDisposed)
                return;

            // Chống lỗi Cross-thread: Nếu hàm bị gọi từ luồng khác, đẩy nó về lại luồng chính (UI Thread)
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => SafeUpdateStatusLabel(text)));
                return;
            }

            toolStripStatusLabel2.Spring = true;
            toolStripStatusLabel2.TextAlign = ContentAlignment.MiddleRight;
            toolStripStatusLabel2.Text = text;
        }
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Thao tác";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;

            if (kryptonButton1_Dong != null)
                toolTip1.SetToolTip(kryptonButton1_Dong, "Đóng cửa sổ này và quay lại màn hình trước!");
        }
        private void tableLayoutPanel2_Paint(object sender, PaintEventArgs e)
        {
            if (_dtBieuDo == null || _dtBieuDo.Rows.Count == 0) return;

            try
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // 1. TÍNH TOÁN TỌA ĐỘ KHUNG VẼ
                int[] colWidths = tableLayoutPanel2.GetColumnWidths();
                int[] rowHeights = tableLayoutPanel2.GetRowHeights();

                if (colWidths.Length < 1 || rowHeights.Length < 3) return;

                int cellX = 0;
                int cellY = rowHeights[0] + rowHeights[1];
                int cellWidth = colWidths[0];
                int cellHeight = rowHeights[2];

                Rectangle rect = new Rectangle(cellX, cellY, cellWidth, cellHeight);
                if (rect.Width <= 0 || rect.Height <= 0) return;

                int paddingX = 50;
                int paddingY = 40;
                int drawWidth = rect.Width - paddingX * 2;
                int drawHeight = rect.Height - paddingY * 2 - 20;
                int bottomY = rect.Y + rect.Height - paddingY;

                if (drawWidth <= 0 || drawHeight <= 0) return;

                // 2. TÌM ĐỈNH BIỂU ĐỒ (MAX)
                int maxQuanSo = 1;
                int soDonVi = 0;

                foreach (DataRow row in _dtBieuDo.Rows)
                {
                    if (row["DonVi"].ToString() == "Tổng cộng") continue;
                    soDonVi++;

                    int sq = row["SyQuan"] != DBNull.Value ? Convert.ToInt32(row["SyQuan"]) : 0;
                    int hsq = row["HaSyQuan"] != DBNull.Value ? Convert.ToInt32(row["HaSyQuan"]) : 0;
                    int cs = row["ChienSiNghiaVu"] != DBNull.Value ? Convert.ToInt32(row["ChienSiNghiaVu"]) : 0;

                    int maxCuaDonVi = Math.Max(sq, Math.Max(hsq, cs));
                    if (maxCuaDonVi > maxQuanSo) maxQuanSo = maxCuaDonVi;
                }

                if (soDonVi == 0) return;

                int step = (maxQuanSo / 5) + 1;
                if (step < 1) step = 1;
                int gridMax = step * 6;

                // 3. KHAI BÁO CỌ VẼ
                using (Pen penAxis = new Pen(Color.FromArgb(150, 150, 150), 2))
                using (Pen penGrid = new Pen(Color.FromArgb(220, 220, 220), 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                using (Brush bSyQuan = new SolidBrush(Color.FromArgb(33, 150, 243)))    // Xanh dương dịu
                using (Brush bHaSyQuan = new SolidBrush(Color.FromArgb(255, 152, 0)))   // Cam nghệ
                using (Brush bChienSi = new SolidBrush(Color.FromArgb(76, 175, 80)))    // Xanh lá cây nhạt
                //FromArgb(39, 174, 96)
                using (Brush bText = new SolidBrush(Color.FromArgb(60, 60, 60)))
                using (Font fontText = new Font("Segoe UI", 9))
                using (Font fontSo = new Font("Segoe UI", 8, FontStyle.Bold))
                using (StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Far })
                {
                    // --- VẼ LƯỚI NGANG ---
                    for (int i = 0; i <= 5; i++)
                    {
                        int gridVal = step * i;
                        int yPos = bottomY - (int)((gridVal / (float)gridMax) * drawHeight);

                        g.DrawLine(penGrid, rect.X + paddingX, yPos, rect.X + rect.Width - paddingX, yPos);

                        if (i > 0)
                        {
                            g.DrawString(gridVal.ToString(), fontText, bText, rect.X + paddingX - 30, yPos - 7);
                        }
                    }

                    g.DrawLine(penAxis, rect.X + paddingX, bottomY, rect.X + rect.Width - paddingX, bottomY);

                    // 4. THUẬT TOÁN DỒN CỘT ĐỘNG (DYNAMIC SHIFTING)
                    // 4. THUẬT TOÁN DỒN CỘT ĐỘNG VÀ TỐI ƯU CHIỀU RỘNG
                    int groupWidth = drawWidth / soDonVi;

                    // Tính toán độ rộng cột (chiếm 75% không gian)
                    int barWidth = (int)((groupWidth * 0.75f) / 3);

                    // KIỂM TRA ĐIỀU KIỆN TỐI ƯU CHIỀU NGANG:
                    // Nếu có từ 4 đơn vị trở xuống, cho phép cột mập lên tối đa 80px (hoặc 100px tùy bạn chỉnh)
                    // Nếu nhiều hơn 4 đơn vị, ép về 35px để tránh dính chùm
                    int maxBarWidth = (soDonVi <= 4) ? 80 : 35;

                    if (barWidth > maxBarWidth) barWidth = maxBarWidth;
                    if (barWidth < 5) barWidth = 5; // Vẫn giữ chốt chặn độ ốm tối thiểu

                    int currentIndex = 0;

                    foreach (DataRow row in _dtBieuDo.Rows)
                    {
                        string donVi = row["DonVi"].ToString();
                        if (donVi == "Tổng cộng") continue;

                        int sq = row["SyQuan"] != DBNull.Value ? Convert.ToInt32(row["SyQuan"]) : 0;
                        int hsq = row["HaSyQuan"] != DBNull.Value ? Convert.ToInt32(row["HaSyQuan"]) : 0;
                        int cs = row["ChienSiNghiaVu"] != DBNull.Value ? Convert.ToInt32(row["ChienSiNghiaVu"]) : 0;

                        int groupX = rect.X + paddingX + (currentIndex * groupWidth);

                        // Đếm số lượng cột thực tế có dữ liệu
                        int activeColumns = 0;
                        if (sq > 0) activeColumns++;
                        if (hsq > 0) activeColumns++;
                        if (cs > 0) activeColumns++;

                        if (activeColumns > 0)
                        {
                            // Tính tổng chiều rộng của khối cột thực tế và lấy tọa độ căn giữa khối đó
                            int totalActiveWidth = activeColumns * barWidth;
                            int currentX = groupX + (groupWidth - totalActiveWidth) / 2;

                            // --- VẼ CỘT SỸ QUAN (Nếu có) ---
                            if (sq > 0)
                            {
                                int hSqPixel = (int)((sq / (float)gridMax) * drawHeight);
                                Rectangle barRect = new Rectangle(currentX, bottomY - hSqPixel, barWidth, hSqPixel);
                                g.FillRectangle(bSyQuan, barRect);
                                g.DrawString(sq.ToString(), fontSo, bText, new RectangleF(currentX, bottomY - hSqPixel - 20, barWidth, 20), sfCenter);

                                currentX += barWidth; // Đẩy tọa độ X sang phải để nhường chỗ cho cột tiếp theo
                            }

                            // --- VẼ CỘT HẠ SỸ QUAN (Nếu có) ---
                            if (hsq > 0)
                            {
                                int hHsqPixel = (int)((hsq / (float)gridMax) * drawHeight);
                                Rectangle barRect = new Rectangle(currentX, bottomY - hHsqPixel, barWidth, hHsqPixel);
                                g.FillRectangle(bHaSyQuan, barRect);
                                g.DrawString(hsq.ToString(), fontSo, bText, new RectangleF(currentX, bottomY - hHsqPixel - 20, barWidth, 20), sfCenter);

                                currentX += barWidth; // Đẩy tọa độ X
                            }

                            // --- VẼ CỘT CHIẾN SĨ (Nếu có) ---
                            if (cs > 0)
                            {
                                int hCsPixel = (int)((cs / (float)gridMax) * drawHeight);
                                Rectangle barRect = new Rectangle(currentX, bottomY - hCsPixel, barWidth, hCsPixel);
                                g.FillRectangle(bChienSi, barRect);
                                g.DrawString(cs.ToString(), fontSo, bText, new RectangleF(currentX, bottomY - hCsPixel - 20, barWidth, 20), sfCenter);
                            }
                        }

                        // --- VẼ TÊN ĐƠN VỊ ---
                        RectangleF unitTextRect = new RectangleF(groupX, bottomY + 5, groupWidth, 20);
                        StringFormat sfUnit = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };
                        g.DrawString(donVi, fontText, bText, unitTextRect, sfUnit);
                        sfUnit.Dispose();

                        currentIndex++;
                    }

                    // 5. VẼ CHÚ THÍCH (LEGEND)
                    int legendY = rect.Y + 10;

                    g.FillRectangle(bSyQuan, rect.X + paddingX, legendY, 15, 15);
                    g.DrawString("Sỹ quan", fontText, bText, rect.X + paddingX + 20, legendY - 1);

                    g.FillRectangle(bHaSyQuan, rect.X + paddingX + 90, legendY, 15, 15);
                    g.DrawString("Hạ sỹ quan", fontText, bText, rect.X + paddingX + 110, legendY - 1);

                    g.FillRectangle(bChienSi, rect.X + paddingX + 200, legendY, 15, 15);
                    g.DrawString("Chiến sĩ nghĩa vụ", fontText, bText, rect.X + paddingX + 220, legendY - 1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Lỗi vẽ biểu đồ: {ex.Message}");
            }
        }
    }
}