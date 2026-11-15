using Microsoft.Data.Sqlite;
using PhanMemThiDua2026;
using System.Data;
using System.Diagnostics;
public static class DataLoader
{
    public static void PreloadDanhSach(string csdlPath)
    {
        if (DataCache.IsLoaded) return;
        if (string.IsNullOrWhiteSpace(csdlPath) || !File.Exists(csdlPath))
            return;

        try
        {
            using var conn = new SqliteConnection($"Data Source={csdlPath}");

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

            using var cmd = new SqliteCommand("SELECT * FROM DanhSach", conn);
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
                    Debug.WriteLine($"[Cảnh Báo] Lỗi giải mã hàng (STT={row.Field<long>("STT")}): {exRowDecrypt.Message}");
                    // Đặt các cột về rỗng thay vì crash
                    foreach (DataColumn col in dt.Columns)
                    {
                        if (col.ColumnName != "STT") row[col] = string.Empty;
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
            var map = thuTuUuTien
                .Select((dv, i) => new { dv, i })
                .ToDictionary(x => x.dv, x => x.i);
            var sorted = dt.AsEnumerable()
                .OrderBy(r =>
                {
                    string dv = r.Field<string>("DonVi") ?? "";
                    return map.ContainsKey(dv) ? map[dv] : int.MaxValue;
                })
                .ThenBy(r => r.Field<long>("STT"))
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
}