using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" + BIỂU ĐỒ TRÒN 3D CHO Form61_TyLeXetThiDuaNam (Khai báo tỷ lệ xét thi đua năm)
    //  CHỈ TRÌNH BÀY, KHÔNG CÓ LOGIC NGHIỆP VỤ.
    //  Cùng ngôn ngữ thiết kế với Form4 (Module_GiaoDienForm4Web): nền trang xám nhạt, thẻ trắng bo góc có tiêu đề,
    //  ô nhập bo góc viền tím khi đang gõ, nút theme tím nhạt.
    //
    //  Quy ước 3 ô nhập (đúng thứ tự trên form):  Loại 1 = CSTĐ,  Loại 2 = CSTT,  Loại 3 = HTNV.
    //  Quy tắc cơ cấu:  Loại 1 NẰM TRONG Loại 2  và  Loại 2 + Loại 3 = 100%.
    //  => biểu đồ chia: [Loại 1] + [phần còn lại của Loại 2 = Loại 2 - Loại 1] + [Loại 3] (+ [Chưa phân bổ] nếu chưa đủ 100%).
    //
    //  - Lớp static độc lập, không tạo .resx, KHÔNG cần sửa .csproj, KHÔNG cần thư viện biểu đồ ngoài:
    //    biểu đồ 3D tự vẽ bằng GDI+ (lớp BieuDoTron3D lồng trong module này).
    //  - Dùng lại TheBoGocF4 (thẻ bo góc) của Module_GiaoDienForm4Web.cs, chỉ dùng các thuộc tính
    //    Name / Dock / Margin / TieuDe / Padding giống hệt Module_GiaoDienForm10Web.
    //  - Tìm control theo Name (không phụ thuộc private) => không đổi tên/kiểu/sự kiện của control nào.
    //    Biểu đồ tự cập nhật bằng cách lắng nghe TextChanged của 3 ô nhập (kể cả lúc form nạp dữ liệu từ CSDL).
    //  - Chỉ CẢNH BÁO trên giao diện khi Loại 2 + Loại 3 khác 100% hoặc Loại 1 > Loại 2; KHÔNG chặn lưu (logic lưu của form giữ nguyên).
    //  - Mọi bước bọc try/catch: lỗi trình bày không bao giờ làm form không mở được.
    //
    //  Dùng trong Form61_TyLeXetThiDuaNam:
    //     constructor, ngay SAU InitializeComponent():   Module_GiaoDienForm61Web.ApDung(this);
    //     TyLeTextBox_Enter / TyLeTextBox_Leave:          Module_GiaoDienForm61Web.VienFocus(textBox, true / false);
    //     MauLabelTyLe / MauLabelDo:                      lấy từ Module_GiaoDienForm61Web.MauNhanThuong / MauNhanChon
    // =====================================================================================
    internal static class Module_GiaoDienForm61Web
    {
        // ---------- Màu dùng chung với Form61 (form tự đổi màu nhãn / viền khi vào - rời ô nên lấy chung từ đây) ----------
        public static readonly Color MauNhanThuong = Color.FromArgb(71, 85, 105);     // nhãn bình thường
        // ---------- Bảng màu / font: lấy đúng giá trị của Form4 / Form10 ----------
        private static readonly Color MauNen = Color.FromArgb(244, 246, 250);
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);
        private static readonly Color MauChuDam = Color.FromArgb(30, 41, 59);
        private static readonly Color MauChuTieuDeBang = Color.FromArgb(75, 20, 130);
        private static readonly Color MauVienO = Color.FromArgb(203, 213, 225);
        private static readonly Color MauVienChon = Color.FromArgb(139, 92, 246);
        // Màu 4 lát của biểu đồ
        private static readonly Color MauLoai1 = Color.FromArgb(109, 40, 217);        // Loại 1 (CSTĐ): tím đậm
        private static readonly Color MauLoai2 = Color.FromArgb(167, 139, 250);       // Loại 2 (CSTT) - phần ngoài Loại 1: tím nhạt
        private static readonly Color MauLoai3 = Color.FromArgb(20, 184, 166);        // Loại 3 (HTNV): xanh ngọc
        private static readonly Color MauChua = Color.FromArgb(203, 213, 225);        // chưa phân bổ: xám
        // Màu dòng trạng thái
        private static readonly Color MauHopLe = Color.FromArgb(22, 163, 74);
        private static readonly Color MauCanhBao = Color.FromArgb(217, 119, 6);
        private static readonly Font FontTieuDe = new Font(Module_HeThong.TenFontHeThongDam, 15F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNhan = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNhanDam = new Font(Module_HeThong.TenFontHeThongDam, 10F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontO = new Font(Module_HeThong.TenFontHeThongDam, 10.5F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new Font(Module_HeThong.TenFontHeThongDam, 9F, FontStyle.Regular, GraphicsUnit.Point);
        private const int CaoDauTrang = 72;         // ảnh + tiêu đề trang
        private const int CaoChanTrang = 52;        // hàng nút
        private const int RongTheNhap = 510;        // thẻ nhập bên trái; thẻ biểu đồ ăn hết phần còn lại
        private const int CaoHangNhap = 46;         // mỗi hàng nhập
        // (Nền, Hover, Nhấn, Viền, Chữ) — theme tím nhạt của Form4: nút chính (tím đậm một nấc)
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutTimDam =
            (Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253), Color.FromArgb(167, 139, 250), Color.FromArgb(167, 139, 250), Color.FromArgb(91, 33, 182));
        // ===================== ĐIỂM VÀO =====================
        public static void ApDung(Form f)
        {
            f.SuspendLayout();
            try
            {
                f.BackColor = MauNen;
                (BieuDoTron3D? BieuDo, Label? TrangThai) bo = (null, null);
                An(() => { bo = DinhBoCuc(f); }, "BoCuc");
                An(() => DinhDauTrang(f), "DauTrang");
                An(() => DinhNhap(f), "Nhap");
                An(() => DinhNut(f), "Nut");
                An(() => GanBieuDo(f, bo.BieuDo, bo.TrangThai), "BieuDo");
                An(() => f.ClientSize = new Size(1280, 510), "KichThuoc");
            }
            finally
            {
                f.ResumeLayout(true);
            }
        }
        private static void An(Action viec, string ten)
        {
            try { viec(); }
            catch (Exception ex) { Debug.WriteLine($"[Module_GiaoDienForm61Web.{ten}] {ex.Message}"); }
        }
        private static T? Tim<T>(Control goc, string ten) where T : Control
            => goc.Controls.Find(ten, true).OfType<T>().FirstOrDefault();
        // ===================== BỐ CỤC TỔNG =====================
        // Bản gốc: 1 cột, 3 hàng chia theo % (tiêu đề | 3 ô nhập | nút) nên chỉ là 1 khối nhỏ.
        // Mới: 2 cột. Hàng 1 = đầu trang (trải 2 cột); hàng 2 = [thẻ nhập tỷ lệ | thẻ biểu đồ tròn 3D]; hàng 3 = nút (trải 2 cột).
        // Các control gốc được GIỮ NGUYÊN, chỉ đổi chỗ chứa.
        private static (BieuDoTron3D? BieuDo, Label? TrangThai) DinhBoCuc(Form f)
        {
            var t2 = Tim<TableLayoutPanel>(f, "tableLayoutPanel2");
            var t1 = Tim<TableLayoutPanel>(f, "tableLayoutPanel1");
            var t3 = Tim<TableLayoutPanel>(f, "tableLayoutPanel3");
            var nut = Tim<Krypton.Toolkit.KryptonButton>(f, "kryptonButton1_LuuVaDongForm");
            if (t2 == null || t1 == null || t3 == null || nut == null) return (null, null);
            BieuDoTron3D bieuDo;
            Label lblTrangThai;
            t2.SuspendLayout();
            try
            {
                t2.BackColor = MauNen;
                t2.Margin = new Padding(0);
                t2.Padding = new Padding(12, 10, 12, 10);
                t2.ColumnCount = 2;
                t2.ColumnStyles.Clear();
                t2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, RongTheNhap));
                t2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                t2.RowStyles.Clear();
                t2.RowStyles.Add(new RowStyle(SizeType.Absolute, CaoDauTrang));
                t2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                t2.RowStyles.Add(new RowStyle(SizeType.Absolute, CaoChanTrang));
                // Đầu trang và hàng nút trải suốt 2 cột
                t2.SetColumnSpan(t3, 2);
                t2.SetColumnSpan(nut, 2);
                // ----- Thẻ nhập: [3 hàng nhập gốc] + [quy tắc + dòng trạng thái] -----
                t2.Controls.Remove(t1);
                t1.Dock = DockStyle.Fill;
                t1.Margin = new Padding(0);
                t1.BackColor = Color.White;
                var ghiChu = new TableLayoutPanel
                {
                    Name = "tableLayoutPanel_GhiChuTyLe",
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2,
                    Margin = new Padding(0),
                    BackColor = Color.White
                };
                ghiChu.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                ghiChu.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                ghiChu.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                var lblQuyTac = new Label
                {
                    Name = "label_QuyTacTyLe",
                    AutoSize = true,
                    MaximumSize = new Size(460, 0), // <-- Tăng từ 290 lên 460 để chữ không bị rớt dòng
                    Font = FontNhan,
                    ForeColor = MauChuNhan,
                    BackColor = Color.White,
                    Margin = new Padding(2, 10, 2, 4),
                    Text = "Lưu ý: CSTĐ nằm trong CSTT; CSTT + HTNV = 100%"
                };
                lblTrangThai = new Label
                {
                    Name = "label_TrangThaiTyLe",
                    AutoSize = true,
                    MaximumSize = new Size(460, 0), // <-- Tăng từ 290 lên 460
                    Font = FontNhanDam,
                    ForeColor = MauChuNhan,
                    BackColor = Color.White,
                    Margin = new Padding(2, 0, 2, 0),
                    Text = ""
                };
                ghiChu.Controls.Add(lblQuyTac, 0, 0);
                ghiChu.Controls.Add(lblTrangThai, 0, 1);
                var noiDungNhap = new TableLayoutPanel
                {
                    Name = "tableLayoutPanel_NoiDungNhapTyLe",
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2,
                    Margin = new Padding(0),
                    BackColor = Color.White
                };
                noiDungNhap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                noiDungNhap.RowStyles.Add(new RowStyle(SizeType.Absolute, CaoHangNhap * 3));
                noiDungNhap.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                noiDungNhap.Controls.Add(t1, 0, 0);
                noiDungNhap.Controls.Add(ghiChu, 0, 1);
                var theNhap = new TheBoGocF4
                {
                    Name = "the_NhapTyLeXetThiDuaNam",
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0, 0, 10, 10),          // khe 10px giữa 2 thẻ và với hàng nút
                    TieuDe = "Nhập tỷ lệ xét thi đua",
                    Padding = new Padding(12, 36, 12, 10)        // 36 = vùng tiêu đề thẻ
                };
                theNhap.Controls.Add(noiDungNhap);
                t2.Controls.Add(theNhap, 0, 1);
                // ----- Thẻ biểu đồ tròn 3D -----
                bieuDo = new BieuDoTron3D { Name = "bieuDoTron3D_TyLeXetThiDuaNam", Dock = DockStyle.Fill, Margin = new Padding(0) };
                var theBieuDo = new TheBoGocF4
                {
                    Name = "the_BieuDoTyLeXetThiDuaNam",
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0, 0, 0, 10),
                    TieuDe = "Biểu đồ minh họa",
                    Padding = new Padding(12, 36, 12, 10)
                };
                theBieuDo.Controls.Add(bieuDo);
                t2.Controls.Add(theBieuDo, 1, 1);
            }
            finally
            {
                t2.ResumeLayout(true);
            }
            return (bieuDo, lblTrangThai);
        }
        // ===================== ĐẦU TRANG: ẢNH + TIÊU ĐỀ =====================
        private static void DinhDauTrang(Form f)
        {
            var t3 = Tim<TableLayoutPanel>(f, "tableLayoutPanel3");
            if (t3 != null)
            {
                t3.BackColor = MauNen;
                t3.Margin = new Padding(0);
                t3.ColumnStyles.Clear();
                t3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
                t3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            }
            var anh = Tim<PictureBox>(f, "pictureBox1");
            if (anh != null)
            {
                anh.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                anh.Margin = new Padding(0, 4, 12, 4);
                anh.SizeMode = PictureBoxSizeMode.Zoom;     // trước đây StretchImage -> méo hình
                anh.BackColor = MauNen;
            }
            // Tiêu đề trang (chữ "Tỷ lệ xét thi đua năm xxxx" do Form61_Load điền): chữ xanh lá căn giữa -> tím đậm căn trái
            var tieuDe = Tim<Label>(f, "label3_TieuDe");
            if (tieuDe != null)
            {
                tieuDe.Font = FontTieuDe;
                tieuDe.ForeColor = MauChuTieuDeBang;
                tieuDe.Anchor = AnchorStyles.Left;
                tieuDe.Margin = new Padding(0);
            }
        }
        // ===================== 3 HÀNG NHẬP: nhãn | ô số | % =====================
        private static void DinhNhap(Form f)
        {
            var t1 = Tim<TableLayoutPanel>(f, "tableLayoutPanel1");
            if (t1 == null) return;
            t1.ColumnStyles.Clear();
            t1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));    // <-- TĂNG LÊN 200F (cột nhãn chữ rộng rãi, không bao giờ bị rớt dòng)
            t1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));    // ô số nhập liệu
            t1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));     // dấu %
            t1.RowStyles.Clear();
            for (int i = 0; i < t1.RowCount; i++)
                t1.RowStyles.Add(new RowStyle(SizeType.Absolute, CaoHangNhap));
            foreach (Control c in t1.Controls)
            {
                int cot = t1.GetColumn(c);
                switch (c)
                {
                    case Label lb:
                        lb.AutoSize = true;
                        lb.Anchor = AnchorStyles.Left;
                        if (cot == 0)       // nhãn đầu hàng
                        {
                            lb.Font = FontNhanDam;
                            lb.ForeColor = MauNhanThuong;
                            lb.Margin = new Padding(0, 0, 6, 0);
                        }
                        else                // dấu "%"
                        {
                            lb.Font = FontNhan;
                            lb.ForeColor = MauChuNhan;
                            lb.Margin = new Padding(8, 0, 0, 0);
                        }
                        break;
                    case Krypton.Toolkit.KryptonTextBox tb:
                        tb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                        tb.Margin = new Padding(0, 3, 0, 3);
                        tb.StateCommon.Back.Color1 = Color.White;
                        KieuOKrypton(tb.StateCommon.Border, tb.StateActive.Border);
                        tb.StateCommon.Content.Font = FontO;
                        tb.StateCommon.Content.Color1 = MauChuDam;
                        break;
                }
            }
        }
        // ===================== NÚT =====================
        private static void DinhNut(Form f)
        {
            var nut = Tim<Krypton.Toolkit.KryptonButton>(f, "kryptonButton1_LuuVaDongForm");
            if (nut == null) return;
            KieuNut(nut, NutTimDam);
            nut.Anchor = AnchorStyles.Right;
            nut.Size = new Size(184, 34);
            nut.Margin = new Padding(0, 0, 4, 0);
        }
        // ===================== NỐI 3 Ô NHẬP VỚI BIỂU ĐỒ =====================
        private static void GanBieuDo(Form f, BieuDoTron3D? bieuDo, Label? lblTrangThai)
        {
            if (bieuDo == null) return;
            var o1 = Tim<Krypton.Toolkit.KryptonTextBox>(f, "kryptonTextBox1_TyLeXetCSTD");   // Loại 1
            var o2 = Tim<Krypton.Toolkit.KryptonTextBox>(f, "kryptonTextBox1_TyLeXetCSTT");   // Loại 2
            var o3 = Tim<Krypton.Toolkit.KryptonTextBox>(f, "kryptonTextBox1_TyLeXetHTNV");   // Loại 3
            if (o1 == null || o2 == null || o3 == null) return;
            void CapNhat()
            {
                int? a = DocTyLe(o1), b = DocTyLe(o2), c = DocTyLe(o3);
                bieuDo.DatGiaTri(a, b, c);
                if (lblTrangThai == null) return;
                var (noiDung, muc) = PhanTich(a, b, c);
                lblTrangThai.Text = noiDung;
                lblTrangThai.ForeColor = muc == 1 ? MauHopLe : muc == 2 ? MauCanhBao : MauChuNhan;
            }
            // Form61 có thể đặt lại chữ ô nhập (dán nội dung sai -> trả về giá trị cũ) => TextChanged chạy nhiều lần, luôn đọc giá trị cuối
            o1.TextChanged += (s, e) => CapNhat();
            o2.TextChanged += (s, e) => CapNhat();
            o3.TextChanged += (s, e) => CapNhat();
            CapNhat();      // lần đầu (form nạp dữ liệu CSDL ở Load sẽ gọi lại qua TextChanged)
        }
        /// <summary>Số nguyên 0..100 trong ô, hoặc null nếu ô trống / không hợp lệ.</summary>
        private static int? DocTyLe(Krypton.Toolkit.KryptonTextBox tb)
            => int.TryParse(tb.Text.Trim(), out int v) && v >= 0 && v <= 100 ? v : null;
        /// <summary>Mức: 0 = trung tính (chưa đủ dữ liệu), 1 = hợp lệ, 2 = cảnh báo.</summary>
        private static (string NoiDung, int Muc) PhanTich(int? l1, int? l2, int? l3)
        {
            if (l1 == null || l2 == null || l3 == null)
                return ("Chưa nhập đủ 3 tỷ lệ.", 0);
            if (l1 > l2)
                return ($"Loại 1 ({l1}%) đang lớn hơn Loại 2 ({l2}%): Loại 1 phải nằm trong Loại 2.", 2);
            int tong = l2.Value + l3.Value;
            if (tong != 100)
                return ($"Loại 2 + Loại 3 = {tong}% (cần bằng 100%).", 2);
            return ("✔ Hợp lệ: Loại 2 + Loại 3 = 100%.", 1);
        }
        // Ô nhập Krypton: nền trắng, viền xám bo góc 8, khi đang gõ viền chuyển tím
        private static void KieuOKrypton(Krypton.Toolkit.PaletteBorder vien, Krypton.Toolkit.PaletteBorder vienActive)
        {
            vien.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            vien.Color1 = MauVienO; vien.Color2 = MauVienO;
            vien.Rounding = 8F;
            vien.Width = 1;
            vienActive.Color1 = MauVienChon; vienActive.Color2 = MauVienChon;
        }
        private static void KieuNut(Krypton.Toolkit.KryptonButton b, (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) m)
        {
            b.StateCommon.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            b.StateCommon.Border.Rounding = 8F;
            b.StateCommon.Border.Width = 1;
            b.StateCommon.Content.ShortText.Font = FontNut;
            b.StateCommon.Content.ShortText.Color1 = m.Chu;
            b.StateNormal.Back.Color1 = m.Nen; b.StateNormal.Back.Color2 = m.Nen;
            b.StateNormal.Border.Color1 = m.Vien; b.StateNormal.Border.Color2 = m.Vien;
            b.StateNormal.Content.ShortText.Color1 = m.Chu;
            b.StateTracking.Back.Color1 = m.Hover; b.StateTracking.Back.Color2 = m.Hover;
            b.StateTracking.Border.Color1 = m.Vien; b.StateTracking.Border.Color2 = m.Vien;
            b.StateTracking.Border.Rounding = 8F;
            b.StateTracking.Content.ShortText.Color1 = m.Chu;
            b.StatePressed.Back.Color1 = m.Down; b.StatePressed.Back.Color2 = m.Down;
            b.StatePressed.Border.Color1 = m.Vien; b.StatePressed.Border.Color2 = m.Vien;
            b.StatePressed.Content.ShortText.Color1 = m.Chu;
            // Nút đang giữ focus / nút mặc định: Krypton lấy nền + viền từ theme (thường trắng) nhưng chữ lấy từ trạng thái của nút
            // => dễ bị chữ trắng trên nền trắng. Ép giống trạng thái thường (đúng cách Form4 đã làm).
            foreach (var ov in new[] { b.OverrideFocus, b.OverrideDefault })
            {
                ov.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
                ov.Back.Color1 = m.Nen; ov.Back.Color2 = m.Nen;
                ov.Border.Color1 = m.Vien; ov.Border.Color2 = m.Vien;
                ov.Content.ShortText.Color1 = m.Chu;
            }
            // Nút bị khóa: tím rất nhạt, chữ tím nhạt vẫn đọc được
            b.StateDisabled.Back.Color1 = Color.FromArgb(245, 243, 255); b.StateDisabled.Back.Color2 = Color.FromArgb(245, 243, 255);
            b.StateDisabled.Border.Color1 = Color.FromArgb(221, 214, 254); b.StateDisabled.Border.Color2 = Color.FromArgb(221, 214, 254);
            b.StateDisabled.Content.ShortText.Color1 = Color.FromArgb(167, 139, 250);
            b.Cursor = Cursors.Hand;
        }
        // =====================================================================================
        //  BIỂU ĐỒ TRÒN 3D — tự vẽ bằng GDI+ (không cần thư viện ngoài, không cần .resx)
        //  Thân bánh dựng bằng cách xếp chồng nhiều lớp elip dịch xuống từng px (xa -> gần) nên mặt bên liền khối,
        //  mặt trên có lớp bóng sáng nhẹ; bên phải là chú thích màu + giá trị.
        // =====================================================================================
        private sealed class BieuDoTron3D : Control
        {
            private int? _l1, _l2, _l3;
            private static readonly Font FontTen = new Font(Module_HeThong.TenFontHeThongDam, 9F, FontStyle.Regular, GraphicsUnit.Point);
            private static readonly Font FontChiTiet = new Font(Module_HeThong.TenFontHeThong, 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            private static readonly Font FontNhanLat = new Font(Module_HeThong.TenFontHeThongDam, 9.5F, FontStyle.Bold, GraphicsUnit.Point);
            public BieuDoTron3D()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = Color.White;
                TabStop = false;
            }
            /// <summary>Giá trị 0..100 của Loại 1 / 2 / 3; null = ô trống hoặc không hợp lệ.</summary>
            public void DatGiaTri(int? l1, int? l2, int? l3)
            {
                if (_l1 == l1 && _l2 == l2 && _l3 == l3) return;
                _l1 = l1; _l2 = l2; _l3 = l3;
                Invalidate();
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(BackColor);
                if (Width < 120 || Height < 80) return;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                int a = Math.Clamp(_l1 ?? 0, 0, 100);
                int b = Math.Clamp(_l2 ?? 0, 0, 100);
                int c = Math.Clamp(_l3 ?? 0, 0, 100);
                int chiLoai2 = Math.Max(b - a, 0);            // Loại 1 nằm trong Loại 2 => phần Loại 2 nằm ngoài Loại 1
                int dung = a + chiLoai2 + c;                  // = max(Loại 1, Loại 2) + Loại 3
                int chuaPhanBo = Math.Max(100 - dung, 0);     // nếu Loại 2 + Loại 3 < 100
                float tong = dung + chuaPhanBo;               // > 100 nếu nhập vượt => biểu đồ tự quy về 100% của tổng nhập
                var lat = new List<(float Gia, Color Mau, bool Xam)>();
                if (a > 0) lat.Add((a, MauLoai1, false));
                if (chiLoai2 > 0) lat.Add((chiLoai2, MauLoai2, false));
                if (c > 0) lat.Add((c, MauLoai3, false));
                if (chuaPhanBo > 0) lat.Add((chuaPhanBo, MauChua, true));
                int cat = (int)(Width * 0.58f);
                VeBanh(g, new Rectangle(4, 4, cat - 8, Height - 8), lat, tong);
                VeChuThich(g, new Rectangle(cat, 4, Width - cat - 4, Height - 8), chuaPhanBo);
            }
            private static void VeBanh(Graphics g, Rectangle vung, List<(float Gia, Color Mau, bool Xam)> lat, float tong)
            {
                const float tyLeCao = 0.56f;      // elip dẹt: cao = 56% rộng (góc nhìn nghiêng)
                const float tyLeDay = 0.12f;      // độ dày thân bánh = 12% đường kính
                float w = Math.Min(vung.Width, vung.Height / (tyLeCao + tyLeDay + 0.06f));
                if (w < 60 || lat.Count == 0) return;
                float h = w * tyLeCao;
                int day = Math.Max(6, (int)(w * tyLeDay));
                float x = vung.X + (vung.Width - w) / 2f;
                float y = vung.Y + (vung.Height - (h + day + 6)) / 2f;
                float rx = w / 2f, ry = h / 2f, cx = x + rx, cy = y + ry;
                // Bóng đổ dưới bánh
                using (var bong = new SolidBrush(Color.FromArgb(28, 71, 85, 105)))
                    g.FillEllipse(bong, x + 3, y + day + 5, w, h);
                // Góc bắt đầu / góc quét của từng lát (bắt đầu từ 12 giờ, chạy theo chiều kim đồng hồ)
                var goc = new List<(float Bd, float Qu)>(lat.Count);
                float bd = -90f;
                foreach (var l in lat)
                {
                    float qu = 360f * l.Gia / tong;
                    goc.Add((bd, qu));
                    bd += qu;
                }
                // Thân bánh: xếp các lớp từ xa (dịch xuống nhiều) tới gần; mỗi lớp lót 1 elip tối để không lộ khe giữa các lát
                for (int k = day; k >= 1; k--)
                {
                    using (var lot = new SolidBrush(Color.FromArgb(88, 80, 120)))
                        g.FillEllipse(lot, x, y + k, w, h);
                    for (int i = 0; i < lat.Count; i++)
                    {
                        using var br = new SolidBrush(Toi(lat[i].Mau));
                        g.FillPolygon(br, DaGiacLat(cx, cy + k, rx, ry, goc[i].Bd, goc[i].Qu));
                    }
                }
                // Mặt trên
                for (int i = 0; i < lat.Count; i++)
                {
                    using var br = new SolidBrush(lat[i].Mau);
                    g.FillPolygon(br, DaGiacLat(cx, cy, rx, ry, goc[i].Bd, goc[i].Qu));
                }
                // Lớp bóng sáng nhẹ từ trên xuống cho cảm giác bóng/khối
                using (var sang = new LinearGradientBrush(new RectangleF(x, y, w, h),
                           Color.FromArgb(85, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), LinearGradientMode.Vertical))
                    g.FillEllipse(sang, x, y, w, h);
                // Đường trắng ngăn các lát
                if (lat.Count > 1)
                {
                    using var but = new Pen(Color.White, 1.6f) { LineJoin = LineJoin.Round };
                    for (int i = 0; i < lat.Count; i++)
                        g.DrawPolygon(but, DaGiacLat(cx, cy, rx, ry, goc[i].Bd, goc[i].Qu));
                }
                // Nhãn % trên các lát đủ rộng
                using var dinh = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                for (int i = 0; i < lat.Count; i++)
                {
                    if (goc[i].Qu < 24f) continue;
                    double giua = (goc[i].Bd + goc[i].Qu / 2f) * Math.PI / 180.0;
                    float px = cx + rx * 0.60f * (float)Math.Cos(giua);
                    float py = cy + ry * 0.60f * (float)Math.Sin(giua);
                    string chu = $"{(int)Math.Round(lat[i].Gia)}%";
                    using var bongChu = new SolidBrush(Color.FromArgb(70, 0, 0, 0));
                    using var mauChu = new SolidBrush(lat[i].Xam ? MauChuNhan : Color.White);
                    if (!lat[i].Xam) g.DrawString(chu, FontNhanLat, bongChu, px + 1f, py + 1f, dinh);
                    g.DrawString(chu, FontNhanLat, mauChu, px, py, dinh);
                }
            }
            private void VeChuThich(Graphics g, Rectangle vung, int chuaPhanBo)
            {
                var muc = new List<(Color Mau, string Ten, string ChiTiet)>
                {
                    (MauLoai1, "Loại 1 · CSTĐ", _l1 == null ? "—" : $"{_l1}% · nằm trong Loại 2"),
                    (MauLoai2, "Loại 2 · CSTT", _l2 == null ? "—" : $"{_l2}% · gồm cả Loại 1"),
                    (MauLoai3, "Loại 3 · HTNV", _l3 == null ? "—" : $"{_l3}%"),
                };
                if (chuaPhanBo > 0 && (_l1 != null || _l2 != null || _l3 != null))
                    muc.Add((MauChua, "Chưa phân bổ", $"{chuaPhanBo}%"));
                const int caoMuc = 42;
                int y0 = vung.Y + Math.Max(0, (vung.Height - muc.Count * caoMuc) / 2);
                using var dang = new StringFormat(StringFormatFlags.NoWrap) { Trimming = StringTrimming.EllipsisCharacter };
                using var chuTen = new SolidBrush(MauChuDam);
                using var chuCT = new SolidBrush(MauChuNhan);
                for (int i = 0; i < muc.Count; i++)
                {
                    int y = y0 + i * caoMuc;
                    using (var o = new SolidBrush(muc[i].Mau))
                        g.FillEllipse(o, vung.X + 2, y + 3, 13, 13);
                    var khuTen = new RectangleF(vung.X + 22, y, vung.Width - 22, 18);
                    var khuCT = new RectangleF(vung.X + 22, y + 18, vung.Width - 22, 18);
                    g.DrawString(muc[i].Ten, FontTen, chuTen, khuTen, dang);
                    g.DrawString(muc[i].ChiTiet, FontChiTiet, chuCT, khuCT, dang);
                }
            }
            // Lát bánh dạng đa giác theo góc tham số của elip (đảm bảo mỗi lát đúng tỷ lệ góc, không phụ thuộc cách GDI+ hiểu góc trên elip dẹt)
            private static PointF[] DaGiacLat(float cx, float cy, float rx, float ry, float batDau, float quet)
            {
                int n = Math.Max(2, (int)Math.Ceiling(Math.Abs(quet) / 2f));
                var p = new PointF[n + 2];
                p[0] = new PointF(cx, cy);
                for (int i = 0; i <= n; i++)
                {
                    double ang = (batDau + quet * i / n) * Math.PI / 180.0;
                    p[i + 1] = new PointF(cx + rx * (float)Math.Cos(ang), cy + ry * (float)Math.Sin(ang));
                }
                return p;
            }
            // Màu mặt bên: tối hơn mặt trên ~34%
            private static Color Toi(Color c)
                => Color.FromArgb(c.A, (int)(c.R * 0.66), (int)(c.G * 0.66), (int)(c.B * 0.66));
        }
    }
}