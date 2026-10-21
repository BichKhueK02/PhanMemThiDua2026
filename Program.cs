///Phiên bản 2 (Tối ưu hiệu suất do Cloude AI
///
using Microsoft.Win32;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
namespace PhanMemThiDua2026
{
    internal static class Program
    {
        // ======================================================================
        //  HẰNG SỐ & CẤU HÌNH
        // ======================================================================
        private const string DeveloperComputerName = "MEOCAM";
        private const int MaxInstancesOnDeveloper = 3;
        private const int MaxInstancesOnClient = 1;
        private const int ExitOk = 0;
        private const int ExitFatal = 1;
        private const int ExitAlreadyRunning = 2;
        private const int ExitInitFailed = 3;
        private const string AdminContact = "Admin TrungKien: 0975.287.973";
        private const string LogFolderName = "PhanMemThiDua2026";
        private const int IeEmulationMode = 11001;        // IE11 edge mode cho WebBrowser
        private const int SlowStartupThresholdMs = 1500;  // ngưỡng ghi log "khởi động chậm" (cả khối lõi)
        private const int SlowStepThresholdMs = 300;      // ngưỡng ghi log từng bước chậm
        private const int BackgroundStartDelayMs = 1500;  // nhường CPU/đĩa cho Form1 dựng xong rồi mới chạy nền
        private const int BackgroundShutdownWaitMs = 3000;
        private const int LogRetentionDays = 30;
        // Bộ chống "bão lỗi" giao diện: >= 5 lỗi UI trong 5 giây => đóng app an toàn
        private const int UiErrorBurstLimit = 5;
        private const long UiErrorBurstWindowMs = 5000;
        // ======================================================================
        //  TRẠNG THÁI TOÀN CỤC
        // ======================================================================
        private static Mutex? _appMutex;
        private static Task? _backgroundTask;
        private static readonly CancellationTokenSource ShutdownCts = new();
        private static readonly string AppMutexName = BuildMutexName("CORE");
        private static readonly string MsgMutexName = BuildMutexName("MSG");
        private static readonly object FileLogLock = new();
        private static readonly string FallbackLogDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            LogFolderName, "Logs");
        private static int _errorDialogOpen;   // 0/1 - chống chồng hộp thoại lỗi
        private static int _uiErrorCount;      // chỉ truy cập từ UI thread
        private static long _uiErrorWindowStart;
        // ======================================================================
        //  ĐIỂM VÀO
        // ======================================================================
        [STAThread]
        private static int Main()
        {
            // 1. Bẫy lỗi toàn cục phải gắn TRƯỚC mọi thứ khác.
            ConfigureGlobalExceptionHandlers();
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
                // 4. Phải xong trước khi có WebBrowser đầu tiên được tạo
                SetBrowserFeatureControl();
                // 5. Cảnh báo tỷ lệ màn hình: lỗi ở đây không được phép làm sập app
                RunNonCritical("Kiểm tra tỷ lệ màn hình",
                    () => Module_KhoiDongTrangChu.KiemTraVaCanhBaoTyLeManHinh());
                // 6. Hệ thống lõi + CSDL (bắt buộc)
                if (!InitializeCore())
                {
                    ShowStartupFailure();
                    return ExitInitFailed;
                }
                // 7. Nạp theme/màu từ CSDL: cần có TRƯỚC khi Form1 hiện để không bị nháy màu.
                RunNonCritical("Nạp theme", () => Module_GiaoDien.KhoiDongDocTheme());
                RunNonCritical("Nạp màu menu chuột phải",
                    () => Module_MenuChuotPhai.KhoiTaoMauTuCSDL(Module_DanduongGPS.DuongDanCSDL2));
                // 8. Tác vụ nền (tường lửa, làm sạch, đồng bộ Hướng dẫn sử dụng...)
                //    chạy sau khi giao diện đã hiện.
                ScheduleBackgroundMaintenance();
                // 9. Giao diện chính
                Application.Run(new Form1());
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
        // ======================================================================
        //  KHỞI TẠO
        // ======================================================================
        private static void ConfigureWinForms()
        {
            // Initialize() (sinh theo .csproj) đã gọi EnableVisualStyles +
            // SetCompatibleTextRenderingDefault + font mặc định, nên không gọi lại ở đây.
            ApplicationConfiguration.Initialize();
            // Mọi thiết lập DPI phải xong trước khi có bất kỳ cửa sổ nào được tạo.
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        }
        // Ghi chú: .GetAwaiter().GetResult() ở đây là BLOCKING có chủ đích. Main() chưa gọi
        // Application.Run nên chưa có WindowsFormsSynchronizationContext -> không deadlock,
        // và giữ nguyên thread STA cho Application.Run(new Form1()).
        // Đánh đổi: UI chưa hiện cho tới khi CSDL init xong.
        private static bool InitializeCore()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                Module_DanduongGPS.XinTraLaiThoiGianNapKeyBase64();
                Module_DanduongGPS.LoiChaoTuSiberia();
                Module_DanduongGPS.HanhTrinhToiColombiaAsync().GetAwaiter().GetResult();
                Module_KhoiTaoCSDL.BinhMinhOSantoriniAsync().GetAwaiter().GetResult();
                Module_NhatKy.TaoBangNhatKy();
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
        // ======================================================================
        //  TÁC VỤ NỀN
        // ======================================================================
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
            RunNonCritical(stepName, action);
        }
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
        // ======================================================================
        //  SINGLE INSTANCE
        // ======================================================================
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
        // ======================================================================
        //  XỬ LÝ LỖI TOÀN CỤC
        // ======================================================================
        private static void ConfigureGlobalExceptionHandlers()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            // Lỗi trên UI thread
            Application.ThreadException += (_, e) => HandleUiThreadException(e.Exception);
            // Lỗi trên thread nền: CLR sẽ đóng tiến trình sau handler này.
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                var ex = e.ExceptionObject as Exception;
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
        // ======================================================================
        //  NHẬT KÝ (2 TẦNG: CSDL + FILE DỰ PHÒNG)
        // ======================================================================
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
        {
            try
            {
                lock (FileLogLock)
                {
                    Directory.CreateDirectory(FallbackLogDir);
                    string file = Path.Combine(FallbackLogDir, $"error-{DateTime.Now:yyyyMMdd}.log");
                    string entry =
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {title}{Environment.NewLine}" +
                        $"{detail}{Environment.NewLine}{Environment.NewLine}";
                    File.AppendAllText(file, entry, Encoding.UTF8);
                }
            }
            catch { }
        }
        private static void PruneOldFallbackLogs()
        {
            try
            {
                if (!Directory.Exists(FallbackLogDir)) return;
                DateTime threshold = DateTime.Now.AddDays(-LogRetentionDays);
                foreach (string file in Directory.EnumerateFiles(FallbackLogDir, "error-*.log"))
                {
                    if (File.GetLastWriteTime(file) < threshold)
                        File.Delete(file);
                }
            }
            catch { }
        }
        // ======================================================================
        //  DỌN DẸP & TIỆN ÍCH HỆ THỐNG
        // ======================================================================
        private static void CleanupResources(bool isPrimary)
        {
            // 1. Báo tác vụ nền dừng (bước chưa chạy sẽ bị bỏ qua), rồi chờ bước đang chạy
            //    (nếu có) hoàn tất tối đa vài giây để không bị kill giữa chừng khi ghi CSDL.
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