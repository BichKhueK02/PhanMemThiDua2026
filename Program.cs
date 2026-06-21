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
            // 1. Tối ưu hóa phản hồi hệ thống (Ưu tiên hàng đầu)
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            SetBrowserFeatureControl();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Tăng độ ưu tiên cho Process giúp app mượt hơn
            using (Process p = Process.GetCurrentProcess())
            {
                p.PriorityClass = ProcessPriorityClass.AboveNormal;
            }

            bool isPrimaryInstance = false;
            try
            {
                // 2. Kiểm soát Single Instance
                try
                {
                    _appMutex = new Mutex(true, AppMutexName, out isPrimaryInstance);
                }
                catch (AbandonedMutexException)
                {
                    isPrimaryInstance = true;
                }

                if (!isPrimaryInstance)
                {
                    ShowSingleInstanceMessage();
                    return;
                }

                // 3. Khởi tạo cấu hình WinForms mặc định
                ApplicationConfiguration.Initialize();

                // 4. Thiết lập bẫy lỗi toàn cục
                ConfigureGlobalExceptionHandlers();

                // 5. Khởi tạo hệ thống lõi (Khởi tạo DB, giải mã đường dẫn)
                if (!KiemTraTrangThaiKhoiDong()) // Đã đổi tên từ NewMethod
                {
                    return; // Dừng khởi động nếu lõi có vấn đề
                }

                // 6. ĐỒNG BỘ DỮ LIỆU
                try
                {
                    Module_HuongDanSuDung.SyncMasterVersion();
                    Module_HuongDanSuDung.UpdateLocal();
                }
                catch (Exception ex)
                {
                    SafeLog("SYSTEM", "Sync Error", $"Lỗi đồng bộ Hướng dẫn sử dụng: {ex.Message}");
                }

                // 7. Gom cụm Tác vụ nền (Preload & Bảo vệ) vào chung 1 Task để tối ưu ThreadPool
                Task.Run(() =>
                {
                    // Chạy preload
                    PreloadBackgroundTasks();

                    // Chạy bảo vệ hệ thống
                    try
                    {
                        Module_KhoiTaoCSDL.ChinhSachBaoVeHeThongEXE(AppContext.BaseDirectory);
                        Module_KhoiTaoCSDL.ChinhSachLamSach();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Lỗi tự vệ hệ thống ngầm: " + ex.Message);
                    }
                });

                // 8. Khởi chạy giao diện chính
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                HandleFatalError(ex);
            }
            finally
            {
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
        private static bool KhoiTaoHeThong()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                Module_DanduongGPS.XinTraLaiThoiGianNapKeyBase64();
                Module_DanduongGPS.LoiChaoTuSiberia();

                // Đã sửa: Dùng GetAwaiter().GetResult() để bắt lỗi nguyên thủy, không bị bọc trong AggregateException
                Module_DanduongGPS.HanhTrinhToiColombiaAsync().GetAwaiter().GetResult();

                // Đã sửa: Bắt buộc chờ DB khởi tạo xong bằng GetAwaiter().GetResult()
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

        private static void PreloadBackgroundTasks()
        {
            try
            {
                // THÀNH CÔNG: KHÔNG làm gì cả
            }
            catch (Exception ex)
            {
                SafeLog("SYSTEM", "Lỗi Preload", $"Thất bại khi nạp bộ nhớ đệm ngầm: {ex.Message}");
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

        private static void SetBrowserFeatureControl()
        {
            try
            {
                string appName = Path.GetFileName(Application.ExecutablePath);
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
                {
                    key.SetValue(appName, 11001, RegistryValueKind.DWord);
                }
            }
            catch { }
        }
    }
}
