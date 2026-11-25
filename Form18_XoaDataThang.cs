///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using Microsoft.Data.Sqlite;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form18_XoaDataThang : Form
    {
        private readonly Form15_ThongKeThiDua _parentForm15;
        private readonly string _csdl4Path = Module_DanduongGPS.DuongDanCSDL4;    
        // CBCS
        private static readonly string[] ThangDisplay ={
            "Tháng 12 (Năm cũ)", "Tháng 1", "Tháng 2", "Tháng 3",
            "Tháng 4", "Tháng 5", "6 Tháng đầu năm", "Tháng 6",
            "Tháng 7", "Tháng 8", "Tháng 9", "Tháng 10",
            "Tháng 11", "Tổng kết năm"
        };
        private static readonly string[] ThangNames = {
            "Thang_12_Nam_Cu", "Thang_1", "Thang_2", "Thang_3",
            "Thang_4", "Thang_5", "Sau_Thang_Dau_Nam", "Thang_6",
            "Thang_7", "Thang_8", "Thang_9", "Thang_10",
            "Thang_11", "TongKet_Nam"
        };
        // TÂN BINH
        // Tân binh / Học viên: mốc theo lịch xét (Module_LichXet), cùng thứ tự hiển thị <-> tên cột
        private static string[] ThangDisplayTanBinh => Module_LichXet.DanhSachChon(Module_CheDoDoiTuong.Hien).Keys.ToArray();
        private static string[] ThangNamesTanBinh => Module_LichXet.DanhSachChon(Module_CheDoDoiTuong.Hien).Values.ToArray();
        public Form18_XoaDataThang(Form15_ThongKeThiDua frm15)
        {
            InitializeComponent();
            Module_BieuTuongNut.ApDung(this);   // biểu tượng nút: Hiện đại = tự vẽ, Cổ điển = ảnh gốc nhúng trong exe
            _parentForm15 = frm15;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
        }
        private void Form18_XoaDataThang_Load(object? sender, EventArgs e)
        {
            try
            {
                comboBox1_ChonThangCanXoaDuLieu.Items.Clear();
                bool laTanBinh = Module_TaiKhoan
                    .LayPhienBanPhanMem()
                    .LaCheDoTanBinh();
                comboBox1_ChonThangCanXoaDuLieu.Items.AddRange(
                    laTanBinh
                        ? ThangDisplayTanBinh
                        : ThangDisplay
                );
                if (comboBox1_ChonThangCanXoaDuLieu.Items.Count > 0)
                {
                    comboBox1_ChonThangCanXoaDuLieu.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể tải danh sách dữ liệu.\n" + ex.Message,
                    Module_HeThong.TieuDe_Loi,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void kryptonButton_XoaDuLieuThangThongKe_Click(object? sender, EventArgs e)
        {
            kryptonButton_XoaDuLieuThangThongKe.Enabled = false;
            try
            {
                int index = comboBox1_ChonThangCanXoaDuLieu.SelectedIndex;
                if (index < 0)
                {
                    MessageBox.Show(
                        "Vui lòng chọn dữ liệu cần xóa.",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
                if (!File.Exists(_csdl4Path))
                {
                    MessageBox.Show(
                        "Không tìm thấy cơ sở dữ liệu.",
                        Module_HeThong.TieuDe_Loi,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
                string tenHienThi =
                    comboBox1_ChonThangCanXoaDuLieu.Text.Trim();
                DialogResult rs = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa dữ liệu của \"{tenHienThi}\" ?",
                    "Xác nhận",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (rs != DialogResult.Yes)
                    return;
                bool laTanBinh = Module_TaiKhoan
                    .LayPhienBanPhanMem()
                    .LaCheDoTanBinh();
                using var cn = new SqliteConnection(
                    Module_KetNoiCSDL.ChuoiKetNoi(_csdl4Path));
                cn.Open();
                using var tran = cn.BeginTransaction();
                try
                {
                    using var cmd = cn.CreateCommand();
                    cmd.Transaction = tran;
                    if (!laTanBinh)
                    {
                        if (index >= ThangNames.Length)
                            throw new InvalidOperationException(
                                "Dữ liệu tháng không hợp lệ.");
                        string cotThang = ThangNames[index];
                        cmd.CommandText =
                            $"UPDATE [{Module_CheDoDoiTuong.ThiDuaThang(CheDoDoiTuong.CBCS)}] SET [{cotThang}] = NULL";
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        if (index >= ThangNamesTanBinh.Length)
                            throw new InvalidOperationException(
                                "Dữ liệu tuần/tháng không hợp lệ.");
                        string cotCanXoa =
                            ThangNamesTanBinh[index];
                        cmd.CommandText =
                            $"UPDATE [{Module_CheDoDoiTuong.BangThiDuaThang}] " +
                            $"SET [{cotCanXoa}] = NULL";
                        cmd.ExecuteNonQuery();
                        using var cmdReset = cn.CreateCommand();
                        cmdReset.Transaction = tran;
                        cmdReset.CommandText =
                        $@"UPDATE [{Module_CheDoDoiTuong.BangThiDuaThang}]
                          SET TS_Loai1 = NULL,
                              TS_Loai2 = NULL,
                              TS_Loai3 = NULL,
                              TS_Loai4 = NULL";
                        cmdReset.ExecuteNonQuery();
                    }
                    tran.Commit();
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
                // ================= NHẬT KÝ =================
                Module_NhatKy.GhiNhatKy(
                    Module_TaiKhoan.TenTaiKhoan_RAM,
                    "Xóa dữ liệu thi đua",
                    $"Mục: {tenHienThi}");
                // ================= LOAD LẠI FORM =================
                if (_parentForm15 != null &&
                    !_parentForm15.IsDisposed)
                {
                    if (laTanBinh)
                        _parentForm15.LoadThongKe_TanBinh();
                    else
                        _parentForm15.CapNhatForm15();
                }
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Lỗi khi xóa dữ liệu:\n" + ex.Message,
                    Module_HeThong.TieuDe_Loi,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                if (!IsDisposed &&
                    kryptonButton_XoaDuLieuThangThongKe != null)
                {
                    kryptonButton_XoaDuLieuThangThongKe.Enabled = true;
                }
            }
        }
    }
}