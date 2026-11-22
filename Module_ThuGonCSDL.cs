///Phiên bản 1 – Thu gọn CSDL an toàn: khử dòng trùng lặp hàng loạt + VACUUM INTO + tráo tệp có kiểm chứng
///
using Microsoft.Data.Sqlite;
using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// THU GỌN CSDL (dùng khi CSDL phình to, cần chép phần mềm dạng portable sang máy khác).
    ///
    /// NGUYÊN NHÂN PHÌNH TO thường gặp: (1) trang rỗng sau khi xóa dữ liệu (freelist);
    /// (2) dữ liệu bị nhập lặp lại hàng trăm lần (vd: bảng Xa / DanhSachTinh trong csdl2 bị nhân 864 lần: 334 MB -> 4,6 MB).
    /// VACUUM thông thường chỉ xử lý được (1); module này xử lý cả (2).
    ///
    /// NGUYÊN TẮC AN TOÀN:
    ///  • Chỉ xóa dòng GIỐNG HỆT dòng khác trên MỌI cột (so từng byte + kiểu lưu trữ, chặt hơn DISTINCT của SQL), giữ lại bản có rowid nhỏ nhất
    ///    => không mất bất kỳ thông tin nào. Chỉ áp dụng cho bảng trùng HÀNG LOẠT (≥ 500 dòng và ≥ 50% số dòng): dấu hiệu nhập lặp do lỗi,
    ///    không đụng tới bảng nghiệp vụ có vài dòng trùng hợp lệ. Bỏ qua bảng có trigger / cột ẩn / WITHOUT ROWID / bảng ảo và bảng bóng của nó.
    ///  • Quét + xóa nằm trong MỘT giao dịch BEGIN IMMEDIATE (không ai chen ghi được), kiểm đếm lại trước khi COMMIT.
    ///  • Thu gọn bằng VACUUM INTO ra tệp MỚI (tệp gốc không bị viết lại), kiểm tra toàn vẹn + đối chiếu số dòng từng bảng, lược đồ, user_version,
    ///    rồi mới tráo bằng File.Replace (một thao tác hệ điều hành, giữ nguyên thuộc tính / ngày tạo). Lỗi ở bất kỳ bước nào => tệp gốc giữ nguyên.
    ///  • Bảo vệ ổ cứng: không đủ lợi (&lt; 1 MB hoặc &lt; 10% tệp) thì KHÔNG ghi gì cả; bảng tạm / sắp xếp đặt trong RAM (không ghi %TEMP%);
    ///    xóa bằng "DELETE toàn bảng" (SQLite chỉ đánh dấu trang rỗng, không viết lại hàng trăm MB).
    /// Module KHÔNG phụ thuộc module khác của phần mềm (kiểm thử độc lập được). Người gọi chịu trách nhiệm lấy khóa bảo trì
    /// (Module_BaoVeDuLieu.ChayDocQuyenAsync) để không chạy song song với VACUUM nền.
    /// </summary>
    internal static class Module_ThuGonCSDL
    {
        // ===================== NGƯỠNG =====================
        private const long SO_DONG_TOI_THIEU_DE_QUET = 1_000;        // bảng nhỏ hơn: không thể đủ 500 dòng trùng với tỉ lệ ≥ 50%
        private const long SO_DONG_TOI_DA_DE_QUET = 5_000_000;       // chặn RAM (≈ 40 byte/dòng khác nhau)
        private const long SO_DONG_TRUNG_TOI_THIEU = 500;
        private const double TI_LE_TRUNG_TOI_THIEU = 0.5;
        private const long BYTE_LOI_TOI_THIEU = 1L * 1024 * 1024;    // < 1 MB lợi: không đáng ghi lại tệp
        private const double TI_LE_LOI_TOI_THIEU = 0.10;             // < 10% tệp: không đáng ghi lại tệp
        private const long DU_PHONG_BYTE = 32L * 1024 * 1024;
        private const long NGUONG_KIEM_TRA_DAY_DU = 256L * 1024 * 1024;
        private const string BANG_TAM_GIU = "temp.\"__pmtd_giu\"";
        private const string BANG_TAM_BAN = "temp.\"__pmtd_ban\"";
        private const string COT_RID = "__pmtd_rid";
        private static readonly string[] BiDanhRowid = { "rowid", "_rowid_", "oid" };
        private static readonly Regex ReWithoutRowid = new(@"\bWITHOUT\s+ROWID\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        // ===================== KẾT QUẢ =====================
        internal sealed record BangTrungLap(string Ten, long SoDong, long SoDongTrung, long ByteTrungUocTinh)
        {
            public bool DuDieuKienKhuTrung => SoDongTrung >= SO_DONG_TRUNG_TOI_THIEU && SoDongTrung >= SoDong * TI_LE_TRUNG_TOI_THIEU;
        }
        internal sealed class BaoCaoPhanTich
        {
            public long KichThuocTep { get; init; }
            public long KichThuocWal { get; init; }
            public long PageSize { get; init; }
            public long PageCount { get; init; }
            public long FreePages { get; init; }
            public List<BangTrungLap> BangTrung { get; } = new();
            public long ByteTrangRong => FreePages * PageSize;
            public long ByteTrungUocTinh => BangTrung.Where(b => b.DuDieuKienKhuTrung).Sum(b => b.ByteTrungUocTinh);
            /// <summary>Ước tính dung lượng thu hồi được (trang rỗng + dòng trùng). KHÔNG tính tệp -wal: đó là nhật ký ghi, tự gộp lại khi checkpoint.</summary>
            public long ByteLangPhiUocTinh => Math.Min(KichThuocTep, ByteTrangRong + ByteTrungUocTinh);
            public double TiLeLangPhi => KichThuocTep > 0 ? ByteLangPhiUocTinh * 100.0 / KichThuocTep : 0;
            public bool NenThuGon => DangKeThuGon(ByteLangPhiUocTinh, KichThuocTep);
        }
        /// <param name="TepDangMo">true = thất bại vì tệp đang bị chương trình/kết nối khác mở (thử lại sau là được, không phải lỗi dữ liệu).</param>
        /// <param name="AutoVacuumSau">auto_vacuum của tệp sau khi chạy (2 = INCREMENTAL), -1 = không rõ.</param>
        internal sealed record KetQuaThuGon(bool ThanhCong, bool DaThayDoi, long TruocByte, long SauByte, long SoDongDaXoa, string ThongBao,
                                            IReadOnlyList<string> ChiTiet, bool TepDangMo = false, long AutoVacuumSau = -1);
        private static bool DangKeThuGon(long byteLoi, long tongByte)
            => byteLoi >= BYTE_LOI_TOI_THIEU && byteLoi >= tongByte * TI_LE_LOI_TOI_THIEU;
        // ===================== PHÂN TÍCH (CHỈ ĐỌC) =====================
        /// <summary>Phân tích chỉ đọc: trang rỗng + bảng trùng lặp hàng loạt. Không ghi gì vào tệp. Ném lỗi nếu không mở được.</summary>
        internal static BaoCaoPhanTich PhanTich(string duongDan, CancellationToken ct = default)
        {
            string full = Path.GetFullPath(duongDan);
            using var cn = MoKetNoi(full, SqliteOpenMode.ReadOnly);
            using var reg = DangKyNgat(cn, ct);
            ChayLenh(cn, "PRAGMA temp_store = MEMORY;");
            var bc = new BaoCaoPhanTich
            {
                KichThuocTep = DoDaiTep(full),
                KichThuocWal = DoDaiTep(full + "-wal"),
                PageSize = DocSo(cn, "PRAGMA page_size;"),
                PageCount = DocSo(cn, "PRAGMA page_count;"),
                FreePages = DocSo(cn, "PRAGMA freelist_count;")
            };
            foreach (var bang in LietKeBangUngVien(cn, null))
            {
                ct.ThrowIfCancellationRequested();
                long soDong = DocSo(cn, $"SELECT COUNT(*) FROM main.{Q(bang.Ten)};");
                if (soDong < SO_DONG_TOI_THIEU_DE_QUET || soDong > SO_DONG_TOI_DA_DE_QUET) continue;
                var (trung, byteTrung) = QuetTrungLap(cn, null, bang, null, ct);
                if (trung > 0) bc.BangTrung.Add(new BangTrungLap(bang.Ten, soDong, trung, byteTrung));
            }
            return bc;
        }
        // ===================== THU GỌN =====================
        /// <summary>
        /// Thu gọn 1 CSDL. Không ném lỗi (trừ OperationCanceledException): mọi thất bại trả về ThanhCong=false và tệp gốc giữ nguyên.
        /// <paramref name="batBuoc"/> = true (người dùng bấm Vacuum, hoặc bảo trì nền đã tự quyết định là cần): luôn chạy VACUUM INTO,
        /// bỏ qua ngưỡng "không đáng thu gọn".
        /// <paramref name="khuTrungLap"/> = false: chỉ dồn tệp (bảo trì nền tự động KHÔNG tự xóa dòng trùng; việc đó chỉ làm khi người dùng bấm).
        /// <paramref name="chapNhanDoiCheDo"/> = true: tệp gốc chưa ở auto_vacuum=INCREMENTAL thì vẫn thay dù tệp mới không nhỏ hơn
        /// (lớn hơn vài KB vì trang bản đồ con trỏ) – đổi 1 lần để về sau dọn trang rỗng bằng incremental_vacuum rẻ, không phải VACUUM toàn phần.
        /// <paramref name="thuMucTam"/>: nơi đặt tệp tạm (vd Database\NhatKyNhaPhatTrien\TepTam\...). Khác ổ đĩa với CSDL thì dùng thư mục của CSDL
        /// (File.Replace chỉ tráo được trong cùng một ổ).
        /// </summary>
        internal static KetQuaThuGon ThuGon(string duongDan, string? thuMucTam, CancellationToken ct = default, bool batBuoc = false,
                                            bool khuTrungLap = true, bool chapNhanDoiCheDo = false)
        {
            var chiTiet = new List<string>();
            string full;
            try { full = Path.GetFullPath(duongDan); }
            catch (Exception ex) { return ThatBai(0, "Đường dẫn CSDL không hợp lệ: " + ex.Message, chiTiet); }
            if (!File.Exists(full)) return ThatBai(0, "Không tìm thấy tệp CSDL.", chiTiet);
            long truoc = DoDaiTep(full) + DoDaiTep(full + "-wal");
            // Tệp đang bị mở ở nơi khác thì bước tráo tệp chắc chắn thất bại => dừng NGAY, không ghi bản sao vô ích xuống ổ cứng.
            // (Không loại trừ hoàn toàn: kiểm tra lại lần nữa ở bước tráo tệp.)
            SqliteConnection.ClearAllPools();
            if (TepDangBiMo(full))
                return ThatBai(truoc, "CSDL đang được chương trình/cửa sổ khác mở (ServiceBackup / ServiceRestore / cửa sổ nhập liệu...). " +
                                      "Hãy đóng chúng rồi thử lại.", chiTiet, tepDangMo: true);
            string? tam = null;
            string? banCu = null;
            try
            {
                long soDongDaXoa = 0;
                long dataVersion;
                string cheDoJournal;
                Dictionary<string, long> soDongNguon;
                List<string> luocDoNguon;
                long userVersion, applicationId;
                using (var cn = MoKetNoi(full, SqliteOpenMode.ReadWrite))
                using (var reg = DangKyNgat(cn, ct))
                {
                    ChayLenh(cn, "PRAGMA foreign_keys = OFF;");     // không bao giờ xóa dây chuyền sang bảng khác
                    ChayLenh(cn, "PRAGMA temp_store = MEMORY;");    // bảng tạm / sắp xếp trong RAM: không ghi %TEMP%
                    cheDoJournal = (DocChuoi(cn, "PRAGMA journal_mode;") ?? "").ToLowerInvariant();
                    long autoVacuumGoc = DocSo(cn, "PRAGMA auto_vacuum;");
                    string? kt = DocChuoi(cn, "PRAGMA quick_check(1);");
                    if (!string.Equals(kt, "ok", StringComparison.OrdinalIgnoreCase))
                        return ThatBai(truoc, "CSDL đang lỗi cấu trúc (quick_check: " + kt + "). KHÔNG thu gọn để bảo vệ dữ liệu còn lại.", chiTiet);
                    // ---- BƯỚC 1: KHỬ TRÙNG LẶP HÀNG LOẠT (1 giao dịch, khóa ghi ngay từ đầu) ----
                    // (Bảo trì nền: khuTrungLap = false => bỏ qua toàn bộ bước này, không ghi gì)
                    if (khuTrungLap)
                    using (var tx = cn.BeginTransaction(deferred: false))
                    {
                        ChayLenh(cn, $"CREATE TEMP TABLE IF NOT EXISTS \"__pmtd_giu\"(rid INTEGER PRIMARY KEY);", tx);
                        foreach (var bang in LietKeBangUngVien(cn, tx))
                        {
                            ct.ThrowIfCancellationRequested();
                            long soDong = DocSo(cn, $"SELECT COUNT(*) FROM main.{Q(bang.Ten)};", tx);
                            if (soDong < SO_DONG_TOI_THIEU_DE_QUET || soDong > SO_DONG_TOI_DA_DE_QUET) continue;
                            var giuLai = new List<long>();
                            var (trung, byteTrung) = QuetTrungLap(cn, tx, bang, giuLai, ct);
                            var thongTin = new BangTrungLap(bang.Ten, soDong, trung, byteTrung);
                            if (!thongTin.DuDieuKienKhuTrung)
                            {
                                if (trung > 0) chiTiet.Add($"Bảng {bang.Ten}: {trung:N0}/{soDong:N0} dòng trùng – chưa tới ngưỡng trùng hàng loạt, giữ nguyên");
                                continue;
                            }
                            KhuTrungMotBang(cn, tx, bang, giuLai, ct);
                            long conLai = DocSo(cn, $"SELECT COUNT(*) FROM main.{Q(bang.Ten)};", tx);
                            if (conLai != giuLai.Count)
                                throw new InvalidOperationException($"Kiểm đếm bảng {bang.Ten} sai lệch ({conLai:N0} ≠ {giuLai.Count:N0}). Đã hoàn tác.");
                            soDongDaXoa += soDong - conLai;
                            chiTiet.Add($"Bảng {bang.Ten}: xóa {soDong - conLai:N0} dòng trùng lặp, giữ {conLai:N0} dòng duy nhất");
                        }
                        ChayLenh(cn, $"DROP TABLE IF EXISTS {BANG_TAM_GIU};", tx);
                        tx.Commit();
                    }
                    if (soDongDaXoa > 0)
                    {
                        // Thống kê truy vấn cũ (sqlite_stat1) vẫn ghi hàng triệu dòng -> cập nhật cho bộ lập kế hoạch truy vấn
                        try { ChayLenh(cn, "PRAGMA optimize;"); } catch { }
                    }
                    // ---- BƯỚC 2: GỘP WAL + QUYẾT ĐỊNH CÓ ĐÁNG GHI LẠI TỆP KHÔNG ----
                    try { ChayLenh(cn, "PRAGMA wal_checkpoint(TRUNCATE);"); } catch (SqliteException) { /* có kết nối khác đang đọc: không sao */ }
                    long pageSize = DocSo(cn, "PRAGMA page_size;");
                    long pageCount = DocSo(cn, "PRAGMA page_count;");
                    long freePages = DocSo(cn, "PRAGMA freelist_count;");
                    long byteSongSong = (pageCount - freePages) * pageSize;
                    long hienTai = DoDaiTep(full) + DoDaiTep(full + "-wal");
                    if (!batBuoc && soDongDaXoa == 0 && !DangKeThuGon(hienTai - byteSongSong, hienTai))
                    {
                        try { ChayLenh(cn, "PRAGMA optimize;"); } catch { }
                        return new KetQuaThuGon(true, false, truoc, hienTai, 0,
                            $"CSDL đã gọn (dư thừa {DinhDang(Math.Max(0, hienTai - byteSongSong))}). Bỏ qua để bảo vệ tuổi thọ ổ cứng.", chiTiet);
                    }
                    // ---- BƯỚC 3: VACUUM INTO RA TỆP MỚI (tệp gốc không bị viết lại) ----
                    string thuMuc = ChonThuMucTam(full, thuMucTam);
                    if (!DuChoTrong(thuMuc, byteSongSong + DU_PHONG_BYTE))
                        return ThatBai(truoc, $"Ổ đĩa không đủ chỗ trống (cần khoảng {DinhDang(byteSongSong + DU_PHONG_BYTE)}).", chiTiet);
                    tam = Path.Combine(thuMuc, Path.GetFileName(full) + ".thugon-" + Guid.NewGuid().ToString("N") + ".tmp");
                    dataVersion = DocSo(cn, "PRAGMA data_version;");
                    // Đặt TRƯỚC VACUUM INTO: tệp mới mang auto_vacuum=INCREMENTAL (đúng cấu hình nền của phần mềm),
                    // nhờ vậy bảo trì cuối tháng không còn phải VACUUM toàn phần chỉ để đổi chế độ.
                    ChayLenh(cn, "PRAGMA auto_vacuum = INCREMENTAL;");
                    using (var cmd = cn.CreateCommand())
                    {
                        cmd.CommandText = "VACUUM INTO $dich;";
                        cmd.CommandTimeout = 0;
                        cmd.Parameters.AddWithValue("$dich", tam);
                        cmd.ExecuteNonQuery();
                    }
                    soDongNguon = DemDongMoiBang(cn);
                    luocDoNguon = DocLuocDo(cn);
                    userVersion = DocSo(cn, "PRAGMA user_version;");
                    applicationId = DocSo(cn, "PRAGMA application_id;");
                    // Tệp mới không nhỏ hơn (CSDL vốn đã gọn; INCREMENTAL thêm trang bản đồ con trỏ) => bỏ tệp mới, giữ nguyên tệp gốc
                    long kichThuocGoc = DoDaiTep(full);
                    bool doiCheDo = chapNhanDoiCheDo && autoVacuumGoc != 2;
                    if (soDongDaXoa == 0 && !doiCheDo && DoDaiTep(tam) >= kichThuocGoc)
                    {
                        XoaKemTepPhu(tam);
                        tam = null;
                        try { ChayLenh(cn, "PRAGMA optimize;"); } catch { }
                        return new KetQuaThuGon(true, false, truoc, kichThuocGoc + DoDaiTep(full + "-wal"), 0,
                            $"Đã chạy Vacuum: CSDL đã ở kích thước tối thiểu ({DinhDang(kichThuocGoc)}), không cần thay tệp.", chiTiet,
                            AutoVacuumSau: autoVacuumGoc);
                    }
                    // ---- BƯỚC 4: HOÀN THIỆN + KIỂM CHỨNG TỆP MỚI ----
                    HoanThienVaKiemChung(tam, cheDoJournal == "wal", soDongNguon, luocDoNguon, userVersion, applicationId, ct);
                    // Có tiến trình/kết nối khác vừa ghi vào tệp gốc sau khi chụp => bản mới đã cũ, hủy
                    if (DocSo(cn, "PRAGMA data_version;") != dataVersion)
                        return ThatBai(truoc, "Có chương trình khác vừa ghi vào CSDL trong lúc thu gọn. Đã hủy an toàn, vui lòng thử lại.", chiTiet, tam, tepDangMo: true);
                }
                // ---- BƯỚC 5: TRÁO TỆP ----
                SqliteConnection.ClearAllPools();   // nhả các kết nối đang nằm chờ trong pool của chính phần mềm
                if (!DonTepPhuSach(full, out string lyDo))
                    return ThatBai(truoc, lyDo, chiTiet, tam, tepDangMo: true);
                using (var fs = new FileStream(tam, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    fs.Flush(flushToDisk: true);    // ép tệp mới xuống đĩa trước khi tráo (an toàn khi mất điện)
                banCu = full + ".truocthugon-" + DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                try
                {
                    File.Replace(tam, full, banCu, ignoreMetadataErrors: true);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    banCu = null;
                    return ThatBai(truoc, "CSDL đang được chương trình khác mở (ServiceBackup / ServiceRestore / cửa sổ khác...). " +
                                          "Hãy đóng chúng rồi thử lại. Chi tiết: " + ex.Message, chiTiet, tam, tepDangMo: true);
                }
                tam = null;
                long autoVacuumSau = -1;
                // ---- BƯỚC 6: KIỂM TRA SAU TRÁO; lỗi (rất hiếm) thì trả lại tệp cũ ----
                try
                {
                    using var cnMoi = MoKetNoi(full, SqliteOpenMode.ReadWrite);
                    if (!string.Equals(DocChuoi(cnMoi, "PRAGMA quick_check(1);"), "ok", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("quick_check sau khi tráo tệp không đạt.");
                    autoVacuumSau = DocSo(cnMoi, "PRAGMA auto_vacuum;");
                }
                catch (Exception ex)
                {
                    SqliteConnection.ClearAllPools();
                    try { File.Replace(banCu, full, null, ignoreMetadataErrors: true); banCu = null; }
                    catch { /* không trả lại được: GIỮ tệp cũ ở banCu, báo cho người dùng */ }
                    return ThatBai(truoc, "Kiểm tra sau khi tráo tệp không đạt, đã trả lại CSDL cũ. Chi tiết: " + ex.Message
                                          + (banCu != null ? $" (Bản cũ còn ở: {banCu})" : ""), chiTiet);
                }
                if (XoaTep(banCu)) banCu = null;
                else chiTiet.Add("Không xóa được tệp cũ (có thể do phần mềm diệt virus đang quét): " + banCu);
                long sau = DoDaiTep(full) + DoDaiTep(full + "-wal");
                return new KetQuaThuGon(true, true, truoc, sau, soDongDaXoa,
                    $"Thu gọn thành công: {DinhDang(truoc)} → {DinhDang(sau)} (giảm {DinhDang(Math.Max(0, truoc - sau))}).", chiTiet, AutoVacuumSau: autoVacuumSau);
            }
            catch (OperationCanceledException) { throw; }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 9) // SQLITE_INTERRUPT
            {
                throw new OperationCanceledException("Đã hủy thu gọn CSDL.", ex, ct);
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 5 || ex.SqliteErrorCode == 6)
            {
                return ThatBai(truoc, "CSDL đang bị khóa bởi chương trình khác (SQLITE_BUSY). Hãy đóng các cửa sổ đang ghi dữ liệu rồi thử lại.", chiTiet, tepDangMo: true);
            }
            catch (Exception ex)
            {
                return ThatBai(truoc, "Không thu gọn được, CSDL giữ nguyên. Chi tiết: " + ex.Message, chiTiet);
            }
            finally
            {
                if (tam != null) XoaKemTepPhu(tam);
            }
        }
        // ===================== KHỬ TRÙNG =====================
        private sealed record BangUngVien(string Ten, string BiDanhRowid, string[] Cot);
        /// <summary>Bảng thường có rowid, không trigger, không cột ẩn/sinh tự động, không phải bảng ảo hay bảng bóng của bảng ảo.</summary>
        private static List<BangUngVien> LietKeBangUngVien(SqliteConnection cn, SqliteTransaction? tx)
        {
            var bang = new List<(string Ten, string Sql)>();
            var coTrigger = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var bangAo = new List<string>();
            using (var cmd = cn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "SELECT type, name, tbl_name, IFNULL(sql,'') FROM main.sqlite_master WHERE type IN ('table','trigger');";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    string loai = r.GetString(0), ten = r.GetString(1), sql = r.GetString(3);
                    if (loai == "trigger") { coTrigger.Add(r.GetString(2)); continue; }
                    if (ten.StartsWith("sqlite_", StringComparison.OrdinalIgnoreCase)) continue;
                    if (sql.TrimStart().StartsWith("CREATE VIRTUAL", StringComparison.OrdinalIgnoreCase)) { bangAo.Add(ten); continue; }
                    bang.Add((ten, sql));
                }
            }
            var kq = new List<BangUngVien>();
            foreach (var (ten, sql) in bang)
            {
                if (coTrigger.Contains(ten) || ReWithoutRowid.IsMatch(sql)) continue;
                if (bangAo.Any(a => ten.StartsWith(a + "_", StringComparison.OrdinalIgnoreCase))) continue;
                var cot = new List<string>();
                bool coCotAn = false;
                using (var cmd = cn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = $"PRAGMA main.table_xinfo({Q(ten)});";
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        if (r.GetInt64(6) != 0) coCotAn = true;   // cột ẩn / generated
                        cot.Add(r.GetString(1));
                    }
                }
                if (coCotAn || cot.Count == 0) continue;
                string? biDanh = BiDanhRowid.FirstOrDefault(b => !cot.Contains(b, StringComparer.OrdinalIgnoreCase));
                if (biDanh == null || cot.Contains(COT_RID, StringComparer.OrdinalIgnoreCase)) continue;
                kq.Add(new BangUngVien(ten, biDanh, cot.ToArray()));
            }
            return kq;
        }
        /// <summary>
        /// Quét tuần tự theo rowid (không sắp xếp, không tệp tạm). SQLite (mã native) dựng cho mỗi dòng 1 chuỗi byte "chữ ký nội dung"
        /// CHÍNH XÁC và không nhập nhằng: TEXT = 't' + hex toàn bộ byte gốc (kể cả byte 0 / UTF-8 lỗi), BLOB = X'..', số nguyên, NULL; REAL in đủ 17 chữ số (khứ hồi chính xác) kèm tiền tố riêng => 1 và 1.0, 'NULL' và NULL, '5' và 5 đều KHÁC nhau
        /// (chặt hơn DISTINCT của SQL). C# chỉ băm SHA-256 (giữ 128 bit) và tra HashSet.
        /// Trả về số dòng trùng + ước tính byte; nếu <paramref name="giuLai"/> != null thì ghi rowid của bản đầu tiên mỗi nhóm.
        /// </summary>
        private static (long Trung, long ByteTrung) QuetTrungLap(SqliteConnection cn, SqliteTransaction? tx, BangUngVien bang, List<long>? giuLai, CancellationToken ct)
        {
            string chuKy = string.Join(" || ',' || ", bang.Cot.Select(c =>
            {
                string qc = Q(c);
                return $"(CASE typeof({qc}) WHEN 'real' THEN 'r' || printf('%!.17g', {qc}) WHEN 'text' THEN 't' || hex({qc}) ELSE quote({qc}) END)";
            }));
            // Dung lượng thật của dòng (byte gốc, không phải dạng hex) để ước tính phần thu hồi được
            string doDai = string.Join(" + ", bang.Cot.Select(c => $"IFNULL(length(CAST({Q(c)} AS BLOB)),0)"));
            using var cmd = cn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandTimeout = 0;
            cmd.CommandText = $"SELECT {bang.BiDanhRowid}, CAST({chuKy} AS BLOB), {doDai} FROM main.{Q(bang.Ten)};";
            var daGap = new HashSet<(ulong, ulong)>();
            Span<byte> bam = stackalloc byte[32];
            long trung = 0, byteTrung = 0, dem = 0;
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                if ((++dem & 0xFFFF) == 0) ct.ThrowIfCancellationRequested();
                byte[] noiDung = r.GetFieldValue<byte[]>(1);
                SHA256.HashData(noiDung, bam);
                var khoa = (BinaryPrimitives.ReadUInt64LittleEndian(bam), BinaryPrimitives.ReadUInt64LittleEndian(bam.Slice(8)));
                if (daGap.Add(khoa)) giuLai?.Add(r.GetInt64(0));
                else { trung++; byteTrung += r.GetInt64(2); }
            }
            return (trung, byteTrung);
        }
        /// <summary>
        /// Chép riêng các dòng giữ lại (kèm rowid gốc) ra bảng tạm trong RAM -> "DELETE toàn bảng" (SQLite chỉ trả trang về freelist,
        /// không viết lại từng trang) -> chèn lại đúng rowid cũ. Ghi đĩa rất ít dù bảng có hàng triệu dòng.
        /// </summary>
        private static void KhuTrungMotBang(SqliteConnection cn, SqliteTransaction tx, BangUngVien bang, List<long> giuLai, CancellationToken ct)
        {
            ChayLenh(cn, $"DELETE FROM {BANG_TAM_GIU};", tx);
            using (var ins = cn.CreateCommand())
            {
                ins.Transaction = tx;
                ins.CommandText = $"INSERT INTO {BANG_TAM_GIU}(rid) VALUES ($r);";
                var p = ins.Parameters.Add("$r", SqliteType.Integer);
                ins.Prepare();
                for (int i = 0; i < giuLai.Count; i++)
                {
                    if ((i & 0xFFFF) == 0) ct.ThrowIfCancellationRequested();
                    p.Value = giuLai[i];
                    ins.ExecuteNonQuery();
                }
            }
            string bangQ = "main." + Q(bang.Ten);
            string dsCot = string.Join(",", bang.Cot.Select(Q));
            ChayLenh(cn, $"DROP TABLE IF EXISTS {BANG_TAM_BAN};", tx);
            // Bảng tạm KHÔNG khai báo kiểu cột (không affinity): giá trị được giữ nguyên tuyệt đối khi chép qua lại
            ChayLenh(cn, $"CREATE TEMP TABLE \"__pmtd_ban\"({COT_RID},{dsCot});", tx);
            ChayLenh(cn, $"INSERT INTO {BANG_TAM_BAN}({COT_RID},{dsCot}) SELECT {bang.BiDanhRowid},{dsCot} FROM {bangQ} " +
                         $"WHERE {bang.BiDanhRowid} IN (SELECT rid FROM {BANG_TAM_GIU});", tx);
            ChayLenh(cn, $"DELETE FROM {bangQ};", tx);
            ChayLenh(cn, $"INSERT INTO {bangQ}({bang.BiDanhRowid},{dsCot}) SELECT {COT_RID},{dsCot} FROM {BANG_TAM_BAN} ORDER BY {COT_RID};", tx);
            ChayLenh(cn, $"DROP TABLE {BANG_TAM_BAN};", tx);
        }
        // ===================== KIỂM CHỨNG =====================
        private static void HoanThienVaKiemChung(string tam, bool batWal, Dictionary<string, long> soDongNguon, List<string> luocDoNguon,
                                                 long userVersion, long applicationId, CancellationToken ct)
        {
            using var cn = MoKetNoi(tam, SqliteOpenMode.ReadWrite);
            using var reg = DangKyNgat(cn, ct);
            ChayLenh(cn, "PRAGMA temp_store = MEMORY;");
            string kiemTra = DoDaiTep(tam) <= NGUONG_KIEM_TRA_DAY_DU ? "PRAGMA integrity_check(1);" : "PRAGMA quick_check(1);";
            string? kq = DocChuoi(cn, kiemTra);
            if (!string.Equals(kq, "ok", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Tệp thu gọn không vượt qua kiểm tra toàn vẹn: " + kq);
            if (!luocDoNguon.SequenceEqual(DocLuocDo(cn), StringComparer.Ordinal))
                throw new InvalidDataException("Lược đồ (bảng/chỉ mục/trigger) của tệp thu gọn khác tệp gốc.");
            var soDongMoi = DemDongMoiBang(cn);
            foreach (var (ten, n) in soDongNguon)
            {
                if (!soDongMoi.TryGetValue(ten, out long m) || m != n)
                    throw new InvalidDataException($"Số dòng bảng {ten} không khớp ({n:N0} ≠ {m:N0}).");
            }
            if (DocSo(cn, "PRAGMA user_version;") != userVersion || DocSo(cn, "PRAGMA application_id;") != applicationId)
                throw new InvalidDataException("user_version / application_id của tệp thu gọn khác tệp gốc.");
            if (batWal)
            {
                // VACUUM INTO luôn tạo tệp ở chế độ DELETE: trả lại WAL như tệp gốc (đóng kết nối cuối cùng thì -wal/-shm tự biến mất)
                string? cheDo = DocChuoi(cn, "PRAGMA journal_mode = WAL;");
                if (!string.Equals(cheDo, "wal", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Không bật lại được chế độ WAL cho tệp thu gọn.");
            }
        }
        private static Dictionary<string, long> DemDongMoiBang(SqliteConnection cn)
        {
            var ten = new List<string>();
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "SELECT name FROM main.sqlite_master WHERE type='table' AND IFNULL(sql,'') NOT LIKE 'CREATE VIRTUAL%';";
                using var r = cmd.ExecuteReader();
                while (r.Read()) ten.Add(r.GetString(0));
            }
            var kq = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (string t in ten) kq[t] = DocSo(cn, $"SELECT COUNT(*) FROM main.{Q(t)};");
            return kq;
        }
        private static List<string> DocLuocDo(SqliteConnection cn)
        {
            var kq = new List<string>();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT type || '|' || name || '|' || tbl_name || '|' || IFNULL(sql,'') FROM main.sqlite_master " +
                              "WHERE name NOT LIKE 'sqlite\\_autoindex%' ESCAPE '\\' ORDER BY type, name;";
            using var r = cmd.ExecuteReader();
            while (r.Read()) kq.Add(r.GetString(0));
            return kq;
        }
        // ===================== TIỆN ÍCH =====================
        private static SqliteConnection MoKetNoi(string path, SqliteOpenMode mode)
        {
            var cn = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = mode,            // không có Create: tệp biến mất giữa chừng thì báo lỗi, không sinh tệp rỗng
                Pooling = false,        // Dispose = đóng hẳn tệp (cần cho việc tráo tệp)
                DefaultTimeout = 30
            }.ToString());
            try { cn.Open(); return cn; }
            catch { cn.Dispose(); throw; }
        }
        /// <summary>sqlite3_interrupt khi hủy: dừng thật câu lệnh dài (VACUUM INTO / quét), SQLite tự hoàn tác.</summary>
        private static CancellationTokenRegistration DangKyNgat(SqliteConnection cn, CancellationToken ct)
        {
            if (!ct.CanBeCanceled) return default;
            return ct.Register(static s =>
            {
                try { var h = ((SqliteConnection)s!).Handle; if (h != null) SQLitePCL.raw.sqlite3_interrupt(h); } catch { }
            }, cn);
        }
        private static void ChayLenh(SqliteConnection cn, string sql, SqliteTransaction? tx = null)
        {
            using var cmd = cn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandTimeout = 0;
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
        private static long DocSo(SqliteConnection cn, string sql, SqliteTransaction? tx = null)
        {
            using var cmd = cn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandTimeout = 0;
            cmd.CommandText = sql;
            object? v = cmd.ExecuteScalar();
            return v == null || v == DBNull.Value ? 0 : Convert.ToInt64(v, CultureInfo.InvariantCulture);
        }
        private static string? DocChuoi(SqliteConnection cn, string sql)
        {
            using var cmd = cn.CreateCommand();
            cmd.CommandTimeout = 0;
            cmd.CommandText = sql;
            return Convert.ToString(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }
        private static string Q(string ten) => "\"" + ten.Replace("\"", "\"\"") + "\"";
        private static long DoDaiTep(string p)
        {
            try { var fi = new FileInfo(p); return fi.Exists ? fi.Length : 0; } catch { return 0; }
        }
        private static string ChonThuMucTam(string full, string? uuTien)
        {
            string thuMucDb = Path.GetDirectoryName(full) ?? throw new IOException("Không xác định được thư mục CSDL.");
            if (string.IsNullOrWhiteSpace(uuTien)) return thuMucDb;
            try
            {
                string u = Path.GetFullPath(uuTien);
                if (!string.Equals(Path.GetPathRoot(u), Path.GetPathRoot(full), StringComparison.OrdinalIgnoreCase)) return thuMucDb;
                Directory.CreateDirectory(u);
                return u;
            }
            catch { return thuMucDb; }
        }
        private static bool DuChoTrong(string thuMuc, long can)
        {
            try
            {
                string? goc = Path.GetPathRoot(Path.GetFullPath(thuMuc));
                if (string.IsNullOrEmpty(goc) || goc.StartsWith(@"\\", StringComparison.Ordinal)) return true;
                return new DriveInfo(goc).AvailableFreeSpace >= can;
            }
            catch { return true; }
        }
        /// <summary>
        /// Trước khi tráo: tệp -wal còn dữ liệu (kết nối khác đang mở) hoặc -journal "nóng" mà nằm cạnh tệp mới sẽ làm HỎNG tệp mới
        /// => phải không còn, hoặc rỗng và xóa được.
        /// </summary>
        private static bool DonTepPhuSach(string full, out string lyDo)
        {
            lyDo = "";
            if (DoDaiTep(full + "-journal") > 0)
            {
                lyDo = "CSDL còn giao dịch dở dang (tệp -journal). Hãy mở/đóng phần mềm một lần rồi thử lại.";
                return false;
            }
            if (DoDaiTep(full + "-wal") > 0)
            {
                lyDo = "CSDL đang được chương trình/cửa sổ khác mở (tệp -wal còn dữ liệu). Hãy đóng chúng rồi thử lại.";
                return false;
            }
            foreach (string hau in new[] { "-wal", "-shm", "-journal" })
            {
                if (!XoaTep(full + hau))
                {
                    lyDo = "CSDL đang được chương trình/cửa sổ khác mở. Hãy đóng chúng (kể cả ServiceBackup / ServiceRestore) rồi thử lại.";
                    return false;
                }
            }
            return true;
        }
        private static bool XoaTep(string? p)
        {
            if (string.IsNullOrEmpty(p)) return true;
            try
            {
                if (File.Exists(p)) { File.SetAttributes(p, FileAttributes.Normal); File.Delete(p); }
                return true;
            }
            catch { return false; }
        }
        private static void XoaKemTepPhu(string p)
        {
            XoaTep(p);
            XoaTep(p + "-wal");
            XoaTep(p + "-shm");
            XoaTep(p + "-journal");
        }
        /// <summary>Có tay cầm khác (tiến trình khác / kết nối SQLite khác) đang mở tệp: thử mở độc quyền rồi đóng ngay, không ghi gì.</summary>
        private static bool TepDangBiMo(string path)
        {
            try
            {
                using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                return false;
            }
            catch (IOException) { return true; }
            catch (UnauthorizedAccessException) { return false; }   // tệp chỉ-đọc: để bước mở kết nối báo lỗi rõ ràng
        }
        private static KetQuaThuGon ThatBai(long truoc, string thongBao, List<string> chiTiet, string? tamCanXoa = null, bool tepDangMo = false)
        {
            if (tamCanXoa != null) XoaKemTepPhu(tamCanXoa);
            return new KetQuaThuGon(false, false, truoc, truoc, 0, thongBao, chiTiet, tepDangMo);
        }
        internal static string DinhDang(long b) =>
            b >= 1024L * 1024 * 1024 ? (b / 1024d / 1024 / 1024).ToString("F2", CultureInfo.InvariantCulture) + " GB" :
            b >= 1024L * 1024 ? (b / 1024d / 1024).ToString("F2", CultureInfo.InvariantCulture) + " MB" :
            (b / 1024d).ToString("F1", CultureInfo.InvariantCulture) + " KB";
    }
}
