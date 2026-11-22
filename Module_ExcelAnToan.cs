//TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
// Tiện ích xuất Excel dùng chung (ClosedXML):
//  - LuuAnToan: ghi ra tệp tạm cùng thư mục -> kiểm tra gói XLSX -> thay thế nguyên tử. Lỗi giữa chừng
//    (mất điện, diệt virus khóa tệp, tệp đang mở trong Excel) không bao giờ để lại tệp đích cụt/hỏng.
//  - KiemTraCoTheGhi: phát hiện sớm tệp đích đang mở trong Excel TRƯỚC khi tốn công dựng workbook.
//  - NapDong / DongTuMang: nạp dữ liệu bằng InsertData 1 lần (thay vì gán từng ô .Value).
//  - DatDoRongCot: tính độ rộng cột theo độ dài chuỗi (thay AdjustToContents đo từng ô bằng GDI, rất chậm).
using ClosedXML.Excel;
using System.IO.Compression;
namespace PhanMemThiDua2026
{
    internal static class Module_ExcelAnToan
    {
        private static readonly object _khoaLuu = new();
        private const int HR_SHARING_VIOLATION = unchecked((int)0x80070020);
        private const int HR_LOCK_VIOLATION = unchecked((int)0x80070021);
        // ===================== LƯU TỆP AN TOÀN =====================
        /// <summary>
        /// Lưu workbook an toàn: tệp tạm duy nhất cùng thư mục, ép ghi xuống đĩa, xác minh cấu trúc XLSX,
        /// rồi thay thế tệp đích (giữ nguyên tệp cũ nếu bất kỳ bước nào thất bại).
        /// </summary>
        public static void LuuAnToan(XLWorkbook wb, string fileDich)
        {
            ArgumentNullException.ThrowIfNull(wb);
            ArgumentException.ThrowIfNullOrWhiteSpace(fileDich);
            string dich = Path.GetFullPath(fileDich);
            string duoi = Path.GetExtension(dich);
            if (string.IsNullOrEmpty(duoi)) { duoi = ".xlsx"; dich += duoi; }
            string thuMuc = Path.GetDirectoryName(dich)
                ?? throw new IOException("Không xác định được thư mục lưu Excel.");
            lock (_khoaLuu)
            {
                Directory.CreateDirectory(thuMuc);
                KiemTraCoTheGhi(dich);
                // Giữ đúng phần mở rộng (.xlsx/.xlsm) vì ClosedXML chọn định dạng theo đuôi tệp
                string tam = Path.Combine(thuMuc, $".{Path.GetFileNameWithoutExtension(dich)}.{Guid.NewGuid():N}.tmp{duoi}");
                try
                {
                    // Ghi vào MemoryStream rồi mới xuống đĩa:
                    //  (1) ClosedXML không từ chối đuôi tệp riêng (vd tệp sao lưu .pmtd...);
                    //  (2) sau SaveAs, ClosedXML giữ tham chiếu tới đích lần trước để làm nguồn cho lần lưu sau. Nếu đích là
                    //      tệp tạm (đã đổi tên) hay FileStream (đã đóng) thì lần lưu thứ 2 sẽ lỗi. MemoryStream không Dispose
                    //      -> luôn còn sống cùng workbook, GC tự thu hồi khi workbook bị giải phóng.
                    var bo = new MemoryStream();
                    wb.SaveAs(bo);
                    using (var fs = new FileStream(tam, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1 << 16))
                    {
                        bo.Position = 0;
                        bo.CopyTo(fs);
                        fs.Flush(true);
                    }
                    XacMinhGoiXlsx(tam);
                    ThayThe(tam, dich);
                }
                finally
                {
                    try { if (File.Exists(tam)) File.Delete(tam); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }
        /// <summary>
        /// Ném IOException có thông báo dễ hiểu nếu tệp đích đang bị chương trình khác (thường là Excel) mở.
        /// Gọi trước khi dựng workbook để người dùng không phải chờ xong mới biết lỗi.
        /// </summary>
        public static void KiemTraCoTheGhi(string fileDich)
        {
            if (string.IsNullOrWhiteSpace(fileDich) || !File.Exists(fileDich)) return;
            try
            {
                using var fs = new FileStream(fileDich, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException ex) when (LaLoiKhoaTep(ex))
            {
                throw new IOException(ThongBaoTepDangMo(fileDich), ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new IOException($"Không có quyền ghi tệp '{Path.GetFileName(fileDich)}' (tệp chỉ đọc hoặc thư mục bị khóa).", ex);
            }
        }
        private static void XacMinhGoiXlsx(string tep)
        {
            using (var fs = new FileStream(tep, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                if (fs.Length == 0) throw new InvalidDataException("Tệp Excel tạm rỗng.");
                fs.Flush(true);   // ép ghi xuống đĩa trước khi thay thế
            }
            using var zip = ZipFile.OpenRead(tep);   // tệp cụt/hỏng sẽ ném InvalidDataException tại đây
            var wbXml = zip.GetEntry("xl/workbook.xml");
            if (zip.GetEntry("[Content_Types].xml") is null || wbXml is null)
                throw new InvalidDataException("Tệp Excel tạm không có cấu trúc XLSX hợp lệ.");
            using var s = wbXml.Open();
            s.CopyTo(Stream.Null);   // đọc thử để chắc chắn giải nén được
        }
        private static void ThayThe(string tam, string dich)
        {
            try
            {
                if (!File.Exists(dich)) { File.Move(tam, dich); return; }
                try
                {
                    File.Replace(tam, dich, null);   // nguyên tử trên NTFS, giữ thuộc tính/ACL của tệp đích
                }
                catch (PlatformNotSupportedException) { File.Move(tam, dich, overwrite: true); }
                catch (IOException ex) when (!LaLoiKhoaTep(ex))
                {
                    // Một số ổ mạng/USB FAT32 không hỗ trợ ReplaceFile -> dùng Move đè
                    File.Move(tam, dich, overwrite: true);
                }
            }
            catch (IOException ex) when (LaLoiKhoaTep(ex))
            {
                throw new IOException(ThongBaoTepDangMo(dich), ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new IOException($"Không có quyền ghi đè tệp '{Path.GetFileName(dich)}'. Tệp có thể đang mở hoặc ở chế độ chỉ đọc.", ex);
            }
        }
        private static bool LaLoiKhoaTep(IOException ex) => ex.HResult is HR_SHARING_VIOLATION or HR_LOCK_VIOLATION;
        private static string ThongBaoTepDangMo(string tep) =>
            $"Tệp '{Path.GetFileName(tep)}' đang được mở bởi chương trình khác (thường là Excel). Hãy đóng tệp rồi xuất lại.";
        // ===================== NẠP DỮ LIỆU NHANH =====================
        /// <summary>Chuỗi rỗng -> null để ô Excel thật sự trống (không phải ô chứa chuỗi rỗng).</summary>
        public static object? ChuanHoa(object? v) => v is string s && s.Length == 0 ? null : (v is DBNull ? null : v);
        /// <summary>
        /// Nạp toàn bộ các dòng vào trang tính bằng 1 lệnh InsertData (nhanh hơn gán từng ô nhiều lần).
        /// Trả về vùng dữ liệu vừa ghi (null nếu không có dòng nào).
        /// </summary>
        public static IXLRange? NapDong(IXLWorksheet ws, int hangDau, int cotDau, IReadOnlyList<object?[]> dong)
        {
            if (dong.Count == 0) return null;
            int soCot = 0;
            for (int i = 0; i < dong.Count; i++) if (dong[i].Length > soCot) soCot = dong[i].Length;
            for (int i = 0; i < dong.Count; i++)
            {
                var d = dong[i];
                for (int c = 0; c < d.Length; c++) d[c] = ChuanHoa(d[c]);
            }
            ws.Cell(hangDau, cotDau).InsertData(dong);
            return ws.Range(hangDau, cotDau, hangDau + dong.Count - 1, cotDau + Math.Max(soCot, 1) - 1);
        }
        /// <summary>
        /// Nạp dãy dòng LƯỜI (stream thẳng từ DataReader/DataView) bằng 1 lệnh InsertData, không tạo List trung gian.
        /// Dãy rỗng thì không ghi gì. Trả về true nếu có ít nhất 1 dòng.
        /// </summary>
        public static bool NapDongLuoi(IXLWorksheet ws, int hangDau, int cotDau, IEnumerable<object?[]> dong)
        {
            using var it = dong.GetEnumerator();
            if (!it.MoveNext()) return false;
            ws.Cell(hangDau, cotDau).InsertData(TiepTuc(it));
            return true;
            static IEnumerable<object?[]> TiepTuc(IEnumerator<object?[]> e)
            {
                do
                {
                    var d = e.Current;
                    for (int c = 0; c < d.Length; c++) d[c] = ChuanHoa(d[c]);
                    yield return d;
                } while (e.MoveNext());
            }
        }
        // ===================== ĐỘ RỘNG CỘT =====================
        /// <summary>
        /// Đặt độ rộng cột theo độ dài nội dung dài nhất (tiêu đề + dữ liệu). Thay thế AdjustToContents
        /// (đo từng ô bằng GDI, chậm và chiếm RAM với hàng nghìn dòng). Chỉ quét tối đa <paramref name="soDongMau"/> dòng.
        /// </summary>
        public static void DatDoRongCot(IXLWorksheet ws, IReadOnlyList<object?[]> dong, int cotDau = 1,
            IReadOnlyList<string?>? tieuDe = null, double toiThieu = 6, double toiDa = 60, int soDongMau = 5000)
        {
            int soCot = tieuDe?.Count ?? 0;
            int n = Math.Min(dong.Count, soDongMau);
            for (int i = 0; i < n; i++) if (dong[i].Length > soCot) soCot = dong[i].Length;
            for (int c = 0; c < soCot; c++)
            {
                int maxLen = 0;
                if (tieuDe != null && c < tieuDe.Count && tieuDe[c] is string td)
                    foreach (var phan in td.Split('\n')) if (phan.Length > maxLen) maxLen = phan.Length;
                for (int r = 0; r < n; r++)
                {
                    var d = dong[r];
                    if (c >= d.Length || d[c] is null) continue;
                    int len = d[c] is string s ? s.Length : Convert.ToString(d[c])?.Length ?? 0;
                    if (len > maxLen) maxLen = len;
                }
                ws.Column(cotDau + c).Width = Math.Clamp(maxLen * 1.15 + 2, toiThieu, toiDa);
            }
        }
        /// <summary>Kẻ viền mảnh trong + ngoài cho cả vùng bằng 1 lần đặt style (thay vì đặt viền từng ô).</summary>
        public static void KeVien(IXLRange? vung)
        {
            if (vung is null) return;
            vung.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            vung.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }
    }
}
