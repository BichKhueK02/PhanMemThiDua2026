using Microsoft.Data.Sqlite;
using PhanMemThiDua2026;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

//Gọi tắt là Module V2
public static class BaoMatAES
{
    private const int MAX_STRING_LENGTH = 256;
    // Tối ưu RAM: Giảm cache size xuống 10.000 là quá đủ cho grid 5000 dòng
    private const int MAX_CACHE_SIZE = 10000;
    private const string PREFIX_V2 = "v2|"; //Phiên bản mã hóa dành cho hệ thống
    public static string yeuMeoCam_v1 = string.Empty;
    public static string yeuChuMeoCam_v1 = string.Empty;
    // CHỈ DÙNG CACHE GIẢI MÃ: Tiết kiệm RAM tối đa.
    private static readonly ConcurrentDictionary<string, string> _giaiMaCache = new(StringComparer.Ordinal);
    // ĐÃ XÓA _maHoaCache: Để ép hàm MaHoa luôn chạy logic tạo chuỗi Random mới và giải phóng RAM.

    private static readonly ThreadLocal<Aes> _aesLocal = new(() =>
    {
        try
        {
            // 1. Chống Debug (Chỉ chạy khi thực sự cần thiết)
            if (Debugger.IsAttached)
            {
                // Thay vì FailFast gây crash, ta có thể throw nhẹ nhàng hơn hoặc xử lý khác
                // Debugger.Break(); 
            }
            // 2. Fallback Key (Dùng Base64 để ẩn mắt thường, không gây lỗi memory)
            string mKpassKey = Encoding.UTF8.GetString(Convert.FromBase64String("eWVLZGV4aUZfNjIwMmF1RGlpaFRtZU1uYWhQ"));
            string mKpasssalt = Encoding.UTF8.GetString(Convert.FromBase64String("VklkZXhpRl82MjAyYXVEaWloVG1lTW5haFA="));
            Aes aes = Aes.Create();
            // 3. Lấy nguyên liệu từ GPS
            string k1 = Module_DanduongGPS.ToiYeuMeoCam1;
            string k2 = Module_DanduongGPS.ToiYeuMeoCam2;
            string i1 = Module_DanduongGPS.ToiCanChuIVMeoCam1;
            string i2 = Module_DanduongGPS.ToiCanChuIVMeoCam2;
            //Cách viết 1
            // 4. Kỹ thuật "Phòng thủ 1 lớp" đã được tối ưu từ 3 lớp (Kết quả đầu ra chính xác 100% như cũ)
            string rawKeyFromGPS = TraLaiTenChoMeoCam(k1 + k2);
            string rawIVFromGPS = TraLaiTenChoMeoCam(i1 + i2);
            string keyFinal = !string.IsNullOrEmpty(rawKeyFromGPS) ? rawKeyFromGPS : TraLaiTenChoMeoCam(mKpassKey);
            string ivFinal = !string.IsNullOrEmpty(rawIVFromGPS) ? rawIVFromGPS : TraLaiTenChoMeoCam(mKpasssalt);
            // 5. Chuyển thành Byte array - ĐÂY LÀ NƠI CẦN BẢO VỆ
            byte[] keyBytes = VanLyDocHanhTaoKey256(keyFinal);
            byte[] ivBytes = BaoCatSaharaTaoIV(ivFinal);

            aes.Key = keyBytes;
            aes.IV = (byte[])ivBytes.Clone();

            // 6. XÓA SẠCH DỮ LIỆU NHẠY CẢM TRÊN MẢNG BYTE (Cách chuẩn, không gây crash)
            // CryptographicOperations.ZeroMemory là hàm an toàn nhất của .NET
            CryptographicOperations.ZeroMemory(keyBytes);
            CryptographicOperations.ZeroMemory(ivBytes);

            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Reset các biến tạm về rỗng (An toàn hơn gán null)
            k1 = k2 = i1 = i2 = rawKeyFromGPS = rawIVFromGPS = keyFinal = ivFinal = string.Empty;

            return aes;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Lỗi khởi tạo AES: " + ex.Message);
            return Aes.Create(); // Trả về đối tượng mặc định để tránh crash luồng
        }
    }, trackAllValues: true);
    public static string MaHoa(string chuoiGoc)
    {
        if (string.IsNullOrEmpty(chuoiGoc)) return string.Empty;

        try
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(chuoiGoc);
            Aes aes = _aesLocal.Value!;
            // Tạo IV ngẫu nhiên mới hoàn toàn
            byte[] randomIv = new byte[16];
            RandomNumberGenerator.Fill(randomIv);
            byte[] cipherBytes = aes.EncryptCbc(inputBytes, randomIv, PaddingMode.PKCS7);
            byte[] payload = new byte[16 + cipherBytes.Length];
            Buffer.BlockCopy(randomIv, 0, payload, 0, 16);
            Buffer.BlockCopy(cipherBytes, 0, payload, 16, cipherBytes.Length);
            return PREFIX_V2 + Convert.ToBase64String(payload);
        }
        catch { return string.Empty; }
    }
    /// GIẢI MÃ: Load từ Cache cho 10000 dòng dữ liệu cực nhanh, tương thích ngược v1 và v2
    public static string GiaiMa(string chuoiMaHoa)
    {
        if (string.IsNullOrEmpty(chuoiMaHoa)) return string.Empty;

        // Đọc từ Cache giúp load dữ liệu CSDL nhanh chóng
        if (_giaiMaCache.TryGetValue(chuoiMaHoa, out string cached)) return cached;

        try
        {
            Aes aes = _aesLocal.Value!;
            string finalResult;

            if (chuoiMaHoa.StartsWith(PREFIX_V2, StringComparison.Ordinal))
            {
                string b64 = chuoiMaHoa.Substring(PREFIX_V2.Length);
                byte[] payload = Convert.FromBase64String(b64);
                byte[] iv = new byte[16];
                Buffer.BlockCopy(payload, 0, iv, 0, 16);
                byte[] cipher = new byte[payload.Length - 16];
                Buffer.BlockCopy(payload, 16, cipher, 0, cipher.Length);

                byte[] decrypted = aes.DecryptCbc(cipher, iv, PaddingMode.PKCS7);
                // 🔥 Gọt sạch ký tự Null
                finalResult = Encoding.UTF8.GetString(decrypted).Trim('\0').Trim();
            }
            else
            {
                // TƯƠNG THÍCH CŨ: Dùng IV mặc định
                byte[] cipher = Convert.FromBase64String(chuoiMaHoa);
                byte[] decrypted = aes.DecryptCbc(cipher, aes.IV, PaddingMode.PKCS7);
                finalResult = Encoding.UTF8.GetString(decrypted).Trim('\0').Trim();
            }

            // Lưu cache giải mã để các lần hiển thị sau trên Grid nhanh hơn
            if (chuoiMaHoa.Length <= MAX_STRING_LENGTH * 2)
            {
                if (_giaiMaCache.Count >= MAX_CACHE_SIZE) DonNheCache(_giaiMaCache, 1000);
                _giaiMaCache.TryAdd(chuoiMaHoa, finalResult);
            }
            return finalResult;
        }
        catch { return string.Empty; }
    }
    // --- CÁC HÀM TIỆN ÍCH HỖ TRỢ GIỮ NGUYÊN ---
    public static string KhoBaoTanVuongGiaiMaHex(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return string.Empty;
        try
        {
            byte[] b = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2) b[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return Encoding.UTF8.GetString(b).Trim();
        }
        catch { return string.Empty; }
    }
    public static string XuanVeOHokkaido(string b64)
    {
        if (string.IsNullOrEmpty(b64)) return string.Empty;
        try { return Encoding.UTF8.GetString(Convert.FromBase64String(b64)); } catch { return string.Empty; }
    }
    private static byte[] VanLyDocHanhTaoKey256(string key)
    {
        using SHA256 sha = SHA256.Create(); return sha.ComputeHash(Encoding.UTF8.GetBytes(key));
    }
    private static byte[] BaoCatSaharaTaoIV(string iv)
    {
        using MD5 md5 = MD5.Create(); return md5.ComputeHash(Encoding.UTF8.GetBytes(iv));
    }
    private static void DonNheCache(ConcurrentDictionary<string, string> cache, int soLuong)
    {
        int count = 0;
        foreach (var key in cache.Keys) { if (cache.TryRemove(key, out _)) count++; if (count >= soLuong) break; }
    }
    public static string TraLaiTenChoMeoCam(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return string.Create(s.Length, s, (span, str) =>
        {
            for (int i = 0, j = str.Length - 1; i < str.Length; i++, j--) span[i] = str[j];
        });
    }
    public static void DonDepTaiNguyen()
    {
        // Nhớ xóa _maHoaCache.Clear() ở đây vì mình đã bỏ nó ở trên
        _giaiMaCache.Clear();
        if (_aesLocal != null) { foreach (var a in _aesLocal.Values) a?.Dispose(); _aesLocal.Dispose(); }
    }
    //--- HÀM RIÊNG DÙNG CHO TẠO KHÓA GỠ CÀI ĐẶT (ĐẢM BẢO KHÔNG TRÙNG LẶP VỚI MODULE V1)
    public static byte[] HoaVanNoTrenDuongRaChienDich256v1(string key)
    {
        using (SHA256 sha = SHA256.Create())
        {
            return sha.ComputeHash(Encoding.UTF8.GetBytes(key));
        }
    }
    public static byte[] DatTruongSonViMienNamRuotThit(string iv)
    {
        using (MD5 md5 = MD5.Create())
        {
            return md5.ComputeHash(Encoding.UTF8.GetBytes(iv));
        }
    }
    public static string MotDoiNguoiMotRungCayv1(string chuoiGoc)
    {
        if (string.IsNullOrEmpty(chuoiGoc) || string.IsNullOrEmpty(yeuMeoCam_v1) || string.IsNullOrEmpty(yeuChuMeoCam_v1))
            return string.Empty;

        byte[] keyBytes = HoaVanNoTrenDuongRaChienDich256v1(yeuMeoCam_v1);
        byte[] ivBytes = DatTruongSonViMienNamRuotThit(yeuChuMeoCam_v1);

        using (Aes aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.IV = ivBytes;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (MemoryStream ms = new MemoryStream())
            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(chuoiGoc);
                cs.Write(inputBytes, 0, inputBytes.Length);
                cs.FlushFinalBlock();

                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }
    public static bool TruyenThuyetGhepManhVoKhoa()
    {
        try
        {
            if (!File.Exists(Module_DanduongGPS.DuongDanCSDL1)) return false;
            using var conn = new SqliteConnection($"Data Source={Module_DanduongGPS.DuongDanCSDL1}");
            conn.Open();
            // Đảo ngược chuỗi rác lại thành câu SQL chuẩn trước khi chạy
            //string sqlQuery = BaoMatAES.TraLaiTenChoMeoCam("1 TIMIL 1 = DI EREHW gnoDiohK_yeK MORF 4_yeK ,3_yeK ,2_yeK ,1_yeK TCELES"); using var cmd = new SqliteCommand(sqlQuery, conn);

            using var cmd = new SqliteCommand(
    "SELECT Key_1, Key_2, Key_3, Key_4 FROM Key_KhoiDong WHERE ID = 1 LIMIT 1",
    conn);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                // ĐỌC DỮ LIỆU TỪ CSDL BẰNG CÁCH LẬT NGƯỢC TÊN CỘT ĐỂ CHE GIẤU
                //string k1 = reader[BaoMatAES.TraLaiTenChoMeoCam("1_yeK")]?.ToString() ?? "";
                //string k2 = reader[BaoMatAES.TraLaiTenChoMeoCam("2_yeK")]?.ToString() ?? "";
                //string k3 = reader[BaoMatAES.TraLaiTenChoMeoCam("3_yeK")]?.ToString() ?? "";
                //string k4 = reader[BaoMatAES.TraLaiTenChoMeoCam("4_yeK")]?.ToString() ?? "";
                string k1 = reader["Key_1"]?.ToString() ?? "";
                string k2 = reader["Key_2"]?.ToString() ?? "";
                string k3 = reader["Key_3"]?.ToString() ?? "";
                string k4 = reader["Key_4"]?.ToString() ?? "";
                // GỠ BẪY: Cắt bỏ 3 ký tự "v2|" đánh lừa ở đầu Key_3 và Key_4
                if (k3.StartsWith("v2|")) k3 = k3.Substring(3);
                if (k4.StartsWith("v2|")) k4 = k4.Substring(3);
                // GHÉP CHUỖI VÀ GIẢI MÃ BẰNG MODULE V2
                BaoMatAES.yeuMeoCam_v1 = BaoMatAES.GiaiMa(k1 + k3);     // Lấy lại "PHANMEMTHIDUA-2026-KEY-256BIT"
                BaoMatAES.yeuChuMeoCam_v1 = BaoMatAES.GiaiMa(k2 + k4);  // Lấy lại "IV-THIDUA-2026KEY"
                // Trả về true nếu giải mã thành công (chuỗi không rỗng)
                return !string.IsNullOrWhiteSpace(BaoMatAES.yeuMeoCam_v1) && !string.IsNullOrWhiteSpace(BaoMatAES.yeuChuMeoCam_v1);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Lỗi giải mã khóa ẩn: {ex.Message}");
        }
        return false;
    }
    public static bool DuongVaoTraiTimEm()
    {
        try
        {
            // 1. Kích hoạt truy tìm và ghép nối Key từ CSDL
            if (!TruyenThuyetGhepManhVoKhoa())
            {
                MessageBox.Show("Cơ sở dữ liệu khởi động bị hỏng hoặc mất khóa an toàn!", "Lỗi bảo mật", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            // 2. Chuẩn bị dữ liệu thô
            string keyPath = Path.Combine(AppContext.BaseDirectory, ".uninstall.key");
            string token = Guid.NewGuid().ToString("N");
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string raw = token + "|PMTD2026";
            // ⭐ BƯỚC QUAN TRỌNG: Gọi hàm mã hóa để tạo chữ ký (Signature)
            string signature = BaoMatAES.MotDoiNguoiMotRungCayv1(raw);
            // 3. Tối ưu bộ nhớ: XÓA NGAY biến Key & IV trên RAM SAU KHI xài xong
            // (Nên dùng string.Empty thay vì null để an toàn hơn cho các thao tác chuỗi)
            BaoMatAES.yeuMeoCam_v1 = string.Empty;
            BaoMatAES.yeuChuMeoCam_v1 = string.Empty;
            // Kiểm tra xem chữ ký có tạo thành công không
            if (string.IsNullOrEmpty(signature)) return false;

            // 4. Tạo nội dung file
            string content = token + Environment.NewLine +
                             timestamp + Environment.NewLine +
                             signature;

            // 5. Nếu file tồn tại → bỏ attribute trước khi ghi (tránh lỗi UnauthorizedAccessException)
            if (File.Exists(keyPath))
            {
                File.SetAttributes(keyPath, FileAttributes.Normal);
            }

            // 6. Ghi file
            File.WriteAllText(keyPath, content, Encoding.UTF8);

            // 7. Set Hidden + System để giấu file
            File.SetAttributes(keyPath, FileAttributes.Hidden | FileAttributes.System);

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể tạo khóa gỡ cài đặt do lỗi quyền truy cập hệ thống:\n" + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }
    //Code hỗ trợ from6 v3| (Chuẩn dành riêng cho việc mã hóa gia cố thêm lớp bảo vệ chuỗi B1 SHEET BAO_CA0_TONG_HOP Ở B1 - bảo vệ chuỗi V2| thô)
    private static string _cachedSig = null;
    private static readonly Random _rnd = new Random();
    // 1. LẤY CHỮ KÝ HỆ THỐNG TỪ CSDL 1 (KEY_1 + KEY_3)
    private static string LaySignatureHeThong()
    {
        if (!string.IsNullOrEmpty(_cachedSig)) return _cachedSig;
        try
        {
            // Sử dụng đường dẫn từ Module_DanduongGPS để đảm bảo ổn định
            string path = Module_DanduongGPS.DuongDanCSDL1;
            using var conn = new SqliteConnection($"Data Source={path};Pooling=True;");
            conn.Open();
            using var cmd = conn.CreateCommand();
            // Dùng TraLaiTenChoMeoCam để che giấu câu query nếu cần
            cmd.CommandText = "SELECT Key_1, Key_3 FROM Key_KhoiDong WHERE ID = 1";
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                _cachedSig = (reader["Key_1"]?.ToString() ?? "") + (reader["Key_3"]?.ToString() ?? "");
            }
        }
        catch { _cachedSig = "SIG_MAC_DINH_2026_MEO_CAM"; }
        return _cachedSig;
    }


    //Phần này dành cho việc xuất tệp EXCEL BAO_CA0_TONG_HOP có chứa dữ liệu nhạy cảm, nên tôi tạo cơ chế mã hóa gia cố thêm 1 lớp nữa (V3)
    //để bảo vệ dữ liệu tốt hơn, tránh bị lộ khi người dùng mở tệp bằng Excel mà không qua phần mềm.
    //Cơ chế này sẽ tạo ra một payload gồm Salt + IV + Ciphertext và kèm theo HMAC để đảm bảo tính toàn vẹn dữ liệu.
    //Khi giải mã, phần mềm sẽ kiểm tra HMAC trước khi giải mã để đảm bảo dữ liệu không bị giả mạo hoặc sửa đổi.
    private const string PREFIX_STEALTH_V3 = "v3|"; // Phiên bản mã hóa dành cho việc bảo vệ dữ liệu tệp excel ở B1 SHEET BAO_CA0_TONG_HOP
    // HÀM TẠO KEY DÙNG CHUNG VỚI MODULE AES
    private static byte[] TaoKeyBaoMatV3(byte[] salt)
    {
        try
        {
            Aes aes = _aesLocal.Value!;

            // Dùng KEY GỐC đang chạy của hệ thống
            byte[] baseKey = aes.Key;

            // PBKDF2 gia cố
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password: baseKey,
                salt: salt,
                iterations: 2000,
                hashAlgorithm: HashAlgorithmName.SHA256);

            return pbkdf2.GetBytes(32);
        }
        catch
        {
            return SHA256.HashData(
                Encoding.UTF8.GetBytes("PMTD2026_FALLBACK_KEY"));
        }
    }
    // MÃ HÓA GIA CỐ V3
    public static string MaHoaGiaCo(string dataGoc)
    {
        if (string.IsNullOrWhiteSpace(dataGoc))
            return string.Empty;

        try
        {
            // =========================
            // RANDOM SALT
            // =========================
            byte[] salt = new byte[16];
            RandomNumberGenerator.Fill(salt);

            // =========================
            // RANDOM IV
            // =========================
            byte[] iv = new byte[16];
            RandomNumberGenerator.Fill(iv);

            // =========================
            // TẠO KEY
            // =========================
            byte[] key = TaoKeyBaoMatV3(salt);

            byte[] plainBytes = Encoding.UTF8.GetBytes(dataGoc);

            byte[] cipherBytes;

            // =========================
            // AES-256-CBC
            // =========================
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;

                aes.Key = key;
                aes.IV = iv;

                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using ICryptoTransform encryptor = aes.CreateEncryptor();

                cipherBytes = encryptor.TransformFinalBlock(
                    plainBytes,
                    0,
                    plainBytes.Length);
            }

            // =========================
            // PAYLOAD = SALT + IV + DATA
            // =========================
            byte[] payload = new byte[
                salt.Length +
                iv.Length +
                cipherBytes.Length];

            Buffer.BlockCopy(salt, 0, payload, 0, salt.Length);

            Buffer.BlockCopy(
                iv,
                0,
                payload,
                salt.Length,
                iv.Length);

            Buffer.BlockCopy(
                cipherBytes,
                0,
                payload,
                salt.Length + iv.Length,
                cipherBytes.Length);

            // =========================
            // HMACSHA256
            // =========================
            byte[] hmac;

            using (var h = new HMACSHA256(key))
            {
                hmac = h.ComputeHash(payload);
            }

            // =========================
            // FINAL = PAYLOAD + HMAC
            // =========================
            byte[] finalData = new byte[
                payload.Length +
                hmac.Length];

            Buffer.BlockCopy(
                payload,
                0,
                finalData,
                0,
                payload.Length);

            Buffer.BlockCopy(
                hmac,
                0,
                finalData,
                payload.Length,
                hmac.Length);

            // =========================
            // BASE64
            // =========================
            string result =
                PREFIX_STEALTH_V3 +
                Convert.ToBase64String(finalData);

            // =========================
            // XÓA MEMORY NHẠY CẢM
            // =========================
            CryptographicOperations.ZeroMemory(key);

            return result;
        }
        catch
        {
            return string.Empty;
        }
    }
    // GIẢI MÃ NHẬN DẠNG
    public static string GiaiMaNhanDang(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        try
        {
            // ==================================================
            // V3
            // ==================================================
            if (input.StartsWith(
                PREFIX_STEALTH_V3,
                StringComparison.Ordinal))
            {
                string b64 =
                    input.Substring(PREFIX_STEALTH_V3.Length);

                byte[] allBytes =
                    Convert.FromBase64String(b64);

                // =========================
                // TÁCH HMAC
                // =========================
                int hmacLength = 32;

                if (allBytes.Length < 64)
                    return string.Empty;

                int payloadLength =
                    allBytes.Length - hmacLength;

                byte[] payload = new byte[payloadLength];
                byte[] hmac = new byte[hmacLength];

                Buffer.BlockCopy(
                    allBytes,
                    0,
                    payload,
                    0,
                    payloadLength);

                Buffer.BlockCopy(
                    allBytes,
                    payloadLength,
                    hmac,
                    0,
                    hmacLength);

                // =========================
                // TÁCH SALT
                // =========================
                byte[] salt = new byte[16];

                Buffer.BlockCopy(
                    payload,
                    0,
                    salt,
                    0,
                    16);

                // =========================
                // TẠO KEY
                // =========================
                byte[] key = TaoKeyBaoMatV3(salt);

                // =========================
                // VERIFY HMAC
                // =========================
                using (var h = new HMACSHA256(key))
                {
                    byte[] computed =
                        h.ComputeHash(payload);

                    bool valid =
                        CryptographicOperations.FixedTimeEquals(
                            computed,
                            hmac);

                    if (!valid)
                        return string.Empty;
                }

                // =========================
                // LẤY IV
                // =========================
                byte[] iv = new byte[16];

                Buffer.BlockCopy(
                    payload,
                    16,
                    iv,
                    0,
                    16);

                // =========================
                // LẤY DATA
                // =========================
                int cipherLength =
                    payload.Length - 32;

                byte[] cipher = new byte[cipherLength];

                Buffer.BlockCopy(
                    payload,
                    32,
                    cipher,
                    0,
                    cipherLength);

                // =========================
                // AES DECRYPT
                // =========================
                using Aes aes = Aes.Create();

                aes.KeySize = 256;
                aes.BlockSize = 128;

                aes.Key = key;
                aes.IV = iv;

                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using ICryptoTransform decryptor =
                    aes.CreateDecryptor();

                byte[] decrypted =
                    decryptor.TransformFinalBlock(
                        cipher,
                        0,
                        cipher.Length);

                CryptographicOperations.ZeroMemory(key);

                return Encoding.UTF8
                    .GetString(decrypted)
                    .Trim('\0')
                    .Trim();
            }

            // ==================================================
            // TƯƠNG THÍCH NGƯỢC V2
            // ==================================================
            if (input.StartsWith(
                PREFIX_V2,
                StringComparison.Ordinal))
            {
                return GiaiMa(input);
            }

            // ==================================================
            // TƯƠNG THÍCH HỆ CŨ STEALTH
            // ==================================================
            try
            {
                string sig = LaySignatureHeThong();

                char[] arr = input.ToCharArray();

                Array.Reverse(arr);

                string base64 = new string(arr);

                int padding =
                    4 - (base64.Length % 4);

                if (padding < 4)
                {
                    base64 =
                        base64.PadRight(
                            base64.Length + padding,
                            '=');
                }

                byte[] bytes =
                    Convert.FromBase64String(base64);

                string decoded =
                    Encoding.UTF8.GetString(bytes);

                if (decoded.Contains(sig))
                {
                    int index =
                        decoded.IndexOf(sig);

                    return decoded.Substring(
                        index + sig.Length);
                }
            }
            catch { }

            return input;
        }
        catch
        {
            return string.Empty;
        }
    }
    ////Gọi tắt là Module V2 (Đây là phần code bổ sung cơ chế bảo vệ tái tạo csdl mã hóa và giải mã
    //public static class BaoMatAES
    //Mã hóa và giải mã các tệp cơ sở dữ liệu csd1.db, csdl2.db, csdl3.db, csdl4.db, csdlex.xlsx ở thư mục dữ liệu của phần mềm (Đảm bảo tính toàn vẹn và bảo mật dữ liệu người dùng)

    // Định danh cấu trúc file nhị phân phân đoạn thế hệ mới (v2026 GCM-Stream)
    private static readonly byte[] MAGIC_BYTES = Encoding.ASCII.GetBytes("PMTD");
    private const ushort FILE_FORMAT_VERSION_STREAM = 0x2026;
    private const int GCM_NONCE_SIZE = 12;
    private const int GCM_TAG_SIZE = 16;
    private const int CHUNK_DATA_SIZE = 65536; // Khối Plaintext cố định 64KB
    private const int CHUNK_TOTAL_SIZE = GCM_TAG_SIZE + CHUNK_DATA_SIZE; // Khối Ciphertext bao gồm cả Tag
    /// <summary>
    /// 🔐 HÀM MÃ HÓA CSDL CHUẨN ENTERPRISE (STREAMING CHUNKED AES-GCM)
    /// RAM đóng băng ở mức tối thiểu, xử lý tệp vô hạn dung lượng, an toàn tuyệt đối cho đĩa HDD cũ.
    /// </summary>
    public static void MaHoaCSDL(string sourceFile, string destFile, byte[] key)
    {
        if (key == null || key.Length != 32)
        {
            throw new CryptographicException("Rủi ro hệ thống: Khóa mật mã đầu vào không chuẩn độ dài AES-256 (32 bytes).");
        }

        // Tạo Base Nonce ngẫu nhiên gốc
        byte[] baseNonce = new byte[GCM_NONCE_SIZE];
        RandomNumberGenerator.Fill(baseNonce);

        using (FileStream fsIn = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, CHUNK_DATA_SIZE, FileOptions.SequentialScan))
        using (FileStream fsOut = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None, CHUNK_DATA_SIZE, FileOptions.WriteThrough))
        {
            long fileLength = fsIn.Length;
            long totalChunks = (long)Math.Ceiling((double)fileLength / CHUNK_DATA_SIZE);

            // 1. Ghi cấu trúc Header quản lý hệ thống (Kích thước: 4B + 2B + 12B + 8B = 26 Bytes)
            fsOut.Write(MAGIC_BYTES, 0, MAGIC_BYTES.Length);
            fsOut.Write(BitConverter.GetBytes(FILE_FORMAT_VERSION_STREAM), 0, 2);
            fsOut.Write(baseNonce, 0, baseNonce.Length);
            fsOut.Write(BitConverter.GetBytes(totalChunks), 0, 8);

            byte[] plaintextBuffer = new byte[CHUNK_DATA_SIZE];
            byte[] ciphertextBuffer = new byte[CHUNK_DATA_SIZE];
            byte[] currentNonce = new byte[GCM_NONCE_SIZE];
            byte[] tag = new byte[GCM_TAG_SIZE];

            using (AesGcm aesGcm = new AesGcm(key, GCM_TAG_SIZE))
            {
                for (long chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
                {
                    // Đọc tối đa 64KB dữ liệu
                    int bytesRead = fsIn.Read(plaintextBuffer, 0, CHUNK_DATA_SIZE);
                    if (bytesRead == 0) break;

                    // Tạo Nonce độc nhất cho khối hiện tại bằng cách kết hợp Base Nonce với chỉ mục Khối
                    XayDungChunkNonce(baseNonce, chunkIndex, currentNonce);

                    // Cắt lát mảng byte để xử lý khối cuối cùng (nếu tệp lẻ dung lượng)
                    ReadOnlySpan<byte> plainSpan = new ReadOnlySpan<byte>(plaintextBuffer, 0, bytesRead);
                    Span<byte> cipherSpan = new Span<byte>(ciphertextBuffer, 0, bytesRead);

                    // Thực thi mã hóa phân đoạn
                    aesGcm.Encrypt(currentNonce, plainSpan, cipherSpan, tag);

                    // Ghi cấu trúc khối: [TAG (16B)] [CIPHERTEXT (N Bytes)]
                    fsOut.Write(tag, 0, tag.Length);
                    fsOut.Write(ciphertextBuffer, 0, bytesRead);
                }
            }

            // Ép xả bộ đệm đĩa cứng ngay lập tức nhằm chống mất dữ liệu khi mất điện đột ngột
            fsOut.Flush(true);

            // Khử sạch vùng nhớ đệm RAM
            CryptographicOperations.ZeroMemory(plaintextBuffer);
            CryptographicOperations.ZeroMemory(ciphertextBuffer);
        }
    }
    /// <summary>
    /// 🔓 HÀM GIẢI MÃ CSDL CHUẨN ENTERPRISE (STREAMING CHUNKED AES-GCM)
    /// Kiểm tra tính toàn vẹn thời gian thực trên từng khối dữ liệu (Real-time Tamper Verification).
    /// </summary>
    public static void GiaiMaCSDL(string sourceFile, string destFile, byte[] key)
    {
        if (key == null || key.Length != 32)
        {
            throw new CryptographicException("Rủi ro hệ thống: Khóa giải mã đầu vào không đạt chuẩn AES-256.");
        }

        using (FileStream fsIn = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, CHUNK_DATA_SIZE, FileOptions.SequentialScan))
        {
            // Thẩm định kích thước tối thiểu của Header hệ thống (26 Bytes)
            if (fsIn.Length < 26)
            {
                throw new CryptographicException("Giải mã thất bại: Tệp cấu hình nhị phân hỏng cấu trúc Header.");
            }

            // 1. Kiểm tra Magic Bytes chống nhận diện sai định dạng
            byte[] magic = new byte[4];
            if (fsIn.Read(magic, 0, 4) != 4 || !CryptographicOperations.FixedTimeEquals(magic, MAGIC_BYTES))
            {
                throw new CryptographicException("Xác thực thất bại: Tệp tin không phải định dạng bảo mật của hệ thống.");
            }

            // 2. Kiểm tra Version cấu trúc
            byte[] versionBytes = new byte[2];
            fsIn.Read(versionBytes, 0, 2);
            ushort version = BitConverter.ToUInt16(versionBytes, 0);
            if (version != FILE_FORMAT_VERSION_STREAM)
            {
                throw new CryptographicException($"Xung đột phân hệ: Phiên bản tệp (0x{version:X4}) không tương thích bộ giải mã v2026.");
            }

            // 3. Trích xuất Base Nonce và Tổng số khối dữ liệu
            byte[] baseNonce = new byte[GCM_NONCE_SIZE];
            byte[] totalChunksBytes = new byte[8];

            if (fsIn.Read(baseNonce, 0, GCM_NONCE_SIZE) != GCM_NONCE_SIZE || fsIn.Read(totalChunksBytes, 0, 8) != 8)
            {
                throw new CryptographicException("Lỗi cấu trúc: File bị cắt cụt dữ liệu tại phân đoạn tham số bảo mật.");
            }
            long totalChunks = BitConverter.ToInt64(totalChunksBytes, 0);

            // 4. Mở luồng ghi tệp tin vận hành đích
            using (FileStream fsOut = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None, CHUNK_DATA_SIZE, FileOptions.WriteThrough))
            using (AesGcm aesGcm = new AesGcm(key, GCM_TAG_SIZE))
            {
                byte[] tag = new byte[GCM_TAG_SIZE];
                byte[] ciphertextBuffer = new byte[CHUNK_DATA_SIZE];
                byte[] plaintextBuffer = new byte[CHUNK_DATA_SIZE];
                byte[] currentNonce = new byte[GCM_NONCE_SIZE];

                for (long chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
                {
                    // Đọc thẻ Tag xác thực của Khối hiện tại
                    if (fsIn.Read(tag, 0, GCM_TAG_SIZE) != GCM_TAG_SIZE)
                    {
                        throw new CryptographicException($"Lỗi cấu trúc: Khối thứ {chunkIndex} bị hỏng hoặc mất thẻ Tag xác thực.");
                    }

                    // Tính toán kích thước Khối Ciphertext cần đọc (Khối cuối cùng có thể nhỏ hơn 64KB)
                    long remainingData = fsIn.Length - fsIn.Position;
                    int currentChunkSize = (chunkIndex == totalChunks - 1)
                        ? (int)(remainingData)
                        : CHUNK_DATA_SIZE;

                    // ⚠️ [GIA CỐ]: Đảm bảo đọc đủ số byte từ đĩa cứng, chống tràn/thiếu byte (Overflow Check)
                    int bytesRead = fsIn.Read(ciphertextBuffer, 0, currentChunkSize);
                    if (bytesRead != currentChunkSize)
                    {
                        throw new CryptographicException($"Lỗi phân hệ đĩa: Đọc thiếu byte tại khối thứ {chunkIndex} (Yêu cầu: {currentChunkSize}B, Thực tế: {bytesRead}B).");
                    }

                    // Tái dựng Nonce chuẩn xác theo chỉ mục Khối
                    XayDungChunkNonce(baseNonce, chunkIndex, currentNonce);

                    Span<byte> cipherSpan = new Span<byte>(ciphertextBuffer, 0, bytesRead);
                    Span<byte> plainSpan = new Span<byte>(plaintextBuffer, 0, bytesRead);

                    try
                    {
                        // Giải mã kết hợp xác thực thời gian thực (Real-time Tag Validation)
                        aesGcm.Decrypt(currentNonce, cipherSpan, tag, plainSpan);
                    }
                    catch (CryptographicException ex)
                    {
                        CryptographicOperations.ZeroMemory(ciphertextBuffer);
                        CryptographicOperations.ZeroMemory(plaintextBuffer);
                        throw new CryptographicException($"[BÁO ĐỘNG CHÍ MẠNG]: Dữ liệu tại khối thứ {chunkIndex} đã bị sửa đổi trái phép (Authentication Tag Mismatch). Tiến trình giải mã bị chặn đứng!", ex);
                    }

                    // Ghi khối sạch ra đĩa vận hành
                    fsOut.Write(plaintextBuffer, 0, bytesRead);
                }

                fsOut.Flush(true);
                CryptographicOperations.ZeroMemory(ciphertextBuffer);
                CryptographicOperations.ZeroMemory(plaintextBuffer);
            }
        }
    }
    /// <summary>
    /// 🧮 Hàm sinh Nonce biến đổi động (Deterministic Nonce Generation)
    /// Sử dụng phép toán XOR bit để sinh Nonce độc nhất từ Base Nonce và chỉ mục khối, đảm bảo không trùng lặp.
    /// </summary>
    private static void XayDungChunkNonce(byte[] baseNonce, long chunkIndex, byte[] outNonce)
    {
        Buffer.BlockCopy(baseNonce, 0, outNonce, 0, GCM_NONCE_SIZE);
        byte[] indexBytes = BitConverter.GetBytes(chunkIndex);

        // XOR 8 bytes của chỉ mục vào 8 bytes cuối của Nonce gốc
        for (int i = 0; i < indexBytes.Length; i++)
        {
            outNonce[GCM_NONCE_SIZE - 1 - i] ^= indexBytes[i];
        }
    }

    //Hỗ trợ form12 về 2 phần mềm sao lưu và khôi phục
    public static string XuyenKhongVeThoiMinh(byte[] data)
    {
        try
        {
            if (data == null || data.Length == 0) return string.Empty;

            return Encoding.UTF8.GetString(data);
        }
        catch
        {
            return string.Empty;
        }
    }
    // ĐẢO CHUỖI HIỆU NĂNG CAO
    public static string XinTraLaiThoiGian(string s)

    {
        if (string.IsNullOrEmpty(s)) return s;

        return string.Create(s.Length, s, (span, str) =>
        {
            for (int i = 0, j = str.Length - 1; i < str.Length; i++, j--)
            {
                span[i] = str[j];
            }
        });
    }
}