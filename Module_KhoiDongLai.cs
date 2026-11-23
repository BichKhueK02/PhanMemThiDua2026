using System.Diagnostics;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Đóng hẳn phần mềm rồi TỰ MỞ LẠI (vd. sau khi đổi kiểu giao diện ở Form12).
    /// Cách làm an toàn:
    ///  1) Tiến trình hiện tại chạy 1 bản mới của chính nó với tham số --mo-lai-sau-pid=&lt;pid hiện tại&gt;;
    ///  2) rồi thoát dứt khoát (Module_ThoatAnToan.ThoatDutKhoat: không bị FormClosing của Form2 chặn, ghi nốt nhật ký);
    ///  3) bản mới (Program.Main gọi ChoTienTrinhCuThoat) CHỜ tiến trình cũ kết thúc hẳn — nhả khóa "1 phiên bản" và tệp CSDL —
    ///     rồi mới khởi động bình thường (màn hình đăng nhập).
    /// </summary>
    internal static class Module_KhoiDongLai
    {
        private const string TienToThamSo = "--mo-lai-sau-pid=";
        private const int ChoToiDaMs = 20000;
        /// <summary>Mở bản mới rồi thoát bản hiện tại. Không mở được bản mới => báo người dùng tự mở lại, vẫn thoát.</summary>
        public static void KhoiDongLaiPhanMem()
        {
            bool daMo = false;
            try
            {
                int pid = Environment.ProcessId;
                string? exe = Environment.ProcessPath;
                if (!string.IsNullOrWhiteSpace(exe) && File.Exists(exe))
                {
                    var psi = new ProcessStartInfo
                    {
                        UseShellExecute = false,
                        WorkingDirectory = AppContext.BaseDirectory
                    };
                    // Chạy bằng "dotnet PhanMemThiDua2026.dll" (hiếm) => phải truyền lại tệp dll
                    if (string.Equals(Path.GetFileNameWithoutExtension(exe), "dotnet", StringComparison.OrdinalIgnoreCase))
                    {
                        psi.FileName = exe;
                        psi.ArgumentList.Add(typeof(Module_KhoiDongLai).Assembly.Location);
                    }
                    else psi.FileName = exe;
                    psi.ArgumentList.Add(TienToThamSo + pid);
                    using var p = Process.Start(psi);
                    daMo = p != null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[KhoiDongLai] Mở bản mới: " + ex.Message);
            }
            if (!daMo)
            {
                try
                {
                    MessageBox.Show("Không tự mở lại được phần mềm.\nPhần mềm sẽ đóng, vui lòng mở lại thủ công để dùng giao diện mới.",
                        "Khởi động lại", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch { }
            }
            Module_ThoatAnToan.ThoatDutKhoat();
        }
        /// <summary>
        /// Gọi ĐẦU Program.Main (trước khi giữ khóa "1 phiên bản"): nếu được mở lại bằng KhoiDongLaiPhanMem thì chờ tiến trình cũ
        /// thoát hẳn (tối đa 20 giây). Không có tham số / lỗi bất kỳ => bỏ qua, khởi động như thường.
        /// </summary>
        public static void ChoTienTrinhCuThoat()
        {
            try
            {
                foreach (string ts in Environment.GetCommandLineArgs())
                {
                    if (!ts.StartsWith(TienToThamSo, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!int.TryParse(ts.AsSpan(TienToThamSo.Length), out int pid) || pid == Environment.ProcessId) return;
                    try
                    {
                        using var cu = Process.GetProcessById(pid);   // đã thoát => ArgumentException
                        cu.WaitForExit(ChoToiDaMs);
                    }
                    catch (ArgumentException) { }                     // tiến trình cũ đã kết thúc
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[KhoiDongLai] Chờ tiến trình cũ: " + ex.Message);
            }
        }
    }
}
