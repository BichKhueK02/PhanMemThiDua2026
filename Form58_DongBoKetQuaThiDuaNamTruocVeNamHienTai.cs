///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Text.RegularExpressions;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form58_DongBoKetQuaThiDuaNamTruocVeNamHienTai : Form
    {
        private bool _dangDongBo;   // chỉ đụng tới trên luồng UI nên bool là đủ (bản cũ dùng SemaphoreSlim nhưng không bao giờ Dispose)
        private static readonly Regex RegexNam = new Regex(@"ThiDua_CBCS_Nam(\d{4})\.db$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        // Kết quả hợp lệ: tra không phân biệt hoa/thường nhưng LUÔN ghi đúng giá trị chuẩn của hệ thống
        // (bản cũ ghi ToUpper() của chuỗi người dùng: phụ thuộc culture máy và có thể lệch hằng PL_* các form khác đang so sánh)
        private static readonly Dictionary<string, string> KetQuaChuan = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [Module_HeThong.PL_CSTD] = Module_HeThong.PL_CSTD,
            [Module_HeThong.PL_CSTT] = Module_HeThong.PL_CSTT,
            [Module_HeThong.PL_HTNV] = Module_HeThong.PL_HTNV,
            [Module_HeThong.PL_KHTNV] = Module_HeThong.PL_KHTNV
        };
        public Form58_DongBoKetQuaThiDuaNamTruocVeNamHienTai()
        {
            InitializeComponent();
            Module_BieuTuongNut.ApDung(this);   // biểu tượng nút: Hiện đại = tự vẽ, Cổ điển = ảnh gốc nhúng trong exe
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            InitToolTips();
        }
        private void Form58_DongBoKetQuaThiDuaNamTruocVeNamHienTai_Load(object? sender, EventArgs e)
        {
            LoadDanhSachCSDLLichSu();
        }
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            toolTip1.SetToolTip(kryptonButton_DongBoDuLieuNamCuVaoCSDLHienTai, "Đồng bộ dữ liệu từ CSDL năm trước vào CSDL hiện tại");
            toolTip1.SetToolTip(kryptonButton_HuongDanSuDung, "Đọc hướng dẫn sử dụng");
        }
        // ===================== DANH SÁCH CSDL LỊCH SỬ =====================
        private void LoadDanhSachCSDLLichSu()
        {
            try
            {
                string folderPath = Module_DanduongGPS.ThuMucLichSuThiDua;
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
                var files = Directory.EnumerateFiles(folderPath, "ThiDua_CBCS_Nam*.db")
                    .Select(p => (Match: RegexNam.Match(Path.GetFileName(p)), FullPath: p))
                    .Where(x => x.Match.Success)
                    .Select(x => new { HienThiNam = x.Match.Groups[1].Value, x.FullPath })
                    .OrderByDescending(x => x.HienThiNam, StringComparer.Ordinal)   // năm mới nhất lên đầu và được chọn sẵn
                    .ToList();
                // Đặt DisplayMember/ValueMember TRƯỚC DataSource: tránh combobox ràng buộc 2 lần và nhảy SelectedValue không cần thiết
                comboBox_ChonCSDLNamCu.DisplayMember = "HienThiNam";
                comboBox_ChonCSDLNamCu.ValueMember = "FullPath";
                comboBox_ChonCSDLNamCu.DataSource = files;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách CSDL lịch sử: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // ===================== TIỆN ÍCH CSDL =====================
        // Pooling=false: đóng kết nối là nhả hẳn tệp .db (không làm kẹt sao lưu/khôi phục). Dùng builder nên đường dẫn có ký tự đặc biệt (;) vẫn an toàn.
        // Không còn PRAGMA journal_mode / busy_timeout: Microsoft.Data.Sqlite đã đặt busy_timeout = 30 giây,
        // còn bản cũ ghi đè xuống 10 giây và tự ý đổi chế độ journal của CSDL hiện tại.
        private static SqliteConnection MoKetNoi(string path, bool chiDoc)
            => Module_KetNoiCSDL.MoKetNoi(path, chiDoc: chiDoc, khongTaoMoi: !chiDoc, pooling: false);
        // Báo lỗi dễ hiểu khi CSDL thiếu cột (thay cho thông báo SQLite khó đọc "no such column")
        private static void KiemTraCot(SqliteConnection cn, string tenBang, string tenCSDL, params string[] cacCot)
        {
            var coTrongBang = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = $"PRAGMA table_info([{tenBang}])";
                using var rd = cmd.ExecuteReader();
                while (rd.Read()) coTrongBang.Add(rd.GetString(1));
            }
            if (coTrongBang.Count == 0)
                throw new InvalidOperationException($"{tenCSDL} không có bảng {tenBang}.");
            foreach (string cot in cacCot)
                if (!coTrongBang.Contains(cot))
                    throw new InvalidOperationException($"{tenCSDL} không có cột {cot} trong bảng {tenBang}.");
        }
        // Số hiệu được MÃ HÓA bằng AES với IV ngẫu nhiên: cùng 1 số hiệu ở hai CSDL cho ra hai chuỗi khác nhau,
        // nên chỉ so khớp được sau khi GIẢI MÃ. GiaiMa không ném lỗi, trả rỗng khi giải mã hỏng và có cache nội bộ.
        private static string GiaiMaSoHieu(string? cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText)) return string.Empty;
            return Module_BaoMatAES.GiaiMa(cipherText).Trim();
        }
        // Trả null khi rỗng / không thuộc 4 mức hợp lệ
        private static string? ChuanHoaKetQua(string? giaTri)
            => !string.IsNullOrWhiteSpace(giaTri) && KetQuaChuan.TryGetValue(giaTri.Trim(), out string? chuan) ? chuan : null;
        // ===================== ĐỒNG BỘ (chạy ở luồng nền) =====================
        private static KetQuaDongBo DongBo(string dbNguonPath, string dbDichPath)
        {
            var kq = new KetQuaDongBo();
            // 1) CSDL năm cũ (chỉ đọc): Số hiệu giải mã -> kết quả tổng kết đã chuẩn hóa (null = trống/không hợp lệ)
            var nguon = new Dictionary<string, string?>(StringComparer.Ordinal);
            using (var cn = MoKetNoi(dbNguonPath, true))
            {
                KiemTraCot(cn, "ThiDuaThang", "CSDL năm cũ", "SoHieu", "TongKet_Nam");
                using var cmd = cn.CreateCommand();
                cmd.CommandText = "SELECT SoHieu, TongKet_Nam FROM ThiDuaThang WHERE SoHieu IS NOT NULL AND TRIM(SoHieu) <> ''";
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    kq.TongSoNguoi++;
                    string soHieu = GiaiMaSoHieu(rd.GetString(0));
                    if (soHieu.Length == 0) { kq.SoGiaiMaLoi++; continue; }
                    nguon[soHieu] = ChuanHoaKetQua(rd.IsDBNull(1) ? null : rd.GetString(1));   // trùng số hiệu: dòng đọc sau cùng thắng
                }
            }
            if (nguon.Count == 0) return kq;
            // 2) CSDL hiện tại: đọc 1 lần (rowid, Số hiệu, kết quả hiện có), tính trước những dòng THẬT SỰ cần đổi
            var canCapNhat = new List<(long RowId, string KetQua)>();
            var daKhop = new HashSet<string>(StringComparer.Ordinal);
            var coDoi = new HashSet<string>(StringComparer.Ordinal);
            using (var cn = MoKetNoi(dbDichPath, false))
            {
                KiemTraCot(cn, "ThiDuaThang", "CSDL hiện tại", "SoHieu", "KQ_ThiDua_Nam_Cu");
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = "SELECT rowid, SoHieu, KQ_ThiDua_Nam_Cu FROM ThiDuaThang WHERE SoHieu IS NOT NULL AND TRIM(SoHieu) <> ''";
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        kq.TongDongDich++;
                        string soHieu = GiaiMaSoHieu(rd.GetString(1));
                        if (soHieu.Length == 0 || !nguon.TryGetValue(soHieu, out string? ketQuaMoi)) continue;
                        daKhop.Add(soHieu);
                        if (ketQuaMoi == null) continue;   // kết quả năm cũ trống/không hợp lệ -> BỎ QUA, không ghi đè dữ liệu đang có (đúng như hướng dẫn)
                        string hienCo = rd.IsDBNull(2) ? string.Empty : rd.GetString(2).Trim();
                        if (string.Equals(hienCo, ketQuaMoi, StringComparison.Ordinal)) continue;
                        canCapNhat.Add((rd.GetInt64(0), ketQuaMoi));
                        coDoi.Add(soHieu);
                    }
                }
                // 3) Ghi: 1 transaction, 1 lệnh dùng lại, cập nhật theo rowid (không quét bảng theo Số hiệu cho từng người như bản cũ)
                if (canCapNhat.Count > 0)
                {
                    using var tx = cn.BeginTransaction();
                    using var upd = cn.CreateCommand();
                    upd.Transaction = tx;
                    upd.CommandText = "UPDATE ThiDuaThang SET KQ_ThiDua_Nam_Cu = @kq WHERE rowid = @id";
                    var pKq = upd.Parameters.Add("@kq", SqliteType.Text);
                    var pId = upd.Parameters.Add("@id", SqliteType.Integer);
                    foreach (var (rowId, ketQua) in canCapNhat)
                    {
                        pKq.Value = ketQua;
                        pId.Value = rowId;
                        upd.ExecuteNonQuery();
                    }
                    tx.Commit();   // lỗi giữa chừng => rollback toàn bộ, CSDL hiện tại không bị cập nhật dở dang
                }
            }
            // 4) Thống kê theo từng người của năm cũ
            kq.SoDongCapNhat = canCapNhat.Count;
            foreach (var cap in nguon)
            {
                if (!daKhop.Contains(cap.Key)) kq.SoKhongKhop++;
                else if (cap.Value == null) kq.SoBoQuaKetQuaTrong++;
                else if (!coDoi.Contains(cap.Key)) kq.SoGiuNguyen++;
            }
            return kq;
        }
        // ===================== HƯỚNG DẪN =====================
        private void kryptonButton_HuongDanSuDung_Click(object? sender, EventArgs e)
        {
            // Hộp thoại modal nên không cần đổi chữ/khóa nút (bản cũ đổi chữ nhưng không vẽ lại nên người dùng không bao giờ thấy)
            const string huongDan = """
                HƯỚNG DẪN ĐỒNG BỘ KẾT QUẢ THI ĐUA NĂM CŨ
                1. MỤC ĐÍCH & PHẠM VI
                - Đưa kết quả tổng kết năm cũ vào cột thống kê năm cũ trong trang dữ liệu năm nay.
                - Chỉ áp dụng cho CBCS.
                2. CƠ CHẾ ĐỐI CHIẾU:
                - Chọn CSDL thi đua của năm cũ.
                - Đối chiếu người giữa hai năm bằng Số hiệu.
                3. AN TOÀN DỮ LIỆU
                - CSDL năm cũ chỉ được đọc, không bị thay đổi.
                - Chỉ cập nhật cột KQ_ThiDua_Nam_Cu trong CSDL năm nay.
                - Người không tồn tại hoặc kết quả không hợp lệ sẽ được bỏ qua.
                4. KẾT QUẢ: Thông báo số bản ghi được cập nhật và giữ nguyên.
                Lưu ý: Vui lòng chọn đúng CSDL của năm trước trước khi thực hiện.
                """;
            MessageBox.Show(huongDan, "Hướng dẫn sử dụng", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        // ===================== NÚT ĐỒNG BỘ =====================
        private async void kryptonButton_DongBoDuLieuNamCuVaoCSDLHienTai_Click(object? sender, EventArgs e)
        {
            if (_dangDongBo)
            {
                MessageBox.Show("Thao tác đồng bộ đang được thực hiện, vui lòng chờ...", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (comboBox_ChonCSDLNamCu.SelectedValue is not string dbNguonPath || string.IsNullOrWhiteSpace(dbNguonPath))
            {
                MessageBox.Show("Vui lòng chọn CSDL năm cũ cần nạp dữ liệu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string dbDichPath = Module_DanduongGPS.DuongDanCSDL4;
            if (!File.Exists(dbNguonPath) || !File.Exists(dbDichPath))
            {
                MessageBox.Show("File CSDL nguồn hoặc CSDL đích không tồn tại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // Chặn chọn nhầm chính CSDL hiện tại làm nguồn (đọc và ghi cùng một tệp)
            if (string.Equals(Path.GetFullPath(dbNguonPath), Path.GetFullPath(dbDichPath), StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("CSDL năm cũ không được trùng với CSDL hiện tại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            using (var frmXacMinh = new Form24_XacMinhAdmin())
            {
                frmXacMinh.TopMost = true;
                frmXacMinh.StartPosition = FormStartPosition.CenterScreen;
                if (frmXacMinh.ShowDialog() != DialogResult.OK) return;
            }
            _dangDongBo = true;
            var nut = sender as Control;
            if (nut != null) nut.Enabled = false;
            UseWaitCursor = true;
            try
            {
                KetQuaDongBo kq = await Task.Run(() => DongBo(dbNguonPath, dbDichPath));
                if (IsDisposed) return;
                string ghiChu = $"Tổng số CBCS kiểm tra: {kq.TongSoNguoi}\n" +
                                $"Cập nhật thành công: {kq.SoDongCapNhat}\n" +
                                $"Giữ nguyên (đã đúng sẵn): {kq.SoGiuNguyen}\n" +
                                $"Không có trong CSDL hiện tại: {kq.SoKhongKhop}\n" +
                                $"Bỏ qua (kết quả năm cũ trống/không hợp lệ): {kq.SoBoQuaKetQuaTrong}";
                if (kq.SoGiaiMaLoi > 0)
                    ghiChu += $"\nKhông thể giải mã: {kq.SoGiaiMaLoi} bản ghi.";
                Module_NhatKy.GhiNhatKy(
                    taiKhoan: Module_TaiKhoan.TenTaiKhoan_RAM,
                    hanhDong: "Đồng bộ kết quả thi đua năm cũ",
                    ghiChu: ghiChu);
                if (kq.TongSoNguoi == 0)
                    MessageBox.Show("CSDL năm cũ chưa có dữ liệu thi đua nào để đồng bộ.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else if (kq.TongDongDich == 0 && kq.SoGiaiMaLoi < kq.TongSoNguoi)   // (nếu mọi Số hiệu năm cũ đều không giải mã được thì hiện bảng kết quả bình thường)
                    MessageBox.Show("CSDL hiện tại chưa có dữ liệu thi đua (ThiDuaThang đang trống) nên không có gì để cập nhật.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show($"Đồng bộ hoàn tất!\n\n{ghiChu}", "Kết quả", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Làm mới trang thống kê (nếu đang mở) trong khối riêng: lỗi ở đây không được báo thành "lỗi đồng bộ" khi dữ liệu đã ghi xong
                if (kq.SoDongCapNhat > 0)
                {
                    try
                    {
                        var frm15 = Application.OpenForms.OfType<Form15_ThongKeThiDua>().FirstOrDefault();
                        if (frm15 != null && !frm15.IsDisposed)
                            await frm15.LamMoiDuLieuAsync();
                    }
                    catch (Exception ex) { Debug.WriteLine("[Form58] Làm mới Form15: " + ex.Message); }
                }
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                    MessageBox.Show($"Lỗi trong quá trình đồng bộ: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _dangDongBo = false;
                if (!IsDisposed)
                {
                    UseWaitCursor = false;
                    if (nut != null) nut.Enabled = true;
                }
            }
        }
        private sealed class KetQuaDongBo
        {
            public int TongSoNguoi { get; set; }          // dòng có Số hiệu ở CSDL năm cũ
            public int TongDongDich { get; set; }         // dòng có Số hiệu ở CSDL hiện tại
            public int SoDongCapNhat { get; set; }        // dòng ở CSDL hiện tại đã được ghi kết quả mới
            public int SoGiuNguyen { get; set; }          // người khớp, kết quả đã đúng sẵn
            public int SoKhongKhop { get; set; }          // người năm cũ không có trong CSDL hiện tại
            public int SoBoQuaKetQuaTrong { get; set; }   // người khớp nhưng kết quả năm cũ trống / không hợp lệ -> không ghi đè
            public int SoGiaiMaLoi { get; set; }
        }
    }
}
