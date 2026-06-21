using System.Security.Cryptography;
using System.Text;

namespace PhanMemThiDua2026
{
    internal static class Module_CapQuyenService
    {
        //// AES-256 KEY
        //// SHA256 => luôn đúng 32 bytes trên mọi máy
        //private static readonly byte[] SHARED_KEY = SHA256.HashData(Encoding.UTF8.GetBytes("PMTD2026_AUTHORIZATION_KEY_9999!"));
        //// HMAC KEY
        //// Dùng để chống sửa đổi file license
        //private static readonly byte[] HMAC_KEY = SHA256.HashData(Encoding.UTF8.GetBytes("PMTD2026_HMAC_SIGNATURE_KEY_2026!"));
        // AES-256 KEY (BINARY FRAGMENT)
        private static readonly byte[] Cache_RAM_SYS_CFG_MOD_A = { 0x21, 0x39, 0x39, 0x39, 0x39, 0x5F };
        private static readonly byte[] Cache_RAM_SYS_CFG_MOD_B = { 0x59, 0x45, 0x4B, 0x5F, 0x4E, 0x4F, 0x49, 0x54, 0x41, 0x5A, 0x49, 0x52 };
        private static readonly byte[] Cache_RAM_SYS_CFG_MOD_C = { 0x4F, 0x48, 0x54, 0x55, 0x41, 0x5F, 0x36, 0x32, 0x30, 0x32, 0x44, 0x54, 0x4D, 0x50 };
        // =====================================================
        // HMAC KEY (BINARY FRAGMENT)
        // =====================================================
        private static readonly byte[] Cache_GPU_SEC_KEY_BLOCK_X = { 0x21, 0x36, 0x32, 0x30, 0x32, 0x5F, 0x59, 0x45, 0x4B, 0x5F };
        private static readonly byte[] Cache_GPU_SEC_KEY_BLOCK_Y = { 0x45, 0x52, 0x55, 0x54, 0x41, 0x4E, 0x47, 0x49, 0x53, 0x5F, 0x43, 0x41 };
        private static readonly byte[] Cache_GPU_SEC_KEY_BLOCK_Z = { 0x4D, 0x48, 0x5F, 0x36, 0x32, 0x30, 0x32, 0x44, 0x54, 0x4D, 0x50 };
        // =====================================================
        // AES + HMAC KEY
        // =====================================================
        private static readonly byte[] SHARED_KEY;
        private static readonly byte[] HMAC_KEY;
        // =====================================================
        // STATIC CONSTRUCTOR
        // =====================================================
        static Module_CapQuyenService()
        {
            try
            {
                string aesReversed = BaoMatAES.XuyenKhongVeThoiMinh(Cache_RAM_SYS_CFG_MOD_A) + BaoMatAES.XuyenKhongVeThoiMinh(Cache_RAM_SYS_CFG_MOD_B) + BaoMatAES.XuyenKhongVeThoiMinh(Cache_RAM_SYS_CFG_MOD_C);
                string hmacReversed = BaoMatAES.XuyenKhongVeThoiMinh(Cache_GPU_SEC_KEY_BLOCK_X) + BaoMatAES.XuyenKhongVeThoiMinh(Cache_GPU_SEC_KEY_BLOCK_Y) + BaoMatAES.XuyenKhongVeThoiMinh(Cache_GPU_SEC_KEY_BLOCK_Z);
                string aesKeyOriginal = BaoMatAES.XinTraLaiThoiGian(aesReversed);
                string hmacKeyOriginal = BaoMatAES.XinTraLaiThoiGian(hmacReversed);
                SHARED_KEY = SHA256.HashData(Encoding.UTF8.GetBytes(aesKeyOriginal));
                HMAC_KEY = SHA256.HashData(Encoding.UTF8.GetBytes(hmacKeyOriginal));
            }
            catch
            {
                SHARED_KEY = new byte[32];
                HMAC_KEY = new byte[32];
            }
        }
        // PHỤC HỒI BYTE[] -> STRING 
        public static string GetLicensePath(string tenPhanMem)
        {
            string folderPath = Path.Combine(AppContext.BaseDirectory, "Database", "Bansaoluu");
            return Path.Combine(folderPath, $"GiayPhepCapQuyen_{tenPhanMem}.dat");
        }
        // TẠO GIẤY PHÉP
        public static void TaoGiayPhep(string tenPhanMem, string fileName)
        {
            try
            {
                // ---------------------------------------------
                // Tạo nội dung giấy phép
                // ---------------------------------------------
                string noiDung =
          TaoNoiDungGiayPhep(tenPhanMem);

                byte[] plainBytes =
                  Encoding.UTF8.GetBytes(noiDung);

                // ---------------------------------------------
                // AES
                // ---------------------------------------------
                using Aes aes = Aes.Create();

                aes.Key = SHARED_KEY;

                // Explicit để tránh khác runtime
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // IV random mỗi lần tạo
                aes.GenerateIV();

                // ---------------------------------------------
                // Encrypt
                // Format:
                // [IV][CIPHER]
                // ---------------------------------------------
                byte[] cipherBytes;

                using (MemoryStream ms = new MemoryStream())
                {
                    // Ghi IV đầu file
                    ms.Write(
            aes.IV,
            0,
            aes.IV.Length
          );

                    using (CryptoStream cs =
                      new CryptoStream(
                        ms,
                        aes.CreateEncryptor(),
                        CryptoStreamMode.Write
                      ))
                    {
                        cs.Write(
                          plainBytes,
                          0,
                          plainBytes.Length
                        );

                        cs.FlushFinalBlock();
                    }

                    cipherBytes = ms.ToArray();
                }

                // ---------------------------------------------
                // HMACSHA256
                // Chống sửa đổi file
                // ---------------------------------------------
                byte[] hmacBytes;

                using (HMACSHA256 hmac =
                  new HMACSHA256(HMAC_KEY))
                {
                    hmacBytes =
                      hmac.ComputeHash(cipherBytes);
                }

                // ---------------------------------------------
                // Final file
                // Format:
                // [IV][CIPHER][HMAC]
                // ---------------------------------------------
                byte[] finalBytes =
          new byte[
            cipherBytes.Length +
            hmacBytes.Length
          ];

                Buffer.BlockCopy(
                  cipherBytes,
                  0,
                  finalBytes,
                  0,
                  cipherBytes.Length
                );

                Buffer.BlockCopy(
                  hmacBytes,
                  0,
                  finalBytes,
                  cipherBytes.Length,
                  hmacBytes.Length
                );

                // ---------------------------------------------
                // Ghi file
                // ---------------------------------------------
                File.WriteAllBytes(
          fileName,
          finalBytes
        );
            }
            catch
            {
                throw;
            }
        }
        // =========================================================
        // KIỂM TRA GIẤY PHÉP
        // =========================================================
        public static bool KiemTraGiayPhep(
      string fileName,
      string tenPhanMemCuaToi
    )
        {
            try
            {
                // ---------------------------------------------
                // File tồn tại ?
                // ---------------------------------------------
                if (!File.Exists(fileName))
                {
                    return false;
                }

                byte[] fullBytes =
                  File.ReadAllBytes(fileName);

                // ---------------------------------------------
                // Kiểm tra độ dài tối thiểu
                // AES IV = 16 bytes
                // HMACSHA256 = 32 bytes
                // ---------------------------------------------
                if (fullBytes.Length < 48)
                {
                    return false;
                }

                // ---------------------------------------------
                // Tách:
                // [IV][CIPHER][HMAC]
                // ---------------------------------------------
                const int HMAC_SIZE = 32;

                int cipherLength =
                  fullBytes.Length - HMAC_SIZE;

                byte[] cipherBytes =
                  new byte[cipherLength];

                byte[] storedHmac =
                  new byte[HMAC_SIZE];

                Buffer.BlockCopy(
                  fullBytes,
                  0,
                  cipherBytes,
                  0,
                  cipherLength
                );

                Buffer.BlockCopy(
                  fullBytes,
                  cipherLength,
                  storedHmac,
                  0,
                  HMAC_SIZE
                );

                // ---------------------------------------------
                // Verify HMAC trước
                // ---------------------------------------------
                byte[] computedHmac;

                using (HMACSHA256 hmac =
                  new HMACSHA256(HMAC_KEY))
                {
                    computedHmac =
                      hmac.ComputeHash(cipherBytes);
                }

                bool hmacHopLe =
                  CryptographicOperations
                  .FixedTimeEquals(
                    storedHmac,
                    computedHmac
                  );

                if (!hmacHopLe)
                {
                    return false;
                }

                // ---------------------------------------------
                // AES
                // ---------------------------------------------
                using Aes aes = Aes.Create();

                aes.Key = SHARED_KEY;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // ---------------------------------------------
                // Đọc IV
                // ---------------------------------------------
                byte[] iv =
          new byte[aes.BlockSize / 8];

                Buffer.BlockCopy(
                  cipherBytes,
                  0,
                  iv,
                  0,
                  iv.Length
                );

                aes.IV = iv;

                // ---------------------------------------------
                // Giải mã
                // ---------------------------------------------
                string noiDungGoc;

                using (
                  MemoryStream ms =
                    new MemoryStream(
                      cipherBytes,
                      iv.Length,
                      cipherBytes.Length - iv.Length
                    )
                )
                {
                    using (
                      CryptoStream cs =
                        new CryptoStream(
                          ms,
                          aes.CreateDecryptor(),
                          CryptoStreamMode.Read
                        )
                    )
                    {
                        using (
                          StreamReader sr =
                            new StreamReader(
                              cs,
                              Encoding.UTF8
                            )
                        )
                        {
                            noiDungGoc =
                              sr.ReadToEnd();
                        }
                    }
                }

                // ---------------------------------------------
                // Parse & Validate
                // ---------------------------------------------
                return ParseAndValidate(
          noiDungGoc,
          tenPhanMemCuaToi
        );
            }
            catch
            {
                return false;
            }
        }
        // =========================================================
        // PARSE & VALIDATE
        // Không dùng Contains()
        // =========================================================
        private static bool ParseAndValidate(
      string content,
      string tenPhanMem
    )
        {
            try
            {
                Dictionary<string, string> data =
                  new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase
                  );

                string[] lines =
                  content.Split(
                    new[]
                    {
       "\r\n",
       "\r",
       "\n"
                    },
                    StringSplitOptions.RemoveEmptyEntries
                  );

                foreach (string line in lines)
                {
                    int index = line.IndexOf('=');

                    if (index <= 0)
                    {
                        continue;
                    }

                    string key =
                      line.Substring(0, index).Trim();

                    string value =
                      line.Substring(index + 1).Trim();

                    data[key] = value;
                }

                // ---------------------------------------------
                // Validate APP
                // ---------------------------------------------
                if (!data.TryGetValue(
          "APP",
          out string appName))
                {
                    return false;
                }

                if (!string.Equals(
                  appName,
                  tenPhanMem,
                  StringComparison.Ordinal
                ))
                {
                    return false;
                }

                // ---------------------------------------------
                // Validate MACHINE
                // ---------------------------------------------
                if (!data.TryGetValue(
          "MACHINE",
          out string machine))
                {
                    return false;
                }

                string currentMachine =
                  $"{Environment.MachineName}_{Environment.UserName}";

                if (!string.Equals(
                  machine,
                  currentMachine,
                  StringComparison.OrdinalIgnoreCase
                ))
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
        // =========================================================
        // TẠO NỘI DUNG GIẤY PHÉP
        // Format rõ ràng để parse ổn định
        // =========================================================
        private static string TaoNoiDungGiayPhep(
      string tenPhanMem
    )
        {
            string quyenHan =
              tenPhanMem.Contains(
                "Restore",
                StringComparison.OrdinalIgnoreCase
              )
              ?
              "Mo_thuc_hien_khoi_phuc_CSDL"
              :
              "Mo_thuc_hien_sao_luu_du_lieu";

            StringBuilder sb =
              new StringBuilder();

            sb.AppendLine("LICENSE_PMTHIDUA2026");

            sb.AppendLine(
              $"APP={tenPhanMem}"
            );

            sb.AppendLine(
              $"ISSUER=PhanMemThiDua2026"
            );

            sb.AppendLine(
              $"PERMISSION={quyenHan}"
            );

            sb.AppendLine(
              $"MACHINE={Environment.MachineName}_{Environment.UserName}"
            );

            sb.AppendLine(
              $"TIME={DateTime.Now:yyyy-MM-dd HH:mm:ss}"
            );

            sb.AppendLine(
              $"VERSION=2026"
            );

            return sb.ToString();
        }
        public static void DonRacGiayPhep(string tenPhanMem)
        {
            try
            {
                string filePath = GetLicensePath(tenPhanMem);

                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                    return;

                // Lớp an toàn 1: Tháo thuộc tính ẩn/chỉ đọc bảo vệ file, bọc chặt chống Crash
                try
                {
                    File.SetAttributes(filePath, FileAttributes.Normal);
                }
                catch { /* Bỏ qua nếu không có quyền hoặc file biến mất giữa chừng */ }

                // Lớp an toàn 2: Vòng lặp thử lại (Retry) kết hợp đổi tên để đối phó với Antivirus/File Lock
                // Nếu file bị Antivirus khóa ngầm, việc đổi tên sẽ "phá xích" Handle cũ, giúp xóa file rác dễ dàng.
                int retryCount = 3;
                int delayMs = 100;

                for (int i = 0; i < retryCount; i++)
                {
                    try
                    {
                        if (!File.Exists(filePath)) return;

                        // Kỹ thuật cao cấp: Đổi tên thành file tạm rồi xóa file tạm, né hoàn toàn Race Condition
                        string tempPath = filePath + "." + Guid.NewGuid().ToString("N") + ".bak";
                        File.Move(filePath, tempPath);

                        // Xóa file tạm
                        File.Delete(tempPath);
                        return; // Xóa thành công thì thoát ngay
                    }
                    catch (IOException)
                    {
                        // Nếu dính File Lock (lỗi IO), đợi một chút rồi thử lại vòng tiếp theo
                        if (i < retryCount - 1)
                        {
                            System.Threading.Thread.Sleep(delayMs);
                            delayMs *= 2; // Tăng thời gian chờ lên (Cấp số nhân)
                        }
                    }
                    catch
                    {
                        // Các lỗi ngẫu nhiên khác (AccessDenied, Unauthorized...) -> cố gắng xóa trực tiếp luôn
                        try { File.Delete(filePath); return; } catch { break; }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CRITICAL_CLEANUP_LICENSE_ERROR] {ex}");
            }
        }
    }
}