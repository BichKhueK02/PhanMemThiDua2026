using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace PhanMemThiDua2026
{
    public partial class Form3_DangKyTaiKhoan : Form
    {
        private readonly string _csdl1Path = Module_DanduongGPS.DuongDanCSDL1;
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private readonly string _csdl3Path = Module_DanduongGPS.DuongDanCSDL3;
        private readonly string _csdl4Path = Module_DanduongGPS.DuongDanCSDL4;

        // CỜ CHỐNG TREO FORM (ANTI-FREEZE FLAG)
        private bool _isInitializing = false;

        // BIẾN LƯU ẢNH CHUẨN BỊ GHI VÀO CSDL
        private byte[] _hinhAnhDaiDienBytes = null;
        private byte[] _thumbnailBytes = null;

        // CẤU HÌNH ẢNH AN TOÀN
        private const int MAX_IMAGE_SIZE = 50 * 1024 * 1024; // 50MB
        private const int MAX_WIDTH = 4096;
        private const int MAX_HEIGHT = 4096;
        public Form3_DangKyTaiKhoan()
        {
            InitializeComponent();
            InitToolTips();
            this.Shown += Form3_Shown;
            // ⭐ 1. Căn giữa Form chuẩn UI/UX
            this.StartPosition = FormStartPosition.CenterScreen;
        }
        private void Form3_Load(object? sender, EventArgs e)
        {
            this.AcceptButton = btn_Luu;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // ⭐ Căn giữa thủ công lớp 2 chống lệch màn hình phụ
            var screen = Screen.PrimaryScreen;
            if (screen != null)
            {
                this.Location = new Point(
                    (screen.Bounds.Width - this.Width) / 2,
                    (screen.Bounds.Height - this.Height) / 2
                );
            }

            // Mặc định mật khẩu ẩn
            text_MatKhauMoi.UseSystemPasswordChar = true;
            text_NhapLaiMatKhau.UseSystemPasswordChar = true;
            Check_HienMatKhau.CheckedChanged += Check_HienMatKhau_CheckedChanged;

            // Thiết lập Text mặc định cho nút ảnh
            kryptonButton1_ThemAnhDaiDien.Text = "Thêm ảnh";
        }
        private void Form3_Shown(object? sender, EventArgs e)
        {
            InitCauHoiVaFocus_DangKy();

            //// ⭐ Chạy Load Ảnh trên luồng riêng để Form hiện lên ngay lập tức, không bị khựng (Freeze UI)
            //this.BeginInvoke(new Action(LoadAnhDaiDienTuCSDL));
        }
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Gợi ý thao tác";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            toolTip1.InitialDelay = 200;
            toolTip1.AutoPopDelay = 1200;
            toolTip1.ReshowDelay = 50;
            toolTip1.ShowAlways = true;

            var tips = new Dictionary<Control, string>
            {
                { Check_HienMatKhau, "Hiển thị hoặc ẩn mật khẩu đang nhập" },
                { btn_Luu, "Lưu thông tin và đăng ký tài khoản mới" },
                { btn_Thoat, "Thoát khỏi màn hình đăng ký tài khoản" }
            };

            foreach (var tip in tips)
            {
                if (tip.Key != null) toolTip1.SetToolTip(tip.Key, tip.Value);
            }
        }
        private void InitCauHoiVaFocus_DangKy()
        {
            _isInitializing = true;

            try
            {
                ComboBox1_CauHoi1.DropDownStyle = ComboBoxStyle.DropDown;
                ComboBox2_CauHoi2.DropDownStyle = ComboBoxStyle.DropDown;

                Module_KhoiTaoCSDL.NapDuLieuCauHoiBaoMat(Module_KhoiTaoCSDL.DanhSachCauHoiNhom1, ComboBox1_CauHoi1);
                Module_KhoiTaoCSDL.NapDuLieuCauHoiBaoMat(Module_KhoiTaoCSDL.DanhSachCauHoiNhom2, ComboBox2_CauHoi2);

                LoadCauHoiDaLuu();

                if (ComboBox1_CauHoi1.SelectedIndex == -1 && ComboBox1_CauHoi1.Items.Count > 0)
                    ComboBox1_CauHoi1.SelectedIndex = 0;

                if (ComboBox2_CauHoi2.SelectedIndex == -1 && ComboBox2_CauHoi2.Items.Count > 0)
                    ComboBox2_CauHoi2.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi khởi tạo Form 3: " + ex.Message);
            }
            finally
            {
                _isInitializing = false;
            }

            // ⭐ Đã fix lỗi bôi đen chữ khi Focus vào Textbox Tài Khoản
            this.ActiveControl = text_TaiKhoanMoi;
            text_TaiKhoanMoi.Focus();
            text_TaiKhoanMoi.SelectionStart = text_TaiKhoanMoi.Text.Length;
            text_TaiKhoanMoi.SelectionLength = 0;
        }
        private void LoadCauHoiDaLuu()
        {
            try
            {
                if (!File.Exists(_csdl1Path)) return;

                using var conn = new SqliteConnection($"Data Source={_csdl1Path}");
                conn.Open();

                string sql = "SELECT ID, CauHoi FROM CauHoiBaoMat WHERE ID IN (1, 2)";
                using var cmd = new SqliteCommand(sql, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string cauHoiMaHoa = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);

                    if (!string.IsNullOrWhiteSpace(cauHoiMaHoa))
                    {
                        string cauHoiGiaiMa = BaoMatAES.GiaiMa(cauHoiMaHoa);
                        if (string.IsNullOrWhiteSpace(cauHoiGiaiMa))
                        {
                            cauHoiGiaiMa = cauHoiMaHoa;
                        }

                        if (id == 1)
                        {
                            int idx = ComboBox1_CauHoi1.FindStringExact(cauHoiGiaiMa);
                            if (idx != -1) ComboBox1_CauHoi1.SelectedIndex = idx;
                            else ComboBox1_CauHoi1.Text = cauHoiGiaiMa;
                        }
                        else if (id == 2)
                        {
                            int idx = ComboBox2_CauHoi2.FindStringExact(cauHoiGiaiMa);
                            if (idx != -1) ComboBox2_CauHoi2.SelectedIndex = idx;
                            else ComboBox2_CauHoi2.Text = cauHoiGiaiMa;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi tải dữ liệu câu hỏi Form 3: " + ex.Message);
            }
        }
        private void Check_HienMatKhau_CheckedChanged(object? sender, EventArgs e)
        {
            bool hien = Check_HienMatKhau.Checked;
            text_MatKhauMoi.UseSystemPasswordChar = !hien;
            text_NhapLaiMatKhau.UseSystemPasswordChar = !hien;
            Check_HienMatKhau.ForeColor = hien ? Color.LimeGreen : Color.Red;
        }
        private void btn_Luu_Click(object? sender, EventArgs e)
        {
            string taiKhoanMoi = text_TaiKhoanMoi.Text.Trim();
            string matKhauMoi = text_MatKhauMoi.Text.Trim();
            string nhapLaiMatKhau = text_NhapLaiMatKhau.Text.Trim();
            string token = text_Token.Text.Trim();

            string cauHoi1 = ComboBox1_CauHoi1.Text.Trim();
            string traLoi1 = TextBox1_CauTraLoi1.Text.Trim();
            string cauHoi2 = ComboBox2_CauHoi2.Text.Trim();
            string traLoi2 = TextBox2_CauTraLoi2.Text.Trim();

            // 1. KIỂM TRA RỖNG
            if (string.IsNullOrWhiteSpace(taiKhoanMoi) || string.IsNullOrWhiteSpace(matKhauMoi) ||
                string.IsNullOrWhiteSpace(nhapLaiMatKhau) || string.IsNullOrWhiteSpace(token) ||
                string.IsNullOrWhiteSpace(cauHoi1) || string.IsNullOrWhiteSpace(traLoi1) ||
                string.IsNullOrWhiteSpace(cauHoi2) || string.IsNullOrWhiteSpace(traLoi2))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. KIỂM TRA ĐỘ DÀI AN TOÀN
            if (matKhauMoi.Length < 8 || token.Length < 8)
            {
                MessageBox.Show("Mật khẩu và Token phải ít nhất 8 ký tự!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 3. KIỂM TRA KHỚP MẬT KHẨU NHẬP LẠI
            if (matKhauMoi != nhapLaiMatKhau)
            {
                MessageBox.Show("Mật khẩu mới và nhập lại không khớp!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                text_NhapLaiMatKhau.Focus();
                return;
            }

            // ⭐ ĐÃ LOẠI BỎ: Kiểm tra tên tài khoản trùng mật khẩu (Theo yêu cầu của bạn)

            try
            {
                using var conn = new SqliteConnection($"Data Source={_csdl1Path}");
                conn.Open();

                // =========================================================================
                // ⭐ KIỂM TRA TRÙNG LẶP TOÀN DIỆN (CHỐNG NHẬP TRÙNG DỮ LIỆU CŨ)
                // =========================================================================

                // Lấy toàn bộ thông tin hiện tại bao gồm cả câu hỏi bảo mật để so sánh
                string oldTk = "", oldMk = "", oldToken = "", oldCh1 = "", oldTl1 = "", oldCh2 = "", oldTl2 = "";

                // Truy vấn thông tin Admin
                using (var cmd = new SqliteCommand("SELECT TenTaiKhoan, MatKhau, Token FROM Admin WHERE ID=1", conn))
                using (var rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        oldTk = BaoMatAES.GiaiMa(rd.IsDBNull(0) ? "" : rd.GetString(0));
                        oldMk = BaoMatAES.GiaiMa(rd.IsDBNull(1) ? "" : rd.GetString(1));
                        oldToken = BaoMatAES.GiaiMa(rd.IsDBNull(2) ? "" : rd.GetString(2));
                    }
                }

                // Truy vấn câu hỏi bảo mật
                using (var cmd = new SqliteCommand("SELECT ID, CauHoi, CauTraLoi FROM CauHoiBaoMat WHERE ID IN (1, 2)", conn))
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        int id = rd.GetInt32(0);
                        if (id == 1)
                        {
                            oldCh1 = BaoMatAES.GiaiMa(rd.IsDBNull(1) ? "" : rd.GetString(1));
                            oldTl1 = BaoMatAES.GiaiMa(rd.IsDBNull(2) ? "" : rd.GetString(2));
                        }
                        else
                        {
                            oldCh2 = BaoMatAES.GiaiMa(rd.IsDBNull(1) ? "" : rd.GetString(1));
                            oldTl2 = BaoMatAES.GiaiMa(rd.IsDBNull(2) ? "" : rd.GetString(2));
                        }
                    }
                }

                // KIỂM TRA XEM TẤT CẢ CÓ GIỐNG HỆT CŨ KHÔNG (Bao gồm cả ảnh nếu bạn muốn)
                // Ở đây ta so sánh các trường văn bản quan trọng nhất
                bool thongTinAdminGiongHeu = taiKhoanMoi.Equals(oldTk) && matKhauMoi == oldMk && token == oldToken;
                bool cauHoiGiongHet = cauHoi1 == oldCh1 && traLoi1 == oldTl1 && cauHoi2 == oldCh2 && traLoi2 == oldTl2;

                if (thongTinAdminGiongHeu && cauHoiGiongHet)
                {
                    MessageBox.Show("Thông tin bạn nhập giống hệt với dữ liệu đang sử dụng.\nKhông có thay đổi nào được thực hiện!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return; // Dừng lại, không thực hiện UPDATE tốn tài nguyên
                }
                // =========================================================================

                // TIẾN HÀNH LƯU (Code giữ nguyên logic lưu của bạn)
                string thoiGianHienTai = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

                // Thực hiện Update Admin
                string sqlAdmin = @"UPDATE Admin SET TenTaiKhoan=@tk, MatKhau=@mk, Token=@token, ThoiGianDangNhap=@tg WHERE ID=1";
                using var cmdAdmin = new SqliteCommand(sqlAdmin, conn);
                cmdAdmin.Parameters.AddWithValue("@tk", BaoMatAES.MaHoa(taiKhoanMoi));
                cmdAdmin.Parameters.AddWithValue("@mk", BaoMatAES.MaHoa(matKhauMoi));
                cmdAdmin.Parameters.AddWithValue("@token", BaoMatAES.MaHoa(token));
                cmdAdmin.Parameters.AddWithValue("@tg", thoiGianHienTai);
                cmdAdmin.ExecuteNonQuery();

                // Thực hiện Update Câu hỏi 1 & 2
                string sqlCH = @"UPDATE CauHoiBaoMat SET CauHoi=@ch, CauTraLoi=@tl, ThoiGian=@tg WHERE ID=@id";

                using (var cmdCH1 = new SqliteCommand(sqlCH, conn))
                {
                    cmdCH1.Parameters.AddWithValue("@ch", BaoMatAES.MaHoa(cauHoi1));
                    cmdCH1.Parameters.AddWithValue("@tl", BaoMatAES.MaHoa(traLoi1));
                    cmdCH1.Parameters.AddWithValue("@tg", thoiGianHienTai);
                    cmdCH1.Parameters.AddWithValue("@id", 1);
                    cmdCH1.ExecuteNonQuery();
                }

                using (var cmdCH2 = new SqliteCommand(sqlCH, conn))
                {
                    cmdCH2.Parameters.AddWithValue("@ch", BaoMatAES.MaHoa(cauHoi2));
                    cmdCH2.Parameters.AddWithValue("@tl", BaoMatAES.MaHoa(traLoi2));
                    cmdCH2.Parameters.AddWithValue("@tg", thoiGianHienTai);
                    cmdCH2.Parameters.AddWithValue("@id", 2);
                    cmdCH2.ExecuteNonQuery();
                }

                CapPhatTokenCSDL();

                // Cập nhật Session và UI
                SessionInfo.TenTaiKhoan = taiKhoanMoi;
                SessionInfo.ThoiGianDangNhap = DateTime.Now;

                var formTrangChu = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
                formTrangChu?.KhoiTaoDuLieuNguoiDung();

                LuuAnhDaiDienVaoCSDL();
                // Đặt dòng này tại Form3_DangKyTaiKhoan lúc lưu thành công tài khoản/ảnh:
                // ⭐ XÓA CACHE TOÀN CỤC SAU KHI LƯU ẢNH THÀNH CÔNG
                Module_DanduongGPS.XoaCacheAvatarToanCuc();
                MessageBox.Show("Cập nhật thông tin tài khoản thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btn_Thoat_Click(object? sender, EventArgs e)
        {
            this.Close();
        }
        private void CapPhatTokenCSDL()
        {
            string tokenNguoiDung = text_Token.Text.Trim();
            string taiKhoanNguoiDung = text_TaiKhoanMoi.Text.Trim();
            string thoiGianHienTai = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

            if (string.IsNullOrWhiteSpace(tokenNguoiDung))
            {
                string duongDan1 = _csdl1Path;
                if (!string.IsNullOrWhiteSpace(duongDan1))
                {
                    try
                    {
                        using var conn = new SqliteConnection($"Data Source={duongDan1}");
                        conn.Open();

                        using var cmd = new SqliteCommand("SELECT Token, TenTaiKhoan FROM Admin WHERE ID=1", conn);
                        using var rd = cmd.ExecuteReader();
                        if (rd.Read())
                        {
                            string rawToken = rd.GetString(0);
                            string rawTaiKhoan = rd.GetString(1);

                            tokenNguoiDung = BaoMatAES.GiaiMa(rawToken);
                            if (string.IsNullOrWhiteSpace(tokenNguoiDung)) tokenNguoiDung = rawToken;

                            taiKhoanNguoiDung = BaoMatAES.GiaiMa(rawTaiKhoan);
                            if (string.IsNullOrWhiteSpace(taiKhoanNguoiDung)) taiKhoanNguoiDung = rawTaiKhoan;
                        }
                        else
                        {
                            tokenNguoiDung = Guid.NewGuid().ToString("N");
                            if (string.IsNullOrWhiteSpace(taiKhoanNguoiDung)) taiKhoanNguoiDung = "Admin";
                        }
                    }
                    catch
                    {
                        tokenNguoiDung = Guid.NewGuid().ToString("N");
                        if (string.IsNullOrWhiteSpace(taiKhoanNguoiDung)) taiKhoanNguoiDung = "Admin";
                    }
                }
            }

            string[] csdls = new string[] { _csdl1Path, _csdl2Path, _csdl3Path, _csdl4Path };

            foreach (var duongDan in csdls)
            {
                if (string.IsNullOrWhiteSpace(duongDan)) continue;

                try
                {
                    using var conn = new SqliteConnection($"Data Source={duongDan}");
                    conn.Open();

                    string sqlCreate = @"CREATE TABLE IF NOT EXISTS Token_XacDinhChinhChu (
                                    ID INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                    STT TEXT, Ma_ToKen TEXT, Tai_Khoan_Cap_Nhat TEXT, Thoi_Gian_Nap TEXT);";
                    using var cmdCreate = new SqliteCommand(sqlCreate, conn);
                    cmdCreate.ExecuteNonQuery();

                    string sqlCheck = "SELECT COUNT(1) FROM Token_XacDinhChinhChu WHERE ID=1";
                    using var cmdCheck = new SqliteCommand(sqlCheck, conn);
                    long count = (long)cmdCheck.ExecuteScalar();

                    if (count == 0)
                    {
                        string sqlInsert = @"INSERT INTO Token_XacDinhChinhChu (ID, STT, Ma_ToKen, Tai_Khoan_Cap_Nhat, Thoi_Gian_Nap)
                                     VALUES (1, @stt, @token, @tk, @tg)";
                        using var cmdInsert = new SqliteCommand(sqlInsert, conn);
                        cmdInsert.Parameters.AddWithValue("@stt", "1");
                        cmdInsert.Parameters.AddWithValue("@token", BaoMatAES.MaHoa(tokenNguoiDung));
                        cmdInsert.Parameters.AddWithValue("@tk", BaoMatAES.MaHoa(taiKhoanNguoiDung));
                        cmdInsert.Parameters.AddWithValue("@tg", thoiGianHienTai);
                        cmdInsert.ExecuteNonQuery();
                    }
                    else
                    {
                        string sqlUpdate = @"UPDATE Token_XacDinhChinhChu
                                     SET STT=@stt, Ma_ToKen=@token, Tai_Khoan_Cap_Nhat=@tk, Thoi_Gian_Nap=@tg WHERE ID=1";
                        using var cmdUpdate = new SqliteCommand(sqlUpdate, conn);
                        cmdUpdate.Parameters.AddWithValue("@stt", "1");
                        cmdUpdate.Parameters.AddWithValue("@token", BaoMatAES.MaHoa(tokenNguoiDung));
                        cmdUpdate.Parameters.AddWithValue("@tk", BaoMatAES.MaHoa(taiKhoanNguoiDung));
                        cmdUpdate.Parameters.AddWithValue("@tg", thoiGianHienTai);
                        cmdUpdate.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi cấp phát Token cho CSDL: {duongDan}\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private byte[] TaoThumbnail(Image imgOriginal, int maxW, int maxH)
        {
            int newW = imgOriginal.Width, newH = imgOriginal.Height;
            double ratio = Math.Min((double)maxW / imgOriginal.Width, (double)maxH / imgOriginal.Height);
            if (ratio < 1.0) { newW = (int)(imgOriginal.Width * ratio); newH = (int)(imgOriginal.Height * ratio); }

            using var bmp = new Bitmap(newW, newH);
            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.Clear(Color.White);
                g.DrawImage(imgOriginal, 0, 0, newW, newH);
            }
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            return ms.ToArray();
        }
        private void kryptonButton1_ThemAnhDaiDien_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Chọn ảnh đại diện",
                Filter = "Image Files (*.jpg; *.jpeg; *.png)|*.jpg;*.jpeg;*.png"
            };

            if (ofd.ShowDialog() != DialogResult.OK) return;

            try
            {
                var fileInfo = new FileInfo(ofd.FileName);

                if (fileInfo.Length == 0 || fileInfo.Length > MAX_IMAGE_SIZE)
                    throw new Exception($"File không hợp lệ hoặc vượt quá dung lượng ({MAX_IMAGE_SIZE / (1024 * 1024)}MB).");

                byte[] bytes = File.ReadAllBytes(ofd.FileName);

                using var ms = new MemoryStream(bytes, writable: false);
                using var img = Image.FromStream(ms, false, true);

                if (img.Width > MAX_WIDTH || img.Height > MAX_HEIGHT)
                    throw new Exception($"Độ phân giải ảnh quá lớn (Tối đa {MAX_WIDTH}x{MAX_HEIGHT}px).");

                _hinhAnhDaiDienBytes = bytes;
                _thumbnailBytes = TaoThumbnail(img, 256, 256);

                using (var thumbMs = new MemoryStream(_thumbnailBytes, writable: false))
                using (var thumbImg = Image.FromStream(thumbMs))
                {
                    var oldImage = pictureBox2_AnhDaiDienAdmin.Image;
                    pictureBox2_AnhDaiDienAdmin.Image = new Bitmap(thumbImg);
                    pictureBox2_AnhDaiDienAdmin.SizeMode = PictureBoxSizeMode.StretchImage;
                    oldImage?.Dispose();
                }

                kryptonButton1_ThemAnhDaiDien.Text = "Đổi ảnh";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ảnh không hợp lệ hoặc bị lỗi!\n\nChi tiết: " + ex.Message, "Lỗi Dữ Liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void LuuAnhDaiDienVaoCSDL()
        {
            if (_hinhAnhDaiDienBytes == null && pictureBox2_AnhDaiDienAdmin.Image != null)
            {
                try
                {
                    Image imgTrenForm = pictureBox2_AnhDaiDienAdmin.Image;
                    using (var bmp = new Bitmap(imgTrenForm))
                    {
                        using (var ms = new MemoryStream())
                        {
                            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                            _hinhAnhDaiDienBytes = ms.ToArray();
                        }
                    }
                    _thumbnailBytes = TaoThumbnail(imgTrenForm, 256, 256);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi trích xuất ảnh từ Form: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (_hinhAnhDaiDienBytes == null) return;

            try
            {
                using var conn = new SqliteConnection($"Data Source={_csdl1Path}");
                conn.Open();

                // ⭐ Đã xóa cột ThoiGianCapNhat và tham số @tg cho khớp với Database
                string sqlUpsert = @"
            INSERT OR REPLACE INTO AvatarAdmin (ID, DuLieuAnh, ThumbnailAnh) 
            VALUES (1, @anh, @thumb);";

                using var cmd = new SqliteCommand(sqlUpsert, conn);
                // Ép kiểu Blob rõ ràng để tránh lỗi định dạng
                cmd.Parameters.Add("@anh", SqliteType.Blob).Value = _hinhAnhDaiDienBytes;
                cmd.Parameters.Add("@thumb", SqliteType.Blob).Value = _thumbnailBytes ?? (object)DBNull.Value;

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // ⭐ Báo lỗi thẳng ra màn hình thay vì giấu trong Debug
                MessageBox.Show("Lỗi ghi ảnh vào CSDL: " + ex.Message, "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PictureBox1_Click(object sender, EventArgs e)
        {
            string tieuDe = "Thông tin Token nội bộ";
            string noiDung = "Token nội bộ là mã dùng để nhận diện các CSDL hợp lệ trong hệ thống.\n\n" +
                             "Token được mã hóa và cấp phát cho từng CSDL nhằm xác thực tính toàn vẹn và xác định CSDL chính chủ sau khi đăng ký.\n\n" +
                             "Người dùng không cần ghi nhớ hay cấu hình thủ công. Nếu Token giữa các CSDL không trùng khớp, hệ thống sẽ tự động phát hiện, khôi phục về bản gốc và đảm bảo an toàn, bảo mật dữ liệu.";

            HienThiFormAo_ThongTin(tieuDe, noiDung);
        }

        /// <summary>
        /// Dựng Form động kế thừa FormAoBase (Anti-Flicker tuyệt đối).
        /// Chuyên hiển thị các văn bản hướng dẫn, giải thích dài.
        /// Thiết kế TextBox tàng hình hỗ trợ bôi đen copy, tự động giải phóng RAM khi đóng.
        /// </summary>
        private void HienThiFormAo_ThongTin(string tieuDe, string noiDung)
        {
            // Chuyển \n thành \r\n để đảm bảo xuống dòng mượt mà trên TextBox
            string noiDungChuan = noiDung.Replace("\n", Environment.NewLine);

            // ⭐ SỬ DỤNG CLASS FORMAOBASE
            using (var formAo = new FormAoBase())
            {
                formAo.Text = "Hệ thống thông báo";
                formAo.Size = new System.Drawing.Size(900, 450); // Nới cao lên một chút cho thoáng chữ
                formAo.FormBorderStyle = FormBorderStyle.FixedDialog;
                formAo.MaximizeBox = false;
                formAo.MinimizeBox = false;
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

                // --- 4. NỘI DUNG VĂN BẢN (TextBox tàng hình hỗ trợ bôi đen copy) ---
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
                txtContent.StateCommon.Content.Color1 = System.Drawing.Color.FromArgb(45, 45, 45); // Xám đậm chống mỏi mắt
                txtContent.StateCommon.Content.Padding = new Padding(0);

                // --- 5. PANEL CHỨA NÚT BẤM ---
                var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = System.Drawing.Color.WhiteSmoke };

                var btnClose = new Krypton.Toolkit.KryptonButton
                {
                    Text = "Đã hiểu",
                    Width = 120,
                    Height = 36,
                    DialogResult = DialogResult.OK
                };
                btnClose.StateCommon.Content.ShortText.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
                btnClose.StateCommon.Border.Rounding = 5;

                // Căn giữa nút Đóng
                btnClose.Location = new System.Drawing.Point((formAo.Width - btnClose.Width) / 2, 12);
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

                // Ngăn TextBox bị bôi đen toàn bộ text khi form vừa load lên
                formAo.Shown += (s, ev) => btnClose.Focus();

                formAo.ShowDialog(this);
            }
        }

        // Đặt 3 dòng này ở phía trên cùng của class Form3_DangKyTaiKhoan
        // ==========================================================
        // FONT DÙNG CHUNG
        // ==========================================================

        private static readonly Font TitleFont = new Font("Segoe UI", 12F, FontStyle.Bold);
        private static readonly Font ContentFont = new Font("Segoe UI", 10.5F, FontStyle.Regular);
        private static readonly Font ButtonFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        private void pictureBox2_ThongTin_DKTK_Click(object sender, EventArgs e)
        {
            const string tieuDe = "         Hướng dẫn";
            const string noiDung = @"
1. Ý nghĩa các trường thông tin:
- Tài khoản: Tên định danh duy nhất giúp hệ thống xác thực người dùng.
(Việc tạo tài khoản mới sẽ tự động xóa tài khoản đang dùng)
- Mật khẩu: Chìa khóa bảo vệ quyền truy cập dữ liệu của bạn.
- Token: Mã nhận diện nội bộ nhằm đảm bảo an toàn và tính toàn vẹn cho CSDL.

2. Hướng dẫn đặt mật khẩu mạnh:
- Độ dài: Tối thiểu 8 ký tự.
- Độ phức tạp: Nên kết hợp chữ hoa, chữ thường, chữ số và ký tự đặc biệt.

3. Cách chọn câu hỏi bảo mật:
- Tính bảo mật: Chọn câu hỏi mà câu trả lời chỉ mình bạn biết.
- Ghi nhớ: Hãy ghi nhớ chính xác đáp án (phân biệt hoa/thường).

Lưu ý: Thông tin bạn nhập đều được mã hóa bằng thuật toán
(V2) AES-256-CBC + Random IV trước khi lưu vào cơ sở dữ liệu.";

            HienThiFormAo_ThongTin_DKTK(tieuDe, noiDung);
        }
        private void HienThiFormAo_ThongTin_DKTK(string tieuDe, string noiDung)
        {
            if (string.IsNullOrWhiteSpace(noiDung)) noiDung = "Không có nội dung.";

            using (var formAo = new FormAoBase())
            {
                // 1. CẤU HÌNH FORM BỀN BỈ (Khóa phóng to)
                formAo.Text = "Hệ thống thông báo";
                formAo.Size = new Size(880, 640);
                formAo.StartPosition = FormStartPosition.CenterParent;
                formAo.FormBorderStyle = FormBorderStyle.FixedDialog; // Khóa resize
                formAo.MaximizeBox = false;
                formAo.MinimizeBox = false;
                formAo.ShowIcon = false;
                formAo.ShowInTaskbar = false;

                // --- 2. PANEL TIÊU ĐỀ CÓ THÊM ICON ---
                var panelTop = new KryptonPanel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(25, 15, 20, 5) };
                panelTop.StateCommon.Color1 = Color.White;

                // TẠO ICON CHUẨN
                var picIcon = new PictureBox
                {
                    Image = SystemIcons.Information.ToBitmap(), // Biểu tượng Information mặc định của Windows
                    SizeMode = PictureBoxSizeMode.CenterImage,
                    Width = 40, // Dành 40px không gian cho Icon
                    Dock = DockStyle.Left
                };

                var lblTitle = new KryptonLabel { Text = tieuDe.ToUpperInvariant(), Dock = DockStyle.Fill, AutoSize = false };
                lblTitle.StateCommon.ShortText.Font = TitleFont;
                lblTitle.StateCommon.ShortText.Color1 = Color.FromArgb(0, 82, 155);
                lblTitle.Padding = new Padding(10, 5, 0, 0); // Khoảng cách 10px giữa Icon và Tiêu đề

                // Add vào PanelTop (Lưu ý: phải BringToFront để Icon ép sát mép trái)
                panelTop.Controls.Add(lblTitle);
                panelTop.Controls.Add(picIcon);
                picIcon.BringToFront();

                // --- 3. PANEL NỘI DUNG ---
                var panelContent = new KryptonPanel { Dock = DockStyle.Fill, Padding = new Padding(25, 10, 25, 20) };
                panelContent.StateCommon.Color1 = Color.White;

                var rtb = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    BorderStyle = BorderStyle.None,
                    BackColor = Color.White,
                    ScrollBars = RichTextBoxScrollBars.Vertical
                };

                // Thuật toán đổ dữ liệu và tô màu
                var lines = noiDung.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    rtb.SelectionStart = rtb.TextLength;

                    if (line.StartsWith("1.") || line.StartsWith("2.") || line.StartsWith("3."))
                    {
                        rtb.SelectionColor = Color.FromArgb(0, 82, 155); // Xanh Navy
                        rtb.SelectionFont = TitleFont;
                    }
                    else if (line.Contains("Lưu ý:"))
                    {
                        rtb.SelectionColor = Color.FromArgb(200, 0, 0); // Đỏ cảnh báo
                        rtb.SelectionFont = TitleFont;
                    }
                    else
                    {
                        rtb.SelectionColor = Color.FromArgb(45, 45, 45); // Xám đậm
                        rtb.SelectionFont = ContentFont;
                    }
                    rtb.AppendText(line + Environment.NewLine);
                }

                panelContent.Controls.Add(rtb);
                rtb.BringToFront();

                // --- 4. PANEL NÚT BẤM ---
                var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = Color.WhiteSmoke };

                var btnClose = new KryptonButton
                {
                    Text = "Đã hiểu",
                    Width = 150,
                    Height = 45,
                    DialogResult = DialogResult.OK
                };
                btnClose.StateCommon.Content.ShortText.Font = ButtonFont;
                btnClose.StateCommon.Border.Rounding = 18;
                btnClose.Cursor = Cursors.Hand; // Thêm con trỏ chuột bàn tay cho đẹp

                // Căn giữa nút
                panelBottom.Resize += (s, e) =>
                {
                    btnClose.Location = new Point((panelBottom.Width - btnClose.Width) / 2, 12);
                };
                btnClose.Location = new Point((formAo.Width - btnClose.Width) / 2, 12);

                panelBottom.Controls.Add(btnClose);

                // --- 5. RÁP VÀO FORM ---
                formAo.Controls.Add(panelContent);
                formAo.Controls.Add(panelTop);
                formAo.Controls.Add(panelBottom);

                formAo.AcceptButton = btnClose;
                formAo.CancelButton = btnClose;

                formAo.Shown += (s, e) => btnClose.Focus();

                formAo.ShowDialog(this);
            }
        }

    }//Mèo cam ----- Ngoài luồng
}