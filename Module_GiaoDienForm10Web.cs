using System.Diagnostics;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CHO Form10_NhatKy (Nhật ký phần mềm) — CHỈ TRÌNH BÀY, KHÔNG CÓ LOGIC NGHIỆP VỤ
    //  Cùng ngôn ngữ thiết kế với Form4 (Module_GiaoDienForm4Web): nền trang xám nhạt, thẻ trắng bo góc,
    //  combo bo tròn, nút theme tím nhạt, bảng có khung bo tròn liền mạch + tiêu đề tím nhạt.
    //
    //  BỐ CỤC GỌN (24/11) — ưu tiên diện tích cho lưới:
    //   ┌ Thẻ "thanh lọc" (1 hàng, tự xuống dòng khi hẹp) ───────────────────────────────────────────┐
    //   │ Máy tính [▾]  Tài khoản [▾]  Từ ngày [..] đến [..]  [Tiến hành lọc] [Làm mới]          (?) │
    //   └────────────────────────────────────────────────────────────────────────────────────────────┘
    //   ┌ Thẻ lưới ──────────────────────────────────────────────────────────────────────────────────┐
    //   │ [ bảng nhật ký ]                                                                           │
    //   │ Sắp xếp (•) Cũ trước ( ) Mới trước     Trang x/y [Trở lại][Tiếp theo]  Số dòng/trang [..][Áp dụng] │
    //   └────────────────────────────────────────────────────────────────────────────────────────────┘
    //  Trước đây: hàng 4 thẻ cao 192 px (ảnh 128 px + Bộ lọc + Phân trang + Sắp xếp) + dải tiêu đề thẻ lưới 36 px.
    //
    //  - Lớp static độc lập, KHÔNG sửa Designer/.resx: chỉ chuyển các control gốc sang khung mới (giữ nguyên tên, kiểu, sự kiện).
    //    Khung chia splitContainer1 thu Panel1 lại (các GroupBox/TableLayoutPanel gốc nằm rỗng trong đó, không hiện).
    //  - Kích thước tự đặt ở đây đã nhân DPI (hệ số k); riêng Min/MaxSize của nút + hàng tableLayoutPanel1 để px 100%
    //    vì Module_TyLeDpi nhân lúc Load (Form10 nằm trong FormCanHieuChinhDpi).
    //  - Mọi bước bọc try/catch: lỗi trình bày không bao giờ làm form không mở được.
    //
    //  Dùng trong Form10_NhatKy:
    //     constructor, ngay SAU InitializeComponent():   Module_GiaoDienForm10Web.ApDung(this);
    //     CauHinhStyleWeb(dgv):                          Module_GiaoDienForm10Web.StyleLuoi(dgv);
    //     CapNhatMauRadioSapXep():                       Module_GiaoDienForm10Web.CapNhatRadio(radio);
    // =====================================================================================
    internal static class Module_GiaoDienForm10Web
    {
        // ---------- Bảng màu / font: lấy đúng giá trị của Form4 ----------
        private static readonly Color MauNen = Color.FromArgb(244, 246, 250);
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);
        private static readonly Color MauChuDam = Color.FromArgb(30, 41, 59);
        private static readonly Color MauTim = Color.FromArgb(109, 40, 217);
        private static readonly Color MauVienO = Color.FromArgb(203, 213, 225);
        private static readonly Color MauVienChon = Color.FromArgb(139, 92, 246);
        private static readonly Color MauVach = Color.FromArgb(226, 232, 240);
        private static readonly Font FontNhan = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontO = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new Font(Module_HeThong.TenFontHeThongDam, 9F, FontStyle.Regular, GraphicsUnit.Point);
        // Font lưới: giữ đúng font hệ thống mà Form10 vẫn dùng cho lưới
        private static readonly Font FontLuoiTieuDe = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Bold);
        private static readonly Font FontLuoiO = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Regular);
        // Khoảng đệm giữa khung bo tròn và bảng (px thiết kế): đủ để 4 góc vuông của bảng nằm lọt trong cung bo
        private const int DemKhungBang = 5;
        private const int CaoHangDieuKhien = 36;   // chiều cao 1 hàng điều khiển (thanh lọc / chân bảng), px 100%
        private const int CaoNut = 30;             // chiều cao nút, px 100% (Module_TyLeDpi nhân lúc Load)
        private static readonly Color MauTieuDeBang = Color.FromArgb(238, 232, 255);
        private static readonly Color MauChuTieuDeBang = Color.FromArgb(75, 20, 130);
        // (Nền, Hover, Nhấn, Viền, Chữ) — theme tím nhạt của Form4
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutTim =
            (Color.FromArgb(237, 233, 254), Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253), Color.FromArgb(196, 181, 253), Color.FromArgb(109, 40, 217));
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutTimDam =
            (Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253), Color.FromArgb(167, 139, 250), Color.FromArgb(167, 139, 250), Color.FromArgb(91, 33, 182));
        // ===================== ĐIỂM VÀO =====================
        public static void ApDung(Form f)
        {
            f.SuspendLayout();
            try
            {
                f.BackColor = MauNen;
                An(() => DinhBoCuc(f), "BoCuc");
                An(() => DinhKieuDieuKhien(f), "KieuDieuKhien");
                An(() => DungKhungLuoi(f), "KhungLuoi");     // trước thanh lọc: thẻ lưới phải có sẵn để xếp thứ tự Dock
                An(() => DungThanhLoc(f), "ThanhLoc");
                An(() => BoTronCacCombo(f), "Combo");         // sau khi chuyển combo sang khung mới (ô bo tròn bám theo chuỗi cha)
                An(() => DinhThanhTrangThai(f), "TrangThai");
            }
            finally
            {
                f.ResumeLayout(true);
            }
        }
        private static void An(Action viec, string ten)
        {
            try { viec(); }
            catch (Exception ex) { Debug.WriteLine($"[Module_GiaoDienForm10Web.{ten}] {ex.Message}"); }
        }
        private static T? Tim<T>(Control goc, string ten) where T : Control
            => goc.Controls.Find(ten, true).OfType<T>().FirstOrDefault();
        private static IEnumerable<T> TatCa<T>(Control goc) where T : Control
        {
            foreach (Control c in goc.Controls)
            {
                if (c is T t) yield return t;
                foreach (var con in TatCa<T>(c)) yield return con;
            }
        }
        private static float HeSo(Control c) => Math.Max(1f, c.DeviceDpi / 96f);
        private static int S(float k, int v) => (int)Math.Round(v * k);
        private static Padding P(float k, int trai, int tren, int phai, int duoi) => new Padding(S(k, trai), S(k, tren), S(k, phai), S(k, duoi));
        // ===================== BỐ CỤC TỔNG =====================
        private static void DinhBoCuc(Form f)
        {
            float k = HeSo(f);
            // Hàng 0 = nội dung (ăn hết), hàng 1 = thanh trạng thái cao cố định (bản gốc chia theo % nên cao thấp theo cửa sổ)
            var t1 = Tim<TableLayoutPanel>(f, "tableLayoutPanel1");
            if (t1 != null)
            {
                t1.BackColor = MauNen;
                t1.Padding = P(k, 10, 8, 10, 2);
                t1.RowStyles.Clear();
                t1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                t1.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));   // px 100% (Module_TyLeDpi nhân)
            }
            var sc = Tim<SplitContainer>(f, "splitContainer1");
            if (sc != null)
            {
                sc.BackColor = MauNen;
                sc.Panel2.BackColor = MauNen;
                sc.Margin = new Padding(0);
                sc.IsSplitterFixed = true;
                sc.Panel1Collapsed = true;   // thanh lọc + lưới cùng nằm ở Panel2 (Dock Top/Fill), chiều cao thanh lọc theo nội dung
            }
        }
        // ===================== KIỂU TỪNG ĐIỀU KHIỂN (chưa đụng bố cục) =====================
        private static void DinhKieuDieuKhien(Form f)
        {
            float k = HeSo(f);
            foreach (var ten in new[] { "label4", "label2", "label3", "label5", "label1" })
                DinhNhan(Tim<Label>(f, ten));
            // Chữ nhãn gọn, đúng nghĩa (code nghiệp vụ không đọc Text các nhãn này)
            if (Tim<Label>(f, "label3") is Label tu) tu.Text = "Từ ngày";
            if (Tim<Label>(f, "label1") is Label soDong) soDong.Text = "Số dòng/trang";   // ô này là số dòng mỗi trang (mặc định 500)
            foreach (var ten in new[] { "comboBox1_MayTinh", "comboBox_LocTaiKhoan" })
            {
                var c = Tim<ComboBox>(f, ten);
                if (c == null) continue;
                c.Font = FontO;                     // phải đặt TRƯỚC khi gắn ô bo tròn (nó chép font của ComboBox)
                c.Anchor = AnchorStyles.Left;
                c.Margin = new Padding(0);
                c.Width = S(k, 160);
                c.DropDownWidth = Math.Max(c.DropDownWidth, S(k, 240));   // tên máy/tài khoản dài vẫn đọc đủ khi thả xuống
            }
            foreach (var ten in new[] { "kryptonDateTimePicker1_NgayThangNamBatDau", "kryptonDateTimePicker1_NgayThangNamKetThuc" })
            {
                var d = Tim<Krypton.Toolkit.KryptonDateTimePicker>(f, ten);
                if (d == null) continue;
                d.Anchor = AnchorStyles.Left;
                d.Margin = new Padding(0);
                d.Width = S(k, 120);
                d.StateCommon.Back.Color1 = Color.White;
                KieuOKrypton(d.StateCommon.Border, d.StateActive.Border);
            }
            var o = Tim<Krypton.Toolkit.KryptonTextBox>(f, "textBox_SoDongHienThi");
            if (o != null)
            {
                o.Anchor = AnchorStyles.Left;
                o.Margin = new Padding(0);   // đặt lại cả lề dọc (lỗi Krypton nhân lặp lề trên/dưới ở DPI ≠ 100%)
                o.Width = S(k, 64);
                o.TextAlign = HorizontalAlignment.Center;
                o.StateCommon.Back.Color1 = Color.White;
                KieuOKrypton(o.StateCommon.Border, o.StateActive.Border);
                o.StateCommon.Content.Font = FontO;
                o.StateCommon.Content.Color1 = MauChuDam;
            }
            DinhNut(f, "kryptonButton1_LocTheoNgayThangNam", NutTimDam);
            DinhNut(f, "kryptonButton1_LamMoiBoLoc", NutTim);
            DinhNut(f, "kryptonButton_TroLai", NutTim);
            DinhNut(f, "kryptonButton_TiepTheo", NutTim);
            DinhNut(f, "kryptonButton_ApDungSoTrang", NutTimDam);
            // "Trang x / y" (chữ do logic điền)
            if (Tim<Label>(f, "label_Trang") is Label trang)
            {
                trang.Font = FontNut;
                trang.ForeColor = MauTim;
                trang.AutoSize = true;
                trang.Anchor = AnchorStyles.Left;
                trang.Margin = new Padding(0);
            }
            // Radio sắp xếp: chữ nói rõ thứ tự thời gian (A–Z = cũ nhất trước; code chỉ đọc Checked)
            if (Tim<RadioButton>(f, "radioButton1_TuAZ") is RadioButton az) az.Text = "Cũ trước (A→Z)";
            if (Tim<RadioButton>(f, "radioButton1_TuZA") is RadioButton za) za.Text = "Mới trước (Z→A)";
            foreach (var ten in new[] { "radioButton1_TuAZ", "radioButton1_TuZA" })
            {
                var rb = Tim<RadioButton>(f, ten);
                if (rb == null) continue;
                rb.AutoSize = true;
                rb.Anchor = AnchorStyles.Left;
                rb.Margin = new Padding(0);
                rb.Cursor = Cursors.Hand;
                CapNhatRadio(rb);
            }
        }
        // ===================== THANH LỌC (thẻ trên cùng) =====================
        private static void DungThanhLoc(Form f)
        {
            var sc = Tim<SplitContainer>(f, "splitContainer1");
            if (sc == null) return;
            float k = HeSo(f);
            int khe = S(k, 14);    // khoảng cách giữa các cụm
            var the = new TheBoGocF4
            {
                Name = "the_ThanhLoc",
                Dock = DockStyle.Top,
                TieuDe = "",
                Margin = new Padding(0),
                Padding = P(k, 14, 7, 10, 7),
                Height = S(k, CaoHangDieuKhien + 14)
            };
            // [cụm lọc tự xuống dòng | biểu tượng hướng dẫn]
            var bang = new TableLayoutPanel
            {
                Name = "bang_ThanhLoc",
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = Color.White
            };
            bang.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bang.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bang.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            var luong = new FlowLayoutPanel
            {
                Name = "luong_ThanhLoc",
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = Color.White
            };
            var cum = new List<Control?>
            {
                Cum(k, new Padding(0, 0, khe, 0), (Tim<Label>(f, "label4"), S(k, 6)), (Tim<ComboBox>(f, "comboBox1_MayTinh"), 0)),
                Cum(k, new Padding(0, 0, khe, 0), (Tim<Label>(f, "label2"), S(k, 6)), (Tim<ComboBox>(f, "comboBox_LocTaiKhoan"), 0)),
                Cum(k, new Padding(0, 0, khe, 0),
                    (Tim<Label>(f, "label3"), S(k, 6)), (Tim<Control>(f, "kryptonDateTimePicker1_NgayThangNamBatDau"), S(k, 6)),
                    (Tim<Label>(f, "label5"), S(k, 6)), (Tim<Control>(f, "kryptonDateTimePicker1_NgayThangNamKetThuc"), 0)),
                Cum(k, new Padding(0),
                    (Tim<Control>(f, "kryptonButton1_LocTheoNgayThangNam"), S(k, 8)), (Tim<Control>(f, "kryptonButton1_LamMoiBoLoc"), 0))
            };
            int tab = 0;
            foreach (var c in cum)
                if (c != null) { c.TabIndex = tab++; luong.Controls.Add(c); }
            bang.Controls.Add(luong, 0, 0);
            // Ảnh hướng dẫn (bấm để xem hướng dẫn): trước chiếm cả thẻ 128 px, nay là biểu tượng nhỏ cuối thanh lọc
            var anh = Tim<PictureBox>(f, "pictureBox1");
            if (anh != null)
            {
                anh.Parent?.Controls.Remove(anh);
                anh.Dock = DockStyle.None;
                anh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                anh.Size = new Size(S(k, 30), S(k, 30));
                anh.Margin = new Padding(S(k, 8), S(k, 3), 0, 0);   // canh giữa hàng đầu (36 - 30) / 2
                anh.SizeMode = PictureBoxSizeMode.Zoom;
                anh.BackColor = Color.White;
                anh.Cursor = Cursors.Hand;
                var tip = new ToolTip();
                tip.SetToolTip(anh, "Xem hướng dẫn sử dụng trang Nhật ký");
                f.Disposed += (s, e) => tip.Dispose();
                bang.Controls.Add(anh, 1, 0);
            }
            the.Controls.Add(bang);
            var vach = new Panel { Name = "khe_ThanhLoc", Dock = DockStyle.Top, Height = S(k, 8), BackColor = MauNen, Margin = new Padding(0) };
            var p2 = sc.Panel2;
            p2.SuspendLayout();
            try
            {
                // Thứ tự Dock: control thêm sau được xếp trước => thẻ lọc trên cùng, khe 8 px, rồi thẻ lưới lấp phần còn lại
                p2.Controls.Add(vach);
                p2.Controls.Add(the);
                foreach (Control c in p2.Controls)
                    if (c.Dock == DockStyle.Fill) c.BringToFront();
            }
            finally { p2.ResumeLayout(true); }
            // Chiều cao thẻ lọc = chiều cao cụm lọc đã tự xuống dòng theo bề ngang hiện tại (rộng: 1 hàng, hẹp: 2–3 hàng)
            bool choCan = false;
            void Can()
            {
                choCan = false;
                if (the.IsDisposed || f.WindowState == FormWindowState.Minimized) return;
                int rongAnh = anh == null ? 0 : anh.Width + anh.Margin.Horizontal;
                int rong = the.ClientSize.Width - the.Padding.Horizontal - rongAnh;
                if (rong < 80) return;
                int cao = Math.Max(luong.GetPreferredSize(new Size(rong, 0)).Height, S(k, CaoHangDieuKhien));
                cao += the.Padding.Vertical;
                if (the.Height != cao) the.Height = cao;
            }
            void YeuCauCan()
            {
                if (choCan || the.IsDisposed) return;
                if (!the.IsHandleCreated) { Can(); return; }
                choCan = true;   // gộp nhiều lần đổi cỡ trong 1 lượt dàn trang thành 1 lần canh, chạy sau khi dàn trang xong
                try { the.BeginInvoke(new Action(Can)); } catch (InvalidOperationException) { choCan = false; }
            }
            the.SizeChanged += (s, e) => YeuCauCan();
            luong.Layout += (s, e) => YeuCauCan();   // nút đổi chữ ("..."), DPI đổi => bề rộng cụm đổi
            f.Load += (s, e) => YeuCauCan();
        }
        /// <summary>
        /// 1 cụm điều khiển không bị tách khi thanh lọc xuống dòng: bảng 1 hàng cao cố định, các control canh giữa theo chiều dọc.
        /// Mỗi phần tử: (control, khoảng cách sau nó).
        /// </summary>
        private static TableLayoutPanel? Cum(float k, Padding margin, params (Control? Ctl, int Khe)[] cacO)
        {
            var co = cacO.Where(x => x.Ctl != null).ToArray();
            if (co.Length == 0) return null;
            var t = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = co.Length,
                RowCount = 1,
                Margin = margin,
                Padding = new Padding(0),
                BackColor = Color.White
            };
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, S(k, CaoHangDieuKhien)));
            for (int i = 0; i < co.Length; i++)
            {
                var c = co[i].Ctl!;
                t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                c.Parent?.Controls.Remove(c);
                c.Dock = DockStyle.None;
                c.Margin = new Padding(0, 0, co[i].Khe, 0);
                c.TabIndex = i;
                t.Controls.Add(c, i, 0);
            }
            return t;
        }
        // ===================== LƯỚI + CHÂN BẢNG (sắp xếp, phân trang) =====================
        // Bảng nằm gọn trong 1 thẻ có KHUNG BO TRÒN liền mạch; không còn dải tiêu đề thẻ (tiêu đề cột của bảng đã đủ rõ).
        private static void DungKhungLuoi(Form f)
        {
            var luoi = Tim<DataGridView>(f, "kryptonDataGridView1");
            if (luoi == null || luoi.Parent is not Control cha) return;
            float k = HeSo(f);
            int gap = S(k, DemKhungBang);
            luoi.BorderStyle = BorderStyle.None;
            luoi.Margin = new Padding(0);
            var the = new TheBoGocF4
            {
                Name = "the_kryptonDataGridView1",
                Dock = DockStyle.Fill,
                TieuDe = "",
                DemKhung = DemKhungBang,
                KhungTheoLuoi = true,
                Padding = new Padding(S(k, 10) + gap, S(k, 10) + gap, S(k, 10) + gap, S(k, 6)),
                LuoiBang = luoi,
                TabIndex = 1
            };
            // Chân bảng: [Sắp xếp ...] ........ [Trang x/y][Trở lại][Tiếp theo] | [Số dòng/trang][ô][Áp dụng]
            var chan = new TableLayoutPanel
            {
                Name = "chan_kryptonDataGridView1",
                Dock = DockStyle.Bottom,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(0, gap + S(k, 4), 0, 0),
                BackColor = Color.White
            };
            chan.Height = S(k, CaoHangDieuKhien) + chan.Padding.Vertical;
            chan.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            chan.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            chan.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            var nhanSapXep = new Label { Name = "label_SapXep", Text = "Sắp xếp:", AutoSize = true };
            DinhNhan(nhanSapXep);
            var cumSapXep = Cum(k, new Padding(0),
                (nhanSapXep, S(k, 8)), (Tim<Control>(f, "radioButton1_TuAZ"), S(k, 12)), (Tim<Control>(f, "radioButton1_TuZA"), 0));
            var vach = new Panel { Name = "vach_PhanTrang", Width = Math.Max(1, S(k, 1)), Height = S(k, 20), BackColor = MauVach, Anchor = AnchorStyles.Left };
            var cumTrang = Cum(k, new Padding(0),
                (Tim<Control>(f, "label_Trang"), S(k, 10)),
                (Tim<Control>(f, "kryptonButton_TroLai"), S(k, 6)), (Tim<Control>(f, "kryptonButton_TiepTheo"), S(k, 14)),
                (vach, S(k, 14)),
                (Tim<Control>(f, "label1"), S(k, 6)), (Tim<Control>(f, "textBox_SoDongHienThi"), S(k, 6)),
                (Tim<Control>(f, "kryptonButton_ApDungSoTrang"), 0));
            if (cumSapXep != null) { cumSapXep.Anchor = AnchorStyles.Left; cumSapXep.TabIndex = 0; chan.Controls.Add(cumSapXep, 0, 0); }
            if (cumTrang != null) { cumTrang.Anchor = AnchorStyles.Right; cumTrang.TabIndex = 1; chan.Controls.Add(cumTrang, 1, 0); }
            cha.SuspendLayout();
            try
            {
                cha.Controls.Remove(luoi);
                cha.Controls.Add(the);
                the.Controls.Add(luoi);     // Fill: xếp sau cùng
                the.Controls.Add(chan);     // Bottom: thêm sau => xếp trước, nằm dưới bảng
                luoi.Dock = DockStyle.Fill;
                luoi.TabIndex = 0;
                chan.TabIndex = 1;
                // Code gốc đặt màu / chiều cao tiêu đề bảng lúc chạy (CaiDatCot) -> thẻ vẽ lại để dải tiêu đề luôn khớp màu
                luoi.ColumnHeadersHeightChanged += (s, e) => the.Invalidate();
                luoi.ColumnHeadersDefaultCellStyleChanged += (s, e) => the.Invalidate();
                luoi.DataBindingComplete += (s, e) => the.Invalidate();
            }
            finally
            {
                cha.ResumeLayout(true);
            }
            // Hẹp không đủ 1 hàng: cụm phân trang xuống hàng 2 (canh trái) thay vì đè lên cụm sắp xếp
            int caoHang = S(k, CaoHangDieuKhien);
            bool? haiHang = null;
            void CanChan()
            {
                if (chan.IsDisposed || cumSapXep == null || cumTrang == null || chan.ClientSize.Width < 50) return;
                bool moi = chan.ClientSize.Width < cumSapXep.PreferredSize.Width + cumTrang.PreferredSize.Width + S(k, 24);
                if (moi == haiHang) return;
                haiHang = moi;
                chan.SuspendLayout();
                try
                {
                    chan.RowStyles.Clear();
                    chan.RowStyles.Add(new RowStyle(SizeType.Absolute, caoHang));
                    if (moi) chan.RowStyles.Add(new RowStyle(SizeType.Absolute, caoHang));
                    chan.RowCount = moi ? 2 : 1;
                    chan.SetCellPosition(cumTrang, new TableLayoutPanelCellPosition(moi ? 0 : 1, moi ? 1 : 0));
                    chan.SetColumnSpan(cumTrang, moi ? 2 : 1);
                    cumTrang.Anchor = moi ? AnchorStyles.Left : AnchorStyles.Right;
                    chan.Height = caoHang * chan.RowCount + chan.Padding.Vertical;
                }
                finally { chan.ResumeLayout(true); }
            }
            // Đổi chiều cao NGAY trong SizeChanged (giữa lượt dàn trang của thẻ) thì thẻ không xếp lại => chân bảng tràn xuống dưới.
            // => gộp và chạy sau khi lượt dàn trang xong.
            bool choCan = false;
            void YeuCauCanChan()
            {
                if (choCan || chan.IsDisposed) return;
                if (!chan.IsHandleCreated) { CanChan(); return; }
                choCan = true;
                try { chan.BeginInvoke(new Action(() => { choCan = false; CanChan(); })); } catch (InvalidOperationException) { choCan = false; }
            }
            chan.SizeChanged += (s, e) => YeuCauCanChan();
            if (cumTrang != null) cumTrang.SizeChanged += (s, e) => YeuCauCanChan();   // nút đổi chữ ("Đang tải...") => rộng hơn
        }
        /// <summary>Radio sắp xếp: mục đang chọn = chữ tím đậm, mục còn lại = chữ xám (bản gốc tô xanh/đỏ).</summary>
        public static void CapNhatRadio(RadioButton rb)
        {
            rb.Font = rb.Checked ? FontNut : FontNhan;
            rb.ForeColor = rb.Checked ? MauTim : MauChuNhan;
        }
        // ===================== COMBO BO TRÒN (dùng OComboWebF4 của Form4) =====================
        private static void BoTronCacCombo(Form f)
        {
            foreach (var cb in TatCa<ComboBox>(f).ToList())   // ToList: Gan thêm control vào thẻ trong lúc duyệt
                if (cb.GetType() == typeof(ComboBox))
                    OComboWebF4.Gan(cb);
        }
        /// <summary>
        /// Style lưới nhật ký giống 2 bảng của Form4: tiêu đề tím nhạt PHẲNG (Krypton mặc định vẽ chuyển sắc + viền hở ở ô đầu),
        /// chữ tiêu đề tím đậm, đường kẻ dòng nhạt, dòng chọn nền xanh rất nhạt. Gọi từ Form10.CauHinhStyleWeb.
        /// </summary>
        public static void StyleLuoi(DataGridView dgv)
        {
            dgv.RowTemplate.Height = 32;
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgv.AllowUserToResizeRows = false;
            dgv.ColumnHeadersHeight = 40;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            dgv.GridColor = Color.FromArgb(235, 238, 243);
            dgv.RowsDefaultCellStyle.BackColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
            dgv.BackgroundColor = Color.White;       // vùng trống dưới dòng cuối: trắng liền với thẻ, không lộ nền xám
            dgv.Margin = new Padding(0);
            dgv.ColumnHeadersDefaultCellStyle.BackColor = MauTieuDeBang;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = MauChuTieuDeBang;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = MauTieuDeBang;
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = MauChuTieuDeBang;
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (dgv is not Krypton.Toolkit.KryptonDataGridView k) return;
            k.GridStyles.Style = Krypton.Toolkit.DataGridViewStyle.List;
            k.StateCommon.Background.Color1 = Color.White;
            k.StateCommon.HeaderColumn.Content.Padding = new Padding(6, 6, 6, 6);
            k.StateCommon.HeaderColumn.Content.Font = FontLuoiTieuDe;
            k.StateCommon.HeaderColumn.Content.Color1 = MauChuTieuDeBang;
            k.StateCommon.DataCell.Content.Font = FontLuoiO;
            k.StateCommon.DataCell.Content.Color1 = MauChuDam;
            k.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            k.StateCommon.DataCell.Border.Color1 = Color.FromArgb(235, 238, 243);
            k.StateCommon.DataCell.Border.Width = 1;
            k.StateCommon.HeaderRow.Back.Color1 = Color.FromArgb(250, 248, 255);
            k.StateCommon.HeaderRow.Back.Color2 = Color.FromArgb(250, 248, 255);
            k.StateCommon.HeaderRow.Border.Color1 = Color.FromArgb(237, 233, 254);
            k.StateSelected.DataCell.Back.Color1 = Color.FromArgb(239, 246, 255);
            k.StateSelected.DataCell.Back.Color2 = Color.FromArgb(239, 246, 255);
            k.StateSelected.DataCell.Content.Color1 = MauChuDam;
            // Hàng tiêu đề: nền phẳng + viền cùng màu nền => khung bo tròn + tiêu đề hợp thành 1 đường viền liền mạch
            TieuDePhang(k.StateCommon.HeaderColumn.Back, k.StateCommon.HeaderColumn.Border);
            TieuDePhang(k.StateTracking.HeaderColumn.Back, k.StateTracking.HeaderColumn.Border);   // rê chuột lên tiêu đề cũng không đổi sang chuyển sắc
            TieuDePhang(k.StatePressed.HeaderColumn.Back, k.StatePressed.HeaderColumn.Border);
        }
        private static void TieuDePhang(Krypton.Toolkit.PaletteBack back, Krypton.Toolkit.PaletteBorder vien)
        {
            back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            back.Color1 = MauTieuDeBang; back.Color2 = MauTieuDeBang;
            vien.Color1 = MauTieuDeBang; vien.Color2 = MauTieuDeBang;
        }
        // ===================== THANH TRẠNG THÁI =====================
        private static void DinhThanhTrangThai(Form f)
        {
            var ss = Tim<StatusStrip>(f, "statusStrip1");
            if (ss == null) return;
            ss.BackColor = Color.White;
            ss.ForeColor = MauChuNhan;
            ss.SizingGrip = false;
            ss.Padding = new Padding(10, 0, 10, 0);
            foreach (var ten in new[] { "toolStripStatusLabel1", "toolStripStatusLabel2", "toolStripStatusLabel3", "toolStripStatusLabel4" })
            {
                var it = ss.Items.Find(ten, false).FirstOrDefault();
                if (it != null) it.Font = FontNhan;
            }
            // Liên kết "Thông tin người dùng": màu tím cùng theme
            if (ss.Items.Find("thongTinNguoiDung_toolstrip", false).FirstOrDefault() is ToolStripStatusLabel lk)
            {
                lk.Font = FontNhan;
                lk.LinkColor = MauTim;
                lk.VisitedLinkColor = MauTim;
                lk.ActiveLinkColor = Color.FromArgb(220, 38, 38);
            }
        }
        // ===================== TIỆN ÍCH TRÌNH BÀY =====================
        private static void DinhNhan(Label? lb)
        {
            if (lb == null) return;
            lb.Font = FontNhan;
            lb.ForeColor = MauChuNhan;
            lb.AutoSize = true;
            lb.Anchor = AnchorStyles.Left;
            lb.Margin = new Padding(0);
        }
        // Ô nhập Krypton: nền trắng, viền xám bo góc 8, khi đang gõ viền chuyển tím (cùng màu viền combo đang chọn)
        private static void KieuOKrypton(Krypton.Toolkit.PaletteBorder vien, Krypton.Toolkit.PaletteBorder vienActive)
        {
            vien.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            vien.Color1 = MauVienO; vien.Color2 = MauVienO;
            vien.Rounding = 8F;
            vien.Width = 1;
            vienActive.Color1 = MauVienChon; vienActive.Color2 = MauVienChon;
        }
        // Nút gọn: rộng theo nội dung (icon + chữ), cao đều CaoNut; Min/MaxSize để px 100% (Module_TyLeDpi nhân lúc Load)
        private static void DinhNut(Form f, string ten, (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) m)
        {
            var b = Tim<Krypton.Toolkit.KryptonButton>(f, ten);
            if (b == null) return;
            KieuNut(b, m);
            b.Dock = DockStyle.None;
            b.Anchor = AnchorStyles.Left;
            b.Margin = new Padding(0);
            b.StateCommon.Content.Padding = new Padding(10, 1, 10, 1);
            b.MinimumSize = new Size(84, CaoNut);
            b.MaximumSize = new Size(0, CaoNut);
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            b.AutoSize = true;
        }
        /// <summary>Nút chính (tím đậm một nấc) — dùng cho cả nút "Đóng" của hộp chi tiết nhật ký.</summary>
        public static void KieuNutChinh(Krypton.Toolkit.KryptonButton b) => KieuNut(b, NutTimDam);
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
            // Nút bị khóa (vd. hết trang để lùi/tiến): tím rất nhạt, chữ tím nhạt vẫn đọc được
            b.StateDisabled.Back.Color1 = Color.FromArgb(245, 243, 255); b.StateDisabled.Back.Color2 = Color.FromArgb(245, 243, 255);
            b.StateDisabled.Border.Color1 = Color.FromArgb(221, 214, 254); b.StateDisabled.Border.Color2 = Color.FromArgb(221, 214, 254);
            b.StateDisabled.Content.ShortText.Color1 = Color.FromArgb(167, 139, 250);
            b.Cursor = Cursors.Hand;
        }
    }
}
