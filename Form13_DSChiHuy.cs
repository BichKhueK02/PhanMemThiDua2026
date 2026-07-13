using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
namespace PhanMemThiDua2026
{
    public partial class Form13_DSChiHuy : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private readonly Color _focusColor = Color.FromArgb(0, 120, 215);
        private readonly Color _hoverColor = Color.FromArgb(100, 180, 255); // 🌟 Thêm màu Hover
        private readonly Color _normalColor = Color.FromArgb(200, 200, 200);
        private KryptonTextBox[] hovatenTextBoxes = Array.Empty<KryptonTextBox>();
        private KryptonTextBox[] chucvuTextBoxes = Array.Empty<KryptonTextBox>();
        private KryptonTextBox[] tatCaTextBoxes = Array.Empty<KryptonTextBox>();
        private bool dangKiemTra;
        private bool daBaoLoi;
        public Form13_DSChiHuy()
        {
            InitializeComponent();
            KhoiTaoTextBox();
            GanSuKien();
            InitToolTips();
        }
        private void Form13_Load(object sender, EventArgs e)
        {
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            LoadDuLieu();
            KhoaChucVuNeuTrong();

        }
        private void KhoiTaoTextBox()
        {
            hovatenTextBoxes = new[]
            {
                Text_hovaten1, Text_hovaten2, Text_hovaten3,
                Text_hovaten4, Text_hovaten5, Text_hovaten6
            }.Where(x => x != null).ToArray();

            chucvuTextBoxes = new[]
            {
                Text_chucvu1, Text_chucvu2, Text_chucvu3,
                Text_chucvu4, Text_chucvu5, Text_chucvu6
            }.Where(x => x != null).ToArray();

            tatCaTextBoxes = hovatenTextBoxes
                .Concat(chucvuTextBoxes)
                .ToArray();
        }
        private void GanSuKien()
        {
            foreach (var tb in chucvuTextBoxes)
            {
                tb.TextChanged -= XuLyInHoaChucVu;
                tb.TextChanged += XuLyInHoaChucVu;
            }

            foreach (var tb in hovatenTextBoxes)
            {
                tb.TextChanged -= Hovaten_TextChanged;
                tb.TextChanged += Hovaten_TextChanged;
            }

            foreach (var tb in tatCaTextBoxes)
            {
                // 🌟 Set mặc định chuẩn (Đồng bộ cả Color1 và Color2 để xóa Gradient)
                tb.StateCommon.Border.DrawBorders = PaletteDrawBorders.All;
                tb.StateCommon.Border.Width = 1;
                tb.StateCommon.Border.Color1 = _normalColor;
                tb.StateCommon.Border.Color2 = _normalColor;
                tb.StateCommon.Border.Rounding = 4;

                // 🧹 Rút sự kiện cũ trước (Chống Memory Leak)
                tb.Enter -= TextBox_Enter;
                tb.Leave -= TextBox_Leave;
                tb.MouseEnter -= TextBox_MouseEnter;
                tb.MouseLeave -= TextBox_MouseLeave;
                tb.KeyDown -= TextBox_KeyDown; // 🌟 BỔ SUNG DÒNG NÀY

                // ⚡ Cắm sự kiện mới
                tb.Enter += TextBox_Enter;
                tb.Leave += TextBox_Leave;
                tb.MouseEnter += TextBox_MouseEnter;
                tb.MouseLeave += TextBox_MouseLeave;
                tb.KeyDown += TextBox_KeyDown; // 🌟 BỔ SUNG DÒNG NÀY
            }
            kryptonButton1_Btn_Capnhat.Click -= kryptonButton1_Btn_Capnhat_Click;
            kryptonButton1_Btn_Capnhat.Click += kryptonButton1_Btn_Capnhat_Click;
        }
        // =========================================================
        // 🌟 HIỆU ỨNG VIỀN TEXTBOX (UX ENHANCEMENT)
        // =========================================================
        // 🌟 BỔ SUNG ĐOẠN NÀY: Hàm bắt sự kiện Enter
        private void TextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Ngăn tiếng "Ting" mặc định của Windows
                kryptonButton1_Btn_Capnhat.PerformClick(); // Tự động click nút Cập nhật
            }
        }
        private void TextBox_Enter(object? sender, EventArgs e)
        {
            if (sender is KryptonTextBox tb)
            {
                tb.StateCommon.Border.Width = 2;
                tb.StateCommon.Border.Color1 = _focusColor;
                tb.StateCommon.Border.Color2 = _focusColor;
                tb.Refresh(); // 🚀 Ép vẽ lại ngay lập tức
            }
        }
        private void TextBox_Leave(object? sender, EventArgs e)
        {
            if (sender is KryptonTextBox tb)
            {
                tb.StateCommon.Border.Width = 1;
                tb.StateCommon.Border.Color1 = _normalColor;
                tb.StateCommon.Border.Color2 = _normalColor;
                tb.Refresh();
            }
        }
        private void TextBox_MouseEnter(object? sender, EventArgs e)
        {
            if (sender is KryptonTextBox tb && !tb.Focused) // Chỉ hover khi ô đó chưa được click
            {
                tb.StateCommon.Border.Width = 1;
                tb.StateCommon.Border.Color1 = _hoverColor;
                tb.StateCommon.Border.Color2 = _hoverColor;
                tb.Refresh();
            }
        }
        private void TextBox_MouseLeave(object? sender, EventArgs e)
        {
            if (sender is KryptonTextBox tb && !tb.Focused)
            {
                tb.StateCommon.Border.Width = 1;
                tb.StateCommon.Border.Color1 = _normalColor;
                tb.StateCommon.Border.Color2 = _normalColor;
                tb.Refresh();
            }
        }
        private void LoadDuLieu()
        {
            string csdl = _csdl2Path;
            if (!System.IO.File.Exists(csdl)) return;

            try
            {
                using var conn = new SqliteConnection($"Data Source={csdl}");
                conn.Open();

                var dtHienThi = new DataTable();
                dtHienThi.Columns.Add("ID", typeof(int));
                dtHienThi.Columns.Add("HoVaTen", typeof(string));
                dtHienThi.Columns.Add("ChucVu", typeof(string));

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT ID, HoVaTen, ChucVu FROM ChiHuyD WHERE ID BETWEEN 1 AND 6 ORDER BY ID";
                    using var reader = cmd.ExecuteReader();

                    int i = 0;
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        // Giải mã an toàn trước khi hiển thị
                        string hoTen = GiaiMaSafe(reader["HoVaTen"]);
                        string chucVu = GiaiMaSafe(reader["ChucVu"]);

                        dtHienThi.Rows.Add(id, hoTen, chucVu);

                        // Đổ vào Textbox tương ứng
                        if (i < hovatenTextBoxes.Length)
                        {
                            hovatenTextBoxes[i].Text = hoTen;
                            chucvuTextBoxes[i].Text = chucVu;
                        }
                        i++;
                    }
                }

                kryptonDataGridView1.DataSource = dtHienThi;
                DinhDangGrid();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi LoadDuLieu Form 13: " + ex.Message);
            }
        }
        private void Hovaten_TextChanged(object? sender, EventArgs e)
        {
            KhoaChucVuNeuTrong();
        }
        private void XuLyInHoaChucVu(object? sender, EventArgs e)
        {
            if (sender is not KryptonTextBox tb) return;

            int pos = tb.SelectionStart;
            string text = tb.Text.ToUpperInvariant();

            if (tb.Text != text)
            {
                tb.Text = text;
                tb.SelectionStart = pos;
            }
        }
        private void KhoaChucVuNeuTrong()
        {
            for (int i = 0; i < hovatenTextBoxes.Length; i++)
            {
                bool dongTruocCoDuLieu =
                    i == 0 || !string.IsNullOrWhiteSpace(hovatenTextBoxes[i - 1].Text);

                hovatenTextBoxes[i].Enabled = dongTruocCoDuLieu;

                chucvuTextBoxes[i].TextChanged -= XuLyInHoaChucVu;
                chucvuTextBoxes[i].Enabled =
                    !string.IsNullOrWhiteSpace(hovatenTextBoxes[i].Text);

                if (!dongTruocCoDuLieu)
                {
                    hovatenTextBoxes[i].Clear();
                    chucvuTextBoxes[i].Clear();
                }
                else if (string.IsNullOrWhiteSpace(hovatenTextBoxes[i].Text))
                {
                    chucvuTextBoxes[i].Clear();
                }

                chucvuTextBoxes[i].TextChanged += XuLyInHoaChucVu;
            }
        }
        private bool KiemTraHopLe()
        {
            if (dangKiemTra) return false;

            dangKiemTra = true;
            daBaoLoi = false;

            for (int i = 0; i < hovatenTextBoxes.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(hovatenTextBoxes[i].Text) &&
                    string.IsNullOrWhiteSpace(chucvuTextBoxes[i].Text))
                {
                    if (!daBaoLoi)
                    {
                        MessageBox.Show(
                            $"Chức vụ của đồng chí {hovatenTextBoxes[i].Text} không được để trống!",
                            "Lỗi",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        daBaoLoi = true;
                    }

                    dangKiemTra = false;
                    return false;
                }
            }

            dangKiemTra = false;
            return true;
        }
        private async void kryptonButton1_Btn_Capnhat_Click(object? sender, EventArgs e)
        {
            if (!KiemTraHopLe()) return;

            string textBanDau = kryptonButton1_Btn_Capnhat.Values.Text;
            Image anhBanDau = kryptonButton1_Btn_Capnhat.Values.Image;

            try
            {
                kryptonButton1_Btn_Capnhat.Enabled = false;
                kryptonButton1_Btn_Capnhat.Values.Text = "Đang lưu...";
                kryptonButton1_Btn_Capnhat.Values.Image = null;
                label13.Text = "Đang mã hóa và lưu dữ liệu...";
                label13.Visible = true;

                await Task.Delay(100); // Nhịp nghỉ cho UI

                // Chạy tác vụ mã hóa và lưu trữ ngầm để không treo Form
                await Task.Run(() =>
                {
                    using var conn = new SqliteConnection($"Data Source={_csdl2Path}");
                    conn.Open();
                    using var tran = conn.BeginTransaction();

                    try
                    {
                        for (int i = 0; i < hovatenTextBoxes.Length; i++)
                        {
                            string hoTenRaw = hovatenTextBoxes[i].Text.Trim();
                            string chucVuRaw = chucvuTextBoxes[i].Text.Trim();

                            // ⭐ THỰC HIỆN MÃ HÓA V2
                            string hoTenMaHoa = string.IsNullOrEmpty(hoTenRaw) ? "" : BaoMatAES.MaHoa(hoTenRaw);
                            string chucVuMaHoa = string.IsNullOrEmpty(chucVuRaw) ? "" : BaoMatAES.MaHoa(chucVuRaw);

                            using var cmd = conn.CreateCommand();
                            cmd.Transaction = tran;
                            cmd.CommandText = "INSERT OR REPLACE INTO ChiHuyD (ID, HoVaTen, ChucVu) VALUES (@id, @hoten, @chucvu)";
                            cmd.Parameters.AddWithValue("@id", i + 1);
                            cmd.Parameters.AddWithValue("@hoten", hoTenMaHoa);
                            cmd.Parameters.AddWithValue("@chucvu", chucVuMaHoa);
                            cmd.ExecuteNonQuery();
                        }
                        tran.Commit();
                    }
                    catch { tran.Rollback(); throw; }
                });

                label13.ForeColor = Color.DarkGreen;
                label13.Text = "✔ Đã bảo mật và lưu thành công.";

                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Cập nhật lãnh đạo", "Thành công");
                LoadDuLieu(); // Nạp lại để cập nhật Grid
            }
            catch (Exception ex)
            {
                label13.ForeColor = Color.Red;
                label13.Text = "Lỗi hệ thống!";
                MessageBox.Show("Lỗi: " + ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                kryptonButton1_Btn_Capnhat.Values.Text = textBanDau;
                kryptonButton1_Btn_Capnhat.Values.Image = anhBanDau;
                kryptonButton1_Btn_Capnhat.Enabled = true;
            }
        }

        private void DinhDangGrid()
        {
            var grid = kryptonDataGridView1;

            // ===== CẤU HÌNH CHUNG =====
            grid.Dock = DockStyle.Fill;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AllowUserToResizeColumns = false;
            grid.AllowUserToOrderColumns = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.ReadOnly = true;
            grid.EnableHeadersVisualStyles = false;

            // ===== THIẾT KẾ PHẲNG & HIỆN ĐẠI (NEW) =====
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal; // Chỉ hiện kẻ ngang, bỏ kẻ dọc nhìn rất sang
            grid.GridColor = Color.FromArgb(235, 235, 235); // Màu kẻ ngang xám nhạt tinh tế

            // ===== STYLE HEADER =====
            grid.ColumnHeadersHeight = 50; // Tăng độ rộng (chiều cao) tiêu đề để thoáng hơn
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing; // Khóa cứng chiều cao
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None; // Bỏ viền bao quanh tiêu đề

            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), // Phóng to font lên một xíu
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(240, 244, 248), // Nền tiêu đề màu xanh xám nhạt cực êm mắt
                ForeColor = Color.FromArgb(40, 40, 40),    // Chữ màu xám than (không dùng đen tuyền)
                SelectionBackColor = Color.FromArgb(240, 244, 248) // Giữ nguyên màu khi lỡ click vào header
            };

            // ===== STYLE ROW =====
            grid.RowTemplate.Height = 38; // Tăng chiều cao của từng dòng dữ liệu để không bị tù túng

            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(45, 45, 45),
                Padding = new Padding(5, 0, 5, 0), // Lùi lề chữ vào 5px để không bị sát vách
                SelectionBackColor = Color.FromArgb(232, 244, 253), // Khi chọn dòng: nền xanh nước biển nhạt
                SelectionForeColor = Color.FromArgb(0, 102, 204)    // Khi chọn dòng: chữ xanh dương đậm
            };

            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 252, 252); // Sọc dưa cực nhạt cho dòng chẵn/lẻ

            // ===== CẤU HÌNH CỘT =====
            CauHinhCot("ID", "STT", 15, DataGridViewContentAlignment.MiddleCenter);
            CauHinhCot("HoVaTen", "Họ và tên chỉ huy", 45, DataGridViewContentAlignment.MiddleLeft);
            CauHinhCot("ChucVu", "Chức vụ chỉ huy", 40, DataGridViewContentAlignment.MiddleLeft);

            // ===== TẮT SORT =====
            foreach (DataGridViewColumn col in grid.Columns)
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        private void CauHinhCot(string name, string header, int fillWeight, DataGridViewContentAlignment align)
        {
            if (kryptonDataGridView1.Columns.Contains(name))
            {
                var col = kryptonDataGridView1.Columns[name];
                col.HeaderText = header;
                col.FillWeight = fillWeight;
                col.DefaultCellStyle.Alignment = align;
            }
        }
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Gợi ý nhập liệu";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            toolTip1.SetToolTip(
                kryptonButton1_Btn_Capnhat,
                "Cập nhật danh sách chỉ huy đơn vị");
        }
        private string GiaiMaSafe(object? value)
        {
            if (value == null || value == DBNull.Value) return string.Empty;
            string input = value.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            try
            {
                string result = BaoMatAES.GiaiMa(input);
                // Nếu giải mã ra rỗng (lỗi định dạng), trả về chính nó (dữ liệu thô cũ)
                return string.IsNullOrEmpty(result) ? input : result;
            }
            catch { return input; }
        }
        // 🛑 BẢO VỆ RAM KHI ĐÓNG FORM (DỌN DẸP SỰ KIỆN)
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                if (tatCaTextBoxes != null)
                {
                    foreach (var tb in tatCaTextBoxes)
                    {
                        if (tb == null || tb.IsDisposed) continue;
                        tb.Enter -= TextBox_Enter;
                        tb.Leave -= TextBox_Leave;
                        tb.MouseEnter -= TextBox_MouseEnter;
                        tb.MouseLeave -= TextBox_MouseLeave;
                        tb.KeyDown -= TextBox_KeyDown; // 🌟 BỔ SUNG DÒNG NÀY
                    }
                }
            }
            catch { }

            base.OnFormClosed(e);
        }
    }
}
