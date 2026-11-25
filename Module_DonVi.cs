using Microsoft.Data.Sqlite;
using System.Globalization;
namespace PhanMemThiDua2026
{
    internal static class Module_DonVi
    {
        //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
        private static List<string>? _cacheDonVi;
        // Bộ đệm nạp từ bảng nào / CSDL nào: đổi phiên bản (DanhSach_DonVi_CBCS <-> DanhSach_DonVi_TanBinh) hoặc đổi CSDL thì tự đọc lại.
        // (Trước đây đổi CBCS -> Tân binh vẫn trả đơn vị CBCS => Form26/Form15/Form4 không khớp đơn vị nào.)
        private static string _cacheKhoa = "";
        // Thứ tự đơn vị đã khai báo (theo ID trong bảng đơn vị của chế độ đang chạy), tra theo chuỗi tên nguyên văn.
        // Dựng lại mỗi khi nạp lại danh sách; đọc từ luồng nền an toàn (đối tượng không đổi sau khi tạo).
        private static volatile BangThuTuDonVi _bangThuTu = BangThuTuDonVi.Rong;
        // ⭐ HÀM MỚI: TỰ ĐỘNG NHẬN DIỆN BẢNG THEO PHIÊN BẢN PHẦN MỀM
        public static string LayTenBangDonVi()
        {
            try { return Module_CheDoDoiTuong.BangDonVi; }   // DanhSach_DonVi_CBCS / _TanBinh / _HocVien
            catch { return Module_CheDoDoiTuong.DonVi(CheDoDoiTuong.CBCS); }
        }
        // LẤY DANH SÁCH ĐƠN VỊ (CHO COMBOBOX / SORT / BIẾN) — đúng thứ tự cán bộ đã khai báo (ID tăng dần)
        public static List<string> GetDanhSachDonVi(bool forceReload = false)
        {
            string csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
            string tenBang = LayTenBangDonVi(); // Gọi hàm tự nhận diện bảng
            string khoa = tenBang + "|" + csdl2Path;
            var cache = _cacheDonVi;
            if (cache != null && !forceReload && khoa == _cacheKhoa)
                return new List<string>(cache);
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(csdl2Path)) return result;
            try
            {
                using var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(csdl2Path));
                cn.Open();
                using var cmd = cn.CreateCommand();
                cmd.CommandText = $"SELECT Ten_DonVi FROM {tenBang} ORDER BY ID ASC";
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    string val = rd["Ten_DonVi"]?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        try
                        {
                            val = Module_BaoMatAES.GiaiMa(val); // giải mã nếu cần
                        }
                        catch { }
                        val = (val ?? "").Trim();
                        if (val.Length > 0) result.Add(val);
                    }
                }
                _cacheKhoa = khoa;
                _cacheDonVi = result;
            }
            catch
            {
                _cacheKhoa = "";   // lỗi đọc: lần sau thử lại
                _cacheDonVi = new List<string>();
            }
            _bangThuTu = new BangThuTuDonVi(_cacheDonVi);
            return new List<string>(_cacheDonVi);
        }
        // Trả về mảng string để dùng toàn hệ thống
        public static string[] LayDanhSachDonViUuTienArray()
        {
            // 🔥 Luôn load mới từ CSDL để chắc chắn giá trị cập nhật
            Reload();
            return GetDanhSachDonVi().ToArray();
        }
        // Xóa cache để reload nếu cần
        public static void Reload()
        {
            _cacheDonVi = null;
        }
        // HÀM KHOI TAO (giữ tương thích code cũ)
        public static void KhoiTao()
        {
            GetDanhSachDonVi();
        }

        // =====================================================================================================
        //  SO KHỚP + SẮP XẾP ĐƠN VỊ DÙNG CHUNG
        //  Tên đơn vị là tên theo quy định (viết liền, tiếng Việt có khoảng trắng, ký tự đặc biệt...) => coi là CHUỖI NGUYÊN VĂN,
        //  KHÔNG chuẩn hóa / đổi hoa thường / bỏ ký tự. Chỉ cắt khoảng trắng hai đầu (như mọi chỗ đọc CSDL/Excel vẫn làm).
        //  Thứ tự = mảng đơn vị đã khai báo (ID tăng dần trong bảng đơn vị của chế độ đang chạy).
        // =====================================================================================================

        /// <summary>Giá trị dùng để so khớp đơn vị: chính chuỗi tên, chỉ cắt khoảng trắng hai đầu.</summary>
        public static string GiaTriSoKhop(string? ten) => (ten ?? "").Trim();

        /// <summary>Khóa sắp xếp/lọc của Form15, Form34: chính chuỗi tên (đã Trim).</summary>
        public static string KhoaSapXep(string? ten) => (ten ?? "").Trim();

        /// <summary>Hai tên có phải cùng một đơn vị không: so sánh chuỗi CHÍNH XÁC (đã Trim).</summary>
        public static bool CungDonVi(string? a, string? b) => string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.Ordinal);

        /// <summary>Đơn vị "BCH" (Ban chỉ huy) — giữ đúng cách kiểm tra cũ (không phân biệt hoa/thường).</summary>
        public static bool LaBCH(string? ten) => string.Equals((ten ?? "").Trim(), "BCH", StringComparison.OrdinalIgnoreCase);

        /// <summary>So sánh bằng CHÍNH XÁC (đã Trim) — dùng cho HashSet/Dictionary tên đơn vị.</summary>
        public static readonly IEqualityComparer<string> SoSanhBang = new SoSanhBangDonVi();
        private sealed class SoSanhBangDonVi : IEqualityComparer<string>
        {
            public bool Equals(string? x, string? y) => CungDonVi(x, y);
            public int GetHashCode(string obj) => StringComparer.Ordinal.GetHashCode((obj ?? "").Trim());
        }

        /// <summary>
        /// Thứ tự cho đơn vị CHƯA khai báo (đơn vị đã khai báo luôn theo mảng khai báo): số so theo giá trị
        /// ("Trung đội 2" trước "Trung đội 10"). Chỉ để xếp chỗ, không đổi tên.
        /// </summary>
        public static readonly IComparer<string> SoSanhTuNhien = new SoSanhTuNhienDonVi();
        private sealed class SoSanhTuNhienDonVi : IComparer<string>
        {
            private static readonly CompareInfo Vi = CultureInfo.GetCultureInfo("vi-VN").CompareInfo;
            public int Compare(string? x, string? y)
            {
                string a = x ?? "", b = y ?? "";
                int i = 0, j = 0;
                while (i < a.Length && j < b.Length)
                {
                    if (char.IsAsciiDigit(a[i]) && char.IsAsciiDigit(b[j]))
                    {
                        int si = i, sj = j;
                        while (i < a.Length && char.IsAsciiDigit(a[i])) i++;
                        while (j < b.Length && char.IsAsciiDigit(b[j])) j++;
                        string na = a.Substring(si, i - si).TrimStart('0'), nb = b.Substring(sj, j - sj).TrimStart('0');
                        if (na.Length != nb.Length) return na.Length.CompareTo(nb.Length);
                        int ss = string.CompareOrdinal(na, nb);
                        if (ss != 0) return ss;
                    }
                    else
                    {
                        int si = i, sj = j;
                        while (i < a.Length && !char.IsAsciiDigit(a[i])) i++;
                        while (j < b.Length && !char.IsAsciiDigit(b[j])) j++;
                        int ss = Vi.Compare(a, si, i - si, b, sj, j - sj, CompareOptions.None);
                        if (ss != 0) return ss;
                    }
                }
                int kq = (a.Length - i).CompareTo(b.Length - j);
                return kq != 0 ? kq : string.CompareOrdinal(a, b);
            }
        }

        /// <summary>Bảng tra thứ tự dựng từ MẢNG đơn vị đã khai báo (tên trùng: giữ vị trí đầu tiên).</summary>
        public sealed class BangThuTuDonVi
        {
            internal static readonly BangThuTuDonVi Rong = new(Array.Empty<string>());
            private readonly Dictionary<string, int> _viTri = new(StringComparer.Ordinal);
            private readonly Dictionary<string, string> _tenChuan = new(StringComparer.Ordinal);
            public IReadOnlyList<string> DanhSach { get; }
            public BangThuTuDonVi(IEnumerable<string>? thuTu)
            {
                var ds = new List<string>();
                if (thuTu != null)
                    foreach (var dv in thuTu)
                    {
                        string k = GiaTriSoKhop(dv);
                        if (k.Length == 0 || _viTri.ContainsKey(k)) continue;
                        _viTri[k] = ds.Count;
                        _tenChuan[k] = dv.Trim();
                        ds.Add(dv.Trim());
                    }
                DanhSach = ds;
            }
            /// <summary>Vị trí theo thứ tự đã khai báo; không có => int.MaxValue (xếp cuối).</summary>
            public int ThuTu(string? donVi) => _viTri.TryGetValue(GiaTriSoKhop(donVi), out int p) ? p : int.MaxValue;
            public bool CoKhaiBao(string? donVi) => _viTri.ContainsKey(GiaTriSoKhop(donVi));
            /// <summary>Tên đúng như đã khai báo nếu khớp khóa; không khớp thì trả lại tên gốc (đã Trim).</summary>
            public string TenChuan(string? donVi)
            {
                string goc = (donVi ?? "").Trim();
                return _tenChuan.TryGetValue(GiaTriSoKhop(goc), out var t) ? t : goc;
            }
            /// <summary>
            /// Bỏ trùng (chuỗi chính xác), đơn vị đã khai báo đứng trước (đúng thứ tự khai báo, hiện bằng tên đã khai báo),
            /// đơn vị lạ xếp sau theo thứ tự tự nhiên.
            /// </summary>
            public List<string> SapXep(IEnumerable<string?> tap)
            {
                var daGap = new HashSet<string>(StringComparer.Ordinal);
                var coKhaiBao = new List<(int P, string Ten)>();
                var la = new List<string>();
                foreach (var s in tap)
                {
                    string goc = (s ?? "").Trim();
                    string k = GiaTriSoKhop(goc);
                    if (k.Length == 0 || !daGap.Add(k)) continue;
                    if (_viTri.TryGetValue(k, out int p)) coKhaiBao.Add((p, _tenChuan[k]));
                    else la.Add(goc);
                }
                coKhaiBao.Sort((x, y) => x.P.CompareTo(y.P));
                la.Sort(SoSanhTuNhien);
                var kq = new List<string>(coKhaiBao.Count + la.Count);
                foreach (var x in coKhaiBao) kq.Add(x.Ten);
                kq.AddRange(la);
                return kq;
            }
        }

        /// <summary>Bảng thứ tự theo danh sách đơn vị đã khai báo của chế độ đang chạy.</summary>
        public static BangThuTuDonVi LayBangThuTu()
        {
            GetDanhSachDonVi();   // bộ đệm còn hạn thì không đọc CSDL
            return _bangThuTu;
        }
        /// <summary>Bảng thứ tự từ một danh sách có sẵn (ảnh chụp lấy ở luồng UI rồi dùng ở luồng nền).</summary>
        public static BangThuTuDonVi TaoBangThuTu(IEnumerable<string>? thuTu) => new(thuTu);

        /// <summary>Vị trí của đơn vị theo thứ tự đã khai báo; không có => int.MaxValue.</summary>
        public static int LayThuTu(string? donVi) => LayBangThuTu().ThuTu(donVi);

        /// <summary>Tên đúng như đã khai báo (nếu khớp), dùng khi ghi dữ liệu mới để không đẻ thêm biến thể.</summary>
        public static string LayTenChuan(string? donVi) => LayBangThuTu().TenChuan(donVi);

        /// <summary>Sắp xếp tập đơn vị: đã khai báo trước (đúng thứ tự cán bộ quy định), lạ sau (tự nhiên).</summary>
        public static List<string> SapXep(IEnumerable<string?> tap, IEnumerable<string>? thuTu = null)
            => (thuTu == null ? LayBangThuTu() : new BangThuTuDonVi(thuTu)).SapXep(tap);

        /// <summary>Điều kiện DataView.RowFilter chọn đúng một đơn vị (chuỗi nguyên văn; chỉ thoát dấu nháy đơn).</summary>
        public static string DieuKienRowFilter(System.Data.DataTable? dt, string cot, string donVi)
            => "[" + cot.Replace("]", @"\]") + "] = '" + donVi.Trim().Replace("'", "''") + "'";
    }
}
