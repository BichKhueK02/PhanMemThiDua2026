using Krypton.Toolkit;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Phần dùng chung của các trang khen thưởng (Form34, Form49, Form50, Form51):
    ///  1. BỘ LỌC KÉP: mỗi combo chỉ liệt kê giá trị còn xuất hiện trong các dòng thỏa MỌI bộ lọc KHÁC (lọc theo đơn vị thì combo
    ///     hình thức/đơn vị khen chỉ còn những gì đơn vị đó có, và ngược lại) => không bao giờ chọn ra tổ hợp rỗng vô lý.
    ///     Giá trị đang chọn luôn được giữ trong danh sách (không tự đổi lựa chọn của người dùng). Danh sách không đổi thì không nạp lại.
    ///  2. Nút thao tác GỌN: bề ngang cố định theo DPI thay vì kéo giãn theo cả thẻ.
    ///  3. Tín hiệu "kho CSDL năm cũ đã đổi" để Form50 (cá nhân) và Form51 (tập thể) — cùng đọc tệp KhenThuong_CBCS_Nam*.db —
    ///     luôn khớp nhau khi một tệp/bảng năm cũ bị xóa hoặc ghi đè.
    /// </summary>
    internal static class Module_BoLocKhenThuong
    {
        // ===================== 1. BỘ LỌC KÉP =====================
        /// <summary>
        /// Nạp lại Items của combo DropDownList: mục 0 = <paramref name="mucTatCa"/>, sau đó là <paramref name="giaTri"/> theo đúng thứ tự.
        /// Giữ nguyên giá trị đang chọn (thêm vào nếu không còn trong danh sách). Trả về true nếu đã nạp lại.
        /// Người gọi tự chặn sự kiện SelectedIndexChanged (cờ hoặc gỡ handler) trong lúc gọi.
        /// </summary>
        public static bool NapLaiCombo(ComboBox? cb, IReadOnlyList<string> giaTri, string mucTatCa)
        {
            if (cb == null || cb.IsDisposed) return false;
            string dangChon = cb.SelectedIndex > 0 ? (cb.GetItemText(cb.SelectedItem) ?? "") : "";
            var moi = new List<string>(giaTri.Count + 2) { mucTatCa };
            bool coDangChon = dangChon.Length == 0;
            foreach (var g in giaTri)
            {
                moi.Add(g);
                if (!coDangChon && string.Equals(g, dangChon, StringComparison.OrdinalIgnoreCase)) coDangChon = true;
            }
            if (!coDangChon) moi.Add(dangChon);   // giữ lựa chọn của người dùng dù tổ hợp hiện tại không còn dòng nào
            if (GiongNhau(cb, moi)) return false;
            cb.BeginUpdate();
            try
            {
                cb.Items.Clear();
                cb.Items.AddRange(moi.Cast<object>().ToArray());
                int idx = 0;
                if (dangChon.Length > 0)
                    for (int i = 1; i < moi.Count; i++)
                        if (string.Equals(moi[i], dangChon, StringComparison.OrdinalIgnoreCase)) { idx = i; break; }
                cb.SelectedIndex = idx;
            }
            finally { cb.EndUpdate(); }
            return true;
        }
        private static bool GiongNhau(ComboBox cb, List<string> moi)
        {
            if (cb.Items.Count != moi.Count) return false;
            for (int i = 0; i < moi.Count; i++)
                if (!string.Equals(cb.GetItemText(cb.Items[i]), moi[i], StringComparison.Ordinal)) return false;
            return true;
        }
        /// <summary>Giá trị lọc của combo: mục 0 ("Tất cả" / "") = không lọc => "".</summary>
        public static string GiaTriLoc(ComboBox? cb)
        {
            if (cb == null || cb.SelectedIndex <= 0) return "";
            return (cb.GetItemText(cb.SelectedItem) ?? "").Trim();
        }
        /// <summary>Tách từ khóa tìm tên: bỏ dấu, chữ thường, nhiều từ thì từ nào cũng phải có ("nguyen van" khớp "Nguyễn Văn An").</summary>
        public static string[] TachTuKhoa(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return Array.Empty<string>();
            return BoLocDanhSach.ChuanHoaTimKiem(s.Trim()).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        }
        public static bool KhopTuKhoa(string tenDaChuanHoa, string[] tu)
        {
            for (int i = 0; i < tu.Length; i++)
                if (tenDaChuanHoa.IndexOf(tu[i], StringComparison.Ordinal) < 0) return false;
            return true;
        }
        /// <summary>Sắp xếp: đơn vị nằm trong thứ tự chuẩn đứng trước (giữ thứ tự chuẩn), còn lại xếp ABC.</summary>
        public static List<string> SapTheoThuTuChuan(IEnumerable<string> tap, IReadOnlyList<string>? thuTuChuan)
        {
            var con = new HashSet<string>(tap.Where(s => !string.IsNullOrWhiteSpace(s)), StringComparer.OrdinalIgnoreCase);
            var kq = new List<string>(con.Count);
            if (thuTuChuan != null)
                foreach (var dv in thuTuChuan)
                    if (con.Remove(dv)) kq.Add(dv);
            kq.AddRange(con.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase));
            return kq;
        }
        public static List<string> SapABC(IEnumerable<string> tap) => SapTheoThuTuChuan(tap, null);
        // ===================== 2. NÚT GỌN =====================
        /// <summary>Bề ngang chuẩn của nút có chữ + biểu tượng trên các trang khen thưởng (px thiết kế, nhân DPI ở nơi dùng).</summary>
        public const int RongNut = 116;
        public const int CaoNut = 34;
        public static int Px(Control c, int v) => (int)Math.Round(v * c.DeviceDpi / 96.0);
        /// <summary>
        /// Xếp lại khối nút Thêm / Sửa / Xóa (+ 2 nút cỡ chữ phía trên) của Form49/Form51: bản cũ chia 3 cột 33% nên mỗi nút dài
        /// theo cả thẻ. Nay: cột 0 co giãn (khoảng trống), 3 cột nút bề ngang cố định dồn về bên phải.
        /// </summary>
        public static void XepNutThaoTacGon(TableLayoutPanel? t9, Control? them, Control? sua, Control? xoa, Control? giam, Control? tang)
        {
            if (t9 == null) return;
            int w = Px(t9, RongNut + 8);   // + khe giữa 2 nút
            t9.SuspendLayout();
            try
            {
                t9.ColumnCount = 4;
                t9.ColumnStyles.Clear();
                t9.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                for (int i = 0; i < 3; i++) t9.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, w));
                DatO(t9, them, 1, 1);
                DatO(t9, sua, 2, 1);
                DatO(t9, xoa, 3, 1);
                DatO(t9, giam, 2, 0);
                DatO(t9, tang, 3, 0);
                foreach (var b in new[] { them, sua, xoa })
                {
                    if (b == null) continue;
                    b.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                    b.Margin = new Padding(Px(t9, 4), 0, Px(t9, 4), 0);
                    b.Height = Px(t9, CaoNut);
                }
            }
            finally { t9.ResumeLayout(true); }
        }
        private static void DatO(TableLayoutPanel t, Control? c, int cot, int hang)
        {
            if (c == null || c.Parent != t) return;
            t.SetCellPosition(c, new TableLayoutPanelCellPosition(cot, hang));
        }
        /// <summary>Đặt nút có bề ngang cố định (không kéo giãn theo ô).</summary>
        public static void NutBeNgangCoDinh(KryptonButton? b, AnchorStyles neo = AnchorStyles.Left)
        {
            if (b == null) return;
            b.Anchor = neo;
            b.AutoSize = false;
            b.Size = new Size(Px(b, RongNut), Px(b, CaoNut));
        }
        // ===================== 3. KHO CSDL NĂM CŨ =====================
        /// <summary>
        /// Bắn khi một tệp KhenThuong_CBCS_Nam*.db được tạo / ghi đè / xóa dữ liệu (bảng giấy khen hoặc bảng tập thể).
        /// Tham số = đường dẫn tệp bị đổi (rỗng = không rõ, quét lại tất cả). Luôn bắn trên luồng giao diện.
        /// </summary>
        public static event Action<string>? KhoNamCuThayDoi;
        public static void BaoKhoNamCuThayDoi(string duongDan = "")
        {
            try { KhoNamCuThayDoi?.Invoke(duongDan ?? ""); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[Module_BoLocKhenThuong.KhoNamCu] " + ex.Message); }
        }
        /// <summary>
        /// Tệp năm cũ còn bảng <paramref name="tenBang"/> và bảng đó còn ít nhất 1 dòng? Dùng để ẨN năm khỏi combo chọn CSDL khi dữ liệu
        /// của trang đó đã bị xóa hết: Form50 xét "ThongKe_GiayKhen", Form51 xét "ThongKe_KhenThuongTapThe". Tệp lỗi / khóa = không có.
        /// </summary>
        public static bool CoDuLieuBang(string? duongDan, string tenBang)
        {
            if (string.IsNullOrEmpty(duongDan) || !File.Exists(duongDan)) return false;
            try
            {
                using var conn = new Microsoft.Data.Sqlite.SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(duongDan, chiDoc: true));
                conn.Open();
                using var kt = conn.CreateCommand();
                kt.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$t LIMIT 1";
                kt.Parameters.AddWithValue("$t", tenBang);
                if (kt.ExecuteScalar() == null) return false;
                using var dem = conn.CreateCommand();
                dem.CommandText = $"SELECT EXISTS(SELECT 1 FROM \"{tenBang}\" LIMIT 1)";   // dừng ở dòng đầu, không đếm cả bảng
                return Convert.ToInt64(dem.ExecuteScalar()) == 1;
            }
            catch { return false; }
        }
        /// <summary>Dấu vết tệp (thời điểm ghi + kích thước, gồm cả tệp -wal): đổi = phải đọc lại, không đổi = dùng dữ liệu đang có trên RAM.</summary>
        public static string DauVetTep(string? duongDan)
        {
            if (string.IsNullOrEmpty(duongDan)) return "";
            try
            {
                var fi = new FileInfo(duongDan);
                if (!fi.Exists) return "";
                var wal = new FileInfo(duongDan + "-wal");
                return $"{fi.LastWriteTimeUtc.Ticks}:{fi.Length}:{(wal.Exists ? wal.LastWriteTimeUtc.Ticks + ":" + wal.Length : "-")}";
            }
            catch { return ""; }
        }
    }
}
