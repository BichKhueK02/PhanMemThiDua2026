using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using System.Data;

namespace PhanMemThiDua2026
{
    public partial class Form19_NapKQPLTuDonVi : Form
    {
        private readonly Form6_XuLyData _form6Ref;
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        // --- THÊM 2 BIẾN NÀY ĐỂ CHỐNG NẠP TRÙNG ---
        private string _donViDaNapThanhCong = "";
        private string _fileDaNapThanhCong = "";
        public Form19_NapKQPLTuDonVi(Form6_XuLyData form6)
        {
            InitializeComponent();
            _form6Ref = form6;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.ShowInTaskbar = false;

            // DÒNG 1: Ẩn label ngay khi khởi tạo Form
            if (toolStripStatusLabel1_ThongBao != null)
                toolStripStatusLabel1_ThongBao.Visible = false;

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
            if (_form6Ref != null &&
         !_form6Ref.IsDisposed &&
         _form6Ref.IsHandleCreated)
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
                // Từ điển tra cứu không phân biệt chữ hoa/thường
                var lookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                using (var cmdAll = new SqliteCommand(
                    "SELECT ID, DonVi, SoHieu FROM DanhSach",
                    cn,
                    tran))
                using (var reader = cmdAll.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            int id = Convert.ToInt32(reader["ID"]);

                            string donViCS = BaoMatAES.GiaiMa(reader["DonVi"]?.ToString() ?? "").Trim();
                            string soHieuCS = BaoMatAES.GiaiMa(reader["SoHieu"]?.ToString() ?? "").Trim();

                            if (!string.IsNullOrWhiteSpace(donViCS) &&
                                !string.IsNullOrWhiteSpace(soHieuCS))
                            {
                                lookup[donViCS + "|" + soHieuCS] = id;
                            }
                        }
                        catch
                        {
                            // Bỏ qua bản ghi lỗi và tiếp tục xử lý
                        }
                    }
                }

                int soDongCapNhat = 0;

                using var cmdUpdate = new SqliteCommand(
                    "UPDATE DanhSach SET PhanLoai=@PhanLoai WHERE ID=@ID",
                    cn,
                    tran);

                var pPhanLoai = cmdUpdate.Parameters.Add("@PhanLoai", SqliteType.Text);
                var pID = cmdUpdate.Parameters.Add("@ID", SqliteType.Integer);

                foreach (DataRow excelRow in dtExcel.Rows)
                {
                    string soHieuExcel = excelRow["SoHieu"]?.ToString()?.Trim() ?? "";
                    string donViExcel = excelRow["DonVi"]?.ToString()?.Trim() ?? "";
                    string phanLoaiExcel = excelRow["PhanLoai"]?.ToString()?.Trim() ?? "";

                    if (!donViExcel.Equals(donViChon, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (lookup.TryGetValue(donViExcel + "|" + soHieuExcel, out int id))
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
            // Lấy thông tin ngay từ đầu để kiểm tra trước
            string donViChon = comboBox1_DonVi.Text.Trim();
            string duongDanFile = label_DuongDan.Text?.Trim();

            // 1. Kiểm tra rỗng
            if (string.IsNullOrWhiteSpace(donViChon))
            {
                MessageBox.Show("Bạn chưa chọn đơn vị!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(duongDanFile) || duongDanFile == "Chưa chọn tệp" || !File.Exists(duongDanFile))
            {
                MessageBox.Show("Vui lòng chọn file Excel!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                kryptonButton1_ChonDuongDan.PerformClick();
                return;
            }

            // 2. THÊM CHỐT CHẶN: Nếu người dùng bấm lại y xì thông tin vừa nạp thành công
            if (donViChon.Equals(_donViDaNapThanhCong, StringComparison.OrdinalIgnoreCase) &&
                duongDanFile.Equals(_fileDaNapThanhCong, StringComparison.OrdinalIgnoreCase))
            {
                if (toolStripStatusLabel1_ThongBao != null)
                {
                    toolStripStatusLabel1_ThongBao.ForeColor = Color.DarkOrange; // Đổi sang màu cam/đỏ để gây chú ý
                    toolStripStatusLabel1_ThongBao.Text = $"Dữ liệu của \"{donViChon}\" từ tệp này đã nạp rồi. Vui lòng chọn đơn vị/tệp khác!";
                    toolStripStatusLabel1_ThongBao.Visible = true;

                    // Tự động dọn dẹp dòng cảnh báo này sau 5 giây
                    _ = TuDongAnThongBaoSau(5000);
                }
                return; // Chặn ngay, không cho chạy xuống dưới
            }

            string textBanDau = kryptonButton_NhapKetQua.Values.Text;
            Image anhBanDau = kryptonButton_NhapKetQua.Values.Image;

            // Ẩn thông báo cũ trước khi bắt đầu tiến trình mới
            if (toolStripStatusLabel1_ThongBao != null)
                toolStripStatusLabel1_ThongBao.Visible = false;

            try
            {
                kryptonButton_NhapKetQua.Enabled = false;
                kryptonButton_NhapKetQua.Values.Text = "Đang xử lý ...";
                kryptonButton_NhapKetQua.Values.Image = null;

                await Task.Delay(100); // Đợi UI cập nhật trạng thái nút bấm

                // Chạy ngầm tác vụ nạp DB
                int soDongCapNhat = await Task.Run(() =>
                {
                    Module_BaNhat.NhapDuLieuVaoBangQuanLyBaNhat(duongDanFile, _csdl2Path);

                    // Dùng using để thu hồi RAM của DataTable ngay sau khi lấy xong
                    using (DataTable dtExcel = DocExcel(duongDanFile))
                    {
                        if (dtExcel == null || dtExcel.Rows.Count == 0) return -1;
                        return CapNhatPhanLoai(donViChon, dtExcel);
                    }
                });

                // Chặn lỗi văng nếu người dùng lỡ tắt Form lúc đang chạy ngầm
                if (IsDisposed || Disposing)
                    return;

                if (soDongCapNhat == -1)
                {
                    MessageBox.Show("File Excel không có dữ liệu hợp lệ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // XỬ LÝ CHÍNH XÁC KHI THÀNH CÔNG
                if (toolStripStatusLabel1_ThongBao != null)
                {
                    toolStripStatusLabel1_ThongBao.ForeColor = Color.DarkGreen;
                    toolStripStatusLabel1_ThongBao.Text = $"[{DateTime.Now:HH:mm:ss}] Đã cập nhật thành công {soDongCapNhat} bản ghi của đơn vị \"{donViChon}\".";
                    toolStripStatusLabel1_ThongBao.Visible = true;

                    _ = TuDongAnThongBaoSau(5000); // Tự động ẩn sau 5 giây
                }

                // ==========================================
                // LƯU LỊCH SỬ ĐỂ CHỐNG NGƯỜI DÙNG BẤM TRÙNG
                _donViDaNapThanhCong = donViChon;
                _fileDaNapThanhCong = duongDanFile;
                // ==========================================

                GoiRefershForm6();
                await Module_BieuDoTronTrangChu.CapNhatBieuDoForm4Async();
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                {
                    MessageBox.Show($"Có lỗi xảy ra: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                if (!IsDisposed)
                {
                    kryptonButton_NhapKetQua.Values.Text = textBanDau;
                    kryptonButton_NhapKetQua.Values.Image = anhBanDau;
                    kryptonButton_NhapKetQua.Enabled = true;
                }
            }
        }
        private async Task TuDongAnThongBaoSau(int milliseconds)
        {
            await Task.Delay(milliseconds);
            if (toolStripStatusLabel1_ThongBao != null && !toolStripStatusLabel1_ThongBao.IsDisposed)
            {
                toolStripStatusLabel1_ThongBao.Visible = false;
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
                Filter = "Excel Files|*.xlsx;*.xlsm",
                Title = "Chọn tệp Excel"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                // Cập nhật nhãn đường dẫn
                label_DuongDan.Text = ofd.FileName;
                label_DuongDan.ForeColor = Color.Green;

                // THÊM MỚI: Báo cho người dùng biết đã chọn tệp thành công
                if (toolStripStatusLabel1_ThongBao != null)
                {
                    // Dùng màu Blue (hoặc màu bạn thích) để phân biệt với thông báo nạp CSDL (DarkGreen)
                    toolStripStatusLabel1_ThongBao.ForeColor = Color.Blue;
                    toolStripStatusLabel1_ThongBao.Text = "Đã chọn tệp excel, vui lòng chọn đơn vị và bấm [Nhập kết quả] để tiến hành đồng bộ.";
                    toolStripStatusLabel1_ThongBao.Visible = true;

                    // Gọi hàm ẩn tự động sau 5 giây (không làm treo UI)
                    _ = TuDongAnThongBaoSau(5000);
                }
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            // Tạo nội dung hướng dẫn sử dụng (dùng ký tự @ để viết chuỗi trên nhiều dòng)
            string msgHuongDan = @"GIỚI THIỆU:
Chức năng này giúp nạp hàng loạt kết quả phân loại thi đua từ file Excel vào hệ thống tự động, bảo mật và chính xác.

HƯỚNG DẪN SỬ DỤNG:
1. Chuẩn bị file Excel: Đảm bảo có dòng tiêu đề và chứa 3 cột bắt buộc (viết đúng chính tả): Số hiệu, Đơn vị, Phân loại.
2. Chọn Đơn vị: Click vào danh sách và chọn đúng đơn vị cần nạp.
3. Chọn File: Bấm [ Chọn đường dẫn ] và tìm tệp Excel vừa chuẩn bị.
4. Thực hiện: Bấm [ Nhập kết quả ] và đợi hệ thống xử lý.

LƯU Ý: 
- Không đóng cửa sổ trong lúc phần mềm đang chạy.
- Hệ thống sẽ tự động đối chiếu, mã hóa dữ liệu và cập nhật biểu đồ sau khi hoàn tất.";

            // Hiển thị hộp thoại thông báo
            MessageBox.Show(msgHuongDan,
                            "Hướng dẫn sử dụng - Nạp Kết Quả",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
        }


    }
}
