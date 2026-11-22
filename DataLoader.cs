using Microsoft.Data.Sqlite;
using PhanMemThiDua2026;
using System.Data;
using System.Diagnostics;
public static class DataLoader
{
    // Hai nơi gọi (Form2 + làm nóng Form2) có thể chạy song song ở luồng nền lúc đăng nhập:
    // khóa để lượt thứ hai chờ rồi dùng lại bộ đệm, không đọc + giải mã cả bảng thêm lần nữa.
    // Chỉ gọi từ luồng nền (luồng UI không bao giờ chờ khóa này => không thể khóa chéo với Invoke của Module_ThongBao).
    private static readonly object _khoaNap = new();
    public static void PreloadDanhSach(string csdlPath)
    {
        if (DataCache.IsLoaded) return;
        lock (_khoaNap)
        {
            NapDanhSachKhongKhoa(csdlPath);
        }
    }
    private static void NapDanhSachKhongKhoa(string csdlPath)
    {
        if (DataCache.IsLoaded) return;
        if (string.IsNullOrWhiteSpace(csdlPath) || !File.Exists(csdlPath))
            return;
        try
        {
            using var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(csdlPath));
            try
            {
                conn.Open();
            }
            catch (SqliteException sqlEx)
            {
                Debug.WriteLine($"[Lỗi Nghiêm Trọng] Không thể mở CSDL '{csdlPath}': {sqlEx.Message}");
                Debug.WriteLine($"[Nguyên Nhân] {sqlEx.InnerException?.Message}");
                Module_ThongBao.Loi($"Lỗi cơ sở dữ liệu: Không thể kết nối.\n\nChi tiết: {sqlEx.Message}\n\nVui lòng khởi động lại ứng dụng hoặc kiểm tra file CSDL.");
                return;
            }
            using var cmd = new SqliteCommand($"SELECT * FROM {Module_TaiKhoan.BangDanhSach}", conn);
            using var reader = cmd.ExecuteReader();
            DataTable dt = new DataTable();
            dt.Load(reader);
            // 🔥 GIẢI MÃ (GIỮ NGUYÊN LOGIC BẠN - nhưng thêm xử lý lỗi từng hàng)
            int countDecryptError = 0;
            foreach (DataRow row in dt.Rows)
            {
                try
                {
                    row["HoVaTen"] = Module_BaoMatAES.GiaiMa(row["HoVaTen"]?.ToString());
                    row["SoHieu"] = Module_BaoMatAES.GiaiMa(row["SoHieu"]?.ToString());
                    row["NamSinh"] = Module_BaoMatAES.GiaiMa(row["NamSinh"]?.ToString());
                    row["QueQuan"] = Module_BaoMatAES.GiaiMa(row["QueQuan"]?.ToString());
                    row["NgayVaoCAND"] = Module_BaoMatAES.GiaiMa(row["NgayVaoCAND"]?.ToString());
                    row["CapBac"] = Module_BaoMatAES.GiaiMa(row["CapBac"]?.ToString());
                    row["ChucVu"] = Module_BaoMatAES.GiaiMa(row["ChucVu"]?.ToString());
                    row["DonVi"] = Module_BaoMatAES.GiaiMa(row["DonVi"]?.ToString());
                    row["PhanLoai"] = Module_BaoMatAES.GiaiMa(row["PhanLoai"]?.ToString());
                    row["GhiChu"] = Module_BaoMatAES.GiaiMa(row["GhiChu"]?.ToString());
                }
                catch (Exception exRowDecrypt)
                {
                    countDecryptError++;
                    Debug.WriteLine($"[Cảnh Báo] Lỗi giải mã hàng (STT={row["STT"]}): {exRowDecrypt.Message}");
                    // Đặt các cột CHUỖI về rỗng thay vì crash. Cột số (ID, STT...) giữ nguyên:
                    // gán "" vào cột kiểu số sẽ ném ArgumentException ngay trong khối catch (bản trước).
                    foreach (DataColumn col in dt.Columns)
                    {
                        if (col.ColumnName != "STT" && col.DataType == typeof(string)) row[col] = string.Empty;
                    }
                }
            }
            if (countDecryptError > 0)
            {
                Debug.WriteLine($"[Cảnh Báo] Tổng cộng {countDecryptError} hàng có lỗi giải mã. Đã bỏ qua các hàng này.");
            }
            // 🔥 SORT SẴN
            string[] thuTuUuTien = Module_DonVi
                .LayDanhSachDonViUuTienArray()
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();
            // Đơn vị trùng tên trong cấu hình: giữ thứ tự xuất hiện đầu tiên (ToDictionary bản trước ném lỗi khi trùng khóa)
            var map = new Dictionary<string, int>(thuTuUuTien.Length);
            for (int i = 0; i < thuTuUuTien.Length; i++)
                map.TryAdd(thuTuUuTien[i], i);
            // Bảng rỗng: CopyToDataTable() ném InvalidOperationException => bản trước hiện nhầm lỗi "Bảng hoặc cột không tồn tại".
            // Giữ hành vi cũ là KHÔNG lưu bộ đệm (lần sau đọc lại CSDL), chỉ bỏ thông báo lỗi sai.
            if (dt.Rows.Count == 0)
            {
                Debug.WriteLine("[OK] Danh sách trống: chưa có bản ghi, không lưu bộ đệm.");
                return;
            }
            DataTable sorted = dt.AsEnumerable()
                .OrderBy(r => map.TryGetValue(r["DonVi"] as string ?? "", out int thuTu) ? thuTu : int.MaxValue)
                .ThenBy(r => LaySttDeSapXep(r["STT"]))
                .CopyToDataTable();
            DataCache.SetDanhSach(sorted);
            Debug.WriteLine($"[OK] Tải danh sách thành công: {sorted.Rows.Count} bản ghi.");
        }
        catch (FileNotFoundException fileEx)
        {
            Debug.WriteLine($"[Lỗi] Không tìm thấy file CSDL: {csdlPath}");
            Debug.WriteLine($"[Chi tiết] {fileEx.Message}");
            Module_ThongBao.Loi($"Không tìm thấy cơ sở dữ liệu:\n{csdlPath}");
        }
        catch (SqliteException sqlEx)
        {
            Debug.WriteLine($"[Lỗi] Lỗi SQLite khi tải danh sách: {sqlEx.Message}");
            Debug.WriteLine($"[Chi tiết] {sqlEx.InnerException?.Message}");
            Module_ThongBao.Loi($"Lỗi cơ sở dữ liệu:\n{sqlEx.Message}");
        }
        catch (InvalidOperationException invOpEx)
        {
            Debug.WriteLine($"[Lỗi] Lỗi hoạt động không hợp lệ: {invOpEx.Message}");
            Module_ThongBao.Loi($"Lỗi dữ liệu: Bảng hoặc cột không tồn tại trong CSDL.\n{invOpEx.Message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Lỗi Tổng Quát] Lỗi tải danh sách: {ex.Message}");
            Debug.WriteLine($"[Chi tiết] {ex.InnerException?.Message}");
            Module_ThongBao.Loi($"Lỗi hệ thống: {ex.Message}");
        }
    }
    /// <summary>
    /// Khóa sắp xếp theo STT, chịu được NULL / kiểu chữ (bản trước dùng Field&lt;long&gt; => 1 dòng STT rỗng làm hỏng cả lần nạp).
    /// STT không hợp lệ xếp cuối, giữ nguyên thứ tự tương đối (OrderBy của LINQ là sắp xếp ổn định).
    /// </summary>
    internal static long LaySttDeSapXep(object? giaTri) => giaTri switch
    {
        long l => l,
        int i => i,
        string s when long.TryParse(s.Trim(), System.Globalization.NumberStyles.Integer,
                                    System.Globalization.CultureInfo.InvariantCulture, out long kq) => kq,
        null or DBNull => long.MaxValue,
        IConvertible c => TryChuyenLong(c),
        _ => long.MaxValue
    };
    private static long TryChuyenLong(IConvertible c)
    {
        try { return c.ToInt64(System.Globalization.CultureInfo.InvariantCulture); }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException) { return long.MaxValue; }
    }
}