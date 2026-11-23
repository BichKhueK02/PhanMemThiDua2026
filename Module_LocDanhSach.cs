// Bộ lọc danh sách CBCS cho Form6 (chạy trên mảng chuỗi đã chuẩn hóa sẵn, KHÔNG dùng DataView.RowFilter).
//
// Vì sao nhanh:
//  - Chuẩn hóa (bỏ dấu, chữ thường) chỉ làm 1 lần khi nạp dữ liệu (ở luồng nền), không làm lại ở mỗi lần lọc.
//  - Mỗi lần lọc chỉ là 1 vòng for trên mảng chuỗi (so sánh Ordinal) -> ~vài mili-giây cho hàng chục nghìn dòng,
//    thay cho trình phân tích biểu thức của DataTable (Convert + LIKE '%..%' so sánh theo văn hóa) phải dựng lại chỉ mục.
//  - Kết quả là mảng chỉ số dòng (int[]) -> lưới ảo đọc thẳng DataTable.Rows[chỉ số], không cần DataView.
//
// Ngữ nghĩa giữ nguyên bản cũ: Họ tên chứa từ khóa; Đơn vị bằng đúng; Phân loại bằng/chứa; "Không PL" gồm rỗng.
// Nâng cấp: tìm tên KHÔNG phân biệt dấu/hoa thường ("nguyen van a" khớp "Nguyễn Văn A"), nhiều từ => mỗi từ đều phải có mặt;
// gõ số hiệu cũng ra; tùy chọn gom tập đơn vị còn người thỏa các điều kiện khác (bộ lọc kép cho combo Đơn vị).
using System.Data;
using System.Globalization;
using System.Text;
namespace PhanMemThiDua2026
{
    internal sealed class BoLocDanhSach
    {
        private readonly string[] _ten;       // Họ tên đã bỏ dấu + chữ thường (chỉ để tìm kiếm)
        private readonly string[] _donVi;     // Đơn vị (đã Trim)
        private readonly string[] _phanLoai;  // Phân loại (đã Trim)
        private readonly bool[] _dongAo;      // Họ tên rỗng = dòng đệm ảo cuối bảng
        private readonly int _oHoTen, _oDonVi, _oPhanLoai, _oSoHieu;
        private BoLocDanhSach(int n, int oH, int oD, int oP, int oS)
        {
            _ten = new string[n];
            _donVi = new string[n];
            _phanLoai = new string[n];
            _dongAo = new bool[n];
            _oHoTen = oH; _oDonVi = oD; _oPhanLoai = oP; _oSoHieu = oS;
        }
        public int SoDong => _ten.Length;
        /// <summary>Dựng chỉ mục từ bảng (an toàn gọi ở luồng nền khi bảng chưa dùng chung).</summary>
        public static BoLocDanhSach TaoTu(DataTable dt)
        {
            int n = dt.Rows.Count;
            var bo = new BoLocDanhSach(n, dt.Columns.IndexOf("HoVaTen"), dt.Columns.IndexOf("DonVi"), dt.Columns.IndexOf("PhanLoai"),
                                       dt.Columns.IndexOf("SoHieu"));
            for (int i = 0; i < n; i++) bo.GhiDong(dt.Rows[i], i);
            return bo;
        }
        /// <summary>Làm tươi 1 dòng sau khi sửa tại chỗ (không dựng lại cả bảng).</summary>
        public void CapNhatDong(DataTable dt, int viTri)
        {
            if ((uint)viTri >= (uint)_ten.Length || viTri >= dt.Rows.Count) return;
            GhiDong(dt.Rows[viTri], viTri);
        }
        private void GhiDong(DataRow r, int i)
        {
            string ten = Doc(r, _oHoTen);
            _dongAo[i] = ten.Length == 0 || string.IsNullOrWhiteSpace(ten);
            // "họ tên|số hiệu" đã bỏ dấu: ô tìm kiếm gõ tên hay số hiệu đều ra ("|" chặn khớp vắt ngang 2 trường)
            string soHieu = Doc(r, _oSoHieu).Trim();
            _ten[i] = soHieu.Length == 0 ? ChuanHoaTimKiem(ten) : ChuanHoaTimKiem(ten) + "|" + ChuanHoaTimKiem(soHieu);
            _donVi[i] = Doc(r, _oDonVi).Trim();
            _phanLoai[i] = Doc(r, _oPhanLoai).Trim();
        }
        private static string Doc(DataRow r, int ord)
        {
            if (ord < 0) return string.Empty;
            object v = r[ord];
            if (v == DBNull.Value || v == null) return string.Empty;
            return v as string ?? Convert.ToString(v, CultureInfo.InvariantCulture) ?? string.Empty;
        }
        /// <summary>Bỏ dấu tiếng Việt, đ->d, chữ thường. Chuỗi toàn ASCII đi đường tắt.</summary>
        public static string ChuanHoaTimKiem(string? s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            bool ascii = true;
            foreach (char c in s) { if (c > 127) { ascii = false; break; } }
            if (ascii) return s.ToLowerInvariant();
            string d = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(d.Length);
            foreach (char c in d)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                if (c == 'đ' || c == 'Đ') { sb.Append('d'); continue; }
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }
        /// <summary>
        /// Trả về danh sách chỉ số dòng (tăng dần) khớp mọi điều kiện, hoặc null nếu KHÔNG có điều kiện nào (= mọi dòng).
        /// Khi có điều kiện, dòng đệm ảo (họ tên rỗng) luôn bị loại.
        /// </summary>
        /// <param name="tuKhoaTen">Từ khóa tìm theo tên (rỗng = bỏ qua).</param>
        /// <param name="donVi">Đơn vị cần khớp đúng (null/rỗng = bỏ qua).</param>
        /// <param name="phanLoai">Giá trị phân loại trong CSDL cần khớp (null/rỗng = bỏ qua).</param>
        /// <param name="laKhongPL">true = lọc người chưa phân loại (rỗng hoặc nằm trong tenKhongPL).</param>
        /// <param name="conDonVi">BỘ LỌC KÉP (tùy chọn): nhận các đơn vị còn người thỏa tên + phân loại (bỏ qua điều kiện đơn vị).</param>
        public int[]? Loc(string? tuKhoaTen, string? donVi, string? phanLoai, bool laKhongPL, string[] tenKhongPL,
                          HashSet<string>? conDonVi = null)
        {
            string[] tu = TachTu(tuKhoaTen);
            string dv = (donVi ?? string.Empty).Trim();
            string pl = (phanLoai ?? string.Empty).Trim();
            bool coTen = tu.Length > 0, coDv = dv.Length > 0, coPl = laKhongPL || pl.Length > 0;
            if (!coTen && !coDv && !coPl && conDonVi == null) return null;
            int n = _ten.Length;
            var kq = new int[n];
            int dem = 0;
            for (int i = 0; i < n; i++)
            {
                if (_dongAo[i]) continue;
                bool okDv = !coDv || string.Equals(_donVi[i], dv, StringComparison.OrdinalIgnoreCase);
                if (!okDv && conDonVi == null) continue;
                if (coPl)
                {
                    string p = _phanLoai[i];
                    if (laKhongPL)
                    {
                        if (p.Length != 0 && !TrongDanhSach(p, tenKhongPL)) continue;
                    }
                    else if (!string.Equals(p, pl, StringComparison.OrdinalIgnoreCase)
                             && p.IndexOf(pl, StringComparison.OrdinalIgnoreCase) < 0) continue;
                }
                if (coTen)
                {
                    string ten = _ten[i];
                    bool khop = true;
                    for (int t = 0; t < tu.Length; t++)
                    {
                        if (ten.IndexOf(tu[t], StringComparison.Ordinal) < 0) { khop = false; break; }
                    }
                    if (!khop) continue;
                }
                if (conDonVi != null && _donVi[i].Length > 0) conDonVi.Add(_donVi[i]);
                if (!okDv) continue;
                kq[dem++] = i;
            }
            if (!coTen && !coDv && !coPl) return null;   // chỉ đi gom đơn vị: không có điều kiện nào => mọi dòng
            if (dem == n) return null;     // lọc xong vẫn đủ mọi dòng (hiếm) -> dùng đường nhanh không ánh xạ
            return kq.AsSpan(0, dem).ToArray();
        }
        private static bool TrongDanhSach(string p, string[] ds)
        {
            for (int k = 0; k < ds.Length; k++)
                if (string.Equals(p, ds[k], StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
        private static string[] TachTu(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return Array.Empty<string>();
            return ChuanHoaTimKiem(s).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        }
        /// <summary>2 kết quả lọc có cùng tập dòng không? (null = mọi dòng của bảng có tong dòng).</summary>
        public static bool CungTap(int[]? a, int[]? b, int tongDong)
        {
            int la = a?.Length ?? tongDong, lb = b?.Length ?? tongDong;
            if (la != lb) return false;
            if (a == null || b == null) return true; // 1 bên là "mọi dòng", cùng độ dài => trùng nhau
            return a.AsSpan().SequenceEqual(b);
        }
        /// <summary>Vị trí (trong kết quả lọc) của dòng nguồn có chỉ số nguon; -1 nếu bị lọc mất.</summary>
        public static int ViTriCuaNguon(int[]? hienThi, int nguon, int tongDong)
        {
            if (hienThi == null) return (uint)nguon < (uint)tongDong ? nguon : -1;
            int k = Array.BinarySearch(hienThi, nguon);
            return k >= 0 ? k : -1;
        }
    }
}