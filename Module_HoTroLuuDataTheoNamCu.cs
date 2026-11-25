using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
namespace PhanMemThiDua2026
{
    // KHU VỰC CÁC CLASS DTO (DATA TRANSFER OBJECT)
    public class FileLichSuDTO
    {
        //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
        public int STT { get; set; }
        public string TenHienThi { get; set; } = null!;
        public string DuongDan { get; set; } = null!;
        /// <summary>Nhóm xét theo tuần (Tân binh HOẶC Học viên) - chỉ để rẽ nhánh giao diện/logic.</summary>
        public bool LaTanBinh { get; set; }
        /// <summary>Chế độ của tệp năm cũ (CBCS / Tân binh / Học viên).</summary>
        public CheDoDoiTuong CheDo { get; set; }
        /// <summary>Bảng thi đua cá nhân trong tệp năm cũ: ThiDuaThang_CBCS / ThiDuaThang_TanBinh / ThiDuaThang_HocVien.</summary>
        public string BangThiDua => Module_CheDoDoiTuong.ThiDuaThang(CheDo);
    }
    public class HistoryCBCSDTO
    {
        // Các trường được nạp ngay sau khi tạo đối tượng (đọc CSDL năm cũ) => "= null!" chỉ để báo trình biên dịch, không sinh mã.
        public string ID = null!, HoVaTen = null!, SoHieu = null!, DonVi = null!, TinhTrang = null!;
        public string KQ_TD = null!, KQ_XL_CB = null!, KQ_XL_DV = null!, T12 = null!;
        public string[] Thang = new string[11]; // Thang_1 -> Thang_11
        public string Sau_Thang_Dau_Nam = null!, TongKet_Nam = null!, TS_Loai1 = null!, TS_Loai2 = null!, TS_Loai3 = null!, TS_Loai4 = null!;
        public string GhiChu = null!;
        // Thuộc tính phục vụ thuật toán tìm kiếm và sắp xếp
        public string HoVaTen_Search = null!;
        public int SortPriority;
        public Dictionary<string, string> CotPhatSinh = new Dictionary<string, string>();
    }
    public class HistoryTanBinhDTO
    {
        public string ID = null!, HoVaTen = null!, SoHieu = null!, DonVi = null!, TinhTrang = null!;
        public string TS_Loai1 = null!, TS_Loai2 = null!, TS_Loai3 = null!, TS_Loai4 = null!;
        public string GhiChu = null!;
        // Thuộc tính phục vụ thuật toán tìm kiếm và sắp xếp
        public string HoVaTen_Search = null!;
        public int SortPriority;
        public Dictionary<string, string> DuLieuThang = new Dictionary<string, string>(); // Lưu tuần và kết quả tháng
        public Dictionary<string, string> CotPhatSinh = new Dictionary<string, string>();
    }
    // CLASS CHÍNH: XỬ LÝ LƯU TRỮ VÀ LỊCH SỬ
    internal static class Module_HoTroLuuDataTheoNamCu
    {
        // ===================== DỌN TỆP NĂM CŨ RỖNG (không để rác trong LuuTruThiDua_LichSu) =====================
        /// <summary>
        /// Các bảng DỮ LIỆU THẬT của từng họ tệp lịch sử. Mọi bảng trong danh sách đều trống (hoặc đã bị xóa hẳn) => tệp là rác.
        /// Bảng tổng ThongKe_DuocKhenThuong_* không tính: xóa giấy khen chỉ đặt SoLuong_Khen = 0, dòng vẫn còn.
        /// Tên tệp không thuộc họ nào => null (KHÔNG BAO GIỜ đụng tới).
        /// </summary>
        private static string[]? BangDuLieuCuaTep(string? duongDan)
        {
            string ten = Path.GetFileName(duongDan ?? "");
            if (!ten.EndsWith(".db", StringComparison.OrdinalIgnoreCase)) return null;
            if (ten.StartsWith("KhenThuong_", StringComparison.OrdinalIgnoreCase) && ten.Contains("_Nam", StringComparison.OrdinalIgnoreCase))
            {
                var c = Module_CheDoDoiTuong.TuTenTep(ten);   // KhenThuong_CBCS_ / _TanBinh_ / _HocVien_ : bảng theo chế độ của chính tệp
                return new[] { Module_CheDoDoiTuong.GiayKhen(c), Module_CheDoDoiTuong.KhenThuongTapThe(c) };   // Form50 cá nhân + Form51 tập thể
            }
            if (ten.StartsWith("ThiDua_CBCS_Nam", StringComparison.OrdinalIgnoreCase))
                return new[] { Module_CheDoDoiTuong.ThiDuaThang(CheDoDoiTuong.CBCS), Module_CheDoDoiTuong.CBCS_PhanLoaiTapThe };   // Form46 cá nhân + Form59 tập thể
            if (ten.StartsWith("ThiDua_TanBinh_Nam", StringComparison.OrdinalIgnoreCase))
                return new[] { Module_CheDoDoiTuong.ThiDuaThang(CheDoDoiTuong.TanBinh) };
            if (ten.StartsWith("ThiDua_HocVien_Nam", StringComparison.OrdinalIgnoreCase))
                return new[] { Module_CheDoDoiTuong.ThiDuaThang(CheDoDoiTuong.HocVien) };
            return null;
        }
        /// <summary>
        /// Xóa hẳn tệp năm cũ (kèm -wal/-shm/-journal) khi MỌI bảng dữ liệu của nó đều trống / không còn.
        /// An toàn: chỉ tệp nằm đúng trong thư mục lịch sử + đúng họ tên; tệp không mở được (đang khóa, hỏng) thì GIỮ nguyên,
        /// không đoán là rỗng; tệp đang bị nơi khác mở thì Windows từ chối xóa => bỏ qua, lần quét sau thử lại.
        /// Trả về true nếu đã xóa.
        /// </summary>
        public static bool XoaTepLichSuNeuRong(string? duongDan)
        {
            string[]? bang = BangDuLieuCuaTep(duongDan);
            if (bang == null || string.IsNullOrEmpty(duongDan) || !File.Exists(duongDan)) return false;
            // Tệp của bản trước còn tên bảng cũ (ThiDuaThang, ThongKe_GiayKhen...) => đổi tên TRƯỚC khi xét rỗng,
            // nếu không bảng tên mới "không có" bị hiểu là trống và tệp còn dữ liệu bị xóa nhầm.
            Module_TenCotKhongDau.ChuanHoaTepNamCu(duongDan);
            try
            {
                string thuMuc = Path.GetFullPath(Module_DanduongGPS.ThuMucLichSuThiDua).TrimEnd(Path.DirectorySeparatorChar);
                string cha = Path.GetDirectoryName(Path.GetFullPath(duongDan))?.TrimEnd(Path.DirectorySeparatorChar) ?? "";
                if (!string.Equals(thuMuc, cha, StringComparison.OrdinalIgnoreCase)) return false;
                using (var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(duongDan, chiDoc: true)))
                {
                    conn.Open();
                    foreach (string b in bang)
                    {
                        using var kt = conn.CreateCommand();
                        kt.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$t LIMIT 1";
                        kt.Parameters.AddWithValue("$t", b);
                        if (kt.ExecuteScalar() == null) continue;                    // bảng đã bị xóa hẳn (DROP) = trống
                        using var co = conn.CreateCommand();
                        co.CommandText = $"SELECT EXISTS(SELECT 1 FROM \"{b}\" LIMIT 1)";
                        if (Convert.ToInt64(co.ExecuteScalar()) == 1) return false;  // còn dữ liệu => giữ tệp
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[XoaTepLichSuNeuRong] Không đọc được {duongDan}: {ex.Message}");
                return false;
            }
            try
            {
                SqliteConnection.ClearAllPools();   // nhả mọi kết nối đang giữ tệp (kể cả kết nối chỉ đọc vừa dùng)
                File.Delete(duongDan);
                foreach (string duoi in new[] { "-wal", "-shm", "-journal" })
                    if (File.Exists(duongDan + duoi)) File.Delete(duongDan + duoi);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Debug.WriteLine($"[XoaTepLichSuNeuRong] Chưa xóa được {duongDan}: {ex.Message}");
                return false;
            }
            try
            {
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Tự xóa tệp CSDL năm cũ không còn dữ liệu",
                    $"{Path.GetFileName(duongDan)} (mọi bảng dữ liệu đã trống)");
            }
            catch { }
            return true;
        }
        /// <summary>Quét cả thư mục lịch sử, xóa mọi tệp năm cũ đã rỗng (rác còn sót từ các bản trước). Trả về số tệp đã xóa.</summary>
        public static int DonRacThuMucLichSu()
        {
            int dem = 0;
            try
            {
                string dir = Module_DanduongGPS.ThuMucLichSuThiDua;
                if (!Directory.Exists(dir)) return 0;
                foreach (string f in Directory.GetFiles(dir, "*.db"))
                    if (XoaTepLichSuNeuRong(f)) dem++;
            }
            catch (Exception ex) { Debug.WriteLine("[DonRacThuMucLichSu] " + ex.Message); }
            return dem;
        }
        public static List<FileLichSuDTO> LayDanhSachFileLichSu()
        {
            DonRacThuMucLichSu();   // tệp năm cũ đã rỗng thì xóa luôn, không liệt kê rác
            var danhSach = new List<FileLichSuDTO>();
            string dir = Module_DanduongGPS.ThuMucLichSuThiDua;
            if (!Directory.Exists(dir)) return danhSach;
            var cheDoHienTai = Module_TaiKhoan.LayPhienBanPhanMem().LayCheDo();
            var files = Directory.GetFiles(dir, "*.db");
            foreach (var file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                if (!fileName.StartsWith("ThiDua_", StringComparison.OrdinalIgnoreCase)) continue;   // chỉ tệp thi đua (không lấy KhenThuong_...)
                var cheDoTep = Module_CheDoDoiTuong.TuTenTep(fileName);
                if (cheDoTep != cheDoHienTai) continue;   // mỗi chế độ chỉ thấy tệp năm cũ của chính nó
                bool isFileTanBinh = Module_CheDoDoiTuong.LaNhomTuan(cheDoTep);
                string nam = fileName.Split(new string[] { "Nam" }, StringSplitOptions.None).LastOrDefault() ?? "???";
                danhSach.Add(new FileLichSuDTO
                {
                    TenHienThi = $"Năm {nam} - {(isFileTanBinh ? Module_TaiKhoan.TenTB_Hoa : "CBCS")}",
                    DuongDan = file,
                    LaTanBinh = isFileTanBinh,
                    CheDo = cheDoTep
                });
            }
            // Thay dòng này:
            // return danhSach.OrderByDescending(x => x.TenHienThi).ToList();
            // BẰNG ĐOẠN CODE NÀY:
            var ketQua = danhSach.OrderByDescending(x => x.TenHienThi).ToList();
            // Đánh số thứ tự sau khi đã sắp xếp năm giảm dần
            for (int i = 0; i < ketQua.Count; i++)
            {
                ketQua[i].STT = i + 1;
            }
            return ketQua;
        }
        public static List<FileLichSuDTO> LayDanhSachFileLichSu_KhenThuongCaNhan()
        {
            DonRacThuMucLichSu();   // tệp năm cũ đã rỗng (cả giấy khen + tập thể) thì xóa luôn
            var danhSach = new List<FileLichSuDTO>();
            // Cả 3 chế độ đều có khen thưởng: chỉ liệt kê tệp năm cũ của ĐÚNG chế độ đang chạy
            var cheDo = Module_CheDoDoiTuong.Hien;
            string tienTo = Module_CheDoDoiTuong.TienToTepKhenThuong(cheDo);
            string dir = Module_DanduongGPS.ThuMucLichSuThiDua;
            if (!Directory.Exists(dir)) return danhSach;
            var files = Directory.GetFiles(dir, tienTo + "*.db");
            foreach (var file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                string nam = fileName.Replace(tienTo, "", StringComparison.OrdinalIgnoreCase);
                danhSach.Add(new FileLichSuDTO
                {
                    TenHienThi = $"Năm {nam}",
                    DuongDan = file,
                    LaTanBinh = Module_CheDoDoiTuong.LaNhomTuan(cheDo),
                    CheDo = cheDo
                });
            }
            // Thay dòng này:
            // return danhSach.OrderByDescending(x => x.TenHienThi).ToList();
            // BẰNG ĐOẠN CODE NÀY:
            var ketQua = danhSach.OrderByDescending(x => x.TenHienThi).ToList();
            // Đánh số thứ tự sau khi đã sắp xếp năm giảm dần
            for (int i = 0; i < ketQua.Count; i++)
            {
                ketQua[i].STT = i + 1;
            }
            return ketQua;
        }
        public static DataTable LoadDataFromHistoryDB(string dbPath, string tableName)
        {
            DataTable dt = new DataTable();
            if (!File.Exists(dbPath)) return dt;
            try
            {
                using (var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(dbPath, chiDoc: true)))
                {
                    cn.Open();
                    using var cmd = cn.CreateCommand();
                    cmd.CommandText = $"SELECT * FROM [{tableName}]";
                    using var rd = cmd.ExecuteReader();
                    dt.Load(rd);
                }
                string[] secureCols = { "HoVaTen", "SoHieu", "DonVi", "GhiChu" };   // GhiChu: chép nguyên bản mã hóa từ danh sách gốc (csdl2)
                foreach (DataRow row in dt.Rows)
                {
                    foreach (string colName in secureCols)
                    {
                        if (dt.Columns.Contains(colName) && row[colName] != DBNull.Value)
                        {
                            string? value = row[colName].ToString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                try
                                {
                                    string decrypted = Module_BaoMatAES.GiaiMa(value);
                                    row[colName] = string.IsNullOrEmpty(decrypted) ? value : decrypted;
                                }
                                catch
                                {
                                    // Giữ nguyên giá trị gốc
                                }
                            }
                        }
                    }
                }
                Debug.WriteLine($"Load thành công CSDL năm cũ: {dt.Rows.Count} dòng, {dt.Columns.Count} cột.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi Load DB Lịch sử: {ex.Message}");
            }
            return dt;
        }
        private static string SafeDecrypt(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            try
            {
                string result = Module_BaoMatAES.GiaiMa(input);
                return string.IsNullOrEmpty(result) ? input : result;
            }
            catch { return input; }
        }
        // LÕI HỆ THỐNG: HÀM REVERSE ATTACH & CLONE DDL
        // GIÚP COPY 100% CẤU TRÚC (PRIMARY KEY, AUTOINCREMENT, INDEX) TỪ DB GỐC
        private static void SaoChepBangVaDuLieuChuanXac(string sourceDbPath, string targetDbPath, string[] tableNames, bool thayBangCu = false)
        {
            // Tệp đích/nguồn của bản trước còn tên bảng cũ (ThiDuaThang...) => đổi tên trước, tránh bảng cũ sót lại giữ chỉ mục trùng tên
            Module_TenCotKhongDau.ChuanHoaTepNamCu(targetDbPath);
            Module_TenCotKhongDau.ChuanHoaTepNamCu(sourceDbPath);
            var schemaStatements = new List<string>();
            var cacBangThucTeCo = new List<string>();
            // BƯỚC 1: Lấy schema gốc (Nguyên văn câu lệnh Create Table, Index, Primary Key, AutoIncrement...)
            using (var cnSource = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(sourceDbPath, pooling: false)))
            {
                cnSource.Open();
                foreach (string tbl in tableNames)
                {
                    using (var cmd = cnSource.CreateCommand())
                    {
                        // Sắp xếp ưu tiên: Tạo Table trước, sau đó mới tạo Index/Trigger
                        cmd.CommandText = @"
                            SELECT sql FROM sqlite_master 
                            WHERE tbl_name = @TenBang AND sql IS NOT NULL 
                            ORDER BY CASE type WHEN 'table' THEN 1 WHEN 'index' THEN 2 WHEN 'trigger' THEN 3 ELSE 4 END ASC;";
                        cmd.Parameters.AddWithValue("@TenBang", tbl);
                        using (var reader = cmd.ExecuteReader())
                        {
                            bool hasTable = false;
                            while (reader.Read())
                            {
                                schemaStatements.Add(reader.GetString(0));
                                hasTable = true;
                            }
                            if (hasTable) cacBangThucTeCo.Add(tbl);
                        }
                    }
                }
            }
            if (cacBangThucTeCo.Count == 0)
                throw new Exception("Không tìm thấy bảng dữ liệu nào trong CSDL nguồn để sao lưu.");
            // BƯỚC 2: Mở CSDL đích (năm cũ), đính kèm CSDL gốc, rồi (xóa bảng cũ) + tạo cấu trúc + bơm dữ liệu TRONG 1 GIAO DỊCH:
            // lỗi ở bất kỳ bước nào => rollback, tệp đích giữ nguyên như trước (quan trọng khi cập nhật lại tệp đã có).
            using (var cnTarget = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(targetDbPath, pooling: false)))
            {
                cnTarget.Open();
                using (var cmd = cnTarget.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA busy_timeout=5000;";
                    cmd.ExecuteNonQuery();
                }
                // ATTACH không chạy được bên trong giao dịch => đính kèm trước
                using (var cmd = cnTarget.CreateCommand())
                {
                    cmd.CommandText = $"ATTACH DATABASE '{sourceDbPath.Replace("'", "''")}' AS DBNguon;";
                    cmd.ExecuteNonQuery();
                }
                try
                {
                    using var tran = cnTarget.BeginTransaction();
                    void Chay(string sql)
                    {
                        using var cmd = cnTarget.CreateCommand();
                        cmd.Transaction = tran;
                        cmd.CommandText = sql;
                        cmd.ExecuteNonQuery();
                    }
                    if (thayBangCu)
                        foreach (string tbl in cacBangThucTeCo) Chay($"DROP TABLE IF EXISTS main.[{tbl}];");   // xóa cả index/trigger đi kèm
                    // Chạy DDL gốc để tái tạo hoàn hảo bảng, ràng buộc và index (tên không kèm tiền tố => tạo trong main)
                    foreach (string sqlCreate in schemaStatements) Chay(sqlCreate);
                    // Bơm toàn bộ dữ liệu
                    foreach (string tbl in cacBangThucTeCo) Chay($"INSERT INTO main.[{tbl}] SELECT * FROM DBNguon.[{tbl}];");
                    tran.Commit();
                }
                finally
                {
                    // Luôn luôn tháo CSDL gốc ra dù có lỗi hay không (Defensive)
                    if (cnTarget.State == ConnectionState.Open)
                    {
                        try
                        {
                            using var cmd = cnTarget.CreateCommand();
                            cmd.CommandText = "DETACH DATABASE DBNguon;";
                            cmd.ExecuteNonQuery();
                        }
                        catch { }
                    }
                }
            }
        }
        // 3 HÀM LƯU TRỮ ĐƯỢC CẬP NHẬT GỌN GÀNG VÀ CHUẨN XÁC
        /// <summary>Đường dẫn tệp lưu kết quả thi đua của NĂM HỆ THỐNG theo phiên bản đang dùng (ThiDua_CBCS_Nam2026.db...).</summary>
        public static string DuongDanTepThiDuaNamHienTai()
        {
            int nam = Module_HeThong.LayNamHeThong();
            var cheDo = Module_TaiKhoan.LayPhienBanPhanMem().LayCheDo();
            return Path.Combine(Module_DanduongGPS.ThuMucLichSuThiDua, $"ThiDua_{Module_CheDoDoiTuong.MaTep(cheDo)}_Nam{nam}.db");
        }
        /// <summary>
        /// Lưu kết quả thi đua năm (csdl4) sang tệp năm cũ, KÈM cột GhiChu lấy từ danh sách gốc (csdl2) theo Số hiệu.
        /// <paramref name="capNhatTepDaCo"/> = true: tệp đã có thì thay các bảng thi đua trong tệp bằng dữ liệu mới (giữ nguyên tệp).
        /// </summary>
        public static string LuuTruDuLieuThiDuaNam(bool capNhatTepDaCo = false)
        {
            int nam = Module_HeThong.LayNamHeThong();
            bool laTanBinh = Module_TaiKhoan
                .LayPhienBanPhanMem()
                .LaCheDoTanBinh();
            string sourcePath = Module_DanduongGPS.DuongDanCSDL4;
            string thuMucLuu = Module_DanduongGPS.ThuMucLichSuThiDua;
            string targetPath = DuongDanTepThiDuaNamHienTai();
            // Kiểm tra CSDL nguồn
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException(
                    "Không tìm thấy cơ sở dữ liệu nguồn (CSDL4).",
                    sourcePath);
            // Đảm bảo thư mục lưu trữ tồn tại
            Directory.CreateDirectory(thuMucLuu);
            // Không ghi đè dữ liệu lưu trữ cũ (trừ khi người dùng chọn cập nhật lại tệp đã có)
            bool tepDaCo = File.Exists(targetPath);
            if (tepDaCo && !capNhatTepDaCo)
                throw new InvalidOperationException(
                    $"Dữ liệu lưu trữ năm {nam} đã tồn tại.");
            string bangThiDua = Module_CheDoDoiTuong.ThiDuaThang(Module_TaiKhoan.LayPhienBanPhanMem().LayCheDo());
            string[] tablesToBackup = laTanBinh
                ? new[] { bangThiDua }
                : new[] { bangThiDua, Module_CheDoDoiTuong.CBCS_PhanLoaiTapThe };
            try
            {
                SaoChepBangVaDuLieuChuanXac(
                    sourcePath,
                    targetPath,
                    tablesToBackup,
                    thayBangCu: tepDaCo);
                GhiCotGhiChuTuDanhSachGoc(Module_DanduongGPS.DuongDanCSDL2, targetPath, bangThiDua);
                return targetPath;
            }
            catch
            {
                // Xóa file dở dang nếu quá trình sao lưu thất bại (CHỈ tệp vừa tạo; tệp đã có thì giao dịch đã rollback)
                try
                {
                    if (!tepDaCo && File.Exists(targetPath))
                    {
                        SqliteConnection.ClearAllPools();
                        File.Delete(targetPath);
                    }
                }
                catch
                {
                    // Không che mất exception gốc.
                }
                throw;
            }
        }
        /// <summary>
        /// Bảng thi đua của csdl4 KHÔNG có cột Ghi chú: Form15 hiển thị Ghi chú bằng cách ghép Số hiệu với danh sách gốc (csdl2).
        /// Vì vậy khi lưu năm phải tự thêm cột GhiChu vào tệp năm cũ và chép Ghi chú của từng người (theo Số hiệu) tại thời điểm lưu,
        /// để sau này danh sách gốc có đổi thì tệp năm cũ vẫn giữ đúng ghi chú của năm đó. Giá trị chép NGUYÊN BẢN (đang mã hóa
        /// như trong csdl2), lúc đọc mới giải mã. Số hiệu trùng trong danh sách gốc => lấy dòng đầu (giống Form15).
        /// Ô đã có Ghi chú (bảng nguồn sau này có cột GhiChu riêng) thì giữ nguyên, chỉ điền ô trống.
        /// </summary>
        public static void GhiCotGhiChuTuDanhSachGoc(string pathCsdl2, string pathCsdlNamCu, string tenBang)
        {
            if (!File.Exists(pathCsdlNamCu)) return;
            // 1. Ghi chú của danh sách gốc: Số hiệu (đã giải mã, Trim) -> Ghi chú (nguyên bản)
            var ghiChuTheoSoHieu = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(pathCsdl2))
            {
                var tho = new List<(string sh, string gc)>();
                using (var cn2 = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(pathCsdl2, chiDoc: true)))
                {
                    cn2.Open();
                    string bangGoc = Module_TaiKhoan.BangDanhSach;
                    if (CoCot(cn2, bangGoc, "SoHieu") && CoCot(cn2, bangGoc, "GhiChu"))
                    {
                        using var cmd = new SqliteCommand($"SELECT SoHieu, GhiChu FROM [{bangGoc}] ORDER BY rowid", cn2);
                        using var rd = cmd.ExecuteReader();
                        while (rd.Read())
                            tho.Add((rd.IsDBNull(0) ? "" : rd.GetValue(0)?.ToString() ?? "",
                                     rd.IsDBNull(1) ? "" : rd.GetValue(1)?.ToString() ?? ""));
                    }
                }
                var shGiaiMa = new string[tho.Count];
                Parallel.For(0, tho.Count, i => shGiaiMa[i] = SafeDecrypt(tho[i].sh).Trim());
                for (int i = 0; i < tho.Count; i++)
                    if (shGiaiMa[i].Length > 0) ghiChuTheoSoHieu.TryAdd(shGiaiMa[i], tho[i].gc);
            }
            // 2. Thêm cột (nếu chưa có) + điền Ghi chú cho từng dòng của tệp năm cũ, trong 1 giao dịch
            using var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(pathCsdlNamCu, pooling: false));
            cn.Open();
            using (var cmd = new SqliteCommand("PRAGMA busy_timeout=5000;", cn)) cmd.ExecuteNonQuery();
            if (!CoCot(cn, tenBang, "SoHieu")) return;
            var canGhi = new List<(long id, string gc)>();
            bool daCoCot = CoCot(cn, tenBang, "GhiChu");
            using (var cmd = new SqliteCommand(daCoCot
                       ? $"SELECT rowid, SoHieu FROM [{tenBang}] WHERE GhiChu IS NULL OR TRIM(GhiChu) = ''"
                       : $"SELECT rowid, SoHieu FROM [{tenBang}]", cn))
            using (var rd = cmd.ExecuteReader())
            {
                var tho = new List<(long id, string sh)>();
                while (rd.Read()) tho.Add((rd.GetInt64(0), rd.IsDBNull(1) ? "" : rd.GetValue(1)?.ToString() ?? ""));
                var sh = new string[tho.Count];
                Parallel.For(0, tho.Count, i => sh[i] = SafeDecrypt(tho[i].sh).Trim());
                for (int i = 0; i < tho.Count; i++)
                    if (sh[i].Length > 0 && ghiChuTheoSoHieu.TryGetValue(sh[i], out string? gc) && !string.IsNullOrWhiteSpace(gc))
                        canGhi.Add((tho[i].id, gc));
            }
            using var tran = cn.BeginTransaction();
            if (!daCoCot)
                using (var cmd = new SqliteCommand($"ALTER TABLE [{tenBang}] ADD COLUMN GhiChu TEXT", cn, tran)) cmd.ExecuteNonQuery();
            if (canGhi.Count > 0)
            {
                using var upd = new SqliteCommand($"UPDATE [{tenBang}] SET GhiChu = @gc WHERE rowid = @id", cn, tran);
                var pGc = upd.Parameters.Add("@gc", SqliteType.Text);
                var pId = upd.Parameters.Add("@id", SqliteType.Integer);
                upd.Prepare();
                foreach (var (id, gc) in canGhi)
                {
                    pGc.Value = gc;
                    pId.Value = id;
                    upd.ExecuteNonQuery();
                }
            }
            tran.Commit();
        }
        private static bool CoCot(SqliteConnection cn, string tenBang, string tenCot)
        {
            using var cmd = new SqliteCommand("SELECT 1 FROM pragma_table_info(@b) WHERE name = @c COLLATE NOCASE LIMIT 1", cn);
            cmd.Parameters.AddWithValue("@b", tenBang);
            cmd.Parameters.AddWithValue("@c", tenCot);
            return cmd.ExecuteScalar() != null;
        }
        public static string LuuTruDuLieuKhenThuongToanDienNam()
        {
            // Cả 3 chế độ: KhenThuong_CBCS_NamX / KhenThuong_TanBinh_NamX / KhenThuong_HocVien_NamX
            var cheDo = Module_TaiKhoan.LayPhienBanPhanMem().LayCheDo();
            int nam = Module_HeThong.LayNamHeThong();
            string fileName = $"{Module_CheDoDoiTuong.TienToTepKhenThuong(cheDo)}{nam}.db";
            string thuMucLuu = Module_DanduongGPS.ThuMucLichSuThiDua;
            string targetPath = Path.Combine(thuMucLuu, fileName);
            if (!File.Exists(Module_DanduongGPS.DuongDanCSDL4))
                throw new FileNotFoundException("Không tìm thấy cơ sở dữ liệu nguồn khen thưởng (CSDL số 4).");
            Directory.CreateDirectory(thuMucLuu);
            if (File.Exists(targetPath))
            {
                System.Windows.Forms.DialogResult result = System.Windows.Forms.MessageBox.Show(
                    $"Dữ liệu khen thưởng tổng hợp năm {nam} đã tồn tại trong thư mục lưu trữ.\nBạn có muốn xóa phiên bản cũ và cập nhật lại bằng dữ liệu mới nhất không?",
                    "Xác nhận ghi đè dữ liệu", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Question, System.Windows.Forms.MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly);
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    try
                    {
                        SqliteConnection.ClearAllPools();
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        File.Delete(targetPath);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Không thể xóa tệp dữ liệu cũ. Chi tiết lỗi: {ex.Message}");
                    }
                }
                else return string.Empty;
            }
            try
            {
                // CSDL4 cũ có thể chưa có đủ bảng của chế độ này -> tạo (rỗng) trước khi chép
                Module_BangKhenThuong.DamBaoTrongTep(Module_DanduongGPS.DuongDanCSDL4, cheDo);
                string[] cacBangCanSaoLuu = { Module_CheDoDoiTuong.KhenThuongNguoi(cheDo), Module_CheDoDoiTuong.GiayKhen(cheDo), Module_CheDoDoiTuong.KhenThuongTapThe(cheDo) };
                SaoChepBangVaDuLieuChuanXac(Module_DanduongGPS.DuongDanCSDL4, targetPath, cacBangCanSaoLuu);
            }
            catch
            {
                SqliteConnection.ClearAllPools();
                try { if (File.Exists(targetPath)) File.Delete(targetPath); } catch { }
                throw;
            }
            return targetPath;
        }
        // XUẤT EXCEL VÀ CẬP NHẬT TÌNH TRẠNG LỊCH SỬ
        public static void XuatExcelLichSuCore(string targetExcelPath, bool laTanBinh, bool isDataMaxMode, List<ColumnExportMeta> exportCols, List<int> filteredIndexes, DataTable dtSource, List<HistoryCBCSDTO> cacheCBCS,List<HistoryTanBinhDTO> cacheTanBinh, string tenTieuDoan)
        {
            int rowCount = filteredIndexes.Count;
            int colCount = exportCols.Count + 1;
            // Bộ đọc mỗi cột tạo 1 lần (trước đây so sánh chuỗi tên cột qua chuỗi if/else cho TỪNG Ô)
            var layCot = exportCols.Select(c => TaoBoLayCotLichSu(c.Name, laTanBinh, isDataMaxMode, dtSource, cacheCBCS, cacheTanBinh)).ToArray();
            var dongDuLieu = new List<object?[]>(rowCount);
            for (int r = 0; r < rowCount; r++)
            {
                int actualIndex = filteredIndexes[r];
                var d = new object?[colCount];
                d[0] = r + 1;
                for (int c = 0; c < layCot.Length; c++) d[c + 1] = layCot[c](actualIndex);
                dongDuLieu.Add(d);
            }
            using (var wb = new ClosedXML.Excel.XLWorkbook())
            {
                var ws = wb.Worksheets.Add("ThongKeThiDua");
                ws.Cell("A1").Value = "DANH SÁCH";
                ws.Range(1, 1, 1, colCount).Merge().Style.Font.SetBold().Font.SetFontSize(16).Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center).Alignment.SetVertical(ClosedXML.Excel.XLAlignmentVerticalValues.Center);
                ws.Cell("A2").Value = laTanBinh ? $"THỐNG KÊ PHÂN LOẠI THI ĐUA CỦA {Module_TaiKhoan.TenTB_InHoa} {tenTieuDoan}" : $"THỐNG KÊ PHÂN LOẠI THI ĐUA CỦA CBCS {tenTieuDoan}";
                ws.Range(2, 1, 2, colCount).Merge().Style.Font.SetBold().Font.SetFontSize(12).Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center).Alignment.SetVertical(ClosedXML.Excel.XLAlignmentVerticalValues.Center);
                int excelStartRow = 4;
                var cellStt = ws.Cell(excelStartRow, 1);
                cellStt.Value = "STT";
                cellStt.Style.Font.Bold = true;
                cellStt.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                cellStt.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                cellStt.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                ws.Column(1).Width = 5;
                var rangeColA = ws.Range(excelStartRow, 1, rowCount + excelStartRow, 1);
                rangeColA.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                rangeColA.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                int excelCol = 2;
                foreach (var col in exportCols)
                {
                    var cell = ws.Cell(excelStartRow, excelCol);
                    cell.Value = string.IsNullOrWhiteSpace(col.HeaderText) ? col.Name.Replace("_", " ") : col.HeaderText;
                    if (col.HeaderText.Contains("Họ và tên")) ws.Column(excelCol).Width = 30;
                    else if (excelCol == 5) ws.Column(excelCol).Width = 20;
                    else ws.Column(excelCol).Width = 15;
                    cell.Style.Font.Bold = true; cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center; cell.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center; cell.Style.Alignment.WrapText = true; cell.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                    if (laTanBinh && (Module_LichXet.DotCuaCot(Module_CheDoDoiTuong.Hien, col.Name)?.So ?? 0) % 2 == 1) cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(220, 235, 255);
                    else if (col.Name.StartsWith("TS_Loai")) cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(230, 250, 230);
                    else cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                    excelCol++;
                }
                Module_ExcelAnToan.NapDong(ws, excelStartRow + 1, 1, dongDuLieu);   // nạp 1 lần thay vì gán từng ô
                if (rowCount > 0)
                {
                    var dataRange = ws.Range(excelStartRow + 1, 1, excelStartRow + rowCount, colCount);
                    dataRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin; dataRange.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin; dataRange.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center; dataRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                    ws.Range(excelStartRow + 1, 1, excelStartRow + rowCount, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;
                    int hoTenColIndex = exportCols.FindIndex(c => c.Name.Equals("HoVaTen", StringComparison.OrdinalIgnoreCase));
                    if (hoTenColIndex >= 0) ws.Range(excelStartRow + 1, hoTenColIndex + 2, excelStartRow + rowCount, hoTenColIndex + 2).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;
                }
                int tongDong = rowCount + excelStartRow + 1;
                var totalCell = ws.Cell(tongDong, 1); totalCell.Value = $"Tổng cộng: {rowCount} {Module_HeThong.Tu_dong_chi} ./."; totalCell.Style.Font.SetBold().Font.SetItalic(); ws.Range(tongDong, 1, tongDong, colCount).Merge();
                Module_BanQuyen.DongDauExcel(wb);
                Module_ExcelAnToan.LuuAnToan(wb, targetExcelPath);
            }
        }
        /// <summary>
        /// Tạo 1 hàm đọc giá trị cho 1 cột (theo chỉ số dòng gốc). Quyết định "cột nào lấy trường nào"
        /// chỉ chạy 1 lần cho mỗi cột thay vì lặp lại cho từng ô.
        /// </summary>
        private static Func<int, object?> TaoBoLayCotLichSu(string ten, bool laTanBinh, bool isDataMaxMode, DataTable dtSource,
            List<HistoryCBCSDTO> cacheCBCS, List<HistoryTanBinhDTO> cacheTanBinh)
        {
            if (!isDataMaxMode)
            {
                if (dtSource == null || !dtSource.Columns.Contains(ten)) return _ => "";
                var dv = dtSource.DefaultView;
                return i => dv[i][ten]?.ToString() ?? "";
            }
            if (laTanBinh)
            {
                Func<HistoryTanBinhDTO, string?> f = ten switch
                {
                    "HoVaTen" => x => x.HoVaTen,
                    "SoHieu" => x => x.SoHieu,
                    "DonVi" => x => x.DonVi,
                    "TinhTrang" => x => x.TinhTrang,
                    "GhiChu" => x => x.GhiChu,
                    "TS_Loai1" => x => x.TS_Loai1,
                    "TS_Loai2" => x => x.TS_Loai2,
                    "TS_Loai3" => x => x.TS_Loai3,
                    "TS_Loai4" => x => x.TS_Loai4,
                    _ => x => x.DuLieuThang.TryGetValue(ten, out var v) ? v : (x.CotPhatSinh.TryGetValue(ten, out var p) ? p : "")
                };
                return i => f(cacheTanBinh[i]) ?? "";
            }
            Func<HistoryCBCSDTO, string?> g;
            switch (ten)
            {
                case "HoVaTen": g = x => x.HoVaTen; break;
                case "SoHieu": g = x => x.SoHieu; break;
                case "DonVi": g = x => x.DonVi; break;
                case "TinhTrang": g = x => x.TinhTrang; break;
                case "GhiChu": g = x => x.GhiChu; break;
                case "KQ_ThiDua_Nam_Cu": g = x => x.KQ_TD; break;
                case "KQ_XepLoaiCB_Nam_Cu": g = x => x.KQ_XL_CB; break;
                case "KQ_XepLoaiDangVien_Nam_Cu": g = x => x.KQ_XL_DV; break;
                case "Thang_12_Nam_Cu": g = x => x.T12; break;
                case "Sau_Thang_Dau_Nam": g = x => x.Sau_Thang_Dau_Nam; break;
                case "TongKet_Nam": g = x => x.TongKet_Nam; break;
                case "TS_Loai1": g = x => x.TS_Loai1; break;
                case "TS_Loai2": g = x => x.TS_Loai2; break;
                case "TS_Loai3": g = x => x.TS_Loai3; break;
                case "TS_Loai4": g = x => x.TS_Loai4; break;
                default:
                    if (ten.StartsWith("Thang_") && int.TryParse(ten.Substring(6), out int t))
                        g = x => t >= 1 && t <= x.Thang.Length ? x.Thang[t - 1] : "";   // chặn tràn chỉ số (vd Thang_12)
                    else
                        g = x => x.CotPhatSinh.TryGetValue(ten, out var p) ? p : "";
                    break;
            }
            return i => g(cacheCBCS[i]) ?? "";
        }
        public static void CapNhatTinhTrangThiDuaNamCu(string pathCsdl2, string pathCsdlNamCu, bool laTanBinh)
        {
            if (string.IsNullOrEmpty(pathCsdlNamCu) || !File.Exists(pathCsdlNamCu)) return;
            if (!File.Exists(pathCsdl2)) return;
            string tableDich = Module_CheDoDoiTuong.ThiDuaThang(Module_CheDoDoiTuong.TuTenTep(pathCsdlNamCu));   // bảng theo chính tệp năm cũ
            try
            {
                var hashSoHieuCsdl2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var cn2 = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(pathCsdl2, chiDoc: true)))
                {
                    cn2.Open();
                    using var cmd = new SqliteCommand($"SELECT SoHieu FROM {Module_TaiKhoan.BangDanhSach}", cn2);
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        string encSh = rd["SoHieu"]?.ToString() ?? "";
                        string? plainSh = SafeDecrypt(encSh)?.Trim();
                        if (!string.IsNullOrEmpty(plainSh))
                        {
                            hashSoHieuCsdl2.Add(plainSh);
                        }
                    }
                }
                var idDangCongTac = new List<int>();
                var idChuyenCongTac = new List<int>();
                using (var cnNamCu = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(pathCsdlNamCu, chiDoc: true)))
                {
                    cnNamCu.Open();
                    using var cmd = new SqliteCommand($"SELECT ID, SoHieu, TinhTrang FROM [{tableDich}]", cnNamCu);
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        int id = Convert.ToInt32(rd["ID"]);
                        string encSh = rd["SoHieu"]?.ToString() ?? "";
                        string? plainSh = SafeDecrypt(encSh)?.Trim();
                        string ttHienTai = rd["TinhTrang"]?.ToString()?.Trim() ?? "";
                        bool tonTaiInCsdl2 = !string.IsNullOrEmpty(plainSh) && hashSoHieuCsdl2.Contains(plainSh);
                        if (tonTaiInCsdl2)
                        {
                            if (ttHienTai != Module_HeThong.TT_DANG_CONG_TAC) idDangCongTac.Add(id);
                        }
                        else
                        {
                            if (ttHienTai != Module_HeThong.TT_CHUYEN_CONG_TAC) idChuyenCongTac.Add(id);
                        }
                    }
                }
                if (idDangCongTac.Count == 0 && idChuyenCongTac.Count == 0) return;
                using (var cnNamCu = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(pathCsdlNamCu)))
                {
                    cnNamCu.Open();
                    using var tran = cnNamCu.BeginTransaction();
                    try
                    {
                        if (idDangCongTac.Count > 0)
                        {
                            using var cmdUpd = new SqliteCommand($"UPDATE [{tableDich}] SET TinhTrang = 'Đang công tác' WHERE ID = @id", cnNamCu, tran);
                            cmdUpd.Parameters.Add("@id", SqliteType.Integer);
                            foreach (int id in idDangCongTac)
                            {
                                cmdUpd.Parameters["@id"].Value = id;
                                cmdUpd.ExecuteNonQuery();
                            }
                        }
                        if (idChuyenCongTac.Count > 0)
                        {
                            using var cmdUpd = new SqliteCommand($"UPDATE [{tableDich}] SET TinhTrang = 'Chuyển công tác' WHERE ID = @id", cnNamCu, tran);
                            cmdUpd.Parameters.Add("@id", SqliteType.Integer);
                            foreach (int id in idChuyenCongTac)
                            {
                                cmdUpd.Parameters["@id"].Value = id;
                                cmdUpd.ExecuteNonQuery();
                            }
                        }
                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi cập nhật TinhTrang CSDL năm cũ: " + ex.Message);
            }
        }
        public static void CapNhatTinhTrangLichSuTuDanhSachGoc(string pathCsdl2, string pathCsdlNamCu, bool laTanBinh)
        {
            if (string.IsNullOrEmpty(pathCsdlNamCu) || !File.Exists(pathCsdlNamCu)) return;
            if (string.IsNullOrEmpty(pathCsdl2) || !File.Exists(pathCsdl2)) return;
            string tableDich = Module_CheDoDoiTuong.ThiDuaThang(Module_CheDoDoiTuong.TuTenTep(pathCsdlNamCu));   // bảng theo chính tệp năm cũ
            try
            {
                var hashSoHieuCsdl2 = new HashSet<string>(10000, StringComparer.OrdinalIgnoreCase);
                using (var cn2 = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(pathCsdl2, chiDoc: true, cacheChung: true)))
                {
                    cn2.Open();
                    using var cmd = new SqliteCommand($"SELECT SoHieu FROM {Module_TaiKhoan.BangDanhSach} WHERE SoHieu IS NOT NULL AND SoHieu <> ''", cn2);
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        string rawSh = rd.GetString(0);
                        string plainSh = SafeDecrypt(rawSh);
                        if (!string.IsNullOrWhiteSpace(plainSh))
                        {
                            hashSoHieuCsdl2.Add(plainSh.Trim());
                        }
                    }
                }
                if (hashSoHieuCsdl2.Count == 0) return;
                using (var cnCu = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(pathCsdlNamCu)))
                {
                    cnCu.Open();
                    using (var cmdPragma = new SqliteCommand("PRAGMA synchronous = NORMAL; PRAGMA journal_mode = WAL;", cnCu))
                    {
                        cmdPragma.ExecuteNonQuery();
                    }
                    using (var cmdCheck = new SqliteCommand("SELECT 1 FROM sqlite_master WHERE type='table' AND name=@tableName LIMIT 1;", cnCu))
                    {
                        cmdCheck.Parameters.AddWithValue("@tableName", tableDich);
                        if (cmdCheck.ExecuteScalar() == null) return;
                    }
                    var updateQueue = new List<(int id, string ttMoi)>(10000);
                    using (var cmdSelect = new SqliteCommand($"SELECT ID, SoHieu, TinhTrang FROM [{tableDich}]", cnCu))
                    using (var rd = cmdSelect.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            int id = rd.GetInt32(0);
                            string rawSh = rd.IsDBNull(1) ? "" : rd.GetString(1);
                            string ttCu = rd.IsDBNull(2) ? "" : rd.GetString(2).Trim();
                            string plainSh = SafeDecrypt(rawSh).Trim();
                            string ttMoi = (!string.IsNullOrEmpty(plainSh) && hashSoHieuCsdl2.Contains(plainSh))
                                ? Module_HeThong.TT_DANG_CONG_TAC
                                : Module_HeThong.TT_CHUYEN_CONG_TAC;
                            if (!string.Equals(ttCu, ttMoi, StringComparison.OrdinalIgnoreCase))
                            {
                                updateQueue.Add((id, ttMoi));
                            }
                        }
                    }
                    if (updateQueue.Count > 0)
                    {
                        using var tran = cnCu.BeginTransaction();
                        try
                        {
                            using var cmdUpd = new SqliteCommand($"UPDATE [{tableDich}] SET TinhTrang = @tt WHERE ID = @id", cnCu, tran);
                            var pTt = cmdUpd.Parameters.Add("@tt", SqliteType.Text);
                            var pId = cmdUpd.Parameters.Add("@id", SqliteType.Integer);
                            cmdUpd.Prepare();
                            foreach (var item in updateQueue)
                            {
                                pTt.Value = item.ttMoi;
                                pId.Value = item.id;
                                cmdUpd.ExecuteNonQuery();
                            }
                            tran.Commit();
                        }
                        catch
                        {
                            tran.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi Cốt lõi Đồng Bộ Tình Trạng Lịch Sử: {ex.Message}");
            }
        }
        /// </summary>
        public static bool LaPhienBanCBCS()
        {
            try
            {
                string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(phienBan)) return false;
                // Nếu KHÔNG chứa từ "tân binh" -> Là phiên bản CBCS
                return !phienBan.LaCheDoTanBinh();
            }
            catch
            {
                return false;
            }
        }
        /// Tự động ẩn/hiện ToolStripMenuItem tùy theo phiên bản (Chỉ hiện ở CBCS, ẩn ở Tân Binh)
        public static void CapNhatAnHienMenuDongBo(ToolStripItem menuItem)
        {
            if (menuItem == null) return;
            // Dùng Available thay vì Visible để WinForms ẩn tuyệt đối trên ToolStrip/ContextMenu
            menuItem.Available = LaPhienBanCBCS();
        }
    }
}