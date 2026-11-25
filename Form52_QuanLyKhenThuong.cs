using System.Reflection;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form52_QuanLyKhenThuong : Form // hoặc KryptonForm
    {
        // ================= CẤU HÌNH =================
        /// <summary>Đặt false nếu form con có handler sự kiện static chạy khi chưa hiện.</summary>
        private const bool NapTruocTaoHandle = true;
        /// <summary>Tên các hàm reload công khai (không tham số) sẽ được tự nhận diện trên form con.</summary>
        private static readonly string[] TenHamReload = { "ReloadDuLieu", "ReloadDuLieuAsync", "ReloadData", "ReloadDataAsync" };
        // 1. Cache năm hệ thống để giảm I/O Database
        private readonly int _namHeThong = Module_HeThong.LayNamHeThong();
        // ================= RAM CACHE =================
        private readonly Dictionary<Type, Func<Form>> _nhaMay = new();          // cách dựng từng loại form con
        private readonly Dictionary<Type, Form> _boNho = new();                 // form đang dùng, giữ trong RAM
        private readonly Dictionary<Type, Form> _formDuPhong = new();           // form dựng sẵn, CHƯA Show (dùng 1 lần)
        private readonly Dictionary<Type, List<Form>> _chiTiet = new();         // form chi tiết "ném" lên trên từng tab
        private readonly Dictionary<Form, string> _tieuDe = new();              // tiêu đề của từng trang
        private readonly HashSet<Form> _daTungHien = new();
        private readonly Dictionary<Type, MethodInfo?> _hamReload = new();
        private readonly Dictionary<Type, long> _lanTaiCuoi = new();            // Environment.TickCount64
        private readonly Dictionary<Type, Task> _dangTai = new();               // mỗi form chỉ tải 1 lần tại 1 thời điểm
        private readonly HashSet<Type> _canTaiLai = new();
        private readonly System.Threading.SynchronizationContext? _ui;
        private Form? _activeSubForm;      // form GỐC của tab đang mở
        private bool _isClosing;
        private bool _dangNapDuPhong;
        // 2. Private set để bảo vệ tính toàn vẹn trạng thái
        public bool DaLoadDuLieu { get; private set; } = false;
        public Form52_QuanLyKhenThuong()
        {
            InitializeComponent();
            Module_CheDoDoiTuong.ApNhanTheoCheDo(this);   // nhãn "CBCS" -> "Tân binh"/"Học viên" theo chế độ (trước khi dựng giao diện hiện đại)
            Module_BieuTuongNut.ApDung(this);   // biểu tượng nút: Hiện đại = tự vẽ, Cổ điển = ảnh gốc nhúng trong exe
            BatDoubleBufferDeQuy(this);   // giảm chớp khi chuyển trang con
            _ui = System.Threading.SynchronizationContext.Current;
            DangKy<Form34_ThongKeKhenThuong>();
            DangKy<Form49_QuanLyKhenThuongTapThe>();
            DangKy<Form50_QuanLyKhenThuongNamCu>();
            DangKy<Form51_QuanLyKhenThuongTapTheNamCu>();
            Module_TaiKhoan.OnPhienBanThayDoi += KhiPhienBanThayDoi_HuyFormDuPhong;
            Disposed += (s, e) =>
            {
                _isClosing = true;
                Module_TaiKhoan.OnPhienBanThayDoi -= KhiPhienBanThayDoi_HuyFormDuPhong;
                foreach (var f in _formDuPhong.Values) HuyForm(f);   // form chưa gắn vào Panel thì Dispose cha không dọn tới
                _formDuPhong.Clear();
            };
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (!e.Cancel) _isClosing = true;   // chặn các tác vụ ngầm còn dang dở
        }
        private void DangKy<T>() where T : Form, new() => _nhaMay[typeof(T)] = () => TaoForm<T>();
        private T TaoForm<T>() where T : Form, new()
        {
            var f = new T
            {
                TopLevel = false,                      // đặt TRƯỚC khi tạo handle để không phải tạo lại handle
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill
            };
            BatDoubleBufferDeQuy(f);
            if (f is Form34_ThongKeKhenThuong frm34)
                frm34.YeuCauLongFormVaoPanel += NhanFormConTuBenNgoai;   // chỉ đăng ký 1 lần lúc tạo
            return f;
        }
        // ================= KHỞI TẠO =================
        private async void Form52_QuanLyKhenThuong_Load(object? sender, EventArgs e)
        {
            quanLyKhenThuongNamHienTai_ToolStripMenuItem.Text = $"Quản lý khen thưởng năm {_namHeThong}";
            await MoTabAsync(typeof(Form34_ThongKeKhenThuong), $"Quản lý khen thưởng {Module_CheDoDoiTuong.TenDoiTuongHienTai} năm {_namHeThong}");
            // Trang đầu xong rồi mới dựng sẵn các trang còn lại (không chặn, không đổi luồng)
            _ = NapTruocGiaoDienAsync();
        }
        /// <summary>
        /// Gọi từ bên ngoài khi dữ liệu thay đổi: tải lại ngay tab đang mở,
        /// các tab khác tự làm mới ở lần mở kế tiếp.
        /// </summary>
        public async Task ReloadDuLieu()
        {
            if (_isClosing || IsDisposed) return;
            _lanTaiCuoi.Clear();
            if (_activeSubForm != null && !_activeSubForm.IsDisposed)
            {
                await TaiDuLieuAsync(_activeSubForm, ep: true);
            }
        }
        // ================= CHUYỂN TAB =================
        /// <summary>
        /// Chuyển tab: HIỆN NGAY từ RAM (đồng bộ), rồi mới tải dữ liệu ngầm.
        /// Bấm lại đúng tab đang mở = làm mới dữ liệu tab đó.
        /// </summary>
        private async Task MoTabAsync(Type t, string tieuDe)
        {
            if (_isClosing || IsDisposed) return;
            bool dangMo = _activeSubForm != null && !_activeSubForm.IsDisposed && _activeSubForm.GetType() == t;
            if (dangMo)
            {
                if (!CoChiTiet(t) && LaDungLau(t)) await TaiDuLieuAsync(_activeSubForm!, ep: true);
                return;
            }
            Form goc;
            try
            {
                using (Module_DoHieuNang.Doan($"Form52: ChuyenTab {t.Name} (dựng + Show trang con)")) goc = ChuyenTab(t, tieuDe);
            }
            catch (Exception ex)
            {
                DaLoadDuLieu = false;
                System.Diagnostics.Debug.WriteLine("[Form52.ChuyenTab] " + ex.Message);
                MessageBox.Show("Lỗi khi mở giao diện:\n" + ex.Message, "Lỗi hệ thống",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // Tab đang có form chi tiết đè lên thì không cần tải form gốc
            if (!CoChiTiet(t)) await TaiDuLieuAsync(goc, ep: false);
        }
        private Form ChuyenTab(Type t, string tieuDe)
        {
            Form? trangCu = TrangDangHien();
            Form? gocCu = _activeSubForm;
            Form goc;
            using (Module_DoHieuNang.Doan($"Form52: LayHoacTaoGoc {t.Name} (new + InitializeComponent)")) goc = LayHoacTaoGoc(t);
            Form hien = TrangTrenCung(t, goc);
            bool lanDau = _daTungHien.Add(goc);
            panelContent.SuspendLayout();
            try
            {
                if (goc.Parent != panelContent) panelContent.Controls.Add(goc);   // chưa Show -> vẫn ẩn
                _activeSubForm = goc;
                _tieuDe[goc] = tieuDe;
                hien.BringToFront();
                using (Module_DoHieuNang.Doan($"Form52: Show {hien.GetType().Name} (Load đồng bộ)")) Module_KhoGiaoDien.ShowTrongKho(hien);   // = Show(); nằm trong kho ẩn thì vẫn chạy Load
                // Ẩn trang cũ SAU khi trang mới đã hiện (tránh 1 khung hình nền trống)
                if (trangCu != null && trangCu != hien) trangCu.Hide();
            }
            finally
            {
                using (Module_DoHieuNang.Doan("Form52: ResumeLayout panelContent")) panelContent.ResumeLayout(true);   // chỉ layout 1 lần
            }
            // Form_Load của lần Show đầu đã tự nạp dữ liệu -> khỏi tải lại ngay
            if (lanDau) _lanTaiCuoi[t] = Environment.TickCount64;
            CapNhatTieuDeFormChinh(_tieuDe.TryGetValue(hien, out string? td) ? td : tieuDe);
            using (Module_DoHieuNang.Doan($"Form52: {hien.GetType().Name}.Update (vẽ ngay)")) hien.Update();   // vẽ ngay trước khi tải dữ liệu
            // Tab cũ không có hàm reload: dọn như cũ để lần sau luôn mở bản mới (đã có form dự phòng dựng nền)
            if (gocCu != null && gocCu != goc && !gocCu.IsDisposed && !LaDungLau(gocCu.GetType()))
                HuyTabDungMotLan(gocCu);
            return goc;
        }
        private Form LayHoacTaoGoc(Type t)
        {
            if (_boNho.TryGetValue(t, out Form? dang) && !dang.IsDisposed) return dang;
            Form moi;
            if (_formDuPhong.Remove(t, out Form? duPhong) && !duPhong.IsDisposed) moi = duPhong;
            else moi = _nhaMay[t]();
            _boNho[t] = moi;
            return moi;
        }
        private Form? TrangDangHien()
            => _activeSubForm == null || _activeSubForm.IsDisposed
                ? null
                : TrangTrenCung(_activeSubForm.GetType(), _activeSubForm);
        private Form TrangTrenCung(Type t, Form goc)
        {
            if (_chiTiet.TryGetValue(t, out List<Form>? ds))
            {
                for (int i = ds.Count - 1; i >= 0; i--)
                    if (!ds[i].IsDisposed) return ds[i];
            }
            return goc;
        }
        private bool CoChiTiet(Type t)
            => _chiTiet.TryGetValue(t, out List<Form>? ds) && ds.Any(f => !f.IsDisposed);
        private void HuyTabDungMotLan(Form f)
        {
            Type t = f.GetType();
            if (CoChiTiet(t)) return;   // còn form chi tiết đang mở -> giữ lại, dọn sau khi chi tiết đóng
            _boNho.Remove(t);
            _lanTaiCuoi.Remove(t);
            HuyForm(f);
            _ = DungLaiDuPhongAsync(t);
        }
        private async Task DungLaiDuPhongAsync(Type t)
        {
            try
            {
                await Task.Delay(500);
                await TaoDuPhongAsync(t, () => IsDisposed || _isClosing);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Form52.DungLaiDuPhong] " + ex.Message);
            }
        }
        private void HuyForm(Form f)
        {
            try
            {
                _daTungHien.Remove(f);
                _tieuDe.Remove(f);
                if (f.IsDisposed) return;
                if (!panelContent.IsDisposed && f.Parent == panelContent) panelContent.Controls.Remove(f);
                f.Close();
                f.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Form52.HuyForm] " + ex.Message);
            }
        }
        // ================= FORM CHI TIẾT (ném lên trên tab hiện tại) =================
        // HÀM CHUYÊN NHẬN FORM CHI TIẾT TỪ BÊN TRONG (TẠM ẨN TRANG ĐANG HIỆN)
        public void NhanFormConTuBenNgoai(Form frmDuocNemLen, string tieuDeMoi)
        {
            if (_isClosing || IsDisposed) return;
            Type chu = _activeSubForm?.GetType() ?? typeof(Form);
            Form? dangHien = TrangDangHien();
            if (!_chiTiet.TryGetValue(chu, out List<Form>? ds)) _chiTiet[chu] = ds = new List<Form>();
            if (!ds.Contains(frmDuocNemLen)) ds.Add(frmDuocNemLen);   // form chi tiết dùng lại: không thêm trùng
            _tieuDe[frmDuocNemLen] = tieuDeMoi;
            frmDuocNemLen.TopLevel = false;
            frmDuocNemLen.FormBorderStyle = FormBorderStyle.None;
            frmDuocNemLen.Dock = DockStyle.Fill;
            frmDuocNemLen.FormClosed -= FormChiTiet_FormClosed;
            frmDuocNemLen.FormClosed += FormChiTiet_FormClosed;
            frmDuocNemLen.FormClosing -= FormChiTiet_FormClosing;
            frmDuocNemLen.FormClosing += FormChiTiet_FormClosing;
            panelContent.SuspendLayout();
            try
            {
                panelContent.Controls.Add(frmDuocNemLen);
                frmDuocNemLen.BringToFront();
                frmDuocNemLen.Show();
                if (dangHien != null && dangHien != frmDuocNemLen) dangHien.Hide();   // ẩn SAU khi trang mới đã hiện
            }
            finally
            {
                panelContent.ResumeLayout(true);
            }
            CapNhatTieuDeFormChinh(tieuDeMoi);
        }
        // Quản lý vòng đời chặt chẽ khi đóng Form chi tiết
        /// <summary>
        /// Form chi tiết DÙNG LẠI (Form35 do Form34 giữ 1 bản duy nhất trong RAM): bấm Đóng thì KHÔNG hủy - hủy là lần mở sau
        /// phải dựng lại cả form (~300 ms InitializeComponent + ~230 ms tạo handle + ~130 ms dựng cột lưới). Chặn lệnh đóng,
        /// gỡ khỏi trang rồi cất lại vào kho giao diện; lần mở sau chỉ còn nạp dữ liệu người mới.
        /// </summary>
        private async void FormChiTiet_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (sender is not Form35_ChiTietKhenThuong { DungLai: true } f35) return;
            if (_isClosing || IsDisposed || e.CloseReason == CloseReason.ApplicationExitCall || e.CloseReason == CloseReason.WindowsShutDown) return;
            e.Cancel = true;
            await DongChiTietAsync(f35, huy: false);
        }
        private async void FormChiTiet_FormClosed(object? sender, FormClosedEventArgs e)
        {
            if (sender is not Form frmChiTiet) return;
            await DongChiTietAsync(frmChiTiet, huy: true);
        }
        private async Task DongChiTietAsync(Form frmChiTiet, bool huy)
        {
            if (huy)
            {
                frmChiTiet.FormClosed -= FormChiTiet_FormClosed;
                frmChiTiet.FormClosing -= FormChiTiet_FormClosing;
            }
            if (_isClosing || IsDisposed) return;
            // Chi tiết này có đang là trang hiện trên màn hình không?
            bool dangHien = ReferenceEquals(frmChiTiet, TrangDangHien());
            // BƯỚC 1: gỡ khỏi danh sách
            Type? chu = null;
            foreach (var kv in _chiTiet)
            {
                if (kv.Value.Remove(frmChiTiet)) { chu = kv.Key; break; }
            }
            if (chu == null && !huy) return;   // không nằm trong trang nào (đã được gỡ trước đó)
            _tieuDe.Remove(frmChiTiet);
            Form? goc = null;
            if (chu != null && (!_chiTiet.TryGetValue(chu, out List<Form>? conLai) || conLai.Count == 0))
            {
                _chiTiet.Remove(chu);
            }
            if (chu != null) _boNho.TryGetValue(chu, out goc);
            bool canHienLai = chu != null && goc != null && !goc.IsDisposed && dangHien
                              && _activeSubForm != null && _activeSubForm.GetType() == chu;
            if (huy)
            {
                if (!panelContent.IsDisposed) panelContent.Controls.Remove(frmChiTiet);
                // BƯỚC 2: huỷ để GC dọn RAM
                if (!frmChiTiet.IsDisposed) frmChiTiet.Dispose();
            }
            else
            {
                // Dùng lại: HIỆN trang bên dưới TRƯỚC rồi mới ẩn chi tiết (không lộ 1 khung hình nền trống).
                // CHỈ ẨN, để nguyên trong panel: gỡ ra / đổi cha làm lưới Form35 gắn lại nguồn dữ liệu và tạo lại cửa sổ
                // (đo: đóng ~80 ms, khựng ~65 ms). Form52 chọn trang theo _chiTiet nên form ẩn nằm lại trong panel không ảnh hưởng.
                if (canHienLai)
                {
                    Form hienTruoc = TrangTrenCung(chu!, goc!);
                    hienTruoc.BringToFront();
                    hienTruoc.Show();
                }
                frmChiTiet.Hide();
            }
            if (chu == null || goc == null || goc.IsDisposed) return;
            // BƯỚC 3: đánh thức trang bên dưới (form gốc hoặc chi tiết trước đó)
            if (canHienLai)
            {
                Form hien = TrangTrenCung(chu, goc);
                hien.BringToFront();
                hien.Show();
                CapNhatTieuDeFormChinh(_tieuDe.TryGetValue(hien, out string? td) ? td : string.Empty);
                hien.Update();
                // Làm mới sau khi đóng chi tiết - nhưng chỉ khi CSDL đã đổi (chỉ xem chi tiết rồi đóng thì khỏi đọc + giải mã lại cả CSDL2/CSDL4)
                if (hien == goc && !ChuKyChuaDoi(chu)) await TaiDuLieuAsync(goc, ep: true);
            }
            else if (_activeSubForm == null || _activeSubForm.GetType() != chu)
            {
                // Tab này đã bị rời đi lúc còn chi tiết; giờ chi tiết đóng hết thì dọn nếu là loại dùng 1 lần
                if (!LaDungLau(chu)) HuyTabDungMotLan(goc);
            }
        }
        // ================= TẢI DỮ LIỆU =================
        private Task TaiDuLieuAsync(Form form, bool ep)
        {
            Type t = form.GetType();
            // Đang tải dở: không chạy song song 2 lần trên cùng 1 form
            if (_dangTai.TryGetValue(t, out Task? dangChay))
            {
                if (ep) _canTaiLai.Add(t);   // tải xong sẽ tải thêm 1 vòng nữa
                return dangChay;
            }
            // Dữ liệu còn tươi (CSDL không đổi kể từ lần tải trước) -> khỏi tải lại.
            // Bản cũ còn coi "vừa tải < 10 giây" là tươi dù CSDL đã đổi: sửa/xóa ở Form6 rồi quay lại ngay thì Form34 hiện dữ liệu cũ.
            if (!ep && _lanTaiCuoi.TryGetValue(t, out long truoc) && DuLieuChuaDoi(t, truoc))
            {
                if (form == _activeSubForm) DaLoadDuLieu = true;
                return Task.CompletedTask;
            }
            Task job = ChayTaiAsync(form, t);
            if (!job.IsCompleted) _dangTai[t] = job;
            return job;
        }
        private async Task ChayTaiAsync(Form form, Type t)
        {
            bool loi = false;
            if (form == _activeSubForm) DaLoadDuLieu = false;
            try
            {
                do
                {
                    _canTaiLai.Remove(t);
                    await GoiReloadAsync(form);
                    _lanTaiCuoi[t] = Environment.TickCount64;
                    _chuKyLanTai[t] = Module_ChuKyCSDL.Lay();
                }
                while (_canTaiLai.Contains(t) && !_isClosing && !form.IsDisposed);
            }
            catch (Exception ex)
            {
                loi = true;
                _lanTaiCuoi.Remove(t);
                Exception goc = ex is TargetInvocationException { InnerException: { } trong } ? trong : ex;
                System.Diagnostics.Debug.WriteLine($"[Lỗi tải dữ liệu {t.Name}]: {goc.Message}");
                if (!_isClosing && !IsDisposed)
                {
                    MessageBox.Show("Lỗi tải lại dữ liệu: " + goc.Message, Module_HeThong.TieuDe_Loi,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                _canTaiLai.Remove(t);
                _dangTai.Remove(t);
                if (!_isClosing && form == _activeSubForm) DaLoadDuLieu = !loi;
            }
        }
        private readonly Dictionary<Type, string> _chuKyLanTai = new();
        private const int TuoiDuLieuKhongDoiToiDaPhut = 10;
        private bool ChuKyChuaDoi(Type t)
            => _lanTaiCuoi.ContainsKey(t)
               && _chuKyLanTai.TryGetValue(t, out string? ck)
               && string.Equals(ck, Module_ChuKyCSDL.Lay(), StringComparison.Ordinal);
        private bool DuLieuChuaDoi(Type t, long lanTai)
            => Environment.TickCount64 - lanTai < TuoiDuLieuKhongDoiToiDaPhut * 60_000L
               && _chuKyLanTai.TryGetValue(t, out string? ck)
               && string.Equals(ck, Module_ChuKyCSDL.Lay(), StringComparison.Ordinal);
        /// <summary>Chờ trang đầu (Form34) nạp xong lần đầu (dùng sau khi Form52 được Show trong kho giao diện).</summary>
        internal Task ChoTrangDauNapXongAsync()
            => _boNho.TryGetValue(typeof(Form34_ThongKeKhenThuong), out Form? f) && f is Form34_ThongKeKhenThuong f34 && !f34.IsDisposed
                ? f34.NapTruocDuLieuAsync()
                : Task.CompletedTask;
        /// <summary>Chờ lượt tải đang chạy của tab đang mở, hoặc tải nếu chưa có / đã cũ. Không ép tải lại.</summary>
        public Task DamBaoDaTaiAsync()
        {
            if (_isClosing || IsDisposed || _activeSubForm == null || _activeSubForm.IsDisposed) return Task.CompletedTask;
            if (CoChiTiet(_activeSubForm.GetType())) return Task.CompletedTask;
            return TaiDuLieuAsync(_activeSubForm, ep: false);
        }
        ///// <summary>
        ///// Gọi từ Form2 khi Form52 còn ẩn (sau đăng nhập, lúc người dùng rảnh): dựng trang đầu (Form34) + nạp sẵn DỮ LIỆU,
        ///// để lần bấm "Khen thưởng" đầu tiên hiện ngay bảng đã có dữ liệu. Gọi trên luồng giao diện.
        ///// </summary>
        private Task GoiReloadAsync(Form f)
        {
            if (f is Form34_ThongKeKhenThuong f34) return f34.ReloadDuLieu();
            MethodInfo? m = LayHamReload(f.GetType());
            if (m == null) return Task.CompletedTask;
            return m.Invoke(f, null) as Task ?? Task.CompletedTask;
        }
        /// <summary>Tìm hàm reload công khai không tham số của form con (cache theo kiểu).</summary>
        private MethodInfo? LayHamReload(Type t)
        {
            if (_hamReload.TryGetValue(t, out MethodInfo? da)) return da;
            MethodInfo? tim = null;
            foreach (string ten in TenHamReload)
            {
                MethodInfo? m = t.GetMethod(ten, BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
                if (m != null && (m.ReturnType == typeof(void) || typeof(Task).IsAssignableFrom(m.ReturnType)))
                {
                    tim = m;
                    break;
                }
            }
            _hamReload[t] = tim;
            return tim;
        }
        // Form có hàm reload -> giữ lâu dài trong RAM + làm mới ngầm. Không có -> dùng 1 lần như cũ (tránh dữ liệu cũ).
        private bool LaDungLau(Type t) => LayHamReload(t) != null;
        // ================= PRELOAD AN TOÀN: dựng sẵn form con vào RAM (luồng UI) =================
        // Form dựng sẵn CHƯA Show nên Load chưa chạy; lúc mở thật chỉ bỏ phần tốn kém là dựng control.
        /// <summary>
        /// Dựng sẵn 1 trang con dự phòng. Tạo handle CHIA LÁT (Module_NhuongUI.TaoHandleTheoLatAsync) thay vì tạo cả cây một mạch:
        /// bản cũ (TaoHandleDeQuy) chặn luồng giao diện 200–400 ms mỗi trang (đo: hieunang 22/11 09:23).
        /// Trang được lấy ra dùng / hiện lên giữa chừng thì dừng chia lát (Show tự tạo nốt phần còn lại).
        /// </summary>
        private async Task TaoDuPhongAsync(Type t, Func<bool> dung)
        {
            if (IsDisposed || _isClosing) return;
            if (_boNho.TryGetValue(t, out Form? dangDung) && !dangDung.IsDisposed) return;
            if (_formDuPhong.TryGetValue(t, out Form? da) && !da.IsDisposed) return;
            try
            {
                Form f;
                using (Module_DoHieuNang.Doan($"Form52 dựng sẵn: new {t.Name} (InitializeComponent)")) f = _nhaMay[t]();
                _formDuPhong[t] = f;
                if (NapTruocTaoHandle)
                    await Module_NhuongUI.TaoHandleTheoLatAsync(f, () => dung() || f.IsDisposed || f.Visible || !_formDuPhong.ContainsValue(f));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Form52.NapTruoc] " + ex.Message);
            }
        }
        /// <summary>
        /// Dựng sẵn các trang con (từng form một, cách nhau một nhịp để giao diện vẫn nhận thao tác).
        /// Gọi nhiều lần an toàn. Có thể gọi từ Module_GiaoDienForm2Web khi Form52 còn ẩn.
        /// </summary>
        internal async Task NapTruocGiaoDienAsync(Func<bool>? huy = null)
        {
            if (_dangNapDuPhong || IsDisposed) return;
            _dangNapDuPhong = true;
            try
            {
                bool Dung() => IsDisposed || _isClosing || (huy?.Invoke() ?? false);
                await Task.Delay(600);   // để trang đầu tiên mở xong trước
                // Kho giao diện chính (5 trang hay dùng) nạp xong rồi mới tới lượt các trang con này: không chen vào lúc vừa đăng nhập
                if (!await Module_NhuongUI.ChoKhoChinhXongAsync(Dung)) return;
                foreach (Type t in new[]
                {
                    typeof(Form49_QuanLyKhenThuongTapThe),
                    typeof(Form50_QuanLyKhenThuongNamCu),
                    typeof(Form51_QuanLyKhenThuongTapTheNamCu),
                    typeof(Form34_ThongKeKhenThuong)
                })
                {
                    if (Dung()) return;
                    // Mỗi form dựng là 1 khối nặng không chia nhỏ được: chờ "đèn đỏ" đăng nhập + người dùng rảnh tay
                    // (trang này có thể đang được nạp sẵn từ lúc người dùng còn gõ mật khẩu)
                    await Module_UuTienDangNhap.NhuongAsync();
                    // Xếp hàng với các bộ dựng sẵn khác (Form53, Form6 -> Form55): không dồn nhiều khối nặng vào cùng 1 nhịp rảnh
                    using var luot = await Module_NhuongUI.GiuLuotNangAsync(Dung);
                    if (luot == null) return;
                    // new + InitializeComponent của mỗi trang là 1 khối 40–240 ms không chia nhỏ được: chỉ làm khi người dùng
                    // đã rời chuột/bàn phím hẳn 0,5 s (trước: 0,18 s - vẫn hay rơi trúng lúc đang thao tác)
                    if (!await Module_NhuongUI.ChoRanhAsync(Dung, ranhMs: 500, toiDaMs: 10000)) return;
                    await TaoDuPhongAsync(t, Dung);
                    await Task.Delay(150);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Form52.NapTruocAsync] " + ex.Message);
            }
            finally
            {
                _dangNapDuPhong = false;
            }
        }
        // Đổi phiên bản phần mềm (CBCS <-> Tân binh): bỏ form dựng sẵn + form đang cache (trừ tab đang mở)
        // để lần mở sau dựng mới như ban đầu
        private void KhiPhienBanThayDoi_HuyFormDuPhong(object? sender, EventArgs e)
        {
            if (_ui != null) _ui.Post(_ => HuyFormDuPhong(), null);
            else if (IsHandleCreated && !IsDisposed) BeginInvoke(new Action(HuyFormDuPhong));
        }
        private void HuyFormDuPhong()
        {
            if (_isClosing || IsDisposed) return;
            foreach (var f in _formDuPhong.Values) HuyForm(f);
            _formDuPhong.Clear();
            foreach (var kv in _boNho.ToList())
            {
                if (kv.Value == _activeSubForm || CoChiTiet(kv.Key)) continue;
                _boNho.Remove(kv.Key);
                _lanTaiCuoi.Remove(kv.Key);
                HuyForm(kv.Value);
            }
        }
        // ================= GIAO TIẾP VỚI FORM CHA (FORM 2) =================
        private void CapNhatTieuDeFormChinh(string tieuDe)
        {
            if (_isClosing) return;
            if (Module_KhoGiaoDien.LaTrongKho(this)) return;   // đang nạp sẵn trong kho: không đổi tiêu đề trang người dùng đang xem
            var frmChinh = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            frmChinh?.CapNhatTieuDe(tieuDe);
        }
        // ===== Giảm chớp: DoubleBuffered cho container (không đụng control khác) =====
        private static void BatDoubleBufferDeQuy(Control goc)
        {
            foreach (Control c in goc.Controls)
            {
                if (c is TableLayoutPanel || c is GroupBox || c is Panel || c is SplitContainer || c is TabPage)
                {
                    typeof(Control).GetProperty("DoubleBuffered",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                        ?.SetValue(c, true);
                }
                BatDoubleBufferDeQuy(c);
            }
        }
        // ================= CÁC SỰ KIỆN CLICK MENU =================
        private async void toolStripMenuItem_KhenThuongCBCSNamHienTai_Click(object? sender, EventArgs e)
            => await MoTabAsync(typeof(Form34_ThongKeKhenThuong), $"Quản lý khen thưởng {Module_CheDoDoiTuong.TenDoiTuongHienTai} năm {_namHeThong}");
        private async void toolStripMenuItem_KhenThuongTapTheNamHienTai_Click(object? sender, EventArgs e)
            => await MoTabAsync(typeof(Form49_QuanLyKhenThuongTapThe), $"Quản lý khen thưởng tập thể năm {_namHeThong}");
        private async void toolStripMenuItem_QuanLyKhenThuongCBCSNamCu_Click(object? sender, EventArgs e)
            => await MoTabAsync(typeof(Form50_QuanLyKhenThuongNamCu), $"Quản lý khen thưởng {Module_CheDoDoiTuong.TenDoiTuongHienTai} năm cũ");
        private async void toolStripMenuItem_QuanLyKhenThuongTapTheNamCu_Click(object? sender, EventArgs e)
            => await MoTabAsync(typeof(Form51_QuanLyKhenThuongTapTheNamCu), "Quản lý khen thưởng tập thể năm cũ");
    }
}