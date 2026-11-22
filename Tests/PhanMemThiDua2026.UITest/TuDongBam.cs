using System.ComponentModel;
using System.Reflection;
namespace PhanMemThiDua2026.UITest
{
    /// <summary>
    /// Bộ bấm thử chạy trên LUỒNG GIAO DIỆN của phần mềm (async/await, không chặn vòng thông điệp).
    ///  1. Đăng nhập bằng tài khoản trong uitest.json.
    ///  2. Form2 (giao diện chính): bấm lần lượt mọi nút / control có sự kiện Click / mục menu / thẻ tab.
    ///     Thứ tự "đi sâu trước": control vừa XUẤT HIỆN sau lần bấm trước (trang mới mở) được bấm trước,
    ///     nút Đóng/Hủy để sau cùng, Xuất trước Nhập (để có tệp vừa xuất dùng cho Nhập).
    ///  3. Form con mở ra (ShowDialog hoặc Show) được bấm thử tới DoSauToiDa tầng rồi đóng.
    ///  4. Mở trực tiếp từng form còn lại chưa gặp (hàm dựng không tham số), bấm thử, đóng.
    /// Mỗi control chỉ bấm một lần trong cả lần chạy (khóa = tên lớp form + đường dẫn tên control + chữ).
    /// </summary>
    internal static class TuDongBam
    {
        private sealed class HanhDong
        {
            public string Khoa = "", Ten = "";
            public int UuTien, ThuTu;
            public Action Thuc = () => { };
        }
        private static CauHinh _ch = new();
        private static Assembly _asm => NapPhanMem.Asm;
        private static readonly HashSet<string> _daBam = new(StringComparer.Ordinal);
        private static readonly HashSet<string> _loaiFormDaGap = new(StringComparer.Ordinal);
        private static readonly HashSet<Form> _formDangThu = new();
        private static readonly List<Form> _nganXep = new();   // form đang bấm thử, phần tử cuối = trong cùng
        private static readonly HashSet<Form> _modalDaNhan = new();
        private static System.Windows.Forms.Timer? _dongHoModal;
        private static int _tongBuoc;
        private static readonly MethodInfo? _onClick = typeof(Control).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(EventArgs) });
        private static readonly MethodInfo? _onMouseClick = typeof(Control).GetMethod("OnMouseClick", BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(MouseEventArgs) });
        private static readonly PropertyInfo? _events = typeof(Component).GetProperty("Events", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly object? _khoaClick = KhoaSuKien("s_clickEvent", "EventClick");
        private static readonly object? _khoaMouseClick = KhoaSuKien("s_mouseClickEvent", "EventMouseClick");
        private static object? KhoaSuKien(params string[] ten)
        {
            foreach (var t in ten)
            {
                var f = typeof(Control).GetField(t, BindingFlags.NonPublic | BindingFlags.Static);
                if (f != null) return f.GetValue(null);
            }
            return null;
        }
        // =============================================================== ĐIỂM VÀO
        public static async void BatDau(CauHinh ch, Form form1)
        {
            _ch = ch;
            try
            {
                _dongHoModal = new System.Windows.Forms.Timer { Interval = 200 };
                _dongHoModal.Tick += (_, _) => CanhFormModal();
                _dongHoModal.Start();
                await Task.Delay(1500);
                var form2 = await DangNhapAsync(form1);
                if (form2 == null) { KetThuc(); return; }
                await Task.Delay(4000);   // Form2 nạp tiếp các trang nền sau khi hiện
                BaoCao.Ghi("----- GIAI ĐOẠN 1: giao diện chính (Form2) và các trang/form mở từ đó -----");
                await KiemThuFormAsync(form2, 0);
                if (_ch.MoTrucTiepCacFormConLai && !form2.IsDisposed)
                {
                    BaoCao.Ghi("");
                    BaoCao.Ghi("----- GIAI ĐOẠN 2: mở trực tiếp các form chưa gặp -----");
                    await MoCacFormConLaiAsync(form2);
                }
            }
            catch (Exception ex)
            {
                BaoCao.SuKien(KetQua.Loi, "Bộ bấm thử dừng vì lỗi: " + ex);
            }
            KetThuc();
        }
        private static void KetThuc()
        {
            _dongHoModal?.Stop();
            ChuongTrinh.DaXong = true;
            BaoCao.TongKet();   // tiến trình điều phối đọc tổng kết từ tệp báo cáo
            BaoCao.Ghi("Đã kiểm thử xong => tắt phần mềm.");
            ThoatPhanMem();
        }
        /// <summary>
        /// Tắt hẳn phần mềm sau khi kiểm thử: dùng chính đường thoát an toàn của phần mềm (Module_ThoatAnToan.ThoatDutKhoat:
        /// ghi nốt nhật ký rồi Environment.Exit) — KHÔNG dùng Application.Exit vì Form2 có thể hỏi/quay về màn đăng nhập.
        /// Chốt chặn: 20 giây sau vẫn còn sống thì tự diệt tiến trình.
        /// </summary>
        private static void ThoatPhanMem()
        {
            new Thread(() =>
            {
                Thread.Sleep(20000);
                try { System.Diagnostics.Process.GetCurrentProcess().Kill(); } catch { }
            }) { IsBackground = true, Name = "UITest - chốt chặn thoát" }.Start();
            try
            {
                var m = _asm.GetType("PhanMemThiDua2026.Module_ThoatAnToan")
                    ?.GetMethod("ThoatDutKhoat", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (m != null && m.GetParameters().All(p => p.IsOptional))
                    m.Invoke(null, m.GetParameters().Select(p => p.DefaultValue).ToArray());
            }
            catch { }
            Environment.Exit(0);
        }
        // =============================================================== ĐĂNG NHẬP
        private static async Task<Form?> DangNhapAsync(Form form1)
        {
            BaoCao.BatDauBuoc("Form1", "Đăng nhập");
            try
            {
                var ten = TimControl(form1, "text_TenDangNhap");
                var mk = TimControl(form1, "text_MatKhau");
                var nut = TimControl(form1, "btn_DangNhap");
                if (ten == null || mk == null || nut == null)
                {
                    BaoCao.SuKien(KetQua.Loi, "Không tìm thấy ô tên đăng nhập / mật khẩu / nút Đăng nhập trên Form1");
                    return null;
                }
                ten.Text = _ch.TenDangNhap;
                mk.Text = _ch.MatKhau;
                Bam(nut);
                var han = DateTime.Now.AddSeconds(120);
                while (DateTime.Now < han)
                {
                    await Task.Delay(300);
                    var f2 = CacForm().FirstOrDefault(f => f.GetType().Name == "Form2_FormCha" && f.Visible);
                    if (f2 != null) { BaoCao.SuKien(KetQua.OK, "Đã vào giao diện chính"); return f2; }
                }
                BaoCao.SuKien(KetQua.Loi, "Sau 120 giây vẫn chưa vào được giao diện chính (sai tài khoản?)");
                return null;
            }
            finally { BaoCao.KetThucBuoc(); }
        }
        // =============================================================== BẤM THỬ MỘT FORM
        private static async Task KiemThuFormAsync(Form form, int doSau)
        {
            string tenForm = form.GetType().Name;
            if (tenForm == "Form9_GoCaiDat") { DongForm(form); return; }
            if (_ch.BoQuaForm.Contains(tenForm) || !_formDangThu.Add(form)) return;
            _loaiFormDaGap.Add(tenForm);
            _nganXep.Add(form);
            var theHe = new Dictionary<string, int>();
            int lanQuet = 0, soBuoc = 0;
            try
            {
                // LƯỢT 1: trạng thái như người dùng vừa mở form (ô trống) => kiểm tra các nhánh kiểm tra dữ liệu / thông báo,
                //          chọn thử từng mục ComboBox, nhấp / nhấp đúp lưới, thẻ tab, menu.
                // LƯỢT 2 (TestSau): mỗi lần bấm đều điền sẵn ô nhập ("UITest"...), chọn dòng lưới => nút Thêm/Sửa/Lưu/Xóa
                //          chạy thật tới cuối. Xóa chỉ bấm khi đã chọn được dòng do chính công cụ tạo (chứa "UITest").
                for (int luot = 1; luot <= (_ch.TestSau && !LaFormBaoMat(tenForm) ? 2 : 1); luot++)
                {
                    bool coDuLieu = luot == 2;
                    if (coDuLieu)
                    {
                        if (!ConDung(form)) break;
                        if (!QuetHanhDong(form, true).Any(h => !_daBam.Contains(h.Khoa))) break;
                        BaoCao.BatDauBuoc(tenForm, "--- Lượt 2: có dữ liệu (tự điền ô nhập + chọn dòng) ---");
                        BaoCao.KetThucBuoc();
                    }
                    while (soBuoc < _ch.SoBuocToiDaMoiForm && ConDung(form))
                    {
                        if (!await ChoDenLuotAsync(form)) break;
                        lanQuet++;
                        if (!coDuLieu) ChonDongDauLuoi(form);
                        var ds = QuetHanhDong(form, coDuLieu);
                        foreach (var h in ds) theHe.TryAdd(h.Khoa, lanQuet);
                        var chon = ds.Where(h => !_daBam.Contains(h.Khoa))
                                     .OrderByDescending(h => theHe[h.Khoa]).ThenBy(h => h.UuTien).ThenBy(h => h.ThuTu)
                                     .FirstOrDefault();
                        if (chon == null) break;
                        _daBam.Add(chon.Khoa);
                        soBuoc++;
                        bool laXoa = LaNutXoa(chon.Ten);
                        if (!coDuLieu)
                        {
                            if (laXoa) BoChonLuoi(form);   // lượt 1: Xóa khi CHƯA chọn dòng => thử nhánh "chưa chọn", không xóa dữ liệu thật
                        }
                        else if (laXoa)
                        {
                            string tx = chon.Ten.ToLowerInvariant();
                            if (tx.Contains("toàn bộ") || tx.Contains("tất cả") || tx.Contains(" all"))
                            {
                                BaoCao.BatDauBuoc(tenForm, chon.Ten);
                                BaoCao.SuKien(KetQua.BoQua, "Xóa toàn bộ/tất cả: chỉ thử ở lượt 1, không lặp lại ở lượt có dữ liệu");
                                BaoCao.KetThucBuoc();
                                continue;
                            }
                            if (!ChonDongLuoi(form, false))
                            {
                                BaoCao.BatDauBuoc(tenForm, chon.Ten);
                                BaoCao.SuKien(KetQua.BoQua, "Không có dòng \"UITest\" do công cụ tạo => không bấm Xóa vào dữ liệu thật");
                                BaoCao.KetThucBuoc();
                                continue;
                            }
                        }
                        else if (LaNutThem(chon.Ten))
                        {
                            BoChonLuoi(form);
                            DienDuLieu(form, true);    // ghi đè ô nhập bằng dữ liệu mẫu UITest => tạo bản ghi mới nhận ra được
                        }
                        else
                        {
                            ChonDongLuoi(form, true);  // ưu tiên dòng UITest, không có thì dòng đầu
                            DienDuLieu(form, false);   // chỉ điền ô còn trống
                        }
                        await ThucHienAsync(form, chon, doSau);
                    }
                }
            }
            finally
            {
                _nganXep.Remove(form);
                _formDangThu.Remove(form);
            }
        }
        private static async Task ThucHienAsync(Form form, HanhDong h, int doSau)
        {
            var truoc = CacForm().ToHashSet();
            _tongBuoc++;
            BaoCao.BatDauBuoc(form.GetType().Name, h.Ten);
            try
            {
                try { h.Thuc(); }
                catch (TargetInvocationException tie) when (tie.InnerException != null) { BaoCao.SuKien(KetQua.Loi, "Ngoại lệ: " + MoTa(tie.InnerException)); }
                catch (Exception ex) { BaoCao.SuKien(KetQua.Loi, "Ngoại lệ: " + MoTa(ex)); }
                await ChoXuLyXongAsync();
                // Form KHÔNG modal mới hiện ra sau lần bấm => bấm thử tầng sau rồi đóng
                foreach (var f in CacForm().Where(f => !truoc.Contains(f) && f.Visible && !f.Modal && !LaFormPhu(f)).ToList())
                {
                    BaoCao.SuKien(KetQua.OK, $"Mở form {f.GetType().Name} \"{f.Text}\"");
                    if (doSau < _ch.DoSauToiDa) await KiemThuFormAsync(f, doSau + 1);
                    DongForm(f);
                    await ChoXuLyXongAsync();
                }
            }
            finally { BaoCao.KetThucBuoc(); }
        }
        // =============================================================== FORM MODAL (ShowDialog)
        // Khi phần mềm gọi ShowDialog, lệnh bấm của ta bị chặn bên trong; vòng thông điệp modal vẫn chạy nên đồng hồ này
        // vẫn chạy => bấm thử form modal ngay tại đây, xong thì đóng để lệnh bấm bên ngoài chạy tiếp.
        private static void CanhFormModal()
        {
            // Form bị ẩn (đóng ShowDialog) => bỏ đánh dấu: phần mềm hay GIỮ form trong RAM rồi ShowDialog lại đúng đối tượng
            // đó (vd Form5 Quên mật khẩu, Form48 Xuất PDF). Bản cũ chỉ bỏ khi Dispose => lần mở thứ 2 không ai xử lý => treo.
            _modalDaNhan.RemoveWhere(f => f.IsDisposed || !f.Visible);
            foreach (var k in _modalLanDau.Keys.Where(f => f.IsDisposed || !f.Visible).ToList()) _modalLanDau.Remove(k);
            foreach (var f in CacForm())
            {
                if (!f.Modal || !f.Visible || f.GetType().Name.Contains("Loading")) continue;
                _modalLanDau.TryAdd(f, DateTime.Now);
                if (_modalDaNhan.Add(f)) { _ = XuLyFormModalAsync(f); continue; }
                // Chốt chặn: form modal mở quá lâu mà không có hoạt động bấm thử nào => đóng cưỡng bức để phần mềm chạy tiếp
                if ((DateTime.Now - _modalLanDau[f]).TotalSeconds > ModalToiDaGiay && BaoCao.GiayKhongHoatDong > ModalToiDaGiay)
                {
                    BaoCao.SuKien(KetQua.Treo, $"Form {f.GetType().Name} \"{f.Text}\" mở {ModalToiDaGiay} giây không phản hồi => đóng cưỡng bức");
                    _modalLanDau[f] = DateTime.Now;
                    DongForm(f);
                }
            }
        }
        private const int ModalToiDaGiay = 120;
        private static readonly Dictionary<Form, DateTime> _modalLanDau = new();
        /// <summary>Form kiểm tra bảo mật: sai 3 lần là phần mềm tự tắt (ThoatDutKhoat) => chỉ thử ở lượt 1, không bấm lặp.</summary>
        private static bool LaFormBaoMat(string ten) => ten is "Form5_QuenPass" or "Form24_XacMinhAdmin" or "Form1";
        private static async Task XuLyFormModalAsync(Form f)
        {
            try
            {
                await Task.Delay(500);
                if (!ConDung(f)) return;
                string ten = f.GetType().Name;
                if (LaFormPhu(f) || _ch.BoQuaForm.Contains(ten))
                {
                    // Form bị bỏ qua (Lời chào, Form1...) mà lại mở dạng modal => phần mềm đứng chờ => đóng nó đi
                    await Task.Delay(1500);
                    BaoCao.SuKien(KetQua.OK, $"Đóng form bị bỏ qua {ten} \"{f.Text}\"");
                    return;   // finally bên dưới đóng form
                }
                if (LaHopThoaiThongBao(f))
                {
                    TraLoiThongBao(f);
                    return;
                }
                BaoCao.SuKien(KetQua.OK, $"Mở hộp thoại {ten} \"{f.Text}\"");
                DienMatKhau(f);
                int doSau = _nganXep.Count;   // tầng của form gọi ShowDialog
                if (doSau <= _ch.DoSauToiDa && !_ch.BoQuaForm.Contains(ten)) await KiemThuFormAsync(f, doSau);
            }
            catch (Exception ex) { BaoCao.SuKien(KetQua.Loi, "Lỗi khi xử lý form modal: " + MoTa(ex)); }
            finally
            {
                if (ConDung(f)) DongForm(f);
            }
        }
        /// <summary>Form nhỏ chỉ có chữ + vài nút (KryptonMessageBox, thông báo tự vẽ...) => coi như hộp thoại thông báo.</summary>
        private static bool LaHopThoaiThongBao(Form f)
        {
            if (f.GetType().Name.Contains("MessageBox", StringComparison.OrdinalIgnoreCase)) return true;
            var tatCa = DuyetControl(f).ToList();
            if (tatCa.Any(c => LaONhapLieu(c))) return false;
            int soNut = tatCa.Count(c => c is IButtonControl && c.Visible);
            return soNut is >= 1 and <= 3 && tatCa.Count < 40;
        }
        private static void TraLoiThongBao(Form f)
        {
            var nut = DuyetControl(f).Where(c => c is IButtonControl && c.Visible && c.Enabled).ToList();
            string noiDung = string.Join(" ", DuyetControl(f).Where(c => c is not IButtonControl).Select(LayChu)
                                .Where(s => !string.IsNullOrWhiteSpace(s))).Replace("\r", " ").Replace("\n", " ");
            string[] uuTien = _ch.TraLoiCo && !HopThoaiHeThong.LaHoiMoTep(f.Text + " " + noiDung)
                ? new[] { "có", "yes", "ok", "đồng ý", "xác nhận", "tiếp tục", "thử lại" }
                : new[] { "không", "no", "hủy", "huỷ", "cancel", "đóng", "ok" };
            Control? chon = null;
            foreach (var t in uuTien)
            {
                chon = nut.FirstOrDefault(n => LayChu(n).Replace("&", "").Trim().ToLowerInvariant().StartsWith(t));
                if (chon != null) break;
            }
            chon ??= nut.FirstOrDefault();
            bool laXacNhan = nut.Any(n => LayChu(n).Trim().ToLowerInvariant() is "có" or "không" or "yes" or "no");
            var kq = BaoCao.PhanLoaiThongDiep(f.Text, noiDung, laXacNhan);
            BaoCao.SuKien(kq, $"Thông báo \"{f.Text}\": {HopThoaiHeThong.RutGon(noiDung, 400)} => bấm {(chon == null ? "(đóng)" : LayChu(chon))}");
            if (chon != null) Bam(chon);
        }
        // =============================================================== GIAI ĐOẠN 2
        private static async Task MoCacFormConLaiAsync(Form form2)
        {
            var cacLoai = _asm.GetTypes()
                .Where(t => typeof(Form).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
                .Where(t => !_ch.BoQuaForm.Contains(t.Name) && t.Name != "Form2_FormCha" && t.Name != "Form9_GoCaiDat")
                .OrderBy(t => SoThuTuForm(t.Name)).ThenBy(t => t.Name)
                .ToList();
            foreach (var t in cacLoai)
            {
                if (_loaiFormDaGap.Contains(t.Name) || form2.IsDisposed) continue;
                _loaiFormDaGap.Add(t.Name);
                Form? f = null;
                BaoCao.BatDauBuoc(t.Name, "(mở trực tiếp)");
                try
                {
                    var truoc = CacForm().ToHashSet();
                    try
                    {
                        f = (Form)Activator.CreateInstance(t)!;
                        f.StartPosition = FormStartPosition.CenterScreen;
                        f.Show(form2);
                    }
                    catch (Exception ex)
                    {
                        BaoCao.SuKien(KetQua.Loi, "Không mở được: " + MoTa(ex is TargetInvocationException { InnerException: { } ie } ? ie : ex));
                        f?.Dispose();
                        f = null;
                    }
                    await ChoXuLyXongAsync();
                    if (f != null && ConDung(f)) await KiemThuFormAsync(f, 1);
                    if (f != null) DongForm(f);
                    foreach (var phu in CacForm().Where(x => !truoc.Contains(x) && !LaFormPhu(x))) DongForm(phu);
                    await ChoXuLyXongAsync();
                }
                finally { BaoCao.KetThucBuoc(); }
            }
        }
        private static int SoThuTuForm(string ten)
        {
            var m = System.Text.RegularExpressions.Regex.Match(ten, @"^Form(\d+)");
            return m.Success ? int.Parse(m.Groups[1].Value) : 9999;
        }
        // =============================================================== QUÉT CONTROL
        /// <param name="coDuLieu">Lượt 2: chỉ lấy nút/menu (khóa riêng "2|" nên bấm lại được), ComboBox/lưới/thẻ/ô tích đã thử ở lượt 1.</param>
        private static List<HanhDong> QuetHanhDong(Form form, bool coDuLieu = false)
        {
            var ds = new List<HanhDong>();
            string goc = (coDuLieu ? "2|" : "") + form.GetType().Name;
            int thuTu = 0;
            bool laNutLuot1 = false;   // đánh dấu lời gọi Them hiện tại là nút/menu (được lặp lại ở lượt 2)
            void Them(string duongDan, string ten, Action thuc, string chuDeXep)
            {
                if (BiBoQua(ten) || BiBoQua(duongDan)) return;
                if (coDuLieu && !laNutLuot1) return;
                ds.Add(new HanhDong
                {
                    Khoa = goc + ":" + duongDan + "|" + ten,
                    Ten = coDuLieu ? ten + " [có dữ liệu]" : ten,
                    Thuc = thuc,
                    ThuTu = thuTu++,
                    UuTien = UuTien(chuDeXep),
                });
            }
            void ThemNut(string duongDan, string ten, Action thuc, string chuDeXep)
            {
                laNutLuot1 = true;
                try { Them(duongDan, ten, thuc, chuDeXep); } finally { laNutLuot1 = false; }
            }
            void Duyet(Control c, string duongDan)
            {
                foreach (Control con in c.Controls)
                {
                    if (con.IsDisposed || !con.Visible) continue;
                    if (con is Form fc && fc != form)
                    {
                        _loaiFormDaGap.Add(fc.GetType().Name);   // trang nhúng trong Form2
                        if (_ch.BoQuaForm.Contains(fc.GetType().Name)) continue;
                    }
                    string ma = string.IsNullOrEmpty(con.Name) ? $"{con.GetType().Name}#{c.Controls.GetChildIndex(con)}" : con.Name;
                    string dd = duongDan + "/" + ma;
                    if (con.ContextMenuStrip != null) DuyetMenu(con.ContextMenuStrip.Items, dd + "/menu", "Menu chuột phải", ThemNut);
                    if (con is ToolStrip ts) { DuyetMenu(ts.Items, dd, "", ThemNut); continue; }
                    // ComboBox: chọn thử tối đa 3 mục (khác mục đang chọn) rồi trả về mục ban đầu
                    if (con.Enabled && LayDanhSachCombo(con, out int soMuc, out int dangChon) && soMuc > 0)
                    {
                        var cbx = con;
                        string kGoc = goc + ":" + dd;
                        _chiSoGocCombo.TryAdd(kGoc, dangChon);
                        int daThem = 0;
                        for (int i = 0; i < soMuc && daThem < 3; i++)
                        {
                            if (i == _chiSoGocCombo[kGoc]) continue;
                            int idx = i;
                            Them(dd + "/chon:" + i, $"[Chọn] {ma} = {ChuMucCombo(cbx, i)}", () => DatChiSoCombo(cbx, idx), "chọn");
                            daThem++;
                        }
                        if (daThem > 0)
                        {
                            int goc0 = _chiSoGocCombo[kGoc];
                            Them(dd + "/tralai", $"[Chọn] {ma} = (trả về mục ban đầu)", () => DatChiSoCombo(cbx, goc0), "chọn");
                        }
                    }
                    // Lưới: nhấp dòng đầu (CellClick: nhiều form nạp dữ liệu dòng vào ô sửa) và nhấp đúp (thường mở form chi tiết)
                    if (con is DataGridView g && g.Enabled && g.Rows.Cast<DataGridViewRow>().Any(r => r.Visible && !r.IsNewRow))
                    {
                        var gg = g;
                        if (CoSuKienLuoi(g, "CellClick") || CoSuKienLuoi(g, "CellMouseClick") || CoSuKienLuoi(g, "SelectionChanged"))
                            Them(dd + "/nhap", $"[Lưới] nhấp dòng đầu ({ma})", () => NhapLuoi(gg, ChiSoDongDau(gg), false), "lưới");
                        if (CoSuKienLuoi(g, "CellDoubleClick") || CoSuKienLuoi(g, "CellMouseDoubleClick") || CoSuKienLuoi(g, "DoubleClick"))
                            Them(dd + "/nhapdup", $"[Lưới] nhấp đúp dòng đầu ({ma})", () => NhapLuoi(gg, ChiSoDongDau(gg), true), "lưới");
                    }
                    if (con is TabControl tab)
                    {
                        foreach (TabPage tp in tab.TabPages)
                        {
                            var tc = tab; var p = tp;
                            Them(dd + "/tab:" + tp.Name, $"[Thẻ] {tp.Text}", () => tc.SelectedTab = p, tp.Text);
                        }
                    }
                    if (LaONhapLieu(con)) continue;   // không đi vào bên trong ô nhập / lưới (control con nội bộ của Krypton)
                    if (con.Enabled)
                    {
                        string chu = LayChu(con);
                        string ten = string.IsNullOrWhiteSpace(chu) ? ma : $"{chu.Trim()} ({ma})";
                        if (con is CheckBox cb)
                        {
                            var x = cb;
                            Them(dd, "[Tích 2 lần] " + ten, () => { x.Checked = !x.Checked; Application.DoEvents(); x.Checked = !x.Checked; }, chu);
                        }
                        else if (con is RadioButton) { /* chọn radio làm đổi trạng thái không trả lại được => bỏ */ }
                        else if (con is IButtonControl || con is ButtonBase)
                            ThemNut(dd, ten, () => Bam(con), chu + " " + ma);
                        else if (CoSuKien(con, _khoaClick) || CoSuKien(con, _khoaMouseClick))
                            Them(dd, ten, () => Bam(con), chu + " " + ma);   // nhãn/ảnh có Click: chỉ lượt 1
                    }
                    Duyet(con, dd);
                }
            }
            Duyet(form, "");
            return ds;
        }
        private static void DuyetMenu(ToolStripItemCollection items, string duongDan, string tienTo, Action<string, string, Action, string> them)
        {
            foreach (ToolStripItem it in items)
            {
                if (it is ToolStripSeparator || !it.Available) continue;
                string ma = string.IsNullOrEmpty(it.Name) ? it.Text ?? "?" : it.Name;
                string dd = duongDan + "/" + ma;
                string chu = (it.Text ?? "").Replace("&", "").Trim();
                if (it is ToolStripDropDownItem dd2 && dd2.DropDownItems.Count > 0)
                {
                    DuyetMenu(dd2.DropDownItems, dd, (tienTo.Length > 0 ? tienTo + " > " : "") + chu, them);
                    continue;
                }
                if (!it.Enabled || it is ToolStripTextBox or ToolStripComboBox or ToolStripLabel or ToolStripProgressBar) continue;
                var x = it;
                string ten = (tienTo.Length > 0 ? $"[{tienTo}] " : "[Menu] ") + (chu.Length > 0 ? chu : ma);
                them(dd, ten, () => x.PerformClick(), chu + " " + ma);
            }
        }
        /// <summary>
        /// Thứ tự bấm trong cùng một "thế hệ": 0 Thêm/Tạo/Lưu (tạo dòng UITest trước) → 1 bình thường (Sửa, Xuất...)
        /// → 2 Nhập/Nạp (dùng tệp vừa xuất) → 3 Xóa (xóa dòng UITest vừa tạo) → 4 Đóng/Hủy (sau cùng).
        /// </summary>
        private static int UuTien(string chu)
        {
            string t = chu.ToLowerInvariant();
            if (t.Contains("đóng") || t.Contains("hủy") || t.Contains("huỷ") || t.Contains("cancel") || t.Contains("close")
                || t.Contains("quay lại") || t.Trim() == "x") return 4;
            if (LaNutXoa(t)) return 3;
            if (t.Contains("nhập") || t.Contains("import") || t.Contains("khôi phục") || t.Contains("restore") || t.Contains("nạp")) return 2;
            if (t.Contains("thêm") || t.Contains("tạo") || t.Contains("lưu") || t.Contains("add") || t.Contains("save")) return 0;
            return 1;
        }
        private static bool LaNutThem(string chu)
        {
            string t = chu.ToLowerInvariant();
            return t.Contains("thêm") || t.Contains("tạo mới") || t.Contains("add") || t.Contains("_them") || t.Contains("đăng ký");
        }
        private static bool LaNutXoa(string chu)
        {
            string t = chu.ToLowerInvariant();
            return t.Contains("xóa") || t.Contains("xoá") || t.Contains("delete") || t.Contains("remove") || t.Contains("_xoa");
        }
        /// <summary>Cấm CỨNG (không phụ thuộc uitest.json): Gỡ cài đặt, Sao lưu/Khôi phục qua dịch vụ (tắt phần mềm, ghi đè CSDL).</summary>
        private static readonly string[] CamTuyetDoi = { "Gỡ cài đặt", "GoCaiDat", "Form9_GoCaiDat", "_Khoiphuc", "_KhoiPhuc", "_SaoLuu" };
        private static bool BiBoQua(string s) =>
            CamTuyetDoi.Any(b => s.Contains(b, StringComparison.OrdinalIgnoreCase))
            || _ch.BoQuaNut.Any(b => !string.IsNullOrWhiteSpace(b) && s.Contains(b, StringComparison.OrdinalIgnoreCase));
        private static bool LaONhapLieu(Control c)
        {
            if (c is TextBoxBase or ListControl or DataGridView or TreeView or ListView or UpDownBase or DateTimePicker
                or MonthCalendar or WebBrowser or ScrollBar) return true;
            string n = c.GetType().Name;
            return n.Contains("TextBox") || n.Contains("ComboBox") || n.Contains("DataGridView") || n.Contains("NumericUpDown")
                   || n.Contains("DateTimePicker") || n.Contains("ListBox") || n.Contains("RichText") || n.Contains("WebView");
        }
        private static bool CoSuKien(Control c, object? khoa)
        {
            if (khoa == null || _events == null) return false;
            try { return _events.GetValue(c) is EventHandlerList l && l[khoa] != null; }
            catch { return false; }
        }
        // =============================================================== THAO TÁC
        private static void Bam(Control c)
        {
            if (c is IButtonControl b) { b.PerformClick(); return; }
            _onClick?.Invoke(c, new object[] { EventArgs.Empty });
            if (CoSuKien(c, _khoaMouseClick))
            {
                var p = new Point(Math.Max(1, c.Width / 2), Math.Max(1, c.Height / 2));
                _onMouseClick?.Invoke(c, new object[] { new MouseEventArgs(MouseButtons.Left, 1, p.X, p.Y, 0) });
            }
        }
        /// <summary>Nhiều nút cần chọn sẵn một dòng (Sửa/Xóa/Xem...) => chọn dòng đầu của lưới nào chưa có dòng chọn.</summary>
        private static void ChonDongDauLuoi(Form f)
        {
            foreach (var g in DuyetControl(f).OfType<DataGridView>())
            {
                try
                {
                    if (!g.Visible || g.Rows.Count == 0 || g.CurrentCell != null) continue;
                    var cot = g.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.Visible);
                    var dong = g.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Visible && !r.IsNewRow);
                    if (cot != null && dong != null) g.CurrentCell = g[cot.Index, dong.Index];
                }
                catch { }
            }
        }
        // =============================================================== DỮ LIỆU MẪU / COMBO / LƯỚI
        public const string DAU_HIEU = "UITest";
        private static readonly Dictionary<string, int> _chiSoGocCombo = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, object?> _khoaSuKienLuoi = new(StringComparer.OrdinalIgnoreCase);
        private static string ChuanHoaTenSuKien(string s) =>
            s.Replace("s_", "", StringComparison.OrdinalIgnoreCase).Replace("EVENT_DATAGRIDVIEW", "", StringComparison.OrdinalIgnoreCase)
             .Replace("EVENT", "", StringComparison.OrdinalIgnoreCase).Replace("_", "").ToLowerInvariant();
        /// <summary>Lưới có gắn sự kiện tên này không (tìm khóa sự kiện tĩnh trong DataGridView/Control theo tên, chịu được đổi tên giữa các bản .NET).</summary>
        private static bool CoSuKienLuoi(DataGridView g, string tenSuKien)
        {
            if (!_khoaSuKienLuoi.TryGetValue(tenSuKien, out var khoa))
            {
                string can = ChuanHoaTenSuKien(tenSuKien);
                khoa = null;
                for (Type? t = typeof(DataGridView); t != null && khoa == null; t = t.BaseType)
                    foreach (var f in t.GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
                        if (f.FieldType == typeof(object) && ChuanHoaTenSuKien(f.Name) == can) { khoa = f.GetValue(null); break; }
                _khoaSuKienLuoi[tenSuKien] = khoa;
            }
            if (khoa == null) return true;   // không dò được khóa => cứ thử
            try { return _events?.GetValue(g) is EventHandlerList l && l[khoa] != null; }
            catch { return true; }
        }
        private static int ChiSoDongDau(DataGridView g) =>
            g.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Visible && !r.IsNewRow)?.Index ?? -1;
        /// <summary>Nhấp (hoặc nhấp đúp) vào ô đầu tiên của dòng như người dùng: đặt ô hiện hành + phát các sự kiện chuột của lưới.</summary>
        private static void NhapLuoi(DataGridView g, int dong, bool nhapDup)
        {
            if (dong < 0 || dong >= g.Rows.Count) return;
            var cot = g.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).OrderBy(c => c.DisplayIndex).FirstOrDefault();
            if (cot == null) return;
            try { g.CurrentCell = g[cot.Index, dong]; } catch { }
            try { g.ClearSelection(); g.Rows[dong].Selected = true; } catch { }
            var ea = new DataGridViewCellEventArgs(cot.Index, dong);
            var me = new DataGridViewCellMouseEventArgs(cot.Index, dong, 5, 5, new MouseEventArgs(MouseButtons.Left, nhapDup ? 2 : 1, 5, 5, 0));
            GoiBaoVe(g, "OnCellMouseDown", me);
            GoiBaoVe(g, "OnCellClick", ea);
            GoiBaoVe(g, "OnCellContentClick", ea);
            GoiBaoVe(g, "OnCellMouseClick", me);
            if (nhapDup)
            {
                GoiBaoVe(g, "OnCellDoubleClick", ea);
                GoiBaoVe(g, "OnCellContentDoubleClick", ea);
                GoiBaoVe(g, "OnCellMouseDoubleClick", me);
                GoiBaoVe(g, "OnDoubleClick", EventArgs.Empty);
            }
            GoiBaoVe(g, "OnCellMouseUp", me);
        }
        private static void GoiBaoVe(object doiTuong, string ten, object thamSo)
        {
            var m = doiTuong.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(x => x.Name == ten && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType.IsInstanceOfType(thamSo));
            m?.Invoke(doiTuong, new[] { thamSo });
        }
        /// <summary>
        /// Chọn dòng có chứa "UITest" (bản ghi do công cụ tạo) trên mọi lưới đang hiện. Không có thì chọn dòng đầu nếu
        /// choPhepDongDau. Trả về true nếu chọn được dòng UITest.
        /// </summary>
        private static bool ChonDongLuoi(Form f, bool choPhepDongDau)
        {
            bool thay = false;
            foreach (var g in DuyetControl(f).OfType<DataGridView>().Where(x => x.Visible && x.Enabled))
            {
                try
                {
                    int dong = -1;
                    foreach (DataGridViewRow r in g.Rows)
                    {
                        if (!r.Visible || r.IsNewRow) continue;
                        if (r.Cells.Cast<DataGridViewCell>().Any(c => (c.Value?.ToString() ?? "").Contains(DAU_HIEU, StringComparison.OrdinalIgnoreCase)))
                        { dong = r.Index; break; }
                    }
                    // Dòng đó đang được chọn sẵn thì không nhấp lại (nhiều lưới bật hộp thông báo mỗi lần nhấp dòng)
                    if (dong >= 0) { thay = true; if (g.CurrentCell?.RowIndex != dong) NhapLuoi(g, dong, false); }
                    else if (choPhepDongDau && g.CurrentCell == null) NhapLuoi(g, ChiSoDongDau(g), false);
                }
                catch { }
            }
            return thay;
        }
        private static void BoChonLuoi(Form f)
        {
            foreach (var g in DuyetControl(f).OfType<DataGridView>())
            {
                try { g.ClearSelection(); g.CurrentCell = null; } catch { }
            }
        }
        /// <summary>
        /// Điền dữ liệu mẫu như người dùng gõ: ô chữ (đoán theo tên: năm, tháng, ngày, số, điện thoại...), ComboBox chưa chọn
        /// => mục đầu, danh sách tích chưa tích => tích mục đầu. ghiDe = true: ghi đè cả ô đã có chữ (dùng trước nút Thêm).
        /// Bỏ qua ô chỉ đọc, ô tìm kiếm/lọc, ô mật khẩu (điền mật khẩu cấu hình).
        /// </summary>
        private static void DienDuLieu(Form f, bool ghiDe)
        {
            foreach (var c in DuyetControl(f).ToList())
            {
                try
                {
                    if (!c.Visible || !c.Enabled) continue;
                    string ten = (c.Name ?? "").ToLowerInvariant();
                    var pt = c.GetType();
                    if (c is CheckedListBox clb)
                    {
                        if (clb.CheckedItems.Count == 0 && clb.Items.Count > 0) clb.SetItemChecked(0, true);
                        continue;
                    }
                    if (LayDanhSachCombo(c, out int soMuc, out int dangChon))
                    {
                        if (dangChon < 0 && soMuc > 0) DatChiSoCombo(c, 0);
                        continue;
                    }
                    if (!pt.Name.Contains("TextBox") || pt.Name.Contains("RichText")) continue;
                    if (c.Parent != null && c.Parent.GetType().Name.Contains("TextBox")) continue;   // ô con bên trong KryptonTextBox
                    if (pt.GetProperty("ReadOnly")?.GetValue(c) is true) continue;
                    if (ten.Contains("timkiem") || ten.Contains("search") || ten.Contains("loc") || ten.Contains("filter") || ten.Contains("tim"))
                        continue;
                    bool laMk = pt.GetProperty("UseSystemPasswordChar")?.GetValue(c) is true
                                || (pt.GetProperty("PasswordChar")?.GetValue(c) is char ch && ch != '\0')
                                || ten.Contains("matkhau") || ten.Contains("pass");
                    if (!ghiDe && !string.IsNullOrWhiteSpace(c.Text)) continue;
                    if (laMk) { if (string.IsNullOrEmpty(c.Text)) c.Text = _ch.MatKhau; continue; }
                    c.Text = GiaTriMau(ten);
                }
                catch { }
            }
        }
        private static string GiaTriMau(string ten)
        {
            var now = DateTime.Now;
            if (ten.Contains("email") || ten.Contains("mail")) return "uitest@example.com";
            if (ten.Contains("sdt") || ten.Contains("dienthoai") || ten.Contains("phone")) return "0900000000";
            if (ten.Contains("ngay") || ten.Contains("date")) return now.ToString("dd/MM/yyyy");
            if (ten.Contains("thang") || ten.Contains("month")) return now.Month.ToString();
            if (ten.Contains("nam") || ten.Contains("year")) return now.Year.ToString();
            if (ten.Contains("sohieu") || ten.Contains("maso") || ten.Contains("_ma") || ten.Contains("code")) return "99" + now.ToString("HHmmss");
            if (ten.Contains("tyle") || ten.Contains("phantram") || ten.Contains("diem") || ten.Contains("soluong") || ten.Contains("tien")
                || ten.Contains("number") || ten.Contains("_so") || ten.Contains("sl")) return "1";
            return DAU_HIEU;
        }
        /// <summary>ComboBox của WinForms hoặc Krypton (đọc Items / SelectedIndex qua reflection).</summary>
        private static bool LayDanhSachCombo(Control c, out int soMuc, out int dangChon)
        {
            soMuc = 0; dangChon = -1;
            if (c is ComboBox cb) { soMuc = cb.Items.Count; dangChon = cb.SelectedIndex; return true; }
            if (!c.GetType().Name.Contains("ComboBox")) return false;
            var pt = c.GetType();
            if (pt.GetProperty("Items")?.GetValue(c) is not System.Collections.ICollection items) return false;
            if (pt.GetProperty("SelectedIndex")?.GetValue(c) is not int sel) return false;
            soMuc = items.Count; dangChon = sel;
            return true;
        }
        private static void DatChiSoCombo(Control c, int i)
        {
            if (c.IsDisposed) return;
            if (c is ComboBox cb) { if (i < cb.Items.Count) cb.SelectedIndex = i; return; }
            var p = c.GetType().GetProperty("SelectedIndex");
            if (LayDanhSachCombo(c, out int soMuc, out _) && i < soMuc) p?.SetValue(c, i);
        }
        private static string ChuMucCombo(Control c, int i)
        {
            try
            {
                var items = c.GetType().GetProperty("Items")?.GetValue(c) as System.Collections.IList;
                object? muc = items != null && i < items.Count ? items[i] : null;
                string s = (c is ComboBox cb ? cb.GetItemText(muc) : c.GetType().GetMethod("GetItemText")?.Invoke(c, new[] { muc }) as string)
                           ?? muc?.ToString() ?? "";
                s = s.Replace("\r", " ").Replace("\n", " ").Trim();
                return s.Length > 40 ? s[..40] + "…" : s;
            }
            catch { return "#" + i; }
        }
        /// <summary>Ô mật khẩu đang trống trong hộp thoại (vd. xác minh Admin) => điền mật khẩu cấu hình.</summary>
        private static void DienMatKhau(Form f)
        {
            foreach (var c in DuyetControl(f))
            {
                if (!LaONhapLieu(c) || !c.GetType().Name.Contains("TextBox") || !string.IsNullOrEmpty(c.Text)) continue;
                var pt = c.GetType();
                bool laMk = pt.GetProperty("UseSystemPasswordChar")?.GetValue(c) is true
                            || (pt.GetProperty("PasswordChar")?.GetValue(c) is char ch && ch != '\0')
                            || c.Name.Contains("MatKhau", StringComparison.OrdinalIgnoreCase)
                            || c.Name.Contains("Pass", StringComparison.OrdinalIgnoreCase);
                if (laMk) { try { c.Text = _ch.MatKhau; } catch { } continue; }
                // Ô tên đăng nhập trống (vd Form24 Xác minh Admin): bản cũ chỉ điền mật khẩu => xác minh luôn sai,
                // Form24 đếm lần sai CHUNG (tĩnh) và sai 3 lần là phần mềm tự tắt.
                string ten = c.Name.ToLowerInvariant();
                if (ten.Contains("tendangnhap") || ten.Contains("taikhoan") || ten.Contains("username") || ten.Contains("user"))
                    try { c.Text = _ch.TenDangNhap; } catch { }
            }
        }
        private static void DongForm(Form f)
        {
            if (f.IsDisposed) return;
            try
            {
                if (f.Modal) f.DialogResult = DialogResult.Cancel;
                else f.Close();
            }
            catch (Exception ex) { BaoCao.SuKien(KetQua.CanhBao, $"Đóng {f.GetType().Name} lỗi: {MoTa(ex)}"); }
        }
        // =============================================================== CHỜ
        /// <summary>Chỉ form TRONG CÙNG được bấm; có hộp thoại hệ thống đang mở thì chờ nó đóng.</summary>
        private static async Task<bool> ChoDenLuotAsync(Form form)
        {
            var han = DateTime.Now.AddSeconds(Math.Max(120, _ch.ChoXuLyToiDaGiay * 2));
            while (DateTime.Now < han)
            {
                if (!ConDung(form)) return false;
                if (_nganXep.Count > 0 && _nganXep[^1] == form && !HopThoaiHeThong.DangMo) return true;
                await Task.Delay(200);
            }
            BaoCao.SuKien(KetQua.Treo, $"{form.GetType().Name}: chờ form/hộp thoại phía trên đóng quá lâu => dừng bấm form này");
            return false;
        }
        /// <summary>Chờ phần mềm xử lý xong: hết con trỏ chờ, hết form "Đang xử lý", hết hộp thoại hệ thống.</summary>
        private static async Task ChoXuLyXongAsync()
        {
            await Task.Delay(_ch.ChoSauMoiLanBamMs);
            var han = DateTime.Now.AddSeconds(_ch.ChoXuLyToiDaGiay);
            while (DateTime.Now < han)
            {
                // KHÔNG dùng Cursor.Current: con trỏ chờ chỉ tự trả về khi chuột di chuyển => bị kẹt WaitCursor mãi
                bool ban = HopThoaiHeThong.DangMo
                           || CacForm().Any(f => f.Visible && (f.UseWaitCursor || f.Cursor == Cursors.WaitCursor || LaFormPhu(f) && f.GetType().Name.Contains("Loading")));
                if (!ban) return;
                await Task.Delay(250);
            }
            BaoCao.SuKien(KetQua.Treo, $"Sau {_ch.ChoXuLyToiDaGiay} giây phần mềm vẫn đang bận");
        }
        // =============================================================== TIỆN ÍCH
        private static List<Form> CacForm()
        {
            try { return Application.OpenForms.Cast<Form>().Where(f => !f.IsDisposed).ToList(); }
            catch { return new List<Form>(); }
        }
        private static bool ConDung(Form f) => !f.IsDisposed && f.IsHandleCreated && f.Visible;
        /// <summary>Form không cần bấm thử: đăng nhập, màn chờ, form đang bị bỏ qua.</summary>
        private static bool LaFormPhu(Form f)
        {
            string n = f.GetType().Name;
            return n == "Form1" || n.Contains("Loading") || n == "FormWelcome" || n == "Form2_FormCha";
        }
        private static IEnumerable<Control> DuyetControl(Control goc)
        {
            foreach (Control c in goc.Controls)
            {
                yield return c;
                foreach (var con in DuyetControl(c)) yield return con;
            }
        }
        private static Control? TimControl(Control goc, string ten) =>
            goc.Controls.Find(ten, true).FirstOrDefault();
        private static string LayChu(Control c)
        {
            try
            {
                string t = c.Text ?? "";
                if (string.IsNullOrWhiteSpace(t))
                {
                    // Krypton: chữ nằm ở Values.Text
                    var values = c.GetType().GetProperty("Values")?.GetValue(c);
                    t = values?.GetType().GetProperty("Text")?.GetValue(values) as string ?? "";
                }
                if (string.IsNullOrWhiteSpace(t)) t = c.AccessibleName ?? "";
                return t.Replace("&", "").Replace("\r", " ").Replace("\n", " ").Trim();
            }
            catch { return ""; }
        }
        private static string MoTa(Exception ex)
        {
            var dong = (ex.StackTrace ?? "").Split('\n').Select(s => s.Trim()).Where(s => s.Contains("PhanMemThiDua2026") && !s.Contains(".UITest.")).Take(3);
            return $"{ex.GetType().Name}: {ex.Message}" + (dong.Any() ? " | " + string.Join(" | ", dong) : "");
        }
    }
}
