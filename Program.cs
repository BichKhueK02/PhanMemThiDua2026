using Microsoft.Win32;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
namespace PhanMemThiDua2026
{
    internal static class Program
    {
        // THIẾT LẬP HỆ THỐNG (IMMUTABLE)
        private static Mutex? _appMutex;
        private static readonly string AppMutexName = BuildMutexName("CORE");
        private static readonly string MsgMutexName = BuildMutexName("MSG");
        [STAThread]
        static void Main()
        {
            // 1. ĐĂNG KÝ BẪY LỖI TOÀN CỤC NGAY ĐẦU TIÊN
            //    (Trước đây bước này nằm sau AcquireInstanceLock/ApplicationConfiguration.Initialize
            //    -> nếu 2 bước đó ném lỗi trên thread nền thì handler chưa kịp gắn.
            //    Đưa lên đầu để không bỏ sót bất kỳ exception nào trong toàn bộ vòng đời app.)
            ConfigureGlobalExceptionHandlers();
            // 2. THIẾT LẬP NỀN TẢNG WINFORMS
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            SetBrowserFeatureControl();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // 3. TĂNG ĐỘ ƯU TIÊN TIẾN TRÌNH
            using (Process p = Process.GetCurrentProcess())
            {
                p.PriorityClass = ProcessPriorityClass.AboveNormal;
            }
            bool isPrimaryInstance = false;
            try
            {
                // 4. KIỂM SOÁT SINGLE INSTANCE
                isPrimaryInstance = AcquireInstanceLock();
                if (!isPrimaryInstance)
                {
                    ShowSingleInstanceMessage();
                    return;
                }
                // 5. KHỞI TẠO CẤU HÌNH WINFORMS
                ApplicationConfiguration.Initialize();
                // 6. KIỂM TRA TỶ LỆ MÀN HÌNH
                Module_KhoiDongTrangChu.KiemTraVaCanhBaoTyLeManHinh();
                // 7. KHỞI TẠO HỆ THỐNG LÕI + CSDL
                if (!KiemTraTrangThaiKhoiDong())
                    return;
                // Đọc cấu hình màu từ CSDL lên RAM sau khi CSDL đã sẵn sàng
                Module_GiaoDien.KhoiDongDocTheme();
                // Nạp màu từ CSDL cho menu chuột phải ngay khi CSDL đã sẵn sàng
                Module_MenuChuotPhai.KhoiTaoMauTuCSDL(Module_DanduongGPS.DuongDanCSDL2);
                // 8. ĐỒNG BỘ DỮ LIỆU KHÔNG BẮT BUỘC
                try
                {
                    Module_HuongDanSuDung.SyncMasterVersion();
                    Module_HuongDanSuDung.UpdateLocal();
                }
                catch (Exception ex)
                {
                    SafeLog("SYSTEM", "Sync Error", $"Lỗi đồng bộ Hướng dẫn sử dụng: {ex.Message}");
                }
                // 9. TÁC VỤ NỀN SAU KHI CORE ĐÃ SẴN SÀNG
                //    Lưu ý: nếu các hàm dưới đây ghi vào cùng CSDL SQLite mà Form1 cũng
                //    đọc/ghi ngay khi khởi tạo, cân nhắc thêm khóa/hàng đợi để tránh
                //    tranh chấp writer (SQLite chỉ cho 1 writer tại một thời điểm).
                Task.Run(() =>
                {
                    try
                    {
                        Module_KhoiTaoCSDL.TuongLuaBaoVeHeThong(AppContext.BaseDirectory);
                        Module_KhoiTaoCSDL.ChinhSachLamSach();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Lỗi tự vệ hệ thống ngầm: " + ex.Message);
                    }
                });
                // 10. KHỞI CHẠY GIAO DIỆN CHÍNH
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                HandleFatalError(ex);
            }
            finally
            {
                // 11. GIẢI PHÓNG TÀI NGUYÊN KHI ỨNG DỤNG KẾT THÚC
                CleanupResources(isPrimaryInstance);
            }
        }
        private static bool KiemTraTrangThaiKhoiDong()
        {
            if (!KhoiTaoHeThong())
            {
                MessageBox.Show(
                    "Tình trạng: Thiếu CSDL khởi động hoặc thư viện hệ thống\n\n" +
                    "Lỗi: Không thể kết nối cơ sở dữ liệu hoặc cấu hình không hợp lệ.\n\n" +
                    "Khắc phục: Vui lòng đóng phần mềm và mở lại để hệ thống khôi phục lại cấu hình mặc định.\n\n" +
                    "Nếu lỗi vẫn tiếp diễn, vui lòng liên hệ Admin TrungKien: 0975.287.973.",
                    "Thông báo sự cố không hồi đáp",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
            return true;
        }
        // XỬ LÝ BẢO MẬT & ĐỊNH DANH (ZERO-ALLOCATION)
        private static string BuildMutexName(string purpose)
        {
            try
            {
                string raw = $"{Environment.MachineName}|PMTD2026_SALT_SECURE|{purpose}";
                byte[] inputBytes = Encoding.UTF8.GetBytes(raw);
                byte[] hashBytes = SHA256.HashData(inputBytes);
                Span<char> hashChars = stackalloc char[32];
                for (int i = 0; i < 16; i++)
                {
                    hashBytes[i].TryFormat(hashChars.Slice(i * 2), out _, "X2");
                }
                return "Local\\" + new string(hashChars);
            }
            catch
            {
                return $"Local\\PMTD2026_FB_{purpose}";
            }
        }
        // QUẢN LÝ KHỞI TẠO & HIỆU SUẤT
        //
        // Ghi chú hiệu năng: các lời gọi .GetAwaiter().GetResult() dưới đây là BLOCKING
        // có chủ đích. Main() chưa gọi Application.Run nên chưa có
        // WindowsFormsSynchronizationContext -> việc block ở đây AN TOÀN hơn so với
        // chuyển Main() thành async (vì continuation sau await có thể nhảy sang thread
        // pool, phá vỡ yêu cầu STA khi gọi Application.Run(new Form1())).
        // Đánh đổi: toàn bộ UI "đứng hình" cho tới khi CSDL init xong. Nếu thời gian init
        // dài, nên cân nhắc splash screen chạy trên thread riêng thay vì tối ưu tại đây.
        private static bool KhoiTaoHeThong()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                Module_DanduongGPS.XinTraLaiThoiGianNapKeyBase64();
                Module_DanduongGPS.LoiChaoTuSiberia();
                Module_DanduongGPS.HanhTrinhToiColombiaAsync().GetAwaiter().GetResult();
                Module_KhoiTaoCSDL.BinhMinhOSantoriniAsync().GetAwaiter().GetResult();
                Module_NhatKy.TaoBangNhatKy();
                sw.Stop();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Core Init Fatal] {ex.Message}");
                return false;
            }
        }
        // LOGGING & EXCEPTION HANDLING (STABILITY)
        private static void ConfigureGlobalExceptionHandlers()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
            {
                SafeLog("SYSTEM", "UI Error", e.Exception.Message);
                ShowErrorDialog("Lỗi Giao Diện", e.Exception.Message);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                SafeLog("SYSTEM", "Fatal Error", ex?.Message ?? "Unknown");
                ShowErrorDialog("Lỗi Hệ Thống", ex?.Message ?? "Ứng dụng buộc phải đóng.");
            };
        }
        private static void SafeLog(string user, string action, string note)
        {
            try { Module_NhatKy.GhiNhatKy(user, action, note); } catch { }
        }
        private static void ShowSingleInstanceMessage()
        {
            using Mutex msgMutex = new Mutex(true, MsgMutexName, out bool created);
            if (created && msgMutex.WaitOne(0))
            {
                MessageBox.Show("Phần mềm hiện đang chạy trong hệ thống.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                msgMutex.ReleaseMutex();
            }
        }
        private static void ShowErrorDialog(string title, string message)
        {
            MessageBox.Show($"Chi tiết lỗi: {message}", title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        private static void HandleFatalError(Exception ex)
        {
            SafeLog("SYSTEM", "Crash", ex.Message);
            MessageBox.Show("Lỗi nghiêm trọng. Ứng dụng sẽ đóng để bảo vệ dữ liệu.",
                "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }
        private static void CleanupResources(bool isPrimary)
        {
            if (isPrimary)
            {
                try { _appMutex?.ReleaseMutex(); } catch { }
            }
            _appMutex?.Dispose();
            try { Module_NhatKy.FlushQueueToDatabase(); } catch { }
        }
        // Chỉ ghi registry nếu giá trị hiện tại chưa đúng, tránh I/O thừa mỗi lần khởi động.
        private static void SetBrowserFeatureControl()
        {
            const int TargetIeMode = 11001;
            try
            {
                string appName = Path.GetFileName(Application.ExecutablePath);
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION");
                object? current = key.GetValue(appName);
                if (current is not int currentValue || currentValue != TargetIeMode)
                {
                    key.SetValue(appName, TargetIeMode, RegistryValueKind.DWord);
                }
            }
            catch { }
        }
        /// <summary>
        /// Giới hạn số instance được phép chạy đồng thời:
        /// - Chạy từ thư mục build (bin\Debug hoặc bin\Release, kể cả khi SDK-style
        ///   project chèn thêm thư mục TFM như net8.0-windows ở giữa) → tối đa 2 instance,
        ///   để tiện vừa debug vừa để 1 bản chạy nền tham chiếu.
        /// - Mọi vị trí khác (bản đã cài đặt/publish cho người dùng cuối) → đúng 1 instance.
        /// </summary>
        private static bool AcquireInstanceLock()
        {
            int maxInstances = IsRunningFromBuildOutput(Application.ExecutablePath) ? 2 : 1;
            for (int slot = 1; slot <= maxInstances; slot++)
            {
                string slotMutexName = $"{AppMutexName}_Slot{slot}";
                // Tạo mutex KHÔNG sở hữu trước, rồi xin quyền sở hữu riêng bằng WaitOne.
                // Cách này đảm bảo ta luôn giữ được tham chiếu mutex để giải phóng đúng
                // sau này, kể cả khi WaitOne ném AbandonedMutexException — khác với cách
                // gộp chung new Mutex(true, name, out _) vốn có thể làm mất tham chiếu
                // nếu exception xảy ra ngay trong constructor.
                var mutex = new Mutex(initiallyOwned: false, name: slotMutexName);
                bool acquired;
                try
                {
                    acquired = mutex.WaitOne(0);
                }
                catch (AbandonedMutexException)
                {
                    // Tiến trình giữ slot này trước đó bị crash mà không giải phóng —
                    // ownership coi như đã chuyển cho ta.
                    acquired = true;
                }
                if (acquired)
                {
                    _appMutex = mutex;
                    return true;
                }
                mutex.Dispose(); // Slot này đang bị chiếm, thử slot kế tiếp (nếu còn).
            }
            return false;
        }
        /// <summary>
        /// Nhận diện ứng dụng đang chạy trực tiếp từ thư mục build (bin/Debug hoặc
        /// bin/Release — tức chạy từ Visual Studio / máy dev), khác với bản đã cài
        /// đặt/publish. Duyệt qua từng đoạn thư mục thay vì chỉ lấy thư mục cha trực
        /// tiếp của exe, để không bị sai khi có thêm thư mục TFM xen giữa
        /// (vd: bin/Debug/net8.0-windows/App.exe của SDK-style project hiện đại).
        /// </summary>
        private static bool IsRunningFromBuildOutput(string exePath)
        {
            string? exeDirectory = Path.GetDirectoryName(exePath);
            if (string.IsNullOrEmpty(exeDirectory))
                return false;
            string[] segments = exeDirectory.Split(
                new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < segments.Length - 1; i++)
            {
                bool isBinSegment = string.Equals(segments[i], "bin", StringComparison.OrdinalIgnoreCase);
                bool isConfigSegment =
                    string.Equals(segments[i + 1], "Debug", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(segments[i + 1], "Release", StringComparison.OrdinalIgnoreCase);
                if (isBinSegment && isConfigSegment)
                    return true;
            }
            return false;
        }
    }
}
