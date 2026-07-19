using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using System.Data;

namespace PhanMemThiDua2026
{
    public partial class Form19_NapKQPLTuDonVi : Form
    {
        private readonly Form6_XuLyData _form6Ref;
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        public Form19_NapKQPLTuDonVi(Form6_XuLyData form6)
        {
            InitializeComponent();
            _form6Ref = form6;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.ShowInTaskbar = false;

            // DÒNG 1: Ẩn label ngay khi khởi tạo Form
            if (label3_ThongBaoThanhCong != null)
                label3_ThongBaoThanhCong.Visible = false;

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
            if (_form6Ref != null && !_form6Ref.IsDisposed)
            {
                _form6Ref.Invoke(new Action(() =>
                {
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
                        catch { }
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

            // DÒNG 2: Ẩn thông báo cũ trước khi bắt đầu tiến trình mới
            if (label3_ThongBaoThanhCong != null)
                label3_ThongBaoThanhCong.Visible = false;

            try
            {
                kryptonButton_NhapKetQua.Enabled = false;
                kryptonButton_NhapKetQua.Values.Text = "Đang xử lý ...";
                kryptonButton_NhapKetQua.Values.Image = null;

                await Task.Delay(100);

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

                int soDongCapNhat = await Task.Run(() =>
                {
                    Module_BaNhat.NhapDuLieuVaoBangQuanLyBaNhat(duongDanFile, _csdl2Path);
                    DataTable dtExcel = DocExcel(duongDanFile);
                    if (dtExcel == null || dtExcel.Rows.Count == 0) return -1;
                    return CapNhatPhanLoai(donViChon, dtExcel);
                });

                if (soDongCapNhat == -1)
                {
                    MessageBox.Show("File Excel không có dữ liệu hợp lệ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // DÒNG 3: XỬ LÝ CHÍNH XÁC KHI THÀNH CÔNG
                if (label3_ThongBaoThanhCong != null)
                {
                    label3_ThongBaoThanhCong.ForeColor = Color.DarkGreen;
                    label3_ThongBaoThanhCong.Text = $"Vào lúc [{DateTime.Now:HH:mm:ss}] Đã cập nhật {soDongCapNhat} bản ghi.";

                    label3_ThongBaoThanhCong.Visible = true; // HIỆN LÊN khi gán giá trị thành công

                    _ = TuDongAnThongBaoSau(5000); // Gọi hàm phụ tự động ẩn sau 5 giây (5000ms)
                }

                GoiRefershForm6();
                await Module_BieuDoTronTrangChu.CapNhatBieuDoForm4Async();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Có lỗi xảy ra: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                kryptonButton_NhapKetQua.Values.Text = textBanDau;
                kryptonButton_NhapKetQua.Values.Image = anhBanDau;
                kryptonButton_NhapKetQua.Enabled = true;
            }
        }
        // DÒNG 4: Hàm phụ trợ đếm ngược thời gian để tự động ẩn label (An toàn không treo UI)
        private async Task TuDongAnThongBaoSau(int milliseconds)
        {
            await Task.Delay(milliseconds);
            if (label3_ThongBaoThanhCong != null && !label3_ThongBaoThanhCong.IsDisposed)
            {
                label3_ThongBaoThanhCong.Visible = false;
            }
        }
        private void LoadComboBoxDonVi()
        {
            try
            {
                comboBox1_DonVi.BeginUpdate();
                comboBox1_DonVi.Items.Clear();
                var danhSachDonVi = Module_DonVi.GetDanhSachDonVi();
                if (danhSachDonVi is { Count: > 0 })
                {
                    comboBox1_DonVi.Items.AddRange(danhSachDonVi.ToArray());
                    comboBox1_DonVi.SelectedIndex = 0;
                }
            }
            finally { comboBox1_DonVi.EndUpdate(); }
        }
        private void kryptonButton1_ChonDuongDan_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
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
