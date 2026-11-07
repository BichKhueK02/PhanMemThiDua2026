///Phiên bản 3
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
using System.Drawing.Drawing2D;
namespace PhanMemThiDua2026
{
    // =====================================================================================
    //  GIAO DIỆN "KIỂU TRANG WEB" CỦA FORM6  —  CHỈ TRÌNH BÀY, KHÔNG CHỨA LOGIC NGHIỆP VỤ
    //  - ApDungGiaoDienWeb() được gọi 1 lần ngay sau InitializeComponent() (trong constructor).
    //  - Một số thuộc tính (màu lưới, bo góc ô nhập...) bị code gốc đặt lại trong Form6_Load,
    //    nên phần "HoanThienGiaoDienSauLoad" được móc vào sự kiện Load SAU Form6_Load để giữ
    //    đúng giao diện mà KHÔNG phải sửa bất kỳ dòng logic nào.
    //  - Không đổi tên / kiểu / sự kiện của control nào => code nghiệp vụ giữ nguyên 100%.
    //  - Mọi kích thước (px) viết theo đơn vị thiết kế 96 DPI; WinForms tự scale theo DPI.
    // =====================================================================================
    // LƯU Ý: KHÔNG gắn [DesignerCategory("Code")] ở đây — attribute trên partial class áp cho CẢ lớp
    // (kể cả Form6_XuLyData.cs) nên sẽ làm mất biểu tượng/designer của form.
    // Việc "file này chỉ là code" được khai báo trong .csproj bằng <SubType>Code</SubType>.
    public partial class Form6_XuLyData
    {
        // ---------- BẢNG MÀU (tông Slate + Blue, giống các web admin hiện đại) ----------
        private static readonly Color MauNenTrang = Color.FromArgb(244, 246, 250);   // nền trang
        private static readonly Color MauThe = Color.White;                          // nền thẻ
        private static readonly Color MauVienThe = Color.FromArgb(226, 232, 240);    // viền thẻ
        private static readonly Color MauChuDam = Color.FromArgb(30, 41, 59);        // tiêu đề / chữ chính
        private static readonly Color MauChuNhan = Color.FromArgb(71, 85, 105);      // nhãn ô nhập
        private static readonly Color MauChuPhu = Color.FromArgb(100, 116, 139);     // chữ phụ
        private static readonly Color MauChinh = Color.FromArgb(37, 99, 235);        // xanh chủ đạo

        private static readonly Font FontTieuDeThe = new Font("Segoe UI Semibold", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNhan = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontNut = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontTrangThai = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private static readonly Font FontMenu = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

        // Bộ màu nút: (Nền, Hover, Nhấn, Viền, Chữ)
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutChinh =
            (Color.FromArgb(37, 99, 235), Color.FromArgb(29, 78, 216), Color.FromArgb(30, 64, 175), Color.FromArgb(37, 99, 235), Color.White);
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutXanhLa =
            (Color.FromArgb(22, 163, 74), Color.FromArgb(21, 128, 61), Color.FromArgb(22, 101, 52), Color.FromArgb(22, 163, 74), Color.White);
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutXanhNhat =
            (Color.FromArgb(239, 246, 255), Color.FromArgb(219, 234, 254), Color.FromArgb(191, 219, 254), Color.FromArgb(191, 219, 254), Color.FromArgb(29, 78, 216));
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutVien =
            (Color.White, Color.FromArgb(248, 250, 252), Color.FromArgb(241, 245, 249), Color.FromArgb(203, 213, 225), Color.FromArgb(51, 65, 85));
        private static readonly (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) NutNguyHiem =
            (Color.FromArgb(254, 242, 242), Color.FromArgb(254, 226, 226), Color.FromArgb(252, 205, 205), Color.FromArgb(252, 165, 165), Color.FromArgb(185, 28, 28));

        // =====================================================================================
        //  1) ÁP DỤNG GIAO DIỆN (gọi trong constructor, ngay sau InitializeComponent)
        // =====================================================================================
        private void ApDungGiaoDienWeb()
        {
            SuspendLayout();
            try
            {
                BackColor = MauNenTrang;

                DinhBoCucTong();
                DinhTheGroupBox();
                DinhKhoiTimKiem();
                DinhKhoiLenh();
                DinhKhoiCapNhat();
                DinhComboBoTron();
                DinhVungLuoi();
                DinhThanhTrangThai();
                DinhMenuNguCanh();
            }
            finally
            {
                ResumeLayout(true);
            }

            // Các phần bị Form6_Load đặt lại -> áp dụng lại SAU Form6_Load (đăng ký sau nên chạy sau).
            Load += (s, e) => { HoanThienGiaoDienSauLoad(); CanGiuaNoiDung(); };
            SizeChanged += (s, e) => CanGiuaNoiDung();
        }

        // ---------- Bố cục tổng: nền trang + khoảng đệm + hàng đầu cố định, phần dưới co giãn ----------
        private void DinhBoCucTong()
        {
            tableLayoutPanel1.BackColor = MauNenTrang;
            tableLayoutPanel1.Padding = new Padding(12, 10, 12, 8);
            tableLayoutPanel1.RowStyles.Clear();
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F)); // hàng tìm kiếm + lệnh
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // form cập nhật + lưới

            tableLayoutPanel2.Margin = new Padding(0);
            tableLayoutPanel2.ColumnStyles.Clear();
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46F));

            // Logo: giữ đúng tỉ lệ ảnh (trước đây kéo giãn làm méo hình)
            pictureBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.Margin = new Padding(0, 8, 14, 8);

            // Vùng dưới: thẻ "Cập nhật" cố định chiều cao, lưới ăn hết phần còn lại khi resize
            splitContainer1.Margin = new Padding(0, 8, 0, 0);
            splitContainer1.BackColor = MauNenTrang;
            splitContainer1.Panel1.BackColor = MauNenTrang;
            splitContainer1.Panel2.BackColor = MauNenTrang;
            try
            {
                splitContainer1.SplitterWidth = 10;                 // khe hở giữa 2 thẻ
                splitContainer1.Panel1MinSize = 110;
                splitContainer1.Panel2MinSize = 120;
                splitContainer1.FixedPanel = FixedPanel.Panel1;     // resize cửa sổ chỉ co giãn lưới
                splitContainer1.SplitterDistance = 134;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                // Kích thước container chưa đủ: bỏ qua, giữ giá trị mặc định của Designer
            }
        }

        // ---------- Biến 3 GroupBox thành "thẻ" trắng bo góc (vẽ đè bằng sự kiện Paint) ----------
        // GroupBox tự vẽ chữ/viền nên không thể "vẽ đè" ổn định (bị chồng 2 tiêu đề).
        // => Chuyển bảng con sang 1 thẻ tự vẽ (TheBoGocF6) thay chỗ GroupBox; GroupBox được giữ lại ở dạng
        //    ẩn để code gốc vẫn gán groupBoxN.Text như cũ, và Text đó được đồng bộ lên tiêu đề của thẻ.
        private void DinhTheGroupBox()
        {
            BocTheTieuDe(groupBox1_TimKiemThongTinCBCS, tableLayoutPanel5, new Padding(0, 0, 10, 0));
            BocTheTieuDe(groupBox2_KhoiLenhDieuKhien, tableLayoutPanel6, new Padding(0));
            BocTheTieuDe(groupBox3_ThongTinCBCS, tableLayoutPanel3, new Padding(0));
        }

        private static void BocTheTieuDe(GroupBox gb, Control noiDung, Padding margin)
        {
            var cha = gb.Parent;
            if (cha == null) return;

            var the = new TheBoGocF6
            {
                Dock = DockStyle.Fill,
                Margin = margin,
                ForeColor = MauChuDam,
                TieuDe = gb.Text,
                Padding = new Padding(12, 34, 12, 8)   // 34 = vùng tiêu đề, tự tính -> không bao giờ đè lên ô nhập
            };

            cha.SuspendLayout();
            try
            {
                if (cha is TableLayoutPanel tlp)
                {
                    var vt = tlp.GetPositionFromControl(gb);
                    tlp.Controls.Remove(gb);
                    tlp.Controls.Add(the, vt.Column, vt.Row);
                }
                else
                {
                    cha.Controls.Remove(gb);
                    cha.Controls.Add(the);
                }

                gb.Visible = false;
                gb.Dock = DockStyle.None;
                the.Controls.Add(noiDung);       // chuyển bảng con (giữ nguyên toàn bộ control bên trong)
                noiDung.Dock = DockStyle.Fill;
                the.Controls.Add(gb);            // giữ GroupBox (ẩn) để logic gán .Text không lỗi
                gb.TextChanged += (s, e) => the.TieuDe = gb.Text;
            }
            finally
            {
                cha.ResumeLayout(true);
            }
        }

        // ---------- Giữ bố cục gọn trên màn hình rộng: nội dung tối đa RongToiDa, căn giữa ----------
        private const int RongToiDa = 1440; // đơn vị thiết kế (96 DPI). Muốn rộng hơn/hẹp hơn chỉ cần sửa số này.

        private void CanGiuaNoiDung()
        {
            if (IsDisposed || tableLayoutPanel1 == null) return;
            int toiDa = LogicalToDeviceUnits(RongToiDa);
            int leToiThieu = LogicalToDeviceUnits(12);
            int le = Math.Max(leToiThieu, (ClientSize.Width - toiDa) / 2);
            var p = tableLayoutPanel1.Padding;
            if (p.Left == le && p.Right == le) return;
            tableLayoutPanel1.Padding = new Padding(le, p.Top, le, p.Bottom);
        }

        // ---------- Thẻ 1: Tìm kiếm ----------
        private void DinhKhoiTimKiem()
        {
            // Cột nhãn tự co theo chữ, cột ô nhập chia theo tỉ lệ -> không còn lệch/cắt chữ khi resize
            tableLayoutPanel5.ColumnStyles.Clear();
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44F));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50F));
            tableLayoutPanel5.Margin = new Padding(0);

            DinhNhan(label3, true);
            DinhNhan(label4);
            DinhNhan(label5);

            DinhODauVao(textBox_TimKiemTheoTen);
            DinhODauVao(comboBox_TimKiemDonVi);
            DinhODauVao(comboBox_XepLoaiThiDua);

            KieuNut(kryptonButton_LamMoiCacOTimKiem, FontNut, NutVien);
            CanNutTheoO(kryptonButton_LamMoiCacOTimKiem, new Padding(8, 0, 0, 0));
        }

        // ---------- Thẻ 2: Khối lệnh ----------
        private void DinhKhoiLenh()
        {
            tableLayoutPanel6.Margin = new Padding(0);
            tableLayoutPanel6.ColumnStyles.Clear();
            tableLayoutPanel6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29F));
            tableLayoutPanel6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));
            tableLayoutPanel6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29F));

            KieuNut(kryptonButton1_ThiDuaBaNhat, FontNut, NutXanhNhat);
            KieuNut(kryptonButton_RefershCSDL, FontNut, NutVien);
            KieuNut(kryptonButton_XoaKetQuaPhanLoai, FontNut, NutNguyHiem);
            KieuNut(kryptonButton1_HuongDanThemDuLieu, FontNut, NutChinh);

            CanNutTheoO(kryptonButton1_ThiDuaBaNhat, new Padding(0, 0, 8, 0));
            CanNutTheoO(kryptonButton_RefershCSDL, new Padding(0, 0, 8, 0));
            CanNutTheoO(kryptonButton_XoaKetQuaPhanLoai, new Padding(0, 0, 8, 0));
            CanNutTheoO(kryptonButton1_HuongDanThemDuLieu, new Padding(0));
        }

        // ---------- Thẻ 3: Form cập nhật thông tin (2 hàng ô nhập) ----------
        private void DinhKhoiCapNhat()
        {
            tableLayoutPanel3.Margin = new Padding(0);
            tableLayoutPanel8.Margin = new Padding(0);
            tableLayoutPanel9.Margin = new Padding(0);
            tableLayoutPanel9.ForeColor = MauChuDam; // (trước đây xanh đậm 0,0,192 -> ô nhập bị chữ xanh)

            // Hàng 1: STT | Họ và tên | Số hiệu | Năm sinh | Quê quán | [Xóa]
            tableLayoutPanel8.ColumnStyles.Clear();
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 0  nhãn STT
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 7F));       // 1  STT
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 2  nhãn Họ và tên
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31F));      // 3  Họ và tên
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 4  nhãn Số hiệu
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13F));      // 5  Số hiệu
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 6  nhãn Năm sinh
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11F));      // 7  Năm sinh
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 8  nhãn Quê quán
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));      // 9  Quê quán
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));    // 10 nút Xóa

            // Hàng 2: Vào CAND | Cấp bậc | Chức vụ | Đơn vị | Phân loại | Ghi chú | [Cập nhật]
            tableLayoutPanel9.ColumnStyles.Clear();
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 0  nhãn Vào CAND
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9F));       // 1  Vào CAND
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 2  nhãn Cấp bậc
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11F));      // 3  Cấp bậc
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 4  nhãn Chức vụ
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));      // 5  Chức vụ
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 6  nhãn Đơn vị
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));      // 7  Đơn vị
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 8  nhãn Phân loại
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));      // 9  Phân loại
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));          // 10 nhãn Ghi chú
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));      // 11 Ghi chú
            tableLayoutPanel9.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));    // 12 nút Cập nhật

            DinhNhan(label6, true); DinhNhan(label7); DinhNhan(label8); DinhNhan(label9); DinhNhan(label10);
            DinhNhan(label12, true); DinhNhan(label13); DinhNhan(label14); DinhNhan(label11); DinhNhan(label16); DinhNhan(label15);

            DinhODauVao(textBox_STT);
            DinhODauVao(textBox_HoVaTen);
            DinhODauVao(textBox_SoHieu);
            DinhODauVao(textBox_NamSinh);
            DinhODauVao(textBox_QueQuan);
            DinhODauVao(textBox_NgayVaoCAND);
            DinhODauVao(textBox_CapBac);
            DinhODauVao(comboBox_ChucVu);
            DinhODauVao(textBox_DonVi);
            DinhODauVao(comboBox_PhanLoai);
            DinhODauVao(textBox_GhiChu);

            KieuNut(kryptonButton_XoaDataCBCS, FontNut, NutNguyHiem);
            KieuNut(kryptonButton_LuuDataCapNhat, FontNut, NutXanhLa);
            CanNutTheoO(kryptonButton_XoaDataCBCS, new Padding(10, 0, 0, 0));
            CanNutTheoO(kryptonButton_LuuDataCapNhat, new Padding(10, 0, 0, 0));
        }

        // ---------- 4 ComboBox: ô chọn + danh sách sổ xuống bo tròn (dùng chung OComboWebF4 / PopupDanhSachF4 trong GiaoDienForm4Web.cs) ----------
        // ComboBox gốc vẫn còn nguyên (Items, Text, SelectedIndex, sự kiện, Enabled/Visible do logic điều khiển); chỉ được ẩn hình và
        // thay bằng ô vẽ tay đặt đúng vị trí. Chọn trên ô mới sẽ gán SelectedIndex của ComboBox gốc nên mọi sự kiện chạy như cũ.
        private void DinhComboBoTron()
        {
            foreach (var cb in new[] { comboBox_TimKiemDonVi, comboBox_XepLoaiThiDua, comboBox_ChucVu, textBox_CapBac, textBox_DonVi, comboBox_PhanLoai })
                if (cb != null && cb.Parent != null) OComboWebF4.Gan(cb);
        }

        // ---------- Lưới dữ liệu nằm trong 1 thẻ trắng bo góc ----------
        private void DinhVungLuoi()
        {
            var the = new TheBoGocF6 { Dock = DockStyle.Fill, Padding = new Padding(10) };
            var p2 = splitContainer1.Panel2;
            p2.SuspendLayout();
            try
            {
                p2.Controls.Add(the);
                the.Controls.Add(kryptonDataGridView1);   // tự gỡ khỏi Panel2, giữ nguyên mọi sự kiện/logic của lưới
                kryptonDataGridView1.Dock = DockStyle.Fill;
                kryptonDataGridView1.BorderStyle = BorderStyle.None;
                kryptonDataGridView1.CellPainting -= VeOGocSTT;
                kryptonDataGridView1.CellPainting += VeOGocSTT;   // đặt tên cho ô góc trên-trái (cột số thứ tự)
            }
            finally
            {
                p2.ResumeLayout(true);
            }
        }

        // Ô góc trên-trái của lưới (phía trên cột số thứ tự) mặc định để trống -> vẽ chữ "STT" cùng tông với tiêu đề cột.
        // Tự vẽ nền + chữ nên không phụ thuộc cách Krypton vẽ ô này; chỉ trình bày, không đụng dữ liệu.
        private void VeOGocSTT(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex != -1 || e.ColumnIndex != -1 || sender is not DataGridView dgv) return;

            var o = e.CellBounds;
            using (var nen = new SolidBrush(Color.FromArgb(237, 233, 254)))
                e.Graphics.FillRectangle(nen, o);
            using (var vien = new Pen(Color.FromArgb(221, 214, 254)))
            {
                e.Graphics.DrawLine(vien, o.Left, o.Bottom - 1, o.Right - 1, o.Bottom - 1);
                e.Graphics.DrawLine(vien, o.Right - 1, o.Top, o.Right - 1, o.Bottom - 1);
            }
            var font = dgv.ColumnHeadersDefaultCellStyle.Font ?? dgv.Font;   // cùng font đậm với tiêu đề cột
            TextRenderer.DrawText(e.Graphics, "STT", font, o, Color.FromArgb(91, 33, 182),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            e.Handled = true;
        }

        // ---------- Thanh trạng thái ----------
        private void DinhThanhTrangThai()
        {
            statusStrip1.BackColor = Color.White;               // (trước đây xanh lá nhạt)
            statusStrip1.ForeColor = MauChuNhan;
            statusStrip1.SizingGrip = false;
            statusStrip1.Padding = new Padding(10, 0, 10, 0);
            toolStripStatusLabel1.Font = FontTrangThai;
            foreach (var lbl in new[] { toolStripStatusLabel2_Loai1, toolStripStatusLabel2_Loai2, toolStripStatusLabel2_Loai3,
                                        toolStripStatusLabel2_Loai4, toolStripStatusLabel2_KhongPL })
            {
                lbl.Font = FontTrangThai;                       // màu chữ từng loại do Form6_Load đặt, giữ nguyên
                lbl.Margin = new Padding(6, 3, 10, 2);
            }
        }

        // ---------- Menu chuột phải ----------
        private void DinhMenuNguCanh()
        {
            contextMenuStrip1.Font = FontMenu;
            contextMenuStrip1.Renderer = new ToolStripProfessionalRenderer(new BangMauMenuF6()) { RoundedEdges = false };
        }

        // =====================================================================================
        //  2) ÁP DỤNG LẠI SAU Form6_Load (các thuộc tính bị code gốc đặt lại trong Load)
        // =====================================================================================
        private void HoanThienGiaoDienSauLoad()
        {
            try
            {
                // Ô nhập Krypton: bo góc đồng nhất (DangKyHieuUngVienTextBox của code gốc đặt = 3)
                foreach (var t in new[] { textBox_TimKiemTheoTen, textBox_STT, textBox_HoVaTen, textBox_SoHieu, textBox_NamSinh,
                                          textBox_QueQuan, textBox_NgayVaoCAND, textBox_GhiChu })
                {
                    t.StateCommon.Border.Rounding = 6F;
                }
                DinhGiaoDienLuoi();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[GiaoDienWeb] " + ex.Message); // giao diện không bao giờ được làm sập form
            }
        }

        // Lưới: tiêu đề xám nhạt, chỉ kẻ ngang từng dòng, dòng chọn xanh nhạt (ghi đè tông tím của DinhDangDataGridViewHienDai)
        private void DinhGiaoDienLuoi()
        {
            var dgv = kryptonDataGridView1;
            dgv.SuspendLayout();
            try
            {
                dgv.StateCommon.Background.Color1 = Color.White;

                // Tiêu đề cột: nền tím nhạt + chữ tím đậm (đúng tông yêu cầu)
                dgv.StateCommon.HeaderColumn.Back.Color1 = Color.FromArgb(237, 233, 254);
                dgv.StateCommon.HeaderColumn.Back.Color2 = Color.FromArgb(237, 233, 254);
                dgv.StateCommon.HeaderColumn.Content.Color1 = Color.FromArgb(91, 33, 182);
                dgv.StateCommon.HeaderColumn.Border.Color1 = Color.FromArgb(221, 214, 254);
                dgv.StateCommon.HeaderColumn.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;

                // Cột số thứ tự bên trái: tím rất nhạt cho liền tông
                dgv.StateCommon.HeaderRow.Back.Color1 = Color.FromArgb(250, 248, 255);
                dgv.StateCommon.HeaderRow.Back.Color2 = Color.FromArgb(250, 248, 255);
                dgv.StateCommon.HeaderRow.Border.Color1 = Color.FromArgb(237, 233, 254);
                dgv.RowHeadersDefaultCellStyle.ForeColor = MauChuPhu;   // số thứ tự do RowPostPaint vẽ

                dgv.StateCommon.DataCell.Back.Color1 = Color.White;
                dgv.StateCommon.DataCell.Content.Color1 = Color.FromArgb(51, 65, 85);
                dgv.StateCommon.DataCell.Border.Color1 = Color.FromArgb(241, 245, 249);
                dgv.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Bottom;

                dgv.StateSelected.DataCell.Back.Color1 = Color.FromArgb(239, 246, 255);
                dgv.StateSelected.DataCell.Back.Color2 = Color.FromArgb(239, 246, 255);
                dgv.StateSelected.DataCell.Content.Color1 = Color.FromArgb(29, 78, 216);
            }
            finally
            {
                dgv.ResumeLayout();
            }
        }

        // =====================================================================================
        //  3) HÀM TIỆN ÍCH TRÌNH BÀY
        // =====================================================================================

        // Nhãn: chữ thường, màu slate (bỏ in nghiêng + xanh đậm cũ), căn trái, có khoảng cách với ô nhập
        private static void DinhNhan(Label lb, bool dauHang = false)
        {
            lb.Font = FontNhan;
            lb.ForeColor = MauChuNhan;
            lb.Anchor = AnchorStyles.Left;
            lb.Margin = new Padding(dauHang ? 0 : 10, 0, 6, 0);
        }

        // Ô nhập (TextBox Krypton / ComboBox): lấp đầy ô lưới, khoảng đệm dọc đều
        private static void DinhODauVao(Control c)
        {
            c.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            c.Margin = new Padding(0, 3, 0, 3);
        }

        // Nút: lấp đầy chiều ngang ô, cao đều 32px -> tự co giãn đẹp khi đổi kích thước cửa sổ
        private static void CanNutTheoO(Krypton.Toolkit.KryptonButton b, Padding margin)
        {
            b.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            b.Margin = margin;
            b.Height = 32;
        }

        private static void KieuNut(Krypton.Toolkit.KryptonButton b, Font font,
            (Color Nen, Color Hover, Color Down, Color Vien, Color Chu) m)
        {
            b.StateCommon.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            b.StateCommon.Border.Rounding = 8F;
            b.StateCommon.Border.Width = 1;
            b.StateCommon.Content.ShortText.Font = font;
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
            // Trạng thái vô hiệu hóa (khi đang làm mới/xử lý): xám nhạt, vẫn đọc được chữ
            b.StateDisabled.Back.Color1 = Color.FromArgb(241, 245, 249); b.StateDisabled.Back.Color2 = Color.FromArgb(241, 245, 249);
            b.StateDisabled.Border.Color1 = Color.FromArgb(226, 232, 240); b.StateDisabled.Border.Color2 = Color.FromArgb(226, 232, 240);
            b.StateDisabled.Content.ShortText.Color1 = Color.FromArgb(148, 163, 184);
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
        //  4) LỚP PHỤ TRỢ TRÌNH BÀY
        // =====================================================================================

        /// <summary>Thẻ trắng bo góc có viền mảnh (nền ngoài lấy màu của control cha).</summary>
        internal sealed class TheBoGocF6 : Panel
        {
            private string _tieuDe = "";
            /// <summary>Tiêu đề vẽ ở góc trên trái thẻ (có thanh nhấn xanh). Rỗng = không vẽ.</summary>
            public string TieuDe
            {
                get => _tieuDe;
                set { _tieuDe = value ?? ""; Invalidate(); }
            }

            public TheBoGocF6()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
                BackColor = Color.White;
            }
            protected override void OnPaintBackground(PaintEventArgs e) =>
                e.Graphics.Clear(Parent?.BackColor ?? Color.FromArgb(243, 244, 246));
            protected override void OnPaint(PaintEventArgs e)
            {
                if (Width < 4 || Height < 4) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int d = (int)(20 * DeviceDpi / 96f); // bán kính 10 (scale theo DPI)
                var r = new Rectangle(0, 0, Width - 1, Height - 1);
                using var path = TaoDuongBoGoc(r, d);
                using var b = new SolidBrush(Color.White);
                using var p = new Pen(Color.FromArgb(226, 232, 240));
                g.FillPath(b, path);
                g.DrawPath(p, path);

                if (_tieuDe.Length > 0)
                {
                    float k = DeviceDpi / 96f;
                    int x = (int)(14 * k), w = Math.Max(3, (int)(3 * k));
                    using (var bar = new SolidBrush(MauChinh))
                        g.FillRectangle(bar, x, (int)(12 * k), w, (int)(14 * k));
                    TextRenderer.DrawText(g, _tieuDe, FontTieuDeThe, new Point(x + w + (int)(7 * k), (int)(10 * k)), MauChuDam,
                        TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.Left | TextFormatFlags.SingleLine);
                }
            }
        }

        /// <summary>Bảng màu menu chuột phải: nền trắng, hover xanh nhạt, viền mảnh.</summary>
        internal sealed class BangMauMenuF6 : ProfessionalColorTable
        {
            public BangMauMenuF6() { UseSystemColors = false; }
            public override Color ToolStripDropDownBackground => Color.White;
            public override Color ImageMarginGradientBegin => Color.White;
            public override Color ImageMarginGradientMiddle => Color.White;
            public override Color ImageMarginGradientEnd => Color.White;
            public override Color MenuBorder => Color.FromArgb(226, 232, 240);
            public override Color MenuItemBorder => Color.FromArgb(191, 219, 254);
            public override Color MenuItemSelected => Color.FromArgb(239, 246, 255);
            public override Color MenuItemSelectedGradientBegin => Color.FromArgb(239, 246, 255);
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(239, 246, 255);
            public override Color SeparatorDark => Color.FromArgb(226, 232, 240);
            public override Color SeparatorLight => Color.White;
        }
    }
}
