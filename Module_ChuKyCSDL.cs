namespace PhanMemThiDua2026
{
    /// <summary>
    /// "Chữ ký" rẻ tiền của dữ liệu: dùng để biết từ lần nạp trước tới giờ dữ liệu CÓ THAY ĐỔI hay không,
    /// nhờ đó mở lại một trang (Trang chủ, Thống kê, Khen thưởng...) mà dữ liệu không đổi thì hiện ngay, KHÔNG nạp lại.
    /// Gồm:
    ///  - bộ đếm thay đổi trong phần mềm (DataCache.Clear, ...);
    ///  - kích thước + thời điểm ghi cuối của CSDL1/2/4 và tệp -wal của chúng (SQLite chế độ WAL ghi vào tệp -wal).
    /// CSDL3 (nhật ký) cố ý KHÔNG tính: ghi nhật ký diễn ra liên tục, không ảnh hưởng dữ liệu các trang này.
    /// Chỉ đọc thuộc tính tệp (vài syscall, &lt; 1 ms), an toàn gọi trên luồng giao diện.
    /// </summary>
    internal static class Module_ChuKyCSDL
    {
        private static long _demThayDoi;
        /// <summary>Gọi khi phần mềm vừa ghi/thay dữ liệu (để chắc chắn lần mở trang kế tiếp sẽ nạp lại).</summary>
        public static void DanhDauThayDoi() => Interlocked.Increment(ref _demThayDoi);
        public static string Lay()
        {
            var sb = new System.Text.StringBuilder(160);
            sb.Append(Interlocked.Read(ref _demThayDoi));
            ThemTep(sb, Module_DanduongGPS.DuongDanCSDL1);
            ThemTep(sb, Module_DanduongGPS.DuongDanCSDL2);
            ThemTep(sb, Module_DanduongGPS.DuongDanCSDL4);
            return sb.ToString();
        }
        private static void ThemTep(System.Text.StringBuilder sb, string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            ThemMotTep(sb, path);
            ThemMotTep(sb, path + "-wal");
        }
        private static void ThemMotTep(System.Text.StringBuilder sb, string path)
        {
            try
            {
                var fi = new FileInfo(path);
                sb.Append('|');
                if (!fi.Exists) { sb.Append('-'); return; }
                sb.Append(fi.Length).Append(':').Append(fi.LastWriteTimeUtc.Ticks);
            }
            catch { sb.Append("|?"); }
        }
    }
}
