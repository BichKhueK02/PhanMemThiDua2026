using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace PhanMemThiDua2026
{
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
            T childForm = new T();
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
        // CÁC SỰ KIỆN CLICK MENU
        private void toolStripMenuItem_KhenThuongCBCSNamHienTai_Click(object? sender, EventArgs e) =>
     OpenSubForm<Form34_ThongKeKhenThuong>(
         $"Quản lý khen thưởng CBCS năm {_namHeThong}");
        private void toolStripMenuItem_KhenThuongTapTheNamHienTai_Click(object? sender, EventArgs e) =>
            OpenSubForm<Form49_QuanLyKhenThuongTapThe>(
                $"Quản lý khen thưởng tập thể năm {_namHeThong}");
        private void toolStripMenuItem_QuanLyKhenThuongCBCSNamCu_Click(object? sender, EventArgs e) =>
            OpenSubForm<Form50_QuanLyKhenThuongNamCu>(
                "Quản lý khen thưởng CBCS năm cũ");
        private void toolStripMenuItem_QuanLyKhenThuongTapTheNamCu_Click(object? sender, EventArgs e) =>
            OpenSubForm<Form51_QuanLyKhenThuongTapTheNamCu>(
                "Quản lý khen thưởng tập thể năm cũ");
    }
}