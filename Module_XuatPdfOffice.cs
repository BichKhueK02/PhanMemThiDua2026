using ClosedXML.Excel;
using Microsoft.CSharp.RuntimeBinder;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  ĐỘNG CƠ XUẤT PDF TỪ EXCEL — TÁCH KHỎI GIAO DIỆN (Form48 chỉ gọi, không chứa logic xuất)
    //
    //  Thứ tự động cơ:  Microsoft Excel  →  WPS Spreadsheets  →  LibreOffice (tự mang theo hoặc cài sẵn)
    //  - Mỗi động cơ chỉ xuất những sheet CHƯA xong; sheet đã xong ở động cơ trước không làm lại.
    //  - Lỗi "của người dùng" (đường dẫn quá dài, tệp PDF đang mở, không có quyền ghi...) dừng ngay,
    //    KHÔNG chuyển động cơ (bản cũ chuyển sang LibreOffice rồi ghi đè âm thầm).
    //  - Không bao giờ đụng vào tệp gốc: mọi động cơ làm việc trên BẢN SAO TẠM (đọc được cả khi tệp đang mở trong Excel/WPS).
    //  - COM chạy trên luồng STA riêng (chuẩn của Office); giám sát thời gian bằng đồng hồ canh, chỉ tiêu diệt tiến trình
    //    MÀ CHÍNH PHẦN MỀM KHỞI TẠO (không bao giờ kill Excel/WPS đang mở của người dùng).
    //  - Mọi PDF xuất ra thư mục tạm rồi mới chuyển vào đích: không để lại tệp cụt/rỗng ở thư mục đích.
    // =====================================================================================
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    internal readonly record struct MucXuatPdf(string TenSheet, string TenPdf);
    internal readonly record struct ThongTinThayThe(string DiaDiemThangNam, string TextKemTheo, int LenDau, int LenGiua);
    internal sealed class KetQuaXuatPdf
    {
        public List<string> DaTao { get; } = new();
        public List<(string Sheet, string Loi)> ThatBai { get; } = new();
        public string? FileCuoi { get; set; }
        public string TenDongCo { get; set; } = "";
    }
    /// <summary>Lỗi do điều kiện của người dùng (cần họ xử lý): dừng hẳn, không thử động cơ khác.</summary>
    internal sealed class LoiNguoiDungException : Exception
    {
        public LoiNguoiDungException(string message) : base(message) { }
    }
    /// <summary>Động cơ này không dùng được (chưa cài, treo, từ chối PDF...): thử động cơ kế tiếp.</summary>
    internal sealed class LoiDongCoException : Exception
    {
        public LoiDongCoException(string message) : base(message) { }
    }
    internal static class Module_XuatPdfOffice
    {
        private const int GioiHanDuongDan = 250;                 // an toàn dưới MAX_PATH = 260
        private const int ThoiGianToiDaMoiSheetCom = 90_000;     // ms
        private const int ThoiGianToiDaMoiSheetLibre = 120_000;  // ms (máy yếu / lần chạy đầu tạo hồ sơ)
        private static readonly string[] ProgIdCom = { "Excel.Application", "Ket.Application", "ET.Application" };
        private const string TieuDeTep = "Ban quyen thuoc PhanMemThiDua2026";
        private const string TacGiaTep = "TrungKien_0975287973";
        private const string GhiChuTep = "Tep tin duoc xuat tu he thong";
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        // =====================================================================================
        //  1) TIỆN ÍCH CÔNG KHAI
        // =====================================================================================
        /// <summary>Tên tệp PDF hợp lệ trên Windows (bỏ ký tự cấm, thêm .pdf, bỏ khoảng trắng/dấu chấm cuối).</summary>
        public static string TenTepHopLe(string ten)
        {
            string s = ten ?? "";
            foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c.ToString(), "");
            s = s.Trim();
            if (!s.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) s += ".pdf";
            return s;
        }
        /// <summary>Liệt kê tên sheet (bỏ THONG_TIN). Đọc bằng luồng chia sẻ nên tệp đang mở trong Excel/WPS vẫn đọc được. Chạy ở luồng nền.</summary>
        public static List<string> LietKeTenSheet(string excelPath)
        {
            try
            {
                using var fs = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var wb = new XLWorkbook(fs);
                var ds = new List<string>();
                foreach (var ws in wb.Worksheets)
                {
                    if (ws.Name.Equals("THONG_TIN", StringComparison.OrdinalIgnoreCase)) continue;
                    ds.Add(ws.Name);
                }
                if (ds.Count == 0) throw new InvalidDataException("Tệp Excel này không có sheet dữ liệu nào hợp lệ!");
                return ds;
            }
            catch (InvalidDataException) { throw; }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new IOException("Không đọc được tệp Excel (đang bị khóa hoặc không đủ quyền): " + ex.Message, ex);
            }
        }
        public static string? TimLibreOffice()
        {
            string portable = Path.Combine(AppContext.BaseDirectory, "LibreOffice", "program", "soffice.exe");
            if (File.Exists(portable)) return portable;
            foreach (var goc in new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            })
            {
                if (string.IsNullOrEmpty(goc)) continue;
                string p = Path.Combine(goc, "LibreOffice", "program", "soffice.exe");
                if (File.Exists(p)) return p;
            }
            return null;
        }
        // =====================================================================================
        //  2) ĐIỂM VÀO
        // =====================================================================================
        /// <param name="baoDaXong">Gọi (từ luồng nền) mỗi khi xong thêm 1 tệp; tham số = tổng số tệp đã xong.</param>
        public static async Task<KetQuaXuatPdf> XuatAsync(
            string excelGoc, string thuMucDich, IReadOnlyList<MucXuatPdf> danhSach,
            ThongTinThayThe tt, Action<int>? baoDaXong, CancellationToken ct = default)
        {
            var p = new PhienXuat { TT = tt, Bao = baoDaXong, Ct = ct };
            p.Muc.AddRange(KiemTraTruocKhiXuat(thuMucDich, danhSach));
            // Thư mục làm việc trong Database\NhatKyNhaPhatTrien\TepTam thay cho %TEMP% (= AppData\Local\Temp)
            string thuMucLamViec = Path.Combine(Module_NhatKyNhaPhatTrien.ThuMucTam, "PMTD_Xuat_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(thuMucLamViec);
                p.ThuMucTam = thuMucLamViec;
                p.BanSao = TaoBanSao(excelGoc, thuMucLamViec);
                var nhatKyDongCo = new List<string>();
                foreach (string progId in ProgIdCom)
                {
                    if (!p.ConLai.Any()) break;
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        await ChayStaAsync(() => XuatBangCom(progId, p)).ConfigureAwait(false);
                    }
                    catch (LoiNguoiDungException) { throw; }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        nhatKyDongCo.Add($"{TenDongCo(progId)}: {ex.Message}");
                        Debug.WriteLine($"[XuatPdf] Động cơ {progId} dừng: {ex.Message}");
                    }
                }
                if (p.ConLai.Any())
                {
                    string? libre = TimLibreOffice();
                    if (libre == null)
                    {
                        nhatKyDongCo.Add("LibreOffice: không tìm thấy.");
                    }
                    else
                    {
                        try
                        {
                            await Task.Run(() => XuatBangLibreOffice(libre, p), ct).ConfigureAwait(false);
                        }
                        catch (LoiNguoiDungException) { throw; }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex)
                        {
                            nhatKyDongCo.Add($"LibreOffice: {ex.Message}");
                            Debug.WriteLine("[XuatPdf] LibreOffice dừng: " + ex.Message);
                        }
                    }
                }
                // Không động cơ nào xuất được dù chỉ 1 tệp, và cũng không có lỗi theo từng sheet → báo rõ nguyên nhân
                if (p.DaTao.Count == 0 && p.Loi.Count == 0)
                {
                    throw new Exception(
                        "Không thể tạo tệp PDF bằng bất kỳ phần mềm văn phòng nào trên máy này (Microsoft Excel, WPS, LibreOffice).\n" +
                        string.Join("\n", nhatKyDongCo.Select(x => "• " + x)));
                }
                var kq = new KetQuaXuatPdf { FileCuoi = p.FileCuoi, TenDongCo = p.TenDongCoDung ?? "" };
                kq.DaTao.AddRange(p.DaTao);
                foreach (var m in p.Muc)
                {
                    if (p.Xong.Contains(m.Muc.TenSheet)) continue;
                    p.Loi.TryGetValue(m.Muc.TenSheet, out string? loi);
                    kq.ThatBai.Add((m.Muc.TenSheet, loi ?? "Không có động cơ nào xuất được sheet này."));
                }
                return kq;
            }
            finally
            {
                try { if (Directory.Exists(thuMucLamViec)) Directory.Delete(thuMucLamViec, true); }
                catch (Exception ex) { Debug.WriteLine("[XuatPdf] Không dọn được thư mục tạm: " + ex.Message); }
            }
        }
        // =====================================================================================
        //  3) KIỂM TRA TRƯỚC KHI XUẤT (gom TẤT CẢ vấn đề rồi báo 1 lần)
        // =====================================================================================
        private static List<(MucXuatPdf Muc, string Dich)> KiemTraTruocKhiXuat(string thuMucDich, IReadOnlyList<MucXuatPdf> ds)
        {
            if (ds.Count == 0) throw new LoiNguoiDungException("Vui lòng tích chọn ít nhất 1 sheet để xuất tệp *.pdf.");
            if (!Directory.Exists(thuMucDich)) throw new LoiNguoiDungException("Thư mục đích để lưu tệp *.pdf không tồn tại.");
            try
            {
                string probe = Path.Combine(thuMucDich, ".pmtd_" + Guid.NewGuid().ToString("N"));
                using (new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose)) { }
            }
            catch (UnauthorizedAccessException)
            {
                throw new LoiNguoiDungException("Hệ thống không có quyền ghi dữ liệu vào thư mục này (Bị từ chối truy cập).\n" +
                    "Vui lòng chọn thư mục khác (Ví dụ: Desktop, Documents) hoặc chạy phần mềm bằng quyền Quản trị viên.");
            }
            catch (Exception ex)
            {
                throw new LoiNguoiDungException("Thư mục đích đang bị lỗi hoặc không thể truy cập:\n" + ex.Message);
            }
            var ketQua = new List<(MucXuatPdf, string)>();
            var daDung = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tooLong = new List<string>();
            var trung = new List<string>();
            var biKhoa = new List<string>();
            foreach (var m in ds)
            {
                string ten = TenTepHopLe(m.TenPdf);
                string dich = Path.Combine(thuMucDich, ten);
                if (dich.Length >= GioiHanDuongDan) { tooLong.Add($"{ten} ({dich.Length} ký tự)"); continue; }
                if (!daDung.Add(dich)) { trung.Add(ten); continue; }
                if (File.Exists(dich))
                {
                    try { using var fs = new FileStream(dich, FileMode.Open, FileAccess.Write, FileShare.None); }
                    catch (IOException) { biKhoa.Add(ten); }
                    catch (UnauthorizedAccessException) { biKhoa.Add(ten + " (chỉ đọc / bị từ chối truy cập)"); }
                }
                ketQua.Add((m, dich));
            }
            var loi = new List<string>();
            if (tooLong.Count > 0) loi.Add("Đường dẫn quá dài — hãy chọn thư mục lưu ngắn hơn (VD: Desktop):\n  • " + string.Join("\n  • ", tooLong));
            if (trung.Count > 0) loi.Add("Có các sheet cùng ra một tên tệp (sẽ ghi đè lẫn nhau):\n  • " + string.Join("\n  • ", trung));
            if (biKhoa.Count > 0) loi.Add("Các tệp PDF sau đang được mở bằng phần mềm khác (Foxit, Chrome...) — hãy đóng rồi xuất lại:\n  • " + string.Join("\n  • ", biKhoa));
            if (loi.Count > 0) throw new LoiNguoiDungException(string.Join("\n\n", loi));
            return ketQua;
        }
        private static string TaoBanSao(string nguon, string thuMuc)
        {
            string dich = Path.Combine(thuMuc, "nguon" + Path.GetExtension(nguon));
            try
            {
                using var src = new FileStream(nguon, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var dst = new FileStream(dich, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                src.CopyTo(dst);
                return dich;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new LoiNguoiDungException("Không đọc được tệp Excel nguồn (đang bị khóa hoặc không đủ quyền):\n" + ex.Message);
            }
        }
        // =====================================================================================
        //  4) TRẠNG THÁI MỘT PHIÊN XUẤT (dùng chung giữa các động cơ)
        // =====================================================================================
        private sealed class PhienXuat
        {
            public string BanSao = "";
            public string ThuMucTam = "";
            public ThongTinThayThe TT;
            public Action<int>? Bao;
            public CancellationToken Ct;
            public readonly List<(MucXuatPdf Muc, string Dich)> Muc = new();
            public readonly HashSet<string> Xong = new(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, string> Loi = new(StringComparer.OrdinalIgnoreCase);
            public readonly List<string> DaTao = new();
            public string? FileCuoi;
            public string? TenDongCoDung;
            private readonly object _khoa = new();
            public IEnumerable<(MucXuatPdf Muc, string Dich)> ConLai
            {
                get { lock (_khoa) return Muc.Where(m => !Xong.Contains(m.Muc.TenSheet)).ToList(); }
            }
            public void DanhDauXong(MucXuatPdf m, string dich, string dongCo)
            {
                int tong;
                lock (_khoa)
                {
                    Xong.Add(m.TenSheet);
                    Loi.Remove(m.TenSheet);
                    DaTao.Add(dich);
                    FileCuoi = dich;
                    TenDongCoDung ??= dongCo;
                    tong = Xong.Count;
                }
                try { Bao?.Invoke(tong); } catch { /* giao diện đã đóng: bỏ qua */ }
            }
            public void GhiLoi(string sheet, string loi) { lock (_khoa) Loi[sheet] = loi; }
        }
        private static Task ChayStaAsync(Action viec)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var t = new Thread(() =>
            {
                try { viec(); tcs.SetResult(); }
                catch (Exception ex) { tcs.SetException(ex); }
            })
            { IsBackground = true, Name = "XuatPdf-COM" };
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            return tcs.Task;
        }
        private static string TenDongCo(string progId) => progId switch
        {
            "Excel.Application" => "Microsoft Excel",
            "Ket.Application" or "ET.Application" => "WPS Spreadsheets",
            _ => progId
        };
        // =====================================================================================
        //  5) ĐỘNG CƠ COM: MICROSOFT EXCEL / WPS  (chạy trên luồng STA)
        // =====================================================================================
        private static void XuatBangCom(string progId, PhienXuat p)
        {
            Type? loai = Type.GetTypeFromProgID(progId);
            if (loai == null) throw new LoiDongCoException("chưa được cài đặt.");
            var pidTruoc = LayPidUngDungVanPhong();
            object? app = null, wbs = null, wb = null;
            int pid = 0;
            bool nenQuit = true;
            try
            {
                app = Activator.CreateInstance(loai) ?? throw new LoiDongCoException("không khởi tạo được tiến trình.");
                dynamic a = app;
                pid = TimPid(a, pidTruoc);
                // Dùng chung phiên của người dùng: tuyệt đối không Quit/đổi cấu hình/kill phiên đó
                if (pid > 0 && pidTruoc.Contains(pid))
                {
                    nenQuit = false;
                    throw new LoiDongCoException("đang dùng chung phiên đang mở của người dùng, bỏ qua để không ảnh hưởng dữ liệu của bạn.");
                }
                KiemTraSanSang(a, pid, pidTruoc);
                Thu(() => a.Visible = false);
                Thu(() => a.DisplayAlerts = false);
                Thu(() => a.ScreenUpdating = false);
                Thu(() => a.EnableEvents = false);
                Thu(() => a.AskToUpdateLinks = false);
                Thu(() => a.AutomationSecurity = 3);   // msoAutomationSecurityForceDisable: KHÔNG chạy macro của tệp .xlsm
                wbs = a.Workbooks;
                try { wb = ((dynamic)wbs).Open(p.BanSao, 0, true); }   // UpdateLinks=0, ReadOnly=true
                catch (COMException ex) { throw new LoiDongCoException("không mở được tệp: " + ex.Message); }
                dynamic w = wb!;
                DatThuocTinh(wb!, "Title", TieuDeTep);
                DatThuocTinh(wb!, "Author", TacGiaTep);
                DatThuocTinh(wb!, "Comments", GhiChuTep);
                DatThuocTinh(wb!, "Company", "Admin: " + Module_TaiKhoan.TenTaiKhoan_RAM);
                foreach (var (muc, dich) in p.ConLai)
                {
                    p.Ct.ThrowIfCancellationRequested();
                    object? sheetsObj = null, sheet = null;
                    try
                    {
                        sheetsObj = w.Worksheets;
                        sheet = ((dynamic)sheetsObj).Item(muc.TenSheet);
                        ApDungDinhDangCom((dynamic)sheet, muc.TenSheet, p.TT);
                        string tam = Path.Combine(p.ThuMucTam, Guid.NewGuid().ToString("N") + ".pdf");
                        bool hetGio = false;
                        using var canh = new System.Threading.Timer(_ =>
                        {
                            hetGio = true;
                            TieuDietNeuLaCuaToi(pid, pidTruoc);
                        }, null, ThoiGianToiDaMoiSheetCom, Timeout.Infinite);
                        try
                        {
                            // Tham số: Type=0(PDF), Filename, Quality=0(Standard), IncludeDocProperties=true, IgnorePrintAreas=false, From, To, OpenAfterPublish=false
                            ((dynamic)sheet).ExportAsFixedFormat(0, tam, 0, true, false, Type.Missing, Type.Missing, false);
                        }
                        catch (COMException ce)
                        {
                            if (hetGio) throw new LoiDongCoException($"bị treo quá {ThoiGianToiDaMoiSheetCom / 1000} giây khi xuất '{muc.TenSheet}' (có thể do hộp thoại ẩn), đã dừng tiến trình.");
                            if (ce.ErrorCode == -2146827284 || ce.ErrorCode == -2146827285)
                                throw new LoiDongCoException("phiên bản Office này không hỗ trợ xuất PDF trực tiếp hoặc đang ở chế độ giới hạn chức năng.");
                            if (DongCoDaChet(ce.HResult)) throw new LoiDongCoException("tiến trình bị đóng giữa chừng.");
                            throw;
                        }
                        finally { canh.Change(Timeout.Infinite, Timeout.Infinite); }
                        HoanTatTep(tam, dich, p, muc, TenDongCo(progId));
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (LoiDongCoException) { throw; }
                    catch (Exception ex)
                    {
                        p.GhiLoi(muc.TenSheet, ex.Message);   // lỗi riêng 1 sheet: ghi nhận, làm tiếp sheet khác
                        Debug.WriteLine($"[XuatPdf/{progId}] sheet {muc.TenSheet}: {ex.Message}");
                    }
                    finally
                    {
                        Tha(sheet);
                        Tha(sheetsObj);
                    }
                }
            }
            finally
            {
                if (wb != null) { try { ((dynamic)wb).Close(false); } catch { } Tha(wb); }
                Tha(wbs);
                if (app != null)
                {
                    if (nenQuit) { try { ((dynamic)app).Quit(); } catch { } }
                    Tha(app);
                }
                GC.Collect(); GC.WaitForPendingFinalizers();
                GC.Collect(); GC.WaitForPendingFinalizers();
                if (nenQuit) DamBaoThoat(pid, pidTruoc);
            }
        }
        private static void HoanTatTep(string tam, string dich, PhienXuat p, MucXuatPdf muc, string tenDongCo)
        {
            var fi = new FileInfo(tam);
            if (!fi.Exists || fi.Length == 0)
                throw new LoiDongCoException($"báo thành công nhưng tệp PDF của '{muc.TenSheet}' bị trống hoặc không được tạo.");
            File.Move(tam, dich, true);
            p.DanhDauXong(muc, dich, tenDongCo);
        }
        private static (string DiaDiem, string KemTheo)? OTheoSheet(string tenSheet)
        {
            switch (tenSheet.ToUpperInvariant())
            {
                case "LOAI_1": case "LOAI_2": case "LOAI_3": case "LOAI_4": case "KHONG_PL": return ("E4", "A7");
                case "BAO CAO TONG HOP": case "BC_BANHAT": return ("H4", "A7");
                case "DS_BANHAT": return ("F3", "A6");
                default: return null;
            }
        }
        //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
        private static void ApDungDinhDangCom(dynamic sheet, string tenSheet, ThongTinThayThe tt)
        {
            var o = OTheoSheet(tenSheet);
            if (o == null) return;
            object? r1 = null, r2 = null;
            try
            {
                r1 = sheet.Range[o.Value.DiaDiem];
                ((dynamic)r1).Value = tt.DiaDiemThangNam;
                r2 = sheet.Range[o.Value.KemTheo];
                ((dynamic)r2).Value = tt.TextKemTheo;
                try { ((dynamic)r2).Characters[tt.LenDau + 1, tt.LenGiua].Font.Underline = 2; }   // xlUnderlineStyleSingle
                catch { /* định dạng chữ bị giới hạn: bỏ qua, nội dung vẫn đúng */ }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[XuatPdf] Định dạng sheet {tenSheet} thất bại: {ex.Message}");
            }
            finally { Tha(r1); Tha(r2); }
        }
        private static void DatThuocTinh(object wb, string ten, string giaTri)
        {
            object? props = null, prop = null;
            try
            {
                props = wb.GetType().InvokeMember("BuiltinDocumentProperties", BindingFlags.GetProperty, null, wb, null);
                prop = props!.GetType().InvokeMember("Item", BindingFlags.GetProperty, null, props, new object[] { ten });
                prop!.GetType().InvokeMember("Value", BindingFlags.SetProperty, null, prop, new object[] { giaTri });
            }
            catch (Exception ex) { Debug.WriteLine($"[XuatPdf] Không đặt được thuộc tính {ten}: {ex.Message}"); }
            finally { Tha(prop); Tha(props); }
        }
        private static void KiemTraSanSang(dynamic a, int pid, HashSet<int> pidTruoc)
        {
            try
            {
                bool ready = a.Ready;
                if (!ready)
                {
                    TieuDietNeuLaCuaToi(pid, pidTruoc);
                    throw new LoiDongCoException("bị kẹt ở hộp thoại khởi động (kích hoạt bản quyền / chế độ an toàn).");
                }
            }
            catch (RuntimeBinderException) { /* WPS không có thuộc tính Ready: coi như sẵn sàng */ }
            catch (COMException)
            {
                TieuDietNeuLaCuaToi(pid, pidTruoc);
                throw new LoiDongCoException("từ chối kết nối COM vì đang hiện hộp thoại (có thể là kích hoạt bản quyền).");
            }
        }
        private static bool DongCoDaChet(int hr) =>
            hr == unchecked((int)0x800706BA) ||   // RPC server unavailable
            hr == unchecked((int)0x800706BE) ||   // RPC call failed
            hr == unchecked((int)0x80010108) ||   // RPC_E_DISCONNECTED
            hr == unchecked((int)0x800706BF);     // remote procedure call failed and did not execute
        private static void Thu(Action f) { try { f(); } catch { /* thuộc tính không có ở WPS/phiên bản này */ } }
        private static void Tha(object? o)
        {
            try { if (o != null && Marshal.IsComObject(o)) Marshal.FinalReleaseComObject(o); } catch { }
        }
        // ---------- quản lý tiến trình: chỉ đụng tiến trình do chính mình tạo ----------
        private static readonly string[] TenTienTrinhVanPhong = { "EXCEL", "et", "wps", "wpsoffice" };
        private static HashSet<int> LayPidUngDungVanPhong()
        {
            var kq = new HashSet<int>();
            foreach (var ten in TenTienTrinhVanPhong)
            {
                try { foreach (var pr in Process.GetProcessesByName(ten)) { kq.Add(pr.Id); pr.Dispose(); } }
                catch { }
            }
            return kq;
        }
        private static int TimPid(dynamic a, HashSet<int> truoc)
        {
            try
            {
                int hwnd = Convert.ToInt32((object)a.Hwnd);
                if (hwnd != 0 && GetWindowThreadProcessId((IntPtr)hwnd, out uint pid) != 0 && pid > 0) return (int)pid;
            }
            catch { }
            // Dự phòng (WPS không có Hwnd): tiến trình văn phòng MỚI xuất hiện; chỉ nhận khi duy nhất
            try
            {
                var moi = LayPidUngDungVanPhong().Where(x => !truoc.Contains(x)).ToList();
                if (moi.Count == 1) return moi[0];
            }
            catch { }
            return 0;
        }
        private static void TieuDietNeuLaCuaToi(int pid, HashSet<int> truoc)
        {
            if (pid <= 0 || truoc.Contains(pid)) return;
            try { using var pr = Process.GetProcessById(pid); pr.Kill(true); } catch { }
        }
        private static void DamBaoThoat(int pid, HashSet<int> truoc)
        {
            if (pid <= 0 || truoc.Contains(pid)) return;
            try
            {
                using var pr = Process.GetProcessById(pid);
                if (!pr.WaitForExit(3000)) pr.Kill(true);   // Quit() không kết thúc tiến trình → diệt tiến trình ma
            }
            catch { /* đã thoát */ }
        }
        // =====================================================================================
        //  6) ĐỘNG CƠ LIBREOFFICE (chạy nền, hồ sơ riêng → không đụng LibreOffice của người dùng)
        // =====================================================================================
        private static void XuatBangLibreOffice(string soffice, PhienXuat p)
        {
            string hoSo = Path.Combine(p.ThuMucTam, "lo_profile");
            string thuMucRa = Path.Combine(p.ThuMucTam, "lo_out");
            Directory.CreateDirectory(thuMucRa);
            using var wbGoc = new XLWorkbook(p.BanSao);
            foreach (var ws in wbGoc.Worksheets)
            {
                var o = OTheoSheet(ws.Name);
                if (o == null) continue;
                try
                {
                    ws.Cell(o.Value.DiaDiem).Value = p.TT.DiaDiemThangNam;
                    ws.Cell(o.Value.KemTheo).Value = p.TT.TextKemTheo;
                    ws.Cell(o.Value.KemTheo).GetRichText().Substring(p.TT.LenDau, p.TT.LenGiua).SetUnderline();
                }
                catch (Exception ex) { Debug.WriteLine($"[XuatPdf] Định dạng (ClosedXML) {ws.Name}: {ex.Message}"); }
            }
            foreach (var (muc, dich) in p.ConLai)
            {
                p.Ct.ThrowIfCancellationRequested();
                string tempXlsx = Path.Combine(p.ThuMucTam, Guid.NewGuid().ToString("N") + ".xlsx");
                try
                {
                    using (var tempWb = new XLWorkbook())
                    {
                        wbGoc.Worksheet(muc.TenSheet).CopyTo(tempWb, muc.TenSheet);
                        tempWb.Properties.Title = TieuDeTep;
                        tempWb.Properties.Author = TacGiaTep;
                        tempWb.Properties.Comments = GhiChuTep;
                        tempWb.Properties.Company = "Admin: " + Module_TaiKhoan.TenTaiKhoan_RAM;
                        tempWb.SaveAs(tempXlsx);
                    }
                    var psi = new ProcessStartInfo
                    {
                        FileName = soffice,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    // ArgumentList: tự xử lý dấu ngoặc kép / khoảng trắng / dấu '\' cuối đường dẫn (bản cũ dùng chuỗi nối tay dễ hỏng)
                    psi.ArgumentList.Add("-env:UserInstallation=" + new Uri(hoSo + Path.DirectorySeparatorChar).AbsoluteUri.TrimEnd('/'));
                    foreach (var s in new[] { "--headless", "--invisible", "--nologo", "--nodefault", "--norestore", "--nofirststartwizard" })
                        psi.ArgumentList.Add(s);
                    psi.ArgumentList.Add("--convert-to"); psi.ArgumentList.Add("pdf");
                    psi.ArgumentList.Add("--outdir"); psi.ArgumentList.Add(thuMucRa);
                    psi.ArgumentList.Add(tempXlsx);
                    using (var pr = Process.Start(psi) ?? throw new Exception("Không khởi động được LibreOffice."))
                    {
                        var dongHo = Stopwatch.StartNew();
                        while (!pr.WaitForExit(250))
                        {
                            if (p.Ct.IsCancellationRequested || dongHo.ElapsedMilliseconds > ThoiGianToiDaMoiSheetLibre)
                            {
                                try { if (!pr.HasExited) pr.Kill(true); } catch { }
                                p.Ct.ThrowIfCancellationRequested();
                                throw new TimeoutException($"LibreOffice bị treo quá {ThoiGianToiDaMoiSheetLibre / 1000} giây ở sheet '{muc.TenSheet}', đã dừng.");
                            }
                        }
                        if (pr.ExitCode != 0) throw new Exception($"LibreOffice từ chối kết xuất (mã lỗi {pr.ExitCode}).");
                    }
                    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
                    string ra = Path.Combine(thuMucRa, Path.GetFileNameWithoutExtension(tempXlsx) + ".pdf");
                    HoanTatTep(ra, dich, p, muc, "LibreOffice");
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    p.GhiLoi(muc.TenSheet, ex.Message);
                    Debug.WriteLine($"[XuatPdf/LibreOffice] sheet {muc.TenSheet}: {ex.Message}");
                }
                finally
                {
                    try { if (File.Exists(tempXlsx)) File.Delete(tempXlsx); } catch { }
                }
            }
        }
    }
}