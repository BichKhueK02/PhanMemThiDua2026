//using Microsoft.Win32;
//using System.Diagnostics;
//using System.Security.Cryptography;
//using System.Text;
//namespace PhanMemThiDua2026
//{
//    internal static class Program
//    {
//        // THIẾT LẬP HỆ THỐNG (IMMUTABLE)
//        private static Mutex? _appMutex;
//        private static readonly string AppMutexName = BuildMutexName("CORE");
//        private static readonly string MsgMutexName = BuildMutexName("MSG");
//        [STAThread]
//        static void Main()
//        {
//            // 1. ĐĂNG KÝ BẪY LỖI TOÀN CỤC NGAY ĐẦU TIÊN
//            //    (Trước đây bước này nằm sau AcquireInstanceLock/ApplicationConfiguration.Initialize
//            //    -> nếu 2 bước đó ném lỗi trên thread nền thì handler chưa kịp gắn.
//            //    Đưa lên đầu để không bỏ sót bất kỳ exception nào trong toàn bộ vòng đời app.)
//            ConfigureGlobalExceptionHandlers();
//            // 2. THIẾT LẬP NỀN TẢNG WINFORMS
//            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
//            SetBrowserFeatureControl();
//            Application.EnableVisualStyles();
//            Application.SetCompatibleTextRenderingDefault(false);
//            // 3. TĂNG ĐỘ ƯU TIÊN TIẾN TRÌNH
//            using (Process p = Process.GetCurrentProcess())
//            {
//                p.PriorityClass = ProcessPriorityClass.AboveNormal;
//            }
//            bool isPrimaryInstance = false;
//            try
//            {
//                // 4. KIỂM SOÁT SINGLE INSTANCE
//                isPrimaryInstance = AcquireInstanceLock();
//                if (!isPrimaryInstance)
//                {
//                    ShowSingleInstanceMessage();
//                    return;
//                }
//                // 5. KHỞI TẠO CẤU HÌNH WINFORMS
//                ApplicationConfiguration.Initialize();
//                // 6. KIỂM TRA TỶ LỆ MÀN HÌNH
//                Module_KhoiDongTrangChu.KiemTraVaCanhBaoTyLeManHinh();
//                // 7. KHỞI TẠO HỆ THỐNG LÕI + CSDL
//                if (!KiemTraTrangThaiKhoiDong())
//                    return;
//                // Đọc cấu hình màu từ CSDL lên RAM sau khi CSDL đã sẵn sàng
//                Module_GiaoDien.KhoiDongDocTheme();
//                // Nạp màu từ CSDL cho menu chuột phải ngay khi CSDL đã sẵn sàng
//                Module_MenuChuotPhai.KhoiTaoMauTuCSDL(Module_DanduongGPS.DuongDanCSDL2);
//                // 8. ĐỒNG BỘ DỮ LIỆU KHÔNG BẮT BUỘC
//                try
//                {
//                    Module_HuongDanSuDung.SyncMasterVersion();
//                    Module_HuongDanSuDung.UpdateLocal();
//                }
//                catch (Exception ex)
//                {
//                    SafeLog("SYSTEM", "Sync Error", $"Lỗi đồng bộ Hướng dẫn sử dụng: {ex.Message}");
//                }
//                // 9. TÁC VỤ NỀN SAU KHI CORE ĐÃ SẴN SÀNG
//                //    Lưu ý: nếu các hàm dưới đây ghi vào cùng CSDL SQLite mà Form1 cũng
//                //    đọc/ghi ngay khi khởi tạo, cân nhắc thêm khóa/hàng đợi để tránh
//                //    tranh chấp writer (SQLite chỉ cho 1 writer tại một thời điểm).
//                Task.Run(() =>
//                {
//                    try
//                    {
//                        Module_KhoiTaoCSDL.TuongLuaBaoVeHeThong(AppContext.BaseDirectory);
//                        Module_KhoiTaoCSDL.ChinhSachLamSach();
//                    }
//                    catch (Exception ex)
//                    {
//                        Debug.WriteLine("Lỗi tự vệ hệ thống ngầm: " + ex.Message);
//                    }
//                });
//                // 10. KHỞI CHẠY GIAO DIỆN CHÍNH
//                Application.Run(new Form1());
//            }
//            catch (Exception ex)
//            {
//                HandleFatalError(ex);
//            }
//            finally
//            {
//                // 11. GIẢI PHÓNG TÀI NGUYÊN KHI ỨNG DỤNG KẾT THÚC
//                CleanupResources(isPrimaryInstance);
//            }
//        }
//        private static bool KiemTraTrangThaiKhoiDong()
//        {
//            if (!KhoiTaoHeThong())
//            {
//                MessageBox.Show(
//                    "Tình trạng: Thiếu CSDL khởi động hoặc thư viện hệ thống\n\n" +
//                    "Lỗi: Không thể kết nối cơ sở dữ liệu hoặc cấu hình không hợp lệ.\n\n" +
//                    "Khắc phục: Vui lòng đóng phần mềm và mở lại để hệ thống khôi phục lại cấu hình mặc định.\n\n" +
//                    "Nếu lỗi vẫn tiếp diễn, vui lòng liên hệ Admin TrungKien: 0975.287.973.",
//                    "Thông báo sự cố không hồi đáp",
//                    MessageBoxButtons.OK,
//                    MessageBoxIcon.Error);
//                return false;
//            }
//            return true;
//        }
//        // XỬ LÝ BẢO MẬT & ĐỊNH DANH (ZERO-ALLOCATION)
//        private static string BuildMutexName(string purpose)
//        {
//            try
//            {
//                string raw = $"{Environment.MachineName}|PMTD2026_SALT_SECURE|{purpose}";
//                byte[] inputBytes = Encoding.UTF8.GetBytes(raw);
//                byte[] hashBytes = SHA256.HashData(inputBytes);
//                Span<char> hashChars = stackalloc char[32];
//                for (int i = 0; i < 16; i++)
//                {
//                    hashBytes[i].TryFormat(hashChars.Slice(i * 2), out _, "X2");
//                }
//                return "Local\\" + new string(hashChars);
//            }
//            catch
//            {
//                return $"Local\\PMTD2026_FB_{purpose}";
//            }
//        }
//        // QUẢN LÝ KHỞI TẠO & HIỆU SUẤT
//        //
//        // Ghi chú hiệu năng: các lời gọi .GetAwaiter().GetResult() dưới đây là BLOCKING
//        // có chủ đích. Main() chưa gọi Application.Run nên chưa có
//        // WindowsFormsSynchronizationContext -> việc block ở đây AN TOÀN hơn so với
//        // chuyển Main() thành async (vì continuation sau await có thể nhảy sang thread
//        // pool, phá vỡ yêu cầu STA khi gọi Application.Run(new Form1())).
//        // Đánh đổi: toàn bộ UI "đứng hình" cho tới khi CSDL init xong. Nếu thời gian init
//        // dài, nên cân nhắc splash screen chạy trên thread riêng thay vì tối ưu tại đây.
//        private static bool KhoiTaoHeThong()
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                Module_DanduongGPS.XinTraLaiThoiGianNapKeyBase64();
//                Module_DanduongGPS.LoiChaoTuSiberia();
//                Module_DanduongGPS.HanhTrinhToiColombiaAsync().GetAwaiter().GetResult();
//                Module_KhoiTaoCSDL.BinhMinhOSantoriniAsync().GetAwaiter().GetResult();
//                Module_NhatKy.TaoBangNhatKy();
//                sw.Stop();
//                return true;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Core Init Fatal] {ex.Message}");
//                return false;
//            }
//        }
//        // LOGGING & EXCEPTION HANDLING (STABILITY)
//        private static void ConfigureGlobalExceptionHandlers()
//        {
//            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
//            Application.ThreadException += (s, e) =>
//            {
//                SafeLog("SYSTEM", "UI Error", e.Exception.Message);
//                ShowErrorDialog("Lỗi Giao Diện", e.Exception.Message);
//            };
//            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
//            {
//                var ex = e.ExceptionObject as Exception;
//                SafeLog("SYSTEM", "Fatal Error", ex?.Message ?? "Unknown");
//                ShowErrorDialog("Lỗi Hệ Thống", ex?.Message ?? "Ứng dụng buộc phải đóng.");
//            };
//        }
//        private static void SafeLog(string user, string action, string note)
//        {
//            try { Module_NhatKy.GhiNhatKy(user, action, note); } catch { }
//        }
//        private static void ShowSingleInstanceMessage()
//        {
//            using Mutex msgMutex = new Mutex(true, MsgMutexName, out bool created);
//            if (created && msgMutex.WaitOne(0))
//            {
//                MessageBox.Show("Phần mềm hiện đang chạy trong hệ thống.",
//                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
//                msgMutex.ReleaseMutex();
//            }
//        }
//        private static void ShowErrorDialog(string title, string message)
//        {
//            MessageBox.Show($"Chi tiết lỗi: {message}", title, MessageBoxButtons.OK, MessageBoxIcon.Error);
//        }
//        private static void HandleFatalError(Exception ex)
//        {
//            SafeLog("SYSTEM", "Crash", ex.Message);
//            MessageBox.Show("Lỗi nghiêm trọng. Ứng dụng sẽ đóng để bảo vệ dữ liệu.",
//                "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
//        }
//        private static void CleanupResources(bool isPrimary)
//        {
//            if (isPrimary)
//            {
//                try { _appMutex?.ReleaseMutex(); } catch { }
//            }
//            _appMutex?.Dispose();
//            try { Module_NhatKy.FlushQueueToDatabase(); } catch { }
//        }
//        // Chỉ ghi registry nếu giá trị hiện tại chưa đúng, tránh I/O thừa mỗi lần khởi động.
//        private static void SetBrowserFeatureControl()
//        {
//            const int TargetIeMode = 11001;
//            try
//            {
//                string appName = Path.GetFileName(Application.ExecutablePath);
//                using RegistryKey key = Registry.CurrentUser.CreateSubKey(
//                    @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION");
//                object? current = key.GetValue(appName);
//                if (current is not int currentValue || currentValue != TargetIeMode)
//                {
//                    key.SetValue(appName, TargetIeMode, RegistryValueKind.DWord);
//                }
//            }
//            catch { }
//        }
//        /// <summary>
//        /// Giới hạn số instance được phép chạy đồng thời:
//        /// - Chạy từ thư mục build (bin\Debug hoặc bin\Release, kể cả khi SDK-style
//        ///   project chèn thêm thư mục TFM như net8.0-windows ở giữa) → tối đa 2 instance,
//        ///   để tiện vừa debug vừa để 1 bản chạy nền tham chiếu.
//        /// - Mọi vị trí khác (bản đã cài đặt/publish cho người dùng cuối) → đúng 1 instance.
//        /// </summary>
//        private static bool AcquireInstanceLock()
//        {
//            int maxInstances = IsRunningFromBuildOutput(Application.ExecutablePath) ? 2 : 1;
//            for (int slot = 1; slot <= maxInstances; slot++)
//            {
//                string slotMutexName = $"{AppMutexName}_Slot{slot}";
//                // Tạo mutex KHÔNG sở hữu trước, rồi xin quyền sở hữu riêng bằng WaitOne.
//                // Cách này đảm bảo ta luôn giữ được tham chiếu mutex để giải phóng đúng
//                // sau này, kể cả khi WaitOne ném AbandonedMutexException — khác với cách
//                // gộp chung new Mutex(true, name, out _) vốn có thể làm mất tham chiếu
//                // nếu exception xảy ra ngay trong constructor.
//                var mutex = new Mutex(initiallyOwned: false, name: slotMutexName);
//                bool acquired;
//                try
//                {
//                    acquired = mutex.WaitOne(0);
//                }
//                catch (AbandonedMutexException)
//                {
//                    // Tiến trình giữ slot này trước đó bị crash mà không giải phóng —
//                    // ownership coi như đã chuyển cho ta.
//                    acquired = true;
//                }
//                if (acquired)
//                {
//                    _appMutex = mutex;
//                    return true;
//                }
//                mutex.Dispose(); // Slot này đang bị chiếm, thử slot kế tiếp (nếu còn).
//            }
//            return false;
//        }
//        /// <summary>
//        /// Nhận diện ứng dụng đang chạy trực tiếp từ thư mục build (bin/Debug hoặc
//        /// bin/Release — tức chạy từ Visual Studio / máy dev), khác với bản đã cài
//        /// đặt/publish. Duyệt qua từng đoạn thư mục thay vì chỉ lấy thư mục cha trực
//        /// tiếp của exe, để không bị sai khi có thêm thư mục TFM xen giữa
//        /// (vd: bin/Debug/net8.0-windows/App.exe của SDK-style project hiện đại).
//        /// </summary>
//        private static bool IsRunningFromBuildOutput(string exePath)
//        {
//            string? exeDirectory = Path.GetDirectoryName(exePath);
//            if (string.IsNullOrEmpty(exeDirectory))
//                return false;
//            string[] segments = exeDirectory.Split(
//                new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
//                StringSplitOptions.RemoveEmptyEntries);
//            for (int i = 0; i < segments.Length - 1; i++)
//            {
//                bool isBinSegment = string.Equals(segments[i], "bin", StringComparison.OrdinalIgnoreCase);
//                bool isConfigSegment =
//                    string.Equals(segments[i + 1], "Debug", StringComparison.OrdinalIgnoreCase) ||
//                    string.Equals(segments[i + 1], "Release", StringComparison.OrdinalIgnoreCase);
//                if (isBinSegment && isConfigSegment)
//                    return true;
//            }
//            return false;
//        }
//    }
//}
using Microsoft.Win32;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace PhanMemThiDua2026
{
    internal static class Program
    {
        // =====================================================================
        //  HẰNG SỐ & CẤU HÌNH
        // =====================================================================

        // Mã thoát tiến trình (hữu ích cho script / giám sát / debug)
        private const int ExitOk = 0;
        private const int ExitFatal = 1;
        private const int ExitAlreadyRunning = 2;
        private const int ExitInitFailed = 3;

        private const string AdminContact = "Admin TrungKien: 0975.287.973";
        private const string LogFolderName = "PhanMemThiDua2026";

        private const int MaxInstancesInBuildOutput = 2; // chạy từ bin\Debug|Release
        private const int MaxInstancesInstalled = 1;     // bản cài đặt cho người dùng cuối
        private const int IeEmulationMode = 11001;       // IE11 edge mode cho WebBrowser
        private const int SlowStartupThresholdMs = 1500; // ngưỡng ghi log "khởi động chậm"
        private const int BackgroundShutdownWaitMs = 3000;
        private const int LogRetentionDays = 30;

        // Bộ chống "bão lỗi" giao diện: >= 5 lỗi UI trong 5 giây => đóng app an toàn
        private const int UiErrorBurstLimit = 5;
        private const long UiErrorBurstWindowMs = 5000;

        // Nâng ưu tiên tiến trình KHÔNG làm UI mượt hơn, còn có thể làm máy chậm đi
        // (tranh CPU với hệ thống/antivirus). Mặc định tắt; đổi thành true nếu thật sự cần.
        private static readonly bool BoostProcessPriority = false;

        // =====================================================================
        //  TRẠNG THÁI TOÀN CỤC
        // =====================================================================

        private static Mutex? _appMutex;
        private static Task? _backgroundTask;

        private static readonly string AppMutexName = BuildMutexName("CORE");
        private static readonly string MsgMutexName = BuildMutexName("MSG");

        private static readonly object FileLogLock = new();
        private static readonly string FallbackLogDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            LogFolderName, "Logs");

        private static int _errorDialogOpen;   // 0/1 - chống chồng hộp thoại lỗi
        private static int _uiErrorCount;      // chỉ truy cập từ UI thread
        private static long _uiErrorWindowStart;

        // =====================================================================
        //  ĐIỂM VÀO
        // =====================================================================

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

                // 4. Chỉ instance chính mới chạm registry / đổi ưu tiên
                TryBoostPriority();
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

                // 7. Nạp theme/màu từ CSDL (thẩm mỹ -> lỗi thì dùng mặc định, không crash)
                RunNonCritical("Nạp theme", () => Module_GiaoDien.KhoiDongDocTheme());
                RunNonCritical("Nạp màu menu chuột phải",
                    () => Module_MenuChuotPhai.KhoiTaoMauTuCSDL(Module_DanduongGPS.DuongDanCSDL2));

                // 8. Đồng bộ Hướng dẫn sử dụng (không bắt buộc)
                RunNonCritical("Đồng bộ Hướng dẫn sử dụng", () =>
                {
                    Module_HuongDanSuDung.SyncMasterVersion();
                    Module_HuongDanSuDung.UpdateLocal();
                });

                // 9. Tác vụ nền: chạy sau khi giao diện đã hiện (xem hàm để biết lý do)
                ScheduleBackgroundMaintenance();

                // 10. Giao diện chính
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
                // 11. Dọn dẹp luôn chạy, kể cả khi lỗi
                CleanupResources(isPrimaryInstance);
            }
        }

        // =====================================================================
        //  KHỞI TẠO
        // =====================================================================

        private static void ConfigureWinForms()
        {
            // Hàm sinh tự động theo cấu hình .csproj (visual styles, font mặc định, DPI...).
            // Gọi TRƯỚC để các thiết lập tường minh bên dưới luôn là giá trị cuối cùng.
            ApplicationConfiguration.Initialize();

            // Mọi thiết lập này phải xong trước khi có bất kỳ cửa sổ nào được tạo.
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
        }

        private static void TryBoostPriority()
        {
            if (!BoostProcessPriority) return;
            try
            {
                using Process p = Process.GetCurrentProcess();
                p.PriorityClass = ProcessPriorityClass.AboveNormal;
            }
            catch (Exception ex)
            {
                WriteFallbackLog("Không đặt được ưu tiên tiến trình", ex.ToString());
            }
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
                // Trước đây chỉ Debug.WriteLine -> bản Release không để lại dấu vết gì.
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

        // Tác vụ nền được hẹn giờ ở lần Idle đầu tiên (sau khi Form1 đã dựng xong) và chạy
        // TUẦN TỰ trong một task duy nhất -> không tranh writer SQLite với Form1 lúc khởi
        // tạo, và các bước nền không giẫm chân nhau.
        private static void ScheduleBackgroundMaintenance()
        {
            EventHandler? onFirstIdle = null;
            onFirstIdle = (_, _) =>
            {
                Application.Idle -= onFirstIdle;
                _backgroundTask = Task.Run(RunBackgroundMaintenance);
            };
            Application.Idle += onFirstIdle;
        }

        private static void RunBackgroundMaintenance()
        {
            RunNonCritical("Tường lửa bảo vệ hệ thống",
                () => Module_KhoiTaoCSDL.TuongLuaBaoVeHeThong(AppContext.BaseDirectory));
            RunNonCritical("Chính sách làm sạch",
                () => Module_KhoiTaoCSDL.ChinhSachLamSach());
            PruneOldFallbackLogs();
        }

        // Chạy một bước KHÔNG bắt buộc: lỗi được ghi nhật ký, ứng dụng vẫn chạy tiếp.
        private static void RunNonCritical(string stepName, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                LogException($"Bước không bắt buộc thất bại: {stepName}", ex);
            }
        }

        // =====================================================================
        //  SINGLE INSTANCE
        // =====================================================================

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
        /// Giới hạn số instance chạy đồng thời:
        /// chạy từ thư mục build -> tối đa 2; bản cài đặt/publish -> đúng 1.
        /// </summary>
        private static bool AcquireInstanceLock()
        {
            int maxInstances = IsRunningFromBuildOutput(Environment.ProcessPath ?? Application.ExecutablePath)
                ? MaxInstancesInBuildOutput
                : MaxInstancesInstalled;

            for (int slot = 1; slot <= maxInstances; slot++)
            {
                Mutex? mutex = null;
                try
                {
                    // Tạo mutex KHÔNG sở hữu, rồi xin quyền riêng bằng WaitOne: luôn giữ được
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
                catch (UnauthorizedAccessException)
                {
                    // Slot đang do tiến trình khác quyền giữ (vd: bản chạy Run as Admin,
                    // bản này chạy thường) -> coi như slot đã bị chiếm, KHÔNG được crash.
                }

                mutex?.Dispose(); // slot bận, thử slot kế tiếp
            }
            return false;
        }

        /// <summary>
        /// Nhận diện chạy trực tiếp từ thư mục build (bin\Debug|Release, kể cả khi có thư mục
        /// TFM xen giữa như net8.0-windows). Thư mục "publish" luôn bị loại trừ vì đó là bản
        /// phát hành, không phải bản dev.
        /// </summary>
        private static bool IsRunningFromBuildOutput(string? exePath)
        {
            string? exeDirectory = Path.GetDirectoryName(exePath);
            if (string.IsNullOrEmpty(exeDirectory))
                return false;

            string[] segments = exeDirectory.Split(
                new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries);

            if (Array.Exists(segments, s => s.Equals("publish", StringComparison.OrdinalIgnoreCase)))
                return false;

            for (int i = 0; i < segments.Length - 1; i++)
            {
                bool isBin = segments[i].Equals("bin", StringComparison.OrdinalIgnoreCase);
                bool isConfig =
                    segments[i + 1].Equals("Debug", StringComparison.OrdinalIgnoreCase) ||
                    segments[i + 1].Equals("Release", StringComparison.OrdinalIgnoreCase);
                if (isBin && isConfig)
                    return true;
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

        // =====================================================================
        //  XỬ LÝ LỖI TOÀN CỤC
        // =====================================================================

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

        // =====================================================================
        //  NHẬT KÝ (2 TẦNG: CSDL + FILE DỰ PHÒNG)
        // =====================================================================

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

        // =====================================================================
        //  DỌN DẸP & TIỆN ÍCH HỆ THỐNG
        // =====================================================================

        private static void CleanupResources(bool isPrimary)
        {
            // 1. Chờ tác vụ nền (tối đa vài giây) để tránh bị kill giữa chừng khi đang ghi CSDL.
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