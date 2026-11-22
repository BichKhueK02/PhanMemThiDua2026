///Phiên bản 1 – Bộ đo hiệu năng khởi động / đăng nhập (chạy được trên máy người dùng, bản Release, không cần Visual Studio)
///
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// ĐO HIỆU NĂNG theo "phiên" (mặc định: từ lúc phần mềm dựng Form1 cho tới khi giao diện chính nạp xong các trang nền).
    /// Ghi ra Database\NhatKyNhaPhatTrien\hieunang-yyyyMMdd.log một BẢN BÁO CÁO DUY NHẤT khi phiên kết thúc, gồm:
    ///   • Dòng thời gian các mốc / khoảng thời gian (luồng giao diện [UI] hay luồng nền [nền]), tính từ đầu phiên;
    ///   • Mọi lần LUỒNG GIAO DIỆN BỊ KẸT ≥ <see cref="NguongKetMs"/> ms (người dùng thấy đứng hình / gõ phím khựng),
    ///     kèm TÊN BƯỚC đang chạy trên luồng giao diện đúng lúc kẹt (do một luồng canh gác riêng chụp lại);
    ///   • Tình trạng ThreadPool (hàng đợi lớn nhất, số luồng lớn nhất): phát hiện nghẽn Task.Run lúc khởi động.
    ///
    /// Nguyên tắc an toàn:
    ///   • Không có phiên => mọi hàm trả về ngay (1 lần đọc biến volatile), không cấp phát.
    ///   • Không bao giờ ném lỗi ra ngoài; không đụng control nào; luồng canh gác là luồng nền ưu tiên thấp.
    ///   • Phiên tự kết thúc sau <see cref="ThoiGianToiDaMacDinhMs"/> dù quên gọi KetThuc; số dòng có giới hạn.
    ///   • Không ghi tài khoản / mật khẩu / dữ liệu người dùng: chỉ tên bước và thời gian.
    ///
    /// Cách dùng:
    ///   using (Module_DoHieuNang.Doan("Form4.HienLuoi")) { ...việc ĐỒNG BỘ trên luồng UI... }   // gắn nhãn cho lần kẹt UI
    ///   using (Module_DoHieuNang.Khoang("Form4.Reload")) { await ...; }                          // khoảng có await (chỉ đo)
    ///   Module_DoHieuNang.Moc("Bấm Đăng nhập");
    /// </summary>
    internal static class Module_DoHieuNang
    {
        /// <summary>Tắt hẳn bộ đo: đặt false (mọi lời gọi thành rỗng).</summary>
        private const bool BatBoDo = true;
        private const int ThoiGianToiDaMacDinhMs = 90_000;
        private const int NguongKetMs = 80;          // luồng UI không trả lời quá ngần này = người dùng cảm nhận được khựng
        private const int NguongGhiDoanMs = 15;      // Doan/Khoang ngắn hơn thì bỏ qua (giảm nhiễu)
        private const int NhipCanhGacMs = 20;
        private const int SoDongToiDa = 600;
        private const string TienToTep = "hieunang";
        private static volatile PhienDo? _phien;
        /// <summary>Đang có phiên đo không.</summary>
        public static bool DangDo => _phien != null;
        /// <summary>
        /// Bắt đầu phiên đo. PHẢI gọi trên luồng giao diện, sau khi đã có control đầu tiên (đã có WindowsFormsSynchronizationContext).
        /// Gọi khi đang có phiên thì bỏ qua.
        /// </summary>
        public static void BatDau(string tenPhien, int toiDaMs = ThoiGianToiDaMacDinhMs)
        {
            if (!BatBoDo || _phien != null) return;
            try
            {
                var sc = SynchronizationContext.Current;
                if (sc is NguCanhDoAwait boc) sc = boc.Goc;                 // đã bọc để chẩn đoán await: canh gác gửi thẳng vào ngữ cảnh gốc
                if (sc is not WindowsFormsSynchronizationContext) return;   // không ở luồng UI: không đo được độ kẹt
                var p = new PhienDo(tenPhien, sc, Environment.CurrentManagedThreadId, toiDaMs);
                if (Interlocked.CompareExchange(ref _phien, p, null) != null) return;
                LocThongDiep.CaiMotLan();
                NguCanhDoAwait.CaiNeuBat(sc);
                p.KhoiDongCanhGac();
            }
            catch (Exception ex) { Debug.WriteLine("[DoHieuNang] BatDau: " + ex.Message); }
        }
        /// <summary>Kết thúc phiên (nếu có) và ghi báo cáo ở luồng nền. Gọi từ luồng nào cũng được, gọi nhiều lần an toàn.</summary>
        /// <param name="dongBo">true = ghi báo cáo ngay trên luồng gọi (dùng lúc thoát phần mềm: luồng nền có thể bị giết trước khi kịp ghi).</param>
        public static void KetThuc(string lyDo, bool dongBo = false)
        {
            var p = Interlocked.Exchange(ref _phien, null);
            p?.Dong(lyDo, dongBo);
        }
        /// <summary>Ghi một mốc thời gian.</summary>
        public static void Moc(string ten) => _phien?.Ghi(ten, -1);
        /// <summary>
        /// Một đoạn ĐỒNG BỘ (không await bên trong) - thường trên luồng UI: đo thời gian và gắn nhãn cho luồng UI trong lúc chạy,
        /// để nếu UI bị kẹt thì báo cáo ghi rõ "kẹt ở đoạn nào". Trả null khi không đo (using(null) hợp lệ).
        /// </summary>
        public static IDisposable? Doan(string ten) => _phien?.MoDoan(ten, ganNhanUi: true);
        /// <summary>Một khoảng có thể chứa await (chạy xen với việc khác): chỉ đo tổng thời gian, KHÔNG gắn nhãn UI.</summary>
        public static IDisposable? Khoang(string ten) => _phien?.MoDoan(ten, ganNhanUi: false);
        // ---------------- Kiểm tra truy cập control SAI LUỒNG (chẩn đoán, chỉ bật bằng PMTD_KIEM_TRA_LUONG=1) ----------------
        [ThreadStatic] private static bool _dangGhiLoiLuong;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> _loiLuongDaGhi = new();
        /// <summary>
        /// CHẨN ĐOÁN: bật kiểm tra nghiêm ngặt của WinForms (bản Release mặc định TẮT => luồng nền chạm control vẫn "chạy được"
        /// nhưng có thể treo/hỏng giao diện ngẫu nhiên). Mỗi lỗi sai luồng được bắt NGAY LÚC NÉM (FirstChanceException - kể cả khi
        /// code nuốt lỗi bằng try/catch) và ghi 1 lần kèm ngăn xếp gọi hàm vào luong-*.log. Gọi trước khi tạo cửa sổ đầu tiên.
        /// </summary>
        public static void BatKiemTraLuongNeuCan()
        {
            if (Environment.GetEnvironmentVariable("PMTD_KIEM_TRA_LUONG") != "1") return;
            Control.CheckForIllegalCrossThreadCalls = true;
            AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
            {
                if (_dangGhiLoiLuong || e.Exception is not InvalidOperationException ioe) return;
                if (ioe.Message.IndexOf("Cross-thread", StringComparison.OrdinalIgnoreCase) < 0 &&
                    ioe.Message.IndexOf("luồng", StringComparison.OrdinalIgnoreCase) < 0) return;
                _dangGhiLoiLuong = true;
                try
                {
                    string stack = new StackTrace(1, false).ToString();
                    // Gộp lỗi trùng theo 6 khung đầu của ngăn xếp (cùng chỗ gọi chỉ ghi 1 lần)
                    string khoa = string.Join("|", stack.Split('\n').Take(8));
                    if (!_loiLuongDaGhi.TryAdd(khoa, 0)) return;
                    Module_NhatKyNhaPhatTrien.GhiDong("luong",
                        $"SAI LUỒNG (luồng #{Environment.CurrentManagedThreadId} '{Thread.CurrentThread.Name}'): {ioe.Message}{Environment.NewLine}{stack}");
                }
                catch { }
                finally { _dangGhiLoiLuong = false; }
            };
        }
        // ---------------- Đo chi phí VẼ từng control (chẩn đoán, chỉ bật bằng biến môi trường PMTD_DO_VE=1) ----------------
        private static readonly bool BatDoVe = Environment.GetEnvironmentVariable("PMTD_DO_VE") == "1";
        private static readonly HashSet<Type> _daDoVe = new();
        /// <summary>
        /// CHẨN ĐOÁN: vẽ thử (DrawToBitmap) từng control trong <paramref name="goc"/>, tính thời gian vẽ RIÊNG của mỗi control
        /// (= vẽ cả cụm trừ phần các control con), ghi 15 control tốn nhất vào hieunang-*.log. Mỗi loại trang chỉ đo 1 lần.
        /// Tắt mặc định (chỉ bật bằng PMTD_DO_VE=1): bản thân việc đo vẽ lại nhiều lần nên rất chậm. Gọi trên luồng UI.
        /// </summary>
        public static void DoChiPhiVe(Control goc)
        {
            if (!BatDoVe || goc.IsDisposed || !goc.IsHandleCreated || !_daDoVe.Add(goc.GetType())) return;
            try
            {
                var kq = new List<(string Ten, double Rieng, double Gom, int W, int H)>();
                var swTong = Stopwatch.StartNew();
                double Do(Control c)
                {
                    double conGom = 0;
                    foreach (Control con in c.Controls)
                        if (con.Visible && con.Width > 0 && con.Height > 0) conGom += Do(con);
                    double gom = 0;
                    try
                    {
                        using var bmp = new Bitmap(Math.Max(1, c.Width), Math.Max(1, c.Height));
                        c.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));   // lần 1: làm nóng
                        long t0 = Stopwatch.GetTimestamp();
                        c.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                        gom = Stopwatch.GetElapsedTime(t0).TotalMilliseconds;
                    }
                    catch { }
                    string ten = $"{c.GetType().Name} '{c.Name}'";
                    kq.Add((ten, Math.Max(0, gom - conGom), gom, c.Width, c.Height));
                    return gom;
                }
                double tong = Do(goc);
                var sb = new StringBuilder();
                sb.AppendLine($"===== [{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] CHI PHÍ VẼ {goc.GetType().Name} ({goc.Width}×{goc.Height}): " +
                              $"vẽ cả trang {tong:F0} ms, {kq.Count} control (đo mất {swTong.ElapsedMilliseconds} ms)");
                foreach (var x in kq.OrderByDescending(x => x.Rieng).Take(15))
                    sb.AppendLine($"    riêng {x.Rieng,6:F1} ms | gồm con {x.Gom,6:F1} ms | {x.W}×{x.H} | {x.Ten}");
                Module_NhatKyNhaPhatTrien.GhiTep(TienToTep, sb.ToString());
            }
            catch (Exception ex) { Debug.WriteLine("[DoHieuNang] DoChiPhiVe: " + ex.Message); }
        }
        // =====================================================================================================
        /// <summary>
        /// Gắn nhãn cho luồng UI theo THÔNG ĐIỆP đang được xử lý (WM_PAINT của control nào, WM_TIMER, lời gọi await/BeginInvoke
        /// quay về...). Nhờ vậy lần kẹt nằm ngoài mọi Doan() vẫn biết "đang làm gì" thay vì chỉ ghi "(vòng lặp thông điệp)".
        /// Cài 1 lần trên luồng UI, không gỡ (khi không có phiên đo: chỉ 1 lần đọc biến rồi trả false - không ảnh hưởng gì).
        /// Lưu ý: thông điệp GỬI THẲNG (SendMessage, RedrawWindow RDW_UPDATENOW) không đi qua đây - chúng nằm trong Doan bao ngoài.
        /// </summary>
        private sealed class LocThongDiep : IMessageFilter
        {
            private static int _daCai;
            private static readonly int WM_GOI_LAI = DangKyThongDiep("WindowsForms12_ThreadCallbackMessage");
            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            private static extern int RegisterWindowMessage(string ten);
            private static int DangKyThongDiep(string ten) { try { return RegisterWindowMessage(ten); } catch { return -1; } }
            public static void CaiMotLan()
            {
                if (Interlocked.Exchange(ref _daCai, 1) != 0) return;
                try { Application.AddMessageFilter(new LocThongDiep()); } catch { }
            }
            public bool PreFilterMessage(ref Message m)
            {
                var p = _phien;
                if (p == null || !p.LaLuongUiCongKhai) return false;
                try
                {
                    string? loai = m.Msg switch
                    {
                        0x000F => "WM_PAINT",
                        0x0113 => "WM_TIMER",
                        0x0201 or 0x0204 => "bấm chuột",
                        0x0202 or 0x0205 => "nhả chuột",
                        0x0100 or 0x0102 => "phím",
                        _ when m.Msg == WM_GOI_LAI => "await/BeginInvoke quay về luồng UI",
                        _ => null   // MOUSEMOVE, NCHITTEST... quá dày: giữ nhãn cũ
                    };
                    if (loai != null) p.DatNhanThongDiep(loai, m.HWnd);
                }
                catch { }
                return false;   // không bao giờ chặn thông điệp
            }
        }
        /// <summary>
        /// CHẨN ĐOÁN (chỉ bật bằng PMTD_DO_AWAIT=1): thay SynchronizationContext của luồng UI bằng bản con của
        /// WindowsFormsSynchronizationContext, gói mỗi lần "await quay về luồng UI" vào một Doan mang TÊN HÀM ASYNC
        /// (vd. "await → Form10_NhatKy.NapDauAsync"). Nhờ vậy biết chính xác đoạn mã nào sau await làm UI đứng hình.
        /// Hành vi không đổi: mọi lời gọi vẫn chuyển cho lớp gốc (Post/Send của WinForms). Tắt mặc định vì đổi ngữ cảnh đồng bộ
        /// của cả luồng UI là việc chỉ nên làm khi chẩn đoán.
        /// </summary>
        private sealed class NguCanhDoAwait : SynchronizationContext
        {
            private static readonly bool Bat = Environment.GetEnvironmentVariable("PMTD_DO_AWAIT") == "1";
            private static int _daCai;
            public SynchronizationContext Goc { get; }
            private NguCanhDoAwait(SynchronizationContext goc) { Goc = goc; }
            public static void CaiNeuBat(SynchronizationContext hienTai)
            {
                if (!Bat || hienTai is not WindowsFormsSynchronizationContext || Interlocked.Exchange(ref _daCai, 1) != 0) return;
                try { SetSynchronizationContext(new NguCanhDoAwait(hienTai)); } catch { }
            }
            public override void Post(SendOrPostCallback d, object? state)
            {
                if (_phien == null) { Goc.Post(d, state); return; }
                string ten = "await → " + TenTiepNoi(d, state);
                Goc.Post(s => { using (Doan(ten)) d(s); }, state);
            }
            public override void Send(SendOrPostCallback d, object? state) => Goc.Send(d, state);
            public override SynchronizationContext CreateCopy() => new NguCanhDoAwait(Goc.CreateCopy());
            /// <summary>Tên hàm async đang chạy tiếp: lấy từ kiểu máy trạng thái (AsyncStateMachineBox&lt;TStateMachine&gt;).</summary>
            private static string TenTiepNoi(SendOrPostCallback d, object? state)
            {
                try
                {
                    object? dich = (state as Delegate)?.Target ?? state ?? d.Target;
                    var t = dich?.GetType();
                    if (t == null) return d.Method.Name;
                    var may = t.IsGenericType ? t.GetGenericArguments().FirstOrDefault(x => x.Name.StartsWith('<')) : null;
                    if (may != null)
                    {
                        string ham = may.Name.Length > 1 && may.Name.IndexOf('>') > 1 ? may.Name[1..may.Name.IndexOf('>')] : may.Name;
                        return $"{may.DeclaringType?.Name}.{ham}";
                    }
                    return t.DeclaringType != null ? $"{t.DeclaringType.Name}.{t.Name}" : t.Name;
                }
                catch { return "?"; }
            }
        }
        private static string MoTaControl(IntPtr hwnd)
        {
            try
            {
                var c = Control.FromHandle(hwnd);
                if (c == null) return "cửa sổ ẩn (Timer/Marshal)";
                string ten = string.IsNullOrEmpty(c.Name) ? c.GetType().Name : c.Name;
                var f = c as Form ?? c.FindForm();
                return f != null && !ReferenceEquals(f, c) ? $"{f.GetType().Name}.{ten}" : ten;
            }
            catch { return "?"; }
        }
        private sealed class PhienDo
        {
            private readonly string _ten;
            private readonly SynchronizationContext _sc;
            private readonly int _idLuongUi;
            private readonly int _toiDaMs;
            private readonly long _batDau = Stopwatch.GetTimestamp();
            private readonly DateTime _batDauLuc = DateTime.Now;
            private readonly ConcurrentQueue<(double Ms, string Dong)> _dong = new();
            private int _soDong;
            private volatile bool _daDong;
            // Nhãn bước đang chạy trên luồng UI (chỉ luồng UI ghi, luồng canh gác đọc)
            private const string NhanVongLap = "(vòng lặp thông điệp)";
            private volatile string _nhanUi = NhanVongLap;
            // Thống kê
            private long _tongKetMs;
            private int _soLanKet;
            private int _ketLonNhatMs;
            private long _hangDoiPoolMax;
            private int _soLuongPoolMax;
            private readonly int _soNhan = Environment.ProcessorCount;
            public PhienDo(string ten, SynchronizationContext sc, int idLuongUi, int toiDaMs)
            {
                _ten = ten; _sc = sc; _idLuongUi = idLuongUi; _toiDaMs = toiDaMs;
            }
            private double MsTuDau() => Stopwatch.GetElapsedTime(_batDau).TotalMilliseconds;
            private bool LaLuongUi => Environment.CurrentManagedThreadId == _idLuongUi;
            public bool LaLuongUiCongKhai => LaLuongUi;
            // Nhãn hiện tại do bộ lọc thông điệp đặt (để không đè lên nhãn của một Doan đang chạy)
            private string? _nhanThongDiepCuoi;
            private IntPtr _hwndCuoi;
            private string? _loaiCuoi;
            public void DatNhanThongDiep(string loai, IntPtr hwnd)
            {
                // Chỉ đổi nhãn khi luồng UI đang ở "vòng lặp thông điệp" hoặc đang mang nhãn thông điệp trước đó
                string hienTai = _nhanUi;
                if (!ReferenceEquals(hienTai, NhanVongLap) && !ReferenceEquals(hienTai, _nhanThongDiepCuoi)) return;
                if (hwnd == _hwndCuoi && ReferenceEquals(loai, _loaiCuoi) && _nhanThongDiepCuoi != null) { _nhanUi = _nhanThongDiepCuoi; return; }
                string nhan = $"{loai} → {MoTaControl(hwnd)}";
                _hwndCuoi = hwnd; _loaiCuoi = loai; _nhanThongDiepCuoi = nhan;
                _nhanUi = nhan;
            }
            public void Ghi(string noiDung, double thoiLuongMs)
            {
                if (_daDong) return;
                if (Interlocked.Increment(ref _soDong) > SoDongToiDa) return;
                string luong = LaLuongUi ? "UI  " : "nền ";
                string dong = thoiLuongMs < 0
                    ? $"[{luong}] ● {noiDung}"
                    : $"[{luong}] {noiDung}: {thoiLuongMs:F0} ms";
                _dong.Enqueue((MsTuDau() - Math.Max(0, thoiLuongMs), dong));   // xếp theo THỜI ĐIỂM BẮT ĐẦU
            }
            public IDisposable? MoDoan(string ten, bool ganNhanUi)
            {
                if (_daDong) return null;
                bool gan = ganNhanUi && LaLuongUi;
                string? nhanCu = null;
                if (gan) { nhanCu = _nhanUi; _nhanUi = ten; }
                return new Doan_(this, ten, gan, nhanCu);
            }
            private sealed class Doan_ : IDisposable
            {
                private readonly PhienDo _p; private readonly string _ten; private readonly bool _gan; private readonly string? _nhanCu;
                private readonly long _t0 = Stopwatch.GetTimestamp();
                private int _xong;
                public Doan_(PhienDo p, string ten, bool gan, string? nhanCu) { _p = p; _ten = ten; _gan = gan; _nhanCu = nhanCu; }
                public void Dispose()
                {
                    if (Interlocked.Exchange(ref _xong, 1) != 0) return;
                    double ms = Stopwatch.GetElapsedTime(_t0).TotalMilliseconds;
                    if (_gan && _p.LaLuongUi) _p._nhanUi = _nhanCu ?? NhanVongLap;
                    if (ms >= NguongGhiDoanMs) _p.Ghi(_ten, ms);
                }
            }
            // ---------------- Luồng canh gác: đo độ trễ của luồng UI ----------------
            public void KhoiDongCanhGac()
            {
                new Thread(CanhGac) { IsBackground = true, Name = "DoHieuNang", Priority = ThreadPriority.AboveNormal }.Start();
            }
            private void CanhGac()
            {
                // Mỗi nhịp gửi 1 "tiếng gõ" vào hàng đợi thông điệp UI và chờ nó được xử lý. Gõ lâu không được trả lời
                // = luồng UI đang bận một khối đồng bộ -> chụp nhãn bước đang chạy (có thể đổi nhiều nhãn trong 1 lần kẹt).
                try
                {
                    while (!_daDong)
                    {
                        if (MsTuDau() > _toiDaMs) { KetThuc($"hết thời gian tối đa {_toiDaMs / 1000} s"); return; }
                        LayMauThreadPool();
                        // Chụp GC/JIT toàn tiến trình lúc gửi tiếng gõ: nếu kẹt thì biết trong lúc kẹt .NET đã dừng bao lâu để dọn rác
                        // (GC chặn mọi luồng) và đã JIT bao lâu -> phân biệt "mã của mình chậm" với "runtime đang bận".
                        TimeSpan gc0 = GC.GetTotalPauseDuration();
                        int gen2_0 = GC.CollectionCount(2);
                        TimeSpan jit0 = System.Runtime.JitInfo.GetCompilationTime(false);
                        long guiLuc = Stopwatch.GetTimestamp();
                        // KHÔNG Dispose: tiếng gõ có thể được UI trả lời SAU khi phiên đã đóng (Set trên đối tượng đã Dispose
                        // sẽ ném lỗi trên luồng UI). MRES chưa từng cấp WaitHandle thì không giữ tài nguyên hệ điều hành.
                        var traLoi = new ManualResetEventSlim(false, spinCount: 0);
                        try { _sc.Post(static o => ((ManualResetEventSlim)o!).Set(), traLoi); }
                        catch { return; }   // luồng UI đã kết thúc
                        if (traLoi.Wait(NguongKetMs))
                        {
                            Thread.Sleep(NhipCanhGacMs);
                            continue;
                        }
                        // ---- Đang kẹt ----
                        double batDauKet = Stopwatch.GetElapsedTime(_batDau, guiLuc).TotalMilliseconds;
                        var cacNhan = new List<string>(2) { _nhanUi };
                        while (!_daDong && !traLoi.Wait(NhipCanhGacMs))
                        {
                            string n = _nhanUi;
                            if (cacNhan[^1] != n && cacNhan.Count < 6) cacNhan.Add(n);
                            LayMauThreadPool();
                            if (MsTuDau() > _toiDaMs) break;
                        }
                        int ketMs = (int)Stopwatch.GetElapsedTime(guiLuc).TotalMilliseconds;
                        Interlocked.Add(ref _tongKetMs, ketMs);
                        Interlocked.Increment(ref _soLanKet);
                        if (ketMs > _ketLonNhatMs) _ketLonNhatMs = ketMs;
                        double gcMs = (GC.GetTotalPauseDuration() - gc0).TotalMilliseconds;
                        int gen2 = GC.CollectionCount(2) - gen2_0;
                        double jitMs = (System.Runtime.JitInfo.GetCompilationTime(false) - jit0).TotalMilliseconds;
                        string runtime = $" | trong lúc kẹt: GC dừng {gcMs:F0} ms{(gen2 > 0 ? $" (gen2 ×{gen2})" : "")}, JIT {jitMs:F0} ms";
                        if (Interlocked.Increment(ref _soDong) <= SoDongToiDa)
                            _dong.Enqueue((batDauKet, $"[UI  ] ▲ UI KẸT {ketMs} ms  (đang chạy: {string.Join(" → ", cacNhan)}){runtime}"));
                    }
                }
                catch (Exception ex) { Debug.WriteLine("[DoHieuNang] canh gác: " + ex.Message); }
            }
            private void LayMauThreadPool()
            {
                try
                {
                    long q = ThreadPool.PendingWorkItemCount;
                    int n = ThreadPool.ThreadCount;
                    if (q > _hangDoiPoolMax) _hangDoiPoolMax = q;
                    if (n > _soLuongPoolMax) _soLuongPoolMax = n;
                }
                catch { }
            }
            // ---------------- Báo cáo ----------------
            public void Dong(string lyDo, bool dongBo)
            {
                if (_daDong) return;
                double tong = MsTuDau();
                _daDong = true;
                if (dongBo) GhiBaoCao(lyDo, tong);
                else _ = Task.Run(() => GhiBaoCao(lyDo, tong));
            }
            private void GhiBaoCao(string lyDo, double tong)
            {
                try
                {
                    var sb = new StringBuilder(8 * 1024);
                    sb.AppendLine($"===== [{_batDauLuc:yyyy-MM-dd HH:mm:ss.fff}] PHIÊN ĐO \"{_ten}\" | tổng {tong:F0} ms | kết thúc: {lyDo}");
                    sb.AppendLine($"      Máy: {_soNhan} luồng CPU, {(Environment.Is64BitProcess ? "x64" : "x86")}, .NET {Environment.Version}" +
                                  $" | JIT toàn tiến trình: {ThongKeJit()}");
                    sb.AppendLine($"      UI kẹt ≥{NguongKetMs} ms: {_soLanKet} lần, tổng {Interlocked.Read(ref _tongKetMs)} ms, lâu nhất {_ketLonNhatMs} ms" +
                                  $" | ThreadPool: hàng đợi lớn nhất {Interlocked.Read(ref _hangDoiPoolMax)}, số luồng lớn nhất {_soLuongPoolMax}");
                    if (_soDong > SoDongToiDa) sb.AppendLine($"      (đã cắt bớt: {_soDong - SoDongToiDa} dòng vượt giới hạn {SoDongToiDa})");
                    foreach (var (ms, dong) in _dong.ToArray().OrderBy(x => x.Ms))
                        sb.Append("  +").Append(ms.ToString("F0").PadLeft(6)).Append(" ms  ").AppendLine(dong);
                    Module_NhatKyNhaPhatTrien.GhiTep(TienToTep, sb.ToString());
                }
                catch (Exception ex) { Debug.WriteLine("[DoHieuNang] ghi báo cáo: " + ex.Message); }
            }
            /// <summary>Số phương thức đã JIT + tổng thời gian JIT của cả tiến trình tới lúc này (bản ReadyToRun sẽ thấp hẳn).</summary>
            private static string ThongKeJit()
            {
                try
                {
                    long so = System.Runtime.JitInfo.GetCompiledMethodCount(false);
                    double ms = System.Runtime.JitInfo.GetCompilationTime(false).TotalMilliseconds;
                    return $"{so} phương thức, {ms:F0} ms";
                }
                catch { return "không đọc được"; }
            }
        }
    }
}
