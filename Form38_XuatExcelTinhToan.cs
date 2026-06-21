using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;

namespace PhanMemThiDua2026
{
    public partial class Form38_XuatExcelTinhToan : Form
    {
        public DataView? _dvHienThi;
        public Dictionary<string, string>? _mapCotThoiGian;
        public string _h1 = "", _h2 = "", _h3 = "", _h4 = "";

        // ⭐ KHÓA AN TOÀN ĐA LUỒNG & TOKEN HỦY TÁC VỤ
        private int _isProcessing = 0;
        private CancellationTokenSource? _cts;
        public Form38_XuatExcelTinhToan()
        {
            InitializeComponent();
        }
        public Form38_XuatExcelTinhToan(DataView dvHienThi, Dictionary<string, string> mapCotThoiGian, string h1, string h2, string h3, string h4)
        {
            InitializeComponent();
            _dvHienThi = dvHienThi;
            _mapCotThoiGian = mapCotThoiGian;
            _h1 = h1; _h2 = h2; _h3 = h3; _h4 = h4;
        }
        private void Form38_XuatExcelTinhToan_Load(object? sender, EventArgs e)
        {
            radioButton1_XuatTheoThuTuTrongBienChe.CheckedChanged += RadioGroup_CheckedChanged;
            radioButton1_XuatTheoPhanLoaiTangDan.CheckedChanged += RadioGroup_CheckedChanged;
            label_DuongDan.TextChanged += Label_DuongDan_TextChanged;

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string tenDonViTieuDe = LayTenDonViTuCSDL();
            string cleanTenDonVi = string.IsNullOrEmpty(tenDonViTieuDe) ? "" : " - " + string.Join("_", tenDonViTieuDe.Split(Path.GetInvalidFileNameChars()));

            string defaultFileName = $"THỐNG KÊ PHÂN LOẠI THI ĐUA THÁNG {DateTime.Now.Month} NĂM {DateTime.Now.Year}{cleanTenDonVi}.xlsx";

            label_DuongDan.Text = Path.Combine(desktopPath, defaultFileName);
            radioButton1_XuatTheoThuTuTrongBienChe.Checked = true;

            UpdateUIColors();
        }
        private void RadioGroup_CheckedChanged(object? sender, EventArgs e) => UpdateUIColors();
        private void Label_DuongDan_TextChanged(object? sender, EventArgs e) => UpdateUIColors();
        private void UpdateUIColors()
        {
            Color mauR1 = radioButton1_XuatTheoThuTuTrongBienChe.Checked ? Color.Green : Color.Red;
            Color mauR2 = radioButton1_XuatTheoPhanLoaiTangDan.Checked ? Color.Green : Color.Red;
            Color mauLabel = !string.IsNullOrWhiteSpace(label_DuongDan.Text) ? Color.Green : Color.Red;

            radioButton1_XuatTheoThuTuTrongBienChe.ForeColor = mauR1;
            radioButton1_XuatTheoPhanLoaiTangDan.ForeColor = mauR2;
            label2_ChonTep.ForeColor = mauLabel;
        }
#pragma warning disable IDE1006 
        private void kryptonButton1_ChonDuongDan_Click(object? sender, EventArgs e)
        {
            string tenDonViTieuDe = LayTenDonViTuCSDL();
            string cleanTenDonVi = string.IsNullOrEmpty(tenDonViTieuDe) ? "" : " - " + string.Join("_", tenDonViTieuDe.Split(Path.GetInvalidFileNameChars()));
            string defaultFileName = $"THỐNG KÊ PHÂN LOẠI THI ĐUA THÁNG {DateTime.Now.Month} NĂM {DateTime.Now.Year}{cleanTenDonVi}.xlsx";

            using var sfd = new SaveFileDialog
            {
                Title = "Chọn nơi lưu file Excel thống kê",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = defaultFileName,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                label_DuongDan.Text = sfd.FileName;
            }
        }
        private async void kryptonButton_TaoTepExcel_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(label_DuongDan.Text))
            {
                MessageBox.Show("Đồng chí vui lòng chọn đường dẫn trước khi xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_dvHienThi == null || _dvHienThi.Count == 0) return;
            if (Interlocked.Exchange(ref _isProcessing, 1) == 1) return;

            // Recreate Token an toàn
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            string originalText = kryptonButton_TaoTepExcel.Text;
            Image originalImage = kryptonButton_TaoTepExcel.Values.Image;

            kryptonButton_TaoTepExcel.Text = "Đang xử lý...";
            kryptonButton_TaoTepExcel.Enabled = false;
            kryptonButton_TaoTepExcel.Values.Image = null;

            Form_Loading frmLoad = new Form_Loading("Đang xuất dữ liệu Excel...");
            this.Enabled = false;
            frmLoad.Show(this);

            try
            {
                string filePath = Path.GetFullPath(label_DuongDan.Text); // Validate Path
                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? string.Empty);

                string tenDonVi = LayTenDonViTuCSDL();
                bool isSortNeeded = radioButton1_XuatTheoPhanLoaiTangDan.Checked;

                // ⭐ KIẾN TRÚC STREAMING: Nối luồng dữ liệu lười biếng (Lazy Evaluation)
                IEnumerable<Form38ExportModel> exportStream = StreamDataViewRows();
                if (isSortNeeded)
                {
                    exportStream = exportStream.OrderBy(r => MucDoUuTienPhanLoai(r.KetQua));
                }
                // Chụp số lượng để in xuống cuối file Excel
                int totalCount = _dvHienThi.Count;
                var token = _cts.Token;
                // Giao toàn bộ việc xuất file cho luồng Background
                string[] dynamicHeaders = { "STT", "Họ và tên", "Số hiệu", "Đơn vị", _h1, _h2, _h3, _h4, "Kết quả", "Ghi chú" };
                await Task.Run(() => ExportToExcelTask(exportStream, totalCount, filePath, tenDonVi, dynamicHeaders, token), token);
                try
                {
                    Module_XuatNhapDuLieuThiDua.MoVaChonTepTrongExplorer(filePath);
                }
                catch (Exception ex)
                {
                    LogError("Lỗi mở thư mục bằng Shell API", ex);
                }
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                LogError("Lỗi Click Xuất Excel", ex);
                MessageBox.Show($"Lỗi xuất dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!frmLoad.IsDisposed) frmLoad.Close();
                this.Enabled = true;
                kryptonButton_TaoTepExcel.Enabled = true;
                kryptonButton_TaoTepExcel.Text = originalText;
                kryptonButton_TaoTepExcel.Values.Image = originalImage;
                Interlocked.Exchange(ref _isProcessing, 0);
            }
        }
#pragma warning restore IDE1006
        // =========================================================================================
        // ⭐ DATA PIPELINE: VIRTUAL ENUMERATOR (SIÊU TIẾT KIỆM RAM)
        // =========================================================================================
        private IEnumerable<Form38ExportModel> StreamDataViewRows()
        {
            if (_dvHienThi == null) yield break;

            string colKey1 = GetColumnKey(_h1);
            string colKey2 = GetColumnKey(_h2);
            string colKey3 = GetColumnKey(_h3);
            string colKey4 = GetColumnKey(_h4);

            foreach (DataRowView drv in _dvHienThi)
            {
                string hoTen = drv["HoVaTen"]?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(hoTen)) continue;

                // Trả thẳng dữ liệu theo kiểu "nhỏ giọt" (Stream), KHÔNG tạo List trung gian
                yield return new Form38ExportModel
                {
                    HoTen = hoTen,
                    SoHieu = ReadValue(drv, "SoHieu"),
                    DonVi = ReadValue(drv, "DonVi"),
                    Cot1 = ReadValue(drv, colKey1),
                    Cot2 = ReadValue(drv, colKey2),
                    Cot3 = ReadValue(drv, colKey3),
                    Cot4 = ReadValue(drv, colKey4),
                    KetQua = NormalizePhanLoai(drv["KetQuaTinhToan"])
                };
            }
        }
        // TÁCH HÀM TIỆN ÍCH ĐỂ CODE SẠCH SẼ
        private string GetColumnKey(string header)
        {
            if (_mapCotThoiGian == null || string.IsNullOrWhiteSpace(header)) return string.Empty;
            return _mapCotThoiGian.TryGetValue(header, out string? key) ? (key ?? string.Empty) : string.Empty;
        }
        private static string ReadValue(DataRowView drv, string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName)) return string.Empty;
            return drv[columnName]?.ToString() ?? string.Empty;
        }
        private static string NormalizePhanLoai(object? value)
        {
            string text = value?.ToString()?.Trim() ?? "";
            return string.IsNullOrWhiteSpace(text) ? "Không phân loại" : text;
        }
        // =========================================================================================
        // ⭐ EXCEL WRITER (LAZY STREAMING & SAFE SAVE)
        // =========================================================================================
        private static void ExportToExcelTask(IEnumerable<Form38ExportModel> exportStream, int totalCount, string filePath, string tenDonVi, string[] headers, CancellationToken token)
        {
            string dbPath = Module_DanduongGPS.DuongDanCSDL2;
            var dictGhiChu = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // BƯỚC A: TẢI GHI CHÚ BẰNG AES CACHE (Giới hạn bộ nhớ 50k)
            if (File.Exists(dbPath))
            {
                var aesCache = new Dictionary<string, string>(50000, StringComparer.Ordinal);
                string optConnStr = $"Data Source={dbPath};Mode=ReadOnly;Cache=Shared;Pooling=True;Default Timeout=15;";

                using var conn = new SqliteConnection(optConnStr);
                conn.Open();

                using var cmdGC = new SqliteCommand("SELECT SoHieu, GhiChu FROM DanhSach WHERE LENGTH(TRIM(IFNULL(GhiChu, ''))) > 0", conn);
                using var rd = cmdGC.ExecuteReader();

                while (rd.Read())
                {
                    token.ThrowIfCancellationRequested();
                    if (aesCache.Count > 50000) aesCache.Clear(); // Chống Memory Leak

                    try
                    {
                        string shRaw = rd.GetString(0);
                        string gcRaw = rd.GetString(1);

                        if (!aesCache.TryGetValue(shRaw, out string? shDec))
                        {
                            shDec = SafeDecrypt(shRaw);
                            aesCache[shRaw] = shDec;
                        }

                        if (!aesCache.TryGetValue(gcRaw, out string? gcDec))
                        {
                            gcDec = SafeDecrypt(gcRaw);
                            aesCache[gcRaw] = gcDec;
                        }

                        if (!string.IsNullOrEmpty(shDec)) dictGhiChu[shDec] = gcDec ?? "";
                    }
                    catch (Exception ex) { LogError("Lỗi đọc AES Loop", ex); }
                }
                aesCache.Clear();
            }

            token.ThrowIfCancellationRequested();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("ThongKe");

            var title1 = ws.Range("A1:J1").Merge();
            title1.Value = "DANH SÁCH";
            title1.Style.Font.Bold = true;
            title1.Style.Font.FontSize = 16;
            title1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var title2 = ws.Range("A2:J2").Merge();
            string suffixDonVi = !string.IsNullOrEmpty(tenDonVi) ? $" - {tenDonVi.ToUpperInvariant()}" : "";
            title2.Value = $"THỐNG KÊ PHÂN LOẠI THI ĐUA THÁNG {DateTime.Now.Month} NĂM {DateTime.Now.Year}{suffixDonVi}";
            title2.Style.Font.Bold = true;
            title2.Style.Font.FontSize = 13;
            title2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Row(4).Height = 31.8;

            // string[] headers = { "STT", "Họ và tên", "Số hiệu", "Đơn vị", "Cột 1", "Cột 2", "Cột 3", "Cột 4", "Kết quả", "Ghi chú" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            int rowIndex = 5;
            int stt = 1;

            // BƯỚC B: CHẠY STREAM DỮ LIỆU ĐỂ RÓT VÀO EXCEL MÀ KHÔNG LƯU RAM
            foreach (var item in exportStream)
            {
                token.ThrowIfCancellationRequested();

                string ghiChu = dictGhiChu.TryGetValue(item.SoHieu, out string? gc) ? gc : "";

                ws.Cell(rowIndex, 1).Value = stt++;
                ws.Cell(rowIndex, 2).Value = item.HoTen;
                ws.Cell(rowIndex, 3).Value = item.SoHieu;
                ws.Cell(rowIndex, 4).Value = item.DonVi;
                ws.Cell(rowIndex, 5).Value = item.Cot1;
                ws.Cell(rowIndex, 6).Value = item.Cot2;
                ws.Cell(rowIndex, 7).Value = item.Cot3;
                ws.Cell(rowIndex, 8).Value = item.Cot4;
                ws.Cell(rowIndex, 9).Value = item.KetQua;
                ws.Cell(rowIndex, 10).Value = ghiChu;

                rowIndex++;

                // DỌN RÁC (COMPACTION) CHỦ ĐỘNG KHI GHI NHIỀU DÒNG
                if (rowIndex % 5000 == 0)
                {
                    System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, false, false);
                }
            }

            // ĐỊNH DẠNG TỔNG THỂ DỮ LIỆU
            if (stt > 1)
            {
                var dataRange = ws.Range(5, 1, rowIndex - 1, 10);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                ws.Range(5, 3, rowIndex - 1, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Range(5, 3, rowIndex - 1, 9).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            var totalRange = ws.Range(rowIndex, 1, rowIndex, 10).Merge();
            totalRange.Value = $"Tổng cộng: {totalCount} đồng chí./.";
            totalRange.Style.Font.Bold = true;
            totalRange.Style.Font.Italic = true;
            totalRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            // CHIỀU RỘNG TĨNH (Tối ưu CPU)
            ws.Column(1).Width = 6;
            ws.Column(2).Width = 24;
            ws.Column(3).Width = 15;
            ws.Column(4).Width = 26;
            ws.Column(5).Width = 15;
            ws.Column(6).Width = 15;
            ws.Column(7).Width = 15;
            ws.Column(8).Width = 15;
            ws.Column(9).Width = 22;
            ws.Column(10).Width = 26;
            Module_BanQuyen.DongDauExcel(wb);
            // BƯỚC C: SAFE SAVE CHỐNG HỎNG FILE
            string tempFile = filePath + ".temp.xlsx";
            wb.SaveAs(tempFile);

            try
            {
                if (File.Exists(filePath)) File.Delete(filePath);
                File.Move(tempFile, filePath);
            }
            catch (Exception ex)
            {
                LogError("Lỗi Ghi đè file Excel (Safe Save)", ex);
                if (File.Exists(tempFile)) File.Delete(tempFile);
                throw new Exception("Không thể ghi đè file! Hãy đảm bảo bạn đã đóng file Excel trước khi xuất.", ex);
            }
        }
        // ==========================================
        // CÁC HÀM TIỆN ÍCH CƠ BẢN (KHÔNG DUPLICATE CODE)
        // ==========================================
        private static string LayTenDonViTuCSDL()
        {
            string dbPath = Module_DanduongGPS.DuongDanCSDL2;
            if (!File.Exists(dbPath)) return "";
            try
            {
                using var connInfo = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;Cache=Shared;Pooling=True");
                connInfo.Open();
                using var cmdInfo = new SqliteCommand("SELECT TenTieuDoan FROM ThongTin LIMIT 1", connInfo);
                return SafeDecrypt(cmdInfo.ExecuteScalar()).Trim();
            }
            catch (Exception ex)
            {
                LogError("Lỗi lấy tên Đơn vị", ex);
                return "";
            }
        }
        private static int MucDoUuTienPhanLoai(string ketQua)
        {
            if (string.IsNullOrWhiteSpace(ketQua) || ketQua.Equals("Không phân loại", StringComparison.OrdinalIgnoreCase)) return 5;
            if (ketQua.Contains("Loại 1", StringComparison.OrdinalIgnoreCase)) return 1;
            if (ketQua.Contains("Loại 2", StringComparison.OrdinalIgnoreCase)) return 2;
            if (ketQua.Contains("Loại 3", StringComparison.OrdinalIgnoreCase)) return 3;
            if (ketQua.Contains("Loại 4", StringComparison.OrdinalIgnoreCase)) return 4;
            return 5;
        }
        private static string SafeDecrypt(object? value)
        {
            if (value == null || value == DBNull.Value) return "";
            string rawValue = value.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(rawValue)) return "";
            try { return BaoMatAES.GiaiMa(rawValue) ?? ""; }
            catch (Exception ex) { LogError("Lỗi AES", ex); return ""; }
        }
        private static void LogError(string context, Exception ex)
        {
            try { Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] [ERROR] {context}: {ex.Message}"); } catch { }
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            radioButton1_XuatTheoThuTuTrongBienChe.CheckedChanged -= RadioGroup_CheckedChanged;
            radioButton1_XuatTheoPhanLoaiTangDan.CheckedChanged -= RadioGroup_CheckedChanged;
            label_DuongDan.TextChanged -= Label_DuongDan_TextChanged;

            _cts?.Cancel();
            _cts?.Dispose();
            base.OnFormClosing(e);
        }
    }
    public class Form38ExportModel
    {
        public string HoTen { get; set; } = string.Empty;
        public string SoHieu { get; set; } = string.Empty;
        public string DonVi { get; set; } = string.Empty;
        public string Cot1 { get; set; } = string.Empty;
        public string Cot2 { get; set; } = string.Empty;
        public string Cot3 { get; set; } = string.Empty;
        public string Cot4 { get; set; } = string.Empty;
        public string KetQua { get; set; } = string.Empty;
        public string GhiChu { get; set; } = string.Empty;
    }
}