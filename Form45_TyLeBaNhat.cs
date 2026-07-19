using Microsoft.Data.Sqlite;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhanMemThiDua2026
{
    public partial class Form45_TyLeBaNhat : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        public Form45_TyLeBaNhat()
        {
            InitializeComponent();

            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            this.AcceptButton = btnLuu; // ✅ Enter = Thêm
            Load += Form45_TyLeBaNhat_Load;

            // Gắn sự kiện chặn phím chữ ngay trong Constructor (Cách làm của bạn rất hay)
            textBoxKrypton_TyLePhanTramBaNhat.KeyPress += txtTyLe_KeyPress;
        }
        private async void Form45_TyLeBaNhat_Load(object sender, EventArgs e)
        {
            // Bỏ hàm KhoiTaoBangTyLeBaNhat() ở ngoài, gộp thẳng vào tiến trình nền của hàm Load để giao diện mượt hơn
            await LoadTyLeBaNhatAsync();
        }
        private async Task LoadTyLeBaNhatAsync()
        {
            try
            {
                string tyLe = await Task.Run(() =>
                {
                    // Thêm Mode=ReadWriteCreate để SQLite tự động sinh ra file nếu file chưa tồn tại
                    using var cn = new SqliteConnection($"Data Source={_csdl2Path};Mode=ReadWriteCreate");
                    cn.Open();

                    using (var cmd = cn.CreateCommand())
                    {
                        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS QuyDinhTyLe_BaNhat
(
    ID INTEGER PRIMARY KEY,
    TyLe TEXT NOT NULL
);
INSERT OR IGNORE INTO QuyDinhTyLe_BaNhat(ID, TyLe)
VALUES(1, '');
";
                        cmd.ExecuteNonQuery();
                    }

                    using var cmdLoad = cn.CreateCommand();
                    cmdLoad.CommandText = "SELECT TyLe FROM QuyDinhTyLe_BaNhat WHERE ID = 1;";
                    object obj = cmdLoad.ExecuteScalar();

                    if (obj == null || obj == DBNull.Value) return "";

                    string value = obj.ToString();

                    if (string.IsNullOrWhiteSpace(value)) return "";

                    try
                    {
                        return BaoMatAES.GiaiMa(value);
                    }
                    catch
                    {
                        return ""; // Trả về rỗng nếu AES đổi key
                    }
                });

                textBoxKrypton_TyLePhanTramBaNhat.Text = tyLe;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không thể đọc dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        //        private async void btnLuu_Click(object sender, EventArgs e)
        //        {
        //            string text = textBoxKrypton_TyLePhanTramBaNhat.Text.Trim();

        //            // Gộp 2 lệnh IF của bạn làm 1 cho gọn
        //            if (!int.TryParse(text, out int tyLe) || tyLe < 0 || tyLe > 100)
        //            {
        //                MessageBox.Show("Vui lòng nhập một số nguyên từ 0 đến 100.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //                textBoxKrypton_TyLePhanTramBaNhat.Focus();
        //                textBoxKrypton_TyLePhanTramBaNhat.SelectAll();
        //                return;
        //            }

        //            await SaveTyLeBaNhatAsync(tyLe);
        //        }
        //        private async Task SaveTyLeBaNhatAsync(int tyLe)
        //        {
        //            string textGoc = btnLuu.Values.Text;
        //            btnLuu.Enabled = false;
        //            btnLuu.Values.Text = "Đang lưu...";

        //            try
        //            {
        //                await Task.Run(() =>
        //                {
        //                    using var cn = new SqliteConnection($"Data Source={_csdl2Path};Mode=ReadWriteCreate");
        //                    cn.Open();
        //                    using var tran = cn.BeginTransaction();

        //                    try
        //                    {
        //                        using var cmd = cn.CreateCommand();
        //                        cmd.Transaction = tran;

        //                        // Kỹ thuật UPSERT của bạn rất xuất sắc, mình giữ nguyên
        //                        cmd.CommandText = @"
        //CREATE TABLE IF NOT EXISTS QuyDinhTyLe_BaNhat
        //(
        //    ID INTEGER PRIMARY KEY,
        //    TyLe TEXT NOT NULL
        //);
        //INSERT INTO QuyDinhTyLe_BaNhat(ID, TyLe)
        //VALUES(1, @TyLe)
        //ON CONFLICT(ID)
        //DO UPDATE SET TyLe=excluded.TyLe;
        //";
        //                        cmd.Parameters.AddWithValue("@TyLe", BaoMatAES.MaHoa(tyLe.ToString()));
        //                        cmd.ExecuteNonQuery();
        //                        tran.Commit();
        //                    }
        //                    catch
        //                    {
        //                        tran.Rollback();
        //                        throw;
        //                    }
        //                });

        //                MessageBox.Show("Đã lưu thành công.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //            }
        //            catch (Exception ex)
        //            {
        //                MessageBox.Show(ex.Message, "Không thể lưu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //            }
        //            finally
        //            {
        //                btnLuu.Enabled = true;
        //                btnLuu.Values.Text = textGoc;
        //            }
        //        }
        private async void btnLuu_Click(object sender, EventArgs e)
        {
            string text = textBoxKrypton_TyLePhanTramBaNhat.Text.Trim();

            if (!int.TryParse(text, out int tyLe) || tyLe < 0 || tyLe > 100)
            {
                MessageBox.Show("Vui lòng nhập một số nguyên từ 0 đến 100.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBoxKrypton_TyLePhanTramBaNhat.Focus();
                textBoxKrypton_TyLePhanTramBaNhat.SelectAll();
                return;
            }

            await SaveTyLeBaNhatAsync(tyLe);
        }
        private async Task SaveTyLeBaNhatAsync(int tyLe)
        {
            string textGoc = btnLuu.Values.Text;
            btnLuu.Enabled = false;
            btnLuu.Values.Text = "Đang lưu...";

            try
            {
                await Task.Run(() =>
                {
                    using var cn = new SqliteConnection($"Data Source={_csdl2Path};Mode=ReadWriteCreate");
                    cn.Open();
                    using var tran = cn.BeginTransaction();

                    try
                    {
                        using var cmd = cn.CreateCommand();
                        cmd.Transaction = tran;

                        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS QuyDinhTyLe_BaNhat(    ID INTEGER PRIMARY KEY,    TyLe TEXT NOT NULL);INSERT INTO QuyDinhTyLe_BaNhat(ID, TyLe)VALUES(1, @TyLe)ON CONFLICT(ID)DO UPDATE SET TyLe=excluded.TyLe;";
                        cmd.Parameters.AddWithValue("@TyLe", BaoMatAES.MaHoa(tyLe.ToString()));
                        cmd.ExecuteNonQuery();
                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                });

                // 1. Nếu lưu DB xong, đổi chữ hiển thị báo thành công
                btnLuu.Values.Text = "Lưu thành công!";

                // 2. Dừng lại chờ theo đúng yêu cầu
                await Task.Delay(200);
            }
            catch (Exception ex)
            {
                // Chỉ khi nào lỗi mới quăng MessageBox ra
                MessageBox.Show(ex.Message, "Không thể lưu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Cuối cùng (dù thành công hay lỗi), luôn trả lại trạng thái gốc cho nút bấm
                btnLuu.Enabled = true;
                btnLuu.Values.Text = textGoc;
            }
        }
        private void txtTyLe_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }
    }
}