using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhanMemThiDua2026
{
    public partial class Form53_QuanLyKetQuaThiDua : Form
    {
        private readonly int _namHeThong = Module_HeThong.LayNamHeThong();
        private readonly SemaphoreSlim _switchLock = new(1, 1);
        // 🟢 [RAM CACHE]: Lưu giữ Instance của các Form con đã khởi tạo vào RAM
        private readonly Dictionary<Type, Form> _subFormCache = new();
        private Form? _activeSubForm;
        private bool _isClosing;
        public bool DaLoadDuLieu { get; private set; }
        public Form53_QuanLyKetQuaThiDua()
        {
            InitializeComponent();

            // Tối ưu render chống giật nháy cho Form Cha & Panel chứa
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            panelContent.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(panelContent, true, null);
        }
        private async void Form53_QuanLyKetQuaThiDua_Load(object sender, EventArgs e)
        {
            if (quanLyThiDuaNamHienTai_ToolStripMenuItem != null)
            {
                quanLyThiDuaNamHienTai_ToolStripMenuItem.Text = $"Quản lý thi đua năm {_namHeThong}";
            }

            if (_activeSubForm == null)
            {
                string tieuDeMacDinh = $"Thống kê kết quả phân loại thi đua \"VÌ ANTQ\" năm {_namHeThong}";
                await OpenSubFormAsync<Form15_ThongKeThiDua>(tieuDeMacDinh);
            }
        }
        // 🟢 HÀM MỞ FORM SIÊU TỐC TỪ RAM (O(1) Access Time)
        private async Task OpenSubFormAsync<T>(string tieuDeForm) where T : Form, new()
        {
            if (_isClosing || IsDisposed) return;

            await _switchLock.WaitAsync();
            try
            {
                Type typeT = typeof(T);

                // 1. Nếu đang mở chính Form đó thì bỏ qua
                if (_activeSubForm != null && _activeSubForm.GetType() == typeT && !_activeSubForm.IsDisposed)
                {
                    return;
                }

                DaLoadDuLieu = false;

                // Tạm dừng vẽ giao diện Panel để ép Render 1 lần duy nhất
                panelContent.SuspendLayout();

                // 2. Ẩn Form cũ (Không Dispose)
                if (_activeSubForm != null && !_activeSubForm.IsDisposed)
                {
                    _activeSubForm.Hide();
                }

                // 3. Lấy Form từ RAM Cache hoặc Khởi tạo mới nếu lần đầu bấm vào
                Form targetForm;
                if (_subFormCache.TryGetValue(typeT, out Form? cachedForm) && cachedForm != null && !cachedForm.IsDisposed)
                {
                    targetForm = cachedForm;
                }
                else
                {
                    // Lần đầu tiên truy cập -> Tạo mới và nạp vào RAM Cache
                    targetForm = new T
                    {
                        TopLevel = false,
                        FormBorderStyle = FormBorderStyle.None,
                        Dock = DockStyle.Fill
                    };

                    panelContent.Controls.Add(targetForm);
                    _subFormCache[typeT] = targetForm;
                }

                _activeSubForm = targetForm;

                // 4. Bật hiển thị ngay lập tức từ RAM
                targetForm.BringToFront();
                targetForm.Show();

                // Mở lại Render Panel ngay khi Form hiện lên
                panelContent.ResumeLayout(true);

                // 5. Cập nhật tiêu đề Form chính
                CapNhatTieuDeFormChinh(tieuDeForm);

                // 6. Reload dữ liệu ngầm nếu cần
                if (targetForm is Form15_ThongKeThiDua frm15)
                {
                    await frm15.ReloadData();
                }
                else if (targetForm is Form46_ThongKeThiDuaNamCu frm46)
                {
                    // await frm46.ReloadData();
                }

                DaLoadDuLieu = true;
            }
            catch (Exception ex)
            {
                DaLoadDuLieu = false;
                System.Diagnostics.Debug.WriteLine($"[Lỗi mở Form con]: {ex.Message}");
                MessageBox.Show($"Lỗi khi mở giao diện:\n{ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Ép Panel mở lại tính năng vẽ/sắp xếp giao diện và cập nhật ngay lập tức
                panelContent.ResumeLayout(true);
                // Giải phóng khóa chuyển Form
                _switchLock.Release();
            }
        }
        // HÀM CLICK MENU TỐI ƯU CỰC ĐOAN (Sử dụng Async/Await mượt mà)
        private async void toolStripMenuItem3_QuanLyThiDuaTapTheNamCu_Click(object sender, EventArgs e)
        {
            if (_isClosing) return;
            const string tieuDe = "Thống kê kết quả phân loại thi đua tập thể năm cũ";

            await OpenSubFormAsync<Form59_QuanLyThiDuaTapTheNamCu>(tieuDe);

            // Lấy instance từ Dictionary Cache
            if (_subFormCache.TryGetValue(typeof(Form59_QuanLyThiDuaTapTheNamCu), out Form? cachedForm)
                && cachedForm is Form59_QuanLyThiDuaTapTheNamCu frm59
                && !frm59.IsDisposed)
            {
                await frm59.ReloadDataAsync();
            }
        }
        private async void quanLyThiDuaNamHienTai_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_isClosing) return;
            string tieuDe = $"Thống kê kết quả phân loại thi đua \"VÌ ANTQ\" năm {_namHeThong}";
            await OpenSubFormAsync<Form15_ThongKeThiDua>(tieuDe);
        }
        private async void quanLyThiDuaNamCu_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_isClosing) return;
            const string tieuDe = "Thống kê kết quả phân loại thi đua CBCS năm cũ";
            await OpenSubFormAsync<Form46_ThongKeThiDuaNamCu>(tieuDe);
        }
        public async Task ReloadDuLieu()
        {
            if (_isClosing || IsDisposed) return;

            await _switchLock.WaitAsync();
            try
            {
                if (_activeSubForm is Form15_ThongKeThiDua frm15 && !frm15.IsDisposed)
                {
                    DaLoadDuLieu = false;
                    await frm15.ReloadData();
                    DaLoadDuLieu = true;
                }
            }
            catch (Exception ex)
            {
                DaLoadDuLieu = false;
                MessageBox.Show($"Lỗi tải lại dữ liệu:\n{ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _switchLock.Release();
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
        // DỌN DẸP SẠCH RAM KHI ĐÓNG FORM CHA
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _isClosing = true;

            try
            {
                _activeSubForm = null;

                // Xóa và Dispose toàn bộ các Form con đang lưu trên RAM Cache
                foreach (var kvp in _subFormCache)
                {
                    Form subForm = kvp.Value;
                    if (subForm != null && !subForm.IsDisposed)
                    {
                        if (panelContent != null && !panelContent.IsDisposed && panelContent.Controls.Contains(subForm))
                        {
                            panelContent.Controls.Remove(subForm);
                        }
                        subForm.Close();
                        subForm.Dispose();
                    }
                }
                _subFormCache.Clear();

                _switchLock.Dispose();
            }
            finally
            {
                base.OnFormClosing(e);
            }
        }
    }
}