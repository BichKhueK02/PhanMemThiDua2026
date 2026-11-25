using System.Drawing.Drawing2D;
using System.Reflection;
///Phiên bản 2 - ẩn viền bo tròn dưới tiêu đề cấu trúc CSDL, bỏ màu nền 2 ô tùy chọn
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CỦA Form31_ChuyenGiaoDuLieu — CHỈ TRÌNH BÀY, KHÔNG CHỨA LOGIC NGHIỆP VỤ
    //  Module độc lập (không phải partial của form) => Visual Studio không sinh thêm file .resx.
    //
    //  Cách dùng: trong constructor Form31_ChuyenGiaoDuLieu, thêm 1 dòng SAU CÙNG:
    //        Module_GiaoDienForm31Web.ApDung(this);
    //  - Control (private trong Designer) được lấy theo TÊN bằng reflection => không sửa Designer, tên, kiểu, sự kiện nào.
    //  - Lỗi trình bày không bao giờ làm form không mở được (bọc try/catch).
    //  - Bật double-buffer cho các bảng/danh sách để mở form và đổi kích thước mượt, không nhấp nháy.
    //  - Mọi kích thước (px) viết theo đơn vị thiết kế 96 DPI.
    // =====================================================================================
    internal sealed class Module_GiaoDienForm31Web
    {
        /// <summary>true = đổi 2 ComboBox sang ô sổ xuống bo tròn (OComboWebF4 trong Module_GiaoDienForm4Web.cs, giống Form6).
        /// Mặc định tắt vì danh sách mục của 2 ComboBox này bị code form xóa/thêm liên tục; bật lên nếu bạn đã thử và thấy ổn.</summary>
        private const bool DungComboBoTron = false;
        private readonly Form _f;
        // ---------- Control lấy theo tên (cùng tên với Designer) ----------
        private readonly TableLayoutPanel tableLayoutPanel1, tableLayoutPanel2, tableLayoutPanel3, tableLayoutPanel4,
            tableLayoutPanel5, tableLayoutPanel6, tableLayoutPanel7, tableLayoutPanel8, tableLayoutPanel9;
        private readonly PictureBox pictureBox1;
        private readonly Label label1, label2, label3, lbl_TrangThaiHoatDong, label1_TieuDeCauTrucCSDLDangDuocChon;
        private readonly CheckBox chk_BackupTruocKhiChuyen, chk_XoaDuLieuCu, checkBox1_ChonTatCaCacCSDL, checkBox1_ChonTatCaCacCSDLDeNap;
        private readonly ComboBox cbo_ChonCSDL_Nguon, comboBox1_CSDLDeNapVao;
        private readonly CheckedListBox checkedListBox1_clb_DanhSachBang;
        private readonly RichTextBox richTextBox1_ThongTinDatabaseDuocChon;
        private readonly Control prb_TienTrinhChuyenGiao;
        private readonly Krypton.Toolkit.KryptonButton kryptonButton_Dong, kryptonButton1_HuongDanSuDung,
            btn_QuetTimKiem, btn_XuatDuLieuJson, btn_NhapDuLieuJson, kryptonButton2_MoThuMuc;
        public static void ApDung(Form f)
        {
            try { new Module_GiaoDienForm31Web(f).Chay(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[Module_GiaoDienForm31Web] " + ex.Message); }
        }
        private Module_GiaoDienForm31Web(Form f)
        {
            _f = f;
            tableLayoutPanel1 = Lay<TableLayoutPanel>("tableLayoutPanel1");
            tableLayoutPanel2 = Lay<TableLayoutPanel>("tableLayoutPanel2");
            tableLayoutPanel3 = Lay<TableLayoutPanel>("tableLayoutPanel3");
            tableLayoutPanel4 = Lay<TableLayoutPanel>("tableLayoutPanel4");
            tableLayoutPanel5 = Lay<TableLayoutPanel>("tableLayoutPanel5");
            tableLayoutPanel6 = Lay<TableLayoutPanel>("tableLayoutPanel6");
            tableLayoutPanel7 = Lay<TableLayoutPanel>("tableLayoutPanel7");
            tableLayoutPanel8 = Lay<TableLayoutPanel>("tableLayoutPanel8");
            tableLayoutPanel9 = Lay<TableLayoutPanel>("tableLayoutPanel9");
            pictureBox1 = Lay<PictureBox>("pictureBox1");
            label1 = Lay<Label>("label1");
            label2 = Lay<Label>("label2");
            label3 = Lay<Label>("label3");
            lbl_TrangThaiHoatDong = Lay<Label>("lbl_TrangThaiHoatDong");
            label1_TieuDeCauTrucCSDLDangDuocChon = Lay<Label>("label1_TieuDeCauTrucCSDLDangDuocChon");
            chk_BackupTruocKhiChuyen = Lay<CheckBox>("chk_BackupTruocKhiChuyen");
            chk_XoaDuLieuCu = Lay<CheckBox>("chk_XoaDuLieuCu");
            checkBox1_ChonTatCaCacCSDL = Lay<CheckBox>("checkBox1_ChonTatCaCacCSDL");
            checkBox1_ChonTatCaCacCSDLDeNap = Lay<CheckBox>("checkBox1_ChonTatCaCacCSDLDeNap");
            cbo_ChonCSDL_Nguon = Lay<ComboBox>("cbo_ChonCSDL_Nguon");
            comboBox1_CSDLDeNapVao = Lay<ComboBox>("comboBox1_CSDLDeNapVao");
            checkedListBox1_clb_DanhSachBang = Lay<CheckedListBox>("checkedListBox1_clb_DanhSachBang");
            richTextBox1_ThongTinDatabaseDuocChon = Lay<RichTextBox>("richTextBox1_ThongTinDatabaseDuocChon");
            prb_TienTrinhChuyenGiao = Lay<Control>("prb_TienTrinhChuyenGiao");
            kryptonButton_Dong = Lay<Krypton.Toolkit.KryptonButton>("kryptonButton_Dong");
            kryptonButton1_HuongDanSuDung = Lay<Krypton.Toolkit.KryptonButton>("kryptonButton1_HuongDanSuDung");
            btn_QuetTimKiem = Lay<Krypton.Toolkit.KryptonButton>("btn_QuetTimKiem");
            btn_XuatDuLieuJson = Lay<Krypton.Toolkit.KryptonButton>("btn_XuatDuLieuJson");
            btn_NhapDuLieuJson = Lay<Krypton.Toolkit.KryptonButton>("btn_NhapDuLieuJson");
            kryptonButton2_MoThuMuc = Lay<Krypton.Toolkit.KryptonButton>("kryptonButton2_MoThuMuc");
        }
        private T Lay<T>(string ten) where T : class
        {
            var fi = _f.GetType().GetField(ten, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                     ?? throw new MissingFieldException(_f.GetType().Name, ten);
            return (T)fi.GetValue(_f)!;
        }
        // ---------- BẢNG MÀU (cùng tông Slate + Blue với Form6) ----------
        private static readonly Color MauNen = Color.FromArgb(244, 246, 250);
        private static readonly Color MauVienThe = Color.FromArgb(226, 232, 240);
        private static readonly Color MauChuDam = Color.FromArgb(30, 41, 59);
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);
        private static readonly Color MauChuPhu = Color.FromArgb(100, 116, 139);
        private static readonly Color MauChinh = Color.FromArgb(37, 99, 235);
        private static readonly Color GradientDau = Color.FromArgb(79, 70, 229);
        private static readonly Color GradientCuoi = Color.FromArgb(124, 58, 237);
        private static readonly Font FontTieuDeThe = new(Module_HeThong.TenFontHeThongDam, 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontTieuDeLon = new(Module_HeThong.TenFontHeThongDam, 16F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNhan = new(Module_HeThong.TenFontHeThong, 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNhap = new(Module_HeThong.TenFontHeThong, 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new(Module_HeThong.TenFontHeThongDam, 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNoiDung = new(Module_HeThong.TenFontHeThong, 10F, FontStyle.Regular, GraphicsUnit.Point);
        // Bộ màu nút: (Nền, Hover, Nhấn, Viền, Chữ)
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutChinh =
            (Color.FromArgb(37, 99, 235), Color.FromArgb(29, 78, 216), Color.FromArgb(30, 64, 175), Color.FromArgb(37, 99, 235), Color.White);
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutXanhLa =
            (Color.FromArgb(22, 163, 74), Color.FromArgb(21, 128, 61), Color.FromArgb(22, 101, 52), Color.FromArgb(22, 163, 74), Color.White);
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutXanhNhat =
            (Color.FromArgb(239, 246, 255), Color.FromArgb(219, 234, 254), Color.FromArgb(191, 219, 254), Color.FromArgb(191, 219, 254), Color.FromArgb(29, 78, 216));
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutVien =
            (Color.White, Color.FromArgb(248, 250, 252), Color.FromArgb(241, 245, 249), Color.FromArgb(203, 213, 225), Color.FromArgb(51, 65, 85));
        private static readonly PropertyInfo? PropDoubleBuffered =
            typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
        // =====================================================================================
        //  ÁP DỤNG GIAO DIỆN
        // =====================================================================================
        private void Chay()
        {
            _f.SuspendLayout();
            try
            {
                _f.BackColor = MauNen;
                BatDoubleBuffer(_f);
                DinhBoCucTong();
                DinhHeader();
                DinhKhoiThan();
                DinhKhoiDieuKhien();
                // Bọc thẻ SAU CÙNG (sau khi đã chỉnh từng control) để giảm số lần bố trí lại
                BocThe(tableLayoutPanel3, new Padding(0, 0, 0, 12), new Padding(18, 0, 18, 0), header: true);
                BocThe(tableLayoutPanel7, new Padding(0, 0, 8, 12), new Padding(14, 10, 14, 12));
                BocThe(richTextBox1_ThongTinDatabaseDuocChon, new Padding(8, 0, 0, 12), new Padding(14, 38, 14, 12),
                    tieuDe: "THÔNG TIN CƠ SỞ DỮ LIỆU");
                BocThe(tableLayoutPanel2, new Padding(0), new Padding(14, 38, 14, 10), tieuDe: "XUẤT / NHẬP DỮ LIỆU");
                foreach (var t in new[] { tableLayoutPanel1, tableLayoutPanel2, tableLayoutPanel3, tableLayoutPanel4, tableLayoutPanel5,
                                          tableLayoutPanel6, tableLayoutPanel7, tableLayoutPanel8, tableLayoutPanel9 })
                    BatDoubleBuffer(t);
                BatDoubleBuffer(checkedListBox1_clb_DanhSachBang);
            }
            finally
            {
                _f.ResumeLayout(true);
            }
        }
        // ---------- Bố cục tổng: header cố định, thân co giãn, chân cố định ----------
        private void DinhBoCucTong()
        {
            tableLayoutPanel1.BackColor = MauNen;
            tableLayoutPanel1.Padding = new Padding(16, 12, 16, 12);
            tableLayoutPanel1.RowStyles.Clear();
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));   // header
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));   // thân: danh sách + thông tin
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 214F));  // chân: điều khiển + tiến trình
            tableLayoutPanel5.Margin = new Padding(0);
            tableLayoutPanel5.ColumnStyles.Clear();
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
        }
        // ---------- Header chuyển màu: logo | tiêu đề | nút Đóng ----------
        private void DinhHeader()
        {
            tableLayoutPanel3.BackColor = Color.Transparent; // để lộ nền gradient của thẻ bọc
            tableLayoutPanel3.Margin = new Padding(0);
            tableLayoutPanel3.ColumnStyles.Clear();
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;   // giữ đúng tỉ lệ ảnh (trước đây kéo giãn làm méo)
            pictureBox1.Margin = new Padding(0, 12, 12, 12);
            pictureBox1.BackColor = Color.Transparent;
            label1.Anchor = AnchorStyles.Left;
            label1.Font = FontTieuDeLon;
            label1.ForeColor = Color.White;
            label1.BackColor = Color.Transparent;
            label1.Margin = new Padding(0);
            KieuNut(kryptonButton_Dong, NutVien);
            CanNut(kryptonButton_Dong, new Padding(0));
        }
        // ---------- Thân: danh sách bảng + RichTextBox thông tin ----------
        private void DinhKhoiThan()
        {
            tableLayoutPanel7.Margin = new Padding(0);
            tableLayoutPanel7.BackColor = Color.White;
            tableLayoutPanel7.RowStyles.Clear();
            tableLayoutPanel7.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableLayoutPanel7.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            label1_TieuDeCauTrucCSDLDangDuocChon.Anchor = AnchorStyles.Left;
            label1_TieuDeCauTrucCSDLDangDuocChon.Font = FontTieuDeThe;
            label1_TieuDeCauTrucCSDLDangDuocChon.ForeColor = MauChuDam;
            label1_TieuDeCauTrucCSDLDangDuocChon.Margin = new Padding(0);
            var clb = checkedListBox1_clb_DanhSachBang;
            clb.Dock = DockStyle.Fill;
            clb.Margin = new Padding(0);
            clb.BorderStyle = BorderStyle.None;
            clb.IntegralHeight = false;
            clb.BackColor = Color.White;
            clb.ForeColor = MauChuDam;
            clb.Font = FontNoiDung;
            GoVienBoTron(clb);   // viền bo tròn của module bo góc chạy sát dưới tiêu đề nên trông như một đường kẻ thừa
            var rtb = richTextBox1_ThongTinDatabaseDuocChon;
            rtb.BorderStyle = BorderStyle.None;
            rtb.BackColor = Color.White;
            rtb.ForeColor = MauChuDam;
            rtb.Font = FontNoiDung;
            rtb.Margin = new Padding(0);
        }
        // ---------- Chân: 3 hàng điều khiển + thanh tiến trình ----------
        private void DinhKhoiDieuKhien()
        {
            tableLayoutPanel2.Margin = new Padding(0);
            tableLayoutPanel2.RowStyles.Clear();
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tableLayoutPanel4.Margin = new Padding(0);
            tableLayoutPanel4.ColumnStyles.Clear();
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // nhãn / tùy chọn
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));     // ô chọn
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));    // nút chính
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));    // nút phụ
            tableLayoutPanel6.Margin = new Padding(0);
            tableLayoutPanel8.Margin = new Padding(0);
            tableLayoutPanel9.Margin = new Padding(0);
            DinhNhanPhai(label2);
            DinhNhanPhai(label3);
            // Hai tùy chọn: KHÔNG dùng màu nền (trước đây nền xanh nhạt khi bật, hồng nhạt khi tắt do nơi khác đặt) -> luôn trong suốt,
            // chỉ chỉnh font/khoảng đệm. Màu chữ giữ nguyên.
            foreach (var c in new[] { chk_BackupTruocKhiChuyen, chk_XoaDuLieuCu })
            {
                c.Font = FontNhan;
                c.Padding = new Padding(6, 2, 6, 2);
                c.Cursor = Cursors.Hand;
                KhoaNenTrongSuot(c);
            }
            foreach (var c in new[] { checkBox1_ChonTatCaCacCSDL, checkBox1_ChonTatCaCacCSDLDeNap })
            {
                c.Font = FontNhan;
                c.ForeColor = MauChuNhan;
                c.Cursor = Cursors.Hand;
            }
            DinhCombo(cbo_ChonCSDL_Nguon);
            DinhCombo(comboBox1_CSDLDeNapVao);
            if (DungComboBoTron)
            {
                foreach (var cb in new[] { cbo_ChonCSDL_Nguon, comboBox1_CSDLDeNapVao })
                    if (cb.Parent != null) OComboWebF4.Gan(cb);
            }
            lbl_TrangThaiHoatDong.Font = FontNhan;
            lbl_TrangThaiHoatDong.ForeColor = MauChuPhu;
            prb_TienTrinhChuyenGiao.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            prb_TienTrinhChuyenGiao.Margin = new Padding(0);
            KieuNut(btn_XuatDuLieuJson, NutChinh);
            KieuNut(btn_NhapDuLieuJson, NutXanhLa);
            KieuNut(btn_QuetTimKiem, NutXanhNhat);
            KieuNut(kryptonButton2_MoThuMuc, NutVien);
            KieuNut(kryptonButton1_HuongDanSuDung, NutVien);
            CanNut(btn_XuatDuLieuJson, new Padding(8, 0, 4, 0));
            CanNut(btn_NhapDuLieuJson, new Padding(8, 0, 4, 0));
            CanNut(btn_QuetTimKiem, new Padding(4, 0, 0, 0));
            CanNut(kryptonButton2_MoThuMuc, new Padding(4, 0, 0, 0));
            CanNut(kryptonButton1_HuongDanSuDung, new Padding(4, 0, 0, 0));
        }
        // =====================================================================================
        //  TIỆN ÍCH TRÌNH BÀY
        // =====================================================================================
        /// <summary>
        /// Gỡ viền bo tròn mà Module_GiaoDienBoTron vẽ quanh danh sách/ô văn bản (nét 2,5px do control CHA vẽ sát mép ngoài).
        /// - Đặt Tag = "KhongBoGoc": có tác dụng nếu module bo góc xử lý control này SAU lúc này.
        /// - Nếu module bo góc đã xử lý TRƯỚC: gỡ control khỏi danh sách viền của control cha (reflection, có try/catch nên
        ///   không làm form lỗi nếu module đó đổi cấu trúc nội bộ) rồi vẽ lại vùng quanh control.
        /// </summary>
        private static void GoVienBoTron(Control c)
        {
            c.Tag = Module_GiaoDienBoTron.TheKhongBoGoc;
            try
            {
                Control? cha = c.Parent;
                if (cha == null) return;
                var truong = typeof(Module_GiaoDienBoTron).GetField("_vienCon", BindingFlags.NonPublic | BindingFlags.Static);
                if (truong?.GetValue(null) is System.Runtime.CompilerServices.ConditionalWeakTable<Control, List<Control>> bang
                    && bang.TryGetValue(cha, out List<Control>? danhSach)
                    && danhSach.Remove(c))
                {
                    cha.Invalidate(Rectangle.Inflate(c.Bounds, 8, 8));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[GoVienBoTron] " + ex.Message);
            }
        }
        /// <summary>
        /// Nền ô tích luôn trong suốt (thấy màu của thẻ phía sau). Code nơi khác đổi BackColor (vd: xanh khi bật / hồng khi tắt)
        /// sẽ bị đặt lại ngay: không cần biết nơi đó nằm ở đâu.
        /// </summary>
        private static void KhoaNenTrongSuot(CheckBox c)
        {
            c.BackColor = Color.Transparent;
            c.BackColorChanged += (s, e) =>
            {
                if (c.BackColor.A != 0) c.BackColor = Color.Transparent;   // đặt về trong suốt sẽ không kích hoạt lại vòng lặp
            };
        }
        private static void BatDoubleBuffer(Control c)
        {
            try { PropDoubleBuffered?.SetValue(c, true); }
            catch { /* không quan trọng */ }
        }
        private static void DinhNhanPhai(Label lb)
        {
            lb.Font = FontNhan;
            lb.ForeColor = MauChuNhan;
            lb.Anchor = AnchorStyles.Right;
            lb.Margin = new Padding(0, 0, 10, 0);
        }
        private static void DinhCombo(ComboBox c)
        {
            c.FlatStyle = FlatStyle.Flat;
            c.Font = FontNhap;
            c.ForeColor = MauChuDam;
            c.BackColor = Color.White;
            c.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            c.Margin = new Padding(0, 3, 0, 3);
        }
        private static void CanNut(Krypton.Toolkit.KryptonButton b, Padding margin)
        {
            b.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            b.Margin = margin;
            b.Height = 34;
        }
        private static void KieuNut(Krypton.Toolkit.KryptonButton b,
            (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) m)
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
            // Trạng thái có FOCUS: nếu không đặt, Krypton rơi về nền trắng của theme -> chữ trắng trên nền trắng
            foreach (var ov in new[] { b.OverrideFocus, b.OverrideDefault })
            {
                ov.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
                ov.Back.Color1 = m.Nen; ov.Back.Color2 = m.Nen;
                ov.Border.Color1 = m.Vien; ov.Border.Color2 = m.Vien;
                ov.Border.Rounding = 8F;
                ov.Border.Width = 1;
                ov.Content.ShortText.Color1 = m.Chu;
                ov.Content.ShortText.Color2 = m.Chu;
            }
            // Vô hiệu hóa (khi đang quét/xử lý): xám nhạt, vẫn đọc được chữ
            b.StateDisabled.Back.Color1 = Color.FromArgb(241, 245, 249); b.StateDisabled.Back.Color2 = Color.FromArgb(241, 245, 249);
            b.StateDisabled.Border.Color1 = MauVienThe; b.StateDisabled.Border.Color2 = MauVienThe;
            b.StateDisabled.Content.ShortText.Color1 = Color.FromArgb(148, 163, 184);
        }
        /// <summary>Thay control trong ô lưới bằng một thẻ bo góc chứa chính control đó (giữ nguyên mọi sự kiện/logic).</summary>
        private static void BocThe(Control noiDung, Padding margin, Padding padding, string tieuDe = "", bool header = false)
        {
            var cha = noiDung.Parent;
            if (cha == null) return;
            var the = new TheF31
            {
                Dock = DockStyle.Fill,
                Margin = margin,
                Padding = padding,
                TieuDe = tieuDe,
                LaHeader = header
            };
            cha.SuspendLayout();
            try
            {
                if (cha is TableLayoutPanel tlp)
                {
                    var vt = tlp.GetPositionFromControl(noiDung);
                    tlp.Controls.Remove(noiDung);
                    tlp.Controls.Add(the, vt.Column, vt.Row);
                }
                else
                {
                    cha.Controls.Remove(noiDung);
                    cha.Controls.Add(the);
                }
                the.Controls.Add(noiDung);
                noiDung.Dock = DockStyle.Fill;
                noiDung.Margin = new Padding(0);
            }
            finally
            {
                cha.ResumeLayout(true);
            }
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
        // =====================================================================================
        //  LỚP PHỤ TRỢ TRÌNH BÀY
        // =====================================================================================
        /// <summary>Thẻ bo góc: nền trắng + viền mảnh + tiêu đề có thanh nhấn xanh; hoặc header chuyển màu (LaHeader).</summary>
        internal sealed class TheF31 : Panel
        {
            private string _tieuDe = "";
            public string TieuDe
            {
                get => _tieuDe;
                set { _tieuDe = value ?? ""; Invalidate(); }
            }
            public bool LaHeader { get; set; }
            public TheF31()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
                BackColor = Color.White;
            }
            protected override void OnPaintBackground(PaintEventArgs e) =>
                e.Graphics.Clear(Parent?.BackColor ?? MauNen);
            protected override void OnPaint(PaintEventArgs e)
            {
                if (Width < 4 || Height < 4) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                float k = DeviceDpi / 96f;
                var r = new Rectangle(0, 0, Width - 1, Height - 1);
                using var path = TaoDuongBoGoc(r, (int)((LaHeader ? 24 : 20) * k)); // bán kính 12 / 10
                if (LaHeader)
                {
                    using var br = new LinearGradientBrush(ClientRectangle, GradientDau, GradientCuoi, 20f);
                    g.FillPath(br, path);
                    return;
                }
                using var nen = new SolidBrush(Color.White);
                using var vien = new Pen(MauVienThe);
                g.FillPath(nen, path);
                g.DrawPath(vien, path);
                if (_tieuDe.Length > 0)
                {
                    int x = (int)(16 * k), w = Math.Max(3, (int)(3 * k));
                    using (var bar = new SolidBrush(MauChinh))
                        g.FillRectangle(bar, x, (int)(13 * k), w, (int)(14 * k));
                    TextRenderer.DrawText(g, _tieuDe, FontTieuDeThe, new Point(x + w + (int)(7 * k), (int)(11 * k)), MauChuDam,
                        TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.Left | TextFormatFlags.SingleLine);
                }
            }
        }
    }
}
