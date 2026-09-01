using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhanMemThiDua2026
{
    public partial class Form58_DongBoKetQuaThiDuaNamTruocVeNamHienTai : Form
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private static readonly HashSet<string> KetQuaHopLe = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            Module_HeThong.PL_CSTD, Module_HeThong.PL_CSTT, Module_HeThong.PL_HTNV, Module_HeThong.PL_KHTNV
        };
        public Form58_DongBoKetQuaThiDuaNamTruocVeNamHienTai()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            InitToolTips();
        }
        private void Form58_DongBoKetQuaThiDuaNamTruocVeNamHienTai_Load(object sender, EventArgs e)
        {
            LoadDanhSachCSDLLichSu();
        }
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            var tips = new Dictionary<Control, string>
            {
                { kryptonButton_DongBoDuLieuNamCuVaoCSDLHienTai, "Đồng bộ dữ liệu từ CSDL năm trước vào CSDL hiện tại" },
                { kryptonButton_HuongDanSuDung, "Đọc hướng dẫn sử dụng" }
            };
            foreach (var tip in tips)
            {
                if (tip.Key != null) // an toàn khi refactor / ẩn control
                    toolTip1.SetToolTip(tip.Key, tip.Value);
            }
        }
        private void LoadDanhSachCSDLLichSu()
        {
            try
            {
                string folderPath = Module_DanduongGPS.ThuMucLichSuThiDua;
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Regex chuẩn xác định dạng tên file chứa 4 chữ số năm
                var regexNam = new Regex(@"ThiDua_CBCS_Nam(\d{4})\.db$", RegexOptions.IgnoreCase);

                var files = Directory.GetFiles(folderPath, "ThiDua_CBCS_Nam*.db")
                    .Select(p => new
                    {
                        Match = regexNam.Match(Path.GetFileName(p)),
                        FullPath = p
                    })
                    .Where(x => x.Match.Success)
                    .Select(x => new
                    {
                        HienThiNam = x.Match.Groups[1].Value,
                        x.FullPath
                    })
                    .OrderByDescending(f => f.HienThiNam)
                    .ToList();

                comboBox_ChonCSDLNamCu.DataSource = files;
                comboBox_ChonCSDLNamCu.DisplayMember = "HienThiNam";
                comboBox_ChonCSDLNamCu.ValueMember = "FullPath";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách CSDL lịch sử: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private KetQuaDongBo DongBoDuLieuInternal(string dbNguonPath, string dbDichPath)
        {
            var result = new KetQuaDongBo();
            var mapSoHieuDich = TaoCacheSoHieuCSDL(dbDichPath);

            if (mapSoHieuDich.Count == 0) return result;

            string connStrNguon = $"Data Source={dbNguonPath};Mode=ReadOnly;";
            string connStrDich = $"Data Source={dbDichPath};";

            using (var connNguon = new SqliteConnection(connStrNguon))
            using (var connDich = new SqliteConnection(connStrDich))
            {
                connNguon.Open();
                connDich.Open();

                // Cấu hình WAL mode và Timeout để chống khóa CSDL tối đa
                using (var cmdPragma = connDich.CreateCommand())
                {
                    cmdPragma.CommandText = "PRAGMA journal_mode = WAL; PRAGMA busy_timeout = 10000;";
                    cmdPragma.ExecuteNonQuery();
                }

                string sqlReadNguon = "SELECT SoHieu, TongKet_Nam FROM ThiDuaThang WHERE SoHieu IS NOT NULL AND TRIM(SoHieu) <> ''";

                using (var cmdRead = new SqliteCommand(sqlReadNguon, connNguon))
                using (var reader = cmdRead.ExecuteReader())
                using (var transDich = connDich.BeginTransaction())
                using (var cmdUpdate = connDich.CreateCommand())
                {
                    cmdUpdate.Transaction = transDich;
                    cmdUpdate.CommandText = @"
                        UPDATE ThiDuaThang 
                        SET KQ_ThiDua_Nam_Cu = @KetQua 
                        WHERE SoHieu = @SoHieuMaHoaDich 
                          AND (KQ_ThiDua_Nam_Cu IS NULL OR KQ_ThiDua_Nam_Cu <> @KetQua);";

                    var pKetQua = cmdUpdate.Parameters.Add("@KetQua", SqliteType.Text);
                    var pSoHieuMaHoaDich = cmdUpdate.Parameters.Add("@SoHieuMaHoaDich", SqliteType.Text);

                    cmdUpdate.Prepare();

                    int idxSoHieu = reader.GetOrdinal("SoHieu");
                    int idxTongKet = reader.GetOrdinal("TongKet_Nam");

                    while (reader.Read())
                    {
                        result.TongSoNguoi++;

                        string soHieuMaHoaNguon = reader.IsDBNull(idxSoHieu) ? string.Empty : reader.GetString(idxSoHieu);
                        string tongKetNamText = reader.IsDBNull(idxTongKet) ? string.Empty : reader.GetString(idxTongKet);

                        string soHieuGiaiMa = GiaiMaAnToan(soHieuMaHoaNguon);
                        if (string.IsNullOrWhiteSpace(soHieuGiaiMa))
                        {
                            result.SoGiaiMaLoi++;
                            continue;
                        }

                        string valCapNhat = ChuanHoaKetQua(tongKetNamText);

                        if (mapSoHieuDich.TryGetValue(soHieuGiaiMa, out string soHieuMaHoaDich))
                        {
                            pKetQua.Value = valCapNhat;
                            pSoHieuMaHoaDich.Value = soHieuMaHoaDich;

                            int affected = cmdUpdate.ExecuteNonQuery();
                            if (affected > 0)
                            {
                                result.SoDongCapNhat += affected;
                            }
                            else
                            {
                                result.SoDongKhongThayDoi++;
                            }
                        }
                        else
                        {
                            result.SoDongKhongThayDoi++;
                        }
                    }

                    transDich.Commit();
                }
            }

            return result;
        }
        private Dictionary<string, string> TaoCacheSoHieuCSDL(string dbPath)
        {
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            string connStr = $"Data Source={dbPath};Mode=ReadOnly;";

            using (var conn = new SqliteConnection(connStr))
            {
                conn.Open();

                using (var cmdPragma = conn.CreateCommand())
                {
                    cmdPragma.CommandText = "PRAGMA busy_timeout = 10000;";
                    cmdPragma.ExecuteNonQuery();
                }

                string sql = "SELECT SoHieu FROM ThiDuaThang WHERE SoHieu IS NOT NULL AND TRIM(SoHieu) <> ''";

                using (var cmd = new SqliteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    int idxSoHieu = reader.GetOrdinal("SoHieu");
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(idxSoHieu)) continue;

                        string rawSoHieuMaHoa = reader.GetString(idxSoHieu);
                        string plainSoHieu = GiaiMaAnToan(rawSoHieuMaHoa);

                        if (!string.IsNullOrWhiteSpace(plainSoHieu) && !dict.ContainsKey(plainSoHieu))
                        {
                            dict[plainSoHieu] = rawSoHieuMaHoa;
                        }
                    }
                }
            }

            return dict;
        }
        private string GiaiMaAnToan(string cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText)) return string.Empty;
            try
            {
                // TODO: Gọi hàm giải mã AES thực tế của bạn tại đây
                return cipherText.Trim();
            }
            catch
            {
                return string.Empty; // Trả về rỗng để bỏ qua bản ghi lỗi thay vì dùng dữ liệu chưa giải mã
            }
        }
        private string ChuanHoaKetQua(string giaTri)
        {
            if (string.IsNullOrWhiteSpace(giaTri)) return string.Empty;

            string val = giaTri.Trim().ToUpper();
            return KetQuaHopLe.Contains(val) ? val : string.Empty;
        }
        private void kryptonButton_HuongDanSuDung_Click(object sender, EventArgs e)
        {
            if (sender is not Krypton.Toolkit.KryptonButton button)
                return;

            string textBanDau = button.Text;
            bool enabledBanDau = button.Enabled;

            try
            {
                button.Enabled = false;
                button.Text = "Đang mở...";

                string huongDan = """
                    HƯỚNG DẪN ĐỒNG BỘ KẾT QUẢ THI ĐUA NĂM CŨ
                    1. MỤC ĐÍCH & PHẠM VI
                    - Đưa kết quả tổng kết năm cũ vào dữ liệu năm nay.
                    - Chỉ áp dụng cho CBCS.
                    2. CƠ CHẾ ĐỐI CHIẾU:
                    - Chọn CSDL thi đua của năm cũ.
                    - Đối chiếu người giữa hai năm bằng Số hiệu.
                    3. AN TOÀN DỮ LIỆU
                    - CSDL năm cũ chỉ được đọc, không bị thay đổi.
                    - Chỉ cập nhật cột KQ_ThiDua_Nam_Cu trong CSDL năm nay.
                    - Người không tồn tại hoặc kết quả không hợp lệ sẽ được bỏ qua.
                    4. KẾT QUẢ: Thông báo số bản ghi được cập nhật và giữ nguyên.
                    Lưu ý: Vui lòng chọn đúng CSDL của năm trước trước khi thực hiện.
                    """;

                MessageBox.Show(
                    huongDan,
                    "Hướng dẫn sử dụng",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            finally
            {
                button.Text = textBanDau;
                button.Enabled = enabledBanDau;
            }
        }
        private async void kryptonButton_DongBoDuLieuNamCuVaoCSDLHienTai_Click(object sender, EventArgs e)
        {
            if (comboBox_ChonCSDLNamCu.SelectedValue == null)
            {
                MessageBox.Show(
                    "Vui lòng chọn CSDL năm cũ cần nạp dữ liệu!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string dbNguonPath =
                comboBox_ChonCSDLNamCu.SelectedValue.ToString();

            string dbDichPath =
                Module_DanduongGPS.DuongDanCSDL4;

            if (!File.Exists(dbNguonPath) ||
                !File.Exists(dbDichPath))
            {
                MessageBox.Show(
                    "File CSDL nguồn hoặc CSDL đích không tồn tại!",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            using (Form24_XacMinhAdmin frmXacMinh =
                   new Form24_XacMinhAdmin())
            {
                frmXacMinh.TopMost = true;
                frmXacMinh.StartPosition =
                    FormStartPosition.CenterScreen;

                if (frmXacMinh.ShowDialog() != DialogResult.OK)
                    return;
            }

            if (!await _semaphore.WaitAsync(0))
            {
                MessageBox.Show(
                    "Thao tác đồng bộ đang được thực hiện, vui lòng chờ...",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            Control btn = sender as Control;

            try
            {
                if (btn != null)
                    btn.Enabled = false;

                Cursor = Cursors.WaitCursor;

                KetQuaDongBo result = await Task.Run(() =>
                    DongBoDuLieuInternal(dbNguonPath, dbDichPath));

                string ghiChu =
                    $"Tổng số CBCS kiểm tra: {result.TongSoNguoi}\n" +
                    $"Cập nhật thành công: {result.SoDongCapNhat}\n" +
                    $"Giữ nguyên/Không khớp: {result.SoDongKhongThayDoi}";

                if (result.SoGiaiMaLoi > 0)
                {
                    ghiChu +=
                        $"\nKhông thể giải mã: {result.SoGiaiMaLoi} bản ghi.";
                }

                Module_NhatKy.GhiNhatKy(
                    taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                    hanhDong: "Đồng bộ kết quả thi đua năm cũ",
                    ghiChu: ghiChu);

                string thongBao =
                    $"Đồng bộ hoàn tất!\n\n{ghiChu}";

                MessageBox.Show(
                    thongBao,
                    "Kết quả",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                Form15_ThongKeThiDua frm15 =
                    Application.OpenForms
                        .OfType<Form15_ThongKeThiDua>()
                        .FirstOrDefault();

                if (frm15 != null && !frm15.IsDisposed)
                {
                    frm15.lamMoi_ToolStripMenuItem_Click(null, null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi trong quá trình đồng bộ: {ex.Message}",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                if (btn != null)
                    btn.Enabled = true;

                Cursor = Cursors.Default;
                _semaphore.Release();
            }
        }
        private class KetQuaDongBo
        {
            public int TongSoNguoi { get; set; }
            public int SoDongCapNhat { get; set; }
            public int SoDongKhongThayDoi { get; set; }
            public int SoGiaiMaLoi { get; set; }
        }
    }
}