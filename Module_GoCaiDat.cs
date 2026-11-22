using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Phía phần mềm mẹ của quy trình gỡ cài đặt (thay cho Module_BaoMatAES.DuongVaoTraiTimEm + tệp ".uninstall.key").
    ///
    /// Cách làm: chạy Uninstall_PhanMemThiDua2026.exe và ghi 1 DÒNG yêu cầu vào stdin của nó rồi đóng. Không ghi tệp ra đĩa,
    /// không dùng khóa/IV nhúng cứng, không đặt thuộc tính Hidden|System.
    ///
    /// Lưu ý trung thực: dòng yêu cầu chỉ chống gỡ NHẦM (bấm đúp vào exe gỡ cài đặt sẽ bị từ chối). Nó KHÔNG phải ranh giới bảo mật:
    /// điều kiện thật sự là mật khẩu Admin ở Form9_GoCaiDat. Vì vậy cố ý không dùng bí mật dùng chung trong exe (loại bí mật đó
    /// ai có exe cũng lấy được, và chuỗi khóa nhúng cứng chính là thứ phần mềm diệt virus hay nghi ngờ).
    ///
    /// Định dạng (khớp Program.DocGoi của Uninstall_PhanMemThiDua2026 2.x):
    ///   PMTDUN3|1|pid|unixGiayUTC|nonceHex|KEEP/ALL|base64(UTF-8 thư mục cài đặt)
    /// </summary>
    internal static class Module_GoCaiDat
    {
        private const string TIEN_TO_GOI = "PMTDUN3";
        private const string TEN_EXE_GO = "Uninstall_PhanMemThiDua2026.exe";
        private const int PHIEN_BAN_TOI_THIEU = 2;   // trình gỡ cài đặt 1.x dùng tệp khóa cũ, không hiểu giao thức này
        /// <summary>
        /// Trình gỡ cài đặt (giai đoạn 1 trong thư mục cài đặt, hoặc giai đoạn 2 chạy từ %TEMP%) đang chạy.
        /// Phần mềm mẹ KHÔNG được khởi động lúc này: nó sẽ mở CSDL trong thư mục đang bị xóa
        /// (vd ServiceBackup/ServiceRestore bị trình gỡ yêu cầu đóng và tự mở lại mẹ, hoặc người dùng bấm shortcut).
        /// </summary>
        public static bool DangGoCaiDat()
        {
            try
            {
                Process[] ds = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(TEN_EXE_GO));
                bool co = ds.Length > 0;
                foreach (Process p in ds) p.Dispose();
                return co;
            }
            catch { return false; }
        }
        /// <summary>Dòng yêu cầu giai đoạn 1 (internal để kiểm thử).</summary>
        internal static string TaoGoi(bool giuDuLieu, string thuMucCaiDat)
        {
            string nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(8));
            string thuMucB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(thuMucCaiDat));
            return string.Join('|', TIEN_TO_GOI, 1, Environment.ProcessId,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds(), nonce, giuDuLieu ? "KEEP" : "ALL", thuMucB64);
        }
        /// <summary>
        /// Khởi chạy trình gỡ cài đặt. Trả true khi đã bàn giao thành công: nơi gọi PHẢI thoát phần mềm ngay sau đó
        /// (trình gỡ cài đặt sẽ chờ phần mềm đóng, không ép tắt).
        /// </summary>
        public static bool KhoiChay(bool giuDuLieu, out string loi)
        {
            loi = string.Empty;
            try
            {
                string thuMuc = Path.GetFullPath(AppContext.BaseDirectory)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string exe = Path.Combine(thuMuc, TEN_EXE_GO);
                if (!File.Exists(exe))
                {
                    loi = "Không tìm thấy tệp gỡ cài đặt (" + TEN_EXE_GO + ") trong thư mục phần mềm.";
                    return false;
                }
                FileVersionInfo vi = FileVersionInfo.GetVersionInfo(exe);
                if (vi.FileMajorPart < PHIEN_BAN_TOI_THIEU)
                {
                    loi = "Trình gỡ cài đặt (" + (vi.FileVersion ?? "?") + ") quá cũ, cần phiên bản 2.0 trở lên.\n" +
                          "Hãy cập nhật " + TEN_EXE_GO + " cùng lúc với phần mềm.";
                    return false;
                }
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    WorkingDirectory = Path.GetTempPath(),   // không để tiến trình gỡ giữ thư mục cài đặt làm thư mục làm việc
                    UseShellExecute = false,                 // cần để chuyển hướng stdin
                    RedirectStandardInput = true,
                    CreateNoWindow = false                   // cửa sổ gỡ cài đặt hiện bình thường (không chạy ẩn)
                };
                using Process? p = Process.Start(psi);
                if (p == null)
                {
                    loi = "Không khởi chạy được trình gỡ cài đặt.";
                    return false;
                }
                p.StandardInput.WriteLine(TaoGoi(giuDuLieu, thuMuc));
                p.StandardInput.Flush();
                p.StandardInput.Close();
                return true;
            }
            catch (Exception ex)
            {
                loi = ex.Message;
                return false;
            }
        }
    }
}
