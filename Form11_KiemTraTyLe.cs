using System.Globalization;

namespace PhanMemThiDua2026
{
    public partial class Form11_KiemTraTyLe : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private static readonly string[] GoiYPhanLoai = { "Loại 1", "Loại 2", "Loại 3", "Loại 4" };
        // 🚀 BỘ NHỚ ĐỆM (CACHE): Lưu sẵn tỷ lệ của cả 4 loại, không cần query DB nhiều lần
        private Dictionary<string, double[]> _cacheTyLe = new Dictionary<string, double[]>();
        private bool _daTaiXong = false;
        private bool _toolTipInited = false;
        public Form11_KiemTraTyLe()
        {
            InitializeComponent();
            this.ShowInTaskbar = false;

            // 🖥️ Tương thích màn hình: Form luôn ra giữa, chống lệch trên màn scale
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            // 🎯 UX: Nhấn Enter ở bất kỳ đâu cũng sẽ kích hoạt nút Tính
            this.AcceptButton = btn_TextTinh;

            // 🎯 UX: Tự động bôi đen text khi ô quân số nhận tiêu điểm
            text_Texttongquanso.Enter += (s, e) => text_Texttongquanso.SelectAll();
            text_Texttongquanso.Click += (s, e) => text_Texttongquanso.SelectAll();
        }
        private void Form11_Load(object sender, EventArgs e)
        {
            // Ép cỡ chữ của ListBox2 nhỏ lại để tiết kiệm diện tích (Size 10)
            ListBox2.Font = new Font(ListBox2.Font.FontFamily, 10f, FontStyle.Regular);

            InitToolTips();

            // Đọc DB 1 lần duy nhất khi mở Form
            TaiDuLieuTuSQLiteVaoCache();

            // Nạp dữ liệu ComboBox
            com_Textphanloai.Items.Clear();
            com_Textphanloai.Items.AddRange(GoiYPhanLoai);
            com_Textphanloai.SelectedIndexChanged -= Com_textphanloai_SelectedIndexChanged;
            com_Textphanloai.SelectedIndexChanged += Com_textphanloai_SelectedIndexChanged;

            _daTaiXong = true;

            // Tự động chọn Loại 2 làm mặc định (sẽ kích hoạt sự kiện SelectedIndexChanged)
            if (com_Textphanloai.Items.Count > 1)
                com_Textphanloai.SelectedIndex = 1;

            if (string.IsNullOrWhiteSpace(text_Texttongquanso.Text))
            {
                ListBox2.Items.Add("⚠️ Đồng chí hãy nhập Tổng quân số!");
                ListBox2.Items.Add("Để thực hiện phép tính số lượng đạt tỷ lệ %");
            }
            text_Texttongquanso.Focus();
        }
        private void InitToolTips()
        {
            if (_toolTipInited) return;
            _toolTipInited = true;

            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Chức năng";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;

            if (btn_TextTinh != null) toolTip1.SetToolTip(btn_TextTinh, "Nhấn Enter hoặc Click để tính toán");
            if (com_Textphanloai != null) toolTip1.SetToolTip(com_Textphanloai, "Chọn phân loại tập thể");
        }
        // KHỐI 1: XỬ LÝ DATABASE & CACHE (HIỆU SUẤT)
        private void TaiDuLieuTuSQLiteVaoCache()
        {
            _cacheTyLe.Clear();
            foreach (var loai in GoiYPhanLoai)
            {
                _cacheTyLe[loai] = new double[3]; // Khởi tạo mảng 3 phần tử (chứa ID 1, 2, 3)
            }

            if (!File.Exists(_csdl2Path))
            {
                MessageBox.Show("Không tìm thấy tệp cơ sở dữ liệu hệ thống!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_csdl2Path}"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        // Lấy luôn cả 4 cột, chỉ 1 vòng quét DB duy nhất
                        cmd.CommandText = "SELECT ID, Loai_1, Loai_2, Loai_3, Loai_4 FROM QuyDinhTyLe WHERE ID IN (1, 2, 3)";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int id = Convert.ToInt32(reader["ID"]);
                                if (id < 1 || id > 3) continue;

                                int index = id - 1; // Chuyển ID (1,2,3) thành Index mảng (0,1,2)

                                _cacheTyLe["Loại 1"][index] = GiaiMaVaChuanHoa(reader["Loai_1"]?.ToString());
                                _cacheTyLe["Loại 2"][index] = GiaiMaVaChuanHoa(reader["Loai_2"]?.ToString());
                                _cacheTyLe["Loại 3"][index] = GiaiMaVaChuanHoa(reader["Loai_3"]?.ToString());
                                _cacheTyLe["Loại 4"][index] = GiaiMaVaChuanHoa(reader["Loai_4"]?.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nạp CSDL Form 11: {ex.Message}", "Lỗi Debug");
            }
        }
        private double GiaiMaVaChuanHoa(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue)) return 0;

            string decryptedValue = BaoMatAES.GiaiMa(rawValue).Trim();
            if (string.IsNullOrWhiteSpace(decryptedValue))
                decryptedValue = rawValue.Trim();

            decryptedValue = decryptedValue.Replace(",", ".");
            double.TryParse(decryptedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double giaTri);

            // Bảo mật dữ liệu: Ép tỷ lệ phải nằm trong khoảng logic 0 - 100%
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
            if (_cacheTyLe.TryGetValue(loaiDaChon, out double[] tyLe))
            {
                text_Textloai1.Text = tyLe[0].ToString(CultureInfo.InvariantCulture);
                text_Textloai2.Text = tyLe[1].ToString(CultureInfo.InvariantCulture);
                text_Textloai3.Text = tyLe[2].ToString(CultureInfo.InvariantCulture);
            }
        }
        // ⚡ Hàm tính toán bất đồng bộ
        private async void btn_texttinh_Click(object sender, EventArgs e)
        {
            // =================================================================
            // BƯỚC 1: KIỂM TRA DỮ LIỆU NGAY LẬP TỨC (KHÔNG CHẠY TIẾN TRÌNH NẾU LỖI)
            // =================================================================
            string strTongQS = text_Texttongquanso.Text.Replace(",", ".");
            if (!double.TryParse(strTongQS, NumberStyles.Any, CultureInfo.InvariantCulture, out double tongQS) || tongQS <= 1 || tongQS > 1000000)
            {
                ListBox2.Items.Clear();
                System.Media.SystemSounds.Exclamation.Play();
                ListBox2.Items.Add("⚠️ Đồng chí hãy nhập Tổng quân số hợp lệ (Từ 2 đến 1.000.000)!");
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
                ListBox2.Items.Add("⚠️ Dữ liệu tỷ lệ từ CSDL không hợp lệ!");
                return; // ⚡ Chặn đứng tại đây
            }

            // =================================================================
            // BƯỚC 2: DỮ LIỆU ĐÃ CHUẨN -> KHÓA UI VÀ CHẠY THANH TIẾN ĐỘ
            // =================================================================
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

                // =================================================================
                // BƯỚC 3: THỰC THI TÍNH TOÁN VÀ TRUYỀN SỐ LIỆU ĐÃ KIỂM TRA VÀO
                // =================================================================
                ThucThiTinhToan(tongQS, pLoai1, pLoai2, pLoai3);
                System.Media.SystemSounds.Beep.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Có lỗi xảy ra: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btn_TextTinh.Text = "Tính";
                btn_TextTinh.Enabled = true;

                tienDo_kryptonProgressBar1.Text = "Hoàn thành";
                tienDo_kryptonProgressBar1.Value = 0;

                text_Texttongquanso.Focus();
            }
        }
        // KHỐI 3: THUẬT TOÁN TÍNH TỶ LỆ CHUẨN XÁC
        // ⚡ Hàm này giờ chỉ tập trung tính toán, nhận luôn con số đầu vào từ nút bấm
        private void ThucThiTinhToan(double tongQS, double pLoai1, double pLoai2, double pLoai3)
        {
            // Chuyển % sang hệ số thập phân
            pLoai1 /= 100.0; pLoai2 /= 100.0; pLoai3 /= 100.0;

            // TÍNH TOÁN QUÂN SỐ CƠ BẢN
            int qLoai2 = (int)Math.Round(tongQS * pLoai2);
            int qLoai3 = (int)Math.Round(tongQS * pLoai3);

            // Loại 1 là tập con của Loại 2 (Chiến sĩ TĐCS xét từ LĐTT)
            int qLoai1 = (int)Math.Round(qLoai2 * pLoai1);
            if (qLoai1 > qLoai2) qLoai1 = qLoai2; // Rào chắn an toàn

            // ⚖️ CÂN BẰNG QUÂN SỐ 
            if (qLoai2 + qLoai3 > tongQS)
            {
                qLoai3 = (int)tongQS - qLoai2;
                if (qLoai3 < 0) qLoai3 = 0;
            }
            else
            {
                qLoai3 = (int)tongQS - qLoai2;
            }

            // TÍNH TOÁN LẠI TỶ LỆ THỰC TẾ ĐẠT ĐƯỢC SAU LÀM TRÒN
            double tlLoai1Thuc = qLoai2 == 0 ? 0 : Math.Round(qLoai1 * 100.0 / qLoai2, 2);
            double tlLoai2Thuc = Math.Round(qLoai2 * 100.0 / tongQS, 2);
            double tlLoai3Thuc = Math.Round(qLoai3 * 100.0 / tongQS, 2);

            // HIỂN THỊ KẾT QUẢ BÁO CÁO
            ListBox2.Items.Add("KẾT QUẢ TÍNH TOÁN THI ĐUA");
            ListBox2.Items.Add(new string('-', 30));

            ListBox2.Items.Add("1. Thông số theo quy định:");
            ListBox2.Items.Add($"   Loại 1: {text_Textloai1.Text}% (Tính trong Loại 2)");
            ListBox2.Items.Add($"   Loại 2: {text_Textloai2.Text}% (Tính trong Tổng QS)");
            ListBox2.Items.Add($"   Loại 3: {text_Textloai3.Text}% (Tính trong Tổng QS)");

            ListBox2.Items.Add("");
            ListBox2.Items.Add($"2. Kết quả khi phân loại tập thể đạt [{com_Textphanloai.Text}]:");
            ListBox2.Items.Add($"   Loại 1: {qLoai1} đ/c (Đạt {tlLoai1Thuc.ToString(CultureInfo.InvariantCulture)}%)");
            ListBox2.Items.Add($"   Loại 2: {qLoai2} đ/c (Đạt {tlLoai2Thuc.ToString(CultureInfo.InvariantCulture)}%)");
            ListBox2.Items.Add($"   Loại 3: {qLoai3} đ/c (Đạt {tlLoai3Thuc.ToString(CultureInfo.InvariantCulture)}%)");

            ListBox2.Items.Add("");
            ListBox2.Items.Add("3. Trích xuất báo cáo nhanh:");
            ListBox2.Items.Add($"   + Số lượng L1/L2 : {qLoai1}/{qLoai2}");
            ListBox2.Items.Add($"   + Số lượng L2/Tổng: {qLoai2}/{(int)tongQS}");
            ListBox2.Items.Add($"   + Số lượng L3/Tổng: {qLoai3}/{(int)tongQS}");
        }
    }
}