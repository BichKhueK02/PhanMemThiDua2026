using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Text;

namespace PhanMemThiDua2026
{
    public partial class Form12 : Form
    {
        private bool _dataChanged = false;
        private bool _isLoading = true;
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private readonly string _csdl3Path = Module_DanduongGPS.DuongDanCSDL3;
        private const int MAX_TOMTAT = 850;
        private const int ABSOLUTE_MAX = 2000; // Giới hạn tuyệt đối ksy tự cấm lưu Meo cam chia sẽ
        private bool _hasLoaded = false;
        private const float FONT_DEFAULT = 14f;
        private float _currentFontSize = FONT_DEFAULT;
        private const float FONT_SIZE_MIN = 8f;
        private const float FONT_SIZE_MAX = 20f;
        private const float FONT_SIZE_STEP = 1f;
        private Font? _dynamicRichTextFont;
        private int _isChangingFont = 0;
        private bool _isDoiTuongChanged = false;       // cờ đánh dấu thay đổi đối tượng phần mềm
        private string _doiTuongBanDau = string.Empty; // lưu giá trị ban đầu của combobox
        private Dictionary<KryptonButton, System.Windows.Forms.Label> _menuMap;
        private Color _defaultLabelColor;
        public Form12()
        {
            InitializeComponent();
            richTextBox1_TomTatGhiChu.HideSelection = false;
            this.UpdateStyles();
        }
        private void Form12_Load(object sender, EventArgs e)
        {
            if (_hasLoaded) return;
            _isLoading = true;

            LoadComboBoxCauHinh();
            LoadComboBoxTuDongXoa();
            Module_DonVi.KhoiTao();

            // ⭐ SỬA LỖI LOAD 1: Phải đảm bảo có Items trước, và dùng .Text thay vì .SelectedItem
            if (comboBox1_ChonSuKienThoat != null)
            {
                if (comboBox1_ChonSuKienThoat.Items.Count == 0)
                {
                    comboBox1_ChonSuKienThoat.Items.AddRange(new string[] { "Thoát ngay", "Đóng về khay hệ thống" });
                }
                comboBox1_ChonSuKienThoat.Text = LoadSuKienThoat();
            }

            InitToolTips();
            NapDanhSachNam();
            LoadNamHeThong();
            // 1. Nạp danh sách trắng trước
            NapDanhSachKyHieuChung();
            // 2. Ép dữ liệu từ DB lên
            LoadFromSQLite();
            // Thêm vào Form12_Load
            LoadCauHinhXemHuongDan();
            // 3. Gắn sự kiện
            GanSuKien();
            _currentFontSize = richTextBox1_TomTatGhiChu.Font.Size;
            // ⭐ BƯỚC 4: TRỊ DỨT ĐIỂM HIGHLIGHT (Tuyệt chiêu)
            this.BeginInvoke(new Action(() =>
            {
                // Nhảy focus ra chỗ khác (Nút lưu)
                if (kryptonButton_LuuThongTin != null && kryptonButton_LuuThongTin.CanFocus)
                {
                    kryptonButton_LuuThongTin.Focus();
                }

                // Trực tiếp tước quyền chọn văn bản của CÁC ComboBox
                comboBox_KyHieu_TenTrungDoan.SelectionLength = 0;
                comboBox_KyHieu_TenTieuDoan.SelectionLength = 0;

                if (comboBox_TenTieuDoan != null) comboBox_TenTieuDoan.SelectionLength = 0;
                if (comboBox1_NamHienTai != null) comboBox1_NamHienTai.SelectionLength = 0;
            }));

            // 👉 BỔ SUNG: NẠP LẠI GIÁ TRỊ CẤU HÌNH TỪ CSDL VÀO COMBOBOX
            // 👉 BỔ SUNG: NẠP LẠI GIÁ TRỊ CẤU HÌNH TỪ CSDL VÀO COMBOBOX
            if (comboBox1_ThoiGianThayDoiAnh != null)
            {
                if (comboBox1_ThoiGianThayDoiAnh.Items.Count == 0)
                {
                    comboBox1_ThoiGianThayDoiAnh.Items.AddRange(new string[] { "Mặc định", "15 giây", "30 giây", "1 phút" });
                }
                comboBox1_ThoiGianThayDoiAnh.Text = Module_HinhAnhTrangChu.DocCauHinhThoiGian();
            }
            // Gán tên máy tính vào TextBox khi giao diện vừa khởi tạo
            _isLoading = false;
            _hasLoaded = true;
        }
        private void Form12_Shown(object sender, EventArgs e)
        {
            label7.Text = "";
            kryptonButton_CapNhatDanhSachChiHuyD.AutoSize = false;

            // Giữ lại để phòng hờ trường hợp người dùng Alt+Tab
            if (kryptonButton_LuuThongTin != null && kryptonButton_LuuThongTin.CanFocus)
            {
                kryptonButton_LuuThongTin.Focus();
            }
            else
            {
                this.ActiveControl = null;
            }
        }
        private void GanSuKien()
        {
            textBox_TrungDoanCSCD.TextChanged += OnDataChanged;
            comboBox_TenTieuDoan.TextChanged += OnDataChanged;
            comboBox_SoLuongDongChoPhepHienThi.TextChanged += OnDataChanged;
            // ✅ THÊM DÒNG NÀY
            comboBox_DoiTuongPhanMem.TextChanged += comboBox_DoiTuongPhanMem_TextChanged;
            // 👉 Đăng ký sự kiện đổi Tab tại đây (Thay 'tabControl1' bằng tên chuẩn xác trên Form của bạn)
            tabControl1.SelectedIndexChanged += tabControl1_SelectedIndexChanged;
        }
        private void OnDataChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;
            _dataChanged = true;
        }
        private void InitToolTips()
        {
            try
            {
                // =====================================================
                // CẤU HÌNH CHUNG
                // =====================================================
                toolTip1.IsBalloon = true;
                toolTip1.ToolTipTitle = "Chức năng";
                toolTip1.ToolTipIcon = ToolTipIcon.Info;

                // =====================================================
                // DANH SÁCH TOOLTIP
                // =====================================================
                var toolTips = new Dictionary<Control, string>()
        {
            { kryptonButton_LuuThongTin, "Lưu thông tin chỉnh sửa" },
            { kryptonButton1_CapNhatDanhSachDonVi, "Cập nhật danh sách đơn vị" },
            { kryptonButton1_CapNhatChucVu, "Cập nhật danh sách chức vụ" },
            { kryptonButton_CapNhatDanhSachChiHuyD, "Cập nhật danh sách chỉ huy ký duyệt" },
            { kryptonButton1_SaoLuu, "Sao lưu dữ liệu hệ thống" },
            { kryptonButton1_Khoiphuc, "Khôi phục dữ liệu từ bản sao lưu" },
            { kryptonButton_LuuCauHinh, "Lưu cấu hình" },
            { kryptonButton1_CaiDatFileExcel, "Căn chỉnh in ấn tệp excel" },
            { kryptonButton2_TangCoChuRichText, "Tăng cỡ chữ" },
            { kryptonButton2_GiamCoChuRichText, "Giảm cỡ chữ" },
            { kryptonButton2_ChuyenGiaoDuLieu, "Chuyển giao dữ liệu khi chuyển sang phiên bản phần mềm mới" },
            { kryptonButton1_BoQuaKiemTraTyLeDoViDacBiet, "Chọn đơn vị có thể bỏ qua việc tính tỷ lệ % ở Bảng 3 - Trang chủ" },
            { kryptonButton1_CaiDatTyLePhanTramE29, "Cài đặt tỷ lệ % theo quy định của Trung đoàn" }
        };

                // =====================================================
                // GÁN TOOLTIP AN TOÀN
                // =====================================================
                foreach (var item in toolTips)
                {
                    try
                    {
                        Control? control = item.Key;

                        if (control == null)
                            continue;

                        if (control.IsDisposed)
                            continue;

                        toolTip1.SetToolTip(control, item.Value);
                    }
                    catch
                    {
                        // Không cho 1 control lỗi làm sập toàn bộ UI
                    }
                }
            }
            catch
            {
                // Chặn crash toàn hệ thống UI
            }
        }
        private void LoadComboBoxTuDongXoa()
        {
            comboBox1_TuDongXoaNhatKy.Items.Clear();
            comboBox1_TuDongXoaNhatKy.Items.AddRange(new object[]
            {
            "Không xóa",
            "1000 dòng xóa tự động",
            "5000 dòng xóa tự động",
            "10000 dòng xóa tự động"
            });

            comboBox1_TuDongXoaNhatKy.SelectedItem = LoadTuDongXoaTuCSDL();
        }
        private void MenuButton_Highlight(object sender, EventArgs e)
        {
            // BỔ SUNG: Chặn đứng lỗi nếu _menuMap chưa được khởi tạo
            if (_menuMap == null)
                return;

            if (sender is not KryptonButton clickedButton)
                return;

            foreach (var pair in _menuMap)
            {
                if (pair.Value == null) continue;

                pair.Value.ForeColor = pair.Key == clickedButton ? Color.Red : _defaultLabelColor;
            }
        }
        private void comboBox1_TuDongXoaNhatKy_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;

            if (comboBox1_TuDongXoaNhatKy.SelectedItem != null)
            {
                LuuTuDongXoaVaoCSDL(comboBox1_TuDongXoaNhatKy.SelectedItem.ToString());
            }
        }
        private void richTextBox1_TomTatGhiChu_TextChanged(object sender, EventArgs e)
        {
            if (_isLoading) return; // Chặn khi load dữ liệu

            int currentLength = richTextBox1_TomTatGhiChu.TextLength;

            // Nếu vượt quá 2000, cắt bớt ngay lập tức
            if (currentLength > ABSOLUTE_MAX)
            {
                int selStart = richTextBox1_TomTatGhiChu.SelectionStart;
                richTextBox1_TomTatGhiChu.Text = richTextBox1_TomTatGhiChu.Text.Substring(0, ABSOLUTE_MAX);
                // Giữ con trỏ ở cuối
                richTextBox1_TomTatGhiChu.SelectionStart = Math.Min(selStart, ABSOLUTE_MAX);
                currentLength = ABSOLUTE_MAX;
                System.Media.SystemSounds.Beep.Play(); // cảnh báo
            }

            // Hiển thị nhãn cảnh báo
            label7.Text = $"Số ký tự: {currentLength}/{MAX_TOMTAT} (tối đa tuyệt đối {ABSOLUTE_MAX})";

            if (currentLength <= MAX_TOMTAT)
                label7.ForeColor = Color.Green;
            else if (currentLength <= ABSOLUTE_MAX)
                label7.ForeColor = Color.Orange; // > MAX nhưng <= 2000: cảnh báo
            else
                label7.ForeColor = Color.Red; // thực tế không xảy ra vì đã cắt

            if (!label7.Visible)
                label7.Visible = true;

            _dataChanged = true;
        }
        // Ngăn nhập nếu vượt quá 2000 ký tự
        private void richTextBox1_TomTatGhiChu_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (richTextBox1_TomTatGhiChu.TextLength >= ABSOLUTE_MAX && !char.IsControl(e.KeyChar))
            {
                e.Handled = true; // không cho nhập thêm
                System.Media.SystemSounds.Beep.Play(); // cảnh báo âm thanh
            }
        }
        private void LuuTuDongXoaVaoCSDL(string luaChon)
        {
            try
            {
                using var cn = new SqliteConnection($"Data Source={_csdl3Path}");
                cn.Open();

                using (var cmdCreate = cn.CreateCommand())
                {
                    cmdCreate.CommandText = @"
                    CREATE TABLE IF NOT EXISTS TuDong_XoaNhatKy (
                        ID INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        Chọn_GiaiTri TEXT
                    );";
                    cmdCreate.ExecuteNonQuery();
                }

                using var cmdUpdate = cn.CreateCommand();
                cmdUpdate.CommandText = "UPDATE TuDong_XoaNhatKy SET Chọn_GiaiTri=@luaChon WHERE ID=1";
                cmdUpdate.Parameters.AddWithValue("@luaChon", luaChon);
                int rowsAffected = cmdUpdate.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    using var cmdInsert = cn.CreateCommand();
                    cmdInsert.CommandText = "INSERT INTO TuDong_XoaNhatKy (Chọn_GiaiTri) VALUES (@luaChon)";
                    cmdInsert.Parameters.AddWithValue("@luaChon", luaChon);
                    cmdInsert.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LuuTuDongXoaVaoCSDL] Lỗi: {ex.Message}");
            }
        }
        private string LoadTuDongXoaTuCSDL()
        {
            string luaChon = "Không xóa";

            try
            {
                using var cn = new SqliteConnection($"Data Source={_csdl3Path}");
                cn.Open();

                using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT Chọn_GiaiTri FROM TuDong_XoaNhatKy WHERE ID=1";
                var result = cmd.ExecuteScalar();
                if (result != null)
                    luaChon = result.ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoadTuDongXoaTuCSDL] Lỗi: {ex.Message}");
            }

            return luaChon;
        }
        private void CheckBox1_ChoPhepGoiYTenTaiKhoan_CheckedChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;

            _dataChanged = true;
            checkBox1_ChoPhepGoiYTenTaiKhoan.ForeColor =
                checkBox1_ChoPhepGoiYTenTaiKhoan.Checked ? Color.Green : Color.Red;
        }
        //Hỗ trợ lưu cấu hình xem hướng dẫn sử dụng vào SQLite
        private static bool KiemTraCotTonTai(
    SqliteConnection cn,
    SqliteTransaction tran,
    string tableName,
    string columnName)
        {
            using var cmd = cn.CreateCommand();
            cmd.Transaction = tran;

            cmd.CommandText = $"PRAGMA table_info({tableName})";

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                string col = reader["name"]?.ToString() ?? "";

                if (col.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
        private static void DamBaoCotTonTai(
    SqliteConnection cn,
    SqliteTransaction tran,
    string tableName,
    string columnName,
    string sqliteType = "TEXT")
        {
            if (KiemTraCotTonTai(cn, tran, tableName, columnName))
                return;

            using var cmd = cn.CreateCommand();
            cmd.Transaction = tran;

            cmd.CommandText =
                $"ALTER TABLE {tableName} ADD COLUMN {columnName} {sqliteType}";

            cmd.ExecuteNonQuery();
        }
        private void LuuCauHinhXemHuongDan()
        {
            try
            {
                string cheDo = comboBox1_XemHuongDanSuDung.SelectedItem?.ToString() ?? "Chế độ web";

                using var cn = new SqliteConnection($"Data Source={_csdl2Path}");
                cn.Open();
                using var cmd = cn.CreateCommand();
                cmd.CommandText = "UPDATE CheDo_XemHuongDan SET CheDoXem_HuongDanSD = @val WHERE ID = 1";
                cmd.Parameters.AddWithValue("@val", cheDo);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu cấu hình xem hướng dẫn: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void LoadCauHinhXemHuongDan()
        {
            try
            {
                using var cn = new SqliteConnection($"Data Source={_csdl2Path}");
                cn.Open();

                // 1. Đảm bảo bảng tồn tại
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS CheDo_XemHuongDan (
                                    ID INTEGER NOT NULL PRIMARY KEY, 
                                    CheDoXem_HuongDanSD TEXT);";
                    cmd.ExecuteNonQuery();
                }

                // 2. Đảm bảo có bản ghi ID=1
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "INSERT OR IGNORE INTO CheDo_XemHuongDan (ID, CheDoXem_HuongDanSD) VALUES (1, 'Chế độ web')";
                    cmd.ExecuteNonQuery();
                }

                // 3. Đọc dữ liệu
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "SELECT CheDoXem_HuongDanSD FROM CheDo_XemHuongDan WHERE ID = 1";
                    var result = cmd.ExecuteScalar();
                    string cheDo = result?.ToString() ?? "Chế độ web";

                    // Set cho ComboBox
                    if (comboBox1_XemHuongDanSuDung.Items.Contains(cheDo))
                        comboBox1_XemHuongDanSuDung.SelectedItem = cheDo;
                    else
                        comboBox1_XemHuongDanSuDung.SelectedIndex = 0; // Mặc định nếu lỗi
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi load cấu hình hướng dẫn: {ex.Message}");
            }
        }
        private static bool _pragmaInitialized = false;
        private static void ApplySQLitePragma(SqliteConnection cn)
        {
            using var cmd = cn.CreateCommand();

            cmd.CommandText = @"
PRAGMA journal_mode=WAL;
PRAGMA synchronous=NORMAL;
PRAGMA foreign_keys=ON;
PRAGMA temp_store=MEMORY;
PRAGMA busy_timeout=15000;
";

            cmd.ExecuteNonQuery();
        }
        private string BuildConnectionString(string dbPath)
        {
            return new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = true,
                Cache = SqliteCacheMode.Private,
                DefaultTimeout = 15
            }.ToString();
        }
        public void LoadFromSQLite()
        {
            if (!File.Exists(_csdl2Path)) return;

            _isLoading = true;

            try
            {
                using var cn = new SqliteConnection(BuildConnectionString(_csdl2Path));
                cn.Open();
                ApplySQLitePragma(cn);
                _pragmaInitialized = true;

                using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT * FROM ThongTin WHERE ID = 1 LIMIT 1;";

                using var reader = cmd.ExecuteReader();
                if (!reader.Read()) return;

                // 1. Đọc dữ liệu từ DB ra biến tạm (Đã bổ sung thuộc tính SoLan)
                var data = new
                {
                    TenTD = SafeDecrypt(reader["TenTrungDoan"]),
                    TenTieuD = SafeDecrypt(reader["TenTieuDoan"]),
                    TomTat = SafeDecrypt(reader["TomTatGhiChu"]),
                    SoLan = SafeDecrypt(reader["SoLanChoPhepHienThi"]), // ĐÃ BỔ SUNG Ở ĐÂY
                    GoiYPass = SafeDecrypt(reader["ChoPhepGoiYMatKhau"]),
                    GoiYAnh = SafeDecrypt(reader["ChoPhepDupChuotVaoAnh_GoiYMatKhau"]),
                    // ⭐ ĐỌC RA TỪ CSDL
                    TenTDDong1 = SafeDecrypt(reader["textBox1_TenTrungDoanDong1"]),
                    KyHieuBC = SafeDecrypt(reader["KyHieuBaoCao"])
                };

                // Load Đối tượng (từ bảng PhienBan_DoiTuong)
                string doiTuong = string.Empty;
                using (var cmd2 = cn.CreateCommand())
                {
                    cmd2.CommandText = "SELECT DoiTuong FROM PhienBan_DoiTuong WHERE ID=1";
                    var dt = cmd2.ExecuteScalar();
                    doiTuong = dt != null ? SafeDecrypt(dt) : "";
                }

                // 2. Cập nhật UI an toàn
                if (IsDisposed || !IsHandleCreated) return;

                BeginInvoke(new Action(() =>
                {
                    if (IsDisposed) return;

                    // 1. TẠM GỠ SỰ KIỆN (Chặn việc nạp dữ liệu kích hoạt false trigger)
                    comboBox_DoiTuongPhanMem.TextChanged -= comboBox_DoiTuongPhanMem_TextChanged;
                    comboBox_SoLuongDongChoPhepHienThi.TextChanged -= OnDataChanged;
                    richTextBox1_TomTatGhiChu.TextChanged -= richTextBox1_TomTatGhiChu_TextChanged;
                    checkBox1_ChoPhepGoiYTenTaiKhoan.CheckedChanged -= CheckBox1_ChoPhepGoiYTenTaiKhoan_CheckedChanged;
                    checkBox1_ChoPhepDupChuotVaoAnhGoiYssaP.CheckedChanged -= checkBox1_ChoPhepDupChuotVaoAnhGoiYssaP_CheckedChanged;

                    // 2. GÁN DỮ LIỆU
                    textBox_TrungDoanCSCD.Text = data.TenTD;
                    comboBox_TenTieuDoan.Text = data.TenTieuD;
                    // ⭐ GÁN LÊN GIAO DIỆN
                    if (textBox1_TenTrungDoanDong1 != null) textBox1_TenTrungDoanDong1.Text = data.TenTDDong1;
                    if (textBox1_KyHieuBaoCao != null) textBox1_KyHieuBaoCao.Text = data.KyHieuBC;
                    // Xử lý đối tượng phần mềm an toàn
                    comboBox_DoiTuongPhanMem.Text = doiTuong;
                    _doiTuongBanDau = doiTuong; // Đảm bảo gán biến gốc ngay lập tức

                    if (!string.IsNullOrEmpty(data.SoLan))
                    {
                        if (comboBox_SoLuongDongChoPhepHienThi.Items.Contains(data.SoLan))
                            comboBox_SoLuongDongChoPhepHienThi.SelectedItem = data.SoLan;
                        else
                            comboBox_SoLuongDongChoPhepHienThi.Text = data.SoLan;
                    }

                    richTextBox1_TomTatGhiChu.Text = data.TomTat;

                    checkBox1_ChoPhepGoiYTenTaiKhoan.Checked = data.GoiYPass == "TRUE";
                    checkBox1_ChoPhepGoiYTenTaiKhoan.ForeColor = checkBox1_ChoPhepGoiYTenTaiKhoan.Checked ? Color.Green : Color.Red;

                    checkBox1_ChoPhepDupChuotVaoAnhGoiYssaP.Checked = data.GoiYAnh == "TRUE";
                    checkBox1_ChoPhepDupChuotVaoAnhGoiYssaP.ForeColor = checkBox1_ChoPhepDupChuotVaoAnhGoiYssaP.Checked ? Color.Green : Color.Red;

                    // Đặt lại cờ hệ thống
                    _isDoiTuongChanged = false;
                    _dataChanged = false;

                    // 3. MỞ LẠI SỰ KIỆN SAU KHI DỮ LIỆU ĐÃ ĐỊNH HÌNH
                    comboBox_DoiTuongPhanMem.TextChanged += comboBox_DoiTuongPhanMem_TextChanged;
                    comboBox_SoLuongDongChoPhepHienThi.TextChanged += OnDataChanged;
                    richTextBox1_TomTatGhiChu.TextChanged += richTextBox1_TomTatGhiChu_TextChanged;
                    checkBox1_ChoPhepGoiYTenTaiKhoan.CheckedChanged += CheckBox1_ChoPhepGoiYTenTaiKhoan_CheckedChanged;
                    checkBox1_ChoPhepDupChuotVaoAnhGoiYssaP.CheckedChanged += checkBox1_ChoPhepDupChuotVaoAnhGoiYssaP_CheckedChanged;
                }));

                LoadKyHieuDonVi(cn);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOAD_ERROR] {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }
        private void LuuKyHieuDonVi(string kyHieuTrungDoan, string kyHieuTieuDoan)
        {
            try
            {
                using var cn = new SqliteConnection($"Data Source={_csdl2Path}");
                cn.Open();
                using var cmd = cn.CreateCommand();
                // Lưu ý: Cột Ký hiệu tiểu đoàn trong DB của bác tên là "KeHieu_TieuDoan" (chữ e)
                cmd.CommandText = "UPDATE KyHieu_DonVi SET KyHieu_TrungDoan = @k1, KeHieu_TieuDoan = @k2 WHERE ID = 1";
                cmd.Parameters.AddWithValue("@k1", SafeEncrypt(kyHieuTrungDoan));
                cmd.Parameters.AddWithValue("@k2", SafeEncrypt(kyHieuTieuDoan));
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { Debug.WriteLine($"[Lỗi LuuKyHieuDonVi] {ex.Message}"); }
        }
        private void LuuNamHeThong(string nam)
        {
            try
            {
                // Ép sang số nguyên, nếu không phải số thì bỏ qua không lưu bậy
                if (!int.TryParse(nam, out int namParsed)) return;

                using var cn = new SqliteConnection($"Data Source={_csdl2Path}");
                cn.Open();
                using var cmd = cn.CreateCommand();
                cmd.CommandText = "UPDATE NamHeThong SET NAM = @nam WHERE ID = 1";
                cmd.Parameters.AddWithValue("@nam", namParsed);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { Debug.WriteLine($"[Lỗi LuuNamHeThong] {ex.Message}"); }
        }
        private void CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is CheckBox cb)
            {
                cb.ForeColor = cb.Checked ? Color.Green : Color.Red;
            }
        }
        private void checkBox1_ChoPhepDupChuotVaoAnhGoiYssaP_CheckedChanged(object sender, EventArgs e)
        {
            if (_isLoading) return; // Chặn không báo lỗi khi Form đang tự động load dữ liệu

            _dataChanged = true;

            // Nếu người dùng vừa TICK chọn (bật tính năng)
            if (checkBox1_ChoPhepDupChuotVaoAnhGoiYssaP.Checked)
            {
                string thongBao = "Tùy vào môi trường sử dụng, bạn có chắc chắn tin cậy máy tính này\n" +
                    "để cho phép hiển thị mật khẩu khi đúp chuột vào Lô gô Cảnh sát cơ động không?\n\n" +
                    "CẢNH BÁO: Tính năng này có nguy cơ làm lộ mật khẩu nếu\n" +
                    "có người khác sử dụng chung máy tính.";
                // Gọi Form ảo xác nhận bảo mật với RichText
                bool rs = HienThiFormAo_XacNhanBaoMat("CẢNH BÁO BẢO MẬT", thongBao);

                if (!rs) // Nếu người dùng chọn Không/Hủy
                {
                    // Tạm thời ngắt sự kiện để code tự động bỏ tick mà không bị lặp vòng lặp
                    checkBox1_ChoPhepDupChuotVaoAnhGoiYssaP.CheckedChanged -= checkBox1_ChoPhepDupChuotVaoAnhGoiYssaP_CheckedChanged;
                    checkBox1_ChoPhepDupChuotVaoAnhGoiYssaP.Checked = false; // Ép trở lại trạng thái Tắt
                    checkBox1_ChoPhepDupChuotVaoAnhGoiYssaP.CheckedChanged += checkBox1_ChoPhepDupChuotVaoAnhGoiYssaP_CheckedChanged;
                }
            }
            // Cập nhật lại màu sắc Xanh/Đỏ của CheckBox
            checkBox1_ChoPhepDupChuotVaoAnhGoiYssaP.ForeColor = checkBox1_ChoPhepDupChuotVaoAnhGoiYssaP.Checked ? Color.Green : Color.Red;
        }
        #region FORM ẢO XÁC NHẬN BẢO MẬT (RICH TEXT)
        /// Dựng Form ảo chuyên hiển thị Cảnh báo bảo mật.  
        private bool HienThiFormAo_XacNhanBaoMat(string tieuDe, string noiDung)
        {
            bool dongY = false;

            using (var formAo = new FormAoBase())
            {
                formAo.Text = "Xác nhận bảo mật";
                formAo.Size = new System.Drawing.Size(800, 480);
                formAo.FormBorderStyle = FormBorderStyle.FixedDialog;
                formAo.MaximizeBox = false; formAo.MinimizeBox = false; formAo.ShowIcon = false;
                formAo.ShowInTaskbar = false;

                // --- 1. PANEL TIÊU ĐỀ ---
                var panelTop = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(25, 20, 20, 5) };
                panelTop.StateCommon.Color1 = System.Drawing.Color.White;

                var lblTitle = new Krypton.Toolkit.KryptonLabel { Text = tieuDe.ToUpper(), Dock = DockStyle.Fill, AutoSize = false };
                lblTitle.StateCommon.ShortText.Font = new System.Drawing.Font("Segoe UI", 12.5F, System.Drawing.FontStyle.Bold);
                lblTitle.StateCommon.ShortText.Color1 = System.Drawing.Color.FromArgb(198, 40, 40); // Tiêu đề Đỏ cảnh báo
                panelTop.Controls.Add(lblTitle);

                var separator = new Label { Height = 1, Dock = DockStyle.Top, BackColor = System.Drawing.Color.FromArgb(240, 200, 200), Margin = new Padding(0, 5, 0, 10) };

                // --- 2. PANEL NỘI DUNG (RICH TEXT BOX) ---
                var panelContent = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Fill, Padding = new Padding(25, 15, 30, 20) };
                panelContent.StateCommon.Color1 = System.Drawing.Color.White;

                var picIcon = new PictureBox { Image = System.Drawing.SystemIcons.Warning.ToBitmap(), SizeMode = PictureBoxSizeMode.CenterImage, Size = new System.Drawing.Size(50, 50), Location = new System.Drawing.Point(25, 15) };

                // Sử dụng RichTextBox thay vì TextBox để cho phép tô màu nhiều loại trong cùng 1 câu
                var rtbContent = new Krypton.Toolkit.KryptonRichTextBox
                {
                    Text = noiDung.Replace("\n", Environment.NewLine),
                    ReadOnly = true,
                    WordWrap = true,
                    ScrollBars = RichTextBoxScrollBars.Vertical,
                    Location = new System.Drawing.Point(90, 15),
                    Width = formAo.Width - 130,
                    Height = panelContent.Height - 35,
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
                };
                rtbContent.StateCommon.Back.Color1 = System.Drawing.Color.White;
                rtbContent.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.None;

                // Font gốc
                Font defaultFont = new System.Drawing.Font("Segoe UI", 11.5F, System.Drawing.FontStyle.Regular);
                Font boldFont = new System.Drawing.Font("Segoe UI", 11.5F, System.Drawing.FontStyle.Bold);
                rtbContent.Font = defaultFont;
                rtbContent.ForeColor = System.Drawing.Color.FromArgb(40, 40, 40);

                // ĐỘNG CƠ TÔ MÀU CHỮ THÔNG MINH
                Action<string, Color, Font> HighlightText = (word, color, font) =>
                {
                    int startIndex = 0;
                    while (startIndex < rtbContent.TextLength)
                    {
                        int wordStartIndex = rtbContent.Text.IndexOf(word, startIndex, StringComparison.OrdinalIgnoreCase);
                        if (wordStartIndex != -1)
                        {
                            rtbContent.SelectionStart = wordStartIndex;
                            rtbContent.SelectionLength = word.Length;
                            rtbContent.SelectionColor = color;
                            rtbContent.SelectionFont = font;
                            startIndex = wordStartIndex + word.Length;
                        }
                        else break;
                    }
                };

                // --- GỌI CÁC TỪ KHÓA CẦN NHẤN MẠNH ---
                HighlightText("tin cậy", System.Drawing.Color.FromArgb(34, 139, 34), boldFont); // Xanh lá
                HighlightText("CẢNH BÁO:", System.Drawing.Color.FromArgb(198, 40, 40), boldFont); // Đỏ đậm
                HighlightText("lộ mật khẩu", System.Drawing.Color.FromArgb(198, 40, 40), boldFont); // Đỏ đậm
                HighlightText("hiển thị mật khẩu", System.Drawing.Color.FromArgb(211, 84, 0), boldFont); // Cam đậm

                // Xóa vết bôi đen
                rtbContent.SelectionStart = 0;
                rtbContent.SelectionLength = 0;

                // --- 3. PANEL NÚT ---
                var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 75, BackColor = System.Drawing.Color.WhiteSmoke };

                var btnYes = new Krypton.Toolkit.KryptonButton { Text = "Vẫn bật", Width = 140, Height = 42, DialogResult = DialogResult.Yes };
                btnYes.StateCommon.Content.ShortText.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
                btnYes.StateCommon.Border.Rounding = 6;
                btnYes.Click += (s, ev) => dongY = true;

                var btnNo = new Krypton.Toolkit.KryptonButton { Text = "Hủy bỏ", Width = 140, Height = 42, DialogResult = DialogResult.No };
                btnNo.StateCommon.Content.ShortText.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
                btnNo.StateCommon.Border.Rounding = 6;
                btnNo.Click += (s, ev) => dongY = false;

                int totalWidth = btnYes.Width + 20 + btnNo.Width;
                int startX = (formAo.Width - totalWidth) / 2;
                btnYes.Location = new System.Drawing.Point(startX, 16);
                btnNo.Location = new System.Drawing.Point(startX + btnYes.Width + 20, 16);

                panelBottom.Controls.Add(btnYes);
                panelBottom.Controls.Add(btnNo);

                panelContent.Controls.Add(picIcon);
                panelContent.Controls.Add(rtbContent);
                panelContent.Controls.Add(separator);

                rtbContent.BringToFront(); picIcon.BringToFront(); separator.SendToBack();

                formAo.Controls.Add(panelContent);
                formAo.Controls.Add(panelTop);
                formAo.Controls.Add(panelBottom);

                // An toàn tuyệt đối: Nhấn phím Enter hoặc Esc đều mặc định là "Hủy bỏ"
                formAo.AcceptButton = btnNo;
                formAo.CancelButton = btnNo;
                formAo.Shown += (s, ev) => btnNo.Focus(); // Bắt chuột tự focus vào nút Hủy Bỏ

                formAo.ShowDialog(this);
            }
            return dongY;
        }
        #endregion
        private void LuuThoiGianThayDoiAnh(string thoiGian)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(thoiGian)) return;

                using var cn = new SqliteConnection($"Data Source={_csdl2Path}");
                cn.Open();

                // 1️⃣ Đảm bảo bảng lưu trữ luôn tồn tại trong hệ thống phân vùng
                using (var cmdCreate = cn.CreateCommand())
                {
                    cmdCreate.CommandText = @"
                CREATE TABLE IF NOT EXISTS CauHinh_ThoiGianAnh (
                    ID INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    ThoiGian_DuocChon TEXT
                );";
                    cmdCreate.ExecuteNonQuery();
                }

                // 2️⃣ Cập nhật dữ liệu cấu hình vào vị trí bản ghi gốc ID = 1
                using var cmdUpdate = cn.CreateCommand();
                cmdUpdate.CommandText = "UPDATE CauHinh_ThoiGianAnh SET ThoiGian_DuocChon = @tg WHERE ID = 1";
                cmdUpdate.Parameters.AddWithValue("@tg", thoiGian.Trim());
                int rowsAffected = cmdUpdate.ExecuteNonQuery();

                // 3️⃣ Nếu chưa có dữ liệu ban đầu -> Tiến hành chèn mới thiết lập
                if (rowsAffected == 0)
                {
                    using var cmdInsert = cn.CreateCommand();
                    cmdInsert.CommandText = "INSERT INTO CauHinh_ThoiGianAnh (ThoiGian_DuocChon) VALUES (@tg)";
                    cmdInsert.Parameters.AddWithValue("@tg", thoiGian.Trim());
                    cmdInsert.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi LuuThoiGianThayDoiAnh] {ex.Message}");
            }
        }

        private readonly SemaphoreSlim _sqliteLock = new SemaphoreSlim(1, 1);
        private async Task<string> SaveToSQLiteAsync()
        {
            if (IsDisposed)
                return "Form đã đóng.";

            await _sqliteLock.WaitAsync();

            try
            {
                if (IsDisposed)
                    return "Form đã đóng.";

                var uiData = CaptureUIData();

                // SQLite là IO sync → KHÔNG cần Task.Run
                return SaveToSQLiteCore(uiData);
            }
            finally
            {
                _sqliteLock.Release();
            }
        }
        private string SaveToSQLiteCore(CauHinhData uiData)
        {
            if (uiData == null)
                return "Dữ liệu không hợp lệ.";

            Directory.CreateDirectory(Path.GetDirectoryName(_csdl2Path));

            using var cn = new SqliteConnection(BuildConnectionString(_csdl2Path));
            cn.Open();
            ApplySQLitePragma(cn);

            using var tran = cn.BeginTransaction();
            try
            {
                // -----------------------------------------------------
                // 1. LƯU BẢNG THÔNG TIN (Code gốc của bạn)
                // -----------------------------------------------------
                // Thay khối using (var cmd = cn.CreateCommand()) lưu bảng ThongTin bằng đoạn này:
                using (var cmd = cn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"
INSERT INTO ThongTin (ID, TenTrungDoan, TenTieuDoan, TomTatGhiChu, SoLanChoPhepHienThi, ChoPhepGoiYMatKhau, ChoPhepDupChuotVaoAnh_GoiYMatKhau, textBox1_TenTrungDoanDong1, KyHieuBaoCao)
VALUES (1, $TenTrungDoan, $TenTieuDoan, $TomTat, $SoLan, $GoiYPass, $GoiYAnh, $TenTDDong1, $KyHieuBC)
ON CONFLICT(ID) DO UPDATE SET
TenTrungDoan = excluded.TenTrungDoan,
TenTieuDoan = excluded.TenTieuDoan,
TomTatGhiChu = excluded.TomTatGhiChu,
SoLanChoPhepHienThi = excluded.SoLanChoPhepHienThi,
ChoPhepGoiYMatKhau = excluded.ChoPhepGoiYMatKhau,
ChoPhepDupChuotVaoAnh_GoiYMatKhau = excluded.ChoPhepDupChuotVaoAnh_GoiYMatKhau,
textBox1_TenTrungDoanDong1 = excluded.textBox1_TenTrungDoanDong1,
KyHieuBaoCao = excluded.KyHieuBaoCao;";

                    cmd.Parameters.AddWithValue("$TenTrungDoan", SafeEncrypt(uiData.TenTrungDoan));
                    cmd.Parameters.AddWithValue("$TenTieuDoan", SafeEncrypt(uiData.TenTieuDoan));
                    cmd.Parameters.AddWithValue("$TomTat", SafeEncrypt(uiData.TomTat));
                    cmd.Parameters.AddWithValue("$SoLan", SafeEncrypt(uiData.SoLanChoPhepHienThi));
                    cmd.Parameters.AddWithValue("$GoiYPass", SafeEncrypt(uiData.ChoPhepGoiYMatKhau));
                    cmd.Parameters.AddWithValue("$GoiYAnh", SafeEncrypt(uiData.ChoPhepDupChuotVaoAnh));

                    // ⭐ MÃ HÓA VÀ LƯU 2 TRƯỜNG MỚI
                    cmd.Parameters.AddWithValue("$TenTDDong1", SafeEncrypt(uiData.TenTrungDoanDong1));
                    cmd.Parameters.AddWithValue("$KyHieuBC", SafeEncrypt(uiData.KyHieuBaoCao));

                    cmd.ExecuteNonQuery();
                }
                // 2. LƯU BẢNG PHIÊN BẢN ĐỐI TƯỢNG (Code bổ sung)
                // -----------------------------------------------------
                using (var cmdDoiTuong = cn.CreateCommand())
                {
                    cmdDoiTuong.Transaction = tran;
                    cmdDoiTuong.CommandText = @"
                INSERT INTO PhienBan_DoiTuong (ID, DoiTuong)
                VALUES (1, $DoiTuong)
                ON CONFLICT(ID) DO UPDATE SET
                DoiTuong = excluded.DoiTuong;";

                    cmdDoiTuong.Parameters.AddWithValue("$DoiTuong", SafeEncrypt(uiData.DoiTuongPhanMem));
                    cmdDoiTuong.ExecuteNonQuery();
                }

                tran.Commit();
                CheckpointSQLite(cn);

                return "Lưu cấu hình thành công!";
            }
            catch (SqliteException ex)
            {
                try { if (tran?.Connection != null) tran.Rollback(); } catch { }
                return $"SQLite Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                try { tran.Rollback(); } catch { }
                return $"System Error: {ex.Message}";
            }
        }
        private async void kryptonButton_LuuCauHinh_Click(object sender, EventArgs e)
        {
            string textBanDau = kryptonButton_LuuCauHinh.Values.Text;
            Image anhBanDau = kryptonButton_LuuCauHinh.Values.Image;

            try
            {
                if (string.IsNullOrWhiteSpace(comboBox_SoLuongDongChoPhepHienThi.Text))
                {
                    MessageBox.Show("Bạn chưa chọn số lượng dòng!", "Cảnh báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    comboBox_SoLuongDongChoPhepHienThi.Focus();
                    return;
                }

                kryptonButton_LuuCauHinh.Enabled = false;
                kryptonButton_LuuCauHinh.Values.Text = "Đang lưu...";
                kryptonButton_LuuCauHinh.Values.Image = null;

                if (label1 != null)
                {
                    label1.ForeColor = Color.Black;
                    label1.Text = "Hệ thống đang thực hiện lưu cấu hình...";
                }
                // 2. 🔥 GỌI HÀM TRUNG GIAN ĐỂ TỰ ĐỘNG CẬP NHẬT SANG FORM10 (NẾU ĐANG MỞ)
                Module_NhatKy.DocVaNapStatusLabelForm10();
                // 🔥 GOM CÁC TÁC VỤ DB LẺ TẺ VÀO ĐÂY TRƯỚC KHI LƯU CHÍNH
                LuuCauHinhXemHuongDan();

                // ⭐ SỬA LỖI LƯU 2: Dùng .Text thay vì .SelectedItem để tuyệt đối không bị dội Null
                if (!string.IsNullOrWhiteSpace(comboBox1_TuDongXoaNhatKy.Text))
                {
                    LuuTuDongXoaVaoCSDL(comboBox1_TuDongXoaNhatKy.Text.Trim());
                }

                // ⭐ BỔ SUNG LƯU SỰ KIỆN THOÁT NGAY TẠI ĐÂY
                if (!string.IsNullOrWhiteSpace(comboBox1_ChonSuKienThoat.Text))
                {
                    LuuSuKienThoat(comboBox1_ChonSuKienThoat.Text.Trim());
                }
                // ⭐ BỔ SUNG LƯU SỰ KIỆN THOÁT NGAY TẠI ĐÂY
                if (!string.IsNullOrWhiteSpace(comboBox1_ChonSuKienThoat.Text))
                {
                    LuuSuKienThoat(comboBox1_ChonSuKienThoat.Text.Trim());
                }

                // ====================================================================
                // 🌟 THÊM CODE: Thực thi lưu thời gian thay đổi ảnh khi bấm Lưu cấu hình
                // ====================================================================
                if (comboBox1_ThoiGianThayDoiAnh != null && !string.IsNullOrWhiteSpace(comboBox1_ThoiGianThayDoiAnh.Text))
                {
                    Module_HinhAnhTrangChu.LuuCauHinhThoiGian(comboBox1_ThoiGianThayDoiAnh.Text.Trim());
                }
                // ====================================================================


                string ketQua = await SaveToSQLiteAsync();

                if (ketQua.Contains("thành công", StringComparison.OrdinalIgnoreCase))
                {
                    if (label1 != null)
                    {
                        label1.ForeColor = Color.DarkGreen;
                        label1.Text = "✔ Đã lưu cấu hình lúc " + DateTime.Now.ToString("HH:mm:ss");
                    }

                    var formCha = Application.OpenForms
                        .OfType<Form2_FormCha>()
                        .FirstOrDefault();

                    formCha?.KhoiTaoHeThongHinhNenAsync();

                    Module_DonVi.KhoiTao();
                    Module_ThongBao.ResetCacheSoDong();

                    if (_isDoiTuongChanged)
                    {
                        Module_NhatKy.GhiNhatKy(
                            Module_TaiKhoan.TenTaiKhoan_RAM,
                            "Đổi đối tượng phần mềm",
                            $"Giá trị mới: {comboBox_DoiTuongPhanMem.Text}"
                        );

                        formCha?.CapNhatGiaoDienTheoPhienBan();

                        var result = MessageBox.Show(
                            "Đã đổi đối tượng phần mềm thành công.\nBạn có muốn cập nhật danh sách đơn vị trực thuộc ngay không?",
                            "Cập nhật danh sách",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question
                        );

                        if (result == DialogResult.Yes)
                        {
                            kryptonButton1_CapNhatDanhSachDonVi_Click(
                                kryptonButton1_CapNhatDanhSachDonVi,
                                EventArgs.Empty);
                        }

                        _doiTuongBanDau = comboBox_DoiTuongPhanMem.Text?.Trim() ?? "";
                        _isDoiTuongChanged = false;
                    }
                }
                else
                {
                    if (label1 != null)
                    {
                        label1.ForeColor = Color.Red;
                        label1.Text = "✘ Lỗi lưu cấu hình!";
                    }

                    MessageBox.Show(ketQua, "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống: " + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                kryptonButton_LuuCauHinh.Values.Text = textBanDau;
                kryptonButton_LuuCauHinh.Values.Image = anhBanDau;
                kryptonButton_LuuCauHinh.Enabled = true;
            }
        }
        private async void kryptonButton_LuuThongTin_Click(object sender, EventArgs e)
        {
            string textBanDau = kryptonButton_LuuThongTin.Values.Text;
            Image anhBanDau = kryptonButton_LuuThongTin.Values.Image;

            try
            {
                kryptonButton_LuuThongTin.Enabled = false;
                kryptonButton_LuuThongTin.Values.Text = "Đang lưu...";
                kryptonButton_LuuThongTin.Values.Image = null;

                if (label1_ThongBaoThanhCong != null)
                {
                    label1_ThongBaoThanhCong.ForeColor = Color.Black;
                    label1_ThongBaoThanhCong.Text = "Đang ghi dữ liệu vào hệ thống...";
                }

                await Task.Delay(100); // Nhịp nghỉ UX

                // ⭐ BẢO HIỂM DỮ LIỆU TAB 2 & CÁC BẢNG LẺ Ở TAB 1
                LuuCauHinhXemHuongDan();
                if (!string.IsNullOrWhiteSpace(comboBox1_TuDongXoaNhatKy.Text))
                    LuuTuDongXoaVaoCSDL(comboBox1_TuDongXoaNhatKy.Text.Trim());

                if (!string.IsNullOrWhiteSpace(comboBox1_ChonSuKienThoat.Text))
                    LuuSuKienThoat(comboBox1_ChonSuKienThoat.Text.Trim());

                // ⭐ LƯU KÝ HIỆU VÀ NĂM NGAY TRƯỚC KHI LƯU CSDL CHÍNH
                if (comboBox_KyHieu_TenTrungDoan != null && comboBox_KyHieu_TenTieuDoan != null)
                {
                    LuuKyHieuDonVi(comboBox_KyHieu_TenTrungDoan.Text.Trim(), comboBox_KyHieu_TenTieuDoan.Text.Trim());
                }

                if (comboBox1_NamHienTai != null && !string.IsNullOrWhiteSpace(comboBox1_NamHienTai.Text))
                {
                    LuuNamHeThong(comboBox1_NamHienTai.Text.Trim());
                }

                // Gọi hàm gốc lưu cấu hình chính
                string thongBao = await SaveToSQLiteAsync();

                // ===== BƯỚC 5: XỬ LÝ KẾT QUẢ THÔNG MINH =====
                if (thongBao.Contains("thành công", StringComparison.OrdinalIgnoreCase))
                {
                    // Nếu THÀNH CÔNG: Chỉ cập nhật Label, không hiện MessageBox
                    if (label1_ThongBaoThanhCong != null)
                    {
                        label1_ThongBaoThanhCong.ForeColor = Color.DarkGreen;
                        label1_ThongBaoThanhCong.Text = "✔ Đã lưu thành công lúc " + DateTime.Now.ToString("HH:mm:ss");
                    }
                }
                else
                {
                    // Nếu THẤT BẠI: Vẫn phải hiện MessageBox để cảnh báo lỗi
                    if (label1_ThongBaoThanhCong != null)
                    {
                        label1_ThongBaoThanhCong.ForeColor = Color.Red;
                        label1_ThongBaoThanhCong.Text = "✘ Lưu thất bại!";
                    }

                    MessageBox.Show(thongBao, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi phát sinh: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                kryptonButton_LuuThongTin.Values.Text = textBanDau;
                kryptonButton_LuuThongTin.Values.Image = anhBanDau;
                kryptonButton_LuuThongTin.Enabled = true;
            }
        }
        private void CheckpointSQLite(SqliteConnection cn)
        {
            try
            {
                using var cmd = cn.CreateCommand();

                cmd.CommandText =
                    "PRAGMA wal_checkpoint(TRUNCATE);";

                cmd.ExecuteNonQuery();
            }
            catch
            {
                // Không cho checkpoint làm crash hệ thống
            }
        }
        private static string SafeEncrypt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            var result = BaoMatAES.MaHoa(value);

            return string.IsNullOrWhiteSpace(result)
                ? ""
                : result;
        }
        private static string SafeDecrypt(object input)
        {
            // 1. Kiểm tra sớm đầu vào: Nếu null hoặc rỗng thì trả về ngay
            string value = input?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }
            // 2. Thực hiện giải mã trong khối Try-Catch an toàn
            try
            {
                return BaoMatAES.GiaiMaNhanDang(value);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DECRYPT_ERROR] {ex}");

                // optional: log file production
                File.AppendAllText("crypto_error.log",
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {ex}\n");

                return string.Empty;
            }
        }
        private void comboBox_DoiTuongPhanMem_TextChanged(object sender, EventArgs e)
        {
            if (_isLoading) return; // chặn khi load dữ liệu

            string currentValue = comboBox_DoiTuongPhanMem.Text?.Trim() ?? "";

            // kiểm tra có thay đổi không
            _isDoiTuongChanged = currentValue != _doiTuongBanDau;

            // đánh dấu dữ liệu thay đổi
            _dataChanged = _isDoiTuongChanged;

            // (debug nếu cần)
            // Debug.WriteLine("Đối tượng thay đổi: " + currentValue);
        }
        private string CheckboxFlag(CheckBox cb) => cb.Checked ? "TRUE" : "FALSE";
        //Nút o trang 2
        private void LuuCauHinhXemHuongDanCore(string cheDo)
        {
            try
            {
                using var cn = new SqliteConnection($"Data Source={_csdl2Path}");
                cn.Open();
                using var cmd = cn.CreateCommand();
                cmd.CommandText = "UPDATE CheDo_XemHuongDan SET CheDoXem_HuongDanSD = @val WHERE ID = 1";
                cmd.Parameters.AddWithValue("@val", cheDo);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { Debug.WriteLine("Lỗi hướng dẫn: " + ex.Message); }
        }
        // Lưu ý hàm LuuSuKienThoat và LuuTuDongXoaVaoCSDL của bạn đã có tham số truyền vào rồi, giữ nguyên logic lõi của 2 hàm đó.
        private async Task<string> ThucHienLuuToanBoHeThongAsync()
        {
            if (IsDisposed) return "Form đã đóng.";

            await _sqliteLock.WaitAsync();
            try
            {
                // 1. CHỤP TOÀN BỘ DỮ LIỆU GIAO DIỆN (UI THREAD)
                var uiData = CaptureUIData();

                // 2. GỌI CÁC HÀM LƯU RỜI RẠC TRƯỚC (Sự kiện thoát, Hướng dẫn, Dọn dẹp)
                LuuSuKienThoat(uiData.SuKienThoatPhanMem);
                LuuCauHinhXemHuongDanCore(uiData.CheDoXemHuongDan);
                LuuTuDongXoaVaoCSDL(uiData.TuDongXoaNhatKy);
                // ====================================================================
                // 🌟 CẬP NHẬT CODE: Đồng bộ hóa luồng nạp lưu ngầm an toàn vật lý
                // ====================================================================
                // LuuThoiGianThayDoiAnh(uiData.ThoiGianThayDoiAnh);
                // THAY BẰNG ĐOẠN NÀY:
                Module_HinhAnhTrangChu.LuuCauHinhThoiGian(uiData.ThoiGianThayDoiAnh);
                // ====================================================================
                // Cập nhật cấu hình thời gian chuyển ảnh (Nếu bạn có hàm ghi)
                // Module_HinhAnhTrangChu.GhiCauHinhThoiGian(uiData.ThoiGianThayDoiAnh);

                // 3. LƯU VÀO CSDL2 CHÍNH (Bảng ThongTin & PhienBan_DoiTuong)
                return SaveToSQLiteCore(uiData);
            }
            finally
            {
                _sqliteLock.Release();
            }
        }
        private void LoadComboBoxCauHinh()
        {
            comboBox_SoLuongDongChoPhepHienThi.Items.Clear();

            for (int i = 3; i <= 30; i++)
            {
                comboBox_SoLuongDongChoPhepHienThi.Items.Add(i.ToString("D2")); // "D2" tự động pad 2 chữ số
            }
        }
        public static void LuuSuKienThoat(string luaChon)
        {
            string dbPath = Module_DanduongGPS.DuongDanCSDL2;

            using var cn = new SqliteConnection($"Data Source={dbPath}");
            cn.Open();

            // Mã hóa giá trị trước khi lưu
            string luaChonMaHoa = BaoMatAES.MaHoa(luaChon);

            // Update ID = 1 nếu có
            using var cmdUpdate = cn.CreateCommand();
            cmdUpdate.CommandText = "UPDATE SuKien_ThoatPhanMem SET SuKien_DuọcChon = @luaChon WHERE ID = 1";
            cmdUpdate.Parameters.AddWithValue("@luaChon", luaChonMaHoa);
            int rowsAffected = cmdUpdate.ExecuteNonQuery();

            // Nếu chưa có record với ID = 1 → insert mới (SQLite tự gán ID)
            if (rowsAffected == 0)
            {
                using var cmdInsert = cn.CreateCommand();
                cmdInsert.CommandText = "INSERT INTO SuKien_ThoatPhanMem (SuKien_DuọcChon) VALUES (@luaChon)";
                cmdInsert.Parameters.AddWithValue("@luaChon", luaChonMaHoa);
                cmdInsert.ExecuteNonQuery();
            }
        }
        public static string LoadSuKienThoat()
        {
            string dbPath = Module_DanduongGPS.DuongDanCSDL2;

            using var cn = new SqliteConnection($"Data Source={dbPath}");
            cn.Open();

            // 1️⃣ Tạo bảng nếu chưa tồn tại
            using (var cmdCreate = cn.CreateCommand())
            {
                cmdCreate.CommandText = @"
        CREATE TABLE IF NOT EXISTS SuKien_ThoatPhanMem (
            ID INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            SuKien_DuọcChon TEXT
        );";
                cmdCreate.ExecuteNonQuery();
            }

            // 2️⃣ Kiểm tra xem record ID = 1 có tồn tại không
            using (var cmdCheck = cn.CreateCommand())
            {
                cmdCheck.CommandText = "SELECT COUNT(*) FROM SuKien_ThoatPhanMem WHERE ID = 1";
                long count = (long)cmdCheck.ExecuteScalar();

                if (count == 0)
                {
                    // Nếu không tồn tại → tạo record mới với giá trị mặc định "Thoát ngay" đã mã hóa
                    using var cmdInsert = cn.CreateCommand();
                    string luaChonMaHoa = BaoMatAES.MaHoa("Thoát ngay");
                    cmdInsert.CommandText = "INSERT INTO SuKien_ThoatPhanMem (SuKien_DuọcChon) VALUES (@luaChon)";
                    cmdInsert.Parameters.AddWithValue("@luaChon", luaChonMaHoa);
                    cmdInsert.ExecuteNonQuery();
                }
            }

            // 3️⃣ Lấy giá trị SuKien_DuọcChon từ ID = 1
            using var cmdLoad = cn.CreateCommand();
            cmdLoad.CommandText = "SELECT SuKien_DuọcChon FROM SuKien_ThoatPhanMem WHERE ID = 1";
            var result = cmdLoad.ExecuteScalar();

            // 4️⃣ Nếu null (không thể xảy ra) → trả về mặc định
            if (result == null)
                return "Thoát ngay";

            // 5️⃣ Giải mã và trả về
            return BaoMatAES.GiaiMa(result.ToString());
        }
        private bool KiemTraDayDuCSDL(out string thongBaoThieu)
        {
            thongBaoThieu = string.Empty;

            string baseDir = AppContext.BaseDirectory;
            string thuMucDatabase = Path.Combine(baseDir, "Database");

            string[] tepBatBuoc =
            {
        "csdl1.db",
        "csdl2.db",
        "csdl3.db",
        "csdl4.db"
    };

            if (!Directory.Exists(thuMucDatabase))
            {
                thongBaoThieu = "Không tìm thấy thư mục Database.";
                return false;
            }

            var danhSachThieu = new StringBuilder();

            foreach (var tep in tepBatBuoc)
            {
                string duongDan = Path.Combine(thuMucDatabase, tep);
                if (!File.Exists(duongDan))
                    danhSachThieu.AppendLine("- " + tep);
            }

            if (danhSachThieu.Length > 0)
            {
                thongBaoThieu = "Hiện CSDL đang thiếu các tệp:\n\n" + danhSachThieu;
                return false;
            }

            return true;
        }
        private void goCaiDat_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormManager.OpenModal<Form9_GoCaiDat>(this);
        }
        private void chuKySo_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormManager.OpenModal<Form7_ThongTinAdmin>(this);
        }
        private void NapDanhSachNam()
        {
            comboBox1_NamHienTai.Items.Clear();

            int namMay = DateTime.Now.Year;

            int min = namMay - 5;
            int max = namMay + 5;

            for (int i = min; i <= max; i++)
            {
                comboBox1_NamHienTai.Items.Add(i);
            }

            comboBox1_NamHienTai.SelectedItem = namMay;
        }
        private void LoadNamHeThong()
        {
            try
            {
                using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                conn.Open();

                using var cmd = conn.CreateCommand();

                // 1️⃣ Tạo bảng nếu chưa có
                cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS NamHeThong(
    ID INTEGER PRIMARY KEY,
    NAM INTEGER
);";
                cmd.ExecuteNonQuery();

                // 2️⃣ Đảm bảo luôn có ID = 1
                cmd.CommandText = @"
INSERT OR IGNORE INTO NamHeThong (ID, NAM)
VALUES (1, strftime('%Y','now'));";
                cmd.ExecuteNonQuery();

                // 3️⃣ Load dữ liệu
                cmd.CommandText = "SELECT NAM FROM NamHeThong WHERE ID = 1;";
                object result = cmd.ExecuteScalar();

                if (result != null && int.TryParse(result.ToString(), out int nam))
                {
                    comboBox1_NamHienTai.SelectedItem = nam;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoadNamHeThong] {ex.Message}");
            }
        }
        private void thongTin_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Lấy chuỗi thông tin từ Module hệ thống
                string thongTin = Module_TaiKhoan.LayThongTinPhienBanVaToken();
                // Gọi Form ảo DataGrid chuyên dụng
                HienThiFormAo_ThongTinPhanMem_Luoi("THÔNG TIN PHIÊN BẢN - BẢN QUYỀN", thongTin);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi hiển thị thông tin phần mềm] {ex.Message}");
            }
        }
        private void HienThiFormAo_ThongTinPhanMem_Luoi(string tieuDe, string rawData)
        {
            // 1. Dọn dẹp chuỗi gốc
            string noiDungChuan = rawData.Replace("\n", Environment.NewLine);
            if (noiDungChuan.StartsWith("THÔNG TIN PHẦN MỀM" + Environment.NewLine + Environment.NewLine))
            {
                noiDungChuan = noiDungChuan.Replace("THÔNG TIN PHẦN MỀM" + Environment.NewLine + Environment.NewLine, "");
            }

            // ⭐ SỬ DỤNG CLASS FORMAOBASE
            using (var formAo = new FormAoBase())
            {
                formAo.Text = "Thông tin phần mềm";
                formAo.Size = new System.Drawing.Size(1100, 480); // Cân đối với lượng thông tin Token
                formAo.FormBorderStyle = FormBorderStyle.FixedDialog;
                formAo.MaximizeBox = false; formAo.MinimizeBox = false;
                formAo.ShowIcon = false; formAo.ShowInTaskbar = false;

                // --- 1. PANEL TIÊU ĐỀ ---
                var panelTop = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(25, 20, 20, 5) };
                panelTop.StateCommon.Color1 = System.Drawing.Color.White;

                var lblTitle = new Krypton.Toolkit.KryptonLabel { Text = tieuDe.ToUpper(), Dock = DockStyle.Fill, AutoSize = false };
                lblTitle.StateCommon.ShortText.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
                lblTitle.StateCommon.ShortText.Color1 = System.Drawing.Color.FromArgb(0, 82, 155); // Xanh đại dương
                panelTop.Controls.Add(lblTitle);

                // --- 2. ĐƯỜNG KẺ NGANG ---
                var separator = new Label { Height = 1, Dock = DockStyle.Top, BackColor = System.Drawing.Color.FromArgb(220, 220, 220), Margin = new Padding(0, 0, 0, 10) };

                // --- 3. PANEL CHỨA LƯỚI ---
                var panelContent = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Fill, Padding = new Padding(25, 10, 25, 15) };
                panelContent.StateCommon.Color1 = System.Drawing.Color.White;

                // --- 4. TẠO GRID HIỆN ĐẠI ---
                var grid = new Krypton.Toolkit.KryptonDataGridView
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    AllowUserToResizeRows = false,
                    RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    MultiSelect = false,
                    BackgroundColor = System.Drawing.Color.White,
                    BorderStyle = BorderStyle.None,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    Cursor = Cursors.Hand
                };

                grid.GridStyles.Style = Krypton.Toolkit.DataGridViewStyle.List;
                grid.StateCommon.DataCell.Content.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Regular);
                grid.StateCommon.HeaderColumn.Content.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);

                grid.Columns.Add("Cot1", "Thông số");
                grid.Columns.Add("Cot2", "Giá trị");
                grid.Columns[0].FillWeight = 34; // Cột thông số hẹp lại
                grid.Columns[1].FillWeight = 66; // Nhường chỗ cho Token dài
                grid.Columns[0].DefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
                grid.Columns[0].DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(60, 60, 60);

                // Tự động Wrap text cho cột Giá trị (Vì Token thường rất dài)
                grid.Columns[1].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

                // --- 5. ĐỔ DỮ LIỆU & TÔ MÀU ---
                string[] lines = noiDungChuan.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    int splitIdx = line.IndexOf(':');
                    if (splitIdx > 0)
                    {
                        string key = line.Substring(0, splitIdx).Trim();
                        string val = line.Substring(splitIdx + 1).Trim();

                        int rIdx = grid.Rows.Add(key, val);
                        var cellVal = grid.Rows[rIdx].Cells[1];

                        // ĐỘNG CƠ TÔ MÀU THÔNG MINH
                        string valLow = val.ToLower();
                        if (valLow.Contains("đã kích hoạt") || valLow.Contains("v1.") || valLow.Contains("hợp lệ") || valLow == "ok")
                        {
                            cellVal.Style.ForeColor = System.Drawing.Color.FromArgb(34, 139, 34); // Xanh lá
                            cellVal.Style.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
                        }
                        else if (valLow.Contains("chưa") || valLow.Contains("lỗi") || valLow.Contains("hết hạn"))
                        {
                            cellVal.Style.ForeColor = System.Drawing.Color.FromArgb(211, 47, 47); // Đỏ đô
                            cellVal.Style.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
                        }
                        else if (key.Contains("Token"))
                        {
                            cellVal.Style.ForeColor = System.Drawing.Color.FromArgb(0, 82, 155); // Xanh Navy cho mã Token
                            cellVal.Style.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Regular); // Đổi font code cho Token
                        }
                        else
                        {
                            cellVal.Style.ForeColor = System.Drawing.Color.FromArgb(45, 45, 45);
                        }
                    }
                    else
                    {
                        // Dòng tiêu đề phụ nếu có
                        int rIdx = grid.Rows.Add(line, "");
                        grid.Rows[rIdx].DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(240, 248, 255);
                        grid.Rows[rIdx].DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(0, 82, 155);
                    }
                }

                // --- 6. PANEL CHỨA NÚT ---
                var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 65, BackColor = System.Drawing.Color.WhiteSmoke };

                // Nút Copy Token
                var btnCopy = new Krypton.Toolkit.KryptonButton { Text = "Sao chép Token", Width = 150, Height = 38 };
                btnCopy.StateCommon.Content.ShortText.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
                btnCopy.StateCommon.Border.Rounding = 5;
                btnCopy.Click += (s, ev) =>
                {
                    try
                    {
                        string keyword = "Token hệ thống:";
                        int idx = rawData.LastIndexOf(keyword);
                        string tokenCopy = "";

                        if (idx >= 0)
                        {
                            tokenCopy = rawData.Substring(idx + keyword.Length).Trim();
                        }
                        else
                        {
                            tokenCopy = noiDungChuan;
                        }

                        if (!string.IsNullOrWhiteSpace(tokenCopy))
                        {
                            Clipboard.SetText(tokenCopy);
                            MessageBox.Show(formAo, "Đã sao chép Token nội bộ vào bộ nhớ tạm!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch { }
                };

                var btnClose = new Krypton.Toolkit.KryptonButton { Text = "Đóng", Width = 110, Height = 38, DialogResult = DialogResult.OK };
                btnClose.StateCommon.Content.ShortText.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
                btnClose.StateCommon.Border.Rounding = 5;

                // Căn giữa 2 nút bấm
                int totalWidth = btnCopy.Width + 15 + btnClose.Width;
                int startX = (formAo.Width - totalWidth) / 2;

                btnCopy.Location = new System.Drawing.Point(startX, 13);
                btnClose.Location = new System.Drawing.Point(startX + btnCopy.Width + 15, 13);

                panelBottom.Controls.Add(btnCopy);
                panelBottom.Controls.Add(btnClose);

                // --- 7. RÁP LAYER LÊN FORM ---
                panelContent.Controls.Add(grid);

                formAo.Controls.Add(panelContent);
                formAo.Controls.Add(separator);
                formAo.Controls.Add(panelTop);
                formAo.Controls.Add(panelBottom);

                separator.BringToFront();
                panelContent.BringToFront();

                formAo.AcceptButton = btnClose;
                formAo.CancelButton = btnClose;

                formAo.Shown += (s, ev) => grid.ClearSelection();

                formAo.ShowDialog(this);
            }
        }
        private bool FileDangMo(string path)
        {
            try
            {
                using (FileStream fs = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.None))
                {
                }
                return false;
            }
            catch (IOException)
            {
                return true;
            }
        }
        private void MoFileExcel()
        {
            try
            {
                string path = Module_DanduongGPS.DuongDanCSDL4ex;

                // ===== CHECK PATH =====
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    MessageBox.Show(
                        "Không tìm thấy tệp Excel!",
                        "Lỗi",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                // ===== CHECK FILE ĐANG MỞ =====
                if (FileDangMo(path))
                {
                    var rs = MessageBox.Show(
                        "Tệp Excel đang được mở.\nBạn có muốn đóng và mở lại không?",
                        "Thông báo",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (rs == DialogResult.No)
                        return;

                    // 🔥 cố gắng kill Excel (optional)
                    foreach (var p in Process.GetProcessesByName("EXCEL"))
                    {
                        try { p.Kill(); }
                        catch { }
                    }

                    System.Threading.Thread.Sleep(500);
                }

                // ===== MỞ FILE =====
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });

                // ===== LOG =====
                Module_NhatKy.GhiNhatKy(
                    Module_TaiKhoan.TenTaiKhoan_RAM,
                    "Mở file Excel cấu hình",
                    $"File: {Path.GetFileName(path)} | {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                );
            }
            catch (Win32Exception)
            {
                MessageBox.Show(
                    "Không có ứng dụng mở file Excel (.xlsx).",
                    "Thiếu phần mềm",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể mở tệp Excel\n" + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        public static class FormManager
        {
            public static void OpenModal<T>(Form parent)
                where T : Form, new()
            {
                if (parent == null || parent.IsDisposed)
                    return;

                try
                {
                    using (T child = new T())
                    {
                        child.StartPosition = FormStartPosition.CenterParent;
                        child.ShowInTaskbar = false;
                        child.FormBorderStyle = FormBorderStyle.FixedDialog;
                        child.MinimizeBox = false;
                        child.MaximizeBox = false;

                        child.ShowDialog(parent);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Không thể mở cửa sổ.\nChi tiết: " + ex.Message,
                        "Lỗi hệ thống",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }
        // Khai báo biến cấp class để theo dõi Form, tránh việc phải quét (query) UI liên tục
        private Form33_KiemTraSucKhoeCSDL? _form33CSDL = null;
        private bool _daNhungFormCauHinhVaoTab = false;
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;
            // ⭐ MỚI: TỰ ĐỘNG LOAD FORM KHI BẤM VÀO TAB "CƠ SỞ DỮ LIỆU"
            if (tabControl1.SelectedTab == tabPage3)
            {
                TuDongNhungFormKiemTraCSDL();
            }
            // CŨ: XỬ LÝ CHO TAB PAGE 4 (Giữ nguyên logic của anh)
            if (tabControl1.SelectedTab == tabPage4)
            {
                if (!_daNhungFormCauHinhVaoTab)
                {
                    try
                    {
                        // Tạm dừng vẽ UI để nhúng Form không bị chớp giật
                        tabPage4.SuspendLayout();

                        // Gọi hàm nhúng chuyên dụng
                        Module_TrangThaiHeThong.NhungFormVaoTabPage(tabPage4);

                        _daNhungFormCauHinhVaoTab = true; // Đánh dấu đã load
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Lỗi nhúng form cấu hình: " + ex.Message);
                    }
                    finally
                    {
                        // Cho phép vẽ lại
                        tabPage4.ResumeLayout();
                    }
                }
            }
        }
        private void LoadKyHieuDonVi(SqliteConnection cn)
        {
            try
            {
                using (var cmdTaoBang = cn.CreateCommand())
                {
                    cmdTaoBang.CommandText = @"
                CREATE TABLE IF NOT EXISTS KyHieu_DonVi (
                    ID INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    KyHieu_TrungDoan TEXT,
                    KeHieu_TieuDoan TEXT
                );";
                    cmdTaoBang.ExecuteNonQuery();
                }

                // ⭐ Đảm bảo luôn có dòng ID=1 (Trống cũng được) để UPSERT phía sau hoạt động trơn tru
                using (var cmdInit = cn.CreateCommand())
                {
                    cmdInit.CommandText = "INSERT OR IGNORE INTO KyHieu_DonVi (ID, KyHieu_TrungDoan, KeHieu_TieuDoan) VALUES (1, '', '');";
                    cmdInit.ExecuteNonQuery();
                }

                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "SELECT KyHieu_TrungDoan, KeHieu_TieuDoan FROM KyHieu_DonVi WHERE ID = 1";
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Lấy ra và giải mã AES ngay lập tức
                            comboBox_KyHieu_TenTrungDoan.Text = SafeDecrypt(reader["KyHieu_TrungDoan"]);
                            comboBox_KyHieu_TenTieuDoan.Text = SafeDecrypt(reader["KeHieu_TieuDoan"]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi LoadKyHieuDonVi: {ex.Message}");
            }
        }
        private void NapDanhSachKyHieuChung()
        {
            // 1. Khai báo danh sách chung
            string[] danhSachKyHieu = {
        "E20", "E21", "E22", "E23", "E24", "E25", "E26", "E27", "E28", "E29", "E30", "E31",
        "TTHL1", "TTHL2", "BTM", "BHL", "BCT", "BHC", "BX",
        "D1", "D2", "D4", "D5", "DHL1", "DHL2", "DHL3", "DHL4",
        "TTM", "TCT", "THC", "C1", "C2", "C3", "C4", "CCG"
    };

            // Tạm dừng vẽ giao diện để nạp dữ liệu nhanh hơn và không bị giật
            comboBox_KyHieu_TenTrungDoan.SuspendLayout();
            comboBox_KyHieu_TenTieuDoan.SuspendLayout();

            // 2. Nạp cho ComboBox
            comboBox_KyHieu_TenTrungDoan.Items.Clear();
            comboBox_KyHieu_TenTrungDoan.Items.AddRange(danhSachKyHieu);

            comboBox_KyHieu_TenTieuDoan.Items.Clear();
            comboBox_KyHieu_TenTieuDoan.Items.AddRange(danhSachKyHieu);

            // 3. Chống Highlight
            comboBox_KyHieu_TenTrungDoan.SelectedIndex = -1;
            comboBox_KyHieu_TenTieuDoan.SelectedIndex = -1;

            comboBox_KyHieu_TenTrungDoan.Text = string.Empty;
            comboBox_KyHieu_TenTieuDoan.Text = string.Empty;

            // Tiếp tục vẽ
            comboBox_KyHieu_TenTrungDoan.ResumeLayout();
            comboBox_KyHieu_TenTieuDoan.ResumeLayout();
        }
        // 1. Khai báo biến toàn cục dạng static để Cache Form5 vĩnh viễn trên RAM
        private static Form5_QuenPass _cachedForm5;
        private void quenMatKhauAd_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 2. Kiểm tra nếu Form chưa từng mở, hoặc đã lỡ bị Hủy (Dispose) thì mới tạo mới
            if (_cachedForm5 == null || _cachedForm5.IsDisposed)
            {
                _cachedForm5 = new Form5_QuenPass();
                _cachedForm5.StartPosition = FormStartPosition.CenterParent;
            }

            // 3. Gọi Form lên (Không dùng 'using' nữa để Form sống sót sau khi đóng)
            DialogResult result = _cachedForm5.ShowDialog(this);

            // 4. Xử lý kịch bản bảo mật
            if (result == DialogResult.Abort)
            {
                Application.Exit(); // Thoát khẩn cấp nếu sai quá 3 lần
            }
        }
        private void taoTaiKhoan_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormManager.OpenModal<Form3_DangKyTaiKhoan>(this);
        }
        private void TuDongNhungFormKiemTraCSDL()
        {
            try
            {
                // Kiểm tra an toàn: Form chưa có hoặc đã bị Dispose
                if (_form33CSDL == null || _form33CSDL.IsDisposed)
                {
                    // Tạm khóa luồng vẽ UI của tabPage3 để chống chớp giật (Flicker)
                    tabPage3.SuspendLayout();

                    _form33CSDL = new Form33_KiemTraSucKhoeCSDL
                    {
                        Text = "Kiểm tra cơ sở dữ liệu",
                        TopLevel = false,
                        FormBorderStyle = FormBorderStyle.None,
                        Dock = DockStyle.Fill
                    };

                    // Thu hồi RAM triệt để khi người dùng đóng Form33
                    _form33CSDL.FormClosed += (s, ev) =>
                    {
                        if (tabPage3.Controls.Contains(_form33CSDL))
                        {
                            tabPage3.Controls.Remove(_form33CSDL);
                        }
                        _form33CSDL.Dispose(); // Ép hệ thống nhả RAM
                        _form33CSDL = null;    // Cắt đứt tham chiếu
                    };

                    // Thêm vào tabPage3 và mở khóa vẽ UI
                    tabPage3.Controls.Add(_form33CSDL);
                    tabPage3.ResumeLayout();
                }

                // Hiển thị, đẩy lên trên cùng và cấp quyền Focus
                if (!_form33CSDL.Visible)
                {
                    _form33CSDL.Show();
                }
                _form33CSDL.BringToFront();
                _form33CSDL.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Không thể tải giao diện kiểm tra cơ sở dữ liệu.\nChi tiết: {ex.Message}",
                    "Lỗi hệ thống",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void ToiUuControl(Control control)
        {
            if (control == null) return;

            control.SuspendLayout();

            // Bật DoubleBuffer cho mọi control
            typeof(Control).GetProperty(
                "DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance)
                ?.SetValue(control, true, null);

            foreach (Control child in control.Controls)
            {
                ToiUuControl(child);
            }

            control.ResumeLayout(false);
        }
        #region CÁC SỰ KIỆN GIAO DIỆN (EVENTS)
        private void pictureBox9_ThongTinNamSys_Click(object sender, EventArgs e)
        {
            try
            {
                // Kiểm tra control
                if (comboBox1_NamHienTai == null)
                {
                    HienThiFormAo_Loi(
                        "LỖI HỆ THỐNG",
                        "Không tìm thấy ô chọn năm hệ thống.");

                    return;
                }

                // Kiểm tra dữ liệu rỗng
                if (string.IsNullOrWhiteSpace(comboBox1_NamHienTai.Text))
                {
                    HienThiFormAo_CanhBao(
                        "THIẾU THÔNG TIN",
                        "Đồng chí vui lòng chọn năm hệ thống trước khi sử dụng.");

                    comboBox1_NamHienTai.Focus();
                    return;
                }

                // Kiểm tra dữ liệu số
                if (!int.TryParse(comboBox1_NamHienTai.Text.Trim(), out int namHeThong))
                {
                    HienThiFormAo_CanhBao(
                        "DỮ LIỆU KHÔNG HỢP LỆ",
                        $"Giá trị [{comboBox1_NamHienTai.Text}] không phải là một năm hợp lệ.");

                    comboBox1_NamHienTai.Focus();
                    return;
                }

                string message =
                    $"Năm hệ thống hiện tại: {namHeThong}\n\n" +
                    $"  • Đây là năm được sử dụng chung cho toàn bộ phần mềm.\n" +
                    $"  • Đồng chí chỉ cần cài đặt một lần để đảm bảo dữ liệu đồng bộ.\n\n" +
                    $"Lưu ý: Nếu máy tính bị sai ngày giờ (do cạn pin CMOS), đồng chí\n" +
                    $"có thể chủ động chỉnh lại năm tại đây để hệ thống kết xuất chuẩn xác.";

                HienThiFormAo_ThongTin(
                    "THÔNG TIN NĂM HỆ THỐNG",
                    message);
            }
            catch (Exception ex)
            {
                HienThiFormAo_Loi(
                    "LỖI XỬ LÝ",
                    $"Đã xảy ra lỗi trong quá trình kiểm tra năm hệ thống:\n{ex.Message}");
            }
        }
        private void kryptonButton1_CaiDatFileExcel_Click(object sender, EventArgs e)
        {
            try
            {
                string msg =
                    "Do mỗi máy tính có tỷ lệ màn hình và độ phân giải khác nhau, việc định dạng\n" +
                    "trang in Excel có thể bị tràn hoặc hiển thị chưa phù hợp.\n\n" +

                    "Chức năng này cho phép Quản trị viên điều chỉnh kích thước trang của các\n" +
                    "Sheet nhằm tối ưu hiển thị cho từng thiết bị.\n\n" +

                    "⚠️ LƯU Ý QUAN TRỌNG:\n" +
                    "  • Chỉ thay đổi kích thước trang (Page Setup / Scale).\n" +
                    "  • Tuyệt đối KHÔNG chỉnh sửa nội dung dữ liệu bên trong.\n" +
                    "  • Việc thay đổi nội dung sai quy cách có thể gây sai lệch hệ thống.\n\n" +

                    "Hệ thống sẽ yêu cầu xác thực quyền Admin.\n" +
                    "Đồng chí có muốn tiếp tục không?";

                if (!HienThiFormAo_XacNhan(
                    "CẤU HÌNH HIỂN THỊ EXCEL",
                    msg))
                {
                    return;
                }

                using (var frm = new Form24_XacMinhAdmin())
                {
                    frm.StartPosition = FormStartPosition.CenterParent;

                    if (frm.ShowDialog(this) == DialogResult.OK)
                    {
                        MoFileExcel();
                    }
                }
            }
            catch (Exception ex)
            {
                HienThiFormAo_Loi(
                    "LỖI MỞ EXCEL",
                    ex.Message);
            }
        }
        #endregion
        #region FORM ẢO ENTERPRISE TỐI ƯU HIỆU SUẤT
        private FormAoBase TaoFormAoCoBan(
            string titleText,
            Size formSize)
        {
            var formAo = new FormAoBase
            {
                Text = titleText,
                Size = formSize,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowIcon = false,
                ShowInTaskbar = false,
                KeyPreview = true,
                TopMost = true
            };

            formAo.SuspendLayout();

            return formAo;
        }
        private KryptonPanel TaoPanelTop(
            string tieuDe,
            Color mauChu)
        {
            var panelTop = new KryptonPanel
            {
                Dock = DockStyle.Top,
                Height = 65,
                Padding = new Padding(25, 20, 20, 5)
            };

            panelTop.StateCommon.Color1 = Color.White;

            var lblTitle = new KryptonLabel
            {
                Text = tieuDe.ToUpper(),
                Dock = DockStyle.Fill,
                AutoSize = false
            };

            lblTitle.StateCommon.ShortText.Font =
                new Font("Segoe UI", 10F, FontStyle.Bold);

            lblTitle.StateCommon.ShortText.Color1 = mauChu;

            panelTop.Controls.Add(lblTitle);

            return panelTop;
        }
        private KryptonPanel TaoPanelNoiDung()
        {
            var panelContent = new KryptonPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 15, 25, 20)
            };

            panelContent.StateCommon.Color1 = Color.White;

            return panelContent;
        }
        private KryptonTextBox TaoTextNoiDung(
            string noiDung,
            Point location,
            Size size)
        {
            var txtContent = new KryptonTextBox
            {
                Text = noiDung,
                ReadOnly = true,
                Multiline = true,
                WordWrap = true,
                ScrollBars = ScrollBars.Vertical,
                Location = location,
                Size = size,
                Anchor = AnchorStyles.Top |
                         AnchorStyles.Bottom |
                         AnchorStyles.Left |
                         AnchorStyles.Right,
                TabStop = false
            };

            txtContent.StateCommon.Back.Color1 = Color.White;
            txtContent.StateCommon.Border.DrawBorders =
                PaletteDrawBorders.None;

            txtContent.StateCommon.Content.Font =
                new Font("Segoe UI", 10.5F, FontStyle.Regular);

            txtContent.StateCommon.Content.Color1 =
                Color.FromArgb(40, 40, 40);

            txtContent.StateCommon.Content.Padding =
                new Padding(0);

            return txtContent;
        }
        private PictureBox TaoIcon(
            Bitmap bmp,
            Point location)
        {
            return new PictureBox
            {
                Image = bmp,
                SizeMode = PictureBoxSizeMode.CenterImage,
                Size = new Size(50, 50),
                Location = location
            };
        }
        private Panel TaoPanelBottom(int height = 60)
        {
            return new Panel
            {
                Dock = DockStyle.Bottom,
                Height = height,
                BackColor = Color.WhiteSmoke
            };
        }
        private KryptonButton TaoButton(
            string text,
            Size size,
            DialogResult result)
        {
            var btn = new KryptonButton
            {
                Text = text,
                Size = size,
                DialogResult = result
            };

            btn.StateCommon.Content.ShortText.Font =
                new Font("Segoe UI", 9.5F, FontStyle.Bold);

            btn.StateCommon.Border.Rounding = 6;

            return btn;
        }
        private void HienThiFormAo_ThongTin(
            string tieuDe,
            string noiDung)
        {
            string nd = noiDung.Replace("\n", Environment.NewLine);

            using (var formAo = TaoFormAoCoBan(
                "Thông báo hệ thống",
                new Size(740, 420)))
            {
                var panelTop = TaoPanelTop(
                    tieuDe,
                    Color.FromArgb(0, 82, 155));

                var panelContent = TaoPanelNoiDung();

                var picIcon = TaoIcon(
                    SystemIcons.Information.ToBitmap(),
                    new Point(20, 15));

                var txtContent = TaoTextNoiDung(
                    nd,
                    new Point(80, 15),
                    new Size(620, 300));

                var panelBottom = TaoPanelBottom();

                var btnClose = TaoButton(
                    "Đã hiểu",
                    new Size(120, 35),
                    DialogResult.OK);

                btnClose.Location =
                    new Point((formAo.Width - btnClose.Width) / 2, 12);

                panelBottom.Controls.Add(btnClose);

                panelContent.Controls.Add(picIcon);
                panelContent.Controls.Add(txtContent);

                formAo.Controls.Add(panelContent);
                formAo.Controls.Add(panelTop);
                formAo.Controls.Add(panelBottom);

                formAo.AcceptButton = btnClose;
                formAo.CancelButton = btnClose;

                formAo.ResumeLayout(false);

                formAo.Shown += (s, e) =>
                {
                    btnClose.Focus();
                };

                formAo.ShowDialog(this);
            }
        }
        private bool HienThiFormAo_XacNhan(
            string tieuDe,
            string noiDung)
        {
            string nd = noiDung.Replace("\n", Environment.NewLine);

            bool dongY = false;

            using (var formAo = TaoFormAoCoBan(
                "Xác nhận thao tác",
                new Size(850, 620)))
            {
                var panelTop = TaoPanelTop(
                    tieuDe,
                    Color.FromArgb(0, 82, 155));

                var panelContent = TaoPanelNoiDung();

                var picIcon = TaoIcon(
                    SystemIcons.Question.ToBitmap(),
                    new Point(25, 15));

                var txtContent = TaoTextNoiDung(
                    nd,
                    new Point(90, 15),
                    new Size(700, 470));

                var panelBottom = TaoPanelBottom(75);

                var btnYes = TaoButton(
                    "Đồng ý",
                    new Size(140, 42),
                    DialogResult.Yes);

                var btnNo = TaoButton(
                    "Hủy bỏ",
                    new Size(140, 42),
                    DialogResult.No);

                btnYes.Click += (s, e) => dongY = true;
                btnNo.Click += (s, e) => dongY = false;

                int totalWidth = btnYes.Width + 20 + btnNo.Width;
                int startX = (formAo.Width - totalWidth) / 2;

                btnYes.Location = new Point(startX, 16);

                btnNo.Location = new Point(
                    startX + btnYes.Width + 20,
                    16);

                panelBottom.Controls.Add(btnYes);
                panelBottom.Controls.Add(btnNo);

                panelContent.Controls.Add(picIcon);
                panelContent.Controls.Add(txtContent);

                formAo.Controls.Add(panelContent);
                formAo.Controls.Add(panelTop);
                formAo.Controls.Add(panelBottom);

                formAo.AcceptButton = btnYes;
                formAo.CancelButton = btnNo;

                formAo.ResumeLayout(false);

                formAo.Shown += (s, e) =>
                {
                    btnNo.Focus();
                };

                formAo.ShowDialog(this);
            }

            return dongY;
        }
        private void HienThiFormAo_CanhBao(
            string tieuDe,
            string noiDung)
        {
            string nd = noiDung.Replace("\n", Environment.NewLine);

            using (var formAo = TaoFormAoCoBan(
                "Lưu ý thao tác",
                new Size(800, 360)))
            {
                var panelTop = TaoPanelTop(
                    tieuDe,
                    Color.FromArgb(211, 84, 0));

                var panelContent = TaoPanelNoiDung();

                var picIcon = TaoIcon(
                    SystemIcons.Warning.ToBitmap(),
                    new Point(20, 10));

                var txtContent = TaoTextNoiDung(
                    nd,
                    new Point(80, 15),
                    new Size(660, 220));

                var panelBottom = TaoPanelBottom();

                var btnClose = TaoButton(
                    "Quay lại",
                    new Size(120, 35),
                    DialogResult.OK);

                btnClose.Location =
                    new Point((formAo.Width - btnClose.Width) / 2, 12);

                panelBottom.Controls.Add(btnClose);

                panelContent.Controls.Add(picIcon);
                panelContent.Controls.Add(txtContent);

                formAo.Controls.Add(panelContent);
                formAo.Controls.Add(panelTop);
                formAo.Controls.Add(panelBottom);

                formAo.AcceptButton = btnClose;
                formAo.CancelButton = btnClose;

                formAo.ResumeLayout(false);

                formAo.Shown += (s, e) =>
                {
                    btnClose.Focus();
                };

                formAo.ShowDialog(this);
            }
        }
        private void HienThiFormAo_Loi(
            string tieuDe,
            string noiDungLoi)
        {
            string nd = noiDungLoi.Replace("\n", Environment.NewLine);

            using (var formAo = TaoFormAoCoBan(
                "Hệ thống ghi nhận sự cố",
                new Size(550, 320)))
            {
                var panelTop = TaoPanelTop(
                    tieuDe,
                    Color.FromArgb(198, 40, 40));

                var panelContent = TaoPanelNoiDung();

                var picIcon = TaoIcon(
                    SystemIcons.Error.ToBitmap(),
                    new Point(20, 15));

                var txtContent = TaoTextNoiDung(
                    nd,
                    new Point(80, 15),
                    new Size(400, 170));

                var panelBottom = TaoPanelBottom();

                var btnCopy = TaoButton(
                    "Sao chép mã lỗi",
                    new Size(160, 35),
                    DialogResult.None);

                var btnClose = TaoButton(
                    "Đóng",
                    new Size(100, 35),
                    DialogResult.OK);

                btnCopy.Click += (s, e) =>
                {
                    try
                    {
                        Clipboard.SetText(nd);

                        MessageBox.Show(
                            formAo,
                            "Đã sao chép mã lỗi.",
                            "Thông báo",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    catch
                    {

                    }
                };

                int totalWidth = btnCopy.Width + 15 + btnClose.Width;

                int startX = (formAo.Width - totalWidth) / 2;

                btnCopy.Location = new Point(startX, 12);

                btnClose.Location = new Point(
                    startX + btnCopy.Width + 15,
                    12);

                panelBottom.Controls.Add(btnCopy);
                panelBottom.Controls.Add(btnClose);

                panelContent.Controls.Add(picIcon);
                panelContent.Controls.Add(txtContent);

                formAo.Controls.Add(panelContent);
                formAo.Controls.Add(panelTop);
                formAo.Controls.Add(panelBottom);

                formAo.AcceptButton = btnClose;
                formAo.CancelButton = btnClose;

                formAo.ResumeLayout(false);

                formAo.Shown += (s, e) =>
                {
                    btnClose.Focus();
                };

                System.Diagnostics.Debug.WriteLine(
                    $"[ERR_UI] {tieuDe} - {nd}");

                formAo.ShowDialog(this);
            }
        }
        #endregion
        private void HienThiFormAo_ThongBaoNgan(string tieuDe, string noiDung)
        {
            string nd = noiDung.Replace("\n", Environment.NewLine);

            // Sử dụng size nhỏ gọn 450 x 220
            using (var formAo = TaoFormAoCoBan("Thông báo hệ thống", new Size(530, 310)))
            {
                var panelTop = TaoPanelTop(tieuDe, Color.FromArgb(0, 82, 155));
                var panelContent = TaoPanelNoiDung();
                var picIcon = TaoIcon(SystemIcons.Information.ToBitmap(), new Point(20, 15));

                // Khung chữ cũng thu nhỏ lại và đặt đúng tọa độ
                var txtContent = TaoTextNoiDung(nd, new Point(80, 25), new Size(330, 60));
                txtContent.ScrollBars = ScrollBars.None; // Tắt thanh cuộn vì nội dung rất ngắn

                var panelBottom = TaoPanelBottom(55); // Chiều cao panel đáy hạ xuống

                var btnClose = TaoButton("Đã hiểu", new Size(125, 34), DialogResult.OK);
                btnClose.Location = new Point((formAo.Width - btnClose.Width) / 2, 11);

                panelBottom.Controls.Add(btnClose);
                panelContent.Controls.Add(picIcon);
                panelContent.Controls.Add(txtContent);

                formAo.Controls.Add(panelContent);
                formAo.Controls.Add(panelTop);
                formAo.Controls.Add(panelBottom);

                formAo.AcceptButton = btnClose;
                formAo.CancelButton = btnClose;

                formAo.ResumeLayout(false);
                formAo.Shown += (s, e) => btnClose.Focus();

                formAo.ShowDialog(this);
            }
        }
        private void ThayDoiKichThuocChu(float step)
        {
            // ===== CHỐNG SPAM CLICK =====
            if (Interlocked.Exchange(ref _isChangingFont, 1) == 1)
                return;

            try
            {
                if (IsDisposed || Disposing)
                    return;

                if (richTextBox1_TomTatGhiChu == null ||
                    richTextBox1_TomTatGhiChu.IsDisposed)
                    return;

                // ===== TÍNH CỠ CHỮ MỚI =====
                float newSize = _currentFontSize + step;

                // ===== GIỚI HẠN AN TOÀN =====
                if (newSize < FONT_SIZE_MIN)
                    newSize = FONT_SIZE_MIN;

                if (newSize > FONT_SIZE_MAX)
                    newSize = FONT_SIZE_MAX;
                // ===== KHÔNG ĐỔI => HIỆN MSG & THOÁT =====
                if (Math.Abs(newSize - _currentFontSize) < 0.1f)
                {
                    // Kiểm tra chiều hướng bấm (step > 0 là đang bấm tăng, ngược lại là giảm)
                    if (step > 0)
                    {
                        HienThiFormAo_ThongBaoNgan(
                            "GIỚI HẠN CỠ CHỮ",
                            $"Cỡ chữ đã đạt mức lớn nhất là {FONT_SIZE_MAX}.");
                    }
                    else if (step < 0)
                    {
                        HienThiFormAo_ThongBaoNgan(
                            "GIỚI HẠN CỠ CHỮ",
                            $"Cỡ chữ đã đạt mức nhỏ nhất là {FONT_SIZE_MIN}.");
                    }
                    return;
                }
                // ===== LẤY FONT GỐC =====
                Font oldDynamicFont = _dynamicRichTextFont;

                Font currentFont = richTextBox1_TomTatGhiChu.Font;

                // ===== TẠO FONT MỚI =====
                Font newFont = new Font(
                    currentFont.FontFamily,
                    newSize,
                    currentFont.Style,
                    GraphicsUnit.Point);

                // ===== TREO RENDER =====
                richTextBox1_TomTatGhiChu.SuspendLayout();

                try
                {
                    richTextBox1_TomTatGhiChu.Font = newFont;
                    _currentFontSize = newSize;

                    // Chỉ lưu font do ta tạo
                    _dynamicRichTextFont = newFont;
                }
                finally
                {
                    richTextBox1_TomTatGhiChu.ResumeLayout();
                }

                // ===== GIẢI PHÓNG FONT CŨ =====
                // Dispose SAU KHI control đã dùng font mới
                oldDynamicFont?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Font Resize Error] {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _isChangingFont, 0);
            }
        }
        private void kryptonButton2_TangCoChuRichText_Click(object sender, EventArgs e)
        {
            ThayDoiKichThuocChu(FONT_SIZE_STEP);
        }
        private void kryptonButton2_GiamCoChuRichText_Click(object sender, EventArgs e)
        {
            ThayDoiKichThuocChu(-FONT_SIZE_STEP);
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                _dynamicRichTextFont?.Dispose();
                _dynamicRichTextFont = null;
            }
            catch { }

            base.OnFormClosing(e);
        }
        private void kryptonButton1_CapNhatDanhSachDonVi_Click(object sender, EventArgs e)
        {
            FormManager.OpenModal<Form20_DonVi>(this);
        }
        private void kryptonButton1_CaiDatTyLePhanTramE29_Click(object sender, EventArgs e)
        {
            FormManager.OpenModal<Form27_TyLeQuyDinhE29>(this);
        }
        private void kryptonButton1_CapNhatChucVu_Click(object sender, EventArgs e)
        {
            FormManager.OpenModal<Form21_ChucVu>(this);
        }
        private void kryptonButton_CapNhatDanhSachChiHuyD_Click(object sender, EventArgs e)
        {
            FormManager.OpenModal<Form13_DSChiHuy>(this);
        }
        // ⭐ HÀM DÙNG CHUNG MỞ FORM CACHE RAM
        private T ShowCachedForm<T>(T cachedForm)
     where T : Form, new()
        {
            try
            {
                if (cachedForm == null || cachedForm.IsDisposed)
                {
                    cachedForm = new T
                    {
                        StartPosition = FormStartPosition.CenterParent
                    };
                }

                if (cachedForm.Visible)
                {
                    cachedForm.BringToFront();
                    cachedForm.Activate();

                    return cachedForm;
                }

                cachedForm.ShowDialog(this);

                if (cachedForm.IsDisposed)
                {
                    return null;
                }

                return cachedForm;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[Lỗi mở Form {typeof(T).Name}] {ex.Message}");

                return null;
            }
        }
        // ============================================================================
        // SAO LƯU DỮ LIỆU - CHUẨN HÓA HỆ THỐNG
        // Thiết kế:
        // - Không dùng MessageBox
        // - Kiểm tra đầy đủ trước khi thoát app
        // - Không crash UI
        // - Dùng ProcessStartInfo an toàn
        // - Tách nhỏ logic bảo trì lâu dài
        // - Tương thích môi trường nội bộ 5~10 năm
        // ============================================================================
        private void kryptonButton1_SaoLuu_Click(object sender, EventArgs e)
        {
            try
            {
                const string tieuDe = "SAO LƯU DỮ LIỆU HỆ THỐNG";

                string noiDung =
                    "Hệ thống sẽ thực hiện sao lưu toàn bộ cơ sở dữ liệu hiện tại.\n\n" +

                    "Trong quá trình này:\n" +
                    "  • Phần mềm sẽ tự động đóng tạm thời.\n" +
                    "  • Chương trình sao lưu riêng sẽ được kích hoạt.\n" +
                    "  • Dữ liệu hiện tại sẽ được bảo toàn trước khi xử lý.\n\n" +

                    "⚠️ LƯU Ý:\n" +
                    "  • Không tắt máy tính trong quá trình sao lưu.\n" +
                    "  • Không chạy nhiều phiên bản phần mềm cùng lúc.\n" +
                    "  • Đảm bảo ổ đĩa còn đủ dung lượng.\n\n" +

                    "Đồng chí có muốn tiếp tục không?";

                if (!HienThiFormAo_XacNhan(tieuDe, noiDung))
                    return;

                // =========================================================
                // KIỂM TRA DATABASE
                // =========================================================

                string thuMucDatabase = Path.Combine(
                    AppContext.BaseDirectory,
                    "Database");

                if (!Directory.Exists(thuMucDatabase))
                {
                    HienThiFormAo_CanhBao(
                        "KHÔNG TÌM THẤY DỮ LIỆU",
                        "Hệ thống không tìm thấy thư mục Database.\n\n" +
                        "Vui lòng kiểm tra lại cấu trúc cài đặt.");
                    return;
                }

                string[] dsBatBuoc =
                {
            "csdl1.db",
            "csdl2.db",
            "csdl3.db",
            "csdl4.db",
            "csdlex.xlsx"
        };

                var dsThieu = dsBatBuoc
                    .Where(f => !File.Exists(
                        Path.Combine(thuMucDatabase, f)))
                    .ToList();

                if (dsThieu.Count > 0)
                {
                    HienThiFormAo_CanhBao(
                        "CƠ SỞ DỮ LIỆU KHÔNG ĐẦY ĐỦ",
                        "Hệ thống phát hiện thiếu các tệp dữ liệu sau:\n\n- " +
                        string.Join("\n- ", dsThieu) +
                        "\n\nVui lòng kiểm tra lại bộ cài hoặc dữ liệu hệ thống.");

                    return;
                }

                // =========================================================
                // KIỂM TRA FILE SAO LƯU
                // =========================================================

                string exeSaoLuu = Path.Combine(
                    AppContext.BaseDirectory,
                    "ServiceBackup.exe");

                if (!File.Exists(exeSaoLuu))
                {
                    HienThiFormAo_Loi(
                        "KHÔNG TÌM THẤY DỊCH VỤ SAO LƯU",
                        "Hệ thống không tìm thấy tệp:\n\n" +
                        "ServiceBackup.exe\n\n" +
                        "Vui lòng kiểm tra lại thư mục cài đặt.");

                    return;
                }

                // =========================================================
                // KHỞI CHẠY SERVICE SAO LƯU
                // =========================================================

                var psi = new ProcessStartInfo
                {
                    FileName = exeSaoLuu,
                    WorkingDirectory = AppContext.BaseDirectory,
                    UseShellExecute = true
                };

                try
                {
                    // Cấp quyền cho ServiceBackup
                    string thuMucBackup = Path.Combine(
                        AppContext.BaseDirectory,
                        "Database",
                        "Bansaoluu"
                    );

                    Directory.CreateDirectory(thuMucBackup);

                    string licPath = Path.Combine(
                        thuMucBackup,
                        "GiayPhepCapQuyen_ServiceBackup.dat"
                    );

                    Module_CapQuyenService.TaoGiayPhep(
                        "ServiceBackup",
                        licPath
                    );
                }
                catch (Exception ex)
                {
                    HienThiFormAo_Loi(
                        "LỖI CẤP QUYỀN",
                        "Không thể tạo file xác thực: " + ex.Message
                    );

                    return;
                }

                Process.Start(psi);

                Application.Exit();

                // ĐÓNG HỆ THỐNG CHÍNH
            }
            catch (Exception ex)
            {
                HienThiFormAo_Loi(
                    "LỖI SAO LƯU DỮ LIỆU",
                    ex.Message);

                System.Diagnostics.Debug.WriteLine(
                    $"[BACKUP_ERROR] {ex}");
            }
        }
        // ============================================================================
        // KHÔI PHỤC DỮ LIỆU - CHUẨN HÓA HỆ THỐNG
        // Thiết kế:
        // - Kiểm tra tính toàn vẹn trước khi khôi phục
        // - Không dùng MessageBox
        // - Chống crash khi thiếu file
        // - Dễ bảo trì lâu dài
        // ============================================================================
        private void kryptonButton1_Khoiphuc_Click(object sender, EventArgs e)
        {
            try
            {
                const string tieuDe = "KHÔI PHỤC DỮ LIỆU HỆ THỐNG";

                string noiDung =
                    "Hệ thống sẽ tiến hành khôi phục dữ liệu từ bản sao lưu.\n\n" +

                    "Trong quá trình này:\n" +
                    "  • Phần mềm hiện tại sẽ tự động đóng.\n" +
                    "  • Dịch vụ khôi phục dữ liệu sẽ được kích hoạt.\n" +
                    "  • Dữ liệu hiện tại có thể bị ghi đè.\n\n" +

                    "⚠️ LƯU Ý QUAN TRỌNG:\n" +
                    "  • Không tắt máy tính giữa quá trình xử lý.\n" +
                    "  • Chỉ khôi phục từ nguồn dữ liệu tin cậy.\n" +
                    "  • Nên sao lưu dữ liệu hiện tại trước khi tiếp tục.\n\n" +

                    "Đồng chí có muốn tiếp tục không?";

                if (!HienThiFormAo_XacNhan(tieuDe, noiDung))
                    return;

                // =========================================================
                // KIỂM TRA CSDL HIỆN TẠI
                // =========================================================

                if (!KiemTraDayDuCSDL(out string thongBaoThieu))
                {
                    HienThiFormAo_CanhBao(
                        "CƠ SỞ DỮ LIỆU KHÔNG HỢP LỆ",
                        thongBaoThieu);

                    return;
                }

                // =========================================================
                // KIỂM TRA FILE SERVICE KHÔI PHỤC
                // =========================================================

                string exeKhoiPhuc = Path.Combine(
                    AppContext.BaseDirectory,
                    "ServiceRestore.exe");

                if (!File.Exists(exeKhoiPhuc))
                {
                    HienThiFormAo_Loi(
                        "KHÔNG TÌM THẤY DỊCH VỤ KHÔI PHỤC",
                        "Hệ thống không tìm thấy tệp:\n\n" +
                        "ServiceRestore.exe\n\n" +
                        "Vui lòng kiểm tra lại thư mục cài đặt.");

                    return;
                }

                // =========================================================
                // KHỞI ĐỘNG DỊCH VỤ KHÔI PHỤC
                // =========================================================

                var psi = new ProcessStartInfo
                {
                    FileName = exeKhoiPhuc,
                    WorkingDirectory = AppContext.BaseDirectory,
                    UseShellExecute = true
                };

                try
                {
                    // Cấp quyền cho ServiceRestore
                    string thuMucBackup = Path.Combine(
                        AppContext.BaseDirectory,
                        "Database",
                        "Bansaoluu"
                    );

                    Directory.CreateDirectory(thuMucBackup);

                    string licPath = Path.Combine(
                        thuMucBackup,
                        "GiayPhepCapQuyen_ServiceRestore.dat"
                    );

                    Module_CapQuyenService.TaoGiayPhep(
                        "ServiceRestore",
                        licPath
                    );
                }
                catch (Exception ex)
                {
                    HienThiFormAo_Loi(
                        "LỖI CẤP QUYỀN",
                        "Không thể tạo file xác thực: " + ex.Message
                    );

                    return;
                }

                Process.Start(psi);

                // =========================================================
                // THOÁT PHẦN MỀM CHÍNH
                // =========================================================

                Application.Exit();
            }
            catch (Exception ex)
            {
                HienThiFormAo_Loi(
                    "LỖI KHÔI PHỤC DỮ LIỆU",
                    ex.Message);

                System.Diagnostics.Debug.WriteLine(
                    $"[RESTORE_ERROR] {ex}");
            }
        }
        private void kryptonButton2_ChuyenGiaoDuLieu_Click(
    object sender,
    EventArgs e)
        {
            try
            {
                var formCha = Application.OpenForms
                    .OfType<Form2_FormCha>()
                    .FirstOrDefault();

                if (formCha == null)
                    return;

                formCha.OpenChildForm<Form31_ChuyenGiaoDuLieu>(
                    "Chuyển giao dữ liệu");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);

                MessageBox.Show(
                    "Không thể mở trang Chuyển giao dữ liệu.\n"
                    + ex.Message,
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        private sealed class CauHinhData
        {
            // Bảng ThongTin (Tab 1)
            public string TenTrungDoan { get; set; }
            public string TenTieuDoan { get; set; }
            public string TomTat { get; set; }
            public string DoiTuongPhanMem { get; set; }

            // Bảng ThongTin (Tab 2)
            public string SoLanChoPhepHienThi { get; set; }
            public string ChoPhepGoiYMatKhau { get; set; }
            public string ChoPhepDupChuotVaoAnh { get; set; }

            // Các bảng thiết lập rời rạc (Tab 2)
            public string SuKienThoatPhanMem { get; set; }
            public string TuDongXoaNhatKy { get; set; }
            public string CheDoXemHuongDan { get; set; }
            public string ThoiGianThayDoiAnh { get; set; }
            // ⭐ BỔ SUNG THÊM 2 TRƯỜNG Ở TAB 1
            public string TenTrungDoanDong1 { get; set; }
            public string KyHieuBaoCao { get; set; }
        }
        private CauHinhData CaptureUIData()
        {
            // Đảm bảo không bị văng lỗi NullReference nếu các control chưa kịp khởi tạo
            return new CauHinhData
            {
                TenTrungDoan = textBox_TrungDoanCSCD.Text?.Trim() ?? "",
                TenTieuDoan = comboBox_TenTieuDoan.Text?.Trim() ?? "",
                TomTat = richTextBox1_TomTatGhiChu.Text?.Trim() ?? "",
                DoiTuongPhanMem = comboBox_DoiTuongPhanMem.Text?.Trim() ?? "",

                SoLanChoPhepHienThi = comboBox_SoLuongDongChoPhepHienThi.Text?.Trim() ?? "",
                ChoPhepGoiYMatKhau = checkBox1_ChoPhepGoiYTenTaiKhoan.Checked ? "TRUE" : "FALSE",
                ChoPhepDupChuotVaoAnh = checkBox1_ChoPhepDupChuotVaoAnhGoiYssaP.Checked ? "TRUE" : "FALSE",

                // 🔥 BẮT CHẶT CÁC GIÁ TRỊ TỪ TAB 2
                SuKienThoatPhanMem = comboBox1_ChonSuKienThoat.Text?.Trim() ?? "Thoát ngay",
                TuDongXoaNhatKy = comboBox1_TuDongXoaNhatKy.Text?.Trim() ?? "Không xóa",
                CheDoXemHuongDan = comboBox1_XemHuongDanSuDung.Text?.Trim() ?? "Chế độ web",
                ThoiGianThayDoiAnh = comboBox1_ThoiGianThayDoiAnh?.Text?.Trim() ?? "Mặc định", // ⭐ BẮT DỮ LIỆU TỪ TAB 1 CHO VÀO ĐÂY
                TenTrungDoanDong1 = textBox1_TenTrungDoanDong1.Text?.Trim() ?? "",
                KyHieuBaoCao = textBox1_KyHieuBaoCao.Text?.Trim() ?? ""
            };
        }

        private void kryptonButton1_BoQuaKiemTraTyLeDoViDacBiet_Click(object sender, EventArgs e)
        {
            try
            {
                // Kiểm tra xem Form40 đã được khởi tạo trong bộ nhớ RAM chưa
                var form40 = Application.OpenForms.OfType<Form40_BoQuaDonViCanhBaoTyLe>().FirstOrDefault();

                if (form40 == null)
                {
                    // Trường hợp 1: Form chưa mở -> Tiến hành tạo mới và hiển thị
                    form40 = new Form40_BoQuaDonViCanhBaoTyLe();
                    form40.Show();
                }
                else
                {
                    // Trường hợp 2: Form đã mở sẵn ở đâu đó dưới nền -> Khôi phục và đẩy lên trên cùng
                    if (form40.WindowState == FormWindowState.Minimized)
                    {
                        form40.WindowState = FormWindowState.Normal;
                    }
                    form40.BringToFront();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi gọi Form40: {ex.Message}");
            }
        }
    }
}//Ngoài luồng