using System.Diagnostics;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    // =====================================================================================================
    //  Form55 = "vỏ" chứa 2 trang con: Form42 (quản lý Ba Nhất) và Form44 (Sổ vàng).
    //
    //  NGUYÊN TẮC (để mở / chuyển trang mượt, không khựng):
    //   1) CHUYỂN TRANG chỉ là Show/Hide 2 form đã dựng sẵn trong RAM (đồng bộ, vài ms). Không đọc CSDL, không giữ khóa.
    //      (Bản cũ giữ khóa điều hướng trong suốt lúc đồng bộ + nạp dữ liệu => bấm menu phải đợi xong cả lượt nạp mới chuyển.)
    //   2) NẠP DỮ LIỆU luôn chạy NỀN sau khi trang đã hiện; mỗi trang chỉ 1 luồng nạp, yêu cầu đến chồng nhau được gộp lại.
    //   3) DỰNG GIAO DIỆN nặng (tạo form, tạo handle, bố cục) chia thành các bước nhỏ ~8 ms chạy lúc rảnh
    //      (cùng mô hình ChuanBiGiaoDienTungPhan của Form60) - Form6 có thể kích hoạt từ trước khi người dùng bấm nút.
    //   4) Dữ liệu Form42 do Form55 điều phối (Form42.FormChaQuanLyNapDuLieu = true) => Form42.Load không nạp lặp
    //      (bản cũ: Form55 nạp 1 lần + Form42.Load nạp thêm 1-2 lần nữa, đọc + giải mã lại cả bảng).
    // =====================================================================================================
    public partial class Form55_QuanLyHeThongThiDuaBaNhat : Form
    {
        // ===================== CẤU HÌNH CỐ ĐỊNH =====================
        private readonly int _namHeThong = Module_HeThong.LayNamHeThong();
        private Form42_QuanLyThiDuaBaNhat? _frm42;
        private Form44_SoVangBaNhat? _frm44;
        private enum Trang { Chua, BaNhat42, SoVang44 }
        private Trang _trang = Trang.Chua;          // trang người dùng đã chọn gần nhất
        private bool _dangDongForm;
        private bool _daDonDep;
        public bool DaLoadDuLieu { get; private set; }
        // ===================== CỜ "CẦN NẠP LẠI" =====================
        private bool _canNapLai;                    // dữ liệu nguồn (Loại 1) đổi  -> đồng bộ + nạp lại Form42
        private bool _canLamMoiTinhTrang42;         // Sổ vàng đổi                -> chỉ vá cột "Tình trạng" của Form42
        private bool _canNapLai44;                  // Sổ vàng đổi                -> nạp lại Form44
        //public void DanhDauCanNapLai() => _canNapLai = true;                          // gọi khi dữ liệu nguồn thay đổi
        public void DanhDauCanNapLaiSoVang() => _canNapLai44 = true;                  // Form42 gọi sau khi lưu vào Sổ vàng
        public void DanhDauCanLamMoiTinhTrang42() => _canLamMoiTinhTrang42 = true;    // Form44 gọi sau khi sửa/xóa/nhập Sổ vàng
        // ===================== BỘ NẠP MỘT LUỒNG =====================
        // Mỗi lúc chỉ chạy 1 lượt nạp; yêu cầu đến khi đang chạy được gộp thành đúng 1 lượt chạy lại.
        // Mọi thứ chạy trên luồng UI nên không cần khóa. Task trả về hoàn tất khi lượt (và lượt chạy lại) kết thúc.
        private sealed class BoNapMotLuong
        {
            private readonly Func<Task> _viec;
            private Task? _dangChay;
            private bool _chayLai;
            public BoNapMotLuong(Func<Task> viec) => _viec = viec;
            public Task YeuCau()
            {
                if (_dangChay is { IsCompleted: false }) { _chayLai = true; return _dangChay; }
                return _dangChay = ChayAsync();
            }
            private async Task ChayAsync()
            {
                do
                {
                    _chayLai = false;
                    try { await _viec(); }
                    catch (Exception ex) { Debug.WriteLine("[Form55.NapNen] " + ex); }
                }
                while (_chayLai);
            }
        }
        private readonly BoNapMotLuong _bo42;
        private readonly BoNapMotLuong _bo44;
        // ===================== KHỞI TẠO =====================
        public Form55_QuanLyHeThongThiDuaBaNhat()
        {
            InitializeComponent();
            _bo42 = new BoNapMotLuong(NapDuLieu42Async);
            _bo44 = new BoNapMotLuong(NapDuLieu44Async);
            BatDoubleBufferDeQuy(this);               // chỉ tinh chỉnh hiển thị: giảm chớp khi chuyển trang con
            Disposed += (s, e) => DonDepKhiDong();
        }
        private bool Dong() => _dangDongForm || IsDisposed;
        // ===== Giảm chớp: DoubleBuffered cho container (không đụng control khác) =====
        private static readonly System.Reflection.PropertyInfo? _pDoubleBuffered =
            typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        private static void BatDoubleBufferDeQuy(Control goc)
        {
            foreach (Control c in goc.Controls)
            {
                if (c is TableLayoutPanel || c is GroupBox || c is Panel || c is SplitContainer || c is TabPage)
                    _pDoubleBuffered?.SetValue(c, true);
                BatDoubleBufferDeQuy(c);
            }
        }
        // ===== Tắt/bật vẽ khi đổi trang: chỉ vẽ lại 1 lần sau khi Show/Hide xong =====
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr h, int msg, int w, int l);
        private const int WM_SETREDRAW = 0x000B;
        private static void TatVe(Control c) { if (c.IsHandleCreated) SendMessage(c.Handle, WM_SETREDRAW, 0, 0); }
        private static void BatVe(Control c) { if (c.IsHandleCreated) { SendMessage(c.Handle, WM_SETREDRAW, 1, 0); c.Invalidate(true); } }
        private void ChuyenForm(Form hien, Form? an)
        {
            if (an != null && an.IsDisposed) an = null;
            TatVe(panelContent);
            try
            {
                hien.Show();
                hien.BringToFront();
                if (an != null && !ReferenceEquals(an, hien)) an.Hide();
            }
            finally { BatVe(panelContent); }
        }
        // ===================== DỰNG FORM CON (LAZY) =====================
        // Đặt sẵn đúng kích thước vùng chứa TRƯỚC khi dựng bố cục: lúc Show thật (Dock = Fill) kích thước không đổi
        // nên các TableLayoutPanel/SplitContainer lồng nhau không phải bố trí lại lần thứ 2.
        private void DatKichThuocTheoVung(Form f)
        {
            Rectangle r = panelContent.ClientRectangle;
            if (r.Width > 0 && r.Height > 0) f.Bounds = r;
        }
        private void TaoForm42NeuCan()
        {
            if (Dong() || (_frm42 != null && !_frm42.IsDisposed)) return;
            var f = new Form42_QuanLyThiDuaBaNhat
            {
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill,
                Visible = false,
                FormChaQuanLyNapDuLieu = true       // Form55 điều phối dữ liệu => Form42.Load chỉ dựng giao diện
            };
            DatKichThuocTheoVung(f);
            panelContent.Controls.Add(f);
            _frm42 = f;
        }
        private void TaoForm44NeuCan()
        {
            if (Dong() || (_frm44 != null && !_frm44.IsDisposed)) return;
            var f = new Form44_SoVangBaNhat
            {
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill,
                Visible = false
            };
            DatKichThuocTheoVung(f);
            panelContent.Controls.Add(f);
            _frm44 = f;
        }
        // ===================== KHUNG CHỜ (CHỈ DÙNG KHI CHƯA DỰNG SẴN) =====================
        // Mở lần đầu mà chưa kịp dựng sẵn: hiện khung + dòng chữ NGAY, việc dựng Form42 để sau khi khung đã vẽ.
        private Label? _lblDangTai;
        private void HienChoDangTai()
        {
            if (_lblDangTai != null || _frm42 != null || _frm44 != null) return;
            _lblDangTai = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "Đang mở trang quản lý thi đua Ba Nhất...",
                ForeColor = Color.FromArgb(71, 85, 105),
                BackColor = Color.FromArgb(244, 246, 250),
                Font = new Font(Module_HeThong.TenFontHeThong, 12F)
            };
            panelContent.Controls.Add(_lblDangTai);
        }
        private void AnChoDangTai()
        {
            var l = _lblDangTai;
            if (l == null) return;
            _lblDangTai = null;
            try
            {
                panelContent.Controls.Remove(l);
                Font ft = l.Font;
                l.Dispose();
                ft.Dispose();                       // Font do chính khung chờ tạo ra
            }
            catch (Exception ex) { Debug.WriteLine("[Form55.AnChoDangTai] " + ex.Message); }
        }
        private void KhiRanhMotLan(Action viec)
        {
            EventHandler? h = null;
            h = (s, e) =>
            {
                Application.Idle -= h;
                if (!Dong()) viec();
            };
            Application.Idle += h;
        }
        // ===================== DỰNG SẴN TỪNG PHẦN (LÚC RẢNH) =====================
        private IEnumerator<bool>? _kichBanChuanBi;
        private bool _daChuanBiXong;
        private bool _dangChayBuoc;
        private System.Windows.Forms.Timer? _timerNen;
        /// <summary>
        /// Bắt đầu dựng sẵn Form42 + Form44 (không Show, không đọc dữ liệu) theo từng bước nhỏ bằng bộ hẹn giờ nội bộ.
        /// Gọi nhiều lần an toàn. Form6 có thể gọi ngay khi tạo Form55 ở trạng thái ẩn để lúc bấm nút chỉ còn Show().
        /// </summary>
        public void BatDauChuanBiNen()
        {
            if (Dong() || _daChuanBiXong || _timerNen != null) return;
            _timerNen = new System.Windows.Forms.Timer { Interval = 30 };
            _timerNen.Tick += (s, e) =>
            {
                if (Control.MouseButtons != MouseButtons.None) return;     // đang nhấn/kéo chuột: nhường người dùng
                if (!ChuanBiGiaoDienTungPhan(8)) DungTimerNen();
            };
            _timerNen.Start();
        }
        private void DungTimerNen()
        {
            var t = _timerNen;
            _timerNen = null;
            if (t == null) return;
            try { t.Stop(); t.Dispose(); } catch { /* dọn dẹp best-effort */ }
        }
        /// <summary>
        /// Chạy các bước dựng sẵn trong tối đa nganSachMs mili-giây (mỗi lần gọi luôn làm ít nhất 1 bước).
        /// Trả về true nếu còn việc, false nếu đã xong (hoặc đã dừng).
        /// </summary>
        public bool ChuanBiGiaoDienTungPhan(int nganSachMs)
        {
            if (Dong() || _daChuanBiXong) return false;
            if (_dangChayBuoc) return true;                       // chống chạy chồng (iterator đang chạy sẽ báo "hết" nếu bị gọi lại)
            _dangChayBuoc = true;
            try
            {
                _kichBanChuanBi ??= CacBuocChuanBi().GetEnumerator();
                var sw = Stopwatch.StartNew();
                do
                {
                    if (!_kichBanChuanBi.MoveNext()) { KetThucChuanBi(); return false; }
                }
                while (sw.ElapsedMilliseconds < nganSachMs);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Form55.ChuanBi] " + ex.Message);
                KetThucChuanBi();                                 // bỏ cuộc: lúc mở thật form vẫn tự dựng khi cần
                return false;
            }
            finally { _dangChayBuoc = false; }
        }
        private void KetThucChuanBi()
        {
            _daChuanBiXong = true;
            try { _kichBanChuanBi?.Dispose(); } catch { }
            _kichBanChuanBi = null;
            DungTimerNen();
        }
        ///// <summary>Tương thích bản cũ: dựng sẵn toàn bộ ngay (dùng khi cần chủ động, bình thường dùng BatDauChuanBiNen).</summary>
        // Mỗi "yield return" là ranh giới giữa 2 bước; trình bơm quyết định có chạy tiếp ngay hay nhường giao diện.
        private IEnumerable<bool> CacBuocChuanBi()
        {
            // ----- Trang Ba Nhất (Form42) -----
            if (Dong()) yield break;
            TaoForm42NeuCan(); yield return true;      // dựng form (khối nặng nhất, không chia nhỏ được)
            if (Dong()) yield break;
            var f42 = _frm42;
            if (f42 != null && !f42.IsDisposed) f42.ChuanBiGiaoDienTruoc(); yield return true;   // bộ lọc, tooltip, cột lưới, menu chuột phải
            if (f42 != null)
                foreach (bool b in TaoHandleTungControl(f42)) yield return b;
            if (f42 != null)
                foreach (bool b in BoCucTruocTungPhan(f42)) yield return b;
            // ----- Trang Sổ vàng (Form44) -----
            if (Dong()) yield break;
            TaoForm44NeuCan(); yield return true;
            if (Dong()) yield break;
            var f44 = _frm44;
            if (f44 != null && !f44.IsDisposed) f44.ChuanBiGiaoDienTruoc(); yield return true;
            if (f44 != null)
                foreach (bool b in TaoHandleTungControl(f44)) yield return b;
            if (f44 != null)
                foreach (bool b in BoCucTruocTungPhan(f44)) yield return b;
            NapTruocSoVangNeuRanh();                             // chỉ có tác dụng khi người dùng đang ở trong trang Ba Nhất
        }
        // Tạo handle từng control (duyệt cây không đệ quy): mỗi bước 1 control, rất nhẹ
        private IEnumerable<bool> TaoHandleTungControl(Control goc)
        {
            var ngan = new Stack<Control>();
            ngan.Push(goc);
            while (ngan.Count > 0)
            {
                if (Dong()) yield break;
                var c = ngan.Pop();
                if (c.IsDisposed) continue;
                if (!c.IsHandleCreated)
                {
                    try { _ = c.Handle; }
                    catch (Exception ex) { Debug.WriteLine("[Form55.Handle] " + ex.Message); continue; }
                }
                for (int i = c.Controls.Count - 1; i >= 0; i--) ngan.Push(c.Controls[i]);
                yield return true;
            }
        }
        // Bố cục trước từng khối (con trước, cha sau) ở đúng kích thước vùng chứa
        private static void ThuThapKhoiBoCuc(Control goc, List<Control> ds)
        {
            foreach (Control c in goc.Controls) ThuThapKhoiBoCuc(c, ds);
            if (goc.HasChildren && (goc is ScrollableControl || goc is GroupBox)) ds.Add(goc);
        }
        private IEnumerable<bool> BoCucTruocTungPhan(Form f)
        {
            var ds = new List<Control>();
            ThuThapKhoiBoCuc(f, ds);
            foreach (var c in ds)
            {
                if (Dong()) yield break;
                if (c.IsDisposed) continue;
                c.PerformLayout();
                yield return true;
            }
        }
        // ===================== NẠP DỮ LIỆU (NỀN, KHÔNG GIỮ KHÓA ĐIỀU HƯỚNG) =====================
        private async Task NapDuLieu42Async()
        {
            var f = _frm42;
            if (f == null || f.IsDisposed || _dangDongForm) return;
            if (!f.DaLoadDuLieu)
            {
                // Lần đầu: LƯỚI CÓ DỮ LIỆU NGAY (1 lượt đọc), rồi mới đồng bộ nền; chỉ nạp lại / vá khi thật sự có đổi
                _canNapLai = false;
                _canLamMoiTinhTrang42 = false;
                await f.LoadDuLieuToanBoDanhSachBaNhatAsync();
                if (_dangDongForm || f.IsDisposed) return;
                await f.DongBoNenSauKhiMoAsync();
            }
            else if (_canNapLai)
            {
                // Dữ liệu nguồn đổi: đồng bộ Loại 1 -> tình trạng Sổ vàng -> nạp lại nếu danh sách đổi, ngược lại chỉ vá cột
                _canNapLai = false;
                _canLamMoiTinhTrang42 = false;
                await f.DongBoNenSauKhiMoAsync();
            }
            else if (_canLamMoiTinhTrang42)
            {
                // Chỉ Sổ vàng đổi (Form44 đã tự cập nhật cột tình trạng trong CSDL): vá đúng cột "Tình trạng", lưới không bind lại
                _canLamMoiTinhTrang42 = false;
                await f.LamMoiTinhTrangTrenLuoiNgayAsync();
            }
            if (_dangDongForm || f.IsDisposed) return;
            DaLoadDuLieu = f.DaLoadDuLieu;
            NapTruocSoVangNeuRanh();                // Form42 xong rồi mới nạp sẵn Sổ vàng, hai việc không tranh nhau
        }
        private async Task NapDuLieu44Async()
        {
            var f = _frm44;
            if (f == null || f.IsDisposed || _dangDongForm) return;
            if (f.DaLoadDuLieu && !_canNapLai44) return;
            _canNapLai44 = false;
            await f.LoadDuLieuSoVangBaNhatAsync();
        }
        // Nạp sẵn Sổ vàng vào RAM khi người dùng đang ở trang Ba Nhất (bấm sang Sổ vàng là có ngay)
        private void NapTruocSoVangNeuRanh()
        {
            if (Dong() || !Visible) return;
            var f = _frm44;
            if (f == null || f.IsDisposed || f.DaLoadDuLieu) return;
            _ = _bo44.YeuCau();
        }
        // ===================== FORM LOAD =====================
        private async void Form55_QuanLyHeThongThiDuaBaNhat_Load(object? sender, EventArgs e)
        {
            if (_dangDongForm) return;
            try
            {
                if (_frm42 == null || _frm42.IsDisposed)
                {
                    // Chưa dựng sẵn: hiện khung chờ ngay, dựng Form42 ngay sau khi khung đã vẽ (không để cửa sổ "đứng hình")
                    HienChoDangTai();
                    KhiRanhMotLan(MoTrangDauTien);
                    return;
                }
                await MoForm42Async();
            }
            catch (Exception ex)
            {
                ThongBaoLoiKhoiTao(ex);
            }
        }
        private async void MoTrangDauTien()
        {
            try
            {
                if (Dong() || _trang == Trang.SoVang44) return;     // người dùng đã tự bấm sang Sổ vàng thì không kéo ngược về
                await MoForm42Async();
            }
            catch (Exception ex)
            {
                ThongBaoLoiKhoiTao(ex);
            }
        }
        private void ThongBaoLoiKhoiTao(Exception ex)
        {
            if (Dong()) return;
            MessageBox.Show(this, "Không thể khởi tạo dữ liệu thi đua Ba Nhất.\n\n" + ex.Message, "Lỗi khởi tạo", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        // ===================== HIỂN THỊ TRANG =====================
        /// <summary>Hiện trang Ba Nhất NGAY (đồng bộ), dữ liệu nạp nền. Task hoàn tất khi lượt nạp nền xong.</summary>
        public Task MoForm42Async()
        {
            if (Dong()) return Task.CompletedTask;
            _trang = Trang.BaNhat42;
            AnChoDangTai();
            TaoForm42NeuCan();
            var f42 = _frm42;
            if (f42 == null || f42.IsDisposed) return Task.CompletedTask;
            ChuyenForm(f42, _frm44);
            CapNhatTieuDeFormChinh($"Quản lý phong trào thi đua Ba Nhất năm {_namHeThong}");
            BatDauChuanBiNen();                      // dựng nốt phần còn lại (Form44...) lúc rảnh
            return _bo42.YeuCau();
        }
        /// <summary>Hiện trang Sổ vàng NGAY (đồng bộ), dữ liệu nạp nền. Task hoàn tất khi lượt nạp nền xong.</summary>
        public Task MoForm44Async()
        {
            if (Dong()) return Task.CompletedTask;
            _trang = Trang.SoVang44;
            AnChoDangTai();
            TaoForm44NeuCan();
            var f44 = _frm44;
            if (f44 == null || f44.IsDisposed) return Task.CompletedTask;
            ChuyenForm(f44, _frm42);
            CapNhatTieuDeFormChinh($"Sổ vàng vinh danh thi đua Ba Nhất năm {_namHeThong}");
            BatDauChuanBiNen();
            return _bo44.YeuCau();
        }
        // MENU - FORM 42
        private async void quanLyThiDuaBaNhat_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            try
            {
                await MoForm42Async();
            }
            catch (Exception ex)
            {
                XuLyLoiHeThong("Không thể mở quản lý thi đua Ba Nhất.", ex);
            }
        }
        // MENU - FORM 44
        private async void quanLySoVangThiDuaBaNhat_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            try
            {
                await MoForm44Async();
            }
            catch (Exception ex)
            {
                XuLyLoiHeThong("Không thể mở Sổ vàng thi đua Ba Nhất.", ex);
            }
        }
        // RELOAD DỮ LIỆU (giữ nguyên hành vi: trang nào đang hiện thì nạp lại trang đó)
        public async Task ReloadDuLieu()
        {
            if (Dong()) return;
            var f42 = _frm42;
            if (f42 != null && !f42.IsDisposed && f42.Visible)
            {
                await f42.DongBoDuLieuLoai1SangBaNhatAsync();
                if (!Dong() && !f42.IsDisposed)
                    await f42.LoadDuLieuToanBoDanhSachBaNhatAsync();
                return;
            }
            var f44 = _frm44;
            if (f44 != null && !f44.IsDisposed && f44.Visible)
                await f44.LoadDuLieuSoVangBaNhatAsync();
        }
        // CẬP NHẬT TIÊU ĐỀ FORM CHA
        private void CapNhatTieuDeFormChinh(string tieuDe)
        {
            if (string.IsNullOrWhiteSpace(tieuDe))
                return;
            var frmChinh = Application.OpenForms
                .OfType<Form2_FormCha>()
                .FirstOrDefault();
            if (frmChinh == null || frmChinh.IsDisposed)
                return;
            frmChinh.CapNhatTieuDe(tieuDe);
        }
        // XỬ LÝ LỖI TẬP TRUNG
        private void XuLyLoiHeThong(string thongBao, Exception ex)
        {
            if (Dong()) return;
            MessageBox.Show(
                this,
                thongBao + "\n\n" + ex.Message,
                "Lỗi hệ thống",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        // ===================== ĐÓNG FORM =====================
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (!e.Cancel) DonDepKhiDong();         // chỉ dọn khi việc đóng thật sự diễn ra (không bị ai hủy)
        }
        // Gọi từ OnFormClosing và từ Disposed (đóng cả ứng dụng thì form con bị Dispose mà không qua FormClosing). Chạy 1 lần.
        private void DonDepKhiDong()
        {
            if (_daDonDep) return;
            _daDonDep = true;
            _dangDongForm = true;                   // các tác vụ nền đang chờ sẽ tự thoát ở lần kiểm tra kế tiếp
            try
            {
                DungTimerNen();
                try { _kichBanChuanBi?.Dispose(); } catch { }
                _kichBanChuanBi = null;
                var f42 = _frm42; _frm42 = null;
                if (f42 != null && !f42.IsDisposed) f42.Dispose();
                var f44 = _frm44; _frm44 = null;
                if (f44 != null && !f44.IsDisposed) f44.Dispose();
                // Không còn SemaphoreSlim để Dispose: bản cũ Dispose khóa khi vẫn còn tác vụ async đang chờ/giải phóng nó
                // (ObjectDisposedException ở khối finally). Điều hướng nay đồng bộ trên luồng UI nên không cần khóa.
            }
            catch
            {
                // Không để lỗi dọn dẹp chặn quá trình đóng Form.
            }
        }
    }
}
