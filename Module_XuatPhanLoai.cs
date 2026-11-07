///Phiên bản 3 – tối ưu hiệu năng / RAM / an toàn. GIỮ NGUYÊN toàn bộ thành viên public của phiên bản 2.
//TrungKien - https://github.com/BichKhueK02/PhanMemThiDua2026
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
namespace PhanMemThiDua2026
{
    public static class Module_XuatPhanLoai
    {
        #region 1. Trạng thái công khai (GIỮ NGUYÊN TÊN)
        public static string LastFilePath = "";
        public static string LinkDanTep = "";
        public static string LinkLuuDuongDanTepXuat = string.Empty;
        private static readonly object _khoaThongTin = new();
        private static volatile bool _daNapThongTin;
        private static string _tenTrungDoan = "";
        private static string _tenTieuDoan = "";
        private static string _tomTatGhiChu = "";
        public static string TEN_TRUNG_DOAN => _tenTrungDoan;
        public static string TEN_TIEU_DOAN => _tenTieuDoan;
        public static string TOM_TAT_GHI_CHU => _tomTatGhiChu;
        // cờ hiệu đảm bảo nhận được link mới khi người dùng thay đổi / khi lần đọc trước thất bại
        private static volatile bool _canReloadLink = true;
        private static volatile bool _daDamBaoBangThang;
        private static int _dangXuat;                       // chặn bấm xuất chồng nhau (bản Async)
        private const string KhoangTrang = "      ";
        // Giữ nguyên hành vi làm tròn cũ (Banker's: 2.5 -> 2). Nếu nghiệp vụ muốn 2.5 -> 3 hãy đổi thành AwayFromZero
        // SAU KHI đối chiếu lại số liệu phân bổ BCH với cấp trên.
        private const MidpointRounding CheDoLamTron = MidpointRounding.ToEven;
        public static int soLuongBCHD;
        public static int soLuongBCHDLoai1;
        public static int soLuongBCHDLoai2;
        public static int soLuongBCHDLoai3;
        public static int soLuongBCHDLoai4;
        public static int soLuongBCHDKhongPhanLoai;
        public class KetQuaTinhBCH
        {
            public int TongBCH;
            public int Loai1;
            public int Loai2;
            public int Loai3;
            public int Loai4;
            public int KhongPhanLoai;
            // Kết quả tính theo đề nghị
            public int DeNghiLoai1;
            public int DeNghiLoai2;
            public int DeNghiLoai3;
        }
        public static string TenTrungDoan
        {
            get
            {
                if (!_daNapThongTin) NapThongTinDonVi();
                return _tenTrungDoan ?? "";
            }
        }
        public static string TenTieuDoan
        {
            get
            {
                if (!_daNapThongTin) NapThongTinDonVi();
                return _tenTieuDoan ?? "";
            }
        }
        public static void NapThongTinDonVi()
        {
            if (_daNapThongTin) return;
            lock (_khoaThongTin)
            {
                if (_daNapThongTin) return;
                if (LayThongTinDonVi(out string trungDoan, out string tieuDoan, out string tomTat))
                {
                    _tenTrungDoan = trungDoan;
                    _tenTieuDoan = tieuDoan;
                    _tomTatGhiChu = tomTat;
                    _daNapThongTin = true;
                }
            }
        }
        /// <summary>MỚI: xoá bộ nhớ đệm tên đơn vị. Gọi sau khi người dùng lưu lại Thông tin đơn vị để lần xuất sau lấy dữ liệu mới.</summary>
        public static void LamMoiThongTinDonVi()
        {
            lock (_khoaThongTin) { _daNapThongTin = false; }
        }
        #endregion
        #region 2. Hạ tầng nội bộ: kết nối CSDL, giải mã có đệm, lưu tệp an toàn
        /// <summary>Mở kết nối. Mặc định CHỈ ĐỌC: không khoá ghi, không vô tình tạo CSDL rỗng khi sai đường dẫn.</summary>
        private static SqliteConnection MoKetNoi(string duongDanDB, bool chiDoc = true)
        {
            if (string.IsNullOrWhiteSpace(duongDanDB) || !File.Exists(duongDanDB))
                throw new FileNotFoundException("Không tìm thấy cơ sở dữ liệu của phần mềm.", duongDanDB);
            var cs = new SqliteConnectionStringBuilder   // Builder tự escape ký tự đặc biệt trong đường dẫn (; = ...)
            {
                DataSource = duongDanDB,
                Mode = chiDoc ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate
            };
            var conn = new SqliteConnection(cs.ToString());
            try { conn.Open(); return conn; }
            catch { conn.Dispose(); throw; }
        }
        /// <summary>Bộ đệm giải mã sống trong PHẠM VI MỘT LẦN XUẤT (xong là giải phóng): mỗi chuỗi mã hoá chỉ giải 1 lần.</summary>
        private sealed class GiaiMaCache
        {
            private readonly Dictionary<string, string> _map = new(StringComparer.Ordinal);
            private const int GioiHan = 8192;
            /// <summary>Giải mã + Trim. Rỗng/lỗi giải mã => trả về chính chuỗi gốc (tương thích dữ liệu V1 chưa mã hoá).</summary>
            public string Lay(string raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return "";
                if (_map.TryGetValue(raw, out string daCo)) return daCo;
                string kq;
                try { kq = Module_BaoMatAES.GiaiMa(raw)?.Trim(); }
                catch { kq = null; }
                if (string.IsNullOrEmpty(kq)) kq = raw.Trim();
                if (_map.Count < GioiHan) _map[raw] = kq;
                return kq;
            }
        }
        private static void GhiNhatKyLoi(string hanhDong, Exception ex)
        {
            Debug.WriteLine($"[{hanhDong}] {ex.Message}");
            try
            {
                Module_NhatKy.GhiNhatKy(
                    taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                    hanhDong: hanhDong,
                    ghiChu: ex.Message);
            }
            catch { /* ghi nhật ký không bao giờ được làm hỏng luồng xuất */ }
        }
        private static string LayChuoi(DataRow r, string tenCot) => Convert.ToString(r[tenCot], CultureInfo.InvariantCulture) ?? "";
        private static string LayChuoi(DataRow r, DataColumn cot) => Convert.ToString(r[cot], CultureInfo.InvariantCulture) ?? "";
        private static string LamSachTenTep(string ten)
        {
            if (string.IsNullOrEmpty(ten)) return "";
            char[] cam = Path.GetInvalidFileNameChars();
            var sb = new System.Text.StringBuilder(ten.Length);
            foreach (char c in ten) sb.Append(Array.IndexOf(cam, c) >= 0 ? '_' : c);
            return sb.ToString().Trim().TrimEnd('.');
        }
        /// <summary>Lưu qua tệp tạm rồi đổi tên: không bao giờ để lại tệp .xlsx hỏng dở khi mất điện/lỗi/diệt virus quét giữa chừng.</summary>
        private static readonly object _khoaLuuExcel = new();
        /// <summary>
        /// Lưu Excel an toàn bằng tệp tạm duy nhất; xác minh gói XLSX trước khi thay thế.
        /// Giữ nguyên tệp đích cũ nếu quá trình lưu hoặc thay thế thất bại.
        /// </summary>
        private static void LuuAnToan(XLWorkbook wb, string fileDich)
        {
            ArgumentNullException.ThrowIfNull(wb);
            ArgumentException.ThrowIfNullOrWhiteSpace(fileDich);
            string dich = Path.GetFullPath(fileDich);
            if (!string.Equals(Path.GetExtension(dich), ".xlsx",
                StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Tệp đích phải có phần mở rộng .xlsx.", nameof(fileDich));
            string thuMuc = Path.GetDirectoryName(dich)
                ?? throw new IOException("Không xác định được thư mục lưu Excel.");
            lock (_khoaLuuExcel)
            {
                Directory.CreateDirectory(thuMuc);
                string tam = Path.Combine(thuMuc,
                    $".{Path.GetFileNameWithoutExtension(dich)}.{Guid.NewGuid():N}.tmp.xlsx");
                try
                {
                    wb.SaveAs(tam);
                    using (var fs = new FileStream(
                        tam, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                    {
                        if (fs.Length == 0)
                            throw new InvalidDataException("Tệp Excel tạm rỗng.");
                        fs.Flush(true);
                    }
                    using (var zip = System.IO.Compression.ZipFile.OpenRead(tam))
                    {
                        if (zip.GetEntry("[Content_Types].xml") is null ||
                            zip.GetEntry("xl/workbook.xml") is null)
                        {
                            throw new InvalidDataException(
                                "Tệp Excel tạm không có cấu trúc XLSX hợp lệ.");
                        }
                    }
                    if (File.Exists(dich))
                    {
                        File.Replace(tam, dich, null);
                    }
                    else
                    {
                        File.Move(tam, dich);
                    }
                }
                finally
                {
                    try
                    {
                        if (File.Exists(tam))
                            File.Delete(tam);
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }
        private static string VietHoaChuDau(string s, string macDinh)
        {
            if (string.IsNullOrWhiteSpace(s)) return macDinh;
            string t = s.ToLowerInvariant();
            return char.ToUpperInvariant(t[0]) + t.Substring(1);
        }
        private static bool LaKhongPhanLoai(string upper) =>
            upper.Contains("KHÔNG PL") || upper.Contains("KHONG PL") || upper.Contains("KPL") || upper.Contains("KHÔNG PHÂN LOẠI");
        private static string ChuanHoaPhanLoaiTieuDe(GiaiMaCache gm, string phanLoai)
        {
            string up = gm.Lay(phanLoai ?? "").ToUpperInvariant();
            return LaKhongPhanLoai(up) ? "KHÔNG PHÂN LOẠI" : up;
        }
        // Quy định ghi ngày/tháng: số < nguong thì thêm số 0 phía trước (tháng: 1,2 -> 01,02 ; ngày: 1..9 -> 01..09)
        private static string DinhDangSo(string v, int nguong)
        {
            if (string.IsNullOrWhiteSpace(v)) return KhoangTrang;
            if (!int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)) return v;
            return n < nguong ? n.ToString("00", CultureInfo.InvariantCulture) : n.ToString(CultureInfo.InvariantCulture);
        }
        #endregion
        #region 3. Windows API: Mở thư mục và chọn tệp thông minh
        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern void ILFree(IntPtr pidl);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern IntPtr ILCreateFromPathW(string pszPath);
        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, uint dwFlags);
        /// <summary>
        /// Gọi thư mục chứa tệp lên màn hình và bôi đen tệp. Không mở thêm cửa sổ nếu thư mục đã mở.
        /// Phải gọi trên luồng giao diện (STA).
        /// </summary>
        public static void MoThuMucVaChonTep(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;
            IntPtr pidlList = ILCreateFromPathW(Path.GetFullPath(filePath));
            if (pidlList == IntPtr.Zero) return;
            try { SHOpenFolderAndSelectItems(pidlList, 0, null, 0); }
            catch (Exception ex) { Debug.WriteLine("MoThuMucVaChonTep: " + ex.Message); }
            finally { ILFree(pidlList); }
        }
        #endregion
        #region 4. Đọc thông tin đơn vị / báo cáo (MỘT kết nối, MỘT lần đọc cho mỗi lần xuất)
        private sealed class ThongTinXuat
        {
            public string Thang = KhoangTrang, Nam = KhoangTrang, Ngay = KhoangTrang, DiaDiem = KhoangTrang; // dùng hiển thị
            public string NamGoc = "";                                        // năm giải mã, có thể rỗng
            public string ThangHT = "";                                       // tháng hệ thống đã định dạng
            public string NamHT = DateTime.Now.Year.ToString(CultureInfo.InvariantCulture);
            public string ChiHuyD = "";
            public string TenTrungDoanDong1 = "", TenTrungDoan = "", TenTieuDoan = "";
            public string KyHieuBaoCao = "...............";
            public string TomTatGhiChu = "";
            public string PTLoai1 = "", PTLoai2 = "", PTLoai3 = "";
            public string LoaiDeNghi = "";
            public string LoaiBaoCao = "", ChonTuan = "";
            public string TenDonViBanHanh = KhoangTrang;
            public string ChucVuDauTien = "", ChucVuNguoiKy = "";
            public bool NguoiKyLaDauTien = true;
        }
        private sealed class ThamSoBCH
        {
            public double L1, L2, L3, L4, KPL;
            public string KyHieuTieuDoan = "D2";
            public string LoaiDeNghi = "";
        }
        private sealed class KetQuaXuat
        {
            public string FilePath = "";
            public string ThuMuc = "";
            public int SoLuong;
            public string TenPhanLoai = "";
            public string ThongBaoKhongDuLieu;   // khác null => không có dữ liệu để xuất
        }
        private static KetQuaXuat TaoKetQuaKhongDuLieu(string thongBao) => new() { ThongBaoKhongDuLieu = thongBao };
        /// <summary>Ảnh chụp bối cảnh (đọc từ các module/UI) lấy trên luồng gọi, để phần xử lý nặng chạy nền an toàn.</summary>
        private readonly record struct BoiCanh(string DuongDanDB, string FileMau, bool LaTanBinh, bool CheDoNam, string[] DonViHopLe);
        private static BoiCanh ChupBoiCanh(bool canDonViHopLe = false)
        {
            string db = Module_DanduongGPS.DuongDanCSDL2;
            DamBaoBangThangHeThong(db);
            bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem()?.Contains("tân binh", StringComparison.OrdinalIgnoreCase) == true;
            string[] donVi = canDonViHopLe
                ? Module_DonVi.LayDanhSachDonViUuTienArray().Where(x => !string.IsNullOrWhiteSpace(x)).ToArray()
                : Array.Empty<string>();
            return new BoiCanh(db, Module_DanduongGPS.DuongDanCSDL4ex, laTanBinh, Module_HeThong.IsCheDoXetThiDuaNam(), donVi);
        }
        private static string DocCot(SqliteDataReader rd, Dictionary<string, int> ord, GiaiMaCache gm, string tenCot)
        {
            if (!ord.TryGetValue(tenCot, out int i) || rd.IsDBNull(i)) return "";
            return gm.Lay(Convert.ToString(rd.GetValue(i), CultureInfo.InvariantCulture) ?? "");
        }
        private static ThongTinXuat DocThongTinXuat(SqliteConnection conn, GiaiMaCache gm, bool laTanBinh, bool canNguoiKy, bool canDonViBanHanh)
        {
            var tt = new ThongTinXuat();
            // 1) Bảng ThongTin (ID = 1): SELECT * nhưng chỉ lấy đúng các cột cần (chịu được CSDL thiếu cột, không nạp cột nặng)
            using (var cmdTT = conn.CreateCommand())
            {
                cmdTT.CommandText = "SELECT * FROM ThongTin WHERE ID = 1 LIMIT 1";
                using var rdTT = cmdTT.ExecuteReader();
                if (rdTT.Read())
                {
                    var ord = new Dictionary<string, int>(rdTT.FieldCount, StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < rdTT.FieldCount; i++) ord[rdTT.GetName(i)] = i;
                    string Doc(string cot) => DocCot(rdTT, ord, gm, cot);
                    tt.Thang = DinhDangSo(Doc("Thang"), 3);
                    tt.Ngay = DinhDangSo(Doc("Ngay"), 10);
                    tt.NamGoc = Doc("Nam");
                    if (tt.NamGoc.Length > 0) { tt.Nam = tt.NamGoc; tt.NamHT = tt.NamGoc; }
                    string diaDiem = Doc("DiaDiem");
                    if (diaDiem.Length > 0) tt.DiaDiem = diaDiem;
                    tt.ChiHuyD = Doc("ChiHuyD");
                    tt.TenTrungDoanDong1 = Doc("textBox1_TenTrungDoanDong1");
                    tt.TenTrungDoan = Doc("TenTrungDoan");
                    tt.TenTieuDoan = Doc("TenTieuDoan");
                    string kyHieu = Doc("KyHieuBaoCao");
                    if (kyHieu.Length > 0) tt.KyHieuBaoCao = kyHieu;
                    tt.TomTatGhiChu = Doc("TomTatGhiChu");
                    tt.PTLoai1 = Doc("PTLoai1");
                    tt.PTLoai2 = Doc("PTLoai2");
                    tt.PTLoai3 = Doc("PTLoai3");
                    tt.LoaiDeNghi = Doc("LoaiDeNghi");
                }
            }
            tt.ThangHT = DocThangHeThong(conn);
            // 2) Loại báo cáo (Tháng/Tuần)
            try
            {
                using var cmdBC = conn.CreateCommand();
                cmdBC.CommandText = "SELECT ChonLoaiBaoCao, ChonTuan FROM ChonLoaiBaoCao WHERE ID = 1";
                using var rdBC = cmdBC.ExecuteReader();
                if (rdBC.Read())
                {
                    tt.LoaiBaoCao = gm.Lay(rdBC.IsDBNull(0) ? "" : rdBC.GetValue(0).ToString());
                    tt.ChonTuan = gm.Lay(rdBC.IsDBNull(1) ? "" : rdBC.GetValue(1).ToString());
                }
            }
            catch (Exception ex) { GhiNhatKyLoi("Xuất PL: đọc ChonLoaiBaoCao", ex); }
            // 3) Đơn vị ban hành tờ trình (chỉ Trình ký CBCS cần)
            if (canDonViBanHanh)
            {
                try
                {
                    using var cmdDV = conn.CreateCommand();
                    cmdDV.CommandText = "SELECT TenDonVi FROM DanhSach_DonViBanHanhToTrinh WHERE ID = 1";
                    object kq = cmdDV.ExecuteScalar();
                    if (kq != null && kq != DBNull.Value)
                    {
                        string v = gm.Lay(kq.ToString());
                        if (v.Length > 0) tt.TenDonViBanHanh = v;
                    }
                }
                catch (Exception ex) { GhiNhatKyLoi("Xuất PL: đọc đơn vị ban hành", ex); }
            }
            // 4) Người ký (bảng ChiHuyD hoặc ChiHuyD_TanBinh – tên bảng cố định, không nhận từ bên ngoài)
            if (canNguoiKy)
            {
                string bang = laTanBinh ? "ChiHuyD_TanBinh" : "ChiHuyD";
                using var cmdCH = conn.CreateCommand();
                cmdCH.CommandText = $"SELECT HoVaTen, ChucVu FROM [{bang}] ORDER BY ID ASC";
                using var rdCH = cmdCH.ExecuteReader();
                int soDong = 0;
                bool khop = false;
                while (rdCH.Read())
                {
                    soDong++;
                    string ht = gm.Lay(rdCH.IsDBNull(0) ? "" : rdCH.GetValue(0).ToString());
                    string cv = gm.Lay(rdCH.IsDBNull(1) ? "" : rdCH.GetValue(1).ToString());
                    if (soDong == 1) tt.ChucVuDauTien = cv;
                    if (!khop && ht.Equals(tt.ChiHuyD, StringComparison.OrdinalIgnoreCase))
                    {
                        khop = true;
                        tt.ChucVuNguoiKy = cv;
                        tt.NguoiKyLaDauTien = soDong == 1;
                    }
                }
                if (!khop) { tt.ChucVuNguoiKy = tt.ChucVuDauTien; tt.NguoiKyLaDauTien = true; }
            }
            return tt;
        }
        private static DataTable DocDanhSach(SqliteConnection conn)
        {
            var dt = new DataTable();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM DanhSach";
            using var rd = cmd.ExecuteReader();
            dt.Load(rd);
            return dt;
        }
        private static DataTable DocBangTyLe(SqliteConnection conn)
        {
            var dt = new DataTable();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM TyLe ORDER BY ID";
            using var rd = cmd.ExecuteReader();
            dt.Load(rd);
            return dt;
        }
        // Đọc ô bảng TyLe an toàn (thiếu dòng/cột => rỗng, không văng IndexOutOfRange)
        private static string TyLeStr(DataTable dt, int dong, string cot) =>
            dong >= 0 && dong < dt.Rows.Count && dt.Columns.Contains(cot)
                ? Convert.ToString(dt.Rows[dong][cot], CultureInfo.InvariantCulture) ?? ""
                : "";
        private static ThamSoBCH DocThamSoBCH(SqliteConnection conn, GiaiMaCache gm, ThongTinXuat tt)
        {
            var ts = new ThamSoBCH { LoaiDeNghi = tt.LoaiDeNghi };
            string cot = ts.LoaiDeNghi.Trim().ToUpperInvariant() switch   // danh sách trắng => không thể tiêm SQL
            {
                "LOẠI 1" => "Loai_1",
                "LOẠI 2" => "Loai_2",
                "LOẠI 3" => "Loai_3",
                "LOẠI 4" => "Loai_4",
                _ => "Khong_PL"
            };
            bool ok = false;
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT ID, {cot} FROM QuyDinhTyLeBCH WHERE ID BETWEEN 1 AND 5";
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    int id = rd.GetInt32(0);
                    string s = (rd.IsDBNull(1) ? "0" : rd.GetValue(1)?.ToString() ?? "0").Replace("%", "");
                    double rate = (double.TryParse(s, out double v) ? v : 0) / 100.0;
                    switch (id)
                    {
                        case 1: ts.L1 = rate; break;
                        case 2: ts.L2 = rate; break;
                        case 3: ts.L3 = rate; break;
                        case 4: ts.L4 = rate; break;
                        case 5: ts.KPL = rate; break;
                    }
                }
                ok = true;
            }
            catch (Exception ex) { GhiNhatKyLoi("Xuất PL: đọc QuyDinhTyLeBCH", ex); }
            if (!ok) { ts.L1 = 0.50; ts.L2 = 1.00; ts.L3 = 0; ts.L4 = 0; ts.KPL = 0; }
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT KyHieu_TieuDoan FROM KyHieu_DonVi WHERE ID = 1";
                object res = cmd.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                {
                    string v = gm.Lay(res.ToString());
                    if (v.Length > 0) ts.KyHieuTieuDoan = v;
                }
            }
            catch { /* giữ mặc định "D2" */ }
            return ts;
        }
        private static void DamBaoBangThangHeThong(string db)
        {
            if (_daDamBaoBangThang) return;
            try
            {
                using var conn = MoKetNoi(db, chiDoc: false);
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "CREATE TABLE IF NOT EXISTS ThangHeThong (ID INTEGER PRIMARY KEY, Thang TEXT);";
                cmd.ExecuteNonQuery();
                _daDamBaoBangThang = true;
            }
            catch (Exception ex) { Debug.WriteLine("DamBaoBangThangHeThong: " + ex.Message); }
        }
        private static string DinhDangThangHeThong(string thang) =>
            int.TryParse(thang, NumberStyles.Integer, CultureInfo.InvariantCulture, out int so)
                ? (so == 1 || so == 2 ? so.ToString("00", CultureInfo.InvariantCulture) : so.ToString(CultureInfo.InvariantCulture))
                : thang;
        private static string DocThangHeThong(SqliteConnection conn)
        {
            string thang = DateTime.Now.Month.ToString(CultureInfo.InvariantCulture);
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Thang FROM ThangHeThong WHERE ID = 1";
                object res = cmd.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                {
                    string t = Convert.ToString(res, CultureInfo.InvariantCulture)?.Trim();
                    if (!string.IsNullOrEmpty(t)) thang = t;
                }
            }
            catch (Exception ex) { Debug.WriteLine("DocThangHeThong: " + ex.Message); }
            return DinhDangThangHeThong(thang);
        }
        private static string ChuoiThoiGian(ThongTinXuat tt)
        {
            if (tt.LoaiBaoCao.Equals("Tuần", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(tt.ChonTuan))
            {
                string t = tt.ChonTuan.Trim().ToUpperInvariant();
                if (!t.Contains("TUẦN")) t = "TUẦN " + t;
                return $"{t} THÁNG {tt.ThangHT}/{tt.NamHT}";
            }
            return $"THÁNG {tt.ThangHT}/{tt.NamHT}";
        }
        private static string TaoTieuDeBaoCao(ThongTinXuat tt)
        {
            if (string.IsNullOrWhiteSpace(tt.ThangHT) || string.IsNullOrWhiteSpace(tt.NamGoc)) return "";
            if (tt.LoaiBaoCao.Equals("Tháng", StringComparison.OrdinalIgnoreCase))
                return $"DANH SÁCH ĐỀ NGHỊ XÉT PHÂN LOẠI THI ĐUA THÁNG {tt.ThangHT}/{tt.NamGoc}";
            if (tt.LoaiBaoCao.Equals("Tuần", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(tt.ChonTuan))
                    return $"DANH SÁCH ĐỀ NGHỊ XÉT PHÂN LOẠI THI ĐUA THÁNG {tt.ThangHT}/{tt.NamGoc}";
                return $"DANH SÁCH ĐỀ NGHỊ XÉT PHÂN LOẠI THI ĐUA {tt.ChonTuan.ToUpperInvariant()} THÁNG {tt.ThangHT}/{tt.NamGoc}";
            }
            return "";
        }
        public static bool LayThongTinDonVi(out string tenTrungDoan, out string tenTieuDoan, out string tomTatGhiChu)
        {
            tenTrungDoan = "";
            tenTieuDoan = "";
            tomTatGhiChu = "";
            try
            {
                string dbPath = Module_DanduongGPS.DuongDanCSDL2;
                if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return false;
                using var cn = MoKetNoi(dbPath);
                using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT TenTrungDoan, TenTieuDoan, TomTatGhiChu FROM ThongTin ORDER BY ID ASC LIMIT 1";
                using var rd = cmd.ExecuteReader();
                if (!rd.Read()) return false;
                tenTrungDoan = SafeDecrypt(rd, "TenTrungDoan");
                tenTieuDoan = SafeDecrypt(rd, "TenTieuDoan");
                tomTatGhiChu = SafeDecrypt(rd, "TomTatGhiChu");
                return true;
            }
            catch
            {
                return false;
            }
        }
        private static string SafeDecrypt(SqliteDataReader rd, string columnName)
        {
            try
            {
                int idx = rd.GetOrdinal(columnName);
                if (rd.IsDBNull(idx)) return "";
                string raw = rd.GetString(idx);
                if (string.IsNullOrWhiteSpace(raw)) return "";
                return Module_BaoMatAES.GiaiMa(raw);
            }
            catch
            {
                return "";
            }
        }
        #endregion
        #region 5. Hàm dựng Excel dùng chung (định dạng theo khối thay vì từng ô)
        private static void GhiO(IXLCell cell, string text, int size, bool bold = false, bool italic = false,
            XLAlignmentHorizontalValues ngang = XLAlignmentHorizontalValues.Center)
        {
            cell.Value = text;
            var st = cell.Style;
            st.Font.FontName = Module_HeThong.Font_Times_New_Roman;
            st.Font.FontSize = size;
            st.Font.Bold = bold;
            st.Font.Italic = italic;
            st.Alignment.Horizontal = ngang;
            st.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        // Gạch chân phần giữa chuỗi: motPhanHai=true => 1/2 chuỗi (xuất lẻ), false => 1/3 chuỗi (xuất tất cả) – giữ đúng hành vi cũ
        private static void GachChan(IXLCell cell, string text, bool motPhanHai)
        {
            if (cell == null || string.IsNullOrWhiteSpace(text)) return;
            cell.Value = text;
            int n = text.Length;
            int gach = motPhanHai ? (int)Math.Ceiling(n / 2.0) : (int)Math.Round(n / 3.0);
            if (gach <= 0) return;
            cell.GetRichText().Substring((n - gach) / 2, gach).SetUnderline();
        }
        private static void GachChanTenDonViBanHanh(IXLRange range, string text)
        {
            if (range == null || string.IsNullOrWhiteSpace(text)) return;
            range.Merge();
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            var cell = range.Cell(1, 1);
            cell.Value = string.Empty;
            var rt = cell.GetRichText();
            rt.ClearText();
            int n = text.Length, gach, start;
            if (n <= 2) { start = 0; gach = n; }                    // 1-2 ký tự: gạch hết
            else if (n == 3) { start = 1; gach = 1; }               // 3 ký tự: chỉ gạch ký tự giữa
            else
            {
                int half = (int)Math.Round(n / 2.0);
                gach = n % 2 != 0 ? (half % 2 != 0 ? half : half + 1) : (half % 2 == 0 ? half : half + 1);
                if (gach > n) gach = n;
                start = (n - gach) / 2;
            }
            void Them(string s, bool gachChan)
            {
                if (string.IsNullOrEmpty(s)) return;
                rt.AddText(s).SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(12).SetBold(true)
                  .SetUnderline(gachChan ? XLFontUnderlineValues.Single : XLFontUnderlineValues.None);
            }
            Them(text.Substring(0, start), false);
            Them(text.Substring(start, gach), true);
            Them(text.Substring(start + gach), false);
        }
        private static void DinhDangKhoiDuLieu(IXLWorksheet ws, int tuDong, int denDong, int soCot)
        {
            if (denDong < tuDong) return;
            var st = ws.Range(tuDong, 1, denDong, soCot).Style;
            st.Font.FontName = Module_HeThong.Font_Times_New_Roman;
            st.Font.FontSize = 13;
            st.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            st.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            st.Border.OutsideBorder = XLBorderStyleValues.Thin;
            st.Border.InsideBorder = XLBorderStyleValues.Thin;
            st.NumberFormat.Format = "@";
        }
        private static void ThuNhoGhiChuDai(IXLWorksheet ws, List<int> cacDong, int cot)
        {
            foreach (int d in cacDong)
            {
                var st = ws.Cell(d, cot).Style;
                st.Font.FontSize = 8;
                st.Alignment.WrapText = true;
            }
        }
        private static void GhiDongNhom(IXLWorksheet ws, int row, int soCot, string text, bool fontChuan)
        {
            var rg = ws.Range(row, 1, row, soCot);
            rg.Merge();
            rg.Value = text;
            var st = rg.Style;
            st.Font.Bold = true;
            if (fontChuan)
            {
                st.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                st.Font.FontSize = 13;
                st.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
            st.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            st.Fill.BackgroundColor = XLColor.LightGray;
            st.Border.OutsideBorder = XLBorderStyleValues.Thin;
            if (fontChuan) st.Border.OutsideBorderColor = XLColor.Black;
        }
        private static List<DataColumn> LayCotXuat(DataTable dt)
        {
            var ds = new List<DataColumn>(dt.Columns.Count);
            foreach (DataColumn c in dt.Columns)
                if (c.ColumnName is not ("ID" or "STT" or "PhanLoai" or "GhiChu")) ds.Add(c);
            return ds;
        }
        private static int KhoaSTT(DataRow r, bool coCot) =>
            coCot && int.TryParse(Convert.ToString(r["STT"], CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out int s)
                ? s : int.MaxValue;
        // Sắp theo STT tăng dần, STT không phải số xếp cuối (OrderBy ổn định, khoá tính 1 lần/dòng)
        private static List<DataRow> SapXepTheoSTT(DataTable dt, IEnumerable<DataRow> data)
        {
            bool co = dt.Columns.Contains("STT");
            return data.OrderBy(r => KhoaSTT(r, co)).ToList();
        }
        private static void GhiODongKy(IXLWorksheet ws, ref int dong, string text)
        {
            ws.Range(dong, 6, dong, 10).Merge();
            var c = ws.Cell(dong, 6);
            c.Value = text;
            var st = c.Style;
            st.Font.Bold = true;
            st.Font.FontName = Module_HeThong.Font_Times_New_Roman;
            st.Font.FontSize = 14;
            st.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            st.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            dong++;
        }
        private static void GhiDonViA1A3(IXLWorksheet ws, ThongTinXuat tt, bool gachMotPhanHai)
        {
            GhiO(ws.Cell("A1"), string.IsNullOrWhiteSpace(tt.TenTrungDoanDong1) ? KhoangTrang : tt.TenTrungDoanDong1, 13);
            if (!string.IsNullOrWhiteSpace(tt.TenTrungDoan))
            {
                GhiO(ws.Cell("A2"), tt.TenTrungDoan, 13);
                GhiO(ws.Cell("A3"), tt.TenTieuDoan, 13, bold: true);
                GachChan(ws.Cell("A3"), tt.TenTieuDoan, gachMotPhanHai);
            }
            else
            {
                GhiO(ws.Cell("A2"), tt.TenTieuDoan, 13, bold: true);
                GachChan(ws.Cell("A2"), tt.TenTieuDoan, gachMotPhanHai);
                var a3 = ws.Cell("A3");
                a3.Value = "";
                a3.Style.Font.Bold = false;
            }
        }
        /// <summary>Chữ ký + A1..A3 + A6 + A7 + E4 của một sheet danh sách (dùng chung cho xuất lẻ và xuất tất cả).</summary>
        private static void GhiTieuDeVaKyTen(IXLWorksheet ws, ThongTinXuat tt, string phanLoai, int dongTong,
            bool inHoaChucVu, bool gachMotPhanHai, GiaiMaCache gm)
        {
            string CV(string s) => inHoaChucVu ? s.ToUpperInvariant() : s;
            int dongKy = dongTong + 1;
            if (tt.NguoiKyLaDauTien) GhiODongKy(ws, ref dongKy, CV(tt.ChucVuDauTien));
            else
            {
                GhiODongKy(ws, ref dongKy, "KT. " + CV(tt.ChucVuDauTien));
                GhiODongKy(ws, ref dongKy, CV(tt.ChucVuNguoiKy));
            }
            dongKy += 4;
            GhiODongKy(ws, ref dongKy, tt.ChiHuyD);
            GhiDonViA1A3(ws, tt, gachMotPhanHai);
            GhiO(ws.Cell("A6"),
                $"CBCS ĐỀ NGHỊ {ChuanHoaPhanLoaiTieuDe(gm, phanLoai)} TRONG PHONG TRÀO THI ĐUA \"VÌ ANTQ\" {ChuoiThoiGian(tt)}",
                14, bold: true);
            var cellA7 = ws.Cell("A7");
            cellA7.Clear(XLClearOptions.Contents);
            var rt = cellA7.GetRichText();
            rt.AddText("(Kèm theo Báo cáo ").SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(14).SetItalic();
            rt.AddText($"số:             {tt.KyHieuBaoCao}, ngày {tt.Ngay}/{tt.Thang}/{tt.Nam}")
              .SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(14).SetItalic().SetUnderline();
            rt.AddText(" của " + VietHoaChuDau(tt.TenTieuDoan, "      ") + ")")
              .SetFontName(Module_HeThong.Font_Times_New_Roman).SetFontSize(14).SetItalic();
            cellA7.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            GhiO(ws.Cell("E4"), $"{tt.DiaDiem}, ngày {tt.Ngay} tháng {tt.Thang} năm {tt.Nam}", 14, italic: true);
        }
        /// <summary>Đổ bảng CBCS vào sheet (từ dòng 10) – dữ liệu ghi từng ô, định dạng theo KHỐI. Trả về dòng "Tổng cộng".</summary>
        private static void GhiSheetCBCS(IXLWorksheet ws, DataTable dt, List<DataRow> data, GiaiMaCache gm,
            bool laTanBinh, int coChuSTT, string phanLoaiCotJ, out int dongTong)
        {
            const int rowStart = 10;
            var cols = LayCotXuat(dt);
            var dataXuat = SapXepTheoSTT(dt, data);
            int row = rowStart;
            foreach (DataRow r in dataXuat)
            {
                ws.Cell(row, 1).Value = row - rowStart + 1;
                for (int c = 0; c < cols.Count; c++)
                {
                    int excelCol = c + 2;
                    ws.Cell(row, excelCol).Value = (laTanBinh && excelCol == 3) ? "" : gm.Lay(LayChuoi(r, cols[c]));
                }
                ws.Cell(row, 10).Value = phanLoaiCotJ ?? gm.Lay(LayChuoi(r, "PhanLoai"));
                row++;
            }
            if (row > rowStart)
            {
                int cuoi = row - 1;
                var stSTT = ws.Range(rowStart, 1, cuoi, 1).Style;
                stSTT.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                stSTT.Font.FontSize = coChuSTT;
                stSTT.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                stSTT.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                if (cols.Count > 0)
                {
                    var stBody = ws.Range(rowStart, 2, cuoi, cols.Count + 1).Style;
                    stBody.NumberFormat.Format = "@";
                    stBody.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                    stBody.Font.FontSize = 13;
                    stBody.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    stBody.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Range(rowStart, 2, cuoi, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    if (cols.Count >= 4)
                        ws.Range(rowStart, 5, cuoi, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                }
                var stJ = ws.Range(rowStart, 10, cuoi, 10).Style;
                stJ.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                stJ.Font.FontSize = 13;
                stJ.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                stJ.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                var stBorder = ws.Range(rowStart, 1, cuoi, 11).Style;
                stBorder.Border.OutsideBorder = XLBorderStyleValues.Thin;
                stBorder.Border.InsideBorder = XLBorderStyleValues.Thin;
            }
            ws.Range(row, 1, row, 11).Merge();
            var tong = ws.Cell(row, 1);
            tong.Value = $"Tổng cộng: {data.Count} {Module_HeThong.Tu_dong_chi}./.";
            var stTong = tong.Style;
            stTong.Font.Bold = true;
            stTong.Font.Italic = true;
            stTong.Font.FontName = Module_HeThong.Font_Times_New_Roman;
            stTong.Font.FontSize = 14;
            stTong.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            dongTong = row;
        }
        /// <summary>Đổ bảng Tân binh bằng InsertData (1 lệnh) + định dạng theo khối.</summary>
        private static bool GhiSheetTanBinh(IXLWorksheet ws, List<DataRow> data, GiaiMaCache gm, out int dongTong, out string phanLoaiDau)
        {
            const int rowStart = 10;
            dongTong = 0;
            phanLoaiDau = "";
            var list = new List<CbcDTO>(data.Count);
            for (int i = 0; i < data.Count; i++)
            {
                DataRow r = data[i];
                list.Add(new CbcDTO
                {
                    STT = i + 1,
                    HoVaTen = gm.Lay(LayChuoi(r, "HoVaTen")),
                    SoHieuCAND = "",   // TÂN BINH KHÔNG CÓ SỐ HIỆU
                    NamSinh = gm.Lay(LayChuoi(r, "NamSinh")),
                    QueQuan = gm.Lay(LayChuoi(r, "QueQuan")),
                    NgayVaoCAND = gm.Lay(LayChuoi(r, "NgayVaoCAND")),
                    CapBac = gm.Lay(LayChuoi(r, "CapBac")),
                    ChucVu = gm.Lay(LayChuoi(r, "ChucVu")),
                    DonVi = gm.Lay(LayChuoi(r, "DonVi")),
                    PhanLoai = gm.Lay(LayChuoi(r, "PhanLoai")),
                    GhiChu = ""
                });
            }
            if (list.Count == 0) return false;
            ws.Cell(rowStart, 1).InsertData(list);
            int rowEnd = rowStart + list.Count - 1;
            var st = ws.Range(rowStart, 1, rowEnd, 11).Style;
            st.Font.FontName = Module_HeThong.Font_Times_New_Roman;
            st.Font.FontSize = 13;
            st.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            st.Border.OutsideBorder = XLBorderStyleValues.Thin;
            st.Border.InsideBorder = XLBorderStyleValues.Thin;
            st.NumberFormat.Format = "@";
            ws.Range(rowStart, 2, rowEnd, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            ws.Range(rowStart, 5, rowEnd, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            foreach (int col in new[] { 1, 3, 4, 6, 7, 8, 9, 10, 11 })
                ws.Range(rowStart, col, rowEnd, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            // (Bỏ vòng lặp thu nhỏ chữ cột Ghi chú: Tân binh luôn để trống GhiChu nên không bao giờ chạy)
            int rowTong = rowEnd + 1;
            var rangeTong = ws.Range(rowTong, 1, rowTong, 11);
            rangeTong.Merge();
            rangeTong.Value = $"Tổng cộng: {list.Count} {Module_HeThong.Tu_dong_chi}./.";
            var stTong = rangeTong.Style;
            stTong.Font.Bold = true;
            stTong.Font.Italic = true;
            stTong.Font.FontName = Module_HeThong.Font_Times_New_Roman;
            stTong.Font.FontSize = 14;
            stTong.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            dongTong = rowTong;
            phanLoaiDau = list[0].PhanLoai;
            return true;
        }
        /// <summary>Ghi dòng "Tổng QS ..." (A7) + khối "Ghi chú" chi tiết bảng TyLe. Trả về dòng trống kế tiếp.</summary>
        private static int GhiTongQSVaGhiChu(IXLWorksheet ws, DataTable dtTyLe, ThongTinXuat tt, string tuTongQS,
            string nhan1, string nhan2, string nhan3, bool thayLoaiTrongMoTa)
        {
            var rangeTQS = ws.Range(7, 1, 7, 7);
            rangeTQS.Merge();
            var stQS = rangeTQS.Style;
            stQS.Font.FontName = Module_HeThong.Font_Times_New_Roman;
            stQS.Font.FontSize = 12;
            stQS.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            stQS.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            rangeTQS.Value = $"Tổng QS {TyLeStr(dtTyLe, 0, "KQ Can dat")} {tuTongQS} (Trong đó: " +
                             $"{nhan1} {tt.PTLoai1}% = {TyLeStr(dtTyLe, 1, "KQ Can dat")} đ/c, " +
                             $"{nhan2} {tt.PTLoai2}% = {TyLeStr(dtTyLe, 2, "KQ Can dat")} đ/c, " +
                             $"{nhan3} {tt.PTLoai3}% = {TyLeStr(dtTyLe, 3, "KQ Can dat")} đ/c)";
            int lastRowB = ws.Column(2).LastCellUsed()?.Address.RowNumber ?? 11;
            int startRow = lastRowB + 2;
            var rangeGhiChu = ws.Range(startRow, 1, startRow, 7);
            rangeGhiChu.Merge();
            rangeGhiChu.Value = "Ghi chú:";
            var stGC = rangeGhiChu.Style;
            stGC.Font.FontName = Module_HeThong.Font_Times_New_Roman;
            stGC.Font.FontSize = 12;
            stGC.Font.Bold = true;
            stGC.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            stGC.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            startRow++;
            for (int i = 0; i < dtTyLe.Rows.Count && i < 6; i++)
            {
                if (int.TryParse(TyLeStr(dtTyLe, i, "KQ Can dat"), out int kqCanDat) && kqCanDat == 0) continue;
                string thongTin = TyLeStr(dtTyLe, i, "Thong tin");
                string kqGuiE29 = TyLeStr(dtTyLe, i, "KQ Gui E29");
                string ketLuan = TyLeStr(dtTyLe, i, "Ket luan");
                if (thayLoaiTrongMoTa)
                {
                    thongTin = thongTin.Replace(Module_HeThong.Loai_1, Module_HeThong.PL_CSTD)
                                       .Replace(Module_HeThong.Loai_2, Module_HeThong.PL_CSTT)
                                       .Replace(Module_HeThong.Loai_3, Module_HeThong.PL_HTNV)
                                       .Replace(Module_HeThong.Loai_4, Module_HeThong.PL_KHTNV);
                }
                string cellValue = thongTin + " " + kqGuiE29;
                if (!string.IsNullOrWhiteSpace(ketLuan)) cellValue += " - " + ketLuan;
                if (i == 0) cellValue += $" {Module_HeThong.Tu_dong_chi}.";
                var range = ws.Range(startRow, 1, startRow, 7);
                range.Merge();
                range.Value = cellValue;
                var st = range.Style;
                st.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                st.Font.FontSize = 12;
                st.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                st.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                startRow++;
            }
            return startRow;
        }
        private static void GhiNgayDiaDiemC4(IXLWorksheet ws, ThongTinXuat tt)
        {
            var c = ws.Cell("C4");
            c.Value = $"{tt.DiaDiem}, ngày {tt.Ngay} tháng {tt.Thang} năm {tt.Nam}";
            c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            c.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        private static void GhiTieuDeA6(IXLWorksheet ws, ThongTinXuat tt)
        {
            string tieuDe = TaoTieuDeBaoCao(tt);
            if (tieuDe.Length == 0) return;   // không có tiêu đề hợp lệ thì GIỮ NGUYÊN nội dung mẫu, không xoá trắng A6
            var rg = ws.Range("A6:G6");
            rg.Value = tieuDe.ToUpperInvariant();
            var st = rg.Style;
            st.Font.Bold = true;
            st.Font.FontName = Module_HeThong.Font_Times_New_Roman;
            st.Font.FontSize = 13;
            st.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            st.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        #endregion
        #region 6. Ánh xạ tên danh hiệu theo Chế độ xét NĂM
        public static string GetTenSheet(string pl) => pl switch
        {
            Module_HeThong.Loai_1 => "LOAI_1",
            Module_HeThong.Loai_2 => "LOAI_2",
            Module_HeThong.Loai_3 => "LOAI_3",
            Module_HeThong.Loai_4 => "LOAI_4",
            _ => "KHONG_PL"
        };
        public static void AnhXaTenCheDoCBCSVaNam(XLWorkbook wb, string singlePhanLoai = null)
        {
            try { AnhXaCheDoNamCore(wb, singlePhanLoai, Module_HeThong.IsCheDoXetThiDuaNam(), Module_HeThong.PL_KHONG_PL); }
            catch (Exception ex) { Debug.WriteLine($"Lỗi ánh xạ Chế độ Năm: {ex.Message}"); }
        }
        public static void AnhXaTenCheDoCBCSVaNam(XLWorkbook wb)
        {
            try { AnhXaCheDoNamCore(wb, null, Module_HeThong.IsCheDoXetThiDuaNam(), "Không xét"); }
            catch (Exception ex) { Debug.WriteLine($"Lỗi ánh xạ Chế độ Năm: {ex.Message}"); }
        }
        // Lõi duy nhất thay cho 2 bản sao trước đây (bản cũ còn gõ nhầm Loai_3 cho LOAI_4 ở từ khoá A6).
        private static void AnhXaCheDoNamCore(XLWorkbook wb, string singlePhanLoai, bool cheDoNam, string thayKhongPlCotJ)
        {
            if (wb == null || !cheDoNam) return;
            var cauHinh = new (string Sheet, string[] KhoaJ, string ThayJ, string[] KhoaA6, string ThayA6)[]
            {
                ("LOAI_1", new[] { Module_HeThong.Loai_1, "LOẠI 1" }, Module_HeThong.PL_CSTD, new[] { "LOẠI 1", Module_HeThong.Loai_1 }, Module_HeThong.PL_CSTD),
                ("LOAI_2", new[] { Module_HeThong.Loai_2, "LOẠI 2" }, Module_HeThong.PL_CSTT, new[] { "LOẠI 2", Module_HeThong.Loai_2 }, Module_HeThong.PL_CSTT),
                ("LOAI_3", new[] { Module_HeThong.Loai_3, "LOẠI 3" }, Module_HeThong.PL_HTNV, new[] { "LOẠI 3", Module_HeThong.Loai_3 }, Module_HeThong.PL_HTNV),
                ("LOAI_4", new[] { Module_HeThong.Loai_4, "LOẠI 4" }, Module_HeThong.PL_KHTNV, new[] { "LOẠI 4", Module_HeThong.Loai_4 }, Module_HeThong.PL_KHTNV),
                ("KHONG_PL", new[] { "Không PL", "KHÔNG PL", "Chưa PL", Module_HeThong.PL_KHONG_PL }, thayKhongPlCotJ, new[] { "Không PL", "KHÔNG PL" }, "KHÔNG PHÂN LOẠI")
            };
            string sheetDich = string.IsNullOrEmpty(singlePhanLoai) ? null : GetTenSheet(singlePhanLoai);
            foreach (var cfg in cauHinh)
            {
                if (sheetDich != null && !cfg.Sheet.Equals(sheetDich, StringComparison.OrdinalIgnoreCase)) continue;
                if (!wb.Worksheets.Contains(cfg.Sheet)) continue;
                var ws = wb.Worksheet(cfg.Sheet);
                // A6: tiêu đề danh sách
                var cellA6 = ws.Cell("A6");
                string a6 = cellA6.GetString();
                if (a6.Length > 0)
                {
                    string moi = a6;
                    foreach (string tk in cfg.KhoaA6)
                    {
                        if (string.IsNullOrEmpty(tk)) continue;     // từ khoá rỗng sẽ thay thế tràn lan => bỏ qua
                        if (moi.Contains(tk, StringComparison.OrdinalIgnoreCase))
                        {
                            moi = moi.Replace(tk, cfg.ThayA6, StringComparison.OrdinalIgnoreCase);
                            break;
                        }
                    }
                    if (!ReferenceEquals(moi, a6) && moi != a6) cellA6.SetValue(moi);
                }
                // Cột J (từ dòng 10): chỉ duyệt các ô có dữ liệu
                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                if (lastRow < 10) continue;
                foreach (var cellJ in ws.Range(10, 10, lastRow, 10).CellsUsed().ToList())
                {
                    string v = cellJ.GetString().Trim();
                    if (v.Length == 0) continue;
                    foreach (string tk in cfg.KhoaJ)
                    {
                        if (string.IsNullOrEmpty(tk)) continue;
                        if (v.Contains(tk, StringComparison.OrdinalIgnoreCase))
                        {
                            cellJ.SetValue(cfg.ThayJ);
                            break;
                        }
                    }
                }
            }
            // Sheet tổng hợp (chỉ khi xuất tất cả)
            if (sheetDich == null && wb.Worksheets.Contains("BAOCAOTONGHOP_NAM"))
            {
                var wsTH = wb.Worksheet("BAOCAOTONGHOP_NAM");
                wsTH.Cell("E10").SetValue(Module_HeThong.PL_CSTD);
                wsTH.Cell("F10").SetValue(Module_HeThong.PL_CSTT);
                wsTH.Cell("G10").SetValue(Module_HeThong.PL_HTNV);
                wsTH.Cell("H10").SetValue(Module_HeThong.PL_KHTNV);
            }
        }
        #endregion
        #region 7. Tính phân bổ BCH
        public static void TinhVaGhiChiHuyTinhToan(XLWorkbook wb)
        {
            if (wb == null) return;
            var gm = new GiaiMaCache();
            int tongBCH = 0;
            ThongTinXuat tt;
            ThamSoBCH ts;
            using (var conn = MoKetNoi(Module_DanduongGPS.DuongDanCSDL2))
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT DonVi FROM DanhSach";
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        string dv = gm.Lay(rd.IsDBNull(0) ? "" : rd.GetValue(0).ToString());
                        if (dv.Equals("BCH", StringComparison.OrdinalIgnoreCase)) tongBCH++;
                    }
                }
                tt = DocThongTinXuat(conn, gm, false, false, false);
                ts = DocThamSoBCH(conn, gm, tt);
            }
            TinhVaGhiChiHuyCore(wb, tongBCH, ts, tt, Module_HeThong.IsCheDoXetThiDuaNam());
        }
        private static void TinhVaGhiChiHuyCore(XLWorkbook wb, int tongBCH, ThamSoBCH ts, ThongTinXuat tt, bool cheDoNam)
        {
            soLuongBCHD = tongBCH;
            soLuongBCHDLoai1 = 0;
            soLuongBCHDLoai2 = 0;
            soLuongBCHDLoai3 = 0;
            soLuongBCHDLoai4 = 0;
            soLuongBCHDKhongPhanLoai = 0;
            if (tongBCH > 0)
            {
                // Thuật toán trích lập tầng
                int mocTranLoai2 = (int)Math.Round(tongBCH * ts.L2, CheDoLamTron);
                soLuongBCHDLoai1 = (int)Math.Round(mocTranLoai2 * ts.L1, CheDoLamTron);
                soLuongBCHDLoai2 = Math.Max(0, mocTranLoai2 - soLuongBCHDLoai1);
                int mocTranLoai3 = (int)Math.Round(tongBCH * ts.L3, CheDoLamTron);
                soLuongBCHDLoai3 = Math.Max(0, mocTranLoai3 - (soLuongBCHDLoai1 + soLuongBCHDLoai2));
                int mocTranLoai4 = (int)Math.Round(tongBCH * ts.L4, CheDoLamTron);
                soLuongBCHDLoai4 = Math.Max(0, mocTranLoai4 - (soLuongBCHDLoai1 + soLuongBCHDLoai2 + soLuongBCHDLoai3));
                soLuongBCHDKhongPhanLoai = (int)Math.Round(tongBCH * ts.KPL, CheDoLamTron);
                // Bù trừ sai số làm tròn
                int tongPhanBo = soLuongBCHDLoai1 + soLuongBCHDLoai2 + soLuongBCHDLoai3 + soLuongBCHDLoai4 + soLuongBCHDKhongPhanLoai;
                if (tongPhanBo != tongBCH && tongPhanBo > 0)
                    soLuongBCHDLoai2 = Math.Max(0, soLuongBCHDLoai2 + (tongBCH - tongPhanBo));
            }
            var ws = wb.Worksheet("DE XUAT");
            GhiTieuDeA6(ws, tt);
            if (tongBCH == 0)
            {
                ws.Row(11).Delete();
                return;
            }
            var range = ws.Range("A11:G11");
            range.Clear(XLClearOptions.Contents);
            range.Merge();
            string deNghiRaw = ts.LoaiDeNghi;
            string tenLoaiHienThi = "...";
            if (!string.IsNullOrWhiteSpace(deNghiRaw))
            {
                if (cheDoNam)
                {
                    tenLoaiHienThi = deNghiRaw.Trim().ToUpperInvariant() switch
                    {
                        "LOẠI 1" or "LOAI 1" => Module_HeThong.Loai_1,
                        "LOẠI 2" or "LOAI 2" => Module_HeThong.Loai_2,
                        "LOẠI 3" or "LOAI 3" => Module_HeThong.Loai_3,
                        "LOẠI 4" or "LOAI 4" => Module_HeThong.Loai_4,
                        "KHÔNG PL" or "KHONG PL" or "KPL" or "KHÔNG PHÂN LOẠI" => Module_HeThong.PL_KHONG_PL,
                        _ => deNghiRaw
                    };
                }
                else tenLoaiHienThi = VietHoaChuDau(deNghiRaw.Trim(), "...");
            }
            int tileHienThi = (int)Math.Round((double)soLuongBCHDLoai1 / tongBCH * 100, CheDoLamTron);
            string line1 = $"Tập thể {ts.KyHieuTieuDoan} đạt {tenLoaiHienThi} thì BCH xét {tileHienThi}%";
            string line2 = $"({Module_HeThong.Loai_1}: {soLuongBCHDLoai1} {Module_HeThong.Tu_dong_chi}; " +
                           $"{Module_HeThong.Loai_2}: {soLuongBCHDLoai2} {Module_HeThong.Tu_dong_chi}; " +
                           $"{Module_HeThong.Loai_3}: {soLuongBCHDLoai3} {Module_HeThong.Tu_dong_chi})";
            var cell = ws.Cell(11, 1);
            cell.Value = $"{line1}\n{line2}";
            var st = cell.Style;
            st.Font.Bold = true;
            st.Font.FontName = Module_HeThong.Font_Times_New_Roman;
            st.Font.FontSize = 13;
            st.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            st.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            st.Fill.BackgroundColor = XLColor.LightGray;
            st.Alignment.WrapText = true;
            ws.Row(11).Height = 36;   // cố định cho 2 dòng chữ cỡ 13
        }
        #endregion
        #region 8. XUẤT LẺ MỘT PHÂN LOẠI
        public static void XuatPhanLoai(string phanLoai, int soThuTuFile)
        {
            try { HienKetQuaXuatPhanLoai(XuatPhanLoaiCore(ChupBoiCanh(), phanLoai)); }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        /// <summary>Bản chạy nền: giao diện không bị đơ khi xuất danh sách lớn. Gọi bằng await từ sự kiện Click.</summary>
        public static Task XuatPhanLoaiAsync(string phanLoai, int soThuTuFile = 0) =>
            ChayNenAsync(ctx => XuatPhanLoaiCore(ctx, phanLoai), HienKetQuaXuatPhanLoai, ex => MessageBox.Show(ex.Message));
        private static void HienKetQuaXuatPhanLoai(KetQuaXuat kq)
        {
            if (kq.ThongBaoKhongDuLieu != null) { Module_ThongBao.Loi(kq.ThongBaoKhongDuLieu); return; }
            GhiNhanTep(kq);
            Module_ThongBao.ThanhCong($"Đã xuất danh sách {kq.SoLuong} CBCS {kq.TenPhanLoai}!");
        }
        private static void GhiNhanTep(KetQuaXuat kq)
        {
            LastFilePath = kq.FilePath;
            LinkDanTep = kq.FilePath;
            if (string.IsNullOrEmpty(kq.ThuMuc)) return;
            try { Module_HeThong.GanIconThuMuc(kq.ThuMuc); }
            catch (Exception ex) { Debug.WriteLine("GanIconThuMuc: " + ex.Message); }
        }
        private static KetQuaXuat XuatPhanLoaiCore(BoiCanh ctx, string phanLoai)
        {
            phanLoai ??= "";
            var gm = new GiaiMaCache();
            // 1. Đọc dữ liệu + thông tin trong MỘT kết nối
            DataTable dt;
            ThongTinXuat tt;
            using (var conn = MoKetNoi(ctx.DuongDanDB))
            {
                dt = DocDanhSach(conn);
                tt = DocThongTinXuat(conn, gm, ctx.LaTanBinh, canNguoiKy: true, canDonViBanHanh: false);
            }
            // 2. Lọc theo phân loại (giải mã mỗi dòng đúng 1 lần)
            string muc = phanLoai.Trim();
            var data = new List<DataRow>();
            foreach (DataRow r in dt.Rows)
                if (gm.Lay(LayChuoi(r, "PhanLoai")).Equals(muc, StringComparison.OrdinalIgnoreCase)) data.Add(r);
            if (data.Count == 0)
                return TaoKetQuaKhongDuLieu($"Không có CBCS phân loại: {phanLoai}!");
            string thuMucGoc = GetLinkLuuDuongDanTepXuat();
            if (string.IsNullOrWhiteSpace(thuMucGoc)) throw new Exception("Bạn chưa chọn thư mục lưu!");
            if (string.IsNullOrWhiteSpace(ctx.FileMau) || !File.Exists(ctx.FileMau)) throw new Exception("Không tìm thấy file mẫu Excel!");
            string thuMucDich = Path.Combine(thuMucGoc, LamSachTenTep($"DANH SÁCH PHÂN LOẠI THI ĐUA THÁNG {tt.ThangHT} NĂM {tt.NamHT}"));
            Directory.CreateDirectory(thuMucDich);
            // Tên phân loại dùng cho tên tệp (có ánh xạ theo chế độ năm)
            string phanLoaiTenTep = gm.Lay(phanLoai);
            if (ctx.CheDoNam)
            {
                string pc = phanLoaiTenTep.ToUpperInvariant();
                if (pc.Contains("LOẠI 1") || pc.Contains("LOAI 1")) phanLoaiTenTep = Module_HeThong.PL_CSTD;
                else if (pc.Contains("LOẠI 2") || pc.Contains("LOAI 2")) phanLoaiTenTep = Module_HeThong.PL_CSTT;
                else if (pc.Contains("LOẠI 3") || pc.Contains("LOAI 3")) phanLoaiTenTep = Module_HeThong.PL_HTNV;
                else if (pc.Contains("LOẠI 4") || pc.Contains("LOAI 4")) phanLoaiTenTep = Module_HeThong.PL_KHTNV;
                else if (LaKhongPhanLoai(pc)) phanLoaiTenTep = "KHÔNG PHÂN LOẠI";
            }
            int sttFile = Directory.EnumerateFiles(thuMucDich, "*.xlsx").Count() + 1;
            string fileName = LamSachTenTep($"{sttFile}. DANH SÁCH {InHoa(phanLoaiTenTep)} CỦA {tt.TenTieuDoan} - {DateTime.Now:yyyyMMdd-HHmmss}.xlsx");
            string fileDich = Path.Combine(thuMucDich, fileName);
            using var wb = new XLWorkbook(ctx.FileMau);
            string sheetName = GetTenSheet(phanLoai);
            if (!wb.Worksheets.Contains(sheetName)) throw new Exception($"Sheet '{sheetName}' không tồn tại trong file mẫu.");
            var ws = wb.Worksheet(sheetName);
            foreach (var sht in wb.Worksheets.ToList())
                if (sht.Name != sheetName && sht.Name != "GIOI_THIEU") wb.Worksheets.Delete(sht.Name);
            // 3. Bảng dữ liệu + tổng cộng
            GhiSheetCBCS(ws, dt, data, gm, ctx.LaTanBinh, 13, gm.Lay(phanLoai), out int dongTong);
            // 4. Chữ ký + tiêu đề (xuất lẻ: chức vụ IN HOA, gạch chân 1/2)
            GhiTieuDeVaKyTen(ws, tt, phanLoai, dongTong, inHoaChucVu: true, gachMotPhanHai: true, gm);
            AnhXaCheDoNamCore(wb, phanLoai, ctx.CheDoNam, Module_HeThong.PL_KHONG_PL);
            Module_BanQuyen.DongDauExcel(wb);
            LuuAnToan(wb, fileDich);
            return new KetQuaXuat { FilePath = fileDich, ThuMuc = thuMucDich, SoLuong = data.Count, TenPhanLoai = phanLoaiTenTep };
        }
        #endregion
        #region 9. XUẤT TẤT CẢ PHÂN LOẠI
        public static void VietDuLieuCBCSVaoSheet(DataTable dt, List<DataRow> data, IXLWorksheet ws)
        {
            var gm = new GiaiMaCache();
            GhiSheetCBCS(ws, dt, data, gm, false, 14, null, out int dongTong);
            string pl = data.Count > 0 ? gm.Lay(LayChuoi(data[0], "PhanLoai")) : "";
            VietTieuDeVaKyTen(ws, pl, dongTong, Module_DanduongGPS.DuongDanCSDL2);
        }
        public static void VietDuLieuTanBinhVaoSheet(DataTable dt, List<DataRow> data, IXLWorksheet ws)
        {
            var gm = new GiaiMaCache();
            if (!GhiSheetTanBinh(ws, data, gm, out int dongTong, out string pl)) return;
            VietTieuDeVaKyTen(ws, pl, dongTong, Module_DanduongGPS.DuongDanCSDL2);
        }
        private static void VietTieuDeVaKyTen(IXLWorksheet ws, string phanLoai, int lastRow, string fileDB)
        {
            var gm = new GiaiMaCache();
            bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem()?.Contains("tân binh", StringComparison.OrdinalIgnoreCase) == true;
            using var conn = MoKetNoi(fileDB);
            var tt = DocThongTinXuat(conn, gm, laTanBinh, canNguoiKy: true, canDonViBanHanh: false);
            GhiTieuDeVaKyTen(ws, tt, phanLoai, lastRow, inHoaChucVu: false, gachMotPhanHai: false, gm);
        }
        public static void XuatTatCaPhanLoai()
        {
            try
            {
                var kq = TaoFileTatCaCore(ChupBoiCanh(), null);
                GhiNhanTep(kq);
                // Thông báo thành công do nơi gọi đảm nhiệm (tránh thông báo 2 lần)
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        /// <summary>Xuất thẳng ra đường dẫn chỉ định (không còn tạo tệp thừa rồi copy/xoá như bản cũ).</summary>
        public static void XuatTatCaPhanLoai(string fileXuat)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileXuat)) throw new Exception("Đường dẫn file xuất không hợp lệ.");
                HienKetQuaTatCa(TaoFileTatCaCore(ChupBoiCanh(), fileXuat));
            }
            catch (Exception ex) { Module_ThongBao.Loi("Lỗi xuất tất cả phân loại:\n" + ex.Message); }
        }
        public static Task XuatTatCaPhanLoaiAsync(string fileXuat = null) =>
            ChayNenAsync(ctx => TaoFileTatCaCore(ctx, fileXuat),
                kq => { if (string.IsNullOrWhiteSpace(fileXuat)) GhiNhanTep(kq); else HienKetQuaTatCa(kq); },
                ex => Module_ThongBao.Loi("Lỗi xuất tất cả phân loại:\n" + ex.Message));
        private static void HienKetQuaTatCa(KetQuaXuat kq)
        {
            GhiNhanTep(kq);
            Module_ThongBao.ThanhCong("Xuất tất cả phân loại thành công!");
        }
        private static KetQuaXuat TaoFileTatCaCore(BoiCanh ctx, string fileXuat)
        {
            var gm = new GiaiMaCache();
            DataTable dt;
            ThongTinXuat tt;
            using (var conn = MoKetNoi(ctx.DuongDanDB))
            {
                dt = DocDanhSach(conn);
                tt = DocThongTinXuat(conn, gm, ctx.LaTanBinh, canNguoiKy: true, canDonViBanHanh: false);
            }
            string thuMucDich, fileDich;
            if (string.IsNullOrWhiteSpace(fileXuat))
            {
                string thuMucGoc = GetLinkLuuDuongDanTepXuat(true);
                if (string.IsNullOrWhiteSpace(thuMucGoc)) throw new Exception("Bạn chưa chọn thư mục lưu!");
                thuMucDich = Path.Combine(thuMucGoc, LamSachTenTep($"DANH SÁCH PHÂN LOẠI THI ĐUA THÁNG {tt.ThangHT} NĂM {DateTime.Now.Year}"));
                Directory.CreateDirectory(thuMucDich);
                int sttFile = Directory.EnumerateFiles(thuMucDich, "*.xlsx").Count() + 1;
                fileDich = Path.Combine(thuMucDich, LamSachTenTep($"{sttFile}. DANH SÁCH TẤT CẢ PHÂN LOẠI - {DateTime.Now:yyyyMMdd-HHmmss}.xlsx"));
            }
            else
            {
                fileDich = Path.GetFullPath(fileXuat);
                Directory.CreateDirectory(Path.GetDirectoryName(fileDich));
                thuMucDich = "";
            }
            if (string.IsNullOrWhiteSpace(ctx.FileMau) || !File.Exists(ctx.FileMau)) throw new Exception("Không tìm thấy file mẫu Excel!");
            using var wb = new XLWorkbook(ctx.FileMau);
            string tenSheetTongHop = ctx.CheDoNam ? "BAOCAOTONGHOP_NAM" : "BAO CAO TONG HOP";
            if (wb.Worksheets.Contains(tenSheetTongHop))
            {
                var wsTH = wb.Worksheet(tenSheetTongHop);
                var range = wsTH.Range("L11:L12");
                if (!range.IsMerged()) range.Merge();
                var cell = wsTH.Cell("L11");
                cell.Value = tt.TomTatGhiChu;
                cell.Style.NumberFormat.Format = "@";
                cell.Style.Alignment.WrapText = true;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            }
            // Gom nhóm theo phân loại MỘT lần (bản cũ lọc + giải mã lại toàn bộ bảng cho từng loại)
            var theoLoai = new Dictionary<string, List<DataRow>>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in dt.Rows)
            {
                string k = gm.Lay(LayChuoi(r, "PhanLoai"));
                if (!theoLoai.TryGetValue(k, out var ds)) theoLoai[k] = ds = new List<DataRow>();
                ds.Add(r);
            }
            string[] phanLoaiArr = { Module_HeThong.Loai_1, Module_HeThong.Loai_2, Module_HeThong.Loai_3, Module_HeThong.Loai_4, Module_HeThong.PL_KHONG_PL };
            var usedSheets = new HashSet<string>(StringComparer.Ordinal);
            foreach (string pl in phanLoaiArr)
            {
                if (!theoLoai.TryGetValue(pl.Trim(), out var data) || data.Count == 0) continue;
                string sheetName = GetTenSheet(pl);
                IXLWorksheet ws = wb.Worksheets.Contains(sheetName) ? wb.Worksheet(sheetName) : wb.AddWorksheet(sheetName);
                int dongTong;
                string phanLoaiHeader;
                if (ctx.LaTanBinh)
                {
                    if (!GhiSheetTanBinh(ws, data, gm, out dongTong, out phanLoaiHeader)) continue;
                }
                else
                {
                    GhiSheetCBCS(ws, dt, data, gm, false, 14, null, out dongTong);
                    phanLoaiHeader = gm.Lay(LayChuoi(data[0], "PhanLoai"));
                }
                GhiTieuDeVaKyTen(ws, tt, phanLoaiHeader, dongTong, inHoaChucVu: false, gachMotPhanHai: false, gm);
                usedSheets.Add(ws.Name);
            }
            AnhXaCheDoNamCore(wb, null, ctx.CheDoNam, "Không xét");
            foreach (var s in wb.Worksheets.Where(s => !usedSheets.Contains(s.Name) && s.Name != tenSheetTongHop && s.Name != "GIOI_THIEU").ToList())
                wb.Worksheets.Delete(s.Name);
            Module_BanQuyen.DongDauExcel(wb);
            LuuAnToan(wb, fileDich);
            return new KetQuaXuat { FilePath = fileDich, ThuMuc = thuMucDich };
        }
        #endregion
        #region 10. TRÌNH KÝ TÂN BINH
        public static void XuatTrinhKyTanBinh(string duongDanLuu, bool xoaXinYKien = false)
        {
            try { HienKetQuaTrinhKyTanBinh(TaoTrinhKyTanBinhCore(ChupBoiCanh(), duongDanLuu, xoaXinYKien)); }
            catch (Exception ex) { BaoLoiTrinhKyTanBinh(ex); }
        }
        public static Task XuatTrinhKyTanBinhAsync(string duongDanLuu, bool xoaXinYKien = false) =>
            ChayNenAsync(ctx => TaoTrinhKyTanBinhCore(ctx, duongDanLuu, xoaXinYKien), HienKetQuaTrinhKyTanBinh, BaoLoiTrinhKyTanBinh);
        private static void HienKetQuaTrinhKyTanBinh(KetQuaXuat kq)
        {
            if (kq.ThongBaoKhongDuLieu != null) { Module_ThongBao.DangXuLy(kq.ThongBaoKhongDuLieu); return; }
            GhiNhanTep(kq);
            MoThuMucVaChonTep(kq.FilePath);
            Module_ThongBao.ThanhCong("Xuất Trình Ký Tân binh thành công!");
        }
        private static void BaoLoiTrinhKyTanBinh(Exception ex)
        {
            Module_ThongBao.DangXuLy("Lỗi xuất Trình Ký:\n" + ex.Message);
            MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        private static KetQuaXuat TaoTrinhKyTanBinhCore(BoiCanh ctx, string duongDanLuu, bool xoaXinYKien)
        {
            var gm = new GiaiMaCache();
            DataTable dt, dtTyLe;
            ThongTinXuat tt;
            using (var conn = MoKetNoi(ctx.DuongDanDB))
            {
                dt = DocDanhSach(conn);
                if (dt.Rows.Count == 0) return TaoKetQuaKhongDuLieu("SDL không có dữ liệu để xuất!");
                tt = DocThongTinXuat(conn, gm, ctx.LaTanBinh, canNguoiKy: false, canDonViBanHanh: false);
                dtTyLe = DocBangTyLe(conn);
            }
            if (string.IsNullOrWhiteSpace(duongDanLuu)) throw new Exception("Bạn chưa chọn thư mục lưu!");
            if (string.IsNullOrWhiteSpace(ctx.FileMau) || !File.Exists(ctx.FileMau)) throw new Exception("Không tìm thấy file mẫu Excel!");
            string thuMucDich = Path.Combine(duongDanLuu, LamSachTenTep($"DANH SÁCH PHÂN LOẠI THI ĐUA THÁNG {tt.ThangHT} NĂM {DateTime.Now.Year}"));
            Directory.CreateDirectory(thuMucDich);
            int soThuTu = Directory.EnumerateFiles(thuMucDich, "*.xlsx").Count() + 1;
            string fileDich = Path.Combine(thuMucDich, $"{soThuTu}. DANH SÁCH TỜ TRÌNH - {DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            using var wb = new XLWorkbook(ctx.FileMau);
            // Giữ sheet DE XUAT + GIOI_THIEU (ẩn)
            foreach (var s in wb.Worksheets.Where(s =>
                         !s.Name.Equals("DE XUAT", StringComparison.OrdinalIgnoreCase) &&
                         !s.Name.Equals("GIOI_THIEU", StringComparison.OrdinalIgnoreCase) &&
                         s.Visibility == XLWorksheetVisibility.Visible).ToList())
                wb.Worksheets.Delete(s.Name);
            var ws = wb.Worksheet("DE XUAT");
            GhiTieuDeA6(ws, tt);
            // Xoá dữ liệu cũ + header
            int lastRow = ws.LastRowUsed()?.RowNumber() ?? 11;
            if (lastRow >= 12) ws.Rows(12, lastRow).Clear(XLClearOptions.Contents);
            var a1 = ws.Cell("A1");
            a1.Value = string.IsNullOrWhiteSpace(tt.TenTieuDoan) ? "" : tt.TenTieuDoan;
            a1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            GhiNgayDiaDiemC4(ws, tt);
            int row = 11;
            GhiDongNhom(ws, row, 7, "DANH SÁCH PHÂN LOẠI TÂN BINH", false);
            row++;
            // Giải mã PhanLoai đúng 1 lần/dòng rồi gom nhóm
            var theoLoai = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string k = gm.Lay(LayChuoi(dt.Rows[i], "PhanLoai")).ToUpperInvariant();
                if (!theoLoai.TryGetValue(k, out var ds)) theoLoai[k] = ds = new List<int>();
                ds.Add(i);
            }
            string[] cacLoai = { "LOẠI 1", "LOẠI 2", "LOẠI 3", "LOẠI 4", "KHÔNG PL" };
            foreach (string loai in cacLoai)
            {
                if (!theoLoai.TryGetValue(loai, out var chiMuc) || chiMuc.Count == 0) continue;
                GhiDongNhom(ws, row, 7, $"{Loai(loai)}: {chiMuc.Count} {Module_HeThong.Tu_dong_chi}", false);
                row++;
                int dauKhoi = row, stt = 1;
                var dongGhiChuDai = new List<int>();
                foreach (int i in chiMuc)
                {
                    DataRow r = dt.Rows[i];
                    ws.Cell(row, 1).Value = stt++;
                    ws.Cell(row, 2).Value = gm.Lay(LayChuoi(r, "HoVaTen"));
                    ws.Cell(row, 3).Value = gm.Lay(LayChuoi(r, "NamSinh"));
                    ws.Cell(row, 4).Value = gm.Lay(LayChuoi(r, "ChucVu"));
                    ws.Cell(row, 5).Value = gm.Lay(LayChuoi(r, "DonVi"));
                    ws.Cell(row, 6).Value = gm.Lay(LayChuoi(r, "PhanLoai"));
                    string ghiChu = gm.Lay(LayChuoi(r, "GhiChu"));
                    ws.Cell(row, 7).Value = ghiChu;
                    if (ghiChu.Length > 15) dongGhiChuDai.Add(row);
                    row++;
                }
                int cuoiKhoi = row - 1;
                DinhDangKhoiDuLieu(ws, dauKhoi, cuoiKhoi, 7);
                if (cuoiKhoi >= 13)   // căn trái cột Họ và tên từ B13 trở xuống
                    ws.Range(Math.Max(dauKhoi, 13), 2, cuoiKhoi, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                ThuNhoGhiChuDai(ws, dongGhiChuDai, 7);
            }
            // Tổng QS (A7) + Ghi chú tỷ lệ
            int startRow = GhiTongQSVaGhiChu(ws, dtTyLe, tt, "đồng chí",
                Module_HeThong.Loai_1, Module_HeThong.Loai_2, Module_HeThong.Loai_3, thayLoaiTrongMoTa: false);
            ws.Rows(12, startRow).AdjustToContents();   // gọi 1 lần (bản cũ gọi lại trong từng vòng lặp => chậm bình phương)
            if (xoaXinYKien)
            {
                int lastRowUsed = ws.LastRowUsed()?.RowNumber() ?? 12;
                for (int r = 12; r <= lastRowUsed; r++)
                    if (!ws.Cell(r, 7).IsMerged() && !ws.Cell(r, 2).IsEmpty())
                        ws.Cell(r, 7).Clear(XLClearOptions.Contents);
            }
            Module_BanQuyen.DongDauExcel(wb);
            LuuAnToan(wb, fileDich);
            return new KetQuaXuat { FilePath = fileDich, ThuMuc = thuMucDich, SoLuong = dt.Rows.Count };
        }
        #endregion
        #region 11. TRÌNH KÝ CBCS
        public static void XuatTrinhKyCBCS(string duongDanLuu)
        {
            try
            {
                NapThongTinDonVi();
                HienKetQuaTrinhKyCBCS(TaoTrinhKyCBCSCore(ChupBoiCanh(canDonViHopLe: true), duongDanLuu));
            }
            catch (Exception ex) { BaoLoiTrinhKyCBCS(ex); }
        }
        public static Task XuatTrinhKyCBCSAsync(string duongDanLuu) =>
            ChayNenAsync(ctx => TaoTrinhKyCBCSCore(ctx, duongDanLuu), HienKetQuaTrinhKyCBCS, BaoLoiTrinhKyCBCS, canDonViHopLe: true);
        private static void HienKetQuaTrinhKyCBCS(KetQuaXuat kq)
        {
            if (kq.ThongBaoKhongDuLieu != null) { Module_ThongBao.DangXuLy(kq.ThongBaoKhongDuLieu); return; }
            GhiNhanTep(kq);
            MoThuMucVaChonTep(kq.FilePath);
            Module_ThongBao.ThanhCong("Xuất Trình Ký thành công!");
        }
        private static void BaoLoiTrinhKyCBCS(Exception ex)
        {
            Module_ThongBao.Loi("Lỗi xuất Trình Ký:\n" + ex.Message);
            MessageBox.Show("Lỗi xuất Trình Ký: \n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        private static KetQuaXuat TaoTrinhKyCBCSCore(BoiCanh ctx, string duongDanLuu)
        {
            bool isCheDoNam = ctx.CheDoNam;
            string GetTenDanhHieu(string loaiGoc)
            {
                if (!isCheDoNam) return Loai(loaiGoc);
                return loaiGoc.ToUpperInvariant() switch
                {
                    "LOẠI 1" => Module_HeThong.PL_CSTD,
                    "LOẠI 2" => Module_HeThong.PL_CSTT,
                    "LOẠI 3" => Module_HeThong.PL_HTNV,
                    "LOẠI 4" => Module_HeThong.PL_KHTNV,
                    "KHÔNG PL" => "Không xét",
                    _ => loaiGoc
                };
            }
            var gm = new GiaiMaCache();
            DataTable dt, dtTyLe;
            ThongTinXuat tt;
            ThamSoBCH ts;
            using (var conn = MoKetNoi(ctx.DuongDanDB))
            {
                dt = DocDanhSach(conn);
                if (dt.Rows.Count == 0) return TaoKetQuaKhongDuLieu("SDL không có dữ liệu để xuất!");
                tt = DocThongTinXuat(conn, gm, false, canNguoiKy: false, canDonViBanHanh: true);
                ts = DocThamSoBCH(conn, gm, tt);
                dtTyLe = DocBangTyLe(conn);
            }
            if (string.IsNullOrWhiteSpace(duongDanLuu)) throw new Exception("Bạn chưa chọn thư mục lưu!");
            if (string.IsNullOrWhiteSpace(ctx.FileMau) || !File.Exists(ctx.FileMau)) throw new Exception("Không tìm thấy file mẫu Excel!");
            string tenThuMuc = isCheDoNam
                ? $"DANH SÁCH PHÂN LOẠI THI ĐUA NĂM {DateTime.Now.Year}"
                : $"DANH SÁCH PHÂN LOẠI THI ĐUA THÁNG {tt.ThangHT} NĂM {DateTime.Now.Year}";
            string thuMucDich = Path.Combine(duongDanLuu, LamSachTenTep(tenThuMuc));
            Directory.CreateDirectory(thuMucDich);
            int soThuTu = Directory.EnumerateFiles(thuMucDich, "*.xlsx").Count() + 1;
            string fileDich = Path.Combine(thuMucDich, $"{soThuTu}. DANH SÁCH TỜ TRÌNH - {DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            // ===== Giải mã PhanLoai/DonVi + khoá STT đúng 1 lần/dòng, rồi gom nhóm (bản cũ giải mã lại ~5 lần/dòng/cột) =====
            int n = dt.Rows.Count;
            bool coSTT = dt.Columns.Contains("STT");
            var chiMucBCH = new List<int>();
            var theoLoai = new Dictionary<string, List<int>>(StringComparer.Ordinal);   // chỉ nhóm KHÔNG BCH
            var plUp = new string[n];
            var dvUp = new string[n];
            var khoaSTT = new int[n];
            for (int i = 0; i < n; i++)
            {
                DataRow r = dt.Rows[i];
                plUp[i] = gm.Lay(LayChuoi(r, "PhanLoai")).ToUpperInvariant();
                dvUp[i] = gm.Lay(LayChuoi(r, "DonVi")).ToUpperInvariant();
                khoaSTT[i] = KhoaSTT(r, coSTT);
                if (dvUp[i] == "BCH") chiMucBCH.Add(i);
                else
                {
                    if (!theoLoai.TryGetValue(plUp[i], out var ds)) theoLoai[plUp[i]] = ds = new List<int>();
                    ds.Add(i);
                }
            }
            var donViHopLe = new HashSet<string>(ctx.DonViHopLe, StringComparer.Ordinal);
            using var wb = new XLWorkbook(ctx.FileMau);
            foreach (var s in wb.Worksheets.Where(s => !s.Name.Equals("DE XUAT", StringComparison.OrdinalIgnoreCase)).ToList())
                wb.Worksheets.Delete(s.Name);
            var ws = wb.Worksheet("DE XUAT");
            // ===== Đầu trang =====
            GhiO(ws.Cell("A1"), string.IsNullOrWhiteSpace(tt.TenTieuDoan) ? KhoangTrang : tt.TenTieuDoan, 13);
            GachChanTenDonViBanHanh(ws.Range("A2:B2"), tt.TenDonViBanHanh);
            GhiNgayDiaDiemC4(ws, tt);
            ws.Cell("A6").Value = isCheDoNam
                ? $"DANH SÁCH ĐỀ NGHỊ XÉT PHÂN LOẠI THI ĐUA NĂM {tt.Nam}"
                : $"DANH SÁCH ĐỀ NGHỊ XÉT PHÂN LOẠI THI ĐUA THÁNG {tt.Thang}/{tt.Nam}";
            int lastRow = ws.LastRowUsed()?.RowNumber() ?? 11;
            if (lastRow >= 12) ws.Rows(12, lastRow).Clear(XLClearOptions.Contents);
            // ===== Phân bổ BCH (dùng luôn số BCH đã đếm, không đọc lại CSDL) =====
            TinhVaGhiChiHuyCore(wb, chiMucBCH.Count, ts, tt, isCheDoNam);
            if (isCheDoNam)
            {
                var cellA10 = ws.Cell("A10");
                string textA10 = cellA10.GetString();
                if (textA10.Length > 0)
                {
                    cellA10.Value = textA10.Replace(Module_HeThong.Loai_1, Module_HeThong.PL_CSTD)
                                           .Replace(Module_HeThong.Loai_2, Module_HeThong.PL_CSTT)
                                           .Replace(Module_HeThong.Loai_3, Module_HeThong.PL_HTNV)
                                           .Replace(Module_HeThong.Loai_4, Module_HeThong.PL_KHTNV);
                }
            }
            // ===== Danh sách BCH =====
            int row = 12, stt = 1;
            int dauBCH = row;
            foreach (int i in chiMucBCH)
            {
                DataRow r = dt.Rows[i];
                ws.Cell(row, 1).Value = stt++;
                ws.Cell(row, 2).Value = gm.Lay(LayChuoi(r, "HoVaTen"));
                ws.Cell(row, 3).Value = gm.Lay(LayChuoi(r, "NamSinh"));
                ws.Cell(row, 4).Value = gm.Lay(LayChuoi(r, "ChucVu"));
                ws.Cell(row, 5).Value = gm.Lay(LayChuoi(r, "DonVi"));
                ws.Cell(row, 6).Value = "";          // Đề xuất
                ws.Cell(row, 7).Value = "Họp xét";   // Xin ý kiến
                row++;
            }
            DinhDangKhoiDuLieu(ws, dauBCH, row - 1, 7);
            GhiDongNhom(ws, row, 7, "DANH SÁCH PHÂN LOẠI CBCS", true);
            row++;
            // ===== Từng loại =====
            string[] cacLoai = { "LOẠI 1", "LOẠI 2", "LOẠI 3", "LOẠI 4", "KHÔNG PL" };
            foreach (string loai in cacLoai)
            {
                theoLoai.TryGetValue(loai, out var chiMucLoai);
                int soLuongKhongBCH = chiMucLoai?.Count ?? 0;
                int soLuongBCHTheoLoai = chiMucBCH.Count == 0 ? 0 : loai switch
                {
                    "LOẠI 1" => soLuongBCHDLoai1,
                    "LOẠI 2" => soLuongBCHDLoai2,
                    "LOẠI 3" => soLuongBCHDLoai3,
                    "LOẠI 4" => soLuongBCHDLoai4,
                    "KHÔNG PL" => soLuongBCHDKhongPhanLoai,
                    _ => 0
                };
                if (soLuongKhongBCH == 0 && soLuongBCHTheoLoai == 0) continue;
                string tenDanhHieu = GetTenDanhHieu(loai);
                GhiDongNhom(ws, row, 7, $"{tenDanhHieu}: {soLuongKhongBCH} {Module_HeThong.Tu_dong_chi}", true);
                row++;
                int dauKhoi = row, sttChung = 1;
                // BCH đề nghị (dòng trống điền sau)
                for (int i = 0; i < soLuongBCHTheoLoai; i++)
                {
                    ws.Cell(row, 1).Value = sttChung++;
                    ws.Cell(row, 2).Value = "";
                    ws.Cell(row, 3).Value = "";
                    ws.Cell(row, 4).Value = "";
                    ws.Cell(row, 5).Value = "BCH";
                    ws.Cell(row, 6).Value = tenDanhHieu;
                    ws.Cell(row, 7).Value = "Họp xét";
                    row++;
                }
                // CBCS (không BCH), thuộc đơn vị hợp lệ, theo STT tăng dần
                var dsCBCS = (chiMucLoai ?? new List<int>())
                    .Where(i => donViHopLe.Contains(dvUp[i]))
                    .OrderBy(i => khoaSTT[i])
                    .ToList();
                var dongGhiChuDai = new List<int>();
                foreach (int i in dsCBCS)
                {
                    DataRow r = dt.Rows[i];
                    ws.Cell(row, 1).Value = sttChung++;
                    ws.Cell(row, 2).Value = gm.Lay(LayChuoi(r, "HoVaTen"));
                    ws.Cell(row, 3).Value = gm.Lay(LayChuoi(r, "NamSinh"));
                    ws.Cell(row, 4).Value = gm.Lay(LayChuoi(r, "ChucVu"));
                    ws.Cell(row, 5).Value = gm.Lay(LayChuoi(r, "DonVi"));
                    ws.Cell(row, 6).Value = tenDanhHieu;
                    string ghiChu = gm.Lay(LayChuoi(r, "GhiChu"));
                    ws.Cell(row, 7).Value = ghiChu;
                    if (ghiChu.Length > 15) dongGhiChuDai.Add(row);
                    row++;
                }
                DinhDangKhoiDuLieu(ws, dauKhoi, row - 1, 7);
                ThuNhoGhiChuDai(ws, dongGhiChuDai, 7);
            }
            // Căn lề cột B
            int lastRowCotB = ws.Column(2).LastCellUsed()?.Address.RowNumber ?? 1;
            if (lastRowCotB >= 1)
                ws.Range(1, 2, Math.Min(9, lastRowCotB), 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            if (lastRowCotB >= 11)
                ws.Range(11, 2, lastRowCotB, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            // Tổng QS + ghi chú tỷ lệ
            string lblL1 = isCheDoNam ? Module_HeThong.PL_CSTD : Module_HeThong.Loai_1;
            string lblL2 = isCheDoNam ? Module_HeThong.PL_CSTT : Module_HeThong.Loai_2;
            string lblL3 = isCheDoNam ? Module_HeThong.PL_HTNV : Module_HeThong.Loai_3;
            int startRow = GhiTongQSVaGhiChu(ws, dtTyLe, tt, Module_HeThong.Tu_dong_chi, lblL1, lblL2, lblL3, thayLoaiTrongMoTa: isCheDoNam);
            // GIỮ NGUYÊN: TinhVaGhiChiHuyCore đã xoá dòng 11 khi không có BCH; lệnh xoá thứ hai dưới đây khép kín khoảng trống do dữ liệu bắt đầu từ dòng 12.
            if (chiMucBCH.Count == 0) ws.Row(11).Delete();
            ws.Rows(12, startRow).AdjustToContents();
            Module_BanQuyen.DongDauExcel(wb);
            AnhXaCheDoNamCore(wb, null, isCheDoNam, "Không xét");
            LuuAnToan(wb, fileDich);
            return new KetQuaXuat { FilePath = fileDich, ThuMuc = thuMucDich, SoLuong = n };
        }
        #endregion
        #region 12. Đường dẫn lưu tệp, tiêu đề, tháng/năm hệ thống
        public static string GetLinkLuuDuongDanTepXuat(bool forceReload = false)
        {
            if (forceReload || string.IsNullOrWhiteSpace(LinkLuuDuongDanTepXuat) || _canReloadLink)
                NapLinkLuuDuongDanTepXuat();   // cờ _canReloadLink do chính hàm này đặt (bản cũ bị ghi đè ngay sau khi lỗi)
            return LinkLuuDuongDanTepXuat ?? string.Empty;
        }
        public static void NapLinkLuuDuongDanTepXuat()
        {
            string csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
            if (string.IsNullOrWhiteSpace(csdl2Path))
            {
                LinkLuuDuongDanTepXuat = string.Empty;
                return;
            }
            try
            {
                using var conn = MoKetNoi(csdl2Path);
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT ChonDuongDanXuatTep FROM ThongTin WHERE ID=1";
                object result = cmd.ExecuteScalar();
                string giaiMa = result == null || result == DBNull.Value ? "" : new GiaiMaCache().Lay(result.ToString());
                LinkLuuDuongDanTepXuat = !string.IsNullOrWhiteSpace(giaiMa) && Directory.Exists(giaiMa) ? giaiMa : string.Empty;
                _canReloadLink = false;
            }
            catch (Exception ex)
            {
                // ghi log nội bộ, KHÔNG làm rối UX
                GhiNhatKyLoi("Lỗi đọc đường dẫn xuất", ex);
                LinkLuuDuongDanTepXuat = string.Empty;
                _canReloadLink = true; // cho phép lần sau đọc lại
            }
        }
        public static string InHoa(string input)
            => string.IsNullOrWhiteSpace(input) ? "" : input.ToUpperInvariant();
        private static string Loai(string loai)
        {
            if (string.IsNullOrWhiteSpace(loai)) return "";
            loai = loai.Trim().ToLowerInvariant();            // "loại 1"
            return char.ToUpperInvariant(loai[0]) + loai.Substring(1);   // "Loại 1"
        }
        public static string LayTieuDeBaoCao()
        {
            try
            {
                using var conn = MoKetNoi(Module_DanduongGPS.DuongDanCSDL2);
                return TaoTieuDeBaoCao(DocThongTinXuat(conn, new GiaiMaCache(), false, false, false));
            }
            catch { return ""; }
        }
        public static string LayTenFileBaoCao()
        {
            try
            {
                using var conn = MoKetNoi(Module_DanduongGPS.DuongDanCSDL2);
                var tt = DocThongTinXuat(conn, new GiaiMaCache(), false, false, false);
                if (tt.LoaiBaoCao.Equals("Tuần", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(tt.ChonTuan))
                    return LamSachTenTep($"DANH SÁCH PHÂN LOẠI THI ĐUA {tt.ChonTuan.ToUpperInvariant()} THÁNG {tt.ThangHT}-{tt.NamGoc}.xlsx");
                return LamSachTenTep($"DANH SÁCH PHÂN LOẠI THI ĐUA THÁNG {tt.ThangHT}-{tt.NamGoc}.xlsx");
            }
            catch
            {
                return $"PHAN_LOAI_THI_DUA_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            }
        }
        // Chỉ giữ số 0 cho tháng 1 và 2. Từ tháng 3-12 trả về số tự nhiên.
        public static string LayThangHeThong()
        {
            string db = Module_DanduongGPS.DuongDanCSDL2;
            DamBaoBangThangHeThong(db);
            try
            {
                using var conn = MoKetNoi(db);
                return DocThangHeThong(conn);
            }
            catch
            {
                return DinhDangThangHeThong(DateTime.Now.Month.ToString(CultureInfo.InvariantCulture));
            }
        }
        public static string LayNamHeThong()
        {
            try
            {
                using var conn = MoKetNoi(Module_DanduongGPS.DuongDanCSDL2);
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Nam FROM ThongTin WHERE ID = 1";
                object res = cmd.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                {
                    string nam = new GiaiMaCache().Lay(res.ToString());
                    if (nam.Length > 0) return nam;
                }
            }
            catch { }
            // Chỉ lấy năm của máy tính làm phương án dự phòng cuối cùng nếu CSDL hỏng
            return DateTime.Now.Year.ToString(CultureInfo.InvariantCulture);
        }
        #endregion
        #region 13. Chạy nền an toàn (cho các hàm ...Async)
        private static async Task ChayNenAsync<T>(Func<BoiCanh, T> viecNen, Action<T> hoanTat, Action<Exception> baoLoi, bool canDonViHopLe = false)
        {
            if (Interlocked.CompareExchange(ref _dangXuat, 1, 0) != 0)
            {
                Module_ThongBao.DangXuLy("Đang xuất tệp, vui lòng chờ hoàn tất...");
                return;
            }
            try
            {
                var ctx = ChupBoiCanh(canDonViHopLe);                       // đọc trạng thái UI trên luồng giao diện
                T kq = await Task.Run(() => viecNen(ctx));                  // xử lý nặng ở luồng nền
                hoanTat(kq);                                                // quay lại luồng giao diện: thông báo, mở thư mục...
            }
            catch (Exception ex) { baoLoi(ex); }
            finally { Volatile.Write(ref _dangXuat, 0); }
        }
        #endregion
    }
    //Data Transfer Object (DTO) dùng cho InsertData
    public class CbcDTO
    {
        public int STT { get; set; }
        public string HoVaTen { get; set; }
        public string SoHieuCAND { get; set; }
        public string NamSinh { get; set; }
        public string QueQuan { get; set; }
        public string NgayVaoCAND { get; set; }
        public string CapBac { get; set; }
        public string ChucVu { get; set; }
        public string DonVi { get; set; }
        public string PhanLoai { get; set; }
        public string GhiChu { get; set; }
    }
}