
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
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
        /// <summary>
        /// Tạo sẵn trang thống kê (Form15) vào RAM cache + tạo handle cho các control con.
        /// KHÔNG Show, KHÔNG đọc dữ liệu: Form15_Load và ReloadData vẫn chạy ở lần mở thật.
        /// </summary>
        public void NapTruocGiaoDien()
        {
            if (_isClosing || IsDisposed) return;
            try
            {
                TaoSan<Form15_ThongKeThiDua>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Form53.NapTruoc] " + ex.Message);
            }
        }
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
                var cacBuoc = new List<Action>();
                if (Module_HoTroLuuDataTheoNamCu.LaPhienBanCBCS())
                {
                    cacBuoc.Add(TaoSan<Form23_ThongKeThiDuaTapThe>);
                    cacBuoc.Add(TaoSan<Form59_QuanLyThiDuaTapTheNamCu>);
                }
                cacBuoc.Add(TaoSan<Form46_ThongKeThiDuaNamCu>);
                foreach (var buoc in cacBuoc)
                {
                    if (_isClosing || IsDisposed) return;
                    try { buoc(); }
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
        private void TaoSan<T>() where T : Form, new()
        {
            Form f = LayHoacTaoForm<T>();
            if (!f.IsHandleCreated) TaoHandleDeQuy(f);
        }
        private static void TaoHandleDeQuy(Control c)
        {
            try { _ = c.Handle; } catch { return; }
            foreach (Control con in c.Controls) TaoHandleDeQuy(con);
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
                target.Show();
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
            // Dữ liệu còn tươi -> khỏi tải lại
            if (!ep && _lanTaiCuoi.TryGetValue(t, out long truoc)
                    && Environment.TickCount64 - truoc < TuoiDuLieuToiDaGiay * 1000L)
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
                }
                while (_canTaiLai.Contains(t) && !_isClosing && !form.IsDisposed);
            }
            catch (Exception ex)
            {
                loi = true;
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