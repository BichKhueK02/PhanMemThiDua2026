using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using PhanMemThiDua2026;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
public class CbcDTO
{
    public int STT { get; set; }
    public string HoVaTen { get; set; } = string.Empty;
    public string SoHieu { get; set; } = string.Empty;
    public string NamSinh { get; set; } = string.Empty;
    public string QueQuan { get; set; } = string.Empty;
    public string NgayVaoCAND { get; set; } = string.Empty;
    public string CapBac { get; set; } = string.Empty;
    public string ChucVu { get; set; } = string.Empty;
    public string DonVi { get; set; } = string.Empty;
    public string PhanLoai { get; set; } = string.Empty;
    public string ThanhTich { get; set; } = string.Empty;
    public string GhiChu { get; set; } = string.Empty;
}
// Dùng cho Form42 - Xuất danh sách gốc (Không cần điều kiện)
public class DanhSachGocBaNhatDTO
{
    public int ID { get; set; }
    public string HoVaTen { get; set; } = string.Empty;
    public string SoHieu { get; set; } = string.Empty;
    public string NamSinh { get; set; } = string.Empty;
    public string QueQuan { get; set; } = string.Empty;
    public string NgayVaoCAND { get; set; } = string.Empty;
    public string CapBac { get; set; } = string.Empty;
    public string ChucVu { get; set; } = string.Empty;
    public string DonVi { get; set; } = string.Empty;
    public string PhanLoai { get; set; } = string.Empty;
    public string GhiChu { get; set; } = string.Empty;
    public string DeNghi { get; set; } = string.Empty;
    public string ThanhTich { get; set; } = string.Empty;
}
// Dùng cho Form42 - Xuất danh sách đề nghị (Có dùng File Mẫu)
public class DeNghiBaNhatDTO
{
    public int ID { get; set; }
    public string HoVaTen { get; set; } = string.Empty;
    public string SoHieu { get; set; } = string.Empty;
    public string NamSinh { get; set; } = string.Empty;
    public string NgayVaoCAND { get; set; } = string.Empty;
    public string CapBac { get; set; } = string.Empty;
    public string ChucVu { get; set; } = string.Empty;
    public string DonVi { get; set; } = string.Empty;
    public string PhanLoai { get; set; } = string.Empty;
    public string ThanhTich { get; set; } = string.Empty;
}
// Dùng cho Form44 - Xuất Sổ vàng (Chỉ có 5 Cột)
public class SoVangDTO
{
    public int STT { get; set; }
    public string HoVaTen { get; set; } = string.Empty;
    public string ThongBaoTrungDoan { get; set; } = string.Empty;
    public string SoTTTrongSo { get; set; } = string.Empty;
    public string ThangCongNhan { get; set; } = string.Empty;
}
public static class Module_BaNhat
{
    // Giữ lại để các nơi khác (nếu có) còn biên dịch được.
    public const int GioiHanToiDa = 3000;
    // Giới hạn an toàn khi NẠP tệp Excel (chống nạp nhầm tệp khổng lồ làm cạn RAM máy yếu).
    private const long TepExcelToiDaBytes = 50L * 1024 * 1024;
    private const int DongExcelToiDa = 50000;
    // ===================== P/Invoke mở Explorer và chọn tệp =====================
    [DllImport("shell32.dll", ExactSpelling = true)]
    private static extern void ILFree(IntPtr pidlList);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern IntPtr ILCreateFromPathW(string pszPath);
    [DllImport("shell32.dll", ExactSpelling = true)]
    private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, IntPtr apidl, int dwFlags);
    // ===================== HẰNG / CẤU TRÚC DÙNG CHUNG =====================
    private static readonly XLColor MauNenTieuDe = XLColor.FromHtml("#EBF1F5");
    private static readonly string[] TieuDeBaNhat =
        { "STT", "Họ và tên", "Số hiệu", "Năm sinh", "Quê quán", "Ngày vào CAND", "Cấp bậc", "Chức vụ", "Đơn vị", "Phân loại", "Ghi chú", "Đề nghị", "Thành tích" };
    private static readonly double[] DoRongBaNhat = { 6, 25, 12, 10, 25, 12, 14, 15, 15, 12, 20, 10, 35 };
    private static readonly int[] CotTraiBaNhat = { 2, 5, 11, 13 };
    private static readonly int[] CotVanBanBaNhat = { 3, 4, 6 };            // Số hiệu, Năm sinh, Ngày vào CAND: luôn là văn bản
    private static readonly string[] TieuDeSoVangGoc =
        { "STT", "Họ và tên", "Số hiệu", "Năm sinh", "Quê quán", "Ngày vào CAND", "Cấp bậc", "Chức vụ", "Đơn vị", "Phân loại", "Ghi chú", "Thành tích", "Thông báo E", "STT Ghi sổ", "Tháng công nhận" };
    private static readonly double[] DoRongSoVangGoc = { 6, 25, 12, 10, 25, 12, 14, 15, 15, 12, 20, 35, 15, 12, 15 };
    private static readonly int[] CotTraiSoVangGoc = { 2, 5, 11, 12 };
    private static readonly int[] CotVanBanSoVangGoc = { 3, 4, 6, 13, 14, 15 };
    // Cột dữ liệu (đã mã hóa) của bảng Sổ vàng và bảng Ba nhất khi nạp từ Excel
    private static readonly string[] CotSoVang =
        { "HoVaTen", "NamSinh", "QueQuan", "NgayVaoCAND", "CapBac", "ChucVu", "DonVi", "PhanLoai", "GhiChu", "ThanhTich", "ThongBaoTrungDoan", "SoTTTrongSo", "ThangCongNhan" };
    private static readonly string[] CotBaNhatCapNhat =
        { "HoVaTen", "NamSinh", "QueQuan", "NgayVaoCAND", "CapBac", "ChucVu", "DonVi", "PhanLoai", "GhiChu" };
    private static readonly string[] CotBaNhatThem =
        { "HoVaTen", "NamSinh", "QueQuan", "NgayVaoCAND", "CapBac", "ChucVu", "DonVi", "PhanLoai", "GhiChu", "DeNghi", "ThanhTich" };
    private static readonly string[] TenThamSo = Enumerable.Range(0, 16).Select(i => "@p" + i).ToArray();
    private static readonly HashSet<string> BangSoVangHopLe =
        new(StringComparer.Ordinal) { "DanhSach_SoVangBaNhat", "DanhSachTanBinh_SoVangBaNhat" };
    // Tên bảng theo phiên bản phần mềm. Chỉ có 2 bộ giá trị cố định => không thể bị chèn SQL.
    private readonly record struct BangDuLieu(string DanhSach, string SoVang, string TomTat);
    private static BangDuLieu ChonBang()
    {
        string pb = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
        return pb.Contains("tân binh", StringComparison.OrdinalIgnoreCase)
            ? new BangDuLieu("DanhSachBaNhat_TanBinh", "DanhSachTanBinh_SoVangBaNhat", "TomTatThanhTichBaNhat_TanBinh")
            : new BangDuLieu("DanhSachBaNhat", "DanhSach_SoVangBaNhat", "TomTatThanhTichBaNhat_CBCS");
    }
    // ===================== TIỆN ÍCH SQLITE / GIẢI MÃ =====================
    // Pooling=false: đóng kết nối là nhả hẳn tệp .db (không làm kẹt sao lưu/khôi phục). chiDoc=true: không thể ghi nhầm.
    private static SqliteConnection MoKetNoi(string dbPath, bool chiDoc)
    {
        var sb = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = chiDoc ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWrite,
            Pooling = false
        };
        var cn = new SqliteConnection(sb.ToString());
        try { cn.Open(); return cn; }
        catch { cn.Dispose(); throw; }
    }
    private static bool BangTonTai(SqliteConnection cn, string ten)
    {
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=@n LIMIT 1";
        cmd.Parameters.AddWithValue("@n", ten);
        return cmd.ExecuteScalar() != null;
    }
    private static string Doc(SqliteDataReader rd, int i) => rd.IsDBNull(i) ? "" : rd.GetString(i);
    // GiaiMa() không ném lỗi và đã có cache => chỉ cần bọc kiểm tra rỗng.
    private static string Giai(string? s) => string.IsNullOrWhiteSpace(s) ? "" : Module_BaoMatAES.GiaiMa(s).Trim();
    private static string GiaiCot(SqliteDataReader rd, int i) => Giai(Doc(rd, i));
    // Dữ liệu cũ có thể chưa mã hóa: giải mã được thì dùng, không thì dùng nguyên văn (trừ chuỗi "v2|..." hỏng => rỗng).
    private static string GiaiHoacGoc(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        string g = Module_BaoMatAES.GiaiMa(raw);
        if (g.Length > 0) return g.Trim();
        raw = raw.Trim();
        return raw.StartsWith("v2|", StringComparison.Ordinal) ? "" : raw;
    }
    private static int ToInt(object? v)
    {
        try { return v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v, CultureInfo.InvariantCulture); }
        catch { return 0; }
    }
    // ===================== TIỆN ÍCH EXCEL / FILE =====================
    private static string OrBlank(string? s) => string.IsNullOrWhiteSpace(s) ? "     " : s;
    private static string VietHoaChuDau(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = s.Trim().ToLowerInvariant();
        return char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
    private static string LamSachTenTep(string s)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s;
    }
    private static string Chuoi(IXLCell c)
    {
        try { return c.GetFormattedString().Trim(); }
        catch { return c.GetString().Trim(); }
    }
    // Ghi ra tệp tạm cùng thư mục rồi Move đè: mất điện/ngắt giữa chừng không để lại tệp cụt; tệp đang mở trong Excel => báo rõ.
    private static void LuuTepAnToan(XLWorkbook wb, string duongDan)
    {
        string thuMuc = Path.GetDirectoryName(Path.GetFullPath(duongDan)) ?? AppContext.BaseDirectory;
        string tam = Path.Combine(thuMuc, "~tmp_" + Guid.NewGuid().ToString("N") + ".xlsx");
        try
        {
            wb.SaveAs(tam);
            try { File.Move(tam, duongDan, true); }
            catch (IOException)
            {
                throw new IOException("Không ghi đè được tệp '" + Path.GetFileName(duongDan) + "'. Có thể tệp đang mở trong Excel — hãy đóng tệp rồi thử lại.");
            }
        }
        finally
        {
            try { if (File.Exists(tam)) File.Delete(tam); } catch { }
        }
    }
    private static string? KiemTraKichThuocTep(string path)
    {
        long len = new FileInfo(path).Length;
        return len > TepExcelToiDaBytes
            ? $"Tệp Excel quá lớn ({len / 1048576} MB). Giới hạn cho phép là {TepExcelToiDaBytes / 1048576} MB."
            : null;
    }
    private static string? KiemTraTieuDe(IXLWorksheet ws, int hang, string[] chuan)
    {
        var row = ws.Row(hang);
        for (int i = 0; i < chuan.Length; i++)
        {
            string h = row.Cell(i + 1).GetString().Trim();
            if (!h.Equals(chuan[i], StringComparison.OrdinalIgnoreCase))
                return $"Cấu trúc tệp Excel không hợp lệ!\nCột số {i + 1} đang là '{h}', yêu cầu chuẩn phải là '{chuan[i]}'.\nVui lòng không tự ý chèn, xóa hoặc đổi tên cột mẫu.";
        }
        return null;
    }
    private static void GhiTieuDeBang(IXLWorksheet ws, int hang, string[] tieuDe, double[]? doRong, double coChu, double chieuCao, XLColor nen)
    {
        for (int i = 0; i < tieuDe.Length; i++) ws.Cell(hang, i + 1).Value = tieuDe[i];
        var r = ws.Range(hang, 1, hang, tieuDe.Length);
        r.Style.Font.Bold = true;
        r.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
        if (coChu > 0) r.Style.Font.FontSize = coChu;
        r.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        r.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        r.Style.Alignment.WrapText = true;
        r.Style.Fill.BackgroundColor = nen;
        r.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        r.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        ws.Row(hang).Height = chieuCao;
        if (doRong != null)
            for (int i = 0; i < doRong.Length; i++) ws.Column(i + 1).Width = doRong[i];
    }
    // Đổ cả khối dữ liệu 1 lần, style theo vùng/cột (thay cho gán style ~15 lần/dòng như bản cũ).
    private static void GhiThanBang(IXLWorksheet ws, int hangDau, List<object[]> dong, int soCot,
                                    int[] cotTrai, int[] cotVanBan, double coChu, double chieuCaoDong)
    {
        int n = dong.Count;
        if (n == 0) return;
        int hangCuoi = hangDau + n - 1;
        // Cột mã/số phải giữ dạng văn bản (tránh mất số 0 đầu của Số hiệu, tránh "15/03/2018" bị đổi thành ngày)
        foreach (int c in cotVanBan) ws.Range(hangDau, c, hangCuoi, c).Style.NumberFormat.Format = "@";
        ws.Cell(hangDau, 1).InsertData(dong);
        var vung = ws.Range(hangDau, 1, hangCuoi, soCot);
        vung.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
        vung.Style.Font.FontSize = coChu;
        vung.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        vung.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        vung.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        vung.Style.Alignment.WrapText = true;
        vung.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        foreach (int c in cotTrai)
            ws.Range(hangDau, c, hangCuoi, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        if (chieuCaoDong > 0) ws.Rows(hangDau, hangCuoi).Height = chieuCaoDong;
    }
    private static void DatO(IXLCell c, string text, double coChu, bool dam = false, bool nghieng = false)
    {
        c.Value = text;
        c.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
        c.Style.Font.FontSize = coChu;
        if (dam) c.Style.Font.Bold = true;
        if (nghieng) c.Style.Font.Italic = true;
        c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }
    // Gạch chân khoảng 1/3 chiều dài, căn giữa chuỗi (chuẩn tên đơn vị trên văn bản). Dùng chung cho mọi báo cáo.
    private static void GachChan1Phan3(IXLCell cell, string text)
    {
        if (cell == null || string.IsNullOrWhiteSpace(text)) return;
        int total = text.Length;
        int len = (int)Math.Round(total / 3.0);
        if (len <= 0) return;
        int start = (total - len) / 2;
        var rich = cell.GetRichText();
        rich.ClearText();
        if (start > 0) rich.AddText(text.Substring(0, start));
        rich.AddText(text.Substring(start, len)).SetUnderline();
        int end = start + len;
        if (end < total) rich.AddText(text.Substring(end));
    }
    // Gạch chân đoạn "cáo số: ... của " trong dòng "(Kèm theo Báo cáo số: ..., ngày ... của ...)"
    private static void GachChanCumBaoCao(IXLCell cell, string text)
    {
        int start = text.IndexOf("cáo", StringComparison.Ordinal);
        int end = text.IndexOf("của", StringComparison.Ordinal);
        if (start < 0 || end <= start) return;
        int len = Math.Min(end - start + 4, text.Length - start);
        cell.GetRichText().Substring(start, len).SetUnderline();
    }
    private static void HienThongBao(Form? chu, string noiDung, string tieuDe, MessageBoxIcon icon)
    {
        void Hien()
        {
            if (chu != null && !chu.IsDisposed) MessageBox.Show(chu, noiDung, tieuDe, MessageBoxButtons.OK, icon);
            else MessageBox.Show(noiDung, tieuDe, MessageBoxButtons.OK, icon);
        }
        if (chu != null && !chu.IsDisposed && chu.IsHandleCreated && chu.InvokeRequired) chu.Invoke(new Action(Hien));
        else Hien();
    }
    private static string GomDanhSachLoi(string lyDo, List<string> ds, int toiDa)
        => $"📌 Từ chối nhập dữ liệu CBCS có tên: {string.Join(", ", ds.Take(toiDa))}{(ds.Count > toiDa ? "..." : "")}\nLý do: {lyDo}\n";
    // Khóa Form cha + hiện Form_Loading; Dispose là mở khóa lại. Idempotent.
    private sealed class ManHinhCho : IDisposable
    {
        private readonly Form? _cha;
        private readonly Form_Loading _frm;
        private bool _daDong;
        public ManHinhCho(Form? cha, string noiDung)
        {
            _cha = cha;
            _frm = new Form_Loading(noiDung);
            if (cha != null) _frm.Icon = cha.Icon;
        }
        public async Task HienAsync()
        {
            if (_cha != null) _cha.Enabled = false;
            _frm.Show(_cha);
            await Task.Delay(50);                        // nhường luồng UI vẽ form chờ
        }
        public void Dispose()
        {
            if (_daDong) return;
            _daDong = true;
            try { _frm.Close(); } catch { }
            if (_cha != null && !_cha.IsDisposed)
            {
                _cha.Enabled = true;
                _cha.Focus();
            }
        }
    }
    // Chống chạy chồng nhưng KHÔNG mất lần gọi cuối: nếu có yêu cầu đến lúc đang chạy, chạy lại đúng 1 lần.
    private sealed class BoChongChayChong
    {
        private int _dang, _canLai;
        public async Task ChayAsync(Func<Task> viec)
        {
            Volatile.Write(ref _canLai, 1);
            do
            {
                if (Interlocked.Exchange(ref _dang, 1) == 1) return;
                try
                {
                    while (Interlocked.Exchange(ref _canLai, 0) == 1)
                    {
                        try { await viec(); }
                        catch (Exception ex) { Debug.WriteLine("[BoChongChayChong] " + ex.Message); }
                    }
                }
                finally { Volatile.Write(ref _dang, 0); }
            } while (Volatile.Read(ref _canLai) == 1);
        }
    }
    private static readonly BoChongChayChong _boTyLe = new();
    private static readonly BoChongChayChong _boSoVang = new();
    // ===================== MỞ EXPLORER VÀ CHỌN TỆP =====================
    public static void MoTepExcelThiDuaPhongTrao3Nhat(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;
        IntPtr pidl = IntPtr.Zero;
        try
        {
            pidl = ILCreateFromPathW(filePath);
            if (pidl == IntPtr.Zero) throw new InvalidOperationException();
            if (SHOpenFolderAndSelectItems(pidl, 0, IntPtr.Zero, 0) < 0) throw new InvalidOperationException();
        }
        catch
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{filePath}\"") { UseShellExecute = true });
            }
            catch { }
        }
        finally
        {
            if (pidl != IntPtr.Zero) ILFree(pidl);
        }
    }
    // =====================================================================================================
    //  THÔNG TIN BÁO CÁO – đọc 1 LẦN bằng 1 kết nối (bản cũ mở 6–7 kết nối và đọc trùng bảng ThongTin)
    // =====================================================================================================
    private sealed class ThongTinBaoCao
    {
        public string Dong1 = "", TrungDoan = "", TieuDoan = "", Thang = "", Nam = "", Ngay = "", DiaDiem = "";
        public string LoaiDeNghi = "", KyHieuBaoCao = "...............", HoTenKy = "";
        public string KyHieuTrungDoan = "", KyHieuTieuDoan = "";
        public string LoaiBaoCao = "", Tuan = "", TomTat = "";
        public string ChucVuTruong = "", ChucVuNguoiKy = "";
        public bool KyTrucTiep = true;                        // true: chỉ huy đứng đầu ký trực tiếp; false: "KT." + chức vụ người ký
        public readonly Dictionary<int, (int hienTai, int canDat)> TyLe = new();
    }
    private static ThongTinBaoCao DocThongTin(string dbPath, string bangTomTat)
    {
        var tt = new ThongTinBaoCao();
        using var cn = MoKetNoi(dbPath, true);
        void Thu(string ten, Action a)
        {
            try { a(); }
            catch (Exception ex) { Debug.WriteLine($"[DocThongTin:{ten}] {ex.Message}"); }
        }
        Thu("ThongTin", () =>
        {
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT textBox1_TenTrungDoanDong1, TenTrungDoan, TenTieuDoan, Thang, Nam, Ngay, DiaDiem, LoaiDeNghi, KyHieuBaoCao, ChiHuyD FROM ThongTin WHERE ID = 1";
            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return;
            tt.Dong1 = GiaiCot(rd, 0);
            tt.TrungDoan = GiaiCot(rd, 1);
            tt.TieuDoan = GiaiCot(rd, 2);
            tt.Thang = GiaiCot(rd, 3);
            tt.Nam = GiaiCot(rd, 4);
            tt.Ngay = GiaiCot(rd, 5);
            tt.DiaDiem = GiaiCot(rd, 6);
            tt.LoaiDeNghi = GiaiCot(rd, 7);
            string kh = GiaiCot(rd, 8);
            if (kh.Length > 0) tt.KyHieuBaoCao = kh;
            tt.HoTenKy = GiaiHoacGoc(Doc(rd, 9));
        });
        Thu("KyHieu_DonVi", () =>
        {
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT KyHieu_TrungDoan, KyHieu_TieuDoan FROM KyHieu_DonVi WHERE ID = 1";
            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return;
            tt.KyHieuTrungDoan = GiaiCot(rd, 0);
            tt.KyHieuTieuDoan = GiaiCot(rd, 1);
        });
        Thu("ChonLoaiBaoCao", () =>
        {
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT ChonLoaiBaoCao, ChonTuan FROM ChonLoaiBaoCao ORDER BY ID DESC LIMIT 1";
            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return;
            tt.LoaiBaoCao = GiaiCot(rd, 0);
            tt.Tuan = GiaiCot(rd, 1);
        });
        Thu("TyLe", () =>
        {
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT ID, [KQ Hien tai], [KQ Can dat] FROM TyLe";
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                tt.TyLe[ToInt(rd.GetValue(0))] = (ToInt(rd.GetValue(1)), ToInt(rd.GetValue(2)));   // DBNull => 0 (bản cũ ném InvalidCast)
        });
        Thu("TomTat", () =>
        {
            if (!BangTonTai(cn, bangTomTat)) return;
            using var cmd = cn.CreateCommand();
            cmd.CommandText = $"SELECT NoiDung FROM [{bangTomTat}] WHERE ID = 1";
            if (cmd.ExecuteScalar() is string s) tt.TomTat = Giai(s);
        });
        Thu("ChiHuyD", () =>
        {
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT HoVaTen, ChucVu FROM ChiHuyD ORDER BY ID ASC";
            using var rd = cmd.ExecuteReader();
            int idx = 0;
            while (rd.Read())
            {
                string ht = GiaiHoacGoc(Doc(rd, 0));
                string cv = GiaiHoacGoc(Doc(rd, 1));
                if (idx == 0) tt.ChucVuTruong = cv;
                if (tt.HoTenKy.Length > 0 && ht.Equals(tt.HoTenKy, StringComparison.OrdinalIgnoreCase))
                {
                    if (idx > 0) { tt.KyTrucTiep = false; tt.ChucVuNguoiKy = cv; }
                    break;
                }
                idx++;
            }
        });
        return tt;
    }
    // Khối đơn vị phía trên (A1–A3) dùng chung cho BC_BANHAT và DS_BANHAT
    private static void GhiKhoiDonVi(IXLWorksheet ws, ThongTinBaoCao tt)
    {
        DatO(ws.Cell("A1"), string.IsNullOrWhiteSpace(tt.Dong1) ? "     " : tt.Dong1, 13);
        if (!string.IsNullOrWhiteSpace(tt.TrungDoan))
        {
            DatO(ws.Cell("A2"), tt.TrungDoan, 13);
            var a3 = ws.Cell("A3");
            DatO(a3, tt.TieuDoan, 13, dam: true);
            GachChan1Phan3(a3, tt.TieuDoan);
        }
        else
        {
            var a2 = ws.Cell("A2");
            DatO(a2, tt.TieuDoan, 13, dam: true);
            GachChan1Phan3(a2, tt.TieuDoan);
            ws.Cell("A3").Value = "";
        }
    }
    private static string TyLeChu(int tu, int mau)
    {
        if (tu == 0) return "0%";
        if (mau == 0) return "Dữ liệu sai";
        return Math.Round(tu * 100.0 / mau, 2).ToString("0.##", CultureInfo.CurrentCulture) + "%";
    }
    // Đặt BC_BANHAT là sheet duy nhất được chọn, con trỏ ở A1 (gỡ "Group sheets")
    private static void DatSheetBaoCaoHienHanh(XLWorkbook wb)
    {
        try
        {
            foreach (var s in wb.Worksheets)
            {
                try { s.SetTabSelected(false); } catch { }
            }
            if (wb.Worksheets.TryGetWorksheet("BC_BANHAT", out var wsBC))
            {
                wsBC.SetTabActive();
                wsBC.SetTabSelected(true);
                wsBC.Cell("A1").SetActive();
                wsBC.Range("A1").Select();
            }
        }
        catch (Exception ex) { Debug.WriteLine("Lỗi định vị con trỏ Excel: " + ex.Message); }
    }
    // =====================================================================================================
    //  BÁO CÁO TỔNG HỢP (sheet BC_BANHAT)
    // =====================================================================================================
    // true  : ghi công thức Excel như bản cũ (Excel tự tính lại khi người dùng sửa số).
    // false : ghi GIÁ TRỊ đã tính sẵn (an toàn nhất cho Office cũ / WPS / chế độ xem trước).
    private const bool GhiBangCongThuc = false;
    private static void GhiBaoCaoTongHop(XLWorkbook wb, ThongTinBaoCao tt)
    {
        var ws = wb.Worksheet("BC_BANHAT");
        string thang = OrBlank(tt.Thang), nam = OrBlank(tt.Nam), ngay = OrBlank(tt.Ngay);
        string diaDiem = OrBlank(tt.DiaDiem), deNghi = OrBlank(tt.LoaiDeNghi);
        // A11: ký hiệu đơn vị
        string kyHieu = string.Join(", ", new[] { tt.KyHieuTieuDoan, tt.KyHieuTrungDoan }.Where(s => s.Length > 0));
        var a11 = ws.Cell("A11");
        DatO(a11, kyHieu, 12);
        a11.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        // A1–A3
        GhiKhoiDonVi(ws, tt);
        // H4: địa điểm, ngày tháng
        DatO(ws.Cell("H4"), $"{diaDiem}, ngày {ngay} tháng {thang} năm {nam}", 14, nghieng: true);
        // A6: tiêu đề thời gian
        string loai = tt.LoaiBaoCao.Trim().ToUpperInvariant();
        string tuan = tt.Tuan.Trim();
        string thangHT = Module_XuatPhanLoai.LayThangHeThong();
        string thoiGian;
        if (loai.Contains("TUẦN"))
        {
            tuan = tuan.ToUpperInvariant().Replace("TUẦN", "").Trim();
            if (tuan.Length == 0) tuan = "1";
            thoiGian = $"TUẦN {tuan} THÁNG {thangHT}/{nam}";
        }
        else thoiGian = $"THÁNG {thangHT}/{nam}";
        ws.Range("A6:M6").Merge();
        DatO(ws.Cell("A6"), $"TRONG THỰC HIỆN PHONG TRÀO THI ĐUA \"BA NHẤT\" {thoiGian}", 14, dam: true);
        // A7: "(Kèm theo Báo cáo số: ...)"
        string textA7 = $"(Kèm theo Báo cáo số:            {tt.KyHieuBaoCao}, ngày {ngay}/{thang}/{nam} của {VietHoaChuDau(tt.TieuDoan)})";
        ws.Range("A7:M7").Merge();
        var a7 = ws.Cell("A7");
        DatO(a7, textA7, 14, nghieng: true);
        GachChanCumBaoCao(a7, textA7);
        // Hàng 11–12: số liệu và tỷ lệ
        (int hienTai, int canDat) T(int id) => tt.TyLe.TryGetValue(id, out var v) ? v : (0, 0);
        int b11 = T(1).hienTai;
        int d11 = T(6).hienTai;
        int e11 = T(2).canDat, f11 = T(3).canDat, g11 = T(4).canDat, h11 = T(5).canDat, i11 = T(6).canDat;
        ws.Cell("B11").Value = b11;
        ws.Cell("C11").Value = b11;
        ws.Cell("D11").Value = d11;
        ws.Cell("E11").Value = e11;
        ws.Cell("F11").Value = f11;
        ws.Cell("G11").Value = g11;
        ws.Cell("H11").Value = h11;
        ws.Cell("I11").Value = i11;
        ws.Cell("K11").Value = deNghi;
        ws.Cell("L11").Value = tt.TomTat;
        if (GhiBangCongThuc)
        {
            ws.Cell("E12").FormulaA1 = "=IFERROR((E11*100)/F11,\"Dữ liệu sai\")";
            ws.Cell("F12").FormulaA1 = "=IFERROR(IF(B11=0,0,(F11*100)/B11),\"Dữ liệu sai\")";
            ws.Cell("G12").FormulaA1 = "=IFERROR(100-F12,\"Dữ liệu sai\")";
            ws.Cell("H12").FormulaA1 = "=IFERROR(IF(OR(H11=\"\",H11=0),\"0%\",(H11*100)/B11 & \"%\"),\"Dữ liệu sai\")";
            ws.Cell("I12").FormulaA1 = "=IFERROR(IF(OR(I11=\"\",I11=0),\"0%\",(I11*100)/B11 & \"%\"),\"Dữ liệu sai\")";
        }
        else
        {
            double f12 = b11 == 0 ? 0 : f11 * 100.0 / b11;
            if (f11 == 0) ws.Cell("E12").Value = "Dữ liệu sai";
            else ws.Cell("E12").Value = e11 * 100.0 / f11;
            ws.Cell("F12").Value = f12;
            ws.Cell("G12").Value = 100 - f12;
            ws.Cell("H12").Value = TyLeChu(h11, b11);
            ws.Cell("I12").Value = TyLeChu(i11, b11);
        }
        ws.Range("E12:I12").Style.NumberFormat.Format = "00.00\"%\"";
        // Ký tên
        if (tt.KyTrucTiep)
        {
            ws.Cell("L14").Value = tt.ChucVuTruong.ToUpperInvariant();
            ws.Cell("L15").Value = "";
        }
        else
        {
            ws.Cell("L14").Value = ("KT. " + tt.ChucVuTruong).ToUpperInvariant();
            ws.Cell("L15").Value = tt.ChucVuNguoiKy.ToUpperInvariant();
        }
        ws.Cell("L17").Value = tt.HoTenKy;
        foreach (string addr in new[] { "L14", "L15", "L17" })
        {
            var c = ws.Cell(addr);
            c.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
            c.Style.Font.FontSize = 14;
            c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            c.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            if (addr != "L17") c.Style.Font.Bold = true;
        }
    }
    // Giữ tên cũ cho nơi khác gọi: mở tệp đã xuất, ghi BC_BANHAT, lưu lại (1 lần mở, 1 lần lưu an toàn).
    public static void XuatBaoCaoTongHop(string fileXuat)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileXuat) || !File.Exists(fileXuat))
                throw new FileNotFoundException("File Excel tổng hợp không tồn tại.");
            string db = Module_DanduongGPS.DuongDanCSDL2;
            if (string.IsNullOrWhiteSpace(db) || !File.Exists(db))
                throw new FileNotFoundException("Không tìm thấy CSDL csdl2.");
            var tt = DocThongTin(db, ChonBang().TomTat);
            using var wb = new XLWorkbook(fileXuat);
            GhiBaoCaoTongHop(wb, tt);
            DatSheetBaoCaoHienHanh(wb);
            LuuTepAnToan(wb, fileXuat);
        }
        catch (Exception ex)
        {
            Module_ThongBao.DangXuLy("Lỗi xuất báo cáo tổng hợp: " + ex.Message);
        }
    }
    // =====================================================================================================
    //  SHEET DS_BaNhat nhúng trong workbook do nơi khác tạo
    // =====================================================================================================
    private static List<object[]> DocDanhSachDeNghi(string dbPath, string bang)
    {
        var ds = new List<object[]>();
        using var cn = MoKetNoi(dbPath, true);
        if (!BangTonTai(cn, bang)) return ds;
        using var cmd = cn.CreateCommand();
        cmd.CommandText = $"SELECT HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu, DeNghi, ThanhTich FROM [{bang}] ORDER BY STT ASC";
        using var rd = cmd.ExecuteReader();
        int stt = 1;
        while (rd.Read())
        {
            if (!GiaiCot(rd, 10).Equals("X", StringComparison.OrdinalIgnoreCase)) continue;
            ds.Add(new object[]
            {
                stt++, GiaiCot(rd, 0), GiaiCot(rd, 1), GiaiCot(rd, 2), GiaiCot(rd, 3), GiaiCot(rd, 4),
                GiaiCot(rd, 5), GiaiCot(rd, 6), GiaiCot(rd, 7), GiaiCot(rd, 8), GiaiCot(rd, 9), "X", GiaiCot(rd, 11)
            });
        }
        return ds;
    }
    public static void XuatDuLieuVaoBangQuanLyBaNhat(XLWorkbook package, string dbPath, bool epXuatFileMau)
    {
        try
        {
            IXLWorksheet wsBN;
            if (package.Worksheets.Contains("DS_BaNhat"))
            {
                wsBN = package.Worksheet("DS_BaNhat");
                wsBN.Clear();
            }
            else wsBN = package.Worksheets.Add("DS_BaNhat");
            wsBN.TabColor = XLColor.Gold;
            wsBN.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
            wsBN.Style.Font.FontSize = 12;
            wsBN.Range("A1:M1").Merge();
            var a1 = wsBN.Cell("A1");
            a1.Value = "DANH SÁCH CÁN BỘ CHIẾN SĨ ĐỀ NGHỊ BIỂU DƯƠNG PHONG TRÀO \"BA NHẤT\"";
            a1.Style.Font.Bold = true;
            a1.Style.Font.FontSize = 14;
            a1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            wsBN.Row(1).Height = 30;
            GhiTieuDeBang(wsBN, 3, TieuDeBaNhat, DoRongBaNhat, 12, 25, MauNenTieuDe);
            if (!epXuatFileMau && !string.IsNullOrWhiteSpace(dbPath) && File.Exists(dbPath))
            {
                var dong = DocDanhSachDeNghi(dbPath, ChonBang().DanhSach);
                GhiThanBang(wsBN, 4, dong, 13, CotTraiBaNhat, CotVanBanBaNhat, 12, 24);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Lỗi thực thi tích hợp tại Sheet DS_BaNhat: " + ex.Message);
        }
    }
    // =====================================================================================================
    //  NẠP DỮ LIỆU TỪ SHEET DS_BaNhat (tệp tổng hợp) – bảng quản lý Ba nhất
    //  SỬA LỖI: bản cũ tra "SoHieu = MaHoa(soHieu)" nhưng MaHoa dùng IV ngẫu nhiên nên KHÔNG BAO GIỜ khớp
    //  => mọi dòng đều bị INSERT trùng. Nay giải mã SoHieu trong CSDL 1 lần, tra bằng Dictionary, cập nhật theo ID.
    // =====================================================================================================
    public static void NhapDuLieuVaoBangQuanLyBaNhat(string excelPath, string dbPath, Form? parentForm = null)
    {
        Form? chu = parentForm ?? (Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null);
        try
        {
            if (!File.Exists(excelPath) || !File.Exists(dbPath)) return;
            string? loiTep = KiemTraKichThuocTep(excelPath);
            if (loiTep != null) { HienThongBao(chu, loiTep, "Lỗi tệp nhập", MessageBoxIcon.Error); return; }
            string bang = ChonBang().DanhSach;
            var boQua = new List<string>();
            using (var wb = new XLWorkbook(excelPath))
            {
                if (!wb.Worksheets.TryGetWorksheet("DS_BaNhat", out var ws)) return;
                string? loiTieuDe = KiemTraTieuDe(ws, 3, TieuDeBaNhat);
                if (loiTieuDe != null) { HienThongBao(chu, loiTieuDe, "Lỗi cấu trúc tệp nhập", MessageBoxIcon.Error); return; }
                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                if (lastRow < 4) return;
                if (lastRow > DongExcelToiDa)
                {
                    HienThongBao(chu, $"Tệp có quá nhiều dòng (>{DongExcelToiDa}).", "Lỗi tệp nhập", MessageBoxIcon.Error);
                    return;
                }
                using var cn = MoKetNoi(dbPath, false);
                // Giải mã SoHieu hiện có 1 lần -> Dictionary(SoHieu -> ID)
                var idTheoSoHieu = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                int nextSTT;
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT ID, SoHieu FROM [{bang}]";
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        int id = rd.GetInt32(0);
                        if (id == -1) continue;
                        string sh = GiaiCot(rd, 1);
                        if (sh.Length > 0) idTheoSoHieu[sh] = id;
                    }
                }
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT IFNULL(MAX(STT), 0) FROM [{bang}]";
                    nextSTT = Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) + 1;
                }
                using var tx = cn.BeginTransaction();
                using var cmdUpd = TaoLenhCapNhat(cn, tx, bang, CotBaNhatCapNhat);
                using var cmdIns = TaoLenhThem(cn, tx, bang, CotBaNhatThem);
                var daXuLy = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int r = 4; r <= lastRow; r++)
                {
                    var row = ws.Row(r);
                    string hoTen = Chuoi(row.Cell(2));
                    if (hoTen.Length == 0) continue;
                    try
                    {
                        string soHieu = Chuoi(row.Cell(3));
                        string deNghi = Chuoi(row.Cell(12));
                        if (soHieu.Length == 0)
                        {
                            boQua.Add($"- {Module_HeThong.Tu_Dong_Chi}: {hoTen} dòng {r} không thể nạp vì thiếu Số hiệu quân số.");
                            continue;
                        }
                        if (!deNghi.Equals("X", StringComparison.OrdinalIgnoreCase))
                        {
                            boQua.Add($"- {Module_HeThong.Tu_Dong_Chi}: {hoTen} (Số hiệu: {soHieu}) bỏ qua do không có dấu X Đề nghị.");
                            continue;
                        }
                        if (!daXuLy.Add(soHieu))
                        {
                            boQua.Add($"- {Module_HeThong.Tu_Dong_Chi}: {hoTen} (Số hiệu: {soHieu}) dòng {r} bị trùng Số hiệu trong tệp, bỏ qua.");
                            continue;
                        }
                        string[] giaTri =
                        {
                            hoTen, Chuoi(row.Cell(4)), Chuoi(row.Cell(5)), Chuoi(row.Cell(6)), Chuoi(row.Cell(7)),
                            Chuoi(row.Cell(8)), Chuoi(row.Cell(9)), Chuoi(row.Cell(10)), Chuoi(row.Cell(11)),
                            "X", Chuoi(row.Cell(13))
                        };
                        if (idTheoSoHieu.TryGetValue(soHieu, out int idCu))
                        {
                            cmdUpd.Parameters["@id"].Value = idCu;
                            GanThamSo(cmdUpd, giaTri, CotBaNhatCapNhat.Length);   // UPDATE không đụng DeNghi/ThanhTich
                            cmdUpd.ExecuteNonQuery();
                        }
                        else
                        {
                            cmdIns.Parameters["@stt"].Value = nextSTT++;
                            cmdIns.Parameters["@sh"].Value = Module_BaoMatAES.MaHoa(soHieu);
                            GanThamSo(cmdIns, giaTri, CotBaNhatThem.Length);
                            cmdIns.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        boQua.Add($"- {Module_HeThong.Tu_Dong_Chi}: {hoTen} (Dòng {r}) - Lỗi kỹ thuật: {ex.Message}");
                    }
                }
                tx.Commit();
            }
            if (boQua.Count > 0)
            {
                string msg = string.Join("\n", boQua.Take(15));
                if (boQua.Count > 15) msg += $"\n... và {boQua.Count - 15} trường hợp khác.";
                HienThongBao(chu, msg, "Thông báo nạp Sổ Vàng", MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[Lỗi NhapDataSoVang] " + ex.Message);
        }
    }
    // UPDATE [bang] SET c0=@p0,... WHERE ID=@id
    private static SqliteCommand TaoLenhCapNhat(SqliteConnection cn, SqliteTransaction tx, string bang, string[] cot)
    {
        var cmd = cn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = $"UPDATE [{bang}] SET {string.Join(", ", cot.Select((c, i) => $"{c}={TenThamSo[i]}"))} WHERE ID=@id";
        for (int i = 0; i < cot.Length; i++) cmd.Parameters.Add(TenThamSo[i], SqliteType.Text);
        cmd.Parameters.Add("@id", SqliteType.Integer);
        return cmd;
    }
    // INSERT [bang] (STT, SoHieu, c0..) VALUES (@stt, @sh, @p0..)
    private static SqliteCommand TaoLenhThem(SqliteConnection cn, SqliteTransaction tx, string bang, string[] cot)
    {
        var cmd = cn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = $"INSERT INTO [{bang}] (STT, SoHieu, {string.Join(", ", cot)}) VALUES (@stt, @sh, {string.Join(", ", cot.Select((_, i) => TenThamSo[i]))})";
        cmd.Parameters.Add("@stt", SqliteType.Integer);
        cmd.Parameters.Add("@sh", SqliteType.Text);
        for (int i = 0; i < cot.Length; i++) cmd.Parameters.Add(TenThamSo[i], SqliteType.Text);
        return cmd;
    }
    // Mã hóa từng giá trị (IV ngẫu nhiên) rồi gán vào tham số @p0..@pN-1. MaHoa("") trả "" như quy ước cũ.
    private static void GanThamSo(SqliteCommand cmd, string[] giaTri, int soCot)
    {
        for (int i = 0; i < soCot; i++)
            cmd.Parameters[TenThamSo[i]].Value = Module_BaoMatAES.MaHoa(giaTri[i]);
    }
    // =====================================================================================================
    //  TỶ LỆ "BA NHẤT" TRÊN THANH TRẠNG THÁI
    // =====================================================================================================
    private readonly record struct KetQuaTyLe(int TyLe, int TongLoai1, int SoDeNghi);
    private static KetQuaTyLe TinhTyLe(string dbPath)
    {
        int tyLe = 0, tongLoai1 = 0, soDeNghi = 0;
        string bangBaNhat = ChonBang().DanhSach;
        using var cn = MoKetNoi(dbPath, true);
        using (var cmd = cn.CreateCommand())
        {
            cmd.CommandText = "SELECT TyLe FROM QuyDinhTyLe_BaNhat WHERE ID = 1";
            if (cmd.ExecuteScalar() is string s) int.TryParse(Giai(s), out tyLe);
        }
        using (var cmd = cn.CreateCommand())
        {
            cmd.CommandText = "SELECT PhanLoai FROM DanhSach WHERE PhanLoai IS NOT NULL AND PhanLoai <> ''";
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                if (GiaiCot(rd, 0).Equals("Loại 1", StringComparison.OrdinalIgnoreCase)) tongLoai1++;
        }
        if (BangTonTai(cn, bangBaNhat))
        {
            using var cmd = cn.CreateCommand();
            cmd.CommandText = $"SELECT DeNghi FROM [{bangBaNhat}] WHERE DeNghi IS NOT NULL AND DeNghi <> ''";
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                if (GiaiCot(rd, 0).Equals("X", StringComparison.OrdinalIgnoreCase)) soDeNghi++;
        }
        return new KetQuaTyLe(tyLe, tongLoai1, soDeNghi);
    }
    private static void HienKetQuaTyLe(ToolStripStatusLabel lbl, string text, System.Drawing.Color mau)
    {
        void Gan()
        {
            if (lbl.IsDisposed) return;
            if (lbl.Text != text) lbl.Text = text;
            lbl.ForeColor = mau;
        }
        var cha = lbl.GetCurrentParent();
        if (cha != null && cha.InvokeRequired) cha.BeginInvoke(new Action(Gan));
        else Gan();
    }
    public static Task TinhToanVaHienThiTyLeBaNhatAsync(ToolStripStatusLabel labelTyLe)
    {
        if (labelTyLe == null || labelTyLe.IsDisposed) return Task.CompletedTask;
        string dbPath = Module_DanduongGPS.DuongDanCSDL2;
        if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return Task.CompletedTask;
        return _boTyLe.ChayAsync(async () =>
        {
            if (labelTyLe.IsDisposed) return;
            var kq = await Task.Run(() => TinhTyLe(dbPath));
            if (labelTyLe.IsDisposed) return;
            int chiTieu = (int)Math.Round(kq.TongLoai1 * (kq.TyLe / 100.0), MidpointRounding.AwayFromZero);
            double tyLeHienTai = kq.TongLoai1 > 0 ? Math.Round((double)kq.SoDeNghi / kq.TongLoai1 * 100, 1) : 0;
            string trangThai = kq.SoDeNghi == chiTieu
                ? "Đạt tỷ lệ"
                : kq.SoDeNghi < chiTieu
                    ? $"Đang thiếu: {chiTieu - kq.SoDeNghi} {Module_HeThong.Tu_dong_chi} "
                    : $"Đang thừa: {kq.SoDeNghi - chiTieu} {Module_HeThong.Tu_dong_chi}";
            string text = $"           Chỉ tiêu “Ba nhất” {kq.TyLe}% = {chiTieu} {Module_HeThong.Tu_dong_chi}    |    Tỷ lệ hiện tại {kq.SoDeNghi} {Module_HeThong.Tu_dong_chi} = {tyLeHienTai}%    |    {trangThai}";
            HienKetQuaTyLe(labelTyLe, text, kq.SoDeNghi == chiTieu ? System.Drawing.Color.DarkGreen : System.Drawing.Color.Red);
        });
    }
    // =====================================================================================================
    //  ĐỒNG BỘ TÌNH TRẠNG "ĐÃ ĐỀ NGHỊ" TỪ SỔ VÀNG
    //  Bản cũ UPDATE mọi dòng (N lần giải mã + N lần mã hóa + N lệnh ghi) dù không có gì đổi.
    //  Nay chỉ ghi những dòng THỰC SỰ thay đổi.
    // =====================================================================================================
    private static void CapNhatTinhTrangSoVang(string dbPath)
    {
        var bang = ChonBang();
        using var cn = MoKetNoi(dbPath, false);
        var daVaoSo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (BangTonTai(cn, bang.SoVang))
        {
            using var cmd = cn.CreateCommand();
            cmd.CommandText = $"SELECT SoHieu FROM [{bang.SoVang}]";
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                string sh = GiaiCot(rd, 0);
                if (sh.Length > 0) daVaoSo.Add(sh);
            }
        }
        if (!BangTonTai(cn, bang.DanhSach)) return;
        const string DA_DE_NGHI = "Đã đề nghị";
        var canGhi = new List<(int Id, string TinhTrang)>();
        using (var cmd = cn.CreateCommand())
        {
            cmd.CommandText = $"SELECT ID, SoHieu, TinhTrang FROM [{bang.DanhSach}]";
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                int id = rd.GetInt32(0);
                if (id == -1) continue;                                   // dòng đệm mồi của CSDL
                string sh = GiaiCot(rd, 1);
                string moi = sh.Length > 0 && daVaoSo.Contains(sh) ? DA_DE_NGHI : "";
                string cu = GiaiCot(rd, 2);
                if (!string.Equals(cu, moi, StringComparison.Ordinal)) canGhi.Add((id, moi));
            }
        }
        if (canGhi.Count == 0) return;
        using var tx = cn.BeginTransaction();
        using var upd = cn.CreateCommand();
        upd.Transaction = tx;
        upd.CommandText = $"UPDATE [{bang.DanhSach}] SET TinhTrang = @tt WHERE ID = @id";
        var pTT = upd.Parameters.Add("@tt", SqliteType.Text);
        var pID = upd.Parameters.Add("@id", SqliteType.Integer);
        foreach (var (id, tinhTrang) in canGhi)
        {
            pTT.Value = Module_BaoMatAES.MaHoa(tinhTrang);
            pID.Value = id;
            upd.ExecuteNonQuery();
        }
        tx.Commit();
    }
    public static Task CapNhatTinhTrangSoVangAsync()
    {
        string dbPath = Module_DanduongGPS.DuongDanCSDL2;
        if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return Task.CompletedTask;
        return _boSoVang.ChayAsync(async () =>
        {
            try { await Task.Run(() => CapNhatTinhTrangSoVang(dbPath)); }
            catch (Exception ex) { Debug.WriteLine("Lỗi tính toán tình trạng sổ vàng: " + ex.Message); }
        });
    }
    // =====================================================================================================
    //  XUẤT DANH SÁCH GỐC (Form 42)
    // =====================================================================================================
    public static async Task XuatDuLieuRaTepExcelTuCSDL_ToiUu(List<DanhSachGocBaNhatDTO> danhSach, string filePathLuu)
    {
        try
        {
            await Task.Run(() =>
            {
                var dong = new List<object[]>(danhSach.Count);
                int stt = 1;
                foreach (var x in danhSach)
                    dong.Add(new object[] { stt++, x.HoVaTen, x.SoHieu, x.NamSinh, x.QueQuan, x.NgayVaoCAND, x.CapBac, x.ChucVu, x.DonVi, x.PhanLoai, x.GhiChu, x.DeNghi, x.ThanhTich });
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("DS_BANHAT_CSDL");
                GhiTieuDeBang(ws, 1, TieuDeBaNhat, DoRongBaNhat, 13, 36, MauNenTieuDe);
                GhiThanBang(ws, 2, dong, 13, CotTraiBaNhat, CotVanBanBaNhat, 13, 36);
                Module_BanQuyen.DongDauExcel(wb);
                LuuTepAnToan(wb, filePathLuu);
            });
            Module_NhatKy.GhiNhatKy(
                taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                hanhDong: "Xuất tệp excel toàn bộ CSDL gốc Ba Nhất",
                ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
            MoTepExcelThiDuaPhongTrao3Nhat(filePathLuu);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi khi xuất file: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    // =====================================================================================================
    //  XUẤT DANH SÁCH ĐỀ NGHỊ + BÁO CÁO TỔNG HỢP (Form 42) – MỘT LẦN MỞ, MỘT LẦN LƯU
    // =====================================================================================================
    private static void GhiSheetDanhSachDeNghi(XLWorkbook wb, ThongTinBaoCao tt, List<DeNghiBaNhatDTO> danhSach)
    {
        var ws = wb.Worksheet("DS_BANHAT");
        string thang = tt.Thang.Trim(), nam = tt.Nam.Trim(), ngay = tt.Ngay.Trim(), diaDiem = tt.DiaDiem.Trim();
        GhiKhoiDonVi(ws, tt);
        // F3:J3 – địa điểm, ngày tháng
        ws.Range("F3:J3").Clear(XLClearOptions.Contents);
        ws.Range("F3:J3").Merge();
        var f3 = ws.Cell("F3");
        DatO(f3, diaDiem.Length > 0 ? $"{diaDiem}, ngày {ngay} tháng {thang} năm {nam}" : $"(ngày {ngay} tháng {thang} năm {nam})", 13, nghieng: true);
        f3.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        // Xóa vùng dữ liệu cũ của mẫu: chỉ tới dòng cuối THỰC SỰ dùng (bản cũ xóa 5.000.000 ô A9:J500000)
        int cuoiMau = ws.LastRowUsed(XLCellsUsedOptions.All)?.RowNumber() ?? 9;
        if (cuoiMau >= 9) ws.Range(9, 1, cuoiMau, 10).Clear(XLClearOptions.All);
        ws.Range("A5:J5").Merge();
        var a5 = ws.Cell("A5");
        a5.Value = $"CBCS ĐỀ NGHỊ BIỂU DƯƠNG GƯƠNG ĐIỂN HÌNH TRONG THỰC HIỆN PHONG TRÀO THI ĐUA BA NHẤT THÁNG {thang}/{nam}";
        a5.Style.Font.Bold = true;
        a5.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        string textA6 = $"(Kèm theo Báo cáo số:                 {tt.KyHieuBaoCao}, ngày {ngay}/{thang}/{nam} của {VietHoaChuDau(tt.TieuDoan)})";
        ws.Range("A6:J6").Merge();
        var a6 = ws.Cell("A6");
        a6.Value = textA6;
        a6.Style.Font.Italic = true;
        a6.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        GachChanCumBaoCao(a6, textA6);
        // Dữ liệu (10 cột)
        var dong = new List<object[]>(danhSach.Count);
        int stt = 1;
        foreach (var x in danhSach)
            dong.Add(new object[] { stt++, x.HoVaTen, x.SoHieu, x.NamSinh, x.NgayVaoCAND, x.CapBac, x.ChucVu, x.DonVi, x.PhanLoai, x.ThanhTich });
        const int hangDau = 9;
        GhiThanBang(ws, hangDau, dong, 10, new[] { 2, 10 }, new[] { 3, 4, 5 }, 13, 0);
        for (int i = 0; i < dong.Count; i++) ws.Row(hangDau + i).ClearHeight();     // để Excel tự co giãn theo nội dung
        int startRow = hangDau + dong.Count;
        var rngTong = ws.Range(startRow, 1, startRow, 2);
        rngTong.Merge();
        var cTong = ws.Cell(startRow, 1);
        cTong.Value = $"Tổng cộng: {dong.Count:00} đồng chí./.";
        cTong.Style.Font.Bold = true;
        cTong.Style.Font.Italic = true;
        cTong.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        cTong.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        // Khối ký tên (cột F–J)
        int dongKy = startRow + 1;
        void GhiDongKy(string text)
        {
            ws.Range(dongKy, 6, dongKy, 10).Merge();
            var c = ws.Cell(dongKy, 6);
            c.Value = text;
            c.Style.Font.Bold = true;
            c.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
            c.Style.Font.FontSize = 14;
            c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            c.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            dongKy++;
        }
        if (tt.KyTrucTiep)
            GhiDongKy(tt.ChucVuTruong.ToUpperInvariant());
        else
        {
            GhiDongKy("KT. " + tt.ChucVuTruong.ToUpperInvariant());
            GhiDongKy(tt.ChucVuNguoiKy.ToUpperInvariant());
        }
        dongKy += 4;
        GhiDongKy(tt.HoTenKy);
    }
    public static async Task XuatDanhSachBaNhatToExcelAsync(Form? parentForm, List<DeNghiBaNhatDTO> danhSach)
    {
        string templatePath = Module_DanduongGPS.DuongDanCSDL4ex;
        string dbPath = Module_DanduongGPS.DuongDanCSDL2;
        if (string.IsNullOrWhiteSpace(templatePath) || !File.Exists(templatePath))
        {
            MessageBox.Show("Không tìm thấy file mẫu Excel tại: " + templatePath, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
        {
            MessageBox.Show("Không tìm thấy cơ sở dữ liệu csdl2.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        // Đọc thông tin đơn vị 1 lần (dùng cho tên tệp gợi ý VÀ cho nội dung báo cáo)
        ThongTinBaoCao tt;
        try
        {
            string bangTomTat = ChonBang().TomTat;
            tt = await Task.Run(() => DocThongTin(dbPath, bangTomTat));
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi đọc thông tin báo cáo: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        string kyHieuTieuDoan = tt.KyHieuTieuDoan.Length > 0 ? tt.KyHieuTieuDoan : "D2";
        using var sfd = new SaveFileDialog
        {
            Filter = "Excel Files|*.xlsx",
            FileName = LamSachTenTep($"BÁO CÁO BIỂU DƯƠNG PHONG TRÀO BA NHẤT CỦA {kyHieuTieuDoan} {DateTime.Now:yyyyMMdd_HHmmss}") + ".xlsx",
            Title = "Chọn nơi lưu file Excel"
        };
        if (sfd.ShowDialog() != DialogResult.OK) return;
        string filePathLuu = sfd.FileName;
        ManHinhCho? cho = null;
        try
        {
            cho = new ManHinhCho(parentForm, "Đang tạo tệp excel, vui lòng đợi...");
            await cho.HienAsync();
            string? loiTongHop = null;
            await Task.Run(() =>
            {
                using var wb = new XLWorkbook(templatePath);
                // Chỉ giữ 2 sheet cần thiết
                var xoa = wb.Worksheets
                    .Where(w => !w.Name.Equals("BC_BANHAT", StringComparison.OrdinalIgnoreCase) &&
                                !w.Name.Equals("DS_BANHAT", StringComparison.OrdinalIgnoreCase))
                    .Select(w => w.Name).ToList();
                foreach (var ten in xoa) wb.Worksheets.Delete(ten);
                GhiSheetDanhSachDeNghi(wb, tt, danhSach);
                // Báo cáo tổng hợp lỗi thì vẫn lưu danh sách (đúng hành vi cũ), chỉ báo riêng
                try { GhiBaoCaoTongHop(wb, tt); }
                catch (Exception ex) { loiTongHop = ex.Message; }
                Module_BanQuyen.DongDauExcel(wb);
                DatSheetBaoCaoHienHanh(wb);
                LuuTepAnToan(wb, filePathLuu);
            });
            Module_NhatKy.GhiNhatKy(
                taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                hanhDong: "Xuất tệp excel báo cáo thi đua Ba Nhất",
                ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
            cho.Dispose();
            cho = null;
            if (loiTongHop != null) Module_ThongBao.DangXuLy("Lỗi xuất báo cáo tổng hợp: " + loiTongHop);
            MoTepExcelThiDuaPhongTrao3Nhat(filePathLuu);
        }
        catch (Exception ex)
        {
            cho?.Dispose();
            cho = null;
            MessageBox.Show("Lỗi xuất file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            cho?.Dispose();
        }
    }
    // =====================================================================================================
    //  NHẬP DANH SÁCH GỐC TỪ EXCEL (Form 42)
    //  Cập nhật theo ID; chỉ giải mã PhanLoai của những người có trong tệp Excel.
    // =====================================================================================================
    private readonly record struct DongNhapBaNhat(string Ten, string SoHieu, string DeNghi, string ThanhTich);
    public static async Task<int> NhapDuLieuTuTepExcelVaoCSDL(Form? parentForm, string excelPath, string dbPath)
    {
        int soThanhCong = 0;
        try
        {
            if (!File.Exists(excelPath) || !File.Exists(dbPath)) return 0;
            string bang = ChonBang().DanhSach;
            var dsKhongCoSoHieu = new List<string>();
            var dsKhongPhaiLoai1 = new List<string>();
            var dsChuaDongBo = new List<string>();
            var dsLoiKyThuat = new List<string>();
            string? loiCauTruc = null;
            string tieuDeLoi = "Lỗi cấu trúc";
            await Task.Run(() =>
            {
                loiCauTruc = KiemTraKichThuocTep(excelPath);
                if (loiCauTruc != null) { tieuDeLoi = "Từ chối nhập liệu"; return; }
                using var wb = new XLWorkbook(excelPath);
                if (!wb.Worksheets.TryGetWorksheet("DS_BANHAT_CSDL", out var ws))
                {
                    loiCauTruc = "Tệp Excel không hợp lệ! Không tìm thấy Sheet 'DS_BANHAT_CSDL'.\nVui lòng chọn đúng tệp được xuất từ chức năng 'Xuất danh sách gốc'.";
                    return;
                }
                loiCauTruc = KiemTraTieuDe(ws, 1, TieuDeBaNhat);
                if (loiCauTruc != null) { tieuDeLoi = "Từ chối nhập liệu"; return; }
                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                if (lastRow < 2) return;
                if (lastRow > DongExcelToiDa)
                {
                    loiCauTruc = $"Tệp có quá nhiều dòng (>{DongExcelToiDa}).";
                    tieuDeLoi = "Từ chối nhập liệu";
                    return;
                }
                // 1) Đọc Excel vào RAM
                var dong = new List<DongNhapBaNhat>();
                var tapSoHieu = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int r = 2; r <= lastRow; r++)
                {
                    var row = ws.Row(r);
                    string ten = Chuoi(row.Cell(2));
                    string sh = Chuoi(row.Cell(3));
                    if (ten.Length == 0 && sh.Length == 0) continue;
                    string dn = Chuoi(row.Cell(12));
                    if (dn.Equals("X", StringComparison.OrdinalIgnoreCase)) dn = "X";     // chuẩn hóa để các nơi đếm/lọc nhất quán
                    dong.Add(new DongNhapBaNhat(ten, sh, dn, Chuoi(row.Cell(13))));
                    if (sh.Length > 0) tapSoHieu.Add(sh);
                }
                if (dong.Count == 0) return;
                using var cn = MoKetNoi(dbPath, false);
                // 2) Bảng gốc "DanhSach": chỉ giải mã PhanLoai của người có trong tệp
                var phanLoaiGoc = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "SELECT SoHieu, PhanLoai FROM DanhSach";
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        string sh = GiaiCot(rd, 0);
                        if (sh.Length > 0 && tapSoHieu.Contains(sh)) phanLoaiGoc[sh] = GiaiCot(rd, 1);
                    }
                }
                // 3) Bảng Ba nhất: SoHieu -> ID
                var idBaNhat = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT ID, SoHieu FROM [{bang}]";
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        int id = rd.GetInt32(0);
                        if (id == -1) continue;
                        string sh = GiaiCot(rd, 1);
                        if (sh.Length > 0 && tapSoHieu.Contains(sh)) idBaNhat[sh] = id;
                    }
                }
                // 4) Đối chiếu + cập nhật
                using var tx = cn.BeginTransaction();
                using var upd = cn.CreateCommand();
                upd.Transaction = tx;
                upd.CommandText = $"UPDATE [{bang}] SET DeNghi = @dn, ThanhTich = @tt WHERE ID = @id";
                var pDn = upd.Parameters.Add("@dn", SqliteType.Text);
                var pTt = upd.Parameters.Add("@tt", SqliteType.Text);
                var pId = upd.Parameters.Add("@id", SqliteType.Integer);
                int dem = 0;
                foreach (var d in dong)
                {
                    string nhan = d.Ten.Length > 0 ? d.Ten : d.SoHieu;
                    try
                    {
                        if (!phanLoaiGoc.TryGetValue(d.SoHieu, out string? pl)) { dsKhongCoSoHieu.Add(nhan); continue; }
                        if (!pl.Equals("Loại 1", StringComparison.OrdinalIgnoreCase)) { dsKhongPhaiLoai1.Add(nhan); continue; }
                        if (!idBaNhat.TryGetValue(d.SoHieu, out int id)) { dsChuaDongBo.Add(nhan); continue; }
                        pId.Value = id;
                        pDn.Value = Module_BaoMatAES.MaHoa(d.DeNghi);
                        pTt.Value = Module_BaoMatAES.MaHoa(d.ThanhTich);
                        if (upd.ExecuteNonQuery() > 0) dem++;
                        else dsLoiKyThuat.Add(nhan + " (Không rõ nguyên nhân DB)");
                    }
                    catch (Exception ex)
                    {
                        dsLoiKyThuat.Add($"{nhan} ({ex.Message})");
                    }
                }
                tx.Commit();
                soThanhCong = dem;
            });
            if (loiCauTruc != null)
            {
                HienThongBao(parentForm, loiCauTruc, tieuDeLoi, MessageBoxIcon.Error);
                return 0;
            }
            int tongLoi = dsKhongCoSoHieu.Count + dsKhongPhaiLoai1.Count + dsChuaDongBo.Count + dsLoiKyThuat.Count;
            if (tongLoi > 0)
            {
                var mt = new List<string> { "⚠️ Quá trình nạp dữ liệu hoàn tất nhưng một số trường hợp bị từ chối do vi phạm ràng buộc:\n" };
                if (dsKhongPhaiLoai1.Count > 0) mt.Add(GomDanhSachLoi("Không được xếp mức Loại 1.", dsKhongPhaiLoai1, 30));
                if (dsKhongCoSoHieu.Count > 0) mt.Add(GomDanhSachLoi("Không tìm thấy Số hiệu quân số trong CSDL chính.", dsKhongCoSoHieu, 20));
                if (dsChuaDongBo.Count > 0) mt.Add(GomDanhSachLoi("Đạt Loại 1 nhưng chưa được đồng bộ sang hệ thống Sổ Vàng (Vui lòng bấm 'Làm mới' hệ thống trước khi nạp lại).", dsChuaDongBo, 20));
                if (dsLoiKyThuat.Count > 0)
                    mt.Add($"📌 Lỗi kỹ thuật với các CBCS: {string.Join("; ", dsLoiKyThuat.Take(15))}{(dsLoiKyThuat.Count > 15 ? "..." : "")}");
                HienThongBao(parentForm, string.Join("\n", mt), "Báo cáo đối chiếu dữ liệu", MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[Lỗi NhapDanhSachGoc] " + ex.Message);
            MessageBox.Show("Lỗi hệ thống khi đọc file Excel: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        return soThanhCong;
    }
    // =====================================================================================================
    //  SỔ VÀNG
    // =====================================================================================================
    public static Task<List<SoVangDTO>> LayDuLieuSoVangTienTienAsync(string tenBang)
        => Task.Run(() => DocSoVang(tenBang));
    private static List<SoVangDTO> DocSoVang(string tenBang)
    {
        var kq = new List<SoVangDTO>();
        try
        {
            if (!BangSoVangHopLe.Contains(tenBang))
            {
                Debug.WriteLine("[DocSoVang] Tên bảng không hợp lệ: " + tenBang);
                return kq;
            }
            string db = Module_DanduongGPS.DuongDanCSDL2;
            if (string.IsNullOrWhiteSpace(db) || !File.Exists(db)) return kq;
            using var cn = MoKetNoi(db, true);
            using var cmd = cn.CreateCommand();
            cmd.CommandText = $"SELECT ID, HoVaTen, ThongBaoTrungDoan, SoTTTrongSo, ThangCongNhan FROM [{tenBang}] ORDER BY STT ASC";
            using var rd = cmd.ExecuteReader();
            int stt = 1;
            while (rd.Read())
            {
                if (rd.IsDBNull(0) || rd.GetInt32(0) == -1) continue;
                // Giải mã tuần tự: GiaiMa đã có cache + động cơ AES theo luồng, PLINQ trên máy ít nhân chỉ tốn thêm chi phí.
                kq.Add(new SoVangDTO
                {
                    STT = stt++,
                    HoVaTen = GiaiCot(rd, 1),
                    ThongBaoTrungDoan = GiaiCot(rd, 2),
                    SoTTTrongSo = GiaiCot(rd, 3),
                    ThangCongNhan = GiaiCot(rd, 4)
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[Lỗi LayDuLieuSoVangTienTienAsync]: " + ex.Message);
            kq.Clear();
        }
        return kq;
    }
    public static async Task XuatDanhSachSoVangToExcelAsync(Form? parentForm, string filePath, string tenBangHienTai, string kyHieuTrungDoan)
    {
        ManHinhCho? cho = null;
        try
        {
            cho = new ManHinhCho(parentForm, "Đang xử lý và xuất dữ liệu ra Excel...");
            await cho.HienAsync();
            var danhSach = await LayDuLieuSoVangTienTienAsync(tenBangHienTai);
            if (danhSach.Count == 0)
            {
                cho.Dispose();
                cho = null;
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string tieuDeThongBao = $"Thông báo của {kyHieuTrungDoan}";
            await Task.Run(() =>
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("DANH_SACH");
                ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
                ws.PageSetup.PageOrientation = XLPageOrientation.Portrait;
                foreach (var (addr, text, size) in new[]
                {
                    ("A1:E1", "SỔ VÀNG", 14),
                    ("A2:E2", "BIỂU DƯƠNG GƯƠNG ĐIỂN HÌNH TRONG THỰC HIỆN PHONG TRÀO THI ĐUA BA NHẤT", 12)
                })
                {
                    var r = ws.Range(addr);
                    r.Merge();
                    r.FirstCell().Value = text;
                    r.Style.Font.Bold = true;
                    r.Style.Font.FontName = Module_HeThong.Font_Times_New_Roman;
                    r.Style.Font.FontSize = size;
                    r.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    r.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                string[] tieuDe = { "STT", "Họ và tên", tieuDeThongBao, "Vào sổ vàng số", "Ghi chú" };
                GhiTieuDeBang(ws, 4, tieuDe, new double[] { 5, 30, 40, 15, 15 }, 0, 24, XLColor.LightGray);
                var dong = new List<object[]>(danhSach.Count);
                foreach (var x in danhSach)
                    dong.Add(new object[] { x.STT, x.HoVaTen, x.ThongBaoTrungDoan, x.SoTTTrongSo, x.ThangCongNhan });
                GhiThanBang(ws, 5, dong, 5, new[] { 2, 3 }, new[] { 4, 5 }, 11, 18);
                Module_BanQuyen.DongDauExcel(wb);
                LuuTepAnToan(wb, filePath);
            });
            cho.Dispose();
            cho = null;
            MoTepExcelThiDuaPhongTrao3Nhat(filePath);
        }
        catch (Exception ex)
        {
            cho?.Dispose();
            cho = null;
            MessageBox.Show($"Lỗi khi xuất Excel: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            cho?.Dispose();
        }
    }
    // =====================================================================================================
    //  NHẬP DỮ LIỆU VÀO SỔ VÀNG TỪ EXCEL (Form 44)
    //  SỬA LỖI: bản cũ tra "SoHieu = MaHoa(soHieu)" (IV ngẫu nhiên => không bao giờ khớp) nên mỗi lần nạp lại
    //  đều INSERT thêm dòng trùng. Nay tra bằng Dictionary(SoHieu đã giải mã -> ID) và cập nhật theo ID.
    // =====================================================================================================
    private readonly record struct DongSoVang(int Dong, string HoTen, string SoHieu, string[] GiaTri);
    public static async Task<int> NhapDuLieuVaoSoVangTuExcel(Form? parentForm, string excelPath, string dbPath)
    {
        int soThanhCong = 0;
        try
        {
            if (!File.Exists(excelPath) || !File.Exists(dbPath)) return 0;
            string bang = ChonBang().SoVang;
            var dsBoQua = new List<string>();
            var dsLoiKyThuat = new List<string>();
            string? loiCauTruc = null;
            await Task.Run(() =>
            {
                loiCauTruc = KiemTraKichThuocTep(excelPath);
                if (loiCauTruc != null) return;
                using var wb = new XLWorkbook(excelPath);
                var ws = wb.Worksheets.FirstOrDefault(w => w.Name.Contains("SO_VANG") || w.Name.Contains("BANHAT"))
                         ?? wb.Worksheets.First();
                // Dò dòng tiêu đề (dòng 1..5): cột C phải là "Số hiệu" – "chìa khóa" nhận diện cán bộ
                int headerRow = 0;
                for (int i = 1; i <= 5; i++)
                {
                    if (ws.Cell(i, 3).GetString().Trim().Equals("Số hiệu", StringComparison.OrdinalIgnoreCase)) { headerRow = i; break; }
                }
                if (headerRow == 0)
                {
                    loiCauTruc = "File Excel không đúng định dạng!\nĐể nạp được dữ liệu, Cột C (Cột số 3) bắt buộc phải là 'Số hiệu'.\n(Đây là chìa khóa để nhận diện cán bộ)";
                    return;
                }
                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                if (lastRow <= headerRow) return;
                if (lastRow > DongExcelToiDa)
                {
                    loiCauTruc = $"Tệp có quá nhiều dòng (>{DongExcelToiDa}).";
                    return;
                }
                // 1) Đọc Excel vào RAM (cột 4..15 -> 12 giá trị; HoTen = cột 2)
                var dong = new List<DongSoVang>();
                for (int r = headerRow + 1; r <= lastRow; r++)
                {
                    var row = ws.Row(r);
                    string hoTen = Chuoi(row.Cell(2));
                    string soHieu = Chuoi(row.Cell(3));
                    if (hoTen.Length == 0 && soHieu.Length == 0) continue;
                    if (hoTen.Length == 0 || soHieu.Length == 0)
                    {
                        dsBoQua.Add($"- Dòng {r}: thiếu {(hoTen.Length == 0 ? "Họ tên" : "Số hiệu")}, bỏ qua.");
                        continue;
                    }
                    // Thứ tự khớp CotSoVang: HoVaTen, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu, ThanhTich, ThongBao, SoTT, Thang
                    dong.Add(new DongSoVang(r, hoTen, soHieu, new[]
                    {
                        hoTen, Chuoi(row.Cell(4)), Chuoi(row.Cell(5)), Chuoi(row.Cell(6)), Chuoi(row.Cell(7)), Chuoi(row.Cell(8)),
                        Chuoi(row.Cell(9)), Chuoi(row.Cell(10)), Chuoi(row.Cell(11)), Chuoi(row.Cell(12)), Chuoi(row.Cell(13)),
                        Chuoi(row.Cell(14)), Chuoi(row.Cell(15))
                    }));
                }
                if (dong.Count == 0) return;
                using var cn = MoKetNoi(dbPath, false);
                // 2) SoHieu hiện có -> ID (giải mã 1 lần)
                var idTheoSoHieu = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT ID, SoHieu FROM [{bang}]";
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        int id = rd.GetInt32(0);
                        if (id == -1) continue;
                        string sh = GiaiCot(rd, 1);
                        if (sh.Length > 0) idTheoSoHieu[sh] = id;
                    }
                }
                int nextSTT;
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT IFNULL(MAX(STT), 0) FROM [{bang}]";
                    nextSTT = Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) + 1;
                }
                // 3) Ghi
                using var tx = cn.BeginTransaction();
                using var upd = TaoLenhCapNhat(cn, tx, bang, CotSoVang);
                using var ins = TaoLenhThem(cn, tx, bang, CotSoVang);
                var daXuLy = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int dem = 0;
                foreach (var d in dong)
                {
                    try
                    {
                        if (!daXuLy.Add(d.SoHieu))
                        {
                            dsBoQua.Add($"- {d.HoTen} (Dòng {d.Dong}): trùng Số hiệu {d.SoHieu} trong tệp, bỏ qua.");
                            continue;
                        }
                        if (idTheoSoHieu.TryGetValue(d.SoHieu, out int id))
                        {
                            upd.Parameters["@id"].Value = id;
                            GanThamSo(upd, d.GiaTri, CotSoVang.Length);
                            upd.ExecuteNonQuery();
                        }
                        else
                        {
                            ins.Parameters["@stt"].Value = nextSTT++;
                            ins.Parameters["@sh"].Value = Module_BaoMatAES.MaHoa(d.SoHieu);
                            GanThamSo(ins, d.GiaTri, CotSoVang.Length);
                            ins.ExecuteNonQuery();
                        }
                        dem++;
                    }
                    catch (Exception ex)
                    {
                        dsLoiKyThuat.Add($"- {d.HoTen} (Dòng {d.Dong}): {ex.Message}");
                    }
                }
                tx.Commit();
                soThanhCong = dem;
            });
            if (loiCauTruc != null)
            {
                HienThongBao(parentForm, loiCauTruc, "Từ chối nạp", MessageBoxIcon.Error);
                return 0;
            }
            if (dsBoQua.Count > 0 || dsLoiKyThuat.Count > 0)
            {
                var mt = new List<string> { "⚠️ Quá trình nạp hoàn tất nhưng có lỗi tại một số dòng:\n" };
                if (dsBoQua.Count > 0) mt.Add(string.Join("\n", dsBoQua.Take(10)) + (dsBoQua.Count > 10 ? "\n..." : ""));
                if (dsLoiKyThuat.Count > 0) mt.Add(string.Join("\n", dsLoiKyThuat.Take(10)) + (dsLoiKyThuat.Count > 10 ? "\n..." : ""));
                HienThongBao(parentForm, string.Join("\n", mt), "Cảnh báo", MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[Lỗi NhapSoVang] " + ex.Message);
            MessageBox.Show("Lỗi định dạng tệp Excel: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        return soThanhCong;
    }
    // =====================================================================================================
    //  XUẤT DỮ LIỆU GỐC SỔ VÀNG (15 cột) để sao lưu / nạp lại (Form 44)
    // =====================================================================================================
    public static async Task XuatDuLieuGocSoVangRaExcelAsync(Form? parentForm, string filePathLuu)
    {
        string dbPath = Module_DanduongGPS.DuongDanCSDL2;
        if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath)) return;
        string tenBang = ChonBang().SoVang;
        ManHinhCho? cho = null;
        try
        {
            cho = new ManHinhCho(parentForm, "Đang đọc và tạo tệp excel Sổ Vàng, vui lòng đợi...");
            await cho.HienAsync();
            // Đọc + giải mã + tạo Excel trong 1 luồng nền (không tách 2 lần Task.Run)
            await Task.Run(() =>
            {
                var dong = new List<object[]>();
                using (var cn = MoKetNoi(dbPath, true))
                {
                    using var cmd = cn.CreateCommand();
                    cmd.CommandText = $"SELECT ID, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi, PhanLoai, GhiChu, ThanhTich, ThongBaoTrungDoan, SoTTTrongSo, ThangCongNhan FROM [{tenBang}] ORDER BY STT ASC";
                    using var rd = cmd.ExecuteReader();
                    int stt = 1;
                    while (rd.Read())
                    {
                        if (rd.IsDBNull(0) || rd.GetInt32(0) == -1) continue;
                        var o = new object[15];
                        o[0] = stt++;
                        for (int i = 1; i < 15; i++) o[i] = GiaiCot(rd, i);
                        dong.Add(o);
                    }
                }
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("DS_SO_VANG_CSDL");
                GhiTieuDeBang(ws, 1, TieuDeSoVangGoc, DoRongSoVangGoc, 13, 36, MauNenTieuDe);
                GhiThanBang(ws, 2, dong, 15, CotTraiSoVangGoc, CotVanBanSoVangGoc, 13, 36);
                Module_BanQuyen.DongDauExcel(wb);
                LuuTepAnToan(wb, filePathLuu);
            });
            Module_NhatKy.GhiNhatKy(
                taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                hanhDong: $"Xuất tệp excel dữ liệu gốc Sổ Vàng ({tenBang})",
                ghiChu: $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
            cho.Dispose();
            cho = null;
            MoTepExcelThiDuaPhongTrao3Nhat(filePathLuu);
        }
        catch (Exception ex)
        {
            cho?.Dispose();
            cho = null;
            MessageBox.Show("Lỗi khi xuất file Sổ vàng gốc: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            cho?.Dispose();
        }
    }
}