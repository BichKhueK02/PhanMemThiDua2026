using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using System.Data;

namespace PhanMemThiDua2026
{
    public partial class Form19_NapKQPLTuDonVi : Form
    {
        private readonly Form6_XuLyData _form6Ref; // Thêm readonly và đổi tên theo chuẩn _
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        public Form19_NapKQPLTuDonVi(Form6_XuLyData form6)
        {
            InitializeComponent();
            _form6Ref = form6;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.ShowInTaskbar = false;

            // FIX LỖI: Thống nhất dùng chữ thường 'k' theo Designer
            kryptonButton_NhapKetQua.Click -= kryptonButton_NhapKetQua_Click;
            kryptonButton_NhapKetQua.Click += kryptonButton_NhapKetQua_Click;
            kryptonButton1_ChonDuongDan.Click -= kryptonButton1_ChonDuongDan_Click;
            kryptonButton1_ChonDuongDan.Click += kryptonButton1_ChonDuongDan_Click;

            label_DuongDan.Text = "Chưa chọn tệp";
            label_DuongDan.ForeColor = Color.Red;

            kryptonButton_NhapKetQua.Height = comboBox1_DonVi.Height + 8;
            kryptonButton1_ChonDuongDan.Height = comboBox1_DonVi.Height + 3;
            InitToolTips();
        }
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Gợi ý thao tác";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;

            // Tối ưu initialization
            var tips = new Dictionary<Control, string>
            {
                { kryptonButton1_ChonDuongDan, "Chọn tệp dữ liệu kết quả cần nhập vào hệ thống" },
                { comboBox1_DonVi, "Chọn đơn vị áp dụng kết quả nhập" },
                { kryptonButton_NhapKetQua, "Thực hiện nhập và đồng bộ kết quả vào cơ sở dữ liệu" }
            };

            foreach (var tip in tips)
            {
                if (tip.Key != null)
                    toolTip1.SetToolTip(tip.Key, tip.Value);
            }
        }
        private readonly Dictionary<string, string> ColMapVN = new()
        {
            { "STT", "STT" }, { "Họ và tên", "HoVaTen" }, { "Số hiệu", "SoHieu" },
            { "Năm sinh", "NamSinh" }, { "Quê quán", "QueQuan" }, { "Ngày vào CAND", "NgayVaoCAND" },
            { "Cấp bậc", "CapBac" }, { "Chức vụ", "ChucVu" }, { "Đơn vị", "DonVi" },
            { "Phân loại", "PhanLoai" }, { "Ghi chú", "GhiChu" }
        };
        private void Form19_NapKQPLTuDonVi_Load(object sender, EventArgs e)
        {
            LoadComboBoxDonVi();
        }
        private void GoiRefershForm6()
        {
            // Kiểm tra an toàn xem Form 6 có tồn tại và chưa bị đóng không
            if (_form6Ref != null && !_form6Ref.IsDisposed)
            {
                // 🔥 ĐIỂM MẤU CHỐT: Dùng Invoke để ép lệnh chạy trên đúng UI Thread của Form 6
                _form6Ref.Invoke(new Action(() =>
                {
                    // Gọi hàm kích hoạt giả lập nút Refresh (F5) bên Form 6.
                    // Hàm này có sẵn vòng đời an toàn: Khóa Grid -> Xóa Cache -> Tải DB -> Vẽ lại Grid
                    _form6Ref.RefreshCSDL();
                }));
            }
        }
        private DataTable DocExcel(string duongDanFile)
        {
            var dt = new DataTable();
            dt.Columns.Add("SoHieu");
            dt.Columns.Add("DonVi");
            dt.Columns.Add("PhanLoai");

            try
            {
                using var stream = File.Open(duongDanFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var wb = new XLWorkbook(stream);
                var ws = wb.Worksheets.FirstOrDefault() ?? throw new Exception("File Excel không có Sheet.");

                var headerRow = ws.Row(1) ?? throw new Exception("Không tìm thấy dòng tiêu đề.");
                var colMap = new Dictionary<string, int>();
                int lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

                for (int c = 1; c <= lastCol; c++)
                {
                    string header = headerRow.Cell(c).GetString().Trim();
                    if (!string.IsNullOrEmpty(header) && ColMapVN.TryGetValue(header, out var internalName))
                    {
                        colMap[internalName] = c;
                    }
                }

                string[] requiredCols = { "SoHieu", "DonVi", "PhanLoai" };
                foreach (var col in requiredCols)
                {
                    if (!colMap.ContainsKey(col))
                    {
                        var vnName = ColMapVN.FirstOrDefault(x => x.Value == col).Key;
                        throw new Exception($"File Excel thiếu cột bắt buộc: {vnName}");
                    }
                }

                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
                for (int r = 2; r <= lastRow; r++)
                {
                    var row = ws.Row(r);
                    if (row == null) continue;

                    string soHieu = row.Cell(colMap["SoHieu"]).GetString().Trim();
                    string donVi = row.Cell(colMap["DonVi"]).GetString().Trim();
                    string phanLoai = row.Cell(colMap["PhanLoai"]).GetString().Trim();

                    if (string.IsNullOrWhiteSpace(soHieu) || string.IsNullOrWhiteSpace(donVi))
                        continue;

                    dt.Rows.Add(soHieu, donVi, phanLoai);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Không đọc được file Excel: " + ex.Message);
            }
            return dt;
        }
        // Đổi từ void sang int để trả về số dòng đã cập nhật
        private int CapNhatPhanLoai(string donViChon, DataTable dtExcel)
        {
            using var cn = new SqliteConnection("Data Source=" + _csdl2Path);
            cn.Open();
            using var tran = cn.BeginTransaction();

            try
            {
                var lookup = new Dictionary<string, int>();
                using (var cmdAll = new SqliteCommand("SELECT ID, DonVi, SoHieu FROM DanhSach", cn, tran))
                using (var reader = cmdAll.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            int id = Convert.ToInt32(reader["ID"]);
                            string donViCS = BaoMatAES.GiaiMa(reader["DonVi"]?.ToString() ?? "").Trim();
                            string soHieuCS = BaoMatAES.GiaiMa(reader["SoHieu"]?.ToString() ?? "").Trim();

                            if (!string.IsNullOrEmpty(donViCS) && !string.IsNullOrEmpty(soHieuCS))
                            {
                                lookup[(donViCS + "|" + soHieuCS).ToLower()] = id;
                            }
                        }
                        catch { /* Bỏ qua dòng lỗi giải mã */ }
                    }
                }

                int soDongCapNhat = 0;
                using var cmdUpdate = new SqliteCommand("UPDATE DanhSach SET PhanLoai=@PhanLoai WHERE ID=@ID", cn, tran);
                var pPhanLoai = cmdUpdate.Parameters.Add("@PhanLoai", SqliteType.Text);
                var pID = cmdUpdate.Parameters.Add("@ID", SqliteType.Integer);

                foreach (DataRow excelRow in dtExcel.Rows)
                {
                    string soHieuExcel = excelRow["SoHieu"]?.ToString()?.Trim() ?? "";
                    string donViExcel = excelRow["DonVi"]?.ToString()?.Trim() ?? "";
                    string phanLoaiExcel = excelRow["PhanLoai"]?.ToString()?.Trim() ?? "";

                    if (!donViExcel.Equals(donViChon, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (lookup.TryGetValue((donViExcel + "|" + soHieuExcel).ToLower(), out int id))
                    {
                        pPhanLoai.Value = BaoMatAES.MaHoa(phanLoaiExcel);
                        pID.Value = id;
                        cmdUpdate.ExecuteNonQuery();
                        soDongCapNhat++;
                    }
                }
                tran.Commit();

                // TRẢ VỀ CON SỐ, TUYỆT ĐỐI KHÔNG GỌI LABEL Ở ĐÂY NỮA
                return soDongCapNhat;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }
        private async void kryptonButton_NhapKetQua_Click(object sender, EventArgs e)
        {
            string textBanDau = kryptonButton_NhapKetQua.Values.Text;
            Image anhBanDau = kryptonButton_NhapKetQua.Values.Image;

            try
            {
                kryptonButton_NhapKetQua.Enabled = false;
                kryptonButton_NhapKetQua.Values.Text = "Đang xử lý ...";
                kryptonButton_NhapKetQua.Values.Image = null;

                await Task.Delay(100); // Bây giờ await đã hoạt động vì có 'async' ở trên

                string donViChon = comboBox1_DonVi.Text.Trim();
                if (string.IsNullOrWhiteSpace(donViChon))
                {
                    MessageBox.Show("Bạn chưa chọn đơn vị!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string duongDanFile = label_DuongDan.Text?.Trim();
                if (string.IsNullOrWhiteSpace(duongDanFile) || duongDanFile == "Chưa chọn tệp" || !File.Exists(duongDanFile))
                {
                    MessageBox.Show("Vui lòng chọn file Excel!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    kryptonButton1_ChonDuongDan.PerformClick();
                    return;
                }

                // 🔥 ĐẨY TOÀN BỘ LOGIC NẶNG XUỐNG LUỒNG NGẦM (TASK.RUN)
                int soDongCapNhat = await Task.Run(() =>
                {
                    DataTable dtExcel = DocExcel(duongDanFile);

                    // Nếu không có dữ liệu, trả về -1 để báo lỗi
                    if (dtExcel is not { Rows.Count: > 0 }) return -1;

                    // Gọi hàm cập nhật và lấy kết quả số lượng dòng
                    return CapNhatPhanLoai(donViChon, dtExcel);
                });

                // 🔥 ĐÃ QUAY LẠI LUỒNG GIAO DIỆN (UI THREAD), THOẢI MÁI CẬP NHẬT LABEL
                if (soDongCapNhat == -1)
                {
                    MessageBox.Show("File Excel không có dữ liệu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (label3_ThongBaoThanhCong != null)
                {
                    label3_ThongBaoThanhCong.ForeColor = Color.DarkGreen;
                    label3_ThongBaoThanhCong.Text = $"Vào lúc [{DateTime.Now:HH:mm:ss}] Đã cập nhật {soDongCapNhat} bản ghi.";
                }

                GoiRefershForm6();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                kryptonButton_NhapKetQua.Values.Text = textBanDau;
                kryptonButton_NhapKetQua.Values.Image = anhBanDau;
                kryptonButton_NhapKetQua.Enabled = true;
            }
        }
        private void LoadComboBoxDonVi()
        {
            try
            {
                comboBox1_DonVi.BeginUpdate();
                comboBox1_DonVi.Items.Clear();
                var danhSachDonVi = Module_DonVi.GetDanhSachDonVi();
                if (danhSachDonVi is { Count: > 0 }) // Tối ưu null check
                {
                    comboBox1_DonVi.Items.AddRange(danhSachDonVi.ToArray());
                    comboBox1_DonVi.SelectedIndex = 0;
                }
            }
            finally { comboBox1_DonVi.EndUpdate(); }
        }
        // FIX LỖI: Thêm async, thống nhất tên hàm và tên Button (chữ k thường)
        private void kryptonButton1_ChonDuongDan_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog // Tối ưu using
            {
                Filter = "Excel Files|*.xlsx;*.xls",
                Title = "Chọn tệp Excel"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                label_DuongDan.Text = ofd.FileName;
                label_DuongDan.ForeColor = Color.Green;
            }
        }
    }
}