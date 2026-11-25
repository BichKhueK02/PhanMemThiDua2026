using Microsoft.Data.Sqlite;
using System.Diagnostics;
namespace PhanMemThiDua2026
{
    /// <summary>Một đợt xét của nhóm tuần: 4 tuần trong 1 tháng -> 1 kết quả tháng.</summary>
    public sealed class DotXet
    {
        /// <summary>Số thứ tự đợt (1..n).</summary>
        public int So { get; init; }
        /// <summary>Tháng / năm lịch của 4 tuần.</summary>
        public int Thang { get; init; }
        public int Nam { get; init; }
        /// <summary>Tháng / năm ghi trên nhãn kết quả (cùng tháng, hoặc tháng kế tiếp nếu cài "kết quả ghi cho tháng sau").</summary>
        public int ThangKetQua { get; init; }
        public int NamKetQua { get; init; }
        /// <summary>4 cột tuần trong bảng thi đua.</summary>
        public string[] CotTuan { get; init; } = Array.Empty<string>();
        /// <summary>Cột kết quả tháng.</summary>
        public string CotKetQua { get; init; } = "";
        /// <summary>4 cột tuần rồi cột kết quả (đúng thứ tự hiển thị).</summary>
        public IEnumerable<string> TatCaCot => CotTuan.Append(CotKetQua);
    }

    /// <summary>Cấu hình lịch xét của 1 chế độ nhóm tuần.</summary>
    public readonly record struct CauHinhLich(int ThangBatDau, int SoThang, bool KetQuaThangSau);

    /// <summary>
    /// LỊCH XÉT NHÓM TUẦN (Tân binh / Học viên): NGUỒN DUY NHẤT về cột tuần / cột kết quả tháng và nhãn hiển thị.
    /// Mọi form (sửa hồ sơ, thống kê, xuất/xóa dữ liệu, tính phân loại, năm cũ) đọc ở đây, không viết cứng tên cột.
    ///
    /// Cả Tân binh và Học viên đều do người dùng cài (Form63): tháng bắt đầu, số tháng (1..6, được kéo sang năm sau),
    /// kết quả ghi cho cùng tháng hay tháng kế tiếp. Mỗi tháng 4 tuần -> 1 kết quả tháng.
    /// Bảng thi đua luôn có đủ 6 đợt: Tuan_{w}_D{d}, Thang_D{d} (cột đặt theo SỐ ĐỢT, không theo tháng lịch => đổi lịch
    /// không phải đổi lược đồ). Lược đồ cũ của Tân binh (Tuan_x_T2..T5 -> Thang_3..6) được tự nâng cấp (NangCapBang).
    /// Mặc định giữ đúng cách xét trước đây: Tân binh tháng 2, 4 tháng, kết quả tháng sau; Học viên tháng 9, 3 tháng.
    /// </summary>
    internal static class Module_LichXet
    {
        public const int SoTuanMotThang = 4;
        public const int SoDotToiDa = 6;
        public static CauHinhLich MacDinh(CheDoDoiTuong c) =>
            c == CheDoDoiTuong.HocVien ? new CauHinhLich(9, 3, false) : new CauHinhLich(2, 4, true);

        // ===================== CỘT VẬT LÝ =====================
        public static string CotTuan(int tuan, int dot) => $"Tuan_{tuan}_D{dot}";
        public static string CotKetQuaDot(int dot) => $"Thang_D{dot}";
        private static readonly string[] _cotVatLy = Enumerable.Range(1, SoDotToiDa)
            .SelectMany(d => Enumerable.Range(1, SoTuanMotThang).Select(w => CotTuan(w, d)).Append(CotKetQuaDot(d)))
            .ToArray();
        /// <summary>Lược đồ CŨ của Tân binh (trước 2026-11-24) - chỉ còn trong tệp năm cũ / CSDL chưa nâng cấp.</summary>
        public static readonly string[] CotVatLyCu =
        {
            "Tuan_1_T2", "Tuan_2_T2", "Tuan_3_T2", "Tuan_4_T2", "Thang_2",
            "Tuan_1_T3", "Tuan_2_T3", "Tuan_3_T3", "Tuan_4_T3", "Thang_3",
            "Tuan_1_T4", "Tuan_2_T4", "Tuan_3_T4", "Tuan_4_T4", "Thang_4",
            "Tuan_1_T5", "Tuan_2_T5", "Tuan_3_T5", "Tuan_4_T5", "Thang_5",
            "Tuan_1_T6", "Tuan_2_T6", "Tuan_3_T6", "Tuan_4_T6", "Thang_6",
        };
        /// <summary>Thứ tự hiển thị của lược đồ cũ: tuần tháng 2..5 -> kết quả tháng 3..6.</summary>
        private static readonly string[] _cotCuTheoThuTu = Enumerable.Range(2, 4)
            .SelectMany(m => Enumerable.Range(1, SoTuanMotThang).Select(w => $"Tuan_{w}_T{m}").Append($"Thang_{m + 1}"))
            .ToArray();

        /// <summary>Mọi cột thời gian có trong bảng thi đua của chế độ (đúng thứ tự trong CSDL). CBCS: rỗng.</summary>
        public static string[] CotVatLy(CheDoDoiTuong c) => Module_CheDoDoiTuong.LaNhomTuan(c) ? _cotVatLy : Array.Empty<string>();
        private const string CotDau = @"""ID"" INTEGER NOT NULL, ""HoVaTen"" TEXT, ""SoHieu"" TEXT, ""DonVi"" TEXT, ""TinhTrang"" TEXT";
        private const string CotCuoi = @"""TS_Loai1"" TEXT, ""TS_Loai2"" TEXT, ""TS_Loai3"" TEXT, ""TS_Loai4"" TEXT";
        /// <summary>CREATE TABLE IF NOT EXISTS bảng thi đua nhóm tuần.</summary>
        public static string DdlThiDuaThang(string ten) =>
            $@"CREATE TABLE IF NOT EXISTS ""{ten}"" ({CotDau}, " +
            string.Join(", ", _cotVatLy.Select(x => $@"""{x}"" TEXT")) +
            $@", {CotCuoi}, PRIMARY KEY(""ID"" AUTOINCREMENT));";

        /// <summary>
        /// Bảng còn lược đồ cũ (Tuan_x_T2..) => dựng lại theo lược đồ 6 đợt trong cùng giao dịch.
        /// Dữ liệu cũ sang đợt 1..4 (tuần tháng 2..5 -> đợt 1..4, Thang_3..6 -> Thang_D1..D4) - khớp lịch mặc định của Tân binh;
        /// cột phát sinh (GhiChu...) giữ nguyên. Bảng đã nâng cấp: không làm gì.
        /// </summary>
        public static void NangCapBang(SqliteConnection cn, SqliteTransaction tran, string ten)
        {
            var cot = new List<string>();
            using (var cmd = cn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = $@"PRAGMA table_info(""{ten}"")";
                using var r = cmd.ExecuteReader();
                while (r.Read()) cot.Add(r.GetString(1));
            }
            if (cot.Count == 0 || cot.Contains(CotKetQuaDot(1), StringComparer.OrdinalIgnoreCase)) return;
            var coSan = new HashSet<string>(cot, StringComparer.OrdinalIgnoreCase);
            var cu = new HashSet<string>(CotVatLyCu, StringComparer.OrdinalIgnoreCase);
            var coBan = new[] { "ID", "HoVaTen", "SoHieu", "DonVi", "TinhTrang", "TS_Loai1", "TS_Loai2", "TS_Loai3", "TS_Loai4" };
            var phatSinh = cot.Where(x => !cu.Contains(x) && !coBan.Contains(x, StringComparer.OrdinalIgnoreCase)).ToList();
            var chep = coBan.Where(coSan.Contains).Select(x => (Moi: x, Cu: x)).ToList();
            for (int d = 1; d <= 4; d++)
            {
                for (int w = 1; w <= SoTuanMotThang; w++)
                    if (coSan.Contains($"Tuan_{w}_T{d + 1}")) chep.Add((CotTuan(w, d), $"Tuan_{w}_T{d + 1}"));
                if (coSan.Contains($"Thang_{d + 2}")) chep.Add((CotKetQuaDot(d), $"Thang_{d + 2}"));
            }
            chep.AddRange(phatSinh.Select(x => (x, x)));
            string tam = ten + "__NangCap";
            string ddl = DdlThiDuaThang(tam);
            if (phatSinh.Count > 0)
                ddl = ddl.Replace(", PRIMARY KEY(", ", " + string.Join(", ", phatSinh.Select(x => $@"""{x}"" TEXT")) + ", PRIMARY KEY(");
            void Chay(string sql)
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tran;
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
            Chay($@"DROP TABLE IF EXISTS ""{tam}""");
            Chay(ddl);
            Chay($@"INSERT INTO ""{tam}"" ({string.Join(", ", chep.Select(x => $@"""{x.Moi}"""))}) " +
                 $@"SELECT {string.Join(", ", chep.Select(x => $@"""{x.Cu}"""))} FROM ""{ten}""");
            Chay($@"DROP TABLE ""{ten}""");
            Chay($@"ALTER TABLE ""{tam}"" RENAME TO ""{ten}""");
            Debug.WriteLine($"[LichXet] Đã nâng cấp {ten} sang lược đồ 6 đợt.");
        }

        // ===================== CẤU HÌNH (csdl2, mỗi chế độ 1 bảng, 1 dòng ID = 1) =====================
        public static string DdlCauHinh(CheDoDoiTuong c) => $@"CREATE TABLE IF NOT EXISTS ""{Module_CheDoDoiTuong.CauHinhLichXet(c)}"" (
    ""ID"" INTEGER NOT NULL, ""ThangBatDau"" INTEGER NOT NULL, ""SoThang"" INTEGER NOT NULL,
    ""KetQuaThangSau"" INTEGER NOT NULL DEFAULT 0, PRIMARY KEY(""ID""));";
        /// <summary>Bảng cấu hình tạo ở bản trước chưa có cột KetQuaThangSau => thêm (lỗi "đã có cột" thì bỏ qua).</summary>
        public static void BoSungCotCauHinh(SqliteConnection cn, SqliteTransaction tran, CheDoDoiTuong c)
        {
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tran;
                cmd.CommandText = $@"SELECT COUNT(*) FROM pragma_table_info('{Module_CheDoDoiTuong.CauHinhLichXet(c)}') WHERE name = 'KetQuaThangSau'";
                if (Convert.ToInt64(cmd.ExecuteScalar()) > 0) return;
                cmd.CommandText = $@"ALTER TABLE ""{Module_CheDoDoiTuong.CauHinhLichXet(c)}"" ADD COLUMN ""KetQuaThangSau"" INTEGER NOT NULL DEFAULT 0";
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { Debug.WriteLine("[LichXet] BoSungCot: " + ex.Message); }
        }

        private static readonly Dictionary<CheDoDoiTuong, CauHinhLich> _boDemCauHinh = new();
        /// <summary>Cấu hình lịch của chế độ. Chưa cài => mặc định (giữ cách xét trước đây).</summary>
        public static CauHinhLich DocCauHinh(CheDoDoiTuong c)
        {
            lock (_boDemCauHinh) if (_boDemCauHinh.TryGetValue(c, out var bd)) return bd;
            var kq = MacDinh(c);
            try
            {
                string db = Module_DanduongGPS.DuongDanCSDL2;
                if (Module_CheDoDoiTuong.LaNhomTuan(c) && !string.IsNullOrWhiteSpace(db) && File.Exists(db))
                {
                    using var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(db));
                    cn.Open();
                    using var cmd = cn.CreateCommand();
                    cmd.CommandText = $@"SELECT ThangBatDau, SoThang, KetQuaThangSau FROM ""{Module_CheDoDoiTuong.CauHinhLichXet(c)}"" WHERE ID = 1";
                    using var r = cmd.ExecuteReader();
                    if (r.Read()) kq = ChuanHoa(new CauHinhLich(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2) != 0), c);
                }
            }
            catch (Exception ex) { Debug.WriteLine("[LichXet] Đọc cấu hình (dùng mặc định): " + ex.Message); }
            lock (_boDemCauHinh) _boDemCauHinh[c] = kq;
            return kq;
        }
        /// <summary>Lưu cấu hình (tạo bảng nếu thiếu), xóa bộ đệm và báo LichThayDoi.</summary>
        public static void LuuCauHinh(CheDoDoiTuong c, CauHinhLich ch)
        {
            if (!Module_CheDoDoiTuong.LaNhomTuan(c)) throw new InvalidOperationException("CBCS không có lịch xét theo tuần.");
            ch = ChuanHoa(ch, c);
            using (var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(Module_DanduongGPS.DuongDanCSDL2, khongTaoMoi: true)))
            {
                cn.Open();
                using var tran = cn.BeginTransaction();
                using (var tao = cn.CreateCommand()) { tao.Transaction = tran; tao.CommandText = DdlCauHinh(c); tao.ExecuteNonQuery(); }
                BoSungCotCauHinh(cn, tran, c);
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tran;
                cmd.CommandText = $@"INSERT INTO ""{Module_CheDoDoiTuong.CauHinhLichXet(c)}"" (ID, ThangBatDau, SoThang, KetQuaThangSau) VALUES (1, @t, @n, @s)
                    ON CONFLICT(ID) DO UPDATE SET ThangBatDau = excluded.ThangBatDau, SoThang = excluded.SoThang, KetQuaThangSau = excluded.KetQuaThangSau;";
                cmd.Parameters.AddWithValue("@t", ch.ThangBatDau);
                cmd.Parameters.AddWithValue("@n", ch.SoThang);
                cmd.Parameters.AddWithValue("@s", ch.KetQuaThangSau ? 1 : 0);
                cmd.ExecuteNonQuery();
                tran.Commit();
            }
            XoaBoDem();
            try { LichThayDoi?.Invoke(); } catch (Exception ex) { Debug.WriteLine("[LichXet] LichThayDoi: " + ex.Message); }
        }
        /// <summary>Báo khi lịch được cài lại (form đang mở dựng lại cột / ô nhập).</summary>
        public static event Action? LichThayDoi;
        private static CauHinhLich ChuanHoa(CauHinhLich ch, CheDoDoiTuong c) => new(
            ch.ThangBatDau is >= 1 and <= 12 ? ch.ThangBatDau : MacDinh(c).ThangBatDau,
            Math.Clamp(ch.SoThang, 1, SoDotToiDa),
            ch.KetQuaThangSau);

        // ===================== LỊCH =====================
        private static (int Nam, long Luc)? _boDemNam;
        private static readonly Dictionary<CheDoDoiTuong, (IReadOnlyList<DotXet> Lich, int Nam)> _boDemLich = new();
        /// <summary>Quên bộ đệm (cài lại lịch, đổi năm hệ thống, khôi phục CSDL).</summary>
        public static void XoaBoDem()
        {
            lock (_boDemCauHinh) _boDemCauHinh.Clear();
            _boDemNam = null;
            lock (_boDemLich) _boDemLich.Clear();
        }
        /// <summary>Năm hệ thống (đọc CSDL tối đa 10 giây một lần).</summary>
        private static int NamHeThong()
        {
            long now = Environment.TickCount64;
            var bd = _boDemNam;
            if (bd.HasValue && now - bd.Value.Luc < 10_000) return bd.Value.Nam;
            int nam;
            try { nam = Module_HeThong.LayNamHeThong(); } catch { nam = DateTime.Now.Year; }
            _boDemNam = (nam, now);
            return nam;
        }

        /// <summary>Dựng lịch từ 1 cấu hình (dùng chung cho LichXet và phần xem trước của Form63).</summary>
        public static IReadOnlyList<DotXet> TaoLich(CauHinhLich ch, int nam)
        {
            var ds = new List<DotXet>(ch.SoThang);
            for (int d = 1; d <= ch.SoThang; d++)
            {
                int chiSo = ch.ThangBatDau - 1 + d - 1;
                int chiSoKq = chiSo + (ch.KetQuaThangSau ? 1 : 0);
                ds.Add(new DotXet
                {
                    So = d,
                    Thang = chiSo % 12 + 1, Nam = nam + chiSo / 12,
                    ThangKetQua = chiSoKq % 12 + 1, NamKetQua = nam + chiSoKq / 12,
                    CotTuan = Enumerable.Range(1, SoTuanMotThang).Select(w => CotTuan(w, d)).ToArray(),
                    CotKetQua = CotKetQuaDot(d)
                });
            }
            return ds.AsReadOnly();
        }
        /// <summary>Các đợt xét đang dùng của chế độ (theo thứ tự thời gian). CBCS: rỗng.</summary>
        public static IReadOnlyList<DotXet> LichXet(CheDoDoiTuong c)
        {
            if (!Module_CheDoDoiTuong.LaNhomTuan(c)) return Array.Empty<DotXet>();
            int nam = NamHeThong();
            lock (_boDemLich)
                if (_boDemLich.TryGetValue(c, out var bd) && bd.Nam == nam) return bd.Lich;
            var lich = TaoLich(DocCauHinh(c), nam);
            lock (_boDemLich) _boDemLich[c] = (lich, nam);
            return lich;
        }
        public static IReadOnlyList<DotXet> LichXet() => LichXet(Module_CheDoDoiTuong.Hien);

        /// <summary>Cột đang dùng theo thứ tự hiển thị: mỗi đợt 4 tuần rồi kết quả.</summary>
        public static string[] CotTheoThuTu(CheDoDoiTuong c) => LichXet(c).SelectMany(d => d.TatCaCot).ToArray();
        /// <summary>
        /// Cột thời gian sẽ hiển thị cho một bảng có sẵn các cột <paramref name="cotCoSan"/> (vd tệp năm cũ), theo thứ tự thời gian.
        /// Tệp năm cũ còn lược đồ cũ (chưa có cột đợt) => hiển thị theo lược đồ cũ (tuần tháng 2..5 -> kết quả tháng 3..6).
        /// </summary>
        public static string[] CotDangDungTrong(CheDoDoiTuong c, ICollection<string> cotCoSan)
        {
            bool Co(string x) => cotCoSan.Contains(x, StringComparer.OrdinalIgnoreCase);
            var kq = CotTheoThuTu(c).Where(Co).ToArray();
            if (kq.Length == 0) kq = _cotCuTheoThuTu.Where(Co).ToArray();
            return kq;
        }
        /// <summary>Các cột kết quả tháng đang dùng (đếm Tổng loại, xuất báo cáo tháng...).</summary>
        public static string[] CotKetQua(CheDoDoiTuong c) => LichXet(c).Select(d => d.CotKetQua).ToArray();
        /// <summary>Tên cột có dạng cột thời gian nhóm tuần (Tuan_..., Thang_...).</summary>
        public static bool LaCotThoiGian(string? ten) =>
            ten != null && (ten.StartsWith("Tuan_", StringComparison.OrdinalIgnoreCase) || ten.StartsWith("Thang_", StringComparison.OrdinalIgnoreCase));
        /// <summary>Cột thời gian có trong lược đồ nhưng không thuộc lịch đang dùng (ẩn khỏi lưới / không cho chọn).</summary>
        public static bool LaCotKhongDung(CheDoDoiTuong c, string? ten) => LaCotThoiGian(ten) && DotCuaCot(c, ten) == null;
        /// <summary>Đợt chứa cột, null nếu không thuộc lịch đang dùng.</summary>
        public static DotXet? DotCuaCot(CheDoDoiTuong c, string? ten) =>
            ten == null ? null : LichXet(c).FirstOrDefault(d => d.TatCaCot.Contains(ten, StringComparer.OrdinalIgnoreCase));

        /// <summary>"Tháng 9" (cùng năm hệ thống) hoặc "Tháng 1/2027" (khóa kéo sang năm sau).</summary>
        public static string NhanThang(int thang, int nam) => NhanThang(thang, nam, NamHeThong());
        public static string NhanThang(int thang, int nam, int namHeThong) => nam == namHeThong ? $"Tháng {thang}" : $"Tháng {thang}/{nam}";
        public static string NhanTuan(DotXet d, int tuan, string ngat = " - ") => $"Tuần {tuan}{ngat}{NhanThang(d.Thang, d.Nam)}";
        public static string NhanKetQua(DotXet d, string ngat = " ") => $"Kết quả{ngat}{NhanThang(d.ThangKetQua, d.NamKetQua)}";

        /// <summary>
        /// Danh sách để người dùng chọn 1 mốc (xuất / xóa / nạp dữ liệu): "Tuần 1 - Tháng 9" ... "Kết quả Tháng 9" -> tên cột.
        /// Thứ tự thời gian, tên hiển thị không trùng.
        /// </summary>
        public static Dictionary<string, string> DanhSachChon(CheDoDoiTuong c)
        {
            var kq = new Dictionary<string, string>();
            foreach (var d in LichXet(c))
            {
                for (int w = 1; w <= d.CotTuan.Length; w++) kq[NhanTuan(d, w)] = d.CotTuan[w - 1];
                kq[NhanKetQua(d)] = d.CotKetQua;
            }
            return kq;
        }

        /// <summary>
        /// Nhãn hiển thị của cột thời gian nhóm tuần. Cột thuộc lịch đang dùng: theo lịch (tháng thật).
        /// Cột ngoài lịch (tệp năm cũ lược đồ cũ, đợt chưa dùng): suy từ tên cột. Không phải cột thời gian: null.
        /// <paramref name="ngat"/> = "\n" cho tiêu đề lưới 2 dòng.
        /// </summary>
        public static string? NhanCot(CheDoDoiTuong c, string? ten, string ngat = "\n")
        {
            if (!LaCotThoiGian(ten)) return null;
            var d = DotCuaCot(c, ten);
            if (d != null)
            {
                int i = Array.FindIndex(d.CotTuan, x => string.Equals(x, ten, StringComparison.OrdinalIgnoreCase));
                return i >= 0 ? NhanTuan(d, i + 1, ngat) : NhanKetQua(d, ngat);
            }
            string[] p = ten!.Split('_');
            if (p.Length == 3 && p[0].Equals("Tuan", StringComparison.OrdinalIgnoreCase))
            {
                if (p[2].StartsWith("T", StringComparison.OrdinalIgnoreCase)) return $"Tuần {p[1]}{ngat}Tháng {p[2].Substring(1)}";
                if (p[2].StartsWith("D", StringComparison.OrdinalIgnoreCase)) return $"Tuần {p[1]}{ngat}Đợt {p[2].Substring(1)}";
            }
            if (p.Length == 2)
            {
                if (p[1].StartsWith("D", StringComparison.OrdinalIgnoreCase)) return $"Kết quả{ngat}Đợt {p[1].Substring(1)}";
                return $"Kết quả{ngat}Tháng {p[1]}";
            }
            return ten;
        }
    }
}
