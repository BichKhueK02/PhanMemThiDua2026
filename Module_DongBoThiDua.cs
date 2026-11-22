///Hổ trợ dịch vụ chuyển dữ liệu tù form6 sang form15 thông qua form17 (xuất dữ liệu sang thống kê) và ngược lại.
///
// Quy tắc "cùng một người" dùng CHUNG cho Form15 (Đồng bộ CSDL2 -> CSDL4) và Form17 (Xuất dữ liệu sang thống kê).
// Trước đây mỗi nơi tự ghép theo một khóa khác nhau (Form15: chỉ Số hiệu và GỘP người trùng; Form17: Họ tên + Số hiệu
// nhưng bảng tra chỉ dựng 1 lần) nên 2 chức năng "đạp chân nhau": thiếu người hoặc tạo thêm dòng trùng.
using System.Text;
namespace PhanMemThiDua2026
{
    internal static class Module_DongBoThiDua
    {
        /// <summary>Một người ở dạng BẢN RÕ (đã giải mã).</summary>
        public readonly record struct Nguoi(string Ht, string Sh, string Dv);
        /// <summary>Giải mã không bao giờ ném lỗi. GiaiMa trả "" khi không giải được (dữ liệu cũ chưa mã hóa) => dùng lại chuỗi gốc.</summary>
        public static string GiaiMaAnToan(string? giaTri)
        {
            if (string.IsNullOrWhiteSpace(giaTri)) return string.Empty;
            try
            {
                string kq = Module_BaoMatAES.GiaiMa(giaTri);
                return string.IsNullOrEmpty(kq) ? giaTri.Trim() : kq.Trim();
            }
            catch { return giaTri.Trim(); }
        }
        /// <summary>Dòng không có cả Họ tên lẫn Số hiệu thì bỏ qua (giữ đúng luật cũ của Form15).</summary>
        public static bool BoQua(string ht, string sh) => string.IsNullOrWhiteSpace(ht) && string.IsNullOrWhiteSpace(sh);
        // Chuẩn hóa CHỈ để so sánh: Unicode NFC, bỏ khoảng trắng thừa/lạ (NBSP, zero-width...), không phân biệt hoa/thường.
        private static string ChuanHoa(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            string t = s.Normalize(NormalizationForm.FormC);
            var sb = new StringBuilder(t.Length);
            bool cach = false;
            foreach (char c in t)
            {
                if (char.IsWhiteSpace(c) || c == '​' || c == '‌' || c == '‍' || c == '﻿')
                {
                    cach = sb.Length > 0;
                    continue;
                }
                if (cach) { sb.Append(' '); cach = false; }
                sb.Append(c);
            }
            return sb.ToString().ToUpperInvariant();
        }
        /// <summary>Có Số hiệu thì Số hiệu là khóa; chưa có (tân binh mới) thì dùng Họ tên + Đơn vị.</summary>
        public static string TaoKey(string ht, string sh, string dv)
        {
            string s = ChuanHoa(sh);
            return s.Length > 0 ? "SH|" + s : "TEN|" + ChuanHoa(ht) + "|" + ChuanHoa(dv);
        }
        /// <summary>
        /// Ghép MỘT-MỘT: mỗi dòng nguồn (CSDL2) nhận tối đa 1 dòng đích (CSDL4) và mỗi dòng đích chỉ được nhận 1 lần.
        /// Trả mảng cùng độ dài nguồn: chỉ số dòng đích, hoặc -1 nếu đích chưa có (cần thêm mới).
        /// <paramref name="dichDaGhep"/>[j] = true nếu dòng đích j đã có nguồn (false => dòng đích "mồ côi").
        /// Nhiều người trùng Số hiệu: ưu tiên ghép cùng Họ tên; còn dư thì ghép theo thứ tự xuất hiện (ID tăng dần).
        /// => 6.000 dòng nguồn luôn ra đúng 6.000 dòng đích, chạy bao nhiêu lần cũng không sinh thêm dòng.
        /// </summary>
        public static int[] GhepMotMot(IReadOnlyList<Nguoi> nguon, IReadOnlyList<Nguoi> dich, out bool[] dichDaGhep)
        {
            var theoKey = new Dictionary<string, List<int>>(dich.Count, StringComparer.Ordinal);
            var tenDich = new string[dich.Count];
            for (int j = 0; j < dich.Count; j++)
            {
                var d = dich[j];
                tenDich[j] = ChuanHoa(d.Ht);
                string k = TaoKey(d.Ht, d.Sh, d.Dv);
                if (!theoKey.TryGetValue(k, out var ds)) theoKey[k] = ds = new List<int>(1);
                ds.Add(j);
            }
            var ketQua = new int[nguon.Count];
            var daDung = new bool[dich.Count];
            var keyNguon = new string[nguon.Count];
            var luot2 = new List<int>();
            // Lượt 1: cùng khóa + cùng Họ tên
            for (int i = 0; i < nguon.Count; i++)
            {
                ketQua[i] = -1;
                var n = nguon[i];
                string k = keyNguon[i] = TaoKey(n.Ht, n.Sh, n.Dv);
                int tim = -1;
                if (theoKey.TryGetValue(k, out var ds))
                {
                    string ten = ChuanHoa(n.Ht);
                    foreach (int j in ds)
                        if (!daDung[j] && tenDich[j] == ten) { tim = j; break; }
                }
                if (tim >= 0) { ketQua[i] = tim; daDung[tim] = true; }
                else luot2.Add(i);
            }
            // Lượt 2: cùng khóa nhưng khác Họ tên (đổi tên / trùng Số hiệu) => lấy dòng đích còn rảnh đầu tiên
            foreach (int i in luot2)
            {
                if (!theoKey.TryGetValue(keyNguon[i], out var ds)) continue;
                foreach (int j in ds)
                    if (!daDung[j]) { ketQua[i] = j; daDung[j] = true; break; }
            }
            dichDaGhep = daDung;
            return ketQua;
        }
    }
}