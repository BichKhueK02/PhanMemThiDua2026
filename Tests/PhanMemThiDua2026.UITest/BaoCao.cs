using System.Diagnostics;
using System.Text;
namespace PhanMemThiDua2026.UITest
{
    internal enum KetQua { OK, CanhBao, Loi, Treo, BoQua }
    /// <summary>
    /// Báo cáo ghi NGAY từng dòng (sập giữa chừng vẫn còn kết quả) vào Database\NhatKyNhaPhatTrien\uitest-yyyyMMdd_HHmmss.log.
    /// Dùng chung cho luồng giao diện và luồng canh hộp thoại hệ thống => mọi thứ qua _khoa.
    /// </summary>
    internal static class BaoCao
    {
        private sealed class Buoc
        {
            public string Form = "", Nut = "";
            public Stopwatch Sw = Stopwatch.StartNew();
            public KetQua KetQua = KetQua.OK;
            public readonly List<string> ChiTiet = new();
            public bool DaBaoTreo;
        }
        private static readonly object _khoa = new();
        private static readonly List<Buoc> _nganXep = new();
        private static readonly List<string> _loiTongHop = new();
        private static readonly Dictionary<KetQua, int> _dem = new();
        private static readonly UTF8Encoding Utf8 = new(true);
        private static readonly Stopwatch _tong = Stopwatch.StartNew();
        /// <summary>Lần cuối có hoạt động (bắt đầu/kết thúc bước, hộp thoại...). Treo = KHÔNG có hoạt động nào quá ngưỡng,
        /// không phải "bước cha chạy lâu" (bước cha mở form con và bấm thử cả form con nên dài là bình thường).</summary>
        private static readonly Stopwatch _hoatDong = Stopwatch.StartNew();
        /// <summary>Thư mục phần mềm đang được test (tiến trình con: chính thư mục đang chạy; điều phối: thư mục người dùng chọn).</summary>
        public static string Goc { get; private set; } = AppContext.BaseDirectory;
        public static string ThuMucNhatKy => Path.Combine(Goc, "Database", "NhatKyNhaPhatTrien");
        public static void DatGoc(string thuMuc) => Goc = thuMuc;
        public static string Tep { get; private set; } = Path.Combine(ThuMucNhatKy, $"uitest-{DateTime.Now:yyyyMMdd_HHmmss}.log");
        /// <summary>Tệp do các lần bấm Xuất tạo ra (TepTam được phần mềm tự dọn sau 12 giờ).</summary>
        public static string ThuMucXuat { get; private set; } = Path.Combine(ThuMucNhatKy, "TepTam", $"UITest_{DateTime.Now:yyyyMMdd_HHmmss}");
        public static string TepLan(string dot, int lan) => Path.Combine(ThuMucNhatKy, $"uitest-{dot}-lan{lan:000}.log");
        /// <summary>Đặt tên tệp theo đợt chạy + số lần (gọi TRƯỚC KhoiDong).</summary>
        public static void DatTen(string dot, int lan)
        {
            Tep = TepLan(dot, lan);
            ThuMucXuat = Path.Combine(ThuMucNhatKy, "TepTam", $"UITest_{dot}_lan{lan:000}");
        }
        private static readonly Dictionary<string, long> _doDaiLogLucDau = new(StringComparer.OrdinalIgnoreCase);
        public static void KhoiDong()
        {
            Directory.CreateDirectory(ThuMucNhatKy);
            Directory.CreateDirectory(ThuMucXuat);
            try
            {
                foreach (var f in Directory.EnumerateFiles(ThuMucNhatKy, "error-*.log"))
                    _doDaiLogLucDau[f] = new FileInfo(f).Length;
            }
            catch { }
            Ghi($"===== KIỂM THỬ GIAO DIỆN TỰ ĐỘNG - {DateTime.Now:dd/MM/yyyy HH:mm:ss} =====");
            Ghi($"Thư mục phần mềm: {Goc}");
            Ghi($"Tệp xuất thử: {ThuMucXuat}");
            Ghi("");
        }
        public static void Ghi(string dong)
        {
            lock (_khoa)
            {
                try { File.AppendAllText(Tep, dong + Environment.NewLine, Utf8); } catch { }
                Debug.WriteLine("[UITest] " + dong);
            }
        }
        public static string TenBuocHienTai
        {
            get { lock (_khoa) return _nganXep.Count == 0 ? "(chưa bắt đầu)" : $"{_nganXep[^1].Form} > {_nganXep[^1].Nut}"; }
        }
        /// <summary>Số giây từ hoạt động cuối (bắt đầu/kết thúc bước, hộp thoại...).</summary>
        public static double GiayKhongHoatDong { get { lock (_khoa) return _hoatDong.Elapsed.TotalSeconds; } }
        public static void BatDauBuoc(string form, string nut)
        {
            lock (_khoa) { _nganXep.Add(new Buoc { Form = form, Nut = nut }); _hoatDong.Restart(); }
        }
        /// <summary>Ghi sự kiện vào bước đang chạy (bước trong cùng). Kết quả nặng hơn sẽ đè kết quả nhẹ.</summary>
        public static void SuKien(KetQua kq, string noiDung)
        {
            lock (_khoa)
            {
                if (_nganXep.Count == 0)
                {
                    Ghi($"[{Nhan(kq)}] (ngoài bước) {noiDung}");
                    if (kq is KetQua.Loi or KetQua.Treo) _loiTongHop.Add($"[{Nhan(kq)}] (ngoài bước) {noiDung}");
                    return;
                }
                var b = _nganXep[^1];
                _hoatDong.Restart();
                b.ChiTiet.Add(noiDung);
                if (MucDo(kq) > MucDo(b.KetQua)) b.KetQua = kq;
            }
        }
        public static void KetThucBuoc()
        {
            lock (_khoa)
            {
                if (_nganXep.Count == 0) return;
                var b = _nganXep[^1];
                _nganXep.RemoveAt(_nganXep.Count - 1);
                _hoatDong.Restart();
                string thut = new string(' ', _nganXep.Count * 2);
                string dong = $"{thut}[{Nhan(b.KetQua)}] {b.Form} > {b.Nut} ({b.Sw.ElapsedMilliseconds} ms)";
                Ghi(dong);
                foreach (var ct in b.ChiTiet) Ghi($"{thut}      · {ct}");
                _dem[b.KetQua] = _dem.GetValueOrDefault(b.KetQua) + 1;
                if (b.KetQua is KetQua.Loi or KetQua.Treo)
                    _loiTongHop.Add(dong.Trim() + (b.ChiTiet.Count > 0 ? "\n      · " + string.Join("\n      · ", b.ChiTiet) : ""));
            }
        }
        /// <summary>Gọi từ luồng canh: bước hiện tại chạy quá ngưỡng => ghi TREO một lần.</summary>
        public static void KiemTraTreo(int nguongGiay)
        {
            lock (_khoa)
            {
                if (_nganXep.Count == 0) return;
                var b = _nganXep[^1];
                if (b.DaBaoTreo || _hoatDong.Elapsed.TotalSeconds < nguongGiay) return;
                b.DaBaoTreo = true;
                b.ChiTiet.Add($"Không có phản hồi nào trong {nguongGiay} giây (có thể treo / xử lý quá lâu)");
                if (MucDo(KetQua.Treo) > MucDo(b.KetQua)) b.KetQua = KetQua.Treo;
                Ghi($"[ĐANG TREO?] {b.Form} > {b.Nut}: {nguongGiay} giây không có phản hồi...");
            }
        }
        public static void TongKet()
        {
            lock (_khoa)
            {
                while (_nganXep.Count > 0) KetThucBuoc();
                Ghi("");
                Ghi("===== TỔNG KẾT =====");
                Ghi($"Thời gian chạy: {_tong.Elapsed:hh\\:mm\\:ss}");
                foreach (KetQua k in Enum.GetValues<KetQua>())
                    Ghi($"  {Nhan(k),-10}: {_dem.GetValueOrDefault(k)}");
                Ghi("");
                if (_loiTongHop.Count == 0) Ghi("Không phát hiện lỗi / treo.");
                else
                {
                    Ghi($"DANH SÁCH LỖI / TREO ({_loiTongHop.Count}):");
                    foreach (var l in _loiTongHop) Ghi("  " + l);
                }
                Ghi("");
                GhiPhanLogLoiMoi();
            }
        }
        /// <summary>Chép phần nhật ký lỗi (error-*.log) phát sinh trong lúc chạy vào cuối báo cáo.</summary>
        private static void GhiPhanLogLoiMoi()
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var f in Directory.EnumerateFiles(ThuMucNhatKy, "error-*.log"))
                {
                    long dau = _doDaiLogLucDau.GetValueOrDefault(f);
                    long dai = new FileInfo(f).Length;
                    if (dai <= dau) continue;
                    using var fs = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    fs.Seek(dau, SeekOrigin.Begin);
                    var buf = new byte[(int)Math.Min(dai - dau, 200_000)];
                    int n = fs.Read(buf, 0, buf.Length);
                    sb.AppendLine($"--- {Path.GetFileName(f)} (+{dai - dau} byte) ---");
                    sb.AppendLine(Encoding.UTF8.GetString(buf, 0, n));
                }
                if (sb.Length == 0) Ghi("Nhật ký lỗi của phần mềm (error-*.log): không có dòng mới.");
                else
                {
                    Ghi("===== NHẬT KÝ LỖI CỦA PHẦN MỀM GHI TRONG LÚC CHẠY =====");
                    Ghi(sb.ToString());
                }
            }
            catch (Exception ex) { Ghi("Không đọc được error-*.log: " + ex.Message); }
        }
        public static string TomTatNgan()
        {
            lock (_khoa)
                return $"OK {_dem.GetValueOrDefault(KetQua.OK)} · Cảnh báo {_dem.GetValueOrDefault(KetQua.CanhBao)} · " +
                       $"Lỗi {_dem.GetValueOrDefault(KetQua.Loi)} · Treo {_dem.GetValueOrDefault(KetQua.Treo)} · Bỏ qua {_dem.GetValueOrDefault(KetQua.BoQua)}";
        }
        private static int MucDo(KetQua k) => k switch
        {
            KetQua.BoQua => 0, KetQua.OK => 1, KetQua.CanhBao => 2, KetQua.Treo => 3, KetQua.Loi => 4, _ => 0,
        };
        private static string Nhan(KetQua k) => k switch
        {
            KetQua.OK => "OK", KetQua.CanhBao => "CẢNH BÁO", KetQua.Loi => "LỖI", KetQua.Treo => "TREO", KetQua.BoQua => "BỎ QUA", _ => "?",
        };
        /// <summary>Phân loại nội dung hộp thoại: chữ "lỗi/error/exception/thất bại" => Lỗi; "không thể/cảnh báo" => Cảnh báo.</summary>
        /// <summary>
        /// Lỗi khi: TIÊU ĐỀ có lỗi/error/thất bại, hoặc nội dung có exception/ngoại lệ, hoặc chữ lỗi nằm ở ĐẦU nội dung
        /// (150 ký tự đầu). Không xét cả đoạn dài vì hộp hướng dẫn/xác nhận hay nhắc chữ "lỗi" (vd "CSDL lỗi thì dừng").
        /// </summary>
        public static KetQua PhanLoaiThongDiep(string tieuDe, string noiDung, bool laXacNhan = false)
        {
            string td = tieuDe.ToLowerInvariant();
            // Hộp HỎI (Có/Không) là câu hỏi xác nhận, không phải báo lỗi => chỉ xét tiêu đề
            string nd = laXacNhan ? "" : noiDung.ToLowerInvariant();
            string dau = nd.Length > 150 ? nd[..150] : nd;
            static bool CoLoi(string s) => s.Contains("lỗi") || s.Contains("error") || s.Contains("thất bại") || s.Contains("failed");
            static bool CoCanhBao(string s) => s.Contains("không thể") || s.Contains("cảnh báo") || s.Contains("warning") || s.Contains("không tìm thấy");
            if (CoLoi(td) || nd.Contains("exception") || nd.Contains("ngoại lệ") || CoLoi(dau)) return KetQua.Loi;
            if (CoCanhBao(td) || CoCanhBao(dau)) return KetQua.CanhBao;
            return KetQua.OK;
        }
    }
}
