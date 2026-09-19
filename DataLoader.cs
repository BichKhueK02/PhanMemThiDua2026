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
            conn.Open();
            using var cmd = new SqliteCommand("SELECT * FROM DanhSach", conn);
            using var reader = cmd.ExecuteReader();
            DataTable dt = new DataTable();
            dt.Load(reader);
            // 🔥 GIẢI MÃ (GIỮ NGUYÊN LOGIC BẠN)
            foreach (DataRow row in dt.Rows)
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
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Preload lỗi: " + ex.Message);
        }
    }
}