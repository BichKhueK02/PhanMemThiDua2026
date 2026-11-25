using Microsoft.Data.Sqlite;
using System.Diagnostics;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// CÀI ĐẶT LỊCH XÉT THI ĐUA NHÓM TUẦN (Tân binh + Học viên, mỗi chế độ 1 cấu hình riêng):
    /// tháng bắt đầu, số tháng (1..6, được kéo sang năm sau), kết quả ghi cho cùng tháng hay tháng kế tiếp.
    /// Mỗi tháng 4 tuần -> 1 kết quả tháng. Lưu vào csdl2 (Module_CheDoDoiTuong.CauHinhLichXet) qua Module_LichXet:
    /// mọi form (Form62 sửa, Form15 thống kê, Form17/18/36, năm cũ) tự theo lịch mới, KHÔNG phải sửa phần mềm.
    /// </summary>
    public partial class Form63_CaiDatLichXet : Form
    {
        private readonly CheDoDoiTuong _cheDo;
        private readonly CauHinhLich _cu;
        private bool _dangNap;

        /// <summary>Dùng cho trình thiết kế Visual Studio; khi chạy mở theo chế độ hiện tại.</summary>
        public Form63_CaiDatLichXet() : this(Module_CheDoDoiTuong.LaNhomTuan(Module_CheDoDoiTuong.Hien)
            ? Module_CheDoDoiTuong.Hien : CheDoDoiTuong.TanBinh)
        { }

        public Form63_CaiDatLichXet(CheDoDoiTuong cheDo)
        {
            InitializeComponent();
            Module_GiaoDienForm63Web.ApDung(this);   // giao diện kiểu web (chỉ trình bày)
            Module_BieuTuongNut.ApDung(this);   // biểu tượng nút tự vẽ
            Icon = Module_BieuTuongNut.IconForm("Lich");
            _cheDo = Module_CheDoDoiTuong.LaNhomTuan(cheDo) ? cheDo : CheDoDoiTuong.TanBinh;
            string ten = _cheDo == CheDoDoiTuong.HocVien ? "học viên" : "tân binh";
            Text = $"Lịch xét thi đua {ten}";
            label_TieuDe.Text = $"Lịch xét thi đua {ten}";
            numericUpDown_SoThang.Maximum = Module_LichXet.SoDotToiDa;
            label_SoThang.Text = $"Số tháng xét (1–{Module_LichXet.SoDotToiDa}):";
            _cu = Module_LichXet.DocCauHinh(_cheDo);
            HienCauHinh(_cu);
            comboBox_ThangBatDau.SelectedIndexChanged += (_, _) => XemTruoc();
            numericUpDown_SoThang.ValueChanged += (_, _) => XemTruoc();
            checkBox_KetQuaThangSau.CheckedChanged += (_, _) => XemTruoc();
        }
        private void HienCauHinh(CauHinhLich ch)
        {
            _dangNap = true;
            try
            {
                comboBox_ThangBatDau.SelectedIndex = ch.ThangBatDau - 1;
                numericUpDown_SoThang.Value = ch.SoThang;
                checkBox_KetQuaThangSau.Checked = ch.KetQuaThangSau;
            }
            finally { _dangNap = false; }
            XemTruoc();
        }
        private CauHinhLich DangChon() => new(comboBox_ThangBatDau.SelectedIndex + 1, (int)numericUpDown_SoThang.Value, checkBox_KetQuaThangSau.Checked);

        /// <summary>Bảng xem trước + cảnh báo khi lịch mới làm đổi nghĩa dữ liệu đã nhập.</summary>
        private void XemTruoc()
        {
            if (_dangNap || comboBox_ThangBatDau.SelectedIndex < 0) return;
            var ch = DangChon();
            int nam = Module_HeThong.LayNamHeThong();
            var lich = Module_LichXet.TaoLich(ch, nam);
            var g = dataGridView_XemTruoc;
            g.SuspendLayout();
            try
            {
                g.Rows.Clear();
                foreach (var d in lich)
                    g.Rows.Add($"Đợt {d.So}", Module_LichXet.NhanThang(d.Thang, d.Nam, nam), Module_LichXet.NhanThang(d.ThangKetQua, d.NamKetQua, nam));
                g.CurrentCell = null;
            }
            finally { g.ResumeLayout(); }
            var canhBao = new List<string>();
            if (ch.ThangBatDau != _cu.ThangBatDau || ch.KetQuaThangSau != _cu.KetQuaThangSau)
                canhBao.Add("Dữ liệu đã nhập gắn theo ĐỢT (đợt 1, 2, ...): đổi tháng bắt đầu / cách ghi kết quả sẽ đổi nhãn tháng của dữ liệu đó. " +
                            "Chỉ nên đổi khi bắt đầu khóa mới (sau khi đã lưu trữ năm cũ).");
            if (ch.SoThang < _cu.SoThang)
                canhBao.Add($"Giảm từ {_cu.SoThang} xuống {ch.SoThang} tháng: dữ liệu các tháng sau bị ẨN (vẫn giữ trong CSDL, tăng lại là hiện lại).");
            label_CanhBao.Text = string.Join("\n", canhBao);
        }
        /// <summary>Có dữ liệu thi đua (ô tuần/tháng nào đó đã có giá trị) chưa.</summary>
        private bool CoDuLieuThiDua()
        {
            try
            {
                using var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(Module_DanduongGPS.DuongDanCSDL4, chiDoc: true));
                cn.Open();
                using var cmd = cn.CreateCommand();
                cmd.CommandText = $"SELECT 1 FROM [{Module_CheDoDoiTuong.ThiDuaThang(_cheDo)}] WHERE " +
                    string.Join(" OR ", Module_LichXet.CotVatLy(_cheDo).Select(c => $"([{c}] IS NOT NULL AND [{c}] <> '')")) + " LIMIT 1";
                return cmd.ExecuteScalar() != null;
            }
            catch (Exception ex) { Debug.WriteLine("[Form63] CoDuLieu: " + ex.Message); return false; }
        }
        private void kryptonButton_DatLaiMacDinh_Click(object? sender, EventArgs e) => HienCauHinh(Module_LichXet.MacDinh(_cheDo));
        private void kryptonButton_Luu_Click(object? sender, EventArgs e)
        {
            var ch = DangChon();
            if (ch == _cu) { DialogResult = DialogResult.OK; Close(); return; }
            bool doiNghia = ch.ThangBatDau != _cu.ThangBatDau || ch.KetQuaThangSau != _cu.KetQuaThangSau || ch.SoThang < _cu.SoThang;
            if (doiNghia && CoDuLieuThiDua() &&
                MessageBox.Show("Đã có dữ liệu thi đua theo lịch hiện tại.\n\n" + label_CanhBao.Text + "\n\nVẫn lưu lịch mới?",
                    "Xác nhận đổi lịch xét", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;
            try
            {
                Module_LichXet.LuuCauHinh(_cheDo, ch);
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Cài đặt lịch xét thi đua",
                    $"{_cheDo}: tháng bắt đầu {ch.ThangBatDau} | số tháng {ch.SoThang} | kết quả tháng sau: {(ch.KetQuaThangSau ? "có" : "không")}");
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không lưu được cài đặt: " + ex.Message, Module_HeThong.TieuDe_Loi, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
