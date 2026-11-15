using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
using System.Diagnostics;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CHO Form59_QuanLyThiDuaTapTheNamCu — CHỈ TRÌNH BÀY, KHÔNG CÓ LOGIC NGHIỆP VỤ
    //  Cùng ngôn ngữ thiết kế với Form4 / Form10 / Form15 / Form34 / Form50: nền trang xám nhạt, thẻ trắng bo góc + thanh nhấn xanh
    //  ở tiêu đề, combo bo tròn, nút theme tím nhạt, bảng có khung bo tròn liền mạch + tiêu đề tím nhạt.
    //
    //  KHÔNG ĐỔI CODE GỐC: Form59 chỉ thêm đúng 1 dòng trong constructor (ngay sau InitializeComponent):
    //        Module_GiaoDienForm59Web.ApDung(this);
    //  - Không sửa Designer, không đổi tên/kiểu/sự kiện của control nào; tìm control theo Name.
    //  - 14 ô tháng, 3 combo, các nút và mọi sự kiện của code gốc giữ nguyên; chỉ đổi cách hiển thị.
    //  - Dùng lại lớp thẻ TheBoGocF4 và ô combo bo tròn OComboWebF4 trong Module_GiaoDienForm4Web.cs (file đó phải còn trong project).
    //  - Mọi bước bọc try/catch: lỗi trình bày không bao giờ làm form không mở được.
    // =====================================================================================
    internal static class Module_GiaoDienForm59Web
    {
        // ---------- Bảng màu / font: lấy đúng giá trị của Form4 ----------
        private static readonly Color MauNen = Color.FromArgb(244, 246, 250);
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);
        private static readonly Color MauVienO = Color.FromArgb(203, 213, 225);
        private static readonly Color MauVienChon = Color.FromArgb(139, 92, 246);
        private static readonly Color MauTieuDeBang = Color.FromArgb(238, 232, 255);
        private static readonly Color MauChuTieuDeBang = Color.FromArgb(75, 20, 130);
        private static readonly Color MauKeBang = Color.FromArgb(235, 238, 243);
        private static readonly Color MauVienThe = Color.FromArgb(226, 232, 240);

        private static readonly Font FontNhan = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontO = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point);

        private const int DemKhungBang = 5;     // khoảng đệm giữa khung bo tròn và bảng (px thiết kế)
        private const int CaoHangChonCsdl = 102;   // hàng thẻ "Chọn CSDL" (gồm khe 10px phía dưới)
        private const int CaoHangCapNhat = 226;    // hàng thẻ "Cập nhật thông tin thi đua" (gồm khe 10px phía dưới)

        // (Nền, Hover, Nhấn, Viền, Chữ) — theme tím nhạt của Form4
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutTim =
            (Color.FromArgb(237, 233, 254), Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253), Color.FromArgb(196, 181, 253), Color.FromArgb(109, 40, 217));
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutTimDam =
            (Color.FromArgb(221, 214, 254), Color.FromArgb(196, 181, 253), Color.FromArgb(167, 139, 250), Color.FromArgb(167, 139, 250), Color.FromArgb(91, 33, 182));

        // 14 ô hiển thị 12 tháng cũ / 1..11 / 6 tháng đầu năm / tổng kết năm (đúng tên control trong Designer)
        private static readonly string[] CacOThang =
        {
            "KryptonTextBox_Thang12NamCu", "KryptonTextBox_Thang1", "KryptonTextBox_Thang2", "KryptonTextBox_Thang3",
            "KryptonTextBox_Thang4", "KryptonTextBox_Thang5", "KryptonTextBox_6ThangDauNam", "KryptonTextBox_Thang6",
            "KryptonTextBox_Thang7", "KryptonTextBox_Thang8", "KryptonTextBox_Thang9", "KryptonTextBox_Thang10",
            "KryptonTextBox_Thang11", "KryptonTextBox_TongKetNam"
        };
        private static readonly string[] CacNhanThang =
        {
            "label4", "label2", "label11", "label14", "label17", "label3", "label19", "label18", "label8", "label13",
            "label1", "label5", "label16", "label6"
        };

        // ===================== ĐIỂM VÀO =====================
        public static void ApDung(Form f)
        {
            f.SuspendLayout();
            try
            {
                f.BackColor = MauNen;
                An(() => DinhBoCuc(f), "BoCuc");
                An(() => BocCacThe(f), "CacThe");
                An(() => DinhTheChonCsdl(f), "TheChonCsdl");
                An(() => DinhTheCapNhat(f), "TheCapNhat");
                An(() => BoTronCacCombo(f), "Combo");
                An(() => BocLuoi(f), "Luoi");
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
            catch (Exception ex) { Debug.WriteLine($"[Module_GiaoDienForm59Web.{ten}] {ex.Message}"); }
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

        // ===================== BỐ CỤC TỔNG =====================
        private static void DinhBoCuc(Form f)
        {
            // 3 hàng: thẻ Chọn CSDL (cao cố định) | thẻ Cập nhật (cao cố định) | thẻ bảng (ăn hết). Bản gốc chia theo % (13% / 33% / 54%).
            var t1 = Tim<TableLayoutPanel>(f, "tableLayoutPanel1");
            if (t1 != null)
            {
                t1.BackColor = MauNen;
                t1.Margin = new Padding(0);
                t1.Padding = new Padding(12, 10, 12, 6);
                t1.RowStyles.Clear();
                t1.RowStyles.Add(new RowStyle(SizeType.Absolute, CaoHangChonCsdl));
                t1.RowStyles.Add(new RowStyle(SizeType.Absolute, CaoHangCapNhat));
                t1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            }

            // Hàng 0: [ảnh | thẻ Chọn CSDL]
            var t2 = Tim<TableLayoutPanel>(f, "tableLayoutPanel2");
            if (t2 != null)
            {
                t2.BackColor = MauNen;
                t2.Margin = new Padding(0, 0, 0, 10);   // khe 10px phía dưới
                t2.ColumnStyles.Clear();                // bản gốc còn dư 1 kiểu cột Absolute 18 không có cột thật
                t2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
                t2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            }
            var t3 = Tim<TableLayoutPanel>(f, "tableLayoutPanel3");
            if (t3 != null)
            {
                t3.BackColor = MauNen;
                t3.Margin = new Padding(0);
                t3.ColumnStyles.Clear();
                t3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            }

            // Hàng 1: bảng bọc thẻ Cập nhật
            var t7 = Tim<TableLayoutPanel>(f, "tableLayoutPanel7");
            if (t7 != null)
            {
                t7.BackColor = MauNen;
                t7.Margin = new Padding(0, 0, 0, 10);
                t7.RowStyles.Clear();                   // bản gốc còn dư 2 kiểu hàng Absolute 55 không có hàng thật
                t7.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            }
        }

        // ===================== THẺ =====================
        private static void BocCacThe(Form f)
        {
            var anh = Tim<PictureBox>(f, "pictureBox1");
            if (anh != null)
            {
                var the = ThayBangThe(anh, "", new Padding(8), new Padding(0, 0, 10, 0));
                if (the != null)
                {
                    anh.Dock = DockStyle.Fill;
                    anh.Margin = new Padding(0);
                    anh.SizeMode = PictureBoxSizeMode.Zoom;   // trước đây StretchImage -> méo hình
                }
            }
            var gb1 = Tim<GroupBox>(f, "groupBox1_TimKiemThongTinCBCS");
            if (gb1 != null) ThayBangThe(gb1, gb1.Text, new Padding(12, 36, 12, 8), new Padding(0));
            var gb2 = Tim<GroupBox>(f, "groupBox1");
            if (gb2 != null) ThayBangThe(gb2, gb2.Text, new Padding(12, 36, 12, 8), new Padding(0));
        }

        /// <summary>
        /// Thay GroupBox / PictureBox nằm trong ô TableLayoutPanel bằng TheBoGocF4 (thẻ trắng bo góc của Form4) đúng ô đó.
        /// GroupBox: chuyển nguyên các control con (kèm mọi sự kiện/logic) sang thẻ rồi bỏ GroupBox rỗng
        /// (code gốc không dùng tới các field groupBox*). PictureBox: đưa chính nó vào thẻ.
        /// </summary>
        private static TheBoGocF4? ThayBangThe(Control nguon, string tieuDe, Padding padding, Padding margin)
        {
            if (nguon.Parent is not TableLayoutPanel cha) return null;

            var vt = cha.GetPositionFromControl(nguon);
            int spanCot = cha.GetColumnSpan(nguon), spanHang = cha.GetRowSpan(nguon);

            var the = new TheBoGocF4
            {
                Name = "the_" + nguon.Name,
                Dock = DockStyle.Fill,
                Margin = margin,
                TieuDe = tieuDe,
                Padding = padding
            };

            cha.SuspendLayout();
            the.SuspendLayout();
            try
            {
                if (nguon is GroupBox gb)
                {
                    var con = gb.Controls.Cast<Control>().ToArray();
                    cha.Controls.Remove(gb);
                    cha.Controls.Add(the, vt.Column, vt.Row);
                    foreach (var c in con)
                    {
                        gb.Controls.Remove(c);
                        c.Dock = DockStyle.Fill;
                        the.Controls.Add(c);
                    }
                    gb.Dispose();
                }
                else
                {
                    cha.Controls.Remove(nguon);
                    cha.Controls.Add(the, vt.Column, vt.Row);
                    the.Controls.Add(nguon);
                }
                if (spanCot > 1) cha.SetColumnSpan(the, spanCot);
                if (spanHang > 1) cha.SetRowSpan(the, spanHang);
            }
            finally
            {
                the.ResumeLayout(false);
                cha.ResumeLayout(true);
            }
            return the;
        }

        // ===================== THẺ 1: CHỌN CƠ SỞ DỮ LIỆU THI ĐUA TẬP THỂ =====================
        private static void DinhTheChonCsdl(Form f)
        {
            // Chọn năm | combo | nút Xóa | nút Làm mới
            var t5 = Tim<TableLayoutPanel>(f, "tableLayoutPanel5");
            if (t5 != null)
            {
                t5.Margin = new Padding(0);
                t5.ColumnStyles.Clear();
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84F));
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116F));
                t5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 136F));
            }
            DinhNhan(Tim<Label>(f, "label10"), true, AnchorStyles.Left);   // Chọn năm

            var cb = Tim<ComboBox>(f, "comboBox1_ChonCscdThiDuaTapTheNamCu");
            if (cb != null)
            {
                cb.Font = FontO;                       // phải đặt TRƯỚC khi gắn ô bo tròn (nó chép font của ComboBox)
                cb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                cb.Margin = new Padding(0);
            }
            NutTrongO(f, "kryptonButton_XoaThiDuaTapThe", NutTim, new Padding(12, 0, 0, 0));      // Xóa
            NutTrongO(f, "kryptonButton1_LamMoi", NutTimDam, new Padding(12, 0, 0, 0));           // Làm mới
        }

        // ===================== THẺ 2: CẬP NHẬT THÔNG TIN THI ĐUA =====================
        private static void DinhTheCapNhat(Form f)
        {
            // 2 khối: lưới 14 ô tháng (ăn hết) + hàng chọn tháng / phân loại / nút Cập nhật (cao cố định)
            var t4 = Tim<TableLayoutPanel>(f, "tableLayoutPanel4");
            if (t4 != null)
            {
                t4.Margin = new Padding(0);
                t4.ColumnStyles.Clear();
                t4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                t4.RowStyles.Clear();
                t4.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                t4.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            }

            // 5 cặp [nhãn | ô]: nhãn rộng cố định (vừa chữ "6 tháng đầu năm"), 5 ô chia đều phần còn lại
            var t10 = Tim<TableLayoutPanel>(f, "tableLayoutPanel10");
            if (t10 != null)
            {
                t10.Margin = new Padding(0);
                t10.ColumnStyles.Clear();   // bản gốc: 10 cột % xen kẽ 11,1 / 8,9 nên ô nhập hẹp không đều
                for (int i = 0; i < 5; i++)
                {
                    t10.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116F));
                    t10.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
                }
                t10.RowStyles.Clear();
                for (int i = 0; i < 3; i++) t10.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / 3F));
            }

            foreach (var ten in CacNhanThang)
            {
                var lb = Tim<Label>(f, ten);
                if (lb == null) continue;
                DinhNhan(lb, false, AnchorStyles.Right);
                lb.Margin = new Padding(0, 0, 8, 0);   // sát ô nhập của nó
            }

            // 14 ô: code gốc chỉ đổ chữ vào; helper chỉ đổi khung (không đụng chữ/nền/chỉ-đọc)
            foreach (var ten in CacOThang)
            {
                var o = Tim<Krypton.Toolkit.KryptonTextBox>(f, ten);
                if (o == null) continue;
                o.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                o.Margin = new Padding(0, 0, 16, 0);   // khe giữa các cặp
                KieuVienO(o.StateCommon.Border, o.StateActive.Border);
            }

            // Hàng điều khiển: nhãn (chữ do code gốc đổi theo tháng) | combo tháng | Chọn phân loại | combo phân loại | nút Cập nhật
            var t6 = Tim<TableLayoutPanel>(f, "tableLayoutPanel6");
            if (t6 != null)
            {
                t6.Margin = new Padding(0);
                t6.ColumnStyles.Clear();
                t6.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 290F));    // label9 (chữ thay đổi: "Cập nhật phân loại / danh hiệu thi đua tập thể:")
                t6.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));    // combo tháng
                t6.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124F));    // "Chọn phân loại"
                t6.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));    // combo phân loại
                t6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));     // nút Cập nhật
            }
            DinhNhan(Tim<Label>(f, "label9_ChonTenThiDuaThang"), false, AnchorStyles.Right);
            DinhNhan(Tim<Label>(f, "label7"), false, AnchorStyles.Right);
            foreach (var ten in new[] { "label9_ChonTenThiDuaThang", "label7" })
            {
                var lb = Tim<Label>(f, ten);
                if (lb != null) lb.Margin = new Padding(0, 0, 10, 0);
            }

            foreach (var ten in new[] { "comboBox1_ChonThang", "comboBox_ChonPhanLoai" })
            {
                var cb = Tim<ComboBox>(f, ten);
                if (cb == null) continue;
                cb.Font = FontO;
                cb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                cb.Margin = new Padding(0);
            }

            var cap = Tim<Krypton.Toolkit.KryptonButton>(f, "kryptonButton_CapNhatThiDuaTapTheNamCu");
            if (cap != null)
            {
                KieuNut(cap, NutTimDam);
                cap.Anchor = AnchorStyles.Left;        // nút rộng cố định 130px, canh trái cạnh combo phân loại
                cap.Margin = new Padding(16, 0, 0, 0);
                cap.Size = new Size(130, 32);
            }
        }

        // ===================== COMBO BO TRÒN (dùng OComboWebF4 của Form4) =====================
        // Combo gốc vẫn tồn tại + hoạt động (chỉ bị ẩn hình): DataSource, SelectedItem/SelectedIndexChanged và mọi code gốc chạy như cũ.
        private static void BoTronCacCombo(Form f)
        {
            foreach (var cb in TatCa<ComboBox>(f).ToList())   // ToList: Gan thêm control vào thẻ trong lúc duyệt
                if (cb.GetType() == typeof(ComboBox))
                    OComboWebF4.Gan(cb);
        }

        // ===================== LƯỚI =====================
        // Bảng nằm gọn trong 1 thẻ có KHUNG BO TRÒN liền mạch (không cắt góc bằng Region): thẻ vẽ khung + dải tiêu đề cùng màu tiêu đề bảng.
        private static void BocLuoi(Form f)
        {
            var luoi = Tim<DataGridView>(f, "kryptonDataGridView1");
            if (luoi == null || luoi.Parent is not TableLayoutPanel cha) return;

            var vt = cha.GetPositionFromControl(luoi);
            const int gap = DemKhungBang;
            var the = new TheBoGocF4
            {
                Name = "the_kryptonDataGridView1",
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                TieuDe = "3. Danh sách thi đua tập thể",
                DemKhung = gap,
                Padding = new Padding(12 + gap, 36 + gap, 12 + gap, 10 + gap),
                LuoiBang = luoi
            };

            cha.SuspendLayout();
            the.SuspendLayout();
            try
            {
                cha.Controls.Remove(luoi);
                cha.Controls.Add(the, vt.Column, vt.Row);
                the.Controls.Add(luoi);
                luoi.Dock = DockStyle.Fill;
                luoi.Margin = new Padding(0);
                luoi.BorderStyle = BorderStyle.None;
            }
            finally
            {
                the.ResumeLayout(false);
                cha.ResumeLayout(true);
            }

            // ---- Áp màu tiêu đề mới, và GIỮ nó sau khi code gốc định dạng lưới trong Load ----
            bool dangDap = false;
            Krypton.Toolkit.KryptonDataGridView? kLuoi = luoi as Krypton.Toolkit.KryptonDataGridView;
            void DapKieu()
            {
                if (dangDap || kLuoi == null) return;   // chốt chống lặp
                dangDap = true;
                try
                {
                    StyleKryptonLuoi(kLuoi);
                    the.Invalidate();                   // dải tiêu đề của thẻ đọc màu này lúc vẽ
                }
                finally { dangDap = false; }
            }
            bool DaDungKieu() => kLuoi != null && kLuoi.StateCommon.HeaderColumn.Back.Color1 == MauTieuDeBang;

            // Code gốc (Load -> DinhDangGiaoDienDataGridTongKetThang) chỉ đặt font/đường kẻ/màu chọn dòng, không đụng màu tiêu đề;
            // các mốc áp lại bên dưới chỉ là chốt an toàn.
            luoi.ColumnAdded += (s, e) => { if (!DaDungKieu()) DapKieu(); };
            luoi.Layout += (s, e) => { if (!DaDungKieu()) DapKieu(); };
            f.Shown += (s, e) => { if (!DaDungKieu()) DapKieu(); };
            luoi.ColumnHeadersHeightChanged += (s, e) => the.Invalidate();
            DapKieu();
        }

        // Phần tiêu đề + đường kẻ của lưới (Krypton). Selection, font, màu chữ do code gốc đặt nên không đụng tới.
        private static void StyleKryptonLuoi(Krypton.Toolkit.KryptonDataGridView k)
        {
            k.StateCommon.HeaderColumn.Content.Color1 = MauChuTieuDeBang;
            k.StateCommon.DataCell.Border.Color1 = MauKeBang;
            k.StateCommon.HeaderRow.Back.Color1 = Color.FromArgb(250, 248, 255);
            k.StateCommon.HeaderRow.Back.Color2 = Color.FromArgb(250, 248, 255);
            k.StateCommon.HeaderRow.Border.Color1 = Color.FromArgb(237, 233, 254);
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

        // ===================== THANH TRẠNG THÁI (KryptonStatusStrip) =====================
        // KryptonStatusStrip vẽ theo theme Krypton (chuyển sắc) nên đặt BackColor không đủ: gán renderer phẳng riêng cho riêng thanh này.
        private static void DinhThanhTrangThai(Form f)
        {
            var ss = Tim<StatusStrip>(f, "kryptonStatusStrip1");
            if (ss == null) return;
            ss.Renderer = new WebStatusRenderer();
            ss.BackColor = Color.White;
            ss.ForeColor = MauChuNhan;
            ss.SizingGrip = false;
            ss.Padding = new Padding(10, 0, 10, 0);
            // Chỉ đổi font/màu chữ: nội dung, căn lề, hiện/ẩn từng nhãn do code gốc điều khiển
            foreach (var ten in new[] { "toolStripStatusLabel1", "toolStripStatusLabel2", "toolStripStatusLabel3_ThongBao" })
            {
                var it = ss.Items.Find(ten, false).FirstOrDefault();
                if (it != null) { it.Font = FontNhan; it.ForeColor = MauChuNhan; }
            }
        }

        /// <summary>Thanh trạng thái phẳng: nền trắng + 1 đường kẻ phía trên, không gradient, không đổ bóng.</summary>
        private sealed class WebStatusRenderer : ToolStripProfessionalRenderer
        {
            public WebStatusRenderer() { RoundedEdges = false; }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using var b = new SolidBrush(Color.White);
                e.Graphics.FillRectangle(b, e.AffectedBounds);
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                using var p = new Pen(MauVienThe, 1f);
                e.Graphics.DrawLine(p, 0, 0, e.ToolStrip.Width, 0);
            }
        }

        // ===================== TIỆN ÍCH TRÌNH BÀY =====================
        private static void DinhNhan(Label? lb, bool dauHang, AnchorStyles canh)
        {
            if (lb == null) return;
            lb.Font = FontNhan;                       // bản gốc: chữ nghiêng 10,2pt xanh đậm
            lb.ForeColor = MauChuNhan;
            lb.Anchor = canh;
            lb.Margin = new Padding(dauHang ? 0 : 12, 0, 6, 0);
        }

        // Viền ô nhập Krypton: xám bo góc 8, khi đang gõ chuyển tím (cùng màu viền combo đang chọn)
        private static void KieuVienO(Krypton.Toolkit.PaletteBorder vien, Krypton.Toolkit.PaletteBorder vienActive)
        {
            vien.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            vien.Color1 = MauVienO; vien.Color2 = MauVienO;
            vien.Rounding = 8F;
            vien.Width = 1;
            vienActive.Color1 = MauVienChon; vienActive.Color2 = MauVienChon;
        }

        private static void NutTrongO(Form f, string ten, (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) m, Padding margin)
        {
            var b = Tim<Krypton.Toolkit.KryptonButton>(f, ten);
            if (b == null) return;
            KieuNut(b, m);
            b.Anchor = AnchorStyles.Left | AnchorStyles.Right;   // nút lấp đầy chiều ngang ô, cao đều 32
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
            // Nút đang giữ focus / nút mặc định: ép giống trạng thái thường để không bị chữ trắng nền trắng
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
    }
}