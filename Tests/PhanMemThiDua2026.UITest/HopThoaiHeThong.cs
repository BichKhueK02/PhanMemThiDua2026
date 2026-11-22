using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
namespace PhanMemThiDua2026.UITest
{
    /// <summary>
    /// Luồng nền canh các hộp thoại HỆ THỐNG (lớp cửa sổ "#32770") của luồng giao diện:
    ///   • MessageBox            : đọc nội dung, ghi báo cáo, bấm Có/OK (hoặc Không/Hủy theo cấu hình).
    ///   • Lưu tệp (SaveFileDialog): điền đường dẫn vào thư mục xuất thử rồi bấm Lưu => kiểm tra được chức năng Xuất.
    ///   • Mở tệp (OpenFileDialog) : chọn tệp vừa xuất (cùng đuôi) hoặc tệp mẫu => kiểm tra Nhập; không có thì Hủy.
    ///   • Chọn thư mục           : điền thư mục xuất thử rồi bấm Chọn.
    /// Chạy ở luồng riêng vì khi hộp thoại mở, luồng giao diện đang bị chặn trong lệnh gọi ShowDialog của phần mềm.
    /// </summary>
    internal static class HopThoaiHeThong
    {
        private const int WM_SETTEXT = 0x000C, WM_GETTEXT = 0x000D, WM_GETTEXTLENGTH = 0x000E, WM_CLOSE = 0x0010, WM_COMMAND = 0x0111;
        private const int IDOK = 1, IDCANCEL = 2, IDABORT = 3, IDRETRY = 4, IDIGNORE = 5, IDYES = 6, IDNO = 7;
        private const int ID_TEN_TEP = 1148, ID_LOAI_TEP = 1136, ID_TEN_TEP_CU = 1152, ID_TEN_TEP_LUU = 1001;
        private delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint threadId, EnumProc cb, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumProc cb, IntPtr lParam);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
        private static readonly uint _tienTrinh = (uint)Environment.ProcessId;
        private static readonly HashSet<uint> _csrss = LayCsrss();
        private static HashSet<uint> LayCsrss()
        {
            try { return System.Diagnostics.Process.GetProcessesByName("csrss").Select(p => (uint)p.Id).ToHashSet(); }
            catch { return new HashSet<uint>(); }
        }
        [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr parent, EnumProc cb, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr hWnd, StringBuilder sb, int max);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern int GetDlgCtrlID(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr w, IntPtr l);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, int msg, IntPtr w, StringBuilder l, int flags, int timeout, out IntPtr result);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, int msg, IntPtr w, string l, int flags, int timeout, out IntPtr result);
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, int msg, IntPtr w, IntPtr l, int flags, int timeout, out IntPtr result);
        [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
        private const int SMTO_ABORTIFHUNG = 0x0002;
        private static uint _luongGiaoDien;
        private static CauHinh _ch = new();
        private static readonly Dictionary<IntPtr, (DateTime LanDau, int SoLan)> _daThay = new();
        private static readonly List<string> _tepDaXuat = new();
        private static volatile bool _dangMo;
        private static int _soTepXuat;
        /// <summary>Đang có hộp thoại hệ thống mở trên luồng giao diện (bộ bấm phải chờ).</summary>
        public static bool DangMo => _dangMo;
        public static IReadOnlyList<string> TepDaXuat { get { lock (_tepDaXuat) return _tepDaXuat.ToArray(); } }
        public static void BatDau(uint luongGiaoDien, CauHinh ch)
        {
            _luongGiaoDien = luongGiaoDien;
            _ch = ch;
            var t = new Thread(VongLap) { IsBackground = true, Name = "UITest - canh hộp thoại hệ thống" };
            t.Start();
        }
        private static void VongLap()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(250);
                    BaoCao.KiemTraTreo(_ch.NguongTreoGiay);
                    // Chốt chặn cuối: cả lần chạy không có hoạt động quá N phút => ghi tổng kết rồi dừng lần này (không treo mãi)
                    if (_ch.DungLanNeuKhongHoatDongPhut > 0 && BaoCao.GiayKhongHoatDong > _ch.DungLanNeuKhongHoatDongPhut * 60 && !ChuongTrinh.DaXong)
                    {
                        ChuongTrinh.DaXong = true;
                        BaoCao.SuKien(KetQua.Treo, $"{_ch.DungLanNeuKhongHoatDongPhut} phút không có hoạt động nào => dừng lần chạy này. Bước cuối: {BaoCao.TenBuocHienTai}");
                        BaoCao.TongKet();
                        try { System.Diagnostics.Process.GetCurrentProcess().Kill(); } catch { }
                    }
                    // Quét MỌI cửa sổ cấp cao của tiến trình (không chỉ luồng giao diện): MessageBox mở với
                    // MessageBoxOptions.DefaultDesktopOnly / ServiceNotification (vd "Xác nhận ghi đè dữ liệu")
                    // không thuộc luồng giao diện => bản cũ không thấy, phần mềm đứng chờ mãi.
                    var ds = new List<IntPtr>();
                    EnumWindows((h, _) =>
                    {
                        // DefaultDesktopOnly/ServiceNotification: Windows để csrss.exe dựng hộp thoại => thuộc csrss, không thuộc
                        // phần mềm (đã thử: PostMessage WM_COMMAND tới hộp đó vẫn bấm được).
                        GetWindowThreadProcessId(h, out uint pid);
                        if ((pid == _tienTrinh || _csrss.Contains(pid)) && IsWindowVisible(h) && TenLop(h) == "#32770") ds.Add(h);
                        return true;
                    }, IntPtr.Zero);
                    if (ds.Count == 0)
                        EnumThreadWindows(_luongGiaoDien, (h, _) =>
                        {
                            if (IsWindowVisible(h) && TenLop(h) == "#32770") ds.Add(h);
                            return true;
                        }, IntPtr.Zero);
                    _dangMo = ds.Count > 0;
                    foreach (var k in _daThay.Keys.ToList())
                        if (!IsWindow(k)) _daThay.Remove(k);
                    foreach (var h in ds)
                    {
                        if (!_daThay.TryGetValue(h, out var tt))
                        {
                            _daThay[h] = (DateTime.Now, 0);   // chờ hộp thoại dựng xong rồi mới xử lý
                            continue;
                        }
                        double tuoi = (DateTime.Now - tt.LanDau).TotalMilliseconds;
                        // lần 1 sau 500 ms; còn mở thì thử lại mỗi 3 giây (lần 2: Hủy, lần 3+: đóng cưỡng bức)
                        if (tuoi < 500 + tt.SoLan * 3000) continue;
                        _daThay[h] = (tt.LanDau, tt.SoLan + 1);
                        XuLy(h, tt.SoLan);
                    }
                }
                catch (Exception ex) { BaoCao.Ghi("[Luồng canh hộp thoại] " + ex.Message); }
            }
        }
        private static void XuLy(IntPtr h, int lanThu)
        {
            string tieuDe = LayChu(h);
            if (lanThu >= 2)
            {
                BaoCao.SuKien(KetQua.CanhBao, $"Hộp thoại \"{tieuDe}\" không đóng được => gửi lệnh đóng");
                PostMessage(h, WM_COMMAND, (IntPtr)IDCANCEL, IntPtr.Zero);
                PostMessage(h, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                return;
            }
            var con = new List<IntPtr>();
            EnumChildWindows(h, (c, _) => { con.Add(c); return true; }, IntPtr.Zero);
            bool laHopThoaiTep = con.Any(c => TenLop(c) is "DirectUIHWND" or "SHELLDLL_DefView" or "ComboBoxEx32")
                                 || con.Any(c => GetDlgCtrlID(c) == ID_TEN_TEP);
            if (laHopThoaiTep) { XuLyHopThoaiTep(h, tieuDe, con, lanThu); return; }
            if (con.Any(c => TenLop(c) == "SysTreeView32"))
            {
                BaoCao.SuKien(KetQua.OK, $"Hộp thoại chọn thư mục kiểu cũ \"{tieuDe}\" => Hủy");
                PostMessage(h, WM_COMMAND, (IntPtr)IDCANCEL, IntPtr.Zero);
                return;
            }
            // MessageBox / TaskDialog
            var chu = con.Where(c => TenLop(c) == "Static").Select(LayChu).Where(s => !string.IsNullOrWhiteSpace(s));
            string noiDung = string.Join(" ", chu).Replace("\r", " ").Replace("\n", " ").Trim();
            var nut = con.Where(c => TenLop(c) == "Button" && IsWindowVisible(c)).Select(GetDlgCtrlID).ToHashSet();
            // "Bạn có muốn mở tệp/thư mục ngay không?" => Không (tránh bật Excel/Explorer sau mỗi lần xuất)
            int[] uuTien = (_ch.TraLoiCo && lanThu == 0 && !LaHoiMoTep(tieuDe + " " + noiDung))
                ? new[] { IDYES, IDOK, IDRETRY, IDIGNORE, IDNO, IDCANCEL }
                : new[] { IDNO, IDCANCEL, IDOK, IDIGNORE, IDABORT };
            int chon = uuTien.FirstOrDefault(nut.Contains);
            if (chon == 0) chon = IDOK;
            if (nut.Count == 1) chon = nut.First();   // hộp thoại chỉ có 1 nút (OK) — Windows có khi gán mã IDCANCEL
            var kq = BaoCao.PhanLoaiThongDiep(tieuDe, noiDung, nut.Contains(IDYES) || nut.Contains(IDNO));
            BaoCao.SuKien(kq, $"Hộp thoại \"{tieuDe}\": {RutGon(noiDung, 400)} => bấm {(nut.Count == 1 ? "OK" : TenNut(chon))}");
            PostMessage(h, WM_COMMAND, (IntPtr)chon, IntPtr.Zero);
        }
        private static void XuLyHopThoaiTep(IntPtr h, string tieuDe, List<IntPtr> con, int lanThu)
        {
            string chuNutOk = con.Where(c => GetDlgCtrlID(c) == IDOK && TenLop(c) == "Button").Select(LayChu).FirstOrDefault() ?? "";
            string ok = chuNutOk.Replace("&", "").ToLowerInvariant();
            string td = tieuDe.ToLowerInvariant();
            IntPtr oTen = TimOTenTep(con);
            string loaiTep = con.Where(c => GetDlgCtrlID(c) == ID_LOAI_TEP).Select(LayChu).FirstOrDefault() ?? "";
            bool laLuu = ok.Contains("save") || ok.Contains("lưu") || td.Contains("lưu") || td.Contains("save") || td.Contains("xuất");
            bool laThuMuc = ok.Contains("folder") || ok.Contains("thư mục") || td.Contains("thư mục") || td.Contains("folder");
            if (lanThu > 0)
            {
                if (laThuMuc && lanThu == 1)
                {
                    // Gõ đường dẫn + OK chỉ ĐI VÀO thư mục; bấm Chọn thư mục lần nữa mới chọn thư mục đang đứng.
                    if (oTen != IntPtr.Zero) SendMessageTimeout(oTen, WM_SETTEXT, IntPtr.Zero, "", SMTO_ABORTIFHUNG, 2000, out _);
                    PostMessage(h, WM_COMMAND, (IntPtr)IDOK, IntPtr.Zero);
                    return;
                }
                BaoCao.SuKien(KetQua.CanhBao, $"Hộp thoại tệp \"{tieuDe}\" không chấp nhận đường dẫn => Hủy");
                PostMessage(h, WM_COMMAND, (IntPtr)IDCANCEL, IntPtr.Zero);
                return;
            }
            if (oTen == IntPtr.Zero)
            {
                BaoCao.SuKien(KetQua.CanhBao, $"Hộp thoại tệp \"{tieuDe}\": không tìm thấy ô tên tệp => Hủy");
                PostMessage(h, WM_COMMAND, (IntPtr)IDCANCEL, IntPtr.Zero);
                return;
            }
            string duongDan;
            if (laThuMuc)
            {
                duongDan = BaoCao.ThuMucXuat;
                BaoCao.SuKien(KetQua.OK, $"Chọn thư mục \"{tieuDe}\" => {duongDan}");
            }
            else if (laLuu)
            {
                string ten = LayChu(oTen).Trim().Trim('"');
                try { ten = Path.GetFileName(ten); } catch { ten = ""; }
                if (string.IsNullOrWhiteSpace(ten)) ten = "uitest" + DuoiDauTien(loaiTep);
                int so = Interlocked.Increment(ref _soTepXuat);
                duongDan = Path.Combine(BaoCao.ThuMucXuat, $"{so:000}_{ten}");
                lock (_tepDaXuat) _tepDaXuat.Add(duongDan);
                BaoCao.SuKien(KetQua.OK, $"Lưu tệp \"{tieuDe}\" => {duongDan}");
            }
            else
            {
                var duoi = Regex.Matches(loaiTep, @"\*\.([A-Za-z0-9]+)").Select(m => "." + m.Groups[1].Value.ToLowerInvariant()).ToHashSet();
                var ungVien = new List<string>();
                lock (_tepDaXuat) ungVien.AddRange(_tepDaXuat);
                try
                {
                    if (!string.IsNullOrWhiteSpace(_ch.ThuMucTepMau) && Directory.Exists(_ch.ThuMucTepMau))
                        ungVien.AddRange(Directory.EnumerateFiles(_ch.ThuMucTepMau));
                }
                catch { }
                string? tep = ungVien.Where(File.Exists)
                    .Where(f => duoi.Count == 0 || duoi.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .OrderByDescending(File.GetLastWriteTime).FirstOrDefault();
                if (tep == null)
                {
                    BaoCao.SuKien(KetQua.CanhBao, $"Mở tệp \"{tieuDe}\" ({loaiTep}): chưa có tệp phù hợp để nhập => Hủy");
                    PostMessage(h, WM_COMMAND, (IntPtr)IDCANCEL, IntPtr.Zero);
                    return;
                }
                duongDan = tep;
                BaoCao.SuKien(KetQua.OK, $"Mở tệp \"{tieuDe}\" => nhập {duongDan}");
            }
            SendMessageTimeout(oTen, WM_SETTEXT, IntPtr.Zero, duongDan, SMTO_ABORTIFHUNG, 2000, out _);
            Thread.Sleep(150);
            PostMessage(h, WM_COMMAND, (IntPtr)IDOK, IntPtr.Zero);
        }
        private static IntPtr TimOTenTep(List<IntPtr> con)
        {
            foreach (var c in con)
            {
                int id = GetDlgCtrlID(c);
                if (id != ID_TEN_TEP && id != ID_TEN_TEP_CU && id != ID_TEN_TEP_LUU) continue;
                if (TenLop(c) == "Edit") return c;
                IntPtr ketQua = IntPtr.Zero;
                EnumChildWindows(c, (e, _) => { if (TenLop(e) == "Edit") { ketQua = e; return false; } return true; }, IntPtr.Zero);
                if (ketQua != IntPtr.Zero) return ketQua;
            }
            return IntPtr.Zero;
        }
        private static string DuoiDauTien(string loaiTep)
        {
            var m = Regex.Match(loaiTep, @"\*(\.[A-Za-z0-9]+)");
            return m.Success ? m.Groups[1].Value : ".txt";
        }
        private static string TenLop(IntPtr h)
        {
            var sb = new StringBuilder(256);
            GetClassName(h, sb, sb.Capacity);
            return sb.ToString();
        }
        private static string LayChu(IntPtr h)
        {
            SendMessageTimeout(h, WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero, SMTO_ABORTIFHUNG, 1000, out IntPtr len);
            int n = (int)len;
            if (n <= 0) return "";
            var sb = new StringBuilder(Math.Min(n, 8000) + 1);
            SendMessageTimeout(h, WM_GETTEXT, (IntPtr)sb.Capacity, sb, SMTO_ABORTIFHUNG, 1000, out _);
            return sb.ToString();
        }
        private static string TenNut(int id) => id switch
        {
            IDOK => "OK", IDCANCEL => "Hủy", IDYES => "Có", IDNO => "Không", IDRETRY => "Thử lại", IDIGNORE => "Bỏ qua", IDABORT => "Hủy bỏ", _ => id.ToString(),
        };
        public static bool LaHoiMoTep(string s)
        {
            string t = s.ToLowerInvariant();
            return t.Contains("muốn mở") || t.Contains("mở tệp ngay") || t.Contains("mở file") || t.Contains("mở thư mục")
                   || t.Contains("open the file") || t.Contains("open file");
        }
        public static string RutGon(string s, int max) => s.Length <= max ? s : s[..max] + "…";
    }
}
