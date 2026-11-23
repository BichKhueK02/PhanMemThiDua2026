using Krypton.Toolkit;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Phần dùng chung của các trang khen thưởng (Form34, Form49, Form50, Form51) — riêng BỘ LỌC KÉP nay dùng cho mọi trang có
    /// nhiều ô lọc (Form6, 10, 15, 29, 36, 42, 44, 46, 60): lõi <see cref="LocKep{T}"/> + cột ẩn tìm kiếm không dấu cho lưới RowFilter.
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
        /// <summary>Một combo tham gia BỘ LỌC KÉP: giá trị đang chọn ("" = không lọc) + cách đọc giá trị của 1 dòng.</summary>
        public sealed class TieuChiKep<T>
        {
            public TieuChiKep(string giaTri, Func<T, string?> lay, Func<string, bool>? hopLe = null)
            {
                GiaTri = (giaTri ?? "").Trim();
                Lay = lay;
                HopLe = hopLe;
            }
            public string GiaTri { get; }
            public Func<T, string?> Lay { get; }
            public Func<string, bool>? HopLe { get; }
            /// <summary>Kết quả: các giá trị còn xuất hiện trong những dòng thỏa MỌI bộ lọc KHÁC (danh sách mới của combo này).</summary>
            public HashSet<string> Con { get; } = new(StringComparer.OrdinalIgnoreCase);
            internal bool Khop(string v) => GiaTri.Length == 0 || string.Equals(v, GiaTri, StringComparison.OrdinalIgnoreCase);
        }
        /// <summary>
        /// Lọc + dựng BỘ LỌC KÉP trong 1 lượt duyệt: trả về chỉ số các dòng thỏa <paramref name="dieuKienChung"/> (tên, tình trạng...)
        /// và MỌI combo; đồng thời mỗi <see cref="TieuChiKep{T}.Con"/> nhận các giá trị còn có trong dòng thỏa mọi điều kiện còn lại.
        /// Dòng trượt đúng 1 combo chỉ góp giá trị cho combo đó; trượt từ 2 combo trở lên thì bỏ hẳn. Giá trị so khớp chính xác
        /// (không phân biệt hoa/thường, đã Trim): chọn "Sư đoàn" không ra lẫn "Sư đoàn 10".
        /// </summary>
        public static List<int> LocKep<T>(IReadOnlyList<T> ds, Func<T, bool>? dieuKienChung, params TieuChiKep<T>[] tieuChi)
        {
            int n = ds.Count, m = tieuChi.Length;
            var kq = new List<int>(n);
            var giaTri = new string[m];
            for (int i = 0; i < n; i++)
            {
                T x = ds[i];
                if (dieuKienChung != null && !dieuKienChung(x)) continue;
                int truot = -1, soTruot = 0;
                for (int k = 0; k < m; k++)
                {
                    string v = (tieuChi[k].Lay(x) ?? "").Trim();
                    giaTri[k] = v;
                    if (!tieuChi[k].Khop(v)) { soTruot++; truot = k; if (soTruot > 1) break; }
                }
                if (soTruot > 1) continue;
                for (int k = 0; k < m; k++)
                {
                    if (soTruot == 1 && k != truot) continue;
                    var tc = tieuChi[k];
                    string v = giaTri[k];
                    if (v.Length > 0 && (tc.HopLe == null || tc.HopLe(v))) tc.Con.Add(v);
                }
                if (soTruot == 0) kq.Add(i);
            }
            return kq;
        }
        // ---------- Dùng cho các lưới gắn thẳng DataTable + DataView.RowFilter (Form36, Form42, Form44, Form60) ----------
        /// <summary>Cột ẩn chứa chuỗi tìm kiếm đã bỏ dấu + chữ thường (ColumnMapping = Hidden, lưới đặt AutoGenerateColumns = false).</summary>
        public const string COT_TIM_KIEM = "__TimKiem";
        private const string KHOA_MOC_TIM_KIEM = "__TimKiem_Nguon";
        /// <summary>
        /// Bảo đảm bảng có cột <see cref="COT_TIM_KIEM"/> = ghép các cột nguồn (bỏ dấu, chữ thường, cách nhau "|") và điền cho mọi dòng.
        /// Gắn 1 lần cho mỗi bảng: sửa ô nguồn / thêm dòng mới thì cột tìm kiếm tự cập nhật theo. Dòng đang Unchanged vẫn giữ Unchanged.
        /// Gọi TRƯỚC khi gắn bảng vào lưới (luồng nền được, khi bảng chưa dùng chung) để không bắn sự kiện đổi dòng lên lưới.
        /// </summary>
        public static void DamBaoCotTimKiem(System.Data.DataTable dt, params string[] cotNguon)
        {
            if (dt == null) return;
            if (!dt.Columns.Contains(COT_TIM_KIEM))
                dt.Columns.Add(new System.Data.DataColumn(COT_TIM_KIEM, typeof(string))
                { ColumnMapping = System.Data.MappingType.Hidden, DefaultValue = "" });
            int oTim = dt.Columns.IndexOf(COT_TIM_KIEM);
            int[] nguon = cotNguon.Select(c => dt.Columns.IndexOf(c)).Where(i => i >= 0).ToArray();
            foreach (System.Data.DataRow r in dt.Rows)
            {
                if (r.RowState == System.Data.DataRowState.Deleted || r.RowState == System.Data.DataRowState.Detached) continue;
                string v = ChuoiTimKiem(r, nguon);
                if (r[oTim] is string cu && string.Equals(cu, v, StringComparison.Ordinal)) continue;
                bool giuNguyen = r.RowState == System.Data.DataRowState.Unchanged;
                r[oTim] = v;
                if (giuNguyen) r.AcceptChanges();
            }
            if (dt.ExtendedProperties.ContainsKey(KHOA_MOC_TIM_KIEM)) { dt.ExtendedProperties[KHOA_MOC_TIM_KIEM] = nguon; return; }
            dt.ExtendedProperties[KHOA_MOC_TIM_KIEM] = nguon;
            dt.ColumnChanged += (s, e) =>
            {
                if (s is not System.Data.DataTable b || e.Column == null || e.Column.ColumnName == COT_TIM_KIEM) return;
                if (b.ExtendedProperties[KHOA_MOC_TIM_KIEM] is not int[] ng || Array.IndexOf(ng, e.Column.Ordinal) < 0) return;
                GhiChuoiTimKiem(e.Row, ng);
            };
            dt.RowChanged += (s, e) =>
            {
                if (e.Action != System.Data.DataRowAction.Add || s is not System.Data.DataTable b) return;
                if (b.ExtendedProperties[KHOA_MOC_TIM_KIEM] is int[] ng) GhiChuoiTimKiem(e.Row, ng);
            };
        }
        private static void GhiChuoiTimKiem(System.Data.DataRow r, int[] nguon)
        {
            if (r.RowState == System.Data.DataRowState.Deleted || r.RowState == System.Data.DataRowState.Detached) return;
            string v = ChuoiTimKiem(r, nguon);
            if (r[COT_TIM_KIEM] is string cu && string.Equals(cu, v, StringComparison.Ordinal)) return;
            r[COT_TIM_KIEM] = v;
        }
        private static string ChuoiTimKiem(System.Data.DataRow r, int[] nguon)
        {
            if (nguon.Length == 1) return BoLocDanhSach.ChuanHoaTimKiem(r[nguon[0]] as string ?? r[nguon[0]]?.ToString());
            var sb = new System.Text.StringBuilder();
            foreach (int o in nguon)
            {
                if (sb.Length > 0) sb.Append('|');
                sb.Append(BoLocDanhSach.ChuanHoaTimKiem(r[o] as string ?? r[o]?.ToString()));
            }
            return sb.ToString();
        }
        /// <summary>Điều kiện RowFilter "mỗi từ khóa (bỏ dấu) đều có mặt" trên cột tìm kiếm; rỗng = không lọc theo tên.</summary>
        public static string DieuKienTimKiem(string? tuKhoa)
        {
            var tu = TachTuKhoa(tuKhoa);
            if (tu.Length == 0) return "";
            return string.Join(" AND ", tu.Select(t => $"[{COT_TIM_KIEM}] LIKE '%{ThoatLike(t)}%'"));
        }
        /// <summary>Thoát chuỗi cho mẫu LIKE của RowFilter: nháy đơn + các ký tự đại diện * % [ ].</summary>
        public static string ThoatLike(string s)
        {
            var sb = new System.Text.StringBuilder(s.Length + 8);
            foreach (char ch in s)
            {
                if (ch == '\'') sb.Append("''");
                else if (ch is '*' or '%' or '[' or ']') sb.Append('[').Append(ch).Append(']');
                else sb.Append(ch);
            }
            return sb.ToString();
        }
        /// <summary>Thoát chuỗi cho phép so sánh "=" của RowFilter (chỉ nhân đôi nháy đơn, KHÔNG thoát ký tự LIKE).</summary>
        public static string ThoatBang(string s) => (s ?? "").Replace("'", "''");
        /// <summary>Dòng có khớp mọi từ khóa trên cột tìm kiếm không (cùng ngữ nghĩa với <see cref="DieuKienTimKiem"/>).</summary>
        public static bool KhopCotTimKiem(System.Data.DataRow r, string[] tu)
            => tu.Length == 0 || KhopTuKhoa(r[COT_TIM_KIEM] as string ?? "", tu);
        /// <summary>Chụp các dòng còn sống của bảng thành mảng (để chạy <see cref="LocKep{T}"/>).</summary>
        public static System.Data.DataRow[] DongConSong(System.Data.DataTable dt)
        {
            var kq = new List<System.Data.DataRow>(dt.Rows.Count);
            foreach (System.Data.DataRow r in dt.Rows)
                if (r.RowState != System.Data.DataRowState.Deleted && r.RowState != System.Data.DataRowState.Detached) kq.Add(r);
            return kq.ToArray();
        }
        /// <summary>Đọc chuỗi (đã Trim) của 1 cột, null/DBNull/không có cột = "".</summary>
        public static string Chuoi(System.Data.DataRow r, string cot)
        {
            var c = r.Table.Columns[cot];
            if (c == null) return "";
            object v = r[c];
            return v == null || v == DBNull.Value ? "" : (v as string ?? v.ToString() ?? "").Trim();
        }
        /// <summary>Hai danh sách chỉ số có giống hệt nhau (cùng thứ tự)?</summary>
        public static bool CungChiSo(IReadOnlyList<int>? a, IReadOnlyList<int>? b)
        {
            if (a == null || b == null || a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++) if (a[i] != b[i]) return false;
            return true;
        }
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
