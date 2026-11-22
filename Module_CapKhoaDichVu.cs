#nullable enable
using System.Buffers.Text;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
namespace PhanMemThiDua2026   // <-- đổi theo namespace thật của phần mềm mẹ (nếu Form12 nằm trong namespace khác)
{
    /// <summary>
    /// PHẦN MỀM MẸ: cấp khóa cho ServiceBackup / ServiceRestore ngay khi bấm nút ở Form12.
    /// - Khóa suy ra tất định từ khóa hệ thống (Module_BaoMatAES.LayKhoaHeThongDungChung) => bản sao lưu ở máy A giải mã được ở máy B.
    /// - Chỉ trao cho ĐÚNG tệp ServiceBackup.exe / ServiceRestore.exe nằm cạnh phần mềm mẹ (không trao cho tệp lạ).
    /// - Đẩy qua stdin của tiến trình con; gói khóa dựng bằng byte (không tạo string chứa khóa) và bị ghi đè 0 ngay sau đó.
    /// </summary>
    public static class Module_CapKhoaDichVu
    {
        // Khóa của định dạng v1 cũ. Chỉ gửi cho ServiceRestore để còn đọc được bản sao lưu đời cũ (*.pmtd).
        private const string MAT_KHAU_V1 = "PhanMemThiDua2026@MeoCam2026";
        internal static byte[] DanXuatKhoaSaoLuu(byte[] khoaHeThong)
            => HKDF.DeriveKey(HashAlgorithmName.SHA256, khoaHeThong, 32,
                Encoding.ASCII.GetBytes("PhanMemThiDua2026/PMTD"), Encoding.ASCII.GetBytes("backup-root/v2"));
        /// <summary>Dựng gói "PMTDKEY2|b64(khóa)|b64(khóa cũ)|hex16\n" hoàn toàn bằng byte. Bên gọi phải ZeroMemory kết quả.</summary>
        internal static byte[] TaoGoiKhoa(byte[] khoaHeThong, bool kemKhoaCu)
        {
            byte[] khoa = DanXuatKhoaSaoLuu(khoaHeThong);
            byte[]? cu = kemKhoaCu ? SHA256.HashData(Encoding.UTF8.GetBytes(MAT_KHAU_V1)) : null;
            byte[] goi = GC.AllocateArray<byte>(9 + 44 + 1 + (cu != null ? 44 : 0) + 1 + 16 + 1, pinned: true);
            try
            {
                int n = 0;
                Encoding.ASCII.GetBytes("PMTDKEY2|", 0, 9, goi, 0); n = 9;
                Base64.EncodeToUtf8(khoa, goi.AsSpan(n), out _, out int w1); n += w1;
                goi[n++] = (byte)'|';
                if (cu != null) { Base64.EncodeToUtf8(cu, goi.AsSpan(n), out _, out int w2); n += w2; }
                Span<byte> h = stackalloc byte[32];
                SHA256.HashData(goi.AsSpan(0, n), h);
                goi[n++] = (byte)'|';
                const string HEX = "0123456789abcdef";
                for (int i = 0; i < 8; i++) { goi[n++] = (byte)HEX[h[i] >> 4]; goi[n++] = (byte)HEX[h[i] & 15]; }
                goi[n++] = (byte)'\n';
                h.Clear();
                return goi;   // n == goi.Length (độ dài đã tính chính xác, không phải sao chép sang mảng khác)
            }
            catch { CryptographicOperations.ZeroMemory(goi); throw; }
            finally
            {
                CryptographicOperations.ZeroMemory(khoa);
                if (cu != null) CryptographicOperations.ZeroMemory(cu);
            }
        }
        // Cờ do ServiceBackup / ServiceRestore đặt trong lúc đọc/ghi CSDL (đối tượng hệ thống có tên, không ghi đĩa).
        // PHẢI trùng Module_KennelServiceCrypto.TEN_CO_DANG_XU_LY của 2 dịch vụ. Dịch vụ bị tắt ngang thì Windows tự gỡ cờ.
        private const string TEN_CO_DICH_VU_DANG_XU_LY = @"Local\PhanMemThiDua2026_DichVuDangXuLyCSDL";
        private static bool DichVuDangXuLy()
        {
            try
            {
                if (!EventWaitHandle.TryOpenExisting(TEN_CO_DICH_VU_DANG_XU_LY, out EventWaitHandle? h)) return false;
                h.Dispose();
                return true;
            }
            catch (UnauthorizedAccessException) { return true; }   // có cờ nhưng không mở được => coi như đang bận
            catch { return false; }
        }
        /// <summary>
        /// Gọi lúc phần mềm mẹ khởi động, TRƯỚC khi đụng tới CSDL: nếu dịch vụ sao lưu/khôi phục đang đọc/ghi CSDL thì chờ
        /// (mở mẹ giữa lúc đang khôi phục => đọc phải CSDL đang bị thay => hỏng/mất dữ liệu). false = người dùng chọn thoát.
        /// </summary>
        public static bool ChoDichVuSaoLuuKhoiPhucXong()
        {
            while (true)
            {
                var sw = Stopwatch.StartNew();
                while (DichVuDangXuLy() && sw.ElapsedMilliseconds < 5000) Thread.Sleep(200);
                if (!DichVuDangXuLy()) return true;
                var kq = MessageBox.Show(
                    "Chương trình sao lưu / khôi phục dữ liệu đang xử lý cơ sở dữ liệu.\n\n" +
                    "Vui lòng chờ chương trình đó hoàn tất rồi bấm \"Thử lại\" (hoặc \"Hủy\" để thoát).",
                    "PhanMemThiDua2026", MessageBoxButtons.RetryCancel, MessageBoxIcon.Information);
                if (kq != DialogResult.Retry) return false;
            }
        }
        /// <summary>Khởi chạy service và trao khóa. Thành công => true (mẹ có thể thoát ngay sau đó).</summary>
        public static bool KhoiChay(string duongDanExe, bool laKhoiPhuc, out string loi)
        {
            loi = "";
            byte[]? khoaHT = null, goi = null;
            try
            {
                // Chỉ trao khóa cho đúng tệp dịch vụ nằm cạnh phần mềm mẹ
                string tenHopLe = laKhoiPhuc ? "ServiceRestore.exe" : "ServiceBackup.exe";
                string full = Path.GetFullPath(duongDanExe);
                string thuMucMe = Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar);
                string thuMucExe = (Path.GetDirectoryName(full) ?? "").TrimEnd(Path.DirectorySeparatorChar);
                if (!string.Equals(Path.GetFileName(full), tenHopLe, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(thuMucExe, thuMucMe, StringComparison.OrdinalIgnoreCase))
                { loi = "Chỉ được mở " + tenHopLe + " nằm cùng thư mục phần mềm chính."; return false; }
                if (!File.Exists(full)) { loi = "Không tìm thấy tệp: " + tenHopLe; return false; }
                khoaHT = Module_BaoMatAES.LayKhoaHeThongDungChung();
                goi = TaoGoiKhoa(khoaHT, kemKhoaCu: laKhoiPhuc);
                var psi = new ProcessStartInfo
                {
                    FileName = full,
                    WorkingDirectory = thuMucExe,
                    UseShellExecute = false,          // bắt buộc để chuyển hướng stdin
                    RedirectStandardInput = true
                };
                using var p = Process.Start(psi);
                if (p == null) { loi = "Không khởi chạy được tiến trình."; return false; }
                var ghi = p.StandardInput.BaseStream;
                ghi.Write(goi, 0, goi.Length);
                ghi.Flush();
                p.StandardInput.Close();              // dữ liệu đã nằm trong ống; con đọc được dù mẹ thoát ngay
                return true;
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 740)
            {
                loi = "Dịch vụ yêu cầu quyền quản trị (UAC) nên không thể trao khóa an toàn. Hãy bỏ yêu cầu quyền admin trong manifest của dịch vụ.";
                return false;
            }
            catch (Exception ex) { loi = ex.Message; return false; }
            finally
            {
                if (khoaHT != null) CryptographicOperations.ZeroMemory(khoaHT);
                if (goi != null) CryptographicOperations.ZeroMemory(goi);
            }
        }
    }
}
