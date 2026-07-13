
using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhanMemThiDua2026
{
    public partial class Form41_TyLeBCHD : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private readonly Color _focusColor = Color.FromArgb(0, 120, 215);
        private readonly Color _normalColor = Color.FromArgb(200, 200, 200);

        public static event Action? OnQuyDinhBCHChanged;

        private CancellationTokenSource? _ctsLuuDuLieu;
        private static readonly object _logLock = new object();
        private System.Windows.Forms.Timer _timerThongBao;

        public Dictionary<string, string[]> DeNghiBCHMapping { get; private set; }
            = new Dictionary<string, string[]>();

        private KryptonTextBox?[,] _txtGrid = new KryptonTextBox?[4, 5];

        private void InitTextBoxGrid()
        {
            _txtGrid = new KryptonTextBox?[4, 5]
            {
                { textBox_A1, textBox_B1, textBox_C1, textBox_D1, textBox_E1 },
                { textBox_A2, textBox_B2, textBox_C2, textBox_D2, textBox_E2 },
                { textBox_A3, textBox_B3, textBox_C3, textBox_D3, textBox_E3 },
                {
                    this.Controls.Find("textBox_A4", true).FirstOrDefault() as KryptonTextBox,
                    this.Controls.Find("textBox_B4", true).FirstOrDefault() as KryptonTextBox,
                    this.Controls.Find("textBox_C4", true).FirstOrDefault() as KryptonTextBox,
                    this.Controls.Find("textBox_D4", true).FirstOrDefault() as KryptonTextBox,
                    this.Controls.Find("textBox_E4", true).FirstOrDefault() as KryptonTextBox
                }
            };
        }

        public Form41_TyLeBCHD()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            this.ShowInTaskbar = false;

            if (label1_ThongBao != null)
            {
                label1_ThongBao.Visible = false;
            }

            _timerThongBao = new System.Windows.Forms.Timer();
            _timerThongBao.Interval = 3000;
            _timerThongBao.Tick += _timerThongBao_Tick;
        }

        private void _timerThongBao_Tick(object? sender, EventArgs e)
        {
            _timerThongBao.Stop();
            if (label1_ThongBao != null)
            {
                label1_ThongBao.Visible = false;
            }
        }

        private void HienThiThongBao(string noiDung, Color mauChu)
        {
            if (label1_ThongBao == null) return;

            label1_ThongBao.AutoSize = true;
            string thoiGian = DateTime.Now.ToString("HH:mm:ss");
            label1_ThongBao.Text = $"[{thoiGian}] {noiDung}";
            label1_ThongBao.ForeColor = mauChu;

            label1_ThongBao.Visible = true;
            label1_ThongBao.BringToFront();
            label1_ThongBao.Refresh();

            _timerThongBao.Stop();
            _timerThongBao.Start();
        }

        private async void Form41_TyLeBCHD_Load(object sender, EventArgs e)
        {
            InitTextBoxGrid();
            await LoadQuyDinhTyLeBCHAsync();
            GanSuKienFocusTextBox();
            InitToolTips();
            SetupStatusStrip();
        }

        private void SetupStatusStrip()
        {
            try
            {
                statusStrip1.SizingGrip = false;
                statusStrip1.AutoSize = false;
                toolStripStatusLabel1.Text = $"Phiên bản: {Module_PhienBan.SoftwareVersion}";
                toolStripStatusLabel1.Alignment = ToolStripItemAlignment.Left;
                toolStripStatusLabel2.Text = Module_PhienBan.NgayThangNamCapNhat;
                toolStripStatusLabel2.Alignment = ToolStripItemAlignment.Right;

                var springLabel = new ToolStripStatusLabel { Spring = true };

                statusStrip1.SuspendLayout();
                statusStrip1.Items.Clear();
                statusStrip1.Items.Add(toolStripStatusLabel1);
                statusStrip1.Items.Add(springLabel);
                statusStrip1.Items.Add(toolStripStatusLabel2);
                statusStrip1.ResumeLayout();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi SetupStatusStrip: " + ex.Message);
            }
        }

        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Thao tác";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;

            if (kryptonButton_LuuE29 != null)
            {
                toolTip1.SetToolTip(kryptonButton_LuuE29, "Lưu quy định tỷ lệ BCH vào cơ sở dữ liệu");
            }
        }

        // ====================================================================
        // LOAD DỮ LIỆU: Bỏ hoàn toàn ký tự % khi nạp lên giao diện TextBox
        // ====================================================================
        private async Task LoadQuyDinhTyLeBCHAsync()
        {
            if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path)) return;

            try
            {
                using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                await conn.OpenAsync();

                DeNghiBCHMapping.Clear();

                int rows = _txtGrid.GetLength(0);
                int cols = _txtGrid.GetLength(1);

                for (int id = 1; id <= rows; id++)
                {
                    string[] values = new string[cols];

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"SELECT Loai_1, Loai_2, Loai_3, Loai_4, Khong_PL 
                                        FROM QuyDinhTyLeBCH WHERE ID=@id";
                    cmd.Parameters.AddWithValue("@id", id);

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        for (int i = 0; i < cols; i++)
                        {
                            // Đọc chuỗi và loại bỏ ký tự % ngay lập tức nếu CSDL lỡ có lưu trước đó
                            string val = reader.IsDBNull(i) ? "0" : reader.GetString(i).Replace("%", "").Trim();
                            values[i] = val;

                            if (_txtGrid[id - 1, i] != null)
                            {
                                _txtGrid[id - 1, i]!.Text = val;
                            }
                        }
                    }

                    DeNghiBCHMapping[$"Loại {id}"] = values;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải dữ liệu tỷ lệ BCH!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine(ex);
            }
        }

        // ====================================================================
        // SAVE DỮ LIỆU: Lưu số thuần túy vào TEXT CSDL (Không chèn thêm đuôi %)
        // ====================================================================
        private async Task SaveQuyDinhTyLeBCHAsync(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path)) return;

            using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
            await conn.OpenAsync(ct);

            using var tran = conn.BeginTransaction();
            try
            {
                int rows = _txtGrid.GetLength(0);
                int cols = _txtGrid.GetLength(1);

                var tempMapping = new Dictionary<string, string[]>();

                using var cmd = conn.CreateCommand();
                cmd.Transaction = tran;
                cmd.CommandText = @"UPDATE QuyDinhTyLeBCH 
                                    SET Loai_1=@l1, Loai_2=@l2, Loai_3=@l3, Loai_4=@l4, Khong_PL=@kpl
                                    WHERE ID=@id";

                cmd.Parameters.Add("@l1", SqliteType.Text);
                cmd.Parameters.Add("@l2", SqliteType.Text);
                cmd.Parameters.Add("@l3", SqliteType.Text);
                cmd.Parameters.Add("@l4", SqliteType.Text);
                cmd.Parameters.Add("@kpl", SqliteType.Text);
                cmd.Parameters.Add("@id", SqliteType.Integer);

                const int COL_LOAI1 = 0;
                const int COL_LOAI2 = 1;
                const int COL_LOAI3 = 2;
                const int COL_LOAI4 = 3;
                const int COL_KHONGPL = 4;

                for (int id = 1; id <= rows; id++)
                {
                    ct.ThrowIfCancellationRequested();

                    string[] values = new string[cols];
                    for (int i = 0; i < cols; i++)
                    {
                        // Lấy chuỗi thô người dùng nhập, xóa sạch mọi ký tự % nếu họ vô tình gõ vào
                        string txtVal = _txtGrid[id - 1, i]?.Text?.Replace("%", "").Trim() ?? "0";

                        // Giới hạn giá trị nhập an toàn từ 0 đến 100
                        if (int.TryParse(txtVal, out int num))
                        {
                            txtVal = Math.Clamp(num, 0, 100).ToString();
                        }
                        else
                        {
                            txtVal = "0"; // Mặc định về 0 nếu nhập chữ lỗi
                        }

                        values[i] = txtVal;
                    }

                    cmd.Parameters["@l1"].Value = values[COL_LOAI1];
                    cmd.Parameters["@l2"].Value = values[COL_LOAI2];
                    cmd.Parameters["@l3"].Value = values[COL_LOAI3];
                    cmd.Parameters["@l4"].Value = values[COL_LOAI4];
                    cmd.Parameters["@kpl"].Value = values[COL_KHONGPL];
                    cmd.Parameters["@id"].Value = id;

                    await cmd.ExecuteNonQueryAsync(ct);
                    tempMapping[$"Loại {id}"] = values;
                }

                tran.Commit();
                DeNghiBCHMapping = tempMapping;
            }
            catch (OperationCanceledException)
            {
                tran.Rollback();
            }
            catch (Exception ex)
            {
                tran.Rollback();
                string logPath = Path.Combine(Application.StartupPath, "LoiHeThong.txt");
                string noiDungLoi = $"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] Lỗi lưu cấu hình BCH Form41: {ex.Message}{Environment.NewLine}";
                try
                {
                    lock (_logLock)
                    {
                        File.AppendAllText(logPath, noiDungLoi);
                    }
                }
                catch { }
                throw;
            }
        }

        private async void kryptonButton_LuuE29_Click(object sender, EventArgs e)
        {
            if (!kryptonButton_LuuE29.Enabled) return;

            _ctsLuuDuLieu?.Dispose();
            _ctsLuuDuLieu = new CancellationTokenSource();
            var token = _ctsLuuDuLieu.Token;

            string textBanDau = kryptonButton_LuuE29.Values.Text;
            Image? anhBanDau = kryptonButton_LuuE29.Values.Image;

            try
            {
                kryptonButton_LuuE29.Enabled = false;
                kryptonButton_LuuE29.Values.Text = "Đang lưu...";
                kryptonButton_LuuE29.Values.Image = null;

                HienThiThongBao("Hệ thống đang thực hiện lưu quy định tỷ lệ BCH...", Color.Black);

                await Task.Delay(250, token);

                await SaveQuyDinhTyLeBCHAsync(token);

                OnQuyDinhBCHChanged?.Invoke();

                HienThiThongBao("✔ Đã lưu quy định tỷ lệ BCH thành công!", Color.DarkGreen);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                HienThiThongBao("✘ Lỗi lưu quy định tỷ lệ BCH!", Color.Red);
                MessageBox.Show("Đã xảy ra lỗi khi lưu dữ liệu vào CSDL:\n\n" + ex.Message,
                                "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!this.IsDisposed)
                {
                    kryptonButton_LuuE29.Values.Text = textBanDau;
                    kryptonButton_LuuE29.Values.Image = anhBanDau;
                    kryptonButton_LuuE29.Enabled = true;
                }
            }
        }

       

        private void TextBox_Enter(object? sender, EventArgs e)
        {
            if (sender is not KryptonTextBox tb) return;
            tb.StateCommon.Border.DrawBorders = PaletteDrawBorders.All;
            tb.StateCommon.Border.Rounding = 4;
            tb.StateCommon.Border.Width = 2;
            tb.StateCommon.Border.Color1 = _focusColor;
        }

        private void TextBox_Leave(object? sender, EventArgs e)
        {
            if (sender is not KryptonTextBox tb) return;
            tb.StateCommon.Border.Width = 1;
            tb.StateCommon.Border.Color1 = _normalColor;
        }

        private void GanSuKienFocusTextBox()
        {
            foreach (Control ctrl in this.Controls)
            {
                GanDeQuyTextBox(ctrl);
            }
        }

        private void GanDeQuyTextBox(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                if (ctrl is KryptonTextBox tb)
                {
                    tb.Enter += TextBox_Enter;
                    tb.Leave += TextBox_Leave;
                }
                if (ctrl.HasChildren)
                    GanDeQuyTextBox(ctrl);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _ctsLuuDuLieu?.Cancel();
            _ctsLuuDuLieu?.Dispose();
            _timerThongBao?.Stop();
            _timerThongBao?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
