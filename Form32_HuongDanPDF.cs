///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using PdfiumViewer;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form32_HuongDanPDF : Form
    {
        // ====== CẤU HÌNH ======
        /// <summary>
        /// Form bị ẩn quá N phút thì giải phóng tài liệu PDF khỏi RAM (hiện lại sẽ tự nạp lại đúng trang đang xem).
        /// 0 = tắt, luôn giữ trong RAM.
        /// </summary>
        private const int TuGiaiPhongSauPhut = 5;
        private PdfViewer pdfViewer = null!;
        private readonly Label _lblDangTai;
        // Font tạo lười (lần đầu cần mới tạo, trên luồng UI) để gọi static DamBaoPdfium() từ luồng nền
        // không kéo theo việc khởi tạo Font / Module_HeThong.
        private static Font? _fontDangTai;
        private static Font LayFontDangTai()
            => _fontDangTai ??= new Font(Module_HeThong.TenFontHeThong, 12F, FontStyle.Italic);
        // ====== Nạp thư viện PDFium (dùng chung toàn app, thread-safe) ======
        private static readonly object _khoaPdfium = new();
        private static bool _daDangKyPdfium = false;
        private static IntPtr _pdfiumHandle = IntPtr.Zero;
        private static string? _loiPdfium;
        // ====== Trạng thái tài liệu đang mở ======
        private int _loadVersion = 0;               // tăng mỗi lần nạp/đóng -> hủy các lần nạp cũ đang chạy nền
        private string? _pathDangMo;
        private long _kichThuocDangMo;
        private DateTime _ngaySuaDangMo;
        private Task<bool>? _taskDangNap;           // lần nạp đang chạy (gộp các lần gọi trùng cùng 1 tệp)
        private string? _pathDangNap;
        private string? _pathNho;                   // tệp nạp thành công gần nhất (để tự nạp lại sau khi giải phóng RAM)
        private int _trangKhoiPhuc;                 // trang đang xem lúc giải phóng RAM
        private System.Windows.Forms.Timer? _timerGiaiPhong;
        public Form32_HuongDanPDF()
        {
            InitializeComponent();
            pdfViewer = new PdfViewer { Dock = DockStyle.Fill, ShowToolbar = true };
            Controls.Add(pdfViewer);
            // Nhãn "Đang tải..." phủ lên viewer trong lúc PDF nạp ở luồng nền -> form hiện ra NGAY, không khựng
            _lblDangTai = new Label
            {
                Text = "Đang tải tài liệu hướng dẫn, vui lòng đợi trong giây lát...",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = LayFontDangTai(),
                ForeColor = Color.DimGray,
                BackColor = Color.White,
                Visible = false
            };
            Controls.Add(_lblDangTai);
            _lblDangTai.BringToFront();
            FormClosed += (s, e) =>
            {
                _timerGiaiPhong?.Stop();
                _timerGiaiPhong?.Dispose();
                _timerGiaiPhong = null;
                DisposePdf();
            };
        }
        /// <summary>
        /// Nạp trước thư viện PDFium (DLL ~vài chục MB, lần nạp đầu là chỗ chậm nhất). Gọi từ luồng nền khi rảnh.
        /// An toàn gọi nhiều lần / từ nhiều luồng.
        /// </summary>
        public static bool DamBaoPdfium()
        {
            lock (_khoaPdfium)
            {
                if (_daDangKyPdfium) return true;
                IntPtr daNap = IntPtr.Zero;
                try
                {
                    // Phần mềm chỉ phát hành x64
                    string pdfPath = Path.Combine(Module_DanduongGPS.ThuMucHuongDan, "pdfium-x64.dll");
                    if (!File.Exists(pdfPath))
                    {
                        _loiPdfium = $"Lỗi bộ cài: Không tìm thấy thư viện PDFium:\n\n{pdfPath}\n\n" +
                                     "Vui lòng kiểm tra lại thư mục HuongDanSuDung.";
                        return false;
                    }
                    // Nạp DLL ngay (nặng nhất) rồi mới đăng ký resolver trả về chính handle này
                    daNap = NativeLibrary.Load(pdfPath);
                    IntPtr handle = daNap;
                    NativeLibrary.SetDllImportResolver(
                        typeof(PdfDocument).Assembly,
                        (name, assembly, path) =>
                            string.Equals(name, "pdfium.dll", StringComparison.OrdinalIgnoreCase) ? handle : IntPtr.Zero);
                    _pdfiumHandle = handle;
                    _daDangKyPdfium = true;
                    _loiPdfium = null;
                    return true;
                }
                catch (Exception ex)
                {
                    _loiPdfium = $"Lỗi nạp thư viện PDFium:\n\n{ex.Message}";
                    // Nạp DLL được mà đăng ký resolver lỗi: trả handle lại, không để rò
                    if (daNap != IntPtr.Zero && !_daDangKyPdfium)
                    {
                        try { NativeLibrary.Free(daNap); } catch { }
                    }
                    return false;
                }
            }
        }
        /// <summary>Nạp sẵn PDFium ở nền, im lặng (Form2 gọi bằng Task.Run sau khi form chính nạp xong).</summary>
        public static void NapTruocThuVien()
        {
            try { DamBaoPdfium(); }
            catch (Exception ex) { Debug.WriteLine("[NapTruocThuVien] " + ex.Message); }
        }
        /// <summary>
        /// Nạp PDF: DLL + parse tài liệu chạy ở luồng nền, UI luôn mượt. Bỏ qua nếu đang mở đúng tệp này (chưa đổi).
        /// Gọi trùng cùng 1 tệp khi đang nạp thì dùng chung 1 lần nạp (không parse 2 lần).
        /// </summary>
        public async Task<bool> LoadPdfAsync(string path)
        {
            if (IsDisposed) return false;
            if (!File.Exists(path))
            {
                MessageBox.Show("File PDF không tồn tại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            string full = Path.GetFullPath(path);
            var fi = new FileInfo(full);
            // Đã mở đúng tệp này và tệp không đổi -> không nạp lại
            if (pdfViewer.Document != null &&
                string.Equals(_pathDangMo, full, StringComparison.OrdinalIgnoreCase) &&
                _kichThuocDangMo == fi.Length && _ngaySuaDangMo == fi.LastWriteTimeUtc)
            {
                _lblDangTai.Visible = false;
                return true;
            }
            // Đang nạp đúng tệp này rồi -> chờ chung kết quả
            if (_taskDangNap != null && string.Equals(_pathDangNap, full, StringComparison.OrdinalIgnoreCase))
            {
                return await _taskDangNap;
            }
            int ver = ++_loadVersion;
            _pathDangNap = full;
            Task<bool> job = NapAsync(full, fi.Length, fi.LastWriteTimeUtc, ver);
            _taskDangNap = job;
            try
            {
                return await job;
            }
            finally
            {
                if (ReferenceEquals(_taskDangNap, job))
                {
                    _taskDangNap = null;
                    _pathDangNap = null;
                }
            }
        }
        private async Task<bool> NapAsync(string full, long kichThuoc, DateTime ngaySuaUtc, int ver)
        {
            _lblDangTai.Visible = true;
            _lblDangTai.BringToFront();
            PdfDocument? doc = null;
            try
            {
                doc = await Task.Run(() =>
                {
                    if (!DamBaoPdfium())
                        throw new InvalidOperationException(_loiPdfium ?? "Không nạp được thư viện PDFium.");
                    return PdfDocument.Load(full);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi load PDF: {ex.Message}");
                if (IsDisposed) return false;
                _lblDangTai.Visible = false;
                if (!string.IsNullOrEmpty(_loiPdfium))
                    MessageBox.Show(_loiPdfium, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Fallback: Mở bằng ứng dụng mặc định hệ thống
                TaTimThayEm_OpenPdfExternally(full);
                Close();
                return false;
            }
            // Form đã đóng / có lần nạp mới hơn trong lúc chờ -> bỏ kết quả này
            if (IsDisposed || ver != _loadVersion)
            {
                doc.Dispose();
                return false;
            }
            var cu = pdfViewer.Document;
            pdfViewer.Document = doc;      // gán tài liệu mới TRƯỚC rồi mới giải phóng tài liệu cũ
            try { cu?.Dispose(); } catch { }
            _daChinhMucLuc = false;
            BeginInvoke(new Action(DatDoRongMucLuc));   // chờ viewer dựng xong mục lục rồi mới chỉnh
            // Nạp lại sau khi giải phóng RAM -> trả về đúng trang đang xem (chỉ khi cùng tệp)
            if (_trangKhoiPhuc > 0 && string.Equals(_pathNho, full, StringComparison.OrdinalIgnoreCase))
            {
                try { pdfViewer.Renderer.Page = _trangKhoiPhuc; } catch { }
            }
            _trangKhoiPhuc = 0;
            _pathDangMo = full;
            _pathNho = full;
            _kichThuocDangMo = kichThuoc;
            _ngaySuaDangMo = ngaySuaUtc;
            _lblDangTai.Visible = false;
            return true;
        }
        // Giữ tên cũ cho nơi khác còn gọi (không chờ kết quả)
        public void GoiTenEmTrongDem_LoadPdf(string path)
        {
            _ = LoadPdfAsync(path);
        }
        private void TaTimThayEm_OpenPdfExternally(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở file: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // ================= GIẢI PHÓNG RAM KHI ẨN LÂU =================
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (IsDisposed || pdfViewer == null) return;
            if (Visible)
            {
                _timerGiaiPhong?.Stop();
                // Form vừa hiện mà mục lục chưa được chỉnh độ rộng -> chỉnh ngay
                if (!_daChinhMucLuc && pdfViewer.Document != null)
                    BeginInvoke(new Action(DatDoRongMucLuc));
                // Hiện lại sau khi đã giải phóng RAM mà chưa ai gọi nạp -> tự nạp lại tệp cũ
                if (pdfViewer.Document == null && _taskDangNap == null
                    && _pathNho != null && File.Exists(_pathNho))
                {
                    _ = LoadPdfAsync(_pathNho);
                }
            }
            else if (TuGiaiPhongSauPhut > 0 && pdfViewer.Document != null)
            {
                _timerGiaiPhong ??= TaoTimerGiaiPhong();
                _timerGiaiPhong.Stop();
                _timerGiaiPhong.Start();
            }
        }
        private System.Windows.Forms.Timer TaoTimerGiaiPhong()
        {
            var t = new System.Windows.Forms.Timer { Interval = TuGiaiPhongSauPhut * 60_000 };
            t.Tick += (s, e) =>
            {
                t.Stop();
                if (!IsDisposed && !Visible && pdfViewer.Document != null)
                {
                    try { _trangKhoiPhuc = pdfViewer.Renderer.Page; } catch { _trangKhoiPhuc = 0; }
                    GiaiPhongTaiLieu(giuDuongDan: true);
                }
            };
            return t;
        }
        // ====== Độ rộng panel Mục lục (bookmark) bên trái ======
        private const double TyLeMucLuc = 0.33;   // 0.33 = 33% chiều rộng form. Muốn rộng hơn thì tăng (vd 0.40)
        private SplitContainer? _splitMucLuc;
        private bool _daChinhMucLuc = false;
        private static SplitContainer? TimSplitContainer(Control goc)
        {
            foreach (Control c in goc.Controls)
            {
                if (c is SplitContainer sc) return sc;
                var con = TimSplitContainer(c);
                if (con != null) return con;
            }
            return null;
        }
        private void DatDoRongMucLuc()
        {
            try
            {
                if (IsDisposed || pdfViewer == null) return;
                _splitMucLuc ??= TimSplitContainer(pdfViewer);
                var sc = _splitMucLuc;
                if (sc == null || sc.Panel1Collapsed || sc.Width <= 0) return;
                int mong = (int)(sc.Width * TyLeMucLuc);
                int toiDa = sc.Width - sc.Panel2MinSize - sc.SplitterWidth;
                int d = Math.Max(sc.Panel1MinSize, Math.Min(mong, toiDa));
                if (d > 0 && sc.SplitterDistance != d)
                    sc.SplitterDistance = d;
                _daChinhMucLuc = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[DatDoRongMucLuc] " + ex.Message);
            }
        }
        private void GiaiPhongTaiLieu(bool giuDuongDan)
        {
            _loadVersion++;   // hủy lần nạp nền đang chờ (nếu có)
            _pathDangMo = null;
            if (!giuDuongDan)
            {
                _pathNho = null;
                _trangKhoiPhuc = 0;
            }
            try
            {
                if (!_lblDangTai.IsDisposed) _lblDangTai.Visible = false;
                if (pdfViewer?.Document != null)
                {
                    var doc = pdfViewer.Document;
                    pdfViewer.Document = null;
                    doc.Dispose();
                }
            }
            catch { }
        }
        /// <summary>Đóng hẳn tài liệu và quên đường dẫn (dùng khi đóng form).</summary>
        public void DisposePdf() => GiaiPhongTaiLieu(giuDuongDan: false);
    }
}