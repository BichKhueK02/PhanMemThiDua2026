using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhanMemThiDua2026
{
    public partial class Form55_QuanLyHeThongThiDuaBaNhat : Form
    {
        
        // CẤU HÌNH CỐ ĐỊNH
        

        private readonly int _namHeThong =
            Module_HeThong.LayNamHeThong();

        
        // FORM CON - CHỈ TẠO 1 INSTANCE / FORM55
        

        private Form42_QuanLyThiDuaBaNhat? _frm42;
        private Form44_SoVangBaNhat? _frm44;

        
        // ĐIỀU KHIỂN ASYNC
        // Ngăn người dùng click menu liên tục làm chạy chồng tác vụ.
        

        private readonly SemaphoreSlim _navigationLock =
            new SemaphoreSlim(1, 1);

        private bool _dangDongForm;

        public bool DaLoadDuLieu { get; private set; }

        
        // CONSTRUCTOR
        

        public Form55_QuanLyHeThongThiDuaBaNhat()
        {
            InitializeComponent();
        }

        
        // FORM LOAD
        

        private async void Form55_QuanLyHeThongThiDuaBaNhat_Load(
            object sender,
            EventArgs e)
        {
            if (_dangDongForm)
                return;

            try
            {
                await MoForm42Async();

                DaLoadDuLieu = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "Không thể khởi tạo dữ liệu thi đua Ba Nhất.\n\n" +
                    ex.Message,
                    "Lỗi khởi tạo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        
        // TẠO FORM 42 - LAZY INITIALIZATION
        

        private void TaoForm42NeuCan()
        {
            if (_frm42 != null && !_frm42.IsDisposed)
                return;

            _frm42 = new Form42_QuanLyThiDuaBaNhat
            {
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill,
                Visible = false
            };

            panelContent.Controls.Add(_frm42);

            _frm42.BringToFront();
        }

        
        // TẠO FORM 44 - LAZY INITIALIZATION
        

        private void TaoForm44NeuCan()
        {
            if (_frm44 != null && !_frm44.IsDisposed)
                return;

            _frm44 = new Form44_SoVangBaNhat
            {
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill,
                Visible = false
            };

            panelContent.Controls.Add(_frm44);

            _frm44.BringToFront();
        }

        
        // HIỂN THỊ FORM 42
        

        public async Task MoForm42Async()
        {
            if (_dangDongForm)
                return;

            await _navigationLock.WaitAsync();

            try
            {
                TaoForm42NeuCan();

                if (_frm44 != null &&
                    !_frm44.IsDisposed &&
                    _frm44.Visible)
                {
                    _frm44.Hide();
                }

                if (_frm42 == null || _frm42.IsDisposed)
                    return;

                _frm42.Show();
                _frm42.BringToFront();

                CapNhatTieuDeFormChinh(
                    $"Quản lý phong trào thi đua Ba Nhất năm {_namHeThong}");

                await _frm42.DongBoDuLieuLoai1SangBaNhatAsync();

                if (_dangDongForm ||
                    _frm42.IsDisposed)
                    return;

                await _frm42.LoadDuLieuToanBoDanhSachBaNhatAsync();
            }
            finally
            {
                _navigationLock.Release();
            }
        }

        
        // HIỂN THỊ FORM 44
        

        public async Task MoForm44Async()
        {
            if (_dangDongForm)
                return;

            await _navigationLock.WaitAsync();

            try
            {
                TaoForm44NeuCan();

                if (_frm42 != null &&
                    !_frm42.IsDisposed &&
                    _frm42.Visible)
                {
                    _frm42.Hide();
                }

                if (_frm44 == null || _frm44.IsDisposed)
                    return;

                _frm44.Show();
                _frm44.BringToFront();

                CapNhatTieuDeFormChinh(
                    $"Sổ vàng vinh danh thi đua Ba Nhất năm {_namHeThong}");

                await _frm44.LoadDuLieuSoVangBaNhatAsync();
            }
            finally
            {
                _navigationLock.Release();
            }
        }

        
        // MENU - FORM 42
        

        private async void quanLyThiDuaBaNhat_ToolStripMenuItem_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                await MoForm42Async();
            }
            catch (Exception ex)
            {
                XuLyLoiHeThong(
                    "Không thể mở quản lý thi đua Ba Nhất.",
                    ex);
            }
        }

        
        // MENU - FORM 44
        

        private async void quanLySoVangThiDuaBaNhat_ToolStripMenuItem_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                await MoForm44Async();
            }
            catch (Exception ex)
            {
                XuLyLoiHeThong(
                    "Không thể mở Sổ vàng thi đua Ba Nhất.",
                    ex);
            }
        }

        
        // RELOAD DỮ LIỆU
        

        public async Task ReloadDuLieu()
        {
            if (_dangDongForm)
                return;

            await _navigationLock.WaitAsync();

            try
            {
                if (_frm42 != null &&
                    !_frm42.IsDisposed &&
                    _frm42.Visible)
                {
                    await _frm42.DongBoDuLieuLoai1SangBaNhatAsync();

                    if (!_dangDongForm &&
                        !_frm42.IsDisposed)
                    {
                        await _frm42.LoadDuLieuToanBoDanhSachBaNhatAsync();
                    }

                    return;
                }

                if (_frm44 != null &&
                    !_frm44.IsDisposed &&
                    _frm44.Visible)
                {
                    await _frm44.LoadDuLieuSoVangBaNhatAsync();
                }
            }
            finally
            {
                _navigationLock.Release();
            }
        }

        
        // CẬP NHẬT TIÊU ĐỀ FORM CHA
        

        private void CapNhatTieuDeFormChinh(string tieuDe)
        {
            if (string.IsNullOrWhiteSpace(tieuDe))
                return;

            var frmChinh = Application.OpenForms
                .OfType<Form2_FormCha>()
                .FirstOrDefault();

            if (frmChinh == null ||
                frmChinh.IsDisposed)
            {
                return;
            }

            frmChinh.CapNhatTieuDe(tieuDe);
        }

        
        // XỬ LÝ LỖI TẬP TRUNG
        

        private void XuLyLoiHeThong(
            string thongBao,
            Exception ex)
        {
            if (_dangDongForm)
                return;

            MessageBox.Show(
                this,
                thongBao + "\n\n" + ex.Message,
                "Lỗi hệ thống",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        
        // ĐÓNG FORM
        

        protected override void OnFormClosing(
            FormClosingEventArgs e)
        {
            _dangDongForm = true;

            try
            {
                // Hủy các tác vụ đang chờ vào lock.
                // Không Dispose Semaphore khi vẫn còn async task
                // có khả năng đang sử dụng nó.
                if (_frm42 != null)
                {
                    if (!_frm42.IsDisposed)
                        _frm42.Dispose();

                    _frm42 = null;
                }

                if (_frm44 != null)
                {
                    if (!_frm44.IsDisposed)
                        _frm44.Dispose();

                    _frm44 = null;
                }

                _navigationLock.Dispose();
            }
            catch
            {
                // Không để lỗi dọn dẹp chặn quá trình đóng Form.
            }

            base.OnFormClosing(e);
        }
    }
}