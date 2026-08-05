
using ClosedXML.Excel;
using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;

namespace PhanMemThiDua2026
{
    public partial class Form48_XuatTepPdf : Form
    {
        // =========================================================================
        // KHAI BÁO WINDOWS API: GỌI THƯ MỤC VÀ CHỌN TỆP ĐƠN NHIỆM
        // =========================================================================
        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern void ILFree(IntPtr pidl);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern IntPtr ILCreateFromPathW(string pszPath);

        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, uint dwFlags);

        // =========================================================================
        // CLASS LƯU TRỮ TRẠNG THÁI ÁNH XẠ
        // =========================================================================
        public class SheetExportItem
        {
            public string OriginalSheetName { get; set; }
            public string TargetPdfName { get; set; }

            public override string ToString()
            {
                if (TargetPdfName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    return $"[{OriginalSheetName}] ➔ {TargetPdfName}";
                return $"[{OriginalSheetName}] ➔ {TargetPdfName}.pdf";
            }
        }
        private string _textGocNutXuatPdf = "Xuất tệp (*.pdf)";
        private string _tenDonVi = "";
        private string _thang = "";
        private string _nam = "";
        private Dictionary<string, string> _mapQuyDinhTenPdf;
        public Form48_XuatTepPdf()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Trình chuyển đổi tệp excel sang *.pdf";
            this.Load += Form48_XuatTepPdf_Load;
            this.ShowInTaskbar = false;

            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.AcceptButton = kryptonButton_XuatTepPdf;

            // Đăng ký sự kiện
            kryptonButton1_ChonDuongDanTepExcel.Click -= KryptonButton1_ChonDuongDanTepExcel_Click;
            kryptonButton1_ChonDuongDanTepExcel.Click += KryptonButton1_ChonDuongDanTepExcel_Click;

            kryptonButton1_ChonDuongDanLuuTepPdf.Click -= kryptonButton1_ChonDuongDanLuuTepPdf_Click;
            kryptonButton1_ChonDuongDanLuuTepPdf.Click += kryptonButton1_ChonDuongDanLuuTepPdf_Click;

            kryptonButton_XuatTepPdf.Click -= kryptonButton_XuatTepPdf_Click;
            kryptonButton_XuatTepPdf.Click += kryptonButton_XuatTepPdf_Click;

            // Tùy chỉnh màu sắc CheckedListBox (Xanh/Đỏ) và tính năng 1 chạm
            checkedListBox1_LietKeTenCacSheet.DrawMode = DrawMode.OwnerDrawFixed;
            checkedListBox1_LietKeTenCacSheet.DrawItem += CheckedListBox1_DrawItem;
            checkedListBox1_LietKeTenCacSheet.ItemCheck += CheckedListBox1_ItemCheck;
            checkedListBox1_LietKeTenCacSheet.SelectedIndexChanged += CheckedListBox1_SelectedIndexChanged;
        }
        private void Form48_XuatTepPdf_Load(object sender, EventArgs e)
        {
            label_DuongDanExcel.Text = "Chưa chọn tệp excel";
            label_DuongDanExcel.ForeColor = Color.Red;

            label_DuongDanPdf.Text = "Chưa chọn thư mục lưu tệp *.pdf";
            label_DuongDanPdf.ForeColor = Color.Red;

            if (toolStripProgressBar1_TienTrinhXuatTep != null)
            {
                toolStripProgressBar1_TienTrinhXuatTep.AutoSize = false;
                toolStripProgressBar1_TienTrinhXuatTep.Size = new Size(200, 12);
                toolStripProgressBar1_TienTrinhXuatTep.Margin = new Padding(1, 5, 1, 5);
                toolStripProgressBar1_TienTrinhXuatTep.Visible = false;
            }

            TaiThongTinDonViVaThoiGian();
            ThietLapDictonaryAnhXa();
            CapNhatStatusStrip(0, 0, 0);
            InitToolTips();
        }
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Gợi ý thao tác";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            toolTip1.InitialDelay = 300;
            toolTip1.AutoPopDelay = 2500;
            toolTip1.ReshowDelay = 100;
            toolTip1.ShowAlways = true;

            var tips = new Dictionary<Control, string>
            {
                { kryptonButton1_ChonDuongDanTepExcel, "Chọn đường dẫn tệp Excel" },
                { kryptonButton1_ChonDuongDanLuuTepPdf, "Chọn đường dẫn lưu tệp *.pdf" },
                { kryptonButton_XuatTepPdf, "Xuất tệp *.pdf" }
            };

            foreach (var tip in tips)
            {
                if (tip.Key != null && !tip.Key.IsDisposed) toolTip1.SetToolTip(tip.Key, tip.Value);
            }
        }
        private void TaiThongTinDonViVaThoiGian()
        {
            _thang = Module_XuatPhanLoai.LayThangHeThong();

            // Chuẩn hóa định dạng tháng theo quy định:
            // 1 -> 01, 2 -> 02, các tháng còn lại giữ nguyên.
            if (int.TryParse(_thang, out int thang))
            {
                if (thang == 1 || thang == 2)
                {
                    _thang = thang.ToString("00");
                }
                else
                {
                    _thang = thang.ToString();
                }
            }

            _nam = Module_XuatPhanLoai.LayNamHeThong();

            try
            {
                using var conn = new SqliteConnection($"Data Source={Module_DanduongGPS.DuongDanCSDL2}");
                conn.Open();

                using var cmd = new SqliteCommand("SELECT TenTieuDoan FROM ThongTin WHERE ID = 1", conn);
                var result = cmd.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                {
                    string td = BaoMatAES.GiaiMa(result.ToString() ?? "").Trim();

                    if (!string.IsNullOrEmpty(td))
                        _tenDonVi = td;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi đọc thông tin đơn vị: " + ex.Message);
                _tenDonVi = "Đơn vị";
            }
        }
        private void ThietLapDictonaryAnhXa()
        {
            string tenDonViHienThi = _tenDonVi;
            if (!string.IsNullOrWhiteSpace(tenDonViHienThi))
            {
                string lowerCase = tenDonViHienThi.ToLower();
                tenDonViHienThi = char.ToUpper(lowerCase[0]) + lowerCase.Substring(1);
            }

            string baseString = $"của {tenDonViHienThi} tháng {_thang}-{_nam}.pdf";

            _mapQuyDinhTenPdf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "LOAI_1", $"Danh sách đề nghị loại 1 phong trào thi đua \"Vì ANTQ\" {baseString}" },
                { "LOAI_2", $"Danh sách đề nghị loại 2 phong trào thi đua \"Vì ANTQ\" {baseString}" },
                { "LOAI_3", $"Danh sách đề nghị loại 3 phong trào thi đua \"Vì ANTQ\" {baseString}" },
                { "LOAI_4", $"Danh sách đề nghị loại 4 phong trào thi đua \"Vì ANTQ\" {baseString}" },
                { "KHONG_PL", $"Danh sách đề nghị không phân loại phong trào thi đua \"Vì ANTQ\" {baseString}" },
                { "BAO CAO TONG HOP", $"Báo cáo tổng hợp phong trào thi đua \"Vì ANTQ\" {baseString}" },
                { "BC_BANHAT", $"Báo cáo tổng hợp đề nghị biểu dương gương tiêu biểu phong trào thi đua \"Ba nhất\" {baseString}" },
                { "DS_BANHAT", $"Danh sách đề nghị biểu dương gương tiêu biểu phong trào thi đua \"Ba nhất\" {baseString}" }
            };
        }
        private void KryptonButton1_ChonDuongDanTepExcel_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Chọn tệp Excel nguồn",
                Filter = "Excel Files|*.xlsx;*.xlsm",
                CheckFileExists = true
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string selectedFile = ofd.FileName;

                // 🌟 BỘ LỌC KIỂM TRA TỆP NGUỒN AN TOÀN
                try
                {
                    using (var wbCheck = new XLWorkbook(selectedFile))
                    {
                        // Kiểm tra xem tệp có chứa ít nhất một sheet hợp lệ hoặc có phải tệp rác không
                        if (!wbCheck.Worksheets.Any())
                        {
                            MessageBox.Show("Tệp Excel này không có sheet dữ liệu nào hợp lệ!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Tệp Excel đang mở hoặc không đúng định dạng cấu trúc hệ thống:\n" + ex.Message, "Lỗi đọc tệp", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                label_DuongDanExcel.Text = selectedFile;
                label_DuongDanExcel.ForeColor = Color.DarkGreen;
                PhanTichVaAnhXaSheetTuExcel(selectedFile);
            }
        }
        private void PhanTichVaAnhXaSheetTuExcel(string excelPath)
        {
            try
            {
                checkedListBox1_LietKeTenCacSheet.SelectedIndexChanged -= CheckedListBox1_SelectedIndexChanged;
                checkedListBox1_LietKeTenCacSheet.Items.Clear();

                using (var wb = new XLWorkbook(excelPath))
                {
                    foreach (var ws in wb.Worksheets)
                    {
                        string sheetName = ws.Name;
                        if (sheetName.Equals("THONG_TIN", StringComparison.OrdinalIgnoreCase)) continue;

                        string targetName = _mapQuyDinhTenPdf.TryGetValue(sheetName, out string mappedName) ? mappedName : $"{sheetName}.pdf";

                        var item = new SheetExportItem { OriginalSheetName = sheetName, TargetPdfName = targetName };
                        checkedListBox1_LietKeTenCacSheet.Items.Add(item, true);
                    }
                }
                CapNhatStatusStrip(checkedListBox1_LietKeTenCacSheet.Items.Count, checkedListBox1_LietKeTenCacSheet.CheckedItems.Count, 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể đọc cấu trúc tệp Excel.\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                checkedListBox1_LietKeTenCacSheet.SelectedIndexChanged += CheckedListBox1_SelectedIndexChanged;
            }
        }
        private void kryptonButton1_ChonDuongDanLuuTepPdf_Click(object sender, EventArgs e)
        {
            // 1. Mặc định khởi điểm là Desktop
            string thuMucMacDinh = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            // 2. KẾT NỐI CSDL (Sự "thông minh" của nút: Tự động nhớ đường dẫn cũ)
            try
            {
                using var conn = new SqliteConnection($"Data Source={Module_DanduongGPS.DuongDanCSDL2}");
                conn.Open();
                using var cmd = new SqliteCommand("SELECT ChonDuongDanXuatTep FROM ThongTin WHERE ID = 1", conn);
                var result = cmd.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                {
                    string giaiMaPath = BaoMatAES.GiaiMa(result.ToString() ?? "").Trim();
                    // Nếu đường dẫn trong CSDL hợp lệ và tồn tại thực tế trên máy tính, lấy nó làm mặc định
                    if (!string.IsNullOrWhiteSpace(giaiMaPath) && Directory.Exists(giaiMaPath))
                    {
                        thuMucMacDinh = giaiMaPath;
                    }
                }
            }
            catch { }

            // 3. MỞ HỘP THOẠI CHỌN THƯ MỤC
            using var fbd = new FolderBrowserDialog
            {
                Description = "Chọn thư mục lưu các tệp PDF xuất ra",
                UseDescriptionForTitle = true, // Đưa dòng Description lên làm Tiêu đề cửa sổ (đẹp hơn)
                ShowNewFolderButton = true,

                // 🌟 BÍ QUYẾT NẰM Ở 2 DÒNG NÀY: 
                // Ép Windows mở TRỰC TIẾP VÀO BÊN TRONG thư mục, hiện sẵn tên thư mục ở ô "Folder:"
                InitialDirectory = thuMucMacDinh,
                SelectedPath = thuMucMacDinh
            };

            // 4. Nếu người dùng bấm "Select Folder" (OK)
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                label_DuongDanPdf.Text = fbd.SelectedPath;
                label_DuongDanPdf.ForeColor = Color.DarkBlue;
            }
        }
        private void CheckedListBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            var clb = (CheckedListBox)sender;
            e.DrawBackground();

            bool isChecked = clb.GetItemChecked(e.Index);
            Color textColor = isChecked ? Color.DarkGreen : Color.Red;

            ButtonState state = isChecked ? ButtonState.Checked : ButtonState.Normal;
            ControlPaint.DrawCheckBox(e.Graphics, e.Bounds.Left + 2, e.Bounds.Top + 2, 14, 14, state);

            using (Brush textBrush = new SolidBrush(textColor))
            {
                e.Graphics.DrawString(clb.Items[e.Index].ToString(), e.Font, textBrush, e.Bounds.Left + 18, e.Bounds.Top + 1);
            }
            e.DrawFocusRectangle();
        }
        private void CheckedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            int checkedCount = checkedListBox1_LietKeTenCacSheet.CheckedItems.Count;
            if (e.NewValue == CheckState.Checked) checkedCount++;
            if (e.NewValue == CheckState.Unchecked) checkedCount--;

            CapNhatStatusStrip(checkedListBox1_LietKeTenCacSheet.Items.Count, checkedCount, 0);
            this.BeginInvoke(new Action(() => checkedListBox1_LietKeTenCacSheet.Invalidate()));
        }
        private void CheckedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = checkedListBox1_LietKeTenCacSheet.SelectedIndex;
            if (index == -1) return;

            bool isChecked = checkedListBox1_LietKeTenCacSheet.GetItemChecked(index);
            checkedListBox1_LietKeTenCacSheet.SetItemChecked(index, !isChecked);
            checkedListBox1_LietKeTenCacSheet.ClearSelected();
        }
        private void CapNhatStatusStrip(int tongSheet, int sheetDaChon, int fileDaTao)
        {
            if (toolStripStatusLabel1_TongCongTepPdf == null) return;

            if (tongSheet == 0)
            {
                toolStripStatusLabel1_TongCongTepPdf.Text = "Chưa nạp dữ liệu từ tệp Excel.";
                return;
            }

            List<string> dsThongTin = new List<string> { $"Tổng cộng: {tongSheet} sheet đã tìm thấy" };
            if (sheetDaChon > 0) dsThongTin.Add($"Đã chọn {sheetDaChon} sheet để tạo PDF");
            if (fileDaTao > 0) dsThongTin.Add($"Đã tạo thành công {fileDaTao} file PDF");

            toolStripStatusLabel1_TongCongTepPdf.Text = string.Join(" | ", dsThongTin);
        }
        // =========================================================================
        // TRUNG TÂM ĐIỀU PHỐI ĐỘNG CƠ (DUAL-ENGINE ORCHESTRATOR)
        // =========================================================================
        private async void kryptonButton_XuatTepPdf_Click(object sender, EventArgs e)
        {
            string excelPath = label_DuongDanExcel.Text;
            string pdfFolderPath = label_DuongDanPdf.Text;

            if (!Directory.Exists(pdfFolderPath))
            {
                MessageBox.Show(
                    "Bạn chưa chọn thư mục lưu tệp *.pdf!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                kryptonButton1_ChonDuongDanLuuTepPdf.PerformClick();
                return;
            }
            if (!Directory.Exists(pdfFolderPath))
            {
                MessageBox.Show("Bạn chưa chọn thư mục đích để lưu tệp *.pdf!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var itemsToExport = checkedListBox1_LietKeTenCacSheet.CheckedItems.Cast<SheetExportItem>().ToList();
            if (itemsToExport.Count == 0)
            {
                MessageBox.Show("Vui lòng tích chọn ít nhất 1 Sheet để xuất tệp *.pdf", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_textGocNutXuatPdf == null) _textGocNutXuatPdf = kryptonButton_XuatTepPdf.Text;
            kryptonButton_XuatTepPdf.Enabled = false;
            kryptonButton_XuatTepPdf.Text = "Đang xử lý...";

            toolStripProgressBar1_TienTrinhXuatTep.Visible = true;
            toolStripProgressBar1_TienTrinhXuatTep.Minimum = 0;
            toolStripProgressBar1_TienTrinhXuatTep.Maximum = itemsToExport.Count;
            toolStripProgressBar1_TienTrinhXuatTep.Value = 0;

            try
            {
                await Task.Delay(50);
                int soLuongXuatThanhCong = 0;
                string filePdfCuoiCung = "";

                var dataThayThe = GetReplacementDataFromDB();

                Type excelType = Type.GetTypeFromProgID("Excel.Application");

                if (excelType != null)
                {
                    try
                    {
                        var result = await Task.Run(() => ExportUsingExcel(excelPath, pdfFolderPath, itemsToExport, excelType, dataThayThe));
                        soLuongXuatThanhCong = result.SuccessCount;
                        filePdfCuoiCung = result.LastFileCreated;
                    }
                    catch (Exception exInterop)
                    {
                        Debug.WriteLine($"Excel Interop thất bại ({exInterop.Message}). Kích hoạt Kế hoạch B...");
                        excelType = null;
                    }
                }

                if (excelType == null)
                {
                    string librePath = GetEmbeddedLibreOfficePath();
                    if (string.IsNullOrEmpty(librePath))
                    {
                        throw new Exception("Hệ thống không tìm thấy Microsoft Excel và cũng không tìm thấy Động cơ kết xuất nội trú. Vui lòng kiểm tra lại cấu trúc phần mềm.");
                    }

                    var result = await Task.Run(() => ExportUsingLibreOffice(excelPath, pdfFolderPath, itemsToExport, librePath, dataThayThe));
                    soLuongXuatThanhCong = result.SuccessCount;
                    filePdfCuoiCung = result.LastFileCreated;
                }

                CapNhatStatusStrip(checkedListBox1_LietKeTenCacSheet.Items.Count, itemsToExport.Count, soLuongXuatThanhCong);

                if (soLuongXuatThanhCong > 0 && !string.IsNullOrEmpty(filePdfCuoiCung) && File.Exists(filePdfCuoiCung))
                {
                    MoThuMucVaChonTep(filePdfCuoiCung);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Quá trình xuất bị gián đoạn:\n" + ex.Message, "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                toolStripProgressBar1_TienTrinhXuatTep.Visible = false;
                kryptonButton_XuatTepPdf.Enabled = true;
                kryptonButton_XuatTepPdf.Text = _textGocNutXuatPdf;
            }
        }
        // =========================================================================
        // ĐỘNG CƠ 1: MS EXCEL INTEROP (CHẤT LƯỢNG CAO NHẤT)
        // =========================================================================
        private (int SuccessCount, string LastFileCreated) ExportUsingExcel(string excelFilePath, string destinationFolder, List<SheetExportItem> itemsToExport, Type ignoredType, (string diaDiemThangNam, string textKemTheo, int lenDau, int lenGiua) dataThayThe)
        {
            int successCount = 0;
            string lastFileCreated = string.Empty;
            dynamic excelApp = null, workbooks = null, workbook = null;

            try
            {
                // Khởi tạo trực tiếp bằng ProgID tiêu chuẩn
                Type excelType = Type.GetTypeFromProgID("Excel.Application");
                if (excelType == null) throw new Exception("Không tìm thấy cấu hình COM Excel trên hệ thống.");

                excelApp = Activator.CreateInstance(excelType);
                excelApp.Visible = false;
                excelApp.DisplayAlerts = false;
                excelApp.ScreenUpdating = false;

                workbooks = excelApp.Workbooks;
                workbook = workbooks.Open(excelFilePath, Type.Missing, true);

                foreach (var item in itemsToExport)
                {
                    dynamic sheet = null;
                    try
                    {
                        sheet = workbook.Worksheets[item.OriginalSheetName];
                        string sName = item.OriginalSheetName.ToUpperInvariant();

                        try
                        {
                            if (sName == "LOAI_1" || sName == "LOAI_2" || sName == "LOAI_3" || sName == "LOAI_4" || sName == "KHONG_PL")
                            {
                                FormatDiaDiem(sheet.Range["E4"], dataThayThe.diaDiemThangNam);
                                FormatKemTheo(sheet.Range["A7"], dataThayThe.textKemTheo, dataThayThe.lenDau, dataThayThe.lenGiua);
                            }
                            else if (sName == "BAO CAO TONG HOP")
                            {
                                FormatDiaDiem(sheet.Range["H4"], dataThayThe.diaDiemThangNam);
                                FormatKemTheo(sheet.Range["A7"], dataThayThe.textKemTheo, dataThayThe.lenDau, dataThayThe.lenGiua);
                            }
                            else if (sName == "BC_BANHAT")
                            {
                                FormatDiaDiem(sheet.Range["H4"], dataThayThe.diaDiemThangNam);
                                FormatKemTheo(sheet.Range["A7"], dataThayThe.textKemTheo, dataThayThe.lenDau, dataThayThe.lenGiua);
                            }
                            else if (sName == "DS_BANHAT")
                            {
                                FormatDiaDiem(sheet.Range["F3"], dataThayThe.diaDiemThangNam);
                                FormatKemTheo(sheet.Range["A6"], dataThayThe.textKemTheo, dataThayThe.lenDau, dataThayThe.lenGiua);
                            }
                        }
                        catch { }

                        string finalPdfName = CleanFileName(item.TargetPdfName);
                        string fullPdfPath = Path.Combine(destinationFolder, finalPdfName);

                        sheet.ExportAsFixedFormat(0, fullPdfPath, 0, true, false, Type.Missing, Type.Missing, false);
                        successCount++;
                        lastFileCreated = fullPdfPath;

                        ReportProgress(successCount, itemsToExport.Count);
                    }
                    finally { if (sheet != null) Marshal.ReleaseComObject(sheet); }
                }
            }
            finally
            {
                if (workbook != null) { workbook.Close(false); Marshal.ReleaseComObject(workbook); }
                if (workbooks != null) Marshal.ReleaseComObject(workbooks);
                if (excelApp != null) { excelApp.Quit(); Marshal.ReleaseComObject(excelApp); }
                GC.Collect(); GC.WaitForPendingFinalizers();
            }

            return (successCount, lastFileCreated);
        }
        // =========================================================================
        // ĐỘNG CƠ 2: DỰ PHÒNG NỘI TRÚ BẰNG CLOSEDXML + LIBREOFFICE (SAFE PROCESS)
        // =========================================================================
        private (int SuccessCount, string LastFileCreated) ExportUsingLibreOffice(string excelFilePath, string destinationFolder, List<SheetExportItem> itemsToExport, string librePath, (string diaDiemThangNam, string textKemTheo, int lenDau, int lenGiua) dataThayThe)
        {
            int successCount = 0;
            string lastFileCreated = string.Empty;

            using (var wbGoc = new XLWorkbook(excelFilePath))
            {
                foreach (var ws in wbGoc.Worksheets)
                {
                    string sName = ws.Name.ToUpperInvariant();
                    try
                    {
                        if (sName == "LOAI_1" || sName == "LOAI_2" || sName == "LOAI_3" || sName == "LOAI_4" || sName == "KHONG_PL")
                        {
                            ws.Cell("E4").Value = dataThayThe.diaDiemThangNam;
                            ws.Cell("A7").Value = dataThayThe.textKemTheo;
                            ws.Cell("A7").GetRichText().Substring(dataThayThe.lenDau, dataThayThe.lenGiua).SetUnderline();
                        }
                        else if (sName == "BAO CAO TONG HOP" || sName == "BC_BANHAT")
                        {
                            ws.Cell("H4").Value = dataThayThe.diaDiemThangNam;
                            ws.Cell("A7").Value = dataThayThe.textKemTheo;
                            ws.Cell("A7").GetRichText().Substring(dataThayThe.lenDau, dataThayThe.lenGiua).SetUnderline();
                        }
                        else if (sName == "DS_BANHAT")
                        {
                            ws.Cell("F3").Value = dataThayThe.diaDiemThangNam;
                            ws.Cell("A6").Value = dataThayThe.textKemTheo;
                            ws.Cell("A6").GetRichText().Substring(dataThayThe.lenDau, dataThayThe.lenGiua).SetUnderline();
                        }
                    }
                    catch { }
                }

                foreach (var item in itemsToExport)
                {
                    string tempExcel = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".xlsx");

                    try
                    {
                        using (var tempWb = new XLWorkbook())
                        {
                            wbGoc.Worksheet(item.OriginalSheetName).CopyTo(tempWb, item.OriginalSheetName);
                            tempWb.SaveAs(tempExcel);
                        }

                        string finalPdfName = CleanFileName(item.TargetPdfName);
                        string fullPdfPath = Path.Combine(destinationFolder, finalPdfName);

                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = librePath,
                            Arguments = $"--headless --invisible --nologo --nodefault --norestore --nofirststartwizard --convert-to pdf \"{tempExcel}\" --outdir \"{destinationFolder}\"",
                            WindowStyle = ProcessWindowStyle.Hidden,
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };

                        using (Process p = new Process())
                        {
                            p.StartInfo = psi;
                            p.Start();

                            // Bảo vệ CPU máy cũ: Chờ tối đa 60 giây.
                            bool exited = p.WaitForExit(60000);
                            if (!exited)
                            {
                                p.Kill();
                                throw new TimeoutException($"Tiến trình render bị treo vì cấu hình phần cứng không đáp ứng tại {item.OriginalSheetName}. Đã buộc dừng.");
                            }
                            if (p.ExitCode != 0)
                            {
                                throw new Exception($"Động cơ dự phòng từ chối kết xuất. Mã lỗi nội bộ: {p.ExitCode}");
                            }
                        }

                        string defaultLibreOutput = Path.Combine(destinationFolder, Path.GetFileNameWithoutExtension(tempExcel) + ".pdf");
                        if (File.Exists(defaultLibreOutput))
                        {
                            if (File.Exists(fullPdfPath)) File.Delete(fullPdfPath);
                            File.Move(defaultLibreOutput, fullPdfPath);

                            successCount++;
                            lastFileCreated = fullPdfPath;
                            ReportProgress(successCount, itemsToExport.Count);
                        }
                    }
                    finally
                    {
                        if (File.Exists(tempExcel)) File.Delete(tempExcel);
                    }
                }
            }
            return (successCount, lastFileCreated);
        }
        // =========================================================================
        // CÁC HÀM TIỆN ÍCH DÙNG CHUNG (HELPER)
        // =========================================================================
        private string GetEmbeddedLibreOfficePath()
        {
            string portablePath = Path.Combine(AppContext.BaseDirectory, "LibreOffice", "program", "soffice.exe");
            if (File.Exists(portablePath)) return portablePath;

            string[] sysPaths = { @"C:\Program Files\LibreOffice\program\soffice.exe", @"C:\Program Files (x86)\LibreOffice\program\soffice.exe" };
            foreach (var p in sysPaths) { if (File.Exists(p)) return p; }
            return null;
        }
        private (string diaDiemThangNam, string textKemTheo, int lenDau, int lenGiua) GetReplacementDataFromDB()
        {
            string diaDiem = "      ", thang = "  ", nam = "    ", kyHieuBaoCao = "...............", tieuDoanHienThi = "      ";
            try
            {
                using var conn = new SqliteConnection($"Data Source={Module_DanduongGPS.DuongDanCSDL2}");
                conn.Open();
                using var cmd = new SqliteCommand("SELECT Thang, Nam, DiaDiem, KyHieuBaoCao, TenTieuDoan FROM ThongTin WHERE ID = 1", conn);
                using var rd = cmd.ExecuteReader();
                if (rd.Read())
                {
                    string rawDiaDiem = BaoMatAES.GiaiMa(rd["DiaDiem"]?.ToString() ?? "").Trim();
                    if (!string.IsNullOrEmpty(rawDiaDiem)) diaDiem = rawDiaDiem;

                    string rawThang = BaoMatAES.GiaiMa(rd["Thang"]?.ToString() ?? "").Trim();
                    if (!string.IsNullOrEmpty(rawThang)) thang = rawThang;

                    string rawNam = BaoMatAES.GiaiMa(rd["Nam"]?.ToString() ?? "").Trim();
                    if (!string.IsNullOrEmpty(rawNam)) nam = rawNam;

                    string rawKyHieu = BaoMatAES.GiaiMa(rd["KyHieuBaoCao"]?.ToString() ?? "").Trim();
                    if (!string.IsNullOrEmpty(rawKyHieu)) kyHieuBaoCao = rawKyHieu;

                    string td = BaoMatAES.GiaiMa(rd["TenTieuDoan"]?.ToString() ?? "").Trim();
                    if (!string.IsNullOrEmpty(td))
                    {
                        td = td.ToLower();
                        tieuDoanHienThi = char.ToUpper(td[0]) + td.Substring(1);
                    }
                }
            }
            catch { }

            string diaDiemThangNam = $"{diaDiem}, ngày                  tháng {thang} năm {nam}";
            string phanDau = "(Kèm theo Báo cáo ";
            string phanGiua = $"số:            {kyHieuBaoCao}, ngày             /{thang}/{nam}";
            string phanCuoi = $" của {tieuDoanHienThi})";
            string textKemTheo = phanDau + phanGiua + phanCuoi;

            return (diaDiemThangNam, textKemTheo, phanDau.Length, phanGiua.Length);
        }
        private void FormatDiaDiem(dynamic range, string text) { range.Value = text; }
        private void FormatKemTheo(dynamic range, string text, int lenDau, int lenGiua)
        {
            range.Value = text;
            try { range.Characters[lenDau + 1, lenGiua].Font.Underline = true; } catch { }
        }
        private void MoThuMucVaChonTep(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;
            IntPtr pidlList = ILCreateFromPathW(filePath);
            if (pidlList != IntPtr.Zero)
            {
                try { SHOpenFolderAndSelectItems(pidlList, 0, null, 0); }
                catch (Exception ex) { Debug.WriteLine("Lỗi API Mở thư mục: " + ex.Message); }
                finally { ILFree(pidlList); }
            }
        }
        private void ReportProgress(int current, int total)
        {
            this.Invoke(new Action(() => {
                if (!this.IsDisposed)
                {
                    toolStripProgressBar1_TienTrinhXuatTep.Value = current;
                    CapNhatStatusStrip(checkedListBox1_LietKeTenCacSheet.Items.Count, total, current);
                }
            }));
        }
        private string CleanFileName(string input)
        {
            string clean = input;
            if (!clean.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) clean += ".pdf";
            foreach (char c in Path.GetInvalidFileNameChars()) clean = clean.Replace(c.ToString(), "");
            return clean;
        }
    }
}