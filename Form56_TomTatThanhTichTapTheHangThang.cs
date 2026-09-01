using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using System.Runtime.InteropServices;

namespace PhanMemThiDua2026
{
    public partial class Form56_TomTatThanhTichTapTheHangThang : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private const int GioiHanKhuyenNghi = 830;
        private const float RichText_MinFontSize = 8f;
        private const float RichText_MaxFontSize = 30f;
        private const float RichText_FontStep = 1f;
        private static readonly SemaphoreSlim _luuSemaphore = new(1, 1);
        
        private bool _daLoadDuLieuThanhCong;
        private bool _dangLuu;
        // ============================================================
        // CĂN ĐỀU ĐOẠN VĂN CHO RICHTEXTBOX
        // - Chỉ thay đổi định dạng hiển thị.
        // - Không thay đổi Text.
        // - Không ảnh hưởng dữ liệu lưu CSDL.
        // - Không sử dụng SendKeys / Ctrl+J giả lập.
        // ============================================================

        private const int WM_USER = 0x0400;
        private const int EM_SETPARAFORMAT = WM_USER + 71;

        private const uint PFM_ALIGNMENT = 0x00000008;
        private const short PFA_JUSTIFY = 4;

        [StructLayout(LayoutKind.Sequential)]
        private struct PARAFORMAT
        {
            public int cbSize;
            public uint dwMask;
            public short wNumbering;
            public short wReserved;
            public int dxStartIndent;
            public int dxRightIndent;
            public int dxOffset;
            public short wAlignment;
            public short cTabCount;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public int[] rgxTabs;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(
            IntPtr hWnd,
            int msg,
            IntPtr wParam,
            ref PARAFORMAT lParam);

        private bool _dangCanDeu = false;

        private void CanDeuRichTextBox()
        {
            if (_dangCanDeu)
                return;

            if (richTextBox1_TomTatThanhTichTapThe == null ||
                richTextBox1_TomTatThanhTichTapThe.IsDisposed ||
                !richTextBox1_TomTatThanhTichTapThe.IsHandleCreated)
            {
                return;
            }

            if (richTextBox1_TomTatThanhTichTapThe.TextLength == 0)
                return;

            _dangCanDeu = true;

            int selectionStart =
                richTextBox1_TomTatThanhTichTapThe.SelectionStart;

            int selectionLength =
                richTextBox1_TomTatThanhTichTapThe.SelectionLength;

            try
            {
                richTextBox1_TomTatThanhTichTapThe.SelectAll();

                var paraFormat = new PARAFORMAT
                {
                    cbSize = Marshal.SizeOf<PARAFORMAT>(),
                    dwMask = PFM_ALIGNMENT,
                    wAlignment = PFA_JUSTIFY,
                    rgxTabs = new int[32]
                };

                SendMessage(
                    richTextBox1_TomTatThanhTichTapThe.Handle,
                    EM_SETPARAFORMAT,
                    IntPtr.Zero,
                    ref paraFormat);
            }
            finally
            {
                // Khôi phục vị trí con trỏ / vùng chọn
                int textLength =
                    richTextBox1_TomTatThanhTichTapThe.TextLength;

                selectionStart =
                    Math.Max(0, Math.Min(selectionStart, textLength));

                selectionLength =
                    Math.Max(
                        0,
                        Math.Min(
                            selectionLength,
                            textLength - selectionStart));

                richTextBox1_TomTatThanhTichTapThe.Select(
                    selectionStart,
                    selectionLength);

                _dangCanDeu = false;
            }
        }
        // Nội dung thực tế đã được đọc thành công từ CSDL khi mở Form.
        // Dùng làm mốc an toàn để biết dữ liệu ban đầu là gì.
        private string _noiDungDaTaiTuCSDL = string.Empty;
        public Form56_TomTatThanhTichTapTheHangThang()
        {
            InitializeComponent();
            ShowInTaskbar = false;
            MaximizeBox = false;
            MinimizeBox = false;
            CauHinhStatusStrip();
            richTextBox1_TomTatThanhTichTapThe.TextChanged += richTextBox1_TomTatThanhTichTapThe_TextChanged;
        }
        private async void Form56_TomTatThanhTichTapTheHangThang_Load(object sender, EventArgs e)
        {
            kryptonButton_LuuDataDeNghi.Enabled = false;
            AcceptButton = kryptonButton_LuuDataDeNghi;
            InitToolTips();
            toolStripStatusLabel1_ThongBao.Text = string.Empty;
            await TaiDuLieuTomTatAsync();
            if (_daLoadDuLieuThanhCong)
            {
                kryptonButton_LuuDataDeNghi.Enabled = true;
                CapNhatSoLuongKyTu();
                // Căn đều toàn bộ nội dung sau khi tải từ CSDL
                CanDeuRichTextBox();
                DuaConTroVeCuoiVanBan();
            }
        }
        private void CauHinhStatusStrip()
        {
            if (toolStripStatusLabel1_ThongBao == null ||
                toolStripStatusLabel1_SoLuongKyTu == null)
            {
                return;
            }

            // ============================================================
            // 1. SỐ LƯỢNG KÝ TỰ
            //    - Luôn căn trái
            //    - Có icon
            // ============================================================

            toolStripStatusLabel1_SoLuongKyTu.TextAlign =
                ContentAlignment.MiddleLeft;

            toolStripStatusLabel1_SoLuongKyTu.ImageAlign =
                ContentAlignment.MiddleLeft;

            toolStripStatusLabel1_SoLuongKyTu.TextImageRelation =
                TextImageRelation.ImageBeforeText;

            // Cho phép label này chiếm phần không gian bên trái
            toolStripStatusLabel1_SoLuongKyTu.Spring = true;


            // ============================================================
            // 2. THÔNG BÁO
            //    - Luôn căn phải
            // ============================================================

            toolStripStatusLabel1_ThongBao.TextAlign =
                ContentAlignment.MiddleRight;

            toolStripStatusLabel1_ThongBao.Spring = false;
        }
        private void CapNhatSoLuongKyTu()
        {
            if (richTextBox1_TomTatThanhTichTapThe == null ||
                toolStripStatusLabel1_SoLuongKyTu == null)
            {
                return;
            }

            int soKyTu = richTextBox1_TomTatThanhTichTapThe.TextLength;

            string noiDung;
            Color mauChu;

            if (soKyTu == 0)
            {
                noiDung = $"Khuyến nghị đoạn văn giới hạn {GioiHanKhuyenNghi} ký tự (Tối đa {Module_BaNhat.GioiHanToiDa} ký tự)";
                mauChu = SystemColors.ControlText;
            }
            else if (soKyTu > Module_BaNhat.GioiHanToiDa)
            {
                noiDung = $"Quá giới hạn ký tự cho phép {soKyTu}/{Module_BaNhat.GioiHanToiDa} ký tự (Không thể lưu!)";
                mauChu = Color.DarkRed;
            }
            else if (soKyTu > GioiHanKhuyenNghi)
            {
                noiDung = $"Đã vượt quá khuyến nghị {soKyTu - GioiHanKhuyenNghi} ký tự";
                mauChu = Color.Red;
            }
            else
            {
                noiDung = $"Số lượng ký tự {soKyTu}/{GioiHanKhuyenNghi} ký tự khuyến nghị";
                mauChu = SystemColors.ControlText;
            }

            // Chỉ cập nhật khi thực sự thay đổi
            if (!string.Equals(toolStripStatusLabel1_SoLuongKyTu.Text, noiDung, StringComparison.Ordinal))
            {
                toolStripStatusLabel1_SoLuongKyTu.Text = noiDung;
            }

            if (toolStripStatusLabel1_SoLuongKyTu.ForeColor != mauChu)
            {
                toolStripStatusLabel1_SoLuongKyTu.ForeColor = mauChu;
            }
        }
        // ⭐ SỰ KIỆN MỚI: Gọi hàm đếm tự động mỗi khi người dùng gõ phím
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
            toolTip1.ToolTipIcon = ToolTipIcon.Info;

            // UX: phản hồi nhanh – không gây khó chịu khi rê chuột qua
            toolTip1.InitialDelay = 300;
            toolTip1.AutoPopDelay = 2500;
            toolTip1.ReshowDelay = 100;
            toolTip1.ShowAlways = true;

            // Bản đồ ánh xạ các nút điều khiển và nội dung hướng dẫn tương ứng
            var tips = new Dictionary<Control, string>
    {
        { kryptonButton2_GiamCoChuRichText, "Giảm cỡ chữ" },
        { kryptonButton2_TangCoChuRichText, "Tăng cỡ chữ" },
        { kryptonButton_LuuDataDeNghi, "Lưu thông tin thi đua tập thể phong trào thi đua 'Vì ANTQ' vào cơ sở dữ liệu" }

    };

            foreach (var tip in tips)
            {
                // Kiểm tra an toàn để tránh lỗi NullReference nếu nút chưa được khởi tạo hoặc bị hủy
                if (tip.Key != null && !tip.Key.IsDisposed)
                {
                    toolTip1.SetToolTip(tip.Key, tip.Value);
                }
            }
        }
        private void richTextBox1_TomTatThanhTichTapThe_TextChanged(
     object sender,
     EventArgs e)
        {
            CapNhatSoLuongKyTu();
            KiemTraHienThiNutCoChu();
            // Tự động căn đều toàn bộ nội dung
            CanDeuRichTextBox();
        }
        private async Task TaiDuLieuTomTatAsync()
        {
            _daLoadDuLieuThanhCong = false;
            _noiDungDaTaiTuCSDL = string.Empty;

            if (string.IsNullOrWhiteSpace(_csdl2Path))
            {
                MessageBox.Show(
                    "Không xác định được đường dẫn CSDL2.",
                    "Lỗi dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            if (!File.Exists(_csdl2Path))
            {
                MessageBox.Show(
                    "Không tìm thấy cơ sở dữ liệu CSDL2:\n\n" +
                    _csdl2Path,
                    "Lỗi dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            try
            {
                var connectionString =
                    new SqliteConnectionStringBuilder
                    {
                        DataSource = _csdl2Path,
                        Mode = SqliteOpenMode.ReadOnly,
                        Cache = SqliteCacheMode.Private,
                        Pooling = false
                    }.ToString();

                using var conn = new SqliteConnection(connectionString);

                await conn.OpenAsync();

                using (var cmdPragma = conn.CreateCommand())
                {
                    cmdPragma.CommandText = @"
PRAGMA busy_timeout = 15000;
PRAGMA query_only = ON;";

                    await cmdPragma.ExecuteNonQueryAsync();
                }

                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"
SELECT TomTatGhiChu
FROM ThongTin
WHERE ID = 1;";

                object? result = await cmd.ExecuteScalarAsync();

                
                // ID = 1 BẮT BUỘC PHẢI TỒN TẠI
                

                if (result == null || result == DBNull.Value)
                {
                    throw new InvalidOperationException(
                        "Không tìm thấy ThongTin.ID = 1.\n\n" +
                        "Theo thiết kế hệ thống, bản ghi này luôn phải tồn tại.");
                }

                string duLieuMaHoa =
                    result.ToString() ?? string.Empty;

                string noiDungGiaiMa;

                
                // CỘT RỖNG = CHƯA CÓ NỘI DUNG
                

                if (string.IsNullOrEmpty(duLieuMaHoa))
                {
                    noiDungGiaiMa = string.Empty;
                }
                else
                {
                    try
                    {
                        noiDungGiaiMa =
                            BaoMatAES.GiaiMa(duLieuMaHoa) ?? string.Empty;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            "Không thể giải mã ThongTin.TomTatGhiChu " +
                            "của ID = 1.\n\n" +
                            "Dữ liệu có thể đã bị ghi đè hoặc bị hỏng.",
                            ex);
                    }
                }

                
                // ⭐ CỰC KỲ QUAN TRỌNG
                // LƯU LẠI BẢN GỐC ĐỌC ĐƯỢC TỪ CSDL
                

                _noiDungDaTaiTuCSDL = noiDungGiaiMa;

                
                // HIỂN THỊ LÊN FORM
                

                richTextBox1_TomTatThanhTichTapThe.Text =
                    noiDungGiaiMa;

                _daLoadDuLieuThanhCong = true;
            }
            catch (SqliteException ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[Form56] SQLITE LOAD ERROR:");

                System.Diagnostics.Debug.WriteLine(ex);

                MessageBox.Show(
                    "Không thể đọc TomTatGhiChu từ CSDL2.\n\n" +
                    "Dữ liệu trên giao diện chưa được phép ghi đè vào CSDL.\n\n" +
                    "Chi tiết kỹ thuật:\n" +
                    ex.Message,
                    "Không thể tải dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[Form56] LOAD ERROR:");

                System.Diagnostics.Debug.WriteLine(ex);

                MessageBox.Show(
                    "Không thể tải TomTatGhiChu.\n\n" +
                    "Vì lý do an toàn, hệ thống không cho phép lưu khi " +
                    "dữ liệu ban đầu chưa được đọc thành công.\n\n" +
                    "Chi tiết:\n" +
                    ex.Message,
                    "Lỗi dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private async Task LuuTomTatGhiChuAsync(string noiDungGoc)
        {
            if (string.IsNullOrWhiteSpace(_csdl2Path))
            {
                throw new InvalidOperationException(
                    "Đường dẫn CSDL2 đang rỗng.");
            }

            if (!File.Exists(_csdl2Path))
            {
                throw new FileNotFoundException(
                    "Không tìm thấy CSDL2.",
                    _csdl2Path);
            }    
            // 1. MÃ HÓA
            string noiDungMaHoa =string.IsNullOrEmpty(noiDungGoc) ?
                string.Empty: BaoMatAES.MaHoa(noiDungGoc);     
            // 2. MỞ CONNECTION RIÊNG
            var connectionString =
                new SqliteConnectionStringBuilder
                {
                    DataSource = _csdl2Path,
                    Mode = SqliteOpenMode.ReadWrite,
                    Cache = SqliteCacheMode.Private,
                    Pooling = false
                }.ToString();

            using var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync();      
            // 3. SQLITE
            using (var cmdPragma = conn.CreateCommand())
            {
                cmdPragma.CommandText = @"
PRAGMA busy_timeout = 15000;
PRAGMA synchronous = FULL;
PRAGMA foreign_keys = ON;";

                await cmdPragma.ExecuteNonQueryAsync();
            }   
            // 4. KIỂM TRA LẠI DỮ LIỆU NGAY TRƯỚC KHI GHI
            //
            // Không để Form56 ghi đè dữ liệu mà một Form khác vừa thay đổi.
            

            using (var cmdCheck = conn.CreateCommand())
            {
                cmdCheck.CommandText = @"
SELECT TomTatGhiChu
FROM ThongTin
WHERE ID = 1;";

                object? result =
                    await cmdCheck.ExecuteScalarAsync();

                if (result == null || result == DBNull.Value)
                {
                    throw new InvalidOperationException(
                        "Không tìm thấy ThongTin.ID = 1.\n\n" +
                        "Giao dịch lưu đã bị hủy.");
                }

                string duLieuMaHoaHienTai =
                    result.ToString() ?? string.Empty;

                string noiDungHienTai =
                    string.IsNullOrEmpty(duLieuMaHoaHienTai)
                        ? string.Empty
                        : BaoMatAES.GiaiMa(duLieuMaHoaHienTai) ?? string.Empty;

                if (!string.Equals(
                        noiDungHienTai,
                        _noiDungDaTaiTuCSDL,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "DỮ LIỆU ĐÃ THAY ĐỔI BỞI MỘT THAO TÁC KHÁC.\n\n" +
                        "Dữ liệu trong CSDL hiện tại không còn giống " +
                        "dữ liệu lúc Form56 được mở.\n\n" +
                        "Hệ thống đã HỦY thao tác lưu để tránh ghi đè " +
                        "và làm mất dữ liệu mới.\n\n" +
                        "Vui lòng đóng Form56 và mở lại.");
                }
            }

            
            // 5. TRANSACTION
            

            using var transaction =
                conn.BeginTransaction();

            try
            {
                // ============================================================
                // 6. CHỈ UPDATE DUY NHẤT TomTatGhiChu CỦA ID = 1
                // ============================================================

                using var cmd =
                    conn.CreateCommand();

                cmd.Transaction =
                    transaction;

                cmd.CommandText = @"
UPDATE ThongTin
SET TomTatGhiChu = @TomTatGhiChu
WHERE ID = 1;";

                cmd.Parameters.Add(
                    new SqliteParameter(
                        "@TomTatGhiChu",
                        DbType.String)
                    {
                        Value = noiDungMaHoa
                    });

                int soDongAnhHuong =
                    await cmd.ExecuteNonQueryAsync();

                if (soDongAnhHuong != 1)
                {
                    throw new InvalidOperationException(
                        "SQLite không cập nhật đúng ThongTin.ID = 1.\n\n" +
                        $"Số dòng bị ảnh hưởng: {soDongAnhHuong}.\n\n" +
                        "Giao dịch đã bị hủy.");
                }

                // ============================================================
                // 7. VERIFY TRONG TRANSACTION
                // ============================================================

                using var cmdVerify =
                    conn.CreateCommand();

                cmdVerify.Transaction =
                    transaction;

                cmdVerify.CommandText = @"
SELECT TomTatGhiChu
FROM ThongTin
WHERE ID = 1;";

                object? verifyResult =
                    await cmdVerify.ExecuteScalarAsync();

                if (verifyResult == null ||
                    verifyResult == DBNull.Value)
                {
                    throw new InvalidOperationException(
                        "Không đọc lại được TomTatGhiChu " +
                        "của ID = 1 trước COMMIT.");
                }

                string duLieuVerify =
                    verifyResult.ToString() ?? string.Empty;

                if (!string.Equals(
                        duLieuVerify,
                        noiDungMaHoa,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "VERIFY thất bại.\n\n" +
                        "Dữ liệu trước COMMIT không đúng với dữ liệu yêu cầu lưu.");
                }

                // ============================================================
                // 8. COMMIT
                // ============================================================

                await transaction.CommitAsync();
            }
            catch
            {
                try
                {
                    await transaction.RollbackAsync();
                }
                catch (Exception rollbackEx)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "[Form56] ROLLBACK ERROR:");

                    System.Diagnostics.Debug.WriteLine(
                        rollbackEx);
                }

                throw;
            }

            
            // 9. VERIFY SAU COMMIT
            

            await KiemTraDuLieuSauKhiCommitAsync(
                noiDungGoc);
        }
        private async void kryptonButton_LuuDataDeNghi_Click(object sender, EventArgs e)
        {
            if (_dangLuu)
                return;
            if (!_daLoadDuLieuThanhCong)
            {
                MessageBox.Show(
                    "Dữ liệu ban đầu chưa được tải thành công.\n\n" +
                    "Vì lý do an toàn, hệ thống không cho phép lưu.",
                    "Không thể lưu dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (richTextBox1_TomTatThanhTichTapThe == null ||
                richTextBox1_TomTatThanhTichTapThe.IsDisposed)
            {
                MessageBox.Show(
                    "Không xác định được vùng nhập nội dung.",
                    "Lỗi giao diện",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            int soKyTu =
                richTextBox1_TomTatThanhTichTapThe.TextLength;

            if (soKyTu > Module_BaNhat.GioiHanToiDa)
            {
                MessageBox.Show(
                    $"Nội dung hiện có {soKyTu} ký tự, vượt quá giới hạn " +
                    $"cho phép ({Module_BaNhat.GioiHanToiDa} ký tự).\n\n" +
                    "Vui lòng rút gọn nội dung trước khi lưu.",
                    "Không thể lưu dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                richTextBox1_TomTatThanhTichTapThe.Focus();
                return;
            }

            bool daGiuKhoa = false;

            string tenNutGoc =
                kryptonButton_LuuDataDeNghi.Text;

            try
            {
                
                // GIỮ KHÓA CHỐNG CLICK LẶP
                

                await _luuSemaphore.WaitAsync();

                daGiuKhoa = true;

                if (_dangLuu)
                    return;
                _dangLuu = true;       
                // LẤY DỮ LIỆU SAU KHI ĐÃ GIỮ KHÓA           
                string noiDungGoc = richTextBox1_TomTatThanhTichTapThe.Text ?? string.Empty;
                // KHÓA UI
                kryptonButton_LuuDataDeNghi.Text = "Đang lưu...";
                kryptonButton_LuuDataDeNghi.Enabled = false;
                kryptonButton_LuuDataDeNghi.Refresh();  
                // KIỂM TRA CSDL
                if (string.IsNullOrWhiteSpace(_csdl2Path))
                {
                    throw new InvalidOperationException(
                        "Đường dẫn CSDL2 đang rỗng.");
                }
                if (!File.Exists(_csdl2Path))
                {
                    throw new FileNotFoundException(
                        "Không tìm thấy CSDL2.",
                        _csdl2Path);
                }      
                // LƯU + VERIFY
                await LuuTomTatGhiChuAsync(noiDungGoc);              
                // CHỈ CẬP NHẬT MỐC SAU KHI VERIFY THÀNH CÔNG
                _noiDungDaTaiTuCSDL =
                    noiDungGoc;             
                // GHI NHẬT KÝ
                Module_NhatKy.GhiNhatKy(
                    taiKhoan:
                        string.IsNullOrWhiteSpace(
                            Module_TaiKhoan.TenTaiKhoan_RAM)
                            ? "Không xác định"
                            : Module_TaiKhoan.TenTaiKhoan_RAM,
                    hanhDong:
                        "Cập nhật thành tích tập thể phong trào thi đua Vì ANTQ" +
                        "(ThongTin.TomTatGhiChu)",

                    ghiChu:
                        $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                );
                // THÔNG BÁO THÀNH CÔNG
                await HienThongBaoLuuThanhCongAsync();
                await Task.Delay(600);
                if (!IsDisposed && !Disposing)
                {
                    Close();
                }
            }
            catch (SqliteException ex)
            {
                System.Diagnostics.Debug.WriteLine("[Form56] SQLITE SAVE ERROR:");
                System.Diagnostics.Debug.WriteLine(ex);
                MessageBox.Show(
                    "Không thể lưu dữ liệu vào cơ sở dữ liệu.\n\n" +
                    "Giao dịch đã được hủy nếu chưa COMMIT.\n\n" +
                    "Dữ liệu trên màn hình vẫn được giữ nguyên.\n\n" +
                    "Chi tiết kỹ thuật:\n" +
                    ex.Message,
                    "Lưu dữ liệu thất bại",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Form56] SAVE ERROR:");
                System.Diagnostics.Debug.WriteLine(ex);
                MessageBox.Show(
                    ex.Message,
                    "Không thể lưu dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                _dangLuu = false;

                if (!IsDisposed &&
                    !Disposing &&
                    kryptonButton_LuuDataDeNghi != null &&
                    !kryptonButton_LuuDataDeNghi.IsDisposed)
                {
                    kryptonButton_LuuDataDeNghi.Text =
                        tenNutGoc;
                    kryptonButton_LuuDataDeNghi.Enabled =
                        _daLoadDuLieuThanhCong;
                }
                if (daGiuKhoa)
                {
                    _luuSemaphore.Release();
                }
            }
        }
        private async Task KiemTraDuLieuSauKhiCommitAsync(string noiDungMongDoi)
        {
            if (string.IsNullOrWhiteSpace(_csdl2Path))
            {
                throw new InvalidOperationException(
                    "Không thể xác minh vì đường dẫn CSDL2 rỗng.");
            }

            var connectionString =
                new SqliteConnectionStringBuilder
                {
                    DataSource = _csdl2Path,
                    Mode = SqliteOpenMode.ReadOnly,
                    Cache = SqliteCacheMode.Private,
                    Pooling = false
                }.ToString();

            using var conn =
                new SqliteConnection(connectionString);

            await conn.OpenAsync();

            using (var cmdPragma = conn.CreateCommand())
            {
                cmdPragma.CommandText =
                    "PRAGMA busy_timeout = 15000;";

                await cmdPragma.ExecuteNonQueryAsync();
            }

            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
SELECT TomTatGhiChu
FROM ThongTin
WHERE ID = 1;";

            object? result =
                await cmd.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "NGHIÊM TRỌNG: Sau COMMIT không còn " +
                    "Thay đổi dữ liệu ID = 1.");
            }

            string duLieuMaHoa =
                result.ToString() ?? string.Empty;

            string noiDungThucTe;

            if (string.IsNullOrEmpty(duLieuMaHoa))
            {
                noiDungThucTe = string.Empty;
            }
            else
            {
                try
                {
                    noiDungThucTe =
                        BaoMatAES.GiaiMa(duLieuMaHoa) ?? string.Empty;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        "Dữ liệu TomTatGhiChu sau COMMIT " +
                        "không thể giải mã.",
                        ex);
                }
            }

            if (!string.Equals(
                    noiDungThucTe,
                    noiDungMongDoi,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "NGHIÊM TRỌNG: SQLite đã COMMIT nhưng " +
                    "connection mới đọc lại dữ liệu không giống " +
                    "nội dung vừa lưu.");
            }
        }
        private async Task HienThongBaoLuuThanhCongAsync()
        {
            if (IsDisposed || Disposing ||
                toolStripStatusLabel1_ThongBao == null ||
                toolStripStatusLabel1_ThongBao.IsDisposed)
            {
                return;
            }

            toolStripStatusLabel1_ThongBao.Text =
                "Đã lưu tóm tắt thành tích vào cơ sở dữ liệu.";

            toolStripStatusLabel1_ThongBao.Visible = true;
            Module_ThongBao.ThanhCong($"Đã cập nhật tóm tắt thành tích");
            await Task.Delay(600);

            if (IsDisposed || Disposing ||
                toolStripStatusLabel1_ThongBao.IsDisposed)
            {
                return;
            }

            toolStripStatusLabel1_ThongBao.Text = string.Empty;
            toolStripStatusLabel1_ThongBao.Visible = false;
        }
        private void DuaConTroVeCuoiVanBan()
        {
            if (richTextBox1_TomTatThanhTichTapThe == null ||
                richTextBox1_TomTatThanhTichTapThe.IsDisposed)
            {
                return;
            }

            richTextBox1_TomTatThanhTichTapThe.Focus();

            int viTriCuoi =
                richTextBox1_TomTatThanhTichTapThe.TextLength;

            richTextBox1_TomTatThanhTichTapThe.SelectionStart = viTriCuoi;
            richTextBox1_TomTatThanhTichTapThe.SelectionLength = 0;

            // Cuộn đến vị trí con trỏ
            richTextBox1_TomTatThanhTichTapThe.ScrollToCaret();
        }
        private void ThayDoiCoChuRichText(float delta)
        {
            if (richTextBox1_TomTatThanhTichTapThe == null ||
                richTextBox1_TomTatThanhTichTapThe.IsDisposed)
            {
                return;
            }

            Font fontHienTai =
                richTextBox1_TomTatThanhTichTapThe.Font;

            if (fontHienTai == null)
                return;

            float kichThuocMoi =
                fontHienTai.Size + delta;

           
            // ĐẠT CỠ CHỮ TỐI ĐA
           

            if (kichThuocMoi >= RichText_MaxFontSize)
            {
                kichThuocMoi = RichText_MaxFontSize;

                if (fontHienTai.Size >= RichText_MaxFontSize)
                {
                    MessageBox.Show(
                        $"Cỡ chữ đã đạt mức tối đa {RichText_MaxFontSize:0}pt.",
                        "Cỡ chữ",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    return;
                }
            }

           
            // ĐẠT CỠ CHỮ TỐI THIỂU
           

            if (kichThuocMoi <= RichText_MinFontSize)
            {
                kichThuocMoi = RichText_MinFontSize;

                if (fontHienTai.Size <= RichText_MinFontSize)
                {
                    MessageBox.Show(
                        $"Cỡ chữ đã đạt mức tối thiểu {RichText_MinFontSize:0}pt.",
                        "Cỡ chữ",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    return;
                }
            }
            // Không có thay đổi thực tế
            if (Math.Abs(kichThuocMoi - fontHienTai.Size) < 0.01f)
                return;

            richTextBox1_TomTatThanhTichTapThe.SuspendLayout();

            try
            {
                richTextBox1_TomTatThanhTichTapThe.Font =
                    new Font(
                        fontHienTai.FontFamily,
                        kichThuocMoi,
                        fontHienTai.Style,
                        GraphicsUnit.Point);

                richTextBox1_TomTatThanhTichTapThe.Focus();
            }
            finally
            {
                richTextBox1_TomTatThanhTichTapThe.ResumeLayout();
            }
        }
        private void kryptonButton2_TangCoChuRichText_Click(object sender, EventArgs e)
        {
            ThayDoiCoChuRichText(RichText_FontStep);
        }
        private void kryptonButton2_GiamCoChuRichText_Click(object sender, EventArgs e){
            ThayDoiCoChuRichText(-RichText_FontStep);
        }
        private void KiemTraHienThiNutCoChu()
        {
            // Kiểm tra xem RichTextBox có nội dung không (bỏ qua khoảng trắng/xuống dòng thừa)
            bool coNoiDung = !string.IsNullOrWhiteSpace(richTextBox1_TomTatThanhTichTapThe.Text);

            // Ẩn/Hiện 2 nút dựa trên kết quả kiểm tra
            kryptonButton2_TangCoChuRichText.Visible = coNoiDung;
            kryptonButton2_GiamCoChuRichText.Visible = coNoiDung;
        }
    }
}