namespace PhanMemThiDua2026
{
    partial class Form9_GoCaiDat
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form9_GoCaiDat));
            cardPanel = new Panel();
            pnlHeader = new Panel();
            PictureBox3 = new PictureBox();
            lblTitle = new Label();
            lblSubtitle = new Label();
            label1 = new Label();
            Text_Admin = new Krypton.Toolkit.KryptonTextBox();
            label2 = new Label();
            Text_Password = new Krypton.Toolkit.KryptonTextBox();
            Chex_HienMatKhau = new CheckBox();
            pnlActions = new FlowLayoutPanel();
            Btn_GoCaiDat = new Krypton.Toolkit.KryptonButton();
            kryptonButton_HuongDanToi = new Krypton.Toolkit.KryptonButton();
            Btn_Thoat = new Krypton.Toolkit.KryptonButton();
            toolTip1 = new ToolTip(components);
            cardPanel.SuspendLayout();
            pnlHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)PictureBox3).BeginInit();
            pnlActions.SuspendLayout();
            SuspendLayout();
            //
            // cardPanel  (phủ kín form, không còn vành ngoài)
            //
            cardPanel.BackColor = Color.FromArgb(30, 41, 59);
            cardPanel.Controls.Add(pnlHeader);
            cardPanel.Controls.Add(label1);
            cardPanel.Controls.Add(Text_Admin);
            cardPanel.Controls.Add(label2);
            cardPanel.Controls.Add(Text_Password);
            cardPanel.Controls.Add(Chex_HienMatKhau);
            cardPanel.Controls.Add(pnlActions);
            cardPanel.Dock = DockStyle.Fill;
            cardPanel.Location = new Point(0, 0);
            cardPanel.Name = "cardPanel";
            cardPanel.Size = new Size(460, 400);
            cardPanel.TabIndex = 0;
            //
            // pnlHeader
            //
            pnlHeader.BackColor = Color.Transparent;
            pnlHeader.Controls.Add(PictureBox3);
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Location = new Point(32, 26);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(396, 72);
            pnlHeader.TabIndex = 0;
            //
            // PictureBox3
            //
            PictureBox3.BackColor = Color.Transparent;
            PictureBox3.Image = (Image)resources.GetObject("PictureBox3.Image");
            PictureBox3.Location = new Point(0, 8);
            PictureBox3.Name = "PictureBox3";
            PictureBox3.Size = new Size(56, 56);
            PictureBox3.SizeMode = PictureBoxSizeMode.Zoom;
            PictureBox3.TabIndex = 0;
            PictureBox3.TabStop = false;
            //
            // lblTitle
            //
            lblTitle.AutoSize = true;
            lblTitle.BackColor = Color.Transparent;
            lblTitle.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle.ForeColor = Color.FromArgb(241, 245, 249);
            lblTitle.Location = new Point(68, 12);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(185, 25);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Gỡ cài đặt phần mềm";
            //
            // lblSubtitle
            //
            lblSubtitle.AutoSize = true;
            lblSubtitle.BackColor = Color.Transparent;
            lblSubtitle.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSubtitle.ForeColor = Color.FromArgb(148, 163, 184);
            lblSubtitle.Location = new Point(70, 42);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(240, 15);
            lblSubtitle.TabIndex = 1;
            lblSubtitle.Text = "Nhập mật khẩu Admin để xác nhận quyền gỡ";
            //
            // label1
            //
            label1.AutoSize = true;
            label1.BackColor = Color.Transparent;
            label1.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.FromArgb(203, 213, 225);
            label1.Location = new Point(32, 116);
            label1.Name = "label1";
            label1.Size = new Size(91, 17);
            label1.TabIndex = 1;
            label1.Text = "Tên tài khoản";
            //
            // Text_Admin
            //
            Text_Admin.Location = new Point(32, 138);
            Text_Admin.Name = "Text_Admin";
            Text_Admin.Size = new Size(396, 38);
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
            // label2
            //
            label2.AutoSize = true;
            label2.BackColor = Color.Transparent;
            label2.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.FromArgb(203, 213, 225);
            label2.Location = new Point(32, 194);
            label2.Name = "label2";
            label2.Size = new Size(64, 17);
            label2.TabIndex = 3;
            label2.Text = "Mật khẩu";
            //
            // Text_Password
            //
            Text_Password.Location = new Point(32, 216);
            Text_Password.Name = "Text_Password";
            Text_Password.Size = new Size(396, 38);
            Text_Password.StateCommon.Back.Color1 = Color.FromArgb(15, 23, 42);
            Text_Password.StateCommon.Border.Color1 = Color.FromArgb(51, 65, 85);
            Text_Password.StateCommon.Border.Color2 = Color.FromArgb(51, 65, 85);
            Text_Password.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom | Krypton.Toolkit.PaletteDrawBorders.Left | Krypton.Toolkit.PaletteDrawBorders.Right;
            Text_Password.StateCommon.Border.Rounding = 10F;
            Text_Password.StateCommon.Border.Width = 1;
            Text_Password.StateCommon.Content.Color1 = Color.FromArgb(226, 232, 240);
            Text_Password.StateCommon.Content.Font = new Font("Segoe UI", 10F);
            Text_Password.StateCommon.Content.Padding = new Padding(10, 8, 10, 8);
            Text_Password.TabIndex = 0;
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
            pnlActions.Controls.Add(Btn_GoCaiDat);
            pnlActions.Controls.Add(kryptonButton_HuongDanToi);
            pnlActions.Controls.Add(Btn_Thoat);
            pnlActions.FlowDirection = FlowDirection.RightToLeft;
            pnlActions.Location = new Point(32, 314);
            pnlActions.Margin = new Padding(0);
            pnlActions.Name = "pnlActions";
            pnlActions.Size = new Size(396, 50);
            pnlActions.TabIndex = 6;
            //
            // Btn_GoCaiDat  (nút chính - màu đỏ vì là thao tác gỡ bỏ)
            //
            Btn_GoCaiDat.Cursor = Cursors.Hand;
            Btn_GoCaiDat.Location = new Point(256, 3);
            Btn_GoCaiDat.Margin = new Padding(6, 3, 0, 3);
            Btn_GoCaiDat.Name = "Btn_GoCaiDat";
            Btn_GoCaiDat.OverrideDefault.Back.Color1 = Color.FromArgb(220, 38, 38);
            Btn_GoCaiDat.OverrideDefault.Back.Color2 = Color.FromArgb(220, 38, 38);
            Btn_GoCaiDat.OverrideDefault.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            Btn_GoCaiDat.OverrideDefault.Border.Color1 = Color.FromArgb(220, 38, 38);
            Btn_GoCaiDat.OverrideDefault.Border.Color2 = Color.FromArgb(220, 38, 38);
            Btn_GoCaiDat.OverrideDefault.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom | Krypton.Toolkit.PaletteDrawBorders.Left | Krypton.Toolkit.PaletteDrawBorders.Right;
            Btn_GoCaiDat.OverrideDefault.Border.Rounding = 8F;
            Btn_GoCaiDat.OverrideDefault.Border.Width = 1;
            Btn_GoCaiDat.OverrideDefault.Content.ShortText.Color1 = Color.White;
            Btn_GoCaiDat.OverrideDefault.Content.ShortText.Color2 = Color.White;
            Btn_GoCaiDat.OverrideDefault.Content.ShortText.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_GoCaiDat.Size = new Size(140, 44);
            Btn_GoCaiDat.StateCommon.Back.Color1 = Color.FromArgb(220, 38, 38);
            Btn_GoCaiDat.StateCommon.Back.Color2 = Color.FromArgb(220, 38, 38);
            Btn_GoCaiDat.StateCommon.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            Btn_GoCaiDat.StateCommon.Border.Color1 = Color.FromArgb(220, 38, 38);
            Btn_GoCaiDat.StateCommon.Border.Color2 = Color.FromArgb(220, 38, 38);
            Btn_GoCaiDat.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom | Krypton.Toolkit.PaletteDrawBorders.Left | Krypton.Toolkit.PaletteDrawBorders.Right;
            Btn_GoCaiDat.StateCommon.Border.Rounding = 8F;
            Btn_GoCaiDat.StateCommon.Border.Width = 1;
            Btn_GoCaiDat.StateCommon.Content.ShortText.Color1 = Color.White;
            Btn_GoCaiDat.StateCommon.Content.ShortText.Color2 = Color.White;
            Btn_GoCaiDat.StateCommon.Content.ShortText.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_GoCaiDat.StatePressed.Back.Color1 = Color.FromArgb(153, 27, 27);
            Btn_GoCaiDat.StatePressed.Back.Color2 = Color.FromArgb(153, 27, 27);
            Btn_GoCaiDat.StatePressed.Border.Color1 = Color.FromArgb(153, 27, 27);
            Btn_GoCaiDat.StatePressed.Border.Color2 = Color.FromArgb(153, 27, 27);
            Btn_GoCaiDat.StateTracking.Back.Color1 = Color.FromArgb(185, 28, 28);
            Btn_GoCaiDat.StateTracking.Back.Color2 = Color.FromArgb(185, 28, 28);
            Btn_GoCaiDat.StateTracking.Border.Color1 = Color.FromArgb(185, 28, 28);
            Btn_GoCaiDat.StateTracking.Border.Color2 = Color.FromArgb(185, 28, 28);
            Btn_GoCaiDat.TabIndex = 0;
            Btn_GoCaiDat.Values.DropDownArrowColor = Color.Empty;
            Btn_GoCaiDat.Values.Text = "Gỡ cài đặt";
            Btn_GoCaiDat.Click += Btn_GoCaiDat_Click;
            //
            // kryptonButton_HuongDanToi  (nút phụ)
            //
            kryptonButton_HuongDanToi.Cursor = Cursors.Hand;
            kryptonButton_HuongDanToi.Location = new Point(130, 3);
            kryptonButton_HuongDanToi.Margin = new Padding(6, 3, 6, 3);
            kryptonButton_HuongDanToi.Name = "kryptonButton_HuongDanToi";
            kryptonButton_HuongDanToi.OverrideDefault.Back.Color1 = Color.FromArgb(51, 65, 85);
            kryptonButton_HuongDanToi.OverrideDefault.Back.Color2 = Color.FromArgb(51, 65, 85);
            kryptonButton_HuongDanToi.OverrideDefault.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            kryptonButton_HuongDanToi.OverrideDefault.Border.Color1 = Color.FromArgb(71, 85, 105);
            kryptonButton_HuongDanToi.OverrideDefault.Border.Color2 = Color.FromArgb(71, 85, 105);
            kryptonButton_HuongDanToi.OverrideDefault.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom | Krypton.Toolkit.PaletteDrawBorders.Left | Krypton.Toolkit.PaletteDrawBorders.Right;
            kryptonButton_HuongDanToi.OverrideDefault.Border.Rounding = 8F;
            kryptonButton_HuongDanToi.OverrideDefault.Border.Width = 1;
            kryptonButton_HuongDanToi.OverrideDefault.Content.ShortText.Color1 = Color.FromArgb(226, 232, 240);
            kryptonButton_HuongDanToi.OverrideDefault.Content.ShortText.Color2 = Color.FromArgb(226, 232, 240);
            kryptonButton_HuongDanToi.OverrideDefault.Content.ShortText.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            kryptonButton_HuongDanToi.Size = new Size(120, 44);
            kryptonButton_HuongDanToi.StateCommon.Back.Color1 = Color.FromArgb(51, 65, 85);
            kryptonButton_HuongDanToi.StateCommon.Back.Color2 = Color.FromArgb(51, 65, 85);
            kryptonButton_HuongDanToi.StateCommon.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            kryptonButton_HuongDanToi.StateCommon.Border.Color1 = Color.FromArgb(71, 85, 105);
            kryptonButton_HuongDanToi.StateCommon.Border.Color2 = Color.FromArgb(71, 85, 105);
            kryptonButton_HuongDanToi.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom | Krypton.Toolkit.PaletteDrawBorders.Left | Krypton.Toolkit.PaletteDrawBorders.Right;
            kryptonButton_HuongDanToi.StateCommon.Border.Rounding = 8F;
            kryptonButton_HuongDanToi.StateCommon.Border.Width = 1;
            kryptonButton_HuongDanToi.StateCommon.Content.ShortText.Color1 = Color.FromArgb(226, 232, 240);
            kryptonButton_HuongDanToi.StateCommon.Content.ShortText.Color2 = Color.FromArgb(226, 232, 240);
            kryptonButton_HuongDanToi.StateCommon.Content.ShortText.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            kryptonButton_HuongDanToi.StatePressed.Back.Color1 = Color.FromArgb(30, 41, 59);
            kryptonButton_HuongDanToi.StatePressed.Back.Color2 = Color.FromArgb(30, 41, 59);
            kryptonButton_HuongDanToi.StatePressed.Border.Color1 = Color.FromArgb(51, 65, 85);
            kryptonButton_HuongDanToi.StatePressed.Border.Color2 = Color.FromArgb(51, 65, 85);
            kryptonButton_HuongDanToi.StateTracking.Back.Color1 = Color.FromArgb(71, 85, 105);
            kryptonButton_HuongDanToi.StateTracking.Back.Color2 = Color.FromArgb(71, 85, 105);
            kryptonButton_HuongDanToi.StateTracking.Border.Color1 = Color.FromArgb(100, 116, 139);
            kryptonButton_HuongDanToi.StateTracking.Border.Color2 = Color.FromArgb(100, 116, 139);
            kryptonButton_HuongDanToi.TabIndex = 2;
            kryptonButton_HuongDanToi.Values.DropDownArrowColor = Color.Empty;
            kryptonButton_HuongDanToi.Values.Text = "Hướng dẫn";
            kryptonButton_HuongDanToi.Click += kryptonButton_HuongDanToi_Click;
            //
            // Btn_Thoat  (nút phụ)
            //
            Btn_Thoat.Cursor = Cursors.Hand;
            Btn_Thoat.Location = new Point(34, 3);
            Btn_Thoat.Margin = new Padding(6, 3, 6, 3);
            Btn_Thoat.Name = "Btn_Thoat";
            Btn_Thoat.OverrideDefault.Back.Color1 = Color.FromArgb(51, 65, 85);
            Btn_Thoat.OverrideDefault.Back.Color2 = Color.FromArgb(51, 65, 85);
            Btn_Thoat.OverrideDefault.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            Btn_Thoat.OverrideDefault.Border.Color1 = Color.FromArgb(71, 85, 105);
            Btn_Thoat.OverrideDefault.Border.Color2 = Color.FromArgb(71, 85, 105);
            Btn_Thoat.OverrideDefault.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom | Krypton.Toolkit.PaletteDrawBorders.Left | Krypton.Toolkit.PaletteDrawBorders.Right;
            Btn_Thoat.OverrideDefault.Border.Rounding = 8F;
            Btn_Thoat.OverrideDefault.Border.Width = 1;
            Btn_Thoat.OverrideDefault.Content.ShortText.Color1 = Color.FromArgb(226, 232, 240);
            Btn_Thoat.OverrideDefault.Content.ShortText.Color2 = Color.FromArgb(226, 232, 240);
            Btn_Thoat.OverrideDefault.Content.ShortText.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_Thoat.Size = new Size(90, 44);
            Btn_Thoat.StateCommon.Back.Color1 = Color.FromArgb(51, 65, 85);
            Btn_Thoat.StateCommon.Back.Color2 = Color.FromArgb(51, 65, 85);
            Btn_Thoat.StateCommon.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            Btn_Thoat.StateCommon.Border.Color1 = Color.FromArgb(71, 85, 105);
            Btn_Thoat.StateCommon.Border.Color2 = Color.FromArgb(71, 85, 105);
            Btn_Thoat.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom | Krypton.Toolkit.PaletteDrawBorders.Left | Krypton.Toolkit.PaletteDrawBorders.Right;
            Btn_Thoat.StateCommon.Border.Rounding = 8F;
            Btn_Thoat.StateCommon.Border.Width = 1;
            Btn_Thoat.StateCommon.Content.ShortText.Color1 = Color.FromArgb(226, 232, 240);
            Btn_Thoat.StateCommon.Content.ShortText.Color2 = Color.FromArgb(226, 232, 240);
            Btn_Thoat.StateCommon.Content.ShortText.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_Thoat.StatePressed.Back.Color1 = Color.FromArgb(30, 41, 59);
            Btn_Thoat.StatePressed.Back.Color2 = Color.FromArgb(30, 41, 59);
            Btn_Thoat.StatePressed.Border.Color1 = Color.FromArgb(51, 65, 85);
            Btn_Thoat.StatePressed.Border.Color2 = Color.FromArgb(51, 65, 85);
            Btn_Thoat.StateTracking.Back.Color1 = Color.FromArgb(71, 85, 105);
            Btn_Thoat.StateTracking.Back.Color2 = Color.FromArgb(71, 85, 105);
            Btn_Thoat.StateTracking.Border.Color1 = Color.FromArgb(100, 116, 139);
            Btn_Thoat.StateTracking.Border.Color2 = Color.FromArgb(100, 116, 139);
            Btn_Thoat.TabIndex = 1;
            Btn_Thoat.Values.DropDownArrowColor = Color.Empty;
            Btn_Thoat.Values.Text = "Đóng";
            Btn_Thoat.Click += Btn_Thoat_Click;
            //
            // Form9_GoCaiDat
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 41, 59);
            ClientSize = new Size(460, 400);
            Controls.Add(cardPanel);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form9_GoCaiDat";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Gỡ cài đặt";
            Load += Form9_Load;
            cardPanel.ResumeLayout(false);
            cardPanel.PerformLayout();
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)PictureBox3).EndInit();
            pnlActions.ResumeLayout(false);
            ResumeLayout(false);
        }
        #endregion
        private Panel cardPanel;
        private Panel pnlHeader;
        private Label lblTitle;
        private Label lblSubtitle;
        private FlowLayoutPanel pnlActions;
        internal Krypton.Toolkit.KryptonButton Btn_GoCaiDat;
        internal Krypton.Toolkit.KryptonButton Btn_Thoat;
        internal Krypton.Toolkit.KryptonButton kryptonButton_HuongDanToi;
        internal Krypton.Toolkit.KryptonTextBox Text_Password;
        internal Krypton.Toolkit.KryptonTextBox Text_Admin;
        internal CheckBox Chex_HienMatKhau;
        internal PictureBox PictureBox3;
        private Label label1;
        private Label label2;
        private ToolTip toolTip1;
    }
}