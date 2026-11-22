namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form53_QuanLyKetQuaThiDua : Form
    {
        // ================= CẤU HÌNH =================
        /// <summary>
        /// Chuyển tab mà dữ liệu form đó vừa tải trong vòng N giây thì KHÔNG tải lại.
        /// Đặt 0 = luôn tải lại (nhưng tải ngầm, không chặn giao diện).
        /// </summary>
        private const int TuoiDuLieuToiDaGiay = 10;
        /// <summary>Tạo sẵn các form con còn lại vào RAM sau khi trang đầu đã hiện xong.</summary>
        private const bool NapTruocNen = true;
        private readonly int _namHeThong = Module_HeThong.LayNamHeThong();
        // 🟢 [RAM CACHE]: giữ Instance các Form con đã khởi tạo
        private readonly Dictionary<Type, Form> _subFormCache = new();
        private readonly Dictionary<Type, long> _lanTaiCuoi = new();   // Environment.TickCount64 lần tải xong gần nhất
        private readonly Dictionary<Type, Task> _dangTai = new();      // mỗi form chỉ được tải 1 lần tại 1 thời điểm
        private readonly HashSet<Type> _canTaiLai = new();             // có yêu cầu tải lại khi đang tải dở
        private Form? _activeSubForm;
        private bool _isClosing;
        private bool _daNapTruocNen;
        public bool DaLoadDuLieu { get; private set; }
        public Form53_QuanLyKetQuaThiDua()
        {
            InitializeComponent();
            // Chống giật nháy cho Form cha & Panel chứa
            DoubleBuffered = true;
            typeof(Panel).GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(panelContent, true, null);
        }
        private async void Form53_QuanLyKetQuaThiDua_Load(object? sender, EventArgs e)
        {
            // 1. Lắng nghe sự kiện từ Module_TaiKhoan
            Module_TaiKhoan.OnPhienBanThayDoi += OnPhienBanThayDoiHandler;
            // 2. Cập nhật giao diện lần đầu
            CapNhatGiaoDienTheoPhienBan();
            // 3. Tiêu đề năm hệ thống
            quanLyThiDuaNamHienTai_ToolStripMenuItem.Text = $"Quản lý thi đua năm {_namHeThong}";
            if (_activeSubForm == null)
            {
                string tieuDeMacDinh = $"Thống kê kết quả phân loại thi đua \"VÌ ANTQ\" năm {_namHeThong}";
                await MoFormAsync<Form15_ThongKeThiDua>(tieuDeMacDinh);
            }
            // 4. Trang đầu đã xong -> mới nạp dần các trang còn lại (không tranh UI với trang đầu)
            if (NapTruocNen) _ = NapTruocCacFormConAsync();
        }
        // ================= PRELOAD AN TOÀN (gọi từ Form2 khi Form53 còn ẩn, trên luồng UI) =================
        ///// <summary>
        ///// Tạo sẵn trang thống kê (Form15) vào RAM cache + tạo handle cho các control con.
        ///// KHÔNG Show, KHÔNG đọc dữ liệu: Form15_Load và ReloadData vẫn chạy ở lần mở thật.
        ///// </summary>
        /// <summary>
        /// Nạp dần các form con còn lại, mỗi lần 1 form và nghỉ giữa các lần
        /// để giao diện không bị đứng. Lần bấm menu đầu tiên sẽ không còn phải chờ dựng form.
        /// </summary>
        private async Task NapTruocCacFormConAsync()
        {
            if (_daNapTruocNen) return;
            _daNapTruocNen = true;
            try
            {
                await Task.Delay(300); // để trang đầu hiện hẳn và người dùng có thể thao tác
                bool Dung() => _isClosing || IsDisposed;
                // Kho giao diện chính (5 trang hay dùng) nạp xong rồi mới tới lượt các trang con này: không chen vào lúc vừa đăng nhập
                if (!await Module_NhuongUI.ChoKhoChinhXongAsync(Dung)) return;
                var cacBuoc = new List<Func<Task>>();
                if (Module_HoTroLuuDataTheoNamCu.LaPhienBanCBCS())
                {
                    cacBuoc.Add(() => TaoSanAsync<Form23_ThongKeThiDuaTapThe>(Dung));
                    cacBuoc.Add(() => TaoSanAsync<Form59_QuanLyThiDuaTapTheNamCu>(Dung));
                }
                cacBuoc.Add(() => TaoSanAsync<Form46_ThongKeThiDuaNamCu>(Dung));
                foreach (var buoc in cacBuoc)
                {
                    if (Dung()) return;
                    // Chờ "đèn đỏ" đăng nhập + người dùng rảnh tay (trang có thể đang được nạp sẵn lúc còn ở màn hình đăng nhập)
                    await Module_UuTienDangNhap.NhuongAsync();
                    // Khối new + InitializeComponent (tới ~240 ms) không chia nhỏ được: chỉ làm khi người dùng rời tay hẳn 0,5 s
                    if (!await Module_NhuongUI.ChoRanhAsync(Dung, ranhMs: 500, toiDaMs: 10000)) return;
                    try { await buoc(); }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("[Form53.NapTruocNen] " + ex.Message);
                    }
                    await Task.Delay(60);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Form53.NapTruocNen] " + ex.Message);
            }
        }
        ///// <summary>
        ///// Gọi từ Form2 khi Form53 còn ẩn (sau đăng nhập, lúc người dùng rảnh): dựng Form15 + nạp sẵn DỮ LIỆU.
        ///// Lần bấm "Thống kê" đầu tiên hiện ngay bảng đã có dữ liệu, không phải chờ nạp. Gọi trên luồng giao diện.
        ///// </summary>
        /// <summary>
        /// Dựng sẵn 1 trang con (chưa Show). Tạo handle CHIA LÁT thay vì cả cây một mạch (bản cũ chặn UI hàng trăm ms mỗi trang).
        /// Trang được mở giữa chừng (Visible) thì dừng chia lát: Show tự tạo nốt phần còn lại.
        /// </summary>
        private async Task TaoSanAsync<T>(Func<bool> dung) where T : Form, new()
        {
            Form f;
            using (Module_DoHieuNang.Doan($"Form53 dựng sẵn: {typeof(T).Name} (new + InitializeComponent)")) f = LayHoacTaoForm<T>();
            if (!f.IsHandleCreated)
                await Module_NhuongUI.TaoHandleTheoLatAsync(f, () => dung() || f.IsDisposed || f.Visible);
        }
        /// <summary>Lấy form từ RAM cache, chưa có thì tạo mới (chưa Show).</summary>
        private Form LayHoacTaoForm<T>() where T : Form, new()
        {
            Type typeT = typeof(T);
            if (_subFormCache.TryGetValue(typeT, out Form? da) && da != null && !da.IsDisposed)
                return da;
            var f = new T
            {
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill
            };
            panelContent.Controls.Add(f);   // chưa Show -> vẫn ẩn
            _subFormCache[typeT] = f;
            return f;
        }
        // ================= PHIÊN BẢN =================
        private void OnPhienBanThayDoiHandler(object? sender, EventArgs e)
        {
            if (_isClosing || IsDisposed || !IsHandleCreated) return;
            if (InvokeRequired)
                BeginInvoke(new Action(CapNhatGiaoDienTheoPhienBan));
            else
                CapNhatGiaoDienTheoPhienBan();
        }
        /// <summary>Ẩn/hiện menu tập thể theo phiên bản hiện tại.</summary>
        private void CapNhatGiaoDienTheoPhienBan()
        {
            bool laCBCS = Module_HoTroLuuDataTheoNamCu.LaPhienBanCBCS();
            quanLyThiDuaTapThe_ToolStripMenuItem.Available = laCBCS;
            quanLyThiDuaTapTheNamCu_ToolStripMenuItem.Available = laCBCS;
        }
        // ================= ĐÓNG FORM & DỌN RAM =================
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            // Có form/handler khác huỷ việc đóng -> KHÔNG được dọn dẹp, giữ nguyên trạng thái
            if (e.Cancel) return;
            _isClosing = true;
            Module_TaiKhoan.OnPhienBanThayDoi -= OnPhienBanThayDoiHandler;
            _activeSubForm = null;
            foreach (var subForm in _subFormCache.Values.ToList())
            {
                try
                {
                    if (subForm.IsDisposed) continue;
                    if (!panelContent.IsDisposed && panelContent.Controls.Contains(subForm))
                        panelContent.Controls.Remove(subForm);
                    subForm.Close();
                    subForm.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("[Form53.Dispose con] " + ex.Message);
                }
            }
            _subFormCache.Clear();
            _lanTaiCuoi.Clear();
            _canTaiLai.Clear();
        }
        // ================= CHUYỂN FORM CON =================
        /// <summary>
        /// Chuyển form: HIỆN NGAY từ RAM (đồng bộ, không await), sau đó mới tải dữ liệu ngầm.
        /// Bấm lại đúng tab đang mở = làm mới dữ liệu tab đó.
        /// </summary>
        private async Task MoFormAsync<T>(string tieuDeForm) where T : Form, new()
        {
            if (_isClosing || IsDisposed) return;
            Form target;
            bool dangMoSan = _activeSubForm != null && !_activeSubForm.IsDisposed
                             && _activeSubForm.GetType() == typeof(T);
            try
            {
                target = dangMoSan ? _activeSubForm! : ChuyenSang<T>(tieuDeForm);
            }
            catch (Exception ex)
            {
                DaLoadDuLieu = false;
                System.Diagnostics.Debug.WriteLine($"[Lỗi mở Form con]: {ex.Message}");
                MessageBox.Show($"Lỗi khi mở giao diện:\n{ex.Message}", "Lỗi hệ thống",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // Form đã hiện trên màn hình, giờ mới tải dữ liệu (không giữ khoá, không chặn chuyển tab khác)
            await TaiDuLieuAsync(target, ep: dangMoSan);
        }
        /// <summary>Phần đồng bộ: gỡ form cũ, hiện form mới, vẽ ngay.</summary>
        private Form ChuyenSang<T>(string tieuDeForm) where T : Form, new()
        {
            Form target = LayHoacTaoForm<T>();
            Form? formCu = (_activeSubForm != null && !_activeSubForm.IsDisposed) ? _activeSubForm : null;
            panelContent.SuspendLayout();
            try
            {
                _activeSubForm = target;
                target.BringToFront();
                Module_KhoGiaoDien.ShowTrongKho(target);   // = Show(); nằm trong kho ẩn thì vẫn chạy Load
                // Ẩn form cũ SAU khi form mới đã hiện (tránh 1 khung hình nền trống, không Dispose)
                if (formCu != null && formCu != target) formCu.Hide();
            }
            finally
            {
                panelContent.ResumeLayout(true);   // chỉ layout 1 lần
            }
            CapNhatTieuDeFormChinh(tieuDeForm);
            target.Update();   // vẽ ngay trước khi bắt đầu tải dữ liệu
            return target;
        }
        // ================= TẢI DỮ LIỆU =================
        private Task TaiDuLieuAsync(Form form, bool ep)
        {
            Type t = form.GetType();
            // Đang tải dở: không chạy song song 2 lần trên cùng 1 form (tránh hỏng lưới/dữ liệu)
            if (_dangTai.TryGetValue(t, out Task? dangChay))
            {
                if (ep) _canTaiLai.Add(t);   // tải xong sẽ tải thêm 1 vòng nữa
                return dangChay;
            }
            // Dữ liệu còn tươi (vừa tải, hoặc CSDL không đổi kể từ lần tải trước) -> khỏi tải lại
            if (!ep && _lanTaiCuoi.TryGetValue(t, out long truoc)
                    && (Environment.TickCount64 - truoc < TuoiDuLieuToiDaGiay * 1000L || DuLieuChuaDoi(t, truoc)))
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
                    Form15_ThongKeThiDua.NK($"Form53: tải {t.Name} BẮT ĐẦU");
                    await GoiReloadAsync(form);
                    Form15_ThongKeThiDua.NK($"Form53: tải {t.Name} XONG");
                    _lanTaiCuoi[t] = Environment.TickCount64;
                    _chuKyLanTai[t] = Module_ChuKyCSDL.Lay();   // chụp SAU khi tải (bản thân lần tải có thể đồng bộ/ghi CSDL)
                }
                while (_canTaiLai.Contains(t) && !_isClosing && !form.IsDisposed);
            }
            catch (Exception ex)
            {
                loi = true;
                Form15_ThongKeThiDua.NK($"Form53: tải {t.Name} LỖI: {ex}");
                _lanTaiCuoi.Remove(t);
                System.Diagnostics.Debug.WriteLine($"[Lỗi tải dữ liệu {t.Name}]: {ex.Message}");
                if (!_isClosing && !IsDisposed)
                {
                    MessageBox.Show($"Lỗi tải lại dữ liệu:\n{ex.Message}", "Lỗi hệ thống",
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
        /// <summary>Tối đa bao lâu thì vẫn làm mới dù CSDL không đổi (an toàn).</summary>
        private const int TuoiDuLieuKhongDoiToiDaPhut = 10;
        private bool DuLieuChuaDoi(Type t, long lanTai)
            => Environment.TickCount64 - lanTai < TuoiDuLieuKhongDoiToiDaPhut * 60_000L
               && _chuKyLanTai.TryGetValue(t, out string? ck)
               && string.Equals(ck, Module_ChuKyCSDL.Lay(), StringComparison.Ordinal);
        /// <summary>Đang có lượt tải nào của tab đang mở thì chờ lượt đó; chưa tải / dữ liệu cũ thì tải. Không bao giờ tải chồng.</summary>
        public Task DamBaoDaTaiAsync()
        {
            if (_isClosing || IsDisposed || _activeSubForm == null || _activeSubForm.IsDisposed) return Task.CompletedTask;
            return TaiDuLieuAsync(_activeSubForm, ep: false);
        }
        /// <summary>Mỗi form con có hàm reload riêng - gom về 1 chỗ.</summary>
        private static Task GoiReloadAsync(Form f) => f switch
        {
            Form15_ThongKeThiDua f15 => f15.ReloadData(),
            Form59_QuanLyThiDuaTapTheNamCu f59 => f59.ReloadDataAsync(),
            // Form23_ThongKeThiDuaTapThe, Form46_ThongKeThiDuaNamCu: giữ nguyên như cũ (không reload ở đây)
            _ => Task.CompletedTask
        };
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
        private void CapNhatTieuDeFormChinh(string tieuDe)
        {
            if (_isClosing) return;
            if (Module_KhoGiaoDien.LaTrongKho(this)) return;   // đang nạp sẵn trong kho: không đổi tiêu đề trang người dùng đang xem
            var frmChinh = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            if (frmChinh != null && !frmChinh.IsDisposed)
            {
                frmChinh.CapNhatTieuDe(tieuDe);
            }
        }
        // ================= MENU =================
        private async void quanLyThiDuaCBCS_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (_isClosing) return;
            string tieuDe = $"Trang thống kê kết quả phân loại thi đua CBCS phong trào \"VÌ ANTQ\" năm {_namHeThong}";
            await MoFormAsync<Form15_ThongKeThiDua>(tieuDe);
        }
        private async void quanLyThiDuaTapThe_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (_isClosing) return;
            string tieuDe = $"Trang thống kê kết quả phân loại thi đua tập thể phong trào \"VÌ ANTQ\" năm {_namHeThong}";
            await MoFormAsync<Form23_ThongKeThiDuaTapThe>(tieuDe);
        }
        private async void quanLyThiDuaCBCSNamCu_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (_isClosing) return;
            const string tieuDe = "Trang thống kê kết quả phân loại thi đua CBCS năm cũ";
            await MoFormAsync<Form46_ThongKeThiDuaNamCu>(tieuDe);
        }
        private async void quanLyThiDuaTapTheNamCu_ToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (_isClosing) return;
            const string tieuDe = "Trang thống kê kết quả phân loại thi đua tập thể năm cũ";
            await MoFormAsync<Form59_QuanLyThiDuaTapTheNamCu>(tieuDe);
        }
    }
}