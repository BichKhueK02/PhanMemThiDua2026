namespace PhanMemThiDua2026
{
    partial class Form8_CauHinhCSDL
    {
        private System.ComponentModel.IContainer components = null;
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form8_CauHinhCSDL));
            cardPanel = new Panel();
            pnlHeader = new Panel();
            picLogo = new PictureBox();
            lblTitle = new Label();
            lblSubtitle = new Label();
            lblAdmin = new Label();
            Text_Admin = new Krypton.Toolkit.KryptonTextBox();
            lblPassword = new Label();
            Text_Password = new Krypton.Toolkit.KryptonTextBox();
            Chex_HienMatKhau = new CheckBox();
            pnlActions = new FlowLayoutPanel();
            btn_DangNhap = new Krypton.Toolkit.KryptonButton();
            btn_Thoat = new Krypton.Toolkit.KryptonButton();
            cardPanel.SuspendLayout();
            pnlHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picLogo).BeginInit();
            pnlActions.SuspendLayout();
            SuspendLayout();
            // 
            // cardPanel
            // 
            cardPanel.BackColor = Color.FromArgb(30, 41, 59);
            cardPanel.Controls.Add(pnlHeader);
            cardPanel.Controls.Add(lblAdmin);
            cardPanel.Controls.Add(Text_Admin);
            cardPanel.Controls.Add(lblPassword);
            cardPanel.Controls.Add(Text_Password);
            cardPanel.Controls.Add(Chex_HienMatKhau);
            cardPanel.Controls.Add(pnlActions);
            cardPanel.Dock = DockStyle.Fill;
            cardPanel.Location = new Point(0, 0);
            cardPanel.Name = "cardPanel";
            cardPanel.Size = new Size(440, 400);
            cardPanel.TabIndex = 0;
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.Transparent;
            pnlHeader.Controls.Add(picLogo);
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Location = new Point(32, 26);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(376, 72);
            pnlHeader.TabIndex = 0;
            // 
            // picLogo
            // 
            picLogo.BackColor = Color.Transparent;
            picLogo.Image = (Image)resources.GetObject("picLogo.Image");
            picLogo.Location = new Point(0, 10);
            picLogo.Name = "picLogo";
            picLogo.Size = new Size(52, 52);
            picLogo.SizeMode = PictureBoxSizeMode.Zoom;
            picLogo.TabIndex = 0;
            picLogo.TabStop = false;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.BackColor = Color.Transparent;
            lblTitle.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle.ForeColor = Color.FromArgb(241, 245, 249);
            lblTitle.Location = new Point(64, 12);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(206, 25);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Cấu hình Cơ sở dữ liệu";
            // 
            // lblSubtitle
            // 
            lblSubtitle.AutoSize = true;
            lblSubtitle.BackColor = Color.Transparent;
            lblSubtitle.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSubtitle.ForeColor = Color.FromArgb(148, 163, 184);
            lblSubtitle.Location = new Point(66, 42);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(234, 15);
            lblSubtitle.TabIndex = 1;
            lblSubtitle.Text = "Nhập tài khoản quản trị để xác thực kết nối";
            // 
            // lblAdmin
            // 
            lblAdmin.AutoSize = true;
            lblAdmin.BackColor = Color.Transparent;
            lblAdmin.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblAdmin.ForeColor = Color.FromArgb(203, 213, 225);
            lblAdmin.Location = new Point(32, 116);
            lblAdmin.Name = "lblAdmin";
            lblAdmin.Size = new Size(99, 17);
            lblAdmin.TabIndex = 1;
            lblAdmin.Text = "Tên đăng nhập";
            // 
            // Text_Admin
            // 
            Text_Admin.Location = new Point(32, 138);
            Text_Admin.Name = "Text_Admin";
            Text_Admin.Size = new Size(376, 42);
            Text_Admin.StateCommon.Back.Color1 = Color.FromArgb(15, 23, 42);
            Text_Admin.StateCommon.Border.Color1 = Color.FromArgb(51, 65, 85);
            Text_Admin.StateCommon.Border.Color2 = Color.FromArgb(51, 65, 85);
            Text_Admin.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom | Krypton.Toolkit.PaletteDrawBorders.Left | Krypton.Toolkit.PaletteDrawBorders.Right;
            Text_Admin.StateCommon.Border.Rounding = 10F;
            Text_Admin.StateCommon.Border.Width = 1;
            Text_Admin.StateCommon.Content.Color1 = Color.FromArgb(226, 232, 240);
            Text_Admin.StateCommon.Content.Font = new Font("Segoe UI", 10F);
            Text_Admin.StateCommon.Content.Padding = new Padding(10, 8, 10, 8);
            Text_Admin.TabIndex = 2;
            // 
            // lblPassword
            // 
            lblPassword.AutoSize = true;
            lblPassword.BackColor = Color.Transparent;
            lblPassword.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblPassword.ForeColor = Color.FromArgb(203, 213, 225);
            lblPassword.Location = new Point(32, 194);
            lblPassword.Name = "lblPassword";
            lblPassword.Size = new Size(66, 17);
            lblPassword.TabIndex = 3;
            lblPassword.Text = "Mật khẩu";
            // 
            // Text_Password
            // 
            Text_Password.Location = new Point(32, 216);
            Text_Password.Name = "Text_Password";
            Text_Password.PasswordChar = '●';
            Text_Password.Size = new Size(376, 42);
            Text_Password.StateCommon.Back.Color1 = Color.FromArgb(15, 23, 42);
            Text_Password.StateCommon.Border.Color1 = Color.FromArgb(51, 65, 85);
            Text_Password.StateCommon.Border.Color2 = Color.FromArgb(51, 65, 85);
            Text_Password.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom | Krypton.Toolkit.PaletteDrawBorders.Left | Krypton.Toolkit.PaletteDrawBorders.Right;
            Text_Password.StateCommon.Border.Rounding = 10F;
            Text_Password.StateCommon.Border.Width = 1;
            Text_Password.StateCommon.Content.Color1 = Color.FromArgb(226, 232, 240);
            Text_Password.StateCommon.Content.Font = new Font("Segoe UI", 10F);
            Text_Password.StateCommon.Content.Padding = new Padding(10, 8, 10, 8);
            Text_Password.TabIndex = 4;
            // 
            // Chex_HienMatKhau
            // 
            Chex_HienMatKhau.AutoSize = true;
            Chex_HienMatKhau.BackColor = Color.Transparent;
            Chex_HienMatKhau.Cursor = Cursors.Hand;
            Chex_HienMatKhau.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Chex_HienMatKhau.ForeColor = Color.FromArgb(148, 163, 184);
            Chex_HienMatKhau.Location = new Point(32, 268);
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
            pnlActions.Location = new Point(32, 314);
            pnlActions.Margin = new Padding(0);
            pnlActions.Name = "pnlActions";
            pnlActions.Size = new Size(376, 50);
            pnlActions.TabIndex = 6;
            // 
            // btn_DangNhap
            // 
            btn_DangNhap.Cursor = Cursors.Hand;
            btn_DangNhap.Location = new Point(226, 3);
            btn_DangNhap.Margin = new Padding(6, 3, 0, 3);
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
            btn_DangNhap.Size = new Size(150, 44);
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
            btn_DangNhap.TabIndex = 0;
            btn_DangNhap.Values.DropDownArrowColor = Color.Empty;
            btn_DangNhap.Values.Text = "Xác thực";
            btn_DangNhap.Click += btn_DangNhap_Click;
            // 
            // btn_Thoat
            // 
            btn_Thoat.Anchor = AnchorStyles.Left;
            btn_Thoat.Cursor = Cursors.Hand;
            btn_Thoat.Location = new Point(114, 3);
            btn_Thoat.Margin = new Padding(6, 3, 6, 3);
            btn_Thoat.Name = "btn_Thoat";
            btn_Thoat.OverrideDefault.Back.Color1 = Color.FromArgb(51, 65, 85);
            btn_Thoat.OverrideDefault.Back.Color2 = Color.FromArgb(51, 65, 85);
            btn_Thoat.OverrideDefault.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn_Thoat.OverrideDefault.Border.Color1 = Color.FromArgb(71, 85, 105);
            btn_Thoat.OverrideDefault.Border.Color2 = Color.FromArgb(71, 85, 105);
            btn_Thoat.OverrideDefault.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom | Krypton.Toolkit.PaletteDrawBorders.Left | Krypton.Toolkit.PaletteDrawBorders.Right;
            btn_Thoat.OverrideDefault.Border.Rounding = 8F;
            btn_Thoat.OverrideDefault.Border.Width = 1;
            btn_Thoat.OverrideDefault.Content.ShortText.Color1 = Color.FromArgb(226, 232, 240);
            btn_Thoat.OverrideDefault.Content.ShortText.Color2 = Color.FromArgb(226, 232, 240);
            btn_Thoat.OverrideDefault.Content.ShortText.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btn_Thoat.Size = new Size(100, 44);
            btn_Thoat.StateCommon.Back.Color1 = Color.FromArgb(51, 65, 85);
            btn_Thoat.StateCommon.Back.Color2 = Color.FromArgb(51, 65, 85);
            btn_Thoat.StateCommon.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn_Thoat.StateCommon.Border.Color1 = Color.FromArgb(71, 85, 105);
            btn_Thoat.StateCommon.Border.Color2 = Color.FromArgb(71, 85, 105);
            btn_Thoat.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom | Krypton.Toolkit.PaletteDrawBorders.Left | Krypton.Toolkit.PaletteDrawBorders.Right;
            btn_Thoat.StateCommon.Border.Rounding = 8F;
            btn_Thoat.StateCommon.Border.Width = 1;
            btn_Thoat.StateCommon.Content.ShortText.Color1 = Color.FromArgb(226, 232, 240);
            btn_Thoat.StateCommon.Content.ShortText.Color2 = Color.FromArgb(226, 232, 240);
            btn_Thoat.StateCommon.Content.ShortText.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btn_Thoat.StatePressed.Back.Color1 = Color.FromArgb(30, 41, 59);
            btn_Thoat.StatePressed.Back.Color2 = Color.FromArgb(30, 41, 59);
            btn_Thoat.StatePressed.Border.Color1 = Color.FromArgb(51, 65, 85);
            btn_Thoat.StatePressed.Border.Color2 = Color.FromArgb(51, 65, 85);
            btn_Thoat.StateTracking.Back.Color1 = Color.FromArgb(71, 85, 105);
            btn_Thoat.StateTracking.Back.Color2 = Color.FromArgb(71, 85, 105);
            btn_Thoat.StateTracking.Border.Color1 = Color.FromArgb(100, 116, 139);
            btn_Thoat.StateTracking.Border.Color2 = Color.FromArgb(100, 116, 139);
            btn_Thoat.TabIndex = 1;
            btn_Thoat.Values.DropDownArrowColor = Color.Empty;
            btn_Thoat.Values.Text = "Hủy";
            btn_Thoat.Click += btn_Thoat_Click;
            // 
            // Form8_CauHinhCSDL
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 41, 59);
            ClientSize = new Size(440, 400);
            Controls.Add(cardPanel);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form8_CauHinhCSDL";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Cấu hình cơ sở dữ liệu";
            Load += Form8_Load;
            cardPanel.ResumeLayout(false);
            cardPanel.PerformLayout();
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picLogo).EndInit();
            pnlActions.ResumeLayout(false);
            ResumeLayout(false);
        }
        #endregion
        private Panel cardPanel;
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