///Phiên bản 3
///
/// 
///Phiên bản 2 — bản viết lại tối ưu cho dữ liệu 100 → 5.000 người
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
//TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
//
//  CAM KẾT TƯƠNG THÍCH (không đổi định dạng dữ liệu, không đổi tên hàm công khai):
//    V1  : Base64(Cipher)                         — AES-CBC, IV mặc định (đọc được)
//    V2  : "v2|"     + Base64(IV16 + Cipher)      — AES-CBC PKCS7
//    V3  : "v3|"     + Base64(Salt16 + IV16 + Cipher + HMAC32)
//    V4  : "AES:v4|" + Base64(Nonce12 + Cipher + Tag16) — AES-GCM (nhật ký)
//    V5  : "v5|"     + Base64(Nonce12 + Cipher + Tag16) — AES-256-GCM, KHÓA PHỤ RIÊNG (dữ liệu từng ô) — MẶC ĐỊNH KHI GHI
//    CSDL: "PMTD" + 0x2026 + BaseNonce12 + TổngKhối8 + n x [Tag16 + Cipher<=64KB] — AES-GCM theo khối
//  => dữ liệu cũ (v1/v2/v3/v4) đọc được như trước, KHÔNG cần chuyển đổi CSDL: ô nào được lưu lại sẽ tự lên v5.
//  ⚠ Phần mềm bản CŨ (trước V5) KHÔNG đọc được ô v5. Còn máy nào chạy bản cũ dùng chung / nhận dữ liệu chuyển giao
//    thì đặt GhiDinhDangV5 = false cho đến khi nâng cấp hết (khi đó MaHoa ghi v2 y như trước).
//
//  V5 so với V2: có Tag xác thực 16 byte (sửa 1 bit chuỗi mã => giải mã trả rỗng, không ra dữ liệu méo),
//  khóa phụ = HMAC-SHA256(khóa hệ thống, nhãn V5) => không dùng chung khóa với CBC/nhật ký/V3; không cần đệm PKCS7.
//
//  ĐIỂM MỚI SO VỚI BẢN TRƯỚC:
//   1) Bộ đệm giải mã 60.000 mục (đủ 5.000 người x ~10 cột), đặt sẵn dung lượng bảng băm.
//   2) Sửa lỗi cờ _dongCoHong: thiếu khóa lúc khởi động không còn khóa cứng đường chậm cả phiên.
//   3) GiaiMaLo: giải mã cả lô, song song khi lô lớn (chạy trong Task.Run), nhường "đèn đỏ" đăng nhập.
//   4) LamNongDanhSachAsync: nạp sẵn bộ đệm ở nền sau đăng nhập (an toàn: khóa đệm là chính chuỗi mã).
//   5) AES-GCM nhật ký: mỗi luồng dùng lại 1 đối tượng AesGcm, không còn tạo mới + sao chép khóa ở mỗi ô.
//   6) MaHoaCoDinh (tùy chọn): mã hóa xác định cho cột phân loại ít giá trị (cùng giá trị => cùng chuỗi mã).
//   6b) GiaiMaKhongLuuDem (quét 1 lượt không làm bẩn bộ đệm) và XoaBoDemGiaiMa (nhả RAM khi đổi kỳ/khôi phục dữ liệu).
//   7) MaHoaCSDL/GiaiMaCSDL: 1 lần ghi/khối, bộ đệm ArrayPool, kiểm tra Header khớp dung lượng tệp
//      (phát hiện tệp bị cắt/sửa Header), nguồn bị thu nhỏ giữa chừng => báo lỗi thay vì tạo tệp hỏng.
public static class Module_BaoMatAES
{
    // ===================== 1. HẰNG SỐ & TRẠNG THÁI =====================
    private const int MAX_STRING_LENGTH = 256;
    // 6.000 người x ~10 cột (DanhSach) + bảng khen thưởng/đơn vị/chức vụ ≈ 70.000–90.000 chuỗi mã khác nhau
    // (IV/nonce ngẫu nhiên => mỗi ô một chuỗi riêng). Ước lượng RAM ~280 byte/mục => 100.000 mục ≈ 28 MB (tối đa);
    // 100 người chỉ dùng vài trăm KB vì bộ đệm chỉ lớn theo dữ liệu thật.
    private const int MAX_CACHE_SIZE = 100000;
    private const int CACHE_DON_MOI_LAN = 2000;
    private const int CACHE_DUNG_LUONG_BAN_DAU = 8191;    // nhỏ lúc đầu (máy 100 người không tốn RAM), tự nở khi cần
    private const string PREFIX_V2 = "v2|"; //Phiên bản mã hóa dành cho hệ thống (CBC, chỉ còn đọc + MaHoaV2/MaHoaCoDinh)
    private const string PREFIX_V5 = "v5|"; //Phiên bản mã hóa dữ liệu ô hiện hành (AES-256-GCM, khóa phụ)
    /// <summary>
    /// true (mặc định): MaHoa ghi định dạng v5 (AES-GCM có xác thực). false: ghi v2 (CBC) như các bản trước —
    /// chỉ dùng khi còn máy chạy phần mềm bản cũ cần đọc dữ liệu do máy này ghi ra. Đọc thì LUÔN hiểu cả v1/v2/v5.
    /// </summary>
    public static volatile bool GhiDinhDangV5 = true;
    // Vùng đệm trên stack cho chuỗi ngắn (tên, đơn vị, phân loại...): không cấp phát heap, không tạo rác cho GC.
    private const int STACK_BYTES = 1024;
    // Đếm lượt "miss" cache để chỉ kiểm tra kích thước cache định kỳ (ConcurrentDictionary.Count khóa TẤT CẢ bucket).
    private static int _giaiMaMissCount;
    private static readonly ConcurrentDictionary<string, string> _giaiMaCache =
        new(Math.Max(Environment.ProcessorCount, 4), CACHE_DUNG_LUONG_BAN_DAU, StringComparer.Ordinal);
    // KHÔNG có bộ đệm cho MaHoa: mỗi lần mã hóa phải sinh IV ngẫu nhiên mới.
    private static readonly ThreadLocal<Aes> _aesLocal = new(() =>
    {
        // Nguyên liệu khóa/IV lấy từ GPS (nguồn chính). KHÔNG dùng khóa dự phòng cứng ở đường này.
        try
        {
            string rawKeyFromGPS = TraLaiTenChoMeoCam(Module_DanduongGPS.ToiYeuMeoCam1 + Module_DanduongGPS.ToiYeuMeoCam2);
            string rawIVFromGPS = TraLaiTenChoMeoCam(Module_DanduongGPS.ToiCanChuIVMeoCam1 + Module_DanduongGPS.ToiCanChuIVMeoCam2);
            if (string.IsNullOrEmpty(rawKeyFromGPS) || string.IsNullOrEmpty(rawIVFromGPS))
            {
                throw new InvalidOperationException(
                    "⛔ CẢNH BÁO BẢO MẬT: GPS key không khả dụng. Không sử dụng hard-coded fallback.\n" +
                    "Hệ thống mã hóa không thể hoạt động. Vui lòng kiểm tra cấu hình Module_DanduongGPS.");
            }
            byte[] keyBytes = VanLyDocHanhTaoKey256(rawKeyFromGPS);
            byte[] ivBytes = BaoCatSaharaTaoIV(rawIVFromGPS);
            try
            {
                Aes aes = Aes.Create();
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                return aes;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(keyBytes);
                CryptographicOperations.ZeroMemory(ivBytes);
            }
        }
        catch (InvalidOperationException invOpEx)
        {
            Debug.WriteLine($"[Lỗi Bảo Mật Nghiêm Trọng] {invOpEx.Message}");
            throw;   // thiếu khóa: không trả mặc định, để nơi gọi biết (ThreadLocal KHÔNG lưu lại lỗi, lần sau thử lại)
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Lỗi] Khởi tạo AES thất bại: {ex.Message}");
            throw new CryptographicException("Hệ thống mã hóa không thể khởi tạo. Ứng dụng không thể hoạt động an toàn.", ex);
        }
    }, trackAllValues: true);
    // ===================== 2. "ĐỘNG CƠ AES" CHO MaHoa / GiaiMa (V2) =====================
    //  Mỗi luồng tạo 1 lần bộ AES-ECB (không đệm) rồi tự ghép CBC bằng XOR.
    //  Aes.EncryptCbc/DecryptCbc (one-shot) phải dựng lại khóa + cấp phát handle CNG ở MỖI lần gọi;
    //  bộ ECB tái sử dụng chỉ gọi BCryptEncrypt/Decrypt. IV vẫn ngẫu nhiên (CSPRNG), sinh theo lô 64 IV/lần.
    //  Tự rơi về đường cũ (one-shot) nếu động cơ gặp lỗi mật mã (cờ _dongCoHong) => không bao giờ hỏng dữ liệu.
    private static volatile bool _dongCoHong;   // true => dùng đường one-shot
    private static volatile bool _tatDongCo;    // chỉ để đo so sánh trong DoTocDo / KiemTraTuongThichMaHoa
    private const int NGUONG_DONG_CO_MA_HOA = 79; // <= 5 khối AES => động cơ nhanh hơn one-shot
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
            Array.Clear(_ivPool, 0, _ivPool.Length);
        }
    }
    // Factory KHÔNG nuốt lỗi: nếu khóa chưa nạp, ngoại lệ nổi lên, ThreadLocal không lưu giá trị hỏng
    // và LayDongCo() chỉ trả null cho lần đó (lần sau thử lại). Trước đây lỗi này bật _dongCoHong vĩnh viễn.
    private static readonly ThreadLocal<DongCoAes> _dongCo =
        new(() => new DongCoAes(_aesLocal.Value!), trackAllValues: true);
    private static DongCoAes? LayDongCo()
    {
        if (_dongCoHong || _tatDongCo) return null;
        try { return _dongCo.Value; }
        catch (InvalidOperationException) { return null; }   // gồm cả ObjectDisposedException và "khóa chưa nạp": KHÔNG khóa cứng
        catch (CryptographicException ex)
        {
            _dongCoHong = true;                               // lỗi mật mã thật sự => dùng đường one-shot
            Debug.WriteLine("Lỗi khởi tạo động cơ AES: " + ex.Message);
            return null;
        }
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
                string t = MaHoa("khoi-dong");                 // v5 (AES-GCM) khi bật
                _ = GiaiMa(t);
                _giaiMaCache.TryRemove(t, out _);
                string t2 = MaHoaV2("khoi-dong");              // v2: vẫn phải nóng vì CSDL cũ toàn là v2
                _ = GiaiMa(t2);
                _giaiMaCache.TryRemove(t2, out _);
                _ = GiaiMa_NhatKy(MaHoa_NhatKy("khoi-dong"));   // làm nóng cả đường AES-GCM của nhật ký
            }
        }
        catch (Exception ex) { Debug.WriteLine("KhoiDongNong: " + ex.Message); }
    }
    // ===================== 3. MaHoa / GiaiMa =====================
    /// <summary>
    /// Mã hóa 1 ô dữ liệu. Mặc định ghi v5 (AES-256-GCM có xác thực); GhiDinhDangV5 = false => ghi v2 như cũ.
    /// Lỗi bất ngờ ở đường v5 (không phải do thiếu khóa) => tự rơi về v2, không bao giờ mất dữ liệu.
    /// </summary>
    public static string MaHoa(string chuoiGoc)
    {
        if (string.IsNullOrEmpty(chuoiGoc)) return string.Empty;
        if (GhiDinhDangV5 && !_v5Hong)
        {
            try { return MaHoaV5(chuoiGoc); }
            catch (InvalidOperationException) { return string.Empty; }   // khóa chưa nạp: v2 cũng không chạy được
            catch (Exception ex)
            {
                _v5Hong = true;                                            // lỗi nền tảng (hiếm) => dùng v2 cả phiên
                Debug.WriteLine("AES-GCM v5 lỗi, chuyển về v2: " + ex.Message);
            }
        }
        return MaHoaV2(chuoiGoc);
    }
    /// <summary>
    /// Mã hóa định dạng cũ v2 (AES-CBC). Chỉ dùng khi bắt buộc cho phần mềm bản cũ đọc được.
    /// </summary>
    public static string MaHoaV2(string chuoiGoc)
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
                        _dongCoHong = true;   // rơi về đường one-shot, không mất dữ liệu
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
    // Đường one-shot: 1 lần EncryptCbc trên vùng stackalloc. Dùng cho chuỗi dài / khi động cơ hỏng.
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
            return DongGoiV2(payload.Slice(0, 16 + cipherLen));
        }
        finally
        {
            if (rented != null) ArrayPool<byte>.Shared.Return(rented, clearArray: true);
        }
    }
    // "v2|" + Base64(payload)
    private static string DongGoiV2(ReadOnlySpan<byte> payload) => DongGoi(PREFIX_V2, payload);
    // <tiền tố> + Base64(payload), dựng chuỗi trên stack (chuỗi ngắn) => đúng 1 lần cấp phát (chính chuỗi kết quả)
    private static string DongGoi(string prefix, ReadOnlySpan<byte> payload)
    {
        int prefixLen = prefix.Length;
        int totalChars = prefixLen + ((payload.Length + 2) / 3) * 4;
        Span<char> chars = totalChars <= 512 ? stackalloc char[512] : new char[totalChars];
        prefix.AsSpan().CopyTo(chars);
        if (!Convert.TryToBase64Chars(payload, chars.Slice(prefixLen), out int charsWritten))
            return string.Empty;
        return new string(chars.Slice(0, prefixLen + charsWritten));
    }
    // GiaiMa: cache -> (động cơ ECB | đường one-shot). Chuỗi không hợp lệ => chuỗi rỗng, KHÔNG cache, KHÔNG ném ngoại lệ.
    public static string GiaiMa(string? chuoiMaHoa)
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
                DonNheCache(_giaiMaCache, CACHE_DON_MOI_LAN);
            _giaiMaCache.TryAdd(chuoiMaHoa, kq);
        }
        return kq;
    }
    /// <summary>
    /// Giải mã nhưng KHÔNG lưu vào bộ đệm (vẫn đọc bộ đệm nếu có sẵn). Dùng khi quét 1 lượt qua bảng lớn mà mỗi chuỗi
    /// chỉ đọc đúng 1 lần (khen thưởng csdl4, nhật ký, xuất báo cáo...) để không đẩy dữ liệu DanhSach ra khỏi bộ đệm.
    /// </summary>
    public static string GiaiMaKhongLuuDem(string chuoiMaHoa)
    {
        if (string.IsNullOrEmpty(chuoiMaHoa)) return string.Empty;
        if (_giaiMaCache.TryGetValue(chuoiMaHoa, out string? cached)) return cached!;
        try { return GiaiMaThat(chuoiMaHoa) ?? string.Empty; }
        catch { return string.Empty; }
    }
    /// <summary>
    /// true nếu chuỗi mang tiền tố của bản mã dữ liệu ô ("v2|" hoặc "v5|"). Dùng thay cho StartsWith("v2|") ở nơi gọi:
    /// giải mã thất bại mà chuỗi là bản mã => KHÔNG được coi chính bản mã là văn bản thường / mã hóa chồng lần nữa.
    /// </summary>
    public static bool LaChuoiMa(string? s) =>
        !string.IsNullOrEmpty(s) &&
        (s.StartsWith(PREFIX_V5, StringComparison.Ordinal) || s.StartsWith(PREFIX_V2, StringComparison.Ordinal));
    private static string? GiaiMaThat(string s)
    {
        if (s.StartsWith(PREFIX_V5, StringComparison.Ordinal)) return GiaiMaV5(s.AsSpan(PREFIX_V5.Length));
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
                    _dongCoHong = true;                    // rơi về đường one-shot
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
    // Đường one-shot: dùng cho chuỗi rất dài / khi động cơ hỏng. Hợp lệ => chuỗi; không hợp lệ => null.
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
    // ===================== 3b. V5 — AES-256-GCM CHO DỮ LIỆU Ô =====================
    //  Định dạng: "v5|" + Base64(Nonce 12 | Cipher n | Tag 16). Bản mã dài đúng bằng bản rõ (không đệm).
    //  Khóa: HMAC-SHA256(khóa hệ thống, NHAN_KHOA_V5) — suy ra tất định từ tệp khóa hiện có => máy nào có tệp khóa
    //        cũng đọc được, KHÔNG phải đổi tệp khóa, không ảnh hưởng ServiceBackup/ServiceRestore.
    //  Nonce: 96 bit ngẫu nhiên (CSPRNG, sinh theo lô 64 nonce/lần). Giới hạn an toàn của NIST cho nonce ngẫu nhiên là
    //         2^32 lần mã hóa / khóa — dư hàng trăm nghìn lần so với nhu cầu (6.000 người x 10 cột x nhiều năm).
    //  AAD  : nhãn định dạng cố định => bản mã v5 không thể "đội lốt" bản mã GCM của nhật ký hay ngược lại.
    private const string NHAN_KHOA_V5 = "PMTD2026|V5|AES-256-GCM|DU-LIEU-O";
    private static readonly byte[] AAD_V5 = Encoding.ASCII.GetBytes("PMTD|v5");
    private const int V5_NONCE = 12, V5_TAG = 16, V5_PHU = V5_NONCE + V5_TAG;
    private static volatile bool _v5Hong;   // true => AES-GCM không dùng được trên máy này, MaHoa ghi v2
    private sealed class DongCoV5 : IDisposable
    {
        private const int NONCE_POOL_BYTES = V5_NONCE * 64;
        public readonly AesGcm Gcm;
        private readonly byte[] _pool = new byte[NONCE_POOL_BYTES];
        private int _pos = NONCE_POOL_BYTES;
        public DongCoV5()
        {
            byte[] k = TaoKhoaPhu(NHAN_KHOA_V5);   // ném InvalidOperationException nếu khóa hệ thống chưa nạp
            try { Gcm = new AesGcm(k, V5_TAG); }
            finally { CryptographicOperations.ZeroMemory(k); }
        }
        public void LayNonce(Span<byte> dich)
        {
            if (_pos >= NONCE_POOL_BYTES) { RandomNumberGenerator.Fill(_pool); _pos = 0; }
            Span<byte> o = _pool.AsSpan(_pos, V5_NONCE);
            o.CopyTo(dich);
            o.Clear();                              // nonce đã cấp phát không bao giờ được dùng lại
            _pos += V5_NONCE;
        }
        public void Dispose()
        {
            Gcm.Dispose();
            Array.Clear(_pool, 0, _pool.Length);
        }
    }
    // Factory không nuốt lỗi: thiếu khóa => ngoại lệ, ThreadLocal không lưu giá trị hỏng, lần sau thử lại.
    private static readonly ThreadLocal<DongCoV5> _dongCoV5 = new(() => new DongCoV5(), trackAllValues: true);
    private static string MaHoaV5(string chuoiGoc)
    {
        DongCoV5 eng = _dongCoV5.Value!;
        int n = Encoding.UTF8.GetByteCount(chuoiGoc);
        int payloadLen = V5_PHU + n;
        int need = payloadLen + n;                       // [nonce][cipher][tag] + [bản rõ]
        byte[]? rented = null;
        Span<byte> buf = need <= STACK_BYTES
            ? stackalloc byte[STACK_BYTES]
            : (rented = ArrayPool<byte>.Shared.Rent(need));
        try
        {
            Span<byte> payload = buf.Slice(0, payloadLen);
            Span<byte> plain = buf.Slice(payloadLen, n);
            Encoding.UTF8.GetBytes(chuoiGoc.AsSpan(), plain);
            eng.LayNonce(payload.Slice(0, V5_NONCE));
            eng.Gcm.Encrypt(payload.Slice(0, V5_NONCE), plain, payload.Slice(V5_NONCE, n),
                            payload.Slice(V5_NONCE + n, V5_TAG), AAD_V5);
            plain.Clear();
            return DongGoi(PREFIX_V5, payload);
        }
        finally
        {
            if (rented != null) ArrayPool<byte>.Shared.Return(rented, clearArray: true);
            else buf.Slice(0, Math.Min(need, STACK_BYTES)).Clear();
        }
    }
    // Hợp lệ => bản rõ (đã chuẩn hóa Trim giống v2); sai Tag / hỏng / thiếu khóa => null.
    private static string? GiaiMaV5(ReadOnlySpan<char> b64)
    {
        DongCoV5 eng;
        try { eng = _dongCoV5.Value!; }
        catch { return null; }
        int maxBytes = (b64.Length / 4 + 1) * 3;
        int need = maxBytes * 2;                         // [dữ liệu thô][bản rõ]
        byte[]? rented = null;
        Span<byte> buf = need <= STACK_BYTES
            ? stackalloc byte[STACK_BYTES]
            : (rented = ArrayPool<byte>.Shared.Rent(need));
        int cLen = 0;
        try
        {
            Span<byte> raw = buf.Slice(0, maxBytes);
            if (!Convert.TryFromBase64Chars(b64, raw, out int rawLen) || rawLen <= V5_PHU) return null;
            cLen = rawLen - V5_PHU;
            Span<byte> plain = buf.Slice(maxBytes, cLen);
            try
            {
                eng.Gcm.Decrypt(raw.Slice(0, V5_NONCE), raw.Slice(V5_NONCE, cLen),
                                raw.Slice(V5_NONCE + cLen, V5_TAG), plain, AAD_V5);
            }
            catch (CryptographicException) { return null; }   // bị sửa / sai khóa: KHÔNG trả dữ liệu méo
            return Encoding.UTF8.GetString(plain).Trim('\0').Trim();
        }
        finally
        {
            if (rented != null) ArrayPool<byte>.Shared.Return(rented, clearArray: true);
            else buf.Slice(0, Math.Min(need, STACK_BYTES)).Clear();
        }
    }
    // ===================== 5. MÃ HÓA XÁC ĐỊNH (TÙY CHỌN) CHO CỘT PHÂN LOẠI ÍT GIÁ TRỊ =====================
    //  Cùng bản rõ => cùng chuỗi mã (IV = 16 byte đầu của HMAC-SHA256(khóa phụ, bản rõ) thay cho IV ngẫu nhiên).
    //  Vẫn là "v2|" + Base64(IV + Cipher) nên GiaiMa()/GiaiMa_Goc() giải được y như cũ.
    //  LỢI ÍCH: ĐƠN VỊ / CẤP BẬC / CHỨC VỤ / PHÂN LOẠI chỉ có vài chục giá trị => 5.000 dòng chỉ còn vài chục chuỗi mã
    //           khác nhau => bộ đệm trúng ~100% cho các cột này; lọc/nhóm được ngay trong SQL (WHERE DonVi = @mã).
    //  ĐÁNH ĐỔI: ai có tệp .db sẽ thấy những dòng nào CÙNG giá trị (không đọc được nội dung).
    //           => CHỈ dùng cho cột phân loại ít giá trị. KHÔNG dùng cho Họ tên, Số hiệu, Quê quán, Ghi chú.
    //  LƯU Ý  : dữ liệu CŨ vẫn là IV ngẫu nhiên; muốn lọc bằng SQL phải chuyển đổi cột 1 lượt (trong transaction, có sao lưu).
    private static readonly Lazy<byte[]> _macKeyCoDinh = new(() =>
    {
        byte[] k = _aesLocal.Value!.Key;
        try { return HMACSHA256.HashData(k, Encoding.ASCII.GetBytes("PMTD2026|IV-CO-DINH")); }
        finally { CryptographicOperations.ZeroMemory(k); }
    }, LazyThreadSafetyMode.PublicationOnly);   // PublicationOnly: lỗi thiếu khóa không bị lưu lại vĩnh viễn
    /// <summary>
    /// Khóa phụ 32 byte theo mục đích = HMAC-SHA256(khóa hệ thống, nhãn). Nhãn khác nhau => khóa độc lập nhau.
    /// Nơi gọi PHẢI tự xóa mảng trả về (CryptographicOperations.ZeroMemory). Ném lỗi nếu khóa hệ thống chưa nạp.
    /// Dùng bởi Module_TokenCSDL (chữ ký toàn vẹn bộ CSDL).
    /// </summary>
    public static byte[] TaoKhoaPhu(string nhan)
    {
        byte[] k = _aesLocal.Value!.Key;
        try { return HMACSHA256.HashData(k, Encoding.UTF8.GetBytes(nhan)); }
        finally { CryptographicOperations.ZeroMemory(k); }
    }
    public static string XuanVeOHokkaido(string b64)
    {
        if (string.IsNullOrEmpty(b64)) return string.Empty;
        try { return Encoding.UTF8.GetString(Convert.FromBase64String(b64)); } catch { return string.Empty; }
    }
    private static byte[] VanLyDocHanhTaoKey256(string key) => SHA256.HashData(Encoding.UTF8.GetBytes(key));
    private static byte[] BaoCatSaharaTaoIV(string iv) => MD5.HashData(Encoding.UTF8.GetBytes(iv));
    // Duyệt thẳng enumerator của ConcurrentDictionary (không khóa, không sao chép toàn bộ khóa).
    private static void DonNheCache(ConcurrentDictionary<string, string> cache, int soLuong)
    {
        int count = 0;
        foreach (var kv in cache)
        {
            if (cache.TryRemove(kv.Key, out _)) count++;
            if (count >= soLuong) break;
        }
    }
    public static string TraLaiTenChoMeoCam(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return string.Create(s.Length, s, (span, str) =>
        {
            for (int i = 0, j = str.Length - 1; i < str.Length; i++, j--) span[i] = str[j];
        });
    }
    // Giải phóng tài nguyên mật mã khi thoát chương trình (gọi nhiều lần vẫn an toàn).
    private static int _daDonDep;
    public static void DonDeprRacTraLaiTaiNguyenHeThong()
    {
        if (Interlocked.Exchange(ref _daDonDep, 1) != 0) return;
        _giaiMaCache.Clear();
        try { foreach (var d in _dongCo.Values) d?.Dispose(); _dongCo.Dispose(); } catch { }
        try { foreach (var v in _dongCoV5.Values) v?.Dispose(); _dongCoV5.Dispose(); } catch { }
        try { foreach (var g in _gcmLocal.Values) g?.Dispose(); _gcmLocal.Dispose(); } catch { }
        try { foreach (var a in _aesLocal.Values) a?.Dispose(); _aesLocal.Dispose(); } catch { }
        if (_macKeyCoDinh.IsValueCreated) CryptographicOperations.ZeroMemory(_macKeyCoDinh.Value);
    }
    // ===================== 7. TIỆN ÍCH TẠO KHÓA 256-BIT (Module_KhoiTaoCSDL còn dùng) =====================
    // Đã GỠ khỏi đây: yeuMeoCam_v1/yeuChuMeoCam_v1, MotDoiNguoiMotRungCayv1, TruyenThuyetGhepManhVoKhoa, DuongVaoTraiTimEm
    // (cơ chế tệp khóa ẩn ".uninstall.key" cũ). Việc gỡ cài đặt nay đi qua Module_GoCaiDat (bàn giao qua stdin).
    public static byte[] HoaVanNoTrenDuongRaChienDich256v1(string key) => SHA256.HashData(Encoding.UTF8.GetBytes(key));
    // ===================== 8. V3 — MÃ HÓA GIA CỐ (BAO_CA0_TONG_HOP) + GIẢI MÃ NHẬN DẠNG ĐA PHIÊN BẢN =====================
    //  Dùng cho tệp EXCEL BAO_CA0_TONG_HOP chứa dữ liệu nhạy cảm: Salt + IV + Ciphertext + HMAC (kiểm tra toàn vẹn
    //  TRƯỚC khi giải mã, chống giả mạo/sửa đổi). PBKDF2 2.000 vòng là một phần của định dạng => KHÔNG đổi.
    private static string? _cachedSig = null;
    private const string PREFIX_STEALTH_V3 = "v3|"; // Phiên bản dành cho việc bảo vệ dữ liệu tệp excel ở B1 SHEET BAO_CA0_TONG_HOP
    // 1. LẤY CHỮ KÝ HỆ THỐNG TỪ CSDL 1 (KEY_1 + KEY_3)
    private static string LaySignatureHeThong()
    {
        if (!string.IsNullOrEmpty(_cachedSig)) return _cachedSig;
        try
        {
            string path = Module_DanduongGPS.DuongDanCSDL1;
            using var conn = new SqliteConnection($"Data Source={path};Pooling=True;");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Key_1, Key_3 FROM Key_KhoiDong WHERE ID = 1";
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                _cachedSig = (reader["Key_1"]?.ToString() ?? "") + (reader["Key_3"]?.ToString() ?? "");
            }
        }
        catch
        {
            // KHÔNG lưu giá trị mặc định vào bộ đệm: lỗi tạm thời (CSDL đang khôi phục) không được khóa cứng cả phiên
            return "SIG_MAC_DINH_2026_MEO_CAM";
        }
        return _cachedSig ?? string.Empty;
    }
    // HÀM TẠO KEY DÙNG CHUNG VỚI MODULE AES (PBKDF2-HMAC-SHA256, 2.000 vòng, 32 byte)
    // Thiếu khóa hệ thống => NÉM LỖI (trước đây âm thầm mã hóa bằng khóa cứng ai cũng biết => tệp Excel coi như không mã hóa).
    private static byte[] TaoKeyBaoMatV3(byte[] salt)
    {
        byte[] baseKey = _aesLocal.Value!.Key;   // KEY GỐC đang chạy của hệ thống
        try { return Rfc2898DeriveBytes.Pbkdf2(baseKey, salt, 2000, HashAlgorithmName.SHA256, 32); }
        finally { CryptographicOperations.ZeroMemory(baseKey); }
    }
    // CHỈ ĐỂ ĐỌC tệp Excel cũ lỡ được tạo bằng khóa cứng khi khóa hệ thống chưa nạp. Không bao giờ dùng để mã hóa.
    private static byte[] KhoaV3CuDeDoc() => SHA256.HashData(Encoding.UTF8.GetBytes("PMTD2026_FALLBACK_KEY"));
    // Kiểm HMAC rồi giải mã V3 bằng 1 khóa; sai HMAC => null.
    private static string? GiaiMaV3VoiKhoa(byte[] all, byte[] key)
    {
        int payloadLen = all.Length - 32;
        Span<byte> computed = stackalloc byte[32];
        HMACSHA256.HashData(key, all.AsSpan(0, payloadLen), computed);
        if (!CryptographicOperations.FixedTimeEquals(computed, all.AsSpan(payloadLen, 32))) return null;
        using Aes aes = Aes.Create();
        aes.Key = key;
        byte[] decrypted = aes.DecryptCbc(all.AsSpan(32, payloadLen - 32), all.AsSpan(16, 16), PaddingMode.PKCS7);
        try { return Encoding.UTF8.GetString(decrypted).Trim('\0').Trim(); }
        finally { CryptographicOperations.ZeroMemory(decrypted); }
    }
    // MÃ HÓA GIA CỐ V3
    public static string MaHoaGiaCo(string dataGoc)
    {
        if (string.IsNullOrWhiteSpace(dataGoc)) return string.Empty;
        byte[]? key = null;
        try
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] iv = RandomNumberGenerator.GetBytes(16);
            key = TaoKeyBaoMatV3(salt);
            byte[] plain = Encoding.UTF8.GetBytes(dataGoc);
            int cipherLen = ((plain.Length / 16) + 1) * 16;
            // FINAL = SALT(16) + IV(16) + CIPHER + HMAC(32)
            byte[] data = new byte[32 + cipherLen + 32];
            salt.CopyTo(data, 0);
            iv.CopyTo(data, 16);
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.EncryptCbc(plain, iv, data.AsSpan(32, cipherLen), PaddingMode.PKCS7);
            }
            HMACSHA256.HashData(key, data.AsSpan(0, 32 + cipherLen), data.AsSpan(32 + cipherLen, 32));
            return PREFIX_STEALTH_V3 + Convert.ToBase64String(data);
        }
        catch
        {
            return string.Empty;
        }
        finally
        {
            if (key != null) CryptographicOperations.ZeroMemory(key);
        }
    }
    // GIẢI MÃ NHẬN DẠNG (V3 -> V2 -> hệ cũ Stealth -> nguyên văn)
    public static string GiaiMaNhanDang(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        byte[]? key = null;
        try
        {
            // V3
            if (input.StartsWith(PREFIX_STEALTH_V3, StringComparison.Ordinal))
            {
                byte[] all = Convert.FromBase64String(input.Substring(PREFIX_STEALTH_V3.Length));
                // Salt16 + IV16 + ít nhất 1 khối AES + HMAC32, phần mã phải là bội số 16
                if (all.Length < 80 || ((all.Length - 64) & 15) != 0) return string.Empty;
                // VERIFY HMAC (so sánh thời gian cố định) TRƯỚC khi giải mã: khóa hệ thống trước, khóa cứng cũ sau
                string? kq = null;
                try
                {
                    key = TaoKeyBaoMatV3(all.AsSpan(0, 16).ToArray());
                    kq = GiaiMaV3VoiKhoa(all, key);
                }
                catch (InvalidOperationException) { }            // khóa hệ thống chưa nạp
                if (kq == null)
                {
                    if (key != null) CryptographicOperations.ZeroMemory(key);
                    key = KhoaV3CuDeDoc();
                    kq = GiaiMaV3VoiKhoa(all, key);
                }
                return kq ?? string.Empty;
            }
            // TƯƠNG THÍCH NGƯỢC V2 / V5 (Excel có thể chứa ô mã hóa dữ liệu thường)
            if (LaChuoiMa(input))
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
                int padding = 4 - (base64.Length % 4);
                if (padding < 4)
                {
                    base64 = base64.PadRight(base64.Length + padding, '=');
                }
                byte[] bytes = Convert.FromBase64String(base64);
                string decoded = Encoding.UTF8.GetString(bytes);
                if (decoded.Contains(sig))
                {
                    int index = decoded.IndexOf(sig);
                    return decoded.Substring(index + sig.Length);
                }
            }
            catch { }
            return input;
        }
        catch
        {
            return string.Empty;
        }
        finally
        {
            if (key != null) CryptographicOperations.ZeroMemory(key);
        }
    }
    // ===================== 9. MÃ HÓA / GIẢI MÃ TỆP CSDL (AES-GCM THEO KHỐI 64KB) =====================
    //  Mã hóa/giải mã các tệp csdl1.db, csdl2.db, csdl3.db, csdl4.db, csdlex.xlsx (toàn vẹn + bảo mật dữ liệu người dùng).
    //  Định dạng giữ nguyên:  "PMTD"(4) + 0x2026(2) + BaseNonce(12) + TổngKhối(8) + n x [Tag(16) + Cipher(<=64KB)]
    private static readonly byte[] MAGIC_BYTES = Encoding.ASCII.GetBytes("PMTD");
    private const ushort FILE_FORMAT_VERSION_STREAM = 0x2026;
    private const int GCM_NONCE_SIZE = 12;
    private const int GCM_TAG_SIZE = 16;
    private const int CHUNK_DATA_SIZE = 65536; // Khối Plaintext cố định 64KB
    private const int HEADER_SIZE = 26;        // 4 + 2 + 12 + 8
    // Đọc đủ <count> byte (Read có thể trả ít hơn dù chưa hết tệp); trả số byte thực đọc được.
    private static int DocDu(Stream s, byte[] buf, int count)
    {
        int tong = 0;
        while (tong < count)
        {
            int r = s.Read(buf, tong, count - tong);
            if (r <= 0) break;
            tong += r;
        }
        return tong;
    }
    /// <summary>
    /// 🔐 MÃ HÓA CSDL (STREAMING CHUNKED AES-GCM). RAM cố định ~130KB, tệp dung lượng bất kỳ.
    /// Mỗi khối ghi ra bằng 1 lệnh duy nhất [Tag][Cipher]. Nguồn bị thu nhỏ giữa chừng => IOException (không tạo tệp hỏng).
    /// </summary>
    public static void MaHoaCSDL(string sourceFile, string destFile, byte[] key)
    {
        if (key == null || key.Length != 32)
        {
            throw new CryptographicException("Rủi ro hệ thống: Khóa mật mã đầu vào không chuẩn độ dài AES-256 (32 bytes).");
        }
        byte[] baseNonce = RandomNumberGenerator.GetBytes(GCM_NONCE_SIZE);
        byte[] plain = ArrayPool<byte>.Shared.Rent(CHUNK_DATA_SIZE);
        byte[] outBuf = ArrayPool<byte>.Shared.Rent(GCM_TAG_SIZE + CHUNK_DATA_SIZE);
        try
        {
            // bufferSize = 1: tự đọc/ghi theo khối 64KB nên tắt bộ đệm của FileStream (khỏi chép 2 lần)
            using var fsIn = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1, FileOptions.SequentialScan);
            using var fsOut = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None, 1, FileOptions.None);
            long fileLength = fsIn.Length;
            long totalChunks = (fileLength + CHUNK_DATA_SIZE - 1) / CHUNK_DATA_SIZE;
            // 1. Header (26 byte)
            Span<byte> header = stackalloc byte[HEADER_SIZE];
            MAGIC_BYTES.CopyTo(header);
            BinaryPrimitives.WriteUInt16LittleEndian(header.Slice(4, 2), FILE_FORMAT_VERSION_STREAM);
            baseNonce.CopyTo(header.Slice(6, GCM_NONCE_SIZE));
            BinaryPrimitives.WriteInt64LittleEndian(header.Slice(18, 8), totalChunks);
            fsOut.Write(header);
            // 2. Các khối
            Span<byte> nonce = stackalloc byte[GCM_NONCE_SIZE];
            using var gcm = new AesGcm(key, GCM_TAG_SIZE);
            long conLai = fileLength;
            for (long chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
            {
                int want = (int)Math.Min(CHUNK_DATA_SIZE, conLai);
                int got = DocDu(fsIn, plain, want);
                if (got != want)
                    throw new IOException("Tệp nguồn bị thu nhỏ trong lúc mã hóa (đang được ghi bởi tiến trình khác?). Đã hủy để không tạo tệp hỏng.");
                conLai -= got;
                XayDungChunkNonce(baseNonce, chunkIndex, nonce);
                gcm.Encrypt(nonce, plain.AsSpan(0, got), outBuf.AsSpan(GCM_TAG_SIZE, got), outBuf.AsSpan(0, GCM_TAG_SIZE));
                fsOut.Write(outBuf, 0, GCM_TAG_SIZE + got);   // [TAG (16B)] [CIPHERTEXT (N Bytes)]
            }
            // Ép xả bộ đệm đĩa cứng ngay lập tức nhằm chống mất dữ liệu khi mất điện đột ngột
            fsOut.Flush(true);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(plain, clearArray: true);
            ArrayPool<byte>.Shared.Return(outBuf, clearArray: true);
        }
    }
    /// <summary>
    /// 🔓 GIẢI MÃ CSDL (STREAMING CHUNKED AES-GCM). Kiểm tra toàn vẹn thời gian thực trên từng khối.
    /// Kiểm tra thêm: Header phải khớp dung lượng tệp (phát hiện cắt cụt / sửa tổng số khối / dữ liệu thừa).
    /// </summary>
    public static void GiaiMaCSDL(string sourceFile, string destFile, byte[] key)
    {
        if (key == null || key.Length != 32)
        {
            throw new CryptographicException("Rủi ro hệ thống: Khóa giải mã đầu vào không đạt chuẩn AES-256.");
        }
        byte[] inBuf = ArrayPool<byte>.Shared.Rent(GCM_TAG_SIZE + CHUNK_DATA_SIZE);
        byte[] plain = ArrayPool<byte>.Shared.Rent(CHUNK_DATA_SIZE);
        try
        {
            using var fsIn = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1, FileOptions.SequentialScan);
            // Thẩm định kích thước tối thiểu của Header hệ thống (26 Bytes)
            if (fsIn.Length < HEADER_SIZE)
            {
                throw new CryptographicException("Giải mã thất bại: Tệp cấu hình nhị phân hỏng cấu trúc Header.");
            }
            byte[] header = new byte[HEADER_SIZE];
            if (DocDu(fsIn, header, HEADER_SIZE) != HEADER_SIZE)
            {
                throw new CryptographicException("Lỗi cấu trúc: File bị cắt cụt dữ liệu tại phân đoạn tham số bảo mật.");
            }
            // 1. Magic Bytes chống nhận diện sai định dạng
            if (!CryptographicOperations.FixedTimeEquals(header.AsSpan(0, 4), MAGIC_BYTES))
            {
                throw new CryptographicException("Xác thực thất bại: Tệp tin không phải định dạng bảo mật của hệ thống.");
            }
            // 2. Version cấu trúc
            ushort version = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(4, 2));
            if (version != FILE_FORMAT_VERSION_STREAM)
            {
                throw new CryptographicException($"Xung đột phân hệ: Phiên bản tệp (0x{version:X4}) không tương thích bộ giải mã v2026.");
            }
            // 3. Base Nonce và tổng số khối
            byte[] baseNonce = header.AsSpan(6, GCM_NONCE_SIZE).ToArray();
            long totalChunks = BinaryPrimitives.ReadInt64LittleEndian(header.AsSpan(18, 8));
            long conLai = fsIn.Length - HEADER_SIZE;
            // 4. Header phải khớp dung lượng: mỗi khối = Tag 16B + 1..64KB dữ liệu (header không được xác thực nên phải tự kiểm tra)
            if (totalChunks < 0 || totalChunks > conLai / GCM_TAG_SIZE || conLai > totalChunks * (GCM_TAG_SIZE + CHUNK_DATA_SIZE))
            {
                throw new CryptographicException("Lỗi cấu trúc: Tổng số khối trong Header không khớp dung lượng tệp (tệp bị cắt cụt hoặc bị sửa đổi).");
            }
            using var fsOut = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None, 1, FileOptions.None);
            using var gcm = new AesGcm(key, GCM_TAG_SIZE);
            Span<byte> nonce = stackalloc byte[GCM_NONCE_SIZE];
            for (long chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
            {
                // Khối cuối có thể nhỏ hơn 64KB: lấy đúng phần còn lại; các khối trước luôn đầy 16 + 64KB
                long take = (chunkIndex == totalChunks - 1) ? conLai : GCM_TAG_SIZE + CHUNK_DATA_SIZE;
                if (take <= GCM_TAG_SIZE || take > GCM_TAG_SIZE + CHUNK_DATA_SIZE || take > conLai)
                {
                    throw new CryptographicException($"Lỗi cấu trúc: Khối thứ {chunkIndex} bị hỏng hoặc mất thẻ Tag xác thực.");
                }
                int takeI = (int)take;
                int got = DocDu(fsIn, inBuf, takeI);
                if (got != takeI)
                {
                    throw new CryptographicException($"Lỗi phân hệ đĩa: Đọc thiếu byte tại khối thứ {chunkIndex} (Yêu cầu: {takeI}B, Thực tế: {got}B).");
                }
                conLai -= takeI;
                int n = takeI - GCM_TAG_SIZE;
                XayDungChunkNonce(baseNonce, chunkIndex, nonce);
                try
                {
                    // Giải mã kết hợp xác thực thời gian thực (Real-time Tag Validation)
                    gcm.Decrypt(nonce, inBuf.AsSpan(GCM_TAG_SIZE, n), inBuf.AsSpan(0, GCM_TAG_SIZE), plain.AsSpan(0, n));
                }
                catch (CryptographicException ex)
                {
                    throw new CryptographicException($"[BÁO ĐỘNG CHÍ MẠNG]: Dữ liệu tại khối thứ {chunkIndex} đã bị sửa đổi trái phép (Authentication Tag Mismatch). Tiến trình giải mã bị chặn đứng!", ex);
                }
                fsOut.Write(plain, 0, n);   // Ghi khối sạch ra đĩa vận hành
            }
            if (conLai != 0)
            {
                throw new CryptographicException("Lỗi cấu trúc: Tệp còn dữ liệu thừa sau khối cuối cùng (tệp bị sửa đổi).");
            }
            fsOut.Flush(true);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(inBuf, clearArray: true);
            ArrayPool<byte>.Shared.Return(plain, clearArray: true);
        }
    }
    /// <summary>
    /// 🧮 Sinh Nonce biến đổi động: XOR 8 byte (little-endian) của chỉ mục khối vào 8 byte cuối của Base Nonce.
    /// </summary>
    private static void XayDungChunkNonce(ReadOnlySpan<byte> baseNonce, long chunkIndex, Span<byte> outNonce)
    {
        baseNonce.Slice(0, GCM_NONCE_SIZE).CopyTo(outNonce);
        for (int i = 0; i < 8; i++)
        {
            outNonce[GCM_NONCE_SIZE - 1 - i] ^= (byte)(chunkIndex >> (8 * i));
        }
    }
    public static byte[] LayKhoaHeThongDungChung()
    {
        // Tận dụng cơ chế sinh khóa 256-bit chuẩn đang chạy ngầm của hệ thống
        string k1 = Module_DanduongGPS.ToiYeuMeoCam1;
        string k2 = Module_DanduongGPS.ToiYeuMeoCam2;
        string rawKeyFromGPS = TraLaiTenChoMeoCam(k1 + k2);
        string keyFinal;
        if (!string.IsNullOrEmpty(rawKeyFromGPS)) keyFinal = rawKeyFromGPS;
        else
        {
            // Khóa dự phòng (giữ nguyên hành vi cũ để không làm hỏng dữ liệu đã mã hóa bằng nó)
            string mKpassKey = Encoding.UTF8.GetString(Convert.FromBase64String("eWVLZGV4aUZfNjIwMmF1RGlpaFRtZU1uYWhQ"));
            keyFinal = TraLaiTenChoMeoCam(mKpassKey);
        }
        return VanLyDocHanhTaoKey256(keyFinal);
    }
    // ===================== 10. MÃ HÓA RIÊNG CHO NHẬT KÝ — V4 (AES-GCM) =====================
    public const string PREFIX_STEALTH_V2 = "AES:v2|";
    public const string PREFIX_STEALTH_V4 = "AES:v4|";
    // Mỗi luồng giữ 1 AesGcm dùng lại (trước đây: tạo mới + sao chép khóa ở MỖI ô; nạp nhật ký = 5 cột x số dòng).
    private static readonly ThreadLocal<AesGcm> _gcmLocal = new(() =>
    {
        byte[] k = _aesLocal.Value!.Key;
        try { return new AesGcm(k, GCM_TAG_SIZE); }
        finally { CryptographicOperations.ZeroMemory(k); }
    }, trackAllValues: true);
    public static string MaHoa_NhatKy(string chuoiGoc)
    {
        if (string.IsNullOrEmpty(chuoiGoc)) return string.Empty;
        byte[]? rented = null;
        try
        {
            const int nonceLen = 12, tagLen = 16;
            int n = Encoding.UTF8.GetByteCount(chuoiGoc);
            int payloadLen = nonceLen + n + tagLen;
            int need = payloadLen + n;                       // [nonce][cipher][tag] + [bản rõ]
            Span<byte> buf = need <= STACK_BYTES
                ? stackalloc byte[STACK_BYTES]
                : (rented = ArrayPool<byte>.Shared.Rent(need));
            Span<byte> nonce = buf.Slice(0, nonceLen);
            Span<byte> cipher = buf.Slice(nonceLen, n);
            Span<byte> tag = buf.Slice(nonceLen + n, tagLen);
            Span<byte> plain = buf.Slice(payloadLen, n);
            RandomNumberGenerator.Fill(nonce);
            Encoding.UTF8.GetBytes(chuoiGoc.AsSpan(), plain);
            _gcmLocal.Value!.Encrypt(nonce, plain, cipher, tag);
            plain.Clear();
            // PAYLOAD = NONCE + CIPHERTEXT + TAG
            int prefixLen = PREFIX_STEALTH_V4.Length;
            int totalChars = prefixLen + ((payloadLen + 2) / 3) * 4;
            Span<char> chars = totalChars <= 512 ? stackalloc char[512] : new char[totalChars];
            PREFIX_STEALTH_V4.AsSpan().CopyTo(chars);
            if (!Convert.TryToBase64Chars(buf.Slice(0, payloadLen), chars.Slice(prefixLen), out int written))
                return string.Empty;
            return new string(chars.Slice(0, prefixLen + written));
        }
        catch { return string.Empty; }
        finally { if (rented != null) ArrayPool<byte>.Shared.Return(rented, clearArray: true); }
    }
    public static string GiaiMa_NhatKy(string chuoiMaHoa)
    {
        if (string.IsNullOrEmpty(chuoiMaHoa)) return string.Empty;
        // Kiểm tra đúng tiền tố "AES:v4|"
        if (!chuoiMaHoa.StartsWith(PREFIX_STEALTH_V4, StringComparison.Ordinal)) return string.Empty;
        byte[]? rented = null;
        try
        {
            const int nonceLen = 12, tagLen = 16;
            ReadOnlySpan<char> b64 = chuoiMaHoa.AsSpan(PREFIX_STEALTH_V4.Length);
            int maxBytes = (b64.Length / 4 + 1) * 3;
            int need = maxBytes * 2;                         // [dữ liệu thô][bản rõ]
            Span<byte> buf = need <= STACK_BYTES
                ? stackalloc byte[STACK_BYTES]
                : (rented = ArrayPool<byte>.Shared.Rent(need));
            Span<byte> raw = buf.Slice(0, maxBytes);
            if (!Convert.TryFromBase64Chars(b64, raw, out int rawLen) || rawLen < nonceLen + tagLen)
                return string.Empty;
            int cLen = rawLen - nonceLen - tagLen;
            ReadOnlySpan<byte> nonce = raw.Slice(0, nonceLen);
            ReadOnlySpan<byte> cipher = raw.Slice(nonceLen, cLen);
            ReadOnlySpan<byte> tag = raw.Slice(nonceLen + cLen, tagLen);
            Span<byte> plain = buf.Slice(maxBytes, cLen);
            _gcmLocal.Value!.Decrypt(nonce, cipher, tag, plain);   // sai Tag => ném lỗi => trả rỗng
            string s = Encoding.UTF8.GetString(plain);
            plain.Clear();
            return s;
        }
        catch { return string.Empty; }
        finally { if (rented != null) ArrayPool<byte>.Shared.Return(rented, clearArray: true); }
    }
}
