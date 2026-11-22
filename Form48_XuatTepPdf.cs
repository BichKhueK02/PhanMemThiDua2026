///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
///Phiên bản 2: phần xuất PDF chuyển sang Module_XuatPdfOffice; form chỉ lo giao diện + gọi.
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form48_XuatTepPdf : Form
    {
        // Đọc trực tiếp mỗi lần dùng: sau khi khôi phục/ghi đè CSDL, Module_DanduongGPS.NapLaiDuongDan() đổi đường dẫn mà form giữ trong RAM không bị cũ
        private static string DuongDanCsdl2 => Module_DanduongGPS.DuongDanCSDL2;
        private const string ChuaChonExcel = "Chưa chọn tệp excel";
        private const string ChuaChonThuMucPdf = "Chưa chọn thư mục lưu tệp *.pdf";
        // KHAI BÁO WINDOWS API: MỞ THƯ MỤC VÀ BÔI ĐEN TỆP
        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern void ILFree(IntPtr pidl);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern IntPtr ILCreateFromPathW(string pszPath);
        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[]? apidl, uint dwFlags);
        private string? _textGocNutXuatPdf;               // lấy từ Designer ở lần đầu, không còn gán cứng khác chữ trên nút
        private string _tenDonVi = "";
        private string _thang = "";
        private string _nam = "";
        private Dictionary<string, string> _mapQuyDinhTenPdf = new(StringComparer.OrdinalIgnoreCase);
        private volatile bool _dangXuatFile;
        private int _soFileDaXong;
        private int _tongSoFile;
        private bool _dangDongBoCheck;                     // true khi CODE đang đổi trạng thái tick (không phải người dùng bấm)
        private readonly System.Windows.Forms.Timer _timerTienTrinh;
        public class SheetExportItem
        {
            public string OriginalSheetName { get; set; } = "";
            public string TargetPdfName { get; set; } = "";
            public override string ToString()
            {
                // Sheet tự do (không nằm trong danh sách quy định): đích mặc định là "TênSheet.pdf" → hiển thị gọn, không mũi tên
                bool laMacDinh = TargetPdfName.Equals($"{OriginalSheetName}.pdf", StringComparison.OrdinalIgnoreCase) ||
                                 TargetPdfName.Equals(OriginalSheetName, StringComparison.OrdinalIgnoreCase);
                string ten = TargetPdfName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? TargetPdfName : TargetPdfName + ".pdf";
                return laMacDinh ? ten : $"[{OriginalSheetName}] ➔ {ten}";
            }
        }
        public Form48_XuatTepPdf()
        {
            InitializeComponent();   // Designer đã đăng ký Load, nút Xuất, nút chọn thư mục, 2 label: KHÔNG đăng ký lại (bản cũ làm Load chạy 2 lần)
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Trình chuyển đổi tệp excel sang *.pdf";
            ShowInTaskbar = false;
            MaximizeBox = false;
            MinimizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            AcceptButton = kryptonButton_XuatTepPdf;
            Module_HeThong.BocBoTronGiaoDien(checkedListBox1_LietKeTenCacSheet);
            _timerTienTrinh = new System.Windows.Forms.Timer(components!) { Interval = 25 };
            _timerTienTrinh.Tick += TimerTienTrinh_Tick;
            kryptonButton1_ChonDuongDanTepExcel.Click -= KryptonButton1_ChonDuongDanTepExcel_Click;
            kryptonButton1_ChonDuongDanTepExcel.Click += KryptonButton1_ChonDuongDanTepExcel_Click;
            // Danh sách sheet: bấm 1 lần là đổi tick (CheckOnClick) — thay cho mẹo SelectedIndexChanged cũ vốn có thể đổi tick 2 lần khi bấm đúng vào ô vuông
            var clb = checkedListBox1_LietKeTenCacSheet;
            clb.CheckOnClick = true;
            clb.DrawMode = DrawMode.OwnerDrawFixed;
            clb.DrawItem += CheckedListBox1_DrawItem;
            clb.ItemCheck += CheckedListBox1_ItemCheck;
            checkBox1_ChonTatCa.Visible = false;
            checkBox1_ChonTatCa.CheckedChanged += CheckBox1_ChonTatCa_CheckedChanged;
            FormClosing += Form48_FormClosing;
            Module_GiaoDienForm48Web.ApDung(this);
        }
        // =====================================================================================
        //  VÒNG ĐỜI FORM
        // =====================================================================================
        private void Form48_XuatTepPdf_Load(object? sender, EventArgs e)
        {
            _textGocNutXuatPdf ??= kryptonButton_XuatTepPdf.Text;
            toolStripProgressBar1_TienTrinhXuatTep.AutoSize = false;
            toolStripProgressBar1_TienTrinhXuatTep.Size = new Size(200, 12);
            toolStripProgressBar1_TienTrinhXuatTep.Margin = new Padding(1, 5, 1, 5);
            InitToolTips();
            CauHinhStatusLabelGioiThieu();
            ReloadGiaoDienVaDuLieu();
        }
        private void Form48_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Đang xuất: đóng form sẽ để mặc tiến trình Excel/LibreOffice chạy ngầm và hỏng tệp dở dang
            if (_dangXuatFile && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                MessageBox.Show("Đang tạo tệp *.pdf. Vui lòng chờ hoàn tất rồi hãy đóng cửa sổ này.",
                    "Đang xử lý", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        /// <summary>
        /// Được gọi từ Form cha mỗi khi mở lại Form 48 từ RAM: xóa dữ liệu cũ, nạp lại cấu hình mới nhất từ CSDL.
        /// </summary>
        public void ReloadGiaoDienVaDuLieu()
        {
            if (_dangXuatFile) return;   // đang xuất: không được reset giữa chừng
            label_DuongDanExcel.Text = ChuaChonExcel;
            label_DuongDanExcel.ForeColor = Color.Red;
            CapNhatIconNutChonExcel();
            label_DuongDanPdf.Text = ChuaChonThuMucPdf;
            label_DuongDanPdf.ForeColor = Color.Red;
            toolStripProgressBar1_TienTrinhXuatTep.Value = 0;
            toolStripProgressBar1_TienTrinhXuatTep.Visible = false;
            checkedListBox1_LietKeTenCacSheet.Items.Clear();
            _dangDongBoCheck = true;
            try { checkBox1_ChonTatCa.Checked = false; checkBox1_ChonTatCa.Visible = false; }
            finally { _dangDongBoCheck = false; }
            kryptonButton_XuatTepPdf.Enabled = false;
            kryptonButton1_ChonDuongDanTepExcel.Enabled = true;
            TaiThongTinDonViVaThoiGian();
            ThietLapDictonaryAnhXa();
            CapNhatStatusStrip(0, 0, 0);
        }
        // =====================================================================================
        //  GIAO DIỆN PHỤ TRỢ
        // =====================================================================================
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            toolTip1.InitialDelay = 300;
            toolTip1.AutoPopDelay = 2500;
            toolTip1.ReshowDelay = 100;
            toolTip1.ShowAlways = true;
            toolTip1.SetToolTip(kryptonButton1_ChonDuongDanLuuTepPdf, "Chọn đường dẫn lưu tệp *.pdf");
            toolTip1.SetToolTip(kryptonButton_XuatTepPdf, "Xuất tệp *.pdf");
            // Tooltip nút chọn Excel do CapNhatIconNutChonExcel đặt (đổi theo trạng thái đã chọn / chưa chọn)
        }
        private void CauHinhStatusLabelGioiThieu()
        {
            var lb = toolStripStatusLabel1_GioiThieu;
            lb.Spring = true;
            lb.TextAlign = ContentAlignment.MiddleRight;
            lb.Text = "💡 Giới thiệu";
            lb.IsLink = true;
            lb.LinkBehavior = LinkBehavior.HoverUnderline;
            lb.Click -= ToolStripStatusLabel1_GioiThieu_Click;
            lb.Click += ToolStripStatusLabel1_GioiThieu_Click;
        }
        private void ToolStripStatusLabel1_GioiThieu_Click(object? sender, EventArgs e)
        {
            const string thongBao =
                "✔ Tạo tệp gửi lên phần mềm QLVB ĐHTN - K02 nhanh chóng.\n" +
                "✔ Ánh xạ tên tệp và bảng biểu theo đúng chuẩn quy định.\n" +
                "✔ Tối ưu định dạng trang in, căn chỉnh lề và cấu hình đóng gói PDF.\n" +
                "✔ Phù hợp với nhiều máy tính và các phần mềm văn phòng khác nhau (Microsoft Excel, WPS, LibreOffice).\n" +
                "✔ Hỗ trợ trích xuất nhanh văn bản phục vụ lưu trữ và chuyển giao số hóa.";
            MessageBox.Show(thongBao, "Giới thiệu chức năng trình tạo *.pdf", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        /// <summary>Cập nhật icon dauCong.png / dauTru.png và ToolTip cho nút chọn tệp Excel.</summary>
        private void CapNhatIconNutChonExcel()
        {
            bool chuaChon = string.IsNullOrWhiteSpace(label_DuongDanExcel.Text) ||
                            label_DuongDanExcel.Text.Equals(ChuaChonExcel, StringComparison.OrdinalIgnoreCase);
            kryptonButton1_ChonDuongDanTepExcel.Values.Image = chuaChon ? Properties.Resources.dauCong : Properties.Resources.dauTru;
            toolTip1.SetToolTip(kryptonButton1_ChonDuongDanTepExcel, chuaChon ? "Chọn đường dẫn tệp excel" : "Hủy chọn tệp excel hiện tại");
        }
        private void CapNhatStatusStrip(int tongSheet, int sheetDaChon, int fileDaTao)
        {
            if (tongSheet == 0)
            {
                toolStripStatusLabel1_TongCongTepPdf.Text = "Chưa nạp dữ liệu từ tệp Excel.";
                return;
            }
            var ds = new List<string> { $"Tổng cộng: {tongSheet} sheet đã tìm thấy" };
            if (sheetDaChon > 0) ds.Add($"Đã chọn {sheetDaChon} sheet để tạo *.pdf");
            if (fileDaTao > 0) ds.Add($"Đã tạo thành công {fileDaTao} file *.pdf");
            toolStripStatusLabel1_TongCongTepPdf.Text = string.Join(" | ", ds);
        }
        // =====================================================================================
        //  DANH SÁCH SHEET + "CHỌN TẤT CẢ"
        // =====================================================================================
        private void CheckBox1_ChonTatCa_CheckedChanged(object? sender, EventArgs e)
        {
            if (_dangDongBoCheck) return;
            var clb = checkedListBox1_LietKeTenCacSheet;
            if (clb.Items.Count == 0) return;
            bool chon = checkBox1_ChonTatCa.Checked;
            checkBox1_ChonTatCa.ForeColor = chon ? Color.DarkGreen : Color.Red;
            _dangDongBoCheck = true;
            clb.BeginUpdate();
            try
            {
                for (int i = 0; i < clb.Items.Count; i++) clb.SetItemChecked(i, chon);
            }
            finally
            {
                clb.EndUpdate();
                _dangDongBoCheck = false;
            }
            CapNhatStatusStrip(clb.Items.Count, clb.CheckedItems.Count, 0);
            clb.Invalidate();
        }
        private void CheckedListBox1_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            if (_dangDongBoCheck) return;
            var clb = checkedListBox1_LietKeTenCacSheet;
            // Số mục sẽ được tick SAU thao tác này (ItemCheck chạy TRƯỚC khi trạng thái đổi)
            int sau = clb.CheckedItems.Count;
            if (e.CurrentValue != CheckState.Checked && e.NewValue == CheckState.Checked) sau++;
            else if (e.CurrentValue == CheckState.Checked && e.NewValue != CheckState.Checked) sau--;
            CapNhatStatusStrip(clb.Items.Count, sau, 0);
            if (checkBox1_ChonTatCa.Visible)
            {
                bool tatCa = sau == clb.Items.Count;
                _dangDongBoCheck = true;
                try { checkBox1_ChonTatCa.Checked = tatCa; checkBox1_ChonTatCa.ForeColor = tatCa ? Color.DarkGreen : Color.Red; }
                finally { _dangDongBoCheck = false; }
            }
            if (IsHandleCreated) BeginInvoke(new Action(() => { if (!clb.IsDisposed) clb.Invalidate(); }));
        }
        private void CheckedListBox1_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || sender is not CheckedListBox clb) return;
            // Tự tô nền (không dùng DrawBackground): tránh dải xanh chọn-dòng làm chữ xanh/đỏ khó đọc
            using (var nen = new SolidBrush(clb.BackColor)) e.Graphics.FillRectangle(nen, e.Bounds);
            bool ok = clb.GetItemChecked(e.Index);
            int o = Math.Min(e.Bounds.Height - 4, LogicalToDeviceUnits(16));
            ControlPaint.DrawCheckBox(e.Graphics, e.Bounds.Left + 4, e.Bounds.Top + (e.Bounds.Height - o) / 2, o, o,
                ok ? ButtonState.Checked : ButtonState.Normal);
            var vung = new Rectangle(e.Bounds.Left + o + 10, e.Bounds.Top, Math.Max(10, e.Bounds.Width - o - 14), e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, clb.Items[e.Index]?.ToString() ?? "", e.Font, vung,
                ok ? Color.DarkGreen : Color.Red,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        }
        // =====================================================================================
        //  NẠP THÔNG TIN TỪ CSDL
        // =====================================================================================
        private void TaiThongTinDonViVaThoiGian()
        {
            _thang = Module_XuatPhanLoai.LayThangHeThong();
            // Chuẩn hóa tháng theo quy định: 1 -> 01, 2 -> 02, các tháng còn lại giữ nguyên
            if (int.TryParse(_thang, out int thang))
                _thang = (thang == 1 || thang == 2) ? thang.ToString("00") : thang.ToString();
            _nam = Module_XuatPhanLoai.LayNamHeThong();
            try
            {
                string db = DuongDanCsdl2;
                if (string.IsNullOrWhiteSpace(db)) throw new FileNotFoundException("Chưa có đường dẫn CSDL2.");
                using var conn = Module_DanduongGPS.MoKetNoiDoc(db);
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT TenTieuDoan FROM ThongTin WHERE ID = 1";
                var result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    string td = Module_BaoMatAES.GiaiMa(result.ToString() ?? "").Trim();
                    if (!string.IsNullOrEmpty(td)) _tenDonVi = td;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi đọc thông tin đơn vị: " + ex.Message);
                if (string.IsNullOrEmpty(_tenDonVi)) _tenDonVi = "Đơn vị";
            }
        }
        private void ThietLapDictonaryAnhXa()
        {
            string tenDonViHienThi = _tenDonVi;
            if (!string.IsNullOrWhiteSpace(tenDonViHienThi))
            {
                string thuong = tenDonViHienThi.ToLower();
                tenDonViHienThi = char.ToUpper(thuong[0]) + thuong.Substring(1);
            }
            string baseString = $"của {tenDonViHienThi} tháng {_thang}-{_nam}.pdf";
            _mapQuyDinhTenPdf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "LOAI_1", $"Danh sách đề nghị loại 1 phong trào thi đua \"Vì ANTQ\" {baseString}" },
                { "LOAI_2", $"Danh sách đề nghị loại 2 phong trào thi đua \"Vì ANTQ\" {baseString}" },
                { "LOAI_3", $"Danh sách đề nghị loại 3 phong trào thi đua \"Vì ANTQ\" {baseString}" },
                { "LOAI_4", $"Danh sách đề nghị loại 4 phong trào thi đua \"Vì ANTQ\" {baseString}" },
                { "KHONG_PL", $"Danh sách đề nghị không phân loại phong trào thi đua \"Vì ANTQ\" {baseString}" },
                { "BAO CAO TONG HOP", $"Danh sách tổng hợp phong trào thi đua \"Vì ANTQ\" {baseString}" },
                { "BC_BANHAT", $"Báo cáo tổng hợp đề nghị biểu dương gương tiêu biểu phong trào thi đua \"Ba nhất\" {baseString}" },
                { "DS_BANHAT", $"Danh sách đề nghị biểu dương gương tiêu biểu phong trào thi đua \"Ba nhất\" {baseString}" }
            };
        }
        /// <summary>Đọc thông tin điền vào sheet (địa điểm, tháng, năm, ký hiệu...). Gọi ở luồng nền.</summary>
        private static ThongTinThayThe DocThongTinThayThe()
        {
            string diaDiem = "      ", thang = "  ", nam = "    ", kyHieuBaoCao = "...............", tieuDoanHienThi = "      ";
            try
            {
                string db = DuongDanCsdl2;
                if (!string.IsNullOrWhiteSpace(db))
                {
                    using var conn = Module_DanduongGPS.MoKetNoiDoc(db);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT Thang, Nam, DiaDiem, KyHieuBaoCao, TenTieuDoan FROM ThongTin WHERE ID = 1";
                    using var rd = cmd.ExecuteReader();
                    if (rd.Read())
                    {
                        static string G(SqliteDataReader r, int i) => Module_BaoMatAES.GiaiMa(r.IsDBNull(i) ? "" : r.GetString(i)).Trim();
                        string v;
                        if ((v = G(rd, 2)).Length > 0) diaDiem = v;
                        if ((v = G(rd, 0)).Length > 0) thang = v;
                        if ((v = G(rd, 1)).Length > 0) nam = v;
                        if ((v = G(rd, 3)).Length > 0) kyHieuBaoCao = v;
                        if ((v = G(rd, 4)).Length > 0) { v = v.ToLower(); tieuDoanHienThi = char.ToUpper(v[0]) + v.Substring(1); }
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine("Lỗi đọc thông tin thay thế: " + ex.Message); }
            string diaDiemThangNam = $"{diaDiem}, ngày                  tháng {thang} năm {nam}";
            const string phanDau = "(Kèm theo Báo cáo ";
            string phanGiua = $"số:            {kyHieuBaoCao}, ngày             /{thang}/{nam}";
            string phanCuoi = $" của {tieuDoanHienThi})";
            return new ThongTinThayThe(diaDiemThangNam, phanDau + phanGiua + phanCuoi, phanDau.Length, phanGiua.Length);
        }
        // =====================================================================================
        //  CHỌN TỆP EXCEL / THƯ MỤC LƯU
        // =====================================================================================
        private async void KryptonButton1_ChonDuongDanTepExcel_Click(object? sender, EventArgs e)
        {
            if (_dangXuatFile) return;
            // Đã chọn rồi → nút đang là dấu "−": hủy chọn
            if (!label_DuongDanExcel.Text.Equals(ChuaChonExcel, StringComparison.OrdinalIgnoreCase))
            {
                ReloadGiaoDienVaDuLieu();
                return;
            }
            using var ofd = new OpenFileDialog
            {
                Title = "Chọn tệp Excel nguồn",
                Filter = "Excel Files|*.xlsx;*.xlsm",
                CheckFileExists = true
            };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;
            string tep = ofd.FileName;
            label_DuongDanExcel.Text = tep;
            label_DuongDanExcel.ForeColor = Color.DarkGreen;
            // Tự gán thư mục chứa tệp Excel làm nơi lưu PDF
            string? thuMuc = Path.GetDirectoryName(tep);
            if (!string.IsNullOrEmpty(thuMuc))
            {
                label_DuongDanPdf.Text = thuMuc;
                label_DuongDanPdf.ForeColor = Color.DarkGreen;
            }
            CapNhatIconNutChonExcel();
            kryptonButton1_ChonDuongDanTepExcel.Enabled = false;
            kryptonButton_XuatTepPdf.Enabled = false;
            toolStripStatusLabel1_DangLoad.Visible = true;
            toolStripStatusLabel1_DangLoad.Text = "Đang phân tích tệp Excel, vui lòng đợi...";
            try
            {
                await PhanTichVaAnhXaSheetTuExcelAsync(tep);
            }
            finally
            {
                if (!IsDisposed)
                {
                    kryptonButton1_ChonDuongDanTepExcel.Enabled = true;
                    kryptonButton_XuatTepPdf.Enabled = checkedListBox1_LietKeTenCacSheet.Items.Count > 0;
                    toolStripStatusLabel1_DangLoad.Visible = false;
                    toolStripStatusLabel1_DangLoad.Text = "";
                }
            }
        }
        private async Task PhanTichVaAnhXaSheetTuExcelAsync(string excelPath)
        {
            try
            {
                checkedListBox1_LietKeTenCacSheet.Items.Clear();
                var tenSheet = await Task.Run(() => Module_XuatPdfOffice.LietKeTenSheet(excelPath));
                if (IsDisposed) return;
                var clb = checkedListBox1_LietKeTenCacSheet;
                _dangDongBoCheck = true;
                clb.BeginUpdate();
                try
                {
                    foreach (string ten in tenSheet)
                    {
                        string dich = _mapQuyDinhTenPdf.TryGetValue(ten, out string? mapped) ? mapped : $"{ten}.pdf";
                        clb.Items.Add(new SheetExportItem { OriginalSheetName = ten, TargetPdfName = dich }, true);
                    }
                }
                finally { clb.EndUpdate(); _dangDongBoCheck = false; }
                _dangDongBoCheck = true;
                try
                {
                    checkBox1_ChonTatCa.Checked = true;
                    checkBox1_ChonTatCa.ForeColor = Color.DarkGreen;
                    checkBox1_ChonTatCa.Visible = clb.Items.Count >= 2;   // chỉ hiện khi từ 2 sheet trở lên
                }
                finally { _dangDongBoCheck = false; }
                CapNhatStatusStrip(clb.Items.Count, clb.CheckedItems.Count, 0);
            }
            catch (Exception ex)
            {
                if (IsDisposed) return;
                MessageBox.Show("Không đọc được tệp Excel (đang bị khóa, sai định dạng hoặc bị đặt mật khẩu):\n" + ex.Message,
                    "Lỗi đọc tệp", MessageBoxButtons.OK, MessageBoxIcon.Error);
                label_DuongDanExcel.Text = ChuaChonExcel;
                label_DuongDanExcel.ForeColor = Color.Red;
                CapNhatIconNutChonExcel();
            }
        }
        private void kryptonButton1_ChonDuongDanLuuTepPdf_Click(object? sender, EventArgs e)
        {
            if (_dangXuatFile) return;
            string thuMucMacDinh = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            try
            {
                string db = DuongDanCsdl2;
                if (!string.IsNullOrWhiteSpace(db))
                {
                    using var conn = Module_DanduongGPS.MoKetNoiDoc(db);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT ChonDuongDanXuatTep FROM ThongTin WHERE ID = 1";
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        string p = Module_BaoMatAES.GiaiMa(result.ToString() ?? "").Trim();
                        if (!string.IsNullOrWhiteSpace(p) && Directory.Exists(p)) thuMucMacDinh = p;   // nhớ thư mục lần trước
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine("Đọc thư mục lưu gần nhất: " + ex.Message); }
            using var fbd = new FolderBrowserDialog
            {
                Description = "Chọn thư mục lưu các tệp *.pdf xuất ra",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true,
                InitialDirectory = thuMucMacDinh,
                SelectedPath = thuMucMacDinh
            };
            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                label_DuongDanPdf.Text = fbd.SelectedPath;
                label_DuongDanPdf.ForeColor = Color.DarkBlue;
            }
        }
        // =====================================================================================
        //  XUẤT PDF
        // =====================================================================================
        private async void kryptonButton_XuatTepPdf_Click(object? sender, EventArgs e)
        {
            if (_dangXuatFile) return;
            string excelPath = label_DuongDanExcel.Text?.Trim() ?? "";
            string pdfFolder = label_DuongDanPdf.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(excelPath) ||
                excelPath.Equals(ChuaChonExcel, StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(excelPath))
            {
                MessageBox.Show("Bạn chưa chọn tệp Excel nguồn hoặc tệp không còn tồn tại!\nHệ thống sẽ mở hộp thoại để bạn chọn tệp ngay bây giờ.",
                    Module_HeThong.Goi_Y_Thao_Tac, MessageBoxButtons.OK, MessageBoxIcon.Information);
                kryptonButton1_ChonDuongDanTepExcel.PerformClick();
                return;
            }
            if (!Directory.Exists(pdfFolder))
            {
                MessageBox.Show("Bạn chưa chọn thư mục đích để lưu tệp *.pdf!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                kryptonButton1_ChonDuongDanLuuTepPdf.PerformClick();
                return;
            }
            var muc = checkedListBox1_LietKeTenCacSheet.CheckedItems.Cast<SheetExportItem>()
                        .Select(i => new MucXuatPdf(i.OriginalSheetName, i.TargetPdfName)).ToList();
            if (muc.Count == 0)
            {
                MessageBox.Show("Vui lòng tích chọn ít nhất 1 Sheet để xuất tệp *.pdf", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            BatDauXuat(muc.Count);
            try
            {
                var tt = await Task.Run(DocThongTinThayThe);
                var kq = await Module_XuatPdfOffice.XuatAsync(excelPath, pdfFolder, muc, tt, BaoDaXong);
                if (IsDisposed) return;
                XuLyKetQua(kq, pdfFolder, muc.Count);
            }
            catch (LoiNguoiDungException ex)
            {
                // Lỗi do điều kiện của người dùng (đường dẫn dài, tệp đang mở, không có quyền ghi...): báo rõ, không coi là sự cố hệ thống
                if (!IsDisposed) MessageBox.Show(ex.Message, "Chưa thể xuất tệp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                Module_NhatKy.GhiNhatKy(
                    taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                    hanhDong: "Lỗi gián đoạn khi xuất tệp PDF",
                    ghiChu: $"Chi tiết lỗi: {ex.Message} | Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
                if (!IsDisposed) MessageBox.Show("Quá trình xuất bị gián đoạn:\n" + ex.Message, "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                await KetThucXuatAsync();
            }
        }
        private void XuLyKetQua(KetQuaXuatPdf kq, string pdfFolder, int tongYeuCau)
        {
            int ok = kq.DaTao.Count;
            CapNhatStatusStrip(checkedListBox1_LietKeTenCacSheet.Items.Count, tongYeuCau, ok);
            if (ok > 0)
            {
                Module_NhatKy.GhiNhatKy(
                    taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                    hanhDong: $"Xuất thành công {ok} tệp PDF",
                    ghiChu: $"Lưu tại: {pdfFolder} | Phần mềm xuất: {kq.TenDongCo} | Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
            }
            if (kq.ThatBai.Count == 0)
            {
                Module_ThongBao.ThanhCong($"Xuất thành công {ok} tệp PDF");
            }
            else
            {
                string chiTiet = string.Join("\n", kq.ThatBai.Select(t => $"• {t.Sheet}: {t.Loi}"));
                Module_NhatKy.GhiNhatKy(
                    taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                    hanhDong: $"Xuất PDF chưa đủ: {ok}/{tongYeuCau} tệp",
                    ghiChu: chiTiet.Replace("\n", " ; "));
                MessageBox.Show(
                    (ok > 0 ? $"Đã tạo {ok}/{tongYeuCau} tệp PDF. Các sheet sau chưa xuất được:\n\n" : "Không tạo được tệp PDF nào. Chi tiết:\n\n") + chiTiet,
                    ok > 0 ? "Xuất chưa hoàn tất" : "Xuất thất bại",
                    MessageBoxButtons.OK, ok > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Error);
            }
            if (ok > 0 && !string.IsNullOrEmpty(kq.FileCuoi) && File.Exists(kq.FileCuoi))
                MoThuMucVaChonTep(kq.FileCuoi);
        }
        // ---------- thanh tiến trình ----------
        private void BatDauXuat(int tong)
        {
            _textGocNutXuatPdf ??= kryptonButton_XuatTepPdf.Text;
            kryptonButton_XuatTepPdf.Enabled = false;
            kryptonButton1_ChonDuongDanTepExcel.Enabled = false;
            kryptonButton1_ChonDuongDanLuuTepPdf.Enabled = false;
            checkedListBox1_LietKeTenCacSheet.Enabled = false;
            kryptonButton_XuatTepPdf.Text = "Đang xử lý...";
            _dangXuatFile = true;
            _soFileDaXong = 0;
            _tongSoFile = tong;
            var pb = toolStripProgressBar1_TienTrinhXuatTep;
            pb.Visible = true;
            pb.Minimum = 0;
            pb.Maximum = tong * 100;   // nhân 100 để thanh trượt mịn
            pb.Value = 0;
            _timerTienTrinh.Start();
        }
        /// <summary>Gọi từ luồng nền khi xong thêm 1 tệp.</summary>
        private void BaoDaXong(int tongDaXong)
        {
            if (IsDisposed || !IsHandleCreated) return;
            try
            {
                BeginInvoke(new Action(() =>
                {
                    if (IsDisposed) return;
                    _soFileDaXong = tongDaXong;
                    var pb = toolStripProgressBar1_TienTrinhXuatTep;
                    int muc = Math.Min(tongDaXong * 100, pb.Maximum);
                    if (pb.Value < muc) pb.Value = muc;
                    CapNhatStatusStrip(checkedListBox1_LietKeTenCacSheet.Items.Count, _tongSoFile, tongDaXong);
                }));
            }
            catch (InvalidOperationException) { /* form vừa đóng */ }
        }
        // Thanh trượt "ảo": chạy mượt tới 90% phần của file đang xử lý, file xong thật mới nhảy tới 100% phần đó
        private void TimerTienTrinh_Tick(object? sender, EventArgs e)
        {
            var pb = toolStripProgressBar1_TienTrinhXuatTep;
            if (!_dangXuatFile || !pb.Visible) return;
            int mucTieuAo = Math.Min(_soFileDaXong * 100 + 90, pb.Maximum);
            if (pb.Value < mucTieuAo) pb.Value++;
        }
        private async Task KetThucXuatAsync()
        {
            _dangXuatFile = false;
            _timerTienTrinh.Stop();
            if (IsDisposed) return;
            var pb = toolStripProgressBar1_TienTrinhXuatTep;
            pb.Value = pb.Maximum;                 // trượt nốt lên 100% cho trọn vẹn
            await Task.Delay(300);
            if (IsDisposed) return;
            pb.Visible = false;
            checkedListBox1_LietKeTenCacSheet.Enabled = true;
            kryptonButton1_ChonDuongDanTepExcel.Enabled = true;
            kryptonButton1_ChonDuongDanLuuTepPdf.Enabled = true;
            kryptonButton_XuatTepPdf.Enabled = checkedListBox1_LietKeTenCacSheet.Items.Count > 0;
            kryptonButton_XuatTepPdf.Text = _textGocNutXuatPdf ?? kryptonButton_XuatTepPdf.Text;
        }
        // =====================================================================================
        //  MỞ THƯ MỤC / TỆP
        // =====================================================================================
        private static void MoThuMucVaChonTep(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;
            IntPtr pidl = ILCreateFromPathW(filePath);
            if (pidl == IntPtr.Zero) return;
            try { SHOpenFolderAndSelectItems(pidl, 0, null, 0); }
            catch (Exception ex) { Debug.WriteLine("Lỗi API Mở thư mục: " + ex.Message); }
            finally { ILFree(pidl); }
        }
        private void label_DuongDanExcel_Click(object? sender, EventArgs e)
        {
            string p = label_DuongDanExcel.Text?.Trim() ?? "";
            if (!string.IsNullOrWhiteSpace(p) && File.Exists(p)) MoThuMucVaChonTep(p);
            else MessageBox.Show("Đường dẫn tệp Excel không tồn tại hoặc chưa được chọn!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void label_DuongDanPdf_Click(object? sender, EventArgs e)
        {
            string p = label_DuongDanPdf.Text?.Trim() ?? "";
            if (!string.IsNullOrWhiteSpace(p) && Directory.Exists(p))
            {
                try { Process.Start(new ProcessStartInfo { FileName = p, UseShellExecute = true, Verb = "open" }); }
                catch (Exception ex) { MessageBox.Show("Không thể mở thư mục:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
            else
            {
                MessageBox.Show("Thư mục lưu tệp PDF không tồn tại hoặc chưa được chọn!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
