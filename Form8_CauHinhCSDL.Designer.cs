namespace PhanMemThiDua2026
{
    partial class Form8_CauHinhCSDL
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form8_CauHinhCSDL));

            pnlBackground = new Panel();
            cardPanel = new Krypton.Toolkit.KryptonPanel();
            pnlHeader = new Panel();
            lblTitle = new Label();
            lblSubtitle = new Label();
            picLogo = new PictureBox();

            lblAdmin = new Label();
            Text_Admin = new Krypton.Toolkit.KryptonTextBox();
            lblPassword = new Label();
            Text_Password = new Krypton.Toolkit.KryptonTextBox();
            Chex_HienMatKhau = new CheckBox();

            pnlActions = new FlowLayoutPanel();
            btn_DangNhap = new Krypton.Toolkit.KryptonButton();
            btn_Thoat = new Krypton.Toolkit.KryptonButton();

            pnlBackground.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)cardPanel).BeginInit();
            cardPanel.SuspendLayout();
            pnlHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picLogo).BeginInit();
            pnlActions.SuspendLayout();
            SuspendLayout();

            // 
            // pnlBackground (Khung nền giả lập hiệu ứng Web App Container)
            // 
            pnlBackground.BackColor = Color.FromArgb(241, 245, 249); // Slate-100
            pnlBackground.Controls.Add(cardPanel);
            pnlBackground.Dock = DockStyle.Fill;
            pnlBackground.Location = new Point(0, 0);
            pnlBackground.Name = "pnlBackground";
            pnlBackground.Padding = new Padding(24);
            pnlBackground.Size = new Size(480, 430);
            pnlBackground.TabIndex = 0;

            // 
            // cardPanel (Thẻ Card chứa Form chính)
            // 
            cardPanel.Controls.Add(pnlHeader);
            cardPanel.Controls.Add(lblAdmin);
            cardPanel.Controls.Add(Text_Admin);
            cardPanel.Controls.Add(lblPassword);
            cardPanel.Controls.Add(Text_Password);
            cardPanel.Controls.Add(Chex_HienMatKhau);
            cardPanel.Controls.Add(pnlActions);
            cardPanel.Dock = DockStyle.Fill;
            cardPanel.Location = new Point(24, 24);
            cardPanel.Name = "cardPanel";
            cardPanel.Size = new Size(432, 382);
            cardPanel.StateCommon.Color1 = Color.White;
            cardPanel.StateCommon.Color2 = Color.White;
            cardPanel.TabIndex = 0;

            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.Transparent;
            pnlHeader.Controls.Add(picLogo);
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Location = new Point(20, 16);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(392, 70);
            pnlHeader.TabIndex = 0;

            // 
            // picLogo
            // 
            picLogo.Image = (Image)resources.GetObject("PictureBox3.Image");
            picLogo.Location = new Point(4, 8);
            picLogo.Name = "picLogo";
            picLogo.Size = new Size(52, 52);
            picLogo.SizeMode = PictureBoxSizeMode.Zoom;
            picLogo.TabIndex = 0;
            picLogo.TabStop = false;

            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI Semibold", 13.8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle.ForeColor = Color.FromArgb(15, 23, 42); // Slate-900
            lblTitle.Location = new Point(66, 10);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(210, 25);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Cấu hình Cơ sở dữ liệu";

            // 
            // lblSubtitle
            // 
            lblSubtitle.AutoSize = true;
            lblSubtitle.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSubtitle.ForeColor = Color.FromArgb(100, 116, 139); // Slate-500
            lblSubtitle.Location = new Point(68, 38);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(254, 15);
            lblSubtitle.TabIndex = 1;
            lblSubtitle.Text = "Nhập tài khoản quản trị để xác thực kết nối";

            // 
                        // lblAdmin
            //
            lblAdmin.AutoSize = true;
            lblAdmin.BackColor = Color.Transparent;
            lblAdmin.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblAdmin.ForeColor = Color.FromArgb(30, 41, 59); // Slate-800
            lblAdmin.Location = new Point(20, 100);
            lblAdmin.Name = "lblAdmin";
            lblAdmin.Size = new Size(98, 17);
            lblAdmin.TabIndex = 1;
            lblAdmin.Text = "Tên đăng nhập";

            // 
            // Text_Admin
            // 
            Text_Admin.Location = new Point(20, 122);
            Text_Admin.Name = "Text_Admin";
            Text_Admin.Size = new Size(392, 38);
            Text_Admin.StateCommon.Back.Color1 = Color.FromArgb(248, 250, 252);
            Text_Admin.StateCommon.Border.Color1 = Color.FromArgb(203, 213, 225);
            Text_Admin.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            Text_Admin.StateCommon.Border.Rounding = 8F;
            Text_Admin.StateCommon.Border.Width = 1;
            Text_Admin.StateCommon.Content.Padding = new Padding(10, 8, 10, 8);
            Text_Admin.StateCommon.Content.Font = new Font("Segoe UI", 10F);
            Text_Admin.TabIndex = 2;

            // 
            // lblPassword
            // 
            lblPassword.AutoSize = true;
            lblPassword.BackColor = Color.Transparent;
            lblPassword.Font = new Font("Segoe UI Medium", 9.5F);
            lblPassword.ForeColor = Color.FromArgb(51, 65, 85);
            lblPassword.Location = new Point(20, 175);
            lblPassword.Name = "lblPassword";
            lblPassword.Size = new Size(64, 17);
            lblPassword.TabIndex = 3;
            lblPassword.Text = "Mật khẩu";

            // 
            // Text_Password
            // 
            Text_Password.Location = new Point(20, 197);
            Text_Password.Name = "Text_Password";
            Text_Password.PasswordChar = '●';
            Text_Password.Size = new Size(392, 38);
            Text_Password.StateCommon.Back.Color1 = Color.FromArgb(248, 250, 252);
            Text_Password.StateCommon.Border.Color1 = Color.FromArgb(203, 213, 225);
            Text_Password.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            Text_Password.StateCommon.Border.Rounding = 8F;
            Text_Password.StateCommon.Border.Width = 1;
            Text_Password.StateCommon.Content.Padding = new Padding(10, 8, 10, 8);
            Text_Password.StateCommon.Content.Font = new Font("Segoe UI", 10F);
            Text_Password.TabIndex = 4;

            // 
            // Chex_HienMatKhau
            // 
            Chex_HienMatKhau.AutoSize = true;
            Chex_HienMatKhau.BackColor = Color.Transparent;
            Chex_HienMatKhau.Cursor = Cursors.Hand;
            Chex_HienMatKhau.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Chex_HienMatKhau.ForeColor = Color.FromArgb(71, 85, 105);
            Chex_HienMatKhau.Location = new Point(20, 246);
            Chex_HienMatKhau.Name = "Chex_HienMatKhau";
            Chex_HienMatKhau.Size = new Size(104, 19);
            Chex_HienMatKhau.TabIndex = 5;
            Chex_HienMatKhau.Text = "Hiện mật khẩu";
            Chex_HienMatKhau.UseVisualStyleBackColor = false;
            Chex_HienMatKhau.CheckedChanged += Chex_HienMatKhau_CheckedChanged;

            // 
            // pnlActions
            // 
            pnlActions.BackColor = Color.Transparent;
            pnlActions.Controls.Add(btn_DangNhap);
            pnlActions.Controls.Add(btn_Thoat);
            pnlActions.FlowDirection = FlowDirection.RightToLeft;
            pnlActions.Location = new Point(20, 285);
            pnlActions.Name = "pnlActions";
            pnlActions.Size = new Size(392, 50);
            pnlActions.TabIndex = 6;

// 
btn_DangNhap.Cursor = Cursors.Hand;
            btn_DangNhap.Location = new Point(262, 3);
            btn_DangNhap.Margin = new Padding(3, 2, 3, 2);
            btn_DangNhap.Name = "btn_DangNhap";
            btn_DangNhap.Size = new Size(127, 40);

            btn_DangNhap.OverrideDefault.Back.Color1 = Color.FromArgb(79, 70, 229);
            btn_DangNhap.OverrideDefault.Back.Color2 = Color.FromArgb(79, 70, 229);
            btn_DangNhap.OverrideDefault.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn_DangNhap.OverrideDefault.Border.Color1 = Color.FromArgb(79, 70, 229);
            btn_DangNhap.OverrideDefault.Border.Color2 = Color.FromArgb(79, 70, 229);
            btn_DangNhap.OverrideDefault.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            btn_DangNhap.OverrideDefault.Border.Rounding = 8F;
            btn_DangNhap.OverrideDefault.Border.Width = 1;
            btn_DangNhap.OverrideDefault.Content.ShortText.Color1 = Color.White;
            btn_DangNhap.OverrideDefault.Content.ShortText.Color2 = Color.White;
            btn_DangNhap.OverrideDefault.Content.ShortText.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);

            btn_DangNhap.StateCommon.Back.Color1 = Color.FromArgb(79, 70, 229);
            btn_DangNhap.StateCommon.Back.Color2 = Color.FromArgb(79, 70, 229);
            btn_DangNhap.StateCommon.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn_DangNhap.StateCommon.Border.Color1 = Color.FromArgb(79, 70, 229);
            btn_DangNhap.StateCommon.Border.Color2 = Color.FromArgb(79, 70, 229);
            btn_DangNhap.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            btn_DangNhap.StateCommon.Border.Rounding = 8F;
            btn_DangNhap.StateCommon.Border.Width = 1;
            btn_DangNhap.StateCommon.Content.ShortText.Color1 = Color.White;
            btn_DangNhap.StateCommon.Content.ShortText.Color2 = Color.White;
            btn_DangNhap.StateCommon.Content.ShortText.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);

            btn_DangNhap.StateTracking.Back.Color1 = Color.FromArgb(67, 56, 202);
            btn_DangNhap.StateTracking.Back.Color2 = Color.FromArgb(67, 56, 202);
            btn_DangNhap.StateTracking.Border.Color1 = Color.FromArgb(67, 56, 202);
            btn_DangNhap.StateTracking.Border.Color2 = Color.FromArgb(67, 56, 202);

            btn_DangNhap.StatePressed.Back.Color1 = Color.FromArgb(55, 48, 163);
            btn_DangNhap.StatePressed.Back.Color2 = Color.FromArgb(55, 48, 163);
            btn_DangNhap.StatePressed.Border.Color1 = Color.FromArgb(55, 48, 163);
            btn_DangNhap.StatePressed.Border.Color2 = Color.FromArgb(55, 48, 163);

            btn_DangNhap.TabIndex = 0;
            btn_DangNhap.Values.DropDownArrowColor = Color.Empty;
            btn_DangNhap.Values.Image = (Image)resources.GetObject("btn_DangNhap.Values.Image");
            btn_DangNhap.Values.Text = "Xác thực";
            btn_DangNhap.Click += btn_DangNhap_Click;

            // 
            // btn_Thoat
            // 
            btn_Thoat.Cursor = Cursors.Hand;
            btn_Thoat.Location = new Point(156, 3);
            btn_Thoat.Margin = new Padding(3, 2, 3, 2);
            btn_Thoat.Name = "btn_Thoat";
            btn_Thoat.Size = new Size(100, 40);

            btn_Thoat.OverrideDefault.Back.Color1 = Color.White;
            btn_Thoat.OverrideDefault.Back.Color2 = Color.White;
            btn_Thoat.OverrideDefault.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn_Thoat.OverrideDefault.Border.Color1 = Color.FromArgb(209, 213, 219);
            btn_Thoat.OverrideDefault.Border.Color2 = Color.FromArgb(209, 213, 219);
            btn_Thoat.OverrideDefault.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            btn_Thoat.OverrideDefault.Border.Rounding = 8F;
            btn_Thoat.OverrideDefault.Border.Width = 1;
            btn_Thoat.OverrideDefault.Content.ShortText.Color1 = Color.FromArgb(75, 85, 99);
            btn_Thoat.OverrideDefault.Content.ShortText.Color2 = Color.FromArgb(75, 85, 99);
            btn_Thoat.OverrideDefault.Content.ShortText.Font = new Font("Segoe UI", 9.5F);

            btn_Thoat.StateCommon.Back.Color1 = Color.White;
            btn_Thoat.StateCommon.Back.Color2 = Color.White;
            btn_Thoat.StateCommon.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn_Thoat.StateCommon.Border.Color1 = Color.FromArgb(209, 213, 219);
            btn_Thoat.StateCommon.Border.Color2 = Color.FromArgb(209, 213, 219);
            btn_Thoat.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            btn_Thoat.StateCommon.Border.Rounding = 8F;
            btn_Thoat.StateCommon.Border.Width = 1;
            btn_Thoat.StateCommon.Content.ShortText.Color1 = Color.FromArgb(75, 85, 99);
            btn_Thoat.StateCommon.Content.ShortText.Color2 = Color.FromArgb(75, 85, 99);
            btn_Thoat.StateCommon.Content.ShortText.Font = new Font("Segoe UI", 9.5F);

            btn_Thoat.StateTracking.Back.Color1 = Color.FromArgb(243, 244, 246);
            btn_Thoat.StateTracking.Back.Color2 = Color.FromArgb(243, 244, 246);
            btn_Thoat.StateTracking.Border.Color1 = Color.FromArgb(156, 163, 175);
            btn_Thoat.StateTracking.Border.Color2 = Color.FromArgb(156, 163, 175);

            btn_Thoat.StatePressed.Back.Color1 = Color.FromArgb(229, 231, 235);
            btn_Thoat.StatePressed.Back.Color2 = Color.FromArgb(229, 231, 235);
            btn_Thoat.StatePressed.Border.Color1 = Color.FromArgb(156, 163, 175);
            btn_Thoat.StatePressed.Border.Color2 = Color.FromArgb(156, 163, 175);

            btn_Thoat.TabIndex = 1;
            btn_Thoat.Values.DropDownArrowColor = Color.Empty;
            btn_Thoat.Values.Image = (Image)resources.GetObject("btn_Thoat.Values.Image");
            btn_Thoat.Values.Text = "Hủy";
            btn_Thoat.Click += btn_Thoat_Click;

            // Form8_CauHinhCSDL
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(480, 430);
            Controls.Add(pnlBackground);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form8_CauHinhCSDL";
            Text = "Cấu hình cơ sở dữ liệu";
            Load += Form8_Load;

            pnlBackground.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)cardPanel).EndInit();
            cardPanel.ResumeLayout(false);
            cardPanel.PerformLayout();
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picLogo).EndInit();
            pnlActions.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlBackground;
        private Krypton.Toolkit.KryptonPanel cardPanel;
        private Panel pnlHeader;
        private PictureBox picLogo;
        private Label lblTitle;
        private Label lblSubtitle;
        private Label lblAdmin;
        private Label lblPassword;
        private FlowLayoutPanel pnlActions;

        internal Krypton.Toolkit.KryptonButton btn_DangNhap;
        internal Krypton.Toolkit.KryptonButton btn_Thoat;
        internal CheckBox Chex_HienMatKhau;
        private Krypton.Toolkit.KryptonTextBox Text_Password;
        private Krypton.Toolkit.KryptonTextBox Text_Admin;
    }
}

//namespace PhanMemThiDua2026
//{
//    partial class Form8_CauHinhCSDL
//    {
//        /// <summary>
//        /// Required designer variable.
//        /// </summary>
//        private System.ComponentModel.IContainer components = null;
//        /// <summary>
//        /// Clean up any resources being used.
//        /// </summary>
//        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
//        protected override void Dispose(bool disposing)
//        {
//            if (disposing && (components != null))
//            {
//                components.Dispose();
//            }
//            base.Dispose(disposing);
//        }
//        #region Windows Form Designer generated code
//        /// <summary>
//        /// Required method for Designer support - do not modify
//        /// the contents of this method with the code editor.
//        /// </summary>
//        private void InitializeComponent()
//        {
//            components = new System.ComponentModel.Container();
//            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form8_CauHinhCSDL));
//            TableLayoutPanel1 = new TableLayoutPanel();
//            TableLayoutPanel3 = new TableLayoutPanel();
//            btn_DangNhap = new Krypton.Toolkit.KryptonButton();
//            btn_Thoat = new Krypton.Toolkit.KryptonButton();
//            TableLayoutPanel2 = new TableLayoutPanel();
//            Text_Password = new Krypton.Toolkit.KryptonTextBox();
//            label2 = new Label();
//            label1 = new Label();
//            Text_Admin = new Krypton.Toolkit.KryptonTextBox();
//            Chex_HienMatKhau = new CheckBox();
//            ImageList1 = new ImageList(components);
//            ImageList2 = new ImageList(components);
//            TableLayoutPanel5 = new TableLayoutPanel();
//            PictureBox3 = new PictureBox();
//            TableLayoutPanel1.SuspendLayout();
//            TableLayoutPanel3.SuspendLayout();
//            TableLayoutPanel2.SuspendLayout();
//            TableLayoutPanel5.SuspendLayout();
//            ((System.ComponentModel.ISupportInitialize)PictureBox3).BeginInit();
//            SuspendLayout();
//            // 
//            // TableLayoutPanel1
//            // 
//            TableLayoutPanel1.ColumnCount = 1;
//            TableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
//            TableLayoutPanel1.Controls.Add(TableLayoutPanel3, 0, 2);
//            TableLayoutPanel1.Controls.Add(TableLayoutPanel2, 0, 0);
//            TableLayoutPanel1.Controls.Add(Chex_HienMatKhau, 0, 1);
//            TableLayoutPanel1.Dock = DockStyle.Fill;
//            TableLayoutPanel1.Location = new Point(174, 2);
//            TableLayoutPanel1.Margin = new Padding(3, 2, 3, 2);
//            TableLayoutPanel1.Name = "TableLayoutPanel1";
//            TableLayoutPanel1.RowCount = 4;
//            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50.9708748F));
//            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 20.38835F));
//            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 24.7572823F));
//            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 3.883495F));
//            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
//            TableLayoutPanel1.Size = new Size(422, 237);
//            TableLayoutPanel1.TabIndex = 9;
//            // 
//            // TableLayoutPanel3
//            // 
//            TableLayoutPanel3.ColumnCount = 2;
//            TableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 49.4680862F));
//            TableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50.5319138F));
//            TableLayoutPanel3.Controls.Add(btn_DangNhap, 1, 0);
//            TableLayoutPanel3.Controls.Add(btn_Thoat, 0, 0);
//            TableLayoutPanel3.Dock = DockStyle.Fill;
//            TableLayoutPanel3.Location = new Point(3, 170);
//            TableLayoutPanel3.Margin = new Padding(3, 2, 3, 2);
//            TableLayoutPanel3.Name = "TableLayoutPanel3";
//            TableLayoutPanel3.RowCount = 1;
//            TableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
//            TableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
//            TableLayoutPanel3.Size = new Size(416, 54);
//            TableLayoutPanel3.TabIndex = 10;
//            // 
//            // btn_DangNhap
//            // 
//            btn_DangNhap.Anchor = AnchorStyles.None;
//            btn_DangNhap.Location = new Point(253, 11);
//            btn_DangNhap.Margin = new Padding(3, 2, 3, 2);
//            btn_DangNhap.Name = "btn_DangNhap";
//            btn_DangNhap.Size = new Size(114, 32);
//            btn_DangNhap.StateCommon.Border.Rounding = 4F;
//            btn_DangNhap.TabIndex = 0;
//            btn_DangNhap.Values.DropDownArrowColor = Color.Empty;
//            btn_DangNhap.Values.Image = (Image)resources.GetObject("btn_DangNhap.Values.Image");
//            btn_DangNhap.Values.Text = "Xác thực";
//            btn_DangNhap.Click += btn_DangNhap_Click;
//            // 
//            // btn_Thoat
//            // 
//            btn_Thoat.Anchor = AnchorStyles.None;
//            btn_Thoat.Location = new Point(45, 11);
//            btn_Thoat.Margin = new Padding(3, 2, 3, 2);
//            btn_Thoat.Name = "btn_Thoat";
//            btn_Thoat.Size = new Size(114, 32);
//            btn_Thoat.StateCommon.Border.Rounding = 4F;
//            btn_Thoat.TabIndex = 1;
//            btn_Thoat.Values.DropDownArrowColor = Color.Empty;
//            btn_Thoat.Values.Image = (Image)resources.GetObject("btn_Thoat.Values.Image");
//            btn_Thoat.Values.Text = "Thoát";
//            btn_Thoat.Click += btn_Thoat_Click;
//            // 
//            // TableLayoutPanel2
//            // 
//            TableLayoutPanel2.ColumnCount = 3;
//            TableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31.27854F));
//            TableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65.0685F));
//            TableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 3.652968F));
//            TableLayoutPanel2.Controls.Add(Text_Password, 1, 1);
//            TableLayoutPanel2.Controls.Add(label2, 0, 1);
//            TableLayoutPanel2.Controls.Add(label1, 0, 0);
//            TableLayoutPanel2.Controls.Add(Text_Admin, 1, 0);
//            TableLayoutPanel2.Dock = DockStyle.Fill;
//            TableLayoutPanel2.Location = new Point(3, 2);
//            TableLayoutPanel2.Margin = new Padding(3, 2, 3, 2);
//            TableLayoutPanel2.Name = "TableLayoutPanel2";
//            TableLayoutPanel2.RowCount = 2;
//            TableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
//            TableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
//            TableLayoutPanel2.Size = new Size(416, 116);
//            TableLayoutPanel2.TabIndex = 0;
//            // 
//            // Text_Password
//            // 
//            Text_Password.Anchor = AnchorStyles.Left | AnchorStyles.Right;
//            Text_Password.Location = new Point(133, 72);
//            Text_Password.Margin = new Padding(3, 2, 3, 2);
//            Text_Password.Name = "Text_Password";
//            Text_Password.Size = new Size(264, 29);
//            Text_Password.StateCommon.Border.Rounding = 8F;
//            Text_Password.StateCommon.Border.Width = 1;
//            Text_Password.TabIndex = 3;
//            // 
//            // label2
//            // 
//            label2.Anchor = AnchorStyles.Right;
//            label2.AutoSize = true;
//            label2.Font = new Font("Segoe UI", 10.8F, FontStyle.Italic);
//            label2.ForeColor = Color.Blue;
//            label2.Location = new Point(58, 77);
//            label2.Name = "label2";
//            label2.Size = new Size(69, 20);
//            label2.TabIndex = 25;
//            label2.Text = "Mật khẩu";
//            // 
//            // label1
//            // 
//            label1.Anchor = AnchorStyles.Right;
//            label1.AutoSize = true;
//            label1.Font = new Font("Segoe UI", 10.8F, FontStyle.Italic);
//            label1.ForeColor = Color.Blue;
//            label1.Location = new Point(23, 19);
//            label1.Name = "label1";
//            label1.Size = new Size(104, 20);
//            label1.TabIndex = 25;
//            label1.Text = "Tên đăng nhập";
//            // 
//            // Text_Admin
//            // 
//            Text_Admin.Anchor = AnchorStyles.Left | AnchorStyles.Right;
//            Text_Admin.Location = new Point(133, 14);
//            Text_Admin.Margin = new Padding(3, 2, 3, 2);
//            Text_Admin.Name = "Text_Admin";
//            Text_Admin.Size = new Size(264, 29);
//            Text_Admin.StateCommon.Border.Rounding = 8F;
//            Text_Admin.StateCommon.Border.Width = 1;
//            Text_Admin.TabIndex = 3;
//            // 
//            // Chex_HienMatKhau
//            // 
//            Chex_HienMatKhau.Anchor = AnchorStyles.None;
//            Chex_HienMatKhau.AutoSize = true;
//            Chex_HienMatKhau.Font = new Font("Segoe UI", 10.8F, FontStyle.Italic, GraphicsUnit.Point, 0);
//            Chex_HienMatKhau.ForeColor = Color.Red;
//            Chex_HienMatKhau.Location = new Point(149, 132);
//            Chex_HienMatKhau.Margin = new Padding(3, 2, 3, 2);
//            Chex_HienMatKhau.Name = "Chex_HienMatKhau";
//            Chex_HienMatKhau.Size = new Size(123, 24);
//            Chex_HienMatKhau.TabIndex = 0;
//            Chex_HienMatKhau.Text = "Hiện mật khẩu";
//            Chex_HienMatKhau.UseVisualStyleBackColor = true;
//            Chex_HienMatKhau.CheckedChanged += Chex_HienMatKhau_CheckedChanged;
//            // 
//            // ImageList1
//            // 
//            ImageList1.ColorDepth = ColorDepth.Depth32Bit;
//            ImageList1.ImageSize = new Size(16, 16);
//            ImageList1.TransparentColor = Color.Transparent;
//            // 
//            // ImageList2
//            // 
//            ImageList2.ColorDepth = ColorDepth.Depth32Bit;
//            ImageList2.ImageSize = new Size(16, 16);
//            ImageList2.TransparentColor = Color.Transparent;
//            // 
//            // TableLayoutPanel5
//            // 
//            TableLayoutPanel5.ColumnCount = 2;
//            TableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28.6912746F));
//            TableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 71.30872F));
//            TableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 16F));
//            TableLayoutPanel5.Controls.Add(TableLayoutPanel1, 1, 0);
//            TableLayoutPanel5.Controls.Add(PictureBox3, 0, 0);
//            TableLayoutPanel5.Dock = DockStyle.Fill;
//            TableLayoutPanel5.Location = new Point(0, 0);
//            TableLayoutPanel5.Margin = new Padding(3, 2, 3, 2);
//            TableLayoutPanel5.Name = "TableLayoutPanel5";
//            TableLayoutPanel5.RowCount = 1;
//            TableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
//            TableLayoutPanel5.Size = new Size(599, 241);
//            TableLayoutPanel5.TabIndex = 12;
//            // 
//            // PictureBox3
//            // 
//            PictureBox3.Anchor = AnchorStyles.Left | AnchorStyles.Right;
//            PictureBox3.Image = (Image)resources.GetObject("PictureBox3.Image");
//            PictureBox3.Location = new Point(3, 48);
//            PictureBox3.Margin = new Padding(3, 2, 3, 2);
//            PictureBox3.Name = "PictureBox3";
//            PictureBox3.Size = new Size(165, 145);
//            PictureBox3.SizeMode = PictureBoxSizeMode.StretchImage;
//            PictureBox3.TabIndex = 13;
//            PictureBox3.TabStop = false;
//            // 
//            // Form8_CauHinhCSDL
//            // 
//            AutoScaleDimensions = new SizeF(7F, 15F);
//            AutoScaleMode = AutoScaleMode.Font;
//            ClientSize = new Size(599, 241);
//            Controls.Add(TableLayoutPanel5);
//            Icon = (Icon)resources.GetObject("$this.Icon");
//            Margin = new Padding(3, 2, 3, 2);
//            Name = "Form8_CauHinhCSDL";
//            Text = "Cấu hình cơ sở dữ liệu";
//            Load += Form8_Load;
//            TableLayoutPanel1.ResumeLayout(false);
//            TableLayoutPanel1.PerformLayout();
//            TableLayoutPanel3.ResumeLayout(false);
//            TableLayoutPanel2.ResumeLayout(false);
//            TableLayoutPanel2.PerformLayout();
//            TableLayoutPanel5.ResumeLayout(false);
//            ((System.ComponentModel.ISupportInitialize)PictureBox3).EndInit();
//            ResumeLayout(false);
//        }
//        #endregion
//        internal TableLayoutPanel TableLayoutPanel1;
//        internal TableLayoutPanel TableLayoutPanel3;
//        internal Krypton.Toolkit.KryptonButton btn_DangNhap;
//        internal Krypton.Toolkit.KryptonButton btn_Thoat;
//        internal TableLayoutPanel TableLayoutPanel2;
//        internal CheckBox Chex_HienMatKhau;
//        internal ImageList ImageList1;
//        internal ImageList ImageList2;
//        internal TableLayoutPanel TableLayoutPanel5;
//        internal PictureBox PictureBox3;
//        private Krypton.Toolkit.KryptonTextBox Text_Password;
//        private Krypton.Toolkit.KryptonTextBox Text_Admin;
//        private Label label1;
//        private Label label2;
//    }
//}