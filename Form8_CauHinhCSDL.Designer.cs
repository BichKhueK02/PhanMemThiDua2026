namespace PhanMemThiDua2026
{
    partial class Form8_CauHinhCSDL
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form8_CauHinhCSDL));
            TableLayoutPanel1 = new TableLayoutPanel();
            TableLayoutPanel3 = new TableLayoutPanel();
            btn_DangNhap = new Krypton.Toolkit.KryptonButton();
            btn_Thoat = new Krypton.Toolkit.KryptonButton();
            TableLayoutPanel2 = new TableLayoutPanel();
            Text_Password = new Krypton.Toolkit.KryptonTextBox();
            label2 = new Label();
            label1 = new Label();
            Text_Admin = new Krypton.Toolkit.KryptonTextBox();
            Chex_HienMatKhau = new CheckBox();
            ImageList1 = new ImageList(components);
            ImageList2 = new ImageList(components);
            TableLayoutPanel5 = new TableLayoutPanel();
            PictureBox3 = new PictureBox();
            TableLayoutPanel1.SuspendLayout();
            TableLayoutPanel3.SuspendLayout();
            TableLayoutPanel2.SuspendLayout();
            TableLayoutPanel5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)PictureBox3).BeginInit();
            SuspendLayout();
            // 
            // TableLayoutPanel1
            // 
            TableLayoutPanel1.ColumnCount = 1;
            TableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            TableLayoutPanel1.Controls.Add(TableLayoutPanel3, 0, 2);
            TableLayoutPanel1.Controls.Add(TableLayoutPanel2, 0, 0);
            TableLayoutPanel1.Controls.Add(Chex_HienMatKhau, 0, 1);
            TableLayoutPanel1.Dock = DockStyle.Fill;
            TableLayoutPanel1.Location = new Point(174, 2);
            TableLayoutPanel1.Margin = new Padding(3, 2, 3, 2);
            TableLayoutPanel1.Name = "TableLayoutPanel1";
            TableLayoutPanel1.RowCount = 4;
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50.9708748F));
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 20.38835F));
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 24.7572823F));
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 3.883495F));
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            TableLayoutPanel1.Size = new Size(419, 206);
            TableLayoutPanel1.TabIndex = 9;
            // 
            // TableLayoutPanel3
            // 
            TableLayoutPanel3.ColumnCount = 2;
            TableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 49.4680862F));
            TableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50.5319138F));
            TableLayoutPanel3.Controls.Add(btn_DangNhap, 1, 0);
            TableLayoutPanel3.Controls.Add(btn_Thoat, 0, 0);
            TableLayoutPanel3.Dock = DockStyle.Fill;
            TableLayoutPanel3.Location = new Point(3, 149);
            TableLayoutPanel3.Margin = new Padding(3, 2, 3, 2);
            TableLayoutPanel3.Name = "TableLayoutPanel3";
            TableLayoutPanel3.RowCount = 1;
            TableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            TableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            TableLayoutPanel3.Size = new Size(413, 47);
            TableLayoutPanel3.TabIndex = 10;
            // 
            // btn_DangNhap
            // 
            btn_DangNhap.Anchor = AnchorStyles.None;
            btn_DangNhap.Location = new Point(251, 7);
            btn_DangNhap.Margin = new Padding(3, 2, 3, 2);
            btn_DangNhap.Name = "btn_DangNhap";
            btn_DangNhap.Size = new Size(114, 32);
            btn_DangNhap.StateCommon.Border.Rounding = 4F;
            btn_DangNhap.TabIndex = 0;
            btn_DangNhap.Values.DropDownArrowColor = Color.Empty;
            btn_DangNhap.Values.Image = (Image)resources.GetObject("btn_DangNhap.Values.Image");
            btn_DangNhap.Values.Text = "Xác thực";
            btn_DangNhap.Click += btn_DangNhap_Click;
            // 
            // btn_Thoat
            // 
            btn_Thoat.Anchor = AnchorStyles.None;
            btn_Thoat.Location = new Point(45, 7);
            btn_Thoat.Margin = new Padding(3, 2, 3, 2);
            btn_Thoat.Name = "btn_Thoat";
            btn_Thoat.Size = new Size(114, 32);
            btn_Thoat.StateCommon.Border.Rounding = 4F;
            btn_Thoat.TabIndex = 1;
            btn_Thoat.Values.DropDownArrowColor = Color.Empty;
            btn_Thoat.Values.Image = (Image)resources.GetObject("btn_Thoat.Values.Image");
            btn_Thoat.Values.Text = "Thoát";
            btn_Thoat.Click += btn_Thoat_Click;
            // 
            // TableLayoutPanel2
            // 
            TableLayoutPanel2.ColumnCount = 3;
            TableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31.27854F));
            TableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65.0685F));
            TableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 3.652968F));
            TableLayoutPanel2.Controls.Add(Text_Password, 1, 1);
            TableLayoutPanel2.Controls.Add(label2, 0, 1);
            TableLayoutPanel2.Controls.Add(label1, 0, 0);
            TableLayoutPanel2.Controls.Add(Text_Admin, 1, 0);
            TableLayoutPanel2.Dock = DockStyle.Fill;
            TableLayoutPanel2.Location = new Point(3, 2);
            TableLayoutPanel2.Margin = new Padding(3, 2, 3, 2);
            TableLayoutPanel2.Name = "TableLayoutPanel2";
            TableLayoutPanel2.RowCount = 2;
            TableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            TableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            TableLayoutPanel2.Size = new Size(413, 101);
            TableLayoutPanel2.TabIndex = 0;
            // 
            // Text_Password
            // 
            Text_Password.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            Text_Password.Location = new Point(132, 61);
            Text_Password.Margin = new Padding(3, 2, 3, 2);
            Text_Password.Name = "Text_Password";
            Text_Password.Size = new Size(262, 29);
            Text_Password.StateCommon.Border.Rounding = 8F;
            Text_Password.StateCommon.Border.Width = 1;
            Text_Password.TabIndex = 3;
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Right;
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 10.8F, FontStyle.Italic);
            label2.ForeColor = Color.Blue;
            label2.Location = new Point(57, 65);
            label2.Name = "label2";
            label2.Size = new Size(69, 20);
            label2.TabIndex = 25;
            label2.Text = "Mật khẩu";
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Right;
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 10.8F, FontStyle.Italic);
            label1.ForeColor = Color.Blue;
            label1.Location = new Point(22, 15);
            label1.Name = "label1";
            label1.Size = new Size(104, 20);
            label1.TabIndex = 25;
            label1.Text = "Tên đăng nhập";
            // 
            // Text_Admin
            // 
            Text_Admin.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            Text_Admin.Location = new Point(132, 10);
            Text_Admin.Margin = new Padding(3, 2, 3, 2);
            Text_Admin.Name = "Text_Admin";
            Text_Admin.Size = new Size(262, 29);
            Text_Admin.StateCommon.Border.Rounding = 8F;
            Text_Admin.StateCommon.Border.Width = 1;
            Text_Admin.TabIndex = 3;
            // 
            // Chex_HienMatKhau
            // 
            Chex_HienMatKhau.Anchor = AnchorStyles.None;
            Chex_HienMatKhau.AutoSize = true;
            Chex_HienMatKhau.Font = new Font("Segoe UI", 10.8F, FontStyle.Italic, GraphicsUnit.Point, 0);
            Chex_HienMatKhau.ForeColor = Color.Red;
            Chex_HienMatKhau.Location = new Point(148, 114);
            Chex_HienMatKhau.Margin = new Padding(3, 2, 3, 2);
            Chex_HienMatKhau.Name = "Chex_HienMatKhau";
            Chex_HienMatKhau.Size = new Size(123, 24);
            Chex_HienMatKhau.TabIndex = 0;
            Chex_HienMatKhau.Text = "Hiện mật khẩu";
            Chex_HienMatKhau.UseVisualStyleBackColor = true;
            Chex_HienMatKhau.CheckedChanged += Chex_HienMatKhau_CheckedChanged;
            // 
            // ImageList1
            // 
            ImageList1.ColorDepth = ColorDepth.Depth32Bit;
            ImageList1.ImageSize = new Size(16, 16);
            ImageList1.TransparentColor = Color.Transparent;
            // 
            // ImageList2
            // 
            ImageList2.ColorDepth = ColorDepth.Depth32Bit;
            ImageList2.ImageSize = new Size(16, 16);
            ImageList2.TransparentColor = Color.Transparent;
            // 
            // TableLayoutPanel5
            // 
            TableLayoutPanel5.ColumnCount = 2;
            TableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28.6912746F));
            TableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 71.30872F));
            TableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 16F));
            TableLayoutPanel5.Controls.Add(TableLayoutPanel1, 1, 0);
            TableLayoutPanel5.Controls.Add(PictureBox3, 0, 0);
            TableLayoutPanel5.Dock = DockStyle.Fill;
            TableLayoutPanel5.Location = new Point(0, 0);
            TableLayoutPanel5.Margin = new Padding(3, 2, 3, 2);
            TableLayoutPanel5.Name = "TableLayoutPanel5";
            TableLayoutPanel5.RowCount = 1;
            TableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            TableLayoutPanel5.Size = new Size(596, 210);
            TableLayoutPanel5.TabIndex = 12;
            // 
            // PictureBox3
            // 
            PictureBox3.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            PictureBox3.Image = (Image)resources.GetObject("PictureBox3.Image");
            PictureBox3.Location = new Point(3, 32);
            PictureBox3.Margin = new Padding(3, 2, 3, 2);
            PictureBox3.Name = "PictureBox3";
            PictureBox3.Size = new Size(165, 145);
            PictureBox3.SizeMode = PictureBoxSizeMode.StretchImage;
            PictureBox3.TabIndex = 13;
            PictureBox3.TabStop = false;
            // 
            // Form8_CauHinhCSDL
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(596, 210);
            Controls.Add(TableLayoutPanel5);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 2, 3, 2);
            Name = "Form8_CauHinhCSDL";
            Text = "Cấu hình cơ sở dữ liệu";
            Load += Form8_Load;
            TableLayoutPanel1.ResumeLayout(false);
            TableLayoutPanel1.PerformLayout();
            TableLayoutPanel3.ResumeLayout(false);
            TableLayoutPanel2.ResumeLayout(false);
            TableLayoutPanel2.PerformLayout();
            TableLayoutPanel5.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)PictureBox3).EndInit();
            ResumeLayout(false);
        }

        #endregion

        internal TableLayoutPanel TableLayoutPanel1;
        internal TableLayoutPanel TableLayoutPanel3;
        internal Krypton.Toolkit.KryptonButton btn_DangNhap;
        internal Krypton.Toolkit.KryptonButton btn_Thoat;
        internal TableLayoutPanel TableLayoutPanel2;
        internal CheckBox Chex_HienMatKhau;
        internal ImageList ImageList1;
        internal ImageList ImageList2;
        internal TableLayoutPanel TableLayoutPanel5;
        internal PictureBox PictureBox3;
        private Krypton.Toolkit.KryptonTextBox Text_Password;
        private Krypton.Toolkit.KryptonTextBox Text_Admin;
        private Label label1;
        private Label label2;
    }
}