using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhanMemThiDua2026
{
    public partial class Form53_QuanLyKetQuaThiDua : Form
    {
        // CACHE CẤU HÌNH TĨNH CỦA FORM
        private readonly int _namHeThong = Module_HeThong.LayNamHeThong();

        // Khóa chuyển Form:
        // - Chống click nhanh tạo nhiều luồng chuyển Form
        // - Bảo vệ toàn bộ Transaction: Tắt Form cũ -> Mở Form mới -> Nạp dữ liệu
        private readonly SemaphoreSlim _switchLock = new(1, 1);
        private Form? _activeSubForm;
        private bool _isClosing;

        public bool DaLoadDuLieu { get; private set; }

        public Form53_QuanLyKetQuaThiDua()
        {
            InitializeComponent();
        }

        // ========================================================================
        // SỰ KIỆN LOAD
        // ========================================================================
        private async void Form53_QuanLyKetQuaThiDua_Load(object sender, EventArgs e)
        {
            if (quanLyThiDuaNamHienTai_ToolStripMenuItem != null)
            {
                quanLyThiDuaNamHienTai_ToolStripMenuItem.Text = $"Quản lý thi đua năm {_namHeThong}";
            }

            // Mở mặc định Form 15 lần đầu tiên
            if (_activeSubForm == null)
            {
                string tieuDeMacDinh = $"Thống kê kết quả phân loại thi đua \"VÌ ANTQ\" năm {_namHeThong}";
                await OpenSubFormAsync<Form15_ThongKeThiDua>(tieuDeMacDinh);
            }
        }

        // ========================================================================
        // RELOAD DỮ LIỆU ĐỊNH TUYẾN DÀNH CHO FORM ĐANG MỞ
        // ========================================================================
        public async Task ReloadDuLieu()
        {
            if (_isClosing || IsDisposed) return;

            // Chờ nếu đang có luồng chuyển Form hoạt động
            await _switchLock.WaitAsync();
            try
            {
                if (_activeSubForm is Form15_ThongKeThiDua frm15 && !frm15.IsDisposed)
                {
                    DaLoadDuLieu = false;
                    await frm15.ReloadData();
                    DaLoadDuLieu = true;
                }
                else if (_activeSubForm is Form46_ThongKeThiDuaNamCu frm46 && !frm46.IsDisposed)
                {
                    // DaLoadDuLieu = false;
                    // await frm46.ReloadData();
                    DaLoadDuLieu = true;
                }
            }
            catch (Exception ex)
            {
                DaLoadDuLieu = false;
                System.Diagnostics.Debug.WriteLine($"[Form53_ReloadDuLieu] {ex}");
                MessageBox.Show($"Lỗi tải lại dữ liệu:\n{ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _switchLock.Release();
            }
        }

        // ========================================================================
        // HÀM MỞ FORM CON SIÊU TỐC (SINGLE ENTRY POINT KIỂM SOÁT VÒNG ĐỜI)
        // ========================================================================
        private async Task OpenSubFormAsync<T>(string tieuDeForm) where T : Form, new()
        {
            if (_isClosing || IsDisposed) return;

            // ⭐ BẢO VỆ TUYỆT ĐỐI TRANSACTION CHUYỂN FORM BẰNG LOCK
            await _switchLock.WaitAsync();
            try
            {
                // 1. Chống Load lại nếu đang mở đúng loại Form
                if (_activeSubForm != null && _activeSubForm.GetType() == typeof(T) && !_activeSubForm.IsDisposed)
                {
                    return;
                }

                // Cờ báo hiệu Form đang trong tiến trình chưa ổn định
                DaLoadDuLieu = false;

                // 2. Tháo gỡ hoàn toàn Form cũ trước khi hủy để tránh rò rỉ bộ nhớ
                if (_activeSubForm != null && !_activeSubForm.IsDisposed)
                {
                    // Gỡ event trước khi hủy để chống rò rỉ RAM (Memory Leak từ Delegate)
                    _activeSubForm.FormClosed -= ChildForm_FormClosed;

                    if (panelContent.Controls.Contains(_activeSubForm))
                    {
                        panelContent.Controls.Remove(_activeSubForm);
                    }
                    _activeSubForm.Close();
                    _activeSubForm.Dispose(); // Xóa sạch không lưu vết
                }

                // 3. Nhúng Form mới
                T childForm = new T();
                _activeSubForm = childForm;

                // ⭐ Đăng ký quản lý sự kiện nếu Form con bị đóng chủ động từ bên trong
                childForm.FormClosed += ChildForm_FormClosed;

                childForm.TopLevel = false;
                childForm.FormBorderStyle = FormBorderStyle.None;
                childForm.Dock = DockStyle.Fill;

                panelContent.Controls.Add(childForm);
                childForm.BringToFront();
                childForm.Show();

                // 4. Báo cho Form 2 cập nhật Tiêu đề
                CapNhatTieuDeFormChinh(tieuDeForm);

                // 5. Nạp Data Ngầm (An toàn 100% vì được Lock bảo vệ, không ai có thể can thiệp Dispose lúc này)
                if (childForm is Form15_ThongKeThiDua frm15)
                {
                    await frm15.ReloadData();
                }
                else if (childForm is Form46_ThongKeThiDuaNamCu frm46)
                {
                    // Đợi khi Form 46 có hàm ReloadData thì mở khóa
                    // await frm46.ReloadData();
                }

                // 6. Chốt trạng thái khi toàn bộ tiến trình nạp data không có lỗi
                DaLoadDuLieu = true;
            }
            catch (Exception ex)
            {
                DaLoadDuLieu = false;
                System.Diagnostics.Debug.WriteLine($"[Lỗi ngầm khi tải Data Form con]: {ex.Message}");
                // ⭐ Báo ra ngoài để User / SysAdmin biết đang bị kẹt ở đâu thay vì im lặng
                MessageBox.Show($"Lỗi khi mở giao diện chức năng:\n{ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // BẮT BUỘC NHẢ KHÓA
                _switchLock.Release();
            }
        }

        // ========================================================================
        // SỰ KIỆN FORM CON TỰ ĐÓNG
        // ========================================================================
        private void ChildForm_FormClosed(object? sender, FormClosedEventArgs e)
        {
            if (sender is not Form childForm) return;

            try
            {
                if (panelContent != null && !panelContent.IsDisposed && panelContent.Controls.Contains(childForm))
                {
                    panelContent.Controls.Remove(childForm);
                }

                if (ReferenceEquals(_activeSubForm, childForm))
                {
                    _activeSubForm = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Form53_ChildFormClosed] {ex.Message}");
            }
        }

        // ========================================================================
        // SỰ KIỆN CLICK MENU
        // ========================================================================
        private async void quanLyThiDuaNamHienTai_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_isClosing) return;
            string tieuDe = $"Thống kê kết quả phân loại thi đua \"VÌ ANTQ\" năm {_namHeThong}";
            await OpenSubFormAsync<Form15_ThongKeThiDua>(tieuDe);
        }

        private async void quanLyThiDuaNamCu_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_isClosing) return;
            const string tieuDe = "Thống kê kết quả phân loại thi đua năm cũ";
            await OpenSubFormAsync<Form46_ThongKeThiDuaNamCu>(tieuDe);
        }

        // ========================================================================
        // HÀM TIỆN ÍCH UI
        // ========================================================================
        private void CapNhatTieuDeFormChinh(string tieuDe)
        {
            if (_isClosing) return;

            var frmChinh = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            if (frmChinh != null && !frmChinh.IsDisposed)
            {
                frmChinh.CapNhatTieuDe(tieuDe);
            }
        }

        // ========================================================================
        // DỌN DẸP & HỦY DIỆT FORM CHA
        // ========================================================================
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _isClosing = true;

            try
            {
                Form? child = _activeSubForm;
                _activeSubForm = null;

                if (child != null && !child.IsDisposed)
                {
                    try
                    {
                        // Gỡ event handler để form rác không bắn sự kiện lung tung lúc hấp hối
                        child.FormClosed -= ChildForm_FormClosed;

                        if (panelContent != null && !panelContent.IsDisposed && panelContent.Controls.Contains(child))
                        {
                            panelContent.Controls.Remove(child);
                        }

                        child.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Form53_CloseChild] {ex.Message}");
                    }
                    finally
                    {
                        if (!child.IsDisposed)
                        {
                            child.Dispose();
                        }
                    }
                }

                // Dọn sạch chiếc ổ khóa cuối cùng trước khi đóng Form cha
                _switchLock.Dispose();
            }
            finally
            {
                base.OnFormClosing(e);
            }
        }
    }
}