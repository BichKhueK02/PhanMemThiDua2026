namespace PhanMemThiDua2026
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            toolTip1 = new ToolTip(components);
            panelDangNhap = new Panel();
            label3_HienThiTenPhanMem = new Label();
            lblPhuDe = new Label();
            label1 = new Label();
            text_TenDangNhap = new Krypton.Toolkit.KryptonTextBox();
            label2 = new Label();
            text_MatKhau = new Krypton.Toolkit.KryptonTextBox();
            Check_HienMatKhau = new CheckBox();
            LinkLabel_QuenMatKhau = new LinkLabel();
            btn_DangNhap = new Krypton.Toolkit.KryptonButton();
            btn_Thoat = new Krypton.Toolkit.KryptonButton();
            LinkLabel1_DangKyTaiKhoanMoi = new LinkLabel();
            label1_ThongBaoPhienBan = new Label();
            label1_PhienBanPhanMem = new Label();
            TableLayoutPanel6 = new TableLayoutPanel();
            PictureBox1 = new PictureBox();
            lblChaoMung = new Label();
            lblMoTa = new Label();
            panelDangNhap.SuspendLayout();
            TableLayoutPanel6.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)PictureBox1).BeginInit();
            SuspendLayout();
            // 
            // panelDangNhap
            // 
            panelDangNhap.BackColor = Color.White;
            panelDangNhap.Controls.Add(label3_HienThiTenPhanMem);
            panelDangNhap.Controls.Add(lblPhuDe);
            panelDangNhap.Controls.Add(label1);
            panelDangNhap.Controls.Add(text_TenDangNhap);
            panelDangNhap.Controls.Add(label2);
            panelDangNhap.Controls.Add(text_MatKhau);
            panelDangNhap.Controls.Add(Check_HienMatKhau);
            panelDangNhap.Controls.Add(LinkLabel_QuenMatKhau);
            panelDangNhap.Controls.Add(btn_DangNhap);
            panelDangNhap.Controls.Add(btn_Thoat);
            panelDangNhap.Controls.Add(LinkLabel1_DangKyTaiKhoanMoi);
            panelDangNhap.Controls.Add(label1_ThongBaoPhienBan);
            panelDangNhap.Controls.Add(label1_PhienBanPhanMem);
            panelDangNhap.Dock = DockStyle.Fill;
            panelDangNhap.Location = new Point(340, 0);
            panelDangNhap.Name = "panelDangNhap";
            panelDangNhap.Size = new Size(520, 510);
            panelDangNhap.TabIndex = 0;
            // 
            // label3_HienThiTenPhanMem
            // 
            label3_HienThiTenPhanMem.AutoSize = true;
            label3_HienThiTenPhanMem.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3_HienThiTenPhanMem.ForeColor = Color.FromArgb(17, 24, 39);
            label3_HienThiTenPhanMem.Location = new Point(56, 48);
            label3_HienThiTenPhanMem.Name = "label3_HienThiTenPhanMem";
            label3_HienThiTenPhanMem.Size = new Size(275, 32);
            label3_HienThiTenPhanMem.TabIndex = 13;
            label3_HienThiTenPhanMem.Text = "Phần mềm thi đua 2026";
            // 
            // lblPhuDe
            // 
            lblPhuDe.AutoSize = true;
            lblPhuDe.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblPhuDe.ForeColor = Color.FromArgb(107, 114, 128);
            lblPhuDe.Location = new Point(60, 88);
            lblPhuDe.Name = "lblPhuDe";
            lblPhuDe.Size = new Size(146, 19);
            lblPhuDe.TabIndex = 14;
            lblPhuDe.Text = "Đăng nhập để tiếp tục";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.FromArgb(55, 65, 81);
            label1.Location = new Point(60, 140);
            label1.Name = "label1";
            label1.Size = new Size(90, 17);
            label1.TabIndex = 3;
            label1.Text = "Tên tài khoản";
            // 
            // text_TenDangNhap
            // 
            text_TenDangNhap.Location = new Point(60, 162);
            text_TenDangNhap.Margin = new Padding(3, 2, 3, 2);
            text_TenDangNhap.Name = "text_TenDangNhap";
            text_TenDangNhap.Size = new Size(400, 38); // Tăng chiều cao lên 38
            text_TenDangNhap.StateCommon.Back.Color1 = Color.White;
            text_TenDangNhap.StateCommon.Border.Color1 = Color.Silver;
            text_TenDangNhap.StateCommon.Border.Color2 = Color.Silver;
            text_TenDangNhap.StateCommon.Border.Rounding = 8F;
            text_TenDangNhap.StateCommon.Border.Width = 1;
            text_TenDangNhap.StateCommon.Content.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            text_TenDangNhap.StateCommon.Content.Padding = new Padding(10, 6, 10, 6); // Thêm padding trên/dưới là 6
            text_TenDangNhap.TabIndex = 0;
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.FromArgb(55, 65, 81);
            label2.Location = new Point(60, 216);
            label2.Name = "label2";
            label2.Size = new Size(66, 17);
            label2.TabIndex = 12;
            label2.Text = "Mật khẩu";
            // 
            // text_MatKhau
            // 
            text_MatKhau.Location = new Point(60, 238);
            text_MatKhau.Margin = new Padding(3, 2, 3, 2);
            text_MatKhau.Name = "text_MatKhau";
            text_MatKhau.Size = new Size(400, 38); // Tăng chiều cao lên 38
            text_MatKhau.StateCommon.Back.Color1 = Color.White;
            text_MatKhau.StateCommon.Border.Color1 = Color.Silver;
            text_MatKhau.StateCommon.Border.Color2 = Color.Silver;
            text_MatKhau.StateCommon.Border.Rounding = 8F;
            text_MatKhau.StateCommon.Border.Width = 1;
            text_MatKhau.StateCommon.Content.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            text_MatKhau.StateCommon.Content.Padding = new Padding(10, 6, 10, 6); // Thêm padding trên/dưới là 6
            text_MatKhau.TabIndex = 1;
            // Check_HienMatKhau
            // 
            Check_HienMatKhau.AutoSize = true;
            Check_HienMatKhau.Cursor = Cursors.Hand;
            Check_HienMatKhau.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Check_HienMatKhau.ForeColor = Color.Red;
            Check_HienMatKhau.Location = new Point(60, 288);
            Check_HienMatKhau.Name = "Check_HienMatKhau";
            Check_HienMatKhau.Size = new Size(110, 21);
            Check_HienMatKhau.TabIndex = 2;
            Check_HienMatKhau.Text = "Hiện mật khẩu";
            Check_HienMatKhau.UseVisualStyleBackColor = true;
            // 
            // LinkLabel_QuenMatKhau
            // 
            LinkLabel_QuenMatKhau.ActiveLinkColor = Color.FromArgb(55, 48, 163);
            LinkLabel_QuenMatKhau.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            LinkLabel_QuenMatKhau.LinkBehavior = LinkBehavior.HoverUnderline;
            LinkLabel_QuenMatKhau.LinkColor = Color.FromArgb(79, 70, 229);
            LinkLabel_QuenMatKhau.Location = new Point(320, 289);
            LinkLabel_QuenMatKhau.Name = "LinkLabel_QuenMatKhau";
            LinkLabel_QuenMatKhau.Size = new Size(140, 20);
            LinkLabel_QuenMatKhau.TabIndex = 3;
            LinkLabel_QuenMatKhau.TabStop = true;
            LinkLabel_QuenMatKhau.Text = "Quên mật khẩu?";
            LinkLabel_QuenMatKhau.TextAlign = ContentAlignment.MiddleRight;
            LinkLabel_QuenMatKhau.VisitedLinkColor = Color.FromArgb(79, 70, 229);
            LinkLabel_QuenMatKhau.LinkClicked += LinkLabel_QuenMatKhau_LinkClicked;
            // 
            // btn_DangNhap
            // 
            btn_DangNhap.Cursor = Cursors.Hand;
            btn_DangNhap.Location = new Point(60, 330);
            btn_DangNhap.Margin = new Padding(3, 2, 3, 2);
            btn_DangNhap.Name = "btn_DangNhap";
            btn_DangNhap.OverrideDefault.Back.Color1 = Color.FromArgb(79, 70, 229);
            btn_DangNhap.OverrideDefault.Back.Color2 = Color.FromArgb(79, 70, 229);
            btn_DangNhap.OverrideDefault.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn_DangNhap.OverrideDefault.Border.Color1 = Color.FromArgb(79, 70, 229);
            btn_DangNhap.OverrideDefault.Border.Color2 = Color.FromArgb(79, 70, 229);
            btn_DangNhap.OverrideDefault.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom | Krypton.Toolkit.PaletteDrawBorders.Left | Krypton.Toolkit.PaletteDrawBorders.Right;
            btn_DangNhap.OverrideDefault.Border.Rounding = 8F;
            btn_DangNhap.OverrideDefault.Border.Width = 1;
            btn_DangNhap.OverrideDefault.Content.ShortText.Color1 = Color.White;
            btn_DangNhap.OverrideDefault.Content.ShortText.Color2 = Color.White;
            btn_DangNhap.OverrideDefault.Content.ShortText.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btn_DangNhap.Size = new Size(400, 44);
            btn_DangNhap.StateCommon.Back.Color1 = Color.FromArgb(79, 70, 229);
            btn_DangNhap.StateCommon.Back.Color2 = Color.FromArgb(79, 70, 229);
            btn_DangNhap.StateCommon.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn_DangNhap.StateCommon.Border.Color1 = Color.FromArgb(79, 70, 229);
            btn_DangNhap.StateCommon.Border.Color2 = Color.FromArgb(79, 70, 229);
            btn_DangNhap.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom | Krypton.Toolkit.PaletteDrawBorders.Left | Krypton.Toolkit.PaletteDrawBorders.Right;
            btn_DangNhap.StateCommon.Border.Rounding = 8F;
            btn_DangNhap.StateCommon.Border.Width = 1;
            btn_DangNhap.StateCommon.Content.ShortText.Color1 = Color.White;
            btn_DangNhap.StateCommon.Content.ShortText.Color2 = Color.White;
            btn_DangNhap.StateCommon.Content.ShortText.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btn_DangNhap.StatePressed.Back.Color1 = Color.FromArgb(49, 46, 129);
            btn_DangNhap.StatePressed.Back.Color2 = Color.FromArgb(49, 46, 129);
            btn_DangNhap.StatePressed.Border.Color1 = Color.FromArgb(49, 46, 129);
            btn_DangNhap.StatePressed.Border.Color2 = Color.FromArgb(49, 46, 129);
            btn_DangNhap.StateTracking.Back.Color1 = Color.FromArgb(67, 56, 202);
            btn_DangNhap.StateTracking.Back.Color2 = Color.FromArgb(67, 56, 202);
            btn_DangNhap.StateTracking.Border.Color1 = Color.FromArgb(67, 56, 202);
            btn_DangNhap.StateTracking.Border.Color2 = Color.FromArgb(67, 56, 202);
            btn_DangNhap.TabIndex = 4;
            btn_DangNhap.Values.DropDownArrowColor = Color.Empty;
            btn_DangNhap.Values.Text = "Đăng nhập";
            btn_DangNhap.Click += btn_DangNhap_Click;
            // 
            // btn_Thoat
            // 
            btn_Thoat.Cursor = Cursors.Hand;
            btn_Thoat.Location = new Point(60, 384);
            btn_Thoat.Margin = new Padding(3, 2, 3, 2);
            btn_Thoat.Name = "btn_Thoat";
            btn_Thoat.OverrideDefault.Back.Color1 = Color.White;
            btn_Thoat.OverrideDefault.Back.Color2 = Color.White;
            btn_Thoat.OverrideDefault.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn_Thoat.OverrideDefault.Border.Color1 = Color.FromArgb(209, 213, 219);
            btn_Thoat.OverrideDefault.Border.Color2 = Color.FromArgb(209, 213, 219);
            btn_Thoat.OverrideDefault.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom | Krypton.Toolkit.PaletteDrawBorders.Left | Krypton.Toolkit.PaletteDrawBorders.Right;
            btn_Thoat.OverrideDefault.Border.Rounding = 8F;
            btn_Thoat.OverrideDefault.Border.Width = 1;
            btn_Thoat.OverrideDefault.Content.ShortText.Color1 = Color.FromArgb(75, 85, 99);
            btn_Thoat.OverrideDefault.Content.ShortText.Color2 = Color.FromArgb(75, 85, 99);
            btn_Thoat.OverrideDefault.Content.ShortText.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btn_Thoat.Size = new Size(400, 38);
            btn_Thoat.StateCommon.Back.Color1 = Color.White;
            btn_Thoat.StateCommon.Back.Color2 = Color.White;
            btn_Thoat.StateCommon.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn_Thoat.StateCommon.Border.Color1 = Color.FromArgb(209, 213, 219);
            btn_Thoat.StateCommon.Border.Color2 = Color.FromArgb(209, 213, 219);
            btn_Thoat.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom | Krypton.Toolkit.PaletteDrawBorders.Left | Krypton.Toolkit.PaletteDrawBorders.Right;
            btn_Thoat.StateCommon.Border.Rounding = 8F;
            btn_Thoat.StateCommon.Border.Width = 1;
            btn_Thoat.StateCommon.Content.ShortText.Color1 = Color.FromArgb(75, 85, 99);
            btn_Thoat.StateCommon.Content.ShortText.Color2 = Color.FromArgb(75, 85, 99);
            btn_Thoat.StateCommon.Content.ShortText.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btn_Thoat.StatePressed.Back.Color1 = Color.FromArgb(229, 231, 235);
            btn_Thoat.StatePressed.Back.Color2 = Color.FromArgb(229, 231, 235);
            btn_Thoat.StateTracking.Back.Color1 = Color.FromArgb(243, 244, 246);
            btn_Thoat.StateTracking.Back.Color2 = Color.FromArgb(243, 244, 246);
            btn_Thoat.StateTracking.Border.Color1 = Color.FromArgb(156, 163, 175);
            btn_Thoat.StateTracking.Border.Color2 = Color.FromArgb(156, 163, 175);
            btn_Thoat.TabIndex = 5;
            btn_Thoat.Values.DropDownArrowColor = Color.Empty;
            btn_Thoat.Values.Text = "Thoát";
            btn_Thoat.Click += btn_Thoat_Click;
            // 
            // LinkLabel1_DangKyTaiKhoanMoi
            // 
            LinkLabel1_DangKyTaiKhoanMoi.ActiveLinkColor = Color.FromArgb(55, 48, 163);
            LinkLabel1_DangKyTaiKhoanMoi.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            LinkLabel1_DangKyTaiKhoanMoi.LinkBehavior = LinkBehavior.HoverUnderline;
            LinkLabel1_DangKyTaiKhoanMoi.LinkColor = Color.FromArgb(79, 70, 229);
            LinkLabel1_DangKyTaiKhoanMoi.Location = new Point(60, 436);
            LinkLabel1_DangKyTaiKhoanMoi.Name = "LinkLabel1_DangKyTaiKhoanMoi";
            LinkLabel1_DangKyTaiKhoanMoi.Size = new Size(400, 22);
            LinkLabel1_DangKyTaiKhoanMoi.TabIndex = 6;
            LinkLabel1_DangKyTaiKhoanMoi.TabStop = true;
            LinkLabel1_DangKyTaiKhoanMoi.Text = "Đăng ký tài khoản mới";
            LinkLabel1_DangKyTaiKhoanMoi.TextAlign = ContentAlignment.MiddleCenter;
            LinkLabel1_DangKyTaiKhoanMoi.VisitedLinkColor = Color.FromArgb(79, 70, 229);
            LinkLabel1_DangKyTaiKhoanMoi.LinkClicked += LinkLabel1_DangKyTaiKhoanMoi_LinkClicked;
            // 
            // label1_ThongBaoPhienBan
            // 
            label1_ThongBaoPhienBan.AutoSize = true;
            label1_ThongBaoPhienBan.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic, GraphicsUnit.Point, 0);
            label1_ThongBaoPhienBan.ForeColor = Color.Green;
            label1_ThongBaoPhienBan.Location = new Point(60, 468);
            label1_ThongBaoPhienBan.Name = "label1_ThongBaoPhienBan";
            label1_ThongBaoPhienBan.Size = new Size(39, 15);
            label1_ThongBaoPhienBan.TabIndex = 12;
            label1_ThongBaoPhienBan.Text = "label1";
            // 
            // label1_PhienBanPhanMem
            // 
            label1_PhienBanPhanMem.AutoSize = true;
            label1_PhienBanPhanMem.Cursor = Cursors.Hand;
            label1_PhienBanPhanMem.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1_PhienBanPhanMem.ForeColor = Color.FromArgb(156, 163, 175);
            label1_PhienBanPhanMem.Location = new Point(60, 485);
            label1_PhienBanPhanMem.Name = "label1_PhienBanPhanMem";
            label1_PhienBanPhanMem.Size = new Size(12, 15);
            label1_PhienBanPhanMem.TabIndex = 13;
            label1_PhienBanPhanMem.Text = "*";
            label1_PhienBanPhanMem.Click += label1_PhienBanPhanMem_Click;
            // 
            // TableLayoutPanel6
            // 
            TableLayoutPanel6.BackColor = Color.FromArgb(79, 70, 229);
            TableLayoutPanel6.ColumnCount = 1;
            TableLayoutPanel6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            TableLayoutPanel6.Controls.Add(PictureBox1, 0, 0);
            TableLayoutPanel6.Controls.Add(lblChaoMung, 0, 1);
            TableLayoutPanel6.Controls.Add(lblMoTa, 0, 2);
            TableLayoutPanel6.Dock = DockStyle.Left;
            TableLayoutPanel6.Location = new Point(0, 0);
            TableLayoutPanel6.Margin = new Padding(0);
            TableLayoutPanel6.Name = "TableLayoutPanel6";
            TableLayoutPanel6.RowCount = 3;
            TableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Percent, 58F));
            TableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Percent, 14F));
            TableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Percent, 28F));
            TableLayoutPanel6.Size = new Size(340, 510);
            TableLayoutPanel6.TabIndex = 1;
            // 
            // PictureBox1
            // 
            PictureBox1.Anchor = AnchorStyles.None;
            PictureBox1.BackColor = Color.Transparent;
            PictureBox1.Image = (Image)resources.GetObject("PictureBox1.Image");
            PictureBox1.Location = new Point(60, 77);
            PictureBox1.Name = "PictureBox1";
            PictureBox1.Size = new Size(220, 140);
            PictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            PictureBox1.TabIndex = 0;
            PictureBox1.TabStop = false;
            // 
            // lblChaoMung
            // 
            lblChaoMung.Anchor = AnchorStyles.None;
            lblChaoMung.AutoSize = true;
            lblChaoMung.BackColor = Color.Transparent;
            lblChaoMung.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblChaoMung.ForeColor = Color.White;
            lblChaoMung.Location = new Point(73, 315);
            lblChaoMung.Name = "lblChaoMung";
            lblChaoMung.Size = new Size(193, 30);
            lblChaoMung.TabIndex = 1;
            lblChaoMung.Text = "Chào mừng trở lại";
            // 
            // lblMoTa
            // 
            lblMoTa.Anchor = AnchorStyles.Top;
            lblMoTa.BackColor = Color.Transparent;
            lblMoTa.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblMoTa.ForeColor = Color.FromArgb(224, 231, 255);
            lblMoTa.Location = new Point(30, 366);
            lblMoTa.Name = "lblMoTa";
            lblMoTa.Size = new Size(280, 50);
            lblMoTa.TabIndex = 2;
            lblMoTa.Text = "Quản lý và theo dõi thi đua\r\nnhanh chóng, chính xác";
            lblMoTa.TextAlign = ContentAlignment.TopCenter;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(860, 510);
            Controls.Add(panelDangNhap);
            Controls.Add(TableLayoutPanel6);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 2, 3, 2);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Đăng nhập";
            panelDangNhap.ResumeLayout(false);
            panelDangNhap.PerformLayout();
            TableLayoutPanel6.ResumeLayout(false);
            TableLayoutPanel6.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)PictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private ToolTip toolTip1;
        private Panel panelDangNhap;
        internal TableLayoutPanel TableLayoutPanel6;
        internal PictureBox PictureBox1;
        private Label lblChaoMung;
        private Label lblMoTa;
        private Label label3_HienThiTenPhanMem;
        private Label lblPhuDe;
        private Label label1;
        private Krypton.Toolkit.KryptonTextBox text_TenDangNhap;
        private Label label2;
        private Krypton.Toolkit.KryptonTextBox text_MatKhau;
        internal CheckBox Check_HienMatKhau;
        internal LinkLabel LinkLabel_QuenMatKhau;
        private Krypton.Toolkit.KryptonButton btn_DangNhap;
        private Krypton.Toolkit.KryptonButton btn_Thoat;
        internal LinkLabel LinkLabel1_DangKyTaiKhoanMoi;
        private Label label1_ThongBaoPhienBan;
        private Label label1_PhienBanPhanMem;
    }
}