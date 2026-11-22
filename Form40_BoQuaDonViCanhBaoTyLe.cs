///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using Microsoft.Data.Sqlite;
using System.Diagnostics;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form40_BoQuaDonViCanhBaoTyLe : Form
    {
        // Đường dẫn CSDL lấy tập trung từ Module hệ thống
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private const string SqlTaoBang = @"
            CREATE TABLE IF NOT EXISTS ""ChonDonVi_DeBoQuaCanhBao"" (
                ""ID""                        INTEGER NOT NULL,
                ""Ten_DonViBoQuaCanhBaoTyLe"" TEXT,
                PRIMARY KEY(""ID"" AUTOINCREMENT)
            );";
        // Danh sách đơn vị kiểu web (do Module_GiaoDienForm40Web dựng ở đầu sự kiện Load)
        private DanhSachDonViWebF40? _dsDonVi;
        private bool _daNapXong; // Chỉ khi nạp thành công mới cho phép ghi CSDL (tránh xóa nhầm cấu hình cũ)
        private bool _dangLuu;   // Đang ghi CSDL: chặn bấm lặp và chặn đóng form giữa chừng
        private bool _daLuu;     // Đã xử lý xong việc lưu → cho phép đóng không hỏi lại
        public Form40_BoQuaDonViCanhBaoTyLe()
        {
            InitializeComponent();
            // Cấu hình UI chặn co dãn và căn giữa màn hình ngay từ hàm khởi tạo
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.FormClosing += Form40_BoQuaDonViCanhBaoTyLe_FormClosing;
        }

        //  LOAD

        private async void Form40_BoQuaDonViCanhBaoTyLe_Load(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_csdl2Path))
            {
                MessageBox.Show(this, "Đường dẫn cơ sở dữ liệu không hợp lệ!", "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }
            // 1. Dựng giao diện kiểu web (form chưa hiện nên người dùng không thấy cảnh vẽ dở)
            DanhSachDonViWebF40 dsUi;
            try
            {
                dsUi = Module_GiaoDienForm40Web.ApDung(this);
                dsUi.DangTai = true;
                _dsDonVi = dsUi;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Lỗi dựng giao diện biểu mẫu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }
            // 2. Nạp dữ liệu ở luồng nền: đọc 2 bảng + giải mã AES không còn chặn giao diện
            this.UseWaitCursor = true;
            try
            {
                string duongDan = _csdl2Path;
                var danhSach = await Task.Run(() => TaiDanhSachDonVi(duongDan));
                if (this.IsDisposed || this.Disposing) return;
                dsUi.NapDuLieu(danhSach);
                _daNapXong = true;
            }
            catch (Exception ex)
            {
                if (!this.IsDisposed)
                {
                    dsUi.DangTai = false;
                    MessageBox.Show(this, $"Lỗi khởi tạo biểu mẫu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                if (!this.IsDisposed) this.UseWaitCursor = false;
            }
        }

        //  TRUY VẤN CSDL (chạy ở luồng nền, dùng API đồng bộ vì SQLite không có I/O bất đồng bộ thật)

        private static string TaoChuoiKetNoi(string duongDan)
        {
            // Dùng builder để đường dẫn có ký tự đặc biệt (; ' ...) không làm hỏng chuỗi kết nối
            return new SqliteConnectionStringBuilder { DataSource = duongDan }.ToString();
        }
        /// <summary>
        /// Tạo bảng cấu hình nếu chưa có, đọc danh sách đã tích, đọc danh sách gốc, giải mã tên và đối chiếu trên RAM.
        /// </summary>
        private static List<(string Ten, bool Chon)> TaiDanhSachDonVi(string duongDan)
        {
            var ketQua = new List<(string Ten, bool Chon)>();
            using var cn = new SqliteConnection(TaoChuoiKetNoi(duongDan));
            cn.Open();
            using (var cmdTao = cn.CreateCommand())
            {
                cmdTao.CommandText = SqlTaoBang;
                cmdTao.ExecuteNonQuery();
            }
            // 1. Các đơn vị ĐÃ LƯU TÍCH CHỌN (không phân biệt hoa/thường)
            var daTich = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "SELECT Ten_DonViBoQuaCanhBaoTyLe FROM ChonDonVi_DeBoQuaCanhBao";
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    if (rd.IsDBNull(0)) continue;
                    string ten = rd.GetString(0).Trim();
                    if (ten.Length > 0) daTich.Add(ten);
                }
            }
            // 2. Danh sách GỐC (tên đang mã hóa) → giải mã → đối chiếu trạng thái
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "SELECT Ten_DonVi FROM DanhSach_DonVi ORDER BY ID ASC";
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    if (rd.IsDBNull(0)) continue;
                    string raw = rd.GetString(0).Trim();
                    if (raw.Length == 0) continue;
                    string ten = raw;
                    try { ten = Module_BaoMatAES.GiaiMa(raw); }
                    catch { /* Không giải mã được → giữ nguyên chuỗi gốc như trước */ }
                    ketQua.Add((ten, daTich.Contains(ten)));
                }
            }
            return ketQua;
        }
        /// <summary>
        /// Xóa cũ ghi mới trong 1 TRANSACTION: hoặc ghi đủ hoặc không ghi gì, cấu hình cũ không bao giờ bị mất giữa chừng.
        /// </summary>
        private static void LuuDanhSachBoQua(string duongDan, List<string> dsDonVi)
        {
            using var cn = new SqliteConnection(TaoChuoiKetNoi(duongDan));
            cn.Open();
            using var trans = cn.BeginTransaction();
            try
            {
                using (var cmd = cn.CreateCommand())
                {
                    cmd.Transaction = trans;
                    cmd.CommandText = SqlTaoBang;
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "DELETE FROM ChonDonVi_DeBoQuaCanhBao;";
                    cmd.ExecuteNonQuery();
                }
                if (dsDonVi.Count > 0)
                {
                    using var cmdThem = cn.CreateCommand();
                    cmdThem.Transaction = trans;
                    cmdThem.CommandText = "INSERT INTO ChonDonVi_DeBoQuaCanhBao (Ten_DonViBoQuaCanhBaoTyLe) VALUES (@ten);";
                    var paramTen = cmdThem.Parameters.Add("@ten", SqliteType.Text);
                    foreach (var tenDonVi in dsDonVi)
                    {
                        paramTen.Value = tenDonVi;
                        cmdThem.ExecuteNonQuery();
                    }
                }
                trans.Commit();
            }
            catch
            {
                try { trans.Rollback(); } catch { /* bỏ qua lỗi rollback, ném lại lỗi gốc */ }
                throw;
            }
        }

        //  LƯU & ĐÓNG

        /// <summary>SỰ KIỆN: Click nút "Đóng và lưu".</summary>
        private async void kryptonButton1_DongFrom_Click(object? sender, EventArgs e)
        {
            await LuuRoiDongAsync();
        }
        private async Task LuuRoiDongAsync()
        {
            if (_dangLuu) return;
            // Chưa nạp xong dữ liệu (lỗi lúc mở form): chỉ đóng, TUYỆT ĐỐI không ghi để khỏi xóa trắng cấu hình đã lưu
            if (_dsDonVi == null || !_daNapXong)
            {
                _daLuu = true;
                this.Close();
                return;
            }
            // Không có thay đổi: không ghi CSDL, không sinh nhật ký thừa
            if (!_dsDonVi.CoThayDoi())
            {
                _daLuu = true;
                this.Close();
                return;
            }
            _dangLuu = true;
            this.UseWaitCursor = true;
            kryptonButton1_DongFrom.Enabled = false;
            List<string> dsDonViDuocTich = _dsDonVi.LayDanhSachDuocChon();
            try
            {
                string duongDan = _csdl2Path;
                await Task.Run(() => LuuDanhSachBoQua(duongDan, dsDonViDuocTich));
            }
            catch (Exception ex)
            {
                _dangLuu = false;
                if (!this.IsDisposed)
                {
                    this.UseWaitCursor = false;
                    kryptonButton1_DongFrom.Enabled = true;
                    MessageBox.Show(this, $"Lỗi trong quá trình lưu dữ liệu: {ex.Message}", "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }
            _dangLuu = false;
            _daLuu = true;
            GhiNhatKyNen(dsDonViDuocTich);
            if (!this.IsDisposed)
            {
                this.UseWaitCursor = false;
                this.Close();
            }
        }
        /// <summary>Ghi nhật ký ngầm (không chặn giao diện, lỗi ghi log không ảnh hưởng việc lưu).</summary>
        private static void GhiNhatKyNen(List<string> dsDonVi)
        {
            var taiKhoan = Module_TaiKhoan.TenTaiKhoan_RAM; // đọc sẵn ở luồng giao diện
            int soLuong = dsDonVi.Count;
            string chiTietDonVi = soLuong > 0 ? string.Join(", ", dsDonVi) : "Không chọn đơn vị nào (Trống)";
            string ghiChu = $"Chi tiết đơn vị: {chiTietDonVi} | Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}";
            _ = Task.Run(() =>
            {
                try
                {
                    Module_NhatKy.GhiNhatKy(
                        taiKhoan: taiKhoan,
                        hanhDong: $"Cập nhật cấu hình: Bỏ qua cảnh báo tỷ lệ cho {soLuong} đơn vị.",
                        ghiChu: ghiChu
                    );
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Lỗi ghi log hệ thống: {ex.Message}");
                }
            });
        }
        /// <summary>
        /// Bấm dấu X mà còn thay đổi chưa lưu → hỏi lại để không mất công tích chọn.
        /// </summary>
        private void Form40_BoQuaDonViCanhBaoTyLe_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_dangLuu) { e.Cancel = true; return; }                       // đang ghi CSDL: không cho đóng giữa chừng
            if (_daLuu || !_daNapXong || _dsDonVi == null) return;
            if (e.CloseReason != CloseReason.UserClosing) return;            // tắt máy / thoát hệ thống: không chặn
            if (!_dsDonVi.CoThayDoi()) return;
            var kq = MessageBox.Show(this,
                "Danh sách đơn vị đã thay đổi nhưng chưa được lưu.\n\nBạn có muốn lưu trước khi đóng không?",
                "Xác nhận", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (kq == DialogResult.Cancel)
            {
                e.Cancel = true;
            }
            else if (kq == DialogResult.Yes)
            {
                e.Cancel = true;            // hủy lần đóng này, lưu xong sẽ tự đóng lại
                _ = LuuRoiDongAsync();
            }
            // No: đóng và bỏ thay đổi
        }

        //  HƯỚNG DẪN

        private const string NoiDungHuongDan =
            "    PHẦN MỀM THỐNG KÊ & PHÂN TÍCH THI ĐUA CHUYÊN SÂU 2026\n\n" +
            "CHỨC NĂNG: CẤU HÌNH MIỄN TRỪ & BỎ QUA CẢNH BÁO TỶ LỆ\n\n" +
            "1. Mục đích nghiệp vụ:\n" +
            "   - Cho phép thiết lập danh sách các đơn vị có tính chất đặc thù\n" +
            "     trong lực lượng (Ví dụ: BCH Tiểu đoàn, các Ban tham mưu,\n" +
            "     bộ phận chuyên trách, cơ yếu...) không phải áp dụng cứng nhắc\n" +
            "     hạn mức tỷ lệ phân loại thi đua quy định của hệ thống.\n\n" +
            "2. Cơ chế hoạt động:\n" +
            "   - Đơn vị nào được TÍCH CHỌN trong danh sách này sẽ được hệ thống\n" +
            "     tự động MIỄN TRỪ cảnh báo lỗi (tô màu đỏ) trên bảng thống kê\n" +
            "     quân số thi đua chính.\n" +
            "   - Định dạng hiển thị của các đơn vị đặc biệt này sẽ được đồng bộ\n" +
            "     như trạng thái an toàn (chữ màu xanh lá, in thường).\n\n" +
            "3. Hướng dẫn sử dụng nhanh:\n" +
            "   - Nhấp chuột 1 chạm vào tên đơn vị để Bật/Tắt dấu tích\n" +
            "     (hoặc dùng phím ↑ ↓ và phím Cách).\n" +
            "   - Gõ vào ô tìm kiếm để lọc nhanh (gõ không dấu cũng được),\n" +
            "     rồi dùng [Chọn tất cả] / [Bỏ chọn] cho các đơn vị đang hiển thị.\n" +
            "   - Nhấn nút [Đóng và lưu] để áp dụng cấu hình vào hệ thống.\n" +
            "     Nếu bấm dấu X khi còn thay đổi chưa lưu, hệ thống sẽ hỏi lại.\n\n" +
            "(*) Lưu ý: Cấu hình này sẽ được lưu trữ vĩnh viễn cho các phiên\n" +
            "    làm việc tiếp theo và tự động ghi nhật ký hệ thống ngầm.";
        private void pictureBox1_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(
                this,
                NoiDungHuongDan,
                "Hướng dẫn nghiệp vụ hệ thống",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
    }
}