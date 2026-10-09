
using System.Diagnostics;
using System.Globalization;

namespace PhanMemThiDua2026
{
    public partial class Form11_KiemTraTyLe : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        public string CheDoXet { get; private set; }
        private static readonly string[] GoiYPhanLoai = { Module_HeThong.Loai_1, Module_HeThong.Loai_2, Module_HeThong.Loai_3, Module_HeThong.Loai_4 };

        // 🚀 BỘ NHỚ ĐỆM (CACHE): Lưu sẵn tỷ lệ của cả 4 loại, không cần query DB nhiều lần
        private Dictionary<string, double[]> _cacheTyLe = new Dictionary<string, double[]>();
        private bool _daTaiXong = false;
        private bool _toolTipInited = false;

        // Constructor nhận biến truyền vào khi khởi tạo
        public Form11_KiemTraTyLe(string cheDo)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cheDo);
            InitializeComponent();
            Module_HeThong.BocBoTronGiaoDien(ListBox2);
            CheDoXet = cheDo;
            ConfigureForm();
            WireEvents();
            // 4. 🎯 CHỈ CẦN SỰ KIỆN SHOWN ĐỂ FOCUS VÀ BÔI ĐEN
            this.Shown += (s, e) =>
            {
                text_Texttongquanso.Focus();
            };
        }
        //private void Form11_KiemTraTyLe_Shown(object? sender, EventArgs e)
        //{
        //    // Đặt con trỏ vào ô nhập quân số và bôi đen toàn bộ văn bản ngay khi Form hiện lên
        //    text_Texttongquanso.Focus();
        //}
        private void ConfigureForm()
        {
            // 2. Cài đặt thuộc tính hiển thị Form
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            AcceptButton = btn_TextTinh;
            //CancelButton = btn_TextTinh; // nếu có
        }


        private void WireEvents()
        {
            text_Texttongquanso.GotFocus -= TextTongQuanSo_GotFocus;
            text_Texttongquanso.GotFocus += TextTongQuanSo_GotFocus;

            text_Texttongquanso.KeyPress -= TextTongQuanSo_KeyPress;
            text_Texttongquanso.KeyPress += TextTongQuanSo_KeyPress;

            text_Texttongquanso.TextChanged -= TextTongQuanSo_TextChanged;
            text_Texttongquanso.TextChanged += TextTongQuanSo_TextChanged;
        }


        private void TextTongQuanSo_KeyPress(object? sender, KeyPressEventArgs e)
        {
            // Cho phép phím điều khiển: Backspace, Ctrl+A, Ctrl+C...
            if (char.IsControl(e.KeyChar))
                return;

            // Chỉ cho phép chữ số ASCII từ 0 đến 9
            if (e.KeyChar < '0' || e.KeyChar > '9')
                e.Handled = true;
        }

        private void TextTongQuanSo_TextChanged(object? sender, EventArgs e)
        {
            if (sender is not Krypton.Toolkit.KryptonTextBox textBox)
                return;

            string text = textBox.Text;
            if (text.Length == 0)
                return;

            // Chặn ký tự không phải số khi dán văn bản hoặc thay đổi nội dung
            string textHopLe = string.Concat(text.Where(c => c >= '0' && c <= '9'));

            if (!string.Equals(text, textHopLe, StringComparison.Ordinal))
            {
                int viTriConTro = textBox.SelectionStart;

                textBox.TextChanged -= TextTongQuanSo_TextChanged;
                textBox.Text = textHopLe;
                textBox.SelectionStart = Math.Min(viTriConTro, textBox.TextLength);
                textBox.TextChanged += TextTongQuanSo_TextChanged;
            }
        }
        private void TextTongQuanSo_GotFocus(object? sender, EventArgs e)
        {
            if (sender is not Krypton.Toolkit.KryptonTextBox ktb) return;
            ktb.BeginInvoke(() => ktb.SelectAll());
        }
        // Hàm cập nhật biến và tải lại dữ liệu nếu Form11 đang mở sẵn hoặc gọi từ bên ngoài
        public void CapNhatCheDoXet(string cheDo)
        {
            CheDoXet = cheDo;

            // Nếu Form đã load xong giao diện thì nạp lại Cache & cập nhật ComboBox khi chế độ xét thay đổi
            if (_daTaiXong)
            {
                if (InvokeRequired) { Invoke(() => CapNhatCheDoXet(cheDo)); return; }
                Com_textphanloai_SelectedIndexChanged(null, EventArgs.Empty);
            }
        }

        private void Form11_Load(object? sender, EventArgs e)
        {
            ListBox2.Font = new Font(ListBox2.Font.FontFamily, 10f, FontStyle.Regular);
            // 🟢 CÀI ĐẶT MÀU SẮC CUSTOM CHO LISTBOX2
            ListBox2.DrawMode = DrawMode.OwnerDrawFixed;
            ListBox2.DrawItem -= ListBox2_DrawItem; // Tránh trùng lặp sự kiện
            ListBox2.DrawItem += ListBox2_DrawItem;
            InitToolTips();

            // Nạp dữ liệu ComboBox trước
            com_Textphanloai.Items.Clear();
            com_Textphanloai.Items.AddRange(GoiYPhanLoai);
            com_Textphanloai.SelectedIndexChanged -= Com_textphanloai_SelectedIndexChanged;
            com_Textphanloai.SelectedIndexChanged += Com_textphanloai_SelectedIndexChanged;

            // Đọc DB 1 lần duy nhất vào Cache khi Load Form
            TaiDuLieuTuSQLiteVaoCache();

            _daTaiXong = true;

            // Tự động chọn Loại 2 làm mặc định (kích hoạt sự kiện SelectedIndexChanged)
            if (com_Textphanloai.Items.Count > 1)
                com_Textphanloai.SelectedIndex = 1;

            if (string.IsNullOrWhiteSpace(text_Texttongquanso.Text))
            {
                ListBox2.Items.Add($"⚠️ {Module_HeThong.Tu_Dong_Chi} hãy nhập Tổng quân số!");
                ListBox2.Items.Add("Để thực hiện phép tính số lượng đạt tỷ lệ %");
            }
          //  text_Texttongquanso.Focus();
        }

        // Màu chọn dùng chung, tránh tạo mới ở mỗi lần vẽ
        // 🎨 Bảng màu dùng chung (tạo một lần, không cấp phát lại mỗi lần vẽ)
        // 🟢 Nền xanh lá nhạt nhất (chuẩn theo ảnh bạn gửi)
        // 🎨 Bảng màu đồng bộ với Module_ThongBao
        private static readonly Color SelectedBackColor = Color.FromArgb(220, 245, 225); // nền xanh nhạt khi chọn
        private static readonly Color SelectedForeColor = Color.FromArgb(35, 120, 65);   // chữ xanh đậm khi chọn
        private static readonly Color NormalForeColor = Color.FromArgb(27, 94, 32);    // chữ xanh tối khi thường

        private const int TextPaddingLeft = 8; // lề trái cho thoáng

        private const TextFormatFlags ItemTextFlags =
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix |
            TextFormatFlags.SingleLine;

        private void ListBox2_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not ListBox lb || e.Index < 0 || e.Index >= lb.Items.Count)
                return;

            bool dangChon = (e.State & DrawItemState.Selected) != 0;
            bool voHieu = (e.State & DrawItemState.Disabled) != 0;

            Color mauNen = dangChon ? SelectedBackColor : lb.BackColor;
            Color mauChu = voHieu ? SystemColors.GrayText
                         : dangChon ? SelectedForeColor
                         : NormalForeColor;

            // Nền
            using (var brushNen = new SolidBrush(mauNen))
                e.Graphics.FillRectangle(brushNen, e.Bounds);

            // Chữ
            string text = lb.GetItemText(lb.Items[e.Index]);
            var vung = new Rectangle(
                e.Bounds.X + TextPaddingLeft, e.Bounds.Y,
                e.Bounds.Width - TextPaddingLeft, e.Bounds.Height);

            TextRenderer.DrawText(e.Graphics, text, e.Font ?? lb.Font, vung, mauChu, ItemTextFlags);

            // Không gọi e.DrawFocusRectangle() → giao diện phẳng, không có viền chấm
        }

        private void InitToolTips()
        {
            if (_toolTipInited) return;
            _toolTipInited = true;
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            if (btn_TextTinh != null) toolTip1.SetToolTip(btn_TextTinh, "Nhấn Enter hoặc Click để tính toán");
            if (com_Textphanloai != null) toolTip1.SetToolTip(com_Textphanloai, "Chọn phân loại tập thể");
        }

        // KHỐI 1: XỬ LÝ DATABASE & CACHE (HIỆU SUẤT) - HỖ TRỢ ĐA BẢNG (TÂN BINH / XÉT NĂM / THÁNG)
        private void TaiDuLieuTuSQLiteVaoCache()
        {
            _cacheTyLe.Clear();

            // 1. KHỞI TẠO CACHE
            foreach (string loai in GoiYPhanLoai)
            {
                _cacheTyLe[loai] = new double[3];
            }

            // 2. KIỂM TRA CSDL
            if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path))
            {
                MessageBox.Show(
                    "Không tìm thấy tệp cơ sở dữ liệu hệ thống!",
                    "Cảnh báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 3. XÁC ĐỊNH PHIÊN BẢN (TÂN BINH)
                bool laTanBinh = false;
                try
                {
                    string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? string.Empty;
                    laTanBinh = phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Form11] Không xác định được phiên bản: {ex.Message}");
                }

                using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_csdl2Path};Mode=ReadOnly;");
                conn.Open();
                using var cmd = conn.CreateCommand();

                // 4. PHÂN NHÁNH XỬ LÝ THEO CHẾ ĐỘ XÉT
                bool isNam = string.Equals(CheDoXet?.Trim(), "Năm", StringComparison.OrdinalIgnoreCase);

                if (isNam && !laTanBinh)
                {
                    // 📌 CHẾ ĐỘ XÉT NĂM: Đọc từ bảng QuyDinhTyLe_XetThiDuaNam (Chỉ có 1 dòng ID = 1)
                    cmd.CommandText = """
                SELECT TyLe_CSTD, TyLe_CSTT, TyLe_HTNV 
                FROM [QuyDinhTyLe_XetThiDuaNam] 
                WHERE ID = 1;
            """;

                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        double cstd = ChuanHoaSoThuan(reader.IsDBNull(0) ? null : reader.GetString(0)); // Loại 1
                        double cstt = ChuanHoaSoThuan(reader.IsDBNull(1) ? null : reader.GetString(1)); // Loại 2
                        double htnv = ChuanHoaSoThuan(reader.IsDBNull(2) ? null : reader.GetString(2)); // Loại 3

                        // Chế độ Năm gán chung bộ tỷ lệ này cho cả 4 phân loại tập thể trong Cache
                        foreach (string loai in GoiYPhanLoai)
                        {
                            _cacheTyLe[loai][0] = cstd;
                            _cacheTyLe[loai][1] = cstt;
                            _cacheTyLe[loai][2] = htnv;
                        }
                    }
                }
                else
                {
                    // 📌 CHẾ ĐỘ XÉT THÁNG / TÂN BINH: Giữ nguyên logic gốc của bạn
                    string tableQuyDinh = laTanBinh ? "QuyDinhTyLe_TanBinh" : "QuyDinhTyLe";

                    cmd.CommandText = $"""
                SELECT
                    ID,
                    {Module_HeThong.COL_LOAI_1},
                    {Module_HeThong.COL_LOAI_2},
                    {Module_HeThong.COL_LOAI_3},
                    {Module_HeThong.COL_LOAI_4}
                FROM [{tableQuyDinh}]
                WHERE ID BETWEEN 1 AND 3;
            """;

                    using var reader = cmd.ExecuteReader();
                    int ordinalId = reader.GetOrdinal("ID");
                    int ordinalLoai1 = reader.GetOrdinal(Module_HeThong.COL_LOAI_1);
                    int ordinalLoai2 = reader.GetOrdinal(Module_HeThong.COL_LOAI_2);
                    int ordinalLoai3 = reader.GetOrdinal(Module_HeThong.COL_LOAI_3);
                    int ordinalLoai4 = reader.GetOrdinal(Module_HeThong.COL_LOAI_4);

                    while (reader.Read())
                    {
                        int id = reader.GetInt32(ordinalId);
                        if (id < 1 || id > 3) continue;
                        int index = id - 1;

                        _cacheTyLe[Module_HeThong.Loai_1][index] = GiaiMaVaChuanHoa(reader.IsDBNull(ordinalLoai1) ? null : reader.GetString(ordinalLoai1));
                        _cacheTyLe[Module_HeThong.Loai_2][index] = GiaiMaVaChuanHoa(reader.IsDBNull(ordinalLoai2) ? null : reader.GetString(ordinalLoai2));
                        _cacheTyLe[Module_HeThong.Loai_3][index] = GiaiMaVaChuanHoa(reader.IsDBNull(ordinalLoai3) ? null : reader.GetString(ordinalLoai3));
                        _cacheTyLe[Module_HeThong.Loai_4][index] = GiaiMaVaChuanHoa(reader.IsDBNull(ordinalLoai4) ? null : reader.GetString(ordinalLoai4));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Lỗi nạp CSDL Form 11]: {ex}");
                MessageBox.Show(
                    $"Lỗi nạp CSDL Form 11:\n\n{ex.Message}",
                    "Lỗi Debug",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Đọc giá trị thuần túy (không qua giải mã AES) dành cho bảng Xét Nam
        private double ChuanHoaSoThuan(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue)) return 0;
            string cleanValue = rawValue.Trim().Replace(",", ".");
            double.TryParse(cleanValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double giaTri);

            if (giaTri < 0) return 0;
            if (giaTri > 100) return 100;
            return giaTri;
        }

        // Đọc giá trị có giải mã AES dành cho bảng Tháng/Tân Binh
        private double GiaiMaVaChuanHoa(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue)) return 0;
            string decryptedValue = Module_BaoMatAES.GiaiMa(rawValue).Trim();
            if (string.IsNullOrWhiteSpace(decryptedValue))
                decryptedValue = rawValue.Trim();

            decryptedValue = decryptedValue.Replace(",", ".");
            double.TryParse(decryptedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double giaTri);

            if (giaTri < 0) return 0;
            if (giaTri > 100) return 100;
            return giaTri;
        }

        // KHỐI 2: TƯƠNG TÁC GIAO DIỆN & LOGIC UI
        private void Com_textphanloai_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!_daTaiXong) return;
            string loaiDaChon = com_Textphanloai.SelectedItem?.ToString() ?? "";

            // Lấy dữ liệu siêu tốc từ RAM (Cache)
            if (_cacheTyLe.TryGetValue(loaiDaChon, out double[]? tyLe) && tyLe != null)
            {
                text_Textloai1.Text = tyLe[0].ToString(CultureInfo.InvariantCulture);
                text_Textloai2.Text = tyLe[1].ToString(CultureInfo.InvariantCulture);
                text_Textloai3.Text = tyLe[2].ToString(CultureInfo.InvariantCulture);
            }
        }

        // ⚡ Hàm tính toán bất đồng bộ
        private async void btn_texttinh_Click(object? sender, EventArgs e)
        {
            // BƯỚC 1: KIỂM TRA DỮ LIỆU NGAY LẬP TỨC (KHÔNG CHẠY TIẾN TRÌNH NẾU LỖI)
            string strTongQS = text_Texttongquanso.Text.Replace(",", ".");
            if (!double.TryParse(strTongQS, NumberStyles.Any, CultureInfo.InvariantCulture, out double tongQS) || tongQS <= 1 || tongQS > 1000000)
            {
                ListBox2.Items.Clear();
                System.Media.SystemSounds.Exclamation.Play();
                ListBox2.Items.Add($"⚠️ {Module_HeThong.Tu_Dong_Chi} hãy nhập Tổng quân số hợp lệ (Từ 2 đến 1.000.000)!");
                text_Texttongquanso.Focus();
                return;
            }

            string strLoai1 = text_Textloai1.Text.Replace(",", ".");
            string strLoai2 = text_Textloai2.Text.Replace(",", ".");
            string strLoai3 = text_Textloai3.Text.Replace(",", ".");

            if (!double.TryParse(strLoai1, NumberStyles.Any, CultureInfo.InvariantCulture, out double pLoai1) ||
                !double.TryParse(strLoai2, NumberStyles.Any, CultureInfo.InvariantCulture, out double pLoai2) ||
                !double.TryParse(strLoai3, NumberStyles.Any, CultureInfo.InvariantCulture, out double pLoai3))
            {
                ListBox2.Items.Clear();
                ListBox2.Items.Add("⚠️️ Dữ liệu tỷ lệ từ CSDL không hợp lệ!");
                return;
            }

            // BƯỚC 2: DỮ LIỆU ĐÃ CHUẨN -> KHÓA UI VÀ CHẠY THANH TIẾN ĐỘ
            try
            {
                btn_TextTinh.Enabled = false;
                ListBox2.Items.Clear();
                tienDo_kryptonProgressBar1.Minimum = 0;
                tienDo_kryptonProgressBar1.Maximum = 100;
                tienDo_kryptonProgressBar1.Value = 0;
                tienDo_kryptonProgressBar1.Visible = true;

                for (int i = 0; i <= 100; i += 4)
                {
                    tienDo_kryptonProgressBar1.Value = i;
                    tienDo_kryptonProgressBar1.Text = $"{i} %";
                    btn_TextTinh.Text = $"Đang xử lý... {i}%";
                    await Task.Delay(15);
                }

                // BƯỚC 3: THỰC THI TÍNH TOÁN VÀ TRUYỀN SỐ LIỆU ĐÃ KIỂM TRA VÀO
                ThucThiTinhToan(tongQS, pLoai1, pLoai2, pLoai3);
                System.Media.SystemSounds.Beep.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Có lỗi xảy ra: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btn_TextTinh.Text = "TÍNH KẾT QUẢ";
                btn_TextTinh.Enabled = true;
                tienDo_kryptonProgressBar1.Text = "Hoàn thành";
                tienDo_kryptonProgressBar1.Value = 0;
                tienDo_kryptonProgressBar1.Visible = false;
                text_Texttongquanso.Focus();
            }
        }

        // KHỐI 3: THUẬT TOÁN TÍNH TỶ LỆ CHUẨN XÁC
        private void ThucThiTinhToan(double tongQS, double pLoai1, double pLoai2, double pLoai3)
        {
            // Chuyển % sang hệ số thập phân
            pLoai1 /= 100.0; pLoai2 /= 100.0; pLoai3 /= 100.0;

            // 1. Ép phần nguyên dưới để Loại 2 không vượt trần % quy định
            int qLoai2 = (int)Math.Floor(tongQS * pLoai2);

            // 2. ⚖️ CÂN BẰNG QUÂN SỐ: Loại 3 bắt buộc gánh toàn bộ phần dư để tổng L2 + L3 = Tổng QS
            int qLoai3 = (int)tongQS - qLoai2;

            // 3. Loại 1 là tập con của Loại 2
            int qLoai1 = (int)Math.Floor(qLoai2 * pLoai1);
            if (qLoai1 > qLoai2) qLoai1 = qLoai2; // Rào chắn an toàn

            // TÍNH TOÁN LẠI TỶ LỆ THỰC TẾ ĐẠT ĐƯỢC SAU LÀM TRÒN
            double tlLoai1Thuc = qLoai2 == 0 ? 0 : Math.Round(qLoai1 * 100.0 / qLoai2, 2);
            double tlLoai2Thuc = Math.Round(qLoai2 * 100.0 / tongQS, 2);
            double tlLoai3Thuc = Math.Round(qLoai3 * 100.0 / tongQS, 2);

            // HIỂN THỊ KẾT QUẢ BÁO CÁO
            ListBox2.Items.Add($"KẾT QUẢ TÍNH TOÁN THI ĐUA ({CheDoXet?.ToUpper()})");
            ListBox2.Items.Add(new string('-', 30));
            ListBox2.Items.Add("1. Thông số theo quy định:");
            ListBox2.Items.Add($"   {Module_HeThong.Loai_1}: {text_Textloai1.Text}% (Tính trong {Module_HeThong.Loai_2})");
            ListBox2.Items.Add($"   {Module_HeThong.Loai_2}: {text_Textloai2.Text}% (Tính trong Tổng QS)");
            ListBox2.Items.Add($"   {Module_HeThong.Loai_3}: {text_Textloai3.Text}% (Tính trong Tổng QS)");
            ListBox2.Items.Add("");
            ListBox2.Items.Add($"2. Kết quả khi phân loại tập thể đạt [{com_Textphanloai.Text}]:");
            ListBox2.Items.Add($"   {Module_HeThong.Loai_1}: {qLoai1} đ/c (Đạt {tlLoai1Thuc.ToString(CultureInfo.InvariantCulture)}%)");
            ListBox2.Items.Add($"   {Module_HeThong.Loai_2}: {qLoai2} đ/c (Đạt {tlLoai2Thuc.ToString(CultureInfo.InvariantCulture)}%)");
            ListBox2.Items.Add($"   {Module_HeThong.Loai_3}: {qLoai3} đ/c (Đạt {tlLoai3Thuc.ToString(CultureInfo.InvariantCulture)}%)");
            ListBox2.Items.Add("");
            ListBox2.Items.Add("3. Trích xuất báo cáo nhanh:");
            ListBox2.Items.Add($"   + Số lượng L1/L2 : {qLoai1}/{qLoai2} = {tlLoai1Thuc.ToString(CultureInfo.InvariantCulture)}%");
            ListBox2.Items.Add($"   + Số lượng L2/Tổng: {qLoai2}/{(int)tongQS} = {tlLoai2Thuc.ToString(CultureInfo.InvariantCulture)}%");
            ListBox2.Items.Add($"   + Số lượng L3/Tổng: {qLoai3}/{(int)tongQS} = {tlLoai3Thuc.ToString(CultureInfo.InvariantCulture)}%");
        }

        private void Label5_Click(object sender, EventArgs e)
        {

        }
    }
}