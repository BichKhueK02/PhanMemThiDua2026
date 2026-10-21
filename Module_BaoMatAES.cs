///Phiên bản 2
///
using Microsoft.Data.Sqlite;
using PhanMemThiDua2026;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
//Gọi tắt là Module V2
public static class Module_BaoMatAES
{
    private const int MAX_STRING_LENGTH = 256;
    // Tối ưu RAM: Giảm cache size xuống 10.000 là quá đủ cho grid 5000 dòng
    private const int MAX_CACHE_SIZE = 10000;
    private const string PREFIX_V2 = "v2|"; //Phiên bản mã hóa dành cho hệ thống
    public static string yeuMeoCam_v1 = string.Empty;
    public static string yeuChuMeoCam_v1 = string.Empty;
    //  Module_BaoMatAES - BẢN TỐI ƯU MaHoa / GiaiMa
    //  GIỮ NGUYÊN 100%: thuật toán (AES-CBC, PKCS7), cách sinh khóa/IV từ _aesLocal,
    //  định dạng "v2|" + Base64(IV16 + Cipher), đường giải mã v1 (IV mặc định),
    //  quy tắc Trim('\0').Trim(), điều kiện cache, và "lỗi => trả chuỗi rỗng".
    //  => Dữ liệu cũ (v1/v2) giải mã như trước; dữ liệu mới vẫn đọc được bằng bản cũ.
    //
    //  CÁCH ÁP DỤNG (chỉ 4 việc, các hàm khác của module KHÔNG đổi):
    //   1) Thêm dòng:  using System.Buffers;          (đầu file)
    //   2) Thêm 2 khai báo ở MỤC 1 vào cạnh các field cache hiện có
    //   3) Thay nguyên hàm MaHoa, GiaiMa, DonNheCache bằng MỤC 2, 3, 4
    //   4) (Tùy chọn) dán MỤC 5 để tự kiểm tra tương thích với dữ liệu thật
    // MỤC 1: KHAI BÁO MỚI
    // Đếm lượt "miss" cache để chỉ kiểm tra kích thước cache định kỳ
    // (ConcurrentDictionary.Count khóa TẤT CẢ bucket -> trước đây gọi ở MỌI lượt miss).
    // MỤC 1: KHAI BÁO MỚI
    // Đếm lượt "miss" cache để chỉ kiểm tra kích thước cache định kỳ
    // (ConcurrentDictionary.Count khóa TẤT CẢ bucket -> trước đây gọi ở MỌI lượt miss).
    private static int _giaiMaMissCount;
    // Vùng đệm trên stack cho chuỗi ngắn (tên, đơn vị, phân loại...): không cấp phát heap, không tạo rác cho GC.
    private const int STACK_BYTES = 1024;
    private static readonly ConcurrentDictionary<string, string> _giaiMaCache = new(Math.Max(Environment.ProcessorCount, 4), 16411, StringComparer.Ordinal);
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
    // ======================================================================
    //  MaHoa / GiaiMa - BẢN TỐI ƯU LẦN 2 (ưu tiên máy yếu)
    //  GIỮ NGUYÊN 100%: AES-CBC + PKCS7, khóa/IV gốc từ _aesLocal, định dạng "v2|" + Base64(IV16 + Cipher),
    //  giải mã v1 (IV mặc định), Trim('\0').Trim(), điều kiện cache, "lỗi => chuỗi rỗng".
    //
    //  ĐIỂM MỚI:
    //  1) "Động cơ AES" riêng cho mỗi luồng: tạo 1 lần bộ AES-ECB (không đệm) rồi tự ghép CBC bằng XOR.
    //     Aes.EncryptCbc/DecryptCbc (one-shot) phải dựng lại khóa + cấp phát handle CNG ở MỖI lần gọi
    //     (vài micro-giây, rất đáng kể trên máy yếu). Bộ ECB tái sử dụng chỉ gọi BCryptEncrypt/Decrypt.
    //  2) Giải mã: ECB giải toàn bộ khối trong 1 lệnh, rồi XOR với khối mã liền trước (đúng định nghĩa CBC).
    //     Kiểm tra PKCS7 bằng tay => dữ liệu hỏng/văn bản thường KHÔNG còn ném CryptographicException.
    //  3) Mã hóa: chuỗi ngắn (<= 79 byte UTF-8) dùng động cơ; chuỗi dài vẫn dùng EncryptCbc 1 lần (nhanh hơn khi nhiều khối).
    //  4) IV vẫn là ngẫu nhiên CSPRNG, nhưng sinh theo lô 64 IV/lần thay vì gọi hệ điều hành mỗi lượt.
    //  5) Bộ đệm giải mã tạo sẵn dung lượng => không bị "resize" nhiều lần khi nạp lưới vài nghìn dòng.
    //  6) Tự rơi về đường cũ nếu động cơ gặp lỗi (cờ _dongCoHong) => không bao giờ làm hỏng dữ liệu.
    // ======================================================================
    private static volatile bool _dongCoHong;   // true => dùng đường cũ (EncryptCbc/DecryptCbc 1 lần)
    private static volatile bool _tatDongCo;    // chỉ để đo so sánh trong DoTocDo / KiemTraTuongThichMaHoa
    private const int NGUONG_DONG_CO_MA_HOA = 79; // <= 5 khối AES => động cơ nhanh hơn one-shot (chỉnh bằng DoTocDo nếu cần)
    private sealed class DongCoAes : IDisposable
    {
        public const int BO_DEM = 2048;
        private const int IV_POOL_BYTES = 16 * 64;
        private readonly Aes _ecb;
        public readonly ICryptoTransform Dec;
        public readonly ICryptoTransform Enc;
        public readonly byte[] In = new byte[BO_DEM];
        public readonly byte[] Out = new byte[BO_DEM];
        public readonly byte[] IvMacDinh = new byte[16];
        private readonly byte[] _ivPool = new byte[IV_POOL_BYTES];
        private int _ivPos = IV_POOL_BYTES;
        public DongCoAes(Aes goc)
        {
            byte[] key = goc.Key;
            try
            {
                _ecb = Aes.Create();
                _ecb.Mode = CipherMode.ECB;
                _ecb.Padding = PaddingMode.None;
                _ecb.Key = key;
                Dec = _ecb.CreateDecryptor();
                Enc = _ecb.CreateEncryptor();
            }
            finally { CryptographicOperations.ZeroMemory(key); }
            goc.IV.CopyTo(IvMacDinh, 0);
        }
        // IV ngẫu nhiên (CSPRNG) lấy từ lô sinh sẵn; ô đã dùng được xóa ngay.
        public void LayIv(Span<byte> dich)
        {
            if (_ivPos >= IV_POOL_BYTES)
            {
                RandomNumberGenerator.Fill(_ivPool);
                _ivPos = 0;
            }
            Span<byte> o = _ivPool.AsSpan(_ivPos, 16);
            o.CopyTo(dich);
            o.Clear();
            _ivPos += 16;
        }
        public void Dispose()
        {
            Dec.Dispose();
            Enc.Dispose();
            _ecb.Dispose();
            Array.Clear(In, 0, In.Length);
            Array.Clear(Out, 0, Out.Length);
        }
    }
    private static readonly ThreadLocal<DongCoAes?> _dongCo = new(() =>
    {
        try { return new DongCoAes(_aesLocal.Value!); }
        catch (Exception ex)
        {
            Debug.WriteLine("Lỗi khởi tạo động cơ AES: " + ex.Message);
            _dongCoHong = true;
            return null;
        }
    }, trackAllValues: true);
    private static DongCoAes? LayDongCo()
    {
        if (_dongCoHong || _tatDongCo) return null;
        try { return _dongCo.Value; }
        catch (ObjectDisposedException) { return null; }
    }
    // a ^= b cho đúng 16 byte (1 khối AES)
    private static void Xor16(Span<byte> dst, ReadOnlySpan<byte> src)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(dst,
            BinaryPrimitives.ReadUInt64LittleEndian(dst) ^ BinaryPrimitives.ReadUInt64LittleEndian(src));
        BinaryPrimitives.WriteUInt64LittleEndian(dst.Slice(8),
            BinaryPrimitives.ReadUInt64LittleEndian(dst.Slice(8)) ^ BinaryPrimitives.ReadUInt64LittleEndian(src.Slice(8)));
    }
    // Làm nóng thư viện mật mã (nạp bcrypt + JIT) ở luồng nền để lần mã hóa/giải mã ĐẦU TIÊN không bị khựng.
    // Gọi 1 lần lúc khởi động:  _ = Task.Run(Module_BaoMatAES.KhoiDongNong);
    // An toàn: bước 1 dùng khóa tạm; bước 2 chỉ chạy khi khóa hệ thống đã nạp (sau LoiChaoTuSiberia).
    public static void KhoiDongNong()
    {
        try
        {
            using (Aes a = Aes.Create())
            {
                a.Key = new byte[32];
                Span<byte> iv = stackalloc byte[16];
                RandomNumberGenerator.Fill(iv);
                byte[] ct = a.EncryptCbc(Encoding.UTF8.GetBytes("khoi-dong-aes"), iv, PaddingMode.PKCS7);
                _ = a.DecryptCbc(ct, iv, PaddingMode.PKCS7);
                a.Mode = CipherMode.ECB;
                a.Padding = PaddingMode.None;
                byte[] blk = new byte[16], dst = new byte[16];
                using (ICryptoTransform e = a.CreateEncryptor()) e.TransformBlock(blk, 0, 16, dst, 0);
                using (ICryptoTransform d = a.CreateDecryptor()) d.TransformBlock(dst, 0, 16, blk, 0);
            }
            if (!string.IsNullOrEmpty(Module_DanduongGPS.ToiYeuMeoCam1) && !string.IsNullOrEmpty(Module_DanduongGPS.ToiCanChuIVMeoCam1))
            {
                string t = MaHoa("khoi-dong");
                _ = GiaiMa(t);
                _giaiMaCache.TryRemove(t, out _);
            }
        }
        catch (Exception ex) { Debug.WriteLine("KhoiDongNong: " + ex.Message); }
    }
    // MaHoa: kết quả KHÔNG đổi định dạng => bản cũ và bản mới đọc chéo được dữ liệu của nhau.
    public static string MaHoa(string chuoiGoc)
    {
        if (string.IsNullOrEmpty(chuoiGoc)) return string.Empty;
        try
        {
            int plainLen = Encoding.UTF8.GetByteCount(chuoiGoc);
            if (plainLen <= NGUONG_DONG_CO_MA_HOA)
            {
                DongCoAes? eng = LayDongCo();
                if (eng != null)
                {
                    try { return MaHoaDongCo(eng, chuoiGoc, plainLen); }
                    catch (Exception ex)
                    {
                        _dongCoHong = true;   // rơi về đường cũ, không mất dữ liệu
                        Debug.WriteLine("Động cơ AES lỗi (MaHoa): " + ex.Message);
                    }
                }
            }
            return MaHoaMotLan(chuoiGoc, plainLen);
        }
        catch { return string.Empty; }
    }
    private static string MaHoaDongCo(DongCoAes eng, string s, int plainLen)
    {
        int padded = (plainLen / 16 + 1) * 16;          // PKCS7 luôn thêm 1..16 byte
        byte[] inb = eng.In, outb = eng.Out;
        try
        {
            Encoding.UTF8.GetBytes(s, 0, s.Length, inb, 0);
            inb.AsSpan(plainLen, padded - plainLen).Fill((byte)(padded - plainLen));
            eng.LayIv(outb.AsSpan(0, 16));               // outb = [IV][cipher...]
            for (int o = 0; o < padded; o += 16)
            {
                // CBC: C[i] = AES(P[i] XOR C[i-1]); C[-1] = IV (đều nằm liền trước trong outb)
                Xor16(inb.AsSpan(o, 16), outb.AsSpan(o, 16));
                if (eng.Enc.TransformBlock(inb, o, 16, outb, o + 16) != 16)
                    throw new CryptographicException("TransformBlock");
            }
            int payloadLen = 16 + padded;
            int prefixLen = PREFIX_V2.Length;
            Span<char> chars = stackalloc char[160];     // tối đa 3 + 128
            PREFIX_V2.AsSpan().CopyTo(chars);
            if (!Convert.TryToBase64Chars(outb.AsSpan(0, payloadLen), chars.Slice(prefixLen), out int written))
                throw new CryptographicException("Base64");
            return new string(chars.Slice(0, prefixLen + written));
        }
        finally { Array.Clear(inb, 0, padded); }        // không để bản rõ nằm lại trong bộ đệm
    }
    // Đường cũ (đã tối ưu ở bản trước): 1 lần EncryptCbc trên vùng stackalloc. Dùng cho chuỗi dài / khi động cơ hỏng.
    private static string MaHoaMotLan(string chuoiGoc, int plainLen)
    {
        byte[]? rented = null;
        try
        {
            Aes aes = _aesLocal.Value!;
            int payloadMax = 16 + ((plainLen / 16) + 1) * 16;
            int need = plainLen + payloadMax;
            Span<byte> buf = need <= STACK_BYTES
                ? stackalloc byte[STACK_BYTES]
                : (rented = ArrayPool<byte>.Shared.Rent(need));
            Span<byte> plain = buf.Slice(0, plainLen);
            Span<byte> payload = buf.Slice(plainLen, payloadMax);
            Encoding.UTF8.GetBytes(chuoiGoc.AsSpan(), plain);
            Span<byte> iv = payload.Slice(0, 16);
            RandomNumberGenerator.Fill(iv);
            int cipherLen = aes.EncryptCbc(plain, iv, payload.Slice(16), PaddingMode.PKCS7);
            int payloadLen = 16 + cipherLen;
            int prefixLen = PREFIX_V2.Length;
            int totalChars = prefixLen + ((payloadLen + 2) / 3) * 4;
            Span<char> chars = totalChars <= 512 ? stackalloc char[512] : new char[totalChars];
            PREFIX_V2.AsSpan().CopyTo(chars);
            if (!Convert.TryToBase64Chars(payload.Slice(0, payloadLen), chars.Slice(prefixLen), out int charsWritten))
                return string.Empty;
            return new string(chars.Slice(0, prefixLen + charsWritten));
        }
        finally
        {
            if (rented != null) ArrayPool<byte>.Shared.Return(rented, clearArray: true);
        }
    }
    // GiaiMa: cache -> (động cơ ECB | đường cũ). Chuỗi không hợp lệ => chuỗi rỗng, KHÔNG cache, KHÔNG ném ngoại lệ.
    public static string GiaiMa(string chuoiMaHoa)
    {
        if (string.IsNullOrEmpty(chuoiMaHoa)) return string.Empty;
        // Đọc từ Cache giúp load dữ liệu CSDL nhanh chóng
        if (_giaiMaCache.TryGetValue(chuoiMaHoa, out string? cached)) return cached!;
        string? kq;
        try { kq = GiaiMaThat(chuoiMaHoa); }
        catch { return string.Empty; }
        if (kq == null) return string.Empty;
        // Lưu cache giải mã để các lần hiển thị sau trên Grid nhanh hơn
        if (chuoiMaHoa.Length <= MAX_STRING_LENGTH * 2)
        {
            // Chỉ kiểm tra kích thước cache mỗi 256 lượt miss (sai lệch tối đa ~255 mục, không đáng kể)
            if ((Interlocked.Increment(ref _giaiMaMissCount) & 0xFF) == 0 && _giaiMaCache.Count >= MAX_CACHE_SIZE)
                DonNheCache(_giaiMaCache, 1000);
            _giaiMaCache.TryAdd(chuoiMaHoa, kq);
        }
        return kq;
    }
    private static string? GiaiMaThat(string s)
    {
        bool laV2 = s.StartsWith(PREFIX_V2, StringComparison.Ordinal);
        ReadOnlySpan<char> b64 = laV2 ? s.AsSpan(PREFIX_V2.Length) : s.AsSpan();
        int maxBytes = (b64.Length / 4 + 1) * 3;           // cận trên số byte sau khi giải Base64
        if (maxBytes <= DongCoAes.BO_DEM)
        {
            DongCoAes? eng = LayDongCo();
            if (eng != null)
            {
                try { return GiaiMaDongCo(eng, b64, laV2); }
                catch (Exception ex)
                {
                    _dongCoHong = true;                    // rơi về đường cũ
                    Debug.WriteLine("Động cơ AES lỗi (GiaiMa): " + ex.Message);
                }
            }
        }
        return GiaiMaMotLan(b64, laV2, maxBytes);
    }
    private static string? GiaiMaDongCo(DongCoAes eng, ReadOnlySpan<char> b64, bool laV2)
    {
        byte[] inb = eng.In, outb = eng.Out;
        int cipherLen = 0;
        try
        {
            // Giải Base64 thẳng vào bộ đệm của luồng (không cấp phát)
            if (!Convert.TryFromBase64Chars(b64, inb, out int rawLen)) return null;
            int start;
            if (laV2) { if (rawLen < 16) return null; start = 16; }   // v2: [IV 16][cipher]
            else start = 0;                                            // v1: IV mặc định
            cipherLen = rawLen - start;
            if (cipherLen == 0 || (cipherLen & 15) != 0) return null;
            // ECB giải toàn bộ khối trong 1 lệnh, rồi XOR với khối mã liền trước (= CBC)
            if (eng.Dec.TransformBlock(inb, start, cipherLen, outb, 0) != cipherLen)
                throw new CryptographicException("TransformBlock");
            ReadOnlySpan<byte> iv0 = laV2 ? inb.AsSpan(0, 16) : eng.IvMacDinh.AsSpan();
            Span<byte> o = outb.AsSpan(0, cipherLen);
            Xor16(o.Slice(0, 16), iv0);
            for (int i = 16; i < cipherLen; i += 16)
                Xor16(o.Slice(i, 16), inb.AsSpan(start + i - 16, 16));
            // Kiểm tra PKCS7 (cùng luật với .NET): 1..16 và mọi byte đệm bằng nhau
            int pad = o[cipherLen - 1];
            if (pad == 0 || pad > 16) return null;
            for (int k = cipherLen - pad; k < cipherLen; k++)
                if (o[k] != pad) return null;
            // 🔥 Gọt sạch ký tự Null (giữ nguyên đúng thứ tự như bản gốc)
            return Encoding.UTF8.GetString(o.Slice(0, cipherLen - pad)).Trim('\0').Trim();
        }
        finally
        {
            if (cipherLen > 0) Array.Clear(outb, 0, cipherLen);    // xóa bản rõ khỏi bộ đệm
        }
    }
    // Đường cũ (bản tối ưu trước): dùng cho chuỗi rất dài / khi động cơ hỏng. Hợp lệ => chuỗi; không hợp lệ => null.
    private static string? GiaiMaMotLan(ReadOnlySpan<char> b64, bool laV2, int maxBytes)
    {
        byte[]? rented = null;
        try
        {
            Aes aes = _aesLocal.Value!;
            int need = maxBytes * 2;                       // [raw][plain]
            Span<byte> buf = need <= STACK_BYTES
                ? stackalloc byte[STACK_BYTES]
                : (rented = ArrayPool<byte>.Shared.Rent(need));
            Span<byte> raw = buf.Slice(0, maxBytes);
            Span<byte> plain = buf.Slice(maxBytes);        // luôn dài >= maxBytes >= độ dài cipher
            if (!Convert.TryFromBase64Chars(b64, raw, out int rawLen)) return null;
            Span<byte> iv = stackalloc byte[16];
            int cipherStart;
            if (laV2)
            {
                if (rawLen < 16) return null;
                raw.Slice(0, 16).CopyTo(iv);
                cipherStart = 16;
            }
            else
            {
                aes.IV.AsSpan().CopyTo(iv);
                cipherStart = 0;
            }
            int cipherLen = rawLen - cipherStart;
            if (cipherLen == 0 || (cipherLen & 15) != 0) return null;
            int plainLen = aes.DecryptCbc(raw.Slice(cipherStart, cipherLen), iv, plain, PaddingMode.PKCS7);
            return Encoding.UTF8.GetString(plain.Slice(0, plainLen)).Trim('\0').Trim();
        }
        finally
        {
            if (rented != null) ArrayPool<byte>.Shared.Return(rented, clearArray: true);
        }
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
    // MỤC 4: DonNheCache
    // Trước: foreach (var key in cache.Keys) -> .Keys sao chép TOÀN BỘ ~10.000 khóa + khóa mọi bucket.
    // Nay  : duyệt thẳng enumerator của ConcurrentDictionary (không khóa, không sao chép), kết quả y hệt.
    private static void DonNheCache(ConcurrentDictionary<string, string> cache, int soLuong)
    {
        int count = 0;
        foreach (var kv in cache)
        {
            if (cache.TryRemove(kv.Key, out _)) count++;
            if (count >= soLuong) break;
        }
    }
    private static string MaHoa_Goc(string chuoiGoc)
    {
        if (string.IsNullOrEmpty(chuoiGoc)) return string.Empty;
        try
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(chuoiGoc);
            Aes aes = _aesLocal.Value!;
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
    private static string GiaiMa_Goc(string chuoiMaHoa)
    {
        if (string.IsNullOrEmpty(chuoiMaHoa)) return string.Empty;
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
                finalResult = Encoding.UTF8.GetString(decrypted).Trim('\0').Trim();
            }
            else
            {
                byte[] cipher = Convert.FromBase64String(chuoiMaHoa);
                byte[] decrypted = aes.DecryptCbc(cipher, aes.IV, PaddingMode.PKCS7);
                finalResult = Encoding.UTF8.GetString(decrypted).Trim('\0').Trim();
            }
            return finalResult;
        }
        catch { return string.Empty; }
    }
    // Kết quả phải là "KHÔNG CÓ KHÁC BIỆT". Nếu có dòng "KHÁC" => đừng triển khai và gửi lại cho mình.
    // Bản này kiểm tra cả 3 đường giải mã (gốc / động cơ ECB / one-shot dự phòng) và vòng MaHoa(mới) -> GiaiMa(gốc).
    public static string KiemTraTuongThichMaHoa(IEnumerable<string>? mauTuCSDL = null)
    {
        var sb = new StringBuilder();
        int ok = 0, khac = 0;
        void SoSanhGiaiMa(string input, string nhan)
        {
            string a = GiaiMa_Goc(input);
            _giaiMaCache.TryRemove(input, out _);          // ép chạy đường giải mã thật, không lấy từ cache
            string b = GiaiMa(input);                       // đường mới (động cơ ECB)
            _giaiMaCache.TryRemove(input, out _);
            string c;
            _tatDongCo = true;
            try { c = GiaiMa(input); }                      // đường one-shot dự phòng
            finally { _tatDongCo = false; }
            _giaiMaCache.TryRemove(input, out _);
            if (string.Equals(a, b, StringComparison.Ordinal) && string.Equals(a, c, StringComparison.Ordinal)) { ok++; return; }
            khac++;
            if (khac <= 15) sb.AppendLine($"KHÁC [{nhan}] input='{input}' | goc='{a}' | dongco='{b}' | motlan='{c}'");
        }
        // 1) Dữ liệu thật từ CSDL (v1 + v2 đang có)
        if (mauTuCSDL != null)
            foreach (var s in mauTuCSDL) SoSanhGiaiMa(s, "CSDL");
        // 2) Chuỗi biên: độ dài sát bội số 16 và sát ngưỡng động cơ (79/80 byte), tiếng Việt, emoji, ký tự Null / khoảng trắng đầu cuối
        string[] mau =
        {
        "a", "Nguyễn Văn A", "Đại đội 1", new string('x', 15), new string('x', 16), new string('y', 17),
        new string('z', 31), new string('z', 32), new string('w', 33), "Tiếng Việt ƒ 😀 ✓", new string('k', 300), new string('q', 600),
        new string('m', 63), new string('m', 64), new string('m', 79), new string('m', 80), new string('m', 95), new string('m', 96),
        "Đại đội trưởng Nguyễn Văn Ánh - Tiểu đoàn 1 - Trung đoàn 2",
        " có khoảng trắng ", "\0nul\0", "abc\0 "
    };
        foreach (var p in mau)
        {
            string eMoi = MaHoa(p);
            string eGoc = MaHoa_Goc(p);
            string expect = p.Trim('\0').Trim();
            if (!eMoi.StartsWith(PREFIX_V2, StringComparison.Ordinal) || eMoi.Length != eGoc.Length)
            { khac++; sb.AppendLine($"KHÁC [định dạng MaHoa] p.Length={p.Length}: moi.Length={eMoi.Length} goc.Length={eGoc.Length}"); }
            else ok++;
            // Bản GỐC (đường độc lập) phải giải được chuỗi do MaHoa mới tạo ra và ra đúng nguyên văn
            string back = GiaiMa_Goc(eMoi);
            if (string.Equals(back, expect, StringComparison.Ordinal)) ok++;
            else { khac++; sb.AppendLine($"KHÁC [vòng MaHoa(mới)->GiaiMa(gốc)] p.Length={p.Length}: '{back}' != '{expect}'"); }
            // Hai lần mã hóa cùng 1 chuỗi phải cho 2 kết quả khác nhau (IV ngẫu nhiên)
            if (!string.Equals(eMoi, MaHoa(p), StringComparison.Ordinal)) ok++;
            else { khac++; sb.AppendLine($"KHÁC [IV trùng nhau] p.Length={p.Length}"); }
            SoSanhGiaiMa(eMoi, "MaHoa(mới) -> giải");      // mới mã -> cả ba đường giải phải ra cùng kết quả
            SoSanhGiaiMa(eGoc, "MaHoa(gốc) -> giải");       // gốc mã -> bản mới giải phải ra cùng kết quả
            // v1 (dữ liệu cũ): AES-CBC với IV mặc định, không có tiền tố
            Aes aes = _aesLocal.Value!;
            string v1 = Convert.ToBase64String(aes.EncryptCbc(Encoding.UTF8.GetBytes(p), aes.IV, PaddingMode.PKCS7));
            SoSanhGiaiMa(v1, "v1");
        }
        // 3) Đầu vào hỏng / văn bản thường: cả hai phải cùng trả chuỗi rỗng (bản mới không ném ngoại lệ)
        string[] hong =
        {
            "abc", "v2|", "v2|AAAA", "v2|!!!!", "!!!", "   ", "Phiên bản dành cho CBCS", new string('A', 24),
            "v2|" + new string('A', 44), "AAAAAAAAAAAAAAAAAAAAAA==", "v2|" + new string('A', 42) + "=="
        };
        foreach (var h in hong) SoSanhGiaiMa(h, "hỏng");
        sb.Insert(0, khac == 0
            ? $"KHÔNG CÓ KHÁC BIỆT ({ok} phép so sánh).\n"
            : $"CÓ {khac} KHÁC BIỆT / {ok + khac}.\n");
        return sb.ToString();
    }
    // Đo tốc độ thật trên CHÍNH máy này: gốc (cũ nhất) / one-shot (bản trước) / động cơ (bản mới).
    // Ví dụ:  MessageBox.Show(Module_BaoMatAES.DoTocDo());
    // Nếu "động cơ" mã hóa chậm hơn "one-shot" thì hạ NGUONG_DONG_CO_MA_HOA xuống (ví dụ 47) hoặc về 0.
    public static string DoTocDo(int soLan = 8000)
    {
        if (soLan < 1000) soLan = 1000;
        if (soLan > 9000) soLan = 9000;                    // < MAX_CACHE_SIZE để lượt "cache trúng" không bị dọn
        const string mau = "Nguyễn Văn Ánh - Đại đội 1";
        var sw = new Stopwatch();
        var sb = new StringBuilder();
        double Us(Action a) { sw.Restart(); a(); sw.Stop(); return sw.Elapsed.TotalMilliseconds * 1000.0 / soLan; }
        try
        {
            for (int i = 0; i < 300; i++) { _ = MaHoa(mau); _ = MaHoa_Goc(mau); }   // làm nóng, loại chi phí JIT lần đầu
            double mGoc = Us(() => { for (int i = 0; i < soLan; i++) _ = MaHoa_Goc(mau); });
            _tatDongCo = true;
            double mMot = Us(() => { for (int i = 0; i < soLan; i++) _ = MaHoa(mau); });
            _tatDongCo = false;
            double mMoi = Us(() => { for (int i = 0; i < soLan; i++) _ = MaHoa(mau); });
            sb.AppendLine($"MÃ HÓA (µs/lần, càng nhỏ càng nhanh) — mẫu {mau.Length} ký tự");
            sb.AppendLine($"  gốc: {mGoc:F2} | one-shot: {mMot:F2} | động cơ: {mMoi:F2}");
            var cts = new string[soLan];
            for (int i = 0; i < soLan; i++) cts[i] = MaHoa_Goc(mau);                 // mỗi chuỗi 1 IV khác nhau => cache trượt hết
            _giaiMaCache.Clear();
            double gGoc = Us(() => { foreach (var c in cts) _ = GiaiMa_Goc(c); });
            _giaiMaCache.Clear(); _tatDongCo = true;
            double gMot = Us(() => { foreach (var c in cts) _ = GiaiMa(c); });
            _giaiMaCache.Clear(); _tatDongCo = false;
            double gMoi = Us(() => { foreach (var c in cts) _ = GiaiMa(c); });
            double gHit = Us(() => { foreach (var c in cts) _ = GiaiMa(c); });       // toàn bộ đã nằm trong cache
            sb.AppendLine($"GIẢI MÃ, cache TRƯỢT (µs/lần) — nạp lưới lần đầu");
            sb.AppendLine($"  gốc: {gGoc:F2} | one-shot: {gMot:F2} | động cơ: {gMoi:F2}");
            sb.AppendLine($"GIẢI MÃ, cache TRÚNG (µs/lần): {gHit:F2}");
        }
        catch (Exception ex) { sb.AppendLine("Lỗi đo: " + ex.Message); }
        finally { _tatDongCo = false; }
        return sb.ToString();
    }
    public static string TraLaiTenChoMeoCam(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return string.Create(s.Length, s, (span, str) =>
        {
            for (int i = 0, j = str.Length - 1; i < str.Length; i++, j--) span[i] = str[j];
        });
    }
    public static void DonDeprRacTraLaiTaiNguyenHeThong()
    {
        // Nhớ xóa _maHoaCache.Clear() ở đây vì mình đã bỏ nó ở trên
        _giaiMaCache.Clear();
        if (_dongCo != null) { foreach (var d in _dongCo.Values) d?.Dispose(); _dongCo.Dispose(); }
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
            //string sqlQuery = Module_BaoMatAES.TraLaiTenChoMeoCam("1 TIMIL 1 = DI EREHW gnoDiohK_yeK MORF 4_yeK ,3_yeK ,2_yeK ,1_yeK TCELES"); using var cmd = new SqliteCommand(sqlQuery, conn);
            using var cmd = new SqliteCommand(
    "SELECT Key_1, Key_2, Key_3, Key_4 FROM Key_KhoiDong WHERE ID = 1 LIMIT 1",
    conn);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                // ĐỌC DỮ LIỆU TỪ CSDL BẰNG CÁCH LẬT NGƯỢC TÊN CỘT ĐỂ CHE GIẤU
                string k1 = reader["Key_1"]?.ToString() ?? "";
                string k2 = reader["Key_2"]?.ToString() ?? "";
                string k3 = reader["Key_3"]?.ToString() ?? "";
                string k4 = reader["Key_4"]?.ToString() ?? "";
                // GỠ BẪY: Cắt bỏ 3 ký tự "v2|" đánh lừa ở đầu Key_3 và Key_4
                if (k3.StartsWith("v2|")) k3 = k3.Substring(3);
                if (k4.StartsWith("v2|")) k4 = k4.Substring(3);
                // GHÉP CHUỖI VÀ GIẢI MÃ BẰNG MODULE V2
                Module_BaoMatAES.yeuMeoCam_v1 = Module_BaoMatAES.GiaiMa(k1 + k3);     // Lấy lại "PHANMEMTHIDUA-2026-KEY-256BIT"
                Module_BaoMatAES.yeuChuMeoCam_v1 = Module_BaoMatAES.GiaiMa(k2 + k4);  // Lấy lại "IV-THIDUA-2026KEY"
                // Trả về true nếu giải mã thành công (chuỗi không rỗng)
                return !string.IsNullOrWhiteSpace(Module_BaoMatAES.yeuMeoCam_v1) && !string.IsNullOrWhiteSpace(Module_BaoMatAES.yeuChuMeoCam_v1);
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
            string signature = Module_BaoMatAES.MotDoiNguoiMotRungCayv1(raw);
            // 3. Tối ưu bộ nhớ: XÓA NGAY biến Key & IV trên RAM SAU KHI xài xong
            // (Nên dùng string.Empty thay vì null để an toàn hơn cho các thao tác chuỗi)
            Module_BaoMatAES.yeuMeoCam_v1 = string.Empty;
            Module_BaoMatAES.yeuChuMeoCam_v1 = string.Empty;
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
            // RANDOM SALT
            byte[] salt = new byte[16];
            RandomNumberGenerator.Fill(salt);
            // RANDOM IV
            byte[] iv = new byte[16];
            RandomNumberGenerator.Fill(iv);
            // TẠO KEY
            byte[] key = TaoKeyBaoMatV3(salt);
            byte[] plainBytes = Encoding.UTF8.GetBytes(dataGoc);
            byte[] cipherBytes;
            // AES-256-CBC
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
            // PAYLOAD = SALT + IV + DATA
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
            // HMACSHA256
            byte[] hmac;
            using (var h = new HMACSHA256(key))
            {
                hmac = h.ComputeHash(payload);
            }
            // FINAL = PAYLOAD + HMAC
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
            // BASE64
            string result =
                PREFIX_STEALTH_V3 +
                Convert.ToBase64String(finalData);
            // XÓA MEMORY NHẠY CẢM
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
            // V3
            if (input.StartsWith(
                PREFIX_STEALTH_V3,
                StringComparison.Ordinal))
            {
                string b64 =
                    input.Substring(PREFIX_STEALTH_V3.Length);
                byte[] allBytes =
                    Convert.FromBase64String(b64);
                // TÁCH HMAC
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
                // TÁCH SALT
                byte[] salt = new byte[16];
                Buffer.BlockCopy(
                    payload,
                    0,
                    salt,
                    0,
                    16);
                // TẠO KEY
                byte[] key = TaoKeyBaoMatV3(salt);
                // VERIFY HMAC
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
                // LẤY IV
                byte[] iv = new byte[16];
                Buffer.BlockCopy(
                    payload,
                    16,
                    iv,
                    0,
                    16);
                // LẤY DATA
                int cipherLength =
                    payload.Length - 32;
                byte[] cipher = new byte[cipherLength];
                Buffer.BlockCopy(
                    payload,
                    32,
                    cipher,
                    0,
                    cipherLength);
                // AES DECRYPT
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
            // TƯƠNG THÍCH NGƯỢC V2
            if (input.StartsWith(
                PREFIX_V2,
                StringComparison.Ordinal))
            {
                return GiaiMa(input);
            }
            // TƯƠNG THÍCH HỆ CŨ STEALTH
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
    //public static class Module_BaoMatAES
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
        using (FileStream fsOut = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None, CHUNK_DATA_SIZE, FileOptions.None))
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
            using (FileStream fsOut = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None, CHUNK_DATA_SIZE, FileOptions.None))
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
    public static string XuyenKhongVeThoiMinhThanhToThanhTo(byte[] data)
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
    // THUẬT TOÁN MÃ HÓA RIÊNG CHO DỮ LIỆU NHẬT KÝ - V4
    public const string PREFIX_STEALTH_V2 = "AES:v2|";
    public const string PREFIX_STEALTH_V4 = "AES:v4|";
    public static string MaHoa_NhatKy(string chuoiGoc)
    {
        if (string.IsNullOrEmpty(chuoiGoc))
            return string.Empty;
        try
        {
            Aes aes = _aesLocal.Value!;
            const int nonceLength = 12;
            const int tagLength = 16;
            byte[] nonce = new byte[nonceLength];
            RandomNumberGenerator.Fill(nonce);
            byte[] plainBytes = Encoding.UTF8.GetBytes(chuoiGoc);
            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tag = new byte[tagLength];
            using (AesGcm aesGcm = new AesGcm(aes.Key, tagLength))
            {
                aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
            }
            // PAYLOAD = NONCE + CIPHERTEXT + TAG
            byte[] payload = new byte[nonceLength + cipherBytes.Length + tagLength];
            Buffer.BlockCopy(nonce, 0, payload, 0, nonceLength);
            Buffer.BlockCopy(cipherBytes, 0, payload, nonceLength, cipherBytes.Length);
            Buffer.BlockCopy(tag, 0, payload, nonceLength + cipherBytes.Length, tagLength);
            return PREFIX_STEALTH_V4 + Convert.ToBase64String(payload);
        }
        catch
        {
            return string.Empty;
        }
    }
    public static string GiaiMa_NhatKy(string chuoiMaHoa)
    {
        if (string.IsNullOrEmpty(chuoiMaHoa))
            return string.Empty;
        try
        {
            // Kiểm tra đúng tiền tố "AES:v4|"
            if (!chuoiMaHoa.StartsWith(PREFIX_STEALTH_V4, StringComparison.Ordinal))
                return string.Empty;
            const int nonceLength = 12;
            const int tagLength = 16;
            // Bóc tách chính xác phần Base64 sau tiền tố "AES:v4|"
            byte[] payload = Convert.FromBase64String(chuoiMaHoa.Substring(PREFIX_STEALTH_V4.Length));
            if (payload.Length < nonceLength + tagLength)
                return string.Empty;
            int cipherLength = payload.Length - nonceLength - tagLength;
            byte[] nonce = new byte[nonceLength];
            byte[] cipherBytes = new byte[cipherLength];
            byte[] tag = new byte[tagLength];
            Buffer.BlockCopy(payload, 0, nonce, 0, nonceLength);
            Buffer.BlockCopy(payload, nonceLength, cipherBytes, 0, cipherLength);
            Buffer.BlockCopy(payload, nonceLength + cipherLength, tag, 0, tagLength);
            byte[] plainBytes = new byte[cipherLength];
            Aes aes = _aesLocal.Value!;
            using (AesGcm aesGcm = new AesGcm(aes.Key, tagLength))
            {
                aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
            }
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            return string.Empty;
        }
    }
}