///Phiên bản 3 (Tối ưu khởi động: bỏ khôi phục CSDL trùng lặp, đẩy tuần tra/đồng bộ nặng xuống nền)
///
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
namespace PhanMemThiDua2026
{
    internal static class Program
    {
        //  HẰNG SỐ & CẤU HÌNH
        private const string DeveloperComputerName = "MEOCAM";
        private const int MaxInstancesOnDeveloper = 3;
        private const int MaxInstancesOnClient = 1;
        private const int ExitOk = 0;
        private const int ExitFatal = 1;
        private const int ExitAlreadyRunning = 2;
        private const int ExitInitFailed = 3;
        private const string AdminContact = "Admin TrungKien: 0975.287.973";
        private const int IeEmulationMode = 11001;        // IE11 edge mode cho WebBrowser
        private const int SlowStartupThresholdMs = 1500;  // ngưỡng ghi log "khởi động chậm" (cả khối lõi)
        private const int SlowStepThresholdMs = 300;      // ngưỡng ghi log từng bước chậm
        private const int BackgroundStartDelayMs = 1500;  // nhường CPU/đĩa cho Form1 dựng xong rồi mới chạy nền
        private const int BackgroundShutdownWaitMs = 3000;
        private const int LogRetentionDays = 30;
        // Bộ chống "bão lỗi" giao diện: >= 5 lỗi UI trong 5 giây => đóng app an toàn
        private const int UiErrorBurstLimit = 5;
        private const long UiErrorBurstWindowMs = 5000;
        //  TRẠNG THÁI TOÀN CỤC
        private static Mutex? _appMutex;
        private static Task? _backgroundTask;
        private static readonly CancellationTokenSource ShutdownCts = new();
        private static readonly string AppMutexName = BuildMutexName("CORE");
        private static readonly string MsgMutexName = BuildMutexName("MSG");
        // Nhật ký dự phòng nay nằm trong <thư mục phần mềm>\Database\NhatKyNhaPhatTrien (không ghi vào %LOCALAPPDATA% nữa)
        private static int _errorDialogOpen;   // 0/1 - chống chồng hộp thoại lỗi
        private static int _uiErrorCount;      // chỉ truy cập từ UI thread
        private static long _uiErrorWindowStart;
        /// <summary>
        /// MULTICORE JIT (System.Runtime.ProfileOptimization): lần chạy này ghi lại danh sách hàm đã JIT vào
        /// Database\NhatKyNhaPhatTrien\jit-khoidong.profile; từ lần chạy SAU, .NET biên dịch trước các hàm đó trên luồng nền
        /// song song (đúng thứ tự đã dùng), nên lúc dựng form lớn luồng giao diện không còn phải tự JIT.
        /// Đo 22/11/2026 (bản build thường, không ReadyToRun): JIT ~3,4 s/phiên; dựng Form42 lần đầu riêng JIT ~250 ms,
        /// bấm Sổ vàng lần đầu UI kẹt 428 ms trong đó JIT 215 ms. Bản publish ReadyToRun vẫn có lợi (phần còn phải JIT).
        /// Tệp nằm trong thư mục nhật ký nhà phát triển (đã có trong danh sách trắng, việc dọn dẹp chỉ xóa *.log) - không ghi AppData.
        /// Lỗi bất kỳ => bỏ qua, phần mềm chạy như cũ.
        /// </summary>
        private static void BatJitDaLuong()
        {
            try
            {
                string thuMuc = Module_NhatKyNhaPhatTrien.ThuMuc;
                Directory.CreateDirectory(thuMuc);
                System.Runtime.ProfileOptimization.SetProfileRoot(thuMuc);
                System.Runtime.ProfileOptimization.StartProfile("jit-khoidong.profile");
            }
            catch (Exception ex) { Debug.WriteLine("[JIT đa luồng] " + ex.Message); }
        }
        //  ĐIỂM VÀO
        [STAThread]
        private static int Main()
        {
            // 1. Bẫy lỗi toàn cục phải gắn TRƯỚC mọi thứ khác.
            ConfigureGlobalExceptionHandlers();
            // 1b. Multicore JIT: xem BatJitDaLuong.
            BatJitDaLuong();
            // 2. Nền tảng WinForms (DPI -> visual styles -> font mặc định).
            ConfigureWinForms();
            bool isPrimaryInstance = false;
            try
            {
                // 3. Single instance
                isPrimaryInstance = AcquireInstanceLock();
                if (!isPrimaryInstance)
                {
                    ShowSingleInstanceMessage();
                    return ExitAlreadyRunning;
                }
                // 3a. Đang gỡ cài đặt => không mở CSDL trong thư mục đang bị xóa
                if (Module_GoCaiDat.DangGoCaiDat())
                {
                    MessageBox.Show("Phần mềm đang được gỡ cài đặt.\nVui lòng chờ trình gỡ cài đặt hoàn tất.",
                        "PhanMemThiDua2026", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return ExitAlreadyRunning;
                }
                // 3b. ServiceBackup / ServiceRestore đang đọc/ghi CSDL => chờ xong mới được mở CSDL
                if (!Module_CapKhoaDichVu.ChoDichVuSaoLuuKhoiPhucXong())
                    return ExitAlreadyRunning;
                // 4. Phải xong trước khi có WebBrowser đầu tiên được tạo
                SetBrowserFeatureControl();
                // 5. Cảnh báo tỷ lệ màn hình: lỗi ở đây không được phép làm sập app
                RunNonCritical("Kiểm tra tỷ lệ màn hình",
                    () => Module_KhoiDongTrangChu.KiemTraVaCanhBaoTyLeManHinh());
                // 5b. Làm nóng song song: nhân CPU còn lại nạp sẵn SQLite/AES/Krypton trong lúc luồng chính khởi tạo CSDL.
                KhoiDongNongSongSong();
                // 6. Hệ thống lõi + CSDL (bắt buộc)
                if (!InitializeCore(out bool thieuTepKhoa))
                {
                    if (thieuTepKhoa) ShowKeyFileFailure();
                    else ShowStartupFailure();
                    return ExitInitFailed;
                }
                // 6b. CSDL bị mất/hỏng và đã được tự tạo lại: báo cho người dùng biết để khôi phục bản sao lưu gần nhất
                ShowDatabaseRecoveryWarning();
                // 7. Theme/màu menu + bảng nhật ký chạy SONG SONG với việc dựng Form1 (thay vì nối đuôi nhau).
                //    Vẫn chờ xong TRƯỚC Application.Run nên Form1 không bị nháy màu như trước.
                var tacVuSong = new[]
                {
                    Task.Run(() => RunNonCritical("Nạp theme", () => Module_GiaoDien.KhoiDongDocTheme())),
                    Task.Run(() => RunNonCritical("Nạp màu menu chuột phải",
                        () => Module_MenuChuotPhai.KhoiTaoMauTuCSDL(Module_DanduongGPS.DuongDanCSDL2))),
                    Task.Run(() => RunNonCritical("Tạo bảng nhật ký", () => Module_NhatKy.TaoBangNhatKy())),
                };
                // 8. Tác vụ nền (tuần tra/đồng bộ CSDL, tường lửa, làm sạch, đồng bộ Hướng dẫn...)
                //    chạy sau khi giao diện đã hiện.
                ScheduleBackgroundMaintenance();
                // 9. Giao diện chính: dựng Form1 trên luồng chính trong lúc các tác vụ ở bước 7 đang chạy nền.
                var form1 = new Form1();
                // Đo hiệu năng phiên khởi động (Database\NhatKyNhaPhatTrien\hieunang-*.log). new Form1() đã cài
                // WindowsFormsSynchronizationContext cho luồng này nên đo được độ kẹt UI ngay từ đây. Phiên kết thúc khi bấm Đăng nhập.
                Module_DoHieuNang.BatDau("Khởi động → màn hình đăng nhập");
                Module_DoHieuNang.Moc($"new Form1() xong - {MsTuLucMoTienTrinh()} ms kể từ lúc mở phần mềm (gồm khởi tạo CSDL)");
                using (Module_DoHieuNang.Doan("Chờ theme/màu menu/bảng nhật ký trước Application.Run"))
                {
                    try { Task.WaitAll(tacVuSong); } catch { /* lỗi từng bước đã được RunNonCritical ghi nhật ký */ }
                }
                Application.Run(form1);
                return ExitOk;
            }
            catch (Exception ex)
            {
                HandleFatalError(ex);
                return ExitFatal;
            }
            finally
            {
                // 10. Dọn dẹp luôn chạy, kể cả khi lỗi
                CleanupResources(isPrimaryInstance);
            }
        }
        /// <summary>Số ms từ lúc Windows tạo tiến trình tới giờ (-1 nếu không đọc được). Chỉ dùng cho nhật ký đo.</summary>
        private static long MsTuLucMoTienTrinh()
        {
            try
            {
                using var p = Process.GetCurrentProcess();
                return (long)(DateTime.Now - p.StartTime).TotalMilliseconds;
            }
            catch { return -1; }
        }
        //  LÀM NÓNG SONG SONG
        // Chạy ở thread pool, fire-and-forget; lỗi ở đây không bao giờ ảnh hưởng ứng dụng.
        // Mục đích: những bước "lần đầu" tốn kém (nạp thư viện native SQLite, bcrypt của AES, nạp assembly Krypton,
        // JIT) diễn ra trên các nhân CPU khác, nên khi luồng chính cần tới thì đã sẵn sàng.
        private static void KhoiDongNongSongSong()
        {
            Task.Run(() =>
            {
                try
                {
                    using var cn = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
                    cn.Open();
                    using var cmd = cn.CreateCommand();
                    cmd.CommandText = "SELECT 1";
                    cmd.ExecuteScalar();
                }
                catch { }
            });
            Task.Run(() => { try { Module_BaoMatAES.KhoiDongNong(); } catch { } });
            Task.Run(() =>
            {
                try
                {
                    // Chỉ nạp assembly (không chạy hàm dựng tĩnh, không đụng UI)
                    _ = typeof(Krypton.Toolkit.KryptonButton).Assembly;
                    _ = typeof(BCrypt.Net.BCrypt).Assembly;
                }
                catch { }
            });
        }
        //  KHỞI TẠO
        private static void ConfigureWinForms()
        {
            // Initialize() (sinh theo .csproj) đã gọi EnableVisualStyles +
            // SetCompatibleTextRenderingDefault + font mặc định, nên không gọi lại ở đây.
            ApplicationConfiguration.Initialize();
            // Mọi thiết lập DPI phải xong trước khi có bất kỳ cửa sổ nào được tạo.
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            // Chẩn đoán (tắt mặc định): PMTD_KIEM_TRA_LUONG=1 => bắt mọi lần luồng nền chạm control, ghi luong-*.log
            Module_DoHieuNang.BatKiemTraLuongNeuCan();
        }
        // Ghi chú: .GetAwaiter().GetResult() ở đây là BLOCKING có chủ đích. Main() chưa gọi
        // Application.Run nên chưa có WindowsFormsSynchronizationContext -> không deadlock,
        // và giữ nguyên thread STA cho Application.Run(new Form1()).
        // Đánh đổi: UI chưa hiện cho tới khi bước khôi phục tệp thiếu xong.
        //
        // LƯU Ý: HanhTrinhToiColombiaAsync() đã gọi Module_KhoiTaoCSDL.BinhMinhOSantoriniAsync()
        // bên trong, nên KHÔNG gọi lại ở đây (bản cũ gọi 2 lần => khởi động chậm gấp đôi).
        private static bool InitializeCore(out bool thieuTepKhoa)
        {
            thieuTepKhoa = false;
            var sw = Stopwatch.StartNew();
            try
            {
                Module_DanduongGPS.XinTraLaiThoiGianNapKeyBase64();
                Module_DanduongGPS.LoiChaoTuSiberia();
                // Khóa nằm ở tệp riêng (Module_TepKhoa). Thiếu/hỏng tệp khóa => dừng NGAY, trước mọi thao tác ghi CSDL.
                if (!Module_DanduongGPS.KhoaDaSanSang)
                {
                    thieuTepKhoa = true;
                    WriteFallbackLog("Thiếu tệp khóa", Module_TepKhoa.LoiGanNhat ?? "Không nạp được khóa.");
                    return false;
                }
                Module_DanduongGPS.HanhTrinhToiColombiaAsync().GetAwaiter().GetResult();
                // TaoBangNhatKy đã chuyển sang chạy song song ở Main (bước 7).
                return true;
            }
            catch (Exception ex)
            {
                LogException("Core Init Fatal", ex);
                return false;
            }
            finally
            {
                sw.Stop();
                if (sw.ElapsedMilliseconds > SlowStartupThresholdMs)
                {
                    WriteFallbackLog("Khởi động chậm",
                        $"Khởi tạo hệ thống lõi mất {sw.ElapsedMilliseconds} ms.");
                }
            }
        }
        private static void ShowKeyFileFailure()
        {
            MessageBox.Show(
                Module_TepKhoa.ThongBaoThieuKhoa() + $"\n\nNếu cần hỗ trợ, vui lòng liên hệ {AdminContact}.",
                "Thiếu khóa bảo mật",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        private static void ShowDatabaseRecoveryWarning()
        {
            try
            {
                var canhBao = Module_KhoiTaoCSDL.CanhBaoKhoiPhucCSDL;
                if (canhBao.Count == 0) return;
                MessageBox.Show(
                    "Phần mềm phát hiện tệp cơ sở dữ liệu bị mất hoặc hỏng và đã tự xử lý để có thể khởi động:\n\n" +
                    "• " + string.Join("\n• ", canhBao) + "\n\n" +
                    "Dữ liệu đang hiển thị có thể KHÔNG phải dữ liệu làm việc gần nhất.\n" +
                    "Khắc phục: vào Cài đặt → Khôi phục CSDL và chọn bản sao lưu gần nhất trong danh sách.\n\n" +
                    $"Nếu cần hỗ trợ, vui lòng liên hệ {AdminContact}.",
                    "Cảnh báo dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                LogException("Hiện cảnh báo khôi phục CSDL", ex);
            }
        }
        private static void ShowStartupFailure()
        {
            MessageBox.Show(
                "Tình trạng: Thiếu CSDL khởi động hoặc thư viện hệ thống\n\n" +
                "Lỗi: Không thể kết nối cơ sở dữ liệu hoặc cấu hình không hợp lệ.\n\n" +
                "Khắc phục: Vui lòng đóng phần mềm và mở lại để hệ thống khôi phục lại cấu hình mặc định.\n\n" +
                $"Nếu lỗi vẫn tiếp diễn, vui lòng liên hệ {AdminContact}.",
                "Thông báo sự cố không hồi đáp",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        //  TÁC VỤ NỀN
        // Hẹn giờ ở lần Idle đầu tiên (sau khi Form1 đã dựng xong), chờ thêm một nhịp
        // BackgroundStartDelayMs để không tranh CPU/đĩa với Form1, rồi chạy TUẦN TỰ trong
        // một task duy nhất -> không tranh writer SQLite với Form1 và các bước không giẫm
        // chân nhau. Khi thoát app, token bị hủy nên các bước chưa chạy sẽ được bỏ qua.
        private static void ScheduleBackgroundMaintenance()
        {
            EventHandler? onFirstIdle = null;
            onFirstIdle = (_, _) =>
            {
                Application.Idle -= onFirstIdle;
                _backgroundTask = Task.Run(() => RunBackgroundMaintenanceAsync(ShutdownCts.Token));
            };
            Application.Idle += onFirstIdle;
        }
        private static async Task RunBackgroundMaintenanceAsync(CancellationToken ct)
        {
            try
            {
                await Task.Delay(BackgroundStartDelayMs, ct).ConfigureAwait(false);
                // Việc nặng đã chuyển khỏi đường khởi động: băm SHA-256 kho lõi,
                // đồng bộ 2 chiều thư mục công cụ/hướng dẫn, khóa ReadOnly.
                // YÊU CẦU: Module_KhoiTaoCSDL phải có hàm public static void BaoTriSauKhoiDong().
                RunBackgroundStep(ct, "Tuần tra & đồng bộ CSDL",
                    () => Module_KhoiTaoCSDL.BaoTriSauKhoiDong());
                RunBackgroundStep(ct, "Tường lửa bảo vệ hệ thống",
                    () => Module_KhoiTaoCSDL.TuongLuaBaoVeHeThong(AppContext.BaseDirectory));
                RunBackgroundStep(ct, "Chính sách làm sạch",
                    () => Module_KhoiTaoCSDL.ChinhSachLamSach());
                RunBackgroundStep(ct, "Đồng bộ Hướng dẫn sử dụng", () =>
                {
                    Module_HuongDanSuDung.SyncMasterVersion();
                    Module_HuongDanSuDung.UpdateLocal();
                });
                RunBackgroundStep(ct, "Dọn nhật ký cũ", PruneOldFallbackLogs);
            }
            catch (OperationCanceledException)
            {
                // Ứng dụng đang đóng: bỏ qua các bước còn lại.
            }
            catch (Exception ex)
            {
                LogException("Tác vụ nền thất bại", ex);
            }
        }
        private static void RunBackgroundStep(CancellationToken ct, string stepName, Action action)
        {
            if (ct.IsCancellationRequested) return;
            // Người dùng đang bấm Đăng nhập (BCrypt + dựng Form2): nhường CPU/đĩa, xong rồi mới chạy bước kế tiếp.
            using (Module_DoHieuNang.Khoang("Bảo trì nền nhường đăng nhập trước bước: " + stepName))
                Module_UuTienDangNhap.NhuongNen(ct);
            if (ct.IsCancellationRequested) return;
            // Chế độ "nền" của Windows cho riêng luồng này: hạ cả ưu tiên CPU lẫn ưu tiên I/O đĩa/bộ nhớ trong lúc chạy bước
            // (băm SHA-256 kho lõi, đồng bộ thư mục... rất nặng đĩa, nhất là máy có phần mềm diệt virus quét từng lần mở tệp).
            // BEGIN/END phải cùng một luồng -> bước chạy ĐỒNG BỘ giữa hai lệnh, không có await ở giữa.
            bool cheDoNen = false;
            try { cheDoNen = SetThreadPriority(GetCurrentThread(), THREAD_MODE_BACKGROUND_BEGIN); }
            catch { /* không hỗ trợ -> chạy ưu tiên thường như cũ */ }
            try
            {
                using (Module_DoHieuNang.Khoang("Bảo trì nền: " + stepName))
                    RunNonCritical(stepName, action);
            }
            finally
            {
                if (cheDoNen)
                {
                    try { SetThreadPriority(GetCurrentThread(), THREAD_MODE_BACKGROUND_END); }
                    catch { }
                }
            }
        }
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentThread();
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetThreadPriority(IntPtr hThread, int nPriority);
        private const int THREAD_MODE_BACKGROUND_BEGIN = 0x00010000;
        private const int THREAD_MODE_BACKGROUND_END = 0x00020000;
        // Chạy một bước KHÔNG bắt buộc: lỗi được ghi nhật ký, ứng dụng vẫn chạy tiếp.
        // Bước nào chạy quá SlowStepThresholdMs sẽ được ghi vào file log để dễ tìm điểm nghẽn.
        private static void RunNonCritical(string stepName, Action action)
        {
            long start = Stopwatch.GetTimestamp();
            try
            {
                action();
            }
            catch (Exception ex)
            {
                LogException($"Bước không bắt buộc thất bại: {stepName}", ex);
            }
            finally
            {
                double ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                if (ms > SlowStepThresholdMs)
                    WriteFallbackLog("Bước chạy chậm", $"{stepName}: {ms:F0} ms");
            }
        }
        //  SINGLE INSTANCE
        // Tên mutex xác định (cùng máy -> cùng tên) để các tiến trình nhận ra nhau.
        private static string BuildMutexName(string purpose)
        {
            try
            {
                string raw = $"{Environment.MachineName}|PMTD2026_SALT_SECURE|{purpose}";
                byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
                return @"Local\" + Convert.ToHexString(hash, 0, 16); // 16 byte đầu = 32 ký tự hex
            }
            catch
            {
                return $@"Local\PMTD2026_FB_{purpose}";
            }
        }
        /// <summary>
        /// Máy dev (MEOCAM) cho chạy tối đa 3 instance, máy người dùng chỉ 1.
        /// </summary>
        private static bool AcquireInstanceLock()
        {
            bool isDeveloper = string.Equals(
                Environment.MachineName,
                DeveloperComputerName,
                StringComparison.OrdinalIgnoreCase);
            int maxInstances = isDeveloper ? MaxInstancesOnDeveloper : MaxInstancesOnClient;
            for (int slot = 1; slot <= maxInstances; slot++)
            {
                Mutex? mutex = null;
                try
                {
                    // Tạo mutex KHÔNG sở hữu, rồi xin quyền bằng WaitOne: luôn giữ được
                    // tham chiếu để giải phóng đúng, kể cả khi gặp AbandonedMutexException.
                    mutex = new Mutex(initiallyOwned: false, name: $"{AppMutexName}_Slot{slot}");
                    bool acquired;
                    try
                    {
                        acquired = mutex.WaitOne(0);
                    }
                    catch (AbandonedMutexException)
                    {
                        // Tiến trình giữ slot trước đó đã crash -> quyền sở hữu chuyển sang ta.
                        acquired = true;
                    }
                    if (acquired)
                    {
                        _appMutex = mutex;
                        return true;
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    // Slot do tiến trình khác quyền giữ (vd: bản Run as Admin) -> coi như bận.
                    WriteFallbackLog("Không thể truy cập Mutex phiên bản", ex.ToString());
                }
                catch (Exception ex)
                {
                    WriteFallbackLog("Lỗi kiểm soát phiên bản", ex.ToString());
                }
                mutex?.Dispose(); // slot bận, thử slot kế tiếp
            }
            return false;
        }
        // Mutex phụ để nhiều lần bấm mở app không bật chồng nhiều hộp thoại thông báo.
        private static void ShowSingleInstanceMessage()
        {
            using var msgMutex = new Mutex(initiallyOwned: false, name: MsgMutexName);
            bool acquired;
            try
            {
                acquired = msgMutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                acquired = true;
            }
            if (!acquired) return; // đã có hộp thoại khác đang hiện
            try
            {
                MessageBox.Show("Phần mềm hiện đang chạy trong hệ thống.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                msgMutex.ReleaseMutex();
            }
        }
        //  XỬ LÝ LỖI TOÀN CỤC
        private static void ConfigureGlobalExceptionHandlers()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            // Lỗi trên UI thread
            Application.ThreadException += (_, e) => HandleUiThreadException(e.Exception);
            // Lỗi trên thread nền: CLR sẽ đóng tiến trình sau handler này.
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                if (LaLoiPostVaoFormDaDong(ex))
                {
                    // Tiếp nối await bị gửi về form đã đóng (thường gặp lúc thoát app): vô hại, chỉ ghi nhật ký.
                    LogException("Cảnh báo (bỏ qua) - tiếp nối async gửi vào form đã đóng", ex);
                    return;
                }
                LogException("Fatal Error", ex);
                ShowErrorDialogOnce("Lỗi Hệ Thống", ex?.Message ?? "Ứng dụng buộc phải đóng.");
            };
            // Task bị bỏ quên không await: ghi log rồi đánh dấu đã quan sát.
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                LogException("Unobserved Task Exception", e.Exception);
                e.SetObserved();
            };
        }
        private static bool LaLoiPostVaoFormDaDong(Exception? ex)
            => ex is InvalidOperationException
               && ex.Message.Contains("window handle has been created", StringComparison.OrdinalIgnoreCase)
               && (ex.StackTrace?.Contains("WindowsFormsSynchronizationContext.Post", StringComparison.Ordinal) ?? false);
        private static void HandleUiThreadException(Exception ex)
        {
            LogException("UI Error", ex);
            // Chống vòng lặp lỗi (vd: lỗi trong OnPaint sẽ bắn lại liên tục).
            long now = Environment.TickCount64;
            if (now - _uiErrorWindowStart > UiErrorBurstWindowMs)
            {
                _uiErrorWindowStart = now;
                _uiErrorCount = 0;
            }
            if (++_uiErrorCount >= UiErrorBurstLimit)
            {
                ShowErrorDialogOnce("Lỗi nghiêm trọng",
                    "Phát hiện lỗi giao diện lặp lại liên tục. Ứng dụng sẽ đóng để bảo vệ dữ liệu.");
                try { Module_NhatKy.FlushQueueToDatabase(); } catch { }
                Environment.Exit(ExitFatal);
                return;
            }
            ShowErrorDialogOnce("Lỗi Giao Diện", ex.Message);
        }
        private static void HandleFatalError(Exception ex)
        {
            LogException("Crash", ex);
            try
            {
                MessageBox.Show(
                    "Lỗi nghiêm trọng. Ứng dụng sẽ đóng để bảo vệ dữ liệu.\n\n" +
                    "Chi tiết kỹ thuật đã được ghi lại để hỗ trợ khắc phục.",
                    "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            catch { }
        }
        // Chỉ cho phép MỘT hộp thoại lỗi tại một thời điểm.
        private static void ShowErrorDialogOnce(string title, string message)
        {
            if (Interlocked.CompareExchange(ref _errorDialogOpen, 1, 0) != 0)
                return;
            try
            {
                MessageBox.Show(
                    $"Chi tiết lỗi: {message}\n\nThông tin kỹ thuật đã được ghi vào nhật ký.",
                    title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
            finally
            {
                Interlocked.Exchange(ref _errorDialogOpen, 0);
            }
        }
        //  NHẬT KÝ (2 TẦNG: CSDL + FILE DỰ PHÒNG)
        private static void SafeLog(string user, string action, string note)
        {
            try { Module_NhatKy.GhiNhatKy(user, action, note); } catch { }
        }
        // Ghi cả CSDL (message ngắn) lẫn file dự phòng (stack trace đầy đủ).
        // File dự phòng sống sót cả khi CSDL chưa sẵn sàng hoặc hỏng.
        private static void LogException(string action, Exception? ex)
        {
            WriteFallbackLog(action, ex?.ToString() ?? "Unknown");
            SafeLog("SYSTEM", action, ex?.Message ?? "Unknown");
        }
        private static void WriteFallbackLog(string title, string detail)
            => Module_NhatKyNhaPhatTrien.GhiTep("error",
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {title}{Environment.NewLine}" +
                $"{detail}{Environment.NewLine}{Environment.NewLine}");
        private static void PruneOldFallbackLogs()
        {
            Module_NhatKyNhaPhatTrien.DonDep(LogRetentionDays);
            Module_NhatKyNhaPhatTrien.ChuyenDuLieuCuTuAppData();
        }
        //  DỌN DẸP & TIỆN ÍCH HỆ THỐNG
        private static void CleanupResources(bool isPrimary)
        {
            // 1. Báo tác vụ nền dừng (bước chưa chạy sẽ bị bỏ qua), rồi chờ bước đang chạy
            //    (nếu có) hoàn tất tối đa vài giây để không bị kill giữa chừng khi ghi CSDL.
            Module_DoHieuNang.KetThuc("thoát phần mềm", dongBo: true);   // phiên đo còn dở (nếu có) vẫn được ghi báo cáo
            try { ShutdownCts.Cancel(); } catch { }
            try { _backgroundTask?.Wait(BackgroundShutdownWaitMs); } catch { }
            // 2. Xả hàng đợi nhật ký xuống CSDL.
            try
            {
                Module_NhatKy.FlushQueueToDatabase();
            }
            catch (Exception ex)
            {
                WriteFallbackLog("Xả nhật ký thất bại", ex.ToString());
            }
            // 3. Giải phóng mutex (đúng thread đã sở hữu - Main thread).
            Mutex? mutex = _appMutex;
            _appMutex = null;
            if (mutex is null) return;
            if (isPrimary)
            {
                try { mutex.ReleaseMutex(); } catch { }
            }
            mutex.Dispose();
        }
        // Chỉ ghi registry khi giá trị hiện tại chưa đúng, tránh I/O thừa mỗi lần khởi động.
        private static void SetBrowserFeatureControl()
        {
            try
            {
                string appName = Path.GetFileName(Environment.ProcessPath ?? Application.ExecutablePath);
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION");
                object? current = key.GetValue(appName);
                if (current is not int currentValue || currentValue != IeEmulationMode)
                {
                    key.SetValue(appName, IeEmulationMode, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                WriteFallbackLog("Không ghi được FEATURE_BROWSER_EMULATION", ex.ToString());
            }
        }
    }
}
/////Phiên bản 3 (Tối ưu khởi động: bỏ khôi phục CSDL trùng lặp, đẩy tuần tra/đồng bộ nặng xuống nền)
/////