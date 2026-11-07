///Phiên bản 5 - Tối ưu mở form tức thì
///
using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form60_NhapLieuCBCS : Form
    {
        //  NGUYÊN TẮC THIẾT KẾ PHIÊN BẢN 5
        //  1. Luồng UI chỉ làm việc nhẹ: dựng bố cục sẵn lúc form còn ẩn, gán dữ liệu đã chuẩn bị.
        //  2. Mọi việc nặng (đọc CSDL, giải mã AES, ghi CSDL, decode logo) chạy NỀN.
        //  3. Dữ liệu lên lưới ngay khi đọc + giải mã xong; việc ghi lại bảng DanhSach_Nhap chạy song song phía sau.
        //  4. Mọi lệnh ghi CSDL đi qua MỘT hàng đợi tuần tự (_chuoiGhiDb) -> không tranh chấp, không chặn UI.
        //  5. Di chuyển lên/xuống cập nhật RAM tức thì (như web), CSDL ghi nền sau.
        //  6. Việc phụ (tooltip, menu chuột phải) hoãn tới sau khi form đã hiện + có dữ liệu.
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private readonly string _connectionString;
        // Bảng dữ liệu làm việc trong RAM (DefaultView luôn sắp theo STT)
        private DataTable? _dtGoc;
        private int _currentSelectedID = 0;
        private bool _dangXuLyHuongDan = false;
        private bool _coThayDoiDuLieu = false;
        private string _ketQuaDongBo = string.Empty;
        // ===== Trạng thái =====
        private bool _daNapXong = false;           // dữ liệu đã lên lưới
        private bool _dangLamMoi = false;          // chống bấm "Làm mới" chồng nhau
        private bool _dangThem = false;            // đang thêm mới (khóa di chuyển / làm mới trong lúc đó)
        private HashSet<string>? _soHieuGocCache;  // Số hiệu (đã giải mã) của bảng DanhSach
        private System.Windows.Forms.Timer? _timerLoc; // Debounce ô tìm kiếm
        private Task? _taskKhoiTao;                // KhoiTaoAsync chỉ chạy đúng 1 lần
        private bool _luoiDaDinhDang;              // lưới chỉ dựng cột 1 lần
        private bool? _laTanBinhCache;             // phiên bản phần mềm không đổi trong 1 lần mở form
        private static bool? _laTanBinhLanTruoc;   // nhớ giữa các lần mở -> đặt tiêu đề đúng ngay khi hiện
        private bool _viecPhuDaLenLich;
        private Queue<Control>? _hangDoiHandle;
        private bool _daEpBoCuc;
        // ===== Hàng đợi ghi CSDL tuần tự (nền) =====
        private Task _chuoiGhiDb = Task.CompletedTask;
        private volatile bool _bangNhapHong;
        private string _loiBangNhap = string.Empty;
        // ===== Tài nguyên dùng chung giữa các lần mở form (không tạo lại, không rò GDI) =====
        private static Font? _fontChu;
        private static Font? _fontTieuDeBold;
        private static Font? _fontStt;
        private static Task<Image?>? _taskLogo;     // logo đã thu nhỏ (decode ở luồng nền)
        private const string SapXepMacDinh = "STT ASC, ID ASC";
        // Bật true để giải mã AES song song CHỈ KHI Module_BaoMatAES.GiaiMa đã được kiểm tra an toàn đa luồng.
        private const bool GIAI_MA_SONG_SONG = false;
        // Đặt true nếu thấy form chớp khi kéo/đổi kích thước (WS_EX_COMPOSITED làm vẽ chậm hơn nhiều với Krypton).
        private static readonly bool BatDoubleBufferToanCay = false;
        private static readonly PropertyInfo? _piDoubleBuffered =
            typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
        // ===== Nhật ký hiệu năng: ghi NỀN qua hàng đợi (không IO trên luồng UI). Đặt false khi xong đo đạc =====
        private const bool GhiNhatKyHieuNang = true;
        private static readonly Stopwatch _dongHoHieuNang = Stopwatch.StartNew();
        private static readonly ConcurrentQueue<string> _hangLog = new();
        private static int _dangXaLog;
        private static long _mocBam;
        public static void GhiHieuNang(string moTa)
        {
            if (!GhiNhatKyHieuNang) return;
            try
            {
                string dong = $"{DateTime.Now:HH:mm:ss.fff} [{_dongHoHieuNang.ElapsedMilliseconds,9} ms] {moTa}";
                Debug.WriteLine("[Form60] " + dong);
                _hangLog.Enqueue(dong);
                if (Interlocked.CompareExchange(ref _dangXaLog, 1, 0) == 0)
                    ThreadPool.QueueUserWorkItem(_ => XaNhatKy());
            }
            catch { /* ghi nhật ký không được làm hỏng chức năng */ }
        }
        private static void XaNhatKy()
        {
            try
            {
                var sb = new StringBuilder();
                while (_hangLog.TryDequeue(out var d)) sb.AppendLine(d);
                if (sb.Length > 0)
                    File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "Form60_hieunang.log"), sb.ToString());
            }
            catch { }
            finally
            {
                Interlocked.Exchange(ref _dangXaLog, 0);
                if (!_hangLog.IsEmpty && Interlocked.CompareExchange(ref _dangXaLog, 1, 0) == 0)
                    ThreadPool.QueueUserWorkItem(_ => XaNhatKy());
            }
        }
        /// <summary>Form6 gọi ngay khi người dùng bấm nút -> đo được "bấm -> form hiện xong".</summary>
        public static void DanhDauThoiDiemBam()
        {
            _mocBam = Stopwatch.GetTimestamp();
            GhiHieuNang("=== BẤM NÚT mở form 60 ===");
        }
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                if (BatDoubleBufferToanCay) cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED (mặc định TẮT: gây giật với Krypton)
                return cp;
            }
        }
        public Form60_NhapLieuCBCS()
        {
            var swKhoiTao = Stopwatch.StartNew();
            InitializeComponent();
            _taskLogo ??= Task.Run(TaoLogoThuNho);   // decode logo 549x455 ở luồng nền, không chặn constructor
            BatDoubleBufferForm60();
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            _connectionString = $"Data Source={_csdl2Path};";
            DangKyCacSuKien();
            CuaSo_KhoiTaoStatusStrip();
            DinhDangDataGridViewHienDai();   // dựng cột + giao diện lưới đúng 1 lần ngay từ đầu (lúc form còn ẩn)
            GhiHieuNang($"Constructor Form60 xong: {swKhoiTao.ElapsedMilliseconds} ms");
        }
        //  LOGO
        private static Image? TaoLogoThuNho()
        {
            try
            {
                var rm = new System.ComponentModel.ComponentResourceManager(typeof(Form60_NhapLieuCBCS));
                using var goc = rm.GetObject("pictureBox1.Image") as Image;
                if (goc == null) return null;
                const int w = 184, h = 118; // gấp đôi kích thước hiển thị để vẫn nét khi 200% DPI
                var bmp = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(goc, new Rectangle(0, 0, w, h));
                }
                return bmp;
            }
            catch { return null; }
        }
        private async Task GanLogoAsync()
        {
            try
            {
                var t = _taskLogo;
                if (t == null) return;
                var img = await t;
                if (img != null && !IsDisposed) pictureBox1.Image = img;
            }
            catch { /* logo chỉ để trang trí */ }
        }
        //  DOUBLE BUFFER / FONT
        private void BatDoubleBufferForm60()
        {
            try
            {
                DoubleBuffered = true;
                BatDoubleBufferDeQuyForm60(this);
            }
            catch { }
        }
        private static void BatDoubleBufferDeQuyForm60(Control cha)
        {
            foreach (Control c in cha.Controls)
            {
                if (c is TableLayoutPanel || c is GroupBox || c is DataGridView)
                {
                    try { _piDoubleBuffered?.SetValue(c, true); } catch { }
                }
                if (c.HasChildren) BatDoubleBufferDeQuyForm60(c);
            }
        }
        private static Font LayFontChu() =>
            _fontChu ??= new Font(Module_HeThong.TenFontHeThong, 9.75F, FontStyle.Regular, GraphicsUnit.Point);
        private static Font LayFontTieuDeBold() =>
            _fontTieuDeBold ??= new Font(Module_HeThong.TenFontHeThong, 9.75F, FontStyle.Bold, GraphicsUnit.Point);
        private static Font LayFontStt() =>
            _fontStt ??= new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Regular, GraphicsUnit.Point);
        /// <summary>Gom nhóm đăng ký toàn bộ sự kiện của Form</summary>
        private void DangKyCacSuKien()
        {
            Shown -= Form60_NhapLieuCBCS_Shown;
            Shown += Form60_NhapLieuCBCS_Shown;
            FormClosed -= Form60_NhapLieuCBCS_FormClosed;
            FormClosed += Form60_NhapLieuCBCS_FormClosed;
            textBox_SoHieu.KeyPress -= textBox_SoHieu_KeyPress;
            textBox_SoHieu.KeyPress += textBox_SoHieu_KeyPress;
            textBox_SoHieu.Leave -= textBox_SoHieu_Leave;
            textBox_SoHieu.Leave += textBox_SoHieu_Leave;
            kryptonDataGridView1.CellClick -= KryptonDataGridView1_CellClick;
            kryptonDataGridView1.CellClick += KryptonDataGridView1_CellClick;
            kryptonDataGridView1.DataError += (s, ev) => { ev.ThrowException = false; };
        }
        // Yêu mèo cam - Lê Trung Kiên
        private bool KiemTraLaTanBinh()
        {
            return _laTanBinhCache ??= (Module_TaiKhoan.LayPhienBanPhanMem() ?? string.Empty)
                .Contains("tân binh", StringComparison.OrdinalIgnoreCase);
        }
        //  KHỞI TẠO / NẠP DỮ LIỆU
        private async void Form60_NhapLieuCBCS_Load(object sender, EventArgs e)
        {
            try
            {
                // Đã biết phiên bản từ lần mở trước -> đặt tiêu đề đúng ngay, khỏi nhấp nháy chữ mặc định
                if (_laTanBinhLanTruoc is bool b) ApDungNhanTheoPhienBan(b);
                _ = GanLogoAsync();
                await KhoiTaoAsync();
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                    MessageBox.Show($"Lỗi khi khởi tạo dữ liệu Form: {ex.Message}", "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>Chuẩn bị dữ liệu + giao diện của form (an toàn khi gọi nhiều lần: chỉ chạy 1 lần).</summary>     
        public Task KhoiTaoAsync() => _taskKhoiTao ??= KhoiTaoNoiBoAsync();
        private sealed class GoiDuLieuKhoiTao
        {
            public DataTable Bang = new DataTable();
            public string[] ChucVu = Array.Empty<string>();
            public string[] DonVi = Array.Empty<string>();
            public bool? LaTanBinh;
            public string? LoiChucVu;
            public Task GhiBangNhap = Task.CompletedTask;
        }
        private async Task KhoiTaoNoiBoAsync()
        {
            try
            {
                var sw = Stopwatch.StartNew();
                toolStripStatusLabel1.Text = "Đang tải dữ liệu...";
                // 1. Việc nặng (đọc, giải mã AES, dựng bảng RAM, khởi động ghi bảng nhập) chạy NỀN
                GoiDuLieuKhoiTao goi = await Task.Run(NapDuLieuNen);
                if (IsDisposed || Disposing) return;
                GhiHieuNang($"Khởi tạo: nạp dữ liệu nền xong {sw.ElapsedMilliseconds} ms ({goi.Bang.Rows.Count} dòng)");
                sw.Restart();
                // 2. Việc nhẹ trên luồng UI
                _chuoiGhiDb = goi.GhiBangNhap;
                _ = goi.GhiBangNhap.ContinueWith(_ =>
                {
                    if (_bangNhapHong && !IsDisposed && IsHandleCreated)
                        BeginInvoke(new Action(() => MessageBox.Show(
                            "Không thể chuẩn bị bảng nhập tạm.\n\nChi tiết: " + _loiBangNhap,
                            "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
                if (goi.LaTanBinh.HasValue)
                {
                    _laTanBinhCache = goi.LaTanBinh;
                    _laTanBinhLanTruoc = goi.LaTanBinh;
                }
                ApDungNhanTheoPhienBan(KiemTraLaTanBinh());
                DoDuLieuLenCacCombo(goi);
                long tCombo = sw.ElapsedMilliseconds;
                GanDuLieuLenGrid(goi.Bang);
                long tLuoi = sw.ElapsedMilliseconds - tCombo;
                CapNhatThanhTrangThaiStatusStrip(goi.Bang.DefaultView.Count);
                GhiHieuNang($"Khởi tạo (luồng UI): combo {tCombo} ms, gán lưới {tLuoi} ms");
                if (!IsHandleCreated) TaoHandleDeQuy(this);
                GanSuKienToanCuc();           // gắn sự kiện lọc SAU CÙNG để lúc nạp không kích hoạt lọc thừa
                ThuLenLichViecPhu();
                if (goi.LoiChucVu != null)
                {
                    MessageBox.Show(
                        $"Không thể tải danh sách chức vụ.\n\nChi tiết: {goi.LoiChucVu}",
                        "Lỗi tải dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                _daNapXong = true;
            }
        }
        /// <summary>Chạy ở luồng nền: KHÔNG được đụng tới control giao diện.</summary>
        private GoiDuLieuKhoiTao NapDuLieuNen()
        {
            var goi = new GoiDuLieuKhoiTao();
            try
            {
                goi.LaTanBinh = (Module_TaiKhoan.LayPhienBanPhanMem() ?? string.Empty)
                    .Contains("tân binh", StringComparison.OrdinalIgnoreCase);
            }
            catch { /* để luồng UI tự xác định sau */ }
            List<object[]> dong = DocVaGiaiMaDanhSachGoc(out string[] chucVu, out string? loiChucVu);
            goi.ChucVu = chucVu;
            goi.LoiChucVu = loiChucVu;
            try
            {
                goi.DonVi = (Module_DonVi.LayDanhSachDonViUuTienArray() ?? Array.Empty<string>())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .ToArray();
            }
            catch { goi.DonVi = Array.Empty<string>(); }
            goi.Bang = DungBangTuDong(dong);   // lưới nhận dữ liệu NGAY, không chờ ghi CSDL
            goi.GhiBangNhap = Task.Run(() => GhiLaiBangNhap(dong)); // ghi DanhSach_Nhap song song phía sau
            return goi;
        }
        private const string ScriptTaoBangNhap = @"
                CREATE TABLE IF NOT EXISTS ""DanhSach_Nhap"" (
                    ""ID""              INTEGER,
                    ""STT""             INTEGER NOT NULL DEFAULT 0,
                    ""HoVaTen""         TEXT NOT NULL,
                    ""SoHieu""          TEXT NOT NULL,
                    ""NamSinh""         TEXT NOT NULL,
                    ""QueQuan""         TEXT,
                    ""NgayVaoCAND""     TEXT,
                    ""CapBac""          TEXT,
                    ""ChucVu""          TEXT,
                    ""DonVi""           TEXT,
                    ""PhanLoai""        TEXT,
                    ""GhiChu""          TEXT,
                    ""ThuTuSapXep""     INTEGER NOT NULL DEFAULT 0,
                    ""ThoiGianTao""     TEXT NOT NULL,
                    ""ThoiGianCapNhat"" TEXT,
                    PRIMARY KEY(""ID"" AUTOINCREMENT)
                );";
        /// <summary>
        /// Đọc DanhSach + DanhSach_ChucVu bằng MỘT kết nối, giải mã mỗi chuỗi mã hóa khác nhau đúng 1 lần.
        /// Trả về các dòng đã giải mã: [0]=STT(int), [1..10]=HoVaTen,SoHieu,NamSinh,QueQuan,NgayVaoCAND,CapBac,ChucVu,DonVi,PhanLoai,GhiChu
        /// </summary>
        private List<object[]> DocVaGiaiMaDanhSachGoc(out string[] chucVu, out string? loiChucVu)
        {
            const int SoCot = 10;
            chucVu = Array.Empty<string>();
            loiChucVu = null;
            var raw = new List<(int Stt, string[] Cot)>();
            var cvRaw = new List<string>();
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new SqliteCommand(
                    "SELECT STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu FROM DanhSach ORDER BY STT;", conn))
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        int stt = rd.IsDBNull(0) ? 0 : rd.GetInt32(0);
                        var cot = new string[SoCot];
                        for (int i = 0; i < SoCot; i++)
                            cot[i] = rd.IsDBNull(i + 1) ? string.Empty : (Convert.ToString(rd.GetValue(i + 1)) ?? string.Empty);
                        raw.Add((stt, cot));
                    }
                }
                try
                {
                    using var cmdCv = new SqliteCommand("""
                        SELECT Ten_ChucVu
                        FROM DanhSach_ChucVu
                        WHERE Ten_ChucVu IS NOT NULL
                          AND TRIM(Ten_ChucVu) <> ''
                        ORDER BY ID ASC;
                        """, conn);
                    using var rdCv = cmdCv.ExecuteReader();
                    while (rdCv.Read())
                        cvRaw.Add(rdCv.IsDBNull(0) ? string.Empty : (Convert.ToString(rdCv.GetValue(0)) ?? string.Empty));
                }
                catch (Exception ex)
                {
                    loiChucVu = ex.Message;
                }
            }
            // Gom các chuỗi mã hóa KHÁC NHAU -> giải mã mỗi chuỗi 1 lần (cấp bậc, đơn vị, phân loại... lặp rất nhiều)
            var chiSo = new Dictionary<string, int>(1024, StringComparer.Ordinal);
            var dsDuy = new List<string>(1024);
            void Gom(string s)
            {
                if (s.Length > 0 && !chiSo.ContainsKey(s)) { chiSo[s] = dsDuy.Count; dsDuy.Add(s); }
            }
            foreach (var r in raw) foreach (var c in r.Cot) Gom(c);
            foreach (var c in cvRaw) Gom(c);
            var giai = new string[dsDuy.Count];
            if (GIAI_MA_SONG_SONG && dsDuy.Count > 64)
                Parallel.For(0, dsDuy.Count, i => giai[i] = GiaiMaAnToan(dsDuy[i]));
            else
                for (int i = 0; i < dsDuy.Count; i++) giai[i] = GiaiMaAnToan(dsDuy[i]);
            string Map(string s) => s.Length == 0 ? string.Empty : giai[chiSo[s]];
            var ketQua = new List<object[]>(raw.Count);
            var cache = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in raw)
            {
                var d = new object[SoCot + 1];
                d[0] = r.Stt;
                for (int i = 0; i < SoCot; i++) d[i + 1] = Map(r.Cot[i]);
                ketQua.Add(d);
                string soHieu = ((string)d[2]).Trim();
                if (soHieu.Length > 0) cache.Add(soHieu);
            }
            _soHieuGocCache = cache;
            var dsCv = new List<string>();
            var daCo = new HashSet<string>(StringComparer.Ordinal);
            foreach (var c in cvRaw)
            {
                string cv = Map(c).Trim();
                if (cv.Length > 0 && daCo.Add(cv)) dsCv.Add(cv);
            }
            chucVu = dsCv.ToArray();
            return ketQua;
        }
        private static DataTable TaoBangRong()
        {
            var dt = new DataTable("DanhSach_Nhap");
            dt.Columns.Add("ID", typeof(long));
            dt.Columns.Add("STT", typeof(long));
            dt.Columns.Add("HoVaTen", typeof(string));
            dt.Columns.Add("SoHieu", typeof(string));
            dt.Columns.Add("NamSinh", typeof(string));
            dt.Columns.Add("QueQuan", typeof(string));
            dt.Columns.Add("NgayVaoCAND", typeof(string));
            dt.Columns.Add("CapBac", typeof(string));
            dt.Columns.Add("ChucVu", typeof(string));
            dt.Columns.Add("DonVi", typeof(string));
            dt.Columns.Add("PhanLoai", typeof(string));
            dt.Columns.Add("GhiChu", typeof(string));
            dt.Columns.Add("ThuTuSapXep", typeof(long));
            dt.Columns.Add("ThoiGianTao", typeof(string));
            dt.Columns.Add("ThoiGianCapNhat", typeof(string));
            return dt;
        }
        /// <summary>Dựng bảng RAM giống hệt bảng DanhSach_Nhap sẽ được ghi (ID = 1..n theo thứ tự STT).</summary>
        private static DataTable DungBangTuDong(List<object[]> dong)
        {
            var dt = TaoBangRong();
            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            dt.BeginLoadData();
            long id = 0;
            foreach (var d in dong)
            {
                id++;
                long stt = Convert.ToInt64(d[0]);
                dt.Rows.Add(new object?[]
                {
                    id, stt, d[1], d[2], d[3], d[4], d[5], d[6], d[7], d[8], d[9], d[10],
                    stt, now, DBNull.Value
                });
            }
            dt.EndLoadData();
            dt.AcceptChanges();
            return dt;
        }
        /// <summary>Chạy NỀN: xóa + ghi lại bảng DanhSach_Nhap (một transaction). Lỗi được ghi vào cờ _bangNhapHong.</summary>
        private void GhiLaiBangNhap(List<object[]> dong)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();
                using (var cmdTao = new SqliteCommand(ScriptTaoBangNhap, conn)) cmdTao.ExecuteNonQuery();
                using var transaction = conn.BeginTransaction();
                try
                {
                    using (var cmdDelete = new SqliteCommand(
                        "DELETE FROM DanhSach_Nhap; DELETE FROM sqlite_sequence WHERE name='DanhSach_Nhap';",
                        conn, transaction))
                    {
                        cmdDelete.ExecuteNonQuery();
                    }
                    const string insertSql = """
                        INSERT INTO DanhSach_Nhap (
                            ID, STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND,
                            CapBac, ChucVu, DonVi, PhanLoai, GhiChu, ThuTuSapXep,
                            ThoiGianTao, ThoiGianCapNhat
                        ) VALUES (
                            $ID, $STT, $HoVaTen, $SoHieu, $NamSinh, $QueQuan, $NgayVaoCAND,
                            $CapBac, $ChucVu, $DonVi, $PhanLoai, $GhiChu, $ThuTuSapXep,
                            $ThoiGianTao, $ThoiGianCapNhat
                        );
                        """;
                    using var cmdInsert = new SqliteCommand(insertSql, conn, transaction);
                    var pID = cmdInsert.Parameters.Add("$ID", SqliteType.Integer);
                    var pSTT = cmdInsert.Parameters.Add("$STT", SqliteType.Integer);
                    var pHoVaTen = cmdInsert.Parameters.Add("$HoVaTen", SqliteType.Text);
                    var pSoHieu = cmdInsert.Parameters.Add("$SoHieu", SqliteType.Text);
                    var pNamSinh = cmdInsert.Parameters.Add("$NamSinh", SqliteType.Text);
                    var pQueQuan = cmdInsert.Parameters.Add("$QueQuan", SqliteType.Text);
                    var pNgayVao = cmdInsert.Parameters.Add("$NgayVaoCAND", SqliteType.Text);
                    var pCapBac = cmdInsert.Parameters.Add("$CapBac", SqliteType.Text);
                    var pChucVu = cmdInsert.Parameters.Add("$ChucVu", SqliteType.Text);
                    var pDonVi = cmdInsert.Parameters.Add("$DonVi", SqliteType.Text);
                    var pPhanLoai = cmdInsert.Parameters.Add("$PhanLoai", SqliteType.Text);
                    var pGhiChu = cmdInsert.Parameters.Add("$GhiChu", SqliteType.Text);
                    var pTuTu = cmdInsert.Parameters.Add("$ThuTuSapXep", SqliteType.Integer);
                    var pTao = cmdInsert.Parameters.Add("$ThoiGianTao", SqliteType.Text);
                    var pCapNhat = cmdInsert.Parameters.Add("$ThoiGianCapNhat", SqliteType.Text);
                    pTao.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    pCapNhat.Value = DBNull.Value;
                    long id = 0;
                    foreach (object[] d in dong)
                    {
                        id++;
                        pID.Value = id;
                        pSTT.Value = d[0];
                        pHoVaTen.Value = d[1];
                        pSoHieu.Value = d[2];
                        pNamSinh.Value = d[3];
                        pQueQuan.Value = d[4];
                        pNgayVao.Value = d[5];
                        pCapBac.Value = d[6];
                        pChucVu.Value = d[7];
                        pDonVi.Value = d[8];
                        pPhanLoai.Value = d[9];
                        pGhiChu.Value = d[10];
                        pTuTu.Value = d[0];
                        cmdInsert.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
                catch
                {
                    try { transaction.Rollback(); } catch { }
                    throw;
                }
            }
            catch (Exception ex)
            {
                _loiBangNhap = ex.Message;
                _bangNhapHong = true;
            }
            GhiHieuNang($"Ghi nền bảng DanhSach_Nhap xong: {sw.ElapsedMilliseconds} ms ({dong.Count} dòng)");
        }
        //  HÀNG ĐỢI GHI CSDL TUẦN TỰ
        /// <summary>Xếp một việc CSDL vào hàng đợi: chạy nền, đúng thứ tự, không bao giờ chồng nhau.</summary>
        private Task<T> XepHangGhiDb<T>(Func<T> viec)
        {
            var truoc = _chuoiGhiDb;
            Task<T> moi = truoc.ContinueWith(_ =>
            {
                if (_bangNhapHong)
                    throw new InvalidOperationException("Bảng nhập tạm chưa sẵn sàng: " + _loiBangNhap);
                return viec();
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            _chuoiGhiDb = moi;
            return moi;
        }
        private void BaoLoiGhiNen(string? thongBao)
        {
            if (IsDisposed || !IsHandleCreated) return;
            BeginInvoke(new Action(async () =>
            {
                try
                {
                    MessageBox.Show($"Lỗi ghi dữ liệu nền:\n{thongBao}\n\nDữ liệu sẽ được nạp lại từ CSDL.",
                        "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    await LoadDuLieuLenGridAsync();
                }
                catch { }
            }));
        }
        //  DỰNG SẴN GIAO DIỆN LÚC ẨN (Form6 gọi lúc rảnh)
        public void ChuanBiGiaoDien()
        {
            if (IsDisposed) return;
            if (!IsHandleCreated) TaoHandleDeQuy(this);
            if (!_daEpBoCuc) { _daEpBoCuc = true; EpBoCucTruoc(); }
        }
        /// <summary>
        /// Tạo handle theo từng phần nhỏ (mỗi lần tối đa nganSachMs), cuối cùng ép bố cục chạy sẵn ở kích thước thật.
        /// Trả về true nếu còn việc. Gọi lặp bằng timer lúc rảnh.
        /// </summary>
        public bool ChuanBiGiaoDienTungPhan(int nganSachMs)
        {
            if (IsDisposed) return false;
            if (_hangDoiHandle == null)
            {
                _hangDoiHandle = new Queue<Control>();
                _hangDoiHandle.Enqueue(this);
            }
            var sw = Stopwatch.StartNew();
            while (_hangDoiHandle.Count > 0 && sw.ElapsedMilliseconds < nganSachMs)
            {
                Control c = _hangDoiHandle.Dequeue();
                if (c.IsDisposed) continue;
                try { _ = c.Handle; } catch { continue; }
                for (int i = 0; i < c.Controls.Count; i++) _hangDoiHandle.Enqueue(c.Controls[i]);
            }
            if (_hangDoiHandle.Count > 0) return true;
            if (!_daEpBoCuc)
            {
                _daEpBoCuc = true;
                EpBoCucTruoc();   // một lượt riêng, để lượt này không vượt ngân sách
                return true;
            }
            return false;
        }
        /// <summary>Đặt kích thước = kích thước panel cha rồi chạy bố cục TableLayoutPanel khi form còn ẩn -> lúc Show chỉ còn vẽ.</summary>
        private void EpBoCucTruoc()
        {
            try
            {
                var cha = Parent;
                if (cha != null && !Visible && cha.ClientSize.Width > 0 && cha.ClientSize.Height > 0)
                    ClientSize = cha.ClientSize;
                SuspendLayout();
                ResumeLayout(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[EpBoCucTruoc] " + ex.Message);
            }
        }
        private static void TaoHandleDeQuy(Control goc)
        {
            try { _ = goc.Handle; } catch { return; }
            for (int i = 0; i < goc.Controls.Count; i++) TaoHandleDeQuy(goc.Controls[i]);
        }
        private void Form60_NhapLieuCBCS_Shown(object sender, EventArgs e)
        {
            textBox_HoVaTen.Focus();
            if (_mocBam != 0)
                GhiHieuNang($"Form60 đã hiện xong (Shown): {(Stopwatch.GetTimestamp() - _mocBam) * 1000 / Stopwatch.Frequency} ms kể từ lúc bấm");
        }
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            ThuLenLichViecPhu();
        }
        /// <summary>Việc phụ (tooltip, tích hợp menu chuột phải) làm SAU khi form đã hiện và có dữ liệu để không chen vào khung hình đầu.</summary>
        private void ThuLenLichViecPhu()
        {
            if (_viecPhuDaLenLich || IsDisposed || !IsHandleCreated || !Visible) return;
            _viecPhuDaLenLich = true;
            var tm = new System.Windows.Forms.Timer { Interval = 300 };
            tm.Tick += (s, ev) =>
            {
                tm.Stop();
                tm.Dispose();
                if (IsDisposed) return;
                var sw = Stopwatch.StartNew();
                InitToolTips();
                long tTip = sw.ElapsedMilliseconds;
                try { Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1); }
                catch (Exception ex) { Debug.WriteLine("[MenuChuotPhai] " + ex.Message); }
                GhiHieuNang($"Việc phụ sau khi hiện: tooltip {tTip} ms, menu chuột phải {sw.ElapsedMilliseconds - tTip} ms");
            };
            tm.Start();
        }
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            toolTip1.InitialDelay = 300;
            toolTip1.AutoPopDelay = 2500;
            toolTip1.ReshowDelay = 100;
            toolTip1.ShowAlways = true;
            var tips = new Dictionary<Control, string>
            {
                { kryptonButton_LamMoiCacOTimKiem, "Làm mới các ô tìm kiếm" },
                { kryptonButton_RefershCSDL, "Làm mới dữ liệu từ CSDL gốc" },
                { btnThemMoi, "Thêm mới CBCS" },
                { kryptonButton1_DiChuyenDataCBCSLen, "Di chuyển dữ liệu lên" },
                { kryptonButton1_DiChuyenDataCBCSXuong, "Di chuyển dữ liệu xuống" },
                { kryptonButton1_DongForm, "Đóng trang này và tự động đồng bộ dữ liệu vào Trang dữ liệu" }
            };
            foreach (var tip in tips)
            {
                if (tip.Key != null && !tip.Key.IsDisposed) toolTip1.SetToolTip(tip.Key, tip.Value);
            }
        }
        private void CuaSo_KhoiTaoStatusStrip()
        {
            toolStripStatusLabel1.Spring = false;
            toolStripStatusLabel1.Alignment = ToolStripItemAlignment.Left;
            toolStripStatusLabel1.TextAlign = ContentAlignment.MiddleLeft;
            toolStripStatusLabel1.ImageAlign = ContentAlignment.MiddleLeft;
            toolStripStatusLabel1.TextImageRelation = TextImageRelation.ImageBeforeText;
        }
        private void CapNhatThanhTrangThaiStatusStrip(int tongQuanSo, string noiDungNutPhai = "Thêm danh sách hàng loạt (Excel)")
        {
            try
            {
                toolStripStatusLabel1.Text = $"Tổng cộng: {tongQuanSo:N0} đồng chí";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[STATUS_STRIP_ERR] {ex.Message}");
            }
        }
        //  LƯỚI DỮ LIỆU
        private DataTable DocDanhSachNhap()
        {
            var dt = new DataTable();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = new SqliteCommand("SELECT * FROM DanhSach_Nhap ORDER BY STT ASC;", conn);
            using var reader = cmd.ExecuteReader();
            dt.Load(reader);
            return dt;
        }
        // Khai báo cột 1 lần duy nhất (AutoGenerateColumns = false)
        private static readonly (string Name, string Header, int Width, bool Center, bool Visible, bool Fill)[] CotLuoi =
        {
            ("ID",              "ID",        0,   false, false, false),
            ("STT",             "STT",       0,   false, false, false),
            ("HoVaTen",         "Họ và tên", 210, false, true,  false),
            ("SoHieu",          "Số hiệu",   105, true,  true,  false),
            ("NamSinh",         "Năm sinh",  95,  true,  true,  false),
            ("QueQuan",         "Quê quán",  250, false, true,  false),
            ("NgayVaoCAND",     "Vào CAND",  125, true,  true,  false),
            ("CapBac",          "Cấp bậc",   95,  true,  true,  false),
            ("ChucVu",          "Chức vụ",   105, true,  true,  false),
            ("DonVi",           "Đơn vị",    110, true,  true,  false),
            ("PhanLoai",        "Phân loại", 110, true,  true,  false),
            ("GhiChu",          "Ghi chú",   0,   false, true,  true),
            ("ThuTuSapXep",     "ThuTuSapXep", 0, false, false, false),
            ("ThoiGianTao",     "ThoiGianTao", 0, false, false, false),
            ("ThoiGianCapNhat", "ThoiGianCapNhat", 0, false, false, false)
        };
        /// <summary>Gán DataTable lên lưới (luồng UI). Cột đã khai báo sẵn nên chỉ đổi nguồn dữ liệu.</summary>
        private void GanDuLieuLenGrid(DataTable dt)
        {
            var dgv = kryptonDataGridView1;
            if (dgv == null || dgv.IsDisposed) return;
            DinhDangDataGridViewHienDai(); // chỉ chạy lần đầu
            _dtGoc = dt;
            try { dt.DefaultView.Sort = SapXepMacDinh; }
            catch (Exception ex) { Debug.WriteLine("[SORT_ERR] " + ex.Message); }
            dgv.SuspendLayout();
            try { dgv.DataSource = dt; }
            finally { dgv.ResumeLayout(); }
            // Nếu đang có điều kiện lọc (sau Thêm mới / Làm mới) -> áp lại cho khớp
            if (_daNapXong) ApDungBoLoc();
        }
        private async Task LoadDuLieuLenGridAsync()
        {
            var dgv = kryptonDataGridView1;
            if (dgv == null || dgv.IsDisposed) return;
            try
            {
                DataTable dt = await XepHangGhiDb(() => DocDanhSachNhap()); // đọc nền, sau mọi lệnh ghi đang chờ
                if (IsDisposed || Disposing) return;
                GanDuLieuLenGrid(dt);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu lên bảng:\n{ex.Message}", "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void GanSuKienToanCuc()
        {
            kryptonDataGridView1.RowPostPaint -= KryptonDataGridView1_RowPostPaint;
            kryptonDataGridView1.RowPostPaint += KryptonDataGridView1_RowPostPaint;
            // Ô gõ tìm kiếm: debounce 150ms để không lọc/vẽ lại lưới theo từng phím
            if (_timerLoc == null)
            {
                _timerLoc = new System.Windows.Forms.Timer { Interval = 150 };
                _timerLoc.Tick += (s, ev) => ApDungBoLoc();
            }
            textBox_TimKiemTheoTen.TextChanged -= KhoiDongLocTre;
            textBox_TimKiemTheoTen.TextChanged += KhoiDongLocTre;
            comboBox_TimKiemDonVi.SelectedIndexChanged -= ThucHienLocKep;
            comboBox_TimKiemDonVi.SelectedIndexChanged += ThucHienLocKep;
        }
        private void KhoiDongLocTre(object? sender, EventArgs e)
        {
            if (_timerLoc == null) return;
            _timerLoc.Stop();
            _timerLoc.Start();
        }
        /// <summary>Vẽ Số Thứ Tự ảo lên Row Header (tự nhảy 1,2,3... kể cả khi lọc).</summary>
        private void KryptonDataGridView1_RowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
        {
            if (sender is not DataGridView dgv) return;
            Rectangle headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, dgv.RowHeadersWidth, e.RowBounds.Height);
            TextRenderer.DrawText(e.Graphics, (e.RowIndex + 1).ToString(), LayFontStt(), headerBounds, SystemColors.ControlText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        }
        private void ThucHienLocKep(object? sender, EventArgs? e) => ApDungBoLoc();
        /// <summary>Lọc kết hợp Tên/Số hiệu (không phân biệt hoa thường) và Đơn vị bằng RowFilter của DataView.</summary>
        private void ApDungBoLoc()
        {
            _timerLoc?.Stop();
            var dt = _dtGoc;
            if (dt == null) return;
            string tuKhoa = textBox_TimKiemTheoTen.Text.Trim();
            if (tuKhoa == "Nhập tìm kiếm...") tuKhoa = string.Empty;
            string donViDuocChon = comboBox_TimKiemDonVi.SelectedItem?.ToString() ?? "Tất cả";
            var dieuKien = new List<string>(2);
            if (tuKhoa.Length > 0)
            {
                string an = EscapeLikeRowFilter(tuKhoa.Replace("'", "''"));
                dieuKien.Add($"(HoVaTen LIKE '%{an}%' OR SoHieu LIKE '%{an}%')");
            }
            if (donViDuocChon != "Tất cả" && donViDuocChon.Length > 0)
                dieuKien.Add($"DonVi = '{donViDuocChon.Replace("'", "''")}'");
            string filter = string.Join(" AND ", dieuKien);
            var view = dt.DefaultView;
            try
            {
                if (!string.Equals(view.RowFilter, filter, StringComparison.Ordinal))
                    view.RowFilter = filter;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[LOC_ERR] " + ex.Message);
                view.RowFilter = string.Empty;
            }
            CapNhatThanhTrangThaiStatusStrip(view.Count);
        }
        private static string EscapeLikeRowFilter(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new StringBuilder(s.Length + 8);
            foreach (char ch in s)
            {
                if (ch == '*' || ch == '%' || ch == '[' || ch == ']') sb.Append('[').Append(ch).Append(']');
                else sb.Append(ch);
            }
            return sb.ToString();
        }
        private void kryptonButton_LamMoiCacOTimKiem_Click(object sender, EventArgs e)
        {
            textBox_TimKiemTheoTen.Text = string.Empty;
            if (comboBox_TimKiemDonVi.Items.Count > 0) comboBox_TimKiemDonVi.SelectedIndex = 0; // "Tất cả"
            ApDungBoLoc();
        }
        /// <summary>Làm mới: nạp lại bảng nhập từ CSDL (sau mọi lệnh ghi đang chờ), khôi phục ô tìm kiếm.</summary>
        private async void kryptonButton_RefershCSDL_Click(object sender, EventArgs e)
        {
            if (!_daNapXong || _dangLamMoi || _dangThem) return;
            _dangLamMoi = true;
            try
            {
                DataTable dt = await XepHangGhiDb(() => DocDanhSachNhap());
                if (IsDisposed || Disposing) return;
                textBox_TimKiemTheoTen.Text = string.Empty;
                if (comboBox_TimKiemDonVi.Items.Count > 0) comboBox_TimKiemDonVi.SelectedIndex = 0;
                GanDuLieuLenGrid(dt);
                toolStripStatusLabel1.Text = "Làm mới dữ liệu thành công.";
                await Task.Delay(200);
                if (IsDisposed || Disposing) return;
                CapNhatThanhTrangThaiStatusStrip(dt.DefaultView.Count);
            }
            catch (Exception ex)
            {
                if (IsDisposed) return;
                toolStripStatusLabel1.Text = "Làm mới dữ liệu thất bại.";
                MessageBox.Show($"Không thể làm mới dữ liệu: {ex.Message}", "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _dangLamMoi = false;
            }
        }
        //  COMBO
        private void DoDuLieuLenCacCombo(GoiDuLieuKhoiTao goi)
        {
            string[] dsDonVi = goi.DonVi;
            NapCombo(comboBox_TimKiemDonVi, "Tất cả", dsDonVi, boTrung: false);
            CauHinhAnToanComboBox(comboBox_ChucVu);
            NapCombo(comboBox_ChucVu, "", goi.ChucVu, boTrung: true);
            CauHinhAnToanComboBox(comboBox_PhanLoai);
            NapCombo(comboBox_PhanLoai, "",
                new[]
                {
                    Module_HeThong.Loai_1,
                    Module_HeThong.Loai_2,
                    Module_HeThong.Loai_3,
                    Module_HeThong.Loai_4,
                    Module_HeThong.PL_KHONG_PL
                }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()),
                boTrung: true);
            comboBox_DonVi.AutoCompleteMode = AutoCompleteMode.None;
            comboBox_DonVi.AutoCompleteSource = AutoCompleteSource.None;
            NapCombo(comboBox_DonVi, "", dsDonVi, boTrung: true);
        }
        private static void NapCombo(ComboBox cbo, string mucDau, IEnumerable<string> ds, bool boTrung)
        {
            if (cbo == null) return;
            cbo.BeginUpdate();
            try
            {
                cbo.Items.Clear();
                cbo.Items.Add(mucDau);
                var daCo = boTrung ? new HashSet<string>(StringComparer.Ordinal) { mucDau } : null;
                foreach (string s in ds)
                {
                    if (daCo == null || daCo.Add(s)) cbo.Items.Add(s);
                }
                cbo.SelectedIndex = 0;
            }
            finally
            {
                cbo.EndUpdate();
            }
        }
        private void CauHinhAnToanComboBox(ComboBox cbo)
        {
            if (cbo == null) return;
            cbo.DropDownStyle = ComboBoxStyle.DropDownList;
            cbo.AutoCompleteMode = AutoCompleteMode.None;
            cbo.AutoCompleteSource = AutoCompleteSource.None;
        }
        //  SỐ HIỆU
        private void textBox_SoHieu_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ' || e.KeyChar == '-') e.Handled = true;
        }
        private void textBox_SoHieu_Leave(object? sender, EventArgs e)
        {
            if (!_daNapXong) return;
            string soHieuNhap = textBox_SoHieu.Text.Trim();
            // CHẾ ĐỘ 1: TÂN BINH
            if (KiemTraLaTanBinh())
            {
                if (string.IsNullOrWhiteSpace(soHieuNhap))
                {
                    textBox_SoHieu.Text = SinhSoHieuTanBinhTiepTheo();
                    return;
                }
                if (!soHieuNhap.StartsWith("ID", StringComparison.OrdinalIgnoreCase))
                {
                    soHieuNhap = int.TryParse(soHieuNhap, out int soThuTu) ? $"ID{soThuTu:D5}" : "ID" + soHieuNhap;
                    textBox_SoHieu.Text = soHieuNhap;
                }
                bool isTrungCSDL2 = false;
                if (_soHieuGocCache != null)
                {
                    isTrungCSDL2 = _soHieuGocCache.Contains(soHieuNhap);
                }
                else
                {
                    try
                    {
                        using var conn = new SqliteConnection($"Data Source={Module_DanduongGPS.DuongDanCSDL2}");
                        conn.Open();
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = "SELECT SoHieu FROM DanhSach WHERE SoHieu IS NOT NULL AND SoHieu <> ''";
                        using var rd = cmd.ExecuteReader();
                        while (rd.Read())
                        {
                            string shGiaiMa = Module_BaoMatAES.GiaiMa(rd.GetString(0));
                            if (string.Equals(shGiaiMa, soHieuNhap, StringComparison.OrdinalIgnoreCase)) { isTrungCSDL2 = true; break; }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi khi kiểm tra dữ liệu CSDL2: " + ex.Message, "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                if (isTrungCSDL2)
                {
                    MessageBox.Show($"Mã [{soHieuNhap}] đã tồn tại trong cơ sở dữ liệu! Hệ thống sẽ tự động cấp mã ID mới.",
                                    "Cảnh báo trùng ID", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox_SoHieu.Text = SinhSoHieuTanBinhTiepTheo();
                    textBox_SoHieu.Focus();
                }
                return;
            }
            // CHẾ ĐỘ 2: CBCS (kiểm tra trên RAM)
            soHieuNhap = ChuoiSoHieuChuan(textBox_SoHieu.Text);
            textBox_SoHieu.Text = soHieuNhap;
            if (string.IsNullOrWhiteSpace(soHieuNhap)) return;
            var dt = _dtGoc;
            if (dt != null && dt.Rows.Count > 0)
            {
                bool isTrung = false;
                foreach (DataRow row in dt.Rows)
                {
                    if (string.Equals(ChuoiSoHieuChuan(row["SoHieu"]?.ToString() ?? string.Empty), soHieuNhap, StringComparison.OrdinalIgnoreCase))
                    { isTrung = true; break; }
                }
                if (!isTrung) SetComboBoxPhanLoaiMacDinh(Module_HeThong.Loai_2);
            }
            else
            {
                SetComboBoxPhanLoaiMacDinh(Module_HeThong.Loai_2);
            }
        }
        private void SetComboBoxPhanLoaiMacDinh(string giaTriMacDinh)
        {
            if (comboBox_PhanLoai == null) return;
            if (comboBox_PhanLoai.Items.Contains(giaTriMacDinh))
            {
                comboBox_PhanLoai.SelectedItem = giaTriMacDinh;
            }
            else
            {
                int index = comboBox_PhanLoai.FindStringExact(giaTriMacDinh);
                if (index != -1) comboBox_PhanLoai.SelectedIndex = index;
                else comboBox_PhanLoai.Text = giaTriMacDinh;
            }
        }
        private string ChuoiSoHieuChuan(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return input.Replace(" ", "").Replace("-", "").Trim();
        }
        /// <summary>
        /// Sinh mã ID tân binh tiếp theo. Mặc định dò trên RAM (tập số hiệu gốc + bảng đang làm việc) -> không đụng CSDL.
        /// Nếu truyền connectionString riêng hoặc dữ liệu chưa nạp thì dùng đường CSDL như bản cũ.
        /// </summary>
        public string SinhSoHieuTanBinhTiepTheo(string? connectionStringForm = null)
        {
            var soHieuDaTonTai = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool dungRam = string.IsNullOrWhiteSpace(connectionStringForm) && _soHieuGocCache != null && _dtGoc != null;
            if (dungRam)
            {
                soHieuDaTonTai.UnionWith(_soHieuGocCache!);
                foreach (DataRow r in _dtGoc!.Rows)
                {
                    string sh = r["SoHieu"]?.ToString()?.Trim() ?? string.Empty;
                    if (sh.Length > 0) soHieuDaTonTai.Add(sh);
                }
            }
            else
            {
                string connString = string.IsNullOrWhiteSpace(connectionStringForm) ? _connectionString : connectionStringForm;
                if (_soHieuGocCache != null)
                {
                    soHieuDaTonTai.UnionWith(_soHieuGocCache);
                }
                else
                {
                    try
                    {
                        using var conn2 = new SqliteConnection($"Data Source={Module_DanduongGPS.DuongDanCSDL2}");
                        conn2.Open();
                        using var cmd = conn2.CreateCommand();
                        cmd.CommandText = "SELECT SoHieu FROM DanhSach WHERE SoHieu IS NOT NULL AND SoHieu <> ''";
                        using var rd = cmd.ExecuteReader();
                        while (rd.Read())
                        {
                            string shGiaiMa = Module_BaoMatAES.GiaiMa(rd.GetString(0));
                            if (!string.IsNullOrWhiteSpace(shGiaiMa)) soHieuDaTonTai.Add(shGiaiMa.Trim());
                        }
                    }
                    catch { }
                }
                try
                {
                    using var connNhap = new SqliteConnection(connString);
                    connNhap.Open();
                    using var cmd = connNhap.CreateCommand();
                    cmd.CommandText = "SELECT SoHieu FROM DanhSach_Nhap WHERE SoHieu IS NOT NULL AND SoHieu <> ''";
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        string sh = rd.GetString(0);
                        if (!string.IsNullOrWhiteSpace(sh)) soHieuDaTonTai.Add(sh.Trim());
                    }
                }
                catch { }
            }
            int chiSo = 1;
            string maMoi;
            do
            {
                maMoi = $"ID{chiSo:D5}";
                chiSo++;
            }
            while (soHieuDaTonTai.Contains(maMoi));
            return maMoi;
        }
        //  FORM <-> DTO
        private CanBoDTO LayDuLieuTuForm()
        {
            return new CanBoDTO
            {
                HoVaTen = textBox_HoVaTen.Text.Trim(),
                SoHieu = textBox_SoHieu.Text.Trim(),
                NamSinh = textBox_NamSinh.Text.Trim(),
                QueQuan = textBox_QueQuan.Text.Trim(),
                NgayVaoCAND = textBox_NgayVaoCAND.Text.Trim(),
                CapBac = textBox_CapBac.Text.Trim(),
                ChucVu = comboBox_ChucVu.SelectedItem?.ToString()?.Trim() ?? comboBox_ChucVu.Text.Trim(),
                DonVi = comboBox_DonVi.SelectedItem?.ToString()?.Trim() ?? comboBox_DonVi.Text.Trim(),
                PhanLoai = comboBox_PhanLoai.SelectedItem?.ToString()?.Trim() ?? comboBox_PhanLoai.Text.Trim(),
                GhiChu = textBox_GhiChu.Text.Trim()
            };
        }
        private void SetComboBoxValue(ComboBox cbo, string value)
        {
            if (cbo == null) return;
            int index = cbo.FindStringExact(value);
            cbo.SelectedIndex = index >= 0 ? index : 0;
        }
        private void XoaTrangCacControl()
        {
            _currentSelectedID = 0;
            textBox_HoVaTen.Text = string.Empty;
            textBox_SoHieu.Text = string.Empty;
            textBox_NamSinh.Text = string.Empty;
            textBox_QueQuan.Text = string.Empty;
            textBox_NgayVaoCAND.Text = string.Empty;
            textBox_CapBac.Text = string.Empty;
            textBox_GhiChu.Text = string.Empty;
            if (comboBox_ChucVu.Items.Count > 0) comboBox_ChucVu.SelectedIndex = 0;
            if (comboBox_DonVi.Items.Count > 0) comboBox_DonVi.SelectedIndex = 0;
            if (comboBox_PhanLoai.Items.Count > 0) comboBox_PhanLoai.SelectedIndex = 0;
            textBox_HoVaTen.Focus();
        }
        //  DI CHUYỂN LÊN / XUỐNG: cập nhật RAM tức thì, ghi CSDL nền theo hàng đợi
        private void kryptonButton1_DiChuyenDataCBCSLen_Click(object sender, EventArgs e) => DiChuyenDongHienTai(-1);
        private void kryptonButton1_DiChuyenDataCBCSXuong_Click(object sender, EventArgs e) => DiChuyenDongHienTai(+1);
        private void DiChuyenDongHienTai(int huong)
        {
            if (!_daNapXong || _dtGoc == null || _dangThem || _dangLamMoi) return;
            if (_bangNhapHong)
            {
                MessageBox.Show("Bảng nhập tạm chưa sẵn sàng: " + _loiBangNhap, "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                var dgv = kryptonDataGridView1;
                if (dgv.CurrentRow == null || dgv.CurrentRow.IsNewRow) return;
                int currentIndex = dgv.CurrentRow.Index;
                int otherIndex = currentIndex + huong;
                if (otherIndex < 0 || otherIndex >= dgv.Rows.Count) return;
                if (dgv.Rows[currentIndex].DataBoundItem is not DataRowView drvCurrent ||
                    dgv.Rows[otherIndex].DataBoundItem is not DataRowView drvOther)
                    return;
                int currentID = Convert.ToInt32(drvCurrent["ID"]);
                int currentSTT = Convert.ToInt32(drvCurrent["STT"]);
                int otherID = Convert.ToInt32(drvOther["ID"]);
                int otherSTT = Convert.ToInt32(drvOther["STT"]);
                _coThayDoiDuLieu = true;
                // 1. RAM trước -> lưới nhảy dòng ngay lập tức
                DatSTTTrongRam(drvCurrent.Row, otherSTT);
                DatSTTTrongRam(drvOther.Row, currentSTT);
                var view = _dtGoc.DefaultView;
                if (!string.Equals(view.Sort, SapXepMacDinh, StringComparison.OrdinalIgnoreCase))
                    view.Sort = SapXepMacDinh;
                // 2. CSDL ghi nền, tuần tự sau mọi lệnh trước đó; lỗi -> báo + nạp lại
                var viec = XepHangGhiDb(() =>
                {
                    CapNhatSTTCSDL(currentID, otherSTT, otherID, currentSTT);
                    return 0;
                });
                _ = viec.ContinueWith(t => BaoLoiGhiNen(t.Exception?.GetBaseException().Message),
                    CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
                // 3. Đặt lại con trỏ vào dòng vừa di chuyển
                BeginInvoke(new Action(() => DatLaiViTriChonTheoID(currentID)));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi di chuyển dữ liệu {(huong < 0 ? "lên" : "xuống")}:\n{ex.Message}",
                    "Lỗi giao diện", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private static void DatSTTTrongRam(DataRow row, int stt)
        {
            row.BeginEdit();
            foreach (string tenCot in new[] { "STT", "ThuTuSapXep" })
            {
                var cot = row.Table.Columns[tenCot];
                if (cot != null) row[cot] = Convert.ChangeType(stt, cot.DataType);
            }
            row.EndEdit();   // chỉ 1 lần sắp xếp lại thay vì 2
        }
        /// <summary>Chạy NỀN. Ném ngoại lệ khi lỗi (hàng đợi sẽ báo).</summary>
        private void CapNhatSTTCSDL(int id1, int stt1, int id2, int stt2)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                const string query = @"
            UPDATE DanhSach_Nhap
            SET STT = $STT, ThuTuSapXep = $STT
            WHERE ID = $ID;";
                using var cmd = new SqliteCommand(query, conn, trans);
                cmd.Parameters.AddWithValue("$STT", stt1);
                cmd.Parameters.AddWithValue("$ID", id1);
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("$STT", stt2);
                cmd.Parameters.AddWithValue("$ID", id2);
                cmd.ExecuteNonQuery();
                trans.Commit();
            }
            catch
            {
                try { trans.Rollback(); } catch { }
                throw;
            }
        }
        private void DatLaiViTriChonTheoID(int id)
        {
            if (kryptonDataGridView1 == null || kryptonDataGridView1.IsDisposed || kryptonDataGridView1.Rows.Count == 0)
                return;
            int targetIndex = -1;
            for (int i = 0; i < kryptonDataGridView1.Rows.Count; i++)
            {
                if (kryptonDataGridView1.Rows[i].IsNewRow) continue;
                if (kryptonDataGridView1.Rows[i].DataBoundItem is DataRowView drv &&
                    drv.Row.Table.Columns.Contains("ID") &&
                    Convert.ToInt32(drv["ID"]) == id)
                {
                    targetIndex = i;
                    break;
                }
            }
            if (targetIndex < 0) return;
            kryptonDataGridView1.ClearSelection();
            DataGridViewRow row = kryptonDataGridView1.Rows[targetIndex];
            row.Selected = true;
            DataGridViewColumn? firstVisibleColumn = kryptonDataGridView1.Columns
                .Cast<DataGridViewColumn>()
                .FirstOrDefault(c => c.Visible && c.Index >= 0);
            if (firstVisibleColumn == null) return;
            DataGridViewCell cell = row.Cells[firstVisibleColumn.Index];
            if (cell.OwningRow == null || cell.OwningRow.IsNewRow || !firstVisibleColumn.Visible) return;
            kryptonDataGridView1.CurrentCell = cell;
        }
        private void DinhDangDataGridViewHienDai()
        {
            if (_luoiDaDinhDang) return;   // chỉ định dạng đúng 1 lần
            var dgv = kryptonDataGridView1;
            if (dgv == null) return;
            _luoiDaDinhDang = true;
            dgv.SuspendLayout();
            try
            {
                dgv.AutoGenerateColumns = false;
                dgv.AllowUserToAddRows = false;
                dgv.AllowUserToDeleteRows = false;
                dgv.AllowUserToOrderColumns = false;
                dgv.AllowUserToResizeColumns = false;
                dgv.AllowUserToResizeRows = false;
                dgv.ReadOnly = true;
                dgv.RowHeadersVisible = true;
                dgv.RowHeadersWidth = 50;
                dgv.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
                dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgv.MultiSelect = false;
                Font fontChuDung = LayFontChu();
                Font fontTieuDeBold = LayFontTieuDeBold();
                int fontHeight = fontChuDung.Height;
                dgv.Font = fontChuDung;
                dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                dgv.ColumnHeadersHeight = (int)(fontHeight * 3.5);
                dgv.RowTemplate.Height = (int)(fontHeight * 1.8);
                dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
                dgv.EnableHeadersVisualStyles = false;
                dgv.BorderStyle = BorderStyle.None;
                dgv.BackgroundColor = Color.White;
                dgv.GridColor = Color.FromArgb(235, 235, 235);
                dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
                dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
                dgv.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
                var cell = dgv.DefaultCellStyle;
                cell.Font = fontChuDung;
                cell.BackColor = Color.White;
                cell.ForeColor = Color.FromArgb(30, 30, 30);
                cell.SelectionBackColor = Color.FromArgb(232, 244, 253);
                cell.SelectionForeColor = Color.FromArgb(0, 102, 204);
                cell.Padding = new Padding(3, 0, 3, 0);
                cell.WrapMode = DataGridViewTriState.False;
                var head = dgv.ColumnHeadersDefaultCellStyle;
                head.Font = fontTieuDeBold;
                head.BackColor = Color.FromArgb(180, 210, 240);
                head.ForeColor = Color.FromArgb(30, 30, 30);
                head.SelectionBackColor = head.BackColor;
                head.SelectionForeColor = head.ForeColor;
                head.WrapMode = DataGridViewTriState.True;
                head.Alignment = DataGridViewContentAlignment.MiddleCenter;
                var rowHead = dgv.RowHeadersDefaultCellStyle;
                rowHead.BackColor = Color.FromArgb(240, 245, 250);
                rowHead.ForeColor = Color.FromArgb(30, 30, 30);
                rowHead.SelectionBackColor = rowHead.BackColor;
                rowHead.SelectionForeColor = rowHead.ForeColor;
                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                dgv.Columns.Clear();
                var cacCot = new DataGridViewColumn[CotLuoi.Length];
                for (int i = 0; i < CotLuoi.Length; i++)
                {
                    var d = CotLuoi[i];
                    var col = new DataGridViewTextBoxColumn
                    {
                        Name = d.Name,
                        DataPropertyName = d.Name,
                        HeaderText = d.Header,
                        Visible = d.Visible,
                        ReadOnly = true,
                        SortMode = DataGridViewColumnSortMode.NotSortable
                    };
                    if (d.Visible)
                    {
                        if (d.Fill)
                        {
                            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                            col.MinimumWidth = 120;
                        }
                        else
                        {
                            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                            col.Width = d.Width;
                        }
                        if (d.Center) col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }
                    cacCot[i] = col;
                }
                dgv.Columns.AddRange(cacCot);
            }
            finally
            {
                dgv.ResumeLayout();
            }
        }
        private void ChonDongVuaThemTheoSoHieu(string soHieu)
        {
            if (string.IsNullOrWhiteSpace(soHieu) || kryptonDataGridView1.Rows.Count == 0) return;
            string targetSoHieu = soHieu.Trim();
            foreach (DataGridViewRow row in kryptonDataGridView1.Rows)
            {
                var cellValue = row.Cells["SoHieu"]?.Value?.ToString()?.Trim();
                if (string.Equals(cellValue, targetSoHieu, StringComparison.OrdinalIgnoreCase))
                {
                    kryptonDataGridView1.ClearSelection();
                    row.Selected = true;
                    foreach (DataGridViewColumn col in kryptonDataGridView1.Columns)
                    {
                        if (col.Visible)
                        {
                            kryptonDataGridView1.CurrentCell = row.Cells[col.Index];
                            break;
                        }
                    }
                    try { kryptonDataGridView1.FirstDisplayedScrollingRowIndex = row.Index; } catch { }
                    break;
                }
            }
        }
        //  ĐÓNG FORM / ĐỒNG BỘ
        private void kryptonButton1_DongForm_Click(object sender, EventArgs e)
        {
            try
            {
                // 🌟 CHUẨN KỸ SƯ: Tìm lại Form6 đang mở và reset cờ để lần sau bật lên sẽ tự động nạp lại dữ liệu mới nhất
                var form6 = Application.OpenForms.OfType<Form6_XuLyData>().FirstOrDefault();
                if (form6 != null && !form6.IsDisposed)
                {
                    form6.DaLoadDuLieu = false; // Reset cờ để ép buộc form6 nạp lại dữ liệu mới từ CSDL
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi reset cờ Form6: " + ex.Message);
            }
            Module_NhatKy.GhiNhatKy(
                Module_TaiKhoan.TenTaiKhoan_RAM ?? "Admin",
                "Đóng form nhập liệu cán bộ chiến sĩ và tự động đồng bộ dữ liệu từ DanhSach_Nhap vào DanhSach",
                $"Vào lúc {DateTime.Now:HH:mm:ss}");
            this.Close();
        }
        private bool HienThiFormAo_XacNhan(string tieuDe, string noiDung)
        {
            string noiDungChuan = noiDung.Replace("\n", Environment.NewLine);
            bool ketQuaDongY = false;
            using (var formAo = new FormAoBase())
            {
                formAo.Text = "Xác nhận thao tác";
                formAo.Size = new System.Drawing.Size(1000, 500);
                formAo.FormBorderStyle = FormBorderStyle.FixedDialog;
                formAo.MaximizeBox = false; formAo.MinimizeBox = false; formAo.ShowIcon = false;
                formAo.ShowInTaskbar = false;
                var panelTop = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Top, Height = 70, Padding = new Padding(30, 25, 20, 5) };
                panelTop.StateCommon.Color1 = System.Drawing.Color.White;
                var lblTitle = new Krypton.Toolkit.KryptonLabel { Text = tieuDe.ToUpper(), Dock = DockStyle.Fill, AutoSize = false };
                lblTitle.StateCommon.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 13F, System.Drawing.FontStyle.Bold);
                lblTitle.StateCommon.ShortText.Color1 = System.Drawing.Color.FromArgb(0, 82, 155);
                panelTop.Controls.Add(lblTitle);
                var separator = new Label { Height = 1, Dock = DockStyle.Top, BackColor = System.Drawing.Color.FromArgb(200, 220, 240), Margin = new Padding(0, 5, 0, 10) };
                var panelContent = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Fill, Padding = new Padding(25, 15, 30, 20) };
                panelContent.StateCommon.Color1 = System.Drawing.Color.White;
                var picIcon = new PictureBox { Image = System.Drawing.SystemIcons.Question.ToBitmap(), SizeMode = PictureBoxSizeMode.CenterImage, Size = new System.Drawing.Size(50, 50), Location = new System.Drawing.Point(25, 15) };
                var txtContent = new Krypton.Toolkit.KryptonTextBox
                {
                    Text = noiDungChuan,
                    ReadOnly = true,
                    Multiline = true,
                    WordWrap = true,
                    ScrollBars = ScrollBars.Vertical,
                    Location = new System.Drawing.Point(90, 15),
                    Width = formAo.Width - 130,
                    Height = panelContent.Height - 35,
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
                };
                txtContent.StateCommon.Back.Color1 = System.Drawing.Color.White;
                txtContent.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.None;
                txtContent.StateCommon.Content.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 11.5F, System.Drawing.FontStyle.Regular);
                txtContent.StateCommon.Content.Color1 = System.Drawing.Color.FromArgb(40, 40, 40);
                txtContent.StateCommon.Content.Padding = new Padding(0);
                var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 75, BackColor = System.Drawing.Color.WhiteSmoke };
                var btnNo = new Krypton.Toolkit.KryptonButton { Text = "Hủy bỏ", Width = 140, Height = 42, DialogResult = DialogResult.No };
                btnNo.StateCommon.Content.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 10.5F, System.Drawing.FontStyle.Bold);
                btnNo.StateCommon.Border.Rounding = 6;
                btnNo.Click += (s, ev) => ketQuaDongY = false;
                var btnYes = new Krypton.Toolkit.KryptonButton { Text = "Đồng ý", Width = 140, Height = 42, DialogResult = DialogResult.Yes };
                btnYes.StateCommon.Content.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 10.5F, System.Drawing.FontStyle.Bold);
                btnYes.StateCommon.Border.Rounding = 6;
                btnYes.Click += (s, ev) => ketQuaDongY = true;
                int totalWidth = btnYes.Width + 20 + btnNo.Width;
                int startX = (formAo.Width - totalWidth) / 2;
                btnYes.Location = new System.Drawing.Point(startX, 16);
                btnNo.Location = new System.Drawing.Point(startX + btnYes.Width + 20, 16);
                panelBottom.Controls.Add(btnYes); panelBottom.Controls.Add(btnNo);
                panelContent.Controls.Add(picIcon); panelContent.Controls.Add(txtContent); panelContent.Controls.Add(separator);
                txtContent.BringToFront(); picIcon.BringToFront(); separator.SendToBack();
                formAo.Controls.Add(panelContent); formAo.Controls.Add(panelTop); formAo.Controls.Add(panelBottom);
                formAo.AcceptButton = btnYes; formAo.CancelButton = btnNo;
                formAo.Shown += (s, ev) => btnNo.Focus();
                formAo.ShowDialog(this);
            }
            return ketQuaDongY;
        }
        /// <summary>Form ảo hiển thị lỗi hệ thống (đỏ thẫm) kèm nút Copy lỗi.</summary>
        private void HienThiFormAo_Loi(string tieuDe, string noiDungLoi)
        {
            string noiDungChuan = noiDungLoi.Replace("\n", Environment.NewLine);
            using (var formAo = new FormAoBase())
            {
                formAo.Text = "Hệ thống ghi nhận sự cố";
                formAo.Size = new System.Drawing.Size(950, 560);
                formAo.FormBorderStyle = FormBorderStyle.FixedDialog;
                formAo.MaximizeBox = false; formAo.MinimizeBox = false; formAo.ShowIcon = false;
                formAo.ShowInTaskbar = false;
                var panelTop = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(25, 20, 20, 5) };
                panelTop.StateCommon.Color1 = System.Drawing.Color.White;
                var lblTitle = new Krypton.Toolkit.KryptonLabel { Text = tieuDe.ToUpper(), Dock = DockStyle.Fill, AutoSize = false };
                lblTitle.StateCommon.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 12F, System.Drawing.FontStyle.Bold);
                lblTitle.StateCommon.ShortText.Color1 = System.Drawing.Color.FromArgb(198, 40, 40);
                panelTop.Controls.Add(lblTitle);
                var separator = new Label { Height = 1, Dock = DockStyle.Top, BackColor = System.Drawing.Color.FromArgb(240, 200, 200), Margin = new Padding(0, 5, 0, 10) };
                var panelContent = new Krypton.Toolkit.KryptonPanel { Dock = DockStyle.Fill, Padding = new Padding(20, 15, 25, 20) };
                panelContent.StateCommon.Color1 = System.Drawing.Color.White;
                var picIcon = new PictureBox { Image = System.Drawing.SystemIcons.Error.ToBitmap(), SizeMode = PictureBoxSizeMode.CenterImage, Size = new System.Drawing.Size(50, 50), Location = new System.Drawing.Point(20, 15) };
                var txtContent = new Krypton.Toolkit.KryptonTextBox
                {
                    Text = noiDungChuan,
                    ReadOnly = true,
                    Multiline = true,
                    WordWrap = true,
                    ScrollBars = ScrollBars.Vertical,
                    Location = new System.Drawing.Point(80, 15),
                    Width = formAo.Width - 110,
                    Height = panelContent.Height - 35,
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
                };
                txtContent.StateCommon.Back.Color1 = System.Drawing.Color.White;
                txtContent.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.None;
                txtContent.StateCommon.Content.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 10.5F, System.Drawing.FontStyle.Regular);
                txtContent.StateCommon.Content.Color1 = System.Drawing.Color.FromArgb(40, 40, 40);
                txtContent.StateCommon.Content.Padding = new Padding(0);
                var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = System.Drawing.Color.WhiteSmoke };
                var btnCopy = new Krypton.Toolkit.KryptonButton { Text = "Sao chép mã lỗi", Width = 160, Height = 35 };
                btnCopy.StateCommon.Content.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 9.5F, System.Drawing.FontStyle.Bold);
                btnCopy.Click += (s, ev) => { try { Clipboard.SetText(noiDungChuan); MessageBox.Show(formAo, "Đã sao chép mã lỗi.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information); } catch { } };
                var btnClose = new Krypton.Toolkit.KryptonButton { Text = "Đóng", Width = 100, Height = 35, DialogResult = DialogResult.OK };
                btnClose.StateCommon.Content.ShortText.Font = new System.Drawing.Font(Module_HeThong.TenFontHeThong, 9.5F, System.Drawing.FontStyle.Bold);
                int totalWidth = btnCopy.Width + 15 + btnClose.Width;
                int startX = (formAo.Width - totalWidth) / 2;
                btnCopy.Location = new System.Drawing.Point(startX, 12);
                btnClose.Location = new System.Drawing.Point(startX + btnCopy.Width + 15, 12);
                panelBottom.Controls.Add(btnCopy); panelBottom.Controls.Add(btnClose);
                panelContent.Controls.Add(picIcon); panelContent.Controls.Add(txtContent); panelContent.Controls.Add(separator);
                txtContent.BringToFront(); picIcon.BringToFront(); separator.SendToBack();
                formAo.Controls.Add(panelContent); formAo.Controls.Add(panelTop); formAo.Controls.Add(panelBottom);
                formAo.AcceptButton = btnClose; formAo.CancelButton = btnClose;
                formAo.Shown += (s, ev) => btnClose.Focus();
                Debug.WriteLine($"[ERR_UI] {tieuDe} - {noiDungChuan}");
                formAo.ShowDialog(this);
            }
        }
        private string MaHoaAnToan(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;
            try { return Module_BaoMatAES.MaHoa(plainText); }
            catch { return plainText; }
        }
        private string GiaiMaAnToan(object cipherObject)
        {
            if (cipherObject == null || cipherObject == DBNull.Value) return string.Empty;
            string cipherText = cipherObject.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;
            try { return Module_BaoMatAES.GiaiMa(cipherText); }
            catch { return cipherText; }   // dữ liệu thô chưa mã hóa
        }
        /// <summary>
        /// Đồng bộ ngược từ DanhSach_Nhap sang DanhSach gốc:
        /// - Đối chiếu theo Số hiệu: đã có thì cập nhật STT/ThuTuSapXep, chưa có thì thêm mới (mã hóa AES).
        /// </summary>
        private void xoaTimKiem_ToolStripMenuItem_Click(object sender, EventArgs e)
            => kryptonButton_LamMoiCacOTimKiem.PerformClick();
        private void lamMoiTrang_ToolStripMenuItem_Click(object sender, EventArgs e)
            => kryptonButton_RefershCSDL.PerformClick();
        private void diChuyenLen_ToolStripMenuItem_Click(object sender, EventArgs e)
            => kryptonButton1_DiChuyenDataCBCSLen.PerformClick();
        private void diChuyenXuong_ToolStripMenuItem_Click(object sender, EventArgs e)
            => kryptonButton1_DiChuyenDataCBCSXuong.PerformClick();
        private void dongFrom_ToolStripMenuItem_Click(object sender, EventArgs e)
            => kryptonButton1_DongForm.PerformClick();
        private void xuatTepMau_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_dangXuLyHuongDan || !_daNapXong || _dtGoc == null) return;
            _dangXuLyHuongDan = true;
            try
            {
                string msg = "QUY TRÌNH THÊM DỮ LIỆU TỪ TỆP EXCEL:\n\n" +
                             "  1. Xuất tệp Excel mẫu từ hệ thống.\n" +
                             "  2. Nhập thông tin vào tệp Excel vừa xuất.\n" +
                             "  3. Tải tệp đó ngược lại vào phần mềm.\n\n" +
                             "💡 Mẹo thao tác: Nhấn chuột phải vào lưới danh sách → Chọn \"Nhập kết quả phân loại từ đơn vị\".\n\n" +
                             $" {Module_HeThong.Tu_dong_chi} có muốn bắt đầu bằng việc xuất tệp Excel mẫu ngay bây giờ không?";
                if (!HienThiFormAo_XacNhan("HƯỚNG DẪN THÊM DỮ LIỆU", msg)) return;
                Module_XuatNhapDuLieuThiDua.ThucThiXuatExcel(this, true, _dtGoc);
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Thêm dữ liệu cán bộ", $"Vào lúc {DateTime.Now:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                HienThiFormAo_Loi("LỖI HỆ THỐNG", $"Đã xảy ra sự cố trong quá trình hướng dẫn:\n{ex.Message}");
            }
            finally
            {
                _dangXuLyHuongDan = false;
            }
        }
        private bool _tuDongNhapExcelSauKhiDongForm;
        private void nhapDuLieuTuTepExcel_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _tuDongNhapExcelSauKhiDongForm = true;
            kryptonButton1_DongForm.PerformClick();
        }
        // [2] THAY THẾ NGUYÊN HÀM: DongBoDuLieuSangDanhSachGoc
        //     Khác bản cũ đúng 2 chỗ (có đánh dấu ★):
        //     ★ Khóa Số hiệu được Trim ở cả 2 phía (cũ: chỉ Trim phía DanhSach_Nhap -> lệch 1 khoảng trắng là
        //       bị coi là "người mới" -> chèn trùng, STT cũ giữ nguyên -> thứ tự sai và lưu vào CSDL)
        //     ★ Cuối transaction đánh lại STT liên tục 1..n (STT ASC, ID ASC) -> không bao giờ còn STT trùng/hở
        // -------------------------------------------------------------------------------------
        private bool DongBoDuLieuSangDanhSachGoc()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            try
            {
                HashSet<string> cotDanhSach = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var cmd = new SqliteCommand("PRAGMA table_info(DanhSach);", conn, transaction))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string? tenCot = reader["name"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(tenCot)) cotDanhSach.Add(tenCot);
                    }
                }
                if (cotDanhSach.Count == 0)
                    throw new InvalidOperationException("Không tìm thấy bảng DanhSach hoặc bảng DanhSach không có cột.");
                if (!cotDanhSach.Contains("ThuTuSapXep"))
                {
                    using (var cmdAdd = new SqliteCommand("ALTER TABLE DanhSach ADD COLUMN ThuTuSapXep INTEGER NOT NULL DEFAULT 0;", conn, transaction))
                        cmdAdd.ExecuteNonQuery();
                    cotDanhSach.Add("ThuTuSapXep");
                }
                DataTable dtNhap = new DataTable();
                using (var cmd = new SqliteCommand("SELECT * FROM DanhSach_Nhap ORDER BY STT ASC;", conn, transaction))
                using (var reader = cmd.ExecuteReader())
                {
                    dtNhap.Load(reader);
                }
                if (dtNhap.Rows.Count == 0)
                {
                    transaction.Commit();
                    _ketQuaDongBo = $"Đã nạp {dtNhap.Rows.Count} CBCS từ DanhSach_Nhap vào DanhSach";
                    return true;
                }
                HashSet<string> cotDanhSachNhap = new HashSet<string>(
                    dtNhap.Columns.Cast<DataColumn>().Select(c => c.ColumnName),
                    StringComparer.OrdinalIgnoreCase);
                List<string> danhSachCotDongBo = cotDanhSach
                    .Where(c => !c.Equals("ID", StringComparison.OrdinalIgnoreCase) && cotDanhSachNhap.Contains(c))
                    .ToList();
                Dictionary<string, long> mapSoHieuToIdGoc = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                if (cotDanhSach.Contains("ID") && cotDanhSach.Contains("SoHieu"))
                {
                    using var cmdSelectGoc = new SqliteCommand("SELECT ID, SoHieu FROM DanhSach;", conn, transaction);
                    using var readerGoc = cmdSelectGoc.ExecuteReader();
                    while (readerGoc.Read())
                    {
                        if (readerGoc["ID"] == DBNull.Value || readerGoc["SoHieu"] == DBNull.Value) continue;
                        long id = Convert.ToInt64(readerGoc["ID"]);
                        // ★ Trim khóa phía DanhSach
                        string soHieuGoc = GiaiMaAnToan(readerGoc["SoHieu"]?.ToString() ?? string.Empty).Trim();
                        if (!string.IsNullOrWhiteSpace(soHieuGoc) && !mapSoHieuToIdGoc.ContainsKey(soHieuGoc))
                            mapSoHieuToIdGoc[soHieuGoc] = id;
                    }
                }
                const string updateSql = @"
UPDATE DanhSach
SET STT = $STT,
    ThuTuSapXep = $STT
WHERE ID = $ID;";
                string insertColumns = string.Join(", ", danhSachCotDongBo.Select(c => $"\"{c}\""));
                string insertValues = string.Join(", ", danhSachCotDongBo.Select(c => $"${c}"));
                string insertSql = $"INSERT INTO DanhSach ({insertColumns}) VALUES ({insertValues});";
                using var cmdUpdate = new SqliteCommand(updateSql, conn, transaction);
                cmdUpdate.Parameters.Add("$STT", SqliteType.Integer);
                cmdUpdate.Parameters.Add("$ID", SqliteType.Integer);
                using var cmdInsert = new SqliteCommand(insertSql, conn, transaction);
                foreach (string tenCot in danhSachCotDongBo)
                    cmdInsert.Parameters.Add($"${tenCot}", SqliteType.Text);
                foreach (DataRow rowNhap in dtNhap.Rows)
                {
                    string soHieuPlain = rowNhap.Table.Columns.Contains("SoHieu") && rowNhap["SoHieu"] != DBNull.Value
                        ? rowNhap["SoHieu"]?.ToString()?.Trim() ?? string.Empty
                        : string.Empty;
                    int sttThucTe = 0;
                    if (rowNhap.Table.Columns.Contains("STT") && rowNhap["STT"] != DBNull.Value)
                        int.TryParse(rowNhap["STT"].ToString(), out sttThucTe);
                    long existingId = 0;
                    bool daTonTai = !string.IsNullOrWhiteSpace(soHieuPlain) && mapSoHieuToIdGoc.TryGetValue(soHieuPlain, out existingId);
                    if (daTonTai)
                    {
                        cmdUpdate.Parameters["$STT"].Value = sttThucTe;
                        cmdUpdate.Parameters["$ID"].Value = existingId;
                        cmdUpdate.ExecuteNonQuery();
                    }
                    else
                    {
                        foreach (string tenCot in danhSachCotDongBo)
                        {
                            object giaTri = DBNull.Value;
                            if (rowNhap.Table.Columns.Contains(tenCot) && rowNhap[tenCot] != DBNull.Value)
                            {
                                if (tenCot.Equals("STT", StringComparison.OrdinalIgnoreCase) ||
                                    tenCot.Equals("ThuTuSapXep", StringComparison.OrdinalIgnoreCase))
                                {
                                    giaTri = sttThucTe;
                                }
                                else
                                {
                                    giaTri = MaHoaAnToan(rowNhap[tenCot]?.ToString() ?? string.Empty);
                                }
                            }
                            cmdInsert.Parameters[$"${tenCot}"].Value = giaTri;
                        }
                        cmdInsert.ExecuteNonQuery();
                    }
                }
                // ★ Đánh lại STT liên tục trong CÙNG transaction (an toàn nếu còn dòng lệch/trùng STT từ trước)
                const string chuanHoaSttSql = @"
WITH Ranked AS (
    SELECT ID, ROW_NUMBER() OVER (ORDER BY STT ASC, ID ASC) AS NewSTT
    FROM DanhSach
)
UPDATE DanhSach
SET STT = (SELECT NewSTT FROM Ranked WHERE Ranked.ID = DanhSach.ID),
    ThuTuSapXep = (SELECT NewSTT FROM Ranked WHERE Ranked.ID = DanhSach.ID);";
                using (var cmdChuanHoa = new SqliteCommand(chuanHoaSttSql, conn, transaction))
                    cmdChuanHoa.ExecuteNonQuery();
                transaction.Commit();
                _ketQuaDongBo = $"Đã nạp {dtNhap.Rows.Count} CBCS từ DanhSach_Nhap vào DanhSach";
                return true;
            }
            catch
            {
                try { transaction.Rollback(); } catch { }
                throw;
            }
        }
        // -------------------------------------------------------------------------------------
        // [1] THAY THẾ NGUYÊN HÀM: Form60_NhapLieuCBCS_FormClosed
        //     - Ghi DanhSach_Nhap -> DanhSach chạy NỀN (không đứng UI)
        //     - Luôn xóa DataCache sau khi ghi (trước đây không xóa -> Form6 nạp lại bản cache cũ)
        //     - Chỉ MỘT nơi nạp lại Form6 (trước đây Form60 và Form6 cùng nạp, chạy chồng nhau)
        // -------------------------------------------------------------------------------------
        private async void Form60_NhapLieuCBCS_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_timerLoc != null)
            {
                _timerLoc.Stop();
                _timerLoc.Dispose();
                _timerLoc = null;
            }
            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            if (formCha != null) formCha.CapNhatTieuDe("Trang phân loại thi đua");
            bool yeuCauNhapExcel = _tuDongNhapExcelSauKhiDongForm;
            _tuDongNhapExcelSauKhiDongForm = false;
            // 1. Đồng bộ DanhSach_Nhap -> DanhSach (nền, một transaction). Đợi hàng đợi ghi nền xong trước.
            if (_coThayDoiDuLieu && !_bangNhapHong)
            {
                Task chuoiGhi = _chuoiGhiDb;
                try
                {
                    await Task.Run(() =>
                    {
                        try { chuoiGhi.GetAwaiter().GetResult(); } catch { }
                        DongBoDuLieuSangDanhSachGoc();
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Lỗi tự động đồng bộ khi đóng form: {ex.Message}",
                        "Cảnh báo CSDL",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            // 2. Nạp lại Form6 đúng 1 lần (xóa cache + nạp + thống kê + biểu đồ Form4)
            var frm6 = TimForm6DangMo();
            if (frm6 != null)
            {
                try
                {
                    frm6.DaLoadDuLieu = false;
                    await frm6.NapLaiSauKhiForm60DongAsync();
                    frm6.DaLoadDuLieu = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Lỗi reload Form6 từ Form60: " + ex.Message);
                }
            }
            else
            {
                DataCache.Clear(); // không có Form6 thì vẫn phải bỏ cache cũ cho các form khác
            }
            // 3. Có yêu cầu nhập Excel -> làm sau khi Form6 đã nạp xong (không cần Task.Delay đoán giờ nữa)
            if (yeuCauNhapExcel && frm6 != null && !frm6.IsDisposed && frm6.IsHandleCreated)
            {
                try
                {
                    frm6.YeuCauNhapDuLieuTuFileExcel();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Lỗi yêu cầu Form6 nhập Excel: " + ex);
                }
            }
        }
        //  NHÃN THEO PHIÊN BẢN
        private void CapNhatLabelTheoPhienBan() => ApDungNhanTheoPhienBan(KiemTraLaTanBinh());
        private void ApDungNhanTheoPhienBan(bool laTanBinh)
        {
            try
            {
                string doiTuongThuong = laTanBinh ? "tân binh" : "CBCS";
                groupBox1_TimKiemThongTinCBCS.Text = $"1. Tìm kiếm thông tin {doiTuongThuong}";
                groupBox3_ThongTinCBCS.Text = $"3. Nhập dữ liệu để thêm mới thông tin {doiTuongThuong}";
                groupBox1.Text = $"4. Danh sách thông tin {doiTuongThuong}";
                textBox_SoHieu.ReadOnly = false;
                textBox_SoHieu.Enabled = true;
                Color mauNen = laTanBinh ? Color.FromArgb(198, 239, 206) : Color.White;
                if (textBox_SoHieu is Krypton.Toolkit.KryptonTextBox kSoHieu)
                    kSoHieu.StateCommon.Back.Color1 = mauNen;
                else
                    textBox_SoHieu.BackColor = mauNen;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi cập nhật giao diện theo phiên bản:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //  CLICK DÒNG -> ĐỔ LÊN Ô NHẬP
        private void KryptonDataGridView1_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow row = kryptonDataGridView1.Rows[e.RowIndex];
            if (row.DataBoundItem is not DataRowView drv) return;
            var dto = new CanBoDTO
            {
                ID = drv["ID"] != DBNull.Value ? Convert.ToInt32(drv["ID"]) : 0,
                HoVaTen = drv["HoVaTen"]?.ToString() ?? "",
                SoHieu = drv["SoHieu"]?.ToString() ?? "",
                NamSinh = drv["NamSinh"]?.ToString() ?? "",
                QueQuan = drv["QueQuan"]?.ToString() ?? "",
                NgayVaoCAND = drv["NgayVaoCAND"]?.ToString() ?? "",
                CapBac = drv["CapBac"]?.ToString() ?? "",
                ChucVu = drv["ChucVu"]?.ToString() ?? "",
                DonVi = drv["DonVi"]?.ToString() ?? "",
                PhanLoai = drv["PhanLoai"]?.ToString() ?? "",
                GhiChu = drv["GhiChu"]?.ToString() ?? ""
            };
            _currentSelectedID = dto.ID;
            textBox_HoVaTen.Text = dto.HoVaTen;
            textBox_SoHieu.Text = dto.SoHieu;
            textBox_NamSinh.Text = dto.NamSinh;
            textBox_QueQuan.Text = dto.QueQuan;
            textBox_NgayVaoCAND.Text = dto.NgayVaoCAND;
            textBox_CapBac.Text = dto.CapBac;
            textBox_GhiChu.Text = dto.GhiChu;
            SetComboBoxValue(comboBox_ChucVu, dto.ChucVu);
            SetComboBoxValue(comboBox_DonVi, dto.DonVi);
            SetComboBoxValue(comboBox_PhanLoai, dto.PhanLoai);
            // Tân binh: khóa ô Số hiệu để bảo vệ mã ID cấp phát
            textBox_SoHieu.ReadOnly = KiemTraLaTanBinh();
        }
        //  THÊM MỚI
        private sealed class KetQuaThem
        {
            public string? CanhBao;
            public bool FocusHoTen;
        }
        private async void btnThemMoi_Click(object sender, EventArgs e)
        {
            if (!_daNapXong || _dangThem || _dtGoc == null) return;
            bool laTanBinh = KiemTraLaTanBinh();
            // Tân binh: LUÔN cấp ID mới (dò trên RAM, tức thì)
            if (laTanBinh) textBox_SoHieu.Text = SinhSoHieuTanBinhTiepTheo();
            var dto = LayDuLieuTuForm();
            if (string.IsNullOrWhiteSpace(dto.HoVaTen))
            {
                MessageBox.Show($"Vui lòng nhập Họ và tên {(laTanBinh ? "tân binh" : "CBCS")} cần thêm mới!",
                                "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox_HoVaTen.Focus();
                return;
            }
            if (!laTanBinh && string.IsNullOrWhiteSpace(dto.SoHieu))
            {
                MessageBox.Show("Vui lòng nhập Số hiệu CAND!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox_SoHieu.Focus();
                return;
            }
            _dangThem = true;
            try
            {
                toolStripStatusLabel1.Text = "Đang thêm dữ liệu...";
                // Toàn bộ thao tác CSDL chạy NỀN trong hàng đợi -> giao diện không đứng
                KetQuaThem kq = await XepHangGhiDb(() => ThemMoiTrongCSDL(dto, laTanBinh));
                if (IsDisposed || Disposing) return;
                if (kq.CanhBao != null)
                {
                    MessageBox.Show(kq.CanhBao, "Trùng dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    if (kq.FocusHoTen) textBox_HoVaTen.Focus(); else textBox_SoHieu.Focus();
                    CapNhatThanhTrangThaiStatusStrip(_dtGoc?.DefaultView.Count ?? 0);
                    return;
                }
                _coThayDoiDuLieu = true;
                await LoadDuLieuLenGridAsync();
                if (IsDisposed || Disposing) return;
                if (!laTanBinh)
                {
                    ChonDongVuaThemTheoSoHieu(dto.SoHieu);
                }
                else if (_dtGoc != null && _dtGoc.Rows.Count > 0)
                {
                    long maxId = _dtGoc.AsEnumerable().Max(r => Convert.ToInt64(r["ID"]));
                    DatLaiViTriChonTheoID((int)maxId);
                }
                XoaTrangCacControl();
                string noiDungThongBao = $"Thêm dữ liệu {(laTanBinh ? "tân binh" : "cán bộ")} thành công: {dto.HoVaTen}";
                toolStripStatusLabel1.Text = noiDungThongBao;
                Module_NhatKy.GhiNhatKy(
                    Module_TaiKhoan.TenTaiKhoan_RAM ?? "Admin",
                    $"Thêm dữ liệu {(laTanBinh ? "tân binh" : "cán bộ")}",
                    $"{noiDungThongBao} lúc {DateTime.Now:HH:mm:ss}");
                await Task.Delay(1500);
                if (IsDisposed || Disposing) return;
                CapNhatThanhTrangThaiStatusStrip(_dtGoc?.DefaultView.Count ?? 0);
            }
            catch (Exception ex)
            {
                Module_NhatKy.GhiNhatKy(
                    Module_TaiKhoan.TenTaiKhoan_RAM ?? "Admin",
                    $"Thêm dữ liệu {(laTanBinh ? "tân binh" : "cán bộ chiến sĩ")} bị lỗi: " + ex.Message,
                    $"Vào lúc {DateTime.Now:HH:mm:ss}");
                MessageBox.Show($"Lỗi khi lưu dữ liệu: {ex.Message}", "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _dangThem = false;
            }
        }
        /// <summary>Chạy NỀN. Kiểm tra trùng -> dồn STT -> chèn -> đánh lại STT liên tục (một transaction).</summary>
        private KetQuaThem ThemMoiTrongCSDL(CanBoDTO dto, bool laTanBinh)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            if (!laTanBinh)
            {
                const string checkQuery = @"
                SELECT COUNT(1) 
                FROM DanhSach_Nhap 
                WHERE TRIM(SoHieu) = TRIM($SoHieu);";
                using var checkCmd = new SqliteCommand(checkQuery, conn, transaction);
                checkCmd.Parameters.AddWithValue("$SoHieu", dto.SoHieu.Trim());
                if (Convert.ToInt64(checkCmd.ExecuteScalar()) > 0)
                    return new KetQuaThem { CanhBao = $"Số hiệu CAND [{dto.SoHieu}] đã tồn tại!" };
            }
            else
            {
                const string checkTBQuery = @"
                SELECT COUNT(1) 
                FROM DanhSach_Nhap 
                WHERE TRIM(HoVaTen) = TRIM($HoVaTen) 
                  AND TRIM(NamSinh) = TRIM($NamSinh) 
                  AND TRIM(DonVi) = TRIM($DonVi);";
                using var checkTBCmd = new SqliteCommand(checkTBQuery, conn, transaction);
                checkTBCmd.Parameters.AddWithValue("$HoVaTen", dto.HoVaTen.Trim());
                checkTBCmd.Parameters.AddWithValue("$NamSinh", (dto.NamSinh ?? "").Trim());
                checkTBCmd.Parameters.AddWithValue("$DonVi", (dto.DonVi ?? "").Trim());
                if (Convert.ToInt64(checkTBCmd.ExecuteScalar()) > 0)
                    return new KetQuaThem
                    {
                        CanhBao = $"Tân binh [{dto.HoVaTen}] thuộc đơn vị [{dto.DonVi}] đã tồn tại trong danh sách!",
                        FocusHoTen = true
                    };
            }
            // STT lớn nhất của đơn vị đó (+1); đơn vị chưa có ai -> cuối danh sách
            string tenDonVi = (dto.DonVi ?? "").Trim();
            int targetSTT;
            const string maxSttDonViQuery = @"
            SELECT MAX(STT) 
            FROM DanhSach_Nhap 
            WHERE TRIM(DonVi) = TRIM($DonVi);";
            using (var maxCmd = new SqliteCommand(maxSttDonViQuery, conn, transaction))
            {
                maxCmd.Parameters.AddWithValue("$DonVi", tenDonVi);
                object? result = maxCmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    targetSTT = Convert.ToInt32(result) + 1;
                }
                else
                {
                    using var maxAllCmd = new SqliteCommand("SELECT IFNULL(MAX(STT), 0) + 1 FROM DanhSach_Nhap;", conn, transaction);
                    targetSTT = Convert.ToInt32(maxAllCmd.ExecuteScalar());
                }
            }
            const string shiftQuery = @"
            UPDATE DanhSach_Nhap 
            SET STT = STT + 1, 
                ThuTuSapXep = ThuTuSapXep + 1 
            WHERE STT >= $TargetSTT;";
            using (var shiftCmd = new SqliteCommand(shiftQuery, conn, transaction))
            {
                shiftCmd.Parameters.AddWithValue("$TargetSTT", targetSTT);
                shiftCmd.ExecuteNonQuery();
            }
            const string insertQuery = @"
            INSERT INTO DanhSach_Nhap (
                STT, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND,
                CapBac, ChucVu, DonVi, PhanLoai, GhiChu, ThuTuSapXep, ThoiGianTao
            ) 
            VALUES (
                $STT, $HoVaTen, $SoHieu, $NamSinh, $QueQuan, $NgayVaoCAND,
                $CapBac, $ChucVu, $DonVi, $PhanLoai, $GhiChu, $STT, $ThoiGianTao
            );";
            using (var cmd = new SqliteCommand(insertQuery, conn, transaction))
            {
                cmd.Parameters.AddWithValue("$STT", targetSTT);
                cmd.Parameters.AddWithValue("$HoVaTen", dto.HoVaTen ?? "");
                cmd.Parameters.AddWithValue("$SoHieu", dto.SoHieu ?? "");
                cmd.Parameters.AddWithValue("$NamSinh", dto.NamSinh ?? "");
                cmd.Parameters.AddWithValue("$QueQuan", dto.QueQuan ?? "");
                cmd.Parameters.AddWithValue("$NgayVaoCAND", dto.NgayVaoCAND ?? "");
                cmd.Parameters.AddWithValue("$CapBac", dto.CapBac ?? "");
                cmd.Parameters.AddWithValue("$ChucVu", dto.ChucVu ?? "");
                cmd.Parameters.AddWithValue("$DonVi", dto.DonVi ?? "");
                cmd.Parameters.AddWithValue("$PhanLoai", dto.PhanLoai ?? "");
                cmd.Parameters.AddWithValue("$GhiChu", dto.GhiChu ?? "");
                cmd.Parameters.AddWithValue("$ThoiGianTao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.ExecuteNonQuery();
            }
            const string reindexSttQuery = @"
            WITH Ranked AS (
                SELECT ROWID, ROW_NUMBER() OVER (ORDER BY STT ASC) AS NewSTT 
                FROM DanhSach_Nhap
            )
            UPDATE DanhSach_Nhap 
            SET STT = (SELECT NewSTT FROM Ranked WHERE Ranked.ROWID = DanhSach_Nhap.ROWID), 
                ThuTuSapXep = (SELECT NewSTT FROM Ranked WHERE Ranked.ROWID = DanhSach_Nhap.ROWID);";
            using (var reindexCmd = new SqliteCommand(reindexSttQuery, conn, transaction))
            {
                reindexCmd.ExecuteNonQuery();
            }
            transaction.Commit();
            return new KetQuaThem();
        }
        // [3] HÀM MỚI (thêm vào class Form60_NhapLieuCBCS): tìm đúng Form6 đang dùng
        // -------------------------------------------------------------------------------------
        private static Form6_XuLyData? TimForm6DangMo()
        {
            var f = Form6_XuLyData.Instance;
            if (f != null && !f.IsDisposed) return f;
            return Application.OpenForms.OfType<Form6_XuLyData>().FirstOrDefault(x => !x.IsDisposed);
        }
    } //Ngoài luồng
    /// <summary>DataGridView thường đã bật double-buffer: vẽ/cuộn nhanh hơn nhiều so với KryptonDataGridView.</summary>
    public class LuoiDuLieuNhe : DataGridView
    {
        public LuoiDuLieuNhe()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }
    }
    public class CanBoDTO
    {
        public int ID { get; set; }
        public int STT { get; set; }
        public string HoVaTen { get; set; } = string.Empty;
        public string SoHieu { get; set; } = string.Empty;
        public string NamSinh { get; set; } = string.Empty;
        public string QueQuan { get; set; } = string.Empty;
        public string NgayVaoCAND { get; set; } = string.Empty;
        public string CapBac { get; set; } = string.Empty;
        public string ChucVu { get; set; } = string.Empty;
        public string DonVi { get; set; } = string.Empty;
        public string PhanLoai { get; set; } = string.Empty;
        public string GhiChu { get; set; } = string.Empty;
        public int ThuTuSapXep { get; set; }
    }
}