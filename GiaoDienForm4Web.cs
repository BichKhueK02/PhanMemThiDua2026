using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
using System.Drawing.Drawing2D;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CHO Form4_TrangDauTien (Trang chủ) — CHỈ TRÌNH BÀY, KHÔNG CÓ LOGIC NGHIỆP VỤ
    //  - Lớp static độc lập (không phải form) => không tạo .resx, KHÔNG cần sửa .csproj.
    //  - Chỉ cần 1 dòng trong constructor Form4_TrangDauTien, ngay SAU InitializeComponent():
    //        GiaoDienForm4Web.ApDung(this);
    //  - Tìm control theo Name (không phụ thuộc private) => không đổi tên/kiểu/sự kiện của bất kỳ control nào.
    //  - Những thứ code gốc tự điều khiển lúc chạy được GIỮ NGUYÊN: màu chữ nhãn phần trăm / ô "Mở file" /
    //    đường dẫn lưu, cỡ chữ label11, màu header 2 bảng (tím), màu nút xuất khi hết dữ liệu...
    //  - Kích thước theo đơn vị thiết kế (WinForms tự scale theo font/DPI).
    //
    //  BẢN NÀY SỬA 2 CHỖ:
    //  1) 2 bảng dữ liệu: KHÔNG còn cắt góc bảng bằng Region (nguyên nhân làm góc bảng bị "mất", răng cưa, lẹm vào ô).
    //     Thay bằng 1 KHUNG BO TRÒN liền mạch vẽ ở thẻ cha; bảng nằm gọn bên trong khung, dải tiêu đề tô cùng màu
    //     với tiêu đề bảng (đọc trực tiếp từ ColumnHeadersDefaultCellStyle nên luôn khớp với màu code gốc đặt).
    //     Tiêu đề bảng ép nền PHẲNG + viền cùng màu nền (Krypton mặc định vẽ chuyển sắc + viền hở ở ô góc) và ô góc ghi "STT".
    //  2) Tab "Thông tin nhanh" / "Biểu đồ minh họa": bỏ kiểu tự vẽ viên thuốc; dùng thanh tab phẳng đơn giản
    //     (chữ + gạch chân xanh ở tab đang chọn). TabControl/TabPage gốc vẫn còn, chỉ ẩn phần tiêu đề tab gốc.
    //  3) Hàng "Chọn loại xuất": combo ngắn lại; 3 nút Xuất trình họp / Xuất tất cả / Xuất tệp (*.pdf) rộng vừa đủ, căn đều trái - giữa - phải.
    //  5) Mọi ComboBox bo tròn góc (OComboWebF4 + PopupDanhSachF4: ô chọn + danh sách sổ xuống vẽ tay bo tròn, ComboBox gốc giữ nguyên logic).
    //  4) Mọi nút dùng theme tím nhạt (nền #EDE9FE, viền #C4B5FD, chữ #6D28D9); nút chính đậm hơn một nấc. Sửa lỗi nút "Lưu thông tin"
    //     sau khi lưu bị chữ trắng trên nền trắng (do trạng thái focus của Krypton dùng nền theme nhưng chữ của nút).
    // =====================================================================================
    internal static class GiaoDienForm4Web
    {
        private static readonly Color MauNen = Color.FromArgb(244, 246, 250);
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);

        private static readonly Font FontNhan = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontKyHieu = new Font("Segoe UI Semibold", 10F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNutLon = new Font("Segoe UI Semibold", 10F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontDanhSach = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        // Khoảng đệm giữa khung bo tròn và bảng (px thiết kế): đủ để 4 góc vuông của bảng nằm lọt trong cung bo bán kính 10
        private const int DemKhungBang = 5;

        // Màu tiêu đề bảng (trùng màu code gốc đặt: nền tím nhạt, chữ tím đậm) — dùng để ép Krypton vẽ tiêu đề PHẲNG, không chuyển sắc
        private static readonly Color MauTieuDeBang = Color.FromArgb(238, 232, 255);
        private static readonly Color MauChuTieuDeBang = Color.FromArgb(75, 20, 130);

        // Bộ màu nút theo theme tím nhạt (lấy đúng màu ảnh mẫu): (Nền, Hover, Nhấn, Viền, Chữ)
        //   NutTim    : nền #EDE9FE, viền #C4B5FD, chữ #6D28D9  (nút thường)
        //   NutTimDam : đậm hơn một nấc cùng tông (nút chính: Lưu thông tin, Xuất tệp, Xuất tất cả) — chữ luôn tối trên nền sáng
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutTim =
            (Color.FromArgb(237, 233, 254), Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253), Color.FromArgb(196, 181, 253), Color.FromArgb(109, 40, 217));
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutTimDam =
            (Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253), Color.FromArgb(167, 139, 250), Color.FromArgb(167, 139, 250), Color.FromArgb(91, 33, 182));

        public static void ApDung(Form f)
        {
            f.SuspendLayout();
            try
            {
                f.BackColor = MauNen;
                DinhBoCuc(f);
                BocCacThe(f);
                DinhKhoiKhaiBao(f);
                DinhKhoiLenh(f);
                BoTronCacCombo(f);
                DinhLuoi(f);
                DinhTabVaDanhSach(f);
                DinhThanhTrangThai(f);
            }
            finally
            {
                f.ResumeLayout(true);
            }
        }

        // ---------- Tìm control theo Name ----------
        private static T? Tim<T>(Control goc, string ten) where T : Control
            => goc.Controls.Find(ten, true).OfType<T>().FirstOrDefault();

        // ---------- Bố cục tổng ----------
        private static void DinhBoCuc(Form f)
        {
            var t1 = Tim<TableLayoutPanel>(f, "tableLayoutPanel1");
            if (t1 != null)
            {
                t1.BackColor = MauNen;
                t1.Padding = new Padding(12, 10, 12, 8);
                t1.ColumnStyles.Clear();
                t1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                t1.RowStyles.Clear();
                t1.RowStyles.Add(new RowStyle(SizeType.Absolute, 244F));   // hàng 1: khai báo + lệnh (cố định, không bị bóp khi cửa sổ nhỏ)
                t1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));    // hàng 2: các bảng + trạng thái (ăn hết phần còn lại)
            }

            foreach (var ten in new[] { "splitContainer1", "splitContainer2", "splitContainer3" })
            {
                var sc = Tim<SplitContainer>(f, ten);
                if (sc == null) continue;
                sc.BackColor = MauNen;
                sc.Panel1.BackColor = MauNen;
                sc.Panel2.BackColor = MauNen;
                sc.Margin = new Padding(0);
                try { sc.SplitterWidth = 10; }          // khe hở giữa các thẻ
                catch (ArgumentException) { }
            }
            var sc1 = Tim<SplitContainer>(f, "splitContainer1");
            if (sc1 != null) sc1.Margin = new Padding(0, 10, 0, 0);
        }

        // ---------- 5 GroupBox -> 5 thẻ trắng bo góc ----------
        // GroupBox gốc được giữ lại (ẩn) trong thẻ để code gốc nếu gán .Text thì tiêu đề thẻ tự cập nhật theo.
        private static void BocCacThe(Form f)
        {
            foreach (var ten in new[] { "groupBox5", "groupBox4", "groupBox1", "groupBox2", "groupBox3" })
            {
                var gb = Tim<GroupBox>(f, ten);
                if (gb != null) BocThe(gb);
            }
        }

        private static void BocThe(GroupBox gb)
        {
            var cha = gb.Parent;
            if (cha == null || gb.Controls.Count == 0) return;
            var noiDung = gb.Controls[0];

            // Thẻ chứa bảng dữ liệu: chừa thêm DemKhungBang quanh bảng để vẽ khung bo tròn liền mạch (không cắt góc bảng)
            var luoi = noiDung as DataGridView;
            int gap = luoi != null ? DemKhungBang : 0;

            var the = new TheBoGocF4
            {
                Dock = DockStyle.Fill,
                TieuDe = gb.Text,
                DemKhung = gap,
                Padding = new Padding(12 + gap, 36 + gap, 12 + gap, 10 + gap)   // 36 = vùng tiêu đề thẻ, nội dung luôn nằm dưới tiêu đề
            };

            cha.SuspendLayout();
            try
            {
                cha.Controls.Remove(gb);
                cha.Controls.Add(the);
                gb.Visible = false;
                gb.Dock = DockStyle.None;
                the.Controls.Add(noiDung);        // chuyển nguyên bảng/lưới/tab sang thẻ (mọi sự kiện + logic giữ nguyên)
                noiDung.Dock = DockStyle.Fill;
                the.Controls.Add(gb);
                gb.TextChanged += (s, e) => the.TieuDe = gb.Text;
                if (luoi != null)
                {
                    the.LuoiBang = luoi;
                    // Code gốc đặt màu / chiều cao tiêu đề bảng lúc chạy -> khung vẽ lại để dải tiêu đề luôn khớp màu
                    luoi.ColumnHeadersHeightChanged += (s, e) => the.Invalidate();
                    luoi.ColumnHeadersDefaultCellStyleChanged += (s, e) => the.Invalidate();
                    luoi.DataBindingComplete += (s, e) => the.Invalidate();
                }
            }
            finally
            {
                cha.ResumeLayout(true);
            }
        }

        // ---------- Thẻ 1: Khai báo thông tin (5 hàng) ----------
        private static void DinhKhoiKhaiBao(Form f)
        {
            var t5 = Tim<TableLayoutPanel>(f, "tableLayoutPanel5");
            if (t5 != null)
            {
                t5.Margin = new Padding(0);
                t5.RowStyles.Clear();
                for (int i = 0; i < 4; i++) t5.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
                t5.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));   // hàng nút Lưu
            }

            // Cột nhãn đầu hàng cố định 140 -> ô nhập của 4 hàng thẳng cột với nhau
            // Hàng 1: Thời gian, địa điểm | địa điểm | Ngày | / | Tháng | / | Năm | Tự động
            DinhHang(f, "tableLayoutPanel15", 172F, new[] { (SizeType.Percent, 40F), (SizeType.AutoSize, 0F), (SizeType.Percent, 14F), (SizeType.AutoSize, 0F),
                                                         (SizeType.Percent, 14F), (SizeType.AutoSize, 0F), (SizeType.Percent, 18F), (SizeType.AutoSize, 0F) });
            // Hàng 2: Đề nghị | combo | Loại 1 | Loại 2 | Loại 3  (nhãn phần trăm do logic điền chữ + màu)
            DinhHang(f, "tableLayoutPanel16", 172F, new[] { (SizeType.Percent, 40F), (SizeType.Percent, 20F), (SizeType.Percent, 20F), (SizeType.Percent, 20F) });
            // Hàng 3: Chỉ huy phê duyệt | combo | Chế độ xét thi đua | combo
            DinhHang(f, "tableLayoutPanel9", 172F, new[] { (SizeType.Percent, 60F), (SizeType.AutoSize, 0F), (SizeType.Percent, 40F) });
            // Hàng 4: Tiêu đề báo cáo | loại | Chọn tháng | số tháng | Chọn tuần | số tuần (logic ẩn/hiện từng cái)
            DinhHang(f, "tableLayoutPanel3", 172F, new[] { (SizeType.Percent, 34F), (SizeType.AutoSize, 0F), (SizeType.Percent, 33F),
                                                        (SizeType.AutoSize, 0F), (SizeType.Percent, 33F) });

            foreach (var ten in new[] { "label5", "label12", "label10", "label4" })
                DinhNhan(Tim<Label>(f, ten), true);
            foreach (var ten in new[] { "label6", "label2_LabelThang", "label2_ChonTuan", "label1_CheDoXetThiDuaTanBinh" })
                DinhNhan(Tim<Label>(f, ten));
            // Dấu "/" giữa ngày/tháng/năm
            foreach (var ten in new[] { "label7", "label8" })
            {
                var l = Tim<Label>(f, ten);
                if (l == null) continue;
                l.Font = FontKyHieu; l.ForeColor = MauChuNhan; l.Anchor = AnchorStyles.None; l.Margin = new Padding(6, 0, 6, 0);
            }
            // Nhãn phần trăm Loại 1/2/3: chỉ chỉnh font + vị trí (chữ + màu do logic điền)
            foreach (var ten in new[] { "label__PhanTramLoai1", "label__PhanTramLoai2", "label__PhanTramLoai3" })
            {
                var l = Tim<Label>(f, ten);
                if (l == null) continue;
                l.Font = FontNhan; l.Anchor = AnchorStyles.Left; l.Margin = new Padding(12, 0, 0, 0);
            }

            foreach (var ten in new[] { "comboBox_DiaDiem", "comboBox_Ngay", "comboBox_Thang", "comboBox_Nam", "com_DeNghi",
                                        "comboBox_ChiHuyD", "comboBox1_CheDoXetThiDua", "comboBox1_ChonLoaiBaoCao",
                                        "comboBox2_ChonSoThang", "comboBox2_ChonSoTuan" })
                DinhOCombo(Tim<ComboBox>(f, ten));

            var tuDong = Tim<CheckBox>(f, "checkBox1_TuDongChonNgayThang");
            if (tuDong != null) { tuDong.Font = FontNhan; tuDong.ForeColor = MauChuNhan; tuDong.Margin = new Padding(12, 3, 0, 3); }

            var luu = Tim<Krypton.Toolkit.KryptonButton>(f, "kryptonButton_LuuThongTin");
            if (luu != null)
            {
                KieuNut(luu, NutTimDam);
                luu.StateCommon.Content.ShortText.Font = FontNutLon;
                luu.Anchor = AnchorStyles.None;     // căn giữa ô 44px
                luu.Size = new Size(260, 34);
            }
        }

        // ---------- Thẻ 2: Lệnh xử lý data (4 hàng) ----------
        private static void DinhKhoiLenh(Form f)
        {
            var t6 = Tim<TableLayoutPanel>(f, "tableLayoutPanel6");
            if (t6 != null) t6.Margin = new Padding(0);

            // Hàng 1: [icon] [....] đường dẫn lưu ............ [Mở file]
            var t11 = DatCot(f, "tableLayoutPanel11", (SizeType.Absolute, 44F), (SizeType.Absolute, 72F), (SizeType.Percent, 100F), (SizeType.AutoSize, 0F));
            DinhAnhBieuTuong(Tim<PictureBox>(f, "pictureBox2"));
            var chon = Tim<Krypton.Toolkit.KryptonButton>(f, "kryptonButton_ChonDuongDanLuu");
            if (chon != null) { KieuNut(chon, NutTim); CanNut(chon, new Padding(0)); }
            var l11 = Tim<Label>(f, "label11");              // cỡ chữ + màu do logic điều khiển -> chỉ chỉnh vị trí
            if (l11 != null) { l11.Anchor = AnchorStyles.Left; l11.Margin = new Padding(10, 0, 6, 0); }
            var moFile = Tim<CheckBox>(f, "Check_MoThuMuc");  // màu xanh/đỏ do logic điều khiển
            if (moFile != null) { moFile.Font = FontNhan; moFile.Anchor = AnchorStyles.Left; moFile.Margin = new Padding(8, 0, 0, 0); }

            // Hàng 2: [icon] Chọn [combo ngắn] ····· [Xuất tệp] ····· [Mở thư mục]
            //   Các nút rộng cố định, khoảng trống chia ĐỀU qua 2 cột đệm; nút cuối sát mép phải thẳng hàng với hàng 3-4.
            var t14 = DatCot(f, "tableLayoutPanel14", (SizeType.Absolute, 44F), (SizeType.AutoSize, 0F), (SizeType.Percent, 26F),
                                                      (SizeType.Absolute, 14F), (SizeType.Percent, 37F), (SizeType.Absolute, 14F), (SizeType.Percent, 37F));
            DinhAnhBieuTuong(Tim<PictureBox>(f, "pictureBox1"));
            DinhNhan(Tim<Label>(f, "label3"), true);
            var cbDeXuat = Tim<ComboBox>(f, "comboBox1_ChonLoaiDeXuat");   // BackColor hồng/xanh do logic điều khiển
            if (cbDeXuat != null) { cbDeXuat.Anchor = AnchorStyles.Left | AnchorStyles.Right; cbDeXuat.Margin = new Padding(8, 3, 10, 3); }
            NutTrongO(f, "kryptonButton_XuatDanhSachLoai", NutTimDam, new Padding(0));
            NutTrongO(f, "kryptonButton_MoThuMuc", NutTim, new Padding(0));
            if (t14 != null)   // chuyển 2 nút sang cột mới (chỉ đổi vị trí ô, không đổi control/sự kiện); nút phải trước để khỏi trùng ô
            {
                var moTM = Tim<Krypton.Toolkit.KryptonButton>(f, "kryptonButton_MoThuMuc");
                var xuat = Tim<Krypton.Toolkit.KryptonButton>(f, "kryptonButton_XuatDanhSachLoai");
                if (moTM != null) t14.SetColumn(moTM, 6);
                if (xuat != null) t14.SetColumn(xuat, 4);
            }

            // Hàng 3: [Xuất trình họp] ····· [Xuất tất cả] ····· [Xuất tệp (*.pdf)]  (nút gọn, căn đều: trái - giữa - phải)
            var t12 = DatCot(f, "tableLayoutPanel12", (SizeType.Percent, 33F), (SizeType.Absolute, 14F), (SizeType.Percent, 34F),
                                                      (SizeType.Absolute, 14F), (SizeType.Percent, 33F));
            NutTrongO(f, "kryptonButton_XuatTrinhKy", NutTim, new Padding(0));
            NutTrongO(f, "kryptonButton_XuatTatCa", NutTimDam, new Padding(0));
            NutTrongO(f, "kryptonButton1_XuatTepPdf", NutTim, new Padding(0));
            if (t12 != null)
            {
                var pdf = Tim<Krypton.Toolkit.KryptonButton>(f, "kryptonButton1_XuatTepPdf");
                var tatCa = Tim<Krypton.Toolkit.KryptonButton>(f, "kryptonButton_XuatTatCa");
                if (pdf != null) t12.SetColumn(pdf, 4);
                if (tatCa != null) t12.SetColumn(tatCa, 2);
            }

            // Hàng 4: 4 nút tiện ích (đều nhau)
            DatCot(f, "tableLayoutPanel7", (SizeType.Percent, 25F), (SizeType.Percent, 25F), (SizeType.Percent, 25F), (SizeType.Percent, 25F));
            NutTrongO(f, "kryptonButton1_TomTatThanhTich", NutTim, new Padding(0, 0, 8, 0));
            NutTrongO(f, "kryptonButton_Refresh", NutTim, new Padding(0, 0, 8, 0));
            NutTrongO(f, "kryptonButton_KiemTraTLvaQS", NutTim, new Padding(0, 0, 8, 0));
            NutTrongO(f, "kryptonButton_MayTinh", NutTim, new Padding(0));
            _ = t11;
        }

        // ---------- Mọi ComboBox: bo tròn góc, đồng bộ với nút + thẻ ----------
        // ComboBox gốc của WinForms không có kiểu bo góc, còn đổi sang KryptonComboBox thì phải sửa Designer + code gốc.
        // Nên giữ nguyên control (cùng tên, vị trí, sự kiện, màu nền do logic đặt, ẩn/hiện do logic) và chỉ vẽ phủ viền bo tròn lên chính nó.
        private static void BoTronCacCombo(Form f)
        {
            foreach (var cb in TatCa<ComboBox>(f).ToList())   // ToList: vì Gan thêm control vào thẻ trong lúc duyệt
                if (cb.GetType() == typeof(ComboBox))      // đúng ComboBox gốc; bỏ qua lớp con (vd. phần ruột của control Krypton)
                    OComboWebF4.Gan(cb);
        }

        private static IEnumerable<T> TatCa<T>(Control goc) where T : Control
        {
            foreach (Control c in goc.Controls)
            {
                if (c is T t) yield return t;
                foreach (var con in TatCa<T>(c)) yield return con;
            }
        }

        // ---------- Lưới: tiêu đề phẳng liền khung, ô góc ghi "STT", viền dòng nhạt ----------
        // Góc bo tròn của bảng KHÔNG xử lý ở đây (không dùng Region) mà do khung của thẻ cha vẽ (xem TheBoGocF4.VeKhungBang).
        // Nguyên nhân viền "mất góc" trước đây: Krypton tự vẽ tiêu đề bằng nền CHUYỂN SẮC + viền xanh xám, riêng ô góc trên-trái
        // không có cạnh trên => viền hở. Ở đây ép tiêu đề thành nền phẳng (cùng màu dải tiêu đề của khung) và viền cùng màu nền
        // (vô hình), nên khung bo tròn + tiêu đề hợp thành 1 đường viền liền mạch.
        private static void DinhLuoi(Form f)
        {
            foreach (var ten in new[] { "kryptonDataGridView1", "kryptonDataGridView2" })
            {
                var d = Tim<Krypton.Toolkit.KryptonDataGridView>(f, ten);
                if (d == null) continue;
                d.BackgroundColor = Color.White;      // vùng trống dưới dòng cuối: trắng liền với thẻ, không lộ nền xám
                d.StateCommon.Background.Color1 = Color.White;
                d.StateCommon.DataCell.Border.Color1 = Color.FromArgb(235, 238, 243);
                d.StateCommon.HeaderRow.Back.Color1 = Color.FromArgb(250, 248, 255);
                d.StateCommon.HeaderRow.Back.Color2 = Color.FromArgb(250, 248, 255);
                d.StateCommon.HeaderRow.Border.Color1 = Color.FromArgb(237, 233, 254);
                d.StateSelected.DataCell.Back.Color1 = Color.FromArgb(239, 246, 255);
                d.StateSelected.DataCell.Back.Color2 = Color.FromArgb(239, 246, 255);

                // Hàng tiêu đề (gồm cả ô góc trên-trái): nền phẳng + viền cùng màu nền
                TieuDePhang(d.StateCommon.HeaderColumn.Back, d.StateCommon.HeaderColumn.Border);
                TieuDePhang(d.StateTracking.HeaderColumn.Back, d.StateTracking.HeaderColumn.Border);   // rê chuột lên tiêu đề cũng không đổi sang chuyển sắc
                TieuDePhang(d.StatePressed.HeaderColumn.Back, d.StatePressed.HeaderColumn.Border);

                GhiSttOGoc(d);
            }
        }

        private static void TieuDePhang(Krypton.Toolkit.PaletteBack back, Krypton.Toolkit.PaletteBorder vien)
        {
            back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            back.Color1 = MauTieuDeBang; back.Color2 = MauTieuDeBang;
            vien.Color1 = MauTieuDeBang; vien.Color2 = MauTieuDeBang;
        }

        // Ô góc trên-trái (phía trên cột số thứ tự) ghi "STT". Krypton vẽ xong cả ô rồi mới gọi CellPainting nên chỉ cần vẽ chữ đè lên;
        // lấy font/màu từ kiểu tiêu đề hiện hành (code gốc đổi cỡ chữ theo kích thước form) => luôn khớp với các tiêu đề cột.
        private static void GhiSttOGoc(Krypton.Toolkit.KryptonDataGridView d)
        {
            d.CellPainting += (s, e) =>
            {
                if (e.RowIndex != -1 || e.ColumnIndex != -1 || e.Graphics == null) return;
                var kieu = d.ColumnHeadersDefaultCellStyle;
                var font = kieu.Font ?? d.Font;
                var mau = kieu.ForeColor.IsEmpty ? MauChuTieuDeBang : kieu.ForeColor;
                TextRenderer.DrawText(e.Graphics, "STT", font, e.CellBounds, mau,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            };
        }

        // ---------- Thẻ 5: tab "Thông tin nhanh" / "Biểu đồ minh họa" ----------
        // Ẩn thanh tiêu đề tab gốc của TabControl (khó làm đẹp), thay bằng thanh tab phẳng đơn giản ở phía trên.
        // TabControl, 2 TabPage, listBox1, pieChart và thứ tự/sự kiện của code gốc vẫn y nguyên; thanh mới chỉ đổi SelectedIndex.
        private static void DinhTabVaDanhSach(Form f)
        {
            foreach (var ten in new[] { "tabPage1", "pieChart" })
            {
                var tp = Tim<TabPage>(f, ten);
                if (tp == null) continue;
                tp.UseVisualStyleBackColor = false;
                tp.BackColor = Color.White;
            }
            var tc = Tim<TabControl>(f, "tabControl1");
            if (tc != null)
            {
                tc.Appearance = TabAppearance.FlatButtons;   // FlatButtons: không vẽ khung 3D quanh nội dung tab
                tc.SizeMode = TabSizeMode.Fixed;
                tc.ItemSize = new Size(0, 1);                // thu thanh tiêu đề gốc còn 1px (không thấy)
                tc.Padding = new Point(0, 0);
                tc.Multiline = false;
                tc.TabStop = false;

                if (tc.Parent is Control cha)
                {
                    var thanh = new ThanhTabDonGianF4(tc);   // Dock = Top
                    cha.SuspendLayout();
                    try
                    {
                        cha.Controls.Add(thanh);
                        cha.Controls.SetChildIndex(tc, 0);    // TabControl (Fill) xếp trước để được tính bố cục SAU thanh Top
                    }
                    finally
                    {
                        cha.ResumeLayout(true);
                    }
                }
            }
            var lb = Tim<ListBox>(f, "listBox1");
            if (lb != null) { lb.Font = FontDanhSach; lb.BackColor = Color.White; }   // bỏ Times New Roman cho đồng bộ
        }

        // ---------- Thanh trạng thái ----------
        private static void DinhThanhTrangThai(Form f)
        {
            var ss = Tim<StatusStrip>(f, "statusStrip1");
            if (ss == null) return;
            ss.BackColor = Color.White;
            ss.ForeColor = MauChuNhan;
            ss.SizingGrip = false;
            ss.Padding = new Padding(10, 0, 10, 0);
            foreach (var ten in new[] { "toolStripStatusLabel1", "toolStripStatusLabel2" })
            {
                var it = ss.Items.Find(ten, false).FirstOrDefault();
                if (it != null) it.Font = FontNhan;
            }
        }

        // =====================================================================================
        //  TIỆN ÍCH TRÌNH BÀY
        // =====================================================================================

        // Đặt kiểu cột cho 1 bảng: cột 0 = nhãn đầu hàng (rộng cố định), các cột sau theo danh sách
        private static void DinhHang(Form f, string ten, float rongNhan, (SizeType Kieu, float Gia)[] cacCot)
        {
            var t = Tim<TableLayoutPanel>(f, ten);
            if (t == null) return;
            t.Margin = new Padding(0);
            t.ColumnStyles.Clear();
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, rongNhan));
            foreach (var c in cacCot) t.ColumnStyles.Add(new ColumnStyle(c.Kieu, c.Gia));
        }

        private static TableLayoutPanel? DatCot(Form f, string ten, params (SizeType Kieu, float Gia)[] cacCot)
        {
            var t = Tim<TableLayoutPanel>(f, ten);
            if (t == null) return null;
            t.Margin = new Padding(0);
            if (t.ColumnCount < cacCot.Length) t.ColumnCount = cacCot.Length;   // cột đệm cuối (Percent) cần có ô; vị trí các control gốc không đổi
            t.ColumnStyles.Clear();
            foreach (var c in cacCot) t.ColumnStyles.Add(new ColumnStyle(c.Kieu, c.Gia));
            return t;
        }

        private static void DinhNhan(Label? lb, bool dauHang = false)
        {
            if (lb == null) return;
            lb.Font = FontNhan;
            lb.ForeColor = MauChuNhan;
            lb.Anchor = AnchorStyles.Left;
            lb.Margin = new Padding(dauHang ? 0 : 12, 0, 6, 0);
        }

        private static void DinhOCombo(ComboBox? c)
        {
            if (c == null) return;
            c.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            c.Margin = new Padding(0, 3, 0, 3);
        }

        private static void DinhAnhBieuTuong(PictureBox? p)
        {
            if (p == null) return;
            p.Dock = DockStyle.Fill;
            p.SizeMode = PictureBoxSizeMode.Zoom;     // trước đây StretchImage -> méo hình
            p.Margin = new Padding(0, 4, 8, 4);
        }

        private static void NutTrongO(Form f, string ten, (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) m, Padding margin)
        {
            var b = Tim<Krypton.Toolkit.KryptonButton>(f, ten);
            if (b == null) return;
            KieuNut(b, m);
            CanNut(b, margin);
        }

        // Nút lấp đầy chiều ngang ô, cao đều 32
        private static void CanNut(Krypton.Toolkit.KryptonButton b, Padding margin)
        {
            b.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            b.Margin = margin;
            b.Height = 32;
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
            // Nút đang giữ focus (vừa bấm xong, hoặc nút mặc định của form): Krypton lấy nền/viền từ theme (thường trắng) nhưng chữ vẫn
            // lấy từ trạng thái của nút => trước đây nút "Lưu thông tin" sau khi lưu bị chữ trắng trên nền trắng. Ép giống trạng thái thường.
            foreach (var ov in new[] { b.OverrideFocus, b.OverrideDefault })
            {
                ov.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
                ov.Back.Color1 = m.Nen; ov.Back.Color2 = m.Nen;
                ov.Border.Color1 = m.Vien; ov.Border.Color2 = m.Vien;
                ov.Content.ShortText.Color1 = m.Chu;
            }
            // Nút bị khóa (vd. đang lưu / chưa có dữ liệu để xuất): tím rất nhạt, chữ tím nhạt vẫn đọc được
            b.StateDisabled.Back.Color1 = Color.FromArgb(245, 243, 255); b.StateDisabled.Back.Color2 = Color.FromArgb(245, 243, 255);
            b.StateDisabled.Border.Color1 = Color.FromArgb(221, 214, 254); b.StateDisabled.Border.Color2 = Color.FromArgb(221, 214, 254);
            b.StateDisabled.Content.ShortText.Color1 = Color.FromArgb(167, 139, 250);
        }
    }

    /// <summary>
    /// Thẻ trắng bo góc + tiêu đề có thanh nhấn xanh (thay GroupBox). Chỉ để trình bày.
    /// Nếu gán LuoiBang: thẻ vẽ thêm 1 KHUNG BO TRÒN liền mạch quanh bảng (viền tím nhạt + dải tiêu đề cùng màu tiêu đề bảng).
    /// </summary>
    // DesignerCategory("Code"): lớp này chỉ là code, VS không mở bằng Designer và không tạo .resx.
    [System.ComponentModel.DesignerCategory("Code")]
    internal sealed class TheBoGocF4 : Panel
    {
        private static readonly Font FontTieuDe = new Font("Segoe UI Semibold", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Color MauVienKhung = Color.FromArgb(221, 214, 254);
        private static readonly Color MauTieuDeMacDinh = Color.FromArgb(238, 232, 255);
        private string _tieuDe = "";

        public string TieuDe
        {
            get => _tieuDe;
            set { _tieuDe = value ?? ""; Invalidate(); }
        }

        /// <summary>Bảng dữ liệu nằm trong thẻ (nếu có) -> thẻ vẽ khung bo tròn quanh bảng.</summary>
        public DataGridView? LuoiBang { get; set; }

        /// <summary>Khoảng đệm (px thiết kế) giữa khung bo tròn và nội dung; đã được cộng vào Padding của thẻ.</summary>
        public int DemKhung { get; set; }

        public TheBoGocF4()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Color.White;
        }

        protected override void OnPaintBackground(PaintEventArgs e) =>
            e.Graphics.Clear(Parent?.BackColor ?? Color.FromArgb(244, 246, 250));

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width < 8 || Height < 8) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float k = DeviceDpi / 96f;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = TaoDuongBoGoc(r, (int)(20 * k)))
            using (var nen = new SolidBrush(Color.White))
            using (var vien = new Pen(Color.FromArgb(226, 232, 240)))
            {
                g.FillPath(nen, path);
                g.DrawPath(vien, path);
            }
            if (LuoiBang != null) VeKhungBang(g, k);
            if (_tieuDe.Length == 0) return;
            int x = (int)(14 * k), w = Math.Max(3, (int)(3 * k));
            using (var bar = new SolidBrush(Color.FromArgb(37, 99, 235)))
                g.FillRectangle(bar, x, (int)(12 * k), w, (int)(14 * k));
            TextRenderer.DrawText(g, _tieuDe, FontTieuDe, new Point(x + w + (int)(7 * k), (int)(10 * k)), Color.FromArgb(30, 41, 59),
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.Left | TextFormatFlags.SingleLine);
        }

        // Khung bo tròn quanh bảng: 4 góc vuông của bảng nằm lọt trong cung bo nên không bị lẹm/mất góc.
        // Phần đệm trên cùng + hai bên của hàng tiêu đề được tô cùng màu tiêu đề bảng => tiêu đề nhìn như bo tròn liền với khung.
        private void VeKhungBang(Graphics g, float k)
        {
            var luoi = LuoiBang;
            if (luoi == null || luoi.IsDisposed) return;
            int gap = (int)Math.Round(DemKhung * k);
            var kh = new Rectangle(Padding.Left - gap, Padding.Top - gap,
                                   Width - Padding.Horizontal + 2 * gap - 1, Height - Padding.Vertical + 2 * gap - 1);
            if (kh.Width < 24 || kh.Height < 24) return;
            int d = (int)Math.Round(20 * k);   // đường kính cung bo (bán kính 10)

            // 1) Dải tiêu đề: từ mép trên khung đến mép dưới hàng tiêu đề của bảng, màu đọc trực tiếp từ code gốc đặt
            int cao = luoi.ColumnHeadersVisible ? luoi.ColumnHeadersHeight : 0;
            int day = Math.Min(luoi.Top + cao, kh.Bottom);
            var mau = luoi.ColumnHeadersDefaultCellStyle.BackColor;
            if (mau.IsEmpty) mau = MauTieuDeMacDinh;
            var dai = new Rectangle(kh.X, kh.Y, kh.Width, day - kh.Y);
            if (dai.Height > d)
            {
                using var pDai = TaoDuongBoGocTren(dai, d);
                using var bDai = new SolidBrush(mau);
                g.FillPath(bDai, pDai);
            }

            // 2) Viền khung bo tròn liền mạch (vẽ sau cùng để phủ mép dải tiêu đề)
            using var pKhung = TaoDuongBoGoc(kh, d);
            using var pen = new Pen(MauVienKhung);
            g.DrawPath(pen, pKhung);
        }

        private static GraphicsPath TaoDuongBoGoc(Rectangle r, int d)
        {
            var path = new GraphicsPath();
            if (d < 2) { path.AddRectangle(r); return path; }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // Hình chữ nhật chỉ bo 2 góc TRÊN (dải tiêu đề)
        private static GraphicsPath TaoDuongBoGocTren(Rectangle r, int d)
        {
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddLine(r.Right, r.Bottom, r.X, r.Bottom);
            path.CloseFigure();
            return path;
        }
    }

    /// <summary>
    /// Thanh tab phẳng, đơn giản: chữ + gạch chân xanh ở tab đang chọn, đường kẻ mảnh bên dưới.
    /// Chỉ đọc tên các TabPage và đổi SelectedIndex của TabControl gốc — không có logic nghiệp vụ.
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal sealed class ThanhTabDonGianF4 : Control
    {
        private static readonly Font FontChon = new Font("Segoe UI Semibold", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontThuong = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Color MauChon = Color.FromArgb(37, 99, 235);
        private static readonly Color MauThuong = Color.FromArgb(100, 116, 139);
        private static readonly Color MauHover = Color.FromArgb(30, 41, 59);
        private static readonly Color MauDuongKe = Color.FromArgb(226, 232, 240);

        private const TextFormatFlags CoDoChu = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;

        private readonly TabControl _tc;
        private int _hover = -1;

        public ThanhTabDonGianF4(TabControl tc)
        {
            _tc = tc;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
            BackColor = Color.White;
            Height = (int)(34 * DeviceDpi / 96f);
            Dock = DockStyle.Top;
            _tc.SelectedIndexChanged += (s, e) => Invalidate();
        }

        // Vị trí từng tab: luôn đo bằng font đậm (rộng nhất) để vị trí không xê dịch khi đổi tab
        private Rectangle[] TinhCacTab()
        {
            float k = DeviceDpi / 96f;
            int dem = (int)(14 * k);
            var kq = new Rectangle[_tc.TabPages.Count];
            int x = (int)(2 * k);
            for (int i = 0; i < kq.Length; i++)
            {
                int w = TextRenderer.MeasureText(_tc.TabPages[i].Text, FontChon, new Size(int.MaxValue, int.MaxValue), CoDoChu).Width + 2 * dem;
                kq[i] = new Rectangle(x, 0, w, Height);
                x += w;
            }
            return kq;
        }

        private int TimTab(Point p)
        {
            var cacTab = TinhCacTab();
            for (int i = 0; i < cacTab.Length; i++)
                if (cacTab[i].Contains(p)) return i;
            return -1;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            float k = DeviceDpi / 96f;
            using (var pen = new Pen(MauDuongKe))
                g.DrawLine(pen, 0, Height - 1, Width, Height - 1);

            int dayGach = Math.Max(2, (int)Math.Round(2 * k));
            var cacTab = TinhCacTab();
            for (int i = 0; i < cacTab.Length; i++)
            {
                bool chon = i == _tc.SelectedIndex;
                var r = cacTab[i];
                Color mau = chon ? MauChon : (i == _hover ? MauHover : MauThuong);
                var vungChu = new Rectangle(r.X, r.Y, r.Width, r.Height - dayGach);
                TextRenderer.DrawText(g, _tc.TabPages[i].Text, chon ? FontChon : FontThuong, vungChu, mau,
                    CoDoChu | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                if (chon)
                {
                    using var gach = new SolidBrush(MauChon);
                    int le = (int)(6 * k);
                    g.FillRectangle(gach, r.X + le, Height - dayGach, Math.Max(1, r.Width - 2 * le), dayGach);
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int idx = TimTab(e.Location);
            if (idx == _hover) return;
            _hover = idx;
            Cursor = idx >= 0 ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hover == -1) return;
            _hover = -1;
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            int idx = TimTab(e.Location);
            if (idx >= 0 && idx != _tc.SelectedIndex) _tc.SelectedIndex = idx;
        }
    }

    // =====================================================================================
    //  COMBOBOX KIỂU WEB (thay cho vẽ phủ viền): ô chọn bo tròn + danh sách sổ xuống bo tròn
    //  Cách làm: ComboBox gốc vẫn nằm nguyên trong form (cùng tên, Items, SelectedIndex, sự kiện, BackColor, Visible, Enabled do code gốc điều khiển)
    //  nhưng được "ẩn hình" (Region rỗng). Một ô vẽ tay OComboWebF4 đặt đúng vị trí của nó, đọc lại trạng thái của ComboBox gốc để vẽ,
    //  và khi người dùng chọn thì GÁN cb.SelectedIndex => mọi sự kiện SelectedIndexChanged của code gốc chạy y như cũ.
    //  Danh sách sổ xuống là cửa sổ riêng PopupDanhSachF4 (bo tròn, hover, dấu tick mục đang chọn, thanh cuộn mảnh).
    // =====================================================================================
    internal static class VeHinhF4
    {
        public static GraphicsPath BoGoc(RectangleF r, float d)
        {
            d = Math.Max(1f, Math.Min(d, Math.Min(r.Width, r.Height)));
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    [System.ComponentModel.DesignerCategory("Code")]
    internal sealed class OComboWebF4 : Control
    {
        private static readonly Color MauVien = Color.FromArgb(203, 213, 225);
        private static readonly Color MauVienHover = Color.FromArgb(167, 139, 250);
        private static readonly Color MauVienChon = Color.FromArgb(139, 92, 246);
        private static readonly Color MauVienKhoa = Color.FromArgb(226, 232, 240);
        private static readonly Color MauChu = Color.FromArgb(30, 41, 59);
        private static readonly Color MauChuKhoa = Color.FromArgb(148, 163, 184);
        private static readonly Color MauMuiTen = Color.FromArgb(100, 116, 139);

        private readonly ComboBox _cb;
        private readonly List<Control> _chuoi = new List<Control>();
        private Control? _host;
        private bool _hover;
        private PopupDanhSachF4? _popup;
        private long _tDong;

        public static void Gan(ComboBox cb) { _ = new OComboWebF4(cb); }

        private OComboWebF4(ComboBox cb)
        {
            _cb = cb;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.StandardClick, true);
            TabStop = true;
            Font = cb.Font;
            Visible = false;

            cb.TabStop = false;                                  // phím Tab đi qua ô mới
            cb.Region = new Region(new Rectangle(0, 0, 0, 0));   // ẩn hình ComboBox gốc (vẫn tồn tại + hoạt động)

            cb.ParentChanged += (s, e) => GanChuoi();
            cb.LocationChanged += DongBo;
            cb.SizeChanged += DongBo;
            cb.VisibleChanged += DongBo;
            cb.EnabledChanged += DongBo;
            cb.FontChanged += (s, e) => { Font = cb.Font; Invalidate(); };
            cb.SelectedIndexChanged += (s, e) => Invalidate();
            cb.SelectedValueChanged += (s, e) => Invalidate();
            cb.TextChanged += (s, e) => Invalidate();
            cb.BackColorChanged += (s, e) => Invalidate();
            cb.ForeColorChanged += (s, e) => Invalidate();
            cb.DataSourceChanged += (s, e) => Invalidate();
            cb.Disposed += (s, e) => Dispose();
            GanChuoi();
        }

        // Ô vẽ tay nằm ở thẻ chứa (không phải trong TableLayoutPanel để khỏi chen vào ô lưới); bám theo mọi thay đổi vị trí của chuỗi cha
        private void GanChuoi()
        {
            foreach (var c in _chuoi) { c.LocationChanged -= DongBo; c.SizeChanged -= DongBo; c.VisibleChanged -= DongBo; }
            _chuoi.Clear();
            var p = _cb.Parent;
            while (p != null && (p is TableLayoutPanel || p is FlowLayoutPanel)) { _chuoi.Add(p); p = p.Parent; }
            if (p == null) return;
            if (_host != p)
            {
                _host?.Controls.Remove(this);
                _host = p;
                _host.Controls.Add(this);
                BringToFront();
            }
            foreach (var c in _chuoi) { c.LocationChanged += DongBo; c.SizeChanged += DongBo; c.VisibleChanged += DongBo; }
            DongBo(null, EventArgs.Empty);
        }

        private void DongBo(object? sender, EventArgs e)
        {
            if (_host == null || IsDisposed) return;
            var pt = _cb.Location;
            for (var c = _cb.Parent; c != null && c != _host; c = c.Parent) pt.Offset(c.Location);
            SetBounds(pt.X, pt.Y, _cb.Width, _cb.Height);
            Enabled = _cb.Enabled;
            Visible = _cb.Visible;
            if (Visible) BringToFront();
            Invalidate();
        }

        private string LayChu()
        {
            if (_cb.SelectedIndex >= 0 && _cb.SelectedItem != null) return _cb.GetItemText(_cb.SelectedItem);
            return _cb.Text ?? "";
        }

        protected override void OnPaintBackground(PaintEventArgs e) =>
            e.Graphics.Clear(Parent?.BackColor ?? Color.White);

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width < 16 || Height < 12) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float k = DeviceDpi / 96f;
            bool on = Enabled;
            bool mo = _popup != null;
            bool nhan = on && (mo || Focused);

            Color nen = !on ? Color.FromArgb(248, 250, 252)
                            : (_cb.BackColor == SystemColors.Window ? Color.White : _cb.BackColor);
            Color vien = !on ? MauVienKhoa : nhan ? MauVienChon : _hover ? MauVienHover : MauVien;
            Color chu = !on ? MauChuKhoa : (_cb.ForeColor == SystemColors.WindowText ? MauChu : _cb.ForeColor);

            // 1 nét viền đều 4 cạnh (cùng độ dày), bo bán kính 8
            float pw = nhan ? 1.5f : 1f;
            var khung = new RectangleF(pw / 2f, pw / 2f, Width - pw, Height - pw);
            using (var path = VeHinhF4.BoGoc(khung, 16f * k))
            using (var bNen = new SolidBrush(nen))
            using (var pen = new Pen(vien, pw))
            {
                g.FillPath(bNen, path);
                g.DrawPath(pen, path);
            }

            int le = (int)(10 * k), phai = (int)(30 * k);
            TextRenderer.DrawText(g, LayChu(), Font, new Rectangle(le, 0, Math.Max(1, Width - le - phai), Height), chu,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

            // Mũi tên (chevron) mảnh, lật lên khi đang mở
            float cx = Width - 15f * k, cy = Height / 2f, h = 3.2f * k, w = 4.6f * k;
            float dy = mo ? -1f : 1f;
            using var penM = new Pen(on ? MauMuiTen : MauChuKhoa, Math.Max(1.5f, 1.6f * k)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            g.DrawLines(penM, new[] { new PointF(cx - w, cy - dy * h / 2f), new PointF(cx, cy + dy * h / 2f), new PointF(cx + w, cy - dy * h / 2f) });
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = false; Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            Focus();
            if (Environment.TickCount64 - _tDong < 300) return;   // bấm lúc danh sách đang mở: danh sách vừa tự đóng => coi như bấm để đóng
            MoDanhSach();
        }

        protected override bool IsInputKey(Keys keyData) =>
            (keyData & Keys.KeyCode) is Keys.Up or Keys.Down or Keys.Home or Keys.End || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyCode)
            {
                case Keys.F4:
                case Keys.Space:
                    MoDanhSach(); e.Handled = true; break;
                case Keys.Down:
                    if (e.Alt) MoDanhSach(); else Chon(_cb.SelectedIndex + 1);
                    e.Handled = true; break;
                case Keys.Up: Chon(_cb.SelectedIndex - 1); e.Handled = true; break;
                case Keys.Home: Chon(0); e.Handled = true; break;
                case Keys.End: Chon(_cb.Items.Count - 1); e.Handled = true; break;
            }
        }

        // Gõ chữ đầu để nhảy tới mục tương ứng (giống ComboBox gốc)
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            if (char.IsControl(e.KeyChar)) return;
            int n = _cb.Items.Count;
            for (int b = 1; b <= n; b++)
            {
                int i = (Math.Max(_cb.SelectedIndex, -1) + b) % n;
                if (_cb.GetItemText(_cb.Items[i]).StartsWith(e.KeyChar.ToString(), StringComparison.CurrentCultureIgnoreCase))
                { Chon(i); e.Handled = true; return; }
            }
        }

        private void Chon(int i)
        {
            if (i < 0 || i >= _cb.Items.Count || i == _cb.SelectedIndex) return;
            _cb.SelectedIndex = i;      // kích hoạt sự kiện SelectedIndexChanged của code gốc
        }

        private void MoDanhSach()
        {
            if (_popup != null || _cb.Items.Count == 0 || !Enabled) return;
            float k = DeviceDpi / 96f;
            var ds = new List<string>();
            int dai = 0;
            foreach (var it in _cb.Items)
            {
                string t = _cb.GetItemText(it);
                ds.Add(t);
                dai = Math.Max(dai, TextRenderer.MeasureText(t, Font).Width);
            }
            int rong = Math.Max(Width, Math.Min(dai + (int)(52 * k), (int)(520 * k)));
            var pop = new PopupDanhSachF4(ds, _cb.SelectedIndex, rong, k, Font, Chon);

            var o = PointToScreen(new Point(0, Height + (int)(4 * k)));
            var wa = Screen.FromControl(this).WorkingArea;
            if (o.Y + pop.Height > wa.Bottom) o.Y = PointToScreen(Point.Empty).Y - pop.Height - (int)(4 * k);
            if (o.X + pop.Width > wa.Right) o.X = wa.Right - pop.Width;
            pop.Location = o;
            pop.FormClosed += (s, e) => { _tDong = Environment.TickCount64; _popup = null; if (!IsDisposed) Invalidate(); };
            _popup = pop;
            Invalidate();
            if (TopLevelControl is Form f) pop.Show(f); else pop.Show();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _popup?.Close(); foreach (var c in _chuoi) { c.LocationChanged -= DongBo; c.SizeChanged -= DongBo; c.VisibleChanged -= DongBo; } }
            base.Dispose(disposing);
        }
    }

    /// <summary>Danh sách sổ xuống bo tròn: hover tím nhạt, mục đang chọn có nền + dấu tick, thanh cuộn mảnh, hỗ trợ bàn phím + lăn chuột.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    internal sealed class PopupDanhSachF4 : Form
    {
        private const int MaxHienThi = 8;
        private static readonly Color MauVien = Color.FromArgb(221, 214, 254);
        private static readonly Color MauHoverNen = Color.FromArgb(245, 243, 255);
        private static readonly Color MauChonNen = Color.FromArgb(237, 233, 254);
        private static readonly Color MauChonChu = Color.FromArgb(91, 33, 182);
        private static readonly Color MauChu = Color.FromArgb(30, 41, 59);

        private readonly List<string> _ds;
        private readonly Action<int> _chon;
        private readonly Font _fontDam;
        private readonly Font _font;
        private readonly float _k;
        private readonly int _cao, _le, _soHienThi, _dangChon;
        private int _dau, _hover;
        private bool _keo;

        public PopupDanhSachF4(List<string> ds, int dangChon, int rong, float k, Font font, Action<int> chon)
        {
            _ds = ds; _chon = chon; _k = k; _font = font; _dangChon = dangChon;
            _fontDam = new Font("Segoe UI Semibold", font.SizeInPoints, FontStyle.Regular, GraphicsUnit.Point);
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.White;
            DoubleBuffered = true;
            _cao = (int)Math.Round(30 * k);
            _le = (int)Math.Round(5 * k);
            _soHienThi = Math.Min(ds.Count, MaxHienThi);
            ClientSize = new Size(rong, _soHienThi * _cao + 2 * _le);
            _hover = dangChon;
            if (dangChon >= _soHienThi) _dau = Math.Max(0, Math.Min(dangChon - _soHienThi / 2, ds.Count - _soHienThi));
            using (var path = VeHinhF4.BoGoc(new RectangleF(0, 0, Width, Height), 20f * k))
                Region = new Region(path);
            Deactivate += (s, e) => Close();
        }

        protected override CreateParams CreateParams
        {
            get { var cp = base.CreateParams; cp.ExStyle |= 0x80; return cp; }   // WS_EX_TOOLWINDOW: không hiện trong Alt+Tab
        }

        private bool CoCuon => _ds.Count > _soHienThi;
        private int VungCuonX => Width - _le - (int)(14 * _k);

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = VeHinhF4.BoGoc(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), 20f * _k))
            using (var pen = new Pen(MauVien))
                g.DrawPath(pen, path);

            int cuonW = CoCuon ? (int)(10 * _k) : 0;
            for (int v = 0; v < _soHienThi; v++)
            {
                int i = _dau + v;
                var r = new Rectangle(_le, _le + v * _cao, Width - 2 * _le - cuonW, _cao);
                bool chon = i == _dangChon, hov = i == _hover;
                if (chon || hov)
                {
                    using var p = VeHinhF4.BoGoc(new RectangleF(r.X, r.Y + 1, r.Width, r.Height - 2), 16f * _k);
                    using var b = new SolidBrush(chon ? MauChonNen : MauHoverNen);
                    g.FillPath(b, p);
                }
                TextRenderer.DrawText(g, _ds[i], chon ? _fontDam : _font,
                    new Rectangle(r.X + (int)(10 * _k), r.Y, r.Width - (int)(34 * _k), r.Height),
                    chon ? MauChonChu : MauChu,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis |
                    TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
                if (chon)   // dấu tick
                {
                    float cx = r.Right - 16f * _k, cy = r.Y + r.Height / 2f, s = 4f * _k;
                    using var pt = new Pen(MauChonChu, Math.Max(1.6f, 1.8f * _k)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                    g.DrawLines(pt, new[] { new PointF(cx - s, cy), new PointF(cx - s * 0.3f, cy + s * 0.75f), new PointF(cx + s, cy - s * 0.7f) });
                }
            }

            if (CoCuon)   // thanh cuộn mảnh bo tròn
            {
                int tong = _soHienThi * _cao;
                int thumb = Math.Max((int)(24 * _k), tong * _soHienThi / _ds.Count);
                int y = _le + (tong - thumb) * _dau / Math.Max(1, _ds.Count - _soHienThi);
                var t = new RectangleF(Width - _le - 7f * _k, y, 4f * _k, thumb);
                using var p = VeHinhF4.BoGoc(t, 4f * _k);
                using var b = new SolidBrush(_keo ? Color.FromArgb(148, 163, 184) : Color.FromArgb(203, 213, 225));
                g.FillPath(b, p);
            }
        }

        private int ViTri(Point p)
        {
            if (p.Y < _le || p.Y >= _le + _soHienThi * _cao) return -1;
            if (CoCuon && p.X >= VungCuonX) return -1;
            return _dau + (p.Y - _le) / _cao;
        }

        private void DatDauTheoY(int y)
        {
            float t = Math.Max(0f, Math.Min(1f, (y - _le) / (float)(_soHienThi * _cao)));
            _dau = (int)Math.Round(t * (_ds.Count - _soHienThi));
            Invalidate();
        }

        private void CuonDen(int i)
        {
            if (i < _dau) _dau = i;
            else if (i >= _dau + _soHienThi) _dau = i - _soHienThi + 1;
            _dau = Math.Max(0, Math.Min(_dau, _ds.Count - _soHienThi));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_keo) { DatDauTheoY(e.Y); return; }
            int i = ViTri(e.Location);
            if (i != _hover) { _hover = i; Invalidate(); }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            if (CoCuon && e.X >= VungCuonX) { _keo = true; Capture = true; DatDauTheoY(e.Y); return; }
            int i = ViTri(e.Location);
            if (i >= 0) { _chon(i); Close(); }
        }

        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _keo = false; Capture = false; Invalidate(); }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!CoCuon) return;
            _dau = Math.Max(0, Math.Min(_dau - Math.Sign(e.Delta) * 2, _ds.Count - _soHienThi));
            Invalidate();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            int n = _ds.Count;
            switch (keyData)
            {
                case Keys.Escape: Close(); return true;
                case Keys.Enter: if (_hover >= 0) _chon(_hover); Close(); return true;
                case Keys.Down: _hover = Math.Min(n - 1, _hover + 1); break;
                case Keys.Up: _hover = Math.Max(0, _hover - 1); break;
                case Keys.Home: _hover = 0; break;
                case Keys.End: _hover = n - 1; break;
                case Keys.PageDown: _hover = Math.Min(n - 1, _hover + _soHienThi); break;
                case Keys.PageUp: _hover = Math.Max(0, _hover - _soHienThi); break;
                default: return base.ProcessCmdKey(ref msg, keyData);
            }
            CuonDen(_hover);
            Invalidate();
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _fontDam.Dispose();
            base.Dispose(disposing);
        }
    }
}