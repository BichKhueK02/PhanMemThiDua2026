using System.Text;
namespace PhanMemThiDua2026.UITest
{
    /// <summary>
    /// Chụp lại dữ liệu của bản phần mềm TRƯỚC khi test và trả về đúng như cũ SAU mỗi lần test (phần mềm đã tắt),
    /// chỉ giữ lại nhật ký test (uitest-*.log). Bản chụp nằm ở &lt;thư mục công cụ&gt;\UITest_SaoLuu\&lt;đợt&gt; (ngoài vùng
    /// dọn dẹp của phần mềm), xóa khi xong cả đợt. Đợt bị ngắt ngang (tắt máy, diệt tiến trình) thì lần mở sau hỏi khôi phục.
    /// Khôi phục kiểu đồng bộ: chỉ chép lại tệp khác bản chụp (kích thước/giờ sửa), xóa tệp mới sinh => nhanh.
    /// </summary>
    internal sealed class SaoLuuDuLieu
    {
        public static readonly string[] CacThuMuc = { "Database", "Database Backup", "window-x64" };
        private const string TEP_DANH_DAU = "DANG_KIEM_THU.txt";
        public static string ThuMucGocSaoLuu => Path.Combine(AppContext.BaseDirectory, "UITest_SaoLuu");
        public string ThuMucPhanMem { get; }
        public string ThuMucBanChup { get; }
        public SaoLuuDuLieu(string thuMucPhanMem, string dot)
        {
            ThuMucPhanMem = thuMucPhanMem;
            ThuMucBanChup = Path.Combine(ThuMucGocSaoLuu, dot);
        }
        private SaoLuuDuLieu(string thuMucPhanMem, string banChup, bool _)
        {
            ThuMucPhanMem = thuMucPhanMem;
            ThuMucBanChup = banChup;
        }
        /// <summary>Chụp dữ liệu. Lỗi => ném ra (không được test khi chưa chụp xong).</summary>
        public void Chup()
        {
            Directory.CreateDirectory(ThuMucBanChup);
            foreach (var ten in CacThuMuc)
            {
                string nguon = Path.Combine(ThuMucPhanMem, ten);
                if (Directory.Exists(nguon)) ChepThuMuc(nguon, Path.Combine(ThuMucBanChup, ten));
            }
            // Đánh dấu ghi SAU CÙNG: có tệp này nghĩa là bản chụp đầy đủ
            File.WriteAllText(Path.Combine(ThuMucBanChup, TEP_DANH_DAU), ThuMucPhanMem, Encoding.UTF8);
        }
        /// <summary>
        /// Trả dữ liệu về như bản chụp, giữ lại các tệp nhật ký test (khớp mẫu giuLai trong Database\NhatKyNhaPhatTrien).
        /// Trả về số tệp đã chép lại + đã xóa.
        /// </summary>
        public (int ChepLai, int Xoa) KhoiPhuc(Func<string, bool> giuLai)
        {
            int chep = 0, xoa = 0;
            foreach (var ten in CacThuMuc)
            {
                string banChup = Path.Combine(ThuMucBanChup, ten);
                string dich = Path.Combine(ThuMucPhanMem, ten);
                if (!Directory.Exists(banChup))
                {
                    // Trước khi test chưa có thư mục này => do lần test sinh ra: xóa (trừ nhật ký test)
                    if (Directory.Exists(dich)) xoa += XoaTrongThuMuc(dich, dich, giuLai, null);
                    continue;
                }
                Directory.CreateDirectory(dich);
                var coTrongBanChup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var f in Directory.EnumerateFiles(banChup, "*", SearchOption.AllDirectories))
                {
                    string tuongDoi = Path.GetRelativePath(banChup, f);
                    coTrongBanChup.Add(tuongDoi);
                    string fd = Path.Combine(dich, tuongDoi);
                    if (giuLai(fd)) continue;
                    var a = new FileInfo(f);
                    var b = new FileInfo(fd);
                    if (b.Exists && b.Length == a.Length && b.LastWriteTimeUtc == a.LastWriteTimeUtc) continue;
                    Directory.CreateDirectory(Path.GetDirectoryName(fd)!);
                    ThuNhieuLan(() =>
                    {
                        if (b.Exists && b.IsReadOnly) b.IsReadOnly = false;
                        File.Copy(f, fd, true);
                    });
                    chep++;
                }
                xoa += XoaTrongThuMuc(dich, dich, giuLai, coTrongBanChup);
            }
            return (chep, xoa);
        }
        /// <summary>Xóa tệp/thư mục không có trong bản chụp (null = xóa hết), trừ tệp giữ lại.</summary>
        private static int XoaTrongThuMuc(string goc, string thuMuc, Func<string, bool> giuLai, HashSet<string>? coTrongBanChup)
        {
            int xoa = 0;
            foreach (var f in Directory.EnumerateFiles(thuMuc))
            {
                if (giuLai(f)) continue;
                if (coTrongBanChup != null && coTrongBanChup.Contains(Path.GetRelativePath(goc, f))) continue;
                ThuNhieuLan(() =>
                {
                    var fi = new FileInfo(f);
                    if (fi.IsReadOnly) fi.IsReadOnly = false;
                    fi.Delete();
                });
                xoa++;
            }
            foreach (var d in Directory.EnumerateDirectories(thuMuc))
            {
                if ((new DirectoryInfo(d).Attributes & FileAttributes.ReparsePoint) != 0) continue;
                xoa += XoaTrongThuMuc(goc, d, giuLai, coTrongBanChup);
                try { if (!Directory.EnumerateFileSystemEntries(d).Any()) Directory.Delete(d); } catch { }
            }
            return xoa;
        }
        public void XoaBanChup()
        {
            try { if (Directory.Exists(ThuMucBanChup)) Directory.Delete(ThuMucBanChup, true); } catch { }
            try { if (Directory.Exists(ThuMucGocSaoLuu) && !Directory.EnumerateFileSystemEntries(ThuMucGocSaoLuu).Any()) Directory.Delete(ThuMucGocSaoLuu); } catch { }
        }
        /// <summary>Bản chụp đầy đủ còn sót từ đợt bị ngắt ngang (nếu có).</summary>
        public static SaoLuuDuLieu? TimBanChupSot()
        {
            try
            {
                if (!Directory.Exists(ThuMucGocSaoLuu)) return null;
                foreach (var d in Directory.EnumerateDirectories(ThuMucGocSaoLuu).OrderByDescending(x => x))
                {
                    string dd = Path.Combine(d, TEP_DANH_DAU);
                    if (!File.Exists(dd)) { try { Directory.Delete(d, true); } catch { } continue; }   // bản chụp dở dang: bỏ
                    string thuMuc = File.ReadAllText(dd, Encoding.UTF8).Trim();
                    if (Directory.Exists(thuMuc)) return new SaoLuuDuLieu(thuMuc, d, true);
                }
            }
            catch { }
            return null;
        }
        private static void ChepThuMuc(string nguon, string dich)
        {
            Directory.CreateDirectory(dich);
            foreach (var f in Directory.EnumerateFiles(nguon))
            {
                string fd = Path.Combine(dich, Path.GetFileName(f));
                ThuNhieuLan(() =>
                {
                    // FileShare.ReadWrite: chép được cả tệp đang mở (nhật ký...)
                    using (var vao = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    using (var ra = new FileStream(fd, FileMode.Create, FileAccess.Write))
                        vao.CopyTo(ra);
                    File.SetLastWriteTimeUtc(fd, File.GetLastWriteTimeUtc(f));
                });
            }
            foreach (var d in Directory.EnumerateDirectories(nguon))
            {
                if ((new DirectoryInfo(d).Attributes & FileAttributes.ReparsePoint) != 0) continue;
                ChepThuMuc(d, Path.Combine(dich, Path.GetFileName(d)));
            }
        }
        /// <summary>Tệp có thể còn bị khóa vài giây sau khi phần mềm tắt => thử lại.</summary>
        private static void ThuNhieuLan(Action a)
        {
            for (int i = 0; ; i++)
            {
                try { a(); return; }
                catch (IOException) when (i < 10) { Thread.Sleep(500); }
                catch (UnauthorizedAccessException) when (i < 10) { Thread.Sleep(500); }
            }
        }
    }
}
