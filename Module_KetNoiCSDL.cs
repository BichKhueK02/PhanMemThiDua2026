using Microsoft.Data.Sqlite;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Tạo chuỗi kết nối SQLite DÙNG CHUNG cho toàn phần mềm.
    ///
    /// Vì sao không ghép tay $"Data Source={duongDan}":
    ///   đường dẫn chứa dấu ';' (thư mục lưu trữ do người dùng chọn, tệp CSDL năm cũ...) làm chuỗi ghép tay vỡ ngay
    ///   ("Format of the initialization string does not conform to specification") => không mở được CSDL.
    ///   SqliteConnectionStringBuilder tự đặt dấu nháy cho giá trị, đọc lại đúng đường dẫn gốc.
    ///
    /// CAM KẾT GIỮ NGUYÊN HÀNH VI: tham số nào để mặc định thì KHÔNG được ghi vào chuỗi, nên kết quả tương đương 1:1
    /// với chuỗi thô "Data Source=..." (Mode=ReadWriteCreate, Pooling=True, Default Timeout=30, Cache=Default).
    /// Chỉ khác ở chỗ đường dẫn được đóng nháy an toàn.
    /// </summary>
    internal static class Module_KetNoiCSDL
    {
        /// <param name="duongDan">Đường dẫn tệp .db (giữ nguyên như nơi gọi truyền vào, không chuẩn hóa).</param>
        /// <param name="chiDoc">true => Mode=ReadOnly.</param>
        /// <param name="khongTaoMoi">true => Mode=ReadWrite (tệp không tồn tại thì báo lỗi, không tạo tệp rỗng). Bỏ qua khi chiDoc=true.</param>
        /// <param name="pooling">false => đóng kết nối là nhả hẳn tệp (cần cho sao lưu / khôi phục / di chuyển tệp).</param>
        /// <param name="thoiGianCho">Default Timeout (giây): thời gian chờ khi CSDL đang bận.</param>
        /// <param name="cacheChung">true => Cache=Shared.</param>
        public static string ChuoiKetNoi(
            string? duongDan,
            bool chiDoc = false,
            bool khongTaoMoi = false,
            bool pooling = true,
            int thoiGianCho = 30,
            bool cacheChung = false)
        {
            var sb = new SqliteConnectionStringBuilder { DataSource = duongDan ?? string.Empty };
            if (chiDoc) sb.Mode = SqliteOpenMode.ReadOnly;
            else if (khongTaoMoi) sb.Mode = SqliteOpenMode.ReadWrite;
            if (!pooling) sb.Pooling = false;
            if (thoiGianCho != 30) sb.DefaultTimeout = thoiGianCho;
            if (cacheChung) sb.Cache = SqliteCacheMode.Shared;
            return sb.ToString();
        }
        /// <summary>
        /// Tạo + MỞ kết nối (tham số như <see cref="ChuoiKetNoi"/>). Mở thất bại => Dispose ngay rồi ném lại lỗi,
        /// không để rò handle tệp (giữ khóa tệp .db làm kẹt sao lưu / khôi phục).
        /// </summary>
        public static SqliteConnection MoKetNoi(
            string? duongDan,
            bool chiDoc = false,
            bool khongTaoMoi = false,
            bool pooling = true,
            int thoiGianCho = 30,
            bool cacheChung = false)
        {
            var cn = new SqliteConnection(ChuoiKetNoi(duongDan, chiDoc, khongTaoMoi, pooling, thoiGianCho, cacheChung));
            try
            {
                cn.Open();
                return cn;
            }
            catch
            {
                cn.Dispose();
                throw;
            }
        }
    }
}
