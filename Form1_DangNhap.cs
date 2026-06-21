using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace PhanMemThiDua2026
{
    public partial class Form1 : Form
    {
        private readonly string _csdl1Path = Module_DanduongGPS.DuongDanCSDL1;
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private readonly Icon? _appIcon;
        public DateTime ThoiGianDangNhap { get; private set; }
        private const int GioiHanGoiYMatKhau = 1;
        private int _soLanDaGoiYMatKhau = 0;
        private bool _dangXuLyGoiYMatKhau = false;
        private int demSai = 0;        // Đếm số lần đăng nhập sai
        // 🌟 UX ENHANCEMENT: HIỆU ỨNG FOCUS CHUẨN WINDOWS 10/11
        private static readonly Color FocusBorderColor = Color.FromArgb(0, 120, 215); // Xanh dương Win 11
        private static readonly Color NormalBorderColor = Color.Silver;
        private const int FocusBorderWidth = 2;
        private const int NormalBorderWidth = 1;
        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            this.Shown += Form1_Show;
            // 2. Chuyển đổi byte[] từ Resource sang Icon chuẩn
            try
            {
                using (var ms = new System.IO.MemoryStream(Properties.Resources.ic_PhanMem))
                {
                    _appIcon = new Icon(ms);
                }
                this.Icon = _appIcon; // Gán icon cho Form
            }
            catch (Exception)
            {
                // Nếu lỗi, sử dụng icon mặc định của hệ thống để không crash app
            }
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            // 🔹 Gắn sự kiện nhấn phím cho 2 ô nhập liệu
            text_TenDangNhap.KeyDown += Text_TenDangNhap_KeyDown;
            text_MatKhau.KeyDown += Text_MatKhau_KeyDown;
        }
        private async void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                this.Text = "Đăng nhập phần mềm phân loại thi đua năm " + Module_NamHeThong.LayNamHeThong();

                // ===== Center Form =====
                var screen = Screen.PrimaryScreen;
                if (screen != null)
                {
                    int screenWidth = screen.Bounds.Width;
                    int screenHeight = screen.Bounds.Height;
                    int formWidth = this.Width > 0 ? this.Width : 800;
                    int formHeight = this.Height > 0 ? this.Height : 600;
                    this.Location = new Point((screenWidth - formWidth) / 2, (screenHeight - formHeight) / 2);
                }

                // ===== Mật khẩu & checkbox =====
                text_MatKhau.UseSystemPasswordChar = true;
               
                // Thêm await để giao diện đợi lấy cấu hình từ DB xong mới chạy tiếp
                await KhoiTaoCheckBox(Check_HienMatKhau, text_MatKhau);
                // ===== Gán event PictureBox1 luôn trước async =====
                PictureBox1.Click -= PictureBox1_Click;
                PictureBox1.Click += PictureBox1_Click;

                // ===== Hiển thị thông báo phiên bản tân binh =====
                string phienBan = Module_TaiKhoan.LayPhienBanPhanMem();
                if (!string.IsNullOrWhiteSpace(phienBan) &&
                    phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase))
                {
                    label1_ThongBaoPhienBan.Text = phienBan;
                    label1_ThongBaoPhienBan.Visible = true;
                }
                else
                {
                    label1_ThongBaoPhienBan.Visible = false;
                }

                // 🌟 TÍCH HỢP UX FOCUS (Chạy khi Handle Form đã sẵn sàng)
                InitFocusEffects();

                // ===== Preload Form2 vào RAM =====
                Module_KhoiDongTrangChu.PreloadForm2();
            }
            catch (Exception ex)
            {
                Module_ThongBao.Loi("Lỗi khởi tạo form: " + ex.Message);
            }
        }
        private async void Form1_Show(object sender, EventArgs e)
        {
            text_TenDangNhap.Focus();

            // ===== Gợi ý tên tài khoản (async) =====
            if (CoChoPhepGoiYTenTaiKhoan())
            {
                try
                {
                    // 🛡️ SQLITE SAFETY: ReadOnly & Timeout chống khóa file lúc Boot
                    string connStr = $"Data Source={_csdl1Path};Mode=ReadOnly;Default Timeout=10;Pooling=True;";
                    await using var conn = new SqliteConnection(connStr);
                    await conn.OpenAsync();

                    await using var cmd = new SqliteCommand("SELECT TenTaiKhoan FROM Admin WHERE ID = 1 LIMIT 1", conn);
                    object val = await cmd.ExecuteScalarAsync();

                    //if (val != null)
                    //{
                    //    text_TenDangNhap.Text = BaoMatAES.GiaiMa(val.ToString());
                    //    text_MatKhau.Focus();
                    //}
                    if (val != null)
                    {
                        text_TenDangNhap.Text = BaoMatAES.GiaiMa(val.ToString() ?? string.Empty);
                        text_MatKhau.Focus();
                    }
                }
                catch { /* Không gợi ý được → im lặng */ }
            }

            // ===== Kiểm tra LinkLabel đăng ký tài khoản mới (async) =====
            try
            {
                bool hienThiLink = true;
                if (!string.IsNullOrWhiteSpace(_csdl2Path) && File.Exists(_csdl2Path))
                {
                    // 🛡️ SQLITE SAFETY
                    string connStr = $"Data Source={_csdl2Path};Mode=ReadOnly;Default Timeout=10;Pooling=True;";
                    await using var conn = new SqliteConnection(connStr);
                    await conn.OpenAsync();

                    await using var cmd = new SqliteCommand("SELECT LinkLabel1_DangKyTaiKhoanMoi FROM ThongTin WHERE ID = 1", conn);
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null && !string.IsNullOrEmpty(result.ToString()) && BaoMatAES.GiaiMa(result.ToString() ?? string.Empty) == "TRUE")
                        //if (!string.IsNullOrEmpty(result?.ToString()) && BaoMatAES.GiaiMa(result.ToString()) == "TRUE")
                    {
                        hienThiLink = false;
                    }
                }
                LinkLabel1_DangKyTaiKhoanMoi.Visible = hienThiLink;
            }
            catch { LinkLabel1_DangKyTaiKhoanMoi.Visible = true; }

            // ===== Hiển thị phiên bản phần mềm chắc chắn =====
            this.BeginInvoke((Action)(() =>
            {
                if (label1_PhienBanPhanMem != null && this.IsHandleCreated)
                {
                    label1_PhienBanPhanMem.AutoSize = true;
                    label1_PhienBanPhanMem.Visible = true;
                    label1_PhienBanPhanMem.Text = "Phiên bản: " + Module_PhienBan.SoftwareVersion;
                    label1_PhienBanPhanMem.ForeColor = Color.Black;
                    label1_PhienBanPhanMem.BringToFront();
                }
            }));

            InitToolTips();

        }
        // 🌟 LOGIC UX: QUẢN LÝ VIỀN KRYPTON THÔNG MINH
        private IEnumerable<Control> GetAllTextBoxes()
        {
            // Trả về danh sách tĩnh để tránh đệ quy quét Form tốn CPU
            return new Control[] { text_TenDangNhap, text_MatKhau };
        }
        private void InitFocusEffects()
        {
            try
            {
                foreach (var ctrl in GetAllTextBoxes())
                {
                    if (ctrl == null || ctrl.IsDisposed) continue;

                    if (ctrl is KryptonTextBox ktb)
                    {
                        ktb.StateCommon.Border.DrawBorders = PaletteDrawBorders.All;
                        ktb.StateCommon.Border.Color1 = NormalBorderColor;
                        ktb.StateCommon.Border.Color2 = NormalBorderColor;
                        ktb.StateCommon.Border.Width = NormalBorderWidth;

                        // Chống Memory Leak: Rút event trước khi gắn
                        ktb.Enter -= KryptonTextBox_EnterFocus;
                        ktb.Leave -= KryptonTextBox_LeaveFocus;
                        ktb.Enter += KryptonTextBox_EnterFocus;
                        ktb.Leave += KryptonTextBox_LeaveFocus;
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine("Init Focus Error: " + ex.Message); }
        }
        private void RemoveFocusEvents()
        {
            try
            {
                foreach (var ctrl in GetAllTextBoxes())
                {
                    if (ctrl == null || ctrl.IsDisposed) continue;
                    if (ctrl is KryptonTextBox ktb)
                    {
                        ktb.Enter -= KryptonTextBox_EnterFocus;
                        ktb.Leave -= KryptonTextBox_LeaveFocus;
                    }
                }
            }
            catch { }
        }
        private void KryptonTextBox_EnterFocus(object? sender, EventArgs e)
        {
            //if (sender is KryptonTextBox ktb)
                if (sender != null && sender is KryptonTextBox ktb)
                {
                ktb.StateCommon.Border.Color1 = FocusBorderColor;
                ktb.StateCommon.Border.Color2 = FocusBorderColor;
                ktb.StateCommon.Border.Width = FocusBorderWidth;
                ktb.Refresh();
            }
        }
        private void KryptonTextBox_LeaveFocus(object? sender, EventArgs e)
        {
            //if (sender is KryptonTextBox ktb)
                if (sender != null && sender is KryptonTextBox ktb)
                {
                ktb.StateCommon.Border.Color1 = NormalBorderColor;
                ktb.StateCommon.Border.Color2 = NormalBorderColor;
                ktb.StateCommon.Border.Width = NormalBorderWidth;
                ktb.Refresh();
            }
        }
        // 🚀 CÁC EVENT & NGHIỆP VỤ CHÍNH
        private void Text_TenDangNhap_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                text_MatKhau.Focus();
            }
        }
        private void Text_MatKhau_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                btn_DangNhap.PerformClick();
            }
        }
        private async void PictureBox1_Click(object? sender, EventArgs e)
        {
            if (_dangXuLyGoiYMatKhau)
                return;

            _dangXuLyGoiYMatKhau = true;

            try
            {
                // =========================================================
                // 1. KIỂM TRA GIỚI HẠN
                // =========================================================
                if (_soLanDaGoiYMatKhau >= GioiHanGoiYMatKhau)
                {
                    text_MatKhau.Clear();
                    text_MatKhau.Focus();
                    return;
                }

                // =========================================================
                // 2. KIỂM TRA FILE CSDL
                // =========================================================
                if (!File.Exists(_csdl2Path) || !File.Exists(_csdl1Path))
                    return;

                // =========================================================
                // 3. KIỂM TRA CHO PHÉP GỢI Ý
                // =========================================================
                string connStr2 =
                    $"Data Source={_csdl2Path};Mode=ReadOnly;Default Timeout=5;Pooling=True;";

                await using var connThongTin = new SqliteConnection(connStr2);

                await connThongTin.OpenAsync();

                const string sqlFlag =
                    "SELECT ChoPhepDupChuotVaoAnh_GoiYMatKhau FROM ThongTin WHERE ID = 1";

                await using var cmdFlag =
                    new SqliteCommand(sqlFlag, connThongTin);

                string flagEncrypted =
                    (await cmdFlag.ExecuteScalarAsync())?.ToString() ?? "";

                string flag =
                    string.IsNullOrWhiteSpace(flagEncrypted)
                        ? "FALSE"
                        : BaoMatAES.GiaiMa(flagEncrypted);

                if (!string.Equals(flag, "TRUE", StringComparison.OrdinalIgnoreCase))
                {
                    text_MatKhau.Clear();
                    return;
                }

                // =========================================================
                // 4. ĐỌC PASSWORD
                // =========================================================
                string connStr1 =
                    $"Data Source={_csdl1Path};Mode=ReadOnly;Default Timeout=5;Pooling=True;";

                await using var connAdmin = new SqliteConnection(connStr1);

                await connAdmin.OpenAsync();

                const string sqlPass =
                    "SELECT MatKhau FROM Admin WHERE ID = 1 LIMIT 1";

                await using var cmdPass =
                    new SqliteCommand(sqlPass, connAdmin);

                string passEncrypted =
                    (await cmdPass.ExecuteScalarAsync())?.ToString() ?? "";

                if (string.IsNullOrWhiteSpace(passEncrypted))
                    return;

                string matKhau = BaoMatAES.GiaiMa(passEncrypted);

                // =========================================================
                // 5. HIỂN THỊ PASSWORD
                // =========================================================
                text_MatKhau.Text = matKhau;

                // =========================================================
                // 6. TĂNG BỘ ĐẾM
                // =========================================================
                _soLanDaGoiYMatKhau++;
            }
            catch (Exception ex)
            {
                text_MatKhau.Clear();

                Module_ThongBao.Loi(
                    "Lỗi hiển thị mật khẩu: " + ex.Message
                );
            }
            finally
            {
                _dangXuLyGoiYMatKhau = false;
            }
        }
        private async Task KhoiTaoCheckBox(CheckBox chk, KryptonTextBox? associatedTextBox = null)
        {
            if (chk == null) return;

            // 🟢 Thay thế Properties.Settings: Đọc trạng thái từ CSDL1
            if (chk == Check_HienMatKhau)
            {
                chk.Checked = await DocTrangThaiHienMatKhauAsync();
            }

            chk.ForeColor = chk.Checked ? Color.Green : Color.Red;

            if (associatedTextBox != null)
                associatedTextBox.UseSystemPasswordChar = !chk.Checked;

            chk.CheckedChanged -= Chk_CheckedChanged;
            chk.CheckedChanged += Chk_CheckedChanged;

            async void Chk_CheckedChanged(object? sender, EventArgs e)
            {
                chk.ForeColor = chk.Checked ? Color.Green : Color.Red;

                if (associatedTextBox != null)
                    associatedTextBox.UseSystemPasswordChar = !chk.Checked;

                // 🟢 Thay thế Properties.Settings.Save(): Lưu trạng thái vào CSDL1
                if (chk == Check_HienMatKhau)
                {
                    await LuuTrangThaiHienMatKhauAsync(chk.Checked);
                }
            }
        }
        // ====================================================================
        // HỆ THỐNG LƯU TRỮ CẤU HÌNH KHỞI ĐỘNG (THAY THẾ MY.SETTINGS)
        // ====================================================================

        /// <summary>
        /// Khởi tạo bảng Check_KhoiDong nếu chưa tồn tại
        /// </summary>
        private async Task KhoiTaoBangCheckKhoiDongAsync(SqliteConnection conn)
        {
            const string sqlCreate = @"
                CREATE TABLE IF NOT EXISTS ""Check_KhoiDong"" (
                    ""ID"" INTEGER NOT NULL,
                    ""GiaTri_Luu"" TEXT,
                    PRIMARY KEY(""ID"" AUTOINCREMENT)
                );";
            using var cmd = new SqliteCommand(sqlCreate, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Đọc trạng thái từ CSDL1 (Tự động fallback về false nếu có lỗi)
        /// </summary>
        private async Task<bool> DocTrangThaiHienMatKhauAsync()
        {
            if (string.IsNullOrWhiteSpace(_csdl1Path) || !File.Exists(_csdl1Path))
                return false;

            try
            {
                string connectionString = $"Data Source={_csdl1Path};Mode=ReadWrite;Default Timeout=5;Pooling=True;";
                await using var conn = new SqliteConnection(connectionString);
                await conn.OpenAsync();

                await KhoiTaoBangCheckKhoiDongAsync(conn);

                const string sqlSelect = "SELECT GiaTri_Luu FROM Check_KhoiDong WHERE ID = 1 LIMIT 1";
                await using var cmd = new SqliteCommand(sqlSelect, conn);
                object? val = await cmd.ExecuteScalarAsync();

                if (val != null && val != DBNull.Value)
                {
                    string decryptedValue = BaoMatAES.GiaiMa(val.ToString() ?? string.Empty);
                    return string.Equals(decryptedValue, "TRUE", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Lỗi đọc Check_KhoiDong] " + ex.Message);
            }
            return false; // Mặc định an toàn
        }

        /// <summary>
        /// Ghi trạng thái vào CSDL1 (Dùng UPSERT để cập nhật hoặc thêm mới)
        /// </summary>
        private async Task LuuTrangThaiHienMatKhauAsync(bool checkedState)
        {
            if (string.IsNullOrWhiteSpace(_csdl1Path)) return;

            try
            {
                string connectionString = $"Data Source={_csdl1Path};Mode=ReadWrite;Default Timeout=5;Pooling=True;";
                await using var conn = new SqliteConnection(connectionString);
                await conn.OpenAsync();

                await KhoiTaoBangCheckKhoiDongAsync(conn);

                string encryptedValue = BaoMatAES.MaHoa(checkedState ? "TRUE" : "FALSE");

                // UPSERT: Thêm mới nếu ID=1 chưa có, cập nhật nếu đã tồn tại
                const string sqlUpsert = @"
                    INSERT INTO Check_KhoiDong (ID, GiaTri_Luu) 
                    VALUES (1, @Val)
                    ON CONFLICT(ID) 
                    DO UPDATE SET GiaTri_Luu = @Val;";

                await using var cmd = new SqliteCommand(sqlUpsert, conn);
                cmd.Parameters.AddWithValue("@Val", encryptedValue);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Lỗi lưu Check_KhoiDong] " + ex.Message);
            }
        }
        private void InitToolTips()
        {
            if (toolTip1 == null) return;
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Gợi ý thao tác";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            SetTip(Check_HienMatKhau, "Hiển thị / Ẩn mật khẩu");
            SetTip(btn_DangNhap, "Đăng nhập vào hệ thống");
            SetTip(btn_Thoat, "Thoát chương trình");
        }
        private void SetTip(Control? control, string text)
        {
            if (control != null) toolTip1.SetToolTip(control, text);
        }
        private void btn_Thoat_Click(object sender, EventArgs e)
        {
            this.Close();
            Application.Exit();
        }
        private void LinkLabel1_DangKyTaiKhoanMoi_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (!LinkLabel1_DangKyTaiKhoanMoi.Enabled) return;

            try
            {
                LinkLabel1_DangKyTaiKhoanMoi.Enabled = false;

                using var frm = new Form3_DangKyTaiKhoan
                {
                    Owner = this,
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.CenterParent
                };

                this.Hide();

                var result = frm.ShowDialog(this);

                if (result == DialogResult.OK)
                {
                    string csdlPath = _csdl2Path;
                    if (string.IsNullOrWhiteSpace(csdlPath) || !File.Exists(csdlPath))
                        throw new Exception("Không tìm thấy CSDL cấu hình.");

                    // 🛡️ SQLITE SAFETY: Có Timeout và Pooling
                    string connStr = $"Data Source={csdlPath};Mode=ReadWrite;Default Timeout=15;Pooling=True;";
                    using var conn = new SqliteConnection(connStr);
                    conn.Open();

                    using var tran = conn.BeginTransaction();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tran;

                        cmd.CommandText = @"INSERT INTO ThongTin (ID) SELECT 1 WHERE NOT EXISTS (SELECT 1 FROM ThongTin WHERE ID = 1);";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"UPDATE ThongTin SET LinkLabel1_DangKyTaiKhoanMoi = @Value WHERE ID = 1;";
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@Value", BaoMatAES.MaHoa("TRUE"));
                        cmd.ExecuteNonQuery();
                    }
                    tran.Commit();

                    LinkLabel1_DangKyTaiKhoanMoi.Visible = false;
                }
            }
            catch (Exception ex)
            {
                Module_ThongBao.Loi("Lỗi xử lý đăng ký: " + ex.Message);
            }
            finally
            {
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    this.Show();
                    this.Activate();
                }
                LinkLabel1_DangKyTaiKhoanMoi.Enabled = true;
            }
        }
        private void LinkLabel_QuenMatKhau_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.Hide();

            using (Form5_QuenPass frm5 = new Form5_QuenPass())
            {
                frm5.StartPosition = FormStartPosition.CenterScreen;
                DialogResult ketQua = frm5.ShowDialog(this);

                if (ketQua == DialogResult.OK)
                {
                    DanhThucFrom2();
                    ThoiGianDangNhap = DateTime.Now;
                }
                else if (ketQua == DialogResult.Abort)
                {
                    Application.Exit();
                }
                else
                {
                    if (!this.IsDisposed && this.IsHandleCreated)
                    {
                        this.Show();
                        this.Activate();
                    }
                }
            }
        }
        private void DanhThucFrom2()
        {
            var form2 = Module_KhoiDongTrangChu.GetForm2();

            if (form2 != null && !form2.IsDisposed)
            {
                form2.StartPosition = FormStartPosition.CenterScreen;
                form2.Show();
                form2.BringToFront();
                this.Hide();
            }
            else
            {
                new Form2_FormCha().Show();
                this.Hide();
            }
        }
        private async void btn_DangNhap_Click(object sender, EventArgs e)
        {
            // 1. NGĂN CHẶN SPAM CLICK
            btn_DangNhap.Enabled = false;

            try
            {
                // --- 2. RESET MÀU NỀN VÀ KIỂM TRA RỖNG ---
                text_TenDangNhap.StateCommon.Back.Color1 = Color.White;
                text_MatKhau.StateCommon.Back.Color1 = Color.White;

                string tenNhap = text_TenDangNhap.Text.Trim();
                string mkNhap = text_MatKhau.Text.Trim();

                KryptonTextBox oCanFocus = null;

                if (string.IsNullOrWhiteSpace(tenNhap))
                {
                    text_TenDangNhap.StateCommon.Back.Color1 = Color.LightGreen;
                    oCanFocus = text_TenDangNhap;
                }

                if (string.IsNullOrWhiteSpace(mkNhap))
                {
                    text_MatKhau.StateCommon.Back.Color1 = Color.LightGreen;
                    oCanFocus ??= text_MatKhau;
                }

                if (oCanFocus != null)
                {
                    oCanFocus.Focus();
                    MessageBox.Show("Vui lòng nhập đầy đủ tài khoản và mật khẩu!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // ====================================================================
                // 3. MỞ KẾT NỐI CSDL BẤT ĐỒNG BỘ VỚI TIMEOUT AN TOÀN
                // ====================================================================
                string connectionString = $"Data Source={_csdl1Path};Mode=ReadOnly;Default Timeout=10;Pooling=True;";
                await using var conn = new SqliteConnection(connectionString);
                await conn.OpenAsync();

                string sqlAdmin = "SELECT encAdminUs, encAdminPa FROM Admin_DefaultValue LIMIT 1";
                await using (var cmdAdmin = new SqliteCommand(sqlAdmin, conn))
                await using (var readerAdmin = await cmdAdmin.ExecuteReaderAsync())
                {
                    if (await readerAdmin.ReadAsync())
                    {
                        //string reversedMixed1 = readerAdmin["encAdminUs"]?.ToString()?.Trim() ?? "";
                        //string reversedMixed2 = readerAdmin["encAdminPa"]?.ToString()?.Trim() ?? "";
                        string reversedMixed1 = (!readerAdmin.IsDBNull(readerAdmin.GetOrdinal("encAdminUs"))) ? readerAdmin["encAdminUs"].ToString()!.Trim() : "";
                        string reversedMixed2 = (!readerAdmin.IsDBNull(readerAdmin.GetOrdinal("encAdminPa"))) ? readerAdmin["encAdminPa"].ToString()!.Trim() : "";
                        string mixed1 = BaoMatAES.TraLaiTenChoMeoCam(reversedMixed1).Trim();
                        string mixed2 = BaoMatAES.TraLaiTenChoMeoCam(reversedMixed2).Trim();

                        if (mixed1.Length == 47 && mixed2.Length == 47)
                        {
                            string encAdminUser = mixed1.Substring(0, 24) + mixed2.Substring(24);
                            string encAdminPass = mixed2.Substring(0, 24) + mixed1.Substring(24);

                            string adminUserGiaiMa = BaoMatAES.GiaiMa(encAdminUser).Trim();
                            string adminPassGiaiMa = BaoMatAES.GiaiMa(encAdminPass).Trim();

                            if (string.Equals(tenNhap, adminUserGiaiMa, StringComparison.Ordinal) &&
                                string.Equals(mkNhap, adminPassGiaiMa, StringComparison.Ordinal))
                            {
                                XacNhanDangNhapThanhCong(tenNhap, "Đăng nhập thành công (Quyền Admin cao nhất)");
                                return;
                            }
                        }
                    }
                }

                // ====================================================================
                // BƯỚC 2: KIỂM TRA TÀI KHOẢN THƯỜNG TRONG BẢNG ADMIN
                // ====================================================================
                string sqlUser = "SELECT TenTaiKhoan, MatKhau FROM Admin";
                bool isLoginSuccess = false;

                await using (var cmdUser = new SqliteCommand(sqlUser, conn))
                await using (var readerUser = await cmdUser.ExecuteReaderAsync())
                {
                    while (await readerUser.ReadAsync())
                    {
                        string userGiaiMa = BaoMatAES.GiaiMa(readerUser.GetString(0)).Trim();
                        string passGiaiMa = BaoMatAES.GiaiMa(readerUser.GetString(1)).Trim();

                        if (string.Equals(tenNhap, userGiaiMa, StringComparison.Ordinal) &&
                            string.Equals(mkNhap, passGiaiMa, StringComparison.Ordinal))
                        {
                            isLoginSuccess = true;
                            break;
                        }
                    }
                }

                // ====================================================================
                // 4. XỬ LÝ KẾT QUẢ CUỐI CÙNG
                // ====================================================================
                if (isLoginSuccess)
                {
                    XacNhanDangNhapThanhCong(tenNhap, "Đăng nhập thành công");
                }
                else
                {
                    XulyDangNhapSai(tenNhap);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối hoặc xử lý dữ liệu: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    btn_DangNhap.Enabled = true;
                }
            }
        }
        private void XacNhanDangNhapThanhCong(string tenNhap, string thongDiepNhatKy)
        {
            SessionInfo.TenTaiKhoan = tenNhap;
            SessionInfo.ThoiGianDangNhap = DateTime.Now;
            Module_NhatKy.GhiNhatKy(
                taiKhoan: tenNhap,
                hanhDong: thongDiepNhatKy,
                ghiChu: $"Thời gian: {SessionInfo.ThoiGianDangNhap:dd-MM-yyyy HH:mm:ss}"
            );
            DanhThucFrom2();
            ThoiGianDangNhap = DateTime.Now;
        }
        private void XulyDangNhapSai(string tenNhap)
        {
            demSai++;
            Module_NhatKy.GhiNhatKy(
                taiKhoan: tenNhap,
                hanhDong: "Đăng nhập thất bại",
                ghiChu: $"Thử {demSai}/3 lần vào lúc {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
            );

            text_MatKhau.Clear();
            text_MatKhau.Focus();

            if (demSai >= 3)
            {
                MessageBox.Show("Hệ thống nghi ngờ truy cập trái phép, phần mềm sẽ thoát!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Application.Exit();
            }
            else
            {
                MessageBox.Show($"Sai tài khoản hoặc mật khẩu! ({demSai}/3)", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private bool CoChoPhepGoiYTenTaiKhoan()
        {
            try
            {
                string dbPath = _csdl2Path;
                if (!File.Exists(dbPath)) return false;

                // 🛡️ SQLITE SAFETY: Thêm Timeout 5s vì đây chỉ là tính năng phụ (Gợi ý)
                using var cn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;Default Timeout=5;Pooling=True;");
                cn.Open();

                using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT ChoPhepGoiYMatKhau FROM ThongTin WHERE ID = @id LIMIT 1";
                cmd.Parameters.AddWithValue("@id", 1);

                object val = cmd.ExecuteScalar();
                if (val == null) return false;

                //string giaiMa = BaoMatAES.GiaiMa(val.ToString());
                string giaiMa = BaoMatAES.GiaiMa(val.ToString() ?? string.Empty);
                return string.Equals(giaiMa, "TRUE", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                RemoveFocusEvents(); // 🧹 Triệt tiêu event UI
                _appIcon?.Dispose(); // 🧹 Giải phóng Icon khỏi RAM
            }
            catch { }
            base.OnFormClosed(e);
        }
        private void label1_PhienBanPhanMem_Click(object sender, EventArgs e)
        {
            try
            {
                // Lấy chuỗi thông tin từ Module hệ thống
                string thongTin = Module_TaiKhoan.LayThongTinPhienBanVaToken();

                // Gọi Form ảo chuyên dụng
                HienThiFormAo_ThongTinPhanMem("THÔNG TIN PHIÊN BẢN VÀ TOKEN", thongTin);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi hiển thị thông tin phần mềm] {ex.Message}");
            }
        }
        /// <summary>
        /// Dựng Form động kế thừa từ FormAoBase (Anti-Flicker tuyệt đối).
        /// Thiết kế TextBox tàng hình để bôi đen, tích hợp Auto-Extract Copy Token.
        /// </summary>
        private void HienThiFormAo_ThongTinPhanMem(string tieuDe, string noiDung)
        {
            // 1. Chuyển \n thành \r\n để đảm bảo xuống dòng mượt mà trên TextBox
            string noiDungChuan = noiDung.Replace("\n", Environment.NewLine);

            // 2. Cắt bỏ dòng "THÔNG TIN PHẦN MỀM" dư thừa ở đầu chuỗi (vì ta đã có Tiêu đề Form)
            if (noiDungChuan.StartsWith("THÔNG TIN PHẦN MỀM" + Environment.NewLine + Environment.NewLine))
            {
                noiDungChuan = noiDungChuan.Replace("THÔNG TIN PHẦN MỀM" + Environment.NewLine + Environment.NewLine, "");
            }

            // ⭐ SỬ DỤNG LỚP CƠ SỞ FORMAOBASE SIÊU MƯỢT CỦA BÁC
            using (var formAo = new FormAoBase())
            {
                formAo.Text = "Thông tin phiên bản và Token";
                formAo.Size = new System.Drawing.Size(780, 520);
                formAo.ShowIcon = false;
                formAo.ShowInTaskbar = false;

                // --- 1. PANEL TIÊU ĐỀ ---
                var panelTop = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(25, 20, 20, 5) };
                panelTop.StateCommon.Color1 = System.Drawing.Color.White;

                var lblTitle = new Krypton.Toolkit.KryptonLabel { Text = tieuDe.ToUpper(), Dock = DockStyle.Fill, AutoSize = false };
                lblTitle.StateCommon.ShortText.Font = new System.Drawing.Font("Segoe UI", 11.5F, System.Drawing.FontStyle.Bold);
                lblTitle.StateCommon.ShortText.Color1 = System.Drawing.Color.FromArgb(0, 82, 155); // Xanh đại dương
                panelTop.Controls.Add(lblTitle);

                // --- 2. ĐƯỜNG KẺ NGANG (Separator) ---
                var separator = new Label { Height = 1, Dock = DockStyle.Top, BackColor = System.Drawing.Color.FromArgb(220, 220, 220), Margin = new Padding(0, 5, 0, 10) };

                // --- 3. PANEL CHỨA NỘI DUNG ---
                var panelContent = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Fill, Padding = new Padding(25, 10, 25, 20) };
                panelContent.StateCommon.Color1 = System.Drawing.Color.White;

                // --- 4. NỘI DUNG VĂN BẢN (TextBox Tàng Hình Hỗ Trợ Bôi Đen Copy) ---
                var txtContent = new Krypton.Toolkit.KryptonTextBox
                {
                    Text = noiDungChuan,
                    ReadOnly = true,
                    Multiline = true,
                    WordWrap = true,
                    ScrollBars = ScrollBars.Vertical,
                    Dock = DockStyle.Fill
                };
                txtContent.StateCommon.Back.Color1 = System.Drawing.Color.White;
                txtContent.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.None;
                txtContent.StateCommon.Content.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Regular);
                txtContent.StateCommon.Content.Color1 = System.Drawing.Color.FromArgb(45, 45, 45);
                txtContent.StateCommon.Content.Padding = new Padding(0);

                // --- 5. PANEL CHỨA NÚT BẤM ---
                var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 65, BackColor = System.Drawing.Color.WhiteSmoke };

                // Nút 1: Copy Token thông minh (Tự động trích xuất chuỗi)
                var btnCopy = new Krypton.Toolkit.KryptonButton { Text = "Sao chép Token", Width = 150, Height = 38 };
                btnCopy.StateCommon.Content.ShortText.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
                btnCopy.StateCommon.Border.Rounding = 5;
                btnCopy.Click += (s, ev) =>
                {
                    try
                    {
                        // Trích xuất phần mã Token để copy
                        string keyword = "Token hệ thống: ";
                        int idx = noiDung.LastIndexOf(keyword);
                        string tokenCopy = "";

                        if (idx >= 0)
                        {
                            tokenCopy = noiDung!.Substring(idx + keyword.Length).Trim();
                        }
                        else
                        {
                            tokenCopy = noiDungChuan; // Fail-safe: Nếu không tìm thấy chữ, copy toàn bộ
                        }

                        if (!string.IsNullOrWhiteSpace(tokenCopy))
                        {
                            Clipboard.SetText(tokenCopy);
                            MessageBox.Show(formAo, "Đã sao chép Token nội bộ vào bộ nhớ tạm!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch { }
                };

                // Nút 2: Đóng
                var btnClose = new Krypton.Toolkit.KryptonButton { Text = "Đóng", Width = 110, Height = 38, DialogResult = DialogResult.OK };
                btnClose.StateCommon.Content.ShortText.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
                btnClose.StateCommon.Border.Rounding = 5;

                // Căn giữa 2 nút bấm
                int totalWidth = btnCopy.Width + 15 + btnClose.Width;
                int startX = (formAo.Width - totalWidth) / 2;

                btnCopy.Location = new System.Drawing.Point(startX, 12);
                btnClose.Location = new System.Drawing.Point(startX + btnCopy.Width + 15, 12);

                panelBottom.Controls.Add(btnCopy);
                panelBottom.Controls.Add(btnClose);

                // --- 6. XẾP LAYER VÀO FORM ---
                panelContent.Controls.Add(txtContent);
                panelContent.Controls.Add(separator);

                // Căn chỉnh Layer để Dock Fill không đè lên Dock Top
                txtContent.BringToFront();
                separator.SendToBack();

                formAo.Controls.Add(panelContent);
                formAo.Controls.Add(panelTop);
                formAo.Controls.Add(panelBottom);

                formAo.AcceptButton = btnClose;
                formAo.CancelButton = btnClose;

                // Tránh nháy nút / bôi đen toàn bộ text khi vừa mở
                formAo.Shown += (s, ev) => btnClose.Focus();

                formAo.ShowDialog(this);
            }
        }
    }
}
