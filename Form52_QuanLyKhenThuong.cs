///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
///
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form52_QuanLyKhenThuong : Form // hoặc KryptonForm
    {
        // 1. CHUẨN KỸ SƯ: Cache năm hệ thống để giảm thiểu I/O Database nhiều lần
        private readonly int _namHeThong = Module_HeThong.LayNamHeThong();
        private Form _activeSubForm = null;
        // 2. CHUẨN KỸ SƯ: Private set để bảo vệ tính toàn vẹn của trạng thái Form
        public bool DaLoadDuLieu { get; private set; } = false;
        public Form52_QuanLyKhenThuong()
        {
            InitializeComponent();
            BatDoubleBufferDeQuy(this);   // chỉ tinh chỉnh hiển thị: giảm chớp khi chuyển trang con
            _ui = System.Threading.SynchronizationContext.Current;
            Module_TaiKhoan.OnPhienBanThayDoi += KhiPhienBanThayDoi_HuyFormDuPhong;
            Disposed += (s, e) =>
            {
                Module_TaiKhoan.OnPhienBanThayDoi -= KhiPhienBanThayDoi_HuyFormDuPhong;
                HuyFormDuPhong();
            };
        }
        // KHỞI TẠO VÀ TẢI DỮ LIỆU
        private void Form52_QuanLyKhenThuong_Load(object? sender, EventArgs e)
        {
            // Cập nhật tên menu động bằng biến cache
            if (quanLyKhenThuongNamHienTai_ToolStripMenuItem != null)
            {
                quanLyKhenThuongNamHienTai_ToolStripMenuItem.Text = $"Quản lý khen thưởng năm {_namHeThong}";
            }
            // Gọi mở form mặc định
            string tieuDeMacDinh = $"Quản lý khen thưởng CBCS năm {_namHeThong}";
            OpenSubForm<Form34_ThongKeKhenThuong>(tieuDeMacDinh);
            DaLoadDuLieu = true;
            // Dựng sẵn các trang con còn lại vào RAM lúc rảnh (không chặn, không đổi luồng)
            _ = NapTruocGiaoDienAsync();
        }
        // 3. CHUẨN KỸ SƯ: Tách biệt logic Reload thực sự, không gọi lại OpenSubForm
        public async Task ReloadDuLieu()
        {
            try
            {
                // Nếu Form đang mở là Form34, ta sẽ ép nó tải lại dữ liệu từ DB
                if (_activeSubForm is Form34_ThongKeKhenThuong frm34)
                {
                    await frm34.ReloadDuLieu();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải lại dữ liệu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // HÀM 1: MỞ FORM BẰNG MENU CLICK (CÓ XÓA FORM CŨ ĐỂ GIẢI PHÓNG RAM)
        private void OpenSubForm<T>(string tieuDeForm) where T : Form, new()
        {
            if (_activeSubForm != null && _activeSubForm.GetType() == typeof(T))
            {
                return;
            }
            T childForm = LayFormMoi<T>();
            if (childForm is Form34_ThongKeKhenThuong frm34)
            {
                frm34.YeuCauLongFormVaoPanel += NhanFormConTuBenNgoai;
            }
            EmbedFormToPanel(childForm, tieuDeForm);
        }
        private void EmbedFormToPanel(Form childForm, string tieuDeForm)
        {
            if (_activeSubForm != null && _activeSubForm != childForm)
            {
                _activeSubForm.Close();
                _activeSubForm.Dispose();
            }
            _activeSubForm = childForm;
            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;
            panelContent.Controls.Add(childForm);
            childForm.BringToFront();
            childForm.Show();
            CapNhatTieuDeFormChinh(tieuDeForm);
        }
        // HÀM 2: CHUYÊN NHẬN FORM CHI TIẾT TỪ BÊN TRONG (TẠM ẨN FORM CŨ)
        public void NhanFormConTuBenNgoai(Form frmDuocNemLen, string tieuDeMoi)
        {
            Form form34Cu = _activeSubForm;
            if (form34Cu != null)
            {
                form34Cu.Hide();
            }
            frmDuocNemLen.TopLevel = false;
            frmDuocNemLen.FormBorderStyle = FormBorderStyle.None;
            frmDuocNemLen.Dock = DockStyle.Fill;
            panelContent.Controls.Add(frmDuocNemLen);
            frmDuocNemLen.BringToFront();
            frmDuocNemLen.Show();
            CapNhatTieuDeFormChinh(tieuDeMoi);
            // 4. CHUẨN KỸ SƯ: Đăng ký Delegate qua một Method tường minh
            // Dùng Tag để truyền tham chiếu Form34 cũ sang Event Handler một cách an toàn
            frmDuocNemLen.Tag = form34Cu;
            frmDuocNemLen.FormClosed += FormChiTiet_FormClosed;
        }
        // 5. CHUẨN KỸ SƯ: Quản lý vòng đời chặt chẽ khi đóng Form Chi Tiết
        private async void FormChiTiet_FormClosed(object? sender, FormClosedEventArgs e)
        {
            if (sender is not Form frmChiTiet) return;
            // BƯỚC 1: Tháo gỡ khỏi Panel để cắt đứt liên kết giao diện
            panelContent.Controls.Remove(frmChiTiet);
            // BƯỚC 2: Hủy đối tượng để GC dọn dẹp RAM
            if (!frmChiTiet.IsDisposed)
            {
                frmChiTiet.Dispose();
            }
            // BƯỚC 3: Đánh thức Form34 dậy từ Tag đã lưu
            if (frmChiTiet.Tag is Form form34Cu && !form34Cu.IsDisposed)
            {
                form34Cu.Show();
                form34Cu.BringToFront();
                _activeSubForm = form34Cu;
                CapNhatTieuDeFormChinh($"Quản lý khen thưởng CBCS năm {_namHeThong}");
                if (form34Cu is Form34_ThongKeKhenThuong frm34)
                {
                    await frm34.ReloadDuLieu();
                }
            }
        }
        // HÀM GIAO TIẾP VỚI FORM CHA (FORM 2)
        private void CapNhatTieuDeFormChinh(string tieuDe)
        {
            var frmChinh = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            frmChinh?.CapNhatTieuDe(tieuDe);
        }
        // ================= PRELOAD AN TOÀN: dựng sẵn form con vào RAM (luồng UI) =================
        // Mỗi form dựng sẵn là một instance MỚI, CHƯA Show nên Load chưa chạy: OpenSubForm lấy ra dùng đúng như
        // khi tự "new T()" (xóa form cũ, Load + nạp dữ liệu khi hiện) - chỉ bỏ phần tốn kém là dựng control.
        private readonly System.Collections.Generic.Dictionary<Type, Form> _formDuPhong = new();
        private readonly System.Threading.SynchronizationContext? _ui;
        private bool _dangNapDuPhong;
        private const bool NapTruocTaoHandle = true;   // đặt false nếu form con có handler sự kiện static chạy khi chưa hiện
        private T LayFormMoi<T>() where T : Form, new()
        {
            if (_formDuPhong.Remove(typeof(T), out Form? duPhong) && duPhong != null && !duPhong.IsDisposed)
                return (T)duPhong;
            var f = new T();
            BatDoubleBufferDeQuy(f);
            return f;
        }
        private void TaoDuPhong<T>() where T : Form, new()
        {
            if (IsDisposed) return;
            Type type = typeof(T);
            if (_formDuPhong.TryGetValue(type, out Form? da) && da != null && !da.IsDisposed) return;
            try
            {
                var f = new T
                {
                    TopLevel = false,                      // đặt TRƯỚC khi tạo handle để không phải tạo lại handle
                    FormBorderStyle = FormBorderStyle.None,
                    Dock = DockStyle.Fill
                };
                BatDoubleBufferDeQuy(f);
                _formDuPhong[type] = f;
                if (NapTruocTaoHandle) TaoHandleDeQuy(f);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Form52.NapTruoc] " + ex.Message);
            }
        }
        /// <summary>
        /// Dựng sẵn các trang con (từng form một, cách nhau một nhịp để giao diện vẫn nhận thao tác).
        /// Gọi nhiều lần an toàn. Có thể gọi từ Form2Preloader khi Form52 còn ẩn.
        /// </summary>
        internal async Task NapTruocGiaoDienAsync(Func<bool>? huy = null)
        {
            if (_dangNapDuPhong || IsDisposed) return;
            _dangNapDuPhong = true;
            try
            {
                bool Dung() => IsDisposed || (huy?.Invoke() ?? false);
                await Task.Delay(600);   // để trang đầu tiên mở xong trước
                if (Dung()) return;
                TaoDuPhong<Form49_QuanLyKhenThuongTapThe>();
                await Task.Delay(150);
                if (Dung()) return;
                TaoDuPhong<Form50_QuanLyKhenThuongNamCu>();
                await Task.Delay(150);
                if (Dung()) return;
                TaoDuPhong<Form51_QuanLyKhenThuongTapTheNamCu>();
                await Task.Delay(150);
                if (Dung()) return;
                TaoDuPhong<Form34_ThongKeKhenThuong>();   // để lần quay lại trang CBCS cũng nhanh
            }
            finally
            {
                _dangNapDuPhong = false;
            }
        }
        private static void TaoHandleDeQuy(Control c)
        {
            try { _ = c.Handle; } catch { return; }
            foreach (Control con in c.Controls) TaoHandleDeQuy(con);
        }
        // Đổi phiên bản phần mềm (CBCS <-> Tân binh): bỏ form dựng sẵn để lần mở sau dựng mới như ban đầu
        private void KhiPhienBanThayDoi_HuyFormDuPhong(object? sender, EventArgs e)
        {
            _ui?.Post(_ => HuyFormDuPhong(), null);
        }
        private void HuyFormDuPhong()
        {
            foreach (var f in _formDuPhong.Values)
            {
                try { if (!f.IsDisposed) f.Dispose(); } catch { }
            }
            _formDuPhong.Clear();
        }
        // ===== Giảm chớp: DoubleBuffered cho container (không đụng control khác) =====
        private static void BatDoubleBufferDeQuy(Control goc)
        {
            foreach (Control c in goc.Controls)
            {
                if (c is TableLayoutPanel || c is GroupBox || c is Panel || c is SplitContainer || c is TabPage)
                {
                    typeof(Control).GetProperty("DoubleBuffered",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                        ?.SetValue(c, true);
                }
                BatDoubleBufferDeQuy(c);
            }
        }
        // CÁC SỰ KIỆN CLICK MENU
        private void toolStripMenuItem_KhenThuongCBCSNamHienTai_Click(object? sender, EventArgs e) => OpenSubForm<Form34_ThongKeKhenThuong>($"Quản lý khen thưởng CBCS năm {_namHeThong}");
        private void toolStripMenuItem_KhenThuongTapTheNamHienTai_Click(object? sender, EventArgs e) => OpenSubForm<Form49_QuanLyKhenThuongTapThe>($"Quản lý khen thưởng tập thể năm {_namHeThong}");
        private void toolStripMenuItem_QuanLyKhenThuongCBCSNamCu_Click(object? sender, EventArgs e) => OpenSubForm<Form50_QuanLyKhenThuongNamCu>("Quản lý khen thưởng CBCS năm cũ");
        private void toolStripMenuItem_QuanLyKhenThuongTapTheNamCu_Click(object? sender, EventArgs e) => OpenSubForm<Form51_QuanLyKhenThuongTapTheNamCu>("Quản lý khen thưởng tập thể năm cũ");
    }
}