using Krypton.Toolkit;
///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using System.Data;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form59_QuanLyThiDuaTapTheNamCu : Form
    {
        // Cache danh sách 14 KryptonTextBox để thao tác nhanh và chống rò rỉ bộ nhớ
        private KryptonTextBox[]? _danhSachTextBoxThang;
        private readonly string _thuMucLichSuPath = Module_DanduongGPS.ThuMucLichSuThiDua;
        public Form59_QuanLyThiDuaTapTheNamCu()
        {
            InitializeComponent();
            DinhMenuChuotPhai();                // trước ApDung: mục "Làm mới" mới thêm cũng được gắn biểu tượng
            Module_BieuTuongNut.ApDung(this);   // biểu tượng nút: Hiện đại = tự vẽ, Cổ điển = ảnh gốc nhúng trong exe
            Module_GiaoDienForm59Web.ApDung(this);   // giao diện kiểu web như Form4 - chỉ đổi trình bày (đây là dòng DUY NHẤT thêm vào code gốc)
            // Chỉ đăng ký sự kiện CellClick ở đây
            this.kryptonDataGridView1.CellClick += kryptonDataGridView1_CellClick;
            InitThangMapping();
        }
        private async void Form59_QuanLyThiDuaTapTheNamCu_Load(object? sender, EventArgs e)
        {
            this.SuspendLayout();
            try
            {
                // 1. Khởi tạo cấu hình Giao diện & Control
                KhoiTaoDanhSachTextBox();
                KhoiTaoComboBoxThangVaPhanLoai();
                InitToolTips();
                Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1);
                DinhDangGiaoDienDataGridTongKetThang(kryptonDataGridView1);
                // 2. Tải danh sách CSDL bất đồng bộ
                await TaiDanhSachCoSoDuLieuNamCuAsync();
                // 3. Chủ động gọi load dữ liệu dòng đầu tiên
                if (comboBox1_ChonCscdThiDuaTapTheNamCu.SelectedItem is NamThiDuaTapTheItem itemChon)
                {
                    TaiDuLieuVaoDataGridView(itemChon.DuongDanCSDL);
                }
                // 🌟 Cập nhật số lượng dòng SAU KHI đã nạp dữ liệu vào DataGridView thành công
                CapNhatSoLuongDongDataGrid();
                // 4. Đăng ký sự kiện SelectedIndexChanged SAU CÙNG
                this.comboBox1_ChonCscdThiDuaTapTheNamCu.SelectedIndexChanged -= comboBox1_ChonCscdThiDuaTapTheNamCu_SelectedIndexChanged;
                this.comboBox1_ChonCscdThiDuaTapTheNamCu.SelectedIndexChanged += comboBox1_ChonCscdThiDuaTapTheNamCu_SelectedIndexChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi Load Form59]: {ex.Message}");
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }
        /// <summary>
        /// Menu chuột phải (không sửa Designer): ẩn "Xóa toàn bộ dữ liệu" (đã có nút Xóa trên form),
        /// thêm "Làm mới" lên đầu = bấm nút Làm mới.
        /// </summary>
        private void DinhMenuChuotPhai()
        {
            xoaToanBoDuLieu_ToolStripMenuItem.Available = false;
            xoaTimKiem_ToolStripMenuItem.Available = false;     // form không có ô tìm kiếm
            xuatDuLieuRaTepExcel_ToolStripMenuItem.Text = "Xuất tệp Excel";
            xuatDuLieuRaTepExcel_ToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+E";
            xuatDuLieuRaTepExcel_ToolStripMenuItem.Click += (_, _) => XuatTepExcel();
            var lamMoi = new ToolStripMenuItem { Name = "lamMoi_ToolStripMenuItem", Text = "Làm mới", ShortcutKeyDisplayString = "F5" };
            lamMoi.Click += (_, _) => kryptonButton1_LamMoi.PerformClick();
            contextMenuStrip1.Items.Insert(0, lamMoi);   // gắn vào menu TRƯỚC để lấy đúng cỡ ảnh của menu (ImageScalingSize)
            Module_BieuTuongNut.GanBieuTuongChoMucThem(this, lamMoi);
        }
        // Phím tắt đồng bộ phần mềm: F5 Làm mới, Ctrl+E Xuất tệp Excel (bắt ở đây, menu chỉ hiện chữ phím tắt)
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            try
            {
                if (Module_PhimTat.XuLy(
                    keyData,
                    actionLamMoi: () => kryptonButton1_LamMoi.PerformClick(),                     // F5
                    actionXuatExcel: () => xuatDuLieuRaTepExcel_ToolStripMenuItem.PerformClick())) // Ctrl+E
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi phím tắt Form59: " + ex.Message);
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        // ===================== XUẤT TỆP EXCEL (cùng mẫu Form23 - thi đua tập thể năm hiện tại) =====================
        private static string LayTenTieuDoan()
        {
            try
            {
                using var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(Module_DanduongGPS.DuongDanCSDL2, chiDoc: true));
                cn.Open();
                using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT TenTieuDoan FROM ThongTin WHERE ID = 1";
                var kq = cmd.ExecuteScalar();
                if (kq != null && kq != DBNull.Value)
                {
                    string chuoi = kq.ToString()!.Trim();
                    string giaiMa = Module_BaoMatAES.GiaiMa(chuoi);
                    return string.IsNullOrEmpty(giaiMa) ? chuoi : giaiMa;
                }
            }
            catch { }
            return string.Empty;
        }
        private void XuatTepExcel()
        {
            // 1. Kiểm tra: đã chọn năm + lưới có dòng dữ liệu
            if (comboBox1_ChonCscdThiDuaTapTheNamCu.SelectedItem is not NamThiDuaTapTheItem itemChon)
            {
                MessageBox.Show("Vui lòng chọn năm cần xuất dữ liệu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var cot = kryptonDataGridView1.Columns.Cast<DataGridViewColumn>()
                .Where(c => c.Visible).OrderBy(c => c.DisplayIndex).ToList();   // bỏ cột ID ẩn
            DataGridViewRow? rowNguon = kryptonDataGridView1.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => !r.IsNewRow);
            if (!kryptonDataGridView1.Visible || cot.Count == 0 || rowNguon == null)
            {
                MessageBox.Show("Không có dữ liệu hợp lệ để xuất Excel.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // 2. Thông tin cơ bản
            string tenDonVi = LayTenTieuDoan();
            if (string.IsNullOrWhiteSpace(tenDonVi)) tenDonVi = "ĐƠN VỊ";
            string tenHienThi = char.ToUpperInvariant(tenDonVi[0]) + tenDonVi.Substring(1).ToLowerInvariant();
            string nam = itemChon.TenHienThi;   // năm của tệp năm cũ đang chọn (không phải năm hệ thống)
            string tenFile = $"BẢNG THỐNG KÊ PHÂN LOẠI THI ĐUA TẬP THỂ {tenDonVi} NĂM {nam}_{DateTime.Now:HHmmss}.xlsx";
            foreach (char c in Path.GetInvalidFileNameChars()) tenFile = tenFile.Replace(c, '_');
            using SaveFileDialog sfd = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                FileName = tenFile,
                Filter = Module_HeThong.BoLoc_Excel,
                AddExtension = true,
                OverwritePrompt = true
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("ThongKe");
                // Tổng kết năm lưu mức phân loại (Loại 1..4) thì đổi sang danh hiệu; đã là danh hiệu thì giữ nguyên
                var mapTongKetNam = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [Module_HeThong.Loai_1] = Form23_ThongKeThiDuaTapThe.DanhHieu_Loai1,
                    [Module_HeThong.Loai_2] = Form23_ThongKeThiDuaTapThe.DanhHieu_Loai2,
                    [Module_HeThong.Loai_3] = Form23_ThongKeThiDuaTapThe.DanhHieu_Loai3,
                    [Module_HeThong.Loai_4] = Form23_ThongKeThiDuaTapThe.DanhHieu_Loai4,
                    [Module_HeThong.PL_KHONG_PL] = string.Empty
                };
                int soCotExcel = cot.Count + 1;
                string cotCuoi = XLHelper.GetColumnLetterFromNumber(soCotExcel);
                // Tiêu đề
                var titleRange = ws.Range($"A1:{cotCuoi}1");
                titleRange.Merge();
                titleRange.Value = $"KẾT QUẢ PHÂN LOẠI TẬP THỂ {tenDonVi} NĂM {nam}";
                titleRange.Style.Font.Bold = true;
                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                // Hàng tiêu đề cột
                ws.Cell(3, 1).Value = Module_HeThong.TieuDeCot.DonVi;
                for (int i = 0; i < cot.Count; i++)
                    ws.Cell(3, i + 2).Value = (cot[i].HeaderText ?? "").Replace("\r", "").Replace("\n", " ").Trim();
                var headerRange = ws.Range($"A3:{cotCuoi}3");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(217, 234, 211);
                // Dữ liệu: 1 dòng của đơn vị
                ws.Cell(4, 1).Value = tenHienThi;
                for (int i = 0; i < cot.Count; i++)
                {
                    string giaTriGoc = rowNguon.Cells[cot[i].Index].Value?.ToString()?.Trim() ?? string.Empty;
                    bool laTongKetNam = cot[i].Name == "TongKet_Nam";
                    string giaTriXuat = laTongKetNam && mapTongKetNam.TryGetValue(giaTriGoc, out string? danhHieu) ? danhHieu : giaTriGoc;
                    var cell = ws.Cell(4, i + 2);
                    cell.Value = giaTriXuat;
                    if (giaTriXuat is Form23_ThongKeThiDuaTapThe.DanhHieu_Loai1 or Form23_ThongKeThiDuaTapThe.DanhHieu_Loai2)
                    {
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.FontColor = XLColor.DarkGreen;
                    }
                    else if (giaTriXuat == Form23_ThongKeThiDuaTapThe.DanhHieu_Loai4)
                    {
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.FontColor = XLColor.DarkRed;
                    }
                }
                ws.Row(4).Height = 36;
                var row4Range = ws.Range($"A4:{cotCuoi}4");
                row4Range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                row4Range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                row4Range.Style.Alignment.WrapText = true;
                // Định dạng chung
                var allRange = ws.Range($"A1:{cotCuoi}4");
                allRange.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                allRange.Style.Font.FontSize = 14;
                allRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range($"A3:{cotCuoi}4").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                ws.Columns($"A:{cotCuoi}").AdjustToContents();
                Module_BanQuyen.DongDauExcel(wb);
                Module_ExcelAnToan.LuuAnToan(wb, sfd.FileName);
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Xuất dữ liệu Excel",
                    $"Xuất kết quả thi đua tập thể năm {nam} | {Path.GetFileName(sfd.FileName)}");
                if (File.Exists(sfd.FileName)) Module_XuatNhapDuLieuThiDua.MoVaChonTepTrongExplorer(sfd.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể xuất Excel.\nChi tiết: " + ex.Message, Module_HeThong.TieuDe_Loi, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { Cursor.Current = Cursors.Default; }
        }
        private void KhoiTaoDanhSachTextBox()
        {
            _danhSachTextBoxThang ??= new KryptonTextBox[]  {
        KryptonTextBox_Thang12NamCu, KryptonTextBox_Thang1, KryptonTextBox_Thang2,
        KryptonTextBox_Thang3, KryptonTextBox_Thang4, KryptonTextBox_Thang5,
        KryptonTextBox_6ThangDauNam, KryptonTextBox_Thang6, KryptonTextBox_Thang7,
        KryptonTextBox_Thang8, KryptonTextBox_Thang9, KryptonTextBox_Thang10,
        KryptonTextBox_Thang11, KryptonTextBox_TongKetNam
            };
        }
        private void InitToolTips()
        {
            if (toolTip1 == null) return;
            try
            {
                toolTip1.IsBalloon = true;
                toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
                toolTip1.ToolTipIcon = ToolTipIcon.Info;
                toolTip1.InitialDelay = 300;
                toolTip1.AutoPopDelay = 2500;
                toolTip1.ReshowDelay = 100;
                toolTip1.ShowAlways = true;
                (Control? control, string noiDung)[] danhSachToolTip =
                {
                    (kryptonButton_CapNhatThiDuaTapTheNamCu, "Cập nhật thông tin thi đua tập thể năm cũ"),
                    (kryptonButton1_LamMoi, "Kiểm tra và kết nối lại CSDL, làm tươi lại giao diện"),
                    (kryptonButton_XoaThiDuaTapThe, "Xóa thông tin thi đua tập thể đang chọn")
                };
                foreach (var (control, noiDung) in danhSachToolTip)
                {
                    GanToolTipAnToan(control, noiDung);
                }
            }
            catch (Exception ex) when (ex is ObjectDisposedException || ex is InvalidOperationException)
            {
                // Bỏ qua lỗi giao diện Tooltip không đáng có
            }
        }
        private void GanToolTipAnToan(Control? control, string noiDung)
        {
            if (control == null || control.IsDisposed || control.Disposing || string.IsNullOrWhiteSpace(noiDung) || toolTip1 == null)
                return;
            try
            {
                toolTip1.SetToolTip(control, noiDung);
            }
            catch (Exception ex) when (ex is ObjectDisposedException || ex is InvalidOperationException)
            {
                // Ignore
            }
        }
        /// <summary>
        /// Hàm Reload dữ liệu public dùng để các Form khác (như Form53) gọi khi Form59 đang nằm trên RAM 
        /// nhằm làm tươi toàn bộ CSDL năm cũ và giao diện.
        /// </summary>
        public async Task ReloadDataAsync()
        {
            // Kiểm tra an toàn xem Form hoặc Control đã bị Hủy (Disposed) chưa
            if (this.IsDisposed || !this.IsHandleCreated) return;
            // Đảm bảo thao tác UI luôn chạy đúng trên Thread chính (UI Thread)
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(async () => await ReloadDataAsync()));
                return;
            }
            this.SuspendLayout();
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                // 1. Khởi tạo danh sách TextBox nếu chưa có
                KhoiTaoDanhSachTextBox();
                // 2. Xóa dữ liệu cũ trên DataGridView & TextBox để chuẩn bị nạp lại
                kryptonDataGridView1.DataSource = null;
                XoaTrangCacTextBox();
                // 3. Quét và tải lại danh sách các file CSDL năm cũ trong thư mục
                await TaiDanhSachCoSoDuLieuNamCuAsync();
                // 4. Nếu ComboBox có dữ liệu năm cũ, tự động tải bảng ghi của item đang được chọn
                if (comboBox1_ChonCscdThiDuaTapTheNamCu.SelectedItem is NamThiDuaTapTheItem itemChon)
                {
                    TaiDuLieuVaoDataGridView(itemChon.DuongDanCSDL);
                }
                // 5. Áp dụng lại định dạng UI DataGridView
                DinhDangGiaoDienDataGridTongKetThang(kryptonDataGridView1);
                CapNhatLabelThiDuaThang();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi Reload Form59]: {ex.Message}");
            }
            finally
            {
                Cursor.Current = Cursors.Default;
                this.ResumeLayout(true);
            }
        }
        // Khai báo ở cấp class, tạo 1 lần duy nhất
        private (KryptonTextBox TextBox, string Column)[] _thangMapping = null!;
        private void InitThangMapping()
        {
            _thangMapping = new (KryptonTextBox, string)[]
            {
        (KryptonTextBox_Thang12NamCu, "Thang_12_Nam_Cu"),
        (KryptonTextBox_Thang1,       "Thang_1"),
        (KryptonTextBox_Thang2,       "Thang_2"),
        (KryptonTextBox_Thang3,       "Thang_3"),
        (KryptonTextBox_Thang4,       "Thang_4"),
        (KryptonTextBox_Thang5,       "Thang_5"),
        (KryptonTextBox_6ThangDauNam, "Sau_Thang_Dau_Nam"),
        (KryptonTextBox_Thang6,       "Thang_6"),
        (KryptonTextBox_Thang7,       "Thang_7"),
        (KryptonTextBox_Thang8,       "Thang_8"),
        (KryptonTextBox_Thang9,       "Thang_9"),
        (KryptonTextBox_Thang10,      "Thang_10"),
        (KryptonTextBox_Thang11,      "Thang_11"),
        (KryptonTextBox_TongKetNam,   "TongKet_Nam"),
            };
        }
        private void kryptonDataGridView1_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || kryptonDataGridView1 == null) return;
            var row = kryptonDataGridView1.Rows[e.RowIndex];
            if (kryptonDataGridView1.Columns.Contains("ID"))
                KryptonTextBox_Thang12NamCu.Tag = row.Cells["ID"].Value;
            foreach (var (textBox, column) in _thangMapping)
                SetTextBoxText(textBox, row, column);
        }
        private void SetTextBoxText(KryptonTextBox txt, DataGridViewRow row, string columnName)
        {
            if (txt == null) return;
            if (row.DataGridView != null && row.DataGridView.Columns.Contains(columnName))
            {
                string value = row.Cells[columnName].Value?.ToString() ?? string.Empty;
                txt.Text = value;
            }
            else
            {
                txt.Text = string.Empty;
            }
        }
        private void comboBox1_ChonCscdThiDuaTapTheNamCu_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (comboBox1_ChonCscdThiDuaTapTheNamCu.SelectedItem is NamThiDuaTapTheItem itemChon)
            {
                TaiDuLieuVaoDataGridView(itemChon.DuongDanCSDL);
            }
        }
        private void comboBox1_ChonThang_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // 1. Cập nhật nội dung Label hiển thị
            CapNhatLabelThiDuaThang();
            // 2. Xóa sạch dữ liệu phân loại cũ
            comboBox_ChonPhanLoai.Items.Clear();
            if (comboBox1_ChonThang.SelectedItem == null)
                return;
            string thangChon = comboBox1_ChonThang.SelectedItem.ToString() ?? string.Empty;
            // Thêm tùy chọn rỗng lên đầu tiên
            comboBox_ChonPhanLoai.Items.Add(string.Empty);
            // Nếu chọn mốc "Tổng kết năm" -> Load 5 Danh hiệu thi đua năm
            if (thangChon.Equals("Tổng kết năm", StringComparison.OrdinalIgnoreCase))
            {
                comboBox_ChonPhanLoai.Items.AddRange(new object[]
                {
            Form23_ThongKeThiDuaTapThe.DanhHieu_Loai1, // "ĐVQT"
            Form23_ThongKeThiDuaTapThe.DanhHieu_Loai2, // "ĐVTT"
            Form23_ThongKeThiDuaTapThe.DanhHieu_Loai3, // "HTNV"
            Form23_ThongKeThiDuaTapThe.DanhHieu_Loai4, // "KHTNV"
            Module_HeThong.PL_KHONG_PL                 // "Không PL"
                });
            }
            // Ngược lại các tháng thông thường -> Load 5 Mức phân loại tháng
            else
            {
                comboBox_ChonPhanLoai.Items.AddRange(new object[]
                {
            Module_HeThong.Loai_1,
            Module_HeThong.Loai_2,
            Module_HeThong.Loai_3,
            Module_HeThong.Loai_4,
            Module_HeThong.PL_KHONG_PL
                });
            }
            // Mặc định chọn giá trị rỗng (index = 0)
            if (comboBox_ChonPhanLoai.Items.Count > 0)
            {
                comboBox_ChonPhanLoai.SelectedIndex = 0;
            }
        }
        private bool KiemTraCoBangThongKeTapThe(string duongDanCSDLCu)
        {
            try
            {
                using var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(duongDanCSDLCu));
                cn.Open();
                using var cmd = cn.CreateCommand();
                cmd.CommandText = @"
                    SELECT COUNT(*) 
                    FROM sqlite_master 
                    WHERE type = 'table' AND name = 'ThongKe_PhanLoaiTapThe_CBCS';";
                long count = (long)(cmd.ExecuteScalar() ?? 0);
                return count > 0;
            }
            catch
            {
                return false;
            }
        }
        private void kryptonButton_CapNhatThiDuaTapTheNamCu_Click(object? sender, EventArgs e)
        {
            if (comboBox1_ChonCscdThiDuaTapTheNamCu.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn cơ sở dữ liệu năm cũ trước khi cập nhật!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (Form24_XacMinhAdmin frmXacMinh = new Form24_XacMinhAdmin())
            {
                frmXacMinh.TopMost = true;
                frmXacMinh.StartPosition = FormStartPosition.CenterScreen;
                if (frmXacMinh.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
            }
            LuuDuLieuLichSu();
        }
        private async void LuuDuLieuLichSu()
        {
            // 1. Kiểm tra ComboBox CSDL Năm cũ
            if (comboBox1_ChonCscdThiDuaTapTheNamCu.SelectedItem is not NamThiDuaTapTheItem itemChon)
            {
                MessageBox.Show("Vui lòng chọn cơ sở dữ liệu năm cũ trước khi cập nhật!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // 2. Kiểm tra ComboBox Tháng
            string tenCot = LayTenCotTheoThang();
            if (string.IsNullOrEmpty(tenCot))
            {
                MessageBox.Show("Vui lòng chọn Tháng cần cập nhật!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // 3. Kiểm tra ComboBox Phân loại
            if (comboBox_ChonPhanLoai.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn Loại phân loại thi đua!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string giaTriLoai = comboBox_ChonPhanLoai.SelectedItem.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(giaTriLoai))
            {
                MessageBox.Show("Giá trị phân loại không hợp lệ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                string duongDanCSDLCu = itemChon.DuongDanCSDL;
                using var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(duongDanCSDLCu));
                cn.Open();
                using var tran = cn.BeginTransaction();
                // 🌟 Đảm bảo dòng ID = 1 luôn tồn tại trước khi UPDATE
                using (var cmdExist = cn.CreateCommand())
                {
                    cmdExist.Transaction = tran;
                    cmdExist.CommandText = @"INSERT OR IGNORE INTO ""ThongKe_PhanLoaiTapThe_CBCS"" (ID) VALUES (1);";
                    cmdExist.ExecuteNonQuery();
                }
                // Mã hóa dữ liệu phân loại bằng AES trước khi lưu
                string giaTriMaHoa = Module_BaoMatAES.MaHoa(giaTriLoai);
                // 🌟 UPDATE duy nhất cột được chọn từ comboBox1_ChonThang
                using (var cmdUpdate = cn.CreateCommand())
                {
                    cmdUpdate.Transaction = tran;
                    cmdUpdate.CommandText = $"UPDATE \"ThongKe_PhanLoaiTapThe_CBCS\" SET \"{tenCot}\" = @Val WHERE ID = 1;";
                    cmdUpdate.Parameters.AddWithValue("@Val", giaTriMaHoa);
                    cmdUpdate.ExecuteNonQuery();
                }
                tran.Commit();
                // Nạp lại dữ liệu lên DataGridView (hoặc UI)
                TaiDuLieuVaoDataGridView(duongDanCSDLCu);
                Module_NhatKy.GhiNhatKy(
                    Module_TaiKhoan.TenTaiKhoan_RAM,
                    $"Cập nhật dữ liệu thi đua tập thể [{comboBox1_ChonThang.Text}: {giaTriLoai}] năm {itemChon.TenHienThi} thành công!",
                    DateTime.Now.ToString(Module_HeThong.DinhDang_NgayGio)
                );
                //MessageBox.Show($"Cập nhật kết quả [{giaTriLoai}] cho [{comboBox1_ChonThang.Text}] năm {itemChon.TenHienThi} thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // 🌟 Cập nhật thông báo lên ToolStripStatusLabel3 -> Delay 200ms -> Đếm số dòng DataGrid
                string msg = $"Cập nhật dữ liệu thi đua {comboBox1_ChonThang.Text} của năm {itemChon.TenHienThi} thành công!";
                await HienThiThongBaoTamThoiAsync(msg, 200);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Có lỗi xảy ra khi lưu CSDL:\n" + ex.Message, "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// Cập nhật hiển thị số lượng bản ghi/dòng dữ liệu trên DataGridView
        /// </summary>
        private void CapNhatSoLuongDongDataGrid()
        {
            int soDong = kryptonDataGridView1.Rows.Cast<DataGridViewRow>()
                            .Count(r => !r.IsNewRow); // Loại trừ dòng thêm mới (nếu có)
            toolStripStatusLabel3_ThongBao.Text = $"Tổng số dữ liệu: {soDong} bản ghi";
        }
        /// <summary>
        /// Hiển thị thông báo tạm thời, sau đó chờ 200ms rồi khôi phục lại số dòng trên DataGridView
        /// </summary>
        private async Task HienThiThongBaoTamThoiAsync(string noiDungThongBao, int miliGiayDelay = 200)
        {
            // 1. Hiển thị thông báo tác vụ thành công
            toolStripStatusLabel3_ThongBao.Text = noiDungThongBao;
            // 2. Tạm dừng bất đồng bộ mà không gây treo/đơ giao diện (UI)
            await Task.Delay(miliGiayDelay);
            // 3. Gọi lại hàm đếm dòng dữ liệu
            CapNhatSoLuongDongDataGrid();
        }
        /// <summary>
        /// Hàm trợ giúp ánh xạ từ ComboBox Tháng sang tên cột trong CSDL SQLite
        /// </summary>
        private string LayTenCotTheoThang()
        {
            if (comboBox1_ChonThang.SelectedItem == null)
                return string.Empty;
            string thangChon = comboBox1_ChonThang.SelectedItem.ToString()!;
            return thangChon switch
            {
                "Tháng 12 (Năm cũ)" => "Thang_12_Nam_Cu",
                "Tháng 1" => "Thang_1",
                "Tháng 2" => "Thang_2",
                "Tháng 3" => "Thang_3",
                "Tháng 4" => "Thang_4",
                "Tháng 5" => "Thang_5",
                "6 Tháng đầu năm" => "Sau_Thang_Dau_Nam",
                "Tháng 6" => "Thang_6",
                "Tháng 7" => "Thang_7",
                "Tháng 8" => "Thang_8",
                "Tháng 9" => "Thang_9",
                "Tháng 10" => "Thang_10",
                "Tháng 11" => "Thang_11",
                "Tổng kết năm" => "TongKet_Nam",
                _ => string.Empty
            };
        }
        private void TaiDuLieuVaoDataGridView(string duongDanCSDLCu)
        {
            if (string.IsNullOrEmpty(duongDanCSDLCu) || !File.Exists(duongDanCSDLCu))
            {
                kryptonDataGridView1.DataSource = null;
                XoaTrangCacTextBox();
                SetDataGridViewVisible(false);
                return;
            }
            DataTable dt = new DataTable();
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("Thang_12_Nam_Cu", typeof(string));
            dt.Columns.Add("Thang_1", typeof(string));
            dt.Columns.Add("Thang_2", typeof(string));
            dt.Columns.Add("Thang_3", typeof(string));
            dt.Columns.Add("Thang_4", typeof(string));
            dt.Columns.Add("Thang_5", typeof(string));
            dt.Columns.Add("Sau_Thang_Dau_Nam", typeof(string));
            dt.Columns.Add("Thang_6", typeof(string));
            dt.Columns.Add("Thang_7", typeof(string));
            dt.Columns.Add("Thang_8", typeof(string));
            dt.Columns.Add("Thang_9", typeof(string));
            dt.Columns.Add("Thang_10", typeof(string));
            dt.Columns.Add("Thang_11", typeof(string));
            dt.Columns.Add("TongKet_Nam", typeof(string));
            try
            {
                using var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(duongDanCSDLCu));
                cn.Open();
                using var cmd = cn.CreateCommand();
                cmd.CommandText = @"
                    SELECT ID, Thang_12_Nam_Cu, Thang_1, Thang_2, Thang_3, Thang_4, Thang_5, 
                           Sau_Thang_Dau_Nam, Thang_6, Thang_7, Thang_8, Thang_9, Thang_10, Thang_11, TongKet_Nam 
                    FROM ThongKe_PhanLoaiTapThe_CBCS 
                    LIMIT 1;";
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    DataRow row = dt.NewRow();
                    row["ID"] = reader["ID"] != DBNull.Value ? Convert.ToInt32(reader["ID"]) : 1;
                    for (int i = 1; i < reader.FieldCount; i++)
                    {
                        string colName = reader.GetName(i);
                        object val = reader.GetValue(i);
                        if (val != DBNull.Value && !string.IsNullOrWhiteSpace(val.ToString()))
                        {
                            string chuoiGoc = val.ToString()!.Trim();
                            try
                            {
                                string chuoiGiaiMa = Module_BaoMatAES.GiaiMa(chuoiGoc);
                                row[colName] = string.IsNullOrEmpty(chuoiGiaiMa) ? chuoiGoc : chuoiGiaiMa;
                            }
                            catch
                            {
                                row[colName] = chuoiGoc;
                            }
                        }
                        else
                        {
                            row[colName] = string.Empty;
                        }
                    }
                    dt.Rows.Add(row);
                }
                kryptonDataGridView1.DataSource = dt;
                SetDataGridViewVisible(dt.Rows.Count > 0);
                DoiTenCotChuyenNghiep(kryptonDataGridView1);
                if (kryptonDataGridView1.Rows.Count > 0)
                {
                    kryptonDataGridView1_CellClick(kryptonDataGridView1, new DataGridViewCellEventArgs(0, 0));
                }
                else
                {
                    XoaTrangCacTextBox();
                }
            }
            catch (Exception ex)
            {
                HienThiTrangThaiKhongCoDuLieu();
                MessageBox.Show($"Lỗi khi tải dữ liệu CSDL lên bảng: {ex.Message}", "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void DinhDangGiaoDienDataGridTongKetThang(KryptonDataGridView dgv)
        {
            if (dgv == null) return;
            typeof(DataGridView).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(dgv, true, null);
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToResizeRows = false;
            dgv.AllowUserToOrderColumns = false;
            dgv.RowHeadersVisible = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.ReadOnly = true;
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.GridStyles.Style = DataGridViewStyle.List;
            dgv.RowTemplate.Height = 36;
            dgv.ColumnHeadersHeight = 50;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            string fontName = Module_HeThong.TenFontHeThong ?? Module_HeThong.TenFontHeThong;
            Font fontBold9 = new Font(fontName, 9.5F, FontStyle.Bold);
            Font fontRegular9 = new Font(fontName, 9.5F, FontStyle.Regular);
            dgv.StateCommon.HeaderColumn.Content.Font = fontBold9;
            dgv.StateCommon.DataCell.Content.Font = fontRegular9;
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.StateCommon.DataCell.Border.Color1 = Color.FromArgb(224, 224, 224);
            dgv.StateCommon.DataCell.Border.DrawBorders = PaletteDrawBorders.All;
            dgv.StateCommon.DataCell.Border.Width = 1;
            dgv.StateSelected.DataCell.Back.Color1 = Color.FromArgb(232, 244, 253);
            dgv.StateSelected.DataCell.Content.Color1 = Color.FromArgb(0, 102, 204);
            if (dgv.Columns.Count == 0) return;
            foreach (DataGridViewColumn col in dgv.Columns)
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            if (dgv.Columns["ID"] != null)
                dgv.Columns["ID"].Visible = false;
        }
        private void DoiTenCotChuyenNghiep(KryptonDataGridView dgv)
        {
            if (dgv.Columns.Count == 0) return;
            dgv.SuspendLayout();
            try
            {
                if (dgv.Columns.Contains("ID"))
                    dgv.Columns["ID"].Visible = false;
                var mapTenCot = new Dictionary<string, string>
                {
                    { "Thang_12_Nam_Cu", "Tháng 12 (Năm cũ)" },
                    { "Thang_1", "Tháng 1" },
                    { "Thang_2", "Tháng 2" },
                    { "Thang_3", "Tháng 3" },
                    { "Thang_4", "Tháng 4" },
                    { "Thang_5", "Tháng 5" },
                    { "Sau_Thang_Dau_Nam", "6 Tháng đầu năm" },
                    { "Thang_6", "Tháng 6" },
                    { "Thang_7", "Tháng 7" },
                    { "Thang_8", "Tháng 8" },
                    { "Thang_9", "Tháng 9" },
                    { "Thang_10", "Tháng 10" },
                    { "Thang_11", "Tháng 11" },
                    { "TongKet_Nam", "Tổng kết năm" }
                };
                foreach (var kvp in mapTenCot)
                {
                    if (dgv.Columns.Contains(kvp.Key))
                    {
                        var col = dgv.Columns[kvp.Key];
                        col.HeaderText = kvp.Value;
                        col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        col.SortMode = DataGridViewColumnSortMode.NotSortable;
                    }
                }
                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
            finally
            {
                dgv.ResumeLayout();
            }
        }
        private async Task TaiDanhSachCoSoDuLieuNamCuAsync()
        {
            comboBox1_ChonCscdThiDuaTapTheNamCu.BeginUpdate();
            comboBox1_ChonCscdThiDuaTapTheNamCu.Items.Clear();
            try
            {
                if (Directory.Exists(_thuMucLichSuPath))
                {
                    List<NamThiDuaTapTheItem> danhSachHople = await Task.Run(() =>
                    {
                        Module_HoTroLuuDataTheoNamCu.DonRacThuMucLichSu();   // dọn tệp năm cũ đã trống hết dữ liệu
                        var dsResult = new List<NamThiDuaTapTheItem>();
                        string[] cacFile = Directory.GetFiles(_thuMucLichSuPath, "ThiDua_CBCS_Nam*.db");
                        foreach (string file in cacFile)
                        {
                            if (KiemTraCoBangThongKeTapThe(file))
                            {
                                string tenFile = Path.GetFileName(file);
                                string tenNam = tenFile.Replace("ThiDua_CBCS_Nam", "").Replace(".db", "");
                                dsResult.Add(new NamThiDuaTapTheItem
                                {
                                    TenHienThi = tenNam,
                                    TenFileGoc = tenFile,
                                    DuongDanCSDL = file
                                });
                            }
                        }
                        return dsResult.OrderBy(x => x.TenHienThi).ToList();
                    });
                    foreach (var item in danhSachHople)
                    {
                        comboBox1_ChonCscdThiDuaTapTheNamCu.Items.Add(item);
                    }
                    if (comboBox1_ChonCscdThiDuaTapTheNamCu.Items.Count > 0)
                    {
                        comboBox1_ChonCscdThiDuaTapTheNamCu.SelectedIndex = 0;
                    }
                }
                else
                {
                    Directory.CreateDirectory(_thuMucLichSuPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi nạp danh sách CSDL năm cũ]: {ex.Message}");
            }
            finally
            {
                comboBox1_ChonCscdThiDuaTapTheNamCu.EndUpdate();
            }
            // Không tìm thấy tệp CSDL nào (hoặc thư mục chưa tồn tại) -> ẩn lưới và hiện thông báo
            if (comboBox1_ChonCscdThiDuaTapTheNamCu.Items.Count == 0)
            {
                HienThiTrangThaiKhongCoDuLieu();
            }
        }
        private async void kryptonButton1_LamMoi_Click(object? sender, EventArgs e)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                KhoiTaoDanhSachTextBox();
                await TaiDanhSachCoSoDuLieuNamCuAsync();
                if (comboBox1_ChonCscdThiDuaTapTheNamCu.SelectedItem is NamThiDuaTapTheItem itemChon)
                {
                    TaiDuLieuVaoDataGridView(itemChon.DuongDanCSDL);
                }
                DinhDangGiaoDienDataGridTongKetThang(kryptonDataGridView1);
                // 🌟 Thông báo làm mới thành công -> Delay 200ms -> Hiện lại số dòng DataGrid
                await HienThiThongBaoTamThoiAsync("Làm mới danh sách dữ liệu thành công!", 200);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải lại dữ liệu: {ex.Message}", "Thông báo lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }
        private async void kryptonButton_XoaThiDuaTapThe_Click(object? sender, EventArgs e)
        {
            if (comboBox1_ChonCscdThiDuaTapTheNamCu.SelectedItem is not NamThiDuaTapTheItem itemChon)
            {
                MessageBox.Show("Vui lòng chọn cơ sở dữ liệu năm cũ trước khi thực hiện xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var dialogResult = MessageBox.Show(
                $"CẢNH BÁO: Thao tác này sẽ XÓA HẲN BẢNG 'ThongKe_PhanLoaiTapThe_CBCS' khỏi CSDL năm {itemChon.TenHienThi}!\n\nBạn có chắc chắn muốn xóa không?",
                "Xác nhận xóa BẢNG CSDL",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );
            if (dialogResult != DialogResult.Yes) return;
            using (Form24_XacMinhAdmin frmXacMinh = new Form24_XacMinhAdmin())
            {
                frmXacMinh.TopMost = true;
                frmXacMinh.StartPosition = FormStartPosition.CenterScreen;
                if (frmXacMinh.ShowDialog() != DialogResult.OK) return;
            }
            try
            {
                string duongDanCSDLCu = itemChon.DuongDanCSDL;
                using (var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(duongDanCSDLCu)))
                {
                    cn.Open();
                    using (var cmd = cn.CreateCommand())
                    {
                        cmd.CommandText = @"DROP TABLE IF EXISTS ""ThongKe_PhanLoaiTapThe_CBCS"";";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "VACUUM;";
                        cmd.ExecuteNonQuery();
                    }
                }
                Module_NhatKy.GhiNhatKy(
                    Module_TaiKhoan.TenTaiKhoan_RAM,
                    $"Xóa bảng thi đua tập thể năm {itemChon.TenHienThi} thành công!",
                    DateTime.Now.ToString(Module_HeThong.DinhDang_NgayGio)
                );
                // Dọn dẹp DataGridView & ẩn vùng lưới trống
                kryptonDataGridView1.DataSource = null;
                XoaTrangCacTextBox();
                SetDataGridViewVisible(false);
                // Bảng thi đua cá nhân (ThiDuaThang) của năm này cũng trống => tệp chỉ còn là rác: xóa hẳn khỏi thư mục lịch sử.
                // Danh sách năm quét lại (năm vừa xóa bảng tập thể luôn biến khỏi combo vì không còn bảng ThongKe_PhanLoaiTapThe_CBCS).
                Module_HoTroLuuDataTheoNamCu.XoaTepLichSuNeuRong(duongDanCSDLCu);
                await TaiDanhSachCoSoDuLieuNamCuAsync();
                // 🌟 Thông báo xóa thành công -> Delay 200ms -> Đếm lại số dòng DataGrid (bằng 0)
                await HienThiThongBaoTamThoiAsync($"Đã xóa toàn bộ dữ liệu bảng thi đua tập thể năm {itemChon.TenHienThi} thành công!", 200);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Có lỗi xảy ra khi xóa dữ liệu CSDL:\n{ex.Message}", "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void XoaTrangCacTextBox()
        {
            if (_danhSachTextBoxThang != null)
            {
                foreach (var txt in _danhSachTextBoxThang)
                {
                    txt.Clear();
                }
            }
        }
        private void KhoiTaoComboBoxThangVaPhanLoai()
        {
            Font fontThuong = new Font(this.Font.FontFamily, 9.5F, FontStyle.Regular);
            this.comboBox1_ChonThang.Font = fontThuong;
            this.comboBox_ChonPhanLoai.Font = fontThuong;
            // Đăng ký sự kiện chuyển đổi tháng
            this.comboBox1_ChonThang.SelectedIndexChanged -= comboBox1_ChonThang_SelectedIndexChanged;
            this.comboBox1_ChonThang.SelectedIndexChanged += comboBox1_ChonThang_SelectedIndexChanged;
            // Nạp danh sách 14 mốc thời gian
            NapDanhSachThang();
            // Tự động chọn Tháng hiện tại
            ChonThangHienTai();
        }
        private void NapDanhSachThang()
        {
            comboBox1_ChonThang.Items.Clear();
            comboBox1_ChonThang.Items.AddRange(new object[]
            {
        "Tháng 12 (Năm cũ)",
        "Tháng 1",
        "Tháng 2",
        "Tháng 3",
        "Tháng 4",
        "Tháng 5",
        "6 Tháng đầu năm",
        "Tháng 6",
        "Tháng 7",
        "Tháng 8",
        "Tháng 9",
        "Tháng 10",
        "Tháng 11",
        "Tổng kết năm"
            });
        }
        /// <summary>
        /// Tự động chọn đúng tháng hiện tại trên hệ thống
        /// </summary>
        private void ChonThangHienTai()
        {
            if (comboBox1_ChonThang.Items.Count == 0) return;
            int thangHienTai = DateTime.Now.Month;
            string tenThangTimKiem = $"Tháng {thangHienTai}";
            int index = -1;
            for (int i = 0; i < comboBox1_ChonThang.Items.Count; i++)
            {
                string itemText = comboBox1_ChonThang.Items[i]?.ToString() ?? "";
                if (itemText.Equals(tenThangTimKiem, StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }
            comboBox1_ChonThang.SelectedIndex = index >= 0 ? index : 0;
        }
        /// <summary>
        /// Cập nhật nội dung label9_ChonTenThiDuaThang linh hoạt dựa theo giá trị chọn trên comboBox1_ChonThang
        /// </summary>
        public void CapNhatLabelThiDuaThang()
        {
            if (label9_ChonTenThiDuaThang == null) return;
            if (comboBox1_ChonThang.SelectedItem?.ToString()?.Equals("Tổng kết năm", StringComparison.OrdinalIgnoreCase) == true)
            {
                label9_ChonTenThiDuaThang.Text = "Cập nhật danh hiệu thi đua tập thể:";
            }
            else
            {
                label9_ChonTenThiDuaThang.Text = "Cập nhật phân loại thi đua tập thể:";
            }
        }
        private const string ThongBaoKhongCoDuLieu = "Không tìm thấy tệp lưu trữ CSDL thi đua năm cũ";
        //private bool _isGridHidden = false;
        private Label? _lblKhongCoDuLieu;
        private Label TaoLabelKhongCoDuLieu()
        {
            var lbl = new Label
            {
                Name = "label_KhongCoDuLieu",
                Text = ThongBaoKhongCoDuLieu,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(100, 100, 100),
                BackColor = Color.Transparent,
                Visible = false
            };
            var font = new Font(Module_HeThong.TenFontHeThong ?? Module_HeThong.TenFontHeThong, 11F, FontStyle.Italic);
            lbl.Font = font;
            lbl.Disposed += (_, _) => font.Dispose();
            return lbl;
        }
        /// <summary>
        /// true  : hiện DataGridView, ẩn thông báo.
        /// false : ẩn DataGridView và hiện thông báo "Không tìm thấy tệp lưu trữ CSDL thi đua năm cũ" ngay tại vị trí lưới.
        /// </summary>
        private void SetDataGridViewVisible(bool visible)
        {
            if (kryptonDataGridView1 == null) return;
            var parent = kryptonDataGridView1.Parent;
            if (parent == null) return;
            parent.SuspendLayout();
            try
            {
                // Tạo Label 1 lần duy nhất, đặt cùng ô (cell) với DataGridView
                if (_lblKhongCoDuLieu == null || _lblKhongCoDuLieu.IsDisposed)
                {
                    _lblKhongCoDuLieu = TaoLabelKhongCoDuLieu();
                    if (parent is TableLayoutPanel tlp)
                    {
                        var pos = tlp.GetPositionFromControl(kryptonDataGridView1);
                        tlp.Controls.Add(_lblKhongCoDuLieu, pos.Column, pos.Row);
                        tlp.SetColumnSpan(_lblKhongCoDuLieu, tlp.GetColumnSpan(kryptonDataGridView1));
                        tlp.SetRowSpan(_lblKhongCoDuLieu, tlp.GetRowSpan(kryptonDataGridView1));
                    }
                    else
                    {
                        parent.Controls.Add(_lblKhongCoDuLieu);
                    }
                }
                // Luôn tắt control cũ trước rồi mới bật control mới để không bị chiếm chung 1 ô
                if (visible)
                {
                    _lblKhongCoDuLieu.Visible = false;
                    kryptonDataGridView1.Visible = true;
                }
                else
                {
                    kryptonDataGridView1.Visible = false;
                    _lblKhongCoDuLieu.Visible = true;
                    _lblKhongCoDuLieu.BringToFront();
                }
            }
            finally
            {
                parent.ResumeLayout(true);
            }
            //_isGridHidden = !visible;
        }
        /// <summary>
        /// Xóa sạch lưới + ô nhập và hiện thông báo không có dữ liệu.
        /// </summary>
        private void HienThiTrangThaiKhongCoDuLieu()
        {
            kryptonDataGridView1.DataSource = null;
            XoaTrangCacTextBox();
            SetDataGridViewVisible(false);
        }

  
        //private void ToggleDataGridView()
        //{
        //SetDataGridViewVisible(_isGridHidden);
        //}
    }
    public class NamThiDuaTapTheItem
    {
        public string TenHienThi { get; set; } = string.Empty;
        public string TenFileGoc { get; set; } = string.Empty;
        public string DuongDanCSDL { get; set; } = string.Empty;
        // Hàm này rất quan trọng để ComboBox tự động hiển thị tên thay vì hiển thị kiểu dữ liệu
        public override string ToString()
        {
            return TenHienThi;
        }
    }
}
/////TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
