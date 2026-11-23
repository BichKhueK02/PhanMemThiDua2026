using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Reflection;
using System.Text;
using Krypton.Toolkit;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Biểu tượng NÚT của mọi form (KryptonButton, mục menu, nhãn thanh trạng thái) — gọi 1 dòng ngay sau InitializeComponent():
    ///     Module_BieuTuongNut.ApDung(this);
    /// Ảnh chèn cũ đã được tách khỏi Designer/.resx sang Icon\CoDien\&lt;Lớp form&gt;__&lt;tên control&gt;.&lt;đuôi&gt; (nhúng vào exe,
    /// tên tài nguyên "CoDien.&lt;tệp&gt;") => dựng form KHÔNG còn giải mã ảnh nào cho nút.
    ///  - "Hiện đại": VẼ biểu tượng bằng GDI+ (nét mảnh kiểu web, có màu theo chức năng), chọn theo chữ / tên của nút
    ///    (Lưu, Xóa, Xuất, Nhập, Làm mới, Tìm kiếm...). Nút nền đậm => biểu tượng trắng (xét lại lúc Load khi đã có màu nền).
    ///  - "Cổ điển": nạp lại đúng ảnh gốc.
    /// Không đụng PictureBox (logo, hình minh họa). Mọi lỗi đều bỏ qua: nút chỉ mất biểu tượng, chức năng không đổi.
    /// </summary>
    internal static class Module_BieuTuongNut
    {
        private const string TienTo = "CoDien.";
        private static Dictionary<string, List<(string Control, string TaiNguyen)>>? _theoLop;
        private static readonly object _khoa = new();
        private const BindingFlags BF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        // Form có module giao diện web đặt kích thước bằng px cố định, KHÔNG tự nhân DPI => Module_TyLeDpi hiệu chỉnh lúc Load
        private static readonly HashSet<string> FormCanHieuChinhDpi = new(StringComparer.Ordinal)
        {
            "Form10_NhatKy", "Form15_ThongKeThiDua", "Form20_DonVi", "Form21_ChucVu", "Form34_ThongKeKhenThuong",
            "Form42_QuanLyThiDuaBaNhat", "Form44_SoVangBaNhat", "Form47_DonViTrucThuoc", "Form57_CapNhatTinhThanhPho",
            "Form59_QuanLyThiDuaTapTheNamCu"
        };
        public static void ApDung(Form f)
        {
            try
            {
                string lop = f.GetType().Name;
                // Chụp kích thước NGAY sau InitializeComponent (đã được Windows co giãn theo DPI) - trước khi module giao diện đặt lại
                var chupDpi = FormCanHieuChinhDpi.Contains(lop) ? Module_TyLeDpi.Chup(f) : null;
                bool coDien;
                try { coDien = Module_KieuGiaoDien.LaCoDien(Module_KieuGiaoDien.DangApDung ?? Module_KieuGiaoDien.DocChiDoc()); }
                catch { coDien = false; }
                float k = Math.Max(1f, f.DeviceDpi / 96f);
                var daXuLy = new HashSet<object>(ReferenceEqualityComparer.Instance);
                var nutCanXetLai = new List<(KryptonButton Nut, string Loai)>();
                foreach (var (ten, taiNguyen) in DanhSachCua(lop))
                {
                    try
                    {
                        object? o = f.GetType().GetField(ten, BF)?.GetValue(f);
                        if (o == null) continue;
                        daXuLy.Add(o);
                        if (coDien)
                        {
                            var anh = NapAnh(taiNguyen);
                            if (anh != null) GanAnh(o, anh);
                            continue;
                        }
                        if (o is PictureBox pb)
                        {
                            if (BangHinh.TryGetValue(lop + "." + ten, out var lh) && lh == "GOC")
                            {
                                var goc = NapAnh(taiNguyen);   // ảnh có ý nghĩa dữ liệu: dùng ảnh gốc ở cả 2 kiểu
                                if (goc != null) pb.Image = goc;
                            }
                            else GanHinhMinhHoa(pb, lop, ten);
                            continue;
                        }
                        string loai = ChonLoai(o, ten);
                        GanAnh(o, VeBieuTuong(loai, CoAnh(o, k), MauCua(loai)));
                        if (o is KryptonButton kb) nutCanXetLai.Add((kb, loai));
                    }
                    catch (Exception ex) { Debug.WriteLine("[BieuTuongNut] " + lop + "." + ten + ": " + ex.Message); }
                }
                // Lúc Load (module giao diện web đã tô màu + đặt kích thước, bố cục đã có kích thước thật):
                //  1) hiệu chỉnh DPI cho các giá trị px cố định; 2) Hiện đại: thêm biểu tượng cho nút chưa có ảnh (nếu đủ chỗ);
                //  3) nút nền đậm => biểu tượng trắng; biểu tượng co theo chiều cao nút thật.
                void KhiLoad(object? s, EventArgs e)
                {
                    f.Load -= KhiLoad;
                    try { if (chupDpi != null) Module_TyLeDpi.HieuChinh(f, chupDpi); }
                    catch (Exception ex) { Debug.WriteLine("[BieuTuongNut] DPI " + lop + ": " + ex.Message); }
                    if (coDien) return;
                    try { ThemBieuTuongChoNutChuaCo(f, daXuLy, nutCanXetLai, k); }
                    catch (Exception ex) { Debug.WriteLine("[BieuTuongNut] thêm " + lop + ": " + ex.Message); }
                    foreach (var (nut, loai) in nutCanXetLai)
                    {
                        try
                        {
                            if (nut.IsDisposed) continue;
                            GanAnh(nut, VeBieuTuong(loai, CoAnh(nut, k), LaNenDam(nut) ? Color.White : MauCua(loai)));
                        }
                        catch { }
                    }
                }
                f.Load += KhiLoad;
            }
            catch (Exception ex) { Debug.WriteLine("[BieuTuongNut] " + ex.Message); }
        }
        /// <summary>
        /// Nút KHÔNG có ảnh từ trước: gán biểu tượng vẽ nếu nhận ra được chức năng VÀ còn đủ chỗ cho cả biểu tượng lẫn chữ
        /// (không làm chữ bị cắt). Nút đã có ảnh do code gán lúc chạy thì để nguyên.
        /// </summary>
        private static void ThemBieuTuongChoNutChuaCo(Form f, HashSet<object> daXuLy, List<(KryptonButton, string)> ketQua, float k)
        {
            foreach (var b in TatCaNut(f))
            {
                // (Không xét Visible: lúc Load form chưa hiện nên mọi control đều Visible = false)
                if (daXuLy.Contains(b) || b.Values.Image != null) continue;
                string chu = (b.Values.Text ?? "").Trim();
                if (chu.Length == 0) continue;
                string loai = ChonLoai(b, b.Name);
                if (loai is "Diem" or "ThongTin") continue;       // không rõ chức năng => không thêm
                int co = CoAnh(b, k);
                var font = b.StateCommon.Content.ShortText.Font ?? b.Font;
                int rongChu = TextRenderer.MeasureText(chu, font).Width;
                int canRong = rongChu + co + (int)Math.Round(26 * k);   // chữ + biểu tượng + khoảng cách + lề hai bên
                if (b.Width < canRong || b.Height < co + (int)Math.Round(6 * k)) continue;
                GanAnh(b, VeBieuTuong(loai, co, MauCua(loai)));
                ketQua.Add((b, loai));
            }
        }
        private static IEnumerable<KryptonButton> TatCaNut(Control goc)
        {
            foreach (Control c in goc.Controls)
            {
                if (c is KryptonButton kb) yield return kb;
                if (c.HasChildren) foreach (var con in TatCaNut(c)) yield return con;
            }
        }
        // ---------------- Danh sách ảnh đã tách theo lớp form ----------------
        private static List<(string, string)> DanhSachCua(string lop)
        {
            lock (_khoa)
            {
                if (_theoLop == null)
                {
                    _theoLop = new Dictionary<string, List<(string, string)>>(StringComparer.Ordinal);
                    foreach (var tn in typeof(Module_BieuTuongNut).Assembly.GetManifestResourceNames())
                    {
                        if (!tn.StartsWith(TienTo, StringComparison.Ordinal)) continue;
                        string tep = tn.Substring(TienTo.Length);          // Lop__control.ext
                        int vt = tep.IndexOf("__", StringComparison.Ordinal);
                        int cham = tep.LastIndexOf('.');
                        if (vt <= 0 || cham <= vt) continue;
                        string l = tep.Substring(0, vt), ctl = tep.Substring(vt + 2, cham - vt - 2);
                        if (!_theoLop.TryGetValue(l, out var ds)) _theoLop[l] = ds = new();
                        ds.Add((ctl, tn));
                    }
                }
                return _theoLop.TryGetValue(lop, out var kq) ? kq : new();
            }
        }
        private static Image? NapAnh(string taiNguyen)
        {
            using var luong = typeof(Module_BieuTuongNut).Assembly.GetManifestResourceStream(taiNguyen);
            if (luong == null) return null;
            var ms = new MemoryStream();          // giữ luồng suốt đời ảnh (GIF động cần)
            luong.CopyTo(ms);
            ms.Position = 0;
            if (taiNguyen.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                using (var ic = new Icon(ms)) return ic.ToBitmap();
            return Image.FromStream(ms);
        }
        private static void GanAnh(object o, Image anh)
        {
            switch (o)
            {
                case KryptonButton kb: kb.Values.Image = anh; break;
                case ToolStripItem it: it.Image = anh; break;
                case ButtonBase b: b.Image = anh; break;
                case PictureBox pb: pb.Image = anh; break;
            }
        }
        private static int CoAnh(object o, float k)
        {
            if (o is ToolStripItem it)
            {
                int h = it.Owner?.ImageScalingSize.Height ?? 0;
                if (it is ToolStripStatusLabel || h <= 0) h = (int)Math.Round(16 * k);
                return Math.Max(14, h);
            }
            int co = (int)Math.Round(18 * k);
            // Biểu tượng co theo chiều cao THẬT của nút (nút thấp => biểu tượng nhỏ lại, không đè lên chữ / bị cắt)
            if (o is Control c && c.Height > 0) co = Math.Min(co, c.Height - (int)Math.Round(10 * k));
            return Math.Max(12, co);
        }
        private static bool LaNenDam(KryptonButton b)
        {
            Color c = b.StateCommon.Back.Color1;
            if (c.IsEmpty || c.A < 128) c = b.StateNormal.Back.Color1;
            if (c.IsEmpty || c.A < 128) return false;
            return (c.R * 299 + c.G * 587 + c.B * 114) / 1000 < 140;
        }
        // ---------------- Chọn loại biểu tượng theo chữ / tên ----------------
        private static string ChonLoai(object o, string ten)
        {
            string chu = o switch
            {
                KryptonButton kb => kb.Values.Text ?? "",
                ToolStripItem it => it.Text ?? "",
                Control c => c.Text ?? "",
                _ => ""
            };
            string s = " " + BoDau(chu) + " " + BoDau(TachTen(ten)) + " ";
            bool Co(params string[] tu) => tu.Any(t => s.Contains(t, StringComparison.Ordinal));
            if (Co("xoa tim kiem", "lam moi cac o tim kiem")) return "XoaLoc";
            if (Co("sao luu")) return "SaoLuu";
            if (Co("khoi phuc")) return "KhoiPhuc";
            if (Co("dong bo")) return "DongBo";
            if (Co("lam moi", "refresh", "refersh", "dat lai", "reset")) return "LamMoi";   // trước "lọc": "Làm mới bộ lọc"
            if (Co("tim kiem", "quet tim", " loc ")) return "KinhLup";
            if (Co("xoa")) return "ThungRac";
            if (Co("pdf")) return "Pdf";
            if (Co("xuat", "tao tep")) return "XuatTep";
            if (Co("nhap")) return "NhapTep";
            if (Co("them anh")) return "Anh";
            if (Co("tang co")) return "ChuTo";
            if (Co("giam co")) return "ChuNho";
            if (Co(" luu", "dang ky")) return "Luu";
            if (Co("them")) return "Cong";
            if (Co(" sua", "cap nhat")) return "ButChi";
            if (Co("di chuyen len")) return "MuiTenLen";
            if (Co("di chuyen xuong")) return "MuiTenXuong";
            if (Co("tiep theo")) return "MuiTenPhai";
            if (Co("tro lai", "tro ve", "quay lai")) return "MuiTenTrai";
            if (Co(" dong ", " dong", "thoat")) return "DongX";
            if (Co("huong dan", "chi tiet")) return "HoiCham";
            if (Co("kiem tra", "xac thuc", " ok ")) return "KiemTra";
            if (Co("may tinh", "tinh phan loai", "tinh toan")) return "MayTinh";
            if (Co("ty le")) return "BieuDoTron";
            if (Co("phan tich", "thong ke", "bao cao", "xem ket qua")) return "BieuDoCot";
            if (Co("khen thuong", "thanh tich", "so vang", "ba nhat", "thi dua")) return "HuyChuong";
            if (Co("tai khoan")) return "NguoiDung";
            if (Co("tap the", "cbcs")) return "NhomNguoi";
            if (Co("csdl", "vacuum", "cau hinh", "ket noi", "tai nguyen")) return "CSDL";
            if (Co("thu muc")) return "ThuMuc";
            if (Co("to trinh")) return "TepTin";
            if (Co("thoi gian")) return "DongHo";
            if (o is ToolStripItem) return "ThongTin";     // nhãn thanh trạng thái / menu không rõ chức năng
            return "Diem";
        }
        private static string TachTen(string ten)
        {
            // "kryptonButton1_XoaTatCa" -> "xoa tat ca"; bỏ tiền tố kiểu control
            var sb = new StringBuilder(" ");
            foreach (char c in ten.Replace("_", " "))
            {
                if (char.IsUpper(c)) sb.Append(' ');
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString().Replace("krypton button", " ").Replace("tool strip menu item", " ")
                     .Replace("tool strip status label", " ").Replace("tool strip label", " ");
        }
        private static string BoDau(string s)
        {
            var d = s.ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(d.Length);
            foreach (char c in d)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
            return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), " +", " ");
        }
        // ---------------- Màu theo chức năng ----------------
        private static Color MauCua(string loai) => loai switch
        {
            "Luu" or "KinhLup" or "NguoiDung" or "ThongTin" => Color.FromArgb(37, 99, 235),
            "SaoLuu" or "XuatTep" or "Cong" or "KiemTra" => Color.FromArgb(22, 163, 74),
            "KhoiPhuc" or "BieuDoCot" => Color.FromArgb(234, 88, 12),
            "ThungRac" or "Pdf" => Color.FromArgb(220, 38, 38),
            "XoaLoc" => Color.FromArgb(225, 29, 72),
            "LamMoi" or "TepTin" => Color.FromArgb(13, 148, 136),
            "DongBo" or "CSDL" => Color.FromArgb(8, 145, 178),
            "NhapTep" => Color.FromArgb(79, 70, 229),
            "ButChi" or "ThuMuc" => Color.FromArgb(217, 119, 6),
            "HuyChuong" => Color.FromArgb(202, 138, 4),
            "HoiCham" or "NhomNguoi" or "Diem" => Color.FromArgb(124, 58, 237),
            "Anh" => Color.FromArgb(219, 39, 119),
            "DongX" => Color.FromArgb(100, 116, 139),
            "ManHinh" or "Goi" => Color.FromArgb(79, 70, 229),
            "ChiaKhoa" => Color.FromArgb(217, 119, 6),
            "Nha" => Color.FromArgb(37, 99, 235),
            "MayChu" => Color.FromArgb(8, 145, 178),
            _ => Color.FromArgb(71, 85, 105),
        };
        // ---------------- Hình minh họa (thay ảnh nặng trong PictureBox) ----------------
        // "Lop.control" -> loại hình. "" = để trống (module giao diện của form vốn bỏ ảnh đó). Tiền tố "Bang:" = dải chữ.
        private static readonly Dictionary<string, string> BangHinh = new(StringComparer.Ordinal)
        {
            ["Form10_NhatKy.pictureBox1"] = "ManHinh",
            ["Form12.pictureBox9"] = "ThongTin",
            ["Form16_LamMoi.pictureBox1"] = "LamMoi",
            ["Form17_XuatDataSangTK.pictureBox1"] = "",
            ["Form18_XoaDataThang.pictureBox1"] = "ThungRac",
            ["Form24_XacMinhAdmin.PictureBox1"] = "NguoiDung",
            ["Form25_ThongKeTaiKhoanDaSuDung.pictureBox1"] = "ChiaKhoa",
            ["Form26_PhanTichQuanSo.pictureBox1"] = "Nha",
            ["Form29_CBCSTrongDienQuanLy.pictureBox3"] = "NguoiDung",
            ["Form3_DangKyTaiKhoan.PictureBox1"] = "HoiCham",
            ["Form3_DangKyTaiKhoan.pictureBox2"] = "Bang:Đăng ký tài khoản mới theo mẫu dưới đây",
            // GIỮ ẢNH GỐC: Form3 lưu chính ảnh đang hiện vào CSDL làm ảnh đại diện mặc định (LuuAnhDaiDienVaoCSDL) => không được đổi
            ["Form3_DangKyTaiKhoan.pictureBox2_AnhDaiDienAdmin"] = "GOC",
            ["Form3_DangKyTaiKhoan.pictureBox2_ThongTin_DKTK"] = "ThongTin",
            ["Form33_KiemTraSucKhoeCSDL.pictureBox2"] = "NguoiDung",
            ["Form33_KiemTraSucKhoeCSDL.pictureBox3"] = "DongBo",
            ["Form36_TinhPhanLoaiThang.pictureBox3"] = "NguoiDung",
            ["Form37_XoaThongKeNangCao.pictureBox2"] = "ThungRac",
            ["Form38_XuatExcelTinhToan.pictureBox1"] = "MayChu",
            ["Form39_ThongTinNguoiDung.pictureBox2_AnhDaiDienAdmin"] = "NguoiDung",
            ["Form4_TrangDauTien.pictureBox2"] = "ThuMuc",
            ["Form43_TomTatThanhTichBaNhat.pictureBox1"] = "DiaCau",
            ["Form56_TomTatThanhTichTapTheHangThang.pictureBox1"] = "DiaCau",
            ["Form46_ThongKeThiDuaNamCu.pictureBox1"] = "ManHinh",
            ["Form48_XuatTepPdf.pictureBox2"] = "ThuMuc",
            ["Form49_QuanLyKhenThuongTapThe.pictureBox1"] = "ManHinh",
            ["Form5_QuenPass.PictureBox2"] = "NguoiDung",
            ["Form50_QuanLyKhenThuongNamCu.pictureBox1"] = "CSDL",
            ["Form51_QuanLyKhenThuongTapTheNamCu.pictureBox1"] = "CSDL",
            ["Form58_DongBoKetQuaThiDuaNamTruocVeNamHienTai.pictureBox1"] = "CSDL",
            ["Form59_QuanLyThiDuaTapTheNamCu.pictureBox1"] = "CSDL",
            ["Form7_ThongTinAdmin.PictureBox1"] = "Goi",
            ["Form7_ThongTinAdmin.pictureBox2"] = "HuyChuong",
            ["Form7_ThongTinAdmin.pictureBox3"] = "HoiCham",
            ["Form7_ThongTinAdmin.pictureBox4"] = "HoiCham",
            ["Form9_GoCaiDat.PictureBox3"] = "Goi",
        };
        private static void GanHinhMinhHoa(PictureBox pb, string lop, string ten)
        {
            if (!BangHinh.TryGetValue(lop + "." + ten, out var loai) || loai.Length == 0) return;
            if (loai.StartsWith("Bang:", StringComparison.Ordinal))
            {
                pb.SizeMode = PictureBoxSizeMode.StretchImage;
                pb.Image = VeBang(loai.Substring(5), Math.Max(40, pb.Width), Math.Max(16, pb.Height), pb.DeviceDpi / 96f);
                return;
            }
            pb.SizeMode = PictureBoxSizeMode.Zoom;
            pb.Image = VeMinhHoa(loai, Math.Max(24, Math.Min(pb.Width, pb.Height)));
        }
        /// <summary>Hình minh họa kiểu "app icon": ô bo tròn màu chuyển sắc + hình trắng ở giữa + ánh sáng nhẹ.</summary>
        internal static Bitmap VeMinhHoa(string loai, int coPx)
        {
            coPx = Math.Max(24, coPx);
            var bmp = new Bitmap(coPx, coPx, PixelFormat.Format32bppPArgb);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);
            Color mau = loai == "DiaCau" ? Color.FromArgb(14, 165, 233) : MauCua(loai);
            Color sang = Color.FromArgb(Math.Min(255, mau.R + 55), Math.Min(255, mau.G + 55), Math.Min(255, mau.B + 55));
            float le = coPx * 0.06f;
            var o = new RectangleF(le, le, coPx - 2 * le, coPx - 2 * le);
            using (var p = BoGoc(o, o.Width * 0.24f))
            {
                using (var to = new LinearGradientBrush(o, sang, mau, 60f)) g.FillPath(to, p);
                var st = g.Save();
                g.SetClip(p);
                using (var bong = new SolidBrush(Color.FromArgb(38, Color.White)))
                    g.FillEllipse(bong, o.X - o.Width * 0.3f, o.Y - o.Height * 0.55f, o.Width * 1.2f, o.Height * 0.95f);
                g.Restore(st);
            }
            int coHinh = (int)Math.Round(coPx * 0.6f);
            using (var hinh = VeBieuTuong(loai, coHinh, Color.White))
                g.DrawImage(hinh, (coPx - coHinh) / 2f, (coPx - coHinh) / 2f, coHinh, coHinh);
            return bmp;
        }
        /// <summary>Dải tiêu đề chữ (thay ảnh chữ chụp sẵn): nền xanh nhạt bo tròn, chữ đậm căn giữa.</summary>
        internal static Bitmap VeBang(string chu, int w, int h, float k)
        {
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            var o = new RectangleF(0.5f, 0.5f, w - 1f, h - 1f);
            using (var p = BoGoc(o, Math.Min(10f * k, h / 2f)))
            using (var to = new LinearGradientBrush(o, Color.FromArgb(239, 246, 255), Color.FromArgb(224, 231, 255), 0f))
            using (var vien = new Pen(Color.FromArgb(199, 210, 254)))
            {
                g.FillPath(to, p);
                g.DrawPath(vien, p);
            }
            float coChu = Math.Max(10f, Math.Min(h * 0.36f, 17f * k));
            using var font = new Font("Segoe UI Semibold", coChu, FontStyle.Regular, GraphicsUnit.Pixel);
            // Nền ảnh trong suốt: KHÔNG dùng TextRenderer/ClearType (viền chữ bị nhòe, đậm) => khử răng cưa thường
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            using var bc = new SolidBrush(Color.FromArgb(55, 48, 163));
            g.DrawString(chu, font, bc, o, sf);
            return bmp;
        }
        // ---------------- Vẽ (khung thiết kế 24x24, nét 1.8) ----------------
        internal static Bitmap VeBieuTuong(string loai, int coPx, Color mau)
        {
            coPx = Math.Max(12, coPx);
            var bmp = new Bitmap(coPx, coPx, PixelFormat.Format32bppPArgb);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);
            g.ScaleTransform(coPx / 24f, coPx / 24f);
            using var but = new Pen(mau, 1.9f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            bool trang = mau.ToArgb() == Color.White.ToArgb();
            switch (loai)
            {
                case "Luu":
                    using (var p = BoGoc(new RectangleF(4f, 4f, 16f, 16f), 2.4f)) g.DrawPath(but, p);
                    g.DrawLines(but, new PointF[] { new(8f, 4f), new(8f, 8.6f), new(15f, 8.6f), new(15f, 4f) });
                    using (var p = BoGoc(new RectangleF(7.6f, 13f, 8.8f, 7f), 1f)) g.DrawPath(but, p);
                    break;
                case "SaoLuu":
                    g.DrawLines(but, new PointF[] { new(4f, 14.5f), new(4f, 20f), new(20f, 20f), new(20f, 14.5f) });
                    g.DrawLine(but, 12f, 15.5f, 12f, 4f);
                    g.DrawLines(but, new PointF[] { new(7.8f, 8.2f), new(12f, 4f), new(16.2f, 8.2f) });
                    break;
                case "NhapTep":
                    g.DrawLines(but, new PointF[] { new(4f, 14.5f), new(4f, 20f), new(20f, 20f), new(20f, 14.5f) });
                    g.DrawLine(but, 12f, 3.5f, 12f, 15f);
                    g.DrawLines(but, new PointF[] { new(7.8f, 10.8f), new(12f, 15f), new(16.2f, 10.8f) });
                    break;
                case "KhoiPhuc":
                    g.DrawArc(but, 4f, 4f, 16f, 16f, 200f, 290f);
                    g.DrawLines(but, new PointF[] { new(3.6f, 5.6f), new(4.6f, 10.2f), new(9.2f, 9.2f) });
                    g.DrawLines(but, new PointF[] { new(12f, 8.2f), new(12f, 12.2f), new(14.8f, 13.8f) });
                    break;
                case "LamMoi":
                    g.DrawArc(but, 4f, 4f, 16f, 16f, -60f, 280f);
                    g.DrawLines(but, new PointF[] { new(20.2f, 3.6f), new(20f, 8.6f), new(15f, 8.2f) });
                    break;
                case "DongBo":
                    g.DrawArc(but, 4f, 4f, 16f, 16f, 200f, 140f);
                    g.DrawLines(but, new PointF[] { new(16.8f, 3.4f), new(19.6f, 7.6f), new(15.2f, 9f) });
                    g.DrawArc(but, 4f, 4f, 16f, 16f, 20f, 140f);
                    g.DrawLines(but, new PointF[] { new(7.2f, 20.6f), new(4.4f, 16.4f), new(8.8f, 15f) });
                    break;
                case "ThungRac":
                    g.DrawLine(but, 4f, 6.5f, 20f, 6.5f);
                    g.DrawLines(but, new PointF[] { new(9.5f, 6.5f), new(9.5f, 3.8f), new(14.5f, 3.8f), new(14.5f, 6.5f) });
                    g.DrawLines(but, new PointF[] { new(6f, 6.5f), new(7f, 20.5f), new(17f, 20.5f), new(18f, 6.5f) });
                    g.DrawLine(but, 10.2f, 10.5f, 10.2f, 16.8f);
                    g.DrawLine(but, 13.8f, 10.5f, 13.8f, 16.8f);
                    break;
                case "KinhLup":
                    g.DrawEllipse(but, 4f, 4f, 11.5f, 11.5f);
                    g.DrawLine(but, 14f, 14f, 20f, 20f);
                    break;
                case "XoaLoc":
                    g.DrawLines(but, new PointF[] { new(3.5f, 4.5f), new(17f, 4.5f), new(11.8f, 11f), new(11.8f, 18.5f), new(8.7f, 16.6f), new(8.7f, 11f), new(3.5f, 4.5f) });
                    g.DrawLine(but, 15.2f, 13.6f, 20.4f, 18.8f);
                    g.DrawLine(but, 20.4f, 13.6f, 15.2f, 18.8f);
                    break;
                case "XuatTep":
                case "Pdf":
                case "TepTin":
                    g.DrawLines(but, new PointF[] { new(13f, 3f), new(5.5f, 3f), new(5.5f, 21f), new(18.5f, 21f), new(18.5f, 8.5f), new(13f, 3f), new(13f, 8.5f), new(18.5f, 8.5f) });
                    if (loai == "XuatTep")
                    {
                        g.DrawLine(but, 9f, 15f, 15f, 15f);
                        g.DrawLines(but, new PointF[] { new(12.5f, 12.5f), new(15f, 15f), new(12.5f, 17.5f) });
                    }
                    else if (loai == "Pdf")
                    {
                        using var nen = new SolidBrush(mau);
                        using (var p = BoGoc(new RectangleF(3f, 12f, 13f, 7f), 1.4f)) g.FillPath(nen, p);
                        using var chu = new Pen(trang ? Color.FromArgb(220, 38, 38) : Color.White, 1.3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                        g.DrawLine(chu, 5.6f, 14f, 13.4f, 14f);
                        g.DrawLine(chu, 5.6f, 16.8f, 10.6f, 16.8f);
                    }
                    else
                    {
                        g.DrawLine(but, 9f, 13f, 15f, 13f);
                        g.DrawLine(but, 9f, 16.5f, 13.5f, 16.5f);
                    }
                    break;
                case "Cong":
                    g.DrawEllipse(but, 3.5f, 3.5f, 17f, 17f);
                    g.DrawLine(but, 12f, 8f, 12f, 16f);
                    g.DrawLine(but, 8f, 12f, 16f, 12f);
                    break;
                case "ButChi":
                    g.DrawLines(but, new PointF[] { new(4.5f, 19.5f), new(5.4f, 15.4f), new(15.6f, 5.2f), new(18.8f, 8.4f), new(8.6f, 18.6f), new(4.5f, 19.5f) });
                    g.DrawLine(but, 13.4f, 7.4f, 16.6f, 10.6f);
                    break;
                case "MuiTenTrai":
                case "MuiTenPhai":
                case "MuiTenLen":
                case "MuiTenXuong":
                    {
                        var st = g.Save();
                        g.TranslateTransform(12f, 12f);
                        g.RotateTransform(loai switch { "MuiTenPhai" => 180f, "MuiTenLen" => 90f, "MuiTenXuong" => -90f, _ => 0f });
                        g.TranslateTransform(-12f, -12f);
                        g.DrawLine(but, 5f, 12f, 19f, 12f);
                        g.DrawLines(but, new PointF[] { new(10.5f, 6.5f), new(5f, 12f), new(10.5f, 17.5f) });
                        g.Restore(st);
                    }
                    break;
                case "DongX":
                    g.DrawEllipse(but, 3.5f, 3.5f, 17f, 17f);
                    g.DrawLine(but, 9f, 9f, 15f, 15f);
                    g.DrawLine(but, 15f, 9f, 9f, 15f);
                    break;
                case "HoiCham":
                    g.DrawEllipse(but, 3.5f, 3.5f, 17f, 17f);
                    g.DrawArc(but, 9.2f, 7f, 5.6f, 5.4f, 190f, 250f);
                    g.DrawLine(but, 12f, 12.4f, 12f, 13.8f);
                    g.DrawLine(but, 12f, 16.6f, 12f, 16.7f);
                    break;
                case "KiemTra":
                    g.DrawEllipse(but, 3.5f, 3.5f, 17f, 17f);
                    g.DrawLines(but, new PointF[] { new(8f, 12.4f), new(10.8f, 15.2f), new(16.2f, 9.2f) });
                    break;
                case "MayTinh":
                    using (var p = BoGoc(new RectangleF(5.5f, 3f, 13f, 18f), 2f)) g.DrawPath(but, p);
                    g.DrawLine(but, 8.5f, 7.5f, 15.5f, 7.5f);
                    foreach (float y in new[] { 12f, 15.2f, 18f })
                        foreach (float x in new[] { 9f, 12f, 15f })
                            g.DrawLine(but, x, y, x + 0.1f, y);
                    break;
                case "BieuDoCot":
                    g.DrawLine(but, 3.8f, 20.2f, 20.2f, 20.2f);
                    using (var p = BoGoc(new RectangleF(5.5f, 12.5f, 3.4f, 7.7f), 1f)) g.DrawPath(but, p);
                    using (var p = BoGoc(new RectangleF(10.3f, 8f, 3.4f, 12.2f), 1f)) g.DrawPath(but, p);
                    using (var p = BoGoc(new RectangleF(15.1f, 4f, 3.4f, 16.2f), 1f)) g.DrawPath(but, p);
                    break;
                case "BieuDoTron":
                    if (trang)
                    {
                        g.DrawEllipse(but, 3f, 3f, 18f, 18f);
                        g.DrawLine(but, 12f, 12f, 12f, 3f);
                        g.DrawLine(but, 12f, 12f, 19.5f, 16.5f);
                        break;
                    }
                    using (var b1 = new SolidBrush(Color.FromArgb(37, 99, 235))) g.FillPie(b1, 3f, 3f, 18f, 18f, -90f, 216f);
                    using (var b2 = new SolidBrush(Color.FromArgb(22, 163, 74))) g.FillPie(b2, 3f, 3f, 18f, 18f, 126f, 108f);
                    using (var b3 = new SolidBrush(Color.FromArgb(245, 158, 11))) g.FillPie(b3, 3f, 3f, 18f, 18f, 234f, 36f);
                    break;
                case "HuyChuong":
                    g.DrawLines(but, new PointF[] { new(8.6f, 13.6f), new(7f, 21f), new(12f, 18.6f), new(17f, 21f), new(15.4f, 13.6f) });
                    g.DrawEllipse(but, 6f, 3f, 12f, 12f);
                    g.DrawPolygon(but, Sao(12f, 9f, 3.1f, 1.35f));
                    break;
                case "NguoiDung":
                    g.DrawEllipse(but, 8.2f, 3.8f, 7.6f, 7.6f);
                    g.DrawArc(but, 4.5f, 13.2f, 15f, 14f, 200f, 140f);
                    break;
                case "NhomNguoi":
                    g.DrawEllipse(but, 5f, 4.5f, 6.4f, 6.4f);
                    g.DrawArc(but, 2.2f, 13.4f, 12f, 11f, 200f, 140f);
                    g.DrawEllipse(but, 14f, 6f, 5f, 5f);
                    g.DrawArc(but, 13f, 13.4f, 9f, 9f, 215f, 110f);
                    break;
                case "CSDL":
                    g.DrawEllipse(but, 5f, 3.5f, 14f, 5f);
                    g.DrawLine(but, 5f, 6f, 5f, 18f);
                    g.DrawLine(but, 19f, 6f, 19f, 18f);
                    g.DrawArc(but, 5f, 9.5f, 14f, 5f, 0f, 180f);
                    g.DrawArc(but, 5f, 15.5f, 14f, 5f, 0f, 180f);
                    break;
                case "ThuMuc":
                    g.DrawLines(but, new PointF[] { new(3f, 18.5f), new(3f, 5.5f), new(9f, 5.5f), new(11f, 8f), new(21f, 8f), new(21f, 18.5f), new(3f, 18.5f) });
                    g.DrawLine(but, 3f, 11f, 21f, 11f);
                    break;
                case "Anh":
                    using (var p = BoGoc(new RectangleF(3f, 4.5f, 18f, 15f), 2.2f)) g.DrawPath(but, p);
                    g.DrawEllipse(but, 6.5f, 7.5f, 3.6f, 3.6f);
                    g.DrawLines(but, new PointF[] { new(3.5f, 17.5f), new(9f, 12.5f), new(13f, 16f), new(16f, 13.2f), new(20.5f, 17.2f) });
                    break;
                case "ChuTo":
                case "ChuNho":
                    using (var f = new Font("Segoe UI Semibold", loai == "ChuTo" ? 13f : 10f, FontStyle.Regular, GraphicsUnit.Pixel))
                    using (var bc = new SolidBrush(mau))
                        g.DrawString("A", f, bc, loai == "ChuTo" ? 2f : 3.5f, loai == "ChuTo" ? 4f : 7f);
                    g.DrawLine(but, 15f, 12f, 21f, 12f);
                    if (loai == "ChuTo") g.DrawLine(but, 18f, 9f, 18f, 15f);
                    break;
                case "DongHo":
                    g.DrawEllipse(but, 3.5f, 3.5f, 17f, 17f);
                    g.DrawLines(but, new PointF[] { new(12f, 7.5f), new(12f, 12f), new(15.2f, 14f) });
                    break;
                case "ThongTin":
                    using (var nen = new SolidBrush(mau)) g.FillEllipse(nen, 3f, 3f, 18f, 18f);
                    using (var chu = new Pen(trang ? Color.FromArgb(37, 99, 235) : Color.White, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        g.DrawLine(chu, 12f, 11f, 12f, 16.6f);
                        g.DrawLine(chu, 12f, 7.6f, 12f, 7.7f);
                    }
                    break;
                case "ChiaKhoa":
                    g.DrawEllipse(but, 3.5f, 9f, 8f, 8f);
                    g.DrawLine(but, 11.5f, 13f, 20.5f, 13f);
                    g.DrawLine(but, 17.5f, 13f, 17.5f, 16.5f);
                    g.DrawLine(but, 20.5f, 13f, 20.5f, 16f);
                    break;
                case "ManHinh":    // màn hình máy tính
                    using (var p = BoGoc(new RectangleF(3f, 4f, 18f, 12.5f), 1.8f)) g.DrawPath(but, p);
                    g.DrawLine(but, 12f, 16.5f, 12f, 19.5f);
                    g.DrawLine(but, 8f, 20f, 16f, 20f);
                    break;
                case "Nha":
                    g.DrawLines(but, new PointF[] { new(3f, 11.5f), new(12f, 3.8f), new(21f, 11.5f) });
                    g.DrawLines(but, new PointF[] { new(5.5f, 9.6f), new(5.5f, 20.2f), new(18.5f, 20.2f), new(18.5f, 9.6f) });
                    g.DrawLines(but, new PointF[] { new(10f, 20.2f), new(10f, 14.5f), new(14f, 14.5f), new(14f, 20.2f) });
                    break;
                case "DiaCau":     // quả địa cầu: vòng tròn + kinh tuyến + vĩ tuyến
                    g.DrawEllipse(but, 3f, 3f, 18f, 18f);
                    g.DrawEllipse(but, 8f, 3f, 8f, 18f);
                    g.DrawLine(but, 3.4f, 12f, 20.6f, 12f);
                    g.DrawArc(but, 4.5f, 2f, 15f, 8f, 20f, 140f);
                    g.DrawArc(but, 4.5f, 14f, 15f, 8f, 200f, 140f);
                    break;
                case "Goi":        // hộp phần mềm
                    g.DrawPolygon(but, new PointF[] { new(12f, 3f), new(20.5f, 7.5f), new(20.5f, 16.5f), new(12f, 21f), new(3.5f, 16.5f), new(3.5f, 7.5f) });
                    g.DrawLines(but, new PointF[] { new(3.5f, 7.5f), new(12f, 12f), new(20.5f, 7.5f) });
                    g.DrawLine(but, 12f, 12f, 12f, 21f);
                    break;
                case "MayChu":     // máy chủ: 3 ngăn xếp chồng + đèn
                    foreach (float y in new[] { 3.5f, 9.5f, 15.5f })
                    {
                        using (var p = BoGoc(new RectangleF(4f, y, 16f, 5f), 1.2f)) g.DrawPath(but, p);
                        g.DrawLine(but, 7f, y + 2.5f, 7.1f, y + 2.5f);
                        g.DrawLine(but, 13f, y + 2.5f, 17f, y + 2.5f);
                    }
                    break;
                default:   // "Diem": viên kim cương nhỏ
                    using (var nen = new SolidBrush(mau))
                        g.FillPolygon(nen, new PointF[] { new(12f, 5f), new(19f, 12f), new(12f, 19f), new(5f, 12f) });
                    break;
            }
            return bmp;
        }
        private static PointF[] Sao(float cx, float cy, float rNgoai, float rTrong)
        {
            var d = new PointF[10];
            for (int i = 0; i < 10; i++)
            {
                double a = -Math.PI / 2 + i * Math.PI / 5;
                float r = i % 2 == 0 ? rNgoai : rTrong;
                d[i] = new PointF(cx + r * (float)Math.Cos(a), cy + r * (float)Math.Sin(a));
            }
            return d;
        }
        private static GraphicsPath BoGoc(RectangleF r, float bk)
        {
            var p = new GraphicsPath();
            float d = bk * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
