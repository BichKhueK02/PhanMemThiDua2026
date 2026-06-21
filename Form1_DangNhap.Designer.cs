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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            TableLayoutPanel5 = new TableLayoutPanel();
            TableLayoutPanel1 = new TableLayoutPanel();
            TableLayoutPanel3 = new TableLayoutPanel();
            btn_Thoat = new Krypton.Toolkit.KryptonButton();
            btn_DangNhap = new Krypton.Toolkit.KryptonButton();
            tableLayoutPanel7 = new TableLayoutPanel();
            label1_PhienBanPhanMem = new Label();
            label1_ThongBaoPhienBan = new Label();
            TableLayoutPanel2 = new TableLayoutPanel();
            Check_HienMatKhau = new CheckBox();
            text_MatKhau = new Krypton.Toolkit.KryptonTextBox();
            label2 = new Label();
            text_TenDangNhap = new Krypton.Toolkit.KryptonTextBox();
            label1 = new Label();
            TableLayoutPanel6 = new TableLayoutPanel();
            LinkLabel_QuenMatKhau = new LinkLabel();
            LinkLabel1_DangKyTaiKhoanMoi = new LinkLabel();
            PictureBox1 = new PictureBox();
            toolTip1 = new ToolTip(components);
            TableLayoutPanel5.SuspendLayout();
            TableLayoutPanel1.SuspendLayout();
            TableLayoutPanel3.SuspendLayout();
            tableLayoutPanel7.SuspendLayout();
            TableLayoutPanel2.SuspendLayout();
            TableLayoutPanel6.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)PictureBox1).BeginInit();
            SuspendLayout();
            // 
            // TableLayoutPanel5
            // 
            TableLayoutPanel5.ColumnCount = 2;
            TableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26.9349842F));
            TableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 73.06502F));
            TableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 16F));
            TableLayoutPanel5.Controls.Add(TableLayoutPanel1, 1, 0);
            TableLayoutPanel5.Controls.Add(TableLayoutPanel6, 0, 0);
            TableLayoutPanel5.Dock = DockStyle.Fill;
            TableLayoutPanel5.Location = new Point(0, 0);
            TableLayoutPanel5.Margin = new Padding(3, 2, 3, 2);
            TableLayoutPanel5.Name = "TableLayoutPanel5";
            TableLayoutPanel5.RowCount = 1;
            TableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            TableLayoutPanel5.Size = new Size(659, 224);
            TableLayoutPanel5.TabIndex = 12;
            // 
            // TableLayoutPanel1
            // 
            TableLayoutPanel1.ColumnCount = 1;
            TableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            TableLayoutPanel1.Controls.Add(TableLayoutPanel3, 0, 1);
            TableLayoutPanel1.Controls.Add(tableLayoutPanel7, 0, 2);
            TableLayoutPanel1.Controls.Add(TableLayoutPanel2, 0, 0);
            TableLayoutPanel1.Dock = DockStyle.Fill;
            TableLayoutPanel1.Location = new Point(180, 2);
            TableLayoutPanel1.Margin = new Padding(3, 2, 3, 2);
            TableLayoutPanel1.Name = "TableLayoutPanel1";
            TableLayoutPanel1.RowCount = 3;
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 60.7758636F));
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 21.98276F));
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 17.2413788F));
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            TableLayoutPanel1.Size = new Size(476, 220);
            TableLayoutPanel1.TabIndex = 9;
            // 
            // TableLayoutPanel3
            // 
            TableLayoutPanel3.ColumnCount = 2;
            TableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            TableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            TableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 16F));
            TableLayoutPanel3.Controls.Add(btn_Thoat, 0, 0);
            TableLayoutPanel3.Controls.Add(btn_DangNhap, 1, 0);
            TableLayoutPanel3.Dock = DockStyle.Fill;
            TableLayoutPanel3.Location = new Point(3, 135);
            TableLayoutPanel3.Margin = new Padding(3, 2, 3, 2);
            TableLayoutPanel3.Name = "TableLayoutPanel3";
            TableLayoutPanel3.RowCount = 1;
            TableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            TableLayoutPanel3.Size = new Size(470, 44);
            TableLayoutPanel3.TabIndex = 10;
            // 
            // btn_Thoat
            // 
            btn_Thoat.Anchor = AnchorStyles.None;
            btn_Thoat.DialogResult = DialogResult.TryAgain;
            btn_Thoat.Location = new Point(63, 7);
            btn_Thoat.Margin = new Padding(3, 2, 3, 2);
            btn_Thoat.Name = "btn_Thoat";
            btn_Thoat.Size = new Size(108, 30);
            btn_Thoat.StateCommon.Border.Rounding = 4F;
            btn_Thoat.StateTracking.Back.Color1 = Color.FromArgb(255, 128, 255);
            btn_Thoat.StateTracking.Back.Color2 = Color.FromArgb(255, 128, 255);
            btn_Thoat.TabIndex = 1;
            btn_Thoat.Values.DropDownArrowColor = Color.Empty;
            btn_Thoat.Values.Image = (Image)resources.GetObject("btn_Thoat.Values.Image");
            btn_Thoat.Values.Text = "Thoát";
            btn_Thoat.Click += btn_Thoat_Click;
            // 
            // btn_DangNhap
            // 
            btn_DangNhap.Anchor = AnchorStyles.None;
            btn_DangNhap.DialogResult = DialogResult.TryAgain;
            btn_DangNhap.Location = new Point(298, 7);
            btn_DangNhap.Margin = new Padding(3, 2, 3, 2);
            btn_DangNhap.Name = "btn_DangNhap";
            btn_DangNhap.Size = new Size(108, 30);
            btn_DangNhap.StateCommon.Border.Rounding = 4F;
            btn_DangNhap.StateTracking.Back.Color1 = Color.FromArgb(192, 255, 192);
            btn_DangNhap.StateTracking.Back.Color2 = Color.FromArgb(192, 255, 192);
            btn_DangNhap.TabIndex = 0;
            btn_DangNhap.Values.DropDownArrowColor = Color.Empty;
            btn_DangNhap.Values.Image = (Image)resources.GetObject("btn_DangNhap.Values.Image");
            btn_DangNhap.Values.Text = "Đăng nhập";
            btn_DangNhap.Click += btn_DangNhap_Click;
            // 
            // tableLayoutPanel7
            // 
            tableLayoutPanel7.ColumnCount = 2;
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72.04659F));
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27.9534111F));
            tableLayoutPanel7.Controls.Add(label1_PhienBanPhanMem, 1, 0);
            tableLayoutPanel7.Controls.Add(label1_ThongBaoPhienBan, 0, 0);
            tableLayoutPanel7.Dock = DockStyle.Fill;
            tableLayoutPanel7.Location = new Point(3, 183);
            tableLayoutPanel7.Margin = new Padding(3, 2, 3, 2);
            tableLayoutPanel7.Name = "tableLayoutPanel7";
            tableLayoutPanel7.RowCount = 1;
            tableLayoutPanel7.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel7.Size = new Size(470, 35);
            tableLayoutPanel7.TabIndex = 12;
            // 
            // label1_PhienBanPhanMem
            // 
            label1_PhienBanPhanMem.Anchor = AnchorStyles.None;
            label1_PhienBanPhanMem.AutoSize = true;
            label1_PhienBanPhanMem.Font = new Font("Segoe UI Light", 8.064F, FontStyle.Italic, GraphicsUnit.Point, 0);
            label1_PhienBanPhanMem.ForeColor = Color.Blue;
            label1_PhienBanPhanMem.Location = new Point(398, 11);
            label1_PhienBanPhanMem.Name = "label1_PhienBanPhanMem";
            label1_PhienBanPhanMem.Size = new Size(11, 13);
            label1_PhienBanPhanMem.TabIndex = 13;
            label1_PhienBanPhanMem.Text = "*";
            label1_PhienBanPhanMem.TextAlign = ContentAlignment.MiddleLeft;
            label1_PhienBanPhanMem.Click += label1_PhienBanPhanMem_Click;
            // 
            // label1_ThongBaoPhienBan
            // 
            label1_ThongBaoPhienBan.Anchor = AnchorStyles.None;
            label1_ThongBaoPhienBan.AutoSize = true;
            label1_ThongBaoPhienBan.Font = new Font("Segoe UI", 8.064F, FontStyle.Italic, GraphicsUnit.Point, 0);
            label1_ThongBaoPhienBan.ForeColor = Color.Green;
            label1_ThongBaoPhienBan.Location = new Point(151, 11);
            label1_ThongBaoPhienBan.Name = "label1_ThongBaoPhienBan";
            label1_ThongBaoPhienBan.Size = new Size(36, 13);
            label1_ThongBaoPhienBan.TabIndex = 12;
            label1_ThongBaoPhienBan.Text = "label1";
            label1_ThongBaoPhienBan.TextAlign = ContentAlignment.BottomCenter;
            // 
            // TableLayoutPanel2
            // 
            TableLayoutPanel2.ColumnCount = 3;
            TableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29.95432F));
            TableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68.30541F));
            TableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 1.740265F));
            TableLayoutPanel2.Controls.Add(Check_HienMatKhau, 1, 3);
            TableLayoutPanel2.Controls.Add(text_MatKhau, 1, 2);
            TableLayoutPanel2.Controls.Add(label2, 0, 2);
            TableLayoutPanel2.Controls.Add(text_TenDangNhap, 1, 1);
            TableLayoutPanel2.Controls.Add(label1, 0, 1);
            TableLayoutPanel2.Dock = DockStyle.Fill;
            TableLayoutPanel2.Location = new Point(3, 2);
            TableLayoutPanel2.Margin = new Padding(3, 2, 3, 2);
            TableLayoutPanel2.Name = "TableLayoutPanel2";
            TableLayoutPanel2.RowCount = 4;
            TableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 2.969709F));
            TableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 37.09981F));
            TableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 32.1056137F));
            TableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 27.8248615F));
            TableLayoutPanel2.Size = new Size(470, 129);
            TableLayoutPanel2.TabIndex = 1;
            // 
            // Check_HienMatKhau
            // 
            Check_HienMatKhau.Anchor = AnchorStyles.Left;
            Check_HienMatKhau.AutoSize = true;
            Check_HienMatKhau.Font = new Font("Segoe UI Semibold", 9.792F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            Check_HienMatKhau.ForeColor = Color.Red;
            Check_HienMatKhau.Location = new Point(143, 98);
            Check_HienMatKhau.Margin = new Padding(3, 2, 3, 2);
            Check_HienMatKhau.Name = "Check_HienMatKhau";
            Check_HienMatKhau.Size = new Size(121, 23);
            Check_HienMatKhau.TabIndex = 0;
            Check_HienMatKhau.Text = "Hiện mật khẩu";
            Check_HienMatKhau.UseVisualStyleBackColor = true;
            // 
            // text_MatKhau
            // 
            text_MatKhau.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            text_MatKhau.Location = new Point(143, 56);
            text_MatKhau.Margin = new Padding(3, 2, 3, 2);
            text_MatKhau.Name = "text_MatKhau";
            text_MatKhau.Size = new Size(315, 29);
            text_MatKhau.StateCommon.Border.Rounding = 8F;
            text_MatKhau.StateCommon.Border.Width = 1;
            text_MatKhau.TabIndex = 11;
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Right;
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI Semibold", 9.792F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.FromArgb(0, 0, 192);
            label2.Location = new Point(68, 61);
            label2.Name = "label2";
            label2.Size = new Size(69, 19);
            label2.TabIndex = 12;
            label2.Text = "Mật khẩu";
            // 
            // text_TenDangNhap
            // 
            text_TenDangNhap.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            text_TenDangNhap.Location = new Point(143, 12);
            text_TenDangNhap.Margin = new Padding(3, 2, 3, 2);
            text_TenDangNhap.Name = "text_TenDangNhap";
            text_TenDangNhap.Size = new Size(315, 29);
            text_TenDangNhap.StateCommon.Border.Rounding = 8F;
            text_TenDangNhap.StateCommon.Border.Width = 1;
            text_TenDangNhap.TabIndex = 2;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Right;
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI Semibold", 9.792F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.FromArgb(0, 0, 192);
            label1.Location = new Point(41, 17);
            label1.Name = "label1";
            label1.Size = new Size(96, 19);
            label1.TabIndex = 3;
            label1.Text = "Tên tài khoản";
            // 
            // TableLayoutPanel6
            // 
            TableLayoutPanel6.ColumnCount = 1;
            TableLayoutPanel6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            TableLayoutPanel6.Controls.Add(LinkLabel_QuenMatKhau, 0, 2);
            TableLayoutPanel6.Controls.Add(LinkLabel1_DangKyTaiKhoanMoi, 0, 1);
            TableLayoutPanel6.Controls.Add(PictureBox1, 0, 0);
            TableLayoutPanel6.Dock = DockStyle.Fill;
            TableLayoutPanel6.Location = new Point(3, 2);
            TableLayoutPanel6.Margin = new Padding(3, 2, 3, 2);
            TableLayoutPanel6.Name = "TableLayoutPanel6";
            TableLayoutPanel6.RowCount = 3;
            TableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Percent, 74.09091F));
            TableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Percent, 13.636364F));
            TableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Percent, 11.818182F));
            TableLayoutPanel6.Size = new Size(171, 220);
            TableLayoutPanel6.TabIndex = 0;
            // 
            // LinkLabel_QuenMatKhau
            // 
            LinkLabel_QuenMatKhau.Anchor = AnchorStyles.None;
            LinkLabel_QuenMatKhau.AutoSize = true;
            LinkLabel_QuenMatKhau.Font = new Font("Segoe UI", 8.064F, FontStyle.Italic, GraphicsUnit.Point, 0);
            LinkLabel_QuenMatKhau.Location = new Point(45, 200);
            LinkLabel_QuenMatKhau.Name = "LinkLabel_QuenMatKhau";
            LinkLabel_QuenMatKhau.Size = new Size(80, 13);
            LinkLabel_QuenMatKhau.TabIndex = 1;
            LinkLabel_QuenMatKhau.TabStop = true;
            LinkLabel_QuenMatKhau.Text = "Quên mật khẩu";
            LinkLabel_QuenMatKhau.LinkClicked += LinkLabel_QuenMatKhau_LinkClicked;
            // 
            // LinkLabel1_DangKyTaiKhoanMoi
            // 
            LinkLabel1_DangKyTaiKhoanMoi.Anchor = AnchorStyles.None;
            LinkLabel1_DangKyTaiKhoanMoi.AutoSize = true;
            LinkLabel1_DangKyTaiKhoanMoi.Font = new Font("Segoe UI", 8.064F, FontStyle.Italic, GraphicsUnit.Point, 0);
            LinkLabel1_DangKyTaiKhoanMoi.Location = new Point(28, 171);
            LinkLabel1_DangKyTaiKhoanMoi.Name = "LinkLabel1_DangKyTaiKhoanMoi";
            LinkLabel1_DangKyTaiKhoanMoi.Size = new Size(115, 13);
            LinkLabel1_DangKyTaiKhoanMoi.TabIndex = 0;
            LinkLabel1_DangKyTaiKhoanMoi.TabStop = true;
            LinkLabel1_DangKyTaiKhoanMoi.Text = "Đăng ký tài khoản mới";
            LinkLabel1_DangKyTaiKhoanMoi.LinkClicked += LinkLabel1_DangKyTaiKhoanMoi_LinkClicked;
            // 
            // PictureBox1
            // 
            PictureBox1.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            PictureBox1.Image = (Image)resources.GetObject("PictureBox1.Image");
            PictureBox1.Location = new Point(4, 18);
            PictureBox1.Margin = new Padding(4, 2, 4, 2);
            PictureBox1.Name = "PictureBox1";
            PictureBox1.Size = new Size(163, 126);
            PictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            PictureBox1.TabIndex = 0;
            PictureBox1.TabStop = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(659, 224);
            Controls.Add(TableLayoutPanel5);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 2, 3, 2);
            Name = "Form1";
            Text = "Đăng nhập";
            Load += Form1_Load;
            TableLayoutPanel5.ResumeLayout(false);
            TableLayoutPanel1.ResumeLayout(false);
            TableLayoutPanel3.ResumeLayout(false);
            tableLayoutPanel7.ResumeLayout(false);
            tableLayoutPanel7.PerformLayout();
            TableLayoutPanel2.ResumeLayout(false);
            TableLayoutPanel2.PerformLayout();
            TableLayoutPanel6.ResumeLayout(false);
            TableLayoutPanel6.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)PictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        internal TableLayoutPanel TableLayoutPanel5;
        internal TableLayoutPanel TableLayoutPanel1;
        internal TableLayoutPanel TableLayoutPanel3;
        internal TableLayoutPanel TableLayoutPanel2;
        internal CheckBox Check_HienMatKhau;
        internal TableLayoutPanel TableLayoutPanel6;
        internal LinkLabel LinkLabel_QuenMatKhau;
        internal LinkLabel LinkLabel1_DangKyTaiKhoanMoi;
        internal PictureBox PictureBox1;
        private Krypton.Toolkit.KryptonButton btn_Thoat;
        private Krypton.Toolkit.KryptonButton btn_DangNhap;
        private Label label1_ThongBaoPhienBan;
        private TableLayoutPanel tableLayoutPanel7;
        private Label label1_PhienBanPhanMem;
        private ToolTip toolTip1;
        private Krypton.Toolkit.KryptonTextBox text_TenDangNhap;
        private Krypton.Toolkit.KryptonTextBox text_MatKhau;
        private Label label2;
        private Label label1;
    }
}
