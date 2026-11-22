//namespace PhanMemThiDua2026
namespace PhanMemThiDua2026
{
    partial class Form39_ThongTinNguoiDung
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form39_ThongTinNguoiDung));
            cardPanel = new Panel();
            pnlHeader = new Panel();
            pictureBox2_AnhDaiDienAdmin = new PictureBox();
            lblTitle = new Label();
            lblSubtitle = new Label();
            label9 = new Label();
            textBox_TenTaiKhoan = new Krypton.Toolkit.KryptonTextBox();
            label13 = new Label();
            textBox_TenUserWindows = new Krypton.Toolkit.KryptonTextBox();
            label12 = new Label();
            textBox_TenMayTinh = new Krypton.Toolkit.KryptonTextBox();
            label10 = new Label();
            textBox_ThoiGianDangNhap = new Krypton.Toolkit.KryptonTextBox();
            label15 = new Label();
            textbox_SeriaMay = new Krypton.Toolkit.KryptonTextBox();
            pnlActions = new FlowLayoutPanel();
            kryptonButton1_DongFrom = new Krypton.Toolkit.KryptonButton();
            toolTip1 = new ToolTip(components);
            cardPanel.SuspendLayout();
            pnlHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2_AnhDaiDienAdmin).BeginInit();
            pnlActions.SuspendLayout();
            SuspendLayout();
            // 
            // cardPanel
            // 
            cardPanel.BackColor = Color.FromArgb(30, 41, 59);
            cardPanel.Controls.Add(pnlHeader);
            cardPanel.Controls.Add(label9);
            cardPanel.Controls.Add(textBox_TenTaiKhoan);
            cardPanel.Controls.Add(label13);
            cardPanel.Controls.Add(textBox_TenUserWindows);
            cardPanel.Controls.Add(label12);
            cardPanel.Controls.Add(textBox_TenMayTinh);
            cardPanel.Controls.Add(label10);
            cardPanel.Controls.Add(textBox_ThoiGianDangNhap);
            cardPanel.Controls.Add(label15);
            cardPanel.Controls.Add(textbox_SeriaMay);
            cardPanel.Controls.Add(pnlActions);
            cardPanel.Dock = DockStyle.Fill;
            cardPanel.Location = new Point(0, 0);
            cardPanel.Name = "cardPanel";
            cardPanel.Size = new Size(480, 540);
            cardPanel.TabIndex = 0;
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.Transparent;
            pnlHeader.Controls.Add(pictureBox2_AnhDaiDienAdmin);
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Location = new Point(32, 26);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(416, 72);
            pnlHeader.TabIndex = 0;
            // 
            // pictureBox2_AnhDaiDienAdmin
            // 
            pictureBox2_AnhDaiDienAdmin.BackColor = Color.Transparent;
            pictureBox2_AnhDaiDienAdmin.Image = (Image)resources.GetObject("pictureBox2_AnhDaiDienAdmin.Image");
            pictureBox2_AnhDaiDienAdmin.Location = new Point(0, 4);
            pictureBox2_AnhDaiDienAdmin.Name = "pictureBox2_AnhDaiDienAdmin";
            pictureBox2_AnhDaiDienAdmin.Size = new Size(64, 64);
            pictureBox2_AnhDaiDienAdmin.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox2_AnhDaiDienAdmin.TabIndex = 0;
            pictureBox2_AnhDaiDienAdmin.TabStop = false;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.BackColor = Color.Transparent;
            lblTitle.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle.ForeColor = Color.FromArgb(241, 245, 249);
            lblTitle.Location = new Point(76, 12);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(226, 25);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Thông tin người sử dụng";
            // 
            // lblSubtitle
            // 
            lblSubtitle.AutoSize = true;
            lblSubtitle.BackColor = Color.Transparent;
            lblSubtitle.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSubtitle.ForeColor = Color.FromArgb(148, 163, 184);
            lblSubtitle.Location = new Point(78, 42);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(210, 15);
            lblSubtitle.TabIndex = 1;
            lblSubtitle.Text = "Chi tiết phiên đăng nhập hiện tại";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.BackColor = Color.Transparent;
            label9.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label9.ForeColor = Color.FromArgb(203, 213, 225);
            label9.Location = new Point(32, 116);
            label9.Name = "label9";
            label9.Size = new Size(65, 17);
            label9.TabIndex = 1;
            label9.Text = "Tài khoản";
            // 
            // textBox_TenTaiKhoan
            // 
            ConfigDarkTextBox(textBox_TenTaiKhoan, 32, 138, "textBox_TenTaiKhoan", 2);
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.BackColor = Color.Transparent;
            label13.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label13.ForeColor = Color.FromArgb(203, 213, 225);
            label13.Location = new Point(32, 182);
            label13.Name = "label13";
            label13.Size = new Size(88, 17);
            label13.TabIndex = 3;
            label13.Text = "User Windows";
            // 
            // textBox_TenUserWindows
            // 
            ConfigDarkTextBox(textBox_TenUserWindows, 32, 204, "textBox_TenUserWindows", 4);
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.BackColor = Color.Transparent;
            label12.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label12.ForeColor = Color.FromArgb(203, 213, 225);
            label12.Location = new Point(32, 248);
            label12.Name = "label12";
            label12.Size = new Size(61, 17);
            label12.TabIndex = 5;
            label12.Text = "Máy tính";
            // 
            // textBox_TenMayTinh
            // 
            ConfigDarkTextBox(textBox_TenMayTinh, 32, 270, "textBox_TenMayTinh", 6);
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.BackColor = Color.Transparent;
            label10.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label10.ForeColor = Color.FromArgb(203, 213, 225);
            label10.Location = new Point(32, 314);
            label10.Name = "label10";
            label10.Size = new Size(87, 17);
            label10.TabIndex = 7;
            label10.Text = "Đăng nhập lúc";
            // 
            // textBox_ThoiGianDangNhap
            // 
            ConfigDarkTextBox(textBox_ThoiGianDangNhap, 32, 336, "textBox_ThoiGianDangNhap", 8);
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.BackColor = Color.Transparent;
            label15.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label15.ForeColor = Color.FromArgb(203, 213, 225);
            label15.Location = new Point(32, 380);
            label15.Name = "label15";
            label15.Size = new Size(64, 17);
            label15.TabIndex = 9;
            label15.Text = "Seria máy";
            // 
            // textbox_SeriaMay
            // 
            ConfigDarkTextBox(textbox_SeriaMay, 32, 402, "textbox_SeriaMay", 10);
            // 
            // pnlActions
            // 
            pnlActions.BackColor = Color.Transparent;
            pnlActions.Controls.Add(kryptonButton1_DongFrom);
            pnlActions.FlowDirection = FlowDirection.RightToLeft;
            pnlActions.Location = new Point(32, 468);
            pnlActions.Margin = new Padding(0);
            pnlActions.Name = "pnlActions";
            pnlActions.Size = new Size(416, 50);
            pnlActions.TabIndex = 11;
            // 
            // kryptonButton1_DongFrom
            // 
            kryptonButton1_DongFrom.Cursor = Cursors.Hand;
            kryptonButton1_DongFrom.Location = new Point(266, 3);
            kryptonButton1_DongFrom.Margin = new Padding(6, 3, 0, 3);
            kryptonButton1_DongFrom.Name = "kryptonButton1_DongFrom";
            kryptonButton1_DongFrom.OverrideDefault.Back.Color1 = Color.FromArgb(79, 70, 229);
            kryptonButton1_DongFrom.OverrideDefault.Back.Color2 = Color.FromArgb(79, 70, 229);
            kryptonButton1_DongFrom.OverrideDefault.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            kryptonButton1_DongFrom.OverrideDefault.Border.Color1 = Color.FromArgb(79, 70, 229);
            kryptonButton1_DongFrom.OverrideDefault.Border.Color2 = Color.FromArgb(79, 70, 229);
            kryptonButton1_DongFrom.OverrideDefault.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom | Krypton.Toolkit.PaletteDrawBorders.Left | Krypton.Toolkit.PaletteDrawBorders.Right;
            kryptonButton1_DongFrom.OverrideDefault.Border.Rounding = 8F;
            kryptonButton1_DongFrom.OverrideDefault.Border.Width = 1;
            kryptonButton1_DongFrom.OverrideDefault.Content.ShortText.Color1 = Color.White;
            kryptonButton1_DongFrom.OverrideDefault.Content.ShortText.Color2 = Color.White;
            kryptonButton1_DongFrom.OverrideDefault.Content.ShortText.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            kryptonButton1_DongFrom.Size = new Size(150, 44);
            kryptonButton1_DongFrom.StateCommon.Back.Color1 = Color.FromArgb(79, 70, 229);
            kryptonButton1_DongFrom.StateCommon.Back.Color2 = Color.FromArgb(79, 70, 229);
            kryptonButton1_DongFrom.StateCommon.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            kryptonButton1_DongFrom.StateCommon.Border.Color1 = Color.FromArgb(79, 70, 229);
            kryptonButton1_DongFrom.StateCommon.Border.Color2 = Color.FromArgb(79, 70, 229);
            kryptonButton1_DongFrom.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom | Krypton.Toolkit.PaletteDrawBorders.Left | Krypton.Toolkit.PaletteDrawBorders.Right;
            kryptonButton1_DongFrom.StateCommon.Border.Rounding = 8F;
            kryptonButton1_DongFrom.StateCommon.Border.Width = 1;
            kryptonButton1_DongFrom.StateCommon.Content.ShortText.Color1 = Color.White;
            kryptonButton1_DongFrom.StateCommon.Content.ShortText.Color2 = Color.White;
            kryptonButton1_DongFrom.StateCommon.Content.ShortText.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            kryptonButton1_DongFrom.StatePressed.Back.Color1 = Color.FromArgb(49, 46, 129);
            kryptonButton1_DongFrom.StatePressed.Back.Color2 = Color.FromArgb(49, 46, 129);
            kryptonButton1_DongFrom.StatePressed.Border.Color1 = Color.FromArgb(49, 46, 129);
            kryptonButton1_DongFrom.StatePressed.Border.Color2 = Color.FromArgb(49, 46, 129);
            kryptonButton1_DongFrom.StateTracking.Back.Color1 = Color.FromArgb(67, 56, 202);
            kryptonButton1_DongFrom.StateTracking.Back.Color2 = Color.FromArgb(67, 56, 202);
            kryptonButton1_DongFrom.StateTracking.Border.Color1 = Color.FromArgb(67, 56, 202);
            kryptonButton1_DongFrom.StateTracking.Border.Color2 = Color.FromArgb(67, 56, 202);
            kryptonButton1_DongFrom.TabIndex = 0;
            kryptonButton1_DongFrom.Values.DropDownArrowColor = Color.Empty;
            kryptonButton1_DongFrom.Values.Image = (Image)resources.GetObject("kryptonButton1_DongFrom.Values.Image");
            kryptonButton1_DongFrom.Values.Text = "Đóng";
            kryptonButton1_DongFrom.Click += kryptonButton1_DongFrom_Click;
            // 
            // Form39_ThongTinNguoiDung
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 41, 59);
            ClientSize = new Size(480, 540);
            Controls.Add(cardPanel);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form39_ThongTinNguoiDung";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Thông tin người sử dụng";
            Load += Form39_ThongTinNguoiDung_Load;
            cardPanel.ResumeLayout(false);
            cardPanel.PerformLayout();
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2_AnhDaiDienAdmin).EndInit();
            pnlActions.ResumeLayout(false);
            ResumeLayout(false);
        }
        // Cấu hình chung cho ô nhập tối màu, bo góc (giống Form8)
        private static void ConfigDarkTextBox(Krypton.Toolkit.KryptonTextBox tb, int x, int y, string name, int tabIndex)
        {
            tb.Location = new Point(x, y);
            tb.Name = name;
            tb.ReadOnly = true;
            tb.Size = new Size(416, 42);
            tb.StateCommon.Back.Color1 = Color.FromArgb(15, 23, 42);
            tb.StateCommon.Border.Color1 = Color.FromArgb(51, 65, 85);
            tb.StateCommon.Border.Color2 = Color.FromArgb(51, 65, 85);
            tb.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom | Krypton.Toolkit.PaletteDrawBorders.Left | Krypton.Toolkit.PaletteDrawBorders.Right;
            tb.StateCommon.Border.Rounding = 10F;
            tb.StateCommon.Border.Width = 1;
            tb.StateCommon.Content.Color1 = Color.FromArgb(226, 232, 240);
            tb.StateCommon.Content.Font = new Font("Segoe UI", 10F);
            tb.StateCommon.Content.Padding = new Padding(10, 8, 10, 8);
            tb.TabIndex = tabIndex;
        }
        #endregion
        private Panel cardPanel;
        private Panel pnlHeader;
        private Label lblTitle;
        private Label lblSubtitle;
        private FlowLayoutPanel pnlActions;
        internal PictureBox pictureBox2_AnhDaiDienAdmin;
        internal Krypton.Toolkit.KryptonButton kryptonButton1_DongFrom;
        private Krypton.Toolkit.KryptonTextBox textbox_SeriaMay;
        private Krypton.Toolkit.KryptonTextBox textBox_TenMayTinh;
        private Krypton.Toolkit.KryptonTextBox textBox_TenTaiKhoan;
        private Krypton.Toolkit.KryptonTextBox textBox_ThoiGianDangNhap;
        private Krypton.Toolkit.KryptonTextBox textBox_TenUserWindows;
        private Label label9;
        private Label label10;
        private Label label12;
        private Label label13;
        private Label label15;
        private ToolTip toolTip1;
    }
}