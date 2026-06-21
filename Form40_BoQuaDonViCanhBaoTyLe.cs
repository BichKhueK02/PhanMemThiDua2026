using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhanMemThiDua2026
{
    public partial class Form40_BoQuaDonViCanhBaoTyLe : Form
    {
        // Đường dẫn CSDL lấy tập trung từ Module hệ thống
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        public Form40_BoQuaDonViCanhBaoTyLe()
        {
            InitializeComponent();

            // Cấu hình UI chặn co dãn và căn giữa màn hình ngay từ hàm khởi tạo
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Đăng ký sự kiện tối ưu UX cải tiến trải nghiệm click 1 chạm
            checkedListBox1_ChonDonViBoQuaTyLe.SelectedIndexChanged += checkedListBox1_ChonDonViBoQuaTyLe_SelectedIndexChanged;
        }
        private async void Form40_BoQuaDonViCanhBaoTyLe_Load(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_csdl2Path))
            {
                MessageBox.Show("Đường dẫn cơ sở dữ liệu không hợp lệ!", "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }

            try
            {
                this.UseWaitCursor = true;
                checkedListBox1_ChonDonViBoQuaTyLe.Enabled = false;

                // 1. Tự động kiểm tra và tạo cấu hình bảng lưu trữ nếu chưa có
                await KiemTraVaTaoBangDuLieuAsync();

                // 2. Load song song dữ liệu từ cả 2 bảng (Sử dụng đối chiếu trên RAM)
                await LoadSongSongDuLieuDonViAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khởi tạo biểu mẫu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.UseWaitCursor = false;
                checkedListBox1_ChonDonViBoQuaTyLe.Enabled = true;
            }
        }
        /// <summary>
        /// GIẢI PHÁP UX ĐỈNH CAO: Người dùng chỉ cần click vào tên dòng chữ, checkbox tự động đảo trạng thái ngay lập tức
        /// </summary>
        private void checkedListBox1_ChonDonViBoQuaTyLe_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = checkedListBox1_ChonDonViBoQuaTyLe.SelectedIndex;
            if (index == -1) return;

            // Đảo ngược trạng thái check hiện tại (Đang check -> uncheck và ngược lại)
            bool isChecked = checkedListBox1_ChonDonViBoQuaTyLe.GetItemChecked(index);
            checkedListBox1_ChonDonViBoQuaTyLe.SetItemChecked(index, !isChecked);

            // Xóa bôi xanh dòng (Clear Selection) để giao diện nhìn thanh thoát, chuyên nghiệp hơn
            checkedListBox1_ChonDonViBoQuaTyLe.ClearSelected();
        }
        /// <summary>
        /// Tạo bảng ChonDonVi_DeBoQuaCanhBao nếu chưa tồn tại trong hệ thống
        /// </summary>
        private async Task KiemTraVaTaoBangDuLieuAsync()
        {
            using (var cn = new SqliteConnection($"Data Source={_csdl2Path}"))
            {
                await cn.OpenAsync();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS ""ChonDonVi_DeBoQuaCanhBao"" (
                            ""ID""                        INTEGER NOT NULL,
                            ""Ten_DonViBoQuaCanhBaoTyLe"" TEXT,
                            PRIMARY KEY(""ID"" AUTOINCREMENT)
                        );";
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
        /// <summary>
        /// SỬA LỖI MÃ HÓA: Đọc độc lập 2 bảng, giải mã bảng gốc rồi đối chiếu dữ liệu trực tiếp trên RAM.
        /// </summary>
        private async Task LoadSongSongDuLieuDonViAsync()
        {
            var danhSachNapUI = new List<Tuple<string, bool>>();

            using (var cn = new SqliteConnection($"Data Source={_csdl2Path}"))
            {
                await cn.OpenAsync();

                // 1. Tải danh sách các đơn vị ĐÃ LƯU TÍCH CHỌN lên RAM (Lưu dạng chữ thường)
                var dsDonViDaTich = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var cmdDaTich = cn.CreateCommand())
                {
                    cmdDaTich.CommandText = "SELECT Ten_DonViBoQuaCanhBaoTyLe FROM ChonDonVi_DeBoQuaCanhBao";
                    using (var rd = await cmdDaTich.ExecuteReaderAsync())
                    {
                        while (await rd.ReadAsync())
                        {
                            string tenDaTich = rd["Ten_DonViBoQuaCanhBaoTyLe"]?.ToString()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(tenDaTich))
                            {
                                dsDonViDaTich.Add(tenDaTich);
                            }
                        }
                    }
                }

                // 2. Tải danh sách GỐC (Lưu dạng mã hóa), giải mã và đối chiếu trạng thái
                using (var cmdGoc = cn.CreateCommand())
                {
                    cmdGoc.CommandText = "SELECT Ten_DonVi FROM DanhSach_DonVi ORDER BY ID ASC";
                    using (var rd = await cmdGoc.ExecuteReaderAsync())
                    {
                        while (await rd.ReadAsync())
                        {
                            string tenDonViRaw = rd["Ten_DonVi"]?.ToString()?.Trim() ?? "";
                            if (string.IsNullOrWhiteSpace(tenDonViRaw)) continue;

                            string tenDonViGiaiMa = tenDonViRaw;
                            try
                            {
                                tenDonViGiaiMa = BaoMatAES.GiaiMa(tenDonViRaw);
                            }
                            catch { }

                            // 3. Đối chiếu: Kiểm tra xem tên đã giải mã có nằm trong danh sách đã tích hay không
                            bool isChecked = dsDonViDaTich.Contains(tenDonViGiaiMa);
                            danhSachNapUI.Add(new Tuple<string, bool>(tenDonViGiaiMa, isChecked));
                        }
                    }
                }
            }

            // Tạm thời gỡ bỏ sự kiện thay đổi Index để quá trình nạp Items ban đầu không kích hoạt nhầm tính năng đảo check
            checkedListBox1_ChonDonViBoQuaTyLe.SelectedIndexChanged -= checkedListBox1_ChonDonViBoQuaTyLe_SelectedIndexChanged;

            checkedListBox1_ChonDonViBoQuaTyLe.BeginUpdate();
            checkedListBox1_ChonDonViBoQuaTyLe.Items.Clear();
            foreach (var item in danhSachNapUI)
            {
                checkedListBox1_ChonDonViBoQuaTyLe.Items.Add(item.Item1, item.Item2);
            }
            checkedListBox1_ChonDonViBoQuaTyLe.EndUpdate();

            // Khôi phục lại sự kiện sau khi nạp dữ liệu sạch hoàn tất
            checkedListBox1_ChonDonViBoQuaTyLe.SelectedIndexChanged += checkedListBox1_ChonDonViBoQuaTyLe_SelectedIndexChanged;
        }
        /// <summary>
        /// SỰ KIỆN: Click nút Đóng Form và Lưu cấu hình
        /// </summary>
        private async void kryptonButton1_DongFrom_Click(object sender, EventArgs e)
        {
            try
            {
                // Đổi con trỏ chuột sang trạng thái chờ xử lý dữ liệu lớn
                this.UseWaitCursor = true;
                kryptonButton1_DongFrom.Enabled = false;

                // 1. Quét giao diện thu thập toàn bộ các đơn vị ĐANG ĐƯỢC TÍCH CHỌN
                var dsDonViDuocTich = new List<string>();
                foreach (var item in checkedListBox1_ChonDonViBoQuaTyLe.CheckedItems)
                {
                    string tenDonVi = item?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(tenDonVi))
                    {
                        dsDonViDuocTich.Add(tenDonVi);
                    }
                }

                // 2. Thực thi ghi đè dữ liệu xuống SQLite một cách an toàn
                await LuuDanhSachBoQuaVaoDbAsync(dsDonViDuocTich);

                // 3. Tắt trạng thái chờ của chuột
                this.UseWaitCursor = false;

                // 4. Chuẩn bị chuỗi thông tin danh sách đơn vị để ghi vào log chuyên sâu
                string chiTietDonVi = dsDonViDuocTich.Count > 0
                    ? string.Join(", ", dsDonViDuocTich)
                    : "Không chọn đơn vị nào (Trống)";

                // 5. Khởi chạy tác vụ ghi nhật ký ngầm (Bất đồng bộ - Không block UI)
                _ = Task.Run(() =>
                {
                    try
                    {
                        // Lấy tên tài khoản từ RAM hoặc gán mặc định nếu trống
                        string taiKhoanHienTai = string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM)
                            ? "Không xác định"
                            : Module_TaiKhoan.TenTaiKhoan_RAM;

                        Module_NhatKy.GhiNhatKy(
                            taiKhoan: taiKhoanHienTai,
                            hanhDong: $"Cập nhật cấu hình: Bỏ qua cảnh báo tỷ lệ cho {dsDonViDuocTich.Count} đơn vị.",
                            ghiChu: $"Chi tiết đơn vị: {chiTietDonVi} | Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                        );
                    }
                    catch (Exception ex)
                    {
                        // Ghi nhận lỗi nội bộ vào hệ thống Output Debug của Visual Studio nếu quá trình ghi log file lỗi
                        System.Diagnostics.Debug.WriteLine($"Lỗi ghi log hệ thống: {ex.Message}");
                    }
                });

                // 6. Đóng cửa sổ biểu mẫu ngay lập tức
                this.Close();
            }
            catch (Exception ex)
            {
                this.UseWaitCursor = false;
                kryptonButton1_DongFrom.Enabled = true;
                MessageBox.Show($"Lỗi trong quá trình lưu dữ liệu: {ex.Message}", "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// Sử dụng TRANSACTION để đồng bộ hóa xóa cũ ghi mới siêu tốc
        /// </summary>
        private async Task LuuDanhSachBoQuaVaoDbAsync(List<string> dsDonVi)
        {
            using (var cn = new SqliteConnection($"Data Source={_csdl2Path}"))
            {
                await cn.OpenAsync();

                using (var trans = cn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = cn.CreateCommand())
                        {
                            cmd.Transaction = trans;

                            // Bước A: Làm sạch bảng lưu cấu hình cũ trước khi nạp bộ mới
                            cmd.CommandText = "DELETE FROM ChonDonVi_DeBoQuaCanhBao;";
                            await cmd.ExecuteNonQueryAsync();

                            // Bước B: Chèn toàn bộ danh sách đơn vị mới được tích vào DB
                            if (dsDonVi.Count > 0)
                            {
                                cmd.CommandText = "INSERT INTO ChonDonVi_DeBoQuaCanhBao (Ten_DonViBoQuaCanhBaoTyLe) VALUES (@ten);";

                                var paramTen = cmd.Parameters.Add("@ten", SqliteType.Text);

                                foreach (var tenDonVi in dsDonVi)
                                {
                                    paramTen.Value = tenDonVi;
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        await trans.CommitAsync();
                    }
                    catch
                    {
                        await trans.RollbackAsync();
                        throw;
                    }
                }
            }
        }
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            // Xây dựng nội dung thông điệp giới thiệu mục đích biểu mẫu
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("    PHẦN MỀM THỐNG KÊ & PHÂN TÍCH THI ĐUA CHUYÊN SÂU 2026");         
            sb.AppendLine();
            sb.AppendLine("CHỨC NĂNG: CẤU HÌNH MIỄN TRỪ & BỎ QUA CẢNH BÁO TỶ LỆ");
            sb.AppendLine();
            sb.AppendLine("1. Mục đích nghiệp vụ:");
            sb.AppendLine("   - Cho phép thiết lập danh sách các đơn vị có tính chất đặc thù");
            sb.AppendLine("     trong lực lượng (Ví dụ: BCH Tiểu đoàn, các Ban tham mưu,");
            sb.AppendLine("     bộ phận chuyên trách, cơ yếu...) không phải áp dụng cứng nhắc");
            sb.AppendLine("     hạn mức tỷ lệ phân loại thi đua quy định của hệ thống.");
            sb.AppendLine();
            sb.AppendLine("2. Cơ chế hoạt động:");
            sb.AppendLine("   - Đơn vị nào được TÍCH CHỌN trong danh sách này sẽ được hệ thống");
            sb.AppendLine("     tự động MIỄN TRỪ cảnh báo lỗi (tô màu đỏ) trên bảng thống kê");
            sb.AppendLine("     quân số thi đua chính.");
            sb.AppendLine("   - Định dạng hiển thị của các đơn vị đặc biệt này sẽ được đồng bộ");
            sb.AppendLine("     như trạng thái an toàn (chữ màu xanh lá, in thường).");
            sb.AppendLine();
            sb.AppendLine("3. Hướng dẫn sử dụng nhanh:");
            sb.AppendLine("   - Nhấp chuột 1 chạm trực tiếp vào tên đơn vị để Bật/Tắt dấu tích.");
            sb.AppendLine("   - Nhấn nút [Đóng Form và Lưu] để áp dụng cấu hình vào hệ thống.");
            sb.AppendLine("(*) Lưu ý: Cấu hình này sẽ được lưu trữ vĩnh viễn cho các phiên");
            sb.AppendLine("    làm việc tiếp theo và tự động ghi nhật ký hệ thống ngầm.");
            // Hiển thị hộp thoại thông báo hướng dẫn chuyên nghiệp
            MessageBox.Show(
                sb.ToString(),
                "Hướng dẫn nghiệp vụ hệ thống",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
    }
}