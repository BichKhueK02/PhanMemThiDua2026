///Phiên bản 5 - thêm đóng gói / nhập thư mục lưu trữ (Bansaoluu, LuuTruThiDua_LichSu); giữ nguyên Designer và tên control
//TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace PhanMemThiDua2026
{
    public partial class Form31_ChuyenGiaoDuLieu : Form
    {
        // ==================== HẰNG SỐ & DỮ LIỆU TĨNH ====================
        private const string LUA_CHON_TAT_CA = Module_HeThong.Tat_Ca;
        private const string TEN_THU_MUC_GOI = Module_ChuyenGiaoDuLieu.TEN_THU_MUC_GOI;
        private const string TIEN_TO_METADATA = Module_ChuyenGiaoDuLieu.TIEN_TO_METADATA;
        private const string DINH_DANG_THOI_GIAN = "dd-MM-yyyy HH:mm:ss";
        private const int THOI_GIAN_CHO_THANH_DEN_100_MS = 450;   // cho thanh chạy tới 100% rồi mới hiện hộp thoại
        private const int THOI_GIAN_AN_THANH_MS = 1500;
        private const int SO_LOI_TOI_DA_TRONG_HOP_THOAI = 15;
        private static readonly string[] TenCSDLMucTieu = { "csdl1", "csdl2", "csdl3", "csdl4" };
        private static readonly Dictionary<string, string> AnhXaTenDatabase = new(StringComparer.OrdinalIgnoreCase)
        {
            { "csdl1", "Database_1 (Danh tính & Cấu hình)" },
            { "csdl2", "Database_2 (Thông tin chung)" },
            { "csdl3", "Database_3 (Nhật ký hệ thống)" },
            { "csdl4", "Database_4 (Dữ liệu nghiệp vụ Thi đua)" }
        };
        private static readonly Color MauTieuDe = Color.Red;
        private static readonly Color MauGiaTri = Color.Green;
        // ==================== KIỂU DỮ LIỆU NỘI BỘ ====================
        /// <summary>Điều kiện để một bước được chạy.</summary>
        private enum DieuKienChay
        {
            Luon,           // luôn chạy (trừ khi nhóm của nó đã bị hủy do bước bắt buộc lỗi)
            NhomKhongLoi    // chỉ chạy khi MỌI bước trước đó trong cùng nhóm đều thành công
        }
        /// <summary>Một đơn vị công việc (xuất/nạp 1 bảng, sao lưu, vacuum...).</summary>
        /// <param name="Nhom">Nhóm (thường là tên CSDL). Lỗi ở bước BatBuoc chỉ hủy phần còn lại của nhóm, các nhóm khác vẫn chạy.</param>
        /// <param name="BatBuoc">Lỗi ở bước này hủy cả nhóm (vd: sao lưu thất bại thì không được nạp CSDL đó).</param>
        /// <param name="TinhThongKe">Có tính vào thống kê "x/y bảng" hay không.</param>
        /// <param name="LaTepLuuTru">Bước xuất/nạp MỘT tệp của thư mục lưu trữ (Bansaoluu, LuuTruThiDua_LichSu): thống kê riêng "x/y tệp lưu trữ".</param>
        private sealed record BuocXuLy(
            string Ten,
            Func<Task> ThucThi,
            string? Nhom = null,
            bool BatBuoc = false,
            bool TinhThongKe = true,
            DieuKienChay DieuKien = DieuKienChay.Luon,
            bool LaTepLuuTru = false);
        private sealed record MoTaTacVu(
            string HanhDongNhatKy,
            string ThaoTac,          // "xuất" | "nạp"
            string DongTuDangLam,    // "Đang kết xuất" | "Đang nạp"
            string TieuDeBaoCao,
            string? ViTriDich,       // hiển thị khi thành công (chỉ với xuất)
            bool GhiVaoCSDL);        // true = nạp dữ liệu vào CSDL (cảnh báo khi dừng giữa chừng)
        private sealed class KetQuaTacVu
        {
            public int TongSo { get; set; }
            public int ThanhCong { get; set; }
            public int TongTepLuuTru { get; set; }
            public int TepLuuTruThanhCong { get; set; }
            public bool BiHuy { get; set; }
            public List<string> DanhSachLoi { get; } = new();
        }
        /// <summary>
        /// Mục của ComboBox/CheckedListBox gắn kèm đường dẫn thật
        /// (thay cho việc suy đường dẫn từ chỉ số vốn rất dễ lệch khi hai danh sách khác nhau).
        /// </summary>
        private sealed class MucCSDL
        {
            public MucCSDL(string hienThi, string? duongDan)
            {
                HienThi = hienThi;
                DuongDan = duongDan;
            }
            public string HienThi { get; }
            public string? DuongDan { get; }
            public bool LaTatCa => DuongDan is null;
            public override string ToString() => HienThi;
        }
        private static readonly MucCSDL MucTatCa = new(LUA_CHON_TAT_CA, null);
        /// <summary>Mục "thư mục lưu trữ" trong danh sách tích chọn ở chế độ "Tất cả" (xuất kèm Bansaoluu / LuuTruThiDua_LichSu).</summary>
        private sealed class MucLuuTru
        {
            public MucLuuTru(GoiXuatLuuTru goi) => Goi = goi;
            public GoiXuatLuuTru Goi { get; }
            public override string ToString() =>
                $"Thư mục {Goi.Nhom.Ten} ({Goi.Tep.Count} tệp, {Module_ChuyenGiaoThuMucLuuTru.DinhDangDungLuong(Goi.TongByte)})";
        }
        // ==================== TRẠNG THÁI (chỉ truy cập từ luồng UI) ====================
        private readonly CancellationTokenSource? _ctsTaiThongTinDatabase;
        private CancellationTokenSource? _ctsQuet;
        private CancellationTokenSource? _ctsTacVu;
        private readonly List<string> _danhSachDuongDanThucTe = new(16); // CSDL có thể XUẤT
        private readonly List<string> _danhSachDuongDanNap = new(4);     // CSDL có gói JSON để NẠP
        private readonly List<GoiXuatLuuTru> _luuTruCoTheXuat = new(2);  // thư mục lưu trữ CÓ DỮ LIỆU để XUẤT kèm
        private readonly List<GoiNhapLuuTru> _luuTruCoTheNap = new(2);   // thư mục lưu trữ có trong gói trên Desktop (kể cả gói có cảnh báo)
        private Control[] _dieuKhienKhoaKhiBan = Array.Empty<Control>();
        private string _duongDanCSDL_Nguon = string.Empty;
        private bool _dangQuet;
        private bool _dangXuLyMigration;
        private bool _dangMoThuMuc;
        private bool _yeuCauDongForm;
        private bool _kichHoatForm12KhiDong;
        private bool _coDuLieuDeNap;       // có gói JSON hợp lệ ngoài Desktop hay không
        private bool _clbLaDanhSachBang;   // true: CheckedListBox đang chứa tên BẢNG; false: chứa tên CSDL (chế độ "Tất cả")
        private int _khoaSuKien;           // >0: đang cập nhật UI bằng code, bỏ qua sự kiện
        private int _doSauKhoaUI;          // đếm lồng nhau để khóa/mở khóa an toàn
        private int _phienNapThongTin;     // chống kết quả cũ ghi đè kết quả mới
        private int _phienThanhTienTrinh;  // chống ẩn nhầm thanh của tác vụ mới
        private bool DangBan => _dangXuLyMigration || _dangQuet;
        /// <summary>Form đã/đang đóng: mọi cập nhật giao diện phải dừng lại.</summary>
        private bool DaHuy => IsDisposed || Disposing;
        // ==================== KHỞI TẠO ====================
        public Form31_ChuyenGiaoDuLieu()
        {
            InitializeComponent();
            Module_BieuTuongNut.ApDung(this);   // biểu tượng nút: Hiện đại = tự vẽ, Cổ điển = ảnh gốc nhúng trong exe
            KhoiTaoFormCauHinh();
            DangKyChuoiSuKien();
            KhoiTaoToolTips();
            Module_GiaoDienForm31Web.ApDung(this);
        }
        public Form31_ChuyenGiaoDuLieu(CancellationTokenSource? ctsTaiThongTinDatabase) : this()
        {
            _ctsTaiThongTinDatabase = ctsTaiThongTinDatabase;
        }
        private void KhoiTaoFormCauHinh()
        {
            SuspendLayout();
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            _dieuKhienKhoaKhiBan = new Control[]
            {
                btn_QuetTimKiem, btn_XuatDuLieuJson, btn_NhapDuLieuJson, kryptonButton2_MoThuMuc,
                cbo_ChonCSDL_Nguon, checkBox1_ChonTatCaCacCSDL,
                comboBox1_CSDLDeNapVao, checkBox1_ChonTatCaCacCSDLDeNap,
                checkedListBox1_clb_DanhSachBang,
                chk_BackupTruocKhiChuyen, chk_XoaDuLieuCu
            };
            prb_TienTrinhChuyenGiao.Visible = false;
            chk_BackupTruocKhiChuyen.Checked = true;
            chk_XoaDuLieuCu.Checked = false;
            lbl_TrangThaiHoatDong.Text = "Sẵn sàng.";
            ApDungTrangThaiNap(); // chưa quét nên chưa có gói để nạp: khóa phần Nhập
            ResumeLayout(false);
        }
        private void KhoiTaoToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Gợi ý";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            toolTip1.InitialDelay = 300;
            toolTip1.AutoPopDelay = 5000;
            toolTip1.ReshowDelay = 100;
            toolTip1.ShowAlways = true;
            toolTip1.SetToolTip(btn_XuatDuLieuJson, "Xuất dữ liệu hệ thống ra tệp định dạng JSON");
            toolTip1.SetToolTip(kryptonButton2_MoThuMuc, "Mở thư mục chứa các tệp dữ liệu");
            toolTip1.SetToolTip(btn_NhapDuLieuJson, "Nhập (Import) dữ liệu từ tệp JSON vào hệ thống");
            toolTip1.SetToolTip(kryptonButton_Dong, "Đóng cửa sổ làm việc này");
            toolTip1.SetToolTip(btn_QuetTimKiem, "Quét và tìm kiếm tệp dữ liệu trong thư mục Desktop");
        }
        /// <summary>
        /// Chỉ đăng ký những sự kiện KHÔNG được gắn trong Designer.
        /// Luôn gỡ (-=) trước khi gắn (+=): nếu Designer đã gắn rồi thì handler vẫn chỉ chạy đúng một lần.
        /// </summary>
        private void DangKyChuoiSuKien()
        {
            cbo_ChonCSDL_Nguon.SelectedIndexChanged -= Cbo_ChonCSDL_Nguon_SelectedIndexChanged;
            cbo_ChonCSDL_Nguon.SelectedIndexChanged += Cbo_ChonCSDL_Nguon_SelectedIndexChanged;
            comboBox1_CSDLDeNapVao.SelectedIndexChanged -= ComboBox1_CSDLDeNapVao_SelectedIndexChanged;
            comboBox1_CSDLDeNapVao.SelectedIndexChanged += ComboBox1_CSDLDeNapVao_SelectedIndexChanged;
            btn_QuetTimKiem.Click -= btn_QuetTimKiem_Click;
            btn_QuetTimKiem.Click += btn_QuetTimKiem_Click;
            checkBox1_ChonTatCaCacCSDL.CheckedChanged -= CheckBox1_ChonTatCaCacCSDL_CheckedChanged;
            checkBox1_ChonTatCaCacCSDL.CheckedChanged += CheckBox1_ChonTatCaCacCSDL_CheckedChanged;
            checkBox1_ChonTatCaCacCSDLDeNap.CheckedChanged -= CheckBox1_ChonTatCaCacCSDLDeNap_CheckedChanged;
            checkBox1_ChonTatCaCacCSDLDeNap.CheckedChanged += CheckBox1_ChonTatCaCacCSDLDeNap_CheckedChanged;
        }
        private void Form31_ChuyenGiaoDuLieu_Load(object? sender, EventArgs e)
        {
            richTextBox1_ThongTinDatabaseDuocChon.Text =
                "Bạn chưa chọn cơ sở dữ liệu...\n" +
                "Vui lòng quét hoặc chọn một cơ sở dữ liệu để xem thông tin.";
        }
        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Update(); // vẽ xong khung hình đầu tiên rồi mới nạp dữ liệu: form hiện ra ngay, không khựng
            await ChayAnToanAsync(TaiDuLieuNenAsync, nameof(OnShown));
        }
        /// <summary>Mọi handler async void đều đi qua đây: ngoại lệ lọt ra khỏi async void sẽ làm sập cả ứng dụng.</summary>
        private async Task ChayAnToanAsync(Func<Task> tacVu, string ten)
        {
            try
            {
                await tacVu();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{ten}] {ex}");
                if (!DaHuy)
                    CapNhatTrangThaiHoatDong("Đã xảy ra lỗi không mong muốn. Vui lòng thử lại.");
            }
        }
        // ==================== KHÓA / MỞ KHÓA GIAO DIỆN ====================
        private void ComboBox1_CSDLDeNapVao_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_khoaSuKien > 0) return;
            // Đồng bộ ô "Chọn tất cả" với mục đang chọn, tránh ô tích "tất cả" nhưng ComboBox lại đang chọn một CSDL riêng lẻ.
            DongBoCheckBoxChonTatCaDeNap(comboBox1_CSDLDeNapVao.SelectedItem is MucCSDL { LaTatCa: true });
        }
        private void CheckBox1_ChonTatCaCacCSDLDeNap_CheckedChanged(object? sender, EventArgs e)
        {
            if (_khoaSuKien > 0 || _dangXuLyMigration || !_coDuLieuDeNap) return;
            if (!checkBox1_ChonTatCaCacCSDLDeNap.Checked && _danhSachDuongDanNap.Count == 0)
            {
                // Gói trên Desktop chỉ có thư mục lưu trữ (không có CSDL riêng lẻ để chọn): chỉ nhập được ở chế độ "Tất cả"
                DongBoCheckBoxChonTatCaDeNap(true);
                CapNhatTrangThaiHoatDong("Gói trên Desktop chỉ có thư mục lưu trữ nên phải nhập bằng 'Chọn tất cả'.");
                return;
            }
            CapNhatMucTatCa(comboBox1_CSDLDeNapVao, checkBox1_ChonTatCaCacCSDLDeNap.Checked);
        }
        private void DongBoCheckBoxChonTatCaDeNap(bool giaTri)
        {
            if (checkBox1_ChonTatCaCacCSDLDeNap.Checked == giaTri) return;
            _khoaSuKien++;
            try { checkBox1_ChonTatCaCacCSDLDeNap.Checked = giaTri; }
            finally { _khoaSuKien--; }
        }
        /// <summary>Thêm/bớt mục "Tất cả" ở đầu ComboBox mà không kích hoạt sự kiện.</summary>
        private void CapNhatMucTatCa(ComboBox cbo, bool bat)
        {
            _khoaSuKien++;
            try
            {
                if (bat)
                {
                    if (!cbo.Items.Contains(MucTatCa)) cbo.Items.Insert(0, MucTatCa);
                    cbo.SelectedItem = MucTatCa;
                }
                else
                {
                    cbo.Items.Remove(MucTatCa);
                    if (cbo.Items.Count > 0) cbo.SelectedIndex = 0;
                }
            }
            finally
            {
                _khoaSuKien--;
            }
        }
        private void KhoaUI()
        {
            if (_doSauKhoaUI++ == 0) ThietLapTrangThaiTuongTacUI(false);
        }
        private void MoKhoaUI()
        {
            if (_doSauKhoaUI > 0 && --_doSauKhoaUI == 0 && !IsDisposed)
                ThietLapTrangThaiTuongTacUI(true);
        }
        private void ThietLapTrangThaiTuongTacUI(bool trangThai)
        {
            foreach (Control c in _dieuKhienKhoaKhiBan) c.Enabled = trangThai;
            // Mở khóa hàng loạt ở trên làm bật lại cả nút Nạp khi KHÔNG có gói dữ liệu để nạp.
            if (trangThai) ApDungTrangThaiNap();
        }
        private void ApDungTrangThaiNap()
        {
            if (_doSauKhoaUI > 0) return; // đang khóa: để MoKhoaUI áp dụng sau
            comboBox1_CSDLDeNapVao.Enabled = _coDuLieuDeNap;
            checkBox1_ChonTatCaCacCSDLDeNap.Enabled = _coDuLieuDeNap;
            btn_NhapDuLieuJson.Enabled = _coDuLieuDeNap;
        }
        private void CapNhatTrangThaiHoatDong(string text)
        {
            if (IsDisposed || lbl_TrangThaiHoatDong.Text == text) return;
            lbl_TrangThaiHoatDong.Text = text;
        }
        // ==================== QUÉT & NẠP DANH SÁCH CSDL ====================
        private async void btn_QuetTimKiem_Click(object? sender, EventArgs e) =>
            await ChayAnToanAsync(TaiDuLieuNenAsync, nameof(btn_QuetTimKiem_Click));
        /// <summary>Quét các CSDL thực tế (Xuất từ hệ thống và Nạp từ Desktop) rồi nạp vào combobox tương ứng.</summary>
        private async Task TaiDuLieuNenAsync()
        {
            if (_dangQuet || _dangXuLyMigration || DaHuy) return;
            _dangQuet = true;
            using var cts = new CancellationTokenSource();
            _ctsQuet = cts;
            CancellationToken token = cts.Token;
            KhoaUI();
            try
            {
                CapNhatTrangThaiHoatDong("Đang quét kiểm tra dữ liệu trên hệ thống và Desktop...");
                // Một lần chuyển sang luồng nền, và chỉ gọi hàm tìm CSDL tốn kém MỘT lần
                var (danhSachXuat, danhSachNap, luuTruXuat, luuTruNap) = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();
                    List<string> xuat = Module_ChuyenGiaoDuLieu.NoiVongTayLonKetNoiTimKiemCSDL();
                    token.ThrowIfCancellationRequested();
                    List<string> nap = LayDanhSachCSDLDeNapTuDesktop(xuat);
                    token.ThrowIfCancellationRequested();
                    // Thư mục lưu trữ: chỉ liệt kê khi CÓ dữ liệu (xuất) / có trong gói đã xác nhận (nạp)
                    List<GoiXuatLuuTru> luuTruX = Module_ChuyenGiaoThuMucLuuTru.LietKeDeXuat();
                    List<GoiNhapLuuTru> luuTruN = Module_ChuyenGiaoThuMucLuuTru.QuetGoiDeNhap(TimThuMucGoiDuLieu());
                    return (xuat, nap, luuTruX, luuTruN);
                }, token);
                token.ThrowIfCancellationRequested();
                if (DaHuy) return;
                CapNhatDanhSachComboboxHaiChieu(danhSachXuat, danhSachNap, luuTruXuat, luuTruNap);
                if (cbo_ChonCSDL_Nguon.Items.Count > 0)
                {
                    await NapThongTinCSDLDangChonAsync();
                }
                else
                {
                    _duongDanCSDL_Nguon = string.Empty; // tránh xuất nhầm bằng đường dẫn của lần quét trước
                    richTextBox1_ThongTinDatabaseDuocChon.Text = "Không tìm thấy cơ sở dữ liệu nào.";
                    CapNhatDanhSachBang(null, laDanhSachBang: true);
                }
                CapNhatTrangThaiHoatDong(TomTatKetQuaQuet(danhSachNap.Count, luuTruNap));
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.WriteLine("[TaiDuLieuNenAsync] " + ex);
                CapNhatTrangThaiHoatDong("Không thể nạp danh sách dữ liệu.");
            }
            finally
            {
                if (ReferenceEquals(_ctsQuet, cts)) _ctsQuet = null;
                _dangQuet = false;
                MoKhoaUI();
            }
        }
        /// <summary>Dòng trạng thái sau khi quét: số CSDL và số thư mục lưu trữ có thể nạp, kèm cảnh báo đầu tiên của gói lưu trữ (nếu có).</summary>
        private static string TomTatKetQuaQuet(int soCSDLNap, IReadOnlyList<GoiNhapLuuTru> luuTruNap)
        {
            int soLuuTru = 0;
            string? canhBao = null;
            foreach (GoiNhapLuuTru g in luuTruNap)
            {
                if (g.Tep.Count > 0) soLuuTru++;
                if (canhBao == null && g.CanhBao.Count > 0) canhBao = $"[{g.Nhom.Ten}] {g.CanhBao[0]}";
            }
            string kq = soLuuTru > 0
                ? $"Đã quét xong: Phát hiện {soCSDLNap} CSDL và {soLuuTru} thư mục lưu trữ mới có sẵn ngoài Desktop để nạp."
                : $"Đã quét xong: Phát hiện {soCSDLNap} CSDL mới có sẵn ngoài Desktop để nạp.";
            if (canhBao != null) kq += " Cảnh báo: " + canhBao;
            return kq;
        }
        private static string? TimThuMucGoiDuLieu() => Module_ChuyenGiaoDuLieu.TimThuMucGoiDuLieu();
        private static string ThuMucGoiMacDinh() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), TEN_THU_MUC_GOI);
        private static string TenHienThi(string duongDanCSDL)
        {
            string ten = Path.GetFileNameWithoutExtension(duongDanCSDL);
            return AnhXaTenDatabase.TryGetValue(ten, out string? bietDanh) ? bietDanh : ten;
        }
        /// <summary>
        /// [AN TOÀN] Tên bảng lấy từ TÊN TỆP JSON trên Desktop (dữ liệu không đáng tin) rồi đi thẳng vào
        /// câu lệnh SQL ở tầng nạp. Chỉ chấp nhận chữ/số/gạch dưới để chặn chèn SQL và tên tệp lạ.
        /// </summary>
        private static bool LaTenBangHopLe(string ten)
        {
            if (string.IsNullOrWhiteSpace(ten) || ten.Length > 128) return false;
            if (ten.StartsWith("sqlite_", StringComparison.OrdinalIgnoreCase)) return false;
            if (char.IsDigit(ten[0])) return false;
            foreach (char c in ten)
            {
                if (!(char.IsLetterOrDigit(c) || c == '_')) return false;
            }
            return true;
        }
        /// <summary>Liệt kê các tệp bảng JSON hợp lệ trong một gói. Chạy trên luồng nền.</summary>
        private static (List<string> HopLe, List<string> BiBo) LietKeBangTrongGoi(string thuMucGoi)
        {
            var hopLe = new List<string>();
            var biBo = new List<string>();
            if (!Directory.Exists(thuMucGoi)) return (hopLe, biBo);
            foreach (string file in Directory.EnumerateFiles(thuMucGoi, "*.json"))
            {
                string ten = Path.GetFileNameWithoutExtension(file);
                if (ten.StartsWith(TIEN_TO_METADATA, StringComparison.OrdinalIgnoreCase)) continue;
                if (LaTenBangHopLe(ten)) hopLe.Add(ten);
                else biBo.Add(Path.GetFileName(file));
            }
            return (hopLe, biBo);
        }
        /// <summary>Chỉ lấy các CSDL thực sự có tệp JSON hợp lệ trong gói trên Desktop. Chạy trên luồng nền.</summary>
        private static List<string> LayDanhSachCSDLDeNapTuDesktop(IReadOnlyList<string> csdlGoc)
        {
            var ketQua = new List<string>(TenCSDLMucTieu.Length);
            string? thuMucGoi = TimThuMucGoiDuLieu();
            if (thuMucGoi == null) return ketQua;
            var theoTen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string f in csdlGoc) theoTen.TryAdd(Path.GetFileNameWithoutExtension(f), f);
            foreach (string dbName in TenCSDLMucTieu)
            {
                string thuMucCon = Path.Combine(thuMucGoi, dbName);
                if (!Directory.Exists(thuMucCon)) continue;
                bool coJsonHopLe = Directory.EnumerateFiles(thuMucCon, "*.json").Any(f =>
                {
                    string ten = Path.GetFileNameWithoutExtension(f);
                    return !ten.StartsWith(TIEN_TO_METADATA, StringComparison.OrdinalIgnoreCase) && LaTenBangHopLe(ten);
                });
                if (coJsonHopLe && theoTen.TryGetValue(dbName, out string? duongDanGoc))
                    ketQua.Add(duongDanGoc);
            }
            return ketQua;
        }
        private void CapNhatDanhSachBang(IEnumerable<object>? danhSach, bool laDanhSachBang)
        {
            var clb = checkedListBox1_clb_DanhSachBang;
            clb.BeginUpdate();
            try
            {
                clb.Items.Clear();
                if (danhSach != null)
                {
                    foreach (object muc in danhSach)
                        clb.Items.Add(muc, true);
                }
            }
            finally
            {
                clb.EndUpdate();
            }
            _clbLaDanhSachBang = laDanhSachBang;
            // Tự động ẩn nhãn tiêu đề nếu danh sách trống, hiện lên nếu có dữ liệu
            label1_TieuDeCauTrucCSDLDangDuocChon.Visible = (clb.Items.Count > 0);
        }
        /// <summary>Tên các BẢNG đang được tích (chỉ có nghĩa khi danh sách đang hiển thị bảng).</summary>
        private string[] LayDanhSachBangDuocChon()
        {
            if (!_clbLaDanhSachBang) return Array.Empty<string>();
            return checkedListBox1_clb_DanhSachBang.CheckedItems
                .Cast<object>()
                .Select(o => o.ToString() ?? string.Empty)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();
        }
        /// <summary>Ở chế độ "Tất cả", danh sách hiển thị CSDL: tôn trọng việc người dùng bỏ tích CSDL nào.</summary>
        private string[] LayCSDLXuatTatCaDuocChon() =>
            checkedListBox1_clb_DanhSachBang.CheckedItems
                .OfType<MucCSDL>()
                .Select(m => m.DuongDan)
                .Where(p => !string.IsNullOrEmpty(p))
                .Select(p => p!)
                .ToArray();
        // ==================== CHỌN CSDL / CHỌN TẤT CẢ ====================
        private async void Cbo_ChonCSDL_Nguon_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_khoaSuKien > 0) return;
            await ChayAnToanAsync(NapThongTinCSDLDangChonAsync, nameof(Cbo_ChonCSDL_Nguon_SelectedIndexChanged));
        }
        private async void CheckBox1_ChonTatCaCacCSDL_CheckedChanged(object? sender, EventArgs e)
        {
            if (_khoaSuKien > 0 || _dangXuLyMigration) return;
            CapNhatMucTatCa(cbo_ChonCSDL_Nguon, checkBox1_ChonTatCaCacCSDL.Checked);
            await ChayAnToanAsync(NapThongTinCSDLDangChonAsync, nameof(CheckBox1_ChonTatCaCacCSDL_CheckedChanged));
        }
        private void DongBoCheckBoxChonTatCa(bool giaTri)
        {
            if (checkBox1_ChonTatCaCacCSDL.Checked == giaTri) return;
            _khoaSuKien++;
            try { checkBox1_ChonTatCaCacCSDL.Checked = giaTri; }
            finally { _khoaSuKien--; }
        }
        /// <summary>Nạp danh sách bảng + thông tin của mục đang chọn trong combobox.</summary>
        private async Task NapThongTinCSDLDangChonAsync()
        {
            if (_dangXuLyMigration || DaHuy) return;
            if (cbo_ChonCSDL_Nguon.SelectedItem is not MucCSDL muc) return;
            DongBoCheckBoxChonTatCa(muc.LaTatCa);
            if (muc.LaTatCa)
            {
                _phienNapThongTin++; // vô hiệu hóa lần nạp đang chờ (nếu có)
                HienThiThongTinChonTatCa();
                CapNhatDanhSachBang(
                    _danhSachDuongDanThucTe.Select(p => (object)new MucCSDL(TenHienThi(p), p))
                        .Concat(_luuTruCoTheXuat.Select(g => (object)new MucLuuTru(g))),
                    laDanhSachBang: false);
                label1_TieuDeCauTrucCSDLDangDuocChon.Text = "DANH SÁCH CƠ SỞ DỮ LIỆU ĐƯỢC XỬ LÝ";
                CapNhatTrangThaiHoatDong("Đã chọn tất cả cơ sở dữ liệu.");
                return;
            }
            string duongDanFile = muc.DuongDan!;
            _duongDanCSDL_Nguon = duongDanFile;
            int phien = ++_phienNapThongTin;
            label1_TieuDeCauTrucCSDLDangDuocChon.Text = "CẤU TRÚC CƠ SỞ DỮ LIỆU ĐANG ĐƯỢC CHỌN";
            KhoaUI();
            try
            {
                CapNhatTrangThaiHoatDong("Đang phân tích cơ sở dữ liệu...");
                richTextBox1_ThongTinDatabaseDuocChon.Clear();
                // Cả danh sách bảng LẪN thông tin chi tiết (kiểm tra toàn vẹn, đọc tệp...) đều lấy ở luồng nền
                var (danhSachBang, thongTinChiTiet) = await Task.Run(() =>
                    (Module_ChuyenGiaoDuLieu.LayDanhSachBang(duongDanFile),
                     Module_ChuyenGiaoDuLieu.LayThongTinChiTietCSDL(duongDanFile)));
                if (phien != _phienNapThongTin || DaHuy) return;
                CapNhatDanhSachBang(danhSachBang, laDanhSachBang: true);
                Module_ChuyenGiaoDuLieu.InThongTinDatabaseLenRichTextBox(
                    richTextBox1_ThongTinDatabaseDuocChon, duongDanFile, danhSachBang, thongTinChiTiet);
                CapNhatTrangThaiHoatDong("Đã nạp thông tin phân vùng dữ liệu.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Database Load] " + ex);
                if (phien == _phienNapThongTin && !DaHuy)
                {
                    // Phải xóa danh sách bảng cũ: nếu giữ lại, bảng của CSDL trước sẽ bị đem đi xuất từ CSDL mới.
                    CapNhatDanhSachBang(null, laDanhSachBang: true);
                    Module_ChuyenGiaoDuLieu.InThongTinDatabaseLenRichTextBox(richTextBox1_ThongTinDatabaseDuocChon, duongDanFile, ex.Message);
                    CapNhatTrangThaiHoatDong("Không thể phân tích dữ liệu.");
                }
            }
            finally
            {
                MoKhoaUI();
            }
        }
        private void HienThiThongTinChonTatCa()
        {
            var rtb = richTextBox1_ThongTinDatabaseDuocChon;
            TamDungVe(rtb);
            try
            {
                rtb.Clear();
                AppendDongThongTin(rtb, "Tên tài khoản: ", $"{Module_TaiKhoan.TenTaiKhoan_RAM}\n");
                AppendDongThongTin(rtb, "Hành động: ", "Đang chọn tất cả cơ sở dữ liệu...\n");
                AppendDongThongTin(rtb, "Tổng số tệp CSDL: ", $"{_danhSachDuongDanThucTe.Count}\n");
                if (_luuTruCoTheXuat.Count > 0)
                {
                    AppendDongThongTin(rtb, "Thư mục lưu trữ kèm theo: ",
                        string.Join(", ", _luuTruCoTheXuat.Select(g => g.Nhom.Ten)) + "\n");
                }
                rtb.SelectionColor = MauGiaTri;
                rtb.AppendText(_luuTruCoTheXuat.Count > 0
                    ? "Khi bấm Xuất hoặc Nhập dữ liệu, hệ thống sẽ tự động duyệt qua và xử lý toàn bộ các CSDL (và thư mục lưu trữ có dữ liệu) trong danh sách.\n"
                    : "Khi bấm Xuất hoặc Nhập dữ liệu, hệ thống sẽ tự động duyệt qua và xử lý toàn bộ các CSDL trong danh sách.\n");
                rtb.SelectionColor = rtb.ForeColor;
            }
            finally
            {
                TiepTucVe(rtb);
            }
        }
        private static void AppendDongThongTin(RichTextBox rtb, string tieuDe, string giaTri)
        {
            rtb.SelectionColor = MauTieuDe;
            rtb.AppendText(tieuDe);
            rtb.SelectionColor = MauGiaTri;
            rtb.AppendText(giaTri);
        }
        // ==================== BỘ CHẠY TÁC VỤ DÙNG CHUNG ====================
        /// <summary>
        /// Khung chạy chung cho cả 4 luồng (xuất 1 / xuất tất cả / nạp 1 / nạp tất cả):
        /// khóa UI → chuẩn bị danh sách bước → chạy tuần tự có hủy an toàn → báo kết quả → mở khóa.
        /// Trả về true nếu tác vụ kết thúc bình thường và form vẫn còn sống.
        /// </summary>
        private async Task<bool> ThucThiTacVuAsync(
            MoTaTacVu moTa,
            Func<CancellationToken, Task<(List<BuocXuLy> CacBuoc, List<string> LoiChuanBi)>> chuanBi)
        {
            // Chốt chặn ngay trong khung chạy: không bao giờ có hai tác vụ ghi dữ liệu song song.
            if (_dangXuLyMigration || DaHuy) return false;
            _dangXuLyMigration = true;
            var cts = new CancellationTokenSource();
            _ctsTacVu = cts;
            CancellationToken token = cts.Token;
            KhoaUI();
            HienThanhTienTrinh();
            CapNhatTrangThaiHoatDong("Đang chuẩn bị...");
            try
            {
                var (cacBuoc, loiChuanBi) = await chuanBi(token);
                if (cacBuoc.Count == 0)
                {
                    prb_TienTrinhChuyenGiao.HoanTat(TrangThaiTienTrinh.CanhBao, chayDen100: false);
                    CapNhatTrangThaiHoatDong("Không có dữ liệu phù hợp để xử lý.");
                    if (!_yeuCauDongForm)
                    {
                        string chiTiet = loiChuanBi.Count > 0 ? "\n\n" + GomDanhSachLoi(loiChuanBi) : string.Empty;
                        MessageBox.Show(this, "Không có dữ liệu phù hợp để xử lý." + chiTiet, "Thông báo",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    KetQuaTacVu ketQua = await ChayCacBuocAsync(moTa, cacBuoc, token);
                    ketQua.DanhSachLoi.InsertRange(0, loiChuanBi);
                    await HienThiKetQuaAsync(moTa, ketQua);
                }
            }
            catch (OperationCanceledException)
            {
                prb_TienTrinhChuyenGiao.HoanTat(TrangThaiTienTrinh.CanhBao, chayDen100: false);
                CapNhatTrangThaiHoatDong("Đã dừng theo yêu cầu.");
            }
            catch (Exception ex)
            {
                await BaoLoiHeThongAsync(moTa, ex);
            }
            finally
            {
                _dangXuLyMigration = false;
                _ctsTacVu = null;   // bỏ tham chiếu trước khi Dispose để OnFormClosing không Cancel nhầm đối tượng đã hủy
                cts.Dispose();
                MoKhoaUI();
            }
            GiaiPhongBoNhoSauTacVu();
            if (DaHuy) return false;
            if (_yeuCauDongForm)
            {
                Close();
                return false;
            }
            _ = AnThanhTienTrinhSauKhiXongAsync();
            return true;
        }
        /// <summary>
        /// [RAM] Sau khi xuất/nạp khối lượng lớn, chuỗi/mảng JSON lớn nằm ở LOH và thường không được trả lại cho hệ điều hành.
        /// Dồn gọn + thu gom MỘT lần khi tác vụ đã xong, chạy ở luồng nền để giao diện không bị khựng.
        /// </summary>
        private static void GiaiPhongBoNhoSauTacVu()
        {
            _ = Task.Run(() =>
            {
                try
                {
                    GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[GiaiPhongBoNho] " + ex.Message);
                }
            });
        }
        private async Task<KetQuaTacVu> ChayCacBuocAsync(MoTaTacVu moTa, IReadOnlyList<BuocXuLy> cacBuoc, CancellationToken token)
        {
            int tong = cacBuoc.Count;
            var ketQua = new KetQuaTacVu
            {
                TongSo = cacBuoc.Count(b => b.TinhThongKe && !b.LaTepLuuTru),
                TongTepLuuTru = cacBuoc.Count(b => b.LaTepLuuTru)
            };
            var nhomBiHuy = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // bước bắt buộc của nhóm đã lỗi
            var nhomCoLoi = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // nhóm có ít nhất một bước lỗi
            // Chuyển từ chế độ "chuẩn bị" (không xác định) sang tiến trình xác định
            prb_TienTrinhChuyenGiao.BatDau();
            for (int i = 0; i < tong; i++)
            {
                if (token.IsCancellationRequested)
                {
                    ketQua.BiHuy = true;
                    break;
                }
                BuocXuLy buoc = cacBuoc[i];
                // Bỏ qua bước không còn đủ điều kiện (vd: sao lưu lỗi thì không nạp/xóa CSDL đó)
                if (buoc.Nhom != null)
                {
                    if (nhomBiHuy.Contains(buoc.Nhom)) continue;
                    if (buoc.DieuKien == DieuKienChay.NhomKhongLoi && nhomCoLoi.Contains(buoc.Nhom)) continue;
                }
                float batDau = i * 100f / tong;
                float tran = (i + 0.92f) * 100f / tong; // trần "bò": không bao giờ chạm bước kế tiếp trước khi xong
                prb_TienTrinhChuyenGiao.BaoTienDo(batDau, tran, $"{i + 1}/{tong}");
                CapNhatTrangThaiHoatDong($"{(int)batDau}% | {moTa.DongTuDangLam}: {buoc.Ten}...");
                try
                {
                    await buoc.ThucThi();
                    if (buoc.LaTepLuuTru) ketQua.TepLuuTruThanhCong++;
                    else if (buoc.TinhThongKe) ketQua.ThanhCong++;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Buoc: {buoc.Ten}] {ex}");
                    ketQua.DanhSachLoi.Add($"- {buoc.Ten}: {ex.Message}");
                    if (buoc.Nhom != null)
                    {
                        nhomCoLoi.Add(buoc.Nhom);
                        if (buoc.BatBuoc) nhomBiHuy.Add(buoc.Nhom);
                    }
                    else if (buoc.BatBuoc)
                    {
                        break; // bước bắt buộc không thuộc nhóm nào: dừng toàn bộ
                    }
                }
            }
            return ketQua;
        }
        private async Task HienThiKetQuaAsync(MoTaTacVu moTa, KetQuaTacVu kq)
        {
            string thoiGian = DateTime.Now.ToString(DINH_DANG_THOI_GIAN);
            string tienDo = MoTaSoLuong(kq);   // "3/3 bảng", "3/3 bảng và 2/2 tệp lưu trữ"...
            string thaoTacHoa = char.ToUpper(moTa.ThaoTac[0]) + moTa.ThaoTac[1..];
            TrangThaiTienTrinh trangThai;
            string trangThaiChu, hanhDongLog, ghiChuLog, tieuDe, noiDung;
            MessageBoxIcon icon;
            if (kq.BiHuy)
            {
                trangThai = TrangThaiTienTrinh.CanhBao;
                trangThaiChu = $"Đã dừng theo yêu cầu ({tienDo}).";
                hanhDongLog = $"{moTa.HanhDongNhatKy} (Đã dừng)";
                ghiChuLog = $"Người dùng dừng khi đã {moTa.ThaoTac} {tienDo} | Thời gian: {thoiGian}";
                tieuDe = "Đã dừng";
                noiDung = $"Đã dừng theo yêu cầu.\nĐã {moTa.ThaoTac} {tienDo} trước khi dừng.";
                if (moTa.GhiVaoCSDL)
                    noiDung += "\n\nDữ liệu đích có thể mới được nạp một phần. Nếu đã bật sao lưu, bạn có thể khôi phục từ bản sao lưu.";
                icon = MessageBoxIcon.Warning;
            }
            else if (kq.DanhSachLoi.Count == 0)
            {
                trangThai = TrangThaiTienTrinh.ThanhCong;
                trangThaiChu = $"100% | {thaoTacHoa} dữ liệu thành công!";
                hanhDongLog = moTa.HanhDongNhatKy;
                ghiChuLog = $"Đã {moTa.ThaoTac} thành công {tienDo} | Thời gian: {thoiGian}";
                tieuDe = "Thành công";
                noiDung = $"Đã {moTa.ThaoTac} thành công {tienDo}.";
                if (!string.IsNullOrEmpty(moTa.ViTriDich)) noiDung += $"\nThư mục: {moTa.ViTriDich}";
                icon = MessageBoxIcon.Information;
            }
            else
            {
                trangThai = TrangThaiTienTrinh.CanhBao;
                trangThaiChu = "Hoàn tất với một số cảnh báo.";
                hanhDongLog = $"{moTa.HanhDongNhatKy} (Có cảnh báo)";
                ghiChuLog = $"Hoàn tất một phần ({tienDo}), {kq.DanhSachLoi.Count} lỗi | Thời gian: {thoiGian}";
                tieuDe = moTa.TieuDeBaoCao;
                noiDung = $"Hoàn tất một phần: {tienDo}.\n\nCác mục bị lỗi (đã bỏ qua):\n{GomDanhSachLoi(kq.DanhSachLoi)}";
                icon = MessageBoxIcon.Warning;
            }
            prb_TienTrinhChuyenGiao.HoanTat(trangThai, chayDen100: !kq.BiHuy);
            CapNhatTrangThaiHoatDong(trangThaiChu);
            await GhiNhatKyAnToanAsync(hanhDongLog, ghiChuLog);
            if (_yeuCauDongForm) return; // người dùng đang đóng form: bỏ qua hộp thoại
            await Task.Delay(THOI_GIAN_CHO_THANH_DEN_100_MS);
            if (DaHuy) return;
            MessageBox.Show(this, noiDung, tieuDe, MessageBoxButtons.OK, icon);
        }
        /// <summary>"3/3 bảng", hoặc "3/3 bảng và 2/2 tệp lưu trữ", hoặc chỉ "2/2 tệp lưu trữ" khi chỉ xử lý thư mục lưu trữ.</summary>
        private static string MoTaSoLuong(KetQuaTacVu kq)
        {
            var phan = new List<string>(2);
            if (kq.TongSo > 0 || kq.TongTepLuuTru == 0) phan.Add($"{kq.ThanhCong}/{kq.TongSo} bảng");
            if (kq.TongTepLuuTru > 0) phan.Add($"{kq.TepLuuTruThanhCong}/{kq.TongTepLuuTru} tệp lưu trữ");
            return string.Join(" và ", phan);
        }
        private async Task BaoLoiHeThongAsync(MoTaTacVu moTa, Exception ex)
        {
            Debug.WriteLine("[TacVu] " + ex);
            if (!DaHuy)
            {
                prb_TienTrinhChuyenGiao.HoanTat(TrangThaiTienTrinh.Loi, chayDen100: false);
                CapNhatTrangThaiHoatDong("Tiến trình bị dừng do lỗi.");
            }
            await GhiNhatKyAnToanAsync($"Lỗi: {moTa.HanhDongNhatKy}", $"Sự cố: {ex.Message} | Thời gian: {DateTime.Now.ToString(DINH_DANG_THOI_GIAN)}");
            if (_yeuCauDongForm || DaHuy) return;
            await Task.Delay(THOI_GIAN_CHO_THANH_DEN_100_MS);
            if (DaHuy) return;
            MessageBox.Show(this, $"Sự cố nghiêm trọng khi {moTa.ThaoTac} dữ liệu:\n{ex.Message}", "Lỗi hệ thống",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        private static string GomDanhSachLoi(IReadOnlyList<string> danhSachLoi)
        {
            if (danhSachLoi.Count <= SO_LOI_TOI_DA_TRONG_HOP_THOAI)
                return string.Join(Environment.NewLine, danhSachLoi);
            return string.Join(Environment.NewLine, danhSachLoi.Take(SO_LOI_TOI_DA_TRONG_HOP_THOAI))
                   + Environment.NewLine
                   + $"... và {danhSachLoi.Count - SO_LOI_TOI_DA_TRONG_HOP_THOAI} lỗi khác.";
        }
        /// <summary>Ghi nhật ký ở luồng nền (ghi đĩa/CSDL không được làm đứng giao diện). Không bao giờ ném lỗi.</summary>
        private static Task GhiNhatKyAnToanAsync(string hanhDong, string ghiChu)
        {
            var taiKhoan = SessionInfo.TenTaiKhoan; // đọc ở luồng UI, luồng nền chỉ dùng bản sao
            return Task.Run(() =>
            {
                try
                {
                    Module_NhatKy.GhiNhatKy(taiKhoan: taiKhoan, hanhDong: hanhDong, ghiChu: ghiChu);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[GhiNhatKy] " + ex.Message);
                }
            });
        }
        // ==================== THANH TIẾN TRÌNH ====================
        private void HienThanhTienTrinh()
        {
            _phienThanhTienTrinh++;
            prb_TienTrinhChuyenGiao.Visible = true;
            prb_TienTrinhChuyenGiao.BatDau(khongXacDinh: true);
        }
        private async Task AnThanhTienTrinhSauKhiXongAsync()
        {
            int phien = _phienThanhTienTrinh;
            try
            {
                await Task.Delay(THOI_GIAN_AN_THANH_MS);
                if (phien != _phienThanhTienTrinh || DaHuy) return;
                prb_TienTrinhChuyenGiao.Visible = false;
                prb_TienTrinhChuyenGiao.DatLai();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[AnThanhTienTrinhSauKhiXongAsync] " + ex.Message);
            }
        }
        // ==================== XUẤT DỮ LIỆU ====================
        private async void btn_XuatDuLieuJson_Click(object? sender, EventArgs e)
        {
            if (DangBan) return;
            await ChayAnToanAsync(XuatDuLieuAsync, nameof(btn_XuatDuLieuJson_Click));
        }
        /// <summary>Thư mục lưu trữ được tích chọn để xuất kèm (chế độ "Tất cả"). Danh sách chưa hiển thị tệp nào thì lấy hết.</summary>
        private List<GoiXuatLuuTru> LayLuuTruXuatDuocChon() =>
            _clbLaDanhSachBang
                ? new List<GoiXuatLuuTru>(_luuTruCoTheXuat)
                : checkedListBox1_clb_DanhSachBang.CheckedItems.OfType<MucLuuTru>().Select(m => m.Goi).ToList();
        /// <summary>Tạo mã phiên bản chung cho cả đợt xuất (chỉ gọi SAU khi đã kiểm tra hợp lệ, tránh đổi trạng thái vô ích).</summary>
        private static string TaoPhienBanXuat()
        {
            Module_ChuyenGiaoDuLieu.ResetPhienBanXuatChung();
            return Module_ChuyenGiaoDuLieu.LayHoacTaoPhienBanXuatChung();
        }
        private async Task XuatDuLieuAsync()
        {
            string thuMucTong = ThuMucGoiMacDinh();
            // Quyết định theo mục đang chọn trong ComboBox (nguồn sự thật duy nhất), không theo ô tích riêng lẻ.
            bool xuatTatCa = cbo_ChonCSDL_Nguon.SelectedItem is MucCSDL { LaTatCa: true };
            // ----- Xuất tất cả CSDL -----
            if (xuatTatCa)
            {
                string[] danhSachCSDL = _clbLaDanhSachBang
                    ? _danhSachDuongDanThucTe.ToArray()
                    : LayCSDLXuatTatCaDuocChon();
                List<GoiXuatLuuTru> luuTruChon = LayLuuTruXuatDuocChon();
                if (danhSachCSDL.Length == 0 && luuTruChon.Count == 0)
                {
                    MessageBox.Show(this, "Vui lòng tích chọn ít nhất một cơ sở dữ liệu hoặc thư mục lưu trữ để xuất!", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string phienBanXuatChung = TaoPhienBanXuat();
                var moTa = new MoTaTacVu("Xuất tất cả CSDL ra JSON", "xuất", "Đang kết xuất", "Báo cáo Kết xuất Dữ liệu",
                    $"Desktop\\{TEN_THU_MUC_GOI}", GhiVaoCSDL: false);
                await ThucThiTacVuAsync(moTa, async token =>
                {
                    var cacBuoc = new List<BuocXuLy>();
                    var loiChuanBi = new List<string>();
                    foreach (string duongDanCSDL in danhSachCSDL)
                    {
                        token.ThrowIfCancellationRequested();
                        string tenCSDL = Path.GetFileNameWithoutExtension(duongDanCSDL);
                        string thuMucCon = Path.Combine(thuMucTong, tenCSDL);
                        CapNhatTrangThaiHoatDong($"Đang nạp danh sách bảng của: {tenCSDL}...");
                        List<string> danhSachBang;
                        try
                        {
                            danhSachBang = await Task.Run(() => Module_ChuyenGiaoDuLieu.LayDanhSachBang(duongDanCSDL), token);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            loiChuanBi.Add($"- [{tenCSDL}] Không đọc được danh sách bảng: {ex.Message}");
                            continue;
                        }
                        ThemCacBuocXuat(cacBuoc, tenCSDL, duongDanCSDL, thuMucCon, danhSachBang, phienBanXuatChung);
                    }
                    // Thư mục lưu trữ: liệt kê lại NGAY BÂY GIỜ (bản sao lưu mới có thể vừa được tạo sau lần quét) và chỉ lấy thư mục có dữ liệu
                    if (luuTruChon.Count > 0)
                    {
                        CapNhatTrangThaiHoatDong("Đang kiểm tra các thư mục lưu trữ...");
                        var tenChon = new HashSet<string>(luuTruChon.Select(g => g.Nhom.Ten), StringComparer.OrdinalIgnoreCase);
                        var (luuTruMoi, loiLuuTru) = await Task.Run(() => ChuanBiGoiLuuTruDeXuat(tenChon, thuMucTong), token);
                        loiChuanBi.AddRange(loiLuuTru);
                        ThemCacBuocXuatLuuTru(cacBuoc, luuTruMoi, thuMucTong, phienBanXuatChung);
                    }
                    // Chỉ thêm bước gán biểu tượng khi thật sự có gì để xuất
                    if (cacBuoc.Count > 0) ThemBuocGanIcon(cacBuoc, thuMucTong);
                    return (cacBuoc, loiChuanBi);
                });
                return;
            }
            // ----- Xuất một CSDL -----
            string nguon = _duongDanCSDL_Nguon;
            if (string.IsNullOrEmpty(nguon))
            {
                MessageBox.Show(this, "Chưa chọn cơ sở dữ liệu nguồn!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string[] danhSachBangChon = LayDanhSachBangDuocChon();
            if (danhSachBangChon.Length == 0)
            {
                MessageBox.Show(this, "Vui lòng tích chọn các bảng cần xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string tenThuMucRieng = Path.GetFileNameWithoutExtension(nguon);
            string thuMucGoiCon = Path.Combine(thuMucTong, tenThuMucRieng);
            string phienBan = TaoPhienBanXuat();
            var moTaDonLe = new MoTaTacVu("Xuất dữ liệu ra JSON", "xuất", "Đang kết xuất", "Báo cáo Kết xuất Dữ liệu",
                $"Desktop\\{TEN_THU_MUC_GOI}\\{tenThuMucRieng}", GhiVaoCSDL: false);
            await ThucThiTacVuAsync(moTaDonLe, _ =>
            {
                var cacBuoc = new List<BuocXuLy>();
                ThemCacBuocXuat(cacBuoc, tenThuMucRieng, nguon, thuMucGoiCon, danhSachBangChon, phienBan);
                ThemBuocGanIcon(cacBuoc, thuMucTong);
                return Task.FromResult((cacBuoc, new List<string>()));
            });
        }
        /// <summary>
        /// Các bước xuất MỘT CSDL: [xóa tệp phiên bản cũ] → từng bảng → [tạo tệp phiên bản mới].
        /// [AN TOÀN] Xóa tệp phiên bản cũ TRƯỚC khi ghi: nếu xuất bị dừng/lỗi giữa chừng thì gói dở dang không còn mang nhãn
        /// phiên bản của đợt xuất trước; tệp phiên bản chỉ được tạo khi mọi bảng đều xuất xong.
        /// </summary>
        private static void ThemCacBuocXuat(
            List<BuocXuLy> cacBuoc, string tenCSDL, string duongDanCSDL, string thuMucCon,
            IEnumerable<string> danhSachBang, string phienBan)
        {
            string nhom = tenCSDL;
            cacBuoc.Add(new BuocXuLy($"[{tenCSDL}] Làm mới tệp thông tin phiên bản", () => Task.Run(() =>
            {
                string tep = Path.Combine(thuMucCon, $"{TIEN_TO_METADATA}{tenCSDL}.json");
                if (File.Exists(tep))
                {
                    File.SetAttributes(tep, FileAttributes.Normal);
                    File.Delete(tep);
                }
            }), nhom, TinhThongKe: false));
            foreach (string tenBang in danhSachBang)
            {
                string bang = tenBang;
                cacBuoc.Add(new BuocXuLy($"[{tenCSDL}] {bang}",
                    () => Task.Run(() => Module_ChuyenGiaoDuLieu.XuatDuLieuRaJson(duongDanCSDL, bang, thuMucCon)),
                    nhom));
            }
            cacBuoc.Add(new BuocXuLy($"[{tenCSDL}] Tạo tệp thông tin phiên bản", () => Task.Run(() =>
            {
                Module_ChuyenGiaoDuLieu.TaoFileMetadataChuan(thuMucCon, tenCSDL, phienBan);
            }), nhom, TinhThongKe: false, DieuKien: DieuKienChay.NhomKhongLoi));
        }
        /// <summary>Chạy ở luồng nền: liệt kê lại các thư mục lưu trữ đã chọn và kiểm tra dung lượng trống của ổ chứa gói.</summary>
        private static (List<GoiXuatLuuTru> HopLe, List<string> Loi) ChuanBiGoiLuuTruDeXuat(ISet<string> tenDuocChon, string thuMucTong)
        {
            var hopLe = new List<GoiXuatLuuTru>(2);
            var loi = new List<string>();
            foreach (GoiXuatLuuTru goi in Module_ChuyenGiaoThuMucLuuTru.LietKeDeXuat())
            {
                if (!tenDuocChon.Contains(goi.Nhom.Ten)) continue;
                try
                {
                    Module_ChuyenGiaoThuMucLuuTru.KiemTraDungLuongTrong(thuMucTong, goi.TongByte);
                    hopLe.Add(goi);
                }
                catch (Exception ex)
                {
                    loi.Add($"- [{goi.Nhom.Ten}] Không xuất được: {ex.Message}");
                }
            }
            return (hopLe, loi);
        }
        /// <summary>
        /// Các bước xuất MỘT thư mục lưu trữ: [làm mới thư mục gói] → từng tệp → [tạo tệp xác nhận].
        /// [AN TOÀN] Tệp xác nhận (tên + dung lượng + SHA-256 từng tệp) chỉ được tạo SAU CÙNG và chỉ khi mọi tệp đã xuất xong:
        /// gói xuất dở dang (lỗi/bị dừng) không có tệp xác nhận nên máy nhận sẽ không nhập.
        /// </summary>
        private static void ThemCacBuocXuatLuuTru(
            List<BuocXuLy> cacBuoc, IEnumerable<GoiXuatLuuTru> cacGoi, string thuMucTong, string phienBan)
        {
            foreach (GoiXuatLuuTru goi in cacGoi)
            {
                NhomThuMucLuuTru nhomLuuTru = goi.Nhom;
                string nhom = nhomLuuTru.Ten;
                string thuMucGoiNhom = Path.Combine(thuMucTong, nhom);
                var daXuat = new List<MucXacNhanLuuTru>(goi.Tep.Count);
                cacBuoc.Add(new BuocXuLy($"[{nhom}] Làm mới thư mục gói", () => Task.Run(() =>
                {
                    Module_ChuyenGiaoThuMucLuuTru.LamMoiThuMucGoi(thuMucGoiNhom, nhomLuuTru);
                }), nhom, BatBuoc: true, TinhThongKe: false));
                foreach (TepLuuTru tep in goi.Tep)
                {
                    string tenTep = tep.Ten;
                    cacBuoc.Add(new BuocXuLy($"[{nhom}] {tenTep}", () => Task.Run(() =>
                    {
                        daXuat.Add(Module_ChuyenGiaoThuMucLuuTru.XuatMotTep(nhomLuuTru, tenTep, thuMucGoiNhom, CancellationToken.None));
                    }), nhom, LaTepLuuTru: true));
                }
                cacBuoc.Add(new BuocXuLy($"[{nhom}] Tạo tệp xác nhận của gói", () => Task.Run(() =>
                {
                    Module_ChuyenGiaoThuMucLuuTru.TaoXacNhan(nhomLuuTru, thuMucGoiNhom, daXuat, phienBan);
                    Module_ChuyenGiaoThuMucLuuTru.CapNhatNhipTim(thuMucTong); // gói vừa xuất không bị dọn nhầm theo mốc của lần xuất trước
                }), nhom, TinhThongKe: false, DieuKien: DieuKienChay.NhomKhongLoi));
            }
        }
        private static void ThemBuocGanIcon(List<BuocXuLy> cacBuoc, string thuMucTong)
        {
            cacBuoc.Add(new BuocXuLy("Định dạng biểu tượng thư mục", () => Task.Run(() =>
            {
                Module_HeThong.GanIconThuMuc(thuMucTong);
            }), TinhThongKe: false));
        }
        // ==================== NHẬP DỮ LIỆU ====================
        private async void btn_NhapDuLieuJson_Click(object? sender, EventArgs e)
        {
            if (DangBan) return;
            await ChayAnToanAsync(async () =>
            {
                if (await NhapDuLieuAsync())
                    await TaiDuLieuNenAsync(); // làm mới danh sách bảng sau khi nạp
            }, nameof(btn_NhapDuLieuJson_Click));
        }
        /// <summary>Trả về true nếu tác vụ đã chạy xong bình thường (cần làm mới giao diện).</summary>
        private async Task<bool> NhapDuLieuAsync()
        {
            // Kiểm tra thư mục trên luồng nền (Desktop/OneDrive có thể chậm) để giao diện không đứng.
            string? thuMucTongNguon = await Task.Run(TimThuMucGoiDuLieu);
            if (DaHuy) return false;
            if (thuMucTongNguon == null)
            {
                MessageBox.Show(this, $"Không tìm thấy thư mục gói dữ liệu trên Desktop!\n\n{ThuMucGoiMacDinh()}",
                    "Thiếu nguồn dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (!_coDuLieuDeNap || comboBox1_CSDLDeNapVao.SelectedItem is not MucCSDL mucChon)
            {
                MessageBox.Show(this, "Chưa chọn cơ sở dữ liệu đích để nạp vào!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            bool saoLuu = chk_BackupTruocKhiChuyen.Checked;
            bool xoaDuLieuCu = chk_XoaDuLieuCu.Checked;
            bool tatCa = mucChon.LaTatCa; // ComboBox là nguồn sự thật duy nhất
            string[] danhSachCSDL = Array.Empty<string>();
            string nguon = string.Empty;
            string thuMucNguon = string.Empty;
            List<string> bangCanNap = new();
            List<string> loiChuanBiDonLe = new();
            if (tatCa)
            {
                // Nạp tất cả = các CSDL CÓ GÓI JSON (danh sách nạp), không phải toàn bộ CSDL hệ thống.
                danhSachCSDL = _danhSachDuongDanNap.ToArray();
                bool coLuuTruDeNap = _luuTruCoTheNap.Any(g => g.Tep.Count > 0);
                if (danhSachCSDL.Length == 0 && !coLuuTruDeNap)
                {
                    MessageBox.Show(this, "Không tìm thấy CSDL đích nào để nạp!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            else
            {
                // Lấy thẳng đường dẫn gắn trong mục (không suy từ chỉ số giữa hai danh sách khác nhau).
                nguon = mucChon.DuongDan ?? string.Empty;
                if (string.IsNullOrEmpty(nguon))
                {
                    MessageBox.Show(this, "Chưa chọn cơ sở dữ liệu đích hợp lệ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                thuMucNguon = Path.Combine(thuMucTongNguon, Path.GetFileNameWithoutExtension(nguon));
                string thuMucKiemTra = thuMucNguon;
                var (tonTai, hopLe, biBo) = await Task.Run(() =>
                {
                    bool co = Directory.Exists(thuMucKiemTra);
                    var r = LietKeBangTrongGoi(thuMucKiemTra);
                    return (co, r.HopLe, r.BiBo);
                });
                if (DaHuy) return false;
                if (!tonTai)
                {
                    MessageBox.Show(this, $"Không tìm thấy gói dữ liệu phân vùng!\n\n{thuMucNguon}",
                        "Thiếu nguồn dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                foreach (string f in biBo)
                    loiChuanBiDonLe.Add($"- Bỏ qua tệp có tên bảng không hợp lệ: {f}");
                bangCanNap = hopLe;
                // Danh sách bảng đang tích trên form thuộc CSDL của ô XUẤT. Chỉ dùng làm bộ lọc khi đúng là cùng một CSDL;
                // nếu khác (hoặc đang ở chế độ "Tất cả") thì nạp mọi bảng có trong gói.
                if (_clbLaDanhSachBang && string.Equals(_duongDanCSDL_Nguon, nguon, StringComparison.OrdinalIgnoreCase))
                {
                    var daTich = new HashSet<string>(LayDanhSachBangDuocChon(), StringComparer.OrdinalIgnoreCase);
                    bangCanNap = bangCanNap.Where(daTich.Contains).ToList();
                }
                if (bangCanNap.Count == 0)
                {
                    MessageBox.Show(this, "Không có bảng dữ liệu hợp lệ nào để nạp. Hãy kiểm tra gói JSON hoặc các bảng đã tích chọn.",
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            if (xoaDuLieuCu)
            {
                string canhBao = "CẢNH BÁO:\n\nToàn bộ dữ liệu cũ của các bảng được nạp sẽ bị xóa trước khi nạp.";
                if (tatCa && _luuTruCoTheNap.Any(g => g.Tep.Count > 0))
                    canhBao += "\n\nCác tệp trong thư mục lưu trữ (Bansaoluu, LuuTruThiDua_LichSu) có cùng tên nhưng khác nội dung " +
                               "cũng sẽ bị thay bằng tệp trong gói.";
                if (!saoLuu)
                    canhBao += "\n\nBẠN ĐANG TẮT SAO LƯU: sau khi xóa sẽ KHÔNG thể khôi phục dữ liệu cũ!";
                canhBao += "\n\nBạn có chắc chắn muốn tiếp tục?";
                if (MessageBox.Show(this, canhBao, "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                {
                    return false;
                }
            }
            // ----- Nạp tất cả CSDL -----
            if (tatCa)
            {
                var moTa = new MoTaTacVu("Nhập tất cả CSDL từ JSON", "nạp", "Đang nạp", "Báo cáo", null, GhiVaoCSDL: true);
                return await ThucThiTacVuAsync(moTa, async token =>
                {
                    var cacBuoc = new List<BuocXuLy>();
                    var loiChuanBi = new List<string>();
                    foreach (string duongDanCSDL in danhSachCSDL)
                    {
                        token.ThrowIfCancellationRequested();
                        string dbName = Path.GetFileNameWithoutExtension(duongDanCSDL);
                        string folderCon = Path.Combine(thuMucTongNguon, dbName);
                        var (hopLe, biBo) = await Task.Run(() => LietKeBangTrongGoi(folderCon), token);
                        foreach (string f in biBo)
                            loiChuanBi.Add($"- [{dbName}] Bỏ qua tệp có tên bảng không hợp lệ: {f}");
                        if (hopLe.Count == 0) continue;
                        ThemCacBuocNhap(cacBuoc, duongDanCSDL, folderCon, hopLe, saoLuu, xoaDuLieuCu, hienNhan: true);
                    }
                    // Thư mục lưu trữ: quét lại gói NGAY BÂY GIỜ; chỉ thư mục nằm trong gói và có tệp hợp lệ mới được nhập
                    token.ThrowIfCancellationRequested();
                    List<GoiNhapLuuTru> luuTruNap = await Task.Run(
                        () => Module_ChuyenGiaoThuMucLuuTru.QuetGoiDeNhap(thuMucTongNguon), token);
                    foreach (GoiNhapLuuTru g in luuTruNap)
                    {
                        foreach (string canhBaoGoi in g.CanhBao)
                            loiChuanBi.Add($"- [{g.Nhom.Ten}] {canhBaoGoi}");
                    }
                    ThemCacBuocNhapLuuTru(cacBuoc, luuTruNap, saoLuu, xoaDuLieuCu, thuMucTongNguon);
                    if (cacBuoc.Count > 0) ThemBuocDonThuMucGocRong(cacBuoc, thuMucTongNguon);
                    return (cacBuoc, loiChuanBi);
                });
            }
            // ----- Nạp một CSDL -----
            var moTaDonLe = new MoTaTacVu("Nhập dữ liệu JSON", "nạp", "Đang nạp", "Báo cáo", null, GhiVaoCSDL: true);
            return await ThucThiTacVuAsync(moTaDonLe, _ =>
            {
                var cacBuoc = new List<BuocXuLy>();
                ThemCacBuocNhap(cacBuoc, nguon, thuMucNguon, bangCanNap, saoLuu, xoaDuLieuCu, hienNhan: false);
                ThemBuocDonThuMucGocRong(cacBuoc, thuMucTongNguon);
                return Task.FromResult((cacBuoc, loiChuanBiDonLe));
            });
        }
        /// <summary>
        /// Các bước nạp cho MỘT CSDL (cùng một nhóm):
        /// sao lưu (bắt buộc) → từng bảng → vacuum → ghi nhận phiên bản vào CSDL3 → dọn gói nguồn đã nạp.
        /// Ba bước cuối CHỈ chạy khi mọi bước trước đó của CSDL này đều thành công.
        /// </summary>
        private static void ThemCacBuocNhap(
            List<BuocXuLy> cacBuoc, string duongDanCSDL, string thuMucNguon,
            IReadOnlyList<string> danhSachBang, bool saoLuu, bool xoaDuLieuCu, bool hienNhan)
        {
            string tenCSDL = Path.GetFileNameWithoutExtension(duongDanCSDL);
            string tienTo = hienNhan ? $"[{tenCSDL}] " : string.Empty;
            string nhom = tenCSDL;
            string fileSaoLuu = string.Empty;
            int soBangDaNap = 0;
            var cacTepDaNap = new List<string>(danhSachBang.Count);
            if (saoLuu)
            {
                cacBuoc.Add(new BuocXuLy($"{tienTo}Tạo điểm khôi phục bảo hiểm", () => Task.Run(() =>
                {
                    fileSaoLuu = Module_ChuyenGiaoDuLieu.SaoLuuCSDLTruocKhiNhap(duongDanCSDL);
                    // Không có bằng chứng đã sao lưu thì tuyệt đối không nạp.
                    if (string.IsNullOrWhiteSpace(fileSaoLuu))
                        throw new IOException("Không tạo được bản sao lưu bảo hiểm nên đã hủy nạp CSDL này.");
                }), nhom, BatBuoc: true, TinhThongKe: false));
            }
            foreach (string tenBang in danhSachBang)
            {
                if (string.IsNullOrWhiteSpace(tenBang)) continue;
                // Bảo vệ: tên bảng phải hợp lệ và không phải tệp metadata
                if (tenBang.StartsWith(TIEN_TO_METADATA, StringComparison.OrdinalIgnoreCase) || !LaTenBangHopLe(tenBang)) continue;
                string bang = tenBang;
                string fileJson = Path.Combine(thuMucNguon, $"{bang}.json");
                cacBuoc.Add(new BuocXuLy($"{tienTo}{bang}", () => Task.Run(() =>
                {
                    if (!File.Exists(fileJson))
                        throw new FileNotFoundException($"Không tìm thấy file JSON nguồn: {Path.GetFileName(fileJson)}");
                    Module_ChuyenGiaoDuLieu.NhapDuLieuVaoCSDL(fileJson, duongDanCSDL, bang, xoaDuLieuCu, fileSaoLuu, thuMucNguon);
                    soBangDaNap++;
                    cacTepDaNap.Add(fileJson);
                }), nhom));
            }
            cacBuoc.Add(new BuocXuLy($"{tienTo}Tối ưu cơ sở dữ liệu", () => Task.Run(() =>
            {
                if (soBangDaNap == 0) return;
                try
                {
                    Module_ChuyenGiaoDuLieu.VacuumDatabase(duongDanCSDL);
                }
                catch (Exception ex)
                {
                    // Vacuum chỉ để gọn tệp: dữ liệu đã nạp xong nên lỗi ở đây không được chặn bước ghi nhận/dọn gói.
                    Debug.WriteLine("[Vacuum] " + ex.Message);
                }
            }), nhom, TinhThongKe: false));
            cacBuoc.Add(new BuocXuLy($"{tienTo}Ghi nhận lịch sử nạp vào CSDL3", () => Task.Run(() =>
            {
                if (soBangDaNap == 0) return;
                string phienBanDaNap = DocPhienBanTuGoi(thuMucNguon, tenCSDL);
                if (!string.IsNullOrEmpty(phienBanDaNap))
                    Module_ChuyenGiaoDuLieu.GhiNhanDaNapThanhCongVaoCSDL3(phienBanDaNap);
            }), nhom, TinhThongKe: false, DieuKien: DieuKienChay.NhomKhongLoi));
            // Chỉ xóa đúng các tệp đã nạp thành công, và chỉ khi CSDL này không có lỗi (tệp lỗi được giữ lại để thử lại).
            cacBuoc.Add(new BuocXuLy($"{tienTo}Dọn gói dữ liệu đã nạp trên Desktop", () => Task.Run(() =>
            {
                XoaGoiDaNap(thuMucNguon, cacTepDaNap);
                // Chỉ khi CSDL nạp xong KHÔNG lỗi mới bỏ điểm khôi phục.
                if (!string.IsNullOrEmpty(fileSaoLuu)) Module_ChuyenGiaoDuLieu.XoaBanSaoLuuAnToan(fileSaoLuu);
            }), nhom, TinhThongKe: false, DieuKien: DieuKienChay.NhomKhongLoi));
        }
        /// <summary>
        /// Các bước nạp MỘT thư mục lưu trữ: [kiểm tra thư mục đích + dung lượng] → từng tệp → [dọn gói đã nạp].
        /// Mỗi tệp được kiểm tra SHA-256 theo tệp xác nhận trước khi đặt vào hệ thống; tệp đã có giống hệt thì bỏ qua;
        /// tệp cùng tên khác nội dung chỉ bị thay khi <paramref name="ghiDe"/> (ô "Xóa dữ liệu trong csdl trước khi nạp"), và bản cũ được sao lưu
        /// trước nếu <paramref name="saoLuu"/>. Tệp lỗi được giữ lại trong gói để thử lại; tệp đã nạp xong được dọn khỏi Desktop.
        /// </summary>
        private static void ThemCacBuocNhapLuuTru(
            List<BuocXuLy> cacBuoc, IEnumerable<GoiNhapLuuTru> cacGoi, bool saoLuu, bool ghiDe, string thuMucTongNguon)
        {
            string? thuMucSaoLuu = saoLuu ? Path.Combine(thuMucTongNguon, Module_ChuyenGiaoDuLieu.TEN_THU_MUC_BACKUP) : null;
            foreach (GoiNhapLuuTru goi in cacGoi)
            {
                if (goi.Tep.Count == 0) continue; // gói chỉ có cảnh báo: không có gì để nạp
                string nhom = goi.Nhom.Ten;
                var daXong = new List<string>(goi.Tep.Count);
                cacBuoc.Add(new BuocXuLy($"[{nhom}] Kiểm tra thư mục đích và dung lượng trống", () => Task.Run(() =>
                {
                    Module_ChuyenGiaoThuMucLuuTru.ChuanBiNhap(goi);
                }), nhom, BatBuoc: true, TinhThongKe: false));
                foreach (MucXacNhanLuuTru muc in goi.Tep)
                {
                    MucXacNhanLuuTru m = muc;
                    cacBuoc.Add(new BuocXuLy($"[{nhom}] {m.TenFile}", () => Task.Run(() =>
                    {
                        Module_ChuyenGiaoThuMucLuuTru.NhapMotTep(goi, m, ghiDe, thuMucSaoLuu, CancellationToken.None);
                        daXong.Add(m.TenFile);
                    }), nhom, LaTepLuuTru: true));
                }
                cacBuoc.Add(new BuocXuLy($"[{nhom}] Dọn gói đã nạp trên Desktop", () => Task.Run(() =>
                {
                    Module_ChuyenGiaoThuMucLuuTru.DonGoiDaNhap(goi, daXong); // chỉ xóa đúng các tệp đã nạp xong
                }), nhom, TinhThongKe: false));
            }
        }
        private static void ThemBuocDonThuMucGocRong(List<BuocXuLy> cacBuoc, string thuMucTongNguon)
        {
            cacBuoc.Add(new BuocXuLy("Dọn thư mục gói dữ liệu (nếu đã hết gói)", () => Task.Run(() =>
            {
                XoaThuMucGocNeuHetGoi(thuMucTongNguon);
            }), TinhThongKe: false));
        }
        // ----- Đọc phiên bản trong tệp metadata (dùng chung hàm của Module, không viết lại) -----
        private static string DocPhienBanTuGoi(string thuMucGoi, string tenCSDL)
        {
            string phienBan = Module_ChuyenGiaoDuLieu.DocPhienBanTuTep(
                Path.Combine(thuMucGoi, $"{TIEN_TO_METADATA}{tenCSDL}.json"));
            if (phienBan.Length > 0 || !Directory.Exists(thuMucGoi)) return phienBan;
            foreach (string tep in Directory.EnumerateFiles(thuMucGoi, $"{TIEN_TO_METADATA}*.json"))
            {
                phienBan = Module_ChuyenGiaoDuLieu.DocPhienBanTuTep(tep);
                if (phienBan.Length > 0) return phienBan;
            }
            return string.Empty;
        }
        // ----- Dọn dẹp an toàn -----
        private static bool LaLienKetThuMuc(string duongDan)
        {
            try { return (File.GetAttributes(duongDan) & FileAttributes.ReparsePoint) != 0; }
            catch { return true; } // không đọc được thuộc tính: coi như không an toàn để xóa
        }
        private static void XoaTepAnToan(string tep)
        {
            try
            {
                if (!File.Exists(tep)) return;
                File.SetAttributes(tep, FileAttributes.Normal);
                File.Delete(tep);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Xóa tệp] {tep}: {ex.Message}");
            }
        }
        /// <summary>Xóa các tệp bảng đã nạp; nếu gói hết bảng thì xóa luôn tệp metadata và thư mục gói (chỉ khi rỗng).</summary>
        private static void XoaGoiDaNap(string thuMucGoiCon, IEnumerable<string> cacTepDaNap)
        {
            try
            {
                foreach (string tep in cacTepDaNap) XoaTepAnToan(tep);
                if (!Directory.Exists(thuMucGoiCon) || LaLienKetThuMuc(thuMucGoiCon)) return;
                bool conBangChuaNap = Directory.EnumerateFiles(thuMucGoiCon, "*.json")
                    .Any(f => !Path.GetFileName(f).StartsWith(TIEN_TO_METADATA, StringComparison.OrdinalIgnoreCase));
                if (conBangChuaNap) return;
                foreach (string meta in Directory.EnumerateFiles(thuMucGoiCon, $"{TIEN_TO_METADATA}*.json").ToArray())
                    XoaTepAnToan(meta);
                if (!Directory.EnumerateFileSystemEntries(thuMucGoiCon).Any())
                    Directory.Delete(thuMucGoiCon, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[XoaGoiDaNap] " + ex.Message);
            }
        }
        /// <summary>Chỉ xóa thư mục gốc "Database-PhanMemThiDua2026" khi KHÔNG còn gói CSDL nào bên trong.</summary>
        private static void XoaThuMucGocNeuHetGoi(string thuMucGoc)
        {
            try
            {
                if (!Directory.Exists(thuMucGoc) || LaLienKetThuMuc(thuMucGoc)) return;
                // Chốt chặn: chỉ đụng tới đúng thư mục có tên của gói dữ liệu
                string ten = Path.GetFileName(Path.TrimEndingDirectorySeparator(thuMucGoc));
                if (!string.Equals(ten, TEN_THU_MUC_GOI, StringComparison.OrdinalIgnoreCase)) return;
                string[] cacMuc = Directory.EnumerateFileSystemEntries(thuMucGoc).ToArray();
                foreach (string muc in cacMuc)
                {
                    string tenMuc = Path.GetFileName(muc);
                    if (Directory.Exists(muc))
                    {
                        // Thư mục sao lưu RỖNG thì bỏ được; còn bản sao lưu (điểm khôi phục) hoặc còn gói chưa nạp thì giữ nguyên tất cả
                        bool laSaoLuuRong = string.Equals(tenMuc, Module_ChuyenGiaoDuLieu.TEN_THU_MUC_BACKUP, StringComparison.OrdinalIgnoreCase)
                                            && !Directory.EnumerateFileSystemEntries(muc).Any();
                        if (!laSaoLuuRong) return;
                        continue;
                    }
                    // Chỉ chấp nhận tệp phụ do phần mềm tạo ra (biểu tượng thư mục, tệp heartbeat)
                    bool laTepPhu = tenMuc.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)
                                    || tenMuc.Equals("system.touch", StringComparison.OrdinalIgnoreCase)
                                    || tenMuc.EndsWith(".ico", StringComparison.OrdinalIgnoreCase);
                    if (!laTepPhu) return;
                }
                foreach (string muc in cacMuc)
                {
                    if (Directory.Exists(muc)) Directory.Delete(muc, false);
                    else XoaTepAnToan(muc);
                }
                new DirectoryInfo(thuMucGoc).Attributes = FileAttributes.Normal; // thư mục có biểu tượng tùy biến thường mang cờ ReadOnly
                Directory.Delete(thuMucGoc, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Lỗi dọn thư mục gốc]: " + ex.Message);
            }
        }
        // ==================== MỞ THƯ MỤC ====================
        private async void kryptonButton2_MoThuMuc_Click(object? sender, EventArgs e)
        {
            if (DangBan || _dangMoThuMuc) return;
            _dangMoThuMuc = true;
            try
            {
                CapNhatTrangThaiHoatDong("Đang kiểm tra cửa sổ thư mục...");
                lbl_TrangThaiHoatDong.Update();
                // Thao tác đĩa (có thể chậm với OneDrive) chạy nền để giao diện không đứng
                string thuMucTong = await Task.Run(() =>
                {
                    string thuMuc = TimThuMucGoiDuLieu() ?? ThuMucGoiMacDinh();
                    Directory.CreateDirectory(thuMuc);
                    return thuMuc;
                });
                if (DaHuy) return;
                if (KichHoatCuaSoDaMoNeuCo(thuMucTong))
                {
                    CapNhatTrangThaiHoatDong("Đã có cửa sổ thư mục đang mở — đưa lên trên.");
                }
                else
                {
                    CapNhatTrangThaiHoatDong("Đang mở thư mục...");
                    await Task.Run(() => MoThuMucMoiTrongExplorer(thuMucTong));
                    CapNhatTrangThaiHoatDong("Sẵn sàng.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[MoThuMuc] " + ex.Message);
                if (!DaHuy) CapNhatTrangThaiHoatDong("Không thể mở thư mục.");
            }
            finally
            {
                _dangMoThuMuc = false;
            }
        }
        /// <summary>Phải chạy trên luồng UI (STA) vì dùng COM Shell.Application.</summary>
        private static bool KichHoatCuaSoDaMoNeuCo(string duongDanThuMuc)
        {
            dynamic? shellApp = null;
            dynamic? danhSachCuaSo = null;
            try
            {
                Type? shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType == null) return false;
                shellApp = Activator.CreateInstance(shellType);
                if (shellApp == null) return false;
                danhSachCuaSo = shellApp.Windows();
                string duongDanCanTim = ChuanHoaDuongDan(duongDanThuMuc);
                foreach (dynamic cuaSo in danhSachCuaSo)
                {
                    try
                    {
                        string? locationUrl = cuaSo.LocationURL as string;
                        if (string.IsNullOrEmpty(locationUrl)) continue;
                        string duongDanCuaSoNay = ChuanHoaDuongDan(Uri.UnescapeDataString(new Uri(locationUrl).LocalPath));
                        if (!string.Equals(duongDanCuaSoNay, duongDanCanTim, StringComparison.OrdinalIgnoreCase)) continue;
                        IntPtr hwnd = new IntPtr((long)cuaSo.HWND);
                        if (hwnd == IntPtr.Zero) continue;
                        if (NativeMethods.IsIconic(hwnd)) NativeMethods.ShowWindowAsync(hwnd, NativeMethods.SW_RESTORE);
                        NativeMethods.SetForegroundWindow(hwnd);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("[Shell Window] " + ex.Message);
                    }
                    finally
                    {
                        if (cuaSo != null) Marshal.ReleaseComObject(cuaSo);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Shell COM Error]: " + ex.Message);
            }
            finally
            {
                if (danhSachCuaSo != null) Marshal.ReleaseComObject(danhSachCuaSo);
                if (shellApp != null) Marshal.ReleaseComObject(shellApp);
            }
            return false;
        }
        private static string ChuanHoaDuongDan(string duongDan) =>
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(duongDan));
        private static void MoThuMucMoiTrongExplorer(string duongDanThuMuc)
        {
            try
            {
                // Process là đối tượng giữ handle hệ thống: phải Dispose
                using Process? tienTrinh = Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{duongDanThuMuc}\"",
                    UseShellExecute = true,
                    ErrorDialog = false
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Explorer Error]: " + ex.Message);
            }
        }
        // ==================== ĐÓNG FORM AN TOÀN ====================
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_dangXuLyMigration)
            {
                e.Cancel = true;
                // Lần đầu: hỏi và yêu cầu dừng tại ranh giới giữa hai bảng (không ngắt giữa chừng một bảng).
                // Các lần sau: chỉ chờ bước hiện tại kết thúc, form sẽ tự đóng.
                if (!_yeuCauDongForm &&
                    MessageBox.Show(this,
                        "Hệ thống đang xuất/nạp dữ liệu.\n\nBạn có muốn dừng an toàn và đóng cửa sổ?\n" +
                        "(Bảng đang xử lý sẽ được hoàn tất trước khi dừng.)",
                        "Đang xử lý dữ liệu", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    _yeuCauDongForm = true;
                    CapNhatTrangThaiHoatDong("Đang dừng an toàn, vui lòng chờ...");
                    try { _ctsTacVu?.Cancel(); }
                    catch (ObjectDisposedException) { }
                }
                return;
            }
            HuyCacTacVuNen();
            base.OnFormClosing(e);
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            try
            {
                if (_kichHoatForm12KhiDong) HienLaiForm12(); // trả quyền điều khiển cho Form12 TRƯỚC, dọn dẹp sau
            }
            finally
            {
                GiaiPhongTaiNguonForm();
            }
        }
        private static void HienLaiForm12()
        {
            if (Application.OpenForms["Form12"] is not Form12 form12) return;
            if (!form12.Visible) form12.Show();
            if (form12.WindowState == FormWindowState.Minimized) form12.WindowState = FormWindowState.Normal;
            form12.BringToFront();
            form12.Activate();
        }
        /// <summary>
        /// [RAM] Form đóng xong vẫn có thể còn được tham chiếu (ShowDialog, sự kiện...). Trả lại ngay các vùng nhớ lớn:
        /// nội dung RichTextBox (bộ đệm RTF), danh sách mục, danh sách đường dẫn, và dừng hoạt ảnh thanh tiến trình.
        /// </summary>
        private void GiaiPhongTaiNguonForm()
        {
            try
            {
                prb_TienTrinhChuyenGiao.DatLai();
                checkedListBox1_clb_DanhSachBang.Items.Clear();
                cbo_ChonCSDL_Nguon.Items.Clear();
                comboBox1_CSDLDeNapVao.Items.Clear();
                richTextBox1_ThongTinDatabaseDuocChon.Clear();
                _danhSachDuongDanThucTe.Clear();
                _danhSachDuongDanThucTe.TrimExcess();
                _danhSachDuongDanNap.Clear();
                _danhSachDuongDanNap.TrimExcess();
                _luuTruCoTheXuat.Clear();
                _luuTruCoTheNap.Clear();
                _dieuKhienKhoaKhiBan = Array.Empty<Control>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[GiaiPhongTaiNguonForm] " + ex.Message);
            }
        }
        private void HuyCacTacVuNen()
        {
            try { _ctsQuet?.Cancel(); }
            catch (ObjectDisposedException) { }
            try { _ctsTaiThongTinDatabase?.Cancel(); }
            catch (ObjectDisposedException) { }
        }
        private void kryptonButton_Dong_Click(object? sender, EventArgs e)
        {
            _kichHoatForm12KhiDong = true;
            Close();
        }
        // ==================== WIN32 (chỉ dùng riêng cho form này) ====================
        private static void TamDungVe(Control c) =>
            NativeMethods.SendMessage(c.Handle, NativeMethods.WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        private static void TiepTucVe(Control c)
        {
            NativeMethods.SendMessage(c.Handle, NativeMethods.WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
            c.Invalidate();
        }
        private static class NativeMethods
        {
            public const int SW_RESTORE = 9;
            public const int WM_SETREDRAW = 0x000B;
            [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
            [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
            [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hWnd);
            [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        }
        // ==================== CẬP NHẬT HAI COMBOBOX ====================
        private void CapNhatDanhSachComboboxHaiChieu(
            List<string> danhSachXuat, List<string> danhSachNap,
            List<GoiXuatLuuTru> luuTruXuat, List<GoiNhapLuuTru> luuTruNap)
        {
            _luuTruCoTheXuat.Clear();
            _luuTruCoTheXuat.AddRange(luuTruXuat);
            _luuTruCoTheNap.Clear();
            _luuTruCoTheNap.AddRange(luuTruNap);
            bool coLuuTruDeNap = luuTruNap.Any(g => g.Tep.Count > 0);
            _khoaSuKien++;
            cbo_ChonCSDL_Nguon.BeginUpdate();
            comboBox1_CSDLDeNapVao.BeginUpdate();
            try
            {
                // --- 1. COMBOBOX XUẤT DỮ LIỆU ---
                string duongDanCu = _duongDanCSDL_Nguon;
                bool chonTatCa = checkBox1_ChonTatCaCacCSDL.Checked;
                cbo_ChonCSDL_Nguon.Items.Clear();
                _danhSachDuongDanThucTe.Clear();
                _danhSachDuongDanThucTe.AddRange(danhSachXuat);
                if (chonTatCa) cbo_ChonCSDL_Nguon.Items.Add(MucTatCa);
                int viTriChon = chonTatCa ? 0 : -1;
                foreach (string duongDan in danhSachXuat)
                {
                    int viTri = cbo_ChonCSDL_Nguon.Items.Add(new MucCSDL(TenHienThi(duongDan), duongDan));
                    // Giữ nguyên lựa chọn của người dùng sau khi quét lại
                    if (!chonTatCa && viTriChon < 0 && string.Equals(duongDan, duongDanCu, StringComparison.OrdinalIgnoreCase))
                        viTriChon = viTri;
                }
                if (cbo_ChonCSDL_Nguon.Items.Count > 0)
                    cbo_ChonCSDL_Nguon.SelectedIndex = viTriChon >= 0 ? viTriChon : 0;
                // --- 2. COMBOBOX NẠP DỮ LIỆU (CHỈ LOAD KHI CÓ GÓI MỚI NGOÀI DESKTOP) ---
                _danhSachDuongDanNap.Clear();
                _danhSachDuongDanNap.AddRange(danhSachNap);
                _coDuLieuDeNap = danhSachNap.Count > 0 || coLuuTruDeNap;
                comboBox1_CSDLDeNapVao.Items.Clear();
                if (_coDuLieuDeNap)
                {
                    // Gói chỉ có thư mục lưu trữ: không có CSDL riêng lẻ để chọn nên bắt buộc ở chế độ "Tất cả" (đang trong _khoaSuKien)
                    if (danhSachNap.Count == 0) checkBox1_ChonTatCaCacCSDLDeNap.Checked = true;
                    if (checkBox1_ChonTatCaCacCSDLDeNap.Checked) comboBox1_CSDLDeNapVao.Items.Add(MucTatCa);
                    foreach (string duongDan in danhSachNap)
                        comboBox1_CSDLDeNapVao.Items.Add(new MucCSDL(TenHienThi(duongDan), duongDan));
                    comboBox1_CSDLDeNapVao.SelectedIndex = 0;
                }
                else
                {
                    // Không có gói nào để nạp: đặt lại ô tích và hiện dòng giữ chỗ (không phải MucCSDL nên không thể bị nạp nhầm)
                    checkBox1_ChonTatCaCacCSDLDeNap.Checked = false;
                    comboBox1_CSDLDeNapVao.Items.Add("(Không có dữ liệu mới ngoài Desktop)");
                    comboBox1_CSDLDeNapVao.SelectedIndex = 0;
                }
                ApDungTrangThaiNap(); // đang khóa UI thì bỏ qua; MoKhoaUI sẽ áp dụng
            }
            finally
            {
                cbo_ChonCSDL_Nguon.EndUpdate();
                comboBox1_CSDLDeNapVao.EndUpdate();
                _khoaSuKien--;
            }
        }
        private void kryptonButton1_HuongDanSuDung_Click(object? sender, EventArgs e)
        {
            string huongDan =
                "HƯỚNG DẪN SỬ DỤNG TÍNH NĂNG CHUYỂN GIAO DỮ LIỆU\n\n" +
                "1. XUẤT DỮ LIỆU RA TỆP JSON:\n" +
                "• Bước 1: Chọn cơ sở dữ liệu nguồn cần xuất (hoặc tích chọn 'Chọn tất cả' để xuất các CSDL của hệ thống; có thể bỏ tích CSDL không muốn xuất).\n" +
                "• Bước 2: Tích chọn các bảng dữ liệu cụ thể trong danh sách cấu trúc bên trái.\n" +
                "• Bước 3: Bấm nút 'Xuất dữ liệu'. Hệ thống sẽ kết xuất các tệp .json kèm tệp thông tin phiên bản (sync_info) vào thư mục 'Database-PhanMemThiDua2026' ngoài Desktop.\n" +
                "• Ở chế độ 'Chọn tất cả', nếu hệ thống đang có dữ liệu trong thư mục 'Bansaoluu' (bản sao lưu CSDL) và 'LuuTruThiDua_LichSu' (kết quả thi đua, khen thưởng các năm trước) thì hai thư mục này hiện trong danh sách và được đóng gói kèm (bỏ tích nếu không muốn gửi). Thư mục không có dữ liệu thì không xuất.\n\n" +
                "2. NHẬP DỮ LIỆU VÀO HỆ THỐNG:\n" +
                "• Bước 1: Khi có các gói dữ liệu mới trên Desktop, hệ thống sẽ tự động quét và hiển thị vào danh sách chọn ở phần Nhập.\n" +
                "• Bước 2: Chọn cơ sở dữ liệu đích cần nạp vào (hoặc tích chọn 'Chọn tất cả'). Hệ thống nạp các bảng có trong gói JSON của CSDL đó.\n" +
                "• Bước 3: Lựa chọn các tùy chọn an toàn:\n" +
                "  - 'Sao lưu trước khi chuyển dữ liệu': tạo bản sao lưu .bak bảo hiểm (khuyên dùng). Sao lưu lỗi thì CSDL đó sẽ KHÔNG được nạp.\n" +
                "  - 'Xóa dữ liệu trong csdl trước khi nạp': làm sạch dữ liệu cũ của các bảng được nạp.\n" +
                "• Bước 4: Bấm nút 'Nhập dữ liệu'. Hệ thống sẽ tự động nạp, tối ưu hóa (Vacuum) và ghi nhận lịch sử an toàn. Chỉ các tệp đã nạp thành công mới được dọn khỏi Desktop; tệp nạp lỗi được giữ lại để bạn thử lại.\n" +
                "• Thư mục 'Bansaoluu' và 'LuuTruThiDua_LichSu' trong gói (nếu có và đầy đủ tệp xác nhận) chỉ được nhập khi chọn 'Chọn tất cả'. Mỗi tệp được kiểm tra toàn vẹn trước khi nhập; tệp đã có giống hệt thì bỏ qua; tệp trùng tên nhưng khác nội dung được giữ nguyên bản hiện tại, trừ khi tích 'Xóa dữ liệu trong csdl trước khi nạp' (bản cũ sẽ được sao lưu trước nếu bật 'Sao lưu trước khi chuyển dữ liệu').\n\n" +
                "CÁC TÍNH NĂNG HỖ TRỢ:\n" +
                "• Nút 'Quét tìm kiếm': làm mới và kiểm tra lại các tệp dữ liệu thực tế trên máy.\n" +
                "• Nút 'Mở thư mục': mở nhanh thư mục chứa dữ liệu ngoài Desktop để bạn dễ dàng kiểm tra trực quan.";
            MessageBox.Show(this, huongDan, "Hướng dẫn sử dụng - Chuyển giao dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
///Phiên bản 4 - tối ưu hiệu suất / bộ nhớ / an toàn (giữ nguyên Designer và tên control)
////TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026