using Microsoft.Data.Sqlite;
using System.Globalization;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Danh sách người của phần KHEN THƯỞNG (bảng ThongKeCBCS_DuocKhenThuong, CSDL4), tách hẳn khỏi phần thi đua (ThiDuaThang).
    /// Quy tắc (Form6 là gốc duy nhất, Số hiệu là khóa duy nhất):
    ///  - Số hiệu có trong DanhSach (Form6)      => có 1 dòng, "Đang công tác", họ tên/đơn vị lấy theo Form6.
    ///  - Số hiệu không còn trong DanhSach        => giữ dòng (giữ giấy khen), chuyển "Chuyển công tác".
    ///  - Số hiệu chỉ còn ở giấy khen (vd. vừa khôi phục) => tạo dòng từ thông tin trên giấy khen, "Chuyển công tác".
    ///  - Mỗi Số hiệu chỉ 1 dòng (dòng trùng do dữ liệu cũ được gộp); SoLuong_Khen = số giấy khen thực tế.
    /// Không bao giờ xóa giấy khen. Chỉ ghi khi có khác biệt (CSDL không đổi thì tệp không bị chạm).
    /// </summary>
    internal static class Module_DongBoKhenThuong
    {
        /// <summary>So Số hiệu: bỏ khoảng trắng 2 đầu, không phân biệt hoa/thường (đúng cách Form35 vẫn so).</summary>
        public static readonly StringComparer SoSanhSoHieu = StringComparer.OrdinalIgnoreCase;
        public sealed class NguoiForm6
        {
            public string HoTen = "", DonVi = "", GhiChu = "";
            public int Stt = int.MaxValue;
        }
        /// <summary>Tóm tắt giấy khen của 1 Số hiệu (đọc sẵn ở nơi gọi).</summary>
        public sealed class GiayKhenTom
        {
            public int SoLuong;
            public string RawHoTen = "", RawSoHieu = "", RawDonVi = "";   // bản mã nguyên gốc của giấy khen đầu tiên
        }
        /// <summary>Một dòng của danh sách khen thưởng sau đồng bộ (bản rõ).</summary>
        public sealed class NguoiKhenThuong
        {
            public long Id;
            public string SoHieu = "", HoTen = "", DonVi = "", GhiChu = "";
        }
        public sealed class KetQua
        {
            public List<NguoiKhenThuong> DanhSach = new();
            public int Them, CapNhat, GopTrung;
            public bool DaGhi;
        }
        private sealed class DongCu
        {
            public long RowId;
            public string Sh = "", Ht = "", Dv = "", Tt = "", GhiChu = "", RawGhiChu = "";
            public int SoLuong;
        }
        internal static string GiaiMa(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            string kq = Module_BaoMatAES.GiaiMa(s);   // có bộ đệm: Form34 nạp lại danh sách này nhiều lần
            if (!string.IsNullOrEmpty(kq)) return kq.Trim();
            return Module_BaoMatAES.LaChuoiMa(s) ? "" : s.Trim();   // bản mã hỏng: coi như rỗng, không lấy bản mã làm số hiệu
        }
        private static string Doc(SqliteDataReader rd, int i) => rd.IsDBNull(i) ? "" : (Convert.ToString(rd.GetValue(i), CultureInfo.InvariantCulture) ?? "");
        /// <summary>
        /// Đối chiếu danh sách khen thưởng với Form6 và trả về danh sách cuối cùng.
        /// <paramref name="choPhepGhi"/> = false (hoặc ghi lỗi do CSDL đang khóa): chỉ tính trên RAM, không ghi gì.
        /// </summary>
        public static KetQua DongBo(SqliteConnection cn4, IReadOnlyDictionary<string, NguoiForm6> form6,
            IReadOnlyDictionary<string, GiayKhenTom> giayKhen, bool choPhepGhi, CancellationToken ct = default)
        {
            if (choPhepGhi)
            {
                try { return ChayMotLan(cn4, form6, giayKhen, ghi: true, ct); }
                catch (SqliteException ex)
                {
                    System.Diagnostics.Debug.WriteLine("[DongBoKhenThuong] Không ghi được, chỉ đối chiếu trên RAM: " + ex.Message);
                }
            }
            return ChayMotLan(cn4, form6, giayKhen, ghi: false, ct);
        }
        private static KetQua ChayMotLan(SqliteConnection cn4, IReadOnlyDictionary<string, NguoiForm6> form6,
            IReadOnlyDictionary<string, GiayKhenTom> giayKhen, bool ghi, CancellationToken ct)
        {
            string ttDang = Module_HeThong.TT_DANG_CONG_TAC, ttChuyen = Module_HeThong.TT_CHUYEN_CONG_TAC;
            using var tran = ghi ? cn4.BeginTransaction() : null;
            try
            {
                if (ghi) Module_BangKhenThuong.DamBao(cn4, tran);
                // 1. Đọc bảng hiện có, gộp dòng trùng Số hiệu (giữ dòng có rowid nhỏ nhất)
                var theoSoHieu = new Dictionary<string, DongCu>(SoSanhSoHieu);
                var thuTu = new List<DongCu>();
                var rowIdCanXoa = new List<long>();
                var ghiChuCanGop = new List<DongCu>();
                bool coBang;
                using (var cmdCo = new SqliteCommand($"SELECT 1 FROM sqlite_master WHERE type='table' AND name='{Module_CheDoDoiTuong.BangKhenThuongNguoi}' LIMIT 1", cn4, tran))
                    coBang = cmdCo.ExecuteScalar() != null;
                if (coBang)
                using (var cmd = new SqliteCommand($"SELECT rowid, HoVaTen, SoHieu, DonVi, TinhTrang, SoLuong_Khen, GhiChu_Khen FROM {Module_CheDoDoiTuong.BangKhenThuongNguoi} ORDER BY rowid", cn4, tran))
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        ct.ThrowIfCancellationRequested();
                        string sh = GiaiMa(Doc(rd, 2));
                        if (sh.Length == 0) continue;
                        string rawGc = Doc(rd, 6);
                        string gc = GiaiMa(rawGc);
                        if (theoSoHieu.TryGetValue(sh, out var chu))
                        {
                            rowIdCanXoa.Add(rd.GetInt64(0));
                            if (chu.GhiChu.Length == 0 && gc.Length > 0) { chu.GhiChu = gc; chu.RawGhiChu = rawGc; ghiChuCanGop.Add(chu); }
                            continue;
                        }
                        int.TryParse(Doc(rd, 5).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int sl);
                        var d = new DongCu
                        {
                            RowId = rd.GetInt64(0), Sh = sh, Ht = GiaiMa(Doc(rd, 1)), Dv = GiaiMa(Doc(rd, 3)),
                            Tt = GiaiMa(Doc(rd, 4)), SoLuong = sl, GhiChu = gc, RawGhiChu = rawGc
                        };
                        theoSoHieu[sh] = d;
                        thuTu.Add(d);
                    }
                }
                var kq = new KetQua { GopTrung = rowIdCanXoa.Count };
                SqliteCommand? cmdCapNhat = null, cmdThem = null;
                if (ghi)
                {
                    cmdCapNhat = new SqliteCommand($"UPDATE {Module_CheDoDoiTuong.BangKhenThuongNguoi} SET HoVaTen=@ht, DonVi=@dv, TinhTrang=@tt, SoLuong_Khen=@sl, GhiChu_Khen=@gc WHERE rowid=@id", cn4, tran);
                    foreach (var p in new[] { "@ht", "@dv", "@tt", "@sl", "@gc" }) cmdCapNhat.Parameters.Add(p, SqliteType.Text);
                    cmdCapNhat.Parameters.Add("@id", SqliteType.Integer);
                    cmdThem = new SqliteCommand($"INSERT INTO {Module_CheDoDoiTuong.BangKhenThuongNguoi} (HoVaTen, SoHieu, DonVi, TinhTrang, SoLuong_Khen) VALUES (@ht, @sh, @dv, @tt, @sl); SELECT last_insert_rowid();", cn4, tran);
                    foreach (var p in new[] { "@ht", "@sh", "@dv", "@tt", "@sl" }) cmdThem.Parameters.Add(p, SqliteType.Text);
                }
                try
                {
                    if (ghi)
                    {
                        using var cmdXoa = new SqliteCommand($"DELETE FROM {Module_CheDoDoiTuong.BangKhenThuongNguoi} WHERE rowid=@id", cn4, tran);
                        var pId = cmdXoa.Parameters.Add("@id", SqliteType.Integer);
                        foreach (long id in rowIdCanXoa) { pId.Value = id; cmdXoa.ExecuteNonQuery(); }
                    }
                    int SoGiayKhen(string sh) => giayKhen.TryGetValue(sh, out var g) ? g.SoLuong : 0;
                    var gopGhiChu = new HashSet<DongCu>(ghiChuCanGop);
                    // 2. Dòng đã có: cập nhật tình trạng / họ tên / đơn vị theo Form6, số lượng theo giấy khen thực tế
                    foreach (var d in thuTu)
                    {
                        ct.ThrowIfCancellationRequested();
                        bool trongForm6 = form6.TryGetValue(d.Sh, out var nguoi);
                        string ht = trongForm6 && nguoi!.HoTen.Length > 0 ? nguoi.HoTen : d.Ht;
                        string dv = trongForm6 && nguoi!.DonVi.Length > 0 ? nguoi.DonVi : d.Dv;
                        string tt = trongForm6 ? ttDang : ttChuyen;
                        int sl = SoGiayKhen(d.Sh);
                        bool doiTen = !string.Equals(ht, d.Ht, StringComparison.Ordinal);
                        bool doiDv = !string.Equals(dv, d.Dv, StringComparison.Ordinal);
                        bool khac = doiTen || doiDv || !string.Equals(tt, d.Tt, StringComparison.Ordinal) || sl != d.SoLuong || gopGhiChu.Contains(d);
                        if (khac)
                        {
                            kq.CapNhat++;
                            if (cmdCapNhat != null)
                            {
                                cmdCapNhat.Parameters["@ht"].Value = Module_BaoMatAES.MaHoa(ht);
                                cmdCapNhat.Parameters["@dv"].Value = Module_BaoMatAES.MaHoa(dv);
                                cmdCapNhat.Parameters["@tt"].Value = tt;
                                cmdCapNhat.Parameters["@sl"].Value = sl.ToString(CultureInfo.InvariantCulture);
                                cmdCapNhat.Parameters["@gc"].Value = string.IsNullOrEmpty(d.RawGhiChu) ? DBNull.Value : d.RawGhiChu;
                                cmdCapNhat.Parameters["@id"].Value = d.RowId;
                                cmdCapNhat.ExecuteNonQuery();
                            }
                        }
                        kq.DanhSach.Add(new NguoiKhenThuong { Id = d.RowId, SoHieu = d.Sh, HoTen = ht, DonVi = dv, GhiChu = d.GhiChu });
                    }
                    long idTam = long.MaxValue;   // dòng chỉ có trên RAM (không ghi được): ID tạm, xếp sau cùng
                    // 3. Người có trong Form6 mà chưa có dòng => thêm "Đang công tác"
                    foreach (var kv in form6)
                    {
                        ct.ThrowIfCancellationRequested();
                        if (theoSoHieu.ContainsKey(kv.Key)) continue;
                        long id = idTam--;
                        if (cmdThem != null)
                        {
                            cmdThem.Parameters["@ht"].Value = Module_BaoMatAES.MaHoa(kv.Value.HoTen);
                            cmdThem.Parameters["@sh"].Value = Module_BaoMatAES.MaHoa(kv.Key);
                            cmdThem.Parameters["@dv"].Value = Module_BaoMatAES.MaHoa(kv.Value.DonVi);
                            cmdThem.Parameters["@tt"].Value = ttDang;
                            cmdThem.Parameters["@sl"].Value = SoGiayKhen(kv.Key).ToString(CultureInfo.InvariantCulture);
                            id = Convert.ToInt64(cmdThem.ExecuteScalar(), CultureInfo.InvariantCulture);
                        }
                        kq.Them++;
                        theoSoHieu[kv.Key] = new DongCu { RowId = id, Sh = kv.Key };
                        kq.DanhSach.Add(new NguoiKhenThuong { Id = id, SoHieu = kv.Key, HoTen = kv.Value.HoTen, DonVi = kv.Value.DonVi });
                    }
                    // 4. Số hiệu chỉ còn ở giấy khen (không có trong Form6, chưa có dòng) => thêm "Chuyển công tác"
                    foreach (var kv in giayKhen)
                    {
                        ct.ThrowIfCancellationRequested();
                        if (theoSoHieu.ContainsKey(kv.Key)) continue;
                        long id = idTam--;
                        var g = kv.Value;
                        if (cmdThem != null)
                        {
                            cmdThem.Parameters["@ht"].Value = g.RawHoTen;
                            cmdThem.Parameters["@sh"].Value = g.RawSoHieu.Length > 0 ? g.RawSoHieu : Module_BaoMatAES.MaHoa(kv.Key);
                            cmdThem.Parameters["@dv"].Value = g.RawDonVi;
                            cmdThem.Parameters["@tt"].Value = ttChuyen;
                            cmdThem.Parameters["@sl"].Value = g.SoLuong.ToString(CultureInfo.InvariantCulture);
                            id = Convert.ToInt64(cmdThem.ExecuteScalar(), CultureInfo.InvariantCulture);
                        }
                        kq.Them++;
                        theoSoHieu[kv.Key] = new DongCu { RowId = id, Sh = kv.Key };
                        kq.DanhSach.Add(new NguoiKhenThuong { Id = id, SoHieu = kv.Key, HoTen = GiaiMa(g.RawHoTen), DonVi = GiaiMa(g.RawDonVi) });
                    }
                }
                finally
                {
                    cmdCapNhat?.Dispose();
                    cmdThem?.Dispose();
                }
                if (tran != null)
                {
                    kq.DaGhi = kq.Them > 0 || kq.CapNhat > 0 || kq.GopTrung > 0;
                    if (kq.DaGhi) tran.Commit(); else tran.Rollback();
                }
                return kq;
            }
            catch
            {
                try { tran?.Rollback(); } catch { }
                throw;
            }
        }
    }
}
