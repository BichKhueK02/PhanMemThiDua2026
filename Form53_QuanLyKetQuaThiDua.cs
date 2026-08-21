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
        private readonly int _namHeThong =
            Module_HeThong.LayNamHeThong();
        // Khóa chuyển Form:
        // - chống click nhanh tạo nhiều luồng chuyển Form
        // - tránh Dispose Form đang được ReloadData()
        private readonly SemaphoreSlim _switchLock = new(1, 1);
        private Form? _activeSubForm;
        private bool _isClosing;
        public bool DaLoadDuLieu { get; private set; }
        public Form53_QuanLyKetQuaThiDua()
        {
            InitializeComponent();
        }
        // FORM LOAD
        public async Task ReloadDuLieu()
        {
            if (_isClosing || IsDisposed)
                return;

            try
            {
                // Nếu Form15 đang tồn tại thì chỉ yêu cầu nó Reload.
                if (_activeSubForm is Form15_ThongKeThiDua frm15 &&
                    !frm15.IsDisposed)
                {
                    await frm15.ReloadData();

                    DaLoadDuLieu = true;
                    return;
                }

                // Nếu Form15 chưa tồn tại thì tạo mới.
                string tieuDeMacDinh =
                    $"Thống kê kết quả phân loại thi đua " +
                    $"\"VÌ ANTQ\" năm {_namHeThong}";

                await OpenSubFormAsync<Form15_ThongKeThiDua>(
                    tieuDeMacDinh);

                DaLoadDuLieu = true;
            }
            catch (Exception ex)
            {
                DaLoadDuLieu = false;

                System.Diagnostics.Debug.WriteLine(
                    $"[Form53_ReloadDuLieu] {ex}");

                MessageBox.Show(
                    $"Lỗi tải lại dữ liệu:\n{ex.Message}",
                    "Hệ thống",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        // =================================================================
        // SỰ KIỆN LOAD & TẢI DỮ LIỆU ĐỊNH TUYẾN
        // =================================================================
        private async void Form53_QuanLyKetQuaThiDua_Load(object sender, EventArgs e)
        {
            // ⭐ TỐI ƯU 1: Xóa đoạn code gán text dư thừa, chỉ giữ lại đúng 1 logic gán
            if (quanLyThiDuaNamHienTai_ToolStripMenuItem != null)
            {
                quanLyThiDuaNamHienTai_ToolStripMenuItem.Text = $"Quản lý thi đua năm {_namHeThong}";
            }

            // ⭐ BỌC THÉP: Kiểm tra nếu chưa có Form nào được gọi thì mới mở Form 15
            if (_activeSubForm == null)
            {
                string tieuDeMacDinh = $"Thống kê kết quả phân loại thi đua \"VÌ ANTQ\" năm {_namHeThong}";
                await OpenSubFormAsync<Form15_ThongKeThiDua>(tieuDeMacDinh);
            }
            DaLoadDuLieu = true;
        }
        // =================================================================
        // HÀM MỞ FORM CON SIÊU TỐC (CHUYỂN SANG TRẢ VỀ TASK ĐỂ ĐỒNG BỘ LUỒNG)
        // =================================================================
        private async Task OpenSubFormAsync<T>(string tieuDeForm) where T : Form, new()
        {
            // 1. Chống Load lại nếu đang mở đúng loại Form
            if (_activeSubForm != null && _activeSubForm.GetType() == typeof(T))
            {
                return;
            }

            // 2. ⭐ TỐI ƯU 2 (QUAN TRỌNG): Tháo gỡ hoàn toàn khỏi giao diện trước khi hủy để tránh rò rỉ bộ nhớ
            if (_activeSubForm != null)
            {
                panelContent.Controls.Remove(_activeSubForm); // Cắt đứt tham chiếu giao diện
                _activeSubForm.Close();
                _activeSubForm.Dispose(); // Ép hệ thống dọn dẹp RAM
            }

            // 3. Nhúng Form mới
            T childForm = new T();
            _activeSubForm = childForm;

            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;

            panelContent.Controls.Add(childForm);
            childForm.BringToFront();
            childForm.Show();

            // 4. Báo cho Form 2 cập nhật Tiêu đề
            CapNhatTieuDeFormChinh(tieuDeForm);

            // 5. Nạp Data Ngầm an toàn
            try
            {
                if (childForm is Form15_ThongKeThiDua frm15)
                {
                    await frm15.ReloadData();
                }
                else if (childForm is Form46_ThongKeThiDuaNamCu frm46)
                {
                    // Đợi khi Form 46 có hàm ReloadData thì mở khóa
                    // await frm46.ReloadData();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi ngầm khi tải Data Form con]: {ex.Message}");
            }
        }
        private void ChildForm_FormClosed(
            object? sender,
            FormClosedEventArgs e)
        {
            if (sender is not Form childForm)
                return;

            try
            {
                if (panelContent != null &&
                    !panelContent.IsDisposed &&
                    panelContent.Controls.Contains(childForm))
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
                System.Diagnostics.Debug.WriteLine(
                    $"[Form53_ChildFormClosed] {ex.Message}");
            }
        }
        private void CapNhatTieuDeFormChinh(
            string tieuDe)
        {
            if (_isClosing)
                return;

            var frmChinh =
                Application.OpenForms
                    .OfType<Form2_FormCha>()
                    .FirstOrDefault();

            if (frmChinh != null &&
                !frmChinh.IsDisposed)
            {
                frmChinh.CapNhatTieuDe(tieuDe);
            }
        }
        private async void
            quanLyThiDuaNamHienTai_ToolStripMenuItem_Click(
                object sender,
                EventArgs e)
        {
            if (_isClosing)
                return;

            string tieuDe =
                $"Thống kê kết quả phân loại thi đua " +
                $"\"VÌ ANTQ\" năm {_namHeThong}";

            await OpenSubFormAsync<Form15_ThongKeThiDua>(
                tieuDe);
        }
        private async void
            quanLyThiDuaNamCu_ToolStripMenuItem_Click(
                object sender,
                EventArgs e)
        {
            if (_isClosing)
                return;

            const string tieuDe =
                "Thống kê kết quả phân loại thi đua năm cũ";

            await OpenSubFormAsync<Form46_ThongKeThiDuaNamCu>(
                tieuDe);
        }
        protected override void OnFormClosing(
            FormClosingEventArgs e)
        {
            _isClosing = true;

            try
            {
                Form? child = _activeSubForm;

                _activeSubForm = null;

                if (child != null &&
                    !child.IsDisposed)
                {
                    try
                    {
                        if (panelContent != null &&
                            !panelContent.IsDisposed &&
                            panelContent.Controls.Contains(child))
                        {
                            panelContent.Controls.Remove(child);
                        }

                        child.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[Form53_CloseChild] {ex.Message}");
                    }
                    finally
                    {
                        if (!child.IsDisposed)
                        {
                            child.Dispose();
                        }
                    }
                }
            }
            finally
            {
                base.OnFormClosing(e);
            }
        }

    }
}