
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
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        // KHAI BÁO WINDOWS API: GỌI THƯ MỤC VÀ CHỌN TỆP ĐƠN NHIỆM     
        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern void ILFree(IntPtr pidl);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern IntPtr ILCreateFromPathW(string pszPath);
        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, uint dwFlags);
        // CLASS LƯU TRỮ TRẠNG THÁI ÁNH XẠ
        // CLASS LƯU TRỮ TRẠNG THÁI ÁNH XẠ
        public class SheetExportItem
        {
            public string OriginalSheetName { get; set; }
            public string TargetPdfName { get; set; }

            public override string ToString()
            {
                // Kiểm tra xem đây có phải là sheet tự do (không nằm trong danh sách quy định) hay không.
                // Ở hàm load, sheet tự do sẽ được gán đích mặc định là "TênSheet.pdf".
                if (TargetPdfName.Equals($"{OriginalSheetName}.pdf", StringComparison.OrdinalIgnoreCase) ||
                    TargetPdfName.Equals(OriginalSheetName, StringComparison.OrdinalIgnoreCase))
                {
                    // Nếu không nằm trong quy định: Hiển thị gọn gàng, KHÔNG CÓ mũi tên
                    if (TargetPdfName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        return TargetPdfName;

                    return $"{TargetPdfName}.pdf";
                }

                // Nếu nằm trong quy định (đã được phần mềm đổi thành tên dài): Hiển thị mũi tên ánh xạ
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
            // 🌟 THÊM SỰ KIỆN CHO NÚT "CHỌN TẤT CẢ"
            if (checkBox1_ChonTatCa != null)
            {
                checkBox1_ChonTatCa.Visible = false; // Mặc định ẩn
                checkBox1_ChonTatCa.CheckedChanged -= CheckBox1_ChonTatCa_CheckedChanged;
                checkBox1_ChonTatCa.CheckedChanged += CheckBox1_ChonTatCa_CheckedChanged;
            }
        }
        private void CheckBox1_ChonTatCa_CheckedChanged(object sender, EventArgs e)
        {
            if (checkedListBox1_LietKeTenCacSheet.Items.Count == 0) return;

            bool isChecked = checkBox1_ChonTatCa.Checked;

            // 🌟 ĐỔI MÀU CHỮ CỦA CHECKBOX (Xanh lá đậm nếu chọn, Đỏ nếu bỏ chọn)
            checkBox1_ChonTatCa.ForeColor = isChecked ? Color.DarkGreen : Color.Red;

            // TẠM NGẮT SỰ KIỆN ĐỂ CHỐNG GIẬT LAG KHI CHẠY VÒNG LẶP
            checkedListBox1_LietKeTenCacSheet.ItemCheck -= CheckedListBox1_ItemCheck;

            // Lặp và set trạng thái cho toàn bộ item
            for (int i = 0; i < checkedListBox1_LietKeTenCacSheet.Items.Count; i++)
            {
                checkedListBox1_LietKeTenCacSheet.SetItemChecked(i, isChecked);
            }

            // BẬT LẠI SỰ KIỆN
            checkedListBox1_LietKeTenCacSheet.ItemCheck += CheckedListBox1_ItemCheck;

            // Cập nhật lại thanh Status
            int checkedCount = checkedListBox1_LietKeTenCacSheet.CheckedItems.Count;
            CapNhatStatusStrip(checkedListBox1_LietKeTenCacSheet.Items.Count, checkedCount, 0);

            // Vẽ lại UI cho ListBox
            checkedListBox1_LietKeTenCacSheet.Invalidate();
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
        // CÁC BIẾN KIỂM SOÁT HIỆU ỨNG THANH TIẾN TRÌNH
        private bool _dangXuatFile = false;
        private int _soFileDaXong = 0;
        private int _tongSoFile = 0;

        // HÀM TẠO HIỆU ỨNG TĂNG DẦN ĐỀU (CHẠY NGẦM SONG SONG)
        private async Task HieuUngThanhTienTrinhAsync()
        {
            while (_dangXuatFile && !this.IsDisposed)
            {
                this.Invoke(new Action(() =>
                {
                    if (toolStripProgressBar1_TienTrinhXuatTep.Visible)
                    {
                        int giaTriHienTai = toolStripProgressBar1_TienTrinhXuatTep.Value;

                        // Cho phép thanh trượt "ảo" lên tối đa 90% tiến độ của file hiện tại đang xử lý
                        // (Luôn chừa lại 10% cuối cùng để chờ file thực sự xuất xong mới snap)
                        int mucTieuAo = (_soFileDaXong * 100) + 90;
                        int maxToanCuc = _tongSoFile * 100;

                        if (giaTriHienTai < mucTieuAo && giaTriHienTai < maxToanCuc)
                        {
                            // Tăng từ từ mỗi nhịp 1% của file
                            toolStripProgressBar1_TienTrinhXuatTep.Value += 1;
                        }
                    }
                }));

                // Tốc độ trượt: 25 mili-giây / nhịp (Rất mượt)
                await Task.Delay(25);
            }
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
                { kryptonButton1_ChonDuongDanTepExcel, "Chọn đường dẫn tệp excel" },
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
        private async void KryptonButton1_ChonDuongDanTepExcel_Click(object sender, EventArgs e)
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

                // 🌟 BƯỚC 1: CẬP NHẬT GIAO DIỆN NGAY LẬP TỨC
                label_DuongDanExcel.Text = selectedFile;
                label_DuongDanExcel.ForeColor = Color.DarkGreen;

                // Khóa nút trong lúc chờ load để tránh user bấm spam liên tục
                kryptonButton1_ChonDuongDanTepExcel.Enabled = false;
                kryptonButton_XuatTepPdf.Enabled = false;

                // Hiện thông báo đang Load dưới StatusStrip
                // (Hãy đảm bảo bạn đã tạo một ToolStripStatusLabel tên là toolStripStatusLabel1_DangLoad trong Designer)
                if (toolStripStatusLabel1_DangLoad != null)
                {
                    toolStripStatusLabel1_DangLoad.Visible = true;
                    toolStripStatusLabel1_DangLoad.Text = "Đang phân tích tệp Excel, vui lòng đợi...";
                }

                // 🌟 BƯỚC 2: GỌI HÀM XỬ LÝ NGẦM
                await PhanTichVaAnhXaSheetTuExcelAsync(selectedFile);

                // 🌟 BƯỚC 3: MỞ KHÓA VÀ DỌN DẸP GIAO DIỆN
                kryptonButton1_ChonDuongDanTepExcel.Enabled = true;

                // Nếu load thành công và có sheet thì mới cho bấm nút Xuất
                kryptonButton_XuatTepPdf.Enabled = checkedListBox1_LietKeTenCacSheet.Items.Count > 0;

                // Ẩn thông báo đang Load
                if (toolStripStatusLabel1_DangLoad != null)
                {
                    toolStripStatusLabel1_DangLoad.Visible = false;
                    toolStripStatusLabel1_DangLoad.Text = "";
                }
            }
        }
        private async Task PhanTichVaAnhXaSheetTuExcelAsync(string excelPath)
        {
            try
            {
                checkedListBox1_LietKeTenCacSheet.SelectedIndexChanged -= CheckedListBox1_SelectedIndexChanged;
                checkedListBox1_LietKeTenCacSheet.Items.Clear();

                // 🌟 ĐẨY TÁC VỤ NẶNG XUỐNG LUỒNG NGẦM (TASK.RUN) ĐỂ GIẢI PHÓNG UI
                var danhSachItem = await Task.Run(() =>
                {
                    var items = new List<SheetExportItem>();

                    // Chỉ mở XLWorkbook ĐÚNG 1 LẦN duy nhất
                    using (var wb = new XLWorkbook(excelPath))
                    {
                        if (!wb.Worksheets.Any())
                        {
                            throw new Exception("Tệp Excel này không có sheet dữ liệu nào hợp lệ!");
                        }

                        foreach (var ws in wb.Worksheets)
                        {
                            string sheetName = ws.Name;
                            if (sheetName.Equals("THONG_TIN", StringComparison.OrdinalIgnoreCase)) continue;

                            string targetName = _mapQuyDinhTenPdf.TryGetValue(sheetName, out string mappedName)
                                                ? mappedName
                                                : $"{sheetName}.pdf";

                            items.Add(new SheetExportItem { OriginalSheetName = sheetName, TargetPdfName = targetName });
                        }
                    }
                    return items;
                });

                // TRẢ KẾT QUẢ TỪ LUỒNG NGẦM LÊN GIAO DIỆN CHÍNH
                foreach (var item in danhSachItem)
                {
                    checkedListBox1_LietKeTenCacSheet.Items.Add(item, true);
                }
                // 🌟 LOGIC HIỂN THỊ NÚT CHỌN TẤT CẢ
                int tongSoSheet = checkedListBox1_LietKeTenCacSheet.Items.Count;
                if (checkBox1_ChonTatCa != null)
                {
                    // Tạm ngắt sự kiện để set trạng thái mặc định (Đang chọn tất cả)
                    checkBox1_ChonTatCa.CheckedChanged -= CheckBox1_ChonTatCa_CheckedChanged;
                    checkBox1_ChonTatCa.Checked = true;
                    // 🌟 ĐẶT MÀU XANH LÁ MẶC ĐỊNH VÌ ĐANG CHECK=TRUE
                    checkBox1_ChonTatCa.ForeColor = Color.DarkGreen;
                    checkBox1_ChonTatCa.CheckedChanged += CheckBox1_ChonTatCa_CheckedChanged;

                    // Chỉ hiện khi có từ 2 sheet trở lên
                    checkBox1_ChonTatCa.Visible = tongSoSheet >= 2;
                }
                CapNhatStatusStrip(checkedListBox1_LietKeTenCacSheet.Items.Count, checkedListBox1_LietKeTenCacSheet.CheckedItems.Count, 0);
            }
            catch (Exception ex)
            {
                // Bẫy lỗi an toàn: File đang mở ở chỗ khác hoặc sai định dạng
                MessageBox.Show("Tệp Excel đang mở hoặc không đúng định dạng:\n" + ex.Message, "Lỗi đọc tệp", MessageBoxButtons.OK, MessageBoxIcon.Error);

                // Khôi phục nhãn về trạng thái lỗi
                label_DuongDanExcel.Text = "Chưa chọn tệp excel";
                label_DuongDanExcel.ForeColor = Color.Red;
            }
            finally
            {
                checkedListBox1_LietKeTenCacSheet.SelectedIndexChanged += CheckedListBox1_SelectedIndexChanged;
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

            // 🌟 ĐỒNG BỘ NGƯỢC VỚI NÚT "CHỌN TẤT CẢ" VÀ ĐỔI MÀU
            if (checkBox1_ChonTatCa != null && checkBox1_ChonTatCa.Visible)
            {
                // Ngắt sự kiện để tránh gọi vòng tròn
                checkBox1_ChonTatCa.CheckedChanged -= CheckBox1_ChonTatCa_CheckedChanged;

                // Kiểm tra xem có đang chọn đủ 100% không
                bool isAllChecked = (checkedCount == checkedListBox1_LietKeTenCacSheet.Items.Count);

                // Tự động Bật/Tắt tick
                checkBox1_ChonTatCa.Checked = isAllChecked;

                // 🌟 TỰ ĐỘNG ĐỔI MÀU CHỮ ĐỒNG BỘ THEO
                checkBox1_ChonTatCa.ForeColor = isAllChecked ? Color.DarkGreen : Color.Red;

                checkBox1_ChonTatCa.CheckedChanged += CheckBox1_ChonTatCa_CheckedChanged;
            }

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
            if (sheetDaChon > 0) dsThongTin.Add($"Đã chọn {sheetDaChon} sheet để tạo *.pdf");
            if (fileDaTao > 0) dsThongTin.Add($"Đã tạo thành công {fileDaTao} file *.pdf");

            toolStripStatusLabel1_TongCongTepPdf.Text = string.Join(" | ", dsThongTin);
        }
        // TRUNG TÂM ĐIỀU PHỐI ĐỘNG CƠ (DUAL-ENGINE ORCHESTRATOR)       
        private async void kryptonButton_XuatTepPdf_Click(object sender, EventArgs e)
        {
            string excelPath = label_DuongDanExcel.Text?.Trim();
            string pdfFolderPath = label_DuongDanPdf.Text?.Trim();

            // =========================================================================
            // 🌟 TÍNH NĂNG THÔNG MINH 1: Tự động gọi nút chọn tệp Excel nếu chưa chọn
            // =========================================================================
            if (string.IsNullOrWhiteSpace(excelPath) ||
                excelPath.Equals("Chưa chọn tệp excel", StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(excelPath))
            {
                MessageBox.Show("Bạn chưa chọn tệp Excel nguồn hoặc tệp không còn tồn tại!\nHệ thống sẽ mở hộp thoại để bạn chọn tệp ngay bây giờ.", "Gợi ý thao tác", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Giả lập cú click của người dùng vào nút Chọn tệp Excel
                kryptonButton1_ChonDuongDanTepExcel.PerformClick();

                // Dừng tiến trình xuất ở đây để người dùng chọn tệp xong mới bấm xuất lại
                return;
            }

            if (!Directory.Exists(pdfFolderPath))
            {
                MessageBox.Show("Bạn chưa chọn thư mục đích để lưu tệp *.pdf!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                kryptonButton1_ChonDuongDanLuuTepPdf.PerformClick();
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

            // 🌟 KHỞI ĐỘNG HIỆU ỨNG MƯỢT
            _dangXuatFile = true;
            _soFileDaXong = 0;
            _tongSoFile = itemsToExport.Count;

            toolStripProgressBar1_TienTrinhXuatTep.Visible = true;
            toolStripProgressBar1_TienTrinhXuatTep.Minimum = 0;
            toolStripProgressBar1_TienTrinhXuatTep.Maximum = _tongSoFile * 100; // Nhân 100 để xé nhỏ bước nhảy
            toolStripProgressBar1_TienTrinhXuatTep.Value = 0;

            // Bắn luồng hiệu ứng chạy song song dưới nền
            _ = HieuUngThanhTienTrinhAsync();
            try
            {
                await Task.Delay(50);
                int soLuongXuatThanhCong = 0;
                string filePdfCuoiCung = "";
                var dataThayThe = GetReplacementDataFromDB();
                bool xuatBangExcelThanhCong = false;

                // 🌟 GIA CỐ 1: Bỏ qua bước kiểm tra ProgID lỏng lẻo, ép khởi tạo trực tiếp
                try
                {
                    var result = await Task.Run(() => ExportUsingExcel(excelPath, pdfFolderPath, itemsToExport, typeof(object), dataThayThe));
                    soLuongXuatThanhCong = result.SuccessCount;
                    filePdfCuoiCung = result.LastFileCreated;
                    xuatBangExcelThanhCong = true;
                }
                catch (Exception exInterop)
                {
                    Debug.WriteLine($"Động cơ Excel từ chối hoạt động ({exInterop.Message}). Kích hoạt Động cơ LibreOffice...");
                    xuatBangExcelThanhCong = false;
                }

                if (!xuatBangExcelThanhCong)
                {
                    string librePath = GetEmbeddedLibreOfficePath();
                    if (string.IsNullOrEmpty(librePath))
                    {
                        throw new Exception("Hệ thống không thể gọi Microsoft Excel trên máy này và cũng không tìm thấy Động cơ kết xuất LibreOffice (Kế hoạch B). Vui lòng kiểm tra lại môi trường Office.");
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
                // 🌟 KẾT THÚC HIỆU ỨNG
                _dangXuatFile = false; // Báo cho luồng ảo dừng lại

                // Trượt nốt lên 100% tổng để tạo cảm giác trọn vẹn
                toolStripProgressBar1_TienTrinhXuatTep.Value = toolStripProgressBar1_TienTrinhXuatTep.Maximum;

                // Đợi 0.3 giây cho người dùng chiêm ngưỡng thành quả 100% trước khi giấu thanh tiến trình đi
                await Task.Delay(300);

                toolStripProgressBar1_TienTrinhXuatTep.Visible = false;
                kryptonButton_XuatTepPdf.Enabled = true;
                kryptonButton_XuatTepPdf.Text = _textGocNutXuatPdf;
            }
        }
        // ĐỘNG CƠ 1: LATE BINDING EXCEL (CHỐNG LỖI PHIÊN BẢN & CHỐNG RÒ RỈ RAM)        
        // ĐỘNG CƠ 1: LATE BINDING EXCEL (CHỐNG LỖI PHIÊN BẢN & CHỐNG RÒ RỈ RAM)    
        private (int SuccessCount, string LastFileCreated) ExportUsingExcel(
            string excelFilePath,
            string destinationFolder,
            List<SheetExportItem> itemsToExport,
            Type ignoredType,
            (string diaDiemThangNam, string textKemTheo, int lenDau, int lenGiua) dataThayThe)
        {
            int successCount = 0;
            string lastFileCreated = string.Empty;

            // 🌟 SỬ DỤNG DYNAMIC THAY VÌ KHAI BÁO CHẾT KIỂU EXCEL.APPLICATION
            dynamic excelApp = null;
            dynamic workbooks = null;
            dynamic workbook = null;

            try
            {
                // BƯỚC 1: TẠO TIẾN TRÌNH TÀNG HÌNH MỚI TINH (KHÔNG ĐỤNG CHẠM TIẾN TRÌNH ĐANG MỞ CỦA USER)
                // Tìm MS Excel, nếu không có thì dự phòng tìm WPS Office
                Type excelType = Type.GetTypeFromProgID("Excel.Application")
                              ?? Type.GetTypeFromProgID("KWPS.Application")
                              ?? Type.GetTypeFromProgID("Ket.Application");

                if (excelType == null)
                    throw new Exception("Không tìm thấy COM Excel hoặc WPS Office trên hệ thống.");

                // Khởi tạo an toàn trên mọi phiên bản .NET
                excelApp = Activator.CreateInstance(excelType);

                // Ép Excel chạy ngầm, không hiện cảnh báo, ưu tiên tốc độ
                excelApp.Visible = false;
                excelApp.DisplayAlerts = false;
                excelApp.ScreenUpdating = false;

                // Mở File an toàn ở chế độ ReadOnly (Tham số thứ 3 là true) để không bị khóa file
                workbooks = excelApp.Workbooks;
                workbook = workbooks.Open(excelFilePath, Type.Missing, true);

                // BƯỚC 2: TIẾN HÀNH DUYỆT VÀ XUẤT TỪNG SHEET RA PDF
                foreach (var item in itemsToExport)
                {
                    dynamic sheet = null;
                    try
                    {
                        sheet = workbook.Worksheets[item.OriginalSheetName];
                        string sName = item.OriginalSheetName.ToUpperInvariant();

                        // Cố gắng định dạng dữ liệu, dùng try-catch bọc lót để không làm sập tiến trình xuất
                        try
                        {
                            if (sName == "LOAI_1" || sName == "LOAI_2" || sName == "LOAI_3" || sName == "LOAI_4" || sName == "KHONG_PL")
                            {
                                FormatDiaDiem(sheet.Range["E4"], dataThayThe.diaDiemThangNam);
                                FormatKemTheo(sheet.Range["A7"], dataThayThe.textKemTheo, dataThayThe.lenDau, dataThayThe.lenGiua);
                            }
                            else if (sName == "BAO CAO TONG HOP" || sName == "BC_BANHAT")
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
                        catch (Exception exFormat)
                        {
                            Debug.WriteLine($"Cảnh báo: Định dạng Sheet {sName} thất bại - {exFormat.Message}");
                        }

                        // Làm sạch tên file và kết xuất
                        string finalPdfName = CleanFileName(item.TargetPdfName);
                        string fullPdfPath = Path.Combine(destinationFolder, finalPdfName);

                        // Tham số 1 (0): Excel.XlFixedFormatType.xlTypePDF
                        // Tham số 3 (0): Excel.XlFixedFormatQuality.xlQualityStandard
                        sheet.ExportAsFixedFormat(0, fullPdfPath, 0, true, false, Type.Missing, Type.Missing, false);

                        successCount++;
                        lastFileCreated = fullPdfPath;

                        // Báo cáo tiến trình lên UI
                        ReportProgress(successCount, itemsToExport.Count);
                    }
                    catch (Exception exSheet)
                    {
                        Debug.WriteLine($"Lỗi xuất PDF sheet {item.OriginalSheetName}: {exSheet.Message}");
                    }
                    finally
                    {
                        // Giải phóng COM của Sheet ngay sau khi dùng xong
                        if (sheet != null) Marshal.ReleaseComObject(sheet);
                    }
                }
            }
            finally
            {
                // BƯỚC 3: DỌN RÁC VÀ TIÊU DIỆT HOÀN TOÀN TIẾN TRÌNH EXCEL MA DƯỚI NỀN
                if (workbook != null)
                {
                    workbook.Close(false);
                    Marshal.ReleaseComObject(workbook);
                }
                if (workbooks != null)
                {
                    Marshal.ReleaseComObject(workbooks);
                }
                if (excelApp != null)
                {
                    excelApp.Quit();
                    Marshal.ReleaseComObject(excelApp);
                }

                // ÉP WIN GIẢI PHÓNG TIẾN TRÌNH MA (CHẠY 2 CHU KỲ MỚI DIỆT ĐƯỢC EXCEL.EXE DƯỚI NỀN)
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            return (successCount, lastFileCreated);
        }
        // SỬA LẠI FORMATTER THÀNH DYNAMIC ĐỂ CỞI TRÓI KHỎI INTEROP DLL
        private void FormatDiaDiem(dynamic range, string text)
        {
            range.Value = text;
        }
        private void FormatKemTheo(dynamic range, string text, int lenDau, int lenGiua)
        {
            range.Value = text;
            try
            {
                // Try-catch bảo vệ trong trường hợp định dạng chữ của Excel bị giới hạn
                range.Characters[lenDau + 1, lenGiua].Font.Underline = true;
            }
            catch { }
        }
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
                        // 🌟 GIA CỐ 3: Silent Catch chống sập App do File Lock
                        try
                        {
                            if (File.Exists(tempExcel)) File.Delete(tempExcel);
                        }
                        catch { Debug.WriteLine("Không thể xóa file tạm ngay lúc này: " + tempExcel); }
                    }
                }
            }
            return (successCount, lastFileCreated);
        }
        // CÁC HÀM TIỆN ÍCH DÙNG CHUNG (HELPER)
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
        private void FormatDiaDiem(Excel.Range range, string text)
        {
            range.Value = text;
        }
        private void FormatKemTheo(Excel.Range range, string text, int lenDau, int lenGiua)
        {
            range.Value = text;

            try
            {
                range.Characters[lenDau + 1, lenGiua].Font.Underline = true;
            }
            catch
            {
            }
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
            // Cập nhật mốc hoàn thành thực tế để luồng ảo (Task) biết đường chạy tiếp
            _soFileDaXong = current;

            this.Invoke(new Action(() =>
            {
                if (!this.IsDisposed)
                {
                    // Khi 1 file thực sự xong, đẩy vọt phần % còn lại lên mốc 100% của file đó
                    int mucTieuThucTe = current * 100;
                    if (toolStripProgressBar1_TienTrinhXuatTep.Value < mucTieuThucTe)
                    {
                        toolStripProgressBar1_TienTrinhXuatTep.Value = mucTieuThucTe;
                    }

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
        private void label_DuongDanExcel_Click(object sender, EventArgs e)
        {
            string filePath = label_DuongDanExcel.Text?.Trim();

            // Kiểm tra xem đường dẫn có hợp lệ và tệp có tồn tại thực tế không
            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            {
                // Gọi API Windows để mở thư mục và chọn (bôi đen) tệp, tận dụng cửa sổ cũ nếu đang mở
                MoThuMucVaChonTep(filePath);
            }
            else
            {
                MessageBox.Show("Đường dẫn tệp Excel không tồn tại hoặc chưa được chọn!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void label_DuongDanPdf_Click(object sender, EventArgs e)
        {
            string folderPath = label_DuongDanPdf.Text?.Trim();

            // Kiểm tra xem thư mục có tồn tại thực tế không
            if (!string.IsNullOrWhiteSpace(folderPath) && Directory.Exists(folderPath))
            {
                try
                {
                    // Sử dụng Process.Start với cờ ShellExecute để mở thư mục. 
                    // Hệ điều hành Windows tự động quản lý: nếu thư mục này đang mở dưới nền, nó sẽ kích hoạt và đưa lên trên.
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = folderPath,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể mở thư mục:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Thư mục lưu tệp PDF không tồn tại hoặc chưa được chọn!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                Description = "Chọn thư mục lưu các tệp *.pdf xuất ra",
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
    }
}