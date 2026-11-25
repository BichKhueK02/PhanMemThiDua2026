///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form46_ThongKeThiDuaNamCu : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        // Quản lý Font tập trung dùng biến hệ thống, chống rò rỉ GDI handle
        private static readonly Font _fontGridHeader95Bold = new Font(Module_HeThong.TenFontHeThong, 9.5F, FontStyle.Bold);
        private static readonly Font _fontGridCell95 = new Font(Module_HeThong.TenFontHeThong, 9.5F, FontStyle.Regular);
        private DataTable? _dtHienTai;
        private List<int> _filteredIndexes = new List<int>(); // Lưu trữ chỉ số dòng đã qua bộ lọc (Virtual Mode Core)
        private Dictionary<int, int> _colIndexMap = new Dictionary<int, int>(); // Bản đồ tọa độ cột siêu tốc
        private List<HistoryCBCSDTO> _dataCacheCBCS = new List<HistoryCBCSDTO>();
        private List<HistoryTanBinhDTO> _dataCacheTanBinh = new List<HistoryTanBinhDTO>();
        private List<string> _cotPhatSinhCacheThongKe = null!;
        private bool _isDataMaxMode = false;
        private const int NGUONG_DU_LIEU = 3000;
        private const string PLACEHOLDER_TIMKIEM = Module_HeThong.Nhap_Tim_Kiem;
        private bool _dangSetPlaceholder = false;
        private bool _isInitialized = false;
        private SolidBrush? _rowHeaderBrush = null;
        private StringFormat? _rowHeaderFormat = null;
        private System.Windows.Forms.Timer _timKiemTimer = null!;
        // 🔥 CACHE INSTANCE FORM 54 TRÊN RAM (Tránh new/dispose lặp lại) 
        public Form46_ThongKeThiDuaNamCu()
        {
            InitializeComponent();
            DinhPhimTatMenuChuotPhai();         // trước ApDung: mục "Làm mới" mới thêm cũng được gắn biểu tượng
            Module_BieuTuongNut.ApDung(this);   // biểu tượng nút: Hiện đại = tự vẽ, Cổ điển = ảnh gốc nhúng trong exe
            TaoBoLocGhiChu();                   // phải trước ApDung: module web định cột + bo tròn cả combo này
            Module_GiaoDienForm46Web.ApDung(this);
            // Chỉ xóa BẢNG thi đua cá nhân của năm; bảng thi đua tập thể (Form59) trong cùng tệp vẫn giữ
            xoaCSDL_ToolStripMenuItem.Text = "Xóa dữ liệu thi đua cá nhân";
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
        private void Form46_ThongKeThiDuaNamCu_Load(object? sender, EventArgs e)
        {
            if (_isInitialized) return;
            Module_DonVi.KhoiTao();
            DinhDangGridBanDau();
            kryptonDataGridView1.ContextMenuStrip = contextMenuStrip1;
            Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1);
            // 1. Đồng bộ sự kiện phản hồi lập tức cho các ComboBox bộ lọc
            // ⭐ CHÈN BỔ SUNG 2 DÒNG NÀY VÀO ĐÂY: Ép gán cứng sự kiện chọn tệp CSDL năm cũ
            comboBox_ChonCSDLNam.SelectedIndexChanged -= comboBox_ChonCSDLNam_SelectedIndexChanged;
            comboBox_ChonCSDLNam.SelectedIndexChanged += comboBox_ChonCSDLNam_SelectedIndexChanged;
            comboBox_TimKiemDonVi.SelectedIndexChanged += ComboBoxFilter_SelectedIndexChanged;
            comboBox1_TinhTrang.SelectedIndexChanged += ComboBoxFilter_SelectedIndexChanged;
            comboBox1_PhanLoaiThiDuaNamCu.SelectedIndexChanged += ComboBoxFilter_SelectedIndexChanged;
            comboBox_LocGhiChu.SelectedIndexChanged += ComboBoxFilter_SelectedIndexChanged;
            // 2. Tích hợp hiệu ứng Placeholder chuẩn cho ô tìm kiếm tên
            InitPlaceholderTimKiem();
            textBox_TimKiemTheoTen.TextChanged += textBox_TimKiemTheoTen_TextChanged;
            textBox_TimKiemTheoTen.Enter += TextBox_TimKiemTheoTen_Enter;
            textBox_TimKiemTheoTen.Leave += TextBox_TimKiemTheoTen_Leave;
            // 3. ⏱️ THIẾT LẬP TIMER TRỄ 300MS CHUẨN UX (Gõ xong mới lọc - Tránh đơ giao diện)
            _timKiemTimer = new System.Windows.Forms.Timer();
            _timKiemTimer.Interval = 200;
            _timKiemTimer.Tick += (s, ev) =>
            {
                _timKiemTimer.Stop();
                ApplyFilter();
            };
            InitToolTips();
            _isInitialized = true;
            // Quét tệp năm cũ + nạp lưới (chạy nền) - ĐÚNG 1 lần; trước đây nạp 2 lần khi mở form
            LoadDanhSachFileLichSu();
        }
        // ===================== PHÍM TẮT MENU CHUỘT PHẢI (đồng bộ Form15/Form34) =====================
        // Chỉ hiện chữ phím tắt trên menu (ShortcutKeyDisplayString); phím được bắt ở ProcessCmdKey bên dưới
        // để không bị gọi 2 lần và tôn trọng trạng thái Enabled của nút/mục menu.
        private void DinhPhimTatMenuChuotPhai()
        {
            var lamMoi = new ToolStripMenuItem { Name = "lamMoi_ToolStripMenuItem", Text = "Làm mới", ShortcutKeyDisplayString = "F5" };
            lamMoi.Click += (_, _) => kryptonButton_CapNhat.PerformClick();
            contextMenuStrip1.Items.Insert(0, lamMoi);   // gắn vào menu TRƯỚC để lấy đúng cỡ ảnh của menu (ImageScalingSize)
            Module_BieuTuongNut.GanBieuTuongChoMucThem(this, lamMoi);
            xoaTimKiem_ToolStripMenuItem.ShortcutKeyDisplayString = "F4";
            xuatDuLieuTepExcel_ToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+E";
            xoaCSDL_ToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+Delete";
            huongDan_ToolStripMenuItem.ShortcutKeyDisplayString = "F1";
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            try
            {
                Keys key = keyData & Keys.KeyCode;
                Keys modifier = keyData & Keys.Modifiers;
                // Ctrl+Shift+Delete: Xóa dữ liệu thi đua cá nhân của năm đang chọn (vẫn hỏi mật khẩu Admin như bấm menu)
                if (modifier == (Keys.Control | Keys.Shift) && key == Keys.Delete)
                {
                    xoaCSDL_ToolStripMenuItem.PerformClick();
                    return true;
                }
                if (Module_PhimTat.XuLy(
                    keyData,
                    actionLamMoi: () => kryptonButton_CapNhat.PerformClick(),              // F5
                    actionXuatExcel: () => xuatDuLieuTepExcel_ToolStripMenuItem.PerformClick(), // Ctrl+E
                    actionTroGiup: () => huongDan_ToolStripMenuItem.PerformClick()))        // F1
                {
                    return true;
                }
                if (modifier == Keys.None && key == Keys.F4)                                 // F4: Xóa tìm kiếm
                {
                    xoaTimKiem_ToolStripMenuItem.PerformClick();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi phím tắt Form46: " + ex.Message);
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        // ===================== BỘ LỌC GHI CHÚ (tạo bằng code, không sửa Designer) =====================
        // Giá trị lấy từ cột GhiChu của chính tệp năm cũ (Form15 "Lưu kết quả thi đua năm" chép Ghi chú từ danh sách gốc lúc lưu).
        // Năm nào không ghi chú (hoặc tệp lưu bằng bản cũ chưa có cột GhiChu) thì combo chỉ có "Tất cả".
        private Label label_GhiChu = null!;
        private ComboBox comboBox_LocGhiChu = null!;
        private void TaoBoLocGhiChu()
        {
            var t5 = tableLayoutPanel5;
            label_GhiChu = new Label
            {
                Name = "label_GhiChu",
                Text = Module_HeThong.TieuDeCot.GhiChu,
                AutoSize = true,
                Anchor = AnchorStyles.Right,
                Font = label4.Font,
                ForeColor = label4.ForeColor,
                Margin = label4.Margin
            };
            comboBox_LocGhiChu = new ComboBox
            {
                Name = "comboBox_LocGhiChu",
                DropDownStyle = ComboBoxStyle.DropDownList,
                FormattingEnabled = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Font = comboBox_TimKiemDonVi.Font,
                Margin = comboBox_TimKiemDonVi.Margin
            };
            comboBox_LocGhiChu.Items.Add(Module_HeThong.Tat_Ca);
            comboBox_LocGhiChu.SelectedIndex = 0;
            t5.SuspendLayout();
            try
            {
                // [... | Phân loại | combo | Ghi chú | combo | nút làm mới]
                t5.ColumnCount = 11;
                while (t5.ColumnStyles.Count < 11) t5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F));
                t5.SetColumn(kryptonButton_LamMoiCacOTimKiem, 10);
                t5.Controls.Add(label_GhiChu, 8, 0);
                t5.Controls.Add(comboBox_LocGhiChu, 9, 0);
            }
            finally { t5.ResumeLayout(false); }
        }
        /// <summary>Ghi chú để so khớp/hiển thị trong combo: gộp xuống dòng, tab, nhiều dấu cách thành 1 dấu cách.</summary>
        private static string ChuanGhiChu(string? gc)
        {
            if (string.IsNullOrEmpty(gc)) return "";
            bool canGop = false;
            for (int i = 0; i < gc.Length; i++)
            {
                char c = gc[i];
                if (c == '\r' || c == '\n' || c == '\t' || (c == ' ' && i + 1 < gc.Length && gc[i + 1] == ' ')) { canGop = true; break; }
            }
            if (!canGop) return gc.Trim();
            return string.Join(' ', gc.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }
        /// <summary>Mở rộng danh sách thả xuống theo ghi chú dài nhất (tối đa 600 px) để đọc được trọn câu.</summary>
        private static void DatRongDanhSach(ComboBox cb)
        {
            int rong = cb.Width;
            foreach (var it in cb.Items)
                rong = Math.Max(rong, TextRenderer.MeasureText(cb.GetItemText(it), cb.Font).Width + SystemInformation.VerticalScrollBarWidth + 8);
            cb.DropDownWidth = Math.Min(rong, (int)Math.Round(600 * cb.DeviceDpi / 96.0));
        }
        private bool _daKhoiTaoToolTip = false;
        private void InitToolTips()
        {
            // Chống gọi lại nhiều lần không cần thiết
            if (_daKhoiTaoToolTip) return;
            // An toàn từ gốc: Kiểm tra ToolTip có tồn tại không
            if (toolTip1 == null) return;
            try
            {
                // ================= CẤU HÌNH CHUNG =================
                toolTip1.IsBalloon = true;
                toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
                toolTip1.ToolTipIcon = ToolTipIcon.Info;
                // UX: Phản hồi nhanh – không gây khó chịu khi rê chuột qua
                toolTip1.InitialDelay = 300;
                toolTip1.AutoPopDelay = 2500;
                toolTip1.ReshowDelay = 100;
                toolTip1.ShowAlways = true;
                // 🌟 TỐI ƯU CẤU TRÚC RAM: Dùng mảng ValueTuple thay cho Dictionary
                // Triệt tiêu chi phí băm (Hashing Overhead) và dọn sạch Heap Allocation.
                (System.Windows.Forms.Control? control, string noiDung)[] danhSachToolTip = new (System.Windows.Forms.Control?, string)[]
                {
                    (kryptonButton_LamMoiCacOTimKiem, "Xóa nội dung tìm kiếm và đặt lại các bộ lọc về trạng thái mặc định"),
                    (kryptonButton_XuatData, "Xuất dữ liệu ra tệp excel (*.xlsx)"),
                    (kryptonButton_CapNhat, "Cập nhật dữ liệu từ CSDL"),
                    (comboBox_LocGhiChu, "Lọc theo Ghi chú đã lưu trong tệp năm cũ (danh sách tự thu hẹp theo các ô lọc khác)")
                };
                // 🌟 XỬ LÝ LỖI PHÂN MẢNH (ISOLATED EXCEPTION)
                int soLoi = 0;
                foreach (var (control, noiDung) in danhSachToolTip)
                {
                    // Bỏ qua an toàn nếu control chưa kịp render hoặc bị null
                    if (control == null)
                    {
                        soLoi++;
                        continue;
                    }
                    try
                    {
                        // Kiểm tra vòng đời của Control trước khi gán API
                        if (control.IsDisposed) continue;
                        toolTip1.SetToolTip(control, noiDung);
                    }
                    catch
                    {
                        // Bẫy lỗi cục bộ: Lỗi ở 1 nút không làm sập vòng lặp gán của các nút khác
                        soLoi++;
                    }
                }
#if DEBUG
                // Hệ thống cảnh báo nội bộ dành riêng cho Lập trình viên (Không hiện ở bản Release)
                if (soLoi > 0)
                    System.Diagnostics.Debug.WriteLine($"[InitToolTips] Hệ thống bỏ qua {soLoi} control do chưa khởi tạo hoặc bị null.");
#endif
                // Đánh dấu hoàn tất để khóa cổng
                _daKhoiTaoToolTip = true;
            }
            catch (Exception ex)
            {
                // Bắt lỗi tổng và in ra Output để Lập trình viên theo dõi
                System.Diagnostics.Debug.WriteLine($"[Lỗi nghiêm trọng tại InitToolTips]: {ex.Message}");
            }
        }
        private void Form46_VisibleChanged(object? sender, EventArgs e)
        {
            // Mỗi khi Form 46 được hiển thị lên màn hình (chuyển tab/chuyển trang)
            if (this.Visible && _isInitialized)
            {
                // Tệp năm cũ + csdl2 không đổi kể từ lần nạp trước (hoặc đang nạp dở) => giữ nguyên lưới, không nạp lại 15.000+ dòng
                if (_dangNapLichSu || string.Equals(_chuKyDaNap, TaoChuKyDuLieu(), StringComparison.Ordinal)) return;
                // ⭐ BƯỚC 1: Bắt buộc nạp lại danh sách File lịch sử.
                // Hàm LoadDanhSachFileLichSu (đã sửa ở trên) sẽ tự động làm ALL:
                // Lọc trùng -> Nạp ComboBox -> Tự động Load dữ liệu lên lưới (nếu có tệp).
                // Không cần dùng PerformClick() nữa để tránh dội luồng (Thread Clash).
                LoadDanhSachFileLichSu();
            }
        }
        private void ComboBoxFilter_SelectedIndexChanged(object? sender, EventArgs e) => ApplyFilter();
        private void textBox_TimKiemTheoTen_TextChanged(object? sender, EventArgs e)
        {
            if (_dangSetPlaceholder) return;
            if (_timKiemTimer == null) return;
            _timKiemTimer.Stop();
            _timKiemTimer.Start(); // Người dùng đang gõ thì reset bộ đếm ngược
        }
        private void TextBox_TimKiemTheoTen_Enter(object? sender, EventArgs e)
        {
            if (textBox_TimKiemTheoTen.Text == PLACEHOLDER_TIMKIEM)
            {
                _dangSetPlaceholder = true;
                textBox_TimKiemTheoTen.Text = "";
                textBox_TimKiemTheoTen.ForeColor = SystemColors.WindowText;
                _dangSetPlaceholder = false;
            }
        }
        private void TextBox_TimKiemTheoTen_Leave(object? sender, EventArgs e)
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
        private void kryptonButton_LamMoiCacOTimKiem_Click(object? sender, EventArgs e)
        {
            _dangSetPlaceholder = true;
            InitPlaceholderTimKiem();
            // Chặn lọc trong lúc đặt lại các combo => chỉ lọc 1 lần ở cuối
            _dangNapComboLoc = true;
            try
            {
                if (comboBox_TimKiemDonVi.Items.Count > 0) comboBox_TimKiemDonVi.SelectedIndex = 0;
                if (comboBox1_TinhTrang.Items.Count > 0) comboBox1_TinhTrang.SelectedIndex = 0;
                if (comboBox1_PhanLoaiThiDuaNamCu.Items.Count > 0) comboBox1_PhanLoaiThiDuaNamCu.SelectedIndex = 0;
                if (comboBox_LocGhiChu.Items.Count > 0) comboBox_LocGhiChu.SelectedIndex = 0;
            }
            finally { _dangNapComboLoc = false; }
            _dangSetPlaceholder = false;
            ApplyFilter();
        }
        public void LoadDanhSachFileLichSu()
        {
            // 1. Ngắt sự kiện tạm thời để tránh lặp luồng dồn dập khi gán DataSource
            comboBox_ChonCSDLNam.SelectedIndexChanged -= comboBox_ChonCSDLNam_SelectedIndexChanged;
            // ⭐ CHUẨN KỸ SƯ: Không dùng hàm quét chung (*.db) nữa vì nó sẽ hốt luôn cả file Khen Thưởng, 
            // gây ra hiện tượng trùng tên và văng lỗi ngầm (vì file Khen thưởng không có bảng Thi Đua).
            string dir = Module_DanduongGPS.ThuMucLichSuThiDua;
            Module_HoTroLuuDataTheoNamCu.DonRacThuMucLichSu();   // tệp năm cũ đã trống cả bảng cá nhân lẫn tập thể => xóa, không liệt kê
            var danhSachSach = new List<FileLichSuDTO>();
            if (Directory.Exists(dir))
            {
                // Tự động nhận diện phiên bản phần mềm để quét đúng tệp
                string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
                var cheDo = phienBan.LayCheDo();
                bool laTanBinh = Module_CheDoDoiTuong.LaNhomTuan(cheDo);
                // ⭐ CHỈ QUÉT ĐÚNG CÁC TỆP THI ĐUA CỦA CHẾ ĐỘ ĐANG CHẠY (ThiDua_CBCS_ / ThiDua_TanBinh_ / ThiDua_HocVien_)
                string pattern = $"ThiDua_{Module_CheDoDoiTuong.MaTep(cheDo)}_Nam*.db";
                var files = Directory.GetFiles(dir, pattern);
                string bangCaNhan = Module_CheDoDoiTuong.ThiDuaThang(cheDo);
                foreach (var file in files)
                {
                    // Tệp năm cũ chứa 2 bảng độc lập (cá nhân + tập thể). Bảng cá nhân đã bị xóa/trống thì năm đó không hiện ở đây
                    // (vẫn còn ở Form59 nếu bảng tập thể còn dữ liệu).
                    if (!Module_BoLocKhenThuong.CoDuLieuBang(file, bangCaNhan)) continue;
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string nam = fileName.Split(new string[] { "Nam" }, StringSplitOptions.None).LastOrDefault() ?? "???";
                    danhSachSach.Add(new FileLichSuDTO
                    {
                        TenHienThi = $"Năm {nam} - {(laTanBinh ? Module_TaiKhoan.TenTB_Hoa : "CBCS")}",
                        DuongDan = file,
                        LaTanBinh = laTanBinh,
                        CheDo = cheDo
                    });
                }
                // Sắp xếp năm mới nhất lên đầu
                danhSachSach = danhSachSach.OrderByDescending(x => x.TenHienThi).ToList();
            }
            // 3. Gán dữ liệu vào ComboBox
            comboBox_ChonCSDLNam.DataSource = danhSachSach;
            comboBox_ChonCSDLNam.DisplayMember = "TenHienThi";
            comboBox_ChonCSDLNam.ValueMember = "DuongDan";
            // 4. Khôi phục lại sự kiện
            comboBox_ChonCSDLNam.SelectedIndexChanged += comboBox_ChonCSDLNam_SelectedIndexChanged;
            // 5. XỬ LÝ GIAO DIỆN & ÉP NẠP DỮ LIỆU
            if (danhSachSach.Count == 0)
            {
                _theHeNap++;              // bỏ kết quả của lượt nạp nền còn đang chạy (nếu có)
                _dangNapLichSu = false;
                this.Cursor = Cursors.Default;
                _chuKyDaNap = TaoChuKyDuLieu();
                kryptonDataGridView1.RowCount = 0;
                kryptonDataGridView1.DataSource = null;
                kryptonDataGridView1.Columns.Clear();
                _filteredIndexes.Clear();
                _dataCacheCBCS.Clear();
                _dataCacheTanBinh.Clear();
                _dtHienTai = null;
                CapNhatTrangThaiHienThi();
                CapNhatHienThiGridTheoSoDong(0, "Không tìm thấy tệp CSDL lưu trữ thi đua năm cũ.");
            }
            else
            {
                // ⭐ TỰ ĐỘNG LOAD: Khi gán DataSource, WinForms đã tự chọn Index = 0.
                // Vì ta quét đúng file ThiDua nên không bao giờ lỗi nữa. Kích hoạt nạp dữ liệu lưới ngay lập tức!
                ThucHienTaiDuLieuLichSu();
            }
            // Cập nhật lại tiêu đề Form cho đồng bộ
            //CapNhatTieuDeTheoNamDuocChon();
        }
        private void KhoiTaoBoLocComboBox(bool laTanBinh)
        {
            // Chặn lọc trong lúc nạp combo bằng cờ (gỡ/gắn sự kiện từng làm sự kiện bị gắn trùng => lọc 2 lần)
            _dangNapComboLoc = true;
            try
            {
                // 1. Nạp và sắp xếp danh sách Đơn vị từ Module_DonVi theo trạng thái bình thường
                // Nạp bằng Items (không dùng DataSource) để BỘ LỌC KÉP thu hẹp được danh sách theo các ô lọc khác
                var dsDonVi = (Module_DonVi.GetDanhSachDonVi() ?? new List<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x) && x != Module_HeThong.Tat_Ca).Select(x => x.Trim()).ToList();
                _dsDonViLoc = dsDonVi;
                var cbDv = comboBox_TimKiemDonVi;
                cbDv.BeginUpdate();
                try
                {
                    if (cbDv.DataSource != null) cbDv.DataSource = null;
                    cbDv.Items.Clear();
                    cbDv.Items.Add(Module_HeThong.Tat_Ca);
                    foreach (var dv in dsDonVi) cbDv.Items.Add(dv);
                    cbDv.SelectedIndex = 0;
                }
                finally { cbDv.EndUpdate(); }
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
                    comboBox1_PhanLoaiThiDuaNamCu!.Items.Clear();
                    comboBox1_PhanLoaiThiDuaNamCu.Items.AddRange(Module_HeThong.DanhSach_PhanLoai_TatCa);
                    if (comboBox1_PhanLoaiThiDuaNamCu.Items.Count > 0) comboBox1_PhanLoaiThiDuaNamCu.SelectedIndex = 0;
                }
                if (comboBox1_TinhTrang.Items.Count > 0) comboBox1_TinhTrang.SelectedIndex = 0;
                comboBox_LocGhiChu.BeginUpdate();
                try
                {
                    comboBox_LocGhiChu.Items.Clear();
                    comboBox_LocGhiChu.Items.Add(Module_HeThong.Tat_Ca);
                    comboBox_LocGhiChu.SelectedIndex = 0;
                }
                finally { comboBox_LocGhiChu.EndUpdate(); }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi khởi tạo danh mục bộ lọc ComboBox: " + ex.Message);
            }
            finally
            {
                _dangNapComboLoc = false;
            }
        }
        private void comboBox_ChonCSDLNam_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!this.IsHandleCreated) return;
            ThucHienTaiDuLieuLichSu();
            // ⭐ BỔ SUNG VÀO ĐÂY: Cập nhật tiêu đề khi đổi năm
            //CapNhatTieuDeTheoNamDuocChon();
        }
        // ⭐ TRÁI TIM BỘ LỌC ĐA TẦNG (cùng mẫu Form15)
        private List<string> _dsDonViLoc = new();
        private bool _dangNapComboLoc;                 // đang thu hẹp combo Đơn vị bằng code -> không bắn lọc lại
        private string[]? _timKiemChuan;               // chế độ thường: chuỗi tìm kiếm không dấu theo từng dòng của DefaultView
        private DataTable? _timKiemChuanBang;
        private string? _timKiemChuanSort;
        private object? _nguonLocTruoc;                // nguồn dữ liệu của lần lọc trước (đổi năm / nạp lại => luôn vẽ lại lưới)
        /// <summary>Chuỗi "họ tên|số hiệu" đã bỏ dấu cho từng dòng (theo thứ tự DefaultView), dựng 1 lần cho mỗi bảng.</summary>
        private string[] LayTimKiemChuan(DataView view)
        {
            var dt = view.Table;
            if (_timKiemChuan != null && ReferenceEquals(_timKiemChuanBang, dt) && _timKiemChuan.Length == view.Count
                && string.Equals(_timKiemChuanSort, view.Sort, StringComparison.Ordinal)) return _timKiemChuan;
            int oTen = dt!.Columns.IndexOf("HoVaTen"), oSh = dt.Columns.IndexOf("SoHieu");
            var kq = new string[view.Count];
            for (int i = 0; i < kq.Length; i++)
            {
                var r = view[i].Row;
                string ten = oTen >= 0 ? BoLocDanhSach.ChuanHoaTimKiem(r[oTen]?.ToString()) : "";
                kq[i] = oSh >= 0 ? ten + "|" + BoLocDanhSach.ChuanHoaTimKiem(r[oSh]?.ToString()) : ten;
            }
            _timKiemChuan = kq; _timKiemChuanBang = dt; _timKiemChuanSort = view.Sort;
            return kq;
        }
        private void ApplyFilter()
        {
            if (_dangNapComboLoc) return;
            _timKiemTimer?.Stop();   // hủy lượt gõ đang chờ -> không lọc 2 lần
            if (_dtHienTai == null)
            {
                kryptonDataGridView1.RowCount = 0;
                CapNhatHienThiGridTheoSoDong(0, "Chưa có dữ liệu bảng thống kê năm cũ.");
                return;
            }
            string dvFilter = comboBox_TimKiemDonVi.Text?.Trim() ?? "";
            string tenFilter = textBox_TimKiemTheoTen.Text?.Trim() ?? "";
            string tinhTrangFilter = comboBox1_TinhTrang.Text?.Trim() ?? "";
            string plFilter = comboBox1_PhanLoaiThiDuaNamCu.Text?.Trim() ?? "";
            if (_dangSetPlaceholder || string.Equals(tenFilter, PLACEHOLDER_TIMKIEM, StringComparison.OrdinalIgnoreCase)) tenFilter = "";
            // Tên / số hiệu KHÔNG phân biệt dấu, hoa/thường; nhiều từ thì từ nào cũng phải có ("nguyen van" khớp "Nguyễn Văn An")
            string[] tu = Module_BoLocKhenThuong.TachTuKhoa(tenFilter);
            var grid = kryptonDataGridView1;
            try
            {
                var ketQua = new List<int>();
                var conDonVi = new HashSet<string>(Module_DonVi.SoSanhBang);   // BỘ LỌC KÉP cho combo Đơn vị
                var conGhiChu = new HashSet<string>(StringComparer.OrdinalIgnoreCase);  // BỘ LỌC KÉP cho combo Ghi chú (không trùng, bỏ rỗng)
                string gcFilter = Module_BoLocKhenThuong.GiaTriLoc(comboBox_LocGhiChu);
                bool hasGC = gcFilter.Length > 0;
                bool hasTen = tu.Length > 0;
                bool hasDV = !string.IsNullOrWhiteSpace(dvFilter) && dvFilter != Module_HeThong.Tat_Ca;
                bool hasTT = !string.IsNullOrWhiteSpace(tinhTrangFilter) && tinhTrangFilter != Module_HeThong.Tat_Ca;
                bool hasPL = !string.IsNullOrWhiteSpace(plFilter) && plFilter != Module_HeThong.Tat_Ca;
                bool laTanBinh = _laTanBinhHienTai;
                string khoaDvLoc = hasDV ? Module_DonVi.GiaTriSoKhop(dvFilter) : "";
                // Dòng đã qua tên + tình trạng + phân loại. Combo Đơn vị nhận đơn vị của dòng khớp Ghi chú; combo Ghi chú nhận
                // ghi chú của dòng khớp Đơn vị; khớp cả hai thì vào kết quả (=> không chọn ra được tổ hợp rỗng).
                void Xet(int i, string donVi, string ghiChu)
                {
                    string dv = (donVi ?? "").Trim();
                    string gc = ChuanGhiChu(ghiChu);
                    bool dvOk = !hasDV || string.Equals(Module_DonVi.GiaTriSoKhop(dv), khoaDvLoc, StringComparison.Ordinal);
                    bool gcOk = !hasGC || string.Equals(gc, gcFilter, StringComparison.OrdinalIgnoreCase);
                    if (gcOk && dv.Length > 0) conDonVi.Add(dv);
                    if (dvOk && gc.Length > 0) conGhiChu.Add(gc);
                    if (dvOk && gcOk) ketQua.Add(i);
                }
                if (_isDataMaxMode)
                {
                    if (laTanBinh)
                    {
                        var ds = _dataCacheTanBinh;
                        ketQua.Capacity = ds.Count;
                        for (int i = 0; i < ds.Count; i++)
                        {
                            var item = ds[i];
                            if (hasTT && !string.Equals(item.TinhTrang, tinhTrangFilter, StringComparison.OrdinalIgnoreCase)) continue;
                            if (hasTen && !Module_BoLocKhenThuong.KhopTuKhoa(item.HoVaTen_Search ?? "", tu)) continue;
                            Xet(i, item.DonVi, item.GhiChu);
                        }
                    }
                    else
                    {
                        var ds = _dataCacheCBCS;
                        ketQua.Capacity = ds.Count;
                        for (int i = 0; i < ds.Count; i++)
                        {
                            var item = ds[i];
                            if (hasTT && !string.Equals(item.TinhTrang, tinhTrangFilter, StringComparison.OrdinalIgnoreCase)) continue;
                            if (hasPL && !string.Equals(item.TongKet_Nam, plFilter, StringComparison.OrdinalIgnoreCase)) continue;
                            if (hasTen && !Module_BoLocKhenThuong.KhopTuKhoa(item.HoVaTen_Search ?? "", tu)) continue;
                            Xet(i, item.DonVi, item.GhiChu);
                        }
                    }
                }
                else
                {
                    // Chế độ Standard: Lọc trên DefaultView của DataTable phát sinh cũ
                    bool hasTongKetColumn = _dtHienTai.Columns.Contains("TongKet_Nam");
                    int oDv = _dtHienTai.Columns.IndexOf("DonVi"), oTT = _dtHienTai.Columns.IndexOf("TinhTrang");
                    int oTK = _dtHienTai.Columns.IndexOf("TongKet_Nam"), oGc = _dtHienTai.Columns.IndexOf("GhiChu");
                    DataView view = _dtHienTai.DefaultView;
                    string[] tim = hasTen ? LayTimKiemChuan(view) : Array.Empty<string>();
                    int count = view.Count;
                    ketQua.Capacity = count;
                    for (int i = 0; i < count; i++)
                    {
                        DataRow r = view[i].Row;
                        if (hasTT && (oTT < 0 || !string.Equals((r[oTT]?.ToString() ?? "").Trim(), tinhTrangFilter, StringComparison.OrdinalIgnoreCase))) continue;
                        if (hasPL && hasTongKetColumn && !string.Equals((r[oTK]?.ToString() ?? "").Trim(), plFilter, StringComparison.OrdinalIgnoreCase)) continue;
                        if (hasTen && !Module_BoLocKhenThuong.KhopTuKhoa(tim[i], tu)) continue;
                        Xet(i, oDv >= 0 ? r[oDv]?.ToString() ?? "" : "", oGc >= 0 ? r[oGc]?.ToString() ?? "" : "");
                    }
                }
                // Combo Đơn vị chỉ còn đơn vị có người thỏa các ô lọc khác (giữ thứ tự danh mục + giá trị đang chọn)
                _dangNapComboLoc = true;
                try
                {
                    if (comboBox_TimKiemDonVi.DataSource == null && comboBox_TimKiemDonVi.Items.Count > 0)
                        Module_BoLocKhenThuong.NapLaiCombo(comboBox_TimKiemDonVi,
                            Module_BoLocKhenThuong.SapTheoThuTuChuan(conDonVi.Where(x => _dsDonViLoc.Contains(x, Module_DonVi.SoSanhBang)), _dsDonViLoc),
                            Module_HeThong.Tat_Ca);
                    if (Module_BoLocKhenThuong.NapLaiCombo(comboBox_LocGhiChu, Module_BoLocKhenThuong.SapABC(conGhiChu), Module_HeThong.Tat_Ca))
                        DatRongDanhSach(comboBox_LocGhiChu);
                }
                finally { _dangNapComboLoc = false; }
                // Cùng đúng tập dòng như đang hiển thị -> không đụng tới lưới (giữ dòng đang chọn + vị trí cuộn).
                // Danh sách MỚI (không Clear danh sách cũ): luồng xuất Excel có thể đang đọc danh sách cũ.
                object nguon = _isDataMaxMode ? (laTanBinh ? _dataCacheTanBinh : _dataCacheCBCS) : _dtHienTai;
                bool giongHet = ReferenceEquals(nguon, _nguonLocTruoc) && grid.RowCount == ketQua.Count
                                && Module_BoLocKhenThuong.CungChiSo(_filteredIndexes, ketQua);
                _filteredIndexes = ketQua;
                _nguonLocTruoc = nguon;
                if (!giongHet)
                {
                    // Tắt vẽ + đổi số dòng nhanh (giảm RowCount từng dòng là nguyên nhân khựng khi dữ liệu nhiều)
                    using var _ = Module_LuoiAo.TamDungVe(grid);
                    Module_LuoiAo.DatSoDong(grid, ketQua.Count, boChon: true);
                    grid.Invalidate();
                }
                string thongBaoRong = (hasTen || hasDV || hasTT || hasPL || hasGC)
                    ? "Không tìm thấy cán bộ, chiến sĩ nào phù hợp với điều kiện lọc."
                    : "Tệp CSDL năm đã chọn chưa có dữ liệu thống kê.";
                CapNhatHienThiGridTheoSoDong(_filteredIndexes.Count, thongBaoRong);
                CapNhatTrangThaiHienThi(); // ⭐ Tự động cập nhật linh hoạt theo trạng thái CSDL
            }
            catch (Exception ex) { Debug.WriteLine("Lỗi ApplyFilter RAM: " + ex.Message); }
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
                ? $"Tổng cộng: {_filteredIndexes.Count} {Module_HeThong.Tu_dong_chi}  [Chế độ hiệu năng cao]"
                : $"Tổng cộng: {_filteredIndexes.Count} {Module_HeThong.Tu_dong_chi}";
        }
        private void KryptonDataGridView1_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            if (_filteredIndexes == null || e.RowIndex < 0 || e.RowIndex >= _filteredIndexes.Count) return;
            try
            {
                int actualViewIndex = _filteredIndexes[e.RowIndex];
                if (_isDataMaxMode)
                {
                    string colName = kryptonDataGridView1.Columns[e.ColumnIndex].Name;
                    if (_laTanBinhHienTai)
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
                                if (data.DuLieuThang.TryGetValue(colName, out string? valT)) e.Value = valT;
                                else if (data.CotPhatSinh != null && data.CotPhatSinh.TryGetValue(colName, out string? valP)) e.Value = valP;
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
                                else if (data.CotPhatSinh != null && data.CotPhatSinh.TryGetValue(colName, out string? valP)) e.Value = valP;
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
        private void KryptonDataGridView1_RowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
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
            // Nhóm tuần: cột thời gian theo lịch xét (Module_LichXet); tệp năm cũ lược đồ cũ vẫn hiện đúng
            var cheDoLich = Module_CheDoDoiTuong.Hien;
            string[] cotThoiGian = laTanBinh
                ? Module_LichXet.CotDangDungTrong(cheDoLich, grid.Columns.Cast<DataGridViewColumn>().Select(c => c.Name).ToList())
                : Array.Empty<string>();
            int namHeThong = Module_HeThong.LayNamHeThong();
            int namCu = namHeThong - 1;
            var headerMap = new Dictionary<string, string>
            {
                ["HoVaTen"] = Module_HeThong.TieuDeCot.HoVaTen,
                ["SoHieu"] = Module_HeThong.TieuDeCot.SoHieu,
                ["DonVi"] = Module_HeThong.TieuDeCot.DonVi,
                ["TinhTrang"] = "Tình trạng",
                ["GhiChu"] = Module_HeThong.TieuDeCot.GhiChu
            };
            if (laTanBinh)
            {
                foreach (string cot in cotThoiGian) headerMap[cot] = Module_LichXet.NhanCot(cheDoLich, cot) ?? cot;
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
            headerMap["TS_Loai1"] = $"Tổng\n{Module_HeThong.Loai_1}";
            headerMap["TS_Loai2"] = $"Tổng\n{Module_HeThong.Loai_2}";
            headerMap["TS_Loai3"] = $"Tổng\n{Module_HeThong.Loai_3}";
            headerMap["TS_Loai4"] = $"Tổng\n{Module_HeThong.Loai_4}";
            foreach (var kv in headerMap)
                if (grid.Columns.Contains(kv.Key)) grid.Columns[kv.Key].HeaderText = kv.Value;
            if (grid.Columns.Contains("ID")) grid.Columns["ID"].Visible = false;
            if (laTanBinh)
            {
                foreach (DataGridViewColumn c in grid.Columns)
                    if (Module_LichXet.LaCotThoiGian(c.Name) && !cotThoiGian.Contains(c.Name)) c.Visible = false;
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
                Font = _fontGridHeader95Bold,
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(240, 244, 248),
                ForeColor = Color.FromArgb(40, 40, 40),
                WrapMode = DataGridViewTriState.True,
                Padding = new Padding(3)
            };
            grid.RowTemplate.Height = 32;
            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = _fontGridCell95,
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
                // đợt lẻ (5 cột mỗi đợt: 4 tuần + kết quả) tô nền xanh nhạt
                string[] nhomToMauXanhDuong = cotThoiGian.Where((_, i) => (i / 5) % 2 == 0).ToArray();
                foreach (var name in nhomToMauXanhDuong)
                    if (grid.Columns.Contains(name)) grid.Columns[name].DefaultCellStyle.BackColor = Color.FromArgb(235, 245, 255);
                string[] cacCotKetQua = cotThoiGian.Where((_, i) => i % 5 == 4).ToArray();
                foreach (var name in cacCotKetQua)
                {
                    if (grid.Columns.Contains(name))
                    {
                        grid.Columns[name].DefaultCellStyle.Font = _fontGridHeader95Bold;
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
                    grid.Columns[name].DefaultCellStyle.Font = _fontGridHeader95Bold;
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
                foreach (string cot in cotThoiGian) XepCot(cot);
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
            //// ⭐ Dọn dẹp Form54 cache khi Form mẹ đóng hoàn toàn
        private async void kryptonButton_CapNhat_Click(object? sender, EventArgs e)
        {
            // ⭐ ĐỌC DỮ LIỆU TỪ GIAO DIỆN (UI THREAD) TRƯỚC KHI VÀO TASK.RUN
            var selectedFile = comboBox_ChonCSDLNam.SelectedItem as FileLichSuDTO;
            // 1. LỚP VỎ UX: LƯU TRẠNG THÁI GỐC ĐỂ PHỤC HỒI SAU KHI XONG
            string textBanDau = kryptonButton_CapNhat.Values.Text;
            Image? anhBanDau = kryptonButton_CapNhat.Values.Image;
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
                    string pathCsdl2 = _csdl2Path;
                    string pathCsdlNamCu = selectedFile.DuongDan;
                    bool laTanBinh = selectedFile.LaTanBinh;
                    // Chạy ngầm an toàn vì các biến pathCsdl2, pathCsdlNamCu là biến chữ (string) độc lập
                    await Task.Run(() =>
                    {
                        Module_HoTroLuuDataTheoNamCu.CapNhatTinhTrangThiDuaNamCu(pathCsdl2, pathCsdlNamCu, laTanBinh);
                    });
                    Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Đối chiếu tình trạng công tác dữ liệu năm cũ",
                        $"Tệp: {Path.GetFileName(pathCsdlNamCu)}");
                }
                // Nạp lại dữ liệu hiển thị lên lưới (chờ nạp nền xong)
                await ThucHienTaiDuLieuLichSuAsync();
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
        private async void kryptonButton_XuatData_Click(object? sender, EventArgs e)
        {
            // 1. LỚP VỎ UX: LƯU TRẠNG THÁI GỐC CHỐNG CLICK TRÙNG LUỒNG
            string textBanDau = kryptonButton_XuatData.Values.Text;
            Image? anhBanDau = kryptonButton_XuatData.Values.Image;
            if (_dangNapLichSu)
            {
                MessageBox.Show("Dữ liệu năm cũ đang được tải, vui lòng đợi trong giây lát rồi xuất lại.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            // Chốt chặn kiểm tra dữ liệu hiển thị
            if (kryptonDataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu trên lưới để xuất tệp!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (comboBox_ChonCSDLNam.SelectedItem == null) return;
            var selectedFile = (FileLichSuDTO)comboBox_ChonCSDLNam.SelectedItem;
            // ⭐ XỬ LÝ TÊN TỆP ĐỘNG THEO YÊU CẦU
            string loai = Module_CheDoDoiTuong.MaTep(selectedFile.CheDo);
            // Trích xuất năm từ chuỗi "Năm 2026 - CBCS" -> lấy số 2026
            string[] parts = selectedFile.TenHienThi.Split(' ');
            string nam = (parts.Length > 1) ? parts[1] : DateTime.Now.Year.ToString();
            string fileName = $"ThongKeThiDua_{loai}_Nam{nam}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            // 2. KHỞI TẠO HỘP THOẠI LƯU TỆP EXCEL
            using var sfd = new SaveFileDialog
            {
                Title = "Chọn nơi lưu file Excel thống kê",
                Filter = Module_HeThong.BoLoc_Excel,
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
                // 3. ĐỌC TIÊN QUYẾT TIÊU CHÍ CỘT THỰC TẾ TRONG FILE SQLITE
                var cotTrongBang = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string tenBang = selectedFile.BangThiDua;
                using (var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(selectedFile.DuongDan)))
                {
                    cn.Open();
                    using var cmd = new SqliteCommand($"PRAGMA table_info([{tenBang}])", cn);
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        string cot = rd["name"].ToString()!;
                        if (!cot.Equals("ID", StringComparison.OrdinalIgnoreCase))
                            cotTrongBang.Add(cot);
                    }
                }
                // 4. TRÍCH XUẤT SIÊU DỮ LIỆU CỘT
                var columnsMeta = kryptonDataGridView1.Columns
                    .Cast<DataGridViewColumn>()
                    .Where(c => cotTrongBang.Contains(c.Name))
                    .Where(c =>
                    {
                        // cột thời gian ngoài lịch xét đã bị ẩn trên lưới => không xuất
                        if (selectedFile.LaTanBinh && Module_LichXet.LaCotThoiGian(c.Name) && !c.Visible) return false;
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
                // 5. KÍCH HOẠT TIẾN TRÌNH LUỒNG NỀN
                await Task.Run(() => Module_HoTroLuuDataTheoNamCu.XuatExcelLichSuCore(targetExcelPath, selectedFile.LaTanBinh, _isDataMaxMode, columnsMeta, _filteredIndexes, _dtHienTai!, _dataCacheCBCS, _dataCacheTanBinh, tenTieuDoan));
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
        private static string SafeDecrypt(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            try
            {
                string result = Module_BaoMatAES.GiaiMa(input);
                return string.IsNullOrEmpty(result) ? input : result;
            }
            catch { return input; }
        }
        private string XacDinhTenTieuDoan()
        {
            try
            {
                using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl2Path));
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT TenTieuDoan FROM ThongTin WHERE ID=1";
                var result = cmd.ExecuteScalar();
                return (result != null) ? SafeDecrypt(result.ToString()!.Trim()) : "Không xác định";
            }
            catch { return "Không xác định"; }
        }
        private void lamMoiTrangToolStripMenuItem_Click(object sender, EventArgs e)
       => kryptonButton_CapNhat.PerformClick();
        // ===================== NẠP DỮ LIỆU NĂM CŨ (CHẠY NỀN) =====================
        // Toàn bộ phần nặng (đồng bộ tình trạng, đọc SQLite, giải mã, sắp xếp) chạy ở luồng nền => giao diện không đơ.
        // Chỉ phần dựng cột + lọc chạy ở luồng giao diện. Lượt nạp cũ bị bỏ nếu người dùng đã đổi năm (_theHeNap).
        private sealed class GoiLichSu
        {
            public bool LaTanBinh, MaxMode;
            public DataTable Bang = null!;
            public List<HistoryCBCSDTO> CBCS = new();
            public List<HistoryTanBinhDTO> TanBinh = new();
            public List<string> CotPhatSinh = new();
        }
        private int _theHeNap;                 // tăng mỗi lượt nạp; kết quả của lượt cũ hơn bị bỏ
        private bool _dangNapLichSu;           // đang nạp nền: chặn Xuất Excel + chặn nạp lại khi form hiện lại
        private bool _laTanBinhHienTai;        // loại dữ liệu ĐANG nằm trên lưới (không đọc SelectedItem cho từng ô)
        private string? _chuKyDaNap;           // "chữ ký" tệp đã nạp: form hiện lại mà không đổi gì thì không nạp lại
        private static readonly string[] BaseColsCBCS = { "ID", "HoVaTen", "SoHieu", "DonVi", "TinhTrang", "KQ_ThiDua_Nam_Cu", "KQ_XepLoaiCB_Nam_Cu", "KQ_XepLoaiDangVien_Nam_Cu", "Thang_12_Nam_Cu", "Thang_1", "Thang_2", "Thang_3", "Thang_4", "Thang_5", "Sau_Thang_Dau_Nam", "Thang_6", "Thang_7", "Thang_8", "Thang_9", "Thang_10", "Thang_11", "TongKet_Nam", "TS_Loai1", "TS_Loai2", "TS_Loai3", "TS_Loai4" };
        // Nhóm tuần: cột chung + mọi cột thời gian (lược đồ 6 đợt và lược đồ cũ)
        private static readonly string[] BaseColsTB = new[] { "ID", "HoVaTen", "SoHieu", "DonVi", "TinhTrang", "TS_Loai1", "TS_Loai2", "TS_Loai3", "TS_Loai4" }
            .Concat(Module_LichXet.CotVatLy(CheDoDoiTuong.TanBinh)).Concat(Module_LichXet.CotVatLyCu).ToArray();
        /// <summary>Tên + giờ sửa + kích thước của các tệp năm cũ và csdl2 (tình trạng công tác lấy từ csdl2).</summary>
        private static string TaoChuKyDuLieu()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                void Them(string f)
                {
                    foreach (var p in new[] { f, f + "-wal" })
                    {
                        var fi = new FileInfo(p);
                        if (fi.Exists) sb.Append(p).Append('|').Append(fi.LastWriteTimeUtc.Ticks).Append('|').Append(fi.Length).Append(';');
                    }
                }
                string dir = Module_DanduongGPS.ThuMucLichSuThiDua;
                if (Directory.Exists(dir))
                    foreach (var f in Directory.GetFiles(dir, "ThiDua_*_Nam*.db").OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) Them(f);
                Them(Module_DanduongGPS.DuongDanCSDL2);
                sb.Append(Module_TaiKhoan.LayPhienBanPhanMem());
                return sb.ToString();
            }
            catch { return Guid.NewGuid().ToString(); }   // không đọc được => coi như đã đổi
        }
        /// <summary>Đọc + giải mã cả tệp năm cũ. Chạy ở luồng nền: KHÔNG đụng tới control nào.</summary>
        private static GoiLichSu DocGoiLichSu(string duongDan, bool laTanBinh, string csdl2Path)
        {
            // Đồng bộ tình trạng (Đang/Chuyển công tác) theo danh sách gốc trước khi đọc
            Module_HoTroLuuDataTheoNamCu.CapNhatTinhTrangLichSuTuDanhSachGoc(csdl2Path, duongDan, laTanBinh);
            string tenBang = Module_CheDoDoiTuong.ThiDuaThang(Module_CheDoDoiTuong.TuTenTep(duongDan));   // bảng theo chính tệp năm cũ
            var goi = new GoiLichSu { LaTanBinh = laTanBinh };
            int tongSo;
            using (var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(duongDan, chiDoc: true)))
            {
                cn.Open();
                using var cmd = new SqliteCommand($"SELECT COUNT(ID) FROM [{tenBang}]", cn);
                tongSo = Convert.ToInt32(cmd.ExecuteScalar());
            }
            goi.MaxMode = tongSo > NGUONG_DU_LIEU;
            if (!goi.MaxMode)
            {
                // Chế độ Standard (<= 3000 dòng): giữ nguyên cơ chế DataTable gốc
                goi.Bang = Module_HoTroLuuDataTheoNamCu.LoadDataFromHistoryDB(duongDan, tenBang);
                return goi;
            }
            // ---- Chế độ hiệu năng cao: đọc thô (chuỗi còn mã hóa) theo chỉ số cột, rồi giải mã song song ----
            var schemaDt = new DataTable();
            var raw = new List<string[]>(tongSo);
            int soCot;
            using (var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(duongDan, chiDoc: true)))
            {
                cn.Open();
                using var cmd = new SqliteCommand($"SELECT * FROM [{tenBang}]", cn);
                using var rd = cmd.ExecuteReader();
                soCot = rd.FieldCount;
                for (int i = 0; i < soCot; i++) schemaDt.Columns.Add(rd.GetName(i), typeof(string));
                while (rd.Read())
                {
                    var dong = new string[soCot];
                    for (int i = 0; i < soCot; i++) dong[i] = rd.IsDBNull(i) ? "" : rd.GetValue(i)?.ToString() ?? "";
                    raw.Add(dong);
                }
            }
            if (!schemaDt.Columns.Contains("GhiChu")) schemaDt.Columns.Add("GhiChu", typeof(string));
            var baseCols = new HashSet<string>(laTanBinh ? BaseColsTB : BaseColsCBCS, StringComparer.Ordinal);
            foreach (DataColumn col in schemaDt.Columns)
                if (!baseCols.Contains(col.ColumnName) && col.ColumnName != "GhiChu") goi.CotPhatSinh.Add(col.ColumnName);
            int O(string ten) { int i = schemaDt.Columns.IndexOf(ten); return i < soCot ? i : -1; }   // -1: tệp không có cột này
            string V(string[] d, int o) => o >= 0 ? d[o] : "";
            int oId = O("ID"), oTen = O("HoVaTen"), oSh = O("SoHieu"), oDv = O("DonVi"), oTT = O("TinhTrang"), oGc = O("GhiChu");
            int oL1 = O("TS_Loai1"), oL2 = O("TS_Loai2"), oL3 = O("TS_Loai3"), oL4 = O("TS_Loai4");
            var oPhatSinh = goi.CotPhatSinh.Select(c => (c, O(c))).ToArray();
            string[] donViUuTien = Module_DonVi.LayDanhSachDonViUuTienArray();
            var dicDonViUuTien = new Dictionary<string, int>(Module_DonVi.SoSanhBang);
            for (int i = 0; i < donViUuTien.Length; i++) dicDonViUuTien.TryAdd(donViUuTien[i], i);
            int n = raw.Count;
            if (laTanBinh)
            {
                // Cột tuần/tháng của Tân binh: tính chỉ số 1 lần
                var oThang = new List<(string ten, int o)>();
                // mọi cột thời gian có thể có trong tệp năm cũ: lược đồ 6 đợt và lược đồ cũ (Tuan_x_T2..)
                foreach (string c in Module_LichXet.CotVatLy(CheDoDoiTuong.TanBinh).Concat(Module_LichXet.CotVatLyCu))
                {
                    int o = O(c);
                    if (o >= 0) oThang.Add((c, o));
                }
                var mang = new HistoryTanBinhDTO[n];
                Parallel.For(0, n, i =>
                {
                    var d = raw[i];
                    var item = new HistoryTanBinhDTO
                    {
                        ID = V(d, oId),
                        HoVaTen = SafeDecrypt(V(d, oTen)),
                        SoHieu = SafeDecrypt(V(d, oSh)),
                        DonVi = SafeDecrypt(V(d, oDv)),
                        TinhTrang = V(d, oTT),
                        TS_Loai1 = V(d, oL1), TS_Loai2 = V(d, oL2), TS_Loai3 = V(d, oL3), TS_Loai4 = V(d, oL4),
                        GhiChu = oGc >= 0 ? SafeDecrypt(d[oGc]) : ""
                    };
                    item.HoVaTen_Search = BoLocDanhSach.ChuanHoaTimKiem(item.HoVaTen) + "|" + BoLocDanhSach.ChuanHoaTimKiem(item.SoHieu);
                    item.SortPriority = dicDonViUuTien.TryGetValue(item.DonVi, out int p) ? p : int.MaxValue;
                    foreach (var (ten, o) in oThang) item.DuLieuThang[ten] = d[o];
                    foreach (var (ten, o) in oPhatSinh) item.CotPhatSinh[ten] = o >= 0 ? d[o] : "";
                    mang[i] = item;
                });
                goi.TanBinh = mang.OrderBy(x => x.SortPriority).ThenBy(x => int.TryParse(x.ID, out int id) ? id : int.MaxValue).ToList();
            }
            else
            {
                int oTD = O("KQ_ThiDua_Nam_Cu"), oXLCB = O("KQ_XepLoaiCB_Nam_Cu"), oXLDV = O("KQ_XepLoaiDangVien_Nam_Cu");
                int oT12 = O("Thang_12_Nam_Cu"), o6T = O("Sau_Thang_Dau_Nam"), oTK = O("TongKet_Nam");
                var oT = new int[11];
                for (int t = 1; t <= 11; t++) oT[t - 1] = O($"Thang_{t}");
                var mang = new HistoryCBCSDTO[n];
                Parallel.For(0, n, i =>
                {
                    var d = raw[i];
                    var item = new HistoryCBCSDTO
                    {
                        ID = V(d, oId),
                        HoVaTen = SafeDecrypt(V(d, oTen)),
                        SoHieu = SafeDecrypt(V(d, oSh)),
                        DonVi = SafeDecrypt(V(d, oDv)),
                        TinhTrang = V(d, oTT),
                        KQ_TD = V(d, oTD), KQ_XL_CB = V(d, oXLCB), KQ_XL_DV = V(d, oXLDV),
                        T12 = V(d, oT12), Sau_Thang_Dau_Nam = V(d, o6T), TongKet_Nam = V(d, oTK),
                        TS_Loai1 = V(d, oL1), TS_Loai2 = V(d, oL2), TS_Loai3 = V(d, oL3), TS_Loai4 = V(d, oL4),
                        GhiChu = oGc >= 0 ? SafeDecrypt(d[oGc]) : ""
                    };
                    item.HoVaTen_Search = BoLocDanhSach.ChuanHoaTimKiem(item.HoVaTen) + "|" + BoLocDanhSach.ChuanHoaTimKiem(item.SoHieu);
                    item.SortPriority = dicDonViUuTien.TryGetValue(item.DonVi, out int p) ? p : int.MaxValue;
                    for (int t = 0; t < 11; t++) item.Thang[t] = V(d, oT[t]);
                    foreach (var (ten, o) in oPhatSinh) item.CotPhatSinh[ten] = o >= 0 ? d[o] : "";
                    mang[i] = item;
                });
                goi.CBCS = mang.OrderBy(x => x.SortPriority).ThenBy(x => int.TryParse(x.ID, out int id) ? id : int.MaxValue).ToList();
            }
            goi.Bang = schemaDt;
            return goi;
        }
        private void ThucHienTaiDuLieuLichSu() => _ = ThucHienTaiDuLieuLichSuAsync();
        private async Task ThucHienTaiDuLieuLichSuAsync()
        {
            int theHe = ++_theHeNap;
            if (comboBox_ChonCSDLNam.SelectedItem is not FileLichSuDTO selectedFile)
            {
                _dangNapLichSu = false;
                kryptonDataGridView1.RowCount = 0;
                _filteredIndexes = new List<int>();
                CapNhatTrangThaiHienThi();
                CapNhatHienThiGridTheoSoDong(0, "Chưa chọn tệp CSDL năm cũ để hiển thị.");
                return;
            }
            _dangNapLichSu = true;
            this.Cursor = Cursors.WaitCursor;
            toolStripStatusLabel1.Text = $"Đang tải dữ liệu {selectedFile.TenHienThi}...";
            string duongDan = selectedFile.DuongDan, csdl2 = _csdl2Path;
            bool laTanBinh = selectedFile.LaTanBinh;
            GoiLichSu? goi = null;
            try
            {
                goi = await Task.Run(() => DocGoiLichSu(duongDan, laTanBinh, csdl2));
            }
            catch (Exception ex) { Debug.WriteLine("Lỗi nạp RAM DTO lịch sử: " + ex.Message); }
            // Đã có lượt nạp mới hơn (đổi năm) hoặc form đã đóng => bỏ kết quả này
            if (theHe != _theHeNap || IsDisposed) return;
            _dangNapLichSu = false;
            this.Cursor = Cursors.Default;
            if (goi == null)
            {
                CapNhatTrangThaiHienThi();
                return;
            }
            _chuKyDaNap = TaoChuKyDuLieu();   // lấy SAU khi đồng bộ tình trạng (bước đó có thể ghi vào tệp năm cũ)
            var grid = kryptonDataGridView1;
            using (Module_LuoiAo.TamDungVe(grid))
            {
                grid.SuspendLayout();
                try
                {
                    // Gán danh sách MỚI (không Clear danh sách cũ tại chỗ)
                    _isDataMaxMode = goi.MaxMode;
                    _laTanBinhHienTai = goi.LaTanBinh;
                    _dataCacheCBCS = goi.CBCS;
                    _dataCacheTanBinh = goi.TanBinh;
                    _cotPhatSinhCacheThongKe = goi.CotPhatSinh;
                    _dtHienTai = goi.Bang;
                    _timKiemChuan = null;
                    _nguonLocTruoc = null;
                    KhoiTaoBoLocComboBox(goi.LaTanBinh);   // đặt lại mọi bộ lọc về "Tất cả" (không bắn lọc)
                    grid.RowCount = 0;
                    grid.DataSource = null;
                    grid.Columns.Clear();
                    var cot = new List<DataGridViewColumn>(_dtHienTai.Columns.Count);
                    foreach (DataColumn dc in _dtHienTai.Columns)
                    {
                        if (dc.ColumnName == "SortPriority" || dc.ColumnName == "HoVaTen_Search") continue;
                        cot.Add(new DataGridViewTextBoxColumn { Name = dc.ColumnName, HeaderText = dc.ColumnName, SortMode = DataGridViewColumnSortMode.NotSortable });
                    }
                    grid.Columns.AddRange(cot.ToArray());   // thêm 1 lần thay vì từng cột (mỗi lần Add lưới tính lại bố cục)
                    ApDungDinhDangGridThongKe(goi.LaTanBinh);
                    _colIndexMap.Clear();
                    for (int i = 0; i < grid.Columns.Count; i++)
                    {
                        int o = _dtHienTai.Columns.IndexOf(grid.Columns[i].Name);
                        if (o >= 0) _colIndexMap[i] = o;
                    }
                    ApplyFilter();
                }
                catch (Exception ex) { Debug.WriteLine("Lỗi dựng lưới lịch sử: " + ex.Message); }
                finally { grid.ResumeLayout(); }
            }
            grid.Invalidate();
        }
        private void xoaTimKiem_ToolStripMenuItem_Click(object sender, EventArgs e) => kryptonButton_LamMoiCacOTimKiem.PerformClick();
        private void xuatDuLieuTepExcel_ToolStripMenuItem_Click(object sender, EventArgs e) => kryptonButton_XuatData.PerformClick();
        private void xoaCSDL_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 1. KIỂM TRA TỆP CSDL LỊCH SỬ ĐANG ĐƯỢC CHỌN
            if (comboBox_ChonCSDLNam.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn năm cần xóa dữ liệu thi đua cá nhân trên danh sách!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var selectedFile = (FileLichSuDTO)comboBox_ChonCSDLNam.SelectedItem;
            string fileToDelete = selectedFile.DuongDan;
            if (string.IsNullOrEmpty(fileToDelete) || !File.Exists(fileToDelete))
            {
                MessageBox.Show("Tệp CSDL không tồn tại trên ổ đĩa hoặc đã bị xóa trước đó!", Module_HeThong.TieuDe_Loi, MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoadDanhSachFileLichSu(); // Rà soát lại danh sách tệp
                return;
            }
            // 2. BẢO VỆ MẤT DỮ LIỆU: HỎI XÁC NHẬN CẢNH BÁO NGUY HIỂM
            string tenBangCaNhan = selectedFile.BangThiDua;
            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn XÓA VĨNH VIỄN dữ liệu thi đua cá nhân đã lưu của:\n👉 {selectedFile.TenHienThi}\n\n" +
                (selectedFile.LaTanBinh ? "" : "Dữ liệu thi đua tập thể của năm này (Quản lý thi đua tập thể năm cũ) KHÔNG bị xóa.\n") +
                "Tệp lưu trữ chỉ bị xóa khi không còn dữ liệu nào khác.\n⚠️ Thao tác này không thể khôi phục!",
                "Xác nhận xóa dữ liệu thi đua cá nhân năm cũ",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes)
                return;
            // 3. XÁC MINH QUYỀN ADMIN (FORM 24)
            DialogResult kq;
            using (Form24_XacMinhAdmin frm = new Form24_XacMinhAdmin())
            {
                frm.TopMost = true;
                frm.StartPosition = FormStartPosition.CenterScreen;
                kq = frm.ShowDialog();
            }
            if (kq != DialogResult.OK)
                return;
            // 4. ⭐ QUY TRÌNH BẢO AN: CẮT LIÊN KẾT GRID VÀ DỌN SẠCH RAM TRƯỚC
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
                // 5. CHỈ XÓA BẢNG THI ĐUA CÁ NHÂN (giống Form59 chỉ xóa bảng tập thể). Bảng còn lại cũng trống/không còn
                //    => XoaTepLichSuNeuRong mới xóa hẳn tệp (kèm -wal/-shm).
                using (var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(fileToDelete, pooling: false)))
                {
                    cn.Open();
                    using var cmd = cn.CreateCommand();
                    cmd.CommandText = "PRAGMA busy_timeout=5000;";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = $"DROP TABLE IF EXISTS [{tenBangCaNhan}];";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "VACUUM;";   // thu hồi dung lượng của bảng vừa xóa
                    cmd.ExecuteNonQuery();
                }
                bool daXoaTep = Module_HoTroLuuDataTheoNamCu.XoaTepLichSuNeuRong(fileToDelete);
                // Ghi nhật ký thao tác hệ thống
                try
                {
                    Module_NhatKy.GhiNhatKy(
                        Module_TaiKhoan.TenTaiKhoan_RAM,
                        "Xóa dữ liệu thi đua cá nhân năm cũ",
                        $"{selectedFile.TenHienThi} ({Path.GetFileName(fileToDelete)}): đã xóa bảng {tenBangCaNhan}" +
                        (daXoaTep ? "; tệp không còn dữ liệu nên đã xóa hẳn" : "; tệp vẫn giữ (còn dữ liệu thi đua tập thể)"));
                }
                catch { }
                //CapNhatTrangThaiHienThi();
                //MessageBox.Show($"Đã xóa vĩnh viễn tệp CSDL lịch sử:\n👉 {selectedFile.TenHienThi}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // 6. NẠP LẠI DANH SÁCH & CẬP NHẬT TỰ ĐỘNG GIAO DIỆN
                // Quét lại thư mục CSDL lịch sử
                // (hàm này tự gỡ/gắn lại sự kiện combo năm và tự nạp tệp đầu tiên lên lưới nếu còn tệp)
                LoadDanhSachFileLichSu();
                if (comboBox_ChonCSDLNam.Items.Count == 0)
                {
                    // Nếu đã xóa hết tệp -> Xóa trắng các ComboBox tìm kiếm và báo quân số = 0
                    if (comboBox_TimKiemDonVi.Items.Count > 0) comboBox_TimKiemDonVi.SelectedIndex = 0;
                    if (comboBox1_TinhTrang.Items.Count > 0) comboBox1_TinhTrang.SelectedIndex = 0;
                    if (comboBox1_PhanLoaiThiDuaNamCu.Items.Count > 0) comboBox1_PhanLoaiThiDuaNamCu.SelectedIndex = 0;
                    _dangNapComboLoc = true;
                    try { comboBox_LocGhiChu.Items.Clear(); comboBox_LocGhiChu.Items.Add(Module_HeThong.Tat_Ca); comboBox_LocGhiChu.SelectedIndex = 0; }
                    finally { _dangNapComboLoc = false; }
                    toolStripStatusLabel1.Text = $"Tổng cộng: 0 {Module_HeThong.Tu_dong_chi}";
                    CapNhatHienThiGridTheoSoDong(0, "Không còn năm nào có dữ liệu thi đua cá nhân đã lưu.");
                }
            }
            catch (Exception ex)
            {
                // Khôi phục lại sự kiện nếu xảy ra ngoại lệ
                comboBox_ChonCSDLNam.SelectedIndexChanged -= comboBox_ChonCSDLNam_SelectedIndexChanged;
                comboBox_ChonCSDLNam.SelectedIndexChanged += comboBox_ChonCSDLNam_SelectedIndexChanged;
                MessageBox.Show($"Không thể xóa dữ liệu do tệp đang bị ứng dụng khác chiếm dụng hoặc lỗi hệ thống:\n\n{ex.Message}", "Lỗi xóa dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
   
        private void huongDan_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            string msgHuongDan = @"GIỚI THIỆU CHỨC NĂNG:
                Màn hình ""Thống kê thi đua năm cũ"" giúp tra cứu, tổng hợp và kết xuất dữ liệu lịch sử của các năm trước. Hệ thống được trang bị lõi xử lý hiệu năng cao, tự động thay đổi cấu trúc bảng biểu phù hợp cho từng nhóm đối tượng (Cán bộ chiến sĩ, Tân binh hoặc Học viên).
                HƯỚNG DẪN SỬ DỤNG:
                1. Chọn dữ liệu: Chọn tệp CSDL năm cũ (VD: Năm 2025 - CBCS) tại danh sách thả xuống góc trái.
                2. Tìm kiếm nhanh: 
                   - Hỗ trợ lọc đa tầng theo: Đơn vị, Tình trạng, và Phân loại.
                   - Ô tìm kiếm tên: Chỉ cần gõ tên (không cần bấm Enter), hệ thống sẽ tự động lọc kết quả sau 0.3 giây.
                   - Bấm [Làm mới] để xóa các bộ lọc và hiển thị lại toàn bộ danh sách.
                3. Cập nhật dữ liệu: Bấm [Cập nhật] để đồng bộ trạng thái mới nhất của quân số vào hồ sơ năm cũ.
                4. Trích xuất báo cáo: Bấm [Xuất Data] để xuất danh sách đang hiển thị trên lưới ra tệp Excel chuyên nghiệp.
                LƯU Ý QUAN TRỌNG:
                - Chức năng [Xóa dữ liệu thi đua cá nhân] (chuột phải) chỉ xóa vĩnh viễn bảng thi đua cá nhân của năm đang chọn; dữ liệu thi đua tập thể của năm đó vẫn giữ. Tệp lưu trữ chỉ bị xóa khi không còn dữ liệu nào. Thao tác này không thể hoàn tác và yêu cầu phải có mật khẩu Admin.";
            MessageBox.Show(msgHuongDan,
                            "Hướng Dẫn Sử Dụng - Thống Kê Năm Cũ",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
        }
        // 🔥 BIẾN CACHE INSTANCE FORM 54 TRÊN RAM (Tránh new/dispose lặp lại)
        private Form54_ThongKeThiDuaTapTheNamCu? _cachedForm54 = null;
        private void kryptonButton1_XemKetQuaThiDuaTapThe_Click(object? sender, EventArgs e)
        {
            // 1. Kiểm tra đã chọn tệp CSDL năm cũ chưa
            if (comboBox_ChonCSDLNam.SelectedItem is not FileLichSuDTO selectedFile)
            {
                MessageBox.Show("Vui lòng chọn một tệp CSDL năm cũ trên danh sách trước khi xem!",
                                "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // 2. Chặn nếu là phiên bản Tân binh (tập thể chỉ áp dụng cho CBCS)
            if (selectedFile.LaTanBinh)
            {
                MessageBox.Show($"Dữ liệu thi đua tập thể chỉ áp dụng cho đối tượng Cán bộ chiến sĩ (CBCS), không áp dụng cho đối tượng {Module_TaiKhoan.TenTB_Hoa}.",
                                "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string namHienThi = selectedFile.TenHienThi;
            string dbPath = selectedFile.DuongDan;
            // 3. ⭐ KIỂM TRA RAM: Quét xem Form54 đã tồn tại chưa
            if (_cachedForm54 == null || _cachedForm54.IsDisposed)
            {
                _cachedForm54 = new Form54_ThongKeThiDuaTapTheNamCu
                {
                    StartPosition = FormStartPosition.CenterParent,
                    ShowInTaskbar = false
                };
            }
            // 4. Bơm dữ liệu năm tương ứng vào Form
            _cachedForm54.CapNhatDuLieuNamCu(namHienThi, dbPath);
            // 5. Hiển thị Form mượt mà
            if (_cachedForm54.Visible)
            {
                if (_cachedForm54.WindowState == FormWindowState.Minimized)
                    _cachedForm54.WindowState = FormWindowState.Normal;
                _cachedForm54.BringToFront();
                _cachedForm54.Focus();
            }
            else
            {
                _cachedForm54.ShowDialog(this);
            }
        }
        private Label? _labelEmptyState;
        private void EnsureEmptyStateLabel()
        {
            if (_labelEmptyState != null) return;
            _labelEmptyState = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(Module_HeThong.TenFontHeThong, 10.5F, FontStyle.Italic),
                ForeColor = Color.DimGray,
                BackColor = Color.White,
                Visible = false
            };
            splitContainer1.Panel2.Controls.Add(_labelEmptyState);
        }
        private void CapNhatHienThiGridTheoSoDong(int soDongHienThi, string thongBaoKhiTrong)
        {
            EnsureEmptyStateLabel();
            if (soDongHienThi <= 0)
            {
                kryptonDataGridView1.Visible = false;
                _labelEmptyState!.Text = thongBaoKhiTrong;
                _labelEmptyState.Visible = true;
                _labelEmptyState.BringToFront();
            }
            else
            {
                _labelEmptyState!.Visible = false;
                kryptonDataGridView1.Visible = true;
            }
        }
    }
    public class ColumnExportMeta
    {
        public string Name { get; set; } = null!;
        public string HeaderText { get; set; } = null!;
    }
}