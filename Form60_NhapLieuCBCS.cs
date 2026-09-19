using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
namespace PhanMemThiDua2026
{
    public partial class Form60_NhapLieuCBCS : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private string _connectionString;
        // Biến lưu trữ bảng dữ liệu gốc trong bộ nhớ để thực hiện Lọc Kép mượt mà
        private DataTable _dtGoc;
        private int _currentSelectedID = 0;
        private bool _dangXuLyHuongDan = false;
        private bool _coThayDoiDuLieu = false;
        private string _ketQuaDongBo = string.Empty;
        public Form60_NhapLieuCBCS()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            _connectionString = $"Data Source={_csdl2Path};";
            // Kiểm tra cấu trúc bảng
            KiemTraVaTaoBangDanhSachNhap();
            // Đăng ký sự kiện an toàn
            DangKyCacSuKien();
            CuaSo_KhoiTaoStatusStrip();
        }
        /// <summary>
        /// Gom nhóm đăng ký toàn bộ sự kiện của Form
        private void DangKyCacSuKien()
        {
            // Sự kiện vòng đời Form
            this.Shown -= Form60_NhapLieuCBCS_Shown;
            this.Shown += Form60_NhapLieuCBCS_Shown;
            this.FormClosed -= Form60_NhapLieuCBCS_FormClosed;
            this.FormClosed += Form60_NhapLieuCBCS_FormClosed;
            // Sự kiện ô Số hiệu
            textBox_SoHieu.KeyPress -= textBox_SoHieu_KeyPress;
            textBox_SoHieu.KeyPress += textBox_SoHieu_KeyPress;
            textBox_SoHieu.Leave -= textBox_SoHieu_Leave;
            textBox_SoHieu.Leave += textBox_SoHieu_Leave;
            // Sự kiện Lưới dữ liệu
            kryptonDataGridView1.CellClick -= KryptonDataGridView1_CellClick;
            kryptonDataGridView1.CellClick += KryptonDataGridView1_CellClick;
        }
        // Yêu mèo cam - Lê Trung Kiên
        private bool KiemTraLaTanBinh()
        {
            return Module_TaiKhoan.LayPhienBanPhanMem()
                .Contains("tân binh", StringComparison.OrdinalIgnoreCase);
        }
        private void Form60_NhapLieuCBCS_Load(object sender, EventArgs e)
        {
            try
            {
                // 1. Load các ComboBox trợ giúp
                LoadComboBoxDonVi();
                KhoiTaoCacControlThongMinh();
                CapNhatLabelTheoPhienBan();
                // 2. TỰ ĐỘNG XÓA BẢNG NHẬP VÀ NẠP LẠI TỪ BẢNG DANH SÁCH GỐC
                LamMoiVaSaoChepDuLieuGoc();
                // 3. Tải dữ liệu từ DanhSach_Nhap lên DataGridView
                LoadDuLieuLenGrid();
                // 4. Định dạng giao diện Lưới
                DinhDangDataGridViewHienDai();
                kryptonDataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                // 5. Gán sự kiện tìm kiếm & Đánh STT
                GanSuKienToanCuc();
                // 6. Cập nhật StatusStrip
                CapNhatThanhTrangThaiStatusStrip(kryptonDataGridView1.Rows.Count);
                // ⭐ TUYỆT CHIÊU TRỊ KRYPTON: Ép bảng màu ngay trước tích tắc Menu mở lên
                Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi khởi tạo dữ liệu Form: {ex.Message}", "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void Form60_NhapLieuCBCS_Shown(object sender, EventArgs e)
        {
            textBox_HoVaTen.Focus();
            InitToolTips();
        }
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            toolTip1.InitialDelay = 300;
            toolTip1.AutoPopDelay = 2500;
            toolTip1.ReshowDelay = 100;
            toolTip1.ShowAlways = true;
            var tips = new Dictionary<Control, string>
            {
                { kryptonButton_LamMoiCacOTimKiem, "Làm mới các ô tìm kiếm" },
                { kryptonButton_RefershCSDL, "Làm mới dữ liệu từ CSDL gốc" },
                { btnThemMoi, "Thêm mới CBCS" },
                { kryptonButton1_DiChuyenDataCBCSLen, "Di chuyển dữ liệu lên" },
                { kryptonButton1_DiChuyenDataCBCSXuong, "Di chuyển dữ liệu xuống" },
                { kryptonButton1_DongForm, "Đóng trang này và tự động đồng bộ dữ liệu vào Trang dữ liệu" }
            };
            foreach (var tip in tips)
            {
                if (tip.Key != null && !tip.Key.IsDisposed) toolTip1.SetToolTip(tip.Key, tip.Value);
            }
        }
        /// Nạp danh sách đơn vị từ Module_DonVi vào ComboBox tìm kiếm
        ///     /// <summary>
        /// Xóa toàn bộ dữ liệu ở bảng DanhSach_Nhap và nạp lại từ bảng DanhSach gốc
        private void LamMoiVaSaoChepDuLieuGoc()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            try
            {
                using (var cmdDelete = new SqliteCommand(
                    "DELETE FROM DanhSach_Nhap; DELETE FROM sqlite_sequence WHERE name='DanhSach_Nhap';",
                    conn, transaction))
                {
                    cmdDelete.ExecuteNonQuery();
                }
                const string insertSql = """
            INSERT INTO DanhSach_Nhap (
                STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND,
                CapBac, ChucVu, DonVi, PhanLoai, GhiChu, ThuTuSapXep,
                ThoiGianTao, ThoiGianCapNhat
            ) VALUES (
                $STT, $HoVaTen, $SoHieu, $NamSinh, $QueQuan, $NgayVaoCAND,
                $CapBac, $ChucVu, $DonVi, $PhanLoai, $GhiChu, $ThuTuSapXep,
                $ThoiGianTao, $ThoiGianCapNhat
            );
            """;
                using var connRead = new SqliteConnection(_connectionString);
                connRead.Open();
                using var cmdSelect = new SqliteCommand("""
            SELECT STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND,
                   CapBac, ChucVu, DonVi, PhanLoai, GhiChu
            FROM DanhSach
            ORDER BY STT;
            """, connRead);
                using var reader = cmdSelect.ExecuteReader();
                using var cmdInsert = new SqliteCommand(insertSql, conn, transaction);
                var pSTT = cmdInsert.Parameters.Add("$STT", SqliteType.Integer);
                var pHoVaTen = cmdInsert.Parameters.Add("$HoVaTen", SqliteType.Text);
                var pSoHieu = cmdInsert.Parameters.Add("$SoHieu", SqliteType.Text);
                var pNamSinh = cmdInsert.Parameters.Add("$NamSinh", SqliteType.Text);
                var pQueQuan = cmdInsert.Parameters.Add("$QueQuan", SqliteType.Text);
                var pNgayVaoCAND = cmdInsert.Parameters.Add("$NgayVaoCAND", SqliteType.Text);
                var pCapBac = cmdInsert.Parameters.Add("$CapBac", SqliteType.Text);
                var pChucVu = cmdInsert.Parameters.Add("$ChucVu", SqliteType.Text);
                var pDonVi = cmdInsert.Parameters.Add("$DonVi", SqliteType.Text);
                var pPhanLoai = cmdInsert.Parameters.Add("$PhanLoai", SqliteType.Text);
                var pGhiChu = cmdInsert.Parameters.Add("$GhiChu", SqliteType.Text);
                var pThuTuSapXep = cmdInsert.Parameters.Add("$ThuTuSapXep", SqliteType.Integer);
                var pThoiGianTao = cmdInsert.Parameters.Add("$ThoiGianTao", SqliteType.Text);
                var pThoiGianCapNhat = cmdInsert.Parameters.Add("$ThoiGianCapNhat", SqliteType.Text);
                pThoiGianTao.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                pThoiGianCapNhat.Value = DBNull.Value;
                int iSTT = reader.GetOrdinal("STT");
                int iHoVaTen = reader.GetOrdinal("HoVaTen");
                int iSoHieu = reader.GetOrdinal("SoHieu");
                int iNamSinh = reader.GetOrdinal("NamSinh");
                int iQueQuan = reader.GetOrdinal("QueQuan");
                int iNgayVaoCAND = reader.GetOrdinal("NgayVaoCAND");
                int iCapBac = reader.GetOrdinal("CapBac");
                int iChucVu = reader.GetOrdinal("ChucVu");
                int iDonVi = reader.GetOrdinal("DonVi");
                int iPhanLoai = reader.GetOrdinal("PhanLoai");
                int iGhiChu = reader.GetOrdinal("GhiChu");
                while (reader.Read())
                {
                    int stt = reader.IsDBNull(iSTT) ? 0 : reader.GetInt32(iSTT);
                    pSTT.Value = stt;
                    pHoVaTen.Value = GiaiMaAnToan(reader.GetValue(iHoVaTen));
                    pSoHieu.Value = GiaiMaAnToan(reader.GetValue(iSoHieu));
                    pNamSinh.Value = GiaiMaAnToan(reader.GetValue(iNamSinh));
                    pQueQuan.Value = GiaiMaAnToan(reader.GetValue(iQueQuan));
                    pNgayVaoCAND.Value = GiaiMaAnToan(reader.GetValue(iNgayVaoCAND));
                    pCapBac.Value = GiaiMaAnToan(reader.GetValue(iCapBac));
                    pChucVu.Value = GiaiMaAnToan(reader.GetValue(iChucVu));
                    pDonVi.Value = GiaiMaAnToan(reader.GetValue(iDonVi));
                    pPhanLoai.Value = GiaiMaAnToan(reader.GetValue(iPhanLoai));
                    pGhiChu.Value = GiaiMaAnToan(reader.GetValue(iGhiChu));
                    pThuTuSapXep.Value = stt;
                    cmdInsert.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        private void CuaSo_KhoiTaoStatusStrip()
        {
            // Label 1: Căn trái, hiển thị hình ảnh + nội dung
            toolStripStatusLabel1.Spring = false;
            toolStripStatusLabel1.Alignment = ToolStripItemAlignment.Left;
            toolStripStatusLabel1.TextAlign = ContentAlignment.MiddleLeft;
            toolStripStatusLabel1.ImageAlign = ContentAlignment.MiddleLeft;
            toolStripStatusLabel1.TextImageRelation = TextImageRelation.ImageBeforeText;
        }
        /// <summary>
        /// Định dạng hiển thị và cập nhật thông số tổng quân số trên thanh StatusStrip
        /// <param name="tongQuanSo">Số lượng quân số/cán bộ cần hiển thị</param>
        /// <param name="noiDungNutPhai">Nội dung hiển thị bên phải (mặc định lấy theo chuẩn)</param>
        private void CapNhatThanhTrangThaiStatusStrip(int tongQuanSo, string noiDungNutPhai = "Thêm danh sách hàng loạt (Excel)")
        {
            try
            {
                // 1. Định dạng Label bên trái (Tổng cộng)
                toolStripStatusLabel1.Text = $"Tổng cộng: {tongQuanSo:N0} đồng chí";
                // 3. Đảm bảo giao diện vẽ lại ngay lập tức
                statusStrip1?.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[STATUS_STRIP_ERR] {ex.Message}");
            }
        }
        private void KiemTraVaTaoBangDanhSachNhap()
        {
            try
            {
                using (var conn = new SqliteConnection(_connectionString))
                {
                    conn.Open();
                    string scriptTaoBang = @"
                CREATE TABLE IF NOT EXISTS ""DanhSach_Nhap"" (
                    ""ID""              INTEGER,
                    ""STT""             INTEGER NOT NULL DEFAULT 0,
                    ""HoVaTen""         TEXT NOT NULL,
                    ""SoHieu""          TEXT NOT NULL,
                    ""NamSinh""         TEXT NOT NULL,
                    ""QueQuan""         TEXT,
                    ""NgayVaoCAND""     TEXT,
                    ""CapBac""          TEXT,
                    ""ChucVu""          TEXT,
                    ""DonVi""           TEXT,
                    ""PhanLoai""        TEXT,
                    ""GhiChu""          TEXT,
                    ""ThuTuSapXep""     INTEGER NOT NULL DEFAULT 0,
                    ""ThoiGianTao""     TEXT NOT NULL,
                    ""ThoiGianCapNhat"" TEXT,
                    PRIMARY KEY(""ID"" AUTOINCREMENT)
                );";
                    using (var cmd = new SqliteCommand(scriptTaoBang, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi kiểm tra/tạo bảng CSDL: {ex.Message}", "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// Hàm phụ trợ giúp Chọn dòng và Set Focus an toàn (Tránh lỗi do Cột 0 bị Ẩn - Visible = false)
        private void LoadDuLieuLenGrid()
        {
            var dgv = kryptonDataGridView1;
            if (dgv == null || dgv.IsDisposed)
                return;
            DataTable dt = new DataTable();
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();
                const string sql = @"
                    SELECT *
                    FROM DanhSach_Nhap
                    ORDER BY STT ASC;";
                using var cmd = new SqliteCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                dt.Load(reader);
                _dtGoc = dt;
                dgv.SuspendLayout();
                try
                {
                    dgv.DataSource = null;
                    dgv.DataSource = dt;
                    if (dgv.Columns["ID"] != null)
                        dgv.Columns["ID"].Visible = false;
                    if (dgv.Columns["STT"] != null)
                        dgv.Columns["STT"].Visible = false;
                    if (dgv.Columns["ThuTuSapXep"] != null)
                        dgv.Columns["ThuTuSapXep"].Visible = false;
                    if (dgv.Columns["ThoiGianTao"] != null)
                        dgv.Columns["ThoiGianTao"].Visible = false;
                    if (dgv.Columns["ThoiGianCapNhat"] != null)
                        dgv.Columns["ThoiGianCapNhat"].Visible = false;
                    var tieuDeCot = new Dictionary<string, string>
                    {
                        ["HoVaTen"] = "Họ và tên",
                        ["SoHieu"] = "Số hiệu",
                        ["NamSinh"] = "Năm sinh",
                        ["QueQuan"] = "Quê quán",
                        ["NgayVaoCAND"] = "Vào CAND",
                        ["CapBac"] = "Cấp bậc",
                        ["ChucVu"] = "Chức vụ",
                        ["DonVi"] = "Đơn vị",
                        ["PhanLoai"] = "Phân loại",
                        ["GhiChu"] = "Ghi chú"
                    };
                    Font fontTieuDe = new Font(
                        Module_HeThong.TenFontHeThong,
                        9.75F,
                        FontStyle.Bold,
                        GraphicsUnit.Point);
                    foreach (var item in tieuDeCot)
                    {
                        if (dgv.Columns[item.Key] != null)
                        {
                            dgv.Columns[item.Key].HeaderText = item.Value;
                            dgv.Columns[item.Key].HeaderCell.Style.Font = fontTieuDe;
                        }
                    }
                }
                finally
                {
                    dgv.ResumeLayout();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi tải dữ liệu lên bảng:\n{ex.Message}",
                    "Lỗi CSDL",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void GanSuKienToanCuc()
        {
            // Sự kiện vẽ STT ảo lên Row Header
            kryptonDataGridView1.RowPostPaint -= KryptonDataGridView1_RowPostPaint;
            kryptonDataGridView1.RowPostPaint += KryptonDataGridView1_RowPostPaint;
            // Sự kiện tìm kiếm thời gian thực
            textBox_TimKiemTheoTen.TextChanged -= ThucHienLocKep;
            textBox_TimKiemTheoTen.TextChanged += ThucHienLocKep;
            comboBox_TimKiemDonVi.SelectedIndexChanged -= ThucHienLocKep;
            comboBox_TimKiemDonVi.SelectedIndexChanged += ThucHienLocKep;
        }
        /// Tự động vẽ Số Thứ Tự ảo lên Row Header (Bên trái ngoài cùng)
        /// Kể cả khi Lọc hay Tìm kiếm đều tự nhảy số từ 1, 2, 3...
        private void KryptonDataGridView1_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            var dgv = sender as KryptonDataGridView;
            if (dgv == null) return;
            // Đặt chuỗi STT tự động từ 1
            string sttText = (e.RowIndex + 1).ToString();
            // Font chữ và cọ vẽ STT
            Font fontStt = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            Brush brushText = SystemBrushes.ControlText;
            // Căn chỉnh chữ nằm giữa Row Header
            StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            // Xác định vùng Row Header
            Rectangle headerBounds = new Rectangle(
                e.RowBounds.Left,
                e.RowBounds.Top,
                dgv.RowHeadersWidth,
                e.RowBounds.Height
            );
            // Vẽ số thứ tự
            e.Graphics.DrawString(sttText, fontStt, brushText, headerBounds, sf);
        }
        /// Thực hiện lọc kết hợp cả Tên (không phân biệt hoa/thường) và Đơn vị
        private void ThucHienLocKep(object sender, EventArgs e)
        {
            if (_dtGoc == null) return;
            // ⭐ 1. Lấy từ khóa và bỏ qua nếu là chữ gợi ý placeholder hoặc khoảng trắng
            string tuKhoa = textBox_TimKiemTheoTen.Text.Trim();
            if (tuKhoa == "Nhập tìm kiếm..." || string.IsNullOrEmpty(tuKhoa))
            {
                tuKhoa = string.Empty;
            }
            string donViDuocChon = comboBox_TimKiemDonVi.SelectedItem != null
                ? comboBox_TimKiemDonVi.SelectedItem.ToString()
                : "Tất cả";
            List<string> filterConditions = new List<string>();
            // ⭐ 2. Thêm điều kiện lọc (có thể tìm kiếm theo HoVaTen hoặc SoHieu nếu muốn)
            if (!string.IsNullOrEmpty(tuKhoa))
            {
                string tuKhoaSafe = tuKhoa.Replace("'", "''");
                // Nếu bạn muốn tìm cả theo Họ tên và Số hiệu, có thể dùng:
                filterConditions.Add($"(HoVaTen LIKE '%{tuKhoaSafe}%' OR SoHieu LIKE '%{tuKhoaSafe}%')");
                // Hoặc nếu chỉ tìm riêng Họ tên:
                // filterConditions.Add($"HoVaTen LIKE '%{tuKhoaSafe}%'");
            }
            if (donViDuocChon != "Tất cả" && !string.IsNullOrEmpty(donViDuocChon))
            {
                filterConditions.Add($"DonVi = '{donViDuocChon.Replace("'", "''")}'");
            }
            // Áp dụng bộ lọc lên DataView của _dtGoc
            _dtGoc.DefaultView.RowFilter = filterConditions.Count > 0
                ? string.Join(" AND ", filterConditions)
                : string.Empty; // Nếu không có điều kiện nào thì reset về rỗng để hiển thị tất cả
            kryptonDataGridView1.DataSource = _dtGoc.DefaultView;
            // Co dãn và cập nhật status
            kryptonDataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            CapNhatThanhTrangThaiStatusStrip(_dtGoc.DefaultView.Count);
        }
        private void kryptonButton_LamMoiCacOTimKiem_Click(object sender, EventArgs e)
        {
            textBox_TimKiemTheoTen.Text = string.Empty;
            if (comboBox_TimKiemDonVi.Items.Count > 0)
            {
                comboBox_TimKiemDonVi.SelectedIndex = 0; // "Tất cả"
            }
            ThucHienLocKep(null, null);// ⭐ Cập nhật lại StatusStrip về tổng quân số ban đầu (khi bỏ bộ lọc)
            CapNhatThanhTrangThaiStatusStrip(kryptonDataGridView1.Rows.Count);
        }
        /// Nút làm mới CSDL: Đồng bộ lại từ DanhSach gốc, tải lại bảng và khôi phục ô tìm kiếm (Không báo Msg)
        private void LoadComboBoxDonVi()
        {
            comboBox_TimKiemDonVi.Items.Clear();
            comboBox_TimKiemDonVi.Items.Add("Tất cả");
            string[] dsDonVi = Module_DonVi.LayDanhSachDonViUuTienArray();
            if (dsDonVi != null && dsDonVi.Length > 0)
            {
                foreach (string dv in dsDonVi)
                {
                    if (!string.IsNullOrWhiteSpace(dv))
                    {
                        comboBox_TimKiemDonVi.Items.Add(dv.Trim());
                    }
                }
            }
            comboBox_TimKiemDonVi.SelectedIndex = 0; // Mặc định chọn "Tất cả"
        }
        private async void kryptonButton_RefershCSDL_Click(object sender, EventArgs e)
        {
            try
            {
                LoadDuLieuLenGrid();
                textBox_TimKiemTheoTen.Text = string.Empty;
                if (comboBox_TimKiemDonVi.Items.Count > 0)
                {
                    comboBox_TimKiemDonVi.SelectedIndex = 0;
                }
                DinhDangDataGridViewHienDai();
                toolStripStatusLabel1.Text = "Làm mới dữ liệu thành công.";
                await Task.Delay(200);
                CapNhatThanhTrangThaiStatusStrip(kryptonDataGridView1.Rows.Count);
            }
            catch (Exception ex)
            {
                toolStripStatusLabel1.Text = "Làm mới dữ liệu thất bại.";
                MessageBox.Show(
                    $"Không thể làm mới dữ liệu: {ex.Message}",
                    "Lỗi CSDL",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        /// Bộ giao diện hiện đại Ocean Blue Theme & Cấu hình chữ đứng hoàn toàn, chọn cả dòng
        private void KhoiTaoCacControlThongMinh()
        {
            // 1. Load danh sách Chức vụ từ CSDL SQLite vào comboBox_ChucVu (Có giải mã AES)
            LoadComboBoxChucVu();
            // 2. Load danh sách Phân loại từ DANH_SACH_PHAN_LOAI vào comboBox_PhanLoai
            LoadComboBoxPhanLoai();
            // 3. Load danh sách Đơn vị nhập liệu từ Module_DonVi (Đã đổi tên gọi hàm)
            LoadComboBoxDonViNhapLieu();
        }
        /// Cấu hình AutoComplete an toàn cho ComboBox.
        /// Giúp tránh lỗi văng ngoại lệ WinForms khi ComboBox ở chế độ DropDownList.
        /// Cấu hình ComboBox chỉ cho phép người dùng CHỌN (không gõ nhập liệu)
        private void CauHinhAnToanComboBox(ComboBox cbo)
        {
            if (cbo == null) return;
            // Chuyển ComboBox sang chỉ chọn
            cbo.DropDownStyle = ComboBoxStyle.DropDownList;
            // Tắt hoàn toàn AutoComplete để tránh xung đột với DropDownList
            cbo.AutoCompleteMode = AutoCompleteMode.None;
            cbo.AutoCompleteSource = AutoCompleteSource.None;
        }
        /// Nạp danh sách Chức vụ từ bảng DanhSach_ChucVu trong CSDL (Có giải mã AES)
        private void LoadComboBoxChucVu()
        {
            try
            {
                CauHinhAnToanComboBox(comboBox_ChucVu);
                comboBox_ChucVu.Items.Clear();
                comboBox_ChucVu.Items.Add("");
                using (var conn = new SqliteConnection(_connectionString))
                {
                    conn.Open();
                    const string sql = """
                SELECT Ten_ChucVu
                FROM DanhSach_ChucVu
                WHERE Ten_ChucVu IS NOT NULL
                  AND TRIM(Ten_ChucVu) <> ''
                ORDER BY ID ASC;
                """;
                    using var cmd = new SqliteCommand(sql, conn);
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        string chucVuGiaiMa = GiaiMaAnToan(reader["Ten_ChucVu"]).Trim();
                        if (!string.IsNullOrWhiteSpace(chucVuGiaiMa) &&
                            !comboBox_ChucVu.Items.Contains(chucVuGiaiMa))
                        {
                            comboBox_ChucVu.Items.Add(chucVuGiaiMa);
                        }
                    }
                }
                comboBox_ChucVu.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Không thể tải danh sách chức vụ.\n\nChi tiết: {ex.Message}",
                    "Lỗi tải dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        /// Nạp danh sách Phân loại từ mảng cố định DANH_SACH_PHAN_LOAI
        private void LoadComboBoxPhanLoai()
        {
            CauHinhAnToanComboBox(comboBox_PhanLoai);
            comboBox_PhanLoai.Items.Clear();
            // ⭐ Thêm giá trị rỗng làm mặc định
            comboBox_PhanLoai.Items.Add("");
            string[] dsPhanLoai = new string[]
            {
            Module_HeThong.Loai_1,
            Module_HeThong.Loai_2,
            Module_HeThong.Loai_3,
            Module_HeThong.Loai_4,
            Module_HeThong.PL_KHONG_PL
            };
            foreach (string pl in dsPhanLoai)
            {
                if (!string.IsNullOrWhiteSpace(pl) && !comboBox_PhanLoai.Items.Contains(pl.Trim()))
                {
                    comboBox_PhanLoai.Items.Add(pl.Trim());
                }
            }
            // Mặc định chọn dòng rỗng đầu tiên
            comboBox_PhanLoai.SelectedIndex = 0;
        }
        /// Nạp danh sách Đơn vị ưu tiên từ Module_DonVi
        /// (Tự động thích ứng nếu bạn dùng ComboBox hoặc TextBox với AutoComplete)
        /// Nạp danh sách Đơn vị ưu tiên từ Module_DonVi cho ô nhập liệu
        private void LoadComboBoxDonViNhapLieu()
        {
            if (comboBox_DonVi == null) return;
            // 1. Tắt AutoComplete hoàn toàn trong Code để không đụng độ với DropDownList
            comboBox_DonVi.AutoCompleteMode = AutoCompleteMode.None;
            comboBox_DonVi.AutoCompleteSource = AutoCompleteSource.None;
            // 2. Làm sạch và nạp dữ liệu
            comboBox_DonVi.Items.Clear();
            comboBox_DonVi.Items.Add(""); // Thêm giá trị rỗng mặc định
            string[] dsDonVi = Module_DonVi.LayDanhSachDonViUuTienArray()
                                          ?.Where(s => !string.IsNullOrWhiteSpace(s))
                                          .ToArray() ?? new string[0];
            foreach (string dv in dsDonVi)
            {
                string dvTrim = dv.Trim();
                if (!comboBox_DonVi.Items.Contains(dvTrim))
                {
                    comboBox_DonVi.Items.Add(dvTrim);
                }
            }
            comboBox_DonVi.SelectedIndex = 0;
        }
        /// 1. Chặn người dùng gõ khoảng trắng và dấu '-' trực tiếp từ bàn phím
        private void textBox_SoHieu_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Cả 2 chế độ đều không cho phép nhập khoảng trắng hoặc dấu gạch ngang
            if (e.KeyChar == ' ' || e.KeyChar == '-')
            {
                e.Handled = true; // Bỏ qua ký tự không hợp lệ
            }
        }
        private void textBox_SoHieu_Leave(object sender, EventArgs e)
        {
            string soHieuNhap = textBox_SoHieu.Text.Trim();
            // CHẾ ĐỘ 1: TÂN BINH (Cấp phát & kiểm tra qua CSDL2 - Mã hóa AES)
            if (KiemTraLaTanBinh())
            {
                // 1. Nếu để trống, tự động sinh mã ID tiếp theo
                if (string.IsNullOrWhiteSpace(soHieuNhap))
                {
                    textBox_SoHieu.Text = SinhSoHieuTanBinhTiepTheo();
                    return;
                }
                // 2. Tự động thêm tiền tố "ID" nếu người dùng chỉ nhập số (VD: nhập "00005" -> "ID00005")
                if (!soHieuNhap.StartsWith("ID", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(soHieuNhap, out int soThuTu))
                    {
                        soHieuNhap = $"ID{soThuTu:D5}";
                    }
                    else
                    {
                        soHieuNhap = "ID" + soHieuNhap;
                    }
                    textBox_SoHieu.Text = soHieuNhap;
                }
                // 3. Kiểm tra trùng mã ID trong CSDL2 (Giải mã AES)
                bool isTrungCSDL2 = false;
                try
                {
                    using (var conn = new SqliteConnection($"Data Source={Module_DanduongGPS.DuongDanCSDL2}"))
                    {
                        conn.Open();
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = "SELECT SoHieu FROM DanhSach WHERE SoHieu IS NOT NULL AND SoHieu <> ''";
                        using var rd = cmd.ExecuteReader();
                        while (rd.Read())
                        {
                            string shGiaiMa = Module_BaoMatAES.GiaiMa(rd.GetString(0));
                            if (string.Equals(shGiaiMa, soHieuNhap, StringComparison.OrdinalIgnoreCase))
                            {
                                isTrungCSDL2 = true;
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi kiểm tra dữ liệu CSDL2: " + ex.Message, "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // 4. Nếu bị trùng ID, thông báo và tự động gán ID mới chưa tồn tại
                if (isTrungCSDL2)
                {
                    MessageBox.Show($"Mã [{soHieuNhap}] đã tồn tại trong cơ sở dữ liệu! Hệ thống sẽ tự động cấp mã ID mới.",
                                    "Cảnh báo trùng ID", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox_SoHieu.Text = SinhSoHieuTanBinhTiepTheo();
                    textBox_SoHieu.Focus();
                }
                return;
            }
            // CHẾ ĐỘ 2: CBCS (Kiểm tra dữ liệu trên RAM)
            // Làm sạch chuỗi (trường hợp người dùng Paste dữ liệu vào)
            soHieuNhap = ChuoiSoHieuChuan(textBox_SoHieu.Text);
            textBox_SoHieu.Text = soHieuNhap;
            if (string.IsNullOrWhiteSpace(soHieuNhap)) return;
            // Kiểm tra dữ liệu trên RAM (_dtGoc)
            if (_dtGoc != null && _dtGoc.Rows.Count > 0)
            {
                bool isTrung = _dtGoc.AsEnumerable().Any(row =>
                {
                    string soHieuTrongRAM = ChuoiSoHieuChuan(row.Field<string>("SoHieu"));
                    return string.Equals(soHieuTrongRAM, soHieuNhap, StringComparison.OrdinalIgnoreCase);
                });
                if (!isTrung)
                {
                    SetComboBoxPhanLoaiMacDinh("Loại 2");
                }
            }
            else
            {
                SetComboBoxPhanLoaiMacDinh("Loại 2");
            }
        }
        private void SetComboBoxPhanLoaiMacDinh(string giaTriMacDinh)
        {
            if (comboBox_PhanLoai == null) return;
            // Cách 1: Nếu ComboBox lưu dạng Chuỗi (Text/Items đơn giản)
            if (comboBox_PhanLoai.Items.Contains(giaTriMacDinh))
            {
                comboBox_PhanLoai.SelectedItem = giaTriMacDinh;
            }
            else
            {
                // Cách 2: Tìm theo chuỗi hiển thị nếu ComboBox dùng DataSource/Object
                int index = comboBox_PhanLoai.FindStringExact(giaTriMacDinh);
                if (index != -1)
                {
                    comboBox_PhanLoai.SelectedIndex = index;
                }
                else
                {
                    comboBox_PhanLoai.Text = giaTriMacDinh;
                }
            }
        }
        private string ChuoiSoHieuChuan(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            // Loại bỏ khoảng trắng và dấu '-'
            return input.Replace(" ", "").Replace("-", "").Trim();
        }
        // Biến lưu ID của bản ghi đang chọn để phục vụ Cập nhật / Xóa
        // Hàm phụ trợ gán giá trị ComboBox an toàn
        /// <summary>
        /// Gom dữ liệu từ các Control trên Form vào đối tượng DTO
        private CanBoDTO LayDuLieuTuForm()
        {
            return new CanBoDTO
            {
                HoVaTen = textBox_HoVaTen.Text.Trim(),
                SoHieu = textBox_SoHieu.Text.Trim(),
                NamSinh = textBox_NamSinh.Text.Trim(),
                QueQuan = textBox_QueQuan.Text.Trim(),
                NgayVaoCAND = textBox_NgayVaoCAND.Text.Trim(),
                CapBac = textBox_CapBac.Text.Trim(),
                ChucVu = comboBox_ChucVu.SelectedItem?.ToString().Trim() ?? comboBox_ChucVu.Text.Trim(),
                DonVi = comboBox_DonVi.SelectedItem?.ToString().Trim() ?? comboBox_DonVi.Text.Trim(),
                PhanLoai = comboBox_PhanLoai.SelectedItem?.ToString().Trim() ?? comboBox_PhanLoai.Text.Trim(),
                GhiChu = textBox_GhiChu.Text.Trim()
            };
        }
        private void SetComboBoxValue(ComboBox cbo, string value)
        {
            if (cbo == null) return;
            int index = cbo.FindStringExact(value);
            cbo.SelectedIndex = index >= 0 ? index : 0;
        }
        /// <summary>
        /// Cập nhật trường ThuTuSapXep cho 2 bản ghi trong một Transaction an toàn.
        private void kryptonButton1_DiChuyenDataCBCSLen_Click(object sender, EventArgs e)
        {
            try
            {
                if (kryptonDataGridView1.CurrentRow == null ||
                    kryptonDataGridView1.CurrentRow.IsNewRow)
                    return;
                int currentIndex = kryptonDataGridView1.CurrentRow.Index;
                if (currentIndex <= 0)
                    return;
                if (kryptonDataGridView1.Rows[currentIndex].DataBoundItem is not DataRowView drvCurrent ||
                    kryptonDataGridView1.Rows[currentIndex - 1].DataBoundItem is not DataRowView drvPrev)
                    return;
                int currentID = Convert.ToInt32(drvCurrent["ID"]);
                int currentSTT = Convert.ToInt32(drvCurrent["STT"]);
                int prevID = Convert.ToInt32(drvPrev["ID"]);
                int prevSTT = Convert.ToInt32(drvPrev["STT"]);
                // 1. Cập nhật thứ tự STT vào CSDL SQLite
                if (!CapNhatSTTCSDL(currentID, prevSTT, prevID, currentSTT))
                    return;
                // ⭐ ĐÁNH DẤU CÓ THAY ĐỔI DỮ LIỆU
                _coThayDoiDuLieu = true;
                // 2. Nạp lại DataTable gốc từ CSDL để giữ dữ liệu mới nhất
                NapLaiDataGocTuCSDL();
                // 3. Thực hiện lọc lại để giữ nguyên trạng thái đang lọc trên DataGridView
                ThucHienLocKep(null, null);
                // 4. Đặt lại con trỏ vào dòng vừa di chuyển
                BeginInvoke(new Action(() =>
                {
                    DatLaiViTriChonTheoID(currentID);
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi di chuyển dữ liệu lên:\n{ex.Message}",
                    "Lỗi giao diện",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void kryptonButton1_DiChuyenDataCBCSXuong_Click(object sender, EventArgs e)
        {
            try
            {
                if (kryptonDataGridView1.CurrentRow == null ||
                    kryptonDataGridView1.CurrentRow.IsNewRow)
                    return;
                int currentIndex = kryptonDataGridView1.CurrentRow.Index;
                if (currentIndex >= kryptonDataGridView1.Rows.Count - 1)
                    return;
                if (kryptonDataGridView1.Rows[currentIndex].DataBoundItem is not DataRowView drvCurrent ||
                    kryptonDataGridView1.Rows[currentIndex + 1].DataBoundItem is not DataRowView drvNext)
                    return;
                int currentID = Convert.ToInt32(drvCurrent["ID"]);
                int currentSTT = Convert.ToInt32(drvCurrent["STT"]);
                int nextID = Convert.ToInt32(drvNext["ID"]);
                int nextSTT = Convert.ToInt32(drvNext["STT"]);
                // 1. Cập nhật thứ tự STT vào CSDL SQLite
                if (!CapNhatSTTCSDL(currentID, nextSTT, nextID, currentSTT))
                    return;
                // ⭐ ĐÁNH DẤU CÓ THAY ĐỔI DỮ LIỆU
                _coThayDoiDuLieu = true;
                // 2. Nạp lại DataTable gốc từ CSDL để giữ dữ liệu mới nhất
                NapLaiDataGocTuCSDL();
                // 3. Thực hiện lọc lại để giữ nguyên trạng thái đang lọc trên DataGridView
                ThucHienLocKep(null, null);
                // 4. Đặt lại con trỏ vào dòng vừa di chuyển
                BeginInvoke(new Action(() =>
                {
                    DatLaiViTriChonTheoID(currentID);
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi di chuyển dữ liệu xuống:\n{ex.Message}",
                    "Lỗi giao diện",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void NapLaiDataGocTuCSDL()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            const string query = "SELECT * FROM DanhSach_Nhap ORDER BY STT ASC;";
            using var cmd = new SqliteCommand(query, conn);
            using var reader = cmd.ExecuteReader();
            _dtGoc = new DataTable();
            _dtGoc.Load(reader);
        }
        private bool CapNhatSTTCSDL(int id1, int stt1, int id2, int stt2)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                // Cập nhật cả STT và ThuTuSapXep để 2 cột luôn đồng bộ tuyệt đối
                const string query = @"
            UPDATE DanhSach_Nhap
            SET STT = $STT, ThuTuSapXep = $STT
            WHERE ID = $ID;";
                using var cmd = new SqliteCommand(query, conn, trans);
                cmd.Parameters.AddWithValue("$STT", stt1);
                cmd.Parameters.AddWithValue("$ID", id1);
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("$STT", stt2);
                cmd.Parameters.AddWithValue("$ID", id2);
                cmd.ExecuteNonQuery();
                trans.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try { trans.Rollback(); } catch { }
                MessageBox.Show($"Lỗi cập nhật thứ tự: {ex.Message}", "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        private void DatLaiViTriChonTheoID(int id)
        {
            if (kryptonDataGridView1 == null ||
                kryptonDataGridView1.IsDisposed ||
                kryptonDataGridView1.Rows.Count == 0)
                return;
            int targetIndex = -1;
            for (int i = 0; i < kryptonDataGridView1.Rows.Count; i++)
            {
                if (kryptonDataGridView1.Rows[i].IsNewRow)
                    continue;
                if (kryptonDataGridView1.Rows[i].DataBoundItem is DataRowView drv &&
                    drv.Row.Table.Columns.Contains("ID") &&
                    Convert.ToInt32(drv["ID"]) == id)
                {
                    targetIndex = i;
                    break;
                }
            }
            if (targetIndex < 0)
                return;
            kryptonDataGridView1.ClearSelection();
            DataGridViewRow row = kryptonDataGridView1.Rows[targetIndex];
            row.Selected = true;
            DataGridViewColumn? firstVisibleColumn = kryptonDataGridView1.Columns
                .Cast<DataGridViewColumn>()
                .FirstOrDefault(c => c.Visible && c.Index >= 0);
            if (firstVisibleColumn == null)
                return;
            DataGridViewCell cell = row.Cells[firstVisibleColumn.Index];
            if (cell.OwningRow == null ||
                cell.OwningRow.IsNewRow ||
                !firstVisibleColumn.Visible)
                return;
            kryptonDataGridView1.CurrentCell = cell;
        }
        private void DinhDangDataGridViewHienDai()
        {
            var dgv = kryptonDataGridView1;
            if (dgv == null) return;
            dgv.SuspendLayout();
            try
            {
                // ⭐ BỔ SUNG 3 DÒNG KHÓA TÍNH NĂNG TẠI ĐÂY
                dgv.AllowUserToOrderColumns = false; // Không cho kéo thả đổi vị trí cột
                dgv.AllowUserToResizeColumns = false; // Không cho kéo dãn độ rộng cột
                dgv.ReadOnly = true;                 // Khóa không cho đúp chuột sửa dữ liệu
                                                     // Mở hiển thị cột Row Header và chỉnh chiều rộng chứa vừa số STT lớn
                dgv.RowHeadersVisible = true;
                dgv.RowHeadersWidth = 50;
                dgv.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
                dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgv.MultiSelect = false;
                // Font chữ tiêu chuẩn và Font tiêu đề In đậm
                Font fontChuDung = new Font(Module_HeThong.TenFontHeThong, 9.75F, FontStyle.Regular, GraphicsUnit.Point);
                Font fontTieuDeBold = new Font(Module_HeThong.TenFontHeThong, 9.75F, FontStyle.Bold, GraphicsUnit.Point);
                int fontHeight = fontChuDung.Height;
                // ⭐ 1. TĂNG CHIỀU CAO TIÊU ĐỀ (Gấp 3.5 lần chiều cao font) ĐỂ KHÔNG BỊ CẮT CHỮ
                dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                dgv.ColumnHeadersHeight = (int)(fontHeight * 3.5);
                dgv.RowTemplate.Height = (int)(fontHeight * 1.8);
                dgv.Font = fontChuDung;
                dgv.DefaultCellStyle.Font = fontChuDung;
                dgv.RowsDefaultCellStyle.Font = fontChuDung;
                dgv.StateCommon.DataCell.Content.Font = fontChuDung;
                // ⭐ 2. ÁP DỤNG FONT IN ĐẬM VÀ BẬT WRAPTEXT CHO TIÊU ĐỀ CỘT
                dgv.ColumnHeadersDefaultCellStyle.Font = fontTieuDeBold;
                dgv.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True; // Bật xuống dòng tiêu đề
                dgv.StateCommon.HeaderColumn.Content.Font = fontTieuDeBold;
                dgv.StateCommon.HeaderColumn.Content.TextH = Krypton.Toolkit.PaletteRelativeAlign.Center; // Căn giữa ngang
                dgv.StateCommon.HeaderColumn.Content.TextV = Krypton.Toolkit.PaletteRelativeAlign.Center; // Căn giữa dọc
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    col.DefaultCellStyle.Font = fontChuDung;
                    col.HeaderCell.Style.Font = fontTieuDeBold;
                    col.HeaderCell.Style.WrapMode = DataGridViewTriState.True; // Bật xuống dòng cho từng cột
                }
                dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                // Nền lưới trắng
                dgv.StateCommon.Background.Color1 = Color.White;
                dgv.StateCommon.DataCell.Back.Color1 = Color.White;
                // Viền lưới thanh mảnh
                dgv.StateCommon.DataCell.Border.Color1 = Color.FromArgb(235, 235, 235);
                dgv.StateCommon.DataCell.Border.DrawBorders = PaletteDrawBorders.All;
                dgv.StateCommon.DataCell.Border.Width = 1;
                // Tiêu đề cột Header - Ocean Blue
                dgv.StateCommon.HeaderColumn.Back.Color1 = Color.FromArgb(180, 210, 240);
                dgv.StateCommon.HeaderColumn.Back.Color2 = Color.FromArgb(180, 210, 240);
                dgv.StateCommon.HeaderColumn.Content.Color1 = Color.FromArgb(30, 30, 30);
                dgv.StateCommon.HeaderColumn.Border.Color1 = Color.FromArgb(150, 180, 210);
                // Row Header (Cột đánh STT ảo) đồng bộ tông Ocean Blue
                dgv.StateCommon.HeaderRow.Back.Color1 = Color.FromArgb(240, 245, 250);
                dgv.StateCommon.HeaderRow.Back.Color2 = Color.FromArgb(240, 245, 250);
                dgv.StateCommon.HeaderRow.Border.Color1 = Color.FromArgb(210, 225, 240);
                // Khi chọn dòng
                dgv.StateSelected.DataCell.Back.Color1 = Color.FromArgb(232, 244, 253);
                dgv.StateSelected.DataCell.Back.Color2 = Color.FromArgb(232, 244, 253);
                dgv.StateSelected.DataCell.Content.Color1 = Color.FromArgb(0, 102, 204);
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    row.Height = dgv.RowTemplate.Height;
                }
                // ⭐ 3. ĐIỀU CHỈNH ĐỘ RỘNG CÁC CỘT (ĐÃ MỞ RỘNG CÁC CỘT NHỎ ĐỂ KHÔNG BỊ CẮT CHỮ)
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    string header = col.HeaderText.Trim();
                    switch (header)
                    {
                        case "Họ và tên":
                            col.Width = 210;
                            break;
                        case "Số hiệu":
                            col.Width = 105; // Tăng từ 95 -> 105
                            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                            break;
                        case "Năm sinh":
                            col.Width = 95; // Tăng từ 80 -> 95
                            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                            break;
                        case "Quê quán":
                            col.Width = 250; // Tăng từ 210 -> 250
                            break;
                        case "Vào CAND":
                            col.Width = 125; // Tăng từ 115 -> 125
                            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                            break;
                        case "Cấp bậc":
                            col.Width = 95; // Tăng từ 70 -> 95
                            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                            break;
                        case "Chức vụ":
                            col.Width = 105; // Tăng từ 75 -> 105
                            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                            break;
                        case "Đơn vị":
                            col.Width = 110; // Tăng từ 80 -> 110
                            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                            break;
                        case "Phân loại":
                            col.Width = 110; // Tăng từ 80 -> 110
                            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                            break;
                        case "Ghi chú":
                            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                            col.MinimumWidth = 120;
                            break;
                    }
                }
            }
            finally
            {
                dgv.ResumeLayout();
            }
        }
        private void ChonDongVuaThemTheoSoHieu(string soHieu)
        {
            if (string.IsNullOrWhiteSpace(soHieu) || kryptonDataGridView1.Rows.Count == 0)
                return;
            string targetSoHieu = soHieu.Trim();
            foreach (DataGridViewRow row in kryptonDataGridView1.Rows)
            {
                // Kiểm tra an toàn giá trị ô Số hiệu (thay "SoHieu" bằng tên cột thực tế trên Grid nếu khác)
                var cellValue = row.Cells["SoHieu"]?.Value?.ToString()?.Trim();
                if (string.Equals(cellValue, targetSoHieu, StringComparison.OrdinalIgnoreCase))
                {
                    // Bỏ chọn toàn bộ dòng cũ
                    kryptonDataGridView1.ClearSelection();
                    // Chọn toàn bộ dòng mới
                    row.Selected = true;
                    // Đặt CurrentCell vào cột hiển thị (Visible = true) đầu tiên để tránh lỗi "invisible cell"
                    foreach (DataGridViewColumn col in kryptonDataGridView1.Columns)
                    {
                        if (col.Visible)
                        {
                            kryptonDataGridView1.CurrentCell = row.Cells[col.Index];
                            break;
                        }
                    }
                    // Tự động cuộn màn hình đến dòng được chọn
                    kryptonDataGridView1.FirstDisplayedScrollingRowIndex = row.Index;
                    break;
                }
            }
        }
        public string SinhSoHieuTanBinhTiepTheo(string connectionStringForm = null)
        {
            // Nếu không truyền tham số, tự động lấy _connectionString của Form hiện tại
            string connString = string.IsNullOrWhiteSpace(connectionStringForm) ? _connectionString : connectionStringForm;
            HashSet<string> soHieuDaTonTai = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // 1. Quét toàn bộ mã ID từ CSDL2 (Mã hóa AES)
            try
            {
                using (var conn2 = new SqliteConnection($"Data Source={Module_DanduongGPS.DuongDanCSDL2}"))
                {
                    conn2.Open();
                    using var cmd = conn2.CreateCommand();
                    cmd.CommandText = "SELECT SoHieu FROM DanhSach WHERE SoHieu IS NOT NULL AND SoHieu <> ''";
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        string shGiaiMa = Module_BaoMatAES.GiaiMa(rd.GetString(0));
                        if (!string.IsNullOrWhiteSpace(shGiaiMa)) soHieuDaTonTai.Add(shGiaiMa.Trim());
                    }
                }
            }
            catch { /* Lớp phòng thủ nếu file CSDL2 chưa khởi tạo */ }
            // 2. Quét tiếp các mã ID đang nằm trong bảng tạm DanhSach_Nhap
            try
            {
                using (var connNhap = new SqliteConnection(connString))
                {
                    connNhap.Open();
                    using var cmd = connNhap.CreateCommand();
                    cmd.CommandText = "SELECT SoHieu FROM DanhSach_Nhap WHERE SoHieu IS NOT NULL AND SoHieu <> ''";
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        string sh = rd.GetString(0);
                        if (!string.IsNullOrWhiteSpace(sh)) soHieuDaTonTai.Add(sh.Trim());
                    }
                }
            }
            catch { }
            // 3. Tự động tìm chỉ số ID000xx lớn nhất chưa xuất hiện
            int chiSo = 1;
            string maMoi = "";
            do
            {
                maMoi = $"ID{chiSo:D5}";
                chiSo++;
            }
            while (soHieuDaTonTai.Contains(maMoi));
            return maMoi;
        }
        private void TuDongCapPhatSoHieuChoTanBinh()
        {
            if (KiemTraLaTanBinh())
            {
                // Cho phép hiển thị và chỉnh sửa nếu cần
                textBox_SoHieu.ReadOnly = false;
                textBox_SoHieu.Enabled = true;
                // Nếu ô số hiệu đang trống thì tự động sinh ID mới
                if (string.IsNullOrWhiteSpace(textBox_SoHieu.Text) || !textBox_SoHieu.Text.StartsWith("ID"))
                {
                    textBox_SoHieu.Text = SinhSoHieuTanBinhTiepTheo();
                }
            }
        }
        private void XoaTrangCacControl()
        {
            // 1. Reset biến lưu ID bản ghi đang chọn
            _currentSelectedID = 0;
            // 2. Xóa trắng nội dung tất cả ô TextBox
            textBox_HoVaTen.Text = string.Empty;
            textBox_SoHieu.Text = string.Empty;
            textBox_NamSinh.Text = string.Empty;
            textBox_QueQuan.Text = string.Empty;
            textBox_NgayVaoCAND.Text = string.Empty;
            textBox_CapBac.Text = string.Empty;
            textBox_GhiChu.Text = string.Empty;
            // 3. Đưa ComboBox về giá trị mặc định đầu tiên
            if (comboBox_ChucVu.Items.Count > 0) comboBox_ChucVu.SelectedIndex = 0;
            if (comboBox_DonVi.Items.Count > 0) comboBox_DonVi.SelectedIndex = 0;
            if (comboBox_PhanLoai.Items.Count > 0) comboBox_PhanLoai.SelectedIndex = 0;
            // 4. Sẵn sàng cho lượt nhập mới: Đặt con trỏ chuột vào ô Họ và Tên
            textBox_HoVaTen.Focus();
        }
        private void kryptonButton1_DongForm_Click(object sender, EventArgs e)
        {
            Module_NhatKy.GhiNhatKy(
                Module_TaiKhoan.TenTaiKhoan_RAM ?? "Admin",
                "Đóng form nhập liệu cán bộ chiến sĩ và tự động đồng bộ dữ liệu từ DanhSach_Nhap vào DanhSach",
                $"Vào lúc {DateTime.Now:HH:mm:ss}");
            this.Close();
        }
        /// Bổ sung sự kiện FormClosed cho Form60 (đăng ký trong Designer hoặc Constructor)
        private bool HienThiFormAo_XacNhan(string tieuDe, string noiDung)
        {
            string noiDungChuan = noiDung.Replace("\n", Environment.NewLine);
            bool ketQuaDongY = false;
            using (var formAo = new FormAoBase())
            {
                formAo.Text = "Xác nhận thao tác";
                formAo.Size = new System.Drawing.Size(1000, 500);
                formAo.FormBorderStyle = FormBorderStyle.FixedDialog;
                formAo.MaximizeBox = false; formAo.MinimizeBox = false; formAo.ShowIcon = false;
                formAo.ShowInTaskbar = false;
                var panelTop = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Top, Height = 70, Padding = new Padding(30, 25, 20, 5) };
                panelTop.StateCommon.Color1 = System.Drawing.Color.White;
                var lblTitle = new Krypton.Toolkit.KryptonLabel { Text = tieuDe.ToUpper(), Dock = DockStyle.Fill, AutoSize = false };
                lblTitle.StateCommon.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 13F, System.Drawing.FontStyle.Bold);
                lblTitle.StateCommon.ShortText.Color1 = System.Drawing.Color.FromArgb(0, 82, 155);
                panelTop.Controls.Add(lblTitle);
                var separator = new Label { Height = 1, Dock = DockStyle.Top, BackColor = System.Drawing.Color.FromArgb(200, 220, 240), Margin = new Padding(0, 5, 0, 10) };
                var panelContent = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Fill, Padding = new Padding(25, 15, 30, 20) };
                panelContent.StateCommon.Color1 = System.Drawing.Color.White;
                var picIcon = new PictureBox { Image = System.Drawing.SystemIcons.Question.ToBitmap(), SizeMode = PictureBoxSizeMode.CenterImage, Size = new System.Drawing.Size(50, 50), Location = new System.Drawing.Point(25, 15) };
                var txtContent = new Krypton.Toolkit.KryptonTextBox
                {
                    Text = noiDungChuan,
                    ReadOnly = true,
                    Multiline = true,
                    WordWrap = true,
                    ScrollBars = ScrollBars.Vertical,
                    Location = new System.Drawing.Point(90, 15),
                    Width = formAo.Width - 130,
                    Height = panelContent.Height - 35,
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
                };
                txtContent.StateCommon.Back.Color1 = System.Drawing.Color.White;
                txtContent.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.None;
                txtContent.StateCommon.Content.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 11.5F, System.Drawing.FontStyle.Regular);
                txtContent.StateCommon.Content.Color1 = System.Drawing.Color.FromArgb(40, 40, 40);
                txtContent.StateCommon.Content.Padding = new Padding(0);
                var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 75, BackColor = System.Drawing.Color.WhiteSmoke };
                var btnNo = new Krypton.Toolkit.KryptonButton { Text = "Hủy bỏ", Width = 140, Height = 42, DialogResult = DialogResult.No };
                btnNo.StateCommon.Content.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 10.5F, System.Drawing.FontStyle.Bold);
                btnNo.StateCommon.Border.Rounding = 6;
                btnNo.Click += (s, ev) => ketQuaDongY = false;
                var btnYes = new Krypton.Toolkit.KryptonButton { Text = "Đồng ý", Width = 140, Height = 42, DialogResult = DialogResult.Yes };
                btnYes.StateCommon.Content.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 10.5F, System.Drawing.FontStyle.Bold);
                btnYes.StateCommon.Border.Rounding = 6;
                btnYes.Click += (s, ev) => ketQuaDongY = true;
                int totalWidth = btnYes.Width + 20 + btnNo.Width;
                int startX = (formAo.Width - totalWidth) / 2;
                btnYes.Location = new System.Drawing.Point(startX, 16);
                btnNo.Location = new System.Drawing.Point(startX + btnYes.Width + 20, 16);
                panelBottom.Controls.Add(btnYes); panelBottom.Controls.Add(btnNo);
                panelContent.Controls.Add(picIcon); panelContent.Controls.Add(txtContent); panelContent.Controls.Add(separator);
                txtContent.BringToFront(); picIcon.BringToFront(); separator.SendToBack();
                formAo.Controls.Add(panelContent); formAo.Controls.Add(panelTop); formAo.Controls.Add(panelBottom);
                formAo.AcceptButton = btnYes; formAo.CancelButton = btnNo;
                formAo.Shown += (s, ev) => btnNo.Focus(); // An toàn: Focus Hủy bỏ
                formAo.ShowDialog(this);
            }
            return ketQuaDongY;
        }
        /// <summary>
        /// Dựng Form ảo chuyên hiển thị Cảnh Báo Validation (Màu Cam Đất).
        /// Dựng Form ảo chuyên hiển thị Lỗi Hệ Thống (Màu Đỏ Thẫm).
        /// Tích hợp nút Copy Lỗi.
        private void HienThiFormAo_Loi(string tieuDe, string noiDungLoi)
        {
            string noiDungChuan = noiDungLoi.Replace("\n", Environment.NewLine);
            using (var formAo = new FormAoBase())
            {
                formAo.Text = "Hệ thống ghi nhận sự cố";
                formAo.Size = new System.Drawing.Size(950, 560);
                formAo.FormBorderStyle = FormBorderStyle.FixedDialog;
                formAo.MaximizeBox = false; formAo.MinimizeBox = false; formAo.ShowIcon = false;
                formAo.ShowInTaskbar = false;
                var panelTop = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(25, 20, 20, 5) };
                panelTop.StateCommon.Color1 = System.Drawing.Color.White;
                var lblTitle = new Krypton.Toolkit.KryptonLabel { Text = tieuDe.ToUpper(), Dock = DockStyle.Fill, AutoSize = false };
                lblTitle.StateCommon.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 12F, System.Drawing.FontStyle.Bold);
                lblTitle.StateCommon.ShortText.Color1 = System.Drawing.Color.FromArgb(198, 40, 40);
                panelTop.Controls.Add(lblTitle);
                var separator = new Label { Height = 1, Dock = DockStyle.Top, BackColor = System.Drawing.Color.FromArgb(240, 200, 200), Margin = new Padding(0, 5, 0, 10) };
                var panelContent = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Fill, Padding = new Padding(20, 15, 25, 20) };
                panelContent.StateCommon.Color1 = System.Drawing.Color.White;
                var picIcon = new PictureBox { Image = System.Drawing.SystemIcons.Error.ToBitmap(), SizeMode = PictureBoxSizeMode.CenterImage, Size = new System.Drawing.Size(50, 50), Location = new System.Drawing.Point(20, 15) };
                var txtContent = new Krypton.Toolkit.KryptonTextBox
                {
                    Text = noiDungChuan,
                    ReadOnly = true,
                    Multiline = true,
                    WordWrap = true,
                    ScrollBars = ScrollBars.Vertical,
                    Location = new System.Drawing.Point(80, 15),
                    Width = formAo.Width - 110,
                    Height = panelContent.Height - 35,
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
                };
                txtContent.StateCommon.Back.Color1 = System.Drawing.Color.White;
                txtContent.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.None;
                txtContent.StateCommon.Content.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 10.5F, System.Drawing.FontStyle.Regular);
                txtContent.StateCommon.Content.Color1 = System.Drawing.Color.FromArgb(40, 40, 40);
                txtContent.StateCommon.Content.Padding = new Padding(0);
                var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = System.Drawing.Color.WhiteSmoke };
                var btnCopy = new Krypton.Toolkit.KryptonButton { Text = "Sao chép mã lỗi", Width = 160, Height = 35 };
                btnCopy.StateCommon.Content.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 9.5F, System.Drawing.FontStyle.Bold);
                btnCopy.Click += (s, ev) => { try { Clipboard.SetText(noiDungChuan); MessageBox.Show(formAo, "Đã sao chép mã lỗi.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information); } catch { } };
                var btnClose = new Krypton.Toolkit.KryptonButton { Text = "Đóng", Width = 100, Height = 35, DialogResult = DialogResult.OK };
                btnClose.StateCommon.Content.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 9.5F, System.Drawing.FontStyle.Bold);
                int totalWidth = btnCopy.Width + 15 + btnClose.Width;
                int startX = (formAo.Width - totalWidth) / 2;
                btnCopy.Location = new System.Drawing.Point(startX, 12);
                btnClose.Location = new System.Drawing.Point(startX + btnCopy.Width + 15, 12);
                panelBottom.Controls.Add(btnCopy); panelBottom.Controls.Add(btnClose);
                panelContent.Controls.Add(picIcon); panelContent.Controls.Add(txtContent); panelContent.Controls.Add(separator);
                txtContent.BringToFront(); picIcon.BringToFront(); separator.SendToBack();
                formAo.Controls.Add(panelContent); formAo.Controls.Add(panelTop); formAo.Controls.Add(panelBottom);
                formAo.AcceptButton = btnClose; formAo.CancelButton = btnClose;
                formAo.Shown += (s, ev) => btnClose.Focus();
                System.Diagnostics.Debug.WriteLine($"[ERR_UI] {tieuDe} - {noiDungChuan}");
                formAo.ShowDialog(this);
            }
        }
        /// <summary>
        /// Đồng bộ toàn bộ dữ liệu từ bảng DanhSach_Nhap (chưa mã hóa) 
        /// sang bảng DanhSach (đã mã hóa AES).
        /// <summary>
        /// Đồng bộ ngược từ DanhSach_Nhap sang DanhSach gốc:
        /// - Đối chiếu theo Số hiệu: Đã tồn tại thì cập nhật thông tin mới, chưa có thì thêm mới.
        /// - Sắp xếp và đánh lại thứ tự (STT, ThuTuSapXep) trong CSDL gốc khớp hoàn toàn với trật tự của DanhSach_Nhap.
        /// /// <summary>
        /// Mã hóa an toàn chuỗi văn bản bằng AES trước khi ghi vào cơ sở dữ liệu.
        private string MaHoaAnToan(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;
            try
            {
                // Gọi phương thức mã hóa AES thực tế trong dự án của bạn (ví dụ: Module_BaoMatAES.MaHoa)
                return Module_BaoMatAES.MaHoa(plainText);
            }
            catch
            {
                // Trường hợp lỗi hoặc đã mã hóa rồi thì trả về nguyên bản để tránh mất dữ liệu
                return plainText;
            }
        }
        /// <summary>
        /// Giải mã an toàn chuỗi từ cơ sở dữ liệu để đối chiếu số hiệu chính xác.
        private string GiaiMaAnToan(object cipherObject)
        {
            if (cipherObject == null || cipherObject == DBNull.Value)
                return string.Empty;
            string cipherText = cipherObject.ToString();
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;
            try
            {
                // Gọi phương thức giải mã AES thực tế trong dự án của bạn
                return Module_BaoMatAES.GiaiMa(cipherText);
            }
            catch
            {
                // Nếu chuỗi chưa được mã hóa từ trước (dữ liệu thô), trả về chính nó
                return cipherText;
            }
        }
        private bool DongBoDuLieuSangDanhSachGoc()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            try
            {
                // 1. Kiểm tra và đảm bảo bảng DanhSach có cột ThuTuSapXep (tránh lỗi SQLite Error 1)
                HashSet<string> cotDanhSach = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var cmd = new SqliteCommand("PRAGMA table_info(DanhSach);", conn, transaction))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string? tenCot = reader["name"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(tenCot))
                            cotDanhSach.Add(tenCot);
                    }
                }
                if (cotDanhSach.Count == 0)
                {
                    throw new InvalidOperationException("Không tìm thấy bảng DanhSach hoặc bảng DanhSach không có cột.");
                }
                // Tự động bổ sung cột ThuTuSapXep vào bảng DanhSach gốc nếu chưa có
                if (!cotDanhSach.Contains("ThuTuSapXep"))
                {
                    using (var cmdAdd = new SqliteCommand("ALTER TABLE DanhSach ADD COLUMN ThuTuSapXep INTEGER NOT NULL DEFAULT 0;", conn, transaction))
                    {
                        cmdAdd.ExecuteNonQuery();
                    }
                    cotDanhSach.Add("ThuTuSapXep");
                }
                // 2. ⭐ SỬA LẠI: Lấy dữ liệu chuẩn theo STT đã sắp xếp thực tế trên giao diện
                DataTable dtNhap = new DataTable();
                using (var cmd = new SqliteCommand("SELECT * FROM DanhSach_Nhap ORDER BY STT ASC;", conn, transaction))
                using (var reader = cmd.ExecuteReader())
                {
                    dtNhap.Load(reader);
                }
                if (dtNhap.Rows.Count == 0)
                {
                    transaction.Commit();
                    _ketQuaDongBo = $"Đã nạp {dtNhap.Rows.Count} CBCS từ DanhSach_Nhap vào DanhSach";
                    return true;
                }
                HashSet<string> cotDanhSachNhap = new HashSet<string>(
                    dtNhap.Columns.Cast<DataColumn>().Select(c => c.ColumnName),
                    StringComparer.OrdinalIgnoreCase);
                // Các cột hợp lệ để đồng bộ khi INSERT (Bỏ qua cột ID tự tăng của bảng gốc)
                List<string> danhSachCotDongBo = cotDanhSach
                    .Where(c => !c.Equals("ID", StringComparison.OrdinalIgnoreCase) && cotDanhSachNhap.Contains(c))
                    .ToList();
                // 3. Map Số hiệu với ID gốc để phân biệt CŨ / MỚI
                Dictionary<string, long> mapSoHieuToIdGoc = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                if (cotDanhSach.Contains("ID") && cotDanhSach.Contains("SoHieu"))
                {
                    using var cmdSelectGoc = new SqliteCommand("SELECT ID, SoHieu FROM DanhSach;", conn, transaction);
                    using var readerGoc = cmdSelectGoc.ExecuteReader();
                    while (readerGoc.Read())
                    {
                        if (readerGoc["ID"] == DBNull.Value || readerGoc["SoHieu"] == DBNull.Value) continue;
                        long id = Convert.ToInt64(readerGoc["ID"]);
                        string soHieuMaHoa = readerGoc["SoHieu"]?.ToString() ?? string.Empty;
                        string soHieuGoc = GiaiMaAnToan(soHieuMaHoa);
                        if (!string.IsNullOrWhiteSpace(soHieuGoc) && !mapSoHieuToIdGoc.ContainsKey(soHieuGoc))
                        {
                            mapSoHieuToIdGoc[soHieuGoc] = id;
                        }
                    }
                }
                // 4. ⭐ SỬA LẠI: Cập nhật đồng bộ cả STT lẫn ThuTuSapXep cho bản ghi gốc
                const string updateSql = @"
    UPDATE DanhSach
    SET STT = $STT,
        ThuTuSapXep = $STT
    WHERE ID = $ID;";
                // 5. Tạo câu lệnh INSERT cho bản ghi mới
                string insertColumns = string.Join(", ", danhSachCotDongBo.Select(c => $"\"{c}\""));
                string insertValues = string.Join(", ", danhSachCotDongBo.Select(c => $"${c}"));
                string insertSql = $"INSERT INTO DanhSach ({insertColumns}) VALUES ({insertValues});";
                // 6. Thực thi đồng bộ
                using var cmdUpdate = new SqliteCommand(updateSql, conn, transaction);
                cmdUpdate.Parameters.Add("$STT", SqliteType.Integer);
                cmdUpdate.Parameters.Add("$ID", SqliteType.Integer);
                using var cmdInsert = new SqliteCommand(insertSql, conn, transaction);
                foreach (string tenCot in danhSachCotDongBo)
                {
                    cmdInsert.Parameters.Add($"${tenCot}", SqliteType.Text);
                }
                foreach (DataRow rowNhap in dtNhap.Rows)
                {
                    string soHieuPlain = rowNhap.Table.Columns.Contains("SoHieu") && rowNhap["SoHieu"] != DBNull.Value
                        ? rowNhap["SoHieu"]?.ToString()?.Trim() ?? string.Empty
                        : string.Empty;
                    // ⭐ SỬA LẠI: Lấy giá trị STT thực tế
                    int sttThucTe = 0;
                    if (rowNhap.Table.Columns.Contains("STT") && rowNhap["STT"] != DBNull.Value)
                    {
                        int.TryParse(rowNhap["STT"].ToString(), out sttThucTe);
                    }
                    long existingId = 0;
                    bool daTonTai = !string.IsNullOrWhiteSpace(soHieuPlain) && mapSoHieuToIdGoc.TryGetValue(soHieuPlain, out existingId);
                    if (daTonTai)
                    {
                        // ⭐ BẢN GHI ĐÃ TỒN TẠI -> CẬP NHẬT CẢ STT VÀ THUTUSAPXEP SANG BẢNG GỐC
                        cmdUpdate.Parameters["$STT"].Value = sttThucTe;
                        cmdUpdate.Parameters["$ID"].Value = existingId;
                        cmdUpdate.ExecuteNonQuery();
                    }
                    else
                    {
                        // ⭐ BẢN GHI CHƯA TỒN TẠI -> THÊM MỚI
                        foreach (string tenCot in danhSachCotDongBo)
                        {
                            object giaTri = DBNull.Value;
                            if (rowNhap.Table.Columns.Contains(tenCot) && rowNhap[tenCot] != DBNull.Value)
                            {
                                object rawValue = rowNhap[tenCot];
                                // Các cột kiểu số INTEGER -> không mã hóa
                                if (tenCot.Equals("STT", StringComparison.OrdinalIgnoreCase) ||
                                    tenCot.Equals("ThuTuSapXep", StringComparison.OrdinalIgnoreCase))
                                {
                                    giaTri = sttThucTe;
                                }
                                else
                                {
                                    string plainText = rawValue?.ToString() ?? string.Empty;
                                    giaTri = MaHoaAnToan(plainText);
                                }
                            }
                            cmdInsert.Parameters[$"${tenCot}"].Value = giaTri;
                        }
                        cmdInsert.ExecuteNonQuery();
                    }
                }
                // ⭐ Chỉ ghi kết quả vào RAM sau khi toàn bộ dữ liệu đã Commit thành công
                transaction.Commit();
                _ketQuaDongBo = $"Đã nạp {dtNhap.Rows.Count} CBCS từ DanhSach_Nhap vào DanhSach";
                return true;
            }
            catch
            {
                try { transaction.Rollback(); } catch { }
                throw;
            }
        }
        private void xoaTimKiem_ToolStripMenuItem_Click(object sender, EventArgs e)
        => kryptonButton_LamMoiCacOTimKiem.PerformClick();
        private void lamMoiTrang_ToolStripMenuItem_Click(object sender, EventArgs e)
            => kryptonButton_RefershCSDL.PerformClick();
        private void diChuyenLen_ToolStripMenuItem_Click(object sender, EventArgs e)
            => kryptonButton1_DiChuyenDataCBCSLen.PerformClick();
        private void diChuyenXuong_ToolStripMenuItem_Click(object sender, EventArgs e)
            => kryptonButton1_DiChuyenDataCBCSXuong.PerformClick();
        //private void themCBCS_ToolStripMenuItem_Click(object sender, EventArgs e)
        //    => btnThemMoi.PerformClick();
        private void dongFrom_ToolStripMenuItem_Click(object sender, EventArgs e)
            => kryptonButton1_DongForm.PerformClick();
        private void xuatTepMau_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_dangXuLyHuongDan) return;
            _dangXuLyHuongDan = true;
            try
            {
                string msg = "QUY TRÌNH THÊM DỮ LIỆU TỪ TỆP EXCEL:\n\n" +
                             "  1. Xuất tệp Excel mẫu từ hệ thống.\n" +
                             "  2. Nhập thông tin vào tệp Excel vừa xuất.\n" +
                             "  3. Tải tệp đó ngược lại vào phần mềm.\n\n" +
                             "💡 Mẹo thao tác: Nhấn chuột phải vào lưới danh sách → Chọn \"Nhập kết quả phân loại từ đơn vị\".\n\n" +
                             $" {Module_HeThong.Tu_dong_chi} có muốn bắt đầu bằng việc xuất tệp Excel mẫu ngay bây giờ không?";
                // Hiển thị hộp thoại xác nhận Yes/No
                if (!HienThiFormAo_XacNhan("HƯỚNG DẪN THÊM DỮ LIỆU", msg)) return;
                // 🔥 GỌI HÀM XUẤT FILE EXCEL MẪU (sử dụng biến _dtGoc chuẩn của Form)
                // Tham số thứ 2 là 'true' để ép xuất ra file mẫu
                Module_XuatNhapDuLieuThiDua.ThucThiXuatExcel(this, true, _dtGoc);
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM ?? "Admin", "Thêm dữ liệu cán bộ", $"Vào lúc {DateTime.Now:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                HienThiFormAo_Loi("LỖI HỆ THỐNG", $"Đã xảy ra sự cố trong quá trình hướng dẫn:\n{ex.Message}");
            }
            finally
            {
                _dangXuLyHuongDan = false;
            }
        }
        // Struct / Class hỗ trợ đọc dữ liệu tạm
        private class DataRowNhap
        {
            public int ID { get; set; }
            public string HoVaTen { get; set; }
            public string SoHieu { get; set; }
            public string NamSinh { get; set; }
            public string QueQuan { get; set; }
            public string NgayVaoCAND { get; set; }
            public string CapBac { get; set; }
            public string ChucVu { get; set; }
            public string DonVi { get; set; }
            public string PhanLoai { get; set; }
            public string GhiChu { get; set; }
        }
        private bool _tuDongNhapExcelSauKhiDongForm;
        private void nhapDuLieuTuTepExcel_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _tuDongNhapExcelSauKhiDongForm = true;
            kryptonButton1_DongForm.PerformClick();
        }
        private async void Form60_NhapLieuCBCS_FormClosed(object sender, FormClosedEventArgs e)
        {
            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            if (formCha != null)
            {
                formCha.CapNhatTieuDe("Trang phân loại thi đua");
            }
            bool yeuCauNhapExcel = _tuDongNhapExcelSauKhiDongForm;
            _tuDongNhapExcelSauKhiDongForm = false;
            if (_coThayDoiDuLieu)
            {
                try
                {
                    DongBoDuLieuSangDanhSachGoc();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Lỗi tự động đồng bộ khi đóng form: {ex.Message}",
                        "Cảnh báo CSDL",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    if (!yeuCauNhapExcel)
                        return;
                }
            }
            if (!yeuCauNhapExcel)
                return;
            var frm6 = Application.OpenForms
                .OfType<Form6_XuLyData>()
                .FirstOrDefault();
            if (frm6 == null || frm6.IsDisposed || frm6.Disposing)
            {
                Debug.WriteLine("Không tìm thấy Form6_XuLyData.");
                return;
            }
            try
            {
                if (!frm6.IsHandleCreated)
                {
                    Debug.WriteLine("Form6 chưa có Handle.");
                    return;
                }
                await Task.Delay(300);
                if (frm6.IsDisposed || frm6.Disposing)
                    return;
                frm6.YeuCauNhapDuLieuTuFileExcel();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi yêu cầu Form6 nhập Excel: " + ex);
            }
        }
        private void CapNhatLabelTheoPhienBan()
        {
            try
            {
                bool laTanBinh = KiemTraLaTanBinh();
                string doiTuongThuong = laTanBinh ? "tân binh" : "CBCS";
                groupBox1_TimKiemThongTinCBCS.Text = $"1. Tìm kiếm thông tin {doiTuongThuong}";
                groupBox3_ThongTinCBCS.Text = $"3. Nhập dữ liệu để thêm mới thông tin {doiTuongThuong}";
                groupBox1.Text = $"4. Danh sách thông tin {doiTuongThuong}";
                textBox_SoHieu.ReadOnly = false; // CBCS được nhập Số hiệu
                textBox_SoHieu.Enabled = true;
                // Áp dụng màu nền nhạt (#C6EFCE) cho các ô bị khóa/chỉ đọc
                Color mauNen = laTanBinh ? Color.FromArgb(198, 239, 206) : Color.White;
                if (textBox_SoHieu is Krypton.Toolkit.KryptonTextBox kSoHieu)
                    kSoHieu.StateCommon.Back.Color1 = mauNen;
                else
                    textBox_SoHieu.BackColor = mauNen;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Lỗi khi cập nhật giao diện theo phiên bản:\n" + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
        private void KryptonDataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow row = kryptonDataGridView1.Rows[e.RowIndex];
            if (row.DataBoundItem is DataRowView drv)
            {
                // Chuyển DataRow sang DTO
                var dto = new CanBoDTO
                {
                    ID = drv["ID"] != DBNull.Value ? Convert.ToInt32(drv["ID"]) : 0,
                    HoVaTen = drv["HoVaTen"]?.ToString() ?? "",
                    SoHieu = drv["SoHieu"]?.ToString() ?? "",
                    NamSinh = drv["NamSinh"]?.ToString() ?? "",
                    QueQuan = drv["QueQuan"]?.ToString() ?? "",
                    NgayVaoCAND = drv["NgayVaoCAND"]?.ToString() ?? "",
                    CapBac = drv["CapBac"]?.ToString() ?? "",
                    ChucVu = drv["ChucVu"]?.ToString() ?? "",
                    DonVi = drv["DonVi"]?.ToString() ?? "",
                    PhanLoai = drv["PhanLoai"]?.ToString() ?? "",
                    GhiChu = drv["GhiChu"]?.ToString() ?? ""
                };
                // Lưu ID đang chọn
                _currentSelectedID = dto.ID;
                // Đổ dữ liệu lên các Control
                textBox_HoVaTen.Text = dto.HoVaTen;
                textBox_SoHieu.Text = dto.SoHieu;
                textBox_NamSinh.Text = dto.NamSinh;
                textBox_QueQuan.Text = dto.QueQuan;
                textBox_NgayVaoCAND.Text = dto.NgayVaoCAND;
                textBox_CapBac.Text = dto.CapBac;
                textBox_GhiChu.Text = dto.GhiChu;
                // Gán giá trị an toàn cho các ComboBox
                SetComboBoxValue(comboBox_ChucVu, dto.ChucVu);
                SetComboBoxValue(comboBox_DonVi, dto.DonVi);
                SetComboBoxValue(comboBox_PhanLoai, dto.PhanLoai);
                // BỔ SUNG: Nếu là Tân binh thì khóa ô Số hiệu để bảo vệ mã ID cấp phát
                if (KiemTraLaTanBinh())
                {
                    textBox_SoHieu.ReadOnly = true;
                }
                else
                {
                    textBox_SoHieu.ReadOnly = false;
                }
            }
        }
        private async void btnThemMoi_Click(object sender, EventArgs e)
        {
            bool laTanBinh = KiemTraLaTanBinh();
            // 0. SỬA TẠI ĐÂY: Khi thêm mới Tân binh, LUÔN CẤP ID MỚI BẤT KỂ TEXTBOX ĐANG CÓ ID CŨ HAY KHÔNG
            if (laTanBinh)
            {
                string idMoi = SinhSoHieuTanBinhTiepTheo();
                textBox_SoHieu.Text = idMoi;
            }
            // Đọc lại DTO sau khi đã gán ID mới vào textBox_SoHieu
            var dto = LayDuLieuTuForm();
            // 1. Kiểm tra Họ và tên
            if (string.IsNullOrWhiteSpace(dto.HoVaTen))
            {
                MessageBox.Show($"Vui lòng nhập Họ và tên {(laTanBinh ? "tân binh" : "CBCS")} cần thêm mới!",
                                "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox_HoVaTen.Focus();
                return;
            }
            // 2. Bắt buộc nhập Số hiệu CHỈ KHI LÀ CBCS
            if (!laTanBinh && string.IsNullOrWhiteSpace(dto.SoHieu))
            {
                MessageBox.Show("Vui lòng nhập Số hiệu CAND!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox_SoHieu.Focus();
                return;
            }
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();
                using var transaction = conn.BeginTransaction();
                // 3. Kiểm tra trùng lặp dữ liệu
                if (!laTanBinh)
                {
                    const string checkQuery = @"
                SELECT COUNT(1) 
                FROM DanhSach_Nhap 
                WHERE TRIM(SoHieu) = TRIM($SoHieu);";
                    using (var checkCmd = new SqliteCommand(checkQuery, conn, transaction))
                    {
                        checkCmd.Parameters.AddWithValue("$SoHieu", dto.SoHieu.Trim());
                        if (Convert.ToInt64(checkCmd.ExecuteScalar()) > 0)
                        {
                            MessageBox.Show($"Số hiệu CAND [{dto.SoHieu}] đã tồn tại!", "Trùng dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            textBox_SoHieu.Focus();
                            return;
                        }
                    }
                }
                else
                {
                    const string checkTBQuery = @"
                SELECT COUNT(1) 
                FROM DanhSach_Nhap 
                WHERE TRIM(HoVaTen) = TRIM($HoVaTen) 
                  AND TRIM(NamSinh) = TRIM($NamSinh) 
                  AND TRIM(DonVi) = TRIM($DonVi);";
                    using (var checkTBCmd = new SqliteCommand(checkTBQuery, conn, transaction))
                    {
                        checkTBCmd.Parameters.AddWithValue("$HoVaTen", dto.HoVaTen.Trim());
                        checkTBCmd.Parameters.AddWithValue("$NamSinh", (dto.NamSinh ?? "").Trim());
                        checkTBCmd.Parameters.AddWithValue("$DonVi", (dto.DonVi ?? "").Trim());
                        if (Convert.ToInt64(checkTBCmd.ExecuteScalar()) > 0)
                        {
                            MessageBox.Show($"Tân binh [{dto.HoVaTen}] thuộc đơn vị [{dto.DonVi}] đã tồn tại trong danh sách!",
                                            "Trùng dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            textBox_HoVaTen.Focus();
                            return;
                        }
                    }
                }
                // 4. LẤY STT LỚN NHẤT CỦA ĐƠN VỊ ĐÓ
                string tenDonVi = (dto.DonVi ?? "").Trim();
                int targetSTT = 0;
                const string maxSttDonViQuery = @"
            SELECT MAX(STT) 
            FROM DanhSach_Nhap 
            WHERE TRIM(DonVi) = TRIM($DonVi);";
                using (var maxCmd = new SqliteCommand(maxSttDonViQuery, conn, transaction))
                {
                    maxCmd.Parameters.AddWithValue("$DonVi", tenDonVi);
                    object result = maxCmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        targetSTT = Convert.ToInt32(result) + 1;
                    }
                    else
                    {
                        const string maxAllQuery = "SELECT IFNULL(MAX(STT), 0) + 1 FROM DanhSach_Nhap;";
                        using var maxAllCmd = new SqliteCommand(maxAllQuery, conn, transaction);
                        targetSTT = Convert.ToInt32(maxAllCmd.ExecuteScalar());
                    }
                }
                // 5. ĐẨY CÁC DÒNG CÓ STT >= targetSTT TĂNG LÊN +1
                const string shiftQuery = @"
            UPDATE DanhSach_Nhap 
            SET STT = STT + 1, 
                ThuTuSapXep = ThuTuSapXep + 1 
            WHERE STT >= $TargetSTT;";
                using (var shiftCmd = new SqliteCommand(shiftQuery, conn, transaction))
                {
                    shiftCmd.Parameters.AddWithValue("$TargetSTT", targetSTT);
                    shiftCmd.ExecuteNonQuery();
                }
                // 6. CHÈN BẢN GHI MỚI VÀO VỊ TRÍ targetSTT
                const string insertQuery = @"
            INSERT INTO DanhSach_Nhap (
                STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND,
                CapBac, ChucVu, DonVi, PhanLoai, GhiChu, ThuTuSapXep, ThoiGianTao
            ) 
            VALUES (
                $STT, $HoVaTen, $SoHieu, $NamSinh, $QueQuan, $NgayVaoCAND,
                $CapBac, $ChucVu, $DonVi, $PhanLoai, $GhiChu, $STT, $ThoiGianTao
            );";
                using (var cmd = new SqliteCommand(insertQuery, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("$STT", targetSTT);
                    cmd.Parameters.AddWithValue("$HoVaTen", dto.HoVaTen ?? "");
                    cmd.Parameters.AddWithValue("$SoHieu", dto.SoHieu ?? "");
                    cmd.Parameters.AddWithValue("$NamSinh", dto.NamSinh ?? "");
                    cmd.Parameters.AddWithValue("$QueQuan", dto.QueQuan ?? "");
                    cmd.Parameters.AddWithValue("$NgayVaoCAND", dto.NgayVaoCAND ?? "");
                    cmd.Parameters.AddWithValue("$CapBac", dto.CapBac ?? "");
                    cmd.Parameters.AddWithValue("$ChucVu", dto.ChucVu ?? "");
                    cmd.Parameters.AddWithValue("$DonVi", dto.DonVi ?? "");
                    cmd.Parameters.AddWithValue("$PhanLoai", dto.PhanLoai ?? "");
                    cmd.Parameters.AddWithValue("$GhiChu", dto.GhiChu ?? "");
                    cmd.Parameters.AddWithValue("$ThoiGianTao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                }
                // 7. TỔNG CHUẨN HÓA LẠI STT THEO CHUỖI LIÊN TỤC
                const string reindexSttQuery = @"
            WITH Ranked AS (
                SELECT ROWID, ROW_NUMBER() OVER (ORDER BY STT ASC) AS NewSTT 
                FROM DanhSach_Nhap
            )
            UPDATE DanhSach_Nhap 
            SET STT = (SELECT NewSTT FROM Ranked WHERE Ranked.ROWID = DanhSach_Nhap.ROWID), 
                ThuTuSapXep = (SELECT NewSTT FROM Ranked WHERE Ranked.ROWID = DanhSach_Nhap.ROWID);";
                using (var reindexCmd = new SqliteCommand(reindexSttQuery, conn, transaction))
                {
                    reindexCmd.ExecuteNonQuery();
                }
                transaction.Commit();
                // Gắn cờ và nạp lại dữ liệu bảng
                _coThayDoiDuLieu = true;
                LoadDuLieuLenGrid();
                // Định vị dòng mới thêm
                if (!laTanBinh)
                {
                    ChonDongVuaThemTheoSoHieu(dto.SoHieu);
                }
                else
                {
                    if (_dtGoc != null && _dtGoc.Rows.Count > 0)
                    {
                        long maxId = _dtGoc.AsEnumerable().Max(r => Convert.ToInt64(r["ID"]));
                        DatLaiViTriChonTheoID((int)maxId);
                    }
                }
                CapNhatThanhTrangThaiStatusStrip(kryptonDataGridView1.Rows.Count);
                XoaTrangCacControl();
                string noiDungThongBao = $"Thêm dữ liệu {(laTanBinh ? "tân binh" : "cán bộ")} thành công: {dto.HoVaTen}";
                toolStripStatusLabel1.Text = noiDungThongBao;
                Module_NhatKy.GhiNhatKy(
                    Module_TaiKhoan.TenTaiKhoan_RAM ?? "Admin",
                    $"Thêm dữ liệu {(laTanBinh ? "tân binh" : "cán bộ")}",
                    $"{noiDungThongBao} lúc {DateTime.Now:HH:mm:ss}"
                );
                await Task.Delay(400);
                CuaSo_KhoiTaoStatusStrip();
            }
            catch (Exception ex)
            {
                Module_NhatKy.GhiNhatKy(
                    Module_TaiKhoan.TenTaiKhoan_RAM ?? "Admin",
                    $"Thêm dữ liệu {(laTanBinh ? "tân binh" : "cán bộ chiến sĩ")} bị lỗi: " + ex.Message,
                    $"Vào lúc {DateTime.Now:HH:mm:ss}"
                );
                MessageBox.Show($"Lỗi khi lưu dữ liệu: {ex.Message}", "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    } //Ngoài luồng
    public class CanBoDTO
    {
        public int ID { get; set; }
        public int STT { get; set; }
        public string HoVaTen { get; set; }
        public string SoHieu { get; set; }
        public string NamSinh { get; set; }
        public string QueQuan { get; set; }
        public string NgayVaoCAND { get; set; }
        public string CapBac { get; set; }
        public string ChucVu { get; set; }
        public string DonVi { get; set; }
        public string PhanLoai { get; set; }
        public string GhiChu { get; set; }
        public int ThuTuSapXep { get; set; }
    }
}