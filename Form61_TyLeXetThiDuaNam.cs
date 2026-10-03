using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
namespace PhanMemThiDua2026
{
    //Tham số xxxyyyy
    public partial class Form61_TyLeXetThiDuaNam : Form
    {
        private readonly string _connectionString;
        private readonly Dictionary<KryptonTextBox, string> _giaTriHopLeTruocDo = new(3);
        private bool _dangXuLyThayDoiTyLe;
        public Form61_TyLeXetThiDuaNam()
        {
            InitializeComponent();
            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = Module_DanduongGPS.DuongDanCSDL2
            }.ToString();
            KryptonTextBox[] cacOTyLe =
            {
                kryptonTextBox1_TyLeXetCSTD,
                kryptonTextBox1_TyLeXetCSTT,
                kryptonTextBox1_TyLeXetHTNV
            };
            foreach (KryptonTextBox textBox in cacOTyLe)
            {
                _giaTriHopLeTruocDo[textBox] = "";
                textBox.KeyPress += TyLe_KeyPress;
                textBox.TextChanged += TyLe_TextChanged;
            }
        }
        private void Form61_TyLeXetThiDuaNam_Load(object sender, EventArgs e)
        {
            // 1. Cập nhật tiêu đề hiển thị Năm hệ thống
            label3_TieuDe.Text = $"Tỷ lệ xét thi đua năm {Module_XuatPhanLoai.LayNamHeThong()}";
            // 2. Kiểm tra và tạo bảng trong csdl2 nếu chưa tồn tại
            TaoBang_QuyDinhTyLe_NeuChuaCo();
            // 3. Tải dữ liệu đã lưu (nếu có) lên Form
            TaiDuLieuTyLe();
        }
        #region Kiểm tra dữ liệu nhập
        /// <summary>Nội dung đang gõ có hợp lệ không (rỗng được phép): chỉ gồm chữ số, tối đa 3 ký tự, giá trị &lt;= 100.</summary>
        private static bool LaNoiDungNhapHopLe(string text)
        {
            if (text.Length == 0) return true;
            if (text.Length > 3) return false;
            foreach (char c in text)
                if (c < '0' || c > '9') return false;
            return int.Parse(text, NumberStyles.None, CultureInfo.InvariantCulture) <= 100;
        }
        /// <summary>Tỷ lệ hoàn chỉnh để lưu: không rỗng và hợp lệ (0 - 100).</summary>
        private static bool LaTyLeHopLe(string? text, out int value)
        {
            value = 0;
            if (string.IsNullOrEmpty(text) || !LaNoiDungNhapHopLe(text)) return false;
            value = int.Parse(text, NumberStyles.None, CultureInfo.InvariantCulture);
            return true;
        }
        private void TyLe_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (sender is not KryptonTextBox textBox) return;
            if (e.KeyChar < '0' || e.KeyChar > '9')
            {
                e.Handled = true;
                return;
            }
            string text = textBox.Text
                .Remove(textBox.SelectionStart, textBox.SelectionLength)
                .Insert(textBox.SelectionStart, e.KeyChar.ToString());
            if (!LaNoiDungNhapHopLe(text))
                e.Handled = true;
        }
        private void TyLe_TextChanged(object? sender, EventArgs e)
        {
            if (_dangXuLyThayDoiTyLe || sender is not KryptonTextBox textBox) return;
            string text = textBox.Text;
            if (LaNoiDungNhapHopLe(text))
            {
                _giaTriHopLeTruocDo[textBox] = text;
                return;
            }
            // Nội dung không hợp lệ (thường do dán) -> trả về giá trị hợp lệ trước đó
            string giaTriCu = _giaTriHopLeTruocDo[textBox];
            int viTriConTro = textBox.SelectionStart;
            try
            {
                _dangXuLyThayDoiTyLe = true;
                textBox.Text = giaTriCu;
                textBox.SelectionStart = Math.Min(viTriConTro, giaTriCu.Length);
            }
            finally
            {
                _dangXuLyThayDoiTyLe = false;
            }
        }
        #endregion
        #region CSDL
        /// <summary>Kiểm tra và khởi tạo bảng QuyDinhTyLe_XetThiDuaNam trong CSDL2</summary>
        private void TaoBang_QuyDinhTyLe_NeuChuaCo()
        {
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS QuyDinhTyLe_XetThiDuaNam (
                        ID INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        TyLe_CSTD TEXT,
                        TyLe_CSTT TEXT,
                        TyLe_HTNV TEXT
                    );";
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi kiểm tra/tạo bảng CSDL: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void TaiDuLieuTyLe()
        {
            string cstd = "", cstt = "", htnv = "";
            bool coDuLieuLoi = false;
            try
            {
                // Đọc xong và đóng kết nối TRƯỚC khi hiện bất kỳ MessageBox nào
                using (var conn = new SqliteConnection(_connectionString))
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        SELECT TyLe_CSTD, TyLe_CSTT, TyLe_HTNV
                        FROM QuyDinhTyLe_XetThiDuaNam
                        ORDER BY ID DESC LIMIT 1;";
                    using var reader = cmd.ExecuteReader();
                    if (!reader.Read()) return;
                    cstd = DocTyLe(reader, 0, ref coDuLieuLoi);
                    cstt = DocTyLe(reader, 1, ref coDuLieuLoi);
                    htnv = DocTyLe(reader, 2, ref coDuLieuLoi);
                }
                kryptonTextBox1_TyLeXetCSTD.Text = cstd;
                kryptonTextBox1_TyLeXetCSTT.Text = cstt;
                kryptonTextBox1_TyLeXetHTNV.Text = htnv;
                if (coDuLieuLoi)
                {
                    MessageBox.Show(
                        "Một số tỷ lệ đã lưu không hợp lệ và đã được để trống. Vui lòng kiểm tra lại trước khi lưu.",
                        "Cảnh báo dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu tỷ lệ: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private static string DocTyLe(SqliteDataReader reader, int cot, ref bool coDuLieuLoi)
        {
            string text = reader.IsDBNull(cot) ? "" : (reader.GetString(cot) ?? "").Trim();
            if (text.Length == 0) return "";
            if (LaTyLeHopLe(text, out int value))
                return value.ToString(CultureInfo.InvariantCulture); // chuẩn hóa, bỏ số 0 đứng đầu
            coDuLieuLoi = true;
            return "";
        }
        #endregion
        private void kryptonButton1_LuuVaDongForm_Click(object sender, EventArgs e)
        {
            if (!LaTyLeHopLe(kryptonTextBox1_TyLeXetCSTD.Text.Trim(), out int cstd) ||
                !LaTyLeHopLe(kryptonTextBox1_TyLeXetCSTT.Text.Trim(), out int cstt) ||
                !LaTyLeHopLe(kryptonTextBox1_TyLeXetHTNV.Text.Trim(), out int htnv))
            {
                MessageBox.Show(
                    "Vui lòng nhập đầy đủ cả 3 tỷ lệ bằng số nguyên từ 0 đến 100.",
                    "Dữ liệu không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                using (var conn = new SqliteConnection(_connectionString))
                {
                    conn.Open();
                    using var tran = conn.BeginTransaction();
                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = tran;
                    cmd.Parameters.Add("@TyLe_CSTD", SqliteType.Text).Value = cstd.ToString(CultureInfo.InvariantCulture);
                    cmd.Parameters.Add("@TyLe_CSTT", SqliteType.Text).Value = cstt.ToString(CultureInfo.InvariantCulture);
                    cmd.Parameters.Add("@TyLe_HTNV", SqliteType.Text).Value = htnv.ToString(CultureInfo.InvariantCulture);
                    // Cập nhật dòng mới nhất; chưa có dòng nào thì thêm mới (1 bước, nằm trong transaction)
                    cmd.CommandText = @"
                        UPDATE QuyDinhTyLe_XetThiDuaNam
                        SET TyLe_CSTD = @TyLe_CSTD, TyLe_CSTT = @TyLe_CSTT, TyLe_HTNV = @TyLe_HTNV
                        WHERE ID = (SELECT MAX(ID) FROM QuyDinhTyLe_XetThiDuaNam);";
                    if (cmd.ExecuteNonQuery() == 0)
                    {
                        cmd.CommandText = @"
                            INSERT INTO QuyDinhTyLe_XetThiDuaNam (TyLe_CSTD, TyLe_CSTT, TyLe_HTNV)
                            VALUES (@TyLe_CSTD, @TyLe_CSTT, @TyLe_HTNV);";
                        cmd.ExecuteNonQuery();
                    }
                    tran.Commit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // Ghi nhật ký tách riêng: lỗi nhật ký không được làm người dùng tưởng lưu thất bại
            try
            {
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Cập nhật tỷ lệ xét thi đua thành công!", $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi ghi nhật ký tỷ lệ xét thi đua: " + ex.Message);
            }
            this.Close();
        }
    }
}
