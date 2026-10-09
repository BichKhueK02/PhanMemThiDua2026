using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Globalization;   // thêm vào đầu file
using System.Runtime.InteropServices;
using System.Text;
namespace PhanMemThiDua2026
{
    public partial class Form4_TrangDauTien : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private Form14? formTinhToan;
        private Panel? piePanel;
        private int highlightedSlice = -1;
        private Dictionary<string, int> pieData = new Dictionary<string, int>();
        private int[] loai1;
        private int[] loai2;
        private int[] loai3;
        private int phanTramLoai1 = 0;
        private int phanTramLoai2 = 0;
        private int phanTramLoai3 = 0;
        private int _cacheCounter = 0; // Đếm siêu nhẹ thay vì dùng .Count
        private int _isClearingCache = 0;
        private bool isComboBoxInitDone = false;
        private bool _allowLoadBang2 = true;
        private readonly Icon _appIcon;
        private readonly Image _iconTrue = Properties.Resources._true;   // Đảm bảo tên file trong Resources là true
        private readonly Image _iconFalse = Properties.Resources._false; // Đảm bảo tên file trong Resources là false                                                                         // private Dictionary<string, string> _dictChiHuyD = new Dictionary<string, string>();
        private Font? _cachedGridFont;
        private Font? _cachedGridFontBold;
        private Font? _cachedGrid2HeaderFont;
        private int _cachedTongBCH = -1; // -1 nghĩa là chưa đếm
        private string _textGocNutMayTinh = null;
        private Image _anhGocNutMayTinh = null;
        // Khai báo Dictionary hỗ trợ tìm kiếm không phân biệt chữ hoa/chữ thường
        private Dictionary<string, string> _dictChiHuyD = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Form48_XuatTepPdf _form48; // giữ sống suốt vòng đời form cha
        private string _textGocNutXuatPdf = null;
        private Image _anhGocNutXuatPdf = null;
        private bool _hasLoaded = false; // Cờ bảo hiểm chặn chạy trùng lặp hàm Load             
        private bool _daKhoiTaoToolTip = false;
        private string _textGocNutKiemTra = null;
        private Image _anhGocNutKiemTra = null;
        private Form11_KiemTraTyLe form11;
        private const int ChieuCao_TieuDe_BangDataGird = 44;
        private string? _textGocNutTomTatThanhTich;
        private bool _dangMoMayTinh = false;
        private bool _dangMoForm11 = false;
        //Nhóm API để gọi mở thư mục bằng File Explorer
        //  PHẦN THÊM MỚI: TỐI ƯU RAM 
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
        private const int SW_RESTORE = 9;
        public void ResetCacheBCH()
        {
            _cachedTongBCH = -1;
        }
        public Form4_TrangDauTien()
        {
            InitializeComponent();
            Module_HeThong.BocBoTronGiaoDien(listBox1);
            BatDoubleBuffer(kryptonDataGridView1);
            BatDoubleBuffer(kryptonDataGridView2);
            try
            {
                using (var ms = new System.IO.MemoryStream(Properties.Resources.ic_PhanMem))
                {
                    _appIcon = new Icon(ms);
                }
                this.Icon = _appIcon; // Gán icon cho Form
            }
            catch (Exception)
            {
                // Nếu lỗi, sử dụng icon mặc định của hệ thống để không crash app
            }
            Shown += Form4_Shown;
            Load += Form4_Load;
            // Thêm dòng này để đăng ký sự kiện tự động căn chỉnh
            this.Resize += Form4_TrangDauTien_Resize;
            InitToolTips();
            com_DeNghi.SelectedIndexChanged += Com_DeNghi_SelectedIndexChanged;
            comboBox1_ChonLoaiBaoCao.SelectedIndexChanged -= comboBox1_ChonLoaiBaoCao_SelectedIndexChanged;
            comboBox1_ChonLoaiBaoCao.SelectedIndexChanged += comboBox1_ChonLoaiBaoCao_SelectedIndexChanged;
            // ĐÚNG: Tên_Control.Tên_Sự_Kiện [+=/-=] Tên_Hàm_Xử_Lý
            comboBox1_CheDoXetThiDua.SelectedIndexChanged -= comboBox1_CheDoXetThiDua_SelectedIndexChanged;
            comboBox1_CheDoXetThiDua.SelectedIndexChanged += comboBox1_CheDoXetThiDua_SelectedIndexChanged;
            if (kryptonDataGridView2 != null)
            {
                kryptonDataGridView2.CellPainting += KryptonDataGridView2_CellPainting;
            }
            // XÓA BEGININVOKE ĐI. CHẠY TRỰC TIẾP NHƯ THẾ NÀY:
            comboBox_DiaDiem.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            comboBox_DiaDiem.AutoCompleteSource = AutoCompleteSource.ListItems;
        }
        private async void Form4_Load(object? sender, EventArgs e)
        {
            if (_hasLoaded) return;
            _hasLoaded = true;
            try
            {
                // 1. Đăng ký sự kiện trước
                Module_DanduongGPS.OnDatabaseChanged -= SuKien_DatabaseChanged;
                Module_DanduongGPS.OnDatabaseChanged += SuKien_DatabaseChanged;
                label11.TextChanged += (s, e) => DieuChinhCoChuLabel11();
                // 2. Nạp cấu hình và dữ liệu nhẹ
                LoadSettings();
                LoadCheckBoxTuDongChonNgayThang();
                Module_QuyDinhTyLe.LoadE29(this.Controls);
                loai1 = Module_QuyDinhTyLe.GetLoaiTapThe("Loai1_TapThe");
                loai2 = Module_QuyDinhTyLe.GetLoaiTapThe("Loai2_TapThe");
                loai3 = Module_QuyDinhTyLe.GetLoaiTapThe("Loai3_TapThe");
                // 3. Đảm bảo CSDL sẵn sàng
                using (var conn = TaoKetNoiCSDL2(readOnly: false))
                {
                    await conn.OpenAsync();
                    await TaoBangCheDoXetThiDuaNamNeuChuaCoAsync(conn, null);
                }
                // 4. Nạp dữ liệu chính cuối cùng
                await ReloadDuLieuAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi trong Form4_Load: " + ex.Message);
            }
        }
        private SqliteConnection TaoKetNoiCSDL2(bool readOnly = false)
        {
            string path = _csdl2Path;
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("Đường dẫn CSDL2 chưa được cấu hình.");
            path = Path.GetFullPath(path);
            if (!File.Exists(path)) throw new FileNotFoundException("Không tìm thấy CSDL2.", path);
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWrite,
                Pooling = true,
                DefaultTimeout = 10
            };
            return new SqliteConnection(builder.ConnectionString);
        }
        private void Com_DeNghi_SelectedIndexChanged(object? sender, EventArgs e)
        {
            LoadQuyDinhTheoDeNghi();
        }
        private void CapNhatLabelPhanTram()
        {
            // 1. Xác định nhãn hiển thị tương ứng (Nếu là Năm thì dùng CSTĐ, CSTT; nếu Tháng dùng Loại 1, Loại 2)
            bool isAnhXa = Module_HeThong.IsKichHoatAnhXa();
            string nhanLoai1 = isAnhXa ? Module_HeThong.PL_CSTD : Module_HeThong.Loai_1;
            string nhanLoai2 = isAnhXa ? Module_HeThong.PL_CSTT : Module_HeThong.Loai_2;
            string nhanLoai3 = isAnhXa ? Module_HeThong.PL_HTNV : Module_HeThong.Loai_3;
            label__PhanTramLoai1.Text = $"{nhanLoai1}: {phanTramLoai1}%";
            label__PhanTramLoai2.Text = $"{nhanLoai2}: {phanTramLoai2}%";
            label__PhanTramLoai3.Text = $"{nhanLoai3}: {phanTramLoai3}%";
            // 2. Xác định màu sắc theo giá trị đang chọn trên com_DeNghi
            Color mau;
            string textDeNghi = com_DeNghi.Text.Trim();
            switch (textDeNghi)
            {
                case Module_HeThong.Loai_1:
                case Module_HeThong.XLDV_DVQT: mau = Color.Green; break;
                case Module_HeThong.Loai_2:
                case Module_HeThong.XLDV_DVTT: mau = Color.Purple; break;
                case Module_HeThong.Loai_3:
                case Module_HeThong.Loai_4:
                case Module_HeThong.XLDV_HTNV:
                case Module_HeThong.XLDV_KHTNV:
                case "Không phân loại":
                case Module_HeThong.PL_KHONG_PL:
                default: mau = Color.Red; break;
            }
            // 3. Áp dụng màu
            label__PhanTramLoai1.ForeColor = mau;
            label__PhanTramLoai2.ForeColor = mau;
            label__PhanTramLoai3.ForeColor = mau;
        }
        private void SetDeNghiVaTinhTyLe()
        {
            Com_DeNghi_SelectedIndexChanged(com_DeNghi, EventArgs.Empty);
        }
        private bool _isSaving = false;
        private async void kryptonButton_LuuThongTin_Click(object? sender, EventArgs e)
        {
            // LỚP VỎ UX: LƯU TRẠNG THÁI GỐC CỦA NÚT
            // Chặn ngay lập tức trước khi chạy bất kỳ Task Async nào
            if (_isSaving) return;
            _isSaving = true;
            string textBanDau = kryptonButton_LuuThongTin.Values.Text;
            Image anhBanDau = kryptonButton_LuuThongTin.Values.Image;
            try
            {
                // THIẾT LẬP TRẠNG THÁI "ĐANG LƯU"
                kryptonButton_LuuThongTin.Enabled = false;
                kryptonButton_LuuThongTin.Values.Text = "Đang lưu...";
                kryptonButton_LuuThongTin.Values.Image = null;
                // Nhịp nghỉ 100ms để giao diện vẽ lại chữ "Đang lưu..."
                await Task.Delay(100);
                // BẮT ĐẦU: 100% CODE GỐC CỦA BẠN (ĐÃ NÂNG CẤP LÊN ASYNC)
                Progress_Start();
                try
                {
                    //  1. Kiểm tra dữ liệu 
                    if (string.IsNullOrWhiteSpace(comboBox_DiaDiem.Text) ||
                        string.IsNullOrWhiteSpace(comboBox_Ngay.Text) ||
                        string.IsNullOrWhiteSpace(comboBox_Thang.Text) ||
                        string.IsNullOrWhiteSpace(comboBox_Nam.Text) ||
                        string.IsNullOrWhiteSpace(comboBox_ChiHuyD.Text) ||
                        string.IsNullOrWhiteSpace(com_DeNghi.Text))
                    {
                        Progress_End();
                        Module_ThongBao.Loi("Chưa khai báo đầy đủ thông tin trên Form!");
                        return;
                    }
                    // Bỏ điều kiện <= 0 hoặc đảm bảo biến đã được cập nhật trước khi lưu
                    // (Ví dụ: cho phép lưu nếu tỷ lệ >= 0)
                    if (phanTramLoai1 < 0 || phanTramLoai2 < 0 || phanTramLoai3 < 0)
                    {
                        Progress_End();
                        Module_ThongBao.Loi("Tỷ lệ phần trăm không hợp lệ!");
                        return;
                    }
                    Progress_Step(10);
                    string csdlPath = _csdl2Path;
                    using (var conn = new SqliteConnection($"Data Source={csdlPath}"))
                    {
                        // ⭐ NÂNG CẤP BẤT ĐỒNG BỘ: OpenAsync
                        await conn.OpenAsync();
                        using (var tran = conn.BeginTransaction())
                        {
                            try
                            {
                                Progress_Step(15);
                                //  2. Chuẩn bị dữ liệu 
                                bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                                string diaDiem = Module_BaoMatAES.MaHoa(comboBox_DiaDiem.Text);
                                string ngay = Module_BaoMatAES.MaHoa(comboBox_Ngay.Text);
                                string thang = Module_BaoMatAES.MaHoa(comboBox_Thang.Text);
                                string nam = Module_BaoMatAES.MaHoa(comboBox_Nam.Text);
                                string chiHuyD = Module_BaoMatAES.MaHoa(comboBox_ChiHuyD.Text);
                                string deNghi = Module_BaoMatAES.MaHoa(com_DeNghi.Text);
                                string ptLoai1 = Module_BaoMatAES.MaHoa(phanTramLoai1.ToString());
                                string ptLoai2 = Module_BaoMatAES.MaHoa(phanTramLoai2.ToString());
                                string ptLoai3 = Module_BaoMatAES.MaHoa(phanTramLoai3.ToString());
                                string loaiBaoCaoRaw = comboBox1_ChonLoaiBaoCao.Text.Trim();
                                string chonTuanRaw = "";
                                if (loaiBaoCaoRaw.Equals("Tuần", StringComparison.OrdinalIgnoreCase))
                                {
                                    chonTuanRaw = comboBox2_ChonSoTuan.Text.Trim();
                                }
                                string loaiBaoCao = Module_BaoMatAES.MaHoa(loaiBaoCaoRaw);
                                string chonTuan = Module_BaoMatAES.MaHoa(chonTuanRaw);
                                // 🟢 CHỈ LẤY GIÁ TRỊ COMBOBOX NẾU LÀ CBCS (!laTanBinh)
                                string cheDoXetThiDua = !laTanBinh
                                    ? (string.IsNullOrWhiteSpace(comboBox1_CheDoXetThiDua.Text) ? "Tháng" : comboBox1_CheDoXetThiDua.Text.Trim())
                                    : "Tháng";
                                Progress_Step(20);
                                //  3. UPSERT bảng ChonLoaiBaoCao 
                                using (var cmd = new SqliteCommand(@"
                                    INSERT INTO ChonLoaiBaoCao (ID, ChonLoaiBaoCao, ChonTuan)
                                    VALUES (1,@Loai,@Tuan)
                                    ON CONFLICT(ID)
                                    DO UPDATE SET
                                    ChonLoaiBaoCao=@Loai,
                                    ChonTuan=@Tuan
                                    ", conn, tran))
                                {
                                    //cmd.Parameters.AddWithValue("@Loai", loaiBaoCao);
                                    // CODE CHUẨN
                                    cmd.Parameters.Add("@Loai", SqliteType.Text).Value = loaiBaoCao;
                                    cmd.Parameters.AddWithValue("@Tuan", chonTuan);
                                    // ⭐ NÂNG CẤP BẤT ĐỒNG BỘ: ExecuteNonQueryAsync
                                    await cmd.ExecuteNonQueryAsync();
                                }
                                // 🟢 CHỈ LƯU BẢNG CheDo_XetThiDuaNam KHI LÀ CBCS (!laTanBinh)
                                if (!laTanBinh)
                                {
                                    using (var cmdCheDo = new SqliteCommand(@"
                                        INSERT INTO CheDo_XetThiDuaNam (ID, ChoPhepCheDoXetThiDuaNam)
                                        VALUES (1, @CheDo)
                                        ON CONFLICT(ID)
                                        DO UPDATE SET ChoPhepCheDoXetThiDuaNam = @CheDo
                                        ", conn, tran))
                                    {
                                        cmdCheDo.Parameters.Add("@CheDo", SqliteType.Text).Value = cheDoXetThiDua;
                                        await cmdCheDo.ExecuteNonQueryAsync();
                                    }
                                }// 🟢 CẬP NHẬT CHUẨN: Gọi Module_HeThong để đồng bộ Cache RAM + Bắn Event
                                 // 🟢 CẮT ĐOẠN SQL THỦ CÔNG CŨ VÀ THAY BẰNG DÒNG NÀY:
                                if (!laTanBinh)
                                {
                                    // Truyền trực tiếp conn và tran của nút Lưu vào để ghi chung 1 Transaction
                                    await Module_HeThong.LuuCheDoXetThiDuaNamAsync(cheDoXetThiDua, conn, tran);
                                }
                                Progress_Step(20);
                                //  3.5 UPSERT bảng ThangHeThong 
                                bool isTanBinh = Module_TaiKhoan.LayPhienBanPhanMem()
                                    .Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                                string thangHTRaw = isTanBinh
                                    ? comboBox2_ChonSoThang.Text.Trim()
                                    : comboBox_Thang.Text.Trim();
                                // 🚀 Validate chặt
                                if (!int.TryParse(thangHTRaw, out int soThang) || soThang < 1 || soThang > 12)
                                {
                                    throw new Exception("Tháng không hợp lệ (1-12)");
                                }
                                // 🚀 Format chuẩn
                                string thangFormat = soThang.ToString("D2");
                                using (var cmdThang = new SqliteCommand(@"
                                INSERT INTO ThangHeThong (ID, Thang)
                                VALUES (1, @Thang)
                                ON CONFLICT(ID)
                                DO UPDATE SET Thang = @Thang
                                ", conn, tran))
                                {
                                    cmdThang.Parameters.Add("@Thang", SqliteType.Text).Value = thangFormat;
                                    // ⭐ NÂNG CẤP BẤT ĐỒNG BỘ: ExecuteNonQueryAsync
                                    await cmdThang.ExecuteNonQueryAsync();
                                }
                                //  4. UPSERT bảng ThongTin 
                                using (var cmd = new SqliteCommand(@"
                                    INSERT INTO ThongTin
                                    (ID,DiaDiem,Ngay,Thang,Nam,ChiHuyD,LoaiDeNghi,PTLoai1,PTLoai2,PTLoai3)
                                    VALUES
                                    (1,@DiaDiem,@Ngay,@Thang,@Nam,@ChiHuyD,@LoaiDeNghi,@PTLoai1,@PTLoai2,@PTLoai3)
                                    ON CONFLICT(ID)
                                    DO UPDATE SET
                                    DiaDiem=@DiaDiem,
                                    Ngay=@Ngay,
                                    Thang=@Thang,
                                    Nam=@Nam,
                                    ChiHuyD=@ChiHuyD,
                                    LoaiDeNghi=@LoaiDeNghi,
                                    PTLoai1=@PTLoai1,
                                    PTLoai2=@PTLoai2,
                                    PTLoai3=@PTLoai3
                                    ", conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@DiaDiem", diaDiem);
                                    cmd.Parameters.AddWithValue("@Ngay", ngay);
                                    cmd.Parameters.AddWithValue("@Thang", thang);
                                    cmd.Parameters.AddWithValue("@Nam", nam);
                                    cmd.Parameters.AddWithValue("@ChiHuyD", chiHuyD);
                                    cmd.Parameters.AddWithValue("@LoaiDeNghi", deNghi);
                                    cmd.Parameters.AddWithValue("@PTLoai1", ptLoai1);
                                    cmd.Parameters.AddWithValue("@PTLoai2", ptLoai2);
                                    cmd.Parameters.AddWithValue("@PTLoai3", ptLoai3);
                                    // ⭐ NÂNG CẤP BẤT ĐỒNG BỘ: ExecuteNonQueryAsync
                                    await cmd.ExecuteNonQueryAsync();
                                }
                                Progress_Step(20);
                                //  5. Commit transaction 
                                // ⭐ NÂNG CẤP BẤT ĐỒNG BỘ: CommitAsync
                                await tran.CommitAsync();
                                // 🟢 [BỔ SUNG CỐT LÕI]: Cập nhật lại Cache RAM & Bắn sự kiện cho toàn hệ thống
                                Module_HeThong.LayCheDoXetThiDuaNam(lamMoiTuCSDL: true); // Force đọc lại Chế độ
                                Module_HeThong.ThongBaoThayDoiThoiGian(); // Thông báo Tháng/Năm đã thay đổi!
                                Module_HeThong.ThongBaoThayDoiThoiGianFrom4(); // Thông báo Tháng/Năm đã thay đổi!                         
                            }
                            catch
                            {
                                // ⭐ NÂNG CẤP BẤT ĐỒNG BỘ: RollbackAsync
                                await tran.RollbackAsync();
                                throw;
                            }
                        }
                    }
                    //  6. Sau khi lưu 
                    if (KiemTraCoDuLieuDanhSach())
                    {
                        await Bang2_Async();
                    }
                    else
                    {
                        // Nếu không có dữ liệu thì chủ động xóa trắng grid
                        kryptonDataGridView2.DataSource = null;
                    }
                    Progress_Step(15);
                    Module_ThongBao.ThanhCong("Đã lưu thông tin vào CSDL!");
                    Module_NhatKy.GhiNhatKy(taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM, hanhDong: "Lưu thông tin khai báo thành công vào CSDL", ghiChu: $"Thời gian: {SessionInfo.ThoiGianDangNhap:dd-MM-yyyy HH:mm:ss}");
                    Progress_End();
                    // Cập nhật ListBox ngay lập tức
                    Module_ThongBao.CapNhatThongTin();
                }
                catch (Exception ex)
                {
                    Progress_End();
                    Module_ThongBao.Loi("Lỗi khi lưu thông tin vào CSDL:\n" + ex.Message);
                }
            }
            finally
            {
                // LỚP VỎ UX: KHÔI PHỤC LẠI TRẠNG THÁI NÚT DÙ THÀNH CÔNG HAY THẤT BẠI
                kryptonButton_LuuThongTin.Values.Text = textBanDau;
                kryptonButton_LuuThongTin.Values.Image = anhBanDau;
                kryptonButton_LuuThongTin.Enabled = true;
                _isSaving = false; // Mở khóa sau khi hoàn tất hoàn toàn
            }
        }
        private bool KiemTraCoDuLieuDanhSach()
        {
            string dbPath = _csdl2Path;
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return false;
            try
            {
                // Chỉ mở ReadOnly để quét cực nhanh
                using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;Pooling=True;");
                conn.Open();
                // Trả về 1 nếu có ít nhất 1 dòng, trả về 0 nếu bảng trống
                using var cmd = new SqliteCommand("SELECT EXISTS(SELECT 1 FROM DanhSach LIMIT 1);", conn);
                return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi kiểm tra Data: " + ex.Message);
                return false;
            }
        }
        private async void Form4_Shown(object? sender, EventArgs e)
        {
            try
            {
                // 1️⃣ Nhịp nghỉ ngắn (150ms) cho luồng đồ họa Windows dựng xong Form4 lên màn hình
                await Task.Delay(150);
                // Kiểm tra an toàn: Nếu cán bộ tắt form nhanh trước 150ms thì thoát ngay
                if (this.IsDisposed || !this.IsHandleCreated) return;
                // 2️⃣ GỘP CHUNG TOÀN BỘ TÁC VỤ VẼ UI VÀO 1 KHỐI ĐỂ TRÁNH NGHẼN LUỒNG CHÍNH
                this.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (this.IsDisposed) return;
                        // Setup UI cơ bản
                        datChieuCaoNut();
                        AnIDGrid(kryptonDataGridView1);
                        // Vẽ đồ thị và gán thông báo
                        KhoiTaoPieChart();
                        Module_ThongBao.GanListBox(listBox1);
                        Module_ThongBao.Info("Trang chủ đã sẵn sàng");
                        LoadDiaDiemAsync();
                        // Thiết lập AutoComplete cho ComboBox địa điểm
                        comboBox_DiaDiem.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                        comboBox_DiaDiem.AutoCompleteSource = AutoCompleteSource.ListItems;
                        // Xóa tiêu điểm (Focus) mặc định để giao diện sạch sẽ, chuyên nghiệp
                        this.ActiveControl = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Lỗi xử lý UI trong BeginInvoke: " + ex.Message);
                    }
                }));
                // 3️⃣ NHỊP NGHỈ UX 2.5 GIÂY - Cho cán bộ nhìn tổng quan bảng biểu, biểu đồ tròn
                await Task.Delay(2500);
                // Kiểm tra fail-safe một lần nữa trước khi bật Form mới
                if (this.IsDisposed || !this.IsHandleCreated) return;
                // 4️⃣ KÍCH HOẠT DUY NHẤT 1 LẦN CƠ CHẾ CHÀO MỪNG / HƯỚNG DẪN NGƯỜI DÙNG
                ModuleWelcome.ShowWelcomeIfNeeded();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi luồng trong Form4_Shown: " + ex.Message);
            }
        }
        private bool _walDaBat = false;
        private string _quanSoSignature = "";
        private bool _bang1EventsWired = false;
        private async Task Bang1_Async()
        {
            string csdl2Path = _csdl2Path;
            if (string.IsNullOrWhiteSpace(csdl2Path) || !File.Exists(csdl2Path))
            {
                Debug.WriteLine("Không tìm thấy CSDL phụ.");
                return;
            }
            try
            {
                // Gọi trên UI thread như cũ (không rõ Module_DonVi có an toàn đa luồng không)
                string[] donViUuTien = Module_DonVi.LayDanhSachDonViUuTienArray()
                    .Select(d => (d ?? string.Empty).Trim()).ToArray();
                // Toàn bộ phần nặng ở luồng nền
                var kq = await Task.Run(() => QuetVaTinhBang1(csdl2Path, donViUuTien));
                // Về UI thread: chỉ gán kết quả
                _dsDonViBoQuaCanhBao = kq.BoQua;
                pieData = kq.PieData;
                _cachedTongBCH = kq.TongBCH;
                if (piePanel != null && !piePanel.IsDisposed) piePanel.Invalidate();
                HienThiBang1Grid(kq.Bang);
            }
            catch (SqliteException sqlEx)
            {
                MessageBox.Show("Lỗi cơ sở dữ liệu SQLite:\n" + sqlEx.Message, "SQLite", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private (DataTable Bang, Dictionary<string, int> PieData, int TongBCH, HashSet<string> BoQua)
    QuetVaTinhBang1(string csdl2Path, string[] donViUuTien)
        {
            var boQua = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var thuTu = new List<string>();
            var dict = new Dictionary<string, ThongKeDonVi>(StringComparer.OrdinalIgnoreCase);
            foreach (var dv in donViUuTien)
            {
                if (string.IsNullOrWhiteSpace(dv) || dict.ContainsKey(dv)) continue;
                dict[dv] = new ThongKeDonVi();
                thuTu.Add(dv);
            }
            int l1p = 0, l2p = 0, l3p = 0, l4p = 0, kplp = 0, tongBCH = 0;
            using var conn = TaoKetNoiCSDL2(readOnly: false);
            conn.Open();
            if (!_walDaBat)
            {
                using var pragma = conn.CreateCommand();
                pragma.CommandText = "PRAGMA journal_mode = WAL;";
                pragma.ExecuteNonQuery();
                _walDaBat = true;
            }
            // Đơn vị bỏ qua cảnh báo
            using (var cmd = new SqliteCommand("SELECT Ten_DonViBoQuaCanhBaoTyLe FROM ChonDonVi_DeBoQuaCanhBao", conn))
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    string ten = rd.IsDBNull(0) ? "" : (rd.GetString(0) ?? "").Trim();
                    if (ten.Length > 0) boQua.Add(ten);
                }
            }
            // MỘT LẦN QUÉT DanhSach
            using (var cmd = new SqliteCommand("SELECT DonVi, PhanLoai FROM DanhSach", conn))
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    string donvi = SafeDecrypt(rd.GetValue(0)).Trim();
                    if (donvi.Length == 0) continue;
                    string phanloai = SafeDecrypt(rd.GetValue(1)).Trim();
                    if (!dict.TryGetValue(donvi, out var tk))
                    {
                        tk = new ThongKeDonVi();
                        dict[donvi] = tk;
                        thuTu.Add(donvi);
                    }
                    tk.TongQS++;
                    if (donvi.Equals("BCH", StringComparison.OrdinalIgnoreCase)) tongBCH++;
                    switch (phanloai)
                    {
                        case Module_HeThong.Loai_1: tk.Loai1++; l1p++; break;
                        case Module_HeThong.Loai_2: tk.Loai2++; l2p++; break;
                        case Module_HeThong.Loai_3: tk.Loai3++; l3p++; break;
                        case Module_HeThong.Loai_4: tk.Loai4++; l4p++; break;
                        default: tk.KhongPL++; kplp++; break;
                    }
                }
            }
            var pie = new Dictionary<string, int>
    {
        { Module_HeThong.Loai_1, l1p }, { Module_HeThong.Loai_2, l2p },
        { Module_HeThong.Loai_3, l3p }, { Module_HeThong.Loai_4, l4p },
        { Module_HeThong.PL_KHONG_PL, kplp }
    };
            // Dựng DataTable
            static string Pt(double tu, double mau) => mau > 0 ? Math.Round(tu * 100.0 / mau, 2).ToString() : "0";
            var dt = new DataTable();
            dt.Columns.Add("DonVi", typeof(string));
            dt.Columns.Add("TongQS", typeof(int));
            dt.Columns.Add("Loai_1", typeof(int));
            dt.Columns.Add("Loai_2", typeof(int));
            dt.Columns.Add("Loai_3", typeof(int));
            dt.Columns.Add("Loai_4", typeof(int));
            dt.Columns.Add("Khong_PL", typeof(int));
            dt.Columns.Add("PhanTramLoai_1", typeof(string));
            dt.Columns.Add("PhanTramLoai_2", typeof(string));
            dt.Columns.Add("PhanTramLoai_3", typeof(string));
            dt.Columns.Add("PhanTramLoai_4", typeof(string));
            dt.Columns.Add("PhanTramKhong_PL", typeof(string));
            int sQS = 0, s1 = 0, s2 = 0, s3 = 0, s4 = 0, sK = 0;
            dt.BeginLoadData();
            foreach (var ten in thuTu)
            {
                if (!dict.TryGetValue(ten, out var tk) || tk.TongQS <= 0) continue;
                dt.Rows.Add(ten, tk.TongQS, tk.Loai1, tk.Loai2, tk.Loai3, tk.Loai4, tk.KhongPL,
                    Pt(tk.Loai1, tk.Loai1 + tk.Loai2),
                    Pt(tk.Loai1 + tk.Loai2, tk.TongQS),
                    Pt(tk.Loai3, tk.TongQS),
                    Pt(tk.Loai4, tk.TongQS),
                    Pt(tk.KhongPL, tk.TongQS));
                sQS += tk.TongQS; s1 += tk.Loai1; s2 += tk.Loai2; s3 += tk.Loai3; s4 += tk.Loai4; sK += tk.KhongPL;
            }
            if (dt.Rows.Count > 0)
            {
                dt.Rows.Add("Tổng cộng", sQS, s1, s2, s3, s4, sK,
                    Pt(s1, s1 + s2), Pt(s1 + s2, sQS), Pt(s3, sQS), Pt(s4, sQS), Pt(sK, sQS));
            }
            dt.EndLoadData();
            // Ghi QuanSoThiDuaD2 (Bang2_Async đọc bảng này). Bỏ qua nếu dữ liệu không đổi.
            var sb = new StringBuilder(csdl2Path);
            foreach (DataRow r in dt.Rows)
            {
                for (int c = 0; c <= 6; c++) sb.Append('|').Append(r[c]);
                sb.Append('\n');
            }
            string sig = sb.ToString();
            if (sig != _quanSoSignature)
            {
                using var tran = conn.BeginTransaction();
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = tran;
                    cmd.CommandText = "DELETE FROM QuanSoThiDuaD2;";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText =
                        @"INSERT INTO QuanSoThiDuaD2 (DonVi, TongQS, Loai_1, Loai_2, Loai_3, Loai_4, Khong_PL)
                  VALUES (@DonVi, @TongQS, @L1, @L2, @L3, @L4, @KPL);";
                    var pDv = cmd.Parameters.Add("@DonVi", SqliteType.Text);
                    var pQs = cmd.Parameters.Add("@TongQS", SqliteType.Integer);
                    var p1 = cmd.Parameters.Add("@L1", SqliteType.Integer);
                    var p2 = cmd.Parameters.Add("@L2", SqliteType.Integer);
                    var p3 = cmd.Parameters.Add("@L3", SqliteType.Integer);
                    var p4 = cmd.Parameters.Add("@L4", SqliteType.Integer);
                    var pK = cmd.Parameters.Add("@KPL", SqliteType.Integer);
                    foreach (DataRow r in dt.Rows)
                    {
                        pDv.Value = r["DonVi"]; pQs.Value = r["TongQS"];
                        p1.Value = r["Loai_1"]; p2.Value = r["Loai_2"]; p3.Value = r["Loai_3"];
                        p4.Value = r["Loai_4"]; pK.Value = r["Khong_PL"];
                        cmd.ExecuteNonQuery();
                    }
                    tran.Commit();
                    _quanSoSignature = sig;
                }
                catch
                {
                    tran.Rollback();
                    _quanSoSignature = "";
                    throw;
                }
            }
            return (dt, pie, tongBCH, boQua);
        }
        private void HienThiBang1Grid(DataTable dt)
        {
            var dgv = kryptonDataGridView1;
            dgv.SuspendLayout();
            try
            {
                var cu = dgv.DataSource as DataTable;
                dgv.DataSource = null;
                cu?.Dispose();
                dgv.DataSource = dt;
                dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
                // Cột nào có giá trị khác 0: quét 1 lượt cho mọi cột
                var coGiaTri = new bool[dt.Columns.Count];
                foreach (DataRow r in dt.Rows)
                    for (int c = 0; c < coGiaTri.Length; c++)
                    {
                        if (coGiaTri[c]) continue;
                        string s = Convert.ToString(r[c])?.Trim() ?? "";
                        if (s.Length > 0 && s != "0" && s != "0%") coGiaTri[c] = true;
                    }
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    col.SortMode = DataGridViewColumnSortMode.NotSortable;
                    if (col.Name == "DonVi") col.HeaderText = "Đơn vị";
                    else if (col.Name == "TongQS") col.HeaderText = "Quân số";
                    int idx = dt.Columns.IndexOf(col.Name);
                    col.Visible = idx >= 0 && coGiaTri[idx];
                }
                AnhXaGiaoDienThiDua();
                if (!_bang1EventsWired)
                {
                    dgv.RowPostPaint -= KryptonDataGridView1_RowPostPaint;
                    dgv.RowPostPaint += KryptonDataGridView1_RowPostPaint;
                    dgv.CellFormatting -= KhaoSatTyLePhanTramCacDonVi;
                    dgv.CellFormatting += KhaoSatTyLePhanTramCacDonVi;
                    dgv.CellClick -= KryptonDataGridView1_CellClick_HienThiThongBao;
                    dgv.CellClick += KryptonDataGridView1_CellClick_HienThiThongBao;
                    _bang1EventsWired = true;
                }
                AutoFitFont_DataGridView(dgv);
                if (_cachedGridFontBold != null)
                    dgv.ColumnHeadersDefaultCellStyle.Font = _cachedGridFontBold;
                dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
                dgv.ColumnHeadersHeight = ChieuCao_TieuDe_BangDataGird;
                UocLuongDoRongCacCotNhanh(dgv, dt);
            }
            finally
            {
                dgv.ResumeLayout();
            }
        }
        private void UocLuongDoRongCacCotNhanh(DataGridView dgv, DataTable dt)
        {
            var fontHeader = dgv.ColumnHeadersDefaultCellStyle.Font ?? dgv.Font;
            var fontCell = dgv.DefaultCellStyle.Font ?? dgv.Font;
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (!col.Visible) continue;
                int maxWidth = TextRenderer.MeasureText(col.HeaderText, fontHeader).Width;
                int idx = dt.Columns.IndexOf(col.Name);
                if (idx >= 0)
                {
                    string dai = "";
                    foreach (DataRow r in dt.Rows)
                    {
                        string s = Convert.ToString(r[idx]) ?? "";
                        if (s.Length > dai.Length) dai = s;
                    }
                    if (dai.Length > 0)
                        maxWidth = Math.Max(maxWidth, TextRenderer.MeasureText(dai, fontCell).Width);
                }
                int width = Math.Min(Math.Max(maxWidth + 20, 30), 200);
                float heSo = col.Name switch
                {
                    "TongQS" or "Loai_1" or "Loai_2" or "Loai_3" or "Loai_4" or "Khong_PL" => 2f,
                    _ => 1.5f
                };
                col.FillWeight = width * heSo;
            }
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }
        private async Task Bang2_Async()
        {
            if (!_allowLoadBang2)
            {
                _allowLoadBang2 = true;
                return;
            }
            if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path))
                return;
            string csdl2Path = _csdl2Path;
            try
            {
                using var connection = new SqliteConnection($"Data Source={csdl2Path}");
                await connection.OpenAsync();
                string kyHieuTrungDoan = "E09";
                // ⭐ NHẬN DIỆN PHIÊN BẢN ĐỂ CHỌN ĐÚNG BẢNG TỶ LỆ KHI ĐỌC/GHI
                bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                string tableQuyDinhTyLe = laTanBinh ? "QuyDinhTyLe_TanBinh" : "QuyDinhTyLe"; // Bảng cấu hình % tương ứng
                try
                {
                    using var cmdKyHieu = new SqliteCommand("SELECT KyHieu_TrungDoan FROM KyHieu_DonVi WHERE ID = 1 LIMIT 1", connection);
                    var result = await cmdKyHieu.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        string giaiMa = GiaiMaAnToan(result.ToString());
                        if (!string.IsNullOrWhiteSpace(giaiMa))
                            kyHieuTrungDoan = giaiMa.Trim();
                    }
                }
                catch (Exception ex) { Debug.WriteLine("Lỗi đọc KyHieu: " + ex.Message); }
                DataTable dtQS = new DataTable();
                using (var cmd = new SqliteCommand("SELECT * FROM QuanSoThiDuaD2 ORDER BY ID", connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    dtQS.Load(reader);
                }
                if (dtQS.Rows.Count == 0)
                {
                    kryptonDataGridView2.DataSource = null;
                    _allowLoadBang2 = false;
                    return;
                }
                _allowLoadBang2 = true;
                DataRow totalRow = dtQS.AsEnumerable().FirstOrDefault(r => r["DonVi"]?.ToString() == "Tổng cộng") ?? dtQS.Rows[dtQS.Rows.Count - 1];
                double loai1Rate = phanTramLoai1 <= 0 ? 0 : phanTramLoai1;
                double loai2Rate = phanTramLoai2 <= 0 ? 0 : phanTramLoai2;
                int tongQuanSo = Convert.ToInt32(totalRow["TongQS"]);
                int loai1 = Convert.ToInt32(totalRow["Loai_1"]);
                int loai2 = Convert.ToInt32(totalRow["Loai_2"]);
                int loai3 = Convert.ToInt32(totalRow["Loai_3"]);
                int loai4 = Convert.ToInt32(totalRow["Loai_4"]);
                int khongPL = Convert.ToInt32(totalRow["Khong_PL"]);
                int duDieuKien = tongQuanSo - loai4 - khongPL;
                if (duDieuKien < 0) duDieuKien = 0;
                int kqCanDat_L2_Tong = (int)Math.Floor(duDieuKien * loai2Rate / 100.0);
                int kqCanDat_L3 = duDieuKien - kqCanDat_L2_Tong;
                int kqCanDat_L1 = (int)Math.Floor(kqCanDat_L2_Tong * loai1Rate / 100.0);
                using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync();
                // ⭐ BẢO VỆ CSDL: Tự động khởi tạo bảng TyLe nếu chưa có cấu trúc
                using (var cmdInit = connection.CreateCommand())
                {
                    cmdInit.Transaction = transaction;
                    cmdInit.CommandText = @"
        CREATE TABLE IF NOT EXISTS [TyLe] (
            ID INTEGER NOT NULL PRIMARY KEY,
            [Thong tin] TEXT,
            [KQ Hien tai] INTEGER,
            [KQ Can dat] INTEGER,
            [KQ Gui E29] TEXT,
            [Ket luan] TEXT
        );";
                    await cmdInit.ExecuteNonQueryAsync();
                }
                using var cmdUpdate = connection.CreateCommand();
                cmdUpdate.Transaction = transaction;
                async Task UpdateTyLeRowAsync(int id, int kqHienTai, int kqCanDat, string kqGuiE29, string ketLuan)
                {
                    cmdUpdate.Parameters.Clear();
                    cmdUpdate.CommandText = @"UPDATE TyLe SET ""KQ Hien tai""=@kqHienTai, ""KQ Can dat""=@kqCanDat, ""KQ Gui E29""=@kqGuiE29, ""Ket luan""=@ketLuan WHERE ID=@id;";
                    cmdUpdate.Parameters.AddWithValue("@kqHienTai", kqHienTai);
                    cmdUpdate.Parameters.AddWithValue("@kqCanDat", kqCanDat);
                    cmdUpdate.Parameters.AddWithValue("@kqGuiE29", kqGuiE29);
                    cmdUpdate.Parameters.AddWithValue("@ketLuan", ketLuan);
                    cmdUpdate.Parameters.AddWithValue("@id", id);
                    await cmdUpdate.ExecuteNonQueryAsync();
                }
                string fmt(double v) => v.ToString("0.00");
                string guiE29_Tong = $"{tongQuanSo}";
                string guiE29_Loai1 = kqCanDat_L2_Tong > 0 ? $"{kqCanDat_L1}/{kqCanDat_L2_Tong} = {fmt(kqCanDat_L1 * 100.0 / kqCanDat_L2_Tong)}%" : "";
                string guiE29_Loai2 = duDieuKien > 0 ? $"{kqCanDat_L2_Tong}/{duDieuKien} = {fmt(kqCanDat_L2_Tong * 100.0 / duDieuKien)}%" : "";
                string guiE29_Loai3 = duDieuKien > 0 ? $"{kqCanDat_L3}/{duDieuKien} = {fmt(kqCanDat_L3 * 100.0 / duDieuKien)}%" : "";
                int[] kqHienTaiArr = { tongQuanSo, loai1, loai2, loai3, loai4, khongPL };
                int[] kqGuiE29Arr = { tongQuanSo, kqCanDat_L1, kqCanDat_L2_Tong, kqCanDat_L3, 0, 0 };
                string[] guiE29Arr = { guiE29_Tong, guiE29_Loai1, guiE29_Loai2, guiE29_Loai3, "", "" };
                string KetLuanRow(int id, int kqHienTai, int kqCanDat, bool tinh)
                {
                    if (!tinh && (id < 5 || id > 6)) return "";
                    switch (id)
                    {
                        case 1:
                            bool loai1Ok = loai1 >= kqCanDat_L1;
                            bool loai2Ok = loai2 >= (kqCanDat_L2_Tong - kqCanDat_L1);
                            bool loai3Ok = loai3 >= kqCanDat_L3;
                            return (loai1Ok && loai2Ok && loai3Ok) ? "Đạt tỷ lệ quy định" : "Chưa đạt tỷ lệ quy định";
                        case 2:
                            if (kqHienTai == kqCanDat_L1) return "";
                            return kqHienTai > kqCanDat_L1 ? $"Đang thừa {kqHienTai - kqCanDat_L1} {Module_HeThong.Tu_dong_chi}" : $"Đang thiếu {kqCanDat_L1 - kqHienTai} đồng chí";
                        case 3:
                            int kqCanDatLoai2Thuan = kqCanDat_L2_Tong - kqCanDat_L1;
                            if (kqHienTai == kqCanDatLoai2Thuan) return "";
                            return kqHienTai > kqCanDatLoai2Thuan ? $"Đang thừa {kqHienTai - kqCanDatLoai2Thuan} {Module_HeThong.Tu_dong_chi}" : $"Đang thiếu {kqCanDatLoai2Thuan - kqHienTai} đồng chí";
                        case 4:
                            if (kqHienTai == kqCanDat_L3) return "";
                            return kqHienTai > kqCanDat_L3 ? $"Đang thừa {kqHienTai - kqCanDat_L3} {Module_HeThong.Tu_dong_chi}" : $"Đang thiếu {kqCanDat_L3 - kqHienTai} đồng chí";
                        case 5:
                        case 6:
                            if (kqHienTai == 0) return "";
                            return $"Phát sinh {kqHienTai} {Module_HeThong.Tu_dong_chi}";
                        default:
                            return "";
                    }
                }
                for (int i = 0; i < 6; i++)
                {
                    bool tinh = i < 4;
                    int kqHienTaiSoSanh = (i == 2) ? loai2 : kqHienTaiArr[i];
                    string kqGuiE29 = guiE29Arr[i];
                    if (i == 4 || i == 5)
                    {
                        if (kqHienTaiArr[i] > 0 && tongQuanSo > 0)
                        {
                            kqGuiE29 = $"{kqHienTaiArr[i]:D2}/{duDieuKien} = {fmt((double)kqHienTaiArr[i] * 100 / tongQuanSo)}%";
                        }
                        else kqGuiE29 = "";
                    }
                    await UpdateTyLeRowAsync(i + 1, kqHienTaiArr[i], kqGuiE29Arr[i], kqGuiE29, KetLuanRow(i + 1, kqHienTaiSoSanh, kqGuiE29Arr[i], tinh));
                }
                await transaction.CommitAsync();
                DataTable dt = new DataTable();
                using (var cmdLoadTyLe = new SqliteCommand("SELECT * FROM TyLe", connection))
                using (var reader = await cmdLoadTyLe.ExecuteReaderAsync())
                {
                    dt.Load(reader);
                }
                kryptonDataGridView2.SuspendLayout();
                kryptonDataGridView2.DataSource = dt;
                AnhXaGiaoDienThiDua(); //Ánh xạ giao diện thi đua
                AutoFitFont_DataGridView(kryptonDataGridView2);
                if (kryptonDataGridView2.Columns.Contains("ID")) kryptonDataGridView2.Columns["ID"].Visible = false;
                kryptonDataGridView2.RowHeadersVisible = true;
                kryptonDataGridView2.RowHeadersWidth = 60;
                kryptonDataGridView2.AllowUserToAddRows = false;
                kryptonDataGridView2.AllowUserToDeleteRows = false;
                kryptonDataGridView2.ReadOnly = true;
                kryptonDataGridView2.SelectionMode = DataGridViewSelectionMode.CellSelect;
                kryptonDataGridView2.MultiSelect = false;
                kryptonDataGridView2.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                kryptonDataGridView2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                void SetColumn(string colName, string headerText, float fillWeight)
                {
                    if (kryptonDataGridView2.Columns.Contains(colName))
                    {
                        var col = kryptonDataGridView2.Columns[colName];
                        col.HeaderText = headerText;
                        col.FillWeight = fillWeight;
                        col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }
                }
                SetColumn("Thong tin", "Thông tin", 15);
                SetColumn("KQ Hien tai", "KQ Hiện tại", 15);
                SetColumn("KQ Can dat", "KQ Cần đạt", 17);
                SetColumn("KQ Gui E29", $"KQ Gửi {kyHieuTrungDoan}", 17);
                SetColumn("Ket luan", "Kết luận", 31);
                float baseSize = kryptonDataGridView2.Font.Size;
                if (_cachedGridFont != null) baseSize = _cachedGridFont.Size;
                kryptonDataGridView2.EnableHeadersVisualStyles = false;
                if (_cachedGrid2HeaderFont == null || Math.Abs(_cachedGrid2HeaderFont.Size - baseSize) > 0.1f)
                {
                    _cachedGrid2HeaderFont?.Dispose();
                    _cachedGrid2HeaderFont = new Font(Module_HeThong.TenFontHeThong, baseSize, FontStyle.Bold);
                }
                kryptonDataGridView2.ColumnHeadersDefaultCellStyle.Font = _cachedGrid2HeaderFont;
                //Mèo Cam
                kryptonDataGridView2.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
                kryptonDataGridView2.ColumnHeadersHeight = ChieuCao_TieuDe_BangDataGird;
                foreach (DataGridViewColumn col in kryptonDataGridView2.Columns) col.SortMode = DataGridViewColumnSortMode.NotSortable;
                kryptonDataGridView2.ResumeLayout();
                kryptonDataGridView2.RowPostPaint -= KryptonDataGridView2_RowPostPaint;
                kryptonDataGridView2.RowPostPaint += KryptonDataGridView2_RowPostPaint;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Bang2 ERROR: " + ex.Message);
            }
        }
        private void AnhXaGiaoDienThiDua()
        {
            // 🌟 SỬA TẠI ĐÂY: Gọi hàm LayCheDoXetThiDuaNam() hoặc IsCheDoXetThiDuaNam()
            bool laXetNam = Module_HeThong.IsCheDoXetThiDuaNam();
            string tenL1 = laXetNam ? Module_HeThong.PL_CSTD : Module_HeThong.Loai_1;
            string tenL2 = laXetNam ? Module_HeThong.PL_CSTT : Module_HeThong.Loai_2;
            string tenL3 = laXetNam ? Module_HeThong.PL_HTNV : Module_HeThong.Loai_3;
            string tenL4 = laXetNam ? Module_HeThong.PL_KHTNV : Module_HeThong.Loai_4;
            string tenKPL = laXetNam ? Module_HeThong.PL_KHONG_PL : Module_HeThong.PL_KHONG_PL;
            // 1. Ánh xạ Header cho Bảng 1
            if (kryptonDataGridView1?.DataSource != null)
            {
                var colMap1 = new Dictionary<string, string>
        {
            { "Loai_1", tenL1 },
            { "Loai_2", tenL2 },
            { "Loai_3", tenL3 },
            { "Loai_4", tenL4 },
            { "Khong_PL", tenKPL },
            { "PhanTramLoai_1", $"% {tenL1}" },
            { "PhanTramLoai_2", $"% {tenL2}" },
            { "PhanTramLoai_3", $"% {tenL3}" },
            { "PhanTramLoai_4", $"% {tenL4}" },
            { "PhanTramKhong_PL", $"% {tenKPL}" }
        };
                foreach (DataGridViewColumn col in kryptonDataGridView1.Columns)
                {
                    if (colMap1.TryGetValue(col.Name, out string headerText))
                    {
                        col.HeaderText = headerText;
                    }
                }
            }
            // 2. Ánh xạ dữ liệu hiển thị cột "Thông tin" cho Bảng 2
            if (kryptonDataGridView2?.DataSource != null && kryptonDataGridView2.Columns.Contains("Thong tin"))
            {
                var rowTextMap = new Dictionary<int, string>
        {
            { 2, tenL1 },
            { 3, tenL2 },
            { 4, tenL3 },
            { 5, tenL4 },
            { 6, tenKPL }
        };
                foreach (DataGridViewRow row in kryptonDataGridView2.Rows)
                {
                    if (row.IsNewRow) continue;
                    if (row.Cells["ID"].Value != null && int.TryParse(row.Cells["ID"].Value.ToString(), out int id))
                    {
                        if (rowTextMap.TryGetValue(id, out string textHienThi))
                        {
                            row.Cells["Thong tin"].Value = textHienThi;
                        }
                    }
                }
            }
        }
        private void KhaoSatTyLePhanTramCacDonVi(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (sender is not DataGridView dgv || e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (!dgv.Columns.Contains("DonVi") || !dgv.Columns.Contains("TongQS") ||
                !dgv.Columns.Contains("Loai_1") || !dgv.Columns.Contains("Loai_2") ||
                !dgv.Columns.Contains("Loai_3") || !dgv.Columns.Contains("Loai_4") ||
                !dgv.Columns.Contains("Khong_PL"))
                return;
            var row = dgv.Rows[e.RowIndex];
            string donViName = row.Cells["DonVi"].Value?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(donViName)) return;
            bool dangChon = row.Selected;
            // Tổng cộng / đơn vị được bỏ qua cảnh báo: luôn xanh, chữ thường
            if (donViName.Equals("Tổng cộng", StringComparison.OrdinalIgnoreCase) ||
                _dsDonViBoQuaCanhBao.Contains(donViName.Trim()))
            {
                e.CellStyle.ForeColor = dangChon ? e.CellStyle.SelectionForeColor : Color.FromArgb(0, 128, 0);
                e.CellStyle.Font = dgv.Font;
                return;
            }
            try
            {
                int tongQS = Convert.ToInt32(row.Cells["TongQS"].Value ?? 0);
                int l1 = Convert.ToInt32(row.Cells["Loai_1"].Value ?? 0);
                int l2 = Convert.ToInt32(row.Cells["Loai_2"].Value ?? 0);
                int l3 = Convert.ToInt32(row.Cells["Loai_3"].Value ?? 0);
                int l4 = Convert.ToInt32(row.Cells["Loai_4"].Value ?? 0);
                int kpl = Convert.ToInt32(row.Cells["Khong_PL"].Value ?? 0);
                if (tongQS <= 0) return;
                int kqCanDat_L1 = 0, kqCanDat_L2_Thuan = 0, kqCanDat_L3 = 0;
                if (donViName.Equals("BCH", StringComparison.OrdinalIgnoreCase))
                {
                    string deNghiTTe = com_DeNghi.Text.Trim().ToUpperInvariant();
                    if (string.IsNullOrWhiteSpace(deNghiTTe) || deNghiTTe.Contains("KHÔNG PL")) return;
                    int bch = tongQS;   // BỎ quét DB: quân số dòng BCH chính là tổng BCH
                    if (deNghiTTe == "LOẠI 1")
                    {
                        kqCanDat_L1 = (int)Math.Floor(bch * 0.75);
                        kqCanDat_L2_Thuan = bch - kqCanDat_L1;
                    }
                    else if (deNghiTTe == "LOẠI 2")
                    {
                        kqCanDat_L1 = (int)Math.Floor(bch * 0.50);
                        kqCanDat_L2_Thuan = bch - kqCanDat_L1;
                    }
                    else if (deNghiTTe == "LOẠI 3" || deNghiTTe == "LOẠI 4")
                    {
                        kqCanDat_L2_Thuan = (int)Math.Floor(bch * 0.50);
                        kqCanDat_L3 = bch - kqCanDat_L2_Thuan;
                    }
                }
                else
                {
                    int duDieuKien = Math.Max(0, tongQS - l4 - kpl);
                    int kqCanDat_L2_Tong = (int)Math.Floor(duDieuKien * (phanTramLoai2 / 100.0));
                    kqCanDat_L3 = duDieuKien - kqCanDat_L2_Tong;
                    kqCanDat_L1 = Math.Min((int)Math.Floor(kqCanDat_L2_Tong * (phanTramLoai1 / 100.0)), kqCanDat_L2_Tong);
                    kqCanDat_L2_Thuan = kqCanDat_L2_Tong - kqCanDat_L1;
                }
                bool datChuan = l1 >= kqCanDat_L1 && l2 >= kqCanDat_L2_Thuan && l3 >= kqCanDat_L3;
                e.CellStyle.ForeColor = dangChon
                    ? e.CellStyle.SelectionForeColor
                    : (datChuan ? Color.FromArgb(0, 128, 0) : Color.FromArgb(255, 0, 0));
                // Chỉ dùng font đã cache, không new Font trong CellFormatting
                e.CellStyle.Font = (!datChuan && _cachedGridFontBold != null) ? _cachedGridFontBold : dgv.Font;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Lỗi khảo sát tỷ lệ Grid] " + ex.Message);
            }
        }
        private void KryptonDataGridView1_CellClick_HienThiThongBao(
        object? sender,
        DataGridViewCellEventArgs e)
        {
            if (sender is not DataGridView dgv ||
                e.RowIndex < 0 ||
                e.ColumnIndex < 0)
            {
                return;
            }
            // Kiểm tra cột bắt buộc
            if (!dgv.Columns.Contains("DonVi"))
                return;
            try
            {
                // Lấy dòng hiện tại một lần -> tránh truy cập dgv.Rows[e.RowIndex] lặp lại
                DataGridViewRow row = dgv.Rows[e.RowIndex];
                string donViName =
                    row.Cells["DonVi"].Value?.ToString()?.Trim()
                    ?? string.Empty;
                if (string.IsNullOrWhiteSpace(donViName) ||
                    donViName.Equals("Tổng cộng", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                // 1. LẤY DỮ LIỆU
                int tongQS = Convert.ToInt32(row.Cells["TongQS"].Value ?? 0);
                int l1 = Convert.ToInt32(row.Cells[Module_HeThong.COL_LOAI_1].Value ?? 0);
                int l2 = Convert.ToInt32(row.Cells[Module_HeThong.COL_LOAI_2].Value ?? 0);
                int l3 = Convert.ToInt32(row.Cells[Module_HeThong.COL_LOAI_3].Value ?? 0);
                int l4 = Convert.ToInt32(row.Cells[Module_HeThong.COL_LOAI_4].Value ?? 0);
                int kpl = Convert.ToInt32(row.Cells["Khong_PL"].Value ?? 0);
                if (tongQS <= 0) return;
                // 2. KHỞI TẠO CHỈ TIÊU CẦN ĐẠT
                int kqCanDat_L1 = 0;
                int kqCanDat_L2_Thuan = 0;
                int kqCanDat_L3 = 0;
                int duDieuKien = Math.Max(0, tongQS - l4 - kpl);
                // 3. XÁC ĐỊNH CHỈ TIÊU
                if (donViName.Equals("BCH", StringComparison.OrdinalIgnoreCase))
                {
                    string deNghiTTe =
                        com_DeNghi.Text.Trim().ToUpperInvariant();
                    // BCH chưa có đề nghị hoặc đề nghị Không PL
                    if (string.IsNullOrWhiteSpace(deNghiTTe) ||
                        deNghiTTe.Contains(Module_HeThong.PL_KHONG_PL.ToUpperInvariant()))
                    {
                        MessageBox.Show(
                            $"Đơn vị: {donViName}\n" +
                            $"Tổng quân số: {tongQS}\n\n" +
                            "Chưa có đề nghị phân loại tập thể nên không xét tỷ lệ.",
                            "Thông tin",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        return;
                    }
                    int bchQS =
                        _cachedTongBCH > 0
                            ? _cachedTongBCH
                            : tongQS;
                    if (deNghiTTe == Module_HeThong.Loai_1.ToUpperInvariant())
                    {
                        kqCanDat_L1 = (int)Math.Floor(bchQS * 0.75);
                        kqCanDat_L2_Thuan = bchQS - kqCanDat_L1;
                    }
                    else if (deNghiTTe == Module_HeThong.Loai_2.ToUpperInvariant())
                    {
                        kqCanDat_L1 = (int)Math.Floor(bchQS * 0.50);
                        kqCanDat_L2_Thuan = bchQS - kqCanDat_L1;
                    }
                    else if (deNghiTTe == Module_HeThong.Loai_3.ToUpperInvariant() ||
                             deNghiTTe == Module_HeThong.Loai_4.ToUpperInvariant())
                    {
                        kqCanDat_L2_Thuan = (int)Math.Floor(bchQS * 0.50);
                        kqCanDat_L3 = bchQS - kqCanDat_L2_Thuan;
                    }
                }
                else
                {
                    // ĐƠN VỊ THÔNG THƯỜNG
                    double rateL1 = phanTramLoai1 / 100.0;
                    double rateL2 = phanTramLoai2 / 100.0;
                    int kqCanDat_L2_Tong =
                        (int)Math.Floor(duDieuKien * rateL2);
                    kqCanDat_L3 =
                        duDieuKien - kqCanDat_L2_Tong;
                    kqCanDat_L1 =
                        (int)Math.Floor(kqCanDat_L2_Tong * rateL1);
                    // Rào chắn an toàn
                    if (kqCanDat_L1 > kqCanDat_L2_Tong)
                        kqCanDat_L1 = kqCanDat_L2_Tong;
                    kqCanDat_L2_Thuan =
                        kqCanDat_L2_Tong - kqCanDat_L1;
                }
                // 4. ĐỐI CHIẾU KẾT QUẢ
                bool isLoai1HopLe = l1 >= kqCanDat_L1;
                bool isLoai2HopLe = l2 >= kqCanDat_L2_Thuan;
                bool isLoai3HopLe = l3 >= kqCanDat_L3;
                bool isDonViDatChuan =
                    isLoai1HopLe &&
                    isLoai2HopLe &&
                    isLoai3HopLe;
                // 5. TẠO NỘI DUNG THÔNG BÁO
                var sb = new StringBuilder();
                sb.AppendLine($"Đơn vị: {donViName.ToUpperInvariant()}");
                sb.AppendLine(new string('-', 40));
                sb.AppendLine(
                    $"  Tổng quân số : {tongQS} {Module_HeThong.Tu_dong_chi}");
                sb.AppendLine(
                    $"  {Module_HeThong.Loai_1,-10}: {l1} {Module_HeThong.Tu_dong_chi}");
                sb.AppendLine(
                    $"  {Module_HeThong.Loai_2,-10}: {l2} {Module_HeThong.Tu_dong_chi}");
                sb.AppendLine(
                    $"  {Module_HeThong.Loai_3,-10}: {l3} {Module_HeThong.Tu_dong_chi}");
                sb.AppendLine(
                    $"  {Module_HeThong.Loai_4,-10}: {l4} {Module_HeThong.Tu_dong_chi}");
                sb.AppendLine(
                    $"  {Module_HeThong.PL_KHONG_PL,-10}: {kpl} {Module_HeThong.Tu_dong_chi}");
                sb.AppendLine(new string('-', 40));
                // 6. QUÂN SỐ ĐỦ ĐIỀU KIỆN
                if (!donViName.Equals("BCH", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine(
                        $"  Quân số đủ điều kiện xét: " +
                        $"{duDieuKien} {Module_HeThong.Tu_dong_chi}");
                    sb.AppendLine();
                }
                // 7. ĐỐI CHIẾU CHỈ TIÊU
                sb.AppendLine(" Đối chiếu chỉ tiêu quy định:");
                sb.AppendLine(
                    $"  {Module_HeThong.Loai_1} (Cần {kqCanDat_L1}):\t" +
                    $"{(isLoai1HopLe ? "Đạt" : "Chưa đạt")}");
                sb.AppendLine(
                    $"  {Module_HeThong.Loai_2} (Cần {kqCanDat_L2_Thuan}):\t" +
                    $"{(isLoai2HopLe ? "Đạt" : "Chưa đạt")}");
                sb.AppendLine(
                    $"  {Module_HeThong.Loai_3} (Cần {kqCanDat_L3}):\t" +
                    $"{(isLoai3HopLe ? "Đạt" : "Chưa đạt")}");
                sb.AppendLine(new string('-', 40));
                // 8. KẾT LUẬN
                sb.AppendLine(
                    $" Kết luận: " +
                    $"{(isDonViDatChuan
                        ? "Đạt tỷ lệ quy định"
                        : "Chưa đạt tỷ lệ quy định")}");
                // 9. HƯỚNG DẪN ĐIỀU CHỈNH
                if (!isDonViDatChuan ||
                    l1 != kqCanDat_L1 ||
                    l2 != kqCanDat_L2_Thuan ||
                    l3 != kqCanDat_L3 ||
                    l4 > 0 ||
                    kpl > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Hướng dẫn điều chỉnh để đạt chuẩn:");
                    if (l1 != kqCanDat_L1)
                    {
                        sb.AppendLine(
                            $"  - {Module_HeThong.Loai_1} đang " +
                            $"{(l1 > kqCanDat_L1 ? "thừa" : "thiếu")} " +
                            $"{Math.Abs(l1 - kqCanDat_L1)} " +
                            $"{Module_HeThong.Tu_dong_chi}.");
                    }
                    if (l2 != kqCanDat_L2_Thuan)
                    {
                        sb.AppendLine(
                            $"  - {Module_HeThong.Loai_2} đang " +
                            $"{(l2 > kqCanDat_L2_Thuan ? "thừa" : "thiếu")} " +
                            $"{Math.Abs(l2 - kqCanDat_L2_Thuan)} " +
                            $"{Module_HeThong.Tu_dong_chi}.");
                    }
                    if (l3 != kqCanDat_L3)
                    {
                        sb.AppendLine(
                            $"  - {Module_HeThong.Loai_3} đang " +
                            $"{(l3 > kqCanDat_L3 ? "thừa" : "thiếu")} " +
                            $"{Math.Abs(l3 - kqCanDat_L3)} " +
                            $"{Module_HeThong.Tu_dong_chi}.");
                    }
                    if (l4 > 0)
                    {
                        sb.AppendLine(
                            $"  - Chú ý: Đang có {l4} " +
                            $"{Module_HeThong.Tu_dong_chi} " +
                            $"xếp {Module_HeThong.Loai_4}.");
                    }
                    if (kpl > 0)
                    {
                        sb.AppendLine(
                            $"  - Chú ý: Đang có {kpl} " +
                            $"{Module_HeThong.Tu_dong_chi} " +
                            $"{Module_HeThong.PL_KHONG_PL}.");
                    }
                }
                // 10. HIỂN THỊ
                MessageBox.Show(
                    sb.ToString(),
                    "Phân tích chỉ tiêu thi đua",
                    MessageBoxButtons.OK,
                    isDonViDatChuan
                        ? MessageBoxIcon.Information
                        : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Lỗi hiển thị thông báo Click Bảng 1: {ex}");
            }
        }
        private void KryptonDataGridView2_RowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
        {
            try
            {
                if (sender is not DataGridView dgv) return;
                string rowNumber = (e.RowIndex + 1).ToString();
                Rectangle rect = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, dgv.RowHeadersWidth, e.RowBounds.Height);
                bool isSelected = dgv.Rows[e.RowIndex].Selected;
                Color textColor = isSelected ? dgv.RowHeadersDefaultCellStyle.SelectionForeColor : dgv.RowHeadersDefaultCellStyle.ForeColor;
                TextRenderer.DrawText(e.Graphics, rowNumber, dgv.Font, rect, textColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RowPostPaint Error: {ex.Message}");
            }
        }
        //private string SafeDecrypt(object? val)
        //{
        //    if (val == null || val == DBNull.Value) return "";
        //    string s = val.ToString() ?? "";
        //    if (string.IsNullOrWhiteSpace(s)) return "";
        //    if (_uiDecryptCache.TryGetValue(s, out string cachedResult))
        //        return cachedResult;
        //    try
        //    {
        //        string dec = Module_BaoMatAES.GiaiMa(s);
        //        string finalVal = string.IsNullOrEmpty(dec) ? s.Trim() : dec;
        //        // BẢO VỆ CPU: Dùng biến đếm độc lập thay vì _uiDecryptCache.Count
        //        if (_uiDecryptCache.Count > 3000)
        //        {
        //            var firstKey = _uiDecryptCache.Keys.FirstOrDefault();
        //            if (firstKey != null)
        //            {
        //                _uiDecryptCache.TryRemove(firstKey, out _);
        //            }
        //        }
        //        if (_uiDecryptCache.TryAdd(s, finalVal))
        //            Interlocked.Increment(ref _cacheCounter);
        //        return finalVal;
        //    }
        //    catch
        //    {
        //        _uiDecryptCache.TryAdd(s, s.Trim());
        //        return s.Trim();
        //    }
        //}
        private string SafeDecrypt(object? val)
        {
            if (val == null || val == DBNull.Value) return "";
            string s = val.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(s)) return "";
            if (_uiDecryptCache.TryGetValue(s, out string? cached))
                return cached;
            string finalVal;
            try
            {
                string dec = Module_BaoMatAES.GiaiMa(s);
                finalVal = string.IsNullOrEmpty(dec) ? s.Trim() : dec;
            }
            catch
            {
                finalVal = s.Trim();
            }
            // Đầy thì xóa sạch (O(1) công sức, không cần tìm "mục cũ nhất")
            if (Volatile.Read(ref _cacheCounter) >= 3000)
            {
                _uiDecryptCache.Clear();
                Interlocked.Exchange(ref _cacheCounter, 0);
            }
            if (_uiDecryptCache.TryAdd(s, finalVal))
                Interlocked.Increment(ref _cacheCounter);
            return finalVal;
        }
        private void AutoFitFont_DataGridView(DataGridView dgv)
        {
            if (dgv == null || dgv.Rows.Count == 0) return;
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            int visibleRows = Math.Max(1, dgv.DisplayedRowCount(false));
            int gridHeight = dgv.ClientSize.Height;
            float estimatedRowHeight = (float)gridHeight / visibleRows;
            float fontSize = Math.Clamp(estimatedRowHeight * 0.40f, 8f, 10f);
            if (_cachedGridFont == null || Math.Abs(_cachedGridFont.Size - fontSize) > 0.1f)
            {
                _cachedGridFont?.Dispose();
                _cachedGridFontBold?.Dispose(); // 👈 Dọn dẹp font in đậm cũ
                // 🟢 Cố định ép font chữ là FontStyle.Regular (chữ thường)
                _cachedGridFont = new Font(Module_HeThong.TenFontHeThong, fontSize, FontStyle.Regular);
                // 🟢 TẠO SẴN 1 FONT IN ĐẬM BỎ VÀO CACHE ĐỂ DÙNG CHUNG (Cực kỳ tối ưu RAM)
                _cachedGridFontBold = new Font(Module_HeThong.TenFontHeThong, fontSize, FontStyle.Bold);
            }
            // 🔥 SỬA CHỮA QUAN TRỌNG TẠI ĐÂY:
            // Gán tường minh Font chữ thường cho Toàn bộ Bảng, và cho Cột (Cells)
            dgv.Font = _cachedGridFont;
            dgv.DefaultCellStyle.Font = _cachedGridFont;
            dgv.RowsDefaultCellStyle.Font = _cachedGridFont;
            dgv.RowTemplate.Height = TextRenderer.MeasureText("A", _cachedGridFont).Height + 6;
        }
        private async Task LoadChiHuyDDictionaryAsync()
        {
            _dictChiHuyD.Clear();
            try
            {
                bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                string tableChiHuy = laTanBinh ? "ChiHuyD_TanBinh" : "ChiHuyD";
                using var cn = TaoKetNoiCSDL2(true);
                await cn.OpenAsync();
                using var cmd = cn.CreateCommand();
                // Sửa query chỉ vào bảng động
                cmd.CommandText = $"SELECT HoVaTen, ChucVu FROM [{tableChiHuy}]";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string hoTen = SafeDecrypt(reader["HoVaTen"]);
                    string chucVu = SafeDecrypt(reader["ChucVu"]);
                    if (string.IsNullOrWhiteSpace(hoTen)) continue;
                    string key = hoTen.ToLowerInvariant();
                    if (!_dictChiHuyD.ContainsKey(key)) _dictChiHuyD.Add(key, chucVu);
                }
            }
            catch (Exception ex) { Debug.WriteLine("Lỗi LoadChiHuyDDictionary: " + ex.Message); }
        }
        // SỬA HÀM THỨ 2 TRONG FORM 4
        private async Task LoadComboBox_ChiHuyDAsync()
        {
            try
            {
                bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                string tableChiHuy = laTanBinh ? "ChiHuyD_TanBinh" : "ChiHuyD";
                comboBox_ChiHuyD.BeginUpdate();
                comboBox_ChiHuyD.DataSource = null;
                comboBox_ChiHuyD.Items.Clear();
                var items = new List<ComboItem>();
                using (var conn = TaoKetNoiCSDL2(true))
                {
                    await conn.OpenAsync();
                    // Sửa query chỉ vào bảng động
                    string sqlChiHuy = $"SELECT ID, HoVaTen FROM [{tableChiHuy}] WHERE HoVaTen IS NOT NULL ORDER BY ID ASC";
                    using (var cmd = new SqliteCommand(sqlChiHuy, conn))
                    using (var rd = await cmd.ExecuteReaderAsync())
                    {
                        while (await rd.ReadAsync())
                        {
                            int id = rd.GetInt32(0);
                            string name = SafeDecrypt(rd["HoVaTen"]);
                            if (!string.IsNullOrWhiteSpace(name))
                            {
                                items.Add(new ComboItem { ID = id, Text = name.Trim() });
                            }
                        }
                    }
                    if (items.Count == 0)
                    {
                        comboBox_ChiHuyD.EndUpdate();
                        return;
                    }
                    comboBox_ChiHuyD.DisplayMember = "Text";
                    comboBox_ChiHuyD.ValueMember = "ID";
                    comboBox_ChiHuyD.DataSource = items;
                    const string sqlThongTin = "SELECT ChiHuyD FROM ThongTin WHERE ID = 1 LIMIT 1";
                    using (var cmd2 = new SqliteCommand(sqlThongTin, conn))
                    using (var rd2 = await cmd2.ExecuteReaderAsync())
                    {
                        if (await rd2.ReadAsync() && !rd2.IsDBNull(0))
                        {
                            string savedName = SafeDecrypt(rd2.GetString(0)).Trim();
                            var match = items.FirstOrDefault(x => string.Equals(x.Text, savedName, StringComparison.OrdinalIgnoreCase));
                            if (match != null)
                            {
                                comboBox_ChiHuyD.SelectedValue = match.ID;
                            }
                            else if (comboBox_ChiHuyD.Items.Count > 0)
                            {
                                comboBox_ChiHuyD.SelectedIndex = 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi LoadComboBox_ChiHuyDAsync: " + ex.Message);
            }
            finally
            {
                comboBox_ChiHuyD.EndUpdate();
            }
        }
        public async Task LoadDiaDiemAsync()
        {
            // 1. KIỂM TRA DATABASE
            if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path)) { return; }
            if (comboBox_DiaDiem == null || comboBox_DiaDiem.IsDisposed || comboBox_DiaDiem.Disposing) { return; }
            // 2. CẤU HÌNH COMBOBOX: CHỈ CHO PHÉP CHỌN - KHÔNG CHO NHẬP
            comboBox_DiaDiem.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox_DiaDiem.AutoCompleteMode = AutoCompleteMode.None;
            comboBox_DiaDiem.AutoCompleteSource = AutoCompleteSource.None;
            comboBox_DiaDiem.TabStop = false;
            comboBox_DiaDiem.Enabled = false;
            try
            {
                var danhSachDiaDiem = new List<string>();
                string? diaDiemDaLuu = null;
                // 3. ĐỌC SQLITE
                using (var conn = TaoKetNoiCSDL2(true))
                {
                    await conn.OpenAsync().ConfigureAwait(false);
                    // LẤY DANH SÁCH TỈNH / THÀNH PHỐ (DỮ LIỆU TEXT THUẦN - KHÔNG AES)
                    const string sqlDiaDiem = """
                    SELECT DISTINCT TenTinhVaThanhPho
                    FROM TinhVaThanhPho
                    WHERE TenTinhVaThanhPho IS NOT NULL
                      AND TRIM(TenTinhVaThanhPho) <> ''
                    ORDER BY TenTinhVaThanhPho COLLATE NOCASE ASC;
                    """;
                    using (var cmd = new SqliteCommand(sqlDiaDiem, conn))
                    using (var rd = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await rd.ReadAsync().ConfigureAwait(false))
                        {
                            if (rd.IsDBNull(0)) continue;
                            string ten = rd.GetString(0).Trim();
                            if (!string.IsNullOrWhiteSpace(ten)) { danhSachDiaDiem.Add(ten); }
                        }
                    }
                    // LẤY ĐỊA ĐIỂM ĐÃ LƯU
                    const string sqlDiaDiemDaLuu = """
                    SELECT DiaDiem
                    FROM ThongTin
                    WHERE ID = 1
                    LIMIT 1;
                    """;
                    using (var cmd = new SqliteCommand(sqlDiaDiemDaLuu, conn))
                    {
                        object? value = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                        if (value != null && value != DBNull.Value)
                        {
                            string chuoiMaHoa = value.ToString() ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(chuoiMaHoa))
                            {
                                try
                                {
                                    // Giữ giải mã ở đây vì cột ThongTin.DiaDiem hiện vẫn đang mã hóa.
                                    diaDiemDaLuu = Module_BaoMatAES.GiaiMa(chuoiMaHoa)?.Trim();
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine("LoadDiaDiemAsync - " + $"Giải mã DiaDiem thất bại: {ex.Message}");
                                    diaDiemDaLuu = null;
                                }
                            }
                        }
                    }
                }
                // 4. KIỂM TRA FORM / CONTROL
                if (IsDisposed || Disposing || comboBox_DiaDiem.IsDisposed || comboBox_DiaDiem.Disposing) { return; }
                // 5. CẬP NHẬT COMBOBOX
                comboBox_DiaDiem.BeginUpdate();
                try
                {
                    comboBox_DiaDiem.Items.Clear();
                    if (danhSachDiaDiem.Count > 0) { comboBox_DiaDiem.Items.AddRange(danhSachDiaDiem.ToArray()); }
                    // KHÔI PHỤC ĐỊA ĐIỂM ĐÃ LƯU
                    int index = -1;
                    if (!string.IsNullOrWhiteSpace(diaDiemDaLuu))
                    {
                        index = danhSachDiaDiem.FindIndex(x => string.Equals(x, diaDiemDaLuu, StringComparison.OrdinalIgnoreCase));
                    }
                    if (index >= 0) { comboBox_DiaDiem.SelectedIndex = index; }                         // Có địa điểm đã lưu → chọn nó
                    else if (comboBox_DiaDiem.Items.Count > 0) { comboBox_DiaDiem.SelectedIndex = 0; }  // Không có → chọn phần tử đầu tiên
                    else { comboBox_DiaDiem.SelectedIndex = -1; }
                }
                finally { comboBox_DiaDiem.EndUpdate(); }
            }
            catch (OperationCanceledException) { /* Người dùng/form bị hủy thao tác. */ }
            catch (Exception ex) { Debug.WriteLine($"LoadDiaDiemAsync: {ex}"); }
            finally
            {
                // 6. LUÔN MỞ KHÓA CONTROL SAU KHI LOAD
                if (comboBox_DiaDiem != null && !comboBox_DiaDiem.IsDisposed && !comboBox_DiaDiem.Disposing)
                {
                    comboBox_DiaDiem.Enabled = true;
                    // Đảm bảo luôn giữ chế độ CHỈ CHỌN
                    comboBox_DiaDiem.DropDownStyle = ComboBoxStyle.DropDownList;
                    comboBox_DiaDiem.AutoCompleteMode = AutoCompleteMode.None;
                    comboBox_DiaDiem.AutoCompleteSource = AutoCompleteSource.None;
                }
            }
        }
        private async Task LoadComboBoxLoaiXuat_CheckBoxAsync()
        {
            string csdlPath = _csdl2Path;
            if (string.IsNullOrWhiteSpace(csdlPath) || !File.Exists(csdlPath)) return;
            try
            {
                using var conn = new SqliteConnection($"Data Source={csdlPath}");
                await conn.OpenAsync();
                using var cmd = new SqliteCommand("SELECT ChonDanhSachXuat, Chex_MoiThuMucXuat FROM ThongTin WHERE ID = 1", conn);
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    string chonXuat = reader["ChonDanhSachXuat"]?.ToString();
                    comboBox1_ChonLoaiDeXuat.Text = string.IsNullOrWhiteSpace(chonXuat) ? "" : Module_BaoMatAES.GiaiMa(chonXuat);
                    string chex = reader["Chex_MoiThuMucXuat"]?.ToString();
                    chex = string.IsNullOrWhiteSpace(chex) ? "FALSE" : Module_BaoMatAES.GiaiMa(chex).ToUpper();
                    Check_MoThuMuc.Checked = chex == "TRUE";
                }
            }
            catch (Exception ex) { Debug.WriteLine("Lỗi LoadComboBoxLoaiXuat: " + ex.Message); }
        }
        private async Task LoadThongTinAsync()
        {
            try
            {
                // 1. Kiểm tra phiên bản ngay từ đầu
                bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                // Khai báo biến hứng dữ liệu
                string chiHuyD = null, loaiDeNghi = null, chonLoaiDeXuat = null, chexMoiThuMuc = null;
                string ngay = null, thang = null, nam = null;
                string loaiBaoCao = null, chonTuan = null;
                string thangHeThongRaw = null;
                using (var conn = TaoKetNoiCSDL2(true))
                {
                    await conn.OpenAsync();
                    // 2. Xây dựng SQL động: CBCS không JOIN lấy B.ChonLoaiBaoCao, B.ChonTuan
                    string sqlMain = laTanBinh
                        ? @"SELECT T.ChiHuyD, T.LoaiDeNghi, T.ChonDanhSachXuat, T.Chex_MoiThuMucXuat, 
                           T.Ngay, T.Thang, T.Nam, B.ChonLoaiBaoCao, B.ChonTuan 
                    FROM ThongTin T 
                    LEFT JOIN ChonLoaiBaoCao B ON B.ID = 1 
                    WHERE T.ID = 1 LIMIT 1"
                        : @"SELECT T.ChiHuyD, T.LoaiDeNghi, T.ChonDanhSachXuat, T.Chex_MoiThuMucXuat, 
                           T.Ngay, T.Thang, T.Nam 
                    FROM ThongTin T 
                    WHERE T.ID = 1 LIMIT 1";
                    using (var cmd = new SqliteCommand(sqlMain, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            chiHuyD = SafeDecrypt(reader["ChiHuyD"]);
                            loaiDeNghi = SafeDecrypt(reader["LoaiDeNghi"]);
                            chonLoaiDeXuat = SafeDecrypt(reader["ChonDanhSachXuat"]);
                            chexMoiThuMuc = SafeDecrypt(reader["Chex_MoiThuMucXuat"]);
                            ngay = SafeDecrypt(reader["Ngay"]);
                            thang = SafeDecrypt(reader["Thang"]);
                            nam = SafeDecrypt(reader["Nam"]);
                            // CHỈ đọc thông tin Báo cáo nếu là TÂN BINH
                            if (laTanBinh)
                            {
                                loaiBaoCao = SafeDecrypt(reader["ChonLoaiBaoCao"]);
                                chonTuan = SafeDecrypt(reader["ChonTuan"]);
                            }
                        }
                    }
                    // 3. Xử lý ThangHeThong: CHỈ thực hiện đối với TÂN BINH
                    if (laTanBinh)
                    {
                        const string sqlCreateTable = "CREATE TABLE IF NOT EXISTS ThangHeThong (ID INTEGER PRIMARY KEY, Thang TEXT);";
                        using (var cmdTaoBang = new SqliteCommand(sqlCreateTable, conn))
                        {
                            await cmdTaoBang.ExecuteNonQueryAsync();
                        }
                        const string sqlThang = "SELECT Thang FROM ThangHeThong WHERE ID = 1 LIMIT 1";
                        using (var cmdThang = new SqliteCommand(sqlThang, conn))
                        {
                            var resThang = await cmdThang.ExecuteScalarAsync();
                            if (resThang != null && resThang != DBNull.Value)
                            {
                                thangHeThongRaw = resThang.ToString().Trim();
                            }
                        }
                    }
                }
                // --- CẬP NHẬT GIAO DIỆN (UI) ---
                // 4. Các Control chung (Cả Tân Binh và CBCS đều dùng)
                comboBox_ChiHuyD.Text = chiHuyD;
                com_DeNghi.Text = loaiDeNghi;
                comboBox1_ChonLoaiDeXuat.Text = chonLoaiDeXuat;
                Check_MoThuMuc.Checked = string.Equals(chexMoiThuMuc, "TRUE", StringComparison.OrdinalIgnoreCase);
                if (!checkBox1_TuDongChonNgayThang.Checked)
                {
                    comboBox_Ngay.Text = ngay;
                    comboBox_Thang.Text = thang;
                    comboBox_Nam.Text = nam;
                }
                // 5. Phân nhánh UI triệt để cho 3 ComboBox báo cáo
                if (!laTanBinh)
                {
                    //CHẾ ĐỘ CBCS ==
                    // Ẩn hoàn toàn khỏi giao diện
                    comboBox2_ChonSoThang.Visible = false;
                    comboBox1_ChonLoaiBaoCao.Visible = false;
                    comboBox2_ChonSoTuan.Visible = false;
                    // Đảm bảo không khóa nhầm hoặc trigger sự kiện dư thừa
                    comboBox1_ChonLoaiBaoCao.Enabled = false;
                }
                else
                {
                    //CHẾ ĐỘ TÂN BINH ==
                    // Hiện các Control
                    comboBox2_ChonSoThang.Visible = true;
                    comboBox1_ChonLoaiBaoCao.Visible = true;
                    comboBox1_ChonLoaiBaoCao.Enabled = true;
                    // Load giá trị Số tháng
                    if (!string.IsNullOrEmpty(thangHeThongRaw))
                    {
                        if (int.TryParse(thangHeThongRaw, out int soThang))
                        {
                            bool matchFound = false;
                            foreach (var item in comboBox2_ChonSoThang.Items)
                            {
                                if (int.TryParse(item.ToString(), out int val) && val == soThang)
                                {
                                    comboBox2_ChonSoThang.SelectedItem = item;
                                    matchFound = true;
                                    break;
                                }
                            }
                            if (!matchFound) comboBox2_ChonSoThang.Text = soThang.ToString();
                        }
                        else
                        {
                            comboBox2_ChonSoThang.Text = thangHeThongRaw;
                        }
                    }
                    // Load Loại Báo Cáo & Số Tuần
                    SetComboBoxValue(comboBox1_ChonLoaiBaoCao, loaiBaoCao);
                    SetComboBoxValue(comboBox2_ChonSoTuan, chonTuan);
                    // Cập nhật hiển thị ẩn/hiện com_ChonSoTuan theo loại báo cáo vừa chọn (Tháng vs Tuần)
                    CapNhatTrangThaiTuan();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi LoadThongTin Form 4: {ex.Message}");
            }
        }
        /// <summary>
        /// Hàm bổ trợ gán giá trị an toàn cho ComboBox
        /// </summary>
        private void SetComboBoxValue(ComboBox cb, string value)
        {
            if (cb == null || string.IsNullOrEmpty(value)) return;
            int index = cb.FindStringExact(value);
            if (index >= 0)
            {
                cb.SelectedIndex = index;
            }
            else
            {
                cb.Text = value; // Fallback nếu chuỗi từ DB không có sẵn trong Items
            }
        }
        public async Task CapNhatDanhSachPhanLoaiDeXuatAsync()
        {
            string selectedGoc = comboBox1_ChonLoaiDeXuat.Text;
            try
            {
                comboBox1_ChonLoaiDeXuat.BeginUpdate();
                comboBox1_ChonLoaiDeXuat.Items.Clear();
                // Đẩy thuật toán duyệt 10.000 dòng xuống luồng phụ
                List<string> danhSachDaLoc = await Task.Run(() => Form6_XuLyData.LayDanhSachPhanLoaiThucTe());
                foreach (var item in danhSachDaLoc)
                {
                    comboBox1_ChonLoaiDeXuat.Items.Add(item);
                }
                if (!string.IsNullOrWhiteSpace(selectedGoc) && comboBox1_ChonLoaiDeXuat.Items.Contains(selectedGoc))
                {
                    comboBox1_ChonLoaiDeXuat.Text = selectedGoc;
                }
                else if (comboBox1_ChonLoaiDeXuat.Items.Count > 0)
                {
                    comboBox1_ChonLoaiDeXuat.SelectedIndex = 0;
                }
                else
                {
                    comboBox1_ChonLoaiDeXuat.Text = "";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi nạp combobox Loại đề xuất: " + ex.Message);
            }
            finally
            {
                comboBox1_ChonLoaiDeXuat.EndUpdate();
            }
        }
        private void CauHoiGiaoDien_PhienBan(bool laTanBinh)
        {
            if (laTanBinh)
            {
                // 🟢 CHẾ ĐỘ TÂN BINH: Mở ra cho phép thao tác
                if (label4 != null) label4.Visible = true;
                comboBox1_ChonLoaiBaoCao.Visible = true;
                comboBox1_ChonLoaiBaoCao.Enabled = true;
                // ⭐ TÂN BINH: Mở phần chọn số tháng
                if (label2_LabelThang != null) label2_LabelThang.Visible = true;
                if (comboBox2_ChonSoThang != null) comboBox2_ChonSoThang.Visible = true;
                // Gọi hàm này để nó tự tính toán ẩn/hiện "Tuần" 
                // dựa trên giá trị đang chọn trong comboBox1
                CapNhatTrangThaiTuan();
            }
            else
            {
                // 🔴 CHẾ ĐỘ CBCS: Đóng lại, ẩn mình hoàn toàn
                if (label4 != null) label4.Visible = false;
                comboBox1_ChonLoaiBaoCao.Visible = false;
                // Ẩn luôn các phần liên quan đến "Tuần"
                if (label2_ChonTuan != null) label2_ChonTuan.Visible = false;
                if (comboBox2_ChonSoTuan != null) comboBox2_ChonSoTuan.Visible = false;
                // ⭐ CBCS: Ẩn phần chọn số tháng cho giao diện gọn
                if (label2_LabelThang != null) label2_LabelThang.Visible = false;
                if (comboBox2_ChonSoThang != null) comboBox2_ChonSoThang.Visible = false;
                // Mặc định giá trị ngầm là "Tháng" để các hàm Xuất file 
                // ở phía sau vẫn chạy đúng logic cho CBCS
                comboBox1_ChonLoaiBaoCao.Text = "Tháng";
            }
        }
        private void Form4_TrangDauTien_Resize(object? sender, EventArgs e)
        {
            if (splitContainer2 == null || WindowState == FormWindowState.Minimized) return;
            int target = splitContainer2.Height / 2;
            // Chỉ gán khi thật sự lệch và hợp lệ với MinSize (tránh exception khi form quá nhỏ)
            if (Math.Abs(splitContainer2.SplitterDistance - target) > 2 &&
                target >= splitContainer2.Panel1MinSize &&
                splitContainer2.Height - target >= splitContainer2.Panel2MinSize)
            {
                splitContainer2.SplitterDistance = target;
            }
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadComboBoxDeNghi();
            Form27_TyLeQuyDinhE29.OnQuyDinhChanged += ReloadQuyDinh;
            // 🔥 set mặc định để có SelectedItem
            if (com_DeNghi.SelectedIndex == -1)
                com_DeNghi.SelectedIndex = 0;
        }
        private void ReloadQuyDinh()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(ReloadQuyDinh));
                return;
            }
            LoadQuyDinhTheoDeNghi(); // 🔥 GỌI TRỰC TIẾP
        }
        private void CapNhatTrangThaiTuan()
        {
            if (comboBox1_ChonLoaiBaoCao.SelectedItem == null)
                return;
            bool laTuan = comboBox1_ChonLoaiBaoCao.Text
                .Equals("Tuần", StringComparison.OrdinalIgnoreCase);
            // Hiện / Ẩn control
            label2_ChonTuan.Visible = laTuan;
            comboBox2_ChonSoTuan.Visible = laTuan;
            if (!laTuan)
            {
                comboBox2_ChonSoTuan.SelectedIndex = -1;
                return;
            }
            // == GỢI Ý TUẦN THEO NGÀY ==
            if (string.IsNullOrWhiteSpace(comboBox2_ChonSoTuan.Text))
            {
                int day = DateTime.Now.Day;
                int tuan =
                    day <= 7 ? 1 :
                    day <= 14 ? 2 :
                    day <= 21 ? 3 : 4;
                string goiY = $"Tuần {tuan}";
                if (comboBox2_ChonSoTuan.Items.Contains(goiY))
                    comboBox2_ChonSoTuan.SelectedItem = goiY;
            }
        }
        private void comboBox1_ChonLoaiBaoCao_SelectedIndexChanged(object? sender, EventArgs e)
        {
            CapNhatTrangThaiTuan();
        }
        // 🌟 TỐI ƯU HIỆU SUẤT: Cờ chặn chống gọi hàm lặp lại gây tốn CPU
        private void LoadCheckBoxTuDongChonNgayThang()
        {
            bool isChecked = false;
            string csdlPath = _csdl2Path;
            try
            {
                if (!string.IsNullOrWhiteSpace(csdlPath) && File.Exists(csdlPath))
                {
                    using var conn = new SqliteConnection($"Data Source={csdlPath}");
                    conn.Open();
                    using var cmd = new SqliteCommand("SELECT TuDongGanNgayThangNamHienTai FROM ThongTin WHERE ID = 1", conn);
                    var result = cmd.ExecuteScalar()?.ToString();
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        string giaiMa = Module_BaoMatAES.GiaiMa(result)?.Trim().ToUpper();
                        isChecked = giaiMa == "TRUE";
                    }
                }
            }
            catch
            {
                isChecked = false;
            }
            // Set trạng thái, màu, chỉ chọn item (không xóa thêm)
            checkBox1_TuDongChonNgayThang.Checked = isChecked;
            checkBox1_TuDongChonNgayThang.ForeColor = isChecked ? Color.Green : Color.Red;
            GanNgayThangNamVaoCombobox(isChecked); // Chỉ chọn và khóa nếu cần
        }
        private void CapNhatThongBaoPhanMem()
        {
            try
            {
                string fileCSDL2 = _csdl2Path;
                if (!File.Exists(fileCSDL2)) return;
                string doiTuong = "";
                using (var cn = new SqliteConnection($"Data Source={fileCSDL2}"))
                {
                    cn.Open();
                    using var cmd = cn.CreateCommand();
                    cmd.CommandText = "SELECT DoiTuong FROM PhienBan_DoiTuong WHERE ID = 1";
                    object? val = cmd.ExecuteScalar();
                    if (val != null)
                    {
                        string? chuoiMaHoa = val.ToString();
                        if (!string.IsNullOrWhiteSpace(chuoiMaHoa))
                        {
                            doiTuong = Module_BaoMatAES.GiaiMa(chuoiMaHoa);
                        }
                    }
                }
                // Khi thiết lập StatusStrip
                toolStripStatusLabel2.Spring = true;      // chiếm phần còn lại
                toolStripStatusLabel2.TextAlign = ContentAlignment.MiddleRight;
                // Hiển thị ở toolStripStatusLabel2, căn phải
                toolStripStatusLabel2.Text = $"Phần mềm: {doiTuong} - Phiên bản " + Module_PhienBan.SoftwareVersion;
                toolStripStatusLabel2.TextAlign = ContentAlignment.MiddleRight;
            }
            catch
            {
                toolStripStatusLabel2.Text = "Phần mềm: (không xác định)";
            }
        }
        private void LuuLuaChonXuatVaCheckBox()
        {
            string csdlPath = _csdl2Path;
            if (string.IsNullOrWhiteSpace(csdlPath) || !File.Exists(csdlPath)) return;
            try
            {
                using var conn = new SqliteConnection($"Data Source={csdlPath}");
                conn.Open();
                // Mã hóa dữ liệu
                string chonXuat = Module_BaoMatAES.MaHoa(comboBox1_ChonLoaiDeXuat.Text.Trim());
                string chex = Module_BaoMatAES.MaHoa(Check_MoThuMuc.Checked ? "TRUE" : "FALSE");
                // Đảm bảo bản ghi ID=1 tồn tại
                using (var cmdCheck = new SqliteCommand("SELECT COUNT(*) FROM ThongTin WHERE ID = 1", conn))
                {
                    long count = (long)cmdCheck.ExecuteScalar();
                    if (count == 0)
                    {
                        using var cmdInsert = new SqliteCommand("INSERT INTO ThongTin (ID) VALUES (1)", conn);
                        cmdInsert.ExecuteNonQuery();
                    }
                }
                // Update
                using var cmdUpdate = new SqliteCommand(@"
UPDATE ThongTin SET
    ChonDanhSachXuat = @ChonDanhSachXuat,
    Chex_MoiThuMucXuat = @Chex_MoiThuMucXuat
WHERE ID = 1", conn);
                cmdUpdate.Parameters.AddWithValue("@ChonDanhSachXuat", chonXuat);
                cmdUpdate.Parameters.AddWithValue("@Chex_MoiThuMucXuat", chex);
                cmdUpdate.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Module_ThongBao.Loi("Lỗi lưu lựa chọn xuất / mở thư mục: " + ex.Message);
            }
        }
        private void CapNhatMauCheckMoThuMuc() => Check_MoThuMuc.ForeColor = Check_MoThuMuc.Checked ? Color.Green : Color.Red;
        private void Check_MoThuMuc_CheckedChanged(object? sender, EventArgs e)
        {
            CapNhatMauCheckMoThuMuc();
            LuuLuaChonXuatVaCheckBox();
        }
        private void comboBox1_ChonLoaiDeXuat_SelectedIndexChanged(object? sender, EventArgs e) => LuuLuaChonXuatVaCheckBox();
        private void LoadSettings() => CapNhatMauCheckMoThuMuc();
        public void LoadComboBoxDeNghi()
        {
            // 1. Kiểm tra điều kiện: Là phiên bản CBCS VÀ ComboBox đang chọn "Năm"
            bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
            bool isCheDoNam = comboBox1_CheDoXetThiDua.Text.Trim().Equals("Năm", StringComparison.OrdinalIgnoreCase);
            bool isKichHoatAnhXa = !laTanBinh && isCheDoNam;
            // 2. Chọn tập danh sách hiển thị tương ứng
            string[] itemsMoi = isKichHoatAnhXa
                ? new string[] { Module_HeThong.XLDV_DVQT, Module_HeThong.XLDV_DVTT, Module_HeThong.XLDV_HTNV, Module_HeThong.XLDV_KHTNV, Module_HeThong.PL_KHONG_PL }
                : new string[] { Module_HeThong.Loai_1, Module_HeThong.Loai_2, Module_HeThong.Loai_3, Module_HeThong.Loai_4, Module_HeThong.PL_KHONG_PL };
            // 3. Nạp vào com_DeNghi (giữ lại vị trí đang chọn nếu hợp lệ)
            int indexCu = com_DeNghi.SelectedIndex;
            com_DeNghi.BeginUpdate();
            try
            {
                com_DeNghi.Items.Clear();
                com_DeNghi.Items.AddRange(itemsMoi);
                // Giữ lại vị trí Index cũ (ví dụ đang chọn dòng 0 thì sau khi đổi vẫn chọn dòng 0)
                if (indexCu >= 0 && indexCu < com_DeNghi.Items.Count)
                {
                    com_DeNghi.SelectedIndex = indexCu;
                }
                else if (com_DeNghi.Items.Count > 0)
                {
                    com_DeNghi.SelectedIndex = 0;
                }
            }
            finally
            {
                com_DeNghi.EndUpdate();
            }
        }
        private void InitToolTips()
        {
            // Chống gọi lại nhiều lần không cần thiết
            if (_daKhoiTaoToolTip) return;
            // An toàn từ gốc: Kiểm tra ToolTip có tồn tại không
            if (toolTip1 == null) return;
            try
            {
                //CẤU HÌNH CHUNG ==
                toolTip1.IsBalloon = true;
                toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
                toolTip1.ToolTipIcon = ToolTipIcon.Info;
                // UX: phản hồi nhanh – không gây khó chịu
                toolTip1.InitialDelay = 300;
                toolTip1.AutoPopDelay = 2500;
                toolTip1.ReshowDelay = 100;
                toolTip1.ShowAlways = true;
                // 🌟 TỐI ƯU CẤU TRÚC RAM: Dùng mảng ValueTuple thay cho Dictionary
                // Triệt tiêu chi phí băm (Hashing Overhead) và dọn sạch Heap Allocation.
                (Control? control, string noiDung)[] danhSachToolTip = new (Control?, string)[]
                {
 //LƯU / KIỂM TRA ==
                    (kryptonButton_LuuThongTin,        "Lưu toàn bộ thông tin đã nhập"),
                    (kryptonButton_Refresh,            "Làm mới dữ liệu và nhập lại từ đầu"),
                    (kryptonButton_KiemTraTLvaQS,      "Kiểm tra quân số và tỷ lệ theo dữ liệu hiện có"),
                    (kryptonButton_MayTinh,            "Mở công cụ máy tính hỗ trợ tính toán nhanh"),
 //XUẤT TỆP ==
                    (kryptonButton_ChonDuongDanLuu,    "Chọn đường dẫn để lưu tệp xuất ra"),
                    (Check_MoThuMuc,                   "Tự động mở thư mục chứa tệp sau khi xuất"),
                    (comboBox1_ChonLoaiDeXuat,         "Chọn loại dữ liệu cần xuất ra Excel"),
                    (kryptonButton_XuatDanhSachLoai,   "Xuất danh sách Excel theo loại đã chọn"),
                    (kryptonButton_XuatTrinhKy,        "Xuất tệp trình ký theo mẫu quy định"),
                    (kryptonButton_XuatTatCa,          "Xuất toàn bộ dữ liệu ra các tệp Excel"),
                    (kryptonButton_MoThuMuc,           "Mở thư mục chứa các tệp đã xuất"),
                    (kryptonButton1_TomTatThanhTich,           "Mở form Tóm tắt tổng hợp thành tích tập thể"),
                    (kryptonButton1_XuatTepPdf,        "Xuất tệp *.pdf để gửi lên phần mềm QLVB ĐHTN")
                };
                // 🌟 XỬ LÝ LỖI PHÂN MẢNH (ISOLATED EXCEPTION)
                int soLoi = 0;
                foreach (var (control, noiDung) in danhSachToolTip)
                {
                    // Bỏ qua an toàn nếu control chưa kịp render hoặc bị null
                    if (control == null)
                    {
                        soLoi++;
                        continue;
                    }
                    try
                    {
                        // Kiểm tra vòng đời của Control trước khi gán API
                        if (control.IsDisposed) continue;
                        toolTip1.SetToolTip(control, noiDung);
                    }
                    catch
                    {
                        // Bẫy lỗi cục bộ: Lỗi ở 1 nút không làm sập vòng lặp gán của các nút khác
                        soLoi++;
                    }
                }
                //if DEBUG
                // Hệ thống cảnh báo nội bộ dành riêng cho Lập trình viên (Không hiện ở bản Release)
                if (soLoi > 0)
                    System.Diagnostics.Debug.WriteLine($"[InitToolTips] Hệ thống bỏ qua {soLoi} control do chưa khởi tạo hoặc bị null.");
                //endif
                // Đánh dấu hoàn tất để khóa cổng
                _daKhoiTaoToolTip = true;
            }
            catch (Exception ex)
            {
                // Bắt lỗi tổng và in ra Output để Lập trình viên theo dõi
                System.Diagnostics.Debug.WriteLine($"[Lỗi nghiêm trọng tại InitToolTips]: {ex.Message}");
            }
        }
        private void Form4_FormClosing(object? sender, FormClosingEventArgs e)
        {
            Module_DanduongGPS.OnDatabaseChanged -= SuKien_DatabaseChanged;
            Form27_TyLeQuyDinhE29.OnQuyDinhChanged -= ReloadQuyDinh;   // MỚI
            _uiDecryptCache.Clear();
            _appIcon?.Dispose();
            _iconTrue?.Dispose();
            _iconFalse?.Dispose();
            _cachedGridFont?.Dispose();
            _cachedGridFontBold?.Dispose();
            _cachedGrid2HeaderFont?.Dispose();
            _pieFont?.Dispose();                                        // MỚI (bước 5)
            if (formTinhToan != null && !formTinhToan.IsDisposed) formTinhToan.Dispose();
            if (form11 != null && !form11.IsDisposed) form11.Dispose();
        }
        private void SuKien_DatabaseChanged()
        {
            if (this.IsHandleCreated && !this.IsDisposed)
            {
                this.BeginInvoke(new Action(async () => await ReloadDuLieuAsync()));
            }
        }
        private void KryptonDataGridView2_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            // 1. Guard Clauses (Kiểm tra an toàn và thoát sớm)
            if (sender is not DataGridView dgv) return;
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgv.Columns[e.ColumnIndex].Name != "Ket luan") return;
            // Chỉ xử lý dòng có STT (ID) = 1
            var sttCell = dgv.Rows[e.RowIndex].Cells["ID"].Value?.ToString();
            if (sttCell != "1") return;
            string noiDung = e.Value?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(noiDung)) return;
            // 2. Xác định trạng thái, Icon và Màu sắc mặc định
            bool isChuaDat = noiDung.IndexOf("Chưa đạt", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isDaDat = noiDung.IndexOf("Đạt tỷ lệ", StringComparison.OrdinalIgnoreCase) >= 0;
            Image iconToDraw = null;
            Color textColor = e.CellStyle.ForeColor;
            if (isChuaDat)
            {
                iconToDraw = _iconFalse;
                textColor = Color.Red;
            }
            else if (isDaDat)
            {
                iconToDraw = _iconTrue;
                textColor = Color.DarkGreen;
            }
            // Nếu không thỏa điều kiện nào, nhả lại cho DataGridView tự vẽ và thoát
            if (iconToDraw == null)
            {
                e.PaintContent(e.CellBounds);
                return;
            }
            // 3. Bắt đầu vẽ: Xóa nền gốc, giữ lại hiệu ứng Bôi đen/Focus
            e.Paint(e.CellBounds, DataGridViewPaintParts.Background |
                                  DataGridViewPaintParts.SelectionBackground |
                                  DataGridViewPaintParts.Focus);
            // Đổi màu chữ thành trắng nếu dòng đang được bôi đen (chọn)
            if ((e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected)
            {
                textColor = e.CellStyle.SelectionForeColor;
            }
            // 4. Các hằng số kích thước
            const int ICON_SIZE = 16;
            const int PADDING_ICON_TEXT = 6;
            // 5. Tính toán tọa độ và vẽ (dùng font cache, KHÔNG new Font mỗi ô)
            Font boldFont = _cachedGridFontBold ?? e.CellStyle.Font;
            int textWidth = TextRenderer.MeasureText(e.Graphics, noiDung, boldFont).Width;
            int totalContentWidth = ICON_SIZE + PADDING_ICON_TEXT + textWidth;
            int xIcon = e.CellBounds.X + Math.Max(0, (e.CellBounds.Width - totalContentWidth) / 2);
            int yIcon = e.CellBounds.Y + (e.CellBounds.Height - ICON_SIZE) / 2;
            e.Graphics.DrawImage(iconToDraw, new Rectangle(xIcon, yIcon, ICON_SIZE, ICON_SIZE));
            int textStartX = xIcon + ICON_SIZE + PADDING_ICON_TEXT;
            Rectangle textBounds = new Rectangle(
                textStartX,
                e.CellBounds.Y,
                Math.Max(0, e.CellBounds.Width - (textStartX - e.CellBounds.X)),
                e.CellBounds.Height);
            TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left |
                                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;
            TextRenderer.DrawText(e.Graphics, noiDung, boldFont, textBounds, textColor, flags);
            // 6. Khóa sự kiện: Báo cho hệ thống biết ta đã tự tay vẽ xong, không cần vẽ đè văn bản gốc lên nữa
            e.Handled = true;
        }
        private readonly SemaphoreSlim _reloadLock = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<string, string> _uiDecryptCache = new(StringComparer.Ordinal);
        private class ThongKeDonVi
        {
            public int TongQS { get; set; }
            public int Loai1 { get; set; }
            public int Loai2 { get; set; }
            public int Loai3 { get; set; }
            public int Loai4 { get; set; }
            public int KhongPL { get; set; }
        }
        private void KryptonDataGridView1_RowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
        {
            var grid = sender as DataGridView;
            if (grid == null || grid.Font == null) return;
            string rowIdx = (e.RowIndex + 1).ToString();
            Rectangle headerBounds = new Rectangle(
                e.RowBounds.Left,
                e.RowBounds.Top,
                grid.RowHeadersWidth,
                e.RowBounds.Height);
            // Tuyệt đối KHÔNG DÙNG new Font() ở đây. Dùng luôn grid.Font
            // Dùng TextRenderer siêu tốc của Windows Core thay cho Graphics.DrawString
            TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding;
            TextRenderer.DrawText(e.Graphics, rowIdx, grid.Font, headerBounds, Color.Black, flags);
        }
        private HashSet<string> _dsDonViBoQuaCanhBao = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private void DieuChinhCoChuLabel11()
        {
            // Bỏ qua nếu label chưa được khởi tạo hoặc đang rỗng
            if (label11 == null || string.IsNullOrWhiteSpace(label11.Text)) return;
            string duongDan = label11.Text.Trim();
            float coChuMoi = label11.Font.Size; // Lấy cỡ chữ hiện tại làm gốc
            // 🌟 ƯU TIÊN 1: Nếu đường dẫn dài từ 70 ký tự trở lên -> Ép về cỡ 8 để chống tràn
            if (duongDan.Length >= 70)
            {
                coChuMoi = 8f;
            }
            // 🌟 ƯU TIÊN 2: Nếu kết thúc bằng chữ "Desktop" (Không phân biệt hoa/thường) -> Phóng to cỡ 10
            else if (duongDan.EndsWith("Desktop", StringComparison.OrdinalIgnoreCase))
            {
                coChuMoi = 10f;
            }
            else
            {
                // (Tùy chọn) Bạn có thể set cỡ chữ mặc định ở đây cho các trường hợp còn lại
                // coChuMoi = 9f; 
            }
            // TỐI ƯU HIỆU NĂNG: Chỉ khởi tạo lại đối tượng Font khi cỡ chữ thực sự có thay đổi
            if (label11.Font.Size != coChuMoi)
            {
                // Giữ nguyên kiểu chữ (FontFamily) và định dạng (Style: Bold, Italic...) hiện có
                label11.Font = new Font(label11.Font.FontFamily, coChuMoi, label11.Font.Style);
            }
        }
        private void GanNgayThangNamVaoCombobox(bool khoaCombobox)
        {
            if (comboBox_Ngay == null || comboBox_Thang == null || comboBox_Nam == null)
                return;
            DateTime now = DateTime.Now;
            int namHeThong = Module_HeThong.LayNamHeThong();
            // Khởi tạo 1 lần duy nhất
            if (!isComboBoxInitDone)
            {
                comboBox_Ngay.Items.Clear();
                comboBox_Thang.Items.Clear();
                comboBox_Nam.Items.Clear();
                for (int i = 1; i <= 31; i++)
                    comboBox_Ngay.Items.Add(i.ToString("D2"));
                // 🌟 SỬA TẠI ĐÂY: Thêm logic định dạng tháng cho Combobox
                for (int i = 1; i <= 12; i++)
                {
                    // Tháng 1, 2 hiển thị "01", "02". Tháng 3 đến 12 hiển thị "3", "4"... "12"
                    string hienThiThang = (i == 1 || i == 2) ? i.ToString("D2") : i.ToString();
                    comboBox_Thang.Items.Add(hienThiThang);
                }
                for (int i = namHeThong - 1; i <= namHeThong + 10; i++)
                    comboBox_Nam.Items.Add(i.ToString());
                isComboBoxInitDone = true;
            }
            // Gán giá trị hiện tại theo đúng quy luật mới
            comboBox_Ngay.SelectedItem = now.Day.ToString("D2");
            // Gán tháng hiện tại cho khớp với danh sách item vừa nạp
            comboBox_Thang.SelectedItem = (now.Month == 1 || now.Month == 2) ? now.Month.ToString("D2") : now.Month.ToString();
            comboBox_Nam.SelectedItem = namHeThong.ToString();
            // Enable / Disable đồng bộ
            bool enable = !khoaCombobox;
            comboBox_Ngay.Enabled = enable;
            comboBox_Thang.Enabled = enable;
            comboBox_Nam.Enabled = enable;
        }
        private void UocLuongDoRongCacCot(DataGridView dgv)
        {
            if (dgv == null || dgv.Columns.Count == 0) return;
            // --- 1️⃣ Ước lượng chiều rộng dựa trên header + nội dung ---
            Dictionary<string, int> colWidths = new Dictionary<string, int>();
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (!col.Visible) continue;
                int maxWidth = TextRenderer.MeasureText(col.HeaderText, dgv.ColumnHeadersDefaultCellStyle.Font).Width;
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.IsNewRow) continue;
                    string val = row.Cells[col.Name].Value?.ToString() ?? "";
                    int w = TextRenderer.MeasureText(val, dgv.DefaultCellStyle.Font).Width;
                    if (w > maxWidth) maxWidth = w;
                }
                // Giới hạn min/max để tránh quá hẹp hoặc quá rộng
                colWidths[col.Name] = Math.Min(Math.Max(maxWidth + 20, 30), 200);
            }
            // --- 2️⃣ Gán FillWeight theo cột và ước lượng ---
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (!col.Visible) continue;
                int width = colWidths.ContainsKey(col.Name) ? colWidths[col.Name] : 50;
                switch (col.Name)
                {
                    case "STT":
                        col.FillWeight = width * 0.5f; // STT nhỏ
                        break;
                    case "DonVi":
                        col.FillWeight = width * 1.5f;
                        break;
                    case "TongQS":
                    case "Loai_1":
                    case "Loai_2":
                    case "Loai_3":
                    case "Loai_4":
                    case "Khong_PL":
                        col.FillWeight = width * 2f;
                        break;
                    case "PhanTramLoai_1":
                    case "PhanTramLoai_2":
                    case "PhanTramLoai_3":
                    case "PhanTramLoai_4":
                    case "PhanTramKhong_PL":
                        col.FillWeight = width * 1.5f;
                        break;
                    default:
                        col.FillWeight = width * 1.5f;
                        break;
                }
            }
            // --- 3️⃣ Bật AutoSizeColumnsMode.Fill để full khung ---
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }
        private void checkBox1_TuDongChonNgayThang_CheckedChanged(object? sender, EventArgs e)
        {
            bool isChecked = checkBox1_TuDongChonNgayThang.Checked;
            // Set combobox
            GanNgayThangNamVaoCombobox(isChecked);
            // Set màu
            checkBox1_TuDongChonNgayThang.ForeColor = isChecked ? Color.Green : Color.Red;
            // Lưu vào CSDL
            try
            {
                string csdlPath = _csdl2Path;
                if (!string.IsNullOrWhiteSpace(csdlPath) && File.Exists(csdlPath))
                {
                    using var conn = new SqliteConnection($"Data Source={csdlPath}");
                    conn.Open();
                    using var cmd = new SqliteCommand(
                        "UPDATE ThongTin SET TuDongGanNgayThangNamHienTai = @val WHERE ID = 1", conn);
                    cmd.Parameters.AddWithValue("@val", Module_BaoMatAES.MaHoa(isChecked ? "TRUE" : "FALSE"));
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                ThongBao("Lỗi lưu trạng thái tự động ngày/tháng/năm: " + ex.Message);
            }
        }
        private void Progress_Start()
        {
            if (IsDisposed || Disposing) return;
            toolStripProgressBar1.Visible = true;
            toolStripProgressBar1.Minimum = 0;
            toolStripProgressBar1.Maximum = 100;
            toolStripProgressBar1.Value = 0;
        }
        private void Progress_Step(int value)
        {
            if (IsDisposed || Disposing) return;
            int newValue = toolStripProgressBar1.Value + value;
            if (newValue > toolStripProgressBar1.Maximum)
                newValue = toolStripProgressBar1.Maximum;
            if (newValue < toolStripProgressBar1.Minimum)
                newValue = toolStripProgressBar1.Minimum;
            toolStripProgressBar1.Value = newValue;
        }
        private void Progress_End()
        {
            if (IsDisposed || Disposing) return;
            toolStripProgressBar1.Value = toolStripProgressBar1.Maximum;
            toolStripProgressBar1.Visible = false;
        }
        private async Task Progress_StepAsync(int value)
        {
            if (IsDisposed || Disposing) return;
            int targetValue = 0;
            Action calcTarget = () =>
            {
                targetValue = toolStripProgressBar1.Value + value;
                if (targetValue > toolStripProgressBar1.Maximum) targetValue = toolStripProgressBar1.Maximum;
                if (targetValue < toolStripProgressBar1.Minimum) targetValue = toolStripProgressBar1.Minimum;
            };
            if (InvokeRequired) Invoke(calcTarget); else calcTarget();
            while (true)
            {
                if (IsDisposed || Disposing) break;
                bool isReached = false;
                Action stepUp = () =>
                {
                    // Tăng bước nhảy lên 2% mỗi khung hình thay vì 1% để trượt nhanh hơn
                    toolStripProgressBar1.Value += (toolStripProgressBar1.Value + 2 <= targetValue) ? 2 : 1;
                    if (toolStripProgressBar1.Value >= targetValue)
                        isReached = true;
                };
                if (InvokeRequired) Invoke(stepUp); else stepUp();
                if (isReached) break;
                // ⭐ ÉP XUNG: Giảm từ 15ms xuống còn 5ms
                await Task.Delay(5);
            }
        }
        private async Task Progress_EndAsync()
        {
            if (IsDisposed || Disposing) return;
            int current = 0;
            int max = 100;
            Action getValues = () =>
            {
                current = toolStripProgressBar1.Value;
                max = toolStripProgressBar1.Maximum;
            };
            if (InvokeRequired) Invoke(getValues); else getValues();
            if (current < max)
            {
                await Progress_StepAsync(max - current);
            }
            // ⭐ ÉP XUNG: Giảm thời gian khựng lại lúc đạt 100% từ 200ms xuống 50ms
            await Task.Delay(50);
            Action closeProgress = () =>
            {
                toolStripProgressBar1.Visible = false;
                toolStripProgressBar1.Value = 0;
            };
            if (InvokeRequired) Invoke(closeProgress); else closeProgress();
        }
        private void datChieuCaoNut()
        {
            int h = comboBox_DiaDiem.PreferredSize.Height;
            Krypton.Toolkit.KryptonButton[] buttons =
            {
                kryptonButton_Refresh,
                kryptonButton_KiemTraTLvaQS,
                kryptonButton_MayTinh,
                kryptonButton_XuatDanhSachLoai,
                kryptonButton_XuatTrinhKy,
                kryptonButton_XuatTatCa,
                kryptonButton_LuuThongTin,
                kryptonButton_ChonDuongDanLuu,
                kryptonButton_MoThuMuc,
                kryptonButton1_TomTatThanhTich,
                kryptonButton1_XuatTepPdf // 🌟 BỔ SUNG THÊM NÚT NÀY VÀO ĐÂY ĐỂ ĐỒNG BỘ CHIỀU CAO
            };
            foreach (var btn in buttons)
                btn.Height = h + 3;
        }
        private void ThongBao(string message)
        {
            if (this.InvokeRequired) // 'this' là Form
            {
                this.Invoke(new Action(() => toolStripStatusLabel1.Text = message));
            }
            else
            {
                toolStripStatusLabel1.Text = message;
            }
        }
        private class ComboItem
        {
            public int ID { get; set; }
            public string Text { get; set; }
            public override string ToString() => Text;
        }
        private void tabPage2_Click(object? sender, EventArgs e)
        {
            piePanel?.Invalidate();
        }
        private void PiePanel_Paint(object? sender, PaintEventArgs e)
        {
            if (piePanel == null || pieData == null || pieData.Count == 0) return;
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            string[] labels = pieData.Keys.ToArray();
            double[] values = pieData.Values.Select(v => (double)v).ToArray();
            double total = values.Sum();
            _pieFont ??= new Font(Module_HeThong.TenFontHeThong, 9, FontStyle.Bold);
            var font = _pieFont;
            if (total <= 0)
            {
                string msg = "Không có dữ liệu để vẽ.";
                var textSize = g.MeasureString(msg, font);
                g.DrawString(msg, font, Brushes.Gray, (piePanel.Width - textSize.Width) / 2, (piePanel.Height - textSize.Height) / 2);
                return;
            }
            Color[] colors = {
        Color.FromArgb(102, 204, 102),
        Color.FromArgb(255, 105, 180),
        Color.FromArgb(135, 206, 250),
        Color.FromArgb(255, 165, 0),
        Color.FromArgb(192, 192, 192)
    };
            int padding = 20;
            int size = Math.Min(piePanel.Width, piePanel.Height) - padding;
            Rectangle pieRect = new Rectangle(padding / 2, padding / 2, size, size);
            float startAngle = 0f;
            for (int i = 0; i < values.Length; i++)
            {
                float sweepAngle = (float)(values[i] / total * 360.0);
                if (sweepAngle <= 0) continue;
                float offsetX = 0, offsetY = 0;
                if (i == highlightedSlice)
                {
                    double midAngle = startAngle + sweepAngle / 2;
                    double rad = Math.PI * midAngle / 180.0;
                    offsetX = (float)(10 * Math.Cos(rad));
                    offsetY = (float)(10 * Math.Sin(rad));
                }
                Rectangle sliceRect = new Rectangle(pieRect.X + (int)offsetX, pieRect.Y + (int)offsetY, pieRect.Width, pieRect.Height);
                var shadowRect = sliceRect;
                shadowRect.Offset(3, 3);
                g.FillPie(Brushes.Gray, shadowRect, startAngle, sweepAngle);
                using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(sliceRect, Color.White, colors[i], 45f);
                g.FillPie(brush, sliceRect, startAngle, sweepAngle);
                g.DrawPie(Pens.White, sliceRect, startAngle, sweepAngle);
                double mid = startAngle + sweepAngle / 2;
                double radMid = Math.PI * mid / 180.0;
                float centerX = sliceRect.Left + sliceRect.Width / 2f;
                float centerY = sliceRect.Top + sliceRect.Height / 2f;
                float labelRadius = size / 3f;
                float labelX = centerX + (float)(labelRadius * Math.Cos(radMid));
                float labelY = centerY + (float)(labelRadius * Math.Sin(radMid));
                string text = $"{labels[i]}: {values[i]} ({values[i] / total:P0})";
                Brush textBrush = values[i] / total > 0.2 ? Brushes.White : Brushes.Black;
                var textSize = g.MeasureString(text, font);
                g.DrawString(text, font, textBrush, labelX - textSize.Width / 2, labelY - textSize.Height / 2);
                startAngle += sweepAngle;
            }
        }
        public async Task RefreshPieChartAsync()
        {
            string csdl2Path = _csdl2Path;
            if (string.IsNullOrWhiteSpace(csdl2Path))
                return;
            pieData = await Task.Run(() =>
            {
                int l1 = 0, l2 = 0, l3 = 0, l4 = 0, kpl = 0;
                try
                {
                    using (var conn = new SqliteConnection($"Data Source={csdl2Path}"))
                    {
                        conn.Open();
                        using (var cmd = new SqliteCommand("SELECT PhanLoai FROM DanhSach", conn))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string phanLoai = SafeDecrypt(reader["PhanLoai"]);
                                switch (phanLoai)
                                {
                                    case Module_HeThong.Loai_1: l1++; break;
                                    case Module_HeThong.Loai_2: l2++; break;
                                    case Module_HeThong.Loai_3: l3++; break;
                                    case Module_HeThong.Loai_4: l4++; break;
                                    case Module_HeThong.PL_KHONG_PL: kpl++; break;
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
                return new Dictionary<string, int>
        {
            {Module_HeThong.Loai_1, l1 },
            {Module_HeThong.Loai_2, l2 },
            {Module_HeThong.Loai_3, l3 },
            {Module_HeThong.Loai_4, l4 },
            {Module_HeThong.PL_KHONG_PL, kpl }
        };
            });
            if (piePanel != null && !piePanel.IsDisposed)
                piePanel.Invalidate();
        }
        private void KhoiTaoPieChart()
        {
            if (piePanel != null) return;
            piePanel = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            piePanel.Paint += PiePanel_Paint;
            pieChart.Controls.Clear();
            pieChart.Controls.Add(piePanel);
            piePanel.Invalidate();   // pieData có thể đã có sẵn từ Bang1_Async
        }
        //Thuật toán khác
        private void kryptonButton_MayTinh_Click(object sender, EventArgs e)
        {
            if (_dangMoMayTinh) return;          // đang mở -> bỏ qua click
            _dangMoMayTinh = true;
            var nut = kryptonButton_MayTinh;
            _textGocNutMayTinh ??= nut.Values.Text;   // lưu text + icon gốc đúng 1 lần
            _anhGocNutMayTinh ??= nut.Values.Image;
            void KhoiPhucNut()
            {
                if (IsDisposed || !IsHandleCreated) return;
                nut.Values.Text = _textGocNutMayTinh;
                nut.Values.Image = _anhGocNutMayTinh;
                nut.Enabled = true;
                _dangMoMayTinh = false;
            }
            nut.Enabled = false;
            nut.Values.Text = "Đang mở...";
            try
            {
                if (formTinhToan == null || formTinhToan.IsDisposed)
                {
                    formTinhToan = new Form14 { Owner = this, ShowInTaskbar = false };
                    formTinhToan.FormClosed += (s, ev) => KhoiPhucNut();
                }
                if (!formTinhToan.Visible) formTinhToan.Show();
                formTinhToan.Activate();
                Module_ThongBao.ThanhCong("Gọi máy tính cơ bản");
            }
            catch (Exception ex)
            {
                KhoiPhucNut();
                MessageBox.Show($"Không thể mở máy tính.\n\nChi tiết: {ex.Message}",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        // ⭐ NÂNG CẤP END: Cho trượt nốt phần còn lại tới 100% rồi mới đóng
        //endregion
        private async void kryptonButton_Refresh_Click(object? sender, EventArgs e)
        {
            string textBanDau = kryptonButton_Refresh.Values.Text;
            Image anhBanDau = kryptonButton_Refresh.Values.Image;
            try
            {
                // 1. KHÓA GIAO DIỆN & BẮT ĐẦU PROGRESS BAR
                kryptonButton_Refresh.Enabled = false;
                kryptonButton_Refresh.Values.Text = "Đang xử lý...";
                kryptonButton_Refresh.Values.Image = null;
                Progress_Start();
                await Task.Delay(100); // Nhường nhịp cho UI render mượt
                // 2. RESET DỮ LIỆU RAM
                await Progress_StepAsync(5);
                Module_TaiKhoan.TenTaiKhoan_RAM = string.Empty;
                Module_TaiKhoan.MatKhau_RAM = string.Empty;
                // 3. RESET ĐƯỜNG DẪN CSDL RAM
                await Progress_StepAsync(5);
                string[] propNames = { "DuongDanCSDL1", "DuongDanCSDL2", "DuongDanCSDL3", "DuongDanCSDL4", "DuongDanCSDL4ex" };
                foreach (string p in propNames)
                {
                    try
                    {
                        var prop = typeof(Module_DanduongGPS).GetProperty(p);
                        if (prop != null && prop.CanWrite) prop.SetValue(null, string.Empty);
                    }
                    catch { }
                }
                await Progress_StepAsync(5);
                // ⭐ 4. TẠO CSDL TRÊN LUỒNG NGẦM (GIẢI PHÓNG UI THREAD)
                string databaseFolder = Path.Combine(AppContext.BaseDirectory, "Database");
                Directory.CreateDirectory(databaseFolder);
                string[] csdlFiles = { "csdl1.db", "csdl2.db", "csdl3.db", "csdl4.db", "csdlex.xlsx" };
                // Ném toàn bộ tác vụ ổ cứng vào Background Thread để Progress Bar không bị giật
                await Task.Run(() =>
                {
                    foreach (string file in csdlFiles)
                    {
                        try
                        {
                            string duongDan = Path.Combine(databaseFolder, file);
                            if (File.Exists(duongDan)) continue;
                            if (file.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                            {
                                var builder = new SqliteConnectionStringBuilder
                                {
                                    DataSource = duongDan,
                                    Mode = SqliteOpenMode.ReadWriteCreate,
                                    Pooling = true
                                };
                                using (var conn = new SqliteConnection(builder.ToString()))
                                {
                                    conn.Open();
                                    using (var cmd = conn.CreateCommand())
                                    {
                                        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS DanhSach (ID INTEGER PRIMARY KEY AUTOINCREMENT);";
                                        cmd.CommandTimeout = 15;
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            else if (file.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                            {
                                using (var wb = new ClosedXML.Excel.XLWorkbook())
                                {
                                    wb.AddWorksheet("Sheet1");
                                    wb.SaveAs(duongDan);
                                }
                            }
                        }
                        catch (Exception exFile)
                        {
                            Debug.WriteLine($"Lỗi tạo file [{file}]: {exFile.Message}");
                        }
                    }
                });
                await Progress_StepAsync(15);
                // 5. CẬP NHẬT LẠI ĐƯỜNG DẪN HỆ THỐNG
                string[] fileNames = { "csdl1.db", "csdl2.db", "csdl3.db", "csdl4.db", "csdlex.xlsx" };
                for (int i = 0; i < propNames.Length; i++)
                {
                    try
                    {
                        var prop = typeof(Module_DanduongGPS).GetProperty(propNames[i]);
                        if (prop != null && prop.CanWrite)
                        {
                            string fullPath = Path.Combine(Module_DanduongGPS.ThuMucCoSoDuLieu, fileNames[i]);
                            prop.SetValue(null, fullPath);
                        }
                    }
                    catch { }
                }
                await Progress_StepAsync(10);
                // 6. RESET CONTROL FORM
                foreach (Control ctrl in this.Controls)
                {
                    try
                    {
                        if (ctrl is System.Windows.Forms.ComboBox cb) cb.SelectedIndex = -1;
                        else if (ctrl is System.Windows.Forms.TextBox tb) tb.Clear();
                        else if (ctrl is System.Windows.Forms.CheckBox ck) ck.Checked = false;
                    }
                    catch { }
                }
                // 7. HOÀN TẤT & ĐÓNG GIAO DIỆN CHỜ
                await Progress_EndAsync();
                ThongBao("Phần mềm đã được làm mới thành công!");
                Module_ThongBao.ThanhCong($"Đã làm mới trang chủ!");
                await Task.Delay(200);
                try { Module_ThongBao.CapNhatThongTin(); } catch { }
            }
            catch (Exception ex)
            {
                Module_ThongBao.Loi("Lỗi khi làm mới phần mềm:\n" + ex.Message);
                Invoke(new Action(() => toolStripProgressBar1.Visible = false));
            }
            finally
            {
                // Phục hồi giao diện nút bấm
                kryptonButton_Refresh.Values.Text = textBanDau;
                kryptonButton_Refresh.Values.Image = anhBanDau;
                kryptonButton_Refresh.Enabled = true;
                // Gọi 1 lần duy nhất ở đây là đủ để cập nhật thanh Status
                Module_TrangThaiHeThong.CapNhatStatusCSDL(statusStrip1, toolStripStatusLabel1);
            }
        }
        private async Task TaoBangCheDoXetThiDuaNamNeuChuaCoAsync(SqliteConnection conn, SqliteTransaction tran)
        {
            // BƯỚC 1: Kiểm tra xem bảng đã tồn tại trong SQLite master chưa
            string sqlCheck = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='CheDo_XetThiDuaNam';";
            using (var cmdCheck = new SqliteCommand(sqlCheck, conn, tran))
            {
                var result = await cmdCheck.ExecuteScalarAsync();
                long count = (result != null && result != DBNull.Value) ? Convert.ToInt64(result) : 0;
                // Nếu bảng ĐÃ TỒN TẠI -> Thoát ngay lập tức, không thực hiện lệnh tạo bảng nữa
                if (count > 0)
                {
                    return;
                }
            }
            // BƯỚC 2: Nếu BẢNG CHƯA TỒN TẠI -> Mới chạy lệnh tạo bảng
            string sqlCreate = @"
        CREATE TABLE IF NOT EXISTS ""CheDo_XetThiDuaNam"" (
            ""ID"" INTEGER NOT NULL,
            ""ChoPhepCheDoXetThiDuaNam"" TEXT,
            PRIMARY KEY(""ID"")
        );";
            using (var cmdCreate = new SqliteCommand(sqlCreate, conn, tran))
            {
                await cmdCreate.ExecuteNonQueryAsync();
            }
        }
        private async Task LoadThongTinCheDoXetThiDuaNamAsync()
        {
            string csdlPath = _csdl2Path;
            try
            {
                using (var conn = new SqliteConnection($"Data Source={csdlPath}"))
                {
                    await conn.OpenAsync();
                    // 1. Kiểm tra xem bảng CheDo_XetThiDuaNam đã tồn tại hay chưa
                    string sqlCheckTable = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='CheDo_XetThiDuaNam';";
                    bool coBang = false;
                    using (var cmdCheck = new SqliteCommand(sqlCheckTable, conn))
                    {
                        var result = await cmdCheck.ExecuteScalarAsync();
                        long count = (result != null && result != DBNull.Value) ? Convert.ToInt64(result) : 0;
                        coBang = (count > 0);
                    }
                    // 2. NẾU BẢNG CHƯA TỒN TẠI -> TẠO BẢNG, CHỌN "Tháng" VÀ THOÁT NGAY
                    if (!coBang)
                    {
                        // Mặc định chọn "Tháng" cho comboBox1_CheDoXetThiDua
                        comboBox1_CheDoXetThiDua.Text = "Tháng";
                        // Thoát ngay lập tức
                        return;
                    }
                    // 3. NẾU BẢNG ĐÃ TỒN TẠI -> ĐỌC DỮ LIỆU VÀ NẠP LÊN FORM
                    string sqlRead = "SELECT ChoPhepCheDoXetThiDuaNam FROM CheDo_XetThiDuaNam WHERE ID = 1;";
                    using (var cmdRead = new SqliteCommand(sqlRead, conn))
                    {
                        var result = await cmdRead.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value && !string.IsNullOrWhiteSpace(result.ToString()))
                        {
                            // Nạp giá trị đã lưu trong CSDL
                            comboBox1_CheDoXetThiDua.Text = result.ToString();
                        }
                        else
                        {
                            // Trường hợp bảng đã có nhưng chưa có dòng dữ liệu -> Mặc định chọn "Tháng"
                            comboBox1_CheDoXetThiDua.Text = "Tháng";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Module_ThongBao.Loi("Lỗi khi tải thông tin chế độ xét thi đua:\n" + ex.Message);
            }
        }
        private bool KiemTraVaToMauDuongDan(Label lbl)
        {
            if (string.IsNullOrWhiteSpace(lbl.Text) || lbl.Text == "Chọn đường dẫn lưu")
            {
                lbl.ForeColor = Color.Red; // tô màu đỏ nếu chưa chọn
                return false;
            }
            lbl.ForeColor = Color.FromArgb(85, 107, 47); // xanh rêu nếu đã chọn
            return true;
        }
        private async void kryptonButton_XuatTrinhKy_Click(object? sender, EventArgs e)
        {
            string textBanDau = kryptonButton_XuatTrinhKy.Values.Text;
            Image anhBanDau = kryptonButton_XuatTrinhKy.Values.Image;
            // THÊM: Gọi Form_Loading
            Form_Loading frmLoad = new Form_Loading("Đang tạo danh sách trình ký, vui lòng đợi...");
            try
            {
                kryptonButton_XuatTrinhKy.Enabled = false;
                kryptonButton_XuatTrinhKy.Values.Text = "Đang xử lý...";
                kryptonButton_XuatTrinhKy.Values.Image = null;
                await Task.Delay(100);
                // BẮT ĐẦU CODE GỐC (Kiểm tra điều kiện trên luồng chính)
                string duongDanLuu = label11.Text?.Trim();
                if (string.IsNullOrWhiteSpace(duongDanLuu) || duongDanLuu.Equals("Chọn đường dẫn lưu", StringComparison.OrdinalIgnoreCase))
                {
                    label11.ForeColor = Color.Red;
                    Module_ThongBao.DangXuLy("Chưa chọn đường dẫn xuất tệp!");
                    return;
                }
                if (duongDanLuu.Contains(": "))
                {
                    int pos = duongDanLuu.IndexOf(": ");
                    duongDanLuu = duongDanLuu.Substring(pos + 2).Trim();
                }
                if (string.IsNullOrWhiteSpace(duongDanLuu))
                {
                    label11.ForeColor = Color.Red;
                    Module_ThongBao.DangXuLy("Chưa chọn thư mục lưu!");
                    return;
                }
                if (!Directory.Exists(duongDanLuu))
                {
                    label11.ForeColor = Color.DarkOrange;
                    try
                    {
                        Directory.CreateDirectory(duongDanLuu);
                    }
                    catch
                    {
                        label11.ForeColor = Color.Red;
                        Module_ThongBao.Loi("Không thể tạo thư mục lưu!");
                        return;
                    }
                }
                label11.ForeColor = Color.DarkGreen;
                bool laTanBinh = false;
                try
                {
                    string phienBan = Module_TaiKhoan.LayPhienBanPhanMem();
                    if (!string.IsNullOrWhiteSpace(phienBan))
                    {
                        laTanBinh = phienBan.IndexOf("tân binh", StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                }
                catch
                {
                    laTanBinh = false;
                }
                bool xoaXinYKien = false;
                if (laTanBinh)
                {
                    xoaXinYKien = MessageBox.Show(
                        "Bạn có muốn xóa nội dung cột \"Xin ý kiến\" trước khi xuất không?",
                        "Xác nhận",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    ) == DialogResult.Yes;
                }
                // THÊM: Hiện loading và đẩy code xử lý nặng xuống Task.Run
                this.Enabled = false;
                frmLoad.Show(this);
                await Task.Run(() =>
                {
                    if (laTanBinh)
                    {
                        Module_XuatPhanLoai.XuatTrinhKyTanBinh(duongDanLuu, xoaXinYKien);
                    }
                    else
                    {
                        Module_XuatPhanLoai.XuatTrinhKyCBCS(duongDanLuu);
                    }
                });
                // Chạy tiếp phần code gốc sau khi xuất xong
                Module_NhatKy.GhiNhatKy(
                    taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                    hanhDong: laTanBinh
                        ? "Xuất danh sách trình ký Tân binh thành công!"
                        : "Xuất danh sách trình ký CBCS thành công!",
                    ghiChu: $"Thời gian: {SessionInfo.ThoiGianDangNhap:dd-MM-yyyy HH:mm:ss}"
                );
                // KẾT THÚC CODE GỐC
            }
            catch (Exception ex)
            {
                label11.ForeColor = Color.Red;
                Module_ThongBao.Loi("Lỗi khi xuất trình ký:\n" + ex.Message);
            }
            finally
            {
                // THÊM: Tắt loading
                frmLoad.Close();
                this.Enabled = true;
                this.Focus();
                kryptonButton_XuatTrinhKy.Values.Text = textBanDau;
                kryptonButton_XuatTrinhKy.Values.Image = anhBanDau;
                kryptonButton_XuatTrinhKy.Enabled = true;
            }
        }
        private async void kryptonButton_XuatTatCa_Click(object? sender, EventArgs e)
        {
            string textBanDau = kryptonButton_XuatTatCa.Values.Text;
            Image anhBanDau = kryptonButton_XuatTatCa.Values.Image;
            // THÊM: Gọi Form_Loading
            Form_Loading frmLoad = new Form_Loading("Đang xuất toàn bộ dữ liệu Excel...");
            try
            {
                kryptonButton_XuatTatCa.Enabled = false;
                kryptonButton_XuatTatCa.Values.Text = "Đang xử lý...";
                kryptonButton_XuatTatCa.Values.Image = null;
                await Task.Delay(100);
                // BẮT ĐẦU CODE GỐC                
                if (string.IsNullOrWhiteSpace(label11.Text) || label11.Text == "Chọn đường dẫn lưu")
                {
                    Module_ThongBao.DangXuLy("Bạn chưa chọn thư mục lưu!");
                    return;
                }
                string duongDanGoc = (label11.Text == "Chọn đường dẫn lưu") ? "" : label11.Text;
                if (string.IsNullOrWhiteSpace(duongDanGoc) || !Directory.Exists(duongDanGoc))
                {
                    Module_ThongBao.Loi("Đường dẫn lưu trong cài đặt không hợp lệ!");
                    return;
                }
                // THÊM: Bật form loading, chạy ngầm phần tạo file
                this.Enabled = false;
                frmLoad.Show(this);
                await Task.Run(() =>
                {
                    //string thangHT = Module_XuatPhanLoai.LayThangHeThong();
                    //string tenThuMuc = $"DANH SÁCH PHÂN LOẠI THI ĐUA THÁNG {thangHT} NĂM {DateTime.Now:yyyy}";
                    bool laXetNam = Module_HeThong.IsCheDoXetThiDuaNam();
                    string thangHT = Module_XuatPhanLoai.LayThangHeThong();
                    string tenThuMuc = laXetNam
                        ? $"DANH SÁCH PHÂN LOẠI THI ĐUA NĂM {DateTime.Now:yyyy}"
                        : $"DANH SÁCH PHÂN LOẠI THI ĐUA THÁNG {thangHT} NĂM {DateTime.Now:yyyy}";
                    string fullThuMuc = Path.Combine(duongDanGoc, tenThuMuc);
                    Directory.CreateDirectory(fullThuMuc);
                    // ⭐ GỌI GÁN ICON CHO THƯ MỤC THÁNG VỪA SINH RA
                    Module_HeThong.GanIconThuMuc(fullThuMuc);
                    int sttFile = Directory.GetFiles(fullThuMuc, "*.xlsx").Length + 1;
                    string fileName = $"{sttFile}. DANH SÁCH TẤT CẢ PHÂN LOẠI - {DateTime.Now:yyyyMMdd-HHmmss}.xlsx";
                    string fileXuat = Path.Combine(fullThuMuc, fileName);
                    Module_XuatPhanLoai.XuatTatCaPhanLoai(fileXuat);
                    Module_XuatPhanLoai.LinkDanTep = fileXuat;
                    //vị trí cập nhật chế độ xét thi đua tháng hoặc năm
                    Module_XuatTongHop.XuatBaoCaoTongHop(fileXuat);
                    try
                    {
                        string csdlPath = _csdl2Path;
                        bool moThuMuc = false;
                        using (var conn = new SqliteConnection($"Data Source={csdlPath}"))
                        {
                            conn.Open();
                            using var cmd = new SqliteCommand("SELECT Chex_MoiThuMucXuat FROM ThongTin WHERE ID = 1", conn);
                            object result = cmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                string giaiMa = Module_BaoMatAES.GiaiMa(result.ToString()).ToUpper();
                                moThuMuc = giaiMa == "TRUE";
                            }
                        }
                        if (moThuMuc && File.Exists(fileXuat))
                        {
                            this.Invoke(new Action(() => Module_XuatPhanLoai.MoThuMucVaChonTep(fileXuat)));
                        }
                    }
                    catch (Exception ex)
                    {
                        // Gọi Invoke nếu cần bắn thông báo lên Form từ Task.Run
                        this.Invoke(new Action(() => Module_ThongBao.Loi("Lỗi khi kiểm tra thư mục mở tự động:\n" + ex.Message)));
                    }
                    Module_NhatKy.GhiNhatKy(
                        taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                        hanhDong: "Xuất danh sách tất cả phân loại thành công!",
                        ghiChu: $"Thời gian: {SessionInfo.ThoiGianDangNhap:dd-MM-yyyy HH:mm:ss}"
                    );
                });
                // KẾT THÚC CODE GỐC
            }
            catch (Exception ex)
            {
                Module_ThongBao.Loi("Lỗi xuất dữ liệu tất cả phân loại:\n" + ex.Message);
            }
            finally
            {
                // THÊM: Đóng form loading
                frmLoad.Close();
                this.Enabled = true;
                this.Focus();
                kryptonButton_XuatTatCa.Values.Text = textBanDau;
                kryptonButton_XuatTatCa.Values.Image = anhBanDau;
                kryptonButton_XuatTatCa.Enabled = true;
            }
        }
        private async void kryptonButton_XuatDanhSachLoai_Click(object? sender, EventArgs e)
        {
            string textBanDau = kryptonButton_XuatDanhSachLoai.Values.Text;
            Image anhBanDau = kryptonButton_XuatDanhSachLoai.Values.Image;
            // THÊM: Gọi form loading
            Form_Loading frmLoad = new Form_Loading("Đang tạo danh sách theo loại...");
            try
            {
                kryptonButton_XuatDanhSachLoai.Enabled = false;
                kryptonButton_XuatDanhSachLoai.Values.Text = "Đang xử lý...";
                kryptonButton_XuatDanhSachLoai.Values.Image = null;
                await Task.Delay(100);
                // BẮT ĐẦU CODE GỐC
                if (string.IsNullOrWhiteSpace(comboBox1_ChonLoaiDeXuat.Text))
                {
                    Module_ThongBao.DangXuLy("Bạn chưa chọn phân loại!");
                    comboBox1_ChonLoaiDeXuat.BackColor = Color.LightPink;
                    return;
                }
                else
                {
                    comboBox1_ChonLoaiDeXuat.BackColor = Color.LightGreen;
                }
                if (!KiemTraVaToMauDuongDan(label11))
                {
                    Module_ThongBao.DangXuLy("Bạn chưa chọn thư mục lưu!");
                    return;
                }
                string plChon = comboBox1_ChonLoaiDeXuat.Text.Trim();
                int sttFile = 1;
                // THÊM: Bật Form loading và đưa phần xuất file vào Task.Run
                this.Enabled = false;
                frmLoad.Show(this);
                await Task.Run(() =>
                {
                    Module_XuatPhanLoai.XuatPhanLoai(plChon, sttFile);
                    try
                    {
                        string csdlPath = _csdl2Path;
                        bool moThuMuc = false;
                        using (var conn = new SqliteConnection($"Data Source={csdlPath}"))
                        {
                            conn.Open();
                            using var cmd = new SqliteCommand("SELECT Chex_MoiThuMucXuat FROM ThongTin WHERE ID = 1", conn);
                            object result = cmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                string giaiMa = Module_BaoMatAES.GiaiMa(result.ToString()).ToUpper();
                                moThuMuc = giaiMa == "TRUE";
                            }
                        }
                        if (moThuMuc && !string.IsNullOrWhiteSpace(Module_XuatPhanLoai.LinkDanTep) && System.IO.File.Exists(Module_XuatPhanLoai.LinkDanTep))
                        {
                            this.Invoke(new Action(() => Module_XuatPhanLoai.MoThuMucVaChonTep(Module_XuatPhanLoai.LinkDanTep)));
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Invoke(new Action(() => Module_ThongBao.Loi("Lỗi khi kiểm tra và mở thư mục xuất:\n" + ex.Message)));
                    }
                    Module_NhatKy.GhiNhatKy(
                        taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                        hanhDong: "Xuất danh sách " + plChon + " thành công!",
                        ghiChu: $"Thời gian: {SessionInfo.ThoiGianDangNhap:dd-MM-yyyy HH:mm:ss}"
                    );
                });
                // KẾT THÚC CODE GỐC
            }
            catch (Exception ex)
            {
                Module_ThongBao.DangXuLy("Lỗi khi xuất dữ liệu:\n" + ex.Message);
            }
            finally
            {
                // THÊM: Tắt form Loading
                frmLoad.Close();
                this.Enabled = true;
                this.Focus();
                kryptonButton_XuatDanhSachLoai.Values.Text = textBanDau;
                kryptonButton_XuatDanhSachLoai.Values.Image = anhBanDau;
                kryptonButton_XuatDanhSachLoai.Enabled = true;
            }
        }
        private void label11_Click(object? sender, EventArgs e)
        {
            try
            {
                // 1. Lấy đường dẫn từ chính label11
                string duongDan = label11.Text?.Trim();
                // 2. Kiểm tra lỏng: chưa cấu hình hoặc đang là placeholder
                if (string.IsNullOrWhiteSpace(duongDan) || duongDan == "Chọn đường dẫn lưu")
                {
                    KiemTraVaToMauDuongDan(label11);
                    MessageBox.Show("Bạn chưa cấu hình thư mục lưu tệp xuất!", "Lưu ý", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                // 3. Kiểm tra thư mục có thực sự tồn tại trên máy không
                if (!Directory.Exists(duongDan))
                {
                    MessageBox.Show("Thư mục này không tồn tại hoặc đã bị xóa/di chuyển ra khỏi máy tính!",
                                    "Không tìm thấy thư mục", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // 4. Kiểm tra xem đã có cửa sổ Explorer mở sẵn thư mục này chưa (tiết kiệm RAM)
                if (KichHoatCuaSoExplorerDaMo(duongDan))
                {
                    Module_ThongBao.ThanhCong("Đã kích hoạt cửa sổ có sẵn (tiết kiệm RAM)!");
                    return;
                }
                // 5. Không có cửa sổ nào đang mở -> mở mới bằng tiến trình chuẩn của Windows
                Process.Start(new ProcessStartInfo()
                {
                    FileName = duongDan,
                    UseShellExecute = true,
                    Verb = "open"
                });
                Module_ThongBao.ThanhCong("Mở thư mục thành công!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể mở thư mục. Lỗi hệ điều hành:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Module_ThongBao.Loi("Mở thư mục thất bại!");
            }
        }
        /// <summary>
        /// Dò trong các cửa sổ Explorer đang mở dưới nền (RAM),
        /// nếu có cửa sổ nào đang trỏ đúng thư mục -> kích hoạt nó lên.
        /// Trả về true nếu tìm thấy và đã kích hoạt thành công.
        /// </summary>
        private bool KichHoatCuaSoExplorerDaMo(string duongDanCanMo)
        {
            try
            {
                Type shellAppType = Type.GetTypeFromProgID("Shell.Application");
                if (shellAppType == null) return false;
                dynamic shellApp = Activator.CreateInstance(shellAppType);
                dynamic cacCuaSo = shellApp.Windows();
                string targetPath = duongDanCanMo.TrimEnd('\\').ToLower();
                foreach (dynamic cuaSo in cacCuaSo)
                {
                    try
                    {
                        string ten = cuaSo.Name; // "Windows Explorer" / "File Explorer"
                        if (ten != "Windows Explorer" && ten != "File Explorer")
                            continue; // bỏ qua cửa sổ trình duyệt (IE/Edge cũ dùng chung COM này)
                        string url = cuaSo.LocationURL;
                        if (string.IsNullOrEmpty(url)) continue;
                        string localPath = new Uri(url).LocalPath.TrimEnd('\\').ToLower();
                        if (localPath == targetPath)
                        {
                            IntPtr hWnd = new IntPtr((long)cuaSo.HWND);
                            if (IsIconic(hWnd))
                                ShowWindow(hWnd, SW_RESTORE); // nếu đang thu nhỏ -> phục hồi
                            SetForegroundWindow(hWnd); // đưa lên trên cùng
                            return true;
                        }
                    }
                    catch
                    {
                        // Bỏ qua từng cửa sổ lỗi (không cho 1 cửa sổ hỏng làm sập cả vòng lặp)
                        continue;
                    }
                }
            }
            catch
            {
                // Nếu COM Shell.Application lỗi (hiếm) -> coi như không tìm thấy,
                // để logic bên dưới tự mở mới, không văng lỗi ra ngoài UI
            }
            return false;
        }
        //  HẾT PHẦN THÊM MỚI 
        private void kryptonButton_MoThuMuc_Click(object? sender, EventArgs e)
        {
            try
            {
                string basePath = label11.Text?.Trim();
                if (string.IsNullOrWhiteSpace(basePath) || basePath == "Chọn đường dẫn lưu")
                {
                    KiemTraVaToMauDuongDan(label11);
                    MessageBox.Show("Bạn chưa cấu hình thư mục lưu tệp xuất!", "Lưu ý", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                //ƯU TIÊN 1: MỞ + CHỌN ĐÚNG TỆP VỪA XUẤT GẦN NHẤT ==
                // Nếu người dùng vừa bấm "Xuất tất cả" hoặc "Xuất danh sách loại" trước đó,
                // Module_XuatPhanLoai.LinkDanTep đã lưu sẵn đường dẫn tệp chính xác.
                string tepGanNhat = Module_XuatPhanLoai.LinkDanTep;
                if (!string.IsNullOrWhiteSpace(tepGanNhat) && File.Exists(tepGanNhat))
                {
                    string thuMucChuaTep = Path.GetDirectoryName(tepGanNhat);
                    // Vẫn kiểm tra tiết kiệm RAM: nếu thư mục chứa tệp đó đã mở sẵn -> chỉ kích hoạt
                    if (KichHoatCuaSoExplorerDaMo(thuMucChuaTep))
                    {
                        Module_ThongBao.ThanhCong("Đã kích hoạt cửa sổ có sẵn (tiết kiệm RAM)!");
                        return;
                    }
                    // Chưa mở -> mở mới và tự động chọn (bôi đen) đúng tệp vừa xuất
                    Module_XuatPhanLoai.MoThuMucVaChonTep(tepGanNhat);
                    Module_ThongBao.ThanhCong("Mở thư mục thành công!");
                    return;
                }
                //ƯU TIÊN 2: DỰ ĐOÁN THƯ MỤC (Nếu chưa từng xuất trong phiên này) ==
                bool laXetNam = Module_HeThong.IsCheDoXetThiDuaNam();
                string thangHT = Module_XuatPhanLoai.LayThangHeThong();
                string folderName = laXetNam
                    ? $"DANH SÁCH PHÂN LOẠI THI ĐUA NĂM {DateTime.Now:yyyy}"
                    : $"DANH SÁCH PHÂN LOẠI THI ĐUA THÁNG {thangHT} NĂM {DateTime.Now:yyyy}";
                string fullPath = Path.Combine(basePath, folderName);
                string thuMucCanMo;
                if (Directory.Exists(fullPath))
                    thuMucCanMo = fullPath;
                else if (Directory.Exists(basePath))
                    thuMucCanMo = basePath;
                else
                {
                    MessageBox.Show("Thư mục này không tồn tại hoặc đã bị xóa/di chuyển ra khỏi máy tính!",
                                    "Không tìm thấy thư mục", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // Kiểm tra tiết kiệm RAM trước khi tạo tiến trình mới
                if (KichHoatCuaSoExplorerDaMo(thuMucCanMo))
                {
                    Module_ThongBao.ThanhCong("Đã kích hoạt cửa sổ có sẵn (tiết kiệm RAM)!");
                    return;
                }
                Process.Start(new ProcessStartInfo()
                {
                    FileName = thuMucCanMo,
                    UseShellExecute = true,
                    Verb = "open"
                });
                Module_ThongBao.ThanhCong("Mở thư mục thành công!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể mở thư mục. Lỗi hệ điều hành:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Module_ThongBao.Loi("Mở thư mục thất bại!");
            }
        }
        private string GiaiMaAnToan(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;
            try
            {
                string giaiMa = Module_BaoMatAES.GiaiMa(raw);
                // 🔥 Fail-safe: nếu giải mã lỗi trả về rỗng → fallback về raw
                return string.IsNullOrWhiteSpace(giaiMa) ? raw.Trim() : giaiMa;
            }
            catch
            {
                // 🔥 Không bao giờ để crash UI
                return raw.Trim();
            }
        }
        private void AnIDGrid(DataGridView dgv)
        {
            foreach (DataGridViewColumn col in dgv.Columns)
                if (string.Equals(col.Name, "ID", StringComparison.OrdinalIgnoreCase))
                    col.Visible = false;
        }
        private async Task KiemTraVaDongBoCSDLAsync()
        {
            var csdls = new[]
            {
        ("CSDL1", Module_DanduongGPS.DuongDanCSDL1),
        ("CSDL2", Module_DanduongGPS.DuongDanCSDL2),
        ("CSDL3", Module_DanduongGPS.DuongDanCSDL3),
        ("CSDL4", Module_DanduongGPS.DuongDanCSDL4)
    };
            // Chuẩn hóa câu lệnh SQL, khai báo 1 lần duy nhất
            string sqlCreate = @"CREATE TABLE IF NOT EXISTS Token_XacDinhChinhChu (ID INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, STT TEXT, Ma_ToKen TEXT, Tai_Khoan_Cap_Nhat TEXT, Thoi_Gian_Nap TEXT);";
            // Sử dụng Tuple để gom nhóm nguyên 1 bản ghi, tránh râu ông nọ cắm cằm bà kia
            var thongTinCSDL = new Dictionary<string, (string Token, string TaiKhoan, string ThoiGian)>();
            var csdlTonTai = new List<string>();
            // BƯỚC 1: ĐỌC DỮ LIỆU (Chạy trên luồng nền để chống đơ UI)
            await Task.Run(() =>
            {
                foreach (var csdl in csdls)
                {
                    if (string.IsNullOrWhiteSpace(csdl.Item2) || !File.Exists(csdl.Item2)) continue;
                    try
                    {
                        using var conn = new SqliteConnection($"Data Source={csdl.Item2}");
                        conn.Open();
                        using var cmdCreate = new SqliteCommand(sqlCreate, conn);
                        cmdCreate.ExecuteNonQuery();
                        string sqlGet = "SELECT Ma_ToKen, Tai_Khoan_Cap_Nhat, Thoi_Gian_Nap FROM Token_XacDinhChinhChu WHERE ID=1";
                        using var cmdGet = new SqliteCommand(sqlGet, conn);
                        using var rd = cmdGet.ExecuteReader();
                        if (rd.Read())
                        {
                            string token = rd.IsDBNull(0) ? "" : Module_BaoMatAES.GiaiMa(rd.GetString(0));
                            string tk = rd.IsDBNull(1) ? "" : Module_BaoMatAES.GiaiMa(rd.GetString(1));
                            string tg = rd.IsDBNull(2) ? "" : rd.GetString(2);
                            // Lưu nguyên cụm 3 giá trị
                            thongTinCSDL[csdl.Item1] = (token, tk, tg);
                        }
                        csdlTonTai.Add(csdl.Item1);
                        // Từ khóa 'using' sẽ tự động đóng connection, không cần gọi conn.Close()
                    }
                    catch (Exception ex)
                    {
                        // Bắt buộc ghi log khi gặp sự cố đọc (VD: Database is locked)
                        Debug.WriteLine($"[Cảnh báo] Lỗi đọc {csdl.Item1}: {ex.Message}");
                    }
                }
            });
            if (csdlTonTai.Count == 0 || thongTinCSDL.Count == 0) return;
            // BƯỚC 2: XÁC ĐỊNH BẢN GHI CHUẨN
            var banGhiChuan = thongTinCSDL.Values
                .GroupBy(v => v) // Nhóm nguyên cụm bản ghi
                .OrderByDescending(g => g.Count()) // Ưu tiên số đông
                .ThenByDescending(g => g.Key.ThoiGian) // Tỷ số hòa -> Ưu tiên mốc thời gian mới nhất
                .FirstOrDefault()?.Key;
            if (banGhiChuan == null) return;
            // Giải nén tuple chuẩn
            var (chuanToken, chuanTK, chuanTG) = banGhiChuan.Value;
            var csdlDaBoSung = new HashSet<string>();
            // BƯỚC 3: CẬP NHẬT GHI ĐÈ (Chạy nền)
            await Task.Run(() =>
            {
                foreach (var kv in csdls)
                {
                    if (!csdlTonTai.Contains(kv.Item1)) continue;
                    // Kiểm tra tính toàn vẹn: Chỉ cập nhật nếu thiếu hoặc sai lệch với bản chuẩn
                    bool canUpdate = !thongTinCSDL.ContainsKey(kv.Item1) ||
                                     thongTinCSDL[kv.Item1].Token != chuanToken ||
                                     thongTinCSDL[kv.Item1].TaiKhoan != chuanTK ||
                                     thongTinCSDL[kv.Item1].ThoiGian != chuanTG;
                    if (canUpdate)
                    {
                        try
                        {
                            using var conn = new SqliteConnection($"Data Source={kv.Item2}");
                            conn.Open();
                            string sqlUpsert = @"INSERT OR REPLACE INTO Token_XacDinhChinhChu 
                                         (ID, STT, Ma_ToKen, Tai_Khoan_Cap_Nhat, Thoi_Gian_Nap)
                                         VALUES (1, 'STT1', @token, @tk, @tg)";
                            using var cmdUpsert = new SqliteCommand(sqlUpsert, conn);
                            cmdUpsert.Parameters.AddWithValue("@token", string.IsNullOrEmpty(chuanToken) ? "" : Module_BaoMatAES.MaHoa(chuanToken));
                            cmdUpsert.Parameters.AddWithValue("@tk", string.IsNullOrEmpty(chuanTK) ? "" : Module_BaoMatAES.MaHoa(chuanTK));
                            cmdUpsert.Parameters.AddWithValue("@tg", chuanTG);
                            cmdUpsert.ExecuteNonQuery();
                            csdlDaBoSung.Add(kv.Item1);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[Cảnh báo] Lỗi ghi đè {kv.Item1}: {ex.Message}");
                        }
                    }
                }
            });
            // BƯỚC 4: THÔNG BÁO VÀ GHI NHẬT KÝ
            if (csdlDaBoSung.Count > 0)
            {
                string thongTinFiles = "";
                foreach (var kv in csdls)
                {
                    if (!csdlDaBoSung.Contains(kv.Item1)) continue;
                    try
                    {
                        var fi = new FileInfo(kv.Item2);
                        thongTinFiles += $"CSDL: {kv.Item1}\n" +
                                         $"Kích thước: {fi.Length:N0} bytes\n" +
                                         $"Ngày tạo: {fi.CreationTime:dd/MM/yyyy HH:mm:ss}\n" +
                                         $"Sửa gần nhất: {fi.LastWriteTime:dd/MM/yyyy HH:mm:ss}\n\n";
                    }
                    // CODE CHUẨN
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Cảnh báo] Lỗi tại: {ex.Message}");
                        // Bắt buộc tích hợp hàm GhiNhatKy() hoặc báo lỗi ra UI
                    }
                }
                string danhSachLoi = string.Join(", ", csdlDaBoSung);
                string msg = $"Phát hiện CSDL bị thiếu hoặc sai lệch dữ liệu định danh ({danhSachLoi})!\n" +
                             "Hệ thống đã tự động khôi phục sự đồng nhất từ dữ liệu chuẩn.\n\n" +
                             "Thông tin chi tiết CSDL được khôi phục:\n" + thongTinFiles;
                // Tối ưu hóa nội dung ghi nhật ký, không nên lưu chuỗi quá dài gây phình Database
                Module_NhatKy.GhiNhatKy(
                    taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                    hanhDong: $"Đồng bộ Token tự động ({danhSachLoi})",
                    ghiChu: $"Khôi phục về bản ghi của: {chuanTK} lúc {chuanTG}"
                );
                // Icon Information hợp lý hơn Warning vì phần mềm đã TỰ ĐỘNG KHÔI PHỤC thành công
                MessageBox.Show(msg, "Đồng bộ Cơ sở dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void kryptonButton_ChonDuongDanLuu_Click(object? sender, EventArgs e)
        {
            try
            {
                string csdl2Path = _csdl2Path;
                if (string.IsNullOrWhiteSpace(csdl2Path) || !File.Exists(csdl2Path))
                {
                    Module_ThongBao.DangXuLy("CSDL phụ không tồn tại.");
                    return;
                }
                string duongDanCu = Module_XuatPhanLoai.GetLinkLuuDuongDanTepXuat(true);
                using var fbd = new FolderBrowserDialog
                {
                    Description = "Chọn thư mục để lưu file Excel",
                    ShowNewFolderButton = true,
                    SelectedPath = !string.IsNullOrWhiteSpace(duongDanCu) && Directory.Exists(duongDanCu)
                        ? duongDanCu
                        : Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };
                if (fbd.ShowDialog() != DialogResult.OK)
                    return;
                string duongDanMoi = fbd.SelectedPath;
                if (!Directory.Exists(duongDanMoi))
                {
                    Module_ThongBao.DangXuLy("Thư mục không hợp lệ.");
                    return;
                }
                // KHÔNG GÁN ICON Ở ĐÂY để giữ nguyên thư mục gốc của người dùng
                using var conn = new SqliteConnection($"Data Source={csdl2Path}");
                conn.Open();
                using var tran = conn.BeginTransaction();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText =
                        @"INSERT INTO ThongTin (ID, ChonDuongDanXuatTep)
                  VALUES (1, @path)
                  ON CONFLICT(ID)
                  DO UPDATE SET ChonDuongDanXuatTep = excluded.ChonDuongDanXuatTep;";
                    cmd.Parameters.AddWithValue("@path", Module_BaoMatAES.MaHoa(duongDanMoi));
                    cmd.ExecuteNonQuery();
                }
                tran.Commit();
                // Update UI
                label11.Text = duongDanMoi;
                label11.ForeColor = Color.FromArgb(85, 107, 47);
                Module_ThongBao.ThanhCong("Đã lưu thư mục thành công.");
            }
            catch (Exception ex)
            {
                Module_ThongBao.DangXuLy("Lỗi: " + ex.Message);
            }
        }
        private void HienThiDuongDanXuatDaChon()
        {
            string duongDanGoc = string.Empty;
            try
            {
                using (var conn = new SqliteConnection($"Data Source={_csdl2Path}"))
                {
                    {
                        conn.Open();
                        string sql = @"
                SELECT ChonDuongDanXuatTep
                FROM ThongTin
                WHERE ID = 1";
                        using (var cmd = new SqliteCommand(sql, conn))
                        {
                            object? result = cmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                string chuoiMaHoa = result.ToString()!;
                                duongDanGoc = Module_BaoMatAES.GiaiMa(chuoiMaHoa); // 🔑 GIẢI MÃ
                            }
                        }
                    }
                }
            }
            catch
            {
                duongDanGoc = string.Empty;
            }
            // == CẬP NHẬT LABEL ==
            if (!string.IsNullOrWhiteSpace(duongDanGoc) &&
                Directory.Exists(duongDanGoc))
            {
                label11.Text = duongDanGoc;
                label11.ForeColor = Color.DarkGreen;
            }
            else
            {
                label11.Text = "Chưa chọn đường dẫn xuất tệp";
                label11.ForeColor = Color.Gray;
            }
        }

 
        private void KiemTraDuLieuDanhSachVaKhoaNut(bool coDuLieu)
        {
            kryptonButton_XuatTrinhKy.Enabled = coDuLieu;
            kryptonButton_XuatTatCa.Enabled = coDuLieu;
            kryptonButton_XuatDanhSachLoai.Enabled = coDuLieu;
            Color mau = coDuLieu ? Color.Black : Color.Gray;
            kryptonButton_XuatTrinhKy.StateCommon.Content.ShortText.Color1 = mau;
            kryptonButton_XuatTatCa.StateCommon.Content.ShortText.Color1 = mau;
            kryptonButton_XuatDanhSachLoai.StateCommon.Content.ShortText.Color1 = mau;
            if (!coDuLieu)
                ThongBao("Chưa có dữ liệu danh sách. Các tính năng xuất tệp bị vô hiệu hóa.");
        }
        public void CapNhatDanhSachPhanLoaiDeXuat()
        {
            // Đảm bảo an toàn luồng (nếu Form 6 gọi hàm này từ Task ngầm)
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(CapNhatDanhSachPhanLoaiDeXuat));
                return;
            }
            // Giữ lại text user đang chọn dở để không làm họ bực mình bị mất lựa chọn
            string selectedGoc = comboBox1_ChonLoaiDeXuat.Text;
            try
            {
                comboBox1_ChonLoaiDeXuat.BeginUpdate();
                comboBox1_ChonLoaiDeXuat.Items.Clear();
                // ⭐ LẤY DANH SÁCH ĐÃ ĐƯỢC LỌC VÀ SẮP XẾP CHUẨN (Loại 1 -> Loại 4 -> Không PL) TỪ FORM 6
                List<string> danhSachDaLoc = Form6_XuLyData.LayDanhSachPhanLoaiThucTe();
                // Nạp vào ComboBox
                foreach (var item in danhSachDaLoc)
                {
                    comboBox1_ChonLoaiDeXuat.Items.Add(item);
                }
                // Phục hồi lại giá trị cũ đang chọn (Nếu giá trị đó vẫn còn tồn tại trong list mới)
                if (!string.IsNullOrWhiteSpace(selectedGoc) && comboBox1_ChonLoaiDeXuat.Items.Contains(selectedGoc))
                {
                    comboBox1_ChonLoaiDeXuat.Text = selectedGoc;
                }
                else if (comboBox1_ChonLoaiDeXuat.Items.Count > 0)
                {
                    // Nếu giá trị cũ bị xóa mất, tự động lùi về Item đầu tiên (thường là Loại 1)
                    comboBox1_ChonLoaiDeXuat.SelectedIndex = 0;
                }
                else
                {
                    comboBox1_ChonLoaiDeXuat.Text = ""; // Bỏ trống nếu DB không có ai bị xếp loại nào
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi nạp combobox Loại đề xuất từ Form 6: " + ex.Message);
            }
            finally
            {
                comboBox1_ChonLoaiDeXuat.EndUpdate();
            }
        }
        private void kryptonButton1_XuatTepPdf_Click(object? sender, EventArgs e)
        {
            if (_textGocNutXuatPdf == null)
            {
                _textGocNutXuatPdf = kryptonButton1_XuatTepPdf.Values.Text;
                _anhGocNutXuatPdf = kryptonButton1_XuatTepPdf.Values.Image;
            }
            kryptonButton1_XuatTepPdf.Enabled = false;
            kryptonButton1_XuatTepPdf.Values.Text = "Đang mở...";
            try
            {
                // Chỉ tạo mới nếu chưa có hoặc đã bị Dispose trước đó
                if (_form48 == null || _form48.IsDisposed)
                {
                    _form48 = new Form48_XuatTepPdf();
                    _form48.ShowInTaskbar = false;
                    _form48.StartPosition = FormStartPosition.CenterParent;
                }
                // 🌟 BÍ QUYẾT Ở ĐÂY: Ép Form 48 làm mới toàn bộ trước khi hiện lên
                _form48.ReloadGiaoDienVaDuLieu();
                _form48.ShowDialog(this); // vẫn modal, vẫn phải chờ đóng
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Đã xảy ra lỗi khi mở form xuất PDF: {ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                kryptonButton1_XuatTepPdf.Values.Text = _textGocNutXuatPdf;
                kryptonButton1_XuatTepPdf.Values.Image = _anhGocNutXuatPdf;
                kryptonButton1_XuatTepPdf.Enabled = true;
            }
        }
        private async Task LoadDuLieuCauHinhTongHopAsync()
        {
            if (string.IsNullOrWhiteSpace(_csdl2Path) || !File.Exists(_csdl2Path)) return;
            bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
            string tableChiHuy = laTanBinh ? "ChiHuyD_TanBinh" : "ChiHuyD";
            var chiHuyItems = new List<ComboItem>();
            _dictChiHuyD.Clear();
            // Dùng 1 kết nối duy nhất cho TẤT CẢ cấu hình
            using var conn = TaoKetNoiCSDL2(true);
            await conn.OpenAsync();
            // 1. Đọc Chỉ Huy
            using (var cmdCH = new SqliteCommand($"SELECT ID, HoVaTen, ChucVu FROM [{tableChiHuy}] WHERE HoVaTen IS NOT NULL ORDER BY ID ASC", conn))
            using (var rdCH = await cmdCH.ExecuteReaderAsync())
            {
                while (await rdCH.ReadAsync())
                {
                    int id = rdCH.GetInt32(0);
                    string hoTen = SafeDecrypt(rdCH["HoVaTen"]);
                    string chucVu = SafeDecrypt(rdCH["ChucVu"]);
                    if (string.IsNullOrWhiteSpace(hoTen)) continue;
                    chiHuyItems.Add(new ComboItem { ID = id, Text = hoTen.Trim() });
                    _dictChiHuyD[hoTen.ToLowerInvariant()] = chucVu;
                }
            }
            // 2. Đọc toàn bộ bảng ThongTin + ChonLoaiBaoCao + ThangHeThong trong 1 lệnh
            const string sqlThongTin = @"
        SELECT T.*, B.ChonLoaiBaoCao, B.ChonTuan, H.Thang AS ThangHeThong
        FROM ThongTin T 
        LEFT JOIN ChonLoaiBaoCao B ON B.ID = 1 
        LEFT JOIN ThangHeThong H ON H.ID = 1 
        WHERE T.ID = 1 LIMIT 1";
            using (var cmdTT = new SqliteCommand(sqlThongTin, conn))
            using (var rdTT = await cmdTT.ExecuteReaderAsync())
            {
                if (await rdTT.ReadAsync())
                {
                    // --- Cập nhật ComboBox Chỉ huy ---
                    comboBox_ChiHuyD.BeginUpdate();
                    comboBox_ChiHuyD.DataSource = chiHuyItems;
                    comboBox_ChiHuyD.DisplayMember = "Text";
                    comboBox_ChiHuyD.ValueMember = "ID";
                    string savedChiHuy = SafeDecrypt(rdTT["ChiHuyD"]).Trim();
                    var match = chiHuyItems.FirstOrDefault(x => string.Equals(x.Text, savedChiHuy, StringComparison.OrdinalIgnoreCase));
                    if (match != null) comboBox_ChiHuyD.SelectedValue = match.ID;
                    else if (chiHuyItems.Count > 0) comboBox_ChiHuyD.SelectedIndex = 0;
                    comboBox_ChiHuyD.EndUpdate();
                    // --- Cập nhật các trường cấu hình UI ---
                    com_DeNghi.Text = SafeDecrypt(rdTT["LoaiDeNghi"]);
                    comboBox1_ChonLoaiDeXuat.Text = SafeDecrypt(rdTT["ChonDanhSachXuat"]);
                    string chex = SafeDecrypt(rdTT["Chex_MoiThuMucXuat"]).ToUpper();
                    Check_MoThuMuc.Checked = (chex == "TRUE");
                    Check_MoThuMuc.ForeColor = Check_MoThuMuc.Checked ? Color.Green : Color.Red;
                    if (!checkBox1_TuDongChonNgayThang.Checked)
                    {
                        comboBox_Ngay.Text = SafeDecrypt(rdTT["Ngay"]);
                        comboBox_Thang.Text = SafeDecrypt(rdTT["Thang"]);
                        comboBox_Nam.Text = SafeDecrypt(rdTT["Nam"]);
                    }
                    // --- Cập nhật Địa điểm (Tránh quét CSDL lại lần 2 ở LoadDiaDiemAsync) ---
                    string diaDiemDaLuu = SafeDecrypt(rdTT["DiaDiem"]).Trim();
                    if (!string.IsNullOrWhiteSpace(diaDiemDaLuu))
                    {
                        if (!comboBox_DiaDiem.Items.Contains(diaDiemDaLuu)) comboBox_DiaDiem.Items.Add(diaDiemDaLuu);
                        comboBox_DiaDiem.Text = diaDiemDaLuu;
                    }
                    // --- Đường dẫn lưu ---
                    string duongDanLuu = SafeDecrypt(rdTT["ChonDuongDanXuatTep"]);
                    if (!string.IsNullOrWhiteSpace(duongDanLuu) && Directory.Exists(duongDanLuu))
                    {
                        label11.Text = duongDanLuu;
                        label11.ForeColor = Color.DarkGreen;
                    }
                    else
                    {
                        label11.Text = "Chưa chọn đường dẫn xuất tệp";
                        label11.ForeColor = Color.Gray;
                    }
                }
            }
        }
        private async void kryptonButton1_TomTatThanhTich_Click(object sender, EventArgs e)
        {
            // 1. LƯU TRẠNG THÁI GỐC CỦA NÚT
            if (_textGocNutTomTatThanhTich == null) _textGocNutTomTatThanhTich = kryptonButton1_TomTatThanhTich.Text;
            // 2. KIỂM TRA FORM56 ĐÃ ĐƯỢC MỞ CHƯA
            Form? frm = Application.OpenForms[nameof(Form56_TomTatThanhTichTapTheHangThang)];
            if (frm != null)
            {
                // Form đã mở → kích hoạt lại
                if (frm.WindowState == FormWindowState.Minimized) frm.WindowState = FormWindowState.Normal;
                frm.Activate();
                frm.Focus();
                return;
            }
            // 3. KHÓA NÚT + ĐỔI TÊN TẠM THỜI
            kryptonButton1_TomTatThanhTich.Enabled = false;
            kryptonButton1_TomTatThanhTich.Text = "Đang mở...";
            try
            {
                // 4. THÔNG BÁO TRẠNG THÁI NGAY LẬP TỨC
                ThongBao("Đang mở trang báo cáo tóm tắt kết quả thi đua tập thể " + "phong trào Vì ANTQ...");
                toolStripStatusLabel1?.Owner?.Invalidate();
                toolStripStatusLabel1?.Owner?.Update();
                // 5. TẠO FORM56
                using var newForm = new Form56_TomTatThanhTichTapTheHangThang { ShowInTaskbar = false, StartPosition = FormStartPosition.CenterScreen };
                // 6. MỞ MODAL
                newForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Đã xảy ra lỗi khi mở trang tóm tắt thành tích:\n\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 7. LUÔN KHÔI PHỤC NÚT
                if (!IsDisposed && !Disposing && kryptonButton1_TomTatThanhTich != null && !kryptonButton1_TomTatThanhTich.IsDisposed)
                {
                    kryptonButton1_TomTatThanhTich.Text = _textGocNutTomTatThanhTich ?? "Thành tích";
                    kryptonButton1_TomTatThanhTich.Enabled = true;
                }
                // 8. CẬP NHẬT LẠI STATUS CSDL
                Module_TrangThaiHeThong.CapNhatStatusCSDL(statusStrip1, toolStripStatusLabel1);
            }
        }
        private Font? _pieFont;
        private sealed class DoubleBufferedPanel : Panel
        {
            public DoubleBufferedPanel()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
            }
        }
        private static void BatDoubleBuffer(Control c)
        {
            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(c, true);
        }
        private volatile bool _canReloadLai = false;
        public async Task ReloadDuLieuAsync()
        {
            if (!await _reloadLock.WaitAsync(0))
            {
                _canReloadLai = true;   // đang bận -> chạy lại sau khi xong
                return;
            }
            try
            {
                do
                {
                    _canReloadLai = false;
                    await ThucHienReloadAsync();
                }
                while (_canReloadLai && !IsDisposed && !Disposing);
            }
            finally
            {
                _reloadLock.Release();
            }
        }
        private async Task ThucHienReloadAsync()
        {
            if (IsDisposed || Disposing) return;
            this.SuspendLayout();
            toolStripProgressBar1.Visible = true;
            // Gỡ các sự kiện gây nhiễu trong lúc nạp dữ liệu
            Check_MoThuMuc.CheckedChanged -= Check_MoThuMuc_CheckedChanged;
            comboBox1_ChonLoaiDeXuat.SelectedIndexChanged -= comboBox1_ChonLoaiDeXuat_SelectedIndexChanged;
            checkBox1_TuDongChonNgayThang.CheckedChanged -= checkBox1_TuDongChonNgayThang_CheckedChanged;
            com_DeNghi.SelectedIndexChanged -= Com_DeNghi_SelectedIndexChanged;
            comboBox1_ChonLoaiBaoCao.SelectedIndexChanged -= comboBox1_ChonLoaiBaoCao_SelectedIndexChanged;
            comboBox1_CheDoXetThiDua.SelectedIndexChanged -= comboBox1_CheDoXetThiDua_SelectedIndexChanged;
            try
            {
                bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem()
                    .Contains("tân binh", StringComparison.OrdinalIgnoreCase);
                CapNhatThongBaoPhanMem();
                // 1. NẠP CHẾ ĐỘ THÁNG/NĂM TRƯỚC TIÊN (CBCS): mọi bước sau đều phụ thuộc vào chế độ này
                if (!laTanBinh)
                {
                    await LoadThongTinCheDoXetThiDuaNamAsync();
                    if (IsDisposed || Disposing) return;
                    // Đồng bộ cache RAM của Module_HeThong để nhãn CSTĐ/CSTT/HTNV khớp với chế độ vừa nạp
                    Module_HeThong.LayCheDoXetThiDuaNam(lamMoiTuCSDL: true);
                }
                // 2. DANH SÁCH ĐỀ NGHỊ + CẤU HÌNH + TỶ LỆ (đã biết chế độ)
                LoadComboBoxDeNghi();
                await LoadDuLieuCauHinhTongHopAsync();
                if (IsDisposed || Disposing) return;
                SetDeNghiVaTinhTyLe(); // -> LoadQuyDinhTheoDeNghi -> đọc đúng bảng Tháng hoặc Năm
                // 3. BẢNG 1 + BẢNG 2 (dùng phanTramLoai1/2/3 vừa nạp)
                bool coDuLieu = KiemTraCoDuLieuDanhSach();
                if (coDuLieu)
                {
                    await Bang1_Async();
                    if (IsDisposed || Disposing) return;
                    _allowLoadBang2 = true;
                    await Task.Yield();
                    await Bang2_Async();
                }
                else
                {
                    pieData = new Dictionary<string, int>();
                    piePanel?.Invalidate();
                    kryptonDataGridView1.DataSource = null;
                    kryptonDataGridView2.DataSource = null;
                }
                if (IsDisposed || Disposing) return;
                // 4. CÁC TÁC VỤ CÒN LẠI
                await KiemTraVaDongBoCSDLAsync();
                Module_XuatPhanLoai.NapLinkLuuDuongDanTepXuat();
                KiemTraDuLieuDanhSachVaKhoaNut(coDuLieu);
                CauHoiGiaoDien_PhienBan(laTanBinh);
                await LoadDiaDiemAsync();
                await LoadThongTinAsync();
                await CapNhatDanhSachPhanLoaiDeXuatAsync();
                if (IsDisposed || Disposing) return;
                // 5. HIỆN/ẨN CONTROL CHẾ ĐỘ THI ĐUA (không nạp lại chế độ ở đây nữa)
                if (comboBox1_CheDoXetThiDua != null)
                    comboBox1_CheDoXetThiDua.Visible = !laTanBinh;
                if (label1_CheDoXetThiDuaTanBinh != null)
                    label1_CheDoXetThiDuaTanBinh.Visible = !laTanBinh;
                Module_TrangThaiHeThong.CapNhatStatusCSDL(statusStrip1, toolStripStatusLabel1);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ReloadDuLieu lỗi: " + ex.Message);
            }
            finally
            {
                // Gắn lại sự kiện LUÔN LUÔN chạy, kể cả khi có exception hoặc return sớm
                // (-= trước để không bao giờ bị gắn đúp)
                if (!IsDisposed)
                {
                    Check_MoThuMuc.CheckedChanged -= Check_MoThuMuc_CheckedChanged;
                    Check_MoThuMuc.CheckedChanged += Check_MoThuMuc_CheckedChanged;
                    comboBox1_ChonLoaiDeXuat.SelectedIndexChanged -= comboBox1_ChonLoaiDeXuat_SelectedIndexChanged;
                    comboBox1_ChonLoaiDeXuat.SelectedIndexChanged += comboBox1_ChonLoaiDeXuat_SelectedIndexChanged;
                    checkBox1_TuDongChonNgayThang.CheckedChanged -= checkBox1_TuDongChonNgayThang_CheckedChanged;
                    checkBox1_TuDongChonNgayThang.CheckedChanged += checkBox1_TuDongChonNgayThang_CheckedChanged;
                    com_DeNghi.SelectedIndexChanged -= Com_DeNghi_SelectedIndexChanged;
                    com_DeNghi.SelectedIndexChanged += Com_DeNghi_SelectedIndexChanged;
                    comboBox1_ChonLoaiBaoCao.SelectedIndexChanged -= comboBox1_ChonLoaiBaoCao_SelectedIndexChanged;
                    comboBox1_ChonLoaiBaoCao.SelectedIndexChanged += comboBox1_ChonLoaiBaoCao_SelectedIndexChanged;
                    comboBox1_CheDoXetThiDua.SelectedIndexChanged -= comboBox1_CheDoXetThiDua_SelectedIndexChanged;
                    comboBox1_CheDoXetThiDua.SelectedIndexChanged += comboBox1_CheDoXetThiDua_SelectedIndexChanged;
                    toolStripProgressBar1.Visible = false;
                    this.ResumeLayout(true);
                }
            }
        }
        // CBCS + chế độ "Năm" -> dùng bảng QuyDinhTyLe_XetThiDuaNam
        private bool LaCheDoNamCBCS()
        {
            bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem()
                .Contains("tân binh", StringComparison.OrdinalIgnoreCase);
            return !laTanBinh &&
                   string.Equals(comboBox1_CheDoXetThiDua.Text?.Trim(), "Năm", StringComparison.OrdinalIgnoreCase);
        }
        // Trả về { CSTĐ, CSTT, HTNV } tương ứng { phanTramLoai1, 2, 3 }; null nếu không đọc được
        private int[]? DocTyLeXetThiDuaNam()
        {
            try
            {
                using var conn = TaoKetNoiCSDL2(readOnly: true);
                conn.Open();
                using var cmd = new SqliteCommand(
                    "SELECT TyLe_CSTD, TyLe_CSTT, TyLe_HTNV FROM QuyDinhTyLe_XetThiDuaNam ORDER BY ID LIMIT 1;", conn);
                using var rd = cmd.ExecuteReader();
                if (!rd.Read()) return null;
                // SafeDecrypt: nếu cột đang mã hóa AES thì giải mã, nếu là chữ thường thì giữ nguyên
                return new[]
                {
            ParseTyLe(SafeDecrypt(rd.GetValue(0))),
            ParseTyLe(SafeDecrypt(rd.GetValue(1))),
            ParseTyLe(SafeDecrypt(rd.GetValue(2)))
        };
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[DocTyLeXetThiDuaNam] " + ex.Message);
                return null;
            }
        }
        private static int ParseTyLe(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            s = s.Replace("%", "").Trim();
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ||
                double.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out d))
                return (int)Math.Round(d);
            return 0;
        }
        private void LoadQuyDinhTheoDeNghi()
        {
            string? selectedText = com_DeNghi.SelectedItem?.ToString()?.Trim();
            string? key = selectedText switch
            {
                Module_HeThong.Loai_1 or Module_HeThong.XLDV_DVQT => "Loai1_TapThe",
                Module_HeThong.Loai_2 or Module_HeThong.XLDV_DVTT => "Loai2_TapThe",
                Module_HeThong.Loai_3 or Module_HeThong.XLDV_HTNV => "Loai3_TapThe",
                Module_HeThong.Loai_4 or Module_HeThong.XLDV_KHTNV => "Loai4_TapThe",
                "Không phân loại" or Module_HeThong.PL_KHONG_PL => "KhongPL_TapThe",
                _ => null
            };
            int[] values;
            if (key != null && LaCheDoNamCBCS() && key != "Loai4_TapThe" && key != "KhongPL_TapThe")
            {
                // CHẾ ĐỘ NĂM: lấy từ bảng QuyDinhTyLe_XetThiDuaNam
                var tyLeNam = DocTyLeXetThiDuaNam();
                if (tyLeNam == null)
                {
                    ThongBao("Không đọc được bảng QuyDinhTyLe_XetThiDuaNam. Tỷ lệ Năm đang để 0.");
                    tyLeNam = new[] { 0, 0, 0 };
                }
                values = tyLeNam;
            }
            else
            {
                // CHẾ ĐỘ THÁNG (hoặc KHTNV / Không PL): giữ nguyên cách cũ
                values = key != null ? Module_QuyDinhTyLe.GetLoaiTapThe(key) : new[] { 0, 0, 0 };
            }
            phanTramLoai1 = values.ElementAtOrDefault(0);
            phanTramLoai2 = values.ElementAtOrDefault(1);
            phanTramLoai3 = values.ElementAtOrDefault(2);
            CapNhatLabelPhanTram();
        }
        private async void comboBox1_CheDoXetThiDua_SelectedIndexChanged(object? sender, EventArgs e)
        {
            try
            {
                string val = comboBox1_CheDoXetThiDua.Text?.Trim() ?? "";
                Module_HeThong.LuuCheDoXetThiDuaNam(val);
                LoadComboBoxDeNghi();        // đổi danh sách đề nghị theo Tháng/Năm
                LoadQuyDinhTheoDeNghi();     // nạp lại tỷ lệ theo bảng tương ứng
                AnhXaGiaoDienThiDua();
                // Bảng 1 tô màu theo phanTramLoai -> chỉ cần vẽ lại
                kryptonDataGridView1.Invalidate();
                // Bảng 2 tính lại chỉ tiêu và kết luận
                _allowLoadBang2 = true;
                if (KiemTraCoDuLieuDanhSach())
                    await Bang2_Async();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[CheDoXetThiDua_Changed] " + ex.Message);
            }
        }
        private void kryptonButton_KiemTraTLvaQS_Click(object? sender, EventArgs e)
        {
            if (_dangMoForm11) return;
            _dangMoForm11 = true;
            kryptonButton_KiemTraTLvaQS.Enabled = false;

            try
            {
                if (_textGocNutKiemTra == null)
                {
                    _textGocNutKiemTra = kryptonButton_KiemTraTLvaQS.Values.Text;
                    _anhGocNutKiemTra = kryptonButton_KiemTraTLvaQS.Values.Image;
                }
                kryptonButton_KiemTraTLvaQS.Values.Text = "Đang xử lý...";

                // 📌 Lấy giá trị chế độ xét thi đua hiện tại từ ComboBox
                string cheDoHienTai = comboBox1_CheDoXetThiDua?.Text?.Trim();
                if (string.IsNullOrWhiteSpace(cheDoHienTai))
                {
                    cheDoHienTai = "Tháng";
                }

                if (form11 == null || form11.IsDisposed)
                {
                    // Truyền giá trị qua Constructor khi tạo mới
                    form11 = new Form11_KiemTraTyLe(cheDoHienTai)
                    {
                        Owner = this,
                        ShowInTaskbar = false
                    };
                    form11.FormClosed += (s, ev) =>
                    {
                        if (!this.IsDisposed && this.IsHandleCreated)
                        {
                            kryptonButton_KiemTraTLvaQS.Values.Text = _textGocNutKiemTra;
                            kryptonButton_KiemTraTLvaQS.Values.Image = _anhGocNutKiemTra;
                            kryptonButton_KiemTraTLvaQS.Enabled = true;
                            _dangMoForm11 = false;
                        }
                    };
                }
                else
                {
                    // 📌 Nếu Form11 đã mở sẵn, gọi phương thức hoặc gán Property để cập nhật lại dữ liệu/biến
                    form11.CapNhatCheDoXet(cheDoHienTai);
                    // Hoặc nếu dùng Property: form11.CheDoXetTruyenVao = cheDoHienTai;
                }

                if (!form11.Visible) form11.Show();
                form11.Activate();
                Module_ThongBao.ThanhCong("Gọi trang kiểm tra tỷ lệ %");
            }
            catch (Exception ex)
            {
                _dangMoForm11 = false;
                kryptonButton_KiemTraTLvaQS.Enabled = true;
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    kryptonButton_KiemTraTLvaQS.Values.Text = _textGocNutKiemTra;
                    kryptonButton_KiemTraTLvaQS.Values.Image = _anhGocNutKiemTra;
                }
                MessageBox.Show($"Không thể mở trang kiểm tra tỷ lệ.\n\nChi tiết: {ex.Message}",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
