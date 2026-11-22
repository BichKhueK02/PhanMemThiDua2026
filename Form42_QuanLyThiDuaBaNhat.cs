///Phiên bản 2
///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
using System.Globalization;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form42_QuanLyThiDuaBaNhat : Form
    {
        // ===================== HẰNG / TRƯỜNG DÙNG CHUNG =====================
        private const float RichText_MinFontSize = 7f;
        private const float RichText_MaxFontSize = 30f;
        private const float RichText_FontStep = 1f;
        private const int GioiHanHienThiChu = 25;          // lưới chỉ hiện 25 ký tự đầu của Quê quán / Ghi chú / Thành tích
        private const int GhiChuMin = 190, ThanhTichMin = 240;
        // Quy ước: mục đầu tiên (chỉ số 0) của hai combobox lọc luôn là "Tất cả" = không lọc.
        private const string TenTatCa = "Tất cả";
        private const string TenDeNghi = "Đề nghị";
        private const string TenKhongDeNghi = "Không";
        public bool DaLoadDuLieu { get; private set; }
        // Đường dẫn CSDL luôn đọc mới từ module dẫn đường (không chụp cứng lúc dựng form)
        private static string DuongDanDb => Module_DanduongGPS.DuongDanCSDL2;
        // Tên bảng theo phiên bản phần mềm: chỉ có 2 bộ giá trị cố định => không thể bị chèn SQL.
        // LayPhienBanPhanMem() mở SQLite + giải mã nên chỉ đọc 1 lần cho mỗi lượt nạp lưới, các thao tác khác dùng lại giá trị đã chụp.
        private readonly record struct BangDuLieu(string DanhSach, string SoVang);
        private BangDuLieu? _bang;
        private BangDuLieu LayBang()
        {
            var b = _bang;
            if (b == null) { b = ChonBang(); _bang = b; }
            return b.Value;
        }
        private static BangDuLieu ChonBang()
        {
            string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
            return phienBan.LaCheDoTanBinh()
                ? new BangDuLieu("DanhSachBaNhat_TanBinh", "DanhSachTanBinh_SoVangBaNhat")
                : new BangDuLieu("DanhSachBaNhat", "DanhSach_SoVangBaNhat");
        }
        private string TenBangDanhSachBaNhat => LayBang().DanhSach;
        //private string TenBangSoVangHienTai => LayBang().SoVang;
        private bool _dangLamMoiCSDL;
        private bool _daKhoiTaoToolTip;
        private bool _daCauHinhGrid;
        private int _soDeNghi;                               // số người đã đề nghị trong bảng đang hiển thị (tính 1 lần khi nạp/lưu)
        private bool[] _cotCatChu = Array.Empty<bool>();    // cột nào bị cắt chữ trên lưới (tra theo chỉ số, không so chuỗi)
        private CancellationTokenSource? _ctsLoadDanhSachBaNhat;
        private readonly SemaphoreSlim _khoaDongBo = new(1, 1);   // không cho 2 lượt đồng bộ ghi CSDL chồng nhau
        private Font? _fontGrid, _fontGridDam, _fontRichTuTao;
        private static readonly System.Reflection.PropertyInfo? _pDoubleBuffered =
            typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr h, int msg, int w, int l);
        private const int WM_SETREDRAW = 0x000B;
        private static void TatVe(Control c) { if (c.IsHandleCreated) SendMessage(c.Handle, WM_SETREDRAW, 0, 0); }
        private static void BatVe(Control c) { if (c.IsHandleCreated) { SendMessage(c.Handle, WM_SETREDRAW, 1, 0); c.Invalidate(true); } }
        // ===================== KHỞI TẠO =====================
        public Form42_QuanLyThiDuaBaNhat()
        {
            InitializeComponent();
            // Mọi chỉnh sửa giao diện sau InitializeComponent gom vào 1 lượt tạm dừng bố cục cả cây => dàn trang đúng 1 lần
            Module_TamDungBoCuc.ChayKhongDanTrang(this, () =>
            {
                Module_GiaoDienForm42_44Web.GanChuMoTimKiem(textBox_TimKiemTheoTen, Module_HeThong.Nhap_Tim_Kiem);   // chữ mờ gốc Windows: rê chuột không giật
                Module_GiaoDienForm42_44Web.ApDung(this);
                // Tinh chỉnh hiển thị: DoubleBuffered sớm cho các khối bố cục + lưới
                BatDoubleBufferDeQuy(this);
                EnableDoubleBuffer(kryptonDataGridView1);
                Module_HeThong.BocBoTronGiaoDien(richTextBox1_ThanhTich);
            });
            Shown += Form42_QuanLyThiDuaBaNhat_Shown;
            Disposed += (s, e) => GiaiPhongTaiNguyenForm42();
            StartPosition = FormStartPosition.CenterScreen;
            // Thanh tiến trình: đặt kích thước cố định 1 lần ở đây (không đổi kích thước ở mỗi lần làm mới => không giật bố cục)
            toolStripProgressBar1_LamMoi.AutoSize = false;
            toolStripProgressBar1_LamMoi.Size = new Size(150, 16);
            toolStripProgressBar1_LamMoi.Visible = false;
            // 1 handler duy nhất cho ô Thành tích (bản cũ gắn 2 nơi)
            richTextBox1_ThanhTich.TextChanged += richTextBox1_ThanhTich_TextChanged;
            // Hiện/ẩn nút Lưu theo việc đã chọn người hay chưa
            textBox_SoHieu.TextChanged += (s, e) => CapNhatTrangThaiNut();
            textBox_HoVaTen.TextChanged += (s, e) => CapNhatTrangThaiNut();
            // Sự kiện của lưới: gắn đúng 1 lần
            kryptonDataGridView1.CellClick += kryptonDataGridView1_CellClick;
            kryptonDataGridView1.CellFormatting += kryptonDataGridView1_CellFormatting;
            kryptonDataGridView1.CellMouseClick += kryptonDataGridView1_CellMouseClick;
            kryptonDataGridView1.CellToolTipTextNeeded += kryptonDataGridView1_CellToolTipTextNeeded;
        }
        private void GiaiPhongTaiNguyenForm42()
        {
            try
            {
                _timerLoc?.Stop();
                _timerLoc?.Dispose();
                _timerLoc = null;
                _ctsLoadDanhSachBaNhat?.Cancel();
                _ctsLoadDanhSachBaNhat?.Dispose();
                _ctsLoadDanhSachBaNhat = null;
                _fontGrid?.Dispose();
                _fontGridDam?.Dispose();
                _fontRichTuTao?.Dispose();
                _fontGrid = _fontGridDam = _fontRichTuTao = null;
            }
            catch { /* dọn dẹp best-effort */ }
        }
        // Giảm chớp khi layout/resize: DoubleBuffered cho container (không đụng RichTextBox/Krypton/ComboBox)
        private static void BatDoubleBufferDeQuy(Control goc)
        {
            foreach (Control c in goc.Controls)
            {
                if (c is TableLayoutPanel || c is GroupBox || c is Panel || c is SplitContainer || c is TabPage)
                    _pDoubleBuffered?.SetValue(c, true);
                BatDoubleBufferDeQuy(c);
            }
        }
        private static void EnableDoubleBuffer(Control ctrl) => _pDoubleBuffered?.SetValue(ctrl, true, null);
        // ===================== CHUẨN BỊ GIAO DIỆN / NẠP DỮ LIỆU KHI MỞ =====================
        /// <summary>
        /// true khi Form55 (form cha) điều phối dữ liệu: nạp lưới + đồng bộ nền đúng 1 lần ở Form55, Form42.Load chỉ dựng giao diện.
        /// false (mặc định) = chạy độc lập như bản cũ: Load tự nạp lưới rồi đồng bộ nền.
        /// </summary>
        public bool FormChaQuanLyNapDuLieu { get; set; }
        private bool _daChuanBiGiaoDien;
        /// <summary>
        /// Dựng sẵn phần giao diện KHÔNG cần dữ liệu: menu chuột phải, bộ lọc, tooltip, ô chỉ-đọc, cột + style lưới.
        /// Có thể gọi lúc form còn ẩn (Form55 dựng sẵn từng phần lúc rảnh) -> lúc Show thật chỉ còn bố cục + dữ liệu.
        /// Gọi nhiều lần an toàn (chỉ chạy 1 lần; KhoiTaoBoLoc xóa danh sách đơn vị nên tuyệt đối không được chạy lại sau khi đã nạp dữ liệu).
        /// </summary>
        public void ChuanBiGiaoDienTruoc()
        {
            if (_daChuanBiGiaoDien || IsDisposed) return;
            _daChuanBiGiaoDien = true;
            Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1);
            KhoiTaoBoLoc();                       // dựng sẵn mục "Tất cả" + gắn sự kiện lọc đúng 1 lần
            AcceptButton = kryptonButton_LuuDataDeNghi;
            InitToolTips();
            KhoaCacTextBox();
            CapNhatTrangThaiNut();
            KiemTraHienThiNutCoChu();
            CapNhatSoLuongKyTu();
            CauHinhGridMotLan();                  // dựng cột + style lưới trước, lúc nạp dữ liệu chỉ còn đổi DataSource
        }
        /// <summary>
        /// MỞ FORM NHANH: dựng giao diện xong là nạp lưới NGAY từ CSDL (1 lượt đọc), người dùng thấy dữ liệu và thao tác được liền.
        /// Phần đồng bộ nguồn (Loại 1) + tình trạng Sổ vàng + tỷ lệ chạy nền SAU đó; chỉ khi có thay đổi mới nạp lại / vá lưới.
        /// Khi được Form55 điều phối (FormChaQuanLyNapDuLieu) thì Load KHÔNG nạp nữa để khỏi đọc + giải mã lặp.
        /// </summary>
        private async void Form42_QuanLyThiDuaBaNhat_Load(object? sender, EventArgs e)
        {
            try
            {
                ChuanBiGiaoDienTruoc();
                if (FormChaQuanLyNapDuLieu) return;
                await LoadDuLieuToanBoDanhSachBaNhatAsync();   // lưới + đơn vị + thống kê + tỷ lệ (kích hoạt ở bước bind)
                if (IsDisposed) return;
                await DongBoNenSauKhiMoAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Form42.Load] " + ex);
            }
        }
        /// <summary>Đồng bộ Loại 1 -> tình trạng Sổ vàng; có đổi danh sách thì nạp lại lưới, không đổi thì chỉ vá cột "Tình trạng". Form55 gọi sau khi lưới đã có dữ liệu.</summary>
        public async Task DongBoNenSauKhiMoAsync()
        {
            bool coDong = await DongBoLoai1CoThayDoiAsync();      // có người mới / bị loại / đổi thông tin so với danh sách Loại 1?
            if (IsDisposed) return;
            await Module_BaNhat.CapNhatTinhTrangSoVangAsync();    // chỉ ghi những dòng thật sự đổi
            if (IsDisposed) return;
            if (coDong) await LoadDuLieuToanBoDanhSachBaNhatAsync();   // danh sách đổi -> nạp lại (hiếm)
            else
            {
                await LamMoiTinhTrangTrenLuoiAsync();                  // không đổi -> chỉ vá cột "Tình trạng", lưới không bị bind lại
                if (IsDisposed) return;
                // Lượt tính tỷ lệ lúc bind lưới chạy SONG SONG với bước đồng bộ ghi CSDL ở trên nên có thể bị
                // "database is locked" (lỗi bị nuốt) => nhãn vẫn là "Tỷ lệ" cho tới khi bấm Làm mới. Tính lại khi CSDL đã rảnh.
                _ = Module_BaNhat.TinhToanVaHienThiTyLeBaNhatAsync(toolStripStatusLabel2_TyLeBaNhat);
            }
        }
        /// <summary>Chỉ vá cột "Tình trạng" trên lưới theo CSDL (Form44 đã tự cập nhật CSDL sau khi sửa/xóa/nhập Sổ vàng).</summary>
        public Task LamMoiTinhTrangTrenLuoiNgayAsync() => LamMoiTinhTrangTrenLuoiAsync();
        private void Form42_QuanLyThiDuaBaNhat_Shown(object? sender, EventArgs e) => textBox_TimKiemTheoTen.Focus();
        // Phím Enter ở các ô LỌC không được kích hoạt nút Lưu (AcceptButton): chỉ lọc ngay.
        // Chặn ở KeyDown không có tác dụng vì Enter được xử lý ở bước dialog-key trước đó.
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Enter && DangOTrongBoLoc())
            {
                ThucHienLocDuLieu();
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }
        private bool DangOTrongBoLoc()
            => textBox_TimKiemTheoTen.ContainsFocus || comboBox_TimKiemDonVi.ContainsFocus || comboBox1_DeNghi.ContainsFocus
               || comboBox_LocGhiChu.ContainsFocus
               // Combobox lọc được phủ bởi ô vẽ tay OComboWebF4 (nằm ở thẻ chứa tableLayoutPanel5) => focus nằm ở ô phủ, không ở ComboBox gốc
               || (ActiveControl is OComboWebF4 o && o.Parent != null && o.Parent == tableLayoutPanel5.Parent);
        // ===================== TIỆN ÍCH CSDL =====================
        // Pooling=false: đóng kết nối là nhả hẳn tệp .db (không làm kẹt sao lưu/khôi phục), giống Module_BaNhat.
        private static SqliteConnection MoKetNoi(bool chiDoc)
            => Module_KetNoiCSDL.MoKetNoi(DuongDanDb, chiDoc: chiDoc, khongTaoMoi: !chiDoc, pooling: false);
        private static string SafeGiaiMa(string? cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText)) return "";
            try { return Module_BaoMatAES.GiaiMa(cipherText).Trim(); }
            catch { return "[Lỗi giải mã]"; }
        }
        private static string DocChuoi(SqliteDataReader rd, int i) => rd.IsDBNull(i) ? "" : rd.GetString(i);
        private static string DocGiai(SqliteDataReader rd, int i) => SafeGiaiMa(rd.IsDBNull(i) ? null : rd.GetString(i));
        //private static void BaoForm55CanNapLai()
            //=> Application.OpenForms.OfType<Form55_QuanLyHeThongThiDuaBaNhat>().FirstOrDefault()?.DanhDauCanNapLai();
        private static void BaoForm55CanNapLaiSoVang()
            => Application.OpenForms.OfType<Form55_QuanLyHeThongThiDuaBaNhat>().FirstOrDefault()?.DanhDauCanNapLaiSoVang();
        // Khóa form cha + hiện Form_Loading; Dispose là mở khóa lại. Idempotent.
        private sealed class ManHinhCho : IDisposable
        {
            private readonly Form _cha;
            private readonly Form_Loading _frm;
            private bool _daDong;
            public ManHinhCho(Form cha, string noiDung)
            {
                _cha = cha;
                _frm = new Form_Loading(noiDung);
                _frm.Icon = cha.Icon;
            }
            public async Task HienAsync()
            {
                _cha.Enabled = false;
                _frm.Show(_cha);
                await Task.Delay(50);                    // nhường luồng UI vẽ form chờ
            }
            public void Dispose()
            {
                if (_daDong) return;
                _daDong = true;
                try { _frm.Close(); } catch { }
                if (!_cha.IsDisposed)
                {
                    _cha.Enabled = true;
                    _cha.Focus();
                }
            }
        }
        // ===================== TOOLTIP =====================
        private void InitToolTips()
        {
            if (_daKhoiTaoToolTip) return;
            try
            {
                toolTip1.IsBalloon = true;
                toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
                toolTip1.ToolTipIcon = ToolTipIcon.Info;
                toolTip1.InitialDelay = 300;
                toolTip1.AutoPopDelay = 2500;
                toolTip1.ReshowDelay = 100;
                toolTip1.ShowAlways = true;
                toolTip1.SetToolTip(kryptonButton_LuuDataDeNghi, "Lưu dữ liệu đề nghị biểu dương ba nhất");
                toolTip1.SetToolTip(kryptonButton_RefershCSDL, "Tải lại và cập nhật dữ liệu mới nhất từ cơ sở dữ liệu");
                toolTip1.SetToolTip(kryptonButton1_ThanhTichTapThe, "Xem và quản lý bảng thành tích của tập thể đơn vị");
                toolTip1.SetToolTip(kryptonButton1_Thoat, "Trở về Trang quản lý thi đua");
                toolTip1.SetToolTip(kryptonButton2_TangCoChuRichText, "Tăng cỡ chữ");
                toolTip1.SetToolTip(kryptonButton2_GiamCoChuRichText, "Giảm cỡ chữ");
                _daKhoiTaoToolTip = true;
            }
            catch (Exception ex) { Debug.WriteLine("[InitToolTips] " + ex.Message); }
        }
        // ===================== LƯỚI =====================
        private void CauHinhGridMotLan()
        {
            if (_daCauHinhGrid) return;
            var g = kryptonDataGridView1;
            EnableDoubleBuffer(g);
            g.AutoGenerateColumns = false;               // quan trọng: không sinh lại cột khi đổi DataSource
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.AllowUserToResizeRows = false;
            g.AllowUserToOrderColumns = false;
            g.RowHeadersVisible = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.MultiSelect = false;
            // Lưới chỉ để xem/chọn: việc sửa Đề nghị / Thành tích làm ở khung "3. Cập nhật thông tin" rồi bấm Lưu.
            // (Bản cũ cho gõ thẳng vào ô lưới nhưng không có chỗ nào ghi xuống CSDL => dữ liệu lưới và CSDL bị lệch.)
            g.ReadOnly = true;
            g.GridStyles.Style = Krypton.Toolkit.DataGridViewStyle.List;
            g.RowTemplate.Height = 36;
            g.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            g.ColumnHeadersHeight = 60;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _fontGridDam = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Bold);
            _fontGrid = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Regular);
            var pad = new Padding(6, 8, 6, 8);
            g.StateCommon.HeaderColumn.Content.Padding = pad;
            g.StateCommon.HeaderColumn.Content.Font = _fontGridDam;
            g.StateCommon.HeaderRow.Content.Padding = pad;
            g.StateCommon.HeaderRow.Content.Font = _fontGridDam;
            g.StateCommon.DataCell.Content.Font = _fontGrid;
            g.StateCommon.DataCell.Border.Color1 = Color.FromArgb(224, 224, 224);
            g.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            g.StateCommon.DataCell.Border.Width = 1;
            g.StateSelected.DataCell.Back.Color1 = Color.FromArgb(232, 244, 253);
            g.StateSelected.DataCell.Back.Color2 = Color.FromArgb(232, 244, 253);
            g.StateSelected.DataCell.Content.Color1 = Color.FromArgb(0, 102, 204);
            // Thêm cột ĐÚNG THỨ TỰ hiển thị -> không cần DisplayIndex
            var giua = DataGridViewContentAlignment.MiddleCenter;
            var trai = DataGridViewContentAlignment.MiddleLeft;
            g.Columns.AddRange(
                Cot("ID", "ID", 50, giua, hien: false),
                Cot("STT", "STT", 35, giua),
                Cot("HoVaTen", "Họ và tên", 215, trai),
                Cot("SoHieu", "Số hiệu", 90, giua),
                Cot("NamSinh", "Năm sinh", 80, giua),
                Cot("QueQuan", "Quê quán", 210, trai),
                Cot("NgayVaoCAND", "Vào CAND", 120, giua),
                Cot("CapBac", "Cấp bậc", 90, giua),
                Cot("ChucVu", "Chức vụ", 110, giua),
                Cot("DonVi", "Đơn vị", 85, giua),
                Cot("PhanLoai", "Phân loại", 90, giua, hien: false),
                Cot("DeNghi", "Đề nghị", 70, giua),
                Cot("TinhTrang", "Tình trạng", 120, giua),
                Cot("GhiChu", "Ghi chú", GhiChuMin, trai, minW: GhiChuMin),
                Cot("ThanhTich", "Thành tích", ThanhTichMin, trai, minW: ThanhTichMin));
            g.Columns["HoVaTen"]!.Frozen = true;          // cuộn ngang vẫn thấy STT + Họ tên
            _cotCatChu = new bool[g.Columns.Count];
            foreach (string ten in new[] { "QueQuan", "GhiChu", "ThanhTich" })
                _cotCatChu[g.Columns[ten]!.Index] = true;
            _daCauHinhGrid = true;
        }
        // Tạo THẲNG cột Krypton: nếu để DataGridViewTextBoxColumn thường, mỗi lần gán DataSource KryptonDataGridView
        // (AutoGenerateKryptonColumns) gỡ ra + chèn lại từng cột để đổi kiểu; gặp cột "Họ và tên" Frozen thì ném
        // "Column cannot be added because it is unfrozen and placed before a frozen column" giữa chừng => lưới mất dần
        // cột ID/STT, bộ lọc không nạp, DaLoadDuLieu = false nên lần mở nào cũng nạp + đồng bộ lại cả bảng (đo 22/11/2026).
        private static Krypton.Toolkit.KryptonDataGridViewTextBoxColumn Cot(string ten, string tieuDe, int rong,
            DataGridViewContentAlignment canh, bool hien = true, int minW = 0)
        {
            var c = new Krypton.Toolkit.KryptonDataGridViewTextBoxColumn
            {
                Name = ten,
                DataPropertyName = ten,
                HeaderText = tieuDe,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ReadOnly = true,
                Visible = hien,
                // Fill: các cột tự chia đều chiều ngang còn lại theo tỷ trọng (FillWeight = độ rộng mong muốn),
                // luôn lấp kín lưới, tự co giãn khi đổi kích thước form, không còn khoảng trống bên phải.
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = rong,
                MinimumWidth = minW > 0 ? minW : Math.Max(30, rong * 6 / 10)
            };
            c.DefaultCellStyle.Alignment = canh;
            c.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            c.Width = rong;
            return c;
        }
        private void kryptonDataGridView1_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            // Chạy cho MỖI ô được vẽ: chỉ tra mảng theo chỉ số cột, không so chuỗi
            if (e.RowIndex < 0 || (uint)e.ColumnIndex >= (uint)_cotCatChu.Length || !_cotCatChu[e.ColumnIndex]) return;
            if (e.Value is string s && s.Length > GioiHanHienThiChu)
            {
                e.Value = s.Substring(0, GioiHanHienThiChu) + " ...";
                e.FormattingApplied = true;
            }
        }
        // Chữ trên lưới bị cắt ở 25 ký tự -> rê chuột để xem đầy đủ.
        // Đọc từ DataView thay vì g.Rows[i].Cells[...] (truy cập Rows[i] làm dòng "chung" bị tách riêng, tốn RAM khi rê qua hàng nghìn dòng).
        private void kryptonDataGridView1_CellToolTipTextNeeded(object? sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || (uint)e.ColumnIndex >= (uint)_cotCatChu.Length || !_cotCatChu[e.ColumnIndex]) return;
            var g = kryptonDataGridView1;
            if (g.DataSource is not DataTable dt || e.RowIndex >= dt.DefaultView.Count) return;
            if (dt.DefaultView[e.RowIndex][g.Columns[e.ColumnIndex].Name] is string s && s.Length > GioiHanHienThiChu)
                e.ToolTipText = s;
        }
        private void KhoaCacTextBox()
        {
            var cacOKhoa = new[]
            {
                textBox_STT, textBox_HoVaTen, textBox_SoHieu, textBox_NamSinh, textBox_QueQuan, textBox_NgayVaoCAND,
                textBox_GhiChu, kryptonTextBox1_CapBac, kryptonTextBox1_ChucVu, kryptonTextBox1_DonVi, kryptonTextBox1_PhanLoai
            };
            foreach (var ktb in cacOKhoa)
            {
                ktb.ReadOnly = true;
                // Màu xanh pastel nhạt dịu mắt, đè lên Theme/Palette của Krypton
                ktb.StateCommon.Back.Color1 = Color.FromArgb(230, 255, 230);
            }
        }
        // ===================== CHỌN DÒNG -> ĐỔ LÊN Ô NHẬP =====================
        private void kryptonDataGridView1_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            // Chỉ xử lý nếu là chuột phải và click vào một dòng hợp lệ
            if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;
            var g = kryptonDataGridView1;
            if (g.IsDisposed || e.RowIndex >= g.Rows.Count) return;
            try
            {
                // 1. Chọn dòng mà người dùng vừa chuột phải
                g.ClearSelection();
                g.Rows[e.RowIndex].Selected = true;
                // 2. Đưa con trỏ về ô vừa bấm (chỉ khi ô đó nhìn thấy được, tránh lỗi với cột ẩn)
                if (e.ColumnIndex >= 0 && g.Columns[e.ColumnIndex].Visible)
                    g.CurrentCell = g.Rows[e.RowIndex].Cells[e.ColumnIndex];
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[CellMouseClick] " + ex);
            }
            // 3. Đổ dữ liệu dòng lên các ô nhập (dùng chung với CellClick)
            DoDuLieuDongLenForm(e.RowIndex);
        }
        private void kryptonDataGridView1_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DoDuLieuDongLenForm(e.RowIndex);
        }
        // Lấy giá trị của một cột trên dòng lưới. Ưu tiên đọc từ nguồn dữ liệu (DataTable luôn có đủ cột
        // kể cả cột ẩn), dự phòng đọc từ ô của lưới. Không bao giờ ném lỗi "Column named ... cannot be found".
        private static object? LayGiaTriDong(DataGridViewRow row, string tenCot)
        {
            if (row.DataBoundItem is DataRowView drv && drv.Row.Table.Columns.Contains(tenCot))
            {
                object v = drv[tenCot];
                return v == DBNull.Value ? null : v;
            }
            var g = row.DataGridView;
            if (g != null && g.Columns.Contains(tenCot))
                return row.Cells[tenCot].Value;
            return null;
        }
        // Đổ dữ liệu dòng đang chọn lên các ô nhập ở phần "3. Cập nhật thông tin".
        private void DoDuLieuDongLenForm(int rowIndex)
        {
            var g = kryptonDataGridView1;
            if (g.IsDisposed || rowIndex < 0 || rowIndex >= g.Rows.Count) return;
            try
            {
                DataGridViewRow row = g.Rows[rowIndex];
                if (row.IsNewRow) return;
                string Chuoi(string tenCot) => LayGiaTriDong(row, tenCot)?.ToString() ?? "";
                textBox_SoHieu.Tag = LayGiaTriDong(row, "ID");
                textBox_STT.Text = Chuoi("STT");
                textBox_HoVaTen.Text = Chuoi("HoVaTen");
                textBox_SoHieu.Text = Chuoi("SoHieu");
                textBox_NamSinh.Text = Chuoi("NamSinh");
                textBox_QueQuan.Text = Chuoi("QueQuan");
                textBox_NgayVaoCAND.Text = Chuoi("NgayVaoCAND");
                kryptonTextBox1_CapBac.Text = Chuoi("CapBac");
                kryptonTextBox1_ChucVu.Text = Chuoi("ChucVu");
                kryptonTextBox1_DonVi.Text = Chuoi("DonVi");
                kryptonTextBox1_PhanLoai.Text = Chuoi("PhanLoai");
                textBox_GhiChu.Text = Chuoi("GhiChu");
                richTextBox1_ThanhTich.Text = Chuoi("ThanhTich");
                // Ô "Đề nghị": cột DeNghi = "X" thì hiện "Đề nghị", ngược lại để trống
                comboBox_DeNghiBaNhat.Text = Chuoi("DeNghi").Trim().Equals("X", StringComparison.OrdinalIgnoreCase)
                    ? TenDeNghi : "";
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[DoDuLieuDongLenForm] " + ex);
            }
        }
        private void CapNhatTrangThaiNut()
        {
            bool coDuLieu = !string.IsNullOrWhiteSpace(textBox_SoHieu.Text) &&
                            !string.IsNullOrWhiteSpace(textBox_HoVaTen.Text);
            if (kryptonButton_LuuDataDeNghi.Visible != coDuLieu) kryptonButton_LuuDataDeNghi.Visible = coDuLieu;
        }
        // ===================== NẠP DỮ LIỆU =====================
        private static DataTable TaoSchemaDanhSachBaNhat()
        {
            var dt = new DataTable
            {
                Locale = CultureInfo.InvariantCulture // hành vi so sánh/sắp xếp nhất quán, không phụ thuộc locale máy
            };
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("STT", typeof(int));
            foreach (string c in new[] { "HoVaTen", "SoHieu", "NamSinh", "QueQuan", "NgayVaoCAND", "CapBac", "ChucVu",
                                         "DonVi", "PhanLoai", "GhiChu", "DeNghi", "ThanhTich", "TinhTrang" })
                dt.Columns.Add(c, typeof(string));
            return dt;
        }
        // Đọc + giải mã ở luồng nền. Dòng đệm của CSDL (ID = -1) không phải người thật nên bỏ qua (khớp cách Module_BaNhat xử lý).
        // Đếm luôn số người đã đề nghị trong lúc đọc để thanh trạng thái không phải duyệt lại cả bảng.
        private static (DataTable dt, int soDeNghi) DocBangBaNhat(string tenBang, CancellationToken token)
        {
            var dt = TaoSchemaDanhSachBaNhat();
            int soDeNghi = 0;
            using var cn = MoKetNoi(true);
            using var cmd = cn.CreateCommand();
            cmd.CommandText = $@"SELECT ID, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi,
                                        PhanLoai, GhiChu, DeNghi, ThanhTich, TinhTrang
                                 FROM [{tenBang}] WHERE ID <> -1 ORDER BY STT ASC, ID ASC";
            using var r = cmd.ExecuteReader();
            var buf = new object[15];
            int stt = 1;
            dt.BeginLoadData();
            try
            {
                while (r.Read())
                {
                    token.ThrowIfCancellationRequested();
                    buf[0] = r.GetInt32(0);
                    buf[1] = stt++;                                   // STT hiển thị = thứ tự trong danh sách
                    for (int c = 1; c < 14; c++) buf[c + 1] = DocGiai(r, c);
                    if (((string)buf[12]).Equals("X", StringComparison.OrdinalIgnoreCase)) soDeNghi++;   // buf[12] = DeNghi
                    dt.Rows.Add(buf);
                }
            }
            finally { dt.EndLoadData(); }
            return (dt, soDeNghi);
        }
        /// <summary>Nạp lại lưới. Lần gọi mới hơn sẽ hủy lần cũ nên chỉ có kết quả mới nhất được ghi vào lưới.</summary>
        public async Task LoadDuLieuToanBoDanhSachBaNhatAsync(CancellationToken externalToken = default)
        {
            string db = DuongDanDb;
            if (string.IsNullOrWhiteSpace(db) || !File.Exists(db)) return;
            ChuanBiGiaoDienTruoc();   // idempotent: đảm bảo bộ lọc + cột lưới đã dựng TRƯỚC khi bind (KhoiTaoBoLoc không được chạy sau khi đã nạp đơn vị)
            // Hủy lần nạp trước (nếu còn chạy) để chỉ có 1 kết quả được ghi vào lưới
            var cu = _ctsLoadDanhSachBaNhat;
            var cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            _ctsLoadDanhSachBaNhat = cts;
            cu?.Cancel();
            cu?.Dispose();
            CancellationToken token = cts.Token;
            try
            {
                // Đọc phiên bản + CSDL + giải mã AES ở luồng nền, UI không bị chặn
                var (bang, dtBaNhat, soDeNghi) = await Task.Run(() =>
                {
                    var b = ChonBang();
                    var (d, n) = DocBangBaNhat(b.DanhSach, token);
                    return (b, d, n);
                }, token);
                // Nếu trong lúc chờ có lần gọi mới hơn hoặc form đã đóng thì bỏ kết quả này
                token.ThrowIfCancellationRequested();
                var g = kryptonDataGridView1;
                if (IsDisposed || g.IsDisposed) return;
                _bang = bang;
                _soDeNghi = soDeNghi;
                // Tắt vẽ trong lúc bind để lưới không nhấp nháy từng bước
                TatVe(g);
                try
                {
                    g.DataSource = dtBaNhat;     // cột đã dựng sẵn, chỉ đổi nguồn dữ liệu
                    NapDuLieuBoLoc(dtBaNhat);    // dựng lại danh sách đơn vị, GIỮ lựa chọn cũ, áp lại bộ lọc + cập nhật thanh trạng thái + tỷ lệ
                }
                finally
                {
                    BatVe(g);
                }
                DaLoadDuLieu = true;             // lưới đã có dữ liệu thật (Form55 dựa vào đây để biết còn phải nạp lần đầu hay không)
            }
            catch (OperationCanceledException)
            {
                // Lần nạp này bị hủy vì có lần gọi mới hơn -> bỏ qua im lặng.
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi nạp bảng Ba Nhất: {ex}");
            }
        }
        private static Dictionary<int, string> DocTinhTrang(string bang)
        {
            var kq = new Dictionary<int, string>();
            using var cn = MoKetNoi(true);
            using var cmd = cn.CreateCommand();
            cmd.CommandText = $"SELECT ID, TinhTrang FROM [{bang}] WHERE ID <> -1";
            using var rd = cmd.ExecuteReader();
            while (rd.Read()) kq[rd.GetInt32(0)] = DocGiai(rd, 1);
            return kq;
        }
        /// <summary>
        /// Chỉ vá cột "Tình trạng" của các dòng thật sự đổi (đọc 1 cột thay vì cả bảng, lưới không bị bind lại => không chớp,
        /// không mất dòng đang chọn). Dùng sau CapNhatTinhTrangSoVangAsync.
        /// </summary>
        private async Task LamMoiTinhTrangTrenLuoiAsync()
        {
            try
            {
                string bang = TenBangDanhSachBaNhat;
                var moi = await Task.Run(() => DocTinhTrang(bang));
                var g = kryptonDataGridView1;
                if (IsDisposed || g.IsDisposed || g.DataSource is not DataTable dt) return;
                // Gom các dòng đổi trước: thường là 0 dòng -> không đụng tới lưới. Có đổi thì tắt vẽ 1 lần
                // (mỗi lần gán ô bắn ListChanged -> lưới vẽ lại + DataView lọc lại theo từng dòng).
                List<(DataRow Row, string TinhTrang)>? doi = null;
                foreach (DataRow r in dt.Rows)
                {
                    if (r["ID"] is int id && moi.TryGetValue(id, out string? tt)
                        && !string.Equals(r["TinhTrang"] as string ?? "", tt, StringComparison.Ordinal))
                        (doi ??= new()).Add((r, tt));
                }
                if (doi == null) return;
                TatVe(g);
                try { foreach (var (r, tt) in doi) r["TinhTrang"] = tt; }
                finally { BatVe(g); }
            }
            catch (Exception ex) { Debug.WriteLine("[LamMoiTinhTrang] " + ex.Message); }
        }
        // ===================== ĐỒNG BỘ DANH SÁCH LOẠI 1 -> BẢNG BA NHẤT =====================
        public Task DongBoDuLieuLoai1SangBaNhatAsync() => DongBoLoai1CoThayDoiAsync();
        private async Task<bool> DongBoLoai1CoThayDoiAsync()
        {
            await _khoaDongBo.WaitAsync();
            try
            {
                bool coThayDoi = await Task.Run(() => DongBoLoai1());
                if (coThayDoi)
                    Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Đồng bộ danh sách Loại 1 sang bảng Ba Nhất", $"Bảng: {TenBangDanhSachBaNhat}");
                return coThayDoi;
            }
            finally { _khoaDongBo.Release(); }
        }
        // So khớp theo SỐ HIỆU ĐÃ GIẢI MÃ (MaHoa dùng IV ngẫu nhiên nên so chuỗi mã hóa sẽ sai ngay khi một nơi nào đó mã hóa lại Số hiệu:
        // người đó bị coi là "mới" và bản cũ - kèm Đề nghị, Thành tích - bị xóa). Sửa/xóa theo ID của bảng đích.
        // Trả về true nếu thật sự có ghi gì vào CSDL.
        private static bool DongBoLoai1()
        {
            string db = DuongDanDb;
            if (string.IsNullOrWhiteSpace(db) || !File.Exists(db)) return false;
            string bang = ChonBang().DanhSach;
            try
            {
                using var cn = MoKetNoi(false);
                // 1. Nguồn: người Loại 1 trong DanhSach. Khóa = Số hiệu giải mã; giá trị giữ nguyên chuỗi MÃ HÓA để chép sang bảng đích
                var nguon = new Dictionary<string, (int Stt, string ShEnc, string[] V)>(StringComparer.OrdinalIgnoreCase);
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT STT, SoHieu, HoVaTen, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu FROM {Module_TaiKhoan.BangDanhSach} WHERE ID <> -1";
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        if (!DocGiai(rd, 9).Equals(Module_HeThong.Loai_1, StringComparison.OrdinalIgnoreCase)) continue;
                        var v = new string[9];   // HoVaTen, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu
                        for (int i = 0; i < 9; i++) v[i] = DocChuoi(rd, i + 2);
                        string shEnc = DocChuoi(rd, 1);
                        int stt = rd.IsDBNull(0) ? 0 : rd.GetInt32(0);
                        nguon[SafeGiaiMa(shEnc)] = (stt, shEnc, v);
                    }
                }
                // 2. Đích: đọc 1 lần. Trùng Số hiệu thì giữ dòng đầu (ID nhỏ nhất), các dòng thừa đưa vào danh sách xóa
                var dich = new Dictionary<string, (int Id, string[] V)>(StringComparer.OrdinalIgnoreCase);
                var canXoa = new List<int>();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT ID, SoHieu, HoVaTen, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu FROM [{bang}] WHERE ID <> -1 ORDER BY ID ASC";
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        int id = rd.GetInt32(0);
                        string sh = DocGiai(rd, 1);
                        if (dich.ContainsKey(sh)) { canXoa.Add(id); continue; }
                        var v = new string[9];
                        for (int i = 0; i < 9; i++) v[i] = DocChuoi(rd, i + 2);
                        dich[sh] = (id, v);
                    }
                }
                // 3. Tính chênh lệch trong RAM
                var canThem = new List<(int Stt, string ShEnc, string[] V)>();
                var canSua = new List<(int Id, string[] V)>();
                foreach (var kv in nguon)
                {
                    if (!dich.TryGetValue(kv.Key, out var d)) canThem.Add((kv.Value.Stt, kv.Value.ShEnc, kv.Value.V));
                    else if (!GiongNhau(d.V, kv.Value.V)) canSua.Add((d.Id, kv.Value.V));
                }
                foreach (var kv in dich)
                    if (!nguon.ContainsKey(kv.Key)) canXoa.Add(kv.Value.Id);
                if (canThem.Count == 0 && canSua.Count == 0 && canXoa.Count == 0) return false;   // không đổi gì -> thoát luôn
                using var tx = cn.BeginTransaction();
                if (canThem.Count > 0)
                {
                    string rong = Module_BaoMatAES.MaHoa("");
                    using var ins = cn.CreateCommand();
                    ins.Transaction = tx;
                    ins.CommandText = $@"INSERT INTO [{bang}]
(STT,HoVaTen,SoHieu,NamSinh,QueQuan,NgayVaoCAND,CapBac,ChucVu,DonVi,PhanLoai,GhiChu,DeNghi,ThanhTich,TinhTrang)
VALUES(@stt,@p0,@sh,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@r,@r,@r)";
                    var pStt = ins.Parameters.Add("@stt", SqliteType.Integer);
                    var pSh = ins.Parameters.Add("@sh", SqliteType.Text);
                    var pv = new SqliteParameter[9];
                    for (int i = 0; i < 9; i++) pv[i] = ins.Parameters.Add($"@p{i}", SqliteType.Text);
                    ins.Parameters.AddWithValue("@r", rong);
                    foreach (var (stt, shEnc, v) in canThem)
                    {
                        pStt.Value = stt;
                        pSh.Value = shEnc;
                        for (int i = 0; i < 9; i++) pv[i].Value = v[i];
                        ins.ExecuteNonQuery();
                    }
                }
                if (canSua.Count > 0)
                {
                    using var upd = cn.CreateCommand();
                    upd.Transaction = tx;
                    upd.CommandText = $@"UPDATE [{bang}] SET HoVaTen=@p0,NamSinh=@p1,QueQuan=@p2,NgayVaoCAND=@p3,
CapBac=@p4,ChucVu=@p5,DonVi=@p6,PhanLoai=@p7,GhiChu=@p8 WHERE ID=@id";
                    var pId = upd.Parameters.Add("@id", SqliteType.Integer);
                    var pv = new SqliteParameter[9];
                    for (int i = 0; i < 9; i++) pv[i] = upd.Parameters.Add($"@p{i}", SqliteType.Text);
                    foreach (var (id, v) in canSua)
                    {
                        pId.Value = id;
                        for (int i = 0; i < 9; i++) pv[i].Value = v[i];
                        upd.ExecuteNonQuery();
                    }
                }
                if (canXoa.Count > 0)
                {
                    using var del = cn.CreateCommand();
                    del.Transaction = tx;
                    del.CommandText = $"DELETE FROM [{bang}] WHERE ID=@id";
                    var pId = del.Parameters.Add("@id", SqliteType.Integer);
                    foreach (int id in canXoa) { pId.Value = id; del.ExecuteNonQuery(); }
                }
                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi đồng bộ Ba Nhất: " + ex.Message);
                return false;
            }
        }
        // Bằng nhau về NỘI DUNG: chuỗi mã hóa trùng thì khỏi giải mã; khác chuỗi mới so bản giải mã
        // (chỉ khác IV ngẫu nhiên thì không tính là thay đổi => không ghi thừa xuống CSDL)
        private static bool GiongNhau(string[] a, string[] b)
        {
            for (int i = 0; i < a.Length; i++)
            {
                if (string.Equals(a[i], b[i], StringComparison.Ordinal)) continue;
                if (!string.Equals(SafeGiaiMa(a[i]), SafeGiaiMa(b[i]), StringComparison.Ordinal)) return false;
            }
            return true;
        }
        // ===================== THANH TRẠNG THÁI =====================
        private void CapNhatTrangThaiDuoiNen(bool tinhLaiTyLe = false)
        {
            int tong = 0, soHienThi = 0;
            bool dangLoc = false;
            if (kryptonDataGridView1.DataSource is DataTable dt)
            {
                tong = dt.Rows.Count;
                dangLoc = dt.DefaultView.RowFilter!.Length > 0;   // bộ lọc đang bật khi RowFilter khác rỗng
                soHienThi = dt.DefaultView.Count;
            }
            string dc = Module_HeThong.Tu_dong_chi;
            if (dangLoc)
            {
                // Đang lọc: Tổng quân số | Kết quả tìm kiếm
                toolStripStatusLabel1.ForeColor = Color.Blue;
                toolStripStatusLabel1.Text = $"Tổng quân số: {tong} {dc} | Kết quả tìm kiếm: {soHienThi} {dc}";
            }
            else
            {
                toolStripStatusLabel1.ForeColor = SystemColors.ControlText;
                toolStripStatusLabel1.Text =
                    tong == 0 ? "Chưa có cán bộ, chiến sĩ nào được đề nghị biểu dương trong phong trào thi đua Ba Nhất"
                    : _soDeNghi == 0 ? $"Tổng cộng: {tong} {dc}."
                    : $"Tổng cộng: {tong} {dc}, đề nghị: {_soDeNghi} {dc}.";
            }
            if (tinhLaiTyLe)   // chỉ tính lại khi dữ liệu thật sự đổi (nạp, lưu, nhập). Module tự gộp các lần gọi chồng nhau.
                _ = Module_BaNhat.TinhToanVaHienThiTyLeBaNhatAsync(toolStripStatusLabel2_TyLeBaNhat);
        }
        // Hiện thông báo tạm rồi tự trả về dòng thống kê MÀ KHÔNG chặn luồng thao tác (bản cũ await Task.Delay làm nút bị khóa thêm).
        // Thông báo mới hơn sẽ vô hiệu hóa lượt trả về của thông báo cũ.
        private int _tokenThongBao;
        private async void ThongBaoTam(string noiDung, int ms = 1500)
        {
            try
            {
                int token = ++_tokenThongBao;
                toolStripStatusLabel1.ForeColor = SystemColors.ControlText;
                toolStripStatusLabel1.Text = noiDung;
                await Task.Delay(ms);
                if (token == _tokenThongBao && !IsDisposed) CapNhatTrangThaiDuoiNen(false);
            }
            catch (Exception ex) { Debug.WriteLine("[ThongBaoTam] " + ex.Message); }
        }
        private void TinhLaiSoDeNghi()
        {
            int n = 0;
            if (kryptonDataGridView1.DataSource is DataTable dt)
                foreach (DataRow r in dt.Rows)
                    if (r["DeNghi"] is string s && s.Trim().Equals("X", StringComparison.OrdinalIgnoreCase)) n++;
            _soDeNghi = n;
        }
        // ===================== BỘ LỌC: Tên / Đơn vị / Đề nghị =====================
        // Toàn bộ form đọc lựa chọn theo SelectedIndex / SelectedItem, KHÔNG so sánh chuỗi hiển thị (.Text).
        private bool _dangNapBoLoc;                    // true khi đang nạp/đặt lại bộ lọc bằng code -> chặn các lượt lọc lẻ tẻ
        private bool _daGanSuKienBoLoc;                // sự kiện lọc chỉ được gắn đúng 1 lần
        private System.Windows.Forms.Timer? _timerLoc; // debounce ô gõ tên
        /// <summary>
        /// Gọi 1 lần ở đầu Load: dựng sẵn mục "Tất cả" cho 2 combobox và gắn sự kiện lọc đúng 1 lần.
        /// </summary>
        private void KhoiTaoBoLoc()
        {
            if (!_daGanSuKienBoLoc)
            {
                _daGanSuKienBoLoc = true;
                textBox_TimKiemTheoTen.TextChanged -= textBox_TimKiemTheoTen_TextChanged;
                textBox_TimKiemTheoTen.TextChanged += textBox_TimKiemTheoTen_TextChanged;
                comboBox_TimKiemDonVi.SelectedIndexChanged -= comboBox_TimKiemDonVi_SelectedIndexChanged;
                comboBox_TimKiemDonVi.SelectedIndexChanged += comboBox_TimKiemDonVi_SelectedIndexChanged;
                // comboBox1_DeNghi dùng chung handler lọc với combobox Đơn vị
                comboBox1_DeNghi.SelectedIndexChanged -= comboBox_TimKiemDonVi_SelectedIndexChanged;
                comboBox1_DeNghi.SelectedIndexChanged += comboBox_TimKiemDonVi_SelectedIndexChanged;
                comboBox_LocGhiChu.SelectedIndexChanged -= comboBox_TimKiemDonVi_SelectedIndexChanged;
                comboBox_LocGhiChu.SelectedIndexChanged += comboBox_TimKiemDonVi_SelectedIndexChanged;
            }
            _dangNapBoLoc = true;
            try
            {
                comboBox_TimKiemDonVi.Items.Clear();
                comboBox_TimKiemDonVi.Items.Add(TenTatCa);
                comboBox_TimKiemDonVi.SelectedIndex = 0;
                comboBox1_DeNghi.Items.Clear();
                comboBox1_DeNghi.Items.AddRange(new object[] { TenTatCa, TenDeNghi, TenKhongDeNghi });
                comboBox1_DeNghi.SelectedIndex = 0;
                comboBox_LocGhiChu.Items.Clear();
                comboBox_LocGhiChu.Items.Add(TenTatCa);
                comboBox_LocGhiChu.SelectedIndex = 0;
            }
            finally { _dangNapBoLoc = false; }
        }
        /// <summary>
        /// Gọi ngay sau khi bảng dữ liệu mới được gán vào lưới. Bảng mới luôn chưa có RowFilter, nên hàm này:
        /// (1) dựng lại danh sách đơn vị từ dữ liệu mới, (2) GIỮ lựa chọn người dùng đang dùng (ô tên, đơn vị, đề nghị),
        /// (3) áp lại bộ lọc đó lên bảng mới đúng 1 lần -> các ô lọc và lưới không bao giờ lệch nhau.
        /// </summary>
        private void NapDuLieuBoLoc(DataTable dt)
        {
            // Danh sách đơn vị theo thứ tự xuất hiện trên lưới (đúng thứ tự STT), không trùng
            // (không phân biệt hoa/thường - khớp cách RowFilter so sánh)
            var dsDonVi = new List<string>();
            var daCo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // Ghi chú: các giá trị khác nhau của cột GhiChu (bỏ trùng, bỏ ô trống), xếp A-Z cho dễ tìm
            var dsGhiChu = new List<string>();
            var daCoGhiChu = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow row in dt.Rows)
            {
                string dv = (row["DonVi"] as string)?.Trim() ?? "";
                if (dv.Length > 0 && daCo.Add(dv)) dsDonVi.Add(dv);
                string gc = (row["GhiChu"] as string)?.Trim() ?? "";
                if (gc.Length > 0 && daCoGhiChu.Add(gc)) dsGhiChu.Add(gc);
            }
            dsGhiChu.Sort(StringComparer.CurrentCultureIgnoreCase);
            CapNhatDanhSachDonVi(dsDonVi);
            CapNhatDanhSachGhiChu(dsGhiChu);
            ThucHienLocDuLieu(tinhLaiTyLe: true); // áp lại trạng thái các ô lọc lên bảng mới + cập nhật thanh trạng thái + tính lại tỷ lệ
        }
        /// <summary>
        /// Đổ danh sách đơn vị vào combobox: mục 0 luôn là "Tất cả", các mục sau là đơn vị.
        /// Giữ nguyên đơn vị đang chọn nếu nó vẫn còn trong danh sách mới, ngược lại về "Tất cả".
        /// Không tự lọc (cờ _dangNapBoLoc chặn sự kiện) - nơi gọi quyết định có lọc hay không.
        /// </summary>
        private void CapNhatDanhSachDonVi(List<string> dsDonVi)
        {
            string? donViDangChon = comboBox_TimKiemDonVi.SelectedIndex > 0
                ? comboBox_TimKiemDonVi.SelectedItem?.ToString()
                : null;
            _dangNapBoLoc = true;
            try
            {
                comboBox_TimKiemDonVi.BeginUpdate();
                try
                {
                    comboBox_TimKiemDonVi.Items.Clear();
                    comboBox_TimKiemDonVi.Items.Add(TenTatCa);
                    foreach (var dv in dsDonVi) comboBox_TimKiemDonVi.Items.Add(dv);
                    int viTri = donViDangChon == null
                        ? -1
                        : dsDonVi.FindIndex(x => string.Equals(x, donViDangChon, StringComparison.OrdinalIgnoreCase));
                    comboBox_TimKiemDonVi.SelectedIndex = viTri >= 0 ? viTri + 1 : 0;
                }
                finally { comboBox_TimKiemDonVi.EndUpdate(); }
                // Combobox Đề nghị là danh sách cố định (đã dựng ở KhoiTaoBoLoc) -> giữ nguyên lựa chọn hiện tại
                if (comboBox1_DeNghi.Items.Count == 0)
                {
                    comboBox1_DeNghi.Items.AddRange(new object[] { TenTatCa, TenDeNghi, TenKhongDeNghi });
                    comboBox1_DeNghi.SelectedIndex = 0;
                }
            }
            finally { _dangNapBoLoc = false; }
        }
        /// <summary>
        /// Đổ danh sách Ghi chú vào combobox lọc: mục 0 là "Tất cả". Giữ lựa chọn cũ nếu vẫn còn, ngược lại về "Tất cả".
        /// Không tự lọc (cờ _dangNapBoLoc chặn sự kiện) - NapDuLieuBoLoc lọc 1 lần sau khi đổ xong.
        /// </summary>
        private void CapNhatDanhSachGhiChu(List<string> dsGhiChu)
        {
            var cb = comboBox_LocGhiChu;
            string? dangChon = cb.SelectedIndex > 0 ? cb.SelectedItem?.ToString() : null;
            _dangNapBoLoc = true;
            try
            {
                cb.BeginUpdate();
                try
                {
                    cb.Items.Clear();
                    cb.Items.Add(TenTatCa);
                    foreach (var gc in dsGhiChu) cb.Items.Add(gc);
                    int viTri = dangChon == null
                        ? -1
                        : dsGhiChu.FindIndex(x => string.Equals(x, dangChon, StringComparison.OrdinalIgnoreCase));
                    cb.SelectedIndex = viTri >= 0 ? viTri + 1 : 0;
                    // Ghi chú dài: danh sách sổ xuống (OComboWebF4) tự nới rộng theo mục dài nhất, không cần đặt DropDownWidth
                }
                finally { cb.EndUpdate(); }
            }
            finally { _dangNapBoLoc = false; }
        }
        // ===== Debounce ô gõ tìm kiếm =====
        private void KhoiDongLocTre()
        {
            if (_dangNapBoLoc || IsDisposed) return;
            if (_timerLoc == null)
            {
                _timerLoc = new System.Windows.Forms.Timer { Interval = 150 };
                _timerLoc.Tick += (s, ev) => ThucHienLocDuLieu(); // ThucHienLocDuLieu tự dừng timer
            }
            _timerLoc.Stop();
            _timerLoc.Start();
        }
        /// <summary>Escape ký tự đặc biệt của LIKE trong DataView.RowFilter: * % [ ]</summary>
        private static string EscapeLikeRowFilter(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new System.Text.StringBuilder(s.Length + 8);
            foreach (char ch in s)
            {
                if (ch == '*' || ch == '%' || ch == '[' || ch == ']') sb.Append('[').Append(ch).Append(']');
                else sb.Append(ch);
            }
            return sb.ToString();
        }
        /// <summary>
        /// Trả về giá trị đang chọn của combobox lọc; null nghĩa là "Tất cả" (không lọc).
        /// Ưu tiên SelectedIndex/SelectedItem vì ComboBox.Text có thể chưa kịp cập nhật bên trong SelectedIndexChanged.
        /// </summary>
        private static string? LayGiaTriLoc(int chiSo, object? mucDangChon, string? chuoiHienThi)
        {
            if (chiSo == 0) return null; // mục 0 luôn là "Tất cả"
            string s = (chiSo > 0 ? mucDangChon?.ToString() : chuoiHienThi)?.Trim() ?? "";
            return s.Length == 0 || s.Equals(TenTatCa, StringComparison.OrdinalIgnoreCase) ? null : s;
        }
        /// <summary>
        /// Đọc trạng thái 3 ô lọc -> dựng 1 chuỗi RowFilter -> áp lên bảng đang bind -> cập nhật thanh trạng thái.
        /// Luôn lọc theo ĐỦ 3 ô cùng lúc (AND), nên gọi từ ô nào kết quả cũng như nhau.
        /// </summary>
        private void ThucHienLocDuLieu(bool tinhLaiTyLe = false)
        {
            _timerLoc?.Stop(); // hủy lượt lọc trễ đang chờ (nếu có)
            if (_dangNapBoLoc || IsDisposed) return;
            if (kryptonDataGridView1.DataSource is not DataTable dt) return;
            var dieuKien = new List<string>(4);
            // 1) Tên: chứa chuỗi đã gõ (đã escape ký tự đặc biệt của LIKE và dấu nháy đơn)
            string ten = textBox_TimKiemTheoTen.Text.Trim();
            if (ten.Length > 0)
                dieuKien.Add($"HoVaTen LIKE '%{EscapeLikeRowFilter(ten.Replace("'", "''"))}%'");
            // 2) Đơn vị: "Tất cả" -> bỏ qua điều kiện này
            string? donVi = LayGiaTriLoc(
                comboBox_TimKiemDonVi.SelectedIndex, comboBox_TimKiemDonVi.SelectedItem, comboBox_TimKiemDonVi.Text);
            if (donVi != null)
                dieuKien.Add($"DonVi = '{donVi.Replace("'", "''")}'");
            // 3) Đề nghị: 0 = Tất cả, 1 = Đề nghị, 2 = Không
            switch (comboBox1_DeNghi.SelectedIndex)
            {
                case 1: dieuKien.Add("DeNghi = 'X'"); break;
                case 2: dieuKien.Add("(DeNghi IS NULL OR DeNghi <> 'X')"); break;
            }
            // 4) Ghi chú: "Tất cả" -> bỏ qua; còn lại khớp đúng giá trị (RowFilter mặc định không phân biệt hoa/thường)
            // (đọc theo chỉ số: mục 0 = "Tất cả"; không so chữ vì một ghi chú có thể trùng chữ "Tất cả")
            string? ghiChu = comboBox_LocGhiChu.SelectedIndex > 0 ? comboBox_LocGhiChu.SelectedItem?.ToString() : null;
            if (!string.IsNullOrEmpty(ghiChu))
                dieuKien.Add($"GhiChu = '{ghiChu.Replace("'", "''")}'");
            string boLoc = string.Join(" AND ", dieuKien);
            try
            {
                dt.DefaultView.RowFilter = boLoc;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[LOC_ERR] " + ex.Message + " | " + boLoc);
                dt.DefaultView.RowFilter = string.Empty;
            }
            // Thanh trạng thái do một hàm duy nhất quyết định (tự nhận biết đang lọc hay không)
            CapNhatTrangThaiDuoiNen(tinhLaiTyLe);
        }
        // Ô gõ tên: lọc trễ 150ms để gõ nhanh không bị giật
        private void textBox_TimKiemTheoTen_TextChanged(object? sender, EventArgs e) => KhoiDongLocTre();
        // Chọn đơn vị (và cả combobox Đề nghị - dùng chung handler): lọc ngay
        private void comboBox_TimKiemDonVi_SelectedIndexChanged(object? sender, EventArgs e) => ThucHienLocDuLieu();
        // Nút "Xóa bộ lọc": đưa cả 3 ô về mặc định và lọc lại đúng 1 lần
        private void kryptonButton_LamMoiCacOTimKiem_Click(object? sender, EventArgs e)
        {
            DatLaiBoLoc();
            textBox_TimKiemTheoTen.Focus(); // sẵn sàng gõ tìm kiếm mới
        }
        /// <summary>
        /// Đặt lại bộ lọc: ô tên rỗng, đơn vị = "Tất cả", đề nghị = "Tất cả", rồi lọc đúng 1 lần.
        /// Cờ _dangNapBoLoc chặn 3 sự kiện Text/SelectedIndexChanged bắn lọc lẻ tẻ giữa chừng,
        /// còn timer debounce được dừng trước để lượt lọc trễ cũ không chạy chen sau khi đã xóa.
        /// </summary>
        private void DatLaiBoLoc()
        {
            _timerLoc?.Stop();
            _dangNapBoLoc = true;
            try
            {
                textBox_TimKiemTheoTen.Text = string.Empty;
                if (comboBox_TimKiemDonVi.Items.Count > 0) comboBox_TimKiemDonVi.SelectedIndex = 0;
                if (comboBox1_DeNghi.Items.Count > 0) comboBox1_DeNghi.SelectedIndex = 0;
                if (comboBox_LocGhiChu.Items.Count > 0) comboBox_LocGhiChu.SelectedIndex = 0;
            }
            finally { _dangNapBoLoc = false; }
            ThucHienLocDuLieu(); // RowFilter rỗng -> lưới hiện đủ danh sách, thanh trạng thái về "Tổng cộng..."
        }
        // ===================== Ô THÀNH TÍCH =====================
        // Tự chọn "Đề nghị" khi người dùng gõ thành tích + cập nhật đếm ký tự + hiện/ẩn nút cỡ chữ
        private void richTextBox1_ThanhTich_TextChanged(object? sender, EventArgs e)
        {
            KiemTraHienThiNutCoChu();
            CapNhatSoLuongKyTu();
            // Bắt buộc phải có Focused: chỉ kích hoạt khi người dùng trực tiếp gõ vào ô.
            // Bỏ qua nếu TextChanged phát sinh do hàm CellClick đổ dữ liệu lên.
            if (!richTextBox1_ThanhTich.Focused) return;
            // TextLength không cấp phát chuỗi (Text.Length thì có, mỗi lần gõ phím)
            bool coChu = richTextBox1_ThanhTich.TextLength > 0 && !string.IsNullOrWhiteSpace(richTextBox1_ThanhTich.Text);
            comboBox_DeNghiBaNhat.SelectedIndex = coChu ? 1 : 0;   // 1 = "Đề nghị", 0 = rỗng/không đề nghị
        }
        private void CapNhatSoLuongKyTu()
        {
            int soKyTu = richTextBox1_ThanhTich.TextLength;
            if (soKyTu == 0)
            {
                label17_SoLuongKyTu.Visible = false;   // rỗng: ẩn nhãn
                return;
            }
            label17_SoLuongKyTu.Visible = true;
            label17_SoLuongKyTu.Text = $"Số lượng: {soKyTu} /{Module_BaNhat.GioiHanToiDa} ký tự...";
            label17_SoLuongKyTu.ForeColor = soKyTu > Module_BaNhat.GioiHanToiDa ? Color.Red : Color.Green;
        }
        private void KiemTraHienThiNutCoChu()
        {
            // Có nội dung (bỏ qua khoảng trắng/xuống dòng thừa) thì mới hiện 2 nút cỡ chữ
            bool coNoiDung = richTextBox1_ThanhTich.TextLength > 0 && !string.IsNullOrWhiteSpace(richTextBox1_ThanhTich.Text);
            kryptonButton2_TangCoChuRichText.Visible = coNoiDung;
            kryptonButton2_GiamCoChuRichText.Visible = coNoiDung;
        }
        private void ThayDoiCoChuRichText(float delta)
        {
            Font fontHienTai = richTextBox1_ThanhTich.Font;
            float kichThuocMoi = Math.Clamp(fontHienTai.Size + delta, RichText_MinFontSize, RichText_MaxFontSize);
            if (Math.Abs(kichThuocMoi - fontHienTai.Size) < 0.01f) return;   // không tạo Font mới nếu không đổi
            richTextBox1_ThanhTich.SuspendLayout();
            try
            {
                var fontMoi = new Font(fontHienTai.FontFamily, kichThuocMoi, fontHienTai.Style, GraphicsUnit.Point);
                var fontCu = _fontRichTuTao;
                richTextBox1_ThanhTich.Font = fontMoi;
                _fontRichTuTao = fontMoi;
                fontCu?.Dispose();    // chỉ giải phóng Font do chính hàm này tạo (Font kế thừa/Designer có thể đang dùng chung)
                richTextBox1_ThanhTich.Focus();
            }
            finally
            {
                richTextBox1_ThanhTich.ResumeLayout();
            }
        }
        private void kryptonButton2_TangCoChuRichText_Click(object? sender, EventArgs e) => ThayDoiCoChuRichText(RichText_FontStep);
        private void kryptonButton2_GiamCoChuRichText_Click(object? sender, EventArgs e) => ThayDoiCoChuRichText(-RichText_FontStep);
        // ===================== LƯU ĐỀ NGHỊ / THÀNH TÍCH =====================
        private static int LuuDeNghi(string bang, int id, string deNghi, string thanhTich)
        {
            using var cn = MoKetNoi(false);
            using var cmd = cn.CreateCommand();
            cmd.CommandText = $"UPDATE [{bang}] SET DeNghi = @DeNghi, ThanhTich = @ThanhTich WHERE ID = @ID";
            cmd.Parameters.AddWithValue("@DeNghi", Module_BaoMatAES.MaHoa(deNghi));
            cmd.Parameters.AddWithValue("@ThanhTich", Module_BaoMatAES.MaHoa(thanhTich));
            cmd.Parameters.AddWithValue("@ID", id);
            return cmd.ExecuteNonQuery();
        }
        private async void kryptonButton_LuuDataDeNghi_Click(object? sender, EventArgs e)
        {
            // Kiểm tra đã chọn CBCS chưa
            if (textBox_SoHieu.Tag == null ||
                !int.TryParse(Convert.ToString(textBox_SoHieu.Tag, CultureInfo.InvariantCulture), out int idBaNhat))
            {
                MessageBox.Show("Vui lòng chọn một CBCS trong danh sách trước khi lưu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // Kiểm tra giới hạn số lượng ký tự trước khi lưu
            int soKyTu = richTextBox1_ThanhTich.TextLength;
            if (soKyTu > Module_BaNhat.GioiHanToiDa)
            {
                MessageBox.Show(
                    $"Nội dung thành tích hiện có {soKyTu} ký tự,\nvượt quá giới hạn cho phép ({Module_BaNhat.GioiHanToiDa} ký tự).\nVui lòng rút gọn nội dung trước khi lưu.",
                    "Không thể lưu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                richTextBox1_ThanhTich.Focus();
                return;
            }
            // Lưu tên gốc, đổi tên nút, khóa nút chống click đúp
            var nut = kryptonButton_LuuDataDeNghi;
            string tenGoc = nut.Text;
            nut.Text = "Đang lưu...";
            nut.Enabled = false;
            nut.Refresh();       // ép giao diện vẽ lại tên nút ngay lập tức
            try
            {
                // Combobox rỗng -> xóa sạch ô thành tích trước khi lưu
                if (string.IsNullOrWhiteSpace(comboBox_DeNghiBaNhat.Text))
                    richTextBox1_ThanhTich.Clear();
                string deNghi = comboBox_DeNghiBaNhat.Text.Trim().Equals(TenDeNghi, StringComparison.OrdinalIgnoreCase) ? "X" : "";
                string thanhTich = richTextBox1_ThanhTich.Text.Trim();
                string bang = TenBangDanhSachBaNhat;
                // Mã hóa + ghi ở luồng nền (SQLite async của Microsoft.Data.Sqlite thực chất chạy đồng bộ => await trực tiếp vẫn chặn UI)
                int soDong = await Task.Run(() => LuuDeNghi(bang, idBaNhat, deNghi, thanhTich));
                if (soDong == 0)
                {
                    MessageBox.Show("Không tìm thấy CBCS cần cập nhật.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Lưu đề nghị Ba Nhất",
                    $"{textBox_HoVaTen.Text.Trim()} (Số hiệu: {textBox_SoHieu.Text.Trim()}) | Đề nghị: {(deNghi == "X" ? "Có" : "Không")} | Thành tích: {thanhTich.Length} ký tự");
                // Đồng bộ lên lưới: cập nhật thẳng DataRow nguồn (không qua ô lưới). Khi bộ lọc "Đề nghị/Không"
                // đang bật, dòng có thể biến mất khỏi lưới ngay khi DeNghi đổi -> ghi qua ô lưới sẽ mất cập nhật thứ hai.
                CapNhatDongNguonSauKhiLuu(idBaNhat, deNghi, thanhTich);
                TinhLaiSoDeNghi();
                CapNhatTrangThaiDuoiNen(true);                       // thống kê + tính lại tỷ lệ (đúng 1 lần)
                ThongBaoTam("Đã lưu thành công!", 1200);
                // Lưới đã được cập nhật ngay trong RAM nên KHÔNG báo Form55 "cần nạp lại" (bản cũ báo => lần quay lại sau đó phải đồng bộ + nạp lại cả bảng)
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                MessageBox.Show("Lỗi khi lưu dữ liệu:\n\n" + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                nut.Text = tenGoc;
                nut.Enabled = true;
                nut.Refresh();
            }
        }
        /// <summary>
        /// Ghi DeNghi + ThanhTich mới vào DataRow của bảng nguồn trong 1 lần BeginEdit/EndEdit
        /// (DataView chỉ lọc lại đúng 1 lần, lưới và bộ lọc luôn nhất quán).
        /// </summary>
        private void CapNhatDongNguonSauKhiLuu(int id, string deNghi, string thanhTich)
        {
            if (kryptonDataGridView1.DataSource is not DataTable dt) return;
            foreach (DataRow r in dt.Rows)
            {
                if (r["ID"] is not int v || v != id) continue;
                r.BeginEdit();
                r["DeNghi"] = deNghi;
                r["ThanhTich"] = thanhTich; // thành tích có thể đã rỗng
                r.EndEdit();
                break;
            }
        }
        // ===================== LÀM MỚI =====================
        private void DatTienTrinh(int giaTri)
        {
            if (toolStripProgressBar1_LamMoi.IsDisposed) return;
            toolStripProgressBar1_LamMoi.Value = Math.Clamp(giaTri, 0, 100);
        }
        private async void kryptonButton_RefershCSDL_Click(object? sender, EventArgs e)
        {
            // Chặn click kép khi đang chạy
            if (_dangLamMoiCSDL) return;
            _dangLamMoiCSDL = true;
            string tenNutGoc = kryptonButton_RefershCSDL.Text;
            kryptonButton_RefershCSDL.Text = "Đang chạy...";
            kryptonButton_RefershCSDL.Enabled = false;
            toolStripStatusLabel1.ForeColor = SystemColors.ControlText;
            toolStripStatusLabel1.Text = "Đang làm mới dữ liệu, vui lòng chờ...";
            toolStripStatusLabel1.Owner?.Update();
            toolStripProgressBar1_LamMoi.Visible = true;
            DatTienTrinh(5);
            UseWaitCursor = true;
            bool loi = false;
            try
            {
                // Tiến trình thật theo từng bước (bản cũ chạy 1 luồng nền chỉ để giả lập phần trăm)
                await DongBoLoai1CoThayDoiAsync();
                DatTienTrinh(40);
                await Module_BaNhat.CapNhatTinhTrangSoVangAsync();
                DatTienTrinh(65);
                await LoadDuLieuToanBoDanhSachBaNhatAsync();   // bind lưới + thống kê + tính lại tỷ lệ (đúng 1 lần)
                DatTienTrinh(100);
                await Task.Delay(200);                         // giữ 100% một nhịp để người dùng thấy
            }
            catch (Exception ex)
            {
                loi = true;
                toolStripStatusLabel1.Text = "Lỗi khi làm mới dữ liệu!";
                MessageBox.Show("Lỗi khi làm mới dữ liệu:\n\n" + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.WriteLine(ex);
            }
            finally
            {
                UseWaitCursor = false;
                toolStripProgressBar1_LamMoi.Visible = false;
                toolStripProgressBar1_LamMoi.Value = 0;
                kryptonButton_RefershCSDL.Text = tenNutGoc;
                kryptonButton_RefershCSDL.Enabled = true;
                _dangLamMoiCSDL = false;
                if (!loi) CapNhatTrangThaiDuoiNen(false);      // trả dòng "Đang làm mới..." về thống kê, kể cả khi nạp lưới bị bỏ qua
            }
        }
        private void lamMoiHeThong_Click(object? sender, EventArgs e) => kryptonButton_RefershCSDL.PerformClick();
        private void xoaTimKiem_Click(object? sender, EventArgs e) => kryptonButton_LamMoiCacOTimKiem.PerformClick();
        // ===================== THÀNH TÍCH TẬP THỂ =====================
        private void kryptonButton1_ThanhTichTapThe_Click(object? sender, EventArgs e)
        {
            var frm = Application.OpenForms["Form43_TomTatThanhTichBaNhat"];
            if (frm != null)
            {
                if (frm.WindowState == FormWindowState.Minimized) frm.WindowState = FormWindowState.Normal;
                frm.Activate();
                return;
            }
            string tenGoc = kryptonButton1_ThanhTichTapThe.Text;
            kryptonButton1_ThanhTichTapThe.Text = "Trang đang mở...";
            try
            {
                using var newForm = new Form43_TomTatThanhTichBaNhat { StartPosition = FormStartPosition.CenterScreen };
                newForm.ShowDialog(this);
            }
            finally
            {
                kryptonButton1_ThanhTichTapThe.Text = tenGoc;
                CapNhatTrangThaiDuoiNen(true);   // Form43 có thể đổi dữ liệu tập thể -> tính lại tỷ lệ
            }
        }
        private void toolStripMenuItem_ThiDuaTapThe_Click(object? sender, EventArgs e) => kryptonButton1_ThanhTichTapThe.PerformClick();
        // ===================== XÓA TOÀN BỘ DANH SÁCH BA NHẤT =====================
        // Xóa + reset bộ đếm ID trong 1 transaction. Không còn VACUUM: VACUUM viết lại cả tệp .db và khóa độc quyền
        // (làm đứng giao diện nhiều giây), trong khi xóa vài nghìn dòng chữ ngắn không đáng kể về dung lượng.
        private static void XoaTatCaBangBaNhat(string bang)
        {
            using var cn = MoKetNoi(false);
            using var tx = cn.BeginTransaction();
            using (var cmd = cn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = $"DELETE FROM [{bang}]";
                cmd.ExecuteNonQuery();
            }
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM sqlite_sequence WHERE name = @n";
                cmd.Parameters.AddWithValue("@n", bang);
                cmd.ExecuteNonQuery();
            }
            catch { /* chưa có sqlite_sequence (bảng chưa từng sinh ID tự tăng) -> bỏ qua */ }
            tx.Commit();
        }
        private async void toolStripMenuItem_XoaChonTatCa_Click(object? sender, EventArgs e)
        {
            // 1. Xác minh quyền admin
            DialogResult kq;
            using (var frm = new Form24_XacMinhAdmin())
            {
                frm.TopMost = true;
                frm.StartPosition = FormStartPosition.CenterScreen;
                kq = frm.ShowDialog();
            }
            if (kq != DialogResult.OK) return;   // Dừng nếu nhập sai mật khẩu hoặc bấm Hủy
            try
            {
                string bang = TenBangDanhSachBaNhat;
                await Task.Run(() => XoaTatCaBangBaNhat(bang));
                // 2. Đồng bộ lại từ danh sách Loại 1, cập nhật tình trạng rồi nạp lưới (mỗi bước đúng 1 lần)
                await DongBoLoai1CoThayDoiAsync();
                await Module_BaNhat.CapNhatTinhTrangSoVangAsync();
                await LoadDuLieuToanBoDanhSachBaNhatAsync();
                ThongBaoTam("Đã xóa và đồng bộ lại dữ liệu thành công!", 1500);
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Xóa toàn bộ dữ liệu bảng Danh sách Ba Nhất", $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi khi xóa toàn bộ dữ liệu Ba Nhất: " + ex.Message);
                MessageBox.Show("Đã xảy ra lỗi khi xóa dữ liệu:\n" + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // ===================== LƯU CBCS ĐỀ NGHỊ VÀO SỔ VÀNG =====================
        // 1 hàng "Đề nghị" đọc từ bảng Ba Nhất. Enc = các cột ở dạng MÃ HÓA, chép nguyên sang Sổ vàng:
        // [0]HoVaTen [1]SoHieu [2]NamSinh [3]QueQuan [4]NgayVaoCAND [5]CapBac [6]ChucVu [7]DonVi [8]PhanLoai [9]GhiChu [10]ThanhTich
        private sealed record HangDeNghi(string SoHieuGiai, string[] Enc);
        private static List<HangDeNghi> DocUngVienSoVang(string bangBaNhat)
        {
            var ds = new List<HangDeNghi>();
            using var cn = MoKetNoi(true);
            using var cmd = cn.CreateCommand();
            cmd.CommandText = $@"SELECT DeNghi, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu, ThanhTich
                                 FROM [{bangBaNhat}] WHERE ID <> -1 ORDER BY STT ASC, ID ASC";
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                if (!DocGiai(rd, 0).Equals("X", StringComparison.OrdinalIgnoreCase)) continue;
                var enc = new string[11];
                for (int i = 0; i < 11; i++) enc[i] = DocChuoi(rd, i + 1);
                string soHieu = SafeGiaiMa(enc[1]);
                if (soHieu.Length == 0) continue;     // không có Số hiệu thì không thể ghi sổ
                ds.Add(new HangDeNghi(soHieu, enc));
            }
            return ds;
        }
        // Trả về (tháng/năm, lỗi, biểu tượng). Có lỗi thì tháng/năm = null.
        private static (string? thangNam, string? loi, MessageBoxIcon icon) DocThangNamCongNhan()
        {
            using var cn = MoKetNoi(true);
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT Thang, Nam FROM ThongTin WHERE ID = 1 LIMIT 1";
            using var rd = cmd.ExecuteReader();
            if (!rd.Read())
                return (null, "Không tìm thấy thông tin Tháng / Năm trong bảng ThongTin.", MessageBoxIcon.Warning);
            string thangEnc = DocChuoi(rd, 0), namEnc = DocChuoi(rd, 1);
            string thang = SafeGiaiMa(thangEnc), nam = SafeGiaiMa(namEnc);
            // GiaiMa không ném lỗi mà trả rỗng khi giải mã hỏng: có chuỗi mã hóa nhưng giải ra rỗng => dữ liệu hỏng
            if ((!string.IsNullOrWhiteSpace(thangEnc) && thang.Length == 0) || (!string.IsNullOrWhiteSpace(namEnc) && nam.Length == 0))
                return (null, "Không thể giải mã thông tin Tháng / Năm trong bảng ThongTin.", MessageBoxIcon.Error);
            if (thang.Length == 0 || nam.Length == 0)
                return (null, "Thông tin Tháng / Năm trong CSDL đang trống hoặc không hợp lệ.", MessageBoxIcon.Warning);
            return ($"{thang}/{nam}", null, MessageBoxIcon.None);
        }
        // So khớp Sổ vàng theo SỐ HIỆU ĐÃ GIẢI MÃ, sửa theo ID (bản cũ so chuỗi mã hóa nên lệch khi Số hiệu bên Sổ vàng
        // được mã hóa lại ở Form44 / nạp Excel => chèn trùng người). Toàn bộ trong 1 transaction.
        private static int GhiVaoSoVang(string bangSoVang, List<HangDeNghi> ds, string thangCongNhan)
        {
            using var cn = MoKetNoi(false);
            var idTheoSoHieu = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = $"SELECT ID, SoHieu FROM [{bangSoVang}] WHERE ID <> -1";
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    string sh = DocGiai(rd, 1);
                    if (sh.Length > 0) idTheoSoHieu[sh] = rd.GetInt32(0);
                }
            }
            int sttTiepTheo;
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = $"SELECT IFNULL(MAX(STT), 0) FROM [{bangSoVang}]";
                sttTiepTheo = Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) + 1;
            }
            string thangEnc = Module_BaoMatAES.MaHoa(thangCongNhan);
            string rong = Module_BaoMatAES.MaHoa(string.Empty);
            using var tx = cn.BeginTransaction();
            using var upd = cn.CreateCommand();
            upd.Transaction = tx;
            upd.CommandText = $@"UPDATE [{bangSoVang}] SET HoVaTen=@p0, NamSinh=@p1, QueQuan=@p2, NgayVaoCAND=@p3, CapBac=@p4,
                                 ChucVu=@p5, DonVi=@p6, PhanLoai=@p7, GhiChu=@p8, ThanhTich=@p9, ThangCongNhan=@tcn WHERE ID=@id";
            var pvUpd = new SqliteParameter[10];
            for (int i = 0; i < 10; i++) pvUpd[i] = upd.Parameters.Add($"@p{i}", SqliteType.Text);
            var pTcnUpd = upd.Parameters.Add("@tcn", SqliteType.Text);
            var pIdUpd = upd.Parameters.Add("@id", SqliteType.Integer);
            pTcnUpd.Value = thangEnc;
            using var ins = cn.CreateCommand();
            ins.Transaction = tx;
            ins.CommandText = $@"INSERT INTO [{bangSoVang}]
(STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu, ThanhTich, ThongBaoTrungDoan, SoTTTrongSo, ThangCongNhan)
VALUES (@stt, @p0, @sh, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @tb, @sott, @tcn)";
            var pSttIns = ins.Parameters.Add("@stt", SqliteType.Integer);
            var pShIns = ins.Parameters.Add("@sh", SqliteType.Text);
            var pvIns = new SqliteParameter[10];
            for (int i = 0; i < 10; i++) pvIns[i] = ins.Parameters.Add($"@p{i}", SqliteType.Text);
            ins.Parameters.AddWithValue("@tb", rong);
            ins.Parameters.AddWithValue("@sott", rong);
            ins.Parameters.AddWithValue("@tcn", thangEnc);
            var daXuLy = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int dem = 0;
            foreach (var h in ds)
            {
                if (!daXuLy.Add(h.SoHieuGiai)) continue;       // trùng Số hiệu trong danh sách đề nghị: chỉ ghi 1 lần
                // Thứ tự p0..p9 = HoVaTen, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu, ThanhTich
                if (idTheoSoHieu.TryGetValue(h.SoHieuGiai, out int id))
                {
                    pvUpd[0].Value = h.Enc[0];
                    for (int i = 1; i < 10; i++) pvUpd[i].Value = h.Enc[i + 1];
                    pIdUpd.Value = id;
                    upd.ExecuteNonQuery();
                }
                else
                {
                    pSttIns.Value = sttTiepTheo++;
                    pShIns.Value = h.Enc[1];
                    pvIns[0].Value = h.Enc[0];
                    for (int i = 1; i < 10; i++) pvIns[i].Value = h.Enc[i + 1];
                    ins.ExecuteNonQuery();
                }
                dem++;
            }
            tx.Commit();
            return dem;
        }
        private async void toolStripMenuItem_luuVaoSoVang_Click(object? sender, EventArgs e)
        {
            try
            {
                var bang = LayBang();
                // 1. Đọc người đã đề nghị (luồng nền; không giữ kết nối mở trong lúc hộp thoại hiện)
                var dsDeNghi = await Task.Run(() => DocUngVienSoVang(bang.DanhSach));
                if (dsDeNghi.Count == 0)
                {
                    MessageBox.Show($"Không tìm thấy {Module_HeThong.Tu_dong_chi} nào được đánh dấu 'Đề nghị' (X)!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var xacNhan = MessageBox.Show(
                    $"Tìm thấy {dsDeNghi.Count:N0} {Module_HeThong.Tu_dong_chi} được đề nghị vào Sổ vàng.\n\nBạn có muốn tiếp tục ghi tên vào Sổ vàng?",
                    "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (xacNhan != DialogResult.Yes) return;
                // 2. Tháng công nhận
                var (thangNam, loiThang, iconLoi) = await Task.Run(() => DocThangNamCongNhan());
                if (thangNam == null)
                {
                    MessageBox.Show(loiThang, "Lỗi dữ liệu", MessageBoxButtons.OK, iconLoi);
                    return;
                }
                // 3. Ghi Sổ vàng
                int soThanhCong = await Task.Run(() => GhiVaoSoVang(bang.SoVang, dsDeNghi, thangNam));
                ThongBaoTam($"Đã chuyển thành công {soThanhCong:N0} {Module_HeThong.Tu_dong_chi} vào Sổ vàng [{bang.SoVang}]!", 2500);
                await Module_BaNhat.CapNhatTinhTrangSoVangAsync();
                await LamMoiTinhTrangTrenLuoiAsync();             // cột "Tình trạng" cập nhật ngay, không cần nạp lại cả lưới
                BaoForm55CanNapLaiSoVang();
                Module_NhatKy.GhiNhatKy(
                    Module_TaiKhoan.TenTaiKhoan_RAM,
                    $"Xuất hàng loạt {soThanhCong:N0} CBCS vào Sổ vàng ({bang.SoVang}), Tháng công nhận: {thangNam}",
                    DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể chuyển dữ liệu vào Sổ vàng.\n\n" + $"Chi tiết: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async void toolStripMenuItem_MoSoVang_Click(object? sender, EventArgs e)
        {
            try
            {
                // Tìm Form55 đang bao bọc bên ngoài, ra lệnh chuyển sang hiển thị Form44 trong panelContent
                var form55 = Application.OpenForms.OfType<Form55_QuanLyHeThongThiDuaBaNhat>().FirstOrDefault();
                if (form55 != null && !form55.IsDisposed)
                    await form55.MoForm44Async();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[MoSoVang] " + ex);
            }
        }
        // ===================== NHẬP EXCEL =====================
        private async void toolStripMenuItem_NhapDuLieu_Click(object? sender, EventArgs e)
        {
            try
            {
                using var ofd = new OpenFileDialog
                {
                    Title = "Chọn tệp Excel danh sách gốc",
                    Filter = "Excel Files (*.xlsx, *.xlsm)|*.xlsx;*.xlsm",
                    Multiselect = false,
                    CheckFileExists = true,
                    CheckPathExists = true
                };
                if (ofd.ShowDialog() != DialogResult.OK) return;
                // Module nhận lại số dòng thành công (truyền 'this' để Module biết form nào gọi)
                int soDongThanhCong = await Module_BaNhat.NhapDuLieuTuTepExcelVaoCSDL(this, ofd.FileName, DuongDanDb);
                // Cập nhật tình trạng Sổ vàng rồi nạp lưới (lưới nạp xong tự cập nhật thanh trạng thái + tỷ lệ)
                await Module_BaNhat.CapNhatTinhTrangSoVangAsync();
                await LoadDuLieuToanBoDanhSachBaNhatAsync();
                ThongBaoTam($"Nhập thành công: {soDongThanhCong} {Module_HeThong.Tu_dong_chi} vào CSDL hệ thống", 2000);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi nhập dữ liệu: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // ===================== XUẤT EXCEL =====================
        //public static string VietHoaChuDau(string input)
        //{
            //if (string.IsNullOrWhiteSpace(input))
                //return "";
            //// 1. Chuyển toàn bộ chuỗi về chữ thường (để đảm bảo "ĐOÀN" thành "đoàn")
            //string s = input.Trim().ToLower();
            //// 2. Viết hoa chữ cái đầu tiên và nối với phần còn lại đã ở dạng chữ thường
            //return char.ToUpper(s[0]) + s.Substring(1);
        //}
        /// <summary>
        /// Hàm gán giá trị và gạch chân 1/2 độ dài chính giữa cho ô Excel (Chuẩn thể thức)
        /// </summary>
        private static void GanTenDonViVaGachChan(IXLRange range, string text)
        {
            if (range == null || string.IsNullOrWhiteSpace(text)) return;
            // 1. Merge range và định dạng căn giữa ô
            range.Merge();
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            // 2. Làm sạch cell
            var cell = range.Cell(1, 1);
            cell.Value = string.Empty;
            var richText = cell.GetRichText();
            richText.ClearText();
            int totalLen = text.Length;
            int underlineLen;
            int start;
            // 3. XỬ LÝ ĐẶC BIỆT CHO CÁC TRƯỜNG HỢP CHUỖI TỪ 1 ĐẾN 3 KÝ TỰ
            if (totalLen <= 2)
            {
                start = 0;
                underlineLen = totalLen;
            }
            else if (totalLen == 3)
            {
                start = 1;
                underlineLen = 1; // Đúng 3 ký tự (ví dụ "K02") -> Chỉ gạch ký tự thứ 2
            }
            else // TRƯỜNG HỢP TỔNG QUÁT (>= 4 KÝ TỰ)
            {
                if (totalLen % 2 != 0) // LẺ (5, 7, 9...)
                {
                    int half = (int)Math.Round(totalLen / 2.0);
                    underlineLen = (half % 2 != 0) ? half : half + 1;
                    if (underlineLen > totalLen) underlineLen = totalLen;
                }
                else // CHẴN (4, 6, 8, 10...)
                {
                    int half = (int)Math.Round(totalLen / 2.0);
                    underlineLen = (half % 2 == 0) ? half : half + 1;
                    if (underlineLen > totalLen) underlineLen = totalLen;
                }
                // Vị trí bắt đầu cân bằng 2 bên
                start = (totalLen - underlineLen) / 2;
            }
            // 4. TIẾN HÀNH GÁN RICH TEXT THEO 3 ĐOẠN (TRÁI - GIỮA - PHẢI)
            string leftText = text.Substring(0, start);
            if (!string.IsNullOrEmpty(leftText))
            {
                richText.AddText(leftText)
                        .SetFontName(Module_HeThong.Font_Times_New_Roman)
                        .SetFontSize(12)
                        .SetBold(true)
                        .SetUnderline(XLFontUnderlineValues.None);
            }
            string midText = text.Substring(start, underlineLen);
            if (!string.IsNullOrEmpty(midText))
            {
                richText.AddText(midText)
                        .SetFontName(Module_HeThong.Font_Times_New_Roman)
                        .SetFontSize(12)
                        .SetBold(true)
                        .SetUnderline(XLFontUnderlineValues.Single);
            }
            string rightText = text.Substring(start + underlineLen);
            if (!string.IsNullOrEmpty(rightText))
            {
                richText.AddText(rightText)
                        .SetFontName(Module_HeThong.Font_Times_New_Roman)
                        .SetFontSize(12)
                        .SetBold(true)
                        .SetUnderline(XLFontUnderlineValues.None);
            }
        }
        // Đọc toàn bộ danh sách (không điều kiện) ở luồng nền, đã giải mã
        private static List<DanhSachGocBaNhatDTO> DocDanhSachGoc(string bang)
        {
            var ds = new List<DanhSachGocBaNhatDTO>();
            using var cn = MoKetNoi(true);
            using var cmd = cn.CreateCommand();
            cmd.CommandText = $@"SELECT ID, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu, DeNghi, ThanhTich
                                 FROM [{bang}] WHERE ID <> -1 ORDER BY STT ASC, ID ASC";
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                ds.Add(new DanhSachGocBaNhatDTO
                {
                    ID = rd.GetInt32(0),
                    HoVaTen = DocGiai(rd, 1),
                    SoHieu = DocGiai(rd, 2),
                    NamSinh = DocGiai(rd, 3),
                    QueQuan = DocGiai(rd, 4),
                    NgayVaoCAND = DocGiai(rd, 5),
                    CapBac = DocGiai(rd, 6),
                    ChucVu = DocGiai(rd, 7),
                    DonVi = DocGiai(rd, 8),
                    PhanLoai = DocGiai(rd, 9),
                    GhiChu = DocGiai(rd, 10),
                    DeNghi = DocGiai(rd, 11),
                    ThanhTich = DocGiai(rd, 12)
                });
            }
            return ds;
        }
        private async void toolStripMenuItem5_XuatDanhSachGoc_Click(object? sender, EventArgs e)
        {
            var muc = toolStripMenuItem5_XuatDanhSachGoc;
            string? textBanDau = muc.Text;
            Image? anhBanDau = muc.Image;
            // Kiểm tra file mẫu theo logic gốc (dù gốc khởi tạo Workbook mới nhưng vẫn kiểm tra)
            string templatePath = Module_DanduongGPS.DuongDanCSDL4ex;
            if (!File.Exists(templatePath))
            {
                MessageBox.Show("Không tìm thấy file mẫu Excel tại: " + templatePath, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // Yêu cầu chọn nơi lưu TRƯỚC KHI hiện màn hình chờ
            using var sfd = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = $"DanhSach_CSDL_BaNhat_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                Title = "Chọn nơi lưu file Danh sách gốc"
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;
            string filePathLuu = sfd.FileName;
            var cho = new ManHinhCho(this, "Đang đọc và tạo tệp excel, vui lòng đợi...");
            try
            {
                muc.Enabled = false;
                muc.Text = "Đang xử lý...";
                muc.Image = null;
                await cho.HienAsync();
                // Đọc + giải mã ở luồng nền: màn hình chờ vẫn chạy mượt (bản cũ đọc ngay trên luồng UI)
                string bang = TenBangDanhSachBaNhat;
                var danhSachGoc = await Task.Run(() => DocDanhSachGoc(bang));
                await Module_BaNhat.XuatDuLieuRaTepExcelTuCSDL_ToiUu(danhSachGoc, filePathLuu);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống khi xuất file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                cho.Dispose();
                muc.Text = textBanDau;
                muc.Image = anhBanDau;
                muc.Enabled = true;
            }
        }
        // Danh sách đề nghị (DeNghi = X) + tổng số Loại 1 + tỷ lệ quy định. Dùng chung cho "Xuất danh sách đề nghị" và "Tờ trình".
        private sealed record DuLieuDeNghi(List<DeNghiBaNhatDTO> DanhSach, int TongLoai1, int TyLe);
        private static DuLieuDeNghi DocDuLieuDeNghi(string bang, int tyLeMacDinh)
        {
            int tyLe = tyLeMacDinh, tongLoai1 = 0;
            var ds = new List<DeNghiBaNhatDTO>();
            using var cn = MoKetNoi(true);
            // 1. Tỷ lệ quy định (đọc không được thì giữ mặc định, không bị TryParse ghi đè về 0)
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "SELECT TyLe FROM QuyDinhTyLe_BaNhat WHERE ID = 1";
                if (cmd.ExecuteScalar() is string s && int.TryParse(SafeGiaiMa(s), out int t)) tyLe = t;
            }
            // 2. Tổng số "Loại 1" từ bảng DanhSach làm mốc tính chỉ tiêu
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = $"SELECT PhanLoai FROM {Module_TaiKhoan.BangDanhSach}";
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                    if (DocGiai(rd, 0).Equals(Module_HeThong.Loai_1, StringComparison.OrdinalIgnoreCase)) tongLoai1++;
            }
            // 3. Danh sách đề nghị: giải mã DeNghi trước, chỉ giải mã các cột còn lại với dòng thật sự là "X"
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = $@"SELECT ID, DeNghi, HoVaTen, SoHieu, NamSinh, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, ThanhTich
                                     FROM [{bang}] WHERE ID <> -1 ORDER BY STT ASC, ID ASC";
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    if (!DocGiai(rd, 1).Equals("X", StringComparison.OrdinalIgnoreCase)) continue;
                    ds.Add(new DeNghiBaNhatDTO
                    {
                        ID = rd.GetInt32(0),
                        HoVaTen = DocGiai(rd, 2),
                        SoHieu = DocGiai(rd, 3),
                        NamSinh = DocGiai(rd, 4),
                        NgayVaoCAND = DocGiai(rd, 5),
                        CapBac = DocGiai(rd, 6),
                        ChucVu = DocGiai(rd, 7),
                        DonVi = DocGiai(rd, 8),
                        PhanLoai = DocGiai(rd, 9),
                        ThanhTich = DocGiai(rd, 10)
                    });
                }
            }
            return new DuLieuDeNghi(ds, tongLoai1, tyLe);
        }
        private async void ToolStripMenuItem_XuatDanhSach_Click(object? sender, EventArgs e)
        {
            var muc = ToolStripMenuItem_XuatDanhSach;
            string? textBanDau = muc.Text;
            Image? anhBanDau = muc.Image;
            try
            {
                muc.Enabled = false;
                muc.Text = "Đang xử lý...";
                muc.Image = null;
                string bang = TenBangDanhSachBaNhat;
                var kqDoc = await Task.Run(() => DocDuLieuDeNghi(bang, 0));
                var danhSachDeNghi = kqDoc.DanhSach;
                // LOGIC KIỂM TRA TỶ LỆ & CẢNH BÁO
                int soLuongChiTieu = (int)Math.Round(kqDoc.TongLoai1 * (kqDoc.TyLe / 100.0), MidpointRounding.AwayFromZero);
                if (danhSachDeNghi.Count < soLuongChiTieu)
                {
                    var dr = MessageBox.Show(
                        $"Chưa đạt tỷ lệ quy định!\nChỉ tiêu: {soLuongChiTieu} {Module_HeThong.Tu_dong_chi} ({kqDoc.TyLe}%).\nHiện tại chỉ có: {danhSachDeNghi.Count} {Module_HeThong.Tu_dong_chi}.\n\nBạn có muốn tiếp tục xuất báo cáo không?",
                        "Cảnh báo tỷ lệ", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (dr == DialogResult.No) return;
                }
                // Kiểm tra danh sách rỗng
                if (danhSachDeNghi.Count == 0)
                {
                    var result = MessageBox.Show(
                        "Hiện tại không có CBCS nào được đề nghị biểu dương.\nBạn có chắc chắn muốn xuất tệp Excel trống?",
                        "Cảnh báo dữ liệu trống", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (result != DialogResult.Yes) return;
                }
                // Truyền 'this' để Module biết form nào đang chạy (khóa form + màn hình chờ)
                await Module_BaNhat.XuatDanhSachBaNhatToExcelAsync(this, danhSachDeNghi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                muc.Text = textBanDau;
                muc.Image = anhBanDau;
                muc.Enabled = true;
            }
        }
        // Thông tin chung của tờ trình (đọc 1 lần ở luồng nền)
        private sealed record ThongTinToTrinh(string TenTrungDoan, string TenTieuDoan, string DiaDiem, string Ngay, string Thang, string Nam);
        private static ThongTinToTrinh DocThongTinToTrinh()
        {
            using var cn = MoKetNoi(true);
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT TenTrungDoan, TenTieuDoan, DiaDiem, Ngay, Thang, Nam FROM ThongTin WHERE ID = 1";
            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return new ThongTinToTrinh("", "", "", "", "", "");
            return new ThongTinToTrinh(DocGiai(rd, 0), DocGiai(rd, 1), DocGiai(rd, 2), DocGiai(rd, 3), DocGiai(rd, 4), DocGiai(rd, 5));
        }
        private async void toolStripMenuItem_ToTrinhBaNhat_Click(object? sender, EventArgs e)
        {
            string templatePath = Module_DanduongGPS.DuongDanCSDL4ex;
            if (!File.Exists(templatePath))
            {
                MessageBox.Show("Không tìm thấy file mẫu Excel tại: " + templatePath, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // UX: luôn mở mặc định là màn hình Desktop
            string thuMucMacDinh = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            using var fbd = new FolderBrowserDialog
            {
                Description = "Chọn thư mục để lưu Tờ trình Ba Nhất",
                ShowNewFolderButton = true,
                InitialDirectory = thuMucMacDinh,
                SelectedPath = thuMucMacDinh
            };
            if (fbd.ShowDialog() != DialogResult.OK) return;
            string directoryPath = fbd.SelectedPath;
            // Đếm và tự động đặt tên tệp theo số thứ tự
            string keyword = "TỜ TRÌNH ĐỀ NGHỊ BIỂU DƯƠNG BA NHẤT THÁNG";
            string[] existingSpecificFiles = Directory.GetFiles(directoryPath, $"*{keyword}*");
            int prefixNumber = existingSpecificFiles.Length > 0
                ? existingSpecificFiles.Length + 1                        // Đã có tờ trình cùng loại -> đếm tệp cùng loại + 1
                : Directory.GetFiles(directoryPath).Length + 1;           // Chưa có -> đếm mọi tệp trong thư mục + 1
            // Vòng lặp fail-safe: chống ghi đè khi người dùng xóa tệp lộn xộn gây trùng số
            string filePathLuu;
            while (true)
            {
                string fileName = $"{prefixNumber}. {keyword} {DateTime.Now:MM-yyyy}.xlsx";
                filePathLuu = Path.Combine(directoryPath, fileName);
                if (!File.Exists(filePathLuu)) break;
                prefixNumber++;
            }
            var cho = new ManHinhCho(this, "Đang khởi tạo tờ trình, vui lòng đợi...");
            try
            {
                await cho.HienAsync();
                string bang = TenBangDanhSachBaNhat;
                await Task.Run(() =>
                {
                    // 1. LẤY DỮ LIỆU TỪ CƠ SỞ DỮ LIỆU (chạy ngầm)
                    var tt = DocThongTinToTrinh();
                    var kqDoc = DocDuLieuDeNghi(bang, 30);     // 30 = tỷ lệ mặc định nếu không đọc được
                    var danhSachDeNghi = kqDoc.DanhSach;
                    int tongSoLoai1 = kqDoc.TongLoai1;
                    int tyLeQuyDinh = kqDoc.TyLe;
                    string tenTrungDoan = tt.TenTrungDoan, tenTieuDoan = tt.TenTieuDoan, diaDiem = tt.DiaDiem;
                    string ngay = tt.Ngay, thang = tt.Thang, nam = tt.Nam;
                    // 2. ĐỔ DỮ LIỆU RA EXCEL (tôn trọng tuyệt đối Template có sẵn)
                    using var wb = new XLWorkbook(templatePath);
                    // Kiểm tra và dọn dẹp Worksheet
                    if (!wb.Worksheets.TryGetWorksheet("TOTRINH_BANHAT", out var ws))
                        throw new Exception("Tệp mẫu không có sheet mang tên 'TOTRINH_BANHAT'. Vui lòng kiểm tra lại file mẫu.");
                    // Xóa tất cả các sheet khác (chỉ giữ lại duy nhất TOTRINH_BANHAT)
                    var sheetsToDelete = wb.Worksheets.Where(w => !w.Name.Equals("TOTRINH_BANHAT", StringComparison.OrdinalIgnoreCase)).Select(w => w.Name).ToList();
                    foreach (var sName in sheetsToDelete)
                        wb.Worksheets.Delete(sName);
                    // Đổ dữ liệu tĩnh vào khung có sẵn
                    ws.Cell("A1").Value = string.IsNullOrWhiteSpace(tenTieuDoan) ? tenTrungDoan.ToUpper() : $"{tenTrungDoan.ToUpper()}\n{tenTieuDoan.ToUpper()}".Trim();
                    // Định dạng và gạch chân chuẩn cho vùng A2:E2
                    string tenDonViBanHanh = "TỔ CHÍNH TRỊ"; // Hoặc lấy từ CSDL / tên tiểu đoàn / phòng ban
                    GanTenDonViVaGachChan(ws.Range("A2:E2"), tenDonViBanHanh);
                    ws.Cell("F3").Value = $"{diaDiem}, ngày {ngay} tháng {thang} năm {nam}";
                    // Vùng ô A5:J5 - tiêu đề tờ trình / đề nghị biểu dương
                    // Nếu thang/nam đọc từ CSDL bị rỗng, lấy tháng/năm hiện tại của hệ thống
                    string thangHienTai = string.IsNullOrWhiteSpace(thang) ? DateTime.Now.Month.ToString("00") : thang;
                    string namHienTai = string.IsNullOrWhiteSpace(nam) ? DateTime.Now.Year.ToString() : nam;
                    string tieuDeText = $"Đề nghị biểu dương gương điển hình trong thực hiện phong trào thi đua \"Ba nhất\" tháng {thangHienTai}/{namHienTai}";
                    var rangeTieuDe = ws.Range("A5:J5");
                    rangeTieuDe.Merge();
                    rangeTieuDe.Value = tieuDeText;
                    rangeTieuDe.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                    rangeTieuDe.Style.Font.FontSize = 13;
                    rangeTieuDe.Style.Font.Bold = true;
                    rangeTieuDe.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    rangeTieuDe.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    rangeTieuDe.Style.Alignment.WrapText = true;
                    ws.Row(5).Height = 28;
                    // Đổ danh sách dữ liệu động từ dòng 9
                    const int dongDau = 9;
                    // Chiếu DTO -> mảng dòng, nạp 1 lần bằng InsertData; định dạng theo VÙNG thay vì từng dòng
                    var dong = danhSachDeNghi
                        .Select((item, i) => new object?[] { i + 1, item.HoVaTen, item.SoHieu, item.NamSinh, item.NgayVaoCAND,
                            item.CapBac, item.ChucVu, item.DonVi, item.PhanLoai, "" /* Cột xin ý kiến để trống */ })
                        .ToList();
                    int startRow = dongDau + dong.Count;
                    if (dong.Count > 0)
                    {
                        int dongCuoi = startRow - 1;
                        Module_ExcelAnToan.NapDong(ws, dongDau, 1, dong);
                        Module_ExcelAnToan.KeVien(ws.Range(dongDau, 1, dongCuoi, 10));   // kẻ bảng giữ thiết kế table
                        ws.Range(dongDau, 1, dongCuoi, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        var cotHoTen = ws.Range(dongDau, 2, dongCuoi, 2).Style.Alignment;
                        cotHoTen.Horizontal = XLAlignmentHorizontalValues.Left;
                        cotHoTen.WrapText = true;
                        var vungGiua = ws.Range(dongDau, 3, dongCuoi, 10).Style.Alignment;   // căn giữa C..J
                        vungGiua.Horizontal = XLAlignmentHorizontalValues.Center;
                        vungGiua.Vertical = XLAlignmentVerticalValues.Center;
                        ws.Rows(dongDau, dongCuoi).Height = 20;
                    }
                    // Chốt "Tổng cộng" ngay dưới danh sách
                    var rngTongCong = ws.Range(startRow, 1, startRow, 10);
                    rngTongCong.Merge().Value = $"Tổng cộng: {danhSachDeNghi.Count:00} {Module_HeThong.Tu_dong_chi}/.";
                    rngTongCong.Style.Font.Bold = true;
                    rngTongCong.Style.Font.Italic = true;
                    rngTongCong.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    ws.Row(startRow).Height = 20;
                    // Khối thống kê
                    int soLuongChiTieu = (int)Math.Round(tongSoLoai1 * (tyLeQuyDinh / 100.0), MidpointRounding.AwayFromZero);
                    double tyLeHienTai = tongSoLoai1 > 0 ? Math.Round((double)danhSachDeNghi.Count / tongSoLoai1 * 100, 1) : 0;
                    string trangThai = danhSachDeNghi.Count == soLuongChiTieu
                        ? "Đạt tỷ lệ"
                        : (danhSachDeNghi.Count < soLuongChiTieu
                            ? $"Thiếu {soLuongChiTieu - danhSachDeNghi.Count} {Module_HeThong.Tu_dong_chi}"
                            : $"Thừa {danhSachDeNghi.Count - soLuongChiTieu} {Module_HeThong.Tu_dong_chi}");
                    int rowThongTin = startRow + 2;
                    var cellThongTin = ws.Cell(rowThongTin, 1);
                    ws.Range(rowThongTin, 1, rowThongTin + 5, 10).Merge(); // Gộp một vùng để chứa text
                    cellThongTin.Value = $"Tổng cộng: {tongSoLoai1} {Module_HeThong.Tu_dong_chi}, đề nghị: {danhSachDeNghi.Count} {Module_HeThong.Tu_dong_chi}.\n" +
                                         $"Chỉ tiêu \"Ba nhất\": {tyLeQuyDinh}% = {soLuongChiTieu} {Module_HeThong.Tu_dong_chi}\n" +
                                         $"Tỷ lệ hiện tại: {danhSachDeNghi.Count} {Module_HeThong.Tu_dong_chi} = {tyLeHienTai}%\n" +
                                         $"Kết luận: {trangThai}";
                    cellThongTin.Style.Font.Bold = true;
                    cellThongTin.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                    cellThongTin.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    cellThongTin.Style.Alignment.WrapText = true;
                    // Active sheet lại đúng "TOTRINH_BANHAT" trước khi lưu
                    ws.SetTabActive();
                    Module_ExcelAnToan.LuuAnToan(wb, filePathLuu);
                });
                // Tự động mở file excel ngay khi tạo xong
                if (File.Exists(filePathLuu))
                    Module_BaNhat.MoTepExcelThiDuaPhongTrao3Nhat(filePathLuu);
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Xuất tờ trình biểu dương Ba Nhất", $"Thành công. Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống khi xuất tờ trình: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                cho.Dispose();
            }
        }
        // ===================== THOÁT TRANG =====================
        private void kryptonButton1_Thoat_Click(object? sender, EventArgs e)
        {
            // 1. Tìm Form cha (Form2_FormCha)
            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            if (formCha == null || formCha.IsDisposed) return;
            // 2. Tìm PanelContainer trên Form cha
            if (formCha.Controls.Find("PanelContainer", true).FirstOrDefault() is not Panel panel || panel.IsDisposed) return;
            // 3. Ẩn Form55 đi thay vì Close/Dispose để GIỮ LẠI TRONG RAM
            var form55 = panel.Controls.OfType<Form55_QuanLyHeThongThiDuaBaNhat>().FirstOrDefault();
            if (form55 != null && !form55.IsDisposed)
                form55.Hide();
            // 4. Tìm Form6_XuLyData và hiển thị lại lên trước
            var form6 = panel.Controls.OfType<Form6_XuLyData>().FirstOrDefault();
            if (form6 != null && !form6.IsDisposed)
            {
                form6.Dock = DockStyle.Fill;
                form6.Show();
                form6.BringToFront();
            }
            // 5. Cập nhật lại tiêu đề hệ thống
            formCha.CapNhatTieuDe("Trang phân loại thi đua");
            if (formCha.Label1 != null)
                formCha.Label1.Text = "Trang phân loại thi đua";
        }
    }
}
