///Phiên bản 2
///
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Krypton.Toolkit;
namespace PhanMemThiDua2026
{
    partial class Form24_XacMinhAdmin
    {
        private IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }
        #region Windows Form Designer generated code
        // ============================================================
        //  BỐ CỤC NGANG (kiểu trang web đăng nhập 2 cột): ClientSize 900 x 520
        //  Trái (42%): nền chàm nhạt - biểu tượng, tiêu đề, mô tả, lưu ý
        //  Phải (58%): nền trắng - tiêu đề, Tên đăng nhập, Mật khẩu, Hiện mật khẩu / Caps Lock, [Xác thực] [Thoát]
        //  Không dùng KryptonGroup bọc ngoài và không có Paint tùy biến => mở nhanh, vẽ nhẹ.
        //  Giữ nguyên tên mọi control cũ và các khóa resx (PictureBox1.Image, btn_XacThuc/btn_Thoat.Values.Image, $this.Icon).
        // ============================================================
        private void InitializeComponent()
        {
            components = new Container();
            ComponentResourceManager resources = new ComponentResourceManager(typeof(Form24_XacMinhAdmin));
            PictureBox1 = new PictureBox();
            label1 = new Label();
            label2 = new Label();
            label_TieuDe = new Label();
            label_MoTa = new Label();
            label_LuuY = new Label();
            label_DangNhap = new Label();
            label_DangNhapPhu = new Label();
            label_CapsLock = new Label();
            text_TenDangNhap = new KryptonTextBox();
            text_MatKhau = new KryptonTextBox();
            check_HienMatKhau = new CheckBox();
            btn_XacThuc = new KryptonButton();
            btn_Thoat = new KryptonButton();
            pnlTrai = new Panel();
            pnlLogo = new VongTronLogo();
            pnlPhai = new Panel();
            TableLayoutPanel5 = new TableLayoutPanel();
            tblTrai = new TableLayoutPanel();
            TableLayoutPanel1 = new TableLayoutPanel();
            TableLayoutPanel2 = new TableLayoutPanel();
            TableLayoutPanel3 = new TableLayoutPanel();
            tblPhu = new TableLayoutPanel();
            tblNut = new TableLayoutPanel();
            toolTip1 = new ToolTip(components);
            ((ISupportInitialize)PictureBox1).BeginInit();
            TableLayoutPanel5.SuspendLayout();
            pnlTrai.SuspendLayout();
            tblTrai.SuspendLayout();
            pnlLogo.SuspendLayout();
            pnlPhai.SuspendLayout();
            TableLayoutPanel1.SuspendLayout();
            TableLayoutPanel2.SuspendLayout();
            TableLayoutPanel3.SuspendLayout();
            tblPhu.SuspendLayout();
            tblNut.SuspendLayout();
            SuspendLayout();
            //
            // PictureBox1
            //
            PictureBox1.Anchor = AnchorStyles.None;
            PictureBox1.BackColor = Color.Transparent;
            PictureBox1.Image = (Image)resources.GetObject("PictureBox1.Image");
            PictureBox1.Location = new Point(22, 25);
            PictureBox1.Margin = new Padding(0);
            PictureBox1.Name = "PictureBox1";
            PictureBox1.Size = new Size(76, 70);
            PictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            PictureBox1.TabIndex = 0;
            PictureBox1.TabStop = false;
            //
            // label_TieuDe
            //
            label_TieuDe.BackColor = Color.Transparent;
            label_TieuDe.Dock = DockStyle.Fill;
            label_TieuDe.Font = new Font("Segoe UI", 19F, FontStyle.Bold);
            label_TieuDe.ForeColor = Color.White;
            label_TieuDe.Location = new Point(0, 220);
            label_TieuDe.Margin = new Padding(0);
            label_TieuDe.Name = "label_TieuDe";
            label_TieuDe.Size = new Size(314, 52);
            label_TieuDe.TabIndex = 1;
            label_TieuDe.Text = "Xác minh quản trị viên";
            label_TieuDe.TextAlign = ContentAlignment.MiddleCenter;
            //
            // label_MoTa
            //
            label_MoTa.BackColor = Color.Transparent;
            label_MoTa.Dock = DockStyle.Fill;
            label_MoTa.Font = new Font("Segoe UI", 10.5F);
            label_MoTa.ForeColor = Color.FromArgb(237, 233, 254);
            label_MoTa.Location = new Point(0, 272);
            label_MoTa.Margin = new Padding(0);
            label_MoTa.Name = "label_MoTa";
            label_MoTa.Size = new Size(314, 56);
            label_MoTa.TabIndex = 2;
            label_MoTa.Text = "Vui lòng nhập tài khoản quản trị để tiếp tục.";
            label_MoTa.TextAlign = ContentAlignment.TopCenter;
            //
            // label_LuuY
            //
            label_LuuY.BackColor = Color.Transparent;
            label_LuuY.Dock = DockStyle.Fill;
            label_LuuY.Font = new Font("Segoe UI", 9.5F);
            label_LuuY.ForeColor = Color.FromArgb(221, 214, 254);
            label_LuuY.Location = new Point(0, 328);
            label_LuuY.Margin = new Padding(0);
            label_LuuY.Name = "label_LuuY";
            label_LuuY.Size = new Size(314, 64);
            label_LuuY.TabIndex = 3;
            label_LuuY.Text = "Thao tác này yêu cầu quyền quản trị.\r\nNhập sai 3 lần, phần mềm sẽ tự động đóng.";
            label_LuuY.TextAlign = ContentAlignment.TopCenter;
            //
            // pnlLogo  (vòng tròn trắng sau biểu tượng - vẽ trong lớp VongTronLogo)
            // 
            pnlLogo.Anchor = AnchorStyles.None;
            pnlLogo.BackColor = Color.Transparent;
            pnlLogo.Controls.Add(PictureBox1);
            pnlLogo.Location = new Point(97, 6);
            pnlLogo.Margin = new Padding(0);
            pnlLogo.Name = "pnlLogo";
            pnlLogo.Size = new Size(120, 120);
            pnlLogo.TabIndex = 0;
            // 
            // tblTrai
            //
            tblTrai.BackColor = Color.Transparent;
            tblTrai.ColumnCount = 1;
            tblTrai.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblTrai.Controls.Add(pnlLogo, 0, 1);
            tblTrai.Controls.Add(label_TieuDe, 0, 2);
            tblTrai.Controls.Add(label_MoTa, 0, 3);
            tblTrai.Controls.Add(label_LuuY, 0, 4);
            tblTrai.Dock = DockStyle.Fill;
            tblTrai.Location = new Point(32, 24);
            tblTrai.Margin = new Padding(0);
            tblTrai.Name = "tblTrai";
            tblTrai.RowCount = 6;
            tblTrai.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tblTrai.RowStyles.Add(new RowStyle(SizeType.Absolute, 132F));
            tblTrai.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            tblTrai.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            tblTrai.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            tblTrai.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tblTrai.Size = new Size(314, 472);
            tblTrai.TabIndex = 0;
            //
            // pnlTrai
            //
            pnlTrai.BackColor = Color.FromArgb(91, 33, 182);
            pnlTrai.Controls.Add(tblTrai);
            pnlTrai.Dock = DockStyle.Fill;
            pnlTrai.Location = new Point(0, 0);
            pnlTrai.Margin = new Padding(0);
            pnlTrai.Name = "pnlTrai";
            pnlTrai.Padding = new Padding(32, 24, 32, 24);
            pnlTrai.Size = new Size(378, 520);
            pnlTrai.TabIndex = 0;
            //
            // label_DangNhap
            //
            label_DangNhap.BackColor = Color.Transparent;
            label_DangNhap.Dock = DockStyle.Fill;
            label_DangNhap.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
            label_DangNhap.ForeColor = Color.FromArgb(127, 119, 219); // Thay màu ở đây
            label_DangNhap.Location = new Point(0, 50);
            label_DangNhap.Margin = new Padding(0);
            label_DangNhap.Name = "label_DangNhap";
            label_DangNhap.Size = new Size(402, 44);
            label_DangNhap.TabIndex = 0;
            label_DangNhap.Text = "Đăng nhập tài khoản quản trị";
            label_DangNhap.TextAlign = ContentAlignment.MiddleLeft;
            //
            // label_DangNhapPhu
            //
            label_DangNhapPhu.BackColor = Color.Transparent;
            label_DangNhapPhu.Dock = DockStyle.Fill;
            label_DangNhapPhu.Font = new Font("Segoe UI", 10F);
            label_DangNhapPhu.ForeColor = Color.FromArgb(100, 116, 139);
            label_DangNhapPhu.Location = new Point(0, 94);
            label_DangNhapPhu.Margin = new Padding(0);
            label_DangNhapPhu.Name = "label_DangNhapPhu";
            label_DangNhapPhu.Size = new Size(402, 34);
            label_DangNhapPhu.TabIndex = 1;
            label_DangNhapPhu.Text = "Nhập mật khẩu xác thựctài khoản quản trị.";
            label_DangNhapPhu.TextAlign = ContentAlignment.TopLeft;
            //
            // label1
            //
            label1.Anchor = AnchorStyles.Left;
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            label1.ForeColor = Color.FromArgb(51, 65, 85);
            label1.Location = new Point(0, 5);
            label1.Margin = new Padding(0);
            label1.Name = "label1";
            label1.Size = new Size(99, 17);
            label1.TabIndex = 3;
            label1.Text = "Tên đăng nhập";
            //
            // label2
            //
            label2.Anchor = AnchorStyles.Left;
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            label2.ForeColor = Color.FromArgb(51, 65, 85);
            label2.Location = new Point(0, 5);
            label2.Margin = new Padding(0);
            label2.Name = "label2";
            label2.Size = new Size(66, 17);
            label2.TabIndex = 5;
            label2.Text = "Mật khẩu";
            //
            // label_CapsLock
            //
            label_CapsLock.Anchor = AnchorStyles.Right;
            label_CapsLock.AutoSize = true;
            label_CapsLock.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            label_CapsLock.ForeColor = Color.FromArgb(185, 28, 28);
            label_CapsLock.Location = new Point(282, 9);
            label_CapsLock.Margin = new Padding(0);
            label_CapsLock.Name = "label_CapsLock";
            label_CapsLock.Size = new Size(120, 15);
            label_CapsLock.TabIndex = 7;
            label_CapsLock.Text = "Caps Lock đang bật";
            label_CapsLock.Visible = false;
            //
            // text_TenDangNhap
            //
            text_TenDangNhap.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            text_TenDangNhap.Location = new Point(0, 28);
            text_TenDangNhap.Margin = new Padding(0);
            text_TenDangNhap.Name = "text_TenDangNhap";
            text_TenDangNhap.Size = new Size(402, 39);
            text_TenDangNhap.StateCommon.Back.Color1 = Color.White;
            text_TenDangNhap.StateCommon.Border.Color1 = Color.FromArgb(203, 213, 225);
            text_TenDangNhap.StateCommon.Border.Color2 = Color.FromArgb(203, 213, 225);
            text_TenDangNhap.StateCommon.Border.DrawBorders = PaletteDrawBorders.Top | PaletteDrawBorders.Bottom | PaletteDrawBorders.Left | PaletteDrawBorders.Right;
            text_TenDangNhap.StateCommon.Border.Rounding = 8F;
            text_TenDangNhap.StateCommon.Border.Width = 1;
            text_TenDangNhap.StateCommon.Content.Color1 = Color.FromArgb(15, 23, 42);
            text_TenDangNhap.StateCommon.Content.Font = new Font("Segoe UI", 10.5F);
            text_TenDangNhap.StateCommon.Content.Padding = new Padding(12, 6, 12, 6);
            text_TenDangNhap.TabIndex = 1;
            //
            // text_MatKhau
            //
            text_MatKhau.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            text_MatKhau.Location = new Point(0, 28);
            text_MatKhau.Margin = new Padding(0);
            text_MatKhau.Name = "text_MatKhau";
            text_MatKhau.PasswordChar = '●';
            text_MatKhau.Size = new Size(402, 39);
            text_MatKhau.StateCommon.Back.Color1 = Color.White;
            text_MatKhau.StateCommon.Border.Color1 = Color.FromArgb(203, 213, 225);
            text_MatKhau.StateCommon.Border.Color2 = Color.FromArgb(203, 213, 225);
            text_MatKhau.StateCommon.Border.DrawBorders = PaletteDrawBorders.Top | PaletteDrawBorders.Bottom | PaletteDrawBorders.Left | PaletteDrawBorders.Right;
            text_MatKhau.StateCommon.Border.Rounding = 8F;
            text_MatKhau.StateCommon.Border.Width = 1;
            text_MatKhau.StateCommon.Content.Color1 = Color.FromArgb(15, 23, 42);
            text_MatKhau.StateCommon.Content.Font = new Font("Segoe UI", 10.5F);
            text_MatKhau.StateCommon.Content.Padding = new Padding(12, 6, 12, 6);
            text_MatKhau.TabIndex = 2;
            //
            // check_HienMatKhau
            //
            check_HienMatKhau.Anchor = AnchorStyles.Left;
            check_HienMatKhau.AutoSize = true;
            check_HienMatKhau.Cursor = Cursors.Hand;
            check_HienMatKhau.Font = new Font("Segoe UI", 9.5F);
            check_HienMatKhau.ForeColor = Color.FromArgb(71, 85, 105);
            check_HienMatKhau.Location = new Point(4, 7);
            check_HienMatKhau.Margin = new Padding(4, 0, 0, 0);
            check_HienMatKhau.Name = "check_HienMatKhau";
            check_HienMatKhau.Size = new Size(110, 21);
            check_HienMatKhau.TabIndex = 3;
            check_HienMatKhau.Text = "Hiện mật khẩu";
            check_HienMatKhau.UseVisualStyleBackColor = true;
            check_HienMatKhau.CheckedChanged += check_HienMatKhau_CheckedChanged;
            //
            // btn_XacThuc
            //
            btn_XacThuc.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            btn_XacThuc.Cursor = Cursors.Hand;
            btn_XacThuc.Location = new Point(209, 9);
            btn_XacThuc.Margin = new Padding(8, 0, 0, 0);
            btn_XacThuc.Name = "btn_XacThuc";
            btn_XacThuc.Size = new Size(193, 44);
            btn_XacThuc.StateCommon.Back.Color1 = Color.FromArgb(99, 102, 241);
            btn_XacThuc.StateCommon.Back.Color2 = Color.FromArgb(99, 102, 241);
            btn_XacThuc.StateCommon.Border.DrawBorders = PaletteDrawBorders.Top | PaletteDrawBorders.Bottom | PaletteDrawBorders.Left | PaletteDrawBorders.Right;
            btn_XacThuc.StateCommon.Border.Rounding = 8F;
            btn_XacThuc.StateCommon.Border.Width = 0;
            btn_XacThuc.StateCommon.Content.ShortText.Color1 = Color.White;
            btn_XacThuc.StateCommon.Content.ShortText.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            btn_XacThuc.StatePressed.Back.Color1 = Color.FromArgb(67, 56, 202);
            btn_XacThuc.StatePressed.Back.Color2 = Color.FromArgb(67, 56, 202);
            btn_XacThuc.StateTracking.Back.Color1 = Color.FromArgb(79, 70, 229);
            btn_XacThuc.StateTracking.Back.Color2 = Color.FromArgb(79, 70, 229);
            btn_XacThuc.TabIndex = 4;
            btn_XacThuc.Values.DropDownArrowColor = Color.Empty;
            btn_XacThuc.Values.Image = (Image)resources.GetObject("btn_XacThuc.Values.Image");
            btn_XacThuc.Values.Text = " Xác thực";
            btn_XacThuc.Click += btn_XacThuc_Click;
            //
            // btn_Thoat
            //
            btn_Thoat.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            btn_Thoat.Cursor = Cursors.Hand;
            btn_Thoat.Location = new Point(0, 9);
            btn_Thoat.Margin = new Padding(0, 0, 8, 0);
            btn_Thoat.Name = "btn_Thoat";
            btn_Thoat.Size = new Size(193, 44);
            btn_Thoat.StateCommon.Back.Color1 = Color.White;
            btn_Thoat.StateCommon.Back.Color2 = Color.White;
            btn_Thoat.StateCommon.Border.Color1 = Color.FromArgb(226, 232, 240);
            btn_Thoat.StateCommon.Border.Color2 = Color.FromArgb(226, 232, 240);
            btn_Thoat.StateCommon.Border.DrawBorders = PaletteDrawBorders.Top | PaletteDrawBorders.Bottom | PaletteDrawBorders.Left | PaletteDrawBorders.Right;
            btn_Thoat.StateCommon.Border.Rounding = 8F;
            btn_Thoat.StateCommon.Border.Width = 1;
            btn_Thoat.StateCommon.Content.ShortText.Color1 = Color.FromArgb(71, 85, 105);
            btn_Thoat.StateCommon.Content.ShortText.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            btn_Thoat.StatePressed.Back.Color1 = Color.FromArgb(241, 245, 249);
            btn_Thoat.StatePressed.Back.Color2 = Color.FromArgb(241, 245, 249);
            btn_Thoat.StateTracking.Back.Color1 = Color.FromArgb(248, 250, 252);
            btn_Thoat.StateTracking.Back.Color2 = Color.FromArgb(248, 250, 252);
            btn_Thoat.TabIndex = 5;
            btn_Thoat.Values.DropDownArrowColor = Color.Empty;
            btn_Thoat.Values.Image = (Image)resources.GetObject("btn_Thoat.Values.Image");
            btn_Thoat.Values.Text = " Thoát";
            btn_Thoat.Click += btn_Thoat_Click;
            //
            // TableLayoutPanel2  (khối Tên đăng nhập)
            //
            TableLayoutPanel2.ColumnCount = 1;
            TableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            TableLayoutPanel2.Controls.Add(label1, 0, 0);
            TableLayoutPanel2.Controls.Add(text_TenDangNhap, 0, 1);
            TableLayoutPanel2.Dock = DockStyle.Fill;
            TableLayoutPanel2.Location = new Point(0, 128);
            TableLayoutPanel2.Margin = new Padding(0);
            TableLayoutPanel2.Name = "TableLayoutPanel2";
            TableLayoutPanel2.RowCount = 2;
            TableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            TableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            TableLayoutPanel2.Size = new Size(402, 80);
            TableLayoutPanel2.TabIndex = 2;
            //
            // TableLayoutPanel3  (khối Mật khẩu)
            //
            TableLayoutPanel3.ColumnCount = 1;
            TableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            TableLayoutPanel3.Controls.Add(label2, 0, 0);
            TableLayoutPanel3.Controls.Add(text_MatKhau, 0, 1);
            TableLayoutPanel3.Dock = DockStyle.Fill;
            TableLayoutPanel3.Location = new Point(0, 208);
            TableLayoutPanel3.Margin = new Padding(0);
            TableLayoutPanel3.Name = "TableLayoutPanel3";
            TableLayoutPanel3.RowCount = 2;
            TableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            TableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            TableLayoutPanel3.Size = new Size(402, 80);
            TableLayoutPanel3.TabIndex = 3;
            //
            // tblPhu  (Hiện mật khẩu | cảnh báo Caps Lock)
            //
            tblPhu.ColumnCount = 2;
            tblPhu.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            tblPhu.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            tblPhu.Controls.Add(check_HienMatKhau, 0, 0);
            tblPhu.Controls.Add(label_CapsLock, 1, 0);
            tblPhu.Dock = DockStyle.Fill;
            tblPhu.Location = new Point(0, 288);
            tblPhu.Margin = new Padding(0);
            tblPhu.Name = "tblPhu";
            tblPhu.RowCount = 1;
            tblPhu.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tblPhu.Size = new Size(402, 36);
            tblPhu.TabIndex = 4;
            //
            // tblNut  ([Xác thực] [Thoát])
            //
            tblNut.ColumnCount = 2;
            tblNut.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblNut.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblNut.Controls.Add(btn_Thoat, 0, 0);
            tblNut.Controls.Add(btn_XacThuc, 1, 0);
            tblNut.Dock = DockStyle.Fill;
            tblNut.Location = new Point(0, 324);
            tblNut.Margin = new Padding(0);
            tblNut.Name = "tblNut";
            tblNut.RowCount = 1;
            tblNut.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tblNut.Size = new Size(402, 62);
            tblNut.TabIndex = 5;
            //
            // TableLayoutPanel1  (cột form bên phải)
            //
            TableLayoutPanel1.BackColor = Color.Transparent;
            TableLayoutPanel1.ColumnCount = 1;
            TableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            TableLayoutPanel1.Controls.Add(label_DangNhap, 0, 1);
            TableLayoutPanel1.Controls.Add(label_DangNhapPhu, 0, 2);
            TableLayoutPanel1.Controls.Add(TableLayoutPanel2, 0, 3);
            TableLayoutPanel1.Controls.Add(TableLayoutPanel3, 0, 4);
            TableLayoutPanel1.Controls.Add(tblPhu, 0, 5);
            TableLayoutPanel1.Controls.Add(tblNut, 0, 6);
            TableLayoutPanel1.Dock = DockStyle.Fill;
            TableLayoutPanel1.Location = new Point(60, 0);
            TableLayoutPanel1.Margin = new Padding(0);
            TableLayoutPanel1.Name = "TableLayoutPanel1";
            TableLayoutPanel1.RowCount = 8;
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            TableLayoutPanel1.Size = new Size(402, 520);
            TableLayoutPanel1.TabIndex = 0;
            //
            // pnlPhai
            //
            pnlPhai.BackColor = Color.White;
            pnlPhai.Controls.Add(TableLayoutPanel1);
            pnlPhai.Dock = DockStyle.Fill;
            pnlPhai.Location = new Point(378, 0);
            pnlPhai.Margin = new Padding(0);
            pnlPhai.Name = "pnlPhai";
            pnlPhai.Padding = new Padding(60, 0, 60, 0);
            pnlPhai.Size = new Size(522, 520);
            pnlPhai.TabIndex = 1;
            //
            // TableLayoutPanel5  (khung chia đôi trái / phải)
            //
            TableLayoutPanel5.BackColor = Color.Transparent;
            TableLayoutPanel5.ColumnCount = 2;
            TableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            TableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            TableLayoutPanel5.Controls.Add(pnlTrai, 0, 0);
            TableLayoutPanel5.Controls.Add(pnlPhai, 1, 0);
            TableLayoutPanel5.Dock = DockStyle.Fill;
            TableLayoutPanel5.Location = new Point(0, 0);
            TableLayoutPanel5.Margin = new Padding(0);
            TableLayoutPanel5.Name = "TableLayoutPanel5";
            TableLayoutPanel5.RowCount = 1;
            TableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            TableLayoutPanel5.Size = new Size(900, 520);
            TableLayoutPanel5.TabIndex = 10;
            //
            // Form24_XacMinhAdmin
            //
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(900, 520);
            Controls.Add(TableLayoutPanel5);
            DoubleBuffered = true;
            Font = new Font("Segoe UI", 9.5F);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form24_XacMinhAdmin";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Xác minh quản trị viên";
            ((ISupportInitialize)PictureBox1).EndInit();
            TableLayoutPanel5.ResumeLayout(false);
            pnlTrai.ResumeLayout(false);
            tblTrai.ResumeLayout(false);
            pnlLogo.ResumeLayout(false);
            pnlPhai.ResumeLayout(false);
            TableLayoutPanel1.ResumeLayout(false);
            TableLayoutPanel2.ResumeLayout(false);
            TableLayoutPanel2.PerformLayout();
            TableLayoutPanel3.ResumeLayout(false);
            TableLayoutPanel3.PerformLayout();
            tblPhu.ResumeLayout(false);
            tblPhu.PerformLayout();
            tblNut.ResumeLayout(false);
            ResumeLayout(false);
        }
        #endregion
        internal PictureBox PictureBox1;
        internal CheckBox check_HienMatKhau;
        private KryptonTextBox text_MatKhau;
        private KryptonTextBox text_TenDangNhap;
        internal Label label1;
        internal Label label2;
        private Label label_TieuDe;
        private Label label_MoTa;
        private Label label_LuuY;
        private Label label_DangNhap;
        private Label label_DangNhapPhu;
        private Label label_CapsLock;
        private KryptonButton btn_XacThuc;
        private KryptonButton btn_Thoat;
        private Panel pnlTrai;
        private VongTronLogo pnlLogo;
        private Panel pnlPhai;
        private TableLayoutPanel tblTrai;
        private TableLayoutPanel tblPhu;
        private TableLayoutPanel tblNut;
        internal TableLayoutPanel TableLayoutPanel1;
        internal TableLayoutPanel TableLayoutPanel2;
        internal TableLayoutPanel TableLayoutPanel3;
        internal TableLayoutPanel TableLayoutPanel5;
        private ToolTip toolTip1;
    }
}